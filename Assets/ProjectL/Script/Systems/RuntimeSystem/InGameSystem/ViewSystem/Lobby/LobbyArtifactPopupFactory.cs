using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 아티팩트 팝업을 로비 팝업 항목으로 만듭니다. 씬의 아티팩트 루트는 이 Factory를 만들 때 한 번 찾습니다
    /// (로비 리팩토링 2차 설계 M-14). 아티팩트 팝업이 로비 계약에 기대지 않게 로비 쪽에서 감쌉니다.
    /// </summary>
    internal sealed class LobbyArtifactPopupFactory : ILobbyPopupFactory
    {
        private readonly ArtifactPopupViewFactory _factory;
        private readonly IAudioSystemService _audio;
        private readonly ArtifactSceneRoot _root;

        /// <summary>씬에서 아티팩트 팝업 루트를 찾아 Factory를 만듭니다. 없거나 여럿이면 예외입니다.</summary>
        internal LobbyArtifactPopupFactory(ArtifactPopupViewFactory factory, IAudioSystemService audio)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _root = ArtifactPopupViewFactory.FindSceneRoot();
        }

        public ILobbyPopup Create(IPopupViewHost host, PopupInputSession input, Camera output, Transform lobbyRoot)
        {
            if (lobbyRoot == null) throw new ArgumentNullException(nameof(lobbyRoot));
            return new Popup(_factory.Create(_root, input, host, output, lobbyRoot.gameObject), _audio);
        }

        private sealed class Popup : ILobbyPopup
        {
            private readonly ArtifactPopupController _controller;
            private readonly IAudioSystemService _audio;

            internal Popup(ArtifactPopupController controller, IAudioSystemService audio)
            {
                _controller = controller ?? throw new ArgumentNullException(nameof(controller));
                _audio = audio;
            }

            public LobbyFacilityKind Facility => LobbyFacilityKind.Artifact;
            public bool CanOpen => _controller.CanOpen;
            public void Open()
            {
                if (_controller.IsOpen) return;
                _controller.Open();
                if (_controller.IsOpen) _audio.Play(AmbgmEvent.Enter);
            }
            public void Start() => _controller.Start();
            public void Tick() => _controller.Tick();
            public void RestoreOutput() => _controller.RestoreOutput();
            public void Dispose() => _controller.Dispose();
        }
    }
}
