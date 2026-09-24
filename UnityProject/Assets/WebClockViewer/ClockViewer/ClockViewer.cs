using Cysharp.Threading.Tasks;
using MirraGames.SDK;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace WebClockViewer
{
    internal class ClockViewer : MonoBehaviour, IAsyncInitializable
    {
        [SerializeField] private Text _utcTimeText;
        [SerializeField] private Text _localTimeText;

        /// <summary>
        /// Shows the current time and starts ticking the labels. The ticking keeps running past the
        /// returned task and stops once <paramref name="cancellationToken"/> is raised or the
        /// object is destroyed.
        /// </summary>
        public UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RefreshLabels();
            ClockUpdateTask(cancellationToken).Forget();
            return UniTask.CompletedTask;
        }

        private async UniTask ClockUpdateTask(CancellationToken cancellationToken)
        {
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, this.GetCancellationTokenOnDestroy());

            while (true)
            {
                // Wake right after the next second boundary so the seconds tick in step with the clock.
                int millisecondsToNextSecond = 1000 - MirraSDK.Time.CurrentDate.Millisecond;
                await UniTask.Delay(millisecondsToNextSecond, DelayType.Realtime, cancellationToken: linkedSource.Token);
                RefreshLabels();
            }
        }

        private void RefreshLabels()
        {
            DateTime currentDate = MirraSDK.Time.CurrentDate;
            DateTime utcDate = currentDate.ToUniversalTime();

            _utcTimeText.text = $"UTC: {utcDate:HH:mm:ss}";
            _localTimeText.text = $"Local time: {currentDate:HH:mm:ss}";
        }
    }
}
