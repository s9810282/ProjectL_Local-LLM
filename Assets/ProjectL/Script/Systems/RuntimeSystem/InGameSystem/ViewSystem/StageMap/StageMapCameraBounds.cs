using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 맵 카메라 이동 범위의 규칙입니다. 범위는 표시된 시작 노드부터 Boss 노드까지입니다.
    /// 카메라 끌기(<see cref="StageMapCameraDrag"/>)는 받은 두 점으로 범위를 세우기만 하고, 씬 표시는 노드를 그리기만 합니다(평가 N-79).
    /// </summary>
    internal static class StageMapCameraBounds
    {
        /// <summary>표시된 시작·Boss 노드의 위치를 찾습니다. 둘 중 하나라도 없으면 예외입니다.</summary>
        internal static void Find(IReadOnlyList<StageProgressData> nodes, StageMapViewDisplay display,
            out Vector3 start, out Vector3 boss)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (display == null) throw new ArgumentNullException(nameof(display));
            Transform startRoot = null;
            Transform bossRoot = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                StageProgressData node = nodes[i];
                if (node.IsStart) display.TryGetNode(node.NodeId, out startRoot);
                else if (node.MapRole == StageMapRole.Boss) display.TryGetNode(node.NodeId, out bossRoot);
            }
            if (startRoot == null || bossRoot == null)
                throw new InvalidOperationException("[StageMap] 카메라 이동 범위에 필요한 시작·Boss 노드가 없습니다.");
            start = startRoot.position;
            boss = bossRoot.position;
        }
    }
}
