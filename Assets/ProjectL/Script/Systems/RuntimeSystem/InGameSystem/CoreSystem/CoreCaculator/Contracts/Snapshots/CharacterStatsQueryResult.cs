using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>보유 능력치 조회 한 번의 결과다. 항목 순서는 보유 목록 순서이며 화면 정렬은 View가 정한다.</summary>
    public sealed class CharacterStatsQueryResult
    {
        public CharacterStatsQueryStatus Status { get; }
        /// <summary>현재 Run의 결과다. 끝난 Run의 보유 정보도 조회되므로 화면은 이 값으로 구분한다.</summary>
        public RunOutcome RunOutcome { get; }
        public IReadOnlyList<OwnedCharacterStatsSnapshot> Items { get; }

        public CharacterStatsQueryResult(CharacterStatsQueryStatus status, RunOutcome runOutcome,
            IReadOnlyList<OwnedCharacterStatsSnapshot> items)
        {
            if (Enum.IsDefined(typeof(CharacterStatsQueryStatus), status) == false)
                throw new ArgumentOutOfRangeException(nameof(status));
            if (Enum.IsDefined(typeof(RunOutcome), runOutcome) == false)
                throw new ArgumentOutOfRangeException(nameof(runOutcome));
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (status == CharacterStatsQueryStatus.Ready ? items.Count == 0 : items.Count != 0)
                throw new ArgumentException("Ready만 항목을 가지며 Ready에는 항목이 하나 이상 필요합니다.", nameof(items));

            var copied = new OwnedCharacterStatsSnapshot[items.Count];
            var ids = new HashSet<CharacterInstanceId>();
            for (int i = 0; i < copied.Length; i++)
            {
                OwnedCharacterStatsSnapshot item = items[i];
                if (item == null || ids.Add(item.OwnedId) == false)
                    throw new ArgumentException("항목은 null이거나 보유 ID가 중복될 수 없습니다.", nameof(items));
                copied[i] = item;
            }

            Status = status;
            RunOutcome = runOutcome;
            Items = Array.AsReadOnly(copied);
        }
    }
}
