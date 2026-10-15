using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem
{
    public enum RenderFeatureType
    {
        None = 0,
        StencilBlending = 1,
        VolumetricLight = 2,
        EnvironmentalScreen = 3,
        SobelEdge = 4,
        FadeInOut = 5,
    }

    public interface IRenderFeatureNotifireService
    {
        RenderFeatureType Type { get; }
        IServiceModule SettingService { get; }
        void UseRenderFreature(bool isRender);
    }
}
