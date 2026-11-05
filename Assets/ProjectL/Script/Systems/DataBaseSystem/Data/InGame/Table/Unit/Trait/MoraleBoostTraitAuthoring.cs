using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class MoraleBoostTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int defeatedEnemyBaseAttackPercent = 20;
        [SerializeField, Min(1)] private int durationControllerTurnEnds = 1;

        internal override UnitTraitData ToData() => new MoraleBoostTraitData(defeatedEnemyBaseAttackPercent, durationControllerTurnEnds);
    }
}
