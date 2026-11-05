using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 화면 좌표가 무엇을 가리키는지 알려 주는 좁은 계약입니다(설계 §7, E.1).
    /// CombatInteraction이 카메라·Transform을 모르고 순수 C#으로 남도록 씁니다.
    /// </summary>
    /// <remarks>구현: 씬 표시(<see cref="CombatSceneDisplay"/>), 테스트 가짜.</remarks>
    internal interface ICombatPointerPicker
    {
        /// <summary>화면 좌표 아래의 필드 칸입니다. 필드 밖이면 false입니다.</summary>
        /// <remarks>손패 선택은 유지 덱 카드(UI 입구)가 맡으므로 이 계약에는 손패 집기·손패 영역이 없습니다(View 단계 설계 W4).</remarks>
        bool TryPickCell(float screenX, float screenY, out CellCoord cell);
    }
}
