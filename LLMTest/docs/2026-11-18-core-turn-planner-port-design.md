# 턴 계획 AI를 게임 Core로 옮기기 (설계 초안)

작성일: 2026-11-18 · 상태: **CP-0~CP-d 구현 완료(2026-11-18 사용자 "CP-d 까지 진행해", §10).** Unity 배치 컴파일 오류 0, 기존 Core 적 AI 시험 603개 통과, Unity 밖 측정 도구로 벤치마크 판 11개를 끝까지 돌림. **Unity 재생 확인 전, 커밋 전.** 계획 한 번 2~16초(.NET 8)라 프레임 예산은 사용자 결정 대기(§10.4).

## 1 배경과 정한 것

- 요청(2026-11-18 사용자): "우리가 지금까지 한 거 유니티 코어에 올리자."
- 사용자가 정한 것(같은 날):
  - **올릴 곳:** 이 연구 저장소(Local LLM)의 Core 사본에만 올린다. 팀 저장소(`../ProjectL`)·develop에는 올리지 않는다.
  - **기존 AI와의 관계:** 지금 게임 적 AI(Utility)는 그대로 두고, 계획 AI를 고를 수 있는 두 번째 적 AI로 더한다. 기본은 지금 AI다.
  - **옮길 판:** `plan4:g1`을 그대로 옮긴다. 빔 4, 2단 평가(빠른 V 상위 4개를 엔진 응수로 다시 매김), G1(메인 쪽 걸음 벌점, g = 1), G2 끔. 판 평가 식은 [계획 AI 설계](2026-11-13-turn-planner-design.md) §2, [2단 평가 설계](2026-11-13-planner-engine-reply-design.md), [메인 함께 노림 설계](2026-11-14-planner-goal-pressure-design.md) §2와 코드 `sim/evaluate.py`·`planner.py`·`reply.py`가 원본이다.
- 이 연구의 원칙("Unity·Core 코드는 바꾸지 않는다", 인계 문서 §1)은 이번 작업에 한해 사용자 지시로 바뀐다. 바꾸는 곳은 이 저장소 사본뿐이다.

## 2 Core에서 확인한 사실 (2026-11-18, develop `1d31ec7d0` 판)

경로 줄임: App = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/`, Ctr = 같은 곳의 `Contracts/`, CS = `.../CoreSystem/CombatSystem/`.

### 2.1 적 AI가 도는 방식
- 적 행동은 **부를 때마다 하나**다. 결정 종류는 공격·이동·적 턴 끝 셋뿐이다(`Ctr/Combat/EnemyAi/EnemyDecision.cs`). "이동 + 공격"은 결정 두 번이다.
- 실제 게임은 준비 경로를 쓴다(`CS/CombatAdvanceService.cs`).
  - `PrepareAdvanceSlice`(:94-149)가 **행동마다 새** `EnemyDecisionSession`을 만들고(:127), 프레임마다 `Step()`을 부른다.
  - 한 프레임 예산은 **1ms**(`BudgetSpent`, :270)이고, 한 프레임에는 준비와 실행 중 하나만 한다(`SpendFrame`).
  - 준비가 끝나면 `AdvancePreparedCombat`(:151-210)이 입력을 다시 모아 그 결정을 정산한다. 거절되면 `RejectSelected()` 뒤 다시 준비한다.
- 행동 사이에 남는 것은 `EnemyDecisionSideCache` 하나다. 전투 번호와 진영 턴 순번으로 묶이고, 턴이 바뀌면 버려진다(`CombatAdvanceService.cs:238-256`).
- 조정 에셋 경로: `DataBaseSO/InGameGroup/EnemyAi/Item/EnemyAiTuning_Default.asset` → `EnemyAiTuningDataSO` → `EnemyAiTuningData.ToSettings()` → `EnemyDecisionSettings` → `CombatAdvanceService._settings` → 세션.
- 즉시 경로(`EnemyAiTurnService.Calculate`)는 조정값을 무시하고 `EnemyDecisionSettings.Default`를 쓴다. 이 경로는 시험·호환용이다.

### 2.2 행동을 돌려 볼 수 있는가
- **돌려 볼 수 있다.** `CombatCommandCalculator.Calculate(input, rules)`는 순수 계산이다(정적 가변 상태 없음). `CombatCommandComposition.Intent(...)`로 행동을 넣고, 받아들여진 결과는 `Project(t)`로 다음 입력이 된다. 거절은 예외가 아니라 `Accepted = false`인 결과다.
- 두 진영을 잇는 예가 이미 있다: `CombatFirstUseRehearsal.cs:87-130, 244-263`(첫 동작 준비용 가상 연쇄).
- 플레이어 응수를 돌리는 순서(추정, 계획에서 확인): 적 턴 끝 → `Project` → 플레이어 턴 시작(Advance) → `Project` → 명령 입력으로 다시 만듦(번호 1, 기대 전투 번호·턴 순번) → 플레이어 유닛 행동 → `Calculate`. 명령 입력을 다시 만드는 꼴은 `Assets/Tests/EditMode/CombatCommandTestData.cs:95-101`(`FreshCommand`)에 있다.
- 승패: 결과의 `Outcome.IsStageEnded`, 입력의 `State.Phase == Finished`.

### 2.3 난수와 숨은 정보
- 치명타는 없다. 난수는 모두 상태 안의 정해진 값이다(아이템·환경·소환·증원 난수 상태). 그래서 같은 입력이면 같은 결과가 나온다.
- 따라서 실제 판을 그대로 돌리면 계획 AI가 **앞으로 나올 난수 결과와 숨은 칸을 미리 보게 된다.**
- 기존 AI는 적이 볼 수 있는 환경으로 바꾼 입력에서 정밀 미리보기를 하고(`CombatActionAssessment.cs:111-117`, `EnemyDecisionContext.cs:120`), 무작위가 끼는 효과는 어림값으로 바꾼다(`EnemyUtilityScorer.cs:55-57`).

### 2.4 재료가 있는 곳

| 계획 AI가 쓰는 것 | Core에서 |
|---|---|
| 이동 목적지·경로 | `CombatMovementCalculator.Range(input)` → `MovementRangeResult.Destinations`·`TryGetRoute` |
| 공격 조준·맞는 칸 | `CombatAttackQueries.EnumerateAimsThatCanHit`·`Selection`(지정 칸 `DesignatedCells`, 스플래시 `AdditionalCells`), 적용 비율 = 지정 100, 스플래시 `SplashPercent` |
| 예상 피해(방어·상성·지형) | `CombatDamageCalculator.Attack`, 또는 `CombatCommandCalculator.Preview` |
| 유닛 사실 | `CharacterCombatFacts`: 진영 `Owner`, 메인 `OwnershipRole`, HP `Health`, 공격력 `Stats.EffectiveAttackPower`, 이동 `Stats.MoveRange`, 사거리 `Stats.AppointRange`, 행동 단계 `ActionUse`, 칸 `OccupiedCells` |
| 공격·이동 제약 | `CombatActionAdmission.IsAttackRestricted`·`IsMovementRestricted` |
| 적 본진 | 구조물 중 `Owner == Enemy && Definition.Key == Structure_Headquarters` |
| 남은 증원 | `State.Reinforcements.RemainingTotal(spec)`, spec = `input.Battle.NexusReinforcements` |
| 도발 | `TauntTargeting`(AI가 고를 때 지키는 규칙) |

- 필요한 함수는 모두 `ProjectL.Core.Application` 어셈블리 안의 internal이다. 같은 어셈블리 안 코드에서는 쓸 수 있다.
- **없는 것:** 전투 시작 때의 적 배치 목록. 계획 AI의 본진 값(= 전투 시작 때 적 유닛 공격력 × 최대 HP 합, `sim/evaluate.py`)을 그대로 구할 곳이 없다(§7 P1).

## 3 설계

### CP1 자리: 별도 폴더 하나 + asmref
- 계획 AI 코드는 모두 새 폴더 **`Assets/ResearchEnemyPlanner/`** 하나에 둔다.
- 폴더에 `ResearchEnemyPlanner.asmref`를 두어 `ProjectL.Core.Application`(GUID `42f479a350493ca4abd078910e78a9ad`)에 붙인다. 그러면 Core의 internal 함수를 그대로 쓰고, Unity 참조 금지(`noEngineReferences`)도 같이 지킨다.
- 이름공간은 `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner`로 한다.
- 기존 파일을 고치는 곳(훅)은 CP2~CP4의 다섯 곳뿐이다. 이 줄들은 §4대로 보관한다.

### CP2 고르기: 조정 에셋의 칸 하나
- `EnemyDecisionSettings`(Ctr)에 `DecisionModel`(`Utility = 0` 기본 / `TurnPlanner = 1`)을 더한다. 열거형은 같은 파일 안에 둔다(새 파일이면 develop 갱신 때 지워져서).
- `EnemyAiTuningDataSO`·`EnemyAiTuningData`에 칸 `decisionModel`을 더하고 `ToSettings()`가 넘긴다.
- 에셋 값을 바꾸지 않으면 기본 Utility라 **지금 게임과 똑같다.** 계획 AI를 쓰려면 `EnemyAiTuning_Default.asset`의 칸을 TurnPlanner로 바꾼다.

### CP3 연결: 세션 안의 갈래 하나
- `EnemyDecisionSession` 생성자가 `settings.DecisionModel == TurnPlanner`이면 할 일을 지금의 `SelectUtility` 대신 계획 AI의 단계(`EnemyTurnPlanner.Steps(...)`)로 고른다.
- 겉모양(`Step`·`IsReady`·`Decision`·`RejectSelected`·`Dispose`)이 그대로라 `CombatAdvanceService`는 고치지 않는다.
- 즉시 경로는 늘 기본 설정이라 지금 AI 그대로다.

### CP4 턴 계획 보관: 옆 캐시에 칸 하나
- 세션은 행동마다 새로 생기지만, 계획 AI는 **턴 전체를 한 번에 계획**한다. 계획은 `EnemyDecisionSideCache`에 칸 하나(`TurnPlan`)로 둔다. 이 캐시는 이미 전투 번호·진영 턴 순번으로 묶이고 턴이 바뀌면 버려진다.
- 세션마다 이렇게 한다.
  - 보관된 계획이 있고, 다음 행동의 "행동 전 판 서명"이 지금 입력과 같으면 그 행동을 바로 낸다(계산 거의 없음).
  - 다르면(예측과 다른 결과·증원·포효 반응 등) 다시 계획한다. 시뮬레이터 `PlannerPolicy.act`와 같은 규칙이다.
- 판 서명: 산 유닛의 (ID, 칸, HP, 행동 단계, 보호막), 구조물 HP, 지형 변화, 국면. 정확한 칸은 계획에서 정한다(P3).

### CP5 판 만들기: Core 계산기로 돌림
- 후보 하나 = `Intent(input.WithActor(id), Move 또는 Attack, aim, path)` → `Calculate` → 거절이면 버리고, 받아들여지면 `Project`로 다음 판을 만든다. 전투가 끝난 판은 이기면 +10⁹, 지면 −10⁹다.
- **특성·기믹이 모두 게임 규칙대로 들어간다.** 시뮬레이터에서 빠졌던 것(가시밭·작렬·기믹·보스 포효 등)도 결과에 들어간다. 점수 식이 특성을 모르는 원칙("결과로 판단")은 그대로다.
- 정보 공정성은 Q-CP1에서 정한다. 권장은 기존 AI처럼 적이 보는 환경으로 바꾼 입력에서 계획하고, 난수 상태는 계획용 고정 씨앗으로 바꾸는 것이다.

### CP6 판 평가 (빠른 V) — 식은 시뮬레이터 그대로

```
V = 자산(적) − 자산(아군) − 겁 0.5 × 위험 + 기회 0.5 × 기회 − 거리 벌점 + G1
```
- 유닛 전력 = 공격력 × HP(메인 × 3). 적 자산에는 본진(본진 값 × HP 비율)과 남은 증원(공격력 × 최대 HP × 수 × 0.5, 본진이 살아 있을 때)이 들어간다.
- 위험·기회·거리 벌점·G1은 `sim/evaluate.py`와 같은 식이다. 재료는 §2.4 표의 Core 함수를 쓴다.
- 도달 표: 유닛이 그 칸에 있을 때 이동해서 닿는 칸 → 적용 비율. 계획 시작 판 기준으로 (유닛, 칸)마다 만들어 계획 동안 다시 쓴다.
- 숫자는 `double`로 계산한다. 같은 판이면 시뮬레이터와 같은 값이 나와야 한다(§6 확인 방법).

### CP7 후보와 빔 — 시뮬레이터 그대로
- 유닛마다 후보: 제자리 공격 전부, 이동 + 공격(도착 칸에서 사거리 + 1 안에 대상이 있을 때), 이동만은 빠른 이동 점수 상위 3개. 그중 V 상위 6개를 펼친다.
- 단계마다 같은 판은 V가 가장 높은 것 하나만 남기고 상위 4개를 다음 단계로 넘긴다. 시작 판("아무것도 안 함")도 후보다.
- 도발당한 유닛은 기존 AI와 같은 `TauntTargeting` 규칙을 지킨다. 게임에서는 도발이 AI가 고를 때만 지키는 규칙이기 때문이다.

### CP8 2단 평가 — 시뮬레이터 그대로
- 시작 판과 단계마다 남긴 판 중 빠른 V 상위 4개를 다시 매긴다.
- **위험 판:** 적 턴을 끝내고 플레이어 턴을 연 판이다. 플레이어 유닛마다 공격 후보를 전부 돌려 순이득(빼앗은 자산 − 잃은 자산)이 가장 큰 것 하나를 고른다. 대상별로 합쳐 HP에서 자른 손실이 위험이다.
- **기회 판:** 위험 판에서 플레이어가 아무것도 안 하고 턴을 넘긴 판이다. 적 유닛마다 같은 식으로 고른다.
- 자산·G1은 빠른 V와 같다.

### CP9 프레임 나눔
- 계획은 작은 단계의 나열(`IEnumerable`)로 쓴다. 한 단계는 후보 하나 돌리고 매기기, 도달 표 한 칸 같은 크기다. 세션 `Step()` 한 번이 단계 하나다. 지금 게임 AI(`AttackSteps`)와 같은 꼴이다.
- 예산(지금 1ms/프레임)을 그대로 둘지는 Q-CP3에서 정한다. 시뮬레이터에서 계획 한 번은 평균 약 0.7초였다(적 `plan4:g1`, 시드 4~7 180판, 프로세스 5개 함께). Core 계산이 한 번에 1ms 가깝다면 1ms 예산으로는 첫 행동까지 수백 프레임(수 초)을 기다릴 수 있다. **가장 큰 위험이 이 비용이다.**

### CP10 거절과 진단
- 계획한 행동이 실제 정산에서 거절되면(`RejectSelected`) 계획을 버리고 다시 계획한다. 다시 계획한 첫 행동도 거절되면 그 한 행동은 기존 AI로 고른다(Q-CP4).
- 계획 수·다시 계획 수·돌린 후보 수·다시 매김 수·계획 시간은 지금 있는 `EnemyDecisionDiagnostics` 칸(펼친 판·미리보기 수 등)에 넣는다. 새 진단 칸은 만들지 않는다(기존 파일을 덜 고치려고).

## 4 develop 갱신 때 보관하는 법

지금 갱신 절차는 `LLMTest/` 밖을 develop과 똑같이 만든다(develop에 없는 파일은 지우고, 있는 파일은 develop 판으로 덮는다). 그대로면 이 작업이 사라진다. 그래서 절차를 바꾼다.

1. 새 파일은 `Assets/ResearchEnemyPlanner/` 한 폴더에만 둔다. 갱신 때 지울 목록과 덮어쓸 범위에서 이 폴더를 뺀다.
2. 기존 파일을 고친 줄(훅 다섯 곳: `EnemyDecisionSettings.cs`, `EnemyAiTuningDataSO.cs`, `EnemyAiTuningData.cs`, `EnemyDecisionSession.cs`, `EnemyDecisionSideCache.cs`)은 `LLMTest/core_port/hooks.patch`에 보관한다.
3. 갱신 뒤 `git apply --3way LLMTest/core_port/hooks.patch`로 다시 붙인다. 충돌하면 멈추고 사용자에게 보고한다. develop에서 그 파일이 바뀌었다는 뜻이기 때문이다.
4. 바뀐 절차는 인계 문서 §2·§10에 적는다.

## 5 범위 밖
- 팀 저장소·develop에 올리는 것.
- Core에서 계획 AI와 게임 AI를 여러 판 자동으로 겨루는 측정. 이번에는 직접 해 보는 확인까지만 하고, 측정은 따로 설계한다(Q-CP6).
- 아군 쪽 계획 AI. 게임의 아군은 사람이 둔다.
- G2·빔 다양성·성향 손잡이를 조정 에셋으로 꺼내는 것. 값은 `plan4:g1`로 코드에 고정한다.

## 6 확인 방법
- **컴파일:** Unity가 꺼져 있을 때 Claude가 Unity 배치 모드로 컴파일한다.
- **기본값 그대로:** 에셋을 바꾸지 않은 판에서 지금 게임과 똑같이 도는지 확인한다(사용자 재생).
- **계획 AI 켬:** 에셋을 TurnPlanner로 바꾸고 전투 몇 판을 둔다(사용자 재생). 적이 턴 전체를 계획한 대로 두는지, 첫 행동까지 얼마나 기다리는지 본다.
- **식이 같은지:** 시뮬레이터와 Core가 같은 규칙을 쓰는 간단한 판에서 빠른 V 값이 같은지 대조한다. 방법은 계획에서 정한다(P4).

## 7 정할 것

### 사용자 결정 (권장은 모두 A) — **2026-11-18 모두 A로 정함**

| 번호 | 물음 | A (권장) | B | C |
|---|---|---|---|---|
| Q-CP1 | 정보 공정성 | 기존 AI처럼 적이 보는 환경 + 난수는 계획용 고정 씨앗 | 실제 판 그대로(숨은 칸·다음 난수를 미리 봄) | |
| Q-CP2 | 고르는 자리 | 조정 에셋 칸 하나(CP2) | 개발용 설정(Play From Boot)만 | |
| Q-CP3 | 프레임 예산 | 첫 단계에서 비용을 재고 그 뒤 정함 | 지금 1ms 그대로 | 계획 AI만 큰 예산 |
| Q-CP4 | 계획한 행동이 거절되면 | 다시 계획, 또 거절이면 그 행동만 기존 AI | 다시 계획, 또 거절이면 적 턴 끝 | |
| Q-CP5 | 시험 | 새 자동 시험 없음. Claude 배치 컴파일 + 사용자 재생 확인 | 계획 AI 시험 몇 개를 같은 폴더에 | |
| Q-CP6 | 여러 판 자동 측정 | 이번 범위 밖, 뒤에 따로 설계 | 이번에 같이 | |

### 계획에서 정할 것
- **P1 본진 값:** 전투 시작 적 배치가 입력에 없다. 후보는 셋이다. ① 첫 계획 때 그 판의 적 유닛(죽은 유닛 포함)으로 정해 옆 캐시보다 오래 사는 곳에 둔다. ② 입력의 모든 적 유닛(죽은 유닛 포함, 증원 포함)으로 매번 구한다. ③ 본진 최대 HP에 비례하는 값.
- **P2 플레이어 응수 연쇄:** §2.2의 순서가 실제로 되는지, 플레이어 턴 시작 효과(재생·증원 등)가 엔진 순서대로 들어가는지 확인한다.
- **P3 판 서명 칸.**
- **P4 시뮬레이터와 값 대조 방법.**
- **P5 이동 후보:** 시뮬레이터는 닿는 칸 전부를 후보로 보지만, 게임 AI 이동 후보는 빔·한도가 있다. 계획 AI는 `Range` 목적지 전부를 쓴다(시뮬레이터와 같게).

## 8 단계(안)

| 단계 | 할 일 | 끝나면 |
|---|---|---|
| CP-0 뼈대·측정 | 폴더·asmref, 훅 다섯 곳, 계획 AI 자리에 "기존 AI와 같은 결정"을 내는 빈 판. Core 행동 한 번의 비용을 진단에 남김 | 켜도 화면이 같다. 비용 숫자로 Q-CP3을 정한다 |
| CP-a 빠른 V·빔 | CP5~CP7, G1, 2단 평가 끔(`plan4:r0:g1` 상당) | 켜면 계획 AI가 실제로 둔다 |
| CP-b 2단 평가 | CP8 | `plan4:g1` 완성 |
| CP-c 예산·진단 | CP9 예산 반영, CP10 진단 | 첫 행동까지 기다리는 시간을 잰다 |
| CP-d 보관·문서 | §4 패치·갱신 절차, 인계 문서, 사용법 | develop을 갱신해도 다시 붙일 수 있다 |

단계마다 멈추고 보고한다.

## 9 Task 계획 (2026-11-18 사용자 "권장대로, 계획 써줘")

정한 것: Q-CP1 A(적이 보는 환경 + 계획용 고정 씨앗), Q-CP2 A(조정 에셋 칸), Q-CP3 A(비용을 잰 뒤 예산 결정), Q-CP4 A(다시 계획, 또 거절이면 그 행동만 기존 AI), Q-CP5 A(새 자동 시험 없음, Claude 배치 컴파일 + 사용자 재생 확인), Q-CP6 A(여러 판 자동 측정은 뒤에 따로).

### 9.1 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| TP-1 | 본진 값(P1) = 그 입력의 모든 적 캐릭터(죽은 유닛이 입력에 남아 있으면 포함)의 공격력 × 최대 HP 합. CP-0에서 죽은 유닛이 입력에 남는지 확인해 보고한다 | 전투 시작 배치가 입력에 없고, 옆 캐시는 턴마다 버려진다. 증원으로 나온 유닛도 들어가 시뮬레이터(시작 배치만)보다 커질 수 있다 |
| TP-2 | 판 서명(P3) = 산 캐릭터의 (ID, 기준 칸, 현재 HP, 행동 단계, 보호막), 구조물의 (ID, HP), 국면·진영 턴 순번 | 시뮬레이터 `signature`와 같은 뜻 |
| TP-3 | 공정 입력: 계획 시작 입력의 환경을 적이 보는 환경(`Environment.VisibleSteps(Owner.Enemy, …)`, 기존 AI와 같은 함수)으로 바꾸고, 난수 상태 넷(아이템·환경 진행·소환·증원)을 고정 씨앗으로 바꾼다. 씨앗 = (진영 턴 순번 × 2 + 1) × 1,000,003 + 다음 개체 ID(시뮬레이터 `prediction_seed`와 같은 꼴). 계획 판과 응수 판 모두 이 입력에서 시작한다 | Q-CP1 A |
| TP-4 | 비용 측정: CP-0의 측정 모드가 시작 판에서 적 유닛의 후보를 모두 한 번씩 돌린다. 사용자가 Unity Profiler에서 지금 있는 표시 `EnemyPreparation`(준비 시간)과 프레임 수를 읽는다. 돌린 수는 진단 `PreviewCalls`에 넣는다 | 새 진단 칸·로그를 만들지 않는다(기존 파일을 덜 고치려고). Core에는 Unity 로그가 없다 |
| TP-5 | 이동 후보(P5) = `CombatMovementCalculator.Range`의 목적지 전부 | 시뮬레이터 `unit_options`와 같게 |
| TP-6 | 거절 알아채기: 세션이 `RejectSelected` 뒤 단계 열거를 이어 부르면, 계획 AI는 앞서 낸 행동이 거절됐다고 본다 | 세션 겉모양을 바꾸지 않는다 |
| TP-7 | 기존 AI로 넘기기: 세션이 자기 `SelectUtility` 단계를 함수로 넘겨 주고, 계획 AI는 두 번째 거절 때 그 단계를 그대로 돈다 | 훅이 세션 한 줄로 끝난다 |
| TP-8 | 계획 AI 값(빔 4, 유닛당 6, 이동만 3, 다시 매김 4, 겁 0.5, 기회 0.5, 걸음 벌점 10, 메인 배수 3, 증원 할인 0.5, g 1)은 계획 AI 폴더의 코드 상수 한 곳(`EnemyTurnPlannerConfig`)에 둔다 | §5: 에셋으로 꺼내지 않는다 |
| TP-9 | 이름: 열거형 `EnemyDecisionModel`(`Utility`·`TurnPlanner`), 에셋 칸 `decisionModel`, 계획 AI `EnemyTurnPlanner`, 보관 칸 형식 `EnemyTurnPlan` | |
| TP-10 | 시뮬레이터와 값 대조(P4)는 이번에 하지 않고, 여러 판 자동 측정 설계(Q-CP6) 때 함께 한다 | 값을 꺼내 볼 길이 시험이나 측정 도구뿐인데, 이번에는 둘 다 만들지 않는다(Q-CP5·Q-CP6 A) |
| TP-11 | 프레임 예산을 바꾸게 되면(CP-c) `CombatAdvanceService`에 훅이 하나 늘어난다. 그때 다시 묻는다 | 지금 예산은 그 파일의 상수다 |
| TP-12 | 컴파일은 Unity가 꺼져 있을 때 Claude가 Unity 배치 모드로 한다. Unity 실행 파일 위치는 CP-0에서 확인한다 | Q-CP5 A |

### 9.2 만들 파일 (모두 `Assets/ResearchEnemyPlanner/`, `.meta`와 함께)

| 파일 | 하는 일 | 단계 |
|---|---|---|
| `ResearchEnemyPlanner.asmref` | `ProjectL.Core.Application`에 붙임 | CP-0 |
| `EnemyTurnPlanner.cs` | 세션이 부르는 입구. 보관 계획 확인 → 행동 내기 / 다시 계획 / 거절 처리 / 기존 AI로 넘김 | CP-0(측정 모드) → CP-a |
| `EnemyTurnPlannerConfig.cs` | TP-8 값 | CP-a |
| `EnemyTurnPlan.cs` | 보관 계획(행동 목록, 행동마다 행동 전 판 서명) | CP-a |
| `PlannerBoard.cs` | 공정 입력(TP-3), 행동 적용(`Intent` → `Calculate` → `Project`), 판 서명(TP-2), 승패 | CP-0 → CP-a |
| `PlannerFacts.cs` | 유닛 전력·자산·본진(TP-1)·증원·대상 목록·승리 대상(플레이어 메인) | CP-a |
| `PlannerReach.cs` | 도달 표((유닛, 칸) → 칸별 적용 비율), 예상 피해, 최선 일격 | CP-a |
| `PlannerEvaluator.cs` | 빠른 V(자산·위험·기회·거리 벌점·G1), 빠른 이동 점수 | CP-a |
| `PlannerCandidates.cs` | 유닛 하나의 후보(제자리 공격·이동·이동 + 공격, 도발 규칙) | CP-a |
| `PlannerBeam.cs` | 빔 탐색(단계로 나뉨), 다시 매김 후보 모음 | CP-a → CP-b |
| `PlannerReply.cs` | 응수 판(위험 판·기회 판), 응수 고르기, 깊은 V | CP-b |

파일 나눔은 시뮬레이터(`evaluate.py`·`planner.py`·`reply.py`·`analysis.py`)를 따른다. 파일 이름은 구현 중 바뀔 수 있고, 바뀌면 이 표를 고친다.

### 9.3 고칠 기존 파일 (훅 다섯 곳)

| 파일 | 고칠 것 | 단계 |
|---|---|---|
| `…/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs` | 같은 파일 안에 `enum EnemyDecisionModel { Utility = 0, TurnPlanner = 1 }`. 속성 `DecisionModel`, 생성자 끝 선택 인자 `decisionModel = Utility`(기존 호출 그대로) | CP-0 |
| `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/EnemyAi/Item/EnemyAiTuningDataSO.cs` | 칸 `decisionModel`(기본 Utility, 설명 툴팁), `GetData`가 넘김 | CP-0 |
| `…/DataBaseSystem/Data/InGame/Table/EnemyAi/Item/EnemyAiTuningData.cs` | 값 `DecisionModel`, `ToSettings()`가 넘김 | CP-0 |
| `…/Application/Combat/EnemyAi/EnemyDecisionSession.cs` | 생성자: 즉시 경로가 아니고 `settings.DecisionModel == TurnPlanner`이면 `EnemyTurnPlanner.Steps(input, rules, settings, sideCache, Diagnostics, 결정 넣기, () => SelectUtility(...))`, 아니면 지금 그대로 | CP-0 |
| `…/Application/Combat/EnemyAi/EnemyDecisionSideCache.cs` | `internal EnemyTurnPlan TurnPlan`, `EnterScope`가 범위가 바뀔 때 같이 비움 | CP-0 |

에셋 `EnemyAiTuning_Default.asset`은 고치지 않는다(칸이 없으면 코드 기본값 Utility). 계획 AI를 켜서 확인할 때만 사용자가 에디터에서 바꾸고, 확인 뒤 되돌린다.

### 9.4 단계별 Task

**CP-0 뼈대·비용 측정** (끝나면 켜도 화면이 같다. 비용 숫자가 나온다)
1. Unity 실행 파일 위치와 배치 컴파일 명령을 확인하고, 고치기 전 판이 컴파일되는지 본다(TP-12).
2. 폴더와 `.asmref`를 만든다.
3. 훅 다섯 곳(§9.3)을 넣는다.
4. `EnemyTurnPlanner` 측정 모드를 만든다: 공정 입력(TP-3)을 만들고, 시작 판의 적 유닛마다 후보 전부를 한 단계에 하나씩 돌려 `PreviewCalls`에 센 뒤, 기존 AI 단계로 넘겨 기존 결정을 낸다.
5. 입력에 죽은 유닛이 남는지 코드로 확인한다(TP-1).
6. 배치 컴파일. 오류가 없으면 사용자 확인을 부탁한다.
   - 에셋 그대로: 지금과 같은지.
   - 에셋 TurnPlanner: 행동이 같은지, Profiler `EnemyPreparation` 시간·프레임 수.
7. 보고: 비용 숫자로 Q-CP3(예산)을 정할 재료.

**CP-a 빠른 V·빔** (끝나면 켜면 계획 AI가 실제로 둔다. 2단 평가 없음 = `plan4:r0:g1` 상당)
1. `PlannerBoard`: 행동 적용·판 서명·승패.
2. `PlannerFacts`: 유닛 전력·자산·본진·증원·대상.
3. `PlannerReach`: 도달 표(유닛을 그 칸에 둔 입력에서 `Range` 목적지 + 각 목적지의 조준이 덮는 칸, 지정 100·스플래시 %), 예상 피해(`CombatDamageCalculator.Attack`, 지형 출발 칸 = 지금 칸), 최선 일격.
4. `PlannerEvaluator`: 위험·기회·거리 표(벽·낭떠러지·구조물 못 지남, 유닛은 지나감)·거리 벌점·G1·빠른 V·빠른 이동 점수.
5. `PlannerCandidates`: 제자리 공격(`EnumerateAimsThatCanHit`), 이동(TP-5), 이동 + 공격(도착 칸에서 사거리 + 1 안에 대상이 있을 때), 도발당한 유닛은 `TauntTargeting` 규칙.
6. `PlannerBeam`: K 4, N 6, 이동만 3. 같은 판은 하나, 시작 판 포함, 전투가 끝나면 멈춤. 결과를 `EnemyTurnPlan`으로 옆 캐시에 넣는다.
7. `EnemyTurnPlanner`: 보관 계획 확인(서명) → 다음 행동 / 다시 계획. 거절 → 다시 계획 → 또 거절이면 기존 AI(TP-6·TP-7). 계획이 비면 적 턴 끝.
8. 배치 컴파일 → 사용자 재생 확인(계획 AI 켬): 적 행동이 그럴듯한지, 오류·멈춤이 없는지, 첫 행동까지 대기.

**CP-b 2단 평가** (끝나면 `plan4:g1` 완성)
1. `PlannerReply` 응수 판:
   - 위험 판 = 적 턴 끝(`CompleteEnemyTurn`) → 플레이어 턴 시작(`StartControllerTurn`) → 명령 입력으로 다시 만들기(공개 생성자, 번호 1, 기대 전투 번호·진영 턴 순번; 꼴은 `CombatFirstUseRehearsal.Base`와 `FreshCommand`).
   - 기회 판 = 위험 판에서 플레이어 턴 끝(`EndPlayerTurn`) → 적 턴 시작.
   - 난수는 고정 씨앗(TP-3).
2. 응수 고르기: 유닛마다 공격 후보 전부를 돌려 순이득 최대 하나(0 이하면 없음). 대상별로 합쳐 HP에서 자른 손실.
3. 다시 매김: 시작 판 + 단계마다 남긴 판에서 빠른 V 상위 4개를 깊은 V로 매겨 고른다.
4. 배치 컴파일 → 사용자 재생 확인.

**CP-c 예산·진단** (끝나면 첫 행동까지 기다리는 시간이 정해진다)
1. 한 단계 크기를 "행동 하나 돌리고 매기기" 정도로 고르게 맞춘다.
2. 진단: 계획 수·다시 계획 수·돌린 수·다시 매김 수를 기존 진단 칸에 넣는다.
3. 사용자가 Profiler로 첫 행동까지 시간을 잰다. 예산을 바꿀지 정한다(TP-11).

**CP-d 보관·문서** (끝나면 develop을 갱신해도 다시 붙일 수 있다)
1. `LLMTest/core_port/hooks.patch`(훅 다섯 곳의 diff)와 갱신 절차(§4)를 쓴다.
2. 사용법: 에셋에서 켜고 끄는 법, Profiler 읽는 법.
3. 이 문서 구현 메모, 인계 문서(§0·§2·§3·§5·§9·§10), 메모리.

### 9.5 진행 규칙
- 단계마다 멈추고 보고한다. 계획과 다른 결정이 필요하면 구현하지 말고 먼저 묻는다.
- 새 자동 시험은 만들지 않는다(Q-CP5 A). 바뀐 코드 때문에 기존 시험이 컴파일에서 깨지면 호출만 맞추고 보고한다.
- 커밋은 사용자가 한다(지시가 있으면 Claude가 한다).
- 연구 쪽 Python 코드·결과 폴더는 바꾸지 않는다.

## 10 구현 기록 (2026-11-18 사용자 "CP-d 까지 진행해")

### 10.1 한 것

| 단계 | 한 것 |
|---|---|
| CP-0 | 폴더 `Assets/ResearchEnemyPlanner/`와 `.asmref`, 연결 줄 다섯 곳(§9.3). 고치기 전 판을 Unity 배치 모드로 컴파일(오류 0) |
| CP-a | 공정 입력·판 서명·행동 돌리기(`PlannerBoard.cs`), 빠른 V(`PlannerEvaluator.cs`), 후보(`PlannerCandidates.cs`), 빔(`PlannerBeam.cs`), 입구·보관·거절 처리(`EnemyTurnPlanner.cs`·`EnemyTurnPlan.cs`) |
| CP-b | 깊은 V(`PlannerReply.cs`): 위험 판(적 턴 끝 → 플레이어 턴 시작 → 명령 입력), 기회 판(플레이어 턴 끝 → 적 턴 시작), 응수 고르기, 다시 매김 |
| CP-c | 진단 칸 넷(`CacheHits`·`CapHits`·`ExpandedNodes`·`PreviewCalls`, 뜻은 `core_port/README.md` §2). 프레임 예산은 바꾸지 않음(TP-11, §10.4) |
| CP-d | 연결 줄 패치 `core_port/hooks.patch`, 사용법·갱신 절차 `core_port/README.md`, Unity 밖 측정 도구 `core_port/harness/`, 인계 문서 |

### 10.2 구현 중 정한 것 (계획과 다르거나 계획에 없던 것)

1. **CP-0의 "측정 모드"는 만들지 않았다.** 그 대신 Unity 밖 측정 도구(`core_port/harness/`)로 실제 계획 비용을 쟀다(TP-4 바뀜). Core 소스와 `Assets/Tests/EditMode`의 Core 시험 도우미를 .NET 8로 묶어 빌드하고, 벤치마크 판(`EnemyAiBenchmarkFixtures`)에서 계획 AI를 돌린다. 저장소 시험이 아니라 측정 도구다(Q-CP5 A). Unity Profiler 측정은 사용자 몫으로 남는다.
2. **도달 표:** 이동으로 닿는 칸마다 `CombatAttackQueries.RangeCells`(게임의 공격 범위 표시와 같은 칸 규칙)를 모으고, 스플래시는 그 칸 둘레 모양(`AppointShapeCalculator.CreateSplashOffsets`)에 스플래시 %를 준다. 계획 §9.4 CP-a 3은 "조준마다 덮는 칸"이었지만, 조준마다 선택 계산(`Selection`)을 하면 너무 무거워 바꿨다. 지정 칸 100·스플래시 %는 같다. 방향형 공격(부채꼴 등)은 모든 방향 모양의 합이 된다.
3. **공정 입력:** 처음에는 `VisibleTo(Owner.Enemy)`로 환경을 바꿨는데, 벽 형상 검사("벽은 양쪽에 공개돼야 한다")에 걸려 벽이 있는 판에서 멈췄다. `VisibleSteps(Owner.Enemy, …, preservePublic: true)`로 바꿔, 적이 모르는 칸만 빼고 양쪽에 공개된 칸은 원본 그대로 둔다.
4. **행동 하나가 Core 검사 예외로 멈추면** 그 후보만 거절로 보고 센다(`PlannerSimulator.Failures`). 계획 전체에서 예외가 나면 그 행동은 지금 AI로 고르고 `CapHits`를 센다. 게임이 멈추지 않게 하려는 것이다. 측정 도구는 이 거름 없이 계획을 직접 돌린다.
5. **거절 뒤 다시 계획(Q-CP4 A):** 보관 계획에서 꺼낸 행동이 거절됐을 때만 지금 판으로 다시 계획한다. 이번 판에서 막 계획한 행동이 거절되면, 다시 계획해도 같은 결정이 나오므로(계산이 결정적이다) 바로 지금 AI로 고른다.
6. **판 서명(TP-2)에서 보호막은 뺐다.** 캐릭터 사실에 보호막 칸이 따로 없다(상태 효과 안에 있다). 예측과 실제가 보호막만 다르면 다시 계획하지 않는다.
7. **증원 전력:** 남은 증원 수 × (그 유닛 키 프로필의 공격력 × 최대 HP) × 0.5. 프로필에 없는 키는 0으로 본다.
8. **다음 턴 공격 가능:** 적 유닛(우리 편)은 무장 해제 같은 제약을 보지 않는다(이번 턴 끝에 풀린다고 본다, 시뮬레이터 `own` 규칙). 플레이어 유닛은 지금 제약을 본다.
9. **도발:** 도발당한 적은 도발한 유닛을 칠 수 있으면 그 조준만 남긴다. 못 치면 다른 대상 공격만 빼고 이동은 그대로 둔다. 지금 AI처럼 도발한 쪽으로 쫓아가는 이동을 따로 고르지는 않는다(빔이 V로 고른다). 2단 평가의 응수에는 도발 규칙을 넣지 않았다(시뮬레이터와 같다).
10. **파일 나눔:** §9.2의 `PlannerFacts.cs`·`PlannerReach.cs`는 `PlannerEvaluator.cs` 하나로 합쳤다. 행동 돌리기(`PlannerSimulator`)는 `PlannerBoard.cs` 안에 둔다.
11. **계획 수치**는 계획 객체(`EnemyTurnPlanStats`)에 담는다. Core에 정적 가변 상태를 두지 않으려는 것이다.
12. **측정 도구의 벤치마크 판:** 판의 메인 지속 상태(`Main`)를 판의 메인 캐릭터에 맞춰 고친다. 시험 판은 빈 판의 값을 쓰는데, 지금 AI는 플레이어 메인을 움직여 보지 않아 드러나지 않던 불일치다. 그대로 두면 2단 평가에서 메인이 이동하며 영혼을 모을 때 "메인 수집 입력이 지속 원본과 다릅니다"로 멈춘다.

### 10.3 확인한 것

- **Unity 배치 컴파일:** 고치기 전 오류 0 → 고친 뒤 오류 0(우리 파일 경고 0). 중간에 툴팁 문자열 안에 줄바꿈이 들어간 오류 1건이 있었고 고쳤다.
- **기존 Core 적 AI 시험**(측정 도구로 `class =~ Enemy`, 저장소 루트에서): 614개 중 통과 603, 실패 0, 건너뜀 11(따로 돌릴 때만 도는 측정용). 지금 AI의 결정 기록을 기준 기록과 대조하는 시험(`CapturedBaselineTraceMatches` 등)이 들어 있으므로, **기본값(지금 AI) 동작은 바뀌지 않았다.**
- **벤치마크 판 11개 × 씨앗 1·7**(적 턴 전체를 게임처럼 행동마다 새 세션으로 돌림): 모두 끝까지 돌았다. 거절 0, 지금 AI로 넘긴 것 0, 후보 예외 0. 씨앗 1과 7은 같은 결과였다.

| 판 | 적 | 계획 한 번(ms, 씨앗 1) | 엔진으로 돌린 행동(빔 + 2단) | 빠른 V로 매긴 판 | 계획한 행동 | 적 턴 | 다시 계획 |
|---|---:|---:|---:|---:|---:|---|---:|
| N5 | 5 | 2,720 | 2,059 + 1,532 | 1,318 | 10 | 모두 이동 + 공격 | 0 |
| N6 | 6 | 3,613 | 2,792 + 1,628 | 1,780 | 12 | 〃 | 0 |
| N8 | 8 | 5,376 | 4,058 + 1,928 | 2,444 | 16 | 〃 | 0 |
| W8(벽에 틈) | 8 | 2,119 | 1,845 + 531 | 349 | 8 | 모두 이동만 | 0 |
| U8(벽이 막음) | 8 | 2,149 | 1,841 + 570 | 346 | 0 | 아무것도 안 함 | 0 |
| A8(공격 방식 넷) | 8 | 5,065 | 3,186 + 1,792 | 1,709 | 16 | 모두 이동 + 공격 | 0 |
| T8(도발) | 8 | 6,767 | 2,350 + 1,793 | 1,428 | 15 | 〃 | 0 |
| R8(선로·낙석) | 8 | 5,476 | 4,058 + 1,444 | 2,444 | 16 | 〃 | 0 |
| X8(토네이도) | 8 | 4,403 | 2,589 + 1,688 | 1,206 | 15 | 〃 | 0 |
| H8(숨은 지뢰·하붑) | 8 | 3,614 | 2,627 + 1,400 | 1,230 | 15 | 16행동(한 번 다시 계획) | 1 |
| G16 | 16 | 15,025 | 9,100 + 0 | 5,157 | 20 | 메인 처치로 끝(2단 후보가 모두 이긴 판) | 0 |

- N5 첫 줄의 시간에는 처음 실행 준비(JIT)가 섞여 있어 두 번째 실행 값을 적었다. 시간은 .NET 8(이 PC) 기준이고 Unity(Mono)에서는 재지 않았다.
- H8의 다시 계획 한 번은 계획이 모르는 숨은 지뢰 때문으로 본다(추정, 행동 기록을 대조하지 않음). 공정 입력이 의도대로 숨은 칸을 보지 않는다는 뜻이기도 하다.
- U8에서 아무것도 안 하는 것은 시뮬레이터 식과 같다. 길이 없으면 걸음 벌점이 0이라 움직일 까닭이 없다. 지금 AI는 이 판에서 움직인다.

### 10.4 비용과 프레임 예산 (사용자 결정 대기)

N8 판에서 데운 뒤 잰 기본 비용(측정 도구 `profile`):

| 일 | 한 번에 |
|---|---:|
| 행동 하나 돌리기(`AdvancePrepared` + `Project`) | 약 0.66ms |
| 공격 범위 조회(유닛 하나, 게임 입구 `GetAttackRange`) | 약 0.67ms |
| 빠른 V 하나 | 약 1.2ms (위험 0.44 + 기회 0.34 + 새 칸의 도달 표 등) |
| 도달 표 하나(처음 만들 때) | 약 0.43ms |
| 지금 AI 결정 하나(참고) | 약 8~60ms |

- 계획 한 번은 행동을 3천~1만 번 돌리고 판을 1천~5천 개 매겨 2~16초가 걸린다. 가장 큰 몫은 Core가 행동 하나를 정산하는 무게다. 시뮬레이터의 Python 엔진보다 훨씬 무겁다.
- **게임에서 기다리는 시간(추정):** 게임은 적 결정을 프레임당 1ms씩만 계산한다(`CombatAdvanceService.BudgetSpent`). 계획 단계 하나가 1ms 안팎이라 프레임마다 한두 단계만 돈다. 계획 단계는 3,600~9,500개라 첫 행동까지 60fps 기준 약 30초~3분을 기다릴 수 있다. 행동이 계획대로 나오면 그다음 행동은 보관 계획에서 바로 꺼낸다.
- 선택지:
  - **A 계획 AI만 예산을 키운다.** `CombatAdvanceService`에 연결 줄이 하나 늘어난다(TP-11). 예를 들어 12ms면 첫 행동까지 약 4~20초(추정), 그동안 프레임이 떨어진다.
  - **B 계산을 줄인다.** 예: 2단 평가 응수에서 이동 + 공격을 빼기, 빔·후보 수 줄이기, 2단 평가 끄기(`plan4:r0:g1` 상당). 시뮬레이터에서 잰 판과 달라지므로 다시 재야 한다.
  - **C 구현을 빠르게 한다.** 공격 범위 조회 기억, 같은 판 평가 재사용 등. 엔진 정산 비용이 대부분이라 효과에 한계가 있다(추정).
- 권장: 먼저 A(예: 12ms)로 실제 재생에서 기다리는 시간을 보고, 그다음 B·C를 정한다.
- **A를 넣었다(2026-11-18 사용자 "응, 유니티 닫았어 넣어줘").** `CombatAdvanceService.BudgetSpent`가 결정 설정이 `TurnPlanner`일 때만 12ms(`TurnPlannerBudgetMilliseconds`)를 쓴다. 지금 AI는 1ms 그대로다. 연결 줄은 여섯 곳이 됐다(`core_port/hooks.patch`). Unity 배치 컴파일 오류 0, 관련 Unity 시험(`EnemyAdvancePreparation*`·`EnemyAiTuning*`) 30개 중 통과 28·실패 0·건너뜀 2(따로 돌릴 때만 도는 측정용). B는 시뮬레이터에서 재 봤고 빔 거름은 손해, 응수 거름은 손해가 판정되지 않았다([속도 개선 설계](2026-11-18-planner-candidate-pruning-design.md) §9·§10).

### 10.5 남은 것

- 프레임 예산: A를 넣음(§10.4). 사용자 재생(2026-11-18): 적 턴 계산 중 140fps → 60fps 안팎, 첫 행동까지 30초 이상. → 별도 스레드·병렬 설계 [planner-background-parallel-design](2026-11-18-planner-background-parallel-design.md)(Q-BP1~Q-BP3 A). BG-a(일 스레드)로 12ms 예산 줄은 걷었다(예산은 다시 모두 1ms).
- Unity 재생 확인: 에셋을 `TurnPlanner`로 바꿔 전투 몇 판(사용자). 지금 예산이면 첫 행동까지 오래 기다린다.
- 커밋(사용자). 새 파일: `Assets/ResearchEnemyPlanner/`(+ `.meta`, `Assets/ResearchEnemyPlanner.meta`), `LLMTest/core_port/`, 이 문서. 고친 파일: 연결 줄 다섯 곳, 인계 문서. Unity 배치 실행이 스스로 바꾼 파일(`Assets/ProjectL/Resources/Font/SCDream7 SDF.asset`, `ProjectSettings/Packages/com.unity.sequences/Settings.json`, `ProjectSettings/ShaderGraphSettings.asset`)은 이 작업이 아니다.
- Core에서 계획 AI와 지금 AI를 여러 판 겨루는 측정(Q-CP6) — 뒤에 따로 설계.
- 시뮬레이터와 판 평가 값 대조(TP-10) — 위 측정과 함께.
