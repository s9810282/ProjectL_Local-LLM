using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class DustScreenTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int attackRangeReduction = 1;
        [SerializeField, Range(0, 100)] private int attackDamageReductionPercent = 20;
        [SerializeField, Min(1)] private int opposingTurnEndsToExpire = 1;
        [SerializeField] private bool exemptsAlliedAttackRangePenalty;

        internal override UnitTraitData ToData() => new DustScreenTraitData(attackRangeReduction, attackDamageReductionPercent,
            opposingTurnEndsToExpire, exemptsAlliedAttackRangePenalty);
    }
}
