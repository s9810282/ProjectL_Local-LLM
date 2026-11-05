using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    [Serializable]
    public sealed class HaboobGimmickAuthoring
    {
        [SerializeField] private bool enabled;
        [SerializeField, Min(1)] private int relocationIntervalControllerTurns = 2;
        [SerializeField, Min(1)] private int warningLeadControllerTurns = 1;
        [SerializeField] private CellInput[] footprintOffsets = new HaboobGimmickSpec().FootprintOffsets
            .Select(c => new CellInput(c.X, c.Y)).ToArray();
        [Tooltip("켜면 전투 시작 때 영역 모양의 상대 좌표 (0,0)을 아래 칸에 놓습니다. 끄면 무작위입니다. 이동(재배치)은 늘 무작위입니다.")]
        [SerializeField] private bool fixedInitialAnchor;
        [Tooltip("모양의 상대 좌표 (0,0)이 놓일 보드 칸입니다. 모양 전체가 판 안에 들어가야 하며, 판 크기 검사는 전투 진입 때 합니다.")]
        [SerializeField] private CellInput initialAnchor;

        public HaboobGimmickData ToData() => !enabled ? null : new HaboobGimmickData(
            relocationIntervalControllerTurns, warningLeadControllerTurns, CellInput.ToCellCoords(footprintOffsets),
            fixedInitialAnchor ? initialAnchor.ToCellCoord() : (CellCoord?)null);
    }
}
