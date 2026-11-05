# 전투 View 구조 리팩토링 Task 계획

- 작성: View 작업, 2026-10-14
- 설계 원본: [구조 리팩토링 설계](../specs/2026-10-14-combat-view-structure-refactor-design.md)
- 상태: **R0~R4 완료(D12 포함, Unity 확인, 2026-10-14).** 2026-10-16 Timeline UI 삭제에 따른 D11 호출 폐기·검증은 §6.12에 기록했다. 가시밭 칸 외형은 전용 View 표 `ThornFieldVisual` 한 곳이 원본이다(설계 D12). 타임라인 공유 요청은 폐기하며, 호진님(가시밭 표) 공유 메모는 미전달이다.
- 진행 방식: Claude가 직접 구현하고, Phase마다 실습 워크시트(artifact)를 함께 전달한다.
- 각 Phase 완료 후 보고하고 멈춘다. 커밋·push·다른 작업자 전달은 사용자가 한다.
- 검증 표기: **MSBuild**(Claude, 스크래치 출력) / **Unity**(사용자 에디터 컴파일·EditMode·재생)를 구분한다.

## 0. Phase 0 — 리뷰·설계·계획

- [x] 최근 3일 커밋과 `CombatViewSystem`·`CombatSceneDisplay`·주변 연출 파일 리뷰(설계 §1 P1~P11).
- [x] 사용자 결정(2026-10-14): R0 = 연출 없이 사실만 반영·모르는 사건은 경고 기록. R4 = 타임라인 UI 포함·Audio 제외. 진행 = 직접 구현 + 워크시트.
- [x] 설계 문서·이 계획 작성. View 계획 §7.77에 시작점 링크.

## 1. R0 — Core 새 사건 3종에서 전투가 멈추지 않게 (D1·D2)

- [x] **R0.1** `OrderedCombatPresentation`: `Play`의 `NotSupportedException`을 제거한다. 표시할 수 없는 사건은 `Tick`에서 사실 반영 없이 건너뛰고, `Play`에서 사건 형식마다 한 번(같은 `Attach` 안) 경고를 남긴다. `Attach`가 경고 기록을 비운다.
- [x] **R0.2** `CombatSceneEventPresenter.CanPresent`에 `CombatFallEvent`·`CombatRoarEvent`·`CombatSummonEvent`를 넣는다. `PresentEvent`에서 소환은 `Created`를 표시 사실에 넣는다. 낙하·포효는 바꾸는 것이 없다.
- [x] **R0.3** 계약 주석: `ICombatResultPresenter.CanPresent`(false = 경고 후 건너뜀), `OrderedCombatPresentation` 클래스 주석.
- [x] **R0.4** 테스트: `CombatMovementViewTests.MissingNonMovementHandler_*`를 "경고 → 건너뜀 → 갱신·확인"으로 바꾼다. `CombatSceneEventPresenterTests`에 낙하·포효(변화 없이 한 번 완료)와 소환(생성 칸에 유닛 표시) 테스트를 더한다.
- [x] **R0.5** MSBuild(`ProjectL.Tests.Framework`) 오류 0, 바꾼 파일의 경고 0(§6.1).
- [x] **R0.6** Unity: 컴파일, EditMode `CombatMovementViewTests`·`CombatSceneEventPresenterTests`·`CombatEnemyPresentationTests`·`CombatPresentationCleanupTests`, 가능하면 보스 스테이지·낭떠러지 재생(사용자). **2026-10-14 사용자 "R0 유니티 확인 완료".**
  - **2026-10-15 정정:** `CombatEnemyPresentationTests`는 2026-10-13부터 .meta GUID가 33자리로 손상돼 Unity가 파일을 무시했으므로 이 확인 때 실행되지 않았다. GUID를 고쳤고 다음 Unity 실행부터 돈다([SV 인수 평가 §6·§7](../specs/2026-10-15-combat-view-structure-acceptance.md)).

## 2. R1 — 공용 정리 헬퍼와 표시 의존성 묶음 (D3·D4)

- [x] **R1.1** `RuntimeSystem/Common/CleanupErrors.cs`(+ .meta). 전투 View 여섯 파일의 복사 헬퍼·반복 try/catch를 교체한다. 메시지 문구는 유지한다.
- [x] **R1.2** `CombatDisplayTables`·`CombatDisplayServices`(Combat/, + .meta). `CombatSceneDisplay(root, tables, services)`.
- [x] **R1.3** `CombatViewSystem`: 표·서비스 필드를 묶음 두 개로, `InitData`는 `CombatDisplayTables.From`, `BindServices`는 `CombatDisplayServices`를 받는다.
- [x] **R1.4** `IsMoveMode`와 Router 판정 비교. 같으면 교체, 다르면 주석만. → 같음, 조건 제거(§6.4).
- [x] **R1.5** 테스트 생성 호출 갱신. 실제 대상은 `BindServices` 3파일 3곳(`CombatEntryIntegrationTests`의 것은 다른 시스템), `CombatSceneDisplay` 생성 7파일 8곳(`CombatMortarAssetTests`의 대상 형식 `new(`  포함). 검증 내용은 바꾸지 않았다.
- [x] **R1.6** MSBuild(완료, §6.4), Unity 전투 View EditMode 전체(사용자). **2026-10-14 사용자 "R1, R2-1 유니티 확인 완료".**

## 3. R2 — 표시 Facade와 사건 재생 분리 (D5~D9)

**사용자 결정(2026-10-14): 두 번에 나눠 각각 멈추고 보고한다.**
- **R2-1:** R2.1(숨은 재생 계약 명시·`FakeDisplay` 기본 구현) + R2.4a(근거 없는 정리 수명의 회귀 먼저).
- **R2-2:** R2.2(재생 조정 이동) + R2.3(개발 패널) + R2.4(정리 묶음) + R2.5(해제 뒤 규칙) + R2.6.

- [x] **R2.1** `ICombatEventPlayback`, `ICombatBattleDisplay` 포함, `OrderedCombatPresentation`의 `as`/`is` 제거, `FakeDisplay` 기본 구현. (R2-1, §6.5)
- [x] **R2.2** `CombatSceneEventPlayback`으로 기차 접촉·사건 순서·버전·실패 정리를 옮긴다(순서 그대로).
- [x] **R2.3** `CombatDevLayerPanelBinding`(씬 변경 없음).
- [x] **R2.4a** 근거가 없는 수명부터 회귀를 추가한다: 이동 계획(범위·편집 고스트) 표시 중 재설치하면 둘 다 남지 않는지(설계 D8 "근거 없음"). 목록을 바꾸기 전에 현재 코드로 통과해야 한다. (R2-1, §6.5 — Unity 확인 완료 2026-10-14)
- [x] **R2.4** 정리 묶음(`UnbindField`·`CancelPlayback`·`ReleaseOwned`)을 만들고 Install 직전·실패 정리의 차이를 정리한다. 수명 근거는 설계 D8 표의 기존 테스트(Install 실패 Object·Terrain·환경 효과, Build 실패, 이동 중 Clear 재설치, Release 뒤 거절 — 표시·좌표 양쪽)와 R2.4a다. `CombatDisplayReuseTests`는 일반 재사용 회귀로 유지하고 Install 실패 수명의 근거로 삼지 않는다.
- [x] **R2.5** 해제 뒤 호출 규칙(D9) 적용.
- [x] **R2.6** MSBuild(완료, §6.6), Unity 전투 View EditMode 전체, 재생(배치·이동·공격·기차)(사용자). **2026-10-14 사용자 "R2-2 유니티 확인 완료".**

## 4. R3 — 사건 Presenter 분리 (D10)

- [x] **R3.1** `CombatAttackPlayback`(공격 4종·투사체 풀·Ballista·관통 대응). 오디오는 콜백 그대로.
- [x] **R3.2** `CombatUnitMotionPlayback`(걸음·회전·복귀).
- [x] **R3.3** `FieldUnitPresenter.TryGetBallistaWeapon(actorId, out weapon)`(기존 `GetBallistaWeapon`의 예외 없는 판)을 더하고, 턴 종료 재장전을 이 조회로 바꿔 `GetComponent<FieldUnitView>()` 루프를 제거한다. ID 키 캐시는 두지 않는다(설계 D10). Tick 대상은 무기 객체 집합 그대로(객체당 한 Tick), 발사·재장전 시작 때 넣고 취소·Clear·Release에서 비운다.
- [x] **R3.3a** 대여 수명 회귀를 추가한다: A의 View 반환 → 같은 무기 객체를 B가 재대여했을 때 A의 턴 종료는 재장전하지 않고, B의 턴 종료는 재장전을 한 번만 하며, 무기 객체당 Tick이 한 번인지. 취소·Clear·Release 뒤 Tick 집합이 비는지.
- [x] **R3.4** MSBuild(완료, §6.7), Unity 공격·적 연출·기차·Mortar·Ballista 테스트, 재생.

## 5. R4 — 타임라인 UI 동기화 (D11, D12는 착수 전 확인)

- [x] **R4.1** `CombatTimelineViewSystem`(새, D11): Combat 씬 Enter에서 연결, 연결된 동안 Tick마다 Buffer에서 보임 파생(표시 안 된 결과가 있으면 유지), Exit·종료·오류 정지에서 닫기. `CombatViewSystem`에서 `SyncTimelineUI`·타임라인 참조·UI 서비스 참조를 지운다. View Factory에 `CombatViewSystem` 다음으로 등록하고, `InGameViewFaultCleanup`에 타임라인 정리 콜백을 더한다. (첫 안 `CombatTimelineUiSync` + 조정자 호출은 사용자 결정 (b)로 대체, §6.8)
- [x] **R4.1a** `CombatTimelineViewSystemTests`: 보임 진리표, 연결 전·표시 대기 중 유지, 밤 종료·전투 종료·전투 정리·다른 전투에서 닫힘, 같은 값 재호출 없음, Exit·오류 정지 닫힘과 오류 뒤 다시 열지 않음, UI 서비스 없는 조립.
- [x] **R4.2** 현강님 공유 메모: 동작 변화 3가지, TimelineUI 내용 쪽 단계 검사 중복 제거 요청(SSOT), 내용이 연출보다 앞서는 문제(별도 후보). 전달은 사용자.
- [x] **R4.3** D12 목적지를 정한다. 사용자 결정(2026-10-14): 가시밭 전용 View 표 `ThornFieldVisual`(설정 하나). 처음 안(필드 표시 설정)을 대체한다. 근거·비교·비용은 설계 D12, 경위는 §6.9. 구현 R4.3a~d는 **사용자 진행 지시 뒤**에 한다.
- [x] **R4.3a — 새 표 추가(한 커밋)** (코드 완료, §6.10)
  - **착수 전:** 호진님께 `IViewQuery` 속성 추가(테스트 가짜 3개)와 `ViewGroup.asset` 표 등록을 알린다. 에셋을 Claude가 YAML로 쓸지 사용자가 에디터로 만들지 정한다.
  - **코드:**
    - `DataBaseSystem/Data/View/Table/ThornField/`에 `ThornFieldVisualData`(`prefab` 하나, 선택)·`ThornFieldVisualDataSO`·`ThornFieldVisualDataTableSO`
    - `Queries/ViewDataBase/ViewTable/ThornFieldVisualTable.cs`: `IThornFieldVisualTableQuery.Config`, 레일처럼 항목 정확히 하나
    - `IViewQuery`·`ViewDataBase` 속성, `ViewDataBaseFactory` 생성·등록, `ViewTableKey.ThornFieldVisual`
  - **같은 커밋:**
    - `IViewQuery` 테스트 가짜 4개(`ArtifactPopupConfigTests`·`LobbyInputTests`·`LobbyViewSystemTests`·`UnityStageInputTests`)
    - `DataBaseSO/ViewGroup/ThornFieldVisual/`의 표·항목 에셋과 `ViewGroup.asset` 등록. 항목 프리팹은 옛 참조와 같은 에셋(`CombatAttack_CactusCannon.asset`의 `thornPrefab`, guid `09fc9d774997b0349b84c5298e5ceca1`)이다.
    - 표 에셋이 없으면 DB 생성·부팅 전체가 실패하므로 나눠 커밋하지 않는다.
  - **아직 그대로:** `CombatSceneDisplay`, 옛 `thornPrefab`, Mortar 필수 검사.
  - **확인:** MSBuild, `DataBaseAssetTests`(`Factory_PreservesExistingViewTables`에 새 조회 추가), Combat 진입이 그대로인지.
- [x] **R4.3b — 연결 확인(사용자 에디터)** (사용자 Unity 확인 완료, 2026-10-14) 새 항목의 프리팹이 옛 참조와 같은 에셋인지, DB 생성·Combat 진입이 그대로인지 확인한다. 확인 전에는 R4.3c로 넘어가지 않는다(옛 필드를 먼저 지우면 에셋을 다시 저장할 때 옛 참조 값이 사라진다).
- [x] **R4.3c — 사용처 이전·옛 필드 제거** (코드 완료, §6.11)
  1. `CombatDisplayTables`에 가시밭 설정을 더한다(`From`과 생성자).
  2. `CombatSceneDisplay`가 그 설정의 프리팹으로 `FieldThornPresenter`를 만들게 하고, `Unit_CactusCannon` 조회를 지운다. 프리팹이 없으면 가시밭이 나올 때의 기존 예외를 유지한다.
  3. 그 뒤 `CombatAttackPresentationSO.thornPrefab`·Mortar 필수 검사·`CombatAttackPresentationData.ThornPrefab`을 지운다.
  4. 가시밭 칸 외형 원본이 `ThornFieldVisual` 한 곳만 남는지 확인한다.
- [x] **R4.3d — 테스트 갱신·실행** (완료 — Unity 확인, 부수 문서는 사용자 결정으로 제외, §6.11)
  - `CombatAttackPresentationSettingsTests`: `MortarSettings_RequireSeparateThornPrefab`은 삭제하거나 반대 검증으로 바꾸고, `thornPrefab` 사례를 정리한다.
  - `CombatMortarAnimationTests`·`CombatMortarPresentationTests`: `thornPrefab` 설정을 지우고, 가시밭 Presenter 생성 경로를 새 표 기준으로 맞춘다.
  - `CombatMortarAssetTests`: 옛 위치 조회를 새 표로 바꾼다.
  - `DataBaseAssetTests`: 가시밭 특성 유닛이 있으면 프리팹이 있고 프리팹 에셋인지 대조하는 검사를 더한다.
  - `CombatDisplayTables` 직접 생성 3곳: `CombatSceneEventPresenterTests` 1곳, `TerrainOperationalDisplayTests` 2곳.
  - Mortar·가시밭 생성·소비·재사용 회귀를 돌린다.
  - 부수 문서: DB-SO 설계는 갱신하지 않는다(사용자 결정 2026-10-14).
  - 사용자 에디터 확인과 Unity 검증이 끝나기 전에는 D12를 완료로 기록하지 않는다.
- [x] **R4.4** MSBuild, Unity 테스트, 재생(밤 진입·밤 종료·승패·포기·재연결).

## 6. 실행 기록

### 6.1 R0 (2026-10-14, 코드 완료·Unity 확인 대기)

- **확인한 원인:** Core가 `CombatFallEvent`(`MovementProjection.cs:177`)·`CombatRoarEvent`(`BossRoarCalculator`·`CombatAttackCalculator`)·`CombatSummonEvent`(`CombatSummonCalculator`)를 결과 사건에 넣는데, View 허용 목록에 없어 `OrderedCombatPresentation.Play`가 `NotSupportedException`을 던졌다. `CombatResultPlayback.Begin`에 catch가 없어 Tick → Bootstrap까지 전파됐다.
- **바꾼 파일:**
  - `OrderedCombatPresentation.cs`: `Play`의 예외 → 경고(형식마다 연결당 1회, `_warnedUnsupported`, `Attach`에서 비움). `Tick`이 표시 계약 없는 사건을 대기·사실 반영 없이 건너뜀. 판정은 `CanPresent(e)` = 이동 또는 `ICombatResultPresenter.CanPresent`.
  - `CombatSceneEventPresenter.cs`: `CanPresent`에 세 사건 추가, `PresentEvent`에서 소환 `Created`를 표시 사실에 반영.
  - `ICombatMovementPresenter.cs`: `CanPresent` 계약 주석(false = 경고 후 건너뜀).
  - 테스트 `CombatMovementViewTests`: `MissingNonMovementHandler_BlocksReconcileAndAcknowledgement` → `MissingNonMovementHandler_WarnsSkipsAndAcknowledges`(경고 기대, 건너뛴 사건 미전달, 갱신 1회, 확인 번호 = 결과 번호). `CombatSceneEventPresenterTests`: `FallAndRoar_CompleteOnceWithoutChangingUnits`(2건), `Summon_ShowsCreatedUnitAtEventWithoutAnimation` 추가.
- **오디오:** 변경 없음(설계 §0).
- **검증:** MSBuild `ProjectL.Tests.Framework`(스크래치 출력) 오류 0, 경고 16건은 모두 기존 다른 파일(바꾼 파일 0건). 새·바뀐 테스트는 Combat.unity·GameObject가 필요해 관리 실행기로 돌릴 수 없어 **Unity EditMode 대기**.
- **사용자 확인 대기:** R0.6.

### 6.2 문서 리뷰 반영 (2026-10-14)

- D8의 수명 검증 근거를 R2.4에 구체화하고, D10의 대여 수명 계약·회귀를 R3.3~R3.3a에 추가했다.
- D12는 미승인을 유지하고, 승인 시 필요한 필드·에셋·테스트 이전을 R4.3a~d로 분리했다.
- 문서 보완만 반영했다. R0의 기존 상태는 유지하며, R1~R4 구현·검증 완료를 뜻하지 않는다.

### 6.3 문서 재검토 반영 (2026-10-14)

6.2의 보완을 코드와 대조한 뒤 네 가지를 고쳤다(사용자 지시 "네 가지 다 반영"). 문서만 바꿨다.

- **D8·R2.4:** 인용 테스트는 모두 실재하지만 Install 직전·실패 정리의 차이 자체는 덮지 못했다. 실패 정리의 Object·Terrain 근거(`TerrainFacts_BothFieldModes_CoexistWithObjectsAndReuseMovingTornado` :348-351)와 환경 효과 근거(`Explosion_FailedReinstallRecallsOldEffectsAndUnbindsEvents`)를 수명별 표로 추가했다. Install 직전 `_movementPlanning.Clear`는 근거가 없어 R2.4a(회귀 먼저 추가)를 새로 두었다.
- **D10·R3.3~R3.3a:** ID 키 캐시 + 대여 무효화 계약 대신, 턴 종료 때 `FieldUnitPresenter.TryGetBallistaWeapon`으로 현재 대여를 조회하는 방식으로 바꿨다. 무기 주인의 원본이 대여 한 곳에만 남고 무효화 결합이 생기지 않는다. Tick용 객체 집합은 유지한다.
- **D12·R4.3a~d:** 옛 필드를 먼저 지우고 새 필드를 필수로 만들면 에디터 연결 전까지 DB 검증·Combat 진입이 깨지고, 에셋 재저장 때 옛 참조 값이 사라진다. 순서를 "새 필드 선택 추가 → 사용자 연결 → 사용처 이전·필수화·옛 필드 제거 → 테스트"로 바꿨다. 옛 참조가 `CombatAttack_CactusCannon.asset` 한 곳임을 확인해 적었고, 영향 테스트를 4개(`CombatAttackPresentationSettingsTests`·`CombatMortarAnimationTests`·`CombatMortarPresentationTests`·`CombatMortarAssetTests`)로 넓혔다.
- **설계 머리말:** 진행 상태("R0 진행")를 빼고 결정만 남겼다. 진행·검증 상태는 이 계획에만 적는다.

### 6.4 R1 (2026-10-14, 완료 — Unity 확인)

- **R0 확인:** 사용자 Unity 확인 완료(R0.6).
- **새 파일(각 .meta 새 GUID):** `RuntimeSystem/Common/CleanupErrors.cs`(`Try`·`ThrowIfAny`·`RunAll`, null 정리도 실패로 모음), `ViewSystem/Combat/CombatDisplayTables.cs`(View DB 8개, `From(IViewQuery)`가 기존 `InitData`의 검사를 옮김, 문구 접두어만 `[CombatDisplayTables]`), `ViewSystem/Combat/CombatDisplayServices.cs`(Color·AreaOutline·DrawPointLine·Effect 필수, Audio 선택).
- **바꾼 파일:**
  - `CombatSceneDisplay`: 생성자 14인자 → `(root, tables, services)`. 개별 null 검사는 묶음으로 옮김. `_fieldConfig` 등 작업 필드는 유지(테스트가 `_fieldConfig`를 리플렉션으로 읽음). 오디오 호출 줄은 그대로. 쓰지 않게 된 using 3개 제거.
  - `CombatViewSystem`: 표·Render 필드 13개 → `_displayTables`·`_displayServices`. `InitData` = `CombatDisplayTables.From`. `BindServices`는 `(…, loadingRegister, entry, displayServices)`. `_audio`·`_ui`는 그대로 두고 `PostBindService`가 묶음에 `_audio`를 넘김. 정리 경로(`Detach`·`StopInput`·`StopInteraction`·`ReleaseInput`·`AbortPlayback`)를 `CleanupErrors`로, 정적 `ThrowIfAny` 제거. null일 수 있는 대상은 대리자를 만들기 전에 확인하고(`_receiver`·`pathInput`·`_interaction`·`_display`), 현재 Layer 읽기는 `ClearLayerIfSet` 안에 두어 실패 모음 범위를 유지했다. `ReleaseInputOrReport`(보고 경로)는 그대로.
  - `OrderedCombatPresentation.Cancel`, `CombatSceneEventPresenter`·`CombatMovementPlanningPresenter`·`CombatPathInput`의 복사 헬퍼 → 한 줄 위임(메시지 유지). "원래 예외를 다시 던지며 정리 실패를 묶는" 다른 패턴(`AggregateException(error, cleanup)`)은 D3 범위 밖이라 그대로 뒀다.
- **R1.4 결론:** `IsMoveMode`(공격 모드 아님)는 Router 판정과 다르지만(배치 Layer·기록 모드에서도 참), 호출 대상 `CombatPathInput.RefreshReturnPreview`가 `AllowsInput()`에서 `facts.IsPathTarget`(Router 판정)으로 스스로 거르고, `Tick`도 같은 호출을 조건 없이 매 프레임 한다. 공격 모드에서는 경로 초안이 이미 취소돼 있어 호출해도 아무것도 하지 않는다. 그래서 조건을 지우고 근거를 주석으로 남겼다(동작 동일).
- **검증:** MSBuild `ProjectL.Tests.Framework`(새 파일 3개는 extra.targets로 추가) 오류 0, 바꾼 파일의 경고 0(전체 경고 8건은 기존 다른 파일), 산출 DLL에 `CombatDisplayTables` 포함 확인. 첫 빌드에서 `CombatMortarAssetTests`(대상 형식 `new(`)를 놓쳐 CS1729가 났고 고친 뒤 통과. 테스트가 리플렉션으로 읽는 private 멤버 중 지운 것은 없음. **Unity EditMode·재생은 사용자 대기(R1.6).**

### 6.5 R2-1 (2026-10-14, 완료 — Unity 확인)

- **R1 Unity 확인:** R2-1 착수 때는 통보 전이었고, 뒤에 R2-1과 함께 확인 완료 통보를 받았다(R1.6). R2-1 착수 시점에 Unity가 csproj를 다시 만들어 R1 새 파일 3개가 들어가 있었다.
- **새 파일:** `ViewSystem/Combat/ICombatEventPlayback.cs`(+ .meta 새 GUID). 결과 표시·재생 제어·이동 재생·이동 시작/끝·순서 준비·Barrier 7개 계약을 묶는다. 기다릴 것이 없는 구현의 기본값("대기 없음·즉시 완료")을 문서 주석에 적었다.
- **바꾼 운영 코드:**
  - `ICombatBattleDisplay`: `ICombatResultPresenter`·`ICombatPresentationControl` 대신 `ICombatEventPlayback`을 포함.
  - `CombatSceneDisplay`: 클래스 선언에서 따로 나열하던 계약 4개 제거(이미 `ICombatBattleDisplay`로 포함). 멤버는 그대로.
  - `OrderedCombatPresentation`: 표시 참조 3개(`_movementPresenter`·`_resultPresenter`·`_control`) → `_eventPlayback` 하나. `as`/`is` 다섯 곳 제거(순서 준비·Barrier 시작·이동 시작·이동 끝·Barrier 완료). 이동 끝은 마지막 걸음이면 항상 `BeginMovementEnd`를 부르고 콜백으로 완료한다.
- **바꾼 테스트:** `CombatViewSystemTests.FakeDisplay`에 기본 구현 5개(`BeginPresentation` 무동작, `CanBeginPresentationEvent`·`IsPresentationComplete` = true, `BeginMovement` 무동작, `BeginMovementEnd` = 바로 완료)를 virtual로 두고 `TickPresentation`도 virtual로 바꿨다. 하위 가짜 6개(`CombatBallistaAttackTests.Display`, `CombatEnemyPresentationTests.PendingEndDisplay`·`Display`, `CombatMortarPresentationTests.Display`, `CombatStandardAttackTests.Display`, `FieldRailwayPresenterTests.OrderedDisplay`)는 계약을 다시 나열하고 `new`·숨김 멤버로 재구현하던 것을 `override`로 바꾸고 나열을 지웠다.
- **동작 동일 근거:** Barrier를 구현하지 않던 가짜(Enemy·Mortar·Standard·Railway)는 예전에도 대기를 건너뛰었고, 이제 기본값 true로 같다. 이동 끝을 구현하지 않던 가짜는 예전에 바로 완료로 넘어갔고, 이제 기본 구현이 콜백을 바로 불러 같은 Tick에 완료한다. `CombatSceneDisplay`는 원래 다섯 계약을 모두 구현했으므로 운영 동작 변화가 없다.
- **R2.4a 회귀:** `CombatSceneEventPresenterTests.Reinstall_EndsMovementPlanning_GhostOpacityAndStaleRangeDoNotSurvive`. 실제 Combat 씬에서 이동 범위를 보이고 편집을 시작한 뒤(고스트·원본 흐림) 정리 없이 다시 Install하면, 고스트(`_active`)가 없고 원본 흐림 대상(`_movementOpacityActor`)이 비며, 새 공격 범위를 그린 뒤 `ClearMovementRange`를 불러도 공격 범위가 남는지(옛 `_hasRange`가 남지 않았는지) 본다. Install 직전 `_movementPlanning.Clear`를 빼면 세 검사 모두 실패하는 구조다.
- **R2-2로 미룬 것:** `CombatSceneEventPresenter`의 개별 계약 나열(재생 객체 분리와 함께 정리).
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 바꾼 파일의 새 경고 0(남은 `FakeDisplay.PointerOverUi` CS0649는 기존). extra.targets 중복으로 CS2002 3건(R1 파일이 이미 csproj에 있음, 빌드 확인 방식의 부산물). DLL에 `ICombatEventPlayback` 포함. **Unity EditMode는 사용자 대기.**

### 6.6 R2-2 (2026-10-14, 완료 — Unity 확인)

- **R1·R2-1 확인:** 사용자 Unity 확인 완료 통보(2026-10-14).
- **새 파일(각 .meta 새 GUID):**
  - `ViewSystem/Combat/CombatSceneEventPlayback.cs`(`ICombatEventPlayback` 구현): `CombatSceneDisplay`의 재생 부분(옛 :595-700)을 순서 그대로 옮겼다. 기차 접촉(`CombatRailwayContactPlayback`)·`_presentationVersion`·실패 정리(`CleanupFailedPresentation`)·`CancelPresentation`(접촉 → 사건 → 환경 효과 → 레일 → 가시밭, 옛 순서 그대로)·`CancelMovement`를 소유한다. 사건 Presenter·레일 표시·필드·환경 효과·가시밭은 참조만 받는다. 정리 실패 메시지 접두어는 `[CombatSceneEventPlayback]`(이 문구를 확인하는 테스트 없음).
  - `ViewSystem/Combat/Display/CombatDevLayerPanelBinding.cs`(`IUIInputReceiver`): 개발 패널 보임·버튼 두 개 슬롯 처리. 씬·SceneRoot 직렬화 필드는 그대로이며 `CombatSceneRoot`의 참조를 받는다.
- **바꾼 운영 코드:**
  - `CombatSceneDisplay`: 재생 계약 멤버는 `CombatSceneEventPlayback`에 넘기고(사건 시작은 그 전에 해제 검사), 개발 패널은 바인딩 객체에 넘긴다. 더 이상 `IUIInputReceiver`가 아니다. 테스트가 리플렉션으로 읽는 필드(`_events`·`_railway`·`_thorns`·`_pointer`·`_fieldConfig`)는 이름·형식을 그대로 두었다(필드 연결 수명은 표시가 계속 정하므로 표시도 참조를 가진다). 파일 전체를 새로 썼고 오디오 호출 줄은 그대로다.
  - `CombatSceneEventPresenter`: 클래스 선언의 계약 7개 나열 → `ICombatEventPlayback`(같은 7개).
- **D8 결론(정리 묶음):** Install 직전 정리와 실패 정리의 차이는 **의도**로 판단했다. Object·Terrain은 Mapper를 붙잡고 스스로 진행하지 않는 수동 표시라 Install 직전에 비우지 않고(바로 뒤 Refresh가 새 Mapper로 다시 그리며 View 재사용), 실패하면 부분 표시가 남지 않게 비운다. 이동 계획 정리는 Install 직전에 이미 했으므로 실패 쪽에 없어도 같다. 그래서 "Mapper를 붙잡고 스스로 진행할 수 있는 표시" 7개를 `_fieldBound` 한 목록으로 정의하고 Install 직전(그대로)·실패(+Object·Terrain)·Clear(+씬 표시 비우기)가 함께 쓴다. 이유는 Install 주석에 남겼다. 묶음을 중첩 실행하지 않고 배열을 이어 붙여(`Combine`) 실패가 예전처럼 한 겹의 `AggregateException`으로 모인다. 소유 자원 해제(ReleaseOwned)는 `Release`의 목록 한 곳, 연출 취소(CancelPlayback)는 `CombatSceneEventPlayback`의 두 메서드다.
  - 순서 변화: Install 직전 = 같음. Install 실패 = 같은 순서에 이동 계획 정리(이미 끝나 무동작)만 더해짐. Clear = HUD 비용·체력 미리보기 정리 두 개가 필드 묶음 뒤로 이동(서로 다른 대상이라 영향 없음). Release·취소 = 같음.
- **D9(해제 뒤 규칙):** 표시 의도 `ClearMovementRange`·`EndMovementPlanning`는 해제 뒤 무시, 집기 `TryPickCell`·`TryPickHandUnit`·`IsOverHandDropZone`·`TryMapPointer`·`TryCellCenterToScreen`는 해제 뒤 false(Raycast 안 함). 사건 시작 `BeginStep`·`BeginMovement`·`BeginMovementEnd`·`ShowReturn`은 해제 뒤 예외를 명시했다. 이 넷은 예전에도 사건 Presenter의 `RequireBound`에서 같은 형식(`InvalidOperationException`)으로 던졌으므로 동작이 같다. `BeginMovement`는 설계 D9 목록에 "확인 누락"으로 적혀 있었는데, 재생 계약(사건 시작 계열)으로 분류해 예외로 맞췄다.
- **테스트:** `CombatPointerMappingTests.AfterRelease_LateDraftAndReturnCalls_AreIgnored_AndUnbindStillAllowed`에 D9 검사 5줄 추가(이동 범위·편집 정리 무시, 손패 집기·드롭 영역·칸 화면 좌표 false). 개발 패널은 기존 `CombatDisplayReuseTests.AuthoredCombatScene_DevLayerButtons_UseInputBinding`이 그대로 검증한다(수신자가 표시에서 바인딩 객체로 바뀌어도 같은 입력 경로).
- **검증:** MSBuild `ProjectL.Tests.Framework`(새 파일 2개는 extra.targets) 오류 0, 바꾼 파일의 경고 0, DLL에 새 형식 2개 포함. 테스트가 읽는 지운 private 필드 없음(`_devPanel`·`_railwayContact`·`_presentationVersion` 등 참조 0건). **Unity EditMode·재생은 사용자 대기(R2.6).**

### 6.7 R3 (2026-10-14, 완료 — Unity 확인)

- **R2-2 확인:** 사용자 Unity 확인 완료 통보(2026-10-14).
- **재생 세대 소유(착수 전 확인):** 보고에서 밝힌 안대로 사건 Presenter가 `_presentationVersion` 하나를 계속 소유하고, 공격·걸음 재생은 생성 때 받은 읽기 함수(`Func<int>`)로만 현재 세대를 확인한다. 각 재생이 세대를 따로 두는 안은 같은 사실(현재 재생 세대)을 세 곳에 두게 되어 SSOT에 어긋나므로 쓰지 않았다. 세대를 올리는 곳은 예전처럼 `CancelMovement` 한 곳이고, 모든 확인 지점도 그대로다.
- **새 파일(각 .meta 새 GUID):**
  - `ViewSystem/Combat/CombatAttackPlayback.cs`: 공격 4종 시작(`TryBegin`, 종류 분기는 이 안 한 곳)·Tick(`Tick(deltaTime, fieldBound)` — 재장전 완료·세대 변경·Ballista 아닌 공격 진행 중이면 false로 그 프레임 중단)·취소·해제, 투사체 풀, Ballista 무기 집합·재장전(`TryBeginReload`), 관통 대응(`CombatBallistaImpactMap`), `BlocksNextEvent`·`CanBegin`·`IsComplete`. 효과음은 시작 때 받은 콜백(`() => PlayAudio(attack)`)으로만 낸다.
  - `ViewSystem/Combat/CombatUnitMotionPlayback.cs`: 걸음 시작·회전·보간·끝(`BeginStep`·`Tick`), 이동 시작 효과음 시점(`NotifyMovementStarted`), 경로 끝 방향 복귀(`BeginFacingRestore`), `Cancel`. 표시 사실 반영(`ShowCharacter`)과 이동 효과음(`PlayMovementSound`)은 사건 Presenter의 메서드를 콜백으로 받는다. Mapper는 호출마다 인자로 받아 사건 Presenter의 연결 상태 하나만 원본으로 둔다.
- **바꾼 파일:**
  - `CombatSceneEventPresenter`(818 → 482줄): 사건 분배·표시 사실·적 간격 대기·피격/사망 반응·효과음·재생 세대만 남겼다. 오디오 메서드(`PlayAudio`·`PlayUnit`·`UnitPreset`·`AttackPreset`)는 본문 그대로다. 테스트가 리플렉션으로 읽는 `_reaction`·`_audio`·`_shownCharacters`는 이름을 유지했다. 공격 효과음 클로저는 공격 사건일 때만 만들도록 `TryBeginAttack` 도우미에 두었다.
  - `FieldUnitPresenter`: `TryGetBallistaWeapon(id, out weapon)` 추가(예외 없는 판). 턴 종료 재장전이 이 조회를 써서 `_ballistaWeapons` 전체를 도는 `GetComponent<FieldUnitView>()` 루프가 없어졌다. 행위자 View 하나의 `GetComponent<BallistaWeaponView>()`는 남는다(턴 종료마다 1회).
- **동작 동일 근거:** 재장전 대상은 예전에 "이 연결에서 발사한 무기 중 지금 그 행위자가 빌린 것"이었고, 지금은 "그 행위자가 빌린 무기 중 장전되지 않은 것"이다. 풀 반환(`ResetForPool`)이 무기를 장전 상태로 되돌리므로 장전되지 않은 무기는 항상 이 연결에서 발사한 무기라 두 조건이 같다. 세대 확인은 같은 값을 읽는다.
  - 정리 순서 변화(서로 다른 대상이라 영향 없음): `CancelMovement`에서 반응 취소가 투사체·무기 정리 뒤로, `Release`에서 반응 해제가 투사체 해제 뒤로 이동. 공격 상태 초기화가 반응 실패 표시 초기화 뒤로 이동. 걸음 끝에서 표시 사실 반영이 걸음 상태 초기화 뒤로 이동(그 사이 읽는 곳 없음).
  - 공격 쪽 예외 문구 접두어가 `[CombatAttackPlayback]`으로 바뀌었다(이 문구를 확인하는 테스트 없음).
- **R3.3a 회귀:** `CombatBallistaAttackTests.ReRentedWeapon_ReloadsOnlyForCurrentOwner_OncePerFrame_AndCancelEmptiesWeaponSet`. A 발사 → A View 반환(무기 장전으로 초기화) → B가 같은 무기 객체 대여(`SameAs` 확인)·발사 → A의 턴 종료는 바로 완료·재장전 없음 → B의 턴 종료는 기존 재장전 시간 그대로(.41 시작, .17 진행 중, .02 완료) 한 번 완료 → 취소 뒤 무기 집합 0. 무기를 한 프레임에 두 번 진행하면 .17 시점에 이미 끝나 실패하는 구조다.
- **검증:** MSBuild `ProjectL.Tests.Framework`(새 파일 2개는 extra.targets) 오류 0, 바꾼 파일의 경고 0, DLL에 새 형식 2개 포함. **Unity EditMode·재생은 사용자 대기(R3.4).**

### 6.8 R4 (2026-10-14, 완료 — Unity 확인)

- **R3 확인:** 사용자 Unity 확인 통보 없음(R3.4 대기 유지). 사용자 "R4 진행" 지시로 착수했다.
- **첫 구현과 재결정:** 처음에는 `CombatTimelineUiSync`(판단·캐시)를 만들고 `CombatViewSystem`이 첫 표시·결과 후처리·Detach·StopInput에서 불렀다(MSBuild 통과). 사용자 질문("이 객체가 `CombatViewSystem`에 있는 것 자체가 문제 아닌가")으로 다시 검토했다. 판단만 분리되고 소유·시점이 조정자에 남아 P10이 반만 풀린 상태였다. (b) 독립 View 시스템과 (c) `TimelineUI` 자체 결정을 비교해 사용자가 (b)를 골랐다. (c)는 씬 연결 때 UISystem이 열리지 않은 UI를 숨겨 `Update`가 멈추고, FW-008상 연결 중 스스로 열 수 없으며, 외부 열기·오류 닫기가 남는다. `TimelineUI`의 중복 단계 검사는 현강님 메모로 요청한다(사용자 결정). 설계 D11을 갱신했다.
- **새 파일:** `ViewSystem/Combat/CombatTimelineViewSystem.cs`. 미커밋 첫 구현 파일 `CombatTimelineUiSync.cs`(.meta 포함)를 이름을 바꿔 대체했다. 일반 C# 클래스라 GUID 의미는 없다.
  - 보임 = 전투가 있음 ∧ 씬 연결 때의 전투와 같음 ∧ `StagePhase == Combat` ∧ `Phase != Finished`(`ShouldShow`).
  - Combat 씬 Enter(100)에서는 연결(전투 번호 기억)만 하고 Tick이 연다. Exit(-100)·`Shutdown`에서 닫고 연결을 끊는다. 오류 정지(`StopInput`)에서 닫고, 오류 뒤 Tick·재연결은 하지 않는다.
  - 표시 안 된 결과가 있으면(P < S) 지난 보임을 유지한다. UI 서비스가 없는 조립에서는 아무것도 하지 않는다. UI 호출이 실패하면 캐시를 바꾸지 않아 다음에 다시 시도한다.
- **바꾼 파일:**
  - `CombatViewSystem`: 타임라인 필드·생성, `ShowBattle`·`FinalizeResult`의 호출과 그 뒤 세대 확인 1개, `Detach`·`StopInput`의 닫기, `SyncTimeline`·`CloseTimeline`, `GlobalSystem.UISystem` using을 지웠다. 이제 UI 서비스를 참조하지 않는다. `FinalizeResult`는 R4 전(현강님 호출 이전 형태)과 같은 순서가 됐다.
  - `InGameViewSystemFactory`: 타임라인 시스템을 만들어 전투 View 바로 다음에 등록하고, 정리 콜백에 `StopInput`을 넘긴다. 호진님도 자주 고치는 파일이라 병합 때 이 몇 줄이 충돌할 수 있다.
  - `InGameViewFaultCleanup`: 선택 인자 `combatTimeline`을 더했다(전투 입력 정지 바로 다음 호출, `Disconnect`에서 해제). 기존 호출(인자 3~4개)은 그대로 컴파일된다.
- **시점 근거:** View 그룹 Tick은 등록 순서다. 전투 View가 `AcknowledgePresented`한 그 Tick 안에서 타임라인 시스템이 P = S를 보고 맞추므로, 원래(결과를 그린 직후·확인 전)와 같은 프레임이다. 재연결 때 표시 안 된 결과가 남아 있으면 그 연출 뒤에 연다. 첫 구현은 첫 표시 즉시 열었다.
- **현강님 코드 대비 동작 변화(메모 §3):** 재연결 때 열림, 씬 해제·종료·오류 정지 때 닫힘, 판단 근거가 명령 종류에서 상태로 바뀜. 정상 흐름은 같다.
- **R4.2 메모:** [현강님 공유](../handoffs/2026-10-14-combat-timeline-ui-hyeongang-share.md)(미전달).
  - §4.1 `TimelineUI.RefreshTimeline`의 단계 검사(Completion·Finished·StagePhase) 중복 제거 요청. `Completion` 조건은 시스템에 없으므로 완료와 Finished가 함께 오는지 확인을 부탁했다.
  - §4.2 목록이 연출보다 먼저 바뀌는 문제(별도 후보, R4 전부터 있던 동작, VR6과 다름).
- **R4.1a 테스트:** `CombatTimelineViewSystemTests` 10개 메서드, 케이스 포함 16건.
  - 보임 진리표 5건, Combat 씬 Enter/Exit 등록·우선순위
  - 연결 전·연결 노드에서는 열지 않음과 같은 값 재호출 없음
  - 밤 종료·전투 종료 결과에서 연출 확인 뒤에만 열고 닫음(2건)
  - 전투 정리·다른 전투에서 닫히고 다시 열지 않음(2건)
  - 씬 Exit 닫기·Shutdown 무호출
  - 실제 `InGameViewFaultCleanup` 경로의 오류 정지 닫기와 오류 뒤 재연결·재열기 없음
  - 닫기 실패 뒤 재시도, UI 서비스 없는 조립
  - Factory에서 전투 View 바로 다음 등록
- **검증:**
  - MSBuild `ProjectL.Tests.Framework`는 오류 0이고, 바꾼 파일의 경고도 0이다. extra.targets에서 Unity csproj에 남은 옛 `CombatTimelineUiSync.cs` 항목은 Remove하고, 새 파일 2개는 Include했다. Unity가 csproj를 다시 만들면 옛 항목은 사라진다.
  - DLL에는 새 형식만 있고 옛 형식은 없다.
  - Unity 내장 Mono 반사 실행기 결과(Unity Test Runner와 다른 증거다):
    - `CombatTimelineViewSystemTests` 16/16 통과
    - `InGameViewFaultCleanupTests` 순수 3/3 통과. 나머지 5건은 GameObject가 필요해 Unity 대기다.
  - **Unity EditMode·재생(밤 진입·밤 종료·승패·포기·재연결)은 사용자 대기(R4.4).**
- **D12(R4.3):** 미승인이라 진행하지 않았다. 진행 여부를 사용자에게 묻는다.
- **작업 트리 덮어쓰기와 복구(같은 날):**
  - 15:28:05에 Unity가 csproj를 다시 만들었고, 같은 초에 `CombatViewSystem.cs`·`CombatSceneDisplay.cs`가 외부 프로그램(IDE로 추정, 확인하지 못함)에 의해 저장됐다.
  - `CombatViewSystem.cs`는 첫 구현(`CombatTimelineUiSync` 사용) 내용으로 돌아가 컴파일이 깨졌다. (b) 수정을 다시 적용했다.
  - `CombatSceneDisplay.cs`는 R2-2 작성본과 빈 줄 하나의 공백만 달랐고, 이를 되돌려 R2-2 작성본과 같게 했다.
  - 다시 확인한 결과: MSBuild 오류 0, 새 테스트 16/16. 새 csproj에서 옛 `CombatTimelineUiSync.cs` 항목은 사라졌다.
- **R3·R4 확인:** 사용자 Unity 확인 완료 통보(2026-10-14). R3.4·R4.4 완료.

### 6.9 D12 재결정 (2026-10-14, 문서만)

- **경위:** 처음 안은 가시밭 프리팹을 필드 표시 설정(`StageFieldViewData`)으로 옮기는 것이었다. 사용자가 "가시밭은 특성에 따라 생성되는데 옮기는 게 맞나"고 물어 Core를 다시 확인했다. 가시밭 칸 목록(`ThornFields`)은 가시밭 특성(`ThornFieldEffectRule`)만 만들고, 칸 정보에는 원인이 없다.
- **비교:** 특성 외형(`TraitVisual_ThornField`), 필드 표시 설정, 가시밭 전용 표, View 종류 키 표, 씬 `CombatSceneRoot`를 비교했다.
  - 특성 외형은 UI 표에 3D 필드가 섞이고, "가시밭 칸 = 특성 결과"라는 대응을 가정해야 한다. 사용자가 이 약점을 지적해 다른 안을 찾았다.
  - 레일(`RailwayVisual`, 설정 하나) 선례를 따라 가시밭 칸 자체를 외형의 주인으로 보는 전용 표가 두 약점을 모두 피한다.
- **전용 표의 추가 단점(코드로 확인, 설계 D12 "비용과 조율"):**
  - `IViewQuery` 가짜 4개(3개는 호진님 파일)
  - 표 에셋이 없으면 부팅 전체 실패 → 코드·에셋·그룹 등록을 한 커밋으로
  - `ViewGroup.asset` 공유 YAML 충돌
  - 스테이지별 모양 불가(레일과 같음)
  - `CombatDisplayTables` 직접 생성 3곳
  - 프리팹 메모리와 보관 GameObject는 지금과 같아 단점이 아니다.
- **결정:** 사용자가 전용 표(B)를 골랐다. 설계 D12·Phase 표·SSOT 행과 계획 R4.3을 다시 썼다. 프리팹은 DB 단계에서 필수로 하지 않는다. 누락은 `DataBaseAssetTests`가 잡고, 런타임은 가시밭이 나올 때의 기존 예외를 유지한다(Claude 제안, 설계에 기록).
- 코드는 바꾸지 않았다.

### 6.10 R4.3a 새 표 추가 (2026-10-14, 코드·에셋 완료·R4.3b 대기)

- **착수:** 사용자 "R4.3a 진행" 지시.
  - 에셋은 Claude가 YAML로 직접 썼다. 레일 표·항목 에셋과 같은 형식이다. 착수 때 정한다고 했던 항목이다.
  - 호진님 공유는 [메모](../handoffs/2026-10-14-thorn-field-visual-table-hojin-share.md)로 남겼다(미전달, 전달은 사용자).
- **새 코드(각 .meta 새 GUID):**
  - `DataBaseSystem/Data/View/Table/ThornField/Item/ThornFieldVisualDataSO.cs`(script guid `6eda9417…`)
    - `ThornFieldVisualData`: `prefab` 하나, null 허용
    - `ThornFieldVisualDataSO`
  - `…/ThornField/ThornFieldVisualDataTableSO.cs`(script guid `54993848…`)
  - `Queries/ViewDataBase/ViewTable/ThornFieldVisualTable.cs`
    - `IThornFieldVisualTableQuery.Config`
    - 레일처럼 항목 정확히 하나를 요구한다. 프리팹은 검사하지 않는다(설계 D12).
  - 폴더 메타 `ThornField`·`ThornField/Item`
- **바꾼 코드:**
  - `IViewQuery`·`ViewDataBase`: `ThornFieldVisualTableQuery`
  - `ViewTableKey.ThornFieldVisual`
  - `ViewDataBaseFactory`: 레일 다음에 생성·등록
- **새 에셋:**
  - `DataBaseSO/ViewGroup/ThornFieldVisual/ThornFieldVisualDataTable.asset`(guid `2ff46812…`)
  - `…/Item/ThornFieldVisual_Default.asset`(guid `f91b54de…`)
    - 프리팹: `{fileID: 4985872456650278110, guid: 09fc9d774997b0349b84c5298e5ceca1}`, 즉 `Prefabs/Combat/ThornCactus.prefab` 루트
    - `CombatAttack_CactusCannon.asset`의 `thornPrefab`과 같은 값이다.
  - 폴더 메타 2개
  - `ViewGroup.asset`의 `databaseTables` 끝에 한 줄 추가(CRLF 유지)
- **테스트:**
  - `IViewQuery` 가짜 4개에 한 줄씩: `ArtifactPopupConfigTests`·`LobbyInputTests`·`UnityStageInputTests`는 `=> null`, `LobbyViewSystemTests`는 기존 레일 줄처럼 `NotSupportedException`
  - `DataBaseAssetTests.Factory_PreservesExistingViewTables`에 새 조회 추가
  - View 그룹을 손으로 조립하는 테스트는 없다. 모두 실제 `ViewGroup.asset`을 복제하므로 새 표가 자동으로 포함된다. 손으로 조립하는 것은 InGame 그룹뿐이다.
- **아직 그대로(R4.3c에서 바꿈):** `CombatSceneDisplay`의 `Unit_CactusCannon` 조회, 옛 `thornPrefab`, Mortar 필수 검사.
- **검증:** MSBuild `ProjectL.Tests.Framework`(새 파일 3개는 extra.targets) 오류 0, 바꾼 파일 경고 0, DLL에 새 형식 5개.
  - 에셋 로드와 DB 생성은 Unity에서만 확인할 수 있다(`DataBaseAssetTests`는 AssetDatabase 사용). **R4.3b 사용자 확인 대기.**

### 6.11 R4.3c·R4.3d 사용처 이전·옛 필드 제거·테스트 (2026-10-14, 완료 — Unity 확인)

- **R4.3b 확인:** 사용자 Unity 확인 완료 통보(2026-10-14). R4.3c를 지시받았다. 옛 필드를 지우면 Mortar 테스트가 컴파일되지 않으므로, R4.3a 보고에서 밝힌 대로 R4.3d 테스트 갱신을 함께 했다.
- **런타임:**
  - `CombatDisplayTables`: 생성자 `railway` 다음에 `thornField`(`ThornFieldVisualData`, null 금지)를 넣고 `ThornField` 속성을 더했다. `From`은 `ThornFieldVisualTableQuery.Config`를 읽고, 없으면 레일과 같은 형식으로 예외를 던진다.
  - `CombatSceneDisplay`:
    - `Unit_CactusCannon` 외형 조회와 Mortar 종류 확인을 지웠다.
    - `tables.ThornField.Prefab`이 있으면 `FieldThornPresenter`를 만든다. 없으면 만들지 않아, 가시밭이 나올 때의 기존 예외("가시밭 표시 프로필이 연결되지 않았습니다")가 그대로 동작한다.
    - 쓰지 않게 된 `CombatPresentation` using도 지웠다.
  - `CombatAttackPresentationSO`: `thornPrefab` 필드, `CreateData` 인자, Mortar 필수 검사를 지웠다. 헤더는 "곡사 포탄"이 됐다.
  - `CombatAttackPresentationData`: `ThornPrefab`(생성자 인자·속성)을 지웠다.
- **에셋:** `CombatAttack_{Ballista,CactusCannon,Linear,Melee}.asset`의 `thornPrefab:` 직렬화 줄을 지웠다(CRLF 유지). Unity가 다음 저장 때 지울 값이지만, 남기면 CactusCannon이 아직 가시밭을 참조하는 것처럼 보이고 다른 사람의 커밋에 섞인다. 이제 `ThornCactus.prefab`(guid `09fc9d77…`)을 참조하는 DB 에셋은 `ThornFieldVisual_Default` 하나뿐이다.
- **테스트(R4.3d):**
  - `CombatAttackPresentationSettingsTests`:
    - `MortarSettings_RequireSeparateThornPrefab`을 삭제했다. 필드가 없어져 반대 검증은 형식이 보장한다. Mortar가 가시밭 없이 만들어지는 것은 기존 종류별 검사가 확인한다.
    - 그 밖의 `thornPrefab` 설정과 복사·직렬화 비교를 지웠다.
  - `CombatMortarAnimationTests`·`CombatMortarPresentationTests`: `thornPrefab` 설정 줄을 지웠다. 연출 테스트는 `FieldThornPresenter`를 직접 만들어 그대로 쓴다.
  - `CombatMortarAssetTests`: `_thornPrefab = ThornFieldVisualTableQuery.Config.Prefab`로 바꿨다. 운영 Catalog에서 새 표의 프리팹이 `ThornCactus.prefab`(guid 확인)인지 검사하므로, R4.3b 확인이 회귀 테스트로 남는다.
  - `CombatSceneEventPresenterTests` 1곳·`TerrainOperationalDisplayTests` 2곳: `CombatDisplayTables` 직접 생성에 `views.ThornFieldVisualTableQuery.Config`를 넣었다(CRLF 맞춤).
  - `DataBaseAssetTests.ThornFieldTraitUnits_HaveThornFieldPrefabAsset`(새): InGame에 `ThornField` 특성 유닛이 있으면 프리팹이 있고 프리팹 에셋이어야 한다.
- **부수 문서(확인 대기):** 계획에 적은 "DB-SO 설계 표 스펙·§11.2"는 하지 않았다. 그 문서는 첫 이전 때의 표 5개(§6.2~6.6)만 다루고, 이후 추가된 레일·특성·지형·오브젝트·로비·아티팩트·입력 설정 표는 적혀 있지 않다. 가시밭 표만 넣으면 문서가 일관되지 않는다. 표 스펙은 이 설계 D12에 있다. 사용자에게 확인한다.
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0. 새 경고는 CS2002뿐이다(Unity가 csproj에 새 파일을 넣은 뒤 extra.targets가 다시 넣어 생긴 중복).
  - 바꾼 테스트는 모두 GameObject·AssetDatabase·ScriptableObject가 필요해 Unity 밖 실행기로는 돌리지 못했다.
  - **Unity EditMode·재생은 사용자 대기.**
- **R4.3c·d 확인과 마무리:** 사용자 Unity 확인 완료 통보(2026-10-14). 부수 문서(DB-SO 설계 표 스펙·§11.2)는 사용자 결정으로 빼고, 이 표의 스펙 원본은 설계 D12로 둔다. D12와 R0~R4 전체가 끝났다.

### 6.12 삭제된 Timeline UI 호출 정리 (2026-10-16, 완료)

- 사용자 범위: 삭제된 Timeline UI를 여는 코드 정리, 다른 전투 로직 유지, 관련 테스트·문서 갱신 뒤 중지. `993b0068`에서 Combat 씬의 Timeline UI가 삭제됐으나 D11의 열기·닫기 시스템이 남아 밤 진입 때 미등록 UI 오류가 발생한다.
- 한 Phase의 계획:
  - [x] T0 실제 Boot → Combat(그래프만 고정한 운영 DB 정의) → 메인 배치 → 밤 진입 회귀 테스트로 기존 오류 재현(RED).
  - [x] T1 `CombatTimelineViewSystem`과 `.meta`, Factory 생성·모듈 등록·오류 정리 연결, `InGameViewFaultCleanup`의 전용 콜백 제거. 폐기된 시스템 테스트를 제거하고 나머지 화면의 오류 정리 순서·예외 집계를 유지한다.
  - [x] T2 같은 Boot 회귀와 전투 View·입력·재생·오류 정리 관련 EditMode 검증(GREEN).
  - [x] T3 D11 현행 상태·본 실행 기록·현강님 공유 메모 갱신, 변경 범위와 임시 도구 제거 확인, 보고 후 중지.
- 당시 보존: Core의 Timeline 데이터·조회·전투 규칙, `CombatViewSystem`의 입력/연출/결과 소비, 기존 씬·에셋, `TimelineUI.cs`·`UIId.Timeline` 식별자. 이후 UI 작업자가 두 식별자도 삭제한 후속 인수는 §6.13을 따른다. UI 재도입 설계는 포함하지 않는다.
- 구조 점검: 삭제된 UI의 보임 상태·Tick·콜백을 함께 제거해 SSOT 잔재를 없앤다. Factory는 조립만, 공용 정리 소유자는 남은 각 화면의 정리와 예외 집계만 맡는다. FW 모듈·씬 수명을 우회하는 대체 경로는 추가하지 않는다.
- 구현: 제품 변경은 Factory·공용 오류 정리 소유자·삭제된 Timeline 전용 시스템에 한정했다. `CombatTimelineViewSystemTests`의 폐기된 기능 16건을 제거하고, `InGameViewFaultCleanupTests`는 남은 4화면의 호출 순서·여러 실패·정리 중 Disconnect 검증을 유지했다.
- 새 회귀 `CombatTimelineRetirementBootSmokeTests`: 실제 Boot·Combat 씬·Database/Live·공개 View 입력 Buffer로 메인 배치와 EnterCombat을 수락받고 각 결과의 표시 완료를 확인한다. 비활성 오브젝트를 포함해 TimelineUI 부재를 확인하고 밤 진입 뒤 5프레임 동안 오류 정지·예상하지 않은 오류 로그를 검사한다. Core 상태·결과·표시 확인 값에 직접 쓰지 않는다. 원래 Editor 설정·씬 구성과 백그라운드 실행 값을 복원하고 테스트 소유 SO 복사본은 해제한다.
- 테스트 공용화: `LobbyBootSmokeTests`의 기존 Combat 그래프 준비를 `ConfigureCombatGraphBoot`로 추출하고 기존 복사본 해제 helper를 internal로 공개했다. 기존 Fixture 테스트의 실행 모드·입력 값은 그대로다. 새 검사는 현재 로스터를 지원하는 Database 정의로만 덮어쓴다.
- 검증(Unity 6000.0.73f1, 열린 Editor의 TestRunnerApi):

  | 대상 | 결과 |
  |---|---|
  | 실제 Boot → 메인 배치 → 밤 진입 회귀 | **1/1 통과**, 최종 `green-3.xml` |
  | `CombatViewSystemTests` | **79/79 통과** |
  | `CombatInteractionTests` | **68/68 통과** |
  | `CombatInputPolicyTests` | **48/48 통과** |
  | `CombatResultPlaybackTests` | **14/14 통과** |
  | `InGameViewFaultCleanupTests` | **14/14 통과** |
  | `CombatCommandGateTests` | **5/5 통과** |
  | 관련 EditMode 합계 | **228/228 통과**, `related-1.xml` |

- RED/GREEN 원본: `.superpowers/sdd/2026-10-16-timeline-ui-retirement/`(로컬 검증 산출물, Git 제외). `red-2.xml`에서 제품 수정 전 정확한 `UIId: Timeline` 미등록 오류를 재현했다. 최종 `green-3` 실행 로그에는 Timeline 오류·닫기 경고·예외가 없다. 전투 진입뿐 아니라 밤 단계까지 검사한 증거이며, 전투 전체 승패·수동 Play·전체 EditMode 완료를 뜻하지 않는다.
- 검증 도구 보정 이력: 첫 `red-1`은 기존 Fixture가 현재 로스터의 `Unit_CactusCannon`을 지원하지 않아 밤 진입 전에 실패해 Database로 바꿨다. `red-2`와 첫 `green-1`에는 중첩 IEnumerator의 ExitPlayMode 정리 오류도 있어 직접 yield하는 정리로 고쳤다. `green-1`의 표시 확인 2/최신 3 불일치는 자동 전투 결과가 이어진 정상 상황으로, 제출한 결과 이상이 표시됐는지를 검사하도록 보정했다. 독립 리뷰에서 같은 조건을 Submit helper에도 적용해 최종 `green-3`을 다시 통과시켰다. 제품 전투 로직은 이 보정 때문에 바꾸지 않았다.
- 기존 경고: `LobbyDepartureIntegrationTests.cs:46`의 중복 using(CS0105), Play의 FMOD Listener 경고가 남아 있다. 최종 테스트의 컴파일 오류·새 코드 경고는 없다. 반복 검증 전체의 알려진 DB 3건·기존 Boot smoke 실패를 수정하거나 전체 재검증한 범위가 아니다.
- 독립 리뷰: 제품 코드의 불필요한 변경·수명 계약 위반은 발견되지 않았다. S/O/L/I/D는 남은 시스템의 책임·확장점·계약·좁은 의존성을 유지하고, 이번에는 삭제한 UI의 조립 연결만 함께 폐기했다. Core·FieldCamera·Architecture는 수정하지 않았다.
- 마무리: 임시 Editor 검증 도구와 `.meta`를 로컬 산출물 폴더로 보관한 뒤 Assets에서 제거했다(`Assets/Editor.meta`는 보존). 이전 NM3 작업본·Combat 씬·ProjectSettings/BuildSettings 등 보존 대상 13파일의 SHA-256이 동일하다. 새 테스트 GUID는 유일하고 삭제한 두 스크립트 GUID의 에셋 참조는 0이다. 갱신 문서의 상대 링크 14개·UTF-8/LF·`git diff --check`를 확인했다. 커밋·push·외부 전달 없이 이 Phase에서 멈춘다.

### 6.13 UI 작업자 삭제 병합 뒤 테스트 컴파일 수정 (2026-10-16)

- 사용자 요청: 머지 뒤 Unity에 나온 TimelineUI 관련 테스트 오류를 UI 작업자 의도에 맞춰 수정한다. 시작 HEAD는 `0923f13e`다. UI 작업자의 `053e151c`는 `TimelineUI.cs`·`UIId.Timeline`·전용 View 시스템과 등록을 삭제했고, `b51b584e`는 대체 타임라인을 추가하지 않았다. §6.12의 식별자 보존은 이 병합 전 이력이다.
- 원인: `CombatTimelineRetirementBootSmokeTests.cs`의 `FindObjectsByType<TimelineUI>` 한 곳이 삭제된 타입을 참조해 CS0246이 발생했다. 타입·UI ID를 복구하지 않고, 비활성 오브젝트를 포함한 실제 MonoBehaviour의 전체 타입 이름으로 기존 UI 부재를 검사하도록 이 단언만 이전했다. 불필요해진 using도 제거했다. 기존 Boot/운영 DB/Live·명령 수락·결과 표시·밤 단계·5프레임 오류/로그·Editor 상태 복원 검사는 유지한다.
- 컴파일 재현·검증: 설치된 Unity 6000.0.73f1의 Roslyn과 현재 Unity 생성 rsp를 사용하고 출력 DLL/ref만 Git 제외 폴더로 분리했다. 테스트 어셈블리 **310소스**에서 수정 전 CS0246 1건(exit1), 수정 후 **오류 0건(exit0)**이다. 기존 `LobbyDepartureIntegrationTests.cs:46`의 중복 using 경고 CS0105 1건은 두 실행에 동일하다. 컴파일 확인이며 실제 Unity 재로드·Boot Play/전체 EditMode 실행 결과로 확대하지 않는다. §6.12의 228/228은 당시 실행 기록이다.
- 범위: 기존 테스트 1개와 현재 계약을 명확히 하는 설계/계획 2문서만 수정한다. Core/Buffer/Framework·UI 제품 코드/ID·Camera/씬/에셋/meta는 유지한다. 외형·규칙·조립의 소유권이나 SSOT/SOLID·FW 수명은 변경하지 않는다. 새 서비스·캐시·등록·에디터 도구는 만들지 않았다.
- 증거: `.superpowers/sdd/2026-10-16-timeline-ui-merge-fix/`의 시작 해시·Editor 로그, `red/compiler.log`·`green/compiler.log`·각 `result.json`, 최종 범위/문서 확인과 독립 검토를 보존한다. 이 수정에서 보고하고 멈추며 스테이징·커밋·push·외부 전달은 하지 않는다.
