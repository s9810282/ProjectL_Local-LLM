using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Background;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 연결 한 번의 씬 표시 부품입니다. 노드·배경 표시와 맵 카메라 끌기를 갖고, 무엇을 언제 그릴지는 Scope가 정합니다.
    /// </summary>
    internal sealed class StageMapViewScene
    {
        private readonly StageMapViewServices _services;
        private readonly StageMapBackgroundDisplay _background;
        private StageMapCameraDrag _cameraDrag;

        internal StageMapViewScene(StageMapSceneRoot root, Camera output, StageMapViewServices services)
        {
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Output = output != null ? output : throw new ArgumentNullException(nameof(output));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _background = new StageMapBackgroundDisplay(services.Backgrounds);
            Display = new StageMapViewDisplay(services.Colors);
#if UNITY_EDITOR
            CameraFraming = new Editor.StageMapCameraFramingSettings(root);
#endif
        }

        internal StageMapSceneRoot Root { get; }
        internal Camera Output { get; }
        internal StageMapViewDisplay Display { get; }
        internal StageMapCameraDrag CameraDrag => _cameraDrag;

        /// <summary>맵 카메라 끌기를 만듭니다. 핸들은 <see cref="BindCameraHandle"/>에서 빌립니다.</summary>
        internal StageMapCameraDrag CreateCameraDrag() => new(Output, Root.MapCamera, _services.Cameras);

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

#if UNITY_EDITOR
        internal Editor.StageMapCameraFramingSettings CameraFraming { get; }
        internal void PreviewCameraFraming() => _cameraDrag?.PreviewFraming(CameraFraming);
#endif

        internal void ShowNodes(int mapKey, IReadOnlyList<StageProgressData> nodes, int currentNodeId)
        {
            Display.Show(mapKey, nodes, _services.MapData, _services.FindVisual, Root.MapRoot, currentNodeId);
        }

        /// <summary>표시된 시작·Boss 노드에서 카메라 이동 범위를 정합니다. 끌기가 없으면 아무것도 하지 않습니다.</summary>
        internal void ConfigureCameraBounds(IReadOnlyList<StageProgressData> nodes)
        {
            if (_cameraDrag == null) return;
            Transform start = null;
            Transform boss = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                StageProgressData node = nodes[i];
                if (node.IsStart) Display.TryGetNode(node.NodeId, out start);
                else if (node.MapRole == StageMapRole.Boss) Display.TryGetNode(node.NodeId, out boss);
            }
            if (start == null || boss == null)
                throw new InvalidOperationException("[StageMap] 카메라 이동 범위에 필요한 시작·Boss 노드가 없습니다.");
            _cameraDrag.SetBounds(start.position, boss.position, Root.MapRoot.forward, Root.MapRoot.up);
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
