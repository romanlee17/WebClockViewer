using Cysharp.Threading.Tasks;
using reromanlee.ConsoleContainer;
using System;
using System.Threading;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace WebClockViewer
{
    /// <summary>
    /// Lets the user edit a clock: opens the time picker for it and applies the choice to
    /// <see cref="IClockTimeEditor"/>. The picker prefab is loaded on first use and kept loaded
    /// until <see cref="IAddressableLoader"/> releases it at the end of the app.
    /// </summary>
    internal sealed class TimeEditingFlow
    {
        private const string DialogAddress = "TimePickerDialog";

        private readonly IClockTimeEditor _clockTime;
        private readonly IAddressableLoader _addressableLoader;
        private readonly IObjectResolver _resolver;
        private readonly IConsoleInstance _consoleInstance;

        private GameObject _dialogPrefab;
        private bool _isEditing;

        public TimeEditingFlow(IClockTimeEditor clockTime, IAddressableLoader addressableLoader,
            IObjectResolver resolver, IConsoleInstance consoleInstance)
        {
            _clockTime = clockTime;
            _addressableLoader = addressableLoader;
            _resolver = resolver;
            _consoleInstance = consoleInstance;
        }

        /// <summary>
        /// Opens the picker for <paramref name="zone"/>'s clock and applies what the user chose.
        /// Does nothing while a picker is already open.
        /// </summary>
        public async UniTask EditAsync(ClockZone zone, CancellationToken cancellationToken)
        {
            if (_isEditing)
            {
                return;
            }

            _isEditing = true;
            try
            {
                GameObject dialogPrefab = await LoadDialogPrefab(cancellationToken);

                DateTime now = _clockTime.Now;
                DateTime zonedNow = zone == ClockZone.Utc ? now.ToUniversalTime() : now;
                TimePickerRequest request = new(
                    zone == ClockZone.Utc ? "UTC" : "local",
                    new TimeSpan(zonedNow.Hour, zonedNow.Minute, 0),
                    _clockTime.IsCustom);

                TimePickerDialog dialog = _resolver.Instantiate(dialogPrefab).GetComponent<TimePickerDialog>();
                TimePickerResult result;
                try
                {
                    result = await dialog.ShowAsync(request, cancellationToken);
                }
                finally
                {
                    if (dialog != null)
                    {
                        Object.Destroy(dialog.gameObject);
                    }
                }

                Apply(result, zone);
            }
            finally
            {
                _isEditing = false;
            }
        }

        private void Apply(TimePickerResult result, ClockZone zone)
        {
            switch (result.Action)
            {
                case TimePickerAction.Confirm:
                    _clockTime.SetTimeOfDay(result.Time, zone);
                    _consoleInstance.CreateText(this, nameof(Apply), $"custom {zone} time set to {result.Time:hh\\:mm}");
                    break;
                case TimePickerAction.Reset:
                    _clockTime.ResetToNetworkTime();
                    _consoleInstance.CreateText(this, nameof(Apply), "back to network time");
                    break;
            }
        }

        private async UniTask<GameObject> LoadDialogPrefab(CancellationToken cancellationToken)
        {
            if (_dialogPrefab != null)
            {
                return _dialogPrefab;
            }

            _dialogPrefab = await _addressableLoader.LoadAsync<GameObject>(DialogAddress, cancellationToken);
            return _dialogPrefab;
        }
    }
}
