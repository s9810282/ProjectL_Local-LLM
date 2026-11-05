#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    internal interface IDevelopmentBufferWriteSink : IServiceModule
    {
        DevelopmentFailureReason TryGrantGold(DevelopmentContext expected, int expectedBalance, int finalBalance);
        DevelopmentFailureReason TryPublishProgress(DevelopmentProgressPreparation preparation);
    }

    internal interface IDevelopmentBufferProvider
    {
        IDevelopmentBufferWriteSink DevelopmentWriteSink { get; }
    }

    /// <summary>Core가 계산한 증가분을 원본 비교 후 설치한다. 지출 누계는 기존 재화 저장소가 유지한다.</summary>
    internal sealed class DevelopmentTransactions : IDevelopmentBufferWriteSink
    {
        private readonly IRunProgressBufferReadSink _run;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly ICurrencyBufferReadSink _currency;
        private readonly ICurrencyBufferStore _store;
        private readonly IMapProgressBufferReadSink _maps;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly IStageProgressBufferStore _stageStore;
        private readonly ICurrentProgressBufferStore _currentStore;

        internal DevelopmentTransactions(IRunProgressBufferReadSink run, ICurrentProgressBufferReadSink current,
            ICombatStateBufferReadSink combat, ICurrencyBufferReadSink currency, ICurrencyBufferStore store,
            IMapProgressBufferReadSink maps, IStageProgressBufferReadSink stages,
            IStageProgressBufferStore stageStore, ICurrentProgressBufferStore currentStore)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _stageStore = stageStore ?? throw new ArgumentNullException(nameof(stageStore));
            _currentStore = currentStore ?? throw new ArgumentNullException(nameof(currentStore));
        }

        public DevelopmentFailureReason TryGrantGold(DevelopmentContext expected, int expectedBalance, int finalBalance)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (_run.RunId.IsEmpty) return DevelopmentFailureReason.NoActiveRun;
            var selection = _current.CurrentStage;
            if (_run.RunId != expected.RunId || _current.CurrentMapKey != expected.ChapterKey ||
                _current.HasSelection != expected.HasSelection ||
                (expected.HasSelection && (selection.NodeId != expected.NodeId.Value || selection.StageKey != expected.StageKey.Value)) ||
                _currency.Amount != expectedBalance)
                return DevelopmentFailureReason.StaleContext;
            if (_run.Outcome != RunOutcome.InProgress) return DevelopmentFailureReason.RunEnded;
            if (_combat.HasBattle) return DevelopmentFailureReason.BattleRetained;
            if (expectedBalance < 0 || finalBalance <= expectedBalance)
                return DevelopmentFailureReason.InvalidAmount;

            var prepared = _store.PrepareAmount(expectedBalance, finalBalance);
            _store.InstallAmount(prepared);
            return DevelopmentFailureReason.None;
        }

        public DevelopmentFailureReason TryPublishProgress(DevelopmentProgressPreparation preparation)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));
            var reason = CheckProgress(preparation);
            if (reason != DevelopmentFailureReason.None) return reason;
            if (preparation.BypassedNodes.Count == 0) return DevelopmentFailureReason.None;

            var clears = new List<PreparedStageClear>();
            foreach (var id in preparation.BypassedNodes)
                clears.Add(_stageStore.PrepareClear(preparation.Expected.ChapterKey, id.Value, true));
            var selection = _currentStore.PrepareDevelopmentSelection(preparation.Expected.ChapterKey,
                preparation.Expected.HasSelection, preparation.ExpectedSelection,
                preparation.FinalSelectedNodeId.Value, preparation.FinalSelectedStageKey, preparation.FinalSelectedStageName);

            // 모든 준비가 성공한 뒤 콜백·계산 없이 확정한다. 마지막 준비 실패도 앞선 통과 표시를 남기지 않는다.
            foreach (var clear in clears) _stageStore.InstallClear(clear);
            _currentStore.InstallSelection(selection);
            return DevelopmentFailureReason.None;
        }

        private DevelopmentFailureReason CheckProgress(DevelopmentProgressPreparation preparation)
        {
            var expected = preparation.Expected;
            if (_run.RunId.IsEmpty) return DevelopmentFailureReason.NoActiveRun;
            var selection = _current.CurrentStage;
            if (_run.RunId != expected.RunId || _current.CurrentMapKey != expected.ChapterKey ||
                _current.HasSelection != expected.HasSelection ||
                selection.NodeId != preparation.ExpectedSelection.NodeId ||
                selection.StageKey != preparation.ExpectedSelection.StageKey ||
                selection.StageName != preparation.ExpectedSelection.StageName ||
                (expected.HasSelection && (selection.NodeId != expected.NodeId.Value || selection.StageKey != expected.StageKey.Value)))
                return DevelopmentFailureReason.StaleContext;
            if (_run.Outcome != RunOutcome.InProgress) return DevelopmentFailureReason.RunEnded;
            if (_combat.HasBattle) return DevelopmentFailureReason.BattleRetained;
            if (_maps.GetMap(expected.ChapterKey).IsClear != preparation.ExpectedMapClear ||
                !ChapterContinuationPreparation.SameGraph(preparation.ExpectedStages, _stages.GetStages(expected.ChapterKey)))
                return DevelopmentFailureReason.StaleContext;

            var graph = preparation.ExpectedStages;
            if (!graph.Any(n => n.NodeId == preparation.TargetNodeId.Value && n.StageKey == preparation.TargetStageKey.Value))
                return DevelopmentFailureReason.StaleContext;
            var ids = preparation.BypassedNodes;
            if (ids.Distinct().Count() != ids.Count || ids.Any(id => id.IsEmpty || !graph.Any(n => n.NodeId == id.Value && !n.IsClear)))
                return DevelopmentFailureReason.StaleContext;
            if (ids.Count == 0)
                return preparation.FinalSelectedNodeId == null && preparation.FinalSelectedStageKey.IsEmpty && preparation.FinalSelectedStageName == null
                    ? DevelopmentFailureReason.None : DevelopmentFailureReason.StaleContext;
            if (preparation.FinalSelectedNodeId != ids[ids.Count - 1] ||
                !graph.Any(n => n.NodeId == preparation.FinalSelectedNodeId.Value.Value && n.StageKey == preparation.FinalSelectedStageKey.Value))
                return DevelopmentFailureReason.StaleContext;
            return DevelopmentFailureReason.None;
        }
    }
}
#endif
