using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Haboob.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 사건 재생이 하붑 표시에 닿는 입구입니다(하붑 View 계획 HP-6). 표시 수명과 결과 끝 다시 맞추기는 표시 쪽이 맡습니다.
    /// </summary>
    internal interface ICombatHaboobField
    {
        /// <summary>하붑 표현 설정입니다.</summary>
        HaboobVisualData Settings { get; }

        /// <summary>구름 칸을 이 목록에 맞춥니다. 그대로인 칸의 연기는 건드리지 않습니다.</summary>
        void Show(IReadOnlyList<CellCoord> cells, FieldGridMapper mapper);

        /// <summary>
        /// 구름을 <paramref name="to"/> 칸으로 옮기기 시작합니다(설계 §9.4). 표시 칸은 이 순간 새 칸이고 연기만 옛 자리에 남아 <see cref="TickMove"/>로 미끄러집니다.
        /// 옮길 연기가 없으면 새 칸에 바로 맞추고 false입니다.
        /// </summary>
        bool BeginMove(IReadOnlyList<CellCoord> from, IReadOnlyList<CellCoord> to, FieldGridMapper mapper);

        /// <summary>옮기는 연기를 진행률([0, 1])의 자리에 둡니다.</summary>
        void TickMove(float progress);
    }
}
