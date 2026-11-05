using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;

namespace ProjectL.Systems.ServiceKeys.DBFactory
{
    internal static class DataBaseServiceKey
    {
        public static readonly SystemGroupKey<IHasArtifactBoardConfig> ArtifactBoardConfig =
            new(nameof(ArtifactBoardConfig));
    }
}
