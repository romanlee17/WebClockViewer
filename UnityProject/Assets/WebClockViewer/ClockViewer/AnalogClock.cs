using System;
using System.Collections.Generic;
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
        private const float FeatherPixels = 1.25f;
        private const float MaxFrameStep = 0.1f;
        private const float SpringSubstep = 1.0f / 240.0f;

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
        private SpringAngle _hourAngle = new(305.0f);
        private SpringAngle _minuteAngle = new(60.0f);
        private SpringAngle _secondAngle = new(180.0f);

        private readonly List<Vector2> _outline = new();
        private bool _hasTime;
        private bool _isAnimating;
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

            _hourAngle.Retarget(hours * 30.0f);
            _minuteAngle.Retarget(minutes * 6.0f);
            _secondAngle.Retarget(seconds * 6.0f);
            _isAnimating = true;
        }

        private void Update()
        {
            // The feather is one screen pixel, so a new canvas scale needs a new mesh.
            float scaleFactor = GetScaleFactor();
            bool isDirty = !Mathf.Approximately(scaleFactor, _lastScaleFactor);
            _lastScaleFactor = scaleFactor;

            if (_isAnimating)
            {
                StepSprings(Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep));
                isDirty = true;
            }

            if (isDirty)
            {
                SetVerticesDirty();
            }
        }

        private void StepSprings(float deltaTime)
        {
            float angularFrequency = 2.0f * Mathf.PI * _springFrequency;
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / SpringSubstep));
            float step = deltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                _hourAngle.Step(step, angularFrequency, _springDampingRatio);
                _minuteAngle.Step(step, angularFrequency, _springDampingRatio);
                _secondAngle.Step(step, angularFrequency, _springDampingRatio);
            }

            bool hourSettled = _hourAngle.TrySettle();
            bool minuteSettled = _minuteAngle.TrySettle();
            bool secondSettled = _secondAngle.TrySettle();
            _isAnimating = !(hourSettled && minuteSettled && secondSettled);
        }

        private float GetScaleFactor()
        {
            Canvas rootCanvas = canvas;
            return rootCanvas != null && rootCanvas.scaleFactor > 0.0f ? rootCanvas.scaleFactor : 1.0f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            float feather = FeatherPixels / GetScaleFactor();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - feather * 0.5f;
            if (radius <= 0.0f)
            {
                return;
            }

            Vector2 center = rect.center;

            BuildCookie(center, radius);
            AddShape(vh, center, _faceColor, feather);

            for (int hour = 0; hour < 12; hour++)
            {
                float markerRadius = hour % 3 == 0 ? QuarterMarkerRadius : MarkerRadius;
                Vector2 markerCenter = Polar(center, radius * MarkerOrbit, hour * 30.0f);
                BuildCircle(markerCenter, radius * markerRadius, feather);
                AddShape(vh, markerCenter, _markerColor, feather);
            }

            AddHand(vh, center, radius * HourHandLength, radius * HourHandWidth, _hourAngle.Value, _hourHandColor, feather);
            AddHand(vh, center, radius * MinuteHandLength, radius * MinuteHandWidth, _minuteAngle.Value, _minuteHandColor, feather);

            Vector2 secondCenter = Polar(center, radius * MarkerOrbit, _secondAngle.Value);
            BuildCircle(secondCenter, radius * SecondDotRadius, feather);
            AddShape(vh, secondCenter, _secondDotColor, feather);

            BuildCircle(center, radius * CenterPinRadius, feather);
            AddShape(vh, center, _faceColor, feather);
        }

        private void AddHand(VertexHelper vh, Vector2 center, float length, float width, float angle, Color handColor, float feather)
        {
            float capRadius = width * 0.5f;
            Vector2 tip = Polar(center, length - capRadius, angle);
            BuildCapsule(center, tip, capRadius, feather);
            AddShape(vh, (center + tip) * 0.5f, handColor, feather);
        }

        /// <summary>
        /// A circle whose rim swells into rounded lobes with narrow valleys between them. The first
        /// lobe points at twelve, so with the default 12 lobes each one sits behind an hour marker.
        /// </summary>
        private void BuildCookie(Vector2 center, float radius)
        {
            _outline.Clear();
            int segments = _faceLobes * 32;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360.0f / segments;
                float valley = (1.0f - Mathf.Cos(_faceLobes * angle * Mathf.Deg2Rad)) * 0.5f;
                _outline.Add(Polar(center, radius * (1.0f - _faceLobeDepth * valley * valley), angle));
            }
        }

        private void BuildCircle(Vector2 center, float radius, float feather)
        {
            _outline.Clear();
            int segments = GetArcSegments(radius, feather, 360.0f);
            for (int i = 0; i < segments; i++)
            {
                _outline.Add(Polar(center, radius, i * 360.0f / segments));
            }
        }

        private void BuildCapsule(Vector2 start, Vector2 end, float radius, float feather)
        {
            _outline.Clear();
            Vector2 direction = end - start;
            float directionAngle = direction.sqrMagnitude > 0.0f
                ? Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg
                : 0.0f;

            int segments = GetArcSegments(radius, feather, 180.0f);
            for (int i = 0; i <= segments; i++)
            {
                _outline.Add(Polar(end, radius, directionAngle - 90.0f + i * 180.0f / segments));
            }
            for (int i = 0; i <= segments; i++)
            {
                _outline.Add(Polar(start, radius, directionAngle + 90.0f + i * 180.0f / segments));
            }
        }

        /// <summary>Enough segments that each one spans about two screen pixels of arc.</summary>
        private static int GetArcSegments(float radius, float feather, float arcDegrees)
        {
            float arcPixels = radius / feather * FeatherPixels * arcDegrees * Mathf.Deg2Rad;
            return Mathf.Clamp(Mathf.CeilToInt(arcPixels * 0.5f), 6, 64);
        }

        /// <summary>
        /// Fills <see cref="_outline"/> as a fan around <paramref name="fanCenter"/>, which must see
        /// the whole outline, and wraps it in a ring that fades to transparent across
        /// <paramref name="feather"/> for antialiasing.
        /// </summary>
        private void AddShape(VertexHelper vh, Vector2 fanCenter, Color shapeColor, float feather)
        {
            Color32 solid = shapeColor * color;
            Color32 transparent = solid;
            transparent.a = 0;

            int count = _outline.Count;
            float halfFeather = feather * 0.5f;
            float outwardSign = GetSignedArea() >= 0.0f ? 1.0f : -1.0f;

            int centerIndex = vh.currentVertCount;
            vh.AddVert(fanCenter, solid, Vector4.zero);

            for (int i = 0; i < count; i++)
            {
                Vector2 previous = _outline[(i + count - 1) % count];
                Vector2 next = _outline[(i + 1) % count];
                Vector2 tangent = next - previous;
                Vector2 normal = new Vector2(tangent.y, -tangent.x).normalized * outwardSign;

                vh.AddVert(_outline[i] - normal * halfFeather, solid, Vector4.zero);
                vh.AddVert(_outline[i] + normal * halfFeather, transparent, Vector4.zero);
            }

            for (int i = 0; i < count; i++)
            {
                int inner = centerIndex + 1 + i * 2;
                int nextInner = centerIndex + 1 + (i + 1) % count * 2;

                vh.AddTriangle(centerIndex, inner, nextInner);
                vh.AddTriangle(inner, inner + 1, nextInner + 1);
                vh.AddTriangle(inner, nextInner + 1, nextInner);
            }
        }

        /// <summary>Positive when <see cref="_outline"/> winds counterclockwise.</summary>
        private float GetSignedArea()
        {
            float area = 0.0f;
            int count = _outline.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 current = _outline[i];
                Vector2 next = _outline[(i + 1) % count];
                area += current.x * next.y - next.x * current.y;
            }
            return area;
        }

        /// <summary>A point at <paramref name="distance"/> from <paramref name="center"/>, with the
        /// angle in clock degrees: zero at twelve o'clock, growing clockwise.</summary>
        private static Vector2 Polar(Vector2 center, float distance, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * distance;
        }

        /// <summary>A damped spring on an unwrapped angle, so hands never spin the long way round.</summary>
        private struct SpringAngle
        {
            private const float SettleAngle = 0.01f;
            private const float SettleVelocity = 0.1f;

            public float Value { get; private set; }
            private float _velocity;
            private float _target;

            public SpringAngle(float angle)
            {
                Value = angle;
                _velocity = 0.0f;
                _target = angle;
            }

            public void Snap(float angle)
            {
                Value = angle;
                _velocity = 0.0f;
                _target = angle;
            }

            public void Retarget(float angle)
            {
                _target += Mathf.DeltaAngle(_target, angle);
            }

            public void Step(float deltaTime, float angularFrequency, float dampingRatio)
            {
                float acceleration = -angularFrequency * angularFrequency * (Value - _target)
                    - 2.0f * dampingRatio * angularFrequency * _velocity;
                _velocity += acceleration * deltaTime;
                Value += _velocity * deltaTime;
            }

            /// <summary>Lands the spring on its target once it has all but stopped.</summary>
            public bool TrySettle()
            {
                if (Mathf.Abs(Value - _target) > SettleAngle || Mathf.Abs(_velocity) > SettleVelocity)
                {
                    return false;
                }

                Value = _target;
                _velocity = 0.0f;
                return true;
            }
        }
    }
}
