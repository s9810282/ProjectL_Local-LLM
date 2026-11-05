using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using AppointShapeCalculator = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat.AppointShapeCalculator;
using AttackDirection = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.AttackDirection;
using CellCoord = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord;
using TargetingShape = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TargetingShape;
using TargetingSplash = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TargetingSplash;

namespace ProjectL.Tool.Editor
{
    // 적 공격 범위 미리보기(참고용, 추가 설계 AD2)입니다. 모양은 Core AppointShapeCalculator 한 곳에서 얻고, 도구는 유닛 칸에 옮기기만 합니다.
    // 벽·지형·고지대·모래폭풍·유닛 점유·대포 인접 금지 같은 전투 판정은 반영하지 않습니다.
    internal static class CombatStageSoRange
    {
        // 방향 값 0(None)은 모든 방향을 합친 모양입니다(Q-AD2 C의 기본).
        internal const int AllDirections = (int)AttackDirection.None;

        private static readonly AttackDirection[] Directions = ((AttackDirection[])Enum.GetValues(typeof(AttackDirection)))
            .Where(direction => direction != AttackDirection.None).ToArray();

        internal static CombatStageSoRangeShape ShapeOf(Object unitDefinition, int direction)
        {
            if (unitDefinition == null) return CombatStageSoRangeShape.Fail("유닛 정의가 없습니다.");
            if (!CombatStageSoAssets.ReadAttack(unitDefinition, out int range, out int shapeValue, out int splashValue))
                return CombatStageSoRangeShape.Fail($"{unitDefinition.name}: 지정 방식 SO가 없어 범위를 그릴 수 없습니다.");
            var shape = (TargetingShape)shapeValue;
            try
            {
                CellCoord[] reach = direction == AllDirections
                    ? AppointShapeCalculator.CreateBaseMask(shape, range)
                    : AppointShapeCalculator.CreateDirectionMask(shape, range, (AttackDirection)direction);
                CellCoord[] splash = AppointShapeCalculator.CreateSplashOffsets((TargetingSplash)splashValue);
                return new CombatStageSoRangeShape(ToVectors(reach), ToVectors(splash), DirectionsOf(shape, range), null);
            }
            catch (ArgumentException exception)
            {
                return CombatStageSoRangeShape.Fail($"{unitDefinition.name}: 범위를 만들 수 없습니다({exception.Message})");
            }
        }

        // 유닛이 차지한 칸마다 모양을 더하고 맵 밖은 버립니다. 스플래시로만 닿는 칸은 따로 담습니다.
        internal static void AddTo(CombatStageSoRangeShape shape, IEnumerable<Vector2Int> origins, CombatStageSoGrid grid,
            Dictionary<Vector2Int, int> reachCounts, HashSet<Vector2Int> splashCells)
        {
            var reach = new HashSet<Vector2Int>();
            foreach (Vector2Int origin in origins)
                foreach (Vector2Int offset in shape.Reach)
                    if (grid.Contains(origin + offset)) reach.Add(origin + offset);
            foreach (Vector2Int cell in reach) reachCounts[cell] = reachCounts.TryGetValue(cell, out int count) ? count + 1 : 1;
            foreach (Vector2Int cell in reach)
                foreach (Vector2Int offset in shape.Splash)
                    if (grid.Contains(cell + offset) && !reach.Contains(cell + offset)) splashCells.Add(cell + offset);
        }

        internal static string DirectionLabel(int direction)
        {
            switch ((AttackDirection)direction)
            {
                case AttackDirection.Up: return "↑";
                case AttackDirection.UpRight: return "↗";
                case AttackDirection.Right: return "→";
                case AttackDirection.DownRight: return "↘";
                case AttackDirection.Down: return "↓";
                case AttackDirection.DownLeft: return "↙";
                case AttackDirection.Left: return "←";
                case AttackDirection.UpLeft: return "↖";
                default: return "전체";
            }
        }

        // 고를 수 있는 방향입니다. 사각형(RadialSquare)은 방향이 없는 모양이라 비우고, 나머지는 Core가 받아 주는 방향만 냅니다.
        private static int[] DirectionsOf(TargetingShape shape, int range)
        {
            if (shape == TargetingShape.RadialSquare) return Array.Empty<int>();
            return Directions.Where(direction => Supports(shape, range, direction)).Select(direction => (int)direction).ToArray();
        }

        private static bool Supports(TargetingShape shape, int range, AttackDirection direction)
        {
            try
            {
                AppointShapeCalculator.CreateDirectionMask(shape, range, direction);
                return true;
            }
            catch (ArgumentOutOfRangeException) { return false; }
        }

        private static Vector2Int[] ToVectors(CellCoord[] cells) => cells.Select(cell => new Vector2Int(cell.X, cell.Y)).ToArray();
    }

    // 유닛 하나의 범위 모양(유닛 칸 기준 상대 칸)입니다. Problem이 있으면 그리지 못한 이유입니다.
    internal sealed class CombatStageSoRangeShape
    {
        public readonly Vector2Int[] Reach, Splash;
        public readonly int[] Directions;
        public readonly string Problem;

        public CombatStageSoRangeShape(Vector2Int[] reach, Vector2Int[] splash, int[] directions, string problem)
        {
            Reach = reach; Splash = splash; Directions = directions; Problem = problem;
        }

        public static CombatStageSoRangeShape Fail(string problem) =>
            new CombatStageSoRangeShape(Array.Empty<Vector2Int>(), Array.Empty<Vector2Int>(), Array.Empty<int>(), problem);
    }
}
