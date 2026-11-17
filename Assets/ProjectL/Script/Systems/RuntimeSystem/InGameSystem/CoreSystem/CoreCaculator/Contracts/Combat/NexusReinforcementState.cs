using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>증원으로 생성된 개체와 소모한 풀 항목을 연결한다.</summary>
    public sealed class NexusReinforcementRecord
    {
        public EntityId EntityId { get; }
        public int PoolEntryIndex { get; }

        public NexusReinforcementRecord(EntityId entityId, int poolEntryIndex)
        {
            if (entityId.IsEmpty)
                throw new ArgumentException("증원 개체 ID가 필요합니다.", nameof(entityId));
            if (poolEntryIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(poolEntryIndex));

            EntityId = entityId;
            PoolEntryIndex = poolEntryIndex;
        }
    }

    /// <summary>전투마다 고정한 넥서스와 증원 난수 및 생성 이력이다. 남은 수량은 이력에서 계산한다.</summary>
    public sealed class NexusReinforcementState
    {
        public static NexusReinforcementState Disabled { get; } = new NexusReinforcementState();
        public EntityId NexusId { get; }
        public uint RandomState { get; }
        /// <summary>마지막 증원 성공 적 턴이다. 아직 성공하지 않았다면 0이다.</summary>
        public long LastSpawnEnemyTurn { get; }
        public IReadOnlyList<NexusReinforcementRecord> Records { get; }
        public bool IsEnabled => !NexusId.IsEmpty;

        private NexusReinforcementState()
        {
            Records = Array.AsReadOnly(Array.Empty<NexusReinforcementRecord>());
        }

        public NexusReinforcementState(EntityId nexusId, uint randomState, IReadOnlyList<NexusReinforcementRecord> records,
            long lastSpawnEnemyTurn = 0)
        {
            if (nexusId.IsEmpty)
                throw new ArgumentException("증원 원본인 넥서스 ID가 필요합니다.", nameof(nexusId));
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (lastSpawnEnemyTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(lastSpawnEnemyTurn));
            if ((records.Count == 0) != (lastSpawnEnemyTurn == 0))
                throw new ArgumentException("증원 이력과 마지막 성공 턴이 일치하지 않습니다.", nameof(lastSpawnEnemyTurn));

            var ids = new HashSet<EntityId> { nexusId };
            var copy = new NexusReinforcementRecord[records.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                var record = records[i];
                if (record == null || !ids.Add(record.EntityId))
                    throw new ArgumentException("증원 이력은 비어 있거나 개체 ID가 중복될 수 없습니다.", nameof(records));

                copy[i] = record;
            }

            NexusId = nexusId;
            RandomState = randomState;
            LastSpawnEnemyTurn = lastSpawnEnemyTurn;
            Records = Array.AsReadOnly(copy);
        }

        public bool SameValues(NexusReinforcementState other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other == null || NexusId != other.NexusId || RandomState != other.RandomState ||
                LastSpawnEnemyTurn != other.LastSpawnEnemyTurn || Records.Count != other.Records.Count)
                return false;

            for (int i = 0; i < Records.Count; i++)
                if (Records[i].EntityId != other.Records[i].EntityId || Records[i].PoolEntryIndex != other.Records[i].PoolEntryIndex)
                    return false;

            return true;
        }

        public bool Contains(EntityId entityId) => Records.Any(record => record.EntityId == entityId);

        /// <summary>성공 턴부터 간격이 지났는지 판정한다. 실패한 시도는 간격을 다시 시작하지 않는다.</summary>
        public bool IsSpawnDue(NexusReinforcementSpec spec, long enemyTurn)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (enemyTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(enemyTurn));
            if (LastSpawnEnemyTurn > enemyTurn)
                throw new InvalidOperationException("마지막 증원 성공 턴이 현재 적 턴보다 늦습니다.");

            return IsEnabled && enemyTurn > 0 && enemyTurn - LastSpawnEnemyTurn >= spec.SpawnIntervalTurns;
        }

        public int RemainingCount(NexusReinforcementSpec spec, int poolEntryIndex)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (poolEntryIndex < 0 || poolEntryIndex >= spec.Pool.Count)
                throw new ArgumentOutOfRangeException(nameof(poolEntryIndex));

            return CopyRemainingCounts(spec)[poolEntryIndex];
        }

        public int RemainingTotal(NexusReinforcementSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return CopyRemainingCounts(spec).Sum();
        }

        internal int[] CopyRemainingCounts(NexusReinforcementSpec spec)
        {
            var remaining = spec.Pool.Select(entry => entry.Count).ToArray();
            foreach (var record in Records)
            {
                int index = record.PoolEntryIndex;
                if (index >= remaining.Length || --remaining[index] < 0)
                    throw new InvalidOperationException("증원 이력이 준비된 풀의 항목 또는 수량과 맞지 않습니다.");
            }

            return remaining;
        }
    }
}
