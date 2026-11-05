using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>노드 사이 연결선 인스턴스의 생성과 정리를 담당하는 일반 C# 클래스입니다.</summary>
    internal sealed class StageLinkView
    {
        private readonly List<GameObject> _instances = new();
        private readonly List<IColorHandle> _colorHandles = new();

        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            StageNodeView nodeView, Transform parent, IColorSystemService colors)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
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

                    Vector3 start = parent.InverseTransformPoint(source.position);
                    Vector3 end = parent.InverseTransformPoint(target.position);
                    Vector3 direction = end - start;
                    float length = direction.magnitude;
                    if (!float.IsFinite(length) || length <= 0.0001f)
                        throw new InvalidOperationException($"[StageLinkView] 연결선 길이가 유효하지 않습니다: {node.NodeId} → {nextId}");

                    GameObject instance = Object.Instantiate(mapData.ConnectionPrefab, parent, false);
                    _instances.Add(instance);
                    instance.name = $"StageLink_{node.NodeId}_{nextId}";
                    MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
                    if (renderer == null)
                        throw new InvalidOperationException("[StageLinkView] 연결선 루트에 MeshRenderer가 필요합니다.");

                    // XY Quad의 세로축을 연결 방향으로 눕힙니다. Root의 회전·배율·이동을 그대로 따릅니다.
                    Transform link = instance.transform;
                    link.localPosition = (start + end) * 0.5f + Vector3.up * mapData.LineHeight;
                    link.localRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                    link.localScale = new Vector3(mapData.LineWidth, length, 1f);

                    IColorHandle color = colors.CreateColorHandle(renderer)
                        ?? throw new InvalidOperationException("[StageLinkView] 연결선 색상 핸들을 만들 수 없습니다.");
                    _colorHandles.Add(color);
                    color.SetOverrideData(0, MaterialBaseTypeKey.MainColor, mapData.LineColor);
                }
            }
        }

        internal void Clear()
        {
            // Renderer가 살아 있는 동안 반환해야 ColorSystem의 등록도 해제됩니다(FW-011).
            foreach (IColorHandle color in _colorHandles) color.Dispose();
            _colorHandles.Clear();
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
