using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

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
        public int? ReserveSlotIndex => _facts.ReserveSlotIndex;
        public CellCoord? Anchor => _facts.Anchor;
        public Rotation4? Rotation => _facts.Rotation;
        public IReadOnlyList<CellCoord> OccupiedCells => _facts.OccupiedCells;
        public CellCoord MovementCheckpointAnchor => _facts.MovementCheckpointAnchor;
        public int CommittedMoveDistance => _facts.CommittedMoveDistance;
        public int PendingMoveActionCost => _facts.PendingMoveActionCost;
        public int TemporaryAttackBonus => _facts.TemporaryAttackBonus;
        public int TemporaryMaxHealthBonus => _facts.TemporaryMaxHealthBonus;
        public int PendingMoveDistance => _facts.PendingMoveDistance;
        public int AttackRefundEligibleMoveCost => _facts.AttackRefundEligibleMoveCost;
        public PositionTileModifier PositionModifier => _facts.PositionModifier;
        public CharacterHeldEnvironmentState HeldEnvironment => _facts.HeldEnvironment;

        internal CharacterData(CharacterCombatFacts facts)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
        }

        public CharacterCombatFacts ToFacts() => _facts;
    }
}
