#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.Development
{
    /// <summary>현재 진행을 순수 계산 입력으로 바꾸고 선택한 경로의 정의만 검증한다.</summary>
    internal sealed class DevelopmentNavigationActions
    {
        private readonly StageServiceManager _definitions;
        private readonly IRunProgressBufferReadSink _run;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IMapProgressBufferReadSink _maps;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly IDevelopmentBufferWriteSink _transactions;
        private readonly Func<DevelopmentContext, DevelopmentFailureReason> _checkAdmission;

        internal DevelopmentNavigationActions(StageServiceManager definitions, IRunProgressBufferReadSink run,
            ICurrentProgressBufferReadSink current, IMapProgressBufferReadSink maps,
            IStageProgressBufferReadSink stages, IDevelopmentBufferWriteSink transactions,
            Func<DevelopmentContext, DevelopmentFailureReason> checkAdmission)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
            _checkAdmission = checkAdmission ?? throw new ArgumentNullException(nameof(checkAdmission));
        }

        internal DevelopmentNavigationResult Execute(DevelopmentContext expected, StageNodeId targetNodeId, bool jump)
        {
            if (expected.ChapterKey < 1)
                return Refused(DevelopmentFailureReason.InvalidTarget, targetNodeId);
            var selection = _current.CurrentStage;
            var graph = _stages.GetStages(expected.ChapterKey).ToArray();
            bool mapClear = _maps.GetMap(expected.ChapterKey).IsClear;
            var input = new DevelopmentNavigationInput(expected, mapClear, graph.Select(node =>
                new DevelopmentNode(new ChapterNodeProgress(new StageNodeId(node.NodeId),
                    node.StageKey == 0 ? default : new StageKey(node.StageKey), node.Depth, node.IsClear,
                    node.NextNodeIds.Select(id => new StageNodeId(id))),
                    node.StageType == StageType.Combat && node.MapRole == StageMapRole.Regular && node.NextNodeIds.Count > 0)).ToArray());
            var plan = jump ? DevelopmentNavigationPlanner.PlanJump(input, targetNodeId)
                : DevelopmentNavigationPlanner.PlanSkip(input, targetNodeId);
            if (plan.Reason != DevelopmentFailureReason.None)
                return Refused(plan.Reason, targetNodeId, plan.NavigationFailureReason);

            bool SameProgress()
            {
                var actual = _current.CurrentStage;
                return _run.RunId == expected.RunId && _current.CurrentMapKey == expected.ChapterKey &&
                    _current.HasSelection == expected.HasSelection &&
                    actual.NodeId == selection.NodeId && actual.StageKey == selection.StageKey &&
                    actual.StageName == selection.StageName &&
                    _maps.Maps.Any(map => map.MapKey == expected.ChapterKey && map.IsClear == mapClear) &&
                    ChapterContinuationPreparation.SameGraph(graph, _stages.GetStages(expected.ChapterKey));
            }

            DevelopmentFailureReason Recheck()
            {
                var reason = _checkAdmission(expected);
                if (reason != DevelopmentFailureReason.None) return reason;
                return SameProgress() ? DevelopmentFailureReason.None : DevelopmentFailureReason.StaleContext;
            }

            StageKey finalKey = default;
            string finalName = null;
            foreach (var id in plan.BypassedNodes.Concat(new[] { targetNodeId }).Distinct())
            {
                var node = graph.Single(n => n.NodeId == id.Value);
                StageRootData definition;
                try
                {
                    definition = _definitions.GetStage(node.StageKey);
                }
                catch when (!SameProgress())
                {
                    return Refused(DevelopmentFailureReason.StaleContext, targetNodeId);
                }
                var reason = Recheck();
                if (reason != DevelopmentFailureReason.None) return Refused(reason, targetNodeId);
                if (definition == null)
                    throw new InvalidOperationException("개발 경로의 스테이지 정의가 필요합니다.");
                definition.Validate();
                if (definition.StageKey != node.StageKey || definition.MapKey != expected.ChapterKey ||
                    definition.StageType != node.StageType || definition.MapRole != node.MapRole)
                    throw new InvalidOperationException("개발 경로의 DB 정의와 생성된 노드 바인딩이 다릅니다.");
                if (plan.BypassedNodes.Contains(id) &&
                    (definition.StageType != StageType.Combat || definition.MapRole != StageMapRole.Regular ||
                     !(definition.StageData is CombatStageDetail)))
                    throw new InvalidOperationException("통과할 노드에는 일반 전투 정의가 필요합니다.");
                try
                {
                    _definitions.ValidateEntry(node.StageKey);
                }
                catch when (!SameProgress())
                {
                    // DB 조회 예외만 원본 변경과 구분한다. 외부 시점 거절로 설정 오류를 가리지 않는다.
                    return Refused(DevelopmentFailureReason.StaleContext, targetNodeId);
                }
                reason = Recheck();
                if (reason != DevelopmentFailureReason.None) return Refused(reason, targetNodeId);
                if (plan.FinalSelectedNodeId == id)
                {
                    finalKey = new StageKey(definition.StageKey);
                    finalName = $"Stage {definition.StageKey}";
                }
            }

            var admission = Recheck();
            if (admission != DevelopmentFailureReason.None) return Refused(admission, targetNodeId);
            var preparation = new DevelopmentProgressPreparation(expected, selection, graph, mapClear,
                plan.BypassedNodes, targetNodeId, plan.TargetStageKey, plan.FinalSelectedNodeId, finalKey, finalName);
            var outcome = _transactions.TryPublishProgress(preparation);
            if (outcome != DevelopmentFailureReason.None) return Refused(outcome, targetNodeId);
            // 게시 뒤에는 정의나 외부 시점 공급자를 다시 호출하지 않는다.
            return new DevelopmentNavigationResult(DevelopmentFailureReason.None, targetNodeId,
                plan.TargetStageKey, plan.BypassedNodes, CaptureCurrent(DevelopmentFailureReason.None));
        }

        private DevelopmentNavigationResult Refused(DevelopmentFailureReason reason, StageNodeId target,
            CoreCaculator.Contracts.Results.ChapterSelectionFailureReason? normalReason = null) =>
            new DevelopmentNavigationResult(reason, target, default, Array.Empty<StageNodeId>(),
                CaptureCurrent(reason == DevelopmentFailureReason.InvalidTarget || reason == DevelopmentFailureReason.AlreadyPassed ||
                    reason == DevelopmentFailureReason.UnsupportedNode || reason == DevelopmentFailureReason.NoSupportedPath
                    ? DevelopmentFailureReason.None : reason), normalReason);

        private DevelopmentContext CaptureCurrent(DevelopmentFailureReason reason)
        {
            var selection = _current.CurrentStage;
            return new DevelopmentContext(_run.RunId, _current.CurrentMapKey, _current.HasSelection,
                _current.HasSelection ? new StageNodeId(selection.NodeId) : default,
                _current.HasSelection ? new StageKey(selection.StageKey) : default, reason);
        }
    }
}
#endif
