# 개발 진입 시작 아이템 지급 설계 (2026-10-18)

- **목표:** Combat 직접 시작(Play From Boot 개발 진행)에서 새 Run을 만든 직후 정해 둔 아이템(키·수량 목록)을 Run 재고에 넣는다. View 쪽 아이템 화면·장착 입력을 만들 때 상점·보상 없이 바로 시험하기 위해서다.
- **상태:** **I-0~I-2 코드 완료, Unity 확인 문제 없음(2026-10-19, 사용자 확인).** **결정 완료(2026-10-19): Q-I1~Q-I3 모두 A(§6)** — DI9 Fixture 아이템 목록 넣기, 설정 창은 키·수량만, DI10 Boot 시험 도우미가 비우고 되돌림. Task 계획: [dev-starting-items-plan](../plans/2026-10-19-dev-starting-items-plan.md)(I-0 기준선 → I-1 지급 경로 → I-2 Fixture 아이템 목록·시험 격리). 가방 표시 작업(같은 날 결정)의 시험 전제다.
- **사용자 요청(2026-10-18):** "GameBootstrap 개발 진입에 시작 아이템 지급(키·수량 목록, Play From Boot 설정 포함)을 넣는 설계부터".
- **범위 밖:** 운영 시작 아이템(로드아웃 규칙). 원본은 `PlayerLoadoutData` 한 곳이어야 하고, 새 Run 거래가 로스터·재화와 함께 한 번에 설치해야 하므로 Core 일이다(§5). 아이템 화면·장착 입력 자체는 이 설계가 다루지 않는다.

## 1. 지금 구조 (코드 확인, 2026-10-18)

| 대상 | 지금 |
|---|---|
| 새 Run의 재고 | `StartNewRun` → `ProgressTransactions.ResetRunProgress` → `_items.Reset()`으로 **빈 재고**. 채우는 코드는 없다. `PlayerLoadoutData`에는 메인·시작 서브·첫 손패 수·시작 Run 재화만 있다 |
| 재고에 넣는 공개 경로 | `IInGameCoreSystemGroupService.Items.Grant(itemKey, quantity)` → `ItemGrantResult(Accepted, Reason, Change)`. 판정은 Core(`ItemInventoryCalculator`), 설치는 Buffer 거래(`ItemInventoryTransactions`). 지금은 시험 코드만 부른다 |
| 지급 거절 사유 | `UnknownItem`(DB 카탈로그에 없는 키), `InvalidQuantity`(수량 0 이하), `QuantityOverflow`, `RunEnded`(Run이 진행 중이 아님). 아이템 테이블이 없는 조립이면 예외 |
| 지급이 보는 카탈로그 | `ItemInventoryService.InitData`가 실제 DB의 `ConsumableItemDataTable`을 읽는다. 전투 정의 소스(Fixture/Database)와 상관없다. 지금 DB: 1 Whiskey, 2 Roket, 3 Rope, 4 Lantern |
| 재고 수명 | 같은 Run의 다음 스테이지로 이어진다. `EnterChapter`는 재고를 건드리지 않는다. 장착된 아이템은 전투 라운드 끝에 사라지고 돌려주지 않는다 |
| 장착 | `CombatViewCommandData.EquipItem/ReplaceItem/UnequipItem` → `ItemEquipmentCalculator`. 배치 국면·플레이어 입력 중, 필드의 메인·서브만. **전투 정의의 `ItemDefinitions`에 키가 있어야 한다**(없으면 `UnknownItem`) |
| 전투 정의의 아이템 목록 | Database 소스(`DatabaseBattleInitializationSource`, 보스 개발 소스도 이것을 씀)는 DB 카탈로그 전체를 `ItemDefinitionAdapter.Build`로 넣는다. **Fixture 소스(`DevBattleInitializationSource`)는 넣지 않아 비어 있다** → Fixture 전투에서는 재고가 있어도 장착이 거절되고 아이템 효과도 돌지 않는다 |
| 개발 진행 준비 | `GameBootstrap.PrepareDevProgress`(Combat 시작 + `useDevProgress`일 때만): `StartNewRun(devLoadoutKey)` → `EnterChapter(devChapterKey)`. Core 공개 서비스만 부르고 Buffer에 쓰지 않는다. 거절은 예외 → 오류 경계(F1) → 진입하지 않음(View 설계 §8.2) |
| 개발 값의 출처 | 씬 `GameBootstrap` 직렬화 값(공용 기본값) + 이 PC의 `GameBootstrapEditorOverrides`(EditorPrefs). 덮어쓰기가 켜지면 재생 시작 때 모든 개발 값을 바꾸고 콘솔에 한 줄 남긴다 |
| 설정 창 | `Assets/ProjectL/Editor/Boot/PlayFromBootSettingsWindow.cs`(asmdef 없음 → Assembly-CSharp-Editor). 공개 형식만 쓸 수 있다 |
| Boot 시험 | Combat으로 시작하는 Boot 시험 여럿이 공용 도우미 `LobbyBootSmokeTests.ConfigureCatalogBoot`로 덮어쓰기를 켜고 값을 정한 뒤, 원래 EditorPrefs를 SessionState에 저장했다가 되돌린다(bool·int 키 목록만) |

## 2. 결정

### DI1 적용 범위

Combat 직접 시작의 개발 진행 준비(`PrepareDevProgress`가 도는 경우)에만 적용한다. Lobby·Stage 시작, 운영 새 Run(`LobbyRunStartJob`·`StageJob`·`CombatExitJobs`)에는 적용하지 않는다. 목록이 비어 있으면(기본값) 지금과 동작이 같다.

### DI2 지급 경로

Core 공개 서비스 `core.Items.Grant(itemKey, quantity)`만 부른다. 키 존재·수량 판정은 Core가 한다. View는 Buffer에 쓰지 않고, 아이템 규칙을 흉내 내 미리 검사하지 않는다(View 설계 §8.2와 같다).

### DI3 시점

`StartNewRun` 성공 직후, `EnterChapter` 전에 목록 순서대로 지급한다.

- 새 Run이 재고를 비우므로 반드시 그 뒤여야 한다. `EnterChapter`는 재고를 건드리지 않으므로 앞뒤가 결과를 바꾸지 않는다. "Run 시작 때 보유"라는 뜻에 맞게 Run 준비 쪽에 둔다.
- 나중에 Core가 운영 시작 아이템을 `StartNewRun` 안에 넣으면, 이 개발 지급은 그 위에 더하는 값이 된다.

### DI4 실패 처리

지급이 거절되면 키·수량·사유를 담은 `InvalidOperationException`을 던진다. 기존 새 Run·챕터 거절과 같이 오류 경계가 받아 진입하지 않는다.

- 거절 앞에서 이미 지급된 줄은 남지만, 첫 오류 뒤 런타임은 다시 돌지 않으므로(새 재생 = 새 런타임) 되돌리지 않는다.
- 같은 키를 여러 줄 적으면 Core 지급이 더한다. 따로 막지 않는다.

### DI5 설정 값의 형태

- **한 줄의 형식:** `DevStartingItem`(키·수량) 공개 직렬화 구조체 하나를 씬 값과 PC 덮어쓰기가 함께 쓴다(형식 원본 하나). 키는 `int`다. 다른 개발 값(로드아웃·챕터 키)과 같고, EditorPrefs에 그대로 담을 수 있다.
  - 위치: `ViewSystem/Boot/DevStartingItem.cs`. 직렬화 필드가 모든 빌드에 있어야 하므로 조건부 컴파일을 걸지 않는다.
- **씬 값:** `GameBootstrap.devStartingItems`(배열, 기본 빈 배열). 기존 개발 설정 묶음(`#pragma warning disable CS0414` 안)에 둔다. 비개발 빌드에서는 읽지 않는다.
- **PC 덮어쓰기:** `GameBootstrapEditorOverrides.DevStartingItems`. EditorPrefs 문자열 키 하나(`ProjectL.BootOverride.DevStartingItems`)에 JSON으로 담는다. 키가 없으면 빈 목록이다.
  - 덮어쓰기가 켜지면 다른 값과 같이 목록 전체를 바꾼다. 빈 목록도 "지급 없음"으로 덮어쓴다.
  - 저장 문자열을 읽지 못하면 재생 시작은 예외로 멈추고("설정 창에서 다시 고르세요", 기존 열거형 검사와 같은 문구 방식), 설정 창은 오류와 "비우기" 버튼을 보인다.

### DI6 지급 기능의 자리

지급 반복·거절 예외·로그용 요약은 개발 전용 정적 함수 `DevStartingItemGrant.Apply(IItemInventoryService items, IReadOnlyList<DevStartingItem> entries)`에 둔다.

- 위치: `ViewSystem/Dev/DevStartingItemGrant.cs`(`UNITY_EDITOR || DEVELOPMENT_BUILD`).
- 반환: 마지막 지급의 `Change.Final`(재고). 목록이 비면 아무것도 부르지 않고 `null`을 돌려준다.
- `GameBootstrap`은 `PrepareDevProgress`에서 순서(DI3)만 정하고 한 번 부른다. 이유는 §4.5 점검을 본다.

### DI7 로그

지급이 있었을 때만 한 줄 남긴다. 예: `[GameBootstrap] 개발 시작 아이템 지급: 1×2, 4×1 → 재고 {1:2, 4:1}`. 재고는 Grant 결과의 `Change.Final`을 쓴다(Buffer를 따로 읽지 않는다). `ApplyEditorOverrides`의 덮어쓰기 로그에도 시작 아이템 줄 수를 더한다.

### DI8 설정 창

`PlayFromBootSettingsWindow`의 개발 진행 묶음 안, 전투 스테이지 아래에 "시작 아이템 (새 Run 직후 지급)" 칸을 둔다.

- 켜지는 조건: 다른 개발 진행 값과 같이 Combat 시작 + Use Dev Progress.
- 줄마다 키·수량 입력과 삭제 버튼, 아래에 추가 버튼. 바뀐 때만 저장한다(지금 창 방식).
- 키나 수량이 1보다 작은 줄이 있으면 오류 안내를 보인다. 키가 DB에 있는지는 창이 검사하지 않는다(Q-I2). 없는 키는 재생 시작 때 Core 사유 `UnknownItem`으로 멈춘다.
- Fixture 정의일 때의 안내는 Q-I1 결정을 따른다.

### DI9 Fixture 전투의 아이템 목록 (Q-I1 추천안 A)

`DevBattleInitializationSource.Read`가 이미 받는 `IDataBaseQueryContext`에서, 운영 DB 소스와 같은 방식으로 아이템 목록을 만들어 전투 정의에 넣는다.

- 방식: `(InGameQuery as IHasConsumableItemTable)?.ConsumableItems` → `ItemDefinitionAdapter.Build(GetAll())` → `BattleInitialization(..., itemDefinitions: items)`.
- 아이템 값은 Fixture에 베끼지 않는다. 카탈로그는 DB 한 곳이고, 변환과 검증은 Core 어댑터 한 곳이다.
- 이 소스는 View 표시 코드가 아니라 Core에 주는 개발 준비 입력이다(View 설계 §8). 이미 같은 쿼리로 `StageTableQuery`를 읽고 있다.
- 결과: Fixture 전투에서도 지급한 아이템을 장착할 수 있고, 아이템 효과가 운영 DB와 같이 돈다.

### DI10 Boot 시험 격리 (Q-I3 추천안 A)

공용 도우미 `LobbyBootSmokeTests`가 시작 아이템 키도 저장·되돌린다(문자열 키 지원 추가). `ConfigureBoot`가 시작 아이템을 빈 목록으로 정한다.

- 이렇게 하면 이 PC에 저장한 시작 아이템이 Combat Boot 시험에 들어가지 않는다.
- 시험이 끝나면 사용자의 저장값이 그대로 돌아온다.

## 3. 바뀌는 파일

| 파일 | 변경 | 소유 |
|---|---|---|
| `ViewSystem/Boot/DevStartingItem.cs`(새) | 키·수량 직렬화 구조체 | View |
| `ViewSystem/Dev/DevStartingItemGrant.cs`(새) | 지급 반복·거절 예외·요약(DI6) | View(개발 전용) |
| `ViewSystem/Boot/GameBootstrap.cs` | 직렬화 목록, `PrepareDevProgress`에 지급 한 번(DI3), 덮어쓰기 적용·로그 | View |
| `ViewSystem/Boot/GameBootstrapEditorOverrides.cs` | `DevStartingItems` 읽기·쓰기(JSON 문자열 키, DI5) | View(에디터 전용) |
| `Assets/ProjectL/Editor/Boot/PlayFromBootSettingsWindow.cs` | 시작 아이템 칸(DI8) | View |
| `ViewSystem/Dev/DevBattleInitializationSource.cs` | 아이템 목록 넣기(DI9, Q-I1 A일 때) | View(개발 전용) |
| `Assets/Tests/EditMode/Framework/LobbyBootSmokeTests.cs` | 문자열 키 저장·되돌림, 빈 목록 지정(DI10, Q-I3 A일 때). 여러 작업자가 쓰는 공용 도우미라 필요한 줄만 바꾼다 | 공용 시험 |
| `Assets/Tests/EditMode/Framework/DevStartingItemGrantTests.cs`(새) | 가짜 `IItemInventoryService`로 순서·거절 예외·빈 목록·요약 검사(Unity 없이 관리 실행) | View 시험 |

Core·Buffer·Factory·ServiceKeys·DB 에셋·Boot 씬은 바꾸지 않는다. Boot 씬은 새 필드가 기본 빈 배열이라 저장하지 않아도 된다. 새 `.cs`는 `.meta`와 함께 다룬다.

## 4. 원칙 점검

기준은 [View 설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)다.

### 4.1 SSOT

| 사실 | 원본 | 이 설계 |
|---|---|---|
| 아이템 키·효과 | DB `ConsumableItemDataTable` | 개발 목록은 키만 든다. 이름·효과를 베끼지 않는다 |
| 지급 가능 판정·수량 | Core `ItemInventoryCalculator` | View는 미리 검사하지 않고 거절 사유를 그대로 보고한다 |
| Run 재고 | Buffer `ItemInventoryBuffer` | 쓰기는 Core 지급 거래만. 로그는 Grant 결과를 쓴다 |
| 개발 시작 아이템 값 | 씬 값(공용 기본) + PC 덮어쓰기 | 기존 개발 값과 같은 두 층이다. 덮어쓰기가 켜지면 PC 값이 이긴다. 형식은 `DevStartingItem` 하나 |
| 전투 정의의 아이템 목록 | Database 소스의 선택(카탈로그 전체) | DI9가 같은 두 줄(테이블 → 어댑터)을 Fixture에 둔다. Core가 선택 규칙을 바꾸면(예: 스테이지별 목록) Fixture도 따라야 한다. Fixture 머리말에 유닛 수치 사본처럼 적어 둔다 |

### 4.2 SOLID

- **S:** `GameBootstrap`은 개발 진행 준비의 순서(B-3)만 더 갖는다. 지급 반복·실패 문구·요약은 `DevStartingItemGrant`, 저장 형식은 덮어쓰기 클래스, 입력 화면은 설정 창이 맡는다.
- **O:** 운영 경로·기존 개발 값 코드는 바뀌지 않고 목록 하나와 호출 하나가 더해진다.
- **I·D:** 지급 함수는 `IItemInventoryService` 하나만 받는다. Buffer 조회·Core 그룹 전체를 넘기지 않는다.

### 4.3 Framework 계약

- 새 서비스·모듈 등록이 없다(FW-001·002 영향 없음).
- 지급은 `Initialize`·아티팩트 보드 준비가 끝난 뒤 `ContinueStartup` 안에서 부른다. 수명 단계 순서(FW-016)를 바꾸지 않는다.
- Core 공개 경로만 부르므로 내부 상태를 우회하지 않는다(FW-004).

### 4.4 DB·경계

- InGame Data·SO는 바꾸지 않는다.
- View 표시 코드는 InGame을 읽지 않는다. DI9는 Core 준비 입력(Fixture 소스)이 읽는 것이다.
- Core 파일은 바꾸지 않는다. `ItemDefinitionAdapter`는 호출만 한다.

### 4.5 기능 변경의 독립성 (점검 기준 §4.6)

| 변경 시나리오 | 수정할 파일 | 이해할 코드 | 검증 |
|---|---|---|---|
| 지급 실패 문구·요약 형식 변경 | `DevStartingItemGrant` | 그 파일과 `ItemGrantResult` | 지급 시험 |
| 저장 형식 변경(JSON → 다른 형식) | 덮어쓰기 클래스(+ 시험 도우미의 키 이름이 같으면 그대로) | 덮어쓰기·설정 창 | 창 저장·재생 로그 |
| Bootstrap 수명·진입 순서 변경 | `GameBootstrap` | 지급은 한 줄 호출로만 보인다 | 기존 Boot 시험 |
| Fixture 내용 변경(유닛·필드) | `DevBattleInitializationSource` | 아이템 목록 두 줄은 따로 떨어져 있다 | 기존 개발 전투 |

지급 반복을 `GameBootstrap` 안 private 메서드로 두면 파일은 하나 줄지만, 494줄짜리 수명 주인을 열어야 지급 실패 처리를 고칠 수 있고 Unity 없이 시험할 수 없다. 그래서 개발 전용 함수로 뗐다. 남는 공통 경계는 "`StartNewRun` 뒤, `EnterChapter` 전"이라는 호출 위치 하나이며, 주인은 `GameBootstrap`이다.

## 5. 한계와 운영 시작 아이템

- Lobby·Stage 시작, 로비 출발의 새 Run에는 적용되지 않는다(DI1). 그 경로에서 시험하려면 Combat 직접 시작을 쓴다.
- **운영 시작 아이템은 Core 요청 사항이다.** 로드아웃(`PlayerLoadoutData`/SO)에 목록을 두고, `RunStartService`가 `NewRunPreparation`에 담아 `ProgressTransactions.PublishNewRun`이 로스터·재화와 함께 설치해야 한다. View가 운영 호출자 셋에서 `StartNewRun` 뒤에 `Grant`를 따로 부르면 다음 문제가 생긴다.
  - 규칙이 호출자 셋에 퍼진다.
  - 새 Run은 성공하고 지급만 실패하면 반쯤 설치된 상태가 남는다.
  - 기획 요청이 생기면 Core 전달 문서에 적는다(이번에는 적지 않는다).
- 아이템 화면·장착 입력이 없으므로, 이 작업만으로는 화면에서 장착을 확인할 수 없다. 이번 확인은 재고 로그와 시험까지다(§7).

## 6. 결정할 질문

**결정(2026-10-19, 사용자): 세 질문 모두 A.**

| 번호 | 질문 | 선택지 | 추천 |
|---|---|---|---|
| Q-I1 | Fixture 전투(개발 기본 정의)에서도 지급한 아이템을 장착·발동할 수 있게 할까? | **A** Fixture 정의에 DB 아이템 목록을 넣는다(DI9) / **B** 그대로 둔다. Fixture에서는 재고만 확인하고, 장착은 Database 정의에서만. 설정 창에 경고를 보인다 / **C** Fixture + 시작 아이템이면 재생 시작을 멈춘다 | **A.** Fixture가 기본 개발 정의라 B·C면 아이템 View 시험 때마다 Database로 바꿔야 한다. 값 사본 없이 두 줄이다 |
| Q-I2 | 설정 창에 키에 해당하는 아이템 이름을 보일까? | **A** 키·수량만. 없는 키는 재생 시작 때 Core 사유로 멈춘다 / **B** 키 옆에 이름을 보인다. 에디터에서 아이템 SO를 찾는 도우미가 Systems 안에 새로 필요하다(SO 형식이 internal이라 창이 직접 못 읽음) | **A.** 지금 아이템은 4개이고 키는 SO 인스펙터에 보인다. 카탈로그를 읽는 에디터 경로를 하나 더 만들지 않는다 |
| Q-I3 | Combat으로 시작하는 Boot 시험이 이 PC의 시작 아이템을 받지 않게 할까? | **A** 공용 도우미가 비우고 되돌린다(DI10) / **B** 시험은 그대로 둔다. 지금 시험은 재고를 보지 않아 결과가 같지만, 잘못된 값이 저장돼 있으면 이 PC에서만 Boot 시험이 시작에서 멈춘다 | **A.** 기능 추가가 다른 시험의 결과를 PC 설정에 묶지 않게 한다. 공용 도우미 몇 줄이다 |

## 7. 확인 방법

- **MSBuild:** 오류·새 경고 0. 새 파일은 extra.targets로 더한다.
- **관리 실행:** 새 지급 시험 PASS, 기존 시험 상태 변화 0.
- **Unity Test Runner:** Boot 시험 묶음(`LobbyBootSmokeTests` 등). DI9를 넣으면 개발 전투를 쓰는 시험도 함께 돌린다.
- **Unity 재생(사용자):**
  1. Play From Boot 설정 → Combat 시작, Fixture, 시작 아이템 `1×2`, `4×1` → 콘솔에 지급 로그와 재고 `{1:2, 4:1}`
  2. 없는 키(예: 99) → 진입하지 않고 `UnknownItem` 사유 예외
  3. 목록 비움 → 지급 로그 없음, 기존과 같은 진입
  4. 덮어쓰기 끔 → 씬 값(기본 빈 목록)으로 시작

## 8. 다음 단계

Q-I1~Q-I3 결정 → Task 계획(한 Phase 예상: 형식·지급 함수·시험 → Bootstrap·덮어쓰기·창 → Fixture 목록·시험 도우미) → 구현 → 보고.
