using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;

namespace ProjectL.Systems.ServiceKeys.Runtime
{
    internal static class ArtifactInputKey
    {
        /// <summary>
        /// 아티팩트 View가 입력을 받으려고 붙는 수신 슬롯입니다. 지금 이 키로 등록·조회하는 곳은 없고,
        /// 슬롯은 <c>InGameViewInputContext.Artifact</c>로 전달됩니다(로비 리팩토링 2차 설계 N-16, Q-R2-7 B).
        /// </summary>
        public static readonly SystemGroupKey<IArtifactInputSlot> ReceiverSlot =
            new(nameof(ReceiverSlot));
    }
}
