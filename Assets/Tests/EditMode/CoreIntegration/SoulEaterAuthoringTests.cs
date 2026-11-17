using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class SoulEaterAuthoringTests
    {
        private const string InGame = "Assets/ProjectL/DataBaseSO/InGameGroup/";
        private const string Growth = InGame + "TraitGrowth/Item/TraitGrowth_DeadHand.asset";
        private const EntityKey SoulEater = EntityKey.Unit_SoulEater;

        [Test]
        public void RegisteredUnitUsesTheForwardRectangleAndHeavyDefault()
        {
            var query = DataBaseAssetTests.ProductionInGame();
            Assert.That((int)SoulEater, Is.EqualTo(27));
            var unit = query.UnitTableQuery.GetUnit(SoulEater);
            Assert.That((unit.OwnerEligibility, unit.Role, unit.Category), Is.EqualTo(
                (UnitOwnerEligibility.PlayerOnly, UnitRole.Regular, CategoryKey.Heavy)));
            Assert.That(unit.Behavior.CanMoveVoluntarily, Is.True);
            Assert.That(unit.Behavior.CanBasicAttack, Is.True);
            Assert.That(unit.KillReward.IsEligible, Is.False);
            Assert.That(unit.TargetingMethodKey, Is.EqualTo(11));
            Assert.That(unit.Traits.Select(t => t.TraitKey), Is.EqualTo(new[] { TraitKey.DeadHand }));

            var raw = UnitSpecAdapter.ReadUnit(unit);
            Assert.That((raw.FootprintWidth, raw.FootprintHeight), Is.EqualTo((unit.FootprintWidth, unit.FootprintHeight)));
            Assert.That((raw.MaxHealth, raw.AttackPower, raw.PhysicalDefense, raw.Priority, raw.AttackRange),
                Is.EqualTo((unit.MaxHealth, unit.AttackPower, unit.PhysicalDefense, unit.Priority, unit.AttackRange)));
            var profile = UnitSpecAdapter.ReadProfile(query, raw);
            Assert.That((profile.Method.Shape, profile.Method.Width, profile.Method.ChoiceMode, profile.Method.ChoiceAngle,
                profile.Method.Splash, profile.Method.SplashPercent), Is.EqualTo((TargetingShape.ForwardRectangle,
                query.TargetingMethodTableQuery.GetTargetingMethod(unit.TargetingMethodKey).Width,
                TargetingChoiceMode.Direction, TargetingChoiceAngle.Unrestricted, TargetingSplash.None, 0)));
            var resolved = OwnedUnitSpecResolver.ResolveBase(raw, DataBaseAssetTests.ActiveGrowthCatalog());
            Assert.That(resolved.Traits.ToArray().Select(t => t.TraitKey),
                Is.EquivalentTo(new[] { TraitKey.DeadHand, TraitKey.SteadfastWill }));

            var asset = AssetDatabase.LoadAssetAtPath<UnitDataSO>(InGame + "Unit/Item/Playerable/Unit_SoulEater.asset");
            var first = (UnitData)asset.GetData();
            var second = (UnitData)asset.GetData();
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Traits.Single(), Is.Not.SameAs(second.Traits.Single()));
        }

        [Test]
        public void GrowthAssetHasIndependentRanksAndFreshData()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TraitGrowthDefinitionDataSO>(Growth);
            Assert.That(asset, Is.Not.Null);
            var first = (TraitGrowthDefinitionData)asset.GetData();
            var registered = DataBaseAssetTests.ActiveGrowthCatalog().GetTrait(TraitKey.DeadHand);
            Assert.That(registered.Group, Is.EqualTo(TraitGrowthGroup.Common));
            Assert.That(registered.Enhanced, Is.Null);
            Assert.That(registered.AllowUnchangedRankValues, Is.True);
            int normal = ((DeadHandTraitData)first.Normal).DamagePercent;
            Assert.That(((DeadHandTraitSpec)registered.Normal).DamagePercent, Is.EqualTo(normal));
            Assert.That((registered.Rank2.Amount, registered.Rank3.Amount, registered.MaxRank),
                Is.EqualTo((first.Rank2.Amount, first.Rank3.Amount, 3)));

            var copy = Object.Instantiate(asset);
            try
            {
                var serialized = new SerializedObject(copy);
                int rank2 = first.Rank2.Amount == 120 ? 121 : 120;
                int rank3 = first.Rank3.Amount == 140 ? 141 : 140;
                serialized.FindProperty("rank2.damagePercent").intValue = rank2;
                serialized.FindProperty("rank3.damagePercent").intValue = rank3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var tuned = (TraitGrowthDefinitionData)copy.GetData();
                Assert.That(((DeadHandTraitData)tuned.Normal).DamagePercent, Is.EqualTo(normal));
                Assert.That((tuned.Rank2.Amount, tuned.Rank3.Amount), Is.EqualTo((rank2, rank3)));
                var second = (TraitGrowthDefinitionData)asset.GetData();
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.Normal, Is.Not.SameAs(first.Normal));
                Assert.That((second.Rank2.Amount, second.Rank3.Amount), Is.EqualTo((first.Rank2.Amount, first.Rank3.Amount)));
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void RegistrationPreservesGravekeeperAndGrantsAcquisitionOnlyThroughRoster()
        {
            var query = DataBaseAssetTests.ProductionInGame();
            var gravekeeper = query.UnitTableQuery.GetUnit(EntityKey.Unit_Gravekeeper);
            Assert.That(gravekeeper.OwnerEligibility, Is.EqualTo(UnitOwnerEligibility.EnemyOnly));
            Assert.That(gravekeeper.Traits.Select(t => t.TraitKey), Is.EqualTo(new[] { TraitKey.Knockback }));
            // develop 918817c87(2026-11-17)이 획득 로스터에 넣어 소울이터는 로스터로만 얻는다. 시작 로드아웃·성장 풀에는 없다.
            Assert.That(query.UnitRosterTableQuery.GetAllUnitRosters().SelectMany(r => r.UnitKeys), Has.Member(SoulEater));
            Assert.That(query.PlayerLoadoutTableQuery.GetAllPlayerLoadouts().SelectMany(l =>
                l.Entries.Select(e => e.UnitKey).Append(l.MainCharacterKey)), Has.No.Member(SoulEater));
            Assert.That(DataBaseAssetTests.ActiveGrowthCatalog().Categories.SelectMany(c => c.Pool), Has.No.Member(TraitKey.DeadHand));
        }

    }
}
