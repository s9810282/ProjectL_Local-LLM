# 전투 Core 서비스 역할 분리 — View/Job 인계

- 작성: 2026-10-15 (Core, 천명). **외부 발송 전 문서다.** 이 문서를 쓴 것이 전달을 뜻하지 않는다.
- 근거: 전투 Core 서비스 책임 분리 설계 (Git history: `2026-10-14-combat-service-responsibility-design.md`), 구현 계획 (Git history: `2026-10-14-combat-service-responsibility-plan.md`) §8.
- 진행 방식(2026-10-15 변경): 처음에는 View 작업자가 병합과 호출자·테스트 이전을 직접 하기로 했으나, 이 브랜치에서 Unity를 바로 돌릴 수 있도록 **사용자 지시로 Core가 운영 7파일과 외부 테스트를 새 API로 옮겼다**(계획 §8 D). 바꾼 것은 접근점 이름과 테스트 대역·준비 방식뿐이며 Job·View 동작은 바꾸지 않았다.
- 상태: C3(구 통합 API 삭제)와 외부 이전까지 반영. 이 브랜치는 컴파일되고 EditMode 테스트를 돌릴 수 있다(§4).
- **View 작업자 병합 안내:** 재작성 중인 `CombatViewSystem`·전투 Job 6개와 충돌하면 **재작성본을 기준으로 두고 §2의 접근점 대응만 반영**하면 된다(이 브랜치의 해당 변경은 한 줄 이름 교체뿐). 외부 테스트 충돌도 같은 방식이며, §3은 Core가 무엇을 바꿨는지의 기록이다.

## 1. 바뀐 점 요약

`ICombatProcessingService`, 구현 `CombatProcessingService`, `CoreGroup.Combat`, 서비스 키 `InGameSystemServiceKey.Combat`를 지웠다. 대신 역할별 계약 6개를 쓴다. 전투 규칙·결과 이벤트 형식·입력/결과 번호·Buffer 원본·표시 완료 확인 계약은 바뀌지 않았다.

| 접근점 (`CoreGroup.`) | 계약 | 메서드 | 결과 의미 |
|---|---|---|---|
| `BattleEntry` | `IBattleEntryService` | `ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId)` | 수락 시 선택과 준비된 전투가 함께 게시된다. 거절은 결과로, 활성 전투가 이미 있거나 의존성이 없으면 상태 오류다 |
| `CombatCommands` | `ICombatCommandService` | `bool ProcessNextCommand()` | 처리해 게시했으면 true. 처리할 명령이 없거나 Run이 끝났으면 **정상 false** |
| `CombatAdvance` | `ICombatAdvanceService` | `bool AdvanceCombat()` | 자동 국면이 아니거나 Run이 끝났으면 정상 false. View 명령을 읽거나 소비하지 않는다 |
| `CombatPreview` | `ICombatPreviewService` | `bool ProcessLatestPreview()` | 새 요청이 없거나 계산 중 요청·전투가 바뀌었으면 false. 게임 상태·명령 번호를 바꾸지 않는다 |
| `CombatTimeline` | `ICombatTimelineQuery` | `CombatTimelineEntrySnapshot[] GetTimeline(int maxEntries = 10)` | 활성 전투가 없으면 상태 오류, 배치·완료 국면은 빈 배열, 음수 개수는 인자 오류. Artifact·DB를 읽지 않는다 |
| `BattleCompletion` | `IBattleCompletionService` | `BattleAbandonResult AbandonBattle()`, `void EndBattle()` | 포기는 완료 기록만 하고 일반 전투 결과를 게시하지 않는다. `EndBattle`은 완료 기록 전 호출이 상태 오류다 |

유지되는 책임 경계: 입력 제거·표시 완료 확인(ack)·Job 완료·씬 전환은 계속 View/Flow 소유다. Job은 정상 false에도 완료한다. 초기화는 기존과 같이 Framework InitData·PostBind 뒤에 쓴다.

**이름 주의:** View LifeCycle의 `ICombatEntryService`(키 `CombatEntry`, 진입 수명)와 새 Core `IBattleEntryService`(키 `BattleEntry`, 진입 판단·준비 게시)는 다른 계약이다.

동작 차이(정리 목적, View 영향 없음 예상):
- 턴 종료 명령(`CombatViewCommandData.EndUnitTurn()`, 행위자 없음)은 명령 서비스가 현재 활성 행위자로 해석한다(기존과 같은 결과).
- 타임라인 조회는 메인 지속 상태·Artifact 정의를 요구하지 않는다(기존에는 전체 명령 입력을 조립해 Artifact DB 오류가 조회까지 막을 수 있었다).
- 미리보기·명령 입력은 조립 시작 때 전투 ID를 고정한다. 입력 조립 도중 전투가 교체되면 미리보기 응답을 버린다.
- 구 테스트 편의 API(`PrepareBattle()`, `ApplyHealth`, `ApplyPersistentMain`, `FinalizeTurn`, `ResolveBattle`)와 구 회복 경로(`Combat.Recover`, `Recovery.Recover(챕터, 노드, 선택지)`)는 운영 계약이 아니어서 대체 API 없이 지웠다. 테스트에서는 아래 §3의 도우미를 쓴다.

## 2. 운영 호출자 교체 (7파일, 적용 완료, `Assets/ProjectL/Script/Systems/RuntimeSystem/` 기준)

| 파일 | 이전 | 현재 |
|---|---|---|
| `LifeCycleSystem/LifeCycles/Combat/Jobs/CombatPrepareJob.cs:43` | `CoreGroup.Combat.EnterBattle(` | `CoreGroup.BattleEntry.EnterBattle(` |
| 같은 Jobs의 `CombatCommandJob.cs:42` | `CoreGroup.Combat.ProcessNextCommand()` | `CoreGroup.CombatCommands.ProcessNextCommand()` |
| 같은 Jobs의 `CombatAdvanceJob.cs:46` | `CoreGroup.Combat.AdvanceCombat()` | `CoreGroup.CombatAdvance.AdvanceCombat()` |
| 같은 Jobs의 `CombatPreviewJob.cs:45` | `CoreGroup.Combat.ProcessLatestPreview()` | `CoreGroup.CombatPreview.ProcessLatestPreview()` |
| 같은 Jobs의 `CombatAbandonJob.cs:37` | `CoreGroup.Combat.AbandonBattle()` | `CoreGroup.BattleCompletion.AbandonBattle()` |
| 같은 Jobs의 `CombatExitJobs.cs:129` | `CoreGroup.Combat.EndBattle()` | `CoreGroup.BattleCompletion.EndBattle()` |
| `InGameSystem/ViewSystem/Combat/CombatViewSystem.cs:112·201` | `ICombatProcessingService _combatQueries` = `CoreGroup.Combat` | `ICombatTimelineQuery _combatQueries` = `CoreGroup.CombatTimeline` (`:715`의 `GetTimeline` 호출은 그대로) |

위 교체는 이 브랜치에 적용돼 있다. 각 파일에서 바뀐 줄은 표의 한 줄(CombatViewSystem은 필드 형식·대입 두 줄)뿐이다.

## 3. 외부 테스트 이전 (Core가 적용, 2026-10-15)

C3에서는 천명 작성 줄만 고쳤고(`CombatMovementViewTests`·`MovementDraftTests`·`CombatDisplayReuseTests`의 해당 줄), 나머지 외부 작성 줄은 사용자 지시에 따라 D에서 Core가 옮겼다. "작성" 열은 원래 줄을 쓴 사람으로, 병합·재작성 때 확인할 대상이다. 줄 번호는 이전 전 기준이다.

| 작성 | 파일:줄 | 이전 | 바꾼 내용 |
|---|---|---|---|
| 용근 | `CombatPlanTests.cs:211·218·227·236·244` | `FakeInGameService`·`FakeCoreService`가 `ICombatProcessingService`를 받고 `Combat`을 구현, `CombatProbe`가 구 인터페이스 구현 | `CombatProbe`를 `internal`로 바꾸고 역할 인터페이스 6개를 구현(구 편의 API 멤버 삭제, 호출 기록은 그대로). `FakeCoreService`는 접근점 6개를 받는다. `FakeInGameService(CombatProbe probe = null, …)`는 하나의 Probe를 6개 접근점에 꽂고, 새 오버로드 `FakeInGameService(CombatRequestTestParts services, …)`는 실제 역할 서비스를 꽂는다(진입은 null — Buffer를 직접 준비한 테스트용) |
| 용근 | `CombatProgressJobTests.cs:650·758·760·773` | `FakeCombat : ICombatProcessingService`, `Core`의 `Combat` | `FakeCombat`은 Job이 쓰는 4개 역할(명령·미리보기·자동 진행·완료)만 구현, `Core`는 그 4개 접근점에 같은 대역을 꽂고 `BattleEntry`·`CombatTimeline`은 null |
| 호진 | `LobbyDepartureIntegrationTests.cs:346·367` | `CountingCore`의 `Combat => _inner.Combat` | 접근점 6개를 `_inner`로 전달 |
| 용근 | `CombatMovementFeedbackTests.cs:168·384·418·440` | Rig 필드 `CombatProcessingService Service`, `Service.Process…` | 필드 형식 `CombatRequestTestParts`, 호출 `Service.Commands.ProcessNextCommand()`·`Service.Preview.ProcessLatestPreview()` |
| 용근 | `CombatMovementViewTests.cs:406·458·493` (`45·179·238`은 오버로드로 그대로) | `r.Service.ProcessLatestPreview()` | `r.Service.Preview.ProcessLatestPreview()` |
| 용근 | `CombatDisplayReuseTests.cs:381·741`, `CombatPointerMappingTests.cs:117` | `service.ProcessLatestPreview()` | `service.Preview.ProcessLatestPreview()` |
| 용근 | `CombatProgressIntegrationTests.cs:267·272~274·297·359·416~420·427~429` | `Core.Combat.EndBattle()`, 맵·선택을 Buffer에 직접 써 두고 `Core.Combat.PrepareBattle()`(무선택 준비) | `Core.BattleCompletion.EndBattle()`. 이 fixture는 준비 뒤 실제 명령을 처리하므로 정의 설치가 필요해 **실제 진입 경로로 바꿨다**: 그래프에 시작 노드 3(깊이 0, 다음 1)을 더하고 `SelectMap`·`SelectStage` 대신 `EnterChapter(1)`, `Prepare(int node = 1)`는 `BattleEntry.EnterBattle(1, node)` 수락을 단언. 두 번째 전투 테스트의 `ConfirmStageEntry(1, 2)` + `Prepare()`는 `Prepare(2)`로(진입이 선택 확정을 포함). `297·359`는 오버로드로 그대로 |
| 용근 | `CombatProgressTestDriver.cs:32` | `CombatPlanTests.CreateRegistry(…)` | 변경 없음(`CombatPlanTests` 이전으로 컴파일) |
| 용근 | `CombatEntryIntegrationTests.cs:655·684` | `f.Core.Combat.AbandonBattle()`, `f.Core.Combat.ApplyHealth(…)` | `f.Core.BattleCompletion.AbandonBattle()`, `CombatTestFixtures.SetHealth(f.Buffers, …)` |
| 용근 | `CombatSceneEventPresenterTests.cs:501·541·552`, `CombatMortarAssetTests.cs:454·555·568` | `core.Combat.EnterBattle/ProcessNextCommand/AdvanceCombat` | `core.BattleEntry/CombatCommands/CombatAdvance` |
| 용근 | `TerrainOperationalDisplayTests.cs:118·220` | `core.Combat.PrepareBattle()` | 준비된 Buffer 상태만 읽으므로 `BattleDefinitionAdapterTests.PrepareCommitted(DatabaseSource(buffers), core, buffers, database)`(운영 DB 정의로 준비 계산·게시, 두 helper를 `internal`로 공개) |
| 호진 | `ArtifactHealingChaliceIntegrationTests.cs:48·64·111·112·118·124·171·177·190·205·235` | `Combat.ApplyHealth`·운영 메서드 | 운영 메서드는 접근점 교체, 체력 입력은 `CombatTestFixtures.SetHealth(fixture.Buffers, …)`. 이 테스트의 3개 case는 작업 전(`56b7d45f`)부터 `InvalidPrimaryTarget`으로 실패한다 |
| 호진 | `StageMapDepthCenteringBootSmokeTests.cs:396`, `StageMapNodeIconBootSmokeTests.cs:206` | `CoreGroup.Combat`의 private `_database` 리플렉션 | `CoreGroup.BattleEntry`의 `_database`(같은 이름의 private 필드). 리플렉션 의존이므로 진입 서비스 필드 이름이 바뀌면 함께 바꿔야 한다 |
| 현강 | `FrameworkRuntimeCompositionTests.cs:208·211` | `InGameSystemServiceKey.Combat` 부재 단언, 조회 필드가 `CoreGroup.Combat`과 같은지 | 최상위 부재 단언을 `BattleEntry`·`CombatTimeline` 키 두 개로, 조회 필드 비교 대상은 `CoreGroup.CombatTimeline` |

테스트 도우미(Core 소유, `Assets/Tests/EditMode/Framework/`):
- `CombatTestFixtures`: `SetHealth`, `SetPersistentMain`, `PrepareCommitted`, `ResolveBattle`, `FinalizeTurn` — 기존 계산기와 Buffer 게시만으로 구 테스트 편의 API가 하던 상태를 만든다.
- `CombatRequestTestParts`: Buffer를 직접 준비한 테스트가 실제 역할 서비스(`Commands`·`Advance`·`Preview`·`Completion`·`Timeline`)를 명시적 정의 대역으로 쓰는 조립.
- `CombatRequestTestParts.Over(core)`: 실제 런타임 Core 그룹에서 전투 역할 서비스(명령·자동 진행·미리보기·타임라인·완료)만 위 조립으로 바꾼 그룹. Buffer에 전투를 다시 준비하는 Job·View 통합 테스트용(진입은 원래 Core).
- `CombatPlanTests.FakeInGameService(CombatRequestTestParts services, …)`: 실제 역할 서비스로 Job을 돌리는 Group 대역(진입 null).
- `CombatPreviewPublicationTests.Create(out CombatRequestTestParts service, …)`: 단일 out 모양 유지. `out _` 사용처는 바꿀 필요가 없다.

## 4. 검증 상태

| 항목 | 결과 |
|---|---|
| C2까지(컴파일 유지 상태) | 전체 EditMode 6555개 중 6473 통과·81 실패·1 skip. 변경 전 `56b7d45f` 전체(6531개 중 82 실패)와 비교해 **새 실패 0건** |
| C3 단독 | 외부 이전 전에는 컴파일러가 첫 외부 파일(`CombatViewSystem.cs`)에서 멈춰 Unity 테스트를 돌릴 수 없었다 |
| D 이전 대상 클래스 | §2·§3 적용 뒤 컴파일 오류 0. 이전한 17개 클래스 426개 중 396 통과·30 실패, 30건 모두 C2 기준선 또는 변경 전 실패와 같은 테스트(렌더·UI·씬·DB 자산 쪽과 `ArtifactHealingChaliceIntegrationTests` 3). 그중 `CombatSceneEventPresenterTests.OperationalDatabaseLive_FirstAndRepeated…`는 운영 DB로 생성한 무작위 그래프의 노드 2에 진입해 생성 결과에 따라 통과·실패가 갈린다(`56b7d45f`에서도 실패, C2에서는 통과) |
| D 전체 EditMode | 6554개 중 6468 통과·85 실패·1 skip. C2와 비교해 빠진 테스트는 C3에서 의도적으로 지운 `BattleDefinitionAdapterTests.DefinitionReload_…` 1개다. C2 실패 81건은 그대로이고 **새 실패 4건**은 모두 Core(천명) 테스트였다: `ArtifactEditingServiceTests.AcceptedPlacementFeedsExistingCombatEffectsOncePerInstance` 3 case, `CoreStageCombatCycleTests.RegisteredPathCycle_PreviewMoveBarrierReturnAttackEndTurn` |
| 새 실패 4건 원인·수정 | Buffer에 전투를 직접 (다시) 준비한 뒤 런타임 명령·미리보기 서비스를 불러 "설치된 전투 정의 없음/전투 번호 불일치" 상태 오류가 났다. C3에서 한시 호환 설치 경로를 지운 결과이며 운영 동작 결함은 아니다. 두 테스트 모두 같은 정의를 명시적으로 주는 `CombatRequestTestParts` 역할 서비스를 쓰게 바꿨다(사이클 테스트는 새 `CombatRequestTestParts.Over(core)`로 Job·View가 보는 Core 그룹의 전투 역할만 교체). 수정 뒤 관련 6개 클래스 146/146 통과 |
| 결론 | 수정 뒤 실패 집합은 C2 기준선 81건과 같다(전체 재실행은 하지 않았고, 새 실패 4건이 포함된 클래스만 다시 돌렸다). 결과 XML·로그: 스크래치패드 `d1-migrated/`·`d1-full/`·`d1-fix/`(임시 위치) |

## 5. 변경 시나리오별 시작 위치(설계 §12.2 대응)

| 바꾸려는 것 | 시작 파일 (`CoreSystem/CombatSystem/` 등) | 함께 볼 계약·테스트 |
|---|---|---|
| 미리보기 종류/응답 | `CombatPreviewService` | `CombatPreviewServiceTests`, `CombatPreviewPublicationTests` |
| 명령 수락·오래된 이동 요청·턴 종료 행위자 | `CombatCommandService` | `CombatCommandServiceTests`, `MovementPublicationTests` |
| 자동 진행 조건 | `CombatAdvanceService` | `CombatAdvanceServiceTests` |
| 타임라인 항목/개수 | `CombatTimelineQuery`, `CoreCaculator/Application/Combat/CombatTimelineCalculator` | `CombatTimelineQueryTests`, `CombatCommonCommandTests`의 Timeline 테스트 |
| 실제/예측 턴 순서 규칙 | `CoreCaculator/Application/Combat/CombatTurnOrder` | 위 + 실제 턴 테스트(양쪽 함께) |
| 진입 허용/준비 | `BattleEntryService` | `BattleEntryServiceTests`, `CoreStageCombatCycleTests` |
| 포기 허용 | `BattleCompletionService` | `BattleCompletionServiceTests` |
| 정의 설치/해제 | `BattleLifetime` | `BattleLifetimeTests` |
| Artifact 효과 변환 | `ArtifactLoadoutReader` | `CombatInputAssemblyTests`, 포기 정산 테스트 |
| 명령 결과 게시/진행 제안 | `CombatTransitionPublisher`, `BattleProgressionPlanner` | `CombatTransitionPublisherTests` |
| 회복 선택 정책 | `RecoverySystem/RecoveryStageService` | 회복 typed 테스트(전투 서비스와 무관) |
| 전투 내부 협력 객체 교체 | 해당 기능 + `Factory/Runtime/InGame/Core/CombatSystemFactory` | `CombatServiceCompositionTests` |
