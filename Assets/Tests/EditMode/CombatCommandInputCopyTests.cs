using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CombatCommandInputCopyTests
    {
        private static readonly string[] Properties = {
            "Number", "Kind", "ActorId", "Aim", "TargetCell", "Rotation", "Battle", "ProcessingKind", "State",
            "Characters", "Profiles", "MainId", "Main", "Owned", "SoulCounts", "Souls", "Artifacts", "Path", "MovementContext",
            "BattleInstanceId", "ArtifactItems", "EnvironmentBindings", "ObstacleContents", "ItemKey", "ItemInventory",
            "ItemDefinitions", "GoldBalance", "FameBalance", "Membership", "ExpectedBattleInstanceId", "ExpectedControllerTurnSequence", "ExpectedMoveCommandNumber" };

        [TestCase(true)]
        [TestCase(false)]
        public void CapturedMembership_InvalidOwnedInputKeepsArgumentContract(bool missingList)
        {
            var s = Fixture();
            var invalid = missingList ? null : new OwnedCharacterRecord[] { null };
            TestDelegate create = () => new CombatCommandInput(s.Number, s.Kind, s.ActorId, s.Aim,
                s.State, s.Characters, s.Profiles, s.Main, invalid, s.SoulCounts, s.Souls, s.Artifacts, membership: s.Membership,
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId);
            if (missingList) Assert.Throws<ArgumentNullException>(create);
            else Assert.Throws<ArgumentException>(create);
        }
        [Test]
        public void PublicPropertyInventory_RequiresAnExplicitPreservationDecision()
        {
            var properties = typeof(CombatCommandInput).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.That(properties.Select(p => p.Name), Is.EquivalentTo(Properties));
            Assert.That(properties.All(p => !p.CanWrite), Is.True);
            var fixture = Fixture();
            foreach (var property in properties)
                Assert.That(property.GetValue(fixture), Is.Not.EqualTo(property.PropertyType.IsValueType
                    ? Activator.CreateInstance(property.PropertyType) : null), property.Name);
            foreach (var name in new[] { "WithIntent", "WithWorld", "WithPersistentFacts" })
                Assert.That(typeof(CombatCommandInput).GetMethod(name).GetParameters().All(p => !p.IsOptional), Is.True, name);
        }

        [Test]
        public void ActorSelection_PreservesExpectedBattleAndControllerIdentity()
        {
            var source = Fixture();
            AssertCopy(source, source.WithActor(new EntityId(99)), new Dictionary<string, object> { ["ActorId"] = new EntityId(99) });
        }

        [TestCase(CombatProcessingKind.Command)]
        [TestCase(CombatProcessingKind.Advance)]
        public void Intent_ReplacesOnlyDeclaredFields_AndKeepsIdentityAndContext(CombatProcessingKind processing)
        {
            var source = Fixture(processing);
            var path = new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(1, 4) });
            var aim = AttackAim.ForCell(new CellCoord(5, 5));
            var context = new MovementPreviewContext(source.BattleInstanceId, Owner.Player, source.Characters[0],
                CombatCommandKind.Move, path);
            var changes = new Dictionary<string, object> { ["Kind"] = CombatCommandKind.Attack, ["Aim"] = aim,
                ["TargetCell"] = new CellCoord(6, 6), ["Path"] = path, ["MovementContext"] = context };

            var copy = source.WithIntent(CombatCommandKind.Attack, aim, new CellCoord(6, 6), path, context);

            AssertCopy(source, copy, changes);
            Assert.That(source.Kind, Is.EqualTo(CombatCommandKind.Move));
            Assert.That(source.Path.Cells.Last(), Is.EqualTo(new CellCoord(2, 3)));
        }

        [Test]
        public void World_SnapshotsChangedCollections_AndPreservesEconomicAndIntentContext()
        {
            var source = Fixture();
            var state = CombatCommandTestData.Input(empty: true).State;
            var characters = source.Characters.Reverse().ToArray();
            var main = new MainCharacterPersistentState(70, 100, 1);
            var counts = source.SoulCounts.ToDictionary(p => p.Key, p => p.Value + 1);
            var souls = new SoulFieldSnapshot(19, Array.Empty<SoulFieldFacts>());
            var bindings = new EnvironmentSemanticBindings(new long[] { 72 }, new long[] { 82 }, new[] { new EntityId(92) });

            var copy = source.WithWorld(state, characters, main, counts, souls, bindings);

            AssertCopy(source, copy, new Dictionary<string, object> { ["State"] = state, ["Characters"] = characters,
                ["Main"] = main, ["SoulCounts"] = counts, ["Souls"] = souls, ["EnvironmentBindings"] = bindings });
            characters[0] = null;
            counts[source.Owned[0].Id] = 99;
            Assert.That(copy.Characters[0], Is.Not.Null);
            Assert.That(copy.SoulCounts[source.Owned[0].Id], Is.EqualTo(source.SoulCounts[source.Owned[0].Id] + 1));
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterCombatFacts>)copy.Characters).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<CharacterInstanceId, int>)copy.SoulCounts).Clear());
            Assert.That(source.Main.CurrentHealth, Is.EqualTo(100));
            Assert.That(source.Souls.NextSoulId, Is.EqualTo(17));
        }

        [Test]
        public void PersistentFacts_SnapshotOwnedAndPreserveAllOtherContext()
        {
            var source = Fixture();
            var owned = source.Owned.Select(o => new OwnedCharacterRecord(o.Id, o.CharacterKey, o.Role, 2, 7)).ToArray();
            var inventory = new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 2 });

            var copy = source.WithPersistentFacts(owned, inventory, 29, 37);

            AssertCopy(source, copy, new Dictionary<string, object> { ["Owned"] = owned, ["ItemInventory"] = inventory,
                ["GoldBalance"] = 29, ["FameBalance"] = 37 });
            owned[0] = null;
            Assert.That(copy.Owned[0].Level, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IList<OwnedCharacterRecord>)copy.Owned).Clear());
            Assert.That(source.Owned.All(o => o.Level == 1), Is.True);
            Assert.That(source.ItemInventory.GetCount(1), Is.EqualTo(3));
            Assert.That((source.GoldBalance, source.FameBalance), Is.EqualTo(((int?)19, (int?)23)));
        }

        [Test]
        public void ExplicitNullClearsOptionalContext_WhileWrapperNullPreservesExistingPath()
        {
            var source = Fixture();
            var cleared = source.WithIntent(source.Kind, source.Aim, source.TargetCell, null, null);
            AssertCopy(source, cleared, new Dictionary<string, object> { ["Path"] = null, ["MovementContext"] = null });
            var funds = source.WithPersistentFacts(source.Owned, null, null, null);
            Assert.That(funds.GoldBalance, Is.Null);
            Assert.That(funds.FameBalance, Is.Null);
            Assert.That(funds.ItemInventory, Is.SameAs(ItemInventorySnapshot.Empty));
            var zero = source.WithPersistentFacts(source.Owned, source.ItemInventory, 0, 0);
            Assert.That((zero.GoldBalance, zero.FameBalance), Is.EqualTo(((int?)0, (int?)0)));
            var world = source.WithWorld(source.State, source.Characters, source.Main, source.SoulCounts, source.Souls, null);
            Assert.That(world.EnvironmentBindings, Is.SameAs(EnvironmentSemanticBindings.Empty));

            var wrapped = CombatCommandComposition.Intent(source, CombatCommandKind.Attack);
            AssertCopy(source, wrapped, new Dictionary<string, object> { ["Kind"] = CombatCommandKind.Attack,
                ["Aim"] = default(AttackAim), ["TargetCell"] = default(CellCoord) });
            var aimed = CombatAttackQueries.WithAim(source, AttackAim.ForCell(new CellCoord(6, 5)));
            AssertCopy(source, aimed, new Dictionary<string, object> { ["Aim"] = AttackAim.ForCell(new CellCoord(6, 5)) });
        }

        [TestCase(CombatCommandKind.None)]
        [TestCase((CombatCommandKind)999)]
        public void Intent_RejectsInvalidKindLikeFreshConstruction(CombatCommandKind kind)
        {
            var s = Fixture();
            AssertSameError(() => s.WithIntent(kind, s.Aim, s.TargetCell, s.Path, s.MovementContext),
                () => new CombatCommandInput(s.Number, kind, s.ActorId, s.Aim, s.State, s.Characters, s.Profiles,
                    s.Main, s.Owned, s.SoulCounts, s.Souls, s.Artifacts,
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId));
        }

        [TestCase("state")]
        [TestCase("characters")]
        [TestCase("characterEntry")]
        [TestCase("main")]
        [TestCase("soulCounts")]
        [TestCase("souls")]
        public void World_RejectsInvalidFactsLikeFreshConstruction(string missing)
        {
            var s = Fixture();
            var state = missing == "state" ? null : s.State;
            var characters = missing == "characters" ? null : missing == "characterEntry" ? new CharacterCombatFacts[] { null } : s.Characters;
            var main = missing == "main" ? null : s.Main;
            var counts = missing == "soulCounts" ? null : s.SoulCounts;
            var souls = missing == "souls" ? null : s.Souls;
            AssertSameError(() => s.WithWorld(state, characters, main, counts, souls, s.EnvironmentBindings),
                () => new CombatCommandInput(s.Number, s.Kind, s.ActorId, s.Aim, state, characters, s.Profiles,
                    main, s.Owned, counts, souls, s.Artifacts,
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId));
        }

        [TestCase("owned")]
        [TestCase("ownedEntry")]
        [TestCase("gold")]
        [TestCase("fame")]
        public void PersistentFacts_RejectInvalidValuesLikeFreshConstruction(string invalid)
        {
            var s = Fixture();
            var owned = invalid == "owned" ? null : invalid == "ownedEntry" ? new OwnedCharacterRecord[] { null } : s.Owned;
            int? gold = invalid == "gold" ? -1 : s.GoldBalance;
            int? fame = invalid == "fame" ? -1 : s.FameBalance;
            AssertSameError(() => s.WithPersistentFacts(owned, s.ItemInventory, gold, fame),
                () => new CombatCommandInput(s.Number, s.Kind, s.ActorId, s.Aim, s.State, s.Characters, s.Profiles,
                    s.Main, owned, s.SoulCounts, s.Souls, s.Artifacts, goldBalance: gold, fameBalance: fame,
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId));
        }

        [Test]
        public void Composition_ProjectsBalancesInventoryAndWorldWithoutChangingOriginalExpectations()
        {
            var source = Fixture();
            var s = source.State;
            var final = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                s.Structures,s.ThornFields,s.Turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,                s.Objective,s.Allocation,s.ItemRandomState,new CombatRewardFacts(false, Array.Empty<CharacterInstanceId>(), 8));
            var inventory = new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 1 });
            var result = new CombatCommandTransition(source, true, "",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(source.Souls, source.Souls), final,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None),
                inventoryChange: new ItemInventoryChange(source.ItemInventory, inventory),
                rewardChange: new CombatRewardChange(19, 27, 23, 23),
                rewardSettlement: new CombatRewardSettlement(1, 8, 0, 0, false, Array.Empty<CharacterRewardReceipt>()));

            var projected = CombatCommandComposition.Project(result);

            AssertCopy(source, projected, new Dictionary<string, object> { ["State"] = final, ["ItemInventory"] = inventory,
                ["GoldBalance"] = 27 });
            Assert.That((result.RewardChange.ExpectedGold, result.RewardChange.ExpectedFame), Is.EqualTo((19, 23)));
            Assert.That(result.InventoryChange.Expected, Is.SameAs(source.ItemInventory));
            Assert.That((source.GoldBalance, source.FameBalance), Is.EqualTo(((int?)19, (int?)23)));
            Assert.Throws<InvalidOperationException>(() => CombatCommandComposition.Project(CombatCommandCalculator.Reject(source, "Rejected")));
        }

        [Test]
        public void MovementWorldProjection_PreservesNondefaultCommandContext()
        {
            var s = Fixture();
            var source = s.WithWorld(s.State, s.Characters, s.Main, s.SoulCounts, s.Souls, EnvironmentSemanticBindings.Empty);
            var world = MovementProjection.ForArrival(source, source.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());

            var current = world.CurrentInput();

            Assert.That(current.State.SameValues(source.State), Is.True);
            Assert.That(current.Main.CurrentHealth, Is.EqualTo(source.Main.CurrentHealth));
            AssertCopy(source, current, new Dictionary<string, object> { ["State"] = current.State, ["Main"] = current.Main });
        }

        private static void AssertSameError(TestDelegate copy, TestDelegate fresh)
        {
            var expected = Assert.Catch<ArgumentException>(fresh);
            var actual = Assert.Catch<ArgumentException>(copy);
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()));
            Assert.That(actual.ParamName, Is.EqualTo(expected.ParamName));
        }

        [Test]
        public void WorldProjection_PreservesCapturedMainIdWhenMainIsOmitted()
        {
            var source = Fixture();
            var projected = source.Characters.Where(x => x.OwnershipRole != CharacterOwnershipRole.Main).ToArray();
            var copy = source.WithWorld(source.State, projected, source.Main, source.SoulCounts, source.Souls, source.EnvironmentBindings);
            Assert.That(source.MainId.IsEmpty, Is.False);
            Assert.That(copy.MainId, Is.EqualTo(source.MainId));
        }

        private static void AssertCopy(CombatCommandInput source, CombatCommandInput copy, IReadOnlyDictionary<string, object> changes)
        {
            Assert.That(copy, Is.Not.SameAs(source));
            foreach (var property in typeof(CombatCommandInput).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var expected = changes.TryGetValue(property.Name, out var value) ? value : property.GetValue(source);
                Assert.That(property.GetValue(copy), Is.EqualTo(expected), property.Name);
            }
        }

        private static CombatCommandInput Fixture(CombatProcessingKind processing = CombatProcessingKind.Command)
        {
            var s = CombatCommandTestData.Input();
            var path = new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) });
            var context = new MovementPreviewContext(83, Owner.Player, s.Characters[0], CombatCommandKind.Move, path);
            return new CombatCommandInput(processing == CombatProcessingKind.Command ? 17 : 0, CombatCommandKind.Move,
                s.ActorId, s.Aim, s.State, s.Characters, s.Profiles, s.Main, s.Owned,
                s.SoulCounts.ToDictionary(p => p.Key, p => p.Value + 2), new SoulFieldSnapshot(17, Array.Empty<SoulFieldFacts>()),
                new ArtifactLoadoutSnapshot(31, Array.Empty<ArtifactPlacedEffectSnapshot>()), new CellCoord(6, 5), Rotation4.R270,
                new CombatBattleFacts(4, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                    progression: new LevelProgressionSpec(150, 100, 3, 1, 1), clearRewards: new StageClearRewardSpec(100, 50, 100)),
                processing, path, context, 83,
                new[] { new ArtifactItemState(7, 8, true, new CellCoord(3, 3), Rotation4.R90, new[] { new CellCoord(3, 3) }) },
                new EnvironmentSemanticBindings(new long[] { 71 }, new long[] { 81 }, new[] { new EntityId(91) }),
                new[] { new ObstacleContentProfile(new EntityId(91), ObstacleContentKind.PowderBarrel, 0) }, 1,
                new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 3 }), new[] { new ItemDefinition(1, new WhiskeyItemSpec(50, 10)) }, 19, 23, new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck.RunDeckSnapshot(
                    new RunId(Guid.NewGuid()), 7, new[] { s.MainId }.Concat(s.Owned.Where(o => o.Id != s.MainId).Select(o => o.Id)).ToArray()),
                expectedBattleInstanceId: 83, expectedControllerTurnSequence: 7, expectedMoveCommandNumber: 9);
        }
    }
}
