using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>충돌 중심 바깥으로 밀어낼 거리를 입력합니다.</summary>
    [Serializable]
    internal sealed class ImpactBoltTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int distanceCells = 1;

        internal override UnitTraitData ToData() => new ImpactBoltTraitData(distanceCells);
    }
}
