# 로비 안내

- 작성자: 호진(이호진)
- 상태: 2026-11-02 코드·에셋 기준. 마지막 변경은 시설 호버 글자 빛남(2026-11-02, Unity 확인 전)이다.
- 대상: 로비 화면을 꾸미고 연결하는 기획자·아티스트. 왜 이렇게 되어 있는지는 [로비 설계](../specs/2026-10-11-lobby-design-Hojin.md), 진행·확인 상태는 [로비 계획](../plans/2026-10-11-lobby-plan-Hojin.md)을 본다.
- 정리: 2026-10-27 옛 1부(기본 출발)·2부(설정 연결 이력)·3부(바닥·시설 모델 교체)를 "만들기 → 연결하기 → 확인하기" 순서로 다시 썼다. 옛 판은 git `10914570`.

## 1 한눈에 보기

### 1.1 무엇을 어디서 고치나

| 바꾸고 싶은 것 | 고치는 곳 | 절 |
|---|---|---|
| 방 모델, 시설(금고·문·탁자) 클릭 영역 | `Prefabs/Lobby/Visuals/Lobby.prefab` | §3.1 |
| 바닥 모델 | `Prefabs/Lobby/Visuals/LobbyFloor.prefab` | §3.2 |
| 다른 방·바닥·캐릭터 프리팹으로 교체 | 모델 SO `LobbyModel_Default` | §2·§3.3 |
| 시작 칸, 시설 앞 도착 칸, 도착 방향, 칸 간격 | 배치판 SO `LobbyBoard_Default` | §4.1 |
| 칸마다 뛰는 높이·시간, 회전 시간 | 이동 SO `LobbyMotion_Default` | §4.2 |
| 캐릭터 크기·바닥 접지·방향 보정 | 표시 보정 SO `LobbyCharacterPresentation_Default` | §5 |
| 시설 호버 때 글자 빛남(글자 색·빛 번짐) | 시설 호버 SO `LobbyFacilityHover_Default` | §3.4 |
| 로비 카메라 구도 | 씬 `LobbyCamera` | §6.1 |
| 아티팩트 보드 위치 | 씬 `ArtifactWorldRoot`, `ArtifactPopupConfig_Default` | §6.2 |
| 로비 UI 배치 | 씬 `LobbyCanvas` | §7 |

SO 다섯은 `Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/`에 있다.

### 1.2 먼저 알아 둘 것

- **편집 중 씬에는 방·바닥·캐릭터가 보이지 않는다.** Play 때 SO에서 읽어 `LobbyRoot` 아래에 만든다. 모델은 Prefab Mode에서 편집한다. 씬에 확인용 방이나 클릭 영역을 따로 두지 않는다.
- **Play 중에 SO를 고쳐도 반영되지 않는다.** 고친 뒤 Play를 멈추고 다시 시작한다(Boot부터 시작하는 Play From Boot 권장).
- 잘못된 값은 Play를 시작할 때 DB 생성이나 로비 연결에서 **역할·필드 이름이 들어간 오류**로 멈춘다. 콘솔의 첫 오류를 보고 그 SO·프리팹을 고친다(§8.3).
- 위치는 칸으로 정한다. 월드 좌표를 직접 넣는 곳은 없다.

## 2 SO 다섯 개와 테이블 연결

[LobbyVisualDataTable.asset](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/LobbyVisualDataTable.asset)을 선택하면 역할 칸 다섯이 보인다. **테이블에서는 어떤 SO를 쓸지 연결하고, 값은 연결된 SO를 선택해서 고친다.**

| 테이블 칸 | 지금 연결된 SO | 그 SO에서 고치는 것 | 새로 만들 때 메뉴 |
|---|---|---|---|
| 모델 | [LobbyModel_Default](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyModel_Default.asset) | `roomPrefab`(방), `floorPrefab`(바닥), `characterPrefab`(캐릭터) | Create → ProjectL → Database → View → `LobbyModelData` |
| 배치판·정착 칸 | [LobbyBoard_Default](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyBoard_Default.asset) | 열·행·칸 간격·중심, 시작 칸·방향, 시설별 도착 칸·방향 | … → `LobbyBoardData` |
| 이동 연출 | [LobbyMotion_Default](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyMotion_Default.asset) | 뛰기 높이, 한 칸 시간, 회전 시간 | … → `LobbyMotionData` |
| 캐릭터 표시 보정 | [LobbyCharacterPresentation_Default](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyCharacterPresentation_Default.asset) | 로컬 위치·Y 회전·균일 크기 | … → `LobbyCharacterPresentationData` |
| 시설 호버 빛남 | [LobbyFacilityHover_Default](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyFacilityHover_Default.asset) | 글자 색·빛 색·빛 크기·부드러움 | … → `LobbyFacilityHoverData` |

```text
DatabaseCatalogSO.asset
└─ ViewGroup.asset
   └─ LobbyVisual/LobbyVisualDataTable.asset
      ├─ Item/LobbyModel_Default.asset
      ├─ Item/LobbyBoard_Default.asset
      ├─ Item/LobbyMotion_Default.asset
      ├─ Item/LobbyCharacterPresentation_Default.asset
      └─ Item/LobbyFacilityHover_Default.asset
```

- 지금 값을 고칠 때는 새 SO를 만들 필요가 없다. 다른 설정을 시험해 보고 싶을 때만 새 SO를 만들어(Project 창 우클릭 → 위 메뉴) 같은 역할 칸에 연결한다.
- 각 칸은 그 역할의 SO 타입만 받는다. 씬 객체는 넣을 수 없다. 칸을 비우면 그 역할이 목록에서 빠지므로 Play 전에 다섯 칸을 모두 채운다.
- 목록에 중복·Missing·다른 타입이 있으면 역할 칸이 잠기고 **원본 연결 목록 (오류 수정)**이 나온다. 문제 행을 고치거나 지운 뒤 빈 역할을 채운다. 잘못된 행을 자동으로 지우지 않는다.
- 역할 칸 변경은 Undo/Redo가 된다(사용자 확인). 테이블은 하나씩 선택해서 편집한다(다중 선택 편집 없음).
- 여러 로비 프리셋을 골라 쓰는 기능은 없다. 다섯 SO는 각각 독립 `.asset`이다.

## 3 방·시설·바닥 모델

### 3.1 방과 시설 클릭 영역 — Lobby.prefab

[Lobby.prefab](../../../Assets/ProjectL/Prefabs/Lobby/Visuals/Lobby.prefab)을 **Prefab Mode**로 연다.

```text
Lobby          (루트: 지금 Y 270° · 크기 3. 저작한 위치·회전·크기가 그대로 쓰인다)
├─ Artifact    LobbyFacilityView  Kind = Artifact    Label = "아티팩트 관리" 글자  + BoxCollider   ← 금고
├─ StartStage  LobbyFacilityView  Kind = StageEntry  Label = "여정 시작" 글자      + BoxCollider   ← 문
└─ Deck        LobbyFacilityView  Kind = Deck        Label = "덱 편집" 글자        + BoxCollider   ← 탁자
```

- 시설 자식에는 `LobbyFacilityView`의 **Kind와 Label**을 고른다. Label에는 그 시설의 안내 글자(같은 방 안 `Canvas` 아래의 `TextMeshProUGUI`)를 끌어다 넣는다. 호버 빛남이 이 글자를 쓴다(§3.4). 두 시설이 같은 글자를 쓸 수 없다.
- 같은 오브젝트의 BoxCollider는 실행 때 자동으로 찾는다. Collider를 끌어다 넣는 칸은 없다.
- 클릭되는 범위 = 그 자식의 Transform + BoxCollider의 Center·Size다. 화면에서 누르기 쉬운 크기로 맞춘다.
- Artifact·StageEntry·Deck이 **각각 하나씩** 있어야 한다. 오브젝트 이름이나 순서는 상관없고 Kind가 기능을 정한다.
- BoxCollider는 시설마다 활성 하나, 크기는 양수여야 한다. 컴포넌트가 없어도 자동으로 만들어 주지 않는다.
- 방 프리팹에는 정적 모델 부품(Transform·MeshFilter·MeshRenderer·LODGroup), 시설의 `LobbyFacilityView`·BoxCollider, 시설 안내 글자 구성(RectTransform·Canvas·CanvasRenderer·CanvasScaler·GraphicRaycaster·TextMeshProUGUI)만 넣는다. 다른 스크립트·Rigidbody·카메라·Light를 넣으면 거부된다. Layer는 자유다.
- 안내 글자는 Raycast Target을 끄는 것이 좋다. 켜 두면 글자 위에서는 UI 가림으로 잡혀 시설 클릭·호버가 되지 않을 수 있다(§7과 같은 규칙).
- **클릭 영역을 옮겨도 캐릭터가 서는 도착 칸은 바뀌지 않는다.** 도착 칸은 배치판 SO에서 따로 고친다(§4.1). 둘이 맞는지는 사람이 Play로 확인한다.

### 3.2 바닥 — LobbyFloor.prefab

- [LobbyFloor.prefab](../../../Assets/ProjectL/Prefabs/Lobby/Visuals/LobbyFloor.prefab)은 루트가 활성·위치 0·회전 0·크기 1이어야 한다. 모델의 크기·방향·높이 보정은 **루트가 아니라 자식**에서 한다.
- 허용 부품은 Transform·MeshFilter·MeshRenderer·LODGroup이고 Collider는 넣지 않는다. Layer는 자유(검사 없음).
- 지금 바닥은 판 1개 + 타일 16개(`FloorTile_<열>_<행>`, MeshRenderer 17개)다. 타일 중심이 배치판 칸 중심과 맞아야 캐릭터가 타일 위에 선다. **바닥과 배치판은 자동으로 맞춰지지 않는다** — 바닥을 바꾸면 §4.1의 칸 규격도 함께 고친다.

### 3.3 새 모델로 바꾸는 순서

1. 모델러가 만든 FBX를 임포트한다.
2. 빈 루트를 가진 래퍼 프리팹을 만들고 모델을 자식으로 넣는다. 자식에서 크기·방향·바닥 높이를 맞추고 Material을 연결한다. 방이라면 시설 자식 셋(§3.1)도 만든다.
3. [LobbyModel_Default](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyModel_Default.asset)의 `roomPrefab`·`floorPrefab` 중 해당 칸에 새 프리팹을 연결한다. 코드는 고치지 않는다.
4. 새 Play로 확인한다(§8).

- 지금 지원하는 모델은 **정적 Mesh**다. 애니메이션·파티클·스킨 메시가 있는 방·바닥은 별도 설계가 필요하다(캐릭터는 예외, §5).
- 아무 크기의 모델이나 카메라·클릭 영역·이동 경로에 자동으로 맞춰 주지 않는다. 크기가 다르면 프리팹 자식과 배치판 SO를 함께 맞춘다.
- 옛 `LobbyRoom.prefab`은 지금 쓰지 않는 예전 래퍼다(지우지 않음).

### 3.4 시설 호버 빛남 — LobbyFacilityHover_Default

시설(금고·문·탁자) 위에 커서를 올리면 그 시설의 안내 글자가 **밝아지고 글자 뒤로 빛이 번진 채로** 있다. 번쩍이지 않는다. 커서가 벗어나면 바로 원래 글자로 돌아간다.

| 필드 | 지금 값 | 뜻 |
|---|---|---|
| `textColor` | (1, 1, 0.92, 1) | 호버하는 동안 글자 색. 흰색에 가까울수록 밝다 |
| `glowColor` | (1, 0.85, 0.45, 1) | 글자 뒤 빛 색. 알파가 빛 세기(1이 가장 셈) |
| `glowSize` | 0.45 | 빛 크기(0~1). 클수록 넓게 퍼진다 |
| `glowSoftness` | 0.5 | 빛 부드러움(0~1). 클수록 가장자리가 흐리다 |
| `glowMaterial` | `LobbyFacilityGlow` | Underlay를 켠 TMP 머티리얼. 빌드에 빛 번짐 셰이더를 남기는 용도라 **값은 쓰지 않는다.** 바꾸지 않아도 된다 |

- 더 세게: 글자 색을 흰색 쪽으로, 빛 색 알파를 1로, 크기를 올린다. 너무 번지면 부드러움을 낮춘다.
- 지금 누를 수 있는 시설만 빛난다. 캐릭터가 이동하는 동안, 팝업이 열린 동안, 출발 처리 중, 설정 화면(ESC)이 열린 동안, UI 위에서는 빛나지 않는다.
- 값은 다음 Play부터 반영된다(§1.2). 실행 중에 바꿔도 지금 화면은 바뀌지 않는다.
- 안내 글자의 글꼴을 다른 셰이더로 바꾸면 `glowMaterial`도 같은 셰이더의 Underlay 머티리얼로 바꿔야 한다(다르면 Play 시작 때 오류).

## 4 배치판 칸과 이동 느낌

### 4.1 칸과 방향 — LobbyBoard_Default

| 필드 | 지금 값 | 뜻 |
|---|---|---|
| `boardColumns`, `boardRows` | 4, 4 | 열·행 수 |
| `cellSize` | (4.25, 3.85) | 칸 간격 X, Z |
| `boardCenter` | (0, 0.1, -0.98) | 배치판 중심(`LobbyRoot` 기준). Y는 캐릭터가 서는 높이 |
| `startCell`, `startFacing` | (1,2), R0 | 로비에 들어올 때 서는 칸·방향 |
| `facilityArrivals` | Artifact (0,2) R270 · StageEntry (1,3) R0 · Deck (2,2) R90 | 시설 종류마다 한 줄. 누르면 가는 칸·방향(금고·문·탁자) |

- 칸 번호는 (열, 행)이고 0부터 센다. 열이 늘면 +X, 행이 늘면 +Z 쪽이다. 바닥 타일 이름 `FloorTile_<열>_<행>`과 같은 번호다.
- 방향: **R0 = +Z, R90 = +X, R180 = -Z, R270 = -X**(위에서 보아 시계 방향). 캐릭터가 도착한 뒤 바라보는 쪽이다.
- 모든 칸은 배치판 안이어야 한다. `facilityArrivals`에는 시설 종류(`LobbyFacilityKind`)마다 정확히 한 줄이 있어야 하고, 도착 칸은 서로 달라야 한다. 시작 칸은 도착 칸과 같아도 된다.
- 시설이 차지하는 칸이나 "도착 칸이 시설 옆인가"는 검사하지 않는다. 시설 클릭 영역과 도착 칸이 자연스러운지 Play로 본다.
- 로비에 다시 들어오면 캐릭터는 항상 시작 칸·방향으로 돌아온다.

### 4.2 이동 느낌 — LobbyMotion_Default

| 필드 | 지금 값 | 뜻 |
|---|---|---|
| `hopHeight` | 1 | 한 칸 뛸 때 가장 높이 뜨는 높이(0 이상) |
| `hopSeconds` | 0.35 | 한 칸 뛰는 시간(양수) |
| `turnSeconds` | 0.1 | 방향을 바꿀 때 도는 시간(0 이상, 0이면 바로 돈다) |

캐릭터는 시설을 누르면 **도착 칸까지 한 칸씩 뛰어** 간 뒤 그 시설 기능을 연다.
- 가로·세로로만 뛴다. 칸이 대각선에 있으면 시설을 바라보는 방향의 축을 마지막에 걷는다. 지금 배치에서는 시설 사이를 옮길 때 시작 칸을 거친다.
- 방향이 바뀌는 칸에서 먼저 돌고 뛴다. 도착 방향과 다르면 마지막에 한 번 더 돈다.
- 같은 도착 칸에 이미 서 있으면 이동 없이 바로 연다. 이동 중에는 다른 시설 클릭을 받지 않는다.
- 칸 간격이 넓으면 뛰기가 낮아 보일 수 있다. `hopHeight`·`hopSeconds`를 화면을 보며 맞춘다.

## 5 캐릭터

- 캐릭터 모델은 모델 SO의 **`characterPrefab`**에 연결한 프리팹을 그대로 복제한다. 지금은 [Akimbo.prefab](../../../Assets/ProjectL/Prefabs/Characters/Colored/Playerable/Akimbo.prefab)이다. 전투에서 쓰는 외형 설정과는 따로다(로비만 바꿀 수 있다).
- 다른 캐릭터로 바꾸려면 이 칸에 다른 캐릭터 프리팹 원본을 연결한다. 프리팹은 활성 원본이어야 한다(씬 오브젝트 불가).
- 크기·바닥 접지·방향은 [LobbyCharacterPresentation_Default](../../../Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyCharacterPresentation_Default.asset)에서 맞춘다.

| 필드 | 지금 값 | 뜻 |
|---|---|---|
| `characterOffset` | (0, -0.08218316, 0) | 모델 위치 보정(발이 바닥에 닿게) |
| `characterYaw` | 0 | 모델 Y 회전 보정(도). 모델 정면이 +Z가 아닐 때 |
| `characterScale` | 2.146858 | 균일 크기(양수). 축마다 다른 크기는 받지 않는다 |

- Play 때 `LobbyRoot` 아래에 `LobbyCharacterRoot → LobbyCharacterVisual → <캐릭터>(Clone)`이 생긴다. 씬에 캐릭터를 미리 두지 않는다.
- 로비 복제본은 Collider가 꺼지고 Layer가 Default가 되며 Animator의 root motion·animation event가 꺼진다. Idle 애니메이션은 그대로 돈다. 원본 프리팹은 바뀌지 않는다.
- 실제로 고른 메인 캐릭터를 자동으로 보여 주는 기능은 아직 없다(설계 §13).

## 6 카메라와 아티팩트 보드

### 6.1 로비 카메라

- 로비 씬의 카메라는 고정 가상 카메라 **`LobbyCamera` 하나**다(직교, Follow·LookAt 없음). 화면을 그리는 실제 Camera는 Boot 씬의 `Main Camera`이며 로비 씬에는 Unity Camera를 두지 않는다.
- 구도를 바꾸려면 `LobbyCamera`의 Transform·Lens를 고친다. 타깃(Follow·LookAt)을 넣거나 직교를 끄면 로비 연결 때 오류가 난다.
- 후처리는 `LobbyCamera`의 `CinemachineVolumeSettings`가 `Assets/Settings/DefaultVolumeProfile.asset`(스테이지 선택 카메라와 같은 프로필)을 쓴다. 프로필을 고치면 스테이지 선택 화면도 바뀐다.
- 배경 Canvas `LobbyBackgroundCanvas`의 카메라 칸은 **비워 둔다.** 실행 때 Boot 카메라를 자동으로 붙인다.
- 배경 Canvas의 `planeDistance`는 `LobbyCamera`의 near~far 안이어야 한다.

### 6.2 아티팩트 보드 위치

- 아티팩트 팝업의 3D 보드는 씬의 `ArtifactWorldRoot` Transform 하나로 위치·회전·크기를 정한다. 고정 `LobbyCamera` 화면에 맞춰 이 루트를 옮긴다. 입력 칸 계산도 같은 루트를 따르므로 보드 모델만 따로 옮기지 않는다.
- 아티팩트 팝업도 기존 `LobbyBackgroundCanvas`를 배경으로 사용한다. 전용 `ArtifactWorldBackground`와 배경 거리·여백 설정은 제거했다(2026-10-30).
- 팝업이 열리면 `LobbyRoot`(방·바닥·캐릭터)가 숨겨진다. 카메라·조명·UI는 숨기지 않는다.
- 보드는 UI 층이라 팝업이 열린 동안만 화면에 그려진다(코드가 처리한다).

## 7 Canvas와 UI

- 로비 화면 UI는 Overlay Canvas **`LobbyCanvas` 하나**(Sort Order 2, Scale With Screen Size 1920×1080, Match 0.5)에 모은다.

```text
LobbyCanvas
├─ LobbyPanel          로비 안내·Clicker(시설 안내)
├─ LobbyDeckPanel      덱 팝업(LobbyDeckUI)
└─ ArtifactPopupPanel  아티팩트 팝업(ArtifactPopupUI)
```

- Panel 오브젝트에 Canvas를 또 붙이지 않는다. 덱·아티팩트 팝업 프리팹에도 Canvas가 없다.
- 안내 글자는 Raycast Target을 끈다(켜면 그 아래 시설이 눌리지 않는다). 배경 Canvas도 클릭을 막지 않는다.
- 버튼의 Inspector `OnClick`은 비워 둔다. 버튼 입력은 코드가 `UIInputBinding`으로 연결한다.
- 메인 캐릭터 얼굴·골드·명성·설정 버튼 같은 **글로벌 UI는 Boot의 `GlobalUICanvas`에 있다.** 로비 씬에 다시 만들지 않는다. 글로벌 UI는 현강님 담당이다.
- 덱 팝업은 지금 빈 목록과 "덱 정보를 준비 중입니다."만 보인다(실제 덱 고르기는 미착수).

## 8 확인하기

### 8.1 Play 확인 순서

SO·프리팹을 고쳤으면 Play From Boot로 로비에 들어가 아래를 본다.

1. 방·바닥·캐릭터가 한 번씩만 보이고, 캐릭터가 시작 칸에서 시작 방향을 보고 서 있다.
2. 금고·문·탁자를 각각 누르면 도착 칸까지 한 칸씩 뛴 뒤 도착 방향을 보고 기능이 열린다(금고 = 아티팩트 팝업, 탁자 = 덱 팝업, 문 = 출발). 시설 사이를 옮길 때는 시작 칸을 거쳐 꺾는다.
3. UI 위에서 누르거나, 시설에서 누르고 다른 곳에서 놓으면 아무 일도 없다. 이동 중 다른 시설 클릭은 무시된다.
4. 팝업을 닫으면 다시 시설을 누를 수 있다. 아티팩트 팝업 동안 로비가 숨겨지고 닫으면 돌아온다.
5. 캐릭터가 이동 중 방 모델을 뚫고 지나가지 않고, 발이 바닥에 닿아 있다.
6. 16:9·16:10·4:3에서 모델·UI가 잘리거나 서로 가리지 않는다. 시설 클릭 영역이 화면의 금고·문·탁자 모양과 맞는다.
7. 출발 뒤 스테이지 선택 화면으로 가고, 로비로 돌아오면 캐릭터가 다시 시작 칸에 있다.
8. 금고·문·탁자 위에 커서를 올리면 그 글자가 밝게 빛난 채로 있고, 벗어나면 돌아온다. 이동·팝업 중에는 빛나지 않는다(§3.4).

자동 검사는 좌표·참조만 본다. 보기 좋은지·누르기 쉬운지는 사람이 확인한다. 성능(Profiler)은 모델을 바꾼 뒤 따로 잰다.

### 8.2 출발이 어떻게 동작하나

문(스테이지 입장)에 도착하면 출발이 한 번 제출되고, 처리 중에는 다른 시설을 누를 수 없다. 남은 아티팩트 편집이 먼저 처리된 뒤 **항상 새 Run**(기본 로드아웃 1)을 만들고 스테이지 선택으로 간다. 아티팩트 배치는 새 Run에도 남는다. 정상 거절이면 로비에서 다시 고를 수 있다(자동 재시도 없음). 진행·거절 안내 문구는 지금 화면에 없다. Run이 끝나면(패배·포기·최종 승리) 로비로 돌아온다. 자세한 규칙은 [설계 §12](../specs/2026-10-11-lobby-design-Hojin.md#12-출발run-종료run-결과)를 본다.

### 8.3 오류가 날 때

| 콘솔에 나온 것 | 고칠 곳 |
|---|---|
| 테이블 역할 누락·중복·다른 타입 | 테이블 다섯 칸(§2) |
| 방·바닥 프리팹 null, 씬 오브젝트, 금지 부품, 루트 변환(바닥) | 모델 SO 연결, 프리팹 구조(§3) |
| 시설 Kind 누락·중복, BoxCollider 누락·복수·비활성·크기 0, 안내 글자(Label) 누락·방 밖·비활성·겹침 | `Lobby.prefab` 시설 자식(§3.1) |
| `[LobbyFacilityHoverData]` 색·크기·부드러움 값, 빛 머티리얼 없음·Underlay 꺼짐·글자와 다른 셰이더 | 시설 호버 SO(§3.4) |
| 칸이 배치판 밖, 도착 칸 중복, 정의되지 않은 방향 | 배치판 SO(§4.1) |
| 높이·시간 값 | 이동 SO(§4.2) |
| 캐릭터 프리팹 null·비활성·씬 오브젝트, 보정 크기 0 이하 | 모델 SO `characterPrefab`, 보정 SO(§5) |
| 로비 가상 카메라 없음·직교 아님·타깃 있음 | 씬 `LobbyCamera`(§6.1) |
| 배경 Canvas 렌더 모드·깊이 | `LobbyBackgroundCanvas`(§6.1) |

### 8.4 관련 기존 시험

프로그래머가 모델·배치를 바꾼 뒤 돌려 볼 기존 EditMode 시험이다. 바닥 Renderer 17개·지금 도착 칸·Collider 값 같은 기대값은 **지금 에셋을 지키는 값**이라, 아트를 바꾸면 그 시험의 기대값을 새 아트에 맞춰 고친다(새 아트가 계약 위반이라는 뜻이 아니다).

| 시험 | 보는 것 |
|---|---|
| `DataBaseAssetTests.LobbyVisual_ActualCatalogProvidesFloorWithoutTemporaryFacilityModels` | 실제 DB의 방·바닥 선택과 방 시설 구성 |
| `LobbyVisualDataTableTests` | 네 역할 조회, 중복·누락·다른 타입 거부, 모델 구조 검사 |
| `LobbyVisualReplacementTests` | 방 교체 때 저작 변환·같은 객체 Collider·바닥/캐릭터 보존, 잘못된 방의 생성 전 거부 |
| `LobbyVisualAssetTests` | 읽기 불가 Mesh 검사, 타일·Renderer 수·시작/도착 자세·클릭 영역, 캐릭터 접지 |
| `LobbyFacilityBootSmokeTests` | 시작점·도착점 사이 이동 경로와 선택 박스·바닥 간섭, 실제 마우스 입력·이동 중 클릭 차단 |
| `LobbyBootSmokeTests`, `LobbyVisualSetTests`, `LobbyViewSystemTests`, `LobbySceneLayoutTests` | Boot 진입, 생성·정리·재진입·오류 수명, `LobbyRoot` 축척·회전 검사 |

## 9 설정 UI

설정 화면(`OptionUI`, ESC로 열기)은 현강님 UI다. 로비 이동과의 연결(설정을 열 때 이동 취소, 팝업과 겹치지 않기)은 로비 쪽에서 아직 인수하지 않았다. 인수 조건과 지금 상태는 [설계 §7.4](../specs/2026-10-11-lobby-design-Hojin.md#74-8번-설정)를 본다. 공용 UI 이력 재진입 문제는 [UI 작업자 인계](../handoffs/2026-10-12-lobby-settings-ui-history-handoff-Hojin.md)(미전달)에 있다.
