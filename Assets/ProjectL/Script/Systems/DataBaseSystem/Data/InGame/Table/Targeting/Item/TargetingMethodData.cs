using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item
{
    /// <summary>유닛이 키로 참조하는 공격 지정 방식입니다. 사거리는 UnitData가 소유합니다.</summary>
    public sealed class TargetingMethodData : IData
    {
        public int MethodKey { get; }
        public TargetingShape Shape { get; }
        public TargetingChoiceMode ChoiceMode { get; }
        public TargetingSplash Splash { get; }
        public int SplashPercent { get; }

        public TargetingMethodData(int methodKey, TargetingShape shape, TargetingChoiceMode choiceMode,
            TargetingSplash splash, int splashPercent)
        {
            MethodKey = methodKey;
            Shape = shape;
            ChoiceMode = choiceMode;
            Splash = splash;
            SplashPercent = splashPercent;
        }

        public void Validate()
        {
            if (MethodKey < 1)
                throw new InvalidOperationException($"[TargetingMethodData] Method key must be at least 1. Key: {MethodKey}.");
            if (Splash == TargetingSplash.None ? SplashPercent != 0 : SplashPercent < 1)
                throw new InvalidOperationException(
                    $"[TargetingMethodData] Splash requires a positive percent; None requires 0. Key: {MethodKey}.");

            // enum 및 ChoiceMode/Splash 조합은 Core Contracts와 같은 규칙으로 검사합니다.
            try
            {
                _ = new TargetingMethodSpec(Shape, ChoiceMode, Splash, SplashPercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"[TargetingMethodData] Targeting method is not supported by Core. Key: {MethodKey} ({exception.Message})",
                    exception);
            }
        }
    }
}
