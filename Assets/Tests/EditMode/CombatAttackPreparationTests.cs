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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class CombatAttackPreparationTests
    {
        [TestCase("single")]
        [TestCase("direction")]
        [TestCase("all")]
        [TestCase("splash")]
        [TestCase("piercing-cap")]
        [TestCase("pull")]
        [TestCase("pull-friendly")]
        [TestCase("cannon-friendly")]
        [TestCase("cannon-neutral")]
        [TestCase("cannon-adjacent")]
        [TestCase("wall")]
        [TestCase("terrain-and-modifier")]
        public void PreparedAndUnpreparedQueriesHaveIdenticalResults(string scenario)
        {
            var input = Input(scenario);
            var actor = input.Characters.Single(unit => unit.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            var prepared = CombatAttackQueries.PrepareSelection(input, actor, profile, environment);
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, input.State.Environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var elevation = AttackElevation.From(environment, input.EnvironmentBindings);

            AttackSelectionEvaluation Selection(CombatAttackQueries.SelectionPreparation preparation)
            {
                AttackSelectionEvaluation result = null;
                foreach (var _ in CombatAttackQueries.SelectionSteps(input, actor, profile, input.Aim, environment,
                    walls, elevation, value => result = value, preparation)) { }
                return result;
            }

            ResolvedAttackDelivery Delivery(CombatAttackQueries.SelectionPreparation preparation)
            {
                ResolvedAttackDelivery result = null;
                int damage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.OrdinaryAttack);
                foreach (var _ in CombatAttackQueries.DeliverySteps(input, actor, profile, input.Aim, damage,
                    environment, walls, elevation, value => result = value, preparation)) { }
                return result;
            }

            var original = Selection(null);
            var reused = Selection(prepared);
            Assert.That(SelectionFacts(reused), Is.EqualTo(SelectionFacts(original)));
            var expected = scenario switch
            {
                "cannon-adjacent" => AttackFailureReason.AppointCellRejected,
                "wall" => AttackFailureReason.BlockedByWall,
                _ => AttackFailureReason.None
            };
            Assert.That(reused.FailureReason, Is.EqualTo(expected));
            if (scenario == "wall") Assert.That(reused.BlockedContacts, Is.Not.Empty);

            var delivery = Delivery(prepared);
            Assert.That(DeliveryFacts(delivery), Is.EqualTo(DeliveryFacts(Delivery(null))));
            if (expected == AttackFailureReason.None)
            {
                Assert.That(delivery, Is.Not.Null);
                Assert.That(delivery.AffectedCells, Is.Not.Empty);
            }
            if (scenario == "piercing-cap") Assert.That(delivery.Hits.Count, Is.EqualTo(1));
            if (scenario.StartsWith("pull", StringComparison.Ordinal)) Assert.That(reused.Pull.RequiresPull, Is.True);
            if (scenario == "cannon-friendly") Assert.That(delivery.Hits.Select(hit => hit.TargetId.Value), Does.Contain(2));
            if (scenario == "cannon-neutral") Assert.That(delivery.Hits.Select(hit => hit.TargetId.Value), Does.Contain(30));

            foreach (var target in input.Characters.Select(unit => unit.EntityId)
                .Concat(input.State.Structures.Select(structure => structure.EntityId)))
            {
                bool Check(CombatAttackQueries.SelectionPreparation preparation)
                {
                    bool hit = false;
                    int damage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.OrdinaryAttack);
                    foreach (var _ in CombatAttackQueries.DeliveryCheckSteps(input, actor, profile, input.Aim,
                        target, damage, environment, walls, elevation, value => hit = value, preparation)) { }
                    return hit;
                }
                Assert.That(Check(prepared), Is.EqualTo(Check(null)), target.ToString());
            }
        }

        [TestCase("input")]
        [TestCase("actor")]
        [TestCase("profile")]
        [TestCase("environment")]
        public void PreparationRejectsDifferentSourceReferences(string source)
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters[0];
            var profile = input.Profiles[0];
            var preparation = CombatAttackQueries.PrepareSelection(input, actor, profile, null);
            var changedInput = source == "input" ? input.WithActor(input.ActorId) : input;
            var changedActor = source == "actor" ? CombatCommandTestData.With(actor, appointRange: 3) : actor;
            var changedProfile = source == "profile" ? new CombatAttackProfile(profile.Definition, profile.Method) : profile;
            var environment = source == "environment" ? new MovementEnvironmentFacts(input.State.Environment.Tiles) : null;
            bool completed = false;
            using var cursor = CombatAttackQueries.SelectionSteps(changedInput, changedActor, changedProfile, input.Aim,
                environment, null, null, _ => completed = true, preparation).GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => cursor.MoveNext());
            Assert.That(completed, Is.False);
        }

        [Test]
        public void UnpreparedIteratorRemainsLazy()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters[0];
            var differentTraits = CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, new KnockbackTraitSpec(1));
            var profile = new CombatAttackProfile(differentTraits, input.Profiles[0].Method);
            bool completed = false;
            var steps = CombatAttackQueries.SelectionSteps(input, actor, profile, input.Aim, null, null, null, _ => completed = true);
            using var cursor = steps.GetEnumerator();
            Assert.That(completed, Is.False);
            Assert.Throws<InvalidOperationException>(() => cursor.MoveNext());
            Assert.That(completed, Is.False);
        }

        [Test]
        public void NullAndExplicitSourceEnvironmentSharePreparation()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters[0];
            var profile = input.Profiles[0];
            var prepared = CombatAttackQueries.PrepareSelection(input, actor, profile, null);
            AttackSelectionEvaluation result = null;
            foreach (var _ in CombatAttackQueries.SelectionSteps(input, actor, profile, input.Aim,
                input.State.Environment, null, null, value => result = value, prepared)) { }
            Assert.That(SelectionFacts(result), Is.EqualTo(SelectionFacts(CombatAttackQueries.Selection(input, actor, profile))));
        }

        private static CombatCommandInput Input(string scenario)
        {
            var single = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            return scenario switch
            {
                "single" => CombatCommandTestData.Input(single),
                "direction" => CombatTraitTestFixtures.FanAttackInput(),
                "all" => CombatCommandTestData.Input(
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0), AttackAim.ForAll()),
                "splash" => CombatCommandTestData.Input(),
                "piercing-cap" => PiercingShotTestData.Input(cap: 1),
                "pull" => PullTestData.Input(),
                "pull-friendly" => PullTestData.Input(owner: Owner.Player),
                "cannon-friendly" => CombatCommandTestData.Input(single, AttackAim.ForCell(new CellCoord(3, 3)), friendly: true, category: CategoryKey.Cannon),
                "cannon-neutral" => NeutralTarget(single),
                "cannon-adjacent" => CombatCommandTestData.Input(single, AttackAim.ForCell(new CellCoord(2, 3)), category: CategoryKey.Cannon),
                "wall" => WallAttackAdmissionTests.Input(new CellCoord(1, 3), new CellCoord(4, 3), walls: new[] { new CellCoord(2, 3) }),
                "terrain-and-modifier" => TerrainAndModifier(single),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
        }

        private static CombatCommandInput NeutralTarget(TargetingMethodSpec method)
        {
            var cell = new CellCoord(4, 3);
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(cell), empty: true, category: CategoryKey.Cannon);
            var target = new StructureCombatFacts(new EntityId(30), new StructureSpec(EntityKey.Structure_PowderBarrel,
                1, 1, new[] { CellCoord.Zero }, 1, false, 0), new ObstacleDurabilitySnapshot(3, 3), cell,
                Rotation4.R0, new[] { cell }, Owner.Neutral);
            return CombatCommandTestData.With(input, state: input.State.With(structures: new[] { target }));
        }

        private static CombatCommandInput TerrainAndModifier(TargetingMethodSpec method)
        {
            var input = CombatCommandTestData.Input(method);
            var origin = input.Characters[0].Anchor.Value;
            var terrain = new StaticTerrainFact(71, origin, StaticTerrainKind.Sandstorm, true, new[] { Owner.Player, Owner.Enemy });
            var modifier = new DestinationTileModifierFact(72, origin, true, new[] { Owner.Player, Owner.Enemy }, 9, 0);
            input = CliffFallTests.WithEnvironment(input, terrain, modifier);
            var actor = PositionTileModifierCalculator.Replace(input.Characters[0],
                PositionTileModifierCalculator.ResolveAt(origin, input.State.Environment));
            actor = CombatCommandTestData.With(actor, appointRange: 6);
            return CombatCommandTestData.With(input, characters: new[] { actor }.Concat(input.Characters.Skip(1)).ToArray());
        }

        private static string SelectionFacts(AttackSelectionEvaluation result)
        {
            var selection = result.Selection;
            var pull = result.Pull;
            return EnemyActionAssessmentTests.Encode(new object[] { result.FailureReason, result.BlockedContacts, result.EmptyAreaCells,
                selection == null ? null : new object[] { Cells(selection.Cells), Cells(selection.ReachableCells),
                    selection.Targets.Select(target => new object[] { target.Key, target.Value }).ToArray(),
                    selection.EligibleCharacters.Select(unit => unit.EntityId).ToArray(),
                    selection.EligibleStructures.Select(structure => structure.EntityId).ToArray(), selection.SelectedTargetId,
                    selection.Policy.AllowsEmptySingle, selection.Policy.AllowsFriendlyTargets,
                    selection.Policy.AllowsNeutralTargets, selection.Policy.ExcludesAdjacentDirectCells },
                pull == null ? null : new object[] { pull.FailureReason, pull.TargetId, pull.StartAnchor,
                    pull.DestinationAnchor, pull.Path, pull.DeliverDirectDamage } });
        }

        private static object[] Cells(AttackCellSelection cells) =>
            new object[] { cells.PrimaryCell, cells.DesignatedCells.ToArray(), cells.AdditionalCells.ToArray() };

        private static string DeliveryFacts(ResolvedAttackDelivery result) => EnemyActionAssessmentTests.Encode(result == null
            ? null : new object[] { result.AffectedCells, result.BlockedContacts, result.PiercingShot,
                result.Hits.Select(hit => new object[] { hit.TargetId, hit.Role, hit.BaseDamage, hit.AppliedPercent,
                    hit.ImpactOrigin, hit.Cause }).ToArray() });
    }
}
