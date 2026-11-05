using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 피격·사망의 표현 값입니다. 사건의 체력·죽음과 런타임 진행 상태는 포함하지 않습니다.
    /// 뼈대(반응 동작 값)와 칸마다의 연출 큐(피격: 이펙트·흔들림·플래시, 사망: 이펙트·흔들림·소리 시점)를 둡니다(범용 연출 구조 설계 G1·G2).
    /// </summary>
    internal sealed class CombatUnitReactionPresentationData
    {
        /// <summary>피격 기울기 각도 상한(도)입니다. 입력 SO의 칸 범위와 같습니다.</summary>
        internal const float MaxHitAngle = 60f;

        /// <param name="hitCues">피격 칸 큐입니다. 플래시(피격 플래시 설계 BP9)·피격 이펙트(피격 이펙트 설계 HE1)·흔들림이 여기 있습니다.</param>
        /// <param name="deathCues">사망 칸 큐입니다. 치명타 이펙트(Q-HE3 A)·흔들림·사망 소리 시점이 여기 있습니다.</param>
        /// <exception cref="ArgumentException">
        /// 값이 규칙에 맞지 않을 때입니다. 값 조건의 원본은 이 생성자이고(범용 연출 구조 계획 Ga.3), 수 판정은 <see cref="CombatPresentationValueRules"/>,
        /// 큐 조합은 <see cref="CombatPresentationCueRules"/>를 따릅니다. 입력 SO는 이 예외에 에셋 이름을 붙여 다시 던집니다.
        /// </exception>
        internal CombatUnitReactionPresentationData(float hitDuration, float hitCellFraction, float hitAngle,
            float deathDuration, float deathRestDuration, CombatEventCues hitCues, CombatEventCues deathCues)
        {
            // 검사 순서는 옛 SO 검사와 같습니다. 여러 값이 함께 잘못됐을 때 먼저 알리는 값이 같습니다.
            CombatPresentationValueRules.RequirePositive(hitDuration, nameof(hitDuration));
            CombatPresentationValueRules.RequireNonNegative(hitCellFraction, nameof(hitCellFraction));
            CombatPresentationValueRules.RequireNonNegative(hitAngle, nameof(hitAngle));
            if (hitAngle > MaxHitAngle)
                throw new ArgumentOutOfRangeException(nameof(hitAngle), hitAngle, $"피격 기울기는 {MaxHitAngle}도 이하여야 합니다.");
            CombatPresentationValueRules.RequirePositive(deathDuration, nameof(deathDuration));
            CombatPresentationValueRules.RequireNonNegative(deathRestDuration, nameof(deathRestDuration));
            if (float.IsInfinity(deathDuration + deathRestDuration))
                throw new ArgumentOutOfRangeException(nameof(deathRestDuration), deathRestDuration, "사망 시간과 머묾 시간의 합이 유한해야 합니다.");
            HitCues = hitCues ?? throw new ArgumentNullException(nameof(hitCues));
            DeathCues = deathCues ?? throw new ArgumentNullException(nameof(deathCues));
            CombatPresentationCueRules.Require(hitCues, CombatCueEvent.Hit);
            CombatPresentationCueRules.Require(deathCues, CombatCueEvent.Death);

            HitDuration = hitDuration;
            HitCellFraction = hitCellFraction;
            HitAngle = hitAngle;
            DeathDuration = deathDuration;
            DeathRestDuration = deathRestDuration;
        }

        internal float HitDuration { get; }
        internal float HitCellFraction { get; }
        internal float HitAngle { get; }
        internal float DeathDuration { get; }
        internal float DeathRestDuration { get; }

        /// <summary>피격 칸 큐입니다(이펙트·흔들림·플래시).</summary>
        internal CombatEventCues HitCues { get; }

        /// <summary>사망 칸 큐입니다(이펙트·흔들림·소리 시점).</summary>
        internal CombatEventCues DeathCues { get; }
    }
}
