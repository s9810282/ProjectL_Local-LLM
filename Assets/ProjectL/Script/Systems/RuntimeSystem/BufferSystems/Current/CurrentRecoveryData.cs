using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>현재 회복 스테이지의 고정 선택지와 한 번의 소비 기록이다.</summary>
    public sealed class CurrentRecoveryData
    {
        public IReadOnlyList<RecoveryOptionSpec> Options { get; }
        public RecoveryOptionId? AppliedOptionId { get; }
        public int PaidAmount { get; }
        public int HealedAmount { get; }

        internal CurrentRecoveryData(IReadOnlyList<RecoveryOptionSpec> options,
            RecoveryOptionId? applied = null, int paid = 0, int healed = 0)
        {
            if (options == null || options.Count == 0) throw new ArgumentException("회복 선택지가 필요합니다.");
            var ids = new HashSet<RecoveryOptionId>();
            var copy = new RecoveryOptionSpec[options.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (options[i] == null || !ids.Add(options[i].Id))
                    throw new ArgumentException("회복 선택지 ID가 중복되거나 비어 있습니다.");
                copy[i] = options[i];
            }
            Options = Array.AsReadOnly(copy);
            AppliedOptionId = applied;
            PaidAmount = paid;
            HealedAmount = healed;
        }

        public bool TryGetOption(RecoveryOptionId id, out RecoveryOptionSpec option)
        {
            foreach (var candidate in Options)
                if (candidate.Id == id) { option = candidate; return true; }
            option = null;
            return false;
        }

        // 회복이 실제로 효과가 있어야 한다는 규칙은 Core(CharacterRecoveryCalculator)가 소유한다. 여기서는 값 범위만 본다.
        internal CurrentRecoveryData WithRecovery(RecoveryOptionId id, int paid, int healed)
        {
            if (AppliedOptionId.HasValue || !TryGetOption(id, out var option) ||
                option.Cost != paid || healed < 0)
                throw new InvalidOperationException("회복 선택지의 소비 상태가 맞지 않습니다.");
            return new CurrentRecoveryData(Options, id, paid, healed);
        }
    }
}
