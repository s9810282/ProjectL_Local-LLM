# 전투 지형 이웃 모양 외형 계획 (2026-10-27)

- 작성일: 2026-10-27. 코드 변경 없음(계획만).
- 설계: [terrain-visual-variants-design](../specs/2026-10-27-combat-terrain-visual-variants-design.md)(TC1~TC5, F1~F19, P1~P5, 결정 §3 모두 A).
- 사용자 요청 흐름:
  1. "TerrainVisualSO를 확장해야 하나"를 물었다. B로 1판을 썼다.
  2. "그냥 C로 지금"이라고 해 2판을 C로 다시 썼다.
  3. "HeightGround 3종류 있음"이라고 알려 줘서 아트에 맞췄다.
  4. "Q-C1~Q-C5 모두 A로 하고 Task 계획 써줘".
- 경로 약칭:
  - `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `M/` = `C/Display/Map/`
  - `DBT/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Terrain/Item/`
  - `DBS/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageEnvironment/Item/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
  - `ENV/` = `Assets/ProjectL/Prefabs/Default/Environment/`
  - `VA/` = `Assets/ProjectL/DataBaseSO/ViewGroup/TerrainVisual/Item/`
- 성격:
  - TC-0은 지금 깨진 에셋을 고친다(설계와 무관, 부트 복구).
  - TC-a는 값만 더한다. 화면 변화가 없고 아무도 부르지 않는다.
  - TC-b는 표시를 바꾼다. 모양 칸이 비어 있어 화면 변화는 없다.
  - TC-c에서 모양 칸을 연결하면 **보스 고지대 L자 모서리 2칸에 경사가 나온다.** 문서도 여기서 쓴다.
- 상시 지시: MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다. 보고에는 "컴파일 미확인"을 적는다. 기존 시험은 컴파일이 깨지는 곳만 최소로 맞춘다.

## 0. 결정과 착수 조건

### 0.1 설계 결정 (2026-10-27, 사용자 "모두 A")

| # | 결정 |
|---|---|
| Q-C1 | 4방향 이웃, 드러난 변 모양 여섯(Center·OneSide·TwoSide·TwoSideOpposite·ThreeSide·FourSide) |
| Q-C2 | 지금 `prefab`이 Center이자 빈 모양의 대체다. 이름 있는 모양 칸 다섯을 더한다 |
| Q-C3 | 모양이 회전을 정한다. 모양을 쓰는 종류의 원본이 스테이지 Terrain 회전 항목에 있으면 예외다 |
| Q-C4 | 이웃 = 같은 종류이고 보이는(켜짐·Player 공개) 칸이다. 숨은 칸·필드 밖은 빈칸이다 |
| Q-C5 | 아트가 없는 셋(TwoSideOpposite·ThreeSide·FourSide)은 빈 칸으로 두고 Center로 그린다. 민형님께 요청 메모를 쓴다 |

### 0.2 계획에서 정한 것 (TP-1~TP-13, 사용자 확인 대상)

설계와 다르게 정한 것은 없다. 모두 설계가 비워 둔 세부다.

| # | 정한 것 | 이유(원칙) |
|---|---|---|
| TP-1 | TC-0에서 우리가 `VA/TerrainVisual_HighGround.asset` YAML을 고친다. `prefab`은 `HeightGround_Center`로, `localScale.y`는 2.5 → **1.9425**로 바꾼다. 그러면 0.4 × 1.9425 = 0.777이라 `surfaceHeight`는 그대로다. 사용자가 이미 에디터에서 고쳤으면 파일을 보고 건너뛴다 | 높이 0.777에 맞춘 HG-3·HG-4a(걸음 뜀 0.1·높이별 테두리)와 화면이 그대로다. 높이 값 원본(Q-HG1)을 바꾸지 않는다 |
| TP-2 | 모양 열거 `TerrainEdgeShape`는 DB 쪽 `DBT/TerrainEdgeShape.cs`에 둔다. R0 기준 드러난 쪽 표는 이 열거의 XML 문서에 둔다(안내 문서는 링크로) | 외형 Data가 열거를 써야 하는데 DataBaseSystem은 ViewSystem을 참조하지 않는다. 선례 `TerrainVisualMode.cs`. 방향 약속의 원본이 한 곳이다 |
| TP-3 | 모양 프리팹 묶음 값 `TerrainEdgeShapePrefabs`(sealed, 칸 다섯·`HasAny`·`Of(shape)`·정적 `None`)는 `DBT/TerrainVisualDataSO.cs` 안에 둔다. 외형 Data 생성자 끝 선택 인자 `TerrainEdgeShapePrefabs edgeShapes = null`이고, null이면 `None`이다 | 외형 Data와 함께만 바뀐다(선례: 같은 파일의 Data + SO). 모양 없음은 대부분 종류의 정상 값이다(설계 TC5) |
| TP-4 | 외형 Data에는 읽기 둘만 더한다. `UsesEdgeShapes`(`EdgeShapes.HasAny`)와 `ShapePrefab(shape)`다. `ShapePrefab`은 Center면 `Prefab`, 빈 칸이면 null을 돌려준다. **"빈 모양 → Center R0" 판단은 외형 선택 함수 한 곳**에만 둔다 | 대체 규칙이 Data와 선택 두 곳에 나뉘지 않는다(SSOT) |
| TP-5 | `DBS/StageEnvironmentVisualDataSO.cs`의 `StageEnvironmentVisualData`에 `TryGetTerrainRotation(sourceId, out rotation)`을 더한다. 옛 `GetTerrainRotation`은 그대로 둔다(시험 다섯 파일이 씀) | Q-C3은 "R0을 적음"과 "안 적음"을 구별해야 한다. 지금 함수는 없으면 R0이라 구별할 수 없다. 우리 파일이다 |
| TP-6 | 외형 열쇠 값 둘은 `M/TerrainLook.cs` 한 파일에 둔다. `TerrainLookKey`(종류·모양, `IEquatable`, 사전 열쇠)와 `TerrainLook`(열쇠·회전)이다 | 둘은 함께만 바뀐다. 값 형식이라 사전 열쇠로 쓸 때 할당이 없다(C#/Unity) |
| TP-7 | `TerrainViewSet` 생성자에 넷째 필수 인자 `Func<MovementTileFact, TerrainContentKind, TerrainLook> lookOf`를 더한다. 대여·반환은 `TerrainLookKey`로 묻고, 그리기 콜백은 `(view, source, look)`이다. `canDisplay(kind)`·`VisitShown`은 그대로다 | 수명 쪽은 "무엇을 빌리나"만 묻고 "어떻게 고르나"는 모른다(SRP). 선택 인자로 두면 시험 입구가 생긴다 |
| TP-8 | 이웃 표 `M/FieldTerrainNeighbors.cs`는 칸마다 `uint` 종류 비트(비트 = `1 << (int)kind`)를 담는다. `FieldTerrainPresenter`가 가지며, mapper의 열·행 수가 다르면 새로 만든다. `Render` 맨 앞에서 `VisitShown`으로 채우되 **모양을 쓰는 종류만** 표시한다 | 한 칸에 여러 종류(고지대 + 모래폭풍)가 같이 있을 수 있다. 배열을 한 번 만들어 다시 쓴다. 모양을 안 쓰는 스테이지는 표를 비우기만 한다 |
| TP-9 | Q-C3 예외는 외형 선택 함수가 던진다. `TerrainViewSet.Render`의 try 안에서 불리므로 지금 실패 정리(모두 반환)를 그대로 탄다. 문구는 `[FieldTerrainPresenter] 이웃 모양을 쓰는 {kind}의 원본 {id}에는 스테이지 외형 회전을 넣을 수 없습니다(모양이 회전을 정합니다).`이다 | 한 판단 = 한 곳. 부분 갱신이 남지 않는다 |
| TP-10 | `T/TerrainViewSetTests.cs` 맞춤은 호출만 바꾼다. 단언은 그대로다. 내용은 §3 TC-b T10 | 우리 시험이다(239줄 모두 우리 줄). 상시 지시대로 컴파일 맞춤만 한다 |
| TP-11 | 인스펙터: 모양 칸 다섯은 `surfaceHeight` 아래 `[Header("이웃 모양 (비우면 Center = Prefab, R0)")]`에 둔다. `prefab` 툴팁에 "이웃 모양을 쓰면 Center(드러난 변 없음)이자 빈 모양의 대체"를 더한다 | 칸의 뜻이 인스펙터에서 바로 보인다 |
| TP-12 | TC-b 끝에 검토 에이전트 하나를 돌린다(정적 검토, 코드만). 특히 §2.3의 16가지 표와 코드 대조를 맡긴다 | 새 시험·컴파일 확인이 없어 회전 표 실수를 잡을 길이 이것뿐이다. 사용자가 원하지 않으면 건너뛴다 |
| TP-13 | 민형님 요청 메모는 새 파일 `docs/superpowers/handoffs/2026-10-27-high-ground-edge-shapes-art-request-minhyeong.md`이다. 빠진 모양 셋, R0 방향 표, 크기·피벗·높이, `GroundGroup.fbx.meta` 미커밋, 격자 데칼용 Rendering Layer `FieldGround`(선택)를 적는다 | 아트 쪽 할 일을 한 문서로 넘긴다 |

### 0.3 착수 조건

1. 작업 트리에 이번 작업이 아닌 변경이 있다(낭떠러지 연출, `Combat.unity`, FMOD 캐시, `Test.asset`, `Unit_ShotGun.asset`, `TerrainVisual_Cliff.asset` 등). 이 계획이 고치는 파일과 겹치는 것은 `TerrainVisual_HighGround.asset` 하나다. 이 에셋은 사용자 아트 작업(옛 프리팹 삭제)과 묶여 있다(§6).
2. TC-0이 먼저다. 지금은 부트가 실패하므로(설계 F17) TC-a의 Unity 확인을 할 수 없다.
3. 각 Phase 끝에서 멈추고 보고한다. 다음 Phase는 사용자 Unity 확인 뒤에 시작한다.

## 1. 지금 코드 (HEAD `a4b2603b` + 작업 트리, 2026-10-27 대조)

| 무엇 | 어디 | 사실 |
|---|---|---|
| 외형 Data | `DBT/TerrainVisualDataSO.cs:9-71` | 생성자 `:25-26`(끝 선택 인자 `surfaceHeight = 0f`), 높이 검사 `:45-59`. SO 칸 `:77-89`, `GetData` `:91-94` |
| 외형 표 | `Queries/ViewDataBase/ViewTable/TerrainVisualTable.cs:31-34` | PlacedPrefab 프리팹 필수·종류 중복 검사. **바꾸지 않는다** |
| 스테이지 회전 | `DBS/StageEnvironmentVisualDataSO.cs:58·76-87` | `GetTerrainRotation` → 없으면 R0 |
| 외형 수명 | `M/TerrainViewSet.cs` | 항목 `:12-17`(`Kind`), 생성자 `:27-33`, `Render` `:38-91`(종류 바뀜 다시 빌림 `:53-57`, 대여 `:60`, 그리기 `:65`), `VisitShown` `:97-114`, 제거 `:148-153` |
| 지형 표시 | `M/FieldTerrainPresenter.cs` | `Instance` `:22-41`(프리팹·기본 회전·크기·오프셋을 외형에서), 풀 `:46`(종류별), 생성자 `:59-69`, `Render` `:103-132`, `CanDisplay` `:273`, `Apply` `:275-292`(회전 `:281-282`), `Rent`·`Return`·`Create` `:294-330` |
| 호출 | `C/Display/CombatSceneDisplay.cs:232·242` | `SyncSurface` → … → `Render`. **바꾸지 않는다** |
| 우리 시험 | `T/TerrainViewSetTests.cs` | `new TerrainViewSet<View>(…)` 10곳(`:27·37·69·96·113·130·143·175·204·218`), 그리기 도우미 `Apply` `:219`, 그리기 람다 둘 `:187·211` |
| 아트 | `ENV/HeightGround_Center`·`_OneSide`·`_TwoSide.prefab` | 루트 GameObject fileID: Center `5212738157284064913`(GUID `ab48d6fe7106193439d75817ea3c3af7`), OneSide `835281560417202317`(`d8aeccc0517aa1a45a17aa8901924856`), TwoSide `2773182547056476624`(`77f4a0fed2e3f8b45af1c68f01e49203`) |

## 2. 규칙 (구현의 기준)

### 2.1 방향과 비트

- 칸 (x, y)의 북 = (x, y+1)이고 필드 +Z다(Core +Y). 동 = (x+1, y), 남 = (x, y−1), 서 = (x−1, y)다.
- 드러난 변 비트: 북 1, 동 2, 남 4, 서 8. 이웃이 **없는** 쪽이 드러난 변이다.
- 시계 방향 90도 돌리기: `Rotate(m) = ((m << 1) | (m >> 3)) & 15`(북 → 동 → 남 → 서 → 북). `Rotation4`도 시계 방향이고, `RotationToWorld(R90)`은 +Z를 +X로 돌린다(`FieldGridMapper.cs:122-125`).

### 2.2 R0 기준 드러난 쪽 (= 프리팹 방향)

| 모양 | R0 비트 | 드러난 쪽 |
|---|---|---|
| Center | 0 | 없음 |
| OneSide | 2 | 동 |
| TwoSide | 3 | 북·동 |
| TwoSideOpposite | 10 | 동·서 |
| ThreeSide | 11 | 북·동·서(남에 이웃) |
| FourSide | 15 | 넷 다 |

### 2.3 16가지 표 (외형 선택 함수가 내야 하는 값)

회전은 "R0 비트를 몇 번 `Rotate`하면 이 칸 비트가 되나"의 가장 작은 수다.

| 드러난 비트 | 쪽 | 모양 | 회전 |
|---|---|---|---|
| 0 | — | Center | R0 |
| 1 | 북 | OneSide | R270 |
| 2 | 동 | OneSide | R0 |
| 3 | 북·동 | TwoSide | R0 |
| 4 | 남 | OneSide | R90 |
| 5 | 북·남 | TwoSideOpposite | R90 |
| 6 | 동·남 | TwoSide | R90 |
| 7 | 북·동·남 | ThreeSide | R90 |
| 8 | 서 | OneSide | R180 |
| 9 | 북·서 | TwoSide | R270 |
| 10 | 동·서 | TwoSideOpposite | R0 |
| 11 | 북·동·서 | ThreeSide | R0 |
| 12 | 남·서 | TwoSide | R180 |
| 13 | 북·남·서 | ThreeSide | R270 |
| 14 | 동·남·서 | ThreeSide | R180 |
| 15 | 넷 다 | FourSide | R0 |

보스 고지대 대조(설계 F4):

| 칸 | 이웃 | 드러난 비트 | 결과 | 지금 아트로 |
|---|---|---|---|---|
| (0,6) | 북 (0,7)·동 (1,6) | 12 | TwoSide R180 | **경사** |
| (10,6) | 서 (9,6)·북 (10,7) | 6 | TwoSide R90 | **경사** |
| (0,7)·(10,7) | 남 | 11 | ThreeSide R0 | Center R0 |
| (1,6)·(3,4)·(8,4) | 서 | 7 | ThreeSide R90 | Center R0 |
| (9,6)·(2,4)·(7,4) | 동 | 13 | ThreeSide R270 | Center R0 |

### 2.4 외형 선택 (원본 하나)

```text
visual = 외형(kind)
if !visual.UsesEdgeShapes:
    return (kind, Center, 스테이지 회전(없으면 R0))           ← 지금과 같음
if 스테이지 Terrain 회전에 source.Id가 있음: 예외(TP-9)
exposed = 4방향 중 이웃 표에 같은 kind가 없는 쪽의 비트
(shape, rotation) = 16가지 표(exposed)
if shape != Center && visual.ShapePrefab(shape) == null:
    return (kind, Center, R0)                                    ← 빈 모양 대체(Q-C5)
return (kind, shape, rotation)
```

## 3. Phase와 Task

### TC-0 깨진 에셋 고치기(부트 복구, 설계 무관)

| # | Task | 파일 |
|---|---|---|
| T0.1 | 에셋 파일을 먼저 읽는다. `prefab`이 아직 GUID `758d7769…`이면 `{fileID: 5212738157284064913, guid: ab48d6fe7106193439d75817ea3c3af7, type: 3}`로 바꾼다. `localScale` y는 2.5 → 1.9425다(TP-1). 이미 고쳐져 있으면 건너뛴다 | `VA/TerrainVisual_HighGround.asset` |
| T0.2 | 보고에 커밋 묶음 1(§6)을 적는다. 특히 `GroundGroup.fbx.meta`(`??`)를 꼭 함께 넣는다 | — |

- 화면 변화: 고지대가 경사 없는 새 상자(Center)로 보인다. 높이 0.777은 그대로다.
- Unity 확인: §5 U0.

### TC-a 값 더하기(화면 변화 없음)

| # | Task | 파일 |
|---|---|---|
| Ta.1 | 새 열거 `TerrainEdgeShape { Center = 0, OneSide, TwoSide, TwoSideOpposite, ThreeSide, FourSide }`를 만든다. XML 문서에 §2.2 표(R0 드러난 쪽)를 넣는다 | 새 `DBT/TerrainEdgeShape.cs` + `.meta`(새 GUID) |
| Ta.2 | `TerrainEdgeShapePrefabs`를 만든다. 생성자 `(oneSide, twoSide, twoSideOpposite, threeSide, fourSide)`, `HasAny`, `GameObject Of(TerrainEdgeShape)`(Center·정의 밖 값은 `ArgumentOutOfRangeException`), 정적 `None`이다 | `DBT/TerrainVisualDataSO.cs` |
| Ta.3 | 외형 Data 생성자 끝에 `TerrainEdgeShapePrefabs edgeShapes = null`을 더한다. 속성 `EdgeShapes`(null이면 `None`), `UsesEdgeShapes`, `ShapePrefab(shape)`(TP-4)를 둔다. 검사 하나를 더한다: `UsesEdgeShapes && prefab == null` → `InvalidOperationException($"{kind}: 이웃 모양을 쓰면 Center 프리팹(Prefab)이 필요합니다.")` | 같은 파일 |
| Ta.4 | SO에 칸 다섯(`oneSidePrefab`·`twoSidePrefab`·`twoSideOppositePrefab`·`threeSidePrefab`·`fourSidePrefab`)과 머리말·툴팁(TP-11)을 더한다. `GetData`가 묶음 값을 넘긴다 | 같은 파일 |
| Ta.5 | `TryGetTerrainRotation(long sourceId, out Rotation4 rotation)`을 더한다. `Find`와 같은 순회이고, 있으면 true다 | `DBS/StageEnvironmentVisualDataSO.cs` |

- 시험 영향 없음: 외형 생성 21곳은 끝 선택 인자라 그대로다. 운영 에셋은 새 칸이 비어 있어 기대값이 그대로다.
- Unity 확인: §5 Ua.

### TC-b 표시 바꾸기(모양 칸이 비어 화면 변화 없음)

| # | Task | 파일 |
|---|---|---|
| Tb.1 | `TerrainLookKey`(readonly struct: `Kind`·`Shape`, `IEquatable`, `GetHashCode` = `((int)Kind << 8) \| (int)Shape`)와 `TerrainLook`(readonly struct: `Key`·`Rotation`, 편의 `Kind`·`Shape`)을 만든다 | 새 `M/TerrainLook.cs` + `.meta` |
| Tb.2 | 이웃 표 `FieldTerrainNeighbors`를 만든다. 생성자 `(columnCount, rowCount)`, `ColumnCount`·`RowCount`, `Clear()`, `Mark(CellCoord, TerrainContentKind)`(범위 밖은 `ArgumentOutOfRangeException`), `Has(CellCoord, TerrainContentKind)`(범위 밖은 false)다 | 새 `M/FieldTerrainNeighbors.cs` + `.meta` |
| Tb.3 | 외형 선택 `CombatTerrainLook`(static)을 만든다. `(TerrainEdgeShape, Rotation4) ShapeOf(int exposed)`는 §2.3 표이고, R0 비트를 최대 세 번 돌려 찾는다. 할당이 없다. `TerrainLook Resolve(kind, visual, source, rotations, neighbors)`는 §2.4이고 예외 문구는 TP-9다 | 새 `M/CombatTerrainLook.cs` + `.meta` |
| Tb.4 | `TerrainViewSet`(TP-7): 항목의 `Kind` → `Key`(종류는 `Key.Kind`), 생성자 넷째 인자 `lookOf`, 대여·반환 `Func<TerrainLookKey, TView>`·`Action<TerrainLookKey, TView>`, 그리기 `Action<TView, MovementTileFact, TerrainLook>`. `Render`는 `canDisplay` 통과 뒤 `look = _lookOf(source, kind)`를 try 안에서 부른다. 같은 원본의 열쇠가 바뀌면 지금 `:53-57` 길로 반환하고 다시 빌린다. 문서 주석도 고친다 | `M/TerrainViewSet.cs` |
| Tb.5 | `FieldTerrainPresenter` 필드: 풀 `Dictionary<TerrainLookKey, CustomPool<Instance>>`, `_neighbors`(처음 `Render` 때 또는 mapper 크기가 다를 때 만듦), 캐시된 대리자 `_markNeighbor`·`_lookOf`. 생성자는 `new TerrainViewSet<Instance>(Rent, Return, CanDisplay, _lookOf)`다 | `M/FieldTerrainPresenter.cs` |
| Tb.6 | `Render`: `_rotations`·`ValidateTerrainRotations` 다음에 이웃 표를 채운다(`Clear` → `_views.VisitShown(…, _markNeighbor)`). `_markNeighbor`는 외형이 `UsesEdgeShapes`일 때만 `Mark`한다. 그다음 `_views.Render`다. `finally`에서 지금처럼 `_mapper`·`_rotations`를 비운다(이웃 표는 다시 쓰므로 남긴다) | 같은 파일 |
| Tb.7 | `_lookOf` = `CombatTerrainLook.Resolve(kind, _visuals.GetTerrainVisual(kind), source, _rotations, _neighbors)`. `Apply(instance, source, look)`의 회전은 `_mapper.RotationToWorld(look.Rotation) * instance.BaseRotation`이다. 지금 `:281`의 스테이지 회전 읽기는 지운다(선택 함수로 옮김) | 같은 파일 |
| Tb.8 | `Rent(key)`·`Return(key, instance)`·`Create(key)`: 프리팹은 `visual.ShapePrefab(key.Shape)`이고, null이면 예외다(선택 함수가 막으므로 계약 오류). `Instance`는 생성자 `(gameObject, prefab, visual, kind)`로 바꿔 기본 회전·크기를 **그 모양 프리팹**에서 읽는다. 오프셋·배율은 외형 값이다 | 같은 파일 |
| Tb.9 | 클래스·메서드 문서 주석에 이웃 모양을 적는다(설계 TC2~TC4 링크) | 같은 파일·`TerrainViewSet.cs` |
| Tb.10 | `T/TerrainViewSetTests.cs` 호출 맞춤(TP-10). 단언은 바꾸지 않는다. ① 도우미 `private static TerrainLook Plain(MovementTileFact _, TerrainContentKind kind) => new(new TerrainLookKey(kind, TerrainEdgeShape.Center), Rotation4.R0);` ② 생성 10곳 끝에 `Plain` ③ `:27` `rented = key.Kind` ④ `:97` 그대로(종류 판단) ⑤ `:143-144` `rented.Add(kind.Kind)`·`returned.Add(kind.Kind)` ⑥ `Apply` 도우미에 셋째 인자 ⑦ `:187`·`:211` 람다에 셋째 인자 `_` | `T/TerrainViewSetTests.cs` |
| Tb.11 | 검토 에이전트 하나(TP-12). 맡길 것: §2.3·§2.4와 코드 대조, 실패 정리 경로, 풀 열쇠 바뀜, 할당 | — |

- 화면 변화 없음: 모든 종류가 `UsesEdgeShapes = false`라 (종류, Center, 스테이지 회전)이다. 지금과 같은 결과다.
- 다른 시험: `FieldTerrainPresenterTests`(호진님 줄 포함)는 생성자·`ValidateTerrainRotations`가 그대로라 고치지 않는다.
- Unity 확인: §5 Ub.

### TC-c 연결·확인·문서

| # | Task | 파일 |
|---|---|---|
| Tc.1 | 모양 칸 둘을 연결한다. `oneSidePrefab` = OneSide 루트, `twoSidePrefab` = TwoSide 루트(§1 표의 fileID·GUID)다. 나머지 셋은 빈 칸이다 | `VA/TerrainVisual_HighGround.asset` |
| Tc.2 | 안내 문서 §2에 "이웃 모양" 절을 더한다(칸 다섯, §2.2 표 링크, 아트 규칙, 빈 모양 대체, 모양 쓰는 종류는 스테이지 회전 금지, 높이 = 메시 높이 × `localScale.y`). 같은 문서의 낡은 줄 하나("HighGround·WeakGround·Cliff는 … 프리팹을 만들지 않는다")를 지금 사실로 고친다 | `docs/superpowers/viewDocs/placed-prefab-terrain-guide.md` |
| Tc.3 | 민형님 요청 메모(TP-13, 미전달) | 새 `docs/superpowers/handoffs/2026-10-27-high-ground-edge-shapes-art-request-minhyeong.md` |
| Tc.4 | 설계 §9 "구현 메모"를 쓴다(계획과 다른 점, 남은 것) | 설계 문서 |
| Tc.5 | CLAUDE.md 현재 상태와 이 계획 §7 실행 기록을 쓴다 | — |

- 화면 변화: 보스 L자 모서리 (0,6)·(10,6)에 경사가 나온다. 나머지 8칸은 Center다.
- Unity 확인: §5 Uc.

## 4. 영향 받는 파일 요약

| 파일 | 작성 | Phase |
|---|---|---|
| `VA/TerrainVisual_HighGround.asset` | 우리 | TC-0·TC-c |
| `DBT/TerrainEdgeShape.cs`(새) | 우리 | TC-a |
| `DBT/TerrainVisualDataSO.cs` | 우리 | TC-a |
| `DBS/StageEnvironmentVisualDataSO.cs` | 우리 | TC-a |
| `M/TerrainLook.cs`·`M/FieldTerrainNeighbors.cs`·`M/CombatTerrainLook.cs`(새) | 우리 | TC-b |
| `M/TerrainViewSet.cs`·`M/FieldTerrainPresenter.cs` | 우리 | TC-b |
| `T/TerrainViewSetTests.cs` | 우리 | TC-b |
| 안내 문서·요청 메모·설계·CLAUDE.md | 우리 | TC-c |

바꾸지 않는 것: Core·Buffer·InGame DB·Factory·서비스 키·`ViewStageDataSO`(민형님)·`TerrainVisualTable`·`CombatSceneDisplay`·다른 사람 시험. 공유 메모는 민형님 아트 요청 하나다.

## 5. Unity 확인 (사용자)

| # | 언제 | 확인 |
|---|---|---|
| U0 | TC-0 뒤 | 컴파일, Boot → 아무 전투 진입 성공(DB 생성 오류 없음). 보스 스테이지(Play From Boot `Dev Battle Stage` = 보스, Database)에서 고지대 10칸이 새 상자로 보이고, 그 위 유닛이 묻히지도 뜨지도 않는다 |
| Ua | TC-a 뒤 | 컴파일. `TerrainVisual_HighGround` 인스펙터에 "이웃 모양" 칸 다섯이 보인다. EditMode `TerrainVisualDatabaseTests`·`EnvironmentDefinitionTests`·`DataBaseAssetTests` |
| Ub | TC-b 뒤 | 컴파일. EditMode `TerrainViewSetTests`·`FieldTerrainPresenterTests`·`TerrainOperationalDisplayTests`·`CombatEnvironmentHoverTests`·`TerrainRotationBindingTests`. 보스 스테이지 화면이 U0과 같다 |
| Uc | TC-c 뒤 | 보스 스테이지에서 다음을 본다. ① (0,6)은 서·남 변이, (10,6)은 동·남 변이 경사다(바깥으로 깎임). ② 나머지 8칸은 경사 없는 상자다. ③ 고지대 칸 호버 설명이 그대로 나온다. ④ 고지대 위 이동·강조 테두리가 지금처럼 보인다. **경사가 안쪽(이웃 쪽)을 향하면** 코드가 아니라 FBX 축을 잘못 읽은 것이다. 해당 프리팹 루트를 Y로 돌려 맞추고 알려 준다(설계 TC1) |

## 6. 커밋 묶음 (커밋은 사용자)

- **묶음 1(TC-0, 아트 교체):**
  - `ENV/HeightGround_Center`·`_OneSide`·`_TwoSide.prefab` + `.meta`
  - `Assets/ProjectL/Resources/Map/Environment/BackgroundTest/Models/GroundGroup.fbx.meta`(**꼭**)
  - `ENV/Terrain_Highground.prefab` + `.meta` 삭제
  - `VA/TerrainVisual_HighGround.asset`(prefab·배율)
- **묶음 2(TC-a~TC-c 코드·문서):** 새 `.cs` 넷 + `.meta`, 고친 `.cs` 넷, `T/TerrainViewSetTests.cs`, `VA/TerrainVisual_HighGround.asset`(모양 칸, 묶음 1 뒤라면 같은 파일의 두 번째 변경), 문서.
- 빼는 것: 낭떠러지 연출 파일, `Combat.unity`, FMOD 캐시·카탈로그, `Test.asset`, `Unit_ShotGun.asset`, `TerrainVisual_Cliff.asset`, `GlobalUICanvas.prefab`, 글꼴 에셋.

## 7. 실행 기록

결정(2026-10-27 사용자): "TP-1~TP-13 그대로 하고 TC-0부터 진행해".

### 7.0 TC-0 (2026-10-27, 파일 변경 없음 — 사용자가 이미 고침)

- T0.1: 에셋을 읽어 보니 사용자가 에디터에서 이미 고쳐 두었다. 그래서 TP-1대로 YAML 수정은 건너뛰었다.
  - `prefab` = `HeightGround_Center`(fileID `5212738157284064913`, GUID `ab48d6fe…`)
  - `localScale` = (1, 1, 1)
  - `surfaceHeight` = **0.45**
- **TP-1과 다른 점:** TP-1은 높이 0.777 유지(배율 1.9425)였는데, 사용자는 배율 1에 높이 0.45를 골랐다. 사용자 값을 따른다. 설계 F18(0.4 × 2.5 = 1.0 ≠ 0.777)은 이 값으로 풀렸다.
- 확인할 것: 메시 높이는 FBX상 0.4(40cm, `useFileScale` 켬)라 `surfaceHeight` 0.45와 0.05 차이가 난다. 고지대 위 유닛이 윗면보다 조금 떠 보이면 0.4로 맞추면 된다. 의도한 값이면 그대로 둔다(§5 U0).
- 지운 옛 프리팹(GUID `758d7769…`)을 참조하는 에셋·프리팹·씬·코드는 이제 없다(검색 0건).
- 커밋 묶음 1(§6)은 그대로다. `GroundGroup.fbx.meta`가 여전히 `??`라 꼭 함께 넣어야 한다.
- 다음: 사용자 U0 확인 → TC-a.
- U0 문제 없음(2026-10-27 사용자).

### 7.1 TC-a (2026-10-27, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Ta.1: 새 `DBT/TerrainEdgeShape.cs` + `.meta`(GUID `d4b7103f7d1fe4d2a7dca818fe18b68f`). 열거 여섯이고, 값마다 R0 기준 드러난 쪽을 XML 문서에 적었다. 형제 `TerrainVisualMode`처럼 `public`이다.
- Ta.2: `TerrainEdgeShapePrefabs`(internal sealed)를 `DBT/TerrainVisualDataSO.cs` 맨 앞에 두었다.
  - 칸 다섯, `HasAny`(생성 때 Unity null 비교로 한 번 정함, 빠진 참조도 빈 칸), `Of(shape)`(Center·정의 밖 값은 `ArgumentOutOfRangeException`), 정적 `None`이다.
- Ta.3: 외형 Data 생성자 끝 선택 인자 `edgeShapes = null`(null → `None`), `EdgeShapes`·`UsesEdgeShapes`·`ShapePrefab(shape)`를 넣었다.
  - 검사 "모양을 쓰면 Center 프리팹 필수"는 높이 검사 뒤에 있다.
- Ta.4: SO 칸 다섯은 `surfaceHeight` 아래 머리말 `이웃 모양 (비우면 Center = Prefab, R0)`에 두었다. 칸마다 R0 방향 툴팁이 있다. `prefab` 툴팁도 더했다. `GetData`가 묶음 값을 넘긴다.
- Ta.5: `StageEnvironmentVisualData.TryGetTerrainRotation`을 더했다. 옛 `GetTerrainRotation`은 그대로다.
- 시험: 고칠 곳이 없다. 외형 생성 호출은 끝 선택 인자라 그대로 컴파일된다. SO를 `SerializedObject`로 쓰는 시험(`EnvironmentDefinitionAuthoringTests`·`TerrainVisualDatabaseTests`)은 기존 칸 이름만 쓴다.
- 화면 변화 없음: 아직 아무도 새 값을 읽지 않는다. 운영 에셋은 새 칸이 비어 있다.
- 다음: 사용자 Ua 확인 → TC-b.
- Ua 문제 없음(2026-10-27 사용자).

### 7.2 TC-b (2026-10-27, 컴파일 미확인, 새 시험 없음)

- Tb.1: 새 `M/TerrainLook.cs` + `.meta`(GUID `096d063a…`). `TerrainLookKey`(종류·모양, `IEquatable`·`==`·`ToString`)와 `TerrainLook`(열쇠·회전)이다.
- Tb.2: 새 `M/FieldTerrainNeighbors.cs` + `.meta`(GUID `5258deb5…`). 칸마다 `uint` 종류 비트, `Mark`(범위 밖 예외)·`Has`(범위 밖 false)·`Clear`이다.
- Tb.3: 새 `M/CombatTerrainLook.cs` + `.meta`(GUID `7069a5fa…`).
  - `ShapeOf(int)`: 비트 수로 모양을 정하고, 2개일 때 마주 봄이면 `TwoSideOpposite`다. R0 비트를 최대 세 번 돌려 회전을 찾는다.
  - `Resolve`: §2.4 그대로다. 예외 문구는 TP-9다.
  - 손으로 §2.3 16가지와 보스 대조 다섯 줄을 따라가 맞춰 봤다. 검토 에이전트 대조는 아래처럼 마지막에 한다.
- Tb.4: `M/TerrainViewSet.cs`. 항목 `Kind` → `Key`, 넷째 필수 인자 `lookOf`, 대여·반환 `TerrainLookKey`, 그리기 `(view, source, look)`이다. 선택은 try 안이다.
  - **계획에 없던 한 줄:** 외형 선택이 다른 종류를 돌려주면 예외다(계약 검사, `VisitShown`·`canDisplay`가 종류로 거르는 것과 어긋나지 않게).
- Tb.5~Tb.8: `M/FieldTerrainPresenter.cs`.
  - 풀은 열쇠별이다. `_lookOf`·`_markNeighbor`는 캐시된 대리자이고, `_neighbors`는 mapper 열·행이 다를 때만 새로 만든다.
  - `Render`는 회전 검사 → `FillNeighbors`(VisitShown, 모양 쓰는 종류만 `Mark`) → 그리기 순서다.
  - `Apply`는 `look.Rotation`을 쓴다. 옛 스테이지 회전 읽기는 선택 함수로 옮겼다.
  - `Create(key)`는 `visual.ShapePrefab(key.Shape)`를 쓰고, null이면 계약 오류다. `Instance`는 모양 프리팹에서 기본 회전·크기를 읽는다.
- Tb.9: 클래스·메서드 주석을 고쳤다.
- Tb.10: `T/TerrainViewSetTests.cs` 호출 맞춤. 생성 10곳에 `Plain`, `key.Kind`·`kind.Kind` 셋, `Apply` 도우미·람다 둘에 셋째 인자, using 한 줄, 도우미 `Plain`이다. 단언은 그대로다(diff +19 −15, CRLF 유지).
- **Tb.11 검토 에이전트(TP-12)는 TC-c 뒤로 옮겼다.** 상시 지시 "검토는 작업 묶음이 끝난 뒤 한 번"(메모리 `review-once-at-end`)을 따른 것이다. 맡길 범위는 그대로(§2.3·§2.4 대조, 실패 정리, 풀 열쇠 바뀜, 할당)이다.
- 다른 시험: `FieldTerrainPresenterTests`(호진님 줄 포함)·외형 생성 21곳은 생성자·서명이 그대로라 고치지 않았다.
- 화면 변화 없음: 모양 칸이 비어 모든 종류가 (종류, Center, 스테이지 회전)이다.
- 다음: 사용자 Ub 확인 → TC-c(모양 칸 연결·문서·요청 메모) → 검토 에이전트 하나.
- Ub 문제 없음(2026-10-27 사용자).

### 7.3 TC-c + 묶음 끝 검토 (2026-10-27, 컴파일 미확인, 새 시험 없음)

- Tc.1: `VA/TerrainVisual_HighGround.asset`에 `oneSidePrefab`(OneSide 루트)·`twoSidePrefab`(TwoSide 루트)를 YAML로 넣었고, 나머지 셋은 `{fileID: 0}`으로 두었다.
  - **그 뒤 디스크에서 세 칸이 `HeightGround_Center`로 채워졌다**(에디터 저장으로 보임, 되돌리지 않음). 아래 검토 중간 1, 사용자 결정 대기.
- Tc.2: 안내 문서에 §2.1 "이웃 모양"을 더했다(칸 표·빈 모양 대체·스테이지 회전 금지·아트 규칙·높이 공식·지금 고지대 값·안쪽 모서리 한계). 낡은 GroundShape 줄과 스테이지 회전 원본(`ViewStageDataSO`)도 지금 사실로 고쳤다.
- Tc.3: 민형님 요청 메모 `handoffs/2026-10-27-high-ground-edge-shapes-art-request-minhyeong.md`(미전달). ThreeSide 급함, TwoSideOpposite·FourSide, 크기·피벗·방향 규칙, `GroundGroup.fbx.meta` 미커밋, 격자 데칼용 Rendering Layer(선택)를 적었다.
- Tc.4: 설계 §9 구현 메모.
- **검토(에이전트 하나, TP-12, 정적):** 높음 0, 중간 1, 낮음 6이다. 16가지 표 16/16, 보스 대조 5/5가 맞다. 예상 점수는 약 8.8이다(중간 1을 고치면 약 8.9).
  - **중간 1(결정 대기):** 에셋의 빈 칸이어야 할 셋에 Center가 들어 있어 빈 모양 대체 길(`Resolve`)이 운영에서 쓰이지 않는다.
    - 보스 ThreeSide 8칸은 열쇠 (HighGround, ThreeSide)로 빌려 Center 메시를 R0·R90·R270으로 돌려 그린다.
    - 상자가 대칭이라 모양은 같다. UV·재질 방향은 달라 보일 수 있다(추정). 같은 프리팹이 풀 둘로 나뉜다.
    - 안내 문서·요청 메모의 "빈 칸" 설명과 어긋난다.
  - 반영한 낮음 넷:
    - 낮음 2: 회전 찾기 순회 두 벌을 `TryFind` 하나로 합쳤다(`StageEnvironmentVisualDataSO.cs`, 동작 같음).
    - 낮음 3: R0 표의 코드 원본을 `CombatTerrainLook.CanonicalOf`로 주석을 바로잡았다. 열거 문서·툴팁·안내 문서는 설명이다.
    - 낮음 5: `TerrainVisualData.EdgeShapes`를 private으로 바꿨다(TP-4의 "읽기 둘만"대로). 방향 상수 넷도 private이다.
    - 낮음 6 일부: `TerrainViewSet.VisitShown` 문서에 이웃 표를 적었다.
  - 기록만 한 낮음:
    - 낮음 1: 스테이지 회전 금지 검사가 보이는 원본에서만 걸린다(숨은 원본은 공개될 때 F1). TP-9의 선택이고, 지금 회전 항목은 모두 비어 있다.
    - 낮음 4: 종류가 선택 인자와 열쇠에 두 번 들어가 계약 검사 한 줄이 생겼다. `lookOf`가 (모양, 회전)만 돌려주면 없어지지만 TP-7 서명을 바꾸는 일이라 그대로 둔다.
    - 낮음 6 나머지: 예외 머리말 `[FieldTerrainPresenter]`는 TP-9 문구 그대로다. 이웃 순회는 모양 쓰는 종류가 없어도 돈다(Refresh 때만, 설계 P2 범위).
  - 상시 지시대로 반영 뒤 다시 검토하지 않는다.
- **중간 1 결정(2026-10-27 사용자 "세 칸은 Center 그대로 둬. 임시 값으로 문서에 기록해줘"):** 세 칸은 `HeightGround_Center` 그대로다(Q-C5 A의 "빈 칸"과 다름). 안내 문서 §2.1·요청 메모 §2·설계 §9에 임시 값으로 적었다. 아트가 오면 칸만 바꾼다.
- 다음: 사용자 Uc 확인(§5). Uc ②의 "나머지 8칸"은 돌아간 Center 상자로 보인다.
