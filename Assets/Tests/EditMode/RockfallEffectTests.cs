using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class RockfallEffectTests
    {
        [Test]
        public void ZeroLossRefreshWithIdenticalBindStillHasImpactEvidence()
        {
            var input = Impact();
            var first = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Enemy, 4));
            var actor = CombatCommandComposition.Project(first).Characters.Single(c => c.EntityId.Value == 1)
                .WithHealth(new HealthStateSnapshot(4, 4));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray());
            var second = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Enemy, 4));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(second.Events));
        }

        [Test]
        public void BindDoesNotBlockAttackAndImpactDoesNotRetargetMovedVictims()
        {
            var input = Impact();
            var result = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Enemy, 4));
            var attack = CombatCommandTestData.FreshCommand(CombatCommandComposition.Project(result), CombatCommandKind.Attack);
            Assert.That(CombatCommandCalculator.Calculate(attack, Array.Empty<IMovementTileRule>()).Accepted, Is.True);
            var actor = input.Characters[0];
            var moved = actor.WithPlacement(actor.Area, null, new CellCoord(0, 0), actor.Rotation, new[] { CellCoord.Zero });
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? moved : c).ToArray());
            var empty = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Enemy, 4));
            Assert.That(empty.Events.OfType<CombatRockfallEvent>().Single().RecipientIds, Is.Empty);
            Assert.That(CombatCommandComposition.Project(empty).Characters.Single(c => c.EntityId == actor.EntityId).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(100, 100, 80)]
        [TestCase(7, 7, 6)]
        [TestCase(4, 4, 4)]
        [TestCase(5, 100, 0)]
        public void DirectHealthLossIgnoresShieldAndFloorsOnce(int hp, int max, int expected)
        {
            var input = Impact();
            var victim = input.Characters[0].With(health: new HealthStateSnapshot(hp, max),
                stats: input.Characters[0].Stats.With(physicalDefense: 999)).WithStatusEffects(
                new StatusEffectSet(new AppliedStatus[] { new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Item, 1, 1, "shield"), 100, true, 0) }));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == victim.EntityId ? victim : c).ToArray());
            var result = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Enemy, 4));
            var after = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == victim.EntityId);
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(expected));
            if (expected > 0)
            {
                Assert.That(after.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.EqualTo(100));
                Assert.That(after.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Single().Key, Is.EqualTo(StatusEffectKey.Bind));
            }
            Assert.That(result.Events.OfType<CombatCharacterEvent>().SelectMany(e => e.DamageOrigins), Is.Empty);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [TestCase(Owner.Player, Owner.Player)]
        [TestCase(Owner.Player, Owner.Enemy)]
        [TestCase(Owner.Enemy, Owner.Player)]
        [TestCase(Owner.Enemy, Owner.Enemy)]
        public void BindKeepsOneFutureAffectedSideEnd(Owner victimSide, Owner endedSide)
        {
            var input = Impact(victimSide == Owner.Player ? 1 : 2);
            var result = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(endedSide, 4));
            var victim = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == (victimSide == Owner.Player ? 1 : 2));
            var installed = AppliedStatusRules.EndControllerTurn(victim.StatusEffects, victimSide, endedSide);
            var bind = installed.Entries.OfType<ActionRestrictionStatus>().Single();
            Assert.That(bind.RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            Assert.That(bind.Origin.Value.Kind.ToString(), Is.EqualTo("StageGimmick"));
            Assert.That(AppliedStatusRules.IsRestricted(installed, StatusEffectKey.Disarm), Is.False);
            Assert.That(AppliedStatusRules.EndControllerTurn(installed, victimSide, victimSide == Owner.Player ? Owner.Enemy : Owner.Player).SameValues(installed), Is.True);
            Assert.That(AppliedStatusRules.EndControllerTurn(installed, victimSide, victimSide).Entries, Is.Empty);
            Assert.That(AppliedStatusRules.OnAreaChanged(installed, CharacterRuntimeArea.Field,
                victimSide == Owner.Player ? CharacterRuntimeArea.Hand : CharacterRuntimeArea.EnemyWaiting).Entries, Is.Empty);
        }

        internal static CombatCommandInput Impact(int victim = 1)
        {
            var input = RockfallGimmickTests.At(RockfallGimmickTests.Input(), 4);
            var cell = input.Characters.Single(c => c.EntityId.Value == victim).Anchor.Value;
            return CombatCommandTestData.With(input, state: input.State.With(gimmicks: input.State.Gimmicks.Replace(
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 4, new[] { cell }))));
        }
    }
}
