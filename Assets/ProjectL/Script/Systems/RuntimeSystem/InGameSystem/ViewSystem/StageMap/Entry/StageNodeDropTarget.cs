using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>건물 외형의 자세와 Renderer 표시만 빌립니다. 노드 활성 정책과 클릭 영역은 소유하지 않습니다.</summary>
    internal sealed class StageNodeDropTarget
    {
        private readonly Vector3 _localPosition;
        private readonly Quaternion _localRotation;
        private readonly Vector3 _localScale;
        private readonly Renderer[] _renderers;
        private readonly bool[] _enabled;

        internal StageNodeDropTarget(int nodeId, Transform visual, Pose groundPose)
        {
            if (nodeId <= 0) throw new ArgumentOutOfRangeException(nameof(nodeId));
            Visual = visual != null ? visual : throw new ArgumentNullException(nameof(visual));
            NodeId = nodeId;
            GroundPose = groundPose;
            LandingPosition = visual.position;
            _localPosition = visual.localPosition;
            _localRotation = visual.localRotation;
            _localScale = visual.localScale;
            _renderers = visual.GetComponentsInChildren<Renderer>(true);
            _enabled = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _enabled[i] = _renderers[i].enabled;
        }

        internal int NodeId { get; }
        internal Transform Visual { get; }
        internal Pose GroundPose { get; }
        internal Vector3 LandingPosition { get; }

        internal void Hide()
        {
            foreach (Renderer renderer in _renderers)
                if (renderer != null) renderer.enabled = false;
        }

        internal void Reveal()
        {
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].enabled = _enabled[i];
        }

        internal void ApplyHeightOffset(Vector3 worldUp, float worldHeight)
        {
            if (Visual == null)
                throw new InvalidOperationException($"[StageNodeDropTarget] 낙하 중 외형이 사라졌습니다: {NodeId}");
            Visual.position = LandingPosition + worldUp * worldHeight;
        }

        internal void Restore()
        {
            if (Visual != null)
            {
                Visual.localPosition = _localPosition;
                Visual.localRotation = _localRotation;
                Visual.localScale = _localScale;
            }
            Reveal();
        }
    }
}
