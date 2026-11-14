"""게임 DB 에셋을 읽어 시뮬레이터 정의로 바꾼다.

값은 복사하지 않고 늘 `Assets/ProjectL/DataBaseSO/InGameGroup`에서 읽는다(게임 데이터가 바뀌면 따라간다).
번호의 뜻(열거형)은 게임 Core 코드에서 옮겼다. 원본 파일은 각 열거형 주석에 있다.
"""

from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum
from functools import cached_property
from pathlib import Path

from .unity_yaml import load_asset, read_guid

PROJECT_ROOT = Path(__file__).resolve().parents[2]
DB_ROOT = PROJECT_ROOT / "Assets" / "ProjectL" / "DataBaseSO" / "InGameGroup"

Cell = tuple[int, int]


class AssetError(ValueError):
    """에셋이 시뮬레이터가 다룰 수 없는 모양일 때."""


class Owner(IntEnum):
    """Contracts/Common/UnitOwnerEligibility.cs"""
    PLAYER_ONLY = 0
    ENEMY_ONLY = 1
    BOTH = 2


class Category(IntEnum):
    """Contracts/Common/CategoryKey.cs. 이동력은 CategoryConfig 에셋이 정한다."""
    MAIN = 10
    RIDER = 20
    HEAVY = 30
    CANNON = 40
    BOSS = 50


class Shape(IntEnum):
    """Contracts/Common/TargetingShape.cs"""
    CARDINAL_LINE = 1
    DIAGONAL_LINE = 2
    EIGHT_DIRECTION_LINE = 3
    FAN = 4
    RADIAL_SQUARE = 5


class ChoiceMode(IntEnum):
    """Contracts/Common/TargetingChoiceMode.cs"""
    SINGLE = 1
    DIRECTION = 2
    ALL = 3


class Splash(IntEnum):
    """Contracts/Common/TargetingSplash.cs"""
    NONE = 0
    CROSS = 1
    DIAGONAL = 2
    RADIAL = 3


class ChoiceAngle(IntEnum):
    """Contracts/Common/TargetingChoiceAngle.cs"""
    UNRESTRICTED = 0
    FRONT = 1
    FRONT_AND_SIDES = 2
    FRONT_AND_REAR = 3


class WallDelivery(IntEnum):
    """Contracts/Commands/AttackOcclusionSpec.cs. IGNORE_WALLS는 곡사."""
    REQUIRE_CLEAR = 1
    IGNORE_WALLS = 2


class WallPropagation(IntEnum):
    """Contracts/Commands/AttackOcclusionSpec.cs"""
    FROM_IMPACT = 1
    IGNORE_WALLS = 2


class TerrainKind(IntEnum):
    """1~5는 Contracts/Combat/StaticTerrainFact.cs, 6·7은 DB 지형 에셋 값."""
    HIGH_GROUND = 1
    SANDSTORM = 2
    WALL = 3
    WEAK_GROUND = 4
    CLIFF = 5
    TORNADO = 6
    PRESENCE_MODIFIER = 7


class Objective(IntEnum):
    """Contracts/Combat/BattleObjective.cs"""
    HEADQUARTERS = 1
    BOSS = 2


@dataclass(frozen=True)
class Targeting:
    name: str
    shape: Shape
    choice: ChoiceMode
    splash: Splash
    splash_percent: int
    angle: ChoiceAngle


@dataclass(frozen=True)
class Trait:
    kind: str           # 에셋의 클래스 이름에서 "TraitAuthoring"을 뗀 것 (예: Knockback)
    data: dict          # 에셋에 적힌 값 그대로 (예: {"level": 1})


@dataclass(frozen=True)
class UnitDef:
    name: str
    key: int
    owner: Owner
    category: Category
    max_hp: int
    defense: int
    attack: int
    attack_range: int
    move: int
    targeting: Targeting
    wall_delivery: WallDelivery
    wall_propagation: WallPropagation
    can_move: bool
    can_attack: bool
    forced_move_immune: bool
    footprint: Cell
    traits: tuple[Trait, ...]

    def trait(self, kind: str) -> Trait | None:
        return next((trait for trait in self.traits if trait.kind == kind), None)


@dataclass(frozen=True)
class TerrainDef:
    name: str
    kind: TerrainKind
    entry_threshold: int


@dataclass(frozen=True)
class StructureDef:
    name: str
    key: int
    footprint: Cell
    max_hp: int
    attack: int


@dataclass(frozen=True)
class EnemyPlacement:
    unit: UnitDef
    cell: Cell
    rotation: int
    victory_target: bool


@dataclass(frozen=True)
class TerrainPlacement:
    terrain: TerrainDef
    cell: Cell
    source_id: int
    known_to: tuple[int, ...]   # 이 지형을 아는 진영 번호들


@dataclass(frozen=True)
class Reinforcement:
    per_turn: int
    padding: int
    field_limit: int
    pool: tuple[tuple[UnitDef, int], ...]


@dataclass(frozen=True)
class StageDef:
    name: str
    width: int
    height: int
    objective: Objective
    enemy_level: int
    seed: int
    enemies: tuple[EnemyPlacement, ...]
    headquarters: StructureDef | None
    headquarters_anchor: Cell | None
    deployment_cells: frozenset[Cell]
    terrain: tuple[TerrainPlacement, ...]
    reinforcement: Reinforcement | None
    rail_cells: tuple[Cell, ...]
    gimmicks: frozenset[str]        # 켜져 있는 기믹 이름: railway, rockfall, haboob, tornado
    corner_cut_ticks: int

    def headquarters_cells(self) -> tuple[Cell, ...]:
        if self.headquarters is None or self.headquarters_anchor is None:
            return ()
        ax, ay = self.headquarters_anchor
        width, height = self.headquarters.footprint
        return tuple((ax + dx, ay + dy) for dy in range(height) for dx in range(width))


@dataclass(frozen=True)
class PlayerLoadout:
    name: str
    main: UnitDef
    subs: tuple[UnitDef, ...]
    hand_size: int


def _cell(value) -> Cell:
    return (int(value["x"]), int(value["y"]))


def _known_to(raw) -> tuple[int, ...]:
    """Unity가 int 배열을 16진 글자(4바이트 리틀 엔디언)로 쓴 값을 푼다."""
    text = str(raw or "")
    if len(text) % 8:
        raise AssetError(f"knownTo 값을 읽을 수 없습니다: {text}")
    return tuple(int.from_bytes(bytes.fromhex(text[i:i + 8]), "little") for i in range(0, len(text), 8))


class GameAssets:
    """게임 DB 에셋 묶음. GUID 참조를 따라가며 정의를 만들고, 한 번 만든 정의는 다시 쓴다."""

    def __init__(self, db_root: Path = DB_ROOT):
        if not db_root.is_dir():
            raise AssetError(f"게임 DB 폴더가 없습니다: {db_root}")
        self.db_root = db_root
        self._paths: dict[str, Path] = {}
        for meta in db_root.rglob("*.asset.meta"):
            guid = read_guid(meta)
            if guid:
                self._paths[guid] = meta.with_suffix("")
        self._cache: dict[Path, object] = {}

    # ---- 참조 ----
    def _path(self, ref) -> Path | None:
        if not isinstance(ref, dict) or not ref.get("guid"):
            return None
        path = self._paths.get(ref["guid"])
        if path is None:
            raise AssetError(f"GUID를 찾을 수 없습니다: {ref['guid']}")
        return path

    def _cached(self, path: Path, build):
        if path not in self._cache:
            self._cache[path] = build(path, load_asset(path))
        return self._cache[path]

    # ---- 정의 ----
    @cached_property
    def movement_by_category(self) -> dict[Category, int]:
        config = load_asset(self.db_root / "Category" / "Item" / "CategoryConfig_Default.asset")
        names = {Category.MAIN: "main", Category.RIDER: "rider", Category.HEAVY: "heavy",
                 Category.CANNON: "cannon", Category.BOSS: "boss"}
        return {category: int(config[f"{name}Movement"]["movementAllowance"]) for category, name in names.items()}

    def targeting(self, ref) -> Targeting:
        def build(path, data):
            return Targeting(path.stem, Shape(data["shape"]), ChoiceMode(data["choiceMode"]),
                             Splash(data.get("splash", 0)), int(data.get("splashPercent", 0)),
                             ChoiceAngle(data.get("choiceAngle", 0)))
        return self._cached(self._require(ref, "지정 방식"), build)

    def unit(self, ref) -> UnitDef:
        def build(path, data):
            if not data.get("targetingMethod", {}).get("guid"):
                raise AssetError(f"{path.stem}: 지정 방식이 없습니다")
            category = Category(data["category"])
            traits = []
            for entry in (data.get("references") or {}).get("RefIds") or []:
                if not isinstance(entry, dict) or not isinstance(entry.get("type"), dict):
                    continue
                kind = str(entry["type"]["class"]).removesuffix("TraitAuthoring")
                traits.append(Trait(kind, dict(entry.get("data") or {})))
            return UnitDef(
                name=path.stem, key=int(data["key"]), owner=Owner(data["ownerEligibility"]), category=category,
                max_hp=int(data["maxHealth"]), defense=int(data["physicalDefense"]), attack=int(data["attackPower"]),
                attack_range=int(data["attackRange"]), move=self.movement_by_category[category],
                targeting=self.targeting(data["targetingMethod"]),
                wall_delivery=WallDelivery(data["wallDelivery"]), wall_propagation=WallPropagation(data["wallPropagation"]),
                can_move=bool(data["canMoveVoluntarily"]), can_attack=bool(data["canBasicAttack"]),
                forced_move_immune=bool(data["isForcedMovementImmune"]),
                footprint=(int(data["footprintWidth"]), int(data["footprintHeight"])), traits=tuple(traits))
        return self._cached(self._require(ref, "유닛"), build)

    def terrain(self, ref) -> TerrainDef:
        def build(path, data):
            return TerrainDef(path.stem, TerrainKind(data["kind"]), int(data.get("entryThreshold", 0)))
        return self._cached(self._require(ref, "지형"), build)

    def structure(self, ref) -> StructureDef:
        def build(path, data):
            return StructureDef(path.stem, int(data["key"]), (int(data["footprintWidth"]), int(data["footprintHeight"])),
                                int(data["maxHealth"]), int(data["attackPower"]))
        return self._cached(self._require(ref, "구조물"), build)

    def _require(self, ref, what: str) -> Path:
        path = self._path(ref)
        if path is None:
            raise AssetError(f"{what} 참조가 비어 있습니다")
        return path

    def unit_named(self, name: str) -> UnitDef:
        """유닛 에셋 이름(예: Unit_Demon)으로 찾는다. 시험과 임시 판 만들기용."""
        matches = [path for path in (self.db_root / "Unit" / "Item").rglob(f"{name}.asset")]
        if len(matches) != 1:
            raise AssetError(f"유닛 {name}을(를) 하나로 찾지 못했습니다: {matches}")
        guid = read_guid(matches[0].with_name(matches[0].name + ".meta"))
        return self.unit({"guid": guid})

    # ---- 스테이지·구성 ----
    def combat_stage_names(self) -> list[str]:
        return sorted(path.stem for path in (self.db_root / "Stage" / "Combat").glob("*.asset"))

    def stage(self, name: str) -> StageDef:
        path = self.db_root / "Stage" / "Combat" / f"{name}.asset"
        if not path.is_file():
            raise AssetError(f"전투 스테이지가 없습니다: {name} (있는 것: {', '.join(self.combat_stage_names())})")
        return self._cached(path, self._build_stage)

    def _build_stage(self, path: Path, data: dict) -> StageDef:
        name = path.stem
        field = load_asset(self._require(data.get("fieldDefinition"), "판"))
        width, height = int(field["columnCount"]), int(field["rowCount"])

        def inside(cell: Cell, what: str) -> Cell:
            if not (0 <= cell[0] < width and 0 <= cell[1] < height):
                raise AssetError(f"{name}: {what} {cell}이(가) 판({width}x{height}) 밖입니다")
            return cell

        objective = Objective(data["objectiveKind"])
        enemies = []
        for entry in data.get("initialEnemyPlacements") or []:
            unit = self.unit(entry["unitDefinition"])
            if unit.footprint != (1, 1):
                raise AssetError(f"{name}: {unit.name}은(는) 여러 칸 유닛이라 전투에 들어갈 수 없습니다")
            enemies.append(EnemyPlacement(unit, inside(_cell(entry["anchor"]), unit.name), int(entry.get("rotation", 0)),
                                          bool(entry.get("isVictoryTarget", 0))))
        if objective is Objective.BOSS and not any(enemy.victory_target for enemy in enemies):
            raise AssetError(f"{name}: 보스 목표인데 격파 대상이 없습니다")

        headquarters = self._path(data.get("headquarters"))
        headquarters_def = self.structure(data["headquarters"]) if headquarters else None
        anchor = _cell(data["enemyHeadquartersAnchor"]) if headquarters_def else None
        if objective is Objective.HEADQUARTERS and headquarters_def is None:
            raise AssetError(f"{name}: 본진 목표인데 본진이 없습니다")

        deploy = [_cell(cell) for cell in data.get("playerDeploymentCells") or []]
        if not deploy:   # 목록이 비면 판의 아래 줄들(CombatPlacementCalculator)
            depth = int(field["playerDeploymentDepth"])
            deploy = [(x, y) for y in range(depth) for x in range(width)]
        for cell in deploy:
            inside(cell, "배치 칸")

        environment = data.get("environment") or {}
        for key in ("obstaclePlacements", "objectPlacements"):
            if environment.get(key):
                raise AssetError(f"{name}: {key}는 아직 시뮬레이터가 다루지 않습니다")
        for key in ("initialWallCells", "staticTerrainPlacements", "initialEnemyStructurePlacements"):
            if data.get(key):
                raise AssetError(f"{name}: {key}는 아직 시뮬레이터가 다루지 않습니다")
        terrain = tuple(TerrainPlacement(self.terrain(entry["definition"]), inside(_cell(entry["cell"]), "지형"),
                                         int(entry["sourceId"]), _known_to(entry.get("knownTo")))
                        for entry in environment.get("terrainPlacements") or [])

        reinforcement = None
        nexus = data.get("nexusReinforcements") or {}
        if nexus.get("enabled"):
            pool = tuple((self.unit(entry["unitDefinition"]), int(entry["count"])) for entry in nexus.get("pool") or [])
            reinforcement = Reinforcement(int(nexus["spawnCountPerTurn"]), int(nexus["rangePadding"]),
                                          int(nexus["fieldEnemyLimit"]), pool)

        gimmicks = set()
        if data.get("railwayEnabled"):
            gimmicks.add("railway")
        for key in ("rockfall", "haboob"):
            if (data.get(key) or {}).get("enabled"):
                gimmicks.add(key)
        if any(placement.terrain.kind is TerrainKind.TORNADO for placement in terrain):
            gimmicks.add("tornado")
        rails = tuple(inside(_cell(cell), "레일") for cell in (data.get("railway") or {}).get("railCells") or [])

        return StageDef(name=name, width=width, height=height, objective=objective,
                        enemy_level=int(data.get("enemyLevel", 1)), seed=int(data.get("timelineSeed", 0)),
                        enemies=tuple(enemies), headquarters=headquarters_def, headquarters_anchor=anchor,
                        deployment_cells=frozenset(deploy), terrain=terrain, reinforcement=reinforcement,
                        rail_cells=rails, gimmicks=frozenset(gimmicks),
                        corner_cut_ticks=int(data.get("cornerCutTicks", 0)))

    def player_loadout(self, name: str = "PlayerLoadout_1") -> PlayerLoadout:
        path = self.db_root / "Loadout" / "Item" / f"{name}.asset"
        if not path.is_file():
            raise AssetError(f"아군 구성이 없습니다: {name}")

        def build(path, data):
            subs = []
            for entry in data.get("entries") or []:
                subs.extend([self.unit(entry["unitDefinition"])] * int(entry["count"]))
            return PlayerLoadout(path.stem, self.unit(data["mainCharacter"]), tuple(subs),
                                 int(data["startingHandSize"]))
        return self._cached(path, build)
