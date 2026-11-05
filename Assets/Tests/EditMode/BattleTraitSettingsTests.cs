using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>전투 특성 설정 표의 선택 규칙(설명 수치 공개 설계 SP3)을 확인한다.</summary>
    public sealed class BattleTraitSettingsTests
    {
        private static readonly CharacterInstanceId OwnedId = new(7);
        private static readonly TraitSettingsSet BaseFire = Set(new StationaryFireTraitSpec(20));
        private static readonly TraitSettingsSet GrownFire = Set(new StationaryFireTraitSpec(35));

        [Test]
        public void For_UsesOwnedSettingsForOwnedCharacterAndUnitBaseOtherwise()
        {
            var table = Table();

            Assert.That(table.For(Character(1, OwnedId, EntityKey.Unit_CactusCannon)), Is.SameAs(GrownFire));
            Assert.That(table.For(Character(2, null, EntityKey.Unit_CactusCannon)), Is.SameAs(BaseFire));
            Assert.That(table.For(Character(3, new CharacterInstanceId(99), EntityKey.Unit_CactusCannon)), Is.SameAs(BaseFire));
            Assert.That(table.ForUnit(EntityKey.Unit_CactusCannon), Is.SameAs(BaseFire));
        }

        [Test]
        public void For_RejectsUnknownUnitAndOwnedKeyMismatch()
        {
            var table = Table();

            Assert.Throws<KeyNotFoundException>(() => table.ForUnit(EntityKey.Unit_Demon));
            Assert.Throws<KeyNotFoundException>(() => table.For(Character(2, null, EntityKey.Unit_Demon)));
            Assert.Throws<InvalidOperationException>(() => table.For(Character(1, OwnedId, EntityKey.Unit_Akimbo)));
            Assert.Throws<ArgumentNullException>(() => table.For(null));
        }

        [Test]
        public void Constructor_RejectsOwnedUnitMissingFromUnitTable()
        {
            var units = new Dictionary<EntityKey, TraitSettingsSet> { [EntityKey.Unit_CactusCannon] = BaseFire };
            var owned = new Dictionary<CharacterInstanceId, (EntityKey, TraitSettingsSet)> {
                [OwnedId] = (EntityKey.Unit_Demon, GrownFire) };

            Assert.Throws<ArgumentException>(() => new BattleTraitSettings(units, owned));
            Assert.Throws<ArgumentNullException>(() => new BattleTraitSettings(null, owned));
            Assert.Throws<ArgumentNullException>(() => new BattleTraitSettings(units, null));
        }

        private static BattleTraitSettings Table() => new(
            new Dictionary<EntityKey, TraitSettingsSet> {
                [EntityKey.Unit_CactusCannon] = BaseFire, [EntityKey.Unit_Akimbo] = TraitSettingsSet.Empty },
            new Dictionary<CharacterInstanceId, (EntityKey, TraitSettingsSet)> {
                [OwnedId] = (EntityKey.Unit_CactusCannon, GrownFire) });

        private static TraitSettingsSet Set(params TraitSpec[] traits) => new(traits.Select(t => (t, TraitOrigin.Unit)));

        private static CharacterCombatFacts Character(int id, CharacterInstanceId? owned, EntityKey key)
        {
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 4, 1, 2, 0, 0,
                Array.Empty<TraitStatusSnapshot>(), CategorySnapshot.CreateStateless(CategoryKey.Main));
            var cell = new CellCoord(id, 0);
            return new CharacterCombatFacts(new EntityId(id), owned, key, owned.HasValue ? Owner.Player : Owner.Enemy,
                owned.HasValue ? CharacterOwnershipRole.Sub : null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(10, 10), stats, null, cell, Rotation4.R0, new[] { cell }, UnitActionUse.Available);
        }
    }
}
