using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class MeteorGimmickTests
    {
        private static readonly StageGimmickId Id = new(1);
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void FirstWarningLocksFullFootprintsAtTwoBeforeImpactAtFour()
        {
            var input = World(CombatCommandTestData.Input(), Spec());
            Assert.That(State(input).NextBoundaryCount, Is.EqualTo(2));
            Assert.That(At(input, 1, StageGimmickBoundaryPhase.Warning), Is.Null);
            var warning = At(input, 2, StageGimmickBoundaryPhase.Warning);
            Assert.That(warning.Events.OfType<CombatMeteorEvent>().Single().Step, Is.EqualTo(MeteorGimmickStep.Warning));
            var current = CombatCommandComposition.Project(warning);
            var state = State(current);
            Assert.That(state.Warnings.Count, Is.InRange(2, 4));
            Assert.That(state.Warnings.SelectMany(site => site.Cells).Distinct().Count(), Is.EqualTo(state.Warnings.Count * 9));
            foreach (var site in state.Warnings)
            {
                Assert.That(site.Cells, Is.EquivalentTo(Square(site.Cells[0], 3)));
                Assert.That(site.ImpactBoundaryCount, Is.EqualTo(4));
            }
            var priorRandom = current.State.EnvironmentProgress.RandomState;
            Assert.That(At(current, 3, StageGimmickBoundaryPhase.Scheduled), Is.Null);
            var forecast = StageGimmickComposition.Default.ForecastSteps(current, 4).Where(f => f.HasValue).Select(f => f.Value).ToArray();
            Assert.That(forecast.Select(f => f.Cell.Value), Is.EquivalentTo(state.Warnings.SelectMany(w => w.Cells)));
            Assert.That(forecast.All(f => f.BoundaryCount == 4 && f.Meteor.MaximumHealthDamagePercent == 60), Is.True);
            var impact = At(current, 4, StageGimmickBoundaryPhase.Scheduled);
            Assert.That(impact.Events.OfType<CombatMeteorImpactEvent>().Select(e => e.Footprint), Is.EqualTo(state.Warnings));
            Assert.That(impact.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(priorRandom));
            Assert.That(current.State.EnvironmentProgress.RandomState, Is.EqualTo(priorRandom));
            Assert.That(((MeteorGimmickState)impact.FinalState.Gimmicks.Get(Id)).NextBoundaryCount, Is.EqualTo(6));
        }

        [Test]
        public void FewerThanMinimumSitesSkipsEntireCycle()
        {
            var input = World(CombatCommandTestData.Input(), Spec(side: 8));
            var skipped = At(input, 2, StageGimmickBoundaryPhase.Warning);
            Assert.That(skipped.Events.OfType<CombatMeteorEvent>().Single().Step, Is.EqualTo(MeteorGimmickStep.CycleSkipped));
            var current = CombatCommandComposition.Project(skipped);
            Assert.That(State(current).Warnings, Is.Empty);
            Assert.That(State(current).NextBoundaryCount, Is.EqualTo(6));
            Assert.That(At(current, 4, StageGimmickBoundaryPhase.Scheduled), Is.Null);
        }

        [Test]
        public void SitesNeverIncludeACellOccupiedByAnotherGimmick()
        {
            // 리뷰 Q1: 유닛이 들어갈 수 없는 사슬 머리 칸은 운석 칸이 될 수 없다.
            var head = new CellCoord(4, 4);
            var gimmicks = new StageGimmickStateSet(new StageGimmickState[] { new LavaChainGimmickState(new StageGimmickId(2), head, 3) });
            var blocked = MeteorGimmickRule.BlockedCells(Array.Empty<StructureCombatFacts>(), MovementEnvironmentFacts.Empty,
                EnvironmentSemanticBindings.Empty, gimmicks);

            Assert.That(blocked, Does.Contain(head));
            for (uint seed = 0; seed < 16; seed++)
            {
                uint random = seed;
                var sites = MeteorGimmickRule.Plan(8, 8, blocked, Spec(), 4, ref random);
                Assert.That(sites, Is.Not.Empty);
                Assert.That(sites.SelectMany(site => site.Cells), Has.No.Member(head), "seed " + seed);
            }
        }

        [Test]
        public void AFirstDrawBetweenTwoFeasibleSitesDoesNotSkipTheCycle()
        {
            var input = World(CombatCommandTestData.Input(), new MeteorGimmickSpec(4, 2, 2, 2, 3, 60, 3, 4));
            var walls = Enumerable.Range(0, 8).SelectMany(y => Enumerable.Range(0, 8).Select(x => new CellCoord(x, y)))
                .Where(cell => cell.X >= 6 || cell.Y >= 3)
                .Select((cell, index) => new StaticTerrainFact(index + 1, cell, StaticTerrainKind.Wall, true,
                    new[] { Owner.Player, Owner.Enemy })).ToArray();
            var environment = new MovementEnvironmentFacts(walls);
            var progress = new EnvironmentProgressFacts(1, walls.Length + 1, 123u);
            input = input.WithWorld(input.State.With(environment: environment, environmentProgress: progress), input.Characters, input.Main, input.SoulCounts,
                input.Souls, new EnvironmentSemanticBindings(walls.Select(wall => wall.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()));

            var final = (MeteorGimmickState)At(input, 2, StageGimmickBoundaryPhase.Warning).FinalState.Gimmicks.Get(Id);

            Assert.That(final.Warnings.Select(site => site.Cells[0]), Is.EqualTo(new[] { CellCoord.Zero, new CellCoord(3, 0) }));
        }

        [Test]
        public void InvalidatedSiteIsCancelledWithoutRetargeting()
        {
            var source = CombatCommandTestData.Input();
            var warnings = new[] { Site(4, 1, 0, 2), Site(4, 2, 3, 2) };
            var wall = new StaticTerrainFact(1, warnings[0].Cells[0], StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { wall });
            var input = World(source, Spec(), new MeteorGimmickState(Id, 4, warnings, Array.Empty<MeteorFlameArea>()));
            input = input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                input.Characters, input.Main, input.SoulCounts, input.Souls, new EnvironmentSemanticBindings(new[] { 1L }, Array.Empty<long>(), Array.Empty<EntityId>()));
            var impact = At(input, 4, StageGimmickBoundaryPhase.Scheduled);
            var results = impact.Events.OfType<CombatMeteorImpactEvent>().ToArray();
            Assert.That(results.Select(e => e.Cancelled), Is.EqualTo(new[] { true, false }));
            Assert.That(results.Select(e => e.Footprint), Is.EqualTo(warnings));
            Assert.That(State(CombatCommandComposition.Project(impact)).Flames.Single().Footprint, Is.SameAs(warnings[1]));
            Assert.That(impact.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ImpactHitsCharacterOnceAndAppliesFireEvenWhenShielded(bool shielded)
        {
            var source = CombatCommandTestData.Input();
            var actor = source.Characters.Single(c => c.EntityId == source.ActorId);
            actor = actor.WithPlacement(actor.Area, actor.ReserveSlotIndex, actor.Anchor, actor.Rotation,
                new[] { actor.Anchor.Value, actor.Anchor.Value + new CellCoord(1, 0) });
            if (shielded) actor = actor.WithStatusEffects(new StatusEffectSet(new AppliedStatus[]
                { new ShieldStatus(new EffectOriginKey(EffectOwnerKind.StageGimmick, 99, 99, "fixture"), 80, true, 1) }));
            source = CombatCommandTestData.With(source, characters: source.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray());
            var sites = new[] { Site(4, 1, 0, 2), Site(4, 2, 3, 2) };
            var input = World(source, Spec(), new MeteorGimmickState(Id, 4, sites, Array.Empty<MeteorFlameArea>()));
            var impact = At(input, 4, StageGimmickBoundaryPhase.Scheduled);
            var current = CombatCommandComposition.Project(impact);
            var after = current.Characters.Single(c => c.EntityId == actor.EntityId);
            Assert.That(impact.Events.OfType<CombatMeteorImpactEvent>().First().RecipientIds, Is.EqualTo(new[] { actor.EntityId }));
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(shielded ? 100 : 40));
            if (shielded) Assert.That(after.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.EqualTo(20));
            var fire = after.StatusEffects.Entries.OfType<FireStatus>().Single();
            Assert.That(fire.RemainingAffectedSideStarts, Is.EqualTo(3));
            Assert.That(fire.Origin, Is.EqualTo(sites[0].FireOrigin(Id)));
            Assert.That(State(current).Flames.Select(f => f.ExpiresAtBoundaryCount), Is.EqualTo(new long[] { 8, 8 }));
        }

        [Test]
        public void OwnSideEndRefreshesOnceAcrossOverlapAndExpiryRemovesOnlyDueSources()
        {
            var old = new MeteorFlameArea(Site(4, 1, 0, 2), 8);
            var newer = new MeteorFlameArea(Site(5, 1, 0, 2), 9);
            var input = World(CombatCommandTestData.Input(), Spec(), new MeteorGimmickState(Id, 10, Array.Empty<MeteorFootprint>(), new[] { old, newer }));
            var refreshed = At(input, 7, StageGimmickBoundaryPhase.EndOccupancy);
            var status = refreshed.Events.OfType<CombatTraitStateEvent>().Single();
            Assert.That(status.Final.EntityId, Is.EqualTo(input.ActorId));
            Assert.That(status.Final.StatusEffects.Entries.OfType<FireStatus>().Single().Origin, Is.EqualTo(newer.Footprint.FireOrigin(Id)));
            var expired = At(CombatCommandComposition.Project(refreshed), 8, StageGimmickBoundaryPhase.Expiry);
            Assert.That(State(CombatCommandComposition.Project(expired)).Flames, Is.EqualTo(new[] { newer }));
            Assert.That(expired.Characters.Characters, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EntryFireCanBeUndoneAndRestoresPreviousFire(bool hadFire)
        {
            var source = MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1));
            var previous = new FireStatus(new EffectOriginKey(EffectOwnerKind.Trait, 3, 1, "previous"), 2);
            if (hadFire) source = CombatCommandTestData.With(source, characters: source.Characters.Select(c => c.EntityId == source.ActorId
                ? c.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { previous })) : c).ToArray());
            var flame = new MeteorFlameArea(Site(4, 1, 2, 1), 8);
            var state = new MeteorGimmickState(Id, 6, Array.Empty<MeteorFootprint>(), new[] { flame });
            var input = World(source, Spec(), state);
            var moved = MovementUndoTestData.Run(input);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(MovementTestData.Actor(moved).StatusEffects.Entries.OfType<FireStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(3));
            Assert.That(moved.FinalState.MovementUndo.Checkpoints.Single().Effects.OfType<MovementFireReceipt>().Count(), Is.EqualTo(1));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            var fire = MovementTestData.Actor(undo).StatusEffects.Entries.OfType<FireStatus>().SingleOrDefault();
            if (hadFire) Assert.That(fire.SameValues(previous), Is.True);
            else Assert.That(fire, Is.Null);
        }

        [Test]
        public void UndoRejectsAnOriginWhoseFlameSourceChangedAfterMove()
        {
            var source = MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1));
            var input = World(source, Spec());
            var movement = MovementUndoTestData.Run(input);
            var undo = MovementUndoTestData.Undo(movement);
            var originFlame = new MeteorFlameArea(Site(4, 1, 0, 0), 8);
            var changed = undo.State.Gimmicks.Replace(new MeteorGimmickState(Id, 6, Array.Empty<MeteorFootprint>(), new[] { originFlame }));
            undo = CombatCommandTestData.With(undo, state: undo.State.With(gimmicks: changed));

            var result = MovementUndoTestData.Run(undo);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("DestinationInvalid"));
        }

        private static MeteorGimmickSpec Spec(int side = 3) => new(4, 2, 2, 4, side, 60, 3, 4);
        private static MeteorFootprint Site(int at, int ordinal, int x, int y) => new(at, ordinal, Square(new CellCoord(x, y), 3));
        private static CellCoord[] Square(CellCoord origin, int side) => Enumerable.Range(0, side)
            .SelectMany(y => Enumerable.Range(0, side).Select(x => origin + new CellCoord(x, y))).ToArray();
        private static MeteorGimmickState State(CombatCommandInput input) => (MeteorGimmickState)input.State.Gimmicks.Get(Id);

        private static CombatCommandTransition At(CombatCommandInput input, long count, StageGimmickBoundaryPhase phase)
        {
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.StartingEnemy,
                controllerTurnSequence: 1, completedPlayerTurnCount: count / 2, completedControllerTurnCount: count);
            var ended = CombatCommandTestData.With(input, state: input.State.With(turn: turn, phase: CombatFlowPhase.Automatic));
            return CombatGimmickCalculator.TryAdvance(ended, new StageGimmickBoundary(Owner.Player, count, phase), Rules);
        }

        private static CombatCommandInput World(CombatCommandInput source, MeteorGimmickSpec spec, MeteorGimmickState prepared = null)
        {
            var definitions = new StageGimmickSpec[] { spec };
            var state = source.State.With(environmentProgress: new EnvironmentProgressFacts(1, 1, 123));
            state = prepared == null ? StageGimmickComposition.Initialize(state, definitions) :
                state.With(gimmicks: new StageGimmickStateSet(new StageGimmickState[] { prepared }));
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0, gimmicks: definitions);
            return new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts, source.TargetCell, source.Rotation,
                battle, source.ProcessingKind, source.Path, source.MovementContext, source.BattleInstanceId,
                environmentBindings: source.EnvironmentBindings, expectedBattleInstanceId: source.ExpectedBattleInstanceId,
                expectedControllerTurnSequence: source.ExpectedControllerTurnSequence);
        }
    }
}
