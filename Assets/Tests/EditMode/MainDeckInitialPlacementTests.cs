using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class MainDeckInitialPlacementTests
    {
        [TestCase(0, 0)]
        [TestCase(2, 2)]
        [TestCase(4, 3)]
        [TestCase(6, 3)]
        public void Preparation_AlwaysDealsMainAndUpToThreeSubs_WithoutDrawingMain(int subs, int drawn)
        {
            var (initialization, roster) = Setup(subs);
            var draw = Draw(roster);
            var prepared = Prepare(initialization, roster, draw);
            var main = prepared.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);

            Assert.That(main.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(main.ReserveSlotIndex, Is.Zero);
            Assert.That(main.Anchor, Is.Null);
            Assert.That(main.Rotation, Is.Null);
            Assert.That(main.OccupiedCells, Is.Empty);
            Assert.That(main.Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(draw.InitialCount, Is.EqualTo(drawn));
            Assert.That(prepared.Characters.Count, Is.EqualTo(1 + drawn));
            Assert.That(prepared.Characters.Select(c => c.ReserveSlotIndex.Value), Is.EqualTo(Enumerable.Range(0, 1 + drawn)));
            Assert.That(prepared.Characters.Skip(1).Select(c => c.OwnedId.Value), Is.EqualTo(draw.Order.Take(drawn)));
            Assert.That(draw.Order, Has.No.Member(main.OwnedId.Value));
            Assert.That(prepared.Combat.DrawCursor, Is.EqualTo(drawn));
        }

        [TestCase(-1, 0)]
        [TestCase(0, 5)]
        public void Preparation_UnplacedMainDoesNotUseLegacyStartingCell(int x, int y)
        {
            var (initialization, roster) = Setup(0, anchor: new CellCoord(x, y));
            var prepared = Prepare(initialization, roster, Draw(roster));
            Assert.That(prepared.Characters.Single().Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(prepared.Characters.Single().OccupiedCells, Is.Empty);
        }

        [TestCase(0, 1)]
        [TestCase(2, 3)]
        [TestCase(4, 4)]
        public void Preparation_CapacityIncludesGuaranteedMain(int subs, int capacity)
        {
            var (initialization, roster) = Setup(subs, capacity);
            Assert.DoesNotThrow(() => Prepare(initialization, roster, Draw(roster)));
            if (capacity == 1) return;
            var (tooSmall, _) = Setup(subs, capacity - 1);
            Assert.Throws<InvalidOperationException>(() => Prepare(tooSmall, roster, Draw(roster)));
        }

        [Test]
        public void Deployment_MainMustBePlaced_AndFailedPlacementPreservesHandIdentityAndHealth()
        {
            var (initialization, roster) = Setup(1);
            var prepared = Prepare(initialization, roster, Draw(roster));
            var input = Input(initialization, roster, prepared);
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);

            var enter = Calculate(input, CombatCommandKind.EnterCombat);
            Assert.That(enter.Accepted, Is.False);
            Assert.That(enter.Reason, Is.EqualTo("InvalidMainCharacter"));
            Assert.That(enter.Characters.Characters, Is.Empty);

            var invalid = Calculate(input, CombatCommandKind.PlaceMainCharacter, new CellCoord(0, 3));
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Characters.Characters, Is.Empty);
            Assert.That(main.Area, Is.EqualTo(CharacterRuntimeArea.Hand));

            var placed = Calculate(input, CombatCommandKind.PlaceMainCharacter, new CellCoord(1, 0));
            Assert.That(placed.Accepted, Is.True, placed.Reason);
            var final = placed.Characters.Characters.Single(c => c.Final.EntityId == main.EntityId).Final;
            Assert.That(final.OwnedId, Is.EqualTo(main.OwnedId));
            Assert.That(final.Health, Is.EqualTo(main.Health));
            Assert.That(final.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(final.ReserveSlotIndex, Is.Null);
            Assert.That(final.Anchor, Is.EqualTo(new CellCoord(1, 0)));
            var entered = Calculate(After(input, placed), CombatCommandKind.EnterCombat);
            Assert.That(entered.Accepted, Is.True, entered.Reason);
        }

        [Test]
        public void Deployment_PositionBonusesStartAtManualPlacement_WithoutHealingMain()
        {
            var (source, roster) = Setup(1);
            var cell = new CellCoord(1, 0);
            var initialization = new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation,
                source.Units, source.Structures, source.Categories,
                environment: new MovementEnvironmentFacts(new MovementTileFact[]
                {
                    new DestinationTileModifierFact(71, cell, true, Array.Empty<Owner>(), 2, 3, 9)
                }), environmentBindings: new EnvironmentSemanticBindings(new[] { 71L }, Array.Empty<long>(), Array.Empty<EntityId>()));
            var prepared = Prepare(initialization, roster, Draw(roster));
            var main = prepared.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            Assert.That(main.PositionModifier.SourceIds, Is.Empty);
            Assert.That(main.Health.MaxHealth, Is.EqualTo(10));
            var placed = Calculate(Input(initialization, roster, prepared), CombatCommandKind.PlaceMainCharacter, cell);
            Assert.That(placed.Accepted, Is.True, placed.Reason);
            var final = placed.Characters.Characters.Single(c => c.Final.EntityId == main.EntityId).Final;
            Assert.That(final.PositionModifier.SourceIds, Is.EqualTo(new[] { 71L }));
            Assert.That(final.Stats.EffectiveAttackPower, Is.EqualTo(7));
            Assert.That(final.Health.MaxHealth, Is.EqualTo(13));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(7));
        }

        internal static CombatCommandTransition Calculate(CombatCommandInput input, CombatCommandKind kind,
            CellCoord cell = default, EntityId actorId = default) => CombatCommandCalculator.Calculate(
                new CombatCommandInput(1, kind, actorId, default, input.State, input.Characters, input.Profiles,
                    input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, targetCell: cell,
                    battle: input.Battle, environmentBindings: input.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (input.State).Turn.ControllerTurnSequence), Array.Empty<IMovementTileRule>());

        internal static CombatCommandInput After(CombatCommandInput input, CombatCommandTransition transition)
        {
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var finals = transition.Characters.Characters.ToDictionary(c => c.Final.EntityId, c => c.Final);
            return new CombatCommandInput(1, input.Kind, input.ActorId, default, transition.FinalState,
                input.Characters.Select(c => finals.TryGetValue(c.EntityId, out var final) ? final : c).ToArray(), input.Profiles,
                transition.Characters.Main?.Final ?? input.Main, input.Owned, input.SoulCounts, transition.Field.Final,
                input.Artifacts, battle: input.Battle, environmentBindings: input.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (transition.FinalState).Turn.ControllerTurnSequence);
        }

        internal static CombatCommandInput Input(BattleInitialization initialization, OwnedCharacterRecord[] roster,
            BattlePreparation prepared)
        {
            return new CombatCommandInput(1, CombatCommandKind.EnterCombat, EntityId.Empty, default,
                prepared.CommandState, prepared.Characters, initialization.Units.Select(unit => new CombatAttackProfile(unit,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray(),
                Main(), roster, roster.ToDictionary(c => c.Id, _ => 0), SoulFieldSnapshot.Empty,
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()),
                battle: new CombatBattleFacts(prepared.Combat.ReserveCapacity, 42, 2, 2, prepared.Combat.DrawOrder, prepared.Combat.DrawCursor),
                environmentBindings: initialization.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (prepared.CommandState).Turn.ControllerTurnSequence);
        }

        internal static InitialDrawPlan Draw(OwnedCharacterRecord[] roster) => InitialDrawCalculator.Plan(
            roster.Where(c => c.Role == CharacterOwnershipRole.Sub).Select(c => new StageDrawCandidate(c.Id, c.CharacterKey)).ToArray(), 42);

        internal static BattlePreparation Prepare(BattleInitialization initialization, OwnedCharacterRecord[] roster, InitialDrawPlan draw) =>
            BattlePreparationCalculator.Prepare(initialization, roster, Main(), roster.ToDictionary(c => c.Id, _ => 0), draw,
                Array.Empty<CharacterCombatFacts>());

        private static MainCharacterPersistentState Main() => new(7, 10, 5);

        internal static (BattleInitialization, OwnedCharacterRecord[]) Setup(int subs, int capacity = 8, CellCoord anchor = default)
        {
            var roster = Enumerable.Range(1, subs + 1).Select(id => new OwnedCharacterRecord(new CharacterInstanceId(id),
                id == 1 ? EntityKey.Unit_Gravekeeper : EntityKey.Unit_Akimbo,
                id == 1 ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub)).ToArray();
            var headquarters = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var stage = new StageConfig(8, 6, capacity, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                Array.Empty<CellCoord>(), Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(), new[] { new InitialEnemyStructurePlacementSpec(headquarters.Key, new CellCoord(6, 4), Rotation4.R0) });
            return (new BattleInitialization(stage, anchor, Rotation4.R0,
                new[] { Unit(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter), Unit(EntityKey.Unit_Akimbo, UnitRole.Regular) },
                new[] { headquarters }, UnitSpecTestFactory.CreateDefaultCategoryCatalog()), roster);
        }

        private static UnitSpec Unit(EntityKey key, UnitRole role) => new(key, role, 1, 1, new[] { CellCoord.Zero },
            10, MoveMethod.LiftAndPlace, 1, CategoryKey.Main, 5, 0,
            100, Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly);
    }
}
