using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 연결마다 팝업 한 벌을 만들고 소유권을 호출한 Scope에 넘깁니다. 로비 씬 Factory가 연결마다 만들며,
    /// 모든 입력이 운영에 필요합니다(검사 후속 정리 설계 §12.2 B1). 출력 마스크도 여기서 만듭니다(로비 리팩토링 2차 설계 N-61).
    /// </summary>
    internal sealed class ArtifactPopupViewFactory
    {
        private readonly RuntimeFaultBoundary _faults;
        private readonly ArtifactPopupServices _services;

        internal ArtifactPopupViewFactory(RuntimeFaultBoundary faults, ArtifactPopupServices services)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// 씬의 아티팩트 팝업 루트를 하나 찾아 구성을 검사합니다. 로비 연결이 이전 연결을 떼기 전에 불러,
        /// 없거나 여럿이거나 구성이 틀리면 기존 연결을 건드리지 않고 멈춥니다(로비 리팩토링 2차 설계 N-80).
        /// </summary>
        internal static ArtifactSceneRoot FindSceneRoot()
        {
            ArtifactSceneRoot[] roots = UnityEngine.Object.FindObjectsByType<ArtifactSceneRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (roots.Length != 1)
                throw new InvalidOperationException("[ArtifactPopupViewFactory] 로비 씬에 ArtifactSceneRoot가 하나 필요합니다.");
            roots[0].Validate();
            return roots[0];
        }

        /// <param name="output">보드 피킹과 출력 마스크에 쓸 출력 카메라입니다. 로비 연결이 받은 것을 넘깁니다(카메라 이전 설계 L1).</param>
        /// <param name="lobbyRoot">팝업 동안 숨길 로비 공간 루트입니다. 로비 연결의 월드 루트를 넘깁니다(로비 리팩토링 2차 설계 M-12).</param>
        internal ArtifactPopupController Create(ArtifactSceneRoot root, PopupInputSession input, IPopupViewHost host,
            Camera output, GameObject lobbyRoot)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (lobbyRoot == null) throw new ArgumentNullException(nameof(lobbyRoot));
            // 입력 세션·호스트·출력 카메라는 받는 부품(컨트롤러·포인터 판정·출력 마스크)이 검사합니다(N-25).
            if (_faults.IsFaulted) throw new InvalidOperationException("[ArtifactPopup] 정지한 런타임에는 팝업을 만들 수 없습니다.");
            // 루트 구성은 FindSceneRoot가 이전 연결을 떼기 전에 검사했습니다(N-80).
            // Configure가 보드 좌표 변환기 하나와 드래그 표시까지 준비합니다(로비 리팩토링 2차 설계 N-02·N-03).
            root.PopupView.Configure(_services.Config, lobbyRoot);
            var pointer = new ArtifactPointerMapper(root.PopupUI, _services.Board, output, root.PopupView.BoardMapper);
            var popupOutput = new ArtifactPopupOutputMask(_services.Outputs, output, host, _services.Config.OutputLayerName);
            var popup = new ArtifactPopupController(_faults, _services, root.PopupView, root.PopupView, root.PopupUI,
                root.PopupUI, pointer, input, host, popupOutput);
            try
            {
                if (host.IsCurrent) root.PopupUI.SetCanClose(false);
                return popup;
            }
            catch (Exception error)
            {
                var errors = new System.Collections.Generic.List<Exception> { error };
                CleanupErrors.Try(popup.Dispose, ref errors);
                throw new AggregateException("[ArtifactPopup] 씬 팝업 생성 중 실패했습니다.", errors);
            }
        }
    }
}
