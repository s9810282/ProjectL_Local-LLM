using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Railway;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback
{
    /// <summary>
    /// 전투 씬에서 결과 하나의 사건 재생을 조정합니다(구조 리팩토링 설계 D5). 기차 발동은 기차 접촉 재생에, 그 밖의 사건은
    /// 단계 전환은 전용 Presenter에, 그 밖의 사건은 사건 Presenter에 넘기며 실패 정리 버전을 소유합니다.
    /// </summary>
    /// <remarks>
    /// <c>CombatSceneDisplayFactory</c>가 조립하고, 재생 계약(<see cref="ICombatEventPlayback"/>)의 구현으로 순차 연출에 직접 연결됩니다.
    /// 표시는 <see cref="ICombatBattleDisplay.EventPlayback"/>으로 이 객체를 넘기기만 하고 위임하지 않습니다(연출 조립 설계 §9 I-4, 결정 Q-R16 A).
    /// 사건 Presenter·레일 표시의 필드 연결(Bind·Install)과 해제(Clear·Release)는 표시가 정하며, 이 객체는 참조만 받습니다.
    /// 씬 연결 해제 뒤 사건 시작은 이 객체가 예외로 거절합니다(리팩토링 설계 D9, 표시가 <see cref="MarkReleased"/>로 알림).
    /// 판단·Tick·취소는 해제 뒤에도 거절하지 않으며, 진행 중인 사건이 없으면 아무것도 하지 않습니다.
    /// </remarks>
    internal sealed class CombatSceneEventPlayback : ICombatEventPlayback
    {
        private readonly CombatSceneEventPresenter _events;
        private readonly FieldRailwayPresenter _railway;
        private readonly FieldView _field;
        private readonly FieldThornPresenter _thorns;
        private readonly CombatRailwayContactPlayback _railwayContact;
        private readonly CombatPhaseTransitionPresenter _phaseTransition;
        // 기차가 필드를 지나는 동안의 카메라 연출(흔들림·줌)입니다(카메라 연출 조각 설계 K9·K10). 늘 있고, 낼 값이 없으면 진입·이탈 알림을 받지 않습니다.
        private readonly CombatTrainCamera _trainCamera;
        // 실패 정리용 버전입니다. 기차 접촉 취소·결과 준비·사건 시작마다 올립니다. 재진입한 취소 뒤 이전 실패 정리가 새 재생을 지우지 않게 합니다.
        // Tick 중 재진입한 취소 뒤 레일 Tick을 건너뛰는 판정에도 씁니다. 사건 Presenter의 재생 세대(공격·걸음·반응 완료 무효화)와 범위가 다른 별도 카운터입니다(구조 개선 SV3).
        private int _cleanupVersion;
        private bool _released;

        /// <param name="characterBody">사망 직전 캐릭터 사실에서 기차 접촉 몸을 만드는 변환입니다(히트 타이밍 설계 H7). 표시 Presenter 전체는 받지 않습니다.</param>
        /// <param name="phaseTransition">국면 사건(전투 진입·밤 끝·승리)의 화면 전환입니다. 필수이고, 시험 조립은 바로 끝나는 가짜 전환을 넘깁니다(View 2라운드 Q-V2g-1).</param>
        /// <param name="camera">맵 카메라 창구입니다(사건 Presenter와 같은 것). 필수입니다. 맵 카메라가 없으면 창구가 줌·흔들림을 내지 않습니다.</param>
        /// <param name="cameraMotion">카메라 연출 값입니다. 필수입니다. 기차 흔들림·줌 값이 없으면 기차에 카메라 연출을 넣지 않습니다.</param>
        internal CombatSceneEventPlayback(
            CombatSceneEventPresenter events,
            FieldRailwayPresenter railway,
            FieldView field,
            FieldThornPresenter thorns,
            Func<CharacterCombatFacts, FieldGridMapper, CombatBody> characterBody,
            CombatPhaseTransitionPresenter phaseTransition,
            CombatCameraWindow camera,
            CombatCameraMotionSettings cameraMotion)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _railway = railway ?? throw new ArgumentNullException(nameof(railway));
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _thorns = thorns ?? throw new ArgumentNullException(nameof(thorns));
            _phaseTransition = phaseTransition ?? throw new ArgumentNullException(nameof(phaseTransition));
            _trainCamera = new CombatTrainCamera(camera ?? throw new ArgumentNullException(nameof(camera)),
                cameraMotion ?? throw new ArgumentNullException(nameof(cameraMotion)));
            // 기차 카메라 값이 없으면 진입·이탈 알림을 받지 않습니다(필드 구간도 계산하지 않음).
            bool trainCues = _trainCamera.HasMotion;
            _railwayContact = new CombatRailwayContactPlayback(_events.BeginConcurrentDeath, _railway.Begin,
                characterBody ?? throw new ArgumentNullException(nameof(characterBody)),
                trainCues ? _trainCamera.Entered : null, trainCues ? _trainCamera.Left : null);
        }

        public bool IsPresentationComplete => _railwayContact.IsComplete && _events.IsPresentationComplete &&
            !_phaseTransition.IsPlaying;

        public bool CanBeginPresentationEvent(CombatEvent item) => !_railwayContact.IsPlaying &&
            !_phaseTransition.IsPlaying &&
            (CombatEventTypeTable.IsRailway(item) ? _events.IsPresentationComplete : _events.CanBeginPresentationEvent(item));

        /// <summary>순차 재생기의 후속 정리 질문을 사건 Presenter에 넘깁니다(유닛 행동 계획 TP-13).</summary>
        public void BindSettlement(ICombatSourceSettlement settlement) => _events.BindSettlement(settlement);

        public void BeginPresentation(CombatResult result)
        {
            ThrowIfReleased();
            if (!_railwayContact.IsComplete || _phaseTransition.IsPlaying || !_events.CanBeginResult)
                throw new InvalidOperationException("이전 결과의 표시가 남아 있습니다.");
            ++_cleanupVersion;
            _events.BeginPresentation(result);
            _railwayContact.Prepare(result, _railway.Paths, _field.Mapper);
        }

        public void BeginStep(CombatCharacterEvent step, Action completed)
        {
            ThrowIfReleased();
            RequireNoActiveRailway();
            _events.BeginStep(step, completed);
        }

        public void BeginMovement(CombatCharacterEvent firstStep)
        {
            ThrowIfReleased();
            _events.BeginMovement(firstStep);
        }

        public void BeginMovementEnd(CombatCharacterEvent lastStep, Action completed)
        {
            ThrowIfReleased();
            RequireNoActiveRailway();
            _events.BeginMovementEnd(lastStep, completed);
        }

        public void ShowReturn(CombatCharacterEvent returned)
        {
            ThrowIfReleased();
            RequireNoActiveRailway();
            _events.ShowReturn(returned);
        }

        public void BeginEvent(CombatEvent e, Action completed)
        {
            ThrowIfReleased();
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (!CanBeginPresentationEvent(e)) throw new InvalidOperationException("이전 사건의 표시가 남아 있습니다.");
            int version = ++_cleanupVersion;
            if (_railwayContact.TryConsumeDeath(e))
            {
                completed();
                return;
            }
            try
            {
                if (_phaseTransition.TryBegin(e, completed))
                    return;
            }
            catch (Exception error)
            {
                CleanupFailedPresentation(version, error);
                throw;
            }
            if (!CombatEventTypeTable.IsRailway(e))
            {
                _events.BeginEvent(e, completed);
                return;
            }
            try
            {
                // 기차 사건은 일반 Presenter를 우회하므로 호출 시점의 소리를 여기서 한 번 냅니다.
                _events.PlayEventSound(e);
                if (version != _cleanupVersion) return;
                // 기차 카메라 정리(남은 줌 되돌림) 뒤에 완료를 알립니다. 완료 콜백 안에서 다음 사건이 줌을 빌릴 수 있게 합니다.
                _railwayContact.Begin((CombatGimmickEvent)e, () =>
                {
                    _trainCamera.End();
                    completed();
                });
            }
            catch (Exception error)
            {
                CleanupFailedPresentation(version, error);
                throw;
            }
        }

        public void TickPresentation(float deltaTime)
        {
            int version = _cleanupVersion;
            bool railwayPlaying = _railwayContact.IsPlaying;
            bool transitionPlaying = _phaseTransition.IsPlaying;
            try
            {
                // 기존 반응을 먼저 진행한 뒤 이번 프레임 접촉을 시작합니다.
                _events.TickPresentation(deltaTime);
                // 기차 카메라는 레일 Tick 앞입니다. 이번 프레임 레일 Tick에서 시작한 줌 복귀가 같은 프레임 시간을 받지 않게 합니다(줌 조각과 같은 규칙).
                if (version == _cleanupVersion) _trainCamera.Tick(deltaTime);
                if (version == _cleanupVersion) _railway.Tick(deltaTime);
                if (version == _cleanupVersion) _phaseTransition.Tick();
            }
            catch (Exception error)
            {
                if (railwayPlaying || transitionPlaying) CleanupFailedPresentation(version, error);
                throw;
            }
        }

        public void CancelMovement() => RunAll(CancelRailwayContacts, _events.CancelMovement, _railway.Cancel);

        // 화약통·지뢰 폭발은 순간 큐라 사건 Presenter의 취소(이펙트 창구 회수)가 함께 회수합니다(이펙트 창구 확장 설계 EC9).
        public void CancelPresentation() => RunAll(CancelPendingEvents, _events.CancelPresentation, _railway.Cancel, _thorns.Clear);

        /// <summary>표시 재연결·해제 시 진행 중인 사건 자원을 모두 반환합니다.</summary>
        internal void CancelPendingEvents() => RunAll(CancelRailwayContacts, CancelPhaseTransition);

        private void CancelPhaseTransition() => _phaseTransition.Cancel();

        /// <summary>
        /// 진행 중인 기차 접촉 묶음을 버리고 실패 정리 버전을 올립니다. 표시 재연결·해제는 CancelPendingEvents를 통해 부릅니다.
        /// </summary>
        internal void CancelRailwayContacts()
        {
            ++_cleanupVersion;
            _railwayContact.Cancel();
            // 기차 줌이 남아 있으면 기준값으로 바로 되돌립니다(설계 K10).
            _trainCamera.End();
        }

        /// <summary>
        /// 씬 연결이 해제됐음을 표시합니다. 그 뒤 사건 시작은 예외입니다(리팩토링 설계 D9). 되돌리지 않습니다.
        /// 자원 정리(기차 접촉 취소 등)는 하지 않으며, 그것은 표시의 해제 목록이 합니다.
        /// </summary>
        internal void MarkReleased() => _released = true;

        private void ThrowIfReleased()
        {
            if (_released)
                throw new InvalidOperationException("[CombatSceneEventPlayback] 씬 연결이 해제된 재생입니다.");
        }

        private void RequireNoActiveRailway()
        {
            if (_railwayContact.IsPlaying) throw new InvalidOperationException("기차 접촉 묶음을 먼저 완료해야 합니다.");
        }

        private void CleanupFailedPresentation(int version, Exception error)
        {
            if (version != _cleanupVersion) return;
            try
            {
                CancelPresentation();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(error, cleanup);
            }
        }

        private static void RunAll(params Action[] actions) =>
            CleanupErrors.RunAll("[CombatSceneEventPlayback] 재생 정리 중 실패했습니다.", actions);
    }
}
