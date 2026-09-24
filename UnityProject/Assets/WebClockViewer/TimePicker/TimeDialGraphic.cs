using UnityEngine;
using UnityEngine.UI;

namespace WebClockViewer
{
    /// <summary>
    /// The time picker's clock face: a cookie-shaped dial and a selector (track, handle and center
    /// dot) that springs between values. The number labels are separate text children.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class TimeDialGraphic : MaskableGraphic
    {
        private const float TrackWidth = 0.018f;
        private const float CenterDotRadius = 0.035f;
        private const float OuterHandleRadius = 0.17f;
        private const float InnerHandleRadius = 0.14f;
        private const float BetweenLabelsDotRadius = 0.028f;

        [Header("Colors")]
        [SerializeField] private Color _dialColor = new(0.2f, 0.22f, 0.29f, 1.0f);
        [SerializeField] private Color _selectorColor = new(0.72f, 0.78f, 1.0f, 1.0f);
        [SerializeField] private Color _selectorDotColor = new(0.12f, 0.18f, 0.38f, 1.0f);

        [Header("Face")]
        [SerializeField, Range(3, 24)] private int _dialLobes = 12;
        [SerializeField, Range(0.0f, 0.3f)] private float _dialLobeDepth = 0.06f;

        [Header("Motion")]
        [SerializeField, Min(0.1f)] private float _springFrequency = 3.0f;
        [SerializeField, Range(0.05f, 1.0f)] private float _springDampingRatio = 0.55f;

        private readonly ShapeMesh _shapes = new();
        private Spring _angle = new(0.0f);
        private Spring _ring = new(TimeDial.OuterRing);
        private bool _isBetweenLabels;
        private float _lastScaleFactor;

        /// <summary>The dial radius in local units, matching what is drawn.</summary>
        public float Radius => Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;

        /// <summary>
        /// Moves the selector to <paramref name="angle"/> (clock degrees) on the ring at
        /// <paramref name="ring"/> times the radius. <paramref name="isBetweenLabels"/> marks a value
        /// with no label under the handle, which M3 shows with a dot inside the handle.
        /// </summary>
        public void SetSelection(float angle, float ring, bool isBetweenLabels, bool animate)
        {
            _isBetweenLabels = isBetweenLabels;
            if (animate)
            {
                _angle.SetAngleTarget(angle);
                _ring.SetTarget(ring);
            }
            else
            {
                _angle.Snap(angle);
                _ring.Snap(ring);
            }
            SetVerticesDirty();
        }

        private void Update()
        {
            float scaleFactor = ShapeMesh.GetScaleFactor(this);
            bool isDirty = !Mathf.Approximately(scaleFactor, _lastScaleFactor);
            _lastScaleFactor = scaleFactor;

            if (_angle.IsMoving || _ring.IsMoving)
            {
                float deltaTime = Time.unscaledDeltaTime;
                _angle.Update(deltaTime, _springFrequency, _springDampingRatio);
                _ring.Update(deltaTime, _springFrequency, _springDampingRatio);
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
            _shapes.AddCookie(center, radius, _dialLobes, _dialLobeDepth, _dialColor * color);

            Color selectorColor = _selectorColor * color;
            float ringProgress = Mathf.InverseLerp(TimeDial.InnerRing, TimeDial.OuterRing, _ring.Value);
            float handleRadius = radius * Mathf.LerpUnclamped(InnerHandleRadius, OuterHandleRadius, ringProgress);
            Vector2 handleCenter = ShapeMesh.Polar(center, radius * _ring.Value, _angle.Value);

            _shapes.AddCapsule(center, handleCenter, radius * TrackWidth * 0.5f, selectorColor);
            _shapes.AddCircle(handleCenter, handleRadius, selectorColor);
            _shapes.AddCircle(center, radius * CenterDotRadius, selectorColor);

            if (_isBetweenLabels)
            {
                _shapes.AddCircle(handleCenter, radius * BetweenLabelsDotRadius, _selectorDotColor * color);
            }
        }
    }
}
