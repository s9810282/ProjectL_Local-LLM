# ProjectL Framework / Systems 개발 가이드

가이드 버전 2.0 · 소스 확인 기준 2026-09-22.

기존 가이드의 FW 계약 ID와 확장 원칙을 유지하면서 폴더, 네임스페이스, 실제 Factory 등록 관계와 예제를 갱신했다. [구조 설명](framework-architecture.md)과 [폴더 요약](../../README.md)을 함께 참고한다. 이전 계획·설계 문서는 당시의 결정 기록이며 현재 조립 관계는 이 문서와 소스를 기준으로 확인한다.

DT 관련 갱신: 2026-10-06. Core / View SO, 엑셀 임포트, 테이블별 조회와 Factory 연결은 [DT 구조 가이드](data-table-core-view-guide.md)를 참고한다.

## 1. 적용 범위

Framework는 공용 기반, Systems는 게임 서비스와 조립을 담당한다. LifeCycle과 GlobalSystem은 RuntimeSystem 직속 형제다. UI·Render 기반 클래스는 각각 GlobalSystem 하위 Base에, LifeCycle 기반 클래스는 LifeCycleSystem/Base에 둔다.

이 문서는 현재 공개 계약과 내부 조립 규칙을 설명한다. 씬 설정 완료, 전체 게임 실행, 성능이나 스레드 안전성을 보증하는 문서는 아니다. 실제 코드 철자 `CoreCaculator`, `Loding`, `Scop`, `Metrial`을 유지한다.

## 2. 배치·네임스페이스·어셈블리

| 코드 | 위치 / 기준 |
|---|---|
| 계약·키 기반 타입·Store·Reader | Framework/SystemBase |
| Builder·공용 실행기 | Framework/Composition |
| DB 기반 타입·Registry | Framework/DataBase |
| Pool·Queue·EventSubscriptionScope | Framework/Utility |
| 실제 키·서비스 그룹 종류 | Systems/ServiceKeys |
| SO 타입·Query | Systems/DataBaseSystem/Data 및 Queries |
| 게임별 생성·등록 | Systems/Factory/Runtime 또는 Loading |
| 게임 실행 구간 | Systems/RuntimeSystem/LifeCycleSystem |
| 공용 게임 서비스 | Systems/RuntimeSystem/GlobalSystem |
| Core | Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator |
| SO 에셋 / 프리팹 / 로드 리소스 | Assets/ProjectL/DataBaseSO, Prefabs, Resources |
| URP 피처 / 에디터 임포터 | Script/Render, Script/Tool/Editor |

네임스페이스는 `ProjectL.`에 Script 아래 상대 폴더 경로를 붙인다. Core의 네임스페이스도 현재 긴 경로를 따르지만 `ProjectL.Core.Contracts/Domain/Application` 어셈블리 이름은 유지한다. Core의 세 asmdef는 Systems 어셈블리에 합쳐지지 않는다.

Framework는 Systems·Core·FMOD를 참조하지 않는다. ProjectL.Systems는 Framework, Core.Contracts, TMP, Unity UI, FMOD에 의존한다. 게임별 구현을 편의상 Framework에 넣어 역참조를 만들지 않는다.

## 3. 조립과 실행

```text
Factory → ExportServices → SystemBuilderHelper → BuildSystemReader
    → SystemGroup → 상위 Builder에 Export → FrameworkRuntime

루트 등록: Buffer → Global → InGame → LifeCycle
루트 종료: LifeCycle → InGame → Global (Buffer는 Shutdown 계약 없음)
```

[FrameworkRuntimeFactory](../Factory/Runtime/RunTimeSystemFactory.cs)는 internal이며 ProjectL.Systems 내부 조립 진입점이다. 파일명과 클래스 이름은 다르다. FrameworkRuntime 생성자는 public이고 Reader를 받아 실행을 담당한다. 기본 Factory를 외부 어셈블리에서 바로 호출할 수 있다고 가정하지 않는다.

FrameworkRuntime은 일반 C# 객체다. Create만으로 실행되지 않으며 현재 Assets 소스에서 Unity Update를 이 Runtime의 Tick에 연결하는 게임 부트스트랩 호출자는 확인되지 않는다. 연동 담당 코드가 생성·데이터·씬 바인딩·프레임 갱신·종료를 연결해야 한다.

| 단계 | 호출과 전제조건 |
|---|---|
| BindScene | 씬의 UI 등을 연결. 반복 호출 자체는 제한하지 않음 |
| InitData | non-null Query를 전달. 성공 완료 후 중복 호출 거부 |
| PostBindService | 각 대상에 루트 서비스 Reader 전달. 성공 완료 후 중복 호출 거부 |
| Initialize | 필요한 InitData·PostBindService 완료 후 한 번 호출 |
| Tick | Initialize 이후. 음수 deltaTime 거부 |
| Shutdown | 루트 모듈 역순 종료. 완료 후 재호출은 무시, 서비스 재조회·재사용은 거부 |

InitData와 PostBindService 사이의 상대 순서, BindScene의 위치까지 Runtime이 고정하는 것은 아니다. 각 기능의 준비 조건에 맞춰 호출한다. 도중에 예외가 발생하면 이미 실행한 모듈을 자동 롤백하지 않는다. 자식 단계 전달과 종료 순서는 개별 그룹 구현도 확인한다.

## 4. 상태 소유와 데이터 흐름

| 담당 | 소유 / 접근 계약 |
|---|---|
| Builder / Store | 등록 목록을 구성한 뒤 읽기 전용 보관. 서비스 객체까지 불변으로 만들지 않음 |
| DataBaseFactory / Registry | Catalog → Group → Table → Query. Registry는 구체 타입으로 정확히 조회 |
| BufferSystemGroup | Current/Stage/Artifact/Character/Currency 상태와 ReadSink/WriteSink |
| Input | Configuration에서 RouteTable을 완성하고 주입된 Layer Runtime을 통해 현재 Layer 사용 |
| UI | UIId 등록, 화면 표시 요청, 뒤로가기 이력 |
| Render | Effect/Mesh/Color/Light/Outline의 Scope·Handle·Pool 수명 |
| Job | 등록된 작업의 우선순위 큐와 현재 실행 작업 |
| LifeCycle | Lobby/Stage/Artifact/Combat 활성 구간과 로딩 콜백 |

DataBaseFactory.Create(catalog)는 IServiceModule Query를 반환한다. 이를 Runtime.InitData에 전달한다. SO 에셋은 DataBaseSO, 타입은 Systems/DataBaseSystem/Data에 있으므로 두 위치를 혼동하지 않는다. Query는 런타임 게임 상태 저장소가 아니다.

InputSnapshot은 입력 값, LightSnapshotData는 생성 요청 DTO, Build Snapshot은 조립 목록이다. Snapshot이라는 이름만으로 동일한 소유권·복사 보장을 부여하지 않는다. JobQueue의 GetSortedSnapshot은 새 리스트이지만 참조형 Job 객체는 공유한다.

## 5. 서비스 접근

아래 `resolver`는 이미 전달받은 `ISystemServiceResolver`다. 필요한 using은 `ProjectL.Framework.Composition`, `ProjectL.Systems.ServiceKeys`다.

```csharp
var global = resolver.GetService(SystemGroupsService.Global);
var inGame = resolver.GetService(SystemGroupsService.InGame);
var lifeCycle = resolver.GetService(SystemGroupsService.LifeCycle);

var input = global.InputServiceCommand;
var ui = global.UIServiceCommand;
var render = global.RenderServiceCommand;
var utility = global.UtilityServiceCommand;
var loadingRegister = global.LodingServiceCommand.LodingRegisterService;
var loading = global.LodingServiceCommand.LodingService;
var jobs = global.JobServiceCommand;
```

| 기능 | API |
|---|---|
| 입력 | input.InputSystemServices.Configure/Submit, input.InputLayerServices.ChangeLayer |
| UI | ui.OpenUI / CloseUI / BackCloseUI / HideAllUI |
| 렌더 | render.EffectSystemServices / MeshSystemServices / ColorSystemServices / LightSystemServices / OutlineSystemServices |
| 유틸리티 | utility.delayService, utility.eventSystemService |
| Job | jobs.JobRegister.Register, jobs.JobRunner.Register/RegisterRange/IsIdle/Clear |
| LifeCycle | lifeCycle.LifeCycleManager.Change/Exit |
| InGame | inGame.CoreGroup, inGame.ViewGroup |

Global은 LifeCycle 접근자를 제공하지 않는다. Render·Job·UI 등의 키가 public이어도 루트에 등록됐다는 뜻은 아니다. 루트에서 그 키를 직접 조회하지 않고 Global Context를 거친다. AudioSystem은 Global Factory에 서비스·모듈로 등록되며 global.AudioServiceCommand로 접근한다. Audio 키는 Global 내부 Reader에서만 조회한다. 상세 사용법은 [Audio 연결](audio-integration.md)을 참고한다.

SystemServiceStore는 현재 Key.Name을 비교하지 않고 `is TService`로 첫 일치 객체를 찾는다. 같은 서비스 타입을 키 이름으로 구별하는 계약은 없다. 소비자에게 필요한 서비스만 전달하는 방식도 정상이다.


## 6. Framework Contract

각 규칙은 **적용 범위 안에서만** 판정한다. MUST는 명시적 API 검증, 접근 경계, 실제 소유·호출 관계가 뒷받침하는 계약이다. SHOULD는 권장 사용 방식이며 위반만으로 FAIL이 되지 않는다. MAY는 코드가 제공하는 선택지다. §14에 기록한 미완성 동작을 새로운 MUST로 승격시키지 않는다.

### MUST

#### FW-001 — 서비스는 제공 인터페이스로 등록한다

**적용:** SystemBuilderHelper를 사용하는 조립 코드.

등록 타입은 `IServiceModule`을 상속한 구체 서비스 **인터페이스**여야 한다. `IServiceModule` 자체, 클래스 타입, null 서비스, 인터페이스를 구현하지 않는 객체를 등록하지 않는다. 같은 Builder에 같은 서비스 인터페이스를 두 번 등록하지 않는다.

- **근거:** [SystemBuilderHelper.cs](../../Framework/Composition/SystemBuilderHelper.cs) → `AddService`, `ValidateService`; [BuildDataSnapShot.cs](../../Framework/SystemBase/BuildDataSnapShot.cs) → `ServiceSnapshot` 생성자.
- **위반 예:** `AddService<ConcreteService>(instance)`, `AddService<IServiceModule>(instance)`, 같은 인터페이스 재등록.
- **정상 예:** `sink.AddService<IInputSystemService>(this)`.
- **판정 주의:** 서로 다른 인터페이스로 같은 객체를 등록하는 것은 일괄 금지하지 않는다.

#### FW-002 — 생명주기 참여는 모듈 등록으로 연결한다

**적용:** Framework 모듈 목록을 통해 실행되어야 하는 시스템.

서비스 등록만으로 생명주기 참여를 기대하지 않는다. 해당 객체를 `AddModule`에도 등록해야 한다. 모듈은 `ISystemModule` 외에 이를 상속하는 구체 모듈 인터페이스를 구현해야 하며, `IServiceModule` 계열 인터페이스만으로는 모듈 요건을 충족하지 않는다. 같은 Builder에 동일 **구체 런타임 타입** 모듈을 중복 등록하지 않는다.

- **근거:** [SystemBuilderHelper.cs](../../Framework/Composition/SystemBuilderHelper.cs) → `AddModule`, `ValidateSystemModule`, `IsValidSystemModuleInterface`; [SystemModuleStore.cs](../../Framework/SystemBase/SystemModuleStore.cs) → 생성자와 `Collect<TModule>`.
- **위반 예:** Tick이 필요한 객체를 AddService만 한 뒤 TickModules에 있다고 가정.
- **정상 예:** `DelaySystem.ExportServices`의 서비스 등록 + 모듈 등록.
- **판정 주의:** Query처럼 서비스만 제공하는 객체는 AddModule이 필수가 아니다. `IExportSystemServices`도 검사상 유효한 모듈 인터페이스다.

#### FW-003 — Builder는 한 번 Build하고 닫는다

**적용:** 한 SystemBuilderHelper 인스턴스의 사용 수명.

`BuildSystemReader()` 이후 같은 Builder로 AddService, AddModule, BuildSystemReader를 다시 호출하지 않는다. Build가 돌려준 읽기 목록을 사후 등록·교체용 상태로 사용하지 않는다.

- **근거:** [SystemBuilderHelper.cs](../../Framework/Composition/SystemBuilderHelper.cs) → `BuildSystemReader`, `EnsureOpen`; [SystemServiceStore.cs](../../Framework/SystemBase/SystemServiceStore.cs), [SystemModuleStore.cs](../../Framework/SystemBase/SystemModuleStore.cs) → 복사와 읽기 전용 보관.
- **위반 예:** 실행 도중 닫힌 Builder에 새 서비스를 끼워 넣음.
- **정상 예:** 필요한 등록 완료 → Build → Reader를 소비자에게 전달.
- **판정 주의:** 등록 목록만 고정된다. 조회한 서비스의 공개 명령으로 상태를 변경하는 것은 정상이다. 다른 그룹을 위해 별도 Builder를 만드는 것도 정상이다.

#### FW-004 — Framework/Systems 소유 내부 구현의 상태 변경 경계를 지킨다

**적용:** 이미 Framework가 관리하는 서비스 Store, UI 이력, 입력 RouteTable, 렌더링 노드·Pool을 이용하는 소비자.

이들의 internal 타입·private 컬렉션을 노출하거나 접근 제한을 우회해 직접 수정하지 않는다. 서비스 조회는 공개 Reader, 입력은 공개 Input 계약, 화면 전환은 UI 서비스, 렌더링 자원은 발급된 Scope/Handle 경로를 사용한다.

- **근거:** [SystemServiceStore.cs](../../Framework/SystemBase/SystemServiceStore.cs)의 internal Store; [InputRouteTable.cs](../RuntimeSystem/GlobalSystem/InputSystem/Core/InputRouteTable.cs)의 internal RouteTable; [UISystem.cs](../RuntimeSystem/GlobalSystem/UISystem/UISystem.cs)의 private `_uis`·`_uiHistory`; [EffectSystem.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/EffectSystem/EffectSystem.cs), [MeshSystem.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/MeshSystem/MeshSystem.cs), [LightSystem.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/LightSystem/LightSystem.cs)의 외부 서비스와 내부 Handle 서비스 분리.
- **위반 예:** 내부 MeshNode를 노출해 서비스가 모르는 경로로 Pool에 반환.
- **정상 예:** `IRenderServiceContext.CoreGroup.MeshSystemServices → IMeshRenderHandle.Dispose()`.
- **판정 주의:** 이미 전달받은 공개 서비스·Scope의 직접 호출은 정상이다. 이 규칙은 프로젝트의 모든 데이터와 Unity 객체를 Framework 소유로 선언하지 않는다.

#### FW-005 — 입력 바인딩은 Configuration 생성 중 완성한다

**적용:** InputController / IInputController 확장.

`InputConfiguration.Create(controllers)`에 하나 이상의 non-null Controller를 제공한다. 각 Controller는 비어 있지 않은 ActionMap과 하나 이상의 유효한 바인딩을 등록해야 한다. 바인딩은 Configure 중에만 추가하고, Configuration 전체에서 같은 `(ActionMap, ActionName)`을 중복 등록하지 않는다.

- **근거:** [InputConfiguration.cs](../RuntimeSystem/GlobalSystem/InputSystem/Configuration/InputConfiguration.cs) → `Create`와 `finally input.Seal()`; [InputController.cs](../RuntimeSystem/GlobalSystem/InputSystem/View/InputController.cs) → `InputBindings.Raw`, `EnsureOpen`; [InputRouteTable.cs](../RuntimeSystem/GlobalSystem/InputSystem/Core/InputRouteTable.cs) → `Add`.
- **위반 예:** Configure에서 저장해 둔 InputBindings에 나중에 Press 추가; 서로 다른 Controller가 같은 Route 등록.
- **정상 예:** `InputController.Configure`에서 Press/Button/Float/Vector2/Raw로 완성.
- **판정 주의:** ActionMap과 ActionName 비교는 Ordinal이며 대소문자를 구분한다.

#### FW-006 — 입력 서비스의 설정 수명을 지킨다

**적용:** IInputSystemService.Configure / Submit, IInputLayerService.ChangeLayer.

정상 사용 전 non-null Layer Runtime과 Configuration으로 Configure를 한 번 수행한다. 재설정 또는 Shutdown 후 재설정·ChangeLayer를 하지 않는다. ChangeLayer에는 유효한 InputLayer를 전달한다.

- **근거:** [InputSystem.cs](../RuntimeSystem/GlobalSystem/InputSystem/View/InputSystem.cs) → `Configure`, `EnsureCanConfigure`, `EnsureAvailable`, `ChangeLayer`, `Shutdown`.
- **위반 예:** 미설정 서비스에 Submit; 같은 서비스에 두 번째 Configure.
- **정상 예:** `InputConfiguration.Create → Configure → Submit / ChangeLayer`.
- **판정 주의:** Shutdown 후 늦게 도착한 Submit은 의도적으로 무시된다. 이를 수명 위반으로 FAIL 처리하지 않는다. CurrentLayer는 미설정·종료 상태에서 default를 반환한다.

#### FW-007 — 수집한 Framework 입력은 Submit 경로로 전달한다

**적용:** 이 InputSystem에 연결된 입력 수집·전달 코드.

입력은 `Submit(actionMap, actionName, phase, value, time)`으로 보내 Layer 검사와 Route의 ValueKind 검사를 거치게 한다. 수집기가 Controller Handler를 직접 실행하여 이 필터를 우회하지 않는다. 유효한 이름·Phase·Value를 사용하고 바인딩한 값 종류와 맞춘다.

- **근거:** [InputSystem.cs](../RuntimeSystem/GlobalSystem/InputSystem/View/InputSystem.cs) → `Submit`, `ValidateSubmission`; [InputRouteTable.cs](../RuntimeSystem/GlobalSystem/InputSystem/Core/InputRouteTable.cs) → `TryDispatch`; [InputSnapshot.cs](../RuntimeSystem/GlobalSystem/InputSystem/Data/InputSnapshot.cs) → `InputValueData.EnsureKind`.
- **위반 예:** 현재 Layer가 허용하지 않는 ActionMap인데 수집기가 Handler 직접 호출; Float Route에 ButtonValue 제출.
- **정상 예:** `Submit → CurrentLayer.Allows → InputSnapshot → TryDispatch → Handler`.
- **판정 주의:** 미허용 Layer 또는 미등록 Route는 전달되지 않는다. Handler가 호출하는 게임 명령을 다른 정상 게임 흐름에서 호출하는 것까지 금지하지 않는다.

#### FW-008 — UI 등록과 사용 전제조건을 지킨다

**적용:** UIBase 확장과 IUISystemService 소비자.

씬 검색으로 연결할 UI는 UIBase를 상속한다. 기본 `ExportUIService(IUIRegistry)`는 `registry.AddUI(ID, this, ParentID)`로 등록한다. 재정의하면 기본 구현을 호출하거나 직접 등록해야 한다. ParentID의 기본값 None은 루트이며, 자식은 등록된 부모 ID를 사용한다. 자기 자신을 부모로 지정하거나 순환 관계를 만들 수 없다. 같은 UISystem의 UIId는 중복될 수 없으며 None을 사용하지 않는다. OpenUI/OpenChildUI/CloseChildUI/CloseUI/BackCloseUI/HideAllUI는 PostBindService와 Initialize 완료 후 호출한다.

- **근거:** [UIBase.cs](../RuntimeSystem/GlobalSystem/UISystem/Base/UIBase.cs) → 확장 메서드; [UISystem.cs](../RuntimeSystem/GlobalSystem/UISystem/UISystem.cs) → `BindScene`, `AddUI`, `EnsureInitialized`.
- **위반 예:** ExportUIService를 재정의하고 등록 생략; 중복 UIId; Initialize 전 OpenUI.
- **정상 예:** `ExportUIService → registry.AddUI(ID, this)`; 준비 완료 뒤 서비스 명령.
- **판정 주의:** UIBase.ID와 AddUI에 넘긴 ID의 동일성은 자동 검사하지 않는다. 동일하게 사용하는 것은 FW-102 권장사항이다.

#### FW-009 — UISystem의 화면 전환 이력을 유지한다

`OpenGlobalUI()`도 PostBindService와 Initialize 완료 후 호출한다. 최초 씬 바인딩에서 수집된 `GlobalUIBase` 루트만 `OpenUI(id, true)`로 함께 열며, 일반 화면·자식 UI를 새로 열거나 기존 일반 화면을 닫지 않는다. 목록에 있는 유지 UI를 `OpenUI(id)`로 개별 호출해도 유지 방식으로 열린다. `GlobalUIBase.ParentID`는 `None`으로 고정되며 자식으로 등록할 수 없다. 유지 루트는 뒤로가기 대상에서 제외되지만 `CloseUI(id)`와 `HideAllUI()`로 닫을 수 있다.

**적용:** UISystem이 등록·관리하는 화면 간 전환.

외부의 열기·닫기·뒤로가기 요청은 IUISystemService를 거친다. UIBase.Open/Hide 직접 호출만으로 화면을 전환한 뒤 시스템 이력도 변경됐다고 간주하지 않는다.

- **근거:** [UISystem.cs](../RuntimeSystem/GlobalSystem/UISystem/UISystem.cs) → `OpenUI`, `CloseUI`, `BackCloseUI`, `RemoveFromHistory`, `OpenCurrentHistoryUI`; [UIBase.cs](../RuntimeSystem/GlobalSystem/UISystem/Base/UIBase.cs) → 표시 콜백.
- **위반 예:** 외부 버튼이 UIBase.Hide만 호출하면서 뒤로가기 Stack에서도 제거됐다고 가정.
- **정상 예:** `CloseUI(id) → 자손부터 Hide → 닫힌 UI 이력 제거`. 이전 화면을 다시 열지 않는다.
- **판정 주의:** UIBase.Open/Hide **구현 내부**에서 Unity 표시를 바꾸는 것은 정상이다. 등록되지 않은 독립 UI의 정책은 이 규칙 밖이다.

자식 ID에 대한 `OpenUI(childId)`는 `OpenChildUI(parentId, childId)`로 연결된다. 부모가 열려 있어야 한다. 일반 루트와 자식은 공통 History에 현재 열린 UI로 기록한다. 새 일반 UI를 열면 부모가 달라도 기존 일반 화면 경로를 닫고 이력에서 제거한다. 다만 새 자식의 열린 조상은 유지한다. `CloseChildUI(parentId)`는 열린 자손을 닫고 부모는 유지하며, `CloseUI(parentId)`는 자손부터 부모까지 닫는다. `BackCloseUI()`는 공통 History 최상단을 닫는다. 닫힌 UI는 History에서 제거되며 이전 화면을 복원하지 않는다.

`OpenUI(rootId, true)`로 연 유지 루트는 History에서 제외하고 열린 루트 상태로 별도 추적한다. 유지 루트의 자식도 일반 UI로 History에 기록하므로 BackCloseUI로 닫을 수 있다. 예를 들어 유지 루트 A/C 아래 자식 B/D가 있으면 D를 열 때 B를 닫고 제거하며, D를 닫아도 B는 복원하지 않는다. A/C는 계속 열린다. HideAllUI는 유지 UI도 모두 숨긴다. bool 오버로드의 false 처리는 현재 경고 후 유지 방식으로 계속 실행하므로 일반 루트는 OpenUI(id)를 사용한다.

열린 자식 관계를 별도 Dictionary로 중복 보관하지 않는다. `_parents`는 등록 관계, `_uiHistory`는 열린 일반 UI의 한 경로, `_openRoots`는 유지 UI를 포함한 열린 루트를 관리한다. 새 UI의 부모부터 올라가 History에 있는 가장 가까운 조상을 찾고, 그 위만 Pop하여 닫는다. 조상이 없으면 History를 모두 비운다. 자손 닫기는 History 최상단에서 부모 관계를 따라 요청한 부모가 있는지 확인한 뒤 그 부모 위만 Pop한다. 해당 부모가 조상이 아니면 현재 경로를 건드리지 않는다. 유지 루트는 History에 없으므로 이 과정에서 닫히지 않는다.

#### FW-010 — Mesh는 등록한 MeshFilter에 구체 DTO로 갱신한다

**적용:** IMeshRenderHandle.Update와 Framework 내부 Mesh 확장.

Initialize에서 빌더가 등록된 뒤, View는 MeshFilter를 Register하고 해당 **구체 제네릭 타입**으로 Update를 호출한다. IMeshUpdateData 구현만으로 자동 등록된다고 가정하지 않는다. 같은 타입의 생성 함수를 중복 등록하지 않는다.

- **근거:** [MeshSystem.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/MeshSystem/MeshSystem.cs) → `Initialize`, `Update<TData>`의 `typeof(TData)`, `RegisterCreateFunction<TData>`.
- **위반 예:** `IMeshUpdateData data = new CubeCreateData(...); handle.Update(data, revision);`는 인터페이스 타입으로 조회하여 미등록 예외.
- **정상 예:** CubeCreateData 변수를 그대로 `handle.Update(data, revision)`에 전달.
- **판정 주의:** 현재 등록 타입은 Cube, Circle, Cylinder, SingleLine, MultiLine, GridCube의 CreateData 여섯 개다. 사용자 DTO 추가는 내부 빌더 등록 변경이 필요하다.

#### FW-011 — 대여한 렌더링 자원은 해당 Scope/Handle 경로로 반환한다

**적용:** Framework가 발급한 EffectScope, IMeshRenderHandle, LightScope/LightHandle 사용.

이 자원을 외부 Pool에 반환하거나 내부 노드의 Unity 객체를 직접 파괴하지 않는다. Mesh Handle 소유자는 사용 종료 시 Dispose를 호출한다. Effect를 명시 종료할 때는 StopAll, Light를 반환할 때는 ReturnAll/Dispose 또는 Handle.Dispose 경로를 사용한다.

- **근거:** [MeshSystem.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/MeshSystem/MeshSystem.cs) → `Register`; [MeshRenderHandle.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/MeshSystem/MeshRenderHandle.cs) → `Dispose`; [EffectScope.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/EffectSystem/EffectScope.cs) → `StopAll`, `Stop`; [LightScope.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/LightSystem/LightScope.cs) → 반환·해제 메서드; 각 내부 Pool의 반환 처리.
- **위반 예:** 등록한 MeshFilter의 runtime Mesh를 직접 Destroy; 대여 중인 Light GameObject를 직접 Destroy.
- **정상 예:** `IMeshRenderHandle.Dispose → MeshNodePool.Return`.
- **판정 주의:** Effect는 자연 종료 콜백으로도 반환된다. Light·Color·Mesh의 해제 후 동작을 동일하게 취급하지 않는다. Color 해제와 Light 개별 Handle 재사용 문제는 DR-004/005 대상이다.

#### FW-012 — DB 등록과 Effect·Light 데이터 요구조건을 충족한다

**적용:** 현재 Catalog/Render Table에 추가하는 설정과 그 설정으로 자원을 요청하는 코드.

Registry의 같은 계층에 같은 구체 타입을 중복 등록하지 않는다. EffectType/LightPresetType은 각 테이블에서 중복되지 않아야 한다. Effect는 non-null Prefab, MaxCount > 0, 0 ≤ InitializeCount ≤ MaxCount가 필요하다. Light는 Empty가 아닌 PresetType, non-null LightPrefab, InitialPoolSize ≥ 0이 필요하다. Effect·Light 사용 전 필요한 Query를 InitData로 전달하고 등록된 타입을 요청한다.

- **근거:** [DataBaseRegistry.cs](../../Framework/DataBase/DataBaseRegistry.cs) → `AddData`; [EffectTable.cs](../DataBaseSystem/Queries/RenderDataBase/RenderTable/EffectTable.cs), [LightTable.cs](../DataBaseSystem/Queries/RenderDataBase/RenderTable/LightTable.cs) → `ValidateData`; [EffectSystem.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/EffectSystem/EffectSystem.cs) → `InitData`, `GetElementNode`, `ValidateEffectData`; [LightSystem.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/LightSystem/LightSystem.cs) → `InitData`, `GetLightHandle`.
- **위반 예:** 중복 Preset; Query 없이 자원 요청; 등록되지 않은 타입을 존재하는 것으로 간주.
- **정상 예:** SO 설정 → Table 검증 → Query 주입 → Scope 자원 요청.
- **판정 주의:** EffectType.Empty는 Table이 아니라 실제 Pool 생성 전 EffectSystem에서 거부한다. 현재 EffectPool은 프리팹 루트의 ParticleSystem과 ParticleStopNotifier를 사용한다. 자식만 있는 Prefab을 안전한 입력으로 보증하지 않는다(DR-006). MaxCount 검증이 런타임 개수 제한을 구현했다는 뜻은 아니다.

#### FW-013 — 이벤트 발행 계약의 타입을 지킨다

**적용:** IEventSystemService 및 StageEventBus 발행 인터페이스.

Subscribe에는 IStageEvent, PublishDomain에는 IDomainEvent, PublishPresentation에는 IPresentationEvent를 사용한다. 같은 이벤트 스트림의 발행자와 구독자는 같은 Bus를 사용하는 서비스에 연결해야 한다.

- **근거:** [StageEventSubscriber.cs](../RuntimeSystem/GlobalSystem/UtilitySystem/EventBuse/Core/StageEventSubscriber.cs) → 제네릭 제약; [EventSystem.cs](../RuntimeSystem/GlobalSystem/UtilitySystem/EventBuse/View/EventSystem.cs) → 내부 `_eventBus`; [StageEventBus.cs](../RuntimeSystem/GlobalSystem/UtilitySystem/EventBuse/Core/StageEventBus.cs) → `Subscribe`, `PublishInternal`의 인스턴스별 `Dictionary<Type, List<Delegate>>`.
- **위반 예:** 서비스로 구독한 이벤트를 별도 new StageEventBus에 발행하면서 기존 구독자에게 전달된다고 가정.
- **정상 예:** 같은 IEventSystemService에서 SubscribeStage와 PublishDomain 사용.
- **판정 주의:** 이벤트는 제네릭 TEvent 타입으로 라우팅되며 상속·마커 전체 구독으로 확장되지 않는다. 독립 스트림을 위한 별도 Bus 사용을 일괄 금지하지 않는다.

#### FW-014 — 공개 Pool의 명시적 입력·콜백 계약을 지킨다

**적용:** CustomPool과 그 확장 사용.

PoolContext의 CreateFunc는 non-null, initialSize는 0 이상이어야 한다. Prewarm에서 생성한 객체와 Return에 전달하는 객체는 non-null이어야 한다. ReturnRule을 구현할 때 OnBeforeReturn이 **반환 허용 여부 검사보다 먼저** 실행된다는 점을 보존한다.

- **근거:** [CustomPool.cs](../../Framework/Utility/CustomPool.cs) → `Validate`, `Prewarm`, `Return`.
- **위반 예:** ReturnRule이 false이므로 OnBeforeReturn도 호출되지 않을 것이라고 전제한 상태 처리.
- **정상 예:** `OnBeforeReturn → CanReturn → 허용 시 Enqueue → OnReturn`.
- **판정 주의:** 중복 반환·소유 Pool·최대 개수는 이 클래스가 검사하지 않는다. 런타임 Get의 CreateFunc 반환값에도 Prewarm과 같은 null 검사가 있다고 보증하지 않는다.

#### FW-015 — Queue 정렬 기준 변경을 Queue에 반영한다

**적용:** JobQueue 내부 참조형 Job의 정렬 값을 변경하거나 비교기를 교체하는 코드.

정렬에 사용되는 Job 값이 바뀌면 다음 정렬 결과를 사용하기 전에 해당 Job의 Refresh 또는 전체 Rebuild로 재정렬한다. 비교기는 SetSortRule로 바꾸며, 스냅샷 리스트를 편집해 원본 큐가 수정됐다고 간주하지 않는다.

- **근거:** [JobQueue.cs](../../Framework/Utility/JobQueue.cs) → `Refresh`, `Rebuild`, `SetSortRule`의 주석과 구현, `GetSortedSnapshot`.
- **위반 예:** Priority만 바꾸고 Peek가 즉시 새 최우선 Job을 반환할 것으로 기대.
- **정상 예:** `job.Priority 변경 → queue.Refresh(job) → Peek / Dequeue`.
- **판정 주의:** 같은 비교 결과일 때 Enqueue 순서를 사용한다. 스냅샷의 참조형 Job 자체 변경은 원본 Job에도 영향을 줄 수 있다.

#### FW-016 — FrameworkRuntime의 실행 수명과 선행 단계를 지킨다

**적용:** ISystemRuntime을 구동하는 연동 코드와 ISystemServiceResolver 소비자.

InitData에는 non-null IServiceModule을 전달한다. InitData 대상이 있으면 해당 단계를, PostBindService 대상이 있으면 해당 단계를 완료한 뒤 Initialize를 호출한다. Tick은 Initialize 이후에 수행하고 음수 deltaTime을 전달하지 않는다. InitData·PostBindService·Initialize의 성공 완료 후 동일 메서드를 재호출하지 않는다. Shutdown 완료 후 생명주기·서비스 조회를 재사용하지 않는다.

- **근거:** [FrameworkRuntime.cs](../../Framework/Composition/FrameworkRuntime.cs) → FrameworkRuntime.InitData, PostBindService, Initialize, Tick, GetService, ThrowIfShutdown.
- **위반 예:** InitDataModules가 있는데 InitData 없이 Initialize; Initialize 전에 Tick; Shutdown 후 GetService.
- **정상 예:** 필요한 데이터·서비스 바인딩 완료 → Initialize → Tick → Shutdown.
- **판정 주의:** Shutdown 자체의 재호출은 완료 후 무시된다. BindScene은 반복 호출을 제한하지 않는다. GetService는 종료 전에는 초기화 완료를 요구하지 않는다. BindScene·InitData·PostBindService 사이의 완전한 고정 순서를 추가하지 않는다. 실패 도중 복구·롤백 보장은 DR-001 대상이다.

### SHOULD

#### FW-101 — 소비자에게 필요한 서비스 인터페이스만 전달한다

Context로 조회한 뒤 필요한 인터페이스를 의존 객체에 전달하는 방식을 권장한다. 모든 소비자가 조립기·전체 모듈 목록을 알아야 할 필요는 없다.

- **근거:** [RenderSystemGroup.cs](../RuntimeSystem/GlobalSystem/RenderSystem/RenderSystemGroup.cs), [InputSystemGroup.cs](../RuntimeSystem/GlobalSystem/InputSystem/InputSystemGroup.cs), [UtilitySystemGroup.cs](../RuntimeSystem/GlobalSystem/UtilitySystem/UtilitySystemGroup.cs)의 인터페이스 프로퍼티 노출.
- **판정:** 넓은 Reader 의존은 WARNING 검토 대상이며 그 자체가 FAIL은 아니다.

#### FW-102 — UI와 입력의 식별자를 일관되게 관리한다

UIBase.ExportUIService에서 `registry.AddUI(ID, this)`를 사용하고, 같은 역할의 InputLayer는 이름·ActionMap 구성을 일관되게 유지하는 것을 권장한다.

- **근거:** [UIBase.cs](../RuntimeSystem/GlobalSystem/UISystem/Base/UIBase.cs), [UISystem.cs](../RuntimeSystem/GlobalSystem/UISystem/UISystem.cs) → ID와 Registry가 별개; [InputLayer.cs](../RuntimeSystem/GlobalSystem/InputSystem/Data/InputLayer.cs) → Equals/GetHashCode는 Name만 사용하고 Allows는 ActionMaps 사용.
- **판정:** 동일 이름·다른 ActionMaps가 필요한 정책은 DR-009. 이를 자동 FAIL로 만들지 않는다.

#### FW-103 — 반환한 자원 참조와 구독 수명을 명시적으로 관리한다

반환한 Handle 참조를 폐기하고 소유 Scope의 수명을 명확히 하며, 서비스로 이벤트를 중복 구독하지 않도록 등록 시점을 관리하는 것을 권장한다.

- **근거:** [LightScope.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/LightSystem/LightScope.cs), [LightPool.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/LightSystem/LightPool.cs)의 Handle 재사용; [EventSystem.cs](../RuntimeSystem/GlobalSystem/UtilitySystem/EventBuse/View/EventSystem.cs)의 구독 보관과 Shutdown 일괄 Clear.
- **판정:** 단기 구독 해제가 필요한 경우 현재 서비스 계약이 없으므로 새로운 정책은 DR-008로 검토한다.

#### FW-104 — 수치·콜백의 추가 요구사항은 호출 경계에서 명시한다

Mesh의 Radius/Segments/좌표, Delay의 시간·재진입, 이벤트 Handler 예외 등 해당 시스템이 보증하지 않는 요구사항은 별도 검증·호출 정책으로 설명하는 것을 권장한다.

- **근거:** [MeshHightLevelBuilder.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/MeshSystem/MeshHightLevelBuilder.cs), [DelaySystem.cs](../RuntimeSystem/GlobalSystem/UtilitySystem/DelaySystem/View/DelaySystem.cs), [StageEventBus.cs](../RuntimeSystem/GlobalSystem/UtilitySystem/EventBuse/Core/StageEventBus.cs)의 계산·동기 콜백 구현.
- **판정:** 기존 검증의 불필요한 복제와 시스템이 제공하지 않는 입력 검증을 구분한다. 후자를 Framework 우회라고 판정하지 않는다.

### MAY

#### FW-201 — Controller와 Layer Runtime을 외부에서 구현할 수 있다

`InputController.Configure`를 재정의하거나 `IInputController`를 직접 구현할 수 있다. `IInputLayerRuntime`도 주입 계약으로 열려 있다.

- **근거:** [InputController.cs](../RuntimeSystem/GlobalSystem/InputSystem/View/InputController.cs), [InputConfiguration.cs](../RuntimeSystem/GlobalSystem/InputSystem/Configuration/InputConfiguration.cs) → 외부 Configure 호출; [InputSystem.cs](../RuntimeSystem/GlobalSystem/InputSystem/View/InputSystem.cs) → Runtime 주입.
- **범위:** 기본 입력 저장소가 폴더 내부에 따로 구현되어 있다는 뜻은 아니다.

#### FW-202 — UIBase를 확장해 씬 UI를 연결할 수 있다

ID, GetUIData, BindUIService, Open, Hide를 구현해 UISystem에 연결할 수 있다. ExportUIService는 기본 등록 구현을 제공하고, 자식 UI는 ParentID를 재정의한다.

- **근거:** [UIBase.cs](../RuntimeSystem/GlobalSystem/UISystem/Base/UIBase.cs), [UISystem.cs](../RuntimeSystem/GlobalSystem/UISystem/UISystem.cs) → `BindScene` 및 콜백 전달.
- **범위:** 화면 내부의 표시·애니메이션 구현은 확장 대상이다. 등록된 화면의 이력은 FW-009를 따른다.

#### FW-203 — 기존 Render DTO와 Scope 조작 API를 사용할 수 있다

등록된 Mesh DTO, EffectScopeRequest, LightSnapshotData를 구성하고, LightHandle의 SetLocalPosition/SetLocalRotation/SetColor/SetIntensity 또는 IColorHandle.SetOverrideData로 조작할 수 있다.

- **근거:** [MeshGeometryData.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/MeshSystem/MeshGeometryData.cs), [EffectScope.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/EffectSystem/EffectScope.cs), [LightScope.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/LightSystem/LightScope.cs), [ColorHandle.cs](../RuntimeSystem/GlobalSystem/RenderSystem/Core/ColorSystem/ColorHandle.cs).
- **범위:** LightSnapshotData는 요청 데이터이며, 공개 Handle의 조작은 내부 상태 우회가 아니다. Color 해제는 별도 확인 대상이다.

#### FW-204 — Pool과 Queue의 공개 확장 계약을 사용할 수 있다

CustomPool에 CreateFunc, IPoolLifecycle, IPoolReturnRule을 주입하거나, JobQueue에 IComparer/Comparison을 주입할 수 있다.

- **근거:** [CustomPool.cs](../../Framework/Utility/CustomPool.cs), [JobQueue.cs](../../Framework/Utility/JobQueue.cs) → 생성자·인터페이스·SetSortRule.
- **범위:** 독립 기능이 자기 Pool/Queue를 소유하는 것은 허용된 API 사용이다. Framework가 소유한 렌더링 노드·Delay 데이터를 별도 관리하는 것과 구별한다.

#### FW-205 — 새로운 모듈·서비스를 Builder로 조립할 수 있다

공개 모듈·서비스 계약과 IExportSystemServices를 구현하고, Builder의 검증 조건을 지켜 Reader를 구성할 수 있다.

- **근거:** [IRunTimeSystemDefinitions.cs](../../Framework/SystemBase/IRunTimeSystemDefinitions.cs), [SystemBuilderHelper.cs](../../Framework/Composition/SystemBuilderHelper.cs), [SystemGroup.cs](../../Framework/SystemBase/SystemGroup.cs).
- **범위:** 기존 루트에 자동 등록되는 Plugin 탐색은 없다. 기존 그룹에 새 Context 키를 추가하거나 Factory 연결을 바꾸는 것은 Framework 변경이며 §7의 변경 시 주의 영역이다.



## 7. 확장 지점

- 새로운 게임 서비스는 Systems에 두고 IServiceModule 파생 인터페이스와 필요한 모듈 계약을 구현한다. 생성은 Factory, 등록은 ExportServices, 조회는 Context를 사용한다.
- UIBase는 GlobalSystem/UISystem/Base에 있다. UI 화면은 Screens 아래에서 ID, GetUIData, BindUIService, Open, Hide를 구현한다. 기본 ExportUIService는 ParentID를 포함해 등록하며, 재정의 시 같은 등록 계약을 유지한다.
- LifeCycleBase는 LifeCycleSystem/Base에 있다. 새 LifeCycle은 자신의 Key와 SceneIndex를 제공하며 LifeCycleSystemFactory에 등록한다. GlobalFactory에 추가하지 않는다.
- InputController 또는 IInputController와 IInputLayerRuntime을 외부에서 구현할 수 있다. 기본 장치 수집·Layer 저장까지 자동 제공되는 것은 아니다.
- Render의 신규 DTO·Scope는 해당 하위 시스템과 Factory/Context에 연결한다. URP RendererFeature는 Script/Render 영역에서 별도로 관리한다.

## 8. 피해야 할 연결

| 잘못된 전제 | 현재 방식 |
|---|---|
| 루트에서 Render·UI·Job을 직접 조회 | Global Context를 먼저 조회 |
| Global이 LifeCycle을 소유 | 루트 LifeCycle 키를 사용 |
| 인터페이스 선언만 하면 서비스가 등록됨 | Factory와 ExportServices 연결 확인 |
| AddService만 하면 Tick 대상이 됨 | AddModule도 등록하고 부모 그룹의 전달 구현 확인 |
| Pool·구체 구현을 캐스트로 꺼내 수동 조작 | 공개 서비스·Scope·Handle 계약 사용 |
| 폴더 안에 있으므로 동일 어셈블리 | 가장 가까운 asmdef 경계 확인 |
| Runtime 생성·컴파일 통과가 게임 실행 완료 | Unity 구동 코드, 데이터와 씬 설정 별도 확인 |

## 9. 새로운 시스템 연결 절차

1. 책임에 맞는 Systems 폴더를 정하고 폴더와 같은 네임스페이스를 사용한다.
2. IServiceModule 파생 인터페이스와 필요한 생명주기 계약을 정의한다.
3. ExportServices에서 서비스와 모듈을 각각 등록한다.
4. 해당 Factory에서 생성하고 그룹 Context에 필요한 서비스만 노출한다.
5. ServiceKeys에 조회 키를 두고 PostBindService에서 의존 서비스를 연결한다.
6. 한 Builder는 한 번만 Build한다. 이미 실행 중인 런타임에 Factory를 다시 호출해 같은 서비스의 별도 상태를 만들지 않는다.
7. 초기화·Tick·역방향 종료와 잘못된 등록/조회 경로를 검증한다.

완전한 최소 예제는 [제작·등록·조회 안내](../../../../../docs/superpowers/framework/03_System_Creation_Registration_Query.md)에 있다. 예제 코드는 신규 기능의 확장 형태이며 현재 프로젝트에 그 기능이 존재한다는 뜻이 아니다.

## 10. 현재 사용 흐름

### 렌더·UI·Job 조회

```csharp
var global = resolver.GetService(SystemGroupsService.Global);
var meshService = global.RenderServiceCommand.CoreGroup.MeshSystemServices;
using var meshHandle = meshService.Register(meshFilter);
meshHandle.Update(data, revision);

global.UIServiceCommand.OpenUI(UIId.Stage);
global.JobServiceCommand.JobRegister.Register(JobRootType.Stage, JobType.Start);
```

UIId는 `ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem`, JobRootType·JobType은 JobRegister 네임스페이스의 타입이다. 위 호출은 필요한 초기화와 씬 UI 등록 완료를 전제로 한다. Color는 IColorHandle를 통해 속성을 조작하며, 사용 종료 시 Dispose로 Renderer 등록과 풀 대여를 해제한다.

### LifeCycle과 로딩

```csharp
var lifeCycle = resolver.GetService(SystemGroupsService.LifeCycle);
lifeCycle.LifeCycleManager.Change(LifeCycleKeys.Stage);
```

LifeCycleKeys와 LifeCycleKey는 `ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base`에 있다. LifeCycleBase는 Global의 로딩 서비스를 루트 resolver로 연결한다. Initialize 때 SceneIndex별 SceneExitLoaded/SceneEnterLoaded Node를 쓰기 전용 `LodingRegisterService`에 등록하고, Enter 요청은 `LodingService.Load`로 위임한다. LodingRegisterSystem은 BeforeLoad·AfterLoad 공통 Node와 Scene별 Node 목록만 보관한다. LodingSystem은 읽기 전용 `LodingRegisterReader`로 정렬된 목록을 받아 BeforeLoad 공통 Node → 현재 State의 SceneExitLoaded Node → LoadSceneAsync → 대상 State의 SceneEnterLoaded Node → AfterLoad 공통 Node 순서로 실행한다. 공통 Node는 Fade·입력·Loading UI를 연결하고, State Node는 해당 화면의 Buffer 바인딩을 담당한다. 같은 단계의 Node는 Priority가 작은 순서와 같은 Priority의 등록 순서로 실행되며, 이 NodeQueue는 Global JobRunner와 별개다. 현재 Lobby/Stage/Artifact/Combat의 SceneIndex는 0/1/2/3이며 실제 Build Settings와 연동해서 사용해야 한다.

JobRegister는 static Plan에서 Job 목록을 생성받아 Runner에 등록한다. 작은 Priority 우선이며 동률은 등록 순서다. 미완료 Job은 다음 Tick에도 Execute될 수 있다. 한 Tick의 즉시 완료 처리 상한은 32개다. 현재 각 Plan의 Start 작업은 임시 구현이며 실제 게임 작업 연결이 남아 있다.

## 11. 변경 영향 확인

| 변경 대상 | 함께 확인할 곳 |
|---|---|
| 그룹 이동 / 네임스페이스 | Factory, ServiceKeys, using, 리플렉션 문자열, 문서 |
| 서비스 계약 / 모듈 단계 | ExportServices, 부모 Context, 실행 순서, 테스트 |
| SO 타입 / 에셋 이동 | .meta GUID, Catalog·Table 참조, 문자열 Asset 경로 |
| MonoBehaviour / RendererFeature 네임스페이스 | GUID 유지, MovedFrom, 에디터 직렬화 연결 |
| Editor 도구 | Editor 폴더 경계, Player 컴파일 제외 |
| Resources 이동 | Resources 폴더 아래 상대 로드 경로 유지 |

현재 렌더 피처와 FakeGI·UIBase는 네임스페이스 이전 정보를 MovedFrom으로 제공한다. 네 URP 피처는 Game/SceneView 카메라만 허용하고 PreviewScene 및 Prefab Stage의 SceneView를 제외한다. 노멀 임포터는 Tool/Editor에서 모델 정점의 평균 노멀을 UV3에 저장한다.

## 12. 통합 체크리스트

- [ ] Framework에 게임별 구현이나 Systems 역참조를 추가하지 않았는가?
- [ ] LifeCycle / Global 형제 계층과 실제 등록 범위가 일치하는가?
- [ ] 서비스·모듈 등록을 구분하고 중복 Build를 피했는가?
- [ ] 필요한 Query·서비스 연결 후 Initialize하며 Tick·Shutdown 담당자가 하나인가?
- [ ] UI·Input·Render의 개별 준비 조건과 반환 계약을 지켰는가?
- [ ] 이동 시 GUID, 네임스페이스, 호출자, 문자열 경로를 함께 확인했는가?
- [ ] 어셈블리 컴파일과 해당 변경의 테스트를 확인하고 에디터 확인 여부를 구분했는가?

## 13. 리뷰 판정

기존 FW-001~016은 코드가 검사하거나 공개 계약이 요구하는 범위에 적용한다. FW-101~104는 권장사항, FW-201~205는 허용된 확장 지점이다. 새 정책을 기존 MUST로 간주하지 않는다.

- **PASS:** 적용되는 계약과 필요한 검증 근거를 충족한다.
- **FAIL:** 구체적인 계약 위반이나 재현 가능한 회귀가 있다.
- **WARNING:** 권장사항·유지보수 관점의 개선이다.
- **NEEDS DESIGN REVIEW:** 공개 계약이 정의하지 않은 수명·복구·소유권 정책이 필요하다.

## 14. 남아 있는 확인 사항

### DR-001 — Unity 구동과 실패 복구

Global/InGame/LifeCycle의 Export와 루트 등록은 구현됐다. 예전의 비어 있는 Export·Require 기본값·잘못된 nameof 지적은 현재 코드에 해당하지 않는다. 다만 실제 Unity 부트스트랩, Runtime Tick·씬 재바인딩의 구동 연결, 초기화 도중 예외의 복구 정책은 별도로 확인해야 한다.

### DR-002 — 키 이름과 서비스 조회

현재 SystemServiceStore는 키 이름이 아닌 서비스 타입 호환성으로 첫 결과를 반환한다. 같은 타입의 인스턴스를 여러 이름으로 구별해야 한다면 별도 계약이 필요하다. DataBaseRegistry의 정확한 구체 타입 조회와 구분한다.

### DR-003 — Outline / Material과 미완성 기능

Outline은 Render Factory와 Context에 연결돼 있다. Loading도 씬 로딩·UI·콜백·Tick 구현이 있다. 하지만 RenderDataBaseFactory의 MetrialTable 등록과 RenderDataBase의 Query 할당은 여전히 주석 처리돼 있다. Outline.Initialize는 MetrialTableQuery와 `_Outline` 머티리얼을 요구하므로 현재 기본 조립의 렌더 초기화 완료를 보증할 수 없다. TextEffectSystem·TextureDataSO는 빈 파일이다. 이 문서 수정에서는 구현을 변경하지 않았다.

### DR-004 — Color Scope 반환

IColorHandle은 Dispose를 노출한다. 시스템 Shutdown도 아직 활성인 Handle을 정리하지만, 일반 수명 종료에서는 소유자가 Dispose를 호출한다. PropertyBlock.Clear가 Renderer의 표시 상태 복원까지 보증하는지도 별도 문제다.

### DR-005 — LightHandle 재사용

Pool이 Handle 객체 자체를 재사용하고 Scope는 Handle 목록을 보관한다. 개별 반환 뒤 다른 Scope로 재대여될 때 이전 참조의 소유권을 IsValid만으로 보증하지 않는다.

### DR-006 — Effect 데이터와 Pool

테이블은 MaxCount를 검증하지만 Pool이 그 값을 이용해 상한을 강제하지는 않는다. EffectSystem의 자식 ParticleSystem 검사와 실제 Pool의 루트 컴포넌트 요구가 다르므로 프리팹 조건을 확인한다.

### DR-007 — Shutdown의 범위

Delay.Shutdown은 비어 있다. 개별 Render 시스템의 정리 범위도 다르다. 모든 Unity 객체가 Destroy되거나 같은 Runtime을 다시 초기화할 수 있다고 가정하지 않는다.

### DR-008 — 이벤트 수명·예외

SubscribeStage는 개별 해제 토큰을 반환하지 않고 서비스 종료 때 일괄 정리한다. Stage라는 이름이 Stage 전환 자동 정리를 의미하지 않는다. 이벤트는 동기 전달하며 구독자별 예외 격리를 제공하지 않는다.

### DR-009 — 입력 Layer 전환

Layer 이름의 동등성과 ActionMap 허용 집합은 구분한다. 장치 상태와 Layer 변경 동기화, 전환 직후 Canceled 입력의 처리는 연결 코드가 결정해야 한다.

### DR-010 — UI 재바인딩

UISystem은 최초 BindScene 완료 전 등록된 `GlobalUIBase`를 `_globalUIs`에 저장한다. 플레이 중에는 해당 UI의 `transform.root.gameObject`를 DontDestroyOnLoad로 처리하므로 같은 루트 아래의 오브젝트도 함께 유지된다. 이후 BindScene은 일반 UI Registry·부모 관계·이력을 다시 만들면서 저장된 유지 UI와 열린 루트 상태를 보존한다. 유지 UI를 다시 수집하거나 서비스에 재바인딩하지 않으며 이후 씬의 새 GlobalUI는 목록에 추가하지 않는다. Shutdown은 유지 UI 목록과 최초 수집 상태도 비운다.

Registry·이력 정리와 실제 UI.Hide·구독 해제는 같은 동작이 아니다. BindScene과 Shutdown에서 화면 표시·구독 수명까지 자동 정리된다고 보증하지 않는다.

### DR-011 — 유틸리티 추가 보장

CustomPool은 중복 반환·소유자·최대 개수 추적을 제공하지 않는다. Mesh 수치 범위, Delay 취소·콜백 재진입 등 추가 요구는 기능 경계에서 정의한다.

### DR-012 — 검증 범위

구조 변경 시 Unity 참조 어셈블리 기반 컴파일 12개 대상이 통과했다. 네임스페이스 변경 전후 372개 테스트 결과는 같았으며 361개 통과, 기존 Core 실패 11개였다. 이후 LifeCycle 계층 수정의 관련 managed 테스트 17개는 통과했다. 서로 중복되는 실행 결과이므로 합산하지 않는다. 실제 Unity Editor 임포트·Play Mode와 네이티브 객체를 생성하는 전체 조립 테스트는 별도 확인 대상이다.

## 15. 문서 유지 기준

폴더 존재, Factory 등록, Runtime 구동, 실제 씬 실행을 서로 구분해서 기록한다. 새 기능이나 소유 관계가 바뀌면 구조 문서·이 가이드·간단 안내 문서의 예제를 함께 갱신한다. 날짜가 붙은 과거 계획과 설계서는 당시 기록으로 유지한다.

이번 문서 갱신에서는 관련 MD 8개의 로컬 링크 104개와 코드 블록 구분을 확인하고, 기존 FW 계약 25개를 유지했다. 최소 조립 예제와 소비자 API 예제 8개는 컴파일을 확인했으며, 최소 예제는 실제 실행해 반환값 1을 확인했다. 구현 코드·에셋·메타데이터 파일 1,356개의 내용은 변경하지 않았다.


## 카메라 서비스와 Cinemachine 참조

카메라는 `global.RenderServiceCommand.CoreGroup.CameraGroup`을 통해 접근한다. CameraCatalogSystem은 BindScene에서 로드된 씬의 CinemachineVirtualCamera를 수집하고 오브젝트 이름으로 GetHandle을 제공한다. 제어 기간에 핸들을 보관하고 사용이 끝나면 Dispose로 반환한다. 줌·흔들림 요청은 CameraEffectSystem.Dispatch로 전달한다. 세부 계약은 [카메라 카탈로그](../../../../../docs/render-camera-catalog.md)와 [카메라 효과](../../../../../docs/render-camera-effects.md)를 참고한다.

현재 카메라 구현과 CameraCatalogSystemTests·CameraEffectSystemTests가 Cinemachine 타입을 직접 사용하므로 Systems 및 Framework 테스트 어셈블리의 Cinemachine 참조를 유지한다. UI·Audio 테스트 참조를 추가할 때 Cinemachine이나 Core.Application 참조를 제거하지 않는다.

