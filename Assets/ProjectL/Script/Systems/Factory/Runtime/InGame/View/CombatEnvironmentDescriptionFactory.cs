using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.RuntimeSystem.Common.Descriptions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>환경 호버 연결 한 번의 설명 틀을 준비합니다. 등록된 외형과 기능별 토큰 계약을 여기서 연결합니다.</summary>
    internal static class CombatEnvironmentDescriptionFactory
    {
        internal static CombatEnvironmentDescriptions Create(CombatDisplayTables tables)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            var objects = new Dictionary<ObjectContentKind, DescriptionTemplate<ObjectSourceFact>>();
            foreach (ObjectContentKind kind in Enum.GetValues(typeof(ObjectContentKind)))
            {
                if (kind == ObjectContentKind.None || !tables.Objects.TryGetObjectVisual(kind, out var visual)) continue;
                objects.Add(kind, new DescriptionTemplate<ObjectSourceFact>($"ObjectVisual.{kind}.Description",
                    visual.Description, CombatEnvironmentTokenBindings.Object(kind)));
            }

            var terrains = new Dictionary<TerrainContentKind, DescriptionTemplate<MovementTileFact>>();
            foreach (TerrainContentKind kind in Enum.GetValues(typeof(TerrainContentKind)))
            {
                if (kind == TerrainContentKind.None || kind == TerrainContentKind.Tornado ||
                    !tables.Terrains.TryGetTerrainVisual(kind, out var visual)) continue;
                terrains.Add(kind, new DescriptionTemplate<MovementTileFact>($"TerrainVisual.{kind}.Description",
                    visual.Description, CombatEnvironmentTokenBindings.Terrain(kind)));
            }

            var obstacles = new Dictionary<ObstacleContentKind, DescriptionTemplate<ObstacleContentProfile>>();
            foreach (ObstacleContentKind kind in Enum.GetValues(typeof(ObstacleContentKind)))
            {
                var key = ObstacleContentProfile.KeyFor(kind);
                if (!tables.Units.TryGetUnitVisual(key, out var visual)) continue;
                obstacles.Add(kind, new DescriptionTemplate<ObstacleContentProfile>($"UnitVisual.{key}.Description",
                    visual.Description, CombatEnvironmentTokenBindings.Obstacle(kind)));
            }

            // 이 기능을 사용하지 않는 DB 구성에서는 외형 누락을 허용합니다. 실제 사용 시 기존 Get 경로가 필수 외형을 요구합니다.
            var dustScreen = tables.Traits.TryGetTraitVisual(TraitKey.DustScreen, out var trail)
                ? new DescriptionTemplate<TraitTrailFact>("TraitVisual.DustScreen.TrailDescription",
                    trail.TrailDescription, CombatEnvironmentTokenBindings.DustScreen()) : null;
            var tornado = tables.Terrains.TryGetTerrainVisual(TerrainContentKind.Tornado, out var tornadoVisual)
                ? new DescriptionTemplate<TornadoMemberSpec>("TerrainVisual.Tornado.Description",
                    tornadoVisual.Description, CombatEnvironmentTokenBindings.Tornado()) : null;
            return new CombatEnvironmentDescriptions(objects, terrains, obstacles, dustScreen, tornado,
                new DescriptionTemplate<RockfallGimmickSpec>("RockfallVisual.Description", tables.Rockfall.Description,
                    CombatEnvironmentTokenBindings.Rockfall()),
                new DescriptionTemplate<HaboobGimmickState>("HaboobVisual.Description", tables.Haboob.Description));
        }
    }
}
