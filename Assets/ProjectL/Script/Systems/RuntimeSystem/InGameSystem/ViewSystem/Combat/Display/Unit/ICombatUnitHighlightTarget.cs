using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 전투 밖의 강조(튜토리얼)가 유닛 표시 하나에서 읽고 빌리는 좁은 계약입니다. <see cref="FieldUnitView"/>가 구현하고,
    /// 표시 표의 <see cref="FieldUnitPresenter.TryGetHighlightTarget"/>가 개체 ID로 내줍니다.
    /// </summary>
    /// <remarks>
    /// 전투 연출 대여 계약(<see cref="ICombatUnitPresentationTarget"/>)과 따로 둡니다. 강조 쪽에만 필요한 경계·HUD 위치를 연출 계약에 섞지 않기 위해서입니다.
    /// 표시가 파괴된 뒤에도 부를 수 있으며, 그때 <see cref="IsActive"/>와 <see cref="TryGetModelBounds"/>는 false입니다.
    /// 나머지 멤버는 <see cref="IsActive"/>가 true일 때만 부릅니다.
    /// </remarks>
    internal interface ICombatUnitHighlightTarget
    {
        /// <summary>지금 묶인 개체 ID입니다. 풀에 돌아가면 비어 있습니다.</summary>
        EntityId EntityId { get; }

        /// <summary>같은 개체라도 모델을 다시 묶을 때마다 바뀌는 번호입니다. 경계·대여가 지난 모델의 것인지 가릅니다.</summary>
        int BodyTrackingVersion { get; }

        /// <summary>파괴되지 않았고 켜져 있는지입니다.</summary>
        bool IsActive { get; }

        /// <summary>유닛 루트의 로컬 → 월드 변환입니다.</summary>
        Matrix4x4 LocalToWorld { get; }

        /// <summary>유닛 HUD의 Transform입니다. HUD가 없으면 null입니다.</summary>
        Transform HudTransform { get; }

        /// <summary>지금 보이는 실제 모델의 월드 경계입니다. HUD·문자·파티클·경계 제외 표시는 포함하지 않습니다. 꺼졌거나 파괴됐으면 false입니다.</summary>
        bool TryGetModelBounds(out Bounds bounds);

        /// <summary>선택 상태를 바꾸지 않고 현재 모델 외곽선만 빌립니다. 이전 대여가 남았거나 꺼진 표시면 예외입니다.</summary>
        ICombatUnitOutline BeginOutline(Color color, float width);
    }
}
