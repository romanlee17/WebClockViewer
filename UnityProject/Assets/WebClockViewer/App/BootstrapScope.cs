using CustomTimeService;
using Cysharp.Threading.Tasks;
using MirraGames.SDK;
using reromanlee.ConsoleContainer;
using System;
using System.Threading;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace WebClockViewer
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class BootstrapScope : LifetimeScope
    {
        private const string ClockViewerAddress = "ClockViewer";

        private static IConsoleInstance _consoleInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            _consoleInstance = new ConsoleInstance("App");
            _consoleInstance.CreateText(nameof(BootstrapScope), nameof(AfterSceneLoad));

            GameObject bootstrapObject = new(nameof(BootstrapScope));
            bootstrapObject.AddComponent<BootstrapScope>();
            DontDestroyOnLoad(bootstrapObject);
        }

        /// <summary>
        /// Cancelled when the app ends, whether that end is the object being destroyed or a fatal
        /// error stopping everything. Every long-running service takes its token, so both routes
        /// bring the same background work down.
        /// </summary>
        private CancellationTokenSource _appLifetimeSource;

        private bool _isClockViewerLoaded;

        protected override void Configure(IContainerBuilder builder)
        {
            _consoleInstance.CreateText(this, nameof(Configure));

            builder.RegisterInstance<IConsoleInstance>(_consoleInstance);
        }

        private void Start()
        {
            _consoleInstance.CreateText(this, nameof(Start));
            _appLifetimeSource = new CancellationTokenSource();
            CancellationToken appLifetimeToken = _appLifetimeSource.Token;

            MirraSDK.WaitForProviders(() =>
            {
                _consoleInstance.CreateText(this, nameof(MirraSDK), "onInitialized called");
                MainEntry(appLifetimeToken).Forget();
            });
        }

        private async UniTask MainEntry(CancellationToken cancellationToken)
        {
            _consoleInstance.CreateText(this, nameof(MainEntry));
            try
            {
                await WaitForTimeSynchronization(cancellationToken);
                _consoleInstance.CreateText(this, nameof(MainEntry), "time synchronized");

                GameObject clockViewerPrefab = await LoadClockViewerPrefab(cancellationToken);
                ClockViewer clockViewer = Container.Instantiate(clockViewerPrefab).GetComponent<ClockViewer>();
                await clockViewer.InitializeAsync(cancellationToken);

                _consoleInstance.CreateText(this, nameof(MainEntry), "clock viewer initialized");
            }
            catch (OperationCanceledException)
            {
                // The app ended before startup finished; nothing is left to show.
            }
            catch (Exception exception)
            {
                _consoleInstance.CreateError(this, nameof(MainEntry), exception.ToString());
                CancelAppLifetime();
            }
        }

        private static async UniTask WaitForTimeSynchronization(CancellationToken cancellationToken)
        {
            UniTaskCompletionSource synchronizedSource = new();
            WebDateTime.WaitForSynchronization(() => synchronizedSource.TrySetResult());
            await synchronizedSource.Task.AttachExternalCancellation(cancellationToken);
        }

        private async UniTask<GameObject> LoadClockViewerPrefab(CancellationToken cancellationToken)
        {
            UniTaskCompletionSource<GameObject> loadSource = new();
            MirraSDK.Assets.LoadAddressable<GameObject>(
                ClockViewerAddress,
                onSuccess: prefab => loadSource.TrySetResult(prefab),
                onError: () => loadSource.TrySetException(
                    new InvalidOperationException($"Failed to load addressable '{ClockViewerAddress}'.")));

            // The instance keeps using the prefab's assets, so the prefab stays loaded until the app ends.
            _isClockViewerLoaded = true;
            return await loadSource.Task.AttachExternalCancellation(cancellationToken);
        }

        private void CancelAppLifetime()
        {
            CancellationTokenSource appLifetimeSource = _appLifetimeSource;
            if (appLifetimeSource == null)
            {
                return;
            }

            try
            {
                appLifetimeSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The app was already shutting down and released the source; nothing left to stop.
            }
        }

        protected override void OnDestroy()
        {
            CancelAppLifetime();
            _appLifetimeSource?.Dispose();
            _appLifetimeSource = null;

            if (_isClockViewerLoaded)
            {
                MirraSDK.Assets.ReleaseAddressable(ClockViewerAddress);
                _isClockViewerLoaded = false;
            }

            base.OnDestroy();
        }
    }
}
