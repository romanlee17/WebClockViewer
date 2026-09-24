using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WebClockViewer
{
    internal enum TimeDialMode
    {
        Hour,
        Minute,
    }

    /// <summary>
    /// The draggable 24-hour dial of the M3 time picker. Hours 00–11 sit on the outer ring and
    /// 12–23 on the inner ring; minutes use the outer ring with a label every five minutes.
    /// Pressing or dragging anywhere on the dial picks the nearest value.
    /// </summary>
    [RequireComponent(typeof(TimeDialGraphic))]
    internal sealed class TimeDial : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>Label ring radii, as fractions of the dial radius.</summary>
        public const float OuterRing = 0.8f;
        public const float InnerRing = 0.53f;

        private const int LabelsPerRing = 12;

        [SerializeField] private TimeDialGraphic _graphic;

        [Header("Labels")]
        [SerializeField] private Font _font;
        [SerializeField] private int _outerFontSize = 32;
        [SerializeField] private int _innerFontSize = 26;
        [SerializeField] private Color _outerLabelColor = new(0.89f, 0.9f, 0.95f, 1.0f);
        [SerializeField] private Color _innerLabelColor = new(0.77f, 0.78f, 0.86f, 1.0f);
        [SerializeField] private Color _selectedLabelColor = new(0.12f, 0.18f, 0.38f, 1.0f);

        private readonly Text[] _outerLabels = new Text[LabelsPerRing];
        private readonly Text[] _innerLabels = new Text[LabelsPerRing];

        /// <summary>Raised while the user presses or drags the dial onto a different value.</summary>
        public event Action<TimeDialMode, int> ValueChanged;

        /// <summary>Raised when the user lets go of the dial.</summary>
        public event Action<TimeDialMode> SelectionFinished;

        public TimeDialMode Mode { get; private set; }
        public int Value { get; private set; }

        private void Awake()
        {
            for (int i = 0; i < LabelsPerRing; i++)
            {
                _outerLabels[i] = CreateLabel($"Outer label {i}", _outerFontSize);
                _innerLabels[i] = CreateLabel($"Inner label {i}", _innerFontSize);
            }
            LayoutLabels();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_outerLabels[0] != null)
            {
                LayoutLabels();
            }
        }

        /// <summary>Shows <paramref name="mode"/>'s labels and puts the selector on <paramref name="value"/>.</summary>
        public void Show(TimeDialMode mode, int value, bool animate)
        {
            Mode = mode;
            Value = value;

            for (int i = 0; i < LabelsPerRing; i++)
            {
                _outerLabels[i].text = mode == TimeDialMode.Hour ? i.ToString("00") : (i * 5).ToString("00");
                _innerLabels[i].text = (i + LabelsPerRing).ToString();
                _innerLabels[i].gameObject.SetActive(mode == TimeDialMode.Hour);
            }

            UpdateSelection(animate);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PickValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            PickValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SelectionFinished?.Invoke(Mode);
        }

        private void PickValue(PointerEventData eventData)
        {
            RectTransform dialTransform = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dialTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 fromCenter = localPoint - dialTransform.rect.center;
            float angle = Mathf.Repeat(Mathf.Atan2(fromCenter.x, fromCenter.y) * Mathf.Rad2Deg, 360.0f);

            int value;
            if (Mode == TimeDialMode.Hour)
            {
                float innerBoundary = (OuterRing + InnerRing) * 0.5f * _graphic.Radius;
                int position = Mathf.RoundToInt(angle / 30.0f) % LabelsPerRing;
                value = fromCenter.magnitude < innerBoundary ? position + LabelsPerRing : position;
            }
            else
            {
                value = Mathf.RoundToInt(angle / 6.0f) % 60;
            }

            if (value == Value)
            {
                return;
            }

            Value = value;
            UpdateSelection(animate: true);
            ValueChanged?.Invoke(Mode, value);
        }

        private void UpdateSelection(bool animate)
        {
            bool isInner = Mode == TimeDialMode.Hour && Value >= LabelsPerRing;
            float angle = Mode == TimeDialMode.Hour ? Value % LabelsPerRing * 30.0f : Value * 6.0f;
            bool isBetweenLabels = Mode == TimeDialMode.Minute && Value % 5 != 0;
            _graphic.SetSelection(angle, isInner ? InnerRing : OuterRing, isBetweenLabels, animate);

            int selectedPosition = Mode == TimeDialMode.Hour
                ? Value % LabelsPerRing
                : isBetweenLabels ? -1 : Value / 5;
            for (int i = 0; i < LabelsPerRing; i++)
            {
                bool isSelected = i == selectedPosition;
                _outerLabels[i].color = isSelected && !isInner ? _selectedLabelColor : _outerLabelColor;
                _innerLabels[i].color = isSelected && isInner ? _selectedLabelColor : _innerLabelColor;
            }
        }

        private void LayoutLabels()
        {
            // Labels are anchored to the dial's center, so ring positions are relative to zero.
            float radius = _graphic.Radius;
            for (int i = 0; i < LabelsPerRing; i++)
            {
                _outerLabels[i].rectTransform.anchoredPosition = ShapeMesh.Polar(Vector2.zero, radius * OuterRing, i * 30.0f);
                _innerLabels[i].rectTransform.anchoredPosition = ShapeMesh.Polar(Vector2.zero, radius * InnerRing, i * 30.0f);
            }
        }

        private Text CreateLabel(string labelName, int fontSize)
        {
            GameObject labelObject = new(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.layer = gameObject.layer;

            RectTransform labelTransform = (RectTransform)labelObject.transform;
            labelTransform.SetParent(transform, worldPositionStays: false);
            labelTransform.anchorMin = labelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelTransform.sizeDelta = new Vector2(fontSize * 2.5f, fontSize * 1.5f);

            Text label = labelObject.GetComponent<Text>();
            label.font = _font;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.alignByGeometry = true;
            label.raycastTarget = false;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}
