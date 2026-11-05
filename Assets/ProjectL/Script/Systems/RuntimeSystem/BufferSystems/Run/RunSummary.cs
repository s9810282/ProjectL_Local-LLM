using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Run
{
    /// <summary>
    /// 끝난 Run 하나의 결과 요약이다. Run 종료 영수증을 설치하는 순간의 값으로 한 번 만들며 바뀌지 않는다.
    /// 진행 중 값(쓴 골드·획득 명성·처치 수)의 사본이 아니라 종료 시점 기록이다(설계 S3).
    /// </summary>
    public sealed class RunSummary
    {
        /// <summary>끝난 Run의 ID다. 새 Run 시작을 거치지 않은 진행(개발·테스트)이면 비어 있다.</summary>
        public RunId RunId { get; }
        public RunCompletionReceipt Completion { get; }
        public RunOutcome Outcome => Completion.Outcome;
        public int GoldSpent { get; }
        public int FameEarned { get; }
        public int EnemiesDefeated { get; }

        public RunSummary(RunId runId, RunCompletionReceipt completion, int goldSpent, int fameEarned, int enemiesDefeated)
        {
            if (goldSpent < 0) throw new ArgumentOutOfRangeException(nameof(goldSpent));
            if (fameEarned < 0) throw new ArgumentOutOfRangeException(nameof(fameEarned));
            if (enemiesDefeated < 0) throw new ArgumentOutOfRangeException(nameof(enemiesDefeated));
            RunId = runId;
            Completion = completion ?? throw new ArgumentNullException(nameof(completion));
            GoldSpent = goldSpent;
            FameEarned = fameEarned;
            EnemiesDefeated = enemiesDefeated;
        }
    }
}
