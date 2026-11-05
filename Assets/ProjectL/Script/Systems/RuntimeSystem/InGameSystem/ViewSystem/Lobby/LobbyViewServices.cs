using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 씬 연결이 쓰는 DB 값과 서비스를 받아 쓰는 쪽별 묶음(씬 표시·입력·출발·아티팩트 팝업)으로 나눕니다(로비 리팩토링 2차 설계 N-01).
    /// System이 서비스를 받은 뒤 한 번 만들고, 연결마다 그대로 넘깁니다. 부품은 자기 묶음만 받습니다.
    /// 모든 칸이 운영에 필요하므로 만들 때 null만 한 번 검사하고, 쓰는 곳은 다시 검사하지 않습니다(검사 후속 정리 설계 §12.2 B1·B2).
    /// DB 값(판·이동·캐릭터 보정·호버·아티팩트 설정)은 DB 생성(<c>LobbyVisualTable</c>·<c>ArtifactPopupConfigTable</c>)이 검사한 원본이라 여기서 다시 검사하지 않습니다(로비 리팩토링 설계 LR5-3).
    /// 배경음 서비스(<see cref="Audio"/>)도 이 묶음이 원본이며, 배경음을 트는 시점은 System이 정합니다. 씬 노드 등록은 넣지 않습니다.
    /// </summary>
    internal sealed class LobbyViewServices
    {
        internal LobbyViewServices(
            LobbyModelData models, LobbyBoardData board, LobbyMotionData motion,
            LobbyCharacterPresentationData characterPresentation, LobbyFacilityHoverData facilityHover,
            IArtifactViewTableQuery artifactVisuals, ArtifactPopupConfigData artifactConfig,
            ILobbyViewBufferWriteSink departure,
            IArtifactBufferReadSink artifactBoard, IArtifactViewBufferReadSink artifactInput,
            IArtifactViewBufferWriteSink artifactWriter, IArtifactCommandResultReadSink artifactResults,
            ILobbyReceiverSlot slot, IArtifactInputSlot artifactSlot,
            IUISystemService ui, IUITextPresentation text, IInputLayerService layers, IDelayService delay,
            IUnityCameraCatalogService outputs, IAudioSystemService audio,
            ILobbyProgressReader progress)
        {
            if (artifactVisuals == null) throw new ArgumentNullException(nameof(artifactVisuals));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Departure = departure ?? throw new ArgumentNullException(nameof(departure));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Scene = new LobbySceneServices(models, board, characterPresentation);
            Input = new LobbyInputServices(layers, delay, motion, facilityHover, audio, slot);
            // 표에 없는 아티팩트 ID는 원인 ID를 담은 예외(KeyNotFoundException)로 멈춥니다(로비 리팩토링 2차 설계 M-03).
            // 아티팩트 도메인 정지(처리 기록 누락)는 LifeCycle 사실이 원본입니다. View는 읽기만 합니다(M-06).
            Artifact = new ArtifactPopupServices(ui, layers, artifactBoard, artifactInput, artifactWriter, artifactResults,
                delay, artifactSlot, artifactVisuals.GetArtifactView, artifactConfig, outputs,
                () => progress.Read().IsArtifactStopped);
        }

        /// <summary>씬 표시(방·판·캐릭터)의 묶음입니다.</summary>
        internal LobbySceneServices Scene { get; }
        /// <summary>로비 입력의 묶음입니다.</summary>
        internal LobbyInputServices Input { get; }
        /// <summary>아티팩트 팝업의 묶음입니다. 아티팩트 팝업 Factory가 직접 받습니다.</summary>
        internal ArtifactPopupServices Artifact { get; }
        /// <summary>출발 창구입니다. 읽기 계약만 주고받습니다(로비 리팩토링 2차 설계 N-75).</summary>
        internal ILobbyViewBufferWriteSink Departure { get; }
        internal IUITextPresentation Text { get; }
        internal IUISystemService UI { get; }
        internal IInputLayerService Layers { get; }
        internal IAudioSystemService Audio { get; }
    }
}
