using System;
using System.Collections.Generic;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 공격 칸의 값입니다(클립 설계 AC8, 클립 칸 다시 짜기 설계 CS1·CS11). 공격 동작(<see cref="CombatClipMotion"/>)과 공격 칸에만 있는 시작 기준입니다.
    /// 옛 발사 목록·피격 선딜레이·재생 배율은 동작의 이벤트 목록·재생 배율로 옮겼습니다(설계 CS2, 사용자 정정 P12).
    /// 소리·이펙트 시점은 공격 묶음의 큐입니다(범용 연출 구조 설계 §5).
    /// </summary>
    internal sealed class CombatClipAttackData
    {
        /// <exception cref="ArgumentException">시작 기준이 정의되지 않았거나 WithLead 지연이 유한한 수가 아닐 때입니다(<see cref="FindStartError"/>).</exception>
        internal CombatClipAttackData(CombatClipMotion motion, CombatAttackMotionStart motionStart = CombatAttackMotionStart.AfterLead,
            float motionStartDelay = 0f)
        {
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            string error = FindStartError(motionStart, motionStartDelay);
            if (error != null) throw new ArgumentException(error, nameof(motionStart));
            MotionStart = motionStart;
            MotionStartDelay = motionStart == CombatAttackMotionStart.WithLead ? motionStartDelay : 0f;
        }

        /// <summary>공격 동작입니다.</summary>
        internal CombatClipMotion Motion { get; }

        /// <summary>본 단계입니다(<see cref="CombatClipMotion.Main"/>).</summary>
        internal CombatClipData Clip => Motion.Main;

        /// <summary>공격 칸의 재생 배율입니다(<see cref="CombatClipMotion.AnimSpeed"/>).</summary>
        internal float AnimSpeed => Motion.AnimSpeed;

        /// <summary>
        /// 이벤트 줄 목록입니다(<see cref="CombatClipMotion.Events"/>, 맨 앞은 늘 Event1 줄 — 적지 않으면 [Event1, 0, 0]). 원거리 = 줄마다 한 발(지점 = 발사 지점, 지연 0),
        /// 근접 = 줄마다 맞는 순간(지연 = 이벤트 뒤 맞기까지의 초, 지점은 쓰지 않음). 종류별 줄 규칙은 외형 표 검사가 봅니다.
        /// </summary>
        internal IReadOnlyList<CombatClipEventEntry> Events => Motion.Events;

        /// <summary>공격 동작 시작 기준입니다(설계 CS11). 맞는 순간의 지연(이벤트 줄)과 별개입니다.</summary>
        internal CombatAttackMotionStart MotionStart { get; }

        /// <summary>
        /// <see cref="CombatAttackMotionStart.WithLead"/>일 때 선행 연출 시작 기준의 공격 동작 시작 초입니다. 기본 기준이면 0입니다.
        /// 양수 = 그만큼 늦게, 0 = 함께, 음수 = 그만큼 먼저 시작하고 선행 구간의 첫 사건 연출이 그 초만큼 기다립니다(<see cref="LeadHoldSeconds"/>, 11-17 사용자 결정 A).
        /// </summary>
        internal float MotionStartDelay { get; }

        /// <summary>선행 구간의 연출 시작에 맞춰 공격 동작을 일찍 시작하는지입니다.</summary>
        internal bool StartsWithLead => MotionStart == CombatAttackMotionStart.WithLead;

        /// <summary>
        /// 공격 동작을 선행 연출보다 먼저 시작할 초입니다(음수 지연의 크기). 이만큼 선행 구간의 첫 사건 연출을 붙잡습니다. 앞당기지 않으면 0입니다.
        /// </summary>
        internal float LeadHoldSeconds => StartsWithLead && MotionStartDelay < 0f ? -MotionStartDelay : 0f;

        /// <summary>
        /// SO 입력과 이 값이 공유하는 시작 기준 검사입니다. 맞으면 null입니다. WithLead 지연은 유한한 수면 부호와 상관없습니다(음수 = 앞당김).
        /// 기본 기준의 지연은 쓰지 않아 보지 않습니다(선행 연출이 언제 끝날지 미리 몰라 앞당길 수 없음 — 공격 전 동작은 앞 단계로 둠).
        /// </summary>
        internal static string FindStartError(CombatAttackMotionStart motionStart, float motionStartDelay)
        {
            if (motionStart != CombatAttackMotionStart.AfterLead && motionStart != CombatAttackMotionStart.WithLead)
                return $"motionStart — 정의되지 않은 시작 기준입니다: {(int)motionStart}";
            if (motionStart == CombatAttackMotionStart.WithLead && !CombatPresentationValueRules.IsFinite(motionStartDelay))
                return "motionStartDelay — 지연은 유한한 초여야 합니다(음수 = 그만큼 먼저 시작)";
            return null;
        }
    }
}
