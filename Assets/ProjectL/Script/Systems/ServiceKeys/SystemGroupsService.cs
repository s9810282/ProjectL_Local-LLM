using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.OutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;

namespace ProjectL.Systems.ServiceKeys
{
    internal static class InGameSystemServiceKey
    {
        public static readonly SystemGroupKey<ICharacterRosterService> Characters = new(nameof(Characters));
        public static readonly SystemGroupKey<ICharacterShopService> Shop = new(nameof(Shop));
        public static readonly SystemGroupKey<IRecoveryStageService> Recovery = new(nameof(Recovery));
        public static readonly SystemGroupKey<ICombatProcessingService> Combat = new(nameof(Combat));
        public static readonly SystemGroupKey<IArtifactEditingService> Artifacts = new(nameof(Artifacts));
        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem.IStageGeneratorSystem> StageGenerator =
            new(nameof(StageGenerator));
        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem.IStageServiceManager> StageServiceManager =
            new(nameof(StageServiceManager));
        public static readonly SystemGroupKey<IChapterProgressionCoordinator> ChapterProgression =
            new(nameof(ChapterProgression));
        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem.IRunStartService> RunStart =
            new(nameof(RunStart));
    }

    internal static class InGameSystemGroupServiceKey
    {
        public static readonly SystemGroupKey<IInGameCoreSystemGroupService> Core =
            new(nameof(Core));
        public static readonly SystemGroupKey<IInGameViewSystemGroupService> View =
            new(nameof(View));
    }

    internal static class LifeCycleSystemServiceKey
    {
        public static readonly SystemGroupKey<ICombatEntryService> CombatEntry = new(nameof(CombatEntry));
        public static readonly SystemGroupKey<ICombatExitService> CombatExit = new(nameof(CombatExit));
        public static readonly SystemGroupKey<ICombatProgressReader> CombatProgress = new(nameof(CombatProgress));
        public static readonly SystemGroupKey<ILifeCycleManager> Manager =
            new(nameof(Manager));
    }

    internal static class JobSystemServiceKey
    {
        public static readonly SystemGroupKey<IJobRunner> JobRunner =
            new(nameof(JobRunner));
        public static readonly SystemGroupKey<IJobRegisterSystem> JobRegister =
            new(nameof(JobRegister));
    }

    public static class SystemGroupsService
    {
        public static readonly SystemGroupKey<IJobSystemServiceContext> Job =
            new(nameof(Job));
        public static readonly SystemGroupKey<IGlobalSystemGroupService> Global =
            new(nameof(Global));
        public static readonly SystemGroupKey<IInGameSystemGroupService> InGame =
            new(nameof(InGame));
        public static readonly SystemGroupKey<ILifeCycleSystemGroupService> LifeCycle =
            new(nameof(LifeCycle));
        public static readonly SystemGroupKey<IRenderServiceContext> Render =
            new(nameof(Render));
        public static readonly SystemGroupKey<IUtilitySystemServiceContext> Utility =
            new(nameof(Utility));
        public static readonly SystemGroupKey<IInputSystemServiceContext> Input =
            new(nameof(Input));
        public static readonly SystemGroupKey<IUISystemService> UI =
            new(nameof(UI));
        public static readonly SystemGroupKey<ILodingSystemServiceContext> Loding =
            new(nameof(Loding));
    }

    internal static class CameraSystemServiceKey
    {
        public static readonly SystemGroupKey<ICameraCatalogService> Catalog = new(nameof(Catalog));
        public static readonly SystemGroupKey<ICameraEffectService> Effect = new(nameof(Effect));
    }

    internal static class RenderSystemServiceKey
    {
        public static readonly SystemGroupKey<ICameraGroupServiceContext> CameraGroup =
            new(nameof(CameraGroup));

        public static readonly SystemGroupKey<IEffectSystemServices> ParticleSystem =
            new(nameof(ParticleSystem));

        public static readonly SystemGroupKey<IMeshSystemService> MeshSystem =
            new(nameof(MeshSystem));

        public static readonly SystemGroupKey<IColorSystemService> ColorSystem =
            new(nameof(ColorSystem));

        public static readonly SystemGroupKey<ILightSystemService> LightSystem = 
            new(nameof(LightSystem));

        public static readonly SystemGroupKey<IOutlineSystemService> OutlineSystem =
            new(nameof(OutlineSystem));

        public static readonly SystemGroupKey<IAreaOutlineSystemService> AreaOutlineSystem =
            new(nameof(AreaOutlineSystem));

        public static readonly SystemGroupKey<IRenderFeatureSystemService> RenderFeatureSystem =
            new(nameof(RenderFeatureSystem));
    }

    internal static class UtilitySystemServiceKey
    {
        public static readonly SystemGroupKey<IDelayService> Delay =
            new(nameof(Delay));

        public static readonly SystemGroupKey<IEventSystemService> Event =
            new(nameof(Event));
    }

    internal static class LodingSystemServiceKey
    {
        public static readonly SystemGroupKey<ILodingRegisterService> Register =
            new(nameof(Register));

        public static readonly SystemGroupKey<ILodingRegisterReader> Reader =
            new(nameof(Reader));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem.ILodingSystemService> Loding =
            new(nameof(Loding));
    }
}
