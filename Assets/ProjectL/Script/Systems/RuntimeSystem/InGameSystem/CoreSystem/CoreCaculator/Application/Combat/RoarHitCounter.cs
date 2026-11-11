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
    /// 포효 Trait를 가진 캐릭터의 밤 피격을 투영 캐릭터의 카운터에 즉시 기록한다(D7). 카운터는 HP처럼 Finish로 게시된다.
    /// 피해 writer가 피해 계산을 시작한 시점(0 피해 포함, 회복 제외)에 호출한다. 진영 턴 종료 카운트는 <see cref="RoarTurnApplication"/>이 한다.
    /// </summary>
    internal static class RoarHitCounter
    {
        /// <summary>
        /// 센 경우 true다. 낮이거나, 죽었거나, 포효 Trait가 없거나, 피격 증가량이 0이거나, 같은 원인에서 이미 셌으면 바꾸지 않는다.
        /// 피격 1회에 더하는 양은 정의의 피격 증가량이다.
        /// </summary>
        internal static bool Record(IDictionary<EntityId, CharacterCombatFacts> characters, EntityId targetId,
            CombatHitCause cause, bool inCombat)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (cause == null) throw new ArgumentNullException(nameof(cause));
            if (!inCombat || !characters.TryGetValue(targetId, out var target) || target.Health.IsDead) return false;
            var traits = target.Stats.TraitStatuses.ToArray();
            int index = Array.FindIndex(traits, t => t is RoarTraitStatusSnapshot);
            if (index < 0) return false;
            var roar = (RoarTraitStatusSnapshot)traits[index];
            if (roar.CountPerHit == 0 || !cause.TryCount(targetId)) return false;
            characters[targetId] = With(target, traits, index, roar.WithHitCount(checked(roar.HitCount + roar.CountPerHit)));
            return true;
        }

        /// <summary>현재 포효 상태다. 포효 Trait가 없으면 null이다.</summary>
        internal static RoarTraitStatusSnapshot Status(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<RoarTraitStatusSnapshot>().SingleOrDefault();

        /// <summary>포효 상태 하나만 바꾼 캐릭터 사본이다. 나머지 특성 상태는 보존한다.</summary>
        internal static CharacterCombatFacts WithStatus(CharacterCombatFacts character, RoarTraitStatusSnapshot status)
        {
            var traits = character.Stats.TraitStatuses.ToArray();
            int index = Array.FindIndex(traits, t => t is RoarTraitStatusSnapshot);
            if (index < 0) throw new InvalidOperationException("포효 Trait가 없는 캐릭터의 카운터를 바꿀 수 없습니다.");
            return With(character, traits, index, status);
        }

        private static CharacterCombatFacts With(CharacterCombatFacts character, TraitStatusSnapshot[] traits, int index,
            RoarTraitStatusSnapshot status)
        {
            if (ReferenceEquals(traits[index], status)) return character;
            traits[index] = status;
            return CombatCommandCalculator.Copy(character, character.Health, CombatCommandEffects.With(character.Stats, traits: traits));
        }

        /// <summary>현재 카운터다. 포효 Trait가 없으면 null이다.</summary>
        internal static int? HitCount(CharacterCombatFacts character) => Status(character)?.HitCount;

        /// <summary>포효 발동 시 카운터를 0으로 되돌린다(G6-2). 넘친 값은 이월하지 않는다. 포효 피해 계산 전에 호출한다.</summary>
        internal static CharacterCombatFacts Reset(CharacterCombatFacts character)
        {
            var status = Status(character) ??
                throw new InvalidOperationException("포효 Trait가 없는 캐릭터의 카운터를 되돌릴 수 없습니다.");
            return WithStatus(character, status.WithHitCount(0));
        }
    }
}
