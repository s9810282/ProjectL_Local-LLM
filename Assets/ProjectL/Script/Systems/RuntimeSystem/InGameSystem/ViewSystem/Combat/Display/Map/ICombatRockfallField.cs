using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 사건 재생이 낙석 표시에 닿는 입구입니다(낙석 View 계획 RP-4). 표시 수명과 결과 끝 다시 맞추기는 표시 쪽이 맡습니다.
    /// </summary>
    internal interface ICombatRockfallField
    {
        /// <summary>낙석 표현 설정입니다.</summary>
        RockfallVisualData Settings { get; }

        /// <summary>맵 프랍 안 터널 폭발 자리의 월드 위치·회전입니다(계획 RP-12). 스테이지에서 폭발을 끄거나 그 오브젝트가 없으면 정상적으로 거짓을 반환합니다(편집 설계 RTE).</summary>
        bool TryGetTunnelPose(out Pose pose);

        /// <summary>돌이 출발하는 능선 두 끝점의 월드 위치입니다(낙석 View 설계 RG2). 끝점이 하나라도 없으면 거짓입니다.</summary>
        bool TryGetRidge(out Vector3 start, out Vector3 end);

        /// <summary>떨어질 칸에 테두리·채우기·느낌표를 켭니다.</summary>
        void ShowWarning(IReadOnlyList<CellCoord> cells, FieldGridMapper mapper);

        /// <summary>경고 칸 표시를 끕니다.</summary>
        void ClearWarning();

        /// <summary>떨어뜨릴 돌을 빌립니다. 크기는 프리팹 크기 × <paramref name="scale"/>입니다. 낙하 동안만 쓰고 <see cref="ReturnStone"/>으로 돌려줍니다.</summary>
        GameObject RentStone(float scale);

        /// <summary>돌을 돌려줘 화면에서 없앱니다.</summary>
        void ReturnStone(GameObject stone);
    }
}
