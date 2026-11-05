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

    public sealed class ActionRestrictionEffectSnapshot : StatusEffectSnapshot
    {
        public ActionRestrictionEffectSnapshot(StatusEffectKey key, IReadOnlyList<StatusEffectSourceSnapshot> sources) : base(key, sources)
        {
            if (key != StatusEffectKey.Disarm && key != StatusEffectKey.Bind) throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}
