using System;
using TMPro;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 프리팹 아래의 Renderer가 유닛 모델(몸)의 일부인지 정하는 규칙 하나입니다(새 SSOT 설계 SN1).
    /// 체력 숫자 같은 문자(TMP), HUD(Canvas), 선택 커서처럼 몸체가 아닌 표시(<see cref="UnitModelBoundsExclusion"/>) 아래는 모델이 아닙니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 이 규칙을 부르는 곳은 다섯입니다. 모델 외형 층(반투명·플래시·디졸브·외곽선)과 몸 경계 측정(<see cref="FieldUnitView"/>),
    /// Cartoon 대체 재질(<see cref="CartoonModelMaterialFallback"/>), 상태가 정하는 모습(<see cref="UnitStatusAppearance"/>),
    /// 배치 고스트(<c>Display/Feedback/PlacementGhost</c>, 유닛 프리팹 복제본)입니다.
    /// </para>
    /// <para>
    /// 유닛 뿌리를 훑지만 일부러 이 규칙을 쓰지 않는 곳은 셋입니다. 진입 숨김(<c>Presentation/Entry/CombatEntryVisibility</c>)과
    /// 준비 숨김(<see cref="CombatUnitPreparationLease"/>)은 체력 숫자·커서까지 모든 표시를 가리고, 랙돌 화면 밖 갱신(<see cref="CombatUnitRagdoll"/>)은
    /// 뼈대를 따르는 SkinnedMeshRenderer만 봅니다.
    /// </para>
    /// </remarks>
    internal static class UnitModelParts
    {
        /// <summary>그 Renderer가 유닛 모델의 일부이면 참입니다.</summary>
        internal static bool Is(Renderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            return renderer.GetComponentInParent<TMP_Text>(true) == null
                && renderer.GetComponentInParent<Canvas>(true) == null
                && renderer.GetComponentInParent<UnitModelBoundsExclusion>(true) == null;
        }
    }
}
