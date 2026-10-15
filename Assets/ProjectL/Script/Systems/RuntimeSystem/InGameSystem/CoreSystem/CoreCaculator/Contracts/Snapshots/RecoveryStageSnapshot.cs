using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class RecoveryStageSnapshot
    {
        public RunId RunId { get; }
        public StageEntryId EntryId { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public long RunRevision { get; }
        public HealthStateSnapshot MainCharacterHealth { get; }
        public PlayerWalletSnapshot Wallet { get; }
        public ReadOnlyMemory<RecoveryOptionSnapshot> Options { get; }
        public RecoveryOptionId? AppliedOptionId { get; }
        public bool IsResolved => AppliedOptionId.HasValue;

        public RecoveryStageSnapshot(RunId runId, StageEntryId entryId, StageNodeId nodeId, StageKey stageKey,
            long runRevision, HealthStateSnapshot mainCharacterHealth, PlayerWalletSnapshot wallet,
            IReadOnlyList<RecoveryOptionSnapshot> options, RecoveryOptionId? appliedOptionId)
        {
            if (runId.IsEmpty) throw new ArgumentException(nameof(runId));
            if (entryId.IsEmpty) throw new ArgumentException(nameof(entryId));
            if (nodeId.IsEmpty) throw new ArgumentException(nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (runRevision < 0) throw new ArgumentOutOfRangeException(nameof(runRevision));
            if (mainCharacterHealth.MaxHealth < 1 || mainCharacterHealth.CurrentHealth < 1 ||
                mainCharacterHealth.CurrentHealth > mainCharacterHealth.MaxHealth)
                throw new ArgumentException("Living main-character health is required.", nameof(mainCharacterHealth));
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Count == 0) throw new ArgumentException("At least one option is required.", nameof(options));
            if (appliedOptionId.HasValue && appliedOptionId.Value.IsEmpty)
                throw new ArgumentException(nameof(appliedOptionId));

            var copied = new RecoveryOptionSnapshot[options.Count];
            var ids = new HashSet<RecoveryOptionId>();
            int healthDeficit = mainCharacterHealth.MaxHealth - mainCharacterHealth.CurrentHealth;
            for (int i = 0; i < options.Count; i++)
            {
                RecoveryOptionSnapshot option = options[i];
                if (option == null) throw new ArgumentException("Options cannot contain null.", nameof(options));
                if (!ids.Add(option.Id)) throw new ArgumentException("Option IDs must be unique.", nameof(options));
                if (option.ProjectedHealAmount > healthDeficit)
                    throw new ArgumentException("Projected healing must fit the current health deficit.", nameof(options));
                copied[i] = option;
            }
            if (appliedOptionId.HasValue && !ids.Contains(appliedOptionId.Value))
                throw new ArgumentException("The applied option must belong to this stage.", nameof(appliedOptionId));

            RunId = runId;
            EntryId = entryId;
            NodeId = nodeId;
            StageKey = stageKey;
            RunRevision = runRevision;
            MainCharacterHealth = mainCharacterHealth;
            Wallet = wallet;
            Options = copied;
            AppliedOptionId = appliedOptionId;
        }
    }
}
