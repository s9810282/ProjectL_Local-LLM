using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;

namespace ProjectL.Systems.ServiceKeys
{
    internal static class ArtifactInputKey
    {
        /// <summary>아티팩트 View가 입력을 받으려고 붙는 수신 슬롯입니다. 루트에 등록됩니다.</summary>
        public static readonly SystemGroupKey<IArtifactInputSlot> ReceiverSlot =
            new(nameof(ReceiverSlot));
    }
}
