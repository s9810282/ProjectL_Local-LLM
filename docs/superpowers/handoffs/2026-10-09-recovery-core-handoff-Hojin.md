# 회복 Core 요청·인계

- 작성자: 호진(이호진)

회복 결과 반환 요청, 회복 이벤트 Core 계약 요청, RC0~RC2 View 계약 인수, V4 입력 Buffer 인계, Event 진입 요청, ER1 Stage/Run origin 인계를 모았다. 원래 문서를 부(部)로 나눠 내용 그대로 담았고, 각 부의 결정 번호와 절 번호는 원래 문서와 같다.

| 부 | 원래 문서 |
|---|---|
| [1부](#1부-회복-결과-반환-요청-view--core) | 회복 결과 반환 요청 (View → Core) |
| [2부](#2부-회복-이벤트-ui-연결을-위한-core-계약-요청) | 회복 이벤트 UI 연결을 위한 Core 계약 요청 |
| [3부](#3부-회복-rc0rc1rc2-view-계약-인수-기록) | 회복 RC0·RC1·RC2 View 계약 인수 기록 |
| [4부](#4부-v4-회복-입력-buffer--core-인계) | V4 회복 입력 Buffer — Core 인계 |
| [5부](#5부-event-노드-회복-진입-core-연결-요청) | Event 노드 회복 진입 Core 연결 요청 |
| [6부](#6부-er1-stagerun-origin-선행-작업-인계) | ER1 Stage/Run origin 선행 작업 인계 |

---

## 1부. 회복 결과 반환 요청 (View → Core)

- 작성일: 2026-10-09
- 작성: View 작업(스테이지 선택 담당, 호진)
- 받는 사람: Core 작업자(천명님)
- 상태: **미전달·미합의·미구현**
- **2026-10-13 대체됨:** 이 요청의 C1~C4는 이후 [회복 Core 계약 요청](#2부-회복-이벤트-ui-연결을-위한-core-계약-요청)의 RC0~RC2로 대체됐다. RC2의 `RecoveryOperationResult`가 Accepted/TradeRejected/StateMismatch로 정상 거절을 돌려준다. 아래의 R-A "임시 방어"는 구현하지 않기로 바뀌었다([비전투 이벤트 UI 설계](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md)). C5의 확인 질문은 회신 기록이 없다. 이 문서는 이력으로만 남긴다.
- 관련: [상점·회복 UI 설계](../specs/2026-10-06-stage-shop-recovery-ui-design-Hojin.md) §2.1·§7 Q2, [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §13.15
- 일정 영향: **View 구현을 막지 않는다**(2026-10-09 사용자 결정 R-A). View는 상점 다음에 현재 Core 기준으로 회복을 구현하고, 예외가 나는 경우는 버튼을 꺼서 막는 임시 방어를 둔다(§3). 이 요청은 그 임시 방어를 없애기 위한 것이다.

### 1. 문제 (코드 확인, 2026-10-09)

`IRecoveryStageService.Recover(chapterKey, nodeId, optionId)`는 정상적인 게임 상황에서도 예외를 던진다.

| 상황 | 현재 동작 | 위치 |
|---|---|---|
| 골드 부족 | `InvalidOperationException("회복 효과가 없거나 비용이 부족합니다.")` | `CharacterRecoveryCalculator` 21~22줄 |
| 회복 효과 없음(체력 가득) | 같은 예외 | 같은 곳 |
| 대상 노드 불일치·거절 | `InvalidOperationException("회복 적용이 거절되었습니다: …")` | `RecoveryStageService.Recover` |
| 같은 선택지 다시 적용 | `false` 반환 | 인터페이스 주석 |
| 다른 선택지가 이미 적용됨 | 예외(설계 §1 확인 내용) | — |

View는 Core 호출을 Job에서 하고, Job 예외는 공용 오류 경계(`RuntimeFaultBoundary.Report`)로 가서 **런타임 전체가 정지 상태로 고정된다.** 체력이 가득 찬 플레이어가 회복 버튼을 누르는 것만으로 게임이 멈출 수 있다.
회복 기능 자체(계산·결제·Buffer 기록·같은 선택지 재적용 `false`)는 이미 동작한다. 문제는 실패 경로뿐이다.

### 2. 요청 사항

| 번호 | 요청 | 비고 |
|---|---|---|
| C1 | 정상 실패는 예외 대신 **실패 사유를 가진 결과**로 반환 | 사유 예: 골드 부족, 회복 효과 없음, 이미 적용됨(같은/다른 선택지), 대상 노드 불일치, Run 종료. 상점 `CharacterPurchaseResult`/`ShopFailureReason`과 같은 방향. 타입 이름은 Core가 정함 |
| C2 | 실패 결과일 때 재화·체력·적용 기록이 **바뀌지 않음**을 보장 | View는 실패 뒤 Buffer를 다시 읽어 화면을 그린다 |
| C3 | 같은 요청을 반복해도 **중복 결제·중복 회복이 없음**을 보장 | 현재 같은 선택지 재적용 `false`와 같은 성격을 결과 사유로 통일 |
| C4 | 예외는 인자 오류·내부 불변식 위반처럼 **복구할 수 없는 경우에만** 남김 | 사망한 메인 등 진입 단계에서 걸러져야 하는 경우의 처리 위치는 Core가 정함 |
| C5 | (확인) `EnterRecovery`·`CompleteRecovery`의 거절이 지금처럼 `ChapterProgressionResult`로 오는지, 예외가 나는 경우가 있는지 | View 실패 처리 분류(설계 §2.1)에 맞추기 위해 |

### 3. View의 임시 방어 (Core 반영 전까지, 구현 예정)

| 실패 상황 | View 처리 |
|---|---|
| 골드 부족 | `Currency.Amount < Cost`면 선택 버튼 끔 |
| 체력 가득 | 현재 체력 ≥ (기본 최대 체력 + 메인 영혼 수)면 선택 버튼 끔. **Core 규칙의 중복**(최대 체력 식)이라 Core가 바꾸면 어긋날 수 있음 |
| 다른 선택지 적용됨 | 하나 적용 뒤 나머지 버튼 끔 |

- 한 파일(`RecoveryTemporaryGuard`)에만 두고 삭제 대상으로 표시한다.
- 남는 위험: 위 판정과 Core 판정이 어긋나면 여전히 예외로 정지한다.

### 4. 받은 뒤 View가 할 일

- 임시 방어 파일을 지운다.
- 회복 Job이 결과를 요청 객체에 기록하고, UI는 사유별 문구를 보여 준 뒤 팝업을 유지한다(설계 §2.1 "정상 거절").
- 예외는 지금처럼 공용 오류 정책으로 보낸다.

### 5. 응답 양식

| 번호 | 수락/보류/거절 | 예정 시점·메모 |
|---|---|---|
| C1 | | |
| C2 | | |
| C3 | | |
| C4 | | |
| C5 | | |

---

## 2부. 회복 이벤트 UI 연결을 위한 Core 계약 요청

- 작성일: 2026-10-11
- 상태: **2026-10-12 RC0·RC1·RC2 문서/소스 인수에 이어 V4 입력 Buffer 구현·검증을 완료했다. 사용자 V4 커밋·Core 병합/인수와 RC3·RC4는 남음.** 최초 대조는 [View 계약 인수 기록](#3부-회복-rc0rc1rc2-view-계약-인수-기록), 실제 V4 API·새 검증 증거·A1/A2 책임은 [V4 Core 인계서](#4부-v4-회복-입력-buffer--core-인계)를 따른다. 새 외부 메시지는 보내지 않았다.
- **2026-10-13 현재:** V4는 사용자 커밋 `3aad1e83` 뒤 Core 브랜치에 병합됐고(`58c2f12e`), RC3-A(번호 처리·결과 보존)는 천명님 `535554e7`로 완료돼 이 브랜치에 들어왔다(`56c38c3a`, Core 계획 §14 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#14-phase-3-execution-record-2026-10-13`)). RC3-B(Stage Event 진입)는 Stage/Run 인계 3건을 기다리며 멈춰 있고(Core 계획 §15 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#15-phase-4-prerequisite-inspection-2026-10-13`)), RC4는 미착수다(→ 바뀜: RC3-B·RC4는 천명님 `92440b83`(2026-10-14)으로 완료·인수 — [이벤트·회복 계획 §3.7 ER2](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#37-er2-core-진입-연결-천명님-2026-10-14)). 아래 §10 표가 상태 원본이다.
- 요청 배경: 사용자가 회복과 후속 비전투 이벤트의 UI 구조에 따라 Core 요청과 선행 View 작업을 분리한 Task 계획 작성을 요청했다.
- 설계: [비전투 이벤트 UI 구조](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md)
- 실행 순서와 View 담당 범위: [Task 계획](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md)
- 기존 요청: [회복 결과 C1~C5](#1부-회복-결과-반환-요청-view--core), [기획 차이 G2·G3](2026-10-06-stageselect-core-requests-Hojin.md#2부-통합-기획서와-상점회복스테이지-규칙-차이-view--core기획-확인-요청)

회복은 StageSelect 안의 UI로 표시한다. View는 입력을 Buffer에 제출하고 결과를 읽어 표시하며, 회복·결제·선택 가능 판단·완료는 Core가 소유한다. 이번 요청은 **회복의 현재 서비스와 번호 있는 실행 경로를 연결하는 것**이다. 성장·교환·랜덤 이벤트 전체의 범용 엔진 구현은 요청하지 않는다.

### 1 최초 요청의 코드 기준과 현재 인수

2026-10-12 `e3305027`에는 RC1 `5cc2c2b8`과 RC2 `fa618534`가 병합돼 있다. `RecoveryCommandData`·`RecoveryStateSnapshot`·`RecoveryProcessedResult` 등 타입, `CoreGroup.RecoveryState.TryGetCurrent`와 문맥 기반 typed Select/Complete 서비스가 제공된다. 실제 선언·경로는 [인수 기록 §2~3](#2-rc1-실제-공개-계약-대조), 현재 V4 계약은 [이벤트·회복 계획 §3.5](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#35-v4-회복-입력-buffer-2026-10-12)(옛 Task 계획 §8)을 따른다. 이 HEAD 위 미커밋 변경으로 V4 입력·공용 등록을 구현·검증했다. Core processor와 결과 읽기 인터페이스는 선언만 있으며 RC3 결과 게시·Stage Event 진입은 미구현이다. (→ 바뀜: RC3-A는 천명님 `535554e7`, RC3-B·RC4는 `92440b83`(2026-10-14)으로 완료 — 아래 §10 표)

아래는 **2026-10-11 최초 요청 당시 조사 이력**이다. 특히 당시 bool/예외·상태 계약 부재 설명을 RC2 이후의 현재 상태로 사용하지 않는다.

현재 `IRecoveryStageService`는 `EnterRecovery → ChapterProgressionResult`, `Recover → bool`, `CompleteRecovery → ChapterProgressionResult`를 제공한다. `CurrentRecoveryData`에 선택지·적용 ID·지불·회복량이 기록된다. 같은 선택지의 중복 결제 방지와 실패 시 상태 불변은 기존 보장으로 유지해야 한다.

다음은 아직 운영 연결이 없다.

- `StageCommandProcessingService`의 일반 노드 처리는 Shop만 허용하며 Event는 `UnsupportedStageType`이다.
- 회복 선택·완료 입력 Buffer, 기대 번호로 입력을 읽는 Core 처리 메서드, 회복 처리 번호·요청별 결과 게시가 없다.
- `RecoveryCommandResult`와 `RecoveryOptionSnapshot` 타입은 있지만 운영 회복 서비스·Buffer에서는 사용하지 않는다.
- `RecoveryStageSnapshot`의 RunId·EntryId·RunRevision도 현재 회복 Buffer의 운영 공개값이라고 가정할 수 없다.

따라서 타입 이름이 있다는 이유로 UI 연결 완료나 C1 해결로 인수하지 않는다. 예외 메시지 문자열을 파싱하는 View 실패 처리는 만들지 않는다.

### 2 담당과 의존 순서

```text
RC0 공동 계약 확정
  → RC1 Core가 문맥·상태·결과 타입 제공
       ├─ V4 View가 RecoveryViewBuffer와 읽기/쓰기 계약 구현
       └─ RC2 Core가 회복 서비스와 표시 상태 게시 보완
  → V4 + RC2 인수 후 RC3 Core가 번호 있는 처리 연결
  → RC4 Core 경계 인수
  → V5~V8 View 실행·화면·씬 연결 및 통합 검증
```

V1 공용 카드, V2 팝업 입력 수명, V3 표시 데이터·프리팹은 RC0~RC4를 기다리지 않는다. 회복 운영 연결은 RC4를 기다린다. Core가 View 입력 Buffer를 기다리면서 View도 아직 없는 Core 타입을 기다리는 순환을 만들지 않기 위해 **계약 타입을 RC1에서 먼저 제공**한다.

### 3 RC0 공동 계약 확정

§3~7은 최초 요청 항목을 보존한다. 당시 제안 서명과 미완료 체크를 현재 제공 상태로 읽지 않는다. 제공된 RC0 정책·RC1 매니페스트·RC2 소스의 대조 결과는 §10과 별도 인수 기록을 우선한다.

**담당:** Core가 규칙·진행 문맥·실패 분류를 확정하고 View가 입력·표시 요구를 대조한다. 아래 이름은 합의를 위한 제안이며 기존 API가 아니다.

| 항목 | 요청 내용 | 합의 완료 증거 |
|---|---|---|
| 진행 문맥 | Core가 발급·검증하는 회복 진입 식별. 같은 chapter/node의 새 Run·새 진입을 구분 | 실제 타입·접근자·발급 및 보존 수명 |
| 입력 | 선택/완료 종류, chapter/node, Core 문맥, 선택 시 `RecoveryOptionId`. 비용·회복량·가능 여부는 입력하지 않음 | 명령 생성 서명·검증 책임 |
| 표시 상태 | 선택 ID·순서·비용·회복 방식/원래 값·실제 예상 회복량·선택 가능 여부·불가 사유·적용 기록 | 읽기 접근자·불변성·갱신 시점 |
| 거래 결과 | 성공·거래 거절·상태 불일치 구분, 지불·회복·선택 사실은 Core 상태가 원본 | 반환 타입·사유별 처리표 |
| 요청 결과 | 요청 번호·종류·chapter/node·선택 ID·Core 문맥을 대조할 수 있음 | 결과 DTO·처리 번호·읽기/설치 API |
| 번호 수명 | 입력 발급 번호와 결과 처리 번호를 Clear·새 Run에서 재사용하지 않음 | 초기화·중복·오버플로 정책 |
| 결과 보존 | 최신 1건이면 미확인 요청 1건 원칙과 다음 처리 허용 시점을 함께 보장 | 소비 전 덮어쓰기 방지 계약 |
| 취소 | 실행 전 입력 소실은 무거래 정상 종료. 거래 적용 뒤 Clear/Hide는 결과 게시를 취소하지 않음 | 처리 반환값·게시 순서 |
| 선택 가능 갱신 | 진입·회복 처리 후 갱신. 활성 회복 중 재화/체력 변경 가능 경로도 반영하거나 그 변경이 금지됨을 명시 | 외부 변경 정책과 재연결 읽기 보장 |
| 노드 완료 | 미선택 완료 허용. 회복 적용과 완료를 별도 명령으로 유지 | Enter/Complete 결과·중복 완료 정책 |

View가 `CanSelect`를 받아도 Core는 실행 시 규칙을 재검증한다. `CanSelect=false`는 View의 입력 허용 표시일 뿐 거래 보안이나 원자성의 대체 수단이 아니다.

**진행 문맥 주의:** 기존 Snapshot을 재사용하기 위해 운영 전체의 RunId/Revision 구조를 확장할지는 Core가 결정한다. View는 임의 Run 번호를 발급하거나, 기존 타입에 넣을 가짜 ID를 만들지 않는다. 최소한의 Core 소유 문맥으로도 요구를 만족할 수 있다면 그 계약을 제시해 달라.

- [ ] 실제 타입 이름·서명·소유 파일·실패 분류표를 회신한다.
- [ ] View가 회신을 Task 계획 V4~V6의 서명표에 반영한다. 이 대조 전에는 해당 구현을 시작하지 않는다.
- [ ] 공용 Buffer 그룹·Factory·결과 Store 파일의 편집 소유와 반영 순서를 정한다.

### 4 RC1 계약 타입 제공

**선행:** RC0. **담당:** Core.

기존 순수 C# 타입의 재사용 또는 보완을 우선 검토한다.

| 기존 타입 | 확인할 점 |
|---|---|
| `RecoveryOptionSnapshot` | Cost·ProjectedHealAmount·CanSelect·UnavailableReason 제공. 원래 회복 방식/값 표시 경로도 필요 |
| `RecoveryCommandResult` | 도메인 결과 후보. 요청 번호·명령 문맥을 담는 처리 기록과 구분 |
| `RecoveryStageSnapshot` | 현재 운영에 없는 식별자를 이미 발급되는 값으로 간주하지 않음 |
| `CurrentRecoveryData` | 현재 운영 사실 원본. 별도 가변 사본을 만들어 동기화하지 않음 |
| `IStageCommandResultReadSink` | 현재 Map/Shop 결과만 제공. 회복 읽기 계약 확장 위치 후보 |

- [ ] 타입만으로 컴파일·생성자 검증 테스트가 가능한 산출물을 제공한다. View UI나 미구현 입력 Buffer를 참조하지 않는다.
- [ ] UnityEngine 의존을 넣지 않고, 선택 목록과 식별값의 불변성·유효 범위를 검증한다.
- [ ] RC0 회신과 다른 서명이 필요하면 문서를 먼저 갱신한다. 타입 제공은 RC2/RC3 운영 연결 완료와 구분한다.

**View 인수:** 계약 타입 컴파일과 테스트 증거를 확인하면 V4를 시작할 수 있다. 아직 운영 화면은 연결하지 않는다.

### 5 RC2 회복 서비스와 표시 상태 게시

**선행:** RC1. **담당:** Core. **V4와 병행 가능.**

- [ ] 골드 부족·효과 없음·이미 선택함은 예외 대신 합의된 결과로 반환한다. Run 종료·다른 진입·상태 소실은 UI를 복구해야 하는 상태 불일치로 구분한다.
- [ ] 같은/다른 선택지 반복에서 기존 중복 결제 방지와 실패 시 체력·재화·적용 기록 불변을 유지한다.
- [ ] 최대 체력, 고정·비율·전체 회복, 실제 회복 상한과 선택 가능 여부를 Core가 계산하고 합의된 읽기 경로에 게시한다.
- [ ] 진입·선택 결과와 RC0에서 정한 재화/체력 변경 시점에 표시 상태를 갱신한다. UI 재연결은 읽기만으로 복구 가능해야 한다.
- [ ] 미선택 완료, 적용 후 완료, 거절·오류 분류를 검증한다. DB 이상·바인딩 누락·불변식 오류는 정상 거래 거절과 섞지 않는다.

**검증 출발점 파일:** `RecoveryStageServiceTests.cs`, `RecoveryStageOperationsTests.cs`, `RecoveryEntryPublicationTests.cs`, `CharacterRecoveryRuleTests.cs`. 앞의 두 파일은 같은 `RecoveryStageServiceTests` fixture다. Core가 변경한 보장에 해당하는 회귀를 추가한다. 실패 시 상태 불변은 기존 C2, 중복 방지는 기존 C3을 새 API에서 유지하는 검증이다.

### 6 RC3 번호 있는 회복 명령 처리

**선행:** RC2와 V4. **담당:** Core.

처리 서비스는 `bool ProcessNextRecoveryCommand(int expectedNumber)` 형태를 제안한다. 이는 상점과 같은 “기대 입력을 직접 조회하여 처리했는가”라는 의미이며, 거래 성공 여부와 구분한다. 실제 이름·반환 의미는 RC0에서 확정한다.

- [ ] 기존 Stage 노드 명령에서 회복 진입을 연결한다. 합법적 Event 노드와 현재 진행 문맥을 확인하고 `EnterRecovery`의 상태와 진입 결과를 게시한다.
- [ ] `IRecoveryViewBufferReadSink`로 기대 번호의 입력을 다시 읽고 선택/완료를 처리한다. Job이 가져온 비용·회복량·판정을 사용하지 않는다.
- [ ] 처리 번호와 요청 결과를 Core가 게시한다. 결과 Store는 Core만 쓰고 View는 읽는다.
- [ ] 실행 전 Clear/번호 교체는 상태·결과를 만들지 않는 정상 종료다. 이미 동기 거래가 적용된 뒤 Clear되어도 결과 게시를 생략하지 않는다.
- [ ] 같은 요청의 재실행·재진입·처리 번호 누락·결과 덮어쓰기·새 Run의 같은 node ID를 검증한다.

**검증 출발점 파일:** `StageCommandProcessingTests.cs`, `RecoveryStageCycleTests.cs`, `RecoveryRuntimeCompositionTests.cs` 및 신규 번호 처리 테스트. 실제 fixture명은 각각 `ShopStageServiceTests`, `CoreStageCombatCycleTests`, `FrameworkRuntimeCompositionTests`다. Core는 입력을 읽는 쪽이고, 입력 소비·제거는 View 요청 수명에서 수행한다.

### 7 RC4 Core 경계 인수

- [ ] RC0의 실제 API 목록과 제공 커밋/변경 파일, 테스트 XML·컴파일 로그를 전달한다.
- [ ] 실제 Buffer 조립으로 진입→선택→완료→노드 클리어를 확인한다. UI를 직접 호출하는 테스트만으로 대체하지 않는다.
- [ ] 거래 거절·상태 불일치·실행 전 취소·중복 선택·같은 chapter/node의 새 Run을 확인한다.
- [ ] 선택 가능 표시와 실행 시 재검증의 일관성, 상태·요청 결과 게시를 확인한다.
- [ ] View가 공개 읽기 계약으로 이 증거를 대조하고 V5~V6 운영 연결 게이트를 통과시킨다.

회신 상태는 **미전달 / 전달 / 계약 합의 / 타입 제공 / 운영 연결 / 인수 완료**로 구분한다. 문서나 타입만 추가된 상태를 인수 완료로 표시하지 않는다.

### 8 파일 소유와 공용 편집 순서

Core 후보 파일은 다음과 같다. 변경 필요 여부와 실제 파일 목록은 Core가 확정한다.

- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStageService.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/StageCommandSystem/StageCommandProcessingService.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CharacterRecoveryCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/RecoveryOptionSnapshot.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/RecoveryStageSnapshot.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Results/RecoveryCommandResult.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/CurrentRecoveryData.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/CurrentProgressBuffer.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/IStageCommandResultReadSink.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/StageServiceTransactions.cs`
- `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs`

View는 `BufferSystems/RecoveryView/`의 입력 Buffer와 읽기/쓰기 인터페이스를 제공한다. View가 Core의 결과 Store나 계산 파일을 대신 구현하지 않는다.

`BufferSystemGroup.cs`·`BufferSystemFactory.cs` 등 공용 조립은 **V4 입력 등록 → Core 출력/처리 연결 반영 → 조립 테스트** 순서로 직렬 반영한다. RC2의 계산·타입 작업은 병행할 수 있지만 같은 파일을 동시에 편집하지 않는다. 공용 등록 변경은 Framework 공유 대상으로 기록한다.

### 9 기획과 후속 이벤트 경계

2026-10-11 사용자가 기획대로 선택지 3개를 승인하여 `Event_Recovery.asset`에 기존 등록 테이블의 3번 옵션을 추가했다. 운영 키 목록은 저작 순서대로 1·2·3이며 비용·회복 규칙은 등록 테이블 값을 유지한다. 변경·검증 기록은 [이벤트·회복 계획 §3.4](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#34-d0-선택지-3개-연결-2026-10-11)(옛 실행 계획 §18)을 따른다. Position·Weight의 필요와 의미, 표시 이미지·설명은 여전히 확인 사항이다. UI는 Core가 게시하는 목록을 표시하고 규칙이나 빈 카드를 생성하지 않는다. 표시 이미지는 View 담당이며 없는 이미지를 Core 데이터에 넣지 않는다.

회복 외 비전투 이벤트를 연결할 때는 Core가 종류·입력·결과를 명시한다. 이번에는 미래의 모든 이벤트 종류 enum과 범용 실행 엔진을 먼저 구현하지 않는다. 현재 `StageType.Event`를 회복으로 처리하는 범위와 다음 종류가 들어올 때 필요한 계약 변경을 인계에 적는다.

### 10 회신 기록

| 단계 | 현재 상태 | 회신에 포함할 것 |
|---|---|---|
| RC0 | 공개 계약 대조·View 문서 반영 완료 | A1 ack 검증 책임은 View 설계에 구체화; A2 요청 수명 연결은 V5~V6 후속 설계 |
| RC1 | `5cc2c2b8` 병합·타입 소스 인수 | 매니페스트와 선언/meta/테스트 소스 일치; 과거 검증 기록과 새 실행 구분 |
| RC2 | `fa618534` 병합·서비스 소스 인수 | RC2-A/B·query/typed 연산·등록 확인; 과거 2,948건 검증 출처는 인수 기록 §5 |
| V4 | 입력·등록·검증 완료, 사용자 커밋 `3aad1e83`, Core 브랜치 병합 `58c2f12e`(2026-10-13 확인) | [Core 인계서](#4부-v4-회복-입력-buffer--core-인계)의 실제 API·Q4 증거·A1/A2 책임 |
| RC3-A | 완료: 천명님 `535554e7`(Core 계획 §14 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#14-phase-3-execution-record-2026-10-13`), 관련 회귀 895건 통과), 이 브랜치 병합 `56c38c3a` | 결과 Store·번호 처리·조립 회신 받음 |
| RC3-B | (→ 바뀜: 완료 — 천명님 `92440b83`, 2026-10-14. 아래는 10-13 당시) Core 정지: Stage/Run 인계 3건 대기(Core 계획 §15 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#15-phase-4-prerequisite-inspection-2026-10-13`)). 담당: 호진([이벤트·회복 계획 §3.6](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#36-er1-stagerun-origin-2026-10-14)(옛 View 계획 §3.2)) | 별도 Stage 진입 수명 검증 |
| RC4 | (→ 바뀜: 완료 — 천명님 `92440b83`, 2026-10-14. 아래는 10-13 당시) 미착수 | 실제 결과 보존·취소·새 Run·노드 완료의 통합 인수 증거 |

View 작업 순서는 [이벤트·회복 계획 §1](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#1-진행-요약)(옛 Task 계획 §3.1·8)을 따른다. 회신 원문인 RC1 인계의 과거 “미커밋/RC2 미구현” 표기를 수정해서 새로운 실행 증거로 만들지 않는다.

#### 10.1 V4 착수 점검 (2026-10-11)

> 아래는 RC1 수령 전 보류 이력이다. 2026-10-12 수령·소스 대조 후의 현재 상태는 위 §10과 [인수 기록](#3부-회복-rc0rc1rc2-view-계약-인수-기록)을 따른다.

사용자가 수령한 최신 자료 `2026-10-06-core-contract-changes-and-status-share.md`를 현재 코드와 대조했다. 자료의 회복 관련 변경은 `PublishRecovery`의 Core 내부 쓰기 Sink 이동이며, V4 선택·완료 입력 문맥의 합의나 RC1 타입 인수 기록은 포함하지 않는다. 사용자가 10월 6일 자료를 받았다는 사실과 이번 RC0 요청의 미전달·미합의 상태는 별개다.

현재 `RecoveryStageSnapshot`, `RecoveryCommandResult`, `RecoveryFailureReason` 후보는 존재한다. 다만 운영 회복 상태 읽기 경로에는 Run/진입 식별자가 없고 Snapshot 생성·게시도 연결되지 않았다. 기존 후보가 있다는 사실만으로 V4 실제 입력 계약을 인수한 것으로 처리하지 않는다. View가 임의로 Core 문맥을 발급하거나 Core 서비스·Store를 수정하지 않는다.

**Core에서 먼저 확정·제공할 최소 산출물:**

1. **RC0 실제 계약표:** 회복 입력에 담을 Core 소유 문맥의 타입·유효 범위·발급/보존 수명·공개 읽기 위치, Select/Complete의 인수·검증 책임, 번호/결과 보존·취소·실패 분류와 공용 파일 편집 소유를 명시한다. 기존 타입을 재사용할 수 있다면 실제 사용할 필드와 보장을 지정한다.
2. **RC1 타입 인수 자료:** 합의된 문맥·상태·결과 타입 또는 기존 타입을 그대로 쓴다는 확정, 실제 API/소유 파일 목록, 유효 범위·불변성 계약 테스트와 컴파일 증거를 제공한다. 운영 발급·상태 게시·처리 연결은 RC2/RC3과 구분하며 RC1에서 View 입력 Buffer를 요구하지 않는다.

인수 후 View는 [이벤트·회복 계획 §3.5](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#35-v4-회복-입력-buffer-2026-10-12)(옛 §8)에 실제 명령 생성 서명을 반영하고 입력 Buffer·읽기/쓰기 Sink·공용 조립만 구현한다. 현재 V4 구현은 보류이며 기존 기능의 컴파일·관련 회귀 **695/695**는 통과했다. 세부 근거와 검증 범위는 [이벤트·회복 계획 §3.5](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#35-v4-회복-입력-buffer-2026-10-12)(옛 §19)를 따른다. 이 확인 기록은 Core 작업자에게 새 요청을 발송했다는 의미가 아니다.

---

## 3부. 회복 RC0·RC1·RC2 View 계약 인수 기록

- 확인일: 2026-10-12 (Asia/Seoul).
- 확인 대상: `C:/Users/as010/Project_L`, `feature/Hojin/Stage_Shop_Event`, HEAD `e33050273000cdb140801f2aeb753cad65b95a2d`.
- 범위: 실제 선언·구현·조립·테스트 소스와 인계 기록을 읽고 View 설계·Task 계획에 반영한 **문서/소스 인수** 기록이다. 이 인수 단계에서는 Unity 컴파일·테스트·회복 Play를 실행하지 않았다.
- 인수 당시 상태: RC0 공개 계약 대조와 RC1 타입·RC2 서비스 소스 인수 완료, V4·RC3·RC4는 미구현이었다. A1과 후속 V5~V6 수명 설계 항목 A2는 §4에서 구분한다.
- **후속 실행:** 같은 날 별도 사용자 승인으로 V4 입력·등록·검증을 완료했다. 현재 제공 API·새 XML/로그·사용자 커밋 대기 상태는 [V4 Core 인계서](#4부-v4-회복-입력-buffer--core-인계), 실행 기록은 [이벤트·회복 계획 §3.5](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#35-v4-회복-입력-buffer-2026-10-12)(옛 View 계획 §21)을 따른다. 아래 §1~5는 최초 문서/소스 인수 증거로 보존한다.

### 1 근거와 전달 상태

| 근거 | 이번에 확인한 사실 |
|---|---|
| [RC0 제안 §4](2026-10-11-recovery-core-code-audit-and-rc0-rc1-proposal.md#4-rc0-proposed-agreement) | 입력·상태·결과 정책 원본. 머리말의 미합의와 §2의 옛 브랜치 부재 목록은 당시 이력이다 |
| [RC1 인계](2026-10-11-recovery-rc1-view-handoff.md) | 사용자가 제공한 Downloads 사본과 저장소 문서의 본문이 줄바꿈·끝 공백을 제외하고 동일하다. 매니페스트의 8개 Core 타입·3개 Systems 인터페이스를 실제 선언과 대조했다 |
| RC1 커밋 `5cc2c2b8` | `[천명] 체력 회복 스테이지 식별 계약 구현`. RC1의 미커밋 표기는 당시 상태이며 현재 HEAD에 포함된다 |
| RC2 커밋 `fa618534` | `[천명] 회복 조회 연결 작업 1`. RC2-A·RC2-B 코드·테스트·설계·계획이 포함되고, 병합 `8186a6c1`을 거쳐 현재 HEAD에 포함된다 |
| RC2~RC4 계획 §9~13 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#9-execution-ledger-and-view-handoff-checkpoints`) | RC2-A/B 완료, RC3-A는 V4 부재로 선행 점검 후 중단, RC3-B는 Stage origin 계약 대기, RC4 미착수 |

이 기록은 새 외부 메시지를 발송하거나 Core 담당자의 추가 합의를 받은 기록이 아니다. 원본 RC1 인계와 Core 실행 이력은 당시 증거로 보존한다. 현재 View 수령·대조 상태의 원본은 이 문서, View 설계 결정은 [설계 §4.2·5](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md), 구현 파일·Task·검증 범위는 [계획 §8·13·20](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md)이다.

### 2 RC1 실제 공개 계약 대조

아래 C는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts`, 네임스페이스 접두사는 `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts`다. 입력은 `.Commands`, 표시 상태는 `.Snapshots`, 결과는 `.Results`에 있으며 Unity·Framework·View를 참조하지 않는 `ProjectL.Core.Contracts` 소속이다. 각 소스와 `.meta`가 존재한다. 전체 생성자·검증 스펙은 RC1 인계 §2와 실제 소스가 원본이다.

| 실제 파일 | View가 인수한 형태 |
|---|---|
| C/Commands/RecoveryEntryContext.cs | readonly struct. `EntryId`, `ChapterKey`, `NodeId`, `StageKey`; 생성자 인수도 같은 순서. 동등 비교는 네 필드 전체 |
| C/Commands/RecoveryCommandKind.cs | `Select = 1`, `Complete = 2` |
| C/Commands/RecoveryCommandData.cs | sealed 불변 클래스. `Kind`, `Context`, `RecoveryOptionId? OptionId`. `Select(RecoveryEntryContext, RecoveryOptionId)` / `Complete(RecoveryEntryContext)` 정적 생성 |
| C/Snapshots/RecoveryOptionState.cs | `Option`, `ProjectedHealAmount`, `UnavailableReason`; `CanSelect`는 파생값 |
| C/Snapshots/RecoveryStateSnapshot.cs | `Context`, `MainCharacterHealth`, `Gold`, 순서가 보존된 읽기 전용 `Options`, `Receipt`, `IsCompleted`; `CanComplete`는 파생값 |
| C/Results/RecoveryResultKind.cs | `Accepted`, `TradeRejected`, `StateMismatch` |
| C/Results/RecoveryOperationResult.cs | `Kind`, `FailureReason`, `AlreadyApplied`, `StateAfter`, `Progression`; `Succeeded`는 파생값. Selected/Completed/TradeRejected/StateMismatch 생성 경로 |
| C/Results/RecoveryProcessedResult.cs | 양수 `Number`, 원본 `Command`, `Result`. 결과 문맥과 명령 종류의 구조적 일관성을 검증 |

Systems 인터페이스는 실제 파일명과 네임스페이스를 그대로 사용한다.

| 소스 경로 (`Assets/ProjectL/Script/Systems/RuntimeSystem/` 아래) | 실제 서명 | 현재 구현/접근 |
|---|---|---|
| BufferSystems/Recovery/IRecoveryStateReadSink.cs | `bool TryGetCurrent(out RecoveryStateSnapshot state)` | RC2 구현·등록 있음. `SystemGroupsService.InGame → CoreGroup.RecoveryState` |
| BufferSystems/Recovery/IRecoveryResultReadSink.cs | `int HandledCommandNumber { get; }`, `bool TryGetLatest(out RecoveryProcessedResult result)` | 선언만 있음. `IBufferQuery.RecoveryResultReadSink`는 RC3 예정이며 현재 접근자로 가정하지 않음 |
| InGameSystem/CoreSystem/RecoverySystem/IRecoveryCommandProcessingService.cs | `bool ProcessNextRecoveryCommand(int expectedNumber)` | 선언만 있음. `CoreGroup.RecoveryCommands`는 RC3 예정이며 현재 접근자로 가정하지 않음 |

세 인터페이스는 `IServiceModule`이다. RC1 인계의 “모두 구현·등록 없음”은 RC1 시점의 설명이며, 현재 상태 조회 서비스에는 적용하지 않는다. 옛 `RecoveryStageSnapshot`/`RecoveryCommandResult`와 RunId·RunRevision 후보를 새 입력 계약으로 혼용하지 않는다.

### 3 RC2 실제 서비스 인수와 한계

- [RecoveryStageService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStageService.cs)는 진입 검증 후 `RecoveryEntryContext`를 발급해 선택지와 함께 설치한다. `CurrentRecoveryData.WithRecovery`는 문맥을 보존한다. 다른 진입·새 Run의 같은 chapter/node/stage라도 새 EntryId로 구별한다. View는 ID를 발급하지 않는다.
- [RecoveryOptionEvaluator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Run/RecoveryOptionEvaluator.cs)를 표시 투영과 기존 계산에서 재사용한다. HP·소울·골드·완료·영수증의 원본은 기존 Buffer다. View에 규칙 계산이나 별도 지갑·영수증을 추가하지 않는다.
- [RecoveryStateQueryService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStateQueryService.cs)는 호출 시점의 사실을 [RecoveryStateProjection](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStateProjection.cs)으로 읽는다. 옵션 규칙은 진입 때 설치된 목록을 사용하고 현재 표시를 위해 InGame DB를 다시 읽지 않는다. 보관한 스냅샷은 과거 값으로 유지된다.
- 현재 상태 읽기 경로는 `runtime.GetService(SystemGroupsService.InGame).CoreGroup.RecoveryState.TryGetCurrent(out state)`다. 비활성 회복은 false/null, 여전히 선택된 완료 노드는 완료 상태를 읽을 수 있다. 누락된 바인딩·필수 데이터 오류를 정상 false로 바꾸지 않는다.
- `IRecoveryStageService.SelectRecovery(RecoveryEntryContext context, RecoveryOptionId optionId)`와 `CompleteRecovery(RecoveryEntryContext context)`는 `RecoveryOperationResult`를 반환한다. 기존 overload는 유지된다. 동일 옵션 재시도·거절·미선택 완료·stale 문맥 처리는 [RC0 §4.3](2026-10-11-recovery-core-code-audit-and-rc0-rc1-proposal.md#43-operations-and-result-classification)의 분류/우선순위를 따른다. 별도 실패 표를 복제하지 않는다.
- 결과 DTO·변경 사실을 먼저 준비하고 재검증 뒤 설치하는 내부 거래 경로가 있다. Factory는 projection/guard를 공유하고 query 서비스와 PostBindService 모듈을 등록한다. Core 내부 transaction/Store는 View에 공개하지 않는다.

위 서비스 존재는 번호 있는 View 요청 경로의 완료를 뜻하지 않는다. UI/Controller가 typed 서비스를 직접 호출해 입력·결과 수명을 우회하지 않는다. `RecoveryCommandProcessingService`, `RecoveryResultBuffer`, `RecoveryViewBuffer`는 없고, 기존 `StageCommandProcessingService`의 Event 진입도 아직 연결되지 않았다.

> 2026-10-13: 세 타입은 이후 V4(`3aad1e83`)와 RC3-A(`535554e7`)로 생겼다. Stage Event 진입(RC3-B)은 아직 없다.

### 4 V4 인수 범위와 남은 좁은 결정

V4는 RC1 `RecoveryCommandData`에 요청 번호를 붙여 한 건의 입력을 보관한다. 공개 API는 Core 설계 §6 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-design.md#6-numbered-processing-and-retained-results-rc3`)와 [이벤트·회복 계획 §3.5](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#35-v4-회복-입력-buffer-2026-10-12)(옛 View 계획 §8)에 맞춘다. 옛 View 제안의 Commands 목록·TryGetNextCommand·RecoveryViewCommandData·RemoveCommandsThrough는 V4 계약에서 제외했다.

확인 책임은 세 층으로 구분한다.

1. **V4 입력 Buffer:** 양수 요청 번호·단조 증가·한 입력·정확한 번호 조회/제거·ack 숫자 범위와 단조성. Clear와 제거는 ack가 아니며 발급 번호와 ack는 같은 Buffer 인스턴스의 새 Run/노드 전환에도 보존한다.
2. **View 요청 수명 소유자:** 실제 결과를 읽고 Number와 전체 Command(Kind, Context 네 필드, OptionId)를 대조한 뒤만 ack한다. 숨김·진입/Run 변경 후에도 과거 소유 결과는 소비하고 새 화면에는 그리지 않는다. 화면/Stage 연결 종료로 유일한 소유 기록을 지워서는 안 된다.
3. **Core RC3 processor/result Store:** 미확인 결과가 있으면 다음 거래·게시를 막고, ack와 처리 이력을 검증한다. 거래 후 Hide/Clear가 있어도 결과 게시를 끝낸다. 이 보장은 RC3/RC4 실제 조립 테스트 대상이다.

**V4 설계 결정 A1:** 위 책임 분리를 [View 설계 §5·결정 A1](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#5-회복-입력-buffer-v4)(옛 §4.2)에 구체화했다. V4 Buffer는 숫자 경계만 검증하며 `IRecoveryResultReadSink`에 의존하지 않는다. 숫자 검사만으로 미게시·취소 번호 또는 실제 결과 읽기를 검증했다고 주장하지 않는다. read/full-command correlation은 호출자 선행 조건이고 handled-history와 미확인 결과 덮어쓰기 차단은 Core 책임이다. 이는 공개 서명을 바꾸지 않는 이번 View 설계 결정이며, 기존 구현이나 새 Core 회신으로 기록하지 않는다. 별도 Core 회신을 V4 필수 선행 조건으로 추가하지 않는다.

**후속 수명 리뷰 항목 A2:** V5~V6 실행 전에 숨김·Stage 재연결·새 Run에 걸쳐 retained Number/Command를 유지하는 구체 소유 객체와 Tick/종료 경로를 확정하고, 계획 §9~10에 파일·주입·Task·수명 테스트를 보충한다. 현재 계획의 Stage 수명 Flow·씬 Controller만으로 runtime Buffer 전체 수명을 보장했다고 처리하지 않는다. RC3 담당자에게 이 설계 책임을 전달하고, RC3의 V4 인수 시 실제 번호/ack 테스트와 함께 확인한다. V4는 숫자/입력 경계를 제공하며 UI 소비자 구현 완료를 주장하지 않는다. A2를 V4 입력 Buffer 구현의 선행 조건으로 확대하지 않는다.

별도 외부 조건은 U0(HUD 입력/초점 차단·복원), D0(운영 외형·DB 인수), RC3-B의 Stage 진입 요청 origin/수명 계약이다. 진입 후 발급되는 Recovery Context로 진입 전 클릭을 검증할 수 없다. 이 조건을 V4에 Core 운영 전체 완료를 요구하는 조건으로 확대하지 않는다.

> 2026-10-13: Core가 RC3-B 재개에 요구하는 Stage/Run 인계 3건(옛 Stage 요청을 Run 교체 전에 취소하는 경로, 같은 챕터·노드 키로 만든 새 Run의 회귀 테스트, Core가 다시 확인할 불변 origin 정보)은 Core 계획 §15 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#15-phase-4-prerequisite-inspection-2026-10-13`)에 있다. 담당은 호진이다(사용자, 2026-10-13). 작업 목록은 [이벤트·회복 계획 §3.6](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#36-er1-stagerun-origin-2026-10-14)(옛 View 계획 §3.2)다.

### 5 검증 증거와 이번 문서 검증

| 출처 | 기록된 검증 | 이번 인수에서의 의미 |
|---|---|---|
| RC1 인계 §5 | 전체 Core 2,122, 선택 Framework 141 통과 | Core 담당자 checkout의 과거 실행 증거 |
| RC2~RC4 계획 §12.2 | 전체 Core 2,139 + 선택 Framework 809 = 2,948 통과, 실패/skip 0 | RC2 상태 조회·typed 서비스·기존 기능의 과거 실행 증거. 번호 처리/팝업 인수는 아님 |
| 현재 코드 | RC1 계약 4 fixture, RC2 context/evaluator/query/typed 및 조립 fixture 존재 | 소스 존재와 선언·조립을 대조함. 현재 HEAD에서 테스트를 재실행한 증거는 아님 |

원본 `Logs/RecoveryRC1/`, `Logs/RecoveryRC2RC4/` XML·로그는 Core 담당자의 `D:/My_ProjectWork/Github_desktop/Project_L`에서 생성됐으며 이 checkout에는 없다. 기록된 통과 수를 현재 PC에서 다시 확인한 통과로 쓰지 않는다. V4 구현 시 최신 HEAD로 컴파일·관련 회귀를 새로 실행한다.

이번 변경은 문서 5개뿐이다. 실제 선언/파일·커밋 포함 관계·계획의 서명·상태·공용 파일 범위를 대조했다. 문서의 로컬 링크 121개와 해당 Markdown 앵커를 검사해 누락이 없었고 `git diff --check`는 종료 코드 0이었다. 새 인수 기록의 UTF-8·행 끝 공백도 확인했다. 설계/Task 자체 점검과 독립 읽기 전용 검토 2건을 반영했다. 새 실행 증거 없이 V4 구현 체크나 RC3/RC4 완료를 표시하지 않았다.

---

## 4부. V4 회복 입력 Buffer — Core 인계

- 작성일: 2026-10-12 (Asia/Seoul).
- 상태: **View Phase D(V4) 구현·검증 완료. 사용자 커밋·push와 Core 병합/인수는 대기 중이며 외부 발송하지 않았다.**
- **2026-10-13 현재:** V4는 사용자 커밋 `3aad1e83` 뒤 Core 브랜치에 병합됐고(`58c2f12e`), RC3-A(번호 처리·결과 보존)는 천명님 `535554e7`로 완료돼 이 브랜치에 들어왔다(`56c38c3a`, Core 계획 §14 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#14-phase-3-execution-record-2026-10-13`)). RC3-B(Stage Event 진입)는 Stage/Run 인계 3건을 기다리며 멈춰 있고(Core 계획 §15 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#15-phase-4-prerequisite-inspection-2026-10-13`)), RC4는 미착수다(→ 바뀜: RC3-B·RC4는 천명님 `92440b83`(2026-10-14)으로 완료·인수). 아래의 "대기" 표기와 §3의 3·4번은 당시 상태다. 상태 원본은 [회복 Core 계약 요청 §10](#10-회신-기록)이다.
- 기준: `feature/Hojin/Stage_Shop_Event`, HEAD `e33050273000cdb140801f2aeb753cad65b95a2d` 위의 미커밋 변경. 이 SHA는 제공 커밋이 아니라 작업 시작 기준이다. 제공 커밋은 사용자가 생성한 후 전달한다.
- 범위: [이벤트·회복 계획 §3.5](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#35-v4-회복-입력-buffer-2026-10-12)(옛 View 계획 §8)의 D4.1~D4.3. RC1 `5cc2c2b8`·RC2 `fa618534`가 포함된 코드로 검증했다. RC3·RC4·V5~V8는 이번 완료 범위가 아니다.

### 1 실제 제공 코드와 접근 경로

정확한 공개 계약의 원본은 계획 §8, A1 책임 결정은 [설계 §5·결정 A1](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#5-회복-입력-buffer-v4)(옛 §4.2)다. 아래는 이번 구현을 대조한 인계 표다.

| 제공 항목 | 실제 파일과 내용 |
|---|---|
| 입력 wrapper | [RecoveryViewCommand.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/RecoveryView/RecoveryViewCommand.cs): 공개 `readonly struct`, `int Number`, 원본 `RecoveryCommandData Data`. 생성자는 `internal`이며 Submit이 번호를 발급한다. 기본값은 입력 없음이다 |
| 입력 Sink·저장소 | [RecoveryViewBuffer.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/RecoveryView/RecoveryViewBuffer.cs): 두 공개 Sink는 `IServiceModule`; 저장소는 `internal sealed` |
| Group 접근 | [BufferSystemGroup.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs): `IBufferQuery.RecoveryViewReadSink` / `RecoveryViewWriteSink`와 Group 생성자 조회 |
| 서비스 키 | [RecoveryViewBufferKey.cs](../../../Assets/ProjectL/Script/Systems/ServiceKeys/Runtime/Buffer/RecoveryViewBufferKey.cs): 내부 `RecoveryViewBufferKey.Read/Write` |
| 생성·등록 | [BufferSystemFactory.cs](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs): Build 전에 한 인스턴스를 생성하고 두 인터페이스로 Export. 두 Sink는 같은 객체이며 runtime마다 별도 객체다 |
| 새 검증 | [RecoveryViewBufferTests.cs](../../../Assets/Tests/EditMode/Framework/RecoveryViewBufferTests.cs): 입력 경계 20건 + 서비스/실제 runtime 조립 4건 |

공개 namespace는 `ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView`다.

```text
IRecoveryViewBufferReadSink
  int AcknowledgedResultNumber { get; }
  bool TryGetCommand(int expectedNumber, out RecoveryViewCommand command)

IRecoveryViewBufferWriteSink
  int Submit(RecoveryCommandData command)
  bool RemoveCommand(int number)
  void AcknowledgeResult(int number)
  void Clear()
```

`RecoveryCommandData`는 Core `.Contracts.Commands`의 불변 타입을 그대로 보관한다. `Select(context, optionId)` / `Complete(context)`가 만든 인스턴스와 Context 전체를 유지하며 별도 명령 DTO·규칙 값·Context 복사 필드를 만들지 않았다. 결과 Store·processor·`RecoveryResultReadSink`/`RecoveryCommands` 접근자는 추가하지 않았다.

### 2 번호와 취소 계약

| 경계 | 실제 동작 |
|---|---|
| 발급 | 새 runtime의 첫 Submit은 1. 슬롯이 비어 있을 때만 다음 번호를 발급한다. 대기 입력이 있으면 `InvalidOperationException`; 기존 입력과 번호를 보존한다 |
| 읽기 | 양수 expectedNumber와 정확히 일치할 때만 true. 반복 조회는 비소비. 다른 양수는 false/default이며 최신 입력으로 대체하지 않는다 |
| 제거 | 양수 번호가 정확히 일치할 때만 true·슬롯 제거. 다른 양수와 반복 제거는 false |
| 비양수 번호 | TryGetCommand·RemoveCommand·wrapper 생성은 `ArgumentOutOfRangeException`. ack의 초기 0 허용과 구분한다 |
| null·상한 | Submit/wrapper의 null Data는 `ArgumentNullException`. 마지막 발급값이 `int.MaxValue`이면 다음 Submit은 상태 변경 전 `InvalidOperationException`. 번호 순환은 없다 |
| Clear·새 Run | Clear는 슬롯만 비운다. Remove/Clear/실제 Run reset·새 Run은 발급·ack를 초기화하지 않는다. Run reset 자체는 대기 입력도 지우지 않는다. 취소한 번호의 공백은 허용한다 |
| 실행 수명 | 같은 runtime thread의 동기 호출 계약. 새 Buffer 인스턴스에서만 번호·ack가 다시 0부터 시작한다. 이전 runtime의 Job을 새 runtime에 적용하지 않는 책임은 후속 실행 경계에 있다 |

**A1:** ack는 `0 <= number <= lastIssued`와 기존 ack 이상을 검사한다. 초기 0·동일 값 재확인은 no-op이며 음수·역행·발급 상한 초과는 상태 변경 전 거절한다. Clear/Remove/Hide가 결과 소비 확인을 대신하지 않는다.

Buffer는 결과 Reader나 Core 처리 이력을 참조하지 않는다. 따라서 발급됐지만 취소되거나 게시되지 않은 번호도 숫자 조건만 맞으면 ack API를 통과할 수 있다. **실제 결과 읽기와 Number/전체 Command 대조 후 호출하는 것은 View 요청 소유자의 선행 조건**, 게시/처리 이력 검증과 미확인 결과 덮어쓰기 차단은 **Core RC3 책임**이다. 이번 ack 테스트를 실제 결과 소비·backpressure의 통합 증거로 사용하지 않는다. 실패 분류·처리 반환값 정책은 [RC0 §4.3~4.4](2026-10-11-recovery-core-code-audit-and-rc0-rc1-proposal.md#43-operations-and-result-classification)를 유지한다.

### 3 공용 편집 순서와 다음 담당

1. 시작 시 HEAD와 dirty 목록을 확인했다. 기존 변경은 계약 인수·설계·계획 관련 문서 5개였으며 공용 C# 파일 변경은 없었다. Core RC3 계획의 V4 대기 상태를 읽었고, 이번 세션의 공용 파일 편집은 View 한 작업자가 담당했다. 독립 검토자는 읽기만 수행했다. 외부 편집 예약이나 동의를 받았다고 기록하지 않는다.
2. **View 입력 작업 완료:** 입력 소스/경계 검증 → Group 입력 프로퍼티·키·테스트 대역 최소 선언 → 조립 RED → Export/Group 조회/Factory 등록·수동 테스트 조립 → Q4a/Q4b 순으로 진행했다. Core 출력 등록을 먼저 만들지 않았다.
3. **사용자:** 코드·meta·관련 문서를 검토하여 커밋·push하고, 실제 커밋 SHA와 이 문서를 Core 작업자에게 전달한다. XML/로그는 로컬 ignored 경로이므로 검증 증거까지 전달할 때 별도로 첨부한다. Codex는 스테이징·커밋·push·외부 메시지를 실행하지 않았다.
4. **Core RC3-A:** 실제 V4 커밋을 병합하고 위 API·수명 테스트를 대조한 뒤 결과 Store·번호 처리·공용 출력/processor 조립을 진행한다. 공용 파일 순서는 계속 **View 입력 → Core 출력/처리 → 실제 조립 검증**이다.
5. **Core RC3-B/RC4:** Stage Event 진입은 Stage/Run 담당자의 요청 origin/수명 계약을 별도로 인수한다. 실제 입력→결과, 미확인 결과 덮어쓰기 차단, 취소/Hide 전후 게시, 같은 키의 새 Run·늦은 요청/결과를 검증해 View에 인계한다.

**A2는 View 후속 설계 책임:** V5~V6 실행 전에 숨김·Stage 재연결·새 Run에 걸쳐 retained Number/Command를 유지할 구체 객체와 Tick/종료 경로를 확정하고 계획 §9~10에 파일·주입·수명 테스트를 보충한다. 현재 V4 입력 저장소와 Stage 수명 Flow/Controller만으로 이 소비자 수명을 구현했다고 판단하지 않는다. RC3 담당자는 이 잔여 책임을 인수 조건에 반영하되 UI 소비자를 Core가 대신 만들지 않는다. U0·D0·Stage origin도 각각 별도 조건이다.

### 4 이번 PC의 검증 증거

실행 환경은 Unity **6000.0.73f1**, Windows, `C:/Users/as010/Project_L`이다. Editor가 열려 있지 않음을 확인한 뒤 숨김 batchmode EditMode를 순차 실행했다. 필터 원본은 [이벤트·회복 계획 §4 시험 위치](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#4-시험-위치)(옛 §13 Q4a/Q4b)다.

증거 디렉터리: `C:/Users/as010/Project_L/.superpowers/sdd/2026-10-11-noncombat-event-ui-plan/v4-20261012/`. 각 실행 이름에 `.xml`, `.log`, `.exit.txt`가 있으며 `run-tests.ps1`과 `verify-evidence.ps1`도 보관했다. 이 경로는 Git ignored이며 Core의 과거 2,948건 기록과 별개다.

| 실행 이름 | 결과 | 의미 |
|---|---|---|
| `V4-baseline` | 1,268/1,268, Unity 0 | 구현 전 Q4a+Q4b 기존 기능 |
| `V4-input-red` | 314 중 295 통과·19 의도한 실패, Unity 2 | 최소 선언에서 새 입력/번호 동작 부재 확인. 기존 294건은 통과 |
| `V4-input-green` | 314/314, Unity 0 | 입력 경계 구현 후 |
| `V4-assembly-red` | 318 중 314 통과·4 의도한 실패, Unity 2 | Export·null guard·실제 Factory/Group 연결 부재 |
| `V4-assembly-green` | **318/318**, Unity 0 | Q4a: 새 24건·RC1 계약·실제 조립·호환 |
| `V4-final-related` | **1,028/1,028**, Unity 0 | Q4b: RC2·상점·아티팩트·Stage 입력/팝업·Camera Boot 등 |

모든 실행의 C# 컴파일 오류는 0. 최종 두 실행의 실패·skip·inconclusive는 0이며, 각 필터가 실제 XML에 존재함을 확인했다. 두 최종 실행의 중복 54건을 제외하면 **1,292개 고유 테스트**로, baseline의 1,268개 전부와 새 V4 24개다. 기존 Camera Boot 8건을 포함하지만 회복 운영 Play·RC3 결과 통합 완료 증거는 아니다.

계획의 검증 범위·SSOT/SOLID/Framework 조립 관점으로 독립 읽기 전용 코드 리뷰를 받았으며 차단할 문제는 없었다. 최종 작업 범위·문서 검사는 계획 §21에 기록한다.

### 5 변경 범위와 규칙 확인

- 새 C# 3개와 meta 4개(RecoveryView 폴더 포함), 공용 입력 조립 C# 3개, 계획된 테스트 호환 C# 3개를 반영했다. 테스트 대역은 `EditFaultQuery`·`ResetSpy`, 수동 조립은 `CharacterRosterServiceTests`이며 기존 기대값은 유지했다.
- Framework FW-001/002/003/016: 인터페이스별 등록·동일 인스턴스·Build 1회·기존 runtime 초기화 순서를 따른다. 입력 저장소에 Tick/InitData/씬 수명이 없어 AddModule이나 새 전역 System을 만들지 않았다.
- SSOT/SOLID/JMH: 원본 불변 명령을 재사용하고 입력/번호 책임만 맡는다. 읽기/쓰기 인터페이스를 분리하며 규칙 계산·결과·요청 수명을 복제하지 않는다. InGame DB 조회·Core 쓰기 Sink를 추가하지 않았다.
- UI·Camera: 화면/UIInputBinding·Button 이벤트·씬·프리팹·InputLayer·CameraCatalog/Effect·FieldCamera·Architecture를 변경하지 않았다. 후속 UI/모달 게이트는 유지했다.
- Core: CoreSystem의 계산·서비스·query·transaction·결과 Store·Core Factory는 변경하지 않았다. `BufferSystems/Core/BufferSystemGroup.cs`는 승인된 공용 입력 접근자 조립만 수정했다.
- Unity가 변경한 FMOD 스캔 시각/로컬 경로는 나머지 내용이 동일함을 확인한 뒤 시작 시점의 정확한 바이트로 복원했다. 옛 에셋 삭제·DB 값 변경은 없다.

**Phase D에서 종료한다.** 다음 실행은 사용자 커밋을 Core가 인수한 뒤 RC3/RC4를 진행하는 단계이며, View V5~V8는 해당 인수와 후속 설계 조건을 따른다.

---

## 5부. Event 노드 회복 진입 Core 연결 요청

- 수신: Core 담당 천명. 선행 Stage/Run 수명 인계: 호진, 필요한 Combat 경계는 용근과 협의.
- 작성·정리: 2026-10-13, ER1 증거 추가: 2026-10-14 (Asia/Seoul). 상태: **전달 준비. S1~S3의 호진 범위 증거 작성 완료, Core/Combat 인수와 C1~C4 구현 대기.** (→ 바뀜: C1~C4에 해당하는 Core RC3-B·RC4는 천명님 `92440b83`(2026-10-14)으로 완료·인수 — [이벤트·회복 계획 §3.7 ER2](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#37-er2-core-진입-연결-천명님-2026-10-14))
- 설계/Task: [연결 설계](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#6-core-계약)(옛 2부, 지금 §6~§9), [이벤트·회복 계획 §1](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#1-진행-요약)(옛 2부 Task 계획).

기존 회복 기능을 재사용하여 **StageSelect의 Event 노드 입력을 회복 진입으로 연결해 주세요.** 회복 진입·선택·완료 API, 상태 조회, 번호 처리와 결과 보존은 이미 제공됩니다. 추가로 필요한 범위는 기존 Stage 노드 endpoint의 Event 분기와 진입 결과 게시, 실제 입력 경로의 통합 인수입니다.

최종 사용자 흐름은 Event 클릭 → 회복 팝업 → 선택 또는 미선택 → 명시적 나가기 → 같은 맵 복귀입니다. Core는 진입/거래/완료 상태를 제공하고, 팝업 표시와 맵 복귀 처리는 View가 연결합니다.

### 1. 이미 구현된 기능과 재사용 범위

| 구분 | 제공된 기능 | 이번 연결에서의 사용 |
|---|---|---|
| Core 회복 서비스 | `RecoveryStageService.EnterRecovery`, `SelectRecovery(context, optionId)`, `CompleteRecovery(context)` 및 기존 호환 API | 기존 진입 검증·비용/회복 계산·중복 방지·노드 완료 정책 재사용 |
| Core 상태 조회 | `CoreGroup.RecoveryState.TryGetCurrent(out state)` | Context·옵션·HP·골드·선택 가능/불가 사유·영수증·완료 상태를 View에 제공 |
| View 회복 입력 V4 | `RecoveryViewBuffer`의 Submit·정확한 번호 조회/제거·소비 확인(ack)·Clear | 기존 Number/불변 명령/숫자 수명 계약 유지. Core에서 입력 Buffer를 새로 만들지 않음 |
| Core 번호 처리 RC3-A | `CoreGroup.RecoveryCommands.ProcessNextRecoveryCommand(int expectedNumber)` | 선택/Complete를 정확한 번호로 처리. 취소/기처리/이전 결과 미소비 대기는 false, 정상 거절/문맥 불일치도 처리 결과 게시 |
| Core 결과 보존 | `IBufferQuery.RecoveryResultReadSink`의 `HandledCommandNumber`·`TryGetLatest` | 요청 Number와 전체 Command가 포함된 결과를 보존하고 미소비 결과의 덮어쓰기를 차단 |
| Core 조립 | 회복 서비스·상태 Query·번호 processor가 Factory에 등록됨 | 같은 회복 서비스 인스턴스를 Stage 진입에도 명시적으로 연결 |
| 등록 데이터 | `Stage_Event` 1301 → `Event_Recovery` → 옵션 1·2·3 | 기존 DB 정의를 사용. 비용/회복량/옵션을 View에서 새로 정하지 않음 |

이 기능들은 구현·병합된 현재 소스를 기준으로 재사용합니다. 이번 요청으로 회복 계산·입력/결과 Buffer·공개 회복 API를 다시 구현할 필요는 없습니다. View의 운영 팝업과 요청 소비자는 별도 후속 작업입니다.

### 2. 현재 막히는 Event 진입 경로 (요청 당시 상태 → 바뀜: `92440b83`로 연결됨)

현재 경로는 `StageViewBuffer → StageNodeEntryFlow → StageNodeEntryJob → ProcessNextNodeCommand`입니다. 마지막 endpoint의 비전투 진입 판정은 **Shop/Regular만 허용**하고 수락 시 상점 서비스를 호출합니다. Event는 `UnsupportedStageType`으로 거절됩니다. 전투 진입은 기존 별도 경로를 유지합니다.

따라서 `EnterRecovery`가 이미 있어도 Event 클릭에서 자동으로 호출되는 연결은 아직 없습니다. 실제 Core 생성 맵의 Event1301 클릭 거절은 중앙 배치 후속 검증 T5에서 확인했습니다([기록](../plans/2026-10-10-stage-map-plan-Hojin.md#35-depth별-중앙-정렬), 옛 스테이지 맵 표시 계획).

추가 작업은 기존 Core RC3-B Task 4.2 및 RC4 Phase 5 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md`)에 해당합니다. 같은 기능의 병렬 구현 계획을 추가하지 않습니다. Core 계획 §15에 남은 Stage/Run 선행 인계를 먼저 해결해야 합니다.

### 3. 추가 Core 요청 C1부터 C4

#### C1 기존 노드 endpoint에 Event 분기 연결

- `ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed)`의 공개 서명과 기존 Stage 입력/Node 결과 경로를 유지해 주세요.
- 합법적인 지원 Event를 기존 회복 진입 검증·정의 조회로 연결해 주세요. 회복 지원 여부는 `RecoveryDefinitionAdapter`의 정의에 따르고 StageKey 1301이나 노드 이름으로 하드코딩하지 않습니다.
- 기존 Shop·Battle·Boss 경로와 미지원 종류의 거절은 유지합니다. View가 직접 `EnterRecovery`를 호출해 번호 입력/Job 경로를 우회하지 않도록 합니다.

#### C2 선택과 회복 상태와 맵 결과를 함께 설치

- 기존 Task 4.2에 계획된 내부 `PrepareEntry`/`InstallEntry`를 구현해 기존 직접 `EnterRecovery`와 Stage endpoint가 같은 진입 로직을 사용하도록 해 주세요. **이 내부 메서드는 현재 제공된 기능이 아니라 남은 연결 작업입니다.**
- 새 Context·옵션·현재 선택과 기존 `StageEntryCommandResult`를 변경 전에 준비하고 함께 설치합니다. 맵 결과는 같은 Number/ChapterKey/NodeId, `Kind=Node`와 진입 판정을 담아야 합니다.
- `ConfirmStageEntry`로 선택만 먼저 확정하지 않습니다. 정의 실패·실행 전 취소·정상 거절에는 부분 선택/회복 상태/HP/골드/클리어 변경을 남기지 않습니다.
- 같은 진입의 재처리가 영수증이나 이미 적용한 회복을 초기화하지 않도록 기존 중복 방지 정책을 유지합니다.

#### C3 옛 요청 재검증과 seed 계약 유지

- 아래 S1~S3 인수 후 외부 정의 조회가 끝났을 때 같은 Number·전체 맵 명령·진입 전 진행 사실을 다시 확인해 주세요. 취소된 옛 클릭이 동일한 키의 새 Run에 적용되지 않아야 합니다.
- 회복 Event의 맵 결과는 `Seed=0`이며 `nextSeed`를 호출하지 않습니다. 공개 endpoint의 non-null 인자 요구와 Shop의 기존 seed 소비/결과는 유지합니다.
- 진입 후 발급되는 `RecoveryEntryContext`를 진입 전 Run 식별자로 사용하거나 새 전역 RunId를 임의 추가하지 않습니다. 추가 token이 필요하면 담당 간 설계 합의가 먼저입니다.

#### C4 Factory 연결과 실제 Core 경계 인수

- 기존 회복 서비스를 Stage 처리 서비스에 명시적으로 주입해 주세요. 회복 Store/transaction을 View에 공개하거나 같은 거래 서비스를 중복 생성하지 않습니다.
- RC4에서 실제 Factory·Stage 입력 → Event 진입 수락/맵 결과 → 일치하는 Context 조회 → V4 Select → 번호/전체 명령 대조·ack/입력 제거 → Complete → 노드 완료 → 다음 전투를 검증해 주세요.
- 미선택 Complete, 정상 거절/중복/문맥 불일치, 처리 전 취소, 처리 후 입력 Clear에도 결과 보존, 같은 키 새 Run, 정의 조회 중 교체, Shop/전투 회귀를 포함합니다.
- 인계에는 변경 파일·공개 서명·지원 Event 범위·origin 합의·실행한 테스트 필터/XML/로그·남은 조건을 포함해 주세요. Core 경계 통과와 실제 회복 팝업 완료는 구분합니다.

### 4. Stage와 Run 담당의 선행 인계 S1부터 S3

담당은 호진입니다. Core RC3-B의 구현 전 아래 실제 수명 보장을 천명님에게 제공해야 합니다.

| 요청 | 인계 내용 |
|---|---|
| S1 교체 전 취소 경계 | 실제 Run/챕터 교체 호출자·소유 객체·등록 중지와 옛 Stage 입력 Clear → 교체 순서. 번호는 보존하고, 남은 queued Job은 정확한 옛 번호의 입력 부재로 거래 없이 false 완료 |
| S2 같은 키 새 Run 회귀 | queued 옛 클릭과 이미 시작한 정의 조회 중 교체를 각각 검증. 같은 chapter/node 키로 새 Run을 만들어도 옛 요청은 새 선택/회복 옵션/진입 결과를 남기지 않음 |
| S3 Core 재확인 정보 | Number·전체 명령·진입 전 사실로 보장 가능한지 합의. 부족하면 scoped origin token을 별도 설계/승인 |

`StageNodeEntryFlow.Reset`이나 Clear API의 존재만으로 S1/S2의 실행 순서와 회귀 증거를 대신하지 않습니다. `RecoveryEntryContext`는 진입 후 문맥이며 `GetStages()` 목록 래퍼 참조도 origin 증거로 사용하지 않습니다.

Stage/Lobby/개발 시작 호출자는 호진 범위에서 확인하고, 운영 Combat 새 Run 경로가 남으면 용근 담당과 협의합니다. Framework Runner 내부 큐는 직접 수정하지 않습니다. 기존 번호/취소 경로로 보장할 수 있으면 이를 우선 사용합니다.

**2026-10-14 ER1 인계 준비:** [Stage/Run 실행 증거](#6부-er1-stagerun-origin-선행-작업-인계)를 추가했습니다. Stage/Lobby 교체 전 Clear와 queued 같은 키 회귀, 번호/기처리 결과 보존 및 등록 차단/재개는 Unity에서 확인했습니다. 정의 조회 중 교체는 옛 Number 무효화까지 확인했으며, 현재 Shop Core의 옛 결과 게시도 기록했습니다. 조회 후 설치 방지는 C3에서 인수해야 합니다. Combat `StartNewRun` 및 `CompleteChapter`의 다음 챕터 설치는 기존 Stage 이탈·Detach·배출 선행 조건을 용근 담당이 인수해야 합니다. S1~S3 전체 합의나 C1~C4 완료로 표시하지 않습니다.

### 5. View가 맡을 후속 작업과 유지할 계약

Core 인수 뒤 View가 다음을 연결합니다([이벤트·회복 계획 §3.8~§3.11 ER3~ER6](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#38-er3-runtime-요청-수명과-실행-경로-2026-10-15)).

- runtime 동안 Number/전체 명령을 보관하고 결과 대조·ack·정확한 입력 제거를 수행합니다. Hide/Detach/fault로 유일한 기록을 잃지 않습니다.
- 기존 Flow/Plan/Job, UISystem·UIInputBinding과 P5/P6.1 구조를 사용해 팝업·모달 입력·현재 상태 복구를 연결합니다.
- Complete 수락 뒤 닫고 맵을 갱신합니다. 완료된 현재 회복 상태가 조회돼도 팝업을 재열지 않습니다.
- 현재 캡슐, 건물·아이콘 동시 숨김, 떠난 완료 노드 봉, 중앙 배치·연결선·카메라·드래그 위치를 유지합니다.

Core의 책임은 규칙/진입/거래/조회/결과 게시이며 Canvas·Popup·Layer·카메라 제어와 View 입력 제거는 요청 범위에 포함하지 않습니다. Framework 등록·입력·UI 이력·자원 수명 계약, Core/Buffer의 권위 상태, SSOT·SOLID 경계를 유지합니다.

### 6. 현재 소스 근거와 요청 상태 (→ 바뀜: 표의 Event 거절·Recovery 미연결은 `92440b83` 이전 상태)

| 소스 | 확인 지점 |
|---|---|
| [RecoveryStageService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStageService.cs) | 직접 EnterRecovery 및 typed Select/Complete 제공 |
| [RecoveryCommandProcessingService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryCommandProcessingService.cs) | 정확한 번호 처리·전체 명령 재확인·거래와 결과 게시 |
| [RecoveryStateQueryService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStateQueryService.cs) | 현재 회복 상태 조회 서비스 |
| [StageCommandProcessingService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/StageCommandSystem/StageCommandProcessingService.cs) | 현재 Shop/Regular 제한·상점 호출, Event 거절 |
| [InGameCoreSystemFactory.cs](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs) | 회복 서비스·processor·query 등록, Stage에 Recovery 의존성 미연결 |

S1~S3의 호진 구현·증거는 준비됐으며 Core/Combat 담당 인수와 C1~C4 구현은 대기입니다. 이 요청 문서는 담당자의 합의나 구현 승인을 대신하지 않습니다. 각 담당 Phase 종료마다 검증 결과를 보고하고 멈춥니다. 외부 발송·스테이징·커밋·push는 하지 않았습니다.

---

## 6부. ER1 Stage/Run origin 선행 작업 인계

작성: 2026-10-14(Asia/Seoul). 사용자 승인 범위인 **호진 Stage/Lobby 구현·검증 완료**, Core·Combat 담당의 전체 origin 인수는 대기한다. ER2는 시작하지 않는다. (→ 바뀜: ER2 = Core RC3-B·RC4는 천명님 `92440b83`(2026-10-14)으로 완료·인수 — [이벤트·회복 계획 §3.7 ER2](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#37-er2-core-진입-연결-천명님-2026-10-14)) 외부 발송·스테이징·커밋·push 없음.

실행 상태의 원본은 [이벤트·회복 계획 §3.6 ER1](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#36-er1-stagerun-origin-2026-10-14)(옛 ER1 실행 기록), 요청·수락 상태의 원본은 [Core 요청 S1~S3/C1~C4](#5부-event-노드-회복-진입-core-연결-요청)이다. 시작 HEAD는 `a79cbfaa859512f7e6382c90dfa2064d0e2fa873`, 브랜치는 `feature/Hojin/Stage_Shop_Event`, 시작 작업 트리는 깨끗했다.

### 1. 실제 교체 호출자와 보장 범위

| 경로/담당 | 실제 호출·설치 | 입력 취소와 등록 경계 | 이번 증거/남은 조건 |
|---|---|---|---|
| Stage/호진 | `NewRunJob` → `RunStart.StartNewRun` | 주입된 `IStageViewBufferWriteSink.Clear` → Core 호출 | 실제 Plan/Runner의 queued 옛 번호 무거래 완료, 같은 키 새 Run 회귀 통과 |
| Stage/호진 | `StageJob` → `ChapterProgression.EnterChapter` | 실행 때 현재 챕터 읽기 → Clear → Core 호출 | 같은 챕터 재진입도 이전 클릭 취소. 앞선 NewRun의 정의 조회 중 들어온 입력도 챕터 준비 전에 다시 취소 |
| Lobby/호진 | `LobbyRunStartJob` → `RunStart.StartNewRun` | `TryStart` 성공·잔존 전투 차단 → Clear → Core 호출 | 실제 JobRegister 회귀 통과. 취소된 출발은 Core 호출과 Clear 모두 생략 |
| Combat/용근 | `CombatNewRunJob` → `StartNewRun` | Job 내부 Clear 없음. 기존 Stage 이탈 등록 중지/배출 및 View Detach Clear가 선행 조건 | 기존 정상 이탈·패배·최종 승리·포기 회귀 통과. 아래 선행 조건을 Combat 담당이 인수해야 함 |
| Combat/용근 | `CombatRunEndJob` → `CompleteChapter` → `ContinueChapter` → `InstallChapter` | 위와 같은 Combat 활성화 전 Stage 이탈 조건 | 실제 다음 챕터 설치 호출자도 포함. 운영 챕터 연결 정의와 추가 호출 경로는 Core/Combat 인수 대상 |
| 개발 시작/호진 | private `GameBootstrap.PrepareDevProgress` → `StartNewRun`, `EnterChapter` | 새 runtime 최초 시작에서 `_startupPending`을 먼저 내린 뒤, 최초 LifeCycle/Stage View 연결 전에 실행 | 실행 중 Run 교체 API가 아니다. 이 경로를 실행 중 재사용하는 보장은 제공하지 않음 |
| 개발 fixture/Core 소유 | `TemporaryCoreRuntimeFactory.CreateScenario` → `EnterChapter` | 매번 새 Buffer/runtime 생성, Stage View/Flow/Job이 없는 fixture 초기화 | 운영 교체 증거로 사용하지 않음 |
| Core 내부 | `PublishNewRun` → `ResetRunProgress`, 챕터 transaction 설치 | 위 공개 호출자의 내부 구현. 자체 Stage 입력 취소는 없음 | Core/Buffer 코드 수정 없음. 새 직접 호출자를 추가하면 취소 선행 조건을 다시 인수해야 함 |

Source: [StageJob](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageJob.cs), [StagePlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs), [LobbyRunStartJob](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyRunStartJob.cs), [CombatExitJobs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatExitJobs.cs), [GameBootstrap](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs), [TemporaryCoreRuntimeFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/Development/TemporaryCoreRuntimeFactory.cs), [ProgressTransactions](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ProgressTransactions.cs).

등록 차단은 새 전역 상태를 만들지 않고 기존 [StageFlow.Tick](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageFlow.cs)의 `!Runner.IsIdle` 경계를 사용한다. 교체 Job 실행 중에는 노드 Job을 등록하지 않고, 배출 뒤 새 입력의 처리가 재개되는 회귀를 실행했다. [StageLifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageLifeCycle.cs)은 준비 Job 등록 뒤 Flow를 Reset하므로 Job 안에서 다시 Stop하지 않는다.

Combat 조건의 소스 근거는 StageLifeCycle의 Exit 준비(-200)·수락된 동일 전투 요청 전환 확인·Runner 배출 확인, [StageMapViewSystem.Detach](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewSystem.cs)의 입력 Clear(-100)이다. 일반 Stage 이탈과 실행 중 강제 교체를 같은 보장으로 확대하지 않는다. 이번에 Combat 파일을 수정하거나 새 직접 호출을 허용하지 않았다.

### 2. 구현과 번호 수명

제품 수정은 Stage/Lobby Job·Plan 4개 파일이다. Plan은 기존 BufferGroup에서 필요한 Stage 입력 WriteSink만 꺼내 Job에 주입한다. 기존 공개 Stage 입력·Core endpoint·Job 등록·UI·Camera 계약은 유지한다.

`Clear`는 입력 목록만 비운다. 발급 번호, `HandledMapCommandNumber`, 게시된 맵 결과를 초기화하지 않는다. 큐의 옛 `StageNodeEntryJob`은 기대 번호와 다른 새 입력을 처리하지 않으며, 기존 Core의 false도 `IsCompleted=true`로 완료한다. 다른 소유자의 Job을 삭제하지 않는다. 새 RunId, 처리 영수증 사본, Runner.Clear, View의 진행 Buffer 쓰기는 추가하지 않았다.

### 3. Unity 검증 증거

열린 Unity 6000.0.73f1의 TestRunnerApi로 EditMode를 실행했다. 두 번째 Editor는 띄우지 않았다. 실제 NUnit 클래스명은 partial `ShopStageServiceTests`이며 파일명으로 선택하지 않았다.

| 실행 | 선택/결과 | XML |
|---|---|---|
| 변경 전 기준 | 261/261 통과 | [baseline.xml](../../../Logs/StageEventRecoveryER1Verification/baseline.xml) |
| 신규 RED | 7건: 1 통과, 취소 누락에 따른 예상 실패 6건 | [red.xml](../../../Logs/StageEventRecoveryER1Verification/red.xml) |
| 신규 GREEN | 7/7 통과 | [green.xml](../../../Logs/StageEventRecoveryER1Verification/green.xml) |
| 관련 회귀 | 434/434 통과, 신규 7건 포함 | [regression.xml](../../../Logs/StageEventRecoveryER1Verification/regression.xml) |
| 기존 Combat Job/Plan/통합 경계 | 88/88 통과 | [combat-boundary.xml](../../../Logs/StageEventRecoveryER1Verification/combat-boundary.xml) |

모든 성공 실행의 실패·skip·inconclusive는 0이다. 기준/회귀/신규는 중복되는 테스트가 있으므로 서로 더하지 않는다. 회귀 434건과 추가 Combat 88건은 서로 다른 fixture다. 원본 XML·명령·status·시작 hash는 `.superpowers/sdd/2026-10-13-stage-event-recovery-entry-plan/`에도 보존한다. `Logs`와 이 scratch 경로는 Git 추적 대상이 아니며 전달 시 XML을 따로 포함한다.

필터:

```text
기준: ^ProjectL.Tests.Framework.(ShopStageServiceTests|StageFlowTests|LobbyDepartureTests|LobbyDepartureIntegrationTests|StageEntryAttemptReadTests|FrameworkRuntimeCompositionTests)$
RED/GREEN: ^ProjectL.Tests.Framework.ShopStageServiceTests.(QueuedStageRequest_SameKeysNewRun_DoesNotEnter|ChapterEntryJob_CancelsQueuedClickBeforeSameChapterEntry|DefinitionLookup_OriginReplacement_InvalidatesOldNumber|LobbyNewRunJob_CancelsStageClickBeforeCoreRunReplacement|RunReplacement_PreservesHandledReceiptAndIssuedNumber|CancelledLobbyDeparture_PreservesStageClickWithoutReplacingRun|ProgressReplacement_BusyRunnerSuppressesRegistrationAndFlowResumes)$
회귀: ^ProjectL.Tests.Framework.(ShopStageServiceTests|StageFlowTests|LobbyDepartureTests|LobbyDepartureIntegrationTests|StageEntryAttemptReadTests|FrameworkRuntimeCompositionTests|ChapterGraphPreparationTests|JobSystemTests|ArtifactBoardConfigTests|CombatEntryIntegrationTests|StageMapEntryObserverTests|StageMapViewSystemTests)$
Combat: ^ProjectL.Tests.Framework.(CombatProgressJobTests|CombatPlanTests|CombatProgressIntegrationTests)$
```

새 [StageRunOriginRegressionTests](../../../Assets/Tests/EditMode/Framework/StageRunOriginRegressionTests.cs)는 실제 Core Factory·Buffer·Plan·JobRegister·Runner로 테스트한다. fixture DB에서 Core가 생성한 챕터/노드/스테이지 키가 교체 전후 같음을 확인하며, 진행 그래프 Buffer를 덮어써 운영 성공을 만들지 않는다. 별도 테스트 대역인 기존 LobbyDepartureTests의 생성자 주입도 새 의존성에 맞췄다. 실제 GameView·마우스 조작·Event 회복 진입은 이번 EditMode 검증에 포함하지 않는다.

독립 리뷰는 신규/회귀 XML과 실제 교체 호출자를 확인하고, 승인한 Stage/Lobby 변경에 수정 필요 결함이 없다고 판정했다. SSOT(입력 번호/권위 상태 원본 유지), SOLID(기존 좁은 Sink 주입), JMH 및 FW-004(공개 경계 사용)를 확인했다. Framework 등록/Build·Input Submit·UI 이력·렌더 자원·Camera API 변경 없음. 전체 origin 인수는 다음 절의 외부 게이트를 유지한다.

### 4. Core 담당 요청과 미확인 인수

**S1:** Stage/Lobby 호출 전 취소 및 queued 옛 Job 완료 증거를 제공한다. Combat의 기존 이탈/Detach/배출 선행 조건은 용근 담당 인수가 필요하다. 새 직접 Run/챕터 교체 경로는 이 보장을 자동으로 얻지 않는다.

**S2:** queued 옛 클릭 → 같은 키 새 Run은 무거래 완료했다. 이미 시작한 정의 조회 중에는 Clear 후 새 입력 번호가 더 커지는 것까지 검증했다. 현재 Shop Core는 조회 후 옛 입력을 재검사하지 않아 아래 진단처럼 옛 결과를 게시한다. 기존 [StageCommandProcessingTests](../../../Assets/Tests/EditMode/Framework/StageCommandProcessingTests.cs)의 `CorePull_EntryInputClearedDuringSynchronousLookup_PublishesCommittedSelection` 계약도 이번 회귀에서 유지했다.

```text
[ER1 legacy Core boundary] Published=True, Number=1, Accepted=True, FreshNumber=2
```

따라서 lookup 회귀 통과는 **번호 무효화 증거**이며 옛 선택/옵션/결과 설치 방지 완료가 아니다. 그 설치 방지는 ER2 Core의 기존 RC3-B/RC4 요청이다.

**S3:** Stage 입력의 단조 Number와 불변 Kind/ChapterKey/NodeId를 보관하고, 외부 정의 조회 뒤 같은 입력·진입 전 진행 사실을 다시 읽어 설치 직전에 확인해 달라. Clear → 새 입력은 `oldNumber != newNumber`로 구별되며 같은 키/같은 그래프에서도 번호를 재사용하지 않는다. `RecoveryEntryContext`는 진입 후 문맥, `GetStages()` wrapper 참조는 origin 근거가 아니다. 모든 교체 호출자의 선행 취소 인수 없이는 전역 origin 보장으로 선언하지 않는다. 부족하면 별도 scoped token 설계·사용자 승인이 필요하며 ER1에서 임의 추가하지 않는다.

담당자의 실제 합의/외부 전달은 아직 없다. 기존 Core의 조회 후 재검사·Event 분기/원자적 설치·Factory/RC4, Combat 선행 조건 인수, View P5/P6.1·팝업/모달은 남아 있다. ER1 호진 범위 보고 후 중단한다.

### 5. 정리와 변경 범위 최종 확인

임시 Unity 검증 도구는 `cleanup` 명령의 `cleaned` 결과로 자기 폴더와 `.meta`를 제거했으며 당시 Play는 꺼져 있었다. 테스트 source/신규 `.meta`와 XML은 보존했다. 새 테스트 GUID `a8f006c97c5644d0a63c8e1f6e41a2ee`는 Assets 내 중복이 없다.

시작 snapshot의 추적 파일 7,004개를 SHA256으로 대조해 승인한 추적 파일 8개(제품 4·기존 테스트 1·문서 3)만 달라졌고, 나머지 6,996개는 같으며 누락은 0이었다. 추가 파일은 신규 테스트/`.meta`와 이 인계 문서다. Core·Framework·UI·Camera·Combat·DB·씬·프리팹·기존 `.meta` 및 FMOD 자동 생성 자산은 원본 hash를 유지했다. 수정한 source/문서의 UTF-8/LF·대체 문자 없음·로컬 링크 123개, `git diff --check` 통과와 staged diff 없음도 확인했다. 형식 정리 후 테스트 로직과 제품 동작 변경은 없었다.
