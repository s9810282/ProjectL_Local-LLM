# 개발 진입 지정 스테이지 배치 계획

- 설계: [dev-placed-stage-entry-design](../specs/2026-10-26-dev-placed-stage-entry-design.md)(DS1~DS8).
- **결정(2026-10-26 사용자 "모두 A로 하고 Task 계획 써줘"):**
  - Q-DS1 A: 조정자 계약 오버로드 + 생성 시스템 인자.
  - Q-DS2 A: Selected 모드는 그대로 둔다.
  - 설계 P1~P5도 그대로 따른다.
- 작성: 2026-10-26. **DP-a 코드 완료(2026-10-27, 컴파일 미확인, 새 시험 없음, Unity 확인 대기) — 기록 §6.1.**
- 경로 약어:
  - `B/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/`
  - `D/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Dev/`
  - `E/` = `Assets/ProjectL/Editor/Boot/`
  - `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/`
  - `CC/` = `C/CoreCaculator/`
  - `S/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/`
  - `T/` = `Assets/Tests/EditMode/`
- 위치는 메서드 이름으로 적는다. 줄 번호는 2026-10-26 HEAD `28f8bbbb` 기준 참고값이다.

## 0. 착수 조건

1. **기준 판:** HEAD `28f8bbbb`. 작업 트리에는 FMOD 캐시 변경 하나만 있다(이번 것이 아님, 건드리지 않음).
2. **확인 방법:** 전투 View 규칙(CLAUDE.md, 2026-10-20 지시)을 이번 개발 진입 작업에도 똑같이 적용한다.
   - MSBuild·관리 실행은 하지 않는다. 새 `[Test]`도 만들지 않는다.
   - 보고에는 "컴파일 미확인"을 적는다. 컴파일·재생은 사용자가 Unity에서 확인한다.
3. **Core 변경 허락:** 사용자가 Core 변경 허락을 받아 두었다(2026-10-26). 바꾼 Core 줄은 Core 전달 문서 §5.53에 적는다.

## 1. 공통 규칙

- **새 파일 형식:**
  - 네임스페이스는 폴더 경로를 따르고, Allman·공백 4칸·LF·UTF-8·`sealed` 기본, 한국어 XML doc이다.
  - `.meta`는 32자리 hex GUID로 새로 만든다.
- **바꾸지 않는 곳:**
  - 생성기 `ChapterGraphGenerator`와 `ChapterGraphSpec`.
  - Selected 모드와 `DevSelectedStageBattleSource`·`DevBossStageBattleSource`.
  - `ResolveDevEntryNode`.
  - Factory·ServiceKeys·Framework·Buffer·씬·DB 에셋.
- **운영 길은 동작이 같아야 한다:**
  - `EnterChapter(int)`는 null 고정으로 새 오버로드를 부른다.
  - `PrepareGraph(int)`는 선택 인자가 비면 지금 코드와 같은 결과를 낸다.
- **실패는 모두 재생 시작의 예외다.** 문구 머리는 `[GameBootstrap]`·`[DevStageRootLookup]`·Core는 머리 없음(그 파일의 기존 꼴)이다.
- Phase는 하나(DP-a)다. Task 순서대로 하고 끝나면 멈추고 보고한다.

## 2. DP-a — Core 고정 배치 (T1~T5)

### T1 고정 값 `CC/Contracts/StageMap/ChapterStagePin.cs` (새, + `.meta`)

- 네임스페이스 `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap`.
- `public sealed class ChapterStagePin`
  - `public StageNodeId NodeId { get; }`, `public StageKey StageKey { get; }`.
  - 생성자 `(StageNodeId nodeId, StageKey stageKey)`: 둘 중 하나라도 기본값(0)이면 `ArgumentException`을 던진다. 두 구조체의 생성자가 1 이상을 보장하지만, `default`로 만든 값은 걸러야 한다.
- XML doc에 쓸 내용:
  - "새로 만드는 챕터 그래프에서 중간 노드 하나의 스테이지를 정해 둡니다."
  - "그래프 모양과 다른 노드는 고정이 없을 때와 같습니다."
  - "지금은 개발 진입만 씁니다."
- `using UnityEngine` 없음(Core는 순수 C#).

### T2 생성 시스템 `C/StageSystem/StageGeneratorSystem.cs`

- 계약 `IStageGeneratorSystem.PrepareGraph(int mapKey, ChapterStagePin pin = null)`, 구현도 같은 서명이다.
  - 계약 XML doc에 "pin이 있으면 그 노드의 스테이지만 바꿉니다"를 적는다.
  - 다른 호출처(조정자 이어가기 `:185`, 시험 넷)는 그대로 컴파일된다.
- 구현(`PrepareGraph` 안, 노드를 `result`에 다 담은 뒤, 마지막 `EnsureRunInProgress()` 앞):
  - `if (pin != null) PlacePinnedStage(result, pin, map, regular, stagesByKey);`
- 새 private static `PlacePinnedStage(List<StageProgressData> nodes, ChapterStagePin pin, StageMapData map, List<int> regular, Dictionary<int, StageRootData> stagesByKey)`
  1. `nodes`에서 `NodeId == pin.NodeId.Value`인 위치를 찾는다. 없으면 `ArgumentException`: `$"Pinned node {id} is not in the generated graph of map {map.MapKey}."` — 이 파일의 기존 예외 문구가 영어라 꼴을 맞춘다.
  2. 그 노드의 깊이가 `1..map.DepthCount - 2` 밖이면 `ArgumentException`(시작·보스 노드에는 놓을 수 없음, 깊이 표기).
  3. `regular.Contains(key)`가 아니면 `ArgumentException`. 문구에 스테이지 키와 역할을 넣는다. 역할은 `stagesByKey`에 있으면 그 `MapRole`, 없으면 "not in map". 중간 노드 후보와 같은 목록으로 판정하므로 보스 거절 규칙의 원본은 하나다(설계 DS4).
  4. 그 자리를 새 `StageProgressData(node.NodeId, key, node.Depth, node.WidthIndex, node.NextNodeIds, stageType: root.StageType, mapRole: root.MapRole)`로 바꾼다. `root = stagesByKey[key]`다.
- 주석 한 줄: "생성기의 난수 순서는 그대로 두고 결과의 한 노드만 바꾼다. 그래서 같은 시드면 모양이 같다."

### T3 조정자 `C/ChapterProgressionCoordinator.cs`

- 계약 `IChapterProgressionCoordinator`의 `EnterChapter(int chapterKey);` 아래에 오버로드를 더한다.
  - 선언: `ChapterProgressionResult EnterChapter(int chapterKey, ChapterStagePin pin);`
  - XML doc: "pin이 있으면 새로 만드는 그래프의 노드 하나에 스테이지를 놓습니다. 그래프가 이미 있거나 이어가기 갈래면 예외입니다."
- 구현:
  - 기존 `EnterChapter(int chapterKey)`는 `=> EnterChapter(chapterKey, null);` 한 줄이 된다.
  - 새 `EnterChapter(int chapterKey, ChapterStagePin pin)` 본문은 기존 본문을 그대로 옮기고 아래만 더한다.
    - 이어가기 갈래(`_currentRead.HasMap && _currentRead.CurrentMapKey != chapterKey`)에 들어가기 전, `pin != null`이면 `InvalidOperationException`을 던진다("다음 챕터 이어가기에는 스테이지 고정을 쓸 수 없습니다").
    - `StageNotFound` 갈래의 `_generator.PrepareGraph(chapterKey)` → `_generator.PrepareGraph(chapterKey, pin)`.
    - 그 갈래 뒤, `pin != null && generated.Count == 0`이면 `InvalidOperationException`을 던진다("스테이지 고정은 새로 만드는 그래프에만 쓸 수 있습니다. 챕터 {key}").
  - `RunEnded` 조기 반환은 지금 순서대로 먼저 둔다. Run이 끝났으면 고정과 상관없이 거절 결과다.
- 이 파일의 다른 곳(이어가기 `:185`의 `PrepareGraph(targetChapterKey)`, 검사 함수들)은 바꾸지 않는다.

### T4 시험 가짜 여섯 — 컴파일만 맞춤 (CLAUDE.md "컴파일이 깨지는 곳만 최소로")

- 파일마다 using 한 줄 `using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;`과 멤버 한 줄을 더한다.
  - 멤버: `public ChapterProgressionResult EnterChapter(int key, ChapterStagePin pin) => throw new NotSupportedException();`
  - 기존 `EnterChapter(int …)` 줄 바로 아래에 두고, 인자 이름은 그 파일의 기존 줄을 따른다.

| 파일 | 위치 | 작성자 |
|---|---|---|
| `T/CoreIntegration/CombatTransitionPublisherTests.cs` | `:140` 아래 | 천명님 |
| `T/Framework/BattleCompletionServiceTests.cs` | `:136` 아래 | 천명님 |
| `T/Framework/BossObjectivePublicationTests.cs` | `:185` 아래 | 천명님 |
| `T/Framework/CombatClearRewardIntegrationTests.cs` | `:318` 아래 | 천명님 |
| `T/Framework/CombatCommandPublicationTests.cs` | `:491` 아래 | 천명님 |
| `T/Framework/CombatProgressJobTests.cs` | `:578` 아래 | 우리 |

- 시험 본문·단언은 고치지 않는다. `IStageGeneratorSystem` 가짜는 없으니(속성이 null을 돌려줄 뿐) 손대지 않는다.

### T5 Core 전달 문서 §5.53 `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md`

- 제목: "개발 진입 스테이지 고정 — `EnterChapter` 오버로드·`PrepareGraph` 선택 인자(2026-10-26, 사용자 허락, 미전달)".
- 내용:
  - 왜 하는지: Play From Boot의 새 개발 모드.
  - 바꾼 줄: 새 계약 파일, 두 계약, 두 구현, 검사 셋.
  - 운영 동작이 같다는 근거.
  - 가짜 다섯에 한 줄씩(파일·줄).
  - 검증 상태: "컴파일 미확인, 새 시험 없음".
- 요청은 없다. 천명님이 이 오버로드를 운영에 쓸 생각이 없으면, 나중에 개발 전용 입구로 옮길 수 있다는 점도 한 줄 적는다.

## 3. DP-a — 개발 진입 (T6~T11)

### T6 입력 SO 읽기 `S/Item/StageRootDataSO.cs`

- 필드 아래에 `internal StageBaseData StageDataInput => stageData;` 한 줄을 더한다. XML doc: "어떤 상세 SO를 참조하는지 SO 단계에서 맞출 때만 씁니다(GetData는 매번 새 Data를 만들어 비교할 수 없음)."
- 직렬화·`GetData`는 바꾸지 않는다.

### T7 루트 찾기 `D/DevStageRootLookup.cs` (새, + `.meta`, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`)

- `internal static class DevStageRootLookup`
- `internal static StageRootData Find(DatabaseCatalogSO catalog, CombatStageData stage)`
  - 인자가 null이면 `ArgumentNullException`이다. GameBootstrap이 비어 있는 에셋을 먼저 더 친절한 문구로 걸러 준다(T8).
  - 순회: `catalog.GetAllData().OfType<DatabaseGroup>()` → `GetAllData().OfType<StageRootDataTableSO>()` → `GetAllData().OfType<StageRootDataSO>()`. 그중 `ReferenceEquals(root.StageDataInput, stage)`인 것을 모은다.
    - 이 꼴은 DB Factory의 `OfType<…TableSO>()` 관례를 따른다. `OfType`이 빈 칸(null)을 거른다.
  - 0개면 `InvalidOperationException`: `$"[DevStageRootLookup] DB 카탈로그의 스테이지 루트 표에 등록되지 않은 CombatStageData입니다: {stage.name}"`.
  - 2개 이상이면 `InvalidOperationException`. 문구에 루트 에셋 이름과 키 목록을 넣는다.
  - 하나면 `(StageRootData)root.GetData()`를 돌려준다. 키·맵·역할의 원본은 DB 루트다.
- 역할(보스) 검사는 여기서 하지 않는다. Core 규칙 한 곳이 한다(T2, 설계 DS4).

### T8 GameBootstrap `B/GameBootstrap.cs`

- **열거 `DevBattleStage`:**
  - `PlacedStage = 3`을 더한다.
  - XML doc에 쓸 내용: PlacedStage는 Dev Stage Data의 전투 스테이지를 Dev Node Id 노드에 실제로 놓고 진입하며, 챕터는 그 스테이지의 맵이다. Database 정의 전용이다.
- **필드:**
  - `devStageKey` 아래에 `[Tooltip(…)] [SerializeField] private CombatStageData devStageData;`를 더한다. 툴팁은 설계 DS1 문구다.
  - `devChapterKey` 툴팁에 "PlacedStage에서는 쓰지 않음(지정 스테이지의 맵)"을 붙인다.
  - `devBattleStage` 툴팁에 PlacedStage를 더한다.
- **`CreateDevBattleSource`:**
  - Fixture 갈래의 경고 사슬에 `else if (devBattleStage == DevBattleStage.PlacedStage)`를 더한다. 문구: "지정 스테이지 데이터는 Database 정의에서만 씁니다. Fixture 개발 전투로 진입합니다."
  - Database + PlacedStage는 지금처럼 끝의 `return null`로 간다. 주석 한 줄: "노드에 스테이지를 놓으므로 기본 DB 소스"
  - XML doc 요약에 한 줄을 더한다.
- **`PrepareDevProgress`** (`#if` 안):
  1. 맨 앞(Run 만들기 전)에서 `int chapterKey = devChapterKey; ChapterStagePin pin = null;`
  2. `devBattleDefinition == Database && devBattleStage == PlacedStage`일 때:
     - `devStageData == null`이면 예외: "[GameBootstrap] Dev Battle Stage가 PlacedStage인데 Dev Stage Data가 비었습니다. 인스펙터나 Play From Boot 설정에서 고르세요."
     - `StageRootData root = DevStageRootLookup.Find(databaseCatalog, devStageData);`
     - `chapterKey = root.MapKey; pin = new ChapterStagePin(new StageNodeId(devNodeId), new StageKey(root.StageKey));`
     - 콘솔 한 줄: `[GameBootstrap] 지정 스테이지 {이름}(키 {k}, 맵 {m})를 노드 {devNodeId}에 놓습니다.` 맵이 `devChapterKey`와 다르면 "Dev Chapter Key {n} 대신 맵 {m}" 구절을 붙인다.
  3. 기존 `StartNewRun`·시작 아이템 지급은 그대로다.
  4. `EnterChapter(devChapterKey)` → `EnterChapter(chapterKey, pin)`. 거절 예외 문구의 챕터도 `chapterKey`로 바꾼다.
  5. `_devEntryNodeId = ResolveDevEntryNode(chapterKey);`
- **using 둘:** `…DataBaseSystem.Data.InGame.Table.Stage.Combat`(CombatStageData)와 `…CoreCaculator.Contracts.StageMap`(ChapterStagePin). `StageRootData`의 using(`…Stage.Item`)도 필요하면 더한다.

### T9 덮어쓰기 저장 `B/GameBootstrapEditorOverrides.cs`

- `DevBattleStage` XML doc에 "3 = Dev Stage Data를 Dev Node Id 노드에 놓음(Database 정의 전용)"을 더한다.
- `DevStageKey` 아래에 넣는 것:
  - **setter `public static CombatStageData DevStageData`:** null이면 `EditorPrefs.DeleteKey(Prefix + "DevStageDataGuid")`다. 아니면 `AssetDatabase.GetAssetPath` → `AssetPathToGUID`를 저장한다. getter는 `TryReadDevStageData`가 성공하면 그 값이고, 실패하면 null이다(설정 창 표시용).
  - **`public static bool TryReadDevStageData(out CombatStageData stage)`:**
    - GUID가 없으면 stage null, true.
    - 있으면 `GUIDToAssetPath` → `LoadAssetAtPath<CombatStageData>`. 찾으면 true다. 경로가 비었거나 형식이 다르면 stage null, false다.
- 정수·문자열 저장 꼴은 기존 `DevStartingItems`(문자열 + `TryRead…`)을 따른다.

### T10 덮어쓰기 적용 `GameBootstrap.ApplyEditorOverrides`

- `devStageKey = …` 다음:
  - `bool stageDataRead = GameBootstrapEditorOverrides.TryReadDevStageData(out CombatStageData stageData);`
  - `if (!stageDataRead && battleStage == DevBattleStage.PlacedStage)`면 예외: "[GameBootstrap] Play From Boot 설정의 Dev Stage Data 에셋을 찾지 못했습니다. 설정 창에서 다시 고르세요."
  - `devStageData = stageData;` — 다른 모드면 읽지 못해도 null이 들어가고 쓰지 않는다.
- 재생 시작 로그의 "전투 스테이지" 뒤 구절: Selected면 키, PlacedStage면 `devStageData` 이름(없으면 "없음").

### T11 설정 창 `E/PlayFromBootSettingsWindow.cs`

- `StageLabels` 넷째: `"지정 스테이지 데이터를 첫 노드에 (Dev Stage Data, Database 전용)"`.
- `DrawOverrides`:
  - 읽기: `CombatStageData stageData = GameBootstrapEditorOverrides.DevStageData;`, `bool stageDataRead = GameBootstrapEditorOverrides.TryReadDevStageData(out _);`
  - Dev Chapter Key 줄을 `DisabledScope(battleStage == 3)`로 감싼다.
  - Dev Stage Key 다음 줄은 `DisabledScope(battleStage != 3)` 안의 `ObjectField("Dev Stage Data", stageData, typeof(CombatStageData), false)`다.
  - 바뀜 쓰기에 `GameBootstrapEditorOverrides.DevStageData = stageData;`를 더한다.
- 도움말(`battleStage == 3`일 때, 다른 모드 도움말 옆):
  - 본문: "지정 스테이지 데이터: 이 전투 스테이지를 Dev Node Id 노드(시작 노드와 이어진 노드여야 함)에 실제로 놓고 진입합니다. 챕터는 그 스테이지의 맵이라 Dev Chapter Key는 쓰지 않습니다. DB 스테이지 루트 표에 등록된 일반 전투 스테이지만 됩니다(보스·미등록은 재생 시작에서 멈춤). Database 정의에서만 동작하며 Fixture는 개발 고정 전투로 들어갑니다."
  - 종류: `definition == 1 && stageData != null`이면 Info, 아니면 Warning.
  - `!stageDataRead`면 Warning을 하나 더: "저장된 Dev Stage Data 에셋을 찾지 못했습니다. 다시 고르세요."
- using `ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat`.

## 4. DP-a — 검토와 기록 (T12~T13)

### T12 검토

- 새 에이전트 하나가 바꾼 파일만 읽고 검토한다. 이 계획과 설계는 넘기지 않고, 목표 한 문단만 준다.
- 볼 것:
  - 운영 `EnterChapter(int)`·`PrepareGraph(int)` 동작이 같은지.
  - 고정 검사 셋과 갈래 예외.
  - 시험 가짜 여섯이 다 맞았는지.
  - 다른 개발 모드(Node·Boss·Selected)와 Fixture 갈래.
  - 덮어쓰기 GUID 읽기.
  - 컴파일 위험(using, 오버로드 모호성 — 인자 하나짜리 호출은 `EnterChapter(int)`로 정해짐).
- 높음·중간은 고치고, 낮음은 반영하거나 기록한다. 실행 기록 §6에 남긴다.

### T13 문서

- 설계 머리 상태를 갱신한다(DP-a 코드 완료, 계획 링크).
- CLAUDE.md 현재 상태 줄을 갱신한다(커밋할 새 파일 둘 + `.meta`).
- 공유는 Core 전달 문서 §5.53 하나뿐이다(T5). 민형님·현강님·호진님 공유는 없다.

## 5. Unity 확인 (사용자)

1. 컴파일 오류가 없는지. 기존 EditMode 시험에 새 실패가 없는지. 특히 조정자 가짜가 있는 여덟 파일(§6.1 1번 포함)과 `ChapterGraphGenerationTests`·`RunTerminationAdmissionTests`·`LobbyDepartureIntegrationTests`를 본다.
2. Play From Boot 설정: Combat 시작, Use Dev Progress, Database, PlacedStage, `Combat_Normal_E`, Dev Node Id 2. 재생하면 다음이 보여야 한다.
   - 콘솔에 "지정 스테이지 Combat_Normal_E(키 1105, 맵 1)를 노드 2에 놓습니다".
   - 전투 필드가 E다.
3. 그 전투를 이긴 뒤 StageSelect에서 노드 2가 E의 외형이고 클리어로 보인다.
4. `Combat_Boss`를 지정하면 재생 시작에서 멈추고, 콘솔 예외에 스테이지 2101과 역할 Boss가 나온다.
5. 표에 등록하지 않은 새 `CombatStageData`(임시로 만들었다가 지움)를 지정하면 "등록되지 않은" 예외로 멈춘다.
6. 아무것도 고르지 않고 PlacedStage면 "Dev Stage Data가 비었습니다"로 멈춘다.
7. Fixture + PlacedStage면 경고 한 줄 뒤 Fixture 전투로 들어간다.
8. Dev Node Id 1이면 Core 고정 검사 예외(시작 노드)로 멈춘다.
9. Node·Boss·Selected 모드, Lobby 시작, 운영 StageSelect → 전투 진입이 지금과 같다.
10. (§6.1 2번) Dev Chapter Key를 2 같은 다른 값으로 두고 PlacedStage로 재생하면, 로그에 "Dev Chapter Key 2 대신 맵 1로" 구절이 나오고 정상 진입한다.
11. (§6.1 3번) PlacedStage에서 에셋을 고른 뒤 그 에셋 GUID가 사라진 상태여도, Lobby 시작이면 재생이 멈추지 않는다.

## 6. 실행 기록

### 6.1 DP-a 코드 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음)

- **사용자 지시:** "DP-a 부터 차례대로 진행해, 검증은 DP 작업 전체 진행 후 마지막에 진행하자". T1~T11을 차례로 하고 T12 검토를 마지막에 한 번 했다. MSBuild·관리 실행·새 `[Test]`는 하지 않았다.
- **새 파일(+ `.meta`):**
  - `CC/Contracts/StageMap/ChapterStagePin.cs`(GUID `9487e84f…`)
  - `D/DevStageRootLookup.cs`(GUID `45d86b92…`)
- **바꾼 파일:**
  - Core: `C/StageSystem/StageGeneratorSystem.cs`, `C/ChapterProgressionCoordinator.cs`
  - DB 입력: `S/Item/StageRootDataSO.cs`
  - 개발 진입: `B/GameBootstrap.cs`, `B/GameBootstrapEditorOverrides.cs`, `E/PlayFromBootSettingsWindow.cs`
  - 시험 가짜 여덟(아래 1)
  - 문서: Core 전달 문서 §5.53, 호진님 메모 `handoffs/2026-10-22-combat-view-structure-hojin-share.md` §13
- **계획과 다른 점:**
  1. **시험 가짜는 여섯이 아니라 여덟이다.** 계획 조사 때 grep 출력이 `head`로 잘려 둘을 놓쳤다(검토 H1).
     - 천명님 `T/Framework/IntimidationBufferTests.cs`(`DefeatProgression`): using + 한 줄.
     - 호진님 `T/Framework/LobbyDepartureIntegrationTests.cs`(`CountingChapterProgression`): 감싸는 가짜라 예외 대신 기존 `EnterChapter(int)`와 같은 꼴(챕터 기록 + 안쪽 전달)로 더했다. 호진님 메모 §13에 적었다.
  2. **진입 요청 챕터 필드 `_devEntryChapterKey`를 더했다.** `TryRequestDevEntry`가 진입 요청에 `devChapterKey`를 쓰고 있었다. 그대로 두면 PlacedStage의 맵과 Dev Chapter Key가 다를 때 챕터가 어긋나 진입이 거절된다(검토 H2, 계획이 놓침). `PrepareDevProgress`가 정한 챕터를 필드에 담아 요청에 쓴다.
  3. **덮어쓰기에서 저장된 에셋을 못 찾을 때 멈추는 조건을 좁혔다.** 계획은 "PlacedStage면"이었다. 지금은 "Combat 시작·개발 진행·Database·PlacedStage 모두"일 때만 멈춘다(검토 L1). Lobby 시작이나 Fixture에서 낡은 GUID 때문에 멈추지 않는다.
  4. **설정 창 라벨을 바꿨다.** "첫 노드에"를 "Dev Node Id 노드에"로 고쳤다(검토 L3, 설계 DS1 문구와 다름). 전투 스테이지 값 비교는 상수 `BossStage`·`SelectedStage`·`PlacedStage`로 바꿨다.
  5. **설정 창은 낡은 GUID를 지우지 않는다.** 저장된 GUID를 읽지 못한 상태(칸이 null)에서 다른 칸만 바꾸면 저장값을 그대로 두고 경고만 남긴다. 에셋을 고르거나, 읽을 수 있던 값을 비우면 그대로 저장한다.
  6. **덮어쓰기 로그 구절은 도우미 `DescribeDevStage`가 만든다**(Selected = 키, PlacedStage = 에셋 이름).
- **T12 검토(새 에이전트 하나, 바뀐 파일만):**
  - 높음 2(H1·H2): 위 1·2로 반영했다.
  - 중간 0.
  - 낮음 3: L1·L3은 위 3·4로 반영했다.
  - L2는 기록만 한다. Core 고정은 깊이 2 이상 중간 노드도 받지만, 개발 직행 진입은 시작 노드에서 이어진 깊이 1 노드만 갈 수 있다. 그래서 Dev Node Id가 깊이 2 이상이면 고정은 되지만 진입이 거절되고, 로그 한 줄만 남긴 채 Boot에서 멈춘다. Node 모드도 같은 성질이라 기존 동작과 같다.
  - 검토가 문제없다고 확인한 것:
    - 이름 충돌이 없다.
    - 오버로드 호출이 모호하지 않다.
    - 계약·구현의 선택 인자가 같다.
    - 전처리 가드가 맞다(비개발 빌드에 남는 것은 필드와 쓰지 않는 using뿐).
    - pin이 null이면 운영 동작이 같다.
    - pin이 조용히 무시되는 갈래가 없다.
    - 실제 에셋에서 카탈로그 → InGameGroup → `StageRootDataTable` → 루트 15개 순회로 루트 하나가 정확히 찾아진다.
- **원칙 자체 점검:** 설계 §5 예상(약 9.0)과 같다고 본다. 늘어난 것은 호진님 가짜 한 곳과 진입 챕터 필드 하나다. 남는 낮음은 넷이다: 개발용 오버로드, 시험 가짜 여덟에 한 줄씩, SO 단계 이름 맞추기, L2.
- **커밋 묶음(사용자):**
  - Core 세 파일(새 계약 포함)과 시험 가짜 여덟은 한 커밋이다. 하나라도 빠지면 시험 어셈블리 컴파일이 깨진다.
  - 개발 진입 넷(`DevStageRootLookup` 포함)과 `StageRootDataSO`는 Core 변경과 같은 커밋이거나 그 뒤여야 한다(새 오버로드를 부름).
  - 새 파일 둘은 `.meta`와 함께 커밋한다. FMOD 캐시는 이번 것이 아니다.

## 7. 후속 후보 (이번 범위 밖)

- Selected 모드 걷기(Q-DS2 B 안). 새 모드가 쓰임새를 대신한다. 걷을 때 EditorPrefs 저장값 2를 처리해야 하고 `DevSelectedStageBattleSource`도 지워야 한다.
- Boss 모드도 노드 배치 방식으로 바꿀지. 보스는 마지막 깊이 노드라 깊이 1 진입 규칙에 막힌다. 지금처럼 전투 정의만 바꾸는 방식이 남는다.
