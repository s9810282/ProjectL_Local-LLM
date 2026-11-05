using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>
    /// 전투 상태에 공개한 특성 설정(설명 수치 공개 설계 SP3)이 규칙이 실제로 쓰는 명령 입력 프로필과 같은지 확인한다.
    /// </summary>
    public sealed class BattleTraitSettingsPublicationTests
    {
        [Test]
        public void PublishedSettings_MatchCommandProfilesForEveryCharacter()
        {
            using var battle = Create();
            var settings = battle.Buffers.CombatStateReadSink.Current.TraitSettings;
            Assert.That(settings, Is.Not.Null, "배치·전투 진입 명령으로 여러 번 복사된 뒤에도 설정 표가 남아야 한다.");

            var input = battle.Publication().Assembler.Assemble(
                new CombatActionRequest(1, CombatCommandKind.Attack, battle.Actor.EntityId, default));
            Assert.That(input.Characters.Any(c => c.OwnedId.HasValue), Is.True);
            Assert.That(input.Characters.Any(c => !c.OwnedId.HasValue), Is.True);
            foreach (var character in input.Characters)
            {
                // 명령의 프로필 선택 규칙: 보유 ID·유닛 키가 맞는 프로필, 없으면 유닛 기본 프로필.
                var profile = input.Profiles.SingleOrDefault(p => p.OwnedId == character.OwnedId && p.Definition.Key == character.CharacterKey)
                    ?? input.Profiles.Single(p => !p.OwnedId.HasValue && p.Definition.Key == character.CharacterKey);
                // 설정 값만 비교한다(SameValues는 표시 출처를 보지 않음).
                var expected = new TraitSettingsSet(profile.Definition.Traits.ToArray().Select(t => (t, TraitOrigin.Unit)));
                Assert.That(settings.For(character).SameValues(expected), Is.True, $"{character.EntityId} {character.CharacterKey}");
            }
        }

        [Test]
        public void PublishedSettings_MatchOwnedCardSettingsOutsideBattle()
        {
            using var battle = Create();
            var settings = battle.Buffers.CombatStateReadSink.Current.TraitSettings;

            Assert.That(settings.For(battle.Actor).SameValues(battle.OwnedStats.TraitSettings), Is.True);
            Assert.That(settings.For(battle.Main).SameValues(battle.MainOwnedStats.TraitSettings), Is.True);
        }

        [Test]
        public void PublishedDisplay_MatchesOwnedCardDisplayOutsideBattle_AndCoversEverySetting()
        {
            // 특성 표시 출처 설계 TO4: 전투 안·밖의 같은 보유 개체는 같은 표시 순서·출처다.
            using var battle = Create();
            var settings = battle.Buffers.CombatStateReadSink.Current.TraitSettings;

            AssertSameDisplay(settings.For(battle.Actor), battle.OwnedStats.TraitSettings);
            AssertSameDisplay(settings.For(battle.Main), battle.MainOwnedStats.TraitSettings);
            var input = battle.Publication().Assembler.Assemble(
                new CombatActionRequest(1, CombatCommandKind.Attack, battle.Actor.EntityId, default));
            foreach (var character in input.Characters)
            {
                var set = settings.For(character);
                Assert.That(set.Display.Select(e => e.Key), Is.EquivalentTo(set.Traits.Select(t => t.TraitKey)),
                    $"{character.EntityId} {character.CharacterKey}");
                Assert.That(set.Display.Count(e => e.Origin == TraitOrigin.Category), Is.LessThanOrEqualTo(1));
            }
        }

        private static void AssertSameDisplay(TraitSettingsSet battle, TraitSettingsSet card) =>
            Assert.That(battle.Display.Select(e => (e.Key, e.Origin)), Is.EqualTo(card.Display.Select(e => (e.Key, e.Origin))));

        // 명령 입력 조립(Publication)은 진입에 쓴 전투 정의가 필요해 정의를 포착하는 출처로 만든다.
        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
    }
}
