using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class CombatDebuffCalculatorTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void IndependentStatuses_ApplyAndClearPreserveOtherComponent(bool intimidationFirst)
        {
            var spec = new IntimidationTraitSpec(20);
            var initial = intimidationFirst
                ? CombatDebuffCalculator.ApplyIntimidation(CharacterDebuffState.Empty, spec)
                : CombatDebuffCalculator.ApplyDefenseReduction(CharacterDebuffState.Empty, 30, false);
            var both = intimidationFirst
                ? CombatDebuffCalculator.ApplyDefenseReduction(initial, 30, false)
                : CombatDebuffCalculator.ApplyIntimidation(initial, spec);
            var clear = CombatDebuffCalculator.ClearIntimidation(both);
            Assert.That(clear.Intimidation, Is.Null);
            Assert.That(clear.DefenseReduction.SameValues(both.DefenseReduction), Is.True);
            var ticked = new DefenseReductionStatusRule()
                .OnTurnEnded(both);
            Assert.That(ticked.DefenseReduction, Is.Null);
            Assert.That(ticked.Intimidation.SameValues(both.Intimidation), Is.True);
            Assert.That(CombatDebuffCalculator.ClearIntimidation(ticked).SameValues(CharacterDebuffState.Empty), Is.True);
            Assert.That(both.DefenseReduction.ReductionPercent, Is.EqualTo(30));
            Assert.That(intimidationFirst ? initial.DefenseReduction == null : initial.Intimidation == null, Is.True);
        }

        [Test]
        public void CombinedStatusEquality_IncludesEachComponentAndAbsence()
        {
            var both = new CharacterDebuffState(new IntimidationDebuffState(20),
                defenseReduction: new DefenseReductionDebuffState(30, 1));
            Assert.That(both.SameValues(new CharacterDebuffState(new IntimidationDebuffState(20),
                defenseReduction: new DefenseReductionDebuffState(30, 1))), Is.True);
            Assert.That(both.SameValues(new CharacterDebuffState(new IntimidationDebuffState(21),
                defenseReduction: both.DefenseReduction)), Is.False);
            Assert.That(both.SameValues(new CharacterDebuffState(both.Intimidation,
                defenseReduction: new DefenseReductionDebuffState(31, 1))), Is.False);
            Assert.That(both.SameValues(new CharacterDebuffState(both.Intimidation)), Is.False);
            Assert.That(both.SameValues(new CharacterDebuffState(defenseReduction: both.DefenseReduction)), Is.False);
            Assert.That(both.SameValues(null), Is.False);
        }

        [TestCase("damage")]
        [TestCase("movement")]
        [TestCase("turn-reset")]
        [TestCase("hand-return")]
        public void CopiesPreserveCombinedState_ReserveEntryClearsItWithoutChangingSource(string operation)
        {
            var source = CombatCommandTestData.Input().Characters[0];
            var both = new CharacterDebuffState(new IntimidationDebuffState(20),
                defenseReduction: new DefenseReductionDebuffState(30, 1));
            source = CombatDebuffApplication.WithDebuffs(source, both);
            var final = operation switch
            {
                "damage" => CombatCommandCalculator.Hurt(source, 5),
                "movement" => source.WithPlacement(source.Area, source.ReserveSlotIndex,
                    CellCoord.Right,
                    source.Rotation, new[] { CellCoord.Right },
                    actionUse: UnitActionUse.Moved),
                "turn-reset" => source.With(actionUse: UnitActionUse.Available),
                "hand-return" => CombatPlacementCalculator.Location(source, CharacterRuntimeArea.Hand, 0, null, null),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
            Assert.That(final.Debuffs.SameValues(operation == "hand-return" ? CharacterDebuffState.Empty : both), Is.True);
            Assert.That(source.Debuffs.SameValues(both), Is.True);
            Assert.That(source.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(source.Area, Is.EqualTo(CharacterRuntimeArea.Field));
        }

        [Test]
        public void Apply_RepeatedCastersRetainFirstReductionUntilCleared()
        {
            var empty = CharacterDebuffState.Empty;
            var first = CombatDebuffCalculator.ApplyIntimidation(empty, new IntimidationTraitSpec(20));
            var second = CombatDebuffCalculator.ApplyIntimidation(first, new IntimidationTraitSpec(50));
            var third = CombatDebuffCalculator.ApplyIntimidation(second, new IntimidationTraitSpec(80));

            Assert.That(empty.Intimidation, Is.Null);
            AssertState(first, 20);
            AssertState(second, 20);
            AssertState(third, 20);
            Assert.That(third.SameValues(second), Is.True);
            var cleared = CombatDebuffCalculator.ClearIntimidation(third);
            Assert.That(cleared.SameValues(empty), Is.True);
            Assert.That(CombatDebuffCalculator.ClearIntimidation(empty).Intimidation, Is.Null);
            var fresh = CombatDebuffCalculator.ApplyIntimidation(cleared, new IntimidationTraitSpec(40));
            AssertState(fresh, 40);
            AssertState(third, 20);
        }

        [TestCase(15, 20, 12)]
        [TestCase(15, 25, 11)]
        [TestCase(15, 100, 0)]
        [TestCase(0, 20, 0)]
        [TestCase(int.MaxValue, 1, 2126008810)]
        public void AttackPower_FloorsPercentageUsingWideIntermediate(int attack, int percent, int expected)
        {
            Assert.That(CombatDebuffCalculator.EvaluateAttackPower(attack, State(percent)), Is.EqualTo(expected));
            Assert.That(CombatDebuffCalculator.EvaluateAttackPower(attack, CharacterDebuffState.Empty), Is.EqualTo(attack));
        }

        [Test]
        public void Equality_DistinguishesPresenceAndAttackReduction()
        {
            var state = State(20);
            Assert.That(state.SameValues(State(20)), Is.True);
            Assert.That(state.SameValues(State(21)), Is.False);
            Assert.That(state.SameValues(CharacterDebuffState.Empty), Is.False);
            Assert.That(CharacterDebuffState.Empty.SameValues(state), Is.False);
            Assert.That(CharacterDebuffState.Empty.SameValues(new CharacterDebuffState()), Is.True);
            Assert.That(state.SameValues(null), Is.False);
            Assert.That(state.Intimidation.SameValues(null), Is.False);
            var independent = CombatDebuffCalculator.ApplyIntimidation(State(50), new IntimidationTraitSpec(80));
            AssertState(independent, 50);
            AssertState(state, 20);
        }

        [TestCase(0)]
        [TestCase(101)]
        public void State_RejectsInvalidPercent(int percent)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntimidationDebuffState(percent));
        }

        [Test]
        public void Calculator_RejectsInvalidInputsEvenWithoutAnActiveDebuff()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntimidationDebuffState(0));
            Assert.Throws<ArgumentNullException>(() => CombatDebuffCalculator.ApplyIntimidation(null, new IntimidationTraitSpec(20)));
            Assert.Throws<ArgumentNullException>(() => CombatDebuffCalculator.ApplyIntimidation(CharacterDebuffState.Empty, null));
            Assert.Throws<ArgumentNullException>(() => CombatDebuffCalculator.ClearIntimidation(null));
            Assert.Throws<ArgumentNullException>(() => CombatDebuffCalculator.EvaluateAttackPower(15, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatDebuffCalculator.EvaluateAttackPower(-1, CharacterDebuffState.Empty));
        }

        private static CharacterDebuffState State(int percent) =>
            new CharacterDebuffState(new IntimidationDebuffState(percent));

        private static void AssertState(CharacterDebuffState state, int percent)
        {
            Assert.That(state.Intimidation, Is.Not.Null);
            Assert.That(state.Intimidation.AttackReductionPercent, Is.EqualTo(percent));
        }
    }
}
