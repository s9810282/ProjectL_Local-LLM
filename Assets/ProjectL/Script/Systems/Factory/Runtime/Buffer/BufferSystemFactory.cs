using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Framework.Composition;
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
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.Runtime.Buffer
{
    #region 시스템 모듈

    internal static class BufferSystemFactory
    {
        internal static IExportSystemServices Create()
        {
            SystemBuilderHelper builder = new();
            var run = new RunProgressBuffer();
            run.ExportServices(builder);
            new RunDeckBuffer().ExportServices(builder);
            new ItemInventoryBuffer().ExportServices(builder);
            new CurrentProgressBuffer().ExportServices(builder);
            new MapProgressBuffer().ExportServices(builder);
            new ArtifactBuffer().ExportServices(builder);
            var characters = new CharacterBuffer();
            characters.ExportServices(builder);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView.TraitGrowthViewBuffer(run,
                characters.TraitGrowthResults).ExportServices(builder);
            new CharacterStatsBuffer().ExportServices(builder);
            new CombatStateBuffer().ExportServices(builder);
            new CurrencyBuffer().ExportServices(builder);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Fame.FameBuffer().ExportServices(builder);
            new StageProgressBuffer().ExportServices(builder);
            new CombatViewBuffer().ExportServices(builder);
            new StageViewBuffer().ExportServices(builder);
            new ArtifactViewBuffer().ExportServices(builder);
            new ShopViewBuffer().ExportServices(builder);
            new RecoveryViewBuffer().ExportServices(builder);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery.RecoveryResultBuffer().ExportServices(builder);
            new LobbyViewBuffer().ExportServices(builder);
            new TutorialBuffer().ExportServices(builder);

            return new BufferSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
