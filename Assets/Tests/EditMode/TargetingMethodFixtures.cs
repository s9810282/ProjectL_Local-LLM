using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    internal static class TargetingMethodFixtures
    {
        // 공급된 슬라이드 4~21의 격자를 그대로 고정한다. 계산 코드로 기대값을 만들지 않는다.
        internal static readonly Example[] Examples =
        {
            new Example(4, TargetingShape.CardinalLine, TargetingChoiceMode.Single, 3, TargetingSplash.None, AttackDirection.None, "...S.../...T.../...S.../SSSCSSS/...S.../...S.../...S..."),
            new Example(5, TargetingShape.DiagonalLine, TargetingChoiceMode.Single, 3, TargetingSplash.None, AttackDirection.None, "S.....S/.S...S./..T.S../...C.../..S.S../.S...S./S.....S"),
            new Example(6, TargetingShape.EightDirectionLine, TargetingChoiceMode.Single, 3, TargetingSplash.None, AttackDirection.None, "S..S..S/.S.S.S./..SSS../SSSCSSS/..SST../.S.S.S./S..S..S"),
            new Example(7, TargetingShape.Fan, TargetingChoiceMode.Single, 3, TargetingSplash.None, AttackDirection.None, ".SSSSS./S.SSS.S/SS.S.SS/TSSCSSS/SS.S.SS/S.SSS.S/.SSSSS."),
            new Example(8, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 2, TargetingSplash.None, AttackDirection.None, "......./.SSSSS./.STSSS./.SSCSS./.SSSSS./.SSSSS./......."),
            new Example(9, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 2, TargetingSplash.Cross, AttackDirection.None, "..A..../.ATASS./.SASSS./.SSCSS./.SSSSS./.SSSSS./......."),
            new Example(10, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 2, TargetingSplash.Diagonal, AttackDirection.None, ".A.A.../.STSSS./.ASASS./.SSCSS./.SSSSS./.SSSSS./......."),
            new Example(11, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 2, TargetingSplash.Radial, AttackDirection.None, ".AAA.../.ATASS./.AAASS./.SSCSS./.SSSSS./.SSSSS./......."),
            new Example(12, TargetingShape.CardinalLine, TargetingChoiceMode.Direction, 3, TargetingSplash.None, AttackDirection.Up, "...T.../...T.../...T.../SSSCSSS/...S.../...S.../...S..."),
            new Example(13, TargetingShape.DiagonalLine, TargetingChoiceMode.Direction, 3, TargetingSplash.None, AttackDirection.UpLeft, "T.....S/.T...S./..T.S../...C.../..S.S../.S...S./S.....S"),
            new Example(14, TargetingShape.Fan, TargetingChoiceMode.Direction, 3, TargetingSplash.None, AttackDirection.Left, ".SSSSS./T.SSS.S/TT.S.SS/TTTCSSS/TT.S.SS/T.SSS.S/.SSSSS."),
            new Example(15, TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, 3, TargetingSplash.None, AttackDirection.Right, "S..S..T/.S.S.T./..SST../SSSCTTT/..SST../.S.S.T./S..S..T"),
            new Example(16, TargetingShape.RadialSquare, TargetingChoiceMode.Direction, 2, TargetingSplash.None, AttackDirection.Left, "......./.TTSSS./.TTSSS./.TTCSS./.TTSSS./.TTSSS./......."),
            new Example(17, TargetingShape.CardinalLine, TargetingChoiceMode.All, 3, TargetingSplash.None, AttackDirection.None, "...T.../...T.../...T.../TTTCTTT/...T.../...T.../...T..."),
            new Example(18, TargetingShape.DiagonalLine, TargetingChoiceMode.All, 3, TargetingSplash.None, AttackDirection.None, "T.....T/.T...T./..T.T../...C.../..T.T../.T...T./T.....T"),
            new Example(19, TargetingShape.EightDirectionLine, TargetingChoiceMode.All, 3, TargetingSplash.None, AttackDirection.None, "T..T..T/.T.T.T./..TTT../TTTCTTT/..TTT../.T.T.T./T..T..T"),
            new Example(20, TargetingShape.Fan, TargetingChoiceMode.All, 3, TargetingSplash.None, AttackDirection.None, ".TTTTT./T.TTT.T/TT.T.TT/TTTCTTT/TT.T.TT/T.TTT.T/.TTTTT."),
            new Example(21, TargetingShape.RadialSquare, TargetingChoiceMode.All, 2, TargetingSplash.None, AttackDirection.None, "......./.TTTTT./.TTTTT./.TTCTT./.TTTTT./.TTTTT./......."),
        };

        internal sealed class Example
        {
            internal int Slide { get; }
            internal TargetingMethodSpec Method { get; }
            internal int Range { get; }
            internal AttackDirection Direction { get; }
            internal string Grid { get; }

            internal Example(int slide, TargetingShape shape, TargetingChoiceMode choice,
                int range, TargetingSplash splash, AttackDirection direction, string grid)
            {
                Slide = slide;
                Method = new TargetingMethodSpec(shape, choice, splash, splash == TargetingSplash.None ? 0 : 50);
                Range = range;
                Direction = direction;
                Grid = grid;
            }

            internal CellCoord[] Cells(string symbols, CellCoord origin)
            {
                return Decode(Grid, symbols, origin);
            }

            internal CellCoord[] BaseCells(CellCoord origin)
            {
                // 스플래시 격자는 A가 S를 덮는다. 선택 범위는 별도의 원본 사각형 격자다.
                return Decode(Slide >= 9 && Slide <= 11 ? Examples[4].Grid : Grid, "ST", origin);
            }

            internal AttackAim Aim(CellCoord origin)
            {
                switch (Method.ChoiceMode)
                {
                    case TargetingChoiceMode.Single: return AttackAim.ForCell(Cells("T", origin)[0]);
                    case TargetingChoiceMode.Direction: return AttackAim.ForDirection(Direction);
                    default: return AttackAim.ForAll();
                }
            }

            public override string ToString() => "Slide" + Slide;
        }

        internal static CellCoord[] Decode(string grid, string symbols, CellCoord origin)
        {
            var result = new List<CellCoord>();
            string[] rows = grid.Split('/');
            if (rows.Length != 7 || rows.Any(row => row.Length != 7))
                throw new ArgumentException("고정 격자는 7×7이어야 합니다.");
            for (int row = 0; row < 7; row++)
                for (int column = 0; column < 7; column++)
                    if (symbols.IndexOf(rows[row][column]) >= 0)
                        result.Add(new CellCoord(origin.X + column - 3, origin.Y + 3 - row));
            return Order(result);
        }

        internal static CellCoord[] Order(IEnumerable<CellCoord> cells)
            => cells.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
    }
}
