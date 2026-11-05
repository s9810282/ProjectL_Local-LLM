using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageEnvironment.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 지형 원본 하나를 어떤 모양·방향으로 그릴지 정하는 유일한 곳입니다(지형 이웃 모양 설계 TC3, 16가지 표는 계획 §2.3).
    /// 이웃 모양을 쓰지 않는 종류는 기본 프리팹 + 스테이지 외형 회전이라 지금과 같습니다.
    /// 빈 모양 칸은 기본 프리팹(Center)을 R0으로 그립니다(설계 Q-C5).
    /// </summary>
    internal static class CombatTerrainLook
    {
        // 드러난 변 비트입니다(이웃이 없는 쪽). 북은 필드 +Z(Core +Y)이고 비트는 시계 방향 순서입니다.
        private const int North = 1;
        private const int East = 2;
        private const int South = 4;
        private const int West = 8;

        /// <summary>
        /// 드러난 변 비트(0~15)의 모양과 회전입니다. 회전은 모양의 R0 비트(<see cref="TerrainEdgeShape"/> 문서)를
        /// 시계 방향으로 몇 번 돌리면 이 비트가 되는지의 가장 작은 수입니다. 할당이 없습니다.
        /// </summary>
        internal static (TerrainEdgeShape Shape, Rotation4 Rotation) ShapeOf(int exposed)
        {
            if (exposed < 0 || exposed > 15)
                throw new ArgumentOutOfRangeException(nameof(exposed), exposed, "드러난 변 비트는 0~15여야 합니다.");

            TerrainEdgeShape shape = ShapeByCount(exposed);
            int mask = CanonicalOf(shape);
            for (int turn = 0; turn < 4; turn++)
            {
                if (mask == exposed) return (shape, (Rotation4)turn);
                mask = RotateClockwise(mask);
            }

            // 모양마다 R0 비트를 돌리면 같은 수의 비트 조합을 모두 지나므로 닿지 않습니다.
            throw new InvalidOperationException($"[CombatTerrainLook] 드러난 변 {exposed}의 회전을 찾지 못했습니다.");
        }

        /// <summary>
        /// 원본 하나의 외형 선택입니다. 모양을 쓰는 종류는 같은 종류의 보이는 이웃(<paramref name="neighbors"/>)으로 모양과 회전을 정하고,
        /// 그 원본에 스테이지 외형 회전이 적혀 있으면 예외입니다(모양이 회전을 정함, 설계 Q-C3).
        /// </summary>
        internal static TerrainLook Resolve(TerrainContentKind kind, TerrainVisualData visual, MovementTileFact source,
            StageEnvironmentVisualData rotations, FieldTerrainNeighbors neighbors)
        {
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (!visual.UsesEdgeShapes)
                return new TerrainLook(new TerrainLookKey(kind, TerrainEdgeShape.Center),
                    rotations?.GetTerrainRotation(source.Id) ?? Rotation4.R0);

            if (rotations != null && rotations.TryGetTerrainRotation(source.Id, out _))
                throw new InvalidOperationException(
                    $"[FieldTerrainPresenter] 이웃 모양을 쓰는 {kind}의 원본 {source.Id}에는 스테이지 외형 회전을 넣을 수 없습니다(모양이 회전을 정합니다).");
            if (neighbors == null) throw new ArgumentNullException(nameof(neighbors));

            CellCoord cell = source.Cell;
            int exposed = 0;
            if (!neighbors.Has(new CellCoord(cell.X, cell.Y + 1), kind)) exposed |= North;
            if (!neighbors.Has(new CellCoord(cell.X + 1, cell.Y), kind)) exposed |= East;
            if (!neighbors.Has(new CellCoord(cell.X, cell.Y - 1), kind)) exposed |= South;
            if (!neighbors.Has(new CellCoord(cell.X - 1, cell.Y), kind)) exposed |= West;

            (TerrainEdgeShape shape, Rotation4 rotation) = ShapeOf(exposed);
            if (shape != TerrainEdgeShape.Center && visual.ShapePrefab(shape) == null)
                return new TerrainLook(new TerrainLookKey(kind, TerrainEdgeShape.Center), Rotation4.R0);
            return new TerrainLook(new TerrainLookKey(kind, shape), rotation);
        }

        private static TerrainEdgeShape ShapeByCount(int exposed)
        {
            int count = (exposed & 1) + ((exposed >> 1) & 1) + ((exposed >> 2) & 1) + ((exposed >> 3) & 1);
            return count switch
            {
                0 => TerrainEdgeShape.Center,
                1 => TerrainEdgeShape.OneSide,
                2 => exposed == (North | South) || exposed == (East | West) ? TerrainEdgeShape.TwoSideOpposite : TerrainEdgeShape.TwoSide,
                3 => TerrainEdgeShape.ThreeSide,
                _ => TerrainEdgeShape.FourSide
            };
        }

        // R0 기준 드러난 쪽입니다. 코드가 쓰는 값은 여기 하나이고, TerrainEdgeShape 문서·SO 툴팁·안내 문서는 이 값의 설명입니다(계획 §2.2).
        private static int CanonicalOf(TerrainEdgeShape shape) => shape switch
        {
            TerrainEdgeShape.Center => 0,
            TerrainEdgeShape.OneSide => East,
            TerrainEdgeShape.TwoSide => North | East,
            TerrainEdgeShape.TwoSideOpposite => East | West,
            TerrainEdgeShape.ThreeSide => North | East | West,
            TerrainEdgeShape.FourSide => North | East | South | West,
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "정의되지 않은 이웃 모양입니다.")
        };

        // 북 → 동 → 남 → 서 → 북입니다. Rotation4도 시계 방향입니다.
        private static int RotateClockwise(int mask) => ((mask << 1) | (mask >> 3)) & 15;
    }
}
