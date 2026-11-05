using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 사건 클립 하나의 재생입니다(클립 설계 AC5, 계획 T13). 클립 대여(<see cref="ICombatUnitClip"/>)에 시각을 넣고,
    /// 시작·존재하는 중간 이벤트·끝을 시각 순으로 콜백에 알립니다. Unity 애니메이션 이벤트는 쓰지 않습니다(설계 AC2).
    /// </summary>
    /// <remarks>
    /// <para>시각은 시작부터 받은 시간의 합이고, 대여에 넣는 시각은 <see cref="CombatClipData.EndTime"/>에서 멈춥니다. 그 자세는 같은 프레임의 Unity 평가에서 보입니다(설계 F16).</para>
    /// <para>대여 복원은 대여 주인이 합니다. 이 클래스는 시각과 이벤트만 다룹니다. 재생 하나에 하나를 만들고, <see cref="Tick"/>은 할당하지 않습니다.</para>
    /// </remarks>
    internal sealed class CombatClipPlayback
    {
        private readonly ICombatUnitClip _lease;
        private readonly CombatClipData _data;
        private readonly Action _started;
        private readonly Action _eventTiming;
        private readonly Action _eventTiming2;
        private readonly Action _eventTiming3;
        private readonly Action _ended;
        private readonly CombatClipEventCursor _cursor;
        private float _time;
        private bool _begun;
        private bool _cancelled;

        /// <param name="started">시작 이벤트에서 부릅니다. 받는 곳이 없으면 null입니다.</param>
        /// <param name="eventTiming">이벤트 타이밍에서 부릅니다. 받는 곳이 없으면 null입니다.</param>
        /// <param name="ended">끝 이벤트에서 부릅니다. 받는 곳이 없으면 null입니다.</param>
        /// <param name="eventTiming2">선택 두 번째 이벤트에서 부릅니다. 기존 호출은 받지 않아도 됩니다.</param>
        /// <param name="eventTiming3">선택 세 번째 이벤트에서 부릅니다. 기존 호출은 받지 않아도 됩니다.</param>
        internal CombatClipPlayback(ICombatUnitClip lease, CombatClipData data, Action started, Action eventTiming, Action ended,
            Action eventTiming2 = null, Action eventTiming3 = null)
        {
            _lease = lease ?? throw new ArgumentNullException(nameof(lease));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _started = started;
            _eventTiming = eventTiming;
            _eventTiming2 = eventTiming2;
            _eventTiming3 = eventTiming3;
            _ended = ended;
            _cursor = new CombatClipEventCursor(data.Timings);
        }

        /// <summary>끝 이벤트를 냈는지입니다. 취소된 재생은 완료가 아닙니다.</summary>
        internal bool IsComplete => _cursor.IsFinished;

        /// <summary>클립을 대여에 꽂고(시각 0) 0초 이벤트를 냅니다. 한 번만 부릅니다.</summary>
        internal void Begin()
        {
            if (_begun)
                throw new InvalidOperationException("[CombatClipPlayback] 이미 시작한 재생입니다.");
            _begun = true;
            if (_cancelled) return;
            _lease.Play(_data);
            Emit(0f);
        }

        /// <summary>시각을 <paramref name="deltaTime"/>만큼 진행하고 그 시각까지의 이벤트를 냅니다. 음수·수가 아닌 시간은 0으로 봅니다. 끝나거나 취소된 뒤에는 아무 일도 하지 않습니다.</summary>
        internal void Tick(float deltaTime)
        {
            if (!_begun)
                throw new InvalidOperationException("[CombatClipPlayback] 시작하지 않은 재생입니다.");
            if (_cancelled || IsComplete) return;
            if (deltaTime > 0f) _time += deltaTime;
            float time = Math.Min(_time, _data.EndTime);
            _lease.SetTime(time);
            Emit(time);
        }

        /// <summary>그 뒤로 콜백을 부르지 않습니다. 콜백 안에서 부르면 남은 이벤트를 내지 않습니다. 대여는 되돌리지 않습니다.</summary>
        internal void Cancel() => _cancelled = true;

        private void Emit(float time)
        {
            while (!_cancelled && _cursor.TryNext(time, out CombatClipEventKind kind))
            {
                Action callback = kind switch
                {
                    CombatClipEventKind.Start => _started,
                    CombatClipEventKind.Event => _eventTiming,
                    CombatClipEventKind.Event2 => _eventTiming2,
                    CombatClipEventKind.Event3 => _eventTiming3,
                    CombatClipEventKind.End => _ended,
                    _ => throw new InvalidOperationException($"[CombatClipPlayback] 재생할 클립 이벤트 종류가 없습니다: {kind}"),
                };
                callback?.Invoke();
            }
        }
    }
}
