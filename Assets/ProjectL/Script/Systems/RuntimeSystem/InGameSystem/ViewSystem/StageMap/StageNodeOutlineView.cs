using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.SelectOutlineSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>진입 가능 건물의 기본 윤곽선과 호버 전환을 소유합니다. 노드 파괴 전에 Clear로 Scope를 반환합니다.</summary>
    internal sealed class StageNodeOutlineView
    {
        // 색은 공용 키(ColorShaderPropertyKey.OutlineColor)를 씁니다. 두께·간격은 SelectOutline 셰이더 전용 이름입니다.
        private static readonly TypeSettingKey<float> OutlineWidth = new("_OutlineThickness");
        private static readonly TypeSettingKey<float> PulseGap = new("_PulseGap");
        private readonly ISelectOutlineSystemService _outlines;
        private readonly List<Highlight> _highlights = new();

        internal StageNodeOutlineView(ISelectOutlineSystemService outlines)
        {
            _outlines = outlines ?? throw new ArgumentNullException(nameof(outlines));
        }

        internal void Tick(StageNodeView nodes, HashSet<int> availableNodes, int hoveredNodeId,
            bool showAvailable, StageNodeHoverStyleData style, float deltaTime)
        {
            bool available = availableNodes.Contains(hoveredNodeId);
            Color hoverColor = available ? style.AvailableOutlineColor : style.UnavailableOutlineColor;
            float hoverWidth = available ? style.AvailableOutlineWidth : style.UnavailableOutlineWidth;
            if (showAvailable && style.IdleOutlineWidth > 0f)
            {
                foreach (int nodeId in availableNodes)
                {
                    // 호버 폭 0으로 끈 대상은 기본 윤곽선을 만들었다 지우기를 반복하지 않습니다.
                    if (nodeId == hoveredNodeId && hoverWidth == 0f) continue;
                    EnsureHighlight(nodeId, nodes.GetVisibleBuilding(nodeId), style.IdleOutlineColor);
                }
            }
            if (hoveredNodeId != 0 && hoverWidth > 0f)
                EnsureHighlight(hoveredNodeId, nodes.GetVisibleBuilding(hoveredNodeId), hoverColor);
            for (int i = _highlights.Count - 1; i >= 0; i--)
            {
                Highlight highlight = _highlights[i];
                bool visible = nodes.GetVisibleBuilding(highlight.NodeId) == highlight.Visual;
                bool hovered = visible && highlight.NodeId == hoveredNodeId;
                bool idle = visible && showAvailable && availableNodes.Contains(highlight.NodeId);
                Color color = hovered ? hoverColor : idle ? style.IdleOutlineColor : highlight.Transition.Color;
                float width = hovered ? hoverWidth : idle ? style.IdleOutlineWidth : 0f;
                highlight.Transition.SetTarget(color, width, style.TransitionDuration);
                if (highlight.Transition.Tick(deltaTime)) highlight.Apply();
                if (highlight.Transition.IsFinished && highlight.Transition.Width == 0f)
                {
                    highlight.Dispose();
                    _highlights.RemoveAt(i);
                }
            }
        }

        private void EnsureHighlight(int nodeId, GameObject visual, Color color)
        {
            if (visual == null) return;
            foreach (Highlight highlight in _highlights)
                if (highlight.Visual == visual) return;
            _highlights.Add(Create(nodeId, visual, color));
        }

        internal void Clear()
        {
            foreach (Highlight highlight in _highlights) highlight.Dispose();
            _highlights.Clear();
        }

        private Highlight Create(int nodeId, GameObject visual, Color color)
        {
            // Scope가 메시 아래 자식을 만들기 전에 원래 건물의 Renderer 목록을 고정합니다.
            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>();
            var highlight = new Highlight(nodeId, visual, color);
            try
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    if (!renderer.enabled) continue;
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null ||
                        !filter.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord3))
                        throw new InvalidOperationException(
                            $"[StageNodeOutlineView] {visual.name}/{renderer.name}: MeshFilter와 임포트된 Outline Normal(UV3)이 필요합니다.");
                    IOutlineScopService scope = _outlines.CreateOutlineScop(
                        new OutlineData(renderer.transform, Vector3.zero, Quaternion.identity));
                    if (scope == null)
                        throw new InvalidOperationException($"[StageNodeOutlineView] {visual.name}: 윤곽선 Scope 생성 실패.");
                    highlight.Scopes.Add(scope);
                    if (scope.ColorHandle == null)
                        throw new InvalidOperationException($"[StageNodeOutlineView] {visual.name}: 윤곽선 ColorHandle이 없습니다.");
                    scope.ColorHandle.SetOverrideData(0, PulseGap, 0f);
                }
                if (highlight.Scopes.Count == 0)
                    throw new InvalidOperationException($"[StageNodeOutlineView] {visual.name}: 활성 건물 메시가 없습니다.");
                highlight.Apply();
                return highlight;
            }
            catch
            {
                highlight.Dispose();
                throw;
            }
        }

        private sealed class Highlight : IDisposable
        {
            internal readonly int NodeId;
            internal readonly GameObject Visual;
            internal readonly List<IOutlineScopService> Scopes = new();
            internal readonly StageHighlightTransition Transition;

            internal Highlight(int nodeId, GameObject visual, Color color)
            {
                NodeId = nodeId;
                Visual = visual;
                Transition = new StageHighlightTransition(color, 0f);
            }

            internal void Apply()
            {
                foreach (IOutlineScopService scope in Scopes)
                {
                    scope.ColorHandle.SetOverrideData(0, ColorShaderPropertyKey.OutlineColor, Transition.Color);
                    scope.ColorHandle.SetOverrideData(0, OutlineWidth, Transition.Width);
                }
            }

            public void Dispose()
            {
                foreach (IOutlineScopService scope in Scopes) scope.Dispose();
                Scopes.Clear();
            }
        }
    }
}
