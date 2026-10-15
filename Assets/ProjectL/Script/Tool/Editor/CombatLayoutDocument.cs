using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectL.Tool.Editor
{
    // Authoring data only. The worksheet contract is shared with the browser tool.
    [Serializable]
    internal sealed class CombatLayoutDocument
    {
        public CombatLayoutStage stage = new CombatLayoutStage();
        public List<CombatLayoutCell> cells = new List<CombatLayoutCell>();
        public List<CombatLayoutUnit> units = new List<CombatLayoutUnit>();
        public long nextUnitKey = 1;
        public CombatReferenceSheet characters = new CombatReferenceSheet();
        public CombatReferenceSheet methods = new CombatReferenceSheet();
        public CombatReferenceSheet obstacles = new CombatReferenceSheet();

        public static CombatLayoutDocument Create()
        {
            var document = new CombatLayoutDocument();
            document.Resize(document.stage);
            return document;
        }

        public CombatLayoutCell Cell(int x, int y) => cells.Find(c => c.x == x && c.y == y);
        public CombatLayoutUnit Unit(int x, int y) => units.Find(u => u.x == x && u.y == y);
        public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < stage.width && y < stage.height;

        public CombatLayoutUnit PlaceCharacter(int characterKey, int x, int y)
        {
            if (!CombatCharacterCatalog.TryOwner(characters, characterKey, out string owner))
                throw new InvalidOperationException("현재 DT 목록에 있는 유닛과 유효한 진형(1=아군, 2=적군)이 필요합니다.");
            if (!Contains(x, y) || Cell(x, y).type == "Blocked" || Unit(x, y) != null)
                throw new InvalidOperationException("유닛은 비어 있는 바닥에 배치해야 합니다.");
            if (nextUnitKey < 1 || nextUnitKey > int.MaxValue)
                throw new InvalidOperationException("Unit 키를 더 생성할 수 없습니다.");
            var unit = new CombatLayoutUnit { key = (int)nextUnitKey, characterKey = characterKey, owner = owner, x = x, y = y };
            units.Add(unit); nextUnitKey++;
            return unit;
        }

        public void PaintObstacle(int obstacleKey, int x, int y)
        {
            if (obstacleKey < 1 || obstacles == null || obstacles.Find("Obstacle_Key", obstacleKey) == null)
                throw new InvalidOperationException("현재 Obstacle DT 목록에 등록된 항목을 선택해 주세요.");
            if (!Contains(x, y))
                throw new InvalidOperationException("맵 안의 좌표가 필요합니다.");
            if (Unit(x, y) != null)
                throw new InvalidOperationException("유닛이 있는 타일은 벽으로 바꿀 수 없습니다.");
            var cell = Cell(x, y);
            cell.type = "Blocked";
            cell.obstacleKey = obstacleKey;
        }

        public void ValidateObstacleReferences()
        {
            foreach (var cell in cells)
                if (cell.obstacleKey > 0 && (obstacles == null || obstacles.Find("Obstacle_Key", cell.obstacleKey) == null))
                    throw new InvalidOperationException($"({cell.x}, {cell.y}): Obstacle 키 {cell.obstacleKey}가 현재 원본 DT에 없습니다.");
        }

        public void Resize(CombatLayoutStage settings)
        {
            settings.Validate();
            if (units.Any(u => u.x >= settings.width || u.y >= settings.height))
                throw new InvalidOperationException("축소할 맵 밖의 유닛을 먼저 이동하거나 삭제해 주세요.");
            if (cells.Any(c => (c.x >= settings.width || c.y >= settings.height) &&
                               (c.type != "Floor" || c.elevation != 0 || c.terrainKey != 0 || c.obstacleKey != 0)))
                throw new InvalidOperationException("축소할 맵 밖의 타일을 먼저 초기화해 주세요.");
            var previous = cells.ToDictionary(c => (c.x, c.y));
            var resized = new List<CombatLayoutCell>();
            for (int y = 0; y < settings.height; y++)
                for (int x = 0; x < settings.width; x++)
                    resized.Add(previous.TryGetValue((x, y), out var cell) ? cell : new CombatLayoutCell { x = x, y = y });
            stage = settings.Copy();
            cells = resized;
        }

        public void Validate()
        {
            if (stage == null || cells == null || units == null)
                throw new InvalidOperationException("맵 데이터가 없습니다.");
            stage.Validate();
            var positions = new HashSet<(int, int)>();
            foreach (var cell in cells)
            {
                if (cell == null || !Contains(cell.x, cell.y) || !positions.Add((cell.x, cell.y)))
                    throw new InvalidOperationException("셀 좌표가 중복되거나 맵 범위를 벗어났습니다.");
                if (cell.type != "Floor" && cell.type != "Blocked" && cell.type != "Rail")
                    throw new InvalidOperationException($"({cell.x}, {cell.y}): 지원하지 않는 타일 종류입니다.");
                if (cell.elevation < 0 || cell.elevation > 20 || cell.terrainKey < 0)
                    throw new InvalidOperationException("높이는 0~20, Terrain 키는 0 이상이어야 합니다.");
                if (cell.obstacleKey < 0 || (cell.obstacleKey > 0 && cell.type != "Blocked"))
                    throw new InvalidOperationException("Obstacle 키는 0 이상이어야 하며 등록된 장애물은 벽 타일에만 배치할 수 있습니다.");
            }
            if (cells.Count != stage.width * stage.height)
                throw new InvalidOperationException("맵의 모든 셀이 필요합니다.");
            positions.Clear();
            var keys = new HashSet<int>();
            foreach (var unit in units)
            {
                if (unit == null || unit.key < 1 || unit.characterKey < 1 || !keys.Add(unit.key))
                    throw new InvalidOperationException("Unit / Character 키는 양수여야 하며 Unit 키가 중복되면 안 됩니다.");
                if (!Contains(unit.x, unit.y) || !positions.Add((unit.x, unit.y)))
                    throw new InvalidOperationException("유닛이 겹치거나 맵 범위를 벗어났습니다.");
                if (unit.owner != "Player" && unit.owner != "Enemy")
                    throw new InvalidOperationException("유닛 소속은 Player 또는 Enemy여야 합니다.");
                if (Cell(unit.x, unit.y).type == "Blocked")
                    throw new InvalidOperationException("벽 위에는 유닛을 배치할 수 없습니다.");
                if (unit.key >= nextUnitKey)
                    throw new InvalidOperationException("다음 Unit 키가 기존 Unit 키보다 커야 합니다.");
            }
            if (nextUnitKey < 1 || nextUnitKey > (long)int.MaxValue + 1)
                throw new InvalidOperationException("다음 Unit 키가 유효 범위를 벗어났습니다.");
        }
    }

    [Serializable]
    internal sealed class CombatLayoutStage
    {
        public int key = 100001;
        public string name = "새 전투 맵";
        public int width = 8, height = 10, reserve = 5, playerDepth = 2, enemyDepth = 2, seed;
        public long night = 1000;
        public CombatLayoutStage Copy() => (CombatLayoutStage)MemberwiseClone();
        public void Validate()
        {
            if (key < 1 || string.IsNullOrWhiteSpace(name) || name.Length > 120)
                throw new InvalidOperationException("Stage 키는 양수, 이름은 1~120자여야 합니다.");
            if (width < 1 || width > 40 || height < 3 || height > 40)
                throw new InvalidOperationException("맵 가로는 1~40, 세로는 3~40이어야 합니다.");
            if (reserve < 1 || playerDepth < 1 || enemyDepth < 1 || (long)playerDepth + enemyDepth >= height)
                throw new InvalidOperationException("대기 슬롯과 배치 깊이는 양수이며 아군+적군 깊이는 맵 세로보다 작아야 합니다.");
            if (night < 1 || night > 9007199254740991L)
                throw new InvalidOperationException("밤 길이는 1~9007199254740991의 정수여야 합니다.");
        }
    }

    [Serializable]
    internal sealed class CombatLayoutCell
    {
        public int x, y, elevation, terrainKey, obstacleKey;
        public string type = "Floor";
    }

    [Serializable]
    internal sealed class CombatLayoutUnit
    {
        public int key, characterKey, x, y;
        public string owner = "Enemy";
    }

    // String values retain missing DT fields across Unity serialization / domain reloads.
    [Serializable]
    internal sealed class CombatReferenceSheet
    {
        public List<string> columns = new List<string>();
        public List<CombatReferenceRow> rows = new List<CombatReferenceRow>();
        public string Value(CombatReferenceRow row, string column)
        {
            int index = columns.IndexOf(column);
            return index < 0 || index >= row.values.Count ? "" : row.values[index];
        }
        public int Number(CombatReferenceRow row, string column) =>
            int.TryParse(Value(row, column), out int number) ? number : 0;
        public CombatReferenceRow Find(string column, int key) => rows.Find(row => Number(row, column) == key);
        public void Set(CombatReferenceRow row, string column, string value)
        {
            int index = columns.IndexOf(column);
            if (index < 0) { index = columns.Count; columns.Add(column); }
            while (row.values.Count <= index) row.values.Add("");
            row.values[index] = value;
        }
    }

    [Serializable]
    internal sealed class CombatReferenceRow
    {
        public List<string> values = new List<string>();
    }
}
