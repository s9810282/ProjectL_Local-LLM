using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>포인터가 현재 증원 본부의 점유 칸에 있을 때만 Buffer의 공급 범위를 보입니다.</summary>
    internal sealed class CombatNexusRangeHover
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionNexusHoverPoll = new("Combat.Action.Interaction.NexusHover.Poll");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionNexusHoverClear = new("Combat.Action.Interaction.NexusHover.Clear");
#endif

        private readonly ICombatNexusRangeFeedback _feedback;
        private bool _shown;

        internal CombatNexusRangeHover(ICombatNexusRangeFeedback feedback)
        {
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        internal void Refresh(CombatStateData state, CellCoord cell)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionNexusHoverPollSample = _profileInteractionNexusHoverPoll.Auto();
#endif

            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.NexusReinforcements == null || !state.CommandState.Reinforcements.IsEnabled)
            {
                Clear();
                return;
            }

            foreach (StructureCombatFacts structure in state.Structures)
            {
                if (structure.EntityId != state.CommandState.Reinforcements.NexusId || structure.Owner != Owner.Enemy
                    || structure.Health.IsDead || !Contains(structure.OccupiedCells, cell))
                    continue;

                IReadOnlyList<CellCoord> area = state.ReadNexusReinforcementArea();
                if (area.Count == 0)
                    break;
                _feedback.ShowNexusRange(area);
                _shown = true;
                return;
            }
            Clear();
        }

        internal void Clear()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionNexusHoverClearSample = _profileInteractionNexusHoverClear.Auto();
#endif

            if (!_shown) return;
            _shown = false;
            _feedback.ClearNexusRange();
        }

        private static bool Contains(IReadOnlyList<CellCoord> cells, CellCoord cell)
        {
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] == cell) return true;
            return false;
        }
    }
}
