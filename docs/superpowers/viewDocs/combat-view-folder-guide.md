# 전투 View 폴더 배치 안내 — 새 파일을 어디에 두나

- 작성일: 2026-10-21
- 갱신: 2026-10-23 조정자 C 단계([설계](../specs/2026-10-23-combat-view-coordinator-c-stage-design.md) Q-C2 A) — 진행 사실 `CombatViewProgress`를 `Input/` 바로 아래에 새로 두고, 포기 컨트롤러를 `ActionButtons/` → `UiEntry/`로 옮김(§0·§1.1·§2)
- 갱신: 2026-10-25 View 단계 2라운드([설계](../specs/2026-10-24-combat-view-stage-round2-design.md) P15, [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md)) — 새 파일 열하나의 자리, 사건 경로 표 `CombatEventRoutes` → 사건 형식 표 `CombatEventTypeTable`(이름 바꾸기), `CombatActionBoundary` 삭제, 생성 계약·공통 수명 어댑터·사건 형식 지식의 규칙(§0·§1.1·§2)
- 갱신: 2026-10-25 View 단계 3라운드([설계](../specs/2026-10-25-combat-view-stage-round3-design.md) Q-V22 A, [계획](../plans/2026-10-25-combat-view-stage-round3-plan.md)) — 새 파일 열여섯의 자리(루트 둘 포함), 수명별 객체·공격 종류 전략·강조 층 표·소리 프리셋의 규칙(§0·§1·§1.1·§2)
- 갱신: 2026-10-27 전투 카메라 조작([설계](../specs/2026-10-26-combat-camera-control-design.md), [계획](../plans/2026-10-27-combat-camera-control-plan.md) §6) — 새 2단 `Input/Cameras/`(카메라 입력), `Presentation/Cameras/`에 시점·제한 규칙, `Input/Contracts/`에 시점 계약(§0·§1.1). 폴더 이름은 `Camera`가 아니라 `Cameras`(§4 이름 규칙)
- 갱신: 2026-10-27 전투 연출 범용 구조([설계](../specs/2026-10-27-combat-presentation-generic-structure-design.md) G1~G8, [계획](../plans/2026-10-27-combat-presentation-generic-structure-plan.md) §13) — 새 파일 여섯의 자리(`Display/Unit/`에 외형별 연출 값 보관, `Presentation/Pieces/`에 코드 모션 조각·조립과 큐 붙이기 셋), 연출 덧붙임은 큐 데이터라는 규칙(§2). 같은 날 투사체·피격 소리([설계](../specs/2026-10-27-combat-projectile-hit-sound-design.md)) — 새 파일 `Playback/Support/CombatProjectileSounds`(공격 값에서 꺼낸 투사체 소리 값), §2 연출 덧붙임 줄에 소리 주인 규칙
- 갱신: 2026-10-28 전투 걸음 모양([설계](../specs/2026-10-28-combat-step-motion-style-design.md) ST1~ST7, [계획](../plans/2026-10-28-combat-step-motion-style-plan.md)) — `Presentation/Movement/`에 이동 종류·걸음 모양 전략·표 다섯, `Playback/Support/`에 걸음 모양 값 `CombatStepLookSettings`, 이동 종류마다 다른 걸음 모양의 규칙(§1·§1.1·§2)
- 갱신: 2026-10-28 전투 이펙트 창구 확장([설계](../specs/2026-10-28-combat-effect-cue-expansion-design.md) EC1~EC10, [계획](../plans/2026-10-28-combat-effect-cue-expansion-plan.md) §6) — `Playback/Ordering/`에 사건 순간 값 `CombatEventMoment`·지형 변화 갈래 `CombatTerrainChanges`, `Playback/Support/`에 순간 → 큐 값 `CombatCueSources`·순간 큐 재생 `CombatMomentCuePlayback`, 환경 폭발 창구 `CombatEnvironmentEffectPresenter`·`CombatExplosionRules` 삭제, 사실 사건의 순간은 형식 표 칸이라는 규칙(§1·§1.1·§2)
- 갱신: 2026-10-28 전투 맵 바닥 타일([맞춤 설계](../specs/2026-10-28-combat-map-tile-alignment-design.md) TA1~TA5·[높이 설계](../specs/2026-10-28-combat-map-tile-height-design.md) TH1~TH6, [높이 계획](../plans/2026-10-28-combat-map-tile-height-plan.md) §8) — `Display/Map/`에 타일 세우기 `StageMapTiles`·맞춤 `CombatMapTileAlignment`·칸별 타일 높이 표 `CombatMapTileSurface`, 칸 위에 놓는 높이는 Mapper 함수 중 하나를 고른다는 규칙(§2)
- 갱신: 2026-10-29 지형 외형 타일 위 / 바닥 타일 교체([설계](../specs/2026-10-29-combat-terrain-tile-replace-design.md) TR1~TR8, [계획](../plans/2026-10-29-combat-terrain-tile-replace-plan.md) §6) — `Display/Map/`에 타일 끄고 켜기 `FieldTileReplacement`, 새 2단 `Presentation/Environment/` 줄(토네이도 외형 연출은 원래 있었고 표에 빠져 있었음, 바닥 교체 무너짐 `CombatTileCollapse`·값), `Players/`의 필드 외형 재생기 이름 바꿈, 칸 위 높이 규칙에 바탕 바꾸기(§0·§1.1·§2)
- 갱신: 2026-10-29 특성 발동 연출([설계](../specs/2026-10-29-combat-trait-activation-presentation-design.md) TA1~TA9, [계획](../plans/2026-10-29-combat-trait-activation-presentation-plan.md) §6) — `Presentation/Attack/`에 공격 모양 값 `CombatAttackShape`, `Playback/Handlers/`에 특성 처리기 `CombatTraitEventHandler`, `Playback/Support/`에 공격 시작 한 곳 `CombatAttackStart`, `Display/Unit/`에 걸린 상태 줄 표 `CombatUnitStatuses`, 특성 발동 연출·걸린 상태 HUD의 규칙(§1.1·§2)
- 갱신: 2026-10-30 특성 시스템 View 대응([설계](../specs/2026-10-29-combat-trait-system-view-design.md) TS1~TS9, [유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) AA1~AA9, [계획](../plans/2026-10-30-combat-trait-system-view-plan.md) §7) — `Playback/Ordering/`에 공격 출처 값 `CombatAttackSource`·후속 정리 질문 `ICombatSourceSettlement`, `Display/Map/`에 흔적 표시 `FieldTrailPresenter`, `Display/Unit/`에 서는 자리 대여 계약 `ICombatUnitStand`, `Presentation/Pieces/`에 공격 조립 앞 항목 `CombatAttackLead`·유닛 행동 넷, 추가 공격 판정(`ChildAttack`)·이동 종류 판정(걸음 원인)·특성 흔적·특성 발동 유닛 행동의 규칙(§1.1·§2)
- 갱신: 2026-11-03 전투 GC 끊김 View 쪽([설계](../specs/2026-11-03-combat-view-gc-hitch-design.md) GH1~GH6, [계획](../plans/2026-11-03-combat-view-gc-hitch-plan.md) §3) — `Presentation/Pieces/`에 할당 없는 열거 값 검사 `CombatEnumValues`, 자주 도는 길의 작은 할당·강조 그리기 때·처음 생성의 규칙(§2)
- 갱신: 2026-11-01 토네이도 전투 View([설계](../specs/2026-11-01-combat-tornado-view-design-Hojin.md) TN1~TN9, [계획](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md)) — `Display/Map/`에 움직이는 토네이도 표시 `FieldTornadoPresenter`·좁은 계약 `ICombatTornadoField`, `Playback/Handlers/`에 `CombatTornadoEventHandler`, `Presentation/Pieces/`에 몸 이동 `CombatTornadoMovePiece`. 지형 표시(`FieldTerrainPresenter`)는 토네이도를 더 그리지 않는다.
- 갱신: 2026-11-02 하붑 전투 View([설계](../specs/2026-11-01-combat-haboob-view-design-Hojin.md), [계획](../plans/2026-11-01-combat-haboob-view-plan-Hojin.md)) — `Display/Map/`에 구름 칸 표시 `FieldHaboobPresenter`·좁은 계약 `ICombatHaboobField`, `Playback/Handlers/`에 `CombatHaboobEventHandler`, `Presentation/Pieces/`에 구름 이동 `CombatHaboobMovePiece`(설계 §9.4).
- 갱신: 2026-11-02 낙석 전투 View([설계](../specs/2026-11-01-combat-rockfall-view-design-Hojin.md) RF1~RF8, [계획](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md)) — `Display/Map/`에 경고 칸·돌 풀 표시 `FieldRockfallPresenter`·좁은 계약 `ICombatRockfallField`, `Display/Field/`에 칸 위 표지 `FieldCellMarkers`, `Playback/Handlers/`에 `CombatRockfallEventHandler`, `Presentation/Pieces/`에 돌 낙하 `CombatRockfallDropPiece`·값 `CombatRockfallDrop`·출발 규칙 `CombatRockfallDrops`(능선, 설계 §10).
- 갱신: 2026-11-01 이펙트 구조 2판([설계](../specs/2026-10-31-combat-effect-lifetime-design.md), [계획](../plans/2026-10-31-combat-effect-structure-plan.md)) — `Presentation/Effects/`에 유지 이펙트 부품·출처 셋·행동 끝 묶음, `Input/Contracts/`에 공격 모드 대상 표시 의도, §2 규칙 줄 "오래 보이는 이펙트의 새 조건"
- 작성: View 작업(스테이지 내부 담당)
- 대상: `ViewSystem/Combat/` 아래에 `.cs`를 새로 만들거나 옮기는 작업자
- 근거: [폴더 정리 계획](../plans/2026-10-19-combat-folder-restructure-plan.md) M1~M7(정한 것)·§3(2026-10-21 이동 대응표)·§9.1(실행 기록), [전투 View 구조 안내](https://claude.ai/artifact/CCPMThCg8ciN3z1VhhzAfz)(파트 01~05)
- **새 파일 위치의 원본은 이 안내 하나다.** 계획 §3 표는 2026-10-21 이동 때의 기록이라, 그 뒤 새 파일은 이 안내를 따르고 표에 더하지 않는다.

## 0. 한 줄 요약

**1단 폴더 = 맡은 단계(입력·표시·결과 재생·연출 부품), 2단 폴더 = 그 단계 안의 역할.** 네임스페이스는 폴더 경로와 같다. 루트에는 새 파일을 두지 않는다(예외: 조정자의 씬·연결 수명을 맡는 객체, 3라운드 Q-V22 A — §2).

`NS` = `ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat`

```text
Combat/                    NS                          조정자·씬 연결·결과 수명·불러온 씬·연결 객체
├─ Input/                  NS.Input                    입력 입구: 수신·분배·허용 판단·Gate·연결 입력 묶음
│  ├─ Interaction/                                     배치·공격·아이템 끌기·호버·공통 수명 계약·넘겨받기·입력 소리
│  ├─ MovePath/                                        이동 경로·복귀
│  ├─ UiEntry/                                         다른 UI(덱·가방·Option)가 부르는 입구
│  ├─ Assist/                                          커서 스냅·환경 설명·거절 안내·튜토리얼 막힘 보고
│  ├─ ActionButtons/                                   행동 버튼·시작 확인·배치 진입·입력 표면
│  ├─ Cameras/                                         카메라 조작(이동·줌) 입력
│  └─ Contracts/                                       입력 → 표시 계약
├─ Display/                NS.Display                  표시 입구(씬 표시 하나)·표시 값 읽기
│  ├─ Feedback/                                        입력 중 표시·궤적 구성
│  ├─ Field/                                           격자·칸·강조·높이 표·궤적 그리기
│  ├─ Unit/                                            유닛·유닛 HUD·표시 창구·클립 채널
│  ├─ Map/                                             지형·오브젝트·레일·열차 칸·가시밭·프랍·바닥 타일
│  └─ Hud/                                             화면 UI·종료 화면 칸
├─ Playback/               NS.Playback                 사건 Presenter·그 필드 창구·재생 조정·단계 전환
│  ├─ Ordering/                                        순차 재생·동시 묶음·사건 형식 표·사건 순간
│  ├─ Handlers/                                        사건 형식별 처리기
│  ├─ Players/                                         시작 조건·실행 목록·재생기
│  └─ Support/                                         대기·소리·소리 프리셋·순간 큐·강조·보인 사실·연출 값
└─ Presentation/           (파일 없음)
   ├─ Pieces/                                          조각·조립·클립 재생
   ├─ Attack/                                          공격 재생·공격 종류 전략·투사체·발사 기하·Gate
   ├─ Movement/                                        걸음 레인·이동 종류·걸음 모양·접촉·몸·높이 식
   ├─ Reaction/                                        피격 반응·맞은 방향
   ├─ Railway/                                         기차 재생·기차 카메라
   ├─ Cameras/                                         카메라 창구·플레이어 카메라 시점
   ├─ Effects/                                         이펙트 창구
   └─ Environment/                                     지형 외형 연출(토네이도·바닥 교체 무너짐)
```

하위 폴더의 네임스페이스는 `NS.<1단>.<2단>`이다(예: `Display/Unit/` → `NS.Display.Unit`).

## 1. 판단 순서

새 파일 하나에 대해 위에서부터 묻고, 처음 "예"가 나온 곳에 둔다.

| 순서 | 질문 | 두는 곳 |
|---|---|---|
| 1 | 실제로 움직이는 연출 부품인가(조각, 공격·걸음·반응·기차 재생, 카메라·이펙트로 내보내는 창구)? | `Presentation/<종류>/` |
| 2 | 결과 안의 사건을 **언제·어떤 순서로** 재생할지 정하거나, 사건 형식을 연출로 바꾸는가? | `Playback/<역할>/` |
| 3 | Buffer의 지금 사실을 화면에 그리는가(씬 Presenter·MonoBehaviour·필드 그리기·화면 UI)? | `Display/<대상>/` |
| 4 | 사용자 입력을 받거나 해석하거나, `CombatViewBuffer`에 명령·미리보기 요청을 쓰는가? | `Input/<기능>/` |
| 5 | 위 어디에도 아니고 조정자(`CombatViewSystem`)의 수명 자체인가? | 루트 — **먼저 묻는다**(루트 14개는 바깥에서 가장 많이 쓰는 형식이라 늘리지 않는다. 3라운드에 조정자의 씬·연결 수명 객체 둘을 사용자 결정 Q-V22 A로 더함) |

1을 2보다 먼저 묻는 까닭: 재생기·처리기도 "언제"를 다루지만, 화면에서 무언가를 실제로 움직이는 코드는 부품 쪽이다. 처리기는 부품을 조립해 넘길 뿐이다.

### 1.1 2단 폴더 고르기

| 1단 | 2단 | 두는 것 | 지금 있는 예 |
|---|---|---|---|
| 루트 | — | 조정자와 그 계약, 조정자의 씬 수명·연결 수명을 맡는 상태 객체(§2) | `CombatViewSystem`, `ICombatViewService`, `CombatLoadedScene`, `CombatViewConnection` |
| `Input/` | (바로 아래) | 입력 공통 입구: 수신·분배·정책·대기 Gate·판단용 사실, 골격이 읽는 View 쪽 진행 사실(제출 정지·완료 표시·포기 요청), 조정자가 연결 수명 동안 갖는 입력 객체 묶음과 그 생성 계약 | `CombatInputReceiver`, `CombatInputRouter`, `CombatInputPolicy`, `CombatCommandGate`, `CombatViewProgress`, `CombatSceneInput`, `ICombatSceneInputFactory`, 입력 연결 `CombatInputConnection`, 입력 생성 재료 `CombatSceneInputSources` |
| | `Interaction/` | 포인터로 하는 입력 기능 하나와 그 호버, 입력 기능 묶음과 공통 수명 계약, 공격 Drag 넘겨받기 | `PlacementDragInteraction`, `AttackTargetingInteraction`, `CombatItemDragInteraction`, `CombatUnitRangeHover`, `CombatInteraction`, `CombatInputOwner`, `CombatPointerInput`, `CombatAttackDragHandover`, `ICombatAttackDragTarget`, 입력 소리 창구 `CombatInputSounds` |
| | `MovePath/` | 이동 경로 초안·견적·범위·복귀 요청 | `CombatPathInput`, `CombatMovementInteraction`, `CombatReturnRequest` |
| | `UiEntry/` | 다른 작업자 UI가 부르는 공개 입구와 그 구현 | `ICombatDeckPlacementInput`·`CombatDeckPlacementInput`, `CombatItemInput`, `CombatAbandonController`(Option의 포기 요청) |
| | `Assist/` | 입력을 돕는 보조(커서 옮기기, 설명 문구, 거절 안내, 튜토리얼 막힘 보고) | `CombatCursorSnap`, `CombatEnvironmentHover`, `CombatActionNotice`, `CombatBlockedInputReport` |
| | `ActionButtons/` | 버튼·확인창으로 하는 입력과 그 표시 계약, 입력 표면(BattleChanger·가방·행동 버튼)의 보임 맞춤 | `CombatActionButtons`, `CombatDeploymentConfirmation`, 배치 진입 `CombatDeploymentEntry`, 턴 종료 `CombatEndTurnEntry`·`CombatEndTurnConfirmation`, `CombatInputSurfaces` |
| | `Cameras/` | 카메라 조작(이동·줌) 키·휠·가장자리 입력. 움직일 양·제한은 모르고 시점 계약에 넘긴다 | `CombatCameraInput` |
| | `Contracts/` | 입력 기능이 표시에 묻고 요청하는 계약(§2) | `ICombatInteractionFeedback`, `ICombatPointerPicker`, `CombatHoverKind`, 카메라 시점 `ICombatCameraControl`, 넉백 미리보기 표시 의도 `ICombatKnockbackPreviewFeedback`, 공격 모드 대상 표시 의도 `ICombatAttackModeTargets` |
| `Display/` | (바로 아래) | 표시 입구 — 조정자가 값만 넘기는 씬 표시 하나와 그 계약·생성 계약, 표시 값 읽기 | `CombatSceneDisplay`, `ICombatBattleDisplay`, `ICombatPackDisplay`, `ICombatSceneFactory`, `CombatDisplayValueReader` |
| | `Feedback/` | 입력 중에만 보이는 표시(고스트·조준·경로 계획·궤적 구성·넉백 미리보기 구성) | `CombatAttackFeedback`, `PlacementGhost`, `CombatAttackTrajectory`, `CombatKnockbackPreview` |
| | `Field/` | 격자·칸·강조 층·칸 높이·필드 위 선·표지 그리기와 그 캔버스 계약 | `FieldView`, `FieldGridMapper`, `FieldHighlightLayers`, `FieldTrajectoryLine`, 이동 경로 띠·화살촉 `FieldMovementPathLine`(그리기)·`FieldMovementPathShape`(모양 규칙), 넉백 화살표 `FieldKnockbackMarkers`·`ICombatKnockbackCanvas`, 정렬 순서 표 `FieldSortingOrders`, 코드가 맞추는 렌더 레이어 표 `FieldRenderLayers`, 칸 채우기 겹침 깊이 `FieldCellFillStack`(겹친 칸은 같은 층 덩어리를 안쪽으로 줄인 판, 모양은 셰이더가 칸 지도로 깎음), 판·테두리 고리 모양 식 `FieldCellShape`(덩어리/칸마다 방식·깊이별 안쪽 거리·반지름, 채우기와 테두리가 함께 씀), 그린 칸·색 기억 `FieldDrawnCells`(테두리·채우기가 함께 씀) |
| | `Unit/` | 유닛 표시·HUD·표시 대여 창구와 그 계약, 외형별 연출 값 보관, HUD 걸린 상태 칸 | `FieldUnitView`, `FieldUnitPresenter`, `CombatUnitPresentationWindow`, `ICombatUnitPose`, 서는 자리 대여 `ICombatUnitStand`(자세 아래 층, 유닛 행동이 옮긴 자리), `CombatUnitPresentationValues`, 걸린 상태 줄 표 `CombatUnitStatuses` |
| | `Map/` | 유닛 밖의 필드 물체(지형·오브젝트·움직이는 토네이도·낙석 경고 칸·떨어지는 돌·하붑 구름 칸의 연기·레일·열차 칸·가시밭·특성 흔적·배경 프랍·바닥 타일과 그 맞춤·칸별 타일 표·바닥 교체 칸의 타일 끄고 켜기) | `FieldTerrainPresenter`, `FieldRailwayPresenter`, `RailwayTrainCars`, 특성 흔적 `FieldTrailPresenter`, 토네이도 `FieldTornadoPresenter`·`ICombatTornadoField`, 낙석 `FieldRockfallPresenter`·`ICombatRockfallField`, 하붑 `FieldHaboobPresenter`·`ICombatHaboobField`, `StageViewPropPresenter`, `StageMapTiles`, `CombatMapTileAlignment`, `CombatMapTileSurface`, `FieldTileReplacement` |
| | `Hud/` | 화면 공간 UI(정보창·행동 패널·종료 화면 칸, 쓰지 않는 옛 손패 `HandPresenter`는 씬 컴포넌트를 뗀 뒤 삭제 예정) | `CombatHud`, `CombatActionPanel`, `CombatUnitInfo`, `CombatRewardSlot` |
| `Playback/` | (바로 아래) | 사건 Presenter와 그 입력 계약(필드 창구 묶음)·재생 조정·단계 전환 | `CombatSceneEventPresenter`, `CombatFieldEventPorts`, `CombatSceneEventPlayback` |
| | `Ordering/` | 사건을 어떤 순서·묶음으로 시작하나, 사건 형식마다의 지식 표(경로·행동 시작·주체·근원·간격·화면 전환·사실 사건의 순간)와 그 값 | `OrderedCombatPresentation`, `CombatConcurrentGroups`, `CombatEventTypeTable`, 순간 값 `CombatEventMoment`, 지형 변화 갈래 `CombatTerrainChanges`, 공격 출처 값 `CombatAttackSource`(공격자·공격 그룹·관통 기록), 후속 정리 질문 `ICombatSourceSettlement`(순차 재생기가 구현) |
| | `Handlers/` | 사건 형식 하나를 연출로 바꾸는 처리기 | `CombatAttackEventHandler`, `CombatRoarEventHandler`, 특성 발동 `CombatTraitEventHandler`, 움직이는 토네이도 `CombatTornadoEventHandler`, 낙석 `CombatRockfallEventHandler`, 하붑 `CombatHaboobEventHandler` |
| | `Players/` | 시작 조건·실행 목록·재생기와 그 계약 | `CombatStartQuery`, `CombatPresentationPlayers`, `CombatPieceRunPlayer`, 필드 외형 재생기 `CombatFieldAppearancePlayer`(옛 `CombatTornadoAppearancePlayer`) |
| | `Support/` | 사건 공통 도우미, 연출 설정 값 묶음 | `CombatEventWait`, `CombatEventSounds`, 소리 프리셋 `CombatSoundPresets`, 씬 표시의 배경음 정책 `CombatBgmPolicy`(국면·승리·완료 뒤), 순간 → 큐 값 `CombatCueSources`, 순간 큐 재생 `CombatMomentCuePlayback`, `CombatShownFacts`, 위치 교환 승객의 미뤄 둔 배치 보관 `CombatDeferredPlacements`(보인 사실이 반영·그리기), `CombatCameraMotionSettings`, 사건 Presenter 연출 값 `CombatEventPresentationValues`, 공격 모양 하나를 코드 모션·클립 조립으로 시작하는 한 곳 `CombatAttackStart`(공격·특성 처리기가 함께 씀), 특성 발동 연출 줄 보관 `CombatActivationRows`(특성 처리기·대신 맞기가 함께 씀), 대신 맞기 앞 항목 만드는 곳 `CombatGuardLead` |
| `Presentation/` | `Pieces/` | 조각 계약·조각·조립·조각 실행, 클립 재생 단위, 큐 붙이기(기준 시점 표·자리 해석·큐 → 항목), 공격 조립 앞 항목, 특성 발동 유닛 행동 조립, 할당 없는 열거 값 검사 | `ICombatPresentationPiece`, `CombatPieceRun`, `CombatEnumValues`, `CombatClipAttack`, `CombatCodeMotionAttack`, `CombatCueComposer`, 앞 항목 `CombatAttackLead`, 토네이도 몸 이동 `CombatTornadoMovePiece`, 돌 낙하 `CombatRockfallDropPiece`·값 `CombatRockfallDrop`·출발 규칙 `CombatRockfallDrops`, 유닛 행동 조립 `CombatUnitActionComposer`·자리 계산 `CombatUnitActionPlaces`·서는 자리 이동 `CombatStandMovePiece`·돌아오기 앞 대기 `CombatUnitSettleWaitPiece` |
| | `Attack/` | 공격 재생·공격 종류 전략·투사체·발사 기하·히트 Gate, 그 입력인 공격 모양 값 | `CombatAttackPlayback`, 공격 모양 `CombatAttackShape`, 전략 `CombatAttackStyle`·`CombatAttackStyles`·`ICombatAttackStyleContext`·종류 셋, `CombatProjectileFlights`, `CombatShotPath`, `CombatHitGate` |
| | `Movement/` | 걸음 레인·이동 종류·걸음 모양·걸음 접촉·몸 모양·걸음 높이 | `CombatUnitMotionSet`, `CombatMovementKind`·성질 `CombatMovementKinds`, 모양 `CombatStepStyle`·`CombatStepStyles`·걷기·뛰기, `CombatStepContacts`, `CombatBody`, `CombatStepHeight` |
| | `Reaction/` | 피격 반응과 그 방향 | `CombatUnitReactionPresenter`, `CombatHitDirection` |
| | `Railway/` | 기차 전용 재생(접촉 예약·경로·기차 카메라) | `CombatRailwayContactPlayback`, `CombatTrainCamera` |
| | `Cameras/` | 맵 카메라로 내보내는 창구, 맵 카메라 위치를 쓰는 플레이어 시점과 그 제한 규칙 | `CombatCameraWindow`, `CombatMapCameras`, `CombatCameraView`, `CombatCameraLimits` |
| | `Effects/` | 이펙트로 내보내는 창구와 이펙트 수명 | `CombatEffectWindow`(입구 `Play` 하나·놓기 값 `CombatEffectPlacement`·손잡이 `CombatEffectHandle`), 몸 위치 따라가기 받침 `CombatEffectFollower`(MonoBehaviour, 창구만 만들고 빌려 줌), 오래 보이는 이펙트 맞춤 `CombatPersistentEffects` + 출처 계약 `ICombatPersistentEffectSource` + 출처 셋(상태 `CombatStatusEffectSource`·보유 동안 `CombatOwnedTraitEffectSource`·공격 모드 대상 `CombatAttackModeEffectSource`), 행동 끝 이펙트 묶음 `CombatEffectActionGroup` |
| | `Environment/` | 지형 외형 하나의 시간 연출(생성·축소·재배치·무너짐)과 그 값. 외형을 끄거나 풀에 돌려주는 것은 지형 표시(`Display/Map/`)가 맡는다 | 토네이도 `CombatTornadoAppearance`·`CombatTornadoLiftTarget`, 바닥 교체 무너짐 `CombatTileCollapse`·여럿 진행 `CombatTileCollapseSet`·값 `CombatTileCollapseSettings` |

## 2. 헷갈리는 경우

| 경우 | 규칙 | 예 |
|---|---|---|
| 오래 보이는 이펙트의 새 조건(2026-11-01) | 조건 열거 값 하나(`CombatTraitAlwaysCondition` 또는 상태 외형 칸) + `Presentation/Effects/`의 출처 클래스 하나. 내기·거두기는 `CombatPersistentEffects`가 하고, 맞추는 곳은 `CombatShownFacts.Render` 하나다(이펙트 끝 규칙 설계 EL4) | 보유 동안 = `CombatOwnedTraitEffectSource` |
| 좁은 계약(인터페이스) | 그 계약이 다루는 기능의 폴더에 둔다. **예외:** 입력 기능 여럿이 표시에 요청하는 계약은 `Input/Contracts/`에 모아, 입력이 무엇을 요청하는지 한 곳에서 보게 한다 | 유닛 자세 계약 `ICombatUnitPose` → `Display/Unit/`, 행동 버튼 표시 `ICombatActionDisplay` → `Input/ActionButtons/`, 궤적 캔버스 `ICombatTrajectoryCanvas` → `Display/Field/` |
| 실제 연출과 미리보기가 같이 쓰는 순수 식 | 실제 연출 쪽에 둔다. 미리보기는 그것을 부른다 | `CombatShotPath`(발사 기하) → `Presentation/Attack/`, `CombatStepHeight` → `Presentation/Movement/` |
| 설정 표에서 꺼낸 연출 값 묶음 | `Playback/Support/` | `CombatAbilityMotionSettings`, `CombatCameraMotionSettings`, `CombatEventPresentationValues`, `CombatProjectileSounds`(공격 값의 투사체 소리), `CombatStepLookSettings`(걸음 모양·뛰기 값) |
| 조정자의 수명 하나(씬·연결·서비스)를 맡는 상태 객체 | 씬·연결 수명은 루트, 서비스 수명 입력 객체는 `Input/` 바로 아래, 연결 수명이지만 기능 하나인 객체는 그 기능 폴더(3라운드 W11, Q-V22 A) | 씬 `CombatLoadedScene`·연결 `CombatViewConnection` → 루트, `CombatInputConnection`·`CombatSceneInputSources` → `Input/`, 배치 진입 `CombatDeploymentEntry` → `Input/ActionButtons/` |
| 공격 종류마다 다른 지식(목표점·궤적·코드 모션·발사) | 새 분기 파일을 만들지 않는다. 종류마다 `CombatAttackStyle` 파생 클래스 하나를 `Presentation/Attack/`에 두고, 표 `CombatAttackStyles`에 한 줄 더한다(빠뜨리면 표가 만들 때 실패). 전략이 공격 재생에 바라는 것은 좁은 맥락 `ICombatAttackStyleContext`로만 받는다(3라운드 W8) | `CombatMeleeStyle`·`CombatLinearStyle`·`CombatMortarStyle`(옛 `CombatBallistaStyle`은 2026-10-27에 걷음) |
| 이동 종류마다 다른 걸음 모양(시간·높이·접촉 시점) | 레인·배치 대여에 분기를 넣지 않는다. 모양마다 `CombatStepStyle` 파생 클래스 하나를 `Presentation/Movement/`에 두고, 이동 종류 `CombatMovementKind`에 값 하나·성질 `CombatMovementKinds`에 한 줄(회전·접촉 Gate·강조·방향 복귀를 하는 "스스로 하는 이동"인지)·표 `CombatStepStyles`에 한 줄 더한다(빠뜨리면 표가 만들 때 실패). 레인·이동 표시에 `== 종류` 비교를 두지 않는다. 종류 판정은 `CombatMovementKinds.Of(걸음 사건)` 한 곳이고 Core 걸음 사실의 이동 원인(`MovementCause`)으로 정한다(밀림 = 원인 Forced, 비거나 Voluntary면 스스로 — 걸음 표시·적 행동 간격이 함께 부름, 옛 추론 `IsVoluntaryMovement`는 걷음, 특성 시스템 View 설계 TS5). 높이 식은 `CombatStepHeight`에 둔다([걸음 모양 설계](../specs/2026-10-28-combat-step-motion-style-design.md) ST2·ST6) | `CombatWalkStepStyle`(걷기·밀림)·`CombatHopStepStyle`(칸마다 뛰기) |
| 강조 층의 순서·색·채움·평소 그림·원점 열기 | 새 목록·상수를 만들지 않는다. 층 표 `FieldHighlightLayers.Rows`에 한 줄 더한다. 원점 열기 열은 기본값이 없어 모든 줄에 적는다(2026-11-04 평가 C1-21). 그리는 순서와 마스크(행동 중·평소·평소 채움·원점 열기·호버 범위)는 표에서 만들고, 열거 누락은 표가 만들 때 검사한다(3라운드 W10) | 행동 중·평소 마스크(지움, 표에서 파생), 열린 변 층 목록(지움, 원점 열기 열에서 파생) |
| 환경 원본(지형·오브젝트·흔적)을 보일지 | 새 조건식을 만들지 않는다. `CombatEnvironmentFacts.IsShown`(켜짐·플레이어 공개)을 부른다(2026-11-04 평가 C1-05) | 흔적·오브젝트·지형 표시, 환경 설명, 위험 범위, 먼지 연막 호버 |
| 소리 프리셋 번호(유닛·공격·배경음) | 코드 switch를 만들지 않는다. 유닛·공격은 유닛 외형 표 칸 둘, 적 공용·배경음은 전투 연출 설정 칸이 원본이고, 값 객체 `CombatSoundPresets`가 답한다(3라운드 W9-2) | 옛 `CombatEventAudio.UnitPreset`·`AttackPreset`(지움) |
| 칸 위에 무엇을 놓을 때의 높이(바닥 타일·고지대) | 새 높이 식을 만들지 않는다. `FieldGridMapper` 함수 넷 중 하나를 고른다 — 바닥면 `CellCenterTo…`(레일·기차), 바탕(타일 윗면, 바닥 교체 칸은 그 지형 높이) `CellBaseTo…`(타일 위 지형 외형), 표면(바탕 + 지형) `CellSurfaceTo…`(유닛·오브젝트·가시밭·이펙트 큐), 격자 높이(지형만, 바닥 타일·바닥 교체 높이 무시) `CellOverlayToWorld`(GridOutline 테두리·채우기·경로선·호버·포인터·커서 스냅). 포인터를 따라가는 고스트는 포인터 점을 `OnSurface`로 그 칸 표면에 맞춘다. 높이 원본은 표 `FieldSurfaceHeights` 하나이고, 지형 외형의 표면 높이는 방식대로 바탕을 바꾸거나(`Replace`, 바닥 교체) 올린다(`Raise`, 타일 위)(타일 교체 설계 TR5) | 오브젝트 → `CellSurfaceToLocal`, 호버 채우기 → `CellOverlayToWorld` |
| 창구를 쓰는 특정 연출 | 창구 폴더가 아니라 그 연출 폴더 | `CombatTrainCamera`(카메라 창구를 씀) → `Presentation/Railway/` |
| 표시와 입력이 같이 쓰는 값 형식 | 그 값을 정하는 쪽 | 호버 상태 `CombatHoverState`는 입력이 정하고 표시가 색만 고름 → `Input/Interaction/` |
| 파일 하나에 형식이 여럿 | 파일 이름의 형식 기준으로 한 폴더. 다른 형식도 같은 네임스페이스 | `CombatActionNotice.cs` 안의 `ICombatActionRejectionSink` → `Input/Assist/`, `CombatViewProgress.cs` 안의 `ICombatProgressHost` → `Input/` |
| 조정자가 소유하고 골격(LifeCycle)이 읽는 View 쪽 상태 | 루트가 아니다(루트는 바깥에서 많이 쓰는 형식 자리). 입력을 막고 여는 사실이면 `Input/` 바로 아래 | 진행 사실 `CombatViewProgress`(제출 정지·완료 표시·포기 요청, C 단계 Q-C2 A) |
| 코드를 설명하는 문서(`.md`) | 설명하는 코드 옆(`.meta` 함께) | `CombatUnitInfo.md` → `Display/Hud/` |
| 전투 View 전용 셰이더·재질 | 코드 폴더가 아니라 `Assets/ProjectL/Prefabs/Combat/Shaders/`·`Prefabs/Combat/Materials/`. 셰이더 값 이름은 그 재질을 쓰는 그리기 부품 한 곳에 상수로 두고 구성 때 검사한다(`Script/Render/`의 공용 셰이더는 다른 작업자 영역) | 칸 채우기 `CombatCellFill.shader`·`CellFill.mat` → 값 이름은 `FieldCellFillStack` |
| 조정자가 받는 생성 계약(Factory를 정적으로 부르지 않게) | 계약은 만드는 것의 폴더에 둔다. 구현은 전투 폴더 밖 Factory 폴더다 | 배경·표시 `ICombatSceneFactory` → `Display/`, 연결 입력 묶음 `ICombatSceneInputFactory` → `Input/`, 구현 `CombatSceneFactory` → `Systems/Factory/Runtime/InGame/View/` |
| 입력 기능이 공통 수명(`CombatInputOwner`)에 참여하는 어댑터 | 새 파일을 만들지 않는다. 기능 파일 끝의 `private sealed class OwnerAdapter`, 창구는 `internal CombatInputOwner InputOwner` | `PlacementDragInteraction.cs`·`CombatPathInput.cs`의 `OwnerAdapter` |
| 사건 형식마다 다른 지식(경로·주체·근원·간격·화면 전환 등) | 새 분류기 파일을 만들지 않는다. 사건 형식 표 `CombatEventTypeTable`의 그 형식 줄에 칸이나 질문을 더한다. 사실 사건이 "어떤 순간인가"(장애물 피해·파괴, 오브젝트 발동·생성, 추락, 약한 지반 붕괴, 아이템 발동)는 형식 표의 순간 칸 `moment`가 답하고(값 `CombatEventMoment`, 질문 `TryGetMoment`), 소리 표와 순간 큐가 같이 읽는다. 새 사실 사건 연출을 더할 때 고칠 곳은 여섯이다(이펙트 창구 확장 설계 검토 F2 = A, 묶음 끝 검토 L3): ① 순간 종류 `CombatMomentKind` 값, ② 형식 표 그 줄의 `moment`, ③ 큐 칸 `CombatCueEvent` 값과 큐 규칙 `AllowsAnchor`·`AllowsPoint` 갈래, ④ 큐 값 자리(외형 SO나 설정 표의 칸과 검사), ⑤ 큐 값 답 `CombatCueSources.For` 갈래, ⑥ 소리를 낸다면 소리 표 `CombatEventSounds.Moment` 갈래. ⑤·⑥의 switch는 시작 때 검사하지 않아, 빠뜨리면 그 사건이 처음 날 때 예외다. 아직 표 밖에 남은 것은 행동 사건 소리(`CombatEventSounds`의 형식 switch, 번호는 `CombatSoundPresets`)다 | 옛 `CombatActionBoundary`(지움)·`CombatEventRoutes`(표로 이름 바꿈)·`CombatExplosionRules`(지움, 폭발은 순간 큐) |
| 연출 덧붙임(이펙트·카메라 흔들림·소리 시점·플래시) | 처리기·조립에 "값이 있으면 항목" 갈래를 만들지 않는다. 처리기는 뼈대(반응·클립·코드 모션·발사)와 기준 시점 표(`CombatCueAnchors`)만 만들고 큐 붙이기 `CombatCueComposer`를 한 줄 부른다. 값은 공격·반응 묶음 SO의 칸 큐(DB `CombatEventCues`)가 원본이고, 사건마다 고를 수 있는 시점·자리는 큐 규칙 `CombatPresentationCueRules` 하나가 정한다. 자리 해석과 위치·회전 보정 적용(`CombatCuePoints.Apply`)은 `CombatCuePoints` 한 곳이다(범용 연출 구조 설계 G2·G3·G6, 이펙트 창구 확장 설계 EC1). 사실 사건(순간)의 큐는 외형 SO·설정 표·토네이도 설정의 큐 칸이 원본이고, `CombatCueSources`가 순간마다 답하며, `CombatMomentCuePlayback`이 막지 않는 곁들임 실행(조립 범위 `Accent`)으로 낸다. 붙은 항목이 없으면 실행을 시작하지 않는다(`CombatComposition.IsEmpty`). 소리는 "무엇(프리셋)"의 주인과 "언제"를 나눈다: 공격·사망은 프리셋이 유닛 외형, 시점이 칸 큐 `sound`이고, 투사체 착탄·비행은 프리셋·지연이 투사체 부품이고 시점이 Hit·Cue로 고정이며, 피격은 반응 묶음 소리 목록이 프리셋·시점·지연을 함께 든다. 효과음 슬롯 대응은 소리 표 `CombatEventSounds` 한 곳이다(투사체·피격 소리 설계 PS1~PS4) | 피격·사망 흔들림(옛 설정 표 → 반응 묶음 큐), 착탄 이펙트(옛 공격 SO `impactEffect` → 공격 큐 Contact), 착탄·비행·피격 소리(새 슬롯은 소리 표의 연결 대기 칸), 화약통·지뢰 폭발(옛 환경 폭발 창구·설정 칸 → 외형 순간 큐) |
| 특성 발동 연출(발동 순간 이펙트·흔들림, 추가 공격 다시 치기) | 특성마다 처리기·분기·사건 갈래를 만들지 않는다. 값은 View DB 특성 외형 표의 그 특성 줄에 있는 발동 연출 줄 `TraitActivationVisual`(열쇠 = 발동 종류, 형태 셋: 큐 / 추가 공격이면 공격 표시 덮어쓰기 / 유닛 행동 `unitAction`)이고, 사건(`CombatEffectsEvent`)은 처리기 `CombatTraitEventHandler` 하나가 받고, 줄 값을 한 번 읽어 보관한다. 큐는 처리기가 자리를 만들어 순간 큐와 같은 곁들임 실행 입구(`CombatMomentCuePlayback.PlayAccent`)로 낸다. **추가 공격인지는 발동 종류 이름이 아니라 Core 사실의 추가 공격 결과(`ChildAttack`)가 있는지로 정한다** — 판정 원본은 형식 표 `CombatEventTypeTable.IsChildAttackStart` 하나(같은 판정이 그 사건을 공격 출처 `CombatAttackSource`를 여는 사건으로 만듦)이고, 칸·방향도 그 사실에서 옮겨 공격 모양 `CombatAttackShape.ForChildAttack` + 공격 시작 `CombatAttackStart`로 친다(줄이 없어도 침, 특성 시스템 View 설계 TS4). 큐 자리(발동 유닛 몸·대상 몸·칸)는 `CombatCuePoints.ForActivation(발동 사실, …)` 한 곳이고, 몸 자리 찾기는 보인 사실 `CombatShownFacts.StandingBodyOf`(캐릭터·구조물, 서는 자리 먼저)다. 공격 재생·종류 전략·Gate의 입력은 공격 사건이 아니라 공격 모양 값 하나라서, 발동 길을 따로 두지 않는다(설계 TA2·TA5·TA6). 새 특성 발동을 보이려면 Core가 발동 사실을 내고 그 특성 외형 에셋에 줄을 채우면 된다(코드 변경 없음) | 처치 연쇄·난사·연쇄 폭발·행운의 탄환·2연발(추가 공격 결과가 있는 발동) |
| 특성 발동 유닛 행동(나가기·돌기 → 머물기 → 돌아오기, 대신 맞기) | 특성마다 이동 코드를 만들지 않는다. 값은 발동 줄의 `unitAction`(행동 유닛·자리·거리·걸음 모양·시간 셋·보는 쪽)이고, 옮긴 자리는 유닛 채널 서는 자리(`Stand`, 자세 아래 층 — 피격 반응이 옮긴 자리에서 자세를 빌림)가 갖고, 끝 자리 계산은 `CombatUnitActionPlaces` 한 곳, 조립은 `CombatUnitActionComposer` 한 곳이다. 시점은 사실이 온 자리가 정한다: 사건 목록 안 발동은 특성 처리기가 행동 유닛마다 유닛 실행을 열고(사건은 기다리지 않음), 공격 결과 안 가로채기(희생)는 공격 시작이 공격 조립의 앞 항목(`CombatAttackLead`)으로 넣는다(공격 동작은 수호자가 나간 뒤, 돌아오기는 그 수호자의 맞는 순간 후속이 끝난 뒤 — 순차 재생기 질문 `ICombatSourceSettlement`). Core 위치는 바꾸지 않는다([유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) AA1~AA8) | 희생(대신 맞기) |
| 특성 흔적 지형(땅 다지기·먼지 연막) | 지형 외형 표에 넣지 않는다. 외형은 그 특성 외형 표의 흔적 칸(`trailPrefab`·`trailLocalOffset`)이고, 놓을지·지울지는 Buffer 환경 목록의 흔적 사실(`TraitTrailFact`, 지운 것도 `Active = false`로 남음)이 정한다. 사건 시점은 사실 처리기가 이동 원본 사건으로 맞추고 결과 끝에 흔적 표시 `FieldTrailPresenter`가 다시 그린다(설계 TS7) | `Trail_GroundCompaction`·`Trail_DustScreen`(임시 아트) |
| 유닛에 걸린 상태(버프·디버프)의 HUD 칸 | `FieldUnitPresenter`에 상태마다 갈래를 만들지 않는다. 아이콘·이름·설명 틀(`{0}`·`{1}` 칸)·구분(버프/디버프)은 View DB 상태 외형 표 `UnitStatusVisual`(종류 `UnitStatusKind`마다 에셋 하나)이 원본이고, 걸렸는지와 틀에 넣을 값의 차례는 상태 줄 표 `CombatUnitStatuses.Rows`의 줄(`StatusRow.Of`: 종류·Core 사실에서 상태 읽기·칸 차례의 값 읽기, 값 수 = 값 읽기 수)이 원본이다. 새 상태는 종류 값 하나 + 상태 외형 에셋 하나 + 줄 하나다. HUD 순서는 디버프 → 버프, 같은 구분은 줄 순서이고, 칸 키는 "종류 + 구분"이다. 틀이 그 줄의 값 수를 받는지는 전투 진입 때 줄 표가 확인한다(설계 TA9) | 사냥꾼의 표식·위압·방어 감소(옛 표식 하드코딩은 걷음) |
| 자주 도는 길(매 프레임·사건마다)의 검사·그리기·생성 | 열거 값 검사는 `System.Enum.IsDefined`(박싱) 대신 `CombatEnumValues.IsDefined<T>((int)값)`(int 바탕 열거). 강조 층·호버를 바꾸는 쪽은 지금처럼 `FieldView` 입구를 부르기만 한다 — 그리기는 `FieldView.LateUpdate`가 프레임당 한 번 한다(꺼져 있으면 바로). 전투 중 처음 생기는 표시(유닛·HUD·고스트·투사체·몸 위 글자·강조 판)는 전투 설치 때 미리 만든다 — 새 풀을 더하면 주인에 `Prewarm`을 두고 `CombatSceneDisplay.Prewarm`(셀 캐릭터 규칙 한 곳)에서 부른다. 그릴 때 넘기는 배열은 주인이 갖고 다시 쓴다(GlobalSystem은 받은 배열을 보관하지 않음) (GC 끊김 설계 GH1·GH2·GH4·GH6) | 사건마다 `Enum.IsDefined`, 강조 바꿀 때마다 `Refresh`, 처음 배치·첫 원거리 공격 때 `Instantiate` |
| 둘 이상에 걸쳐 고르기 어려움 | 만들기 전에 묻는다. 고른 까닭을 커밋·계획에 한 줄 남긴다 | — |

전투 폴더 밖은 그대로다. Factory는 `Systems/Factory/Runtime/InGame/View/`, 시험은 `Assets/Tests/EditMode/Framework/`(평면, Combat 폴더 구조를 따라 하지 않음)에 둔다.

## 3. 파일을 만들거나 옮길 때

1. `namespace`를 폴더 경로대로 쓴다(블록 네임스페이스). 다른 Combat 폴더의 형식을 쓰면 그 폴더의 `using`을 더한다. 자기 폴더와 그 위 폴더(예: `NS.Display.Unit` 파일에게 `NS.Display`·`NS`)의 형식은 `using` 없이 보인다.
2. `.cs`와 `.meta`를 함께 다룬다. 옮길 때 GUID를 유지한다.
3. MonoBehaviour·ScriptableObject를 다른 네임스페이스로 옮기면 클래스 위에 `[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "옛 네임스페이스")]`를 붙인다(2026-10-21 16개와 같은 꼴, 문서 주석 뒤·다른 특성 앞).
4. 다른 작업자 브랜치가 옛 위치·옛 네임스페이스로 만든 파일은 병합 뒤 이 안내대로 옮긴다([공유 메모](../handoffs/2026-10-21-combat-folder-restructure-share.md) §2).

## 4. 새 하위 폴더를 만들 때

- 지금 폴더로 설명이 안 되는 역할이 생기고, 그 역할의 파일이 함께 늘어날 때만 만든다. 만들기 전에 묻는다.
- **폴더 이름은 다른 형식 이름과 겹치지 않게 한다.** 네임스페이스는 같은 이름의 형식을 가린다(`Data.InGame.Enum`이 `System.Enum`을 가린 문제와 같음). 안에 있는 파일에서 그 이름이 형식이 아니라 네임스페이스로 풀린다.
  - 쓰지 않는 이름: `Camera`(UnityEngine) → `Cameras`, `Path`(System.IO) → `MovePath`, `Common`(`ViewSystem.Common` 등) → `Support`, `Terrain`·`Screen`·`Motion`·`Environment`(Unity·System 형식).
  - `Input`·`Display`는 Unity 형식 이름이지만 쓴다. `ViewSystem.Input` 네임스페이스와 `NS.Display`가 정리 전부터 있어 Combat 코드가 이미 `UnityEngine.Input`·`UnityEngine.Display`를 단순 이름으로 쓰지 않는다.
  - `Presentation/Environment/`도 예외다. 금지한 이름이지만 이미 있고 Combat 코드에 `System.Environment` 사용이 없다(2026-11-02 대조). 쓰게 되면 `System.Environment`로 적는다.
  - 확인 방법: 저장소에서 `(class|interface|struct|enum) <이름>\b`을 찾고, Unity·System에 같은 이름의 형식이 있는지 본다.
- 새 폴더의 `.meta`는 Unity가 만든 것을 커밋한다(손으로 만들지 않음).
- 이 안내 §0 트리와 §1.1 표에 한 줄 더한다.
