using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.ServiceKeys
{
    internal static class StageInputKey
    {
        /// <summary>스테이지 내부 View가 입력을 받으려고 붙는 수신 슬롯입니다. 루트에 등록됩니다.</summary>
        public static readonly SystemGroupKey<IStageInputReceiverSlot> ReceiverSlot =
            new(nameof(ReceiverSlot));
    }
}
