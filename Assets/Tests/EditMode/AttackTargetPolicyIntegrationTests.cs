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
        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void Attack_ActorIsExcluded(CategoryKey category)
        {
            var input = AttackInput(category, TargetingSplash.None);
            var center = new CellCoord(2, 3);
            var characters = input.Characters.Select(c => c.EntityId == new EntityId(2)
                ? CombatCommandTestData.With(c, position: center) : c).ToArray();
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, characters: characters), AttackAim.ForCell(center));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(DamageEvents(result, input.ActorId), Is.Empty);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(input.ActorId));
        }

        [Test]
        public void Splash_KilledTargetReceivesOneDamageEvent()
        {
            var input = AttackInput(CategoryKey.Main, TargetingSplash.Radial);
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
        public void Attack_AlliedStructureIsExcluded()
        {
            var input = AttackInput(CategoryKey.Main, TargetingSplash.Radial);
            var cell = new CellCoord(4, 4);
            var structure = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100),
                cell, Rotation4.R0, new[] { cell }, Owner.Player);
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth,s.BoardHeight,                s.Phase,new[] { structure },s.Turn));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(structure.EntityId));
        }

        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void AttackableObstacle_ExplosionCanDamageAllyAsIndependentEnvironment(CategoryKey category)
        {
            var input = AttackInput(category, TargetingSplash.None);
            var barrel = ObstacleReactionTests.Barrel(51, Center, 1, 7);
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

        private static CombatCharacterEvent[] DamageEvents(CombatCommandTransition result, EntityId id) =>
            result.Events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId == id &&
                e.Final.Health.CurrentHealth < e.Expected.Health.CurrentHealth).ToArray();

        private static CharacterCombatFacts Final(CombatCommandTransition result, EntityId id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == id);

        private static CombatCommandInput AttackInput(CategoryKey category, TargetingSplash splash, Owner owner = Owner.Player)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, splash,
                splash == TargetingSplash.None ? 0 : 70);
            var source = CombatCommandTestData.Input(method, empty: true);
            var snapshot = CategorySnapshot.CreateStateless(category);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), owner, 20),
                category: snapshot);
            var hostile = owner == Owner.Player ? Owner.Enemy : Owner.Player;
            var characters = new[] { actor, source.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, Center, hostile),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, AllyCell, owner),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(3, 4), hostile) };
            var profiles = source.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, category), method);
            var s = source.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,                owner == Owner.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,                s.Structures,s.Turn);
            return new CombatCommandInput(owner == Owner.Player ? 1 : 0, CombatCommandKind.Attack, actor.EntityId, source.Aim,
                state, characters, profiles, source.Main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0), source.Souls, source.Artifacts,
                processingKind: owner == Owner.Player ? CombatProcessingKind.Command : CombatProcessingKind.Advance,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }
    }
}
