using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>모든 탄의 최초 접촉 뒤 출처를 한 번 해제하는 외부 진행 조각입니다. 시계와 투사체 자원은 소유하지 않습니다.</summary>
    /// <remarks>
    /// 접촉마다 접촉 표시 입구(<c>contactFeedback</c>)를 접촉 순번으로 부릅니다. 그 입구가 탄별 숫자와 앞 탄의 피격(곁들임)을 냅니다
    /// (<c>CombatAttackStart</c>가 묶음). 마지막 순번 뒤에 출처를 풀어 Core 피해 사건(피격·사망·체력)이 이어집니다.
    /// </remarks>
    internal sealed class CombatVolleyReleasePiece : ICombatPresentationPiece
    {
        private enum State
        {
            Created,
            Running,
            Released,
            Cancelled,
        }

        private readonly Action _release;
        private readonly Func<bool> _canRelease;
        private readonly Action<int> _contactFeedback;
        private readonly bool[] _contacted;
        private int _remaining;
        private int _presentedContacts;
        private State _state;
        private Action _releaseHit;
        private Action _end;

        internal CombatVolleyReleasePiece(int count, Action release, Func<bool> canRelease = null, Action<int> contactFeedback = null)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "집계할 발사는 하나 이상이어야 합니다.");
            _release = release ?? throw new ArgumentNullException(nameof(release));
            _canRelease = canRelease;
            _contactFeedback = contactFeedback;
            _contacted = new bool[count];
            _remaining = count;
        }

        public bool IsEnded => false;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (_state != State.Created)
                throw new InvalidOperationException("[CombatVolleyReleasePiece] 접촉 집계는 한 번만 시작합니다.");
            _releaseHit = CombatHitMoment.ReleaseThenSignal(_release, context.SignalCallback(CombatPieceSignal.Hit));
            _end = context.EndCallback();
            _state = State.Running;
        }

        /// <summary>조립 때 발사별로 주입할 접촉 입구입니다. 가변 상태나 실행 맥락을 다른 조각에 내주지 않습니다.</summary>
        internal Action ContactCallback(int index)
        {
            if (index < 0 || index >= _contacted.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "발사 목록에 있는 인덱스여야 합니다.");
            return () => Contact(index);
        }

        private void Contact(int index)
        {
            if (_state == State.Cancelled || _state == State.Released) return;
            if (_state != State.Running)
                throw new InvalidOperationException($"[CombatVolleyReleasePiece] 집계 준비 전에 접촉했습니다: 발사 {index}");
            if (_contacted[index]) return;
            _contacted[index] = true;
            _remaining--;
            TryRelease();
        }

        private void TryRelease()
        {
            if (_state != State.Running || _canRelease?.Invoke() == false) return;

            // 조기 공격의 합류 전 접촉도 보관했다가 표시합니다. 접촉 표시(숫자·앞 탄 피격)는 기존 후속 해제보다 먼저 냅니다.
            while (_presentedContacts < _contacted.Length - _remaining)
            {
                int index = _presentedContacts++;
                _contactFeedback?.Invoke(index);
                if (_state != State.Running || _canRelease?.Invoke() == false) return;
            }
            if (_remaining != 0) return;

            // 해제가 재진입/취소를 일으켜도 이미 센 접촉을 다시 처리하지 않습니다.
            _state = State.Released;
            Action releaseHit = _releaseHit;
            Action end = _end;
            releaseHit();
            end();
        }

        public void Tick(float deltaTime)
        {
            TryRelease();
        }

        public void Cancel()
        {
            _state = State.Cancelled;
            _releaseHit = null;
            _end = null;
        }
    }
}
