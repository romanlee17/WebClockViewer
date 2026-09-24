using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace WebClockViewer
{
    /// <summary>
    /// Material 3 time picker dialog with a draggable dial and a keyboard input mode. It only
    /// collects a time of day; the caller decides what the result means.
    /// </summary>
    internal sealed class TimePickerDialog : MonoBehaviour
    {
        private const float ShowDuration = 0.18f;
        private const float HideDuration = 0.12f;
        private const float ShownScale = 1.0f;
        private const float HiddenScale = 0.85f;

        [Header("Frame")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _container;
        [SerializeField] private Text _headline;
        [SerializeField] private Button _scrim;

        [Header("Dial mode")]
        [SerializeField] private Button _hourBox;
        [SerializeField] private Image _hourBoxImage;
        [SerializeField] private Text _hourBoxText;
        [SerializeField] private Button _minuteBox;
        [SerializeField] private Image _minuteBoxImage;
        [SerializeField] private Text _minuteBoxText;
        [SerializeField] private GameObject _dialRow;
        [SerializeField] private TimeDial _dial;

        [Header("Input mode")]
        [SerializeField] private InputField _hourField;
        [SerializeField] private Image _hourFieldImage;
        [SerializeField] private InputField _minuteField;
        [SerializeField] private Image _minuteFieldImage;
        [SerializeField] private GameObject _inputLabels;

        [Header("Actions")]
        [SerializeField] private Button _modeToggle;
        [SerializeField] private IconGraphic _modeIcon;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _okButton;

        [Header("Colors")]
        [SerializeField] private Color _boxColor = new(0.2f, 0.22f, 0.29f, 1.0f);
        [SerializeField] private Color _boxTextColor = new(0.89f, 0.9f, 0.95f, 1.0f);
        [SerializeField] private Color _selectedBoxColor = new(0.25f, 0.3f, 0.52f, 1.0f);
        [SerializeField] private Color _selectedBoxTextColor = new(0.87f, 0.89f, 1.0f, 1.0f);
        [SerializeField] private Color _errorBoxColor = new(0.55f, 0.11f, 0.09f, 1.0f);
        [SerializeField] private Color _errorTextColor = new(0.98f, 0.87f, 0.86f, 1.0f);

        [Header("Motion")]
        [SerializeField, Min(0.1f)] private float _springFrequency = 3.0f;
        [SerializeField, Range(0.05f, 1.0f)] private float _springDampingRatio = 0.6f;

        private UniTaskCompletionSource<TimePickerResult> _resultSource;
        private string _zoneName;
        private bool _isInputMode;
        private TimeDialMode _dialMode;
        private int _hour;
        private int _minute;
        private bool _hasHourError;
        private bool _hasMinuteError;

        private void Awake()
        {
            _scrim.onClick.AddListener(() => Close(TimePickerAction.Cancel));
            _cancelButton.onClick.AddListener(() => Close(TimePickerAction.Cancel));
            _resetButton.onClick.AddListener(() => Close(TimePickerAction.Reset));
            _okButton.onClick.AddListener(Confirm);
            _modeToggle.onClick.AddListener(ToggleMode);
            _hourBox.onClick.AddListener(() => SelectDialMode(TimeDialMode.Hour));
            _minuteBox.onClick.AddListener(() => SelectDialMode(TimeDialMode.Minute));
            _hourField.onValueChanged.AddListener(_ => SetFieldError(hourError: false, _hasMinuteError));
            _minuteField.onValueChanged.AddListener(_ => SetFieldError(_hasHourError, minuteError: false));
            _dial.ValueChanged += OnDialValueChanged;
            _dial.SelectionFinished += OnDialSelectionFinished;
        }

        /// <summary>
        /// Opens the dialog and completes once the user confirms, resets or cancels, after the
        /// closing animation. Cancelling <paramref name="cancellationToken"/> closes it as cancelled.
        /// </summary>
        public async UniTask<TimePickerResult> ShowAsync(TimePickerRequest request, CancellationToken cancellationToken)
        {
            _zoneName = request.ZoneName;
            _hour = request.InitialTime.Hours;
            _minute = request.InitialTime.Minutes;
            _resetButton.gameObject.SetActive(request.CanReset);
            _resultSource = new UniTaskCompletionSource<TimePickerResult>();

            SetInputMode(false);
            SelectDialMode(TimeDialMode.Hour, animate: false);

            CancellationToken destroyToken = this.GetCancellationTokenOnDestroy();
            AnimateAsync(HiddenScale, ShownScale, 0.0f, 1.0f, ShowDuration, destroyToken).Forget();

            TimePickerResult result;
            using (cancellationToken.Register(() => _resultSource.TrySetResult(
                       new TimePickerResult(TimePickerAction.Cancel, default))))
            {
                result = await _resultSource.Task;
            }

            _canvasGroup.interactable = false;
            await AnimateAsync(ShownScale, HiddenScale, 1.0f, 0.0f, HideDuration, destroyToken)
                .SuppressCancellationThrow();
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        private void Update()
        {
            if (_resultSource == null || !_canvasGroup.interactable)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close(TimePickerAction.Cancel);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Confirm();
            }
            else if (_isInputMode && Input.GetKeyDown(KeyCode.Tab))
            {
                (_hourField.isFocused ? _minuteField : _hourField).ActivateInputField();
            }

            if (_isInputMode)
            {
                ApplyFieldColors(_hourField, _hourFieldImage, _hasHourError);
                ApplyFieldColors(_minuteField, _minuteFieldImage, _hasMinuteError);
            }
        }

        private void Confirm()
        {
            if (_isInputMode && !TryReadFields())
            {
                return;
            }

            Close(TimePickerAction.Confirm);
        }

        private void Close(TimePickerAction action)
        {
            _resultSource?.TrySetResult(new TimePickerResult(action, new TimeSpan(_hour, _minute, 0)));
        }

        private void ToggleMode()
        {
            // Leaving input mode keeps the typed time, so invalid input has to be fixed first.
            if (_isInputMode && !TryReadFields())
            {
                return;
            }

            SetInputMode(!_isInputMode);
        }

        private void SetInputMode(bool isInputMode)
        {
            _isInputMode = isInputMode;
            _headline.text = isInputMode ? $"Enter {_zoneName} time" : $"Select {_zoneName} time";
            _modeIcon.Icon = isInputMode ? IconGraphic.Glyph.Clock : IconGraphic.Glyph.Keyboard;

            _hourBox.gameObject.SetActive(!isInputMode);
            _minuteBox.gameObject.SetActive(!isInputMode);
            _dialRow.SetActive(!isInputMode);
            _hourField.gameObject.SetActive(isInputMode);
            _minuteField.gameObject.SetActive(isInputMode);
            _inputLabels.SetActive(isInputMode);

            if (isInputMode)
            {
                _hourField.SetTextWithoutNotify(_hour.ToString("00"));
                _minuteField.SetTextWithoutNotify(_minute.ToString("00"));
                SetFieldError(hourError: false, minuteError: false);
                _hourField.ActivateInputField();
            }
            else
            {
                SelectDialMode(_dialMode, animate: false);
            }
        }

        private void SelectDialMode(TimeDialMode mode)
        {
            SelectDialMode(mode, animate: true);
        }

        private void SelectDialMode(TimeDialMode mode, bool animate)
        {
            _dialMode = mode;
            _dial.Show(mode, mode == TimeDialMode.Hour ? _hour : _minute, animate);
            RefreshBoxes();
        }

        private void OnDialValueChanged(TimeDialMode mode, int value)
        {
            if (mode == TimeDialMode.Hour)
            {
                _hour = value;
            }
            else
            {
                _minute = value;
            }
            RefreshBoxes();
        }

        private void OnDialSelectionFinished(TimeDialMode mode)
        {
            // As in M3, letting go of the hour moves straight on to the minutes.
            if (mode == TimeDialMode.Hour)
            {
                SelectDialMode(TimeDialMode.Minute);
            }
        }

        private void RefreshBoxes()
        {
            _hourBoxText.text = _hour.ToString("00");
            _minuteBoxText.text = _minute.ToString("00");

            bool isHourSelected = _dialMode == TimeDialMode.Hour;
            _hourBoxImage.color = isHourSelected ? _selectedBoxColor : _boxColor;
            _hourBoxText.color = isHourSelected ? _selectedBoxTextColor : _boxTextColor;
            _minuteBoxImage.color = isHourSelected ? _boxColor : _selectedBoxColor;
            _minuteBoxText.color = isHourSelected ? _boxTextColor : _selectedBoxTextColor;
        }

        private bool TryReadFields()
        {
            bool isHourValid = TryParseField(_hourField, 23, out int hour);
            bool isMinuteValid = TryParseField(_minuteField, 59, out int minute);
            SetFieldError(!isHourValid, !isMinuteValid);

            if (!isHourValid || !isMinuteValid)
            {
                (isHourValid ? _minuteField : _hourField).ActivateInputField();
                return false;
            }

            _hour = hour;
            _minute = minute;
            return true;
        }

        private static bool TryParseField(InputField field, int maxValue, out int value)
        {
            return int.TryParse(field.text, out value) && value >= 0 && value <= maxValue;
        }

        private void SetFieldError(bool hourError, bool minuteError)
        {
            _hasHourError = hourError;
            _hasMinuteError = minuteError;
        }

        private void ApplyFieldColors(InputField field, Image fieldImage, bool hasError)
        {
            fieldImage.color = hasError ? _errorBoxColor : field.isFocused ? _selectedBoxColor : _boxColor;
            field.textComponent.color = hasError ? _errorTextColor : field.isFocused ? _selectedBoxTextColor : _boxTextColor;
        }

        /// <summary>
        /// Fades the dialog while its container springs between scales, the overshoot giving the
        /// expressive entrance. Completes once the fade is done and the spring has settled.
        /// </summary>
        private async UniTask AnimateAsync(float fromScale, float toScale, float fromAlpha, float toAlpha,
            float fadeDuration, CancellationToken cancellationToken)
        {
            Spring scale = new(fromScale);
            scale.SetTarget(toScale);
            float elapsed = 0.0f;

            while (true)
            {
                float fade = Mathf.Clamp01(elapsed / fadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, fade);
                _container.localScale = Vector3.one * scale.Value;

                // Closing only needs the fade; the dialog is gone before the spring would settle.
                bool isClosing = toAlpha < fromAlpha;
                if (fade >= 1.0f && (isClosing || !scale.IsMoving))
                {
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                float deltaTime = Time.unscaledDeltaTime;
                elapsed += deltaTime;
                scale.Update(deltaTime, _springFrequency, _springDampingRatio);
            }
        }
    }
}
