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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class EnhancementTargetDamageTests
    {

        internal static CombatCommandInput Input(TraitSpec[] traits, TraitStatusSnapshot[] statuses,
            int distance = 1, int attack = 10, int victimAttack = 1, int defense = 50)
        {
            var input = CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(1 + distance, 3)), empty: true);
            var actor = CombatTraitTestFixtures.Stats(CombatCommandTestData.With(input.Characters[0], traits: statuses), attack: attack);
            var victim = CombatTraitTestFixtures.Stats(CombatCommandTestData.Character(2, EntityKey.Unit_Demon,
                new CellCoord(1 + distance, 3), Owner.Enemy), defense, victimAttack);
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, attack,
                CategoryKey.Main, traits), profiles[0].Method);
            return CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], victim }, profiles: profiles);
        }

        internal static int Damage(CombatCommandInput input)
        {
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId.Value == 2);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId.Value == 2).TargetHealthAfter,
                Is.EqualTo(hit.TargetHealthAfter));
            return hit.DamageDealt;
        }


        [TestCase(true, 7, 11, 20)]
        [TestCase(true, 7, 10, 20)]
        [TestCase(false, 10, 6, 20)]
        public void Multiplier_CountsTemporaryAttackOnceOnEitherSide(bool buffActor, int attack, int victimAttack, int expected)
        {
            var input = Input(new TraitSpec[] { new StationaryFireTraitSpec(100) },
                new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot() }, attack: attack, victimAttack: victimAttack, defense: 0);
            input = CombatCommandTestData.With(input, input.Characters.Select(c =>
            {
                if (c.EntityId.Value != (buffActor ? 1 : 2)) return c;
                var s = c.Stats;
                var stats = new UnitStatsSnapshot(s.MoveMethod,s.MoveRange,s.AppointRange,                    s.AttackPower,s.PhysicalDefense,s.Priority,s.TraitStatuses.ToArray(),s.Category,                    artifactAttackBonus: 3,effectiveAttackPower: s.AttackPower + 3);
                return new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                    c.Area, c.Health, stats, c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.ActionUse, temporaryAttackBonus: 3);
            }).ToArray());
            Assert.That(Damage(input), Is.EqualTo(expected));
        }

        [TestCase(1, 0, 100, 2)]
        [TestCase(0, 0, 100, 0)]
        [TestCase(3, 1, 50, 2)]
        public void Multiplier_IsAfterMinimumAndDefense(int attack, int victimAttack, int defense, int expected)
        {
            Assert.That(Damage(Input(new TraitSpec[] { new StationaryFireTraitSpec(100) },
                new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot() }, attack: attack,
                victimAttack: victimAttack, defense: defense)), Is.EqualTo(expected));
        }

        [Test]
        public void Multiplier_PreservesMinimumAbsorptionAndItems()
        {
            var input = Input(new TraitSpec[] { new StationaryFireTraitSpec(100) },
                new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot() }, attack: 30, defense: 0);
            input = CombatItemDirectDamageTests.Items(CombatCommandTestData.With(input, input.Characters.Select(c =>
                c.EntityId.Value == 2 ? CombatItemDirectDamageTests.Equip(c, 100, 2) : c).ToArray()));
            Assert.That(Damage(input), Is.EqualTo(30));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(CombatItemDirectDamageTests.Final(result, 2).Items.EquippedItemKey, Is.Null);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray());
            Assert.That(Damage(input), Is.Zero);
            result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(CombatItemDirectDamageTests.Final(result, 2).Items.EquippedItemKey, Is.EqualTo(2));
        }

        [Test]
        public void Terrain_PreservesReductionBeforeFinalMultiplier()
        {
            var input = Input(new TraitSpec[] { new StationaryFireTraitSpec(100) },
                new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot() }, defense: 50);
            input = StaticTerrainIntegrationTests.Terrain(input,
                StaticTerrainIntegrationTests.Tile(71, new CellCoord(2, 3), StaticTerrainKind.HighGround));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var victim = input.Characters.Single(c => c.EntityId.Value == 2);
            var damage = CombatDamageCalculator.Attack(input, new CombatEffectEngine(),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                actor, victim, 10, AttackTargetRole.Primary, 100, actor.Anchor, null);
            Assert.That(damage.DamageAfterPhysicalDefense, Is.EqualTo(5));
            Assert.That(damage.IncomingReduction, Is.Zero);
            Assert.That(damage.DamageAfterReceivedPercentages, Is.EqualTo(3.5m));
            Assert.That(damage.FinalDamage, Is.EqualTo(6));
        }

        [TestCase(true, 4)]
        [TestCase(false, 10)]
        public void Multiplier_UsesCurrentDebuffedAttack_AndRetainsIntimidation(bool debuffActor, int expected)
        {
            var input = Input(new TraitSpec[] { new StationaryFireTraitSpec(100), new IntimidationTraitSpec(20) },
                new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot(), new IntimidationTraitStatusSnapshot(20) }, victimAttack: 9);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == (debuffActor ? 1 : 2)
                ? CombatDebuffApplication.WithDebuffs(c, new CharacterDebuffState(new IntimidationDebuffState(50))) : c).ToArray());
            Assert.That(Damage(input), Is.EqualTo(expected));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var victim = CombatItemDirectDamageTests.Final(result, 2);
            Assert.That(victim.Debuffs.Intimidation.AttackReductionPercent, Is.EqualTo(debuffActor ? 20 : 50));
        }
    }
}
