using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class EnhancementPoisonThornTests
    {

        internal static CombatCommandInput Run(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, CombatTraitTestFixtures.ThornRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            return CombatCommandComposition.Project(result);
        }

        [Test]
        public void DeferredPoisonThorn_DoesNotCreateFieldsOrArmDefenseTimer()
        {
            var attack = CombatCommandEffectTests.ActorEffect(new ThornFieldTraitSpec(7, 5, 30, UnitTurnTickPhase.Started),
                new ThornFieldTraitStatusSnapshot(7, 5, 30, UnitTurnTickPhase.Started));
            var final = Run(attack);
            Assert.That(final.State.ThornFields, Is.Empty);
            Assert.That(final.Characters.All(c => c.Debuffs.DefenseReduction == null), Is.True);
            var movement = CombatTraitTestFixtures.ThornMovement();
            var moved = Run(movement).Characters.Single(c => c.EntityId == movement.ActorId);
            Assert.That(moved.Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(moved.Debuffs.DefenseReduction, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MovementContextRejectsChangedPoisonPayload(bool changeStatus)
        {
            var input = CombatTraitTestFixtures.ThornMovement();
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State,
                input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                path: input.Path, battleInstanceId: 1,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence);
            var before = MovementContextCalculator.Capture(input, Owner.Player, CombatTraitTestFixtures.ThornRules);
            var changed = changeStatus ? CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatDebuffApplication.WithDebuffs(c, new CharacterDebuffState(defenseReduction: new DefenseReductionDebuffState(20, 1))) : c).ToArray())
                : CombatCommandTestData.With(input, state: CombatCommandEffectTests.WithFields(input,
                    new ThornFieldCellSnapshot(new CellCoord(2, 3), 5, 7, 20, UnitTurnTickPhase.Started)));
            Assert.That(MovementContextCalculator.SameValues(before, MovementContextCalculator.Capture(changed, Owner.Player, CombatTraitTestFixtures.ThornRules)), Is.False);
        }

        [Test]
        public void DeathClearsExistingStatus_AndOrdinaryFieldDoesNotAddOne()
        {
            var attack = CombatTraitTestFixtures.KnockbackInput(health: 1);
            attack = CombatCommandTestData.With(attack, attack.Characters.Select(c => c.EntityId.Value == 2
                ? CombatDebuffApplication.WithDebuffs(c, new CharacterDebuffState(defenseReduction: new DefenseReductionDebuffState(30, 2))) : c).ToArray());
            Assert.That(Run(attack).Characters.Single(c => c.EntityId.Value == 2).Debuffs.DefenseReduction, Is.Null);
            var input = CombatTraitTestFixtures.ThornMovement(0);
            Assert.That(Run(input).Characters.Single(c => c.EntityId == input.ActorId).Debuffs.DefenseReduction, Is.Null);
        }

        [Test]
        public void DeadContactTargetReceivesNoStatus()
        {
            var input = CombatTraitTestFixtures.ThornMovement(hp: 7);
            var actor = Run(input).Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(actor.Health.IsDead, Is.True);
            Assert.That(actor.Debuffs.DefenseReduction, Is.Null);
        }

   }
}
