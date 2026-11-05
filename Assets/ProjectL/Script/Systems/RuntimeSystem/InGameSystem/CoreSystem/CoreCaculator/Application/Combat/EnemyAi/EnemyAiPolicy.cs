using System.Collections.Generic;
using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
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
            => TrySelectAttackAim(input, range, out selected, out _);

        internal bool TrySelectAttackAim(CombatCommandInput input, AttackRangeResult range, out AttackAim selected, out EntityId appliedTaunt)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyAttackAim);
            selected = default;
            appliedTaunt = EntityId.Empty;
            if (!range.Succeeded) return false;
            if (range.AttackerId != input.ActorId) throw new ArgumentException(nameof(range));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var units = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).ToDictionary(c => c.EntityId);
            var structures = input.State.Structures.ToDictionary(s => s.EntityId);
            var taunters = TauntTargeting.Sources(input, actor);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            var elevation = AttackElevation.From(environment, input.EnvironmentBindings);
            int commonDamage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile);
            bool Delivers(AttackAim aim, EntityId targetId) => CombatAttackQueries.DeliversTo(input, actor, profile,
                aim, targetId, commonDamage, environment, walls, elevation);
            var required = taunters.FirstOrDefault(source => range.AttackAims.ToArray().Any(aim => Delivers(aim, source.EntityId)));
            // 칠 수 없는 도발 출처에게 갈 수 있으면 이번 행동은 추적 경로가 맡습니다.
            // 갈 수도 없으면 도발을 지킬 수 없으므로 도발이 없는 것처럼 고릅니다(2026-11-02 결정).
            if (taunters.Length != 0 && required == null && !ReachableTaunter(input, taunters).IsEmpty) return false;
            bool found = false;
            (int allies, int targetKind, long damage, int hits, long distance, int y, int x, int direction) best = default;
            foreach (var aim in range.AttackAims.ToArray())
            {
                if (required != null && !Delivers(aim, required.EntityId)) continue;
                AttackPreview preview;
                CombatCommandTransition projection;
                using (CoreProfiling.Sample(CoreProfileSection.EnemyAimPreview))
                    preview = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), _movementRules, out projection);
                if (!preview.CanAttack) continue;
                var damageByTarget = ProjectedDamage(projection);

                var targetIds = new HashSet<EntityId>(preview.Targets.ToArray().Select(t => t.TargetId));
                targetIds.UnionWith(damageByTarget.Keys);
                int allies = 0, hits = 0, unitHits = 0; long damage = 0;
                foreach (var targetId in targetIds)
                {
                    bool unit = units.TryGetValue(targetId, out var target);
                    Owner owner = unit ? target.Owner : structures[targetId].Owner;
                    if (unit && owner == actor.Owner) allies++;
                    bool kind = unit || structures[targetId].Definition.Key == EntityKey.Structure_Headquarters;
                    long dealt = damageByTarget.GetValueOrDefault(targetId);
                    // 합법적인 적대 대상은 피해 0이어도 공격한다. 실제 피해량은 선택 동점 해소에만 쓴다.
                    if (kind && _hostilityPolicy.AreHostile(actor.Owner, owner) )
                    { damage += dealt; hits++; if (unit) unitHits++; }
                }
                if (hits == 0) continue;
                var rank = Rank(aim, allies, unitHits > 0 ? 0 : 1, damage, hits, 0);
                if (found && rank.CompareTo(best) >= 0) continue;
                found = true; best = rank; selected = aim;
            }
            if (found && required != null) appliedTaunt = required.EntityId;
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
            => TrySelectPursuitTarget(input, out selected, out _);

        internal bool TrySelectPursuitTarget(CombatCommandInput input, out EntityId selected, out EntityId appliedTaunt)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyPursuitTarget);
            appliedTaunt = EntityId.Empty;
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            // 갈 수 있는 도발 출처가 있으면 그쪽으로, 없으면 도발을 지킬 수 없으므로 기존 추적 규칙을 따릅니다.
            var taunter = ReachableTaunter(input, TauntTargeting.Sources(input, actor));
            if (!taunter.IsEmpty)
            {
                selected = appliedTaunt = taunter;
                return true;
            }
            var units = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                _hostilityPolicy.AreHostile(actor.Owner, c.Owner)).Select(c => (c.EntityId, Anchor: c.Anchor.Value)).ToArray();
            var targets = units.Length > 0 ? units : input.State.Structures.Where(s => !s.Health.IsDead &&
                s.Definition.Key == EntityKey.Structure_Headquarters && _hostilityPolicy.AreHostile(actor.Owner, s.Owner))
                .Select(s => (s.EntityId, s.Anchor)).ToArray();
            selected = targets.OrderBy(t => CalculateManhattan(actor.Anchor.Value, t.Anchor)).ThenBy(t => t.EntityId)
                .Select(t => t.EntityId).DefaultIfEmpty(EntityId.Empty).First();
            return !selected.IsEmpty;
        }

        /// <summary>
        /// 추적 경로가 있는 첫 도발 출처(가까운 순)를 고릅니다. 이미 이동했거나 이동이 막힌 적은 경로가 없습니다.
        /// 공격 고르기와 추적 고르기가 "도발을 지킬 수 있나"를 같은 판정으로 봅니다.
        /// </summary>
        private static EntityId ReachableTaunter(CombatCommandInput input, IEnumerable<CharacterCombatFacts> taunters) =>
            taunters.Where(source => EnemyMovementPathfinder.TryFind(input, source.EntityId, out _))
                .Select(source => source.EntityId).DefaultIfEmpty(EntityId.Empty).First();

        private static (int allies, int targetKind, long damage, int hits, long distance, int y, int x, int direction)
            Rank(AttackAim aim, int allies, int targetKind, long damage, int hits, long distance) =>
            (allies, targetKind, -damage, -hits, distance, aim.Cell?.Y ?? 0, aim.Cell?.X ?? 0, (int)aim.Direction);

        private static long CalculateManhattan(CellCoord left, CellCoord right)
        {
            long xDistance = Math.Abs((long)left.X - right.X);
            long yDistance = Math.Abs((long)left.Y - right.Y);

            return xDistance + yDistance;
        }
    }
}
