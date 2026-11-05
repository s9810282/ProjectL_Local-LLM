using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>게시된 종료 기록을 결과 조회용 값으로 투영한다. 회복·보상 정산을 다시 실행하지 않는다.</summary>
    public static class BattleResultCalculator
    {
        public static BattleResultSnapshot Create(BattleCompletionChanges completion, int stageDepth,
            RunCompletionReceipt runCompletion)
        {
            if (completion == null) throw new ArgumentNullException(nameof(completion));
            var progress = completion.Progression;
            if (runCompletion != null && (runCompletion.ChapterKey != progress.ChapterKey ||
                runCompletion.NodeId != progress.NodeId || runCompletion.StageKey != progress.StageKey))
                throw new ArgumentException("회차 종료와 전투 종료의 스테이지가 다릅니다.", nameof(runCompletion));

            var main = completion.Characters.Characters.Single(c =>
                c.Final.OwnershipRole == CharacterOwnershipRole.Main).Final;
            var persistent = completion.Characters.Main?.Final
                ?? throw new ArgumentException("완료 기록에 메인 지속 상태가 없습니다.", nameof(completion));
            CharacterSettlementCalculator.ValidateMain(main, persistent);
            var health = new HealthStateSnapshot(persistent.CurrentHealth,
                BattleCompletionCalculator.BattleEndMaxHealth(main, persistent));
            return new BattleResultSnapshot(progress.ChapterKey, progress.NodeId, progress.StageKey, stageDepth,
                completion.Outcome, runCompletion?.Outcome ?? RunOutcome.InProgress, health);
        }
    }
}
