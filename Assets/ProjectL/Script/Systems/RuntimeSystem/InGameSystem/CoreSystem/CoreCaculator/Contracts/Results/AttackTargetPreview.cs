using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AttackTargetPreview
    {
        public IReadOnlyList<AttackHitContactSnapshot> Contacts { get; }
        private AttackTargetPreview(AttackTargetPreview source, IReadOnlyList<AttackHitContactSnapshot> contacts)
        { this = source; Contacts = AttackHitContactSnapshot.Copy(contacts); }
        public AttackTargetPreview WithContacts(IReadOnlyList<AttackHitContactSnapshot> contacts) => new AttackTargetPreview(this, contacts);
        public EntityId TargetId { get; }
        /// <summary>새 지정 방식의 대상 역할이다. None은 아직 이전되지 않은 기존 계산 결과를 뜻한다.</summary>
        public AttackTargetRole Role { get; }
        /// <summary>이미 적용된 스플래시 단계 비율이다. 표시 계층에서 피해에 다시 곱하지 않는다.</summary>
        public int AppliedSplashPercent { get; }
        public int ExpectedDamage { get; } // 공격·통과 피해 예상 합계이며 위치 보정 정규화와 구분한다.
        /// <summary>직접 타격 상세에 포함되지 않는 공격 전후의 환경·폭발·통과 피해 합계다.</summary>
        public int SecondaryDamage { get; }
        /// <summary>직접 타격 여부입니다. 이동만 하는 아군과 이동 중 환경 피해는 false입니다.</summary>
        public bool IsDirectHit { get; }
        /// <summary>위치 보정 제거로 감소할 HP이며 예상 피해량에는 포함하지 않는다.</summary>
        public int HealthNormalizationLoss { get; }
        public int TargetHealthBefore { get; } // 공격 전 대상의 체력
        public int TargetHealthAfter { get; } // 공격 후 대상의 체력
        public bool TargetKilled { get; } // 대상이 사망했는지 여부
        /// <summary>
        /// 이 공격으로 대상의 재구성(적 전용 특성)이 일어나는지입니다. 그러면 치명 피해를 받지만 살아 있고 예상 체력은 최대입니다.
        /// 피격 결과(<see cref="AttackHitResult.TargetReconstructed"/>)에서 만든 미리보기는 그 값을 옮기고, 넉백·작렬 부딪힘 보조 행은 큰 생성자 인자로 받습니다
        /// (공격 장부의 같은 판정, 2026-11-11 View 재구성 연출·묶음 끝 검토 M2). 넘기지 않으면 거짓입니다.
        /// </summary>
        public bool TargetReconstructs { get; }
        public ReadOnlyMemory<CellCoord> TargetOccupiedCells { get; } // 공격 대상이 현재 실제로 차지하고 있는 모든 맵 셀
        /// <summary>상성 적용 전 피해입니다.</summary>
        public int DamageBeforeMatchup { get; }
        /// <summary>적용된 상성 배율의 분자입니다.</summary>
        public int MatchupNumerator { get; }
        /// <summary>적용된 상성 배율의 분모입니다.</summary>
        public int MatchupDenominator { get; }
        /// <summary>실제로 적용된 피해 감소량입니다.</summary>
        public int IncomingReduction { get; }

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

        /// <summary>체력과 사망 여부는 확정 피격 결과를 사용한다. 추락 같은 비피해 사망을 예상 피해량에 더하지 않는다.</summary>
        public AttackTargetPreview(
            AttackHitResult hit, int expectedDamage, CellCoord[] targetOccupiedCells,
            int damageBeforeMatchup, int matchupNumerator, int matchupDenominator, int incomingReduction,
            int secondaryDamage = 0)
            : this(hit.TargetId, expectedDamage, hit.TargetHealthBefore, targetOccupiedCells,
                damageBeforeMatchup, matchupNumerator, matchupDenominator, incomingReduction,
                hit.Role, hit.AppliedSplashPercent, hit.HealthNormalizationLoss, secondaryDamage,
                targetHealthAfter: hit.TargetHealthAfter, healthRecovery: hit.HealthRecovery)
        {
            Contacts = hit.Contacts;
            IsDirectHit = hit.Contacts == null || hit.Contacts.Count > 0;
            TargetHealthAfter = hit.TargetHealthAfter;
            TargetKilled = hit.TargetKilled;
            TargetReconstructs = hit.TargetReconstructed;
        }

        public AttackTargetPreview(
            EntityId targetId, int expectedDamage, int targetHealthBefore, CellCoord[] targetOccupiedCells,
            int damageBeforeMatchup, int matchupNumerator, int matchupDenominator, int incomingReduction,
            AttackTargetRole role = AttackTargetRole.None, int appliedSplashPercent = 100, int healthNormalizationLoss = 0,
            int secondaryDamage = 0, int? targetHealthAfter = null, bool isDirectHit = true, int healthRecovery = 0,
            bool targetReconstructs = false)
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
            IsDirectHit = isDirectHit;
            ExpectedDamage = expectedDamage;
            SecondaryDamage = secondaryDamage;
            if (healthRecovery < 0) throw new ArgumentOutOfRangeException(nameof(healthRecovery));
            if (healthNormalizationLoss < 0 || healthNormalizationLoss > (long)targetHealthBefore + healthRecovery)
                throw new ArgumentOutOfRangeException(nameof(healthNormalizationLoss));
            HealthNormalizationLoss = healthNormalizationLoss;
            TargetHealthBefore = targetHealthBefore;
            if (targetHealthAfter < 0) throw new ArgumentOutOfRangeException(nameof(targetHealthAfter));
            TargetHealthAfter = targetHealthAfter ?? checked((int)Math.Max(0L, (long)TargetHealthBefore + healthRecovery - ExpectedDamage - HealthNormalizationLoss));
            TargetKilled = TargetHealthAfter == 0;
            TargetReconstructs = targetReconstructs;

            CellCoord[] copiedCells = new CellCoord[targetOccupiedCells.Length];
            Array.Copy(targetOccupiedCells, copiedCells, targetOccupiedCells.Length);

            TargetOccupiedCells = copiedCells;
        }
    }
}
