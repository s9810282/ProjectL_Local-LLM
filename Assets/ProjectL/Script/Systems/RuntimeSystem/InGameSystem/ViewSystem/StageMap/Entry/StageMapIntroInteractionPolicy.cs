using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>교체 가능한 진입 연출의 추가 차단입니다. 원래 입력 조건과 카메라 소유권은 변경하지 않습니다.</summary>
    internal sealed class StageMapIntroInteractionPolicy
    {
        private readonly StageMapIntroPresentation _intro;

        internal StageMapIntroInteractionPolicy(StageMapIntroPresentation intro)
        {
            _intro = intro ?? throw new ArgumentNullException(nameof(intro));
        }

        internal bool BlocksInteraction => _intro.IsPending;
    }
}
