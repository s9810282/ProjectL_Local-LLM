using System;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>
    /// 지금 요청할 수 있도록 공개한 정의 하나입니다(튜토리얼 설계 §5).
    /// 단계 구성·문구는 정의 데이터가 가지며 여기 싣지 않습니다.
    /// </summary>
    public sealed class TutorialDefinitionInfo
    {
        public int TutorialKey { get; }
        public string DisplayName { get; }

        internal TutorialDefinitionInfo(int tutorialKey, string displayName)
        {
            if (tutorialKey <= 0)
                throw new ArgumentOutOfRangeException(nameof(tutorialKey), tutorialKey, "0보다 커야 합니다.");

            TutorialKey = tutorialKey;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }
    }
}
