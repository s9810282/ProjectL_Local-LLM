using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>불변 원본을 참조하는 출처별 조회값입니다. 서로 다른 시계를 한 만료 수치로 합치지 않습니다.</summary>
    public sealed class StatusEffectSourceSnapshot
    {
        public AppliedStatus Status { get; }
        public EffectOriginKey? Origin => Status.Origin;
        public StatusEffectLifetimeKind LifetimeKind => Status.Lifetime;
        public int? RemainingControllerTurnEnds => (Status as AttackIncreaseStatus)?.RemainingControllerTurnEnds;
        public int? RemainingAffectedSideTurnEnds => Status switch
        {
            ActionRestrictionStatus restriction => restriction.RemainingAffectedSideTurnEnds,
            WeaknessExposureStatus exposure => exposure.RemainingAffectedSideTurnEnds,
            _ => null
        };
        public long? LastResetControllerTurnCount => (Status as ShieldStatus)?.LastResetControllerTurnCount;
        public int? RemainingAffectedSideStarts => Status switch
        {
            FireStatus fire => fire.RemainingAffectedSideStarts,
            BleedingStatus bleeding => bleeding.RemainingAffectedSideStarts,
            ReconstructionStatus reconstruction => reconstruction.RemainingOwnTurnStarts,
            _ => null
        };
        public long? ExpiresAtCompletedControllerTurnCount => (Status as MovementRangeDecreaseStatus)?.ExpiresAtCompletedControllerTurnCount;
        public bool Eligible => Status is not ShieldStatus shield || shield.Eligible;

        public StatusEffectSourceSnapshot(AppliedStatus status)
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }
    }

    /// <summary>효과별 유효값과 적용 순서를 보존한 출처 세부값입니다. 상태를 변경하거나 시계를 진행하지 않습니다.</summary>
    public abstract class StatusEffectSnapshot
    {
        public StatusEffectKey Key { get; }
        public StatusEffectClassification Classification => StatusEffectCatalog.ClassificationOf(Key);
        public IReadOnlyList<StatusEffectSourceSnapshot> Sources { get; }

        protected StatusEffectSnapshot(StatusEffectKey key, IReadOnlyList<StatusEffectSourceSnapshot> sources)
        {
            _ = StatusEffectCatalog.ClassificationOf(key);
            var copy = (sources ?? throw new ArgumentNullException(nameof(sources))).ToArray();
            if (copy.Any(s => s == null || s.Status.Key != key)) throw new ArgumentException("효과와 출처 종류가 일치해야 합니다.", nameof(sources));
            Key = key;
            Sources = Array.AsReadOnly(copy);
        }
    }

    public sealed class AttackIncreaseEffectSnapshot : StatusEffectSnapshot
    {
        public int AppliedAmount { get; }
        public int HeldForAttackAmount { get; }
        public int TotalAmount => checked(AppliedAmount + HeldForAttackAmount);
        public StatusEffectLifetimeKind HeldLifetimeKind => StatusEffectLifetimeKind.AttackCompletion;

        public AttackIncreaseEffectSnapshot(int appliedAmount, int heldForAttackAmount, IReadOnlyList<StatusEffectSourceSnapshot> sources)
            : base(StatusEffectKey.AttackIncrease, sources)
        {
            if (appliedAmount < 0) throw new ArgumentOutOfRangeException(nameof(appliedAmount));
            if (heldForAttackAmount < 0) throw new ArgumentOutOfRangeException(nameof(heldForAttackAmount));
            AppliedAmount = appliedAmount;
            HeldForAttackAmount = heldForAttackAmount;
            _ = TotalAmount;
        }
    }

    public sealed class AttackDecreaseEffectSnapshot : StatusEffectSnapshot
    {
        public int ReductionPercent { get; }
        public AttackDecreaseEffectSnapshot(int reductionPercent, IReadOnlyList<StatusEffectSourceSnapshot> sources)
            : base(StatusEffectKey.AttackDecrease, sources)
        {
            if (reductionPercent < 1 || reductionPercent > 100) throw new ArgumentOutOfRangeException(nameof(reductionPercent));
            ReductionPercent = reductionPercent;
        }
    }

    public sealed class DefenseIncreaseEffectSnapshot : StatusEffectSnapshot
    {
        public int Amount { get; }
        public StatusEffectLifetimeKind LifetimeKind => StatusEffectLifetimeKind.CurrentEligibility;
        public DefenseIncreaseEffectSnapshot(int amount) : base(StatusEffectKey.DefenseIncrease, Array.Empty<StatusEffectSourceSnapshot>())
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
        }
    }

    public sealed class DefenseDecreaseEffectSnapshot : StatusEffectSnapshot
    {
        public int EffectiveDefense { get; }
        public DefenseDecreaseEffectSnapshot(int effectiveDefense, IReadOnlyList<StatusEffectSourceSnapshot> sources)
            : base(StatusEffectKey.DefenseDecrease, sources)
        {
            if (effectiveDefense < 0) throw new ArgumentOutOfRangeException(nameof(effectiveDefense));
            EffectiveDefense = effectiveDefense;
        }
    }

    public sealed class ShieldEffectSnapshot : StatusEffectSnapshot
    {
        public int RemainingEligibleBudget { get; }
        public ShieldEffectSnapshot(int remainingEligibleBudget, IReadOnlyList<StatusEffectSourceSnapshot> sources)
            : base(StatusEffectKey.Shield, sources)
        {
            if (remainingEligibleBudget < 0) throw new ArgumentOutOfRangeException(nameof(remainingEligibleBudget));
            RemainingEligibleBudget = remainingEligibleBudget;
        }
    }

    /// <summary>도발당함 조회값입니다. 값은 없고, 출처마다 도발한 유닛 하나입니다.</summary>
    public sealed class TauntedEffectSnapshot : StatusEffectSnapshot
    {
        public TauntedEffectSnapshot(IReadOnlyList<StatusEffectSourceSnapshot> sources)
            : base(StatusEffectKey.Taunted, sources)
        {
        }
    }

    public sealed class WeaknessExposureEffectSnapshot : StatusEffectSnapshot
    {
        public int EffectiveIncreasePercent { get; }
        public WeaknessExposureEffectSnapshot(int effectiveIncreasePercent, IReadOnlyList<StatusEffectSourceSnapshot> sources)
            : base(StatusEffectKey.WeaknessExposure, sources)
        {
            if (effectiveIncreasePercent < 0) throw new ArgumentOutOfRangeException(nameof(effectiveIncreasePercent));
            EffectiveIncreasePercent = effectiveIncreasePercent;
        }
    }

    public sealed class RampageEffectSnapshot : StatusEffectSnapshot
    {
        public int AttackBonus { get; }
        public int MovementBonus { get; }
        public int KillAttackBonus { get; }
        /// <summary>현재 조건의 표시 수명입니다. 처치 누적 원본은 특성 상태이며 되돌리기 재계산 대상이 아닙니다.</summary>
        public StatusEffectLifetimeKind ConditionLifetimeKind => StatusEffectLifetimeKind.CurrentEligibility;

        public RampageEffectSnapshot(int attackBonus, int movementBonus, int killAttackBonus)
            : base(StatusEffectKey.Rampage, Array.Empty<StatusEffectSourceSnapshot>())
        {
            if (attackBonus < 0) throw new ArgumentOutOfRangeException(nameof(attackBonus));
            if (movementBonus < 0) throw new ArgumentOutOfRangeException(nameof(movementBonus));
            if (killAttackBonus < 0 || killAttackBonus > attackBonus) throw new ArgumentOutOfRangeException(nameof(killAttackBonus));
            AttackBonus = attackBonus;
            MovementBonus = movementBonus;
            KillAttackBonus = killAttackBonus;
        }
    }

    public sealed class FireEffectSnapshot : StatusEffectSnapshot
    {
        public int Damage => ((FireStatus)Sources[0].Status).Damage;
        public int RemainingAffectedSideStarts => ((FireStatus)Sources[0].Status).RemainingAffectedSideStarts;
        public FireEffectSnapshot(FireStatus status) : base(StatusEffectKey.Fire, new[] { new StatusEffectSourceSnapshot(status) }) { }
    }

    public sealed class ReconstructionEffectSnapshot : StatusEffectSnapshot
    {
        public int RemainingOwnTurnStarts => ((ReconstructionStatus)Sources[0].Status).RemainingOwnTurnStarts;
        public ReconstructionEffectSnapshot(ReconstructionStatus status)
            : base(StatusEffectKey.Reconstructing, new[] { new StatusEffectSourceSnapshot(status) }) { }
    }

    public sealed class BleedingEffectSnapshot : StatusEffectSnapshot
    {
        public IReadOnlyList<BleedingStack> Stacks => ((BleedingStatus)Sources[0].Status).Stacks;
        public int RemainingAffectedSideStarts => ((BleedingStatus)Sources[0].Status).RemainingAffectedSideStarts;
        public BleedingEffectSnapshot(BleedingStatus status) : base(StatusEffectKey.Bleeding, new[] { new StatusEffectSourceSnapshot(status) }) { }
    }

    public sealed class MovementRangeDecreaseEffectSnapshot : StatusEffectSnapshot
    {
        public int Magnitude => ((MovementRangeDecreaseStatus)Sources[0].Status).Magnitude;
        public long ExpiresAtCompletedControllerTurnCount => ((MovementRangeDecreaseStatus)Sources[0].Status).ExpiresAtCompletedControllerTurnCount;
        public MovementRangeDecreaseEffectSnapshot(MovementRangeDecreaseStatus status)
            : base(StatusEffectKey.MovementRangeDecrease, new[] { new StatusEffectSourceSnapshot(status) }) { }
    }

    public sealed class ActionRestrictionEffectSnapshot : StatusEffectSnapshot
    {
        public ActionRestrictionEffectSnapshot(StatusEffectKey key, IReadOnlyList<StatusEffectSourceSnapshot> sources) : base(key, sources)
        {
            if (key != StatusEffectKey.Disarm && key != StatusEffectKey.Bind) throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}
