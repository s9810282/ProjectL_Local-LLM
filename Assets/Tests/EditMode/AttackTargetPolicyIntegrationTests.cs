using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
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
    public sealed class AttackTargetPolicyIntegrationTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord Center = new CellCoord(4, 3);
        private static readonly EntityId AllyId = new EntityId(3);
        private static readonly CellCoord AllyCell = new CellCoord(5, 3);

        [TestCase(CategoryKey.Base, TargetingSplash.None)]
        [TestCase(CategoryKey.Base, TargetingSplash.Radial)]
        [TestCase(CategoryKey.Rider, TargetingSplash.None)]
        [TestCase(CategoryKey.Rider, TargetingSplash.Radial)]
        [TestCase(CategoryKey.Heavy, TargetingSplash.None)]
        [TestCase(CategoryKey.Heavy, TargetingSplash.Radial)]
        public void NonCannonThornField_AlliedOccupantHasNoDamageOrPublishedHit(CategoryKey category, TargetingSplash splash)
        {
            var input = ThornInput(category, splash);
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(Final(result, AllyId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(AllyId));
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Has.No.Member(AllyId));
            Assert.That(DamageEvents(result, AllyId), Is.Empty);
            Assert.That(result.FinalState.ThornFields.Select(f => f.Cell), Has.No.Member(AllyCell));
            Assert.That(Final(result, input.ActorId).Stats.CurrentActionCost, Is.EqualTo(3));
            AssertHostileThornDamage(input, preview, result, splash);
        }

        [TestCase(TargetingSplash.None, 5)]
        [TestCase(TargetingSplash.Radial, 19)]
        public void CannonThornField_AlliedOccupantReceivesImmediateDamageWithActorOrigin(TargetingSplash splash, int damage)
        {
            var input = ThornInput(CategoryKey.Cannon, splash);
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, AllyId).Health.CurrentHealth, Is.EqualTo(100 - damage));
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == AllyId);
            Assert.That(hit.Role, Is.EqualTo(AttackTargetRole.Additional));
            Assert.That(hit.DamageDealt, Is.EqualTo(damage));
            var predicted = preview.Targets.ToArray().Single(t => t.TargetId == AllyId);
            Assert.That(predicted.ExpectedDamage, Is.EqualTo(damage));
            Assert.That(predicted.SecondaryDamage, Is.EqualTo(5));
            var thorn = DamageEvents(result, AllyId).Last().DamageOrigins.Single();
            Assert.That(thorn.PhysicalCell, Is.EqualTo(AllyCell));
            Assert.That(thorn.CreditedCharacterId, Is.EqualTo(input.ActorId));
            Assert.That(thorn.AttackGroup, Is.GreaterThan(1));
            Assert.That(result.FinalState.ThornFields.Select(f => f.Cell), Has.No.Member(AllyCell));
            Assert.That(Final(result, input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(input.ActorId));
            AssertHostileThornDamage(input, preview, result, splash);
        }

        [Test]
        public void EnemyThornField_UsesTheSameFriendlyRestriction()
        {
            var input = ThornInput(CategoryKey.Base, TargetingSplash.None, Owner.Enemy);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, AllyId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(DamageEvents(result, AllyId), Is.Empty);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(AllyId));
            Assert.That(Final(result, new EntityId(4)).Health.CurrentHealth, Is.EqualTo(95));
        }

        [TestCase(TargetingSplash.None, 5)]
        [TestCase(TargetingSplash.Radial, 19)]
        public void CannonThornField_LegalOuterAimCanDamageAlliedNearOccupant(TargetingSplash splash, int damage)
        {
            var input = ThornInput(CategoryKey.Cannon, splash);
            var center = new CellCoord(3, 3);
            var near = new CellCoord(2, 3);
            var characters = input.Characters.Select(c => c.EntityId == new EntityId(2)
                ? CombatCommandTestData.With(c, position: center)
                : c.EntityId == AllyId ? CombatCommandTestData.With(c, position: near) : c).ToArray();
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, characters: characters), AttackAim.ForCell(center));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray(), Has.Member(input.Aim));
            Assert.That(range.AttackAims.ToArray(), Has.No.Member(AttackAim.ForCell(near)));
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == AllyId).ExpectedDamage, Is.EqualTo(damage));
            Assert.That(Final(result, AllyId).Health.CurrentHealth, Is.EqualTo(100 - damage));
            Assert.That(DamageEvents(result, AllyId).Last().DamageOrigins.Single().PhysicalCell, Is.EqualTo(near));
            Assert.That(Final(result, input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(input.ActorId));
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void ThornField_ActorInsideApplicationRingIsExcluded(CategoryKey category)
        {
            var input = ThornInput(category, TargetingSplash.None);
            var center = new CellCoord(2, 3);
            var characters = input.Characters.Select(c => c.EntityId == new EntityId(2)
                ? CombatCommandTestData.With(c, position: center) : c).ToArray();
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, characters: characters), AttackAim.ForCell(center));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(DamageEvents(result, input.ActorId), Is.Empty);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(input.ActorId));
            Assert.That(result.FinalState.ThornFields.Select(f => f.Cell), Has.No.Member(input.Characters[0].Anchor.Value));
        }

        [Test]
        public void ThornField_DirectlyKilledOccupantDoesNotReceiveImmediateDamage()
        {
            var input = ThornInput(CategoryKey.Base, TargetingSplash.Radial);
            var id = new EntityId(4);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == id
                ? CombatCommandTestData.With(c, hp: 10) : c).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, id).Health.IsDead, Is.True);
            Assert.That(DamageEvents(result, id).Length, Is.EqualTo(1));
            Assert.That(DamageEvents(result, id).Single().DamageOrigins.Single().AttackGroup, Is.EqualTo(1));
        }

        [Test]
        public void ThornField_AlliedStructureStillPreventsFieldCreation()
        {
            var input = ThornInput(CategoryKey.Base, TargetingSplash.Radial);
            var cell = new CellCoord(4, 4);
            var structure = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100),
                cell, Rotation4.R0, new[] { cell }, Owner.Player);
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, new[] { structure }, s.ThornFields, s.Timeline, s.Turn));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.FinalState.ThornFields.Select(f => f.Cell), Has.No.Member(cell));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(structure.EntityId));
        }

        [Test]
        public void PersistentThornField_MovementStillCausesEnvironmentalDamage()
        {
            var input = MovementTestData.SafeRoute(5, 4, 1, CellCoord.Zero, CellCoord.Right);
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures,
                new[] { new ThornFieldCellSnapshot(CellCoord.Right, 5, 5) }, s.Timeline, s.Turn));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new ThornMovementTileRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input.ActorId).Health.CurrentHealth, Is.EqualTo(95));
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            var origin = DamageEvents(result, input.ActorId).Single().DamageOrigins.Single();
            Assert.That(origin.PhysicalCell, Is.EqualTo(CellCoord.Right));
            Assert.That(origin.CreditedCharacterId, Is.Null);
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void AttackableObstacle_ExplosionCanDamageAllyAsIndependentEnvironment(CategoryKey category)
        {
            var input = ThornInput(category, TargetingSplash.None);
            var barrel = ObstacleReactionTests.Barrel(51, Center, 10, 7);
            input = ObstacleReactionTests.Copy(input, characters: input.Characters.Where(c => c.EntityId != new EntityId(2)).ToArray(),
                structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var range = CombatCommandCalculator.GetAttackRange(input);
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(range.AttackAims.ToArray(), Has.Member(input.Aim));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(Final(result, AllyId).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(AllyId));
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Has.No.Member(AllyId));
            var origin = DamageEvents(result, AllyId).Single().DamageOrigins.Single();
            Assert.That(origin.PhysicalCell, Is.EqualTo(Center));
            Assert.That(origin.CreditedCharacterId, Is.EqualTo(input.ActorId));
            Assert.That(origin.AttackGroup, Is.GreaterThan(1));
        }

        private static void AssertHostileThornDamage(CombatCommandInput input,
            AttackPreview preview,
            CombatCommandTransition result, TargetingSplash splash)
        {
            var id = new EntityId(4);
            int damage = splash == TargetingSplash.None ? 5 : 19;
            Assert.That(Final(result, id).Health.CurrentHealth, Is.EqualTo(100 - damage));
            var target = preview.Targets.ToArray().Single(t => t.TargetId == id);
            Assert.That(target.ExpectedDamage, Is.EqualTo(damage));
            Assert.That(target.SecondaryDamage, Is.EqualTo(5));
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == id).Role, Is.EqualTo(AttackTargetRole.Additional));
            var origin = DamageEvents(result, id).Last().DamageOrigins.Single();
            Assert.That(origin.CreditedCharacterId, Is.EqualTo(input.ActorId));
            Assert.That(origin.PhysicalCell, Is.EqualTo(new CellCoord(3, 4)));
        }

        private static CombatCharacterEvent[] DamageEvents(CombatCommandTransition result, EntityId id) =>
            result.Events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId == id &&
                e.Final.Health.CurrentHealth < e.Expected.Health.CurrentHealth).ToArray();

        private static CharacterCombatFacts Final(CombatCommandTransition result, EntityId id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == id);

        private static CombatCommandInput ThornInput(CategoryKey category, TargetingSplash splash, Owner owner = Owner.Player)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, splash,
                splash == TargetingSplash.None ? 0 : 70);
            var source = CombatCommandTestData.Input(method, empty: true);
            var snapshot = category == CategoryKey.Rider ? CategorySnapshot.CreateRider(false)
                : category == CategoryKey.Heavy ? CategorySnapshot.CreateHeavy(Rotation4.R90, false, 50)
                : CategorySnapshot.CreateStateless(category);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), owner, 20),
                traits: new TraitStatusSnapshot[] { new ThornFieldTraitStatusSnapshot(5, 5) }, category: snapshot);
            var hostile = owner == Owner.Player ? Owner.Enemy : Owner.Player;
            var characters = new[] { actor, source.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, Center, hostile),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, AllyCell, owner),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(3, 4), hostile) };
            var profiles = source.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, category,
                new ThornFieldTraitSpec(5, 5)), method);
            var s = source.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight,
                owner == Owner.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic, actor.EntityId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn);
            return new CombatCommandInput(owner == Owner.Player ? 1 : 0, CombatCommandKind.Attack, actor.EntityId, source.Aim,
                state, characters, profiles, source.Main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0), source.Souls, source.Artifacts,
                processingKind: owner == Owner.Player ? CombatProcessingKind.Command : CombatProcessingKind.Advance);
        }
    }
}
