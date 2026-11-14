"""게임 적 AI(교환 모형 Utility) 점수식을 그대로 옮긴 평가기 rule-game.

원본(Project_L develop 424242ab8):
  - 점수식: Core .../Application/Combat/EnemyAi/EnemyUtilityScorer.cs
  - 대기 비교·치명 걸러내기: 같은 폴더 EnemyDecisionSession.cs (SelectUtility, OptionSteps, PositionFacts)
  - 위협 필드: 같은 폴더 EnemyThreatField.cs (플레이어 현재 자리에서 칸별 최대 단일 공격)

입력은 두 파일을 합친다. 한 사실은 한 곳에만 둔다.
  - 공통 사실: rule_fixtures_v2/F*.json (rule-v3과 같은 상황 파일, 바꾸지 않는다)
  - 게임 전용 사실: rule_fixtures_game/F*.game.json (교전 비용, 다음 턴 공격 대상, 대기 사실 등)

사용법:
  python LLMTest/tools/rule_game.py               # 모든 덧붙임 파일, 실제 게임 값(asset)
  python LLMTest/tools/rule_game.py F02           # 상황 하나
  python LLMTest/tools/rule_game.py --profile codeDefault
매 실행은 results/rule_game/<UTC 시각>/ 을 새로 만든다.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from rule_policy import PolicyInputError, evaluate_case, load_json

ROOT = Path(__file__).resolve().parents[1]

# ── 게임 코드 상수(EnemyUtilityScorer.cs, EnemyAiTuning.cs). 조정 에셋이 바꾸지 않는 값이다. ──
OPPOSING_LOSS_WEIGHT = 1              # 상대 HP 손실 천분율 1당
OPPOSING_ELIMINATION_WEIGHT = 2000    # 상대 처치 1당
ALLIED_LOSS_WEIGHT = -3               # 우리 편(행위자 포함) HP 손실 천분율 1당
ALLIED_ELIMINATION_WEIGHT = -4000     # 우리 편(행위자 포함) 사망 1당
RESTRICTION_WEIGHT = 1
RESTRICTION_UNIT_PERMILLE = 500       # 행동 제약(공격 불가·이동 불가) 하나
HAZARD_WEIGHT = -4                    # 기믹 피해 천분율 1당
ENGAGEMENT_STEP_WEIGHT = 20           # 교전 거리 1칸 줄 때마다
ENGAGEMENT_POINT_LIMIT = 100          # 접근 점수 상한(±)
FOLLOWUP_WEIGHT = 1                   # 이동 뒤 공격 이득

KINDS = ("wait", "move", "attack", "move_attack")
PROFILE_KEYS = ("deathTradeThresholdPercent", "threatAversionPercent", "nextTurnApproachPercent")
TERM_KEYS = ("attack", "hazard", "playerRisk", "progress", "followup", "nextTurn", "bind")
HOLD_ID = "(hold)"   # 대기 후보가 없는 상황에서 "아무것도 안 함"을 뜻하는 이름


# ───────────────────────────── 설정 ─────────────────────────────

def load_weights(path=ROOT / "config" / "rule_game_weights.json", profile=None):
    """설정 파일에서 조정값 셋을 읽어 점수식이 쓰는 모양(EnemyScoreWeights)으로 바꾼다."""
    settings = load_json(path)
    name = profile or settings["defaultProfile"]
    if name not in settings["profiles"]:
        raise PolicyInputError(f"profile: unknown {name}")
    raw = settings["profiles"][name]
    for key in PROFILE_KEYS:
        if type(raw.get(key)) is not int or raw[key] < 0:
            raise PolicyInputError(f"profiles.{name}.{key}: non-negative integer required")
    return {
        "version": settings["version"], "profile": name,
        "deathTradePermille": raw["deathTradeThresholdPercent"] * 10,   # 백분율 × 10 = 천분율
        "threatPercent": raw["threatAversionPercent"],
        "nextTurnPercent": raw["nextTurnApproachPercent"],
    }


# ─────────────────────────── 계산 도우미 ───────────────────────────

def loss_permille(loss, remaining, maximum):
    """EnemyUtilityScorer.LossPermille: 남은 HP를 넘지 않는 손실을 최대 HP 천분율로. 정수 나눗셈."""
    return 0 if maximum <= 0 else max(0, min(loss, remaining)) * 1000 // maximum


def _int(value, path, minimum=0):
    if type(value) is not int or value < minimum:
        raise PolicyInputError(f"{path}: integer >= {minimum} required")
    return value


def _changes(players, allies, actor, hq, before_self, after_self, player_after, ally_after, hq_after):
    """CombatActionAssessment가 세는 방식: 상대(플레이어 유닛)와 우리 편(행위자·다른 적·적 본진)의 손실·사망."""
    opponent_loss = opponent_kills = allied_loss = allied_kills = 0
    for key, record in players.items():
        lost = max(0, record["hp"] - player_after[key])
        opponent_loss += lost * 1000 // record["maxHp"]
        opponent_kills += record["hp"] > 0 and player_after[key] == 0
    allied_loss += max(0, before_self - after_self) * 1000 // actor["maxHp"]
    allied_kills += before_self > 0 and after_self == 0
    for key, record in allies.items():
        lost = max(0, record["hp"] - ally_after[key])
        allied_loss += lost * 1000 // record["maxHp"]
        allied_kills += record["hp"] > 0 and ally_after[key] == 0
    # 상황 파일의 headquarters는 적 본진이다(2026-10-15 정책 설계). 게임 식으로는 우리 편 구조물이다.
    allied_loss += max(0, hq["hp"] - hq_after) * 1000 // hq["maxHp"]
    allied_kills += hq["hp"] > 0 and hq_after == 0
    return {"opponentLoss": opponent_loss, "opponentKills": opponent_kills,
            "alliedLoss": allied_loss, "alliedKills": allied_kills}


def attack_benefit(change, restriction=0):
    """EnemyUtilityScorer.AttackBenefit."""
    return (OPPOSING_LOSS_WEIGHT * change["opponentLoss"] + OPPOSING_ELIMINATION_WEIGHT * change["opponentKills"]
            + ALLIED_LOSS_WEIGHT * change["alliedLoss"] + ALLIED_ELIMINATION_WEIGHT * change["alliedKills"]
            + RESTRICTION_WEIGHT * RESTRICTION_UNIT_PERMILLE * restriction)


def _target_value(players, next_turn, path):
    """EnemyEngagementField.ValueOf: 결정 시작 때 HP 기준. 처치면 처치 가중치를 더한다."""
    if next_turn is None:
        return 0
    target = next_turn.get("targetId")
    damage = _int(next_turn.get("damage"), f"{path}.nextTurn.damage", 1)
    if target not in players or players[target]["hp"] == 0:
        raise PolicyInputError(f"{path}.nextTurn.targetId: living player required")
    record = players[target]
    value = loss_permille(damage, record["hp"], record["maxHp"])
    return value + OPPOSING_ELIMINATION_WEIGHT if damage >= record["hp"] else value


def terms(facts, weights):
    """EnemyUtilityScorer.Terms. 모든 값은 점수에 더하는 부호 그대로다."""
    risk = (weights["deathTradePermille"] if facts["threatLethal"]
            else weights["threatPercent"] * facts["threatPermille"] // 100)
    value = {
        "attack": facts["attackBenefit"],
        "hazard": HAZARD_WEIGHT * facts["hazardLoss"],
        "playerRisk": -risk,
        "progress": max(-ENGAGEMENT_POINT_LIMIT, min(ENGAGEMENT_POINT_LIMIT, ENGAGEMENT_STEP_WEIGHT * facts["progress"])),
        "followup": FOLLOWUP_WEIGHT * max(0, facts["followup"]),
        "nextTurn": weights["nextTurnPercent"] * max(0, facts["nextTurnStrike"]) // 100,
        "bind": -RESTRICTION_WEIGHT * RESTRICTION_UNIT_PERMILLE if facts["bind"] else 0,
    }
    value["total"] = sum(value[key] for key in TERM_KEYS)
    return value


# ─────────────────────────── 위치 사실 ───────────────────────────

def _position(players, actor, position_hp, threats, hazard, progress, followup, next_turn_value, bind, terminal):
    """EnemyDecisionSession.PositionFacts: 행동 뒤 종점에서 보는 위험·접근·다음 턴 값."""
    if terminal or position_hp == 0:
        # 승패가 확정됐거나 행위자가 죽었으면 이후 경계를 보지 않는다.
        return {"positionHp": position_hp, "hazardLoss": 0, "threatDamage": 0, "threatPermille": 0,
                "threatLethal": False, "progress": 0, "followup": 0, "nextTurnStrike": 0, "bind": False}
    # 위협 필드는 플레이어 "현재 자리"에서 칠 수 있는 공격만, 칸마다 가장 센 한 방만 남긴다.
    damage = max((item["damage"] for item in threats), default=0)
    return {"positionHp": position_hp,
            "hazardLoss": loss_permille(hazard, position_hp, actor["maxHp"]),
            "threatDamage": damage,
            "threatPermille": loss_permille(damage, position_hp, actor["maxHp"]),
            "threatLethal": damage > 0 and damage >= position_hp,
            "progress": progress, "followup": followup, "nextTurnStrike": next_turn_value, "bind": bind}


def _in_place_threats(candidate, players, extra, path):
    """상황 파일의 제자리 위협 + 이번 행동으로 죽은 플레이어의 위협(게임 위협 필드에는 남는다)."""
    result = []
    for item in candidate["exposure"]["threats"]:
        if item["reach"] == "in_place" and players[item["opponentId"]]["hp"] > 0:
            result.append({"opponentId": item["opponentId"], "damage": _int(item["damage"], f"{path}.threat.damage")})
    for item in extra:
        key = item.get("opponentId")
        if key not in players or players[key]["hp"] == 0 or candidate["playerHpAfter"][key] != 0:
            raise PolicyInputError(f"{path}.killedThreats: opponent killed by this candidate required")
        result.append({"opponentId": key, "damage": _int(item.get("damage"), f"{path}.killedThreats.damage", 1)})
    return result


# ─────────────────────────── 후보 평가 ───────────────────────────

def _outcome(players, main_id, hq_after, player_after):
    """rule-v3과 같은 종료 판정(메인 사망 우선): 적 승리 / 적 패배 / 진행."""
    if player_after[main_id] == 0:
        return "enemy_win"
    return "enemy_loss" if hq_after == 0 else None


def _evaluate_candidate(case, overlay_entry, players, allies, actor, hq, main_id, before_cost, weights):
    candidate_id = case["id"]
    path = f"candidate {candidate_id}"
    kind = case.get("kind")
    if kind not in KINDS:
        raise PolicyInputError(f"{path}.kind: wait/move/attack/move_attack required")
    entry = overlay_entry or {}
    moving = kind in ("move", "move_attack")
    if moving:
        after_cost = entry.get("engagementCostAfter")
        if after_cost is not None:
            _int(after_cost, f"{path}.engagementCostAfter")
        progress = 0 if before_cost is None or after_cost is None else before_cost - after_cost
    else:
        progress = 0   # 제자리 공격·대기는 종점이 출발 칸이다
    if "nextTurn" not in entry:
        raise PolicyInputError(f"{path}.nextTurn: required in overlay (null allowed)")
    next_turn_value = _target_value(players, entry["nextTurn"], path)
    restriction = entry.get("restrictionDelta", 0)   # 상대 제약 +1, 우리 편 제약 −1(개수)
    if type(restriction) is not int:
        raise PolicyInputError(f"{path}.restrictionDelta: integer required")
    bind = entry.get("forecastBind", False) is True

    hp = actor["hp"]
    final_hp = case["selfHpAfter"]
    notes = []
    if kind == "move_attack":
        # 게임은 "이동"을 고르고 그 종점에서의 공격을 후속 공격 값으로 미리 더한다. 공격은 다음 결정에서 실제로 한다.
        moved_hp = _int(entry.get("selfHpAfterMove", hp), f"{path}.selfHpAfterMove")
        unchanged_players = {key: players[key]["hp"] for key in players}
        unchanged_allies = {key: allies[key]["hp"] for key in allies}
        move_change = _changes(players, allies, actor, hq, hp, moved_hp, unchanged_players, unchanged_allies, hq["hp"])
        follow_change = _changes(players, allies, actor, hq, moved_hp, final_hp,
                                 case["playerHpAfter"], case["allyHpAfter"], case["headquartersHpAfter"])
        followup = attack_benefit(follow_change, restriction) if moved_hp > 0 else 0
        attack = attack_benefit(move_change)
        position_hp = moved_hp
        terminal = None   # 이동 자체는 전투를 끝내지 않는다.
        notes.append("move_attack → 이동 + 후속 공격 값")
    else:
        change = _changes(players, allies, actor, hq, hp, final_hp,
                          case["playerHpAfter"], case["allyHpAfter"], case["headquartersHpAfter"])
        attack = attack_benefit(change, restriction)
        followup = 0
        position_hp = final_hp
        terminal = _outcome(players, main_id, case["headquartersHpAfter"], case["playerHpAfter"])
    threats = _in_place_threats(case, players, entry.get("killedThreats", []), path)
    hazard = _int(case["exposure"]["hazardDamage"], f"{path}.hazardDamage")
    # 승패 등급(GuaranteedTerminal)은 평가에 불확실성이 하나도 없을 때만 붙는다. 그런데 모든 평가가 HiddenEnvironment를
    # 늘 달고 시작해서(CombatActionAssessment.cs:99, EnemyDecisionSession.cs:182) 실제로는 붙지 않는다(2026-11-13 확인).
    # 그래서 승패가 나는 행동도 등급 없이 점수로만 보고, 위치 항목도 그대로 본다. "적 승리면 치명 아님" 예외도 생기지 않는다.
    facts = {"attackBenefit": attack, **_position(players, actor, position_hp, threats, hazard, progress, followup,
                                                   next_turn_value, bind, None)}
    if position_hp == 0:
        lethal, lethal_rank = True, (True, sys.maxsize, sys.maxsize)
    else:
        lethal = hazard >= position_hp
        lethal_rank = (lethal, hazard, hazard)
    tier = 0
    if terminal:
        notes.append(f"이 행동으로 전투가 끝나지만({terminal}) 게임은 승패 등급을 주지 않는다")
    return {"id": candidate_id, "label": case["label"], "kind": kind,
            "gameAction": "hold" if kind == "wait" else "attack" if kind == "attack" else "move",
            "eligible": True, "reason": "", "facts": facts, "terms": terms(facts, weights), "tier": tier,
            "lethal": lethal, "lethalRank": lethal_rank, "notes": notes}


def _hold_from_overlay(raw, players, actor, weights):
    """대기 후보가 없는 상황의 대기 사실(덧붙임 파일의 hold)."""
    path = "hold"
    threats = []
    for item in raw.get("inPlaceThreats", []):
        key = item.get("opponentId")
        if key not in players:
            raise PolicyInputError(f"{path}.inPlaceThreats: unknown opponent {key}")
        if players[key]["hp"] > 0:
            threats.append({"opponentId": key, "damage": _int(item.get("damage"), f"{path}.damage", 1)})
    hazard = _int(raw.get("hazardDamage", 0), f"{path}.hazardDamage")
    if "nextTurn" not in raw:
        raise PolicyInputError(f"{path}.nextTurn: required (null allowed)")
    facts = {"attackBenefit": 0, **_position(players, actor, actor["hp"], threats, hazard, 0, 0,
                                             _target_value(players, raw["nextTurn"], path), False, None)}
    lethal = hazard >= actor["hp"]
    return {"id": HOLD_ID, "label": "대기(덧붙임 파일)", "kind": "wait", "gameAction": "hold", "eligible": True,
            "reason": "", "facts": facts, "terms": terms(facts, weights), "tier": 0, "lethal": lethal,
            "lethalRank": (lethal, hazard, hazard), "notes": []}


def _rank(item):
    return (item["tier"], item["terms"]["total"])


def _intent_key(item):
    """EnemyUtilityScorer.CompareIntent: 공격이 이동보다 앞. 상황 파일에는 좌표가 없어 그다음은 후보 ID로 대신한다."""
    return (0 if item["gameAction"] == "attack" else 1, item["id"])


def evaluate_game_case(base, overlay, weights):
    """기대 선택을 보지 않고 게임 AI가 이 행위자로 무엇을 고르는지와 항목별 점수를 돌려준다."""
    if base.get("schemaVersion") != 2:
        raise PolicyInputError("base.schemaVersion: 2 required")
    if overlay.get("schema") != "rule-game-overlay-1" or overlay.get("situationId") != base.get("situationId"):
        raise PolicyInputError("overlay: schema rule-game-overlay-1 and matching situationId required")
    actor = base["self"]
    players = {item["id"]: item for item in base["players"]}
    allies = {item["id"]: item for item in base["allies"]}
    hq = base["headquarters"]
    main_id = next(key for key, item in players.items() if item["role"] == "main")
    before_cost = overlay.get("engagementCostBefore")
    if before_cost is not None:
        _int(before_cost, "engagementCostBefore")
    entries = overlay.get("candidates", {})
    known = {item["id"] for item in base["candidates"]}
    if set(entries) - known:
        raise PolicyInputError("overlay.candidates: unknown id " + ", ".join(sorted(set(entries) - known)))

    results = []
    for case in base["candidates"]:
        if not case.get("legal", False):
            results.append({"id": case["id"], "label": case["label"], "kind": case.get("kind"), "eligible": False,
                            "reason": "declared_illegal"})
            continue
        if case["moveSteps"] > actor["moveRange"]:
            results.append({"id": case["id"], "label": case["label"], "kind": case.get("kind"), "eligible": False,
                            "reason": "move_out_of_range"})
            continue
        results.append(_evaluate_candidate(case, entries.get(case["id"]), players, allies, actor, hq, main_id,
                                           before_cost, weights))
    eligible = [item for item in results if item["eligible"]]
    waits = [item for item in eligible if item["kind"] == "wait"]
    if len(waits) > 1:
        raise PolicyInputError("candidates: at most one wait candidate")
    if waits:
        if "hold" in overlay:
            raise PolicyInputError("overlay.hold: not allowed when the base has a wait candidate")
        hold = waits[0]
    elif "hold" in overlay:
        hold = _hold_from_overlay(overlay["hold"], players, actor, weights)
    else:
        raise PolicyInputError("overlay.hold: required when the base has no wait candidate")

    # ── EnemyDecisionSession.SelectUtility의 선택 규칙 ──
    options = [item for item in eligible if item["kind"] != "wait"]
    hold_rank, hold_lethal = _rank(hold), hold["lethal"]
    any_safe = any(not item["lethal"] for item in options)
    ranked = []
    if any_safe:
        # 안전한 후보가 있으면 치명 후보는 모두 뺀다. 남은 후보는 대기보다 "확실히" 높아야 한다(제자리가 치명이면 비교하지 않음).
        ranked = [item for item in options if not item["lethal"] and (hold_lethal or _rank(item) > hold_rank)]
    elif options and hold_lethal:
        # 제자리와 모든 후보가 치명이면 가장 덜 치명적인 후보만 남겨 대기와 비교한다.
        least = min(item["lethalRank"] for item in options)
        ranked = [item for item in options if item["lethalRank"] == least and _rank(item) > hold_rank]
    admitted = {item["id"] for item in ranked}
    for item in options:
        item["beatsHold"] = _rank(item) > hold_rank
        item["admitted"] = item["id"] in admitted
    if ranked:
        top = max(_rank(item) for item in ranked)
        tied = sorted((item for item in ranked if _rank(item) == top), key=_intent_key)
        chosen, tie = tied[0], len(tied) > 1
        reason = "selected"
    else:
        chosen, tie = hold, False
        reason = ("lethal_only" if options and not any_safe else
                  "no_option" if not options else "exchange_loss")
    approximations = []
    moves = [item for item in options if item["gameAction"] == "move"]
    if len(moves) > 2:
        approximations.append("이동 후보가 3개 이상: 게임은 상위 2개만 정밀 평가한다(여기서는 모두 정밀로 봄)")
    if tie:
        approximations.append("점수 동점: 게임은 칸 좌표로 정하지만 여기서는 후보 ID로 정했다")
    return {"situationId": base["situationId"], "title": base["title"], "profile": weights["profile"],
            "choice": chosen["id"], "choiceAction": chosen["gameAction"], "holdReason": reason,
            "hold": hold, "candidates": results, "approximations": approximations,
            "assumptions": overlay.get("assumptions", [])}


# ─────────────────────────── 실행 ───────────────────────────

def load_pair(overlay_path):
    overlay = load_json(overlay_path)
    base_path = ROOT / overlay["base"]
    return load_json(base_path), overlay, base_path


def _sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


CSV_COLUMNS = ["situation", "candidate", "kind", "gameAction", "eligible", "lethal", "tier", *TERM_KEYS, "total",
               "beatsHold", "chosen", "ruleV3Victory", "expectedVictory"]


def main():
    parser = argparse.ArgumentParser(description="게임 적 AI 점수식(rule-game)으로 상황을 평가한다.")
    parser.add_argument("case", nargs="?", help="상황 ID(예: F02). 없으면 모든 덧붙임 파일")
    parser.add_argument("--profile", help="config/rule_game_weights.json의 프로필 이름(기본: defaultProfile)")
    parser.add_argument("--output", type=Path, default=ROOT / "results" / "rule_game")
    args = parser.parse_args()

    weights = load_weights(profile=args.profile)
    v3_settings = load_json(ROOT / "config" / "rule_weights_v3.json")
    paths = sorted((ROOT / "rule_fixtures_game").glob("*.game.json"))
    if args.case:
        paths = [p for p in paths if p.name.split(".")[0].casefold() == args.case.casefold()]
    if not paths:
        print(f"덧붙임 파일을 찾지 못했습니다: {args.case or 'rule_fixtures_game/*.game.json'}", file=sys.stderr)
        return 2

    started = datetime.now(timezone.utc)
    run_dir = args.output / started.strftime("%Y%m%dT%H%M%S.%fZ")
    run_dir.mkdir(parents=True)
    print(f"rule-game {weights['version']} | profile {weights['profile']} | "
          f"목숨값 {weights['deathTradePermille']} 위협 {weights['threatPercent']}% 다음 턴 {weights['nextTurnPercent']}%")
    rows, summaries, sources = [], [], {}
    for path in paths:
        base, overlay, base_path = load_pair(path)
        sources[path.name] = _sha(path)
        sources[base_path.name] = _sha(base_path)
        result = evaluate_game_case(base, overlay, weights)
        v3 = evaluate_case(base, v3_settings)["choices"]["victory"]
        expected = base.get("expected", {}).get("victory", [])
        summaries.append({"situationId": result["situationId"], "game": result["choice"], "holdReason": result["holdReason"],
                          "ruleV3Victory": v3, "expectedVictory": expected,
                          "gameMatchesExpected": result["choice"] in expected, "gameMatchesRuleV3": result["choice"] == v3,
                          "approximations": result["approximations"]})
        print(f"\n{result['situationId']} {result['title']}")
        print(f"  게임 AI: {result['choice']} ({result['holdReason']}) | rule-v3: {v3} | 기대: {', '.join(expected)}")
        for item in [result["hold"]] + [c for c in result["candidates"] if c.get("kind") != "wait"]:
            if not item["eligible"]:
                print(f"  - {item['id']:<16} 제외({item['reason']})")
                continue
            t = item["terms"]
            mark = "★" if item["id"] == result["choice"] else " "
            flag = " 치명" if item["lethal"] else ""
            print(f"  {mark} {item['id']:<16} {item['gameAction']:<6} total {t['total']:>6} = attack {t['attack']} "
                  f"followup {t['followup']} nextTurn {t['nextTurn']} progress {t['progress']} "
                  f"risk {t['playerRisk']} hazard {t['hazard']} bind {t['bind']}{flag}")
        for note in result["approximations"]:
            print(f"  ※ {note}")
        for item in [result["hold"]] + [c for c in result["candidates"] if c.get("kind") != "wait"]:
            row = {"situation": result["situationId"], "candidate": item["id"], "kind": item.get("kind"),
                   "gameAction": item.get("gameAction", ""), "eligible": item["eligible"], "lethal": item.get("lethal", ""),
                   "tier": item.get("tier", ""), "beatsHold": item.get("beatsHold", ""),
                   "chosen": item["id"] == result["choice"], "ruleV3Victory": v3, "expectedVictory": " ".join(expected)}
            row.update({key: item["terms"][key] for key in (*TERM_KEYS, "total")} if item["eligible"] else {})
            rows.append(row)
        (run_dir / f"{result['situationId']}.json").write_text(
            json.dumps(result, ensure_ascii=False, indent=2, default=str) + "\n", encoding="utf-8")

    with (run_dir / "results.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=CSV_COLUMNS, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)
    run = {"startedUtc": started.isoformat(), "weights": weights, "sources": sources,
           "evaluatorSha256": _sha(Path(__file__)), "summary": summaries}
    (run_dir / "run.json").write_text(json.dumps(run, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    agree = sum(item["gameMatchesExpected"] for item in summaries)
    same = sum(item["gameMatchesRuleV3"] for item in summaries)
    print(f"\n게임 AI = 기획 기대 {agree}/{len(summaries)} · 게임 AI = rule-v3 {same}/{len(summaries)}")
    print("결과:", run_dir)
    return 0


if __name__ == "__main__":
    sys.exit(main())
