using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class WallAttackAdmissionTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord Actor = new CellCoord(1, 3);
        private static readonly CellCoord Center = new CellCoord(4, 3);
        private static readonly AttackOcclusionSpec Ignore = new AttackOcclusionSpec(AttackWallDelivery.IgnoreWalls, AttackWallPropagation.IgnoreWalls);

        [TestCase(128, false, true)]
        [TestCase(0, false, false)]
        [TestCase(128, true, true)]
        [TestCase(0, true, false)]
        public void ArbitrarySlope_W07W08_UsesExactCells(int cut, bool reverse, bool clear)
        {
            var a = new CellCoord(3, 5);
            var b = new CellCoord(8, 9);
            var input = Input(reverse ? b : a, reverse ? a : b, cut: cut,
                walls: new[] { new CellCoord(4, 5), new CellCoord(5, 5), new CellCoord(6, 5) });
            Assert.That(input.Aim.Cell, Is.EqualTo(reverse ? a : b));
            AssertAgreement(input, clear ? AttackFailureReason.None : AttackFailureReason.BlockedByWall);
            var evaluation = Evaluate(input);
            Assert.That(evaluation.BlockedContacts.Count, Is.EqualTo(clear ? 0 : 1));
            if (!clear)
            {
                Assert.That(evaluation.Selection, Is.Null);
                Assert.That(evaluation.BlockedContacts[0].Origin, Is.EqualTo(reverse ? b : a));
                Assert.That(evaluation.BlockedContacts[0].Contact, Is.EqualTo(reverse ? a : b));
                Assert.That(evaluation.BlockedContacts[0].WallSourceIds, Is.EqualTo(new long[] { 71, 72, 73 }));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NonCardinalDeltaFiveThree_CannotUseRepresentativeFacing(bool reverse)
        {
            var a = new CellCoord(1, 1);
            var b = new CellCoord(6, 4);
            AssertAgreement(Input(reverse ? b : a, reverse ? a : b, walls: new[] { new CellCoord(3, 2) }), AttackFailureReason.BlockedByWall);
        }

        [Test, Combinatorial]
        public void RestrictivePrimary_EligibilityPrecedesWall(
            [Values(CategoryKey.Base, CategoryKey.Rider, CategoryKey.Heavy)] CategoryKey category,
            [Values(false, true)] bool friendly)
        {
            var input = Input(Actor, Center, category: category, walls: new[] { new CellCoord(2, 3) },
                targets: friendly ? new[] { Target(2, Center, Owner.Player), Target(3, new CellCoord(4, 4)) } : new[] { Target(3, new CellCoord(4, 4)) },
                method: Single(TargetingSplash.Radial));
            AssertAgreement(input, friendly ? AttackFailureReason.NonHostile : AttackFailureReason.InvalidPrimaryTarget);
            Assert.That(Evaluate(input).BlockedContacts, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Cannon_EmptyCenterRequiresDelivery_AndPaysOnceIfClear(bool blocked)
        {
            var input = Input(Actor, Center, category: CategoryKey.Cannon, targets: Array.Empty<CharacterCombatFacts>(),
                walls: blocked ? new[] { new CellCoord(2, 3) } : Array.Empty<CellCoord>());
            AssertAgreement(input, blocked ? AttackFailureReason.BlockedByWall : AttackFailureReason.None);
            if (!blocked) Assert.That(CombatCommandCalculator.Calculate(input, Rules).Attack.Value.ActionCostSpent, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WallOccupiedCenter_RejectsEvenCannonAndIgnore(bool ignore)
        {
            var input = Input(Actor, Center, category: CategoryKey.Cannon, policy: ignore ? Ignore : null,
                targets: Array.Empty<CharacterCombatFacts>(), walls: new[] { Center });
            AssertAgreement(input, AttackFailureReason.AppointCellRejected);
        }

        [Test, Combinatorial]
        public void Groups_DistinguishPartialBlockedAllBlockedAndEmpty(
            [Values(TargetingChoiceMode.Direction, TargetingChoiceMode.All)] TargetingChoiceMode choice,
            [Values(0, 1, 2)] int scenario)
        {
            var targets = scenario == 0 ? Array.Empty<CharacterCombatFacts>() : scenario == 1
                ? new[] { Target(2, Center) } : new[] { Target(2, Center), Target(3, new CellCoord(2, 5)) };
            var input = Input(Actor, Center, targets: targets, walls: new[] { new CellCoord(2, 3) },
                method: new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0));
            var expected = scenario == 0 ? (choice == TargetingChoiceMode.Direction ? AttackFailureReason.NoEligibleTargets : AttackFailureReason.UnresolvedEmptyAll)
                : scenario == 1 ? AttackFailureReason.BlockedByWall : AttackFailureReason.None;
            AssertAgreement(input, expected);
            if (scenario == 2)
            {
                var evaluation = Evaluate(input);
                Assert.That(evaluation.Selection.Targets.Select(t => t.Key.Value), Is.EqualTo(new long[] { 3 }));
                Assert.That(evaluation.Selection.Cells.DesignatedCells.ToArray(), Has.Member(Center));
                Assert.That(evaluation.Selection.ReachableCells.DesignatedCells.ToArray(), Has.No.Member(Center));
                Assert.That(evaluation.BlockedContacts.Select(b => b.Contact), Has.Member(Center));
            }
        }

        [Test]
        public void MultiCellStructure_UsesIncludedContacts_DeduplicatesAndKeepsPrimary()
        {
            var structure = Structure(new CellCoord(4, 3), new CellCoord(4, 4), new CellCoord(4, 5), new CellCoord(4, 6));
            var input = Input(Actor, new CellCoord(4, 6), targets: Array.Empty<CharacterCombatFacts>(), structures: new[] { structure },
                walls: new[] { new CellCoord(2, 3) }, method: new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0));
            // (4,5)는 기울기 2/3으로 벽 내부를 지난다. (4,6)은 1/1로 모서리에 접해 통과한다.
            var walls = WallGeometryBuilder.Build(10, 10, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            Assert.That(AttackOcclusionResolver.CheckDirect(Actor, new CellCoord(4, 5), walls, AttackOcclusionSpec.Default), Is.Not.Null);
            Assert.That(AttackOcclusionResolver.CheckDirect(Actor, new CellCoord(4, 6), walls, AttackOcclusionSpec.Default), Is.Null);
            AssertAgreement(input, AttackFailureReason.None);
            Assert.That(CombatCommandCalculator.Calculate(input, Rules).Attack.Value.Hits.Length, Is.EqualTo(1));
            AssertAgreement(WithMethod(input, Single(), AttackAim.ForCell(Center)), AttackFailureReason.BlockedByWall);
            var single = WithMethod(input, Single(TargetingSplash.Radial), AttackAim.ForCell(new CellCoord(4, 6)));
            AssertAgreement(single, AttackFailureReason.None);
            Assert.That(Evaluate(single).Selection.Targets.Single().Value, Is.EqualTo(AttackTargetRole.Primary));
        }

        [Test, Combinatorial]
        public void DeliveryAndPropagation_AreIndependent(
            [Values(false, true)] bool ignoreDelivery, [Values(false, true)] bool ignorePropagation)
        {
            var impact = new CellCoord(3, 3);
            var neighbor = new CellCoord(4, 4);
            var input = Input(Actor, impact, targets: new[] { Target(2, impact), Target(3, neighbor) },
                walls: new[] { new CellCoord(4, 3) }, cut: 0, method: Single(TargetingSplash.Radial),
                policy: new AttackOcclusionSpec(ignoreDelivery ? AttackWallDelivery.IgnoreWalls : AttackWallDelivery.RequireClear,
                    ignorePropagation ? AttackWallPropagation.IgnoreWalls : AttackWallPropagation.FromImpact));
            // 한 칸 스플래시의 대각 접촉은 모서리에 접해 통과한다. 더 긴 전파 선분으로 정책 두 단계를 독립 검증한다.
            AssertAgreement(input, AttackFailureReason.None);
            var walls = WallGeometryBuilder.Build(
                10, 10, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            var policy = input.Profiles[0].Occlusion;
            Assert.That(AttackOcclusionResolver.CheckDirect(
                impact, new CellCoord(5, 3), walls, policy) == null, Is.EqualTo(ignoreDelivery));
            Assert.That(AttackOcclusionResolver.CheckPropagation(
                impact, new CellCoord(5, 3), walls, policy) == null, Is.EqualTo(ignorePropagation));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IgnoredDelivery_DoesNotChangeFactions(bool friendly)
        {
            var input = Input(Actor, Center, policy: Ignore, walls: new[] { new CellCoord(2, 3) },
                targets: new[] { Target(2, Center, friendly ? Owner.Player : Owner.Enemy) });
            AssertAgreement(input, friendly ? AttackFailureReason.NonHostile : AttackFailureReason.None);
        }

        [Test]
        public void IgnoreAttackWalls_DoesNotGrantMovement()
        {
            var input = Input(Actor, Center, policy: Ignore, walls: new[] { new CellCoord(2, 3) });
            AssertAgreement(input, AttackFailureReason.None);
            var path = new MovementPath(new[] { Actor, new CellCoord(2, 3) });
            Assert.That(MovementRouteValidator.Validate(input, path, out _), Is.EqualTo(MovementFailureReason.DestinationOccupied));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Splash_WallOccupiedCellIsNeverReachableEvenWhenIgnored(bool ignore)
        {
            var input = Input(Actor, Center, method: Single(TargetingSplash.Radial), policy: ignore ? Ignore : null,
                walls: new[] { new CellCoord(5, 3) });
            var selection = Evaluate(input).Selection;
            Assert.That(selection.Cells.AdditionalCells.ToArray(), Has.Member(new CellCoord(5, 3)));
            Assert.That(selection.ReachableCells.AdditionalCells.ToArray(), Has.No.Member(new CellCoord(5, 3)));
            AssertAgreement(input, AttackFailureReason.None);
        }

        [Test]
        public void PublishedCells_PreserveRawSelectionSeparatelyFromAffectedCells()
        {
            var wall = new CellCoord(5, 3);
            var input = Input(Actor, Center, method: Single(TargetingSplash.Radial), walls: new[] { wall });
            var result = CombatCommandCalculator.Calculate(input, Rules).Attack.Value;
            var preview = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(result.AdditionalCells.ToArray(), Has.Member(wall));
            Assert.That(preview.AdditionalCells.ToArray(), Has.Member(wall));
            Assert.That(result.AffectedCells.ToArray(), Has.No.Member(wall));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pierce_ReachableCellsStopAtWallAndPhysicalStructure(bool ignore)
        {
            var input = Input(Actor, Center, pierce: true, policy: ignore ? Ignore : null,
                targets: new[] { Target(2, new CellCoord(3, 5)) }, structures: new[] { Structure(new CellCoord(4, 3)) },
                walls: new[] { new CellCoord(2, 3) });
            var selection = Evaluate(input).Selection;
            Assert.That(selection, Is.Not.Null);
            Assert.That(selection.ReachableCells.DesignatedCells.ToArray().Contains(Center), Is.EqualTo(ignore));
            Assert.That(selection.ReachableCells.DesignatedCells.ToArray(), Has.No.Member(new CellCoord(2, 3)));
            Assert.That(selection.ReachableCells.DesignatedCells.ToArray(), Has.No.Member(new CellCoord(5, 3)));
            Assert.That(selection.PierceTrace.ReachableCells, Is.EqualTo(selection.ReachableCells.DesignatedCells.ToArray()));
        }

        [Test]
        public void BlockedPrimary_CannotBeRescuedByClearSplash()
        {
            var input = Input(Actor, new CellCoord(4, 4), method: Single(TargetingSplash.Radial),
                targets: new[] { Target(2, new CellCoord(4, 4)), Target(3, new CellCoord(3, 5)) },
                walls: new[] { new CellCoord(2, 3) }, cut: 0);
            AssertAgreement(input, AttackFailureReason.BlockedByWall);
        }

        [Test]
        public void LowAp_RangeKeepsRawCellsAndTargets_WhileBlockedAimIsUnavailable()
        {
            var input = Input(Actor, Center, walls: new[] { new CellCoord(2, 3) });
            input = CombatCommandTestData.With(input, characters: input.Characters.Select((c, i) => i == 0 ? CombatCommandTestData.With(c, ap: 0) : c).ToArray());
            Assert.That(Evaluate(input).FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
            Assert.That(Evaluate(input).BlockedContacts, Is.Not.Empty);
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.RangeCells.ToArray(), Has.Member(Center));
            Assert.That(range.InRangeTargetIds.ToArray(), Has.Member(new EntityId(2)));
            Assert.That(range.AttackAims.ToArray(), Has.No.Member(input.Aim));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pierce_WallBeforePhysicalAllyDoesNotInventHiddenTarget(bool ignore)
        {
            var allied = Structure(new CellCoord(3, 3));
            allied = new StructureCombatFacts(allied.EntityId, allied.Definition, allied.Health, allied.Anchor, allied.Rotation, allied.OccupiedCells, Owner.Player);
            var input = Input(Actor, Center, pierce: true, policy: ignore ? Ignore : null, structures: new[] { allied }, walls: new[] { new CellCoord(2, 3) });
            AssertAgreement(input, AttackFailureReason.NoEligibleTargets);
            Assert.That(Evaluate(input).BlockedContacts, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pierce_AllBlockedKeepsFailureEvidence_IgnoreRestoresTargets(bool ignore)
        {
            var input = Input(Actor, Center, policy: ignore ? Ignore : null, walls: new[] { new CellCoord(2, 3) }, pierce: true);
            AssertAgreement(input, ignore ? AttackFailureReason.None : AttackFailureReason.BlockedByWall);
            if (!ignore) Assert.That(Evaluate(input).BlockedContacts.Single().Contact, Is.EqualTo(Center));
        }

        [Test]
        public void BlockedAttack_IsRejectedBeforePendingMovementAndEffects()
        {
            var input = Input(Actor, Center, walls: new[] { new CellCoord(2, 3) }, pending: true);
            var actor = input.Characters[0];
            actor = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner, actor.OwnershipRole,
                actor.Area, actor.Health, actor.Stats, actor.ReserveSlotIndex, actor.Anchor, actor.Rotation, actor.OccupiedCells,
                actor.MovementCheckpointAnchor, 2, 1, pendingMoveDistance: 1, attackRefundEligibleMoveCost: 2);
            var s = input.State;
            input = CombatCommandTestData.With(input, characters: input.Characters.Select((c, i) => i == 0 ? actor : c).ToArray(),
                state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight, s.Structures,
                    s.ThornFields, s.Timeline, s.Turn, s.Environment, new EnvironmentProgressFacts(1, 100, 0xA51E), s.WallBlocking));
            var beforeStats = actor.Stats;
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Reason, Is.EqualTo(nameof(AttackFailureReason.BlockedByWall)));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FinalState, Is.SameAs(input.State));
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Characters.Souls, Is.Empty);
            Assert.That(result.ConsumedSoulIds, Is.Empty);
            Assert.That(result.Field.Final.SameValues(input.Souls), Is.True);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(actor.PendingMoveActionCost, Is.EqualTo(1));
            Assert.That(actor.Stats, Is.EqualTo(beforeStats));
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(((MoveAttackDiscountTraitStatusSnapshot)actor.Stats.TraitStatuses.Span[0]).DiscountArmed, Is.True);
            Assert.That(actor.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(1, 2)));
            Assert.That(result.FinalState.EnvironmentProgress, Is.SameAs(input.State.EnvironmentProgress));
            Assert.That(actor.AttackRefundEligibleMoveCost, Is.EqualTo(2));
            Assert.That(actor.PendingMoveDistance, Is.EqualTo(1));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(0xA51E));
        }

        [Test]
        public void ZeroDamageOrZeroBudget_DoesNotRedefineSpatialAdmission()
        {
            var input = Input(Actor, Center, attack: 0);
            AssertAgreement(input, AttackFailureReason.None);
            Assert.That(CombatCommandCalculator.Calculate(input, Rules).Attack.Value.Hits.ToArray().All(h => h.DamageDealt == 0), Is.True);
            AssertAgreement(Input(Actor, Center, pierce: true, cap: 0), AttackFailureReason.None);
        }

        [Test]
        public void OcclusionContract_PropagatesThroughCatalog_AndRejectsUndefinedValues()
        {
            var input = Input(Actor, Center, policy: Ignore);
            var spec = input.Profiles[0].Definition;
            Assert.That(spec.Occlusion, Is.SameAs(Ignore));
            Assert.That(input.Profiles[0].Occlusion, Is.SameAs(Ignore));
            Assert.That(new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault()).GetRequired(spec.Key).Occlusion, Is.SameAs(Ignore));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackOcclusionSpec((AttackWallDelivery)0, AttackWallPropagation.FromImpact));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackOcclusionSpec(AttackWallDelivery.RequireClear, (AttackWallPropagation)3));
            Assert.That(AttackOcclusionSpec.Default.Delivery, Is.EqualTo(AttackWallDelivery.RequireClear));
            Assert.That(AttackOcclusionSpec.Default.Propagation, Is.EqualTo(AttackWallPropagation.FromImpact));
            Assert.That((int)AttackFailureReason.DeferredMultiCellUnit, Is.EqualTo(21));
            Assert.That((int)AttackFailureReason.BlockedByWall, Is.EqualTo(22));
        }

        [Test]
        public void Evaluation_DefensivelyCopiesEvidenceOnFailure()
        {
            var block = new AttackBlockedContact(Actor, Center, AttackOcclusionPhase.Direct, new long[] { 71 }, new[] { new CellCoord(2, 3) });
            var evidence = new[] { block };
            var result = AttackSelectionEvaluation.Failure(AttackFailureReason.BlockedByWall, evidence);
            evidence[0] = null;
            Assert.That(result.Selection, Is.Null);
            Assert.That(result.BlockedContacts.Single(), Is.SameAs(block));
            Assert.Throws<NotSupportedException>(() => ((IList<AttackBlockedContact>)result.BlockedContacts).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(() => AttackSelectionEvaluation.Failure(AttackFailureReason.None));
        }

        private static AttackSelectionEvaluation Evaluate(CombatCommandInput input) =>
            CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles[0]);

        private static void AssertAgreement(CombatCommandInput input, AttackFailureReason reason)
        {
            Assert.That(Evaluate(input).FailureReason, Is.EqualTo(reason), "selection");
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(preview.FailureReason, Is.EqualTo(reason), "preview");
            Assert.That(result.Accepted, Is.EqualTo(reason == AttackFailureReason.None), result.Reason);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(reason == AttackFailureReason.None), "range");
            if (reason != AttackFailureReason.None) Assert.That(result.Reason, Is.EqualTo(reason.ToString()));
            else Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Is.EqualTo(result.Attack.Value.Hits.ToArray().Select(t => t.TargetId)));
        }

        private static TargetingMethodSpec Single(TargetingSplash splash = TargetingSplash.None) =>
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, splash, splash == TargetingSplash.None ? 0 : 70);

        private static CharacterCombatFacts Target(int id, CellCoord cell, Owner owner = Owner.Enemy) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, owner);

        private static StructureCombatFacts Structure(params CellCoord[] cells) => new StructureCombatFacts(new EntityId(20),
            new StructureSpec(EntityKey.Structure_Headquarters, 1, cells.Length, Enumerable.Range(0, cells.Length).Select(y => new CellCoord(0, y)).ToArray(), 100, false, 0),
            new HealthStateSnapshot(100, 100), cells[0], Rotation4.R0, cells);

        private static CombatCommandInput WithMethod(CombatCommandInput input, TargetingMethodSpec method, AttackAim aim) =>
            CombatAttackQueries.WithAim(CombatCommandTestData.With(input, profiles: input.Profiles.Select((p, i) => i == 0
                ? new CombatAttackProfile(p.Definition, method) : p).ToArray()), aim);

        private static CombatCommandInput Input(CellCoord actorCell, CellCoord center, CategoryKey category = CategoryKey.Base,
            CharacterCombatFacts[] targets = null, StructureCombatFacts[] structures = null, CellCoord[] walls = null,
            int cut = 128, AttackOcclusionSpec policy = null, TargetingMethodSpec method = null, bool pending = false,
            bool pierce = false, int cap = 9, int attack = 20)
        {
            method ??= pierce ? new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0) : Single();
            var aim = method.ChoiceMode == TargetingChoiceMode.Single ? AttackAim.ForCell(center)
                : method.ChoiceMode == TargetingChoiceMode.All ? AttackAim.ForAll() : AttackAim.ForDirection(AttackDirection.Right);
            var input = CombatCommandTestData.Input(method, aim, empty: true, pending: pending, category: category);
            var actor = CombatCommandTestData.With(input.Characters[0], position: actorCell, appointRange: 9,
                traits: pierce ? new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) }
                    : pending ? new TraitStatusSnapshot[] { new MoveAttackDiscountTraitStatusSnapshot(true, true) } : Array.Empty<TraitStatusSnapshot>());
            // 영점 피해 검사는 실제 전투 능력치까지 영점으로 맞춘다.
            if (attack == 0) actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, actorCell, Owner.Player, 0), appointRange: 9);
            var spec = input.Profiles[0].Definition;
            var profile = new CombatAttackProfile(new UnitSpec(spec.Key, spec.Role, 1, 1, new[] { CellCoord.Zero }, spec.MaxHealth,
                spec.MoveMethod, spec.MoveRange, spec.MoveCost, 9, category, attack, spec.MaxActionCost, spec.AttackCost,
                spec.PhysicalDefense, spec.Priority, spec.Delay, true, pierce ? new TraitSpec[] { new PiercingFalloffTraitSpec(30, cap) }
                    : pending ? new TraitSpec[] { new MoveAttackDiscountTraitSpec(1, 1) } : spec.Traits.ToArray(), spec.OwnerEligibility, policy), method);
            var characters = new[] { actor, input.Characters[1] }.Concat(targets ?? new[] { Target(2, center) }).ToArray();
            input = CombatCommandTestData.With(input, characters: characters, profiles: new[] { profile, input.Profiles[1], input.Profiles[2] });
            input = WallMovementTests.WithWalls(input, (walls ?? Array.Empty<CellCoord>()).Select((c, i) => WallStateLifecycleTests.Wall(71 + i, c)).ToArray());
            var s = input.State;
            return CombatCommandTestData.With(input, state: new CombatCommandState(10, 10, s.Phase, s.ActiveActorId, s.IsNight,
                structures ?? s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment, s.EnvironmentProgress, new WallBlockingSpec(cut)));
        }
    }
}
