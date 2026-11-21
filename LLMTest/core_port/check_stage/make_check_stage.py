"""계획 AI 재생 확인용 스테이지(Combat_Research_AiCheck)를 만든다.

무엇을 보려는 판인가는 LLMTest/docs/2026-11-19-research-planner-value-fix-port-design.md §8.6.
만드는 것: 전투 스테이지·스테이지 루트·View 배경 에셋(+ .meta)과 표 두 곳(StageRootDataTable, StageViewPropDataTable)의 등록 줄.
틀은 Combat_LD_P30(15x15, 레일·기믹 없음)이다. 배치만 바꾼다. 다시 돌리면 같은 GUID로 덮어쓴다(표 줄은 한 번만 넣는다).

사용(저장소 루트에서): python -I LLMTest/core_port/check_stage/make_check_stage.py [--remove]
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
DB = ROOT / "Assets" / "ProjectL" / "DataBaseSO"
COMBAT = DB / "InGameGroup" / "Stage" / "Combat"
STAGE_ROOT = DB / "InGameGroup" / "Stage" / "Root"
VIEW = DB / "ViewGroup" / "StageViewProp" / "Item"
ROOT_TABLE = DB / "InGameGroup" / "Stage" / "StageRootDataTable.asset"
VIEW_TABLE = DB / "ViewGroup" / "StageViewProp" / "StageViewPropDataTable.asset"

NAME = "Research_AiCheck"
TEMPLATE = "LD_P30"
STAGE_KEY = 10010
# 한 번 정한 GUID(다시 만들어도 참조가 안 깨지게 고정한다)
GUID_COMBAT = "7a1c5e0d9b3f4c62a8d4e1f07b2c9a31"
GUID_ROOT = "3e9b7d21c4a64f58b1d0a6c3e8f57b42"
GUID_VIEW = "c2f4a86e1d7b4a09b3e5d9f1a0c6b753"

DEMON = "8691f0aecbdb41d79c20b0675842d0d8"      # Unit_Demon (HP 20, 공 8, 사거리 1, 이동 3)
WALL = "53257e8874554d73a75d8ca177f14c4c"       # Terrain_Wall

# ---------------------------------------------------------------- 배치 (x, y). y = 0이 플레이어 쪽
BAIT = [(11, 5), (13, 5)]                                        # 미끼 구역 앞의 적 둘
SWARM = [(1, 12), (2, 12), (3, 12), (1, 11), (2, 11)]            # 본대로 몰려오는 무리 다섯
SEALED = [(12, 13), (13, 13)]                                    # 벽으로 막힌 방 안의 적 둘
ENEMIES = BAIT + SWARM + SEALED
HEADQUARTERS = (6, 13)                                           # 2x2
REAR_POCKET = [(x, y) for y in range(3) for x in range(0, 4)]    # 본대 배치 칸(왼쪽 아래)
BAIT_POCKET = [(x, y) for y in range(3) for x in range(11, 14)]  # 미끼 배치 칸(오른쪽 아래)
DEPLOYMENT = sorted(REAR_POCKET + BAIT_POCKET, key=lambda c: (c[1], c[0]))
DIVIDER = [(7, y) for y in range(0, 7)]                          # 두 배치 구역 사이의 벽(돌아가는 길을 길게)
ROOM = [(10, y) for y in range(11, 15)] + [(x, 11) for x in range(11, 15)]   # 막힌 방의 벽(안쪽 x 11~14, y 12~14)
WALLS = sorted(DIVIDER + ROOM, key=lambda c: (c[1], c[0]))


def read(path: Path) -> str:
    return path.read_bytes().decode("utf-8").replace("\r\n", "\n")


def write(path: Path, text: str) -> None:
    path.write_bytes(text.replace("\n", "\r\n").encode("utf-8"))


def replace_block(text: str, key: str, block: str) -> str:
    """맨 위 칸(들여쓰기 2칸) key의 줄부터 다음 맨 위 칸 전까지를 block으로 바꾼다."""
    pattern = re.compile(rf"^  {re.escape(key)}:.*?(?=^  [A-Za-z_])", re.S | re.M)
    assert len(pattern.findall(text)) == 1, key
    return pattern.sub(lambda _: block, text)


def meta(guid: str) -> str:
    return (f"fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n"
            "  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def table_line(guid: str) -> str:
    return f"  - {{fileID: 11400000, guid: {guid}, type: 2}}\n"


def set_table(path: Path, guid: str, present: bool) -> None:
    text = read(path)
    line = table_line(guid)
    if present and line not in text:
        assert text.endswith("\n")
        text += line
    if not present:
        text = text.replace(line, "")
    write(path, text)


def build() -> None:
    assert len(set(ENEMIES)) == len(ENEMIES) and not set(ENEMIES) & set(WALLS)
    assert not set(DEPLOYMENT) & set(WALLS)

    # 전투 스테이지
    text = read(COMBAT / f"Combat_{TEMPLATE}.asset")
    text = text.replace(f"m_Name: Combat_{TEMPLATE}", f"m_Name: Combat_{NAME}")
    enemies = "  initialEnemyPlacements:\n" + "".join(
        f"  - unitDefinition: {{fileID: 11400000, guid: {DEMON}, type: 2}}\n"
        f"    anchor:\n      x: {x}\n      y: {y}\n    rotation: 0\n    isVictoryTarget: 0\n    startingItem: {{fileID: 0}}\n"
        for x, y in ENEMIES)
    text = replace_block(text, "initialEnemyPlacements", enemies)
    assert text.count("  nexusReinforcements:\n    enabled: 1\n") == 1
    text = text.replace("  nexusReinforcements:\n    enabled: 1\n", "  nexusReinforcements:\n    enabled: 0\n")
    cells = "  playerDeploymentCells:\n" + "".join(f"  - x: {x}\n    y: {y}\n" for x, y in DEPLOYMENT)
    text = replace_block(text, "playerDeploymentCells", cells)
    text = replace_block(text, "enemyHeadquartersAnchor",
                         f"  enemyHeadquartersAnchor:\n    x: {HEADQUARTERS[0]}\n    y: {HEADQUARTERS[1]}\n")
    terrain = "  environment:\n    terrainPlacements:\n" + "".join(
        f"    - definition: {{fileID: 11400000, guid: {WALL}, type: 2}}\n      sourceId: {index}\n"
        f"      cell:\n        x: {x}\n        y: {y}\n      knownTo: 0100000002000000\n"
        "      randomizeInitialPosition: 0\n      tornadoInitialHorizontalSign: 0\n"
        for index, (x, y) in enumerate(WALLS, start=1))
    terrain += "    obstaclePlacements: []\n    objectPlacements: []\n"
    text = replace_block(text, "environment", terrain)
    # 기믹은 모두 끈다(틀 P30은 낙석이 켜져 있다). 확인하려는 것만 남긴다.
    for gimmick in ("rockfall", "haboob"):
        text, count = re.subn(rf"^  {gimmick}:\n    enabled: \d\n", f"  {gimmick}:\n    enabled: 0\n", text, flags=re.M)
        assert count == 1, gimmick
    assert "  railwayEnabled: 0\n" in text
    write(COMBAT / f"Combat_{NAME}.asset", text)
    write(COMBAT / f"Combat_{NAME}.asset.meta", meta(GUID_COMBAT))

    # 스테이지 루트(일반 전투, 맵 1)
    root = read(STAGE_ROOT / f"Stage_{TEMPLATE}.asset")
    root = root.replace(f"m_Name: Stage_{TEMPLATE}", f"m_Name: Stage_{NAME}")
    root, count = re.subn(r"stageKey: \d+", f"stageKey: {STAGE_KEY}", root)
    assert count == 1 and "mapKey: 1\n" in root and "mapRole: 0\n" in root
    root, count = re.subn(r"stageData: \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}",
                          f"stageData: {{fileID: 11400000, guid: {GUID_COMBAT}, type: 2}}", root)
    assert count == 1
    write(STAGE_ROOT / f"Stage_{NAME}.asset", root)
    write(STAGE_ROOT / f"Stage_{NAME}.asset.meta", meta(GUID_ROOT))

    # View 배경(틀과 같은 배경·타일, 스테이지 키만 다르다)
    view = read(VIEW / f"StageViewProp_{TEMPLATE}.asset")
    view = view.replace(f"m_Name: StageViewProp_{TEMPLATE}", f"m_Name: StageViewProp_{NAME}")
    view, count = re.subn(r"stageKey: \d+", f"stageKey: {STAGE_KEY}", view)
    assert count == 1
    assert "  terrainRotations: []\n" in view and "  objectRotations: []\n" in view, "틀의 View에 외형 회전이 있습니다"
    write(VIEW / f"StageViewProp_{NAME}.asset", view)
    write(VIEW / f"StageViewProp_{NAME}.asset.meta", meta(GUID_VIEW))

    set_table(ROOT_TABLE, GUID_ROOT, True)
    set_table(VIEW_TABLE, GUID_VIEW, True)
    print(f"만듦: Combat_{NAME}, Stage_{NAME}(키 {STAGE_KEY}), StageViewProp_{NAME} + 표 등록 둘")


def remove() -> None:
    for path in (COMBAT / f"Combat_{NAME}.asset", STAGE_ROOT / f"Stage_{NAME}.asset", VIEW / f"StageViewProp_{NAME}.asset"):
        for target in (path, path.with_name(path.name + ".meta")):
            if target.exists():
                target.unlink()
    set_table(ROOT_TABLE, GUID_ROOT, False)
    set_table(VIEW_TABLE, GUID_VIEW, False)
    print(f"지움: {NAME} 에셋 셋(+ .meta)과 표 등록 둘")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    remove() if "--remove" in sys.argv[1:] else build()
