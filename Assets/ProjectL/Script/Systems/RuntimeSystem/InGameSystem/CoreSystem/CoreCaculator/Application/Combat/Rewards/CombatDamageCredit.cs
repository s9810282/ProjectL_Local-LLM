using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards
{
    /// <summary>
    /// 피해 하나가 어느 보유 아군의 것인지 정하는 규칙 한 곳이다. 처치 보상(경험치·골드)과 스테이지 기록의 넣은 피해가
    /// 같은 규칙을 쓴다(유닛별 스테이지 기록 설계 D4). 판단만 하고 상태를 바꾸지 않는다.
    /// </summary>
    internal static class CombatDamageCredit
    {
        /// <summary>
        /// 출처 목록의 피해를 받는 아군이다(경험치·레벨업 설계 §3.2). 기록된 첫 가해자 → 가시심장 반사의 맞은 아군 →
        /// 플레이어 명령의 행동자 순서로 처음 맞는 규칙 하나를 쓴다. 받는 개체는 살아 있지 않아도 된다. 보유 아군이 아니면 null이다.
        /// </summary>
        internal static EntityId? Recipient(CombatCommandInput input, IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected,
            IReadOnlyList<CombatDamageOrigin> origins, IReadOnlyList<ThornHeartReflectionContext> reflections)
        {
            var id = CharacterRecipient(input, projected, origins, reflections);
            return id.HasValue ? OwnedAlly(projected, id.Value) : null;
        }

        /// <summary>특성은 양 진영의 실제 귀속을 쓰고, 기존 보상은 보유 아군 필터를 그대로 적용합니다.</summary>
        internal static EntityId? CharacterRecipient(CombatCommandInput input, IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected,
            IReadOnlyList<CombatDamageOrigin> origins, IReadOnlyList<ThornHeartReflectionContext> reflections)
        {
            var credited = origins.FirstOrDefault(o => o.CreditedCharacterId.HasValue);
            if (credited != null)
                return credited.CreditedCharacterId.Value;

            var reflection = reflections.FirstOrDefault(r =>
                origins.Any(o => o.AttackGroup == r.ReflectionAttackGroup));
            if (reflection != null)
                return reflection.StruckAllyId;

            if (input.ProcessingKind != CombatProcessingKind.Command || input.ActorId.IsEmpty)
                return null;
            return input.ActorId;
        }

        /// <summary>한 명령 사건 목록의 가시심장 반사 문맥이다. <see cref="Recipient"/>의 둘째 규칙이 읽는다.</summary>
        internal static IReadOnlyList<ThornHeartReflectionContext> Reflections(IReadOnlyList<CombatEvent> events) =>
            events.OfType<CombatArtifactEvent>().Where(e => e.Reflection != null).Select(e => e.Reflection).ToArray();

        private static EntityId? OwnedAlly(IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected, EntityId id) =>
            projected.TryGetValue(id, out var facts) && facts.Owner == Owner.Player && facts.OwnedId.HasValue
                ? id : null;
    }
}
