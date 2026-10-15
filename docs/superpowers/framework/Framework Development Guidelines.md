# Framework / Systems Development Guidelines

2026-09-22 현재 폴더·서비스 계층으로 갱신했다. 기존 책임 분리, Scope/Handle, Factory, Pool, 이벤트와 DB 설계 원칙은 유지한다. 이 문서의 일반 설계 권장과 코드가 실제 강제하는 계약은 구분한다. 정확한 FW 계약·미완성 기능은 [개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), 조립 관계는 [구조 문서](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md)를 참고한다.

## 현재 배치 기준

- Framework에는 Composition / SystemBase / DataBase / Utility의 공용 기반만 둔다. 게임 서비스·키·Factory·SO 타입은 Systems에 둔다.
- RuntimeSystem의 BufferSystems / GlobalSystem / InGameSystem / LifeCycleSystem은 형제다. UI·Render는 Global 하위, LifeCycle은 루트 그룹이다.
- 루트 조회는 Global·InGame·LifeCycle이다. Render·UI·Job 등은 Global Context를 거친다. 키가 public이어도 루트 등록을 뜻하지 않는다.
- CoreCaculator는 InGameSystem/CoreSystem 아래에 있지만 Contracts / Domain / Application asmdef는 분리된다. Framework는 Systems·Core·FMOD에 역참조하지 않는다.
- 네임스페이스는 `ProjectL.<Script 기준 상대 폴더 경로>`다. asmdef 이름과 네임스페이스를 같은 것으로 취급하지 않는다.
- 루트 등록 순서는 Buffer → Global → InGame → LifeCycle이다. 각 단계는 해당 계약을 구현한 그룹에 전달하고 종료는 역순이다. 자식 그룹의 단계 전달·종료 순서는 구현을 확인한다.
- SO 에셋 / Prefabs / Resources는 Assets/ProjectL 아래에 둔다. URP 피처는 Script/Render, 임포터는 Script/Tool/Editor다.

## 1. 문서 목적

이 문서는 현재 ProjectL Framework 위에서 새로운 System, Service, Scope, Handle, Pool 및 관련 기능을 개발할 때 따라야 하는 구조적 원칙과 작업 절차를 정의한다.

목표는 단순히 코드를 동작시키는 것이 아니라 다음 구조적 특성을 지속적으로 유지하는 것이다.

- System 간 결합도 최소화
- 책임의 명확한 분리
- Framework Lifecycle의 일관성 유지
- Service 기반 기능 접근
- Factory 기반 System Composition
- Build 이후 구조의 안정성 확보
- 필요 이상으로 거대한 Manager / God Class가 생성되는 것을 방지
- 향후 기능 확장을 고려한 구조 유지
- Runtime에서 불필요한 검색, 할당 및 GC 발생 최소화

---

# 2. 가장 중요한 기본 원칙

새로운 기능을 추가할 때 다음 질문을 먼저 한다.

> "이 기능은 어느 객체가 책임져야 하는가?"

기능을 단순히 기존 System에 추가하지 않는다.

기능의 성격에 따라 다음과 같이 분리한다.

```text
System
    └─ 기능의 상태와 실행 Lifecycle

Service
    └─ 외부에 제공하는 기능 API

Scope
    └─ 특정 사용 단위 / 작업 단위의 Lifetime 관리

Handle
    └─ 실제 Runtime 객체 또는 리소스에 대한 경량 접근 단위

Factory
    └─ 객체 생성 및 System Composition

Group
    └─ 관련 System의 도메인 단위 묶음

Store / Reader
    └─ Build된 객체의 저장 및 조회

Pool
    └─ 반복 생성되는 객체의 재사용
```

하나의 객체가 위 역할을 여러 개 동시에 담당하도록 확장하지 않는다.

---

# 3. System 설계 원칙

## 3.1 System은 하나의 명확한 책임을 가진다

System에 새로운 기능을 추가하기 전에 해당 기능이 현재 System의 책임에 속하는지 확인한다.

예:

```text
RenderSystem
├─ Effect
├─ Mesh
├─ Color
└─ Light
```

위 구조에서 각 기능의 구현은 독립적인 System으로 유지한다.

```text
EffectSystem
MeshSystem
ColorSystem
LightSystem
```

그리고 필요할 경우:

```text
RenderSystemGroup
```

에서 묶는다.

### 지양

```text
RenderSystem
├─ Effect
├─ Mesh
├─ Color
├─ Light
├─ Audio
├─ UI
├─ Input
├─ Network
└─ ...
```

System이 모든 기능을 직접 관리하기 시작하면 God Class가 된다.

---

# 4. 새로운 System을 추가하는 기준

새로운 기능을 추가할 때 다음 순서로 판단한다.

### Step 1. 독립된 책임인가?

다른 기능과 상태 및 Lifecycle이 명확하게 분리된다면 별도의 System을 고려한다.

### Step 2. 외부에서 사용할 API가 필요한가?

필요하다면 Service Interface를 정의한다.

```csharp
public interface IMySystemService : IServiceModule
{
}
```

### Step 3. Framework Lifecycle에 참여하는가?

필요한 Lifecycle만 구현한다.

```csharp
ISystemBindScene
ISystemInitData
ISystemPostBindService
ISystemInitialize
ISystemTick
ISystemShutdown
```

모든 Interface를 무조건 구현하지 않는다.

### Step 4. 기존 Group에 포함되는가?

관련 도메인이 있다면 기존 Group에 포함한다.

```text
MySystem
    ↓
RenderSystemGroup
```

도메인 자체가 다르면 새로운 Group을 고려한다.

---

# 5. Service 설계

Service는 System의 내부 구현을 외부에 노출하기 위한 것이 아니다.

Service는 다음 목적을 가진다.

> "외부에서 이 System을 어떻게 사용할 것인가?"

따라서 Service에는 사용자가 필요한 기능만 노출한다.

```csharp
public interface IEffectSystemServices : IServiceModule
{
    EffectScope CreateScope();
}
```

System 내부의 데이터 구조나 관리용 컬렉션을 직접 노출하지 않는다.

### 지양

```csharp
public interface IMyService
{
    Dictionary<int, Object> Objects { get; }
    List<Object> InternalList { get; }
    void UpdateInternalState();
}
```

### 권장

```csharp
public interface IMyService : IServiceModule
{
    MyScope CreateScope();
}
```

외부 사용자가 필요한 작업만 API로 제공한다.

---

# 6. Service와 System의 책임 구분

다음 구분을 유지한다.

```text
System
→ 내부 상태 관리
→ 실제 기능 실행
→ Lifecycle 처리
→ Resource / Pool 관리

Service
→ 외부 API 제공
→ 사용자가 System 기능을 호출할 진입점

Scope
→ 특정 사용자의 작업 단위
→ 사용 기간 동안 필요한 상태 보유

Handle
→ 개별 Runtime 객체에 대한 접근 / 제어 단위
```

예:

```text
LightSystem
    ↓
ILightSystemService
    ↓
LightScope
    ↓
LightHandle
```

이 구조를 유지하면 하나의 Service가 모든 상태를 직접 관리하는 구조를 피할 수 있다.

---

# 7. Scope 설계

Scope는 특정 작업이나 사용자의 Lifetime을 표현할 필요가 있을 때 사용한다.

예:

```text
Register(MeshFilter)
CreateLightScope()
CreateEffectScope()
```

Scope는 System 자체가 아니다.

Scope의 역할은:

```text
System
  ↓
Scope 생성
  ↓
사용자가 Scope를 통해 작업
  ↓
Dispose / Release
  ↓
System에 자원 반환
```

이다.

Scope를 만들었다면 누가 소유권을 가지고 있는지도 명확해야 한다.

예:

```csharp
using var handle = meshSystem.Register(meshFilter);
handle.Update(data);
```

또는 명시적으로:

```csharp
var handle = meshSystem.Register(meshFilter);

...

handle.Dispose();
```

### 주의

System 내부에 모든 Scope를 무조건 영구적으로 보관하지 않는다.

현재 Framework에서는 Shutdown 시 Scope를 정리하는 구조가 있으므로, Scope Lifetime 정책을 명확히 유지한다.

---

# 8. Handle 설계

Handle은 실제 Runtime 객체를 직접 외부에 노출하는 대신 접근 계층으로 사용할 수 있다.

예:

```text
LightScope
    ↓
LightHandle
    ↓
Unity Light
```

Handle의 장점은 다음과 같다.

- 실제 객체의 소유권을 외부에 넘기지 않음
- Lifetime 제어 가능
- 향후 내부 구현 변경 가능
- Pool 또는 Resource Manager와 연결 가능

따라서 외부 코드가 내부 Unity Object를 직접 관리하게 만드는 구조는 가급적 피한다.

---

# 9. Factory 규칙

Factory는 Framework의 Composition Root이다.

Factory의 책임은 다음과 같다.

```text
System 생성
Service Export
SystemGroup 구성
Runtime 구성
```

예:

```text
RenderSystemFactory
├─ EffectSystem 생성
├─ MeshSystem 생성
├─ ColorSystem 생성
├─ LightSystem 생성
└─ RenderSystemGroup 구성
```

System 내부에서 다른 System을 직접 생성하지 않는다.

### 지양

```csharp
public class MeshSystem
{
    private LightSystem _lightSystem = new LightSystem();
}
```

### 권장

```text
MeshSystem
LightSystem
    ↓
RenderSystemFactory
    ↓
RenderSystemGroup
```

의존성이 필요하다면 Composition 단계에서 연결한다.

---

# 10. Builder 사용 규칙

`SystemBuilderHelper`는 System을 실행하기 위한 객체가 아니다.

Builder의 역할은 다음으로 제한한다.

```text
Service 등록
Module 등록
중복 검증
Build Snapshot 생성
```

예:

```csharp
system.ExportServices(systemBuilderHelper);
```

모든 System 등록이 완료되면:

```csharp
var reader =
    systemBuilderHelper.BuildSystemReader();
```

를 통해 Build를 완료한다.

Build 이후 Runtime 코드에서 Builder를 직접 사용하지 않는다.

---

# 11. Build 이후 구조를 변경하지 않는다

Framework의 중요한 원칙 중 하나는:

```text
Build 전
→ 조립 단계

Build 후
→ 실행 / 조회 단계
```

를 분리하는 것이다.

Flow:

```text
Factory
    ↓
Builder
    ↓
Build
    ↓
Snapshot
    ↓
Store
    ↓
Reader
    ↓
Runtime
```

Build 이후에는 System 구성 자체를 동적으로 변경하려고 하지 않는다.

새로운 System을 Runtime 중간에 추가해야 하는 요구가 생긴다면, 그것이 정말 System Composition의 문제인지부터 검토한다.

---

# 12. Snapshot과 Store

Build 과정에서 생성된 데이터는 Snapshot으로 고정한다.

```text
SystemBuildSnapshot
├─ Services
└─ Modules
```

Snapshot은 Build 단계의 결과를 보존하는 역할을 한다.

그 이후:

```text
SystemBuildSnapshot
        ↓
ServiceStore
ModuleStore
        ↓
SystemReader
```

구조로 변환한다.

이 구조를 유지하는 이유는 Runtime에서 System 구성 정보가 임의로 변경되지 않도록 하기 위함이다.

---

# 13. System 조회

Runtime에서 System을 직접 생성하지 않는다.

```text
Factory
→ 생성

Resolver / Reader
→ 조회
```

기본적인 접근 방향:

```text
ISystemServiceResolver
        ↓
SystemGroupKey<T>
        ↓
Service
```

타입 기반 접근을 우선한다.

예:

```csharp
var render =
    resolver.GetService(SystemGroupsService.Global).RenderServiceCommand;
```

이후:

```text
Render Context
├─ Effect Service
├─ Mesh Service
├─ Color Service
└─ Light Service
```

형태로 접근한다.

---

# 14. 문자열 기반 조회 최소화

System이나 Service를 찾기 위해 문자열 검색을 기본 방식으로 사용하지 않는다.

### 지양

```csharp
GetService("Render");
GetSystem("Light");
```

### 권장

```csharp
resolver.GetService(SystemGroupsService.Global).RenderServiceCommand;
```

또는 Framework에서 제공하는 타입 기반 API를 사용한다.

타입 안정성을 최대한 유지한다. 현재 Store는 Key.Name이 아닌 `is TService`로 첫 일치 객체를 찾으므로 같은 서비스 타입을 키 이름으로 구별하지 않는다.

---

# 15. Lifecycle 규칙

System Lifecycle은 Framework가 관리한다.

다음은 연결 코드가 선택할 수 있는 실행 순서 예시다. Runtime은 Initialize 전 필요한 InitData·PostBindService 완료를 검사하지만 이들 사이의 상대 순서와 BindScene 위치까지 고정하지 않는다:

```text
BindScene
    ↓
InitData
    ↓
PostBindService
    ↓
Initialize
    ↓
Tick
    ↓
Shutdown
```

단, 모든 System이 모든 단계에 참여하는 것은 아니다.

필요한 Interface만 구현한다.

---

## BindScene

Unity Scene 또는 Scene Object와의 연결이 필요한 System에서 사용한다.

---

## InitData

DataBase 또는 외부 Query 등의 초기 데이터를 받아야 할 경우 사용한다.

```csharp
ISystemInitData<T>
```

를 우선 고려한다.

가능하면 구체적인 데이터 타입을 명확하게 지정한다.

---

## PostBindService

다른 System의 Service가 등록된 이후 Service Resolver를 통해 접근해야 하는 경우 사용한다.

```csharp
void PostBindService(
    ISystemServiceResolver resolver)
{
}
```

직접 다른 System을 생성하거나 찾지 않는다.

---

## Initialize

실제 System 내부 초기화가 필요한 시점에 사용한다.

등록된 Factory / Builder 구성과 Runtime 초기화 책임을 혼동하지 않는다.

---

## Tick

매 프레임 처리해야 하는 기능만 구현한다.

Tick에서 다음 작업을 무분별하게 수행하지 않는다.

```text
Find
GetComponent
LINQ
불필요한 Allocation
반복적인 Dictionary 생성
불필요한 Object 생성
```

Runtime Hot Path는 가능한 한 안정적인 메모리 구조를 사용한다.

---

## Shutdown

System이 소유한 Resource를 정리한다.

예:

```text
Scope
Subscription
Pool
Native Resource
Unity Object
Event Handler
```

특히 Event Subscription과 Scope는 Shutdown 시점에 누락되지 않도록 한다.

---

# 16. Pool 사용 규칙

Pool은 무조건 사용하는 것이 아니라 **반복 생성 비용과 Lifetime 특성**을 기준으로 적용한다.

현재 Framework의 Pool 구조는:

```text
PoolContext<T>
├─ CreateFunc
├─ initialSize
├─ ReturnRule
└─ Lifecycle
```

로 구성되어 있다.

Pool을 사용할 때 다음 책임을 분리한다.

```text
Pool
→ 객체 생성 / 보관 / 반환

ReturnRule
→ 반환 가능 여부 판단

Lifecycle
→ Get / Return 시 상태 처리
```

따라서 Pool 클래스 자체에 특정 객체의 모든 초기화 규칙을 집어넣지 않는다.

---

# 17. Prewarm 판단

초기 생성 비용이 크고 빈번하게 사용되는 객체라면 Prewarm을 고려한다.

```text
초기 생성 비용 높음
+
사용 빈도 높음
+
동시 사용량 예측 가능
        ↓
Prewarm 고려
```

반대로 사용 빈도가 낮거나 생성 비용이 낮은 객체까지 무조건 Prewarm하지 않는다.

목표는 메모리와 CPU 사이의 균형이다.

---

# 18. Pool Lifecycle

Pool을 사용하는 객체는 상태 초기화를 명확하게 정의한다.

예:

```text
Get
 ↓
OnGet

사용

Return
 ↓
OnBeforeReturn
 ↓
ReturnRule
 ↓
Pool
 ↓
OnReturn
```

특히 반환 전에 이전 사용자의 상태가 남아 있지 않도록 초기화한다.

예:

```csharp
public void OnBeforeReturn(DelayData item)
{
    item.Clear();
}
```

---

# 19. Runtime Allocation 주의

Framework는 Runtime에서 반복적인 Allocation을 줄이는 것을 중요하게 생각한다.

특히 Tick 경로에서는 다음을 주의한다.

```text
new
LINQ
ToArray()
Boxing
Closure
Delegate 생성
문자열 생성
불필요한 List 생성
```

단, Allocation을 무조건 제거하는 것이 목적은 아니다.

**실제 Hot Path에서 반복적으로 발생하는 Allocation을 우선 제거한다.**

가독성을 심각하게 훼손하면서 모든 Allocation을 제거하는 식의 과도한 최적화는 지양한다.

---

# 20. Event System 사용 규칙

Event System은 다음 계층을 유지한다.

```text
StageEvent
├─ DomainEvent
└─ PresentationEvent
```

Domain과 Presentation의 의미를 혼합하지 않는다.

```text
Domain
→ 게임 규칙 / 상태 변화

Presentation
→ 표현 / 연출 / 화면 반응
```

Subscription에는 Lifetime이 존재한다.

따라서 Subscription을 생성했다면 반드시 적절한 Scope 또는 Shutdown Lifecycle에서 해제될 수 있어야 한다.

---

# 21. Event Callback에서 주의할 사항

Event Callback 안에서 무거운 System 처리를 연쇄적으로 수행하지 않는다.

특히:

```text
Event
 ↓
Event
 ↓
Event
 ↓
Event
```

형태의 깊은 연쇄 구조는 추적하기 어려운 Runtime Flow를 만든다.

Event는 의존성을 줄이기 위한 수단이지 모든 System 호출을 대체하는 수단이 아니다.

직접적인 의존성이 더 명확한 경우 Service 호출을 우선한다.

---

# 22. DataBase 사용 규칙

DataBase의 ScriptableObject 데이터와 Runtime System의 책임을 분리한다.

```text
ScriptableObject
→ 설정 / 정적 데이터

Database Query
→ 데이터 접근

System
→ Runtime 상태 / 실행
```

Runtime System이 ScriptableObject를 직접 여기저기 검색하지 않는다.

가능하면:

```text
Database
    ↓
Query
    ↓
InitData
    ↓
System
```

구조를 사용한다.

---

# 23. Query와 Runtime 상태 분리

Database에서 가져온 데이터 자체를 Runtime System의 상태 저장소처럼 사용하지 않는다.

예:

```text
EffectDataSO
```

는 설정 데이터이고,

```text
Effect Runtime State
```

는 System이 관리하는 Runtime 데이터다.

두 Lifetime과 변경 가능성을 구분한다.

---

# 24. Cast 사용 원칙

Interface 기반 구조에서는 불필요한 Cast를 줄인다.

### 지양

```csharp
var system =
    (MeshSystem)resolver.GetService(...);
```

### 권장

```csharp
IMeshSystemService mesh =
    resolver.GetService(...);
```

외부에서는 구현 클래스가 아니라 Service Interface를 사용한다.

Cast가 반복적으로 필요하다면 해당 API의 추상화가 잘못된 것인지 검토한다.

---

# 25. GetComponent / Find 남용 금지

Framework System 간 연결을 위해 다음을 기본 방식으로 사용하지 않는다.

```text
Find
FindObjectOfType
GetComponent
GameObject 검색
Tag 검색
문자열 검색
```

대신 Framework의:

```text
Factory
Service
SystemReader
SystemServiceResolver
Scope
Handle
```

구조를 활용한다.

Scene Object가 반드시 필요한 경우에만 `BindScene` 등 명시적인 Lifecycle을 사용한다.

---

# 26. God Class 방지 기준

다음 현상이 나타나면 System 분리를 검토한다.

```text
하나의 System이 너무 많은 Service를 직접 제공한다.
하나의 System이 여러 도메인의 Pool을 관리한다.
하나의 System이 다른 System의 내부 상태를 직접 관리한다.
하나의 System에 수많은 Create / Update / Release API가 누적된다.
하나의 Factory가 모든 Runtime 로직까지 수행한다.
```

특히 다음과 같은 구조는 피한다.

```text
GameManager
 ├─ Render
 ├─ Audio
 ├─ UI
 ├─ Input
 ├─ Network
 ├─ Effect
 ├─ Character
 ├─ Database
 └─ ...
```

대신:

```text
Runtime
├─ RenderGroup
├─ UtilityGroup
├─ DatabaseGroup
└─ ...
```

처럼 책임을 분리한다.

---

# 27. 과도한 추상화 금지

확장성을 고려하되, 사용되지 않는 추상화를 미리 만들지 않는다.

다음 상황에서는 Interface / Generic / Factory Layer 추가를 신중하게 판단한다.

```text
현재 구현이 하나뿐이다.
확장 요구가 실제로 존재하지 않는다.
추상화가 호출 경로만 복잡하게 만든다.
구체적인 구현보다 Interface가 더 이해하기 어렵다.
```

목표는:

```text
확장 가능한 구조
≠
모든 것을 추상화한 구조
```

이다.

---

# 28. 새로운 기능을 추가하기 전 확인 순서

새 기능을 구현하기 전에 다음 질문을 순서대로 확인한다.

```text
1. 이 기능의 책임은 무엇인가?

2. 기존 System의 책임인가?
   아니면 새로운 System인가?

3. 외부에서 어떤 API가 필요한가?

4. Service로 노출해야 하는가?

5. Scope가 필요한가?

6. Handle이 필요한가?

7. Pool이 필요한가?

8. Lifecycle에 어느 단계가 필요한가?

9. 기존 SystemGroup에 속하는가?

10. Factory에서 어떻게 조립할 것인가?

11. 다른 System과 어떤 Service 관계를 갖는가?

12. Runtime Hot Path에서 Allocation이 발생하는가?

13. Shutdown 시 Resource가 정상적으로 정리되는가?
```

---

# 29. 구현 순서

새 System은 다음 순서로 구현한다.

```text
[1] 책임 정의
        ↓
[2] Service Interface 정의
        ↓
[3] Runtime 데이터 구조 정의
        ↓
[4] Scope / Handle 필요성 판단
        ↓
[5] System 구현
        ↓
[6] Lifecycle 구현
        ↓
[7] ExportServices 구현
        ↓
[8] Factory 등록
        ↓
[9] SystemGroup 연결
        ↓
[10] Runtime 조회 API 연결
        ↓
[11] Shutdown / Lifetime 검증
        ↓
[12] Allocation / Performance 검증
```

---

# 30. 코드 작성 시 우선순위

코드를 작성할 때 우선순위는 다음과 같다.

```text
1. 책임 분리
2. 올바른 Lifecycle
3. 명확한 의존성
4. Service API 안정성
5. Resource Lifetime
6. 확장성
7. Runtime 성능
8. 미세 최적화
```

성능 때문에 구조를 무너뜨리지 않는다.

반대로 구조를 위해 명백한 Hot Path 비용을 무시하지도 않는다.

---

# 31. 최종 Architecture 원칙

현재 Framework의 기본적인 방향은 다음 구조를 유지하는 것이다.

```text
                    Application
                         │
                         ▼
                  Service / Context
                         │
                         ▼
                       System
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
            Scope      Handle      Pool
              │          │          │
              └──────────┼──────────┘
                         ▼
                     Runtime
```

System의 Composition은:

```text
Factory
   ↓
Builder
   ↓
Snapshot
   ↓
Store
   ↓
Reader
   ↓
SystemGroup
   ↓
FrameworkRuntime
```

Lifecycle은:

```text
BindScene
   ↓
InitData
   ↓
PostBindService
   ↓
Initialize
   ↓
Tick
   ↓
Shutdown
```

외부 접근은:

```text
Application
   ↓
SystemServiceResolver
   ↓
SystemGroupKey<T>
   ↓
Service
   ↓
Scope / Handle
```

로 유지한다.

---

# 32. 개발자가 반드시 피해야 하는 것

다음 패턴은 Framework 구조를 우회하는 것으로 간주한다.

```text
System 내부에서 다른 System 직접 new
System 내부에서 Scene 검색으로 다른 System 획득
Service 대신 구현 클래스 Cast
Runtime에서 Builder로 새로운 System 등록
문자열 기반 System 검색
System 하나에 무관한 기능 계속 추가
Scope의 Lifetime 불명확
Handle이 실제 객체의 소유권까지 가져감
Pool에 모든 객체를 무조건 Prewarm
Tick에서 반복적인 Allocation
Event로 모든 의존성을 해결
Database SO를 Runtime 상태처럼 사용
Shutdown에서 Subscription / Resource 미정리
```

이러한 구현이 필요한 것처럼 보이면 먼저 기존 Architecture에서 해당 책임을 어디에 배치해야 하는지 검토한다.

---

# 33. 개발 판단의 핵심

새로운 코드를 작성할 때 가장 중요한 질문은 다음과 같다.

> "어떻게 동작하게 만들 것인가?"보다  
> "이 기능의 책임은 어디에 있어야 하는가?"

그리고 두 번째 질문은 다음이다.

> "이 기능을 나중에 다른 구현으로 교체하거나 확장해야 할 때, 현재 System을 수정하지 않고 조립 단계에서 교체할 수 있는가?"

마지막으로:

> "이 기능이 추가되었을 때 기존 System이 더 많은 책임을 가지게 되는가?"

세 질문에 대한 답을 기준으로 Architecture를 결정한다.

---

# 34. 요약

Framework 개발의 핵심 규칙은 다음과 같다.

```text
System
→ 기능의 실행과 상태 관리

Service
→ 외부 API

Scope
→ 사용 단위의 Lifetime

Handle
→ Runtime 객체 접근

Pool
→ 반복 객체 재사용

Factory
→ 생성 및 Composition

SystemGroup
→ 도메인 단위 묶음

Builder
→ Build 단계 등록

Snapshot
→ Build 결과 고정

Store
→ Runtime 데이터 보관

Reader / Resolver
→ Runtime 조회

FrameworkRuntime
→ Lifecycle 실행
```

그리고 전체 설계의 핵심은:

```text
작은 책임의 System
        +
명확한 Service
        +
명시적인 Lifetime
        +
Factory 기반 Composition
        +
Framework가 관리하는 Lifecycle
        +
Build 이후 안정적인 Read 구조
        =
확장 가능한 Framework
```

새로운 기능은 기존 구조를 우회해서 빠르게 만드는 것보다, **어느 계층의 책임인지 먼저 결정한 후 해당 계층에 맞는 방식으로 추가한다.**
