using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>전투 중 생성 개체가 합성 단계를 거쳐도 최종 Created 하나로 남는지 확인한다(계획 T2.1, D9).</summary>
    public sealed class CombatCreationCompositionTests
    {
        private static readonly EntityId Summoner = new EntityId(2);
        private static readonly EntityId Created = new EntityId(11);

        [Test]
        public void Project_CarriesCreatedCharacterAllocationAndSummonRecord()
        {
            var creation = CreationStep(hp: 100);

            var next = CombatCommandComposition.Project(creation);

            Assert.That(next.Characters.Any(c => c.EntityId == Created), Is.True);
            Assert.That(next.State.Allocation.NextEntityId, Is.EqualTo(12));
            Assert.That(next.State.Turn.Summons.Single().SummonedId, Is.EqualTo(Created));
        }

        [TestCase(100, false)]
        [TestCase(1, true)]
        public void CreateThenDamageOrDie_RemainsOneFinalCreatedFact(int hp, bool dies)
        {
            var creation = CreationStep(hp);
            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(
                CombatCommandComposition.Project(creation), CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(4, 3))),
                Array.Empty<IMovementTileRule>());
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            // 두 번째 단계 입장에서는 생성 개체가 기존 개체다.
            Assert.That(attack.Characters.Characters.Any(c => c.Final.EntityId == Created), Is.True);
            Assert.That(attack.Characters.Created, Is.Empty);

            var merged = CombatCommandComposition.Merge(creation.Input, new[] { creation, attack });

            var created = merged.Characters.Created.Single();
            Assert.That(created.EntityId, Is.EqualTo(Created));
            Assert.That(merged.Characters.Characters.Any(c => c.Final.EntityId == Created), Is.False);
            Assert.That(created.Health.CurrentHealth, Is.LessThan(hp));
            Assert.That(created.Health.IsDead, Is.EqualTo(dies));
            Assert.That(created.Area, Is.EqualTo(dies ? CharacterRuntimeArea.Excluded : CharacterRuntimeArea.Field));
            Assert.That(merged.FinalState.Allocation.NextEntityId, Is.EqualTo(12));
            Assert.That(merged.FinalState.Turn.Summons.Single().SummonedId, Is.EqualTo(Created));
        }

        [Test]
        public void Merge_WithoutCreation_HasNoCreated()
        {
            var input = Base();
            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Attack,
                AttackAim.ForCell(new CellCoord(2, 3))), Array.Empty<IMovementTileRule>());

            var merged = CombatCommandComposition.Merge(input, new[] { attack });

            Assert.That(merged.Characters.Created, Is.Empty);
            Assert.That(merged.FinalState.Allocation.SameValues(input.State.Allocation), Is.True);
        }

        [Test]
        public void OwnedChanges_ProjectToNextStepAndMergeIntoOneChange()
        {
            var input = Base();
            var original = input.Owned.First();
            var first = Progress(original, 1, 10);
            var second = Progress(original, 2, 5);
            var step1 = OwnedStep(input, original, first);

            var next = CombatCommandComposition.Project(step1);
            var step2 = OwnedStep(next, first, second);
            var merged = CombatCommandComposition.Merge(input, new[] { step1, step2 });

            Assert.That(next.Owned.Select(o => o.Id), Is.EqualTo(input.Owned.Select(o => o.Id)));
            Assert.That(next.Owned.First(), Is.SameAs(first));
            Assert.That(next.Owned.Skip(1), Is.EqualTo(input.Owned.Skip(1)));
            var change = merged.Characters.Owned.Single();
            Assert.That(change.Expected, Is.SameAs(original));
            Assert.That(change.Final.SameValues(second), Is.True);
        }

        [Test]
        public void Merge_WithoutOwnedChanges_HasNoOwnedChanges()
        {
            var input = Base();
            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Attack,
                AttackAim.ForCell(new CellCoord(2, 3))), Array.Empty<IMovementTileRule>());

            Assert.That(CombatCommandComposition.Merge(input, new[] { attack }).Characters.Owned, Is.Empty);
            Assert.That(CombatCommandComposition.Project(attack).Owned, Is.EqualTo(input.Owned));
        }

        [Test]
        public void AllocationAndSummonContracts_RejectInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatAllocationFacts(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatAllocationFacts((long)int.MaxValue + 2, 1));
            Assert.That(CombatAllocationFacts.Unavailable.IsAvailable, Is.False);
            Assert.Throws<ArgumentException>(() => new CombatSummonRecord(Created, Created, TraitKey.Necromancy));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatSummonRecord(Created, Summoner, TraitKey.None));
            var record = new CombatSummonRecord(Created, Summoner, TraitKey.Necromancy);
            Assert.Throws<ArgumentException>(() => new CombatTurnFacts(StagePhase.Combat, summons: new[] { record, record }));
            Assert.Throws<ArgumentException>(() => new CombatTurnFacts(StagePhase.Combat,
                enemyOrigins: new[] { new CombatEnemyOrigin(Created, CellCoord.Zero, Rotation4.R0) }, summons: new[] { record }));
        }

        [Test]
        public void CommandState_ComparesAllocationAndSummons()
        {
            var state = Base().State;
            var moved = With(state, new CombatAllocationFacts(12, 1), state.Turn);
            var recorded = With(state, state.Allocation, Turn(state.Turn, new[] { new CombatSummonRecord(Created, Summoner, TraitKey.Necromancy) }));

            Assert.That(moved.SameValues(state), Is.False);
            Assert.That(recorded.SameValues(state), Is.False);
            Assert.That(With(state, state.Allocation, state.Turn).SameValues(state), Is.True);
        }

        /// <summary>계산기 없이 생성 단계 하나를 손으로 만든다. 위치·기록·할당·참가자를 생성 규칙에 맞게 채운다.</summary>
        private static CombatCommandTransition CreationStep(int hp)
        {
            var input = Base();
            var created = CombatCommandTestData.With(CombatCommandTestData.Character(Created.Value, EntityKey.Unit_Demon,
                new CellCoord(4, 3), Owner.Enemy, actionUse: UnitActionUse.Deferred), hp: hp);
            var turn = Turn(input.State.Turn, new[] { new CombatSummonRecord(Created, Summoner, TraitKey.Necromancy) });
            var final = With(input.State, new CombatAllocationFacts(12, 5), turn);
            return new CombatCommandTransition(input, true, "",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(), created: new[] { created }),
                new SoulFieldChange(input.Souls, input.Souls), final, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None));
        }

        /// <summary>계산기 없이 보유 기록 변경만 있는 단계 하나를 만든다. 레벨업 판단은 이 시험의 대상이 아니다.</summary>
        private static CombatCommandTransition OwnedStep(CombatCommandInput input, OwnedCharacterRecord expected,
            OwnedCharacterRecord final) => new(input, true, "",
            new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                owned: new[] { new OwnedCharacterChange(expected, final) }),
            new SoulFieldChange(input.Souls, input.Souls), input.State, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None));

        private static OwnedCharacterRecord Progress(OwnedCharacterRecord r, int level, int experience) =>
            new(r.Id, r.CharacterKey, r.Role, level, experience);

        private static CombatCommandInput Base()
        {
            var source = CombatCommandTestData.Input(empty: true);
            var summoner = CombatCommandTestData.Character(Summoner.Value, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy);
            var s = source.State;
            var turn = source.State.Turn;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.Structures,                s.ThornFields,turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,s.Objective,                new CombatAllocationFacts(11, 1));
            return CombatCommandTestData.With(source, source.Characters.Append(summoner).ToArray(), state);
        }

        private static CombatCommandState With(CombatCommandState s, CombatAllocationFacts allocation, CombatTurnFacts turn) =>
            new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.Structures,                s.ThornFields,turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,s.Objective,allocation);

        private static CombatTurnFacts Turn(CombatTurnFacts t, CombatSummonRecord[] summons) => new CombatTurnFacts(t.StagePhase, t.ControllerPhase, t.ControllerTurnSequence,
            t.CompletedPlayerTurnCount, t.EnemyOrigins, summons);
    }
}
