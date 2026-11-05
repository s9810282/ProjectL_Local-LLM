using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class HunterTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Tooltip("미입력은 -1입니다. 사용할 수치를 명시해야 합니다.")] private int attackPerTargetKill = -1;
        [SerializeField, Min(1)] private int maxRewardsPerStage = 3;

        internal override UnitTraitData ToData() => new HunterTraitData(attackPerTargetKill, maxRewardsPerStage);
    }
}
