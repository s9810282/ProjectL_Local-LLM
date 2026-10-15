using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.ServiceKeys
{
    internal static class StageSelectionInputKey
    {
        /// <summary>맵 선택 View가 입력을 받으려고 붙는 수신 슬롯입니다. 루트에 등록됩니다.</summary>
        public static readonly SystemGroupKey<IStageSelectionReceiverSlot> ReceiverSlot =
            new(nameof(ReceiverSlot));
    }
}
