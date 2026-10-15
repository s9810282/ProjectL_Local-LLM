using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>노드 사이 연결선 인스턴스의 생성과 정리를 담당하는 일반 C# 클래스입니다.</summary>
    internal sealed class StageLinkView
    {
        private readonly List<GameObject> _instances = new();

        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            StageNodeView nodeView, Transform parent)
        {
            if (mapData.ConnectionPrefab == null)
                throw new InvalidOperationException("[StageLinkView] Connection prefab is required.");

            foreach (StageProgressData node in nodes)
            {
                if (!nodeView.TryGetNode(node.NodeId, out Transform source))
                    throw new InvalidOperationException($"[StageLinkView] Missing source: {node.NodeId}");

                foreach (int nextId in node.NextNodeIds)
                {
                    if (!nodeView.TryGetNode(nextId, out Transform target))
                        throw new InvalidOperationException($"[StageLinkView] Missing target: {nextId}");

                    GameObject instance = Object.Instantiate(mapData.ConnectionPrefab, parent, false);
                    _instances.Add(instance);
                    instance.name = $"StageLink_{node.NodeId}_{nextId}";
                    LineRenderer line = instance.GetComponentInChildren<LineRenderer>(true);
                    if (line == null)
                        throw new InvalidOperationException("[StageLinkView] Connection requires a LineRenderer.");

                    // 선의 자식 Transform과 맵 Root의 회전·크기를 반영하며 Root 이동도 따라갑니다.
                    Vector3 offset = parent.TransformVector(Vector3.up * mapData.LineHeight);
                    line.useWorldSpace = false;
                    line.loop = false;
                    line.positionCount = 2;
                    line.startWidth = mapData.LineWidth;
                    line.endWidth = mapData.LineWidth;
                    line.SetPosition(0, line.transform.InverseTransformPoint(source.position + offset));
                    line.SetPosition(1, line.transform.InverseTransformPoint(target.position + offset));
                }
            }
        }

        internal void Clear()
        {
            foreach (GameObject instance in _instances)
            {
                if (instance == null) continue;
                instance.SetActive(false);
                Object.Destroy(instance);
            }
            _instances.Clear();
        }
    }
}
