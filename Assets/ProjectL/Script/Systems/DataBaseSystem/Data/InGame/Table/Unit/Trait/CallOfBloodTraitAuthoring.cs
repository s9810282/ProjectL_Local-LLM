using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class CallOfBloodTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Range(0, 100)] private int enemyMaxHealthHealPercent = 10;

        internal override UnitTraitData ToData() => new CallOfBloodTraitData(enemyMaxHealthHealPercent);
    }
}
