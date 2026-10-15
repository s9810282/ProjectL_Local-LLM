using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>DB에서 조립한 적 출현 후보 사실이다. 상태나 별도 저작 원본을 보관하지 않는다.</summary>
    public readonly struct EnemySpawnCandidate
    {
        public UnitSpec Definition { get; }
        public int Weight { get; }

        public EnemySpawnCandidate(UnitSpec definition, int weight)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
            Weight = weight;
        }
    }
}
