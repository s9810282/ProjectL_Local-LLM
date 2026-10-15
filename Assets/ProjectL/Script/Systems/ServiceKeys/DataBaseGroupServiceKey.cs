using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.ServiceKeys
{
    internal static class RunProgressBufferKey
    {
        public static readonly SystemGroupKey<IRunProgressBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IRunProgressBufferStore> Store = new(nameof(Store));
    }

    internal static class MapProgressBufferKey
    {
        public static readonly SystemGroupKey<IMapProgressBufferStore> Store = new(nameof(Store));
        public static readonly SystemGroupKey<IMapProgressBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IMapProgressBufferWriteSink> Write = new(nameof(Write));
    }

    internal static class CurrencyBufferKey
    {
        public static readonly SystemGroupKey<ICurrencyBufferStore> Store = new(nameof(Store));
        public static readonly SystemGroupKey<ICurrencyBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<ICurrencyBufferWriteSink> Write = new(nameof(Write));
    }

    internal static class StageProgressBufferKey
    {
        public static readonly SystemGroupKey<IStageProgressBufferStore> Store = new(nameof(Store));
        public static readonly SystemGroupKey<IStageProgressBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IStageProgressBufferWriteSink> Write = new(nameof(Write));
    }

    internal static class ArtifactBufferKey
    {
        public static readonly SystemGroupKey<IArtifactBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IArtifactInitializationWriteSink> Initialize = new(nameof(Initialize));
        public static readonly SystemGroupKey<IArtifactEditWriteSink> Edit = new(nameof(Edit));
        public static readonly SystemGroupKey<IArtifactPreviewWriteSink> Preview = new(nameof(Preview));
        public static readonly SystemGroupKey<IArtifactEditResultReadSink> EditResults = new(nameof(EditResults));
        public static readonly SystemGroupKey<IArtifactPreviewReadSink> Previews = new(nameof(Previews));
    }

    internal static class CharacterBufferKey
    {
        public static readonly SystemGroupKey<ICharacterBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<ICharacterBufferWriteSink> Write = new(nameof(Write));
        public static readonly SystemGroupKey<ICharacterBufferStore> Store = new(nameof(Store));
    }

    internal static class CombatStateBufferKey
    {
        public static readonly SystemGroupKey<ICombatResultHistoryReadSink> History = new(nameof(History));
        public static readonly SystemGroupKey<ICombatStateBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<ICombatResultReadSink> Results = new(nameof(Results));
        public static readonly SystemGroupKey<ICombatStateBufferStore> Store = new(nameof(Store));
    }

    internal static class CurrentProgressBufferKey
    {
        public static readonly SystemGroupKey<ICurrentProgressBufferStore> Store = new(nameof(Store));
        public static readonly SystemGroupKey<ICurrentProgressBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<ICurrentProgressBufferWriteSink> Write = new(nameof(Write));
    }

    internal static class CombatViewBufferKey
    {
        public static readonly SystemGroupKey<ICombatViewBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<ICombatViewBufferWriteSink> Write = new(nameof(Write));
    }

    internal static class StageViewBufferKey
    {
        public static readonly SystemGroupKey<IStageViewBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IStageViewBufferWriteSink> Write = new(nameof(Write));
    }

    internal static class ArtifactViewBufferKey
    {
        public static readonly SystemGroupKey<IArtifactViewBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IArtifactViewBufferWriteSink> Write = new(nameof(Write));
    }

    internal static class DataBaseGroupKey
    {
        public static readonly SystemGroupKey<IRenderQuery> Render =
            new(nameof(Render));
        public static readonly SystemGroupKey<IInGameQuery> InGame =
            new(nameof(InGame));
        public static readonly SystemGroupKey<IViewQuery> View =
            new(nameof(View));
        public static readonly SystemGroupKey<IHasArtifactBoardConfig> ArtifactBoardConfig =
            new(nameof(ArtifactBoardConfig));
    }

    internal static class BufferSystemKey
    {
        public static readonly SystemGroupKey<IBufferQuery> Buffer = new(nameof(Buffer));
    }

    internal static class RenderTableKey 
    {
        public static readonly SystemGroupKey<IEffectTableQuery> Effect =
            new(nameof(Effect));

        public static readonly SystemGroupKey<IMetrialTableQuery> Metrial =
            new(nameof(Metrial));

        public static readonly SystemGroupKey<ILightTableQuery> Light =
            new(nameof(Light));
    }

    internal static class InGameTableKey
    {
        public static readonly SystemGroupKey<IStageMapTableQuery> StageMap =
            new(nameof(StageMap));
        public static readonly SystemGroupKey<IStageTableQuery> Stage =
            new(nameof(Stage));
        public static readonly SystemGroupKey<IFieldTableQuery> Field =
            new(nameof(Field));
        public static readonly SystemGroupKey<IUnitTableQuery> Unit =
            new(nameof(Unit));
        public static readonly SystemGroupKey<ITargetingMethodTableQuery> TargetingMethod =
            new(nameof(TargetingMethod));
        public static readonly SystemGroupKey<IStructureTableQuery> Structure =
            new(nameof(Structure));
        public static readonly SystemGroupKey<IEnemyLoadoutTableQuery> EnemyLoadout =
            new(nameof(EnemyLoadout));
        public static readonly SystemGroupKey<IPlayerLoadoutTableQuery> PlayerLoadout =
            new(nameof(PlayerLoadout));
        public static readonly SystemGroupKey<IUnitRosterTableQuery> UnitRoster =
            new(nameof(UnitRoster));
        public static readonly SystemGroupKey<IArtifactDefinitionTableQuery> ArtifactDefinition =
            new(nameof(ArtifactDefinition));
        public static readonly SystemGroupKey<ICategoryConfigTableQuery> CategoryConfig =
            new(nameof(CategoryConfig));
        public static readonly SystemGroupKey<IArtifactBoardConfigTableQuery> ArtifactBoardConfig =
            new(nameof(ArtifactBoardConfig));
        public static readonly SystemGroupKey<IStageEconomyTableQuery> StageEconomy =
            new(nameof(StageEconomy));
        public static readonly SystemGroupKey<IRecoveryOptionTableQuery> RecoveryOption =
            new(nameof(RecoveryOption));
    }

    internal static class ViewTableKey
    {
        public static readonly SystemGroupKey<IArtifactPopupConfigTableQuery> ArtifactPopupConfig =
            new(nameof(ArtifactPopupConfig));
        public static readonly SystemGroupKey<IStageViewPropTableQuery> StageViewProp =
            new(nameof(StageViewProp));
        public static readonly SystemGroupKey<IStageFieldViewTableQuery> StageFieldView =
            new(nameof(StageFieldView));
        public static readonly SystemGroupKey<IStageMapViewTableQuery> StageMapView =
            new(nameof(StageMapView));
        public static readonly SystemGroupKey<IStageNodeVisualTableQuery> StageNodeVisual =
            new(nameof(StageNodeVisual));
        public static readonly SystemGroupKey<IUnitVisualTableQuery> UnitVisual =
            new(nameof(UnitVisual));
        public static readonly SystemGroupKey<IArtifactViewTableQuery> ArtifactView =
            new(nameof(ArtifactView));
        public static readonly SystemGroupKey<ITraitVisualTableQuery> TraitVisual =
            new(nameof(TraitVisual));
        public static readonly SystemGroupKey<IRecoveryOptionVisualTableQuery> RecoveryOptionVisual =
            new(nameof(RecoveryOptionVisual));
        public static readonly SystemGroupKey<IRailwayVisualTableQuery> RailwayVisual =
            new(nameof(RailwayVisual));
        public static readonly SystemGroupKey<IInputSettingsTableQuery> InputSettings =
            new(nameof(InputSettings));
    }
}
