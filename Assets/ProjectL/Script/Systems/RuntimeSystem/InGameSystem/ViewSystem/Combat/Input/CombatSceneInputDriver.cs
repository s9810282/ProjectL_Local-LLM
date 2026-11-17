using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 전투 입력의 분배와 입력 쪽 맞춤입니다(조정자 나누기 설계 CS3, 평가 C2-02). 조정자에서 옮긴 일을 맡습니다:
    /// 수신 창구가 넘긴 입력 하나의 분배(<see cref="Route"/>), 선택 아군 가용성 조회·환경 설명·행동 버튼·카메라 맞춤, 목표 입력 Layer 적용.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 수명은 서비스 수명입니다(Q-CS1 A). 조정자가 서비스를 받을 때 입력 사실 읽기 다음에 만듭니다(P2). 씬 연결은 필드로 갖지 않고 매 호출 인자로 받습니다.
    /// 외부 호출에서 돌아올 때마다 그 연결로 이어 가도 되는지는 재확인 계약(<see cref="ICombatConnectionGuard"/>)에 묻습니다.
    /// </para>
    /// <para>
    /// 무엇을 할지(허용·막힘·목표 Layer)는 정책(<see cref="CombatInputPolicy"/>) 한 곳이 정하고, 이 객체는 사실을 읽어 정책에 묻고 연결 입력 묶음·입력 표면·입력 연결에 넘깁니다.
    /// 언제 부를지(Tick 순서·결과 후처리)는 조정자가 정합니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatSceneInputDriver
    {
        private readonly ICombatConnectionGuard _guard;
        private readonly CombatInputFactsReader _facts;
        private readonly CombatInputConnection _inputConnection;
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICharacterBufferReadSink _characters;
        // 미리보기 응답 단계의 사실 공급자입니다. 입력 기능 묶음이 감시 단계 뒤에 부릅니다(View 2라운드 W5-3). 한 번만 만듭니다.
        private readonly Func<CombatInputFacts> _readFollowUpFacts;

        internal CombatSceneInputDriver(
            ICombatConnectionGuard guard,
            CombatInputFactsReader facts,
            CombatInputConnection inputConnection,
            ICombatStateBufferReadSink state,
            ICharacterBufferReadSink characters)
        {
            _guard = guard ?? throw new ArgumentNullException(nameof(guard));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _inputConnection = inputConnection ?? throw new ArgumentNullException(nameof(inputConnection));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _readFollowUpFacts = () => _facts.Read(followUp: true);
        }

        /// <summary>
        /// 수신 창구가 입구 검사를 통과한 입력을 나눕니다. 포인터 이동이 아니면 처리 뒤 행동 버튼을 맞춥니다
        /// (공격 모드 진입·취소, 제출·대기 시작, B6). 키와 버튼이 같은 경로입니다.
        /// </summary>
        /// <param name="connection">입력이 들어온 순간의 씬 연결입니다. 조정자가 그때 읽어 넘깁니다.</param>
        /// <remarks>
        /// 수신 창구는 슬롯에 연결돼 있을 때만 조정자를 거쳐 이것을 부르고, 슬롯 연결은 연결 입력 묶음을 만든 뒤 <see cref="SyncInputLayer"/> 한 곳에서만 합니다.
        /// 그래서 여기서 묶음은 항상 있습니다. 이 불변식이 깨지면 수신 창구가 예외를 오류 경계에 보고합니다.
        /// 배치 확인 갈래·정보창 선택·표면 맞춤은 여기서 하고, Router·환경 호버는 묶음이 맡습니다(계획 TP-7).
        /// </remarks>
        internal void Route(CombatViewConnection connection, CombatInputRouter.Request request, float screenX, float screenY)
        {
            // 카메라 입력은 상태만 저장한다. 선택·가용성·행동 버튼과 무관하므로 묶음에 바로 넘긴다(카메라 조작 설계 CC2, 계획 P5).
            if (CombatInputRouter.IsCameraRequest(request))
            {
                if (_guard.IsLive(connection))
                    connection.Input.Route(request, screenX, screenY, _characters.Characters);
                return;
            }
            if (request == CombatInputRouter.Request.EnterCombat)
            {
                connection.Input.RequestEnterCombat();
                if (_guard.IsLive(connection))
                    SyncActionButtons(connection);
                return;
            }
            // 전경 입력 소유자(Drag·공격 모드·경로 지정)가 포인터를 쓰는 동안은 기존 선택을 유지합니다. 그 판정은 묶음 입구 하나이고(평가 C2-02·C8-01),
            // 그 밖의 선택 자격은 Policy 한 곳에서 판단합니다.
            bool consumed = false;
            if (request == CombatInputRouter.Request.SelectPressed)
                connection.Input.BeginSelectPress();
            if (request == CombatInputRouter.Request.SelectPressed
                && connection.Input.CanInspectOnPress
                && _guard.IsLive(connection)
                && CombatInputPolicy.CanSelectUnit(_facts.Read(includeUnhandledCommand: true)))
            {
                if (connection.Inspection.TryPickInspection(screenX, screenY, out var cell, out var occupant))
                {
                    consumed = connection.Input.TryPressDestination(cell, !occupant.IsEmpty);
                    if (!_guard.IsLive(connection) || !_guard.IsInputReady)
                        return;
                    if (!consumed)
                        connection.Inspection.InspectUnit(occupant);
                }
                // 정보창의 외부 UI 콜백 뒤에는 지난 입력을 새 연결에 보내지 않습니다.
                if (!_guard.IsLive(connection) || !_guard.IsInputReady)
                    return;
            }
            if (request == CombatInputRouter.Request.SelectReleased && _guard.IsLive(connection)
                && connection.Input.OwnsDestinationRelease)
            {
                bool picked = connection.Inspection.TryPickInspection(screenX, screenY, out var cell, out var occupant);
                connection.Input.ReleaseDestination(picked && occupant.IsEmpty ? cell : null);
                if (!_guard.IsLive(connection)) return;
                consumed = true;
            }
            if (request != CombatInputRouter.Request.CycleUnitCategory)
                SyncAvailability(connection);
            // Abort 뒤 Unbind가 보내는 정리 신호는 기존 소유자에게 끝까지 전달합니다(R2). 그래서 이 갈래는 연결만 봅니다.
            bool cleanupRequest = request == CombatInputRouter.Request.FocusLost || request == CombatInputRouter.Request.SelectCanceled;
            if (!_guard.IsConnected(connection) || (!_guard.IsLive(connection) && !cleanupRequest))
                return;
            bool cancelingPlacement = request == CombatInputRouter.Request.Cancel && connection.Input.IsPlacementDragActive;
            // 턴 종료는 확인창 입구를 먼저 지난다. 확인이 필요 없으면 입구가 지금과 같은 Router 경로로 바로 넘긴다.
            if (request == CombatInputRouter.Request.EndTurn)
                connection.Input.RequestEndTurn();
            else if (request == CombatInputRouter.Request.CycleUnitCategory)
            {
                if (!TryCycleUnitCategory(connection))
                    return;
            }
            else if (!consumed)
                connection.Input.Route(request, screenX, screenY, _characters.Characters);
            if (!_guard.IsLive(connection))
                return;
            if (cancelingPlacement && !connection.Input.IsPlacementDragActive)
                connection.Inspection.Deselect();
            SyncAvailability(connection);
            if (!_guard.IsLive(connection))
                return;
            SyncEnvironmentHover(connection);
            if (!_guard.IsLive(connection))
                return;
            if (request != CombatInputRouter.Request.PointerMoved)
                SyncActionButtons(connection);
        }

        private bool TryCycleUnitCategory(CombatViewConnection connection)
        {
            CombatInputFacts facts = _facts.Read(includeUnhandledCommand: true);
            if (!CombatInputPolicy.CanCycleUnitCategory(facts))
                return false;

            var nextActor = CombatCharacterQueries.FindNextPlayerByCategory(_characters.Characters, facts.SelectedActorId);
            if (nextActor.IsEmpty)
                return false;

            connection.Input.PrepareActorSwitch();
            // 조준·경로 표시 정리 콜백이 씬 연결을 바꿀 수 있습니다.
            if (!_guard.IsLive(connection) || !_guard.IsInputReady)
                return false;

            return connection.Inspection.TrySelectActor(nextActor);
        }

        /// <summary>
        /// 매 Tick의 입력 단계입니다: 가용성 조회 → 입력 기능 묶음(감시 단계, 새 응답이면 응답 단계) → 가용성 조회.
        /// 단계마다 이 연결로 이어 가도 되는지 다시 묻고, 끝까지 이어 가도 되면 참을 돌려줍니다. 조정자는 거짓이면 결과 소비로 넘어가지 않습니다.
        /// </summary>
        /// <param name="previewAnswered">이번 Tick에 현재 미리보기 요청의 새 응답을 관측했는지입니다(변경 관측의 값).</param>
        internal bool TickInputs(CombatViewConnection connection, bool previewAnswered)
        {
            SyncAvailability(connection);
            if (!_guard.IsLive(connection))
                return false;
            // 가용성 응답 소비/슬롯 변경 뒤 현재 사실을 새로 읽습니다.
            connection.Input.TickInteraction(_facts.Read(), _characters.Characters,
                _state.TryGetLatestPreview(out var latestPreview) ? latestPreview : null,
                previewAnswered, _readFollowUpFacts);
            if (!_guard.IsLive(connection))
                return false;
            SyncAvailability(connection);
            return _guard.IsLive(connection);
        }

        /// <summary>
        /// Tick 끝의 맞춤입니다: 행동 버튼 → 환경 설명 → 카메라 → HUD 가용성. 행동 버튼은 여러 입력 경로에서 바뀌는 View 상태라 Tick 끝에서 현재 사실로 한 번 파생합니다(구조 개선 SV6).
        /// 카메라는 매 프레임 한 번 진행하고, 움직일지는 입력 사실로 정책이 정합니다(카메라 조작 설계 CC6).
        /// </summary>
        internal void TickEnd(CombatViewConnection connection, float deltaTime)
        {
            SyncActionButtons(connection);
            SyncEnvironmentHover(connection);
            // 시점이 바뀌면 묶음이 마지막 포인터로 칸을 다시 집는다(평가 C10-01).
            connection.Input?.TickCamera(_facts.Read(), deltaTime, _characters.Characters);
            if (!_guard.IsLive(connection) || connection.Input == null)
                return;
            // 배경 조회 둘이 같은 사실·응답을 씁니다. 조회가 슬롯에 요청을 써도 결과 번호·마지막 응답·답한 번호는 바뀌지 않아 한 번만 읽습니다(포효 예고 검토 L4).
            CombatInputFacts facts = _facts.Read(includeUnhandledCommand: true);
            int answered = _state.AnsweredPreviewNumber;
            var latest = _state.TryGetLatestPreview(out var preview) ? preview : null;
            // 포효 예고(결과마다 임박한 보스마다 한 번)가 HUD 묶음 조회(판마다 한 번, 새 SSOT 설계 SN2)보다 먼저 슬롯을 씁니다(포효 예고 계획 TP-11).
            connection.Input.SyncRoarForecast(facts, _characters.Characters, answered, latest);
            // 선택·호버·경로 미리보기가 슬롯을 먼저 사용한 뒤에 HUD 조회를 진행합니다.
            if (_guard.IsLive(connection) && connection.Input != null)
                connection.Input.SyncHudActionAvailability(facts, answered, latest);
        }

        /// <summary>선택 아군의 가용성 조회 수명만 맞춥니다. 조회가 사실을 다시 읽지 않도록 현재 사실을 값으로 넘깁니다.</summary>
        internal void SyncAvailability(CombatViewConnection connection)
        {
            if (!_guard.IsLive(connection) || connection.Input == null)
                return;
            connection.Input.SyncAvailability(_facts.Read(includeUnhandledCommand: true), _characters.Characters,
                _state.AnsweredPreviewNumber);
        }

        /// <summary>표시가 끝난 결과만 설명합니다. 같은 칸·결과 번호의 문구는 다시 생성하지 않습니다. 입력 객체가 없거나 정지 뒤면 아무것도 하지 않습니다.</summary>
        internal void SyncEnvironmentHover(CombatViewConnection connection)
        {
            CombatSceneInput input = connection?.Input;
            if (input == null || !input.HasEnvironmentHover) return;
            CombatInputFacts facts = _facts.Read();
            bool allowed = CanReceiveUiInput() && CombatInputPolicy.CanShowEnvironmentHover(facts)
                && facts.PointerTarget != CombatInputRouter.PointerTarget.None;
            input.SyncEnvironmentHover(facts.HasBattle ? _state.Current : null, facts.LatestResultSequence, allowed);
        }

        /// <summary>
        /// 행동 버튼(공격·턴 종료·이동 되돌리기)을 현재 사실로 맞춥니다(행동 버튼 설계 B3·B6). 판단은 Policy와 공격 진입 판단을 읽기만 하고,
        /// 패널은 값이 바뀔 때만 씁니다. Tick 끝·입력 직후(<see cref="Route"/>)·확인창 수락 직후에 부릅니다(구조 개선 SV6). 정지·입력 닫힘·해제의 숨김은 각 경로가 즉시 합니다.
        /// 입력 객체가 없으면(첫 그리기 전·해제 뒤) 아무것도 하지 않습니다.
        /// </summary>
        internal void SyncActionButtons(CombatViewConnection connection)
        {
            if (connection?.Input == null)
                return;
            connection.Input.SyncConfirmations();
            if (!connection.Shown)
                return;

            // 턴 종료 판단에는 선행 미처리 명령까지 읽는다(Router의 EndTurn과 같은 사실).
            CombatInputFacts facts = _facts.Read(includeUnhandledCommand: true);
            // 세 표면이 같은 막힘 판단 하나를 쓴다(View 단계 설계 W2). 맞춤은 입력 표면이 한다(View 2라운드 W6-2).
            CombatActionButtonState buttons = connection.Input.ReadActionButtons(facts);
            connection.Surfaces.Sync(facts, buttons.CanEnterAttack, buttons.IsAttackActive, buttons.CanUseReturn, buttons.Availability, buttons.Skill);
        }

        /// <summary>덱·가방 UI 입구의 수신 조건입니다. 막힘 판단은 Policy 한 곳이고, 입력이 준비됐을 때만 사실을 읽습니다.</summary>
        internal bool CanReceiveUiInput() => _guard.IsInputReady && !CombatInputPolicy.IsInputBlocked(_facts.Read());

        /// <summary>
        /// 정책이 정한 목표 Layer로 입력 연결을 맞춥니다. 목표가 없으면(준비 전·종료·입력 닫힘·전투 정리) 입력을 해제합니다.
        /// 실제 Layer와 같으면 다시 바꾸지 않습니다. 슬롯 연결·Layer 변경·해제는 입력 연결이 합니다(View 3라운드 W11-1).
        /// </summary>
        internal void SyncInputLayer() =>
            _inputConnection.Apply(CombatInputPolicy.GetDesiredLayer(_facts.Read()));
    }
}
