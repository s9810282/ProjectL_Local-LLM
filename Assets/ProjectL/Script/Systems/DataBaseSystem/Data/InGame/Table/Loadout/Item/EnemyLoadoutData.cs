using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item
{
    /// <summary>적 출현 후보 하나입니다. Weight는 추첨 가중치입니다.</summary>
    public readonly struct EnemyLoadoutEntry
    {
        public EntityKey UnitKey { get; }
        public int Weight { get; }

        public EnemyLoadoutEntry(EntityKey unitKey, int weight)
        {
            UnitKey = unitKey;
            Weight = weight;
        }
    }

    /// <summary>
    /// 스테이지 적 구성 규칙입니다. Core가 받는 순수 C# 데이터이며 EnemyLoadoutDataSO가 생성합니다.
    /// </summary>
    public sealed class EnemyLoadoutData : IData
    {
        public int LoadoutKey { get; }
        public IReadOnlyList<EnemyLoadoutEntry> Entries { get; }
        public int SpawnCount { get; }
        public int RandomSeed { get; }

        public EnemyLoadoutData(int loadoutKey, EnemyLoadoutEntry[] entries, int spawnCount, int randomSeed)
        {
            LoadoutKey = loadoutKey;
            Entries = new ReadOnlyCollection<EnemyLoadoutEntry>(entries ?? Array.Empty<EnemyLoadoutEntry>());
            SpawnCount = spawnCount;
            RandomSeed = randomSeed;
        }

        public void Validate()
        {
            if (LoadoutKey < 1)
                throw new InvalidOperationException("[EnemyLoadoutData] LoadoutKey must be positive.");
            if (SpawnCount < 0)
                throw new InvalidOperationException($"[EnemyLoadoutData] SpawnCount must not be negative. LoadoutKey: {LoadoutKey}");
            foreach (EnemyLoadoutEntry entry in Entries)
            {
                if (entry.UnitKey == EntityKey.None)
                    throw new InvalidOperationException($"[EnemyLoadoutData] Entry unit is missing. LoadoutKey: {LoadoutKey}");
                if (entry.Weight < 1)
                    throw new InvalidOperationException($"[EnemyLoadoutData] Entry weight must be at least 1. LoadoutKey: {LoadoutKey}");
            }
        }
    }
}
