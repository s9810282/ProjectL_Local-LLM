using System;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    [Serializable]
    public sealed class LavaChainGimmickAuthoring
    {
        [SerializeField] private bool enabled;
        [SerializeField] private Vector2Int initialHeadCell;
        [SerializeField, Min(2)] private int cycleControllerTurns = 3;
        [SerializeField, Min(1)] private int warningLeadControllerTurns = 1;
        [SerializeField, Min(1)] private int pushDistance = 1;

        public LavaChainGimmickData ToData() => !enabled ? null : new LavaChainGimmickData(
            new CellCoord(initialHeadCell.x, initialHeadCell.y), cycleControllerTurns, warningLeadControllerTurns, pushDistance);
    }
}
