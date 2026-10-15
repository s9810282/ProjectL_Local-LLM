# Systems 서비스 안내

2026-09-22 기준. 서비스 등록·조회 기반은 Framework에, 실제 게임 API는 Systems에 있다. [전체 구조](01_Framework_Overview.md)와 [상세 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)을 함께 참고한다.

## 등록과 Context

서비스 인터페이스는 IServiceModule을 상속한다. ExportServices의 AddService는 조회 계약이고 AddModule은 실행 대상 등록이다. 한 객체가 둘 다 필요하면 각각 등록한다. Context는 관련 서비스 인터페이스를 모아 제공하며 내부 Store를 그대로 노출할 필요는 없다.

## 루트에서 조회하는 그룹

```csharp
var global = resolver.GetService(SystemGroupsService.Global);
var inGame = resolver.GetService(SystemGroupsService.InGame);
var lifeCycle = resolver.GetService(SystemGroupsService.LifeCycle);
```

필요한 using: `ProjectL.Framework.Composition`, `ProjectL.Systems.ServiceKeys`. `resolver`는 이미 구성된 ISystemServiceResolver다. Buffer 그룹과 Read/WriteSink는 내부 키를 이용하는 조립·시스템 코드에서 사용한다.

| 루트 그룹 | 제공 API |
|---|---|
| Global | RenderServiceCommand, UtilityServiceCommand, InputServiceCommand, LodingServiceCommand, UIServiceCommand, JobServiceCommand |
| InGame | CoreGroup, ViewGroup |
| LifeCycle | LifeCycleManager |

Global에 LifeCycle 접근자는 없다. SystemGroupsService.Render/UI/Input/Utility/Loding/Job은 하위 Reader의 키이며 루트에 직접 등록되지 않는다.

## Global 서비스

| 접근 경로 | 기능 / 반환 객체 |
|---|---|
| global.RenderServiceCommand.CoreGroup.EffectSystemServices | GetEffectHandle → EffectScope |
| global.RenderServiceCommand.CoreGroup.MeshSystemServices | Register(MeshFilter) → IMeshRenderHandle.Update |
| global.RenderServiceCommand.CoreGroup.ColorSystemServices | CreateColorHandle → IColorHandle |
| global.RenderServiceCommand.ViewGroup.AreaOutlineSystemServices | Create() → IAreaOutlineHandle.Update |
| global.RenderServiceCommand.CoreGroup.LightSystemServices | CreateScope → LightScope → LightHandle |
| global.RenderServiceCommand.ViewGroup.OutlineSystemServices | CreateOutlineScop → IOutlineScopService |
| global.UtilityServiceCommand.delayService | AddDelayEvent, Runtime Tick으로 처리 |
| global.UtilityServiceCommand.eventSystemService | SubscribeStage / PublishDomain / PublishPresentation |
| global.InputServiceCommand.InputSystemServices | Configure / Submit |
| global.InputServiceCommand.InputLayerServices | ChangeLayer |
| global.UIServiceCommand | OpenUI / CloseUI / BackCloseUI / HideAllUI |
| global.LodingServiceCommand.LodingService | SceneIndex 콜백 등록, Load / Unload |
| global.JobServiceCommand.JobRegister | Register(rootType, jobType) |
| global.JobServiceCommand.JobRunner | Register / RegisterRange / IsIdle / Clear |

이름의 `Loding` 철자는 실제 API 그대로다. IColorHandle과 IAreaOutlineHandle은 Dispose로 등록을 해제한다. 각 Handle의 수명 계약은 동일하지 않다.

Outline은 등록돼 있지만 Initialize에서 사용하는 Material Query의 기본 등록은 아직 주석 상태다. AudioSystem은 보존돼 있으나 기본 Global Factory/Context에 연결돼 있지 않다. 사용 가능한 API 선언과 전체 실행 준비 상태를 구분한다.

## LifeCycle과 Job

```csharp
var lifeCycle = resolver.GetService(SystemGroupsService.LifeCycle);
lifeCycle.LifeCycleManager.Change(LifeCycleKeys.Stage);

var global = resolver.GetService(SystemGroupsService.Global);
global.JobServiceCommand.JobRegister.Register(JobRootType.Stage, JobType.Start);
```

LifeCycleKeys는 `ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base`, JobRootType·JobType은 `ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister`에 있다. 위 호출은 Runtime 초기화 이후를 전제로 한다.

LifeCycleManager는 활성 구간을 관리하고, 실제 진입은 Loading 완료 콜백으로 전달된다. JobRegister는 LifeCycleSystem/LifeCycles 아래의 Plan을 통해 Job 목록을 만들고 Global의 Runner에 등록한다. Job은 현재 Priority 순서로 실행되며 미완료 작업은 다음 Tick에도 Execute될 수 있다.

## 데이터 Query

DataBaseFactory.Create(DatabaseCatalogSO)는 Query를 IServiceModule로 반환한다. Runtime.InitData에 전달하며 데이터베이스는 별도 생성 경로다.

- InGame Query: StageMapTable / StageTable / StageViewPropTable.
- Render Query: EffectTable / LightTable. MetrialTable의 실제 Factory 등록은 미완성.
- DataBaseRegistry는 항목의 구체 타입으로 조회한다. SystemServiceStore의 타입 호환성 조회와 다르다.
- SO 에셋은 Assets/ProjectL/DataBaseSO, 타입 정의는 Script/Systems/DataBaseSystem/Data에 있다.

## 조회 시 주의

등록 목록은 Build 이후 고정된다. SystemServiceStore는 현재 키 이름을 비교하지 않고 `is TService`로 첫 결과를 반환한다. 동일 타입을 이름이 다른 키로 분리하는 API로 사용하지 않는다. 소비자에게 필요한 인터페이스만 전달해도 된다.
