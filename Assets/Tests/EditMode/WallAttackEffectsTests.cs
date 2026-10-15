using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using static ProjectL.Tests.Core.WallStateLifecycleTests;

namespace ProjectL.Tests.Core
{
    public sealed class WallAttackEffectsTests
    {
        private static readonly CellCoord Center = new CellCoord(4, 3);
        private static readonly CellCoord WallCell = new CellCoord(5, 3);
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void GeneratedThorn_ExcludesWallBeforeFieldMutation_AndKeepsClearEffects(bool ignorePropagation, bool oldField)
        {
            var input = ThornInput(ignorePropagation, oldField);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var blockedField = result.FinalState.ThornFields.SingleOrDefault(f => f.Cell == WallCell);
            if (oldField)
            {
                Assert.That(result.FinalState.ThornFields.Any(f => f.Cell == WallCell), Is.True);
                Assert.That(blockedField.RemainingUnitTurns, Is.EqualTo(3));
                Assert.That(blockedField.ThornDamage, Is.EqualTo(11));
            }
            else Assert.That(result.FinalState.ThornFields.Any(f => f.Cell == WallCell), Is.False);
            var clear = result.FinalState.ThornFields.Single(f => f.Cell == new CellCoord(3, 2));
            Assert.That(clear.RemainingUnitTurns, Is.EqualTo(5));
            Assert.That(clear.ThornDamage, Is.EqualTo(7));
            Assert.That(FinalHealth(result, input, 2), Is.EqualTo(80));
            Assert.That(FinalHealth(result, input, 3), Is.EqualTo(93));
            Assert.That(result.Attack.Value.AdditionalCells.ToArray(), Is.Empty);
            AssertContact(result.BlockedContacts.Single(), AttackOcclusionPhase.Propagation, Center, WallCell);
            Assert.That(result.Attack.Value.BlockedContacts, Is.EqualTo(result.BlockedContacts));
            var preview = CombatCommandCalculator.Preview(input, Rules);
            AssertContact(preview.BlockedContacts.Single(), AttackOcclusionPhase.Propagation, Center, WallCell);
        }

        [Test]
        public void Splash_KeepsRawMaskButExcludesOccupiedNeighborAndPublishesPropagationEvidence()
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId != new EntityId(4)).ToArray());
            input = WallMovementTests.WithWalls(input, Wall(71, WallCell));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.AdditionalCells.ToArray(), Has.Member(WallCell));
            Assert.That(result.Attack.Value.AffectedCells.ToArray(), Has.No.Member(WallCell));
            AssertContact(result.BlockedContacts.Single(), AttackOcclusionPhase.Propagation, Center, WallCell);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).BlockedContacts.All(c => c.Phase == AttackOcclusionPhase.Direct), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Propagation_UsesImpactAndExactLongSegment_IgnoreNeverGrantsWallOccupancy(bool ignore)
        {
            var input = ThornInput(ignore, false);
            var walls = WallGeometryBuilder.Build(8, 8, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            var far = new CellCoord(7, 3);
            var selection = new AttackCellSelection(Center, new[] { Center }, new[] { WallCell, far, new CellCoord(4, 4) });
            var filtered = AttackOcclusionResolver.ResolveCells(new CellCoord(1, 3), selection, walls, input.Profiles[0].Occlusion);
            Assert.That(filtered.Cells.AdditionalCells.ToArray().Contains(far), Is.EqualTo(ignore));
            Assert.That(filtered.Cells.AdditionalCells.ToArray(), Has.No.Member(WallCell));
            Assert.That(filtered.Cells.AdditionalCells.ToArray(), Has.Member(new CellCoord(4, 4)));
            AssertContact(filtered.BlockedContacts.Single(c => c.Contact == WallCell), AttackOcclusionPhase.Propagation, Center, WallCell);
            if (!ignore) AssertContact(filtered.BlockedContacts.Single(c => c.Contact == far), AttackOcclusionPhase.Propagation, Center, far);
        }

        [Test]
        public void RejectedInitialThornAttack_HasEvidenceAndNoEffects()
        {
            var input = WallMovementTests.WithWalls(ThornInput(false, false), Wall(71, new CellCoord(2, 3)));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(AttackFailureReason.BlockedByWall.ToString()));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState, Is.SameAs(input.State));
            AssertContact(result.BlockedContacts.Single(), AttackOcclusionPhase.Direct, new CellCoord(1, 3), Center);
            AssertContact(CombatCommandCalculator.Preview(input, Rules).BlockedContacts.Single(), AttackOcclusionPhase.Direct, new CellCoord(1, 3), Center);
        }

        [Test]
        public void PersistentThorn_RemainsEnvironmentalOnEntryBehindWall()
        {
            var input = MovementTestData.SafeRoute(5, 2, 1, new CellCoord(6, 2), new CellCoord(6, 3));
            input = WallMovementTests.WithWalls(input, Wall(71, WallCell));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(8, 8, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, new[] { new ThornFieldCellSnapshot(new CellCoord(6, 3), 5, 9) }, s.Timeline, s.Turn,
                s.Environment, s.EnvironmentProgress, s.WallBlocking));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new ThornMovementTileRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(91));
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(new CellCoord(6, 3)));
            Assert.That(result.BlockedContacts, Is.Empty);
            Assert.That(result.FinalState.ThornFields, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnvironmentalExplosion_KeepsExistingDamagePolicy_RequiresInitialAdmission(bool blockedInitial)
        {
            var input = ObstacleReactionTests.Barrels(ObstacleReactionTests.Barrel(51, Center, 1, 7));
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId != new EntityId(4)).ToArray());
            var wallInput = WallMovementTests.WithWalls(input, Wall(71, blockedInitial ? new CellCoord(2, 3) : WallCell));
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, wallInput.State, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                battleInstanceId: input.BattleInstanceId, environmentBindings: new EnvironmentSemanticBindings(
                    wallInput.EnvironmentBindings.TerrainSourceIds, input.EnvironmentBindings.ObjectSourceIds,
                    input.EnvironmentBindings.ObstacleEntityIds), obstacleContents: input.ObstacleContents);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.EqualTo(!blockedInitial), result.Reason);
            Assert.That(FinalHealth(result, input, 3), Is.EqualTo(blockedInitial ? 100 : 93));
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.EqualTo(!blockedInitial));
            if (blockedInitial)
            {
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.FinalState, Is.SameAs(input.State));
            }
            else
            {
                Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Single().Reaction, Is.EqualTo(ObstacleReactionKind.Destroyed));
                Assert.That(result.BlockedContacts, Is.Empty);
            }
        }

        [Test]
        public void Composition_PreservesEvidenceFromEveryAcceptedStepInOrder()
        {
            var input = ThornInput(false, false);
            var first = CombatCommandCalculator.Calculate(input, Rules);
            var next = CombatCommandComposition.Project(first);
            next = CombatAttackQueries.WithAim(next, AttackAim.ForCell(Center));
            var second = CombatCommandCalculator.Calculate(next, Rules);
            Assert.That(second.Accepted, Is.True, second.Reason);
            var merged = CombatCommandComposition.Merge(input, new[] { first, second });
            Assert.That(merged.BlockedContacts, Is.EqualTo(first.BlockedContacts.Concat(second.BlockedContacts)));
            Assert.That(merged.BlockedContacts.Count, Is.EqualTo(2));
        }

        [Test]
        public void AffordabilityRejection_DoesNotEraseSpatialEvidence()
        {
            var input = ThornInput(false, false);
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, ap: 0) : c).ToArray(), profiles: input.Profiles.Select((p, i) => i == 0
                    ? new CombatAttackProfile(p.Definition, method) : p).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(AttackFailureReason.ActionCostInsufficient.ToString()));
            Assert.That(result.Events, Is.Empty);
            AssertContact(result.BlockedContacts.Single(), AttackOcclusionPhase.Propagation, Center, WallCell);
            AssertContact(CombatCommandCalculator.Preview(input, Rules).BlockedContacts.Single(), AttackOcclusionPhase.Propagation, Center, WallCell);
        }

        private static CombatCommandInput ThornInput(bool ignore, bool oldField)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method);
            var actor = CombatCommandTestData.With(input.Characters[0], traits: new TraitStatusSnapshot[] { new ThornFieldTraitStatusSnapshot(7, 5) });
            var profiles = input.Profiles.ToArray();
            var spec = CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Base, new ThornFieldTraitSpec(7, 5));
            profiles[0] = new CombatAttackProfile(new UnitSpec(spec.Key, spec.Role, 1, 1, new[] { CellCoord.Zero }, spec.MaxHealth,
                spec.MoveMethod, spec.MoveRange, spec.MoveCost, 4, spec.CategoryKey, spec.AttackPower, spec.MaxActionCost,
                spec.AttackCost, spec.PhysicalDefense, spec.Priority, spec.Delay, true, spec.Traits.ToArray(), spec.OwnerEligibility,
                new AttackOcclusionSpec(AttackWallDelivery.RequireClear, ignore ? AttackWallPropagation.IgnoreWalls : AttackWallPropagation.FromImpact)), method);
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId != new EntityId(4))
                .Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(), profiles: profiles);
            input = WallMovementTests.WithWalls(input, Wall(71, WallCell));
            if (!oldField) return input;
            var s = input.State;
            return CombatCommandTestData.With(input, state: new CombatCommandState(8, 8, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, new[] { new ThornFieldCellSnapshot(WallCell, 3, 11) }, s.Timeline, s.Turn,
                s.Environment, s.EnvironmentProgress, s.WallBlocking));
        }

        private static int FinalHealth(CombatCommandTransition result, CombatCommandInput input, int id) =>
            (result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == new EntityId(id))?.Final ??
             input.Characters.Single(c => c.EntityId == new EntityId(id))).Health.CurrentHealth;

        private static void AssertContact(AttackBlockedContact contact, AttackOcclusionPhase phase, CellCoord origin, CellCoord cell)
        {
            Assert.That(contact.Phase, Is.EqualTo(phase));
            Assert.That(contact.Origin, Is.EqualTo(origin));
            Assert.That(contact.Contact, Is.EqualTo(cell));
            Assert.That(contact.WallSourceIds, Is.EqualTo(new long[] { 71 }));
        }
    }
}
