using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>기본 공격 사거리 밖에서 끌어올 추가 거리를 입력합니다.</summary>
    [Serializable]
    internal sealed class PullTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int extraRangeCells = 1;

        internal override UnitTraitData ToData() => new PullTraitData(extraRangeCells);
    }
}
