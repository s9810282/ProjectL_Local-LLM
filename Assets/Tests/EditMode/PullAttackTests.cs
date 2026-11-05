using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class PullAttackTests
    {
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void OuterEnemy_MovesBeforeDirectDamage(int x, int y)
        {
            var input = PullTestData.Input(3, 2, 5, new CellCoord(x, y));
            var result = PullTestData.Run(input);
            var expected = new CellCoord(7 + x * 3, 7 + y * 3);
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(expected));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(80));
            var health = result.Events.OfType<CombatCharacterEvent>().First(e => e.Kind == CombatEventKind.HealthChanged);
            Assert.That(health.Expected.Anchor, Is.EqualTo(expected));
            var events = result.Events.ToArray();
            int pullIndex = Array.FindIndex(events, e => e is CombatPullEvent);
            int attackIndex = Array.FindIndex(events, e => e is CombatAttackEvent);
            Assert.That(pullIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(pullIndex, Is.LessThan(attackIndex));
            Assert.That(attackIndex, Is.LessThan(Array.IndexOf(events, health)));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved), Is.False);
            Assert.That(PullTestData.Final(result).ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [Test]
        public void BlockedRoute_NoMovementOrDamage()
        {
            var input = PullTestData.Input(extra: 3, distance: 4);
            input = CombatCommandTestData.With(input, input.Characters.Append(
                CombatCommandTestData.Character(55, EntityKey.Unit_Demon, new CellCoord(9, 7), Owner.Enemy)).ToArray());
            var result = CombatCommandCalculator.Calculate(input, PullTestData.NoRules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("PullPathBlocked"));
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(11, 7)));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void FriendlyPull_DoesNotTouchShieldReflectionMarkOrVictimAction()
        {
            var input = PullTestData.Input(owner: Owner.Player);
            var mark = new HuntersMarkState(new EntityId(99), Owner.Enemy, 25);
            var victim = HeldEnvironmentEffects.AcquireBadge(input.Characters.Single(c => c.EntityId == PullTestData.Target), 41)
                .With(debuffs: new CharacterDebuffState(huntersMark: mark), actionUse: UnitActionUse.Moved);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == victim.EntityId ? victim : c).ToArray(),
                artifacts: ThornHeartTestData.Input().Artifacts);
            var result = PullTestData.Run(input);
            var final = PullTestData.Final(result);
            Assert.That(final.Anchor, Is.EqualTo(new CellCoord(8, 7)));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(final.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(final.Debuffs.HuntersMark.SameValues(mark), Is.True);
            Assert.That(final.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(final.Stats.SameValues(victim.Stats), Is.True);
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Any(e => e.Reflection != null), Is.False);
            Assert.That(result.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HeavyAloneCanBePulled_ExplicitImmunityRejects(bool immune)
        {
            var input = PullTestData.TargetCategory(PullTestData.Input(), CategoryKey.Heavy, immune);
            var result = CombatCommandCalculator.Calculate(input, PullTestData.NoRules);
            Assert.That(result.Accepted, Is.EqualTo(!immune), result.Reason);
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(immune ? 9 : 8, 7)));
        }

        [Test]
        public void OldSelectedCell_IsNeverRetargeted()
        {
            var input = PullTestData.Input();
            var result = PullTestData.Run(input);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { PullTestData.Target }));
            Assert.That(result.Attack.Value.AffectedCells.ToArray(), Is.EqualTo(new[] { new CellCoord(8, 7) }));
            Assert.That(result.Attack.Value.AffectedCells.ToArray(), Has.No.Member(input.Aim.Cell.Value));
        }

        [TestCase(EntityKey.Unit_Akimbo)]
        [TestCase(EntityKey.Unit_ShotGun)]
        public void EquivalentPullTraitOnDifferentUnitKey_ExecutesSameMovement(EntityKey key)
        {
            var result = PullTestData.Run(PullTestData.Input(key: key));
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(8, 7)));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void InnerEnemy_KeepsOrdinaryAttackWithoutMovement()
        {
            var result = PullTestData.Run(PullTestData.Input(distance: 1));
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(8, 7)));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Events.Any(e => e.GetType().Name == "CombatPullEvent"), Is.False);
        }
    }
}
