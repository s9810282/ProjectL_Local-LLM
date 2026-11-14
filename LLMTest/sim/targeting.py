"""공격 모양: 지정 방식이 덮는 칸.

게임 원본(Core Domain/Combat): AppointShapeCalculator.cs, AttackSelectionResolver.cs, TargetingAngleCalculator.cs.
오프셋은 공격자 칸 기준이다. 정면은 플레이어가 +y, 적이 -y다.
"""

from __future__ import annotations

from enum import Enum

from .assets import Cell, ChoiceAngle, Shape, Splash


class Side(Enum):
    PLAYER = "player"
    ENEMY = "enemy"

    @property
    def other(self) -> "Side":
        return Side.ENEMY if self is Side.PLAYER else Side.PLAYER


# AttackDirection 열거 순서(Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft)
DIRECTIONS: dict[str, Cell] = {
    "Up": (0, 1), "UpRight": (1, 1), "Right": (1, 0), "DownRight": (1, -1),
    "Down": (0, -1), "DownLeft": (-1, -1), "Left": (-1, 0), "UpLeft": (-1, 1),
}
CARDINAL = ("Up", "Right", "Down", "Left")
DIAGONAL = ("UpRight", "DownRight", "DownLeft", "UpLeft")
_CARDINAL_STEPS = [(0, 1), (1, 0), (0, -1), (-1, 0)]
_DIAGONAL_STEPS = [(1, 1), (1, -1), (-1, -1), (-1, 1)]
_EIGHT_STEPS = [(0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1)]


def _in_square(x: int, y: int, r: int) -> bool:
    return (x, y) != (0, 0) and -r <= x <= r and -r <= y <= r


def _in_expanding(x: int, y: int, forward: Cell, r: int) -> bool:
    fx, fy = forward
    depth = x * fx + y * fy
    if depth < 1 or depth > r:
        return False
    lateral = x * fy + y * -fx     # 오른쪽 = (fy, -fx)
    return -(depth - 1) <= lateral <= depth - 1


def base_contains(shape: Shape, r: int, x: int, y: int) -> bool:
    """방향 없이 모양이 그 오프셋을 덮는가(ContainsBaseMask). 한 칸 고르기의 조준 칸 검사."""
    if not _in_square(x, y, r):
        return False
    if shape is Shape.CARDINAL_LINE:
        return x == 0 or y == 0
    if shape is Shape.DIAGONAL_LINE:
        return abs(x) == abs(y)
    if shape is Shape.EIGHT_DIRECTION_LINE:
        return x == 0 or y == 0 or abs(x) == abs(y)
    if shape is Shape.FAN:
        return any(_in_expanding(x, y, step, r) for step in _CARDINAL_STEPS)
    return True


def supported_directions(shape: Shape) -> tuple[str, ...]:
    return DIAGONAL if shape is Shape.DIAGONAL_LINE else CARDINAL


def _rays(steps, r: int) -> list[Cell]:
    cells = {(sx * d, sy * d) for sx, sy in steps for d in range(1, r + 1)}
    return sorted(cells, key=lambda c: (c[1], c[0]))


def direction_offsets(shape: Shape, r: int, direction: str) -> list[Cell]:
    """방향을 고른 모양의 칸(CreateDirectionMask)."""
    if direction not in supported_directions(shape):
        raise ValueError(f"{shape.name}은(는) {direction} 방향을 받지 않습니다")
    forward = DIRECTIONS[direction]
    if shape in (Shape.CARDINAL_LINE, Shape.DIAGONAL_LINE):
        return _rays([forward], r)
    if shape is Shape.EIGHT_DIRECTION_LINE:   # 정면 + 정면 양옆 대각
        right = (forward[1], -forward[0])
        return _rays([forward, (forward[0] - right[0], forward[1] - right[1]),
                      (forward[0] + right[0], forward[1] + right[1])], r)
    cells = []
    for y in range(-r, r + 1):
        for x in range(-r, r + 1):
            if shape is Shape.FAN:
                include = _in_expanding(x, y, forward, r)
            else:
                include = x * forward[0] + y * forward[1] >= 1
            if include:
                cells.append((x, y))
    return cells


def all_offsets(shape: Shape, r: int) -> list[Cell]:
    """방향 없이 모양 전체(전체 지정)."""
    if shape is Shape.CARDINAL_LINE:
        return _rays(_CARDINAL_STEPS, r)
    if shape is Shape.DIAGONAL_LINE:
        return _rays(_DIAGONAL_STEPS, r)
    if shape is Shape.EIGHT_DIRECTION_LINE:
        return _rays(_EIGHT_STEPS, r)
    return [(x, y) for y in range(-r, r + 1) for x in range(-r, r + 1) if base_contains(shape, r, x, y)]


def splash_offsets(splash: Splash) -> list[Cell]:
    if splash is Splash.NONE:
        return []
    if splash is Splash.CROSS:
        return _rays(_CARDINAL_STEPS, 1)
    if splash is Splash.DIAGONAL:
        return _rays(_DIAGONAL_STEPS, 1)
    return [(x, y) for y in (-1, 0, 1) for x in (-1, 0, 1) if (x, y) != (0, 0)]


def angle_contains(angle: ChoiceAngle, side: Side, x: int, y: int) -> bool:
    """진영의 고정 정면 기준 각도 제한(TargetingAngleCalculator.Contains)."""
    if (x, y) == (0, 0):
        return False
    forward = y if side is Side.PLAYER else -y
    if angle is ChoiceAngle.UNRESTRICTED:
        return True
    if angle is ChoiceAngle.FRONT:
        return forward > 0 and abs(x) <= forward
    if angle is ChoiceAngle.FRONT_AND_SIDES:
        return forward >= 0
    return abs(x) <= abs(forward)
