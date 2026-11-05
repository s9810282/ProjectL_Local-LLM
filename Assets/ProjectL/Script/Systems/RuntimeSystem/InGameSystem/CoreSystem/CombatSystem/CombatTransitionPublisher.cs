using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 명령·자동 진행의 계산 결과를 한 번에 게시한다. 전투를 끝낸 결과에는 진행 제안과 완료 기록만 붙인다.
    /// 이미 정산된 종료 회복·사망 영혼을 다시 계산하지 않으며, 포기 요청이나 미리보기는 받지 않는다.
    /// </summary>
    internal sealed class CombatTransitionPublisher : ISystemPostBindService
    {
        private readonly BattleProgressionPlanner _planner;
        private IBattleBufferWriteSink _battle;
        private IStageProgressBufferReadSink _stages;

        internal CombatTransitionPublisher(BattleProgressionPlanner planner)
        {
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _battle = buffers.BattleWriteSink;
            _stages = buffers.StageProgressReadSink;
        }

        /// <summary>게시했으면 true다. 완료 판단 중 Run이 끝났으면 아무것도 게시하지 않고 false다.</summary>
        public bool Publish(CombatCommandTransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            if (_battle == null)
                throw new InvalidOperationException("전투 결과 게시는 Buffer 바인딩이 필요합니다.");
            using var sample = CoreProfilerMarkers.Publish.Auto();
            if (transition.Accepted && transition.Outcome.IsStageEnded)
            {
                var progress = _planner.Plan(transition.Outcome.Outcome);
                if (!progress.Accepted)
                    return false;
                BattleRewardReceipt receipt = null;
                if (transition.Outcome.Outcome == StageOutcome.Victory && transition.Input.Battle?.ClearRewards != null)
                {
                    var nodes = _stages.GetStages(progress.ChapterKey).Where(s => s.NodeId == progress.NodeId.Value &&
                        s.StageKey == progress.StageKey.Value).ToArray();
                    if (nodes.Length != 1) throw new InvalidOperationException("완료한 노드의 스테이지 좌표를 찾을 수 없습니다.");
                    receipt = BattleRewardReceiptCalculator.Create(transition, nodes[0].Depth, nodes[0].WidthIndex);
                }
                var settled = new SoulActionChanges(transition.Characters, transition.Field, transition.ConsumedSoulIds);
                // 종료 회복과 사망 영혼은 계산기에 이미 합쳐져 있으므로 완료 기록만 연결한다.
                transition = transition.WithCompletion(new BattleCompletionChanges(settled, transition.Outcome, progress, receipt));
            }
            _battle.Apply(transition);
            return true;
        }
    }
}
