using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 SO에서 입력하는 자폭 단계입니다. 적 전용(EnemyOnly) 유닛에만 넣을 수 있습니다.</summary>
    [Serializable]
    internal sealed class SelfDestructTraitAuthoring : UnitTraitAuthoring
    {
        [Tooltip("사망 직전 공격력 대비 폭발 피해. 1: 50%, 2: 75%, 3: 110%.")]
        [SerializeField, Range(1, 3)] private int level = 1;

        internal override UnitTraitData ToData() => new SelfDestructTraitData(level);
    }
}
