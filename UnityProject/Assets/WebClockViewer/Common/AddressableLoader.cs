using Cysharp.Threading.Tasks;
using MirraGames.SDK;
using System;
using System.Threading;

namespace WebClockViewer
{
    /// <summary>Awaitable wrapper over MirraSDK's callback-based Addressables loading.</summary>
    internal static class AddressableLoader
    {
        /// <summary>
        /// Loads the asset at <paramref name="address"/>. The load starts even when the token is
        /// cancelled later, so callers release the address with
        /// <c>MirraSDK.Assets.ReleaseAddressable</c> once they called this, whatever the outcome.
        /// </summary>
        public static async UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken)
        {
            UniTaskCompletionSource<T> loadSource = new();
            MirraSDK.Assets.LoadAddressable<T>(
                address,
                onSuccess: asset => loadSource.TrySetResult(asset),
                onError: () => loadSource.TrySetException(
                    new InvalidOperationException($"Failed to load addressable '{address}'.")));

            return await loadSource.Task.AttachExternalCancellation(cancellationToken);
        }
    }
}
