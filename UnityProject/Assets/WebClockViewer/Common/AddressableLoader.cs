using Cysharp.Threading.Tasks;
using MirraGames.SDK;
using System;
using System.Collections.Generic;
using System.Threading;

namespace WebClockViewer
{
    /// <summary>Loads Addressables through MirraSDK with reference counting per address.</summary>
    internal interface IAddressableLoader
    {
        /// <summary>
        /// Loads the asset at <paramref name="address"/> and takes one reference to it, which the
        /// caller gives back with <see cref="Release"/>. When the load fails or is cancelled the
        /// reference is dropped here, so the caller releases only after a successful load. Use one
        /// asset type per address.
        /// </summary>
        UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken);

        /// <summary>Gives back one reference taken by <see cref="LoadAsync{T}"/>.</summary>
        void Release(string address);
    }

    /// <summary>
    /// Keeps MirraSDK's Addressables provider on its safe paths. The provider tracks one handle per
    /// address, so this loader never starts a second load for an address that is still loaded or
    /// loading; callers share the load and its references are counted here. It also never releases
    /// a handle before its load completes, because Addressables recycles an operation released
    /// in flight while the provider's completion callback stays attached to it.
    /// </summary>
    internal sealed class AddressableLoader : IAddressableLoader, IDisposable
    {
        private readonly Dictionary<string, Entry> _entries = new();
        private bool _isDisposed;

        public async UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AddressableLoader));
            }

            // The reference is taken before the load starts, so a load that completes right away
            // is not mistaken for one nobody wants.
            bool isNewEntry = !_entries.TryGetValue(address, out Entry entry);
            if (isNewEntry)
            {
                entry = new Entry();
                _entries.Add(address, entry);
            }
            entry.References++;
            if (isNewEntry)
            {
                StartLoad<T>(address, entry);
            }

            try
            {
                object asset = await entry.Source.Task.AttachExternalCancellation(cancellationToken);

                // The provider completes from inside Addressables' completion callback. Resuming on
                // the next frame keeps callers' instantiation and setup out of that callback.
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                return (T)asset;
            }
            catch
            {
                // Released by entry, not address: after a failure a newer load may own the address.
                ReleaseEntry(address, entry);
                throw;
            }
        }

        public void Release(string address)
        {
            if (_entries.TryGetValue(address, out Entry entry))
            {
                ReleaseEntry(address, entry);
            }
        }

        private void ReleaseEntry(string address, Entry entry)
        {
            if (entry.References == 0)
            {
                return;
            }

            entry.References--;
            if (entry.References == 0 && entry.IsLoaded && RemoveEntry(address, entry))
            {
                ReleaseThroughSdk(address);
            }
            // Still loading: OnLoaded releases it unless someone takes a reference first.
        }

        /// <summary>
        /// Releases every address still held when the owning scope ends. Loads still in flight are
        /// released as they complete.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (KeyValuePair<string, Entry> pair in _entries)
            {
                pair.Value.References = 0;
                if (pair.Value.IsLoaded)
                {
                    ReleaseThroughSdk(pair.Key);
                }
            }
            _entries.Clear();
        }

        private void StartLoad<T>(string address, Entry entry)
        {
            try
            {
                MirraSDK.Assets.LoadAddressable<T>(
                    address,
                    onSuccess: asset => OnLoaded(address, entry, asset),
                    onError: () => OnFailed(address, entry));
            }
            catch (Exception exception)
            {
                // Without this the entry would stay pending and every later load of the address would hang.
                RemoveEntry(address, entry);
                entry.Source.TrySetException(exception);
            }
        }

        private void OnLoaded(string address, Entry entry, object asset)
        {
            entry.IsLoaded = true;
            if (entry.References == 0)
            {
                // Everyone gave up, or the scope ended, while the load was in flight.
                RemoveEntry(address, entry);
                ReleaseThroughSdk(address);
            }
            entry.Source.TrySetResult(asset);
        }

        private void OnFailed(string address, Entry entry)
        {
            // The provider already forgot the failed handle, so there is nothing to release.
            RemoveEntry(address, entry);
            entry.Source.TrySetException(new InvalidOperationException($"Failed to load addressable '{address}'."));
        }

        /// <summary>Removes <paramref name="entry"/> if it still owns the address; true when it did.</summary>
        private bool RemoveEntry(string address, Entry entry)
        {
            return _entries.TryGetValue(address, out Entry current) && current == entry && _entries.Remove(address);
        }

        private static void ReleaseThroughSdk(string address)
        {
            // MirraSDK drops its instance when its own object is destroyed, and at quit or when
            // leaving Play Mode that can happen before our scope ends. Addressables unloads
            // everything then anyway, so a release is only needed while the SDK is alive.
            if (MirraSDK.IsInitialized)
            {
                MirraSDK.Assets.ReleaseAddressable(address);
            }
        }

        private sealed class Entry
        {
            public readonly UniTaskCompletionSource<object> Source = new();
            public int References;
            public bool IsLoaded;
        }
    }
}
