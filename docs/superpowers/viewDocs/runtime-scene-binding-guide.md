# 런타임과 씬 연결 안내 — 씬에 붙는 시스템 만들기

- 작성일: 2026-10-02
- 작성: View 작업(스테이지 내부 담당)
- 대상: 새 씬(스테이지 맵·타이틀 등)이나 씬 오브젝트를 다루는 시스템을 만드는 작업자
- 근거: [전투 View 설계](../specs/2026-09-30-combat-view-design.md) §10.0(Bootstrap 역할)·§11(씬 번호)·W2(씬 연결), Framework [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-016·DR-001
- 브랜치: `feature/Yonggeun/Stage` (View Phase B1 이후)

## 0. 한 줄 요약

**GameBootstrap은 직접 쓰지 않는다.** 게임 전체에 런타임이 하나 떠 있고, 씬은 그 런타임에 **끼워졌다 빠지는 대상**이다. 씬에 붙는 시스템은 런타임에 한 번 등록해 두고, "내 씬이 로드되면 / 떠나면" 할 일을 **씬 번호에 등록**한다. 씬 오브젝트는 참조만 들고 있고, 시스템이 씬에 들어올 때 한 번 찾아 연결한다.

### 0.1 두 층: 시스템과 씬 오브젝트

| 층 | 무엇 | 언제 생기나 | 어디에 있나 |
|---|---|---|---|
| **시스템**(일반 C#) | 언제 무엇을 그릴지, 입력을 어떻게 해석할지, 결과를 확인하는 순서 같은 **판단** (예: `CombatViewSystem`, `StageMapViewSystem`) | **게임 시작 때 Boot에서 한 번** Factory가 만들어 런타임에 등록한다. 게임이 끝날 때까지 유지된다 | 런타임 안(씬 밖) |
| **씬 오브젝트**(MonoBehaviour) | SceneRoot, Presenter·View, Canvas, 프리팹 같은 **그리는 것과 참조** | **지금처럼 에디터에서 씬에 미리 올려 둔다** | 각 씬 |

씬에 들어갈 때 일어나는 일은 **생성이 아니라 연결(Attach)** 이다.

```text
게임 시작(Boot)   : Factory가 StageMapViewSystem을 만들어 등록 → "씬 1 들어오면 Attach" 예약
StageSelect 로드  : 씬에 미리 올려 둔 StageMapSceneRoot를 시스템이 한 번 찾아서 붙잡음
씬에 있는 동안    : 시스템이 Buffer 값을 읽어 SceneRoot 아래 Presenter에 "이렇게 그려라" 전달
씬을 떠남         : 붙잡은 참조를 놓음(Detach). 시스템 자체는 그대로 살아 있음
다시 들어옴       : 새로 로드된 씬의 SceneRoot를 다시 찾아 붙잡음
```

유닛 View처럼 개수가 바뀌는 것은 Presenter가 풀에서 빌리고 돌려준다. 시스템을 새로 만드는 것과는 다르다.

**예전 방식과의 차이:** 예전에는 판단 로직을 가진 MonoBehaviour(설치기·세션 호스트 등)를 씬마다 올려 두고 그 씬 안에서 스스로 돌게 했다. 이제 판단은 씬 밖의 시스템으로 옮기고 씬 MonoBehaviour는 그리기만 한다. 씬 오브젝트가 시스템을 찾아가지 않고, 시스템이 씬 오브젝트를 찾아온다.

**이렇게 하는 이유**

1. **Framework 규칙(FW-003):** 런타임은 Build 뒤 구조(시스템 목록)를 바꾸지 않는다. 씬 진입 때 시스템을 추가할 수 없으므로 처음에 등록해 두고 씬에 붙었다 떨어진다.
2. **상태가 씬보다 오래 산다:** Buffer(전투 상태·입력 기록), Job, 결과 확인 번호는 씬을 바꿔도 이어져야 한다. 씬 MonoBehaviour에 두면 씬 전환 때 함께 사라진다.
3. **주입 경로가 하나다:** 시스템은 PostBindService로 필요한 서비스만 받는다. 씬 오브젝트가 정적 싱글턴으로 서비스를 찾지 않는다.
4. **테스트할 수 있다:** 판단이 일반 C#에 있으므로 결과 확인 순서·입력 대기 같은 규칙을 Unity 없이 EditMode 테스트로 검증한다.

한 줄로: **씬에는 보이는 것만 미리 올려 두고, 판단은 게임 시작 때 만들어 둔 시스템이 씬에 들어올 때 붙어서 한다.**

## 1. 구조

```text
[Boot 씬 → DontDestroyOnLoad]
GameBootstrap ── 소유 ──► FrameworkRuntime (게임 전체에 하나)
  (생성·수명 단계·매 프레임 Tick·종료만)   ├─ Buffer / Global(로딩·입력·Job…)
                                             ├─ InGame: Core 시스템, View 시스템   ← 씬에 붙는 시스템은 여기
                                             └─ LifeCycle: Stage, Combat, …        ← 구간의 Flow는 여기

[씬 A 로드] 로딩 시스템이 "씬 번호 A"에 등록된 Enter 처리를 priority 순서로 실행
   priority 0   : 해당 LifeCycle.OnEnter (Flow 시작)
   priority 100 : 해당 View 시스템 Attach (씬의 SceneRoot를 한 번 찾음)
[씬 A 떠남] "씬 번호 A"의 Exit 처리
   priority -100: View 시스템 Detach (씬 참조를 놓음)
   priority 0   : LifeCycle.OnExit (Flow 정리)
```

| 구성 | 하는 일 | 하지 않는 일 |
|---|---|---|
| `GameBootstrap` | 런타임 생성·수명 단계·시작 전환 1회·매 프레임 Tick·오류 시 정지·종료(설계 §10.0 B-1~B-8) | 서비스 찾기 창구, 씬 연결, 구간 전환, 규칙·표시 |
| LifeCycle(`StageLifeCycle` 등) | 구간의 Flow 시작·정리. `LifeCycleBase`가 씬 번호의 Enter/Exit 등록을 대신 해 준다 | 화면 그리기 |
| 씬에 붙는 시스템(View 시스템) | 서비스 받기(PostBindService) → 씬 번호에 Enter/Exit 등록(Initialize) → 씬에 들어오면 SceneRoot 연결 → Tick에서 표시 | 씬 오브젝트를 Tick마다 찾기, Core 소유 Buffer 쓰기 |
| SceneRoot(MonoBehaviour) | 씬 오브젝트 참조 묶음, 저작 검증 | 동작, 런타임·Bootstrap 찾기 |

씬 번호는 `SceneIndices` 한 곳의 상수를 쓴다: Boot 0, Stage(`StageSelect.unity`) 1, Combat 2, Lobby 3·Artifact 4(예약).

## 2. 예시 코드

스테이지 맵 씬에 붙는 View 시스템을 예로 든다. **예시이며 저장소에 넣은 코드가 아니다.** 스테이지 맵은 다른 작업 영역이므로 실제 이름·위치는 그 담당자가 정한다. 아래 두 파일은 2026-10-02에 `ProjectL.Systems`와 함께 MSBuild로 컴파일해 오류·새 경고가 없음을 확인했다(Unity 실행은 아님).

### 2.1 SceneRoot — 씬의 참조 묶음

MonoBehaviour는 **파일 이름 = 클래스 이름**이어야 씬에 붙일 수 있다. 파일 하나에 하나만 둔다.

```csharp
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>스테이지 맵 씬의 참조 묶음입니다. 동작은 없고 런타임 쪽 View 시스템이 찾아서 씁니다.</summary>
    [DisallowMultipleComponent]
    public sealed class StageMapSceneRoot : MonoBehaviour
    {
        [SerializeField] private Transform mapRoot;

        internal Transform MapRoot => mapRoot;
    }
}
```

### 2.2 씬에 붙는 시스템

```csharp
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 씬에 붙었다 떨어지는 View 시스템 예시입니다.
    /// 런타임에 한 번 등록되고, 씬이 로드될 때마다 씬의 SceneRoot를 한 번 찾아 연결합니다.
    /// </summary>
    internal sealed class StageMapViewSystem :
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        // LifeCycle의 Enter(0) 뒤에 연결하고, LifeCycle의 Exit(0) 전에 정리한다.
        private const int EnterPriority = 100;
        private const int ExitPriority = -100;

        private ILodingRegisterService _loadingRegister;
        private StageMapSceneRoot _root;

        // 1) 런타임 시작 때 한 번: 필요한 서비스를 받아 둔다(정적 찾기 없음).
        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _loadingRegister = resolver.GetService(SystemGroupsService.Global)
                .LodingServiceCommand
                .LodingRegisterService;
            // Buffer 읽기 싱크, 입력 슬롯 등도 여기서 받아 필드에 보관한다.
        }

        // 2) 런타임 시작 때 한 번: "내 씬이 로드되면 / 떠나면" 할 일을 씬 번호에 등록한다.
        public void Initialize()
        {
            _loadingRegister.RegisterSceneEnterLoaded(SceneIndices.Stage, new LodingNode(EnterPriority, Attach));
            _loadingRegister.RegisterSceneExitLoaded(SceneIndices.Stage, new LodingNode(ExitPriority, Detach));
        }

        // 3) 매 프레임: 씬에 연결돼 있을 때만 일한다(Find·GetComponent·LINQ 금지).
        public void Tick(float deltaTime)
        {
            if (_root == null)
                return;

            // 예: Buffer 값을 읽어 _root 아래 표시를 갱신한다.
        }

        // 4) 런타임 종료: 씬 연결이 남아 있으면 정리한다(여러 번 불려도 안전하게).
        public void Shutdown()
        {
            Detach();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddModule<ISystemModule>(this);
        }

        // 씬 로드가 끝난 직후 한 번: 씬 오브젝트를 찾는 유일한 자리다.
        private void Attach()
        {
            _root = UnityEngine.Object.FindFirstObjectByType<StageMapSceneRoot>();
            if (_root == null)
                throw new InvalidOperationException("[StageMapViewSystem] StageMapSceneRoot is missing in the stage map scene.");
        }

        // 씬을 떠나기 직전: 씬 오브젝트 참조를 놓는다. 다음 씬에 남은 참조를 쓰지 않게 한다.
        private void Detach()
        {
            _root = null;
        }
    }
}
```

### 2.3 런타임에 등록 — Factory 한 줄

`Build` 전에 등록한다(FW-003: Build 뒤 구조 변경 금지). View 시스템이면 `InGameViewSystemFactory`다.

```csharp
public static IExportSystemServices Create(
    CombatExecutionMode executionMode = CombatExecutionMode.Live,
    RuntimeFaultBoundary faultBoundary = null)
{
    SystemBuilderHelper builder = new();
    new StageMapViewSystem().ExportServices(builder);   // ← 추가
    return new InGameViewSystemGroup(builder.BuildSystemReader());
}
```

`InGameViewSystemGroup`이 등록된 모듈에 PostBindService·Initialize·Tick·Shutdown을 그대로 전달하므로 다른 연결은 필요 없다. 다른 시스템이 이 시스템을 호출해야 하면 `IXxx : IServiceModule` 인터페이스를 만들고 `sink.AddService<IXxx>(this)`도 한다(FW-001·002).

## 3. 한 번 실행했을 때의 순서

| 시점 | 호출 | 예시 시스템이 하는 일 |
|---|---|---|
| Boot 씬 시작 | Bootstrap: DB 생성 → 런타임 조립 | Factory에서 생성·등록 |
| | `BindScene` → `InitData` → `PostBindService` | 로딩 등록 서비스 등 받아 두기 |
| | `Initialize` | 씬 번호 1에 Enter(100)·Exit(-100) 등록 |
| | 시작 구간 `Change` → 씬 로드 시작 | — |
| 매 프레임 | Bootstrap: `runtime.Tick` | 연결 전이면 아무것도 안 함 |
| 씬 1 로드 완료 | 로딩: Enter 처리(0 → 100) | `StageLifeCycle.OnEnter` 뒤 `Attach`(SceneRoot 찾기) |
| 매 프레임 | `Tick` | 연결됐으면 표시 갱신 |
| 씬 1을 떠남 | 로딩: Exit 처리(-100 → 0) | `Detach` 뒤 `StageLifeCycle.OnExit` |
| 게임 종료 | Bootstrap: 입력 정리 → `Shutdown` 1회 | `Detach`(멱등) |

## 4. Boot 씬과 실행

- Build Settings: Boot 0 · StageSelect 1 · Combat 2. 옛 `Stage.unity`(옛 전투 씬)는 넣지 않는다.
- Boot 씬의 `GameBootstrap` 인스펙터: Database Catalog(`DataBaseSO/DatabaseCatalogSO.asset`), Start Life Cycle(Combat·Stage), Execution Mode(Live·RecordInputOnly).
- **런타임은 Boot 씬에서 시작해야 뜬다.** 다른 씬을 열어 둔 채 Play하면 런타임이 없다.
- **편의 토글:** 메뉴 `ProjectL > Play From Boot`를 켜면 어느 씬을 편집하다 Play해도 Build Settings 0번 씬(Boot)부터 시작하고, Play를 멈추면 편집하던 씬으로 돌아온다(`Tool/Editor/PlayFromBootToggle.cs`, `EditorSceneManager.playModeStartScene`).
  - 상태는 **내 PC의 EditorPrefs에만** 저장되고 기본값은 꺼짐이다. 다른 작업자의 Play(HMUTest·JMHTest 등)는 바뀌지 않는다.
  - 시작 씬은 Build Settings에서 첫 번째로 켜진 씬이다. Build Settings를 바꾸면 자동으로 다시 적용된다.
  - 켜 둔 상태에서 Boot와 무관한 테스트 씬을 단독으로 돌리려면 토글을 끈다.

## 5. 하지 말 것

| 하지 말 것 | 이유 |
|---|---|
| `GameBootstrap`이나 런타임을 정적으로 찾기 | 서비스 찾기 창구가 아니다. 서비스는 PostBindService로 받는다 |
| `BindScene`에서 씬 오브젝트 찾기에 기대기 | `BindScene`은 Boot 씬에서 한 번만 불리고 씬 로드 뒤 다시 불리지 않는다(DR-001). 씬 로드 Enter 처리에서 찾는다 |
| Tick·입력 경로에서 `Find`·`GetComponent`·LINQ | Hot Path 금지. 찾기는 Attach 한 번 |
| Detach 뒤 씬 오브젝트 참조를 쥐고 있기 | 씬이 바뀌면 오브젝트가 사라진다(`LoadSceneMode.Single`) |
| 씬 번호를 숫자로 직접 쓰기 | `SceneIndices` 상수를 쓴다. 겹치면 다른 구간의 Enter/Exit가 함께 실행된다 |
| `LifeCycleManager.Change`를 아무 데서나 호출 | 구간 전환의 주인·정리 순서는 F2(골격 Phase 6)에서 정한다. 지금 전환은 Bootstrap의 시작 1회뿐이다 |
| View에서 Core 소유 Buffer 쓰기 | View가 쓰는 Buffer는 `CombatViewBuffer`뿐이다 |

## 6. 알려진 제약

- **씬 재바인딩:** `UISystem`처럼 `BindScene`에서 씬 UI를 찾는 시스템은 Boot 씬(빈 씬)만 보고 끝난다. 씬별 UI를 UISystem에 붙이려면 Framework 쪽 결정(DR-001)이 필요하다. 전투 HUD는 그래서 씬 Canvas로 둔다(설계 §13.3).
- **구간 전환:** Title → Stage → Combat → Stage의 전환 호출자는 아직 없다(F2).
- **Bootstrap의 형태:** Framework가 지정한 형태가 아니라 View 쪽 구현 결정이다(설계 §10.0). Framework 담당자 확인 대기 중이며, 계획이 생기면 그쪽에 맞춘다.
