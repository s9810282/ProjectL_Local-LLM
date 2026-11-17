using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    [Serializable]
    public sealed class MeteorGimmickAuthoring
    {
        [SerializeField] private bool enabled;
        [SerializeField, Min(2)] private int cycleControllerTurns = 4;
        [SerializeField, Min(1)] private int warningLeadControllerTurns = 2;
        [SerializeField, Min(1)] private int minimumMeteorCount = 2;
        [SerializeField, Min(1)] private int maximumMeteorCount = 4;
        [SerializeField, Min(1)] private int footprintSideLength = 3;
        [SerializeField, Range(0, 100)] private int maximumHealthDamagePercent = 60;
        [SerializeField, Min(1)] private int fireAffectedSideStarts = 3;
        [SerializeField, Min(1)] private int flameControllerTurns = 4;

        public MeteorGimmickData ToData() => !enabled ? null : new MeteorGimmickData(cycleControllerTurns,
            warningLeadControllerTurns, minimumMeteorCount, maximumMeteorCount, footprintSideLength,
            maximumHealthDamagePercent, fireAffectedSideStarts, flameControllerTurns);
    }
}
