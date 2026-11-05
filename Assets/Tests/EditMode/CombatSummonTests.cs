using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Summoning;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>사령술 보류의 무부작용과 재사용하는 위치·생존 상한 계산을 확인한다.</summary>
    public sealed class CombatSummonTests
    {
        internal static readonly EntityId Boss = new EntityId(2);
        internal static readonly NecromancyTraitSpec Ability = new NecromancyTraitSpec(EntityKey.Unit_Gravekeeper, 2, 7);

        [Test]
        public void SameFacts_RejectDeferredSummonWithoutAllocatingOrPublishing()
        {
            var input = Input(new CellCoord(3, 3));
            foreach (var result in new[] { Summon(input), Summon(input) })
            {
                Assert.That(result.Accepted, Is.False);
                Assert.That(result.Reason, Is.EqualTo("DeferredUnitTurnTiming"));
                Assert.That(result.Characters.Created, Is.Empty);
                Assert.That(result.Characters.Characters, Is.Empty);
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.FinalState.SameValues(input.State), Is.True);
                Assert.That(result.Field.Final.SameValues(input.Souls), Is.True);
            }
            Assert.That(input.State.Allocation.NextEntityId, Is.EqualTo(11));
        }

        [Test]
        public void Candidates_RespectBoardEdgeAndOccupancy_WhileActivationRemainsUnavailable()
        {
            var corner = new CellCoord(0, 0);
            var oneLeft = Input(corner, Enemy(5, new CellCoord(1, 0)), Enemy(6, new CellCoord(0, 1)));
            Assert.That(CombatSummonCalculator.Candidates(oneLeft, corner), Is.EqualTo(new[] { new CellCoord(1, 1) }));
            Assert.That(Summon(oneLeft).Accepted, Is.False);

            var full = Input(corner, Enemy(5, new CellCoord(1, 0)), Enemy(6, new CellCoord(0, 1)), Enemy(7, new CellCoord(1, 1)));
            Assert.That(CombatSummonCalculator.Candidates(full, corner), Is.Empty);
            Assert.That(Summon(full).Accepted, Is.False);
            Assert.That(CombatSummonCalculator.Candidates(Input(new CellCoord(3, 3)), new CellCoord(3, 3)),
                Is.EqualTo(new[] { new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(4, 2), new CellCoord(2, 3),
                    new CellCoord(4, 3), new CellCoord(2, 4), new CellCoord(3, 4), new CellCoord(4, 4) }));
        }

        [Test]
        public void LivingLimit_CountsFieldAndWaitingOfSameSource_Only()
        {
            var field = Enemy(20, new CellCoord(6, 6), EntityKey.Unit_Gravekeeper);
            var waiting = Waiting(21);
            var dead = CombatCommandCalculator.Hurt(Enemy(22, new CellCoord(6, 5), EntityKey.Unit_Gravekeeper), 1000);
            var otherSource = Enemy(23, new CellCoord(5, 6), EntityKey.Unit_Gravekeeper);
            var regular = Enemy(24, new CellCoord(5, 5), EntityKey.Unit_Gravekeeper);
            var records = new[]
            {
                Record(20, Boss), Record(21, Boss), Record(22, Boss), Record(23, new EntityId(30)),
            };
            var other = Enemy(30, new CellCoord(0, 7));

            var blocked = Input(new CellCoord(3, 3), records, 25, field, waiting, dead, otherSource, regular, other);
            Assert.That(CombatSummonCalculator.LivingSummons(blocked, Boss, TraitKey.Necromancy), Is.EqualTo(2));
            Assert.That(Summon(blocked).Accepted, Is.False);

            var oneDeadMore = Input(new CellCoord(3, 3), records, 25,
                field, CombatCommandCalculator.Hurt(Enemy(21, new CellCoord(6, 4), EntityKey.Unit_Gravekeeper), 1000), dead, otherSource, regular, other);
            Assert.That(CombatSummonCalculator.LivingSummons(oneDeadMore, Boss, TraitKey.Necromancy), Is.EqualTo(1));
            var rejected = Summon(oneDeadMore);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Created, Is.Empty);
        }

        [Test]
        public void DeferredActivation_DoesNotReadSpawnProfileOrAllocate()
        {
            var input = Input(new CellCoord(3, 3));
            Assert.That(CombatSummonCalculator.TryCalculate(input,
                new NecromancyTraitSpec(EntityKey.Unit_Ballista, 2, 7), Array.Empty<IMovementTileRule>()).Reason,
                Is.EqualTo("DeferredUnitTurnTiming"));
            Assert.That(Summon(Input(new CellCoord(3, 3), nextId: 0)).Reason, Is.EqualTo("DeferredUnitTurnTiming"));
        }

        [Test]
        public void DeferredActivation_DoesNotConsumeTheLastIdentity()
        {
            var input = Input(new CellCoord(3, 3), nextId: (long)int.MaxValue + 1);
            var result = Summon(input);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FinalState.Allocation.SameValues(input.State.Allocation), Is.True);
            Assert.That(result.Characters.Created, Is.Empty);
        }

        [Test]
        public void DeferredActivation_DoesNotTriggerArrivalMineOrRewards()
        {
            var regular = CombatCommandTestData.With(Enemy(5, new CellCoord(1, 0), EntityKey.Unit_Gravekeeper), hp: 10);
            var blocker = Enemy(6, new CellCoord(0, 1));
            var mine = MineEnvironmentTests.Mine(id: 50, damage: 10, cell: new CellCoord(1, 1));
            var input = Input(CellCoord.Zero, Array.Empty<CombatSummonRecord>(), 11,
                new ObjectSourceFact[] { mine }, Altar(), regular, blocker);
            var result = CombatSummonCalculator.TryCalculate(input, Ability, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.True);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.RewardChange, Is.Null);
        }

        internal static CombatCommandTransition Summon(CombatCommandInput input) =>
            CombatSummonCalculator.TryCalculate(input, Ability, Array.Empty<IMovementTileRule>());

        internal static CombatCommandInput Input(CellCoord bossCell, params CharacterCombatFacts[] extra) =>
            Input(bossCell, Array.Empty<CombatSummonRecord>(), 11, extra);

        internal static CombatCommandInput Input(CellCoord bossCell, CombatSummonRecord[] records, long nextId, params CharacterCombatFacts[] extra) =>
            Input(bossCell, records, nextId, Array.Empty<ObjectSourceFact>(), null, extra);

        internal static CombatCommandInput Input(CellCoord bossCell, long nextId) =>
            Input(bossCell, Array.Empty<CombatSummonRecord>(), nextId, Array.Empty<ObjectSourceFact>(), null);

        internal static CombatCommandInput Input(CellCoord bossCell, CombatSummonRecord[] records, long nextId,
            ObjectSourceFact[] objects, ArtifactLoadoutSnapshot? artifacts, params CharacterCombatFacts[] extra)
        {
            var source = CombatCommandTestData.Input(empty: true);
            var boss = Enemy(Boss.Value, bossCell);
            boss = new CharacterCombatFacts(boss.EntityId, null, boss.CharacterKey, boss.Owner, null, boss.Area, boss.Health,
                boss.Stats, null, boss.Anchor, Rotation4.R90, boss.OccupiedCells, boss.ActionUse);
            var characters = source.Characters.Append(boss).Concat(extra).ToArray();
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                controllerTurnSequence: 2, completedPlayerTurnCount: 3, summons: records);
            var state = new CombatCommandState(8,8,CombatFlowPhase.Automatic,true,                Array.Empty<StructureCombatFacts>(),Array.Empty<ThornFieldCellSnapshot>(),                turn,new MovementEnvironmentFacts(objects),                allocation: nextId == 0 ? null : new CombatAllocationFacts(nextId, CombatAllocationFacts.InitialSummonRandomState));
            var gravekeeper = new CombatAttackProfile(UnitSpecTestFactory.Create(EntityKey.Unit_Gravekeeper, 1, 1, new[] { CellCoord.Zero },
                10, MoveMethod.LiftAndPlace, 1, 10, 30, Array.Empty<TraitSpec>(), categoryKey: CategoryKey.Rider),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var owned = characters.Where(c => c.OwnedId.HasValue).Select(c => new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
                .OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, Boss, default, state, characters,
                source.Profiles.Append(gravekeeper).ToArray(), source.Main, owned, owned.ToDictionary(o => o.Id, _ => 0),
                source.Souls, artifacts ?? source.Artifacts, processingKind: CombatProcessingKind.Advance,
                battle: new CombatBattleFacts(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0,
                    categories: UnitSpecTestFactory.CreateDefaultCategoryCatalog()),
                environmentBindings: ObjectArrivalOrderTests.Bindings(objects.Select(o => o.Id).ToArray()),
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }

        internal static CharacterCombatFacts Enemy(int id, CellCoord cell, EntityKey key = EntityKey.Unit_Demon) =>
            CombatCommandTestData.Character(id, key, cell, Owner.Enemy);

        private static CharacterCombatFacts Waiting(int id)
        {
            var c = Enemy(id, new CellCoord(6, 4), EntityKey.Unit_Gravekeeper);
            return new CharacterCombatFacts(c.EntityId, null, c.CharacterKey, c.Owner, null, CharacterRuntimeArea.EnemyWaiting,
                c.Health, c.Stats, null, null, null, Array.Empty<CellCoord>(), c.ActionUse);
        }

        private static CombatSummonRecord Record(int summoned, EntityId summoner) =>
            new CombatSummonRecord(new EntityId(summoned), summoner, TraitKey.Necromancy);

        private static ArtifactLoadoutSnapshot Altar() => new ArtifactLoadoutSnapshot(3, new[]
            { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) });
    }
}
