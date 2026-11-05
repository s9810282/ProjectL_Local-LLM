using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.RuntimeSystem.Common.Descriptions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>환경 설명의 허용 토큰과 공개값 연결 원본입니다. 같은 이름도 설명 종류 안에서만 의미를 가집니다.</summary>
    internal static class CombatEnvironmentTokenBindings
    {
        internal static DescriptionToken<ObjectSourceFact>[] Object(ObjectContentKind kind) => kind switch
        {
            ObjectContentKind.Mine => new[]
            {
                DescriptionToken<ObjectSourceFact>.From<MineSourceFact>("피해량", source => source.Damage, "n")
            },
            ObjectContentKind.Flag => new[]
            {
                DescriptionToken<ObjectSourceFact>.From<FlagSourceFact>("우선도증가량", source => source.PriorityBonus, "n")
            },
            // 배지는 현재 공개 수치가 없는 고정 설명입니다. 임의 토큰을 허용하지 않습니다.
            ObjectContentKind.SheriffBadge => Array.Empty<DescriptionToken<ObjectSourceFact>>(),
            _ => throw new InvalidOperationException($"[CombatEnvironmentTokenBindings] Object 설명 등록이 없습니다: {kind}.")
        };

        internal static DescriptionToken<MovementTileFact>[] Terrain(TerrainContentKind kind) => kind switch
        {
            TerrainContentKind.WeakGround => new[]
            {
                DescriptionToken<MovementTileFact>.From<StaticTerrainFact>("붕괴횟수", source => source.EntryThreshold, "n"),
                DescriptionToken<MovementTileFact>.From<StaticTerrainFact>("남은횟수", source => source.RemainingEntries)
            },
            TerrainContentKind.PresenceModifier => new[]
            {
                DescriptionToken<MovementTileFact>.From<DestinationTileModifierFact>("공격력증가량", source => source.AttackBonus),
                DescriptionToken<MovementTileFact>.From<DestinationTileModifierFact>("최대체력증가량", source => source.MaxHealthBonus)
            },
            // 고지대·모래폭풍 수치 공개는 후속 요청입니다. 기존 고정 문구만 보존합니다.
            TerrainContentKind.HighGround or TerrainContentKind.Sandstorm or TerrainContentKind.Wall or TerrainContentKind.Cliff =>
                Array.Empty<DescriptionToken<MovementTileFact>>(),
            _ => throw new InvalidOperationException($"[CombatEnvironmentTokenBindings] 정적 지형 설명 등록이 없습니다: {kind}.")
        };

        internal static DescriptionToken<ObstacleContentProfile>[] Obstacle(ObstacleContentKind kind) => kind switch
        {
            ObstacleContentKind.PowderBarrel => new[]
            {
                new DescriptionToken<ObstacleContentProfile>("피해량", source => source.ExplosionDamage, "n")
            },
            // 피격/파괴 보상은 독립된 공개값이므로 하나의 토큰으로 합치지 않습니다.
            ObstacleContentKind.Supplies => new[]
            {
                new DescriptionToken<ObstacleContentProfile>("피격공격력증가량", source => source.HitAttackBonus),
                new DescriptionToken<ObstacleContentProfile>("파괴공격력증가량", source => source.DestructionAttackBonus)
            },
            _ => throw new InvalidOperationException($"[CombatEnvironmentTokenBindings] 장애물 설명 등록이 없습니다: {kind}.")
        };

        internal static DescriptionToken<TraitTrailFact>[] DustScreen() => new[]
        {
            new DescriptionToken<TraitTrailFact>("사거리감소량", source => source.AttackRangeReduction),
            new DescriptionToken<TraitTrailFact>("피해감소율", source => source.AttackDamageReductionPercent)
        };

        internal static DescriptionToken<TornadoMemberSpec>[] Tornado() => new[]
        {
            new DescriptionToken<TornadoMemberSpec>("이동주기", source => source.RepeatIntervalControllerTurns, "n")
        };

        internal static DescriptionToken<RockfallGimmickSpec>[] Rockfall() => new[]
        {
            new DescriptionToken<RockfallGimmickSpec>("최대체력손실률", source => source.MaximumHealthLossPercent, "n")
        };
    }
}
