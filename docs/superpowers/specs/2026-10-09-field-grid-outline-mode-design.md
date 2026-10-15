# 필드 격자 아웃라인 표시 모드 설계 (2026-10-09)

- **목표:** 전투 필드 표시를 두 모드로 나눈다. 기존 **셀 프리팹 모드**는 그대로 두고, 새 **격자 아웃라인 모드**는 평소 필드에 아무것도 그리지 않다가 이동·공격 대상 지정·배치를 할 때만 격자 Decal과 AreaOutline으로 구역과 색을 보여 준다.
- **상태:** 설계 확정(2026-10-09, F7·F8 추천안 승인), O1~O3 코드 반영(Unity 확인 대기). Task 계획·진행 기록은 View 계획 §7.62에만 적는다.
- **사용자 결정(2026-10-09):**
  - U1. 모드는 `StageFieldViewDataSO`의 값으로 고른다. 기존 동작은 셀 프리팹 모드다.
  - U2. 격자 아웃라인 모드는 셀 프리팹을 만들지 않는다. 바닥은 스테이지 배경(StageViewProp)이 맡는다.
  - U3. 평소에도 보이는 것은 **현재 행위자 칸 아웃라인**뿐이다. 아군 배치 구역은 손패 배치 드래그 중에만, 적 구역·레일은 숨긴다.
  - U4. 격자 Decal은 범위·영향·이동 경로(·차단 벽) 층에 칸이 있거나 배치 드래그 중일 때만 켠다. 단순 호버로는 켜지 않는다.
  - U5. 행동 중 Decal이 캐릭터를 덮는 문제는 일단 허용한다. Quad(Unlit) 방식·Decal Layers는 별도로 정한다.
  - U6. 격자 셰이더는 민형님 `Render/Shader/Decal/GridDecal.shadergraph`를 쓴다. 2026-10-09 사용자 지시로 그 파일에 `_GridOrigin`·`_MaskCenter`를 다시 넣어 격자선을 칸 경계에 맞춘다(View 계획 §7.62).
  - U8(2026-10-09). Q로 이동 경로 지정을 시작한 순간(시작 칸 대기 포함)부터 확정·취소까지도 행동 중으로 보고 격자를 켠다(View 계획 §7.62).
  - U7. F7·F8은 추천안대로 한다(아웃라인 모드 칸 간격은 SO 값, 셀 프리팹 모드는 Decal을 쓰지 않음).

## 1. 지금 구조

| 대상 | 현재 |
|---|---|
| `FieldView` | 셀 풀 대여·배치, 칸 간격 계산(셀 프리팹 크기 × `cellOffsetSize` + padding), 층 상태(`_layers`·호버·정적 색), 칸 색 칠하기(`ApplyColor`), 평면 Raycast 집기, 격자 Decal 표시를 **한 클래스**가 한다 |
| 층 API | `SetLayerCells`·`ClearLayer`·`SetHoverCell`·`ClearHoverCell`·`ApplyStaticHighlights`. 호출자는 `CombatSceneDisplay` 하나다(현재 행위자·범위·영향·차단 벽·이동 경로·호버) |
| 우선순위 | `FieldHighlightLayers.Resolve`: 호버 > 차단 벽 > 영향 > 경로 > 범위 > 현재 행위자 > 정적. 칸 하나에 색 하나 |
| 정적 강조 | `ApplyStaticHighlights`가 칸마다 **색**(아군·적 구역, 레일)을 저장한다. 어떤 종류인지는 남기지 않는다 |
| 격자 Decal | `FieldGridDecal`. 2026-10-08부터 `Build` 때 머티리얼이 있으면 **항상** 켠다 |
| 집기 | 셀 콜라이더가 아니라 수학 평면 Raycast(`TryRayToFieldPlane`). 셀이 없어도 칸 선택은 된다 |
| AreaOutline | Render View 서비스 `IAreaOutlineSystemService.Create()` → `IAreaOutlineHandle.Update(AreaOutlineData)`. 붙은 칸은 바깥 테두리 하나로 합친 **메시**다. 칸 목록이 비면 빈 메시. 원점은 (0,0) 칸 중심의 월드 좌표, `CellSize`는 칸 중심 간격. 핸들 오브젝트는 `RenderRuntimeRoot` 아래(DontDestroyOnLoad)에 있다 |

## 2. 구조 결정

```text
FieldView (MonoBehaviour, 씬 참조·색 설정·집기)
  ├ FieldHighlightState          층 상태의 유일한 원본(순수 C#): 칸별 층 비트, 정적 구역 종류, 호버, 배치 드래그 여부
  └ IFieldHighlightRenderer      상태를 화면에 그리는 방법 하나
      ├ FieldCellColorRenderer   셀 프리팹 모드: 셀 풀 + 칸 색(기존 ApplyColor·우선순위)
      └ FieldOutlineRenderer     격자 아웃라인 모드: 층별 AreaOutline 핸들 + FieldGridDecal
```

- **F1 모드 값:** `StageFieldViewDataSO`에 `fieldDisplayMode`(`FieldDisplayMode { CellPrefab, GridOutline }`)를 둔다. 기본은 `CellPrefab`이라 기존 에셋은 동작이 바뀌지 않는다. enum은 같은 View DB 폴더에 둔다(P5).
- **F2 상태와 그리기 분리(SRP·SSOT):** 어느 칸에 어느 층이 있는지는 `FieldHighlightState` 한 곳에만 있다. 두 렌더러는 상태를 읽어 그리기만 하고 사본을 들지 않는다. `FieldView`의 공개 층 API와 `CombatSceneDisplay` 호출은 **바꾸지 않는다**(O). `FieldHighlightState`는 Unity 없이 테스트한다.
- **F3 정적 강조는 종류로 저장:** 칸마다 색 대신 `FieldStaticZone { None, PlayerZone, EnemyZone, Rail }`을 저장한다. 색은 렌더러가 `FieldView` 직렬화 색에서 고른다. 아웃라인 모드는 아군 구역 칸만 골라 그려야 하므로 종류가 필요하다.
- **F4 렌더러 계약:** `FieldView`가 층 API를 처리하면 상태를 바꾸고 바뀐 것(칸 index 목록, 바뀐 층 비트, 호버·배치 변화)을 렌더러에 알린다. 셀 렌더러는 바뀐 칸만 다시 칠하고(지금과 같은 비용), 아웃라인 렌더러는 바뀐 층의 테두리만 다시 만든다. 구성(`Build`)·해제(`Release`)·파괴(`Dispose`)는 렌더러가 자기 자원(셀 풀, 핸들, Decal)을 책임진다(FW-011).
- **F5 아웃라인 모드 그리기 규칙:**
  - **행동 중** = 범위·영향·경로·차단 벽 층 중 하나라도 칸이 있음, 또는 배치 드래그 중.
  - **평소:** 현재 행위자 층 아웃라인만 그린다. Decal·호버·배치 구역은 숨긴다.
  - **행동 중:** Decal을 켜고, 칸이 있는 층마다 아웃라인 하나, 호버 칸 아웃라인(상태별 색), 배치 드래그 중이면 아군 배치 구역 아웃라인을 그린다.
  - **겹침:** 칸 하나에 색 하나를 고르던 우선순위 대신 모든 층을 그린다. 높은 층이 위에 오도록 층 순서대로 Y를 조금씩 올린다(우선순위 순서는 `FieldHighlightLayers`의 순서 한 곳을 따른다).
  - **핸들:** 층마다 전투 동안 핸들 하나를 유지하고, 숨길 때는 빈 칸 목록으로 갱신한다. 칸이 바뀔 때마다 Create/Dispose하지 않는다. `Release`에서 모두 Dispose한다.
- **F6 배치 드래그 신호:** `CombatSceneDisplay.ShowGhost`/`HideGhost`에서 `FieldView.SetPlacementActive(bool)`를 부른다. `ICombatInteractionFeedback`·Interaction은 바꾸지 않는다. 셀 프리팹 모드에서는 이 값이 화면을 바꾸지 않는다.
- **F7 칸 간격의 출처(U7 확정):** 지금 칸 간격은 셀 프리팹 크기에서 나온다. 아웃라인 모드는 셀을 만들지 않으므로 SO에 `outlineCellSpacing`(칸 중심 간격, XZ)과 `gridLineWidth`(격자선 두께)를 둔다. 모드마다 출처는 하나다. 유닛 위치도 이 간격을 따른다.
  - 다른 안: 셀 프리팹을 크기 기준으로만 측정하고 만들지는 않는다. 같은 값이 프리팹에 숨어 있게 돼 추천하지 않는다.
- **F8 셀 프리팹 모드의 Decal(U7 확정):** 셀 프리팹 모드는 2026-10-08 이전처럼 Decal을 쓰지 않는다. 격자 Decal은 아웃라인 모드 전용이 된다.
- **F9 서비스 연결:** `CombatViewSystem.PostBindService`에서 `global.RenderServiceCommand.ViewGroup.AreaOutlineSystemServices`를 받아 지금 `IColorSystemService`처럼 `CombatSceneDisplay` → `FieldView.Build`로 넘긴다. 새 서비스·모듈 등록은 없다.
- **F10 설정 검사:** `Validate`는 모드별로 한다. `CellPrefab`은 셀 프리팹 필수, `GridOutline`은 격자 Decal 머티리얼·칸 간격·선 두께·아웃라인 두께가 필수다.
- **F11 회전:** Decal과 AreaOutline 모두 월드 XZ 기준이다. 필드 루트가 회전돼 있으면 아웃라인 모드는 경고 한 번 뒤 그리지 않는다(지금 Decal 규칙과 같다).

## 3. 바뀌는 파일

| 파일 | 변경 |
|---|---|
| `DataBaseSystem/Data/View/Table/StageField/Item/StageFieldViewDataSO.cs` | `fieldDisplayMode`, `outlineCellSpacing`, `gridLineWidth`, `outlineThickness`, `outlineYOffset`, 모드별 `Validate` |
| `…/StageField/Item/FieldDisplayMode.cs`(신규) | enum |
| `Display/FieldHighlightState.cs`(신규) | §2 F2·F3 |
| `Display/FieldHighlightPalette.cs`(신규) | `FieldView` 직렬화 색 묶음. 두 표시기가 같은 색 원본을 쓴다 |
| `Display/IFieldHighlightRenderer.cs`(신규) | §2 F4 |
| `Display/FieldCellColorRenderer.cs`(신규) | `FieldView`의 셀 풀·`ApplyColor`를 옮긴다 |
| `Display/FieldOutlineRenderer.cs`(신규) | §2 F5 |
| `Display/FieldGridDecal.cs` | 선 두께를 셀 크기 대신 값으로 받는다 |
| `Display/FieldView.cs` | 상태·렌더러 선택·`SetPlacementActive`. 집기·좌표 변환은 그대로 |
| `Display/FieldHighlightLayers.cs` | 아웃라인 렌더러가 쓸 층 순서를 같은 곳에 둔다 |
| `Combat/CombatSceneDisplay.cs`, `Combat/CombatViewSystem.cs` | AreaOutline 서비스 전달, Ghost 표시 때 배치 신호 |
| `ViewGroup/StageFieldView/Item/StageFieldView_Default.asset` | 새 값(모드는 `CellPrefab` 유지) |
| 테스트 `Tests/EditMode/Framework/` | `FieldHighlightState`, 행동 중 판정, 설정 검사 |

Render 쪽 파일(AreaOutline, 셰이더, 렌더러 설정)은 바꾸지 않는다.

## 4. SOLID·SSOT·FW 점검

- **S:** 상태(`FieldHighlightState`), 그리기(렌더러 둘), 씬 연결·집기(`FieldView`)가 각자 하나의 이유로 바뀐다. 지금 `FieldView`가 갖고 있던 셀 풀·칠하기는 셀 렌더러로 나간다.
- **O:** 새 표시 방식은 렌더러를 하나 더하면 된다. 호출자(`CombatSceneDisplay`·Interaction)는 그대로다.
- **L:** 두 렌더러 모두 "상태를 받은 그대로 그린다, 해제는 여러 번 불려도 안전하다"를 지킨다.
- **I/D:** 아웃라인 렌더러는 `IAreaOutlineSystemService`만 안다. `FieldView`는 렌더러 인터페이스만 본다. 구현이 둘이라 인터페이스가 정당하다(지침 §27).
- **SSOT:** 층 상태는 `FieldHighlightState`, 색은 `FieldView` 직렬화 값, 층 순서는 `FieldHighlightLayers`, 모드·간격·두께는 View DB SO 한 곳이다. 모드마다 칸 간격 출처는 하나다(F7).
- **FW:** FW-011 대여 자원(AreaOutline 핸들·Decal 머티리얼 인스턴스)은 `Release`/`Dispose`에서 반환한다. 새 서비스 등록·Builder 변경은 없다. 씬 검색(`Find`)을 쓰지 않는다.
- **경계:** Buffer·Core를 읽지 않는다. 값은 지금처럼 `CombatSceneDisplay`가 넘긴다.

## 5. 범위 밖·알려진 차이

- **캐릭터 덮임(U5):** 행동 중 Decal 상자 안의 캐릭터 아래쪽에 격자선이 그려진다. 해결안(Unlit Quad, Decal Layers, 얇은 상자)은 따로 정한다.
- **격자선과 아웃라인 어긋남(해소, 2026-10-09):** 셰이더에 `_GridOrigin`·`_MaskCenter`를 다시 넣어 `FieldGridDecal`이 필드 모서리·중심을 넘긴다. 마스크 반경은 민형님 머티리얼의 `_RadiusMaskSize`(현재 10)를 따른다.
- **AreaOutline 점선·흐름 미적용:** `AreaOutlineData`의 `IsDashed`·`DashCount`·`DashFill`·`FlowDirection`·`FlowSpeed`는 지금 `AreaOutlineHandle.Update`가 머티리얼에 넣지 않는다(메시와 `MainColor`만 넣는다). 실선으로 그려진다. 필요하면 민형님 요청 메모로 남긴다(Render 영역).
- **적 구역·레일:** 아웃라인 모드에서는 보이지 않는다(U3). 필요해지면 렌더러 규칙만 바꾸면 된다.
- **RecordInputOnly:** 입력 표시 경로가 같으므로 모드 차이 외의 차이는 없다.
