using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Run
{
    internal sealed class RecoveryStageState
    {
        internal RunId RunId { get; }
        internal StageEntryId EntryId { get; }
        internal StageNodeId NodeId { get; }
        internal StageKey StageKey { get; }
        internal ReadOnlyMemory<RecoveryOptionSpec> Options { get; }
        internal RecoveryOptionId? AppliedOptionId { get; private set; }
        internal int SpentAmount { get; private set; }
        internal int HealedAmount { get; private set; }
        internal bool IsResolved => AppliedOptionId.HasValue;

        internal RecoveryStageState(RecoveryStageEntryRequest request, RecoveryStageSpec spec)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (request.StageKey != spec.StageKey)
                throw new ArgumentException("Recovery input must match the requested stage.", nameof(spec));

            RunId = request.RunId;
            EntryId = request.EntryId;
            NodeId = request.NodeId;
            StageKey = request.StageKey;
            Options = spec.Options;
        }

        internal bool TryGetOption(RecoveryOptionId optionId, out RecoveryOptionSpec option)
        {
            foreach (RecoveryOptionSpec candidate in Options.Span)
            {
                if (candidate.Id == optionId)
                {
                    option = candidate;
                    return true;
                }
            }

            option = null;
            return false;
        }

        // The owning service validates and prepares the complete transaction before these assignments.
        internal void ApplyPrevalidated(RecoveryOptionId optionId, int spentAmount, int healedAmount)
        {
            AppliedOptionId = optionId;
            SpentAmount = spentAmount;
            HealedAmount = healedAmount;
        }
    }
}
