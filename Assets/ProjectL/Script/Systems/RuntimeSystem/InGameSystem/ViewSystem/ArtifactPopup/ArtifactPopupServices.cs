using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 아티팩트 팝업이 쓰는 DB 값과 서비스의 묶음입니다. 로비 서비스 묶음이 한 번 만들고, 아티팩트 팝업 Factory가 직접 받습니다
    /// (로비 리팩토링 2차 설계 N-01). 만들 때 null만 한 번 검사하고, 쓰는 곳은 다시 검사하지 않습니다.
    /// 설정 값(<see cref="Config"/>)은 DB 생성(<c>ArtifactPopupConfigTable</c>)이 검사한 원본입니다.
    /// </summary>
    internal sealed class ArtifactPopupServices
    {
        internal ArtifactPopupServices(IUISystemService ui, IInputLayerService layers, IArtifactBufferReadSink board,
            IArtifactViewBufferReadSink input, IArtifactViewBufferWriteSink writer, IArtifactCommandResultReadSink results,
            IDelayService delay, IArtifactInputSlot slot, Func<int, ArtifactViewData> findVisual,
            ArtifactPopupConfigData config, IUnityCameraCatalogService outputs, Func<bool> isStopped)
        {
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Results = results ?? throw new ArgumentNullException(nameof(results));
            Delay = delay ?? throw new ArgumentNullException(nameof(delay));
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            FindVisual = findVisual ?? throw new ArgumentNullException(nameof(findVisual));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
            IsStopped = isStopped ?? throw new ArgumentNullException(nameof(isStopped));
        }

        internal IUISystemService UI { get; }
        internal IInputLayerService Layers { get; }
        internal IArtifactBufferReadSink Board { get; }
        internal IArtifactViewBufferReadSink Input { get; }
        internal IArtifactViewBufferWriteSink Writer { get; }
        internal IArtifactCommandResultReadSink Results { get; }
        internal IDelayService Delay { get; }
        internal IArtifactInputSlot Slot { get; }
        /// <summary>표에 없는 아티팩트 ID는 원인 ID를 담은 예외로 멈춥니다(로비 리팩토링 2차 설계 M-03).</summary>
        internal Func<int, ArtifactViewData> FindVisual { get; }
        internal ArtifactPopupConfigData Config { get; }
        internal IUnityCameraCatalogService Outputs { get; }
        /// <summary>이번 로비 진입에서 아티팩트 도메인이 멈췄는지 묻습니다. 부를 때마다 LifeCycle 사실을 새로 읽습니다.</summary>
        internal Func<bool> IsStopped { get; }
    }
}
