using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>
    /// 화면 전환 Scop에 전달하는 요청 값입니다.
    /// 속도는 초당 진행량이며 목표 변화량에 도달하면 진행을 완료합니다.
    /// </summary>
    public struct ScreenTransitionData
    {
        public LayerMask Layer;
        /// <summary>null이면 현재 카메라를 유지하며 카메라 조회와 우선순위 변경을 하지 않습니다.</summary>
        public string TargetVirtureCamera;
        public Color StencileMaskeColor;
        public float ChangeSpeed;
        public float TargetChangeAmount;
    }
}
