using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>Buffer 원본에서 View DB 종류 키로 변환하는 단일 매핑입니다.</summary>
    internal static class EnvironmentVisualKinds
    {
        internal static ObjectContentKind ObjectKind(ObjectSourceFact source) => source switch
        {
            MineSourceFact => ObjectContentKind.Mine,
            FlagSourceFact => ObjectContentKind.Flag,
            SheriffBadgeSourceFact => ObjectContentKind.SheriffBadge,
            _ => throw new InvalidOperationException($"지원하지 않는 Object 원본입니다: {source?.GetType().Name}.")
        };

        internal static TerrainContentKind TerrainKind(MovementTileFact source) => source switch
        {
            StaticTerrainFact { Kind: StaticTerrainKind.HighGround } => TerrainContentKind.HighGround,
            StaticTerrainFact { Kind: StaticTerrainKind.Sandstorm } => TerrainContentKind.Sandstorm,
            StaticTerrainFact { Kind: StaticTerrainKind.Wall } => TerrainContentKind.Wall,
            StaticTerrainFact { Kind: StaticTerrainKind.WeakGround } => TerrainContentKind.WeakGround,
            StaticTerrainFact { Kind: StaticTerrainKind.Cliff } => TerrainContentKind.Cliff,
            DestinationTileModifierFact => TerrainContentKind.PresenceModifier,
            _ => throw new InvalidOperationException($"지원하지 않는 Terrain 원본입니다: {source?.GetType().Name}.")
        };
    }
}
