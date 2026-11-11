using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 포인터가 올라간 칸과 그 칸의 가능 여부 표시 의도만 전하는 계약입니다(설계 §7, E.2).
    /// </summary>
    /// <remarks>
    /// 옛 <c>ICombatInteractionFeedback</c>을 쓰는 쪽마다 나눈 가운데 호버 칸 몫입니다(씬 표시 나누기 설계 DS2, 결정 Q-DS2 A).
    /// 호버 추적(<see cref="CombatHoverTracker"/>)이 쓰고, 배치 Drag·공격 조준·아이템 Drag가 그 추적에 넘깁니다.
    /// 구현: 필드 표시(<c>FieldView</c>). 필드가 구성되지 않았으면(설치 전·정리·해제 뒤) 아무것도 하지 않습니다.
    /// </remarks>
    internal interface ICombatHoverCellFeedback
    {
        /// <summary>포인터가 올라간 칸과 그 칸의 가능 여부 표시입니다. 종류는 색만 가르며 공격 조준은 공격 호버 색을 씁니다.</summary>
        void SetHoverCell(CellCoord cell, CombatHoverState state, CombatHoverKind kind);

        void ClearHoverCell();
    }
}
