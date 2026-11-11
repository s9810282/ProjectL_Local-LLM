using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Background;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 연결 한 번의 씬 표시 부품입니다. 노드·배경 표시와 맵 카메라 끌기를 갖고, 무엇을 언제 그릴지는 Scope가 정합니다.
    /// </summary>
    internal sealed class StageMapViewScene
    {
        private readonly StageMapViewSceneServices _services;
        private readonly StageMapBackgroundDisplay _background;
        private readonly Camera _output;
        private StageMapCameraDrag _cameraDrag;

        internal StageMapViewScene(StageMapSceneRoot root, Camera output, StageMapViewSceneServices services)
        {
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _output = output != null ? output : throw new ArgumentNullException(nameof(output));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _background = new StageMapBackgroundDisplay(services.Backgrounds);
            var placement = new StageMarkerPlacement(services.MapData.MarkerVisit.FrontClearance);
            Display = new StageMapViewDisplay(services.Colors, services.Outlines, placement);
        }

        internal StageMapSceneRoot Root { get; }
        internal StageMapViewDisplay Display { get; }
        internal StageMapCameraDrag CameraDrag => _cameraDrag;

        /// <summary>맵 카메라 끌기를 만듭니다. 핸들은 <see cref="BindCameraHandle"/>에서 빌립니다.</summary>
        internal StageMapCameraDrag CreateCameraDrag() => new(_output, Root.MapCamera, _services.Cameras);

        /// <summary>출력 카메라로 표시된 노드를 집는 부품을 만듭니다. 출력 카메라는 씬 부품 밖에 내놓지 않습니다(평가 N-03).</summary>
        internal StageNodePicker CreatePicker(float pickDistance) => new(_output, Display, pickDistance);

        internal void AdoptCameraDrag(StageMapCameraDrag drag)
        {
            _cameraDrag = drag ?? throw new ArgumentNullException(nameof(drag));
        }

        /// <summary>Catalog의 AfterLoad 재수집 뒤 맵 카메라 핸들을 한 번 빌립니다. 이미 빌렸거나 끌기가 없으면 아무것도 하지 않습니다.</summary>
        internal void BindCameraHandle()
        {
            StageMapCameraDrag drag = _cameraDrag;
            if (drag == null || drag.HasHandle) return;
            drag.BindHandle();
        }

        internal void ShowBackground(int mapKey) => _background.Show(mapKey, Root.BackgroundRoot);

        internal void ClearBackground() => _background.Dispose();

        internal void CollectLinkDrawTargets(List<IStageLinkDrawTarget> targets) => Display.CollectLinkDrawTargets(targets);

        internal void ShowNodes(int mapKey, IReadOnlyList<StageProgressData> nodes, int currentNodeId,
            List<StageNodeDropTarget> dropTargets = null)
        {
            Display.Show(mapKey, nodes, _services.MapData, _services.FindVisual, Root.MapRoot, currentNodeId, dropTargets);
        }

        /// <summary>표시된 노드로 카메라 이동 범위를 정합니다(규칙은 <see cref="StageMapCameraBounds"/>). 끌기가 없으면 아무것도 하지 않습니다.</summary>
        internal void ConfigureCameraBounds(IReadOnlyList<StageProgressData> nodes)
        {
            if (_cameraDrag == null) return;
            StageMapCameraBounds.Find(nodes, Display, out Vector3 start, out Vector3 boss);
            _cameraDrag.SetBounds(start, boss, Root.MapRoot.forward, Root.MapRoot.up);
        }

        /// <summary>카메라 → 노드 표시 → 배경 순으로 정리합니다(옛 Scope 정리 순서).</summary>
        internal void Dispose()
        {
            List<Exception> errors = null;
            CleanupErrors.Try(ReleaseCamera, ref errors);
            CleanupErrors.Try(Display.Clear, ref errors);
            CleanupErrors.Try(_background.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 씬 표시 정리 중 실패했습니다.");
        }

        private void ReleaseCamera()
        {
            StageMapCameraDrag drag = _cameraDrag;
            _cameraDrag = null;
            drag?.Dispose();
        }
    }
}
