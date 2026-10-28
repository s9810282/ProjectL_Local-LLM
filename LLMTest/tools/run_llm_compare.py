"""rule_fixtures/R*.json 을 룰 정책과 로컬 LLM이 같은 후보로 판단하게 하고 선택을 비교한다.

사용법:
  python LLMTest/tools/run_llm_compare.py                  # 모든 R 상황, 승리·역할 지시문 둘 다
  python LLMTest/tools/run_llm_compare.py R19              # 상황 하나
  python LLMTest/tools/run_llm_compare.py --policy victory # 지시문 하나만
  python LLMTest/tools/run_llm_compare.py --order reverse  # 후보 순서 뒤집기(순서 불변 시험)
  python LLMTest/tools/run_llm_compare.py --order shuffle --seed 1
  python LLMTest/tools/run_llm_compare.py --prompt guide   # 우선순위를 말로 적은 가이드판 지시문

LLM 입력은 llm_projection.project가 R 파일에서 뽑는다. 룰이 미평가로 둔 상황은 LLM에도 보내지 않는다.
매 실행은 results/llm_compare/<UTC 시각>/ 을 새로 만든다.
종료 코드: 0 모든 응답이 형식 검증 통과, 1 형식 오류·요청 실패가 하나라도 있음, 2 실행 준비 실패.
"""
import argparse
import csv
import hashlib
import json
import platform
import statistics
import sys
import urllib.error
from datetime import datetime, timezone
from pathlib import Path

from llm_projection import ORDERS, project
from rule_policy import PolicyInputError, load_json, to_jsonable, validate_weights
from run_ollama_test import DEFAULT_MODEL, OPTIONS, TIMEOUT_S, post, schema, send, timing, validate, write_json

ROOT = Path(__file__).resolve().parents[1]
POLICIES = ("victory", "role")
PROMPTS = {"basic": "compare.{policy}.txt", "guide": "compare.{policy}.guide.txt"}
COLUMNS = ["situation", "policy", "status", "detail", "llm_choice", "llm_alias", "rule_choice", "expected",
           "match_rule", "match_expected", "choice_is_max", "candidates", "scores", "reason", "wall_ms",
           "total_s", "load_s", "prompt_s", "eval_s", "prompt_tokens", "out_tokens", "tokens_per_s",
           "done_reason", "raw"]


def build_request(model, system, view):
    ids = [c["id"] for c in view["candidates"]]
    return {
        "model": model,
        "messages": [{"role": "system", "content": system},
                     {"role": "user", "content": json.dumps(view, ensure_ascii=False)}],
        "stream": False,
        "think": False,
        "format": schema(len(ids), ids),
        "options": OPTIONS,
    }


def compare(case, policy, view, aliases, result, response, wall_ms, error):
    expected = case["expected"].get(policy, [])
    rule = result["choices"][policy]
    row = {"situation": case["situationId"], "policy": policy, "rule_choice": rule,
           "expected": " ".join(expected), "candidates": len(aliases), "wall_ms": wall_ms}
    if error:
        row.update(status="request_error", detail=error)
        return row
    row.update(timing(response))
    raw = response.get("message", {}).get("content", "")
    row["raw"] = raw
    ids = list(aliases)
    d, kind, detail = validate(raw, len(ids), row["done_reason"], ids)
    if kind:
        row.update(status=kind, detail=detail)
        return row
    choice = aliases[d["choice"]]
    scores = d["scores"]
    row.update(status="ok", llm_alias=d["choice"], llm_choice=choice, match_rule=choice == rule,
               match_expected=choice in expected, choice_is_max=scores[ids.index(d["choice"])] == max(scores),
               scores=" ".join(f"{alias}:{aliases[alias]}={score}" for alias, score in zip(ids, scores)),
               reason=d["reason"])
    return row


def summarize(rows):
    summary = {}
    for policy in POLICIES:
        part = [r for r in rows if r["policy"] == policy]
        if not part:
            continue
        valid = [r for r in part if r["status"] == "ok"]
        timed = [r["wall_ms"] for r in part if r["status"] != "request_error"]
        summary[policy] = {
            "requests": len(part), "valid": len(valid),
            "errors": {k: sum(1 for r in part if r["status"] == k) for k in sorted({r["status"] for r in part}) if k != "ok"},
            "matchExpected": sum(1 for r in valid if r["match_expected"]),
            "matchRule": sum(1 for r in valid if r["match_rule"]),
            "choiceIsMax": sum(1 for r in valid if r["choice_is_max"]),
            "wallMsMedian": statistics.median(timed) if timed else None,
            "wallMsMax": max(timed) if timed else None,
        }
    return summary


def main():
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description="룰 정책과 로컬 LLM의 선택을 같은 후보로 비교한다.")
    parser.add_argument("case", nargs="?", help="상황 ID(예: R19). 없으면 모든 R 상황")
    parser.add_argument("--policy", choices=POLICIES + ("both",), default="both")
    parser.add_argument("--model", default=DEFAULT_MODEL)
    parser.add_argument("--weights", type=Path, default=ROOT / "config" / "rule_weights.json")
    parser.add_argument("--fixtures", type=Path, default=ROOT / "rule_fixtures", help="상황 폴더(보류 검증은 validation_fixtures)")
    parser.add_argument("--output", type=Path, default=ROOT / "results" / "llm_compare")
    parser.add_argument("--no-warmup", action="store_true", help="모델 로딩용 첫 요청을 생략")
    parser.add_argument("--order", choices=ORDERS, default="file", help="LLM에 보여줄 후보 순서")
    parser.add_argument("--seed", type=int, default=0, help="--order shuffle의 순서 seed")
    parser.add_argument("--prompt", choices=tuple(PROMPTS), default="basic", help="지시문 판: 목표만(basic) 또는 우선순위 포함(guide)")
    args = parser.parse_args()
    policies = POLICIES if args.policy == "both" else (args.policy,)
    order = {"order": args.order, **({"seed": args.seed} if args.order == "shuffle" else {})}

    try:
        settings = load_json(args.weights)
        validate_weights(settings)
        paths = sorted(args.fixtures.glob("*.json"))
        if args.case:
            paths = [p for p in paths if p.stem.casefold() == args.case.casefold()]
        if not paths:
            raise PolicyInputError(f"상황을 찾지 못했습니다: {args.case or args.fixtures}")
        sources = [(p, p.read_bytes(), load_json(p)) for p in paths]
        prompts = {policy: (ROOT / "prompts" / PROMPTS[args.prompt].format(policy=policy)).read_bytes() for policy in policies}
        server_version = post("/api/version").get("version", "")
    except (OSError, ValueError, urllib.error.URLError) as e:
        print(f"실행 준비 실패: {e}", file=sys.stderr)
        return 2

    started = datetime.now(timezone.utc)
    run_dir = args.output / started.strftime("%Y%m%dT%H%M%S.%fZ")
    for name in ("projections", "requests", "responses"):
        (run_dir / name).mkdir(parents=True)
    print(f"model: {args.model} | options: {OPTIONS} | ollama {server_version} | policies: {', '.join(policies)} | prompt {args.prompt} | {order}")

    jobs, skipped = [], []
    for path, _, case in sources:
        try:
            view, aliases, result = project(case, settings, args.order, args.seed)
        except PolicyInputError as e:
            skipped.append({"situation": case["situationId"], "reason": str(e)})   # 룰과 같은 상황 집합만 비교한다
            print(f"{case['situationId']}: 룰 미평가라 보내지 않음 | {e}")
            continue
        write_json(run_dir / "projections" / f"{case['situationId']}.json",
                   {"view": view, "aliases": aliases, "ruleChoices": result["choices"]})
        jobs.append((case, view, aliases, result))

    warmup = None
    if jobs and not args.no_warmup:
        print("warmup...")   # 첫 요청은 모델 로딩 시간이 섞이므로 통계에서 뺀다
        case, view = jobs[0][0], jobs[0][1]
        response, wall_ms, error = send(build_request(args.model, prompts[policies[0]].decode("utf-8"), view))
        warmup = {"situation": case["situationId"], "wall_ms": wall_ms, "error": error,
                  "load_s": timing(response)["load_s"] if response else None}

    rows = []
    for case, view, aliases, result in jobs:
        sid = case["situationId"]
        for policy in policies:
            body = build_request(args.model, prompts[policy].decode("utf-8"), view)
            response, wall_ms, error = send(body)
            write_json(run_dir / "requests" / f"{sid}.{policy}.json", body)
            write_json(run_dir / "responses" / f"{sid}.{policy}.json", response if response is not None else {"error": error})
            row = compare(case, policy, view, aliases, result, response, wall_ms, error)
            rows.append(row)
            if row["status"] == "ok":
                mark = "기대 일치" if row["match_expected"] else "기대 불일치"
                print(f"{sid} {policy:7}: LLM {row['llm_choice']} · 룰 {row['rule_choice']} · {mark} · {wall_ms}ms | {row['reason']}")
            else:
                print(f"{sid} {policy:7}: {row['status'].upper()} {wall_ms}ms | {row['detail']} | {row.get('raw', '')}")

    with (run_dir / "results.csv").open("w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=COLUMNS, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)
    summary = summarize(rows)
    tools = ROOT / "tools"
    write_json(run_dir / "run.json", to_jsonable({
        "startedUtc": started.isoformat(), "model": args.model, "options": OPTIONS, "ollamaVersion": server_version,
        "python": platform.python_version(), "timeoutSeconds": TIMEOUT_S, "warmup": warmup,
        "weightsVersion": settings["version"], "candidateOrder": order, "promptVariant": args.prompt,
        "fixturesDir": args.fixtures.resolve().name,
        "promptSha256": {policy: hashlib.sha256(raw).hexdigest() for policy, raw in prompts.items()},
        "codeSha256": {name: hashlib.sha256((tools / name).read_bytes()).hexdigest()
                       for name in ("run_llm_compare.py", "llm_projection.py", "rule_policy.py", "run_ollama_test.py")},
        "fixtures": {p.name: hashlib.sha256(raw).hexdigest() for p, raw, _ in sources},
        "skipped": skipped, "summary": summary, "rows": rows,
    }))
    for policy, s in summary.items():
        print(f"[{policy}] 유효 {s['valid']}/{s['requests']} · 오류 {s['errors'] or '없음'} · 기대 일치 {s['matchExpected']} · "
              f"룰 일치 {s['matchRule']} · 지연 중앙값 {s['wallMsMedian']}ms")
    print("결과:", run_dir)
    return 0 if all(r["status"] == "ok" for r in rows) else 1


if __name__ == "__main__":
    sys.exit(main())
