using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView
{
    /// <summary>발급 번호와 원본 회복 입력을 함께 보관한다. 기본값은 입력이 없음을 뜻한다.</summary>
    public readonly struct RecoveryViewCommand
    {
        public int Number { get; }
        public RecoveryCommandData Data { get; }

        internal RecoveryViewCommand(int number, RecoveryCommandData data)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            Data = data ?? throw new ArgumentNullException(nameof(data));
            Number = number;
        }
    }
}
