using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>넥서스가 전투 동안 공급할 유닛 종류와 수량이다.</summary>
    public sealed class NexusReinforcementEntry
    {
        public EntityKey UnitKey { get; }
        public int Count { get; }

        public NexusReinforcementEntry(EntityKey unitKey, int count)
        {
            if (unitKey == EntityKey.None || !Enum.IsDefined(typeof(EntityKey), unitKey))
                throw new ArgumentOutOfRangeException(nameof(unitKey));
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            UnitKey = unitKey;
            Count = count;
        }
    }

    /// <summary>전투 시작 때 고정되는 넥서스 증원 설정이다.</summary>
    public sealed class NexusReinforcementSpec
    {
        public const int DefaultFieldEnemyLimit = 5;
        public const int DefaultSpawnCountPerTurn = 2;
        public const int DefaultRangePadding = 2;
        public const int DefaultSpawnIntervalTurns = 1;

        public IReadOnlyList<NexusReinforcementEntry> Pool { get; }
        public int SpawnCountPerTurn { get; }
        public int RangePadding { get; }
        public int EnemyLevel { get; }
        /// <summary>적 턴 시작 때 필드의 살아 있는 적이 이 수 이상이면 증원하지 않는다. 초기 배치 수는 제한하지 않는다.</summary>
        public int FieldEnemyLimit { get; }
        public int SpawnIntervalTurns { get; }

        public NexusReinforcementSpec(IReadOnlyList<NexusReinforcementEntry> pool, int enemyLevel,
            int spawnCountPerTurn = DefaultSpawnCountPerTurn, int rangePadding = DefaultRangePadding,
            int fieldEnemyLimit = DefaultFieldEnemyLimit, int spawnIntervalTurns = DefaultSpawnIntervalTurns)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (enemyLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(enemyLevel));
            if (spawnCountPerTurn < 1)
                throw new ArgumentOutOfRangeException(nameof(spawnCountPerTurn));
            if (rangePadding < 1)
                throw new ArgumentOutOfRangeException(nameof(rangePadding));
            if (fieldEnemyLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(fieldEnemyLimit));
            if (spawnIntervalTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(spawnIntervalTurns));

            var copy = new NexusReinforcementEntry[pool.Count];
            var keys = new HashSet<EntityKey>();
            int total = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                var entry = pool[i] ?? throw new ArgumentException("증원 풀에 빈 항목을 넣을 수 없습니다.", nameof(pool));
                if (!keys.Add(entry.UnitKey))
                    throw new ArgumentException($"증원 풀에 유닛 키가 중복됩니다: {entry.UnitKey}.", nameof(pool));
                if (entry.Count > int.MaxValue - total)
                    throw new ArgumentException("증원 풀의 전체 수량이 int 범위를 초과합니다.", nameof(pool));

                total += entry.Count;
                copy[i] = entry;
            }

            Array.Sort(copy, (left, right) => left.UnitKey.CompareTo(right.UnitKey));
            Pool = Array.AsReadOnly(copy);
            SpawnCountPerTurn = spawnCountPerTurn;
            RangePadding = rangePadding;
            EnemyLevel = enemyLevel;
            FieldEnemyLimit = fieldEnemyLimit;
            SpawnIntervalTurns = spawnIntervalTurns;
        }
    }
}
