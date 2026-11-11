using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 표시 대여 칸 하나의 수명 규칙입니다(클립 계획 T10a, 결정 Q-AC11 A). <see cref="FieldUnitView"/>의 채널(자세·서는 자리·크기·플래시·클립)이 같이 씁니다.
    /// </summary>
    /// <remarks>
    /// <para>규칙은 셋입니다. ① 칸에는 대여 하나만 있습니다. ② 지난 대여의 복원(늦은 복원)은 아무 일도 하지 않습니다(같은 대여 객체를 다시 쓰지 않으므로 참조로 가립니다).
    /// ③ 밖에서 끝낼 때(<see cref="Reset"/>: 표시 꺼짐·다른 ID로 재대여·풀 반환)는 취소 콜백을 먼저 꺼내 두고, 복원한 뒤, 복원이 예외를 내도 칸을 비우고 콜백을 한 번 부릅니다.</para>
    /// <para>시작 조건과 예외 문구, 대여의 스냅숏·복원 내용, 쓸 수 있는지(<c>IsAvailable</c>)의 채널별 조건은 채널(대여 클래스) 쪽에 둡니다.
    /// 카메라 줌 대여는 맵이 빠질 때 되돌리지 않는 다른 규칙이라 이 칸을 쓰지 않습니다.</para>
    /// </remarks>
    internal sealed class UnitPresentationLeaseSlot<TLease> where TLease : class
    {
        private TLease _current;
        private Action _cancelled;

        /// <summary>지금 대여입니다. 없으면 null입니다.</summary>
        internal TLease Current => _current;

        /// <summary>새 대여를 칸에 넣습니다. <paramref name="cancelled"/>는 밖에서 끝날 때 한 번 부릅니다(대여가 스스로 돌아오면 부르지 않음).</summary>
        /// <exception cref="InvalidOperationException">이전 대여가 남아 있을 때입니다. 채널은 이 전에 자기 문구로 먼저 검사합니다.</exception>
        internal void Hold(TLease lease, Action cancelled)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (_current != null)
                throw new InvalidOperationException("[UnitPresentationLeaseSlot] 이전 대여가 아직 남아 있습니다.");
            _current = lease;
            _cancelled = cancelled;
        }

        /// <summary><paramref name="lease"/>가 지금 대여인지입니다. 지난 대여면 false입니다.</summary>
        internal bool IsCurrent(TLease lease) => lease != null && ReferenceEquals(_current, lease);

        /// <summary>
        /// 대여가 스스로 돌아올 때 칸을 비웁니다(취소 콜백은 부르지 않음). 지금 대여가 아니면 아무것도 하지 않고 false입니다(늦은 복원).
        /// 대여는 true일 때만 자기 복원 내용을 적용합니다.
        /// </summary>
        internal bool Release(TLease lease)
        {
            if (!IsCurrent(lease))
                return false;
            _current = null;
            _cancelled = null;
            return true;
        }

        /// <summary>
        /// 밖에서 대여를 끝냅니다. 취소 콜백을 꺼낸 뒤 <paramref name="restore"/>로 지금 대여를 복원하고, 복원이 예외를 내도 칸을 비우고 콜백을 한 번 부릅니다.
        /// 대여가 없으면 아무 일도 하지 않습니다. <paramref name="restore"/>는 대여의 복원 함수(안에서 <see cref="Release"/>를 부름)입니다.
        /// </summary>
        internal void Reset(Action<TLease> restore)
        {
            if (restore == null) throw new ArgumentNullException(nameof(restore));
            TLease lease = _current;
            Action cancelled = _cancelled;
            _cancelled = null;
            try
            {
                if (lease != null)
                    restore(lease);
            }
            finally
            {
                _current = null;
                cancelled?.Invoke();
            }
        }
    }
}
