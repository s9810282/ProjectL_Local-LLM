# PlacedPrefab Terrain 입력·Unity 확인

> 공통 12개 Wall의 ㄴ자 형상 지원과 C3 실제 Unity 인수를 완료했다. 11개 운영 스테이지의 DB/PrepareBattle·두 표시 모드, 선택된 한 스테이지의 Database/Live 진입·빈칸 이동을 통과했다. 현재 검증 상태는 연결 Wall Phase 계획 (Git history: `2026-10-13-connected-wall-geometry-plan.md`)을 따른다. 아래 입력 절차와 View 표시 연결은 유지한다.

2026-10-13 기준. Wall·Sandstorm·Tornado·PresenceModifier의 프리팹 표시와 스테이지별 외형 회전이 연결됐다. 런타임 위치·활성·공개는 Core가 준비한 Buffer에서, 외형·회전은 ViewGroup에서 읽는다. 사용자 그림에 따라 운영 11개 Combat 스테이지(일반 A~J·Boss)에 공통 9×9 배치를 적용했다. Wall은 12개(Source Id=201~212), Known To=Player·Enemy이며 외형 회전은 생략해 기본 R0이다. y=2/6의 x=1,2,6,7과 y=3/5의 x=2,7에 배치했다. Object 3종은 (3,2)/(4,2)/(5,2), Obstacle 2종은 (0,5)/(8,5)에 종류별 하나씩 배치했다. 기존 종류별 수치·회전과 중앙 y=4 레일, 아래 y=0~1 아군 설치·위 y=7~8 적 스폰 구역은 유지한다. 다른 Terrain 종류는 아직 배치하지 않았다. 설계·검증 기록은 [공통 배치 계획](../plans/2026-10-13-combat-common-layout-plan.md)에 있다.

## 1. 위치·공개 설정

`Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Combat/Combat_Normal_A.asset` 등 실행할 스테이지 SO의 **Environment > Terrain Placements**에 입력한다. 네 종류 모두 이 배열을 사용한다. 기존 Wall Cells/Static Terrain은 호환용이며 새 배치는 Environment에서 작성한다.

| 필드 | 입력 |
|---|---|
| Definition | `InGameGroup/Terrain/Item/Terrain_{Wall,Sandstorm,Tornado,PresenceModifier}.asset` 중 하나 |
| Source Id | 스테이지 안에서 유일한 양수 ID. 다른 Terrain·Object·호환 지형의 ID와 겹치지 않게 작성 |
| Cell X/Y | 0부터 시작하는 열/행. X는 필드 로컬 +X, Y는 +Z. 해당 Field Definition 범위 안에 작성 |
| Known To | 화면에 처음부터 표시하려면 Player를 포함. Enemy만 있거나 배열이 비어 있으면 Player 화면에는 표시되지 않음 |

토네이도의 초기 발동·반복 간격이나 위치 보정 능력치 등 규칙은 Definition SO에서 설정한다. 이동할 다음 칸이나 발동 시점을 View에서 계산하지 않는다. 장애물의 점유 칸, 본부·유닛 등과의 배치 가능 여부는 Core 입력 검증을 따른다.

Stage Key는 StageRoot SO의 값이다. 예를 들어 Normal A는 1101이다. `Dev Node Id`는 맵 노드 번호이므로 Stage Key와 다르다. 다른 Stage Key의 스테이지에서는 같은 Source Id를 다시 사용할 수 있다.

## 2. 외형·회전 설정

| 대상 | SO/필드 | 역할 |
|---|---|---|
| 종류별 외형 | `ViewGroup/TerrainVisual/Item/TerrainVisual_*.asset` | Visual Mode(OnTile·ReplaceTile, §2.2), Prefab, Local Offset/Scale, Surface Height |
| 스테이지별 방향 | `ViewGroup/StageEnvironmentVisual/Item/`의 해당 Stage Key SO | Terrain Rotations의 Source Id와 Rotation |
| 외형 등록 | `ViewGroup/TerrainVisual/TerrainVisualDataTable.asset` | 네 Terrain 외형 Items 등록 |
| 회전 등록 | `ViewGroup/StageEnvironmentVisual/StageEnvironmentVisualDataTable.asset` | 실행할 Stage Key의 회전 SO 등록 |

Terrain Rotations의 Source Id에는 위 Terrain Placements의 ID를 그대로 넣는다. Rotation은 R0/R90/R180/R270이다. 같은 ID를 Object Rotations에 넣으면 분류 오류가 된다. 회전 SO 자체가 없거나 해당 ID 입력을 생략하면 R0을 사용한다. 스테이지를 바꿔도 이전 회전은 남지 않는다.

최종 회전은 **필드 회전 × SO 외형 회전 × Prefab 루트의 원래 로컬 회전**이다. Local Offset은 외형 방향과 관계없이 필드 로컬 축 기준으로 적용한다. Local Scale은 Prefab 루트 스케일에 곱한다. Terrain에는 Field Unit Scale을 적용하지 않는다.

현재 네 외형은 Default Cube 기반 임시 프리팹으로 연결돼 있다. Wall 회색, Sandstorm 모래색, Tornado 보라, PresenceModifier 청록이다. 정식 외형을 쓸 때 해당 SO의 Prefab을 교체한다. 타일 위(OnTile) 외형의 Prefab을 다시 비우면 DB 생성 검증에서 오류가 난다. 대칭인 Cube는 회전해도 비슷하게 보이므로 Play 중 인스턴스 Transform 회전이나 비대칭 프리팹으로 확인한다.

HighGround·WeakGround·Cliff도 같은 표시기로 그린다. 그릴지는 외형 SO에 Prefab이 있는지로 정하고(고지대 View 설계 HG2), 놓는 자리와 Surface Height의 뜻은 표시 방식이 정한다(§2.2). 스테이지별 회전의 원본은 2026-10-23부터 `ViewGroup/StageViewProp/Item/`의 `ViewStageDataSO` `terrainRotations`다(ViewStage 표 설계, 위 표의 옛 환경 SO는 카탈로그 밖).

### 2.1 이웃 모양 (붙은 지형의 끝·모서리 자동)

2026-10-27 추가([지형 이웃 모양 설계](../specs/2026-10-27-combat-terrain-visual-variants-design.md)·[계획 §2](../plans/2026-10-27-combat-terrain-visual-variants-plan.md#2-규칙-구현의-기준)). 같은 종류 지형이 붙어 있으면, 칸마다 드러난 변(이웃이 없는 쪽)에 맞는 모양 프리팹을 골라 돌린다. 이웃은 북·동·남·서 4방향만 보고, 같은 종류이면서 Player에게 보이는 칸만 센다. 숨은 칸과 필드 밖은 빈칸이다. 북은 필드 +Z(Core +Y)다.

| 외형 SO 칸 | 모양 | 드러난 변 | R0 기준 드러난 쪽(= 프리팹을 만드는 방향) |
|---|---|---|---|
| Prefab | Center | 0 | 없음. 빈 모양 칸의 대체이기도 하다 |
| One Side Prefab | OneSide | 1 | 동(+X) |
| Two Side Prefab | TwoSide | 2, 이웃함(바깥 모서리) | 북·동 |
| Two Side Opposite Prefab | TwoSideOpposite | 2, 마주 봄(폭 1 줄) | 동·서 |
| Three Side Prefab | ThreeSide | 3(끝) | 북·동·서(남에 이웃) |
| Four Side Prefab | FourSide | 4(외딴 칸) | 넷 다 |

- 모양 칸은 비워도 된다. **빈 모양은 Center(Prefab)를 R0으로 그린다.** 모양 칸이 하나라도 있으면 Prefab이 필수다(DB 생성 검사).
- 모양 칸이 하나라도 있는 종류는 **회전을 모양이 정한다.** 그 종류의 Source Id를 `terrainRotations`에 넣으면 그 스테이지에 들어갈 때 예외가 난다.
- 칸마다 고른 회전은 위 표의 R0 그림을 시계 방향으로 몇 번 돌리면 그 칸의 드러난 변과 같아지는지다. 16가지 전체 표는 계획 §2.3이다.
- **아트 규칙:**
  - 모든 모양은 Center와 같은 크기·피벗(바닥 가운데)·높이로 만든다.
  - 이웃이 있는 쪽은 수직으로 막고, 드러난 쪽만 경사·가장자리를 둔다.
  - 프리팹이 표의 방향과 다르면 프리팹 루트를 Y축으로 돌려 맞춘다. 표시기가 루트 로컬 회전을 곱하므로 코드는 고치지 않는다.
- 높이: `surfaceHeight`는 종류에 하나이고 모든 모양이 같이 쓴다. **메시 높이 × Local Scale y**와 같아야 유닛이 묻히거나 뜨지 않는다. 자동 검사는 없다.
- 2026-10-27 고지대: Prefab = `HeightGround_Center`, One Side = `HeightGround_OneSide`, Two Side = `HeightGround_TwoSide`(`Prefabs/Default/Environment/`, 메시는 민형님 `GroundGroup.fbx`).
  - **임시 값(사용자 결정 2026-10-27):** 아트가 없는 셋(Two Side Opposite·Three Side·Four Side)에도 `HeightGround_Center`를 넣어 두었다. 아트가 오면 그 칸만 바꾼다.
  - 그래서 그 셋은 빈 모양 대체(Center R0)를 타지 않는다. 고른 회전대로 Center 상자를 돌려 그린다. 상자가 대칭이라 모양은 같지만, 재질·UV 방향은 칸마다 돌아가 보일 수 있다.
  - 보스 고지대 10칸 중 L자 모서리 2칸만 경사가 있고, 나머지 8칸(세 변 = 끝)은 돌아간 Center 상자다.
- 한계: 안쪽 모서리는 표현하지 못한다. ㄱ자로 꺾인 넓은 고지대의 안쪽 칸은 Center가 되어, 옆 칸 경사와 만나는 자리에 작은 수직 면이 보일 수 있다.

### 2.2 표시 방식 — 타일 위 / 바닥 타일 교체

2026-10-29 추가([타일 교체 설계](../specs/2026-10-29-combat-terrain-tile-replace-design.md) TR1~TR8·[계획](../plans/2026-10-29-combat-terrain-tile-replace-plan.md)). 외형 SO의 Visual Mode는 두 값이고, Surface Height의 뜻이 방식마다 다르다.

| Visual Mode | 종류(지금) | Prefab | Surface Height | 놓는 자리 |
|---|---|---|---|---|
| OnTile(0) | 모래폭풍·토네이도·존재 변경·벽·고지대 | 필수(비면 DB 생성 오류) | 바탕(바닥 타일 윗면) 위로 올리는 블록 높이, 0 이상. 0이면 바탕 그대로 | 칸 바탕(타일 윗면, 타일이 없으면 바닥면) + Local Offset. 토네이도는 2×2 덮는 칸 바탕 중 가장 높은 값 |
| ReplaceTile(1) | 약한 지반·낭떠러지 | 선택. 비면 타일만 비운다(빈 구멍) | 그 칸의 새 바탕 = 바뀐 타일의 윗면(필드 바닥면 기준, 음수 가능). 낭떠러지는 구멍 바닥 | 타일이 있는 칸: 그 칸의 바닥 타일을 끄고, 끈 타일 자리(`MapTileRoot` 아래)에 놓는다. 타일이 없는 칸: 칸 가운데 바닥면 + Local Offset |

- 토네이도는 OnTile이어야 한다(DB 생성 검사).
- **바닥 교체 프리팹 만드는 법:** 타일 세트(`CombatMapTileSetSO`)의 타일 프리팹과 같은 크기·피벗으로 만든다. 그러면 끈 타일 자리에 그대로 맞는다. 타일 칸에서 Local Offset·Local Scale·외형 회전은 타일 루트 기준이다. 윗면 높이는 그 외형의 Surface Height와 같아야 유닛이 묻히거나 뜨지 않는다(자동 검사 없음).
- 타일은 공개된(Player에게 보이는) 바닥 교체 칸만 끈다. 전투를 나가거나 표시를 해제하면 다시 켠다.
- 그리드(칸 테두리·채우기·경로선·호버·데칼·포인터)는 지형이 올린 높이만 따른다. 바닥 교체 높이는 타일 높이처럼 그리드에 영향을 주지 않는다.
- **약한 지반 → 낭떠러지(붕괴 순간):** 약한 지반 외형이 떨리다가 아래로 가라앉고 사라진다. 떨리는 동안은 다음 사건(그 칸 유닛 추락)이 기다리고, 가라앉기는 유닛 추락과 함께 진행된다. 값은 설정 표 `CombatPresentationSettings_Default`의 "약한 지반 붕괴 연출" 다섯 칸(2026-10-29 에셋 값: 떨림 시간 0.35·세기 0.1·빠르기 25, 가라앉기 시간 0.5·깊이 1.5, 거리는 월드 단위. 코드 기본값은 세기만 0.04)이다. 떨림은 끝으로 갈수록 약해져 가라앉기로 튐 없이 넘어간다. 떨어지는 유닛은 "낭떠러지 추락 연출" 값(2026-10-29 기본 0.6초·깊이 3, 시간 절반부터 작아져 끝에 0.05배)대로 구멍 아래로 떨어진다.
- 2026-10-29 값: HighGround OnTile·0.45, WeakGround ReplaceTile·`Prefabs/Stage/MapProp/Test/WeekGround.prefab`·0, Cliff ReplaceTile·Prefab 없음.
- 한계:
  - 타일 한 장에 필드 칸이 여럿 놓이면(타일이 칸보다 큼), 한 칸의 바닥 교체가 그 타일 전체를 비운다. 맞춤 때 콘솔 경고가 한 번 나온다.
  - 타일이 없는 배경(A·B·C·보스)에서는 낭떠러지가 보이지 않는다(Prefab 없음). 지금 그 스테이지들에는 약한 지반·낭떠러지 배치가 없다.
  - 전투 스테이지 편집 SO 모드의 3D 미리보기는 표면 높이를 모두 "올림"으로 읽고 타일 교체를 모른다(호진님 공유, [SO 모드 3D 사본 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) §6).

## 3. Database 모드로 실행

`ProjectL > Play From Boot 설정...`에서 아래 값을 설정한다. 이 PC의 EditorPrefs 덮어쓰기이며 씬에 저장된 GameBootstrap 값은 바뀌지 않는다.

| 설정 | 값 |
|---|---|
| Play From Boot | 켬 |
| GameBootstrap 값 덮어쓰기 (이 PC만) | 켬 |
| Start Life Cycle | Combat |
| Execution Mode | Live |
| Use Dev Progress | 켬 |
| Battle Definition | Database (운영 DB 정의) |
| Dev Loadout Key | 1 |
| Dev Chapter Key | 1 |
| Dev Node Id | 운영 DB 맵의 전투 노드 번호 |

Play하면 Boot에서 새 Run·챕터를 준비하고 해당 노드 진입을 요청한다. Database에서는 실제 맵 그래프를 쓰며 노드에 선택되는 Stage Key는 맵 생성 결과를 따른다. 노드 2가 실행하려는 Combat SO를 항상 뜻하지는 않는다. 콘솔의 진입 거절 사유와 현재 선택된 Stage Key를 확인하고 그 스테이지에 입력한 배치를 대조한다. (개발 진입의 전투 정의는 Database 하나다. Fixture 모드는 2026-10-28에 지웠다.)

표시된 Terrain은 Combat 씬 Field의 CellsRoot 아래 **FieldTerrains**에 있다. 이름 끝의 번호가 Source Id다. 토네이도는 Core 결과 갱신 시 같은 ID 인스턴스의 위치가 바뀌며 외형 회전은 유지된다. 비공개·비활성·삭제된 원본은 비활성화해 풀에 반환하고 씬 연결이 해제되면 전용 루트를 제거한다. Terrain HUD는 없다.

두 필드 모드는 `ViewGroup/StageFieldView/Item/StageFieldView_Default.asset`의 Field Display Mode로 선택한다. CellPrefab과 GridOutline 모두 FieldGridMapper를 통해 같은 배치·회전 경로를 사용한다. 기본 바닥을 가리거나 낭떠러지를 만드는 처리는 포함하지 않는다.

## 4. EditMode 확인

Unity 컴파일 완료 후 `Window > General > Test Runner > EditMode`에서 다음 테스트를 실행한다. 실제 Combat 씬을 여는 테스트는 사용자 씬 보존을 위해 Combat.unity가 이미 열려 있으면 Ignore하므로 Boot 등 다른 씬에서 실행한다.

- `DataBaseAssetTests`, `EnvironmentAuthoringAssetTests`, `TerrainVisualDatabaseTests`: 실제 Catalog/외형 에셋 연결.
- `TerrainRotationBindingTests`, `TerrainViewSetTests`: 회전 분류·원본 수명.
- `FieldTerrainPresenterTests`: 네 회전/필드 축/Prefab 원래 회전, 누락·스테이지 변경, 토네이도 이동과 회전 유지, GroundShape 제외, 실패 정리.
- `TerrainOperationalDisplayTests`: 메모리 SO 복사본으로 운영 DB/Core 준비 → Buffer → 실제 Combat 표시, Object·Obstacle HUD 공존, 두 필드 모드, Clear/Release. 운영 SO를 저장하지 않음.
- `CombatSceneEventPresenterTests`: 기존 Object 표시, 두 필드 모드 Terrain 표시와 Install 실패 정리, Obstacle 체력 전용 HUD 회귀.

외부 컴파일·관리 테스트 통과와 Unity Test Runner/Play 확인은 별도 증거다. 실행 결과와 실제 레벨 입력 확인은 [Phase 계획](../plans/2026-10-12-placed-prefab-terrain-view-plan.md)에 기록한다.

2026-10-13 C3 검증: DataBaseAssetTests 23건·환경 에셋 15건을 포함한 Native EditMode 178/178, Database/Live Boot 재생 1/1 통과. 11개 스테이지 × CellPrefab/GridOutline은 자동 준비·표시 검증이며 모든 스테이지를 각각 Live로 재생했다는 의미는 아니다. 실제 입력/미리보기/결과를 통한 ㄴ자 빈칸 이동과 Wall 칸 거절도 확인했다. 마우스 조작과 외형의 육안 평가는 별도 확인할 수 있다. 본부는 현재 SO Y=8을 보존했다.
