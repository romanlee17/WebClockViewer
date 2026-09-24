using System;
using UnityEngine;
using UnityEngine.UI;

namespace WebClockViewer
{
    /// <summary>
    /// Material 3 Expressive analog clock: a scalloped "cookie" face, rounded pill hands and a
    /// seconds dot that springs from tick to tick. The whole clock is generated as one mesh whose
    /// edges fade out over a single screen pixel, so it stays crisp at any size without textures.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class AnalogClock : MaskableGraphic
    {
        // Proportions relative to the face radius.
        private const float MarkerOrbit = 0.83f;
        private const float MarkerRadius = 0.024f;
        private const float QuarterMarkerRadius = 0.036f;
        private const float HourHandLength = 0.5f;
        private const float HourHandWidth = 0.2f;
        private const float MinuteHandLength = 0.72f;
        private const float MinuteHandWidth = 0.13f;
        private const float SecondDotRadius = 0.07f;
        private const float CenterPinRadius = 0.04f;

        [Header("Colors")]
        [SerializeField] private Color _faceColor = new(0.20f, 0.23f, 0.36f, 1.0f);
        [SerializeField] private Color _markerColor = new(0.78f, 0.81f, 0.95f, 0.5f);
        [SerializeField] private Color _hourHandColor = new(0.56f, 0.64f, 0.96f, 1.0f);
        [SerializeField] private Color _minuteHandColor = new(0.87f, 0.89f, 1.0f, 1.0f);
        [SerializeField] private Color _secondDotColor = new(1.0f, 0.71f, 0.55f, 1.0f);

        [Header("Face")]
        [SerializeField, Range(3, 24)] private int _faceLobes = 12;
        [SerializeField, Range(0.0f, 0.3f)] private float _faceLobeDepth = 0.1f;

        [Header("Motion")]
        [SerializeField, Min(0.1f)] private float _springFrequency = 2.5f;
        [SerializeField, Range(0.05f, 1.0f)] private float _springDampingRatio = 0.4f;

        // The classic 10:10:30 pose until the first time arrives, so the prefab previews nicely.
        private Spring _hourAngle = new(305.0f);
        private Spring _minuteAngle = new(60.0f);
        private Spring _secondAngle = new(180.0f);

        private readonly ShapeMesh _shapes = new();
        private bool _hasTime;
        private float _lastScaleFactor;

        /// <summary>
        /// Points the hands at <paramref name="time"/>. The first call snaps them; later calls let
        /// them spring over, taking the short way round.
        /// </summary>
        public void SetTime(DateTime time)
        {
            float seconds = time.Second;
            float minutes = time.Minute + seconds / 60.0f;
            float hours = time.Hour % 12 + minutes / 60.0f;

            if (!_hasTime)
            {
                _hasTime = true;
                _hourAngle.Snap(hours * 30.0f);
                _minuteAngle.Snap(minutes * 6.0f);
                _secondAngle.Snap(seconds * 6.0f);
                SetVerticesDirty();
                return;
            }

            _hourAngle.SetAngleTarget(hours * 30.0f);
            _minuteAngle.SetAngleTarget(minutes * 6.0f);
            _secondAngle.SetAngleTarget(seconds * 6.0f);
        }

        private void Update()
        {
            // The feather is one screen pixel, so a new canvas scale needs a new mesh.
            float scaleFactor = ShapeMesh.GetScaleFactor(this);
            bool isDirty = !Mathf.Approximately(scaleFactor, _lastScaleFactor);
            _lastScaleFactor = scaleFactor;

            if (_hourAngle.IsMoving || _minuteAngle.IsMoving || _secondAngle.IsMoving)
            {
                float deltaTime = Time.unscaledDeltaTime;
                _hourAngle.Update(deltaTime, _springFrequency, _springDampingRatio);
                _minuteAngle.Update(deltaTime, _springFrequency, _springDampingRatio);
                _secondAngle.Update(deltaTime, _springFrequency, _springDampingRatio);
                isDirty = true;
            }

            if (isDirty)
            {
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            _shapes.Begin(this, vh);

            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - _shapes.Feather * 0.5f;
            if (radius <= 0.0f)
            {
                return;
            }

            Vector2 center = rect.center;
            _shapes.AddCookie(center, radius, _faceLobes, _faceLobeDepth, _faceColor * color);

            // With the default 12 lobes each hour marker sits in front of a lobe.
            for (int hour = 0; hour < 12; hour++)
            {
                float markerRadius = hour % 3 == 0 ? QuarterMarkerRadius : MarkerRadius;
                Vector2 markerCenter = ShapeMesh.Polar(center, radius * MarkerOrbit, hour * 30.0f);
                _shapes.AddCircle(markerCenter, radius * markerRadius, _markerColor * color);
            }

            AddHand(center, radius * HourHandLength, radius * HourHandWidth, _hourAngle.Value, _hourHandColor);
            AddHand(center, radius * MinuteHandLength, radius * MinuteHandWidth, _minuteAngle.Value, _minuteHandColor);

            Vector2 secondCenter = ShapeMesh.Polar(center, radius * MarkerOrbit, _secondAngle.Value);
            _shapes.AddCircle(secondCenter, radius * SecondDotRadius, _secondDotColor * color);
            _shapes.AddCircle(center, radius * CenterPinRadius, _faceColor * color);
        }

        private void AddHand(Vector2 center, float length, float width, float angle, Color handColor)
        {
            float capRadius = width * 0.5f;
            Vector2 tip = ShapeMesh.Polar(center, length - capRadius, angle);
            _shapes.AddCapsule(center, tip, capRadius, handColor * color);
        }
    }
}
