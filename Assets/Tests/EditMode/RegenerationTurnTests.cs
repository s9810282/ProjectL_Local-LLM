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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class RegenerationTurnTests
    {
        [TestCase(CombatControllerPhase.Player)]
        [TestCase(CombatControllerPhase.Enemy)]
        public void SideEnd_HealsOnlyTheLivingFieldHoldersOfItsOwnSide(CombatControllerPhase phase)
        {
            var source = Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 10, 40, 100);
            source = Regeneration(source, EntityKey.Unit_Demon, 10, 40, 100);
            var input = EnemyControllerPhaseTests.Input(source, phase, completed: phase == CombatControllerPhase.Enemy ? 1 : 0);
            var result = phase == CombatControllerPhase.Player
                ? CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>())
                : CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Single(c => c.EntityId.Value == 1).Health.CurrentHealth,
                Is.EqualTo(phase == CombatControllerPhase.Player ? 50 : 40));
            Assert.That(final.Characters.Single(c => c.EntityId.Value == 2).Health.CurrentHealth,
                Is.EqualTo(phase == CombatControllerPhase.Enemy ? 50 : 40));
            var recoveries = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.Regeneration).ToArray();
            Assert.That(recoveries.Select(t => t.SourceUnitId.Value), Is.EquivalentTo(phase == CombatControllerPhase.Player ? new[] { 1 } : new[] { 2, 3, 4 }));
            var recovery = recoveries.Single(t => t.SourceUnitId.Value == (phase == CombatControllerPhase.Player ? 1 : 2));
            Assert.That(recovery.TraitActivationKind, Is.EqualTo(TraitActivationKind.HealthRecovered));
            Assert.That(recovery.Magnitude, Is.EqualTo(10));
            Assert.That(source.Characters.Single(c => c.EntityId.Value == 1).Health.CurrentHealth, Is.EqualTo(40));
        }

        [TestCase(CombatControllerPhase.StartingPlayer)]
        [TestCase(CombatControllerPhase.StartingEnemy)]
        public void SideStart_ResetsEligibilityWithoutHealing(CombatControllerPhase phase)
        {
            var source = Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 3, 60, 100);
            source = Regeneration(source, EntityKey.Unit_Demon, 9, 40, 100);
            source = CombatCommandTestData.With(source, characters: source.Characters
                .Select(c => c.With(actionUse: UnitActionUse.Moved)).ToArray());
            var result = CombatTurnCalculator.BeginControllerTurn(EnemyControllerPhaseTests.Input(source, phase), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            foreach (var before in source.Characters)
            {
                var after = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == before.EntityId);
                Assert.That(after.Health, Is.EqualTo(before.Health));
                bool starts = before.Owner == (phase == CombatControllerPhase.StartingPlayer ? Owner.Player : Owner.Enemy);
                Assert.That(after.ActionUse, Is.EqualTo(starts ? UnitActionUse.Available : UnitActionUse.Moved));
            }
        }

        [TestCase(1)]
        [TestCase(60)]
        [TestCase(100)]
        public void PreviewMoveAndAttack_DoNotHealTheHolder(int health)
        {
            var input = Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 7, health, 100);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            var attack = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var move = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(2, 3)), Array.Empty<IMovementTileRule>());
            foreach (var result in new[] { attack, move })
            {
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == 1)
                    .Health.CurrentHealth, Is.EqualTo(health));
                Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.HealthChanged &&
                    e.Final.Health.CurrentHealth > e.Expected.Health.CurrentHealth), Is.False);
            }
            Assert.That(input.Characters[0].Health.CurrentHealth, Is.EqualTo(health));
        }

        [Test]
        public void OwnSideEnd_CapsRecoveryAndDoesNotRepeatOnRejectedReplay()
        {
            var source = Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 10, 95, 100);
            var result = CombatTurnCalculator.EndPlayerTurn(EnemyControllerPhaseTests.Input(source, CombatControllerPhase.Player), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Single(c => c.EntityId.Value == 1).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitKey == TraitKey.Regeneration).Magnitude, Is.EqualTo(5));
            var rejected = CombatTurnCalculator.EndPlayerTurn(final, Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
            Assert.That(rejected.Events, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OwnSideEnd_DeadAndOffFieldHoldersCannotHeal(bool dead)
        {
            var source = Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 10, dead ? 0 : 40, 100);
            source = Regeneration(source, EntityKey.Unit_Demon, 10, dead ? 0 : 40, 100);
            if (!dead) source = CombatCommandTestData.With(source, characters: source.Characters.Select(c =>
                c.EntityId.Value == 1 ? c.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()) :
                c.Owner == Owner.Enemy ? c.WithPlacement(CharacterRuntimeArea.EnemyWaiting, null, null, null, Array.Empty<CellCoord>()) : c).ToArray());
            var playerEnd = CombatTurnCalculator.EndPlayerTurn(EnemyControllerPhaseTests.Input(source, CombatControllerPhase.Player), Array.Empty<IMovementTileRule>());
            var enemyEnd = CombatTurnCalculator.EndEnemyPhase(EnemyControllerPhaseTests.Input(source, CombatControllerPhase.Enemy), Array.Empty<IMovementTileRule>());
            foreach (var result in new[] { playerEnd, enemyEnd })
            {
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                    .Where(t => t.TraitKey == TraitKey.Regeneration), Is.Empty);
                Assert.That(CombatCommandComposition.Project(result).Characters.Where(c => c.EntityId.Value == 1 || c.EntityId.Value == 2)
                    .Select(c => c.Health.CurrentHealth), Is.All.EqualTo(dead ? 0 : 40));
            }
        }

        [TestCase(false, 7)]
        [TestCase(false, 1000)]
        [TestCase(true, 1000)]
        public void EndEnvironment_ResolvesDamageAndOutcomeBeforeRegeneration(bool killMain, int damage)
        {
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, killMain ? new CellCoord(3, 0) : CellCoord.Zero, Owner.Player);
            var main = CombatCommandTestData.Character(10, EntityKey.Unit_MainCharacter, killMain ? CellCoord.Zero : new CellCoord(3, 0), Owner.Player, main: true);
            var source = TornadoEnvironmentTests.Input(completed: 0, sequence: 3, characters: new[] { actor, main }, width: 4, height: 1,
                sources: new MovementTileFact[] { TornadoEnvironmentTests.Source(cell: CellCoord.Zero),
                    new StaticTerrainFact(33, new CellCoord(2, 0), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }),
                    new MineSourceFact(32, CellCoord.Right, 1, damage, true, new[] { Owner.Player, Owner.Enemy }) });
            source = Regeneration(source, EntityKey.Unit_Akimbo, 10, 40, 100);
            source = Regeneration(source, EntityKey.Unit_MainCharacter, 10, 60, 100);
            source = CombatCommandTestData.With(source, main: source.Main.WithCurrentHealth(60));
            var result = CombatTurnCalculator.EndPlayerTurn(source, TornadoEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Single(c => c.EntityId.Value == 1).Health.CurrentHealth, Is.EqualTo(killMain ? 40 : damage == 7 ? 43 : 0));
            Assert.That(final.Main.CurrentHealth, Is.EqualTo(killMain ? 0 : 70));
            var events = result.Events.ToList();
            int damaged = events.FindIndex(e => e is CombatCharacterEvent c && c.Final.Health.CurrentHealth < c.Expected.Health.CurrentHealth);
            Assert.That(damaged, Is.GreaterThanOrEqualTo(0));
            var recoveries = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitKey == TraitKey.Regeneration).ToArray();
            if (killMain)
            {
                Assert.That(result.Outcome.IsStageEnded, Is.True);
                Assert.That(recoveries, Is.Empty);
                Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
                Assert.That(CombatTurnCalculator.BeginControllerTurn(final, Array.Empty<IMovementTileRule>()).Accepted, Is.False);
            }
            else
            {
                Assert.That(result.Outcome.IsStageEnded, Is.False);
                int healed = events.FindIndex(e => e is CombatCharacterEvent c && c.Final.Health.CurrentHealth > c.Expected.Health.CurrentHealth);
                Assert.That(healed, Is.GreaterThan(damaged));
                Assert.That(recoveries.Length, Is.EqualTo(damage == 7 ? 2 : 1));
            }
        }

        internal static CombatCommandInput Regeneration(CombatCommandInput input, EntityKey key, int amount, int hp, int maximum,
            int interval = 1, int progress = 0)
        {
            var configured = Trait(input, key, new RegenerationTraitSpec(amount, interval), new RegenerationTraitStatusSnapshot(amount, interval, progress));
            return CombatCommandTestData.With(configured, characters: configured.Characters.Select(c =>
                c.CharacterKey == key ? Health(c, hp, maximum) : c).ToArray());
        }

        private static CombatCommandInput Trait(CombatCommandInput input, EntityKey key, TraitSpec spec, TraitStatusSnapshot status)
        {
            var characters = input.Characters.Select(c => c.CharacterKey == key ? CombatCommandCalculator.Copy(c, c.Health,
                CombatCommandEffects.With(c.Stats, traits: c.Stats.TraitStatuses.ToArray().Append(status).ToArray())) : c).ToArray();
            var profiles = input.Profiles.Select(p =>
            {
                if (p.Definition.Key != key) return p;
                var d = p.Definition;
                return new CombatAttackProfile(new UnitSpec(d.Key, d.Role, d.FootprintWidth, d.FootprintHeight,
                    d.FootprintLocalCells.ToArray(), d.MaxHealth, d.MoveMethod, d.AttackRange,
                    d.CategoryKey, d.AttackPower, d.PhysicalDefense, d.Priority, d.Traits.ToArray().Append(spec).ToArray(), d.OwnerEligibility, d.Occlusion, d.Behavior), p.Method);
            }).ToArray();
            return CombatCommandTestData.With(input, characters: characters, profiles: profiles);
        }

        private static CharacterCombatFacts Health(CharacterCombatFacts c, int hp, int maximum) =>
            CombatCommandCalculator.Copy(c, new HealthStateSnapshot(hp, maximum), c.Stats);


    }
}
