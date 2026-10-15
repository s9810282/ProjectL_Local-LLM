using System.Collections.Generic;
using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>
    /// 실제 결과를 인게임에 반영하는 것이 아닌, 선택 자체를 반환
    /// </summary>
    internal sealed class EnemyAiPolicy
    {
        private readonly HostilityPolicy _hostilityPolicy;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;

        internal EnemyAiPolicy(HostilityPolicy hostilityPolicy, IReadOnlyList<IMovementTileRule> movementRules)
        {
            if (hostilityPolicy == null)
                throw new ArgumentNullException(nameof(hostilityPolicy));

            _hostilityPolicy = hostilityPolicy;
            _movementRules = movementRules ?? throw new ArgumentNullException(nameof(movementRules));
        }

        /// <summary>같은 공격 계산의 미리보기로 기존 점수와 대상 종류 우선순위를 평가한다.</summary>
        internal bool TrySelectAttackAim(CombatCommandInput input, AttackRangeResult range, out AttackAim selected)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            selected = default;
            if (!range.Succeeded || !range.CanAffordAttack) return false;
            if (range.AttackerId != input.ActorId) throw new ArgumentException(nameof(range));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var units = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).ToDictionary(c => c.EntityId);
            var structures = input.State.Structures.ToDictionary(s => s.EntityId);
            bool targetUnits = units.Values.Any(c => _hostilityPolicy.AreHostile(actor.Owner, c.Owner));
            bool found = false;
            (int allies, long damage, int hits, long distance, int y, int x, int direction) best = default;
            foreach (var aim in range.AttackAims.ToArray())
            {
                var preview = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), _movementRules, out var projection);
                if (!preview.CanAttack) continue;
                var damageByTarget = ProjectedDamage(projection);
                var targetIds = new HashSet<EntityId>(preview.Targets.ToArray().Select(t => t.TargetId));
                targetIds.UnionWith(damageByTarget.Keys);
                int allies = 0, hits = 0; long damage = 0;
                foreach (var targetId in targetIds)
                {
                    bool unit = units.TryGetValue(targetId, out var target);
                    Owner owner = unit ? target.Owner : structures[targetId].Owner;
                    if (unit && owner == actor.Owner) allies++;
                    bool kind = targetUnits ? unit : !unit && structures[targetId].Definition.Key == EntityKey.Structure_Headquarters;
                    long dealt = damageByTarget.GetValueOrDefault(targetId);
                    if (kind && _hostilityPolicy.AreHostile(actor.Owner, owner) && dealt > 0)
                    { damage += dealt; hits++; }
                }
                if (damage == 0) continue;
                var rank = Rank(aim, allies, damage, hits, 0);
                if (found && rank.CompareTo(best) >= 0) continue;
                found = true; best = rank; selected = aim;
            }
            return found;
        }

        /// <summary>동일한 공개 투영의 피해 사건을 합산한다. 회복·위치 정규화와 직접 타격의 무피해 접촉은 구분한다.</summary>
        private static Dictionary<EntityId, long> ProjectedDamage(CombatCommandTransition projection)
        {
            var damage = new Dictionary<EntityId, long>();
            foreach (var entry in projection.Events)
            {
                EntityId id;
                int loss;
                switch (entry)
                {
                    case CombatCharacterEvent character when
                        (character.Kind == CombatEventKind.HealthChanged || character.Kind == CombatEventKind.Died) &&
                        character.DamageOrigins.Count > 0:
                        id = character.Final.EntityId;
                        loss = character.Expected.Health.CurrentHealth - character.Final.Health.CurrentHealth;
                        break;
                    case CombatStructureEvent structure when structure.DamageOrigins.Count > 0:
                        id = structure.Final.EntityId;
                        loss = structure.Expected.Health.CurrentHealth - structure.Final.Health.CurrentHealth;
                        break;
                    default:
                        continue;
                }
                if (loss > 0) damage[id] = checked(damage.GetValueOrDefault(id) + loss);
            }
            return damage;
        }

        internal bool TrySelectPursuitTarget(CombatCommandInput input, out EntityId selected)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var units = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                _hostilityPolicy.AreHostile(actor.Owner, c.Owner)).Select(c => (c.EntityId, Anchor: c.Anchor.Value)).ToArray();
            var targets = units.Length > 0 ? units : input.State.Structures.Where(s => !s.Health.IsDead &&
                s.Definition.Key == EntityKey.Structure_Headquarters && _hostilityPolicy.AreHostile(actor.Owner, s.Owner))
                .Select(s => (s.EntityId, s.Anchor)).ToArray();
            selected = targets.OrderBy(t => CalculateManhattan(actor.Anchor.Value, t.Anchor)).ThenBy(t => t.EntityId)
                .Select(t => t.EntityId).DefaultIfEmpty(EntityId.Empty).First();
            return !selected.IsEmpty;
        }

        private static (int allies, long damage, int hits, long distance, int y, int x, int direction)
            Rank(AttackAim aim, int allies, long damage, int hits, long distance) =>
            (allies, -damage, -hits, distance, aim.Cell?.Y ?? 0, aim.Cell?.X ?? 0, (int)aim.Direction);

        private static long CalculateManhattan(CellCoord left, CellCoord right)
        {
            long xDistance = Math.Abs((long)left.X - right.X);
            long yDistance = Math.Abs((long)left.Y - right.Y);

            return xDistance + yDistance;
        }
    }
}
