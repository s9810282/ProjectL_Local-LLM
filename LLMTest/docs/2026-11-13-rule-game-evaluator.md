# rule-game: 게임 적 AI 점수식 복제 평가기

작성일: 2026-11-13 · 상태: 1판 완료(F01~F06 평가, 시험 통과)

## 1 왜 만들었나

연구 구조를 둘로 나눈다(2026-11-13 사용자 결정).

| 평가기 | 역할 |
|---|---|
| **rule-game** | 비교 기준. 지금 게임에 들어 있는 적 AI(교환 모형 Utility)를 그대로 옮긴 것 |
| rule-v3 | 설계 재료. 위험 합산·확실성 단계·유닛 역할 층처럼 게임 AI에 없는 개념을 담고 있다 |

원본은 Project_L develop 424242ab8(이 저장소 c449c977)의 Core `EnemyUtilityScorer.cs`·`EnemyDecisionSession.cs`·`EnemyThreatField.cs`, 조정값은 `EnemyAiTuning_Default.asset`이다.

## 2 파일

| 파일 | 내용 |
|---|---|
| `tools/rule_game.py` | 평가기와 실행 명령. 점수식 상수는 게임 코드 상수라 여기에 둔다 |
| `config/rule_game_weights.json` | 조정값 셋. `asset`(20/20/200, 실제 게임)·`codeDefault`(50/50/50) |
| `rule_fixtures_game/F*.game.json` | 게임 전용 사실 덧붙임. 공통 사실은 `rule_fixtures_v2/`를 그대로 읽는다 |
| `tests/test_rule_game.py` | 식·선택 규칙·F01~F06 선택 시험 |

실행: `python LLMTest/tools/rule_game.py [상황] [--profile codeDefault]` → `results/rule_game/<UTC>/`

## 3 v2 상황을 게임 식으로 읽는 법

| 게임 사실 | v2에서 | 덧붙임 파일에서 |
|---|---|---|
| 상대 손실·처치 | `playerHpAfter` (메인도 일반 유닛과 같음) | |
| 우리 편 손실·사망 | `selfHpAfter` + `allyHpAfter` + `headquartersHpAfter`(적 본진) | |
| 플레이어 위험 | `exposure.threats` 중 **`in_place`만, 가장 센 한 방** | `killedThreats`: 이번 행동으로 죽은 플레이어의 위협(게임 위협 필드에는 남는다) |
| 기믹 | `exposure.hazardDamage`(확실한 피해로 봄) | `forecastBind` |
| 접근 진척 | | `engagementCostBefore`·`engagementCostAfter`(모든 대상 기준) |
| 다음 턴 공격 | | `nextTurn` 대상·피해(값은 결정 시작 HP로 계산) |
| 대기 | `kind: wait` 후보 | 대기 후보가 없으면 `hold` |
| 제약 변화 | | `restrictionDelta` |

`move_attack`은 게임에서 "이동" 하나다. 종점에서의 공격은 **후속 공격 값**으로 미리 더하고, 실제 공격은 다음 결정에서 한다. 그래서 이동 뒤 메인 처치도 이번 결정에서는 승패 등급을 받지 않는다.

## 4 결과(F01~F06)

| 상황 | 게임 AI (asset) | 게임 AI (codeDefault) | rule-v3 | 기획 기대 |
|---|---|---|---|---|
| F01 처치 대 메인 피해 | a 처치 | a | a | a |
| F02 확실성 단계 | **a 메인(몰아치면 죽음)** | b | c 후퇴 | b |
| F03 제자리 대 이동 뒤 메인 | **a 제자리** | b | b | b |
| F04 앞선 결과 반영 | a | a | a | a |
| F04b Sniper 생존 대조 | **a 공격(6+4로 죽음)** | **a** | b 후퇴 | b |
| F05 낙석 예고 | b | b | b | b |
| F06 KillChain | a | a | a | a |
| 기대와 일치 | 4/7 | 6/7 | 6/7 | |

읽는 법:
- **F02·F04b:** 게임 위협 필드는 가장 센 한 방만 본다. 여러 명이 함께 치면 죽는 자리를 안전하다고 본다. F04b는 조정값과 상관없이 틀린다.
- **F02·F03:** 다음 턴 공격 200%가 갈랐다. 종점에서 다음 턴에 칠 대상의 값이 두 배로 들어가서, 메인이나 제자리처럼 값이 큰 쪽으로 기운다. 코드 기본값(50%)이면 기대와 같다.

## 4.1 정정(2026-11-13, 시뮬레이터 S-d 중 확인)

- **승패 등급(±1)은 게임에서 붙지 않는다.** `GuaranteedTerminal`은 평가에 불확실성이 하나도 없을 때만 참인데(`EnemyUtilityScorer.cs:24`), 모든 평가가 `HiddenEnvironment`를 달고 시작한다(`CombatActionAssessment.cs:99`, 대기는 `EnemyDecisionSession.cs:182`). 1판은 "제자리 메인 처치 = 등급 1"과 "적 승리면 치명 아님"을 넣었는데 둘 다 걷었다. 메인 처치도 점수(1000‰ + 2000)로만 본다.
- F01~F06의 게임 AI 선택은 바뀌지 않았다(에셋 4/7, 코드 기본값 6/7). 우리 시험 `test_in_place_kill_of_main_is_top_tier`는 `test_main_kill_gets_no_terminal_tier`로 고쳤다.
- 이 평가기는 모든 후보를 정밀 평가로 본다. 게임은 정밀 예산(결정당 16, 유닛당 8)이 있고, 예산 밖의 어림 평가에는 처치 +2000·제약 +500이 없으며 피해에 방어를 빼지 않는다. 실제 판 위의 동작은 시뮬레이터 `sim/enemy/game_ai.py`가 이 절차까지 옮겼다.

## 5 한계

- 덧붙임 파일의 값(교전 비용, 다음 턴 대상, F02 대기 칸 위협)은 지도가 없는 synthetic 상황에 대한 가정이다. 각 파일의 `assumptions`에 근거를 적었다. 특히 F02·F03은 다음 턴 값에 따라 답이 바뀐다.
- 게임은 이동 후보를 빔(6)으로 줄이고 상위 2개만 정밀 평가한다. 여기서는 모두 정밀로 본다(이동 3개 이상이면 경고).
- 동점이면 게임은 칸 좌표로 정하지만 상황 파일에는 좌표가 없어 후보 ID로 정한다(경고).
- 행위자 순서(지금 칠 수 있는 유닛 먼저)·도발 의무·여러 칸 유닛은 다루지 않는다.
- 위협 피해는 v2 값(방어 반영)이다. 게임 위협 필드는 공통 피해(방어 전)를 쓴다.
- 게임 코드가 바뀌면 이 파일도 고쳐야 한다. 실제 게임 진단 기록과 한 번 맞춰 보는 것이 다음 확인 단계다.

## 6 v2 상황 파일에서 찾은 것(고치지 않음)

- F06: `a_hit_main`이 출발 칸에서 메인을 치는데 `approachCostBefore`는 2다. 덧붙임 파일에서는 0으로 둔다.
- F04·F04b·F06: 후퇴 칸의 `approachCostAfter` 4 ≤ 이동력 4인데 `canThreatenNext`는 false다. 게임 규칙으로는 다음 턴 공격이 잡힌다.
