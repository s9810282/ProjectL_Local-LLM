using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills
{
    internal static class MagicBulletCalculator
    {
        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            bool judgment = input.State.PlayerSkill.UpgradeKey == PlayerSkillUpgradeKey.Judgment;
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var route = Path(input).ToArray();
            var policy = new AttackTargetingPolicy(true, true, false, false);
            var characters = input.Characters.Where(c => AttackTargetEligibility.CanTargetCharacter(actor, c, policy)).ToArray();
            var structures = input.State.Structures.Where(s => AttackTargetEligibility.CanTargetStructure(actor, s,
                input.EnvironmentBindings.ObstacleEntityIds.Contains(s.EntityId), policy)).ToArray();
            var encountered = new HashSet<EntityId>();
            var contacts = new List<(EntityId Id, int PathIndex)>();
            for (int i = 0; i < route.Length; i++)
            {
                var cell = route[i].Cell;
                foreach (var id in characters.Where(c => c.OccupiedCells.Contains(cell)).Select(c => c.EntityId)
                    .Concat(structures.Where(s => s.OccupiedCells.Contains(cell)).Select(s => s.EntityId)).OrderBy(id => id))
                    if (encountered.Add(id)) contacts.Add((id, i));
            }
            var cells = new AttackCellSelection(null, route.Select(p => p.Cell).ToArray(), Array.Empty<CellCoord>());
            // 심판의 범위와 적 수는 첫 조우에서 끝난다. 뒤의 경로 후보는 포함하지 않는다.
            var selection = new ResolvedAttackSelection(cells, cells, contacts.Take(judgment ? 1 : contacts.Count).Select(c =>
                new KeyValuePair<EntityId, AttackTargetRole>(c.Id, AttackTargetRole.Effect)).ToArray(), characters, structures, policy);
            var world = new CommandAttackProjection(input, input.State.Environment, null);
            // 투사체의 범위 적 수는 각 타격 직전에 남은 조우 예산으로 계산한다.
            var scope = new AttackTraitScope(actor);
            TraitAttackNotifications.Apply(input, world, scope);
            actor = world.Characters[input.ActorId];
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            int common = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.PlayerSkillAttack, world.Events, scope);
            int budget = 1;
            if (!judgment)
            {
                var falloff = engine.EvaluatePierceFalloff(bindings, new PierceFalloffEffectContext(common, 0));
                if (falloff.Contributions.Count != 1)
                    throw new InvalidOperationException("마탄에는 기존 관통 감쇠 설정 한 개가 필요합니다.");
                budget = falloff.TargetBudget.Value;
            }
            var delivery = new ResolvedAttackDelivery(contacts.Select(c =>
                new DeliveredAttackHit(c.Id, AttackTargetRole.Effect, common, 100, actor.Anchor)), cells.DesignatedCells.ToArray());
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, input.State.Environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var plan = new CommandAttackPlan(input, rules, profile, actor, actor, input.State.Environment, walls,
                selection, delivery, engine, bindings, new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                common, actor.Rotation.Value, false, damageKind: CombatDamageKind.PlayerSkillAttack, attackGroup: world.AttackGroups.Next(),
                traitScope: scope, completeConsequences: true, damagePercent: judgment ? 150 : 100,
                bypassPhysicalDefense: judgment, projectileTargetBudget: budget, projectileFalloff: !judgment,
                forcedDisplacement: judgment && contacts.Count > 0
                    ? new AttackDisplacementRequest(route[contacts[0].PathIndex].Incoming, 2) : null,
                // 꺾인 뒤 맞은 대상은 공격자 칸 기준 방향이 탄 진행과 다르다. 넉백은 맞은 구간의 들어온 방향을 쓴다(넉백 방향 설계 2026-11-17).
                projectileIncomingSteps: judgment ? null : contacts.ToDictionary(c => c.Id, c => route[c.PathIndex].Incoming));
            var ledger = new CommandAttackLedger();
            var arrival = AttackDeliveryExecutor.Execute(plan, world, ledger, new HashSet<EntityId>());
            var actual = ledger.HitIds.SelectMany(id => ledger.Contacts(id)).OrderBy(c => c.DeliveryIndex).ToArray();
            var positions = contacts.ToDictionary(c => c.Id, c => c.PathIndex);
            int length = budget == 0 ? 0 : actual.Length >= budget ? positions[actual.Last().OriginalTargetId] + 1 : route.Length;
            var path = route.Take(length).Select(p => p.Cell).ToArray();
            var incoming = actual.ToDictionary(c => c.DeliveryIndex, c => route[positions[c.OriginalTargetId]].Incoming);
            var impacts = actual.ToDictionary(c => c.DeliveryIndex, c => route[positions[c.OriginalTargetId]].Cell);
            var effects = world.Events.OfType<CombatEffectsEvent>().ToArray();
            var traits = effects.SelectMany(e => e.Traits).Where(t => t.ChildAttack == null &&
                (t.ResultOrigin == null || t.ResultOrigin.AttackGroup == plan.AttackGroup &&
                 t.ResultOrigin.CreditedCharacterId == actor.EntityId)).ToArray();
            var occurrence = new CombatPlayerSkillAttackEvent(AttackResultFactory.CreatePlayerSkill(plan, ledger, 0,
                traits, effects.SelectMany(e => e.Categories).ToArray(), path, incoming, impacts));
            world.Events.Insert(0, occurrence);
            ledger.BindHealthClaims(world.Damage, occurrence);
            return PlayerSkillCalculator.Complete(input, arrival);
        }

        internal static PlayerSkillSelectionResult Selection(CombatCommandInput input, PlayerSkillSourceStamp source)
        {
            var points = input.PlayerSkillRequest?.Points ?? Array.Empty<CellCoord>();
            int required = input.State.PlayerSkill.UpgradeKey == PlayerSkillUpgradeKey.Distortion ? 3 : 2;
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            int range = EnvironmentTerrainRules.AppointRange(input, actor);
            var failure = points.Count > required ? PlayerSkillFailureReason.WrongPointCount : PlayerSkillFailureReason.None;
            for (int i = 0; failure == PlayerSkillFailureReason.None && i < points.Count; i++)
                failure = ValidatePoint(input, walls, actor.Anchor.Value, points, i, points[i], range);
            var candidates = new List<CellCoord>();
            if (failure == PlayerSkillFailureReason.None && points.Count < required)
                for (int y = 0; y < input.State.BoardHeight; y++)
                    for (int x = 0; x < input.State.BoardWidth; x++)
                    {
                        var cell = new CellCoord(x, y);
                        if (ValidatePoint(input, walls, actor.Anchor.Value, points, points.Count, cell, range) == PlayerSkillFailureReason.None)
                            candidates.Add(cell);
                    }
            return new PlayerSkillSelectionResult(source, points, required, candidates, failure);
        }

        private static PlayerSkillFailureReason ValidatePoint(CombatCommandInput input, WallGeometry walls,
            CellCoord origin, IReadOnlyList<CellCoord> points, int index, CellCoord point, int range)
        {
            if (!input.State.IsInside(point)) return PlayerSkillFailureReason.PointOutsideBoard;
            if (walls.Occupies(point)) return PlayerSkillFailureReason.PointOnWall;
            var from = index == 0 ? origin : points[index - 1];
            var delta = point - from;
            if ((delta.X == 0) == (delta.Y == 0)) return PlayerSkillFailureReason.InvalidSegmentDirection;
            if (index > 0)
            {
                var previous = from - (index == 1 ? origin : points[index - 2]);
                if ((long)previous.X * delta.X + (long)previous.Y * delta.Y != 0)
                    return PlayerSkillFailureReason.InvalidSegmentDirection;
            }
            if (Math.Abs((long)delta.X) + Math.Abs((long)delta.Y) > range) return PlayerSkillFailureReason.PointOutOfRange;
            var step = Step(from, point);
            for (var cell = from + step; cell != point; cell += step)
                if (walls.Occupies(cell)) return PlayerSkillFailureReason.SegmentBlockedByWall;
            return PlayerSkillFailureReason.None;
        }

        private static CellCoord Step(CellCoord from, CellCoord to) => new(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

        // 호출자가 같은 선택 검증을 마친 뒤에만 사용한다. 마지막 선택점은 굴절점이 아니라 마지막 구간의 방향이다.
        internal static IEnumerable<(CellCoord Cell, CellCoord Incoming)> Path(CombatCommandInput input)
        {
            var points = input.PlayerSkillRequest.Points;
            var from = input.Characters.Single(c => c.EntityId == input.ActorId).Anchor.Value;
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            for (int i = 0; i < points.Count; i++)
            {
                var step = Step(from, points[i]);
                for (var cell = from + step; input.State.IsInside(cell) && !walls.Occupies(cell); cell += step)
                {
                    yield return (cell, step);
                    if (i < points.Count - 1 && cell == points[i]) break;
                }
                from = points[i];
            }
        }
    }
}
