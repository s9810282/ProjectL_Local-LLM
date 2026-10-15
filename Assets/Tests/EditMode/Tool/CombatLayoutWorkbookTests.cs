using System;
using System.IO;
using System.Linq;
using NPOI.XSSF.UserModel;
using NUnit.Framework;

namespace ProjectL.Tool.Editor.Tests
{
    public sealed class CombatLayoutWorkbookTests
    {
        private string directory;
        private string PathFor(string name) => Path.Combine(directory, name + ".xlsx");

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ProjectL-CombatLayout-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test]
        public void RoundTrip_PreservesCoordinatesHeightsReferencesAndLargeIntegers()
        {
            var source = CombatLayoutDocument.Create();
            source.stage.seed = int.MinValue;
            source.stage.night = 9007199254740991L;
            source.Cell(2, 3).elevation = 7;
            source.Cell(1, 1).type = "Rail";
            source.Cell(1, 1).terrainKey = 45;
            source.units.Add(new CombatLayoutUnit { key = int.MaxValue, characterKey = 101, x = 2, y = 3, owner = "Player" });
            source.nextUnitKey = (long)int.MaxValue + 1;
            var reference = new CombatReferenceRow();
            source.characters.rows.Add(reference);
            source.characters.Set(reference, "Character_Key", "101");
            source.characters.Set(reference, "Character_NameKOR", "기사");
            source.characters.Set(reference, "Character_HealthValue", "");
            source.characters.Set(reference, "EditorCharacter_Color", "#56c5b8");
            CombatLayoutWorkbook.Save(PathFor("roundtrip"), source);

            var actual = CombatLayoutWorkbook.Load(PathFor("roundtrip"));
            Assert.AreEqual(source.stage.night, actual.stage.night);
            Assert.AreEqual(int.MinValue, actual.stage.seed);
            Assert.AreEqual((long)int.MaxValue + 1, actual.nextUnitKey);
            Assert.AreEqual(7, actual.Cell(2, 3).elevation);
            Assert.AreEqual("Rail", actual.Cell(1, 1).type);
            Assert.AreEqual(45, actual.Cell(1, 1).terrainKey);
            Assert.AreEqual("Player", actual.units.Single().owner);
            Assert.AreEqual(2, actual.units.Single().x);
            Assert.AreEqual(3, actual.units.Single().y);
            Assert.AreEqual("기사", actual.characters.Value(actual.characters.rows.Single(), "Character_NameKOR"));
            Assert.AreEqual("", actual.characters.Value(actual.characters.rows.Single(), "Character_HealthValue"));
            Assert.AreEqual("#56c5b8", actual.characters.Value(actual.characters.rows.Single(), "EditorCharacter_Color"));
        }

        [Test]
        public void Export_UsesBrowserContractAndUnitCellElevation()
        {
            var source = CombatLayoutDocument.Create();
            source.Cell(3, 2).elevation = 4;
            source.units.Add(new CombatLayoutUnit { key = 1, characterKey = 11, x = 3, y = 2 });
            source.nextUnitKey = 2;
            CombatLayoutWorkbook.Save(PathFor("contract"), source);
            using (var stream = File.OpenRead(PathFor("contract")))
            {
                var book = new XSSFWorkbook(stream);
                try
                {
                    Assert.AreEqual("CombatStage_SchemaVersion", book.GetSheet("CombatStageMaster").GetRow(0).GetCell(9).StringCellValue);
                    Assert.AreEqual(1, book.GetSheet("CombatStageMaster").GetRow(1).GetCell(9).NumericCellValue);
                    Assert.AreEqual(4, book.GetSheet("CombatUnitMaster").GetRow(1).GetCell(6).NumericCellValue);
                    Assert.AreEqual(20, book.GetSheet("CombatCellMaster").GetRow(20).GetCell(0).NumericCellValue);
                    Assert.AreEqual(80, book.GetSheet("CombatCellMaster").LastRowNum);
                }
                finally { book.Close(); }
            }
        }

        [Test]
        public void InvalidExport_PreservesExistingFile()
        {
            string path = PathFor("existing");
            var source = CombatLayoutDocument.Create();
            CombatLayoutWorkbook.Save(path, source);
            byte[] previous = File.ReadAllBytes(path);
            source.units.Add(new CombatLayoutUnit { key = 1, characterKey = 5, x = 0, y = 0 });
            source.units.Add(new CombatLayoutUnit { key = 2, characterKey = 5, x = 0, y = 0 });
            source.nextUnitKey = 3;
            Assert.Throws<InvalidOperationException>(() => CombatLayoutWorkbook.Save(path, source));
            CollectionAssert.AreEqual(previous, File.ReadAllBytes(path));
        }

        [Test]
        public void Resize_RejectsClippingAndPreservesDocument()
        {
            var source = CombatLayoutDocument.Create();
            source.Cell(7, 8).elevation = 1;
            var settings = source.stage.Copy(); settings.width = 7;
            Assert.Throws<InvalidOperationException>(() => source.Resize(settings));
            Assert.AreEqual(8, source.stage.width);
            Assert.AreEqual(1, source.Cell(7, 8).elevation);
            Assert.AreEqual(80, source.cells.Count);
        }

        [Test]
        public void Import_RejectsMismatchedUnitElevation()
        {
            string path = PathFor("bad-height");
            var source = CombatLayoutDocument.Create();
            source.units.Add(new CombatLayoutUnit { key = 1, characterKey = 5, x = 0, y = 0 });
            source.nextUnitKey = 2;
            CombatLayoutWorkbook.Save(path, source);
            Rewrite(path, book => book.GetSheet("CombatUnitMaster").GetRow(1).GetCell(6).SetCellValue(12));
            Assert.Throws<InvalidDataException>(() => CombatLayoutWorkbook.Load(path));
        }

        [Test]
        public void Import_RejectsFractionalCoordinatesDuplicateHeadersAndUnknownVersion()
        {
            var source = CombatLayoutDocument.Create();
            string fractional = PathFor("fractional");
            CombatLayoutWorkbook.Save(fractional, source);
            Rewrite(fractional, book => book.GetSheet("CombatCellMaster").GetRow(1).GetCell(2).SetCellValue(.5));
            Assert.Throws<InvalidDataException>(() => CombatLayoutWorkbook.Load(fractional));
            string duplicate = PathFor("duplicate");
            CombatLayoutWorkbook.Save(duplicate, source);
            Rewrite(duplicate, book => book.GetSheet("CombatStageMaster").GetRow(0).GetCell(1).SetCellValue("CombatStage_Key"));
            Assert.Throws<InvalidDataException>(() => CombatLayoutWorkbook.Load(duplicate));
            string version = PathFor("version");
            CombatLayoutWorkbook.Save(version, source);
            Rewrite(version, book => book.GetSheet("CombatStageMaster").GetRow(1).GetCell(9).SetCellValue(99));
            Assert.Throws<InvalidDataException>(() => CombatLayoutWorkbook.Load(version));
        }

        [Test]
        public void Export_CanReplacePreviouslySavedWorkbook()
        {
            string path = PathFor("replace");
            var source = CombatLayoutDocument.Create();
            CombatLayoutWorkbook.Save(path, source);
            source.stage.name = "수정한 스테이지";
            CombatLayoutWorkbook.Save(path, source);
            Assert.AreEqual(source.stage.name, CombatLayoutWorkbook.Load(path).stage.name);
            Assert.IsEmpty(Directory.GetFiles(directory, "*.tmp"));
        }

        private static void Rewrite(string path, Action<XSSFWorkbook> action)
        {
            XSSFWorkbook book;
            using (var input = File.OpenRead(path)) book = new XSSFWorkbook(input);
            try { action(book); using (var output = File.Create(path)) book.Write(output); }
            finally { book.Close(); }
        }
    }
}
