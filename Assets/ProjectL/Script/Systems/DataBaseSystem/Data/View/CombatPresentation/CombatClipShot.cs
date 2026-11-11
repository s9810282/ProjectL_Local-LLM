using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>공격 클립에서 발사할 이벤트와 총구 번호를 고르는 입력입니다.</summary>
    [Serializable]
    internal sealed class CombatClipShotEntry
    {
        [SerializeField] private CombatClipEventId eventId = CombatClipEventId.Event;
        [SerializeField, Min(0)] private int point;

        internal CombatClipShot ToData() => new(eventId, point);
    }

    /// <summary>번호별 클립 이벤트에 연결한 한 발입니다. 이벤트 번호와 총구 번호는 별개입니다.</summary>
    internal readonly struct CombatClipShot
    {
        internal CombatClipShot(CombatClipEventId eventId, int point)
        {
            if (!CombatClipEventIds.IsDefined(eventId))
                throw new ArgumentOutOfRangeException(nameof(eventId), eventId, "정의된 클립 이벤트여야 합니다.");
            if (point < 0) throw new ArgumentOutOfRangeException(nameof(point), point, "발사 지점 번호는 0 이상이어야 합니다.");
            EventId = eventId;
            Point = point;
        }

        internal CombatClipEventId EventId { get; }
        internal int Point { get; }
    }
}
