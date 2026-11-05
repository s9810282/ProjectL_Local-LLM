using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingSplashDamageTests
    {
        private static TargetDamagePipeline Pipeline() =>
            new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()));

        [TestCase(20, 50, 10)]
        [TestCase(7, 50, 3)]
        [TestCase(13, 30, 3)]
        [TestCase(1, 50, 0)]
        [TestCase(7, 0, 0)]
        [TestCase(7, 100, 7)]
        [TestCase(0, 50, 0)]
        [TestCase(int.MaxValue, 100, int.MaxValue)]
        [TestCase(int.MaxValue, 99, 2126008810)]
        [TestCase(2, int.MaxValue, 42949672)]
        public void Additional_TruncatesWidenedProduct(int damage, int percent, int expected)
        {
            Assert.That(SplashDamageCalculator.CalculateAdditional(damage, percent), Is.EqualTo(expected));
            Assert.That(Pipeline().Calculate(damage, AttackTargetRole.Additional, percent,
                CategoryKey.Main, null).FinalDamage, Is.EqualTo(expected));
        }

        [TestCase(-1, 50)]
        [TestCase(7, -1)]
        public void Additional_RejectsNegativeOperands(int damage, int percent)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SplashDamageCalculator.CalculateAdditional(damage, percent));
        }

        [TestCase(int.MaxValue, 101)]
        [TestCase(int.MaxValue, int.MaxValue)]
        public void Additional_RejectsUnrepresentableResult(int damage, int percent)
        {
            Assert.Throws<OverflowException>(() => SplashDamageCalculator.CalculateAdditional(damage, percent));
            Assert.Throws<OverflowException>(() => Pipeline().Calculate(damage, AttackTargetRole.Additional, percent,
                CategoryKey.Main, null));
        }

        [TestCase(AttackTargetRole.Primary)]
        [TestCase(AttackTargetRole.Designated)]
        public void NonAdditional_BypassesScalingEvenWhenProductWouldOverflow(AttackTargetRole role)
        {
            Assert.That(Pipeline().Calculate(int.MaxValue, role, int.MaxValue, CategoryKey.Main, null).FinalDamage,
                Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void ThreeAdditionalTargets_ReceiveIndependentDamageWithoutSplittingPool()
        {
            var pipeline = Pipeline();
            var values = Enumerable.Range(0, 3).Select(_ =>
                pipeline.Calculate(20, AttackTargetRole.Additional, 50, CategoryKey.Main, null).FinalDamage).ToArray();
            Assert.That(values, Is.EqualTo(new[] { 10, 10, 10 }));
            Assert.That(pipeline.Calculate(20, AttackTargetRole.Primary, 50, CategoryKey.Main, null).FinalDamage,
                Is.EqualTo(20));
        }

        [Test]
        public void OrdinaryOrder_SplashTruncationPrecedesExistingHalfUpMatchup()
        {
            var damage = Pipeline().Calculate(7, AttackTargetRole.Additional, 50, CategoryKey.Heavy, CategoryKey.Rider);
            Assert.That(damage.DamageBeforeMatchup, Is.EqualTo(3));
            Assert.That(damage.MatchupNumerator, Is.EqualTo(15));
            Assert.That(damage.MatchupDenominator, Is.EqualTo(10));
            Assert.That(damage.DamageAfterMatchup, Is.EqualTo(5));
            Assert.That(damage.DamageAfterPhysicalDefense, Is.EqualTo(5));
            Assert.That(damage.IncomingReduction, Is.Zero);
            Assert.That(damage.FinalDamage, Is.EqualTo(5));
        }

        [Test]
        public void CommonDamage_IsNotPrimaryHpLossOrTargetAdjustedDamage()
        {
            // 실제 불변 캐릭터 사실에서 공격자 가산을 반영한 D=13을 한 번 확정한다.
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace,1,1,10,0,0,Array.Empty<TraitStatusSnapshot>(),                CategorySnapshot.CreateStateless(CategoryKey.Rider),artifactAttackBonus: 3);
            var actor = new CharacterCombatFacts(new EntityId(1), new CharacterInstanceId(1), EntityKey.Unit_Akimbo,
                Owner.Player, CharacterOwnershipRole.Sub, CharacterRuntimeArea.Field, new HealthStateSnapshot(10,10),
                stats, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, UnitActionUse.Available,
                temporaryAttackBonus: 3);
            int commonDamage = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()))
                .EvaluateAttackPower(actor, 0, new[] { actor });
            Assert.That(commonDamage, Is.EqualTo(13));
            var pipeline = Pipeline();
            var primary = pipeline.Calculate(commonDamage, AttackTargetRole.Primary, 30,
                CategoryKey.Rider, CategoryKey.Heavy, physicalDefense: 50);
            var additional = pipeline.Calculate(commonDamage, AttackTargetRole.Additional, 30,
                CategoryKey.Rider, CategoryKey.Cannon, physicalDefense: 25);
            var primaryPreview = Target(2, primary, 2, AttackTargetRole.Primary, 100);
            var additionalPreview = Target(3, additional, 10, AttackTargetRole.Additional, 30);
            Assert.That(primary.DamageBeforeMatchup, Is.EqualTo(13));
            Assert.That(primary.DamageAfterMatchup, Is.EqualTo(9));
            Assert.That(primary.FinalDamage, Is.EqualTo(4));
            Assert.That(primaryPreview.TargetHealthAfter, Is.Zero);
            Assert.That(primaryPreview.TargetHealthBefore - primaryPreview.TargetHealthAfter, Is.EqualTo(2));
            Assert.That(additional.DamageBeforeMatchup, Is.EqualTo(3));
            Assert.That(additional.DamageAfterMatchup, Is.EqualTo(4));
            Assert.That(additional.FinalDamage, Is.EqualTo(3));
            Assert.That(additionalPreview.TargetHealthAfter, Is.EqualTo(7));
        }

        [Test]
        public void ZeroSplashAndZeroAttack_RemainZeroBeforeAttackMinimum()
        {
            Assert.That(Pipeline().Calculate(1, AttackTargetRole.Additional, 50, CategoryKey.Heavy, CategoryKey.Rider).FinalDamage,
                Is.Zero);
            Assert.That(Pipeline().Calculate(0, AttackTargetRole.Primary, 50, CategoryKey.Main, null).FinalDamage,
                Is.Zero);
        }

        [TestCase(AttackTargetRole.None)]
        [TestCase((AttackTargetRole)99)]
        public void OrdinaryPipeline_RejectsMissingOrUndefinedRole(AttackTargetRole role)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Pipeline().Calculate(7, role, 50, CategoryKey.Main, null));
        }

        [Test]
        public void OrdinaryPipeline_RejectsMalformedNumericalInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Pipeline().Calculate(-1, AttackTargetRole.Primary, 50, CategoryKey.Main, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => Pipeline().Calculate(7, AttackTargetRole.Primary, -1, CategoryKey.Main, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => Pipeline().Calculate(7, AttackTargetRole.Additional, 50, CategoryKey.Main, null, physicalDefense: -1));
        }

        [Test]
        public void RoleMetadata_PreservesFinalValuesWithoutRescaling()
        {
            var preview = Target(2, Pipeline().Calculate(13, AttackTargetRole.Additional, 30,
                CategoryKey.Main, null), 2, AttackTargetRole.Additional, 30);
            var hit = new AttackHitResult(preview.TargetId, preview.TargetHealthBefore, preview.TargetHealthAfter,
                preview.Role, preview.AppliedSplashPercent);
            Assert.That(preview.ExpectedDamage, Is.EqualTo(3));
            Assert.That(preview.AppliedSplashPercent, Is.EqualTo(30));
            Assert.That(hit.Role, Is.EqualTo(AttackTargetRole.Additional));
            Assert.That(hit.AppliedSplashPercent, Is.EqualTo(30));
            Assert.That(hit.DamageDealt, Is.EqualTo(2));
        }

        [Test]
        public void Metadata_RejectsInvalidRoleScaleAndKeepsLegacyUnspecified()
        {
            var legacy = new AttackTargetPreview(new EntityId(2), 7, 10, new[] { CellCoord.Up });
            Assert.That(legacy.Role, Is.EqualTo(AttackTargetRole.None));
            Assert.That(legacy.AppliedSplashPercent, Is.EqualTo(100));
            Assert.That(new AttackHitResult(new EntityId(2), 10, 3).Role, Is.EqualTo(AttackTargetRole.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackHitResult(new EntityId(2), 10, 3, (AttackTargetRole)99, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackHitResult(new EntityId(2), 10, 3, AttackTargetRole.Additional, -1));
            Assert.Throws<ArgumentException>(() => new AttackHitResult(new EntityId(2), 10, 3, AttackTargetRole.Primary, 50));
            Assert.Throws<ArgumentException>(() => Target(2, Pipeline().Calculate(7, CategoryKey.Main, null), 10,
                AttackTargetRole.Designated, 50));
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void NewResults_CarrySelectionWithoutArtificialPrimary(TargetingChoiceMode choice)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            var aim = choice == TargetingChoiceMode.All ? AttackAim.ForAll() : AttackAim.ForDirection(AttackDirection.Left);
            var selection = CombatCommandTestData.ResolvedCells(method, 1, CellCoord.Zero, aim);
            var damage = Pipeline().Calculate(7, AttackTargetRole.Designated, 0, CategoryKey.Main, null);
            var target = Target(2, damage, 10, AttackTargetRole.Designated, 100);
            var affected = selection.DesignatedCells.ToArray();
            var preview = Preview(EntityId.Empty, selection, affected, new[] { target });
            var result = Result(EntityId.Empty, selection, affected,
                new[] { new AttackHitResult(target.TargetId, 10, 3, target.Role, target.AppliedSplashPercent) });
            affected[0] = new CellCoord(99,99);
            Assert.That(preview.PrimaryCell, Is.Null);
            Assert.That(result.PrimaryCell, Is.Null);
            Assert.That(preview.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(result.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(preview.DesignatedCells.ToArray(), Is.EqualTo(selection.DesignatedCells.ToArray()));
            Assert.That(result.DesignatedCells.ToArray(), Is.EqualTo(selection.DesignatedCells.ToArray()));
            Assert.That(preview.AdditionalCells.IsEmpty, Is.True);
            Assert.That(result.AdditionalCells.IsEmpty, Is.True);
            Assert.That(preview.AffectedCells.ToArray(), Is.EqualTo(selection.DesignatedCells.ToArray()));
            Assert.That(result.Hits.Span[0].DamageDealt, Is.EqualTo(7));
        }

        [Test]
        public void SingleResults_KeepPrimaryAdditionalAndFinalEffectCellsSeparate()
        {
            var selection = new AttackCellSelection(CellCoord.Up, new[] { CellCoord.Up }, new[] { new CellCoord(0,2) });
            var primaryId = new EntityId(2);
            var effects = new[] { CellCoord.Up, new CellCoord(0,2) };
            var target = Target(2, Pipeline().Calculate(7, AttackTargetRole.Primary, 50, CategoryKey.Main, null),
                10, AttackTargetRole.Primary, 100);
            var preview = Preview(primaryId, selection, effects, new[] { target });
            var result = Result(primaryId, selection, effects,
                new[] { new AttackHitResult(primaryId, 10, 3, AttackTargetRole.Primary, 100) });
            Assert.That(preview.PrimaryCell, Is.EqualTo(CellCoord.Up));
            Assert.That(result.PrimaryCell, Is.EqualTo(CellCoord.Up));
            Assert.That(preview.TargetCell, Is.EqualTo(CellCoord.Up));
            Assert.That(result.TargetCell, Is.EqualTo(CellCoord.Up));
            Assert.That(result.AdditionalCells.ToArray(), Is.EqualTo(new[] { new CellCoord(0,2) }));
            Assert.That(preview.AffectedCells.ToArray(), Is.EqualTo(effects));
            Assert.Throws<ArgumentException>(() => Preview(EntityId.Empty, selection, effects, new[] { target }));
            Assert.Throws<ArgumentException>(() => Result(EntityId.Empty, selection, effects,
                new[] { new AttackHitResult(primaryId, 10, 3, AttackTargetRole.Primary, 100) }));
            var emptyAim = Result(EntityId.Empty, selection, effects, Array.Empty<AttackHitResult>());
            Assert.That(emptyAim.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(emptyAim.PrimaryCell, Is.EqualTo(CellCoord.Up));
            Assert.That(emptyAim.AdditionalCells.ToArray(), Is.EqualTo(new[] { new CellCoord(0,2) }));
            Assert.That(Preview(EntityId.Empty, selection, effects, Array.Empty<AttackTargetPreview>()).CanAttack, Is.True);
            var all = new AttackCellSelection(null, effects, Array.Empty<CellCoord>());
            Assert.Throws<ArgumentException>(() => Preview(primaryId, all, effects, new[] { target }));
            Assert.Throws<ArgumentException>(() => Result(primaryId, all, effects, Array.Empty<AttackHitResult>()));
        }

        [Test]
        public void NewResults_RejectLegacyUnspecifiedRolesAndPrimaryLabelsOnAll()
        {
            var all = new AttackCellSelection(null, new[] { CellCoord.Up }, Array.Empty<CellCoord>());
            var legacy = new AttackTargetPreview(new EntityId(2), 7, 10, new[] { CellCoord.Up });
            Assert.Throws<ArgumentException>(() => Preview(EntityId.Empty, all, new[] { CellCoord.Up }, new[] { legacy }));
            Assert.Throws<ArgumentException>(() => Result(EntityId.Empty, all, new[] { CellCoord.Up },
                new[] { new AttackHitResult(new EntityId(2), 10, 3) }));
            var primary = Target(2, Pipeline().Calculate(7, AttackTargetRole.Primary, 50, CategoryKey.Main, null),
                10, AttackTargetRole.Primary, 100);
            Assert.Throws<ArgumentException>(() => Preview(EntityId.Empty, all, new[] { CellCoord.Up }, new[] { primary }));
            Assert.Throws<ArgumentException>(() => Result(EntityId.Empty, all, new[] { CellCoord.Up },
                new[] { new AttackHitResult(new EntityId(2), 10, 3, AttackTargetRole.Primary, 100) }));
        }

        private static AttackTargetPreview Target(int id, TargetDamageBreakdown damage, int hp,
            AttackTargetRole role, int percent) => new AttackTargetPreview(new EntityId(id), damage.FinalDamage, hp,
                new[] { CellCoord.Up }, damage.DamageBeforeMatchup, damage.MatchupNumerator,
                damage.MatchupDenominator, damage.IncomingReduction, role, percent);

        private static AttackPreview Preview(EntityId primary, AttackCellSelection selection,
            CellCoord[] affected, AttackTargetPreview[] targets) => AttackPreview.Success(new EntityId(1), primary,
                Rotation4.R0, new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked), affected, targets,
                Array.Empty<TraitStatusSnapshot>(), CategorySnapshot.CreateStateless(CategoryKey.Main), selection);

        private static AttackResult Result(EntityId primary, AttackCellSelection selection,
            CellCoord[] affected, AttackHitResult[] hits) => AttackResult.Success(new EntityId(1), primary,
                Rotation4.R0, new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked), affected, hits,
                new StageSnapshot(1, 1, StagePhase.Combat, Array.Empty<EntitySnapshot>(), Array.Empty<ReserveSlotSnapshot>()),
                Array.Empty<TraitActivationSnapshot>(), Array.Empty<CategoryActivationSnapshot>(), selection);
    }
}
