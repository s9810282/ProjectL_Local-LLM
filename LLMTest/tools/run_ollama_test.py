"""fixtures/*.json 을 Ollama에 돌려 results/<모델>.csv 로 저장한다.
사용법: python tools/run_ollama_test.py [모델이름]
경로는 이 스크립트 기준(LLMTest/)이라 어느 폴더에서 실행해도 된다."""
import csv, glob, json, os, sys, time, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
URL = "http://localhost:11434/api/chat"
MODEL = sys.argv[1] if len(sys.argv) > 1 else "hf.co/unsloth/Qwen3.5-4B-GGUF:Q4_K_M"
OPTIONS = {"temperature": 0, "seed": 42, "num_predict": 200}   # 실험 조건. 바꾸면 기록할 것
with open(os.path.join(ROOT, "prompts", "system.txt"), encoding="utf-8") as f:
    SYSTEM = f.read()


def schema(n):
    return {
        "type": "object",
        "properties": {
            "scores": {"type": "array", "items": {"type": "integer", "minimum": 0, "maximum": 10},
                       "minItems": n, "maxItems": n},
            "choice": {"type": "integer", "enum": list(range(n))},
            "reason": {"type": "string", "maxLength": 80},
        },
        "required": ["scores", "choice", "reason"],
    }


def ask(s):
    n = len(s["candidates"])
    # ruleChoice, _note 등 비교용 필드는 보내지 않는다
    user = json.dumps({"self": s["self"], "candidates": s["candidates"]}, ensure_ascii=False)
    body = {
        "model": MODEL,
        "messages": [{"role": "system", "content": SYSTEM}, {"role": "user", "content": user}],
        "stream": False,
        "think": False,
        "format": schema(n),
        "options": OPTIONS,
    }
    req = urllib.request.Request(URL, data=json.dumps(body).encode("utf-8"),
                                 headers={"Content-Type": "application/json"})
    t0 = time.perf_counter()
    with urllib.request.urlopen(req, timeout=180) as r:
        res = json.load(r)
    ms = (time.perf_counter() - t0) * 1000
    return res["message"]["content"], ms, res.get("eval_count", 0)


def validate(raw, n):
    """스키마 제약이 무시될 수 있으므로 코드에서 다시 검증한다(가이드 원칙 6). 실패 사유를 돌려준다."""
    try:
        d = json.loads(raw)
    except json.JSONDecodeError:
        return None, "parse"
    sc = d.get("scores")
    if not isinstance(sc, list) or len(sc) != n or not all(isinstance(x, int) and 0 <= x <= 10 for x in sc):
        return d, "scores"
    if not isinstance(d.get("choice"), int) or not 0 <= d["choice"] < n:
        return d, "choice"
    return d, ""


def main():
    files = sorted(glob.glob(os.path.join(ROOT, "fixtures", "*.json")))
    situations = []
    for p in files:
        with open(p, encoding="utf-8") as f:
            situations.append(json.load(f))
    if not situations:
        sys.exit("fixtures/*.json 이 없습니다.")
    print(f"model: {MODEL} | options: {OPTIONS}")
    print("warmup...")
    ask(situations[0])  # 첫 요청은 모델 로딩 시간이 섞이므로 버린다

    os.makedirs(os.path.join(ROOT, "results"), exist_ok=True)
    out = os.path.join(ROOT, "results", MODEL.replace("/", "_").replace(":", "_") + ".csv")
    with open(out, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["situation", "valid", "choice", "rule_choice", "agree_rule", "choice_is_max",
                    "scores", "latency_ms", "out_tokens", "reason", "raw"])
        for s in situations:
            sid = s.get("situationId", "?")
            try:
                raw, ms, tokens = ask(s)
            except Exception as e:
                w.writerow([sid, "error", "", "", "", "", "", "", "", "", str(e)])
                print(f"{sid}: ERROR {e}")
                continue
            d, err = validate(raw, len(s["candidates"]))
            rule = s.get("ruleChoice")
            if err:
                w.writerow([sid, err, "", rule, "", "", "", round(ms), tokens, "", raw])
                print(f"{sid}: INVALID({err}) {ms:.0f}ms | {raw}")
                continue
            sc, ch = d["scores"], d["choice"]
            w.writerow([sid, "ok", ch, rule, "" if rule is None else ch == rule, sc[ch] == max(sc),
                        " ".join(map(str, sc)), round(ms), tokens, d.get("reason", ""), raw])
            print(f"{sid}: choice={ch} rule={rule} scores={sc} {ms:.0f}ms | {d.get('reason', '')}")
    print("saved:", out)


if __name__ == "__main__":
    main()
