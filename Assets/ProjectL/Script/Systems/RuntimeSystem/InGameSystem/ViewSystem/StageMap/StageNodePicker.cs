using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>씬 카메라의 레이로 표시 중인 노드를 찾습니다. 상태를 갖지 않고 카메라를 움직이지 않습니다.</summary>
    /// <remarks>
    /// 레이는 호출할 때마다 카메라의 현재 위치로 만들므로 Play 중 카메라를 움직여도 맞습니다.
    /// 카메라 제공 방식이 정해지면 이 피커를 만드는 곳만 바꿉니다.
    /// </remarks>
    internal sealed class StageNodePicker : IStageNodePicker
    {
        private readonly Camera _camera;
        private readonly StageMapViewDisplay _display;
        private readonly LayerMask _nodeMask;
        private readonly float _pickDistance;

        internal StageNodePicker(Camera camera, StageMapViewDisplay display, LayerMask nodeMask, float pickDistance)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            _camera = camera;
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _nodeMask = nodeMask;
            _pickDistance = pickDistance;
        }

        public bool TryPickNode(float screenX, float screenY, out int nodeId)
        {
            nodeId = 0;
            if (_camera == null)
                return false;

            Ray ray = _camera.ScreenPointToRay(new Vector3(screenX, screenY, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, _pickDistance, _nodeMask, QueryTriggerInteraction.Collide))
                return false;

            return _display.TryGetNodeId(hit.collider.transform, out nodeId);
        }
    }
}
