"""상황(fixture) 하나를 Ollama에 보내고, 요청과 응답을 그대로 출력한다. 2단계 학습용.
사용법: python tools/ask_once.py [상황이름]   (기본 S02)"""
import json, os, sys, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # LLMTest 폴더
NAME = sys.argv[1] if len(sys.argv) > 1 else "S02"

# 1. 재료 읽기: 규칙(system 프롬프트)과 문제(fixture)
with open(os.path.join(ROOT, "prompts", "system.txt"), encoding="utf-8") as f:
    system = f.read()
with open(os.path.join(ROOT, "fixtures", NAME + ".json"), encoding="utf-8") as f:
    fixture = json.load(f)

# 2. 모델에게 보낼 상황: self와 candidates만. ruleChoice와 _note는 빼야 공정하다
n = len(fixture["candidates"])
situation = json.dumps({"self": fixture["self"], "candidates": fixture["candidates"]}, ensure_ascii=False)

# 3. 요청 만들기
request = {
    "model": "hf.co/unsloth/Qwen3.5-4B-GGUF:Q4_K_M",
    "messages": [
        {"role": "system", "content": system},
        {"role": "user", "content": situation},   # JSON을 '문자열'로 넣는다
    ],
    "stream": False,
    "think": False,
    "format": {   # 응답 모양 강제: 점수 n개, 선택은 0~n-1 중 하나, 이유는 글자
        "type": "object",
        "properties": {
            "scores": {"type": "array", "items": {"type": "integer", "minimum": 0, "maximum": 10},
                       "minItems": n, "maxItems": n},
            "choice": {"type": "integer", "enum": list(range(n))},
            "reason": {"type": "string", "maxLength": 80},
        },
        "required": ["scores", "choice", "reason"],
    },
    "options": {"temperature": 0, "seed": 42, "num_predict": 200},
}
print("=== 보내는 요청 ===")
print(json.dumps(request, ensure_ascii=False, indent=2))

# 4. 보내기 (HTTP POST)
req = urllib.request.Request("http://localhost:11434/api/chat",
                             data=json.dumps(request).encode("utf-8"),
                             headers={"Content-Type": "application/json"})
with urllib.request.urlopen(req, timeout=180) as r:
    response = json.load(r)

# 5. 응답 원문
print("\n=== 받은 응답 원문 ===")
print(json.dumps(response, ensure_ascii=False, indent=2))

# 6. content는 JSON '문자열'이라 한 번 더 파싱해야 객체가 된다
answer = json.loads(response["message"]["content"])
print("\n=== 해석 ===")
print(f"점수: {answer['scores']}")
print(f"선택: {answer['choice']}   (룰 AI의 답: {fixture.get('ruleChoice')})")
print(f"이유: {answer['reason']}")
print(f"멈춘 이유: {response.get('done_reason')}   (length면 num_predict에 걸려 잘린 것)")

# 7. 통계: API의 시간 단위는 나노초 → 초로 바꾼다
s = lambda key: response.get(key, 0) / 1e9
print("\n=== 통계 ===")
print(f"전체 {s('total_duration'):.2f}초 = 로딩 {s('load_duration'):.2f} + 입력 {s('prompt_eval_duration'):.2f} + 출력 {s('eval_duration'):.2f}")
print(f"입력 토큰 {response.get('prompt_eval_count')}개, 출력 토큰 {response.get('eval_count')}개")
if s("eval_duration") > 0:
    print(f"출력 속도 {response['eval_count'] / s('eval_duration'):.1f} 토큰/초")
