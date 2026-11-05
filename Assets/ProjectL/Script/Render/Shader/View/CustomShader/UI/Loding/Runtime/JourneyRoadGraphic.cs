using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>걷기의 접지 속도에 맞춰 바닥 흔적·그림자·고삐를 그린다.</summary>
    public sealed class JourneyRoadGraphic : JourneyGraphic
    {
        protected override Vector2 SourceSize => new Vector2(1000, 563);

        protected override void Populate(VertexHelper helper)
        {
            AddEllipse(helper, new Vector2(505, 372), new Vector2(310, 9), new Color(0.29f, 0.19f, 0.11f, 0.11f));
            for (int i = 0; i < 22; i++)
            {
                float x = 840f - JourneyMotionMath.Fraction(i / 22.0 + PresentationTime * Motion.RoadSpeed / 680.0) * 680f;
                float y = 365f + i % 4 * 5f;
                AddLine(helper, new Vector2(x, y), new Vector2(x + 9 + i % 5 * 8, y), 1.5f, new Color(0.95f, 0.82f, 0.61f, 0.4f));
            }
            for (int i = 0; i < 12; i++)
            {
                float p = JourneyMotionMath.Fraction(PresentationTime * 0.65 + i / 12.0);
                AddEllipse(helper, new Vector2(265 - p * 100, 362 - p * 17), new Vector2(5 + p * 20, 3 + p * 7),
                    new Color(0.9f, 0.74f, 0.51f, (1f - p) * 0.13f));
            }
            AddLine(helper, new Vector2(560, 310), new Vector2(674, 280), 4f, new Color(0.41f, 0.29f, 0.19f));
            float phase = JourneyMotionMath.Phase(Motion, PresentationTime);
            float sag = 3f + Mathf.Sin(phase - 1f) * 1.3f;
            AddRein(helper, new Vector2(611, 272), new Vector2(688, 278 + sag), new Vector2(778, 253 + Mathf.Sin(phase + 0.65f)), 1.6f);
            AddRein(helper, new Vector2(611, 278), new Vector2(688, 285 + sag), new Vector2(781, 259 + Mathf.Sin(phase + 0.65f)), 1.2f);
        }

        private void AddLine(VertexHelper helper, Vector2 a, Vector2 b, float width, Color tint)
        {
            int start = helper.currentVertCount;
            Vector2 delta = b - a;
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized * width * 0.5f;
            Vector2 uv = JourneySpriteLayout.SolidUv;
            AddVertex(helper, a - normal, uv, tint * color);
            AddVertex(helper, a + normal, uv, tint * color);
            AddVertex(helper, b + normal, uv, tint * color);
            AddVertex(helper, b - normal, uv, tint * color);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }

        private void AddEllipse(VertexHelper helper, Vector2 center, Vector2 radius, Color tint)
        {
            const int segments = 24;
            int start = helper.currentVertCount;
            AddVertex(helper, center, JourneySpriteLayout.SolidUv, tint * color);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                AddVertex(helper, center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y), JourneySpriteLayout.SolidUv, tint * color);
                if (i > 0)
                {
                    helper.AddTriangle(start, start + i, start + i + 1);
                }
            }
        }

        private void AddRein(VertexHelper helper, Vector2 a, Vector2 control, Vector2 b, float width)
        {
            Vector2 previous = a;
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                Vector2 next = (1f - t) * (1f - t) * a + 2f * (1f - t) * t * control + t * t * b;
                AddLine(helper, previous, next, width, new Color(0.39f, 0.27f, 0.18f));
                previous = next;
            }
        }
    }
}
