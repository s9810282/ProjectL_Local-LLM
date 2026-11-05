# 스텐실 화면 전환 사용법

2026-10-18. 카메라·Feature 코드 연결 완료, 실제 Unity 화면 확인은 대기 중이다.

## 시스템 자동 초기화

호출자는 Init을 호출하지 않는다. RenderSystemFactory가 C# ScreenTransitionInitModule을 주입하고 ScreenTransitionSystem이 Framework의 Initialize 단계에서 한 번 읽는다. 공개 서비스에는 CreateScop만 있다. 내부 설정 Data도 외부 API에서 제거했다.

설정 원본은 `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/ScreenTransitionSystem/ScreenTransitionInitModule.cs` 한 곳이다. 기본값은 다음과 같다.

| 항목 | 값 |
|---|---|
| Main 오브젝트 이름 | Main Camera |
| 고정 레이어 | Default, BothRendering, DayCameraRender, NightCameraRender |
| 원 중심 (월드 XZ) | (0, 0) |
| 경계 폭 | 5.48 (기존 PC_Renderer 설정 기준) |

AfterRendering·BefalRendering은 요청으로 바뀌는 레이어이므로 고정 마스크에 포함하지 않는다. 레이어 이름은 TagManager에서 찾으며 누락되면 초기화 오류로 알린다. 항상 포함할 레이어·카메라 이름·원 설정을 바꿀 때는 모듈을 수정한다. 씬 설정 컴포넌트나 호출자 초기화 코드는 추가하지 않는다.

타입의 네임스페이스는 `ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem`이다. 시스템 초기화에서는 설정만 저장하고 카메라 Handle은 Play에서 확보한다. Play는 Main의 기존 마스크에 고정 레이어를 포함시키고, 완료 마스크는 항상 `FixedLayer | data.Layer`로 계산한다. Cinemachine Brain이 버추얼 카메라를 볼 수 있도록 두 버추얼 카메라 레이어도 고정 마스크에 포함했다.

Main은 활성 GameObject의 enabled 카메라여야 한다. 카메라 오브젝트는 외부에서 준비하고, Unity 카탈로그가 최초 등록할 때 출력 카메라의 최상위 루트를 DontDestroyOnLoad로 처리한다. 서비스는 카메라를 새로 만들지 않는다.

StencilBlendingFeature의 기존 TargetRenderTexture·ComputeShader·Line Presets를 설정해야 한다. 2026-11-02부터 화면 전환 시스템은 RenderTexture용 렌더 카메라를 조회하거나 제어하지 않는다. TargetRenderTexture에 그릴 카메라의 타깃·마스크·렌더 순서·활성 상태는 외부 구성에서 관리한다.

## 요청과 반환

```csharp
var transition = global.RenderServiceCommand.ViewGroup.ScreenTransitionSystemServices;
IScreenTransitionScop scop = transition.CreateScop();
scop.Play(new ScreenTransitionData
{
    Layer = targetLayers,
    TargetVirtureCamera = null,
    StencileMaskeColor = Color.cyan,
    ChangeSpeed = 3f,
    TargetChangeAmount = 30f
});
```

System이 Pool에서 Scop 객체를 꺼내고, Struct를 받은 Scop이 내부 서비스로 자신의 활성화를 알린다. 진행 중에는 호출자가 소유 참조를 유지한다. 사용 수명이 끝나는 지점에서 `scop.Dispose()`한다. 즉시 Dispose하면 진행이 취소된다. 반환한 참조는 다시 사용하지 않는다.

같은 Scop에 Play를 다시 호출하면 기존 요청을 정리하고 새 Data로 0부터 시작한다. 입력 검증에 실패한 요청은 기존 요청을 유지한다. 실행 준비 중 하위 자원 확보에 실패하면 진행을 멈추고 확보한 자원을 정리한다.

`TargetVirtureCamera`가 null이면 현재 시점을 유지하고 버추얼 카메라를 조회하거나 Priority를 변경하지 않는다. 2026-11-02부터 전투의 밤 전환은 이 방식으로 요청하며 별도 Night 카메라를 요구하지 않는다. 이름을 지정하면 목표 버추얼 카메라 하나만 현재 최고 Priority보다 높인다. 다른 카메라의 Priority는 건드리지 않는다. 최고값이 int.MaxValue면 같은 값으로 올린 뒤 Cinemachine의 동순위 선택 순서를 맨 위로 이동한다. 지정한 대상 버추얼 카메라는 활성 상태여야 한다. 구도·블렌딩 계산은 Brain이 수행한다.

## 완료와 취소

- 진행량을 스텐실 월드 XZ 원 반경에 직접 적용한다. `ChangeSpeed`는 초당 반경 증가량이며 중심·경계 폭은 Init 설정이다.
- 목표 반경에 도달하면 마지막 반경의 스텐실 합성 패스가 기록될 때까지 기다린다. 그다음 Tick에서 Main 컬링을 `FixedLayer | data.Layer`로 확정하고 스텐실 사용을 해제한다. 이때 `IsCompleted`가 true가 된다. 렌더가 발생하지 않거나 패스가 건너뛰어지면 완료되지 않는다.
- 완료 뒤에도 Scop 객체와 출력 사용 소유권은 Dispose 전까지 유지한다. 다른 Scop의 Play는 기존 소유자를 Dispose하기 전까지 거부한다. 같은 Scop의 재요청은 가능하다.
- 완료 후 Dispose는 확정된 Main 마스크와 선택한 버추얼 Priority를 유지한다. 임시 스텐실 설정과 Handle/Feature Scope를 정리하고 Scop 자체를 Pool로 반환한다.
- 완료 전 Dispose는 Main의 저장된 설정과 대상 버추얼 카메라의 이전 Priority를 복원한다. 경계선 색은 런타임 덮어쓰기만 사용하며 Line Presets 원본을 수정하지 않는다.

현재의 반경 직접 적용과 동시 요청 거부는 구현 기본값이다. 질문에 대한 사용자 선택이 도착하면 해당 정책을 조정한다. 사용자가 확정한 사항은 대상 Priority 하나만 상승, Init 고정 레이어, Struct 입력, 목표량 완료, 호출자 Dispose 반환이다.

## Unity 확인 항목

현재 셰이더는 Main의 표면 깊이에서 월드 XZ 원을 계산하며, 표면 깊이가 없는 배경은 원본을 유지한다. `TargetChangeAmount`는 원하는 표면 영역을 덮도록 지정한다. 화면 픽셀 전체가 바뀌었는지 자동 판정하는 API는 아니다.

실제 씬에서는 동일 구도·버추얼 카메라 레이어·RT 렌더 순서·원 반경·마지막 프레임 경계를 확인한다. 가짜 카메라/Feature 테스트 통과와 실제 GPU 화면 검증은 구분한다.

## 전투 배치·전투 자동 전환

2026-11-02 현재 `CombatPhaseTransitionPresenter.TryBegin`의 자동 화면 전환 재생은 비활성이다. 전투 진입·밤 종료의 기존 BGM과 사건 처리는 일반 재생 경로로 처리한다.

사용자 요청으로 운영 전투의 밤 전환 설정(레이어·카메라·색), 밤 요청 생성과 맵 안 `Night Camera` 존재 검사를 제거했다. `Night Camera`·`NightMap`이 없는 맵도 사용할 수 있다. `Day Camera` 존재 검사와 View DB의 아침 설정(`morningTransitionLayer`·`morningTransitionCamera`·`morningTransitionColor`, 공통 속도·양)은 유지한다. 자동 전환을 다시 켠 것은 아니다.

맵은 카메라 수집보다 먼저 설치돼야 이동·줌·흔들림의 카메라 조회에 포함된다. 공용 ScreenTransitionSystem과 아래 JMHTest 수동 테스트는 기존 계약을 유지한다. `NightCameraRender`는 공용 Render가 사용하는 프로젝트 레이어이며 맵의 Night 카메라 필수 요구와는 별개다.

최신 변경·검증은 [Night 설정 제거 N1 기록](superpowers/plans/2026-11-02-combat-night-transition-settings-removal-plan.md), 과거 양방향 자동 전환 연결은 [2026-10-18 설계·기록](superpowers/specs/2026-10-18-combat-phase-stencil-transition-design.md)을 따른다.

## JMHTest 수동 테스트

`Assets/Scenes/JMHTest.unity`의 TestRuner 오브젝트에서 실행한다. 기존 After/Before 환경·카메라·RT를 사용한다. 기존 이동·줌·흔들림 테스트 코드와 Inspector 설정은 사용자 요청으로 제거했고, AreaOutline 테스트는 이 씬에서 끈다. 사용자는 Init을 호출하지 않는다.

배경 FBX의 불필요한 Camera 때문에 이름 중복이 발생한 것을 확인하여, 이 씬이 사용하는 rock_test_002zz·smallrock·TESTMAP·ROCK1fbx·rock1_test·rock2의 Import Cameras를 껐다. 에디터 미리보기 필터와 별개인 모델 임포트 설정이며 해당 FBX를 사용하는 모든 인스턴스에 적용된다. Unity 재임포트가 끝난 뒤 Play한다.

1. JMHTest 씬을 열고 Play한다. 다른 씬으로 시작되면 `ProjectL > Play From Boot`를 끄고 다시 실행한다.
2. Game 뷰 오른쪽 패널에서 `Play Before (BefalRendering) -> Night Camera`를 누른다. 반대 방향은 `Play AfterRendering -> Day Camera`다. 프로젝트의 비포 레이어 실제 이름은 `BefalRendering`이다.
3. 경계선 색·속도·목표 반경은 Runner의 `Screen Transition Tests: After / Before` Inspector 항목에서 바꾸고 Play 또는 `Restart same Scop`으로 적용한다. 기본값은 After=cyan, Before=magenta, 속도 12, 목표 반경 100이다.
4. 진행량과 두 카메라의 컬링 마스크·버추얼 Priority를 확인한다. 목표 반경 뒤 마지막 스텐실 패스를 기다린 다음 `PASS: completed`가 표시된다. PASS는 Main의 대상 레이어 포함·반대 레이어 제외 검사이며 실제 화면 확인은 별도로 한다.
5. 진행 중 `Dispose Scop`은 취소·복원, 완료 후에는 확정된 Main 마스크를 유지하며 반환한다. 반환 후 `Rent Scop only / check pool reuse`를 누르면 같은 Pool 객체를 받았는지 표시한다. `Restart same Scop`은 대여를 유지한 채 진행량 0부터 재생한다.
6. `Reset test environment`는 Scop을 반환하고 실행 시작 때의 Main/Render 설정과 Day/Night Priority를 복원한다. Runner 종료 때도 같은 정리 경로를 사용한다.

구현은 `TestRuner.ScreenTransitionTests.cs`에 분리했다. 기존 Runner의 IMGUI 방식을 사용하며 별도 Canvas·입력 이벤트 등록은 추가하지 않았다. 코드 빌드와 관리 테스트는 확인했고, 실제 Unity Play 화면 확인은 대기 중이다.
