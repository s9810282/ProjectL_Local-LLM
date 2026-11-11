using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 씬 연결이 쓰는 값을 쓰는 쪽별 묶음(씬 표시·노드 진입·맵 입력·팝업 조립)으로 나눠 담고,
    /// 연결 수명(Scope)이 직접 쓰는 칸만 따로 둡니다(스테이지 선택 리팩토링 설계 §2.9 C-19, 로비 2차 R2-c와 같은 꼴).
    /// System이 서비스를 받은 뒤 한 번 만들고, 연결마다 그대로 넘깁니다. 부품은 자기 묶음만 받습니다.
    /// 모든 칸이 운영에 필요하므로 만들 때 null만 한 번 검사하고, 쓰는 곳은 다시 검사하지 않습니다(검사 후속 정리 설계 §12.2 B1·B2).
    /// 연결 사이에 남는 상태 객체(노드 진입 가능 읽기·인트로 기록·노드 낙하 이펙트)는 System이 만들어 필요한 묶음에만 넣습니다.
    /// System만 쓰는 배경음·씬 노드 등록·입력 Buffer 비우기는 넣지 않습니다.
    /// </summary>
    internal sealed class StageMapViewServices
    {
        internal StageMapViewServices(StageMapViewSceneServices scene, StageMapViewEntryServices entry,
            StageMapViewInputServices input, StageMapViewPopupServices popups,
            StageMapViewData mapData, ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            IStageViewBufferReadSink requests, IRunProgressBufferReadSink runProgress, IRecoveryStateReadSink recoveryState,
            IUISystemService ui, IInputLayerService layers, ILodingSystemService loading, StageMapViewNotice notice,
            StageMapIntroHistory introHistory, StageNodeDropEffects nodeDropEffects, IAudioSystemService nodeDropAudio,
            StageMapViewCompletionServices completion)
        {
            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Popups = popups ?? throw new ArgumentNullException(nameof(popups));
            MapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));
            Requests = requests ?? throw new ArgumentNullException(nameof(requests));
            RunProgress = runProgress ?? throw new ArgumentNullException(nameof(runProgress));
            RecoveryState = recoveryState ?? throw new ArgumentNullException(nameof(recoveryState));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
            Loading = loading ?? throw new ArgumentNullException(nameof(loading));
            Notice = notice ?? throw new ArgumentNullException(nameof(notice));
            IntroHistory = introHistory ?? throw new ArgumentNullException(nameof(introHistory));
            NodeDropEffects = nodeDropEffects ?? throw new ArgumentNullException(nameof(nodeDropEffects));
            NodeDropAudio = nodeDropAudio ?? throw new ArgumentNullException(nameof(nodeDropAudio));
            Completion = completion ?? throw new ArgumentNullException(nameof(completion));
        }

        /// <summary>씬 표시(노드·배경·맵 카메라)의 묶음입니다.</summary>
        internal StageMapViewSceneServices Scene { get; }
        /// <summary>노드 진입(표시 소유권·결과 관찰·이동·출전)의 묶음입니다.</summary>
        internal StageMapViewEntryServices Entry { get; }
        /// <summary>맵 선택 입력의 묶음입니다.</summary>
        internal StageMapViewInputServices Input { get; }
        /// <summary>팝업 Factory 넷을 만들 때 쓰는 묶음입니다. 씬 조립만 받습니다.</summary>
        internal StageMapViewPopupServices Popups { get; }
        /// <summary>노드 완료 연출의 System 수명 부품과 필요한 읽기/이동 서비스입니다.</summary>
        internal StageMapViewCompletionServices Completion { get; }

        // 아래는 연결 수명(Scope)과 그 안의 진입 준비·비전투 복원 대기·인트로 조립이 쓰는 칸입니다.
        internal StageMapViewData MapData { get; }
        internal ICurrentProgressBufferReadSink Current { get; }
        internal IStageProgressBufferReadSink Stages { get; }
        internal IStageViewBufferReadSink Requests { get; }
        internal IRunProgressBufferReadSink RunProgress { get; }
        internal IRecoveryStateReadSink RecoveryState { get; }
        internal IUISystemService UI { get; }
        internal IInputLayerService Layers { get; }
        internal ILodingSystemService Loading { get; }
        /// <summary>맵 진입 거절 안내를 띄우는 공용 안내입니다. 상태가 없어 연결 사이에도 남습니다.</summary>
        internal StageMapViewNotice Notice { get; }
        internal StageMapIntroHistory IntroHistory { get; }
        internal StageNodeDropEffects NodeDropEffects { get; }
        internal IAudioSystemService NodeDropAudio { get; }
    }
}
