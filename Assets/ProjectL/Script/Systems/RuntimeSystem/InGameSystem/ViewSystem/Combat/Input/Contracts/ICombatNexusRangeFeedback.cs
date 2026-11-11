using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>적 본부 호버 동안 증원 공급 범위만 표시하는 창구입니다.</summary>
    internal interface ICombatNexusRangeFeedback
    {
        void ShowNexusRange(IReadOnlyList<CellCoord> cells);

        void ClearNexusRange();
    }
}
