# 비전투 이벤트·회복 계획

- 작성자: 호진(이호진)
- 상태: V1~V4·ER1~ER6 끝(2026-10-11~10-15, 마지막 사용자 커밋 `51f5a892`). 확인은 에이전트 batch EditMode·Core fixture·출력 검증까지이고, 사용자 Unity 확인·사람 직접 조작 확인은 기록이 없다. 남은 것은 사람 확인, U0 후속, 정식 아트다.
- 설계: [비전투 이벤트·회복 설계](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md)
- 정리: 2026-10-27 `plans/2026-10-11-noncombat-event-ui-plan.md`(V1~V8 Task 계획)·`plans/2026-10-13-stage-event-recovery-entry-plan.md`(ER1~ER6 Task 계획)와 이 문서의 옛 1~4부(ER5 선행 점검, ER6 운영 경로 인계 포함)를 합쳤다. 끝난 Task 단계·시험 수치·실행 명령·증거 경로(`.superpowers/sdd/…`)는 git `10914570`.

## 1 진행 요약

V5~V8은 ER3~ER6과 같은 작업이다(2026-10-13 Event 연결 계획이 옛 V5~V8을 보완해 이어받음).

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| V1 | 공용 카드에서 상점 ID 분리 | 완료(10-11) | batch 관련 회귀·운영 상점 Play smoke 통과 | `fb3f418a` |
| V2 | 상점 모달 입력 수명 추출(`StagePopupSession`) | 완료(10-11), 뒤에 공용 `PopupViewScope`로 대체 | batch 통과 | `fb3f418a` |
| V3 | 회복 외형 칸·표시 프리팹 `RecoveryPanel` | 완료(10-11) | batch 통과, 자동 미리보기 PNG | `fb3f418a` |
| D0 선택지 | `Event_Recovery`에 옵션 3 연결(1·2·3) | 완료(10-11) | batch 통과, 실제 에셋 교차 검증 | `fb3f418a` |
| V4 | 회복 입력 Buffer | 완료(10-12) | batch 통과, Core 병합 `58c2f12e` | `3aad1e83` |
| ER1 | Stage/Run origin(교체 직전 Stage 입력 Clear) | Stage·Lobby 완료(10-14) | batch 통과 | `662da3dd` |
| ER2 | Core RC3-B(Event 진입)·RC4(통합 인수) | 천명님 완료(10-14), View 인수 | Core 시험 | `92440b83`(천명님) |
| ER3 = V5 | runtime 요청 수명·Flow·Plan·Job | 완료(10-15) | batch 통과, 새 실패 0 | `cb08fc77` |
| ER4 = V6 | 팝업 UI·Controller·Factory | 완료(10-15) | batch 통과, 새 실패 0 | `cb08fc77` |
| ER5 = V7 | 상점 입력 정책으로 운영 프리팹·씬 연결 | 완료(10-15) | batch 통과, 3열 렌더 확인 | `51f5a892` |
| ER6 = V8 | 운영 Boot 경로·Core fixture·출력 검증 | 완료(10-15) | batch 통과, 사람 조작 미실행 | `51f5a892` |
| U0 | 글로벌 HUD 전체 입력·초점 차단 | 후속(10-15 사용자 분리) | — | — |

Core 쪽 단계(천명님): RC0 계약·RC1 타입 `5cc2c2b8`·RC2 서비스 `fa618534`(View 병합 `8186a6c1`), RC3-A 번호 처리·결과 보존 `535554e7`(이 브랜치 병합 `56c38c3a`), RC3-B·RC4 `92440b83`.

## 2 남은 일

사람 확인(자동 시험·합성 입력·PNG로 대신 체크하지 않는다):

- [ ] Boot부터 실제 화면에서 reachable Event를 마우스로 선택하고 회복 카드·현재 캡슐·한글 제목/HP·사유가 읽히는지 본다.
- [ ] full HP의 선택 불가 표시와 무반응, 명시적 나가기로 현재 노드만 완료된 뒤 같은 맵으로 돌아오는지 본다.
- [ ] 실제 마우스를 누른 채 맵을 드래그하다 팝업을 열고, 늦은 이동이 카메라를 밀지 않고 닫은 뒤 새 누름으로만 움직이는지 본다.
- [ ] 키보드·패드로 회복 버튼 선택·Submit과 같은 프레임 중복 방지를 본다(글로벌 HUD 초점 가두기는 U0).
- [ ] 다음 비전투 노드로 이동했을 때 옛 회복 노드의 완료 봉과 새 현재 캡슐을 본다.
- [ ] 자연스럽게 피해를 입은 진행에서 유료 선택·잔액 부족 거절·선택 후 나가기·이후 전투 HP 지속을 본다. Core fixture의 무료 옵션 통과를 운영 DB에 무료 옵션이 있다는 뜻으로 읽지 않는다.

후속·보류:

- [ ] U0: UI 담당 공개 lease·Navigation/초점 복원 계약을 받은 뒤 설계·Task([설계 §12.1](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#121-u0-글로벌-hud-전체-입력-차단-후속)).
- [ ] D0 후속: 정식 카드 그림·설명, 회복 텐트 노드 외형. Position·Weight는 기획·Core 확인부터.
- [ ] 상점·회복 "제출 중" 하나·공통 UI 기반(결정 완료·진행 전): [검사 후속 정리 계획](2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md).
- [ ] 공유 메모 전달(§5, 모두 미전달).
- 알려진 한계(할 일 아님): 운영 Run 교체 호출자는 정상 이탈 전제이고 실행 중 임의 교체는 보장하지 않는다(ER1·Core §17). Job 안에서 전체 runtime Shutdown 재진입은 지원하지 않는다(ER3).

## 3 작업별 결과

### 3.1 V1 공용 카드의 상점 ID 분리 (2026-10-11)

- 공용 `StageOfferCardContent`·`StageOfferCardView`에서 `ShopOfferId`를 뺐다. 새 `ShopOfferCardEntry`가 ID와 Content를 묶고, `ShopUI.Render`가 표시 당시 ID를 `_renderedOfferIds`에 복사한다.
- 소비자 조정: `ShopPopupModelBuilder`, 시험 `ShopUITests`·`ShopUITestFixture`·`ShopPopupModelBuilderTests`·`StageShopUIAssetTests`·`ShopStagePlaySmokeTests`(카드 ID 대신 상품 순서·명시 ID). 프리팹 직렬화·GUID는 그대로다.
- 카드 책임은 [상점 화면 작업 안내](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md)에 반영했다. 독립 리뷰 결함 없음.

### 3.2 V2 상점 모달 입력 수명 추출 (2026-10-11)

- `MAP/StagePopupSession.cs`를 만들어 Layer 소유 표식·정상 복귀·오류/이탈 정리를 맡겼다. `ShopPopupController`는 표식만 보관하고 문맥·결과·3초 문구는 그대로 가졌다.
- Begin은 서비스가 반환될 때 목표 Layer가 확립된 경우에만 성공한다(공개 입력 서비스가 보류 요청을 알려 주지 않아서). 다른 Layer 전환 콜백 안의 새 Begin은 지원하지 않는다.
- 시험 조건 보정: 장치 입력 시험은 임시 `InputSettings` 복제본으로 준비하고 원본을 복원한다. `StageMapCameraBootSmokeTests`에 마우스를 누른 채 상점을 여는 사례를 더했다.
- 바뀐 것: `StagePopupSession`은 `ba0c9d7e`(2026-10-15, View 구조 P단계)에서 지워지고 공용 `PopupViewScope`·`PopupInputSession`이 대신한다. 복원하지 않는다.

### 3.3 V3 회복 표시 데이터와 프리팹 (2026-10-11)

- `RecoveryOptionVisualDataSO`에 선택적 `icon`·`description`(`Icon`·`Description`)을 더했다. 그림 null·설명 빈값인 기존 에셋도 유효하다.
- `Prefabs/UI/Recovery/RecoveryPanel.prefab`을 만들었다(전체 화면 포인터 차단, 중앙 패널, 공용 카드 중첩 템플릿 1개 비활성). 새 시험 `RecoveryViewAssetTests`.
- 계획과 다른 점: 실제 표시 검수에서 한글 줄 높이 때문에 Title·Status가 Ellipsis로 통째로 숨는 결함을 찾아 높이만 60→80, 32→40으로 고쳤다.
- 운영 StageSelect·UIId·입력에는 연결하지 않았다(ER5에서 `RecoveryUI.prefab`으로 연결).

### 3.4 D0 선택지 3개 연결 (2026-10-11)

- 사용자 "기획대로 3개". `Event_Recovery.asset`의 1·2 참조 뒤에 등록 테이블의 `InGameGroup/Recovery/Item/RecoveryOption_3.asset`(10G·Full)을 더했다. 값은 바꾸지 않았다.
- `Stage/Event/Recovery/Item`의 중복 SO는 지우거나 고치지 않았다. 당시 `Stage_Event`(1301)는 Stage 테이블에 없었고 2026-10-13에 등록됐다.

### 3.5 V4 회복 입력 Buffer (2026-10-12)

- 사용자 요청 범위. RC0·RC1·RC2를 문서·소스로 인수한 뒤(10-12, HEAD `e3305027`, 브랜치 `feature/Hojin/Stage_Shop_Event`) 구현했다. 계약은 [설계 §5](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#5-회복-입력-buffer-v4).
- 새 `BUF/RecoveryView/RecoveryViewCommand.cs`·`RecoveryViewBuffer.cs`·시험 `RecoveryViewBufferTests`. 공용 세 파일(`BufferSystemGroup.cs`·`BufferSystemFactory.cs`·`DataBaseGroupServiceKey.cs`)은 입력 줄만, 천명님 시험 대역 셋은 호환 줄만 고쳤다.
- 계획과 다른 점: 처음 제안한 상점식 다중 큐를 입력 한 건·정확 번호로 바꿨다(RC0 인수). 10-11에는 RC0·RC1을 확인할 수 없어 V4를 보류했다가 10-12 인수 뒤 진행했다.
- 독립 리뷰 차단 없음. 인계는 [Core 요청·인계 4부](../handoffs/2026-10-09-recovery-core-handoff-Hojin.md#4부-v4-회복-입력-buffer--core-인계).

### 3.6 ER1 Stage/Run origin (2026-10-14)

- Core가 RC3-B를 멈추고 요구한 세 가지(취소 경로·같은 키 새 Run 회귀·origin 정보)를 호진 담당으로 처리했다(2026-10-13 사용자).
- `StageJob`·`StagePlan`·`LobbyRunStartJob`·`LobbyPlan`에 Stage 입력 WriteSink를 주입해 실제 Run·챕터 교체 직전에 Clear한다. 취소된 Lobby 요청과 잔존 전투 거절은 Clear 전에 막는다. 새 시험 `StageRunOriginRegressionTests`(예: `QueuedStageRequest_SameKeysNewRun_DoesNotEnter`, `DefinitionLookup_OriginReplacement_InvalidatesOldNumber`), `LobbyDepartureTests` 생성자 맞춤.
- 남긴 것: 이미 시작된 정의 조회의 옛 결과 게시는 Core 재검사(ER2)가 막는다. Combat 새 Run·`CompleteChapter` 다음 챕터 설치는 정상 이탈·Detach 전제(용근님 영역). 인계는 [Core 요청·인계 6부](../handoffs/2026-10-09-recovery-core-handoff-Hojin.md#6부-er1-stagerun-origin-선행-작업-인계).

### 3.7 ER2 Core 진입 연결 (천명님, 2026-10-14)

- Event 진입 설치·seed 비소비·Confirm 선확정 없음·조회 취소/정의 실패 불변·반복 진입 영수증·같은 키 옛 origin 차단, Shop·Battle·Boss 회귀, 실제 입력 → 진입 → 선택 → 완료 → 다음 전투 통합(RC4)을 천명님이 구현했다. View는 `92440b83`의 서비스·Factory를 대조해 인수했다.
- "Combat 담당 추가 승인이 선행"이라던 옛 해석은 Core §17에서 정정됐다(정상 이탈 전제).

### 3.8 ER3 runtime 요청 수명과 실행 경로 (2026-10-15)

- `VIEW/Recovery/RecoveryRequestLifetime`(+ 역할 인터페이스 셋), `RecoveryFlow`·`RecoveryPlan`·`RecoveryCommandJob`, `JobRootType.Recovery = 5`, `StageFlow` 등록 순서(맵 → 상점 → 회복), 공용 fault 정리 끝의 recovery를 넣었다. 계약은 [설계 §8](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#8-runtime-요청-수명-er3-a2).
- 계획과 다른 점: 옛 V5 예상 서명은 `RecoveryCommandJob(number, core, IRecoveryRequestExecution)`·`RecoveryPlan.Register(service, type)`로 구체화했다. 회복 예약이 없는 종료가 다른 도메인 큐를 지우던 문제를 시험으로 찾아 "자기 예약이 있을 때만 Clear"로 고쳤다.
- 시험: `RecoveryRequestLifetimeTests`·`RecoveryRequestLifetimeIntegrationTests`. 전체 실행에서 기존 실패 외 새 실패 0. 과거 자기 출력 backpressure 시험은 단위 주입이며 실제 Core 통합 증거가 아니다.

### 3.9 ER4 팝업 UI·Controller (2026-10-15)

- `MAP/Recovery/` 다섯 파일과 `UI/RecoveryUI`·`IRecoveryPopupDisplay`·`RecoveryPopupModel`·`RecoveryOfferCardEntry`, `StageMapEntryObserver`의 비전투 복구(`RestoreNoncombat`), `StageMapSceneRoot.recoveryUI` 참조 자리를 만들었다. 계약은 [설계 §9](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#9-팝업-표시복구입력-er4er5).
- 계획과 다른 점: Hide 알림 안에서 다시 열린 새 표시를 옛 finally가 숨기는 결함을 찾아 고쳤다. 리뷰 지적으로 `OwnedMismatch_ClosesOnlyMatchingDisplayedEntry`를 더했다.
- 당시 회복은 `Begin(null)`(게임 액션 전부 끔)이었고 `UIId.Recovery = 16`이었다. 둘 다 ER5·병합 뒤 바뀌었다.
- 시험: `RecoveryUITests`·`RecoveryPopupControllerTests`·`RecoveryPopupModelBuilderTests`·`RecoveryStageMapViewTests`. 새 실패 0.

### 3.10 ER5 상점 입력 정책으로 씬 연결 (2026-10-15)

- 선행 점검(HEAD `95dbf3f5`)에서 U0 공개 API가 없음을 확인했다. 사용자가 "회복도 상점과 같은 입력 정책으로 먼저 연결, 글로벌 UI 전체 차단은 후속"으로 바꿨다.
- `RecoveryPopupController`의 Layer를 null → 상점 팝업 Layer로 바꾸고, `RecoveryPanel`을 바탕으로 `RecoveryUI.prefab`을 만들어 `StageSelect.unity`에 인스턴스 하나와 SceneRoot 참조를 연결했다(Unity Editor API).
- 계획과 다른 점: 3열 렌더에서 제목·HP·두 줄 사유 잘림을 찾아 배치를 고치고 메시 검사 회귀를 더했다. `StageMapDepthCenteringBootSmokeTests`(호진) 기대를 "Event 수락 때 회복 화면이 열림·열린 중 맵 재클릭 요청 0"으로 바꿨다. 다른 작업의 컴파일 오류(`ArtifactDataTableTests.cs:190`)로 잠시 멈췄다가 그쪽이 고친 뒤 이어 했다.
- 시험: 새 `RecoverySceneConnectionTests`·`RecoveryModalInputTests`(실제 클래스는 partial `RecoveryPopupControllerTests`). 남은 실패 2건은 회복과 무관한 기존 DB 실패(`EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition` Stage 1101 회전, `LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride` `LineShader.mat` 없음)다.

### 3.11 ER6 운영 경로 검증 (2026-10-15)

- 새 `StageMapRecoveryEntryBootSmokeTests`(운영 Boot/DB, 개발 fixture 끔)와 `RecoverySceneConnectionTests`의 무료 선택 사례를 더했다. 제품 코드·씬·프리팹·DB는 바꾸지 않았다.
- 확인한 것: 실제 Event 클릭 → 같은 Context로 회복 열림, full HP(50/50·20G)에서 세 선택지 NoEffect·버튼 무반응, 미선택 Complete로 현재 노드만 완료·HP/골드 불변·같은 맵 복귀, 완료 뒤 재연결에서 다시 안 열림, 다음 노드로 가면 봉·캡슐, 누른 채 드래그 취소. 손상 HP 유료(HP 5→8, 10G→7G)·무료(HP 5→10) 선택과 다음 전투 HP 지속은 Core fixture로 따로 확인했다.
- 계획과 다른 점: 드문 Event → 비전투 조합을 찾는 시나리오만 후보 탐색 상한을 64 → 512로 나눴다(seed·게임 상태는 건드리지 않음). batch가 LF로 다시 저장한 `ProjectSettings/TimeManager.asset`은 원래 바이트로 되돌렸다.
- 전체 실행의 실패는 모두 기존 실패다(관련 집합에서는 `ArtifactHealingChaliceIntegrationTests` 3건). 독립 리뷰 필수 수정 0. 사람 직접 조작은 §2로 남겼다.

### 3.12 10-16 뒤 다른 작업이 바꾼 것

회복 동작 계약은 그대로이고, 각 작업 문서가 원본이다.

- 2026-10-19 `58e36c6a`: 상점·회복 카드를 공통 카드 프리팹으로([상점 계획](2026-10-09-stage-shop-plan-Hojin.md)). 지금 회복은 `StageEventOfferDescriptionCard.prefab`을 쓴다(강화 Q-E1).
- 2026-10-26 `ee55d650`: StageSelect Canvas 통합. 회복 화면 참조가 `StageScreenRootUI.recoveryUI`로 옮겨졌다([스테이지 맵 계획](2026-10-10-stage-map-plan-Hojin.md)).
- 2026-10-27 `b1dd4962`: 팝업 Layer가 `StageSelectionInputLayers.Popup`(이름 `"StageMapPopup"`)으로([검사 후속 정리 계획](2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md) C-2).
- 2026-10-27 현강님 `315cb578`: 회복·상점 카드 배치 통일(직렬화 칸 이름 그대로).

## 4 시험 위치

시험 필터는 파일 이름이 아니라 실제 NUnit 클래스 이름을 쓴다. 회복 Boot·실제 프리팹 시험은 카메라 출력 검사가 있어 그래픽 가능한 Editor가 필요하다.

| 무엇을 보나 | 시험 |
|---|---|
| 공용 카드·상점 ID | `ShopUITests`, `ShopPopupModelBuilderTests`, `StageShopUIAssetTests` |
| 회복 외형·프리팹 | `RecoveryViewAssetTests`, `DataBaseAssetTests` |
| 입력 Buffer | `RecoveryViewBufferTests`(+ Core 계약 `RecoveryInputContractTests` 등) |
| origin | `StageRunOriginRegressionTests` |
| 요청 수명·실행 | `RecoveryRequestLifetimeTests`, `RecoveryRequestLifetimeIntegrationTests`, `RecoveryCommandProcessingTests`, `RecoveryRuntimeAcceptanceTests` |
| 팝업 | `RecoveryUITests`, `RecoveryPopupControllerTests`(파일 `RecoveryModalInputTests.cs` 포함), `RecoveryPopupModelBuilderTests`, `RecoveryStageMapViewTests` |
| 실제 프리팹·씬 | `RecoverySceneConnectionTests` |
| 운영 Boot 경로 | `StageMapRecoveryEntryBootSmokeTests`(5개: `ActualEventClick_OpensRecoveryWithMatchingContext`, `CompleteWithoutSelection_ClosesAndClearsOnlyCurrentNode`, `CompletedCurrentEntry_ReattachDoesNotOpen`, `AfterCompletion_NextNodeRestoresMapInputAndPole`, `ModalCancelHeldDrag_RestoresCameraPose`), `StageMapDepthCenteringBootSmokeTests` |
| 다음 전투 HP 지속 | `CoreStageCombatCycleTests.RecoveryCycle_TransfersPersistentHealthToNextBattle`(파일 `RecoveryStageCycleTests.cs`) |
| 상점 회귀 | `ShopStageServiceTests`(파일 `ShopPopupControllerTests.cs`·`StageCommandProcessingTests.cs` 등 partial), `ShopStagePlaySmokeTests`(Explicit, 메서드 이름으로 따로 실행) |

Boot·실제 프리팹 묶음 필터: `^ProjectL\.Tests\.Framework\.(StageMapRecoveryEntryBootSmokeTests|RecoverySceneConnectionTests)$`

## 5 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [회복 Core 요청·인계](../handoffs/2026-10-09-recovery-core-handoff-Hojin.md)(RC0~RC4 요구, V4 인계, Event 진입 요청, ER1 인계) | 천명님 | Core는 같은 브랜치 병합으로 V4를 인수했다. 문서 자체의 외부 전달 기록은 없다 |
| [ER4 UI·Framework 공유](../handoffs/2026-10-15-recovery-er4-ui-framework-share-Hojin.md) | 현강님·민형님 | 미전달 |
| [비전투 View 구조 민형님 공유](../handoffs/2026-10-15-noncombat-view-scope-framework-share-minhyeong-Hojin.md)(요청 소유자 등록·정리 순서 포함) | 민형님 | 미전달 |
| [상점 화면 작업 안내](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md)(V1 카드 책임) | 현강님 | 미전달 |
