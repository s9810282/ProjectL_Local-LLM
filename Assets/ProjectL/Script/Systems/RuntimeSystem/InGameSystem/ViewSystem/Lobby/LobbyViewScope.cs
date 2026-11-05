using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 씬 연결 한 번의 수명입니다. 부품 넷(씬 표시·팝업·로비 입력·출발)과 로비 화면 글자를 갖고,
    /// 시작·Tick·정리 순서와 입력 판정 둘을 정합니다.
    /// </summary>
    /// <remarks>
    /// 연결 확인(<see cref="CanWork"/>)은 재진입할 수 있는 호출(UI 열고 닫기·Instantiate·외부 콜백) 뒤에만 다시 합니다.
    /// 순수 Buffer 읽기 뒤에는 하지 않습니다(검사 후속 정리 설계 §12.2 B7).
    /// </remarks>
    internal sealed class LobbyViewScope : IPopupViewHost, IDisposable
    {
        private readonly LobbyViewServices _services;
        private readonly RuntimeFaultBoundary _faults;
        // 로비 캔버스 안의 제목·시설 안내 묶음입니다. 따로 여닫는 화면이 아니라 시설 팝업 동안만 숨기므로
        // UIBase로 등록하지 않고 여기서 켜고 끕니다(10-28 Q-G5 B, 로비 리팩토링 설계 LR3).
        private readonly GameObject _lobbyTexts;
        private readonly LobbyViewScene _scene;
        private readonly LobbyViewPopups _popups;
        private readonly LobbyViewInput _input;
        private readonly LobbyDepartureFlow _departure;
        // 부품에 넘기는 연결 확인입니다. 한 번만 만들어 Tick마다 대리자를 새로 만들지 않습니다.
        private readonly Func<bool> _canWork;
        private bool _started;
        private bool _readyForTick;
        // 시설 팝업이 열려 있는지입니다. 팝업 수명이 알려 주는 값이라 여기 한 곳에만 둡니다.
        private bool _popupBlocked;

        /// <param name="output">화면을 그리는 출력 카메라입니다. 씬 소유가 아니며 연결 때 받은 것을 씁니다(카메라 이전 설계 L1).</param>
        internal LobbyViewScope(LobbySceneRoot root, Camera output, LobbyViewServices services, LobbyViewPopups popups,
            RuntimeFaultBoundary faults)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            // 화면 연결 검사는 System이 이전 연결을 떼기 전에 LobbyCanvasView.ValidateComposition으로 합니다.
            _lobbyTexts = root.UI.LobbyTexts;
            _canWork = () => CanWork;
            _scene = new LobbyViewScene(root, output, services.Scene);
            _input = new LobbyViewInput(services.Input, () => IsCurrent, _faults);
            _departure = new LobbyDepartureFlow(services.Departure, services.Text, _canWork);
        }

        internal bool IsDisposed { get; private set; }
        internal bool IsSuperseded { get; private set; }
        public bool IsCurrent => !IsSuperseded;
        internal bool CanWork => !IsDisposed && !IsSuperseded && !_faults.IsFaulted;

        /// <summary>
        /// 로비 화면 글자 → 씬 표시 → 팝업 → 로비 입력 → 팝업 시작(이미 열린 화면 인수) 순으로 시작합니다.
        /// </summary>
        /// <returns>준비를 끝내 Tick을 받을 수 있으면 true입니다. 도중에 연결이 끝나면 false입니다(로비 리팩토링 2차 설계 N-12).</returns>
        internal bool Start(Action requireRunning)
        {
            if (requireRunning == null) throw new ArgumentNullException(nameof(requireRunning));
            if (_started || !CanWork) return false;
            _started = true;
            // 앞 연결이 시설 팝업 중에 끝났으면 로비 화면 글자가 꺼진 채 남으므로 연결마다 다시 켭니다.
            _lobbyTexts.SetActive(true);
            if (!_scene.Start(requireRunning, _canWork, () => IsCurrent)) return false;
            var session = new PopupInputSession(new ViewAttachmentInputLayers(_services.Layers, () => IsCurrent),
                LobbyInputLayers.Lobby, () => CanWork && !_departure.IsHolding);
            if (!_popups.Create(this, session, _scene.Output, _scene.Root.WorldRoot, _canWork)) return false;
            _input.Create(_scene.InputTargets, new LobbyFacilityActions(FacilityActions(), CanRunFacility),
                IsInteractionBlocked);
            requireRunning();
            if (!CanWork) return false;
            if (!_input.Connect(_canWork)) return false;
            _popups.Start();
            if (!CanWork) return false;
            // 준비 뒤의 Tick은 팝업·입력이 모두 있다고 보고 직접 부릅니다.
            _readyForTick = true;
            return true;
        }

        internal void Tick()
        {
            if (!CanWork || !_readyForTick) return;
            _popups.Tick();
            if (!CanWork) return;
            _input.Tick();
            if (!CanWork) return;
            _departure.Tick();
        }

        internal void Supersede() => IsSuperseded = true;

        public void SetPopupBlocked(bool blocked)
        {
            if (!CanWork) return;
            // 팝업이 닫히면 늘 막힘을 풉니다. 로비 Layer로 돌아오지 못한 경우(출발 처리 중·설정창 등)는
            // 쓰는 순간의 판정(출발 처리 = IsHolding, Layer = CanRunFacility)이 막습니다(로비 리팩토링 2차 설계 N-04).
            _popupBlocked = blocked;
            // 로비 화면 글자(제목·시설 안내)는 시설 팝업 동안 숨깁니다. 팝업 화면은 자기 제목을 갖습니다.
            _lobbyTexts.SetActive(!blocked);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            _readyForTick = false;
            List<Exception> errors = null;
            // 공유 출력(배경 카메라·아티팩트 출력)은 다른 정리 콜백이 새 연결을 게시하기 전에 먼저 되돌립니다.
            // 나머지는 시작의 역순입니다. 정리 중 입력은 판정(끝난 연결)이 막습니다.
            CleanupErrors.Try(_scene.RestoreOutput, ref errors);
            CleanupErrors.Try(_popups.RestoreOutput, ref errors);
            CleanupErrors.Try(_departure.CancelOwnedRequest, ref errors);
            CleanupErrors.Try(_popups.Dispose, ref errors);
            CleanupErrors.Try(_input.Dispose, ref errors);
            CleanupErrors.Try(_scene.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[Lobby] 씬 연결 정리 중 실패했습니다.");
        }

        // 입력 판정 둘은 쓰는 순간 원본(팝업 알림·출발 요청·입력 Layer)을 읽고 막힘 사본을 두지 않습니다(설계 §12.2 B4).

        /// <summary>시설 고르기·도착 실행을 막는지입니다. 끝난 연결, 시설 팝업, 출발 처리 중이면 막습니다.</summary>
        private bool IsInteractionBlocked() => !CanWork || _popupBlocked || _departure.IsHolding;

        /// <summary>
        /// 도착한 시설(덱·아티팩트·출발)을 지금 실행해도 되는지입니다. 막히지 않았고 로비 입력 Layer여야 합니다.
        /// 세 시설이 모두 이 판정 하나를 거칩니다(로비 리팩토링 2차 설계 M-02).
        /// </summary>
        private bool CanRunFacility() => !IsInteractionBlocked() && _input.IsLobbyLayer;

        /// <summary>
        /// 시설 종류 → 실행 항목 표의 재료입니다. 팝업은 자기 시설 종류로, 스테이지 입장은 출발 의도로 들어갑니다(로비 리팩토링 2차 설계 M-13·N-54).
        /// 빠진 종류·겹친 종류는 실행 표가 만들 때 막습니다.
        /// </summary>
        private IEnumerable<KeyValuePair<LobbyFacilityKind, ILobbyFacilityAction>> FacilityActions()
        {
            foreach (ILobbyPopup popup in _popups.Popups)
                yield return new KeyValuePair<LobbyFacilityKind, ILobbyFacilityAction>(popup.Facility, popup);
            yield return new KeyValuePair<LobbyFacilityKind, ILobbyFacilityAction>(LobbyFacilityKind.StageEntry,
                new LobbyFacilityAction(() => true, _departure.Submit));
        }
    }
}
