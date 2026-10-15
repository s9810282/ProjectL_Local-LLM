using System;
using System.IO;
using NPOI.XSSF.UserModel;
using NUnit.Framework;

namespace ProjectL.Tool.Editor.Tests
{
    public sealed class CombatObstacleCatalogTests
    {
        private string directory;
        private string WorkbookPath => Path.Combine(directory, "obstacles.xlsx");

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ProjectL-ObstacleTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        private static CombatLayoutDocument CreateDocument()
        {
            var document = CombatLayoutDocument.Create();
            var row = new CombatReferenceRow();
            document.obstacles.rows.Add(row);
            document.obstacles.Set(row, "Obstacle_Key", "501");
            document.obstacles.Set(row, "Obstacle_NameKOR", "돌벽");
            document.obstacles.Set(row, "Obstacle_HealthValue", "120");
            document.obstacles.Set(row, "Obstacle_HitEffect", "17");
            document.obstacles.Set(row, "Obstacle_DestroyEffect", "18");
            document.obstacles.Set(row, "Obstacle_EffectApplicationPoint", "2");
            document.obstacles.Set(row, "Obstacle_Description", "9001");
            return document;
        }

        [Test]
        public void Placement_UsesObstacleDtWithoutOverwritingTerrainOrElevation()
        {
            var document = CreateDocument();
            document.Cell(1, 2).terrainKey = 45;
            document.Cell(1, 2).elevation = 7;
            document.PaintObstacle(501, 1, 2);
            Assert.AreEqual("Blocked", document.Cell(1, 2).type);
            Assert.AreEqual(501, document.Cell(1, 2).obstacleKey);
            Assert.AreEqual(45, document.Cell(1, 2).terrainKey);
            Assert.AreEqual(7, document.Cell(1, 2).elevation);
            document.Validate();
            document.ValidateObstacleReferences();
        }

        [Test]
        public void Placement_RejectsMissingDtOccupiedAndOutOfBoundsWithoutChangingCell()
        {
            var document = CreateDocument();
            document.units.Add(new CombatLayoutUnit { key = 1, characterKey = 101, x = 0, y = 0, owner = "Player" });
            document.nextUnitKey = 2;
            Assert.Throws<InvalidOperationException>(() => document.PaintObstacle(999, 1, 1));
            Assert.Throws<InvalidOperationException>(() => document.PaintObstacle(0, 1, 1));
            Assert.Throws<InvalidOperationException>(() => document.PaintObstacle(501, 0, 0));
            Assert.Throws<InvalidOperationException>(() => document.PaintObstacle(501, -1, 0));
            Assert.AreEqual("Floor", document.Cell(1, 1).type);
            Assert.AreEqual(0, document.Cell(1, 1).obstacleKey);
            Assert.AreEqual("Floor", document.Cell(0, 0).type);
        }

        [Test]
        public void Validation_RejectsNegativeOrNonWallObstacleKeys()
        {
            var document = CreateDocument();
            document.Cell(0, 0).obstacleKey = -1;
            Assert.Throws<InvalidOperationException>(() => document.Validate());
            document.Cell(0, 0).obstacleKey = 501;
            Assert.Throws<InvalidOperationException>(() => document.Validate());
            document.Cell(0, 0).type = "Blocked";
            document.Validate();
        }

        [Test]
        public void DtRefresh_RejectsMissingKeyForExportButPreservesPlacedWall()
        {
            var document = CreateDocument();
            document.PaintObstacle(501, 1, 1);
            document.obstacles = new CombatReferenceSheet();
            Assert.Throws<InvalidOperationException>(() => document.ValidateObstacleReferences());
            Assert.AreEqual(501, document.Cell(1, 1).obstacleKey);
            Assert.AreEqual("Blocked", document.Cell(1, 1).type);
        }

        [Test]
        public void RoundTrip_PreservesObstacleIdentityAndReferenceFieldsSeparatelyFromTerrain()
        {
            var document = CreateDocument();
            document.Cell(2, 3).terrainKey = 45;
            document.PaintObstacle(501, 2, 3);
            CombatLayoutWorkbook.Save(WorkbookPath, document);
            using (var stream = File.OpenRead(WorkbookPath))
            {
                var book = new XSSFWorkbook(stream);
                try
                {
                    Assert.AreEqual(2, book.GetSheet("CombatStageMaster").GetRow(1).GetCell(9).NumericCellValue);
                    Assert.AreEqual("CombatCell_ObstacleKey", book.GetSheet("CombatCellMaster").GetRow(0).GetCell(7).StringCellValue);
                }
                finally { book.Close(); }
            }
            var actual = CombatLayoutWorkbook.Load(WorkbookPath);
            Assert.AreEqual(501, actual.Cell(2, 3).obstacleKey);
            Assert.AreEqual(45, actual.Cell(2, 3).terrainKey);
            Assert.AreEqual("Blocked", actual.Cell(2, 3).type);
            var row = actual.obstacles.Find("Obstacle_Key", 501);
            Assert.NotNull(row);
            Assert.AreEqual("돌벽", actual.obstacles.Value(row, "Obstacle_NameKOR"));
            Assert.AreEqual(120, actual.obstacles.Number(row, "Obstacle_HealthValue"));
            Assert.AreEqual(18, actual.obstacles.Number(row, "Obstacle_DestroyEffect"));
        }

        [Test]
        public void Import_AcceptsLegacyWorkbookWithoutObstacleColumnOrSnapshot()
        {
            var document = CombatLayoutDocument.Create();
            document.Cell(1, 1).type = "Blocked";
            CombatLayoutWorkbook.Save(WorkbookPath, document);
            XSSFWorkbook book;
            using (var stream = File.OpenRead(WorkbookPath)) book = new XSSFWorkbook(stream);
            try
            {
                var sheet = book.GetSheet("CombatCellMaster");
                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row?.GetCell(7) != null) row.RemoveCell(row.GetCell(7));
                }
                using (var stream = File.Create(WorkbookPath)) book.Write(stream);
            }
            finally { book.Close(); }
            var actual = CombatLayoutWorkbook.Load(WorkbookPath);
            Assert.AreEqual("Blocked", actual.Cell(1, 1).type);
            Assert.AreEqual(0, actual.Cell(1, 1).obstacleKey);
            Assert.AreEqual(0, actual.obstacles.rows.Count);
        }

        [Test]
        public void Import_RejectsVersionTwoWithoutObstacleIdentityColumn()
        {
            var document = CreateDocument();
            document.PaintObstacle(501, 1, 1);
            CombatLayoutWorkbook.Save(WorkbookPath, document);
            XSSFWorkbook book;
            using (var stream = File.OpenRead(WorkbookPath)) book = new XSSFWorkbook(stream);
            try
            {
                var header = book.GetSheet("CombatCellMaster").GetRow(0);
                header.RemoveCell(header.GetCell(7));
                using (var stream = File.Create(WorkbookPath)) book.Write(stream);
            }
            finally { book.Close(); }
            Assert.Throws<InvalidDataException>(() => CombatLayoutWorkbook.Load(WorkbookPath));
        }

        [Test]
        public void ReferenceImport_ReadsObstacleMasterAndRejectsFractionalNumericFields()
        {
            var book = new XSSFWorkbook();
            try
            {
                var sheet = book.CreateSheet("ObstacleMaster");
                var header = sheet.CreateRow(0);
                header.CreateCell(0).SetCellValue("Obstacle_Key");
                header.CreateCell(1).SetCellValue("Obstacle_NameKOR");
                header.CreateCell(2).SetCellValue("Obstacle_HealthValue");
                sheet.CreateRow(1).CreateCell(0).SetCellValue("# 설명");
                var row = sheet.CreateRow(2);
                row.CreateCell(0).SetCellValue(501);
                row.CreateCell(1).SetCellValue("돌벽");
                row.CreateCell(2).SetCellValue(120);
                using (var stream = File.Create(WorkbookPath)) book.Write(stream);
                var reference = CombatLayoutWorkbook.LoadReferences(WorkbookPath, "ObstacleMaster", "Obstacle_Key");
                Assert.AreEqual(1, reference.rows.Count);
                Assert.AreEqual(120, reference.Number(reference.Find("Obstacle_Key", 501), "Obstacle_HealthValue"));
                row.GetCell(2).SetCellValue(120.5);
                using (var stream = File.Create(WorkbookPath)) book.Write(stream);
                Assert.Throws<InvalidDataException>(() => CombatLayoutWorkbook.LoadReferences(WorkbookPath, "ObstacleMaster", "Obstacle_Key"));
            }
            finally { book.Close(); }
        }
    }
}
