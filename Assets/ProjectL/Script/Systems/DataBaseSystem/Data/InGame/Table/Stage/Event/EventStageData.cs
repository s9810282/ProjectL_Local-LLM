using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event
{
    /// <summary>
    /// 회복 스테이지(EventStageDetail)의 입력 SO입니다. <see cref="StageType.Event"/>를 회복으로 씁니다(설계 E1).
    /// 선택지는 RecoveryOptionDataSO를 끌어다 놓고, CreateStageData에서 OptionId로 바꿉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EventStageData", menuName = "ProjectL/Database/InGame/Stage/Event (Recovery)")]
    public sealed class EventStageData : StageBaseData
    {
        [SerializeField] private RecoveryOptionDataSO[] recoveryOptions = new RecoveryOptionDataSO[0];

        public override StageType StageType => StageType.Event;

        protected override IStageData CreateStageData()
        {
            var keys = new int[recoveryOptions?.Length ?? 0];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = recoveryOptions[i] != null ? recoveryOptions[i].OptionId : 0;
            return new EventStageDetail(keys);
        }
    }
}
