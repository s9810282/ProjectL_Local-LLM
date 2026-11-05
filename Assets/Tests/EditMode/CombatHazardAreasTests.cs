using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    /// <summary>환경 위험 피해 칸 공개 입구(호버 공격 범위 설계 §8 HZ2, 위험 범위 계획 T3)를 확인합니다.</summary>
    public sealed class CombatHazardAreasTests
    {
        private static readonly CellCoord Center = new(2, 2);

        [Test]
        public void MineCells_IncludeCenter_ClipBoardEdges()
        {
            Assert.That(Mine(Center, 5, 5), Has.Count.EqualTo(9));
            Assert.That(Mine(Center, 5, 5), Has.Member(Center));
            Assert.That(Mine(CellCoord.Zero, 5, 5), Is.EquivalentTo(new[] {
                CellCoord.Zero, CellCoord.Right, CellCoord.Up, new CellCoord(1, 1) }));
            Assert.That(Mine(CellCoord.Zero, 1, 1), Is.EqualTo(new[] { CellCoord.Zero }));
        }

        [Test]
        public void PowderBarrelCells_ExcludeCenter_ClipBoardEdges()
        {
            Assert.That(Barrel(Center, 5, 5), Has.Count.EqualTo(8));
            Assert.That(Barrel(Center, 5, 5), Has.No.Member(Center));
            Assert.That(Barrel(CellCoord.Zero, 5, 5), Is.EquivalentTo(new[] {
                CellCoord.Right, CellCoord.Up, new CellCoord(1, 1) }));
            Assert.That(Barrel(CellCoord.Zero, 1, 1), Is.Empty);
        }

        [Test]
        public void Cells_AppendWithoutClearing()
        {
            var cells = new List<CellCoord> { new(9, 9) };
            CombatHazardAreas.MineCells(CellCoord.Zero, 5, 5, cells);
            CombatHazardAreas.PowderBarrelCells(new CellCoord(4, 4), 5, 5, cells);

            Assert.That(cells[0], Is.EqualTo(new CellCoord(9, 9)), "미리 든 칸을 지우지 않는다.");
            Assert.That(cells, Has.Count.EqualTo(1 + 4 + 3));
        }

        [Test]
        public void InvalidBoardOriginOrList_Rejects()
        {
            var cells = new List<CellCoord>();
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatHazardAreas.MineCells(Center, 0, 5, cells));
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatHazardAreas.MineCells(Center, 5, 0, cells));
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatHazardAreas.PowderBarrelCells(new CellCoord(5, 0), 5, 5, cells));
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatHazardAreas.PowderBarrelCells(new CellCoord(0, -1), 5, 5, cells));
            Assert.Throws<ArgumentNullException>(() => CombatHazardAreas.MineCells(Center, 5, 5, null));
            Assert.Throws<ArgumentNullException>(() => CombatHazardAreas.PowderBarrelCells(Center, 5, 5, null));
            Assert.That(cells, Is.Empty);
        }

        [Test]
        public void RuleFunctions_UseSameCellsInSameOrder()
        {
            foreach (CellCoord origin in new[] { Center, CellCoord.Zero, new CellCoord(4, 1) })
            {
                Assert.That(MineMovementTileRule.Cells(origin, 5, 5), Is.EqualTo(Mine(origin, 5, 5)));
                Assert.That(ObstacleReactionCalculator.BarrelCells(origin, 5, 5), Is.EqualTo(Barrel(origin, 5, 5)));
            }
        }

        private static List<CellCoord> Mine(CellCoord origin, int width, int height)
        {
            var cells = new List<CellCoord>();
            CombatHazardAreas.MineCells(origin, width, height, cells);
            return cells;
        }

        private static List<CellCoord> Barrel(CellCoord origin, int width, int height)
        {
            var cells = new List<CellCoord>();
            CombatHazardAreas.PowderBarrelCells(origin, width, height, cells);
            return cells;
        }
    }
}
