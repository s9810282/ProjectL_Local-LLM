# 전투 이동 경로 화살표(선과 이어진 화살촉·꺾임 이음) 설계 (2026-10-29)

- **상태:** 설계 결정 완료. **Q-MA1~Q-MA3 모두 A(2026-10-29 사용자 "권장안으로 설계", §3), 설계에서 정한 것 P1~P8 그대로(2026-10-29 사용자 "그대로, 계획 써줘", §4).** Task 계획은 [movement-path-arrow-plan](../plans/2026-10-29-combat-movement-path-arrow-plan.md)(MA-a 값 → MA-b 그리기 → MA-c 문서·공유·검토, TP-1~TP-10 그대로). **MA-a·MA-b 코드·에셋 완료, MA-c 문서·공유 완료(2026-10-29, 컴파일 미확인) — 구현 메모 §8.**
- **요청(사용자, 2026-10-29):** "경로가 점선인데 화살표와 같은 방식으로 바꿀 수 있는지 RenderSystem 측 가능성 검토" → "꺾이는 부분에 대한 검토도 추가" → "마지막에만 화살촉을 두는 건 맞는데, 프리팹이 아니라 경로랑 선으로 자연스럽게 이어져야" → "권장안으로 설계".
- **점검 기준:** 사용자 7기준 — SSOT, SOLID, JMH Framework 지침·FW 계약, 기능 변경의 독립성, 캡슐화, C#·Unity 권장 구조, 무의미한 If·Tick 검사(§6).
- **바꾸지 않는 것:** GlobalSystem(RenderSystem의 `DrawPointLineSystem`·`MeshSystem`·`ColorSystem`), 경로 입력(`MovementDraft`·`CombatMovementInteraction`), 경로 상태(`FieldHighlightState.MovementPath`), 경로 색(Combat 씬 `FieldView.movementDraftColor`), CellPrefab 모드, 칸 테두리·채우기·격자 데칼.
- **바로잡을 것:** 지금 경로선은 점선이 아니라 실선이다(F2). 화면에서 점선으로 보인 것은 경로 아래 이동 범위 칸·호버 칸 테두리(AreaOutline 점선)다.

## 1. 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | 지금 이동 경로선은 GlobalSystem `DrawPointLineSystem`의 선 핸들(`LineHandle`)이 그린다. `FieldOutlineRenderer`가 이동 초안 층 슬롯에 선 핸들 하나를 두고, 격자 높이가 다른 구간마다 추가 선 핸들을 만든다(`AddPart(line: true)`). 점은 칸 가운데, 높이는 `movementPathYOffset` + 격자 높이 + 슬롯 순서 × 0.002다. 이 줄들은 민형님 `b7c79107`("이동경로 라인 표시")이다. | `FieldOutlineRenderer.cs:174-175`·`:296-331`·`:476-489` |
| F2 | 선 재질은 Render DB 열쇠 `_LineColor` → `Render/Shader/View/CustomShader/Line/LineShader.mat`(URP Lit, 불투명, 텍스처 없음)이라 실선이다. 이 재질은 GlobalSystem 안(`DrawPointLineSystem.InitData`)에서만 읽힌다. 전투 View는 Render DB 재질 표를 읽지 않는다. | `DrawPointLineSystem.cs:61`, `LineColor.asset` |
| F3 | MultiLine 띠는 구간마다 따로 만든 사각형이고, 구간끼리 잇는 처리가 없다. 꺾인 칸 바깥 모서리에 두께 절반 크기의 빈 홈이 생긴다. 지금 에셋의 경로 두께는 0.4(10-29 값 나눔 뒤 사용자 값)라 홈이 0.2로 보인다. | `MeshHightLevelBuilder.CreateMultiLine`, `StageFieldView_Default.asset` |
| F4 | `DrawPointLine`의 끝점 이펙트는 회전이 늘 고정(`Quaternion.identity`)이라 화살촉이 경로 방향을 보지 못한다. 쓰려면 Render DB `EffectType` 등록도 필요하다. | `LineHandle.cs:84-85` |
| F5 | MeshSystem이 만드는 모양은 Cube·Circle·Cylinder·SingleLine·MultiLine·GridCube·GridOutline 일곱이다. 등록 함수는 `MeshSystem.Initialize` 안의 private이라 밖에서 새 모양(화살표·이음 메시)을 더할 수 없다. | `MeshSystem.cs:26-35`·`:113-121` |
| F6 | Circle을 꼭짓점 3개로 만들면 채운 정삼각형이다. 중심 기준이고 첫 꼭짓점은 +Z, 법선은 위, 위에서 보이는 감김이다. 반지름 1이면 꼭지 z = 1, 밑변 z = −0.5, 밑변 너비 = √3이다. | `CreateCircle`, `CalcCirclePoint` |
| F7 | MultiLine 사각형의 감김은 구간 방향의 부호와 상관없다(`(끝−시작) × 오른쪽`이 늘 아래 → 위를 봄). 같은 축을 되돌아가는 짧은 구간도 그대로 보인다. | `CalcLineCorners` |
| F8 | 선례: 공격 궤적 `FieldTrajectoryLine`이 MeshSystem MultiLine + ColorSystem을 View에서 조합한다. 재질은 View DB(`trajectoryMaterial`), 좌표는 필드 셀 루트 로컬, 구성·해제는 필드 수명이다. 그때 Framework 작업자 의견으로 `DrawPointLineSystem`은 고치지 않고 하위 Mesh·Color를 쓰기로 했다. | `FieldTrajectoryLine.cs`, [공격 궤적 설계](2026-10-19-combat-attack-trajectory-design.md) TJ1 |
| F9 | 경로는 칸이 겹치지 않는다. 지나온 칸에 다시 들어가면 `MovementDraft.Accept`가 그 칸까지 자른다. 걸음은 상하좌우 한 칸이라 칸마다 들어온 방향·나가는 방향이 하나씩이고, 꺾임은 늘 직각이다. U턴·교차는 없다. | `MovementDraft.cs:36-52` |
| F10 | 경로를 그리는 동안 도착 칸에는 이동 고스트가 선다. | `CombatMovementPlanningPresenter.cs:82` |
| F11 | 격자 높이(지형만, 고지대)가 다른 칸에서는 구간을 나누고, 구간마다 다음 구간 첫 칸까지 그 구간 높이로 그린다(타일 높이 계획 §8.7 H1). 세로 구간은 MultiLine 띠의 너비 방향이 x축으로 고정돼(위 방향이면 `cross(up, forward)`) 쓸 수 없다. | `FieldOutlineRenderer.ApplyPath`, `CalcLineCorners` |
| F12 | `IDrawPointLineSystemService`는 `CombatDisplayServices.PointLines` → Factory `CombatSceneDisplayFactory.cs:77`·`:178` → `CombatSceneDisplay` → `FieldView.Build` → `FieldOutlineRenderer`로 내려간다. 경로선 밖에 쓰는 곳이 없고, 시험 참조도 없다(`Assets/Tests` 검색 0). | 저장소 검색 |
| F13 | 지금 선 렌더러는 그림자 설정이 기본값(드리움·받음 켬)이고 정렬 순서도 기본값이다. 궤적은 그림자를 끈다. | `LinePool.CreateHandle`, `FieldTrajectoryLine.Build` |

## 2. 설계

### MA1. 경로 그리기 부품 하나: `FieldMovementPathLine`

- `Display/Field/FieldMovementPathLine.cs`(새, `FieldTrajectoryLine` 꼴, F8). 띠·꺾임 이음·화살촉을 이 부품 하나가 그린다.
- **나누는 선:** "무엇을·언제·어느 높이에" 그리나는 지금처럼 `FieldOutlineRenderer`가 정한다(이동 초안 칸 모으기 `Collect`, 행동 중 판단, 높이 구간 나누기, 슬롯 순서 높이). 부품은 받은 구간만 그리고 전투·유닛·상태를 모른다.
- 입력은 구간 목록(구간마다 필드 로컬 점, 높이 포함)과 색, 그리고 화살촉 방향(경로 마지막 두 칸의 차, 상하좌우 한 칸 — F9)이다. 정확한 서명은 Task 계획에서 정한다.
- 좌표는 필드 셀 루트 로컬이다. 점 = `FieldGridMapper.CellCenterToLocal(칸)` + 위 × (격자 높이 + `movementPathYOffset` + 슬롯 순서 높이)이고, 지금 선의 높이 식과 같다(GridOutline은 회전하지 않은 필드에서만 그림).
- 경로 칸이 하나뿐이면(걸음 없음) 띠·화살촉을 숨긴다. 지금 선이 점 둘 미만이면 안 보이는 것과 같다.

### MA2. 꺾임 이음: 같은 띠 안에서 메운 직각 모서리 (Q-MA2 A)

- 꺾인 칸 가운데 C에서, 들어온 방향을 dIn, 나가는 방향을 dOut, 두께 절반을 h라 하면, 점 C 하나를 셋으로 바꾼다: **C + dIn·h → C → C − dOut·h**.
  - 들어온 띠가 C를 h만큼 지나고(되돌아오는 짧은 구간은 같은 띠 안에 겹침), 나가는 띠가 C보다 h만큼 뒤에서 시작한다. 둘을 합치면 바깥 홈이 메워진 직각(마이터) 모서리가 된다.
  - 모든 구간이 가로·세로 방향이라(F9) 대각선으로 삐져나오는 곳이 없고, 되돌아가는 구간도 보인다(F7). 겹친 곳은 같은 재질·같은 색의 불투명이라 보이지 않는다.
  - 메시가 더 생기지 않는다(같은 MultiLine).
- 방향이 같은 칸의 점은 뺀다(직선 구간은 사각형 하나).
- 이 규칙의 원본은 순수 함수 하나다(점 목록 → 이음을 넣은 점 목록). 배열은 점 개수마다 한 번 만들어 다시 쓴다(궤적 `PointsFor`와 같음).
- 둥근 모서리(Q-MA2 B)는 꺾인 칸마다 원판 메시가 따로 필요해 풀이 들어 뺐다.

### MA3. 화살촉: 같은 재질의 삼각형 메시 하나 (Q-MA1 A)

- MeshSystem Circle(꼭짓점 3, 반지름 1, 중심 0) 메시를 구성 때 **한 번** 만든다(F6). 경로가 바뀌면 메시는 그대로 두고 화살촉 오브젝트의 위치·회전·배율만 바꾼다.
  - 배율: x = 너비 / √3, z = 길이 / 1.5, y = 1.
  - 회전: 화살촉 방향(경로 마지막 걸음 방향, 필드 로컬)을 +Z로.
  - 위치: 꼭지 − 방향 × 길이 / 1.5(메시 꼭지가 z = 1이라 배율 뒤 꼭지까지 길이 / 1.5).
- **꼭지 자리 = 도착 칸 가운데 + 방향 × 꼭지 자리 값**(Q-MA1 A, 기본 0 = 칸 가운데). 도착 칸에 이동 고스트가 서서 가운데가 가려질 수 있어(F10), 값으로 앞뒤를 고칠 수 있게 한다.
- 높이는 도착 칸의 격자 높이 + `movementPathYOffset` + 슬롯 순서 높이로 띠와 같다.
- **띠 끝은 꼭지 − 방향 × 길이 / 2**(화살촉 가운데)로 줄인다. 띠가 꼭지 밖으로 나오지 않고, 화살촉이 띠 끝을 덮는다. 그 자리의 삼각형 너비는 너비 / 2라서 너비 ≥ 두께 × 2이면 띠가 삼각형 안에 든다(검사, MA5).
- 띠와 화살촉이 겹치는 곳은 같은 평면이지만 같은 재질·같은 색·같은 법선이라 화소가 같아 깜빡임이 보이지 않는다.
- 높이 구간이 나뉘어 마지막 구간이 도착 칸 하나뿐이면(낮은 칸 → 고지대 도착) 마지막 구간은 띠가 없고, 화살촉만 고지대 윗면에 놓인다. 앞 구간 띠는 지금처럼 도착 칸까지 낮은 높이로 이어진다(F11).

### MA4. 재질·색: 한 곳 (Q-MA3 A)

- **재질:** View DB `StageFieldViewData`의 "Movement Path" 묶음에 새 칸 `movementPathMaterial`(GridOutline 모드면 필수)을 둔다. 기본 에셋에는 지금 선과 같은 `LineShader.mat`을 연결한다(화면 그대로). 띠·화살촉이 이 재질 하나를 쓴다.
  - 재질에 `_BaseColor`가 없으면 구성 때 예외로 멈춘다(`DrawPointLineSystem.InitData`와 같은 조건).
  - `LineShader.mat`은 Render DB `_LineColor`도 가리키지만, 그것은 GlobalSystem 선 시스템의 재질이고 경로는 더 쓰지 않으므로 같은 사실을 두 곳에 두는 것이 아니다.
- **색:** 지금 팔레트의 이동 경로 색(`FieldView.movementDraftColor` → `FieldHighlightPalette.MovementDraft`) 하나를 ColorSystem `MainColor` 덮어쓰기로 띠·화살촉에 같은 값으로 넣는다. 색이 바뀔 때만 넣는다.

### MA5. 값과 검사

- 그대로 쓰는 것: `movementPathThickness`(띠 두께), `movementPathYOffset`(띠·화살촉 높이).
- 새 칸(모두 "Movement Path" 묶음, 필드 로컬 단위):
  - `movementPathArrowLength`(화살촉 길이, 기본 0.5)
  - `movementPathArrowWidth`(화살촉 밑변 너비, 기본 0.8)
  - `movementPathArrowTipOffset`(꼭지 자리, 기본 0 = 도착 칸 가운데, 양수 = 진행 방향 앞)
- 검사(GridOutline 모드, `ValidateGridOutline`):
  - 재질 필수, 길이 > 0, 너비 ≥ 두께 × 2(MA3).
  - |꼭지 자리| ≤ 칸 간격(`outlineCellSpacing`의 작은 쪽) / 2 — 꼭지가 도착 칸 안.
  - 길이 / 2 − 꼭지 자리 < 칸 간격의 작은 쪽 — 마지막 걸음의 띠가 남는다.
- 기본값은 지금 두께 0.4·칸 간격 1에 맞췄다(꼭지 = 칸 가운데, 밑변 = 칸 경계, 띠 끝 = 칸 가운데에서 0.25 뒤).

### MA6. 소유·수명

- `FieldOutlineRenderer`가 이동 초안 층 슬롯에서 선 핸들 대신 이 부품을 쓴다. `Build`에서 부품을 구성하고 `Release`에서 거둔다.
- 높이 구간마다 띠가 하나씩 필요하다. 부품이 띠 오브젝트를 구간 수만큼 처음 필요할 때 만들어 `Release`까지 다시 쓴다(지금 추가 선 핸들과 같은 수명). 쓰지 않는 띠는 숨긴다. 화살촉은 하나다.
- 칸 목록·색·높이 표 판이 지난번과 같으면 다시 그리지 않는다(렌더러의 지금 비교 `Slot.SameAs`·`ForgetDrawn` 그대로). 매 프레임 하는 일은 없다.
- 렌더러에서 선 갈래를 걷는다: 슬롯의 `LineHandle`, `AddPart(line: true)`, `ReleaseSlot`의 선 핸들 정리, `Apply`의 `DrawPointLineData` 갈래.

### MA7. 쓰지 않게 되는 서비스 걷기

- `IDrawPointLineSystemService`를 `FieldOutlineRenderer` 생성자·`FieldView.Build`·`EnsureRenderer`·`CombatSceneDisplay`·`CombatDisplayServices.PointLines`·Factory 두 줄(`CombatSceneDisplayFactory.cs:77`·`:178`)에서 뺀다(F12, 쓰는 곳이 없어짐).
- Factory 줄은 민형님 영역의 우리 연결 줄이라 공유 문서에 적는다. GlobalSystem `DrawPointLineSystem` 자체는 그대로다.

## 3. 정할 것 — 결정 완료(2026-10-29 사용자 "권장안으로 설계")

| # | 질문 | 결정 |
|---|---|---|
| Q-MA1 | 화살촉 꼭지 자리 | **A** — 도착 칸 가운데, 앞뒤는 값(`movementPathArrowTipOffset`)으로 조절. (B: 밑변을 칸 가운데에 두어 꼭지가 칸 가장자리 쪽) |
| Q-MA2 | 꺾임 모양 | **A** — 같은 띠 안에서 메운 직각 모서리(MA2). (B: 둥근 모서리, 꺾인 칸마다 원판 메시·풀) |
| Q-MA3 | 재질 | **A** — 지금 `LineShader.mat`(URP Lit)을 View DB 칸에 연결, 화면 그대로. (B: 빛 영향 없는 Unlit 새 재질) |

## 4. 설계에서 정한 것 (사용자 확인 대상)

| # | 정한 것 | 이유 |
|---|---|---|
| P1 | 부품은 `FieldOutlineRenderer`가 갖는다(궤적처럼 `FieldView`가 갖지 않음). | 경로는 강조 층의 하나라 "언제·어느 높이에"(행동 중 판단·높이 구간·슬롯 순서 높이)가 렌더러에 있다. 그 판단을 옮기지 않는다. |
| P2 | 높이 구간 규칙은 지금 그대로(구간마다 띠 하나, 다음 구간 첫 칸까지 그 높이). | 세로 구간은 MultiLine으로 그릴 수 없다(F11). 내려가는 쪽이 높은 높이로 다음 칸까지 떠 보이는 것은 지금 성질이다. |
| P3 | 화살촉 메시는 구성 때 한 번 만들고 오브젝트 위치·회전·배율만 바꾼다. | Circle은 방향이 고정이라(F6) 회전은 오브젝트가 맡아야 하고, 경로가 바뀔 때 메시를 다시 만들 필요가 없다. |
| P4 | 띠 끝 = 화살촉 가운데, 검사 너비 ≥ 두께 × 2. | 띠가 꼭지 밖으로 나오지 않고 삼각형 안에 든다(MA3). |
| P5 | 그림자·정렬 순서는 지금 선과 같게 기본값. | Q-MA3 A(화면 그대로)와 맞춘다. 궤적은 끄지만 경로는 바꾸지 않는다. |
| P6 | 방향이 같은 칸의 점은 뺀다. | 직선 구간을 사각형 하나로. 결과 화면은 같고 메시가 작아진다. |
| P7 | `IDrawPointLineSystemService` 사슬을 걷는다(MA7). | 쓰지 않는 의존을 남기지 않는다. 시험 참조가 없어 시험 변경도 없다. |
| P8 | 화살촉 기본값 길이 0.5·너비 0.8·꼭지 자리 0. | 지금 두께 0.4·칸 간격 1 기준(MA5). |

## 5. 바꾸는 곳 (예상, 권장안 기준)

| 파일 | 바뀜 | 작성자 |
|---|---|---|
| `Display/Field/FieldMovementPathLine.cs`(새, + `.meta`) | 띠·이음·화살촉 부품(MA1~MA3·MA6) | 우리 |
| `Display/Field/FieldOutlineRenderer.cs` | 선 핸들 갈래를 부품 호출로, `_pointLines` 삭제, 머리 주석 | 우리 + 민형님 줄(`b7c79107`: `:7`·`:15`·`:44`·`:96`·`:173-178`·`:448-454`·`:480-490`·`:508`·`:738`) |
| `Display/Field/FieldView.cs` | `Build`·`EnsureRenderer` 인자에서 선 서비스 삭제 | 우리 |
| `Display/CombatSceneDisplay.cs` | 선 서비스 필드·생성자 인자 삭제 | 우리 |
| `CombatDisplayServices.cs` | `PointLines` 삭제 | 우리 |
| Factory `CombatSceneDisplayFactory.cs:77`·`:178` | 선 서비스 줄·인자 삭제(연결 줄) | 우리(민형님 영역) |
| `StageFieldViewDataSO.cs` | 칸 넷(재질·길이·너비·꼭지 자리)·읽기·검사 | 우리 |
| `StageFieldView_Default.asset` | 재질 `LineShader.mat` 연결·값 셋 | — |

- 코드와 에셋은 한 커밋이어야 한다(재질이 비면 GridOutline 검사가 DB 생성을 막음).
- 시험: 참조가 없어 바꾸지 않는다(F12). View 작업이라 새 시험은 쓰지 않는다.
- 문서·공유: 격자 아웃라인 설계 머리말, 폴더 배치 안내 `Display/Field/` 줄, 민형님 Factory 공유 §38(Factory 두 줄 + `FieldOutlineRenderer`의 민형님 줄), 호진님 필드 설정 표 메모 추가 절(새 칸 넷). 모두 미전달로 둔다.

## 6. 7기준 점검 (예상, 알려진 문제 기준)

| 기준 | 지금(경로선 부분) | 설계 뒤 | 근거 |
|---|---|---|---|
| SSOT | 7.5 | 9.0 | 경로 모양·재질·색이 부품 하나·재질 칸 하나·색 하나. 꺾임 규칙은 순수 함수 하나. 지금은 경로 재질이 GlobalSystem 안의 Render DB 열쇠에 묶여 View에서 고를 수 없다. |
| SOLID | 7.5 | 9.0 | 렌더러는 무엇·언제, 부품은 어떻게 그리나. 렌더러의 선·테두리 두 갈래(`LineHandle`/`Handle`)가 하나로 준다. |
| FW | 9.0 | 9.0 | GlobalSystem 무변경, 하위 MeshSystem·ColorSystem 공개 계약만 쓴다(궤적 선례). Factory는 연결 줄만. |
| 독립성 | 7.5 | 9.0 | 경로 모양을 바꿀 때 부품 하나·SO 칸만 고친다. 지금은 화살촉·이음을 넣을 자리가 없다. |
| 캡슐화 | 8.5 | 9.0 | 부품은 internal이고 렌더러만 부른다. |
| C#·Unity | 8.0 | 9.0 | 메시·배열 재사용, 화살촉 메시는 한 번. 꺾임 홈(두께 0.4에서 0.2)이 사라진다. |
| If·Tick | 9.0 | 9.0 | 경로가 바뀔 때만 다시 채운다(그대로). |
| **합** | **약 8.1** | **약 9.0** | |

- 남는 것(낮음): 높이가 바뀌는 칸의 구간 나눔은 지금 성질 그대로(P2). 화살촉 크기가 칸 간격 검사에 묶인다(MA5).

## 7. Phase (확인 뒤 Task 계획에서 자세히)

- **착수 조건(2026-10-29 작업 트리 확인):**
  - 경로선 값 나눔(같은 날, 스테이지 전)이 `StageFieldViewDataSO.cs`·`StageFieldView_Default.asset`·`FieldOutlineRenderer.cs`를 이미 바꿔 두었다. MA-a·MA-b가 같은 파일을 고치므로 값 나눔을 먼저 커밋하거나 함께 커밋한다.
  - 다른 세션의 지형 타일 교체 작업(TR, [설계](2026-10-29-combat-terrain-tile-replace-design.md))이 `FieldSurfaceHeights`·`FieldGridMapper`를 고치는 중이다. 이 설계는 두 파일을 고치지 않고 `OverlayHeightAt`·`CellCenterToLocal`만 읽는다. MA-b 착수 때 두 입구가 그대로인지 다시 본다.

- **MA-a 값:** SO 칸 넷·읽기·검사, 기본 에셋 재질 연결·값. 아직 아무도 읽지 않아 화면 변화 없음. Unity 확인: 부트 DB 생성 통과.
- **MA-b 그리기:** 부품(띠·이음·화살촉), 렌더러 선 갈래 교체, 서비스 사슬·Factory 두 줄 걷기. Unity 확인: 직선·꺾임 경로, 화살촉 방향 넷, 고지대를 오르내리는 경로, 경로 한 칸(숨김), 경로 지우기.
- **MA-c 문서·공유·묶음 끝 검토:** 격자 아웃라인 설계 머리말, 폴더 배치 안내, 민형님·호진님 공유, 새 에이전트 하나로 7기준 검토.

## 8. 구현 메모 (2026-10-29, MA-a·MA-b 코드 완료, 컴파일 미확인)

실행 기록 원본은 [계획](../plans/2026-10-29-combat-movement-path-arrow-plan.md) §5다. 여기에는 설계와 비교해 알아 둘 것만 적는다.

- **만든 것:**
  - 모양 규칙 `Display/Field/FieldMovementPathShape`(정적, 할당 없음): `Fill`(직선 점 빼기 P6 · 꺾임 점 `C + dIn·h → C → C − dOut·h` MA2 · 마지막 구간 끝 줄이기), `TrimAlong`(꼭지 자리 − 길이 / 2), `ArrowPose`(MA3).
  - 그리기 `Display/Field/FieldMovementPathLine`: 띠는 높이 구간 번호마다 오브젝트 하나(`FieldMovementPath_{번호}`)를 처음 필요할 때 만들고, 화살촉 `FieldMovementPathArrow`는 `Build`에서 Circle 꼭짓점 셋 메시를 한 번 넣는다(P3). 색은 바뀐 오브젝트에만 넣는다.
  - `FieldOutlineRenderer`가 이동 초안 층 슬롯을 이 부품으로 그린다(P1). 다시 그릴지는 슬롯의 칸·색 기억 그대로다. `CanReuse`에 경로 재질 비교를 더했다.
  - SO "Movement Path" 칸 넷과 검사 `ValidateMovementPath()` 하나(MA5), 기본 에셋 `LineShader.mat`·0.5·0.8·0(P8).
  - 선 서비스 사슬(`FieldView`·`CombatSceneDisplay`·`CombatDisplayServices`·Factory 세 줄)을 걷었다(MA7·P7). GlobalSystem `DrawPointLineSystem`·서비스 키는 그대로 남는다.
- **설계와 다르게 한 것:**
  - 검사는 `ValidateGridOutline` 끝에 줄로 넣지 않고 함수 하나로 묶었다(옛 두께·높이 검사도 옮김, 조건은 같음).
  - 설계 §5 표는 Factory를 `:77`·`:178` 두 줄로 적었지만, using `:9`도 함께 지웠다(계획 Tb.7대로).
  - 기본 에셋은 LF다(계획의 CRLF 적음은 낡음).
- **알아 둘 것(설계대로 남은 성질):**
  - 고지대에서 낮은 칸으로 내려와 끝나는 경로는 앞 구간 띠가 도착 칸 가운데까지 높은 높이로 이어지고, 화살촉은 낮은 도착 칸 높이에 놓인다(P2).
  - 값은 필드 표시를 만들 때 한 번 읽으므로 다음 Combat 진입부터 반영된다.
  - 화살촉 메시 감김은 위에서 시계 방향 = 앞면으로 코드를 대조했다. Unity에서 화살촉이 전혀 안 보이면 이 가정부터 본다.
- **확인:** 컴파일·재생 확인은 사용자 몫이다(새 시험 없음, 시험 변경 없음, MSBuild 안 돌림). 묶음 끝 검토 결과는 계획 §5.3에 적는다.
- **공유(모두 미전달):** 민형님 Factory 공유 §38(Factory 세 줄·`b7c79107` 줄), 호진님 필드 설정 표 메모 "이동 경로 화살표 칸 넷" 절. 격자 아웃라인 설계 머리말·폴더 배치 안내 `Display/Field/` 줄도 고쳤다.

### 8.1 묶음 끝 검토 반영 (2026-10-29 사용자 "검토에서 나온거 권장안대로 진행해", 컴파일 미확인)

- **M1(결함):** 끝 줄이기를 "점이 둘 이상인 마지막 구간"에 건다(`FieldMovementPathLine.Show`). 높이가 바뀐 칸에서 끝나는 경로도 앞 구간 띠가 화살촉 가운데에서 멈춘다. 화살촉 높이는 지금처럼 도착 칸 높이다(P2의 높이 차이는 남음).
- **M2(독립성):** 경로 재질을 Render DB `LineColor`와 함께 쓰던 `LineShader.mat`에서 떼어, 새 `Prefabs/Combat/Materials/MovementPath.mat`(지금 작업 트리의 `LineShader.mat`을 이름만 바꿔 복제 — 화면 같음)를 기본 에셋에 연결했다. Q-MA3 A("지금 재질 연결")를 이 복제본으로 바꾼 것이다.
- **L3:** 화살촉 메시 반지름·꼭짓점 수를 `FieldMovementPathShape.ArrowMeshRadius`·`ArrowMeshSegments` 한 곳에 두고, 길이·너비 상수와 그리는 쪽이 그 값을 쓴다.
- **L4:** 렌더러의 재질 사본을 걷고 부품이 `Uses(config)`로 답한다.
- **L8:** 검사에 "화살촉 길이 ≤ 칸 간격"을 더했다(MA5 보충).
- **L9:** `Show`가 읽기 전용 목록 형식을 받는다.
- **그대로:** L5(방향 두 번 계산, 값 같음)·L6(화살촉 +Z가 GlobalSystem 메시 순서에 기댐)·L7(그림자 기본값, P5)·L10(렌더러 크기)·L11(색만 바뀌어도 메시 다시 만듦·닿지 않는 방어 검사).

## 9. 높이 규칙 보탬 (2026-10-29, 겹친 강조 2단계 계획 §7.6)

- 사용자(10-29): "색 겹침 현상 호버 타일 뿐만 아니라 경로 + 경로 끝 화살표도 포함해줘".
- 원인: 띠·화살촉은 불투명(깊이 씀)이고 높이가 경로 값 + 이동 초안 층의 테두리 칸 번호 × 0.002였습니다. 그보다 뒤 칸 테두리(호버 테두리 포함)는 더 높아 깊이 시험을 지나, 반투명 점선이 띠·화살촉 위에 덧칠됐습니다.
- 고침: 경로 높이 = 경로 값 + 모든 테두리 칸 수 × 0.002(마지막 테두리 칸보다 한 단계 위). View DB는 경로 높이 ≥ 칸 테두리 높이를 검사합니다. P5(정렬 순서 없음)는 그대로입니다.
- 투명 큐로 옮겨 정렬 순서로 맨 뒤에 그리는 안은 버렸습니다. 깊이를 쓰지 않는 투명 물체(이동 중 반투명 유닛·고스트)보다 늦게 그려져 그 위를 덮기 때문입니다.
