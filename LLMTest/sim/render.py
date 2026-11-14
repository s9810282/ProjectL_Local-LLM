"""스테이지를 글자 판으로 그린다. 위쪽이 적 본진, 아래쪽이 아군 배치 칸(게임 y가 클수록 위)."""

from __future__ import annotations

from collections import Counter

from .assets import StageDef, TerrainKind

TERRAIN_MARK = {
    TerrainKind.WALL: "#",
    TerrainKind.CLIFF: "v",
    TerrainKind.WEAK_GROUND: "~",
    TerrainKind.HIGH_GROUND: "^",
    TerrainKind.SANDSTORM: "s",
    TerrainKind.TORNADO: "@",
    TerrainKind.PRESENCE_MODIFIER: "+",
}
LEGEND = ("# 벽  v 낭떠러지  ~ 약한 지반  ^ 고지대  s 모래폭풍  @ 토네이도  + 강화 칸  "
          "H 적 본진  = 레일  , 아군 배치 칸  . 빈 칸  a~ 적 유닛")


def _letter(index: int) -> str:
    letters = "abcdefghijklmnopqrstuvwxyz"
    return letters[index] if index < len(letters) else "?"


def stage_problems(stage: StageDef) -> list[str]:
    """한 칸에 두 가지가 겹친 곳 등, 게임이 거절할 수 있는 배치."""
    problems = []
    blocked = {p.cell for p in stage.terrain if p.terrain.kind in (TerrainKind.WALL, TerrainKind.CLIFF)}
    occupied = Counter(enemy.cell for enemy in stage.enemies)
    headquarters = set(stage.headquarters_cells())
    for cell, count in occupied.items():
        if count > 1:
            problems.append(f"적 {count}명이 같은 칸 {cell}")
        if cell in blocked:
            problems.append(f"적이 벽·낭떠러지 칸 {cell}")
        if cell in headquarters:
            problems.append(f"적이 본진 칸 {cell}")
    for cell in headquarters:
        if not (0 <= cell[0] < stage.width and 0 <= cell[1] < stage.height):
            problems.append(f"본진 칸 {cell}이(가) 판 밖")
    terrain_cells = Counter(p.cell for p in stage.terrain)
    problems += [f"지형 {count}개가 같은 칸 {cell}" for cell, count in terrain_cells.items() if count > 1]
    return problems


def usable_deployment_cells(stage: StageDef) -> frozenset:
    """배치 칸 중 벽·낭떠러지가 아닌 칸(CombatPlacementCalculator: 벽·낭떠러지에는 놓을 수 없다)."""
    blocked = {p.cell for p in stage.terrain if p.terrain.kind in (TerrainKind.WALL, TerrainKind.CLIFF)}
    return stage.deployment_cells - blocked


def render_stage(stage: StageDef) -> str:
    grid = {}
    for cell in stage.deployment_cells:
        grid[cell] = ","
    for cell in stage.rail_cells:
        grid[cell] = "="
    for placement in stage.terrain:
        grid[placement.cell] = TERRAIN_MARK[placement.terrain.kind]
    for cell in stage.headquarters_cells():
        grid[cell] = "H"
    for index, enemy in enumerate(stage.enemies):
        grid[enemy.cell] = _letter(index)

    lines = [f"{stage.name}  {stage.width}x{stage.height}  목표={stage.objective.name}  "
             f"적 레벨={stage.enemy_level}  시드={stage.seed}"]
    header = "    " + "".join(str(x % 10) for x in range(stage.width))
    lines.append(header)
    for y in range(stage.height - 1, -1, -1):
        lines.append(f"{y:>3} " + "".join(grid.get((x, y), ".") for x in range(stage.width)))
    lines.append(header)
    lines.append(LEGEND)
    lines.append("")
    lines.append("적 유닛")
    for index, enemy in enumerate(stage.enemies):
        unit = enemy.unit
        traits = ",".join(trait.kind for trait in unit.traits) or "-"
        lines.append(f"  {_letter(index)} {unit.name:<34} {enemy.cell}  HP {unit.max_hp} 공 {unit.attack} "
                     f"방 {unit.defense} 사거리 {unit.attack_range} 이동 {unit.move} {unit.category.name} "
                     f"{unit.targeting.name} 특성 {traits}" + ("  [격파 목표]" if enemy.victory_target else ""))
    if stage.headquarters:
        lines.append(f"적 본진  {stage.headquarters.name} @ {stage.headquarters_anchor} "
                     f"{stage.headquarters.footprint[0]}x{stage.headquarters.footprint[1]} HP {stage.headquarters.max_hp}")
    lines.append(f"아군 배치 칸 {len(stage.deployment_cells)}개 (벽·낭떠러지를 빼면 {len(usable_deployment_cells(stage))}개)")
    if stage.reinforcement:
        r = stage.reinforcement
        pool = ", ".join(f"{unit.name}x{count}" for unit, count in r.pool)
        lines.append(f"적 증원  턴당 {r.per_turn}, 판 상한 {r.field_limit}, 본진 둘레 {r.padding}칸  목록: {pool}")
    else:
        lines.append("적 증원  없음")
    terrain = Counter(p.terrain.kind.name for p in stage.terrain)
    lines.append("지형  " + (", ".join(f"{kind} {count}" for kind, count in sorted(terrain.items())) or "없음"))
    lines.append("기믹  " + (", ".join(sorted(stage.gimmicks)) or "없음"))
    hidden = [p for p in stage.terrain if set(p.known_to) != {1, 2}]
    if hidden:
        lines.append(f"한쪽만 아는 지형 {len(hidden)}개: " + ", ".join(f"{p.cell}{p.known_to}" for p in hidden))
    problems = stage_problems(stage)
    lines.append("배치 문제  " + ("; ".join(problems) if problems else "없음"))
    return "\n".join(lines)
