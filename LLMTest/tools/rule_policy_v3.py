"""진영 턴(상황 형식 v2, schemaVersion 2)용 가치 평가기 rule-v3.

기준은 docs/2026-11-06-faction-turn-criteria-v2.md, 입력 모양은 docs/2026-11-06-faction-turn-format-v2.md를 따른다.
v1 평가기(rule_policy.evaluate_case의 schemaVersion 1 경로)는 바꾸지 않는다.
가중치·종료 결과 우선·역할 허용 차이 규칙은 v1과 같고, 반격 위험·진척·넉백·역할 프로필 정의만 다르다.
"""

from __future__ import annotations

from decimal import Decimal

from rule_policy import (
    ONE,
    WEIGHT_KEYS,
    ZERO,
    PolicyInputError,
    _boolean,
    _bounded_hp,
    _clamp,
    _cost,
    _decimal,
    _integer,
    _list,
    _object,
    _required,
    _text,
)


UNITS = ("Demon", "Gravekeeper", "UndeadHeavy", "UndeadCannonCart")
PROFILE_KEYS = {
    "Demon": ("threat", "approach"),
    "Gravekeeper": ("knockback", "threat"),
    "UndeadHeavy": ("threat", "approach"),
    "UndeadCannonCart": ("threat", "spacing"),
}
KINDS = ("wait", "move", "attack", "move_attack")
REACHES = ("in_place", "after_move")
DISCOUNT_KEYS = ("in_place", "after_move", "chain", "hazard")


def validate_weights_v3(settings):
    """rule-v3 설정을 검사한다. 기대 선택이나 상황 파일에 의존하지 않는다."""
    settings = _object(settings, "settings")
    _text(_required(settings, "version", "settings"), "settings.version")
    if settings.get("mainDamageMode") not in ("reference_hp", "max_hp"):
        raise PolicyInputError("settings.mainDamageMode: reference_hp/max_hp required")
    if settings.get("riskMode") != "faction_turn":
        raise PolicyInputError("settings.riskMode: faction_turn required for rule-v3")
    if _decimal(_required(settings, "mainReferenceHp", "settings"), "mainReferenceHp") <= ZERO:
        raise PolicyInputError("settings.mainReferenceHp: positive value required")
    for key in ("roleMaxLoss", "roleBonus", "spacingMin", "spacingFull"):
        _decimal(_required(settings, key, "settings"), key)
    if _decimal(settings["spacingFull"], "spacingFull") <= _decimal(settings["spacingMin"], "spacingMin"):
        raise PolicyInputError("settings.spacingFull: must exceed spacingMin")
    discounts = _object(_required(settings, "riskDiscounts", "settings"), "settings.riskDiscounts")
    if set(discounts) != set(DISCOUNT_KEYS):
        raise PolicyInputError("settings.riskDiscounts: exact keys in_place/after_move/chain/hazard required")
    for key in DISCOUNT_KEYS:
        if _decimal(discounts[key], f"riskDiscounts.{key}") > ONE:
            raise PolicyInputError(f"settings.riskDiscounts.{key}: must be <= 1")
    worth = _object(_required(settings, "unitWorth", "settings"), "settings.unitWorth")
    if set(worth) != set(UNITS):
        raise PolicyInputError("settings.unitWorth: every evaluated unit required")
    for unit in UNITS:
        _decimal(worth[unit], f"unitWorth.{unit}")
    weights = _object(_required(settings, "weights", "settings"), "settings.weights")
    if set(weights) != set(WEIGHT_KEYS):
        raise PolicyInputError("settings.weights: exact documented keys required")
    for key in WEIGHT_KEYS:
        _decimal(weights[key], f"settings.weights.{key}")
    profiles = _object(_required(settings, "roleProfiles", "settings"), "roleProfiles")
    if set(profiles) != set(UNITS):
        raise PolicyInputError("roleProfiles: every evaluated unit required")
    for unit, keys in PROFILE_KEYS.items():
        profile = _object(profiles[unit], f"roleProfiles.{unit}")
        if set(profile) != set(keys):
            raise PolicyInputError(f"roleProfiles.{unit}: wrong keys")
        if sum((_decimal(profile[key], f"roleProfiles.{unit}.{key}") for key in keys), ZERO) != ONE:
            raise PolicyInputError(f"roleProfiles.{unit}: coefficients must sum to 1")


def _power(hp, maximum):
    return ZERO if hp <= 0 else ONE + Decimal(hp) / Decimal(maximum)


def _potential(cost, reference):
    return ZERO if cost is None else ONE / (ONE + Decimal(cost) / Decimal(reference))


def _players(raw):
    records = {}
    for index, item in enumerate(_list(raw, "players")):
        record = _object(item, f"players[{index}]")
        identifier = _text(_required(record, "id", "player"), "player.id")
        if identifier in records:
            raise PolicyInputError(f"players: duplicate id {identifier}")
        if record.get("role") not in ("main", "regular"):
            raise PolicyInputError("player.role: main/regular required")
        _text(_required(record, "unit", "player"), "player.unit")
        maximum = _integer(_required(record, "maxHp", "player"), "player.maxHp", 1)
        _bounded_hp(_required(record, "hp", "player"), maximum, "player.hp")
        _integer(_required(record, "moveRange", "player"), "player.moveRange", 1)
        _integer(_required(record, "attackRange", "player"), "player.attackRange", 1)
        for trait in _list(_required(record, "traits", "player"), "player.traits"):
            _text(trait, "player.trait")
        records[identifier] = record
    return records


def _allies(raw):
    records = {}
    for index, item in enumerate(_list(raw, "allies")):
        record = _object(item, f"allies[{index}]")
        identifier = _text(_required(record, "id", "ally"), "ally.id")
        if identifier in records:
            raise PolicyInputError(f"allies: duplicate id {identifier}")
        _text(_required(record, "unit", "ally"), "ally.unit")
        maximum = _integer(_required(record, "maxHp", "ally"), "ally.maxHp", 1)
        _bounded_hp(_required(record, "hp", "ally"), maximum, "ally.hp")
        records[identifier] = record
    return records


def _after_map(raw, records, path):
    after = _object(raw, path)
    if set(after) != set(records):
        raise PolicyInputError(f"{path}: every id exactly once required")
    return {key: _bounded_hp(after[key], records[key]["hp"], f"{path}.{key}") for key in records}


def _turn(raw, actor_id):
    turn = _object(raw, "turn")
    if turn.get("dayNight") not in ("day", "night"):
        raise PolicyInputError("turn.dayNight: day/night required")
    order = [_text(item, "turn.enemyOrder[]") for item in _list(_required(turn, "enemyOrder", "turn"), "turn.enemyOrder")]
    if len(set(order)) != len(order):
        raise PolicyInputError("turn.enemyOrder: duplicate id")
    acting = _text(_required(turn, "actingId", "turn"), "turn.actingId")
    if acting != actor_id or acting not in order:
        raise PolicyInputError("turn.actingId: must be self.id and appear in enemyOrder")
    acted = _list(_required(turn, "actedAllies", "turn"), "turn.actedAllies")
    before = order[:order.index(acting)]
    acted_ids = []
    for item in acted:
        entry = _object(item, "actedAlly")
        acted_ids.append(_text(_required(entry, "id", "actedAlly"), "actedAlly.id"))
        _text(_required(entry, "summary", "actedAlly"), "actedAlly.summary")
    if acted_ids != before:
        raise PolicyInputError("turn.actedAllies: must list the units before actingId in enemyOrder, in order")


def _hazards(raw):
    identifiers = set()
    for index, item in enumerate(_list(raw, "announcedHazards")):
        hazard = _object(item, f"announcedHazards[{index}]")
        identifier = _text(_required(hazard, "id", "hazard"), "hazard.id")
        if identifier in identifiers:
            raise PolicyInputError("announcedHazards: duplicate id")
        identifiers.add(identifier)
        _text(_required(hazard, "description", "hazard"), "hazard.description")
        _integer(_required(hazard, "damage", "hazard"), "hazard.damage")
    return identifiers


def _exposure(raw, players, after, self_hp, terminal, discounts, worth, maximum, hazard_ids):
    """다음 아군 진영 턴 한 번의 노출을 확실성 단계별로 할인한 미래 전력 손실로 바꾼다."""
    exposure = _object(raw, "exposure")
    if _required(exposure, "complete", "exposure") is not True:
        raise PolicyInputError("exposure.complete: incomplete observation")
    _text(_required(exposure, "assumptions", "exposure"), "exposure.assumptions")
    threats = _list(_required(exposure, "threats", "exposure"), "exposure.threats")
    non_threats = _list(_required(exposure, "nonThreats", "exposure"), "exposure.nonThreats")
    hazard_damage = _integer(_required(exposure, "hazardDamage", "exposure"), "exposure.hazardDamage")
    hazard_sources = _list(exposure.get("hazardIds", []), "exposure.hazardIds")
    for item in hazard_sources:
        if _text(item, "exposure.hazardIds[]") not in hazard_ids:
            raise PolicyInputError("exposure.hazardIds: unknown announced hazard")
    if hazard_damage > 0 and not hazard_sources:
        raise PolicyInputError("exposure.hazardDamage: hazardIds required when positive")
    live = {key for key in players if after[key] > 0}
    covered, exempt = set(), set()
    damage = {"in_place": 0, "after_move": 0, "chain": 0}
    counts = {"in_place": 0, "after_move": 0}
    trace = []
    for index, item in enumerate(threats):
        threat = _object(item, f"threats[{index}]")
        identifier = _text(_required(threat, "opponentId", "threat"), "threat.opponentId")
        if identifier not in players:
            raise PolicyInputError("threat: unknown opponentId")
        if identifier in covered:
            raise PolicyInputError("threat: duplicate opponentId (one attack per player turn)")
        covered.add(identifier)
        reach = threat.get("reach")
        if reach not in REACHES:
            raise PolicyInputError("threat.reach: in_place/after_move required")
        hit = _integer(_required(threat, "damage", "threat"), "threat.damage")
        if identifier not in live:
            trace.append({"opponentId": identifier, "status": "ignored_dead_opponent"})
            continue
        damage[reach] += hit
        counts[reach] += 1
        trace.append({"opponentId": identifier, "reach": reach, "damage": hit})
    # KillChain 연쇄는 맵 전체 무작위 대상이라 행위자에게 직접 닿지 않는 플레이어에게서도 생긴다. 위협과 따로 적는다.
    chain_ids = set()
    for index, item in enumerate(_list(exposure.get("chains", []), "exposure.chains")):
        chain = _object(item, f"chains[{index}]")
        identifier = _text(_required(chain, "opponentId", "chain"), "chain.opponentId")
        if identifier not in players or identifier in chain_ids:
            raise PolicyInputError("chain: unknown or duplicate opponentId")
        chain_ids.add(identifier)
        amount = _integer(_required(chain, "damage", "chain"), "chain.damage")
        _text(_required(chain, "note", "chain"), "chain.note")
        if identifier not in live:
            trace.append({"opponentId": identifier, "status": "ignored_dead_chain"})
            continue
        damage["chain"] += amount
        trace.append({"opponentId": identifier, "chainDamage": amount})
    for index, item in enumerate(non_threats):
        entry = _object(item, f"nonThreats[{index}]")
        identifier = _text(_required(entry, "opponentId", "nonThreat"), "nonThreat.opponentId")
        _text(_required(entry, "reason", "nonThreat"), "nonThreat.reason")
        if identifier not in players or identifier in exempt:
            raise PolicyInputError("nonThreats: unknown or duplicate opponentId")
        exempt.add(identifier)
    if covered & exempt:
        raise PolicyInputError("exposure coverage contradictory: both threat and nonThreat")
    if live - (covered | exempt):
        raise PolicyInputError("exposure coverage missing: " + ", ".join(sorted(live - (covered | exempt))))
    facts = {"hazardDamage": hazard_damage, "inPlace": {"attackers": counts["in_place"], "damage": damage["in_place"]},
             "afterMove": {"attackers": counts["after_move"], "damage": damage["after_move"]},
             "chainDamage": damage["chain"], "trace": trace}
    if terminal or self_hp == 0:
        return ZERO, {**facts, "note": "no future exposure after battle end or actor death",
                      "hpAfterInPlace": None, "hpAfterAll": None}
    stages = (("hazard", hazard_damage), ("in_place", damage["in_place"]),
              ("after_move", damage["after_move"]), ("chain", damage["chain"]))
    hp = self_hp
    loss = ZERO
    levels = {}
    for name, amount in stages:
        nxt = max(0, hp - amount)
        loss += _decimal(discounts[name], f"riskDiscounts.{name}") * (_power(hp, maximum) - _power(nxt, maximum))
        hp = nxt
        levels[name] = hp
    facts["hpAfterInPlace"] = levels["in_place"]
    facts["hpAfterAll"] = levels["after_move"]
    return worth * loss, facts


def _role_score(unit, facts, hit_count, after_potential, terminal, alive, settings, players, player_after):
    facts = _object(facts, "roleFacts")
    threat = _boolean(_required(facts, "canThreatenNext", "roleFacts"), "canThreatenNext")
    distance = _cost(_required(facts, "nearestPlayerDistance", "roleFacts"), "nearestPlayerDistance")
    delays = _list(_required(facts, "knockbackDelays", "roleFacts"), "knockbackDelays")
    values, displaced = [], set()
    for raw in delays:
        delay = _object(raw, "knockbackDelay")
        target = _text(_required(delay, "targetId", "knockbackDelay"), "knockbackDelay.targetId")
        if target not in player_after or player_after[target] <= 0:
            raise PolicyInputError("knockbackDelay.targetId: surviving player required")
        if target in displaced:
            raise PolicyInputError("knockbackDelay.targetId: duplicate displaced target")
        displaced.add(target)
        before = _cost(_required(delay, "before", "knockbackDelay"), "knockbackDelay.before")
        after = _cost(_required(delay, "after", "knockbackDelay"), "knockbackDelay.after")
        if before is None:
            values.append(ZERO)
        elif after is None:
            values.append(ONE)
        else:
            # 밀린 플레이어의 이동 1회분만큼 늦어지면 1이다(기준 §2.4).
            values.append(_clamp(Decimal(after - before) / Decimal(players[target]["moveRange"])))
    alive_now = alive and not terminal
    t = ONE if threat and alive_now else ZERO
    approach = after_potential if alive_now else ZERO
    displacement = ZERO if terminal or not values else sum(values, ZERO) / len(values)
    spacing = ZERO
    if unit == "UndeadCannonCart" and alive_now:
        if distance is None:
            raise PolicyInputError("UndeadCannonCart.nearestPlayerDistance: required while battle continues")
        low = _decimal(settings["spacingMin"], "spacingMin")
        spacing = _clamp((Decimal(distance) - low) / (_decimal(settings["spacingFull"], "spacingFull") - low))
    parts = {"threat": t, "approach": approach, "knockback": displacement, "spacing": spacing}
    profile = settings["roleProfiles"][unit]
    return sum((_decimal(profile[key], key) * parts[key] for key in PROFILE_KEYS[unit]), ZERO)


def _excluded(candidate, reason):
    return {"id": candidate["id"], "label": candidate["label"], "eligible": False, "reason": reason,
            "outcome": None, "features": {}, "contributions": {}, "victoryScore": None,
            "roleScore": None, "roleEligible": False, "response": {}}


def evaluate_case_v3(case, settings):
    """기대 선택을 참조하지 않고 동일 후보의 승리·역할 선택과 기여도를 반환한다(v1과 같은 결과 모양)."""
    validate_weights_v3(settings)
    case = _object(case, "case")
    if type(case.get("schemaVersion")) is not int or case["schemaVersion"] != 2:
        raise PolicyInputError("schemaVersion: integer 2 required for rule-v3")
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
    actor_id = _text(_required(actor, "id", "self"), "self.id")
    unit = actor.get("unit")
    if unit not in UNITS:
        raise PolicyInputError("self.unit: unsupported unit for rule-v3")
    maximum = _integer(_required(actor, "maxHp", "self"), "self.maxHp", 1)
    hp_before = _bounded_hp(_required(actor, "hp", "self"), maximum, "self.hp")
    if hp_before == 0:
        raise PolicyInputError("self.hp: actor must be alive at start")
    move_range = _integer(_required(actor, "moveRange", "self"), "self.moveRange", 1)
    _integer(_required(actor, "attackRange", "self"), "self.attackRange", 1)
    budget = actor.get("budget")
    if budget is not None:
        budget = _integer(budget, "self.budget")
    _turn(_required(case, "turn", "case"), actor_id)
    players = _players(_required(case, "players", "case"))
    allies = _allies(_required(case, "allies", "case"))
    if set(players) & set(allies) or actor_id in players or actor_id in allies:
        raise PolicyInputError("self/player/ally ids must be disjoint")
    mains = [key for key in players if players[key]["role"] == "main"]
    if len(mains) != 1 or players[mains[0]]["hp"] == 0:
        raise PolicyInputError("players: exactly one living main required")
    main_id = mains[0]
    hazard_ids = _hazards(_required(case, "announcedHazards", "case"))
    headquarters = _object(_required(case, "headquarters", "case"), "headquarters")
    hq_max = _integer(_required(headquarters, "maxHp", "headquarters"), "headquarters.maxHp", 1)
    hq_before = _bounded_hp(_required(headquarters, "hp", "headquarters"), hq_max, "headquarters.hp")
    if hq_before == 0:
        raise PolicyInputError("headquarters: battle already ended at start")
    before_cost = _cost(_required(case, "approachCostBefore", "case"), "approachCostBefore")
    weights = {key: _decimal(settings["weights"][key], f"weights.{key}") for key in WEIGHT_KEYS}
    discounts = settings["riskDiscounts"]
    worth = _decimal(settings["unitWorth"][unit], f"unitWorth.{unit}")
    results, seen = [], set()
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
        kind = candidate.get("kind")
        if kind not in KINDS:
            raise PolicyInputError(f"{identifier}.kind: wait/move/attack/move_attack required")
        steps = _integer(_required(candidate, "moveSteps", identifier), "moveSteps")
        if kind in ("wait", "attack") and steps != 0:
            raise PolicyInputError(f"{identifier}.moveSteps: must be 0 for {kind}")
        if kind in ("move", "move_attack") and steps == 0:
            raise PolicyInputError(f"{identifier}.moveSteps: must be positive for {kind}")
        if steps > move_range:
            results.append(_excluded(candidate, f"move_out_of_range: {steps} > {move_range}"))
            continue
        if budget is not None:
            cost = _integer(_required(candidate, "budgetCost", identifier), "budgetCost")
            if cost > budget:
                results.append(_excluded(candidate, f"budget_exceeded: {cost} > {budget}"))
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
        if kind in ("wait", "move") and (hit_ids or any(player_after[k] != players[k]["hp"] for k in players)):
            raise PolicyInputError(f"{identifier}: {kind} cannot damage players")
        outcome = "enemy_win" if player_after[main_id] == 0 else "enemy_loss" if hq_after == 0 else "ongoing"
        terminal = outcome != "ongoing"
        future_loss, exposure = _exposure(_required(candidate, "exposure", identifier), players, player_after,
                                          hp_after, terminal, discounts, worth, maximum, hazard_ids)
        main_reference = players[main_id]["maxHp"] if settings["mainDamageMode"] == "max_hp" else _decimal(settings["mainReferenceHp"], "mainReferenceHp")
        main_damage = Decimal(players[main_id]["hp"] - player_after[main_id]) / main_reference
        damage = sum((Decimal(players[key]["hp"] - player_after[key]) / players[key]["maxHp"]
                      for key in sorted(players) if key != main_id), ZERO)
        kills = sum(1 for key in players if key != main_id and players[key]["hp"] > 0 and player_after[key] == 0)
        friendly_loss = sum((Decimal(allies[key]["hp"] - ally_after[key]) / allies[key]["maxHp"] +
                             (ONE if allies[key]["hp"] > 0 and ally_after[key] == 0 else ZERO)
                             for key in sorted(allies)), ZERO)
        after_potential = _potential(after_cost, move_range)
        progress = after_potential - _potential(before_cost, move_range) if hp_after > 0 and not terminal else ZERO
        actual_self_loss = worth * (_power(hp_before, maximum) - _power(hp_after, maximum))
        self_loss = actual_self_loss + future_loss
        base_loss = Decimal(hq_before - hq_after) / hq_max
        role = _role_score(unit, _required(candidate, "roleFacts", identifier), len(hit_ids), after_potential,
                           terminal, hp_after > 0, settings, players, player_after)
        features = {"M": main_damage, "D": damage, "K": Decimal(kills), "P": progress,
                    "F": friendly_loss, "R": self_loss, "B": base_loss, "Q": role}
        contributions = {"main": weights["main"] * main_damage, "damage": weights["damage"] * damage,
                         "kill": weights["kill"] * kills, "progress": weights["progress"] * progress,
                         "friendlyLoss": -weights["friendlyLoss"] * friendly_loss,
                         "selfLoss": -weights["selfLoss"] * self_loss, "baseLoss": -weights["baseLoss"] * base_loss}
        victory_score = sum(contributions.values(), ZERO)
        contributions["roleBonus"] = _decimal(settings["roleBonus"], "roleBonus") * role
        exposure["actualSelfLoss"] = actual_self_loss
        exposure["futurePowerLoss"] = future_loss
        # run_rule_test의 CSV 칸과 맞추려고 v1 이름도 함께 둔다.
        exposure["capacityDamage"] = exposure["hazardDamage"] + exposure["inPlace"]["damage"] + exposure["afterMove"]["damage"]
        exposure["appliedDamage"] = None if exposure["hpAfterAll"] is None else hp_after - exposure["hpAfterAll"]
        results.append({"id": identifier, "label": candidate["label"], "eligible": True, "reason": "",
                        "kind": kind, "outcome": outcome, "features": features, "contributions": contributions,
                        "victoryScore": victory_score, "roleScore": victory_score + contributions["roleBonus"],
                        "roleEligible": False, "response": exposure})
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
