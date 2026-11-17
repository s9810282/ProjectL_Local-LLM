using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class NexusReinforcementDefinitionTests
    {
        [Test]
        public void DefaultDefinition_IsSeparateAndImmutable()
        {
            var a = new NexusReinforcementEntry(EntityKey.Unit_Demon, 3);
            var b = new NexusReinforcementEntry(EntityKey.Unit_Cowboy, 2);
            var rows = new[] { b, a };
            var spec = new NexusReinforcementSpec(rows, 3);
            rows[0] = null;

            Assert.That(spec.Pool, Is.EqualTo(new[] { a, b }));
            Assert.Throws<NotSupportedException>(() => ((IList<NexusReinforcementEntry>)spec.Pool)[0] = b);
            Assert.That(spec.Pool.Sum(row => row.Count), Is.EqualTo(5));
            Assert.That(spec.SpawnCountPerTurn, Is.EqualTo(2));
            Assert.That(spec.RangePadding, Is.EqualTo(2));
            Assert.That(spec.EnemyLevel, Is.EqualTo(3));
            Assert.That(spec.FieldEnemyLimit, Is.EqualTo(5));

            var initialization = Initialization(spec);
            var profiles = initialization.WithAttackProfiles(initialization.AttackProfiles);
            var definition = profiles.WithTraitGrowth(initialization.TraitGrowth);
            Assert.That(definition.NexusReinforcements, Is.SameAs(spec));
            var prepared = Prepare(definition);
            Assert.That(prepared.Combat.NexusReinforcements, Is.SameAs(spec));
            Assert.That(prepared.Characters.Count(c => c.Owner == Owner.Enemy), Is.EqualTo(1));
        }

        [Test]
        public void Interval_DefaultAndConfiguredValues()
        {
            var pool = Array.Empty<NexusReinforcementEntry>();
            Assert.That(new NexusReinforcementSpec(pool, 1).SpawnIntervalTurns, Is.EqualTo(1));

            foreach (int interval in new[] { 2, 3 })
            {
                var spec = new NexusReinforcementSpec(pool, 1, spawnIntervalTurns: interval);
                Assert.That(spec.SpawnIntervalTurns, Is.EqualTo(interval));
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Interval_RejectsNonPositiveValues(int interval)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new NexusReinforcementSpec(Array.Empty<NexusReinforcementEntry>(), 1, spawnIntervalTurns: interval));
            Assert.That(error.ParamName, Is.EqualTo("spawnIntervalTurns"));
        }

        [Test]
        public void EnabledEmptyPool_IsValid()
        {
            var spec = new NexusReinforcementSpec(Array.Empty<NexusReinforcementEntry>(), 1, 4, 1);
            Assert.That(Prepare(Initialization(spec)).Combat.NexusReinforcements.Pool, Is.Empty);
            Assert.That(spec.SpawnCountPerTurn, Is.EqualTo(4));
            Assert.That(spec.RangePadding, Is.EqualTo(1));
        }

        [Test]
        public void Definition_KeepsConfiguredFieldEnemyLimit()
        {
            var spec = new NexusReinforcementSpec(Array.Empty<NexusReinforcementEntry>(), 1, fieldEnemyLimit: 8);
            Assert.That(spec.FieldEnemyLimit, Is.EqualTo(8));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new NexusReinforcementSpec(Array.Empty<NexusReinforcementEntry>(), 1, fieldEnemyLimit: 0));
        }

        [TestCase(EntityKey.None, 1)]
        [TestCase((EntityKey)int.MaxValue, 1)]
        [TestCase(EntityKey.Unit_Demon, 0)]
        [TestCase(EntityKey.Unit_Demon, -1)]
        public void PoolRow_RejectsInvalidKeyOrCount(EntityKey key, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NexusReinforcementEntry(key, count));
        }

        [TestCase(0, 2, 2)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 2, 0)]
        [TestCase(1, 2, -1)]
        public void Definition_RejectsInvalidLevelBatchOrPadding(int level, int batch, int padding)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new NexusReinforcementSpec(Array.Empty<NexusReinforcementEntry>(), level, batch, padding));
        }

        [Test]
        public void Definition_RejectsNullDuplicateAndOverflowingRows()
        {
            var a = new NexusReinforcementEntry(EntityKey.Unit_Demon, int.MaxValue);
            var b = new NexusReinforcementEntry(EntityKey.Unit_Cowboy, 1);
            Assert.Throws<ArgumentNullException>(() => new NexusReinforcementSpec(null, 1));
            Assert.Throws<ArgumentException>(() => new NexusReinforcementSpec(new[] { a, null }, 1));
            Assert.Throws<ArgumentException>(() => new NexusReinforcementSpec(new[] { a, a }, 1));
            Assert.Throws<ArgumentException>(() => new NexusReinforcementSpec(new[] { a, b }, 1));
        }

        [TestCase("boss objective", "본부 파괴")]
        [TestCase("missing nexus", "적 본부")]
        [TestCase("multiple nexus", "적 본부")]
        [TestCase("missing unit", "유닛 정의")]
        [TestCase("missing profile", "공격 프로필")]
        [TestCase("owned profile only", "공격 프로필")]
        [TestCase("player only", "소유 권한")]
        [TestCase("boss unit", "보스")]
        [TestCase("multi-cell", "한 칸")]
        [TestCase("invalid profile cell", "한 칸")]
        public void EnabledPreparation_RejectsInvalidConfiguration(string condition, string reason)
        {
            var spec = new NexusReinforcementSpec(new[] { new NexusReinforcementEntry(EntityKey.Unit_Cowboy, 1) }, 3);
            var error = Assert.Throws<InvalidOperationException>(() => Prepare(Initialization(spec, condition)));
            Assert.That(error.Message, Does.Contain(reason));
        }

        [TestCase("missing nexus")]
        [TestCase("multiple nexus")]
        [TestCase("initial cap")]
        [TestCase("boss objective")]
        public void DisabledPreparation_KeepsLegacyValidation(string condition)
        {
            Assert.That(Prepare(Initialization(null, condition)).Combat.NexusReinforcements, Is.Null);
        }

        [Test]
        public void EnabledPreparation_AllowsInitialEnemiesAboveFieldLimit()
        {
            var spec = new NexusReinforcementSpec(new[] { new NexusReinforcementEntry(EntityKey.Unit_Cowboy, 1) }, 3);
            var prepared = Prepare(Initialization(spec, "initial cap"));
            Assert.That(prepared.Combat.NexusReinforcements, Is.SameAs(spec));
            Assert.That(prepared.Characters.Count(c => c.Owner == Owner.Enemy), Is.GreaterThan(spec.FieldEnemyLimit));
        }

        internal static BattleInitialization Initialization(NexusReinforcementSpec spec, string condition = "")
        {
            var main = UnitSpecTestFactory.Create(EntityKey.Unit_Akimbo, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 1, 1, 1, Array.Empty<TraitSpec>(), role: UnitRole.MainCharacter,
                ownerEligibility: UnitOwnerEligibility.PlayerOnly);
            var initial = UnitSpecTestFactory.Create(EntityKey.Unit_Demon, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 1, 1, ownerEligibility: UnitOwnerEligibility.EnemyOnly);
            var cells = condition == "multi-cell" ? new[] { CellCoord.Zero, new CellCoord(1, 0) } : new[] { CellCoord.Zero };
            var candidate = UnitSpecTestFactory.Create(EntityKey.Unit_Cowboy, cells.Length, 1, cells, 10,
                MoveMethod.LiftAndPlace, 1, 1, 1, Array.Empty<TraitSpec>(),
                categoryKey: condition == "boss unit" ? CategoryKey.Boss : CategoryKey.Main,
                ownerEligibility: condition == "player only" ? UnitOwnerEligibility.PlayerOnly : UnitOwnerEligibility.EnemyOnly);
            var units = condition == "missing unit" ? new[] { main, initial } : new[] { main, initial, candidate };
            bool omitCandidateProfile = condition is "missing profile" or "owned profile only" or "multi-cell" or "invalid profile cell";
            var profiles = UnitSpecTestFactory.Profiles(units.Where(u => u != candidate || !omitCandidateProfile).ToArray());
            if (condition == "owned profile only")
                profiles = profiles.Append(new CombatAttackProfile(candidate, UnitSpecTestFactory.DefaultMethod(),
                    new CharacterInstanceId(99))).ToArray();
            if (condition == "invalid profile cell")
            {
                var invalid = UnitSpecTestFactory.Create(candidate.Key, 1, 1, new[] { new CellCoord(1, 0) }, 10,
                    MoveMethod.LiftAndPlace, 1, 1, ownerEligibility: UnitOwnerEligibility.EnemyOnly);
                profiles = profiles.Append(new CombatAttackProfile(invalid, UnitSpecTestFactory.DefaultMethod())).ToArray();
            }

            var objective = condition == "boss objective" ? BattleObjectiveKind.DesignatedBossDefeated :
                BattleObjectiveKind.EnemyHeadquartersDestroyed;
            var initialEnemies = Enumerable.Range(0, condition == "initial cap" ? 6 : 1)
                .Select(x => new InitialEnemyPlacementSpec(initial.Key, new CellCoord(x, 4), Rotation4.R0,
                    isVictoryTarget: objective == BattleObjectiveKind.DesignatedBossDefeated)).ToArray();
            var nexus = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            int nexusCount = condition == "missing nexus" ? 0 : condition == "multiple nexus" ? 2 : 1;
            var placements = Enumerable.Range(0, nexusCount)
                .Select(x => new InitialEnemyStructurePlacementSpec(nexus.Key, new CellCoord(x, 5), Rotation4.R0)).ToArray();
            var stage = new StageConfig(8, 6, 8, PlayerDeploymentZone.Rows(2), 42, initialEnemies,
                Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), placements);
            return new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, units, new[] { nexus },
                UnitSpecTestFactory.CreateDefaultCategoryCatalog(), profiles, objectiveKind: objective, nexusReinforcements: spec);
        }

        internal static BattlePreparation Prepare(BattleInitialization definition)
        {
            var id = new CharacterInstanceId(1);
            var owned = new[] { new OwnedCharacterRecord(id, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main) };
            return BattlePreparationCalculator.Prepare(definition, owned, new MainCharacterPersistentState(10, 10, 1),
                new Dictionary<CharacterInstanceId, int> { [id] = 0 }, new InitialDrawPlan(Array.Empty<CharacterInstanceId>()),
                Array.Empty<CharacterCombatFacts>());
        }
    }
}
