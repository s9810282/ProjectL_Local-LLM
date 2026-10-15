/* Pure layout model. Sheet column names are a new export contract, not existing Unity enums. */
(function (root) {
  'use strict';
  const MAX_KEY = 2147483647;
  const SHEETS = {
    CombatStageMaster: ['CombatStage_Key', 'CombatStage_Name', 'CombatStage_MapWidth', 'CombatStage_MapHeight', 'CombatStage_ReserveCapacity', 'CombatStage_PlayerDeploymentDepth', 'CombatStage_EnemyDeploymentDepth', 'CombatStage_Seed', 'CombatStage_NightLength', 'CombatStage_SchemaVersion'],
    CombatCellMaster: ['CombatCell_Key', 'CombatCell_StageKey', 'CombatCell_X', 'CombatCell_Y', 'CombatCell_Elevation', 'CombatCell_Type', 'CombatCell_TerrainKey'],
    CombatUnitMaster: ['CombatUnit_Key', 'CombatUnit_StageKey', 'CombatUnit_CharacterKey', 'CombatUnit_Owner', 'CombatUnit_X', 'CombatUnit_Y', 'CombatUnit_Elevation']
  };
  Object.values(SHEETS).forEach(Object.freeze);
  Object.freeze(SHEETS);
  function integer(value, name, min = 0, max = MAX_KEY) {
    if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < min || value > max)
      throw new Error(`${name}: ${min}~${max} 범위의 정수가 필요해.`);
    return value;
  }
  function text(value, name) {
    if (typeof value !== 'string' || !value.trim() || value.length > 120) throw new Error(`${name}: 1~120자의 이름이 필요해.`);
    return value;
  }
  function checkStage(s) {
    if (!s || typeof s !== 'object') throw new Error('스테이지 설정이 없어.');
    integer(s.key, 'Stage 키', 1); text(s.name, '배치 이름');
    integer(s.width, '가로', 1, 40); integer(s.height, '세로', 3, 40);
    integer(s.reserve, '대기 슬롯', 1); integer(s.playerDepth, '아군 깊이', 1, 39); integer(s.enemyDepth, '적군 깊이', 1, 39);
    if (s.playerDepth + s.enemyDepth >= s.height) throw new Error('아군 깊이 + 적군 깊이는 맵의 세로 칸 수보다 작아야 해.');
    integer(s.seed, '스테이지 Seed', -2147483648); integer(s.night, '밤 길이', 1, Number.MAX_SAFE_INTEGER);
  }
  const position = (x, y) => `${x},${y}`;
  function inBounds(stage, x, y) {
    return Number.isInteger(x) && Number.isInteger(y) && x >= 0 && y >= 0 && x < stage.width && y < stage.height;
  }
  function create() {
    const stage = { key: 100001, name: '새 전투 맵', width: 8, height: 10, reserve: 5, playerDepth: 2, enemyDepth: 2, seed: 0, night: 1000 };
    return { stage, cells: makeCells(stage), units: [], nextUnitKey: 1 };
  }
  function makeCells(stage, oldCells = []) {
    const old = new Map(oldCells.map(c => [position(c.x, c.y), c]));
    const result = [];
    for (let y = 0; y < stage.height; y++) for (let x = 0; x < stage.width; x++)
      result.push({ x, y, elevation: old.get(position(x, y))?.elevation || 0, type: old.get(position(x, y))?.type || 'Floor', terrainKey: old.get(position(x, y))?.terrainKey || 0 });
    return result;
  }
  function validate(state) {
    if (!state || typeof state !== 'object') throw new Error('맵 데이터가 없어.');
    checkStage(state.stage);
    if (!Array.isArray(state.cells) || !Array.isArray(state.units)) throw new Error('셀 또는 유닛 목록이 잘못됐어.');
    const cells = new Map();
    for (const c of state.cells) {
      if (!c || !inBounds(state.stage, c.x, c.y)) throw new Error('셀 좌표가 맵 범위를 벗어났어.');
      if (!['Floor', 'Blocked', 'Rail'].includes(c.type)) throw new Error(`셀 (${c.x}, ${c.y})의 종류가 잘못됐어.`);
      integer(c.terrainKey, 'Terrain 키');
      integer(c.elevation, '셀 높이', 0, 20);
      const p = position(c.x, c.y);
      if (cells.has(p)) throw new Error(`셀 (${c.x}, ${c.y})이 중복됐어.`);
      cells.set(p, c);
    }
    if (cells.size !== state.stage.width * state.stage.height) throw new Error('맵의 모든 셀 정보가 필요해.');
    const keys = new Set(), occupied = new Set();
    for (const u of state.units) {
      if (!u) throw new Error('빈 유닛 행이 있어.');
      integer(u.key, 'Unit 키', 1); integer(u.characterKey, 'Character 키', 1);
      if (keys.has(u.key)) throw new Error(`Unit 키 ${u.key}가 중복됐어.`);
      keys.add(u.key);
      if (!['Player', 'Enemy'].includes(u.owner)) throw new Error(`Unit ${u.key}의 소속은 Player 또는 Enemy여야 해.`);
      if (!inBounds(state.stage, u.x, u.y)) throw new Error(`Unit ${u.key}가 맵 범위를 벗어났어.`);
      const p = position(u.x, u.y);
      if (occupied.has(p)) throw new Error(`(${u.x}, ${u.y})에 유닛이 겹쳐 있어.`);
      if (cells.get(p).type === 'Blocked') throw new Error(`Unit ${u.key}를 벽 위에 배치할 수 없어.`);
      occupied.add(p);
    }
    integer(state.nextUnitKey, '다음 Unit 키', 1, MAX_KEY + 1);
    if (state.units.some(u => u.key >= state.nextUnitKey)) throw new Error('다음 Unit 키는 현재 Unit 키보다 커야 해.');
    return state;
  }
  function resize(state, stage) {
    checkStage(stage);
    if (state.units.some(u => !inBounds(stage, u.x, u.y))) throw new Error('축소한 맵 밖에 유닛이 있어. 먼저 이동하거나 삭제해 줘.');
    if (state.cells.some(c => !inBounds(stage, c.x, c.y) && (c.type !== 'Floor' || c.terrainKey !== 0 || c.elevation !== 0)))
      throw new Error('축소한 맵 밖에 편집한 타일이 있어. 먼저 바닥 / Terrain 키 0으로 변경해 줘.');
    return validate({ stage: { ...stage }, cells: makeCells(stage, state.cells), units: state.units.map(u => ({ ...u })), nextUnitKey: state.nextUnitKey });
  }
  function tables(state) {
    validate(state); const s = state.stage;
    return {
      CombatStageMaster: [[...SHEETS.CombatStageMaster], [s.key, s.name, s.width, s.height, s.reserve, s.playerDepth, s.enemyDepth, s.seed, s.night, 1]],
      CombatCellMaster: [[...SHEETS.CombatCellMaster], ...state.cells.slice().sort((a, b) => a.y - b.y || a.x - b.x).map(c => [c.y * s.width + c.x + 1, s.key, c.x, c.y, c.elevation, c.type, c.terrainKey])],
      CombatUnitMaster: [[...SHEETS.CombatUnitMaster], ...state.units.slice().sort((a, b) => a.key - b.key).map(u => [u.key, s.key, u.characterKey, u.owner, u.x, u.y, state.cells.find(c => c.x === u.x && c.y === u.y).elevation])]
    };
  }
  function records(rows, required, sheetName) {
    if (!Array.isArray(rows) || !rows.length) throw new Error(`${sheetName}: 헤더 행이 없어.`);
    const headers = rows[0];
    for (const h of required) {
      if (!headers.includes(h)) throw new Error(`${sheetName}: ${h} 컬럼이 없어.`);
      if (headers.filter(v => v === h).length !== 1) throw new Error(`${sheetName}: ${h} 헤더가 중복됐어.`);
    }
    return rows.slice(1).filter(r => r.some(v => v !== null && v !== undefined && v !== '')).map(r =>
      Object.fromEntries(required.map(h => [h, r[headers.indexOf(h)]])));
  }
  function fromTables(t) {
    const r = {};
    for (const [sheet, columns] of Object.entries(SHEETS)) r[sheet] = records(t[sheet], columns, sheet);
    if (r.CombatStageMaster.length !== 1) throw new Error('이 편집기는 파일당 스테이지 1개를 지원해.');
    const s = r.CombatStageMaster[0];
    if (s.CombatStage_SchemaVersion !== 1) throw new Error('지원하지 않는 배치 엑셀 버전이야. SchemaVersion 1이 필요해.');
    const stage = { key: s.CombatStage_Key, name: s.CombatStage_Name, width: s.CombatStage_MapWidth, height: s.CombatStage_MapHeight, reserve: s.CombatStage_ReserveCapacity, playerDepth: s.CombatStage_PlayerDeploymentDepth, enemyDepth: s.CombatStage_EnemyDeploymentDepth, seed: s.CombatStage_Seed, night: s.CombatStage_NightLength };
    checkStage(stage);
    const cells = r.CombatCellMaster.map(c => {
      if (c.CombatCell_StageKey !== stage.key) throw new Error('셀의 Stage 키가 맵 설정과 달라.');
      if (c.CombatCell_Key !== c.CombatCell_Y * stage.width + c.CombatCell_X + 1) throw new Error('Cell 키는 Y × 가로 + X + 1이어야 해.');
      return { x: c.CombatCell_X, y: c.CombatCell_Y, elevation: c.CombatCell_Elevation, type: c.CombatCell_Type, terrainKey: c.CombatCell_TerrainKey };
    });
    const units = r.CombatUnitMaster.map(u => {
      if (u.CombatUnit_StageKey !== stage.key) throw new Error('유닛의 Stage 키가 맵 설정과 달라.');
      const cell = cells.find(c => c.x === u.CombatUnit_X && c.y === u.CombatUnit_Y);
      if (!cell || u.CombatUnit_Elevation !== cell.elevation) throw new Error('유닛의 높이가 배치 셀의 높이와 달라.');
      return { key: u.CombatUnit_Key, characterKey: u.CombatUnit_CharacterKey, owner: u.CombatUnit_Owner, x: u.CombatUnit_X, y: u.CombatUnit_Y };
    });
    const nextUnitKey = units.reduce((next, u) => Math.max(next, u.key + 1), 1);
    return validate({ stage, cells, units, nextUnitKey });
  }
  function characters(rows) {
    const rs = records(rows, ['Character_Key'], 'CharacterMaster');
    const header = rows[0]; const kor = header.indexOf('Character_NameKOR'), name = header.indexOf('Character_Name');
    const nonblank = rows.slice(1).filter(r => r.some(v => v !== null && v !== undefined && v !== ''));
    const seen = new Set();
    return rs.map((r, i) => {
      integer(r.Character_Key, `CharacterMaster ${i + 2}행 키`, 1);
      if (seen.has(r.Character_Key)) throw new Error(`Character 키 ${r.Character_Key}가 중복됐어.`);
      seen.add(r.Character_Key);
      return { key: r.Character_Key, name: String(nonblank[i][kor] || nonblank[i][name] || `Character ${r.Character_Key}`) };
    });
  }
  const api = { SHEETS, MAX_KEY, create, validate, resize, tables, fromTables, characters, position, inBounds };
  root.CombatModel = api;
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
})(typeof globalThis !== 'undefined' ? globalThis : window);
