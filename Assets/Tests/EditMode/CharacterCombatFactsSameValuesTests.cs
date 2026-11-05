using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
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
    /// Buffer 계약 지식 이전 계획 B-2: 옛 Buffer 비교기에서 옮긴 전투 사실·능력치 값 비교가 필드 하나의 차이도 잡는지 확인한다.
    /// 최대 행동 비용(생성자가 고정값만 받음)과 실효 공격력(다른 값에서 파생)은 하나만 다른 사례를 만들 수 없어 따로 두지 않는다.
    /// </summary>
    public sealed class CharacterCombatFactsSameValuesTests
    {
        private static readonly CellCoord A = new(1, 1);
        private static readonly CellCoord B = new(1, 2);

        [Test]
        public void StatusDifference_IsDetectedWithoutChangingIdentityOrStats()
        {
            var before = Facts();
            var statuses = new StatusEffectSet(new AppliedStatus[] { new AttackIncreaseStatus(
                new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Vanguard, "grant"), 3, 2) });
            var changed = before.WithStatusEffects(statuses);
            AssertDifferent(before, changed);
            Assert.That(before.Stats.SameValues(changed.Stats), Is.True);
            Assert.That(changed.With().SameValues(changed), Is.True);
        }

        [Test]
        public void SameFacts_AreEqual()
        {
            Assert.That(Facts().SameValues(Facts()), Is.True);
            Assert.That(Facts().SameIdentity(Facts()), Is.True);
        }

        [Test]
        public void IdentityDifferences_AreDetectedByBoth()
        {
            AssertDifferent(Facts(), Facts(entityId: 2), identity: true);
            AssertDifferent(Facts(), Facts(ownedId: 2), identity: true);
            AssertDifferent(Facts(), Facts(key: EntityKey.Unit_Gravekeeper), identity: true);
            AssertDifferent(Facts(), Facts(role: CharacterOwnershipRole.Main), identity: true);
            AssertDifferent(Enemy(1), Enemy(2), identity: true);
            AssertDifferent(Facts(), Enemy(1), identity: true);
        }

        [Test]
        public void FactDifferences_AreDetectedAndKeepIdentity()
        {
            AssertDifferent(Facts(), Facts(health: new HealthStateSnapshot(9, 20)));
            AssertDifferent(Facts(), Facts(health: new HealthStateSnapshot(10, 21)));
            AssertDifferent(Hand(0), Hand(1));
            AssertDifferent(Facts(), Hand(0));
            AssertDifferent(Facts(), Facts(anchor: B));
            AssertDifferent(Facts(), Facts(rotation: Rotation4.R90));
            AssertDifferent(Facts(), Facts(cells: new[] { A, B }));
            AssertDifferent(Facts(cells: new[] { A, B }), Facts(cells: new[] { B, A }));
            AssertDifferent(Facts(), Facts(actionUse: UnitActionUse.Moved));
            AssertDifferent(Facts(), Facts(temporaryAttack: 1));
            AssertDifferent(Facts(), Facts(temporaryMaxHealth: 1));
            AssertDifferent(Facts(), Facts(position: new PositionTileModifier(A, new long[] { 1 }, 1, 0)));
            AssertDifferent(Facts(), Facts(held: new CharacterHeldEnvironmentState(1, true)));
            AssertDifferent(Facts(), Facts(debuffs: new CharacterDebuffState(new IntimidationDebuffState(10))));
            AssertDifferent(Facts(), Facts(items: new CharacterItemState(3, Array.Empty<ItemTemporaryEffect>())));
            AssertDifferent(Facts(), Facts(experience: CharacterExperienceFacts.ForAlly(5)));
            AssertDifferent(Facts(), Facts(stageRecord: CharacterStageRecord.Empty.Add(distanceMoved: 1)));
        }

        [Test]
        public void StatsDifferences_AreDetected()
        {
            AssertDifferent(Facts(), Facts(stats: Stats(moveMethod: MoveMethod.None)));
            AssertDifferent(Facts(), Facts(stats: Stats(moveRange: 4)));
            AssertDifferent(Facts(), Facts(stats: Stats(appointRange: 2)));
            AssertDifferent(Facts(), Facts(stats: Stats(attackPower: 11)));
            AssertDifferent(Facts(), Facts(stats: Stats(soulAttack: 2)));
            AssertDifferent(Facts(), Facts(stats: Stats(soulMaxHealth: 1)));
            AssertDifferent(Facts(), Facts(stats: Stats(artifactAttack: 3)));
            AssertDifferent(Facts(), Facts(stats: Stats(positionAttack: 1)));
            AssertDifferent(Facts(), Facts(stats: Stats(physicalDefense: 2)));
            AssertDifferent(Facts(), Facts(stats: Stats(priority: 8)));
        }

        [Test]
        public void CategoryAndTraitDifferences_AreDetected()
        {
            AssertDifferent(Facts(), Facts(stats: Stats(category: CategorySnapshot.CreateStateless(CategoryKey.Rider))));
            AssertDifferent(Facts(stats: Stats(category: CategorySnapshot.CreateStateless(CategoryKey.Rider))),
                Facts(stats: Stats(category: CategorySnapshot.CreateStateless(CategoryKey.Heavy))));
            AssertDifferent(Facts(), Facts(stats: Stats(traits: new TraitStatusSnapshot[] { new SteadfastWillTraitStatusSnapshot() })));
            Assert.That(Facts(stats: Stats(traits: new TraitStatusSnapshot[] { new SteadfastWillTraitStatusSnapshot() }))
                .SameValues(Facts(stats: Stats(traits: new TraitStatusSnapshot[] { new SteadfastWillTraitStatusSnapshot() }))),
                Is.True);
        }

        [Test]
        public void DefaultStatsWithNullCategory_ComparesLikeTheOldComparer()
        {
            Assert.That(Facts(stats: default(UnitStatsSnapshot)).SameValues(Facts(stats: default(UnitStatsSnapshot))), Is.True);
            Assert.That(Facts(stats: default(UnitStatsSnapshot)).SameValues(Facts()), Is.False);
        }

        [Test]
        public void NullArgument_ThrowsLikeTheOldComparer()
        {
            Assert.Throws<NullReferenceException>(() => Facts().SameValues(null));
            Assert.Throws<NullReferenceException>(() => Facts().SameIdentity(null));
        }

        private static void AssertDifferent(CharacterCombatFacts left, CharacterCombatFacts right, bool identity = false)
        {
            Assert.That(left.SameValues(right), Is.False);
            Assert.That(right.SameValues(left), Is.False);
            Assert.That(left.SameIdentity(right), Is.EqualTo(!identity));
        }

        private static UnitStatsSnapshot Stats(MoveMethod moveMethod = MoveMethod.LiftAndPlace, int moveRange = 3,
            int appointRange = 1, int attackPower = 10, int physicalDefense = 1,
            int priority = 7,
            TraitStatusSnapshot[] traits = null, CategorySnapshot category = null, int artifactAttack = 2,
            int soulAttack = 1, int soulMaxHealth = 0, int positionAttack = 0) =>
            new(moveMethod,moveRange,appointRange,attackPower,physicalDefense,priority,                traits ?? Array.Empty<TraitStatusSnapshot>(),category ?? CategorySnapshot.CreateStateless(CategoryKey.Main),                artifactAttack,null,soulAttack,soulMaxHealth,positionAttack);

        private static CharacterCombatFacts Facts(UnitStatsSnapshot? stats = null, HealthStateSnapshot? health = null,
            int entityId = 1, int ownedId = 1, EntityKey key = EntityKey.Unit_Akimbo,
            CharacterOwnershipRole role = CharacterOwnershipRole.Sub, CellCoord? anchor = null,
            Rotation4 rotation = Rotation4.R0, CellCoord[] cells = null, UnitActionUse actionUse = UnitActionUse.Available,
            int temporaryAttack = 0, int temporaryMaxHealth = 0, PositionTileModifier position = null, CharacterHeldEnvironmentState held = null,
            CharacterDebuffState debuffs = null, CharacterItemState items = null, CharacterExperienceFacts experience = null,
            CharacterStageRecord stageRecord = null) =>
            new(new EntityId(entityId), new CharacterInstanceId(ownedId), key, Owner.Player, role,
                CharacterRuntimeArea.Field, health ?? new HealthStateSnapshot(10, 20), stats ?? Stats(), null,
                anchor ?? A, rotation, cells ?? new[] { A }, actionUse, temporaryAttack,
                temporaryMaxHealth, position, held, debuffs, items, experience, stageRecord);

        private static CharacterCombatFacts Hand(int slot) =>
            new(new EntityId(1), new CharacterInstanceId(1), EntityKey.Unit_Akimbo, Owner.Player, CharacterOwnershipRole.Sub,
                CharacterRuntimeArea.Hand, new HealthStateSnapshot(10, 20), Stats(), slot, null, null,
                Array.Empty<CellCoord>());

        private static CharacterCombatFacts Enemy(int level) =>
            new(new EntityId(1), null, EntityKey.Unit_Akimbo, Owner.Enemy, null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(10, 20), Stats(), null, A, Rotation4.R0, new[] { A },
                experience: CharacterExperienceFacts.ForEnemy(level));
    }
}
