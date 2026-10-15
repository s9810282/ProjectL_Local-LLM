using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>
    /// 전투 완료 기록(노드·맵 완료, 영혼 필드 정산, Run 종료 영수증)을 준비하고 설치한다.
    /// 완료 여부와 정산 값은 Core가 정하고, 여기서는 원본 상태와 저장소 불변식만 확인한다.
    /// </summary>
    internal sealed class BattleCompletionTransaction
    {
        private readonly BufferGuards _guards;
        private readonly ICombatStateBufferReadSink _combatRead;
        private readonly IStageProgressBufferReadSink _stageRead;
        private readonly IMapProgressBufferReadSink _mapRead;
        private readonly ICombatStateBufferStore _combatStore;
        private readonly IStageProgressBufferStore _stageStore;
        private readonly IMapProgressBufferStore _mapStore;
        private readonly IRunProgressBufferStore _runStore;

        internal BattleCompletionTransaction(BufferGuards guards, ICombatStateBufferReadSink combatRead,
            IStageProgressBufferReadSink stageRead, IMapProgressBufferReadSink mapRead,
            ICombatStateBufferStore combatStore, IStageProgressBufferStore stageStore,
            IMapProgressBufferStore mapStore, IRunProgressBufferStore runStore)
        {
            _guards = guards ?? throw new System.ArgumentNullException(nameof(guards));
            _combatRead = combatRead ?? throw new System.ArgumentNullException(nameof(combatRead));
            _stageRead = stageRead ?? throw new System.ArgumentNullException(nameof(stageRead));
            _mapRead = mapRead ?? throw new System.ArgumentNullException(nameof(mapRead));
            _combatStore = combatStore ?? throw new System.ArgumentNullException(nameof(combatStore));
            _stageStore = stageStore ?? throw new System.ArgumentNullException(nameof(stageStore));
            _mapStore = mapStore ?? throw new System.ArgumentNullException(nameof(mapStore));
            _runStore = runStore ?? throw new System.ArgumentNullException(nameof(runStore));
        }

        internal Prepared Prepare(BattleCompletionChanges changes)
        {
            _guards.EnsureRunInProgress();
            var progress = changes.Progression;
            _guards.ValidateSelection(progress.ChapterKey, progress.NodeId, progress.StageKey);
            if (_combatRead.Completion != null)
                throw new System.InvalidOperationException("이미 완료된 전투입니다.");
            bool found = false;
            foreach (var stage in _stageRead.GetStages(progress.ChapterKey))
                if (stage.NodeId == progress.NodeId.Value && stage.StageKey == progress.StageKey.Value && !stage.IsClear)
                    found = true;
            if (!found || _mapRead.GetMap(progress.ChapterKey).IsClear)
                throw new System.InvalidOperationException("완료할 스테이지의 진행 상태가 맞지 않습니다.");
            PreparedStageClear stageClear = progress.ClearStage
                ? _stageStore.PrepareClear(progress.ChapterKey, progress.NodeId.Value, true) : null;
            MapProgressData? mapClear = progress.ClearMap
                ? _mapStore.PrepareClear(progress.ChapterKey, true) : (MapProgressData?)null;
            SoulFieldSnapshot field = changes.Settlement != null
                ? _combatStore.PrepareSoulField(changes.Settlement.Field) : null;

            RunCompletionReceipt run = changes.RunCompletion == null ? null : _runStore.PrepareCompletion(changes.RunCompletion);
            return new Prepared(changes, stageClear, mapClear, field, run);
        }

        internal void Install(Prepared prepared)
        {
            if (prepared.Field != null) _combatStore.InstallSoulField(prepared.Field);
            if (prepared.Stage != null) _stageStore.InstallClear(prepared.Stage);
            if (prepared.Map.HasValue) _mapStore.InstallClear(prepared.Map.Value);
            _combatStore.InstallCompletion(prepared.Changes);
            if (prepared.Run != null) _runStore.InstallCompletion(prepared.Run);
        }

        internal sealed class Prepared
        {
            internal BattleCompletionChanges Changes { get; }
            internal PreparedStageClear Stage { get; }
            internal MapProgressData? Map { get; }
            internal SoulFieldSnapshot Field { get; }
            internal RunCompletionReceipt Run { get; }
            internal Prepared(BattleCompletionChanges changes, PreparedStageClear stage,
                MapProgressData? map, SoulFieldSnapshot field, RunCompletionReceipt run)
            { Changes = changes; Stage = stage; Map = map; Field = field; Run = run; }
        }
    }
}
