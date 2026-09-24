using Cysharp.Threading.Tasks;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
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

        }

        private async void Start()
        {

        }

        private async UniTask MainEntry()
        {

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