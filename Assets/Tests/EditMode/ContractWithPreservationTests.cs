using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Buffer 계약 지식 이전 계획 M2: 계약 사본(With*)은 지정한 값만 바꾸고 나머지 공개 속성을 모두 보존하며,
    /// 클래스 계약이면 항상 새 객체를 돌려준다.
    /// </summary>
    public sealed class ContractWithPreservationTests
    {
        private static MainCharacterPersistentState Main() =>
            new(18, 30, 15, 4);

        [Test]
        public void MainState_WithEachField_ChangesOnlyThatField()
        {
            var source = Main();

            AssertOnlyChanged(source, source.With(currentHealth: 7), nameof(MainCharacterPersistentState.CurrentHealth));
            AssertOnlyChanged(source, source.With(persistentMaxHealth: 31), nameof(MainCharacterPersistentState.PersistentMaxHealth));
            AssertOnlyChanged(source, source.With(persistentAttack: 16), nameof(MainCharacterPersistentState.PersistentAttack));
            AssertOnlyChanged(source, source.With(persistentDefense: 5), nameof(MainCharacterPersistentState.PersistentDefense));
            AssertOnlyChanged(source, source.WithCurrentHealth(9), nameof(MainCharacterPersistentState.CurrentHealth));
        }

        [Test]
        public void MainState_With_AlwaysReturnsNewObjectAndKeepsValues()
        {
            var source = Main();
            var copy = source.With();

            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.SameValues(source), Is.True);
            Assert.That(source.WithCurrentHealth(source.CurrentHealth), Is.Not.SameAs(source));
        }

        [Test]
        public void MainState_With_RunsConstructorValidation()
        {
            var source = Main();

            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => source.With(persistentMaxHealth: 0)).ParamName,
                Is.EqualTo("persistentMaxHealth"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => source.WithCurrentHealth(-1)).ParamName,
                Is.EqualTo("currentHealth"));
        }

        [Test]
        public void Facts_WithHealth_ChangesOnlyHealthAndReturnsNewObject()
        {
            var source = FactsValue();
            var copy = source.WithHealth(new HealthStateSnapshot(9, 20));

            AssertOnlyChanged(source, copy, nameof(CharacterCombatFacts.Health));
            Assert.That(source.WithHealth(source.Health), Is.Not.SameAs(source));
            Assert.That(source.WithHealth(source.Health).SameValues(source), Is.True);
        }

        [Test]
        public void Facts_WithEachValue_ChangesOnlyThatValue()
        {
            var f = FactsValue();
            AssertOnlyChanged(f, f.With(health: new HealthStateSnapshot(9, 20)), nameof(CharacterCombatFacts.Health));
            AssertOnlyChanged(f, f.With(stats: StatsValue().With(attackPower: 11)), nameof(CharacterCombatFacts.Stats));
            AssertOnlyChanged(f, f.With(actionUse: UnitActionUse.Moved), nameof(CharacterCombatFacts.ActionUse));
            AssertOnlyChanged(f, f.With(positionModifier: new PositionTileModifier(new CellCoord(1, 1), new long[] { 1 }, 1, 0)),
                nameof(CharacterCombatFacts.PositionModifier));
            AssertOnlyChanged(f, f.With(heldEnvironment: new CharacterHeldEnvironmentState(1, true)),
                nameof(CharacterCombatFacts.HeldEnvironment));
            AssertOnlyChanged(f, f.With(debuffs: new CharacterDebuffState(new IntimidationDebuffState(10))),
                nameof(CharacterCombatFacts.Debuffs), nameof(CharacterCombatFacts.StatusEffects));
            AssertOnlyChanged(f, f.With(items: new CharacterItemState(3, Array.Empty<ItemTemporaryEffect>())),
                nameof(CharacterCombatFacts.Items));
            AssertOnlyChanged(f, f.With(experience: CharacterExperienceFacts.ForAlly(5)), nameof(CharacterCombatFacts.Experience));
            AssertOnlyChanged(f, f.With(stageRecord: CharacterStageRecord.Empty.Add(kills: 1)),
                nameof(CharacterCombatFacts.StageRecord));
            var recorded = f.With(stageRecord: CharacterStageRecord.Empty.Add(damageDealt: 3));
            Assert.That(recorded.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>())
                .StageRecord.SameValues(recorded.StageRecord), Is.True);
            AssertOnlyChanged(f, f.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()),
                nameof(CharacterCombatFacts.Area), nameof(CharacterCombatFacts.ReserveSlotIndex),
                nameof(CharacterCombatFacts.Anchor), nameof(CharacterCombatFacts.Rotation),
                nameof(CharacterCombatFacts.OccupiedCells));
            AssertOnlyChanged(f, f.With());
            Assert.That(f.With(), Is.Not.SameAs(f));
            Assert.That(f.WithPlacement(f.Area, f.ReserveSlotIndex, f.Anchor, f.Rotation, f.OccupiedCells), Is.Not.SameAs(f));
        }

        [Test]
        public void Facts_WithPlacement_RunsConstructorValidation()
        {
            var f = FactsValue();

            Assert.That(Assert.Throws<ArgumentException>(() =>
                    f.WithPlacement(CharacterRuntimeArea.Field, null, null, Rotation4.R0, new[] { new CellCoord(1, 1) })).Message,
                Is.EqualTo("필드 캐릭터의 위치 값이 올바르지 않습니다."));
            Assert.Throws<ArgumentOutOfRangeException>(() => f.With(actionUse: (UnitActionUse)999));
        }

        [Test]
        public void Stats_WithEachValue_ChangesOnlyThatValueAndItsDerivedValues()
        {
            var s = StatsValue();
            AssertOnlyChanged(s, s.With(moveMethod: MoveMethod.None), nameof(UnitStatsSnapshot.MoveMethod));
            AssertOnlyChanged(s, s.With(moveRange: 4), nameof(UnitStatsSnapshot.MoveRange));
            AssertOnlyChanged(s, s.With(appointRange: 2), nameof(UnitStatsSnapshot.AppointRange), nameof(UnitStatsSnapshot.AttackRange));
            AssertOnlyChanged(s, s.With(attackPower: 11), nameof(UnitStatsSnapshot.AttackPower),
                nameof(UnitStatsSnapshot.EffectiveAttackPower));
            AssertOnlyChanged(s, s.With(physicalDefense: 2), nameof(UnitStatsSnapshot.PhysicalDefense));
            AssertOnlyChanged(s, s.With(priority: 8), nameof(UnitStatsSnapshot.Priority));
            AssertOnlyChanged(s, s.With(traitStatuses: new TraitStatusSnapshot[] { new SteadfastWillTraitStatusSnapshot() }),
                nameof(UnitStatsSnapshot.TraitStatuses));
            AssertOnlyChanged(s, s.With(category: CategorySnapshot.CreateStateless(CategoryKey.Rider)), nameof(UnitStatsSnapshot.Category),
                nameof(UnitStatsSnapshot.IdentityCategoryKey), nameof(UnitStatsSnapshot.CombatCategoryKey));
            AssertOnlyChanged(s, s.With(artifactAttackBonus: 3), nameof(UnitStatsSnapshot.ArtifactAttackBonus),
                nameof(UnitStatsSnapshot.EffectiveAttackPower));
            AssertOnlyChanged(s, s.With(soulAttackBonus: 2), nameof(UnitStatsSnapshot.SoulAttackBonus));
            AssertOnlyChanged(s, s.With(soulMaxHealthBonus: 1), nameof(UnitStatsSnapshot.SoulMaxHealthBonus));
            AssertOnlyChanged(s, s.With(positionAttackBonus: 1), nameof(UnitStatsSnapshot.PositionAttackBonus),
                nameof(UnitStatsSnapshot.EffectiveAttackPower));
            AssertOnlyChanged(s, s.With());
            Assert.That(Assert.Throws<ArgumentException>(() => s.With(effectiveAttackPower: 99)).ParamName,
                Is.EqualTo("effectiveAttackPower"));
        }

        [Test]
        public void Facts_WithPlacementAndValues_ChangesOnlyRequestedValues()
        {
            var source = FactsValue();
            var health = new HealthStateSnapshot(9, 20);
            var stats = source.Stats.With(attackPower: 11);
            var copy = source.WithPlacement(area: CharacterRuntimeArea.Hand, reserveSlotIndex: 0, anchor: null,
                rotation: null, occupiedCells: Array.Empty<CellCoord>(), health: health, stats: stats,
                actionUse: UnitActionUse.Moved);

            AssertOnlyChanged(source, copy, nameof(CharacterCombatFacts.Area), nameof(CharacterCombatFacts.ReserveSlotIndex),
                nameof(CharacterCombatFacts.Anchor), nameof(CharacterCombatFacts.Rotation), nameof(CharacterCombatFacts.OccupiedCells),
                nameof(CharacterCombatFacts.Health), nameof(CharacterCombatFacts.Stats), nameof(CharacterCombatFacts.ActionUse));
            var chained = source.With(health: health, stats: stats, actionUse: UnitActionUse.Moved)
                .WithPlacement(area: CharacterRuntimeArea.Hand, reserveSlotIndex: 0, anchor: null,
                    rotation: null, occupiedCells: Array.Empty<CellCoord>());
            Assert.That(copy.SameValues(chained), Is.True);
            Assert.That(copy, Is.Not.SameAs(source));
        }

        [Test]
        public void Facts_WithPlacementAndValues_KeepsConstructorFailureOrder()
        {
            var source = FactsValue();
            Assert.That(Assert.Throws<ArgumentNullException>(() => source.WithPlacement(
                area: CharacterRuntimeArea.Field, reserveSlotIndex: null, anchor: null, rotation: Rotation4.R0,
                occupiedCells: null, actionUse: (UnitActionUse)999)).ParamName, Is.EqualTo("occupiedCells"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => source.WithPlacement(
                area: CharacterRuntimeArea.Field, reserveSlotIndex: null, anchor: null, rotation: Rotation4.R0,
                occupiedCells: source.OccupiedCells, actionUse: (UnitActionUse)999)).ParamName, Is.EqualTo("actionUse"));
        }

        [Test]
        public void Facts_WithPlacementAndValues_CopiesOccupiedCells()
        {
            var source = FactsValue();
            var anchor = new CellCoord(2, 2);
            var cells = new[] { anchor };
            var copy = source.WithPlacement(area: source.Area, reserveSlotIndex: null, anchor: anchor,
                rotation: source.Rotation, occupiedCells: cells, health: new HealthStateSnapshot(9, 20));
            cells[0] = new CellCoord(3, 3);

            Assert.That(copy.OccupiedCells, Is.EqualTo(new[] { anchor }));
            Assert.That(copy.OccupiedCells, Is.Not.SameAs(cells));
            Assert.That(copy.OccupiedCells, Is.Not.SameAs(source.OccupiedCells));
            Assert.That(source.OccupiedCells, Is.EqualTo(new[] { new CellCoord(1, 1) }));
        }

        internal static UnitStatsSnapshot StatsValue() =>
            new(MoveMethod.LiftAndPlace,3,1,10,1,7,                Array.Empty<TraitStatusSnapshot>(),CategorySnapshot.CreateStateless(CategoryKey.Main),2,null,1,0,0);

        internal static CharacterCombatFacts FactsValue() =>
            new(new EntityId(1), new CharacterInstanceId(1), EntityKey.Unit_Akimbo, Owner.Player, CharacterOwnershipRole.Sub,
                CharacterRuntimeArea.Field, new HealthStateSnapshot(10, 20), StatsValue(), null, new CellCoord(1, 1),
                Rotation4.R0, new[] { new CellCoord(1, 1) }, UnitActionUse.Available);

        /// <summary>
        /// 공개 인스턴스 속성을 모두 비교한다. 이름을 준 속성은 달라야 하고 나머지는 같아야 한다.
        /// 값 형식은 Equals, 목록은 순서 비교, SameValues가 있는 형식은 그것, 나머지는 참조 비교다.
        /// </summary>
        internal static void AssertOnlyChanged(object before, object after, params string[] changed)
        {
            Assert.That(after.GetType(), Is.EqualTo(before.GetType()));
            foreach (var property in before.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                bool same = SameValue(property.GetValue(before), property.GetValue(after));
                if (changed.Contains(property.Name))
                    Assert.That(same, Is.False, $"{property.Name}은 바뀌어야 한다.");
                else
                    Assert.That(same, Is.True, $"{property.Name}은 보존되어야 한다.");
            }
        }

        private static bool SameValue(object left, object right)
        {
            if (left == null || right == null) return left == null && right == null;
            // 특성 목록은 ReadOnlyMemory라 Equals가 배열 참조를 비교한다. 생성자가 배열을 복사하므로 원소를 비교한다.
            if (left is ReadOnlyMemory<TraitStatusSnapshot> leftTraits && right is ReadOnlyMemory<TraitStatusSnapshot> rightTraits)
                return leftTraits.ToArray().SequenceEqual(rightTraits.ToArray());
            var sameValues = left.GetType().GetMethod("SameValues", new[] { left.GetType() });
            if (sameValues != null && !sameValues.IsStatic) return (bool)sameValues.Invoke(left, new[] { right });
            if (left is string) return left.Equals(right);
            if (left is IEnumerable a && right is IEnumerable b)
                return a.Cast<object>().SequenceEqual(b.Cast<object>());
            return left.GetType().IsValueType ? left.Equals(right) : ReferenceEquals(left, right);
        }
    }
}
