using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandSacrificeTests
    {
        [Test]
        public void RedirectUsesTheSweptTargetsDistanceForBothPlanningAndDamage()
        {
            var input = Input(new CellCoord(7, 2));
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(p.Definition, method) : p).ToArray());
            input = CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(3, 2)));
            input = DeadHandDamageTests.WithDistanceScale(input, new[] { 50, 150 });

            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var swept = result.Attack.Value.Sweep.Targets.Single();
            Assert.That(swept.FinalAnchor, Is.EqualTo(new CellCoord(6, 2)));
            Assert.That(swept.Stop, Is.EqualTo(AttackSweepStop.Completed));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(91));
            Assert.That(Used(result), Is.EqualTo(1));
        }

        [TestCase(6, 2, true)]
        [TestCase(2, 1, false)]
        public void ProtectionUsesPostMovementPosition(int x, int y, bool protects)
        {
            var input = Input(new CellCoord(x, y));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(protects ? 5 : 0));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(protects ? 94 : 100));
            Assert.That(TraitCombatFixtures.Final(result, 5).Anchor, Is.EqualTo(new CellCoord(x, y)));
            Assert.That(result.Attack.Value.Sweep.CapturedTargetIds, Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(Used(result), Is.EqualTo(protects ? 1 : 0));
        }

        [Test]
        public void CapturedProtectorMovesOnceAndReceivesBothHits()
        {
            var result = TraitCombatFixtures.Attack(Input(new CellCoord(4, 2)));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Sweep.Targets.Count(t => t.TargetId.Value == 5), Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 5).Anchor, Is.EqualTo(new CellCoord(5, 2)));
            Assert.That(TraitCombatFixtures.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(4, 2)));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(88));
            Assert.That(Used(result), Is.EqualTo(1));
        }

        [Test]
        public void ExistingCapacityAllowsOnlyOneRedirect()
        {
            var input = Input(new CellCoord(6, 2), radius: 2);
            var extra = CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Demon,
                new CellCoord(2, 2), Owner.Player), hp: 5);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(extra).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Used(result), Is.EqualTo(1));
            Assert.That(new[] { 2, 3 }.Count(id => TraitCombatFixtures.Final(result, id).Health.IsDead), Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(94));
        }

        private static int Used(CombatCommandTransition result) => TraitCombatFixtures.Final(result, 5).Stats.TraitStatuses.ToArray()
            .OfType<SacrificeTraitStatusSnapshot>().Single().ActivationsThisEnemyTurn;

        private static CombatCommandInput Input(CellCoord guardCell, int radius = 1)
        {
            var basis = DeadHandTestData.Input(active: true);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo,
                new CellCoord(3, 1), Owner.Enemy, attack: 6), appointRange: 2,
                traits: new TraitStatusSnapshot[] { new DeadHandTraitStatusSnapshot() });
            var victim = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon,
                new CellCoord(3, 2), Owner.Player), hp: 5);
            var guard = CombatCommandTestData.Character(5, EntityKey.Unit_Shielder, guardCell, Owner.Player);
            var characters = new[] { actor, basis.Characters.Single(c => c.EntityId.Value == 10), victim, guard };
            var profiles = basis.Profiles.Append(new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Shielder, 1),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, Array.Empty<StructureCombatFacts>(),
                new CombatTurnFacts(StagePhase.Combat,
                    controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1));
            var input = new CombatCommandInput(0, CombatCommandKind.Attack, actor.EntityId, basis.Aim, state, characters, profiles,
                basis.Main, characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value,
                    c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0),
                basis.Souls, basis.Artifacts, processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: 1);
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(radius, 1));
        }
    }
}
