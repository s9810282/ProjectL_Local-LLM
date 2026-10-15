# StageSelect 노드 → 전투 진입 — Core 요청사항

- 작성일: **2026-10-06**
- 작성: 스테이지 선택 View / 대상: **천명님(Core)**. R1·R2가 가리키는 `DatabaseBattleInitializationSource`·`CharacterRosterService`는 모두 천명님 코드다.
- 코드 기준: `feature/Hojin/Stage_Shop_Event`, `a6d1fdbc`. 천명님 최신 브랜치 `feature/Cheon/refactor/core-system`(`cb3f2ec6`, 2026-10-06)에서도 `DatabaseBattleInitializationSource.cs`는 `81c8c38c`(2026-10-03) 이후 바뀌지 않았다(이 PC가 마지막으로 받은 원격 정보 기준).
- 상태: **문서 준비 완료, 외부 전달 전.** Core 생산 코드는 수정하지 않았다.
- 이 문서는 StageSelect에서 노드를 눌러 전투로 들어가는 **운영 경로**에서 막히는 지점만 다룬다. 이미 요청한 내용은 다시 적지 않고 원본에 링크한다. 원본: [2026-10-05 Combat Core 요청사항](2026-10-05-combat-core-requests-phase6b.md).

## 1. 현재 경로와 막히는 곳

| 단계 | 담당 | 코드 | 상태 |
|---|---|---|---|
| 1. 노드 클릭 → 진입 요청 기록 | View | `StageMapInteraction` → StageView Buffer `SelectStageNode` | 연결됨 |
| 2. Combat 노드만 골라 진입 요청 | View | `StageMapEntryFlow` → `ICombatEntryService.RequestEntry(CurrentMapKey, nodeId)` | 연결됨 (Roslyn 컴파일만 확인, Play 미검증) |
| 3. 진입 Job → Core 호출 | 골격 | `CombatPrepareJob` → `CoreGroup.Combat.EnterBattle(chapterKey, nodeId)` | 연결됨 |
| 4. 전투 정의 조회 | **Core** | `EnterBattle` → `ReadDefinition` → `DatabaseBattleInitializationSource.Read` | **항상 `T1/G7` 예외 → 오류 경계에서 정지** |
| 5. 승인 뒤 Combat 전환 | 골격 | `CombatLifeCycle` → `LifeCycleManager.Change(Combat)` | 연결됨 |

- `RequestEntry`의 첫 인자로 `CurrentMapKey`를 넘긴다. `EnterBattle`도 `CurrentMapKey`를 챕터 키로 비교하므로 같은 키로 본다. 다르게 해석해야 하면 알려 달라.
- 개발용 `DevBattleInitializationSource`는 GameBootstrap이 **Combat으로 바로 시작할 때만** 주입한다. StageSelect에서 시작하면 항상 4단계의 DB source를 쓴다. 이 경로를 개발 fixture로 우회하지 않았다.

## 2. 요청

| 번호 | 요청 | 원본 / 상세 | 필요한 시점 |
|---|---|---|---|
| **R1** | **T1/G7 차단 해제 가능 여부 확인과 기본 source 완성** (아래 §2.1) | [기존 요청 2](2026-10-05-combat-core-requests-phase6b.md#1-필요한-구현-세-가지), [DB 경계 인계 §5.6](2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달). 완료 조건은 원본을 따른다 | **StageSelect → 전투 진입의 유일한 차단 요인.** 이 경로로 운영 진입을 검증하려면 먼저 필요 |
| **R2** | **새 Run 시작 경계 확인** (아래 §2.2) | 새 질문. 기존 요청 3 §1.1의 확인 항목 1(새 Run 시작 API)과 같은 경계이므로 함께 회신해도 된다 | R1과 함께. 로스터가 없으면 R1이 끝나도 운영 진입 입력이 비어 있다 |

### 2.1 R1 — 막아 둔 조건이 충족됐는지 확인

`DatabaseBattleInitializationSource.Read`의 `T1/G7` 예외는 `6367026e`(2026-10-01)에서 의도적으로 넣은 차단이다. 바로 위 주석이 이유를 적고 있다: "저작 스키마가 확정되기 전에는 키·기존 enum으로 새 방식의 원본을 만들지 않는다."

그 저작 스키마는 2026-10-04 View A2로 제공됐다([DB 경계 인계 §5.6](2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달)).

- `TargetingMethodData`(`MethodKey`·`Shape`·`ChoiceMode`·`Splash`·`SplashPercent`)와 `TargetingMethodTable`
- `UnitData.TargetingMethodKey`(SO 참조를 `GetData()`에서 키로 변환), Factory 조립 때 키 교차 검사
- `IInGameQuery.TargetingMethodTableQuery`(`GetTargetingMethod`·`TryGetTargetingMethod`·`GetAllTargetingMethods`)
- 에셋: MethodKey 1(RadialSquare/Single), 2(Fan/Direction)와 유닛 5개 연결
- 검증: [Stage 병합 검증 기록](2026-10-04-stage-merge-verification.md#verification)의 Unity EditMode 2,255/2,255 통과에 A2 테스트가 포함됨. 운영 전투 진입까지 검증한 것은 아니다.

**질문:**

1. 위 데이터로 주석의 차단 조건이 충족됐는가? 충족됐다면 이 키로 `TargetingMethodSpec`을 조립하고, `BattleInitialization` 전체를 반환하도록 연결해 달라. 옛 enum 추정 변환이나 예외 삭제만으로 끝내지 않는다는 기존 조건은 그대로다.
2. 아직 부족한 것이 있다면 **무엇이 빠졌는지**(테이블 필드, 조회 API, 에셋 값, 다른 정의) 구체적으로 알려 달라. View 쪽에서 채울 수 있는 데이터면 View가 제공한다.

### 2.2 R2 — 새 Run 시작 경계

**확인한 사실 (`a6d1fdbc`):**

- `ICharacterRosterService.InitializeRoster(loadoutKey)`는 있지만, 운영 경로에서 호출하는 곳이 없다. 호출자는 GameBootstrap 개발 경로(Combat 직접 시작)와 `TemporaryCoreRuntimeFactory`(개발 조립)뿐이다.
- `StagePlan`에 `JobType.NewRun`(`ResetRunProgressJob` → `StageJob`)이 있지만, 등록하는 곳이 없다. `StageLifeCycle`은 `JobType.Start`만 등록한다.
- `DatabaseBattleInitializationSource.Read`는 보유 로스터(`CharacterReadSink.Owned`)를 입력으로 쓴다.

**질문:**

1. 새 Run을 시작할 때 Core가 **하나의 공개 경계**(로스터 초기화 + Run 진행 초기화 + Run 재화 초기화)를 제공할 계획인가? 아니면 호출자가 `InitializeRoster`와 `NewRun` Job을 각각 부르는 것이 의도인가?
2. 각각 부르는 것이 의도라면 **호출 순서**와 각 단계의 거절·실패 시 **되돌림 의미**(예: 로스터는 성공했는데 맵 준비가 실패한 경우)를 알려 달라.
3. 로스터가 비어 있는 상태에서 `EnterBattle`을 부르면 어떤 결과가 나와야 하는가(거절 사유 / 예외)?
4. 로드아웃 키의 운영 원본은 무엇인가? 현재 DB에는 `PlayerLoadout_1`만 있다. 선택 UI가 없다면 기본 로드아웃 키를 어디서 읽어야 하는지 알려 달라.

## 3. View·골격에서 할 일 (요청 범위 밖)

- **새 Run 호출자 배치**(어느 LifeCycle·화면에서 부를지)는 Flow·Plan·Job 담당인 View·골격이 정한다([기존 요청 문서 §3](2026-10-05-combat-core-requests-phase6b.md#3-골격view에서-할-일과-요청-범위-밖)과 같은 경계). Core에는 위 계산·초기화 경계만 묻는다.
- 진입 거절의 화면 표시는 지금은 로그뿐이다. 거절 사유별 UI는 View 후속 작업이다.
- `StageLifeCycle.OnExit`의 `StageMapFlow 종료` TODO는 View 쪽 정리 항목이다.

## 4. 회신 항목과 상태

요청별 **실제 파일·API 서명·호출 시점·반환/실패/중복 호출 의미·검증 결과**를 알려 달라. 정해지지 않은 게임 규칙은 미정이라고 적고, 임의 값으로 채우지 않는다.

| 항목 | 문서 준비 | 외부 전달 | 답변/API 인수 |
|---|---|---|---|
| R1 T1/G7 차단 해제·기본 source | 완료 (원본: 기존 요청 2, 차단 조건 확인은 §2.1) | 미확인 | 미수신 |
| R2 새 Run 시작 경계 | 완료 | 전 | 미수신 |

이 문서는 전달용 요청 정리다. Core 구현이나 담당자 합의, 외부 발송이 끝났다는 뜻이 아니다.
