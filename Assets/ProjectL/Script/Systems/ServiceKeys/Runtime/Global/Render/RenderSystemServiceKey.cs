using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.DrawPointLineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.SelectOutlineSystem;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global.Render
{
    internal static class RenderSystemServiceKey
    {
        public static readonly SystemGroupKey<IEffectSystemServices> ParticleSystem =
            new(nameof(ParticleSystem));

        public static readonly SystemGroupKey<IMeshSystemService> MeshSystem =
            new(nameof(MeshSystem));

        public static readonly SystemGroupKey<IColorSystemService> ColorSystem =
            new(nameof(ColorSystem));

        public static readonly SystemGroupKey<ILightSystemService> LightSystem = 
            new(nameof(LightSystem));

        public static readonly SystemGroupKey<ISelectOutlineSystemService> SelectOutlineSystem =
            new(nameof(SelectOutlineSystem));

        public static readonly SystemGroupKey<IAreaOutlineSystemService> AreaOutlineSystem =
            new(nameof(AreaOutlineSystem));

        public static readonly SystemGroupKey<IDrawPointLineSystemService> DrawPointLineSystem =
            new(nameof(DrawPointLineSystem));

        public static readonly SystemGroupKey<IRenderFeatureSystemService> RenderFeatureSystem =
            new(nameof(RenderFeatureSystem));
    }
}
