using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class TornadoGimmickResolutionTests
    {
        internal static readonly StageGimmickId Id = new StageGimmickId(1);
        internal static readonly Owner[] Both = { Owner.Player, Owner.Enemy };

        [TestCase(StagePhase.Deployment, 0)]
        [TestCase(StagePhase.Combat, 1)]
        public void EntryUsesOnlyOngoingCombatAndDoesNotReschedule(StagePhase phase, int throws)
        {
            var input = Input(phase: phase);
            var projection = new MovementProjection(input);
            TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 3), TornadoEnvironmentTests.Rules);
            var result = projection.Complete();
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(throws));
            Assert.That(result.FinalState.Gimmicks.SameValues(input.State.Gimmicks), Is.True);
            Assert.That(result.FinalState.EnvironmentProgress.RandomState == input.State.EnvironmentProgress.RandomState, Is.EqualTo(throws == 0));
            Assert.That(input.Characters.Single(c => c.EntityId == new EntityId(1)).Anchor, Is.EqualTo(new CellCoord(1, 3)));
        }

        [Test]
        public void ActivationAndIdleReadDoNotThrowButActualCoveredStepDoes()
        {
            var input = Input();
            var projection = new MovementProjection(input);
            projection.CurrentInput();
            Assert.That(projection.Events, Is.Empty);
            projection.Enter(new CellCoord(2, 3));
            TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(2, 3), TornadoEnvironmentTests.Rules);
            Assert.That(projection.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated).Expected.Anchor,
                Is.EqualTo(new CellCoord(2, 3)));
        }

        [Test]
        public void WarningPrecedesMatchingSweepAndPreservesTurnFacts()
        {
            var input = Input(count: 1);
            var result = Advance(input, 1);
            var group = (TornadoGimmickState)result.FinalState.Gimmicks.Get(Id);
            Assert.That(group.Members[0].Phase, Is.EqualTo(TornadoGimmickPhase.Warning));
            Assert.That(group.Members[0].NextBoundaryCount, Is.EqualTo(2));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated), Is.Empty);
            Assert.That(result.FinalState.Turn.SameValues(input.State.Turn), Is.True);
            Assert.That(result.Events.OfType<CombatTornadoGimmickEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void FinishedInputCannotStartContactOrSpendRandomness()
        {
            var input = Input();
            input = input.WithWorld(input.State.With(phase: CombatFlowPhase.Finished), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var projection = new MovementProjection(input);
            TornadoCalculator.ResolveGroupedEntry(projection, input.ActorId, input.Path.Origin, TornadoEnvironmentTests.Rules);
            Assert.That(projection.Events, Is.Empty);
            Assert.That(projection.CurrentInput().State.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        [TestCase(1, 1, 0)]
        [TestCase(3, 4, 1)]
        [TestCase(6, 6, 3)]
        public void SweepThrowsAtFirstTouchAndPublishesEveryReachedCenter(int x, int y, int step)
        {
            var input = Input(center: new CellCoord(2, 2), warned: true, victim: new CellCoord(x, y));
            var result = Advance(input, 2);
            var relocation = result.Events.OfType<CombatCharacterEvent>().First(e => e.Kind == CombatEventKind.Relocated && e.Final.EntityId == new EntityId(1));
            Assert.That(relocation.TornadoContact.StepIndex, Is.EqualTo(step));
            Assert.That(relocation.TornadoContact.Center, Is.EqualTo(new CellCoord(2 + step, 2 + step)));
            Assert.That(result.Events.OfType<CombatTornadoCenterEvent>().Select(e => e.Center),
                Is.EqualTo(new[] { new CellCoord(2, 2), new CellCoord(3, 3), new CellCoord(4, 4), new CellCoord(5, 5) }));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated && e.Final.EntityId == new EntityId(1)), Is.EqualTo(1));
        }

        [Test]
        public void SameStormLandingAndLaterCoveredStepsThrowOnceButIndependentEntryCanThrowAgain()
        {
            var input = Input(width: 3, height: 5, center: new CellCoord(1, 2), victim: new CellCoord(1, 2),
                sources: WallsExcept(3, 5, new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(2, 4)));
            var projection = new MovementProjection(input);
            TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 2), TornadoEnvironmentTests.Rules);
            Assert.That(projection.Character(new EntityId(1)).Anchor, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(projection.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
            TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 1), TornadoEnvironmentTests.Rules);
            Assert.That(projection.Character(new EntityId(1)).Anchor, Is.EqualTo(new CellCoord(1, 2)));
            Assert.That(projection.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(2));
            Assert.That(projection.CurrentInput().State.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(projection.Complete()));
        }

        [Test]
        public void OverlappingSourcesTerminateAtoBtoAAndRetainSourceOrder()
        {
            var input = Input(width: 3, height: 5, center: new CellCoord(1, 2), victim: new CellCoord(1, 2), multiple: true,
                sources: WallsExcept(3, 5, new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(2, 4)));
            var projection = new MovementProjection(input);
            TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 2), TornadoEnvironmentTests.Rules);
            Assert.That(projection.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated).Select(e => e.TornadoSourceId),
                Is.EqualTo(new long?[] { 31, 32 }));
            Assert.That(projection.Character(new EntityId(1)).Anchor, Is.EqualTo(new CellCoord(1, 2)));
        }

        [Test]
        public void LandingUsesCurrentOccupancyAndExcludesCliffsWallsAndDeparture()
        {
            var input = Input(sources: new MovementTileFact[] {
                new StaticTerrainFact(100, new CellCoord(0, 0), StaticTerrainKind.Wall, true, Both),
                new StaticTerrainFact(101, new CellCoord(1, 0), StaticTerrainKind.Cliff, true, Both) });
            var candidates = TornadoCalculator.GroupedLandingCandidates(input, new EntityId(1));
            Assert.That(candidates.Take(3), Is.EqualTo(new[] { new CellCoord(2, 0), new CellCoord(3, 0), new CellCoord(4, 0) }));
            Assert.That(candidates.Contains(new CellCoord(1, 3)) || candidates.Contains(new CellCoord(7, 7)), Is.False);
            var projection = new MovementProjection(input);
            TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 3), TornadoEnvironmentTests.Rules);
            Assert.That(TornadoCalculator.GroupedLandingCandidates(projection.CurrentInput(), new EntityId(10)).Contains(new CellCoord(1, 3)), Is.True);
            Assert.That(TornadoCalculator.GroupedLandingCandidates(projection.CurrentInput(), new EntityId(10)).Contains(projection.Character(new EntityId(1)).Anchor.Value), Is.False);
        }

        [Test]
        public void NoLegalLandingDoesNotMoveOrDraw()
        {
            var input = Input(sources: WallsExcept(8, 8, new CellCoord(1, 3), new CellCoord(7, 7)));
            var projection = new MovementProjection(input);
            TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 3), TornadoEnvironmentTests.Rules);
            Assert.That(projection.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated), Is.Empty);
            Assert.That(projection.CurrentInput().State.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ForcedMovementImmunityPrecedesMulticellSupportBoundary(bool immune)
        {
            var input = Input();
            var profiles = input.Profiles.Select(p =>
            {
                if (p.Definition.Key != EntityKey.Unit_Akimbo) return p;
                var d = p.Definition;
                return new CombatAttackProfile(new UnitSpec(d.Key, d.Role, d.FootprintWidth, d.FootprintHeight,
                    d.FootprintLocalCells.ToArray(), d.MaxHealth, d.MoveMethod, d.AttackRange, d.CategoryKey,
                    d.AttackPower, d.PhysicalDefense, d.Priority, d.Traits.ToArray(), d.OwnerEligibility,
                    behavior: new UnitBehaviorSpec(true, true, immune)), p.Method);
            }).ToArray();
            var characters = input.Characters.Select(c => c.EntityId != new EntityId(1) ? c :
                c.WithPlacement(c.Area, c.ReserveSlotIndex, c.Anchor, c.Rotation, new[] { c.Anchor.Value, c.Anchor.Value + CellCoord.Right })).ToArray();
            input = CombatCommandTestData.With(input, profiles: profiles);
            input = input.WithWorld(input.State, characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var projection = new MovementProjection(input);
            if (immune)
            {
                Assert.DoesNotThrow(() => TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 3), TornadoEnvironmentTests.Rules));
                Assert.That(projection.Events.OfType<CombatCharacterEvent>(), Is.Empty);
                Assert.That(projection.CurrentInput().State.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
            }
            else Assert.Throws<NotSupportedException>(() => TornadoCalculator.ResolveGroupedEntry(projection, new EntityId(1), new CellCoord(1, 3), TornadoEnvironmentTests.Rules));
        }

        [Test]
        public void LandingAheadOfStormDoesNotRepeatTheThrow()
        {
            var input = Input(center: new CellCoord(2, 2), warned: true, victim: new CellCoord(1, 1),
                sources: WallsExcept(8, 8, new CellCoord(1, 1), new CellCoord(4, 4), new CellCoord(7, 7)));
            var result = Advance(input, 2);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(c => c.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(1)).Final.Anchor, Is.EqualTo(new CellCoord(4, 4)));
        }

        [Test]
        public void SweepRecipientsAreUniqueLivingFieldUnitsInEntityOrder()
        {
            var input = Input(center: new CellCoord(2, 2), warned: true, victim: new CellCoord(1, 1));
            var characters = input.Characters.Concat(new[] {
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(1, 2), Owner.Enemy),
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy) }).ToArray();
            input = input.WithWorld(input.State, characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var result = Advance(input, 2);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(c => c.Kind == CombatEventKind.Relocated).Take(3).Select(c => c.Final.EntityId.Value),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void NestedTilePushIntoAnotherStormInterruptsRemainingStepsAndSettlesAtActualAnchor()
        {
            var allowed = new[] { new CellCoord(1, 1), new CellCoord(3, 1), new CellCoord(3, 2),
                new CellCoord(3, 3), new CellCoord(3, 4), new CellCoord(5, 5) };
            var sources = WallsExcept(6, 6, allowed).Append((MovementTileFact)new PushTile()).ToArray();
            var input = Input(width: 6, height: 6, center: new CellCoord(1, 1), victim: new CellCoord(1, 1),
                multiple: true, secondCenter: new CellCoord(4, 3), sources: sources);
            var projection = new MovementProjection(input);
            var rules = TornadoEnvironmentTests.Rules.Append(new PushRule()).ToArray();
            TornadoCalculator.ResolveGroupedEntry(projection, input.ActorId, input.Path.Origin, rules);
            Assert.That(projection.Events.OfType<CombatCharacterEvent>().Where(c => c.Kind == CombatEventKind.Relocated).Select(c => c.TornadoSourceId),
                Is.EqualTo(new long?[] { 31, 32 }));
            Assert.That(projection.Events.OfType<CombatCharacterEvent>().Where(c => c.Kind == CombatEventKind.Moved).Select(c => c.Final.Anchor.Value),
                Is.EqualTo(new[] { new CellCoord(3, 2) }));
            Assert.That(projection.Character(input.ActorId).Anchor, Is.EqualTo(new CellCoord(3, 4)));
            Assert.That(projection.ArrivalCells[input.ActorId], Is.EqualTo(new CellCoord(3, 4)));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(projection.Complete()));
        }

        [Test]
        public void ReturningToHistoricalArrivalAnchorCollectsNewlyAvailableSoul()
        {
            var destination = new CellCoord(1, 1);
            var input = Input(width: 3, height: 5, center: new CellCoord(1, 2), victim: new CellCoord(1, 2),
                sources: WallsExcept(3, 5, destination, new CellCoord(1, 2), new CellCoord(2, 4)));
            var field = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Trait, 7, (int)TraitKey.CallOfBlood, "arrival-regression"), destination, 1, 1) });
            input = input.WithWorld(input.State, input.Characters, input.Main, input.SoulCounts, field, input.EnvironmentBindings);
            var projection = new MovementProjection(input);
            // 앞선 착지 뒤 이동만 진행된 상태를 표현한다. 이 과거 의존 기록이 새 착지의 영혼 수집을 막아서는 안 된다.
            projection.ArrivalCells[input.ActorId] = destination;
            TornadoCalculator.ResolveGroupedEntry(projection, input.ActorId, input.Path.Origin, TornadoEnvironmentTests.Rules);
            Assert.That(projection.Character(input.ActorId).Anchor, Is.EqualTo(destination));
            Assert.That(projection.Character(input.ActorId).Stats.SoulAttackBonus, Is.EqualTo(1));
            Assert.That(projection.CurrentInput().Souls.Entries, Is.Empty);
            Assert.That(projection.Events.OfType<CombatSoulEvent>().Count(e => e.Kind == CombatEventKind.SoulsCollected), Is.EqualTo(1));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(projection.Complete()));
        }

        [Test]
        public void AcceptedSideEndPassesItsInjectedLandingRulesToScheduledStorm()
        {
            var allowed = new[] { new CellCoord(1, 1), new CellCoord(3, 1), new CellCoord(3, 2),
                new CellCoord(3, 3), new CellCoord(3, 4), new CellCoord(5, 5) };
            var input = Input(count: 1, width: 6, height: 6, center: new CellCoord(1, 1), victim: new CellCoord(1, 1),
                warned: true, multiple: true, secondCenter: new CellCoord(4, 3),
                sources: WallsExcept(6, 6, allowed).Append((MovementTileFact)new PushTile()).ToArray());
            var group = (TornadoGimmickState)input.State.Gimmicks.Get(Id);
            var second = group.Members[1];
            var final = new TornadoGimmickState(Id, new[] { group.Members[0],
                new TornadoMemberState(second.SourceId, second.Center, -1, TornadoGimmickPhase.Cooldown, 9, null) });
            input = input.WithWorld(input.State.With(gimmicks: input.State.Gimmicks.Replace(final)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var result = CombatTurnCalculator.EndPlayerTurn(input, TornadoEnvironmentTests.Rules.Append(new PushRule()).ToArray());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(2));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).Select(e => e.Final.Anchor.Value),
                Does.Contain(new CellCoord(3, 2)));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated).Take(2).Select(e => e.TornadoSourceId),
                Is.EqualTo(new long?[] { 31, 32 }));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }
        internal static CombatCommandTransition Advance(CombatCommandInput input, long count) =>
            new TornadoGimmickRule(TornadoEnvironmentTests.Rules).TryAdvance(input, input.Battle.GimmickDefinitions.Single(), Id,
                new StageGimmickBoundary(Owner.Player, count));

        internal static CombatCommandInput Input(StagePhase phase = StagePhase.Combat, long count = 2, CellCoord? center = null,
            bool warned = false, CellCoord? victim = null, int width = 8, int height = 8, bool multiple = false,
            MovementTileFact[] sources = null, CellCoord? secondCenter = null)
        {
            var source = CombatCommandTestData.Input(empty: true);
            var characters = source.Characters.Select(c => CombatCommandTestData.With(c,
                position: c.EntityId == new EntityId(1) ? victim ?? new CellCoord(1, 3) : new CellCoord(width - 1, height - 1))).ToArray();
            var members = new[] { new TornadoMemberSpec(31, Both, center ?? new CellCoord(1, 3), 1) }.ToList();
            if (multiple) members.Add(new TornadoMemberSpec(32, Both, secondCenter ?? center ?? new CellCoord(1, 3), 1));
            var spec = new TornadoGimmickSpec(members);
            var group = new TornadoGimmickState(Id, members.Select(m => new TornadoMemberState(m.SourceId, m.InitialCenter.Value, 1,
                warned ? TornadoGimmickPhase.Warning : TornadoGimmickPhase.Cooldown, warned ? 2 : 1,
                warned ? new CellCoord(1, 1) : (CellCoord?)null)).ToArray());
            var turn = new CombatTurnFacts(phase, controllerPhase: phase == StagePhase.Combat ? CombatControllerPhase.Player : CombatControllerPhase.None,
                controllerTurnSequence: 1, completedControllerTurnCount: count);
            var environment = new MovementEnvironmentFacts(sources ?? Array.Empty<MovementTileFact>());
            var state = new CombatCommandState(width, height, CombatFlowPhase.PlayerInput, true,
                source.State.Structures, source.State.ThornFields, turn, environment, gimmicks: new StageGimmickStateSet(new[] { group }));
            var battle = new CombatBattleFacts(4, 1, width, height, Array.Empty<CharacterInstanceId>(), 0,
                gimmicks: new[] { spec });
            return new CombatCommandInput(1, source.Kind, source.ActorId, source.Aim, state, characters, source.Profiles, source.Main,
                source.Owned, source.SoulCounts, source.Souls, source.Artifacts, path: new MovementPath(new[] { characters[0].Anchor.Value }),
                battle: battle, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1,
                environmentBindings: new EnvironmentSemanticBindings(environment.Tiles.Where(t => t is StaticTerrainFact || t is DestinationTileModifierFact).Select(t => t.Id).ToArray(),
                    environment.Tiles.OfType<ObjectSourceFact>().Select(t => t.Id).ToArray(), Array.Empty<EntityId>()));
        }

        internal static MovementTileFact[] WallsExcept(int width, int height, params CellCoord[] allowed) =>
            Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x => new CellCoord(x, y)))
                .Where(c => !allowed.Contains(c)).Select((c, i) => (MovementTileFact)new StaticTerrainFact(100 + i, c, StaticTerrainKind.Wall, true, Both)).ToArray();

        internal sealed class PushTile : MovementTileFact
        {
            internal PushTile() : base(900, new CellCoord(3, 1), "tornado-test-push", 0, true, Both) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new PushTile();
        }

        internal sealed class PushRule : IMovementTileRule
        {
            public string Key => "tornado-test-push";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => new MovementStepEffects(
                forcedMovement: input.Phase == MovementEffectPhase.Hazard ? new[] { new MovementForcedMoveRequest(input.MoverId, Rotation4.R0, 3) } : null);
        }
    }
}
