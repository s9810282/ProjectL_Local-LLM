using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class WeakGroundMovementTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord A = CellCoord.Zero;
        private static readonly CellCoord B = CellCoord.Right;
        private static readonly CellCoord C = new CellCoord(2, 0);
        private static readonly CellCoord D = new CellCoord(3, 0);


        // 지나가기만 해도 센다. 한도에 닿은 칸(B)은 명령 끝에 무너지고, D에 멈춘 이동자는 떨어지지 않는다(약한 지반 설계 WG1, 2026-10-27 View 작업에서 사용자 허락으로 기대값 변경).
        [TestCase(0L)]
        [TestCase(1L)]
        public void Movement_CountsEveryEntry_AndCollapsesAtCommandEnd(long count)
        {
            var input = Terrain(Route(A, B, C, D), Ground(71, B, 2, count), Ground(72, C, 5));
            var preview = CombatMovementCalculator.Preview(input, NoRules);
            Assert.That(preview.CanMove, Is.True);
            Assert.That(preview.PublicEffects.OfType<CombatMovementSourceEvent>(), Is.Not.Empty);
            var result = Calculate(input);
            if (count == 0)
            {
                Assert.That(Fact(result, 71).Kind, Is.EqualTo(StaticTerrainKind.WeakGround));
                Assert.That(Fact(result, 71).EntryCount, Is.EqualTo(1));
            }
            else
            {
                Assert.That(Fact(result, 71).Kind, Is.EqualTo(StaticTerrainKind.Cliff));
            }
            Assert.That(Fact(result, 72).EntryCount, Is.EqualTo(1));
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.False);
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
            Assert.That(MovementTestData.Actor(result).Health.IsDead, Is.False);
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        // 미리보기는 보이는 환경만 보고, 실행은 숨은 지반도 센다(보류 전 규칙, 약한 지반 설계 WG1-6, 2026-10-27 사용자 허락으로 기대값 변경).
        [Test]
        public void HiddenGround_PreviewDoesNotRevealOrCountItButExecutionDoes()
        {
            var ground = new StaticTerrainFact(71, B, StaticTerrainKind.WeakGround, true, new[] { Owner.Enemy }, 2);
            var input = Terrain(Route(A, B, C), ground);
            var preview = CombatMovementCalculator.Preview(input, NoRules);
            Assert.That(preview.PublicEffects.OfType<CombatMovementSourceEvent>(), Is.Empty);
            var context = MovementContextCalculator.Capture(input, Owner.Player, NoRules);
            Assert.That(context.VisibleEnvironment.Tiles, Is.Empty);
            Assert.That(MovementContextCalculator.Matches(context, Terrain(input, ground.RegisterEntry()), Owner.Player, NoRules), Is.True);
            var result = Calculate(input);
            Assert.That(Fact(result, 71).SameValues(ground.RegisterEntry()), Is.True);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(C));
        }

        // 넉백·끌어오기 진입도 센다(약한 지반 설계 WG1, Q-WG2 B, 2026-10-27 사용자 허락으로 기대값 변경). 한도 2 미만이라 무너지지 않는다.
        [Test]
        public void ForcedAttackRelocations_CountEveryEntryWithoutChangingAttackerSettlement()
        {
            var seed = CliffFallTests.Knockback();
            var input = Terrain(seed, Ground(71, new CellCoord(5, 3)), Ground(72, new CellCoord(5, 5)));
            var result = Calculate(input);
            var control = Calculate(seed);
            Assert.That(Fact(result, 71).EntryCount, Is.EqualTo(1));
            Assert.That(Fact(result, 72).EntryCount, Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(result).SameValues(MovementTestData.Actor(control)), Is.True);
            Assert.That(result.Events.OfType<CombatMovementSourceEvent>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void RejectedRoute_DoesNotCountValidPrefix()
        {
            var input = Terrain(Route(A, B, C), Ground(71, B), Cliff(72, C));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Events, Is.Empty);
            Assert.That(Fact(result, 71).EntryCount, Is.Zero);
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
        }

        [Test]
        public void Cliff_MovementPreviewRangeAndAiUseSharedLegality()
        {
            var input = Terrain(Route(A, B, C), Cliff(71, B));
            Assert.That(CombatMovementCalculator.Preview(input, NoRules).CanMove, Is.False);
            Assert.That(CombatMovementCalculator.Range(input).Destinations.ToArray().Select(d => d.TargetAnchor), Has.No.Member(B));
            Assert.That(MovementRouteValidator.CanEnter(input, B), Is.False);
            var enemy = Terrain(EnemyMovementPathTests.Input(actorCell: new CellCoord(0, 1), targetCell: new CellCoord(4, 1)),
                Cliff(71, new CellCoord(1, 1)));
            Assert.That(EnemyMovementPathfinder.TryFind(enemy, new EntityId(2), out var path), Is.True);
            Assert.That(path.Cells, Has.No.Member(new CellCoord(1, 1)));
            Calculate(CombatCommandComposition.Intent(enemy, CombatCommandKind.Move, path: path));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerPlacement_RejectsCliffAndDoesNotCountWeakGround(bool fromHand)
        {
            var source = Route(A, B);
            if (fromHand)
                source = CombatCommandTestData.With(source, characters: source.Characters.Select(c => c.EntityId == source.ActorId
                    ? CombatPlacementCalculator.Location(c, CharacterRuntimeArea.Hand, 0, null, null) : c).ToArray());
            var weak = Deployment(Terrain(source, Ground(71, B)), B);
            Assert.That(CombatPlacementCalculator.Preview(weak).CanPlace, Is.True);
            var placed = Calculate(weak);
            Assert.That(Fact(placed, 71).EntryCount, Is.Zero);
            var cliff = Deployment(Terrain(source, Cliff(71, B)), B);
            Assert.That(CombatPlacementCalculator.Preview(cliff).CanPlace, Is.False);
            Assert.That(CombatCommandCalculator.Calculate(cliff, NoRules).Accepted, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnemyRedeployment_ExcludesCliffsAndDoesNotInventAnAvailableCell(bool fullRow)
        {
            var cliffs = (fullRow ? Enumerable.Range(0, 8) : new[] { 2 }).Select(x => Cliff(71 + x, new CellCoord(x, 7))).ToArray();
            var source = Terrain(Route(A, B), cliffs);
            var enemy = CombatPlacementCalculator.Location(CombatCommandTestData.Character(2, EntityKey.Unit_Demon,
                new CellCoord(2, 7), Owner.Enemy), CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var characters = source.Characters.Select(c => c.OwnershipRole == CharacterOwnershipRole.Main
                ? CombatCommandTestData.With(c, position: new CellCoord(7, 4)) : c).Append(enemy).ToArray();
            var s = source.State;
            var state = new CombatCommandState(8,8,s.Phase,s.Structures,new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: new[] {
                    new CombatEnemyOrigin(enemy.EntityId, new CellCoord(2, 7), Rotation4.R0) }),s.Environment);
            var battle = new CombatBattleFacts(8, 42, 7, 1, Array.Empty<CharacterInstanceId>(), 0);
            var input = new CombatCommandInput(1, CombatCommandKind.EnterCombat, source.ActorId, default, state, characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: battle, environmentBindings: source.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
            var projected = characters.ToDictionary(c => c.EntityId);
            var projection = MovementProjection.ForArrival(input, projected, new List<CombatEvent>());
            var allocation = input.State.Allocation;
            Assert.That(CombatPlacementCalculator.RedeployEnemies(input, projected, projection, NoRules, ref allocation), Is.EqualTo(!fullRow));
            Assert.That(projected[enemy.EntityId].Anchor, Is.EqualTo(fullRow ? (CellCoord?)null : new CellCoord(1, 7)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreviewContext_RejectsChangedCountOrKind(bool collapse)
        {
            var ground = Ground(71, B, 1);
            var input = Terrain(Route(A, B, C), ground);
            var expected = MovementContextCalculator.Capture(input, Owner.Player, NoRules);
            Assert.That(MovementContextCalculator.Matches(expected, input, Owner.Player, NoRules), Is.True);
            var changed = ground.RegisterEntry();
            if (collapse) changed = changed.Collapse();
            Assert.That(MovementContextCalculator.Matches(expected, Terrain(input, changed), Owner.Player, NoRules), Is.False);
        }

        [TestCase("identity")]
        [TestCase("cell")]
        [TestCase("threshold")]
        [TestCase("visibility")]
        [TestCase("inactive")]
        [TestCase("skipped-count")]
        [TestCase("early-collapse")]
        public void TerrainReplacement_RejectsReconfigurationWithoutChangingState(string change)
        {
            var ground = Ground(71, B);
            var input = Terrain(Route(A, B), ground);
            var projection = new MovementProjection(input);
            var replacement = change switch
            {
                "identity" => Ground(72, B, count: 1),
                "cell" => Ground(71, C, count: 1),
                "threshold" => Ground(71, B, 5, 1),
                "visibility" => new StaticTerrainFact(71, B, StaticTerrainKind.WeakGround, true, new[] { Owner.Player }, 2, 1),
                "inactive" => new StaticTerrainFact(71, B, StaticTerrainKind.WeakGround, false, ground.KnownTo, 2, 1),
                "skipped-count" => Ground(71, B, count: 2),
                _ => Cliff(71, B)
            };
            Assert.Throws<InvalidOperationException>(() => projection.ChangeTerrain(ground, replacement));
            Assert.That(projection.Environment.SameValues(input.State.Environment), Is.True);
            Assert.That(projection.Events, Is.Empty);
        }

        [Test]
        public void TerrainReplacement_RequiresCurrentFactAndPreservesBindingsOnLegalCollapse()
        {
            var ground = Ground(71, B, 1);
            var input = Terrain(Route(A, B), ground);
            var projection = new MovementProjection(input);
            var entered = ground.RegisterEntry();
            projection.ChangeTerrain(ground, entered);
            Assert.Throws<InvalidOperationException>(() => projection.ChangeTerrain(ground, entered));
            var cliff = entered.Collapse();
            projection.ChangeTerrain(entered, cliff);
            Assert.Throws<InvalidOperationException>(() => projection.ChangeTerrain(cliff, ground));
            Assert.That(projection.Environment.Tiles.Single().SameValues(cliff), Is.True);
            Assert.That(projection.Bindings.SameValues(input.EnvironmentBindings), Is.True);
            Assert.That(projection.Events.OfType<CombatMovementSourceEvent>().Select(e => e.Final), Is.EqualTo(new[] { entered, cliff }));
            Assert.That(ground.EntryCount, Is.Zero);
        }

        private static CombatCommandInput Deployment(CombatCommandInput source, CellCoord target)
        {
            var s = source.State;
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,                s.Structures,new CombatTurnFacts(StagePhase.Deployment),s.Environment);
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0);
            return new CombatCommandInput(1, CombatCommandKind.PlaceUnit, source.ActorId, default, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                target, Rotation4.R0, battle, environmentBindings: source.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }

        private static CombatCommandInput Route(params CellCoord[] cells) => MovementTestData.SafeRoute(cells);
        private static CombatCommandInput Terrain(CombatCommandInput input, params StaticTerrainFact[] terrain)
        {
            var source = StaticTerrainIntegrationTests.Terrain(input, terrain);
            return new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, source.State,
                source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                source.TargetCell, source.Rotation, source.Battle, source.ProcessingKind, source.Path,
                battleInstanceId: input.BattleInstanceId, environmentBindings: source.EnvironmentBindings,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence);
        }
        private static StaticTerrainFact Ground(long id, CellCoord cell, int threshold = 2, long count = 0) =>
            new StaticTerrainFact(id, cell, StaticTerrainKind.WeakGround, true, new[] { Owner.Player, Owner.Enemy }, threshold, count);
        private static StaticTerrainFact Cliff(long id, CellCoord cell) =>
            new StaticTerrainFact(id, cell, StaticTerrainKind.Cliff, true, new[] { Owner.Player, Owner.Enemy });
        private static StaticTerrainFact Fact(CombatCommandTransition transition, long id) =>
            (StaticTerrainFact)transition.FinalState.Environment.Tiles.Single(t => t.Id == id);
        private static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }
    }
}
