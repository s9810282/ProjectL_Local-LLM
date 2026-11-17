using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    /// <summary>효과가 끝나는 방식입니다. 각 효과 타입이 <see cref="AppliedStatus.Lifetime"/>으로 선언합니다.</summary>
    public enum StatusEffectLifetimeKind
    {
        ControllerTurnEnds = 1,
        AffectedSideTurnEnds = 2,
        AttackCompletion = 3,
        BattleEnd = 4,
        ReconciledBudget = 5,
        /// <summary>
        /// 값·시계·예산 없이 지금 사실이 조건을 만족하는 동안만 있는 상태입니다. 보유 조정이 사실이 바뀔 때마다 다시 계산합니다.
        /// 이동 되돌리기는 이 상태를 영수증 없이 되돌린 자리의 다시 계산으로 복원합니다(<see cref="Results.MovementUndoResult"/>).
        /// </summary>
        CurrentEligibility = 6,
        UntilReserveReturnOrDeath = 7,
        AffectedSideTurnStarts = 8,
        NextAffectedSideTurnEnd = 9
    }

    /// <summary>수신자가 보유하는 불변 효과 값입니다. 출처 없는 값은 레거시 사실 복원에만 사용합니다.</summary>
    public abstract class AppliedStatus
    {
        public StatusEffectKey Key { get; }
        public EffectOriginKey? Origin { get; }

        protected AppliedStatus(StatusEffectKey key, EffectOriginKey? origin)
        {
            _ = StatusEffectCatalog.ClassificationOf(key);
            if (origin.HasValue && !origin.Value.IsValid) throw new ArgumentException("유효한 효과 출처가 필요합니다.", nameof(origin));
            Key = key;
            Origin = origin;
        }

        /// <summary>이 효과가 끝나는 방식입니다. 조회값과 이동 되돌리기가 이 값 하나를 읽습니다.</summary>
        public abstract StatusEffectLifetimeKind Lifetime { get; }

        public abstract bool SameValues(AppliedStatus other);
    }

    public enum WeaknessExposureLifetimeKind
    {
        AffectedSideTurnEnds = 1,
        UntilReserveReturnOrDeath = 2
    }

    public sealed class WeaknessExposureStatus : AppliedStatus
    {
        public int IncreasePercent { get; }
        public WeaknessExposureLifetimeKind LifetimeKind { get; }
        public int? RemainingAffectedSideTurnEnds { get; }
        public MovementEffectId? Movement { get; }
        public override StatusEffectLifetimeKind Lifetime => LifetimeKind == WeaknessExposureLifetimeKind.AffectedSideTurnEnds
            ? StatusEffectLifetimeKind.AffectedSideTurnEnds : StatusEffectLifetimeKind.UntilReserveReturnOrDeath;

        public WeaknessExposureStatus(EffectOriginKey origin, int increasePercent, int remainingAffectedSideTurnEnds)
            : this(origin, increasePercent, WeaknessExposureLifetimeKind.AffectedSideTurnEnds, remainingAffectedSideTurnEnds)
        {
        }

        public WeaknessExposureStatus(EffectOriginKey origin, int increasePercent, WeaknessExposureLifetimeKind lifetimeKind,
            int? remainingAffectedSideTurnEnds = null, MovementEffectId? movement = null)
            : base(StatusEffectKey.WeaknessExposure, origin)
        {
            if (increasePercent < 0) throw new ArgumentOutOfRangeException(nameof(increasePercent));
            if (!Enum.IsDefined(typeof(WeaknessExposureLifetimeKind), lifetimeKind)) throw new ArgumentOutOfRangeException(nameof(lifetimeKind));
            if (lifetimeKind == WeaknessExposureLifetimeKind.AffectedSideTurnEnds
                ? !remainingAffectedSideTurnEnds.HasValue || remainingAffectedSideTurnEnds.Value < 1
                : remainingAffectedSideTurnEnds.HasValue)
                throw new ArgumentOutOfRangeException(nameof(remainingAffectedSideTurnEnds));
            if (movement.HasValue && (!movement.Value.IsValid || origin.Kind != EffectOwnerKind.Trait || origin.OwnerInstanceId != movement.Value.ActorId.Value))
                throw new ArgumentException("이동 기여분은 이동 행위자의 특성 출처와 일치해야 합니다.", nameof(movement));
            IncreasePercent = increasePercent;
            LifetimeKind = lifetimeKind;
            RemainingAffectedSideTurnEnds = remainingAffectedSideTurnEnds;
            Movement = movement;
        }

        public override bool SameValues(AppliedStatus other) => other is WeaknessExposureStatus status &&
            Origin == status.Origin && IncreasePercent == status.IncreasePercent && LifetimeKind == status.LifetimeKind &&
            RemainingAffectedSideTurnEnds == status.RemainingAffectedSideTurnEnds && Movement == status.Movement;
    }

    public sealed class AttackDecreaseStatus : AppliedStatus
    {
        public int ReductionPercent { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.AttackCompletion;

        public AttackDecreaseStatus(EffectOriginKey? origin, int reductionPercent)
            : base(StatusEffectKey.AttackDecrease, origin)
        {
            if (reductionPercent < 1 || reductionPercent > 100) throw new ArgumentOutOfRangeException(nameof(reductionPercent));
            ReductionPercent = reductionPercent;
        }

        public override bool SameValues(AppliedStatus other) => other is AttackDecreaseStatus status &&
            Origin == status.Origin && ReductionPercent == status.ReductionPercent;
    }

    public sealed class ActionRestrictionStatus : AppliedStatus
    {
        public int RemainingAffectedSideTurnEnds { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.AffectedSideTurnEnds;

        public ActionRestrictionStatus(StatusEffectKey key, EffectOriginKey origin, int remainingAffectedSideTurnEnds) : base(key, origin)
        {
            if (!Supports(key)) throw new ArgumentOutOfRangeException(nameof(key));
            if (remainingAffectedSideTurnEnds < 1) throw new ArgumentOutOfRangeException(nameof(remainingAffectedSideTurnEnds));
            RemainingAffectedSideTurnEnds = remainingAffectedSideTurnEnds;
        }

        public override bool SameValues(AppliedStatus other) => other is ActionRestrictionStatus status &&
            Key == status.Key && Origin == status.Origin && RemainingAffectedSideTurnEnds == status.RemainingAffectedSideTurnEnds;

        public static bool Supports(StatusEffectKey key) => key is StatusEffectKey.Disarm or StatusEffectKey.Bind or StatusEffectKey.Stun;
    }

    public sealed class DefenseDecreaseStatus : AppliedStatus
    {
        public int ReductionPercent { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.BattleEnd;

        public DefenseDecreaseStatus(EffectOriginKey origin, int reductionPercent) : base(StatusEffectKey.DefenseDecrease, origin)
        {
            if (reductionPercent < 0 || reductionPercent > 100) throw new ArgumentOutOfRangeException(nameof(reductionPercent));
            ReductionPercent = reductionPercent;
        }

        public override bool SameValues(AppliedStatus other) => other is DefenseDecreaseStatus status &&
            Origin == status.Origin && ReductionPercent == status.ReductionPercent;
    }

    public sealed class ShieldStatus : AppliedStatus
    {
        public int RemainingBudget { get; }
        public bool Eligible { get; }
        public long LastResetControllerTurnCount { get; }

        public Owner? ExpiresAtSideTurnStart { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.ReconciledBudget;

        public ShieldStatus(EffectOriginKey origin, int remainingBudget, bool eligible, long lastResetControllerTurnCount)
            : this(origin, remainingBudget, eligible, lastResetControllerTurnCount, null) { }

        public ShieldStatus(EffectOriginKey origin, int remainingBudget, bool eligible, long lastResetControllerTurnCount,
            Owner? expiresAtSideTurnStart)
            : base(StatusEffectKey.Shield, origin)
        {
            if (remainingBudget < 0) throw new ArgumentOutOfRangeException(nameof(remainingBudget));
            if (lastResetControllerTurnCount < 0) throw new ArgumentOutOfRangeException(nameof(lastResetControllerTurnCount));
            if (expiresAtSideTurnStart.HasValue && expiresAtSideTurnStart != Owner.Player && expiresAtSideTurnStart != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(expiresAtSideTurnStart));
            ExpiresAtSideTurnStart = expiresAtSideTurnStart;
            RemainingBudget = remainingBudget;
            Eligible = eligible;
            LastResetControllerTurnCount = lastResetControllerTurnCount;
        }

        public override bool SameValues(AppliedStatus other) => other is ShieldStatus status && Origin == status.Origin &&
            RemainingBudget == status.RemainingBudget && Eligible == status.Eligible && LastResetControllerTurnCount == status.LastResetControllerTurnCount &&
            ExpiresAtSideTurnStart == status.ExpiresAtSideTurnStart;
    }

    public sealed class AttackIncreaseStatus : AppliedStatus
    {
        public int Amount { get; }
        public int RemainingControllerTurnEnds { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.ControllerTurnEnds;

        public AttackIncreaseStatus(EffectOriginKey origin, int amount, int remainingControllerTurnEnds)
            : base(StatusEffectKey.AttackIncrease, origin)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (remainingControllerTurnEnds < 1) throw new ArgumentOutOfRangeException(nameof(remainingControllerTurnEnds));
            Amount = amount;
            RemainingControllerTurnEnds = remainingControllerTurnEnds;
        }

        public override bool SameValues(AppliedStatus other) => other is AttackIncreaseStatus status &&
            Origin == status.Origin && Amount == status.Amount && RemainingControllerTurnEnds == status.RemainingControllerTurnEnds;
    }

    /// <summary>
    /// 도발 범위 안에 있는 AI 진영 유닛의 '도발당함'입니다. 값이 없고 도발 보유자(출처)마다 하나입니다.
    /// 부여가 아니라 보유 조정이 조건(도발 규칙)이 맞는 동안 게시합니다. 적 AI의 대상 고르기는 이 상태가 아니라 같은 규칙 함수를 읽습니다.
    /// </summary>
    public sealed class TauntedStatus : AppliedStatus
    {
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.CurrentEligibility;

        public TauntedStatus(EffectOriginKey origin)
            : base(StatusEffectKey.Taunted, origin)
        {
            if (origin.Kind != EffectOwnerKind.Trait || origin.DefinitionKey != (int)TraitKey.Taunt)
                throw new ArgumentException("도발당함의 출처는 도발 특성이어야 합니다.", nameof(origin));
        }

        public override bool SameValues(AppliedStatus other) => other is TauntedStatus && Origin == other.Origin;
    }
}
