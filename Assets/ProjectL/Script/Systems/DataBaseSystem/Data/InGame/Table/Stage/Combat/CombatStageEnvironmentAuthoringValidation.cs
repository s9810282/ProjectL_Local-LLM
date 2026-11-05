using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    /// <summary>SO의 환경 입력만 검사합니다. 유닛·본부를 포함한 최종 배치는 Core 준비가 검사합니다.</summary>
    internal static class CombatStageEnvironmentAuthoringValidation
    {
        internal static void Validate(CombatStageDetail stage, int width, int height)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            // 최초 무작위 토네이도는 칸이 없으므로 공간 검증에서 빼고 ID만 예약한다.
            var environment = StageEnvironmentAdapter.Read(width, height, stage.TerrainPresencePlacements,
                stage.StaticTerrainPlacements, stage.ObjectPlacements, stage.TornadoPlacements, stage.WallCells,
                stage.InitialRandomTornadoPlacements.Select(p => p.SourceId).ToArray());
            var obstacles = new StructureCombatFacts[stage.ObstaclePlacements.Count];
            for (int i = 0; i < obstacles.Length; i++)
            {
                var placement = stage.ObstaclePlacements[i];
                var definition = placement.Definition;
                // 이 SO의 장애물 정의는 꽉 찬 직사각형입니다. 임의 모양의 회전 공식을 복제하지 않습니다.
                bool swap = placement.Rotation == Rotation4.R90 || placement.Rotation == Rotation4.R270;
                int columns = swap ? definition.FootprintHeight : definition.FootprintWidth;
                int rows = swap ? definition.FootprintWidth : definition.FootprintHeight;
                if (placement.Anchor.X < 0 || placement.Anchor.Y < 0 ||
                    (long)placement.Anchor.X + columns > width || (long)placement.Anchor.Y + rows > height)
                {
                    throw new InvalidOperationException($"Obstacle Placements[{i}]의 회전된 점유 영역이 필드를 벗어납니다.");
                }

                var occupied = new CellCoord[checked(columns * rows)];
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        occupied[y * columns + x] = new CellCoord(placement.Anchor.X + x, placement.Anchor.Y + y);
                    }
                }

                // 검증 안에서만 쓰는 임시 ID이며 운영 개체 ID를 SO에 기록하지 않습니다.
                obstacles[i] = new StructureCombatFacts(new EntityId(i + 1), definition,
                    new HealthStateSnapshot(definition.MaxHealth, definition.MaxHealth),
                    placement.Anchor, placement.Rotation, occupied, Owner.Neutral);
            }

            var bindings = new EnvironmentSemanticBindings(environment.Bindings.TerrainSourceIds,
                environment.Bindings.ObjectSourceIds, obstacles.Select(o => o.EntityId).ToArray());
            CellEnvironmentQuery.ValidateInitialPlacement(width, height, environment.Environment, obstacles,
                Array.Empty<CharacterCombatFacts>(), bindings, stage.WallBlocking,
                stage.Railway?.RailCells ?? Array.Empty<CellCoord>());
        }
    }
}
