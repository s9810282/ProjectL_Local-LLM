using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>게시 Run과 원래 요청 Run을 분리해 오래된 요청의 실패도 현재 문맥에서 소비한다.</summary>
    public sealed class TraitGrowthProcessedResult
    {
        public RunId RunId { get; }
        public int Number { get; }
        public TraitGrowthChoiceRequest Request { get; }
        public TraitGrowthResult Result { get; }
        public TraitGrowthProcessedResult(RunId runId, int number, TraitGrowthChoiceRequest request, TraitGrowthResult result)
        {
            if (runId.IsEmpty || number < 1)
                throw new ArgumentException("결과 게시 식별자가 올바르지 않습니다.");
            RunId = runId;
            Number = number;
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            if (result.Succeeded && (request.RunId != runId || result.Final.Id != request.OwnedId ||
                result.Final.Growth.Selections.LastOrDefault()?.StepId != request.StepId ||
                result.Final.Growth.Selections.Last().TraitKey != request.TraitKey))
                throw new ArgumentException("성공 결과와 원래 요청이 다릅니다.", nameof(result));
        }
    }
}
