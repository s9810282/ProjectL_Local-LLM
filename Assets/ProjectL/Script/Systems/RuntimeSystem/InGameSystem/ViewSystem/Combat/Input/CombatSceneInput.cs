using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 씬 연결 한 번의 입력 객체(대기 관문·입력 기능 묶음·Router·덱 배치 입구·아이템 입구·환경 호버·거절 안내·배치 진입·행동 가용성·카메라 조작)와 그 정리 순서입니다
    /// (View 2라운드 W6-1, 배치 진입은 3라운드 W11-2, 카메라 조작은 카메라 조작 설계 CC1).
    /// 입력 Factory(<c>CombatSceneInputFactory.CreateSceneInput</c>)가 첫 그리기 성공 뒤 한 번에 만듭니다. 연결 상태(<c>CombatViewConnection</c>)가 이것 하나를 갖습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 수명이 둘입니다(계획 검증 H2). <see cref="Stop"/>은 가용성 조회를 정지하고 거절 안내·환경 호버·덱 배치·아이템을 정리한 뒤 관문을 비우고 묶음을 정지합니다.
    /// 관문·묶음·Router는 정지 뒤에도 남습니다. 입력 해제의 Unbind 도중 슬롯이 보내는 SelectCanceled를 Router·묶음이 받아 표현을 정리해야 하기 때문입니다(R2).
    /// 조정자가 입력 해제 뒤 연결 상태와 함께 이 객체를 놓습니다.
    /// </para>
    /// <para>
    /// 덱 호버 입구는 표시를 인수할 때 만들어 첫 그리기 전에도 있으므로 여기 없고 연결 상태가 갖습니다(계획 TP-6).
    /// 배치 진입(<see cref="Deployment"/>, 배치 확인창과 그 콜백)과 턴 종료(<see cref="EndTurn"/>, 턴 종료 확인창)는 연결 수명이라 여기 있습니다(3라운드 설계 P23). 닫기는 조정자가 지금과 같은 자리에서 부르고,
    /// <see cref="Stop"/>은 닫지 않습니다. 정지 뒤에도 남아, 정지 뒤의 닫기(분리)가 같은 객체를 닫습니다.
    /// HUD 아이템 아이콘 해제 연결은 생성이 모두 끝난 뒤 연결 상태가 걸고 풉니다(계획 검증 L11).
    /// </para>
    /// <para>
    /// 조정자는 입력 기능 묶음의 수명 단계(매 Tick·결과 재생 전·결과 후처리·확인 뒤)와 정보창 막기(<see cref="CanInspectOnPress"/>)를 이 묶음의 입구로 부르고,
    /// 입구는 묶음에 그대로 넘깁니다(평가 C8-01 1단계). 행동 버튼·가용성 맞춤과 입력 사실 읽기는 아직 <see cref="Interaction"/>을 직접 읽습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatSceneInput
    {
        private readonly CombatInputRouter _router;
        private CombatDeckPlacementInput _deckPlacement;
        private CombatItemInput _itemInput;
        // 정지 뒤에는 null입니다.
        private CombatEnvironmentHover _environmentHover;
        private CombatActionNotice _notices;
        // 카메라 조작 입력입니다(카메라 조작 설계 CC1). 정지 뒤에는 null입니다.
        private CombatCameraInput _camera;

        internal CombatSceneInput(CombatCommandGate gate, CombatInteraction interaction, CombatInputRouter router,
            CombatDeckPlacementInput deckPlacement, CombatItemInput itemInput, CombatEnvironmentHover environmentHover,
            CombatActionNotice notices, CombatDeploymentEntry deployment, CombatEndTurnEntry endTurn,
            CombatActionAvailabilityTracker actionAvailability, CombatCameraInput camera, CombatMovementUndoInput movementUndo)
        {
            MovementUndo = movementUndo ?? throw new ArgumentNullException(nameof(movementUndo));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            ActionAvailability = actionAvailability ?? throw new ArgumentNullException(nameof(actionAvailability));
            Deployment = deployment ?? throw new ArgumentNullException(nameof(deployment));
            EndTurn = endTurn ?? throw new ArgumentNullException(nameof(endTurn));
            Gate = gate ?? throw new ArgumentNullException(nameof(gate));
            Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _deckPlacement = deckPlacement ?? throw new ArgumentNullException(nameof(deckPlacement));
            _itemInput = itemInput ?? throw new ArgumentNullException(nameof(itemInput));
            _environmentHover = environmentHover ?? throw new ArgumentNullException(nameof(environmentHover));
            _notices = notices ?? throw new ArgumentNullException(nameof(notices));
        }

        /// <summary>시작/턴 종료 대기의 원본입니다. 조정자가 Resolve·HasPending으로 씁니다.</summary>
        internal CombatCommandGate Gate { get; }

        /// <summary>입력 기능 묶음입니다.</summary>
        internal CombatInteraction Interaction { get; }

        /// <summary>
        /// 선택 누름으로 정보창을 열어도 되는지입니다. 전경 입력 소유자(배치·아이템 Drag, 공격 모드, 경로 지정)가 포인터를 쓰는 동안은 거짓이라 기존 선택이 남습니다.
        /// 판정 원본은 입력 기능 묶음의 <see cref="CombatInteraction.IsInputOwnerActive"/> 하나라, 전경 소유자가 늘어도 따라갑니다(평가 C2-02·C8-01).
        /// </summary>
        internal bool CanInspectOnPress => !Interaction.IsInputOwnerActive;

        /// <summary>배치 국면의 전투 진입(배치 확인창)입니다. 정지 뒤에도 남습니다.</summary>
        internal CombatDeploymentEntry Deployment { get; }

        /// <summary>전투 국면의 턴 종료(턴 종료 확인창)입니다. 배치 진입과 같은 수명이고 정지 뒤에도 남습니다.</summary>
        internal CombatEndTurnEntry EndTurn { get; }

        /// <summary>전투 입력을 막는 확인창(배치·턴 종료)이 열려 있는지입니다.</summary>
        internal bool IsConfirmationOpen => Deployment.IsOpen || EndTurn.IsOpen;

        /// <summary>
        /// 열린 확인창을 모두 닫습니다. 조정자가 분리·입력 정지·입력 닫힘의 맨 앞에서 부릅니다.
        /// 하나가 실패해도 나머지를 닫고 모은 예외를 던집니다.
        /// </summary>
        internal void CloseConfirmations()
        {
            List<Exception> errors = null;
            try { Deployment.Close(); }
            catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            try { EndTurn.Close(); }
            catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            if (errors != null)
                throw new AggregateException("[CombatSceneInput] 확인창을 닫는 중 실패했습니다.", errors);
        }

        /// <summary>열린 확인창이 지금 사실로 더 열려 있으면 안 되면 닫습니다. 행동 버튼을 맞출 때 부릅니다.</summary>
        internal void SyncConfirmations()
        {
            Deployment.Sync();
            EndTurn.Sync();
        }

        /// <summary>선택 아군의 Core 가용성 조회입니다. 이 연결 입력이 소유하고 정지합니다.</summary>
        internal CombatActionAvailabilityTracker ActionAvailability { get; }

        internal CombatMovementUndoInput MovementUndo { get; }

        /// <summary>새 결과/닫힘에서 가용성 조회를 지웁니다. 명령 Buffer는 건드리지 않습니다.</summary>
        internal void ClearActionAvailability() => ActionAvailability.Clear();

        /// <summary>손패 카드 입구입니다. 정지 뒤에는 null입니다.</summary>
        internal ICombatDeckPlacementInput DeckPlacement => _deckPlacement;

        /// <summary>가방 아이템 입구입니다. 정지 뒤에는 null입니다.</summary>
        internal ICombatItemInput ItemInput => _itemInput;

        /// <summary>
        /// 입구 검사를 통과한 입력 하나를 Router로 나누고 환경 호버를 맞춥니다(옛 조정자 <c>RouteInput</c>의 Router·환경 호버 부분).
        /// 포커스 상실·선택 강제 종료면 환경 설명을 지우고, 포인터 이동이면 그 위치를 넘깁니다.
        /// 카메라 입력(<see cref="CombatInputRouter.IsCameraRequest"/>)은 Router로 보내지 않고 카메라 입력에 상태로만 넘기며,
        /// 포인터 이동은 가장자리 이동용으로, 포커스 상실은 카메라 입력 비우기로도 넘깁니다(카메라 조작 설계 CC2).
        /// </summary>
        internal void Route(CombatInputRouter.Request request, float screenX, float screenY, IReadOnlyList<CharacterData> characters)
        {
            if (request == CombatInputRouter.Request.CameraPan)
            {
                _camera?.PanChanged(screenX, screenY);
                return;
            }
            if (request == CombatInputRouter.Request.CameraZoom)
            {
                _camera?.Scrolled(screenX);
                return;
            }
            bool canSelectEnvironment = !Interaction.IsInputOwnerActive;
            _router.Route(request, screenX, screenY, characters);
            if (request == CombatInputRouter.Request.FocusLost || request == CombatInputRouter.Request.SelectCanceled)
            {
                _environmentHover?.Clear();
                if (request == CombatInputRouter.Request.FocusLost)
                    _camera?.Clear();
            }
            else if (request == CombatInputRouter.Request.PointerMoved)
            {
                _environmentHover?.PointerMoved(screenX, screenY);
                _camera?.PointerMoved(screenX, screenY);
            }
            else if (request == CombatInputRouter.Request.SelectPressed)
            {
                _environmentHover?.SelectPressed(screenX, screenY, characters,
                    canSelectEnvironment && !Interaction.IsInputOwnerActive);
            }
            else if (request != CombatInputRouter.Request.SelectReleased)
            {
                _environmentHover?.CancelSelection();
            }
        }

        /// <summary>
        /// 카메라 입력을 이번 프레임 한 번 진행합니다(카메라 조작 설계 CC6). 움직여도 되는지는 정책 한 곳이 정합니다(<see cref="CombatInputPolicy.CanMoveCamera"/>).
        /// 시점이 바뀌었으면 마지막 포인터 위치로 포인터 이동을 Router에 다시 보냅니다. 마우스가 그대로여도 그 아래 칸이 바뀌기 때문입니다(평가 C10-01).
        /// 환경 호버는 Tick마다 스스로 다시 집으므로 따로 보내지 않습니다. 정지 뒤에는 아무것도 하지 않습니다.
        /// </summary>
        internal void TickCamera(in CombatInputFacts facts, float deltaTime, IReadOnlyList<CharacterData> characters)
        {
            if (_camera == null)
                return;
            if (_camera.Tick(CombatInputPolicy.CanMoveCamera(facts), deltaTime)
                && _camera.TryGetPointer(out float screenX, out float screenY))
                _router.RefreshPointer(screenX, screenY, characters);
        }

        /// <summary>환경 설명을 지금 사실로 맞춥니다. 허용 여부는 조정자가 입력 사실로 정합니다. 정지 뒤에는 아무것도 하지 않습니다.</summary>
        internal void SyncEnvironmentHover(CombatStateData current, int resultSequence, bool allowed) =>
            _environmentHover?.Refresh(current, resultSequence, allowed, !Interaction.IsInputOwnerActive);

        /// <summary>환경 호버가 있는지(정지 전인지)입니다. 없으면 조정자가 허용 사실을 읽지 않습니다.</summary>
        internal bool HasEnvironmentHover => _environmentHover != null;

        /// <summary>
        /// 입력 기능 묶음의 매 Tick 입구입니다(감시 단계 뒤, 새 응답이면 응답 단계). <see cref="CombatInteraction.Tick"/>에 그대로 넘깁니다(평가 C8-01).
        /// </summary>
        internal void TickInteraction(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CombatPreviewResult latestPreview,
            bool answered, Func<CombatInputFacts> readAnswerFacts) =>
            Interaction.Tick(facts, characters, latestPreview, answered, readAnswerFacts);

        /// <summary>결과 재생 전입니다. <see cref="CombatInteraction.BeforeResult"/>에 그대로 넘깁니다(평가 C8-01).</summary>
        internal void BeforeResult() => Interaction.BeforeResult();

        /// <summary>
        /// 표시가 끝난 결과를 받습니다. 거절 사유를 안내하고(정지 뒤에는 안내 없음), 입력 기능 묶음에 명령 결과를 알립니다
        /// (그 공격 모드가 낸 공격이 수락됐으면 이어지는 후처리에서 공격 모드가 끝남).
        /// </summary>
        internal void OnResult(CombatResult result)
        {
            _notices?.OnResult(result);
            Interaction.CommandResolved(result);
        }

        /// <summary>결과 후처리(확인 전)입니다. <see cref="CombatInteraction.AfterResult"/>에 그대로 넘깁니다(평가 C8-01).</summary>
        internal void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
            Interaction.AfterResult(facts, characters);

        /// <summary>결과 확인 뒤입니다. <see cref="CombatInteraction.AfterAcknowledged"/>에 그대로 넘깁니다(평가 C8-01).</summary>
        internal void AfterAcknowledged() => Interaction.AfterAcknowledged();

        /// <summary>가용성 조회·환경 설명을 지우고 입력 기능 묶음의 입력(경로 포함)을 정리합니다. 대기·명령 Buffer는 보존합니다.</summary>
        internal void CancelPresentation()
        {
            ClearActionAvailability();
            _environmentHover?.Clear();
            Interaction.CancelInputs();
        }

        /// <summary>
        /// 가용성 조회를 정지하고 거절 안내·환경 호버·덱 배치·아이템을 정리한 뒤 대기를 끝내고 입력 기능 묶음을 정지합니다.
        /// 실패는 <paramref name="errors"/>에 모으고 나머지를 이어 갑니다. 관문·묶음·Router는 남깁니다(클래스 설명). 명령 Buffer는 보존합니다.
        /// </summary>
        internal void Stop(ref List<Exception> errors)
        {
            CleanupErrors.Try(ActionAvailability.Stop, ref errors);
            _notices = null;
            if (_environmentHover != null) CleanupErrors.Try(_environmentHover.Clear, ref errors);
            _environmentHover = null;
            _deckPlacement?.Release();
            _deckPlacement = null;
            _itemInput?.Release();
            _itemInput = null;
            // 카메라는 그 자리에 둡니다. 처음 위치·크기로 되돌리는 것은 표시 해제입니다(카메라 조작 계획 P6).
            _camera?.Clear();
            _camera = null;
            // 로컬 대기는 표시 정리와 별개로 항상 끝낸다. 입력 닫힘(제출 정지·포기 요청)은 여기를 거치지 않으므로 대기를 보존한다.
            Gate.Clear();
            // 입력 기능 묶음이 경로 입력 연결을 먼저 끊고 참조를 놓은 뒤 배치·공격을 정리한다. 정지 뒤에는 경로를 다시 부르지 않는다(SV4).
            CleanupErrors.Try(Interaction.Stop, ref errors);
        }
    }
}
