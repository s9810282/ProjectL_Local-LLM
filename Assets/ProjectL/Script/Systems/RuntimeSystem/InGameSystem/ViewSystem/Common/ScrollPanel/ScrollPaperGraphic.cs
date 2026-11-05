using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel
{
    /// <summary>탑뷰 종이와 말린 양 끝을 Canvas 메시로 그립니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ScrollPaperGraphic : MaskableGraphic
    {
        internal const float BodyWidthRatio = 0.84f;
        [SerializeField] private Texture2D paperTexture;
        [SerializeField, Range(0f, 1f)] private float reveal = 1f;

        public override Texture mainTexture => paperTexture != null ? paperTexture : Texture2D.whiteTexture;
        public float BodyWidth => rectTransform.rect.width * BodyWidthRatio;

        public void SetReveal(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(reveal, value)) return;
            reveal = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            float half = BodyWidth * Mathf.Max(0.002f, reveal) * 0.5f;
            float radius = rect.width * Mathf.Lerp(0.071f, 0.065f, reveal);
            Vector2 center = rect.center;
            float bodyHalfHeight = rect.height * 0.42f;
            Color32 tint = color;
            const int strips = 48;
            for (int i = 0; i < strips; i++)
            {
                float a = i / (float)strips;
                float b = (i + 1) / (float)strips;
                float x0 = Mathf.Lerp(-half, half, a);
                float x1 = Mathf.Lerp(-half, half, b);
                // 아틀라스 왼쪽 75%는 펼친 면입니다. 펼칠 때 무늬를 늘리지 않습니다.
                float u0 = (0.5f + x0 / BodyWidth) * 0.75f;
                float u1 = (0.5f + x1 / BodyWidth) * 0.75f;
                AddQuad(vh, center.x + x0, center.x + x1,
                    center.y - bodyHalfHeight, center.y + bodyHalfHeight,
                    u0, u1, tint);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                // 오른쪽 25%의 접힌 끝 그림을 좌우 대칭으로 사용합니다.
                float rollCenter = center.x + side * (half + rect.width * 0.016f * (1f - reveal));
                float rollHalfHeight = rect.height * 0.49f;
                float startU = side < 0 ? 1f : 0.75f;
                float endU = side < 0 ? 0.75f : 1f;
                AddQuad(vh, rollCenter - radius, rollCenter + radius,
                    center.y - rollHalfHeight, center.y + rollHalfHeight,
                    startU, endU, tint);
            }
        }

        private static void AddQuad(VertexHelper vh, float x0, float x1, float y0, float y1,
            float u0, float u1, Color32 tint)
        {
            int index = vh.currentVertCount;
            AddVertex(vh, x0, y0, u0, 0f, tint);
            AddVertex(vh, x0, y1, u0, 1f, tint);
            AddVertex(vh, x1, y1, u1, 1f, tint);
            AddVertex(vh, x1, y0, u1, 0f, tint);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddVertex(VertexHelper vh, float x, float y, float u, float v, Color32 tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y, 0f);
            vertex.color = tint;
            vertex.uv0 = new Vector4(u, v, 0f, 0f);
            vh.AddVert(vertex);
        }
    }
}
