using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>완료 표시 목록에 적용된 선택을 읽습니다. 정보 대상에는 적도 포함됩니다.</summary>
    internal interface ICombatSelectionReader
    {
        EntityId SelectedEntityId { get; }
        EntityId SelectedActorId { get; }
        IReadOnlyList<CellCoord> SelectedActorCells { get; }
    }
}
