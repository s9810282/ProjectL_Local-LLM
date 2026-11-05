using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>거래와 결과 설치에 필요한 준비 값과 이전 게시 이력을 묶는다.</summary>
    internal sealed class PreparedRecoveryPublication
    {
        internal StageServiceTransactions Owner { get; }
        internal PreparedRecoveryMutation Mutation { get; }
        internal RecoveryProcessedResult Result { get; }
        internal int AcknowledgedNumber { get; }
        internal int HandledNumber { get; }
        internal bool Installed { get; set; }

        internal PreparedRecoveryPublication(StageServiceTransactions owner, PreparedRecoveryMutation mutation,
            RecoveryProcessedResult result, int acknowledgedNumber, int handledNumber)
        {
            Owner = owner;
            Mutation = mutation;
            Result = result;
            AcknowledgedNumber = acknowledgedNumber;
            HandledNumber = handledNumber;
        }
    }
}

