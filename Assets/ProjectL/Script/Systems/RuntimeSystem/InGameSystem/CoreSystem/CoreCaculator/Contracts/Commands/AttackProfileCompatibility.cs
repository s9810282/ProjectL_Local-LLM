using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public static class AttackProfileCompatibility
    {
        /// <summary>지정 계약의 입력 종류입니다. 진영·사거리는 이 판정에 포함하지 않습니다.</summary>
        public static AttackAimKind GetSupportedAimKind(TargetingMethodSpec method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            switch (method.ChoiceMode)
            {
                case TargetingChoiceMode.Single:
                    return AttackAimKind.Cell;
                case TargetingChoiceMode.Direction:
                    return AttackAimKind.Direction;
                case TargetingChoiceMode.All:
                    return AttackAimKind.All;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method));
            }
        }

    }
}
