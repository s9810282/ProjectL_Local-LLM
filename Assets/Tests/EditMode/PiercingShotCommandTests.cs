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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class PiercingShotCommandTests
    {
        private static AttackResult Execute(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result.Attack.Value;
        }

        [Test]
        public void ProductionBallistaProfile_DeliversOneShot()
        {
            var input = PiercingShotTestData.Input();
            var original = input.Characters[0];
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace,5,99,10,0,30,                original.Stats.TraitStatuses.ToArray(),CategorySnapshot.CreateStateless(CategoryKey.Cannon));
            var actor = new CharacterCombatFacts(original.EntityId, original.OwnedId, EntityKey.Unit_Ballista,
                original.Owner, original.OwnershipRole, original.Area, new HealthStateSnapshot(10, 10), stats,
                original.ReserveSlotIndex, original.Anchor, original.Rotation, original.OccupiedCells, original.ActionUse);
            var definition = UnitSpecTestFactory.Create(EntityKey.Unit_Ballista, 1, 1, new[] { CellCoord.Zero },
                10, MoveMethod.LiftAndPlace, 99, 10, 30,
                new TraitSpec[] { new PiercingFalloffTraitSpec(30, int.MaxValue) }, categoryKey: CategoryKey.Cannon);
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(definition, profiles[0].Method);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(), profiles: profiles);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = Execute(input);
            Assert.That(result.AttackGroup, Is.GreaterThan(0));
            var directEvents = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()).Events
                .OfType<CombatCharacterEvent>().Where(e => e.DamageOrigins.Any(o => o.CreditedCharacterId == actor.EntityId)).ToArray();
            Assert.That(directEvents, Is.Not.Empty);
            Assert.That(directEvents.All(e => e.DamageOrigins.All(o => o.AttackGroup == result.AttackGroup)), Is.True);
            Assert.That(result.PrimaryTargetId.Value, Is.EqualTo(2));
            Assert.That(result.PiercingShot.Step, Is.EqualTo(CellCoord.Right));
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(e => e.TargetId.Value), Is.EqualTo(new[] { 9, 2, 7 }));
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(e => e.RemainingPercent), Is.EqualTo(new[] { 100, 70, 40 }));
            Assert.That(preview.Targets.ToArray().OrderBy(t => t.TargetId.Value).Select(t => t.DamageBeforeMatchup), Is.EqualTo(new[] { 7, 4, 10 }));
            Assert.That(preview.PiercingShot.Encounters.ToArray(), Is.EqualTo(result.PiercingShot.Encounters.ToArray()));
            Assert.That(input.Characters.Where(c => c.EntityId != actor.EntityId).All(c => c.Health.CurrentHealth == 100), Is.True);
        }

        [Test]
        public void SelectedB_HitsAThenBThenC_WithoutChangingSelection()
        {
            var input = PiercingShotTestData.Input();
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = Execute(input);
            Assert.That(result.PrimaryTargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(result.DesignatedCells.ToArray(), Is.EqualTo(new[] { new CellCoord(5, 3) }));
            Assert.That(result.AdditionalCells.IsEmpty, Is.True);
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(h => h.TargetId.Value), Is.EqualTo(new[] { 9, 2, 7 }));
            Assert.That(result.Hits.ToArray().OrderBy(h => h.TargetId.Value).Select(h => h.DamageDealt), Is.EqualTo(new[] { 7, 4, 10 }));
            Assert.That(result.Hits.ToArray().Single(h => h.TargetId.Value == 2).Role, Is.EqualTo(AttackTargetRole.Primary));
            Assert.That(result.Hits.ToArray().Where(h => h.TargetId.Value != 2).All(h => h.Role == AttackTargetRole.Effect && h.AppliedSplashPercent == 100), Is.True);
            Assert.That(preview.PrimaryTargetId, Is.EqualTo(result.PrimaryTargetId));
            Assert.That(preview.PiercingShot.Encounters.ToArray(), Is.EqualTo(result.PiercingShot.Encounters.ToArray()));
            Assert.That(preview.AffectedCells.ToArray(), Is.EqualTo(result.PiercingShot.TraveledCells.ToArray()));
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetHealthAfter), Is.EqualTo(result.Hits.ToArray().Select(t => t.TargetHealthAfter)));
            Assert.That(input.Characters.All(c => c.Health.CurrentHealth == 100), Is.True);
        }

        [Test]
        public void ShortRange_LimitsAimOnly_ShotStillRunsToBoardEdge()
        {
            // 사거리 4: 조준 B(5,3)는 사거리 안, C(6,3)는 밖이다. 관통은 사거리와 무관하게 보드 끝(7,3)까지 간다.
            var input = PiercingShotTestData.Input(appointRange: 4);
            var rangeCells = CombatCommandCalculator.GetAttackRange(input).RangeCells.ToArray();
            Assert.That(rangeCells, Has.Member(new CellCoord(5, 3)));
            Assert.That(rangeCells, Has.No.Member(new CellCoord(6, 3)));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = Execute(input);
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(h => h.TargetId.Value), Is.EqualTo(new[] { 9, 2, 7 }));
            Assert.That(result.PiercingShot.TraveledCells.ToArray().Last(), Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(result.PiercingShot.Endpoint, Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(result.PiercingShot.StopReason, Is.EqualTo(PiercingShotStopReason.BoardEdge));
            Assert.That(preview.PiercingShot.Endpoint, Is.EqualTo(result.PiercingShot.Endpoint));
        }

        [Test]
        public void TraitRemoved_OnlySelectedBIsHit()
        {
            var result = Execute(PiercingShotTestData.Input(piercing: false));
            Assert.That(result.Hits.Length, Is.EqualTo(1));
            Assert.That(result.Hits.Span[0].TargetId.Value, Is.EqualTo(2));
            Assert.That(result.Hits.Span[0].DamageDealt, Is.EqualTo(10));
            Assert.That(result.PiercingShot, Is.Null);
        }

        [Test]
        public void TenDamage_ProducesFourPositiveHitsWithThirtyPointFalloff()
        {
            var input = PiercingShotTestData.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Concat(new[] { PiercingShotTestData.Character(6, 7) }).ToArray());
            var result = Execute(input);
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(e =>
                result.Hits.ToArray().Single(h => h.TargetId == e.TargetId).DamageDealt), Is.EqualTo(new[] { 10, 7, 4, 1 }));
            Assert.That(result.PiercingShot.Endpoint, Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(result.PiercingShot.StopReason, Is.EqualTo(PiercingShotStopReason.TargetBudget));
        }

        [TestCase(0, 10, 0, 1)] [TestCase(1, 10, 1, 3)]
        [TestCase(99, 0, 0, 1)] [TestCase(99, 1, 2, 5)]
        public void ExhaustedBudget_PreservesUnhitAimAndEndsAtLastEncounter(int cap, int damage, int hits, int endX)
        {
            var input = PiercingShotTestData.Input(cap, damage);
            var result = Execute(input);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Hits.Length, Is.EqualTo(hits));
            Assert.That(result.PrimaryTargetId.Value, Is.EqualTo(2));
            Assert.That(result.PiercingShot.Endpoint, Is.EqualTo(new CellCoord(endX, 3)));
            Assert.That(result.PiercingShot.StopReason, Is.EqualTo(PiercingShotStopReason.TargetBudget));
            Assert.That(preview.Targets.Length, Is.EqualTo(hits));
            Assert.That(preview.PiercingShot.Endpoint, Is.EqualTo(result.PiercingShot.Endpoint));
            if (hits == 0) Assert.That(result.AffectedCells.IsEmpty, Is.True);
            if (damage == 1) Assert.That(result.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 1, 1 }));
        }

        [Test]
        public void BFirst_ReceivesOnePrimaryHit()
        {
            var input = PiercingShotTestData.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId.Value != 9 && c.EntityId.Value != 7).ToArray());
            var result = Execute(input);
            Assert.That(result.Hits.Length, Is.EqualTo(1));
            Assert.That(result.Hits.Span[0].Role, Is.EqualTo(AttackTargetRole.Primary));
            Assert.That(result.Hits.Span[0].DamageDealt, Is.EqualTo(10));
        }

        [TestCase(false)] [TestCase(true)]
        public void CannonEmptyAim_WithOrWithoutEncounters_IsAccepted(bool noEncounters)
        {
            var input = PiercingShotTestData.Input(emptyAim: true);
            if (noEncounters) input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.Owner == Owner.Player).ToArray());
            var result = Execute(input);
            Assert.That(result.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(result.PrimaryCell, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(result.Hits.Length, Is.EqualTo(noEncounters ? 0 : 2));
            Assert.That(result.Hits.ToArray().All(h => h.Role == AttackTargetRole.Effect), Is.True);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
        }

        [Test]
        public void SelectingCOnSameRay_ChangesIdentityButNotDeliveredDamage()
        {
            var input = PiercingShotTestData.Input();
            var b = Execute(input);
            var c = Execute(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(6, 3))));
            Assert.That(c.PrimaryTargetId.Value, Is.EqualTo(7));
            Assert.That(c.PiercingShot.TraveledCells.ToArray(), Is.EqualTo(b.PiercingShot.TraveledCells.ToArray()));
            Assert.That(c.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(b.Hits.ToArray().Select(h => h.DamageDealt)));
        }

        [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)]
        [TestCase(1,1)] [TestCase(1,-1)] [TestCase(-1,1)] [TestCase(-1,-1)]
        public void AllEightDirections_PublishExactVectorAndSpatialOrder(int x, int y)
        {
            var input = PiercingShotTestData.Input();
            var actor = CombatCommandTestData.With(input.Characters[0], position: new CellCoord(3, 3));
            var aim = new CellCoord(3 + 2*x, 3 + 2*y);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1],
                PiercingShotTestData.Character(9, 3+x, 3+y), PiercingShotTestData.Character(2, aim.X, aim.Y),
                PiercingShotTestData.Character(7, 3+3*x, 3+3*y) });
            var result = Execute(CombatAttackQueries.WithAim(input, AttackAim.ForCell(aim)));
            Assert.That(result.PiercingShot.Step, Is.EqualTo(new CellCoord(x, y)));
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(e => e.TargetId.Value), Is.EqualTo(new[] { 9, 2, 7 }));
        }

        [Test]
        public void WallBeforeAim_RejectsWithoutDamage()
        {
            var input = WallMovementTests.WithWalls(PiercingShotTestData.Input(), WallStateLifecycleTests.Wall(71, new CellCoord(4, 3)));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(nameof(AttackFailureReason.BlockedByWall)));
            Assert.That(input.Characters.All(c => c.Health.CurrentHealth == 100), Is.True);
        }

        [TestCase(TargetingShape.CardinalLine)] [TestCase(TargetingShape.DiagonalLine)] [TestCase(TargetingShape.RadialSquare)]
        public void SingleShapes_SupportAlignedPiercingWithoutUnitWhitelist(TargetingShape shape)
        {
            var input = PiercingShotTestData.Input();
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(profiles[0].Definition, new TargetingMethodSpec(shape, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            if (shape == TargetingShape.DiagonalLine)
                input = CombatCommandTestData.With(input, characters: new[] { input.Characters[0], input.Characters[1], PiercingShotTestData.Character(2, 3, 5) });
            input = CombatCommandTestData.With(input, profiles: profiles);
            Assert.That(Execute(shape == TargetingShape.DiagonalLine ? CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(3,5))) : input).PiercingShot, Is.Not.Null);
        }

        [TestCase(1)] [TestCase(99)]
        public void StructureBeforeB_StopsAtStructureEvenWhenBudgetEndsThere(int cap)
        {
            var input = PiercingShotTestData.Input(cap);
            var cell = new CellCoord(3, 3);
            var structure = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100),
                cell, Rotation4.R0, new[] { cell }, Owner.Enemy);
            var s = input.State;
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId.Value != 9).ToArray(),
                state: new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                    new[] { structure },s.ThornFields,turn: s.Turn));
            var result = Execute(input);
            Assert.That(result.PrimaryTargetId.Value, Is.EqualTo(2));
            Assert.That(result.Hits.ToArray().Select(h => h.TargetId.Value), Is.EqualTo(new[] { 20 }));
            Assert.That(result.Hits.Span[0].Role, Is.EqualTo(AttackTargetRole.Effect));
            Assert.That(result.PiercingShot.Endpoint, Is.EqualTo(cell));
            Assert.That(result.PiercingShot.StopReason, Is.EqualTo(PiercingShotStopReason.Structure));
        }

        [Test]
        public void FullyArmoredA_TakesMinimumOneAndConsumesEncounter()
        {
            var input = PiercingShotTestData.Input();
            var armored = CombatCommandTestData.With(CombatCommandTestData.Character(9, EntityKey.Unit_Ballista,
                new CellCoord(3, 3), Owner.Enemy), category: CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            armored = armored.With(stats: armored.Stats.With(physicalDefense: 100));
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId.Value == 9 ? armored : c).ToArray(),
                profiles: input.Profiles.Concat(new[] { new CombatAttackProfile(
                    CombatCommandTestData.Spec(armored.CharacterKey, 1, CategoryKey.Heavy), input.Profiles[1].Method) }).ToArray());
            var result = Execute(input);
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(e => e.TargetId.Value), Is.EqualTo(new[] { 9, 2, 7 }));
            Assert.That(result.Hits.ToArray().Single(h => h.TargetId.Value == 9).DamageDealt, Is.EqualTo(1));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Single(h => h.TargetId.Value == 9).ExpectedDamage, Is.EqualTo(1));
            Assert.That(result.Hits.ToArray().Single(h => h.TargetId.Value == 2).DamageDealt, Is.EqualTo(7));
        }

        [Test]
        public void ArtifactBonus_UsesSharedDamageCalculationAndActivatesOnce()
        {
            var input = PiercingShotTestData.Input(damage: 0);
            var actor = input.Characters[0];
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                artifacts: new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(12, 4, ArtifactLastStandRuleTests.LastStandEffect()) }));
            Assert.That(CombatAttackCalculator.CommonAttackDamage(input, actor, input.Profiles[0]), Is.EqualTo(5));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 4, 2, 5 }));
            Assert.That(preview.Targets.ToArray().Select(h => h.ExpectedDamage), Is.EqualTo(new[] { 4, 2, 5 }));
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Single().Activations.Single().AppliedAmount, Is.EqualTo(5));
            var final = result.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final;
            Assert.That(final.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(actor.ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out _), Is.False);
        }

        [Test]
        public void WallAfterAim_StopsDeliveryAndPublishesSeparateBlockedEvidence()
        {
            var input = PiercingShotTestData.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 7
                ? CombatCommandTestData.With(c, position: new CellCoord(7, 3)) : c).ToArray());
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71, new CellCoord(6, 3)));
            var result = Execute(input);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.PiercingShot.StopReason, Is.EqualTo(PiercingShotStopReason.Wall));
            Assert.That(result.PiercingShot.Endpoint, Is.EqualTo(new CellCoord(6, 3)));
            Assert.That(result.AffectedCells.ToArray(), Is.EqualTo(Enumerable.Range(2, 4).Select(x => new CellCoord(x, 3))));
            Assert.That(result.PiercingShot.Encounters.ToArray().Select(e => e.TargetId.Value), Is.EqualTo(new[] { 9, 2 }));
            Assert.That(result.BlockedContacts.ToArray().Single().Contact, Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(preview.BlockedContacts.Count, Is.EqualTo(result.BlockedContacts.Count));
        }

        [TestCase(TargetingChoiceMode.Direction, TargetingSplash.None)]
        [TestCase(TargetingChoiceMode.All, TargetingSplash.None)]
        [TestCase(TargetingChoiceMode.Single, TargetingSplash.Radial)]
        public void IncompatibleComposition_IsRejectedExplicitly(TargetingChoiceMode choice, TargetingSplash splash)
        {
            var input = PiercingShotTestData.Input();
            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(input.Profiles[0].Definition,
                new TargetingMethodSpec(TargetingShape.EightDirectionLine, choice, splash, splash == TargetingSplash.None ? 0 : 100)));
        }

        /// <summary>특성은 일반 규칙이 이미 거부하는 조준의 실패 사유를 바꾸지 않는다.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void PiercingTrait_DoesNotChangeOrdinaryFailureReason(bool piercing)
        {
            var input = PiercingShotTestData.Input(piercing: piercing);
            AssertRejected(input, AttackAim.ForDirection(AttackDirection.Right), AttackFailureReason.InvalidAttackAim);
            AssertRejected(input, AttackAim.ForCell(new CellCoord(3, 4)), AttackFailureReason.InvalidPrimaryTarget);
            AssertRejected(input, AttackAim.ForCell(new CellCoord(1, 3)), AttackFailureReason.InvalidPrimaryTarget);
        }

        private static void AssertRejected(CombatCommandInput input, AttackAim aim, AttackFailureReason expected)
        {
            var result = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(result.Reason, Is.EqualTo(expected.ToString()), aim.Kind + " " + aim.Cell);
        }

        // 원점(제로 벡터)은 일반 기하가 먼저 거부하고, 정렬되지 않은 유효 조준만 전달 실패가 된다.
        [TestCase(1, 3, AttackFailureReason.InvalidPrimaryTarget)]
        [TestCase(5, 4, AttackFailureReason.UnsupportedAttackDelivery)]
        public void UnsupportedVector_IsRejectedAndAbsentFromRange(int x, int y, AttackFailureReason expected)
        {
            var input = PiercingShotTestData.Input(emptyAim: true);
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(profiles[0].Definition, new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            input = CombatCommandTestData.With(input, profiles: profiles);
            var aim = AttackAim.ForCell(new CellCoord(x,y));
            var result = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(result.Reason, Is.EqualTo(expected.ToString()));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray(), Has.No.Member(aim));
        }
    }
}
