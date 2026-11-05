using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;

namespace ProjectL.Systems.ServiceKeys.DBFactory
{
    internal static class DataBaseGroupKey
    {
        public static readonly SystemGroupKey<IRenderQuery> Render =
            new(nameof(Render));

        public static readonly SystemGroupKey<IInGameQuery> InGame =
            new(nameof(InGame));

        public static readonly SystemGroupKey<IViewQuery> View =
            new(nameof(View));
    }
}
