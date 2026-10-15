using System;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem
{
    public readonly struct RenderFeatureKey<TService>
        where TService : class, IServiceModule
    {
        internal RenderFeatureType FeatureType { get; }

        public RenderFeatureKey(RenderFeatureType featureType)
        {
            if (featureType == RenderFeatureType.None)
                throw new ArgumentOutOfRangeException(nameof(featureType));

            FeatureType = featureType;
        }

        public override string ToString() => FeatureType.ToString();
    }

}
