using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>시간 샘플과 아틀라스를 받아 UI 메시를 그리는 공통 위젯.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class JourneyGraphic : MaskableGraphic
    {
        [SerializeField] private Texture2D atlas;
        [SerializeField] private JourneyMotionSettings motion;
        private double _presentationTime;
        private Rect _drawingRect;

        public override Texture mainTexture => atlas != null ? atlas : Texture2D.whiteTexture;
        protected JourneyMotionSettings Motion => motion;
        protected double PresentationTime => _presentationTime;
        protected abstract Vector2 SourceSize { get; }

        public void ValidateConfiguration()
        {
            if (atlas == null || motion == null || material == null)
            {
                throw new InvalidOperationException($"{name}: JourneyGraphic의 atlas, motion, material 참조가 필요하다.");
            }
            if (atlas.width != JourneySpriteLayout.AtlasSize || atlas.height != JourneySpriteLayout.AtlasSize)
            {
                throw new InvalidOperationException($"{name}: 여정 아틀라스 크기가 좌표 계약과 다르다.");
            }
            motion.ValidateConfiguration();
        }

        public void SetPresentationTime(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            if (_presentationTime == value)
            {
                return;
            }
            _presentationTime = value;
            SetVerticesDirty();
        }

        protected sealed override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            // 에디터에서 컴포넌트를 만들고 직렬화 참조를 주입하기 전에는 그리지 않는다.
            if (atlas == null || motion == null)
            {
                return;
            }
            _drawingRect = GetPixelAdjustedRect();
            Populate(helper);
        }

        protected abstract void Populate(VertexHelper helper);
        protected virtual Vector2 Deform(Vector2 point, int part) => point;

        protected void AddGrid(VertexHelper helper, RectInt region, float[] xs, float[] ys, int part, float shade = 1f)
        {
            int start = helper.currentVertCount;
            Color tint = color;
            tint.r *= shade;
            tint.g *= shade;
            tint.b *= shade;
            for (int y = 0; y < ys.Length; y++)
            {
                for (int x = 0; x < xs.Length; x++)
                {
                    Vector2 source = new Vector2(xs[x], ys[y]);
                    AddVertex(helper, Deform(source, part), JourneySpriteLayout.GetUv(region, source), tint);
                }
            }
            for (int y = 0; y < ys.Length - 1; y++)
            {
                for (int x = 0; x < xs.Length - 1; x++)
                {
                    int a = start + y * xs.Length + x;
                    helper.AddTriangle(a, a + 1, a + xs.Length + 1);
                    helper.AddTriangle(a, a + xs.Length + 1, a + xs.Length);
                }
            }
        }

        protected void AddVertex(VertexHelper helper, Vector2 point, Vector2 uv, Color tint)
        {
            Vector2 size = SourceSize;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(
                _drawingRect.xMin + point.x / size.x * _drawingRect.width,
                _drawingRect.yMax - point.y / size.y * _drawingRect.height);
            vertex.uv0 = uv;
            vertex.color = tint;
            helper.AddVert(vertex);
        }
    }
}
