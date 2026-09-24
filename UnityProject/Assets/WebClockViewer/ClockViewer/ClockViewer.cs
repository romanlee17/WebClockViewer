using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace WebClockViewer
{
    internal class ClockViewer : MonoBehaviour, IAsyncInitializable
    {
        [SerializeField] private Text _utcTimeText;
        [SerializeField] private Text _utcDateText;
        [SerializeField] private Text _localTimeText;
        [SerializeField] private Text _localDateText;
        [SerializeField] private AnalogClock _utcAnalogClock;
        [SerializeField] private AnalogClock _localAnalogClock;

        private IClockTime _clockTime;

        [Inject]
        public void Construct(IClockTime clockTime)
        {
            _clockTime = clockTime;
        }

        /// <summary>
        /// Shows the current time and starts ticking the labels. The ticking keeps running past the
        /// returned task and stops once <paramref name="cancellationToken"/> is raised or the
        /// object is destroyed.
        /// </summary>
        public UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _clockTime.Changed += RefreshLabels;
            RefreshLabels();
            ClockUpdateTask(cancellationToken).Forget();
            return UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            if (_clockTime != null)
            {
                _clockTime.Changed -= RefreshLabels;
            }
        }

        private async UniTask ClockUpdateTask(CancellationToken cancellationToken)
        {
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, this.GetCancellationTokenOnDestroy());

            while (true)
            {
                // Wake right after the next second boundary so the seconds tick in step with the clock.
                int millisecondsToNextSecond = 1000 - _clockTime.Now.Millisecond;
                await UniTask.Delay(millisecondsToNextSecond, DelayType.Realtime, cancellationToken: linkedSource.Token);
                RefreshLabels();
            }
        }

        private void RefreshLabels()
        {
            DateTime currentDate = _clockTime.Now;
            DateTime utcDate = currentDate.ToUniversalTime();

            _utcTimeText.text = $"UTC time: {utcDate:HH:mm:ss}";
            _utcDateText.text = $"UTC date: {utcDate:yyyy-MM-dd}";
            _localTimeText.text = $"Local time: {currentDate:HH:mm:ss}";
            _localDateText.text = $"Local date: {currentDate:yyyy-MM-dd}";

            _utcAnalogClock.SetTime(utcDate);
            _localAnalogClock.SetTime(currentDate);
        }
    }
}
