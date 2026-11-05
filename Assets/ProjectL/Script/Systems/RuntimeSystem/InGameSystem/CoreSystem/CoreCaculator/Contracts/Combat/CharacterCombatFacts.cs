using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>Core 계산과 Buffer 반영 사이에서 사용하는 불변 캐릭터 사실 집합이다.</summary>
    public sealed class CharacterCombatFacts
    {
        // ⚠ 필드를 추가·삭제할 때 반드시 함께 고친다: SameValues, SameIdentity(식별 값이면), Rebuild(With* 공통 경로),
        //   ContractShapeGuardTests 목록. 메인 지속 값이 비치는 값이면 MatchesMainState도. 건너뛰면 ContractShapeGuardTests가 실패한다.
        private readonly IReadOnlyList<CellCoord> _occupiedCells;

        public EntityId EntityId { get; }
        public CharacterInstanceId? OwnedId { get; }
        public EntityKey CharacterKey { get; }
        public Owner Owner { get; }
        public CharacterOwnershipRole? OwnershipRole { get; }
        public CharacterRuntimeArea Area { get; }
        public HealthStateSnapshot Health { get; }
        public UnitStatsSnapshot Stats { get; }
        public int? ReserveSlotIndex { get; }
        public CellCoord? Anchor { get; }
        public Rotation4? Rotation { get; }
        public IReadOnlyList<CellCoord> OccupiedCells => _occupiedCells;
        public UnitActionUse ActionUse { get; }
        public int TemporaryAttackBonus { get; }
        public int TemporaryMaxHealthBonus { get; }
        public PositionTileModifier PositionModifier { get; }
        public CharacterHeldEnvironmentState HeldEnvironment { get; }
        public CharacterDebuffState Debuffs { get; }
        public CharacterItemState Items { get; }
        public StatusEffectSet StatusEffects { get; }
        public CharacterTraitEffects TraitEffects { get; }
        /// <summary>적의 생성 레벨 또는 아군의 이번 전투 획득 경험치다. 사실을 다시 만드는 곳은 이 값을 그대로 넘겨야 한다.</summary>
        public CharacterExperienceFacts Experience { get; }
        /// <summary>보유 아군의 이번 전투 행동 기록이다. 획득 경험치와 수명이 같다. 사실을 다시 만드는 곳은 이 값을 그대로 넘겨야 한다.</summary>
        public CharacterStageRecord StageRecord { get; }

        public CharacterCombatFacts(
            EntityId entityId,
            CharacterInstanceId? ownedId,
            EntityKey characterKey,
            Owner owner,
            CharacterOwnershipRole? ownershipRole,
            CharacterRuntimeArea area,
            HealthStateSnapshot health,
            UnitStatsSnapshot stats,
            int? reserveSlotIndex,
            CellCoord? anchor,
            Rotation4? rotation,
            IReadOnlyList<CellCoord> occupiedCells,
            UnitActionUse actionUse = UnitActionUse.Deferred,
            int temporaryAttackBonus = 0,
            int temporaryMaxHealthBonus = 0,
            PositionTileModifier positionModifier = null, CharacterHeldEnvironmentState heldEnvironment = null,
            CharacterDebuffState debuffs = null, CharacterItemState items = null,
            CharacterExperienceFacts experience = null, CharacterStageRecord stageRecord = null,
            CharacterTraitEffects traitEffects = null, StatusEffectSet statusEffects = null)
        {
            if (entityId.IsEmpty)
                throw new ArgumentException("전투 개체 ID가 필요합니다.", nameof(entityId));
            if (characterKey == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(characterKey));
            if (owner != Owner.Player && owner != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(owner));
            if (Enum.IsDefined(typeof(CharacterRuntimeArea), area) == false)
                throw new ArgumentOutOfRangeException(nameof(area));
            if (occupiedCells == null)
                throw new ArgumentNullException(nameof(occupiedCells));
            if (temporaryAttackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(temporaryAttackBonus));
            if (temporaryMaxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(temporaryMaxHealthBonus));

            if (!Enum.IsDefined(typeof(UnitActionUse), actionUse)) throw new ArgumentOutOfRangeException(nameof(actionUse));
            ActionUse = actionUse;
            ValidateIdentity(owner, ownedId, ownershipRole);
            ValidateLocation(owner, area, reserveSlotIndex, anchor, rotation, occupiedCells.Count);
            experience ??= owner == Owner.Enemy ? CharacterExperienceFacts.EnemyDefault : CharacterExperienceFacts.AllyDefault;
            if ((owner == Owner.Enemy) != experience.EnemyLevel.HasValue)
                throw new ArgumentException("적은 생성 레벨만, 아군은 획득 경험치만 가질 수 있습니다.", nameof(experience));
            if (experience.Earned != 0 && ownedId.HasValue == false)
                throw new ArgumentException("획득 경험치는 보유 개체만 가질 수 있습니다.", nameof(experience));
            stageRecord ??= CharacterStageRecord.Empty;
            if (stageRecord.IsEmpty == false && ownedId.HasValue == false)
                throw new ArgumentException("스테이지 기록은 보유 개체만 가질 수 있습니다.", nameof(stageRecord));

            var cells = new CellCoord[occupiedCells.Count];
            for (int i = 0; i < occupiedCells.Count; i++)
                cells[i] = occupiedCells[i];

            EntityId = entityId;
            OwnedId = ownedId;
            CharacterKey = characterKey;
            Owner = owner;
            OwnershipRole = ownershipRole;
            Area = area;
            Health = health;
            Stats = stats;
            ReserveSlotIndex = reserveSlotIndex;
            Anchor = anchor;
            Rotation = rotation;
            _occupiedCells = Array.AsReadOnly(cells);
            TemporaryAttackBonus = temporaryAttackBonus;
            TemporaryMaxHealthBonus = temporaryMaxHealthBonus;
            PositionModifier = positionModifier ?? PositionTileModifier.None;
            HeldEnvironment = heldEnvironment ?? CharacterHeldEnvironmentState.Empty;
            Items = items ?? CharacterItemState.Empty;
            StatusEffects = statusEffects ?? CharacterStatusCompatibility.Import(debuffs, traitEffects);
            if (statusEffects != null && traitEffects != null && !CharacterStatusCompatibility.MatchesTraitEffects(statusEffects, traitEffects))
                throw new ArgumentException("정규 효과와 레거시 특성 효과가 일치하지 않습니다.", nameof(traitEffects));
            if (statusEffects != null && debuffs != null && !CharacterStatusCompatibility.MatchesDebuffs(statusEffects, debuffs))
                throw new ArgumentException("정규 효과와 레거시 디버프가 일치하지 않습니다.", nameof(debuffs));
            Debuffs = CharacterStatusCompatibility.ProjectDebuffs(StatusEffects, debuffs);
            traitEffects ??= CharacterTraitEffects.Empty;
            TraitEffects = CharacterStatusCompatibility.ProjectTraitEffects(StatusEffects, traitEffects);
            Experience = experience;
            StageRecord = stageRecord;
        }

        /// <summary>
        /// 메인 전투 사실이 지속 원본을 그대로 비추는지다. 현재 체력·공격·최대 행동 비용·이동 범위·이동 비용·방어가 지속 값과 같아야 한다.
        /// 메인 지속 값과 전투 사실의 대응은 이 함수 한 곳이 정한다.
        /// </summary>
        public bool MatchesMainState(MainCharacterPersistentState main)
        {
            if (main == null) throw new ArgumentNullException(nameof(main));
            return Health.CurrentHealth == main.CurrentHealth &&
                   Stats.AttackPower == main.PersistentAttack &&
                   Stats.PhysicalDefense == main.PersistentDefense;
        }

        /// <summary>식별 값(개체·보유·정의·소유자·보유 역할·적 생성 레벨)이 같은지다. 인자가 null이면 예외가 난다.</summary>
        public bool SameIdentity(CharacterCombatFacts other)
        {
            return EntityId == other.EntityId &&
                   OwnedId == other.OwnedId &&
                   CharacterKey == other.CharacterKey &&
                   Owner == other.Owner &&
                   OwnershipRole == other.OwnershipRole &&
                   // 적 레벨은 생성 때 정해지고 전투 동안 바뀌지 않는다. 사본이 값을 잃으면 식별 변경으로 드러난다.
                   Experience.EnemyLevel == other.Experience.EnemyLevel;
        }

        /// <summary>
        /// 모든 사실 값이 같은지다. 점유 칸은 순서까지 비교한다. 인자가 null이면 예외가 난다(옛 Buffer 비교기와 같다).
        /// 필드를 추가하면 여기에 비교를 더한다(ContractShapeGuardTests가 알려 준다).
        /// </summary>
        public bool SameValues(CharacterCombatFacts other)
        {
            if (SameIdentity(other) == false ||
                Area != other.Area || ActionUse != other.ActionUse ||
                Health.CurrentHealth != other.Health.CurrentHealth ||
                Health.MaxHealth != other.Health.MaxHealth ||
                Stats.SameValues(other.Stats) == false ||
                ReserveSlotIndex != other.ReserveSlotIndex ||
                Anchor != other.Anchor ||
                Rotation != other.Rotation ||
                TemporaryAttackBonus != other.TemporaryAttackBonus ||
                TemporaryMaxHealthBonus != other.TemporaryMaxHealthBonus ||
                !PositionModifier.SameValues(other.PositionModifier) ||
                !HeldEnvironment.SameValues(other.HeldEnvironment) ||
                !Debuffs.SameValues(other.Debuffs) ||
                !Items.SameValues(other.Items) ||
                !StatusEffects.SameValues(other.StatusEffects) ||
                !TraitEffects.SameValues(other.TraitEffects) ||
                !Experience.SameValues(other.Experience) ||
                !StageRecord.SameValues(other.StageRecord) ||
                OccupiedCells.Count != other.OccupiedCells.Count)
                return false;

            for (int i = 0; i < OccupiedCells.Count; i++)
                if (OccupiedCells[i] != other.OccupiedCells[i])
                    return false;

            return true;
        }

        /// <summary>체력만 바꾸며 행동 사용과 위치를 보존한다.</summary>
        public CharacterCombatFacts WithHealth(HealthStateSnapshot health) => With(health: health);

        /// <summary>지정한 값만 바꾸며 null 선택 인자는 기존 값을 보존한다.</summary>
        public CharacterCombatFacts With(HealthStateSnapshot? health = null, UnitStatsSnapshot? stats = null,
            PositionTileModifier positionModifier = null, CharacterHeldEnvironmentState heldEnvironment = null,
            CharacterDebuffState debuffs = null, CharacterItemState items = null,
            CharacterExperienceFacts experience = null, UnitActionUse? actionUse = null,
            CharacterStageRecord stageRecord = null, CharacterTraitEffects traitEffects = null,
            int? temporaryAttackBonus = null, int? temporaryMaxHealthBonus = null) =>
            Rebuild(health ?? Health, stats ?? Stats, Area, ReserveSlotIndex, Anchor, Rotation, OccupiedCells,
                actionUse ?? ActionUse, positionModifier ?? PositionModifier, heldEnvironment ?? HeldEnvironment,
                debuffs ?? Debuffs, items ?? Items, experience ?? Experience, stageRecord ?? StageRecord, traitEffects ?? TraitEffects,
                CharacterStatusCompatibility.ReplaceTraitEffects(CharacterStatusCompatibility.ReplaceDebuffs(StatusEffects, debuffs), traitEffects),
                temporaryAttackBonus, temporaryMaxHealthBonus);

        /// <summary>정규 효과 원본만 바꾸며 아직 이전하지 않은 레거시 효과와 다른 사실은 보존합니다.</summary>
        public CharacterCombatFacts WithStatusEffects(StatusEffectSet statuses) =>
            Rebuild(Health, Stats, Area, ReserveSlotIndex, Anchor, Rotation, OccupiedCells, ActionUse,
                PositionModifier, HeldEnvironment, Debuffs, Items, Experience, StageRecord, TraitEffects,
                statuses ?? throw new ArgumentNullException(nameof(statuses)));

        /// <summary>위치 묶음의 null은 비우기를 뜻한다. 행동 사용은 명시한 경우만 바꾼다.</summary>
        public CharacterCombatFacts WithPlacement(CharacterRuntimeArea area, int? reserveSlotIndex, CellCoord? anchor,
            Rotation4? rotation, IReadOnlyList<CellCoord> occupiedCells, HealthStateSnapshot? health = null,
            UnitStatsSnapshot? stats = null, UnitActionUse? actionUse = null) =>
            Rebuild(health ?? Health, stats ?? Stats, area, reserveSlotIndex, anchor, rotation, occupiedCells,
                actionUse ?? ActionUse, PositionModifier, HeldEnvironment, Debuffs, Items, Experience, StageRecord, TraitEffects, StatusEffects);

        private CharacterCombatFacts Rebuild(HealthStateSnapshot health, UnitStatsSnapshot stats, CharacterRuntimeArea area,
            int? reserveSlotIndex, CellCoord? anchor, Rotation4? rotation, IReadOnlyList<CellCoord> occupiedCells,
            UnitActionUse actionUse, PositionTileModifier positionModifier, CharacterHeldEnvironmentState heldEnvironment,
            CharacterDebuffState debuffs, CharacterItemState items, CharacterExperienceFacts experience,
            CharacterStageRecord stageRecord, CharacterTraitEffects traitEffects, StatusEffectSet statusEffects,
            int? temporaryAttackBonus = null, int? temporaryMaxHealthBonus = null) =>
            new CharacterCombatFacts(EntityId, OwnedId, CharacterKey, Owner, OwnershipRole, area, health, stats,
                reserveSlotIndex, anchor, rotation, occupiedCells, actionUse, temporaryAttackBonus ?? TemporaryAttackBonus, temporaryMaxHealthBonus ?? TemporaryMaxHealthBonus,
                positionModifier, heldEnvironment, CharacterStatusCompatibility.ProjectDebuffs(statusEffects, debuffs), items, experience, stageRecord,
                CharacterStatusCompatibility.ProjectTraitEffects(statusEffects, traitEffects), statusEffects);

        private static void ValidateIdentity(Owner owner, CharacterInstanceId? ownedId,
            CharacterOwnershipRole? ownershipRole)
        {
            if (ownedId.HasValue != ownershipRole.HasValue)
                throw new ArgumentException("보유 ID와 보유 역할은 함께 지정해야 합니다.");

            if (ownedId.HasValue == false)
                return;

            if (ownedId.Value.IsEmpty)
                throw new ArgumentException("보유 ID는 비어 있을 수 없습니다.", nameof(ownedId));
            if (owner != Owner.Player)
                throw new ArgumentException("보유 캐릭터는 Player 소유여야 합니다.", nameof(owner));
            if (ownershipRole != CharacterOwnershipRole.Main &&
                ownershipRole != CharacterOwnershipRole.Sub)
                throw new ArgumentOutOfRangeException(nameof(ownershipRole));
        }

        private static void ValidateLocation(Owner owner, CharacterRuntimeArea area,
            int? reserveSlotIndex, CellCoord? anchor, Rotation4? rotation, int occupiedCellCount)
        {
            switch (area)
            {
                case CharacterRuntimeArea.Hand:
                    if (owner != Owner.Player || reserveSlotIndex.HasValue == false ||
                        reserveSlotIndex.Value < 0 || anchor.HasValue ||
                        rotation.HasValue || occupiedCellCount != 0)
                        throw new ArgumentException("손패 캐릭터의 위치 값이 올바르지 않습니다.");
                    break;

                case CharacterRuntimeArea.Field:
                    if (reserveSlotIndex.HasValue || anchor.HasValue == false ||
                        rotation.HasValue == false || occupiedCellCount == 0)
                        throw new ArgumentException("필드 캐릭터의 위치 값이 올바르지 않습니다.");
                    if (Enum.IsDefined(typeof(Rotation4), rotation.Value) == false)
                        throw new ArgumentOutOfRangeException(nameof(rotation));
                    break;

                case CharacterRuntimeArea.EnemyWaiting:
                    if (owner != Owner.Enemy || reserveSlotIndex.HasValue || anchor.HasValue ||
                        rotation.HasValue || occupiedCellCount != 0)
                        throw new ArgumentException("대기 중인 적의 위치 값이 올바르지 않습니다.");
                    break;

                case CharacterRuntimeArea.Excluded:
                    if (reserveSlotIndex.HasValue || anchor.HasValue || rotation.HasValue ||
                        occupiedCellCount != 0)
                        throw new ArgumentException("제외된 캐릭터는 위치 값을 가질 수 없습니다.");
                    break;
            }
        }
    }
}
