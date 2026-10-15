using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class AttackTargetAdmissionTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord Center = new CellCoord(4, 3);

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void InvalidSingle_DoesNotSettlePendingMovementOrConsumeDiscount(CategoryKey category)
        {
            foreach (string scenario in new[] { "empty", "friendly", "dead", "empty-splash", "friendly-splash" })
            {
                var targets = new List<CharacterCombatFacts>();
                if (scenario.StartsWith("friendly")) targets.Add(Target(2, Center, Owner.Player));
                if (scenario == "dead") targets.Add(CombatCommandTestData.With(Target(2, Center), hp: 0));
                if (scenario.EndsWith("splash")) targets.Add(Target(3, new CellCoord(4, 4)));
                var input = Input(category, targets: targets.ToArray(), pending: true,
                    method: Single(TargetingSplash.Radial));
                AssertRejected(input, scenario.StartsWith("friendly")
                    ? AttackFailureReason.NonHostile : AttackFailureReason.InvalidPrimaryTarget, scenario);
                Assert.That(input.Characters[0].PendingMoveActionCost, Is.EqualTo(1), scenario);
                Assert.That(((MoveAttackDiscountTraitStatusSnapshot)input.Characters[0].Stats.TraitStatuses.Span[0]).DiscountArmed, Is.True);
            }
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void HostilePrimary_IsAllowedForBothOwners(CategoryKey category)
        {
            foreach (Owner owner in new[] { Owner.Player, Owner.Enemy })
            {
                var input = Input(category, owner: owner,
                    targets: new[] { Target(2, Center, owner == Owner.Player ? Owner.Enemy : Owner.Player) });
                var result = AssertAccepted(input);
                Assert.That(result.Attack.Value.Hits.ToArray().Single().TargetId, Is.EqualTo(new EntityId(2)));
                Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
                Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Stats.CurrentActionCost, Is.EqualTo(3));
            }
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void HostileStructureAndAuthoredObstacle_AreAllowed(CategoryKey category)
        {
            foreach (bool obstacle in new[] { false, true })
            {
                var structure = Structure(20, Center, obstacle ? Owner.Neutral : Owner.Enemy, obstacle);
                var input = Input(category, structures: new[] { structure }, obstacle: obstacle);
                Assert.That(AssertAccepted(input).Attack.Value.Hits.ToArray().Single().TargetId, Is.EqualTo(structure.EntityId));
            }
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void AlliedStructure_IsNotAnEligiblePrimary(CategoryKey category)
            => AssertRejected(Input(category, structures: new[] { Structure(20, Center, Owner.Player) }), AttackFailureReason.NonHostile);

        [TestCase(false)]
        [TestCase(true)]
        public void Cannon_EmptyOrFriendlyPrimary_PaysOnce(bool friendly)
        {
            var input = Input(CategoryKey.Cannon, targets: friendly ? new[] { Target(2, Center, Owner.Player) } : null);
            var result = AssertAccepted(input);
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(friendly ? 1 : 0));
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            if (friendly)
            {
                Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
                Assert.That(CombatCommandCalculator.GetAttackRange(input).InRangeTargetIds.ToArray(), Has.Member(new EntityId(2)));
            }
        }

        [TestCase(-1, -1)] [TestCase(0, -1)] [TestCase(1, -1)]
        [TestCase(-1, 0)] [TestCase(0, 0)] [TestCase(1, 0)]
        [TestCase(-1, 1)] [TestCase(0, 1)] [TestCase(1, 1)]
        public void Cannon_CenterAndEightNeighbors_AreNeverDirectAims(int dx, int dy)
        {
            var cell = new CellCoord(1 + dx, 3 + dy);
            var input = Input(CategoryKey.Cannon, aim: AttackAim.ForCell(cell));
            AssertRejected(input, dx == 0 && dy == 0 ? AttackFailureReason.InvalidPrimaryTarget : AttackFailureReason.AppointCellRejected);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).RangeCells.ToArray(), Has.No.Member(cell));
        }

        [TestCase(3, 3)] [TestCase(3, 5)] [TestCase(1, 5)] [TestCase(1, 1)]
        public void Cannon_ImmediatelyOutsideThreeByThree_IsLegal(int x, int y)
            => AssertAccepted(Input(CategoryKey.Cannon, aim: AttackAim.ForCell(new CellCoord(x, y))));

        [Test]
        public void Cannon_LegalSplashCanReachNearCellsButNotTheActor()
        {
            var input = Input(CategoryKey.Cannon, aim: AttackAim.ForCell(new CellCoord(3, 3)),
                method: Single(TargetingSplash.Radial), targets: new[] { Target(2, new CellCoord(2, 3), Owner.Player) });
            var result = AssertAccepted(input);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(result.Attack.Value.Hits.Span[0].Role, Is.EqualTo(AttackTargetRole.Additional));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(14));
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void NonCannon_GroupFiltersAllies_AndRejectsFriendlyOnly(TargetingChoiceMode choice)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            var aim = choice == TargetingChoiceMode.Direction ? AttackAim.ForDirection(AttackDirection.Right) : AttackAim.ForAll();
            var ally = Target(2, new CellCoord(3, 3), Owner.Player);
            var input = Input(CategoryKey.Base, method: method, aim: aim, targets: new[] { ally });
            AssertRejected(input, choice == TargetingChoiceMode.Direction ? AttackFailureReason.NoEligibleTargets : AttackFailureReason.UnresolvedEmptyAll);
            input = Input(CategoryKey.Base, method: method, aim: aim, targets: new[] { ally, Target(3, Center) });
            Assert.That(AssertAccepted(input).Attack.Value.Hits.ToArray().Select(h => h.TargetId),
                Is.EqualTo(new[] { new EntityId(3) }));
        }

        [Test]
        public void NonCannon_SplashFiltersAlliesAndSelf_WithHostilePrimary()
        {
            var input = Input(CategoryKey.Base, method: Single(TargetingSplash.Radial), aim: AttackAim.ForCell(new CellCoord(2, 3)),
                targets: new[] { Target(2, new CellCoord(2, 3)), Target(3, new CellCoord(2, 4), Owner.Player) });
            Assert.That(AssertAccepted(input).Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Cannon)]
        public void RangeKeepsGeometryWhenUnaffordable_AndZeroDamageIsStillLegal(CategoryKey category)
        {
            var input = Input(category, targets: new[] { Target(2, Center) }, attack: 0);
            Assert.That(AssertAccepted(input).Attack.Value.Hits.Span[0].DamageDealt, Is.Zero);
            var range = CombatCommandCalculator.GetAttackRange(input);
            var chars = input.Characters.ToArray();
            chars[0] = CombatCommandTestData.With(chars[0], ap: 0);
            var poor = CombatCommandTestData.With(input, chars);
            var poorRange = CombatCommandCalculator.GetAttackRange(poor);
            Assert.That(poorRange.CanAffordAttack, Is.False);
            Assert.That(poorRange.RangeCells.ToArray(), Is.EqualTo(range.RangeCells.ToArray()));
            Assert.That(poorRange.AttackAims.ToArray(), Is.EqualTo(range.AttackAims.ToArray()));
            Assert.That(CombatCommandCalculator.Preview(poor, Rules).FailureReason, Is.EqualTo(AttackFailureReason.ActionCostInsufficient));
        }

        [Test]
        public void IndirectTargetOutsideRange_RemainsPreviewOnly()
        {
            var primary = new CellCoord(5, 3);
            var input = Input(CategoryKey.Base, method: Single(TargetingSplash.Radial), aim: AttackAim.ForCell(primary),
                targets: new[] { Target(2, primary), Target(3, new CellCoord(6, 3)) });
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.InRangeTargetIds.ToArray(), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(AssertAccepted(input).Attack.Value.Hits.Length, Is.EqualTo(2));
        }

        [Test]
        public void NonCannonPierce_DoesNotInheritCannonNearExclusion()
        {
            var input = Input(CategoryKey.Base, pierce: true, targets: new[] { Target(2, new CellCoord(2, 3)) });
            Assert.That(Pierce(input, 1).Select(h => h.Id), Is.EqualTo(new[] { new EntityId(2) }));
        }

        [Test]
        public void FilteredCharacter_DoesNotConsumePierceEncounter()
        {
            var input = Input(CategoryKey.Base, pierce: true, targets: new[] {
                Target(2, new CellCoord(3, 3), Owner.Player), Target(3, Center) });
            var hits = Pierce(input, 1);
            Assert.That(hits.Select(h => h.Id), Is.EqualTo(new[] { new EntityId(3) }));
            Assert.That(hits[0].Encounter, Is.Zero);
        }

        [Test]
        public void NeutralStructure_RequiresExplicitObstacleEligibilityForNonCannon()
        {
            var actor = Input(CategoryKey.Base).Characters[0];
            var target = Structure(20, Center, Owner.Neutral, obstacle: true);
            Assert.That(AttackTargetEligibility.CanTargetStructure(actor, target, false, default), Is.False);
            Assert.That(AttackTargetEligibility.CanTargetStructure(actor, target, true, default), Is.True);
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void EnemyActor_AlsoRejectsEmptyAndOwnSideTargets(CategoryKey category)
        {
            AssertRejected(Input(category, owner: Owner.Enemy), AttackFailureReason.InvalidPrimaryTarget);
            AssertRejected(Input(category, owner: Owner.Enemy, targets: new[] { Target(2, Center, Owner.Enemy) }),
                AttackFailureReason.NonHostile);
        }

        [Test]
        public void PierceFiniteBudget_PreservesEncounterOrder()
        {
            var input = Input(CategoryKey.Cannon, pierce: true, targets: new[] {
                Target(4, new CellCoord(5, 3)), Target(2, new CellCoord(3, 3)), Target(3, Center) });
            var hits = Pierce(input, 2);
            Assert.That(hits.Select(h => h.Id.Value), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(hits.Select(h => h.Encounter), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(hits.Select(h => h.Ray), Is.EqualTo(new[] { 0, 0 }));
        }

        [Test]
        public void CannonPierce_MultiCellStructureStopsEachRayAndAggregatesDamage()
        {
            var cells = Enumerable.Range(1, 5).Select(y => new CellCoord(3, y)).ToArray();
            var structure = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 5,
                    Enumerable.Range(0, 5).Select(y => new CellCoord(0, y)).ToArray(), 100, false, 0),
                new HealthStateSnapshot(100, 100), cells[0], Rotation4.R0, cells, Owner.Enemy);
            var input = Input(CategoryKey.Cannon, pierce: true, structures: new[] { structure }, targets: new[] { Target(2, Center) });
            var hits = Pierce(input, 4);
            Assert.That(hits.Select(h => h.Id.Value), Is.EqualTo(new[] { 20, 20, 20 }));
            Assert.That(hits.Select(h => h.Ray), Is.EqualTo(new[] { 0, 1, 2 }));
            var attack = AssertAccepted(input).Attack.Value;
            Assert.That(attack.Hits.Length, Is.EqualTo(1));
            Assert.That(attack.Hits.Span[0].DamageDealt, Is.EqualTo(60));
        }

        [Test]
        public void ResolvedSelection_IsImmutableAndFailuresReturnNoSelection()
        {
            var input = Input(CategoryKey.Base, targets: new[] { Target(2, Center) });
            var evaluation = CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles[0]);
            Assert.That(evaluation.FailureReason, Is.EqualTo(AttackFailureReason.None));
            var original = evaluation.Selection;
            var targets = original.Targets.ToArray();
            var characters = original.EligibleCharacters.ToArray();
            var structures = original.EligibleStructures.ToArray();
            var copy = new ResolvedAttackSelection(original.Cells, original.ReachableCells, targets, characters, structures, original.Policy, original.PierceTrace);
            targets[0] = default;
            characters[0] = null;
            Assert.That(copy.Targets[0].Key, Is.EqualTo(new EntityId(2)));
            Assert.That(copy.EligibleCharacters[0].EntityId, Is.EqualTo(new EntityId(2)));
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterCombatFacts>)copy.EligibleCharacters)[0] = null);
            var failed = CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles[0], AttackAim.ForCell(new CellCoord(5, 3)));
            Assert.That(failed.FailureReason, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget));
            Assert.That(failed.Selection, Is.Null);
        }

        // 선정 때 수집한 공간 조우에 명시적 타격 수만 적용한다.
        private static (EntityId Id, int Encounter, int Ray)[] Pierce(CombatCommandInput input, int budget)
        {
            var evaluation = CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles[0]);
            Assert.That(evaluation.FailureReason, Is.EqualTo(AttackFailureReason.None));
            return PierceTargetCollector.ApplyBudget(evaluation.Selection.PierceTrace, budget);
        }

        [Test]
        public void FriendlyBlocker_StopsPierceWithoutTakingDamage()
        {
            var input = Input(CategoryKey.Base, pierce: true,
                structures: new[] { Structure(20, new CellCoord(3, 3), Owner.Player) }, targets: new[] { Target(2, Center) });
            AssertRejected(input, AttackFailureReason.NoEligibleTargets);
        }

        [Test]
        public void CannonPierce_RetainsNearExclusionAndFalloff()
        {
            var input = Input(CategoryKey.Cannon, pierce: true, targets: new[] {
                Target(2, new CellCoord(2, 3)), Target(3, new CellCoord(3, 3), Owner.Player), Target(4, Center) });
            var hits = AssertAccepted(input).Attack.Value.Hits.ToArray();
            Assert.That(hits.Select(h => h.TargetId.Value), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(hits.Select(h => h.DamageDealt), Is.EqualTo(new[] { 20, 14 }));
        }

        private static void AssertRejected(CombatCommandInput input, AttackFailureReason reason, string scenario = null)
        {
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.Succeeded, Is.True, scenario);
            Assert.That(range.AttackAims.ToArray(), Has.No.Member(input.Aim), scenario);
            var preview = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(preview.CanAttack, Is.False, scenario);
            Assert.That(preview.FailureReason, Is.EqualTo(reason), scenario);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False, scenario);
            Assert.That(result.Reason, Is.EqualTo(reason.ToString()), scenario);
            Assert.That(result.Characters.Characters, Is.Empty, scenario);
            Assert.That(result.Characters.Souls, Is.Empty, scenario);
            Assert.That(result.Events, Is.Empty, scenario);
            Assert.That(result.FinalState, Is.SameAs(input.State), scenario);
            Assert.That(result.Field.Final, Is.SameAs(input.Souls), scenario);
            Assert.That(result.Attack, Is.Null, scenario);
        }

        private static CombatCommandTransition AssertAccepted(CombatCommandInput input)
        {
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray(), Has.Member(input.Aim));
            var preview = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(preview.CanAttack, Is.True, preview.FailureReason.ToString());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.Targets.ToArray().Select(t => (t.TargetId, t.TargetHealthAfter)),
                Is.EqualTo(result.Attack.Value.Hits.ToArray().Select(h => (h.TargetId, h.TargetHealthAfter))));
            return result;
        }

        private static TargetingMethodSpec Single(TargetingSplash splash = TargetingSplash.None) =>
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, splash, splash == TargetingSplash.None ? 0 : 70);

        private static CharacterCombatFacts Target(int id, CellCoord cell, Owner owner = Owner.Enemy) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, owner);

        private static StructureCombatFacts Structure(int id, CellCoord cell, Owner owner, bool obstacle = false) =>
            new StructureCombatFacts(new EntityId(id),
                new StructureSpec(obstacle ? EntityKey.Structure_PowderBarrel : EntityKey.Structure_Headquarters,
                    1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), cell, Rotation4.R0, new[] { cell }, owner);

        private static CombatCommandInput Input(CategoryKey category, TargetingMethodSpec method = null, AttackAim? aim = null,
            CharacterCombatFacts[] targets = null, StructureCombatFacts[] structures = null, bool pending = false,
            Owner owner = Owner.Player, int attack = 20, bool pierce = false, bool obstacle = false)
        {
            method ??= pierce
                ? new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0)
                : Single();
            aim ??= pierce ? AttackAim.ForDirection(AttackDirection.Right) : AttackAim.ForCell(Center);
            var source = CombatCommandTestData.Input(method, aim, empty: true);
            var snapshot = category == CategoryKey.Rider ? CategorySnapshot.CreateRider(false)
                : category == CategoryKey.Heavy ? CategorySnapshot.CreateHeavy(Rotation4.R90, false, 50)
                : CategorySnapshot.CreateStateless(category);
            TraitSpec[] traits = pierce ? new TraitSpec[] { new PiercingFalloffTraitSpec(30, 99) }
                : pending ? new TraitSpec[] { new MoveAttackDiscountTraitSpec(1, 1) } : Array.Empty<TraitSpec>();
            TraitStatusSnapshot[] statuses = pierce ? new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) }
                : pending ? new TraitStatusSnapshot[] { new MoveAttackDiscountTraitStatusSnapshot(true, true) } : Array.Empty<TraitStatusSnapshot>();
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo,
                new CellCoord(1, 3), owner, attack, pending: pending), category: snapshot, traits: statuses);
            var characters = new[] { actor, source.Characters[1] }.Concat(targets ?? Array.Empty<CharacterCombatFacts>()).ToArray();
            var profiles = source.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, attack, category, traits), method);
            var state = new CombatCommandState(8, 8, owner == Owner.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,
                actor.EntityId, false, structures ?? Array.Empty<StructureCombatFacts>(), source.State.ThornFields, source.State.Timeline);
            var obstacles = obstacle ? structures.Select(s => new ObstacleContentProfile(s.EntityId, ObstacleContentKind.PowderBarrel, 0)).ToArray()
                : Array.Empty<ObstacleContentProfile>();
            return new CombatCommandInput(owner == Owner.Player ? 1 : 0, CombatCommandKind.Attack, actor.EntityId, aim.Value, state, characters, profiles,
                source.Main, characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0), source.Souls, source.Artifacts,
                processingKind: owner == Owner.Player ? CombatProcessingKind.Command : CombatProcessingKind.Advance,
                battleInstanceId: obstacle ? 1 : 0,
                environmentBindings: new EnvironmentSemanticBindings(Array.Empty<long>(), Array.Empty<long>(), obstacles.Select(o => o.EntityId).ToArray()),
                obstacleContents: obstacles);
        }
    }
}
