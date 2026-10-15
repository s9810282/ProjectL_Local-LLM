using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ProjectL.Tool.Editor
{
    internal static class CombatLayoutWorkbook
    {
        internal static readonly string[] StageColumns = { "CombatStage_Key", "CombatStage_Name", "CombatStage_MapWidth", "CombatStage_MapHeight", "CombatStage_ReserveCapacity", "CombatStage_PlayerDeploymentDepth", "CombatStage_EnemyDeploymentDepth", "CombatStage_Seed", "CombatStage_NightLength", "CombatStage_SchemaVersion" };
        internal static readonly string[] CellColumns = { "CombatCell_Key", "CombatCell_StageKey", "CombatCell_X", "CombatCell_Y", "CombatCell_Elevation", "CombatCell_Type", "CombatCell_TerrainKey", "CombatCell_ObstacleKey" };
        internal static readonly string[] UnitColumns = { "CombatUnit_Key", "CombatUnit_StageKey", "CombatUnit_CharacterKey", "CombatUnit_Owner", "CombatUnit_X", "CombatUnit_Y", "CombatUnit_Elevation" };

        public static void Save(string path, CombatLayoutDocument document)
        {
            document.Validate();
            var book = new XSSFWorkbook();
            try
            {
                var s = document.stage;
                // Legacy browser editors reject version 2 instead of silently discarding obstacle identity.
                int version = document.cells.Any(cell => cell.obstacleKey > 0) ? 2 : 1;
                WriteSheet(book, "CombatStageMaster", StageColumns, new[] { new object[] { s.key, s.name, s.width, s.height, s.reserve, s.playerDepth, s.enemyDepth, s.seed, s.night, version } });
                WriteSheet(book, "CombatCellMaster", CellColumns, document.cells.OrderBy(c => c.y).ThenBy(c => c.x)
                    .Select(c => new object[] { c.y * s.width + c.x + 1, s.key, c.x, c.y, c.elevation, c.type, c.terrainKey, c.obstacleKey }));
                WriteSheet(book, "CombatUnitMaster", UnitColumns, document.units.OrderBy(u => u.key)
                    .Select(u => new object[] { u.key, s.key, u.characterKey, u.owner, u.x, u.y, document.Cell(u.x, u.y).elevation }));
                WriteReferences(book, "EditorCharacterMaster", document.characters);
                WriteReferences(book, "EditorMethodMaster", document.methods);
                WriteReferences(book, "EditorObstacleMaster", document.obstacles);
                // Finish serialization before touching the destination. Replace only after a successful write.
                string fullPath = Path.GetFullPath(path);
                string temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var stream = File.Create(temporary)) book.Write(stream);
                    PreserveNightLength(temporary, s.night);
                    if (File.Exists(fullPath)) File.Replace(temporary, fullPath, null);
                    else File.Move(temporary, fullPath);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            finally { book.Close(); }
        }

        public static CombatLayoutDocument Load(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var book = Open(path, stream);
                try
                {
                    var stages = Records(book.GetSheet("CombatStageMaster"), StageColumns);
                    if (stages.Count != 1) throw new InvalidDataException("배치 엑셀은 파일당 스테이지 1개여야 합니다.");
                    var row = stages[0];
                    int version = Int(row, "CombatStage_SchemaVersion");
                    if (version != 1 && version != 2) throw new InvalidDataException("SchemaVersion 1 또는 2만 지원합니다.");
                    var document = new CombatLayoutDocument
                    {
                        stage = new CombatLayoutStage
                        {
                            key = Int(row, StageColumns[0]), name = Text(row, StageColumns[1]),
                            width = Int(row, StageColumns[2]), height = Int(row, StageColumns[3]), reserve = Int(row, StageColumns[4]),
                            playerDepth = Int(row, StageColumns[5]), enemyDepth = Int(row, StageColumns[6]),
                            seed = Int(row, StageColumns[7]), night = Integer(row, StageColumns[8], 1, 9007199254740991L)
                        }
                    };
                    document.stage.Validate();
                    // The obstacle column is optional when opening older SchemaVersion 1 workbooks.
                    foreach (var c in Records(book.GetSheet("CombatCellMaster"), version == 1 ? CellColumns.Take(7) : CellColumns))
                    {
                        var cell = new CombatLayoutCell { x = Int(c, CellColumns[2]), y = Int(c, CellColumns[3]), elevation = Int(c, CellColumns[4]), type = Text(c, CellColumns[5]), terrainKey = Int(c, CellColumns[6]) };
                        if (c.TryGetValue(CellColumns[7], out var obstacleValue) && obstacleValue != null && Convert.ToString(obstacleValue, CultureInfo.InvariantCulture) != "")
                            cell.obstacleKey = (int)Integer(c, CellColumns[7], 0, int.MaxValue);
                        if (Int(c, CellColumns[1]) != document.stage.key || Integer(c, CellColumns[0], 1, int.MaxValue) != (long)cell.y * document.stage.width + cell.x + 1)
                            throw new InvalidDataException("셀의 Stage 키 또는 Cell 키가 잘못됐습니다.");
                        document.cells.Add(cell);
                    }
                    foreach (var u in Records(book.GetSheet("CombatUnitMaster"), UnitColumns))
                    {
                        var unit = new CombatLayoutUnit { key = Int(u, UnitColumns[0]), characterKey = Int(u, UnitColumns[2]), owner = Text(u, UnitColumns[3]), x = Int(u, UnitColumns[4]), y = Int(u, UnitColumns[5]) };
                        var cell = document.Cell(unit.x, unit.y);
                        if (Int(u, UnitColumns[1]) != document.stage.key || cell == null || Int(u, UnitColumns[6]) != cell.elevation)
                            throw new InvalidDataException("유닛의 Stage 키 또는 셀 높이가 맞지 않습니다.");
                        document.units.Add(unit);
                        document.nextUnitKey = Math.Max(document.nextUnitKey, (long)unit.key + 1);
                    }
                    if (book.GetSheet("EditorCharacterMaster") != null)
                        document.characters = ReadReferences(book.GetSheet("EditorCharacterMaster"), "Character_Key");
                    if (book.GetSheet("EditorMethodMaster") != null)
                        document.methods = ReadReferences(book.GetSheet("EditorMethodMaster"), "Method_Key");
                    if (book.GetSheet("EditorObstacleMaster") != null)
                        document.obstacles = ReadReferences(book.GetSheet("EditorObstacleMaster"), "Obstacle_Key");
                    document.Validate();
                    return document;
                }
                finally { book.Close(); }
            }
        }

        public static CombatReferenceSheet LoadReferences(string path, string sheetName, string keyColumn)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var book = Open(path, stream);
                try { return ReadReferences(book.GetSheet(sheetName), keyColumn); }
                finally { book.Close(); }
            }
        }

        private static IWorkbook Open(string path, Stream stream) =>
            string.Equals(Path.GetExtension(path), ".xls", StringComparison.OrdinalIgnoreCase)
                ? (IWorkbook)new HSSFWorkbook(stream) : new XSSFWorkbook(stream);

        private static void PreserveNightLength(string path, long night)
        {
            // The bundled NPOI version formats doubles with 15 digits. Keep the browser's
            // full safe-integer range by writing this one long field as exact numeric XML.
            using (var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Update))
            using (var stream = archive.GetEntry("xl/worksheets/sheet1.xml").Open())
            {
                var xml = new XmlDocument();
                xml.Load(stream);
                var namespaces = new XmlNamespaceManager(xml.NameTable);
                namespaces.AddNamespace("s", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                var value = xml.SelectSingleNode("//s:c[@r='I2']/s:v", namespaces);
                if (value == null) throw new InvalidDataException("엑셀 밤 길이 셀을 찾지 못했습니다.");
                value.InnerText = night.ToString(CultureInfo.InvariantCulture);
                stream.Position = 0; stream.SetLength(0); xml.Save(stream);
            }
        }

        private static object Value(ICell cell)
        {
            if (cell == null) return null;
            var type = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;
            switch (type)
            {
                case CellType.Numeric: return cell.NumericCellValue;
                case CellType.String: return cell.StringCellValue;
                case CellType.Boolean: return cell.BooleanCellValue;
                case CellType.Blank: return null;
                default: throw new InvalidDataException($"{cell.Sheet.SheetName} {cell.RowIndex + 1}행: 오류 셀은 사용할 수 없습니다.");
            }
        }

        private static List<Dictionary<string, object>> Records(ISheet sheet, IEnumerable<string> required)
        {
            if (sheet == null || sheet.GetRow(0) == null) throw new InvalidDataException("필요한 시트 또는 헤더가 없습니다.");
            var header = sheet.GetRow(0);
            var columns = new Dictionary<string, int>();
            for (int i = 0; i < header.LastCellNum; i++)
            {
                string name = Convert.ToString(Value(header.GetCell(i)), CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(name)) continue;
                if (columns.ContainsKey(name)) throw new InvalidDataException($"{sheet.SheetName}: {name} 헤더가 중복됐습니다.");
                columns.Add(name, i);
            }
            foreach (string name in required)
                if (!columns.ContainsKey(name)) throw new InvalidDataException($"{sheet.SheetName}: {name} 컬럼이 없습니다.");
            var records = new List<Dictionary<string, object>>();
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;
                if (Value(row.GetCell(0)) is string comment && comment.StartsWith("#", StringComparison.Ordinal)) continue;
                var record = columns.ToDictionary(pair => pair.Key, pair => Value(row.GetCell(pair.Value)));
                if (record.Values.Any(value => value != null && Convert.ToString(value, CultureInfo.InvariantCulture) != "")) records.Add(record);
            }
            return records;
        }

        private static long Integer(Dictionary<string, object> row, string column, long min, long max)
        {
            if (!row.TryGetValue(column, out var value) || value == null || value is bool ||
                !decimal.TryParse(value is double numeric ? numeric.ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number) ||
                decimal.Truncate(number) != number || number < min || number > max)
                throw new InvalidDataException($"{column}: {min}~{max} 범위의 정수가 필요합니다.");
            return (long)number;
        }
        private static int Int(Dictionary<string, object> row, string column) => (int)Integer(row, column, int.MinValue, int.MaxValue);
        private static string Text(Dictionary<string, object> row, string column) => row.TryGetValue(column, out var value) && value is string text
            ? text : throw new InvalidDataException($"{column}: 문자열이 필요합니다.");

        private static CombatReferenceSheet ReadReferences(ISheet sheet, string keyColumn)
        {
            var required = keyColumn == "Method_Key"
                ? new[] { "Method_Key", "Method_Type", "Method_ChoiceRange", "Method_Splash", "Method_SplashValue" }
                : new[] { keyColumn };
            var records = Records(sheet, required);
            var result = new CombatReferenceSheet();
            var header = sheet.GetRow(0);
            for (int i = 0; i < header.LastCellNum; i++)
            {
                string column = Convert.ToString(Value(header.GetCell(i)), CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(column)) result.columns.Add(column);
            }
            var keys = new HashSet<int>();
            foreach (var record in records)
            {
                int key = (int)Integer(record, keyColumn, 1, int.MaxValue);
                if (!keys.Add(key)) throw new InvalidDataException($"{sheet.SheetName}: 키 {key}가 중복됐습니다.");
                foreach (string column in result.columns)
                {
                    object value = record[column];
                    if (value == null || Convert.ToString(value, CultureInfo.InvariantCulture) == "") continue;
                    if (IsNumericReference(column)) Integer(record, column, int.MinValue, int.MaxValue);
                }
                result.rows.Add(new CombatReferenceRow { values = result.columns.Select(column => Convert.ToString(record[column], CultureInfo.InvariantCulture)).ToList() });
            }
            return result;
        }

        private static bool IsNumericReference(string column) => column.StartsWith("Method_", StringComparison.Ordinal) ||
            new[] { "Obstacle_Key", "Obstacle_HealthValue", "Obstacle_HitEffect", "Obstacle_DestroyEffect", "Obstacle_EffectApplicationPoint", "Obstacle_Description" }.Contains(column) ||
            new[] { "Character_Key", "Character_Level", "Character_RoleType", "Character_PositionType", "Character_HealthValue", "Character_DefenseValue", "Character_AttackValue", "Character_MoveMethod", "Character_MoveRange", "Character_AttackMethod", "Character_AttackRange" }.Contains(column);

        private static void WriteReferences(IWorkbook book, string name, CombatReferenceSheet reference)
        {
            if (reference == null || reference.columns.Count == 0) return;
            WriteSheet(book, name, reference.columns, reference.rows.Select(row => reference.columns.Select(column =>
            {
                string value = reference.Value(row, column);
                if (string.IsNullOrEmpty(value)) return null;
                return IsNumericReference(column) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) ? (object)number : value;
            }).ToArray()));
        }

        private static void WriteSheet(IWorkbook book, string name, IEnumerable<string> columns, IEnumerable<object[]> records)
        {
            var sheet = book.CreateSheet(name);
            var header = sheet.CreateRow(0);
            string[] names = columns.ToArray();
            for (int i = 0; i < names.Length; i++) { header.CreateCell(i).SetCellValue(names[i]); sheet.SetColumnWidth(i, 24 * 256); }
            sheet.CreateFreezePane(0, 1);
            int index = 1;
            foreach (var values in records)
            {
                var row = sheet.CreateRow(index++);
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == null) continue;
                    var cell = row.CreateCell(i);
                    if (values[i] is string text) cell.SetCellValue(text);
                    else cell.SetCellValue(Convert.ToDouble(values[i], CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
