using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>패널의 빈 영역 클릭이 부모의 버튼 입력으로 전파되지 않게 한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UIPointerClickBoundary : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData) { }
    }
}
