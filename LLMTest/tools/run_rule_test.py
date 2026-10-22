"""고정 synthetic 상황에서 두 룰 정책과 계수 민감도를 비교한다."""

from __future__ import annotations

import argparse
import copy
import csv
import hashlib
import json
from datetime import datetime, timezone
from decimal import Decimal
from pathlib import Path
import sys

import rule_policy
from rule_policy import (
    PolicyInputError,
    evaluate_case,
    load_json,
    to_jsonable,
    validate_weights,
)


ROOT = Path(__file__).resolve().parents[1]


def json_text(value):
    return json.dumps(to_jsonable(value), ensure_ascii=False, indent=2) + "\n"


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def profiles(settings, sweep):
    """비교마다 한 항목만 변경하고 실제 사용 설정을 결과에 보관한다."""
    yield "revised", copy.deepcopy(settings)
    if not sweep:
        return
    changes = [
        ("legacy", {"mainDamageMode": "max_hp", "riskMode": "single_attack", "main": 60}),
        ("main_only", {"mainDamageMode": "reference_hp", "riskMode": "single_attack", "main": settings["weights"]["main"]}),
        ("risk_only", {"mainDamageMode": "max_hp", "riskMode": "ap_budget", "main": 60}),
    ]
    changes += [(f"main_{v}", {"main": v}) for v in (20, 25, 35, 40)]
    changes += [(f"discount_{v}", {"riskDiscount": Decimal(v)}) for v in ("0.25", "0.75", "1")]
    changes += [(f"role_loss_{v}", {"roleMaxLoss": v}) for v in (0, 6, 18)]
    changes += [(f"role_bonus_{v}", {"roleBonus": v}) for v in (10, 30)]
    for name, overrides in changes:
        item = copy.deepcopy(settings)
        for key, value in overrides.items():
            if key == "main":
                item["weights"]["main"] = value
            else:
                item[key] = value
        yield name, item


def compare_expected(case, result, error):
    expected = case.get("expected")
    if not isinstance(expected, dict):
        return False, "expected가 없거나 객체가 아님"
    if expected.get("status") == "unevaluated":
        return error is not None, "예상 미평가" if error else "미평가 예상 상황이 평가됨"
    if error:
        return False, "선택 예상 상황을 평가하지 못함"
    choices = result["choices"]
    missing = []
    for policy in ("victory", "role"):
        accepted = expected.get(policy)
        if not isinstance(accepted, list) or not accepted or choices[policy] not in accepted:
            missing.append(policy)
    return not missing, "일치" if not missing else "기대 선택 불일치: " + ", ".join(missing)


def evaluate_one(case, setting, profile, case_path, raw_hash):
    result, error = None, None
    try:
        result = evaluate_case(copy.deepcopy(case), copy.deepcopy(setting))
    except PolicyInputError as exc:
        error = str(exc)
    matched, note = compare_expected(case, result, error)
    return {
        "profile": profile,
        "situationId": case.get("situationId", case_path.stem),
        "caseFile": case_path.name,
        "caseSha256": raw_hash,
        "status": "unevaluated" if error is not None else "evaluated",
        "expected": case.get("expected"),
        "expectedMatch": matched,
        "expectedNote": note,
        "error": error,
        "result": result,
    }


def csv_rows(records):
    for record in records:
        result = record["result"]
        common = {
            "profile": record["profile"],
            "case": record["situationId"],
            "status": record["status"],
            "expectedMatch": record["expectedMatch"],
        }
        if result is None:
            yield dict(common, id="", excludedReason=record["error"])
            continue
        expected = record["expected"] or {}
        for candidate in result["candidates"]:
            candidate_id = candidate["id"]
            response = candidate.get("response", {})
            yield dict(
                common,
                id=candidate_id,
                excludedReason=candidate.get("reason") or "",
                outcome=candidate.get("outcome"),
                W=candidate.get("victoryScore"),
                Q=candidate.get("features", {}).get("Q"),
                Role=candidate.get("roleScore"),
                roleEligible=candidate.get("roleEligible"),
                contributions=json.dumps(to_jsonable(candidate.get("contributions", {})), ensure_ascii=False, sort_keys=True),
                responseCapacityDamage=response.get("capacityDamage"),
                responseAppliedDamage=response.get("appliedDamage"),
                responseAttacks=response.get("attacksApplied"),
                responseTrace=json.dumps(to_jsonable(response.get("trace", [])), ensure_ascii=False),
                victoryExpected=candidate_id in expected.get("victory", []),
                roleExpected=candidate_id in expected.get("role", []),
                victorySelected=candidate_id == result["choices"]["victory"],
                roleSelected=candidate_id == result["choices"]["role"],
            )


def describe_choice(record):
    if record["result"] is None:
        return "미평가"
    chosen = record["result"]["choices"]
    return f"승리 {chosen['victory']} / 역할 {chosen['role']}"


def md_cell(value):
    return str(value).replace("|", "\\|").replace("\n", " ")


def markdown_report(records, metadata):
    revised = {row["situationId"]: row for row in records if row["profile"] == "revised"}
    lines = [
        "# 룰 AI 오프라인 비교 결과",
        "",
        f"실행 UTC: {metadata['createdUtc']}",
        "",
        "synthetic 입력의 가치 평가 결과다. 실제 Core 합법성·승률·재미를 입증하지 않는다.",
        "",
        "## 기본 설정의 기대 판단 확인",
        "",
        "| 상황 | 기대 판단 | 실제 선택 | 판정 |",
        "|---|---|---|---|",
    ]
    for record in revised.values():
        expected = record["expected"] or {}
        expectation = "미평가" if expected.get("status") == "unevaluated" else (
            f"승리 {expected.get('victory', [])} / 역할 {expected.get('role', [])}"
        )
        lines.append("| " + " | ".join(md_cell(x) for x in (
            record["situationId"], expectation, describe_choice(record),
            "통과" if record["expectedMatch"] else record["expectedNote"],
        )) + " |")
    lines += ["", "기대 이유와 미평가 사유:", ""]
    for record in revised.values():
        reason = (record["expected"] or {}).get("reason", "기대 이유 없음")
        lines.append(f"- {record['situationId']}: {reason}")
        if record["error"]:
            lines.append(f"  - 미평가 사유: {record['error']}")
    comparison, sensitivity = [], []
    for record in records:
        if record["profile"] == "revised":
            continue
        baseline = revised[record["situationId"]]
        before = baseline["result"]["choices"] if baseline["result"] else None
        after = record["result"]["choices"] if record["result"] else None
        if before == after and baseline["status"] == record["status"]:
            continue
        target = comparison if record["profile"] in ("legacy", "main_only", "risk_only") else sensitivity
        target.append(f"- {record['situationId']} / {record['profile']}: {describe_choice(baseline)} → {describe_choice(record)}")
    lines += ["", "## 이전식과 개별 변경 비교", ""]
    lines += comparison or ["선택이 달라진 상황 없음."]
    lines += ["", "## 한 항목 변경 시 선택이 달라진 상황", ""]
    lines += sensitivity or ["선택이 달라진 상황 없음."]
    failures = sum(not row["expectedMatch"] for row in revised.values())
    lines += ["", f"기본 설정: {len(revised)}개 중 기대 불일치 {failures}개.", "", "입력 원문·설정·해시와 항목별 점수는 같은 폴더의 JSON·CSV에 보관했다.", ""]
    return "\n".join(lines)


def create_run_dir(output):
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    output.mkdir(parents=True, exist_ok=True)
    run_dir = output / stamp
    run_dir.mkdir(exist_ok=False)
    return run_dir


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            try:
                stream.reconfigure(encoding="utf-8")
            except (OSError, ValueError):
                pass
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("case", nargs="?", help="상황 ID 또는 파일명(예: R01)")
    parser.add_argument("--sweep", action="store_true", help="이전식·한 항목 계수 변경도 비교")
    parser.add_argument("--weights", type=Path, default=ROOT / "config" / "rule_weights.json")
    parser.add_argument("--output", type=Path, default=ROOT / "results" / "rules", help="매 실행의 고유 UTC 폴더를 만들 상위 경로")
    parser.add_argument("--list", action="store_true", help="상황 목록만 표시")
    args = parser.parse_args(argv)
    paths = sorted((ROOT / "rule_fixtures").glob("*.json"))
    if not paths:
        parser.error("rule_fixtures에 상황 JSON이 없습니다.")
    sources, ids = [], set()
    try:
        for path in paths:
            case = load_json(path)
            if not isinstance(case, dict):
                raise PolicyInputError(f"{path.name}: 상황은 객체여야 합니다.")
            case_id = case.get("situationId", path.stem)
            if not isinstance(case_id, str) or case_id in ids:
                raise PolicyInputError(f"중복되거나 잘못된 상황 ID: {case_id}")
            ids.add(case_id)
            sources.append((path, case))
        if args.case:
            query = args.case.casefold().removesuffix(".json")
            sources = [(path, case) for path, case in sources if query in (path.stem.casefold(), case.get("situationId", path.stem).casefold())]
            if not sources:
                parser.error(f"상황을 찾을 수 없습니다: {args.case}")
        if args.list:
            for path, case in sources:
                print(f"{case.get('situationId', path.stem)}: {case.get('title', path.stem)}")
            return 0
        settings = load_json(args.weights)
        validate_weights(settings)
        variants = list(profiles(settings, args.sweep))
        for _, setting in variants:
            validate_weights(setting)
    except (OSError, ValueError) as exc:
        print(f"입력 오류: {exc}", file=sys.stderr)
        return 2

    run_dir = create_run_dir(args.output)
    input_dir = run_dir / "inputs"
    input_dir.mkdir()
    source_info = []
    for path, case in sources:
        raw = path.read_bytes()
        (input_dir / path.name).write_bytes(raw)
        source_info.append((path, case, digest(raw)))
    settings_raw = args.weights.read_bytes()
    (run_dir / "settings.source.json").write_bytes(settings_raw)
    profile_settings = {}
    records = []
    for profile, setting in variants:
        canonical = json_text(setting).encode("utf-8")
        profile_settings[profile] = {"sha256": digest(canonical), "settings": setting}
        for path, case, raw_hash in source_info:
            records.append(evaluate_one(case, setting, profile, path, raw_hash))
    metadata = {
        "createdUtc": datetime.now(timezone.utc).isoformat(),
        "weightsFile": str(args.weights.resolve()),
        "weightsSourceSha256": digest(settings_raw),
        "runnerSha256": digest(Path(__file__).read_bytes()),
        "evaluatorSha256": digest(Path(rule_policy.__file__).read_bytes()),
        "profiles": profile_settings,
        "results": records,
    }
    (run_dir / "results.json").write_text(json_text(metadata), encoding="utf-8", newline="\n")
    columns = [
        "profile", "case", "id", "status", "excludedReason", "outcome", "W", "Q", "Role", "roleEligible",
        "contributions", "responseCapacityDamage", "responseAppliedDamage", "responseAttacks", "responseTrace",
        "victoryExpected", "roleExpected", "victorySelected", "roleSelected", "expectedMatch",
    ]
    with (run_dir / "candidates.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=columns)
        writer.writeheader()
        writer.writerows(csv_rows(records))
    (run_dir / "report.md").write_text(markdown_report(records, metadata), encoding="utf-8", newline="\n")
    failures = sum(row["profile"] == "revised" and not row["expectedMatch"] for row in records)
    print(f"{len(sources)}개 상황 · {len(variants)}개 설정 · 기본 기대 불일치 {failures}개")
    for record in records:
        if record["profile"] == "revised":
            print(f"{record['situationId']}: {describe_choice(record)} · 기대 {'일치' if record['expectedMatch'] else '불일치'}")
    print(f"결과: {run_dir.resolve()}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
