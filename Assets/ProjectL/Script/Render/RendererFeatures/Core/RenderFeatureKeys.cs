using ProjectL.Render.Shader.PostProcessing.EnvironmentalScreenPost;
using ProjectL.Render.Shader.PostProcessing.FadeInOut;
using ProjectL.Render.Shader.PostProcessing.SovelEdge;
using ProjectL.Render.Shader.PostProcessing.Stencil;
using ProjectL.Render.Shader.PostProcessing.VolmetrickLight;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;

namespace ProjectL.Render.Shader.PostProcessing.Core
{
    public static class RenderFeatureKeys
    {
        public static readonly RenderFeatureKey<IStencilSettingService> Stencil =
            new(RenderFeatureType.StencilBlending);

        public static readonly RenderFeatureKey<IVolmetrickLightSettingService> VolumetricLight =
            new(RenderFeatureType.VolumetricLight);

        public static readonly RenderFeatureKey<IEnvironmentalScreenSettingService> EnvironmentalScreen =
            new(RenderFeatureType.EnvironmentalScreen);

        public static readonly RenderFeatureKey<ISobelEdgeSettingService> SobelEdge =
            new(RenderFeatureType.SobelEdge);

        public static readonly RenderFeatureKey<IFadeInOutSettingService> FadeInOut =
            new(RenderFeatureType.FadeInOut);
    }
}
