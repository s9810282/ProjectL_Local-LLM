using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;

namespace ProjectL.Tests.Core
{
    public sealed class TornadoEnvironmentTests
    {
        internal static readonly IMovementTileRule[] Rules = {
            new ThornMovementTileRule(), new FlagMovementTileRule(), new SheriffBadgeMovementTileRule(), new MineMovementTileRule() };
        internal static TornadoSourceFact Source(long id = 31, CellCoord? cell = null, long due = 3) =>
            new(id, cell ?? new CellCoord(1, 3), new[] { Owner.Player, Owner.Enemy }, due);
        internal static CombatCommandInput Input(long completed = 2, MovementTileFact[] sources = null,
            long night = 0, int nightLength = 1000, CharacterCombatFacts[] characters = null, int width = 8, int height = 8)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            characters ??= seed.Characters.ToArray();
            sources ??= new MovementTileFact[] { Source() };
            var environment = new MovementEnvironmentFacts(sources);
            var turn = new CombatTurnFacts(StagePhase.Combat, currentNightTime: night, completedUnitTurnCount: completed,
                participants: characters.Select(c => new CombatTurnParticipant(c.EntityId, c.Stats.Priority, c.Stats.Delay)).ToArray());
            bool enemy = characters[0].Owner == Owner.Enemy;
            var state = new CombatCommandState(width, height, enemy ? CombatFlowPhase.Automatic : CombatFlowPhase.PlayerInput, characters[0].EntityId,
                true, seed.State.Structures, seed.State.ThornFields, characters.Select(c =>
                    CombatTimelineEntrySnapshot.ForUnit(c.EntityId, c.Stats.Priority, 0, c.EntityId == characters[0].EntityId)).ToArray(), turn, environment);
            return new CombatCommandInput(enemy ? 0 : 1, CombatCommandKind.EndUnitTurn, characters[0].EntityId, default, state,
                characters, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(4, nightLength, 1, width, height, Array.Empty<CharacterInstanceId>(), 0, 100, 0, null),
                processingKind: enemy ? CombatProcessingKind.Advance : CombatProcessingKind.Command,
                environmentBindings: new EnvironmentSemanticBindings(sources.Where(s => s is TornadoSourceFact || s is StaticTerrainFact ||
                    s is DestinationTileModifierFact).Select(s => s.Id).ToArray(), sources.OfType<ObjectSourceFact>().Select(s => s.Id).ToArray(), Array.Empty<EntityId>()));
        }

        [TestCase(0, 3)]
        [TestCase(1, 3)]
        [TestCase(2, 6)]
        public void ThirdCompletedTurn_IsFirstDue_AndEarlierTurnsDoNotRelocate(int completed, int nextDue)
        {
            var i = Input(completed);
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            var source = r.FinalState.Environment.Tiles.OfType<TornadoSourceFact>().Single();
            Assert.That(source.NextDueCompletedTurn, Is.EqualTo(nextDue));
            Assert.That(source.Cell == Source().Cell, Is.EqualTo(completed < 2));
            Assert.That(r.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(completed + 1));
            Assert.That(i.State.Environment.Tiles.Single().SameValues(Source()), Is.True);
        }

        [Test]
        public void EmptySourceCell_StillRespawnsWithoutMovingAUnit()
        {
            var i = Input(sources: new MovementTileFact[] { Source(cell: new CellCoord(2, 2)) });
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.FinalState.Environment.Tiles.Single().Cell, Is.Not.EqualTo(new CellCoord(2, 2)));
            var final = CombatCommandComposition.Project(r);
            Assert.That(final.Characters.Select(c => c.Anchor), Is.EqualTo(i.Characters.Select(c => c.Anchor)));
            Assert.That(r.FinalState.EnvironmentProgress.RandomState, Is.Not.EqualTo(i.State.EnvironmentProgress.RandomState));
            Assert.That(r.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(i.State.EnvironmentProgress.NextSourceId));
        }

        [Test]
        public void RepeatedCalculation_IsDeterministic_AndPublishedDueDoesNotRepeat()
        {
            var i = Input();
            var first = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(first.FinalState.SameValues(CombatCommandCalculator.Calculate(i, Rules).FinalState), Is.True);
            var projected = CombatCommandComposition.Project(first);
            var next = Input(3, projected.State.Environment.Tiles.ToArray(), characters: projected.Characters.ToArray());
            var r = CombatCommandCalculator.Calculate(next, Rules);
            Assert.That(r.FinalState.Environment.SameValues(first.FinalState.Environment), Is.True);
            Assert.That(first.FinalState.Environment.Tiles.OfType<TornadoSourceFact>().Single().NextDueCompletedTurn, Is.EqualTo(6));
        }

        [Test]
        public void NightEndingTurn_ResolvesTornadoBeforeRecall()
        {
            var r = CombatCommandCalculator.Calculate(Input(night: 90, nightLength: 100), Rules);
            Assert.That(r.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(3));
            Assert.That(r.FinalState.Environment.Tiles.OfType<TornadoSourceFact>().Single().NextDueCompletedTurn, Is.EqualTo(6));
            Assert.That(CombatCommandComposition.Project(r).Characters.Single(c => c.EntityId == new EntityId(1)).Area,
                Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(CombatCommandComposition.Project(r).Characters.Single(c => c.EntityId == new EntityId(10)).Area,
                Is.EqualTo(CharacterRuntimeArea.Field));
            var kinds = r.Events.Select(e => e.Kind.ToString()).ToArray();
            Assert.That(Array.IndexOf(kinds, "TornadoActivated"), Is.LessThan(Array.IndexOf(kinds, "NightEnded")));
            Assert.That(Array.IndexOf(kinds, "TornadoActivated"), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void NoCandidate_SkipsRandomButAdvancesDue()
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var actor = CombatMovementCalculator.MoveTo(seed.Characters[0], CellCoord.Zero);
            var main = CombatMovementCalculator.MoveTo(seed.Characters[1], CellCoord.Right);
            var i = Input(sources: new MovementTileFact[] { Source(cell: CellCoord.Zero),
                new StaticTerrainFact(32, CellCoord.Right, StaticTerrainKind.Sandstorm, true, new[] { Owner.Player }) },
                characters: new[] { actor, main }, width: 2, height: 1);
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.FinalState.Environment.Tiles.OfType<TornadoSourceFact>().Single().NextDueCompletedTurn, Is.EqualTo(6));
            Assert.That(r.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(i.State.EnvironmentProgress.RandomState));
            Assert.That(CombatCommandComposition.Project(r).Characters.Select(c => c.Anchor), Is.EqualTo(i.Characters.Select(c => c.Anchor)));
        }

        [Test]
        public void Teleport_ReconcilesPositionBeforeArrivalDamage_AndConsumesObject()
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var actor = CombatMovementCalculator.MoveTo(seed.Characters[0], CellCoord.Zero);
            var main = CombatMovementCalculator.MoveTo(seed.Characters[1], new CellCoord(2, 0));
            var sources = new MovementTileFact[] { Source(cell: CellCoord.Zero),
                new DestinationTileModifierFact(33, CellCoord.Right, true, new[] { Owner.Player }, 7, 10),
                new MineSourceFact(34, CellCoord.Right, 1, 1, true, new[] { Owner.Player }) };
            var i = Input(sources: sources, characters: new[] { actor, main }, width: 3, height: 1);
            var r = CombatCommandCalculator.Calculate(i, Rules);
            var final = CombatCommandComposition.Project(r).Characters.Single(c => c.EntityId == actor.EntityId);
            Assert.That(final.Anchor, Is.EqualTo(CellCoord.Right));
            Assert.That(final.Stats.PositionAttackBonus, Is.EqualTo(7));
            Assert.That(final.Health.MaxHealth, Is.EqualTo(110));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(99));
            Assert.That(r.FinalState.Environment.Tiles.Single(s => s.Id == 34).Active, Is.False);
            Assert.That(final.PendingMoveActionCost, Is.Zero);
            Assert.That(final.MovementCheckpointAnchor, Is.EqualTo(CellCoord.Right));
        }

        [Test]
        public void MixedFactionTurns_AdvanceOneClock()
        {
            var seed = Input(0);
            var enemy = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy);
            var first = CombatCommandCalculator.Calculate(seed, Rules);
            var second = CombatCommandCalculator.Calculate(Input(first.FinalState.Turn.CompletedUnitTurnCount,
                first.FinalState.Environment.Tiles.ToArray(), characters: new[] { enemy }.Concat(seed.Characters).ToArray()), Rules);
            var third = CombatCommandCalculator.Calculate(Input(second.FinalState.Turn.CompletedUnitTurnCount,
                second.FinalState.Environment.Tiles.ToArray()), Rules);
            Assert.That(first.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(1));
            Assert.That(second.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(2));
            Assert.That(third.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(3));
            Assert.That(third.Events.OfType<CombatTornadoEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void QueriesAndMovement_DoNotAdvanceClockOrRandom()
        {
            var i = Input();
            var move = new CombatCommandInput(i.Number, CombatCommandKind.Move, i.ActorId, default, i.State, i.Characters,
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, battle: i.Battle,
                path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) }), environmentBindings: i.EnvironmentBindings);
            Assert.That(CombatMovementCalculator.Preview(move, Rules).CanMove, Is.True);
            CombatTurnCalculator.Forecast(i); CombatTurnCalculator.Forecast(i);
            var committed = CombatCommandCalculator.Calculate(move, Rules);
            Assert.That(committed.Accepted, Is.True, committed.Reason);
            Assert.That(committed.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(2));
            Assert.That(committed.FinalState.Environment.SameValues(i.State.Environment), Is.True);
            Assert.That(committed.FinalState.EnvironmentProgress.SameValues(i.State.EnvironmentProgress), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Forecast_UsesCapturedDueWithoutRandomOrHiddenDestinations(bool endsNight)
        {
            var i = Input(night: endsNight ? 90 : 0, nightLength: endsNight ? 100 : 1000);
            var forecast = CombatTurnCalculator.Forecast(i, 4);
            Assert.That(forecast[0].Kind, Is.EqualTo(CombatTimelineEntryKind.UnitTurn));
            Assert.That(forecast[1].EnvironmentSourceId, Is.EqualTo(31));
            Assert.That(forecast[1].RemainingUnitTurns, Is.EqualTo(1));
            Assert.That(forecast[1].GimmickId, Is.Null);
            if (endsNight) Assert.That(forecast[2].Kind, Is.EqualTo(CombatTimelineEntryKind.NightEnd));
            Assert.That(i.State.EnvironmentProgress.RandomState, Is.EqualTo(1));
            var hidden = Input(sources: new MovementTileFact[] { new TornadoSourceFact(31, new CellCoord(1, 3), new[] { Owner.Enemy }) });
            Assert.That(CombatTurnCalculator.Forecast(hidden).Any(e => e.EnvironmentSourceId.HasValue), Is.False);
        }

        [Test]
        public void EnterNextNight_PreservesGlobalCountAndTornadoDue_ButRailwayStartsRelativeToNewNight()
        {
            var seed = Input(6, new MovementTileFact[] { Source(due: 9) });
            var hq = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100),
                new CellCoord(2, 7), Rotation4.R0, new[] { new CellCoord(2, 7) });
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                new[] { hq }, seed.State.ThornFields, seed.State.Timeline,
                new CombatTurnFacts(StagePhase.Deployment, 2, true, completedUnitTurnCount: 6), seed.State.Environment);
            var battle = new CombatBattleFacts(4, 1000, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 100, 0, null,
                gimmicks: new StageGimmickSpec[] { new RailwayGimmickSpec(3, 3) });
            var i = new CombatCommandInput(1, CombatCommandKind.EnterCombat, EntityId.Empty, default, state,
                seed.Characters, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: battle, environmentBindings: seed.EnvironmentBindings);
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(6));
            Assert.That(r.FinalState.Turn.Gimmicks.Single().NextActivationTurn, Is.EqualTo(9));
            Assert.That(r.FinalState.Environment.SameValues(seed.State.Environment), Is.True);
        }

        [Test]
        public void SimultaneousSources_UseSourceIdOrderAndCurrentOccupancy()
        {
            var r = CombatCommandCalculator.Calculate(Input(sources: new MovementTileFact[] {
                Source(32, new CellCoord(2, 2)), Source(31, new CellCoord(3, 3)) }), Rules);
            Assert.That(r.Events.OfType<CombatTornadoEvent>().Select(e => e.Expected.Id), Is.EqualTo(new[] { 31L, 32L }));
            Assert.That(r.FinalState.Environment.Tiles.Select(s => s.Cell).Distinct().Count(), Is.EqualTo(2));
            Assert.That(r.FinalState.Environment.Tiles.OfType<TornadoSourceFact>().All(t => t.NextDueCompletedTurn == 6), Is.True);
        }

        [Test]
        public void Candidates_SeparateUnitTerrainAndSourceRules_RespectAllLivingStructures()
        {
            var i = Input(sources: new MovementTileFact[] { Source(),
                new StaticTerrainFact(32, new CellCoord(2, 2), StaticTerrainKind.HighGround, true, new[] { Owner.Player }),
                new SheriffBadgeSourceFact(33, new CellCoord(3, 2), 1, true, new[] { Owner.Player }) });
            var structure = new StructureCombatFacts(new EntityId(50), new StructureSpec(EntityKey.Structure_Headquarters,
                2, 1, new[] { CellCoord.Zero, CellCoord.Right }, 100, false, 0), new HealthStateSnapshot(100, 100),
                new CellCoord(4, 2), Rotation4.R0, new[] { new CellCoord(4, 2), new CellCoord(5, 2) });
            var s = i.State;
            var state = new CombatCommandState(8, 8, s.Phase, s.ActiveActorId, true, new[] { structure }, s.ThornFields,
                s.Timeline, s.Turn, s.Environment, s.EnvironmentProgress);
            i = CombatCommandTestData.With(i, state: state);
            var unit = TornadoCalculator.UnitCandidates(i, Source().Cell);
            var source = TornadoCalculator.SourceCandidates(i, Source());
            Assert.That(unit, Has.Member(new CellCoord(2, 2)));
            Assert.That(source, Has.No.Member(new CellCoord(2, 2)));
            Assert.That(unit, Has.Member(new CellCoord(3, 2)));
            Assert.That(source, Has.Member(new CellCoord(3, 2)));
            Assert.That(unit, Has.No.Member(new CellCoord(7, 7)));
            Assert.That(source, Has.Member(new CellCoord(7, 7)));
            foreach (var cell in structure.OccupiedCells) { Assert.That(unit, Has.No.Member(cell)); Assert.That(source, Has.No.Member(cell)); }
        }

        [Test]
        public void RespawnUnderUnit_DoesNotTriggerAnExtraArrivalOrTeleport()
        {
            var seed = Input();
            var actor = CombatMovementCalculator.MoveTo(seed.Characters[0], CellCoord.Right);
            var main = CombatMovementCalculator.MoveTo(seed.Characters[1], new CellCoord(2, 0));
            var r = CombatCommandCalculator.Calculate(Input(sources: new MovementTileFact[] { Source(cell: CellCoord.Zero) },
                characters: new[] { actor, main }, width: 3, height: 1), Rules);
            Assert.That(r.FinalState.Environment.Tiles.Single().Cell, Is.EqualTo(actor.Anchor));
            Assert.That(r.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated), Is.Empty);
            Assert.That(CombatCommandComposition.Project(r).Characters.Select(c => c.Anchor), Is.EqualTo(new[] { actor.Anchor, main.Anchor }));
        }

        [Test]
        public void RelocationDeath_DropsHeldFlagOnce_AndKeepsLaterSourceProcessing()
        {
            var seed = Input();
            var actor = HeldEnvironmentEffects.AcquireFlag(CombatMovementCalculator.MoveTo(seed.Characters[0], CellCoord.Zero),
                new FlagSourceFact(500, CellCoord.Zero, 1, 7, true, new[] { Owner.Player }));
            var main = CombatMovementCalculator.MoveTo(seed.Characters[1], new CellCoord(3, 0));
            var i = Input(sources: new MovementTileFact[] { Source(cell: CellCoord.Zero),
                new MineSourceFact(34, CellCoord.Right, 1, 100, true, new[] { Owner.Player }) },
                characters: new[] { actor, main }, width: 4, height: 1);
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Outcome.IsStageEnded, Is.False);
            Assert.That(CombatCommandComposition.Project(r).Characters.Single(c => c.EntityId == actor.EntityId).Health.IsDead, Is.True);
            Assert.That(r.Events.OfType<CombatObjectCreatedEvent>().Count(), Is.EqualTo(1));
            Assert.That(r.FinalState.Environment.Tiles.OfType<FlagSourceFact>().Single().Id, Is.EqualTo(35));
            Assert.That(r.FinalState.Environment.Tiles.OfType<TornadoSourceFact>().Single().NextDueCompletedTurn, Is.EqualTo(6));
        }

        [Test]
        public void RandomState_AdvancesOncePerMultiCandidateChoice()
        {
            // 유닛 이동(후보 여러 개) → 지뢰 사망 → 사망 칸 깃발 드롭(후보 하나, 추첨 없음) → 재생성(후보 여러 개)
            var seed = Input();
            var actor = HeldEnvironmentEffects.AcquireFlag(CombatMovementCalculator.MoveTo(seed.Characters[0], CellCoord.Zero),
                new FlagSourceFact(500, CellCoord.Zero, 1, 7, true, new[] { Owner.Player }));
            var main = CombatMovementCalculator.MoveTo(seed.Characters[1], new CellCoord(3, 0));
            var i = Input(sources: new MovementTileFact[] { Source(cell: CellCoord.Zero),
                new MineSourceFact(34, CellCoord.Right, 1, 100, true, new[] { Owner.Player }) },
                characters: new[] { actor, main }, width: 4, height: 1);
            int unitChoices = TornadoCalculator.UnitCandidates(i, CellCoord.Zero).Count;
            int respawnChoices = TornadoCalculator.SourceCandidates(i, Source(cell: CellCoord.Zero)).Count;
            Assert.That(unitChoices, Is.EqualTo(2));
            Assert.That(respawnChoices, Is.EqualTo(3));

            var r = CombatCommandCalculator.Calculate(i, Rules);

            uint expected = i.State.EnvironmentProgress.RandomState;
            EnvironmentRandom.NextIndex(ref expected, unitChoices);
            EnvironmentRandom.NextIndex(ref expected, respawnChoices);
            Assert.That(r.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(expected));
            Assert.That(r.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
            Assert.That(r.Events.OfType<CombatObjectCreatedEvent>().Single().Source.Cell, Is.EqualTo(CellCoord.Right));
            Assert.That(r.Events.OfType<CombatTornadoEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void TerminalArrival_StopsRemainingTornadosAndDoesNotRespawn()
        {
            var seed = Input();
            var main = CombatMovementCalculator.MoveTo(seed.Characters[1], CellCoord.Zero);
            var actor = CombatMovementCalculator.MoveTo(seed.Characters[0], new CellCoord(2, 0));
            var i = Input(sources: new MovementTileFact[] { Source(31, CellCoord.Zero), Source(32, new CellCoord(2, 0)),
                new MineSourceFact(33, CellCoord.Right, 1, 100, true, new[] { Owner.Player }) },
                characters: new[] { actor, main }, width: 3, height: 1);
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Outcome.IsStageEnded, Is.True);
            Assert.That(r.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(r.FinalState.Environment.Tiles.OfType<TornadoSourceFact>().All(t => t.NextDueCompletedTurn == 3), Is.True);
            Assert.That(r.Events.OfType<CombatTornadoEvent>(), Is.Empty);
            Assert.That(r.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
        }

        [Test]
        public void SourceVisibilityCopy_RetainsDueAndOwnsItsVisibility()
        {
            var owners = new[] { Owner.Player };
            var source = new TornadoSourceFact(1, CellCoord.Zero, owners, 6);
            owners[0] = Owner.Enemy;
            Assert.That(source.KnownTo, Is.EqualTo(new[] { Owner.Player }));
            var visible = (TornadoSourceFact)source.WithKnownTo(new[] { Owner.Enemy });
            Assert.That(visible.NextDueCompletedTurn, Is.EqualTo(6));
            Assert.That(source.AfterActivation(CellCoord.Right).NextDueCompletedTurn, Is.EqualTo(9));
        }
    }
}
