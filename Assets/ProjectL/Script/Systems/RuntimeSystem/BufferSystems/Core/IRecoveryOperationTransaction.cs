using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    internal interface IRecoveryOperationTransaction
    {
        void InstallEntry(RecoveryEntryPreparation preparation, StageEntryCommandResult? result, IStageCommandResultStore resultStore);
        PreparedRecoveryMutation PrepareSelection(RecoveryEntryContext context, CharacterRecoveryDecision decision);
        PreparedRecoveryMutation PrepareCompletion(RecoveryEntryContext context);
        void InstallMutation(PreparedRecoveryMutation prepared);
        PreparedRecoveryPublication PreparePublication(PreparedRecoveryMutation mutation, RecoveryProcessedResult result, int acknowledgedNumber);
        void InstallPublication(PreparedRecoveryPublication prepared);
    }
}
