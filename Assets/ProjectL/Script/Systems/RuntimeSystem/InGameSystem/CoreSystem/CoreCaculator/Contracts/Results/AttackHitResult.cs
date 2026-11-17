using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AttackHitResult
    {
        public IReadOnlyList<AttackHitContactSnapshot> Contacts { get; }
        public EntityId TargetId { get; }
        /// <summary>일반 공격의 대상 역할이다. 기존 호출의 None은 역할을 추정하지 않은 값이다.</summary>
        public AttackTargetRole Role { get; }
        /// <summary>정산 전에 적용된 비율이며 실제 HP 손실에 다시 적용하지 않는다.</summary>
        public int AppliedSplashPercent { get; }
        public int DamageDealt { get; }
        /// <summary>장애물 피격의 전후 체력 값은 생존 여부만 나타낸다.</summary>
        public bool IsObstacle { get; }
        /// <summary>피격 정산 도중 피해 적용 경계(아이템)가 적용한 회복이며 실제 피해와 분리한다. 공격 결과 고정 시점까지의 값이다.</summary>
        public int HealthRecovery { get; }
        /// <summary>위치 보정 제거에 따른 HP 상한 감소이며 공격 피해에는 포함하지 않는다.</summary>
        public int HealthNormalizationLoss { get; }
        public int TargetHealthBefore { get; }
        public int TargetHealthAfter { get; }
        public bool TargetKilled { get; }
        /// <summary>
        /// 이 공격으로 대상의 재구성(적 전용 특성)이 일어났는지입니다. 그러면 치명 피해를 받았지만 살아 있고 <see cref="TargetHealthAfter"/>는 최대 체력입니다.
        /// 판정은 재구성 규칙(<c>ReconstructionTraitRule.Triggered</c>)이고, 공격 미리보기가 이 값을 대상 미리보기로 옮깁니다(2026-11-11 View 재구성 연출).
        /// </summary>
        public bool TargetReconstructed { get; }

        public AttackHitResult(EntityId targetId, int targetHealthBefore, int targetHealthAfter,
            AttackTargetRole role, int appliedSplashPercent, int healthNormalizationLoss, int healthRecovery,
            IReadOnlyList<AttackHitContactSnapshot> contacts, bool targetReconstructed = false, bool isObstacle = false)
            : this(targetId, targetHealthBefore, targetHealthAfter, role, appliedSplashPercent, healthNormalizationLoss, healthRecovery,
                targetReconstructed, isObstacle)
        {
            Contacts = AttackHitContactSnapshot.Copy(contacts);
            var pairs = Contacts.Select(c => (c.Role, c.AppliedSplashPercent)).Distinct().ToArray();
            if (pairs.Length == 1 && (role != pairs[0].Role || appliedSplashPercent != pairs[0].AppliedSplashPercent) ||
                pairs.Length > 1 && (role != AttackTargetRole.None || appliedSplashPercent != 100))
                throw new ArgumentException("집계 역할과 접촉 역할이 다릅니다.", nameof(contacts));
        }

        public AttackHitResult(EntityId targetId, int targetHealthBefore, int targetHealthAfter,
            AttackTargetRole role = AttackTargetRole.None, int appliedSplashPercent = 100, int healthNormalizationLoss = 0,
            int healthRecovery = 0, bool targetReconstructed = false, bool isObstacle = false)
        {
            if (Enum.IsDefined(typeof(AttackTargetRole), role) == false)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (appliedSplashPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(appliedSplashPercent));
            if (role != AttackTargetRole.Additional && appliedSplashPercent != 100)
                throw new ArgumentException("추가 대상 이외의 적용 비율은 100이어야 합니다.", nameof(appliedSplashPercent));
            Contacts = null;
            Role = role;
            AppliedSplashPercent = appliedSplashPercent;
            if (targetId.IsEmpty)
                throw new ArgumentException("대상 ID는 비어 있을 수 없습니다.", nameof(targetId));
            if (targetHealthBefore < 0)
                throw new ArgumentOutOfRangeException(nameof(targetHealthBefore), "대상 체력(이전)은 음수일 수 없습니다.");
            if (targetHealthAfter < 0)
                throw new ArgumentOutOfRangeException(nameof(targetHealthAfter), "대상 체력(이후)은 음수일 수 없습니다.");
            if (healthRecovery < 0) throw new ArgumentOutOfRangeException(nameof(healthRecovery));
            if ((long)targetHealthAfter > (long)targetHealthBefore + healthRecovery)
            {
                throw new ArgumentOutOfRangeException(nameof(targetHealthAfter), "공격 후 체력은 공격 전 체력보다 클 수 없습니다.");
            }

            TargetId = targetId;
            if (healthNormalizationLoss < 0 || healthNormalizationLoss > (long)targetHealthBefore - targetHealthAfter + healthRecovery)
                throw new ArgumentOutOfRangeException(nameof(healthNormalizationLoss));
            HealthNormalizationLoss = healthNormalizationLoss;
            HealthRecovery = healthRecovery;
            if (isObstacle && (targetHealthBefore > 1 || targetHealthAfter > 1 || healthNormalizationLoss != 0 ||
                healthRecovery != 0 || targetReconstructed))
                throw new ArgumentException("장애물 공격 결과에는 생존 여부만 기록합니다.", nameof(isObstacle));
            IsObstacle = isObstacle;
            DamageDealt = isObstacle ? 0 : checked(targetHealthBefore - targetHealthAfter - healthNormalizationLoss + healthRecovery);
            TargetHealthBefore = targetHealthBefore;
            TargetHealthAfter = targetHealthAfter;
            TargetKilled = TargetHealthAfter == 0;
            TargetReconstructed = targetReconstructed;
        }
    }
}
