using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    [Serializable]
    public sealed class RockfallGimmickAuthoring
    {
        [SerializeField] private bool enabled;
        [SerializeField, Min(1)] private int cooldownControllerTurns = 2;
        [SerializeField, Min(1)] private int warningControllerTurns = 2;
        [SerializeField, Min(1)] private int minimumRockCount = 3;
        [SerializeField, Min(1)] private int maximumRockCount = 5;
        [SerializeField, Range(0, 100)] private int maximumHealthLossPercent = 20;
        [SerializeField, Min(1)] private int bindAffectedSideTurns = 1;
        [SerializeField] private CellInput[] footprintOffsets = { new CellInput(0, 0) };

        public RockfallGimmickData ToData() => !enabled ? null : new RockfallGimmickData(
            cooldownControllerTurns, warningControllerTurns, minimumRockCount, maximumRockCount,
            maximumHealthLossPercent, bindAffectedSideTurns, CellInput.ToCellCoords(footprintOffsets));
    }
}
