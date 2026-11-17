using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class JudgmentInteractionTests
    {
        [Test]
        public void CollisionKeepsItsOwnSourceWhileJudgmentOverridesItsLongerDistance()
        {
            var wall = new StaticTerrainFact(71, new CellCoord(5, 5), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });
            var input = MagicBulletTestData.Input(new[] { new CellCoord(5, 3), new CellCoord(5, 4) }, PlayerSkillUpgradeKey.Judgment,
                targets: new[] { MagicBulletSkillTests.Target(2, 5, 4) }, terrain: new[] { wall },
                extraTrait: new CrashImpactTraitSpec(4, 10, 10));
            var result = PowderBlossomFixture.Calculate(input);
            var knockback = PowderBlossomFixture.Attacks(result).Single().Knockbacks.Single();
            Assert.That(knockback.RequestedDistance, Is.EqualTo(2));
            Assert.That(knockback.Step, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(knockback.Path, Is.Empty);
            Assert.That(knockback.Collision, Is.Not.Null);
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Any(t => t.TraitKey == TraitKey.CrashImpact && t.TraitActivationKind == TraitActivationKind.CrashImpactCollided), Is.True);
        }

        [Test]
        public void ExistingImmunityPreventsMovementButNotTheHit()
        {
            var input = Input();
            var equipped = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new SteadfastWillTraitSpec());
            input = MagicBulletTestData.Copy(input, characters: equipped.Characters.ToArray(), profiles: equipped.Profiles.ToArray());
            var attack = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single();
            Assert.That(attack.Hits.Single().DamageDealt, Is.EqualTo(150));
            Assert.That(attack.Knockbacks.Single().Stop, Is.EqualTo(AttackKnockbackStop.Immune));
        }

        [Test]
        public void HazardAndMineAreConsumedOnTheActualForcedPath()
        {
            var input = Input();
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { MineEnvironmentTests.Mine(71, damage: 7, cell: new CellCoord(5, 6)) });
            var state = EnvironmentDamageTests.WithDamageObjects(input.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment)), (new CellCoord(5, 5), 6));
            var bindings = new EnvironmentSemanticBindings(Array.Empty<long>(), new long[] { 71 }, Array.Empty<EntityId>());
            input = input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls,
                EnvironmentDamageTests.WithDamageObjectBindings(bindings, state));
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules.Concat(EnvironmentDamageTests.DamageRules).ToArray());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(EnvironmentDamageTests.ActiveDamageTiles(result.FinalState), Is.Empty);
            Assert.That(result.FinalState.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.False);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId.Value == 2).Final.Health.CurrentHealth, Is.EqualTo(837));
            CombatHealthEvidenceValidator.Validate(result);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void IncomingSegmentOverridesLongerAndImpactCenterDisplacement(int trait)
        {
            TraitSpec extra = trait == 1 ? new KnockbackTraitSpec(5) : trait == 2 ? new ImpactBoltTraitSpec(5) : null;
            var input = Input(extra: extra);
            var result = PowderBlossomFixture.Calculate(input);
            var attack = PowderBlossomFixture.Attacks(result).Single();
            var move = attack.Knockbacks.Single();
            Assert.That(move.RequestedDistance, Is.EqualTo(2));
            Assert.That(move.Path, Is.EqualTo(new[] { new CellCoord(5, 5), new CellCoord(5, 6) }));
            Assert.That(move.Step, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(attack.Hits.Count, Is.EqualTo(1));
            Assert.That(attack.ProjectilePath.Last(), Is.EqualTo(new CellCoord(5, 4)));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved)
                .All(e => e.MovementCause.Agency == CombatMovementAgency.Forced), Is.True);
        }

        [Test]
        public void JudgmentBypassesOnlyPhysicalDefense()
        {
            var terrain = new StaticTerrainFact(71, new CellCoord(5, 4), StaticTerrainKind.Sandstorm, true, new[] { Owner.Player, Owner.Enemy });
            var input = Input(terrain: new[] { terrain });
            var result = PowderBlossomFixture.Calculate(input);
            var damage = PowderBlossomFixture.Attacks(result).Single().DamageByRecipient[new EntityId(2)];
            Assert.That(damage.DamageBeforeMatchup, Is.EqualTo(150));
            Assert.That(damage.DamageAfterPhysicalDefense, Is.EqualTo(150));
            Assert.That(damage.FinalDamage, Is.EqualTo(120));
        }

        [TestCase(StaticTerrainKind.Wall, 0)]
        [TestCase(StaticTerrainKind.Cliff, 1)]
        public void BlockerOrLethalTerrainStopsTheSharedSteppedMove(StaticTerrainKind kind, int steps)
        {
            var terrain = new StaticTerrainFact(71, new CellCoord(5, 5), kind, true, new[] { Owner.Player, Owner.Enemy });
            var result = PowderBlossomFixture.Calculate(Input(terrain: new[] { terrain }));
            var move = PowderBlossomFixture.Attacks(result).Single().Knockbacks.Single();
            Assert.That(move.Path.Count, Is.EqualTo(steps));
            Assert.That(move.Stop, Is.EqualTo(kind == StaticTerrainKind.Wall ? AttackKnockbackStop.Blocked : AttackKnockbackStop.Interrupted));
            CombatHealthEvidenceValidator.Validate(result);
        }

        [Test]
        public void FirstHitDeathDoesNotResumeProjectile()
        {
            var input = Input();
            input = MagicBulletTestData.Copy(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(1, 1000)) : c).ToArray());
            var attack = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single();
            Assert.That(attack.Hits.Count, Is.EqualTo(1));
            Assert.That(attack.Hits.Single().TargetKilled, Is.True);
            Assert.That(attack.ProjectilePath.Last(), Is.EqualTo(new CellCoord(5, 4)));
        }

        internal static CombatCommandInput Input(TraitSpec extra = null, StaticTerrainFact[] terrain = null)
        {
            var target = MagicBulletSkillTests.Target(2, 5, 4);
            target = target.With(stats: target.Stats.With(physicalDefense: 80));
            return MagicBulletTestData.Input(new[] { new CellCoord(5, 3), new CellCoord(5, 4) }, PlayerSkillUpgradeKey.Judgment,
                targets: new[] { target, MagicBulletSkillTests.Target(3, 5, 7) }, extraTrait: extra, terrain: terrain);
        }
    }
}
