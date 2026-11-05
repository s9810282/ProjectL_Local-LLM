# 전투 보드·선로 진입 연출 — 진행 계약·조립 공유 (2026-11-03)

R1~R9 코드 구현 완료, **컴파일 미확인**. 최신 R9는 맵 선로 양끝 5칸 표시 연장이다. R4 기본 동작은 사용자가 확인했다. 이 문서는 로컬 공유 메모이며 외부 발송하지 않았다.

로딩 화면이 완전히 걷힌 다음 실제 생성 타일과 Terrain·배경 구역이 순차 등장한다. 보드 완료 후 기믹을 공개하고 선로가 0번 쪽 끝부터 순차 낙하한다. 선로가 모두 착지하면 적 Nexus가 낙하한다. 지면의 FallingRocks가 자연 종료되면 초기 적이 기존 소환 상승 조각으로 등장한다. 유닛 등장 완료 후 상시 효과를 켜고 입력과 자동 전투 진행을 재개한다.

## 변경 계약

- `ICombatProgressView.IsEntryPresentationPending` 추가. `CombatViewProgress`가 현재 연결의 진입 객체를 조회하며 별도 상태를 저장하지 않는다. 별도 구현체가 있다면 이 읽기 프로퍼티를 제공해야 한다.
- `CombatProgressFacts`와 `CombatInputFacts`에 같은 사실을 추가했다. P < S 기반 `IsPresenting`은 변경하지 않았다. `CombatPlan`은 진입 중 새 처리를 기다리되 완료/포기의 제출 정지 우선순위를 유지한다.
- 내부 `ICombatEntryPresentation`은 `IsPending`, `Tick(deltaTime, isLoading)`, `Cancel()`만 제공한다. 표시가 소유하고 씬 연결은 읽기/진행/정리 참조만 갖는다.
- 내부 `IFieldHighlightRenderer.SetVisible`은 기존 필드 상태를 보존한 채 표시를 억제한다. CellPrefab은 필드 표시 루트를 숨기고, GridOutline은 같은 루트와 Global 소유 테두리의 빈 칸 Update를 함께 사용한다. Render 공용 API 변경은 없다.
- 실제 조립의 `CombatViewSystem.BindServices`는 기존 로딩 등록 서비스와 함께 로딩 상태 서비스도 받는다. 진입 없는 시험 조립에서만 상태 인자를 생략할 수 있고, 진입 객체가 있는 연결에서 누락되면 인수 전에 예외로 거절한다.

## 생성·등록·조회 경로 점검

| 대상 | 생성 | 내부 등록/보관 | 상위 제공 | 소비 |
|---|---|---|---|---|
| 로딩 상태 | 기존 `LodingFactory`의 `LodingSystem` | 로딩 Builder의 `ILodingSystemService`, `LodingSystemGroup.LodingService` | 로딩 Context를 Global 그룹이 `LodingServiceCommand`로 제공 | CombatViewSystem.PostBindService → Global Context → LodingService |
| 전투 View | 기존 `InGameViewSystemFactory` | View Builder의 `ICombatViewService`와 Tick/Shutdown 모듈 | `InGameViewSystemGroup.Combat` → InGame Context | 기존 LifeCycle/Buffer/씬 Enter/Exit 흐름 |
| 진입 부품 | `CombatSceneDisplayFactory` → `CombatBoardEntryFactory` | `CombatSceneDisplay`와 현재 `CombatViewConnection`의 참조 | 서비스 Export 없음 | Install의 Prepare, CombatViewSystem의 Tick/Cancel |

같은 Builder의 중복 등록과 하위 서비스를 루트에 재등록하는 우회 모두 추가하지 않았다. 새 UI 화면이나 별도 UI 등록도 없다. 유닛 HUD의 내부 소유자 표시만 억제하고 기존 UI 등록·해제 수명을 유지한다.

## 수명과 설정

- 완성 위치에서 첫 구성을 끝내고 타일·지형·적·격자 표시를 준비한다. AfterLoad나 로딩 진행률 1을 시작 조건으로 사용하지 않는다.
- 적은 일반 완료의 `CombatComposition`에 Pose와 기존 `CombatRisePiece`를 조립한다. 가짜 Core 사건을 발행하지 않는다.
- 오류·설치 실패·씬 해제에서는 자세와 표시 및 대여를 복구한 뒤 기존 표시/배경을 반환한다. 지난 완료 콜백으로 새 입력을 여는 경로는 없다.
- View 연출 설정의 `boardReveal`: enabled=true, sweepSeconds=1, tileSeconds=0.4, depth=1.5, overshoot=0.15. 적 시간/깊이/회전은 기존 SummonRise 설정을 그대로 사용한다. 기존 설정 에셋이나 씬은 수정하지 않았다.
- Core·FieldCamera·Architecture 변경 없음. 컴파일·재생은 사용자가 Unity에서 확인한다. 상세 수동 인수는 [구현 계획](../plans/2026-11-02-combat-board-reveal-plan.md)의 Unity 인수표를 따른다.

## 배경 구역 연결 추가 (R4)

`StageViewPropPresenter`는 타일 정렬 뒤 `CombatBackdropRevealTargets`로 `DayMap/BackGroundProp/RevealArea_*` 직속 구역을 읽는다. 각 구역은 필드 로컬 X·Z에서 가까운 생성 타일을 기준으로 기존 시간표를 공유한다. `CombatBoardEntryFactory`가 읽기 전용 대상 목록을 `CombatBoardEntryPresentation` 생성자의 두 번째 인자로 전달한다. 진입 Prepare가 기존 타일/지형 대상에 구역을 추가하며, 공개 진행 계약과 서비스 등록은 그대로다. 현재 `NewStageTest`의 29개 구역은 별도 Inspector 연결 없이 참여한다.

R3의 Default·Coner와 증가 묶음 설정도 그대로 사용한다. 구역 수는 묶음 크기에 포함하지 않으며, 배경 안착까지 기존 보드 완료를 기다린 뒤 적 소환을 시작한다. 구역의 위치·Renderer 표시는 기존 취소/완료/씬 해제 경로에서 복구한다. 카메라와 조명은 등록 구역 밖에 있다. 이번 R4에서는 프리팹·씬·설정 에셋을 수정하지 않았다.

## 적 Nexus 낙하 추가 (R5)

설치 후 내부 `CombatBoardEntryPresentation.Prepare`는 Characters와 Structures를 함께 받는다. 실제 적 Nexus는 `FieldUnitPresenter`가 살아 있는 적 `Structure_Headquarters`로 수집한다. 배경 장식 건물은 이 대상이 아니다. `CombatBoardEntryFactory.Create`에 기존 `CombatEffectWindow` 인자가 추가됐고, `CombatSceneDisplayFactory`가 사건 재생과 같은 창구를 전달한다. Global Render 서비스의 생성·등록·Export/조회 경로와 공개 진행 계약은 변경하지 않았다.

진입 소유자는 보드→Nexus→적 순서를 담당한다. Nexus 전용 부품이 자세 대여와 숨김/HUD를 관리하고, 완성 배치의 지면 중심에서 `FallingRocks`를 1회 재생해 자연 종료까지 기다린다. Nexus의 취소는 자기 자세와 이펙트만 반환하며 공용 효과 창구를 닫지 않는다. 기존 설치 실패/오류/해제가 먼저 진입을 취소하고 표시·효과 창구를 반환한다. 설정은 기존 SO의 `nexusDrop`에 Enabled/Height/Seconds로 추가했다. 설정 에셋을 변경했으므로 Unity 재생과 DataBaseAssetTests 확인은 사용자에게 인계한다.

## 기믹·상시 효과 공개 시점 추가 (R6)

`CombatSceneDisplayFactory`가 새 `CombatEntryDeferredDisplay` 하나를 만들고 SceneDisplay 생성자와 `CombatBoardEntryFactory.Create`에 함께 전달한다. SceneDisplay는 이벤트 Bind 뒤, 최초 Refresh 전에 Entry.PrepareDisplay를 호출한다. 기믹 최종 사실은 같은 부품의 Render를 통하며, 보드 완료에 토네이도·낙석 경고·모래바람을 공개한다. 기차 선로는 기존 로딩 중 Install/Paths를 유지하고 설치 루트 표시만 이때 함께 공개한다.

`CombatShownFacts.SyncPersistent`는 진입 보류 중 상시/상태 유지 효과 생성을 건너뛴다. 유닛 등장 완료에만 현재 표시 사실로 효과를 동기화한다. Nexus 착지 같은 일회성 효과 창구는 억제하지 않는다. 정상 완료와 Cancel을 구분해, 취소 때 새 효과/기믹을 재생하지 않고 스냅샷과 보류를 버린다. 공개 진행 계약·서비스 등록·Global Render 조회/Export와 기존 기믹 발동 경로는 변경하지 않았다.

## 선로 순차 낙하 추가 (R7, 2026-11-03)

외부 연장 정책은 아래 R8로 대체한다. 이 절의 외부 조각 포함 순서는 당시 구현 이력이다.

R6의 보드 완료 시 선로 전체 공개를 순차 낙하로 확장했다. `FieldRailwayPresenter.SegmentTracks`는 기존 설치된 RailTile/Rail 쌍을 직선별 0번 쪽 외부 끝→보드→반대편 외부 끝 순서로 제공한다. `CombatBoardEntryFactory.Create`는 같은 Railway Presenter를 추가 인자로 받아 목록을 `CombatEntryRailwayAppearance`에 전달한다. Factory 생성 뒤 Install에서 채워진 목록을 Prepare에서 수집하므로 설치 전 빈 복사본을 보관하지 않는다.

보드 완료 때 설치 루트가 켜져도 개별 대기 조각은 숨겨져 있으며 순번에 맞춰 공개/가속 낙하한다. 선로가 모두 완료돼야 Nexus를 시작한다. 여러 직선은 나란히 시작하고 열차 Reverse와 독립적이다. 취소는 선로 제거 전에 원래 위치와 Renderer 표시를 복구한다. 공개 진행 계약과 열차 발동/경로/이펙트 서비스 경로는 유지했다.

`CombatPresentationSettings_Default`의 Railway Drop에서 Height=10, Seconds=0.45, Segment Gap Seconds=0.08을 조절한다. Enabled=false는 보드 완료 때 즉시 전체 선로 표시다. Board Reveal/Nexus Drop의 기존 사용자 수치는 보존했다. 컴파일/재생 및 DataBaseAssetTests 확인은 사용자에게 인계한다.

## 맵 내부 선로만 생성 (R8, 2026-11-03)

사용자 결정으로 선로 외형은 `RailwayTrackLayout.Track.Cells`의 맵 내부 칸에만 생성한다. 기존 양쪽 230칸씩 연장은 만들지 않는다. 외부 460조각을 기다리던 약 36.8초가 제거되며, 이제 보드 첫 선로부터 낙하하고 직선 칸 수 N에 대해 `(N-1)×SegmentGapSeconds+Seconds` 뒤 선로 단계가 끝난다. 바깥 조각을 별도 묶음으로 떨어뜨리는 제안 대신 생성 자체를 제거한 결정이다.

OutsideCells/TravelPaddingCells는 열차의 화면 밖 이동 여유로만 남으며 Inspector 설명을 정정했다. AddTrain의 경로·속도·접촉과 설정 에셋 수치는 보존했다. 진입 진행/취소/등록/Export 계약 변경 없음. Unity에서 맵 경계의 선로 끝, 바로 시작되는 낙하, 이어지는 Nexus/적, 실제 열차 발동을 확인한다. **컴파일 미확인**.

## 양 끝 5칸 표시 연장 (R9, 2026-11-03)

최종 사용자 요청으로 R8을 확장했다. 각 직선 첫/마지막 칸에서 각각 5칸을 표시용으로 더 만든다. RailwayVisual_Default의 **Rail Extension Cells=5**가 새 조절 지점이며 0이면 맵 내부만 생성한다. OutsideCells=230은 열차 이동 여유로만 남고 생성 개수로 사용하지 않는다. 추가 구간도 기존 순차 낙하에 참여해 현재 간격에서는 0.8초가 추가된다. 열차 경로/접촉과 진입 공개 계약은 유지했다. 코드/정적 검토 완료, **컴파일 미확인**. Unity에서 양끝 5칸/낙하/열차 발동과 DataBaseAssetTests를 확인한다.
