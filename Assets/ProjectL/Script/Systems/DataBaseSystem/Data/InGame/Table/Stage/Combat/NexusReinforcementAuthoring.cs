using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    [Serializable]
    public sealed class NexusReinforcementAuthoring
    {
        [Serializable]
        private struct PoolRow
        {
            [SerializeField] private UnitDataSO unitDefinition;
            [SerializeField, Min(1)] private int count;

            public NexusReinforcementEntry ToEntry()
            {
                if (unitDefinition == null)
                    throw new InvalidOperationException("증원 풀 항목에 유닛 정의가 필요합니다.");

                return new NexusReinforcementEntry(unitDefinition.Key, count);
            }
        }

        [SerializeField] private bool enabled;
        [SerializeField, Min(1)] private int spawnCountPerTurn = NexusReinforcementSpec.DefaultSpawnCountPerTurn;
        [SerializeField, Min(1)] private int rangePadding = NexusReinforcementSpec.DefaultRangePadding;
        [Tooltip("적 턴 시작 때 필드의 살아 있는 적이 이 수 이상이면 증원하지 않습니다. 초기 배치 수는 제한하지 않습니다.")]
        [SerializeField, Min(1)] private int fieldEnemyLimit = NexusReinforcementSpec.DefaultFieldEnemyLimit;
        [Tooltip("적 턴 단위의 증원 간격입니다. 설정한 수의 적 턴째에 처음 시도합니다. 막히면 다음 적 턴마다 재시도하고, 한 기라도 생성하면 그 턴부터 간격을 다시 셉니다.")]
        [SerializeField, Min(1)] private int spawnIntervalTurns = NexusReinforcementSpec.DefaultSpawnIntervalTurns;
        [SerializeField] private PoolRow[] pool = Array.Empty<PoolRow>();

        public NexusReinforcementSpec ToSpec(int enemyLevel)
        {
            if (!enabled)
                return null;

            var entries = new NexusReinforcementEntry[pool?.Length ?? 0];
            for (int i = 0; i < entries.Length; i++)
                entries[i] = pool[i].ToEntry();

            return new NexusReinforcementSpec(entries, enemyLevel, spawnCountPerTurn, rangePadding, fieldEnemyLimit,
                spawnIntervalTurns);
        }
    }
}
