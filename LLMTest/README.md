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

기존 `fixtures/S01~S06`, `tools/ask_once.py`, `tools/run_ollama_test.py`, `prompts`는 보존했다. 기존 Ollama 상황에는 새 점수식에 필요한 대상별 최대 HP·AP·위험 일정 등이 부족해 새 `rule_fixtures`와 형식을 분리했다.

현재 비교는 기획 기대와 계산의 일관성을 확인한다. 실제 승률, 상대 이동을 포함한 최적 행동, 역할 정책의 재미는 Core 연결과 플레이 검증이 필요하다.
