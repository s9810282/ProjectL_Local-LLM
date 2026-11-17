using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>진영 시작에 포착한 지속 피해만 공통 피해 경계로 전달합니다. 전체 묶음의 종료 판정은 호출자가 맡습니다.</summary>
    internal static class PeriodicStatusApplication
    {
        internal static void RefreshFire(Dictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events,
            EntityId recipient, EffectOriginKey origin, int duration)
        {
            var before = characters[recipient];
            if (before.Health.IsDead || before.Area != CharacterRuntimeArea.Field) return;
            var statuses = PeriodicStatusRules.ApplyFire(before.StatusEffects, origin, duration);
            if (before.StatusEffects.SameValues(statuses)) return;
            var after = before.WithStatusEffects(statuses);
            characters[recipient] = after;
            CombatCommandEvents.TraitState(events, before, after);
        }

        internal static void BeginSide(CombatCommandInput input, MovementProjection projection, Owner side)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (side != Owner.Player && side != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(side));
            var batch = projection.Damage.Characters.Values.Where(c => c.Owner == side &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).OrderBy(c => c.EntityId)
                .Select(c => (c.EntityId, Fire: c.StatusEffects.Entries.OfType<FireStatus>().SingleOrDefault(),
                    Bleeding: c.StatusEffects.Entries.OfType<BleedingStatus>().SingleOrDefault())).ToArray();
            foreach (var recipient in batch)
            {
                if (recipient.Fire != null)
                {
                    Tick(projection, recipient.EntityId, recipient.Fire.Origin.Value, recipient.Fire.Damage);
                    Consume(projection, recipient.EntityId, recipient.Fire);
                }
                if (recipient.Bleeding == null) continue;
                foreach (var stack in recipient.Bleeding.Stacks)
                    Tick(projection, recipient.EntityId, stack.Origin, stack.Damage);
                Consume(projection, recipient.EntityId, recipient.Bleeding);
            }
        }

        private static void Tick(MovementProjection projection, EntityId recipient, EffectOriginKey source, int amount)
        {
            if (projection.Damage.Characters[recipient].Health.IsDead) return;
            EntityId? sourceId = source.Kind == EffectOwnerKind.Trait && source.OwnerInstanceId > 0
                ? new EntityId(source.OwnerInstanceId) : null;
            CellCoord? sourceCell = sourceId.HasValue && projection.Damage.Characters.TryGetValue(sourceId.Value, out var actor)
                ? actor.Anchor : null;
            // 사라진 출처도 ID는 유지합니다. 보상 수신 가능 여부는 기존 처치 정산이 판단합니다.
            var origin = new CombatDamageOrigin(sourceCell, sourceId, projection.NextAttackGroup());
            projection.Damage.Apply(recipient, CombatDamageKind.DamageOverTime, origin, amount, effectOrigin: source);
        }

        private static void Consume(MovementProjection projection, EntityId recipient, AppliedStatus captured)
        {
            var before = projection.Damage.Characters[recipient];
            if (before.Health.IsDead) return;
            // 피해 처리 도중 새로 부여·갱신된 상태의 미래 횟수는 이번 시작에서 소비하지 않습니다.
            if (!before.StatusEffects.Entries.Any(s => ReferenceEquals(s, captured))) return;
            var after = before.WithStatusEffects(PeriodicStatusRules.ConsumeStartTick(before.StatusEffects, captured.Key));
            projection.Damage.Characters[recipient] = after;
            CombatCommandEvents.TraitState(projection.Events, before, after);
        }
    }
}
