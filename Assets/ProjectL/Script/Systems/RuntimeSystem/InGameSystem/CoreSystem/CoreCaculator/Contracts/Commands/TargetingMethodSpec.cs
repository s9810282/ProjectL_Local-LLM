using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>지정 방식의 불변 계약입니다. 사거리·대상 조건·충격 효과는 포함하지 않습니다.</summary>
    public sealed class TargetingMethodSpec
    {
        public TargetingShape Shape { get; }
        public TargetingChoiceMode ChoiceMode { get; }
        public TargetingSplash Splash { get; }
        public int SplashPercent { get; }

        public TargetingMethodSpec(
            TargetingShape shape,
            TargetingChoiceMode choiceMode,
            TargetingSplash splash,
            int splashPercent)
        {
            if (shape == TargetingShape.None || !Enum.IsDefined(typeof(TargetingShape), shape))
                throw new ArgumentOutOfRangeException(nameof(shape));
            if (choiceMode == TargetingChoiceMode.None || !Enum.IsDefined(typeof(TargetingChoiceMode), choiceMode))
                throw new ArgumentOutOfRangeException(nameof(choiceMode));
            if (!Enum.IsDefined(typeof(TargetingSplash), splash))
                throw new ArgumentOutOfRangeException(nameof(splash));
            if (splashPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(splashPercent));
            if (choiceMode != TargetingChoiceMode.Single && splash != TargetingSplash.None)
                throw new ArgumentException("추가 범위는 단일 지정 방식에서만 사용할 수 있습니다.", nameof(splash));

            Shape = shape;
            ChoiceMode = choiceMode;
            Splash = splash;
            // None의 비율은 계산에서 사용하지 않습니다. 저작 상한은 별도 기획 계약입니다.
            SplashPercent = splashPercent;
        }
    }
}
