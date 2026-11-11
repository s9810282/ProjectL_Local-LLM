using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.SelectOutlineSystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>씬 표시(노드·배경·맵 카메라)가 쓰는 값의 묶음입니다(스테이지 선택 리팩토링 설계 §2.9 C-19).</summary>
    internal sealed class StageMapViewSceneServices
    {
        internal StageMapViewSceneServices(StageMapViewData mapData, IStageNodeVisualTableQuery nodeVisuals,
            IStageMapBackgroundTableQuery backgrounds, IColorSystemService colors, ISelectOutlineSystemService outlines,
            ICameraCatalogService cameras)
        {
            MapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
            if (nodeVisuals == null) throw new ArgumentNullException(nameof(nodeVisuals));
            FindVisual = node => nodeVisuals.TryGetNodeVisual(node.StageKey, node.StageType, node.MapRole,
                out StageNodeVisualData visual) ? visual : null;
            Backgrounds = backgrounds ?? throw new ArgumentNullException(nameof(backgrounds));
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            Outlines = outlines ?? throw new ArgumentNullException(nameof(outlines));
            Cameras = cameras ?? throw new ArgumentNullException(nameof(cameras));
        }

        internal StageMapViewData MapData { get; }
        internal Func<StageProgressData, StageNodeVisualData> FindVisual { get; }
        internal IStageMapBackgroundTableQuery Backgrounds { get; }
        internal IColorSystemService Colors { get; }
        internal ISelectOutlineSystemService Outlines { get; }
        internal ICameraCatalogService Cameras { get; }
    }
}
