using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 결과 하나의 사건을 기존 View Tick 안에서 Core 순서대로 표시에 넘기는 연출입니다(설계 §9·§9.2).
    /// 걸음은 표시(<see cref="ICombatMovementPlaybackPresenter.BeginStep"/>)가 유닛별 레인에서 보간하고 걸음 사건 완료를 알리며, 그 밖의 사건은 <see cref="ICombatEventPlayback.BeginEvent"/>가 완료를 알립니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Buffer 확인·Flow·최종 표시 반영은 CombatViewSystem의 책임입니다. 세대 번호로 취소 뒤 늦게 온 완료 통지를 무시합니다.
    /// 표시 계약이 없는 사건은 사실 반영 없이 건너뛰고 경고만 남깁니다(리팩토링 설계 D1). 최종 사실은 결과 확인 전 표시 갱신이 맞춥니다.
    /// 표시 쪽 계약은 <see cref="ICombatEventPlayback"/> 하나로 받고 형식 검사로 꺼내 쓰지 않습니다(D6).
    /// </para>
    /// <para>
    /// 시작 순서·의존은 이 객체가 소유합니다(동시 처리 설계 C2·C3·C4). 사건 i는 앞의 모든 사건이 완료됐거나,
    /// 같은 "같은 순간" 묶음(<see cref="CombatConcurrentGroups"/>)이면서 주체가 다를 때 시작 후보이고, 표시가 허락하면 시작합니다.
    /// 같은 유닛의 사건 순서와 묶음 밖 사건 순서는 지금과 같고, 묶음 뒤 사건은 묶음 전체가 끝난 뒤입니다(Q-C2 A).
    /// 유닛 자원·Gate 판단(지금 시작해도 되는지)은 표시가 소유합니다.
    /// </para>
    /// <para>
    /// 걸음은 유닛별 레인에서 함께 돌 수 있고, 걸음별 사건 완료는 시작 때 넘긴 통지로 받습니다(동시 처리 설계 C5·C6).
    /// 통지는 상태만 바꾸고, 경로 끝 연출 시작 등 다음 동작은 이 객체의 Tick 훑기가 합니다(걸음 Tick 안 재진입 방지).
    /// </para>
    /// </remarks>
    internal sealed class OrderedCombatPresentation : ICombatPresentation, ICombatSourceSettlement
    {
        private enum EventState : byte
        {
            Pending,
            Started,
            // 걸음의 사건 완료 통지를 받았고, 경로 끝 판단을 기다립니다(Tick 훑기가 처리).
            StepDone,
            // 마지막 걸음이 끝나고 경로 끝 연출(방향 복귀)을 기다립니다.
            Ending,
            Completed,
        }

        private ICombatEventPlayback _eventPlayback;
        private CombatMovementPlayback _movement;
        private CombatResult _result;
        private Action<CombatPresentationOutcome> _finished;
        private readonly CombatConcurrentGroups _groups = new();
        // 결과 하나의 사건별 상태입니다. 결과마다 새 배열이라 이전 결과의 늦은 완료가 새 상태를 바꾸지 못합니다.
        private EventState[] _states = Array.Empty<EventState>();
        // 첫 미완료 사건 색인입니다. 앞은 모두 완료입니다.
        private int _first;
        private int _generation;
        private bool _isCancelling;
        // 이 연결에서 이미 경고한 사건 형식입니다. 적 턴마다 같은 경고가 쌓이지 않게 연결마다 한 번만 남깁니다.
        private readonly HashSet<Type> _warnedUnsupported = new();

        public void Attach(ICombatEventPlayback playback)
        {
            if (playback == null)
                throw new ArgumentNullException(nameof(playback));

            Cancel();
            _eventPlayback = playback;
            _movement = new CombatMovementPlayback(_eventPlayback);
            _warnedUnsupported.Clear();
            // 후속 정리 질문의 원본은 이 재생기 상태입니다(유닛 행동 계획 TP-13). 표시 쪽은 붙을 때 받습니다.
            _eventPlayback.BindSettlement(this);
        }

        /// <summary>
        /// 출처의 맞는 순간 후속 중 그 유닛 것이 모두 완료됐는지입니다(<see cref="ICombatSourceSettlement"/>). 앞 색인(<c>_first</c>) 앞은 모두 완료라 그 뒤만 봅니다.
        /// 아직 시작하지 않았고 차례 규칙(<see cref="CanStart"/>) 때문에 지금 시작할 수 없는 후속은 세지 않습니다(묶음 끝 검토 M1).
        /// 그 후속 앞의 사건이 출처 전체 종료를 기다리면(예: 공격자 자신의 반사 피해 — Gate 규칙 1), 세는 순간 출처 실행과 서로 기다려 멈추기 때문입니다.
        /// 그때 수호자는 먼저 돌아오고, 그 후속은 돌아온 자리에서 나옵니다.
        /// </summary>
        public bool IsSettled(CombatEvent source, EntityId unit)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (_result == null) return true;
            for (int i = _first; i < _states.Length; i++)
            {
                if (_states[i] == EventState.Completed || !_groups.IsHitFollowUpOf(_result.Events[i], source, unit))
                    continue;
                if (_states[i] == EventState.Pending && !CanStart(i))
                    continue;
                return false;
            }
            return true;
        }

        public void Play(CombatResult result, Action<CombatPresentationOutcome> onFinished)
        {
            if (_eventPlayback == null || _result != null)
                throw new InvalidOperationException("연출 연결/순서가 잘못되었습니다.");

            foreach (var e in result.Events)
            {
                if (CombatEventTypeTable.RouteOf(e) == CombatEventRoute.Skip && _warnedUnsupported.Add(e.GetType()))
                    UnityEngine.Debug.LogWarning(
                        $"[OrderedCombatPresentation] 표시 계약이 없는 전투 사건을 건너뜁니다: {e.GetType().Name}({e.Kind}), 결과 {result.Sequence}. " +
                        "최종 상태는 결과 확인 전 표시 갱신으로 맞춥니다.");
            }

            _movement ??= new CombatMovementPlayback(_eventPlayback);
            _result = result;
            _finished = onFinished;
            _states = new EventState[result.Events.Count];
            _first = 0;
            _groups.Begin(result);
            _eventPlayback.BeginPresentation(result);
            Tick(0);
        }

        public void Tick(float deltaTime)
        {
            // 최초 필드 생성은 결과 사건 없이 시작됩니다. 같은 재생기 프레임 경로를 계속 진행합니다.
            _eventPlayback?.TickPresentation(deltaTime);
            if (_result == null)
                return;

            int generation = _generation;
            for (int i = _first; _result != null && i < _states.Length; i++)
            {
                if (_states[i] == EventState.Pending)
                {
                    CombatEvent e = _result.Events[i];
                    // 표시 계약이 없는 사건은 대기·사실 반영 없이 넘긴다(경고는 Play에서 남겼다).
                    if (CombatEventTypeTable.RouteOf(e) == CombatEventRoute.Skip)
                        _states[i] = EventState.Completed;
                    else if (CanStart(i) && _eventPlayback.CanBeginPresentationEvent(e))
                    {
                        Start(i, e);
                        // 사건 시작 안에서 취소·다시 연결되면 이 Tick을 멈춘다.
                        if (_generation != generation || _result == null)
                            return;
                    }
                }

                if (_states[i] == EventState.StepDone)
                {
                    FinishStep(i);
                    if (_generation != generation || _result == null)
                        return;
                }

                if (_states[i] == EventState.Completed)
                {
                    if (i == _first) _first++;
                    continue;
                }

                // 묶음 밖의 미완료 사건은 뒤 사건 누구도 지나갈 수 없다(설계 C4 훑기 멈춤).
                if (_groups.IsBarrier(i))
                    break;
            }

            if (_result == null || _first < _states.Length)
                return;

            if (!_eventPlayback.IsPresentationComplete)
                return;

            var finished = _finished;
            _result = null;
            _finished = null;
            finished(CombatPresentationOutcome.Completed);
        }

        public void Detach()
        {
            try
            {
                Cancel();
            }
            finally
            {
                _eventPlayback = null;
                _movement = null;
            }
        }

        public void Cancel()
        {
            if (_isCancelling)
                return;

            _isCancelling = true;
            _generation++;
            _result = null;
            _finished = null;
            _states = Array.Empty<EventState>();
            _first = 0;
            _groups.Clear();
            var movement = _movement;
            _movement = null;
            List<Exception> errors = null;
            try
            {
                // 한 자원의 해제가 실패해도 다른 연출 정리를 시도한다. 늦은 완료는 이미 무효화했다.
                if (movement != null)
                    CleanupErrors.Try(movement.Detach, ref errors);
                if (_eventPlayback != null)
                    CleanupErrors.Try(_eventPlayback.CancelPresentation, ref errors);
            }
            finally
            {
                _isCancelling = false;
            }

            CleanupErrors.ThrowIfAny(errors, "[OrderedCombatPresentation] 연출 취소 중 정리에 실패했습니다.");
        }

        // 설계 C2: 앞의 모든 사건이 완료됐거나, 같은 묶음이면서 주체가 다르면 시작 후보다. 같은 유닛의 걸음은 앞 사건 완료를 기다리므로 차례로 돈다.
        private bool CanStart(int index)
        {
            for (int j = _first; j < index; j++)
            {
                // 경로 끝 연출(방향 복귀)을 기다리는 걸음 뒤로는 묶음과 관계없이 시작하지 않는다(지금 동작, C-1 검토 2).
                if (_states[j] == EventState.Ending)
                    return false;
                // 같은 묶음의 앞 사건은 아직 시작하지 않았어도 지나갈 수 있다(설계 C2 규칙 2, 계획 C-2에서 C-1 제한을 풂).
                // 예: 걸음 Gate에 막힌 이동자 피해 뒤의 이웃 피해는 접촉 순간, 접촉 밖 후속 뒤의 관통 대상은 화살이 닿는 순간 시작한다.
                if (_states[j] != EventState.Completed && !_groups.CanOverlap(j, index))
                    return false;
            }
            return true;
        }

        private void Start(int index, CombatEvent e)
        {
            _states[index] = EventState.Started;
            int generation = _generation;
            EventState[] states = _states;
            CombatEventRoute route = CombatEventTypeTable.RouteOf(e);
            if (route == CombatEventRoute.Step)
            {
                var c = (CombatCharacterEvent)e;
                // 걸음 사건 완료 통지는 상태만 바꾼다. 경로 끝 판단은 Tick 훑기가 한다(걸음 Tick 안에서 다른 연출을 시작하지 않음).
                _movement.BeginStep(c, () =>
                {
                    if (_generation == generation && ReferenceEquals(states, _states) && _states[index] == EventState.Started)
                        _states[index] = EventState.StepDone;
                });
            }
            else if (route == CombatEventRoute.Return)
            {
                _movement.ShowReturn((CombatCharacterEvent)e);
                if (_generation == generation && ReferenceEquals(states, _states) && _states[index] == EventState.Started)
                    _states[index] = EventState.Completed;
            }
            else
            {
                _eventPlayback.BeginEvent(e, () =>
                {
                    if (_generation == generation && ReferenceEquals(states, _states) && _states[index] == EventState.Started)
                        _states[index] = EventState.Completed;
                });
            }
        }

        // 사건 완료 통지를 받은 걸음을 마친다. 경로 끝 걸음이면 경로 끝 연출(방향 복귀)을 거쳐 완료한다.
        private void FinishStep(int index)
        {
            var step = (CombatCharacterEvent)_result.Events[index];
            if (!IsLastMovementStep(index, step))
            {
                _states[index] = EventState.Completed;
                return;
            }
            _states[index] = EventState.Ending;
            int generation = _generation;
            EventState[] states = _states;
            _eventPlayback.BeginMovementEnd(step, () =>
            {
                if (_generation == generation && ReferenceEquals(states, _states) && _states[index] == EventState.Ending)
                    _states[index] = EventState.Completed;
            });
        }

        /// <summary>
        /// 중간 표시 사건은 건너뛰되 새 행동(<see cref="CombatEventTypeTable.IsActionStart"/>: 공격·포효·소환·턴)·다른 대상 이동을 경로 경계로 삼습니다.
        /// 포효 뒤 같은 유닛의 넉백 이동을 앞 자발적 경로의 연속으로 보지 않습니다(보스 활성화 설계 D8).
        /// </summary>
        private bool IsLastMovementStep(int index, CombatCharacterEvent step)
        {
            for (int i = index + 1; i < _result.Events.Count; i++)
            {
                CombatEvent next = _result.Events[i];
                if (CombatEventTypeTable.IsActionStart(next))
                    break;
                if (next is CombatCharacterEvent character)
                {
                    if (character.Kind == CombatEventKind.Returned)
                        break;
                    if (character.Kind == CombatEventKind.Moved)
                        return character.Expected.EntityId != step.Expected.EntityId;
                }
            }
            return true;
        }
    }
}
