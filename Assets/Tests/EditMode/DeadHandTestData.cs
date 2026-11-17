using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    internal static class DeadHandTestData
    {
        internal static CombatCommandInput Input(CellCoord[] targets = null, AttackSweepSide? side = null,
            bool active = false, int percent = 100, int power = 6, TargetingMethodSpec method = null, AttackAim? aim = null)
        {
            method ??= new TargetingMethodSpec(TargetingShape.ForwardRectangle, TargetingChoiceMode.Direction,
                TargetingSplash.None, 0, width: 3);
            var source = CombatCommandTestData.Input(method, aim ?? AttackAim.ForDirection(AttackDirection.Up), empty: true);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo,
                new CellCoord(3, 1), Owner.Player, attack: power), appointRange: 2,
                traits: active ? new TraitStatusSnapshot[] { new DeadHandTraitStatusSnapshot() } : Array.Empty<TraitStatusSnapshot>());
            var main = CombatCommandTestData.With(source.Characters[1], position: CellCoord.Zero);
            var characters = new[] { actor, main }.Concat((targets ?? new[] { new CellCoord(3, 2) })
                .Select((cell, index) => CombatCommandTestData.Character(index + 2, EntityKey.Unit_Demon, cell, Owner.Enemy))).ToArray();
            var profile = new CombatAttackProfile(UnitSpecTestFactory.Create(EntityKey.Unit_Akimbo, 1, 1,
                new[] { CellCoord.Zero }, 100, MoveMethod.LiftAndPlace, 2, power, 100,
                active ? new TraitSpec[] { new DeadHandTraitSpec(percent) } : Array.Empty<TraitSpec>(), categoryKey: CategoryKey.Main), method);
            var input = CombatCommandTestData.With(source, characters,
                profiles: source.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo ? profile : p).ToArray());
            return input.WithIntent(input.Kind, input.Aim, input.TargetCell, input.Path, input.MovementContext, side);
        }

        internal static (DeadHandAttackEvaluation Evaluation, CommandAttackPlan Plan, CommandAttackProjection World,
            CommandAttackLedger Ledger, MovementProjection Arrival) Prepare(CombatCommandInput input, IMovementTileRule[] rules = null)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var selection = CombatAttackQueries.Selection(input, actor, profile).Selection;
            var contribution = new EffectAttackSweepContribution(TraitEffectOrigin.Create(actor.EntityId, TraitKey.DeadHand), 100);
            var evaluation = DeadHandAttackCalculator.Prepare(input, actor, profile, selection, contribution);
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            var world = new CommandAttackProjection(input, input.State.Environment, Array.Empty<AttackBlockedContact>());
            var plan = new CommandAttackPlan(input, rules ?? Array.Empty<IMovementTileRule>(), profile, actor, actor,
                input.State.Environment, walls, selection, new ResolvedAttackDelivery(Array.Empty<DeliveredAttackHit>(), Array.Empty<CellCoord>()),
                new CombatEffectEngine(), CombatCommandEffects.Capture(actor, profile),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())), 6,
                Rotation4.R0, false);
            var ledger = new CommandAttackLedger();
            ledger.BeginScope(plan, world);
            return (evaluation, plan, world, ledger, world.CreateArrivalProjection(plan));
        }

        internal static (AttackSweepSnapshot Sweep, CommandAttackProjection World) Sweep(CombatCommandInput input, IMovementTileRule[] rules = null)
        {
            var work = Prepare(input, rules);
            AttackSweepSnapshot sweep = null;
            foreach (var _ in DeadHandAttackCalculator.ExecuteSteps(work.Evaluation, work.Plan, work.World, work.Ledger,
                work.Arrival, value => sweep = value)) { }
            return (sweep, work.World);
        }
    }
}
