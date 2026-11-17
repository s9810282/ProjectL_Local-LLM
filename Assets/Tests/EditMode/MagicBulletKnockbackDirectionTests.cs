using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 넉백 특성을 가진 마탄 일반·왜곡은 대상을 처음 맞힌 구간의 들어온 방향으로 민다(마탄 넉백 방향 설계 2026-11-17).
    /// 꺾인 뒤 맞은 대상에서 예전 규칙(저격수 칸 기준)과 결과가 갈린다.
    /// </summary>
    public sealed class MagicBulletKnockbackDirectionTests
    {
        // 저격수 (1,1) → A(3,1) → B(3,4) 방향. 첫 구간은 +x, 꺾인 뒤는 +y로 날아간다.
        private static readonly CellCoord Caster = new(1, 1);
        private static readonly CellCoord[] BentPoints = { new(3, 1), new(3, 4) };
        private static readonly TargetingMethodSpec SingleNoSplash =
            new(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);

        [Test]
        public void BentSegmentPushesAlongIncomingDirection()
        {
            var input = Input(BentPoints, PlayerSkillUpgradeKey.None,
                MagicBulletSkillTests.Target(2, 2, 1), MagicBulletSkillTests.Target(3, 3, 3));
            var attack = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single();

            var straight = attack.Knockbacks.Single(k => k.TargetId.Value == 2);
            Assert.That(straight.Step, Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(straight.Path, Is.EqualTo(new[] { new CellCoord(3, 1) }));

            // 예전 규칙이면 저격수 칸에서 대상 쪽인 (1,1) 대각선이었다.
            var bent = attack.Knockbacks.Single(k => k.TargetId.Value == 3);
            Assert.That(bent.Step, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(bent.Path, Is.EqualTo(new[] { new CellCoord(3, 4) }));
            Assert.That(attack.Knockbacks.Select(k => k.Step), Is.EqualTo(new[] { attack.IncomingDirections[0], attack.IncomingDirections[1] }));
        }

        [Test]
        public void DistortionFinalSegmentPushesAlongTravelNotBackTowardCaster()
        {
            // A(4,1) → B(4,4) → C(2,4): 마지막 구간은 −x로 날아간다.
            var points = new[] { new CellCoord(4, 1), new CellCoord(4, 4), new CellCoord(2, 4) };
            var input = Input(points, PlayerSkillUpgradeKey.Distortion, MagicBulletSkillTests.Target(3, 3, 4));
            var attack = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single();

            // 예전 규칙이면 (1,1)로, 탄이 온 쪽(+x)으로 되밀렸다.
            var move = attack.Knockbacks.Single(k => k.TargetId.Value == 3);
            Assert.That(move.Step, Is.EqualTo(new CellCoord(-1, 0)));
            Assert.That(move.Path, Is.EqualTo(new[] { new CellCoord(2, 4) }));
            Assert.That(move.Step, Is.EqualTo(attack.IncomingDirections[0]));
        }

        [Test]
        public void ObstacleOnBentSegmentIsPushedAlongIncomingDirection()
        {
            var barrel = PowderBlossomFixture.Structure(51, EntityKey.Structure_PowderBarrel, 3, 3, 5, Owner.Neutral);
            var input = Input(BentPoints, PlayerSkillUpgradeKey.None, Array.Empty<CharacterCombatFacts>(),
                new[] { barrel, PowderBlossomFixture.Structure(50, EntityKey.Structure_Headquarters, 7, 7, 1000, Owner.Enemy) },
                new[] { new ObstacleContentProfile(barrel.EntityId, ObstacleContentKind.PowderBarrel, 10) });
            var attack = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single();

            var move = attack.Knockbacks.Single(k => k.TargetId.Value == 51);
            Assert.That(move.TargetKind, Is.EqualTo(AttackKnockbackTargetKind.Structure));
            Assert.That(move.Step, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(move.Path, Is.EqualTo(new[] { new CellCoord(3, 4) }));
        }

        [Test]
        public void SelfDestructCorpseOnBentSegmentMovesAlongIncomingDirection()
        {
            var bomber = CombatCommandTestData.With(
                CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, new CellCoord(3, 3), Owner.Enemy, 20),
                hp: 5, traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(1) });
            var input = Input(BentPoints, PlayerSkillUpgradeKey.None, bomber);
            input = MagicBulletTestData.Copy(input, profiles: input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, new SelfDestructTraitSpec(1)),
                SingleNoSplash)).ToArray());
            var result = PowderBlossomFixture.Calculate(input);

            var traits = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).ToArray();
            var corpse = traits.Single(t => t.TraitActivationKind == TraitActivationKind.CorpseDisplaced);
            Assert.That(corpse.TargetCells, Is.EqualTo(new[] { new CellCoord(3, 3), new CellCoord(3, 4) }));
            Assert.That(traits.Single(t => t.TraitActivationKind == TraitActivationKind.SelfDestructExploded).TargetCell,
                Is.EqualTo(new CellCoord(3, 4)));
        }

        [Test]
        public void ForecastPublishesTheSameBentKnockbackAsExecution()
        {
            var input = Input(BentPoints, PlayerSkillUpgradeKey.None,
                MagicBulletSkillTests.Target(2, 2, 1), MagicBulletSkillTests.Target(3, 3, 3));
            var forecast = PlayerSkillQueries.Forecast(input, Array.Empty<IMovementTileRule>(), input.PlayerSkillSource.Value);
            Assert.That(forecast.CanUse, Is.True, forecast.FailureReason.ToString());
            var predicted = forecast.Prediction.Events.OfType<CombatPlayerSkillAttackEvent>().Single().Attack.Knockbacks;
            var actual = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single().Knockbacks;
            Assert.That(predicted.Select(k => (k.TargetId, k.Step, k.FinalAnchor)),
                Is.EqualTo(actual.Select(k => (k.TargetId, k.Step, k.FinalAnchor))));
        }

        /// <summary>넉백 1칸 특성을 가진 저격수와, 전투가 끝나지 않도록 경로 밖에 남는 적 하나를 둔다.</summary>
        private static CombatCommandInput Input(CellCoord[] points, PlayerSkillUpgradeKey upgrade, params CharacterCombatFacts[] targets) =>
            Input(points, upgrade, targets, null, null);

        private static CombatCommandInput Input(CellCoord[] points, PlayerSkillUpgradeKey upgrade, CharacterCombatFacts[] targets,
            StructureCombatFacts[] structures, ObstacleContentProfile[] obstacles) =>
            MagicBulletTestData.Input(points, upgrade, center: Caster,
                targets: targets.Append(MagicBulletSkillTests.Target(9, 6, 6)).ToArray(),
                structures: structures, obstacles: obstacles, extraTrait: new KnockbackTraitSpec(1));
    }
}
