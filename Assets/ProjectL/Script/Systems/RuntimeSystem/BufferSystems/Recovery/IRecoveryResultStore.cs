using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery
{
    internal interface IRecoveryResultStore : IServiceModule
    {
        RecoveryProcessedResult PrepareResult(RecoveryProcessedResult result, int acknowledgedNumber);
        void InstallResult(RecoveryProcessedResult prepared);
    }
}

