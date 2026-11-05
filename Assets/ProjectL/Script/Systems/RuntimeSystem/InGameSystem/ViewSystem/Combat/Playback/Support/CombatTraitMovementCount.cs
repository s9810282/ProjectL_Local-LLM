using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>완료한 자발 진입 사건만 셉니다. 능력치나 발동 규칙을 계산하지 않습니다.</summary>
    internal static class CombatTraitMovementCount
    {
        internal static int Before(IReadOnlyList<CombatEvent> events, int index, EntityId source)
        {
            int count = 0;
            for (int i = index - 1; i >= 0; i--)
            {
                CombatEvent e = events[i];
                if (CombatEventTypeTable.IsActionStart(e) || e.Kind == CombatEventKind.Returned || e is CombatEffectsEvent effects && effects.Traits.Any(t => t.SourceUnitId == source && CombatTraitFeedbackProfiles.Get(t.TraitKey, t.TraitActivationKind).Count == CombatBodyTextCount.CompletedMovementCells))
                    break;
                if (e is CombatCharacterEvent step && step.Kind == CombatEventKind.Moved && step.Final.EntityId == source && step.Expected.Anchor != step.Final.Anchor && step.MovementCause?.Agency != CombatMovementAgency.Forced)
                    count++;
            }

            return count;
        }
    }
}
