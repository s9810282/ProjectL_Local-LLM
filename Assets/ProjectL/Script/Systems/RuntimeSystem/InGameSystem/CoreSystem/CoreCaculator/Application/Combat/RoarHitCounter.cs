using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 포효 Trait를 가진 캐릭터의 밤 피격 횟수를 투영 캐릭터에 즉시 기록한다(D7). 카운터는 HP처럼 Finish로 게시된다.
    /// 피해 writer가 피해 계산을 시작한 시점(0 피해 포함, 회복 제외)에 호출한다.
    /// </summary>
    internal static class RoarHitCounter
    {
        /// <summary>센 경우 true다. 낮이거나, 죽었거나, 포효 Trait가 없거나, 같은 원인에서 이미 셌으면 바꾸지 않는다.</summary>
        internal static bool Record(IDictionary<EntityId, CharacterCombatFacts> characters, EntityId targetId,
            CombatHitCause cause, bool isNight)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (cause == null) throw new ArgumentNullException(nameof(cause));
            if (!isNight || !characters.TryGetValue(targetId, out var target) || target.Health.IsDead) return false;
            var traits = target.Stats.TraitStatuses.ToArray();
            int index = Array.FindIndex(traits, t => t is RoarTraitStatusSnapshot);
            if (index < 0 || !cause.TryCount(targetId)) return false;
            var roar = (RoarTraitStatusSnapshot)traits[index];
            traits[index] = new RoarTraitStatusSnapshot(checked(roar.HitCount + 1), roar.HitThreshold);
            characters[targetId] = CombatCommandCalculator.Copy(target, target.Health, CombatCommandEffects.With(target.Stats, traits: traits));
            return true;
        }

        /// <summary>현재 카운터다. 포효 Trait가 없으면 null이다.</summary>
        internal static int? HitCount(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<RoarTraitStatusSnapshot>().SingleOrDefault()?.HitCount;

        /// <summary>포효 발동 시 카운터를 0으로 되돌린다(G6-2). 포효 피해 계산 전에 호출한다.</summary>
        internal static CharacterCombatFacts Reset(CharacterCombatFacts character)
        {
            var traits = character.Stats.TraitStatuses.ToArray();
            int index = Array.FindIndex(traits, t => t is RoarTraitStatusSnapshot);
            if (index < 0) throw new InvalidOperationException("포효 Trait가 없는 캐릭터의 카운터를 되돌릴 수 없습니다.");
            traits[index] = new RoarTraitStatusSnapshot(0, ((RoarTraitStatusSnapshot)traits[index]).HitThreshold);
            return CombatCommandCalculator.Copy(character, character.Health, CombatCommandEffects.With(character.Stats, traits: traits));
        }
    }
}
