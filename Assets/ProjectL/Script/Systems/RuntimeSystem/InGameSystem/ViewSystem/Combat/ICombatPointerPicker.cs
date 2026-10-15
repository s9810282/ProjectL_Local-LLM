using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 화면 좌표가 무엇을 가리키는지 알려 주는 좁은 계약입니다(설계 §7, E.1).
    /// CombatInteraction이 카메라·Transform을 모르고 순수 C#으로 남도록 씁니다.
    /// </summary>
    /// <remarks>구현: 씬 표시(<see cref="CombatSceneDisplay"/>), 테스트 가짜.</remarks>
    internal interface ICombatPointerPicker
    {
        /// <summary>화면 좌표 아래의 필드 칸입니다. 필드 밖이면 false입니다.</summary>
        bool TryPickCell(float screenX, float screenY, out CellCoord cell);

        /// <summary>화면 좌표 아래의 손패 유닛입니다. 없으면 false입니다.</summary>
        bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId);

        /// <summary>화면 좌표가 손패로 되돌리는 Drop 영역 안인지입니다.</summary>
        bool IsOverHandDropZone(float screenX, float screenY);
    }
}
