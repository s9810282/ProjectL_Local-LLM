# 개발 진입 지정 스테이지 배치 설계 (2026-10-26)

- **목표:** Play From Boot 개발 진입의 `DevBattleStage`에 새 값을 더한다. 이 값은 `CombatStageData` 에셋을 직접 받고, 그 스테이지를 맵의 처음 방문 노드(Dev Node Id)에 실제로 놓은 채 진입한다. 전투 정의만 바꾸는 지금 Selected와 달리, 노드·전투·클리어 기록이 모두 그 스테이지가 된다.
- **상태:** **결정 완료(2026-10-26 사용자 "모두 A로 하고 Task 계획 써줘"): Q-DS1 A·Q-DS2 A, P1~P5 그대로.** Task 계획: [dev-placed-stage-entry-plan](../plans/2026-10-26-dev-placed-stage-entry-plan.md)(Phase DP-a 하나, T1~T13). **DP-a 코드 완료(2026-10-27, 컴파일 미확인, 새 시험 없음, Unity 확인 대기).** 계획과 다른 점 여섯은 계획 §6.1에 적었다. 그중 설계에 닿는 것은 셋이다: 시험 가짜가 여섯이 아니라 여덟(§1 표와 §3), 진입 요청도 지정 스테이지의 맵을 씀(DS7), 설정 창 라벨 "Dev Node Id 노드에"(DS1).
- **사용자 요청(2026-10-26):** "Play From Boot에서 DevBattleStage enum에 새로운 타입을 추가할건데 해당 타입은 CombatStageData를 직접적으로 연결하고 플레이 시 해당 StageData를 처음 방문한 노드 취급하는건데 이해되니? 코어 측에는 내가 코어 건든다고 허락을 구해놓은 상태야" → "권장대로 설계 문서 써줘".
- **사용자 결정(2026-10-26, 권장안 수락):**
  1. 스테이지 키는 DB 카탈로그에서 그 `CombatStageData`를 참조하는 `StageRootDataSO`로 찾는다. 등록되지 않은 에셋이면 재생 시작에서 멈추고 문구를 남긴다.
  2. 챕터는 그 스테이지 루트의 맵 키로 자동으로 정한다. 이 모드에서는 Dev Chapter Key를 쓰지 않는다.
  3. 스테이지를 놓을 노드는 Dev Node Id 노드다(기본 2, 깊이 1의 첫 노드).
  4. 보스 스테이지는 거절한다. 일반 전투 스테이지만 받는다.
  5. Database 정의 전용이다. Fixture면 경고하고 Fixture 전투로 진입한다.
- **작업 영역:** 부트·개발 진입은 우리 영역이다. Core 두 파일(천명님)은 사용자가 허락을 받아 둔 변경이고, 구현 때 Core 전달 문서 §5에 적는다. DB 입력 SO 한 줄(`StageRootDataSO`, DB-SO 이전 때 우리가 쓴 파일)은 읽기 전용 접근자만 더한다.
- **범위 밖:** 운영 스테이지 맵 진입, 그래프 생성 알고리즘(`ChapterGraphGenerator`) 자체, 기존 Selected 모드 정리(Q-DS2).

## 1. 지금 구조 (코드 확인, 2026-10-26)

| 대상 | 지금 |
|---|---|
| 개발 전투 스테이지 | `GameBootstrap.DevBattleStage { Node = 0, Boss = 1, Selected = 2 }`. Boss와 Selected는 Database 정의 전용이다. **노드는 그대로 두고 전투 정의만** 바꾼다(`DevBossStageBattleSource`·`DevSelectedStageBattleSource`). Fixture에서 둘을 고르면 경고하고 Fixture로 진입한다(`CreateDevBattleSource`) |
| Selected의 한계 | 노드의 스테이지는 무작위 그대로다. 그래서 전투는 지정 스테이지인데, StageSelect의 노드 외형과 클리어 기록은 원래 뽑힌 스테이지가 된다. 노드가 비전투로 뽑히면 `ResolveDevEntryNode`가 다른 전투 노드로 옮긴다 |
| 개발 진행 준비 | `PrepareDevProgress`: `StartNewRun(devLoadoutKey)` → 시작 아이템 지급 → `EnterChapter(devChapterKey)` → `_devEntryNodeId = ResolveDevEntryNode(devChapterKey)`. 거절은 예외로 보고하고 진입하지 않는다 |
| 진입 노드 정하기 | `ResolveDevEntryNode`가 Buffer 맵 그래프를 읽는다. Dev Node Id 노드가 전투면 그 번호를 쓰고, 아니면 시작 노드에서 이어진 첫 전투 노드를 쓴다 |
| 그래프 만들기 | `ChapterProgressionCoordinator.EnterChapter(int)`는 그 챕터에 그래프가 없을 때만 `IStageGeneratorSystem.PrepareGraph(mapKey)`를 부른다. 그 뒤 `ChapterEntryPreparation`으로 게시하고 Buffer가 설치한다. **진입 때는 그래프를 다시 검사하지 않는다**(`ValidateTargetGraph`는 다음 챕터 이어가기 경로에서만 쓴다) |
| 생성 시스템 | `StageGeneratorSystem.PrepareGraph`가 맵의 스테이지 루트를 Regular 목록과 보스 하나로 나눈다. 그다음 `ChapterGraphSpec`을 만들어 순수 생성기를 부르고, 노드마다 루트에서 스테이지 종류와 맵 역할을 채운다 |
| 생성기 | `ChapterGraphGenerator.Generate`(순수 계산). 시작 노드는 1번이다. 깊이 1부터 번호가 2, 3, …으로 이어지고, 중간 노드는 Regular 키 중 하나를 무작위로 받는다(중복 허용). 마지막 깊이에는 보스 노드가 하나 있다. 난수 호출 순서가 모양을 정한다 |
| 전투 진입 검사 | `BattleEntryService.ValidateCombatTarget`은 노드의 스테이지가 그 챕터의 전투 스테이지인지 본다. 아니면 거절 대신 예외를 던진다 |
| 진입 노드 제한 | 선택이 없으면 Core는 시작 노드에서 이어진 노드(깊이 1)로만 진입을 허락한다(`ResolveDevEntryNode` 주석) |
| 스테이지 루트 SO | `StageRootDataSO`는 private 필드 `stageKey`·`mapKey`·`mapRole`·`stageData`(`StageBaseData` SO 참조)를 갖는다. `GetData()`는 호출할 때마다 순수 Data를 새로 만든다 → 어떤 `CombatStageData`인지는 **SO 참조로만** 맞출 수 있다 |
| `CombatStageData` | `StageBaseData`를 상속한 입력 SO다. 자기 키가 없다. 키·맵·역할은 그것을 참조하는 루트가 가진다 |
| 지금 DB | 루트 15개(맵 1). 전투 일반 1101~1111(`Combat_Normal_A`~`K`), 상점 1201, 강화 1202, 이벤트 1301, 보스 2101(`Combat_Boss`). **`CombatStageData` 12개는 각각 루트 하나만 참조한다** |
| 카탈로그 | `GameBootstrap.databaseCatalog`(`DatabaseCatalogSO`)는 그룹·표·항목을 공개 `GetAllData()`로 순회할 수 있다. 스테이지 루트 표는 `InGameGroup/Stage/StageRootDataTable.asset`에 있다 |
| 개발 값 출처 | 씬 `GameBootstrap` 직렬화 값에, 이 PC의 `GameBootstrapEditorOverrides`(EditorPrefs, `#if UNITY_EDITOR`) 값이 덮어쓴다. 설정 창은 `Assets/ProjectL/Editor/Boot/PlayFromBootSettingsWindow.cs`다. 덮어쓰기는 `ApplyEditorOverrides`가 재생 시작 때 적용하고, 정의 안 된 열거 값이면 예외를 낸다 |
| Boot 시험 | Combat으로 시작하는 Boot 시험(`LobbyBootSmokeTests`·`ConnectedWallBootSmokeTests`)은 `DevBattleStage = 0`으로 정한 뒤 되돌린다. 새 값은 시험에 새어 들어가지 않는다 |
| 조정자 계약을 구현하는 시험 가짜 | `IChapterProgressionCoordinator` 구현 가짜는 여섯 곳이다. 다섯은 천명님 시험(`CombatTransitionPublisherTests:123`·`BattleCompletionServiceTests:121`·`BossObjectivePublicationTests:174`·`CombatClearRewardIntegrationTests:309`·`CombatCommandPublicationTests:480`)이고, 하나는 우리 시험(`CombatProgressJobTests:563`)이다. 모두 `EnterChapter(int)`가 `NotSupportedException`이다. `IStageGeneratorSystem` 구현 가짜는 없다 |

## 2. 결정 (설계)

### DS1 새 값과 입력

- 열거에 `PlacedStage = 3`을 더한다. 설정 창 문구는 "지정 스테이지 데이터를 첫 노드에 (Database 전용)"이다.
- 씬 필드 `[SerializeField] private CombatStageData devStageData;`를 더한다. 툴팁에는 "Dev Battle Stage가 PlacedStage일 때, 이 전투 스테이지를 Dev Node Id 노드에 놓고 진입한다. 챕터는 그 스테이지의 맵이다"라고 쓴다.
- EditorPrefs에는 에셋 GUID 문자열(`DevStageDataGuid`)을 저장한다. 덮어쓰기는 GUID로 에셋을 불러와 `devStageData`에 넣는다. PlacedStage인데 GUID가 가리키는 에셋이 없으면 예외로 시작을 멈추고 "설정 창에서 다시 고르세요"를 남긴다.
- 기존 Selected·Dev Stage Key는 그대로다(Q-DS2).

### DS2 스테이지 키 찾기 (결정 1)

- 새 개발 도우미 `ViewSystem/Dev/DevStageRootLookup`(정적)이 맡는다. 카탈로그의 그룹·표·항목을 돌면서 `StageRootDataSO`만 고르고, 그 입력 참조가 받은 `CombatStageData`와 같은 객체인 것을 찾는다.
- 맞는 루트가 0개면 "DB 카탈로그의 스테이지 표에 등록되지 않은 CombatStageData입니다: <에셋 이름>" 예외를 던진다. 2개 이상이면 "여러 스테이지 루트가 같은 CombatStageData를 참조합니다: <키 목록>" 예외를 던진다. 지금 DB는 1:1이다.
- 찾은 루트에서 공개 `GetData()`로 `StageRootData`(키·맵·역할)를 읽는다. 새 접근자는 입력 참조 읽기 하나뿐이다: `StageRootDataSO`에 `internal StageBaseData StageDataInput => stageData;`(같은 어셈블리, 동작 변화 없음).
- 이 이름 맞추기는 런타임 DB(`StageTableQuery`)를 만든 바로 그 카탈로그를 읽는다. 그래서 키의 원본은 그대로 DB 루트 하나다.

### DS3 챕터 (결정 2)

- 챕터 키는 찾은 루트의 맵 키다. `EnterChapter`와 `ResolveDevEntryNode`가 이 값을 쓴다.
- `devChapterKey`와 다르면 콘솔에 한 줄 남긴다: "Dev Chapter Key n 대신 지정 스테이지의 맵 m으로 진입합니다".
- 설정 창에서는 이 모드일 때 Dev Chapter Key 칸을 비활성으로 둔다.

### DS4 Core 고정 배치 (결정 3·4, Q-DS1 A 기준)

- 새 순수 계약 `Contracts/StageMap/ChapterStagePin`(sealed class)을 만든다. 담는 값은 `StageNodeId NodeId`와 `StageKey StageKey`다.
- 조정자 계약에 오버로드 `EnterChapter(int chapterKey, ChapterStagePin pin)`을 더한다. 기존 `EnterChapter(int)`는 `pin: null`로 이 오버로드를 부르므로 운영 동작이 같다.
- 생성 시스템 `PrepareGraph(int mapKey, ChapterStagePin pin = null)`은 생성기 결과를 받은 뒤 고정된 노드 하나만 그 스테이지로 바꾼다. 스테이지 종류와 맵 역할은 그 스테이지의 루트에서 다시 채운다.
- 생성기(`ChapterGraphGenerator`)와 `ChapterGraphSpec`은 고치지 않는다. 난수 호출 순서가 같으므로, 같은 시드라면 층·연결·다른 노드는 고정이 없을 때와 같다.
  > **2026-10-27 바뀜** ([챕터 스테이지 분포 설계](2026-10-27-chapter-stage-distribution-design.md) §12 A-PIN): 위 두 줄은 더 이상 맞지 않는다. 고정은 이제 Core 생성기 입력(`ChapterGraphSpec.Pin`)이고, 종류별 최소 개수(상점·회복 등)를 고정 노드까지 포함해 지킨다. 층·연결은 고정이 없을 때와 같지만 **다른 노드의 스테이지는 달라질 수 있다**. 고정 오류는 `ArgumentException` 대신 맵 키를 담은 `InvalidOperationException`(안쪽에 원래 예외)으로 나온다.
- Core 검사는 모두 예외로 하고, 문구에 노드·스테이지 키를 넣는다.
  - 고정은 새로 만드는 그래프에만 쓴다. 이미 그래프가 있거나 다음 챕터 이어가기 갈래면 예외다.
  - 노드가 생성된 그래프에 있어야 하고, 시작(깊이 0)·보스(마지막 깊이)가 아닌 중간 층이어야 한다.
  - 스테이지는 이 맵의 Regular 루트여야 한다(생성 시스템이 이미 만든 Regular 목록에 있어야 함). 이 규칙 하나가 **결정 4의 보스 거절**을 맡는다. GameBootstrap은 같은 검사를 따로 두지 않는다(SSOT).
- 전투 스테이지인지는 필드 형식(`CombatStageData`)이 보장한다. 루트의 스테이지 종류는 그 상세에서 나오므로 Combat이다.

### DS5 진입 노드와 전투 정의 (결정 3)

- 고정할 노드는 `devNodeId`다. 고정된 노드는 전투이므로 `ResolveDevEntryNode`는 바꾸지 않아도 `devNodeId`를 그대로 돌려준다.
- `CreateDevBattleSource`는 PlacedStage + Database에서 `null`(운영과 같은 기본 DB 소스)을 돌려준다. 노드의 스테이지가 곧 지정 스테이지이므로 개발 소스가 필요 없다.
- 승리하면 StageSelect의 그 노드가 지정 스테이지의 외형과 클리어로 보인다. 운영 경로와 같다.

### DS6 Fixture (결정 5)

- Fixture 정의에서 PlacedStage를 고르면 경고한다: "지정 스테이지 데이터는 Database 정의에서만 씁니다. Fixture 개발 전투로 진입합니다."
- 그 뒤 고정·챕터 바꾸기를 하지 않고 지금 Fixture 진입(`devChapterKey`·`devNodeId`)과 같게 간다. Boss·Selected와 같은 꼴이다.

### DS7 흐름 (PlacedStage + Database)

```text
ApplyEditorOverrides (에디터: GUID → devStageData)
PrepareDevProgress
  ├─ devStageData 없음 → 예외
  ├─ DevStageRootLookup.Find(databaseCatalog, devStageData) → 루트(키 k, 맵 m) · 미등록/모호 → 예외
  ├─ StartNewRun(devLoadoutKey) → 시작 아이템 지급
  ├─ EnterChapter(m, new ChapterStagePin(devNodeId, k))   ← Core: 고정 배치·검사(보스·노드·이미 그래프)
  └─ _devEntryNodeId = ResolveDevEntryNode(m)              ← 그대로 devNodeId
진입 Job → Core 진입 검사(깊이 1 연결 등, 지금 그대로) → 기본 DB 소스로 스테이지 k 전투
```

- 키 찾기는 `StartNewRun`보다 먼저 한다. 잘못된 입력이면 Run을 만들기 전에 멈춘다.
- 재생 시작 로그(`ApplyEditorOverrides`의 한 줄)에는 전투 스테이지 뒤에 에셋 이름을 붙인다.

### DS8 멈추는 경우 (모두 재생 시작에서 예외 → 진입하지 않음)

| 경우 | 누가 | 문구의 요점 |
|---|---|---|
| PlacedStage인데 에셋이 비었음 | GameBootstrap | Dev Stage Data를 고르라 |
| 저장된 GUID의 에셋이 없음 | 덮어쓰기 | 설정 창에서 다시 고르라 |
| 카탈로그에 등록 안 됨 | 찾기 도우미 | 에셋 이름 |
| 여러 루트가 참조 | 찾기 도우미 | 루트 키 목록 |
| 보스·이 맵의 Regular가 아님 | Core 생성 시스템 | 스테이지 키·역할 |
| Dev Node Id가 없는 노드·시작·보스 노드 | Core 생성 시스템 | 노드 번호·깊이 |
| 고정할 수 없는 갈래(그래프가 이미 있음 등) | Core 조정자 | 챕터 키 |
| Dev Node Id가 깊이 2 이상(시작에서 바로 이어지지 않음) | Core 진입(기존) | 지금과 같은 진입 거절 |

## 3. 바뀌는 파일 (예상)

| 파일 | 영역 | 바뀜 |
|---|---|---|
| `ViewSystem/Boot/GameBootstrap.cs` | 우리(부트·개발 진입) | 열거 값, `devStageData` 필드, `PrepareDevProgress` 갈래, `CreateDevBattleSource` 경고·null, 덮어쓰기 적용·로그 |
| `ViewSystem/Boot/GameBootstrapEditorOverrides.cs` | 우리 | `DevStageDataGuid`(문자열), `DevBattleStage` 문서에 3 |
| `Editor/Boot/PlayFromBootSettingsWindow.cs` | 우리 | 라벨 넷째, `ObjectField`(CombatStageData, 모드 3일 때만), 모드 3이면 Dev Chapter Key 비활성 |
| 새 `ViewSystem/Dev/DevStageRootLookup.cs`(+ `.meta`) | 우리 | 카탈로그에서 루트 찾기 |
| `DataBaseSystem/.../Stage/Item/StageRootDataSO.cs` | DB 입력(우리 DB-SO 작업 파일) | `internal` 읽기 한 줄 |
| 새 `CoreCaculator/Contracts/StageMap/ChapterStagePin.cs`(+ `.meta`) | Core(사용자 허락) | 순수 값 |
| `CoreSystem/ChapterProgressionCoordinator.cs` | Core(천명님, 사용자 허락) | 계약 오버로드, 구현(고정 갈래 검사·생성 인자) |
| `CoreSystem/StageSystem/StageGeneratorSystem.cs` | Core(천명님, 사용자 허락) | `PrepareGraph` 끝 선택 인자, 고정 배치·검사 |
| 시험 가짜 여섯(§1 표) | 천명님 다섯·우리 하나 | 새 오버로드 한 줄(`NotSupportedException`) — 컴파일이 깨지는 곳만 최소로 맞춤(CLAUDE.md 규칙) |

Factory·ServiceKeys·Framework·씬·DB 에셋은 바뀌지 않는다. `PrepareGraph(1)`처럼 부르는 기존 시험 넷은 선택 인자라 그대로 컴파일된다.

## 4. 정할 것

### Q-DS1 Core 고정 배치 입구

| 안 | 내용 | 좋은 점 | 나쁜 점 |
|---|---|---|---|
| **A (권장)** | 조정자 계약에 `EnterChapter(chapterKey, pin)` 오버로드를 더하고, 생성 시스템이 인자로 받는다(DS4) | 입력이 호출 인자로만 흐른다. 숨은 상태나 순서 결합이 없다. 운영 `EnterChapter(int)`는 같은 길에 null을 넘긴다 | 시험 가짜 여섯에 한 줄씩 더해야 한다(다섯은 천명님 시험). 운영 계약에 개발에서만 쓰는 오버로드가 생긴다 |
| B | 생성 시스템에 "다음 그래프 고정 예약" 메서드를 두고, GameBootstrap이 `EnterChapter` 전에 예약한다 | 조정자 계약과 시험 가짜를 고치지 않는다 | 생성 시스템이 상태를 갖는다. 예약과 소비의 순서에 기대고, 소비되지 않은 예약이 남을 수 있다 |
| C | 조정자 계약에 기본 구현이 있는 인터페이스 멤버를 더한다 | 가짜를 고치지 않는다 | 이 프로젝트에 선례가 없다. 가짜가 조용히 기본 구현을 물려받아 계약이 흐려진다 |

### Q-DS2 기존 Selected 모드

- **A (권장):** 이번에는 그대로 둔다. 새 모드가 Selected의 쓰임새(전투만 지정)를 거의 대신하므로, 걷는 것은 후속 후보로 적는다.
- B: 이번에 걷는다. 이 경우 EditorPrefs에 저장된 값 2가 정의되지 않은 값이 되어 그 PC는 재생 시작에서 멈춘다. 그 처리와 `DevSelectedStageBattleSource` 삭제가 범위에 들어간다.

### 계획에서 정할 것(설계 확인 대상)

- P1 이름: 열거 `PlacedStage`, 계약 `ChapterStagePin`, 도우미 `DevStageRootLookup`, EditorPrefs 키 `DevStageDataGuid`.
- P2 지정 스테이지가 다른 노드에도 무작위로 또 나올 수 있다(생성기가 중복을 허용함). 그대로 둔다.
- P3 맵이 Run마다 시드를 바꾸면 그래프 모양은 재생마다 다르다. 고정된 노드의 스테이지만 늘 같다.
- P4 Core 검사의 예외 형식: 고정 인자가 잘못되면 `ArgumentException`, 고정할 수 없는 갈래면 `InvalidOperationException`(코딩 규칙).
- P5 Phase는 하나로 본다. Core 고정·가짜 여섯·Core 전달 문서 기록을 먼저 하고, 그다음 개발 진입·설정 창을 한다. Core가 먼저 있어야 GameBootstrap이 컴파일된다.

## 5. 원칙 점검 (7기준, 예상)

| 기준 | 점수 | 근거 |
|---|---|---|
| SSOT | 9 | 스테이지 키의 원본은 DB 루트 하나다(SO 참조로 찾기만 함). 보스 거절은 Core 규칙 한 곳이다. 노드 번호는 Dev Node Id 하나다. 진입 노드 결정은 기존 함수를 그대로 쓴다 |
| SOLID | 8.5 | 순수 생성기는 그대로 두고, DB를 아는 생성 시스템이 고정을 맡는다. 조정자는 전달만 한다. 감점: 운영 계약에 개발용 오버로드가 생김(ISP, 낮음) |
| FW | 9.5 | 서비스·Factory·키는 그대로다. Core 공개 서비스만 부르고 Buffer에 쓰지 않는다 |
| 독립성 | 9 | 운영 `EnterChapter(int)`·`PrepareGraph(int)`의 동작이 같다. 다른 개발 모드도 같다. 감점: 다른 사람 시험 가짜 다섯에 한 줄씩(낮음) |
| 캡슐화 | 8.5 | 입력 SO의 internal 읽기 하나가 늘고(같은 어셈블리), 카탈로그 순회는 개발 도우미 한 곳에만 둔다 |
| C#/Unity 권장 구조 | 9 | 에셋은 GUID로 저장하고, 개발 코드는 `UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 안에 둔다. 실패는 재생 시작에서 바로 드러난다 |
| 무의미한 If/Tick | 9.5 | Tick이 없다. 모드 갈래 두 곳(`PrepareDevProgress`·`CreateDevBattleSource`)뿐이다 |

**예상 약 9.0/10**(알려진 문제 기준). 남는 낮음 셋: 개발용 오버로드, 시험 가짜 한 줄씩, SO 단계 이름 맞추기.

## 6. 시험·확인

- 새 `[Test]`는 만들지 않는다(CLAUDE.md, 2026-10-20 지시). MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
- 기존 시험은 컴파일이 깨지는 가짜 여섯만 맞춘다. 실행 결과가 바뀔 시험은 없다고 본다. 고정이 없는 길은 동작이 같고, Boot 시험은 `DevBattleStage = 0`을 쓴다.
- Unity 확인(사용자):
  1. 설정 창에서 PlacedStage + Database, `Combat_Normal_E` 지정 → 재생 → 콘솔에 맵 1 진입. 전투 필드가 E다.
  2. 그 전투를 이긴 뒤 StageSelect의 노드 2가 E의 외형이고 클리어로 표시된다.
  3. `Combat_Boss` 지정 → 재생 시작에서 멈추고, Core 예외 문구에 스테이지 2101·Boss가 나온다.
  4. 표에 등록하지 않은 새 `CombatStageData` → 멈추고 "등록되지 않은" 문구가 나온다.
  5. Fixture 정의 + PlacedStage → 경고 한 줄 뒤 Fixture 전투.
  6. Node·Boss·Selected 모드와 Lobby 시작이 지금과 같다.
  7. Dev Node Id 1(시작 노드) → Core 고정 검사 예외.

## 7. 공유

- 천명님: Core 전달 문서 §5에 새 절(구현 때 §5.53)을 쓴다. 내용은 계약 오버로드, 생성 시스템 선택 인자, 새 계약 파일, 가짜 다섯 한 줄씩이며 미전달로 둔다. Core 요청은 없다(직접 구현, 사용자 허락).
- 민형님·현강님·호진님: 해당 없음. StageSelect 표시 코드는 고치지 않는다. 노드가 운영과 같은 데이터를 가질 뿐이다.
