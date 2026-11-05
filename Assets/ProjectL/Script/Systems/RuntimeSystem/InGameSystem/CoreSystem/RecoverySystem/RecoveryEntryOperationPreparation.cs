using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    /// <summary>같은 회복 처리 범위에서 한 번 설치할 진입 판정과 선택 제안입니다.</summary>
    internal sealed class RecoveryEntryOperationPreparation
    {
        internal RecoveryStageService Owner { get; }
        internal object Scope { get; }
        internal ChapterProgressionResult Result { get; }
        internal RecoveryEntryPreparation Entry { get; }
        internal bool Consumed { get; set; }

        internal RecoveryEntryOperationPreparation(RecoveryStageService owner, object scope,
            ChapterProgressionResult result, RecoveryEntryPreparation entry = null)
        {
            Owner = owner;
            Scope = scope;
            Result = result;
            Entry = entry;
        }
    }
}
