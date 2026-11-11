using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 선택 커서처럼 실제 몸체가 아닌 표시의 하위 Renderer를 유닛 모델에서 제외합니다. 모델 경계 측정과 모델 외형 층(반투명·플래시·디졸브·외곽선) 수집 모두에서 빠집니다.
    /// 판정은 <see cref="FieldUnitView"/>의 모델 일부 판정 한 곳이 합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class UnitModelBoundsExclusion : MonoBehaviour
    {
    }
}
