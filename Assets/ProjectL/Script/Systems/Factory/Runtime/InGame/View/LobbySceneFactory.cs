using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>
    /// 로비 씬 연결의 조립입니다. 팝업 Factory 목록(덱 → 아티팩트)을 만들고 Scope에 넘깁니다.
    /// 상태가 없고 <see cref="Instance"/> 하나만 씁니다.
    /// </summary>
    internal sealed class LobbySceneFactory : ILobbySceneFactory
    {
        internal static readonly LobbySceneFactory Instance = new();

        private LobbySceneFactory()
        {
        }

        public LobbyViewPopups FindPopups(LobbySceneRoot root, LobbyViewServices services, LobbyMainSelection selection,
            RuntimeFaultBoundary faults)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (faults == null) throw new ArgumentNullException(nameof(faults));
            // 팝업 순서가 시작(앞에서부터)·Tick과 정리(뒤에서부터)의 순서입니다.
            return new LobbyViewPopups(new ILobbyPopupFactory[]
            {
                new LobbyDeckPopupFactory(services.Deck, root.UI.DeckScreen, selection, faults),
                new LobbyArtifactPopupFactory(new ArtifactPopupViewFactory(faults, services.Artifact), services.Audio)
            });
        }

        public LobbyViewScope Create(LobbySceneRoot root, Camera output, LobbyViewServices services, LobbyViewPopups popups,
            LobbyMainSelection selection, RuntimeFaultBoundary faults)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (popups == null) throw new ArgumentNullException(nameof(popups));
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (faults == null) throw new ArgumentNullException(nameof(faults));
            return new LobbyViewScope(root, output, services, popups, selection, faults);
        }
    }
}
