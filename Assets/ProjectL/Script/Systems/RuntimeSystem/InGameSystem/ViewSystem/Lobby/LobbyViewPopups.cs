using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 연결 한 번의 팝업 부품입니다. 순서 있는 팝업 Factory 목록으로 팝업을 같은 입력 세션에 만들고,
    /// 시작은 목록 순서, Tick·정리는 그 역순으로 돕니다(로비 리팩토링 2차 설계 M-14). 지금 목록은 덱 → 아티팩트입니다.
    /// </summary>
    internal sealed class LobbyViewPopups
    {
        private readonly ILobbyPopupFactory[] _factories;
        private readonly List<ILobbyPopup> _popups = new();
        private Func<bool> _canWork;

        internal LobbyViewPopups(IReadOnlyList<ILobbyPopupFactory> factories)
        {
            if (factories == null) throw new ArgumentNullException(nameof(factories));
            _factories = new ILobbyPopupFactory[factories.Count];
            for (int i = 0; i < factories.Count; i++)
                _factories[i] = factories[i] ?? throw new ArgumentException($"[Lobby] 팝업 Factory {i}번이 없습니다.", nameof(factories));
        }

        /// <summary>만든 팝업 항목입니다. 시설 실행 표가 종류마다 하나씩 받습니다.</summary>
        internal IReadOnlyList<ILobbyPopup> Popups => _popups;

        /// <summary>
        /// 목록 순서대로 팝업을 만듭니다. 시작(이미 열린 화면 인수)은 입력을 연결한 뒤 <see cref="Start"/>에서 합니다.
        /// 도중에 연결이 끝나면 방금 만든 것을 정리하고 false입니다(앞서 만든 것은 <see cref="Dispose"/>가 정리합니다).
        /// </summary>
        /// <param name="lobbyRoot">팝업 동안 숨길 로비 공간 루트(로비 씬 루트의 월드 루트)입니다.</param>
        internal bool Create(IPopupViewHost host, PopupInputSession input, Camera output, Transform lobbyRoot, Func<bool> canWork)
        {
            if (lobbyRoot == null) throw new ArgumentNullException(nameof(lobbyRoot));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            foreach (ILobbyPopupFactory factory in _factories)
            {
                ILobbyPopup popup = factory.Create(host, input, output, lobbyRoot);
                if (!canWork())
                {
                    popup.Dispose();
                    return false;
                }
                _popups.Add(popup);
            }
            return true;
        }

        /// <summary>목록 순서로 이미 열린 화면을 인수합니다. 사이에 연결이 끝나면 멈춥니다.</summary>
        internal void Start()
        {
            for (int i = 0; i < _popups.Count; i++)
            {
                if (i > 0 && !_canWork()) return;
                _popups[i].Start();
            }
        }

        /// <summary>목록 역순으로 진행합니다(옛 Scope Tick 순서: 아티팩트 → 덱).</summary>
        internal void Tick()
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                if (i < _popups.Count - 1 && !_canWork()) return;
                _popups[i].Tick();
            }
        }

        /// <summary>팝업이 바꾼 공유 출력 설정을 되돌립니다. 다른 정리 콜백이 새 연결을 게시하기 전에 부릅니다.</summary>
        internal void RestoreOutput()
        {
            List<Exception> errors = null;
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                ILobbyPopup popup = _popups[i];
                CleanupErrors.Try(popup.RestoreOutput, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[Lobby] 팝업 출력 복원 중 실패했습니다.");
        }

        /// <summary>목록 역순으로 정리합니다(옛 Scope 정리 순서: 아티팩트 → 덱).</summary>
        internal void Dispose()
        {
            List<Exception> errors = null;
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                ILobbyPopup popup = _popups[i];
                CleanupErrors.Try(popup.Dispose, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[Lobby] 팝업 정리 중 실패했습니다.");
        }
    }
}
