using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 한 연산의 고지대 칸 집합이다. 연산마다 만드는 파생값이며 저장하지 않는다.
    /// 고지대 → 저지대 직격의 Wall 무시 공식(<see cref="BypassesWalls"/>)은 이 형식에만 둔다.
    /// 관찰자 투영은 호출자가 넘긴 환경을 따른다: 미리보기·범위·AI는 공개 환경, 실행은 전체 환경(설계 D8).
    /// </summary>
    internal sealed class AttackElevation
    {
        private readonly HashSet<CellCoord> _highGroundCells;

        internal AttackElevation(IEnumerable<CellCoord> highGroundCells)
        {
            if (highGroundCells == null) throw new ArgumentNullException(nameof(highGroundCells));
            _highGroundCells = new HashSet<CellCoord>(highGroundCells);
        }

        internal static AttackElevation From(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings) =>
            new AttackElevation(EnvironmentTerrainRules.TerrainCells(environment, bindings, StaticTerrainKind.HighGround));

        internal bool IsHigh(CellCoord cell) => _highGroundCells.Contains(cell);

        /// <summary>출발 칸이 고지대이고 맞는 칸이 고지대가 아니면 그 직격 선분은 Wall에 막히지 않는다(설계 D1).</summary>
        internal bool BypassesWalls(CellCoord origin, CellCoord contact) => IsHigh(origin) && !IsHigh(contact);
    }
}
