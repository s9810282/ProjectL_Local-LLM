"""판 상태: 유닛·구조물·지형·턴. 엔진(rules.py)이 복사본을 만들어 바꾼다.

게임 원본: 행동 사용 단계 Contracts/Combat/UnitActionUseRules.cs(Deferred·Available·Moved·Attacked),
전투 시작 Application/Combat/CombatTurnCalculator.cs(필드 유닛은 Deferred로 시작, 플레이어 먼저).
"""

from __future__ import annotations

import copy
from dataclasses import dataclass, field
from enum import Enum
import random

from .assets import AssetError, Category, Cell, Objective, PlayerLoadout, StageDef, TerrainKind, UnitDef
from .targeting import Side
from .walls import WallGeometry


class ActionUse(Enum):
    DEFERRED = "deferred"     # 이번 턴에는 행동할 수 없다(새로 들어온 유닛)
    AVAILABLE = "available"   # 이동·공격 모두 할 수 있다
    MOVED = "moved"           # 이동했다. 공격만 할 수 있다
    ATTACKED = "attacked"     # 공격했다. 더 할 수 없다


class Outcome(Enum):
    PLAYER_WIN = "player_win"
    ENEMY_WIN = "enemy_win"


@dataclass(slots=True)
class Unit:
    uid: int
    defn: UnitDef
    side: Side
    cell: Cell
    hp: int
    max_hp: int
    attack: int
    defense: int
    use: ActionUse = ActionUse.DEFERRED
    is_main: bool = False
    victory_target: bool = False
    # 특성 상태(S-b2에서 쓴다): 이름 → 남은 값
    statuses: dict = field(default_factory=dict)
    shield: int = 0

    @property
    def alive(self) -> bool:
        return self.hp > 0

    @property
    def name(self) -> str:
        return self.defn.name


@dataclass(slots=True)
class Structure:
    sid: int
    name: str
    side: Side
    cells: tuple[Cell, ...]
    hp: int
    max_hp: int

    @property
    def alive(self) -> bool:
        return self.hp > 0


class StaticBoard:
    """전투 동안 바뀌지 않는 판 정보. 모든 복사본이 함께 쓴다."""

    def __init__(self, stage: StageDef):
        self.stage = stage
        self.width = stage.width
        self.height = stage.height
        self.walls = WallGeometry([p.cell for p in stage.terrain if p.terrain.kind is TerrainKind.WALL],
                                  stage.corner_cut_ticks)
        self.high_ground = frozenset(p.cell for p in stage.terrain if p.terrain.kind is TerrainKind.HIGH_GROUND)
        self.sandstorm = frozenset(p.cell for p in stage.terrain if p.terrain.kind is TerrainKind.SANDSTORM)
        self.weak_threshold = {p.cell: p.terrain.entry_threshold for p in stage.terrain
                               if p.terrain.kind is TerrainKind.WEAK_GROUND}

    def inside(self, cell: Cell) -> bool:
        return 0 <= cell[0] < self.width and 0 <= cell[1] < self.height


@dataclass(slots=True)
class BattleState:
    board: StaticBoard
    units: dict[int, Unit]
    structures: dict[int, Structure]
    cliffs: frozenset[Cell]                 # 약한 지반이 무너지면 늘어난다
    weak_entries: dict[Cell, int]           # 약한 지반 칸마다 들어온 횟수(아직 안 무너진 칸만)
    turn_side: Side = Side.PLAYER
    completed_turns: int = 0                # 양쪽 턴 끝을 모두 센 수(게임 CompletedControllerTurnCount)
    round: int = 1                          # 플레이어 턴 + 적 턴 = 1라운드
    outcome: Outcome | None = None
    reinforcement_pool: dict[str, int] = field(default_factory=dict)
    rng: random.Random = field(default_factory=random.Random)
    next_id: int = 1
    log: list = field(default_factory=list)

    # ---- 복사 ----
    def clone(self, keep_log: bool = False) -> "BattleState":
        units = {uid: copy.copy(unit) for uid, unit in self.units.items()}
        for unit in units.values():
            unit.statuses = dict(unit.statuses)
        rng = random.Random()
        rng.setstate(self.rng.getstate())
        return BattleState(board=self.board, units=units,
                           structures={sid: copy.copy(s) for sid, s in self.structures.items()},
                           cliffs=self.cliffs, weak_entries=dict(self.weak_entries), turn_side=self.turn_side,
                           completed_turns=self.completed_turns, round=self.round, outcome=self.outcome,
                           reinforcement_pool=dict(self.reinforcement_pool), rng=rng, next_id=self.next_id,
                           log=list(self.log) if keep_log else [])

    # ---- 조회 ----
    def living(self, side: Side | None = None) -> list[Unit]:
        return [u for u in self.units.values() if u.alive and (side is None or u.side is side)]

    def unit_at(self, cell: Cell) -> Unit | None:
        for unit in self.units.values():
            if unit.alive and unit.cell == cell:
                return unit
        return None

    def structure_at(self, cell: Cell) -> Structure | None:
        for structure in self.structures.values():
            if structure.alive and cell in structure.cells:
                return structure
        return None

    def main_character(self) -> Unit | None:
        return next((u for u in self.units.values() if u.is_main), None)

    def blocks_entry(self, cell: Cell) -> bool:
        """일반 이동·배치가 못 들어가는 지형(벽·낭떠러지, EnvironmentTerrainRules.BlocksVoluntaryEntry)."""
        return self.board.walls.occupies(cell) or cell in self.cliffs

    def is_free(self, cell: Cell, ignore_uid: int | None = None) -> bool:
        if not self.board.inside(cell) or self.blocks_entry(cell):
            return False
        if self.structure_at(cell) is not None:
            return False
        unit = self.unit_at(cell)
        return unit is None or unit.uid == ignore_uid


def _make_unit(state: BattleState, defn: UnitDef, side: Side, cell: Cell, *, is_main=False, victory=False) -> Unit:
    unit = Unit(uid=state.next_id, defn=defn, side=side, cell=cell, hp=defn.max_hp, max_hp=defn.max_hp,
                attack=defn.attack, defense=defn.defense, is_main=is_main, victory_target=victory)
    state.units[unit.uid] = unit
    state.next_id += 1
    return unit


def player_traits(defn: UnitDef) -> tuple[str, ...]:
    """플레이어 소유 유닛이 병과로 받는 기본 특성(OwnedUnitSpecResolver): 기병 도약, 중장 굳건한 의지."""
    if defn.category is Category.RIDER:
        return ("Leap",)
    if defn.category is Category.HEAVY:
        return ("SteadfastWill",)
    return ()


def new_battle(stage: StageDef, loadout: PlayerLoadout, placements: dict[int, Cell], seed: int | None = None) -> BattleState:
    """처음 판. placements는 아군 구성 순서 번호(0 = 메인, 1.. = 서브) → 배치 칸."""
    if stage.enemy_level != 1:
        raise AssetError(f"{stage.name}: 적 레벨 {stage.enemy_level}의 성장은 아직 다루지 않습니다")
    board = StaticBoard(stage)
    cliffs = frozenset(p.cell for p in stage.terrain if p.terrain.kind is TerrainKind.CLIFF)
    state = BattleState(board=board, units={}, structures={}, cliffs=cliffs,
                        weak_entries={cell: 0 for cell in board.weak_threshold},
                        rng=random.Random(stage.seed if seed is None else seed))
    for enemy in stage.enemies:
        _make_unit(state, enemy.unit, Side.ENEMY, enemy.cell, victory=enemy.victory_target)
    if stage.headquarters is not None:
        structure = Structure(sid=state.next_id, name=stage.headquarters.name, side=Side.ENEMY,
                              cells=stage.headquarters_cells(), hp=stage.headquarters.max_hp,
                              max_hp=stage.headquarters.max_hp)
        state.structures[structure.sid] = structure
        state.next_id += 1
    if stage.reinforcement is not None:
        for unit, count in stage.reinforcement.pool:
            state.reinforcement_pool[unit.name] = state.reinforcement_pool.get(unit.name, 0) + count

    roster = [loadout.main, *loadout.subs]
    if 0 not in placements:
        raise AssetError("메인 캐릭터를 배치해야 전투를 시작할 수 있습니다")
    deployable = stage.deployment_cells
    for index, cell in sorted(placements.items()):
        if not 0 <= index < len(roster):
            raise AssetError(f"아군 구성에 {index}번 유닛이 없습니다")
        if cell not in deployable or not state.is_free(cell):
            raise AssetError(f"{roster[index].name}을(를) {cell}에 놓을 수 없습니다")
        _make_unit(state, roster[index], Side.PLAYER, cell, is_main=index == 0)
    return state
