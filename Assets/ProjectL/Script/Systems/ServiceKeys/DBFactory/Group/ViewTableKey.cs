using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;

namespace ProjectL.Systems.ServiceKeys.DBFactory.Group
{
    internal static class ViewTableKey
    {
        public static readonly SystemGroupKey<IViewStageTableQuery> ViewStage =
            new(nameof(ViewStage));

        public static readonly SystemGroupKey<ITerrainVisualTableQuery> TerrainVisual =
            new(nameof(TerrainVisual));

        public static readonly SystemGroupKey<IObjectVisualTableQuery> ObjectVisual =
            new(nameof(ObjectVisual));

        public static readonly SystemGroupKey<IStageEnvironmentVisualTableQuery> StageEnvironmentVisual =
            new(nameof(StageEnvironmentVisual));

        public static readonly SystemGroupKey<ILobbyVisualTableQuery> LobbyVisual =
            new(nameof(LobbyVisual));

        public static readonly SystemGroupKey<IArtifactPopupConfigTableQuery> ArtifactPopupConfig =
            new(nameof(ArtifactPopupConfig));

        public static readonly SystemGroupKey<IStageViewPropTableQuery> StageViewProp =
            new(nameof(StageViewProp));

        public static readonly SystemGroupKey<IStageFieldViewTableQuery> StageFieldView =
            new(nameof(StageFieldView));

        public static readonly SystemGroupKey<IStageMapViewTableQuery> StageMapView =
            new(nameof(StageMapView));

        public static readonly SystemGroupKey<IStageMapBackgroundTableQuery> StageMapBackground =
            new(nameof(StageMapBackground));

        public static readonly SystemGroupKey<IStageNodeVisualTableQuery> StageNodeVisual =
            new(nameof(StageNodeVisual));

        public static readonly SystemGroupKey<IUnitVisualTableQuery> UnitVisual =
            new(nameof(UnitVisual));

        public static readonly SystemGroupKey<IArtifactViewTableQuery> ArtifactView =
            new(nameof(ArtifactView));

        public static readonly SystemGroupKey<ITraitVisualTableQuery> TraitVisual =
            new(nameof(TraitVisual));
        public static readonly SystemGroupKey<ICategoryVisualTableQuery> CategoryVisual =
            new(nameof(CategoryVisual));

        public static readonly SystemGroupKey<IConsumableItemVisualTableQuery> ConsumableItemVisual =
            new(nameof(ConsumableItemVisual));

        public static readonly SystemGroupKey<IUnitStatusVisualTableQuery> UnitStatusVisual =
            new(nameof(UnitStatusVisual));

        public static readonly SystemGroupKey<IPlayerSkillVisualTableQuery> PlayerSkillVisual =
            new(nameof(PlayerSkillVisual));

        public static readonly SystemGroupKey<IStageOfferTextTableQuery> StageOfferText =
            new(nameof(StageOfferText));

        public static readonly SystemGroupKey<ITutorialDefinitionTableQuery> TutorialDefinition =
            new(nameof(TutorialDefinition));

        public static readonly SystemGroupKey<IRecoveryOptionVisualTableQuery> RecoveryOptionVisual =
            new(nameof(RecoveryOptionVisual));

        public static readonly SystemGroupKey<IRailwayVisualTableQuery> RailwayVisual =
            new(nameof(RailwayVisual));

        public static readonly SystemGroupKey<IRockfallVisualTableQuery> RockfallVisual =
            new(nameof(RockfallVisual));

        public static readonly SystemGroupKey<IHaboobVisualTableQuery> HaboobVisual =
            new(nameof(HaboobVisual));

        public static readonly SystemGroupKey<IInputSettingsTableQuery> InputSettings =
            new(nameof(InputSettings));

        public static readonly SystemGroupKey<ICombatPresentationSettingsTableQuery> CombatPresentationSettings =
            new(nameof(CombatPresentationSettings));
    }
}
