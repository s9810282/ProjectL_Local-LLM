using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;

namespace ProjectL.Tests.Framework
{
    public sealed partial class ChapterGraphPreparationTests
    {
        // 실제 Factory의 모듈·하위 서비스 등록은 유지하고 루트의 초기화 호출만 관찰한다.
        private sealed class ResetSpy : IBufferQuery, IRunProgressBufferWriteSink,
            IExportSystemServices, ISystemWriteSink
        {
            private readonly IBufferQuery inner;
            private ISystemWriteSink sink;
            public int ResetCalls { get; private set; }
            public ResetSpy(IBufferQuery inner) => this.inner = inner;
            public void ResetRunProgress() { ResetCalls++; inner.RunProgressWriteSink.ResetRunProgress(); }
            // 새 Run 시작도 이전 Run을 비우므로 초기화 호출로 센다.
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.RosterAcquireResult PublishNewRun(
                ProjectL.Systems.RuntimeSystem.BufferSystems.Run.NewRunPreparation preparation)
            { ResetCalls++; return inner.RunProgressWriteSink.PublishNewRun(preparation); }
            public void ExportServices(ISystemWriteSink target)
            {
                sink = target;
                ((IExportSystemServices)inner).ExportServices(this);
            }
            public void AddService<T>(T service) where T : class, IServiceModule => AddService(typeof(T), service);
            public void AddService(Type type, object service) => sink.AddService(type,
                type == typeof(IBufferQuery) ? this : service);
            public void AddModule<T>(T module) where T : class => sink.AddModule(module);
            public IChapterContinuationBufferWriteSink ChapterContinuationWriteSink => inner.ChapterContinuationWriteSink;
            public IChapterProgressBufferWriteSink ChapterProgressWriteSink => inner.ChapterProgressWriteSink;
            public ProjectL.Systems.RuntimeSystem.BufferSystems.Run.IRunProgressBufferReadSink RunProgressReadSink => inner.RunProgressReadSink;
            public IRunProgressBufferWriteSink RunProgressWriteSink => this;
            public ICharacterPurchaseBufferWriteSink CharacterPurchaseWriteSink => inner.CharacterPurchaseWriteSink;
            public IBattleBufferWriteSink BattleWriteSink => inner.BattleWriteSink;
            public IRecoveryStageBufferWriteSink RecoveryStageWriteSink => inner.RecoveryStageWriteSink;
            public IShopStageBufferWriteSink ShopStageWriteSink => inner.ShopStageWriteSink;
            public ICurrentProgressBufferReadSink CurrentProgressReadSink => inner.CurrentProgressReadSink;
            public ICurrentProgressBufferWriteSink CurrentProgressWriteSink => inner.CurrentProgressWriteSink;
            public IMapProgressBufferReadSink MapProgressReadSink => inner.MapProgressReadSink;
            public IMapProgressBufferWriteSink MapProgressWriteSink => inner.MapProgressWriteSink;
            public IArtifactBufferReadSink ArtifactReadSink => inner.ArtifactReadSink;
            public IArtifactInitializationWriteSink ArtifactInitializationWriteSink => inner.ArtifactInitializationWriteSink;
            public IArtifactEditWriteSink ArtifactEditWriteSink => inner.ArtifactEditWriteSink;
            public IArtifactPreviewWriteSink ArtifactPreviewWriteSink => inner.ArtifactPreviewWriteSink;
            public IArtifactEditResultReadSink ArtifactEditResultReadSink => inner.ArtifactEditResultReadSink;
            public IArtifactPreviewReadSink ArtifactPreviewReadSink => inner.ArtifactPreviewReadSink;
            public ICharacterBufferReadSink CharacterReadSink => inner.CharacterReadSink;
            public ICharacterBufferWriteSink CharacterWriteSink => inner.CharacterWriteSink;
            public ICombatStateBufferReadSink CombatStateReadSink => inner.CombatStateReadSink;
            public ICombatResultReadSink CombatResultReadSink => inner.CombatResultReadSink;
            public ICombatResultHistoryReadSink CombatResultHistoryReadSink => inner.CombatResultHistoryReadSink;
            public ICurrencyBufferReadSink CurrencyReadSink => inner.CurrencyReadSink;
            public ICurrencyBufferWriteSink CurrencyWriteSink => inner.CurrencyWriteSink;
            public IStageProgressBufferReadSink StageProgressReadSink => inner.StageProgressReadSink;
            public IStageProgressBufferWriteSink StageProgressWriteSink => inner.StageProgressWriteSink;
            public ICombatViewBufferReadSink CombatViewReadSink => inner.CombatViewReadSink;
            public ICombatViewBufferWriteSink CombatViewWriteSink => inner.CombatViewWriteSink;
            public IStageViewBufferReadSink StageViewReadSink => inner.StageViewReadSink;
            public IStageViewBufferWriteSink StageViewWriteSink => inner.StageViewWriteSink;
            public IArtifactViewBufferReadSink ArtifactViewReadSink => inner.ArtifactViewReadSink;
            public IArtifactViewBufferWriteSink ArtifactViewWriteSink => inner.ArtifactViewWriteSink;
        }
    }
}
