using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    /// <summary>스테이지 SO의 플랫폼 입력을 순수 불변 정의로 변환한다.</summary>
    [Serializable]
    internal sealed class MovingPlatformAuthoring
    {
        [SerializeField, Min(1)] private long sourceId;
        [SerializeField, Range(1, 3)] private int length = 1;
        [Tooltip("기준 칸에서 양의 방향으로 점유 칸을 늘리는 축입니다. 이동 축은 경로 양 끝에서 결정됩니다.")]
        [SerializeField] private PlatformAxis shapeAxis = PlatformAxis.Horizontal;
        [SerializeField] private CellInput routeStart;
        [SerializeField] private CellInput routeEnd;
        [SerializeField] private CellInput initialAnchor;
        [Tooltip("이동 축 좌표가 증가하면 1, 감소하면 -1입니다.")]
        [SerializeField] private int initialSign = 1;

        internal MovingPlatformSpec ToSpec() => new MovingPlatformSpec(sourceId, length, shapeAxis,
            routeStart.ToCellCoord(), routeEnd.ToCellCoord(), initialAnchor.ToCellCoord(), initialSign);
    }
}
