using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 SO에서 입력하는 재구성 단계입니다. 적 전용(EnemyOnly) 유닛에만 넣을 수 있습니다.</summary>
    [Serializable]
    internal sealed class ReconstructionTraitAuthoring : UnitTraitAuthoring
    {
        [Tooltip("1: 체력 최대치로 재구성. 2: 최대 체력 50% 보호막 추가. 3: 부활할 때 주변 8칸을 1칸 밀어냄(2 포함).")]
        [SerializeField, Range(1, 3)] private int level = 1;

        internal override UnitTraitData ToData() => new ReconstructionTraitData(level);
    }
}
