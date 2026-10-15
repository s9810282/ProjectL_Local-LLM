using System;
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

        public static CellCoord[] CreateBaseMask(TargetingShape shape, int range)
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
                default: return CreateSquare(range);
            }
        }

        public static CellCoord[] CreateDirectionMask(TargetingShape shape, int range, AttackDirection direction)
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
                default:
                    return CreateFilledMask(shape, range, forward, CheckedCellCount((long)range * (2L * range + 1L)));
            }
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

        internal static bool ContainsBaseMask(TargetingShape shape, int range, long x, long y)
        {
            ValidateRange(range);
            ValidateShape(shape);
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

        private static CellCoord[] CreateFilledMask(TargetingShape shape, int range, CellCoord? forward, int count)
        {
            var cells = new CellCoord[count];
            int index = 0;
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
                    if (include)
                        cells[index++] = new CellCoord((int)x, (int)y);
                }
            return cells;
        }

        private static CellCoord[] MergeRays(CellCoord[] steps, int range)
        {
            int count = CheckedCellCount(range, steps.Length);
            if (count == 0)
                return Array.Empty<CellCoord>();
            var rays = new CellCoord[steps.Length][];
            var positions = new int[steps.Length];
            for (int i = 0; i < rays.Length; i++)
            {
                rays[i] = CreateRay(steps[i], range);
                if (CompareCells(rays[i][0], rays[i][range - 1]) > 0)
                    Array.Reverse(rays[i]);
            }
            var cells = new CellCoord[count];
            // 최대 여덟 개의 정렬된 광선을 병합하므로 광선 길이에 선형이다.
            for (int output = 0; output < cells.Length; output++)
            {
                int next = -1;
                for (int ray = 0; ray < rays.Length; ray++)
                    if (positions[ray] < range &&
                        (next < 0 || CompareCells(rays[ray][positions[ray]], rays[next][positions[next]]) < 0))
                        next = ray;
                cells[output] = rays[next][positions[next]++];
            }
            return cells;
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

            long sideLength = 2L * range + 1L;
            if (sideLength > (int.MaxValue + 1L) / sideLength)
                throw new ArgumentOutOfRangeException(nameof(range), "결과 셀 배열이 너무 큽니다.");

            long cellCount = sideLength * sideLength - 1L;
            CellCoord[] cells = new CellCoord[(int)cellCount];
            int index = 0;

            for (long y = -(long)range; y <= range; y++)
            {
                for (long x = -(long)range; x <= range; x++)
                {
                    if (ContainsSquare(x, y, range) == false)
                        continue;

                    cells[index++] = new CellCoord((int)x, (int)y);
                }
            }

            return cells;
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
