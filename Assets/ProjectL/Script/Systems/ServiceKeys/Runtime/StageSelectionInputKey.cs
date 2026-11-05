using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.ServiceKeys.Runtime
{
    internal static class StageSelectionInputKey
    {
        /// <summary>
        /// 맵 선택 View가 입력을 받으려고 붙는 수신 슬롯입니다. 지금 이 키로 등록·조회하는 곳은 없고,
        /// 슬롯은 <c>InGameViewInputContext.StageSelection</c>으로 전달됩니다(검사 후속 정리 R-f F6).
        /// </summary>
        public static readonly SystemGroupKey<IStageSelectionReceiverSlot> ReceiverSlot =
            new(nameof(ReceiverSlot));
    }
}
