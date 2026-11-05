# 전투 고지대 View 표시 — 설계 초안 (2판)

작성: 2026-10-20. 상태: **결정 완료(2026-10-20 사용자): Q-HG1~Q-HG6 모두 A, P1~P5 그대로.** Task 계획: [high-ground-view-plan](../plans/2026-10-20-combat-high-ground-view-plan.md). 코드 변경 없음.

사용자 요청 흐름:
1. Core의 고지대 작업을 확인했다(§1 F1·F2).
2. "그건 그대로 두고 View 측 처리 진행하자". "그것"은 고지대 툴팁의 "벽 무시" 줄과 Core 미구현이 서로 다른 점이다. Core 요청은 적지 않고 툴팁 문구도 바꾸지 않는다.
3. 1판(평면 테두리 권장)을 본 뒤 사용자가 방향을 정했다. ① 필드에 설정된 지형에서 고지대 정보를 받아 고지대 프리팹을 만들어 배치한다. ② 이동할 때 고지대로 갈 수 있는지와, 단순 이동에서 높이 이동까지. 이 순서를 0~4단계로 다시 썼다.

1판의 "평면 테두리"는 버리고, 실제 높이가 있는 블록과 높이 이동으로 간다.

## 1. 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | Core 고지대 규칙은 셋이다. 저지대 → 고지대 이동 +2 AP, 저지대에서 온 공격 피해 70%, 저지대 → 고지대 넉백 불가. 셋 다 Core 미리보기(이동 범위·비용·예상 체력)와 결과에 이미 들어 있다. **"고지대로 갈 수 있나"도 Core가 정한다**(AP가 모자라면 이동 범위에 그 칸이 없다). View는 그 범위·결과를 보여 줄 뿐 판단하지 않는다(P6). | `Domain/Movement/EnvironmentTerrainRules.cs`, `MovementRouteValidator` |
| F2 | "고지대 → 저지대 공격 시 벽 무시"는 설계·계획만 있고 코드는 없다(천명님 계획 H1 대기). 구현돼도 Buffer·View 계약은 그대로다(그 설계 D9). | `specs/2026-10-14-high-ground-wall-bypass-design.md` |
| F3 | View가 쓸 사실은 Buffer에 이미 있다. `StaticTerrainFact`(종류 `HighGround`·`Active`·`KnownTo`)와 `EnvironmentSemanticBindings.TerrainSourceIds`. Core에는 전투 중 고지대를 바꾸는 코드가 없다(읽기만 함). 새 Core·Buffer 필드는 필요 없다. | `StaticTerrainFact.cs`, Core 전체 검색 |
| F4 | 필드에는 고지대가 안 보인다. `TerrainViewSet.TryResolveKind`가 고지대·약한 지반·낭떠러지를 이름으로 골라 건너뛴다. 10-12 설치형 지형 설계가 "GroundShape 표시는 후속 범위"로 남긴 자리다. | `Display/TerrainViewSet.cs:136` |
| F5 | **잠복 결함:** 그 세 외형이 운영 `TerrainVisualDataTable`에 없다(`TerrainVisualDatabaseTests`가 미등록을 단언). 그런데 현강님 호버 설명 `CombatEnvironmentText.Build`는 `GetTerrainVisual`을 불러, 공개된 고지대 칸에 호버하면 `KeyNotFoundException`이 난다. 배치가 없어 지금은 안 드러난다. | `CombatEnvironmentText.cs:38`, `TerrainVisualDatabaseTests.cs:212` |
| F6 | 표시 방식의 원본이 둘이다. 외형 SO `VisualMode`와 F4의 이름 목록. 이름 목록은 세 외형이 표에 없어 조회가 실패하니까 생긴 것이다. | `TerrainVisualMode.cs`, `FieldTerrainPresenter.CanDisplay` |
| F7 | 외형 SO에 프리팹 칸이 있지만 `TerrainVisual_HighGround`의 프리팹은 비어 있다. 임시 외형 선례로 `Prefabs/Environment/Gameplay/Cube_Terrain_Sandstorm.prefab`이 있다. | 외형 SO 에셋 |
| F8 | 설치형 지형 표시 `FieldTerrainPresenter`는 종류별 풀 → 칸 중심 + 오프셋 배치 → 스테이지 회전을 이미 한다. 고지대 블록도 이 경로를 그대로 쓸 수 있다. | `Display/FieldTerrainPresenter.cs:234·253` |
| F9 | 화면의 모든 위치는 높이 0 평면이다. 칸 → 위치 변환 `FieldGridMapper.CellCenterToLocal/ToWorld`를 약 25곳이 부른다. 유닛의 서 있는 위치는 `FieldUnitPresenter.CenterOf` 한 곳, 걸음 보간은 `CombatUnitPresentationWindow.Placement.Interpolate` 한 곳(두 사실 위치를 직선 보간)이다. 넉백도 같은 걸음 경로를 탄다(동시 처리 C-3). | `FieldGridMapper.cs:58`, `FieldUnitPresenter.cs:550`, `CombatUnitPresentationWindow.cs:354` |
| F10 | 장면 표시 `Refresh` 순서는 유닛(`_events.Refresh`) → 오브젝트 → 지형이다. 유닛이 높이를 알려면 높이 표가 유닛보다 먼저 채워져야 한다. | `CombatSceneDisplay.cs:196-198` |
| F11 | GridOutline 강조 테두리는 슬롯마다 높이 하나(`_yOffset` + 층 간격)로 그린다. 채우기(`FieldCellFill`)는 칸마다 위치를 받는다. 격자 데칼은 깊이 1인 투영 상자다. 포인터는 높이 0 평면 Ray로 칸을 고른다. | `FieldOutlineRenderer.cs:313·326`, `FieldCellFill.cs:99`, `StageFieldView_Default.gridDecalDepth`, `CombatPointerMapper.TryRayToFieldPlane` |
| F12 | 운영 전투 스테이지 12개 모두 고지대 배치가 0이다. 개발 Fixture 정의에는 환경이 없다. | `InGameGroup/Stage/Combat/*.asset` |

## 2. 단계 (0 → 4, 각 단계 끝에서 멈추고 보고)

### 0단계 — 외형 표 등록과 높이 값 칸 (작음)

- 고지대·약한 지반·낭떠러지 셋을 `TerrainVisualDataTable`에 등록한다. F5 예외가 셋 다 사라지고, 호버 설명이 외형 SO의 이름·설명으로 나온다.
- 외형 SO에 새 칸 `surfaceHeight`(칸 표면 높이, 필드 로컬 단위)를 더한다. 고지대만 0보다 크고 나머지는 0이다(Q-HG1).
- DB 검사(외형 Data 생성자 + 표):
  - 높이는 유한한 0 이상이어야 한다.
  - 높이가 0보다 크면 GroundShape여야 하고 프리팹도 있어야 한다. 블록 없이 유닛만 떠 있는 화면을 막기 위해서다.
  - PlacedPrefab은 지금처럼 프리팹이 필수이고 높이는 0이다.
- 화면 변화: 고지대 칸 호버 설명만. **이때부터 고지대 설명의 "벽 무시" 줄이 화면에 나와 Core 동작(벽에 막힘)과 다른 것이 드러난다.** 문구는 그대로 둔다(사용자 결정).

### 1단계 — 고지대 블록 배치 (사용자 ①)

- F6의 이름 목록을 걷는다. 이제 "그릴 수 있나"는 "외형에 프리팹이 있나" 하나로 정한다. PlacedPrefab은 늘 프리팹이 있고, GroundShape는 있으면 그린다.
- 블록은 F8 경로 그대로 칸 중심(높이 0) + 오프셋에 놓는다. 블록 프리팹은 피벗이 바닥이고 윗면이 `surfaceHeight`에 오도록 만든다(Q-HG2).
- 보이는 지형 거르기(분류·켜짐·Player 공개)는 지금 `TerrainViewSet` 한 곳이다. 숨은 고지대(Player에 비공개)는 블록도 높이도 없다. Core 고지대 설계 D8("미리보기는 공개 정보")과 같은 방향이다.
- 화면 변화: 고지대 칸에 블록이 생긴다. **이 단계만으로는 그 위 유닛이 블록에 묻힌다** → 2단계와 붙여서 확인한다.

### 2단계 — 칸 표면 높이 원본 하나 + 서 있는 유닛

- 새 순수 표 `FieldSurfaceHeights`를 만든다. 필드 하나에 하나, 칸 수 크기 배열, 기본 0이다.
  - 채우는 쪽은 하나다. Refresh 맨 앞에서 보이는 지형 원본의 외형 `surfaceHeight`로 채운다. 한 칸에 여럿이면 가장 큰 값을 쓴다.
  - 거르기는 1단계와 같은 함수를 쓴다(SSOT).
- 읽는 쪽은 `FieldGridMapper`의 새 함수 `CellSurfaceToWorld(cell)`(칸 중심 + 위 방향 × 높이)이다.
  - 옛 `CellCenterToWorld`는 지금처럼 바닥(높이 0)이다. 블록·레일·데칼 같은 바닥 물건은 그대로 두고, 높이가 필요한 곳만 새 함수로 옮긴다.
  - 25곳의 동작이 한꺼번에 몰래 바뀌지 않게 하려는 것이다(독립성).
- `FieldUnitPresenter.CenterOf`가 새 함수를 쓴다. 서 있는 위치, 걸음 보간의 양 끝, 배치 고스트가 함께 따라온다.
  - 여러 칸 유닛은 점유 칸 높이 중 가장 높은 값을 쓴다(묻히지 않게).
- Refresh 순서: 높이 표 채우기 → 유닛 → 오브젝트 → 지형 블록(F10).
- 화면 변화: 고지대 위 유닛이 블록 윗면에 선다.

### 3단계 — 높이 이동 (사용자 ②)

- 갈 수 있는지는 Core가 정한다(F1). 이동 범위·경로 미리보기·넉백 결과를 지금처럼 보여 준다. View에 새 판단은 없다.
- 걸음 보간 한 곳(`Placement.Interpolate`)이 높이가 다른 두 칸 사이를 오르내리는 모양으로 간다(Q-HG3).
  - 모양 식은 새 순수 함수 하나(`CombatStepHeight`)에 둔다. 걸음·넉백이 같이 쓴다.
  - 뜀 높이 같은 값은 DB 설정 표 `CombatPresentationSettings`에 둔다(A-5 선례).
- 같은 높이끼리 걸을 때는 지금과 똑같다(높이 차 0이면 직선).
- 화면 변화: 오를 때·내려올 때 블록 모서리를 넘어가는 걸음이 보인다. 넉백으로 떨어질 때도 같다.
- **보충(2026-10-28, [걸음 모양 설계](2026-10-28-combat-step-motion-style-design.md) ST2):** 높이 식 원본은 그대로 `CombatStepHeight` 하나다. 걷기 모양은 `At`(같은 높이 = 평평, 다르면 호)을, 칸마다 뛰는 자발 이동 모양은 `Arc`(늘 호, 꼭대기 = 높은 칸 + `hopHeight`)를 쓴다. `Placement.Interpolate`는 높이를 걸음 모양에 묻는다.

### 4단계 — 높이 때문에 따라 고칠 것

높이가 생기면 바닥 평면을 가정한 곳이 어긋난다. 이번 범위는 Q-HG4로 정한다.

| 묶음 | 대상 | 어긋나는 것 | 고치는 방향 |
|---|---|---|---|
| 4a 입력·강조(권장: 이번 범위) | 강조 테두리 | 슬롯 하나가 높이 하나라 고지대 칸 테두리가 블록 안에 묻힌다(F11) | 고지대 칸만 표면 높이로 따로 그린다(슬롯을 높이 단계별로 나눔) |
| | 강조 채우기 | 칸 위치가 높이 0이다 | 칸 위치를 `CellSurfaceToWorld`로 |
| | 격자 데칼 | 깊이 1이라 블록 높이에 따라 윗면에 격자가 안 닿을 수 있다 | 깊이를 높이에 맞춰 늘리거나 블록 윗면을 같은 Rendering Layer에 둔다(확인 뒤 정함) |
| | 포인터 칸 고르기 | 평면 Ray라 블록 윗면을 누르면 시점 때문에 뒤쪽 칸이 잡힌다 | 먼저 고지대 높이 평면과 만나는 점을 보고, 그 칸이 고지대면 그 칸, 아니면 바닥 평면 |
| 4b 연출 높이(권장: 1~3 확인 뒤 다음 계획) | 투사체·궤적 | 출발은 유닛 + 발사 높이, 목표는 바닥 칸이라 고지대 대상이 발밑을 맞는다 | 목표·출발 칸 위치를 표면으로 |
| | 피격 이펙트·몸 중심 | 사실 기반 몸 중심이 바닥 기준이다 | 몸 중심 식에 표면 높이 |
| | 칸 위 물건 | 지뢰·깃발·배지·가시밭이 고지대 칸에 있으면 블록 안이다 | 놓는 위치를 표면으로 |
| | 기차·레일 | 고지대 칸에 레일이 있으면 묻힌다 | 스테이지 배치에서 겹치지 않는지 확인만(레일은 바닥 물건) |

접촉 판정(걸음·투사체·기차)은 가로 몸(XZ)만 보므로 높이와 상관없다. 카메라(FieldCamera)는 다른 작업자 영역이라 건드리지 않는다.

## 3. 정할 것 (권장은 모두 A)

### Q-HG1 고지대 높이 값의 원본
- **A (권장)** 외형 SO 새 칸 `surfaceHeight`. 블록·유닛·걸음·강조가 이 값 하나를 읽는다. 블록 프리팹은 이 높이에 맞춰 만든다.
- B 블록 프리팹 콜라이더 윗면에서 읽는다. 값 칸은 없지만, 블록을 꺼내기 전(Refresh 맨 앞)에 높이를 알아야 하는 순서와 맞지 않고 프리팹마다 콜라이더가 필수가 된다.
- C 코드 상수. 기획이 바꿀 수 없다.

### Q-HG2 블록 프리팹
- **A (권장)** 우리가 임시 큐브 프리팹 `Prefabs/Environment/Gameplay/Cube_Terrain_HighGround.prefab`을 만든다(모래폭풍 임시 큐브 선례, 높이 = `surfaceHeight`). 아트 프리팹이 오면 SO 칸만 바꾼다.
- B 아트 프리팹이 올 때까지 높이를 0으로 둔다(블록·높이 없음, 호버 설명만). 1~3단계 재생 확인을 미룬다.

### Q-HG3 오르내리는 모양
- **A (권장)** 뛰어오르기. 가로는 지금처럼 직선이고, 높이는 두 칸 높이 사이를 이으면서 위로 볼록한 호를 더한다. 호 높이가 블록 모서리보다 높아 블록을 뚫지 않는다. 값(뜀 높이)은 DB 설정 표에 둔다.
- B 비스듬한 직선. 2단계만으로 저절로 생기지만 블록 모서리를 뚫고 지나간다.
- C 계단식. 먼저 수직으로 올라간 뒤 가로로 간다. 내려올 때는 반대다. 딱딱해 보인다.

### Q-HG4 4단계 범위
- **A (권장)** 4a(강조·데칼·포인터)는 이번 설계 범위에 넣는다. 안 하면 고지대 칸의 이동·공격 범위가 블록에 가려지고 클릭이 어긋나 플레이에 바로 지장이 있다. 4b(연출 높이)는 1~3·4a 확인 뒤 다음 계획으로 한다.
- B 4a·4b 모두 이번.
- C 1~3만 하고 4는 모두 나중.

### Q-HG5 약한 지반·낭떠러지
- **A (권장)** 0단계 등록으로 호버 설명만 생긴다. 필드 표시(블록·구멍)는 나중에 같은 길로 더한다. 높이 0이라 2~4단계에 영향이 없다.
- B 같이 그린다. 낭떠러지는 "구멍"이라 음의 높이 또는 다른 외형이 필요하고, 약한 지반 → 낭떠러지로 바뀌는 순간도 정해야 한다.

### Q-HG6 재생 확인용 고지대 배치
- **A (권장)** 사용자가 에디터에서 운영 스테이지 하나(예: `Combat_Normal_A`)의 `terrainPlacements`에 고지대를 임시로 몇 칸 넣고 확인한 뒤 되돌린다(커밋 안 함). 코드 시험은 가짜 사실로 따로 고정한다.
- B 운영 스테이지에 정식으로 배치한다. 레벨 데이터라 기획 확인이 필요하다.
- C 배치가 생길 때까지 재생 확인을 미룬다.

## 4. 설계에서 정한 것 (사용자 확인 대상)

- P1 옛 `CellCenterToWorld`는 바닥 그대로 두고 표면 함수를 따로 만든다(§2 2단계). 높이가 필요한 곳만 옮긴다.
- P2 여러 칸 유닛 높이는 점유 칸 중 가장 높은 값이다.
- P3 높이 표는 Refresh마다 다시 채운다. 고지대는 전투 중 바뀌지 않지만(F3) Buffer 원본을 매번 따른다(전투 목표 선례). 결과 재생 중에는 직전 Refresh 표를 쓴다.
- P4 블록 프리팹에 BoxCollider가 있어도 고지대는 접촉 원본이 아니라 몸 조회에 쓰이지 않는다. 콜라이더를 요구하지 않는다.
- P5 Core·Buffer·InGame DB·Factory는 바꾸지 않는다. Core 전달 문서에 쓸 것이 없다.

## 5. 영향 받는 시험 (예상)

| 단계 | 시험 | 이유 |
|---|---|---|
| 0 | `TerrainVisualDatabaseTests`(실제 DB) | 세 외형 미등록 단언 → 등록·높이 단언 |
| 0 | 외형 SO·Data 시험 | 높이 칸·검사 규칙 |
| 1 | `TerrainViewSetTests.GroundShapeSource_…` | 이름 목록 대신 프리팹 유무로 그림 |
| 1 | `FieldTerrainPresenterTests` GroundShape 두 시험 | 프리팹 있는 GroundShape는 그림 |
| 2 | 유닛 위치·걸음 보간 시험 | 고지대 칸 사례 추가(평지 사례는 그대로여야 함) |
| 3 | 새 `CombatStepHeight` 순수 시험 | 같은 높이 = 직선, 오르기·내리기 호 |
| 4a | `FieldOutlineRendererTests` 슬롯 수·`FillNames`, 포인터 시험 | 높이 단계별 슬롯, 두 평면 고르기 |

평지 스테이지(지금 운영 12개 전부)에서는 모든 기존 시험의 기대값이 바뀌지 않아야 한다. 이것이 단계마다의 회귀 기준이다.

## 6. 착수 조건과 겹치는 작업

- 호버 위험 범위 HZ-b(다른 세션, 커밋 전)가 같은 파일을 고쳐 두었다: `TerrainViewSet`·`FieldHighlightLayers`·`FieldHighlightPalette`·`FieldHighlightState`·`FieldOutlineRenderer`·`FieldView`, 새 `CombatEnvironmentFacts`. **HZ-a·HZ-b 커밋 뒤 착수를 권장한다**(0~2단계는 `TerrainViewSet`만, 4a는 나머지와 겹침).
- 공유 메모:
  - 호진님: 외형 표 등록·새 칸.
  - 현강님: 호버 설명이 고지대 등에서 이제 나옴.
  - 민형님: 새 설정 값이 생기면 Factory 공유 한 줄(Factory 코드는 무변경 예상).

## 7. 원칙 점검 (7기준, 권장안 예상)

| 기준 | 예상 | 근거 |
|---|---|---|
| SSOT | 8.5 | 높이 값(SO)·높이 표(Refresh 한 곳)·보이는 지형 거르기·걸음 높이 식이 각각 한 곳. 표시 방식 이름 목록 제거 |
| SOLID | 8 | 높이 표는 채우는 쪽·읽는 쪽이 나뉨. 지형 표시기는 블록만 맡음 |
| FW | 9 | 서비스·Factory 무변경, DB 표 계약 확장만 |
| 독립성 | 8 | 옛 바닥 함수를 그대로 두어 높이가 필요한 곳만 바뀜. 평지 스테이지 동작 그대로 |
| 캡슐화 | 8.5 | 높이는 mapper의 표면 함수로만 읽음 |
| C#/Unity | 8.5 | 배열 재사용, Refresh 때만 채움, 걸음 식은 할당 없는 순수 함수 |
| If/Tick | 8.5 | 매 프레임 새 일 없음(걸음 진행 중 식 하나 더) |

예상 약 8.4/10(4a 포함). 남는 것:
- 4b 전까지는 연출이 바닥 기준이다(고지대 대상이 발밑을 맞음).
- 높이 단계별 테두리 슬롯이 늘어난다(높이 값이 하나면 두 배).

## 8. Phase (결정 뒤 Task 계획에서 자세히)

- HG-0 표 등록·높이 칸·검사 → 호버 설명
- HG-1 블록 배치(이름 목록 제거, 임시 큐브) → HG-2 높이 표·유닛 높이(1과 함께 재생 확인)
- HG-3 높이 이동(걸음·넉백)
- HG-4a 강조·데칼·포인터
- (다음 계획) HG-4b 연출 높이
