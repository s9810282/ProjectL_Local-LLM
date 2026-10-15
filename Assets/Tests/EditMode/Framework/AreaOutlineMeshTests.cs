using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    /// <summary>AreaOutline 메시의 U가 점선 길이를 주면 루프 둘레에 비례해 늘어나는지 확인합니다.</summary>
    public sealed class AreaOutlineMeshTests
    {
        [Test]
        public void DashLength_RepeatsPerLoopByPerimeter()
        {
            // 칸 하나: 둘레 4 → 점선 길이 0.5면 8번. 1×3 줄: 둘레 8 → 16번. 점선 하나의 길이가 같다.
            Assert.That(MaxU(new[] { new Vector2Int(0, 0) }, 0.5f), Is.EqualTo(8f).Within(1e-4f));
            Assert.That(MaxU(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, 0.5f),
                Is.EqualTo(16f).Within(1e-4f));
        }

        [Test]
        public void DashLength_RoundsToWholeRepeats_AndKeepsAtLeastOne()
        {
            Assert.That(MaxU(new[] { new Vector2Int(0, 0) }, 0.7f), Is.EqualTo(6f).Within(1e-4f), "4 ÷ 0.7 = 5.7 → 6");
            Assert.That(MaxU(new[] { new Vector2Int(0, 0) }, 100f), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void NoDashLength_KeepsNormalizedLoop()
        {
            Assert.That(MaxU(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, 0f), Is.EqualTo(1f).Within(1e-4f));
        }

        private static float MaxU(Vector2Int[] cells, float dashLength)
        {
            var data = new GridOutlineCreateData("Test", cells, Vector3.zero, Vector2.one, 0f, 0.1f, Color.white, dashLength);
            return MeshHightLevelBuilder.CreateGridOutline(data).UVs.Max(uv => uv.x);
        }
    }
}
