using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;

namespace ProjectL.Systems.ServiceKeys.DBFactory.Group
{
    internal static class RenderTableKey
    {
        public static readonly SystemGroupKey<IEffectTableQuery> Effect =
            new(nameof(Effect));

        public static readonly SystemGroupKey<IMetrialTableQuery> Metrial =
            new(nameof(Metrial));

        public static readonly SystemGroupKey<ILightTableQuery> Light =
            new(nameof(Light));
    }
}
