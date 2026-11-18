"""
계획 AI가 부른 Core 함수 안의 할당·시간과 우리 코드의 몫을 나눠 재는 측정 전용 사본을 만든다
(LLMTest/docs/2026-11-18-planner-background-parallel-design.md §16).

Assets/ResearchEnemyPlanner/*.cs를 LLMTest/core_port/probe/planner/로 복사하고, Core 호출을
AllocProbe.E(slot, AllocProbe.B(), <호출>)로 감싼다. 저장소의 계획 AI 파일은 건드리지 않는다.
저장소 루트에서: python -I LLMTest/core_port/probe/instrument.py
"""
import re
import shutil
import sys
import pathlib

RECV = r"\b[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*"
SLOTS = [
    ("정산", [r"CombatCommandCalculator\.AdvancePrepared\(", r"CombatCommandCalculator\.Calculate\("]),
    ("다음 판 만들기", [r"CombatCommandComposition\.Project\("]),
    ("공격 범위·선택 조회", [r"CombatCommandCalculator\.GetAttackRange\(", r"CombatAttackQueries\.Selection\(",
                       r"CombatAttackQueries\.SelectionRange\("]),
    ("도달 표의 공격 칸·이동 칸", [r"CombatAttackQueries\.RangeCells\(", r"CombatMovementCalculator\.UnitMoveCells\(",
                          r"_engine\.EvaluateAttackTargeting\(", r"CombatCommandEffects\.Capture\(",
                          r"AppointShapeCalculator\.CreateSplashOffsets\("]),
    ("이동 범위 탐색", [r"MovementRangeSearch\.Calculate\(", r"\bsearch\.BuildRoute\(",
                   r"CombatActionAdmission\.MovementAllowance\("]),
    ("예상 피해", [r"CombatAttackCalculator\.CommonAttackDamage\(", r"CombatDamageCalculator\.Attack\("]),
    ("공격 프로필", [r"CombatCommandCalculator\.RequiredProfile\("]),
    ("입력 복사(행위자·의도·판)", [RECV + r"\.WithActor\(", RECV + r"\.WithWorld\(", r"\bstate\.With\(",
                           r"\bnew CombatCommandInput\(", r"CombatCommandComposition\.Intent\("]),
    ("환경 시점 바꾸기", [RECV + r"\.VisibleTo\("]),
    ("작은 규칙 판정", [r"UnitBehaviorRules\.\w+\(", r"CombatActionAdmission\.(?:IsMovementRestricted|IsAttackRestricted)\(",
                    r"EnvironmentTerrainRules\.BlocksVoluntaryEntry\(", r"CombatMovementCalculator\.MoveTo\("]),
]

BACKSLASH = chr(92)


def close_paren(text, open_at):
    depth, i, n = 0, open_at, len(text)
    while i < n:
        ch = text[i]
        if ch in ('"', "'"):
            i += 1
            while text[i] != ch:
                i += 2 if text[i] == BACKSLASH else 1
        elif ch == '(':
            depth += 1
        elif ch == ')':
            depth -= 1
            if depth == 0:
                return i + 1
        i += 1
    raise ValueError("unbalanced")


def instrument(text):
    comments = [(m.start(), m.end()) for m in re.finditer(r"//[^\n]*", text)]

    def in_comment(p):
        return any(a <= p < b for a, b in comments)

    spans = set()
    for slot, (_, patterns) in enumerate(SLOTS):
        for pattern in patterns:
            for m in re.finditer(pattern, text):
                start = m.start()
                if in_comment(start) or (start > 0 and text[start - 1] == '.'):
                    continue
                spans.add((start, close_paren(text, m.end() - 1), slot))
    inserts = []
    for start, end, slot in spans:
        inserts.append((start, 1, f"AllocProbe.E({slot}, AllocProbe.B(), "))
        inserts.append((end, 0, ")"))
    # 높은 자리부터 넣는다. 같은 자리면 닫는 괄호를 먼저 넣어 여는 쪽이 그 앞에 오게 한다.
    for pos, _, s in sorted(inserts, key=lambda x: (x[0], -x[1]), reverse=True):
        text = text[:pos] + s + text[pos:]
    return text, len(spans)


here = pathlib.Path(__file__).resolve().parent
source = here.parents[2] / "Assets" / "ResearchEnemyPlanner"
root = here / "planner"
if root.exists():
    shutil.rmtree(root)
root.mkdir()
for f in source.glob("*.cs"):
    shutil.copy(f, root / f.name)
shutil.copy(here / "AllocProbe.cs", root / "AllocProbe.cs")
shutil.copy(here / "DamageLog.cs", root / "DamageLog.cs")
# --damage-log: 예상 피해마다 질문과 답을 DamageLog에 적는다(되풀이 비율·어긋남 세기, 병렬 설계 §17). 시간·할당 측정에는 쓰지 않는다.
damage_log = "--damage-log" in sys.argv
# --move-memo: 빠른 이동 점수의 MoveTo를 DamageLog.MoveTo(같은 유닛 객체·같은 칸이면 같은 사본)로 바꾼다(기억 설계 DM3 측정).
move_memo = "--move-memo" in sys.argv
total = 0
for f in sorted(root.glob("*.cs")):
    if f.name in ("AllocProbe.cs", "DamageLog.cs"):
        continue
    src = f.read_text(encoding="utf-8-sig")
    if move_memo and f.name == "PlannerEvaluator.cs":
        anchor = "var moved = CombatMovementCalculator.MoveTo(unit, destination);"
        if src.count(anchor) != 1:
            raise SystemExit("QuickMoveScore의 MoveTo 줄을 찾지 못했습니다(계획 AI 코드가 바뀜).")
        src = src.replace(anchor, "var moved = DamageLog.MoveTo(unit, destination);")
    out, n = instrument(src)
    if f.name == "PlannerBoard.cs":
        out = out.replace("Failures++;", "Failures++; AllocProbe.Unwind();", 1)
    if damage_log and f.name == "PlannerEvaluator.cs":
        anchor = "return Math.Max(0, result.FinalDamage);"
        if out.count(anchor) != 1:
            raise SystemExit("PlannerEvaluator.Estimate의 반환 줄을 찾지 못했습니다(계획 AI 코드가 바뀜).")
        out = out.replace(anchor, "DamageLog.Record(board, attacker, target, damage, role, percent, result.FinalDamage); " + anchor)
    f.write_text(out, encoding="utf-8")
    print(f"{f.name}: {n}")
    total += n
print("total", total)
sys.stdout.reconfigure(encoding="utf-8")
print("slots:", "|".join(s for s, _ in SLOTS))
