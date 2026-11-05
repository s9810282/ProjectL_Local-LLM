using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 현재 챕터와 전투 승패로 스테이지 완료 진행 제안을 얻는다. 명령 결과 게시와 포기 정산이 공유하는 규칙 경계다.
    /// HP 정산·입력 수락·Buffer 게시를 하지 않는다.
    /// </summary>
    internal sealed class BattleProgressionPlanner : ISystemPostBindService
    {
        private readonly IChapterProgressionCoordinator _progression;
        private ICurrentProgressBufferReadSink _current;

        /// <param name="progression">없으면 완료 판단을 요청할 때 상태 오류다(기존 계약). 완료가 없는 처리에는 필요하지 않다.</param>
        internal BattleProgressionPlanner(IChapterProgressionCoordinator progression)
        {
            _progression = progression;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _current = resolver.GetService(BufferSystemKey.Buffer).CurrentProgressReadSink;
        }

        /// <summary>
        /// 수락된 진행 제안 또는 Run 종료 거절을 돌려준다. Run 종료는 호출자가 정상 실패로 처리한다.
        /// 그 밖의 진행 거절은 부분 게시를 막기 위해 상태 오류로 드러낸다.
        /// </summary>
        public ChapterProgressionResult Plan(StageOutcome outcome)
        {
            if (_progression == null)
                throw new InvalidOperationException("진행 판단 서비스가 필요합니다.");
            if (_current == null)
                throw new InvalidOperationException("진행 판단은 Buffer 바인딩이 필요합니다.");
            var progress = _progression.PlanStageResolution(_current.CurrentMapKey, outcome);
            if (!progress.Accepted && progress.FailureReason == ChapterProgressionFailureReason.RunEnded)
                return progress;
            if (!progress.Accepted)
                throw new InvalidOperationException($"스테이지 완료가 거절되었습니다: {progress.FailureReason}");
            return progress;
        }
    }
}
