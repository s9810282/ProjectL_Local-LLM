# 전투스테이지 편집

Unity 에디터에서도 **ProjecL → 전투 스테이지 편집**으로 같은 배치 엑셀을 편집할 수 있습니다. [에디터 도구 사용 안내](../README.md)를 참고하세요. 두 도구는 아래의 SchemaVersion 1 저장 계약을 공유합니다.

위치: Unity 프로젝트의 `Assets/ProjectL/Script/Tool/전투스테이지 편집/`. `index.html`을 Chrome / Edge에서 열면 됩니다. 인터넷·설치·서버 없이 실행되며 `vendor`를 포함한 폴더 전체를 보관해야 합니다.

## 편집 흐름

1. 시작 화면에서 **새로 만들기** 또는 **기존 엑셀 불러오기**를 선택합니다. 브라우저 마지막 작업이 있으면 별도의 복원 버튼이 나타납니다. 자동으로 이전 맵에 진입하지 않습니다.
2. 새로 만들면 **왼쪽 도구창 + 중앙 3D 편집창**이 열립니다. Stage 키·Seed·맵 가로/세로를 입력하고 맵 설정을 적용합니다. 크기만큼 3D 타일이 생성됩니다.
3. 왼쪽의 **Character · Method 엑셀 불러오기**에서 `Assets/DataTable/CharacterMaster.xlsx`와 `MethodMaster.xlsx`를 선택합니다. 두 파일을 한 번에 선택하거나 차례로 불러올 수 있습니다.
4. 유닛 리스트는 이름·직업·생명력·방어력·공격력·이동/공격 거리와 **실제 3D로 렌더링한 캡슐 미리보기**를 보여줍니다. 카드를 클릭하면 해당 유닛의 배치 도구가 선택됩니다. 아래 색상 선택기로 캐릭터별 색을 지정합니다. 맵에 배치된 같은 Character 키의 캡슐도 함께 바뀝니다. 원본 DT의 진형(1=아군, 2=적군)을 기본 소속으로 사용하며 배치 소속을 변경할 수 있습니다.
5. 타일을 클릭하면 유닛이 배치됩니다. 같은 Character 키를 여러 번 배치할 수 있습니다. 각 배치는 별도의 Unit 키를 가집니다. 배치된 캡슐 위에 **직업, 생명력, 방어력, 공격력, 이동 거리, 공격 거리·방식**이 표시됩니다.
6. **선택 / 이동**에서 유닛을 클릭하면 중앙 정보창과 왼쪽 선택 속성이 표시됩니다. 이동 범위(초록)·공격 범위(주황)·겹치는 범위(노랑)를 선택한 유닛 주변의 타일에 보여줍니다. 그 다음 빈 타일을 클릭하면 유닛이 이동합니다.
7. **높이 칠하기**에서 높이를 정하고 클릭·드래그하면 단차를 편집할 수 있습니다. 유닛은 타일 높이를 따라갑니다. 선택 속성에서도 높이·타일 종류·Terrain 키를 수정할 수 있습니다.
8. **엑셀 Export**를 누르면 `CombatLayout_<StageKey>.xlsx`가 다운로드됩니다. 사용자가 생성된 엑셀을 **`Assets/DataTable`에 넣어 보관**합니다. 브라우저가 그 폴더에 자동으로 기록하지 않습니다. 기존 배치 엑셀을 불러오면 맵·유닛·색상·표시용 원본 정보가 복원됩니다.

3D 조작은 좌클릭 편집, 우클릭 드래그 회전, 휠 줌, Shift+우클릭 드래그 화면 이동입니다. Esc는 선택 해제, Delete는 유닛 삭제입니다. Ctrl+Z / Ctrl+Shift+Z / Ctrl+Y로 배치·지형을 되돌릴 수 있으며 한 번의 페인트 드래그는 하나의 편집 이력입니다. 이력은 최대 100개입니다. DT 불러오기·표시 색은 배치 이력과 별도로 관리합니다.

`시작 화면` 버튼은 현재 작업을 브라우저에 보관하고 선택 화면으로 돌아갑니다. `새로 만들기`를 다시 선택하면 빈 맵으로 시작합니다. 원본 DT와 색상 설정은 계속 사용할 수 있습니다. 브라우저 자동 저장은 정책에 따라 제한될 수 있으므로 엑셀 파일로도 보관하세요.

## 원본 DT 표시

직업은 엑셀의 `Character_RoleType`을 사용합니다. 불러온 Character 사양서의 `숫자 = 이름` 설명을 읽으며 사양서가 없는 파일은 현재 프로젝트 사양서의 일반/기병/수비병/포병/메인 정의를 기본으로 사용합니다. Unity의 임시 Enum 파일은 참조하지 않습니다.

능력치는 `Character_HealthValue`, `Character_DefenseValue`, `Character_AttackValue`입니다. 이동은 `Character_MoveMethod` / `Character_MoveRange`, 공격은 `Character_AttackMethod` / `Character_AttackRange`를 읽습니다. Method 키로 `MethodMaster`의 유형·선택 방식·추가 지정 형태·배율을 표시합니다.

현재 프로젝트의 CharacterMaster와 MethodMaster는 헤더만 있는 상태에서도 불러올 수 있습니다. 실제 행이 없으면 리스트가 비어 있습니다. 키를 직접 입력해 배치할 수도 있지만 **원본에 없는 정보는 `—`로 표시하며, 능력치나 공격 방식을 임의로 만들지 않습니다.**

### 범위 표시의 의미

이 범위는 **레벨 디자인용 DT 형태 미리보기**입니다. 전투 시뮬레이션이나 실제 이동 가능 경로 계산은 아닙니다.

| Method_Type | 표시 형태 |
|---|---|
| 1 | 상하좌우 직선, 거리 이내 |
| 2 | 대각 직선, 거리 이내 |
| 3 | 상하좌우 + 대각 직선 |
| 4 | 선택한 방향으로 90도 부채, 직선/대각 거리 이내 |
| 5 | 평면 유클리드 거리 기준 원형 |

1~5의 유형 이름은 현재 Method 사양서에서 확인했습니다. 부채 각도와 원형 거리 기준은 사양서에 자세한 기하 규칙이 없어 편집기의 표시 규칙으로 정했습니다. 부채 방향 선택은 미리보기 설정이며 유닛 방향을 저장하지 않습니다. 이동 표시에서 벽 타일 자체는 제외합니다. 중간 장애물·유닛 점유·단차·행동비용·시야에 따른 경로/공격 판정은 하지 않습니다. `Method_ChoiceRange`, `Method_Splash`, `Method_SplashValue`는 정보로 표시하며 실제 대상 선택·스플래시 피해 계산은 하지 않습니다. 참조 Method가 없거나 지원하지 않는 유형이면 해당 범위를 그리지 않습니다.

## 엑셀 저장 계약

첫 행은 컬럼명, 두 번째 행부터 데이터입니다. Core 배치 시트 3개와 편집기 표시 정보를 복원하는 시트 2개를 생성합니다. **기존 CharacterMaster / MethodMaster 파일을 변경하지 않습니다.**

| 시트 | 저장 내용 |
|---|---|
| CombatStageMaster | Stage 키·이름·Seed·가로/세로·추가 Core 설정·버전 |
| CombatCellMaster | 모든 타일의 키·Stage 키·X/Y·높이·종류·Terrain 키 |
| CombatUnitMaster | 배치 키·Stage 키·Character 키·소속·X/Y·배치 셀 높이 |
| EditorCharacterMaster | 표시용 Character 정보 사본·캡슐 색상·직업명 |
| EditorMethodMaster | 표시용 Method 정보 사본 |

`Editor*` 시트는 **편집기 복원용**입니다. 게임 데이터가 아니라 DT 표시값의 사본이며, 최신 원본 DT를 다시 불러오면 표시값이 갱신되고 기존 선택 색은 유지합니다. 기존 3개 시트만 있는 배치 파일도 읽습니다. 그 파일에서 원본이 없는 Character 키는 다시 DT를 불러와 연결해야 합니다.

### Core 시트 컬럼

- Stage: `CombatStage_Key`, `CombatStage_Name`, `CombatStage_MapWidth`, `CombatStage_MapHeight`, `CombatStage_ReserveCapacity`, `CombatStage_PlayerDeploymentDepth`, `CombatStage_EnemyDeploymentDepth`, `CombatStage_Seed`, `CombatStage_NightLength`, `CombatStage_SchemaVersion`.
- Cell: `CombatCell_Key`, `CombatCell_StageKey`, `CombatCell_X`, `CombatCell_Y`, `CombatCell_Elevation`, `CombatCell_Type`, `CombatCell_TerrainKey`.
- Unit: `CombatUnit_Key`, `CombatUnit_StageKey`, `CombatUnit_CharacterKey`, `CombatUnit_Owner`, `CombatUnit_X`, `CombatUnit_Y`, `CombatUnit_Elevation`.

Stage 키는 기존 `StageMaster.Stage_Key`에 맞춰 사용자가 입력합니다. Seed는 입력한 값을 저장하며 자동 생성에 쓰지 않습니다. `StageConfig.TimelineSeed`와 같은 의미로 사용할지는 Unity 런타임 연결 때 정해야 합니다.

맵 폭 1~40, 높이 3~40, 셀 높이 0~20의 정수입니다. 추가 설정의 배치 깊이는 양수이며 아군+적군 깊이가 맵 높이보다 작아야 합니다. 대기 슬롯·밤 길이도 양수입니다. NightLength는 JavaScript의 정확한 정수 범위(2^53−1)까지만 지원합니다.

평면 좌표는 왼쪽 아래 `(0, 0)`, X는 오른쪽, Y는 위쪽입니다. 3D에서는 `(X, Elevation × 0.45, -Y)`로 보여주며 저장되는 Elevation은 원래 칸 단위입니다. 셀마다 높이를 기록합니다. Unit 높이는 해당 Cell 높이와 같아야 합니다. 한 유닛은 한 칸을 점유합니다. `Floor / Blocked / Rail`, `Player / Enemy`는 문자열로 저장합니다.

**Cell / Unit 키는 스테이지 안에서 고유합니다.** Cell 키는 `Y × MapWidth + X + 1`이며 폭을 바꾸면 다시 계산됩니다. Unit 키는 증가하는 배치 인스턴스 번호입니다. 여러 파일을 합칠 때 `(StageKey, Key)`로 식별해야 합니다. 기존 전역 int 키 인덱스에 그대로 합치면 스테이지 간 키가 충돌할 수 있습니다.

## Unity 연결 범위

툴은 Unity `Assets/ProjectL/Script/Tool/전투스테이지 편집`에 보관하고, 생성한 엑셀은 `Assets/DataTable`에 넣습니다. 기존 Unity C#·SO는 이번 편집기 수정에서 바꾸지 않았습니다. 새 세 시트에 대응하는 `IExcelData` 클래스·테이블 SO·조회/팩토리 등록·런타임 변환은 별도 작업입니다. 현재 Core의 `StageConfig` / `CellCoord`에는 단차 처리 계약이 없으므로 게임에서 높이를 쓰려면 해당 지원도 추가해야 합니다. 파일을 DataTable에 넣는 것만으로 전투가 생성되지는 않습니다.

## 파일 구성

```text
Assets/ProjectL/Script/Tool/전투스테이지 편집/
  index.html
  styles.css
  app.js             시작 흐름, 편집, 엑셀 입출력
  model.js           배치 검증, 좌표, Core 시트 계약
  reference-data.js  DT 표시 정보, 색상, 범위 형태, 편집 사본
  viewport.js        3D 맵과 캡슐, 카메라, 범위 표시
  README.md
  vendor/
    xlsx.full.min.js  SheetJS CE 0.20.3
    LICENSE.txt      Apache-2.0
    three.min.js     Three.js r160 classic browser build
    THREE-LICENSE.txt MIT
```

외부 라이브러리는 [SheetJS 공식 브라우저 배포판](https://docs.sheetjs.com/docs/getting-started/installation/standalone/)과 [Three.js r160](https://github.com/mrdoob/three.js/tree/r160)을 로컬로 포함했습니다.
