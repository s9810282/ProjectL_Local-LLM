using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>접지 구간을 보존한 연속 메시 변형으로 말의 네 다리와 고개를 그린다.</summary>
    public sealed class JourneyHorseGraphic : JourneyGraphic
    {
        private static readonly float[] LegXs = { 0, 543 };
        private static readonly float[] HindYs = { 392, 417, 433, 449, 465, 481, 493, 509, 525, 541, 557, 573, 593 };
        private static readonly float[] ForeYs = { 438, 463, 479, 495, 511, 521, 537, 553, 569, 575, 591, 614 };
        private static readonly float[] BodyXs = { 0, 130, 230, 300, 365, 430, 490, 543 };
        private static readonly float[] BodyYs = { 0, 107, 180, 250, 320, 385, 425, 455, 480, 500 };
        private float _phase;
        private float _bodyBob;
        private JourneyMotionMath.HoofPose _hoof;

        protected override Vector2 SourceSize => new Vector2(543, 724);

        protected override void Populate(VertexHelper helper)
        {
            _phase = JourneyMotionMath.Phase(Motion, PresentationTime);
            _bodyBob = -2.1f * Mathf.Cos(_phase * 2f) + 0.5f * Mathf.Sin(_phase);
            AddLeg(helper, 0, 0.5f, true);
            AddLeg(helper, 1, 0f, true);
            AddLeg(helper, 2, 0f, false);
            AddLeg(helper, 3, 0.5f, false);
            AddGrid(helper, JourneySpriteLayout.HorseBody, BodyXs, BodyYs, 4);
        }

        protected override Vector2 Deform(Vector2 point, int part)
        {
            if (part == 4)
            {
                float head = JourneyMotionMath.Smooth(250, 390, point.x) * (1f - JourneyMotionMath.Smooth(350, 450, point.y));
                float nod = Mathf.Sin(_phase + 0.65f) * 0.012f;
                return point + new Vector2(-(point.y - 350f) * nod * head, _bodyBob + (point.x - 325f) * nod * head);
            }
            bool hind = part == 0 || part == 2;
            bool far = part < 2;
            float hy = hind ? 417f : 463f;
            float ky = hind ? 493f : 521f;
            float fx = hind ? 149f : 268f;
            float fy = hind ? 557f : 575f;
            float offset = far ? (hind ? -22f : 68f) : 0f;
            float correction = 607f - (hind ? 590f : 611f) - (far ? 8f : 0f);
            Vector2 upper = new Vector2(offset, _bodyBob);
            Vector2 knee = new Vector2(offset + _hoof.Offset * 0.46f + _hoof.Lift * 0.32f,
                correction * 0.5f - _hoof.Lift * 0.28f + _bodyBob * 0.4f);
            Vector2 foot = new Vector2(offset + _hoof.Offset, correction - _hoof.Lift);
            if (point.y >= fy)
            {
                Vector2 local = point - new Vector2(fx, fy);
                float c = Mathf.Cos(_hoof.Roll);
                float s = Mathf.Sin(_hoof.Roll);
                return new Vector2(fx, fy) + foot + new Vector2(local.x * c - local.y * s, local.x * s + local.y * c);
            }
            if (point.y <= hy)
            {
                return point + upper;
            }
            return point + (point.y < ky ? Vector2.Lerp(upper, knee, (point.y - hy) / (ky - hy)) :
                Vector2.Lerp(knee, foot, (point.y - ky) / (fy - ky)));
        }

        private void AddLeg(VertexHelper helper, int part, float phase, bool far)
        {
            _hoof = JourneyMotionMath.SampleHoof(Motion, PresentationTime, phase);
            bool hind = part == 0 || part == 2;
            AddGrid(helper, hind ? JourneySpriteLayout.HorseHind : JourneySpriteLayout.HorseFore,
                LegXs, hind ? HindYs : ForeYs, part, far ? 0.79f : 1f);
        }
    }
}
