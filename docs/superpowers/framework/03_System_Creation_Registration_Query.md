# System 제작 / 등록 / 조회

2026-09-22 기준. 신규 게임 기능은 Systems에 두며, 공용 기반만 Framework에 둔다. 아래 예제는 기존 등록 계약을 보여 주기 위한 예시이며 현재 게임에 Counter 기능을 추가하는 문서는 아니다.

## 1. 위치와 계약 선택

1. Global·InGame·LifeCycle·Buffer 중 책임에 맞는 소속을 정한다.
2. `ProjectL.<Script 아래 상대 폴더>` 네임스페이스를 사용한다.
3. 외부 API는 IServiceModule 파생 인터페이스로, 실행 참여는 필요한 모듈 인터페이스로 정의한다.
4. UI/Render 기반은 Global 안에, LifeCycle은 RuntimeSystem 직속에 둔다.

## 2. 컴파일 가능한 최소 조립 예제

아래는 `Systems/RuntimeSystem/GlobalSystem/CounterSystem`에 새 기능을 둔다고 가정한 예제다. 실제 Global 편입 단계는 다음 절에 설명한다. 생명주기와 조회 계약을 보여 주기 위해 예제 Runtime에는 Counter만 등록한다.

```csharp
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.CounterSystem
{
    public interface ICounterService : IServiceModule
    {
        int Count { get; }
        void Increment();
    }

    internal sealed class CounterSystem :
        ICounterService, ISystemInitialize, ISystemShutdown, IExportSystemServices
    {
        private bool _ready;
        public int Count { get; private set; }

        public void Initialize() { _ready = true; }
        public void Shutdown() { _ready = false; }

        public void Increment()
        {
            if (!_ready) throw new InvalidOperationException("Not initialized.");
            Count++;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICounterService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    internal static class CounterCompositionExample
    {
        internal static int Run()
        {
            var builder = new SystemBuilderHelper();
            new CounterSystem().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());

            runtime.Initialize();
            var key = new SystemGroupKey<ICounterService>("CounterExample");
            var counter = runtime.GetService(key);
            counter.Increment();
            int result = counter.Count;
            runtime.Shutdown();
            return result;
        }
    }
}
```

AddService는 서비스 인터페이스를 등록한다. AddModule은 실행 대상에 포함시킨다. 이 예제에는 InitData·PostBindService 모듈이 없어서 바로 Initialize할 수 있다. 실제 게임 Runtime에는 두 단계의 대상이 있으므로 필요한 준비를 완료해야 한다.

## 3. 실제 그룹에 편입

예제의 Counter 같은 Global 서비스를 추가한다면 다음을 연결한다.

- System 구현의 ExportServices.
- Global 또는 해당 하위 Factory의 생성·등록.
- 해당 그룹의 키와 Context 프로퍼티.
- 그룹에서 필요한 InitData / PostBindService / Initialize / Tick / Shutdown 전달.
- 등록·조회·단계 순서를 확인하는 테스트.

키는 실제 구현에서 Systems/ServiceKeys에 모은다. Factory는 Systems/Factory에 둔다. 내부 조립 예제의 Runtime 생성 코드를 여러 소비자에서 반복하지 않는다. 일반 소비자는 주입받은 기존 서비스를 사용한다.

Builder는 한 번 Build한 뒤 AddService / AddModule / 재Build를 거부한다. Query처럼 실행 단계가 없는 서비스에는 불필요한 모듈을 붙이지 않는다.

## 4. 현재 게임 서비스 조회

```csharp
var global = resolver.GetService(SystemGroupsService.Global);
var mesh = global.RenderServiceCommand.CoreGroup.MeshSystemServices;
using var handle = mesh.Register(meshFilter);
handle.Update(data);

var lifeCycle = resolver.GetService(SystemGroupsService.LifeCycle);
```

`resolver`는 ISystemServiceResolver이고 SystemGroupsService는 ProjectL.Systems.ServiceKeys에 있다. 실제 기능 사용은 초기화 완료를 전제로 한다. Render·UI·Input·Utility·Loading·Job은 Global Context로 조회하며 LifeCycle은 루트에서 직접 조회한다.

## 5. 새 LifeCycle 등록

LifeCycleSystem/Base/LifeCycleBase를 확장하고 Key·SceneIndex 및 필요한 훅을 구현한다. Factory/Runtime/LifeCycle/LifeCycleSystemFactory에서 등록한다. 루트 FrameworkRuntimeFactory가 LifeCycle 그룹을 Global·InGame 다음에 등록하며, 역순 종료로 의존 서비스보다 먼저 정리한다.

GlobalSystemFactory에 LifeCycle을 등록하거나 Global Context에 LifeCycle 접근자를 추가하지 않는다. Loading 서비스는 PostBindService에서 루트 resolver로 Global을 찾아 연결한다.

## 6. 검증과 영향 범위

서비스 인터페이스, Factory, Context, ServiceKeys, 부모의 단계 전달, 네임스페이스·어셈블리 참조를 함께 확인한다. 직렬화되는 Unity 타입을 옮기면 .meta GUID와 필요한 MovedFrom 정보를 유지한다. CoreCaculator는 Systems 아래에 있어도 세 개의 별도 Core asmdef에 속한다.

상세 계약은 [개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), 설계 권장은 [개발 지침](Framework%20Development%20Guidelines.md)에 있다.
