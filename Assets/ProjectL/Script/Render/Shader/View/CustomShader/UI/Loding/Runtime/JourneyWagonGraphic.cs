using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>차체의 완충, 운전자의 지연 흔들림과 바퀴를 그린다.</summary>
    public sealed class JourneyWagonGraphic : JourneyGraphic
    {
        private static readonly float[] Xs = { 0, 500, 980, 1040, 1100, 1160, 1220, 1280, 1340, 1400, 1460, 1536 };
        private static readonly float[] Ys = { 0, 115, 180, 240, 300, 360, 420, 480, 540, 600, 660, 740, 915, 1024 };
        private float _phase;
        private float _slowPhase;

        protected override Vector2 SourceSize => new Vector2(1536, 1024);

        protected override void Populate(VertexHelper helper)
        {
            _phase = JourneyMotionMath.Phase(Motion, PresentationTime);
            _slowPhase = JourneyMotionMath.Fraction(PresentationTime / (Motion.StrideDuration * 2.0)) * Mathf.PI * 2f;
            AddGrid(helper, JourneySpriteLayout.Wagon, Xs, Ys, 0);
            AddWheel(helper, new Vector2(311, 740));
            AddWheel(helper, new Vector2(1149, 740));
        }

        protected override Vector2 Deform(Vector2 point, int part)
        {
            float sprung = 1f - JourneyMotionMath.Smooth(540, 735, point.y);
            float driver = JourneyMotionMath.Smooth(975, 1100, point.x) * (1f - JourneyMotionMath.Smooth(535, 650, point.y));
            float hair = (1f - JourneyMotionMath.Smooth(1080, 1160, point.x)) * JourneyMotionMath.Smooth(980, 1020, point.x) *
                JourneyMotionMath.Smooth(330, 420, point.y) * (1f - JourneyMotionMath.Smooth(530, 560, point.y));
            float suspension = Mathf.Sin(_phase * 2f - 1.25f) * Motion.Suspension + Mathf.Sin(_slowPhase - 0.8f) * 1.5f;
            float lean = Mathf.Sin(_phase - 1.1f) * Motion.DriverLean;
            return point + new Vector2(
                -(point.y - 600f) * lean * driver + Mathf.Sin(_phase - 1.6f - point.y * 0.008f) * 5f * hair,
                suspension * sprung + (point.x - 1240f) * lean * driver + Mathf.Sin(_phase - 1.4f) * 2f * driver);
        }

        private void AddWheel(VertexHelper helper, Vector2 center)
        {
            const int segments = 48;
            const float radius = 129f;
            float angle = JourneyMotionMath.Fraction(PresentationTime * Motion.RoadSpeed /
                (174f * JourneySpriteLayout.WagonDisplayScale) / (Math.PI * 2.0)) * Mathf.PI * 2f;
            int start = helper.currentVertCount;
            AddVertex(helper, center, JourneySpriteLayout.GetUv(JourneySpriteLayout.Wagon, center), color);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector2 point = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Vector2 sample = center + new Vector2(Mathf.Cos(a - angle), Mathf.Sin(a - angle)) * radius;
                AddVertex(helper, point, JourneySpriteLayout.GetUv(JourneySpriteLayout.Wagon, sample), color);
                if (i > 0)
                {
                    helper.AddTriangle(start, start + i, start + i + 1);
                }
            }
        }
    }
}
