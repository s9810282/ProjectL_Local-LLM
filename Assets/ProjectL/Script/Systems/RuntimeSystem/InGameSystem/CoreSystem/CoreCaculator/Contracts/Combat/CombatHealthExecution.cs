using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatHealthOperationKind
    {
        Damage = 1,
        EffectRecovery,
        Environment,
        Lifecycle,
        Growth,
        Normalization
    }

    public readonly struct CombatHealthSource
    {
        public CombatHealthOperationKind Kind { get; }
        public EffectOriginKey? EffectOrigin { get; }
        public IReadOnlyList<CombatDamageOrigin> DamageOrigins { get; }
        public CombatEvent ContextEvent { get; }
        public MovementTileFact EnvironmentSource { get; }
        public CombatDamageCause? DamageCause { get; }
        internal CombatDamageKind? DamageKind { get; }

        internal CombatHealthSource(CombatHealthOperationKind kind, EffectOriginKey? effectOrigin = null,
            IReadOnlyList<CombatDamageOrigin> damageOrigins = null, CombatEvent contextEvent = null,
            CombatDamageCause? damageCause = null, MovementTileFact environmentSource = null, CombatDamageKind? damageKind = null)
        {
            if (!Enum.IsDefined(typeof(CombatHealthOperationKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (effectOrigin.HasValue && !effectOrigin.Value.IsValid)
                throw new ArgumentException("체력 변경의 효과 출처가 유효해야 합니다.", nameof(effectOrigin));
            if (damageCause.HasValue && !Enum.IsDefined(typeof(CombatDamageCause), damageCause.Value))
                throw new ArgumentOutOfRangeException(nameof(damageCause));
            var origins = (damageOrigins ?? Array.Empty<CombatDamageOrigin>()).ToArray();
            if (origins.Any(origin => origin == null))
                throw new ArgumentException("피해 출처에는 null을 넣을 수 없습니다.", nameof(damageOrigins));
            switch (kind)
            {
                case CombatHealthOperationKind.Damage when !damageCause.HasValue || origins.Length == 0:
                    throw new ArgumentException("피해에는 실제 출처와 피해 원인이 필요합니다.");
                case CombatHealthOperationKind.EffectRecovery when !effectOrigin.HasValue:
                    throw new ArgumentException("효과 회복에는 효과 출처가 필요합니다.", nameof(effectOrigin));
                case CombatHealthOperationKind.Environment:
                case CombatHealthOperationKind.Lifecycle:
                case CombatHealthOperationKind.Growth:
                case CombatHealthOperationKind.Normalization:
                    if (contextEvent == null && !effectOrigin.HasValue &&
                        (kind != CombatHealthOperationKind.Environment || environmentSource == null))
                        throw new ArgumentException("체력 변경에는 담당 효과 또는 실행 문맥 사건이 필요합니다.");
                    break;
            }
            if ((kind is CombatHealthOperationKind.EffectRecovery or CombatHealthOperationKind.Growth or CombatHealthOperationKind.Normalization) &&
                (damageCause.HasValue || origins.Length != 0))
                throw new ArgumentException("회복·성장·상한 보정에는 피해 원인을 넣을 수 없습니다.");
            if (origins.Length != 0 && !damageCause.HasValue)
                throw new ArgumentException("피해 출처에는 피해 원인이 필요합니다.", nameof(damageCause));

            Kind = kind;
            EffectOrigin = effectOrigin;
            DamageOrigins = Array.AsReadOnly(origins);
            ContextEvent = contextEvent;
            EnvironmentSource = environmentSource;
            DamageCause = damageCause;
            if (damageKind.HasValue && (!Enum.IsDefined(typeof(CombatDamageKind), damageKind.Value) || kind != CombatHealthOperationKind.Damage))
                throw new ArgumentException("피해 종류는 실제 피해 실행에만 지정합니다.", nameof(damageKind));
            DamageKind = damageKind;
        }
    }

    public sealed class CombatHealthStep
    {
        public EntityId RecipientId { get; }
        public HealthStateSnapshot Before { get; }
        public HealthStateSnapshot After { get; }
        public int ActualLoss { get; }
        public int ActualRecovery { get; }
        public int NormalizationLoss { get; }
        public CombatHealthSource Source { get; }
        public CombatEvent EvidenceEvent { get; }
        public int EvidenceOrdinal { get; }

        internal CombatHealthStep(EntityId recipientId, HealthStateSnapshot before, HealthStateSnapshot after,
            int actualLoss, int actualRecovery, int normalizationLoss, CombatHealthSource source,
            CombatEvent evidenceEvent, int evidenceOrdinal)
        {
            if (recipientId.IsEmpty) throw new ArgumentException("체력 변경의 수신자가 필요합니다.", nameof(recipientId));
            // 스냅샷 생성자가 범위를 검사하고, 여기서는 구조체 기본값도 거부합니다.
            if (before.MaxHealth < 1) throw new ArgumentException("변경 전 체력 스냅샷이 유효해야 합니다.", nameof(before));
            if (after.MaxHealth < 1) throw new ArgumentException("변경 후 체력 스냅샷이 유효해야 합니다.", nameof(after));
            if (actualLoss < 0) throw new ArgumentOutOfRangeException(nameof(actualLoss));
            if (actualRecovery < 0) throw new ArgumentOutOfRangeException(nameof(actualRecovery));
            if (normalizationLoss < 0) throw new ArgumentOutOfRangeException(nameof(normalizationLoss));
            long remaining = (long)before.CurrentHealth - actualLoss - normalizationLoss;
            if (remaining < 0 || remaining + actualRecovery != after.CurrentHealth)
                throw new ArgumentException($"수신자 {recipientId}의 체력 변화량이 스냅샷과 일치하지 않습니다.");
            if (source.DamageOrigins == null)
                throw new ArgumentException("체력 변경의 출처가 필요합니다.", nameof(source));
            bool amountsMatchSource = source.Kind switch
            {
                CombatHealthOperationKind.Damage => actualRecovery == 0 && normalizationLoss == 0,
                CombatHealthOperationKind.EffectRecovery => actualLoss == 0 && normalizationLoss == 0,
                CombatHealthOperationKind.Normalization => actualLoss == 0 && actualRecovery == 0,
                _ => true
            };
            if (!amountsMatchSource)
                throw new ArgumentException($"수신자 {recipientId}의 체력 변화량이 출처 종류 {source.Kind}와 일치하지 않습니다.", nameof(source));
            EvidenceEvent = evidenceEvent ?? throw new ArgumentNullException(nameof(evidenceEvent));
            if (evidenceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(evidenceOrdinal));

            RecipientId = recipientId;
            Before = before;
            After = after;
            ActualLoss = actualLoss;
            ActualRecovery = actualRecovery;
            NormalizationLoss = normalizationLoss;
            Source = source;
            EvidenceOrdinal = evidenceOrdinal;
        }
    }

    /// <summary>한 실행에서 확정된 체력 단계와 기존 사건·이동 사용 근거입니다.</summary>
    public sealed class CombatHealthExecution
    {
        public IReadOnlyList<CombatHealthStep> Steps { get; }
        public IReadOnlyList<CombatEvent> Evidence { get; }
        public IReadOnlyList<MovementEffectUse> MovementUses { get; }

        internal CombatHealthExecution(IReadOnlyList<CombatHealthStep> steps, IReadOnlyList<CombatEvent> evidence,
            IReadOnlyList<MovementEffectUse> movementUses = null)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            var events = evidence.ToArray();
            var uses = (movementUses ?? Array.Empty<MovementEffectUse>()).ToArray();
            if (events.Length == 0 || events.Any(e => e == null) || uses.Any(use => use == null || !events.Contains(use.Execution)))
                throw new ArgumentException("체력 실행은 자신의 사건과 이동 사용 근거를 함께 가져야 합니다.", nameof(evidence));
            Evidence = Array.AsReadOnly(events);
            MovementUses = Array.AsReadOnly(uses);
            var stepCopy = steps.ToArray();
            if (stepCopy.Any(step => step == null || !Evidence.Any(e => ReferenceEquals(e, step.EvidenceEvent))) ||
                stepCopy.Distinct().Count() != stepCopy.Length)
                throw new ArgumentException("체력 단계는 자신의 근거 사건에 연결되며 중복될 수 없습니다.", nameof(steps));

            Steps = Array.AsReadOnly(stepCopy);
        }

        internal static IReadOnlyList<MovementEffectUse> CollectMovementUses(IReadOnlyList<CombatHealthExecution> executions,
            IReadOnlyList<MovementEffectUse> additionalUses = null)
        {
            var result = new List<MovementEffectUse>();
            foreach (var use in executions.SelectMany(execution => execution.MovementUses).Concat(additionalUses ?? Array.Empty<MovementEffectUse>()))
                if (!result.Any(use.SameValues)) result.Add(use);
            return result.AsReadOnly();
        }
    }
}
