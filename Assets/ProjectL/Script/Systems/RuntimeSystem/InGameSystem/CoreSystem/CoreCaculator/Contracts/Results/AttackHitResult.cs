using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AttackHitResult
    {
        public EntityId TargetId { get; }
        /// <summary>일반 공격의 대상 역할이다. 기존 호출의 None은 역할을 추정하지 않은 값이다.</summary>
        public AttackTargetRole Role { get; }
        /// <summary>정산 전에 적용된 비율이며 실제 HP 손실에 다시 적용하지 않는다.</summary>
        public int AppliedSplashPercent { get; }
        public int DamageDealt { get; }
        /// <summary>위치 보정 제거에 따른 HP 상한 감소이며 공격 피해에는 포함하지 않는다.</summary>
        public int HealthNormalizationLoss { get; }
        public int TargetHealthBefore { get; }
        public int TargetHealthAfter { get; }
        public bool TargetKilled { get; }

        public AttackHitResult(EntityId targetId, int targetHealthBefore, int targetHealthAfter,
            AttackTargetRole role = AttackTargetRole.None, int appliedSplashPercent = 100, int healthNormalizationLoss = 0)
        {
            if (Enum.IsDefined(typeof(AttackTargetRole), role) == false)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (appliedSplashPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(appliedSplashPercent));
            if (role != AttackTargetRole.Additional && appliedSplashPercent != 100)
                throw new ArgumentException("추가 대상 이외의 적용 비율은 100이어야 합니다.", nameof(appliedSplashPercent));
            Role = role;
            AppliedSplashPercent = appliedSplashPercent;
            if (targetId.IsEmpty)
                throw new ArgumentException("대상 ID는 비어 있을 수 없습니다.", nameof(targetId));
            if (targetHealthBefore < 0)
                throw new ArgumentOutOfRangeException(nameof(targetHealthBefore), "대상 체력(이전)은 음수일 수 없습니다.");
            if (targetHealthAfter < 0)
                throw new ArgumentOutOfRangeException(nameof(targetHealthAfter), "대상 체력(이후)은 음수일 수 없습니다.");
            if (targetHealthAfter > targetHealthBefore)
            {
                throw new ArgumentOutOfRangeException(nameof(targetHealthAfter), "공격 후 체력은 공격 전 체력보다 클 수 없습니다.");
            }

            TargetId = targetId;
            if (healthNormalizationLoss < 0 || healthNormalizationLoss > targetHealthBefore - targetHealthAfter)
                throw new ArgumentOutOfRangeException(nameof(healthNormalizationLoss));
            HealthNormalizationLoss = healthNormalizationLoss;
            DamageDealt = targetHealthBefore - targetHealthAfter - healthNormalizationLoss;
            TargetHealthBefore = targetHealthBefore;
            TargetHealthAfter = targetHealthAfter;
            TargetKilled = TargetHealthAfter == 0;
        }
    }
}
