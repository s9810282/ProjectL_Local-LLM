using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 원거리 클립 공격의 접촉 집계입니다. 마지막 탄의 접촉마다 그 대상을 해제하고, 마지막 탄의 마지막 접촉에서 남은 대상을 모두 해제하는
    /// 외부 진행 조각입니다(관통·죽음의 손 View 설계 D6, 규칙 원본 <see cref="CombatContactRelease"/>). 시계와 투사체 자원은 소유하지 않습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 한 공격의 탄들은 같은 공격 모양을 쓰므로 "대상마다 그 대상의 마지막 접촉"은 "마지막으로 처음 닿은 탄의 접촉"입니다. 앞 탄의 접촉은 해제하지 않습니다.
    /// </para>
    /// <para>
    /// 탄이 처음 닿을 때마다 접촉 표시 입구(<c>contactFeedback</c>)를 접촉 순번으로 부릅니다. 그 입구가 탄별 숫자와 앞 탄의 피격(곁들임)을 냅니다
    /// (<c>CombatAttackStart</c>가 묶음). 관통이 아닌 탄은 접촉이 하나라, 마지막 탄의 첫 접촉에서 출처를 풀어 Core 피해 사건(피격·사망·체력)이 이어집니다(옛 동작과 같음).
    /// 조기 공격의 합류 전 접촉은 해제 허용(<c>canRelease</c>)까지 순서대로 보관합니다(계획 TP-5).
    /// </para>
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

        private readonly Action<EntityId> _releaseTarget;
        private readonly Action _release;
        private readonly Func<bool> _canRelease;
        private readonly Action<int> _contactFeedback;
        private readonly bool[] _contacted;
        // 받은 접촉(탄 순번, 대상, 그 탄의 마지막 접촉인지)입니다. 해제 허용 전에도 순서대로 쌓고 _processed까지 처리했습니다.
        private readonly List<(int Shot, EntityId Target, bool Final)> _pending = new();
        private int _processed;
        private int _remaining;
        private int _presentedContacts;
        private int _lastShot = -1;
        private bool _draining;
        private State _state;
        private CombatContactRelease _contacts;
        private Action _end;

        /// <param name="releaseTarget">대상 하나를 해제하는 입구입니다(<c>ICombatSourceLifetime.Release</c>). 없으면 대상별로 풀지 않고 마지막 접촉에서 모두 해제합니다
        /// (그때 타격 신호도 마지막 접촉에서 납니다).</param>
        /// <param name="release">남은 대상을 모두 해제하는 입구입니다(<c>ICombatSourceLifetime.ReleaseAll</c>).</param>
        internal CombatVolleyReleasePiece(int count, Action<EntityId> releaseTarget, Action release, Func<bool> canRelease = null,
            Action<int> contactFeedback = null)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "집계할 발사는 하나 이상이어야 합니다.");
            _releaseTarget = releaseTarget;
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
            _contacts = new CombatContactRelease(_releaseTarget, _release, context.SignalCallback(CombatPieceSignal.Hit));
            _end = context.EndCallback();
            _state = State.Running;
        }

        /// <summary>조립 때 발사별로 주입할 접촉 입구입니다(대상, 그 탄의 마지막 접촉인지). 가변 상태나 실행 맥락을 다른 조각에 내주지 않습니다.</summary>
        internal Action<EntityId, bool> ContactCallback(int index)
        {
            if (index < 0 || index >= _contacted.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "발사 목록에 있는 인덱스여야 합니다.");
            return (target, final) => Contact(index, target, final);
        }

        private void Contact(int index, EntityId target, bool final)
        {
            if (_state == State.Cancelled || _state == State.Released) return;
            if (_state != State.Running)
                throw new InvalidOperationException($"[CombatVolleyReleasePiece] 집계 준비 전에 접촉했습니다: 발사 {index}");
            _pending.Add((index, target, final));
            TryRelease();
        }

        private void TryRelease()
        {
            // 표시·해제 콜백 안에서 접촉이 다시 들어오면 쌓기만 하고, 바깥 반복이 이어서 처리합니다(같은 항목 두 번 처리 막기, PD-a 검토 L2).
            if (_draining || _state != State.Running || _canRelease?.Invoke() == false) return;
            _draining = true;
            try
            {
                Drain();
            }
            finally
            {
                _draining = false;
            }
        }

        private void Drain()
        {
            while (_processed < _pending.Count)
            {
                (int shot, EntityId target, bool final) = _pending[_processed];
                if (!_contacted[shot])
                {
                    // 탄의 첫 접촉입니다. 마지막으로 처음 닿은 탄이 해제할 탄입니다.
                    _contacted[shot] = true;
                    _remaining--;
                    if (_remaining == 0) _lastShot = shot;
                    // 접촉 표시(숫자·앞 탄 피격)는 기존 후속 해제보다 먼저 냅니다. 표시가 실행을 멈추면 이 접촉의 해제는 다음 Tick에 이어 합니다.
                    _contactFeedback?.Invoke(_presentedContacts++);
                    if (_state != State.Running || _canRelease?.Invoke() == false) return;
                }
                _processed++;
                if (shot != _lastShot) continue;
                if (final)
                {
                    // 해제가 재진입/취소를 일으켜도 이미 센 접촉을 다시 처리하지 않습니다.
                    _state = State.Released;
                    CombatContactRelease contacts = _contacts;
                    Action end = _end;
                    contacts.Contact(target, true);
                    end();
                    return;
                }
                _contacts.Contact(target, false);
                if (_state != State.Running) return;
            }
        }

        public void Tick(float deltaTime)
        {
            TryRelease();
        }

        public void Cancel()
        {
            _state = State.Cancelled;
            _contacts = null;
            _end = null;
            _pending.Clear();
        }
    }
}
