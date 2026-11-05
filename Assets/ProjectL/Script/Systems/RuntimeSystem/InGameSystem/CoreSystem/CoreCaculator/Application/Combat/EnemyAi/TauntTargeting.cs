using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>현재 출처만 조회합니다. 공격·이동 가능 여부와 자원 정산은 기존 소유자에게 맡깁니다.</summary>
    internal static class TauntTargeting
    {
        internal static CharacterCombatFacts[] Sources(CombatCommandInput input, CharacterCombatFacts actor) =>
            Sources(input, input.Characters, actor);

        /// <summary>
        /// 주어진 유닛 목록에서 <paramref name="actor"/>를 도발하는 유닛을 가까운 순으로 찾습니다.
        /// 적 AI(명령 입력의 유닛)와 보유 조정(이번 투영의 유닛)이 같은 판정을 쓰도록 목록만 받습니다.
        /// </summary>
        internal static CharacterCombatFacts[] Sources(CombatCommandInput input, IEnumerable<CharacterCombatFacts> characters,
            CharacterCombatFacts actor, CombatProfileReadSet profileReads = null) => characters
            .Where(source => !source.Health.IsDead && source.Area == CharacterRuntimeArea.Field &&
                source.Stats.TraitStatuses.ToArray().Any(t => t.Key == Contracts.Common.TraitKey.Taunt))
            .Where(source => CombatCommandEffects.Capture(source, CombatCommandCalculator.RequiredProfile(input, source, profileReads))
                .Any(binding => binding.Rule is TauntEffectRule rule &&
                    rule.Affects(new EffectRuleInput(binding.Origin, binding.ExpectedState), source, actor)))
            .OrderBy(source => source.OccupiedCells.Min(cell => actor.OccupiedCells.Min(origin =>
                Math.Abs((long)cell.X - origin.X) + Math.Abs((long)cell.Y - origin.Y))))
            .ThenBy(source => source.EntityId).ToArray();
    }
}
