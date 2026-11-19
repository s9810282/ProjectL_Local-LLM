using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>원래 타격 대상의 현재 HP와 공격력을 비교합니다. 숫자 피해의 희생 수신자와 구분합니다.</summary>
    internal static class FinishingShotApplication
    {
        internal static void Record(CharacterCombatFacts actor, EntityId target, int health,
            IReadOnlyList<CellCoord> cells, CombatDamageOrigin origin, List<CombatEvent> events)
        {
            CombatCommandEvents.Trait(events, actor.EntityId, TraitKey.FinishingShot, TraitActivationKind.InstantDefeat,
                health, new[] { target }, cells, origin);
        }

        /// <summary>
        /// 한 타격에서 마무리 사격 즉사가 실제로 서는지의 유일한 판단입니다. 희생 넘기기가 먼저 정해지고, 수호자가 대신 맞았으면
        /// 원래 대상은 맞지 않으므로 즉사가 서지 않습니다. 수호자에게는 즉사를 판정하지 않고 숫자 피해만 줍니다(2026-11-10 사용자 결정).
        /// 공격 직접 피해·처치 연쇄 추가 공격·희생 모의 계산이 이 함수를 부릅니다.
        /// </summary>
        internal static bool FiresOnHit(CombatCommandInput input, CharacterCombatFacts actor, EntityId originalId, EntityId recipientId,
            CharacterCombatFacts target, StructureCombatFacts structure = null) =>
            originalId == recipientId && Qualifies(input, actor, target, structure);

        /// <summary>지키지 않으면 즉사로 죽을 대상인지(희생 넘기기 전 판정)입니다. 희생 계획이 치명 타격을 셀 때만 씁니다.</summary>
        internal static bool Qualifies(CombatCommandInput input, CharacterCombatFacts actor, CharacterCombatFacts target,
            StructureCombatFacts structure = null)
        {
            if (target == null && structure == null || !global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitStatusScan.Has(actor.Stats.TraitStatuses, TraitKey.FinishingShot)) return false;
            var health = target?.Health ?? structure.Health;
            var owner = target?.Owner ?? structure.Owner;
            if (health.IsDead || !new HostilityPolicy().AreHostile(actor.Owner, owner)) return false;
            foreach (var binding in CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor)))
                if (binding.Rule is FinishingShotEffectRule rule && rule.Qualifies(new EffectRuleInput(binding.Origin, binding.ExpectedState),
                    CombatEffectiveStatsCalculator.AttackPower(actor), health)) return true;
            return false;
        }
    }
}
