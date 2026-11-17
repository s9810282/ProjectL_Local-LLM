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
            var result = CombatCommandCalculator.Calculate(input, CombatTraitTestFixtures.DamageRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            return CombatCommandComposition.Project(result);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MovementContextRejectsChangedHazardOrStatusPayload(bool changeStatus)
        {
            var input = CombatTraitTestFixtures.HazardMovement();
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State,
                input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                path: input.Path, battleInstanceId: 1,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence);
            var before = MovementContextCalculator.Capture(input, Owner.Player, CombatTraitTestFixtures.DamageRules);
            var changed = changeStatus ? CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatDebuffApplication.WithDebuffs(c, new CharacterDebuffState(defenseReduction: new DefenseReductionDebuffState(20, 1))) : c).ToArray())
                : CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageTiles(input.State, (new CellCoord(2, 3), 8)));
            Assert.That(MovementContextCalculator.SameValues(before, MovementContextCalculator.Capture(changed, Owner.Player, CombatTraitTestFixtures.DamageRules)), Is.False);
        }

        [Test]
        public void DeathClearsExistingStatus_AndHazardDoesNotAddOne()
        {
            var attack = CombatTraitTestFixtures.KnockbackInput(health: 1);
            attack = CombatCommandTestData.With(attack, attack.Characters.Select(c => c.EntityId.Value == 2
                ? CombatDebuffApplication.WithDebuffs(c, new CharacterDebuffState(defenseReduction: new DefenseReductionDebuffState(30, 2))) : c).ToArray());
            Assert.That(Run(attack).Characters.Single(c => c.EntityId.Value == 2).Debuffs.DefenseReduction, Is.Null);
            var input = CombatTraitTestFixtures.HazardMovement();
            Assert.That(Run(input).Characters.Single(c => c.EntityId == input.ActorId).Debuffs.DefenseReduction, Is.Null);
        }

        [Test]
        public void DeadContactTargetReceivesNoStatus()
        {
            var input = CombatTraitTestFixtures.HazardMovement(hp: 7);
            var actor = Run(input).Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(actor.Health.IsDead, Is.True);
            Assert.That(actor.Debuffs.DefenseReduction, Is.Null);
        }

   }
}
