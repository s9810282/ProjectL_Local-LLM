"""벽 시야: 공격 선분이 벽에 막히는지.

게임 원본(Core Domain/Combat): WallGeometryBuilder.cs(연결 성분·볼록 조각·모서리 깎기),
WallSegmentClipper.cs(선분 자르기), AttackOcclusionResolver.cs(직격·파급 검사).
칸 한 변은 1024틱이고 칸 가운데가 (x*1024, y*1024)다. 선분은 칸 가운데에서 칸 가운데까지다.
"""

from __future__ import annotations

from dataclasses import dataclass
from fractions import Fraction

from .assets import AssetError, Cell

TICKS_PER_TILE = 1024
_HALF = TICKS_PER_TILE // 2


@dataclass(frozen=True)
class WallPart:
    """볼록 조각 하나. exterior는 바깥 변(열린 경계), corners는 깎은 바깥 모서리."""
    x_min: int
    x_max: int
    y_min: int
    y_max: int
    cut: int
    exterior: frozenset[str]   # "L", "R", "B", "T"
    corners: frozenset[str]    # "BL", "TL", "BR", "TR"


_ALL_SIDES = frozenset("LRBT")
_ALL_CORNERS = frozenset({"BL", "TL", "BR", "TR"})


def _clip(nx: int, ny: int, limit: int, ax: int, ay: int, dx: int, dy: int, span: list, exterior: bool = True) -> bool:
    u = nx * ax + ny * ay - limit
    v = nx * dx + ny * dy
    if v == 0:
        return u < 0 if exterior else u <= 0
    if v > 0:
        upper = Fraction(-u, v)
        if upper < span[1]:
            span[1] = upper
    else:
        lower = Fraction(u, -v)
        if lower > span[0]:
            span[0] = lower
    return span[0] < span[1]


def segment_hits_part(start: Cell, end: Cell, part: WallPart) -> bool:
    """선분이 조각의 열린 내부와 겹치는가(WallSegmentClipper.IntersectsPart)."""
    ax, ay = start[0] * TICKS_PER_TILE, start[1] * TICKS_PER_TILE
    dx, dy = (end[0] - start[0]) * TICKS_PER_TILE, (end[1] - start[1]) * TICKS_PER_TILE
    span = [Fraction(0), Fraction(1)]
    p = part
    sides = ((-1, 0, -p.x_min, "L"), (1, 0, p.x_max, "R"), (0, -1, -p.y_min, "B"), (0, 1, p.y_max, "T"))
    for nx, ny, limit, side in sides:
        if not _clip(nx, ny, limit, ax, ay, dx, dy, span, side in p.exterior):
            return False
    corners = (("BL", -1, -1, -p.x_min - p.y_min - p.cut), ("TL", -1, 1, -p.x_min + p.y_max - p.cut),
               ("BR", 1, -1, p.x_max - p.y_min - p.cut), ("TR", 1, 1, p.x_max + p.y_max - p.cut))
    for name, nx, ny, limit in corners:
        if name in p.corners and not _clip(nx, ny, limit, ax, ay, dx, dy, span):
            return False
    return True


class WallGeometry:
    """판의 벽 칸에서 만든 불변 형상. 공격 선분 검사 결과를 기억한다(벽은 전투 중 바뀌지 않는다)."""

    def __init__(self, wall_cells, corner_cut_ticks: int):
        self.cells = frozenset(wall_cells)
        self._parts: list[WallPart] = []
        self._cache: dict[tuple[Cell, Cell], bool] = {}
        component_of: dict[Cell, int] = {}
        remaining = set(self.cells)
        for seed in sorted(self.cells, key=lambda c: (c[1], c[0])):
            if seed not in remaining:
                continue
            remaining.discard(seed)
            component, pending = [], [seed]
            while pending:
                cell = pending.pop()
                component.append(cell)
                component_of[cell] = len(self._parts)
                for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    neighbor = (cell[0] + ox, cell[1] + oy)
                    if neighbor in remaining:
                        remaining.discard(neighbor)
                        pending.append(neighbor)
            self._add_component(component, corner_cut_ticks)
        for cell in self.cells:
            for ox, oy in ((1, -1), (1, 1)):
                other = (cell[0] + ox, cell[1] + oy)
                if other in component_of and component_of[other] != component_of[cell]:
                    raise AssetError(f"서로 다른 벽 덩어리가 대각선으로 붙어 있습니다: {cell}, {other}")

    def _add_component(self, cells: list[Cell], cut: int) -> None:
        xs, ys = [c[0] for c in cells], [c[1] for c in cells]
        if min(xs) == max(xs) or min(ys) == max(ys):   # 한 줄짜리 벽은 감싼 사각형 하나
            self._parts.append(WallPart(min(xs) * TICKS_PER_TILE - _HALF, max(xs) * TICKS_PER_TILE + _HALF,
                                        min(ys) * TICKS_PER_TILE - _HALF, max(ys) * TICKS_PER_TILE + _HALF,
                                        cut, _ALL_SIDES, _ALL_CORNERS))
            return
        occupied = set(cells)
        for x, y in cells:
            left, right = (x - 1, y) in occupied, (x + 1, y) in occupied
            bottom, top = (x, y - 1) in occupied, (x, y + 1) in occupied
            exterior = frozenset(side for side, inside in (("L", left), ("R", right), ("B", bottom), ("T", top))
                                 if not inside)
            corners = set()
            if not left and not bottom and (x - 1, y - 1) not in occupied:
                corners.add("BL")
            if not left and not top and (x - 1, y + 1) not in occupied:
                corners.add("TL")
            if not right and not bottom and (x + 1, y - 1) not in occupied:
                corners.add("BR")
            if not right and not top and (x + 1, y + 1) not in occupied:
                corners.add("TR")
            self._parts.append(WallPart(x * TICKS_PER_TILE - _HALF, x * TICKS_PER_TILE + _HALF,
                                        y * TICKS_PER_TILE - _HALF, y * TICKS_PER_TILE + _HALF,
                                        cut, exterior, frozenset(corners)))

    def occupies(self, cell: Cell) -> bool:
        return cell in self.cells

    def blocks(self, start: Cell, end: Cell) -> bool:
        """두 칸 가운데를 잇는 선분이 벽 내부를 지나는가."""
        key = (start, end)
        result = self._cache.get(key)
        if result is None:
            result = any(segment_hits_part(start, end, part) for part in self._parts)
            self._cache[key] = result
        return result
