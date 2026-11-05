# 개발 진입 시작 아이템 지급 계획

- 설계: [dev-starting-items-design](../specs/2026-10-18-dev-starting-items-design.md)(DI1~DI10). 결정 Q-I1~Q-I3 모두 A(2026-10-19).
- 작성: 2026-10-19. 사용자 지시는 "시작 아이템 계획부터 해야 가방 만든 거 테스트가 되니 시작 아이템부터"다. 가방 표시 작업(CLAUDE.md 2026-10-19 결정 ①~⑧)의 시험 전제다.
- 경로 약어:
  - `B/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/`
  - `D/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Dev/`
  - `E/` = `Assets/ProjectL/Editor/Boot/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
- 위치는 메서드 이름으로 적는다.
- **사용자 결정(2026-10-19):** "I-2까지 진행해". §1의 "Phase마다 멈추고 보고"를 이번에 한해 바꿔 I-0~I-2를 이어서 했다. Phase마다 빌드와 새 시험은 확인했고, 관리 실행 전체 비교는 마지막에 한 번 했다. Unity 결과는 I-1·I-2를 한 번에 받는다.

## 0. 착수 조건과 기준선 (I-0, 코드 없음)

1. **기준 커밋:** HEAD `8c34b864`(2026-10-19 현강님 브랜치 Fast-forward 병합 뒤). 앞 기준선(`364936a1`)은 병합으로 낡았다.
2. **csproj 대조:** Unity csproj의 Compile 목록과 디스크 `.cs`를 비교한다. 병합으로 들어온 `PackUI.cs`·`PackExpansion.cs`·`BattleChangerUI.cs`·`BattleChangerUITests.cs`가 csproj에 없으면 extra.targets로 더한다(메모리 `projectl-build-check`).
3. **기준선:**
   - MSBuild 오류·경고 목록을 기록한다.
   - 관리 실행 전체를 메서드 단위로 기록한다.
   - 관리 실행 도구는 이 세션 스크래치에 없으므로 [분리 계획 §7](2026-10-18-combat-scene-event-presenter-split-plan.md#7-관리-실행-방법-세션-스크래치-도구) 방법으로 다시 만든다.
4. **실행 뒤 정리:** 관리 실행이 남기는 `Assets/__TornadoLegacy_*.asset`는 `git status`로 확인하고 지운다.

## 1. 공통 규칙

- **위치·형식:**
  - 새 코드는 `B/`·`D/`에 둔다. 네임스페이스는 폴더 경로를 따르고, Allman, `sealed` 기본, 한국어 XML doc이다.
  - `.meta`는 32자리 hex GUID로 새로 만든다.
- **Core·Buffer·Factory·ServiceKeys·DB 에셋·Boot 씬은 바꾸지 않는다.** Core는 공개 서비스 `core.Items.Grant`만 부르고, `ItemDefinitionAdapter`는 호출만 한다.
- **씬 값과 PC 값:** 시작 아이템은 Play From Boot 설정(이 PC)에서 넣는 것을 기본으로 안내한다. Boot 씬 인스펙터에 넣으면 공용 씬 파일이 바뀐다.
- **Phase마다 확인:**
  - MSBuild 오류 0, 새 경고 0(기준선 대비)
  - 관리 실행 기존 시험 상태 변화 0(메서드 단위로 PASS → 다른 상태 없음), 새 시험 PASS
  - Unity Test Runner 확인 목록과 사용자 재생 확인 목록
- Phase마다 멈추고 보고한다.

## 2. I-1 — 개발 지급 경로 (DI1~DI8)

### T1 한 줄의 형식 `B/DevStartingItem.cs` (새)

- `[Serializable] public struct DevStartingItem`: `[SerializeField] private int itemKey`, `[SerializeField, Min(1)] private int quantity`, 생성자 `(int itemKey, int quantity)`, `ItemKey`·`Quantity` 읽기.
- 조건부 컴파일을 걸지 않는다. 직렬화 필드는 모든 빌드에 있어야 한다.
- 값 검사는 하지 않는다. 판정은 Core가 하고(DI2), 설정 창이 1 미만을 안내한다(T6).
- 공개 형식이다. 설정 창(Assembly-CSharp-Editor)이 쓰기 때문이다.

### T2 지급 함수 `D/DevStartingItemGrant.cs` (새, `UNITY_EDITOR || DEVELOPMENT_BUILD`)

- **`internal static ItemInventorySnapshot Apply(IItemInventoryService items, IReadOnlyList<DevStartingItem> entries)`**
  - 인자가 `null`이면 `ArgumentNullException`.
  - 목록 순서대로 `items.Grant(key, quantity)`를 부른다. 목록이 비면 부르지 않고 `null`을 돌려준다.
  - 거절이면 `InvalidOperationException`을 던진다. 문구: `[DevStartingItems] 개발 시작 아이템 지급이 거절됐습니다. 줄 {n}, 키 {key}, 수량 {quantity}, 사유 {Reason}`. 남은 줄은 부르지 않는다(DI4).
  - 결과가 `null`이면 `InvalidOperationException`.
  - 반환은 마지막 결과의 `Change.Final`이다.
- **`internal static string Describe(IReadOnlyList<DevStartingItem> entries, ItemInventorySnapshot final)`**
  - 로그 한 줄(DI7)을 만든다. 예: `1×2, 4×1 → 재고 {1:2, 4:1}`. 재고는 키 순이다.

### T3 지급 시험 `T/DevStartingItemGrantTests.cs` (새, 관리 실행 대상)

- 가짜 `IItemInventoryService`(호출 기록 + 미리 정한 결과)를 쓴다. `IServiceModule`은 빈 표식 인터페이스다.
- 사례:
  - 빈 목록 → 호출 0, `null`
  - 두 줄 → 순서대로 호출, 마지막 `Final` 반환
  - 둘째 줄 거절 → 예외 문구에 줄·키·수량·사유가 있고, 셋째 줄은 부르지 않음
  - `null` 인자 둘 → `ArgumentNullException`
  - `Describe` 형식과 키 순서

### T4 Bootstrap `B/GameBootstrap.cs`

- **필드:** 개발 설정 묶음(`#pragma warning disable CS0414` 안), `devBattleStage` 다음에 `[SerializeField] private DevStartingItem[] devStartingItems = Array.Empty<DevStartingItem>();`. Tooltip: "새 Run 직후 지급할 아이템(키·수량). Combat 직접 시작의 개발 진행에서만 쓴다".
- **`PrepareDevProgress`:** `StartNewRun` 성공 확인 뒤, `EnterChapter` 앞에서 지급한다(DI3).
  - `DevStartingItemGrant.Apply(core.Items, devStartingItems ?? Array.Empty<DevStartingItem>())`
  - 결과가 `null`이 아니면 `Debug.Log("[GameBootstrap] 개발 시작 아이템 지급: " + Describe(...))`
  - 순서 이유를 주석 한 줄로 단다: 새 Run이 재고를 비우고, 챕터 진입은 재고를 건드리지 않는다.
- **`ApplyEditorOverrides`:**
  - `devStartingItems = GameBootstrapEditorOverrides.DevStartingItems;` 저장값을 읽지 못하면 그 예외로 시작을 멈춘다. 기존 열거형 검사와 같다.
  - 덮어쓰기 로그에 `시작 아이템 {n}줄`을 더한다.
- 클래스 머리말 remarks의 개발 설정 문장에 "시작 아이템 지급"을 더한다.

### T5 PC 덮어쓰기 `B/GameBootstrapEditorOverrides.cs`

- **키:** `ProjectL.BootOverride.DevStartingItems`(문자열, JSON). 감싸는 형식은 `[Serializable] private sealed class StoredItems { public DevStartingItem[] items; }`이다(`JsonUtility`).
- **`public static DevStartingItem[] DevStartingItems`**
  - get: 키가 없거나 빈 문자열이면 빈 배열이다. 읽지 못하면 `InvalidOperationException("[Play From Boot] 시작 아이템 설정을 읽지 못했습니다. 설정 창에서 비우고 다시 넣으세요.")`.
  - set: `null`이나 빈 배열이면 키를 지운다. 없음 = 빈 목록이라, 시험 도우미가 되돌리기 쉽다.
- **`public static bool TryReadDevStartingItems(out DevStartingItem[] items)`:** 설정 창용이다. 읽지 못하면 `false`.

### T6 설정 창 `E/PlayFromBootSettingsWindow.cs`

- **자리:** 개발 진행 묶음, `Dev Battle Stage` 아래, "개발 전투 필드" 앞. 같은 비활성 조건(Combat 시작 + Use Dev Progress)을 따른다.
- **화면:**
  - 굵은 제목 "시작 아이템 (새 Run 직후 지급)"
  - 줄마다 `키`·`수량` IntField와 "삭제" 버튼, 아래에 "추가" 버튼(키 1·수량 1로 추가)
  - 값 입력은 바뀐 때만 저장하고, 추가·삭제는 누른 즉시 저장한다.
- **안내:**
  - 1 미만이 있으면 Error: "키와 수량은 1 이상이어야 합니다. 없는 키는 재생 시작 때 Core 사유(UnknownItem)로 멈춥니다."
  - 저장값을 읽지 못하면 Error + "비우기" 버튼
- 창 최소 높이가 모자라면 `minSize`를 늘린다(지금 360×460, 스크롤 있음).

### I-1 확인

- **관리 실행:** T3 PASS, 기존 상태 변화 0.
- **Unity Test Runner:** Boot 시험 묶음(`LobbyBootSmokeTests`, `CombatTimelineRetirementBootSmokeTests`, `HojinCombatAttackDragIntegrationTests`, `StageLegendBootSmokeTests` 등). 이 PC의 시작 아이템을 비운 상태에서 돌린다. 격리는 I-2의 T9다.
- **사용자 재생:**
  1. 설정 창에서 Combat 시작 + Database 정의 + `1×2`·`4×1` → 콘솔 지급 로그·재고 `{1:2, 4:1}`
  2. 키 99 → 진입하지 않고 `UnknownItem` 사유 예외
  3. 목록 비움 → 지급 로그 없음
  4. 덮어쓰기 끔 → 씬 값(빈 목록)
- **I-1만으로는 Fixture 전투에서 장착할 수 없다.** 재고 확인만 된다. 장착 시험은 I-2 뒤 가방 작업에서 한다.

## 3. I-2 — Fixture 아이템 목록·시험 격리 (DI9·DI10)

### T7 Fixture 정의 `D/DevBattleInitializationSource.cs`

- **`Read`:** `ValidateCombatStage` 뒤에 운영 DB 소스와 같은 방식으로 아이템 목록을 만든다. `(database.InGameQuery as IHasConsumableItemTable)?.ConsumableItems` → `ItemDefinitionAdapter.Build(GetAll())`.
  - 표가 없는 조회(`TemporaryCoreDatabase` 등)는 `null` → 아이템 없이 만든다. 운영 소스와 같고, 기존 시험이 쓰는 경로다.
- **`CreateDefinition`:** 아이템 목록을 받아 `new BattleInitialization(..., profiles, itemDefinitions: items)`로 넘긴다.
- **remarks:** "아이템 정의는 DB 카탈로그 전체(운영 DB 소스와 같은 선택)"를 더한다. Core가 선택 규칙을 바꾸면 따라야 한다는 점도 적는다(설계 §4.1).

### T8 Fixture 아이템 시험 `T/DevBattleItemDefinitionTests.cs` (새)

- **(관리 실행)** `TemporaryCoreDatabase`로 `Read` → `ItemDefinitions`가 비어 있음.
- **(Unity)** 실제 카탈로그(`DataBaseFactory.Create(DatabaseCatalogSO)`)로 `Read` → `ItemDefinitions` 키 = 카탈로그 키(지금 1~4).
  - 스테이지 키는 DB의 전투 스테이지 하나로, 착수 때 확인해 상수로 둔다.
- **(Unity)** 장착 수락: Fixture 정의로 준비 → `core.Items.Grant(1, 2)` → 메인 배치 → `EquipItem(main, 1)` → 재고 1·장착 키 1.
  - 조립은 `CombatItemIntegrationTests`의 도우미(`Runtime`·`Prepare`·`Command`)를 대조해 이 파일에 최소로 둔다. 그 파일(천명님 작성)은 고치지 않는다.

### T9 Boot 시험 격리 `T/LobbyBootSmokeTests.cs`

- **저장·되돌림:**
  - 문자열 키 목록 `StringPreferences = { "DevStartingItems" }`를 둔다.
  - `SavePreference`·`RestorePreference`에 문자열 경로(`SessionState.SetString`/`GetString`, 없던 키는 삭제)를 더한다.
- **`ConfigureBoot`:** 저장 뒤 `GameBootstrapEditorOverrides.DevStartingItems = Array.Empty<DevStartingItem>()`(키 삭제)로 정한다.
  - Combat 개발 진행으로 시작하는 Boot 시험(`CombatTimelineRetirementBootSmokeTests`·`HojinCombatAttackDragIntegrationTests`, 이 파일의 Combat 사례)은 모두 `ConfigureCombatGraphBoot` → `ConfigureCatalogBoot` → `ConfigureBoot`를 지나므로 한 곳이면 된다.
  - 이 도우미를 쓰지 않는 `BootStageStartOverride`·`ConnectedWallBootSmokeTests`는 Stage 시작이거나 개발 진행을 끄므로 시작 아이템이 적용되지 않는다(DI1).
- `Cleanup_RestoresOriginalPlayStartSceneAndOverridePresence`가 깨지지 않는지 확인한다.
- 여러 작업자가 쓰는 공용 도우미이므로 필요한 줄만 바꾸고, 보고에 적는다.

### I-2 확인

- **관리 실행:** T8 관리 사례 PASS, 기존 상태 변화 0. `TemporaryCoreDatabase`를 쓰는 Fixture 시험(`CharacterActionCostPublicationTests` 등)은 그대로여야 한다.
- **Unity Test Runner:** T8 전체, Boot 시험 묶음(시작 아이템을 넣어 둔 상태에서도 같은 결과), `CombatItemIntegrationTests`, Fixture 개발 전투를 쓰는 시험.
- **사용자 재생:** Fixture 정의 + 시작 아이템 → 진입 정상, 지급 로그. 장착은 가방 작업 전이라 화면에서는 확인할 수 없다(T8 장착 수락 시험이 대신한다).

## 4. 공유·기록

- Core 파일을 바꾸지 않으므로 Core 전달 문서에는 적을 것이 없다.
- `LobbyBootSmokeTests` 변경(T9)은 공용 시험 도우미라 보고에 적는다. 별도 공유 문서는 쓰지 않는다. 다른 작업자의 Boot 시험이 EditorPrefs 키 하나를 더 저장·되돌리게 된다.
- 커밋 때 새 파일 `DevStartingItem.cs`·`DevStartingItemGrant.cs`·`DevStartingItemGrantTests.cs`·`DevBattleItemDefinitionTests.cs`를 `.meta`와 함께 넣는다.

## 5. 실행 기록

### 5.1 I-0 기준선 (2026-10-19, HEAD `8c34b864`)

- **csproj 대조:** 모든 csproj의 Compile 목록과 디스크 `.cs`(FMOD 플러그인 제외 2,257개)를 비교했다. 디스크에만 있는 파일은 0개다. 병합분(`PackUI`·`BattleChangerUI` 등)도 이미 들어 있다.
- **MSBuild(`ProjectL.Tests.Framework.csproj`):** 오류 0, 고유 경고 34개(기준 목록은 스크래치 `base/warnings.txt`).
- **관리 실행 도구:** 이전 세션 스크래치의 `Runner.cs`·`Runner.exe`·`build.sh`·`runeach.sh`(S-0 판)를 이 세션 스크래치로 복사하고 경로만 바꿨다. 판정·순번 실행은 분리 계획 §7과 같다.
- **관리 실행 전체(기준선 산출물, 5,436개):** PASS 3,026 · NATIVE 2,269 · FAIL 92 · UNSUPPORTED 49. 비교는 §5.4.
- **실행 뒤 정리:** 이번 실행이 남긴 `Assets/__TornadoLegacy_45223f07797c4dbc8b19f571b56e68d6.asset`(+ Unity가 만든 `.meta`, untracked)을 지웠다. 커밋돼 있는 이전 두 벌은 그대로 두었다.
- **발견(보고만, 손대지 않음):** 실행 도중(01:22~01:23) `DataBaseSO/InGameGroup/ConsumableItem/Item/`에 `CactusCannon-1.fbx`·`bomber_.fbx`(+ Unity가 만든 `.meta`, untracked)가 생겼다. 이 작업이 만든 파일이 아니다. 에디터에서 넣은 것으로 보이며, I-2 실행이 끝났을 때는 없어져 있었다.

### 5.2 I-1 지급 경로 (2026-10-19)

- **새 파일:** `B/DevStartingItem.cs`, `D/DevStartingItemGrant.cs`, `T/DevStartingItemGrantTests.cs`(+ `.meta`, GUID 32자리).
- **바꾼 파일:**
  - `B/GameBootstrap.cs`: 직렬화 목록, `PrepareDevProgress`에서 `StartNewRun` 뒤·`EnterChapter` 앞 지급과 로그, `ApplyEditorOverrides` 적용·로그
  - `B/GameBootstrapEditorOverrides.cs`: `DevStartingItems`(JSON 키, 빈 목록 = 키 삭제), `TryReadDevStartingItems`
  - `E/PlayFromBootSettingsWindow.cs`: `DrawStartingItems`
- **계획과 다른 점:** `Describe`·`Apply`의 `null` 인자는 모두 `ArgumentNullException`으로 했다. Bootstrap은 직렬화 값이 `null`일 때를 대비해 빈 배열로 바꿔 넘긴다.
- **MSBuild:**
  - `ProjectL.Tests.Framework.csproj`(새 파일은 extra.targets): 오류 0, 경고 목록은 기준선과 같다. 산출 DLL에 새 타입이 들어간 것을 확인했다.
  - `Assembly-CSharp-Editor.csproj`(설정 창): 오류 0, 바꾼 파일의 경고 0.
  - 개발 정의를 뺀 `ProjectL.Systems.csproj`(비개발 빌드): 오류 0, 바꾼 파일의 경고 0.
- **새 시험(관리 실행):** `DevStartingItemGrantTests` 6/6 PASS.

### 5.3 I-2 Fixture 아이템 목록·시험 격리 (2026-10-19)

- **새 파일:** `T/DevBattleItemDefinitionTests.cs`(+ `.meta`). 실제 DB 시험의 전투 스테이지는 1101(Stage_Normal_A)이다.
- **바꾼 파일:**
  - `D/DevBattleInitializationSource.cs`: `ReadItemDefinitions` → `CreateDefinition(items)` → `itemDefinitions:`, remarks
  - `T/LobbyBootSmokeTests.cs`: `StringPreferences`, `SaveStringPreference`·`RestoreStringPreference`, `ConfigureBoot`에서 시작 아이템 비움
- **MSBuild:** 오류 0, 경고 목록은 기준선과 같다.
- **새 시험(관리 실행):** `DatabaseWithoutItemTable_BuildsWithoutItems` PASS. 실제 카탈로그를 읽는 두 사례(`ProductionCatalog_FixtureCarriesTheCatalogItems`·`ProductionCatalog_GrantedItemCanBeEquippedInFixtureBattle`)는 `AssetDatabase` 때문에 NATIVE라 Unity 확인이 필요하다.

### 5.4 관리 실행 비교와 Unity 확인 목록

- **I-2 최종 코드 관리 실행(5,445개):** PASS 3,033 · NATIVE 2,271 · FAIL 92 · UNSUPPORTED 49.
  - 기준선 대비 메서드 이름 단위로 **기존 5,436개의 상태 변화 0**이다. 기준선에만 있는 이름도 0개다.
  - 새로 생긴 이름은 9개다: `DevStartingItemGrantTests` 6 PASS, `DevBattleItemDefinitionTests` 1 PASS · 2 NATIVE.
  - FAIL 92는 기준선과 같은 이름들이다(이번 범위 밖).
- **실행 뒤 정리:** 이 실행이 남긴 `Assets/__TornadoLegacy_5e1150a67a37423ba5de2718fd91e1fc.asset`(+ `.meta`, untracked)을 지웠다.
- **Unity Test Runner 확인 목록(사용자):**
  - `DevBattleItemDefinitionTests` 전체(특히 실제 카탈로그 두 사례: 아이템 목록 일치, 지급 → 장착 수락)
  - `DevStartingItemGrantTests`
  - Boot 시험: `LobbyBootSmokeTests`(특히 `Cleanup_RestoresOriginalPlayStartSceneAndOverridePresence`), `CombatTimelineRetirementBootSmokeTests`, `HojinCombatAttackDragIntegrationTests`, `StageLegendBootSmokeTests`, `LobbyDeckBootSmokeTests`, `LobbyDepartureBootSmokeTests`, `ArtifactPopupBootSmokeTests`, `StageMapNodeMovementBootSmokeTests`
    - 설정 창에 시작 아이템을 넣어 둔 상태로 한 번 돌려, 결과가 같고 끝난 뒤 설정이 그대로 돌아오는지 본다(T9).
  - `CombatItemIntegrationTests`, `CharacterActionCostPublicationTests`(Fixture 정의)
- **사용자 재생 확인:**
  1. Play From Boot 설정 → Combat 시작 + Use Dev Progress + 시작 아이템 `1×2`·`4×1` → 콘솔에 `개발 시작 아이템 지급: 1×2, 4×1 → 재고 {1:2, 4:1}`, 진입 정상(Fixture·Database 둘 다)
  2. 키 99 → 진입하지 않고 `[DevStartingItems] … 키 99 … 사유 UnknownItem` 예외
  3. 목록 비움 → 지급 로그 없음, 기존과 같은 진입
  4. 덮어쓰기 끔 → 씬 값(빈 목록)으로 시작
  5. 설정 창: 추가·삭제·값 변경이 다음 재생에 반영되는지, 키 0 입력 시 오류 안내
- **Unity 확인(2026-10-19, 사용자):** 문제 없음.
- **화면에서 장착은 아직 확인할 수 없다.** 가방 드래그 장착(가방 작업)이 없기 때문이다. 장착 수락은 `ProductionCatalog_GrantedItemCanBeEquippedInFixtureBattle`이 대신한다.
- **알려 둘 점:** 장착 결과에는 `CombatItemEvent`가 들어 있다. 이 사건은 재생 경로 표에 없어서, 장착하면 "표시 계약 없음" 경고가 사건마다 남는다. 가방 결정 ③(경로 표에 추가)에서 없어진다.
- **커밋 때 함께 넣을 새 파일:** `B/DevStartingItem.cs`, `D/DevStartingItemGrant.cs`, `T/DevStartingItemGrantTests.cs`, `T/DevBattleItemDefinitionTests.cs`(각 `.meta` 포함).

### 5.5 천명님 런 덱 변경 뒤 시험 맞춤 (2026-10-20)

- 병합 `a8d0ce97`(천명님 `f6a18f3f`·`16333690` — 런 덱·Main별 상태)으로 `NewRunPreparation` 생성자가 Run ID·초기 덱 지급 index를 받고, 전투 준비가 참가 목록(지금 Run 덱)과 로스터 버전을 요구하게 됐다. `DevBattleItemDefinitionTests.ProductionCatalog_GrantedItemCanBeEquippedInFixtureBattle`가 옛 생성자로 시험 어셈블리 컴파일을 막아(CS7036, 사용자 보고) 새 Run은 시험 도우미 `CharacterBufferTestData.NewRun`(0번 = 메인 덱)으로, 전투 준비는 `RunDeckReadSink.TryGetCurrent`의 덱·`TryGetMainState(deck.MainId)`·`RosterRevision`을 넘기는 꼴(`CombatTestFixtures.PrepareCommitted`와 같음)로 고쳤다. 시험 뜻(지급 아이템을 Fixture 전투에서 장착)은 그대로. MSBuild·Unity 확인 전.
