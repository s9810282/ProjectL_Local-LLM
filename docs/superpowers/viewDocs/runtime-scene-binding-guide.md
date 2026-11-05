# 런타임과 씬 연결 안내 — 씬에 붙는 시스템 만들기

- 작성일: 2026-10-02
- 작성: View 작업(스테이지 내부 담당)
- 대상: 새 씬(스테이지 맵·타이틀 등)이나 씬 오브젝트를 다루는 시스템을 만드는 작업자
- 근거: [전투 View 설계](../specs/2026-09-30-combat-view-design.md) §10.0(Bootstrap 역할)·§11(씬 번호)·W2(씬 연결), Framework [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-016·DR-001
- 브랜치: `feature/Yonggeun/Stage` (View Phase B1 이후)
- 2026-10-13 갱신: 씬 번호, Enter/Exit 우선순위, Bootstrap이 하는 일, 전환 호출자, View가 쓰는 Buffer, Play From Boot 경로를 현재 코드에 맞췄다(사용자 요청).
- 2026-10-22 갱신: 전체 씬 Node 순서를 §1.1 표 한 곳에 모았다(전투 개선 설계 §7.2 E5). Bootstrap의 순서 숫자는 이름 있는 상수가 됐다. Run 종료 복귀 설명을 지금 코드에 맞췄다.
- 2026-10-23~24 갱신: Bootstrap·Combat 씬 시스템 Node 숫자의 원본을 코드 `RuntimeSystem/ErrorHandling/SceneNodeOrder.cs`로 옮기고 §1.1 표가 그것을 가리키게 했다(골격 점검 설계 K-4). Combat·Stage·Lobby가 섞인 Exit −100·Enter 100 줄을 나눴다.
- 2026-10-15 P6 갱신: 비전투 View의 System·Scope 분리와 로비 단일 모듈 연결을 반영했다. 구현·검증 이력은 [비전투 View 계획](../plans/2026-10-13-view-structure-plan-Hojin.md)의 P0~P6 실행 기록을 따른다.
- 2026-10-16 소스 재대조: System/Scope 책임, Enter 200의 글로벌 표시 호출과 일반 패배 복귀 정책을 확인했다. 이 문서 대조에서 Unity·테스트를 다시 실행하지 않았다.

## 0. 한 줄 요약

**GameBootstrap은 직접 쓰지 않는다.** 게임 전체에 런타임이 하나 떠 있고, 씬은 그 런타임에 **끼워졌다 빠지는 대상**이다. 씬에 붙는 시스템은 런타임에 한 번 등록해 두고, "내 씬이 로드되면 / 떠나면" 할 일을 **씬 번호에 등록**한다. 씬 오브젝트는 참조만 들고 있고, 시스템이 씬에 들어올 때 한 번 찾아 연결한다.

### 0.1 두 층: 시스템과 씬 오브젝트

| 층 | 무엇 | 언제 생기나 | 어디에 있나 |
|---|---|---|---|
| **시스템**(일반 C#) | 서비스 연결·씬 수명 등록·Tick 전달 (예: `CombatViewSystem`, `StageMapViewSystem`). 비전투 View의 표시·입력·결과 관찰은 연결마다 생성하는 Scope와 하위 객체가 맡는다 | **게임 시작 때 Boot에서 한 번** Factory가 만들어 런타임에 등록한다. 게임이 끝날 때까지 유지된다 | 런타임 안(씬 밖) |
| **씬 오브젝트**(MonoBehaviour) | SceneRoot, Presenter·View, Canvas, 프리팹 같은 **그리는 것과 참조** | **지금처럼 에디터에서 씬에 미리 올려 둔다** | 각 씬 |

씬에 들어갈 때 **시스템 자체를 새로 만들지 않고 연결(Attach)** 한다. 비전투 View의 씬 연결용 Scope는 이때 새로 만든다(§0.2).

```text
게임 시작(Boot)   : Factory가 StageMapViewSystem을 만들어 등록 → "씬 2(`SceneIndices.Stage`) 들어오면 Attach" 예약
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

### 0.2 비전투 View의 씬 연결 수명

로비 Enter 100에는 **`LobbyViewSystem` 하나만** 붙는다. 이 System이 `LobbySceneRoot`·`ArtifactSceneRoot`를 진입 때 한 번 찾고 `LobbyViewScope`를 생성·게시한 뒤 `Start`한다. 덱과 아티팩트는 그 Scope가 소유하는 팝업이며, 별도 `ArtifactViewSystem`은 없다(`SceneIndices.Artifact` 4는 예약 유지). 스테이지 맵도 `StageMapViewSystem`이 `StageMapViewScope`를 소유한다. System은 서비스 연결·Enter/Exit 등록·Tick 위임·StopInput을 맡고, Scope는 씬 연결 한 번의 하위 객체·입력·표현을 소유한다. Scope는 Framework 모듈로 등록하지 않는다. 새 Scope를 외부 호출 전에 게시하고, 정리 중 새 연결이 생기면 옛 콜백이 새 UI·Layer를 건드리지 않도록 현재 소유자를 검사한다. Exit -100·StopInput에서 Scope를 폐기하며, 로비 카메라는 마지막에 Handle 경로로 반납한다. 팝업 수명은 공용 `PopupViewScope`가 맡고 외부 숨김·열림의 입력 전환은 다음 Tick에 처리한다. 세부 계약의 원본은 [비전투 View 설계 §3.3](../specs/2026-10-13-view-structure-design-Hojin.md#23-수명-규칙)·[§4.2](../specs/2026-10-13-view-structure-design-Hojin.md#42-popupviewscope)다.

### 0.3 새 View 기능의 구조 선택

호진의 새 View 작업에서 System·Scope·Factory·Controller의 책임, 별도 Scope·Factory를 만드는 판단 기준과 현재 코드 예시는 [View 기능 개발 가이드](view-development-guide-Hojin.md)를 참고한다. 이 문서는 씬 연결·등록·실행 순서를 설명한다.

현재 StageMap의 상점·회복 Controller와 이동 소유자는 `StageMapViewScope` 하나 아래에 있다. 회복 요청 번호·결과 소비/ack는 씬 밖 `RecoveryRequestLifetime`이 소유한다. 2026-10-16의 Akimbo 표시 이동·전투 전 표시 ticket/lease는 [노드 이동 설계](../specs/2026-10-10-stage-map-design-Hojin.md#33-노드-이동-nm-사용자-2026-10-16)와 [NM 계획](../plans/2026-10-10-stage-map-plan-Hojin.md#37-노드-이동)이 원본이며 별도 View System이나 두 번째 맵 Scope를 만들지 않는다.

## 1. 구조

```text
[Boot 씬 → DontDestroyOnLoad]
GameBootstrap ── 소유 ──► FrameworkRuntime (게임 전체에 하나)
  (생성·수명 단계·매 프레임 Tick·종료만)   ├─ Buffer / Global(로딩·입력·Job…)
                                             ├─ InGame: Core 시스템, View 시스템   ← 씬에 붙는 시스템은 여기
                                             └─ LifeCycle: Stage, Combat, …        ← 구간의 Flow는 여기

[씬 A 로드] 로딩 시스템이 "씬 번호 A"에 등록된 Enter 처리를 priority 순서로 실행
   UI 다시 연결(Bootstrap) → (Combat만) SceneRoot 찾기·배경 맵 세우기 → 카메라 다시 연결(Bootstrap) → LifeCycle.OnEnter → View Attach → 화면 열기(Bootstrap)
[씬 A 떠남] "씬 번호 A"의 Exit 처리
   이탈 준비 검사(LifeCycle) → View Detach(Combat은 배경 맵도 없앰) → LifeCycle.OnExit
   (정확한 priority와 씬별 등록은 아래 §1.1 표에 모아 두었다. Bootstrap·Combat 씬 시스템의 숫자 원본은 코드 SceneNodeOrder다)
```

### 1.1 전체 씬 Node 순서 표

같은 단계의 Node는 priority가 작은 것부터, 같은 priority면 등록 순서대로 돈다(로딩 Node 큐는 Global JobRunner와 별개다). 숫자의 원본은 오른쪽 칸의 상수다. **Bootstrap과 Combat 씬 시스템(전투 View·튜토리얼)의 숫자 원본은 코드 `RuntimeSystem/Common/SceneNodeOrder.cs`** 한 곳이다. 기준 값은 숫자로, 그에 붙는 값은 식으로 적혀 있다(골격 점검 설계 K-4). Stage·Lobby 시스템의 자기 Node는 각 시스템의 상수다. 새 Node를 더할 때는 숫자 원본을 고치고 이 표도 함께 고친다.

| 단계 | priority | 씬 | 누가 | 하는 일 | 숫자의 원본 |
|---|---|---|---|---|---|
| Exit | -200 | Stage·Lobby | `StageLifeCycle`·`LobbyLifeCycle` | 이탈 준비 검사(로비: 실제 편집 결과·Runner Idle) | 각 LifeCycle의 `ExitPreparationPriority` |
| Exit | -110 | Combat | `TutorialViewSystem` | 튜토리얼 표시 해제 | `SceneNodeOrder.TutorialDetach`(= `CombatViewRelease` − 10) |
| Exit | -100 | Combat | `CombatViewSystem` | `ReleaseScene` = View Detach(씬 참조를 놓음) 뒤 배경 맵도 없앰 | `SceneNodeOrder.CombatViewRelease`(= `LifeCycle` − 100) |
| Exit | -100 | Stage·Lobby | `StageMapViewSystem`·`LobbyViewSystem` | View Detach(씬 참조를 놓음) | 각 시스템의 `DetachPriority` |
| Exit | 0 | 각 구간 | `LifeCycleBase` | `OnExit`(Flow 정리, 전투는 정상 이탈이면 진입 요청을 닫음) | `LifeCycleBase`의 숫자 0(기반) |
| Enter | -200 | Stage·Combat·Lobby | `GameBootstrap` | UISystem `BindScene` 다시 부름 | `SceneNodeOrder.BindSceneUI` |
| Enter | -195 | Combat | `CombatViewSystem` | SceneRoot 찾기·배경 맵 세우기(카메라 다시 연결보다 먼저) | `SceneNodeOrder.CombatBackdrop`(= `BindCameras` − 5) |
| Enter | -190 | Stage·Combat·Lobby | `GameBootstrap` | CameraGroup `BindScene` 다시 부름 | `SceneNodeOrder.BindCameras` |
| Enter | 0 | 각 구간 | `LifeCycleBase` | `OnEnter`(Flow 시작, 전투는 수락된 진입 활성화) | `LifeCycleBase`의 숫자 0(기반) |
| Enter | 90 | Combat | `GameBootstrap` | 덱 카드 부모 준비(`OpenUI(Deck,true)`) | `SceneNodeOrder.PrepareCombatDeck`(= `CombatViewAttach` − 10) |
| Enter | 100 | Combat | `CombatViewSystem` | Attach(-195에서 찾은 SceneRoot를 씀) | `SceneNodeOrder.CombatViewAttach`(= `LifeCycle` + 100) |
| Enter | 100 | Stage·Lobby | `StageMapViewSystem`·`LobbyViewSystem` | Attach(SceneRoot를 한 번 찾음) | 각 시스템의 `AttachPriority` |
| Enter | 110 | Combat | `TutorialViewSystem` | 튜토리얼 표시 연결 | `SceneNodeOrder.TutorialAttach`(= `CombatViewAttach` + 10) |
| Enter | 200 | Stage·Combat·Lobby | `GameBootstrap` | 화면 열기(Lobby: `ShowLobby`·`OpenUI(Clicker)`, Stage: `ShowStageSelect`·`OpenUI(StageSelect)`, Combat: `ShowCombat`·CombatLog·NightChecker·Deck·UnitCard) | `SceneNodeOrder.OpenSceneUI` |
| AfterLoad | 10 | 모든 씬 | `CameraCatalogSystem` | 카메라 목록 연결 | Framework |
| AfterLoad | 100 | 모든 씬(자기 Scope일 때만 일함) | `StageMapViewSystem`·`LobbyViewSystem` | 카메라 핸들 연결 | `CameraHandlePriority` |

- **같은 프레임의 Runner 순서(의도된 규약):** 로딩 Node와 별개로, 매 프레임 그룹 Tick 순서(Buffer → Global → InGame → LifeCycle)가 공용 Job Runner를 누가 먼저 잡는지 정한다. 튜토리얼(`TutorialViewSystem`, InGame Tick)은 Runner가 비었으면 같은 프레임의 전투(`CombatLifeCycle`, LifeCycle Tick)보다 먼저 자기 판단을 건다. 둘 다 할 일이 있으면 튜토리얼 Job이 먼저 돌고 전투는 Runner가 다시 빈 다음 프레임에 건다. 동작을 바꾸지 않는 규약이며, 새 루트를 더할 때도 "Runner가 비면 내 일을 건다"는 이 순서를 따른다(전투 개선 설계 §7.3 F6).
- Bootstrap의 씬별 Node는 `GameBootstrap.BindUILifeCycle`의 표 하나에 있다. UI·카메라 다시 연결은 Framework가 씬마다 다시 연결하는 수단을 서비스 계약으로 주지 않아 그룹 모듈로 캐스트해 부른다(그 캐스트 둘도 같은 자리에만 있다).
- **Combat 배경 맵을 -195에 세우는 이유(2026-10-23, 카메라 요구 2026-11-02 갱신):** 맵 프리팹 또는 맵 SO 안의 버추얼 카메라를 카메라 줌이 이름으로 카메라 목록에서 찾는다. 맵이 -190 카메라 수집보다 먼저 있어야 그 수집에 들어간다. 그래야 로딩 끝(AfterLoad)의 재수집에 기대지 않는다. 현재 `Day Camera`는 필수이며 `Night Camera`·`NightMap`은 필수가 아니다. 밤 화면 전환 설정은 제거했고 자동 단계 전환은 비활성 상태를 유지한다. 스테이지 키는 씬 로드 전 전투 준비가 Buffer에 게시하므로 이 시점에 읽을 수 있다. 설치 순서 원본은 [배경 설계](../specs/2026-10-23-combat-backdrop-before-camera-bind-design.md)(BD1~BD4), Night 설정 제거 기록은 [N1 계획](../plans/2026-11-02-combat-night-transition-settings-removal-plan.md)이다.

| 구성 | 하는 일 | 하지 않는 일 |
|---|---|---|
| `GameBootstrap` | 런타임 생성·수명 단계·시작 전환 1회·매 프레임 Tick·오류 시 정지·종료(설계 §10.0 B-1~B-8). 씬 로드마다 UI·카메라 다시 연결(Enter -200·-190). Enter 200에서 Lobby는 `ShowLobby`, Stage는 `ShowStageSelect` 뒤 `OpenUI(Deck,true)`, Combat은 `OpenUI(CombatLog,true)`·`OpenUI(Deck,true)` 호출 | 서비스 찾기 창구, View 시스템의 SceneRoot 연결, 시작 이후의 구간 전환, 규칙 |
| LifeCycle(`StageLifeCycle` 등) | 구간의 Flow 시작·정리. `LifeCycleBase`가 씬 번호의 Enter/Exit 등록을 대신 해 준다 | 화면 그리기 |
| 씬에 붙는 시스템(View 시스템) | 서비스 받기(PostBindService) → 씬 번호에 Enter/Exit 등록(Initialize) → SceneRoot 연결. 비전투 View는 Scope를 게시하고 Tick·정리를 위임 | 씬 오브젝트를 Tick마다 찾기, Core 소유 Buffer 쓰기 |
| SceneRoot(MonoBehaviour) | 씬 오브젝트 참조 묶음, 저작 검증 | 동작, 런타임·Bootstrap 찾기 |

씬 번호는 `SceneIndices` 한 곳의 상수를 쓴다: Boot 0, Lobby(`Lobby.unity`) 1, Stage(`StageSelect.unity`) 2, Combat 3, Artifact 4(예약)(2026-10-13 변경). 로비 공간·고정 카메라·Boot 연결은 [로비 계획 LV1 실행 기록](../plans/2026-10-11-lobby-plan-Hojin.md#31-lv1-공간고정-카메라boot-진입)을 따른다.

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
| | `Initialize` | 씬 번호 2(`SceneIndices.Stage`)에 Enter(100)·Exit(-100) 등록 |
| | 공통 보드 준비 Request → Tick·게시/Runner Idle → Ready → 시작 구간 요청 | 준비 중 표시 시스템은 연결 전 상태 유지 |
| 매 프레임 | Bootstrap: `runtime.Tick` | 연결 전이면 아무것도 안 함 |
| 씬 2 로드 완료 | 로딩: Enter 처리(-200 UI 연결 → -190 카메라 연결 → 0 → 100 → 200 글로벌 UI 열기) | `StageLifeCycle.OnEnter` 뒤 `Attach`(SceneRoot 찾기) |
| 매 프레임 | `Tick` | 연결됐으면 표시 갱신 |
| 씬 2를 떠남 | 로딩: 이탈 준비(-200) → Exit(-100 → 0) | 이탈 준비 검사를 통과한 뒤 `Detach`·`StageLifeCycle.OnExit` |
| 게임 종료 | Bootstrap: 입력 정리 → `Shutdown` 1회 | `Detach`(멱등) |


**LV5 현재 진입 경계:** GameBootstrap은 Initialize 뒤 공통 ArtifactBoardPreparation을 Request하고 Runtime Tick을 계속 구동한다. 보드 게시·독립 준비 Job 배출 후 IsReady가 되면 개발 진행 준비와 최초 구간 진입을 한 번 요청한다. Lobby는 NewRun 없이 보드만 준비한다. 운영 Build Settings는 Boot(0)·Lobby(1)·StageSelect(2)·Combat(3)이며 기본 시작은 Lobby다. 아래 Stage 예시는 씬 연결 방식의 예시이며 공통 준비를 생략하는 운영 지침이 아니다.

## 4. Boot 씬과 실행

- Build Settings: Boot 0 · Lobby 1 · StageSelect 2 · Combat 3(2026-10-13 변경). 옛 `Stage.unity`(옛 전투 씬)는 넣지 않는다.
- Boot 씬의 `GameBootstrap` 인스펙터: Database Catalog(`DataBaseSO/DatabaseCatalogSO.asset`), Start Life Cycle(Combat·Stage·Lobby, 기본 Lobby), Execution Mode(Live·RecordInputOnly).
- **런타임은 Boot 씬에서 시작해야 뜬다.** 다른 씬을 열어 둔 채 Play하면 런타임이 없다.
- **편의 토글:** 메뉴 `ProjectL > Play From Boot`를 켜면 어느 씬을 편집하다 Play해도 Build Settings 0번 씬(Boot)부터 시작하고, Play를 멈추면 편집하던 씬으로 돌아온다(`Assets/ProjectL/Editor/Boot/PlayFromBootToggle.cs`, `EditorSceneManager.playModeStartScene`).
  - 상태는 **내 PC의 EditorPrefs에만** 저장되고 기본값은 꺼짐이다. 다른 작업자의 Play(HMUTest·JMHTest 등)는 바뀌지 않는다.
  - 시작 씬은 Build Settings에서 첫 번째로 켜진 씬이다. Build Settings를 바꾸면 자동으로 다시 적용된다.
  - 켜 둔 상태에서 Boot와 무관한 테스트 씬을 단독으로 돌리려면 토글을 끈다.

## 5. 하지 말 것

| 하지 말 것 | 이유 |
|---|---|
| `GameBootstrap`이나 런타임을 정적으로 찾기 | 서비스 찾기 창구가 아니다. 서비스는 PostBindService로 받는다 |
| `BindScene`에서 씬 오브젝트 찾기에 기대기 | 런타임의 `BindScene`은 Boot 씬에서 한 번만 불린다(DR-001). 씬 로드마다 다시 불리는 것은 Bootstrap이 따로 부르는 UISystem·CameraGroup뿐이다(Enter -200·-190). View 시스템은 씬 로드 Enter 처리(100)에서 찾는다. 예외로 Combat은 배경 맵을 카메라 수집 전에 세우려고 -195에서 한 번 찾는다(찾는 곳은 여전히 한 곳) |
| Tick·입력 경로에서 `Find`·`GetComponent`·LINQ | Hot Path 금지. 찾기는 Attach 한 번 |
| Detach 뒤 씬 오브젝트 참조를 쥐고 있기 | 씬이 바뀌면 오브젝트가 사라진다(`LoadSceneMode.Single`) |
| 씬 번호를 숫자로 직접 쓰기 | `SceneIndices` 상수를 쓴다. 겹치면 다른 구간의 Enter/Exit가 함께 실행된다 |
| `LifeCycleManager.Change`를 아무 데서나 호출 | 지금 전환 호출자는 Bootstrap의 시작 1회(`GameBootstrap`), 로비 출발(`LobbyDepartureFlow` → Stage), 전투 진입(`CombatEntryTransitionJob` → Combat), 전투 이탈(`CombatExitJobs` → 목적지)이다. 새 전환은 이 주인들과 정리 순서를 맞춘다 |
| View에서 Core 소유 Buffer 쓰기 | View는 자기 입력 Buffer(`CombatViewBuffer`·`StageViewBuffer`·`ShopViewBuffer`·`ArtifactViewBuffer`·`RecoveryViewBuffer`·`LobbyViewBuffer`)의 WriteSink만 쓴다. `IBufferQuery`가 모든 싱크를 보여 주므로 이 경계는 관례로 지킨다 |

## 6. 알려진 제약

- **씬 재바인딩:** 런타임 `BindScene`은 Boot 씬에서 한 번만 불린다(DR-001). 대신 Bootstrap이 Stage·Combat·Lobby 씬 로드마다 UISystem과 CameraGroup의 `BindScene`을 다시 부른다(Enter -200·-190, `GameBootstrap`). 이것은 View 쪽 Bootstrap 결정이며 Framework 계약이 바뀐 것은 아니다. 전투 HUD는 씬 Canvas로 둔다(설계 §13.3).
- **구간 전환:** Boot → Lobby(시작) → Stage(로비 출발) → Combat(노드 진입) → Stage(일반 승리) 전환은 연결됐다. Title 씬은 없으며(2026-10-13 삭제) Boot가 시작 화면 역할을 한다. 2026-10-22 현재 **Run이 끝나면(일반 패배·자발적 포기·최종 승리) Lobby**이고, 새 Run은 로비 출발이 만든다(전투 쪽 새 Run은 2026-10-22에 걷었다, [전투 개선 설계 §4](../specs/2026-10-22-combat-view-structure-improvement-design.md)). 정책 원본은 [전투 Job·Plan 설계 J7](../specs/2026-10-10-combat-job-plan-restructure-design.md#j7-이탈-연쇄-k21-유지)이며 검증·남은 범위는 [로비 계획 LV10](../plans/2026-10-11-lobby-plan-Hojin.md#311-lv10-run-종료-뒤-로비-복귀)을 따른다. 전체 Run 종료 로비 복귀와 새 Run 생성 위치 통합은 완료로 표시하지 않는다.
- **Bootstrap의 형태:** Framework가 지정한 형태가 아니라 View 쪽 구현 결정이다(설계 §10.0). Framework 담당자 확인 대기 중이며, 계획이 생기면 그쪽에 맞춘다.
