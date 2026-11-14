"""스테이지 판을 글자로 보여 준다.

사용:
  python LLMTest/sim/show.py                 전투 스테이지 목록
  python LLMTest/sim/show.py Combat_LD_P20   그 판
  python LLMTest/sim/show.py --all           모든 전투 스테이지
"""

from __future__ import annotations

import argparse
from pathlib import Path
import sys

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    from sim.assets import GameAssets
    from sim.render import render_stage
else:
    from .assets import GameAssets
    from .render import render_stage


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="게임 전투 스테이지를 글자 판으로 그린다")
    parser.add_argument("stage", nargs="?", help="전투 스테이지 에셋 이름 (예: Combat_LD_P20)")
    parser.add_argument("--all", action="store_true", help="모든 전투 스테이지")
    args = parser.parse_args(argv)
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    assets = GameAssets()
    names = assets.combat_stage_names()
    if args.all:
        print("\n\n".join(render_stage(assets.stage(name)) for name in names))
    elif args.stage:
        print(render_stage(assets.stage(args.stage)))
    else:
        print("\n".join(names))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
