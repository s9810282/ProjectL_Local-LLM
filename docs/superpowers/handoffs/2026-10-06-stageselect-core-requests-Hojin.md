# StageSelect Core 요청

- 작성자: 호진(이호진)

StageSelect에서 Core 작업자(천명님)에게 보낸 요청(전투 진입, 기획 규칙 차이, Run seed, 요청 처리 번호)을 모았다. 원래 문서를 부(部)로 나눠 내용 그대로 담았고, 각 부의 결정 번호와 절 번호는 원래 문서와 같다.

| 부 | 원래 문서 |
|---|---|
| [1부](#1부-stageselect-노드--전투-진입--core-요청사항) | StageSelect 노드 → 전투 진입 — Core 요청사항 |
| [2부](#2부-통합-기획서와-상점회복스테이지-규칙-차이-view--core기획-확인-요청) | 통합 기획서와 상점·회복·스테이지 규칙 차이 (View → Core·기획 확인 요청) |
| [3부](#3부-run-seed-도입-요청-view--core) | Run seed 도입 요청 (View → Core) |
| [4부](#4부-stageselect-요청-처리-번호-기록-요청-view--core) | StageSelect 요청 처리 번호 기록 요청 (View → Core) |
| [5부](#5부-상점-팝업-정리에서-옮긴-천명님-줄-view--core-알림-2026-10-27-미전달) | 상점 팝업 정리에서 옮긴 천명님 줄 (View → Core 알림, 2026-10-27) — 요청 아님 |
| [6부](#6부-검사-후속-정리-요청-묶음--seed-다시참조-검사아티팩트-buffer-2026-10-29-미전달) | 검사 후속 정리 요청 묶음 — seed 다시·참조 검사·아티팩트 Buffer (2026-10-29) |

---

## 1부. StageSelect 노드 → 전투 진입 — Core 요청사항

- 작성일: **2026-10-06**
- 작성: 스테이지 선택 View / 대상: **천명님(Core)**. R1·R2가 가리키는 `DatabaseBattleInitializationSource`·`CharacterRosterService`는 모두 천명님 코드다.
- 코드 기준: `feature/Hojin/Stage_Shop_Event`, `a6d1fdbc`. 천명님 최신 브랜치 `feature/Cheon/refactor/core-system`(`cb3f2ec6`, 2026-10-06)에서도 `DatabaseBattleInitializationSource.cs`는 `81c8c38c`(2026-10-03) 이후 바뀌지 않았다(이 PC가 마지막으로 받은 원격 정보 기준).
- 상태: **문서 준비 완료, 외부 전달 전.** Core 생산 코드는 수정하지 않았다.
- **2026-10-13 종결:** 두 요청 모두 Core 작업으로 해소됐다. R1: 천명님 `5f2d0381`(2026-10-06, 이 브랜치 병합 `f68408b1`)로 T1/G7 차단이 풀려 기본 DB source가 전투 정의를 돌려주며, 같은 날 StageSelect → Combat 실제 진입을 확인했다([스테이지 맵 계획 §3.1](../plans/2026-10-10-stage-map-plan-Hojin.md#31-스테이지-선택-view-1번부터-6번까지)(옛 SA 계획 "2부 스테이지 선택 View 계획" §7)의 2026-10-06 기록). R2: 새 Run은 공개 경계 `IRunStartService.StartNewRun(int loadoutKey)`(`RosterAcquireResult` 반환) 하나로 제공됐다. 로드아웃 키는 운영 선택 UI 전까지 1을 쓰며, 같은 값이 `StagePlan.DefaultLoadoutKey`와 `CombatPlan.NewRunLoadoutKey` 두 곳에 있다. §1의 `StageMapEntryFlow`와 §3의 `StageMapFlow` TODO는 이후 SA에서 `StageNodeEntryFlow`로 바뀌어 없다.
- 이 문서는 StageSelect에서 노드를 눌러 전투로 들어가는 **운영 경로**에서 막히는 지점만 다룬다. 이미 요청한 내용은 다시 적지 않고 원본에 링크한다. 원본: [2026-10-05 Combat Core 요청사항](2026-10-05-combat-core-requests-phase6b.md).

### 1. 현재 경로와 막히는 곳 (요청 당시 상태 → 바뀜: 위 2026-10-13 종결 줄)

| 단계 | 담당 | 코드 | 상태 |
|---|---|---|---|
| 1. 노드 클릭 → 진입 요청 기록 | View | `StageMapInteraction` → StageView Buffer `SelectStageNode` | 연결됨 |
| 2. Combat 노드만 골라 진입 요청 | View | `StageMapEntryFlow` → `ICombatEntryService.RequestEntry(CurrentMapKey, nodeId)` | 연결됨 (Roslyn 컴파일만 확인, Play 미검증) |
| 3. 진입 Job → Core 호출 | 골격 | `CombatPrepareJob` → `CoreGroup.Combat.EnterBattle(chapterKey, nodeId)` | 연결됨 |
| 4. 전투 정의 조회 | **Core** | `EnterBattle` → `ReadDefinition` → `DatabaseBattleInitializationSource.Read` | **항상 `T1/G7` 예외 → 오류 경계에서 정지** |
| 5. 승인 뒤 Combat 전환 | 골격 | `CombatLifeCycle` → `LifeCycleManager.Change(Combat)` | 연결됨 |

- `RequestEntry`의 첫 인자로 `CurrentMapKey`를 넘긴다. `EnterBattle`도 `CurrentMapKey`를 챕터 키로 비교하므로 같은 키로 본다. 다르게 해석해야 하면 알려 달라.
- 개발용 `DevBattleInitializationSource`는 GameBootstrap이 **Combat으로 바로 시작할 때만** 주입한다. StageSelect에서 시작하면 항상 4단계의 DB source를 쓴다. 이 경로를 개발 fixture로 우회하지 않았다.

### 2. 요청 (→ 바뀜: R1·R2 모두 해소, §4 표)

| 번호 | 요청 | 원본 / 상세 | 필요한 시점 |
|---|---|---|---|
| **R1** | **T1/G7 차단 해제 가능 여부 확인과 기본 source 완성** (아래 §2.1) | [기존 요청 2](2026-10-05-combat-core-requests-phase6b.md#1-필요한-구현-세-가지), [DB 경계 인계 §5.6](2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달). 완료 조건은 원본을 따른다 | **StageSelect → 전투 진입의 유일한 차단 요인.** 이 경로로 운영 진입을 검증하려면 먼저 필요 |
| **R2** | **새 Run 시작 경계 확인** (아래 §2.2) | 새 질문. 기존 요청 3 §1.1의 확인 항목 1(새 Run 시작 API)과 같은 경계이므로 함께 회신해도 된다 | R1과 함께. 로스터가 없으면 R1이 끝나도 운영 진입 입력이 비어 있다 |

#### 2.1 R1 — 막아 둔 조건이 충족됐는지 확인

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

#### 2.2 R2 — 새 Run 시작 경계

**확인한 사실 (`a6d1fdbc`):**

- `ICharacterRosterService.InitializeRoster(loadoutKey)`는 있지만, 운영 경로에서 호출하는 곳이 없다. 호출자는 GameBootstrap 개발 경로(Combat 직접 시작)와 `TemporaryCoreRuntimeFactory`(개발 조립)뿐이다.
- `StagePlan`에 `JobType.NewRun`(`ResetRunProgressJob` → `StageJob`)이 있지만, 등록하는 곳이 없다. `StageLifeCycle`은 `JobType.Start`만 등록한다.
- `DatabaseBattleInitializationSource.Read`는 보유 로스터(`CharacterReadSink.Owned`)를 입력으로 쓴다.

**질문:**

1. 새 Run을 시작할 때 Core가 **하나의 공개 경계**(로스터 초기화 + Run 진행 초기화 + Run 재화 초기화)를 제공할 계획인가? 아니면 호출자가 `InitializeRoster`와 `NewRun` Job을 각각 부르는 것이 의도인가?
2. 각각 부르는 것이 의도라면 **호출 순서**와 각 단계의 거절·실패 시 **되돌림 의미**(예: 로스터는 성공했는데 맵 준비가 실패한 경우)를 알려 달라.
3. 로스터가 비어 있는 상태에서 `EnterBattle`을 부르면 어떤 결과가 나와야 하는가(거절 사유 / 예외)?
4. 로드아웃 키의 운영 원본은 무엇인가? 현재 DB에는 `PlayerLoadout_1`만 있다. 선택 UI가 없다면 기본 로드아웃 키를 어디서 읽어야 하는지 알려 달라.

### 3. View·골격에서 할 일 (요청 범위 밖)

- **새 Run 호출자 배치**(어느 LifeCycle·화면에서 부를지)는 Flow·Plan·Job 담당인 View·골격이 정한다([기존 요청 문서 §3](2026-10-05-combat-core-requests-phase6b.md#3-골격view에서-할-일과-요청-범위-밖)과 같은 경계). Core에는 위 계산·초기화 경계만 묻는다.
- 진입 거절의 화면 표시는 지금은 로그뿐이다. 거절 사유별 UI는 View 후속 작업이다.
- `StageLifeCycle.OnExit`의 `StageMapFlow 종료` TODO는 View 쪽 정리 항목이다.

### 4. 회신 항목과 상태

요청별 **실제 파일·API 서명·호출 시점·반환/실패/중복 호출 의미·검증 결과**를 알려 달라. 정해지지 않은 게임 규칙은 미정이라고 적고, 임의 값으로 채우지 않는다.

| 항목 | 문서 준비 | 외부 전달 | 답변/API 인수 |
|---|---|---|---|
| R1 T1/G7 차단 해제·기본 source | 완료 (원본: 기존 요청 2, 차단 조건 확인은 §2.1) | 미확인 | 해소(`5f2d0381`, 2026-10-13 확인) |
| R2 새 Run 시작 경계 | 완료 | 전 | 해소(`StartNewRun`, 2026-10-13 확인) |

이 문서는 전달용 요청 정리다. Core 구현이나 담당자 합의, 외부 발송이 끝났다는 뜻이 아니다.

---

## 2부. 통합 기획서와 상점·회복·스테이지 규칙 차이 (View → Core·기획 확인 요청)

- 작성일: 2026-10-09
- 작성: View 작업(스테이지 선택 담당, 호진)
- 받는 사람: Core 작업자(천명님), 기획 확인 필요
- 상태: **미전달·미합의**
- **2026-10-13 현재:** 여전히 미전달·미합의이며 G1~G5 회신과 기획 답변 담당이 없다. G2의 "`Event_Recovery` 선택지 2개"는 이후 3개가 됐다.
- 기준 기획서: `Dropbox/Project L/기획 문서/통합 기획서.pptx` Ver0.3(2026-09-28 수정), 슬라이드 12~28
- 관련: [상점·회복 UI 설계](../specs/2026-10-06-stage-shop-recovery-ui-design-Hojin.md), [상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md), [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §13.16

### 0. View의 현재 결정 (사용자, 2026-10-09)

상점은 **지금 Core 규칙(진열된 캐릭터 구매) 기준으로 먼저** 만든다(A안). 아래 차이가 확정·구현되면 View는 표시 모델(카드 내용)만 바꾼다. 요청 흐름·팝업 구조는 그대로 쓴다.

### 1. 차이

| 번호 | 항목 | 기획서 | 현재 Core·DB | 확인·요청 |
|---|---|---|---|---|
| G1 | 상점(= "서브 캐릭터 획득") 규칙 | 카드 3장 각각 **비용 + 캐릭터 목록**. 고르면 목록에서 **무작위 1명 획득**("캐릭터 뽑기"). 카드에 대표 이미지·"목록 : 전체/@@". 데이터 `ShopMaster`: Key·Name·Position(왼·가운데·오른)·Cost·CharacterList·Weight·Description | 후보(캐릭터+가격)에서 seed로 **정해진 캐릭터 3명** 진열 → 고른 캐릭터 **구매**(`EnterShop`·`Purchase`) | 기획 확정 여부 확인. 확정되면 Core에 뽑기 규칙·목록 데이터·결과(뽑힌 캐릭터) 게시 요청. 카드 한 장에서 여러 번 뽑을 수 있는지도 확인 |
| G2 | 회복 선택지 | **3개**(왼·가운데·오른), 회복량 절댓값·% 모두, 이미지(모닥불·텐트 등). 데이터 `HealMaster`: Position·Cost·Measure·Amount·Weight | `Event_Recovery` 선택지 2개, 위치·가중치 없음 | 선택지 수·위치·가중치(무작위 선택 여부) 확인. 이미지는 View DB(`RecoveryOptionVisual`)에 필드 추가로 처리 예정 |
| G3 | 비전투 스테이지 종류 | 5종: 메인 회복, 캐릭터 획득, 메인 성장 갈림길, 캐릭터 교환, 랜덤 이벤트 | 회복·상점 2종(`StageType.Event` = 회복) | 나머지 3종은 종류별 Core 계약이 필요(설계 S10). 우선순위 확인 |
| G4 | 맵 구성 | `StageMaster`의 RoleType(1 시작·2 일반·3 엘리트·4 보스·5 회복·6 상점), **Weight**, 연속 등장 가능 여부·최대 횟수로 등장 조절 | 등록된 일반 스테이지 중 무작위(`StageGeneratorSystem`), 엘리트 없음 | 맵당 상점·이벤트 수를 이 가중치 규칙으로 할지 확인. Excel `StageMaster` 임포터(민형님)와 연결 시점 확인 |
| G5 | 회복 선택 수 | "선택한다"/"선택하지 않는다"만 동작 | Core는 선택지 하나만 적용 | 일치. 상점에서 여러 장 구매 가능한지는 기획서에 없음 → 확인 |

### 2. View 영향 정리

- G1이 바뀌어도 View 변경: 카드 내용(이름 → 대표 이미지·목록 문구), 결과 문구(뽑힌 캐릭터 표시). 요청 종류 이름이 바뀌면 Job 호출만 교체.
- G2: View DB 이미지 필드, 카드 수는 이미 "선택지 수만큼" 설계.
- G3·G4: View는 노드 외형 등록과 분기만 추가.

### 3. 응답 양식

| 번호 | 확정/보류/변경 | 담당·시점·메모 |
|---|---|---|
| G1 | | |
| G2 | | |
| G3 | | |
| G4 | | |
| G5 | | |

---

## 3부. Run seed 도입 요청 (View → Core)

- 작성일: 2026-10-09
- 작성: View 작업(스테이지 선택 담당, 호진)
- 받는 사람: Core 작업자(천명님)
- 상태: **미전달·미합의·미구현**
- **2026-10-13 현재:** 여전히 미전달·미구현이며 R1~R5 회신이 없다. §3의 임시 처리는 이후 구현돼, 지금은 `JobRegisterSystem`의 `new Random().Next` 하나가 맵·상점 진입 seed를 만들어 Plan → Job → Core로 넘기고 Core는 받은 seed를 진입 결과(`StageEntryCommandResult.Seed`)에 남긴다. §3의 "구현 예정·코드 없음"은 당시 상태다.
- 관련: [상점·회복 UI 설계](../specs/2026-10-06-stage-shop-recovery-ui-design-Hojin.md) §7 Q1, [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §13.14

### 1. 요청 요약

우선 목표는 **상점 결과를 다시 만들 수 있는 기준**을 마련하는 것입니다.
새 Run을 시작할 때 Run seed 하나를 만들어 Run Buffer에 보관하고, **상점 seed는 Core가 현재 Run과 노드에서 직접 유도**해 주세요(R4).
최종 운영 경로에서 View는 seed를 만들지도, 섞지도 않습니다. 고정 seed 주입은 테스트용 경로로만 남깁니다.

### 2. 배경 (코드 확인, 2026-10-09)

| 무작위를 쓰는 곳 | 지금 seed 출처 |
|---|---|
| 상점 제안 `ICharacterShopService.EnterShop(chapterKey, nodeId, seed)` | **호출자가 넘김.** Buffer에 Run seed가 없어 호출자가 만들 근거가 없음 |
| 챕터 그래프 `StageGeneratorSystem` | 맵 설정 `Seed`, 또는 `RandomizeSeedEachRun`이면 `Environment.TickCount` |
| 적 배치 `EnemySpawnCalculator` | 전투 스테이지 DB `RandomSeed` |
| 첫 손패·턴 동점 순서 | 전투 스테이지 DB `TimelineSeed` |

- 상점 제안을 같은 조건에서 다시 만들 기준값이 없습니다. 버그 재현과 테스트 밖 결과 확인이 어렵습니다.
- 각 기능이 따로 seed를 만들면 "이 Run의 무작위"를 한 곳에서 관리할 수 없습니다(SSOT).
- 참고: 저장·불러오기는 생성된 상품·진행 상태를 저장하는 방식으로도 가능하므로 Run seed의 필수 조건은 아닙니다. 반대로 seed 하나만으로 Run 전체 재현이 완성되지도 않습니다(난수 사용 순서, 후보 순서, DB·규칙 버전도 결과에 영향). 이 요청은 범위를 **상점 결과**로 좁힙니다.

### 3. View의 임시 처리 (Run seed가 생기기 전까지)

상태: **구현 예정**(상점 Phase에서 만든다. 2026-10-09 현재 코드 없음). (→ 바뀜: 구현됨 — 위 2026-10-13 줄)

- 상점 진입 Job이 진입할 때마다 `System.Random`으로 seed를 만들어 `EnterShop`에 넘깁니다(설계 Q1 B안).
- 제안 3개는 `EnterShop`이 Buffer에 고정하고, 완료 전 같은 노드 재진입이 막혀 있으므로 화면 중 바뀌지 않습니다.
- seed 생성은 Job에 주입하는 함수 하나로 분리합니다(테스트는 고정값 주입). 이 함수는 임시 경로이며, R4가 오면 삭제합니다. View가 Run seed와 챕터·노드를 섞는 방식으로 바꾸지 않습니다(상품 생성 규칙이 View에 남기 때문).
- 한계: 테스트 밖에서는 같은 상점 결과를 다시 만들 수 없습니다.

### 4. 요청 사항

| 번호 | 요청 | 비고 |
|---|---|---|
| R1 | `StartNewRun` 때 Run seed 하나를 만들어 Run Buffer(`RunProgress`)에 보관 | 만드는 방법(시간 기반·외부 지정)은 Core가 정함. 개발·테스트용으로 고정 seed를 넣는 경로가 있으면 좋음 |
| R2 | (선택) 읽기 Sink에 공개(예: `IRunProgressBufferReadSink.RunSeed`) | 디버그 표시·재현 기록용. View가 seed 계산에 쓰지 않음 |
| R3 | 기능별 seed 유도 규칙을 Core가 소유 | 예: 상점 = Run seed + 챕터 + 노드. 같은 노드면 같은 Run에서 같은 제안 |
| R4 | **(권장 최종 경로)** 운영 경로의 상점 진입이 seed 인자 없이 Run seed에서 직접 유도 | 고정 seed를 넣는 경로는 테스트용으로 따로 유지 가능. 서명 형태는 Core가 정함. 이 경우 View 임시 seed 함수는 삭제 |
| R5 | (검토) 챕터 그래프 `RandomizeSeedEachRun`의 `Environment.TickCount`를 Run seed 유도로 교체할지 | Run 재현성을 맞추려면 필요. 결정은 Core |

### 5. 받은 뒤 View가 할 일

- R1·R3·R4가 오면: seed 함수와 Job 인자를 삭제하고 새 상점 진입 서명에 맞춤.
- R1만 오고 R4가 없으면: View가 섞지 않고 임시 무작위 seed를 유지한 채 R4를 다시 요청.
- 어느 경우든 상점 UI·요청 흐름은 바뀌지 않습니다.

### 6. 응답 양식

| 번호 | 수락/보류/거절 | 예정 시점·메모 |
|---|---|---|
| R1 | | |
| R2 | | |
| R3 | | |
| R4 | | |
| R5 | | |

---

## 4부. StageSelect 요청 처리 번호 기록 요청 (View → Core)

- 작성일: 2026-10-10
- 작성: View 작업(스테이지 선택 담당, 호진)
- 받는 사람: Core 작업자(천명님), H4 전투 진입 연동은 전투 실행 계층 담당자와 함께 확인
- 상태: **외부 미전달·미합의**. 2026-10-11 사용자 승인으로 H3/H4(P5/P6)에 이어 H1/H2(P7)도 로컬 구현했다. 이후 H1/H2 Core 입력 직접 조회의 자동 검증을 완료했고, 추가 승인된 H3/H4 직접 조회 구현·자동 테스트와 최종 빌드·독립 검토까지 완료했다. 최초 P56 번호·내용 API와 수치는 과거 기록이다. 현재 API·수명·교체 방법과 검증은 [P7 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md#2부-stageselect-p7-아티팩트-core-구현교체-인계), [P5/P6 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md)를 따른다.
- 관련: [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)(옛 StageSelect 구조 정렬 설계 SA), [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §13.13

> **현재 기준:** 사용자 병합 `9d4c76e5`와 그 위의 연결 보완이다. 아래 H1~H4는 로컬 구현된 계약의 외부 인수 요청이며, 아직 구현하지 않은 새 요청 목록이 아니다. 독립 `CombatEntryService`·`CombatPlan.Decide`·`Next`/`ExitCheck`와 맵 번호 연결, 현재 검증은 [스테이지 맵 계획 §4.2](../plans/2026-10-10-stage-map-plan-Hojin.md#42-병합-후-연결-보완)(옛 병합 검토 §7)를 따른다. 날짜별 원 요청·검증 기록은 보존한다.

### 1. 요청 요약

전투에서 만들어 주신 방식(`1f833e1c` 처리 번호, `6367026e` `ProcessNextCommand`)처럼, **아티팩트·상점·스테이지 노드 진입에서도 "몇 번 요청까지 처리했고 결과가 무엇인지"를 Core가 Core 쪽 Buffer에 남겨 주세요.**
편집·프리뷰·상점의 기존 처리 함수는 유지하면서 **요청 번호와 결과 게시**를 요청합니다. H4는 전투를 포함한 제출된 맵 진입 시도 전체로 확대하며, 미지원 이벤트/회복·없는 노드도 Core가 정상 거절을 기록할 경로가 필요합니다. 전투의 기존 준비/활성화 수명은 유지합니다.

### 2. 왜 필요한가

- 최초 요청은 View → 입력 Buffer → Flow → JobRegister → Plan → Job → Core → Buffer → View로 정렬하기 위한 것이었습니다. 현재 맵·상점·아티팩트는 이 경로를 사용하며, 전투의 진행 판단은 별도 Flow 대신 `CombatPlan.Decide`가 맡습니다.
- View는 "내가 보낸 N번 요청이 처리됐고 결과가 무엇인지"를 알아야 문구를 보여 주고 입력 Buffer를 정리할 수 있습니다.
- 2026-10-10 요청 당시에는
  - 아티팩트: `LastEditResult`·`LastPreview`에 요청 번호가 없어 어느 요청의 결과인지 알 수 없습니다.
  - 상점: 구매·완료 실패 사유가 반환값으로만 나오고 Buffer에 남지 않습니다.
- 프로젝트 원칙(2026-09-30 설계 원칙 §4.1): **처리 여부(마지막 처리 번호)는 Core 쪽 Buffer가 원본이고, View 입력 Buffer에 "처리됨"을 두지 않는다.** 그래서 View·실행 계층이 대신 기록하지 않고 요청드립니다.

### 3. 요청 사항

| 번호 | 도메인 | 요청 | Core 쪽에 남길 것 |
|---|---|---|---|
| H1 | 아티팩트 편집 | Core가 입력 Buffer에서 배치·이동·반환을 직접 조회. Job은 등록한 기대 번호만 전달 | 처리한 마지막 편집 번호 + 그 결과(`ArtifactActionResult`) |
| H2 | 아티팩트 프리뷰 | Core가 최신 프리뷰를 직접 조회하고 기대 번호 확인. 배치 안 된 인스턴스의 이동 프리뷰처럼 유효 입력의 계산을 생략하는 경우도 그 번호로 "건너뜀"을 남기기 | 응답한 마지막 프리뷰 번호 + 결과(`ArtifactPlacementPreview` 또는 건너뜀) |
| H3 | 상점 행동 | Core가 ShopView 입력을 직접 읽고 구매·완료 분기. Job은 기대 번호만 전달 | 처리한 마지막 상점 번호 + 결과 요약(수락 여부, `ShopFailureReason` / `ChapterProgressionFailureReason`) |
| H4 | 제출된 모든 맵 노드 진입 시도 | Core가 StageView 입력의 필수 챕터·노드를 직접 읽음. 전투는 기존 RequestEntry/Prepare 수명과 맵 기대 번호 유지, 일반 맵은 임시 seed 공급자만 추가 전달. 미지원/없는/도달 불가 노드도 정상 거절 | Core가 실행한 마지막 맵 요청 번호 + 수락/정상 거절 결과. 전투 수락은 준비 수락이며 Active/씬 전환 완료와 별개 |

전투도 H4의 번호 기록 대상입니다. 현재 맵 경로는 독립 `CombatEntryService.RequestEntry(chapter, node, N) → CombatPrepareJob → StageCommands.TryProcessNextBattleCommand(N, out result) → Prepared/Rejected`입니다. 수락된 같은 요청만 뒤따르는 `CombatEntryTransitionJob`에서 Activating으로 바꾸고 Combat 로딩 완료 뒤 Active가 됩니다. Core 서비스가 내부에서 기존 전투 준비를 수행하며 새로운 공통 진입 확정 단계를 앞에 붙이지 않습니다. 맵 번호가 없는 Bootstrap 준비 경로와 구분하고, Core를 실행하기 전 취소된 시도에는 H4 기록을 만들지 않습니다(§3.3).

#### 3.1 현재 후속 형태 (H1~H4 Core 직접 조회)

H1/H2는 직접 조회로 전환·자동 검증을 완료했다. H3/H4도 같은 조회 주체로 바꾸는 사용자 승인 후속 구현·자동 테스트를 완료했으며 최종 빌드·독립 검토도 완료했다. 기대 번호는 등록 이후 Clear/같은 Stage 재연결로 생긴 새 요청을 이전 Job이 처리하지 못하게 하는 보호값이다. 직접 조회나 무인자 서명은 Framework/UI의 MUST가 아니다.

| 영역 | 현재 계약 | 결과 읽기 |
|---|---|---|
| H1/H2 | `bool ArtifactCommands.ProcessNextCommand(int expectedRequestNumber)` / `ProcessLatestPreview(int expectedRequestNumber)` | `IBufferQuery.ArtifactCommandResults` |
| H3 | `bool StageCommands.ProcessNextShopCommand(int expectedRequestNumber)` | `IBufferQuery.StageCommandResults` |
| H4 일반 | `bool StageCommands.ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed)` | 같은 결과 Read Sink |
| H4 전투 | `bool StageCommands.TryProcessNextBattleCommand(int expectedRequestNumber, out ChapterProgressionResult result)` | 같은 결과 Read Sink + 기존 전투 실행 수명 |

Core가 바인딩된 도메인 입력 Read Sink에서 현재 요청을 읽는다. Job은 기대 번호와 좁은 Core 인터페이스만 가지며 payload·입력/결과 Sink·결과 대조를 제거한다(일반 맵의 임시 seed 공급자 예외). 없음·기대 번호 불일치·이미 처리됨은 상태/번호 변경 없이 false이며 Job은 정상 완료한다. true는 수락뿐 아니라 정상 거절의 결과 게시도 포함한다. 입력 삭제는 View 소유다.

H1/H2는 계산 전·게시 전 번호 확인으로 취소/교체된 제안을 게시하지 않는다. H3/H4의 seed 공급자는 유효 입력 확인·재진입 잠금 뒤 한 번 호출하고 호출 뒤 현재 번호를 재확인한다. **기존 동기 Shop/Combat 서비스가 실행에 들어간 뒤에는 정상 결과를 반드시 게시한다.** DB 콜백의 입력 Clear도 이미 설치한 상태의 결과를 버리는 이유가 아니며 거래 롤백/commit 이후 취소로 설명하지 않는다. DB 예외는 기존 거래 원자성·이전 결과 보존을 검증한다.

최초 P56의 (a) `EnterNode(N, chapter, node, seed)`·`EnterBattle(N, chapter, node)`·`Purchase(N, chapter, node, offer)`·`CompleteShop(N, chapter, node)`, 최초 P7의 `ProcessCommand(N, data)`·`ProcessPreview(N, data)`는 번호·내용 전달 방식의 과거 계약이다. 현재 직접 조회와 병행하는 API 지침이 아니다. 후속 검증은 SA 계획 P6.6~P6.10과 P7.5~P7.9로 구분한다.

#### 3.2 공통 조건

- 한 번에 하나씩 처리합니다. 번호는 줄어들지 않습니다.
- **정상 거절도 처리한 것**으로 기록합니다(번호 + 거절 사유).
- 예외로 끝나면 기록하지 않습니다(공용 오류 경계가 정지).
- Core는 View 입력 Buffer를 고치지 않습니다(정리는 View가 처리 번호를 보고 함).
- 새 Run·Buffer 초기화 때 처리 번호의 초기화 시점과 입력 번호의 대응 수명을 명시해 주세요. 오래된 번호/결과가 새 요청을 가리지 않아야 합니다.
- 번호가 유효한데 Core가 계산을 건너뛰는 H2는 번호+skip 결과가 필요합니다. 입력 슬롯이 취소/교체돼 Core가 false를 반환한 경우에는 기록을 위조하지 않고 입력 수명으로 기다림을 끝냅니다. 아티팩트 편집도 Clear/Detach로 기대 번호의 입력이 사라졌다면 Flow 기다림을 끝내 새 연결의 요청을 등록합니다. 같은 기대 입력이 남고 실제 Core 기록이 누락된 경우에는 해당 Flow 정지를 유지합니다. 비전투 맵·상점 Flow도 처리 번호 또는 기대 입력 소멸로 기다림을 끝냅니다. 전투 Cancelled는 기존 Job 배출과 View 입력 제거까지 같은 요청을 보존합니다.

#### 3.3 H4의 분기별 계약과 담당 경계

| 경우 | 요청/판정 경로 | 처리 완료·정리 기준 |
|---|---|---|
| 상점 진입 수락/정상 거절 | 기존 EnterShop 경로에 맵 번호를 전달 | Core H4 번호+결과. View가 결과 표시·입력 제거 |
| 전투 진입 수락/정상 거절 | 번호 있는 RequestEntry가 입력 N·chapter/node 대응을 등록 전 검사. Prepare가 `TryProcessNextBattleCommand(N, out result)`로 Core 직접 조회·기존 Combat.EnterBattle 실행 후 게시 | Core H4 번호+결과. 수락 뒤 기존 활성화 절차를 계속하며 View의 맵 잠금은 Active 또는 해당 전환 Detach까지 유지 |
| 미지원 이벤트/회복·없는 노드·진입 불가 | StageNodeEntryJob이 Core 판정 경로에 전달 | 정상 거절 번호+사유. View/Flow가 안내만 하고 처리 번호 없이 끝내지 않음 |
| 전투 실행 전 취소 | Created/Queued 취소 또는 Core 미실행 false의 Prepare 전용 내부 Running→Cancelled. 일반 Running 취소는 거부 | H4 없음. View가 동일 요청의 Cancelled를 관찰해 입력 폐기, Flow는 Job 배출과 입력 제거까지 대기 |
| 로딩/Runner 바쁨·맵 준비 전 | Flow가 등록하지 않고 기다림 | 입력 유지. busy 예외를 Core 정상 거절로 위조하지 않음 |
| 정상 씬 이탈 때 미등록 입력 | 소유 수명 정리 | 입력 폐기·새 입력 차단. 등록된 Job은 취소/배출 보장 후 정리, H4 위조 없음 |
| 예외·공용 fault | 기존 공용 오류 경계 | 같은 런타임의 Tick·새 등록·재실행 금지를 보장하고 View 즉시 정리. Idle/실패 Job 배출을 기다리지 않으며 H4 위조 없음 |

- 맵 입력은 필수 `StageViewCommandData.ChapterKey`와 NodeId를 가집니다. 실제 표시한 맵 챕터를 사용하고 표시 전 제출하지 않으며 StageLifeCycle도 입력 챕터로 전투를 분기합니다. 다른 챕터의 결과는 자기 입력만 정리하고 현재 화면/문구를 변경하지 않습니다. 미정리 맵 입력은 하나만 유지합니다. 취소 번호가 건너뛰어도 정상이며, View가 입력 Buffer를 정리합니다.
- 전투의 번호 없는 Bootstrap 진입은 그대로 둡니다. 가짜 맵 번호를 발급하거나 일반 전투 행동 처리 번호와 합치지 않습니다.
- 기존 전투/상점의 진입 함수 앞에 공통 `ConfirmStageEntry`를 호출해 노드 선택을 변경하거나 진입을 두 번 확정하지 않습니다.
- 전투 작업자에게 함께 요청: 맵 N 전달 방식, 준비 수락과 Active 구분, 취소 가능 상태·Job 배출·참조 보존. `TryCancel`/취소 요청의 실패를 Cancelled로 간주하지 않습니다.
- 취소 관찰은 새 Core 결과 Buffer가 아닙니다. SA는 StageLifeCycle이 `IStageEntryAttemptRead : IServiceModule`의 `TryGetCombatEntry(out int mapCommandNumber, out CombatEntryRequest request)`로 Flow가 가진 **같은 기존 요청 참조**를 제공하도록 계획했습니다. View가 N을 제거하고 취소 Job도 배출될 때까지 보존합니다. 2026-10-11 사용자 승인으로 로컬 전투 번호 전달과 같은 요청 참조 보존을 구현했으며, Core 번호 계약과 전체 소비자 회귀를 완료했습니다(기존 기준선 실패 유지, 상세 SA 계획 P5·P6). 외부 담당자 인수 완료를 뜻하지 않습니다.
- 상점 seed는 [Run seed 요청 R4](#3부-run-seed-도입-요청-view--core)라는 별도 계약입니다. H4 (a)/(b) 중 무엇을 선택하든 R4 인수 전에는 기존 임시 seed 주입, 인수 후에는 Core 유도를 사용합니다. View가 Run seed를 직접 섞지 않습니다.

### 4. View 쪽 준비와 이후 전환

- 입력 Buffer: `ArtifactViewBuffer` 확장(프리뷰 슬롯), `ShopViewBuffer` 새로 만들기, `StageViewBuffer` 유지.
- 실행 계층: 도메인별 Flow(`StageNodeEntryFlow`·`ShopFlow`·`ArtifactEditFlow`)와 조정자 `StageFlow`, 도메인별 루트 Plan·Job. Job은 Core 함수를 한 번 호출하며 결과를 대신 쓰지 않습니다. 맵·상점·아티팩트 Job은 정상 bool 반환 뒤 완료하고 결과 Sink를 직접 검사하지 않으며, Flow가 기대 번호와 입력 수명을 확인합니다.
- P4 당시에는 기존 소비자를 유지했다. 사용자 승인으로 P5/P6 맵·상점, P7 아티팩트 경로를 순서대로 전환했다. H1/H2 직접 조회의 완료와 현재 H3/H4 직접 조회의 자동 테스트 완료·최종 빌드/독립 검토 완료 상태를 구분하며, 상세 검증은 SA 계획 P6.6~P6.10·P7 기록을 따른다.

### 5. 응답 양식

| 번호 | 수락/보류/거절 | 형태((a)/(b))·예정 시점·메모 |
|---|---|---|
| H1 | 사용자 추가 승인으로 직접 조회 보완·자동 검증 완료, 외부 응답 없음 | (b), ArtifactCommands.ProcessNextCommand(expectedRequestNumber) + ArtifactBuffer 번호/의도/ArtifactActionResult |
| H2 | 사용자 추가 승인으로 직접 조회 보완·자동 검증 완료, 외부 응답 없음 | (b), ArtifactCommands.ProcessLatestPreview(expectedRequestNumber) + 번호/의도/Preview 또는 IsSkipped |
| H3 | 직접 조회 후속 로컬 구현·자동 검증 완료, 외부 응답 없음 | (b), StageCommands.ProcessNextShopCommand(expectedRequestNumber) + CurrentProgressBuffer 결과. 최초 P56 검증은 이력 |
| H4 | 직접 조회 후속 로컬 구현·자동 검증 완료, 외부 응답 없음 | (b), ProcessNextNodeCommand(N, nextSeed) / TryProcessNextBattleCommand(N, out result) + CurrentProgressBuffer 결과. 필수 챕터·전투 대상 검증/수명 포함 |

#### 5.1 인수 때 채울 증거

문서 전달이나 가짜 Sink 테스트만으로 인수 완료가 되지 않습니다. 도메인별로 다음을 응답에 함께 기록해 주세요.

- 수락한 범위, 실제 API/결과 읽기 접근자, 반영 코드 위치·버전.
- 성공/정상 거절/예외, H2 skip·취소/교체, H4 전투·상점·미지원/없는 노드의 실제 게시 검증. H1/H2는 계산 중 취소/교체의 게시 차단과 같은 Stage 재연결 후 이전 Job의 새 입력 실행 차단, 기대 입력 소멸 뒤 Flow의 새 요청 등록, 유효 입력의 실제 기록 누락 정지도 확인.
- 입력 번호와 처리 번호의 새 Run·씬 왕복·Buffer 재생성 수명 및 오래된 응답 배제 검증.
- H3/H4의 필수 챕터·입력 Clear/교체·seed 취소/재진입·동기 commit 이후 결과 게시·DB 예외 원자성, Flow의 소멸 입력 대기 해제/새 요청 등록. H4 번호 있는 요청의 대상검증·준비/활성화·미실행 내부 취소/일반 Running 취소 거부·Job 배출 후 재진입과 별도의 전투 작업자 인수 기록.
- 컴파일/관련 테스트 결과. Core 게시 기록이 빠진 정상 반환을 View/Job이 대신 기록하는 우회는 금지.

H3/H4의 실제 파일·API·번호 수명·중복/예외·교체 절차와 RED 증거는 [P5/P6 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md)에 기록했다. 입력/결과 번호는 Clear·새 Run·씬 왕복에 유지하고 전체 Buffer 재생성 때 함께 초기화한다. 성공 뒤 추가 실패가 없는 필드 설치로 기존 동기 거래를 보존한다. **최초 P56 이력**은 전체 3702건 중 3667 통과·기존 실패 34·기존 판정 불가 1, 추가 실패 0, 순차 MSBuild exit 0이다. 직접 조회 후속 자동 테스트는 `Logs/StageSelectSAVerification/stage-shop-pull-related.xml` 365/364 통과/실패 0/기존 판정 불가 1, `stage-shop-pull-all.xml` 3769/3734 통과/기존 실패 34/기존 판정 불가 1/skip 0이다. 전체 실패 fullname은 `p7-pull-final-all.xml`과 같아 추가/해소 0이며 Unity exit 2를 전체 통과로 해석하지 않는다. 기준선·RED는 P56 인계 §7에 기록했고 당시 최종 빌드·독립 검토도 완료했다. 외부 응답이나 전달·합의 기록은 아직 없다. **R3 `c773625a` 미병합 안내는 당시 이력이다.** 현재 사용자 병합에는 독립 전투 진입·진행 구조가 들어 있으며, 번호 전달과 전환 수명 보완도 적용했다. 현행 교체 절차와 병합 후 검증은 P56 인계 §5·§8 및 위 병합 검토 §7을 따른다.

---

## 5부. 상점 팝업 정리에서 옮긴 천명님 줄 (View → Core 알림, 2026-10-27, 미전달)

- 작성: 호진 / 대상: 천명님. **요청이 아니라 알림이다.** Core 코드·계약은 바꾸지 않았다.
- 무엇: 상점·회복 UI를 공통 기반 하나로 모으고 제출을 Controller 한 곳으로 옮기면서(검사 후속 정리 4번, 커밋 `354bd8d7d`), 천명님이 혼합 상점(`a5133b32`, 10-19)에서 `ShopUI`에 쓴 줄을 옮겼다. 원본: [검사 후속 정리 설계 §7](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md#7-4번-제출-중-하나와-팝업-공통-기반), 기록 [계획 §3.9·§3.10](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#39-s-a-공통-기반-4번).

### 1. 옮긴 줄

| `a5133b32`의 `ShopUI` 줄 | 지금 |
|---|---|
| 방문 문맥 `_context`(`ShopVisitContext`) 보관 | `ShopUI._context` 그대로(`Render` 때 모델에서 복사) |
| 제출 허용의 `!_context.EntryId.IsEmpty` | `ShopUI.HasRendered`. 기반 `StageOfferPopupUI`가 입력 허용에 쓴다 |
| 나가기 버튼 문구(`_exitText`, 모델 `ExitText`) | 기반 `RenderCards(…, exitLabel, …)`. **빈 값이면 프리팹 문구로 되돌림**(전에는 직전 문구가 남았다 — 유일한 동작 차이) |
| `ShopViewCommandData.Purchase(_context, offerId)`·`Complete(_context)` 만들기와 Buffer 제출 | `ShopUI`는 수신자 `IShopPopupInput.Purchase(context, offerId)`·`Complete(context)`만 부르고, 명령 만들기·제출은 `ShopPopupController`의 수신자 메서드가 한다 |

### 2. 그대로인 것

- `ShopPopupController`에 있는 천명님 줄(47줄: 방문 문맥 대조, 방문당 구매 등)은 바꾸지 않았다.
- `ShopVisitContext`·`ShopViewCommandData`·`CurrentShopData`(`VisitKind` 포함)·Core 상점 처리는 그대로다.

### 3. 같이 알릴 결정

- 노드 종류(`StageType` 전투·상점·이벤트)는 셋 그대로 둔다(10-28 사용자). 처음 검토했던 "둘로 합치기"는 요청하지 않는다. 상점과 강화의 구분은 지금처럼 스테이지 상세 종류(`ShopStageDetail`·`EnhancementStageDetail`)가 원본이고, View는 Buffer `CurrentShopData.VisitKind`로 고른다.

### 4. 덧붙임 — R-a에서 바뀐 천명님 줄 (2026-10-28, 미전달)

- `ViewSystem/StageMap/Shop/ShopPopupViewFactory.cs`의 아이템 외형 표(`itemVisuals`, `a5133b32`) 세 줄: 선택 인자(`= null`)·null 허용 대입·`_itemVisuals != null` 갈래 → 필수 인자·null이면 생성 때 예외·갈래 삭제. 운영 조립은 늘 넘기고 있어 화면 차이는 없습니다. 스테이지 맵 서비스가 모두 필수가 되면서(검사 후속 정리 R-a) 닿지 않게 된 갈래를 걷은 것입니다.

---

## 6부. 검사 후속 정리 요청 묶음 — seed 다시·참조 검사·아티팩트 Buffer (2026-10-29, 미전달)

- 작성: 호진 / 대상: 천명님(2번은 용근·민형님과 함께 정할 일). 상태: **미전달·미합의**.
- 출처: [검사 후속 정리 계획 §6.9](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#69-phase-r-f--묶음-8-작은-정리-묶음-7-요청) R-f(묶음 7·8), 지적 원본은 [설계 §1.2](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md#12-10-27-점수의-지적-목록) F2·DM4·F7·F6.

### 1. 상점 seed — 3부 다시 요청 (F2)

- 3부 R1~R5는 그대로 유효하고 회신이 없습니다.
- 지금 seed 자리: `JobRegisterSystem`의 `_nextShopSeed`(`new Random().Next`) → `StagePlan.Next` → `StageNodeEntryJob` → Core `ProcessNextNodeCommand(기대 번호, nextSeed)`. 이름은 상점용인데 노드 진입 전체에 쓰입니다. 그래서 상점 결과를 다시 만들 수 없습니다.
- R4(운영 경로는 Core가 Run seed에서 직접 유도)가 오면 View는 seed 함수와 Job 인자를 지웁니다.

### 2. DB 생성 때 참조 검사 (DM4)

- 지금 InGame DB 생성(`InGameDataBaseFactory`)은 SO 사이 참조가 **등록된 표 안**을 가리키는지 보지 않습니다.
- 실제 사례: `InGameGroup/Stage/Event/Event_Recovery.asset`의 회복 선택지 셋 중 둘이 미등록 사본 표(`InGameGroup/Stage/Event/Recovery/`, 값이 다름)를 가리켰습니다. 10-29 R-e에서 등록된 표(`InGameGroup/Recovery/`)로 바꿨습니다. 이벤트는 선택지 번호만 읽어 게임 값은 그대로입니다. 사본 표는 지우지 않았고 참조하는 곳이 없습니다.
- 요청: DB 생성 때 다음을 검사해 잘못되면 부트에서 멈추게.
  - 이벤트 회복 선택지가 등록된 회복 표에 있는가.
  - 스테이지가 가리키는 맵 키·상점 키가 등록된 표에 있는가.
  - 맵마다 보스가 하나인가(지금은 Run 시작 때에야 예외).
- `InGameDataBaseFactory` 줄은 용근·민형님 영역이라 셋이 담당을 정해 주세요. SO 모드 도구(호진)는 등록할 때만 일부(보스 하나 등)를 보며 DB 생성 검사를 대신하지 않습니다.

### 3. 아티팩트 Buffer의 소비자 없는 읽기 둘 (F7)

- `ArtifactBuffer`의 `IArtifactEditResultReadSink.LastEditResult`·`IArtifactPreviewReadSink.LastPreview`(`3a6b4cd3`)는 Buffer 그룹·키(`ArtifactBufferKey.EditResults`·`Previews`)로 나가지만 읽는 곳이 없습니다.
- 같은 사실을 번호 있는 결과(`IArtifactCommandResultReadSink`의 `LastCommand`·`LastAnsweredPreview`)가 이미 가집니다.
- 제안: 둘을 걷기. 천명님 줄이라 View는 고치지 않았습니다.

### 4. 알림 — 회복 읽기 계약 주석 둘을 고침 (F6, 요청 아님)

- `BufferSystems/Recovery/IRecoveryResultReadSink.cs`·`IRecoveryStateReadSink.cs`의 "구현과 서비스 등록은 RC2/RC3에서 제공합니다"를 실제 구현으로 바꿨습니다(주석만, 10-29 사용자 지시).
  - 결과 읽기: 구현 `RecoveryResultBuffer`, Buffer 그룹이 냄.
  - 상태 읽기: 구현 Core `RecoveryStateQueryService`, `InGameCoreSystemGroup.RecoveryState`로 읽음.
- 상태 읽기 계약은 Buffer 네임스페이스에 있지만 Core 조회 계약입니다. Core 쪽으로 옮길지는 천명님이 정해 주세요(제안).

### 5. 알림 — 강화 노드를 맵 1에서 잠시 뺌 (요청 아님)

- 10-29 사용자 결정. 강화 노드(1202)는 Core가 `EnhancementShopUnavailable`로 거절하는데(`d48542034`), 맵 1에는 늘 하나 이상 나왔습니다. 플레이어가 상점과 똑같이 생긴 노드를 눌러도 아무 일이 없었습니다.
- 바꾼 것(에셋만):
  - `InGameGroup/Stage/StageRootDataTable.asset`에서 `Stage_Enhancement`(1202) 등록을 뺐습니다.
  - `InGameGroup/StageMap/Item/StageMap_Chapter1.asset`의 강화 분포를 가중치 4·최소 1(천명님 `ccaf47c8`) → 0·0으로 바꿨습니다. 분포가 남은 채 후보가 없으면 `ChapterStagePool`이 Run 시작 때 예외를 냅니다.
- 메인 강화 상점이 생기고 진입 거절을 풀 때 둘을 되돌립니다. 원본은 [강화 View 설계 §14.5](../specs/2026-10-19-enhancement-view-integration-design-Hojin.md#145-보류와-범위-밖)입니다.

### 6. 응답 양식

| 번호 | 수락/보류/거절 | 담당·예정 시점·메모 |
|---|---|---|
| 1 (3부 R1~R5) | | |
| 2 | | |
| 3 | | |
| 4 (옮길지) | | |
