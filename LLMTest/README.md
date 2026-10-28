# 엔진 없이 룰 AI 정책 비교하기

Python 3.10 이상이면 Unity·Ollama 서버·추가 패키지 설치 없이 승리 지향과 역할 지향 정책을 비교할 수 있다. 입력 JSON의 최종 피해·경로·위험 일정은 기획자가 제공하는 synthetic 가정이다. 게임 전투를 시뮬레이션하거나 Core와 같은 합법성을 증명하는 도구는 아니다.

## 실행

프로젝트 루트 `D:\Unity 6000.0.73f1\Local LLM`에서 실행한다. 스크립트는 자기 위치를 기준으로 입력을 찾으므로 다른 폴더에서는 스크립트의 절대 경로를 사용해도 된다.

```powershell
# 모든 상황을 기본 설정으로 비교
python LLMTest/tools/run_rule_test.py

# 이전식·개별 변경·가중치 민감도까지 비교
python LLMTest/tools/run_rule_test.py --sweep

# 반복 공격 위험 사례 하나만 비교
python LLMTest/tools/run_rule_test.py R04 --sweep

# 상황 목록 보기
python LLMTest/tools/run_rule_test.py --list

# 별도 설정 파일 사용
python LLMTest/tools/run_rule_test.py --weights LLMTest/config/rule_weights.json --sweep

# 입력·공식 경계 검증
python -m unittest discover -s LLMTest/tests -v
```

콘솔에는 상황별 두 정책의 선택과 기대 일치 여부가 나온다. 매 실행은 `results/rules/<고유 UTC 시각>/`을 만들고 이전 결과를 덮어쓰지 않는다. `--output`은 이 고유 폴더를 만들 상위 경로를 지정한다.

- `report.md`: 기대 판단과 실제 선택, 이전식 비교, 선택이 뒤집힌 민감도 사례.
- `candidates.csv`: 후보별 승리·역할 점수, 항목별 기여, 위험 일정, 제외 사유. UTF-8 BOM으로 저장한다.
- `results.json`: Decimal 점수 문자열, 프로필별 설정, 입력·설정·실행 코드 SHA256.
- `inputs/`, `settings.source.json`: 실행 당시 입력과 설정 원문.

미평가가 기대된 상황도 기록한다. 잘못된 AP 일정이나 관측 누락은 위험 0으로 바꾸지 않는다. 기본 설정의 기대 선택과 다르면 종료 코드 1이며, 입력 파일·설정 자체의 로딩 오류는 2다. 민감도 프로필이 기본 기대와 다른 것은 탐색 결과로 남기고 실행 실패로 취급하지 않는다.

## 설정과 상황 바꾸기

공식의 수치 원본은 [config/rule_weights.json](config/rule_weights.json)이다. `weights.main`은 메인 압박 계수, `riskDiscount`는 미래 손실 할인, `roleMaxLoss`는 역할을 위해 허용하는 전술 점수 차이, `roleBonus`는 역할 준수 보너스다. 나머지 항목을 유지한 채 하나씩 바꾸어 비교한다. 별도 실험은 설정 파일을 복사해 `--weights`로 지정한다.

새 상황은 `rule_fixtures`의 JSON 하나를 복사하고 `situationId`를 유일하게 만든다. 계산 전에 `expected`에 원하는 선택과 기획 이유를 적는다. 숫자를 바꾼 뒤 실패하면 항목별 점수와 입력 가정을 확인한다. 결과에 맞춰 기대값을 자동으로 고치는 기능은 없다.

[상황 형식](docs/rule-fixture-format.md)에 필수 항목과 AP 일정의 의미를 정리했다. `legal=true`는 작성자의 가정이며 도구는 비용 선지불·이동 예산 같은 일부 산술만 추가 확인한다. `fixed` 위험은 반복 중 최종 피해와 비용이 불변일 때 사용하고, 할인·환급·피해 변화는 `sequence`에 공격별 값을 적는다. Focus·Rider·넉백 규칙 자체를 Python에서 계산하지 않는다.

## 실험 자료

- [설계 v2](docs/2026-10-15-rule-ai-policy-design.md)
- [Phase A 계획과 실행 기록](docs/2026-10-15-rule-ai-offline-test-plan.md)
- [대표 상황 비교와 가중치 판단](docs/2026-10-15-rule-ai-offline-comparison.md)
- [오프라인 입력과 실제 전투 값의 대응](docs/2026-10-22-rule-ai-core-input-mapping.md)
- [특성·아이템 변형 상황과 가중치 재비교](docs/2026-10-22-rule-ai-variant-comparison.md)
- [로컬 LLM 실행 검증과 공통 비교 입력 초안](docs/2026-10-22-llm-run-and-common-input.md)
- [7주차 차주 계획 수행 정리와 발표·공유 안내](docs/2026-10-22-work-summary-and-presentation-guide.md)
- [인계: 다른 PC·새 세션에서 이어가기](docs/2026-10-29-llm-enemy-ai-handoff.md) — 새 세션은 이 문서부터 읽는다

기존 `fixtures/S01~S06`, `tools/ask_once.py`, `prompts`는 보존했다. 기존 Ollama 상황에는 새 점수식에 필요한 대상별 최대 HP·AP·위험 일정 등이 부족해 새 `rule_fixtures`와 형식을 분리했다.

## 로컬 LLM 실행

Ollama 서버(`localhost:11434`)와 모델이 필요하다. 추가 패키지는 없다.

```powershell
# S01~S06 일괄 (첫 요청은 모델 로딩용으로 통계에서 뺀다)
python LLMTest/tools/run_ollama_test.py

# 상황 하나
python LLMTest/tools/run_ollama_test.py S02

# 다른 모델
python LLMTest/tools/run_ollama_test.py --model <모델 이름>
```

매 실행은 `results/llm/<고유 UTC 시각>/`을 만든다.

- `requests/`, `responses/`: 상황별 보낸 요청과 Ollama 응답 원문(시간 통계 포함).
- `results.csv`: 선택·점수·이유, 형식 오류 종류(`parse`·`truncated`·`scores`·`choice`·`reason`·`request_error`), 지연(전체·로딩·입력·출력 초, 토큰 수, 토큰/초). UTF-8 BOM.
- `run.json`: 모델·옵션·Ollama 버전·system 프롬프트와 상황 파일 해시, 요약, warmup 기록.

`tools/ask_once.py`는 요청과 응답을 화면에 그대로 보여주는 학습용 도구로 남겼다.

## 룰 정책과 LLM 비교

`rule_fixtures/R*.json`을 원본으로, 룰 평가기와 LLM이 같은 후보를 판단한다. LLM 화면은 `tools/llm_projection.py`가 만든다. 가중치·점수·기대·label은 넣지 않고, 후보 ID는 `c1, c2…` 별칭으로 바꾼다.

```powershell
# 모든 R 상황, 승리·역할 지시문 둘 다
python LLMTest/tools/run_llm_compare.py

# 상황 하나, 지시문 하나
python LLMTest/tools/run_llm_compare.py R19 --policy victory

# 후보 순서 뒤집기·섞기(순서 불변 시험)
python LLMTest/tools/run_llm_compare.py --order reverse
python LLMTest/tools/run_llm_compare.py --order shuffle --seed 1

# 우선순위를 말로 적은 가이드판 지시문(prompts/compare.*.guide.txt)
python LLMTest/tools/run_llm_compare.py --prompt guide --order reverse

# 보류 검증 상황(validation_fixtures, 지시문·가중치 조정에 쓰지 않음)
python LLMTest/tools/run_llm_compare.py --fixtures LLMTest/validation_fixtures --prompt guide
python LLMTest/tools/run_rule_test.py --fixtures LLMTest/validation_fixtures

# 여러 실행의 선택을 원래 후보 ID로 나란히 비교
python LLMTest/tools/compare_llm_runs.py LLMTest/results/llm_compare/<실행1> LLMTest/results/llm_compare/<실행2>
```

LLM은 같은 입력에는 같은 답을 내지만 후보 순서만 바꿔도 판단이 바뀔 수 있다. 비교는 최소 파일 순서와 뒤집기 두 가지로 돌리고, 순서와 무관한 선택만 집계한다.

매 실행은 `results/llm_compare/<고유 UTC 시각>/`에 `projections/`(LLM 화면과 별칭 표), `requests/`, `responses/`, `results.csv`, `run.json`을 남긴다. 룰이 미평가로 둔 상황은 LLM에도 보내지 않는다. 지시문은 `prompts/compare.victory.txt`, `prompts/compare.role.txt`다.

현재 비교는 기획 기대와 계산의 일관성을 확인한다. 실제 승률, 상대 이동을 포함한 최적 행동, 역할 정책의 재미는 Core 연결과 플레이 검증이 필요하다.
