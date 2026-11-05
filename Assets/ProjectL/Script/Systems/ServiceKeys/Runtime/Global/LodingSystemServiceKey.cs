using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global
{
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
