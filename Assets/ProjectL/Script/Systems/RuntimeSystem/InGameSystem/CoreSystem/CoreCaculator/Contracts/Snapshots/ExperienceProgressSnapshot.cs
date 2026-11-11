using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// 보유 카드 한 장의 다음 레벨 진행 조회값이다. 요구량 공식은 Core 규칙(ExperienceRules)만 가지며 이 값은 계산 결과만 옮긴다.
    /// 경험치가 꺼진 설정이면 <see cref="IsEnabled"/>가 거짓이고 <see cref="Required"/>·<see cref="Remaining"/>은 0이다.
    /// </summary>
    public sealed class ExperienceProgressSnapshot
    {
        /// <summary>경험치 설정이 켜져 있는지다. 꺼져 있으면 레벨이 오르지 않으므로 진행 막대를 그리지 않는다.</summary>
        public bool IsEnabled { get; }
        /// <summary>현재 레벨 안의 진행 경험치다. 보유 기록의 진행 경험치와 같다.</summary>
        public int Current { get; }
        /// <summary>현재 레벨에서 다음 레벨로 오르는 데 필요한 전체 요구량이다. 꺼짐이면 0이다.</summary>
        public int Required { get; }
        /// <summary>다음 레벨까지 남은 경험치다. 꺼짐이면 0이다.</summary>
        public int Remaining => IsEnabled ? Required - Current : 0;

        public ExperienceProgressSnapshot(bool isEnabled, int current, int required)
        {
            if (current < 0)
                throw new ArgumentOutOfRangeException(nameof(current));
            if (isEnabled ? required <= current : required != 0)
                throw new ArgumentException(isEnabled
                    ? "켜진 진행값의 요구량은 진행 경험치보다 커야 합니다."
                    : "꺼진 진행값의 요구량은 0이어야 합니다.", nameof(required));
            IsEnabled = isEnabled;
            Current = current;
            Required = required;
        }
    }
}
