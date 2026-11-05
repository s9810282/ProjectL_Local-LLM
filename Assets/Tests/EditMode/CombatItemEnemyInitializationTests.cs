using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class CombatItemEnemyInitializationTests
    {
        [TestCase(null)]
        [TestCase(2)]
        public void EnemyInitialEquipment_IsOptionalAndPreparationIsDeterministic(int? key)
        {
            var initial = Definition(key);
            var first = Prepare(initial);
            var second = Prepare(initial);
            Assert.That(first.Characters.Single(c => c.Owner == Owner.Enemy).Items.EquippedItemKey, Is.EqualTo(key));
            Assert.That(first.Characters.Single(c => c.Owner == Owner.Player).Items.EquippedItemKey, Is.Null);
            Assert.That(first.CommandState.ItemRandomState, Is.EqualTo(42u));
            Assert.That(second.CommandState.ItemRandomState, Is.EqualTo(first.CommandState.ItemRandomState));
        }

        [Test]
        public void UnknownStartingKey_RejectsBeforeAnyPreparedPublication()
        {
            Assert.Throws<InvalidOperationException>(() => Prepare(Definition(999)));
            var valid = Prepare(Definition(2));
            Assert.Throws<InvalidOperationException>(() => Prepare(Definition(2), valid.Characters));
            Assert.That(valid.Characters.Single(c => c.Owner == Owner.Enemy).Items.EquippedItemKey, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonpositiveStartingKey_IsRejected(int key) => Assert.Throws<ArgumentOutOfRangeException>(() =>
            new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R0, startingItemKey: key));

        internal static BattleInitialization Definition(int? key)
        {
            var units = new[] {
                Unit(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly),
                Unit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly) };
            var stage = new StageConfig(8, 6, 4, 2, 2, 42,
                new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R0, startingItemKey: key) },
                Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), Array.Empty<InitialEnemyStructurePlacementSpec>());
            return new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, units, Array.Empty<StructureSpec>(),
                UnitSpecTestFactory.CreateDefaultCategoryCatalog(), itemDefinitions: new[] { new ItemDefinition(2, new RoketItemSpec(20, 50)) });
        }
        private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility) =>
            new UnitSpec(key, role, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace,
                role == UnitRole.MainCharacter ? 1 : 2, CategoryKey.Main, 5, 0,
                1, Array.Empty<TraitSpec>(), eligibility);
        internal static BattlePreparation Prepare(BattleInitialization d, IReadOnlyList<CharacterCombatFacts> active = null)
        {
            var id = new CharacterInstanceId(1);
            return BattlePreparationCalculator.Prepare(d, new[] { new OwnedCharacterRecord(id, EntityKey.Unit_Gravekeeper,
                CharacterOwnershipRole.Main) }, new MainCharacterPersistentState(10, 10, 5),
                new Dictionary<CharacterInstanceId, int> { [id] = 0 }, new InitialDrawPlan(Array.Empty<CharacterInstanceId>()),
                active ?? Array.Empty<CharacterCombatFacts>());
        }
    }
}
