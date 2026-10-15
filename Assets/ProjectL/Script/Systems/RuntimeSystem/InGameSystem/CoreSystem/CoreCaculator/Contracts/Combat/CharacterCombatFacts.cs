using System;
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
        public CellCoord MovementCheckpointAnchor { get; }
        public int CommittedMoveDistance { get; }
        public int PendingMoveActionCost { get; }
        public int TemporaryAttackBonus { get; }
        public int TemporaryMaxHealthBonus { get; }
        public int PendingMoveDistance { get; }
        public int AttackRefundEligibleMoveCost { get; }
        public PositionTileModifier PositionModifier { get; }
        public CharacterHeldEnvironmentState HeldEnvironment { get; }

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
            CellCoord movementCheckpointAnchor,
            int committedMoveDistance,
            int pendingMoveActionCost,
            int temporaryAttackBonus = 0,
            int temporaryMaxHealthBonus = 0,
            int pendingMoveDistance = 0,
            int attackRefundEligibleMoveCost = 0,
            PositionTileModifier positionModifier = null, CharacterHeldEnvironmentState heldEnvironment = null)
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
            if (committedMoveDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(committedMoveDistance));
            if (pendingMoveActionCost < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingMoveActionCost));
            if (temporaryAttackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(temporaryAttackBonus));
            if (temporaryMaxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(temporaryMaxHealthBonus));
            if (pendingMoveDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingMoveDistance));
            if (attackRefundEligibleMoveCost < 0)
                throw new ArgumentOutOfRangeException(nameof(attackRefundEligibleMoveCost));

            ValidateIdentity(owner, ownedId, ownershipRole);
            ValidateLocation(owner, area, reserveSlotIndex, anchor, rotation, occupiedCells.Count);

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
            MovementCheckpointAnchor = movementCheckpointAnchor;
            CommittedMoveDistance = committedMoveDistance;
            PendingMoveActionCost = pendingMoveActionCost;
            TemporaryAttackBonus = temporaryAttackBonus;
            TemporaryMaxHealthBonus = temporaryMaxHealthBonus;
            PendingMoveDistance = pendingMoveDistance;
            AttackRefundEligibleMoveCost = attackRefundEligibleMoveCost;
            PositionModifier = positionModifier ?? PositionTileModifier.None;
            HeldEnvironment = heldEnvironment ?? CharacterHeldEnvironmentState.Empty;
        }

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
