using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// 보유 카드의 전투 밖 공격 무늬다. 지정 방식 모양·진영 정면 각도·기준 사거리만 반영하고,
    /// 지형 사거리 보정·보드·하붑·벽·특성 효과(끌어오기 추가 사거리, 캐논 인접 금지 등)는 넣지 않는다.
    /// 조준 종류는 <see cref="AttackProfileCompatibility.GetSupportedAimKind"/>로 <see cref="Method"/>에서 얻는다.
    /// </summary>
    public sealed class AttackPatternSnapshot
    {
        public TargetingMethodSpec Method { get; }

        /// <summary>공격자 칸 (0,0) 기준 아군 정면의 지정 가능 칸이다. 방향 조준은 모든 방향을 합친다. Y·X 순서다.</summary>
        public IReadOnlyList<CellCoord> RangeCells { get; }

        /// <summary>주 칸 기준 추가 범위 칸이다. 추가 범위가 없으면 비어 있다.</summary>
        public IReadOnlyList<CellCoord> SplashOffsets { get; }

        public AttackPatternSnapshot(TargetingMethodSpec method, IReadOnlyList<CellCoord> rangeCells,
            IReadOnlyList<CellCoord> splashOffsets)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            if (rangeCells == null)
                throw new ArgumentNullException(nameof(rangeCells));
            if (splashOffsets == null)
                throw new ArgumentNullException(nameof(splashOffsets));
            if ((method.Splash == TargetingSplash.None) != (splashOffsets.Count == 0))
                throw new ArgumentException("추가 범위 칸은 지정 방식에 추가 범위가 있을 때만 있어야 합니다.", nameof(splashOffsets));

            RangeCells = Array.AsReadOnly(Copy(rangeCells));
            SplashOffsets = Array.AsReadOnly(Copy(splashOffsets));
        }

        private static CellCoord[] Copy(IReadOnlyList<CellCoord> cells)
        {
            var copied = new CellCoord[cells.Count];
            for (int i = 0; i < copied.Length; i++)
                copied[i] = cells[i];
            return copied;
        }
    }
}
