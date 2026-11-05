# StageSelect Core 교체 인계

- 작성자: 호진(이호진)

StageSelect P5/P6 Core 결과 계약 교체와 P7 아티팩트 Core 교체 인계를 모았다. 원래 문서를 부(部)로 나눠 내용 그대로 담았고, 각 부의 결정 번호와 절 번호는 원래 문서와 같다.

| 부 | 원래 문서 |
|---|---|
| [1부](#1부-stageselect-p5p6-core-결과-계약과-교체-인계) | StageSelect P5/P6 Core 결과 계약과 교체 인계 |
| [2부](#2부-stageselect-p7-아티팩트-core-구현교체-인계) | StageSelect P7 아티팩트 Core 구현·교체 인계 |

---

## 1부. StageSelect P5/P6 Core 결과 계약과 교체 인계

- 작성일: 2026-10-11
- 대상: Core 작업자·전투 실행 계층 작업자
- 권한: 사용자가 SA P5/P6의 필요한 Core·Buffer 선행 구현에 이어 H3/H4의 Core 입력 직접 조회 전환도 승인했다. **외부 전달·작업자 합의·외부 인수 완료 기록은 아니다.**
- 상태: **현재 기준은 사용자가 incoming을 선택해 완료한 병합 `9d4c76e50c044c8f4215c2f7d3e963bbc1082f07`과 그 위의 연결 보완이다.** incoming의 독립 `CombatEntryService`·진행/전환 Job을 유지한 H3/H4 연결과 새 검증은 §5·§8을 따른다. 최초 `p56-*`와 직접 조회 후속 수치는 과거 이력이며 이번 실행을 대신하지 않는다. 이전 R3(`c773625a`) 미병합 메모는 현재 지시가 아니다.
- 범위: H3/H4와 맵·상점·번호 있는 전투 소비자, 같은 등록기/Runner·공용 오류 경계 연결. H1/H2 API는 별도 [P7 인계](#2부-stageselect-p7-아티팩트-core-구현교체-인계)를 유지한다. 상점 S5의 과거 Play/Profiler 완료와 SA P8 잔여 아티팩트 Play/Profiler·Run seed R4·외부 인수는 구분한다.
- 원본: [처리 번호 요청](2026-10-06-stageselect-core-requests-Hojin.md#4부-stageselect-요청-처리-번호-기록-요청-view--core), [Core 경계 기록 §5.18](2026-09-24-view-core-data-boundary-handoff.md#518-stageselect-p5p6-core-결과-계약-2026-10-11-사용자-승인-미전달), [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획 P6.6~P6.10).

### 1. 현재 후속 API 계약

`IInGameCoreSystemGroupService.StageCommands`의 `IStageCommandProcessingService`가 요청 내용을 직접 읽는다. 네임스페이스는 `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageCommandSystem`이다. 직접 조회는 사용자가 선택한 프로젝트 계약이며 Framework/UI가 의무화한 서명이 아니다.

```csharp
bool ProcessNextShopCommand(int expectedRequestNumber);
bool ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed);
bool TryProcessNextBattleCommand(int expectedRequestNumber, out ChapterProgressionResult result);
```

- H3는 바인딩된 `ShopViewReadSink`에서 가장 오래된 미처리 입력을 읽고 구매/완료를 분기한다. 입력은 `Purchase(chapterKey, nodeId, offerId)`·`Complete(chapterKey, nodeId)`로 문맥을 필수 전달한다. 기존 명시적 chapter/node 게임 서비스에 위임하며 가격·상품 생성·지급·완료 규칙을 복제하지 않는다.
- H4 일반은 `StageViewReadSink`에서 챕터·노드를 읽는다. 기존 `ChapterProgressionCoordinator.RevalidateStageEntry`와 상점의 `CharacterShopService.EnterShop`을 사용한다. 미지원 이벤트·회복·전투/일반 역할이 아닌 상점은 `UnsupportedStageType`, 없는/도달 불가 노드·챕터 불일치·종료 Run은 기존 정상 거절을 게시한다. 잘못된 DB 정의의 예외는 정상 거절로 바꾸지 않는다.
- H4 전투도 같은 맵 입력에서 챕터·노드를 읽고 기존 `CombatProcessingService.EnterBattle(chapterKey, nodeId)`를 실행한다. `true`는 **준비 수락 또는 정상 거절 결과 게시**다. Active/씬 활성화 완료와 별개이며 공통 `ConfirmStageEntry`를 앞서 호출하지 않는다.
- 세 API의 입력 없음·기대 번호 불일치·이미 처리됨은 `false`이며 계산·게임 상태 변경·새 결과 게시가 없다. `true`를 수락과 혼동하지 않는다. 기대 번호는 이전 Job이 Clear/재연결 뒤의 새 요청을 처리하지 못하게 한다.
- `StageViewCommandData.ChapterKey`는 필수다. View는 실제 표시한 맵의 챕터를 넣고 맵 표시 전에는 제출하지 않는다. StageLifeCycle의 전투 분기도 해당 입력 챕터를 쓴다. 다른 챕터의 결과는 자기 입력만 정리하고 현재 화면을 열거나 문구를 쓰지 않는다.
- 번호 없는 Bootstrap의 `RequestEntry(chapterKey, nodeId)`는 `MapCommandNumber = 0`과 기존 `CoreGroup.Combat.EnterBattle`을 유지한다. 새 처리 서비스에 0이나 가짜 맵 번호를 전달하지 않는다.

**최초 P56 API 이력:** `EnterNode(N, chapter, node, seed)`, `EnterBattle(N, chapter, node)`, `Purchase(N, chapter, node, offer)`, `CompleteShop(N, chapter, node)`는 요청서의 (a) 번호·내용 인자 방식이었다. 현재 보완은 (b) 직접 조회로 교체하며 두 API를 병행하는 지침이 아니다. 당시 같은 최신 번호/문맥의 결과 재사용·오래된/다른 문맥 번호 예외와 §6 테스트 수치도 과거 계약의 이력이다.

### 2. 결과 저장소와 실행 수명

`IBufferQuery.StageCommandResults`는 `BufferSystems.Current.IStageCommandResultReadSink`를 제공한다.

```csharp
int HandledMapCommandNumber { get; }
int HandledShopCommandNumber { get; }
bool TryGetMapResult(out StageEntryCommandResult result);
bool TryGetShopResult(out ShopCommandResult result);
```

| 결과 구조체 | 공개 내용 |
|---|---|
| `StageEntryCommandResult` | `RequestNumber`, `ChapterKey`, `NodeId`, `Kind`(`Node`/`Battle`), `Result`(`ChapterProgressionResult`) |
| `ShopCommandResult` | `RequestNumber`, `ChapterKey`, `NodeId`, `Kind`(`Purchase`/`Complete`), `OfferId`, `PurchaseResult`, `CompletionResult`, `Accepted` |

`CurrentProgressBuffer`가 도메인별 마지막 구조체 하나씩을 소유하고 번호는 구조체의 `RequestNumber`에서 읽는다. 거절 `ChapterProgressionResult`의 chapter/node는 0일 수 있으므로 소비자는 결과 구조체의 **요청 문맥**을 사용한다. 읽기 계약과 Core 내부 `IStageCommandResultStore`를 분리하며 View·Flow·Job은 Store에 쓰지 않는다.

1. 입력 번호는 양수·도메인별 증가이며 맵/상점은 별개다. 취소 번호는 건너뛰어도 된다. Core는 현재 가장 오래된 미처리 입력이 기대 번호와 일치할 때만 실행한다. 이미 처리된 호출은 false로 끝내고 중복 구매·상점 재생성·노드 재완료를 하지 않는다.
2. 맵 일반/전투는 같은 실행 guard, 상점 구매/완료는 다른 공용 guard를 사용한다. 같은 도메인 재진입은 예외이며 더 큰 번호가 먼저 게시되는 것을 막는다. guard는 finally에서 해제하되 공용 fault가 난 같은 런타임은 재개하지 않는다.
3. 입력의 유효성 확인과 재진입 잠금 뒤 임시 `nextSeed`를 한 번 호출하고, 호출 뒤 현재 입력 번호를 재확인한다. 공급자가 입력을 Clear/교체했다면 false로 끝내며 기존 게임 서비스를 실행하지 않는다. 이 준비 경계와 동기 거래 시작 이후를 구분한다.
4. 기존 동기 Shop/Combat 서비스가 실행에 들어가 정상 반환하면 수락·정상 거절 모두 게시한다. DB 콜백이 입력을 Clear하더라도 설치된 상태의 결과를 버리지 않는다. **Clear는 실행 중 거래 롤백이나 commit 이후 취소가 아니다.** 예외 때는 새 결과를 게시하지 않고 이전 결과를 유지하며 기존 거래의 원자성을 검증한다.
5. `StageViewBuffer.Clear`·`ShopViewBuffer.Clear`·prefix 삭제는 발급 번호를 유지한다. 결과도 `CurrentProgressBuffer.Clear`·새 Run·씬 왕복에서 유지한다. **전체 Buffer 재생성 때만 입력 발급 번호와 결과 번호를 함께 0부터 시작한다.**
6. 실행 전 취소·fault·Detach의 입력 폐기는 Core 실행 결과가 아니다. 가짜 수락/거절을 게시하지 않는다. UI는 Attach 수명·자기 대기 번호·상점 문맥을 대조한다.

기존 `EnterShop`, `EnterBattle`, `Purchase`, `CompleteShop`은 동기 검증·계산·준비→설치 뒤 결과를 반환한다. 반환 뒤 불변 결과 구조체를 만들고 바인딩한 내부 Store에 필드를 설치하는 구간에는 실패 가능한 검증·DB/서비스 조회·할당·이벤트·외부 콜백을 추가하지 않는다. 입력을 다시 읽어 이미 설치된 상태의 결과만 누락시키는 경로도 만들지 않는다. 기존 `StageServiceTransactions`·`BattleTransactions`·`ProgressTransactions`의 거래 경계를 유지한다. 향후 게시에 실패 가능한 동작을 넣는다면 실패 가능한 준비를 상태 변경 앞으로 옮기거나 기존 거래의 준비→설치 안에 통합해야 한다.

### 3. 파일과 책임

아래 경로는 `Assets/ProjectL/Script/Systems/` 기준이다. 후속 구현 완료 여부는 SA 계획의 새 검증 기록을 따른다.

| 파일/영역 | 계약·교체 책임 |
|---|---|
| `RuntimeSystem/InGameSystem/CoreSystem/StageCommandSystem/StageCommandProcessingService.cs` | 입력 Read Sink 직접 조회·기대 번호·guard·기존 서비스 호출·Core 결과 게시 |
| `RuntimeSystem/BufferSystems/StageView/StageViewCommand.cs`와 맵 View | 필수 ChapterKey, 실제 표시 맵의 문맥으로 제출·오래된 챕터 결과 배제 |
| `RuntimeSystem/BufferSystems/Current/CurrentProgressBuffer.cs`, `IStageCommandResultReadSink.cs`, `StageEntryCommandResult.cs`, `ShopCommandResult.cs` | 기존 불변 결과·Read/내부 Store·번호 수명 유지 |
| `RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs`, `ServiceKeys/DataBaseGroupServiceKey.cs`(→ 바뀜: 지금은 `ServiceKeys/Runtime/Buffer/*Key.cs`로 나뉨) | 기존 입력/결과 서비스 조회·typed key |
| `Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs`, `RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs`, `ServiceKeys/SystemGroupsService.cs` | 기존 서비스 주입·StageCommands 등록/바인딩. 별도 중복 모듈 추가 없음 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs`, `ShopPlan.cs`, `Stage/Jobs/StageNodeEntryJob.cs`, `ShopCommandJob.cs` | 기대 번호·좁은 Core 인터페이스(맵 임시 seed 공급자 예외), Job payload/Sink/결과 검사 제거 |
| `CombatEntryRequest`, `CombatEntryService`, `CombatPrepareJob`, `StageLifeCycle` | 독립 EntryService의 번호·대상 검증, Prepare의 Core 미실행 내부 취소, incoming 전환 Job에 맞춘 Stage 이탈/복귀와 취소 관찰 |
| `StageNodeEntryFlow`, `ShopFlow`, `StageMapEntryObserver`, `StageMapInteraction` | 입력 소멸의 대기 해제·실제 표시 챕터·View 입력 정리 |

기존 규칙 서비스 본문에 구매/전투 규칙을 다시 구현하지 않는다. 최초 P56의 `UnsupportedStageType` enum 추가와 결과/등록 파일은 이력이며 현재 연결 파일·검증 증거는 §8과 병합 검토 §7을 따른다.

### 4. 소비자 교체 순서와 전투 경계

1. 새 직접 조회 API·입력 Read 바인딩·결과 Read 계약을 먼저 컴파일한다. 기존 번호 없는 게임 서비스는 유지한다.
2. Stage/Shop Plan이 등록 번호를 확보하고 Job에 기대 번호와 Core 인터페이스만 전달한다. 맵 임시 seed 공급자 외의 payload·입력/결과 Sink를 Job에서 제거한다. Job은 Core를 한 번 호출하고 정상 true/false 뒤 완료하며 결과 게시/대조를 하지 않는다.
3. 번호 있는 `RequestEntry`는 등록 전에 입력의 N·chapter/node가 요청 수명과 일치하는지 검사한다. `CombatPrepareJob`은 N으로 `TryProcessNextBattleCommand`를 호출한다. Core 미실행 false라면 **Prepare 전용 내부 Running → Cancelled**로 끝내고 H4를 위조하지 않는다. 일반 `TryCancel`의 Running 취소는 계속 거부한다. 번호 없는 Bootstrap은 기존 경로다.
4. 전투 수락 뒤 Prepared → Activating → Active를 유지한다. H4 수락만으로 맵 잠금을 풀지 않고 Active 또는 해당 요청이 소유한 Stage 이탈/Detach까지 기다린다. Cancelled는 `IStageEntryAttemptRead`가 같은 요청 참조를 보존하며 기존 Job 배출과 View의 N 제거를 모두 확인한다.
5. 비전투 맵·상점 Flow는 Core 처리 번호 ≥ N 또는 기대 입력 소멸로 기다림을 끝내고 새 요청을 등록한다. 같은 입력이 남은 실제 기록 누락은 해당 도메인만 정지한다. 실제 Core/Job 예외는 공용 fault다. View는 자기 결과의 문맥을 대조하고 입력을 제거한다.
6. 외부 대체 API 인수 때 입력 조회 주체·기대 번호·필수 챕터·false/true 의미·게시 시점·전투 수명을 먼저 비교한다. Job/Plan·Core 바인딩·전투/View 소비자를 같은 변경에서 교체하고 관련 회귀를 실행한 뒤 facade 제거 여부를 결정한다. 반환값만 제공하는 API를 Job의 대리 게시로 호환시키지 않는다.

임시 seed 제공자는 시스템별 `JobRegisterSystem`의 `readonly Func<int> _nextShopSeed = new Random().Next`다. static StagePlan은 이를 Job에 전달하고 **Core가 유효 입력 확인·잠금 뒤** 한 번 호출한다. 테스트는 고정 함수를 주입한다. [Run seed R4](2026-10-06-stageselect-core-requests-Hojin.md#3부-run-seed-도입-요청-view--core) 인수 완료가 아니며 View의 Run seed 혼합이나 static Plan 난수 상태를 추가하지 않는다.

### 5. 병합된 전투 진행 구조와 연결 계약

과거에는 `origin/feature/Yonggeun/Stage`의 `c773625a`를 읽기 검토하고 미병합으로 기록했다. 현재는 사용자가 incoming `c1b9b0a3`를 선택한 병합 `9d4c76e5`가 기준이며, **독립 `CombatEntryService`·`CombatPlan.Decide`·`Next`/`ExitCheck`·전환 Job 경로가 실제 운영 구조**다. 이전 `CombatFlow`·`CombatExitSequence`·삭제된 `ILifeCycleTransitionGuard`를 복원하지 않는다.

- `CombatEntryService`가 RequestEntry/Current의 유일한 제공자다. 양수 맵 N과 첫 미처리 입력의 번호·chapter/node를 등록 전에 확인하고 Queued 요청 재사용에도 세 값을 비교한다. 번호 0 Bootstrap을 같은 서비스에서 분기한다.
- 같은 `CombatPrepareJob`이 양수 N → `StageCommands.TryProcessNextBattleCommand`, 번호 없음 → 기존 Combat API로 실행한다. Core 미실행 false는 내부 `CancelUnprocessed`와 Job 완료이며 가짜 H4 게시나 직접 Combat API fallback이 없다. 다음 Transition Job은 Rejected/Cancelled면 전환하지 않고 완료한다.
- `CombatEntryTransitionJob.Execute`는 Runner의 현재 Job인 동안 BeginActivation → Manager.Change(Combat) → Load를 호출한다. **Stage -200은 같은 Flow 요청·양수 N·EntryService.Current 참조·Accepted·준비 Job 완료·Activating·Manager Combat을 모두 확인했을 때만 busy 이탈을 허용**한다. 이 검사가 View Detach(-100)와 LifeCycle Exit(0)보다 먼저다. 무관한 busy·fault 정상 전환은 거부하며 거절 전에 Flow/입력을 정리하지 않는다.
- Combat 씬 Enter에서 `CombatLifeCycle`이 EntryService.ActivateAccepted를 동기로 호출한다. 여기서 현재 선택까지 검증해 Active로 만든다. H4 준비 수락과 Active는 별개이며 활성화 fault 뒤 H4를 지우거나 Core 준비를 다시 실행하지 않는다.
- 같은 Stage 취소 관찰은 `IStageEntryAttemptRead` → `LifeCycles.StageEntryAttempts`로 유지한다. Current만으로 대체하지 않고 View의 N 제거와 취소 Job 배출까지 같은 요청 참조를 보존한다.
- 전투 종료 후 Stage 로드 완료가 32개 Job 예산 뒤의 ExitCheck보다 빠르면, 같은 Current의 Active·Accepted·준비 Job 완료·전투 없음·Manager Stage를 확인해 Stage 준비만 보류한다. 같은 Current가 Closed이고 전투 없음·Runner idle인 뒤 기존 준비 Job/Reset을 한 번 실행한다. 무관한 busy 최초 진입은 거부하고 fault/Shutdown 뒤에는 등록하지 않는다.
- `LifeCycleManager.Exit` 거절 시 기존 active를 복구한다. 그룹의 역순 Shutdown에서 이미 정리한 자식에는 다시 Exit/Unload를 요청하지 않고 일반 모듈의 Exit fallback은 유지한다. EntryService 종료는 이미 끝난 Cancelled/Rejected 의미를 보존하고 참조를 해제한다.

`JobRegisterSystem`의 기존 Stage seed·Artifact Buffer·Shop Plan 연결과 Core typed key/StageEntryAttempts projection을 보완한다. 별도 Runner나 같은 입력의 소비자를 추가하지 않는다. `LifeCycleSystemFactory`는 `StageLifeCycle(faults)`로 공용 오류 경계를 넘긴다. 이 연결 수명에 대한 새 검증은 §8을 따르며 과거 P56 통과를 병합 통과로 확대하지 않는다.

### 6. 최초 P5/P6 검증 기록 (직접 조회 보완 전 이력)

- 테스트 먼저: `Assets/Tests/EditMode/Framework/StageCommandProcessingTests.cs`의 실제 `ShopStageServiceTests` partial fixture 18건. 실제 `BufferSystemFactory`·`InGameCoreSystemFactory`·`FrameworkRuntime`을 조립하며 DB 조회 경계에서만 오류/재진입을 주입한다.
- Core 검증 범위: 진입·구매·완료 수락/거절, 미지원/없는/도달 불가 노드, 입력 미소비, 같은 N 중복 실행 방지, 오래된/다른 문맥 N, 예외의 결과 미게시, 맵·상점 재진입 guard, 새 Run/전체 Buffer 수명.
- 전투 통합 검증: `CombatEntryIntegrationTests`에서 실제 Runtime/LifeCycle/Register/Plan/Runner/Core/Buffer로 맵 번호와 수락→Active, 취소·fault·Bootstrap을 확인한다. 로딩은 테스트 대역이며 운영 Unity 씬 인수와 구분한다.
- RED: `Logs/StageSelectSAVerification/p56-first-red-unsandboxed.xml` — 188건 중 142 통과·46 실패. 새 Core 18건 모두 API 골격의 `NotImplementedException` 또는 예상 예외 불일치로 실패하는 것을 확인한 뒤 구현했다. 이 숫자는 완료 검증이 아니다.
- 최초 P56 최종: `Logs/StageSelectSAVerification/p56-all.xml` **3702건 / 3667 통과 / 34 기존 실패 / 1 기존 판정 불가 / 0 skip**, Unity exit 2. P3.10 기준선 실패 이름과 비교한 추가/해소는 0건이다. 관련 414건은 413 통과·0 실패·기존 카메라 판정 불가 1건이며 위 Core 18건과 새 소비자/전투/UI 회귀를 포함한다. 전부 통과한 전체 테스트라고 표현하지 않는다.
- 실제 ShopUI·카드 프리팹·StageSelect 참조 검증 3건도 통과했다. 에디터 저장 전후 기존 씬 85개 오브젝트 ID와 아티팩트 배경의 실제 Transform 보존을 별도 확인했다. 상점 맵 등록·사용자 Play·Profiler는 S5에 남아 있다.
- Unity 종료 뒤 순차 MSBuild `ProjectL.Tests.Framework.csproj /restore /t:Build /v:minimal /nologo` exit 0(`p56-msbuild.log`). 새 소스 포함/삭제 소스 제외를 각 생성 프로젝트에서 확인했으며 Framework 기반 구현은 수정하지 않았다.

### 7. 직접 조회 후속 검증 (사용자 병합 전 이력)

SA 계획 P6.6~P6.9의 직접 조회 API·필수 챕터·입력 취소/교체·seed 취소, 상점 수락/거절/중복, DB 예외와 재진입, 동기 서비스 실행 중 Clear 뒤 결과 게시, Flow의 새 요청 재개, 실제 번호 있는 전투 대상 검증·미실행 취소·Prepared→Active·거절·씬 배출 후 재진입, View 표시 챕터·오래된 결과 회귀를 완료했다. §1~4가 현재 교체 계약이며 Job의 대리 게시나 번호·내용 API 병행으로 되돌리지 않는다.

증거 위치는 `Logs/StageSelectSAVerification/`이며 XML과 같은 이름의 실행 로그를 함께 보관한다.

| 실행 | 확인된 결과 |
|---|---|
| `stage-shop-pull-baseline-native.xml` | 277건 / 276 통과 / 실패 0 / 기존 판정 불가 1 / skip 0 |
| `stage-shop-pull-red.xml` | 27건 / 10 통과 / 기대 실패 17 / 판정 불가 0 / skip 0 |
| `stage-shop-pull-related.xml` | **365건 / 364 통과 / 실패 0 / 기존 판정 불가 1 / skip 0** |
| `stage-shop-pull-all.xml` | **3769건 / 3734 통과 / 기존 실패 34 / 기존 판정 불가 1 / skip 0** |

전체 실패 fullname 집합은 `p7-pull-final-all.xml`과 정확히 같아 **추가 실패 0·해소 0**이다. Unity exit 2와 남아 있는 실패/판정 불가를 보존하며 전체 통과라고 표현하지 않는다. §6은 최초 번호·내용 API의 과거 이력으로 이번 직접 조회 검증과 구분한다.

P6.10까지 로컬 구현·자동 검증을 완료했다. Unity 종료 뒤 순차 `MSBuild ProjectL.Tests.Framework.csproj /restore /t:Build /v:minimal /nologo` **exit 0**이며 증거는 `stage-shop-pull-msbuild.log`다. 새 문맥 독립 검토 `stage_shop_pull_final_review`는 **Critical 0 / Important 0**이며 유일한 Minor 파일명 오타를 `StageViewCommand.cs`로 수정했다. 최종 전체 테스트·빌드 뒤 운영 코드·테스트 변경은 없다. 변경은 기존 파일 36개(운영 15·테스트 13·문서 8), 추가/삭제/meta 변경 0개이며 인코딩·공백·`git diff --check`를 확인했다. 외부 Core/전투/Framework/UI 전달·합의, 수동 Play/Profiler·S5·R4·P8/P9도 별도 미완료 상태를 유지한다.

### 8. 사용자 병합 후 연결 보완·검증 (2026-10-11)

이번 검사 대상은 `9d4c76e5`와 작업 트리의 운영 8개·테스트 9개 C# 보완이다. 새 기준선 `merged-baseline.log`는 **exit 1**, 3인자 진입 인터페이스 미구현(CS0535)과 삭제된 guard 참조(CS0246/CS0538)로 컴파일되지 않았다. 따라서 아래 새 전체 결과의 비교 대상은 컴파일 가능한 이전 실행이며, 병합 HEAD에서 새 전체 기준선이 통과했다고 쓰지 않는다.

증거 위치: `.superpowers/sdd/2026-10-11-post-user-merge/`. 전체 판정과 파일 목록의 원본은 [스테이지 맵 계획 §4.2](../plans/2026-10-10-stage-map-plan-Hojin.md#42-병합-후-연결-보완)(옛 병합 검토 §7)·[§3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획)이다.

| 새 실행 | 결과 |
|---|---|
| 공유 오류 조립 RED | 새 회귀 1건 실패. Stage가 공용 fault에서 멈추지 않고 보드 설정 검사까지 도달함을 재현 |
| `repaired-all.xml` | **4037건 / 4001 통과 / 실패 34 / 판정 불가 1 / skip 1**, native exit 2. 공유 오류 조립 회귀는 통과 |
| 위 전체 실행의 관련 회귀 부분집합 | **612건 / 611 통과 / 실패 0 / 기존 판정 불가 1**. 별도 실행으로 합산하지 않음 |
| `repaired-play.xml` | **4/4 통과**, native exit 0. 상점 1·아티팩트 2·벽 공격 1, 판정 불가/skip 0 |

`verification-summary.json`은 전체 실패의 `(fullname, message)`와 중복 개수를 보존해 `actual-main-all.xml` 및 `Logs/ShopS53aVerification/s53a-all.xml`과 비교한다. 실패 34건이 정확히 같아 **추가 실패 0**이며 전체 통과는 아니다. 기존 번호 조회·수락/거절·취소·실제 등록기·동기 이탈·빠른 복귀/32개 예산·UI/상점/아티팩트 회귀를 새 결과와 구분해 기록했다.

단일 실행 소유는 Bootstrap → FrameworkRuntime → 기존 Global Runner/등록기, 단일 오류 정리 연결은 View 그룹의 `InGameViewFaultCleanup`이다. Core 도메인 계산과 결과 게시, View 입력 제거는 기존 소유자에 남는다. `ArtifactEditingService`의 서로 다른 두 인터페이스를 같은 객체로 제공하는 FW-001 등록을 중복 실행으로 오인하지 않는다. Core 계산·Framework 기반·UI·씬·에셋·GridDecal·타 작업자 문서는 HEAD의 선택을 보존했다. 외부 전달·합의·커밋·push는 수행하지 않았고 사용자 검토/커밋을 위한 작업 트리 상태다.

---

## 2부. StageSelect P7 아티팩트 Core 구현·교체 인계

> **2026-10-13 상태(LV5):** 아래 StageLifeCycle·StageFlow 연결 설명은 LV5 전 기록이다. 지금은 `LobbyLifeCycle`이 `ArtifactEditFlow`를 연결하고 편집은 로비에서만 연다. Core 서비스·기대 번호 계약은 그대로다. 또 §3의 3번·7번 "해당 Flow만 정지"와 달리 로비에서는 기록 누락을 공용 오류(전체 정지)로 보고한다. [View 구조 설계 §6](../specs/2026-10-13-view-structure-design-Hojin.md#6-기록-누락-처리-정책-d8d11d12)(옛 비전투 View 구조 개선 설계 §6 D11)에서 "그 도메인만 정지 + 이탈 허용"으로 정했다(2026-10-13, 사용자 결정). 현재 기준은 [로비 계획 §3.9 LV5](../plans/2026-10-11-lobby-plan-Hojin.md#39-lv5-아티팩트-로비-이전)(옛 §8·§14.9)다.

- 작성: 2026-10-11, View 작업자(호진) 측 로컬 작업
- 근거: 사용자가 P7과 필요한 Core 서비스·Buffer의 선행 구현을 직접 승인했다. 외부 작업자에게 전달하거나 합의한 기록은 없다.
- 범위: SA 계획 P7.0~P7.4와 P7.5~P7.9의 Core 직접 조회 보완(사용자 추가 승인, 로컬 구현·자동 검증 완료). 맵·상점 H3/H4의 이전 변경은 [P5/P6 인계](#1부-stageselect-p5p6-core-결과-계약과-교체-인계)를 따른다.
- 원본: [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)(옛 SA 설계), [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획), [H1~H4 요청서](2026-10-06-stageselect-core-requests-Hojin.md#4부-stageselect-요청-처리-번호-기록-요청-view--core).
- 현행 기준: 병합 `9d4c76e5`와 그 위의 연결 보완. P7의 Core 직접 조회·기대 번호·UI 수명 계약은 유지한다. 공용 등록기와 전투 전환 연결은 [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)(옛 SA 설계 §0.4), 현재 자동 검증은 [스테이지 맵 계획 §4.2](../plans/2026-10-10-stage-map-plan-Hojin.md#42-병합-후-연결-보완)(옛 병합 검토 §7)를 따른다. 아래 §4·§6의 P7 변경·수치는 당시 이력이며 이번 문서 갱신에서 다시 실행한 결과가 아니다.

### 1. 현재 경로와 책임

`ArtifactInputLayer → ArtifactDragController → ArtifactViewBuffer → StageLifeCycle의 StageFlow/ArtifactEditFlow → JobRegisterSystem → ArtifactPlan → ArtifactCommandJob/ArtifactPreviewJob → CoreGroup.ArtifactCommands → ArtifactBuffer → View`

Stage·Shop·Artifact·Combat의 등록은 같은 `JobRegisterSystem`과 `JobRunnerSystem`을 사용한다. 병합 후 보완은 기존 ArtifactPlan에 Buffer를 전달하는 연결과 `CoreGroup.ArtifactCommands` 조회를 복구했으며 아티팩트 Scheduler나 별도 Core 인스턴스를 추가하지 않았다. 전체 호출 순서는 [View 구조 설계 §9](../specs/2026-10-13-view-structure-design-Hojin.md#9-흐름-시퀀스)(옛 시퀀스 UML)을 따른다.

| 경계 | 맡는 일 |
|---|---|
| UI | 기존 UIInputBinding으로 열기·닫기를 UISystem에 요청. OpenStateChanged를 동기로 알리고 전달받은 표시 값만 그린다. UI의 Buffer·DB 폴링은 추가하지 않았다 |
| DragController | 실제 선택·회전·좌표를 입력 Buffer에 제출. 미정리 편집이 있으면 같은 Tick의 추가 편집·프리뷰를 막는다. 현재 번호와 문맥이 맞는 결과를 표시하고 편집 입력을 제거한다 |
| Session | 화면·입력 슬롯·카메라·자기 Layer·입력 Buffer의 연결 수명. 숨김은 프리뷰만 지우며 제출된 편집을 유지한다. 숨김 중 편집 완료는 화면을 열지 않고 문구를 보관·입력을 정리한다 |
| Refresher / Status | Refresher는 스냅샷과 View DB를 표시 값으로 변환. Status는 대기 > 프리뷰 실패 > 마지막 편집 결과 순서의 문구를 보관한다 |
| Flow / Plan / Job | Flow는 읽기와 등록 순서만 조정. static Plan이 등록 대상의 기대 번호와 좁은 Core 인터페이스만 struct Job에 전달한다. Job은 Core를 한 번 호출하고 정상 반환하면 완료한다. 요청 내용·입력 Sink·결과 Sink를 보관하지 않는다 |
| Core / Buffer | Core가 바인딩된 입력 Read Sink에서 요청을 직접 조회하고 기대 번호·처리 여부를 검사한다. 기존 계산 포트가 게임 규칙을 판단하고 Core가 번호·요청 내용·완성된 제안을 결과 Buffer에 함께 게시한다. 입력 삭제는 View가 맡는다 |

Framework 기반 클래스, UISystem/UIBase/UIInputBinding, 전투 실행 코드는 이번 P7에서 변경하지 않았다. CombatPlanTests의 Core 가짜에는 새 서비스 접근자를 추가했다. Core 서비스는 기존 ArtifactEditingService가 추가 제공 인터페이스를 등록하므로 Factory에 별도 모듈을 중복 등록하지 않는다.

### 2. 실제 H1·H2 계약

현재 선택은 **(b) Core가 입력 Buffer에서 요청을 직접 조회**하는 방식이다. 사용자가 전투와 요청 조회 주체를 맞추도록 추가 승인했다. 최초 P7의 `ProcessCommand(N, data)` / `ProcessPreview(N, data)`는 교체 대상인 과거 계약이며 병행 API로 유지하지 않는다. 이번 보완의 실행·회귀 결과는 §6의 별도 기록을 따른다.

```csharp
bool ProcessNextCommand(int expectedRequestNumber);
bool ProcessLatestPreview(int expectedRequestNumber);
```

위 두 함수는 기존 `IArtifactCommandProcessingService : IServiceModule`의 계약이다. 같은 partial `ArtifactEditingService`가 제공하며 `CoreGroup.ArtifactCommands`로 호출한다. Core는 바인딩한 `IArtifactViewBufferReadSink`에서 편집의 다음 요청 또는 최신 프리뷰를 직접 읽는다. H1은 Place/Move/Return, H2는 Place/Move이다. `expectedRequestNumber`는 Plan이 등록할 때 확인한 양수 번호이며 요청 내용은 전달하지 않는다. ArtifactId·InstanceId는 기존 Core 계약처럼 **0도 유효**하고 음수는 거부한다.

- `true`: 현재 기대 번호의 요청을 처리하여 번호·내용·결과를 Core 결과 Buffer에 게시했다. 정상 거절과 H2의 정상 건너뜀도 포함한다.
- `false`: 입력이 없거나 현재 번호가 기대 번호와 다르거나 이미 처리했다. 진입 시 이 조건이면 계산하지 않고, 계산 중 취소·교체를 확인했다면 만들어진 제안을 게시하지 않는다. 어느 경우든 게임 상태·결과 번호를 변경하지 않으며 Job은 `false`도 정상 완료로 취급한다.
- 계산 전과 결과 게시 직전에 현재 입력 번호를 확인한다. 계산 중 취소·교체된 요청의 제안은 버리고 번호를 진행시키지 않는다. 잘못된 인자·내부 재진입·DB/계산/게시 예외는 정상 건너뜀과 구분하여 기존 공용 오류 경계로 전달한다.

전투와 **요청 조회·계산·결과 게시를 Core가 맡는 점**을 맞췄다. 전투의 무인자 처리 함수와 달리 기대 번호를 인자로 유지한 것은 아티팩트 연결 수명을 보호하기 위한 프로젝트 결정이다. 같은 Stage 안의 Session Detach→Attach는 정상 씬 이탈의 Runner 배출 가드를 지나지 않을 수 있어, 등록된 이전 Job이 새 연결의 요청을 실행하지 않도록 번호를 고정한다. Framework가 요구하는 필수 함수 형태라고 해석하지 않는다. FW-101의 좁은 의존성 권장은 Job에 Core 인터페이스만 전달하는 것으로 적용한다. UI 문서의 표시·입력·수명 계약은 이 호출 형태를 지정하지 않는다.

`IBufferQuery.ArtifactCommandResults : IArtifactCommandResultReadSink`:

| 읽기 값 | 의미 |
|---|---|
| HandledCommandNumber / LastCommand | 마지막 H1 번호, 원래 의도, ArtifactActionResult |
| AnsweredPreviewNumber / LastAnsweredPreview | 마지막 H2 번호, 원래 의도, ArtifactPlacementPreview 또는 IsSkipped |

- 성공과 정상 거절은 모두 해당 번호를 게시한다. 예: LayoutLocked, InstanceNotFound, OutOfBounds.
- 현재 유효한 이동 프리뷰의 대상이 배치 목록에 없으면 Core가 `IsSkipped == true`인 응답을 게시한다. 잘못된 계산기의 null 반환을 정상 생략으로 처리하지 않는다.
- DB·계산·게시 검증 예외는 결과 번호를 진행시키지 않는다. ArtifactBuffer는 기존 PublishEdit의 검증·스냅샷 복사를 완료한 뒤 번호 결과를 값으로 대입한다. 상태 설치 뒤 추가 조회·검증·할당을 하지 않는다.
- 이미 처리한 번호의 재호출은 `false`이며 기존 결과를 유지한다. 호출자가 요청 내용을 바꾸어 넘기는 경로는 없고, 입력 Buffer의 발급 번호를 재사용하지 않는다. H1/H2 내부 재진입은 거부한다.
- 기존 번호 없는 IArtifactEditingService와 LastEditResult/LastPreview는 보드 초기화·기존 Core 소비자의 호환을 위해 유지한다. **운영 팝업은 번호 없는 결과로 완료를 판단하지 않는다.** 편집이 legacy LastPreview를 비워도 마지막 H2 번호 영수증은 유지한다.

### 3. 취소·숨김·재진입 수명

1. 입력 명령과 프리뷰는 ArtifactViewBuffer의 한 발급기를 공유하며, H1/H2 완료 번호는 각각 기록한다. 입력 Clear·새 Run·씬 왕복·Core 서비스 재조립만으로 발급/완료 번호를 0으로 돌리지 않는다. 전체 Buffer 구성을 새로 만들 때 함께 초기화한다. 기존 아티팩트 보드의 Run 간 유지 정책은 변경하지 않았다.
2. Job은 등록 시 기대 번호만 캡처한다. Core가 실행 시 현재 입력과 대조하여 없음/교체/이미 처리됨이면 false를 반환한다. 이때 가짜 처리 결과를 만들지 않는다. 같은 Stage에서 Detach→Attach 뒤 새 입력이 생겨도 이전 Job은 새 번호를 처리하지 않으며, 새 등록이 해당 번호를 처리한다. 계산 중 취소·교체도 게시 직전 재확인으로 제외한다.
3. ArtifactEditFlow의 편집 기다림은 처리 번호 ≥ N 또는 입력 목록에 기대 편집 N이 더 이상 없으면 끝난다. 같은 Stage의 Detach→Attach에서 이전 Job이 false로 끝나도 새 요청을 등록할 수 있게 하는 조건이다. 같은 N이 여전히 남고 처리 기록이 없으면 기존대로 해당 Flow만 정지한다. 프리뷰 기다림은 응답 번호 ≥ N, 슬롯 없음, 슬롯 번호 변경 중 하나로 끝난다. 표시에는 현재 슬롯·드래그·응답 번호가 정확히 일치해야 한다. 완료 조건과 표시 조건을 합치지 않는다.
4. 닫기/회전/드래그 취소는 프리뷰를 지운다. 닫기는 제출된 편집을 지우지 않는다. 같은 연결에서 재열면 마지막 편집 문구를 유지하는 기존 동작도 보존한다.
5. 정상 씬 이탈은 StageLifeCycle의 -200 준비 노드가 View의 -100 Detach보다 먼저 검사한다. 일반 busy 이탈은 거부하되, 현재 병합 구조에서는 동일 Flow 요청·양수 맵 번호·EntryService.Current 참조·준비 수락·준비 Job 완료·Activating·Manager의 Combat 목적이 모두 일치하는 전환 Job의 동기 이탈만 허용한다. 자세한 조건은 SA 설계 §0.4가 원본이다. 동기 콜백에서 기다리거나 공용 Runner.Clear를 호출하지 않으며, 허용된 이탈에서 Session이 자기 입력을 비운다.
6. fault/Shutdown에서는 추가 Tick·재실행을 전제로 하지 않는다. Session은 구독·슬롯·드래그·자기 Layer·입력을 가능한 한 모두 정리하며 예외를 모은다. DragController.Stop은 이전 수신자를 영구 종료해 재연결 뒤 새 입력을 지우지 못하게 한다. ArtifactViewSystem의 중복 입력 삭제 경로는 제거했다.
7. Job/Core 예외는 기존 공용 RuntimeFaultBoundary 범위다. Job은 결과 Sink를 읽어 게시 계약을 검사하지 않는다. Flow가 정상 완료 뒤에도 기대 입력이 남은 실제 번호 누락을 진단하는 경우는 해당 Flow만 정지한다(SA §2.4). 맵/상점의 현재 요청 조회·결과 게시는 Core가 맡으며 Stage/ShopFlow는 완료 번호와 입력 수명을 읽는다([P56 Core 인계](#1부-stageselect-p5p6-core-결과-계약과-교체-인계)). 공용 경계의 연결·해제는 P3.9의 `InGameViewFaultCleanup` 한 곳에서 유지한다.

### 4. 변경 파일

아래 경로의 기준은 `Assets/ProjectL/Script/Systems/`이다.

| 영역 | 파일·변경 |
|---|---|
| Core | `RuntimeSystem/InGameSystem/CoreSystem/ArtifactSystem/ArtifactCommandProcessing.cs`는 같은 partial ArtifactEditingService의 번호 처리 구현. 이번 보완에서 요청 내용 인자를 제거하고 입력 직접 조회·기대 번호·게시 직전 재확인을 맡는다. `ArtifactEditingService.cs`는 기존 추가 서비스 export·결과 Sink에 입력 Read Sink 바인딩을 더한다 |
| 결과 Buffer | `RuntimeSystem/BufferSystems/Artifact/ArtifactCommandResults.cs` 추가, `ArtifactBuffer.cs`에 원자적 편집/번호 게시와 프리뷰/skip 게시 |
| 등록 | `InGameCoreSystemGroup.cs`, `BufferSystems/Core/BufferSystemGroup.cs`, `ServiceKeys/SystemGroupsService.cs`, `DataBaseGroupServiceKey.cs`에 좁은 서비스 키·접근자 |
| 입력 | `ArtifactViewCommand.cs`의 ID 0 허용, 명령/프리뷰 데이터의 값 문맥 비교 |
| 실행 | ArtifactPlan이 기대 번호와 Core 인터페이스만 두 Job에 전달한다. Job의 요청 내용·입력/결과 Sink 의존성을 제거한다. 기존 JobRegisterSystem의 Plan Buffer 전달과 StageLifeCycle 연결은 유지한다. ArtifactEditFlow는 기대 편집 입력이 없어진 경우 취소로 기다림을 끝내며 새 입력을 등록한다 |
| View | ArtifactDragController·Interaction·Session·ViewSystem·Status·Text·BoardRefresher의 요청 객체 의존 제거와 번호 결과 소비 |
| 제거 | ArtifactEditScheduler, ArtifactPreviewSlot, ArtifactEditRequest(프리뷰 요청/상태 포함), ArtifactEditJob, CreateEditJob/CreatePreviewJob. 기존 ArtifactPreviewJob 본문은 새 번호 Job으로 교체 |
| 테스트 | 기존 UI·Session·View·Interaction·Boot 테스트를 입력/결과 계약으로 이전. 옛 Scheduler 테스트는 새 Core/Plan/Job·입력 연타·취소/교체 테스트로 대체. Core/Buffer 가짜에 새 제공 인터페이스 반영 |

삭제된 파일의 `.meta`도 제거했다. 새 파일에는 `.meta`를 추가했고 유지된 파일의 GUID는 변경하지 않았다. 최초 P7 파일 목록과 당시 이전 파일 사본은 `.superpowers/sdd/2026-10-10-stageselect-structure-alignment-plan/p7-files.json`, `p7-before/`에 보관한다.

### 5. Core 작업자가 교체하는 순서

1. 위 Core 직접 조회 API·결과 형태를 유지할지, 다른 Core API로 바꿀지 결정한다. 입력 Read Sink 바인딩, 기대 번호 검사, 계산 전/게시 전 취소·교체 검사와 이미 처리한 번호의 false 계약을 함께 인수한다. 게임 규칙은 기존 IArtifactEditCalculation을 계속 사용하고 번호 게시의 원본을 View/Job으로 옮기지 않는다.
2. 같은 API라면 번호 처리 partial과 내부 결과 Store 구현만 대체한다. ArtifactEditingService가 두 인터페이스를 한 모듈에서 제공하는 등록을 유지하거나, 분리할 경우 제공 인터페이스·모듈을 각각 한 번만 등록한다.
3. API/결과 형태를 바꾸면 **Core 등록·Buffer 키/접근자 → ArtifactPlan/두 Job → StageLifeCycle의 Flow 읽기 → DragController/Status 결과 소비**를 한 변경 단위로 맞춘다. static Plan에는 처리 번호나 최신 요청 상태를 두지 않는다.
4. 기대 번호 인자를 없애고 전투와 같은 무인자 API로 바꾸려면, 같은 Stage의 Detach→Attach에도 기존 Job이 새 요청을 처리하지 못하는 동등한 수명 보장을 먼저 마련한다. StageLifeCycle의 이탈 준비 검사만으로 대체하지 않는다. 제거된 `ILifeCycleTransitionGuard`를 복원하는 지침도 아니다. 현재 입력과 관계없는 이전 결과 표시나 취소 번호의 처리 기록을 만들지 않는다.
5. 번호 저장소를 새 Run마다 초기화하는 정책을 원하면 입력 발급기와 함께 바꾸고 등록된 Job·이전 응답이 새 수명에 섞이지 않는 식별 계약과 테스트를 먼저 마련한다. 결과 번호만 초기화하거나 입력 번호만 재사용하지 않는다.
6. 관련·실제 Boot·공용 오류·이탈/재진입 테스트와 전체 기준선 비교, Unity 뒤 순차 MSBuild를 실행한다. 사용처가 없어진 로컬 구현만 제거한다. 옛 Scheduler/요청 객체 경로를 병행 복구하지 않는다.

### 6. 검증과 남은 범위

아래는 **최초 P7 구현의 자동 검증 이력**이다. Core 직접 조회로 바꾼 이번 보완의 최종 증거는 다음 「Core 직접 조회 보완」 절에 별도로 기록한다. 아래 과거 통과를 새 API의 통과 증거로 사용하지 않는다. 최초 증거는 `Logs/StageSelectSAVerification/p7-*`에 보관한다. 아래 전체 실행과 검토 후 추가 테스트의 실행 시점을 구분한다.

- RED: `p7-core-red`에서 새 계약 미구현과 ID 0 거부 6건, `p7-plan-red`에서 기존 Plan 미지원 4건을 확인했다.
- Core GREEN: `p7-core-green`의 기존/새 편집·Buffer 관련 66건이 통과했다.
- 수명 RED: `p7-lifetime-red`에서 이전 수신자가 새 연결 프리뷰를 지우는 문제를 재현한 뒤 Stop으로 보완했다.
- P8의 사용자 Play·연속 프리뷰 등록 Profiler, P9 문서 통합, 상점 S5·Run seed R4, 외부 Core/Framework/UI 인수는 별도다. 이번 자동 Boot/고정 후보 할당 검증을 이 항목들의 완료로 해석하지 않는다.

- 전체 `p7-all.xml`: **3712건 / 3677 통과 / 34 실패 / 기존 판정 불가 1 / 0 skip**, Unity exit 2. 실패 34개의 전체 이름을 `p56-all.xml`과 비교해 일치함을 확인했다. **새 실패 0**이며 전체 통과라고 표현하지 않는다.
- 전체 실행 중 관련 499건: **498 통과 / 기존 실패 1**. 기존 실패는 ArtifactPopupConfigTests.ActualViewDatabase_ProvidesLayoutAndModelsAndDescriptions의 배치값 기대 2.2 / 실제 1.79이며 변경 전 p7-baseline에서도 재현했다.
- 초기 관련 실행 `p7-related.xml`은 499건 / 497 통과 / 2 실패였다. 이 중 LatePreviewAfterClose_IsNotDisplayed는 테스트에서 입력 슬롯 바인딩 뒤 수신자를 교체한 fixture 오류였으며, 연결 전에 설정하도록 고친 뒤 위 전체 실행에서 통과했다.
- 독립 검토 후 숨김 완료 테스트 1개를 추가한 최종 `p7-related-final.xml`은 **500건 / 499 통과 / 기존 배치값 실패 1**, Unity exit 2다. 숨김 중 정상 거절의 입력 제거·화면 쓰기 없음·재열기 문구 복원과 LatePreviewAfterClose_IsNotDisplayed 모두 통과했다. 이 추가 테스트는 앞선 전체 3712건에 포함되지 않는다. 전체 실행 뒤 운영 코드는 변경하지 않았다.
- 실제 Boot/StageSelect 2건은 통과했다. Framework 입력 → 새 Buffer/Flow/Job/Core → 프리뷰·배치·회전·이동·반환과 고정 후보 Tick의 GC 0 검사를 포함한다. 정상 이탈 가드·취소 Job의 새 입력 무시, 닫기 후/교체 후 늦은 프리뷰 배제, 숨김 편집 보존, 공용 fault 즉시 정리와 이전 수신자 종료도 관련 회귀로 확인했다.
- 옛 Scheduler의 17개 테스트는 제거한 요청 객체의 상태 머신 대신 Core/Plan/Job·입력 잠금·UI 수명에 대한 새 28개 사례로 대체했다. 기존 View/UI/Interaction/Boot 동작 테스트는 새 계약으로 이전했다.
- 독립 검토에서 Critical/Important 코드 결함은 없었다. 비차단 지적은 설계 §2.6의 W1 과거 기록 명시, 위 숨김 완료 테스트 추가, 초기/최종 검증 기록 구분으로 반영했다.
- 빌드: Unity 종료 뒤 순차 MSBuild ProjectL.Tests.Framework.csproj /restore /t:Build /v:minimal /nologo 종료 코드 **0**. 마지막 보강 테스트까지 포함한 p7-msbuild-final.log에서 Systems와 Tests 어셈블리 컴파일을 확인했다. 기존 경고는 남아 있다.
- 파일 확인: 이번 P7 변경은 소스·테스트·meta 52개와 문서 7개다. 새 meta GUID 6개는 각각 한 번만 존재하고 기존 meta 수정은 없다. 삭제 타입/생성 함수의 C# 참조 0건, 새 소스의 생성 csproj 포함 확인, 새 trailing whitespace 0건, 소스·문서 CRLF를 확인했다. 기존 공유 파일의 공백은 보존했다. 커밋·push·외부 전달은 하지 않았다.

#### Core 직접 조회 보완 — 자동 검증 완료

사용자가 P7 완료 뒤 전투와 같은 요청 조회 주체로 통일하도록 승인했다. H3/H4 맵·상점 API는 당시 P7 보완의 대상이 아니었으며, 이후 승인된 직접 조회 전환과 현재 계약은 [P56 Core 인계 §7](#7-직접-조회-후속-검증-사용자-병합-전-이력)을 따른다. 다음 실제 경로와 회귀를 확인하여 SA 계획 P7.5~P7.9를 완료 처리했다.

- Core가 실제 입력 Buffer에서 선택한 값으로 계산하고 정상 성공·거절·미배치 이동 프리뷰 건너뜀의 번호/결과를 게시한다.
- 등록 뒤 취소·교체·입력 Clear와 같은 Stage 재연결 뒤 이전 Job은 새 요청을 실행하지 않는다. 취소·교체가 계산 중 발생해도 상태/번호를 게시하지 않는다.
- 같은 번호 재호출은 새 계산/게시가 없고, 새 요청은 이전 기다림을 끝낸 Flow가 새 Job을 등록하여 처리한다. 기대 입력이 남은 번호 누락은 도메인 정지를 유지한다. 예외 시 번호 불변·공용 오류 정지, 입력 삭제의 View 소유와 숨김/재열기 동작을 보존한다.
- 관련 Core/Plan/Job·Session·Flow·공용 오류·실제 Boot 입력 회귀, 전체 기준선 비교와 순차 MSBuild를 실행했다. 최종 결과는 아래와 같다.

- 재연결 RED: `p7-pull-reconnect-red`의 실제 연결 수명 2건 중 프리뷰는 통과했고 편집은 이전 기대 번호 1의 Core 처리 기록이 없다는 ArtifactEditFlow 진단으로 실패했다. 기대 편집 입력이 없으면 취소로 기다림을 끝내도록 ArtifactEditFlow만 보완했다. 최종 회귀/전체/빌드 결과는 아래와 같다.

최종 증거는 `Logs/StageSelectSAVerification/p7-pull-*`와 `.superpowers/sdd/2026-10-10-stageselect-structure-alignment-plan/p7-pull-verification.json`에 보관한다.

- 새 Core 직접 조회 계약의 RED는 `p7-pull-red.xml` **9건 / 0 통과 / 예상 실패 9**다. 재연결 RED의 편집 실패를 수정한 뒤 최종 실행에서 같은 Stage의 Session Detach→Attach 편집·프리뷰 2건이 모두 통과했다. 정상 씬 이탈의 미완료 Job 차단·배출 후 재진입도 통과했다.
- 최종 관련 `p7-pull-final-related.xml`: **515건 / 514 통과 / 기존 실패 1 / 판정 불가 0 / skip 0**, Unity exit 2. 남은 실패는 기존 ArtifactPopupConfigTests 배치값 기대 2.2 / 실제 1.79다.
- 최종 전체 `p7-pull-final-all.xml`: **3728건 / 3693 통과 / 기존 실패 34 / 기존 판정 불가 1 / skip 0**, Unity exit 2. 실패 전체 이름 집합이 변경 전 `p7-all.xml`과 정확히 일치하며 **추가 실패 0**이다. 전체 통과로 표현하지 않는다.
- 실제 Boot/StageSelect 2건, 계산 중 취소·교체, 중복 실행 방지, 새 요청 처리, 같은 입력이 남은 처리 기록 누락의 도메인 정지, 숨김 완료·늦은 프리뷰·공용 오류 회귀가 통과했다. Core 입력 조회 통합 테스트와 Session/Flow 수명 테스트를 함께 사용했다.
- Unity 종료 뒤 순차 `MSBuild ProjectL.Tests.Framework.csproj /restore /t:Build /v:minimal /nologo` **exit 0**. `p7-pull-msbuild.log`에서 Systems와 Tests 어셈블리 컴파일을 확인했다. 기존 경고는 남아 있다. 최종 전체 실행 이후 운영 코드·테스트 변경은 없다.
- 새 문맥의 독립 검토 결과 Critical/Important/Minor 결함 **0건**. 이번 보완은 기존 파일 **19개(운영 코드 6, 테스트 6, 문서 7)** 수정이며 파일 추가·삭제와 meta 변경은 없다. 생성 csproj 포함, 제거한 payload API 호출 0건, Job의 입력·결과 Sink 의존성 0건, CRLF와 새 trailing whitespace 0건을 확인했다.
- Job의 기대 번호는 같은 Stage 재연결 뒤 이전 Job이 새 요청을 실행하지 못하게 하는 보호값이다. 요청 내용 조회·계산·결과 게시는 Core, 입력 삭제는 View가 맡는다. Framework/UI가 이 API 형태를 필수로 정했다고 해석하지 않는다.
- P8 사용자 Play·움직이는 프리뷰의 Profiler, P9 문서 통합, 상점 S5·Run seed R4는 별도다. 외부 전달·합의·커밋·push는 하지 않았다.
