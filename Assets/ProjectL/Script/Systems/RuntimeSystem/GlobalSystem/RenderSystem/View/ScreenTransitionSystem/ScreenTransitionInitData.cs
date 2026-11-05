using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>런타임에서 한 번 저장하는 출력 이름·고정 레이어·원 설정입니다.</summary>
    internal struct ScreenTransitionInitData
    {
        public string MainCamera;
        public LayerMask FixedLayer;
        public Vector2 CircleCenter;
        public float TransitionWidth;
    }
}
