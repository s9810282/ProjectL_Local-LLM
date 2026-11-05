# 전투 공격 궤적 미리보기 설계 (2026-10-19)

- **목표:** 공격 모드에서 포인터가 올라간 칸까지 **공격 궤적을 점선으로** 보인다. 곡사(포물선)와 직선 두 모양, 직선이 벽에 막히면 막힌 곳에서 끊고 X를 띄운다.
- **상태:** 설계 결정 완료(§7, 모두 A), 코드 변경 없음. Task 계획은 [attack-trajectory-plan](../plans/2026-10-19-combat-attack-trajectory-plan.md). 원칙 평가는 §6, 성능은 §3.
- **사용자 결정(2026-10-19): MultiLine UV는 지금 고치지 않는다. 문제가 생기면 그때 고친다.** 그래서 하위 Render 변경 없이 지금 UV 위에서 점선을 만든다(TJ7). 고치는 날이 와도 궤적 화면은 같게 설계했다.
- **그리는 수단:** Framework 작업자 의견(2026-10-19) "상위 렌더(`DrawPointLineSystem`)가 아니라 하위 렌더를 고치거나 더해서 쓰기"를 따른다. 하위 Mesh·Color 부품을 View에서 조합하고(`FieldCellFill`과 같은 꼴), 상위 `DrawPointLineSystem`은 고치지 않는다. 하위 부품도 지금은 고치지 않는다(아래 사용자 결정).

## 0. 요청과 해석

- 사용자 요청(2026-10-19): 플레이어 유닛이 공격 모드일 때 호버한 타일까지 공격 궤적을 표기한다. 공격 방식은 곡사·직선 두 가지다. 레퍼런스 2장 — 곡사 점선이 벽을 넘어 대상 칸에 떨어짐(끝에 표지), 직선 점선이 벽에서 멈추고 X와 "벽에 막힘".
- 사용자 걱정: "런타임에 수시로 점을 생성하게 될 만큼 성능 부하가 생길 것 같다." → §3에서 답한다. 요약하면 **점을 객체로 만들지 않는다.** 궤적 하나는 메시 하나이고, 점 무늬는 셰이더가 UV로 찍는다.
- 해석:
  - **보이는 때:** 공격 모드에서 호버 칸에 대한 Core 응답이 공격 가능이거나(유효 궤적), 그 칸이 벽에 막혔다는 Core 증거가 있을 때(막힌 궤적).
  - **모양:** 공격자 외형의 공격 연출 종류가 정한다(곡사 = 포물선, 직선·Ballista = 직선, 근접 = 없음). 막힘 여부는 Core가 정한다. 두 사실을 서로에게서 추론하지 않는다.
  - **궤적 = 실제 발사와 같은 길:** 미리보기와 실제 발사체가 같은 함수로 시작점·끝점·곡선을 계산한다.

## 1. 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | 공격 조준은 범위 응답(조준 목록·AP 판정·막힘 증거)을 받은 뒤 호버 칸마다 Core 미리보기를 요청한다. 막힌 칸은 상세 요청을 하지 않고 범위 응답의 증거(접촉 칸 열쇠)만 표시한다. 같은 칸 호버는 다시 처리하지 않는다. | `AttackTargetingInteraction.cs:234`(`ApplyRangeAnswer`)·`:260`(`ApplyAttackAnswer`)·`:282`(`RequestHoverForCell`), `CombatHoverTracker` |
| F2 | 호버 응답 `AttackPreview`에 `CanAttack`·`PrimaryCell`·`AttackDirection`·`AffectedCells`·`PiercingShot`·`BlockedContacts`가 있다. 막힘 증거 `AttackBlockedContact`는 `Origin`·`Contact`·`Phase`(Direct = 전달 막힘, Propagation = 착탄 뒤 파급 막힘)·`WallCells`를 가진다. | `Contracts/Results/AttackPreview.cs`, `AttackBlockedContact.cs` |
| F3 | 벽 통과 여부는 Core 규칙이다(`UnitData.Occlusion`, `RequireClear`/`IgnoreWalls`). `AttackOcclusionSpec.Lobbed` 주석: "궤적 연출이나 특정 유닛의 자동 설정은 포함하지 않는다". | `AttackOcclusionSpec.cs:14` |
| F4 | 궤적 모양은 View 외형 값이다: `UnitVisualData.AttackPresentation.Kind`(Linear·Melee·Mortar·Ballista), `ArcHeightInCells`, `ProjectileHeight`. 창구가 표시 중인 유닛의 값과 무기를 준다. | `CombatAttackPresentationSO.cs`, `CombatUnitPresentationWindow.cs:186`(`TryGetAttackPresentation`)·`TryGetMortarPresentation`·`GetBallistaWeapon` |
| F5 | 실제 발사의 기하가 여러 파일에 흩어져 있다. 곡선 식은 발사체 풀 안(`Lerp + up × 높이 × 4t(1−t)`), 곡사 높이는 곡사 애니메이션 안(`WorldCellWidth × ArcHeightInCells`), 직선 시작점 규칙(위치 + 높이, 목표 쪽으로 0.2)은 직선 애니메이션 안, Ballista 끝점은 Ballista 애니메이션 안, 끝점 해석은 `CombatAttackTarget.Resolve(AttackResult)`. | `CombatProjectilePool.cs:282-289`, `CombatMortarAnimation.cs:158-159`, `CombatLinearAttackAnimation.cs:59-63`, `CombatBallistaAnimation.cs:88-91`, `CombatAttackTarget.cs:9` |
| F6 | 경로가 몸에 처음 닿는 진행률을 구하는 함수가 이미 있다(`CombatContactGeometry.TrySegmentContact`). 몸이 없거나 닿지 않으면 Core 접촉 칸을 경로에 투영한다(히트 타이밍 설계 H5·Q3). 칸 하나의 몸은 `FieldGridMapper.CellBody`. | `CombatContactGeometry.cs:24`, `CombatAttackPlayback.cs:447-462` |
| F7 | 하위 Mesh는 월드 좌표 점을 그대로 받는다: `IMeshSystemService.Register(MeshFilter)` → `Update(new MultiLineCreateData(이름, Vector3[], 두께, uvScale, 색))`. 메시 노드는 런타임 Mesh 하나를 재사용하고(`Clear` 뒤 다시 채움), 빌더는 갱신마다 List 다섯 개를 새로 만든다. | `MeshSystem.cs`, `MeshNodePool.cs`(`MeshNode.Apply`), `MeshHightLevelBuilder.cs:177`(`CreateMultiLine`) |
| F8 | **MultiLine UV 결함:** `CalcLineUVs`가 누적 거리를 선 폭 방향(uv.y)에 넣고, 길이 방향(uv.x)은 구간마다 0→1로 다시 시작한다. 호출처는 `CreateMultiLine` 하나, 사용자는 `DrawPointLine` 하나(텍스처 없는 URP Lit 재질이라 화면에 드러나지 않음). MultiLine UV를 검사하는 시험은 없다. 다른 경로 메시(GridOutline)의 규칙은 uv.x = 길이 방향 누적값, uv.y = 폭 0/1이다. **사용자 결정으로 이번에는 고치지 않는다.** | `MeshLowLevelBuilder.cs:57-61`, `MeshSystemHandleTests.cs:146-171`, `LineShader.mat` |
| F9 | `AreaOutline` 셰이더: Transparent·ZWrite Off·ZTest LEqual·Cull Off, uv.x로 점선을 끊고(`_DashEnabled`·`_DashCount`·`_DashFill`) `_Time`으로 흐름을 움직인다(`_FlowDirection`·`_FlowSpeed`). 점선은 길이 방향만 끊어 네모 모양이다. | `Render/Shader/View/CustomShader/Outline/AreaOutline.shader` |
| F10 | 선례: `FieldCellFill`이 View에서 Mesh·Color 핸들을 직접 조합한다. `FieldView.Build`에서 만들고 `FieldView.Release`에서 거둔다. 필요한 서비스(`Meshes`·`Colors`·`Effects`)는 `CombatDisplayServices`에 이미 있다. | `Display/FieldCellFill.cs`, `Display/FieldView.cs:72·217`, `CombatDisplayServices.cs` |
| F11 | 필드 입력 피드백 값의 자리: 모양 값(테두리 두께·점선 길이·채움, 채우기 재질)은 View DB `StageFieldViewData`, 색은 `FieldView` 팔레트(씬 값). | `StageFieldViewDataSO.cs:43-51`, `FieldView.cs:31-53` |
| F12 | 입력 중 표시 의도 계약 `ICombatInteractionFeedback`의 구현은 씬 표시(`CombatSceneDisplay`, `ICombatBattleDisplay` 경유)와 시험 가짜 둘(`CombatInteractionTests.FakeFeedback`, `HojinCombatAttackDragTests.Probe`). 공격 피드백은 Factory가 만든다. | `ICombatInteractionFeedback.cs`, `CombatSceneDisplayFactory.cs:60` |
| F13 | 표시 쪽에는 결과 재생 밖의 매 프레임 Tick이 없다. | 클립 설계 4차 확인 사실 |

## 2. 설계

### TJ1. 궤적 하나 = 메시 하나

- 궤적은 **MultiLine 메시 하나 + 렌더러 하나**다. 점선 무늬는 셰이더가 UV로 찍는다(같은 길이 구간 하나에 점 하나, TJ7).
- 점마다 GameObject·메시·파티클을 만들지 않는다. 점이 몇 개든 그리기는 한 번이다.
- 흐름(점이 목표 쪽으로 움직임)은 셰이더 `_Time`이 맡는다. CPU는 매 프레임 아무것도 하지 않는다.

### TJ2. 책임 넷

| 책임 | 소유자 | 하는 일 | 하지 않는 일 |
|---|---|---|---|
| 언제·무엇을 보일지 | `AttackTargetingInteraction`(기존) | 범위·호버 응답을 받는 지금 자리에서 표시 의도만 낸다 | 좌표·모양 계산 |
| 발사가 어디로 가나 | **`CombatShotPath`**(새, 순수 정적) | 시작점·끝점·곡선 식·곡사 높이. 실제 발사와 미리보기가 함께 부른다 | 막힘 판정, 그리기 |
| 궤적 구성 | **`CombatAttackTrajectory`**(새, Display) | 공격자 외형·무기를 창구에서 읽고, 경로(촘촘한 점 배열)·끊는 점·상태를 만든다 | 점선 규칙, 메시·핸들 수명 |
| 그리기 | **`FieldTrajectoryLine`**(새, Display, `FieldView` 소유) | 경로를 같은 길이 구간으로 다시 나누기(점선 규칙, TJ7), Mesh·Color 핸들 수명, 버퍼, X 표지 위치·보임 | 공격·유닛 지식 |

- `CombatAttackTrajectory`는 `CombatAttackFeedback`이 가진다(지금 범위·영향·막힘 표시를 맡는 곳).
- `FieldTrajectoryLine`은 `FieldCellFill`처럼 `FieldView.Build`에서 만들고 `FieldView.Release`에서 거둔다. 필드 표시 모드(CellPrefab·GridOutline)와 상관없이 있다.

### TJ3. 발사 기하 원본 하나 (`CombatShotPath`)

실제 발사 코드(F5)에 흩어진 식을 순수 함수로 모으고, 발사체 풀·애니메이션과 궤적이 모두 이것을 부른다. 실제 발사 쪽은 **동작이 같은 정리**다.

| 함수 | 지금 있는 곳 | 쓰는 곳 |
|---|---|---|
| `ArcPoint(from, to, up, height, t)` = `Lerp(from, to, t) + up × height × 4t(1−t)` | `CombatProjectilePool.Advance` | 풀(실제 포탄), 궤적 |
| `ArcHeight(mapper, settings)` = `WorldCellWidth × ArcHeightInCells` | `CombatMortarAnimation.Fire` | 곡사 애니메이션, 궤적 |
| `LinearStart(origin, target, up, height)` = 위치 + 높이, 목표 쪽으로 최대 0.2 | `CombatLinearAttackAnimation` | 직선 애니메이션, 궤적 |
| `RaisedEnd(cellCenter, up, height)` | 직선·Ballista 애니메이션 | 두 애니메이션, 궤적 |
| `CombatAttackTarget.Resolve(primaryCell, direction, affectedCells, mapper, origin)` (기존 `Resolve(AttackResult)`가 이것을 부름) | `CombatAttackTarget` | 실제 공격(결과), 궤적(미리보기 응답) |

- 곡사·Ballista의 시작점은 무기의 `MuzzlePosition`(창구가 주는 무기, 읽기만)이다. 미리보기는 쉬는 자세의 총구에서 시작한다(실제 발사는 버킷이 기운 뒤라 조금 다르다, 관찰 O-3).
- 직선 시작점의 "위치"는 유닛의 지금 표시 사실 위치다. 창구에 읽기 조회를 하나 더한다(이름·형태는 계획에서 정한다. 창구는 이미 `_targets.FactPosition`으로 같은 값을 계산한다).

### TJ4. 모양 표

| 외형 종류 | 궤적 | 시작 | 끝 |
|---|---|---|---|
| Mortar(곡사) | 포물선 | 무기 총구 | 주 칸 중심(실제 포탄과 같음) |
| Linear(직선) | 직선 | `LinearStart` | `Resolve` 결과 + 발사 높이 |
| Ballista | 직선 | 무기 총구 | `PiercingShot.Endpoint` 중심 + 발사 높이 |
| Melee | 없음 | — | — |

| 조준 종류 | 궤적 |
|---|---|
| Cell(칸) | 보인다 |
| Direction(방향, 예: 샷건) | Q-T1 (권장: 보인다. `Resolve`가 실제 발사와 같은 끝점을 준다) |
| All(전체) | 없음 |

### TJ5. 보이는 때·지우는 때

| 상황 | 궤적 | 근거 사실 |
|---|---|---|
| 호버 응답 `CanAttack = true` | 유효 궤적(끝 = 응답으로 해석한 끝점) | Core 호버 응답 |
| 호버 칸이 범위 응답의 막힘 증거를 가짐(Direct) | 막힌 궤적(끝 = 접촉 칸, 첫 Direct 벽에서 끊음) | Core 범위 응답 |
| 호버 응답 `CanAttack = false`이고 Direct 증거가 있음 | 막힌 궤적 | Core 호버 응답 |
| 그 밖(범위 밖, AP 부족, 응답 대기, 결과 재생 전, 모드 종료, 행위자 바뀜) | 지움 | — |

- 지우는 자리는 지금 `ClearAffectedCells`·`ClearAttackBlockers`를 부르는 자리와 같다(호버 칸 바뀜, 응답 거절, `BeforeResult`, `AfterResult`, `Exit`·`ResetRangeAnswer`).
- 막힌 궤적의 X 뒤 구간: Q-T7(권장: 그리지 않음, 레퍼런스 2와 같음).

### TJ6. 끊는 점 (막힘)

- 끊는 근거는 **Direct 증거뿐**이다. Propagation은 착탄 뒤 파급이 막힌 것이라 궤적은 끝까지 간다(파급이 막힌 벽 칸은 지금처럼 `BlockedWall` 층이 보인다).
- 끊는 진행률 = 경로의 바닥 투영이 Core가 준 벽 칸(`WallCells`)의 몸(`CellBody`)에 처음 닿는 진행률 중 가장 작은 값(`TrySegmentContact`). 닿지 않으면 첫 벽 칸 중심을 경로에 투영한다(히트 타이밍 H5의 대체 규칙과 같음).
- 곡선의 바닥 투영은 `t`에 대해 직선이므로(`Lerp` + 위쪽 성분), 바닥 진행률이 곧 곡선 매개변수다. 곡선도 같은 `t`에서 자른다.
- X 표지는 끊는 점에 둔다.
- 계약 문구 "View에서 벽 형상을 계산하지 않는다"(`ShowAttackBlockers`)와의 관계: 막힘 여부와 막은 벽 칸은 Core 증거를 그대로 쓰고, View는 그 칸을 그릴 경로에 투영할 뿐이다. 벽 모양(모서리 잘림 등)은 다시 계산하지 않는다.

### TJ7. 점선 만드는 법 (하위 Render 변경 없음)

사용자 결정으로 MultiLine UV를 그대로 둔다. 하위 Render·셰이더·상위 `DrawPointLineSystem` 모두 고치지 않는다.

- **지금 UV에서 쓸 수 있는 사실:** `AreaOutline` 셰이더는 uv.x만 읽는다(F9). 지금 MultiLine의 uv.x는 구간마다 0→1이다(F8). 그래서 `_DashCount = 1`이면 **구간 하나에 점 하나**가 찍힌다.
- **점선 규칙 = 같은 길이 구간:** 그리기 부품이 경로를 `점 간격`에 가장 가까운 같은 길이 구간으로 다시 나눈다.
  - 구간 수 `N = max(1, round(경로 길이 / 점 간격))`, 구간 길이 = 경로 길이 / N.
  - 곡사 경로는 구성 부품이 촘촘한 점 배열로 주고, 그리기 부품이 누적 길이로 같은 길이 지점을 찾는다(호 길이 기준, 현과 호의 차이는 점 간격 기준 1% 안팎이라 보이지 않는다).
  - 이 규칙은 그리기 부품 한 곳에만 둔다(셰이더 값 `_DashCount`·`_DashFill`과 함께).
- **UV를 나중에 고쳐도 같은 화면:** `uvScale = 1 / 구간 길이`로 넘긴다.
  - 지금은 `uvScale`이 폭 방향 값에만 곱해지고, 셰이더가 폭 방향을 읽지 않으므로 영향이 없다.
  - UV가 고쳐지면 uv.x = 누적 거리 / 구간 길이 = (구간 번호 + 구간 안 비율)이 되어, 셰이더가 쓰는 소수 부분이 지금과 똑같다.
  - 그래서 고치는 쪽(민형님이든 우리든)이 궤적을 따로 손볼 필요가 없다. 같은 길이 구간 규칙은 고친 뒤에도 그대로 둔다.
- **점 자리:** 셰이더는 점 중심을 uv.x 정수(구간 이음매)에 둔다. 그래서 시작(총구)과 끝(목표)에는 점이 반쪽씩 보인다. 흐름(`_FlowSpeed`)이 켜져 있으면 점이 계속 움직이므로 눈에 띄지 않는다. 반쪽 점이 문제가 되면 셰이더에 위상 값을 더해야 하므로 그때 다룬다(O-6).
- **흐름 방향:** `_FlowDirection = 1`이면 점이 목표 쪽으로 흐른다. 구간 길이가 모두 같아서 이음매에서도 무늬가 끊기지 않는다.
- **둥근 점은 미룬다:** 점을 둥글게 깎으려면 셰이더가 폭 방향 0→1(uv.y)을 읽어야 하는데, 지금 uv.y는 누적 거리라 쓸 수 없다. 둥근 점은 UV를 고칠 때 함께 다룬다. 그때까지 점은 네모(길이 = 구간 × 채움, 폭 = 두께)다.

### TJ8. 값의 자리

| 값 | 자리 | 이유 |
|---|---|---|
| 재질(AreaOutline 셰이더), 두께, 점 간격(월드 단위), 점 채움 비율, 흐름 속도 | `StageFieldViewData` 새 묶음 "Attack Trajectory" | 테두리 점선 길이·채움과 같은 자리(F11). 값 조건은 Data 한 곳에서 검사 |
| 유효 색, 막힘 색 | `FieldView` 팔레트 | 다른 필드 강조 색과 같은 자리(F11), Q-T5 |
| X 표지 | Q-T2(권장: `StageFieldViewData`의 프리팹 칸) | — |
| 곡사 경로 표본 수(32구간) | 코드 상수(`CombatAttackTrajectory`) | 곡선을 같은 길이로 다시 나눌 때 쓰는 정밀도일 뿐 기획 값이 아님. 화면의 점 간격은 DB 값이 정한다 |

- 셰이더 값 `_DashEnabled = 1`·`_DashCount = 1`·`_FlowDirection = 1`은 점선 규칙(TJ7)의 일부라 그리기 부품의 코드가 정한다. `_DashFill`·`_FlowSpeed`는 위 DB 값이다.

### TJ9. 바뀌지 않는 것

- 하위 Render(MultiLine UV 포함)·`AreaOutline` 셰이더·상위 `DrawPointLineSystem`.
- Core·Buffer·미리보기 요청 흐름(새 요청 없음).
- 범위·영향·막힌 벽 칸 강조, 비용·예상 체력 표시.
- 실제 공격 연출의 화면(TJ3은 같은 식을 옮기기만 한다).

## 3. 성능 설계

### 3.1 결론

점을 런타임에 "생성"하지 않는다. 궤적은 필드가 만들어질 때 **한 번** 만든 메시 하나를 다시 채워 쓰고, 다시 채우는 때는 **호버 칸이 바뀌어 보일 궤적이 달라질 때뿐**이다. 매 프레임 CPU 비용은 0이다.

### 3.2 언제 얼마나 드나

| 때 | CPU | 할당 | GPU |
|---|---|---|---|
| 필드 구성(Combat 진입) | GameObject·MeshFilter·Renderer 하나, Mesh·Color 핸들 등록, X 표지 하나 | 이때만 | — |
| 매 프레임(궤적 보임) | 0 (Update·Tick 없음, 흐름은 셰이더) | 0 | 투명 띠 하나, 그리기 1회. 구간 수 = 경로 길이 / 점 간격(예: 5칸 직선·간격 0.3칸 → 17구간, 정점 68개) |
| 호버 칸이 바뀜(사람 손 속도, 초당 몇 번) | 경로 표본 2개(직선) 또는 33개(곡사), 같은 길이 구간으로 다시 나눔(구간 수만큼), 메시 다시 채움 1회, 색은 상태가 바뀔 때만 | View 0(버퍼 재사용, §3.3). 하위 빌더 List 5개(수 KB, 관찰 O-1) | 같음 |
| 같은 궤적 다시 요청 | 0 (마지막 입력과 같으면 건너뜀) | 0 | — |

- 칸이 바뀔 때 이미 Core 미리보기 요청·처리가 한 번 돈다(기존 비용). 궤적 갱신은 그보다 작다.

### 3.3 버퍼는 한 번 만들어 다시 쓴다

- **경로 배열(구성 부품):** 길이를 모양마다 고정한다 — 직선 2점, 곡사 33점(32구간). 막혀서 끊겨도 같은 개수로 `[0, t끊음]`을 다시 표본한다.
- **구간 점 배열(그리기 부품):** `MultiLineCreateData`는 배열 길이를 점 개수로 쓰므로 길이가 `N + 1`이어야 한다. N마다 배열을 처음 쓸 때 한 번 만들고 다시 쓴다. N의 상한은 필드 크기가 정하므로(가장 긴 경로 / 점 간격) 배열 수에도 상한이 있다. 처음 몇 번 뒤로는 View 할당 0.
- **누적 길이 표:** 곡사 경로용 33칸 하나를 다시 쓴다.
- **같은 입력 건너뜀:** 마지막 입력(공격자, 시작·끝 좌표, 끊는 진행률, 모양, 상태)을 기억하고, 같으면 메시를 다시 채우지 않는다.

### 3.4 택하지 않은 방법

| 방법 | 기각 이유 |
|---|---|
| 점마다 GameObject(풀) | 점 수만큼 Transform 갱신·그리기, 길이 바뀔 때 대여·반납 |
| 점마다 파티클(`SetParticles`) | 그리기는 하나지만 점 배열을 CPU가 매번 채움, 흐름도 CPU |
| 상위 `DrawPointLineSystem` 확장 | Framework 작업자 의견(하위를 쓰기), 칸 단위 입력·고정 재질 |
| 하위 MultiLine UV를 먼저 고침 | 사용자 결정으로 보류(문제 생기면 그때). 지금 방식은 고친 뒤에도 화면이 같다(TJ7) |

### 3.5 Framework 지침 대조

- §15 Tick: 쓰지 않는다.
- §16·17 Pool·Prewarm: 객체 하나를 계속 다시 쓰므로 Pool이 필요 없다.
- §19 Runtime Allocation: 궤적 갱신은 입력 사건 빈도라 Hot Path가 아니다. View 쪽은 할당 0. 하위 빌더의 갱신당 List 할당은 하위 Render 쪽 개선 거리로 남긴다(O-1).
- §25 GetComponent/Find: 쓰지 않는다(무기는 창구가 이미 가진 참조).

### 3.6 확인 방법

- 완료 판정은 위 구조 보장(Update 없음, 버퍼 재사용, 같은 입력 건너뜀)을 시험으로 확인한다(§5).
- 수치 기준은 두지 않는다(기준 없는 성능 측정은 확장 시점의 완료 증거로 미룬다). 사용자 확인으로 Profiler 한 번: 포인터를 멈춘 프레임 GC Alloc 0, 칸이 바뀐 프레임만 몇 KB.

## 4. Phase와 고치는 파일

Phase마다 멈추고 보고한다.

| Phase | 내용 | 파일 |
|---|---|---|
| TJ-0 | 기준선(MSBuild·관리 실행), 착수 조건 확인(§8) | — |
| TJ-1 | 발사 기하 공유(동작 같음) | 새 `Display/CombatShotPath.cs`, `CombatProjectilePool.cs`, `CombatMortarAnimation.cs`, `CombatLinearAttackAnimation.cs`, `CombatBallistaAnimation.cs`, `CombatAttackTarget.cs`, 새 시험 |
| TJ-2 | 그리기 부품·값(점선 규칙 TJ7 포함) | 새 `Display/FieldTrajectoryLine.cs`, `FieldView.cs`(구성·해제·팔레트 색 둘), `StageFieldViewDataSO.cs`·Data(값 묶음·검사), `StageFieldView_Default.asset`, 궤적 재질 에셋(Q-T4), 호진님 공유 메모 |
| TJ-3 | 구성·입력 연결 | 새 `Display/CombatAttackTrajectory.cs`, `CombatAttackFeedback.cs`, 피드백 계약(Q-T6), `CombatSceneDisplay.cs`(위임), `AttackTargetingInteraction.cs`(의도 호출), `CombatUnitPresentationWindow.cs`(읽기 조회 하나), `CombatSceneDisplayFactory.cs:60`·`CombatSceneInputFactory.cs`(연결), 시험 가짜, 민형님 Factory 공유 절 |
| 보류 | MultiLine UV 고침 + 둥근 점(문제가 생기면 그때, 사용자 결정) | `MeshLowLevelBuilder.cs`, `AreaOutline.shader`, 민형님 공유. 궤적 코드는 고치지 않아도 된다(TJ7) |

## 5. 시험

| 대상 | 시험 | 실행 |
|---|---|---|
| `CombatShotPath` | 곡선 양 끝·꼭대기 높이, 직선 시작 0.2 규칙, `Resolve` 새 형태와 기존 `Resolve(AttackResult)`가 같은 값 | 관리 실행 |
| 실제 공격 연출 | 기존 공격·곡사·Ballista 시험이 그대로 통과(동작 같음) | Unity |
| `CombatAttackTrajectory` | 종류별 경로(곡사 33점·꼭대기 높이, 직선 2점), 근접·전체 조준 없음, 막힘 끊는 점(Direct만, 대체 투영), 같은 입력이면 그리기 부품 갱신 0회 | 관리 실행(그리기 가짜) |
| 점선 규칙(같은 길이 구간, 그리기 부품 안 순수 함수) | `N = max(1, round(길이 / 간격))`, 구간 길이가 모두 같음(곡선은 1% 안), `uvScale = 1 / 구간 길이`, N별 배열 재사용 | 관리 실행 |
| `FieldTrajectoryLine` | 구성·갱신·지움(렌더러 숨김)·해제, 셰이더 값(`_DashCount = 1` 등), 색은 상태 바뀔 때만 | Unity |
| 입력 | 호버 응답 가능 → 유효 궤적, 막힌 칸 → 막힌 궤적, 칸 바뀜·결과 전·모드 종료 → 지움 | 관리 실행(`CombatInteractionTests` 가짜) |
| 값 | 새 값 조건(재질 필수·간격 > 0·채움 0~1) | `DataBaseAssetTests`·Data 시험 |

## 6. 원칙 평가

### 6.1 SSOT

| 사실 | 원본(한 곳) | 궤적에서 |
|---|---|---|
| 공격 가능·막힘 여부, 막은 벽 칸 | Core 응답(`CanAttack`, `BlockedContacts`) | 읽기만 |
| 조준 대상·끝 칸 | Core 응답(`PrimaryCell`·`AttackDirection`·`AffectedCells`·`PiercingShot`) | `Resolve`로 해석(실제 공격과 같은 함수) |
| 곡사·직선 모양, 곡사 높이, 발사 높이 | View DB 외형(`AttackPresentation`) | 창구에서 읽기 |
| 곡선 식·시작점·끝점 규칙 | `CombatShotPath`(TJ3로 새 원본, 지금은 네 파일에 흩어짐) | 실제 발사와 공유 |
| 경로가 칸에 처음 닿는 곳 | `CombatContactGeometry` + H5 대체 규칙 | 같은 함수 |
| 점 모양 값 | `StageFieldViewData` | 읽기 |
| 점선 규칙(같은 길이 구간 하나 = 점 하나, 셰이더 값) | `FieldTrajectoryLine` | 그 부품만 안다 |
| 색 | `FieldView` 팔레트 | 읽기 |
| 언제 보일지 | `AttackTargetingInteraction` | 의도만 받음 |

- 새로 생기는 사본: 없다. 마지막 입력 기억(§3.3)은 같은 입력을 건너뛰기 위한 표시 캐시이고 원본이 아니다.
- 오히려 TJ3이 지금 흩어진 발사 기하(F5)를 한 곳으로 모은다.
- 남는 위험: 외형 종류(View)와 벽 통과 규칙(Core)은 다른 사실이라 어긋날 수 있다(곡사 외형인데 `RequireClear`면 벽을 넘는 곡선에 X). 어느 쪽도 사본이 아니므로 SSOT 위반은 아니고, DB 교차 검사 후보로 남긴다(O-2).

### 6.2 SOLID

- **S:** 판단(입력)·기하(순수 함수)·구성(Display)·그리기(Display)를 나눴다. 그리기 부품은 공격을 모르고, 구성 부품은 핸들 수명과 점선 규칙을 모른다. 지금 UV에 맞춘 점선 규칙(TJ7)은 그리기 부품 안에만 있다.
- **O:** 새 발사 종류는 `CombatShotPath`와 모양 표(TJ4) 한 곳에 더한다. 종류 분기는 `switch` 하나로 남는다(종류가 넷이라 표 객체로 바꾸지 않는다, 지침 §27).
- **L:** 피드백 구현은 지금 계약처럼 멱등이어야 한다(지움을 여러 번 불러도 안전).
- **I:** 궤적 의도는 공격 조준만 쓴다. 기존 `ICombatInteractionFeedback`(배치 Drag·아이템 Drag도 받음)에 더하면 관계없는 가짜까지 고쳐야 한다. 그래서 좁은 계약을 권장한다(Q-T6 A). [구조 재평가](2026-10-15-combat-view-structure-reevaluation.md) N5(피드백 계약 3분할)와 같은 방향이며, N5 전체를 이번에 하지는 않는다.
- **D:** 서비스·창구·값은 생성자로 받는다. Find·GetComponent 없음.

### 6.3 Framework 지침·계약

| 항목 | 판정 |
|---|---|
| FW-001·002 서비스 등록 | 새 서비스 없음. 내부 협력 객체는 등록하지 않는다(§4.6) |
| FW-004 내부 우회 | Mesh·Color 공개 핸들만 쓴다 |
| FW-010 구체 DTO | `MultiLineCreateData`(등록된 형식)를 구체 형식으로 넘긴다 |
| FW-011 반환 | 해제 때 Color → Mesh 핸들 `Dispose`, GameObject 파괴(`FieldCellFill`과 같은 순서) |
| FW-012 Effect DB | X 표지를 프리팹으로 하면(Q-T2 A) 해당 없음. 이펙트로 하면 Effect 표 요구조건을 따른다 |
| 지침 §11 Build 뒤 구조 불변 | 필드 구성 때 한 번 만든다 |
| 지침 §15·19·25 | §3.5 |
| 공용 파일 변경 | Render 하위·셰이더 변경 없음(사용자 결정). Factory 두 곳만 → 민형님 공유 문서 |
| 하위 동작에 기댐 | 지금 MultiLine UV(구간마다 0→1)에 기댄다. 다만 `uvScale = 1 / 구간 길이`라 UV를 고쳐도 화면이 같고(TJ7), 기대는 곳은 그리기 부품 한 곳이다. 내부 상태를 우회해 바꾸지는 않는다(FW-004 위반 아님) |

### 6.4 기능 변경의 독립성 (대표 시나리오)

| 변경 시나리오 | 고칠 곳 | 이해할 코드 | 검증 |
|---|---|---|---|
| 점 간격·크기·색 바꾸기 | DB 값·팔레트(코드 0) | 없음 | 화면 |
| 둥근 점으로 바꾸기 | 보류 항목(하위 UV 고침 + 셰이더 옵션). 궤적은 셰이더 값 한 줄 | UV 함수·셰이더 | 궤적 화면 + 격자 테두리(MultiLine 사용자) |
| 하위 MultiLine UV를 고침 | `MeshLowLevelBuilder` 한 곳. 궤적 코드 0(TJ7) | UV 함수 | 궤적 화면 + 격자 테두리 |
| 곡사 높이·곡선 식 바꾸기 | `CombatShotPath` 한 곳 | 그 함수 | 실제 포탄 + 궤적(의도된 결합: 둘은 같아야 한다) |
| 언제 보일지 바꾸기(예: 방향 조준 끄기) | 모양 표(TJ4) 또는 `AttackTargetingInteraction` 한 곳 | 그 표 | 입력 시험 |
| 그리는 수단 교체(하위 Render 변경) | `FieldTrajectoryLine` 한 곳 | 그 부품 | 그리기 시험 |
| 새 발사 종류 | `CombatShotPath`·모양 표·연출 | 발사 기하 | 연출 + 궤적 |

- 남는 공통 경계: 피드백 계약(입력 ↔ 표시), `FieldView` 수명(구성·해제), MultiLine UV의 지금 동작(그리기 부품이 기대지만 고쳐도 같은 화면).
- TJ-1은 실제 공격 연출 파일 네 곳을 건드린다. 동작이 같은 정리지만 검증은 공격 연출 전체로 넓다. 이 비용은 SSOT 이득(발사 기하 원본 하나)과 바꾼다.

### 6.5 점수

SSOT 9 · SOLID 8 · FW 9 · 독립성 7 = **8.3/10** (UV 고침 보류 전 판 8.0)

- 감점: 발사 종류 분기가 `switch`로 남음(S/O, 수용), 하위 UV의 지금 동작에 기댐(FW, 고쳐도 같은 화면이라 작게), TJ-1(발사 기하 공유)의 넓은 검증 범위(독립성), 값이 DB·팔레트 두 자리로 나뉨(Q-T5).
- 보류 판이 오른 이유: Render 하위 공용 파일을 바꾸지 않아 민형님 영역 변경과 MultiLine 사용자 회귀 확인이 빠졌다.
- 화면 쪽 한계(점수 밖): 점이 네모이고, 경로 양 끝에 반쪽 점이 보인다(TJ7, O-6).

## 7. 정할 것

**결정 완료(2026-10-19, 사용자 "모두 A"): Q-T1·Q-T2·Q-T4~Q-T7 모두 A, Q-T3은 위 UV 보류 결정에 따름.** Task 계획은 [attack-trajectory-plan](../plans/2026-10-19-combat-attack-trajectory-plan.md).

| # | 질문 | 권장(= 결정) | 다른 안 |
|---|---|---|---|
| Q-T1 | 방향 조준(샷건) 궤적 | **A** 보인다(`Resolve`가 실제 발사와 같은 끝점) | B 칸 조준만 |
| Q-T2 | 막힘 X 표지 | **A** View 프리팹(`StageFieldViewData` 칸, 필드 구성 때 하나 만들어 옮김) | B Render 이펙트 종류 추가(FW-012), C 표지 없음(벽 칸 강조만) |
| Q-T3 | 둥근 점 | **결정됨(2026-10-19):** 지금은 네모 점. 둥근 점은 UV 고침과 함께 보류(사용자 결정 "UV는 문제 생기면 그때") | — |
| Q-T4 | 재질 | **A** 궤적 전용 재질 칸 + 새 재질 에셋(AreaOutline 셰이더) | B `highlightFillMaterial` 재사용 |
| Q-T5 | 색 자리 | **A** `FieldView` 팔레트(다른 필드 강조 색과 같은 자리) | B `StageFieldViewData`에 모양 값과 함께 |
| Q-T6 | 피드백 계약 | **A** 좁은 새 계약(공격 조준만 받음, 기존 가짜 둘은 그대로) | B 기존 `ICombatInteractionFeedback`에 추가 |
| Q-T7 | 막힌 궤적의 X 뒤 구간 | **A** 그리지 않음(레퍼런스 2) | B 흐리게 이어 그림 |

## 8. 착수 조건·겹치는 작업

- **유닛 호버 공격 범위(UR):** U-1 Core 변경이 작업 트리에 커밋 전으로 있고, U-2(View)는 공격 범위 층·입력 소유자를 건드린다. TJ-3은 같은 입력·피드백 주변이라 UR U-2 커밋 뒤 착수를 권장한다(Q-T6 A면 계약 충돌은 줄어든다). TJ-1·TJ-2는 입력 쪽을 건드리지 않아 그 전에 해도 된다.
- **폴더 정리(M):** 착수하면 이 문서의 경로가 바뀐다. 먼저 끝난 쪽 뒤에 다른 쪽(경로 고침은 M-4 문서 정리가 맡음).
- **클립 AC-5(원거리 발사 조각):** 나중에 발사 조각도 `CombatShotPath`를 쓰면 된다. 충돌 없음.
- **MultiLine UV 고침은 보류다(사용자 결정).** 다른 사람이 먼저 고쳐도 궤적은 손볼 곳이 없다(TJ7).

## 9. 관찰

- **O-1** 하위 MultiLine 빌더가 갱신마다 List 다섯 개를 새로 만든다. Hot Path가 아니라 이번에는 두고, 하위 Render 개선 거리로 민형님 공유 문서에 적는다.
- **O-2** 외형 종류와 Core 벽 통과 규칙이 어긋날 수 있다. `DataBaseAssetTests`에 교차 검사(곡사 외형 ⇔ `IgnoreWalls`)를 둘지는 기획 확인 뒤.
- **O-3** 곡사 미리보기는 쉬는 자세 총구에서, 실제 포탄은 버킷이 기운 뒤 총구에서 시작한다. 차이는 작고 고치지 않는다.
- **O-4** SingleLine(사각형 UV)은 길이 방향이 uv.y다. 보류 항목으로 MultiLine을 uv.x로 고치면 두 선의 UV 규칙이 달라진다. 그때 함께 본다.
- **O-5** 보류 항목을 하는 날, `DrawPointLine` 사용자(격자 테두리)는 화면이 같다(텍스처 없는 재질). 그날 확인할 범위는 궤적·격자 테두리 둘이다.
- **O-6** 점 중심이 구간 이음매에 오므로 경로 양 끝에 반쪽 점이 보인다(TJ7). 흐름이 켜져 있으면 눈에 띄지 않는다. 문제가 되면 셰이더 위상 값이 필요하므로 보류 항목과 함께 다룬다.

## 10. 구현 뒤 원칙 평가 (TJ-1~TJ-3, 2026-10-19, 코드 변경 없음)

사용자 요청으로 구현 결과(계획 §7.1~§7.3)를 같은 네 기준으로 다시 평가했다. TJ-1·TJ-2는 Unity 문제 없음(사용자 확인), TJ-3은 컴파일·Unity 확인 전이라 코드를 읽어 평가했다.

### 10.1 결론

**SSOT 8 · SOLID 7 · FW 9 · 독립성 8 = 8.0/10** (설계 때 예상 8.3). 높음 0, 중간 1(V1), 낮음 6.

설계 때 놓친 것은 V1이다. 설계 §6.1은 "끝점 해석 = 실제 공격과 같은 함수(`Resolve`)"라고 적었지만, **외형별로 어느 끝점을 쓰는지**(곡사 = 주 칸, Ballista = 관통 종점, 직선 = `Resolve`)는 실제 공격 쪽 세 곳과 궤적 쪽 두 곳에 따로 있다.

### 10.2 SSOT

| 사실 | 원본 | 판정 |
|---|---|---|
| 곡선 식·곡사 높이·발사 높이·0.2 출발 오프셋 | `CombatShotPath` | 한 곳 ✓(TJ-1이 네 곳을 모음) |
| 점선 규칙(같은 길이 구간 = 점 하나) | `FieldTrajectorySegments` + `FieldTrajectoryLine`의 셰이더 값 | 한 곳 ✓ |
| 보이는 조준 종류 | `AttackTargetingInteraction.ShowsTrajectory` | 한 곳 ✓ |
| Direct만 끊음·근접은 궤적 없음 | `CombatAttackTrajectory` | 한 곳 ✓ |
| 궤적 값·색 | `StageFieldViewData`·`FieldView` | 한 곳씩 ✓ |
| 공격자 위치 | 표시 Transform(`FieldUnitView.cs:402`와 같은 값) | 같은 사실 ✓ |
| **외형별 끝점 선택** | 실제: `CombatAttackPlayback.cs:185`(직선)·`:382`(곡사)·`CombatBallistaAnimation.cs:89`(Ballista) / 궤적: `CombatAttackTrajectory.Show`·`ShowBlocked` | **둘로 나뉨 — V1** |
| 접촉 대체 규칙(처음 닿음, 아니면 Core 칸 투영) | `CombatAttackPlayback.ContactProgress`·`CombatAttackTrajectory.CutProgress` | 두 곳(함수는 공유) — V3 |
| 재질 필수 | DB `Validate` + `FieldView`의 맨 설정 허용 | 의도한 두 규칙(계획 P1), 감점 없음 |

### 10.3 SOLID

- **S:** 판단(입력)·기하(순수)·구성·그리기가 나뉘었다 ✓. 다만 공격 조준 입력이 462 → 496줄로 늘었고, "호버 표시 지우기"(영향 칸·막힌 벽·궤적) 세 줄 묶음이 네 곳에 반복된다(V2). `FieldView`는 301 → 355줄 — 궤적 수명이 필드 수명이라 맞는 자리지만 책임이 하나 늘었다(수용).
- **O:** 새 발사 외형을 더하면 실제 연출과 궤적의 `TryResolveShot`·`Show`·`ShowBlocked` 세 갈래를 함께 고쳐야 한다(V1과 같은 뿌리).
- **L:** 시험 가짜는 멱등 무동작이고, 씬 표시는 해제 뒤 호출을 무시한다 ✓.
- **I:** 새 계약 셋(`ICombatAttackTrajectoryFeedback`·`ICombatTrajectoryCanvas`·`ICombatUnitPlacementReader`)이 모두 좁고, 기존 가짜는 `FakeDisplay` 하나만 늘었다 ✓.
- **D:** 생성자 주입이다. 구성은 구체 `CombatUnitPresentationWindow`를 받지만 기존 표시 부품(`CombatMovementPlanningPresenter` 등)과 같은 관례다(감점 없음).

### 10.4 FW 지침·계약

| 항목 | 판정 |
|---|---|
| FW-001·002 | 새 서비스·등록 없음 ✓ |
| FW-004 | 공개 Mesh·Color 핸들만 씀 ✓ |
| FW-010 | `MultiLineCreateData` 구체 형식 ✓ |
| FW-011 | Color → Mesh → GameObject 순서 해제, 여러 번 안전 ✓ |
| 지침 §15·§19·§25 | Tick 없음, 구간 수별 배열·벽 목록·경로 버퍼 재사용, 런타임 Find 없음(DB 검사의 프리팹 Renderer 확인은 기존 셀 프리팹과 같은 적재 시점) ✓ |
| 하위 동작 기댐 | MultiLine UV 현재 동작에 기대지만 고쳐도 같은 화면, 기대는 곳 한 곳, 공유 문서 §18에 적음 — 작은 감점 |

### 10.5 변경 독립성 (대표 시나리오, 구현 기준)

| 변경 | 고칠 곳 | 판정 |
|---|---|---|
| 점 간격·크기·색 | DB·씬 값 | 코드 0 ✓ |
| 곡선 식·곡사 높이 | `CombatShotPath` 한 곳 | ✓(실제·궤적이 같이 바뀜, 의도) |
| 끝점 규칙(예: Ballista 끝 바꾸기) | 실제 연출 + 궤적 두 갈래 | ✗ V1 |
| 새 발사 외형 | 실제 연출 + 궤적 세 갈래 | ✗ V1 |
| 언제 보일지 | `AttackTargetingInteraction` 한 곳 | ✓(단, 지우기 자리가 넷 — V2) |
| 그리는 수단 교체 | `FieldTrajectoryLine` 한 곳 | ✓ |
| MultiLine UV 고침 | 궤적 코드 0 | ✓ |
| 입력 시험 조립 바뀜 | 벽 공격 시험·궤적 입력 시험 둘(도우미 약 20줄 겹침) | 낮음 V4(천명님 파일 보호로 수용) |

### 10.6 발견

| # | 등급 | 기준 | 내용 |
|---|---|---|---|
| V1 | 중간 | SSOT·O·독립성 | 외형별 끝점 선택이 실제 공격(세 곳)과 궤적(두 갈래)에 따로 있다. 끝점 규칙을 바꾸면 미리보기와 실제가 어긋날 수 있다 |
| V2 | 낮음 | S·독립성 | "호버 표시 지우기" 세 줄 묶음이 조준 입력 네 곳에 반복(`AttackTargetingInteraction.cs:109·165·305·445`) — 새 호버 표시를 더할 때 빠뜨리기 쉽다 |
| V3 | 낮음 | SSOT | 접촉 대체 규칙(처음 닿음, 아니면 첫 칸 투영)이 실제 접촉 시각과 끊는 진행률에 두 번 쓰였다 |
| V4 | 낮음 | 독립성 | 입력 시험 조립 도우미가 벽 공격 시험과 겹친다(수용) |
| V5 | 낮음 | 정확성 | 같은 입력 건너뜀 기억이 캔버스가 밖에서 비워질 때(필드 해제 뒤 다시 구성) 낡을 수 있다 — 지금 흐름에선 모드 종료가 먼저 지워 실제로는 안 생김 |
| V6 | 낮음 | 화면 | 방향 조준이 막혔을 때 끝을 호버 칸으로 잡는다. 증거의 접촉 칸(`Contact`)이 더 정확하다 |
| V7 | 낮음 | FW | MultiLine UV 현재 동작에 기댐(10.4, 수용) |

### 10.7 개선안 (코드 변경 없음, 사용자 결정 대기)

| # | 내용 | 고칠 곳 | 효과 |
|---|---|---|---|
| **P-V1** | 외형별 끝점 선택을 함수 하나로(예: `CombatShotPath.End(kind, 주 칸, 방향, 영향 칸, 관통 종점, mapper, 출발점, 발사 높이)`). 실제 공격의 결과(`AttackResult`)와 궤적의 응답(`AttackPreview`)이 같은 네 값을 넘긴다. 궤적의 두 갈래도 하나로 합쳐진다 | `CombatAttackPlayback`·`CombatBallistaAnimation`(동작 동일)·`CombatAttackTrajectory`, 시험 | SSOT 9·SOLID 8·독립성 9 |
| **P-V2** | 조준 입력에 `ClearHoverDisplay()` 하나(영향 칸·막힌 벽·궤적), 세 곳이 부름(`ResetRangeAnswer`는 세대 검사 때문에 그대로) | `AttackTargetingInteraction` 한 파일, 동작 동일 | SOLID·독립성 소폭 |
| P-V3 | 접촉 대체 규칙 공용 함수(`CombatContactGeometry`에 "처음 닿음 아니면 투영" 하나) | `CombatContactGeometry`·`CombatAttackPlayback`·`CombatAttackTrajectory`, 동작 동일 | SSOT 소폭 |
| P-V5 | 건너뜀 기억에 캔버스 좌표 변환 참조를 넣어 필드가 다시 구성되면 다시 그림 | `CombatAttackTrajectory` 한 줄 | 정확성 |
| P-V6 | 막힌 궤적 끝을 첫 Direct 증거의 접촉 칸으로(없으면 받은 칸) | `CombatAttackTrajectory` | 화면 |

- 권장: **P-V1·P-V2·P-V5·P-V6** 묶음(P-V3은 실제 접촉 시각 코드를 건드리는 데 비해 이득이 작아 선택). 예상 **SSOT 9 · SOLID 8 · FW 9 · 독립성 9 = 8.8/10**.
- P-V1은 실제 공격 연출 파일을 건드리므로(동작 동일) 공격 연출 Unity 시험을 다시 돌려야 한다.

### 10.8 개선 적용 (2026-10-19, 사용자 결정 "P-V1·P-V2·P-V5·P-V6 적용, MSBuild 건너뜀")

- **P-V1:** `CombatShotPath.TryTarget(외형, 주 칸, 방향, 영향 칸, 관통 종점, mapper, 출발점, out 목표점)` 하나가 외형별 목표점을 정한다(곡사 = 주 칸, 없으면 false / Ballista = 관통 종점, 없으면 아래 / 직선·근접 = `CombatAttackTarget.Resolve`). 실제 공격의 직선·근접(`CombatAttackPlayback` 직접 공격)·곡사(`TryBeginMortar`, 옛 예외 문구 그대로)·Ballista(`CombatBallistaAnimation`)와 궤적이 모두 이것을 부른다(실제 공격은 동작 동일). 궤적의 `Show`·`ShowBlocked` 두 갈래는 목표점만 다르게 받는 `Draw` 하나와 출발·끝 `Segment` 하나로 합쳤다.
- **P-V2:** 공격 조준 입력에 `ClearHoverDisplay()`(영향 칸·막힌 벽·궤적), 세 곳이 부른다(순서 그대로). 모드 정리는 세대 검사 때문에 따로.
- **P-V5:** 같은 입력 기억 `Drawn`에 필드 좌표 변환 참조를 넣어, 필드가 다시 구성되면 같은 경로라도 다시 그린다.
- **P-V6:** 막힌 궤적의 끝은 첫 Direct 증거의 접촉 칸(없으면 받은 칸).
- **시험:** `CombatShotPathTests.TryTarget_PicksEndPerPresentationKind`(Unity), `CombatAttackTrajectoryTests`에 `ShowBlocked_EndsTowardEvidenceContact_NotPassedCell`·`SameInput_RedrawsAfterFieldRebuilt`(Unity), 시험 가짜 캔버스의 좌표 변환을 바꿀 수 있게 함.
- **다시 매긴 점수: SSOT 9 · SOLID 8 · FW 9 · 독립성 8 = 8.5/10**(예상 8.8보다 낮게 고침).
  - V1·V2·V5·V6 해소.
  - 남는 낮음 V8(새로 적음): 외형별 **출발점과 끝 높이 조합**(곡사·Ballista = 총구, 직선 = 유닛 위치에서 0.2 앞, 곡사 끝은 바닥·나머지는 발사 높이)이 실제 연출 세 곳과 궤적 `TryResolveShot`·`Segment`에 따로 있다. 같은 기하 함수(`Raise`·`LinearStart`)를 쓰므로 식은 하나지만 "어느 외형이 무엇에서 출발하나"는 두 벌이다. 예: Ballista를 유닛 위치에서 쏘게 바꾸면 궤적도 같이 고쳐야 한다. 실제 연출 생성자 구조를 바꿔야 해서 이번에는 두었다.
  - V3(접촉 대체 규칙 두 곳)·V4(시험 도우미)·V7(UV 기댐)은 그대로.
