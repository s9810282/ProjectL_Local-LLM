using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills
{
    internal static class PowderBlossomCalculator
    {
        private const int DamagePercent = 50;
        private static readonly int[][] MaskWidths =
        {
            new[] { 3, 3, 3 },
            new[] { 3, 5, 5, 5, 3 },
            new[] { 3, 5, 7, 7, 7, 5, 3 }
        };

        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            var skill = input.State.PlayerSkill;
            bool condemnation = skill.UpgradeKey == PlayerSkillUpgradeKey.Condemnation;
            bool bloom = skill.UpgradeKey == PlayerSkillUpgradeKey.Bloom;
            var world = new CommandAttackProjection(input, input.State.Environment, null);
            var engine = new CombatEffectEngine();
            var pipeline = new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()));
            MovementProjection arrival = null;
            int expansion = condemnation ? 1 : 0;
            int ordinal = 0;

            while (true)
            {
                var current = arrival?.CurrentInput() ?? input;
                var actor = world.Characters[input.ActorId];
                var profile = CombatCommandCalculator.RequiredProfile(current, actor);
                var selection = Select(current, actor, expansion);
                var scope = new AttackTraitScope(actor, capturedEnemyCount: AttackTraitScope.CountEnemies(actor, selection));
                int eventStart = world.Events.Count;
                TraitAttackNotifications.Apply(current, world, scope);
                actor = world.Characters[input.ActorId];
                int commonDamage = CombatAttackCalculator.CommonAttackDamage(current, actor, profile,
                    CombatDamageKind.PlayerSkillAttack, world.Events, scope);

                var environment = current.State.Environment;
                var walls = WallGeometryBuilder.Build(current.State.BoardWidth, current.State.BoardHeight, environment,
                    current.EnvironmentBindings, current.State.WallBlocking);
                var delivery = new ResolvedAttackDelivery(selection.Targets.Select(t =>
                    new DeliveredAttackHit(t.Key, t.Value, commonDamage, 100, actor.Anchor)),
                    selection.Cells.DesignatedCells.ToArray());
                var plan = new CommandAttackPlan(current, rules, profile, actor, actor, environment, walls,
                    selection, delivery, engine, CombatCommandEffects.Capture(actor, profile), pipeline, commonDamage,
                    actor.Rotation.Value, publicPreview: false, damageKind: CombatDamageKind.PlayerSkillAttack,
                    attackGroup: world.AttackGroups.Next(), traitScope: scope, completeConsequences: true, damagePercent: DamagePercent);

                var ledger = new CommandAttackLedger();
                var recipients = new HashSet<EntityId>();
                arrival = AttackDeliveryExecutor.Execute(plan, world, ledger, recipients, arrival,
                    beforeKillChain: condemnation ? projection => ApplyStun(projection, actor.EntityId, recipients) : null);

                var effects = world.Events.Skip(eventStart).OfType<CombatEffectsEvent>().ToArray();
                var traits = effects.SelectMany(e => e.Traits).Where(t => t.ChildAttack == null &&
                    (t.ResultOrigin == null || t.ResultOrigin.AttackGroup == plan.AttackGroup &&
                     t.ResultOrigin.CreditedCharacterId == actor.EntityId)).ToArray();
                var occurrence = new CombatPlayerSkillAttackEvent(AttackResultFactory.CreatePlayerSkill(plan, ledger,
                    ordinal, traits, effects.SelectMany(e => e.Categories).ToArray()));
                world.Events.Insert(eventStart, occurrence);
                ledger.BindHealthClaims(world.Damage, occurrence);

                if (condemnation || ledger.DirectKilledEnemyIds.Count == 0 || arrival.Terminal ||
                    world.Characters[input.ActorId].Health.IsDead || world.Characters[input.ActorId].Area != CharacterRuntimeArea.Field)
                    break;
                ordinal = checked(ordinal + 1);
                if (bloom && expansion < MaskWidths.Length - 1) expansion++;
            }

            return PlayerSkillCalculator.Complete(input, arrival);
        }

        private static ResolvedAttackSelection Select(CombatCommandInput input, CharacterCombatFacts actor, int expansion)
        {
            var center = actor.Anchor.Value;
            var widths = MaskWidths[expansion];
            var cells = new List<CellCoord>();
            for (int row = 0; row < widths.Length; row++)
            {
                int halfWidth = widths[row] / 2;
                for (int dx = -halfWidth; dx <= halfWidth; dx++)
                {
                    var cell = new CellCoord(center.X + dx, center.Y + row - widths.Length / 2);
                    if (cell != center && input.State.IsInside(cell)) cells.Add(cell);
                }
            }

            var mask = new AttackCellSelection(null, cells.ToArray(), Array.Empty<CellCoord>());
            // 벽은 범위에 영향을 주지 않는다. 기존 피격 자격과 개체별 중복 제거는 그대로 사용한다.
            var policy = new AttackTargetingPolicy(true, false, false, false);
            var characters = input.Characters.Where(target => AttackTargetEligibility.CanTargetCharacter(actor, target, policy)).ToArray();
            var structures = input.State.Structures.Where(target => target.Owner != actor.Owner &&
                AttackTargetEligibility.CanTargetStructure(actor, target,
                    input.EnvironmentBindings.ObstacleEntityIds.Contains(target.EntityId), policy)).ToArray();
            var targets = new AttackTargetResolver().CollectCandidates(mask, characters, structures);
            return new ResolvedAttackSelection(mask, mask, targets, characters, structures, policy);
        }

        private static void ApplyStun(MovementProjection arrival, EntityId actor, IEnumerable<EntityId> recipients)
        {
            var origin = new EffectOriginKey(EffectOwnerKind.PlayerSkill, actor.Value, (int)PlayerSkillKey.PowderBlossom, "condemnation");
            foreach (var recipient in recipients.OrderBy(id => id))
                arrival.ApplyStatus(recipient, new ActionRestrictionStatus(StatusEffectKey.Stun, origin, 1));
        }
    }
}
