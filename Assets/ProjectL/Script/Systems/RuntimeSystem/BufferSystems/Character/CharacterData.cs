using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    /// <summary>CharacterBuffer가 공개하는 불변 캐릭터 조회값입니다.</summary>
    [Serializable]
    public sealed class CharacterData
    {
        private readonly CharacterCombatFacts _facts;

        public EntityId EntityId => _facts.EntityId;
        public CharacterInstanceId? OwnedId => _facts.OwnedId;
        public EntityKey CharacterKey => _facts.CharacterKey;
        public Owner Owner => _facts.Owner;
        public CharacterOwnershipRole? OwnershipRole => _facts.OwnershipRole;
        public CharacterRuntimeArea Area => _facts.Area;
        public HealthStateSnapshot Health => _facts.Health;
        public UnitStatsSnapshot Stats => _facts.Stats;
        /// <summary>보유 효과와 디버프를 반영한 표시 공격력입니다. 피해 전용 배율은 포함하지 않습니다.</summary>
        public int EffectiveAttackPower { get; }
        public int EffectivePhysicalDefense { get; }
        /// <summary>깃발·디버프·아이템 효과를 반영한 현재 우선도입니다.</summary>
        public int EffectivePriority { get; }
        public int? ReserveSlotIndex => _facts.ReserveSlotIndex;
        public CellCoord? Anchor => _facts.Anchor;
        public Rotation4? Rotation => _facts.Rotation;
        public IReadOnlyList<CellCoord> OccupiedCells => _facts.OccupiedCells;
        public UnitActionUse ActionUse => _facts.ActionUse;
        public int TemporaryAttackBonus => _facts.TemporaryAttackBonus;
        public int TemporaryMaxHealthBonus => _facts.TemporaryMaxHealthBonus;
        public PositionTileModifier PositionModifier => _facts.PositionModifier;
        public CharacterHeldEnvironmentState HeldEnvironment => _facts.HeldEnvironment;
        public CharacterDebuffState Debuffs => _facts.Debuffs;
        public StatusEffectSet StatusEffects => _facts.StatusEffects;
        public IReadOnlyList<StatusEffectSnapshot> StatusEffectSnapshots { get; }
        public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items.CharacterItemState Items => _facts.Items;
        /// <summary>보유 아군의 이번 전투 기록(받은·넣은 피해, 이동 거리, 처치 수)입니다. 적은 늘 비어 있습니다.</summary>
        public CharacterStageRecord StageRecord => _facts.StageRecord;

        internal CharacterData(CharacterCombatFacts facts)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            // 계산 규칙은 Core 한 곳에 두고, 같은 불변 사실에서 만든 표시값만 공개합니다.
            EffectiveAttackPower = CombatEffectiveStatsCalculator.AttackPower(_facts);
            EffectivePhysicalDefense = CombatEffectiveStatsCalculator.PhysicalDefense(_facts);
            EffectivePriority = CombatEffectiveStatsCalculator.Priority(_facts);
            StatusEffectSnapshots = StatusEffectReadModel.Capture(_facts);
        }

        public CharacterCombatFacts ToFacts() => _facts;
    }
}
