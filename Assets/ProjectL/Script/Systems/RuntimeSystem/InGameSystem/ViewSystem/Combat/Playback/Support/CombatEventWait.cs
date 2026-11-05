using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 결과 재생의 대기 칸 하나입니다(Presenter 분리 설계 S2). 적 행동 간격·턴 간격·걸음 전 간격의 남은 시간과,
    /// 간격 뒤에 표시하거나 완료할 대기 사건을 갖습니다. 행동은 순서대로라 칸은 하나입니다(동시 처리 설계 C5).
    /// 간격 값은 <see cref="CombatEnemyPresentationPacing"/>이 원본이고, 이 클래스는 시간과 대기 사건만 갖습니다.
    /// 걸음 레인과의 순서(간격 동안 레인을 멈춤)와 시작 전제는 이 칸을 가진 걸음·대기 재생기(<see cref="CombatStepPlayer"/>)가 정합니다(연출 조립 설계 B12).
    /// </summary>
    internal sealed class CombatEventWait
    {
        private float _remaining;
        private CombatEvent _event;
        private Action _completed;
        private bool _shownBeforeGap;

        /// <summary>간격 뒤 대기 사건이 있는지입니다. 있으면 다음 사건을 시작하지 않습니다.</summary>
        internal bool HasEvent => _event != null;

        /// <summary>간격을 시작합니다. 남은 시간을 덮어씁니다.</summary>
        internal void StartGap(float seconds) => _remaining = seconds;

        /// <summary>
        /// 간격이 끝나면 처리할 사건을 겁니다. <paramref name="shownBeforeGap"/>이면 사건은 이미 보였고 간격 뒤 완료만 알립니다.
        /// 아니면 간격 뒤 사건을 표시합니다.
        /// </summary>
        internal void HoldAfterGap(CombatEvent e, Action completed, bool shownBeforeGap)
        {
            _event = e;
            _completed = completed;
            _shownBeforeGap = shownBeforeGap;
        }

        /// <summary>
        /// 보인 사건을 <paramref name="seconds"/>만큼 머물게 한 뒤 완료합니다(동작을 시작하지 못한 포효·소환, 보스 활성화 설계 D6).
        /// 걸음 전 간격과 달리 시작 전제를 보지 않습니다.
        /// </summary>
        internal void HoldShown(CombatEvent e, Action completed, float seconds)
        {
            _remaining = seconds;
            HoldAfterGap(e, completed, true);
        }

        /// <summary>
        /// 프레임 시간을 간격에 씁니다. 간격이 이 프레임 안에 끝나지 않으면 false입니다.
        /// 끝났거나 간격이 없으면 true이고, 간격에 쓰고 남은 시간을 <paramref name="seconds"/>에 돌려줍니다.
        /// </summary>
        internal bool Consume(ref float seconds)
        {
            if (_remaining > 0f)
            {
                if (seconds < _remaining)
                {
                    _remaining -= seconds;
                    return false;
                }
                seconds -= _remaining;
                _remaining = 0f;
            }
            return true;
        }

        /// <summary>대기 사건을 꺼내고 칸에서 비웁니다. 남은 간격 시간은 건드리지 않습니다.</summary>
        internal bool TryTake(out CombatEvent e, out Action completed, out bool shownBeforeGap)
        {
            e = _event;
            completed = _completed;
            shownBeforeGap = _shownBeforeGap;
            if (e == null) return false;
            ClearEvent();
            return true;
        }

        /// <summary>남은 간격과 대기 사건을 모두 비웁니다(재생 취소).</summary>
        internal void Clear()
        {
            _remaining = 0f;
            ClearEvent();
        }

        private void ClearEvent()
        {
            _event = null;
            _completed = null;
            _shownBeforeGap = false;
        }
    }
}
