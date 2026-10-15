using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>현재 구현이 지원하는 환경 콘텐츠 범위다. 새 콘텐츠를 활성화할 때 이 판정만 고친다.
    /// 분류는 바인딩이 소유하며 여기서는 타입이 나타내는 내용의 지원 여부만 판단한다.</summary>
    public static class EnvironmentContentSupport
    {
        /// <summary>초기 배치에서 Terrain으로 분류할 수 있는 원본이다.</summary>
        public static bool IsSupportedInitialTerrain(MovementTileFact source) =>
            source is DestinationTileModifierFact || IsExclusiveTerrain(source);

        /// <summary>같은 셀의 다른 Terrain과 아직 조합하지 않는 원본이다.</summary>
        public static bool IsExclusiveTerrain(MovementTileFact source) =>
            source is StaticTerrainFact || source is TornadoSourceFact;

        /// <summary>도착 처리와 생성이 구현된 Object다.</summary>
        public static bool IsSupportedObject(MovementTileFact source) =>
            source is SheriffBadgeSourceFact || source is MineSourceFact || source is FlagSourceFact;
    }
}
