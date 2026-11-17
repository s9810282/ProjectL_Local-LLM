using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public static class AppointShapeCalculator
    {
        /// <summary>광선 인덱스는 전방·왼쪽 대각·오른쪽 대각 순서다. 정렬된 범위 마스크와 구분한다.</summary>
        internal static CellCoord[] CreateOrderedRaySteps(TargetingShape shape, AttackDirection direction)
        {
            ValidateShape(shape);
            if (shape != TargetingShape.CardinalLine && shape != TargetingShape.DiagonalLine &&
                shape != TargetingShape.EightDirectionLine)
                throw new NotSupportedException("광선 형태의 지정 방식만 순서 있는 방향을 제공합니다.");
            if (!IsDirectionSupported(shape, direction)) throw new ArgumentOutOfRangeException(nameof(direction));
            var forward = GetDirectionStep(direction);
            if (shape != TargetingShape.EightDirectionLine) return new[] { forward };
            var right = new CellCoord(forward.Y, -forward.X);
            return new[] { forward, forward - right, forward + right };
        }

        private static readonly CellCoord[] CardinalSteps =
        {
            CellCoord.Up, CellCoord.Right, CellCoord.Down, CellCoord.Left,
        };
        private static readonly CellCoord[] DiagonalSteps =
        {
            new CellCoord(1, 1), new CellCoord(1, -1),
            new CellCoord(-1, -1), new CellCoord(-1, 1),
        };
        private static readonly CellCoord[] EightSteps =
        {
            CellCoord.Up, new CellCoord(1, 1), CellCoord.Right, new CellCoord(1, -1),
            CellCoord.Down, new CellCoord(-1, -1), CellCoord.Left, new CellCoord(-1, 1),
        };

        public static CellCoord[] CreateBaseMask(TargetingShape shape, int range, int width = 0)
        {
            ValidateRange(range);
            ValidateShape(shape);
            switch (shape)
            {
                case TargetingShape.CardinalLine: return MergeRays(CardinalSteps, range);
                case TargetingShape.DiagonalLine: return MergeRays(DiagonalSteps, range);
                case TargetingShape.EightDirectionLine: return MergeRays(EightSteps, range);
                case TargetingShape.Fan:
                    return CreateFilledMask(shape, range, null, CheckedCellCount((long)range * range, 4));
                case TargetingShape.ForwardRectangle:
                    return RectangleMaskSteps(range, width, CardinalSteps).Where(c => c.HasValue).Select(c => c.Value).ToArray();
                default: return CreateSquare(range);
            }
        }

        public static CellCoord[] CreateDirectionMask(TargetingShape shape, int range, AttackDirection direction, int width = 0)
        {
            ValidateRange(range);
            ValidateShape(shape);
            if (IsDirectionSupported(shape, direction) == false)
                throw new ArgumentOutOfRangeException(nameof(direction));
            CellCoord forward = GetDirectionStep(direction);
            switch (shape)
            {
                case TargetingShape.CardinalLine:
                case TargetingShape.DiagonalLine:
                case TargetingShape.EightDirectionLine:
                    return MergeRays(CreateOrderedRaySteps(shape, direction), range);
                case TargetingShape.Fan:
                    return CreateFilledMask(shape, range, forward, CheckedCellCount((long)range * range));
                case TargetingShape.ForwardRectangle:
                    return RectangleMaskSteps(range, width, new[] { forward }).Select(c => c.Value).ToArray();
                default:
                    return CreateFilledMask(shape, range, forward, CheckedCellCount((long)range * (2L * range + 1L)));
            }
        }

        /// <summary>행·열 순서를 유지하고, 칸을 내지 않는 검사도 한 진행 단위로 남긴다.</summary>
        internal static IEnumerable<CellCoord?> MaskSteps(TargetingShape shape, int range, AttackDirection? direction, int width = 0)
        {
            ValidateRange(range);
            ValidateShape(shape);
            if (direction.HasValue && !IsDirectionSupported(shape, direction.Value))
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (shape is TargetingShape.CardinalLine or TargetingShape.DiagonalLine or TargetingShape.EightDirectionLine)
            {
                var steps = direction.HasValue ? CreateOrderedRaySteps(shape, direction.Value) :
                    shape == TargetingShape.CardinalLine ? CardinalSteps :
                    shape == TargetingShape.DiagonalLine ? DiagonalSteps : EightSteps;
                CheckedCellCount(range, steps.Length);
                foreach (var cell in RayMaskSteps(steps, range)) yield return cell;
                yield break;
            }
            var forward = direction.HasValue ? GetDirectionStep(direction.Value) : (CellCoord?)null;
            if (shape == TargetingShape.ForwardRectangle)
            {
                foreach (var cell in RectangleMaskSteps(range, width, forward.HasValue ? new[] { forward.Value } : CardinalSteps))
                    yield return cell;
                yield break;
            }
            if (shape == TargetingShape.Fan) CheckedCellCount((long)range * range, forward.HasValue ? 1 : 4);
            else if (forward.HasValue) CheckedCellCount((long)range * (2L * range + 1L));
            else CheckedSquareCount(range);
            foreach (var cell in FilledMaskSteps(shape, range, forward)) yield return cell;
        }

        public static CellCoord[] CreateSplashOffsets(TargetingSplash splash)
        {
            switch (splash)
            {
                case TargetingSplash.None: return Array.Empty<CellCoord>();
                case TargetingSplash.Cross: return MergeRays(CardinalSteps, 1);
                case TargetingSplash.Diagonal: return MergeRays(DiagonalSteps, 1);
                case TargetingSplash.Radial: return CreateSquare(1);
                default: throw new ArgumentOutOfRangeException(nameof(splash));
            }
        }

        internal static bool ContainsBaseMask(TargetingShape shape, int range, long x, long y, int width = 0)
        {
            ValidateRange(range);
            ValidateShape(shape);
            if (shape == TargetingShape.ForwardRectangle)
            {
                int first = RectangleLateralStart(width);
                foreach (var forward in CardinalSteps)
                    if (ContainsRectangle(x, y, forward, range, first, first + width - 1))
                        return true;
                return false;
            }
            if (ContainsSquare(x, y, range) == false)
                return false;
            switch (shape)
            {
                case TargetingShape.CardinalLine: return x == 0 || y == 0;
                case TargetingShape.DiagonalLine: return Math.Abs(x) == Math.Abs(y);
                case TargetingShape.EightDirectionLine: return x == 0 || y == 0 || Math.Abs(x) == Math.Abs(y);
                case TargetingShape.Fan:
                    for (int i = 0; i < CardinalSteps.Length; i++)
                        if (ContainsExpanding(x, y, CardinalSteps[i], range))
                            return true;
                    return false;
                default: return true;
            }
        }

        internal static bool IsDirectionSupported(TargetingShape shape, AttackDirection direction)
        {
            bool cardinal = direction == AttackDirection.Up || direction == AttackDirection.Right ||
                direction == AttackDirection.Down || direction == AttackDirection.Left;
            bool diagonal = direction == AttackDirection.UpRight || direction == AttackDirection.DownRight ||
                direction == AttackDirection.DownLeft || direction == AttackDirection.UpLeft;
            return shape == TargetingShape.DiagonalLine ? diagonal : cardinal;
        }

        internal static CellCoord GetDirectionStep(AttackDirection direction)
        {
            switch (direction)
            {
                case AttackDirection.Up: return CellCoord.Up;
                case AttackDirection.UpRight: return new CellCoord(1, 1);
                case AttackDirection.Right: return CellCoord.Right;
                case AttackDirection.DownRight: return new CellCoord(1, -1);
                case AttackDirection.Down: return CellCoord.Down;
                case AttackDirection.DownLeft: return new CellCoord(-1, -1);
                case AttackDirection.Left: return CellCoord.Left;
                case AttackDirection.UpLeft: return new CellCoord(-1, 1);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        internal static CellCoord[] Translate(CellCoord[] offsets, CellCoord origin)
        {
            var cells = new CellCoord[offsets.Length];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new CellCoord(checked(origin.X + offsets[i].X), checked(origin.Y + offsets[i].Y));
            return cells;
        }

        private static int RectangleLateralStart(int width)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            return -(width / 2);
        }

        internal static int MaximumBaseDistance(TargetingMethodSpec method, int range) =>
            method.Shape == TargetingShape.ForwardRectangle && range > 0
                ? Math.Max(range, -RectangleLateralStart(method.Width))
                : range;

        private static bool ContainsRectangle(long x, long y, CellCoord forward, int range, int first, int last)
        {
            long depth = x * forward.X + y * forward.Y;
            long lateral = x * forward.Y - y * forward.X;
            return depth >= 1 && depth <= range && lateral >= first && lateral <= last;
        }

        internal static IEnumerable<CellCoord> RectangleRangeSteps(int range, int width, TargetingChoiceAngle angle, Owner owner)
        {
            ValidateRange(range);
            var legalDirections = CardinalSteps.Where(direction => TargetingAngleCalculator.Contains(angle, owner, CellCoord.Zero, direction))
                .ToArray();
            foreach (var cell in RectangleMaskSteps(range, width, legalDirections))
                if (cell.HasValue && TargetingAngleCalculator.Contains(angle, owner, CellCoord.Zero, cell.Value))
                    yield return cell.Value;
        }

        private static IEnumerable<CellCoord?> RectangleMaskSteps(int range, int width, CellCoord[] directions)
        {
            int first = RectangleLateralStart(width);
            int last = first + width - 1;
            long cellsPerDirection = (long)range * width;
            // 마주 보는 두 범위는 겹치지 않습니다. 인접 방향과의 교차 넓이를 한 번씩 뺍니다.
            long overlap = (long)Math.Min(range, -first) * Math.Min(range, last);
            int intersections = 0;
            for (int i = 0; i < directions.Length; i++)
                for (int j = i + 1; j < directions.Length; j++)
                    if (directions[i].X != directions[j].X && directions[i].Y != directions[j].Y)
                        intersections++;
            // 방향별 셀 수부터 검사해 합계의 곱셈이 long 범위를 넘지 않게 합니다.
            CheckedCellCount(cellsPerDirection);
            CheckedCellCount(cellsPerDirection * directions.Length - overlap * intersections);
            if (range == 0) yield break;

            // 방향별 직사각형을 행·열 순서로 병합합니다. 겹치는 교차부는 순서상 연속으로 만나므로 한 번만 반환합니다.
            var starts = new CellCoord[directions.Length];
            var ends = new CellCoord[directions.Length];
            var positions = new CellCoord[directions.Length];
            var active = new bool[directions.Length];
            for (int i = 0; i < directions.Length; i++)
            {
                var step = directions[i];
                var right = new CellCoord(step.Y, -step.X);
                var a = new CellCoord(step.X + right.X * first, step.Y + right.Y * first);
                var b = new CellCoord(step.X * range + right.X * last, step.Y * range + right.Y * last);
                starts[i] = new CellCoord(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                ends[i] = new CellCoord(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
                positions[i] = starts[i];
                active[i] = true;
            }

            CellCoord? previous = null;
            while (true)
            {
                int next = -1;
                for (int i = 0; i < positions.Length; i++)
                    if (active[i] && (next < 0 || CompareCells(positions[i], positions[next]) < 0))
                        next = i;
                if (next < 0) yield break;
                var cell = positions[next];
                if (cell.X < ends[next].X) positions[next] = new CellCoord(cell.X + 1, cell.Y);
                else if (cell.Y < ends[next].Y) positions[next] = new CellCoord(starts[next].X, cell.Y + 1);
                else active[next] = false;
                yield return previous.HasValue && previous.Value.Equals(cell) ? null : cell;
                previous = cell;
            }
        }

        private static CellCoord[] CreateFilledMask(TargetingShape shape, int range, CellCoord? forward, int count)
        {
            var cells = new CellCoord[count];
            int index = 0;
            foreach (var cell in FilledMaskSteps(shape, range, forward))
                if (cell.HasValue) cells[index++] = cell.Value;
            return cells;
        }

        private static IEnumerable<CellCoord?> FilledMaskSteps(TargetingShape shape, int range, CellCoord? forward)
        {
            for (long y = -(long)range; y <= range; y++)
                for (long x = -(long)range; x <= range; x++)
                {
                    bool include;
                    if (forward.HasValue == false)
                        include = ContainsBaseMask(shape, range, x, y);
                    else if (shape == TargetingShape.Fan)
                        include = ContainsExpanding(x, y, forward.Value, range);
                    else
                        include = x * forward.Value.X + y * forward.Value.Y >= 1L;
                    yield return include ? new CellCoord((int)x, (int)y) : (CellCoord?)null;
                }
        }

        private static CellCoord[] MergeRays(CellCoord[] steps, int range)
        {
            int count = CheckedCellCount(range, steps.Length);
            if (count == 0) return Array.Empty<CellCoord>();
            var cells = new CellCoord[count];
            int index = 0;
            foreach (var cell in RayMaskSteps(steps, range)) cells[index++] = cell;
            return cells;
        }

        private static IEnumerable<CellCoord> RayMaskSteps(CellCoord[] steps, int range)
        {
            if (range == 0) yield break;
            var positions = new long[steps.Length];
            var directions = new int[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                bool reversed = CompareCells(steps[i], CellCoord.Zero) < 0;
                positions[i] = reversed ? range : 1;
                directions[i] = reversed ? -1 : 1;
            }
            // 최대 여덟 개의 정렬된 광선을 병합하므로 광선 길이에 선형이다.
            while (true)
            {
                int next = -1;
                CellCoord cell = default;
                for (int ray = 0; ray < steps.Length; ray++)
                {
                    long depth = positions[ray];
                    if (depth < 1 || depth > range) continue;
                    var candidate = new CellCoord((int)(steps[ray].X * depth), (int)(steps[ray].Y * depth));
                    if (next >= 0 && CompareCells(candidate, cell) >= 0) continue;
                    next = ray;
                    cell = candidate;
                }
                if (next < 0) yield break;
                positions[next] += directions[next];
                yield return cell;
            }
        }

        private static int CompareCells(CellCoord left, CellCoord right)
        {
            int y = left.Y.CompareTo(right.Y);
            return y == 0 ? left.X.CompareTo(right.X) : y;
        }

        private static int CheckedCellCount(long count, int multiplier = 1)
        {
            if (count > int.MaxValue / multiplier)
                throw new ArgumentOutOfRangeException("range", "결과 셀 배열이 너무 큽니다.");
            return (int)(count * multiplier);
        }

        private static void ValidateShape(TargetingShape shape)
        {
            if (shape == TargetingShape.None || Enum.IsDefined(typeof(TargetingShape), shape) == false)
                throw new ArgumentOutOfRangeException(nameof(shape));
        }

        public static CellCoord[] CreateSquare(int range)
        {
            ValidateRange(range);
            return CreateFilledMask(TargetingShape.RadialSquare, range, null, CheckedSquareCount(range));
        }

        private static int CheckedSquareCount(int range)
        {
            long sideLength = 2L * range + 1L;
            if (sideLength > (int.MaxValue + 1L) / sideLength)
                throw new ArgumentOutOfRangeException(nameof(range), "결과 셀 배열이 너무 큽니다.");

            return (int)(sideLength * sideLength - 1L);
        }

        public static CellCoord[] CreateRay(CellCoord directionStep, int range)
        {
            ValidateRange(range);
            ValidateDirectionStep(directionStep);

            CellCoord[] cells = new CellCoord[range];

            for (long depth = 1L; depth <= range; depth++)
            {
                cells[(int)(depth - 1L)] = new CellCoord(
                    checked((int)(directionStep.X * depth)),
                    checked((int)(directionStep.Y * depth)));
            }

            return cells;
        }

        public static CellCoord[] CreateExpanding(CellCoord forwardStep, int range)
        {
            ValidateRange(range);
            ValidateCardinalStep(forwardStep, nameof(forwardStep));

            long cellCount = (long)range * range;
            if (cellCount > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(range), "결과 셀 배열이 너무 큽니다.");

            CellCoord[] cells = new CellCoord[(int)cellCount];
            CellCoord rightStep = new CellCoord(forwardStep.Y, -forwardStep.X);
            int index = 0;

            for (long depth = 1L; depth <= range; depth++)
            {
                long halfWidth = depth - 1L;

                for (long lateral = -halfWidth; lateral <= halfWidth; lateral++)
                {
                    cells[index++] = new CellCoord(
                        checked((int)(forwardStep.X * depth + rightStep.X * lateral)),
                        checked((int)(forwardStep.Y * depth + rightStep.Y * lateral)));
                }
            }

            return cells;
        }

        internal static bool ContainsSquare(long localX, long localY, int range)
        {
            ValidateRange(range);

            return (localX != 0L || localY != 0L) &&
                   localX >= -(long)range && localX <= range &&
                   localY >= -(long)range && localY <= range;
        }

        internal static bool ContainsRay(
            long localX,
            long localY,
            CellCoord directionStep,
            int range)
        {
            ValidateRange(range);
            ValidateDirectionStep(directionStep);

            long depth;
            if (directionStep.X != 0)
            {
                depth = localX * directionStep.X;
                if (localY != directionStep.Y * depth)
                    return false;
            }
            else
            {
                depth = localY * directionStep.Y;
                if (localX != 0L)
                    return false;
            }

            return depth >= 1L && depth <= range;
        }

        internal static bool ContainsExpanding(
            long localX,
            long localY,
            CellCoord forwardStep,
            int range)
        {
            ValidateRange(range);
            ValidateCardinalStep(forwardStep, nameof(forwardStep));

            CellCoord rightStep = new CellCoord(forwardStep.Y, -forwardStep.X);
            long depth = localX * forwardStep.X + localY * forwardStep.Y;
            if (depth < 1L || depth > range)
                return false;

            long lateral = localX * rightStep.X + localY * rightStep.Y;
            long halfWidth = depth - 1L;
            return lateral >= -halfWidth && lateral <= halfWidth;
        }

        private static void ValidateRange(int range)
        {
            if (range < 0)
                throw new ArgumentOutOfRangeException(nameof(range));
        }

        private static void ValidateDirectionStep(CellCoord directionStep)
        {
            if (directionStep.X < -1 || directionStep.X > 1 ||
                directionStep.Y < -1 || directionStep.Y > 1 ||
                (directionStep.X == 0 && directionStep.Y == 0))
            {
                throw new ArgumentOutOfRangeException(nameof(directionStep));
            }
        }

        private static void ValidateCardinalStep(CellCoord step, string parameterName)
        {
            bool isCardinalDirection =
                ((step.X == -1 || step.X == 1) && step.Y == 0) ||
                (step.X == 0 && (step.Y == -1 || step.Y == 1));

            if (isCardinalDirection == false)
            {
                throw new ArgumentException(
                    "방향은 상하좌우 중 한 칸 방향이어야 합니다.",
                    parameterName);
            }
        }
    }
}
