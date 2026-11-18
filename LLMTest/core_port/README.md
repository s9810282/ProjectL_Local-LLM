# 턴 계획 AI의 게임 Core 판 (연구 저장소 전용)

연구 시뮬레이터의 적 턴 계획 AI(`plan4:g1`)를 게임 Core에 옮긴 것이다. 설계·계획·구현 기록은 [Core 이식 설계](../docs/2026-11-18-core-turn-planner-port-design.md)에 있다. 2026-11-18부터 깊은 V는 한 턴 더 보기(`plan4:g1:l1`, [한 턴 더 보기 설계](../docs/2026-11-18-planner-two-turn-lookahead-design.md) §10)다.

- **이 저장소(Local LLM)에만 있다.** 팀 저장소(Project_L)·develop에는 올리지 않았다(2026-11-18 사용자 결정).
- 기본은 지금 게임 적 AI(Utility)다. 조정 에셋에서 고를 때만 계획 AI가 돈다.

## 1 파일

| 자리 | 내용 |
|---|---|
| `Assets/ResearchEnemyPlanner/` | 계획 AI 코드 전부(+ `.meta`). `ResearchEnemyPlanner.asmref`로 `ProjectL.Core.Application` 어셈블리에 붙는다 |
| `LLMTest/core_port/hooks.patch` | 기존 게임 파일 여섯 곳에 넣은 연결 줄(아래 표) |
| `LLMTest/core_port/harness/` | Unity 밖에서 Core를 묶어 계획 AI를 돌려 보는 측정 도구(저장소 시험이 아니다) |

계획 AI 코드(`Assets/ResearchEnemyPlanner/`):

| 파일 | 하는 일 | 시뮬레이터 원본 |
|---|---|---|
| `EnemyTurnPlanner.cs` | 세션이 부르는 입구. 보관 계획 확인, 계획, 2단 평가, 거절 처리, 예외가 나면 지금 AI로 | `planner.PlannerPolicy` |
| `EnemyTurnPlannerConfig.cs` | 고정 값 둘(끝난 판 값, 계산 스레드 우선순위 Lowest). 성향·계산 크기 값은 조정 에셋 프리셋으로 옮김(아래 §2) | `PlannerConfig` |
| `EnemyTurnPlan.cs` | 한 적 턴의 계획(행동마다 행동 전 판 서명)과 계획 수치 | — |
| `PlannerBoard.cs` | 공정 입력(적이 모르는 칸 빼기 + 난수 씨앗), 판 서명, 입력 다시 만들기, 행동 돌리기(`PlannerSimulator`) | `planner.signature`, `prediction_seed` |
| `PlannerEvaluator.cs` | 빠른 V(자산·위험·기회·거리 벌점·G1), 도달 표, 예상 피해, 빠른 이동 점수 | `evaluate.Evaluator` |
| `PlannerCandidates.cs` | 유닛 하나의 후보(제자리 공격·이동·이동 + 공격, 도발 규칙) | `analysis.unit_options` |
| `PlannerBeam.cs` | 빔 탐색, 다시 매김 후보 모음 | `PlannerPolicy._make_plan` |
| `PlannerReply.cs` | 깊은 V: 한 턴 더 보기(위험 판에서 플레이어 유닛을 차례대로 둔 뒤 판의 빠른 V). 옛 깊은 V(위험 판·기회 판에서 따로 고른 응수)도 남김 | `reply.py` |

기존 파일에 넣은 연결 줄(`hooks.patch`):

| 파일 | 넣은 것 |
|---|---|
| `…/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs` | 열거형 `EnemyDecisionModel { Utility, TurnPlanner }`, 속성 `DecisionModel`·`PlannerStyle`, 생성자 끝 선택 인자, 성향 값 묶음 `EnemyTurnPlannerStyle`(기본값의 원본) |
| `…/Application/Combat/EnemyAi/EnemyDecisionSession.cs` | 생성자 갈래: `TurnPlanner`면 `EnemyTurnPlanner.Steps`(설정의 성향 값을 넘김), 일 스레드를 기다리는 중 표시 `IsWaiting` |
| `…/Application/Combat/EnemyAi/EnemyDecisionSideCache.cs` | 턴 계획 보관 칸 `TurnPlan`, 범위가 바뀌면 비움 |
| `…/DataBaseSystem/Data/InGame/Table/EnemyAi/Item/EnemyAiTuningData.cs` | 값 `DecisionModel`·`PlannerStyle`, `ToSettings()`가 넘김 |
| `…/DataBaseSystem/Data/InGame/Table/EnemyAi/Item/EnemyAiTuningDataSO.cs` | 인스펙터 칸 "적 AI 종류 (연구용)", 성향 프리셋 칸과 그 위 판 점수 식 상자 |
| `…/DataBaseSystem/Data/InGame/Table/EnemyAi/Item/EnemyTurnPlannerPresetSO.cs`(새 파일) | 성향 프리셋 입력 SO, 판 점수 식 `Formula`, 설명 상자 속성·그리기 |
| `Assets/ProjectL/DataBaseSO/InGameGroup/EnemyAi/Item/EnemyTurnPlannerPreset_Default.asset`(새 파일) | 기본 프리셋(값 = 기본값) |
| `Assets/ProjectL/DataBaseSO/InGameGroup/EnemyAi/Item/EnemyAiTuning_Default.asset` | `decisionModel: 1`(TurnPlanner), `plannerPreset` = 기본 프리셋 |
| `…/CoreSystem/CombatSystem/CombatAdvanceService.cs` | 계획 AI가 일 스레드를 기다리는 동안 그 프레임 준비를 멈춤, 새 계획이 나오면 콘솔에 시간·쓴 성향 값 한 줄(2026-11-18 BG-a. 그 전의 12ms 예산 줄은 걷음, 예산은 모두 1ms). 기다리는 동안 프레임마다 시간과 GC 수집 여부를 모아 계획이 끝날 때 한 줄로 냄(프레임 진단, 병렬 설계 §13.1. 그 전의 GC 조각 1ms 실험은 걷음) |

## 2 켜고 끄기

1. Unity에서 `Assets/ProjectL/DataBaseSO/InGameGroup/EnemyAi/Item/EnemyAiTuning_Default.asset`을 연다.
2. "적 AI 종류 (연구용)"의 **Decision Model**을 `TurnPlanner`로 바꾸고 재생한다. 값은 게임 시작 때 한 번 읽는다.
3. 끝나면 `Utility`로 되돌린다. 에셋에 칸이 없거나 `Utility`면 지금 게임과 똑같다.

성향 바꾸기(2026-11-18, [Core 이식 설계](../docs/2026-11-18-core-turn-planner-port-design.md) §11):

1. `EnemyTurnPlannerPreset_Default.asset`을 복제해(Ctrl+D) 이름을 붙인다. 새로 만들 때는 메뉴 `ProjectL/Database/InGame/EnemyTurnPlannerPreset (연구용)`.
2. 값을 바꾼다. 칸에 마우스를 올리면 뜻이 나오고, 맨 위 상자가 판 점수 식이다.
3. 조정 에셋 `EnemyAiTuning_Default.asset`의 **Planner Preset** 칸에 넣고 재생한다. 비우면 기본값이다.
4. 콘솔의 `[턴 계획 AI] 준비 시작부터 결정까지 …` 줄 끝에 쓴 값이 나온다.

**지금 알아 둘 것 (2026-11-18 BG-a):** 계획은 일 스레드에서 돈다(`LLMTest/docs/2026-11-18-planner-background-parallel-design.md`). 그래서 계산하는 동안에도 프레임은 거의 떨어지지 않을 것으로 본다(추정, 재생 확인 전). 대신 기다림은 계산 시간 그대로라, 적 턴 첫 행동까지 Unity 에디터에서 약 20초로 본다(추정: 12ms 예산 때 사용자가 본 30초 이상에서 계산 몫만). 계획이 끝나면 콘솔에 `[턴 계획 AI] 준비 시작부터 결정까지 N초 …` 한 줄이 남는다. 첫 행동 뒤에는 보관한 계획에서 바로 꺼낸다. 여러 코어로 나눠 계산하는 일(BG-b)은 아직이다.

진단(`EnemyDecisionDiagnostics`, 세션마다):

| 칸 | 계획 AI에서의 뜻 |
|---|---|
| `CacheHits` | 보관한 턴 계획에서 행동을 꺼낸 수 |
| `CapHits` | 계획이 실패해(예외 또는 지금 판에 맞는 행동 없음) 그 행동을 지금 AI로 고른 수 |
| `ExpandedNodes` | 빠른 V로 매긴 판 수 |
| `PreviewCalls` | 엔진으로 돌린 행동 수(빔 + 2단 평가) |

## 3 develop 갱신 때

갱신 절차(인계 문서 §0·§10, 커밋 `c449c977`·`235b6291`의 방식)는 `LLMTest/` 밖을 develop과 똑같이 만든다. 이 작업을 지키려면 두 가지를 바꾼다.

1. develop에 없는 파일을 지울 때 `Assets/ResearchEnemyPlanner/`와 `Assets/ResearchEnemyPlanner.meta`를 목록에서 뺀다. 프리셋 SO·기본 프리셋(위 표의 새 파일)은 지워져도 3번 패치가 다시 만든다(`.meta` GUID도 패치 안에 있다).
2. 파일을 develop 판으로 맞출 때도 그 폴더를 뺀다.

```bash
git checkout FETCH_HEAD -- . ':(exclude)LLMTest' ':(exclude)Assets/ResearchEnemyPlanner' ':(exclude)Assets/ResearchEnemyPlanner.meta'
```

3. 그 뒤 연결 줄을 다시 붙인다. **충돌하면 멈추고 사용자에게 보고한다**(develop에서 그 파일이 바뀌었다는 뜻이다).

```bash
git apply --3way LLMTest/core_port/hooks.patch
```

4. Unity 배치 컴파일(오류 0)과 측정 도구(아래)로 확인한다.
5. 연결 줄을 고쳤으면 패치를 다시 만든다: 위 표의 고친 파일(코드 여섯 + 조정 에셋)은 `git diff 07561fc7 -- <파일>`, 새 파일 넷은 `git diff --no-index -- /dev/null <파일>`을 이어 붙인다. `git apply -R --check`로 확인한다.

## 4 측정 도구

Unity 밖에서 Core 소스와 `Assets/Tests/EditMode`의 Core 시험 도우미를 묶어 빌드한다. .NET 8 SDK가 필요하다. 저장소 루트에서 돌린다(시험이 루트 기준 경로로 기준 기록을 읽는다).

```bash
dotnet build LLMTest/core_port/harness/Harness
```

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- N5,N8,G16 1
```

벤치마크 판(`EnemyAiBenchmarkFixtures`의 판 ID, 씨앗)마다 한 줄을 출력한다. 출력은 다음 셋이다.
- 지금 AI 결정 하나의 시간.
- 계획 하나의 시간·돌린 수·매긴 판 수.
- 계획 AI로 적 턴 전체를 게임처럼(행동마다 새 세션) 돌린 결과: 행동 수, 거절 수, 지금 AI로 넘긴 수, 보관 계획 사용 수.

판의 메인 지속 상태는 판의 메인 캐릭터에 맞춰 고친다(시험 판은 빈 판의 값을 쓰는데, 지금 AI는 플레이어 메인을 움직여 보지 않아 드러나지 않던 불일치다).

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- profile N8
```

행동 하나 돌리기, 공격 범위 조회, 판 평가 하나의 시간을 따로 잰다.

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- tests --where "class =~ Enemy"
```

기존 Core 적 AI 시험을 돌린다. 기본값(지금 AI)이 바뀌지 않았는지 볼 때 쓴다.

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- dump out.txt
```

기준 판 11개 × 씨앗 1·7의 계획과 적 턴 전체 행동을 파일에 적는다(시간 없음). 고치기 전후 파일을 그대로 비교해 결정이 같은지 본다. BG-0 기준은 `LLMTest/results/core_port/20261118_bg0_base_decisions.txt`. 끝 인자로 깊은 V 종류를 고른다(`dump out.txt N5,N8 1,7 l0`). 게임 길과 다른 종류면 계획만 적는다.

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- lookahead
```

같은 실행에서 옛 깊은 V(l0)와 한 턴 더 보기(l1)의 계획 시간·할당·엔진 수·계획이 같은지를 판마다 적는다.

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- seq N5,N8 weak
```

위험 판에서 플레이어 유닛마다 따로 고른 응수와 차례대로 둔 응수를 나란히 적는다. `style N8`은 기본값이 아닌 성향 값이 결정 설정·세션을 거쳐 계획 AI에 닿는지 본다. `weak`면 첫 유닛이 노린 적의 HP를 그 한 방으로 낮춰, 차례대로 둘 때 뒤 유닛이 다른 적을 노리는지 본다.

다른 세션과 같이 쓸 때 `bin`이 잠기면 빌드 출력을 따로 둔다: `dotnet build LLMTest/core_port/harness/Harness --artifacts-path <다른 폴더>` 뒤 `<다른 폴더>/bin/Harness/debug/ProjectL.Tests.Core.exe <모드>`.

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- cancel G16
```

계획 도중 세션을 버리면 일 스레드가 멈추는지 CPU 시간으로 본다. .NET의 단계 컴파일이 뒤에서 쓰는 CPU를 빼려면 `DOTNET_TieredCompilation=0`을 붙인다.

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- settle N8,T8,G16
```

정산 한 번의 사건 수, 정산·결과 묶음 검증·다음 판 만들기의 시간과 할당을 잰다.

```bash
dotnet run --project LLMTest/core_port/harness/Harness --no-build -- allocbytype N5,N8,T8,G16
```

계획 하나(l0)가 할당한 메모리를 형식별로 나눈다(런타임 할당 표본). 결과 해석은 [병렬 설계](../docs/2026-11-18-planner-background-parallel-design.md) §15.
