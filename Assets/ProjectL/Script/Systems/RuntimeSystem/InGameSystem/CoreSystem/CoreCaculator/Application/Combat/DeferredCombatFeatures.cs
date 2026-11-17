using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>새 진영 턴에서 아직 재설계하지 않은 기능의 고정 지원 범위다.</summary>
    public static class DeferredCombatFeatures
    {
        // 다시 켜기 전 결정 사항(core-review-remediation 설계 §5.1):
        // - 사령술(R1): 공용 적 진영에서의 소환 주기를 기획과 정한 뒤에 소환을 되살린다.
        public static bool IsTraitAvailable(TraitKey key)
        {
            if (!Enum.IsDefined(typeof(TraitKey), key) || key == TraitKey.None)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (IsTraitRetired(key) || IsImplementationPending(key)) return false;
            return key != TraitKey.Focus && key != TraitKey.Necromancy;
        }

        /// <summary>직렬화 번호만 보존하며 실행·저작에서는 허용하지 않는 특성이다.</summary>
        public static bool IsTraitRetired(TraitKey key)
        {
            if (!Enum.IsDefined(typeof(TraitKey), key) || key == TraitKey.None)
                throw new ArgumentOutOfRangeException(nameof(key));
            return key == TraitKey.ThornField || key == TraitKey.MoveAttackDiscount ||
                key == TraitKey.DoorBreaching || key == TraitKey.ContemptForWeak ||
                key == TraitKey.PrecisionShot || key == TraitKey.DoubleShot ||
                key == TraitKey.LuckyBullet || key == TraitKey.ChainExplosion;
        }

        internal static bool IsImplementationPending(TraitKey key) => false;

        public static string GetTraitUnavailableReason(TraitKey key) =>
            IsTraitAvailable(key) ? "" : IsTraitRetired(key) ? "Retired" :
                IsImplementationPending(key) ? "ImplementationPending" : "DeferredUnitTurnTiming";

        /// <summary>특성 구성 요소의 공용 사용 가능 사유 조회 계약입니다. 현재 부분 보류 항목은 없습니다.</summary>
        public static string GetTraitPartialUnavailableReason(TraitKey key)
        {
            IsTraitAvailable(key);
            return "";
        }

        /// <summary>전투 안팎에서 같은 사유를 쓰도록 특성 하나의 사용 가능 여부를 만든다.</summary>
        public static TraitAvailabilitySnapshot GetTraitAvailability(TraitKey key) =>
            new(key, GetTraitUnavailableReason(key), GetTraitPartialUnavailableReason(key));

        /// <summary>
        /// 깃발 획득(우선도 보너스)은 진영 턴에서 재설계 전까지 보류한다. 깃발 보류의 유일한 원본이다.
        /// false인 동안 전투 준비는 배치된 깃발 원본을 비활성으로 만든다(ID·진입 순번은 저작 값 그대로).
        /// </summary>
        public static bool IsFlagPickupAvailable => false;
    }
}
