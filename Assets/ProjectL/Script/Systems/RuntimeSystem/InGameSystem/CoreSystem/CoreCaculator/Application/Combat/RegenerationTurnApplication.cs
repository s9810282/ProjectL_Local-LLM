using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>환경·승패 정산을 마친 진영 종료 투영에서 재생 주기와 기존 회복 경로를 연결합니다.</summary>
    internal static class RegenerationTurnApplication
    {
        internal static void EndOwnSide(CombatCommandInput input, CombatDamageApplication settlement, Owner endedSide)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (settlement == null) throw new ArgumentNullException(nameof(settlement));
            if (endedSide != Owner.Player && endedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(endedSide));
            if (input.State.Phase == CombatFlowPhase.Finished) return;
            foreach (var character in settlement.Characters.Values.Where(c => c.Owner == endedSide &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).OrderBy(c => c.EntityId).ToArray())
            {
                var status = Status(character);
                if (status == null || !status.IsAvailable) continue;
                int progress = RegenerationTraitRule.EndOwnSide(status, out bool due);
                var progressed = WithProgress(character, status, progress);
                int actual = 0;
                var final = due ? CharacterSettlementCalculator.Recover(progressed, status.HealAmount, out actual) : progressed;
                settlement.Characters[character.EntityId] = final;
                CombatCommandEvents.TraitState(settlement.Events, character, progressed);
                settlement.RecordRecovery(character.EntityId, actual);
                if (actual == 0) continue;
                CombatCommandEvents.Trait(settlement.Events, character.EntityId, TraitKey.Regeneration,
                    TraitActivationKind.HealthRecovered, actual, new[] { character.EntityId });
                CombatCommandEvents.Health(settlement.Events, progressed, final);
            }
        }

        /// <summary>필드 이탈·사망·전투 종료 때 재생 진행값만 비웁니다. 무관한 사실은 보존합니다.</summary>
        internal static CharacterCombatFacts Reset(CharacterCombatFacts character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var status = Status(character);
            return status == null ? character : WithProgress(character, status, 0);
        }

        private static RegenerationTraitStatusSnapshot Status(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<RegenerationTraitStatusSnapshot>().SingleOrDefault();

        private static CharacterCombatFacts WithProgress(CharacterCombatFacts character, RegenerationTraitStatusSnapshot status, int progress)
        {
            if (status.CompletedEligibleTurnEnds == progress) return character;
            var updated = new RegenerationTraitStatusSnapshot(status.HealAmount, status.HealEveryOwnSideTurnEnds, progress)
                .WithAvailability(status.UnavailableReason, status.PartialUnavailableReason);
            return character.With(stats: character.Stats.With(traitStatuses: character.Stats.TraitStatuses.ToArray()
                .Select(s => s.Key == TraitKey.Regeneration ? updated : s).ToArray()));
        }
    }
}
