# 전투 고지대 View 표시 — Task·Phase 계획

설계 원본: [고지대 View 설계 2판](../specs/2026-10-20-combat-high-ground-view-design.md). 사용자 결정(2026-10-20): Q-HG1~Q-HG6 모두 A, 설계에서 정한 것 P1~P5 그대로.

Phase가 끝날 때마다 멈추고 보고한다. 커밋·push는 사용자가 한다.

> 현재 상태(2026-10-20): **HG-0~HG-4a 코드 완료**(HG-0~HG-2는 MSBuild 오류 0·관리 실행 PASS 101/FAIL 0, HG-3·HG-4a는 사용자 지시로 MSBuild 생략 = 컴파일 미확인, Unity 시험 미확인, §8.1~§8.3). 블록 프리팹은 사용자가 `Prefabs/Default/Environment/Terrain_Highground.prefab`으로 만들어 연결했고, 높이 값(T2.8)은 0.777(§8.3). 다음은 사용자 재생 확인, 그 뒤 HG-4b(연출 높이, 다음 계획).

## 0. 착수 전

### 0.1 착수 조건

- 설계 2판 §6의 겹침은 풀렸다. 호버 위험 범위 HZ-a·HZ-b가 사용자 커밋 `5de448e3`으로 들어갔고, 작업 트리에는 이 작업 문서 말고 바뀐 코드가 없다(에디터가 바꾼 FMOD·글꼴 캐시는 범위 밖).
- 새로 확인한 사실: 민형님이 `e74526b1`([민형] 고지대 모델링)로 `Assets/ProjectL/Resources/Map/Highground.fbx`를 올렸다.
  - 모델 하나(`Cube`), 머티리얼 하나(`Material`), 테이크 없음.
  - 단위는 cm(UnitScaleFactor 1)이고 정점 범위는 x −49.7~50.3, y 0.17~31.1, z −49.9~50.1이다. Unity 기본 가져오기(단위 변환)로는 약 **1 × 0.31 × 1, 피벗이 바닥 가운데**다. 칸 간격 1 × 1(`StageFieldView_Default.outlineCellSpacing`)에 맞는다.
  - 스크래치 FBX 파서로 읽었다. 실제 가져오기 배율은 Unity에서 확인한다.
- **`Highground.fbx.meta`가 커밋되지 않았다.** Unity가 처음 가져올 때 PC마다 다른 GUID를 만들어, 이 FBX를 참조하는 프리팹이 다른 사람 PC에서 깨진다. 이 PC에서 생긴 `.meta`를 커밋하거나 민형님께 올려 달라고 해야 한다(보고만, 삭제·수정 없음).

### 0.2 계획에서 정한 것 (사용자 확인 대상)

| # | 정한 것 | 이유 |
|---|---|---|
| P6 | **블록 외형은 Q-HG2 A의 임시 큐브 대신 민형님 `Highground.fbx`를 감싼 프리팹으로 한다.** 위치는 `Prefabs/Environment/Gameplay/Terrain_HighGround.prefab`. 그 FBX를 쓸 수 없으면 임시 큐브로 돌아간다. | 결정 때는 아트가 없다고 봤는데 그사이 모델이 생겼다. 결정과 다르므로 확인을 받는다. |
| P7 | `surfaceHeight`는 블록 윗면 높이(필드 로컬 단위)다. 값은 Unity에서 가져온 블록 크기를 재서 넣는다(예상 약 0.31). | 블록·유닛·걸음·강조가 같은 값을 읽는다(Q-HG1 A). |
| P8 | 칸 높이 표 `FieldSurfaceHeights`는 `FieldGridMapper`가 생성자에서 만들어 가진다. 생성자 서명은 그대로이고 기본값은 모두 0이다. | 높이가 필요한 곳은 이미 모두 mapper를 받는다. 시험의 `new FieldGridMapper(…)`(20개 이상) 무변경. |
| P9 | 여러 칸 유닛 높이 규칙(설계 P2, 점유 칸 중 가장 높은 값)은 `FieldGridMapper.SurfaceHeightOf(cells, anchor)` 한 곳에 둔다. | 서 있는 위치와 걸음 보간이 같은 규칙을 쓴다. |
| P10 | 걸음 시작 회전 방향(`CombatUnitMotionPlayback.cs:207`)은 위 방향을 빼고 가로로 투영한다. | 높이가 다른 두 칸 사이에서 유닛이 앞뒤로 기울지 않게 한다. |
| P11 | 뜀 식 `CombatStepHeight.At(h0, h1, t, hop)`: 높이 차가 0이면 h0 그대로(지금과 같음), 아니면 `lerp(h0, h1, t) + (|h1 − h0| / 2 + hop) · 4t(1 − t)`. `hop`은 설정 표 새 값 `stepClimbHop`(기본 0.1, 0 이상). | t = 0.5(칸 경계)에서 높은 칸 높이 + hop 이상이고, 그 뒤로 높은 칸 높이 아래로 내려가지 않아 블록 모서리를 뚫지 않는다. hop = 0이면 모서리에 딱 닿는다. |
| P12 | 강조 테두리와 이동 경로선은 칸을 높이별로 묶어 높이마다 그리기 손잡이를 따로 둔다. 손잡이는 그 높이 칸이 처음 생길 때 만든다. 경로선은 높이가 같은 연속 구간마다 한 줄이고, 높이가 바뀌는 칸 경계에서 끊긴다(오르내림 선은 보류). | 하위 `AreaOutlineData`·`DrawPointLineData`는 높이를 하나만 받는다. 하위 Render는 고치지 않는다(궤적 작업 때 Framework 작업자 의견). 평지 스테이지는 손잡이 수가 지금과 같다. |
| P13 | CellPrefab 표시 모드는 이번 범위 밖이다. 블록이 그 칸의 칸 프리팹을 가린다(보고만). | 운영 설정은 GridOutline이고, 칸 프리팹은 Build 때 놓여 높이를 모른다. |
| P14 | 포인터 칸 고르기는 `FieldView.TryRayToCell` 한 곳에서 한다. 높은 높이 평면부터 Ray와 만나는 점을 보고, 그 칸의 높이가 그 평면이면 그 칸이다. 아니면 다음 높이를 보고, 마지막은 바닥이다. Drag 고스트·이동 포인터가 쓰는 "필드 평면 점"은 그대로 둔다. | 칸 고르기만 높이를 알면 된다. 칸 밖 따라가기는 바닥 평면으로 충분하다. |
| P15 | 격자 데칼은 투영 상자가 바닥부터 가장 높은 칸 높이까지 덮게 한다. 블록 렌더러의 Rendering Layer는 `FieldGround`로 둔다. 정확한 깊이·피벗 식은 HG-4a에서 Unity 재생을 보고 정한다. | 지금 깊이 1이 블록 높이를 덮는지는 피벗에 따라 다르다. 블록이 데칼 레이어에 없으면 윗면에 격자가 안 그려진다. |

### 0.3 검증 방식

- Phase마다 MSBuild(바뀐 파일 새 경고 0)와 바뀐 클래스 묶음 관리 실행을 한다. 전체 관리 실행은 하지 않는다. Unity Test Runner·재생 확인은 사용자가 한다.
- **회귀 기준:** 평지 스테이지(지금 운영 12개 전부)에서 기존 시험의 기대값이 하나도 바뀌지 않는다. 기존 시험을 고치는 것은 고지대를 직접 단언하던 시험(§1~§5 각 표)뿐이다.
- 재생 확인용 배치(Q-HG6 A): 사용자가 `Combat_Normal_A`의 `terrainPlacements`에 `Terrain_HighGround`를 임시로 몇 칸 넣는다. 예: 아군 배치 구역 한 칸, 가운데 두세 칸 이어서. `knownTo`는 Player·Enemy로 하고, 확인 뒤 되돌린다(커밋 안 함).

## 1. Phase HG-0 — 외형 표 등록·높이 칸·검사

화면 변화: 공개된 고지대·약한 지반·낭떠러지 칸에 호버하면 설명이 나온다. 지금 숨어 있는 `KeyNotFoundException`이 없어진다. **고지대 설명의 "벽 무시" 줄이 화면에 나온다**(문구 그대로, 사용자 결정).

| Task | 내용 | 파일 |
|---|---|---|
| T0.1 | `TerrainVisualData`에 `SurfaceHeight`를 더한다. 생성자 검사: 유한한 0 이상, 0보다 크면 GroundShape이고 프리팹이 있어야 함, PlacedPrefab이면 0이어야 함. 예외는 `InvalidOperationException`, 한국어 문구. | `DataBaseSystem/Data/View/Table/Terrain/Item/TerrainVisualDataSO.cs` |
| T0.2 | `TerrainVisualDataSO`에 칸 `surfaceHeight`(`Min(0)`, 툴팁 "블록 윗면 높이, 필드 로컬 단위. 0이면 바닥과 같음")를 더하고 `GetData`로 넘긴다. | 같은 파일 |
| T0.3 | `TerrainVisualDataTable.asset`에 세 외형을 등록한다: `TerrainVisual_HighGround`(`b12be8d8…`)·`_WeakGround`(`abec384a…`)·`_Cliff`(`14c3cf68…`). 세 SO의 높이는 0(HG-2 전까지). 표 검사는 지금 그대로(PlacedPrefab 프리팹 필수·종류 중복 금지). | `DataBaseSO/ViewGroup/TerrainVisual/` |
| T0.4 | 시험 고침: `TerrainVisualDatabaseTests.cs:212`의 "세 종류 미등록" 단언 → 등록·GroundShape·높이 0 단언(실제 DB, Unity 전용). | `Tests/EditMode/Framework/TerrainVisualDatabaseTests.cs` |
| T0.5 | 새 시험: 외형 Data 높이 규칙 사례(음수·무한·PlacedPrefab 높이·프리팹 없는 높이 거절, GroundShape 프리팹 + 높이 허용). 실제 DB로 고지대 칸 호버 설명이 예외 없이 이름·설명을 내는 사례(Unity 전용). | `EnvironmentDefinitionTests.cs`, `CombatEnvironmentHoverTests.cs` |

- 커밋: 코드(T0.1·T0.2)와 에셋(T0.3)은 한 커밋이다. 표 등록 없이 코드만 있으면 화면이 그대로이고, 에셋만 있으면 SO 칸이 없다.
- 공유:
  - 호진님 새 메모 `handoffs/2026-10-20-high-ground-terrain-visual-hojin-share.md`: 표 등록 셋·새 칸·검사 규칙.
  - 현강님: 기존 메모에 한 줄(호버 설명이 고지대 등에서 이제 나옴, `CombatEnvironmentText` 무변경).

## 2. Phase HG-1 — 고지대 블록 배치

화면 변화: 고지대 칸에 블록이 생긴다. **그 위 유닛은 블록에 묻힌다**(HG-2에서 해결, 재생 확인은 HG-2와 함께).

| Task | 내용 | 파일 |
|---|---|---|
| T1.1 | `TerrainViewSet.TryResolveKind`의 세 종류 이름 목록을 걷는다(F6). 원본 하나의 "보이는가" 판단(분류·켜짐·Player 공개·종류 있음)을 함수 하나로 묶는다. `Render`와 HG-2의 `VisitShown`이 이것을 같이 쓴다. | `Display/TerrainViewSet.cs` |
| T1.2 | `FieldTerrainPresenter.CanDisplay`를 `VisualMode == PlacedPrefab`에서 "외형에 프리팹이 있음"으로 바꾼다. PlacedPrefab은 표 검사로 늘 프리팹이 있으니 지금과 같다. | `Display/FieldTerrainPresenter.cs:231` |
| T1.3 | 블록 프리팹 `Prefabs/Environment/Gameplay/Terrain_HighGround.prefab`(P6): 루트 아래 `Highground.fbx`를 두고, 렌더러 Rendering Layer = `FieldGround`(P15), 콜라이더 없음(설계 P4). **먼저 사용자가 Unity를 열어 FBX `.meta`를 만들어야 한다**(GUID를 알아야 참조할 수 있음). 그 뒤 Claude가 YAML로 만들거나 사용자가 에디터에서 만든다. | 새 프리팹 + `.meta` |
| T1.4 | `TerrainVisual_HighGround`의 `prefab`에 T1.3을 넣는다(높이는 아직 0). | 외형 SO 에셋 |
| T1.5 | 시험 고침: `TerrainViewSetTests.GroundShapeSource_DoesNotRequestModeOrRental_…`은 이제 그릴 수 있는지를 묻는다 → "프리팹 없는 바닥 지형은 대여하지 않고 이전 View를 반환" 단언으로. `FieldTerrainPresenterTests`의 GroundShape 두 시험(`GroundShapeRotation_…`·`GroundShapeMode_…`)은 "프리팹 없는 GroundShape는 그리지 않음"을 지키고 "프리팹 있는 GroundShape는 그림" 사례를 더한다. 호진님 `HojinCombatTornadoTerrainVisualTests`는 토네이도만 쓰므로 영향 없음(실행으로 확인). | `TerrainViewSetTests.cs`, `FieldTerrainPresenterTests.cs` |

## 3. Phase HG-2 — 칸 높이 표·서 있는 유닛

화면 변화: 고지대 위 유닛·배치 고스트가 블록 윗면에 선다. 걸음은 아직 비스듬한 직선이다(HG-3에서 뜀).

| Task | 내용 | 파일 |
|---|---|---|
| T2.1 | 새 순수 표 `FieldSurfaceHeights`: 칸 수 크기 float 배열, `Reset()`, `Raise(cell, height)`(큰 값만 남김), `HeightAt(cell)`, `MaxHeight`, 쓰는 높이 목록(HG-4a용). 할당은 생성 때 한 번이다. | 새 `Display/FieldSurfaceHeights.cs`(+ `.meta`) |
| T2.2 | `FieldGridMapper`가 생성자에서 표를 만들어 `Surface`로 내놓는다(P8). 새 `CellSurfaceToWorld(cell)`, `SurfaceHeightOf(cells, anchor)`(P9)를 더한다. 옛 `CellCenterToLocal/ToWorld`는 그대로 둔다(설계 P1). | `Display/FieldGridMapper.cs` |
| T2.3 | `FieldTerrainPresenter.SyncSurface(environment, bindings, mapper)`: 표를 비우고, T1.1의 같은 판단을 통과한 원본마다 그 외형의 `SurfaceHeight`로 `Raise`한다. 높이를 채우는 쪽은 이 하나뿐이다. | `Display/FieldTerrainPresenter.cs` |
| T2.4 | `CombatSceneDisplay.Refresh` 맨 앞(유닛 그리기 전)에서 `SyncSurface`를 부른다. `Install`도 끝에서 `Refresh`를 부르므로 첫 표시도 따른다(설계 F10, P3). | `CombatSceneDisplay.cs:184` |
| T2.5 | `FieldUnitPresenter.CenterOf` = 지금 위치 + 위 방향 × `SurfaceHeightOf`. 서 있는 위치·구조물·`FactPosition`(걸음 양 끝)·몸 상자(`BodyOf`·`BodyCenterOf`)가 같이 따라온다. 접촉은 가로(XZ)만 보므로 판정은 같고, 피격 이펙트 몸 중심은 저절로 높이를 따른다(설계 4b의 일부가 여기서 풀림). | `Display/FieldUnitPresenter.cs:550` |
| T2.6 | 배치 고스트 칸 위치를 `CellSurfaceToWorld`로 바꾼다. | `CombatPlacementFeedback.cs:37` |
| T2.7 | 걸음 시작 회전 방향을 가로로 투영한다(P10). | `CombatUnitMotionPlayback.cs:207` |
| T2.8 | `TerrainVisual_HighGround.surfaceHeight`에 Unity에서 잰 블록 윗면 높이를 넣는다(P7). | 외형 SO 에셋 |
| T2.9 | 새 시험: `FieldSurfaceHeightsTests`(순수, 큰 값 남김·비움·범위 밖 예외), mapper 표면 함수(평지 = 옛 함수와 같음·고지대 칸 높이·여러 칸 최댓값), `SyncSurface`(숨김·꺼짐·분류 밖은 0), 고지대 위 유닛 위치·고스트(Unity 전용), 회전 투영. 평지 기존 시험 무변경 확인. | 새 시험 파일(+ `.meta`) |

## 4. Phase HG-3 — 높이 이동(걸음·넉백)

화면 변화: 높이가 다른 칸 사이 걸음이 블록 모서리를 넘는 뜀이 된다. 넉백으로 떨어질 때도 같다. 같은 높이 걸음은 지금과 똑같다.

| Task | 내용 | 파일 |
|---|---|---|
| T3.1 | 새 순수 함수 `CombatStepHeight.At(h0, h1, t, hop)`(P11). 할당 없음, 입력 검사(유한·t 0~1·hop 0 이상). | 새 `Display/CombatStepHeight.cs`(+ `.meta`) |
| T3.2 | 설정 표 새 값 `stepClimbHop`(기본 0.1)을 "유닛 이동 연출" 머리말 아래에 둔다. 값 조건은 설정 Data 한 곳(유한·0 이상)이고 에셋 값도 넣는다. | `CombatPresentationSettingsDataSO.cs`, `CombatPresentationSettings_Default.asset` |
| T3.3 | 값 흐름: Factory `stepClimbHop: presentation.StepClimbHop` → `CombatSceneEventPresenter`(끝 선택 인자, 기본 0) → `CombatUnitMotionSet` → `CombatUnitMotionPlayback`. 선택 인자라 Presenter를 직접 만드는 기존 시험 8개 이상은 무변경이다. | `CombatSceneDisplayFactory.cs:97` 주변, 세 생성자 |
| T3.4 | `ICombatUnitPlacement.Interpolate`에 `hop` 인자를 더한다(구현은 창구의 `Placement` 하나뿐, 시험 가짜 없음). 위치 = 가로 보간(양 끝에서 높이를 뺀 위치) + 위 방향 × `CombatStepHeight.At`. | `Display/ICombatUnitPlacement.cs`, `Display/CombatUnitPresentationWindow.cs:354`, `CombatUnitMotionPlayback.cs:170` |
| T3.5 | 새 시험: `CombatStepHeightTests`(순수: 같은 높이 = 직선, 양 끝값 정확, 오르기 t ≥ 0.5에서 높은 칸 높이 이상, 내리기 대칭, hop 0). 설정 값 조건(`CombatPresentationSettingsDataTests`). 실제 DB 값을 읽는 Factory 시험이 있으면 새 값 한 줄. | 새 시험 파일(+ `.meta`), 기존 설정 시험 |

- 커밋: 설정 Data 코드와 설정 에셋은 한 커밋이다(A-5 규칙).
- 공유:
  - 민형님 Factory 공유 §21: 이름 있는 인자 한 줄.
  - 호진님 설정 표 공유 §6: 새 값 하나.

## 5. Phase HG-4a — 강조·데칼·포인터

화면 변화: 고지대 칸의 이동·공격·위험 범위, 호버, 경로선이 블록 윗면에 보인다. 블록 윗면에도 격자가 있고, 블록 윗면을 누르면 그 칸이 잡힌다.

| Task | 내용 | 파일 |
|---|---|---|
| T4.1 | 테두리 슬롯마다 칸을 높이로 묶는다. 높이 0은 지금 손잡이를 그대로 쓰고, 다른 높이는 그 높이가 처음 생길 때 손잡이를 만든다. `YOffset` = 지금 값 + 그 높이. 호버 슬롯·레일·배치 구역도 같은 묶기를 쓴다(P12). | `Display/FieldOutlineRenderer.cs:196·304` |
| T4.2 | 이동 경로선(`MovementDraft`)은 높이가 같은 연속 구간마다 한 줄이다(P12). | 같은 파일 |
| T4.3 | 채우기는 칸 로컬 중심에 그 칸 높이를 더한다(`FieldCellFill.Update`가 칸마다 위치를 받음). | 같은 파일(채우기 수집) |
| T4.4 | 격자 데칼 투영 상자가 바닥부터 `Surface.MaxHeight`까지 덮게 한다(P15). 데칼은 구성 뒤 첫 Refresh에 켜고, 높이 표는 그 Refresh 맨 앞에서 채워지므로 순서가 맞다. | `Display/FieldGridDecal.cs:41`, `FieldOutlineRenderer` |
| T4.5 | 포인터 칸 고르기를 높이 순으로 한다(P14). | `Display/FieldView.cs:217` |
| T4.6 | 시험: 아웃라인 시험은 평지 사례가 지금 기대값 그대로인지(손잡이 수 포함) 확인하고, 고지대 사례(높이별 손잡이·YOffset·경로선 끊김·채우기 위치)를 더한다. 포인터 시험(고지대 윗면 Ray → 그 칸, 블록 앞 바닥 → 바닥 칸)은 EditMode Transform·Ray로 한다. | `FieldOutlineRendererTests.cs`, 새 사례 |

## 6. 다음 계획(HG-4b) 후보 — 이번에 하지 않음

- 투사체·궤적의 출발·목표 칸 위치(`CombatShotPath`·`CombatAttackTarget`·`CombatAttackTrajectory`)를 표면으로.
- 칸 위 물건(`FieldObjectPresenter`·`FieldThornPresenter`·`CombatEnvironmentEffectPresenter`)의 놓는 위치를 표면으로.
- 피격 반응 밀림 방향(`CombatHitDirection`, 이미 가로 투영)은 확인만.
- 기차·레일은 스테이지 배치가 고지대와 겹치지 않는지 확인만.

## 7. 새 파일과 커밋 묶음

| Phase | 새 파일(모두 `.meta`와 함께) | 함께 커밋할 에셋 |
|---|---|---|
| HG-0 | 없음 | `TerrainVisualDataTable.asset` |
| HG-1 | `Terrain_HighGround.prefab`, (민형님 확인 뒤) `Highground.fbx.meta` | `TerrainVisual_HighGround.asset` |
| HG-2 | `FieldSurfaceHeights.cs`, `FieldSurfaceHeightsTests.cs` | `TerrainVisual_HighGround.asset`(높이) |
| HG-3 | `CombatStepHeight.cs`, `CombatStepHeightTests.cs` | `CombatPresentationSettings_Default.asset` |
| HG-4a | 없음(시험 사례는 기존 파일) | 없음 |

## 8. 실행 기록

### 8.1 HG-0~HG-2 (2026-10-20)

사용자 지시: "HG-2까지는 쭉 진행해"(§0.2 P6~P15는 쓴 그대로 따름). 진행 중 "MSBuild 스킵"이라, 빌드·관리 실행은 HG-2 코드까지 한 번만 했다.

**한 것**

| Phase | 결과 |
|---|---|
| HG-0 | `TerrainVisualData.SurfaceHeight`(생성자 끝 선택 인자, 기본 0)와 검사 셋(유한·0 이상 / 0보다 크면 GroundShape / 0보다 크면 프리팹 필수), SO 칸 `surfaceHeight`, `TerrainVisualDataTable.asset`에 세 외형 등록(셋 다 프리팹 없음·높이 0). |
| HG-1 | `TerrainViewSet`의 이름 목록 `TryResolveKind`를 지우고 판단 하나 `TryShow`(분류·종류·켜짐·Player 공개)로 묶었다. 같은 판단을 쓰는 `VisitShown`을 더했다. `FieldTerrainPresenter.CanDisplay`는 "외형에 프리팹이 있음"이다. |
| HG-2 | 새 `FieldSurfaceHeights`(+ `.meta`). `FieldGridMapper`의 `Surface`·`CellSurfaceToWorld`·`SurfaceHeightOf`(여러 칸 = 최댓값)·`SurfaceOffset`. `FieldTerrainPresenter.SyncSurface`(실패하면 바닥으로 되돌림). `CombatSceneDisplay.Refresh` 맨 앞에서 부름(Install도 Refresh를 거침). `FieldUnitPresenter.CenterOf`에 표면 높이(서 있는 위치·걸음 양 끝·몸 상자가 따라옴). 배치 고스트는 `CellSurfaceToWorld`. 걸음 시작 회전은 가로 투영. |

**시험**

- 고친 기존 시험:
  - `TerrainVisualDatabaseTests` 실제 카탈로그(이름 바뀜, 등록·GroundShape·설명 단언)
  - `TerrainViewSetTests` 바닥 지형 시험(같은 판단을 거치고 그릴지는 표시 판단이 정함)
  - `FieldTerrainPresenterTests` SetUp(세 외형을 프리팹 없이 등록)과 GroundShape 시험 둘의 이름
- 새 시험:
  - `EnvironmentDefinitionTests` 높이 규칙 넷(사례 6)
  - `TerrainVisualDatabaseTests.ActualCatalog_GroundShapeHoverDescriptionsDoNotThrow`
  - `TerrainViewSetTests.VisitShown_…`, 공용 도우미 `Ground`
  - `FieldTerrainPresenterTests` 블록 배치·높이 채우기
  - 새 파일 `FieldSurfaceHeightsTests.cs`(+ `.meta`, 순수 5·Unity 전용 3)
  - `CombatPlacementFeedbackTests.GhostCoordinates_StandOnRaisedCellSurface`

**검증**

- MSBuild(`ProjectL.Tests.Framework.csproj`, 새 파일 둘은 추가 targets로 넣음): 오류 0, 바꾼 파일 새 경고 0. csproj 누락 대조로 빠진 파일은 새 두 파일뿐이었다.
- 바뀐 클래스 묶음 관리 실행 8클래스 138개: PASS 101, FAIL 0, NATIVE 37(Unity 전용: 프리팹·Transform·실제 DB).
- Unity 컴파일·Test Runner·재생은 미확인이다.

**영향 확인(코드만)**

- 실제 DB로 장면 표시를 만드는 시험(`TerrainOperationalDisplayTests`·`CombatSceneEventPresenterTests`)은 낭떠러지 원본을 쓴다. 이제 외형을 조회하지만 표에 등록돼 있고 프리팹이 없어 그리지 않으므로, 기대값(블록 수 등)은 그대로다.
- 평지에서는 `CenterOf`·`CellSurfaceToWorld`가 옛 값과 비트 단위로 같다(높이 0 → 0 변위).
- 걸음 회전 가로 투영은 평지에서 차이가 부동소수 오차 수준이다.

**계획과 다른 점**

1. **T1.3·T1.4·T2.8(블록 프리팹·SO 연결·높이 값)은 하지 못했다.** `Highground.fbx.meta`가 아직 없다(Unity가 10:39 이후 갱신하지 않음, FBX는 11:06에 받음). GUID를 모르면 프리팹이 FBX를 참조할 수 없고, 남의 에셋에 `.meta`를 손으로 만들지는 않았다. 조사로 확인한 것: 이 저장소의 FBX 중첩 프리팹은 모델 루트 Transform fileID `-8679921383154817045`를 공통으로 쓴다(`CannonEnemy.prefab` 등). 그래서 `.meta`만 생기면 프리팹을 YAML로 만들 수 있다.
2. `FieldSurfaceHeights`에 최대 높이·높이 목록은 넣지 않았다. HG-4a(데칼·포인터·높이별 손잡이)가 쓸 때 더한다.
3. 걸음 회전 투영의 시험은 만들지 않았다. 걸음 재생 시험 조립이 커서, HG-3 재생 확인으로 갈음한다.
4. 높이 채우기는 보이는 지형 원본 모두에 `Raise`를 부른다. 높이 0은 표를 바꾸지 않으므로 별도 if를 두지 않았다.

**남은 에디터 작업(사용자)**

1. Unity에 포커스를 주어 `Highground.fbx`를 가져오게 한다. `.meta`가 생기면 그 `.meta`를 커밋하거나 민형님께 알린다.
2. 그 뒤 Claude에게 알리면 `Prefabs/Environment/Gameplay/Terrain_HighGround.prefab`(FBX 중첩, 콜라이더 없음)을 만들고, `TerrainVisual_HighGround`의 `prefab`·`surfaceHeight`(FBX 윗면 약 0.31, 가져온 크기로 확인)를 넣는다. 에디터에서 직접 해도 된다.
3. 재생 확인(Q-HG6 A): `Combat_Normal_A`에 고지대를 임시로 배치한다. 볼 것은 블록이 생기는지, 그 위 유닛·고스트가 블록 윗면에 서는지, 걸음이 비스듬히 오르내리는지(HG-3 전이라 뜀 없음)다.

**커밋 묶음**

- 코드 `TerrainVisualDataSO.cs`와 에셋 `TerrainVisualDataTable.asset`은 한 커밋이다.
- 새 파일 `FieldSurfaceHeights.cs`·`FieldSurfaceHeightsTests.cs`는 `.meta`와 함께 커밋한다.
- 공유 메모 둘(미전달): 호진님 새 메모 `handoffs/2026-10-20-high-ground-terrain-visual-hojin-share.md`, 현강님 `handoffs/2026-10-20-environment-facts-helper-hyeongang-share.md` §4.

**원칙 자체 점검(7기준)**

- SSOT 9: 보이는 지형 판단·높이 값·여러 칸 높이 규칙이 각각 한 곳이고, 이름 목록을 없앴다.
- SOLID 8.5: 채우기는 지형 표시, 읽기는 mapper, 서기는 유닛 위치 함수가 맡는다.
- FW 9: 서비스·Factory 무변경.
- 독립성 8.5: 바닥 함수를 그대로 두어 평지 동작이 같다.
- 캡슐화 8.5, C#/Unity 8.5(배열 재사용·캐시한 대리자), If/Tick 9(매 프레임 일 없음).
- 약 8.8. 낮음 하나: `Refresh` 순서(높이 → 유닛)를 지키는 것이 주석과 시험에만 기대고 형식으로 강제되지 않는다.

### 8.2 HG-3 (2026-10-20)

사용자 지시: "HG-3 진행해줘. 프리팹은 FBX .meta 생기면 따로 알려줄게." 앞 턴의 "MSBuild 스킵"을 이어 **빌드·관리 실행을 하지 않았다(컴파일 미확인).**

**한 것**

- 새 순수 함수 `Display/CombatStepHeight.cs`(+ `.meta`).
  - `At(from, to, t, hop)`: 높이가 같으면 시작 높이 그대로이고, 다르면 `from + Δ·t + (|Δ|/2 + hop)·4t(1−t)`(P11)이다.
- 설정 표 새 값 `stepClimbHop`(기본 0.1, "유닛 이동 연출" 묶음).
  - 조건 함수 `ValidateStepClimbHop`(유한한 0 이상)를 `Validate`의 끝에서 부른다.
  - 에셋 `CombatPresentationSettings_Default.asset`에 `stepClimbHop: 0.1`을 적었다.
- 값 흐름: Factory 이름 있는 인자 → `CombatSceneEventPresenter`(끝 선택 인자, 기본 0, 생성자에서 같은 조건 함수로 검사) → `CombatUnitMotionSet` → `CombatUnitMotionPlayback`(둘 다 끝 선택 인자).
- `ICombatUnitPlacement.Interpolate`에 `climbHop` 인자를 더했다. 구현은 창구 `Placement` 하나이고 시험 가짜는 없다.
  - 사실 위치에서 표면 높이를 빼 가로를 직선으로 잇고, 높이는 `CombatStepHeight.At`로 다시 더한다.
  - 같은 높이면 0을 빼고 더하므로 옛 직선 보간과 같다.

**시험**

- 새 `CombatStepHeightTests.cs`(+ `.meta`, 순수 5메서드·사례 15): 같은 높이 = 평평, 양 끝값, 오르기가 경계부터 블록 위, 내리기 = 오르기의 거울, hop은 가운데만 올림.
- `CombatPresentationSettingsDataTests`: `stepClimbHop` 거절 사례 둘, 실행 조건·기본값 단언 셋.
- 고친 기존 시험은 없다. 새 인자가 모두 기본값 있는 선택 인자라, Presenter를 직접 만드는 시험 8개 이상이 그대로다.

**계획과 다른 점**

1. `CombatStepHeight.At`은 입력 검사를 하지 않는다. 계획 T3.1에는 "입력 검사"가 있었다. 뜀 높이는 생성 때 한 번 검사하고 진행률은 걸음 재생이 0~1로 묶으므로, 매 프레임 같은 검사를 되풀이하지 않게 했다(If/Tick 기준).
2. Factory가 실제 DB 값을 넘기는지 보는 시험은 더하지 않았다. 설정 값 하나하나를 단언하는 Factory 시험이 없어서다.

**확인 방법(사용자)**

- Unity 컴파일 뒤 `CombatStepHeightTests`·`CombatPresentationSettingsDataTests`를 돌린다. 평지 걸음 회귀는 `CombatMovementViewTests`·`CombatSceneEventPresenterTests`·`CombatStepContactGateTests`로 본다.
- 재생: 블록 프리팹을 연결한 뒤(남은 에디터 작업 §8.1) 고지대를 오르내리는 걸음과 넉백으로 떨어지는 뜀을 본다. 같은 높이 걸음은 지금과 같아야 한다.

**공유(미전달)**

- 민형님 Factory 공유 §21: 이름 있는 인자 한 줄.
- 호진님 설정 표 공유 §6: 새 값 하나.

**커밋 묶음**

- 설정 Data 코드와 설정 에셋은 한 커밋이다.
- 새 파일 `CombatStepHeight.cs`·`CombatStepHeightTests.cs`는 `.meta`와 함께 커밋한다.

**원칙 자체 점검(7기준)**

- SSOT 9: 높이 식 한 곳(걸음·넉백 공유), 값과 조건은 설정 표 한 곳.
- SOLID 8.5, FW 9: Factory 한 줄, 서비스 무변경.
- 독립성 9: 같은 높이는 옛 동작 그대로.
- 캡슐화 8.5, C#/Unity 8.5(할당 없는 식), If/Tick 9.
- 약 8.8.

### 8.3 HG-4a와 사용자 재생 보고 (2026-10-20)

사용자 지시: "HG-4a 진행해줘". 빌드·관리 실행은 하지 않았다(앞 지시 "MSBuild 스킵" 유지, 컴파일 미확인).

**한 것**

| 항목 | 결과 |
|---|---|
| 높이 표 | `FieldSurfaceHeights`에 `Version`(값이 바뀔 때만 오름), `HasRaised`, `MaxHeight`, `RaisedLevels`(오름차순, 늦게 계산)를 더했다. `BeginRefill`/`EndRefill`은 다시 채우는 동안의 변경을 끝에서 한 번만 비교한다. `SyncSurface`는 바뀜 여부를 돌려주고, `CombatSceneDisplay.Refresh`가 바뀌었을 때 `FieldView.RepaintSurface()`를 부른다. 강조 갱신이 없는 Refresh에서도 높이별 그리기가 맞게 하기 위해서다. |
| 테두리 | `FieldOutlineRenderer`가 슬롯 칸을 높이로 묶는다(`ApplyArea`). 높이 0은 기본 핸들, 다른 높이는 처음 필요할 때 만든 추가 핸들 `FieldOutline_<층>_<n>`로, `YOffset`에 표면 높이를 더해 그린다. 높이 표 `Version`이 바뀌면 그린 기억을 모두 지워 다시 그린다. 해제 때 추가 핸들도 돌려준다. |
| 경로선 | 높이가 같은 연속 구간마다 한 줄이다(`ApplyPath`). 첫 구간은 기본 핸들, 다음 구간은 추가 핸들이고, 쓰지 않은 핸들은 비운다. |
| 채우기 | 칸 위치를 `CellSurfaceToWorld`로 바꿨다. |
| 데칼 | `FieldGridDecal.Show`에 `surfaceTop`(선택 인자)을 더해 투영 상자를 그만큼 위로 늘린다. 표시기는 가장 높은 칸 높이가 바뀌면 다시 켠다. |
| 포인터 | `FieldView.TryRayToCell`이 높은 평면부터 본다. 바닥 평면 점(`TryRayToFieldPlane`)은 그대로다. |

평지에서는 핸들 수·`YOffset`·데칼 배치·채우기 위치가 모두 옛 값과 같다(높이 0을 더함).

**시험**

- `FieldSurfaceHeightsTests`: 높이 목록·최대 높이, 다시 채우기와 `Version`.
- `FieldTerrainPresenterTests`: `SyncSurface` 바뀜 여부 단언.
- `FieldOutlineRendererTests`(새 다섯):
  - 높은 칸 추가 핸들(호버 포함, 해제)
  - 평지 핸들 수 그대로
  - 경로 구간 셋
  - 채우기 높이
  - 데칼 높이
  - `SetUp`·`RebuildWithFill`이 mapper를 필드로 가지게 고쳤다.
- `CombatPlacementFeedbackTests.FieldRay_PicksRaisedCellTop_BeforeTheGroundBehindIt`
- `TerrainVisualDatabaseTests`: 고지대 프리팹·높이 단언.

**사용자 재생 보고와 처리(같은 날)**

사용자 보고: "1. 고지대 지역 고스트 프리팹 드래그 시 고지대에 따른 위치 변화, 2. 고지대 이동 시 점프 안함".

- 확인한 것:
  - 사용자가 FBX를 가져오고(`.meta` 생김) 프리팹 `Prefabs/Default/Environment/Terrain_Highground.prefab`(FBX 변형, 180° 회전)을 만들어 `TerrainVisual_HighGround.prefab`에 넣었다.
  - 외형 `localScale.y`는 2.5로 두었다.
  - 그런데 **`surfaceHeight`가 0**이라 높이 표가 비었다. 그래서 고스트·유닛이 바닥에 있고 걸음에 뜀이 없다.
  - 코드 쪽은 이미 맞다. 드래그 고스트는 칸 위에서 `MoveGhostToCell` → `CellSurfaceToWorld`를 쓰고, 칸 고르기는 높이를 안다. 테스트 배치(`Combat_Normal_E`, 고지대 4칸, Player·Enemy 공개)도 맞다.
- 고친 것: `surfaceHeight`를 **0.777**로 넣었다(계획 T2.8). FBX 윗면 31.086cm × 가져오기 배율 0.01 × 외형 `localScale.y` 2.5 ≈ 0.7772다.
- 주의: 블록 높이를 바꾸려고 `localScale.y`를 고치면 `surfaceHeight`도 같이 고쳐야 한다(Q-HG1 A의 대가, 값 두 곳).
- 남은 에디터 작업:
  - 블록 렌더러의 Rendering Layer Mask에 `FieldGround`를 켜야 블록 윗면에도 격자 데칼이 찍힌다(P15). FBX 변형이라 렌더러 fileID를 몰라 YAML로 넣지 않았다.
  - `Highground.fbx.meta`·새 프리팹 커밋 또는 민형님 공유.
  - `Combat_Normal_E` 임시 배치는 확인 뒤 되돌린다(Q-HG6 A).

**확인 방법(사용자)**

- Unity 컴파일 뒤 `FieldOutlineRendererTests`·`FieldSurfaceHeightsTests`·`FieldTerrainPresenterTests`·`CombatPlacementFeedbackTests`·`TerrainVisualDatabaseTests`를 돌린다.
- 재생에서 볼 것:
  1. 고지대 위 유닛·고스트가 블록 윗면에 서는지
  2. 오르내릴 때 뜀이 있는지
  3. 고지대 칸의 이동·공격 범위·호버·경로선이 블록 윗면에 보이는지
  4. 블록 윗면을 누르면 그 칸이 잡히는지

**원칙 자체 점검(7기준)**

- SSOT 8.5: 높이별 그리기가 높이 표 하나를 따르고, 바뀜 신호도 표 하나다. 블록 높이 값이 `localScale`과 `surfaceHeight` 두 곳인 점은 남는다.
- SOLID 8: 표시기가 늘었지만 나누기·그리기·핸들 수명이 메서드별로 나뉘었다.
- FW 9: 하위 Render 무변경.
- 독립성 8.5: 평지 동작 그대로.
- 캡슐화 8.5.
- C#/Unity 8.5: 재사용 목록, Refresh 때만 계산.
- If/Tick 8.5: 평지 빠른 길 하나.
- 약 8.5.

### 8.4 재생 보고 2 — 고지대 칸 포인터 (2026-10-20)

사용자 보고: "고지대로 이동도 하는데 고스트 프리팹은 안올라감. 행동이 셀 좌표 기준이라 고지대 상단이 아닌 가려진 타일 아래를 가리켜야 정상 행동으로 인식함."

**원인(코드 대조)**

- HG-4a는 칸 고르기 `FieldView.TryRayToCell`만 높이를 알게 했다.
- 바닥 평면 점을 그대로 쓰던 곳이 셋 남아 있었다.
  - `CombatPointerMapper.TryMapPointer`: 이동 경로 Drag·공격 Drag·커서 스냅이 쓴다.
  - 이동 Drag 고스트 위치 `CombatSceneDisplay.UpdateMovementPointer`
  - 배치 고스트의 칸 밖 따라가기 `MoveGhostToPointer`
- 그래서 블록 윗면을 가리키면 시점 때문에 그 뒤쪽 칸으로 셌고, 이동 고스트는 바닥에 머물렀다.
- 커서 스냅의 칸 중심 화면 좌표도 바닥 기준이었다.

**고친 것** — 포인터 점 규칙을 `FieldView.TryRayToSurface` 하나로 모았다.

- 동작: 높은 칸 평면부터 보고 그 높이의 칸이면 그 점, 아니면 바닥 평면 점(격자 밖 포함)이다.
- `TryRayToCell`은 이 점으로 칸을 정한다(동작은 HG-4a와 같음).
- `CombatPointerMapper.TryRayToFieldPlane(화면)`을 `TryRayToSurface`로 바꿨다. 호출처 셋(`TryMapPointer`·`MoveGhostToPointer`·`UpdateMovementPointer`)이 표면 점을 쓴다.
- `TryCellCenterToScreen`은 `CellSurfaceToWorld`를 쓴다(옮긴 커서가 같은 칸으로 다시 잡힘).
- 평지에서는 모두 옛 값과 같다(높이 0).

**검증**

- 2026-10-20 사용자 상시 지시(메모리 "전투 View는 MSBuild·새 시험 없음")에 따라 빌드·새 시험은 없다(컴파일 미확인).
- 바꾼 서명의 호출처를 grep으로 모두 대조했다. 시험은 FieldView의 `TryRayToFieldPlane(Ray)`만 부르고 그 함수는 그대로라, 고칠 시험은 없다.
- HG-0~HG-4a에서 만든 새 시험(§8.1~§8.3)은 이 지시 전에 만든 것이라 그대로 두었다.

**재생 확인(사용자)**

- 배치 드래그와 이동 Drag에서 블록 윗면을 가리키면:
  - 그 칸이 잡히는지
  - 고스트가 블록 위로 올라가는지
  - 경로가 그 칸으로 이어지는지
- 공격 Drag·커서 스냅이 고지대 칸에서 맞는지.
