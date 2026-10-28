"""룰 상황(rule_fixtures)에서 LLM이 읽을 사실만 뽑는다.

원본은 R 파일 하나다. 합법성·종료 결과·반격 일정 산술은 rule_policy.evaluate_case의 결과를 그대로 쓰고,
가중치·점수·기대 선택·label은 넣지 않는다. 후보 ID도 평가 단어를 담을 수 있어 중립 별칭(c1, c2…)으로 바꾼다.
"""

from __future__ import annotations

import copy
import random

from rule_policy import evaluate_case

ORDERS = ("file", "reverse", "shuffle")


def _action_text(actions):
    parts = []
    for action in actions:
        if action["kind"] == "move":
            parts.append(f"move {action['steps']}")
        else:
            parts.append(action["kind"])
    return " -> ".join(parts)


def _losses(records, after):
    return [{"id": record["id"], **({"role": record["role"]} if "role" in record else {}),
             "hpBefore": record["hp"], "hpAfter": after[record["id"]], "killed": after[record["id"]] == 0}
            for record in records if after[record["id"]] < record["hp"]]


def _next_turn(evaluated, self_hp_after):
    if evaluated["outcome"] != "ongoing" or self_hp_after == 0:
        return None
    damage = int(evaluated["response"]["appliedDamage"])
    return {"expectedDamage": damage, "hpAfter": self_hp_after - damage, "dies": self_hp_after - damage == 0}


BATTLE = {"ongoing": "continues", "enemy_win": "enemy_wins", "enemy_loss": "enemy_loses"}


def _ordered(candidates, order, seed, case_id):
    """순서 불변 시험용으로 후보 순서를 바꾼다. 별칭은 바뀐 순서대로 다시 붙는다."""
    if order not in ORDERS:
        raise ValueError(f"order: {ORDERS} 중 하나가 필요")
    candidates = list(candidates)
    if order == "reverse":
        candidates.reverse()
    elif order == "shuffle":
        random.Random(f"{seed}:{case_id}").shuffle(candidates)   # 같은 seed와 상황이면 같은 순서
    return candidates


def project(case, settings, order="file", seed=0):
    """(LLM 화면, 별칭→원래 ID, 룰 평가 결과)를 돌려준다.
    미평가 상황은 rule_policy와 같은 PolicyInputError를 던져 LLM에도 보내지 않는다.
    order는 file(파일 순서)·reverse·shuffle(seed와 상황 ID로 고정)이다. 룰 평가는 순서와 무관하다."""
    result = evaluate_case(copy.deepcopy(case), copy.deepcopy(settings))
    evaluated = {item["id"]: item for item in result["candidates"]}
    actor = case["self"]
    view_candidates, aliases = [], {}
    eligible = [raw for raw in case["candidates"] if evaluated[raw["id"]]["eligible"]]   # 불법·AP 부족 후보는 코드가 이미 제외했다
    for raw in _ordered(eligible, order, seed, case["situationId"]):
        item = evaluated[raw["id"]]
        alias = f"c{len(view_candidates) + 1}"
        aliases[alias] = raw["id"]
        facts = raw["roleFacts"]
        view_candidates.append({
            "id": alias,
            "actions": _action_text(raw["actions"]),
            "battle": BATTLE[item["outcome"]],
            "hits": _losses(case["players"], raw["playerHpAfter"]),
            "allyLosses": _losses(case["allies"], raw["allyHpAfter"]),
            "headquartersHpAfter": raw["headquartersHpAfter"],
            "self": {"hpAfter": raw["selfHpAfter"], "diesNow": raw["selfHpAfter"] == 0},
            "beforeMyNextTurn": _next_turn(item, raw["selfHpAfter"]),
            "mainApproachCost": {"before": case["approachCostBefore"], "after": raw["approachCostAfter"]},
            "next": {"canThreatenNext": facts["canThreatenNext"], "nearestPlayerDistance": facts["nearestPlayerDistance"]},
            "knockback": [{"targetId": k["targetId"], "headquartersApproachBefore": k["before"],
                           "headquartersApproachAfter": k["after"]} for k in facts["knockbackDelays"]],
        })
    view = {
        "self": {key: actor[key] for key in ("unit", "hp", "maxHp", "ap", "maxAp")},
        "players": [{key: p[key] for key in ("id", "role", "hp", "maxHp")} for p in case["players"]],
        "allies": [{key: a[key] for key in ("id", "hp", "maxHp")} for a in case["allies"]],
        "headquarters": dict(case["headquarters"]),
        "candidates": view_candidates,
    }
    return view, aliases, result
