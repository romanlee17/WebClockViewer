using UnityEngine;
using UnityEngine.UI;

namespace WebClockViewer
{
    /// <summary>
    /// A small filled Material icon drawn as a mesh. Details such as keys and clock hands are
    /// punched out in <see cref="_cutoutColor"/>, which should match the surface behind the icon.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class IconGraphic : MaskableGraphic
    {
        internal enum Glyph
        {
            Keyboard,
            Clock,
        }

        [SerializeField] private Glyph _glyph;
        [SerializeField] private Color _cutoutColor = new(0.16f, 0.17f, 0.23f, 1.0f);

        private readonly ShapeMesh _shapes = new();
        private float _lastScaleFactor;

        public Glyph Icon
        {
            get => _glyph;
            set
            {
                if (_glyph == value)
                {
                    return;
                }

                _glyph = value;
                SetVerticesDirty();
            }
        }

        private void Update()
        {
            float scaleFactor = ShapeMesh.GetScaleFactor(this);
            if (!Mathf.Approximately(scaleFactor, _lastScaleFactor))
            {
                _lastScaleFactor = scaleFactor;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            _shapes.Begin(this, vh);

            Rect rect = GetPixelAdjustedRect();
            float size = Mathf.Min(rect.width, rect.height);
            if (size <= 0.0f)
            {
                return;
            }

            Vector2 center = rect.center;
            switch (_glyph)
            {
                case Glyph.Keyboard:
                    AddKeyboard(center, size);
                    break;
                case Glyph.Clock:
                    AddClock(center, size);
                    break;
            }
        }

        private void AddKeyboard(Vector2 center, float size)
        {
            Vector2 bodySize = new(size * 0.92f, size * 0.64f);
            _shapes.AddRoundedRect(new Rect(center - bodySize * 0.5f, bodySize), size * 0.1f, color);

            float keyRadius = size * 0.052f;
            for (int row = 0; row < 2; row++)
            {
                float y = center.y + size * (0.15f - row * 0.15f);
                for (int column = 0; column < 5; column++)
                {
                    float x = center.x + size * (column - 2) * 0.16f;
                    _shapes.AddCircle(new Vector2(x, y), keyRadius, _cutoutColor);
                }
            }

            float spaceY = center.y - size * 0.16f;
            _shapes.AddCapsule(
                new Vector2(center.x - size * 0.2f, spaceY),
                new Vector2(center.x + size * 0.2f, spaceY),
                keyRadius,
                _cutoutColor);
        }

        private void AddClock(Vector2 center, float size)
        {
            _shapes.AddCircle(center, size * 0.46f, color);

            float handRadius = size * 0.05f;
            _shapes.AddCapsule(center, center + new Vector2(0.0f, size * 0.26f), handRadius, _cutoutColor);
            _shapes.AddCapsule(center, center + new Vector2(size * 0.19f, -size * 0.1f), handRadius, _cutoutColor);
        }
    }
}
