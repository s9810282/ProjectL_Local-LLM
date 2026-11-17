using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 전투 입력 의도를 처리 순서대로 대상에 나눕니다(구조 개선 VR2, 설계 VR2 표).
    /// 포인터 입력의 대상은 <see cref="ResolvePointerTarget"/> 한 곳에서 정하고, 시작·턴 종료는 Gate로 직접 제출합니다.
    /// </summary>
    /// <remarks>
    /// 모드·대기·초안을 소유하지 않고 정책을 계산하지 않습니다. 사실은 판단이 필요할 때마다 공급자에게서 새로 읽으며,
    /// 외부 표시 콜백(경로 취소 등) 뒤에는 다시 읽습니다. 오류 경계·입력 연결 검사는 입력 수신 창구가 먼저 합니다.
    /// 입력 기능(배치·공격·경로)은 입력 기능 묶음(<see cref="CombatInteraction"/>)을 통해서만 부릅니다(구조 개선 SV4).
    /// 커서 스냅·UI 가림은 같은 씬 연결 수명이라 생성 인자로 받습니다. 입력마다 새 대리자를 만들지 않습니다.
    /// </remarks>
    internal sealed class CombatInputRouter
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInputPointerRefreshPoll = new("Combat.Action.Input.PointerRefresh.Poll");
#endif

        /// <summary>외부 입력 콜백의 종류입니다. 콜백마다 클로저를 만들지 않으려고 한 입구로 모읍니다.</summary>
        internal enum Request
        {
            PointerMoved,
            SelectPressed,
            SelectReleased,
            SelectCanceled,
            Cancel,
            EnterCombat,
            EndTurn,
            AttackMode,
            FocusLost,
            Return,
            /// <summary>공격 버튼 전용(행동 버튼 설계 A2): 공격 모드면 끝내고, 아니면 <see cref="AttackMode"/>와 같습니다.</summary>
            AttackToggle,
            /// <summary>카메라 이동 키 방향(x·y)입니다. 이 Router가 아니라 카메라 입력이 받습니다(<see cref="IsCameraRequest"/>).</summary>
            CameraPan,
            /// <summary>휠 값(x)입니다. 이 Router가 아니라 카메라 입력이 받습니다(<see cref="IsCameraRequest"/>).</summary>
            CameraZoom,
            /// <summary>
            /// 회전 키 값(x, −1 = Z·+1 = C)입니다. 이 Router가 아니라 카메라 입력이 받습니다(<see cref="IsCameraRequest"/>, 카메라 회전 설계 CR5).
            /// 기존 값의 번호가 바뀌지 않게 맨 끝에 둡니다(회전 계획 TP-10).
            /// </summary>
            CameraRotate,
            /// <summary>
            /// 가운데 버튼 끌기 회전의 누름(x = 1)·뗌(x = 0)입니다. 이 Router가 아니라 카메라 입력이 받습니다(<see cref="IsCameraRequest"/>, 카메라 회전 설계 CR8).
            /// 기존 값의 번호가 바뀌지 않게 맨 끝에 둡니다.
            /// </summary>
            CameraRotateDrag,
            /// <summary>병종 순환은 유닛 선택을 소유한 씬 입력 분배에서 처리합니다.</summary>
            CycleUnitCategory,
            /// <summary>
            /// 플레이어 액티브 스킬 모드 진입·끝입니다(R 키·행동 패널 스킬 버튼, 스킬 View 설계 SK1·SK6). 키와 버튼이 같은 입구 하나입니다.
            /// 기존 값의 번호가 바뀌지 않게 맨 끝에 둡니다.
            /// </summary>
            Skill
        }

        /// <summary>
        /// 카메라 조작 입력인지입니다(카메라 조작 설계 CC2). 이 판정 하나를 수신 창구(확인창 막기 통과, 계획 P5)·조정자(가벼운 경로)·
        /// 연결 입력 묶음(카메라 입력으로 보냄)이 함께 씁니다. 카메라 입력은 <see cref="Route"/>로 오지 않습니다.
        /// </summary>
        internal static bool IsCameraRequest(Request request) =>
            request == Request.CameraPan || request == Request.CameraZoom || request == Request.CameraRotate
            || request == Request.CameraRotateDrag;

        /// <summary>포인터 입력을 받을 소유자입니다.</summary>
        internal enum PointerTarget
        {
            None = 0,
            Placement = 1,
            Attack = 2,
            Path = 3,
            /// <summary>배치 Layer에서 가방 아이템을 끄는 중입니다(가방 설계 P4).</summary>
            Item = 4,
            /// <summary>전투 Layer에서 플레이어 액티브 스킬 모드입니다(전투 스킬 View 설계 SK1).</summary>
            Skill = 5
        }

        private readonly CombatInteraction _interaction;
        private readonly CombatCommandGate _gate;
        private readonly CombatMovementUndoInput _movementUndo;
        // 인자는 선행 미처리 명령까지 읽을지 여부다(턴 종료 판단).
        private readonly Func<bool, CombatInputFacts> _readFacts;
        // 커서 그리드 스냅(커서 그리드 스냅 설계 C1)과 UI 가림 판정(행동 버튼 설계 B5)입니다. 필수입니다(평가 C5-02).
        private readonly CombatCursorSnap _cursorSnap;
        private readonly ICombatPointerOcclusion _occlusion;
        // 실제로 막은 입력 시도만 알린다. 상태 조회·호버·최종 Gate 검사는 알리지 않는다.
        private readonly Action _reportBlockedInput;

        /// <param name="cursorSnap">커서 그리드 스냅입니다. 스냅할지는 입력 기능 묶음의 스냅 모드가 정합니다.</param>
        /// <param name="occlusion">UI 가림 판정입니다.</param>
        /// <param name="reportBlockedInput">튜토리얼 제한이 실제로 막은 입력 시도 하나를 알리는 입구입니다.</param>
        /// <param name="movementUndo">이동 되돌리기(복귀) 입구입니다.</param>
        internal CombatInputRouter(CombatInteraction interaction, CombatCommandGate gate, Func<bool, CombatInputFacts> readFacts,
            CombatCursorSnap cursorSnap, ICombatPointerOcclusion occlusion, Action reportBlockedInput, CombatMovementUndoInput movementUndo)
        {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _cursorSnap = cursorSnap ?? throw new ArgumentNullException(nameof(cursorSnap));
            _occlusion = occlusion ?? throw new ArgumentNullException(nameof(occlusion));
            _reportBlockedInput = reportBlockedInput ?? throw new ArgumentNullException(nameof(reportBlockedInput));
            _movementUndo = movementUndo ?? throw new ArgumentNullException(nameof(movementUndo));
        }

        /// <summary>
        /// 포인터 입력 대상의 유일한 판정식입니다. 실제 Layer를 먼저 보고, 배치는 아이템을 끄는 중이면 Item 아니면 Placement,
        /// 전투는 스킬 모드면 Skill, 공격 모드면 Attack, 아니면 Path입니다(모드가 없어도 전투 포인터는 경로 입력, 두 모드는 전경이 배타). 그 밖의 Layer는 None입니다.
        /// </summary>
        /// <remarks>
        /// 실시간 상태(공격 모드·아이템 드래그·스킬 모드)를 넘기는 곳은 입력 기능 묶음의 <see cref="CombatInteraction.ResolvePointerTarget"/> 하나입니다(평가 C4-13).
        /// Router와 묶음의 감시 단계는 그것을 부르고, 사실만 받는 곳(경로 입력·환경 호버)은 입력 사실 읽기가 같은 함수로 채운
        /// <see cref="CombatInputFacts.PointerTarget"/>을 읽습니다. 연결 입력이 없을 때만 사실 읽기가 이 판정식에 셋 다 거짓을 넘깁니다.
        /// 아이템 인자에 기본값을 두지 않습니다(View 2라운드 W5-2).
        /// </remarks>
        internal static PointerTarget ResolvePointerTarget(InputLayer layer, bool attackActive, bool itemDragActive, bool skillActive)
        {
            if (layer == StageInputLayers.Deployment)
                return itemDragActive ? PointerTarget.Item : PointerTarget.Placement;
            if (layer == StageInputLayers.Combat)
                return skillActive ? PointerTarget.Skill : attackActive ? PointerTarget.Attack : PointerTarget.Path;

            return PointerTarget.None;
        }

        /// <summary>입력 하나를 나눕니다. 카메라 요청(<see cref="IsCameraRequest"/>)은 오지 않습니다.</summary>
        /// <param name="cameraDragging">
        /// 가운데 버튼 끌기 회전 중인지입니다(카메라 회전 설계 CR8 검토 M2). 참이면 포인터 이동에도 커서 스냅을 하지 않습니다. 스냅이 옮긴 커서가
        /// 다음 포인터 이동으로 와서 끈 양에 섞이고 커서도 튀기 때문입니다. 원본은 카메라 입력의 끄는 상태이고, 연결 입력 묶음이 넘깁니다.
        /// </param>
        internal void Route(Request request, float screenX, float screenY, IReadOnlyList<CharacterData> characters,
            bool cameraDragging = false)
        {
            switch (request)
            {
                case Request.FocusLost:
                    // 경로 취소 → 배치·공격 표현 정리.
                    _interaction.CancelInputs();
                    return;
                case Request.SelectCanceled:
                    // 표현만 정리한다(경로 먼저). 명령을 기록하지 않으며 대기·종료 중에도 가능하다.
                    _interaction.SelectCanceled();
                    return;
                case Request.Cancel:
                {
                    // 스킬 모드의 우클릭은 마지막 점 빼기(점이 없으면 모드 끝)이고 경로는 건드리지 않는다(전투 스킬 View 설계 SK1).
                    if (_interaction.IsSkillActive)
                    {
                        _interaction.SkillBack(_readFacts(false), characters);
                        return;
                    }
                    // 가방 아이템을 끄는 중이면 그것만 끝낸다(회수로 가지 않음, 가방 설계 P4).
                    if (_interaction.IsItemDragActive)
                    {
                        _interaction.CancelItemSelection();
                        return;
                    }
                    // 배치 단계 우클릭은 Drag 중이 아니면 포인터 아래 필드 유닛을 손패로 되돌린다. 대상이 없으면 기존 취소로 간다.
                    // 포인터가 UI(HUD 아이콘 등) 위면 되돌리지 않는다. 그 우클릭은 UI가 받는다(가방 설계 P6).
                    CombatInputFacts facts = _readFacts(false);
                    if (_interaction.ResolvePointerTarget(facts.CurrentLayer) == PointerTarget.Placement
                        && !IsLastPlacementPointerOverUi()
                        && _interaction.TryReturnPlacedUnit(facts, characters))
                        return;
                    // Drag 또는 공격 모드를 끝낸 뒤 경로를 취소한다(경로는 견적을 다시 요청).
                    _interaction.Cancel(characters);
                    return;
                }
                case Request.Return:
                    if (!RejectTutorialCommand(CombatViewCommandKind.UndoMove, includeUnhandledCommand: true))
                        _movementUndo.Request();
                    return;
                case Request.Skill:
                    // R·스킬 버튼: 스킬 모드면 끝내고, 아니면 최신 사실로 진입을 시도한다(전투 스킬 View 설계 SK1·SK6).
                    // 경로 취소는 들어갈 수 있을 때만 한다 — 메인을 고르지 않은 R(흔한 진입 실패)이 다른 유닛의 경로 초안을 지우지 않게(계획 §6.6, 묶음 끝 검토 L1).
                    if (_interaction.IsSkillActive)
                    {
                        _interaction.CancelSkill(characters);
                        return;
                    }
                    if (RejectTutorialSkillMode())
                        return;
                    if (_interaction.CanEnterSkill(_readFacts(false)))
                        _interaction.LeaveMoveMode();
                    _interaction.SkillModeRequested(_readFacts(false));
                    return;
                case Request.EnterCombat:
                    if (RejectTutorialCommand(CombatViewCommandKind.EnterCombat))
                        return;
                    // 성공했을 때만 대기 기록 뒤 Interaction 표시를 정리한다. 경로 취소는 하지 않는다.
                    if (_gate.SubmitPending(_readFacts(false), CombatViewCommandData.EnterCombat()))
                        _interaction.CancelPresentation();
                    return;
                case Request.EndTurn:
                    if (RejectTutorialCommand(CombatViewCommandKind.EndPlayerTurn, includeUnhandledCommand: true))
                        return;
                    // 경로를 먼저 취소한다. 제출이 거절돼도 취소는 이미 끝났다. 취소가 표시 콜백을 부르므로 사실은 그 뒤에 읽는다.
                    _interaction.LeaveMoveMode();
                    var endFacts = _readFacts(true);
                    if (_gate.SubmitPending(endFacts, CombatViewCommandData.EndPlayerTurn(
                        endFacts.BattleInstanceId, endFacts.ControllerTurnSequence)))
                        _interaction.CancelPresentation();
                    return;
                case Request.AttackToggle:
                    // 공격 버튼: 공격 모드 중이면 모드만 끝낸다(공격 모드 중에는 경로가 없으므로 경로는 건드리지 않는다).
                    if (_interaction.IsAttackActive)
                    {
                        _interaction.CancelDragOrAttack(characters);
                        return;
                    }
                    if (RejectTutorialAttackMode())
                        return;
                    EnterAttackMode();
                    return;
                case Request.AttackMode:
                    if (RejectTutorialAttackMode())
                        return;
                    EnterAttackMode();
                    return;
                case Request.PointerMoved:
                case Request.SelectPressed:
                case Request.SelectReleased:
                    RoutePointer(request, screenX, screenY, characters);
                    // 소유자가 이 위치로 경로 샘플링·호버를 먼저 처리한 뒤, 스냅 모드면 칸이 바뀐 경우에만 커서를 칸 중심으로 옮긴다.
                    // 가운데 버튼 끌기 회전 중에는 옮기지 않는다(스냅 안 함 = 기억한 칸도 비움 → 뗀 뒤 첫 이동에서 다시 스냅).
                    if (request == Request.PointerMoved)
                        _cursorSnap.PointerMoved(_interaction.IsCursorSnapMode && !cameraDragging, screenX, screenY);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request), request, null);
            }
        }

        /// <summary>
        /// 카메라 시점이 바뀐 Tick에 마지막 포인터 위치로 포인터 이동을 한 번 다시 보냅니다(평가 C10-01). 마우스는 그대로인데 그 아래 칸이
        /// 바뀌었으므로, 호버·고스트·미리보기·경로가 지금 포인터 아래 칸을 다시 집습니다.
        /// </summary>
        /// <remarks>
        /// 실제 포인터 이동과 같은 판정식·소유자로 보내되 두 가지는 하지 않습니다. 커서 스냅은 시점이 움직이는 동안 커서가 튀지 않도록 하지 않고,
        /// 공격 Drag 후보의 이동 Drag 승격은 마우스를 움직이지 않았으므로 하지 않습니다. 포인터 이동의 UI 가림은 그 승격에만 쓰므로 묻지 않습니다.
        /// </remarks>
        internal void RefreshPointer(float screenX, float screenY, IReadOnlyList<CharacterData> characters)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInputPointerRefreshPollSample = _profileInputPointerRefreshPoll.Auto();
#endif

            CombatInputFacts facts = _readFacts(false);
            PointerTarget target = _interaction.ResolvePointerTarget(facts.CurrentLayer);
            _interaction.PointerMoved(target, facts, characters, screenX, screenY);
        }

        // 제한은 모드 종료·경로 취소보다 먼저 확인한다. 막힌 시도 하나는 이 Router에서 한 번만 보고한다.
        private bool RejectTutorialCommand(CombatViewCommandKind kind, bool includeUnhandledCommand = false)
        {
            if (CombatInputPolicy.AllowsTutorialCommand(_readFacts(includeUnhandledCommand), kind))
                return false;

            _reportBlockedInput();
            return true;
        }

        private bool RejectTutorialAttackMode()
        {
            if (CombatInputPolicy.AllowsTutorialPreview(_readFacts(false), CombatViewPreviewKind.AttackRange))
                return false;

            _reportBlockedInput();
            return true;
        }

        // 튜토리얼 허용표에는 스킬 조회가 없어 제한 중에는 스킬 모드가 막힌다(전투 스킬 View 설계 P9).
        private bool RejectTutorialSkillMode()
        {
            if (CombatInputPolicy.AllowsTutorialPreview(_readFacts(false), CombatViewPreviewKind.SkillAvailability))
                return false;

            _reportBlockedInput();
            return true;
        }

        private void EnterAttackMode()
        {
            // E: 경로를 취소한 뒤 최신 사실로 공격 모드 진입을 시도한다.
            _interaction.LeaveMoveMode();
            _interaction.AttackModeRequested(_readFacts(false));
        }

        // 취소 입력에는 좌표가 없어 배치 입력이 기억한 마지막 포인터로 봅니다. 포인터를 받은 적이 없으면 false입니다.
        private bool IsLastPlacementPointerOverUi() =>
            _interaction.TryGetLastPlacementPointer(out float screenX, out float screenY)
            && _occlusion.IsPointerOverUi(screenX, screenY);

        private void RoutePointer(Request request, float screenX, float screenY, IReadOnlyList<CharacterData> characters)
        {
            CombatInputFacts facts = _readFacts(false);
            PointerTarget target = _interaction.ResolvePointerTarget(facts.CurrentLayer);

            // 버튼을 누른 같은 마우스 떼기가 버튼 뒤 칸의 공격으로 쓰이지 않게 한다(B5).
            // 버튼 클릭과 Select 액션의 처리 순서는 정해져 있지 않으므로 공격 모드 여부와 관계없이 막는다. 경로 대상은 보지 않는다.
            bool blockedByUi = _occlusion.IsPointerOverUi(screenX, screenY);
            // 이미 켜진 공격 모드의 실제 클릭도 안내한다. 호버와 이동 Drag 승격은 기존 기능이 처리한다.
            // helper보다 먼저 반환해 막힌 release가 제스처·모드를 정리하지 않게 한다.
            if (target == PointerTarget.Attack && request == Request.SelectReleased && !blockedByUi
                && !CombatInputPolicy.AllowsTutorialCommand(facts, CombatViewCommandKind.Attack))
            {
                _reportBlockedInput();
                return;
            }
            // 아이템을 UI 위(가방 등)에서 놓으면 장착하지 않고 드래그만 끝낸다(가방 설계 P4).
            if (target == PointerTarget.Item && request == Request.SelectReleased && blockedByUi)
            {
                _interaction.CancelItemSelection();
                return;
            }
            if (_interaction.TryHandleAttackDragPointer(request, screenX, screenY, blockedByUi))
                return;
            if ((target == PointerTarget.Attack || target == PointerTarget.Placement || target == PointerTarget.Item || target == PointerTarget.Skill)
                && request != Request.PointerMoved && blockedByUi)
                return;

            if (request == Request.PointerMoved)
                _interaction.PointerMoved(target, facts, characters, screenX, screenY);
            else if (request == Request.SelectPressed)
                _interaction.SelectPressed(target, facts, characters, screenX, screenY);
            else
                _interaction.SelectReleased(target, facts, characters, screenX, screenY);
        }
    }
}
