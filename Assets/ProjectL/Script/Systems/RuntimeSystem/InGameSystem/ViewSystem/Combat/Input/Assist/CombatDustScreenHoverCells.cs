using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>호버한 먼지 연막과 같은 Rider 특성 출처의 활성·공개 흔적 칸을 모읍니다. 임시 외형으로 종류를 판단하지 않습니다.</summary>
    internal static class CombatDustScreenHoverCells
    {
        /// <summary>기존 목록에 같은 출처의 전체 연막을 중복 없이 더합니다. 한 칸에 여러 출처가 겹치면 각 그룹을 합칩니다.</summary>
        internal static void Append(IReadOnlyList<MovementTileFact> sources, CellCoord cell, List<CellCoord> into)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (into == null) throw new ArgumentNullException(nameof(into));

            // 먼지 연막 판정과 보임은 CombatEnvironmentFacts 한 곳입니다(평가 C1-05, 작은 묶음 설계 P11). 조건은 모두 부작용 없는 읽기입니다.
            for (int i = 0; i < sources.Count; i++)
            {
                if (!CombatEnvironmentFacts.IsDustScreen(sources[i], out TraitTrailFact hovered) || hovered.Cell != cell
                    || !CombatEnvironmentFacts.IsShown(hovered))
                    continue;

                for (int j = 0; j < sources.Count; j++)
                {
                    if (CombatEnvironmentFacts.IsDustScreen(sources[j], out TraitTrailFact trail) && trail.Origin == hovered.Origin
                        && CombatEnvironmentFacts.IsShown(trail) && !into.Contains(trail.Cell))
                        into.Add(trail.Cell);
                }
            }
        }
    }
}
