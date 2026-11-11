# 스테이지 맵 계획

- 작성자: 호진(이호진)
- 상태: 맵 표시·노드 이동·SA 실행 구조·배경·Canvas·UI 소유권·캐릭터 표시를 구현했다. 사용자 Unity 확인 대기는 UI 소유권 P3(로비)·캐릭터 표시 SC-1이다. SA P8 통합 확인은 미완료이고 공유 메모는 모두 미전달이다. 10-29~11-02 카메라 구도 도구·연결선 메시·덱 확인 뒤 출전·건물만 표시는 코드 완료, 재생 확인 대기다(§3.14~§3.17). Stage Job·Plan 재구성(SJ)은 보류다(2026-10-26 사용자). 스테이지 맵 여정 포기는 결정 대기다(설계 §12).
- 설계: [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)
- 안내: [스테이지 맵 안내](../viewDocs/stage-map-guide-Hojin.md)(3D 아이콘 제작·연결, 선택 입력)
- 정리: 2026-10-27 다음 문서를 이 문서로 합쳤다. StageSelect 구조 정렬 계획(SA, 옛 스테이지 선택 View 계획 포함), 스테이지 맵 표시 계획(Depth 정렬·종류 아이콘·3D 아이콘·범례와 그 완료 기록), 노드 이동 계획(NM1 Boot smoke 조사 포함), 배경 계획, Canvas 통합 계획, UI 소유권 계획, 캐릭터 표시 계획, Stage Job·Plan 재구성 계획(보류), c1b9b0a 병합 검토 안내(guides). 2026-11-02 카메라 구도 계획 둘·연결선 메시 계획·덱 확인 뒤 출전 계획을 §3.15~§3.17로 합쳤다(그 옛 판은 git `3ad97e734`). 끝난 Task 단계·시험 수치·실행 로그·증거 경로는 git `10914570`.

## 1 진행 요약

| 작업(코드명) | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| 선택 View 1~5번 | 맵 읽기·표시, 화면 수명·맵 준비(T7), 노드 클릭 진입 요청 | 완료(10-04) | 사용자 Play(10-04) | `f76d9c44`·`7eec980b` |
| 선택 View 6번 | 진행 연결. 전투 진입 요청만 먼저 연결하고 나머지는 SA가 대신함 | SA로 대체 | 전투 노드 클릭 → Combat 자동 검증(10-06) | `c714424b` |
| SA P0 | 기준선, Core 요청 대조(P0.3) | P0.3만 미완료 | — | — |
| SA P1~P4 | 조회 계약, U1 표시 준비, 아티팩트 책임 분리·공용 오류(P3.9)·UI 계약 교체(P3.10), 입력 Buffer·Flow·Plan·Job 뼈대 | 완료(10-10~10-11) | 자동 검증 | `7345109e` |
| SA P5·P6 | 맵·상점 새 경로 전환, Core 직접 조회(P6.6~P6.10, H3·H4) | 완료(10-11) | 자동 검증 | `7345109e` |
| SA P7 | 아티팩트 경로 교체, Core 직접 조회(P7.5~P7.9, H1·H2) | 완료(10-11) | 자동 검증 | `7345109e` |
| SA P8 | 통합 확인(사용자 Play·Profiler·EditMode 전체 비교) | 미완료 | — | — |
| SA P9 | 문서 통합·시퀀스 UML | 완료(10-11) | — | `7345109e` |
| c1b9b0a 병합 검토 | 상대 우선 병합 조사, 사용자 병합 `9d4c76e5` 뒤 연결 보완 | 완료(10-11) | 자동 검증(실제 Play 4/4) | 검토 `a441daa1`, 보완 `dce453db` |
| 카메라 드래그 | 빈 배경 드래그로 진행축 이동 | 완료(10-11) | 자동 검증 | `3ea220b0` |
| 상점 안내·현재 위치 | 구매 안내 3초, 현재 노드 캡슐·완료 봉 | 완료(10-11) | 자동 검증, 미관은 사용자 재생 대상 | `3ea220b0` |
| Depth 정렬 C1~C3(+T5) | 같은 Depth 노드를 시작 노드 중심선에 정렬 | 완료(10-13) | 사용자 GameView 직접 조작 정상(10-13) | `a79cbfaa` |
| 종류 아이콘 I1~I3 | 네 종류 아이콘·Event 임시 외형, 건물과 동시 숨김 | 완료(10-13) | 자동·출력 이미지. 사람 미관 승인 미실시 | `a79cbfaa` |
| 노드 이동 NM0~NM3 | 수락 → 목적지 숨김 → 아킴보 이동 → 착지 뒤 진입 | NM0~NM2 완료, NM3 체크 2개 미완료 | 사용자 기본 Play 정상(10-16) | `f009a15a`·`edcda135`·`70de0066` |
| 3D 아이콘 N3D0~N3D2·공통 회전 | Sprite 아이콘 → 3D 프리팹 슬롯, `IconYaw` | 11-01 폐기(§3.14) | — | `f37a79a0`·`58e36c6a` |
| 범례 P0·P1 | StageSelect 범례 UI | 완료(10-17) | 자동·캡처. 사람 물리 입력 미확인 | `32654528` |
| 배경 P1·P2 | 챕터별 배경 View DB, 1챕터 프리팹 | 완료(10-25) | 사용자 확인(10-25) | `8106842e` |
| Canvas P1·P2 | StageSelect Canvas 하나로 통합 | 완료(10-26), 덱 Canvas는 UI 소유권 P1이 되돌림 | 사용자 컴파일 확인, 덱 버튼 가림 발견 | `ee55d650` |
| UI 소유권 P1·P2 | 독립 덱 Canvas 복원, StageMap UI 참조를 Canvas 쪽으로 | 완료(10-26) | P2 사용자 확인(10-26) | `ee55d650` |
| UI 소유권 P3 | Lobby UI·배경 참조 분리 | 완료(10-26) | 컴파일·재생 미확인 | `334fb533` |
| 캐릭터 표시 SC-1 | 기존 Akimbo 원본을 맵 마커로, 표시 보정 값 | 코드 완료(10-26) | 외부 빌드만, Unity 미확인 | `8ee9984e` |
| SJ-1~SJ-3 | Stage 실행 계층 Job·Plan 재구성 | 보류(10-26 사용자) | — | — |
| 덱 확인 뒤 출전 | 전투 노드 → 화면 이동 → 덱 창 "출전" → 제출 | 코드·프리팹 완료(10-29) | Unity 미확인 | `006e68a01` |
| 카메라 구도 도구 | 에디터 전용 Position·Rotation·Size, Alt 조작, 재생 중 확정 | 코드 완료(10-29·10-30) | 재생 일부만 | `3444e8c63`·`2ea0b77a4` |
| 연결선 메시 | LineRenderer → Quad, SO 색 | 코드·에셋 완료(10-30) | Unity 미확인 | `2ea0b77a4` |
| 건물만 표시 | 노드·아이콘 걷기, 건물 클릭 | 코드 완료(11-01), 높이 고침(11-02) | 재생 확인 대기 | `9e17d0e66` |
| 노드 호버 정보 NH-a~NH-c | 건물 호버 때 전역 HoverPannel에 제목·설명 | 완료(11-03), 검토 반영, NH-d 3판(§3.18) | Ua·Ub·Uc·Ud 문제 없음(사용자) | 커밋 전 |

## 2 남은 일

### 2.1 사용자 Unity 확인 대기

- [ ] 챕터 2 진입 임시 막기(§3.19): 챕터 1 보스 승리 → Run 승리·엔딩 → 로비. 챕터 2가 준비되면 `StageMap_Chapter1`의 다음 챕터 키를 2로 되돌릴 것.
- [ ] UI 소유권 P3(로비): 컴파일, LobbySceneRoot `Ui` → LobbyCanvasView와 하위 참조 연결, 로비 배경·월드 모델 표시와 배경 Canvas Render Camera, 시설·덱·아티팩트 입력, 로비 ↔ StageSelect 왕복, 재생 종료 뒤 배경 Canvas의 저작 Render Camera 복원.
- [ ] 캐릭터 SC-1: 캐릭터 1개·Idle·발 접지, 다음 노드 이동·회전·착지, 상점 진입, 전투 복귀·재진입 때 중복 없음.
- [ ] 건물만 표시(§3.14)·연결선 메시(§3.16): 건물 위 클릭, 이동 뒤 숨김·복원, 건물·캐릭터·연결선 높이, 연결선 끝점·분기·SO 색(재생 재시작 뒤), 씬 왕복 때 색 핸들 중복 없음.
- [ ] 덱 확인 뒤 출전(§3.17): 일반 덱 창은 확정만, 전투 노드는 화면 이동 뒤 덱 창·출전 전 진입 없음, 빈 칸 있을 때만 확인 창(취소 = 후보 유지), 출전 연타·닫기·뒤로·Core 거절 때 실제 위치 복원, 상점·회복·증강·일반 덱 회귀.
- [ ] 카메라 구도 도구(§3.15): Alt+휠·Alt+가운데, 재생 중 확정 → 종료 뒤 편집 씬 반영·Undo, 씬 저장 뒤 다음 재생에 유지.
- [ ] UI 소유권 P2의 Editor 메뉴 Ensure StageSelect Deck Edit UI 실제 실행(Shop UI 메뉴는 11-02 공통 레이아웃 SO-a에서 도구째 지움).
- [ ] 배경: 상점·회복 뒤 중복 없음, 별도 씬 이탈 정리.
- [ ] 노드 이동 상세 Play(NM3 체크 1): 아킴보 방향·접지, 클릭 연타·배경 drag·설정 UI, 전투·상점·회복 각각의 착지 뒤 진입.
- [ ] 범례: 사람의 GameView 직접 조작·미관 승인, 물리 마우스 입력(종류 아이콘은 11-01 폐기).

### 2.2 미완료 작업

- [ ] SA P8: 완료 조건 대조(P8.0), 사용자 Play(P8.1: 전투·상점·미지원/없는 노드 거절 뒤 재클릭, 상점 구매·나가기, StageSelect → Combat → StageSelect 뒤 입력 재개, HideAllUI·같은 프레임 연속 구매·키보드 Submit 차단), Profiler(P8.2: 무입력 GC 0, 드래그 중 프리뷰 등록 할당), EditMode 전체 비교(P8.3). 아티팩트 항목은 2026-10-13 LV5로 로비 화면이 맡는다([로비 계획](2026-10-11-lobby-plan-Hojin.md)).
- [ ] SA P0.3: Core 요청 H1~H4와 설계 대조, 사용자가 전달할 자료 정리.
- [ ] NM3 체크 2: 워밍업 뒤 반복 실제 이동의 CPU·GC.Alloc(입력·Delay·외형 콜백 분리).
- [x] ~~3D 아이콘 미관·가독성 확인~~: 11-01 아이콘을 모두 걷어 필요 없어졌다(§3.14).
- [ ] 2챕터 이후 배경 아트와 챕터 전환 시각 확인.
- [ ] 엘리트 노드: 범례에 행만 있고 노드 생성은 없다.
- [ ] Run seed R4: 미전달·미구현. 지금은 `JobRegisterSystem`의 임시 seed가 맵·상점 진입 seed를 만든다([Core 요청](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md) 3부).
- [ ] 스테이지 맵·비전투 스테이지의 여정 포기: Q-A1~Q-A7 사용자 결정 대기, 코드 변경 없음([설계](../specs/2026-10-10-stage-map-design-Hojin.md) §12). 전투 밖 Run 종료 Core 입구가 없다.

### 2.3 이 계획에서 고치지 않는 문제

- 측정용 Player가 활성 Combat에서 Quit할 때 150초를 넘겼다(NM3, 원인 미확정). 숨김 전용 Player는 정상 종료했다.
- 개발 Fixture `DevBattleInitializationSource.CreateDefinition`에 CactusCannon이 없어 Combat Fixture + `PlayerLoadout_1` 조합이 실패한다(NM1 조사 10-16). 2026-10-27 코드에서도 Fixture 목록에 없다. 부트·개발 진입은 용근님 영역이다.
- 같은 Editor에서 Play를 반복하면 FMOD `ERR_MEMORY`가 날 수 있다(NM1 조사). Boot smoke에는 프레임 진행과 씬·표시 준비 경계 관측 보강이 필요하다.
- 노드 요청 발급 뒤 이동 시작 전에 `CanOpenDeckEdit`가 덱 열기를 허용해 덱·스테이지 팝업 입력이 겹칠 수 있다(Canvas P2 검수, 미재현).
- 서로 다른 모달을 외부에서 강제로 연속 Open하는 경우의 입력 소유 제약은 확대 지원하지 않는다. 이전 입력 반환 뒤 다음 화면을 연다.
- 기준선 실패(10-17 관찰, 지금 상태는 확인 필요): `DataBaseAssetTests` 셋(Stage 1101 벽 좌표 `(7,3)`/`(6,3)`, Stage 2101 적 본부 Y 8/0, 옛 `LineShader.mat` 경로), `GlobalUITests.GlobalCanvasPrefab_CreatesOneLiveUnitCard_AndRetainsViewsAcrossRebinds` 하나. 고치지 않았다.

## 3 작업별 결과

### 3.1 스테이지 선택 View 1번부터 6번까지

SA 전의 단계 작업이다(2026-10-03~10-06, 브랜치 `feature/Hojin/Stage_Shop_Event`).

- 1·2번(10-03): `StageMapViewReader`, `StageMapViewDisplay`·`StageNodeView`·`StageLinkView`. 외형 조회는 `StageNodeVisualTable` 하나, 클리어 표시는 `ClearedIndicator`.
- 3번(10-04 완료): `StageMapSceneRoot`(`mapRoot`), `StageMapViewSystem`(모듈만 등록, Enter 100·Exit -100), Factory 등록 한 줄. 사용자가 Play에서 맵 전체 표시를 확인했다.
- T3(10-03 사용자 결정): Factory 등록으로 깨진 천명님 `ChapterGraphPreparationTests`의 `withJobRuntime` 조립을 Core Factory + `JobInGameService`로 고쳤다([Core 전달 §5.5](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)).
- T7(10-04 사용자 결정): `StageLifeCycle.OnEnter`가 `Register(Stage, Start)`를 한 번 걸어 맵을 준비한다.
- 10-04 Boot 재생 차단 해소(사용자 결정): `PlayerInputActions`의 `Artifact` 맵에 `RotateLeft` = Q, `RotateRight` = E 바인딩을 더했다.
- 4·5번(10-04 완료, 사용자 EditMode·Play 확인): `StageMapInteraction`, `IStageNodePicker`·`StageNodePicker`, `IStageMapInteractionFeedback`·`StageMapInteractionLog`, 노드 프리팹 자식 `HitTarget` 계약.
- 같은 날 Core 머지 뒤 View 조립 복원, `b5d14d7` 카메라 머지·`1fe9cce` 병합 충돌을 사용자 지시로 정리했다.
- 6번(10-06, 사용자 승인): `StageMapEntryFlow`가 전투 노드 클릭을 `CombatEntry.RequestEntry`로 넘겼다. Core `5f2d0381`(병합 `f68408b1`) 뒤 전투 노드 클릭 → Combat 진입을 `StageMapCombatEntryBootSmokeTests`로 확인했다. 나머지 진행 연결은 SA(§3.2)가 대신했고 `StageMapEntryFlow`는 SA P6에서 지웠다.
- 10-06 발견: 커밋된 `Boot.unity` 시작 구간이 Combat(0)으로 돌아가 있었다(현강님 `efb04eea` 병합). Boot smoke는 `BootStageStartOverride`로 시험 안에서만 Stage를 쓴다. 기본 시작 구간은 이후 `StartLifeCycle.Lobby`로 정해졌다.

옛 결정(10-03~10-04). SA 설계의 D1~D14와 번호가 겹치므로 "옛 선택 View D"로 부른다.

| ID | 결정 | 지금 |
|---|---|---|
| 옛 D1 | 3번은 View 연결만(b안) | 바뀐 것: 10-04 사용자 결정으로 맵 준비(T7)만 앞당김. 지금 진행 연결은 SA |
| 옛 D2 | 카메라는 씬 배치 그대로, 코드는 관여 안 함 | 바뀐 것: 카메라 드래그(§3.3)가 대신함 |
| 옛 D3 | Attach마다 데이터가 준비되면 한 번 그림 | 유지 |
| 옛 D4 | `StageMapViewSystem`은 모듈로만 등록 | 유지 |
| 옛 D5 | 피커는 `Camera.main`(사용자 A안) | 바뀐 것: §3.3이 대신함 |
| 옛 D6 | 팝업 중 맵 클릭 차단은 입력 Layer 전환, UI 위 판정 없음 | 바뀐 것: UI 위 판정은 `StageMapPointerBlocker`(§3.3). Layer 전환은 그대로 |
| 옛 D7 | 갈 수 있는 노드 판단은 Core, View는 강조하지 않음 | 유지 |
| Q1 | 같은 노드에서 누르고 떼면 바로 진입 요청 | 유지(10-04 사용자) |
| Q2 | 요청 기록은 에디터 전용 로그 | 유지(10-04 사용자) |
| Q3 | 이펙트는 연결 자리만, 강조 색 없음 | 유지(10-04 사용자) |

### 3.2 SA 구조 정렬

맵·상점·아티팩트를 도메인별 입력 Buffer → Flow → Plan·Job으로 정렬하고 Core가 입력을 직접 조회하게 했다. 결정 D1~D14와 실행 계층 계약은 [설계](../specs/2026-10-10-stage-map-design-Hojin.md) §3.1·§4에 있다. 용어: W1 = 옛 소비자만 운영하던 과도 단계, U1 = 버튼 → 입력 Buffer UI 계약, S4V = 상점 화면 구현(상점 계획), H1~H4 = Core 처리 번호 요청(H1 아티팩트 편집·H2 프리뷰·H3 상점·H4 노드 진입), R4 = Run seed.

- P0(10-10): 기준선 기록. P0.3(Core 요청 대조·전달 자료)은 남았다.
- P1(10-10, D7): `IViewQuery`에 `ArtifactPopupConfigTableQuery`를 넣고 `IHasArtifactPopupConfig`와 캐스트를 지웠다.
- P2(10-10, D6·D8): `ShopPopupModel`·`StageOfferCardContent`·`ArtifactPoolItemView`를 UI 폴더 `Screens/Stage/`로 옮겼다(GUID 유지). `ArtifactPopupUI`의 Update 폴링·Buffer·DB 조회를 걷고 `IsOpen`·`OpenStateChanged`를 공개했다. 실제 ShopUI는 S4V.
- P3.1~P3.8(10-10, D5): 아티팩트 View를 `ArtifactPointerMapper`·`ArtifactPopupStatus`·`IArtifactDragPresenter`(`ArtifactPopupView`)·`ArtifactBoardRefresher`·`ArtifactDragController`·`ArtifactPopupSession`으로 나눴다. `ArtifactViewSystem`은 조립·Tick만 맡는다.
- P3.9(10-10, 사용자 승인): 공용 오류 입력 정리 콜백의 소유자를 View 그룹의 `InGameViewFaultCleanup`(Factory 조립)으로 옮겼다. 각 화면 `StopInput`을 차례로 부르고 예외를 모은다. `CombatViewSystem.Shutdown`의 공용 콜백 해제만 그룹으로 옮겼다. 전투 작업자 미전달·미합의다([공용 오류 협의](../handoffs/2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md), Core 전달 §5.16). 지금 정리 순서는 그 메모 머리(10-16)를 따른다.
- P3.10(10-11, 사용자 승인): `IArtifactPopupHost`·`IArtifactPopupScreen`을 지우고 UI 옆 `IArtifactPopupDisplay`와 동기 `OpenStateChanged` 구독으로 바꿨다(Core 전달 §5.17). 상점 Host·Screen은 W1로 남겼다가 P6에서 지웠다.
- P4(10-10): `ShopViewBuffer`, `ArtifactViewBuffer` 최신 프리뷰 슬롯, `RemoveCommandsThrough`·`Clear`, `JobRootType.Shop = 4`·`ShopPlan`, `StageFlow`·`StageNodeEntryFlow`·`ShopFlow`·`ArtifactEditFlow` 뼈대, `IStageEntryAttemptRead`. 운영에는 연결하지 않았다(W1). Core 전달 §5.15. P4 시험 수치·증거 경로는 git `10914570`.
- P5·P6(10-11): 사용자가 필요한 Core·Buffer 선행 구현을 승인했다(외부 미전달·미합의).
  - `StageLifeCycle`이 `StageFlow`를 소유한다. `ShopCommandJob`(H3)·`StageNodeEntryJob`(H4)을 쓰고, 전투는 `RequestEntry`·`CombatPrepareJob`에 맵 번호를 넘긴다.
  - 실제 `ShopUI`·`StageOfferCardView`·`IShopPopupDisplay`·프리팹·씬을 연결했다. 맵 입력 정리는 `StageMapEntryObserver`가 한다.
  - 이탈 순서는 StageLifeCycle 이탈 준비(-200) → View Detach(-100) → Exit(0)이다.
  - 옛 `StageMapEntryFlow`·상점 Scheduler·Request·Job·`IShopEntryRequester`·Host·Screen·`StagePlan.CreateShopJob`을 지웠다.
  - 사용자 요청 추가 정리: `ArtifactViewSystem`의 입력 수신 인터페이스와 전달 함수 일곱을 지웠다.
- P6.6~P6.10(10-11, 사용자 추가 승인): H3·H4를 Core 직접 조회로 바꿨다. `ProcessNextShopCommand(int)`, `ProcessNextNodeCommand(int, Func<int> nextSeed)`, `TryProcessNextBattleCommand(int, out ChapterProgressionResult)`. `StageViewCommandData.ChapterKey`를 필수로 했다. 번호 있는 `RequestEntry`는 등록 전에 번호·챕터·노드 대응을 검사하고, Core 미실행 false일 때만 Prepare 안에서 취소한다. 독립 검토 Critical 0·Important 0.
- P7(10-11, 사용자 승인): `ArtifactCommandJob`·`ArtifactPreviewJob`으로 바꾸고 `ArtifactEditScheduler`·`ArtifactPreviewSlot`·옛 요청·Job을 지웠다.
- P7.5~P7.9(10-11, 사용자 추가 승인): H1·H2를 `ProcessNextCommand(int)`·`ProcessLatestPreview(int)` 직접 조회로 바꿨다. 같은 Stage 재연결 시험에서 편집 대기가 막혀 `ArtifactEditFlow`는 기대 편집 입력이 사라지면 취소로 끝내도록 고쳤다. 기대 번호는 재연결 보호값이며 Framework 필수 형태가 아니다.
- P8: 미완료(§2.2).
- P9(10-11): AR·BF 초안의 유효 근거를 SA 설계로 합치고 중복 초안 둘을 지웠다. 시퀀스 UML을 저장했다(지금은 [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md)의 흐름 절). 민형님 Framework 원본 셋은 같은 날 사용자 지시로 복원하고 갱신 요청을 [Framework 공유](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md) §10에 옮겼다.
- 주석·자체 문서 현행화(10-11, 사용자 요청): C# 주석 네 파일과 자체 문서를 현재 경로로 맞췄다. 다른 작업자 문서의 갱신 요청은 Framework 공유 §12에 모았다.

Core 교체 순서·API·수명 계약은 [Core 교체 인계](../handoffs/2026-10-11-stageselect-core-replacement-handoff-Hojin.md)(1부 P5·P6, 2부 P7)가 원본이다.

### 3.3 진행 방향 카메라 드래그

2026-10-11 사용자 선택: 빈 배경을 왼쪽 버튼으로 끌면 시작 ↔ Boss 진행축으로만 움직인다. 각도·직교 크기는 그대로이고, UI·노드에서 누른 입력은 드래그가 되지 않는다. 옛 선택 View D5·D6을 대신했다.

- `StageMapInteraction`이 누름 소유권(UI 차단·노드·배경)을 정한다. UI 판정은 `StageMapPointerBlocker`(현재 좌표의 `GraphicRaycaster`).
- `StageMapCameraDrag`는 연결 때 `CameraCatalogSystem.GetHandle`을 한 번 빌리고 Position만 바꿔 `Customize`로 넘긴다. 범위는 Buffer의 Start·Boss와 표시된 노드 위치로 구한다.
- `StageSelect.unity`: Main Camera에 CinemachineBrain, 맵용 `StageMapCamera`(우선순위 10). 그때 함께 둔 `StageArtifactCamera`는 LV5(10-13) 뒤 `Lobby.unity`의 `LobbyArtifactCamera`가 대신한다.
- `GameBootstrap`이 Stage·Combat 로드 뒤 -190에서 CameraGroup `BindScene`을 부른다. Framework 내부·FieldCamera·Core는 고치지 않았다.
- 기존 실패 네 시험을 사용자 승인으로 정식 수정했다: `CameraEffectSystemTests`의 렌더 통합 시험(Play 모드로), `GameBootstrapFaultBoundaryTests` 두 사례(기대 로그), `StageMapViewSystemTests.EnterNode_WithoutMainCamera_Throws`(태그 격리).
- 공유: [카메라 공유](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md)(미전달).

### 3.4 상점 안내와 현재 위치 표시

2026-10-11 사용자 승인. 상점 하단의 중복 구매 완료 문구를 걷고 성공 안내는 `DelayService`로 3초 뒤 비운다. 현재 노드는 캡슐만, 떠난 클리어 노드는 봉 하나, 미클리어 노드는 기존 외형이다. 실행 기록은 [상점 계획](2026-10-09-stage-shop-plan-Hojin.md) 2부가 원본이다. 미관은 사용자 재생 확인 대상이다.

### 3.5 Depth별 중앙 정렬

2026-10-13. 같은 Depth 노드의 `(최소 + 최대 WidthIndex) / 2`를 시작 노드 중심선에 맞춘다. DB·Buffer에 새 값을 저장하지 않는다.

- C1: 새 `StageMapNodeLayout`, `StageNodeView`가 생성 때 적용. 기존 단일 노드 기대 위치 X8 → X0만 시험에서 고쳤다.
- C2: 회귀 시험 보강, 네 종류 촬영 fixture 중심을 X=0으로 고쳤다. 제품 코드 변경 없음.
- C3: 새 `StageMapDepthCenteringBootSmokeTests`로 실제 Core 그래프를 확인했다. 사용자가 "직접 조작까지 정상"이라고 답했다(10-13).
- T5(사용자 별도 승인): 같은 시험으로 상점 수락·이벤트 미지원 거절을 실제 클릭으로 확인했다. 이벤트 진입을 지원한 것은 아니다(그 뒤 회복 진입은 [회복 계획](2026-10-11-noncombat-event-recovery-plan-Hojin.md)).
- 독립 리뷰는 C1~C3 모두 지적 없음.

### 3.6 노드 종류 아이콘과 이벤트 임시 외형

2026-10-13. 일반 전투 = 일반 해골, 보스 = 보스 해골, 이벤트 = 물음표, 상점 = 텐트, 엘리트는 보류. 현재 노드와 떠난 클리어 노드에서는 아이콘을 건물과 함께 숨긴다.

- I1(사용자 지시): `StageRootDataTable`의 잘못된 상세 참조(Event_Recovery) 하나를 `Stage_Event` 루트로 바꿨다(기존 12개·순서 유지). Event View SO에 보라 Cube `(0.6, 0.35, 0.8, 1)`·물음표를 연결하고, 공통 `StageMapNode`에 비활성 `IconRoot`를 더했다.
- I2: `StageMapViewData`에 `IconRootName`·`IconHeight`·`IconGap`과 검사, 새 `StageNodeIconLayout`. 노드마다 외형을 한 번 조회하고 `ApplyAppearance` 한 곳에서 건물·아이콘을 함께 숨긴다. 시작 노드는 아이콘을 생략한다.
- I3: 새 `StageMapNodeIconBootSmokeTests`. 시험만 고쳤다(FourTypes 캡처 closure → `VerifyFourTypes()`). 기존 held-mouse 시험의 실패 원인은 실제·합성 Pointer 장치 경쟁이며, 격리 실행에서 통과했다(원본 시험·에셋 무변경).
- 사용자가 10-13 `IconHeight`를 1로 저장했다(`IconGap` 0.25). 그 값으로 재검증했다.
- 사람의 GameView 직접 조작·미관 최종 승인은 미실시다. 독립 리뷰 필수 수정 0.

### 3.7 노드 이동

2026-10-16 사용자 결정: Core 수락 확인 → 목적지 박스 숨김 → 로비처럼 이동 → 도착 뒤 진입. 로비 이동 코드의 공용화를 포함한다. 수치는 StageMap 높이 0.8·총 0.6초(구간 12:35:12), 로비 0.35·0.12/0.35/0.12초 그대로다. Bootstrap 직접 진입(맵 번호 0)은 기다리지 않는다.

- NM0: 기준선(HEAD `f9127861`).
- NM1(`f009a15a`): 로비 이동 두 클래스를 `Common/ChessPieceMotion`·`ChessPieceMotionSettings`로 옮기고(GUID 유지) local 좌표 모드를 더했다. 로비 소비자와 시험 넷을 새 타입에 연결했다. Boot smoke 다섯이 실패해 조사했다. 결론은 공용화가 원인이 아니다(개발 Fixture의 CactusCannon 누락, 같은 Editor의 FMOD 자원 오류, 재현되지 않은 진입 대기 둘). §2.3 참고.
- NM2(`edcda135`, A·B·C를 한 Phase로): `ICombatEntryService`에 좁은 owner 역할(`IMapCombatPresentationOwnerSource`)과 `MapCombatPresentationLease`·요청별 ticket을 연결했다. 준비 완료 뒤 Pending이면 기존 전환 Job이 기다리고, Ready 뒤 `BeginActivation → Manager.Change`가 한 번 돈다. `StageMapNodeMovement`가 같은 마커·아킴보를 재사용해 이동하고(local Y 0.12), 이동 중에는 목적지 Visual·아이콘·완료 봉만 숨긴다. 상점·회복은 착지 뒤 한 번 열린다.
  - 오류 처리: 준비 기록 누락·권위 위치 불일치·현재 owner의 Ready 게시 실패는 오류, 전투 owner 상실은 Abandoned, 비전투 이동 중 Detach·교체는 공용 오류 정지다.
  - Core·CombatPlan·Factory·ServiceKeys·Framework는 고치지 않았다. 독립 검토 필수 이슈 없음.
- NM2 후속(10-16): 사용자가 발판 간격·연결선 가림을 제기했다. 원인은 시험 `StageMapNodeMovementTests`가 실제 SO의 `GetData()` 객체를 공유해 열린 Editor 메모리에 `nodeScale` 1.5를 남긴 것이다(파일 값은 1). 복제 SO로 격리하고 회귀 사례를 더했다. 제품 변경 없음.
- NM3(`70de0066`): 새 `StageMapNodeMovementBootSmokeTests`로 실제 Boot·DB·Core의 거절·연타·상점·회복·다음 노드·전투 진입·포기 뒤 새 Run 복귀를 확인했다. Development Player에서 외형 숨김·복구 1,000쌍의 관리 할당 0 B, 같은 인스턴스 유지를 확인해 `SetActive` 재사용을 유지했다. 반복 실제 이동 전체 비용은 측정하지 않았다.
- NM3 사용자 Play(10-16): "크기·연결선·이동·진입 모두 정상". 기본 범위만 인수했고 세부 체크는 §2.1에 남겼다.
- NM3 일반 승리 복귀 후속(10-16, 사용자 승인, NV0~NV3): `StageMapNodeMovementBootSmokeTests.Victory.cs`로 일반 승리 → 같은 맵 현재 완료 노드 즉시 복원 → 다음 상점 노드 이동을 확인했다.
- 삭제된 Timeline UI 호출 정리는 별도 작업이다([구조 리팩토링 계획 §6.12](2026-10-14-combat-view-structure-refactor-plan.md#612-삭제된-timeline-ui-호출-정리-2026-10-16-완료)).
- 공유: [노드 이동 공유](../handoffs/2026-10-16-stage-map-node-movement-framework-combat-share-Hojin.md)(작성만, 미전달).

### 3.8 3D 종류 아이콘과 공통 회전

2026-10-17 사용자가 설계·계획·N3D0~N3D2 실행을 승인했다(기준선 실패 셋은 그대로 두는 조건). 계약은 `Sprite Icon` → 선택적 `GameObject IconPrefab`(null 정상, 정적 MeshFilter·MeshRenderer만)이다. 상세 계약은 설계 §5.3, 제작·연결 방법은 안내에 있다.

- N3D0: 기준선 조사. 계획에 빠진 직접 소비자 `ShopStagePlaySmokeTests.cs`를 찾아 사용자 지시로 계획에 넣었다. 기준선 시험은 사용자 승인으로 Editor를 닫고 배치 실행했다.
- N3D1: 코드·에셋 슬롯 전환. 네 SO의 `iconPrefab`을 명시적 null로 저장했고 공통 `StageMapNode`의 `IconRoot`에서 SpriteRenderer만 지웠다(GUID 유지). 독립 리뷰 0·0·0.
- N3D2: 실제 URP 출력·입력 검증. 처음 두 실패는 DepthCentering 시험이 0.6초 이동·착지 뒤 팝업 계약을 기다리지 않은 것이라 시험만 고쳤다. 아이콘 시험에 실제 건물 위 간격(`IconGap`) 측정을 더했다. 독립 리뷰 0.
- 완료 구분: 코드·에셋 슬롯 전환 완료, 테스트 모델 자동 검증 완료. 운영 아이콘은 10-19 `58e36c6a`에서 연결됐고 미관 승인은 기록 없음.
- 공통 회전(10-19 사용자 정정): 모든 아이콘이 같은 고정 방향(Y 약 210°)을 쓴다. `StageMapViewData.IconYaw`를 더하고 `StageMapView_Default.asset`에 넣었다(지금 값 185, 코드 기본 210). Scope가 `MapRoot.rotation × Y(IconYaw)`를 넘긴다. 바뀐 것: 아이콘마다 캐릭터를 향하는 각도 → 공통 고정 회전(10-19, 사용자 정정). 옛 해석의 클래스·시험은 지웠다. 외부 빌드만 했고 Unity는 미확인이다.

### 3.9 범례

2026-10-17. 통합 기획서 15슬라이드 5번의 범례를 StageSelect에 붙였다. 사용자가 설계안, 명패·본문 그림, 다섯 행 문구를 승인했다.

- `StageLegendUI : UIBase`(`UIId.StageSelect` = 4, Parallel), 전용 프리팹 `StageLegendUI.prefab`, `StageSelect.unity`에 인스턴스 하나.
- 다섯 행: 일반·엘리트(노란 해골 `_0`)·보스·이벤트·상점 스테이지. 나인슬라이스 `UI_2_textbox`, 배경만 Raycast, 전용 Canvas order 100(상점·회복 110, 글로벌 999 아래).
- 연결: `GameBootstrap.OpenStageUI`에 guard와 열기 두 줄을 더했다. 바뀐 것: 그 두 줄은 Canvas P2(10-26)에서 지우고 `StageMapSelectionPresentation`이 연다.
- 시험: 새 `StageLegendUITests`·`StageLegendBootSmokeTests`. 기준선 실패 `GlobalUITests` 하나는 그대로다.
- 사이드 대화 제한으로 독립 리뷰는 하지 않았다. 사람의 물리 마우스·회복 진입 재검증은 하지 않았다. 범례의 엘리트 행은 노드 생성 구현이 아니다.
- 공유: [범례 UI 공유](../handoffs/2026-10-17-stage-legend-ui-handoff-Hojin.md)(미발송).
- 11-03 숨김(사용자 지시, 프리팹만, 재생 미확인): `StageLegendUI.prefab`의 `Frame`을 끔. 코드·씬 무변경. 이유와 되돌리는 법은 설계 §5.5 끝 줄.

### 3.10 챕터별 배경

2026-10-25 사용자가 설계·계획·첫 Phase를 승인했고, 같은 날 P2를 추가 승인했다. Codex가 직접 진행했다. 새 시험·MSBuild·EditMode 실행은 하지 않는 조건이다.

- P1: 새 C# 여섯(`StageMapBackgroundData`·`SO`·`TableSO`, `IStageMapBackgroundTableQuery`·`StageMapBackgroundTable`, `StageMap/Background/StageMapBackgroundDisplay`). 독립 리뷰 0·0·0.
- P2: DB 등록(`ViewDataBaseFactory`, `ViewTableKey.StageMapBackground`, `IViewQuery.StageMapBackgroundTableQuery`), `StageMapViewSystem`·Scope·SceneRoot(`BackgroundRoot` 필수) 연결, 프리팹 `Prefabs/StageSelect/Backgrounds/StageMapBackground_Chapter1.prefab`, `DataBaseSO/ViewGroup/StageMapBackground/` 표·항목, `ViewGroup.asset`, `StageSelect.unity`(MapRoot 형제 `BackgroundRoot`, 고정 Probs 제거). 기존 시험 11파일은 조립만 맞췄다. 독립 리뷰 0·0·0.
- 사용자 확인(10-25): 컴파일 오류 없음, 1챕터 배경 정상 표시, 전투 왕복 뒤 중복 없음. 나머지 미확인은 §2.1.
- 공유: [배경 공유](../handoffs/2026-10-25-stage-map-background-framework-share-Hojin.md)(외부 미전달, 고친 시험 목록 포함).

### 3.11 Canvas 통합

2026-10-26 사용자 확정(Q1·Q2): 덱 편집 중에도 범례를 유지한다. 공통 Canvas는 평소 글로벌 UI 뒤, 편집 중 앞, 닫으면 복원한다. 사용자가 StageScreenRoot 형제 부모안과 P2 마무리를 승인했다. Codex가 직접 진행했다.

- P1: `StageSelectPanelUI : UIGroup`(`UIId.StageSelect`, Parallel), `StageMapCanvasOrder`(덱 `IsOpen` → Canvas 정렬). 독립 리뷰 0.
- P2: 실제 부모 `StageScreenRootUI`(`UIId.StageScreenRoot` = 40), 상점·회복 `ParentID` = StageScreenRoot, `DeckEditUI.ParentID` = StageSelect, 표시 수명 `StageMapSelectionPresentation`·`StageMapScreenPresentation`, 씬 `StageSelectCanvas` 하나(1920×1080, order 4)와 네 UI의 개별 Canvas 제거. `GameBootstrap`의 StageSelect 초기 Open 두 줄을 Scope로 옮겼다. 기존 시험 11파일 조립만 맞추고 개별 Canvas 정렬 전용 사례 하나를 지웠다. 최종 독립 리뷰 0·0·0.
- 계획과 다르게 한 점: 정상 복귀와 HideAll이 같은 `Hide(false)` 알림으로 와서 원래 설계로 구분할 수 없었다. 공용 UISystem 계약 확장을 제안했지만 승인받지 않았고, StageScreenRoot 형제 부모의 수명으로 해결했다. 처음에는 Bootstrap 초기 Open을 남겼다가 재개 범위에서 옮겼다.
- 사용자 확인(10-26): `CleanupErrors` using 누락을 고친 뒤 컴파일 오류 없음. 덱 열기 버튼이 글로벌 이미지에 가려 클릭되지 않음 → §3.12 P1.
- 공유: [Canvas 통합 인계](../handoffs/2026-10-26-stage-select-canvas-integration-handoff-Hojin.md)(미발송).

### 3.12 UI 소유권 분리

2026-10-26 사용자 승인: 이 방향으로 Codex가 직접 진행한다(Claude 협업은 나중). 공용 UISystem·UIBase·UIGroup·PopupViewScope·PopupInputSession은 고치지 않는다.

- P1 독립 덱 Canvas 복원: `DeckEditUI.prefab`의 Canvas·Scaler·Raycaster를 원래 GUID로 되살렸다(Overlay, order 100, 공통 Canvas는 4). `StageSelectPanel`은 공통 Canvas와 형제 루트, 범례는 공통 Canvas 자식이면서 그룹 멤버다. Scope의 `StageMapCanvasOrder` 사용을 걷었다(파일은 그 뒤 `b1dd4962`, 10-27에서 지워졌다). 독립 리뷰 0.
- P2 StageMap UI 참조 이전: 새 `StageMapCanvasView`(canvas·deckCanvas·selection·screens). `StageSelectPanelUI`가 `DeckScreen`, `StageScreenRootUI`가 `ShopScreen`·`RecoveryScreen`과 구성 검증을 갖는다. `StageMapSceneRoot`에는 mapRoot·backgroundRoot·mapCamera·ui만 남았다. Scope는 화면 인터페이스를 생성자 인자로 받는다. 기존 시험 13파일(조립 4·조회 9)만 맞췄다.
  - 계획 밖 발견: Editor 메뉴 두 개가 옛 SceneRoot 필드를 찾는다. 사용자 승인("두 Editor 도구도 최소 수정")으로 `StageShopUISetup.cs`·`StageDeckEditUISetup.cs`를 새 소유자에 맞췄다. 독립 리뷰의 Important 1이 이것이다.
  - 사용자 확인(10-26): "유니티에서 1~7번 확인했는데 컴파일 오류 없고 덱 편집, 상점·회복 복귀, 전투 왕복도 정상". P2 인수 완료.
- P3 Lobby UI·배경 분리: 새 `LobbyCanvasView`·`LobbyBackgroundView`·`LobbyBackgroundCameraBinding`. `LobbySceneRoot`에는 lobbyCamera·worldRoot·ui만 남았다. `LobbyBackgroundView.ConnectOutput(Camera, float, float, Func<bool>)`가 `IDisposable` 연결을 돌려주고, 원래 worldCamera를 복원하며 실패 때 되돌린다. `LobbyVisualSet.Create`·`LobbyCharacterVisual.Create`는 `Transform worldRoot`만 받는다. 기존 시험 10파일만 맞췄다. 독립 리뷰 0·0·0. 컴파일·재생 미확인(§2.1). 로비 쪽 상세는 [로비 계획](2026-10-11-lobby-plan-Hojin.md)과 함께 본다.
- 공유 메모는 따로 쓰지 않았다. Canvas 통합 인계 머리가 이 결과를 가리킨다.

### 3.13 캐릭터 표시

2026-10-26 사용자 선택: 기존 Akimbo 프리팹을 다시 쓰고, 전투 데이터는 쓰지 않으며, 새 프리팹은 만들지 않는다.

- SC-1: `StageMapViewDataSO`에 `MarkerOffset`·`MarkerYaw`·`MarkerScale`과 검증, `StageCurrentMarkerView.ShowAt`이 비활성 Root 아래 보정 루트와 복제본을 만든다(복제본만 Default Layer·Collider 끔·root motion·animation events 끔). `StageMapViewDisplay`의 표시 동등성 비교에 보정 값을 넣었다. `StageMapView_Default.asset`에 기존 Akimbo와 보정 값을 연결했다. 기존 시험 둘을 옮겼다.
- 외부 `dotnet build` 오류 0(경고는 모두 범위 밖). Unity 시험은 다른 인스턴스가 프로젝트를 잡고 있어 실행하지 못했다. 독립 검토 0. 사용자 확인 항목은 §2.1.
- 선택 제안(반영 안 함): 원본 Collider·Layer·root motion 상태 단언, 비정상 값·0이 아닌 Yaw 시험.

### 3.14 건물만 표시

2026-11-01 사용자 요청: 노드와 아이콘을 전부 없애고, 건물을 눌러 처리. 그것으로 연결했던 SO·내부 구현도 함께 걷음(§3.6·§3.8의 아이콘 결과를 대신함).

- 코드: `StageNodeView`가 프리팹 없이 빈 노드 자리를 만들고 건물을 놓는다. `StageNodeViewParts`·`StageNodeIconLayout` 삭제, 아이콘 회전 인자를 `StageMapViewDisplay`·`StageMapViewScene`에서 걷음.
- 클릭: 켜진 건물 메시를 재서 노드 자리에 BoxCollider를 붙이고 건물이 보이는 동안만 켠다. 숨은 현재·완료 노드는 Core가 늘 거절해 잃는 동작이 없고, 그 자리의 누름은 카메라 끌기가 된다. 시작 노드가 아닌데 건물이 없으면 표시 때 오류로 멈춘다.
- SO: `StageMapViewData`의 `nodePrefab`·`iconHeight`·`iconGap`·`iconYaw` 삭제, `buildingHeight` 추가(옛 `VisualRoot` 높이 0.75 그대로). `StageNodeVisualData`의 `iconPrefab` 삭제.
- 에셋 삭제(사용자 지시): `Infrastructure/StageMapNode.prefab`, `Tile.prefab`, `Prefab/*_Icon.prefab` 다섯과 폴더.
- 계획에 없던 결과: 완료 봉도 노드 프리팹에 있어 함께 없어졌다. 떠난 완료 노드에는 아무것도 없다.
- 그대로 둔 것: 연결선, 현재 노드의 캐릭터, 범례 UI.
- 참조가 없어져 지우지 않고 보고한 것: `Models/*_Icon.fbx` 다섯, `Infrastructure/Tile.fbx`·`StageMapCleared.mat`·`StageMapHighlight.mat`·`StageMapNodeIcon.mat`·`tripo_material_…` 재질 둘, `StageMapCleared 1.mat`.
- 컴파일 미확인, 새 시험 없음. 재생 확인 대기: 부트, 건물 위 클릭, 이동 뒤 숨김·복원, 건물·캐릭터 높이.
- 2026-11-02 사용자 요청(건물과 연결선 높이가 너무 떨어져 보임): `buildingHeight` 뜻을 피벗 위로 올리는 값에서 건물 밑면 높이로 바꿨다. 켜진 메시를 재서 맞추므로 모델마다 피벗이 달라도 밑면이 같다. 기본 에셋 값 0.75 → 0.05(연결선 높이와 같음). 컴파일 미확인, 재생 확인 대기.

### 3.15 그래픽 작업용 카메라 구도 도구 (10-29·10-30)

설계 §8.1. 사용자가 이 기능의 C# 작성을 Codex에 맡겼다(예외 허용). 새 시험 없음, 커밋은 사용자.

- 10-29 1판: Root에 에디터 전용 미리보기 상태, `StageMapCameraDrag`에 원본·작업 구도와 경계 다시 계산, 커스텀 인스펙터(잠금 해제·미리보기·확정 및 잠금·취소, SessionState). 확정은 편집 모드에서만. 검토 지적으로 기울기 입력을 Slider → FloatField(유효성은 맵 평면을 향하는지).
- 10-29 후속 셋: Alt+왼쪽·오른쪽 끌기(`CameraModifier`, `initialStateCheck = true`로 Alt를 누른 채 맵이 켜져도 맞음), Alt+가운데 전진·후진(`CameraDolly`), Alt+휠 Size(`CameraZoom`, Transform과 가상 카메라를 같은 Undo로 기록).
- 가운데 끌기 신고(사용자 "Position이 안 바뀐다"): 입력을 바꾸지 않고 SessionState 진단(입력 수신·시작 차단·위치 반영/거절)을 넣었다. 다음 실행에서 위치 반영 경로에 닿은 것을 확인했고 앞선 신고의 원인은 확정하지 않았다. 사용자의 확정·취소는 추정하지 않아 저장된 씬 값은 덮지 않았다.
- 10-30 재생 중 확정: 확정 스냅샷을 `StageMapCameraFramingPending`(+ `.meta`)이 들고 재생 종료 뒤 편집 씬에 넣는다. 취소하면 마지막 확정 구도로, 다시 풀면 확정 값에서 시작. 기존 구도 키 재사용이라 실행 중 씬 경로·카메라 이름을 바꾸면 자동 반영이 실패하고 값은 세션에 남는다.
- 확인: InputActions JSON·바인딩·ID 중복, `git diff --check`, 읽기 전용 검토(고칠 것 없음). Size 칸이 7로 보이는 것은 Unity에서 봤다. Alt+휠·재생 중 확정 → 다음 재생 유지는 재생 미확인.

### 3.16 연결선 메시 전환 (10-30)

설계 §5.6. 사용자가 MeshRenderer 교체·SO 색·Codex 직접 수정을 승인했다.

- 바꾼 것: `StageMapViewDataSO`·Input → `StageMapViewData.LineColor`(검사 포함), System·Services·Scene·Display에 `IColorSystemService`, `StageLinkView` Quad 배치·색 핸들, `StageMapConnection.prefab`(GUID 유지) MeshFilter·MeshRenderer, 재질 Cull Off, SO 청록.
- FW 점검: ColorSystem은 기존 등록 경로로만 받고, 핸들은 Renderer 파괴 전 반환(FW-011). 출력 카메라(Boot Main Camera)·RenderTexture 카메라 마스크에 Default 레이어가 있다.
- 확인: 바꾼 10파일 diff·LF 검사, 읽기 전용 리뷰(런타임 결함 없음, 적용 시점 안내만 툴팁에 반영). Unity 컴파일·재생 미확인.

### 3.17 덱 확인 뒤 출전 (10-29)

설계 §6.7. 사용자 승인, Codex 구현, Core·LifeCycle 무변경, 새 시험 없음.

- 새 코드: `StageCombatDeparture`, `DeckCombatConfirmUI`(+ `.meta`, `UIId.DeckCombatConfirm`). 고침: `DeckEditDraft`(변경 여부), `DeckEditPopupController`(전투용 열기·출전·확인 수명), `DeckEditUI`(`startCombatButton`·출전 계약), `StageMapInteraction`·`StageMapViewScope`·`StageMapViewEntry`, `DeckEditUI` 프리팹·StageSelect 위치.
- 자체 검수로 보탠 것: 결과를 다시 볼 때 착지 완료 중복 게시 막기, 다른 Run 덱으로 재평가되면 이전 출전 예약 취소, 확인 창 키보드 선택 범위 제한.
- 사용자 정리: 새 출전 버튼만 남기고 씬의 옛 버튼을 지워 원본 프리팹에도 반영(사용자가 저장한 확인 창 크기·이미지 유지). 같은 날 확인 창 조건을 "변경 있음" → "빈 칸 있음"으로 바꿈.
- 확인: 새 프리팹 블록 ID·참조·부모 자식, C#·`.meta` 짝, 코드 diff 공백. Unity 컴파일·재생 미확인. 공유: [덱 출전 공유](../handoffs/2026-10-29-deck-combat-entry-share-Hojin.md)(미전달).

### 3.18 노드 호버 정보 (11-02~11-03)

설계 §5.7. 건물에 마우스를 올리면 전역 HoverPannel(`IUITextPresentation`)에 그 스테이지의 제목·설명을 보인다. 결정: Q-NH1 A(비전투 = 공통 화면 글 표, 전투 = 노드 외형 표)·Q-NH3~Q-NH5 A, 계획에서 정한 P-NH1~P-NH7 그대로(11-03 사용자). 새 시험 없음, 공유 메모 없음(현강님 패널·서비스는 공개 입구만 씀).

- NH-a 글 칸: `StageNodeVisualDataSO`·`StageNodeVisualData`에 `description`(생성자 끝 선택 인자, null → 빈 문자열), 전투 외형 에셋 이름 "일반 전투"·"보스 전투"(임시, 설명은 비움 — 사용자·기획이 넣음). 코드와 에셋은 한 커밋.
- NH-b 호버 표시: 새 `StageMap/StageNodeHoverInfo.cs`(`.meta`는 Unity가 만듦 — 커밋 때 함께), `StageMapInteractionLog.cs`(+ `.meta`) 삭제, 피드백 계약 넷(`HoverChanged(chapterKey, nodeId)`·`HoverMoved`·`NodePressed`·`EntryRequested`), `StageMapInteraction.ClearPointer`, `StageMapViewSystem` 조립 한 줄, `StageMapViewScope.Tick` 끝 `ClearHoverOutsideSelection`.
- 설계와 다른 점(P-NH1·P-NH3, 승인): 팝업·설정 창 때 지우기는 `SetPopupBlocked`가 아니라 Tick 끝 Layer 확인 한 줄(설정 창은 `OverrideLayer`로 Layer만 바꿔 알림이 없음), 패널 서비스는 Services가 아니라 부품 생성자로.
- 계획에 없던 것: 표시를 마치기 전(챕터 0)·다시 그리기 직전이라 목록에 없는 노드는 띄우지 않음, 이미 밖에서 닫힌 패널은 `UnHover`하지 않음(`HoverHideVersion`).
- NH-c 문서: 설계 §5.7 구현 메모·§5.2·§8 한 줄, [스테이지 맵 안내](../viewDocs/stage-map-guide-Hojin.md) §2.1(호버 글 넣는 곳).
- 확인: Ua·Ub 문제 없음(11-03 사용자 — 컴파일, 설명 칸, 서로 다른 시설 글, 커서 따라감·가장자리, UI 위·누름·팝업·설정 창·끌기 때 숨김과 다시 뜸, 현재·완료·시작 노드, 전투 전환 뒤 남지 않음).
- 묶음 끝 검토(11-03, 새 에이전트 하나, 읽기 전용): 약 8.6(반영 뒤 예상 약 9.2), 높음 0·중간 1·낮음 12. M1 글이 없을 때 예외가 입력 콜백에서 터져 오류 경계(F1)를 거치지 않음(같은 Scope의 `BlocksEntryForGrowth`·`InterceptEntry`는 경계에 보고함). 낮음: L1 숨김 규칙 세 곳·Scope가 입력 부품 안을 봄(설계 점검 줄과 다름), L2 "맵 선택 Layer인가" 식 다섯 곳, L3 노드 → 외형 조회 두 곳, L4 연결마다 상태 있는 부품이 서비스 수명 묶음에, L5 누른 채 밖에 나갔다 돌아와 떼면 패널이 보이는 채로 진입, L6 `NodePressed` 인자 안 씀, L7 진입 로그 자리, L8 예외 다시 감싸기·닿지 않는 검사, L9 `GetStages`가 읽기 전용 감싸개를 새로 만듦(문서 "할당 없음" 틀림), L10 전투 환경 호버는 설명이 비면 안 띄움(P-NH6 "같은 규칙" 틀림), L11 비전투 외형의 이름·설명 칸이 안 쓰이는데 보임, L12 에디터 구도 미리보기가 카메라를 옮기면 패널이 옛 글로 남음. 문제없음 확인: 씬 전환·종료 순서, 팝업·설정·튜토리얼 Layer, 맵 다시 그리기, 노드 이동·출전, 루트 재등록·직접 조회 없음.
- **검토 반영(11-03 사용자 "권장대로 반영", 새 시험 없음, Uc 문제 없음 — 사용자):**
  - M1: `StageNodeHoverInfo`가 생성 인자로 보고 함수(`_faults.Report`)를 받아 `HoverChanged`·`HoverMoved`·`NodePressed`의 오류를 오류 경계에 보내고 그 대상을 버린다. 비전투 글이 없을 때는 표의 `KeyNotFoundException` 그대로(P-NH5의 `InvalidOperationException` 감싸기는 L8로 걷음).
  - L5·L6: 맵 입력이 뗄 때(고르는 순간, 진입 요청 직전)에도 `NodePressed`를 알린다. `NodePressed`는 그 노드가 호버 대상일 때만 억제한다.
  - L1·L2: `StageMapViewInput.IsSelectionLayer` 하나로 모으고(입력 둘·Scope 둘이 부름), 호버 지우기를 `StageMapViewInput.ReleaseHoverOutsideSelection`으로 옮김(Scope Tick은 한 줄로 부름).
  - L3: 노드 → 외형 규칙 `StageMapViewServices.VisualOf` 하나를 건물 표시(`FindVisual`)와 호버가 같이 부른다. L8: 닿지 않는 "전투 외형 없음" 검사를 걷음.
  - L4: Services `Feedback` 칸 주석(연결 사이 상태를 갖고 입력 정리가 비움). L9·L10: 설계 구현 메모 문구 고침, 설계 점검 줄을 숨김 규칙 세 갈래로.
  - 그대로: L7(설계 NH2대로), L11(툴팁으로 충분), L12(에디터 전용, 재현 미확인).
  - Uc(사용자): ① 컴파일 ② Ub ①·⑤·⑥ 다시(호버 표시, 누름·같은 건물, 팝업·설정 창) ③ 건물을 누른 채 밖에 나갔다 돌아와 떼면 패널 없이 진입 ④ 범례·덱 버튼·연결선 등 기존 맵 동작(선택 화면 표시·증강 자동 열기 조건이 같은 판정을 씀).
- **NH-d 3판 — 모든 스테이지 글을 StageOfferText로(11-03 사용자 "공통 줄 + 키 전용", 새 시험 없음, Ud 문제 없음 — 사용자, 설계 §5.7 3판):**
  - 코드: `StageOfferTextTarget.Info = 2`, SO 칸 `stageType`·`mapRole`(Info이고 키가 빌 때만, 툴팁), Data `IsCommonInfo`·`StageType`·`MapRole`·검사(Info는 이름·설명만 필수, 공통 줄 종류·역할 확인), 표 `GetNodeText(키, 종류, 역할)`(키 줄 → 공통 줄)·공통 줄 겹침 검사·`GetForStage`는 `Stage` 줄만. `StageNodeHoverInfo`는 종류 갈래·외형 표를 걷고 `GetNodeText` 하나, `StageMapViewSystem` 생성 줄 인자 하나 줄임.
  - 되돌림(커밋 전이던 것): 노드 외형 `description` 칸·툴팁·Data 인자, 전투 외형 이름(다시 "Normal Stage A"·"Boss Stage"), `StageMapViewServices.VisualOf`(인라인으로). 외형 코드 두 파일은 HEAD와 같아짐.
  - 에셋: 새 `StageOfferText_Combat`(Info, 전투·일반 "일반 전투")·`StageOfferText_Boss`(Info, 전투·보스 "보스 전투")(+ `.meta`), `StageOfferTextDataTable` 두 줄. 설명은 임시("전투를 치르는 스테이지입니다." / "이 챕터의 보스와 싸우는 스테이지입니다."). 코드와 에셋은 한 커밋(코드만이면 새 칸 값이 기본, 에셋만이면 `target: 2`가 없는 값이라 DB 생성 실패).
  - 문서: 안내 §2.1 다시 씀, 공통 화면 설계 SO5 칸 표, 설계 §5.2·§5.7 3판·점검 줄.
  - Ud(사용자): ① 컴파일, DB 생성 오류 없음 ② 일반·보스 전투 건물 호버에 새 글 ③ 용병 고용소·훈련소·상점·회복 글 그대로 ④ 상점·훈련소·회복 팝업 머리줄·버튼 글 그대로 ⑤ `StageOfferText_Combat` 인스펙터에 Stage Type·Map Role 칸.
- 보고만: 노드 외형 전용 항목의 `Stage Key` 1·2·3·4가 실제 스테이지 키(1201~1303)와 맞지 않아 쓰이지 않는다. 그래서 Shop 종류 시설 셋은 모두 `_MainCharacterUpgradeStageNode`로, 회복 셋은 `_RandomEventStageNode`로 그려진다(설계 §5.7). 안내 §2의 SO 표 이름(`NodeVisual_Shop_Regular` 등)도 지금 에셋 이름과 다르다. → 11-03 작업 트리에서 사용자가 고침(이 세션 밖, 커밋 전): `NodeVisual_MercenaryRecruitment` 2 → 1201, `NodeVisual_SupportingCharacterGrowth` 3 → 1202, `NodeVisual_Recovery` 4 → 0(공통 Event), 표에서 `NodeVisual_RandomEvent` 등록을 뺌(에셋은 남음). 호버 글은 전투만 외형 표를 읽어 영향 없음. 안내 §2 표는 아직 옛 이름.
  - 11-03 고침(에셋만, 컴파일·재생 미확인): 용병 1201·훈련소 1202 전용, 회복은 Event 공통(키 0), `NodeVisual_RandomEvent`는 표 등록에서 뺌(에셋 남김). 내용은 설계 §5.7 같은 줄 아래.

### 3.19 챕터 2 진입 임시 막기 (11-11)

빌드용 임시 조치다(사용자 "챕터2는 아직 다 만든게 아니라서 막는게 좋을꺼 같습니다"). 에셋 한 줄만 바꿨고 코드는 그대로다. 재생 미확인, 커밋 전.

- 바꾼 것: `DataBaseSO/InGameGroup/StageMap/Item/StageMap_Chapter1.asset`의 `nextChapterKey` 2 → 0. 이 칸은 `1258dd685`(11-05)에서 챕터 2를 열 때 2가 됐다. SO 툴팁대로 0이면 마지막 챕터라, 챕터 1 보스 승리가 Run 승리로 끝난다(결과 화면·엔딩 → 로비).
- 그대로 둔 것: `StageMap_Chapter2.asset`·표 등록·챕터 2 스테이지 에셋. 표 검사는 다음 챕터 키가 0보다 클 때만 등록을 보므로 지금 값으로 DB 생성은 그대로다. 챕터 2는 어디서도 이어지지 않아 들어갈 수 없다.
- 되돌리기: 챕터 2가 준비되면 같은 칸을 2로 돌린다.
- 확인(사용자): ① DB 생성 오류 없음 ② 챕터 1 보스를 이기면 챕터 2 맵이 아니라 Run 승리 결과·엔딩 뒤 로비로 감.

## 4 c1b9b0a 병합 검토 (2026-10-11)

### 4.1 병합한 것

- 대상: 천명님 커밋 `c1b9b0a3`(`origin/feature/Cheon/refactor/core-system`). 내 브랜치 `feature/Hojin/Stage_Shop_Event`, 공통 조상 `758d0387`. 사용자 요청은 "상대방 코드를 우선하고, 내 코드를 새 구조에 맞춘다"였다.
- 상대 변경의 뜻: 전투 진행에서 `CombatFlow`·`CombatExitSequence`를 걷고 `CombatPlan.Decide`가 다음 단계를 정한다. 전투 진입은 독립 `CombatEntryService`가 요청 하나를 소유하고, Prepare 다음 `CombatEntryTransitionJob`이 전환한다.
- 결론: 상대 우선 자동 병합만으로는 빌드되지 않았다. 고칠 곳은 Job 등록과 맵 → 전투 진입 수명 연결이었다. Framework 규칙은 전투의 `Decide` 꼴을 모든 도메인에 복제하라고 하지 않으므로 맵·상점·아티팩트 Flow는 그대로 두었다.
- 사용자가 상대 우선으로 직접 병합했다(`9d4c76e5`).

### 4.2 병합 후 연결 보완

수정 전 Unity 컴파일이 실패했다(CS0535: `CombatEntryService`가 맵 번호 인터페이스 미구현, CS0246·CS0538: 지워진 `ILifeCycleTransitionGuard` 참조). 운영 코드 여덟·시험 아홉을 고쳤고 새 런타임 클래스·서비스·스케줄러는 만들지 않았다(`dce453db`).

| 운영 파일 | 보완 |
|---|---|
| `JobRegisterSystem` | Stage seed·Artifact Buffer·Shop Plan을 같은 Register·Runner에 연결. 상대 Combat 등록 유지 |
| `LifeCycleSystemGroup`·`SystemGroupsService` | `StageEntryAttempts`·Core 명령 서비스 조회 계약 복원 |
| `CombatEntryService` | 첫 미처리 입력의 번호·챕터·노드 검증, 같은 Queued 요청 재사용 조건 |
| `CombatPrepareJob` | 양수 맵 번호는 Core `StageCommands` 직접 조회, 입력이 사라지면 미실행 취소. Bootstrap 번호 0 경로 유지 |
| `StageLifeCycle` | 같은 요청·번호·수락·Job 완료·Activating·Combat 목적의 정상 동기 전환만 Runner busy 허용 |
| `LifeCycleManager` | 일반 Exit 실패 때 활성 참조 복원 |
| `LifeCycleSystemFactory` | 같은 `RuntimeFaultBoundary`를 Stage·Combat·EntryService에 주입(`new StageLifeCycle(faults)`) |

실제 Play 자동 검증 4/4(상점 노드 → 구매 → 나가기 → 맵 갱신 → 다음 전투, 아티팩트 표시·편집, 전투 GridOutline)를 통과했다. 화면 미관·물리 마우스·Profiler 확인은 아니다. 읽기 전용 독립 검토 차단 지적 0.

### 4.3 한 번만 소유하는 실행 경계

병합 때 정한 소유권 결론이다. 2026-10-27 코드에서 아래 이름이 그대로 있다(Grep). 전투 진입의 세부 단계는 그 뒤 전투 골격 작업으로 바뀌었으므로 전투 문서를 따른다.

| 범위 | 소유자 |
|---|---|
| Runtime 구동 | `GameBootstrap` 하나 |
| 작업 등록·실행 | Global `JobRegisterSystem`·`JobRunnerSystem`. 도메인 Plan이 같은 Runner에 등록 |
| 전투 진입 요청 | 독립 `CombatEntryService` 한 곳 |
| 맵·상점 입력 처리 | Core `StageCommandProcessingService`(번호 확인 뒤 Buffer를 읽음) |
| 아티팩트 입력 처리 | Core `ArtifactEditingService`의 명령 처리 부분(두 인터페이스가 같은 인스턴스) |
| 런타임 오류 상태 | 하나의 `RuntimeFaultBoundary`를 모든 소비자에 주입 |
| 공용 입력 정리 연결·해제 | `InGameViewFaultCleanup`이 콜백 하나를 소유하고 화면별 `StopInput`을 부름 |

등록 이름·인터페이스가 여러 개라고 중복 실행은 아니다. Core의 맵·상점, 아티팩트, 전투 서비스는 서로 다른 규칙이라 각각 둔다. 정리 함수의 멱등 재호출과 공용 콜백 중복 연결은 다른 문제다. 옛 `CombatFlow`·`CombatExitSequence`·전환 guard·상점 Scheduler는 되살리지 않았다.

### 4.4 문서 병합 정정

- 상대 쪽 설명 문서(추가 16·수정 7)는 "상대 커밋 `c1b9b0a` 기준 이력" 머리를 붙여 보존했다. 전투 문서 다섯이 이 검토를 그 근거로 가리킨다.
- 2026-10-13 정정: [Core 경계 인계](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)의 결합은 `a441daa1`까지만 지켜졌다. 실제 병합 `9d4c76e5`에서 상대 판이 채택돼 내 절이 빠졌고, 2026-10-13에 새 번호로 복원했다(§5.13~§5.18, §13.13~§13.16). 번호 대응은 그 문서의 기록 위치 표를 본다.
- 민형님 Framework 원본 셋은 보존하고, 갱신은 [Framework 공유](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md) §10에 요청한다.

## 5 보류 — Stage Job·Plan 재구성 (SJ)

- 보류(2026-10-26 사용자): 착수하지 않는다. 코드는 그대로다(Flow 넷·`ShopPlan`·`RecoveryPlan`·`JobRootType.Shop = 4`·`Recovery = 5`). 이유·다시 시작할 조건·그때 할 일은 [설계](../specs/2026-10-10-stage-map-design-Hojin.md) §11이 원본이다.
- 설계 결정(2026-10-25 확정): Q-S1 E·Q-S2 A·Q-S3 C·Q-S4 D·Q-S5 A·Q-S6 A, 수정안 3·4, P1~P10.
- 계획에서 정한 것: SP13은 확정(10-25 사용자: 원래 설계대로 `StagePlan`·`StageJob`의 민형님·천명님 줄을 고치고 공유 메모에 적는다, 천명님 시험 셋은 고친다, `JobType.NewRun`은 남긴다). SP1~SP12는 확인 대기다.
  - SP1 Phase 셋, SP2 규칙 함수 `StageCommandFacts`, SP3 `StageStepKind`·`StageStep`, SP4 `L/RunStart/RunStartContract.RequireInstalledRoster`, SP5 `StageLifeCycle` 생성자 필수 인자, SP6 `StageProgress` 의존은 `PostBindService`로, SP7 방문 번호, SP8 기록 누락 판정 한 줄, SP9 어긋남 문구, SP10 Job 우선순위 그대로, SP11 Plan 입구 시험은 생성 입구로, SP12 공유 빈 목록.
- 용근님 코드는 연결 부분만 바꾼다(10-25 사용자). 전투 쪽과 덱 편집 창 파일은 바꾸지 않는다.
- 확인 대기 두 건: 대상이 없어져 함께 지워지는 천명님 시험 `ShopReceipt_WaitsForViewAcknowledgmentBeforeSchedulingNextRequest`(`StageFlowTests`)와 `Enter_WithoutRoster_RegistersNewRunInsteadOfStart`(`ArtifactBoardConfigTests`).

다시 시작할 때의 Phase 개요(줄 번호는 모두 `130b33085` 기준이라 착수 때 다시 대조한다):

| Phase | 내용 | 동작 |
|---|---|---|
| SJ-1 | 새 부품: `StageCommandFacts`, `StageVisit`(상태만), `StageProgressFacts`·`StageProgress`, `StageStep`, `StagePlan`의 순수 함수(`Decide`·`TryGetCombatEntry`·`CanLeave`·`FirstChapterKey`), `RunStartContract`(로비 Job이 먼저 씀), 조립 줄 | 같음 |
| SJ-2 | 한 번에 전환: Job 다섯, `IStageVisitJobs`, `StagePlan.Register`(`Next`만), JobRegister 줄, `StageLifeCycle` 트리거, 바깥 계약(`IStageEntryAttemptRead`·`IStageDeckEditFacts`)을 `StageProgress`로, 회복 취소 자리를 View Detach로, Flow 넷·`ShopPlan`·`RecoveryPlan`·`JobRootType` 두 값 삭제, 깨지는 시험 맞춤. 중간 상태가 컴파일되지 않아 한 커밋 | 바뀜(설계 B1~B12) |
| SJ-3 | 안내 문서·이전 결정 표시, 공유 메모 셋(용근님·민형님·Core 전달 §5) | — |

착수 조건에 "고칠 파일·줄·주인 재대조"를 더한다. 옛 계획의 줄 주인 표와 시험 조정 표는 git `10914570`에 있다.

## 6 다른 작업자 파일 변경

| 작업 | 파일(주인) | 변경 | 공유 |
|---|---|---|---|
| 선택 View T3 | `ChapterGraphPreparationTests`(천명) | `withJobRuntime` 조립 수정(10-03 사용자 결정) | Core 전달 §5.5 |
| 선택 View 10-04 | `PlayerInputActions.inputactions`(공용 입력 에셋) | Artifact 회전 바인딩 Q·E 추가(사용자 결정) | — |
| SA P3.9 | `CombatViewSystem.cs`(용근), `InGameViewSystemGroup`, `InGameViewSystemFactory.cs`(민형 Factory) | 공용 콜백 해제를 View 그룹으로, Factory가 `InGameViewFaultCleanup` 조립 | 공용 오류 협의(미전달), Core 전달 §5.16 |
| SA P4 | `BufferSystemGroup`·`BufferSystemFactory`·Buffer 키·`IBufferQuery`, `JobRegisterSystem`·`JobRootType`(민형 Factory·ServiceKeys·Job 영역) | Shop Buffer 등록, `JobRootType.Shop = 4`·Shop Plan 등록 | [Framework 공유](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md), Core 전달 §5.15 |
| SA P5·P6 | `CombatEntryRequest.cs`·`CombatPrepareJob.cs`·진입 서비스(전투 골격) | 맵 번호 전달·등록 전 대응 검사·미실행 false 내부 취소(사용자 승인 로컬 변경) | [Core 교체 인계](../handoffs/2026-10-11-stageselect-core-replacement-handoff-Hojin.md), 공용 오류 협의 |
| c1b9b0a 보완 | `JobRegisterSystem`·`LifeCycleSystemGroup`·`SystemGroupsService`·`LifeCycleManager`·`LifeCycleSystemFactory`(민형), `CombatEntryService`·`CombatPrepareJob`(전투 골격) | §4.2 표 | Framework 공유, Core 교체 인계 |
| NM2 | `CombatEntryService`·전투 진입 전환 Job(전투 골격) | 맵 표시 owner 역할·lease·ticket 대기 | [노드 이동 공유](../handoffs/2026-10-16-stage-map-node-movement-framework-combat-share-Hojin.md)(미전달) |
| NM3 | [전투 Job·Plan 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md) J5(용근 문서) | 현재 원본을 가리키는 좁은 참조 한 줄 | 노드 이동 공유 |
| 범례 | `GameBootstrap.OpenStageUI`(용근) | guard·열기 두 줄 추가(뒤에 Canvas P2가 지움) | 범례 UI 공유 |
| 배경 P2 | `ViewDataBaseFactory.cs`·`ViewTableKey.cs`(민형), `ViewDataBase.cs`, `ViewGroup.asset` | 배경 표 등록·조회 노출 | 배경 공유(미전달) |
| Canvas P2 | `UIIDs.cs`(민형), `GameBootstrap`(용근), `DeckEditUI.cs`(용근) | `StageScreenRoot = 40` 추가, StageSelect 초기 Open 두 줄 제거, `DeckEditUI.ParentID` = StageSelect | Canvas 통합 인계(미발송) |
| UI 소유권 P1 | `DeckEditUI.prefab`(용근) | 독립 Canvas·Scaler·Raycaster 복원(원래 GUID) | Canvas 통합 인계 머리 |
| UI 소유권 P2 | `StageDeckEditUISetup.cs`(용근), `StageShopUISetup.cs`(호진) | 새 소유자·부모·Canvas 연결(사용자 승인) | 없음 |

## 7 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [Core 요청](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md)(1부 전투 진입, 3부 Run seed R4, 4부 처리 번호 H1~H4) | 천명님 | 1부는 10-13 종결(Core 작업으로 해소). 3부·4부 미전달·미합의 |
| [Core 교체 인계](../handoffs/2026-10-11-stageselect-core-replacement-handoff-Hojin.md) | Core·전투 실행 계층 | 미전달 |
| [Core 경계 인계](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.5·§5.15~§5.18 | 천명님 | §5.15~§5.18 미전달 |
| [Framework 공유](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md)(§10 원본 갱신 요청, §12 옛 구조 설명 갱신 요청) | 민형님 | 미전달·미합의, 전달 전 갱신 필요 |
| [상점 UI 공유](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md) | 현강님 | 미전달 |
| [공용 오류 협의](../handoffs/2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md) | 용근님(전투) | 미전달·미합의 |
| [카메라 공유](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md) | Framework·Render 카메라 담당 | 미전달 |
| [노드 이동 공유](../handoffs/2026-10-16-stage-map-node-movement-framework-combat-share-Hojin.md) | Framework·전투 진입 담당 | 미전달 |
| [범례 UI 공유](../handoffs/2026-10-17-stage-legend-ui-handoff-Hojin.md) | UI 작업자 | 미발송 |
| [배경 공유](../handoffs/2026-10-25-stage-map-background-framework-share-Hojin.md) | 민형님 | 미전달 |
| [Canvas 통합 인계](../handoffs/2026-10-26-stage-select-canvas-integration-handoff-Hojin.md) | Framework·UI 작업자 | 미발송 |

## 8 관련 문서

- [로비·스테이지 맵 검사 후속 정리 계획](2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md): 10-26 검사 1~6번(이름 조회 제거·필수 의존·작은 정리·제출 중).
- [상점 계획](2026-10-09-stage-shop-plan-Hojin.md): 상점 S0~S5, 상점 안내·위치 표시, 이벤트 팝업 UI.
- [회복·이벤트 계획](2026-10-11-noncombat-event-recovery-plan-Hojin.md): 이벤트 노드 → 회복 진입.
- [View 구조 계획](2026-10-13-view-structure-plan-Hojin.md): `StageMapViewScope`·`PopupViewScope`·입력 조립·책임 분리.
- [로비 계획](2026-10-11-lobby-plan-Hojin.md): LV5 아티팩트 이전, 로비 이동 원본.
- [아티팩트 계획](2026-10-05-artifact-popup-plan-Hojin.md): 아티팩트 팝업 초기 진행.
