using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Buffer 계약 지식 이전 계획 M1: 계약 타입의 공개 속성 목록을 고정한다.
    /// 필드를 추가·삭제하면 이 시험이 실패한다. 실패하면 메시지에 적힌 곳을 먼저 고치고, 그다음 이 목록을 갱신한다.
    /// 목록에서 이름을 빼거나 시험을 끄는 방식으로 통과시키지 않는다.
    /// </summary>
    public sealed class ContractShapeGuardTests
    {
        [Test]
        public void MovementUndoContractsRequireExplicitComparisonAndCopyDecisions()
        {
            AssertProperties(typeof(MovementUndoCheckpoint), "SameValues·원본 생성 및 게시 검증",
                "BattleInstanceId", "ControllerTurnSequence", "MoveCommandNumber", "ActorId", "Anchor", "Rotation",
                "OccupiedCells", "RecordedDistance", "OriginFacts", "ExpectedActorAfterMove", "Movement", "Participants", "Effects");
            AssertProperties(typeof(MovementUndoOriginFacts), "Capture·SameValues·원점 의존 범위",
                "Cell", "Tiles", "TerrainSourceIds", "ObjectSourceIds", "Thorns", "Souls");
            AssertProperties(typeof(MovementUndoState), "SameValues·WithoutAffected", "Checkpoints");
            AssertProperties(typeof(CombatCommandState), "With·SameValues·SameGameplayValues·이동 투영",
                "MovementUndo", "Gimmicks", "BoardWidth", "BoardHeight", "Phase", "IsNight", "ItemRandomState", "Rewards",
                "Structures", "ThornFields", "Turn", "TraitTurn", "Environment", "EnvironmentProgress", "WallBlocking", "Objective", "Allocation");
            AssertProperties(typeof(CombatActionAvailability), "생성자 기본값·WithSourceRevision",
                "SourceRevision", "MovementUndo", "BattleInstanceId", "ControllerTurnSequence", "ActorId", "ActionUse",
                "MovementAllowance", "MoveFailure", "AttackFailure", "CanMove", "CanAttack");
        }

        [TestCase(typeof(CombatCommandState))]
        [TestCase(typeof(CombatActionAvailability))]
        [TestCase(typeof(UnitStatsSnapshot))]
        [TestCase(typeof(CharacterBaselineStatsSnapshot))]
        public void CombatContracts_ExposeNoPricesOrBudgets(Type boundary)
        {
            foreach (var property in new[] { "PlayerBudget", "EnemyBudget", "MoveCost", "AttackCost" })
                Assert.That(boundary.GetProperty(property), Is.Null, boundary.Name + "." + property);
            Assert.That(typeof(UnitActionUseSettlementSnapshot).GetProperties().Select(p => p.Name),
                Is.EquivalentTo(new[] { "ActorId", "BeforeActionUse", "FinalActionUse" }));
        }

        [Test]
        public void MainCharacterPersistentState_PropertiesAreFixed()
        {
            AssertProperties(typeof(MainCharacterPersistentState),
                "SameValues, Rebuild(With 공통 경로), 전투 사실에 비치는 값이면 CharacterCombatFacts.MatchesMainState",
                nameof(MainCharacterPersistentState.CurrentHealth),
                nameof(MainCharacterPersistentState.PersistentMaxHealth),
                nameof(MainCharacterPersistentState.PersistentAttack),
                nameof(MainCharacterPersistentState.PersistentDefense));
        }

        [Test]
        public void CharacterCombatFacts_PropertiesAreFixed()
        {
            AssertProperties(typeof(CharacterCombatFacts),
                "SameValues, SameIdentity(식별 값이면), Rebuild·With·WithPlacement, 메인 지속 값이 비치면 MatchesMainState",
                nameof(CharacterCombatFacts.EntityId), nameof(CharacterCombatFacts.OwnedId),
                nameof(CharacterCombatFacts.CharacterKey), nameof(CharacterCombatFacts.Owner),
                nameof(CharacterCombatFacts.OwnershipRole), nameof(CharacterCombatFacts.Area),
                nameof(CharacterCombatFacts.ActionUse), nameof(CharacterCombatFacts.Health), nameof(CharacterCombatFacts.Stats),
                nameof(CharacterCombatFacts.ReserveSlotIndex), nameof(CharacterCombatFacts.Anchor),
                nameof(CharacterCombatFacts.Rotation), nameof(CharacterCombatFacts.OccupiedCells), nameof(CharacterCombatFacts.TemporaryAttackBonus),
                nameof(CharacterCombatFacts.TemporaryMaxHealthBonus), nameof(CharacterCombatFacts.PositionModifier),
                nameof(CharacterCombatFacts.HeldEnvironment), nameof(CharacterCombatFacts.Debuffs),
                nameof(CharacterCombatFacts.Items), nameof(CharacterCombatFacts.Experience),
                nameof(CharacterCombatFacts.StageRecord), nameof(CharacterCombatFacts.TraitEffects), nameof(CharacterCombatFacts.StatusEffects));
        }

        [Test]
        public void UnitStatsSnapshot_PropertiesAreFixed()
        {
            AssertProperties(typeof(UnitStatsSnapshot), "SameValues, With",
                nameof(UnitStatsSnapshot.MoveMethod), nameof(UnitStatsSnapshot.MoveRange),
                nameof(UnitStatsSnapshot.AppointRange), nameof(UnitStatsSnapshot.AttackRange),
                nameof(UnitStatsSnapshot.AttackPower), nameof(UnitStatsSnapshot.SoulAttackBonus),
                nameof(UnitStatsSnapshot.SoulMaxHealthBonus), nameof(UnitStatsSnapshot.ArtifactAttackBonus),
                nameof(UnitStatsSnapshot.PositionAttackBonus), nameof(UnitStatsSnapshot.TraitAttackBonus), nameof(UnitStatsSnapshot.EffectiveAttackPower),
                nameof(UnitStatsSnapshot.PhysicalDefense),
                nameof(UnitStatsSnapshot.Priority),
                nameof(UnitStatsSnapshot.TraitStatuses), nameof(UnitStatsSnapshot.Category),
                nameof(UnitStatsSnapshot.SpecializationCategory), nameof(UnitStatsSnapshot.IdentityCategoryKey), nameof(UnitStatsSnapshot.CombatCategoryKey));
        }

        [Test]
        public void CategorySnapshot_PropertiesAreFixed()
        {
            AssertProperties(typeof(CategorySnapshot), "CategorySnapshot.Same",
                nameof(CategorySnapshot.Key));
        }

        [Test]
        public void TraitState_PropertiesAreFixed()
        {
            AssertProperties(typeof(CharacterDebuffState), "SameValues·기존 디버프 재구성·종료 정리·Buffer 설치",
                nameof(CharacterDebuffState.Intimidation), nameof(CharacterDebuffState.DefenseReduction),
                nameof(CharacterDebuffState.HuntersMark), nameof(CharacterDebuffState.Disarm), nameof(CharacterDebuffState.AnkleBreaker),
                nameof(CharacterDebuffState.ArmorBreak));
            AssertProperties(typeof(DisarmState), "SameValues·진영 종료·갱신 출처",
                nameof(DisarmState.Origin), nameof(DisarmState.RemainingAffectedSideTurnEnds));
            AssertProperties(typeof(AnkleBreakerState), "SameValues·진영 종료·갱신 출처",
                nameof(AnkleBreakerState.Origin), nameof(AnkleBreakerState.RemainingAffectedSideTurnEnds));
            AssertProperties(typeof(CharacterTraitEffects), "생성자 복사·SameValues·공격력 투영",
                nameof(CharacterTraitEffects.AttackGrants), nameof(CharacterTraitEffects.AttackBonus),
                nameof(CharacterTraitEffects.Shields), nameof(CharacterTraitEffects.ActiveShieldTotal), nameof(CharacterTraitEffects.CohesionDefenseBonus));
            AssertProperties(typeof(TraitShieldGrant), "SameValues·출처별 잔량·배치 자격·진영 종료 초기화",
                nameof(TraitShieldGrant.Origin), nameof(TraitShieldGrant.RemainingBudget), nameof(TraitShieldGrant.Eligible),
                nameof(TraitShieldGrant.LastResetControllerTurnCount));
            AssertProperties(typeof(TraitAttackGrant), "SameValues·출처·만료 계약",
                nameof(TraitAttackGrant.Origin), nameof(TraitAttackGrant.Amount), nameof(TraitAttackGrant.RemainingControllerTurnEnds));
            AssertProperties(typeof(TraitTurnFacts), "SameValues·With·명령 합성·Buffer 설치",
                nameof(TraitTurnFacts.PlayerAttackActionCount), nameof(TraitTurnFacts.EnemyAttackActionCount));
            AssertProperties(typeof(OwnedCharacterRecord), "SameValues·XP·성장 재구성·전투 준비·새 Run 초기화",
                nameof(OwnedCharacterRecord.Id), nameof(OwnedCharacterRecord.CharacterKey), nameof(OwnedCharacterRecord.Role),
                nameof(OwnedCharacterRecord.Level), nameof(OwnedCharacterRecord.Experience), nameof(OwnedCharacterRecord.Growth),
                nameof(OwnedCharacterRecord.HunterAttackBonus), nameof(OwnedCharacterRecord.SpecializationCategory));
        }

        [Test]
        public void CharacterRewardReceipt_PropertiesAreFixed()
        {
            AssertProperties(typeof(CharacterRewardReceipt), "CharacterRewardReceipt.SameValues(합계 같은 파생 값은 제외)",
                nameof(CharacterRewardReceipt.CharacterId), nameof(CharacterRewardReceipt.CharacterKey),
                nameof(CharacterRewardReceipt.KillExperience), nameof(CharacterRewardReceipt.ClearExperience),
                nameof(CharacterRewardReceipt.TotalExperience), nameof(CharacterRewardReceipt.LevelBefore),
                nameof(CharacterRewardReceipt.ExperienceBefore), nameof(CharacterRewardReceipt.RequiredExperienceBefore),
                nameof(CharacterRewardReceipt.LevelAfter), nameof(CharacterRewardReceipt.ExperienceAfter),
                nameof(CharacterRewardReceipt.RequiredExperienceAfter), nameof(CharacterRewardReceipt.StageRecord));
        }

        [Test]
        public void CharacterStageRecord_PropertiesAreFixed()
        {
            AssertProperties(typeof(CharacterStageRecord), "CharacterStageRecord.SameValues·IsEmpty·IsAtLeast·Add",
                nameof(CharacterStageRecord.DamageTaken), nameof(CharacterStageRecord.DamageDealt),
                nameof(CharacterStageRecord.DistanceMoved), nameof(CharacterStageRecord.Kills),
                nameof(CharacterStageRecord.IsEmpty));
        }

        private static void AssertProperties(Type type, string mustUpdate, params string[] expected)
        {
            var actual = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var sorted = expected.OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var added = actual.Except(sorted).ToArray();
            var removed = sorted.Except(actual).ToArray();
            Assert.That(added.Length == 0 && removed.Length == 0, Is.True,
                $"{type.Name}의 공개 속성이 바뀌었다(추가: {string.Join(", ", added)} / 삭제: {string.Join(", ", removed)}). " +
                $"먼저 {mustUpdate}에 반영한 뒤 ContractShapeGuardTests 목록을 갱신하라.");
        }
    }
}
