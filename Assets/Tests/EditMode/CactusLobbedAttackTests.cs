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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using static ProjectL.Tests.Core.WallStateLifecycleTests;

namespace ProjectL.Tests.Core
{
    public sealed class CactusLobbedAttackTests
    {
        private static readonly CellCoord Impact = new CellCoord(4, 3);
        private static readonly CellCoord PropagationWall = new CellCoord(5, 3);
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [TestCase(false)]
        [TestCase(true)]
        public void Cactus_DeliveryUsesExplicitPolicy_InRangePreviewAndExecution(bool lobbed)
        {
            var input = Input(lobbed ? AttackOcclusionSpec.Lobbed : AttackOcclusionSpec.Default);
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);

            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(lobbed));
            Assert.That(preview.CanAttack, Is.EqualTo(lobbed));
            Assert.That(result.Accepted, Is.EqualTo(lobbed), result.Reason);
            if (!lobbed)
            {
                Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
                Assert.That(result.FinalState, Is.SameAs(input.State));
                Assert.That(result.Events, Is.Empty);
                return;
            }

            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EquivalentTo(new[] { new EntityId(2), new EntityId(3) }));
            foreach (var target in preview.Targets.ToArray())
                Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == target.TargetId).Final.Health.CurrentHealth,
                    Is.EqualTo(target.TargetHealthAfter));
            Assert.That(result.Attack.Value.AffectedCells.ToArray(), Has.No.Member(PropagationWall));
            Assert.That(result.FinalState.ThornFields.Any(f => f.Cell == PropagationWall), Is.False);
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(3)).SecondaryDamage, Is.Zero);
            Assert.That(result.BlockedContacts, Is.Not.Empty);
            Assert.That(result.BlockedContacts.All(c => c.Phase == AttackOcclusionPhase.Propagation && c.Origin == Impact), Is.True);
            Assert.That(preview.BlockedContacts.Select(c => c.Contact), Is.EqualTo(result.BlockedContacts.Select(c => c.Contact)));
        }

        [Test]
        public void Lobbed_PropagationStillStopsAtWall_ForSplashAndTraitCells()
        {
            var input = Input(AttackOcclusionSpec.Lobbed);
            var walls = WallGeometryBuilder.Build(8, 8, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            var behindWall = new CellCoord(6, 3);
            var clear = new CellCoord(4, 4);
            var cells = new[] { PropagationWall, behindWall, clear };
            // 현재 스플래시와 가시는 한 칸 범위다. 긴 파급 선분도 검사해 벽 점유 배제만으로 통과하지 않게 한다.
            var splash = AttackOcclusionResolver.ResolveCells(new CellCoord(1, 3),
                new AttackCellSelection(Impact, new[] { Impact }, cells), walls, AttackElevation.From(input.State.Environment, input.EnvironmentBindings), input.Profiles[0].Occlusion);
            var thorn = AttackOcclusionResolver.ResolvePropagationCells(Impact, cells, walls, input.Profiles[0].Occlusion);

            Assert.That(splash.Cells.DesignatedCells.ToArray(), Is.EqualTo(new[] { Impact }));
            Assert.That(splash.Cells.AdditionalCells.ToArray(), Is.EqualTo(new[] { clear }));
            Assert.That(thorn.Cells, Is.EqualTo(new[] { clear }));
            Assert.That(splash.BlockedContacts.Select(c => c.Contact), Is.EquivalentTo(new[] { PropagationWall, behindWall }));
            Assert.That(thorn.BlockedContacts.Select(c => c.Contact), Is.EquivalentTo(new[] { PropagationWall, behindWall }));
        }

        [Test]
        public void Lobbed_CannotLandOnWallCell()
        {
            var input = CombatAttackQueries.WithAim(Input(AttackOcclusionSpec.Lobbed), AttackAim.ForCell(PropagationWall));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray(), Has.No.Member(input.Aim));
            Assert.That(CombatCommandCalculator.Preview(input, Rules).FailureReason, Is.EqualTo(AttackFailureReason.AppointCellRejected));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.FinalState, Is.SameAs(input.State));
        }

        private static CombatCommandInput Input(AttackOcclusionSpec policy)
        {
            // SO 도입 전의 테스트용 구성이다. 아래 능력치는 운영 밸런스 데이터가 아니다.
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);
            var source = CombatCommandTestData.Input(method, category: CategoryKey.Cannon);
            var actor = CombatCommandTestData.With(
                CombatCommandTestData.Character(1, EntityKey.Unit_CactusCannon, new CellCoord(1, 3), Owner.Player, 20),
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new ThornFieldTraitStatusSnapshot(7, 5) });
            var characters = source.Characters.Where(c => c.EntityId != new EntityId(4))
                .Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray();
            var definition = new UnitSpec(EntityKey.Unit_CactusCannon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero },
                100, MoveMethod.LiftAndPlace, 4, CategoryKey.Cannon, 20, 0, 100,
                new TraitSpec[] { new ThornFieldTraitSpec(7, 5) }, UnitOwnerEligibility.PlayerOnly, policy);
            var profiles = source.Profiles.Select((p, i) => i == 0 ? new CombatAttackProfile(definition, method) : p).ToArray();
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, source.State,
                characters, profiles, source.Main, characters.Where(c => c.OwnedId.HasValue)
                    .Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                source.SoulCounts, source.Souls, source.Artifacts,
                expectedBattleInstanceId: source.ExpectedBattleInstanceId, expectedControllerTurnSequence: source.ExpectedControllerTurnSequence,
                battleInstanceId: source.BattleInstanceId);
            return WallMovementTests.WithWalls(input, Wall(71, new CellCoord(2, 3)), Wall(72, PropagationWall));
        }
    }
}
