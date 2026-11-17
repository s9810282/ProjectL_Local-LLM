using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 클립 이벤트 목록의 줄 입력입니다(클립 칸 다시 짜기 설계 CS2). 옛 발사 목록(<c>shots</c>)·피격 선딜레이(<c>hitDelay</c>)를 대신합니다.
    /// 줄의 뜻은 칸·공격 종류가 정합니다: 원거리 = 한 발, 근접 = 맞는 순간, 피격 = 타이밍 표시(외형 표 검사 <see cref="CombatUnitAnimationRules"/>).
    /// </summary>
    [Serializable]
    internal sealed class CombatClipEventEntryInput
    {
        [Tooltip("등록할 클립 이벤트 번호입니다(Event1 = ClipEvent, Event2 = ClipEvent2, Event3 = ClipEvent3). 주 클립에 그 이름표가 있어야 합니다.")]
        [SerializeField] private CombatClipEventId eventId = CombatClipEventId.Event1;
        [Tooltip("원거리 공격 = 발사 지점 번호(이벤트 컴포넌트 Event Points의 칸). 근접·피격은 지금 쓰지 않으며 값 제약이 없습니다.")]
        [SerializeField, Min(0)] private int point;
        [Tooltip("근접 공격 = 이벤트 뒤 대상이 맞기까지의 초(옛 피격 선딜레이). 원거리·피격은 0만 됩니다.")]
        [SerializeField, Min(0f)] private float delay;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatClipEventEntryInput()
        {
        }

        internal CombatClipEventEntryInput(CombatClipEventId eventId, int point, float delay)
        {
            this.eventId = eventId;
            this.point = point;
            this.delay = delay;
        }

        internal CombatClipEventEntry ToData() => new(eventId, point, delay);
    }

    /// <summary>클립 이벤트 목록의 줄 하나입니다. 이벤트 번호·지점 번호·지연은 서로 별개입니다.</summary>
    internal readonly struct CombatClipEventEntry
    {
        /// <exception cref="ArgumentOutOfRangeException">번호가 표에 없거나 지점이 음수이거나 지연이 유한한 0 이상이 아닐 때입니다.</exception>
        internal CombatClipEventEntry(CombatClipEventId eventId, int point, float delay)
        {
            if (!CombatClipEventIds.IsDefined(eventId))
                throw new ArgumentOutOfRangeException(nameof(eventId), eventId, "정의된 클립 이벤트여야 합니다.");
            if (point < 0) throw new ArgumentOutOfRangeException(nameof(point), point, "이벤트 지점 번호는 0 이상이어야 합니다.");
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            EventId = eventId;
            Point = point;
            Delay = delay;
        }

        internal CombatClipEventId EventId { get; }

        /// <summary>원거리 발사 지점 번호입니다(근접·피격은 읽지 않으며 값 제약이 없음).</summary>
        internal int Point { get; }

        /// <summary>근접의 이벤트 뒤 맞기까지의 초입니다(원거리·피격은 0).</summary>
        internal float Delay { get; }
    }
}
