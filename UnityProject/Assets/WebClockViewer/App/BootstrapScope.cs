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

        protected override void Configure(IContainerBuilder builder)
        {
            _consoleInstance.CreateText(this, nameof(Configure));

            builder.RegisterInstance<IConsoleInstance>(_consoleInstance);
        }

        private void Start()
        {
            _consoleInstance.CreateText(this, nameof(Start));
            MirraSDK.WaitForProviders(() =>
            {
                _consoleInstance.CreateText(this, nameof(MirraSDK), "onInitialized called");
                MainEntry().Forget();
            });
        }

        private async UniTask MainEntry()
        {
            _consoleInstance.CreateText(this, nameof(MainEntry));
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

        }
    }
}