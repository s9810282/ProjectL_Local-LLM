using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    /// <summary>경험치·레벨업 설계 §3.1: 요구량 = 역할별 단계 × 현재 레벨, 레벨 안에서 새로 세고 남은 값은 넘긴다.</summary>
    public sealed class ExperienceRulesTests
    {
        // 기획서 Ver0.1 값: 메인 150, 서브 100, 처치 10(임시), 메인 성장 체 3·공 1·방 1.
        private static readonly LevelProgressionSpec Spec = new(150, 100, 10, 3, 1, 1);

        [TestCase(CharacterOwnershipRole.Main, 1, 150)]
        [TestCase(CharacterOwnershipRole.Main, 3, 450)]
        [TestCase(CharacterOwnershipRole.Sub, 1, 100)]
        [TestCase(CharacterOwnershipRole.Sub, 2, 200)]
        public void Required_IsRoleStepTimesCurrentLevel(CharacterOwnershipRole role, int level, int required)
        {
            Assert.That(ExperienceRules.Required(role, level, Spec), Is.EqualTo(required));
        }

        [Test]
        public void EnabledProgression_DoesNotRequireLegacyKillFactor()
        {
            var configured = new LevelProgressionSpec(150, 100, 3, 1, 1);
            Assert.That(configured.IsEnabled, Is.True);
            Assert.That(configured.KillExperiencePerEnemyLevel, Is.Zero);
            Assert.That(ExperienceRules.Gain(Sub(1, 0), 100, configured).Final.Level, Is.EqualTo(2));
            Assert.That(ExperienceRules.Gain(Sub(1, 0), 100, new LevelProgressionSpec(150, 100, 999, 3, 1, 1)).Final.Level, Is.EqualTo(2));
        }

        [TestCase(1, 0, 40, 1, 40, 0)]
        [TestCase(1, 60, 40, 2, 0, 1)]
        [TestCase(1, 90, 30, 2, 20, 1)]
        // 100(1→2) + 200(2→3)을 넘고 50이 남는다.
        [TestCase(1, 0, 350, 3, 50, 2)]
        [TestCase(3, 0, 299, 3, 299, 0)]
        public void Gain_SubLevelsUpAndCarriesRemainder(int level, int experience, int amount,
            int finalLevel, int finalExperience, int levelsGained)
        {
            var gain = ExperienceRules.Gain(Sub(level, experience), amount, Spec);

            Assert.That((gain.Final.Level, gain.Final.Experience, gain.LevelsGained),
                Is.EqualTo((finalLevel, finalExperience, levelsGained)));
            Assert.That((gain.Final.Id, gain.Final.CharacterKey, gain.Final.Role),
                Is.EqualTo((new CharacterInstanceId(2), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)));
        }

        [Test]
        public void Gain_MainUsesMainStep()
        {
            var main = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_MainCharacter,
                CharacterOwnershipRole.Main);

            var gain = ExperienceRules.Gain(main, 160, Spec);

            Assert.That((gain.Final.Level, gain.Final.Experience, gain.LevelsGained), Is.EqualTo((2, 10, 1)));
        }

        [Test]
        public void Gain_DisabledOrZero_ReturnsSameRecordWithoutEvaluatingRequirement()
        {
            // 요구량 이상의 진행값은 요구량을 계산하면 예외다. 꺼짐·획득 0이면 계산하지 않으므로 그대로 돌아온다.
            var record = Sub(1, 100);

            Assert.That(ExperienceRules.Gain(record, 50, default).Final, Is.SameAs(record));
            Assert.That(ExperienceRules.Gain(record, 0, Spec).Final, Is.SameAs(record));
            Assert.That(ExperienceRules.Gain(record, 0, Spec).LevelsGained, Is.Zero);
        }

        [Test]
        public void Gain_RejectsNegativeAmount_BrokenProgressAndOverflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ExperienceRules.Gain(Sub(1, 0), -1, Spec));
            Assert.Throws<InvalidOperationException>(() => ExperienceRules.Gain(Sub(1, 100), 1, Spec));
            Assert.Throws<OverflowException>(() => ExperienceRules.Gain(Sub(1, 99), int.MaxValue, Spec));
        }

        [Test]
        public void Spec_DefaultIsDisabled_AndEnabledValuesAreRangeChecked()
        {
            Assert.That(default(LevelProgressionSpec).IsEnabled, Is.False);
            Assert.That(Spec.IsEnabled, Is.True);
            Assert.Throws<InvalidOperationException>(() => ExperienceRules.Required(CharacterOwnershipRole.Sub, 1, default));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgressionSpec(0, 100, 10, 3, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgressionSpec(150, 0, 10, 3, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgressionSpec(150, 100, 0, 3, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgressionSpec(150, 100, 10, -1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgressionSpec(150, 100, 10, 3, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgressionSpec(150, 100, 10, 3, 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ExperienceRules.Required(CharacterOwnershipRole.Sub, 0, Spec));
        }

        private static OwnedCharacterRecord Sub(int level, int experience) => new(new CharacterInstanceId(2),
            EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, level, experience);
    }
}
