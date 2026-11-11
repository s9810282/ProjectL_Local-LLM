using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>작렬의 날아가는 거리와 부딪힘 피해 비율을 입력합니다.</summary>
    [Serializable]
    internal sealed class CrashImpactTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1), Tooltip("피해를 입힌 대상이 일직선으로 날아가는 최대 칸 수입니다.")]
        private int distanceCells = 3;

        [SerializeField, Range(0, 100), Tooltip("벽·맵 끝·고지대 턱·구조물에 부딪히면 날아간 대상이 받는 피해입니다. 날아간 대상 최대 체력의 %입니다.")]
        private int wallDamagePercent = 10;

        [SerializeField, Range(0, 100), Tooltip("유닛에 부딪히면 날아간 대상과 부딪힌 유닛이 각각 받는 피해입니다. 날아간 대상 최대 체력의 %입니다.")]
        private int unitDamagePercent = 10;

        internal override UnitTraitData ToData() => new CrashImpactTraitData(distanceCells, wallDamagePercent, unitDamagePercent);
    }
}
