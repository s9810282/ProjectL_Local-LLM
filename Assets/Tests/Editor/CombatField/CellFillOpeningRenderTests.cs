using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.CombatField.Editor.Tests
{
    public sealed class CellFillOpeningRenderTests
    {
        private const int Size = 256;
        private Material _material;
        private Texture2D _region;
        private Texture2D _hole;
        private Texture2D _heights;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_region);
            Object.DestroyImmediate(_hole);
            Object.DestroyImmediate(_heights);
        }

        [TestCase(GridOutlineSide.Left, false)]
        [TestCase(GridOutlineSide.Right, false)]
        [TestCase(GridOutlineSide.Down, false)]
        [TestCase(GridOutlineSide.Up, false)]
        [TestCase(GridOutlineSide.Left, true)]
        [TestCase(GridOutlineSide.Right, true)]
        [TestCase(GridOutlineSide.Down, true)]
        [TestCase(GridOutlineSide.Up, true)]
        public void Shader_OpensChosenBand_AndKeepsOtherThree(GridOutlineSide side, bool eachCell)
        {
            Create(1, 1);
            _material.SetFloat("_JoinCells", eachCell ? 0f : 1f);
            _material.SetVector("_Inset", new Vector4(0.05f, 0.05f, 0f, 0f));
            _material.SetFloat("_CornerRadius", 0.1f);
            var probes = new[] { new Vector2(-0.41f, 0f), new Vector2(0.41f, 0f), new Vector2(0f, -0.41f), new Vector2(0f, 0.41f) };
            Texture2D closed = Render(1, 1);
            int line = side == GridOutlineSide.Right || side == GridOutlineSide.Up ? 1 : 0;
            FieldCellFillStack.SetOpening(_material, new GridOutlineOpening(side, line));
            Texture2D opened = Render(1, 1);
            try
            {
                for (int i = 0; i < probes.Length; i++)
                {
                    Assert.That(Read(closed, probes[i], 1, 1), Is.GreaterThan(0.7f), "닫힌 채움에는 네 가장자리 띠가 있습니다.");
                    float alpha = Read(opened, probes[i], 1, 1);
                    Assert.That(alpha, (int)side == i + 1 ? Is.LessThan(0.25f) : Is.GreaterThan(0.7f), $"{side}, EachCell {eachCell}, probe {i}");
                }
            }
            finally
            {
                Object.DestroyImmediate(closed);
                Object.DestroyImmediate(opened);
            }

            FieldCellFillStack.SetOpening(_material, default);
            Texture2D restored = Render(1, 1);
            try
            {
                Assert.That(Read(restored, probes[(int)side - 1], 1, 1), Is.GreaterThan(0.7f), "열린 변을 초기화하면 같은 재질에 띠가 복원됩니다.");
            }
            finally { Object.DestroyImmediate(restored); }
        }

        [Test]
        public void Shader_MergedOpening_HasNoBrightSeamsBetweenCells()
        {
            Create(3, 2);
            FieldCellFillStack.SetOpening(_material, new GridOutlineOpening(GridOutlineSide.Down, 0));
            Texture2D pixels = Render(3, 2);
            try
            {
                foreach (float x in new[] { 0f, 0.48f, 0.52f, 1f, 1.48f, 1.52f, 2f })
                    Assert.That(Read(pixels, new Vector2(x, -0.46f), 3, 2), Is.LessThan(0.25f), $"열린 변의 칸 경계 x={x}");
                Assert.That(Read(pixels, new Vector2(-0.46f, 0.5f), 3, 2), Is.GreaterThan(0.7f));
                Assert.That(Read(pixels, new Vector2(2.46f, 0.5f), 3, 2), Is.GreaterThan(0.7f));
                Assert.That(Read(pixels, new Vector2(1f, 1.46f), 3, 2), Is.GreaterThan(0.7f));
            }
            finally { Object.DestroyImmediate(pixels); }
        }

        [Test]
        public void Shader_Opening_KeepsUpperPlateHoleMask()
        {
            Create(1, 1);
            _hole.SetPixelData(new byte[] { 1 }, 0);
            _hole.Apply(false, false);
            _material.SetVector("_HoleInset", new Vector4(0.2f, 0.2f, 0f, 0f));
            FieldCellFillStack.SetOpening(_material, new GridOutlineOpening(GridOutlineSide.Down, 0));
            Texture2D pixels = Render(1, 1);
            try
            {
                Assert.That(Read(pixels, Vector2.zero, 1, 1), Is.LessThan(0.01f));
                Assert.That(Read(pixels, new Vector2(0f, -0.46f), 1, 1), Is.InRange(0.1f, 0.25f));
                Assert.That(Read(pixels, new Vector2(0.46f, 0f), 1, 1), Is.GreaterThan(0.7f));
            }
            finally { Object.DestroyImmediate(pixels); }
        }

        [TestCase(0.25f, 0f)]
        [TestCase(0.4f, 0f)]
        [TestCase(0.45f, 0.075f)]
        public void Shader_WideFade_HasNoSmallSquaresInsideMergedRegion(float width, float inset)
        {
            Create(3, 3);
            _material.SetFloat("_EdgeFadeWidth", width);
            _material.SetFloat("_CenterOpacity", 0f);
            _material.SetVector("_Inset", new Vector4(inset, inset, 0f, 0f));
            _material.SetFloat("_CornerRadius", 0.15f);
            Texture2D pixels = Render(3, 3);
            try
            {
                for (float z = 0.55f; z < 1.46f; z += 0.05f)
                    for (float x = 0.55f; x < 1.46f; x += 0.05f)
                        Assert.That(Read(pixels, new Vector2(x, z), 3, 3), Is.LessThan(0.01f), $"내부 작은 사각형: {x}, {z}, 폭 {width}");
                Assert.That(Read(pixels, new Vector2(-0.48f + inset, 1f), 3, 3), Is.GreaterThan(0.8f), "실제 바깥 테두리는 남습니다.");
            }
            finally { Object.DestroyImmediate(pixels); }
        }

        [Test]
        public void Shader_CenterActor_OpensAllFourAdjacentBands_AndKeepsOuterBorder()
        {
            Create(3, 3);
            _region.SetPixelData(new byte[] { 1, 1, 1, 1, 0, 1, 1, 1, 1 }, 0);
            _region.Apply(false, false);
            _material.SetFloat("_EdgeFadeWidth", 0.4f);
            FieldCellFillStack.SetOpening(_material, new GridOutlineOpening(GridOutlineSide.None, 0, new RectInt(1, 1, 1, 1)));
            Texture2D pixels = Render(3, 3);
            try
            {
                foreach (Vector2 point in new[] { new Vector2(0.46f, 1f), new Vector2(1.54f, 1f), new Vector2(1f, 0.46f), new Vector2(1f, 1.54f) })
                    Assert.That(Read(pixels, point, 3, 3), Is.LessThan(0.25f), $"캐릭터 인접 띠 {point}");
                Assert.That(Read(pixels, new Vector2(1f, 1f), 3, 3), Is.LessThan(0.01f));
                Assert.That(Read(pixels, new Vector2(-0.46f, 1f), 3, 3), Is.GreaterThan(0.7f));
            }
            finally { Object.DestroyImmediate(pixels); }
        }

        private void Create(int width, int height)
        {
            Shader shader = Shader.Find("ProjectL/Combat/CellFill");
            Assert.That(shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            _material = new Material(shader);
            ShaderUtil.CompilePass(_material, 0, true);
            _region = new Texture2D(width, height, TextureFormat.R8, false, true);
            _hole = new Texture2D(width, height, TextureFormat.R8, false, true);
            _heights = new Texture2D(width, height, TextureFormat.RFloat, false, true);
            var kinds = new byte[width * height];
            for (int i = 0; i < kinds.Length; i++) kinds[i] = 1;
            _region.SetPixelData(kinds, 0);
            _hole.SetPixelData(new byte[kinds.Length], 0);
            _heights.SetPixelData(new float[kinds.Length], 0);
            _region.Apply(false, false);
            _hole.Apply(false, false);
            _heights.Apply(false, false);
            _material.SetTexture("_RegionMap", _region);
            _material.SetTexture("_HoleMap", _hole);
            _material.SetTexture("_HeightMap", _heights);
            _material.SetVector("_GridCorner", new Vector4(-0.5f, -0.5f, 0f, 0f));
            _material.SetVector("_GridSize", new Vector4(width, height, 0f, 0f));
            _material.SetVector("_CellSize", Vector2.one);
            _material.SetFloat("_RegionKind", 1f);
            _material.SetColor("_BaseColor", new Color(1f, 0f, 0f, 1f));
            _material.SetFloat("_EdgeFadeWidth", 0.25f);
            _material.SetFloat("_CenterOpacity", 0.15f);
        }

        // 실제 운영 셰이더/열린 변 전달을 GPU로 그립니다. 빨강을 투명 검정 위에 합성해 R이 채움 알파입니다.
        private Texture2D Render(int width, int height)
        {
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(width - 0.5f, 0f, -0.5f),
                    new Vector3(width - 0.5f, 0f, height - 0.5f), new Vector3(-0.5f, 0f, height - 0.5f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            var commands = new CommandBuffer();
            Texture2D pixels = null;
            try
            {
                target.Create();
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, Color.clear);
                commands.SetViewport(new Rect(0f, 0f, Size, Size));
                Matrix4x4 view = Matrix4x4.identity;
                view.SetRow(1, new Vector4(0f, 0f, 1f, 0f));
                view.SetRow(2, new Vector4(0f, 1f, 0f, 0f));
                commands.SetViewProjectionMatrices(view, Matrix4x4.Ortho(-0.6f, width - 0.4f, -0.6f, height - 0.4f, -1f, 1f));
                commands.DrawMesh(mesh, Matrix4x4.identity, _material, 0, 0);
                Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active = target;
                pixels = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                pixels.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
                pixels.Apply(false, false);
                return pixels;
            }
            catch
            {
                Object.DestroyImmediate(pixels);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                commands.Dispose();
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(mesh);
            }
        }

        private static float Read(Texture2D pixels, Vector2 point, int width, int height) =>
            pixels.GetPixel(Mathf.Clamp((int)((point.x + 0.6f) / (width + 0.2f) * Size), 0, Size - 1),
                Mathf.Clamp((int)((point.y + 0.6f) / (height + 0.2f) * Size), 0, Size - 1)).r;
    }
}
