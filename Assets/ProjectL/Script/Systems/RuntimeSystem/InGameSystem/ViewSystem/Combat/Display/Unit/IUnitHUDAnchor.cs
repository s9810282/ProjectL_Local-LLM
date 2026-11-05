using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>유닛 표시가 소유한 HUD 기준점과 표시 의도입니다. 카메라와 화면 좌표는 알지 못합니다.</summary>
    public interface IUnitHUDAnchor
    {
        Vector3 HudWorldPosition { get; }
        bool IsHudVisible { get; }

        /// <summary>
        /// HUD 층이 숨겨졌습니다. HUD는 층의 직접 자식이라 함께 꺼지며 표시 상태(체력 등)를 지우므로,
        /// 기준점은 HUD에 쓴 내용의 기억을 버려 다음 그리기가 모두 다시 쓰게 합니다(GC 끊김 계획 §3.7 L1). 층을 바꾸지 않습니다.
        /// </summary>
        void OnHudLayerHidden();
    }
}
