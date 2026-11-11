using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class HuntersMarkAuthoringTests
    {
        [Test]
        public void DefaultTenPercent_RoundTripsAuthoringAdapterRegistryCodecStatus() => RoundTrip(null, 10);

        [TestCase(0)]
        [TestCase(25)]
        [TestCase(150)]
        [TestCase(int.MaxValue)]
        public void ConfiguredPercentage_ChangesThroughRealAuthoringPath(int percent) => RoundTrip(percent, percent);

        [Test]
        public void NegativePercentage_RejectsThroughDataAndAdapter()
        {
            var data = new HuntersMarkTraitData(-1, 1);
            Assert.Throws<InvalidOperationException>(() => data.Validate());
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data));
        }

        [TestCase(10, 22)]
        [TestCase(25, 25)]
        public void ConfiguredPercentage_ControlsActualAlliedDamage(int percent, int expectedDamage)
        {
            var authoring = new HuntersMarkTraitAuthoring();
            typeof(HuntersMarkTraitAuthoring).GetField("bonusDamagePercent", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(authoring, percent);
            var spec = TraitSpecAdapter.Read(authoring.ToData());
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new HuntersMarkBattleSource(spec));
            var target = battle.Character(EntityKey.Unit_Demon);
            battle.Submit(battle.Attack(battle.Actor, target.Anchor.Value));
            var marked = battle.Character(EntityKey.Unit_Demon);
            Assert.That(marked.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Single().IncreasePercent, Is.EqualTo(percent));
            var ally = battle.Character(EntityKey.Unit_DynamiteHammer);
            battle.Submit(battle.Attack(ally, marked.Anchor.Value));
            var final = battle.Character(EntityKey.Unit_Demon);
            Assert.That(marked.Health.CurrentHealth - final.Health.CurrentHealth, Is.EqualTo(expectedDamage));
            Assert.That(final.StatusEffects.SameValues(marked.StatusEffects), Is.True);
        }

        [TestCase(10)]
        [TestCase(25)]
        public void OwnedProjection_PreservesConfiguredPercentageWithoutChangingSource(int percent)
        {
            using var fixture = new CoreAuthoringFixture();
            var originalTrait = new HuntersMarkTraitAuthoring();
            CoreAuthoringFixture.Set(originalTrait, "bonusDamagePercent", 30);
            CoreAuthoringFixture.Set(originalTrait, "durationAffectedSideTurnEnds", 1);
            var original = fixture.CreateUnit(EntityKey.Unit_ShotGun, CategoryKey.Heavy, originalTrait);
            var copy = UnityEngine.Object.Instantiate(original);
            try
            {
                using (var serialized = new SerializedObject(copy))
                {
                    serialized.FindProperty("traits").GetArrayElementAtIndex(0)
                        .FindPropertyRelative("bonusDamagePercent").intValue = percent;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var spec = UnitSpecAdapter.ReadUnit((UnitData)copy.GetData());
                var owned = new OwnedCharacterRecord(new CharacterInstanceId(91), spec.Key, CharacterOwnershipRole.Sub);
                var profile = new CombatAttackProfile(spec,
                    new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0));
                var stats = OwnedCharacterStatsCalculator.Calculate(new[] { profile },
                    UnitSpecAdapter.ReadCategories(ConfiguredMovementBattle.Config), new[] { owned },
                    null, new Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 }, default).Single();
                Assert.That(stats.HuntersMarkDescription.BonusDamagePercent, Is.EqualTo(percent));
                Assert.That(stats.HuntersMarkDescription.DurationAffectedSideTurnEnds, Is.EqualTo(1));
                Assert.That(((HuntersMarkTraitData)((UnitData)original.GetData()).Traits.Single()).BonusDamagePercent,
                    Is.EqualTo(30));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        private static void RoundTrip(int? authored, int expected)
        {
            // 에디터가 실제로 발견하는 직렬화 특성에서 시작해 운영 어댑터와 전투 준비를 거칩니다.
            var type = TypeCache.GetTypesDerivedFrom<UnitTraitAuthoring>()
                .SingleOrDefault(t => t.Name == "HuntersMarkTraitAuthoring");
            Assert.That(type, Is.Not.Null, "Hunter's Mark authoring is not registered for serialization.");
            var authoring = (UnitTraitAuthoring)Activator.CreateInstance(type);
            if (authored.HasValue)
                type.GetField("bonusDamagePercent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(authoring, authored.Value);
            var data = authoring.ToData();
            data.Validate();
            Assert.That((int)data.TraitKey, Is.EqualTo(190));
            var spec = TraitSpecAdapter.Read(data);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new HuntersMarkBattleSource(spec));
            var status = battle.Actor.Stats.TraitStatuses.ToArray().OfType<HuntersMarkTraitStatusSnapshot>().Single();
            Assert.That(status.BonusDamagePercent, Is.EqualTo(expected));
            Assert.That(status.DurationAffectedSideTurnEnds, Is.EqualTo(1));
            // 실제 미리보기 캡처에서도 새 상태를 등록/코덱이 처리해야 합니다.
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId,
                AttackAim.ForCell(battle.Character(EntityKey.Unit_Demon).Anchor.Value)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
        }
    }
}
