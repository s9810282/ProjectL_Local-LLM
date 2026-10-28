"""fixtures/S*.json 을 Ollama에 보내 응답 원문·형식 오류·지연을 저장한다.

사용법:
  python LLMTest/tools/run_ollama_test.py            # 모든 상황(일괄)
  python LLMTest/tools/run_ollama_test.py S02        # 상황 하나(단일)
  python LLMTest/tools/run_ollama_test.py --model <이름>

매 실행은 results/llm/<UTC 시각>/ 을 새로 만들고 이전 결과를 덮어쓰지 않는다.
경로는 이 스크립트 기준(LLMTest/)이라 어느 폴더에서 실행해도 된다.
종료 코드: 0 모든 응답이 형식 검증 통과, 1 형식 오류·요청 실패가 하나라도 있음, 2 실행 준비 실패.
"""
import argparse
import csv
import hashlib
import json
import platform
import statistics
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
# Windows에서 localhost는 IPv6를 먼저 시도해 요청마다 약 2초가 늘어난다(2026-10-22 측정). IPv4 주소를 직접 쓴다.
SERVER = "http://127.0.0.1:11434"
DEFAULT_MODEL = "hf.co/unsloth/Qwen3.5-4B-GGUF:Q4_K_M"
OPTIONS = {"temperature": 0, "seed": 42, "num_predict": 200}   # 실험 조건. 바꾸면 기록에 남는다
TIMEOUT_S = 180


def schema(n, ids=None):
    """ids가 있으면 choice를 그 문자열 ID 중 하나로, 없으면 0~n-1 정수로 강제한다."""
    choice = {"type": "string", "enum": list(ids)} if ids else {"type": "integer", "enum": list(range(n))}
    return {
        "type": "object",
        "properties": {
            "scores": {"type": "array", "items": {"type": "integer", "minimum": 0, "maximum": 10},
                       "minItems": n, "maxItems": n},
            "choice": choice,
            "reason": {"type": "string", "maxLength": 80},
        },
        "required": ["scores", "choice", "reason"],
    }


def build_request(model, system, situation):
    # ruleChoice, _note 등 비교용 필드는 보내지 않는다
    user = json.dumps({"self": situation["self"], "candidates": situation["candidates"]}, ensure_ascii=False)
    return {
        "model": model,
        "messages": [{"role": "system", "content": system}, {"role": "user", "content": user}],
        "stream": False,
        "think": False,
        "format": schema(len(situation["candidates"])),
        "options": OPTIONS,
    }


def post(path, body=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    req = urllib.request.Request(SERVER + path, data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=TIMEOUT_S) as r:
        return json.load(r)


def validate(raw, n, done_reason, ids=None):
    """스키마 제약이 무시될 수 있으므로 코드에서 다시 검증한다(가이드 원칙 6).
    (해석된 객체 또는 None, 오류 종류, 설명)을 돌려준다. 오류 종류 ""는 통과다."""
    try:
        d = json.loads(raw)
    except json.JSONDecodeError as e:
        kind = "truncated" if done_reason == "length" else "parse"
        return None, kind, str(e)
    if not isinstance(d, dict):
        return None, "parse", "최상위가 객체가 아님"
    sc = d.get("scores")
    if not isinstance(sc, list) or len(sc) != n or not all(type(x) is int and 0 <= x <= 10 for x in sc):
        return d, "scores", f"정수 0~10 점수 {n}개가 필요"
    ch = d.get("choice")
    if ids is not None and ch not in ids:
        return d, "choice", f"후보 ID {list(ids)} 중 하나가 필요"
    if ids is None and (type(ch) is not int or not 0 <= ch < n):
        return d, "choice", f"0~{n - 1} 정수가 필요"
    if not isinstance(d.get("reason"), str) or not d["reason"].strip():
        return d, "reason", "비어 있지 않은 문자열이 필요"
    return d, "", ""


def seconds(response, key):
    return round(response.get(key, 0) / 1e9, 3)   # Ollama 시간 단위는 나노초


def timing(response):
    """Ollama 응답의 시간·토큰 통계."""
    eval_s = seconds(response, "eval_duration")
    out_tokens = response.get("eval_count", 0)
    return {"done_reason": response.get("done_reason", ""),
            "total_s": seconds(response, "total_duration"), "load_s": seconds(response, "load_duration"),
            "prompt_s": seconds(response, "prompt_eval_duration"), "eval_s": eval_s,
            "prompt_tokens": response.get("prompt_eval_count", 0), "out_tokens": out_tokens,
            "tokens_per_s": round(out_tokens / eval_s, 1) if eval_s > 0 else ""}


def send(body):
    """채팅 요청 하나를 보내고 (응답 또는 None, 클라이언트 측정 ms, 요청 오류)를 돌려준다."""
    t0 = time.perf_counter()
    try:
        response = post("/api/chat", body)
    except (urllib.error.URLError, TimeoutError, OSError, json.JSONDecodeError) as e:
        return None, round((time.perf_counter() - t0) * 1000), str(e)
    return response, round((time.perf_counter() - t0) * 1000), ""


def ask(model, system, situation):
    body = build_request(model, system, situation)
    return (body, *send(body))


def record(situation, response, wall_ms, request_error):
    sid = situation["situationId"]
    n = len(situation["candidates"])
    rule = situation.get("ruleChoice")
    row = {"situation": sid, "status": "", "detail": "", "choice": "", "rule_choice": rule, "agree_rule": "",
           "choice_is_max": "", "scores": "", "reason": "", "wall_ms": wall_ms}
    if request_error:
        row.update(status="request_error", detail=request_error)
        return row
    row.update(timing(response))
    raw = response.get("message", {}).get("content", "")
    row["raw"] = raw
    d, kind, detail = validate(raw, n, row["done_reason"])
    if kind:
        row.update(status=kind, detail=detail)
        return row
    sc, ch = d["scores"], d["choice"]
    row.update(status="ok", choice=ch, agree_rule="" if rule is None else ch == rule,
               choice_is_max=sc[ch] == max(sc), scores=" ".join(map(str, sc)), reason=d["reason"])
    return row


COLUMNS = ["situation", "status", "detail", "choice", "rule_choice", "agree_rule", "choice_is_max", "scores",
           "reason", "wall_ms", "total_s", "load_s", "prompt_s", "eval_s", "prompt_tokens", "out_tokens",
           "tokens_per_s", "done_reason", "raw"]


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description="고정 상황을 로컬 LLM에 보내 응답을 기록한다.")
    parser.add_argument("case", nargs="?", help="상황 ID(예: S02). 없으면 모든 상황")
    parser.add_argument("--model", default=DEFAULT_MODEL)
    parser.add_argument("--output", type=Path, default=ROOT / "results" / "llm", help="실행 폴더를 만들 상위 경로")
    parser.add_argument("--no-warmup", action="store_true", help="모델 로딩용 첫 요청을 생략")
    args = parser.parse_args()

    paths = sorted((ROOT / "fixtures").glob("S*.json"))
    if args.case:
        paths = [p for p in paths if p.stem.casefold() == args.case.casefold()]
    if not paths:
        print(f"상황을 찾지 못했습니다: {args.case or 'fixtures/S*.json'}", file=sys.stderr)
        return 2
    sources = [(p, p.read_bytes()) for p in paths]
    situations = [json.loads(raw.decode("utf-8")) for _, raw in sources]
    system_raw = (ROOT / "prompts" / "system.txt").read_bytes()
    system = system_raw.decode("utf-8")
    try:
        server_version = post("/api/version").get("version", "")
    except (urllib.error.URLError, OSError) as e:
        print(f"Ollama 서버에 연결하지 못했습니다({SERVER}): {e}", file=sys.stderr)
        return 2

    started = datetime.now(timezone.utc)
    run_dir = args.output / started.strftime("%Y%m%dT%H%M%S.%fZ")
    (run_dir / "requests").mkdir(parents=True)
    (run_dir / "responses").mkdir()
    print(f"model: {args.model} | options: {OPTIONS} | ollama {server_version}")

    warmup = None
    if not args.no_warmup:
        print("warmup...")   # 첫 요청은 모델 로딩 시간이 섞이므로 통계에서 뺀다
        _, response, wall_ms, error = ask(args.model, system, situations[0])
        warmup = {"situation": situations[0]["situationId"], "wall_ms": wall_ms, "error": error,
                  "load_s": seconds(response, "load_duration") if response else None}
        write_json(run_dir / "warmup.json", {"summary": warmup, "response": response})

    rows = []
    for situation in situations:
        sid = situation["situationId"]
        body, response, wall_ms, error = ask(args.model, system, situation)
        write_json(run_dir / "requests" / f"{sid}.json", body)
        write_json(run_dir / "responses" / f"{sid}.json", response if response is not None else {"error": error})
        row = record(situation, response, wall_ms, error)
        rows.append(row)
        if row["status"] == "ok":
            print(f"{sid}: choice={row['choice']} rule={row['rule_choice']} scores=[{row['scores']}] "
                  f"{wall_ms}ms | {row['reason']}")
        else:
            print(f"{sid}: {row['status'].upper()} {wall_ms}ms | {row['detail']} | {row.get('raw', '')}")

    with (run_dir / "results.csv").open("w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=COLUMNS, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)

    valid = [r for r in rows if r["status"] == "ok"]
    timed = [r["wall_ms"] for r in rows if r["status"] != "request_error"]
    summary = {
        "requests": len(rows), "valid": len(valid),
        "errors": {k: sum(1 for r in rows if r["status"] == k) for k in sorted({r["status"] for r in rows}) if k != "ok"},
        "agreeRule": sum(1 for r in valid if r["agree_rule"] is True),
        "choiceIsMax": sum(1 for r in valid if r["choice_is_max"] is True),
        "wallMsMedian": statistics.median(timed) if timed else None,
        "wallMsMax": max(timed) if timed else None,
    }
    write_json(run_dir / "run.json", {
        "startedUtc": started.isoformat(), "model": args.model, "options": OPTIONS, "ollamaVersion": server_version,
        "python": platform.python_version(), "timeoutSeconds": TIMEOUT_S, "warmup": warmup,
        "systemPromptSha256": hashlib.sha256(system_raw).hexdigest(),
        "fixtures": {p.name: hashlib.sha256(raw).hexdigest() for p, raw in sources},
        "summary": summary, "rows": rows,
    })
    print(f"유효 {summary['valid']}/{summary['requests']} · 오류 {summary['errors'] or '없음'} · "
          f"룰 일치 {summary['agreeRule']} · 지연 중앙값 {summary['wallMsMedian']}ms")
    print("결과:", run_dir)
    return 0 if len(valid) == len(rows) else 1


if __name__ == "__main__":
    sys.exit(main())
