"""고정 JSON 상황의 가치 평가기. Core의 지형·전투 규칙을 재구현하지 않는다."""

from __future__ import annotations

import json
from decimal import Decimal
from pathlib import Path


ZERO = Decimal(0)
ONE = Decimal(1)
TWO = Decimal(2)
UNITS = ("Demon", "Gravekeeper", "Ballista")
WEIGHT_KEYS = ("main", "damage", "kill", "progress", "friendlyLoss", "selfLoss", "baseLoss")


class PolicyInputError(ValueError):
    """누락되거나 모순된 입력을 안전 값으로 대체하지 않고 호출자에게 알린다."""


def load_json(path):
    """정밀한 설정 소수를 보존하고 중복 키를 거부한다."""
    def reject_constant(value):
        raise PolicyInputError(f"nonfinite JSON number: {value}")

    def unique_object(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise PolicyInputError(f"duplicate JSON key: {key}")
            result[key] = value
        return result

    try:
        with Path(path).open(encoding="utf-8-sig") as handle:
            return json.load(handle, parse_float=Decimal, parse_constant=reject_constant,
                             object_pairs_hook=unique_object)
    except (OSError, json.JSONDecodeError) as error:
        raise PolicyInputError(str(error)) from error


def to_jsonable(value):
    if isinstance(value, Decimal):
        return format(value, "f")
    if isinstance(value, dict):
        return {key: to_jsonable(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [to_jsonable(item) for item in value]
    return value


def _object(value, path):
    if not isinstance(value, dict):
        raise PolicyInputError(f"{path}: object required")
    return value


def _required(obj, key, path):
    if key not in obj:
        raise PolicyInputError(f"{path}.{key}: missing")
    return obj[key]


def _text(value, path):
    if not isinstance(value, str) or not value.strip():
        raise PolicyInputError(f"{path}: nonempty string required")
    return value


def _list(value, path):
    if not isinstance(value, list):
        raise PolicyInputError(f"{path}: array required")
    return value


def _integer(value, path, minimum=0):
    if type(value) is not int or value < minimum:
        raise PolicyInputError(f"{path}: integer >= {minimum} required")
    return value


def _boolean(value, path):
    if type(value) is not bool:
        raise PolicyInputError(f"{path}: bool required")
    return value


def _decimal(value, path, minimum=ZERO):
    if isinstance(value, bool) or not isinstance(value, (int, float, Decimal)):
        raise PolicyInputError(f"{path}: finite number required")
    number = Decimal(str(value))
    if not number.is_finite() or number < minimum:
        raise PolicyInputError(f"{path}: finite number >= {minimum} required")
    return number


def _cost(value, path):
    return None if value is None else _integer(value, path)


def _bounded_hp(value, maximum, path):
    hp = _integer(value, path)
    if hp > maximum:
        raise PolicyInputError(f"{path}: HP exceeds allowed value {maximum}")
    return hp


def validate_weights(settings):
    """공식 설정을 검사한다. 기대 선택이나 fixture에 의존하지 않는다."""
    settings = _object(settings, "settings")
    _text(_required(settings, "version", "settings"), "settings.version")
    if settings.get("mainDamageMode") not in ("reference_hp", "max_hp"):
        raise PolicyInputError("settings.mainDamageMode: reference_hp/max_hp required")
    if settings.get("riskMode") not in ("ap_budget", "single_attack"):
        raise PolicyInputError("settings.riskMode: ap_budget/single_attack required")
    for key in ("mainReferenceHp", "progressReferenceAp", "knockbackReferenceAp"):
        if _decimal(_required(settings, key, "settings"), key) <= ZERO:
            raise PolicyInputError(f"settings.{key}: positive value required")
    for key in ("riskDiscount", "roleMaxLoss", "roleBonus", "spacingMin", "spacingFull"):
        _decimal(_required(settings, key, "settings"), key)
    if _decimal(settings["riskDiscount"], "riskDiscount") > ONE:
        raise PolicyInputError("settings.riskDiscount: must be <= 1")
    if _decimal(settings["spacingFull"], "spacingFull") <= _decimal(settings["spacingMin"], "spacingMin"):
        raise PolicyInputError("settings.spacingFull: must exceed spacingMin")
    _integer(_required(settings, "laneReferenceTargets", "settings"), "laneReferenceTargets", 2)
    weights = _object(_required(settings, "weights", "settings"), "settings.weights")
    if set(weights) != set(WEIGHT_KEYS):
        raise PolicyInputError("settings.weights: exact documented keys required")
    for key in WEIGHT_KEYS:
        _decimal(weights[key], f"settings.weights.{key}")
    profiles = _object(_required(settings, "roleProfiles", "settings"), "roleProfiles")
    required_profiles = {"Demon": ("threat", "approach"), "Gravekeeper": ("knockback", "threat"), "Ballista": ("lane", "spacing")}
    if set(profiles) != set(UNITS):
        raise PolicyInputError("roleProfiles: three documented units required")
    for unit, keys in required_profiles.items():
        profile = _object(profiles[unit], f"roleProfiles.{unit}")
        if set(profile) != set(keys):
            raise PolicyInputError(f"roleProfiles.{unit}: wrong keys")
        if sum((_decimal(profile[key], f"roleProfiles.{unit}.{key}") for key in keys), ZERO) != ONE:
            raise PolicyInputError(f"roleProfiles.{unit}: coefficients must sum to 1")


def _health_records(records, path, players=False):
    result = {}
    for index, raw in enumerate(_list(records, path)):
        record = _object(raw, f"{path}[{index}]")
        identifier = _text(_required(record, "id", path), f"{path}.id")
        if identifier in result:
            raise PolicyInputError(f"{path}: duplicate id {identifier}")
        maximum = _integer(_required(record, "maxHp", path), f"{path}.maxHp", 1)
        hp = _bounded_hp(_required(record, "hp", path), maximum, f"{path}.hp")
        role = record.get("role")
        if players and role not in ("main", "regular"):
            raise PolicyInputError(f"{path}.role: main/regular required")
        result[identifier] = {"id": identifier, "hp": hp, "maxHp": maximum, "role": role}
    return result


def _after_map(raw, records, path):
    raw = _object(raw, path)
    if set(raw) != set(records):
        raise PolicyInputError(f"{path}: must contain exactly all starting ids")
    return {key: _bounded_hp(raw[key], records[key]["hp"], f"{path}.{key}") for key in sorted(records)}


def _clamp(value):
    return min(ONE, max(ZERO, value))


def _potential(cost, reference):
    return ZERO if cost is None else ONE / (ONE + Decimal(cost) / reference)


def _power(hp, maximum):
    return ZERO if hp <= 0 else ONE + Decimal(hp) / Decimal(maximum)


def _excluded(candidate, reason):
    return {"id": candidate["id"], "label": candidate["label"], "eligible": False, "reason": reason,
            "outcome": None, "features": {}, "contributions": {}, "victoryScore": None,
            "roleScore": None, "roleEligible": False, "response": {}}


def _action_budget(candidate, actor):
    ap = actor["ap"]
    distance = 0
    for index, raw in enumerate(_list(_required(candidate, "actions", candidate["id"]), "actions")):
        action = _object(raw, f"actions[{index}]")
        kind = _required(action, "kind", "action")
        if kind not in ("attack", "move", "wait"):
            raise PolicyInputError("action.kind: attack/move/wait required")
        cost = _integer(_required(action, "cost", "action"), "action.cost", 1 if kind == "attack" else 0)
        refund = _integer(_required(action, "refund", "action"), "action.refund")
        steps = _integer(_required(action, "steps", "action"), "action.steps")
        if (kind == "move" and steps == 0) or (kind != "move" and steps != 0):
            raise PolicyInputError("action.steps: positive for move, zero otherwise")
        if ap < cost:
            return f"ap_insufficient: action {index} needs {cost}, has {ap}; refund is after payment"
        ap = ap - cost + refund
        if ap > actor["maxAp"]:
            raise PolicyInputError("action.refund: AP exceeds maxAp")
        distance += steps
    if distance > actor["moveRange"]:
        return "move_budget_exceeded: declared steps exceed remaining moveRange"
    return None


def _response(window, players, after, self_hp, terminal, settings):
    window = _object(window, "responseWindow")
    if terminal or self_hp == 0:
        return {"capacityDamage": ZERO, "appliedDamage": ZERO, "attacksApplied": 0, "trace": [],
                "note": "no future response after battle end or actor death"}
    if _required(window, "complete", "responseWindow") is not True:
        raise PolicyInputError("responseWindow.complete: incomplete observation/schedule")
    _text(_required(window, "assumptions", "responseWindow"), "responseWindow.assumptions")
    opportunities = _list(_required(window, "opportunities", "responseWindow"), "responseWindow.opportunities")
    non_threats = _list(_required(window, "nonThreats", "responseWindow"), "responseWindow.nonThreats")
    live = {key for key in players if after[key] > 0}
    exempt = set()
    for raw in non_threats:
        entry = _object(raw, "nonThreat")
        identifier = _text(_required(entry, "opponentId", "nonThreat"), "nonThreat.opponentId")
        _text(_required(entry, "reason", "nonThreat"), "nonThreat.reason")
        if identifier not in players or identifier in exempt:
            raise PolicyInputError("nonThreats: unknown or duplicate opponentId")
        exempt.add(identifier)
    covered = set()
    prepared = []
    trace = []
    for opportunity_index, raw in enumerate(opportunities):
        opportunity = _object(raw, "opportunity")
        identifier = _text(_required(opportunity, "opponentId", "opportunity"), "opportunity.opponentId")
        if identifier not in players:
            raise PolicyInputError("opportunity: unknown opponentId")
        if identifier not in live:
            trace.append({"opponentId": identifier, "status": "ignored_dead_opponent"})
            continue
        covered.add(identifier)
        ap = _integer(_required(opportunity, "availableAp", "opportunity"), "availableAp")
        max_ap = _integer(_required(opportunity, "maxAp", "opportunity"), "maxAp", 1)
        if ap > max_ap:
            raise PolicyInputError("opportunity.availableAp: exceeds maxAp")
        mode = _required(opportunity, "mode", "opportunity")
        if mode == "fixed":
            if opportunity.get("repeatSafe") is not True:
                raise PolicyInputError("fixed response: repeatSafe=true required; dynamic effects need sequence")
            cost = _integer(_required(opportunity, "attackCost", "opportunity"), "attackCost", 1)
            damage = _integer(_required(opportunity, "damagePerAttack", "opportunity"), "damagePerAttack")
            count = ap // cost
            if count > 10000:
                raise PolicyInputError("fixed response: too many attacks for offline fixture")
            attacks = [{"cost": cost, "damage": damage, "refund": 0} for _ in range(count)]
        elif mode == "sequence":
            attacks = _list(_required(opportunity, "attacks", "opportunity"), "opportunity.attacks")
        else:
            raise PolicyInputError("opportunity.mode: fixed/sequence required")
        for attack_index, raw_attack in enumerate(attacks):
            attack = _object(raw_attack, "response attack")
            cost = _integer(_required(attack, "cost", "response attack"), "response.cost", 1)
            damage = _integer(_required(attack, "damage", "response attack"), "response.damage")
            refund = _integer(_required(attack, "refund", "response attack"), "response.refund")
            before = ap
            if before < cost:
                raise PolicyInputError(f"response AP insufficient: {identifier} attack {attack_index}; refund cannot prepay")
            ap = before - cost + refund
            if ap > max_ap:
                raise PolicyInputError("response refund: AP exceeds maxAp")
            prepared.append({"opponentId": identifier, "opportunityIndex": opportunity_index,
                             "attackIndex": attack_index, "apBefore": before, "cost": cost,
                             "refund": refund, "apAfter": ap, "damage": damage})
    if live - (covered | exempt):
        raise PolicyInputError("response coverage missing: " + ", ".join(sorted(live - (covered | exempt))))
    if live & covered & exempt:
        raise PolicyInputError("response coverage contradictory: both opportunity and nonThreat")
    chosen = []
    seen = set()
    for attack in prepared:
        identifier = attack["opponentId"]
        if settings["riskMode"] == "single_attack" and identifier in seen:
            continue
        chosen.append(attack)
        seen.add(identifier)
    hp = self_hp
    capacity = sum((Decimal(attack["damage"]) for attack in chosen), ZERO)
    applied_count = 0
    for attack in chosen:
        applied = min(hp, attack["damage"])
        if hp > 0:
            applied_count += 1
        hp -= applied
        trace.append({**attack, "appliedDamage": applied, "selfHpAfter": hp,
                      "status": "applied" if applied > 0 else "no_hp_damage"})
    return {"capacityDamage": capacity, "appliedDamage": Decimal(self_hp - hp),
            "attacksApplied": applied_count, "trace": trace}


def _role_score(unit, facts, hit_count, after_potential, terminal, alive, settings, player_after):
    facts = _object(facts, "roleFacts")
    threat = _boolean(_required(facts, "canThreatenNext", "roleFacts"), "canThreatenNext")
    distance = _cost(_required(facts, "nearestPlayerDistance", "roleFacts"), "nearestPlayerDistance")
    delays = _list(_required(facts, "knockbackDelays", "roleFacts"), "knockbackDelays")
    values = []
    displaced_ids = set()
    for raw in delays:
        delay = _object(raw, "knockbackDelay")
        target_id = _text(_required(delay, "targetId", "knockbackDelay"), "knockbackDelay.targetId")
        if target_id not in player_after or player_after[target_id] <= 0:
            raise PolicyInputError("knockbackDelay.targetId: surviving player required")
        if target_id in displaced_ids:
            raise PolicyInputError("knockbackDelay.targetId: duplicate displaced target")
        displaced_ids.add(target_id)
        before = _cost(_required(delay, "before", "knockbackDelay"), "knockbackDelay.before")
        after = _cost(_required(delay, "after", "knockbackDelay"), "knockbackDelay.after")
        if before is None:
            values.append(ZERO)
        elif after is None:
            values.append(ONE)
        else:
            values.append(_clamp(Decimal(after - before) / _decimal(settings["knockbackReferenceAp"], "knockbackReferenceAp")))
    displacement = sum(values, ZERO) / len(values) if values else ZERO
    t = ONE if threat and alive and not terminal else ZERO
    approach = after_potential if alive and not terminal else ZERO
    if terminal:
        displacement = ZERO
    profile = settings["roleProfiles"][unit]
    if unit == "Demon":
        return _decimal(profile["threat"], "threat") * t + _decimal(profile["approach"], "approach") * approach
    if unit == "Gravekeeper":
        return _decimal(profile["knockback"], "knockback") * displacement + _decimal(profile["threat"], "threat") * t
    lane = _clamp(Decimal(hit_count - 1) / (settings["laneReferenceTargets"] - 1))
    spacing = ZERO
    if not terminal and alive:
        if distance is None:
            raise PolicyInputError("Ballista.nearestPlayerDistance: required while battle continues")
        spacing = _clamp((Decimal(distance) - _decimal(settings["spacingMin"], "spacingMin")) /
                         (_decimal(settings["spacingFull"], "spacingFull") - _decimal(settings["spacingMin"], "spacingMin")))
    return _decimal(profile["lane"], "lane") * lane + _decimal(profile["spacing"], "spacing") * spacing


def evaluate_case(case, settings):
    """기대 선택을 참조하지 않고 동일 후보의 두 정책 선택과 기여도를 반환한다."""
    validate_weights(settings)
    case = _object(case, "case")
    if type(case.get("schemaVersion")) is not int or case["schemaVersion"] != 1:
        raise PolicyInputError("schemaVersion: integer 1 required; legacy Ollama fixtures are different")
    if case.get("source") != "synthetic":
        raise PolicyInputError("source: this offline version supports synthetic only")
    case_id = _text(_required(case, "situationId", "case"), "situationId")
    title = _text(_required(case, "title", "case"), "title")
    assumptions = _list(_required(case, "assumptions", "case"), "assumptions")
    if not assumptions:
        raise PolicyInputError("assumptions: synthetic evidence required")
    for assumption in assumptions:
        _text(assumption, "assumption")
    actor = _object(_required(case, "self", "case"), "self")
    if actor.get("unit") not in UNITS:
        raise PolicyInputError("self.unit: unsupported role")
    maximum = _integer(_required(actor, "maxHp", "self"), "self.maxHp", 1)
    hp_before = _bounded_hp(_required(actor, "hp", "self"), maximum, "self.hp")
    if hp_before == 0:
        raise PolicyInputError("self.hp: actor must be alive at start")
    _integer(_required(actor, "maxAp", "self"), "self.maxAp", 1)
    if _integer(_required(actor, "ap", "self"), "self.ap") > actor["maxAp"]:
        raise PolicyInputError("self.ap: exceeds maxAp")
    _integer(_required(actor, "moveRange", "self"), "self.moveRange")
    players = _health_records(_required(case, "players", "case"), "players", players=True)
    allies = _health_records(_required(case, "allies", "case"), "allies")
    if set(players) & set(allies):
        raise PolicyInputError("player/ally ids must be disjoint")
    mains = [key for key in players if players[key]["role"] == "main"]
    if len(mains) != 1 or players[mains[0]]["hp"] == 0:
        raise PolicyInputError("players: exactly one living main required")
    main_id = mains[0]
    headquarters = _object(_required(case, "headquarters", "case"), "headquarters")
    hq_max = _integer(_required(headquarters, "maxHp", "headquarters"), "headquarters.maxHp", 1)
    hq_before = _bounded_hp(_required(headquarters, "hp", "headquarters"), hq_max, "headquarters.hp")
    if hq_before == 0:
        raise PolicyInputError("headquarters: battle already ended at start")
    before_cost = _cost(_required(case, "approachCostBefore", "case"), "approachCostBefore")
    progress_reference = _decimal(settings["progressReferenceAp"], "progressReferenceAp")
    weights = {key: _decimal(settings["weights"][key], f"weights.{key}") for key in WEIGHT_KEYS}
    results = []
    seen = set()
    for raw in _list(_required(case, "candidates", "case"), "candidates"):
        candidate = _object(raw, "candidate")
        identifier = _text(_required(candidate, "id", "candidate"), "candidate.id")
        _text(_required(candidate, "label", "candidate"), "candidate.label")
        if identifier in seen:
            raise PolicyInputError(f"candidate.id: duplicate {identifier}")
        seen.add(identifier)
        if not _boolean(_required(candidate, "legal", "candidate"), "candidate.legal"):
            reason = _text(_required(candidate, "legalityNote", "candidate"), "legalityNote")
            results.append(_excluded(candidate, "declared_illegal: " + reason))
            continue
        budget_error = _action_budget(candidate, actor)
        if budget_error:
            results.append(_excluded(candidate, budget_error))
            continue
        hp_after = _bounded_hp(_required(candidate, "selfHpAfter", identifier), hp_before, "selfHpAfter")
        player_after = _after_map(_required(candidate, "playerHpAfter", identifier), players, "playerHpAfter")
        ally_after = _after_map(_required(candidate, "allyHpAfter", identifier), allies, "allyHpAfter")
        hq_after = _bounded_hp(_required(candidate, "headquartersHpAfter", identifier), hq_before, "headquartersHpAfter")
        after_cost = _cost(_required(candidate, "approachCostAfter", identifier), "approachCostAfter")
        hit_ids = _list(_required(candidate, "hitIds", identifier), "hitIds")
        for hit_id in hit_ids:
            _text(hit_id, "hitId")
            if hit_id not in players or player_after[hit_id] >= players[hit_id]["hp"]:
                raise PolicyInputError("hitIds: every hit must name a player with positive HP loss")
        if len(set(hit_ids)) != len(hit_ids):
            raise PolicyInputError("hitIds: duplicate player id")
        outcome = "enemy_win" if player_after[main_id] == 0 else "enemy_loss" if hq_after == 0 else "ongoing"
        terminal = outcome != "ongoing"
        response = _response(_required(candidate, "responseWindow", identifier), players, player_after, hp_after, terminal, settings)
        main_reference = players[main_id]["maxHp"] if settings["mainDamageMode"] == "max_hp" else _decimal(settings["mainReferenceHp"], "mainReferenceHp")
        main_damage = Decimal(players[main_id]["hp"] - player_after[main_id]) / main_reference
        damage = sum((Decimal(players[key]["hp"] - player_after[key]) / players[key]["maxHp"]
                      for key in sorted(players) if key != main_id), ZERO)
        kills = sum(1 for key in players if key != main_id and players[key]["hp"] > 0 and player_after[key] == 0)
        friendly_loss = sum((Decimal(allies[key]["hp"] - ally_after[key]) / allies[key]["maxHp"] +
                             (ONE if allies[key]["hp"] > 0 and ally_after[key] == 0 else ZERO)
                             for key in sorted(allies)), ZERO)
        after_potential = _potential(after_cost, progress_reference)
        progress = after_potential - _potential(before_cost, progress_reference) if hp_after > 0 and not terminal else ZERO
        actual_self_loss = _power(hp_before, maximum) - _power(hp_after, maximum)
        response_hp = hp_after - int(response["appliedDamage"])
        future_loss = _power(hp_after, maximum) - _power(response_hp, maximum)
        self_loss = actual_self_loss + _decimal(settings["riskDiscount"], "riskDiscount") * future_loss
        base_loss = Decimal(hq_before - hq_after) / hq_max
        role = _role_score(actor["unit"], _required(candidate, "roleFacts", identifier), len(hit_ids),
                           after_potential, terminal, hp_after > 0, settings, player_after)
        features = {"M": main_damage, "D": damage, "K": Decimal(kills), "P": progress,
                    "F": friendly_loss, "R": self_loss, "B": base_loss, "Q": role}
        contributions = {"main": weights["main"] * main_damage, "damage": weights["damage"] * damage,
                         "kill": weights["kill"] * kills, "progress": weights["progress"] * progress,
                         "friendlyLoss": -weights["friendlyLoss"] * friendly_loss,
                         "selfLoss": -weights["selfLoss"] * self_loss, "baseLoss": -weights["baseLoss"] * base_loss}
        victory_score = sum(contributions.values(), ZERO)
        contributions["roleBonus"] = _decimal(settings["roleBonus"], "roleBonus") * role
        response["actualSelfLoss"] = actual_self_loss
        response["futurePowerLoss"] = future_loss
        results.append({"id": identifier, "label": candidate["label"], "eligible": True, "reason": "",
                        "outcome": outcome, "features": features, "contributions": contributions,
                        "victoryScore": victory_score, "roleScore": victory_score + contributions["roleBonus"],
                        "roleEligible": False, "response": response})
    eligible = [item for item in results if item["eligible"]]
    if not eligible:
        raise PolicyInputError("no eligible candidates")
    rank = {"enemy_loss": 0, "ongoing": 1, "enemy_win": 2}
    best_outcome = max(rank[item["outcome"]] for item in eligible)
    comparable = [item for item in eligible if rank[item["outcome"]] == best_outcome]
    winner = min(comparable, key=lambda item: (-item["victoryScore"], item["id"]))
    floor = winner["victoryScore"] - _decimal(settings["roleMaxLoss"], "roleMaxLoss")
    for item in eligible:
        if rank[item["outcome"]] < best_outcome:
            item["reason"] = "lower_outcome"
        elif item["victoryScore"] < floor:
            item["reason"] = "outside_role_budget"
        else:
            item["roleEligible"] = True
    role_winner = min((item for item in comparable if item["roleEligible"]),
                      key=lambda item: (-item["roleScore"], -item["victoryScore"], item["id"]))
    return {"situationId": case_id, "title": title, "source": case["source"],
            "choices": {"victory": winner["id"], "role": role_winner["id"]},
            "bestVictoryScore": winner["victoryScore"], "roleFloor": floor, "candidates": results}
