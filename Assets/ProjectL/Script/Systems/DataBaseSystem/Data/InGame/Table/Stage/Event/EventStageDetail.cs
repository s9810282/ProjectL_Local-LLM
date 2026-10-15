using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event
{
    /// <summary>
    /// 회복 스테이지 상세 데이터입니다(<see cref="StageType.Event"/> = 회복, 설계 E1).
    /// Core가 받는 순수 C# 데이터이며 EventStageData SO가 생성합니다.
    /// Core RecoveryStageSpec(stageKey, options)에 대응하며, 선택지는 RecoveryOptionTable의 키로 가리킵니다.
    /// </summary>
    public sealed class EventStageDetail : IStageData
    {
        public StageType StageType => StageType.Event;

        public IReadOnlyList<int> RecoveryOptionKeys { get; }

        public EventStageDetail(int[] recoveryOptionKeys)
        {
            RecoveryOptionKeys = new ReadOnlyCollection<int>(recoveryOptionKeys ?? Array.Empty<int>());
        }

        public void Validate()
        {
            // Core RecoveryStageSpec과 같이 선택지가 하나 이상이고 서로 달라야 합니다.
            if (RecoveryOptionKeys.Count == 0)
                throw new InvalidOperationException("[EventStageDetail] Recovery stage requires at least one option.");

            var keys = new HashSet<int>();
            foreach (int key in RecoveryOptionKeys)
            {
                if (key < 1)
                    throw new InvalidOperationException("[EventStageDetail] Recovery option is missing.");
                if (!keys.Add(key))
                    throw new InvalidOperationException($"[EventStageDetail] Duplicate recovery option: {key}.");
            }
        }
    }
}
