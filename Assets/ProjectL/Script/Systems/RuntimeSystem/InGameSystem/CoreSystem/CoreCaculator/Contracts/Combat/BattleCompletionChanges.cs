using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>정리 전 게시할 최종 캐릭터 값, 승패 및 진행 결정이다.</summary>
    public sealed class BattleCompletionChanges
    {
        public CharacterChanges Characters { get; }
        public StageOutcomeSnapshot Outcome { get; }
        public ChapterProgressionResult Progression { get; }
        public SoulActionChanges Settlement { get; }
        /// <summary>시도 종료(패배·포기)로 Run을 끝낼 때 Core가 확정한 영수증이다. 다른 경우 null이다.</summary>
        public RunCompletionReceipt RunCompletion { get; }

        public BattleCompletionChanges(CharacterChanges characters, StageOutcomeSnapshot outcome,
            ChapterProgressionResult progression)
        {
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Progression = progression ?? throw new ArgumentNullException(nameof(progression));
            if (!outcome.IsStageEnded || !progression.Accepted || progression.SelectMap || progression.SelectStage ||
                (outcome.Outcome == Common.StageOutcome.Victory
                    ? !progression.ClearStage || progression.EndAttempt
                    : !progression.EndAttempt || progression.ClearStage || progression.ClearMap))
                throw new ArgumentException("완료 승패와 진행 결정이 일치해야 합니다.");
            Outcome = outcome;
            RunCompletion = progression.EndAttempt
                ? new RunCompletionReceipt(Common.RunOutcome.Defeat, progression.ChapterKey, progression.NodeId, progression.StageKey) : null;
        }

        public BattleCompletionChanges(SoulActionChanges settlement, StageOutcomeSnapshot outcome,
            ChapterProgressionResult progression) : this(settlement?.Characters, outcome, progression)
        {
            Settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
        }


    }
}
