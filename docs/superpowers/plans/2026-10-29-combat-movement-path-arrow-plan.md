# 전투 이동 경로 화살표(선과 이어진 화살촉·꺾임 이음) Task 계획 (2026-10-29)

- **설계:** [movement-path-arrow-design](../specs/2026-10-29-combat-movement-path-arrow-design.md)(MA1~MA7).
- **결정(2026-10-29 사용자):**
  - "권장안으로 설계" → Q-MA1~Q-MA3 모두 A. 꼭지는 도착 칸 가운데이고 값으로 조절한다. 꺾임은 같은 띠 안에서 메운 직각 모서리다. 재질은 지금 `LineShader.mat`을 View DB 칸에 연결한다.
  - "그대로, 계획 써줘" → 설계에서 정한 것 P1~P8 그대로.
- **진행 규칙(CLAUDE.md·메모리):**
  - View 작업이라 새 시험을 쓰지 않는다. 전투 View라 MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적고, 컴파일·재생 확인은 사용자가 Unity에서 한다.
  - Phase가 끝나면 멈추고 보고한다. 검토 에이전트는 묶음 끝(MA-c)에 한 번만 돌린다.
  - 커밋은 사용자가 한다.

## 0. 착수 조건·기준 판

- **기준 판:** HEAD `3be9ad77` + 작업 트리.
- **같은 파일 겹침 ①:** 경로선 값 나눔(2026-10-29, 스테이지 전)이 `StageFieldViewDataSO.cs`·`StageFieldView_Default.asset`·`FieldOutlineRenderer.cs`를 이미 바꿔 두었다. 이 계획도 세 파일을 고친다. 값 나눔을 먼저 커밋하는 것을 권하고, 아니면 이 작업과 한 커밋으로 넣는다(값 나눔만 따로 떼면 hunk를 나눠야 함).
- **같은 파일 겹침 ②:** 다른 세션의 지형 타일 교체 작업(TR)이 `FieldSurfaceHeights.cs`·`FieldGridMapper.cs`를 고치는 중이다. 이 계획은 두 파일을 고치지 않고 `OverlayHeightAt`·`CellCenterToLocal`·`StepToLocal`만 읽는다. MA-b 착수 때 세 입구가 그대로인지 다시 본다.
- **다른 사람 줄(고친 뒤 공유):** 민형님 `b7c79107`("이동경로 라인 표시")의 줄이 다음 파일에 있다.
  - `FieldOutlineRenderer.cs` `:7`·`:15`·`:44`·`:96`·`:173-178`·`:448-454`·`:480-490`·`:508`·`:738`
  - `CombatSceneDisplay.cs` `:11`·`:73`
  - `FieldView.cs` `:7`·`:126-127`
  - `Collect`의 이동 초안 갈래(`:448-454`)는 그대로 둔다. 나머지는 선 핸들을 걷으면서 바뀌거나 지워진다.
- **Factory:** `CombatSceneDisplayFactory.cs` `:9`(using)·`:77`·`:178`은 민형님 영역의 우리 연결 줄이다(작성은 우리). 바꾸면 공유 문서에 적는다.
- **시험:** `IDrawPointLineSystemService`·`CombatDisplayServices`·`FieldOutlineRenderer`·`new CombatSceneDisplay(`를 부르는 시험은 없다(`Assets/Tests` 검색 0). 시험 변경 없음.

## 0.1 계획에서 정한 것 (사용자 확인 대상)

| # | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 모양 규칙(직선 점 빼기·꺾임 점 끼우기·띠 끝 줄이기·화살촉 자세)은 정적 클래스 `FieldMovementPathShape`에, 그리기는 `FieldMovementPathLine`에 둔다. | 궤적의 `FieldTrajectorySegments`(점선 규칙) + `FieldTrajectoryLine`(그리기)과 같은 꼴이다. 설계 MA2의 "규칙 원본 = 순수 함수 하나"를 지킨다. |
| TP-2 | 렌더러가 칸을 필드 로컬 점(격자 높이 + 경로 높이 + 슬롯 순서 높이 포함)으로 바꿔 부품에 넘긴다. 부품은 점·방향·색만 받는다. | 높이 구간·슬롯 순서 높이는 렌더러가 이미 안다(설계 P1). 부품은 격자·상태를 모른다. |
| TP-3 | 다시 그릴지는 렌더러가 경로 칸 전체·색으로 판단한다(지금 `Slot.SameAs`). 경로 슬롯은 추가 핸들(`Parts`)을 쓰지 않는다. | 높이 구간은 칸 목록에서 정해지므로 칸·색이 같으면 그림도 같다. 높이 표가 바뀌면 `ForgetDrawn`이 이미 다시 그리게 한다. |
| TP-4 | 띠 오브젝트는 구간 번호마다 처음 필요할 때 만들어 `Release`까지 다시 쓴다. 쓰지 않는 띠는 렌더러를 끈다. | 지금 추가 선 핸들과 같은 수명이다(설계 MA6). |
| TP-5 | `CanReuse`에 경로 재질 비교 한 줄을 더한다. | 칸 채우기 재질과 같은 이유(한 씬에서 렌더러를 다시 쓸 때 재질이 같아야 함)다. |
| TP-6 | 재질의 `_BaseColor` 검사는 부품 `Build`가 한다. DB 검사는 재질이 있는지만 본다. | 재질 속성은 렌더 쪽 사실이라 DB가 보지 않는다(넉백 미리보기 계획 TP-3 선례). 조건은 `DrawPointLineSystem.InitData`와 같다. |
| TP-7 | SO 새 칸은 "Movement Path" 묶음의 `movementPathYOffset` 바로 뒤에 재질 → 길이 → 너비 → 꼭지 자리 순서로 둔다. 에셋 YAML도 같은 순서다. | 묶음이 하나로 읽힌다. |
| TP-8 | 오브젝트 이름은 띠 `FieldMovementPath_{구간 번호}`, 화살촉 `FieldMovementPathArrow`다. 부모는 필드 셀 루트다. | 하이어라키에서 찾기 쉽다. 궤적(`FieldTrajectory`)과 같은 부모다. |
| TP-9 | 화살촉 메시(Circle, 꼭짓점 3, 반지름 1, 중심 0)는 `Build`에서 한 번 넣는다. 보이지 않을 때는 렌더러를 끈다. | 설계 P3. |
| TP-10 | 그림자·정렬 순서는 설정하지 않는다(기본값). | 설계 P5(지금 선과 같게). |

## 1. Phase MA-a — 값 (화면 변화 없음)

| Task | 파일 | 할 일 |
|---|---|---|
| Ta.1 | `DataBaseSystem/Data/View/Table/StageField/Item/StageFieldViewDataSO.cs` | ① "Movement Path" 묶음 `movementPathYOffset` 뒤에 칸 넷을 둔다(TP-7): `Material movementPathMaterial`, `[Min(0f)] float movementPathArrowLength = 0.5f`, `[Min(0f)] float movementPathArrowWidth = 0.8f`, `float movementPathArrowTipOffset = 0f`. 툴팁에는 뜻·단위(필드 로컬 = 월드, 셀 루트 크기 1), "띠와 화살촉이 같은 재질·색", 꼭지 자리의 부호(양수 = 진행 방향 앞)를 적는다. ② 읽기 넷 `MovementPathMaterial`·`MovementPathArrowLength`·`MovementPathArrowWidth`·`MovementPathArrowTipOffset`. ③ `ValidateGridOutline` 끝에 검사: 재질 필수, 길이 > 0, 너비 ≥ 두께 × 2, `|꼭지 자리| ≤ min(칸 간격) / 2`, `길이 / 2 − 꼭지 자리 < min(칸 간격)`(설계 MA5). 문구는 파일 규칙대로 영어. ④ 묶음 머리말·`movementPathThickness` 툴팁의 "경로선"을 "경로 띠"로 고치고, 칸 간격 툴팁은 그대로 둔다. |
| Ta.2 | `DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset` | `movementPathYOffset: 0.02` 뒤에 `movementPathMaterial: {fileID: 2100000, guid: 57ea059c63ab8ba41b274f17749421f0, type: 2}`(`LineShader.mat`), `movementPathArrowLength: 0.5`, `movementPathArrowWidth: 0.8`, `movementPathArrowTipOffset: 0`. 이 파일은 CRLF라 Edit 도구로 고친다(넣은 뒤 줄 끝 확인). |

- 아무도 새 값을 읽지 않으므로 화면은 그대로다.

**Ua (사용자 Unity 확인):**
1. 컴파일 오류가 없다.
2. `StageFieldView_Default` 인스펙터 "Movement Path"에 칸 넷이 보이고, 재질 = `LineShader`, 0.5·0.8·0이다.
3. Combat 진입·경로 그리기가 지금과 같다.
4. 너비를 0.7로 바꾸면(두께 0.4 × 2 = 0.8 미만) 부트 DB 생성에서 막힌다. 확인 뒤 0.8로 되돌린다.

## 2. Phase MA-b — 그리기 (화면에서 화살표 경로)

| Task | 파일 | 할 일 |
|---|---|---|
| Tb.1 | 새 `ViewSystem/Combat/Display/Field/FieldMovementPathShape.cs` (+ `.meta`) | `internal static class`. ① `Fill(IReadOnlyList<Vector3> run, bool trimEnd, float trimAlong, float halfThickness, List<Vector3> result)`: 방향이 같은 점을 빼고(설계 P6), `trimEnd`면 마지막 점을 "마지막 점 + 마지막 방향 × `trimAlong`"으로 바꾼 뒤, 꺾인 점 C마다 `C + dIn·h`, `C`, `C − dOut·h`를 넣는다(설계 MA2). 방향은 가로 성분(x·z)으로만 비교한다(구간 안 점은 높이가 같음). ② `ArrowPose(Vector3 destination, Vector3 direction, float tipOffset, float length, float width, out Vector3 position, out Quaternion rotation, out Vector3 scale)`: 꼭지 = 도착 + 방향 × `tipOffset`, 위치 = 꼭지 − 방향 × 길이 / 1.5, 회전 = `Quaternion.LookRotation(방향, Vector3.up)`, 배율 = (너비 / √3, 1, 길이 / 1.5)(설계 MA3). ③ 띠 끝 줄이는 거리 `TrimAlong(tipOffset, length) = tipOffset − length / 2`. 클래스 주석에 MultiLine 감김이 방향 부호와 무관하다는 근거(설계 F7)와 "할당 없음, 결과 목록은 부르는 쪽 것"을 적는다. |
| Tb.2 | 새 `ViewSystem/Combat/Display/Field/FieldMovementPathLine.cs` (+ `.meta`) | `internal sealed class`, `FieldTrajectoryLine` 꼴(설계 MA1). 생성자 `(Transform parent, StageFieldViewData config, IMeshSystemService meshes, IColorSystemService colors)`: 재질·두께·화살촉 값 셋을 읽고, 재질이 없으면 `ArgumentException`. `Build()`: 재질 `_BaseColor` 검사(TP-6), 화살촉 오브젝트(MeshFilter·MeshRenderer, 렌더러 끔) → `Register` → `CircleCreateData(이름, Vector3.zero, 1f, 3, Color.white)` 한 번 → 색 핸들. 실패하면 만든 것을 거두고 전파한다. `Show(IReadOnlyList<List<Vector3>> runs, int runCount, Vector3 lastStep, Color color)`: 구간마다 `FieldMovementPathShape.Fill`(마지막 구간만 `trimEnd`) → 점 수가 2 이상이면 그 번호의 띠에 `MultiLineCreateData(이름, 점 배열, 두께, 1f, Color.white)`, 아니면 그 띠를 끈다. 띠가 없으면 만든다(TP-4). 쓰지 않는 띠는 끈다. 화살촉은 마지막 구간의 마지막 점(줄이기 전)과 `lastStep`으로 자세를 놓고 켠다. 색은 바뀐 오브젝트에만 `MainColor`로 넣는다. `Clear()`: 모두 끈다. `Release()`: 색 → 메시 → 오브젝트 순으로 각각 거두고 실패를 모아 한 번 던진다(여러 번 불려도 안전). 점 배열은 점 수마다 한 번 만들어 다시 쓴다(`PointsFor`, MultiLine은 배열 길이를 점 수로 씀). |
| Tb.3 | `ViewSystem/Combat/Display/Field/FieldOutlineRenderer.cs` | ① 생성자 인자 `IDrawPointLineSystemService pointLines`·필드 `_pointLines`·using `DrawPointLineSystem`을 지운다. 대신 필드 `_path = new FieldMovementPathLine(cellsRoot, config, meshes, colors)`와 재질 기억(TP-5)을 둔다. ② `Build`: 이동 초안 슬롯은 AreaOutline 핸들을 만들지 않고 `_path.Build()`를 부른다(실패 정리 경로에 포함). ③ `Refresh`의 갈래: `slot.Layer == MovementDraft`면 `ApplyPath`. ④ `ApplyPath`: `slot.SameAs(_scratch, color)`면 끝. 아니면 지금과 같은 높이 구간 나누기(다음 구간 첫 칸까지)로 구간마다 로컬 점 목록을 채운다. 점 = `_mapper.CellCenterToLocal(칸) + Vector3.up × (격자 높이 + _pathYOffset + 슬롯 순서 × LayerHeightStep)`. 칸이 둘 이상이면 `lastStep = _mapper.StepToLocal(마지막 칸 − 그 앞 칸)`으로 `_path.Show(…)`, 아니면 `_path.Clear()`. 그 뒤 `slot.Remember`. 구간 점 목록은 재사용 목록으로 둔다(할당 없음). ⑤ `Apply`의 `LineHandle` 갈래, `AddPart`의 `line` 인자, `ReleaseSlot`의 선 핸들 정리, `Slot.LineHandle`을 지운다. ⑥ `ReleaseHandles`가 `_path.Release()`를 부른다(실패 모음에 포함). ⑦ `CanReuse`에 `config.MovementPathMaterial == _pathMaterial`. ⑧ 클래스 머리 주석(`:15`·`:26`·`:29`·`:30`)을 "이동 경로는 `FieldMovementPathLine`이 띠·꺾임 이음·화살촉으로 그림, 정렬 순서는 불투명이라 기본값"으로 고친다. `ApplyPath` 문서 주석도 고친다. |
| Tb.4 | `ViewSystem/Combat/Display/Field/FieldView.cs` | `Build`·`EnsureRenderer`에서 `IDrawPointLineSystemService pointLines` 인자·null 검사·using을 지우고, `FieldOutlineRenderer` 생성 호출을 맞춘다. |
| Tb.5 | `ViewSystem/Combat/Display/CombatSceneDisplay.cs` | 생성자 인자 `pointLines`·필드 `_pointLines`·using을 지우고, `_field.Build(…)` 호출을 맞춘다. |
| Tb.6 | `ViewSystem/Combat/CombatDisplayServices.cs` | 생성자 인자 `pointLines`·속성 `PointLines`·using·`From`의 `render.ViewGroup.DrawPointLineSystemServices` 줄을 지운다. |
| Tb.7 | `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` | `:9` using, `:77` 지역 변수, `:178` 인자를 지운다(연결 줄, 민형님 공유). |

- **확인할 호출처(컴파일 맞춤):**
  - `FieldView.Build`를 부르는 곳은 `CombatSceneDisplay` 하나다.
  - `new CombatSceneDisplay(`는 Factory 하나다.
  - `new CombatDisplayServices(`는 `From` 하나다.
  - `new FieldOutlineRenderer(`는 `FieldView.EnsureRenderer` 하나다.
  - 착수 때 다시 검색한다.
- **남는 것:** GlobalSystem `DrawPointLineSystem`·`RenderViewGroup.DrawPointLineSystemServices`는 그대로다(다른 쓰임을 위해 남음).

**Ub (사용자 Unity 확인):**
1. 컴파일 오류가 없다.
2. 직선 경로: 띠가 도착 칸까지 이어지고, 끝에 같은 색 삼각형 화살촉이 진행 방향을 본다. 띠가 꼭지 밖으로 나오지 않는다.
3. 꺾인 경로(두께 0.4): 꺾인 칸 바깥 모서리에 홈이 없고 직각으로 꺾인다. 여러 번 꺾어도 그렇다.
4. 화살촉 방향 넷(상하좌우)이 모두 맞다.
5. 경로가 한 칸뿐이면(유닛 칸만) 띠·화살촉이 없다. 경로를 지우거나 이동 모드를 끝내면 모두 사라진다.
6. 고지대를 오르는 경로·내리는 경로·고지대에서 끝나는 경로: 띠는 지금처럼 구간마다 이어지고, 화살촉은 도착 칸 높이에 놓인다.
7. 낮·밤 모두 색·명암이 지금 경로선과 같다(같은 재질).
8. 칸 테두리·채우기·호버·궤적은 지금과 같다.
9. 하이어라키에 경로용 `DrawPointLine` 오브젝트가 더 생기지 않고, 필드 셀 루트 아래 `FieldMovementPath_0`·`FieldMovementPathArrow`가 보인다.
10. 설정 에셋 꼭지 자리·길이·너비를 바꾸면 다음 전투 진입부터 반영된다. 확인 뒤 되돌린다.

## 3. Phase MA-c — 문서·공유·묶음 끝 검토

| Task | 파일 | 할 일 |
|---|---|---|
| Tc.1 | `docs/superpowers/specs/2026-10-09-field-grid-outline-mode-design.md` 머리말 | 2026-10-29 한 줄: 이동 경로는 `DrawPointLine` 대신 `FieldMovementPathLine`(띠·직각 이음·화살촉), 정책 원본은 화살표 설계. |
| Tc.2 | `docs/superpowers/viewDocs/combat-view-folder-guide.md` `Display/Field/` 줄 | 예에 `FieldMovementPathLine`·`FieldMovementPathShape`를 더한다. |
| Tc.3 | `docs/superpowers/handoffs/2026-10-15-combat-scene-display-factory-framework-share.md` 새 절(쓰기 직전 끝 번호를 다시 읽음, 지금 §37 다음) | Factory `:9`·`:77`·`:178` 삭제, `FieldOutlineRenderer`·`CombatSceneDisplay`·`FieldView`의 민형님 줄(§0)이 바뀐 점, 경로가 더는 `DrawPointLineSystem`을 쓰지 않음. 미전달. |
| Tc.4 | `docs/superpowers/handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md` 새 절 | 새 SO 칸 넷(뜻·기본값·검사), 재질 연결. 미전달. |
| Tc.5 | 설계 문서 새 §8 "구현 메모" | 설계·계획과 다르게 한 것, 남은 것. |
| Tc.6 | 검토 에이전트 하나 | MA-a·MA-b 변경을 7기준으로 검토한다(새 에이전트, 우리 결론 없이 코드만 줌). 결과는 이 계획 §5에 적고, 반영할지는 사용자가 정한다. |

## 4. 커밋 묶음 (커밋은 사용자)

- **한 커밋에 함께 넣을 것:**
  - 새 `.cs` 둘 + `.meta`: `FieldMovementPathShape`, `FieldMovementPathLine`.
  - SO 코드와 기본 에셋. 코드만 있고 에셋 재질이 비면 GridOutline 검사가 부트 DB 생성을 막는다.
- MA-a만 따로 커밋해도 컴파일된다(값만 생김). MA-b는 MA-a 위에서만 컴파일된다.
- 경로선 값 나눔(§0 ①)이 그때까지 커밋 전이면 같은 커밋에 넣는다.

## 5. 실행 기록

### 5.1 MA-a 코드·에셋 완료 (2026-10-29 사용자 "그대로, MA-a 진행" — TP-1~TP-10 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

- **`StageFieldViewDataSO.cs`:**
  - "Movement Path" 묶음 머리말을 "이동 경로 띠·화살촉"으로 바꿨다. 두께·높이 툴팁의 "경로선"을 "경로 띠"(높이는 "띠와 화살촉")로 고쳤다.
  - `movementPathYOffset` 뒤에 칸 넷을 두었다: `movementPathMaterial`, `movementPathArrowLength`(0.5), `movementPathArrowWidth`(0.8), `movementPathArrowTipOffset`(0).
  - 읽기 넷을 더했다. 두께·높이 읽기 문서 주석도 "띠"로 고쳤다.
  - 검사는 새 함수 `ValidateMovementPath()` 하나로 모았다. 옛 두께·높이 검사 두 줄도 이 함수로 옮겼고, `ValidateGridOutline` 끝에서 부른다. 조건은 계획 Ta.1 ③ 그대로이며, 두 줄 주석으로 근거(띠 끝 = 화살촉 가운데, 마지막 걸음의 띠 길이)를 남겼다.
- **`StageFieldView_Default.asset`:** `movementPathYOffset` 뒤에 `LineShader.mat`(`57ea059c…`, `fileID 2100000`) 연결과 0.5·0.8·0을 넣었다.
- **계획과 다른 점:**
  - 계획 Ta.2는 에셋이 CRLF라고 적었지만, 지금 작업 트리는 LF다(경로 두께를 0.4로 바꿀 때 Unity가 다시 저장함, HEAD 블롭도 LF). 그대로 LF로 넣었다.
  - 검사를 `ValidateGridOutline` 끝에 줄로 넣지 않고 함수 하나로 묶었다. 파일의 `ValidateGridDecal` 꼴을 따랐고, 조건은 같다.
- **확인한 것:**
  - GridOutline 칸(`outlineCellSpacing`)을 가진 에셋은 기본 에셋 하나뿐이라, 재질 필수 검사로 막힐 다른 에셋이 없다.
  - 남아 있는 시험에서 이 설정을 만드는 곳은 없다(`PullAndMarkPresentationTests`는 가짜 렌더러의 `CanReuse`만 가짐).
- **화면 변화 없음:** 새 값은 아직 아무도 읽지 않는다.
- **같은 파일에 섞인 것:** 두 파일 모두 경로선 값 나눔(같은 날, 스테이지 전) hunk가 함께 있다. 커밋 묶음은 §0 ①·§4대로.
- **Ua 확인 대기:** §1의 네 항목. → **Ua 문제 없음(2026-10-29 사용자).**

### 5.2 MA-b 코드 완료 (2026-10-29 사용자 "Ua 문제 없음, MA-b", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

- **착수 확인:** 읽는 입구 셋(`FieldSurfaceHeights.OverlayHeightAt`·`FieldGridMapper.CellCenterToLocal`·`StepToLocal`)은 다른 세션(TR) 변경 뒤에도 그대로다.
- **새 파일 둘(+ `.meta`, 이웃 파일처럼 CRLF):**
  - `Display/Field/FieldMovementPathShape.cs`(GUID `fc88756e…`): `Fill`(직선 점 빼기·꺾임 점 `C + dIn·h → C → C − dOut·h`·마지막 구간 끝 줄이기), `TrimAlong`(꼭지 자리 − 길이 / 2), `ArrowPose`(꼭지 = 도착 + 방향 × 꼭지 자리, 위치 = 꼭지 − 방향 × 길이 / 1.5, 배율 (너비 / √3, 1, 길이 / 1.5)). 방향은 가로 성분으로만 보고, 할당하지 않는다.
  - `Display/Field/FieldMovementPathLine.cs`(GUID `783a5121…`): `Build`(머티리얼 `_BaseColor` 검사 TP-6, 화살촉 오브젝트·Circle 꼭짓점 셋 메시 한 번), `Show(runs, runCount, lastStep, color)`(구간마다 띠, 점이 둘 미만이면 띠를 끔, 남는 띠는 끔, 화살촉 자세·색), `Clear`, `Release`(오브젝트마다 Color → Mesh → 오브젝트, 실패 모음). 띠는 구간 번호마다 처음 필요할 때 만든다(TP-4). 점 배열은 점 수마다 한 번 만든다. 색은 바뀐 오브젝트에만 넣는다. 그림자·정렬 순서는 설정하지 않는다(TP-10).
- **`FieldOutlineRenderer.cs`:**
  - 생성자 인자 `IDrawPointLineSystemService`·필드·using을 지웠다. 부품 `_path`와 경로 머티리얼 기억을 두고, `CanReuse`에 머티리얼 비교 한 줄을 더했다(TP-5). `_pathThickness`·`_runCells`는 지웠다(두께는 부품이 읽음).
  - `Build`: 이동 초안 층 슬롯은 테두리 핸들을 만들지 않고, `_path.Build()`를 부른다(실패 정리에 포함).
  - `Refresh`: 갈래 판정을 `IsPath(slot)`(이동 초안 층) 하나로 했다.
  - `ApplyPath`: 칸·색이 같으면 끝. 칸이 둘 미만이면 `Clear`. 아니면 지금 높이 구간 규칙(다음 구간 첫 칸까지)대로 구간마다 로컬 점을 채운다(점 = `CellCenterToLocal` + 위 × (격자 높이 + 경로 높이 + 층 순서 높이), TP-2). 마지막 걸음 `StepToLocal(마지막 칸 − 앞 칸)`과 함께 `Show`한다. 다시 그릴지는 슬롯의 칸·색 기억 그대로다(TP-3).
  - `Apply`의 선 갈래, `AddPart`의 `line` 인자, `ReleaseSlot`의 선 핸들, `Slot.LineHandle`을 지웠다. `ReleaseHandles`가 `_path.Release()`를 부른다. 머리·`ApplyPath`·`Slot` 주석을 고쳤다.
- **선 서비스 연결 걷기:**
  - `FieldView.Build`·`EnsureRenderer` 인자·null 검사·using.
  - `CombatSceneDisplay` 생성자 인자·필드·using·`_field.Build` 호출.
  - `CombatDisplayServices` 생성자 인자·속성·using·`From`의 한 줄. 줄 끝 하나가 LF로 바뀌었던 것을 CRLF로 되돌렸다.
  - Factory `CombatSceneDisplayFactory.cs`의 using·지역 변수·인자 한 줄(연결 줄, 민형님 공유는 MA-c).
  - 남은 참조는 GlobalSystem과 `ServiceKeys`의 서비스 키뿐이다(그대로 둠).
- **다른 사람 줄:** 민형님 `b7c79107` 줄 — `FieldOutlineRenderer`(using·머리 주석·필드·생성자 검사·`Build` 선 핸들·`Apply` 선 갈래·`Slot.LineHandle`; `Collect` 이동 초안 갈래는 그대로), `CombatSceneDisplay` `:11`·`:73`, `FieldView` `:7`·`:126-127`. MA-c 공유에 적는다.
- **계획과 다른 점 없음.**
- **알아 둘 것(지금 성질 그대로, 설계 P2):** 고지대에서 낮은 칸으로 내려와 끝나는 경로는, 앞 구간 띠가 도착 칸 가운데까지 높은 높이로 이어지고 화살촉은 낮은 도착 칸 높이에 놓인다(띠가 화살촉 위에 떠 보일 수 있음).
- **커밋 묶음:** 새 `.cs` 둘 + `.meta`, MA-a(SO·에셋)와 한 커밋이다(MA-b는 MA-a 위에서만 컴파일). `CombatSceneDisplay.cs`에는 다른 세션(TR)의 `BindTiles` 한 줄이 우리 `_field.Build` 줄 바로 아래에 있어, 따로 커밋하면 hunk를 나눠야 한다.
- **Ub 확인 대기:** §2의 열 항목. 화살촉이 전혀 안 보이면 메시 감김(뒷면) 문제일 수 있으니 알려 주면 된다(Circle 감김은 위에서 시계 방향 = 앞면으로 대조함).

### 5.3 MA-c 문서·공유·묶음 끝 검토 (2026-10-29 사용자 "수정했고 MA-c 진행해", 코드 변경 없음)

- **Ub:** 사용자가 Ub 결과를 따로 알리지 않고 MA-c를 지시했다. 그 직전에 사용자가 고친 컴파일 오류(천명님 `PullAndMarkPresentationTests.cs:424`, `CombatFieldEventPorts` 새 인자 `appearanceBlocksStart`)는 다른 세션의 지형 타일 교체 TR-c2 변경 때문이고, 이 작업과는 관계없다. 사용자가 `() => false` 한 인자를 더했다(시험 내용 그대로).
- **Tc.1:** 격자 아웃라인 설계 머리말에 2026-10-29 줄(이동 경로는 `FieldMovementPathLine`이 띠·직각 이음·화살촉으로 그림, 정책 원본은 화살표 설계).
- **Tc.2:** 폴더 배치 안내 `Display/Field/` 줄 예에 `FieldMovementPathLine`(그리기)·`FieldMovementPathShape`(모양 규칙).
- **Tc.3:** 민형님 Factory 공유 **§38**(쓰기 직전 끝 번호 §37 확인). Factory 세 줄(using·지역 변수·인자), `b7c79107` 줄이 바뀐 곳(`FieldOutlineRenderer`·`CombatSceneDisplay`·`FieldView`), 우리 `CombatDisplayServices` 줄, GlobalSystem 그대로, 하위 부품 사용 방식, §34 정렬 순서 제안은 경로에 더 해당 없음. 같은 Factory 파일의 TR-c2 줄은 그쪽 절에 적힌다고 적었다. **다른 세션(TR-d)도 이 문서에 절을 더할 수 있어 그쪽은 §39부터다.** 미전달.
- **Tc.4:** 호진님 필드 설정 표 메모 새 절 "이동 경로 화살표 칸 넷"(뜻·기본값·검사·에셋·반영 시점, SO 모드 도구는 이 칸을 읽지 않음 — `Tool/` 검색 0). 미전달.
- **Tc.5:** 설계 §8 구현 메모와 머리 상태 줄.
- **Tc.6 묶음 끝 검토(새 에이전트 하나, 코드만 줌, 7기준): 약 9.46** — SSOT 8.5·SOLID 9.75·FW 9.75·독립성 9.25·캡슐화 9.75·C#/Unity 9.5·If/Tick 9.75. 설계 예상 9.0보다 높다. 높음 0·중간 2·낮음 9. 중간 둘은 코드로 다시 확인했다.
  - **M1(중간, 결함):** 마지막 높이 구간이 한 칸이면(고지대에서 바닥으로 내려와 끝나거나 바닥에서 고지대로 올라가 끝남) 그 구간은 띠가 꺼지고, 끝 줄이기는 늘 마지막 구간에만 걸려 앞 구간 띠가 줄지 않는다. 앞 구간 띠는 도착 칸 가운데까지 구간 높이로 이어져, 내려와 끝나면 0.4 폭 띠가 화살촉 꼭지까지 위를 덮는다. 같은 원인으로 구간 경계 칸에서 꺾이면 그 칸은 이음이 없다(두 구간 높이가 달라 원래 끊김). §5.2 "알아 둘 것"은 높이 차이만 적었고 줄이기가 빠지는 것은 적지 않았다. 제안: 끝 줄이기를 "점이 둘 이상인 마지막 구간"에 건다.
  - **M2(중간, 독립성):** 경로 재질 `LineShader.mat`은 Render DB `LineColor`와 같은 에셋이고 `_EMISSION`이 켜져 있다. ColorSystem은 `_BaseColor`만 덮어써 보이는 색은 "팔레트 색 + 재질 발광"이다. 경로 모습을 바꾸려면 GlobalSystem 쪽 재질 에셋을 고치게 된다. 작업 트리에서 이 재질 색·발광이 초록으로 바뀌어 있다(스테이지 전, 사용자 변경). 제안(데이터만): 전용 재질을 복제해 `movementPathMaterial`에 연결.
  - **낮음:** L3 화살촉 메시 값(반지름 1·꼭짓점 3)과 거기서 나온 상수(1.5·√3)가 Line·Shape 두 곳, L4 렌더러 `_pathMaterial`은 `CanReuse` 비교만 하는 사본, L5 마지막 방향을 렌더러·Shape가 따로 계산(값 같음), L6 화살촉 +Z는 GlobalSystem 내부 `CalcCirclePoint` 순서에 기댐(공개 계약 아님), L7 그림자 기본값(Lit 불투명이 드리우고 받음, 궤적은 끔, 설계 P5로 정한 것), L8 길이 검사가 넉넉함(한 칸보다 긴 화살촉 허용), L9 `Show`가 바꿀 수 있는 목록 형식을 받음, L10 렌더러가 경로 점 조립까지 맡음(780줄), L11 색만 바뀌어도 띠 메시를 다시 만듦·닿지 않는 방어 검사 둘.
  - **문제 없던 것(코드 확인):** 화살촉·띠가 위에서 보이는 감김, 꼭지·밑변·띠 끝 자리, 꺾임 이음과 연속 지그재그, 중간 구간은 늘 점 둘 이상, `ShownColor`는 해제 뒤 낡지 않음, 점 배열 재사용 안전, `Release` 여러 번 안전·`Build` 실패 정리·파괴 순서, 선 서비스 사슬 GlobalSystem 밖 참조 0, 매 프레임 일 없음.
  - **반영 여부는 사용자가 정한다(코드 변경 없음).**

### 5.4 묶음 끝 검토 반영 (2026-10-29 사용자 "검토에서 나온거 권장안대로 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **M1:** `FieldMovementPathLine.Show`가 끝을 줄일 구간 `trimRun`을 "마지막 구간이 점 둘 이상이면 그 구간, 아니면 그 앞 구간"으로 고른다(중간 구간은 늘 점 둘 이상). 화살촉 높이는 그대로 도착 칸 높이.
- **M2:** 새 재질 `Prefabs/Combat/Materials/MovementPath.mat`(+ `.meta`, GUID `c18a30c4…`) — 지금 작업 트리의 `LineShader.mat`(사용자가 초록으로 바꾼 값)을 `m_Name`만 바꿔 복제. `StageFieldView_Default.asset`의 `movementPathMaterial`을 이것으로 바꿨다. `LineShader.mat` 작업 트리 변경(사용자)은 그대로 두었다.
- **L3:** `FieldMovementPathShape.ArrowMeshRadius`(1)·`ArrowMeshSegments`(3) — 길이·너비 상수가 반지름에서 나오고, `ArrowPose` 위치가 반지름을 곱하며, `Build`의 `CircleCreateData`가 두 값을 읽는다.
- **L4:** `FieldOutlineRenderer._pathMaterial` 삭제, `CanReuse`는 `_path.Uses(config)`.
- **L8:** `ValidateMovementPath`에 "화살촉 길이 > 칸 간격이면 예외"(기본 0.5 ≤ 1이라 통과), 길이 툴팁에 한 줄.
- **L9:** `Show(IReadOnlyList<IReadOnlyList<Vector3>> runs, …)` — 렌더러의 `List<List<Vector3>>`가 공변 변환으로 그대로 넘어간다.
- **그대로:** L5·L6·L7·L10·L11(설계 §8.1).
- **문서:** 격자 아웃라인 설계 머리말·민형님 공유 §38·호진님 메모(재질·길이 검사) 고침, 설계 §8.1.
- **커밋:** 새 재질 + `.meta`는 기본 에셋과 한 커밋이다(없으면 재질 참조가 깨져 GridOutline 검사가 부트 DB 생성을 막음). 앞서 스테이지에 올린 판은 이 반영 전이다.
- **확인할 것(Unity):** 컴파일, 경로 색·명암이 반영 전과 같음, 고지대에서 내려와 끝나는 경로에서 띠가 화살촉 가운데에서 멈춤, 화살촉 길이를 1.1로 바꾸면 부트 DB 생성에서 막힘(확인 뒤 되돌림).
