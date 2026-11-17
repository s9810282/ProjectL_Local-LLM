using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 피격·사망의 표현 값입니다. 사건의 체력·죽음과 런타임 진행 상태는 포함하지 않습니다.
    /// 뼈대(반응 동작 값)와 칸마다의 연출 큐(피격: 이펙트·흔들림·플래시, 사망: 이펙트·흔들림)를 둡니다. 피격·사망 소리는 유닛 외형의 소리 줄입니다(유닛 소리 설계 US2)(범용 연출 구조 설계 G1·G2).
    /// </summary>
    internal sealed class CombatUnitReactionPresentationData
    {
        /// <summary>피격 기울기 각도 상한(도)입니다. 입력 SO의 칸 범위와 같습니다.</summary>
        internal const float MaxHitAngle = 60f;

        /// <param name="hitCues">피격 칸 큐입니다. 플래시(피격 플래시 설계 BP9)·피격 이펙트(피격 이펙트 설계 HE1)·흔들림이 여기 있습니다.</param>
        /// <param name="deathCues">사망 칸 큐입니다. 치명타 이펙트(Q-HE3 A)·흔들림이 여기 있습니다.</param>
        /// <param name="hitMotionWithClip">피격 클립이 있는 유닛도 코드 피격 움직임을 낼지입니다(<see cref="PlaysHitMotion"/>).</param>
        /// <exception cref="ArgumentException">
        /// 값이 규칙에 맞지 않을 때입니다. 값 조건의 원본은 이 생성자이고(범용 연출 구조 계획 Ga.3), 수 판정은 <see cref="CombatPresentationValueRules"/>,
        /// 큐 조합은 <see cref="CombatPresentationCueRules"/>를 따릅니다. 입력 SO는 이 예외에 에셋 이름을 붙여 다시 던집니다.
        /// </exception>
        internal CombatUnitReactionPresentationData(float hitDuration, float hitCellFraction, float hitAngle,
            float deathDuration, float deathRestDuration, CombatEventCues hitCues, CombatEventCues deathCues, bool hitMotionWithClip)
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
            _hitMotionWithClip = hitMotionWithClip;
        }

        /// <summary>피격 클립이 있는 유닛도 코드 피격 움직임을 내는지입니다. 밖에서는 읽지 않고, 판단은 <see cref="PlaysHitMotion"/> 하나로 합니다.</summary>
        private readonly bool _hitMotionWithClip;

        internal float HitDuration { get; }
        internal float HitCellFraction { get; }
        internal float HitAngle { get; }
        internal float DeathDuration { get; }
        internal float DeathRestDuration { get; }

        /// <summary>피격 칸 큐입니다(이펙트·흔들림·플래시).</summary>
        internal CombatEventCues HitCues { get; }

        /// <summary>사망 칸 큐입니다(이펙트·흔들림).</summary>
        internal CombatEventCues DeathCues { get; }

        /// <summary>
        /// 코드 피격 움직임(맞은 방향으로 밀렸다 돌아오며 젖힘, 넉백 때 밀리는 동안 젖힘)을 낼지의 규칙 원본입니다(2026-11-17 사용자 "끄고 켜는 옵션, 기본은 끔").
        /// 피격 클립이 없는 유닛은 늘 내고, 있는 유닛은 <see cref="_hitMotionWithClip"/>이 켜졌을 때만 냅니다.
        /// 내지 않아도 피격 시간(<see cref="HitDuration"/>)·피격 칸 큐(플래시·이펙트·흔들림·소리)는 그대로라 큐의 끝 시점은 같습니다.
        /// 표시 쪽은 유닛 표시 창구의 같은 이름 질의 하나로 부릅니다(그 유닛의 피격 클립 유무를 창구가 넣음).
        /// </summary>
        /// <param name="hasHitClip">그 유닛에 피격 동작이 있는지입니다(클립 묶음의 피격 칸).</param>
        internal bool PlaysHitMotion(bool hasHitClip) => !hasHitClip || _hitMotionWithClip;
    }
}
