using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WebClockViewer
{
    /// <summary>
    /// Builds filled shapes into a graphic's mesh. Each shape is a fan wrapped in a ring that fades
    /// to transparent over about one screen pixel, so custom graphics stay crisp at any canvas scale
    /// without textures. Colors are used as given; callers apply the graphic's own tint.
    /// </summary>
    internal sealed class ShapeMesh
    {
        private const float FeatherPixels = 1.25f;

        private readonly List<Vector2> _outline = new();
        private VertexHelper _vertexHelper;

        /// <summary>The edge fade width in local units: one screen pixel and a bit.</summary>
        public float Feather { get; private set; } = FeatherPixels;

        /// <summary>
        /// The canvas scale the feather depends on. Graphics compare it each frame and rebuild
        /// their mesh when it changes.
        /// </summary>
        public static float GetScaleFactor(Graphic graphic)
        {
            Canvas canvas = graphic.canvas;
            return canvas != null && canvas.scaleFactor > 0.0f ? canvas.scaleFactor : 1.0f;
        }

        /// <summary>Clears <paramref name="vh"/> and starts building <paramref name="graphic"/>'s mesh into it.</summary>
        public void Begin(Graphic graphic, VertexHelper vh)
        {
            vh.Clear();
            _vertexHelper = vh;
            Feather = FeatherPixels / GetScaleFactor(graphic);
        }

        public void AddCircle(Vector2 center, float radius, Color color)
        {
            _outline.Clear();
            int segments = GetArcSegments(radius, 360.0f);
            for (int i = 0; i < segments; i++)
            {
                _outline.Add(Polar(center, radius, i * 360.0f / segments));
            }
            AddOutline(center, color);
        }

        /// <summary>A pill running from <paramref name="start"/> to <paramref name="end"/> with round caps.</summary>
        public void AddCapsule(Vector2 start, Vector2 end, float radius, Color color)
        {
            _outline.Clear();
            Vector2 direction = end - start;
            float directionAngle = direction.sqrMagnitude > 0.0f
                ? Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg
                : 0.0f;

            int segments = GetArcSegments(radius, 180.0f);
            for (int i = 0; i <= segments; i++)
            {
                _outline.Add(Polar(end, radius, directionAngle - 90.0f + i * 180.0f / segments));
            }
            for (int i = 0; i <= segments; i++)
            {
                _outline.Add(Polar(start, radius, directionAngle + 90.0f + i * 180.0f / segments));
            }
            AddOutline((start + end) * 0.5f, color);
        }

        /// <summary>
        /// A circle whose rim swells into rounded lobes with narrow valleys between them; the first
        /// lobe points at twelve o'clock.
        /// </summary>
        public void AddCookie(Vector2 center, float radius, int lobes, float depth, Color color)
        {
            _outline.Clear();
            int segments = Mathf.Max(lobes, 1) * 32;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360.0f / segments;
                float valley = (1.0f - Mathf.Cos(lobes * angle * Mathf.Deg2Rad)) * 0.5f;
                _outline.Add(Polar(center, radius * (1.0f - depth * valley * valley), angle));
            }
            AddOutline(center, color);
        }

        public void AddRoundedRect(Rect rect, float cornerRadius, Color color)
        {
            _outline.Clear();
            cornerRadius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            int segments = GetArcSegments(cornerRadius, 90.0f);
            // Clockwise from the top-left corner; each corner sweeps the quarter it faces.
            AddCorner(new Vector2(rect.xMin + cornerRadius, rect.yMax - cornerRadius), cornerRadius, 270.0f, segments);
            AddCorner(new Vector2(rect.xMax - cornerRadius, rect.yMax - cornerRadius), cornerRadius, 0.0f, segments);
            AddCorner(new Vector2(rect.xMax - cornerRadius, rect.yMin + cornerRadius), cornerRadius, 90.0f, segments);
            AddCorner(new Vector2(rect.xMin + cornerRadius, rect.yMin + cornerRadius), cornerRadius, 180.0f, segments);
            AddOutline(rect.center, color);
        }

        /// <summary>
        /// A point at <paramref name="distance"/> from <paramref name="center"/>, with the angle in
        /// clock degrees: zero at twelve o'clock, growing clockwise.
        /// </summary>
        public static Vector2 Polar(Vector2 center, float distance, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * distance;
        }

        /// <summary>A quarter arc sweeping 90° clockwise from <paramref name="startAngle"/>.</summary>
        private void AddCorner(Vector2 center, float radius, float startAngle, int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                _outline.Add(Polar(center, radius, startAngle + i * 90.0f / segments));
            }
        }

        /// <summary>Enough segments that each one spans about two screen pixels of arc.</summary>
        private int GetArcSegments(float radius, float arcDegrees)
        {
            float arcPixels = radius / Feather * FeatherPixels * arcDegrees * Mathf.Deg2Rad;
            return Mathf.Clamp(Mathf.CeilToInt(arcPixels * 0.5f), 4, 64);
        }

        /// <summary>
        /// Fills <see cref="_outline"/> as a fan around <paramref name="fanCenter"/>, which must see
        /// the whole outline, and wraps it in the fading ring.
        /// </summary>
        private void AddOutline(Vector2 fanCenter, Color color)
        {
            Color32 solid = color;
            Color32 transparent = solid;
            transparent.a = 0;

            int count = _outline.Count;
            float halfFeather = Feather * 0.5f;
            float outwardSign = GetSignedArea() >= 0.0f ? 1.0f : -1.0f;

            int centerIndex = _vertexHelper.currentVertCount;
            _vertexHelper.AddVert(fanCenter, solid, Vector4.zero);

            for (int i = 0; i < count; i++)
            {
                Vector2 previous = _outline[(i + count - 1) % count];
                Vector2 next = _outline[(i + 1) % count];
                Vector2 tangent = next - previous;
                Vector2 normal = new Vector2(tangent.y, -tangent.x).normalized * outwardSign;

                _vertexHelper.AddVert(_outline[i] - normal * halfFeather, solid, Vector4.zero);
                _vertexHelper.AddVert(_outline[i] + normal * halfFeather, transparent, Vector4.zero);
            }

            for (int i = 0; i < count; i++)
            {
                int inner = centerIndex + 1 + i * 2;
                int nextInner = centerIndex + 1 + (i + 1) % count * 2;

                _vertexHelper.AddTriangle(centerIndex, inner, nextInner);
                _vertexHelper.AddTriangle(inner, inner + 1, nextInner + 1);
                _vertexHelper.AddTriangle(inner, nextInner + 1, nextInner);
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
    }
}
