using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class TraitActivationSnapshot
    {
        public EntityId SourceUnitId { get; }
        public EntityId? TargetId => TargetIds.Count == 1 ? TargetIds[0] : (EntityId?)null;
        public IReadOnlyList<EntityId> TargetIds { get; }
        public IReadOnlyList<CellCoord> TargetCells { get; }
        public CombatDamageOrigin ResultOrigin { get; }
        public Rotation4? AttackDirection { get; }
        public CellCoord? TargetCell { get; }
        public TraitKey TraitKey { get; }
        public TraitActivationKind TraitActivationKind { get; }
        public int Magnitude { get; }

        public AttackInterceptionSnapshot Interception { get; }
        public AttackOutcomeSnapshot ChildAttack { get; }
        private TraitActivationSnapshot(TraitActivationSnapshot source, AttackInterceptionSnapshot interception, AttackOutcomeSnapshot childAttack)
            : this(source.SourceUnitId, source.TargetId, source.TraitKey, source.TraitActivationKind, source.Magnitude,
                source.TargetIds, source.TargetCells, source.ResultOrigin, source.AttackDirection, source.TargetCell)
        { Interception = interception; ChildAttack = childAttack; }
        public TraitActivationSnapshot WithInterception(AttackInterceptionSnapshot interception)
        {
            if (interception == null) throw new ArgumentNullException(nameof(interception));
            if (ChildAttack != null || TraitActivationKind != TraitActivationKind.DamageRedirected || SourceUnitId != interception.GuardianId ||
                TargetId != interception.ProtectedId || ResultOrigin == null || !ResultOrigin.CreditedCharacterId.HasValue ||
                ResultOrigin.CreditedCharacterId.Value.IsEmpty || ResultOrigin.AttackGroup <= 0 ||
                TraitKey == TraitKey.Sacrifice && interception.GuardianId != interception.ActualRecipientId)
                throw new ArgumentException("가로채기와 발동 주체가 일치하지 않습니다.", nameof(interception));
            return new TraitActivationSnapshot(this, interception, null);
        }
        public TraitActivationSnapshot WithChildAttack(AttackOutcomeSnapshot childAttack)
        {
            if (childAttack == null) throw new ArgumentNullException(nameof(childAttack));
            if (Interception != null || (TraitActivationKind != TraitActivationKind.FollowUpAttack && TraitActivationKind != TraitActivationKind.KillChainAttack) ||
                ResultOrigin == null || ResultOrigin.CreditedCharacterId != SourceUnitId || ResultOrigin.AttackGroup <= 0)
                throw new ArgumentException("추가 공격 시작 정보가 필요합니다.", nameof(childAttack));
            childAttack.ValidateScope(SourceUnitId, ResultOrigin.AttackGroup);
            return new TraitActivationSnapshot(this, null, childAttack);
        }

        public TraitActivationSnapshot(
            EntityId sourceUnitId,
            EntityId? targetId,
            TraitKey traitKey,
            TraitActivationKind traitActivationKind,
            int magnitude,
            IReadOnlyList<EntityId> targetIds = null,
            IReadOnlyList<CellCoord> targetCells = null,
            CombatDamageOrigin resultOrigin = null,
            Rotation4? attackDirection = null,
            CellCoord? targetCell = null)
        {
            if (sourceUnitId == EntityId.Empty)
                throw new ArgumentException(nameof(sourceUnitId));
            if (targetId.HasValue && targetId.Value == EntityId.Empty)
                throw new ArgumentException(nameof(targetId));
            if (traitKey == TraitKey.None || Enum.IsDefined(typeof(TraitKey), traitKey) == false)
                throw new ArgumentOutOfRangeException(nameof(traitKey));
            if (traitActivationKind == TraitActivationKind.None ||
                Enum.IsDefined(typeof(TraitActivationKind), traitActivationKind) == false)
                throw new ArgumentOutOfRangeException(nameof(traitActivationKind));
            if (magnitude < 0)
                throw new ArgumentOutOfRangeException(nameof(magnitude));

            var ids = targetIds == null ? targetId.HasValue ? new[] { targetId.Value } : Array.Empty<EntityId>() : targetIds.ToArray();
            if (ids.Any(id => id.IsEmpty) || ids.Distinct().Count() != ids.Length)
                throw new ArgumentException("대상 목록에는 유효하고 중복 없는 ID가 필요합니다.", nameof(targetIds));
            if (targetId.HasValue && (ids.Length != 1 || ids[0] != targetId.Value))
                throw new ArgumentException("단일 대상과 대상 목록이 충돌합니다.", nameof(targetIds));
            var cells = (targetCells ?? Array.Empty<CellCoord>()).ToArray();
            if (cells.Distinct().Count() != cells.Length)
                throw new ArgumentException("대상 칸은 중복될 수 없습니다.", nameof(targetCells));
            if (attackDirection.HasValue && !Enum.IsDefined(typeof(Rotation4), attackDirection.Value))
                throw new ArgumentOutOfRangeException(nameof(attackDirection));

            SourceUnitId = sourceUnitId;
            TargetIds = Array.AsReadOnly(ids);
            TargetCells = Array.AsReadOnly(cells);
            ResultOrigin = resultOrigin;
            AttackDirection = attackDirection;
            TargetCell = targetCell;
            TraitKey = traitKey;
            TraitActivationKind = traitActivationKind;
            Magnitude = magnitude;
        }
    }
}
