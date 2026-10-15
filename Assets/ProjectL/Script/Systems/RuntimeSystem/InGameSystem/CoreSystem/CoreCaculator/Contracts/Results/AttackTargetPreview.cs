using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AttackTargetPreview
    {
        public EntityId TargetId { get; }
        /// <summary>새 지정 방식의 대상 역할이다. None은 아직 이전되지 않은 기존 계산 결과를 뜻한다.</summary>
        public AttackTargetRole Role { get; }
        /// <summary>이미 적용된 스플래시 단계 비율이다. 표시 계층에서 피해에 다시 곱하지 않는다.</summary>
        public int AppliedSplashPercent { get; }
        public int ExpectedDamage { get; } // 공격·통과 피해 예상 합계이며 위치 보정 정규화와 구분한다.
        /// <summary>직접 타격 상세에 포함되지 않는 폭발·통과 등의 후속 피해 합계다.</summary>
        public int SecondaryDamage { get; }
        /// <summary>위치 보정 제거로 감소할 HP이며 예상 피해량에는 포함하지 않는다.</summary>
        public int HealthNormalizationLoss { get; }
        public int TargetHealthBefore { get; } // 공격 전 대상의 체력
        public int TargetHealthAfter { get; } // 공격 후 대상의 체력
        public bool TargetKilled { get; } // 대상이 사망했는지 여부
        public ReadOnlyMemory<CellCoord> TargetOccupiedCells { get; } // 공격 대상이 현재 실제로 차지하고 있는 모든 맵 셀
        /// <summary>Sum of pre-matchup damage when multiple rays hit this entity.</summary>
        public int DamageBeforeMatchup { get; }
        /// <summary>적용된 상성 배율의 분자입니다.</summary>
        public int MatchupNumerator { get; }
        /// <summary>적용된 상성 배율의 분모입니다.</summary>
        public int MatchupDenominator { get; }
        /// <summary>Sum of the reductions actually applied to individual hits.</summary>
        public int IncomingReduction { get; }

        /// <summary>
        /// Multi-hit detail only. Scalar damage/reduction fields are sums of processed hits;
        /// the common matchup ratio must not be reapplied to summed damage.
        /// </summary>
        public ReadOnlyMemory<PierceRayHitPreview> RayHits { get; }

        public AttackTargetPreview(
            EntityId targetId,
            int expectedDamage,
            int targetHealthBefore,
            CellCoord[] targetOccupiedCells)
            : this(targetId, expectedDamage, targetHealthBefore, targetOccupiedCells,
                damageBeforeMatchup: expectedDamage,
                matchupNumerator: 1,
                matchupDenominator: 1,
                incomingReduction: 0)
        {
        }

        public AttackTargetPreview(
            EntityId targetId,
            int expectedDamage,
            int targetHealthBefore,
            CellCoord[] targetOccupiedCells,
            int damageBeforeMatchup,
            int matchupNumerator,
            int matchupDenominator,
            int incomingReduction)
            : this(targetId, expectedDamage, targetHealthBefore, targetOccupiedCells,
                damageBeforeMatchup, matchupNumerator, matchupDenominator, incomingReduction,
                Array.Empty<PierceRayHitPreview>())
        {
        }

        public AttackTargetPreview(
            EntityId targetId, int expectedDamage, int targetHealthBefore, CellCoord[] targetOccupiedCells,
            int damageBeforeMatchup, int matchupNumerator, int matchupDenominator, int incomingReduction,
            PierceRayHitPreview[] rayHits,
            AttackTargetRole role = AttackTargetRole.None, int appliedSplashPercent = 100, int healthNormalizationLoss = 0,
            int secondaryDamage = 0)
        {
            if (Enum.IsDefined(typeof(AttackTargetRole), role) == false)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (appliedSplashPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(appliedSplashPercent));
            if (role != AttackTargetRole.Additional && appliedSplashPercent != 100)
                throw new ArgumentException("추가 대상 이외의 적용 비율은 100이어야 합니다.", nameof(appliedSplashPercent));
            Role = role;
            AppliedSplashPercent = appliedSplashPercent;
            if (damageBeforeMatchup < 0)
                throw new ArgumentOutOfRangeException(nameof(damageBeforeMatchup));
            if (matchupNumerator < 0)
                throw new ArgumentOutOfRangeException(nameof(matchupNumerator));
            if (matchupDenominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(matchupDenominator));
            if (incomingReduction < 0)
                throw new ArgumentOutOfRangeException(nameof(incomingReduction));

            DamageBeforeMatchup = damageBeforeMatchup;
            MatchupNumerator = matchupNumerator;
            MatchupDenominator = matchupDenominator;
            IncomingReduction = incomingReduction;

            if (targetId.IsEmpty)
                throw new ArgumentException("대상의 아이디는 Empty일 수 없습니다.", nameof(targetId));
            if (expectedDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedDamage), "예상 피해량은 0 이상이어야 합니다.");
            if (secondaryDamage < 0 || secondaryDamage > expectedDamage)
                throw new ArgumentOutOfRangeException(nameof(secondaryDamage));
            if (targetHealthBefore < 0)
                throw new ArgumentOutOfRangeException(nameof(targetHealthBefore), "공격 전 대상의 체력은 0 이상이어야 합니다.");
            if (targetOccupiedCells == null)
                throw new ArgumentNullException(nameof(targetOccupiedCells), "대상이 점유한 셀 좌표 배열은 null일 수 없습니다.");

            TargetId = targetId;
            ExpectedDamage = expectedDamage;
            SecondaryDamage = secondaryDamage;
            if (healthNormalizationLoss < 0 || healthNormalizationLoss > targetHealthBefore)
                throw new ArgumentOutOfRangeException(nameof(healthNormalizationLoss));
            HealthNormalizationLoss = healthNormalizationLoss;
            TargetHealthBefore = targetHealthBefore;
            TargetHealthAfter = (int)Math.Max(0L, (long)TargetHealthBefore - ExpectedDamage - HealthNormalizationLoss);
            TargetKilled = TargetHealthAfter == 0;

            CellCoord[] copiedCells = new CellCoord[targetOccupiedCells.Length];
            Array.Copy(targetOccupiedCells, copiedCells, targetOccupiedCells.Length);

            TargetOccupiedCells = copiedCells;

            if (rayHits == null) throw new ArgumentNullException(nameof(rayHits));
            var copiedHits = (PierceRayHitPreview[])rayHits.Clone();
            if (copiedHits.Length == 1)
                throw new ArgumentException("Ray detail requires at least two hits.", nameof(rayHits));
            int seen = 0, before = 0, reduction = 0, final = 0, matchup = 0, defense = 0;
            foreach (PierceRayHitPreview hit in copiedHits)
            {
                if (hit.RayIndex < 0 || hit.RayIndex > 2 || hit.EncounterIndex < 0 ||
                    hit.MatchupDenominator <= 0 || (seen & (1 << hit.RayIndex)) != 0 ||
                    hit.MatchupNumerator != matchupNumerator || hit.MatchupDenominator != matchupDenominator)
                    throw new ArgumentException("Hits require unique rays and the aggregate matchup ratio.", nameof(rayHits));
                seen |= 1 << hit.RayIndex;
                before = checked(before + hit.DamageBeforeMatchup);
                reduction = checked(reduction + hit.IncomingReduction);
                final = checked(final + hit.FinalDamage);
                matchup = checked(matchup + hit.DamageAfterMatchup);
                defense = checked(defense + hit.DamageAfterPhysicalDefense);
            }
            if (copiedHits.Length > 0 &&
                (before != damageBeforeMatchup || reduction != incomingReduction || (long)final + secondaryDamage != expectedDamage))
                throw new ArgumentException("Ray hit sums must match aggregate values.", nameof(rayHits));
            RayHits = copiedHits;
        }
    }
}
