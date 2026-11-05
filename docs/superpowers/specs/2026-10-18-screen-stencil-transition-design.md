# 스텐실 화면 전환 상위 연출 — 설계·Task

2026-10-18. T1a·T1b·T2·T3 자동 초기화·T4 수동 테스트 환경과 T5 기존 카메라 테스트 정리를 구현했다. 공개 Init은 제거했으며 시스템이 C# 초기화 모듈을 읽는다. 실제 Unity 화면·GPU 검증은 대기 중이다. 사용법은 [화면 전환 사용법](../../render-screen-transition.md)을 참고한다.

## 사용자 결정

- AreaOutline처럼 Render의 상위 연출 기능으로 분리한다.
- Unity Camera 카탈로그, 버추얼 Camera 카탈로그, RenderFeatureSystem을 사용한다. 카메라는 GameObject 이름으로 조회한다.
- 호출자가 전환할 버추얼 카메라와 대상 레이어를 전달한다.
- 스텐실로 목표 화면으로 전환하고, 전환이 완료된 뒤 Main Camera의 컬링을 목표 레이어로 변경한다.
- 항상 포함할 고정 레이어가 있다. Init 때 한 번 저장하고 모든 전환에서 유지한다. 이 결정은 앞선 '요청 마스크로 완전 교체'를 보완한다.
- Main Camera는 시작부터 DontDestroyOnLoad로 재사용하며 Render Camera는 후처리용이다. Camera Handle은 Pool에서 객체 자체를 대여·반환한다.
- 상위 연출도 Scop 객체 자체를 Pool에서 대여·반환한다. 연출 완료 시 자동 반환하지 않으며 호출자가 Dispose할 때 정리 후 그 Scop을 반환한다.
- 전환 Data는 Layer, TargetVirtureCamera, StencileMaskeColor, ChangeSpeed와 목표 변화량을 가진다. TargetVirtureCamera·StencileMaskeColor의 철자는 사용자 지정 이름을 유지한다.
- StencileMaskeColor는 스텐실 원의 경계선 색이다. ChangeSpeed는 초당 전환 진행량의 증가 속도다. 목표 변화량에 도달하면 완료한다.
- RenderSystemGroup에 Tick 전달을 추가한다. ScreenTransitionSystem은 Scop 전용 내부 서비스를 제공하고, Scop이 자신의 활성화·비활성화를 알리면 활성 Scop만 Tick한다.
- System은 Pool에서 Scop을 꺼내 반환한다. 사용자는 그 Scop에 Struct를 전달하고 Scop이 System에 자신의 활성화를 알린다. 같은 Scop에 Play(data)를 다시 호출하면 새 Data로 진행량을 0부터 다시 시작한다.
- 버추얼 카메라는 대상 하나의 Priority만 올린다. 다른 카메라의 Priority는 변경하지 않는다. Brain이 구도와 블렌딩을 계산한다. 앞서 검토한 출력별 별도 버추얼 구도는 사용하지 않는다.
- 초기화는 시스템이 모듈을 불러와 처리한다. 사용자 선택에 따라 설정은 C# 초기화 모듈에 미리 정의하며, 호출자는 Init 없이 대여한 Scop에 Play만 호출한다. 기존 대여·Dispose 책임은 유지한다.

## 책임

| 담당 | 책임 |
|---|---|
| Factory | 서비스 생성과 의존성·초기 설정 연결 |
| CameraGroup / Render 그룹 | 서비스 노출과 모듈 수명 전달. RenderSystemGroup은 자식 TickModules에도 Tick 전달 |
| UnityCameraCatalogSystem | Unity Camera 이름 조회, Handle 대여/반환, 컬링·RenderTexture·enabled 조작 |
| CameraCatalogSystem | CinemachineVirtualCamera 이름 조회, Handle 대여/반환, 버추얼 구도 설정 |
| RenderFeatureSystem | Feature Scope 사용·반환과 활성 참조 수 |
| ScreenTransitionSystem | 전환 Scop Pool, 대여·활성 목록과 Tick 호출 관리. PostBind에서 필요한 서비스 연결 |
| ScreenTransitionScop | 요청 Data·진행량·완료 상태, 활성화 통지와 Dispose 반환. 출력 객체에 구체 적용 위임 |
| ScreenTransitionOutputProvider | Init 설정 한 곳과 하나인 Main 출력의 사용 소유권 |
| ScreenTransitionOutput | Scop과 함께 재사용. Camera Handle·Feature Scope 소유, 반경 적용·최종 컬링 확정·복원과 정리 |
| 호출 View | 전환 시점과 목표 버추얼 카메라·대상 레이어 결정 |

상위 연출은 `Systems/RuntimeSystem/GlobalSystem/RenderSystem/View`에 둔다. 카탈로그/Pool에 전환 절차를 넣지 않고, Core의 국면 판정도 복제하지 않는다. 이번 설계에서 Core·FieldCamera·Architecture·운영 씬을 수정하지 않는다.

## 입력과 컬링 원본

시스템의 Framework Initialize에서 C# ScreenTransitionInitModule.Load를 한 번 호출하고 내부 Provider.Init에 설정을 저장한다. 공개 Init과 외부 초기화 Data는 제거했다. 모듈이 MainCamera/RenderCamera 이름, FixedLayer, CircleCenter, TransitionWidth의 유일한 코드 원본이다. 설정을 읽을 때는 카메라를 대여하지 않고 Play에서 Handle 확보 후 Main 마스크에 고정 레이어를 포함한다. 이후 완료 마스크는 항상 저장한 고정 마스크와 요청 Layer만 조합한다. 전환 중 Main.cullingMask에서 고정 마스크를 추출하지 않는다.

모듈의 기본값은 Main Camera / RenderTexture Camera, 고정 레이어 Default·BothRendering·DayCameraRender·NightCameraRender, 원 중심 (0, 0), 폭 5.48이다. 폭·중심은 기존 PC_Renderer 설정을 기준으로 옮겼다. 고정 레이어는 TagManager 이름으로 조회하며 없는 이름은 오류다. 요청으로 바뀌는 AfterRendering·BefalRendering을 고정 마스크에 넣지 않는다. 두 Brain의 버추얼 후보 필터를 위해 두 버추얼 카메라 오브젝트 레이어를 포함한다.

전환 입력은 다음 Data 형식으로 구현했다. 목표 변화량의 필드 이름은 `TargetChangeAmount`다. 현재 진행량은 요청 Data를 수정하는 대신 대여된 Scop 내부에서 관리한다.

```csharp
public struct ScreenTransitionData
{
    public LayerMask Layer;
    public string TargetVirtureCamera;
    public Color StencileMaskeColor;
    public float ChangeSpeed;
    public float TargetChangeAmount;
}
```

Layer는 최종 컬링에 포함할 대상 마스크, TargetVirtureCamera는 카메라 오브젝트 이름, StencileMaskeColor는 원의 경계선 색이다. ChangeSpeed와 TargetChangeAmount는 같은 진행량 단위를 사용한다. Play에서 빈 카메라 이름, 유한한 양수가 아닌 속도·목표 변화량, 유한하지 않은 색 성분을 거부한다. 검증 실패 시 기존 요청과 진행량은 보존한다. 진행량의 스텐실 월드 반경 매핑과 실제 전체 화면을 덮는 목표 조건은 T0에서 확정한다. 고정 완료값 1이나 고정 연출 시간을 완료 조건으로 넣지 않는다. 속도와 목표값이 정해지면 소요 시간은 목표 변화량 / ChangeSpeed에서 파생되므로 별도 Duration 원본은 만들지 않는다.

구현 API는 `IRenderViewServiceContext.ScreenTransitionSystemServices.CreateScop()`으로 Scop을 대여한 뒤 `scop.Play(data)`, 사용 종료 시 `scop.Dispose()`다. 호출자가 초기화할 API나 System이 요청 Data를 받는 외부 Play API는 두지 않는다. 같은 Scop에 재요청하면 기존 활성화를 먼저 해제하고 새 Data·진행량 0으로 다시 활성화한다. 메인/렌더 카메라 이름·fixedLayerMask는 C# 초기화 모듈에서 정하며 요청 Data에 중복해서 담지 않는다.

```csharp
IScreenTransitionScop scop = render.ViewGroup.ScreenTransitionSystemServices.CreateScop();
scop.Play(new ScreenTransitionData
{
    Layer = targetLayers,
    TargetVirtureCamera = "TargetCamera",
    StencileMaskeColor = Color.cyan,
    ChangeSpeed = 0.5f,
    TargetChangeAmount = 2f
});
// 이후 호출자의 사용 수명이 끝나는 지점에서 반환한다.
scop.Dispose();
```

Data는 값으로 복사되며 Data/CurrentChangeAmount/IsCompleted는 읽기 전용 프로퍼티다. 대여만 한 Scop은 Tick하지 않는다. 목표 반경 도달 후 마지막 합성 패스 기록을 기다리는 Finishing 상태를 거쳐 Main 마스크를 확정하고 완료·비활성화한다. 완료되어도 대여는 유지한다. Dispose에서 먼저 비활성화하고 Pool 반환 직전에 Data·진행량·상태·주입된 소유자 참조를 비운다. 재대여 시 같은 Scop 객체를 사용하며 반환한 참조는 재사용하지 않는다. IsCompleted는 최종 마스크 적용 완료를 뜻하지만 모든 화면 픽셀의 변화량을 측정한 결과는 아니다.

```text
전환 후 Main 컬링 = fixedLayerMask | targetLayerMask
```

항상 Init에서 받은 고정 마스크를 기준으로 조합한다. 현재 Main.cullingMask에 새 레이어를 계속 OR하지 않는다. 예를 들어 A에서 B, 이어서 C로 바뀌면 각각 고정|B, 고정|C가 되며 B가 잔류하지 않는다. 대상 레이어가 여러 개여도 같은 비트 연산을 쓴다.

현재 기본 구현은 Main·Render 모두 같은 FixedLayer를 사용한다. 진행량은 기존 셰이더의 월드 XZ 반경에 바로 적용하고 중심·폭은 Init에서 받는다. 다른 Scop이 출력 소유권을 가진 동안 새 Scop의 요청은 거부한다. 이 기본 정책은 질문에 대한 추가 사용자 답변이 오면 조정한다. 사용자가 명시적으로 확정한 Priority 하나만 상승 정책은 그대로 유지한다.

## 실행 순서

1. Pool에서 전환 Scop 객체를 대여한다. Play(data) 때 Init 설정과 요청을 검증하고 필요한 Camera Handle·Feature Scope를 확보한다.
2. Main은 기존 레이어 마스크를 유지하고 Render에는 FixedLayer | data.Layer와 스텐실 TargetRenderTexture를 설정한다. Render의 렌더 순서를 Main보다 앞에 둔다. 대상 버추얼 카메라 하나의 Priority만 올리고 두 출력의 구도는 기존 Brain이 계산한다.
3. 내부 Feature Scope로 스텐실 기능을 사용하고 중심·반경·요청 경계선 색을 적용한다.
4. 준비가 완료된 Scop은 전용 서비스에 자신을 활성화했다고 알린다. System이 활성 Scop에 Tick(deltaTime)을 전달하고, Scop은 현재 진행량을 ChangeSpeed * deltaTime만큼 증가시킨다. 목표값을 넘어가면 TargetChangeAmount로 제한한다. 시간 진행에는 DelayService 예약을 사용하지 않는다.
5. 목표 반경 도달 시 스텐실 PresentationVersion을 저장하고 합성 패스가 추가로 기록될 때까지 기다린다. 다음 Tick에서 Main.cullingMask에 `fixedLayerMask | data.Layer`를 적용한다. 스텐실 설정을 복원하고 Feature 사용을 해제한 뒤 완료 상태로 둔다. Scop 객체와 출력 사용 소유권은 Dispose 전까지 유지한다.
6. 호출자가 Dispose하면 먼저 비활성화를 알리고 Render의 임시 설정·스텐실 설정·Feature Scope·Camera Handle을 정리한다. 완료 전 취소는 Main의 저장 상태와 대상 카메라의 이전 Priority도 복원하고, 완료 후에는 확정된 Main 마스크와 대상 Priority를 유지한다. Data·자원·진행 상태를 비운 뒤 그 Scop 객체 자체를 Pool에 반환한다.

메인 출력은 한 Scop이 소유하며 완료된 Scop도 Dispose 전까지 소유권을 유지한다. 준비·Tick 적용 실패는 비활성화 후 확보한 자원을 정리한다. 다른 출력 자원의 복원에 실패해도 Handle 반환과 소유권 해제를 끝까지 시도한다. Play의 고정 마스크 추가를 제외하면 완료 전 Main의 대상 마스크 적용이나, 정리 뒤 반환된 Handle을 사용하는 Tick을 허용하지 않는다.

전환 Scop과 기존 RenderFeatureScop은 별개다. 사용자 Pool 결정은 상위 전환 Scop 자체의 대여/반환에 적용한다. 내부 Feature Scope는 기존 공개 수명 API로 획득·해제한다. 반환한 전환 Scop 참조는 이후 사용하지 않는다.

## Tick과 Scop 전용 서비스

사용자가 확정한 호출 경로는 다음과 같다.

```text
FrameworkRuntime.Tick(deltaTime)
  → GlobalSystemGroup.Tick(deltaTime)
    → RenderSystemGroup.Tick(deltaTime)
      → ScreenTransitionSystem.Tick(deltaTime)
        → 활성 ScreenTransitionScop.Tick(deltaTime)
```

RenderSystemGroup은 ISystemTick을 구현하고 기존 다른 그룹과 동일하게 자식 TickModules에 전달한다. ScreenTransitionSystem도 ISystemTick과 AddModule 등록으로 참여한다. RenderViewGroup은 서비스 조회 문맥이므로 별도의 Tick 전달 계층을 추가하지 않는다. 개별 Scop은 Framework 모듈로 등록하지 않는다.

Scop 전용 서비스의 개념 계약은 다음과 같다. 구체 이름은 구현 단계에서 동일 폴더의 기존 Pool 서비스 명명과 맞춘다.

```csharp
internal interface IScreenTransitionScopService
{
    void Activate(ISystemTick scop);
    void Deactivate(ISystemTick scop);
}
```

이 계약은 System이 구현하고 Scop에 직접 주입하는 내부 협력 계약이다. 외부 호출자용 CreateScop 서비스와 분리하며, 전역 서비스 키로 등록하거나 호출자가 활성 목록을 직접 조작하게 하지 않는다. Pool 반환 계약도 활성화 통지와 구분한다.

Tick 기반 구현에서는 기존 ISystemTick을 입력 계약으로 재사용한다. 실제 ScreenTransitionScop이 이 계약을 구현하고 내부 전용 서비스에 자신을 전달한다. 이 입력 타입이 Scop을 Framework 모듈로 등록한다는 뜻은 아니다. 별도의 IScreenTransitionScopPoolService로 객체 자체를 반환한다. System의 대여 목록은 반환·Shutdown 회수에, 활성 목록은 Tick에 사용하므로 완료된 대여 자원도 추적한다.

- CreateScop은 대여만 한다. Play의 자원 확보가 성공한 뒤 Scop이 Activate(this)를 호출한다.
- System은 활성 목록이 비어 있으면 바로 반환한다. 활성 목록이 Tick 대상의 원본이며 매 프레임 대여된 Scop 전체를 검색하지 않는다. 중복 활성화로 같은 Scop을 두 번 Tick하지 않는다.
- Scop은 자기 진행량과 완료 상태를 관리한다. 목표 도달 후 필요한 최종 출력 처리를 마치면 Deactivate(this)를 호출한다. 완료된 Scop은 Tick을 멈추지만 Dispose 전까지 Pool로 반환하지 않는다.
- 취소·실패·Dispose에서는 자원 해제와 Pool 반환보다 먼저 비활성화한다. 이미 비활성인 Scop의 비활성화는 안전하게 처리한다. Shutdown에서는 활성 여부와 별개로 대여 자원을 모두 회수한다.
- Tick 중 활성 목록에서 빠져도 다른 Scop의 Tick을 누락하거나 중복 호출하지 않게 한다. Tick 도중 새로 활성화되거나 반환 후 재대여된 Scop은 다음 Tick부터 처리하고, 기존 순회 항목으로 새 대여를 실행하지 않는다. 이를 위한 순회·변경 반영 방식은 구현과 함께 검증한다.

Tick은 연출 진행량을 갱신하는 경로다. Cinemachine Brain의 실제 출력 반영과 마지막 합성 프레임·컬링 변경 시점은 별도 렌더 경계이므로 T0의 완료 조건으로 계속 다룬다.

## 코드에서 확인한 구현 경계

1. RenderFeatureSystem의 `CreateScop()` → `UseRenderFeature(key)` → Scope 반환 경로를 사용한다. Feature 참조 수와 별도로 OutputProvider가 메인 출력 소유권을 한 Scop에 한정하여 전환끼리의 설정 충돌을 막는다.
2. 기존 IStencilSettingService는 유지하고 Systems의 IStencilTransitionSettingService를 추가했다. 요청 경계선 색·설정 저장/복원·출력 이름·패스 기록 버전을 제공하며 Line Presets 자산은 변경하지 않는다. Shader는 Main 깊이의 월드 XZ 원 안에 합성하고 깊이가 없는 배경은 원본을 유지한다. 목표 반경과 실제 합성 기록 확인을 완료 경계로 사용하며 화면 전체 픽셀 변화량을 측정하지 않는다.
3. 사용자가 전환 구도는 버추얼 Priority 하나만 높이는 방식으로 결정했다. 두 Brain이 같은 대상과 구도를 보도록 씬을 구성한다. Unity Handle은 Brain·Transform·렌즈를 노출하지 않고 출력 설정 캡처·복원·렌더 순서만 추가했다. 별도 Brain override나 구도 계산은 도입하지 않았다.
4. 기존 IStencilSettingService·RenderFeatureKeys·LineKey는 Assembly-CSharp에 그대로 둔다. Systems의 좁은 계약을 Render가 구현하므로 역참조·asmdef 변경·타입 이동 없이 연결된다. 프로덕션에서는 리플렉션이나 구체 타입 캐스트로 경계를 우회하지 않는다. 어셈블리 경계 테스트만 실제 Render 타입을 리플렉션으로 생성한다.

## SSOT·SOLID·FW·변경 독립성 점검

- 고정 마스크의 원본은 Init 설정 한 곳이다. 전환의 최종 마스크는 여기에서 계산하고 별도의 편집 가능한 누적 상태를 두지 않는다. 실제 출력 상태의 원본은 Camera, 실제 구도의 계산은 Cinemachine, 요청 목표·색·속도는 요청 Data, 현재 진행 상태는 대여된 전환 Scop이 각각 소유한다. 소요 시간은 목표량과 속도에서 계산한다.
- 카탈로그 조회·Handle Pool 수명, 연출 기능, Factory 조립을 분리한다(SRP). 상위 연출은 필요한 카메라/스텐실 계약에 의존한다(ISP/DIP). 대여·반환과 씬 수명 계약을 지킨다(LSP). 연출 표현 변경이 카탈로그 전체 개편으로 이어지지 않게 한다(OCP).
- FW-001~003의 서비스/모듈 등록·단일 Build, FW-004의 공개 제어 경계, FW-011/014의 Scope/Handle/Pool 반환, FW-016의 초기화 선행 조건을 유지한다.
- Tick 전달은 그룹, 활성 목록과 순회는 System, 진행량·완료 판단은 Scop에 둔다. Scop 전용 계약은 내부 주입으로 연결하여 FW 전역 서비스·모듈을 늘리지 않는다. Delay 콜백과 직접 Tick을 함께 사용하지 않는다.
- 변경 시나리오: 스텐실 반경 곡선 변경은 상위 연출과 해당 테스트에, 카메라 이름 조회 변경은 카탈로그에, 의존성 추가는 Factory에 모인다. 공통 스텐실 계약과 출력별 구도 연결은 영향받는 카메라/렌더 통합 검증이 필요한 실제 공유 경계로 명시한다.

## Task 계획과 리뷰 지점

### Phase T1a — Tick 기반 (완료)

2026-10-18 사용자가 Tick 구조 구현을 지시했다. 이 지시 범위의 선행 기반을 T0의 출력 설계와 독립된 Phase로 먼저 구현했다. 아래 T0의 미결정 출력 동작은 이번 Phase에서 정하지 않는다.

- RenderSystemGroup의 ISystemTick과 자식 TickModules 전달을 구현한다.
- RenderSystemFactory에서 ScreenTransitionSystem을 생성하고 Tick/Shutdown 모듈로 등록한다.
- 내부 IScreenTransitionScopService의 Activate/Deactivate와 활성 목록을 구현한다. 외부 서비스 키·CreateScop API는 이 기반 단계에서 추가하지 않는다.
- 순회 중 비활성화는 빈 슬롯으로 즉시 반영하고 새 활성화는 별도 대기 목록에 넣는다. finally에서 활성 목록을 압축하고 대기 목록을 합쳐 같은 객체의 재활성화도 다음 Tick부터 처리한다.
- 가짜 Scop으로 중복 활성화, 완료 후 Tick 중지·Dispose 분리, 순회 중 제거·신규 활성화·동일 객체 재대여, 예외·Shutdown·중첩 Tick, 그룹 전달과 메모리 할당을 검증한다.
- 완료 후 보고하고 멈춘다. 실제 Scop의 자원 대여·반환 구현이나 전환 완료 검증으로 보고하지 않는다.

### Phase T0 — 출력 경계 (코드 연결안 반영)

- 출력별 버추얼 구도 연결 방식, Render의 고정 마스크, 스텐실 계약의 어셈블리 위치를 확정한다.
- Init 설정과 확정된 Data 5개 필드의 진행량→반경 매핑, 원 중심·기본 경계선 프리셋을 정한다.
- 완료/실패/취소, 재요청과 마지막 합성 프레임·컬링 변경 순서를 확정한다.
- 공개 API와 필요한 하위 API의 최소 변경 목록·검증 계획을 작성하고 보고한다.

현재 연결안: 버추얼 Priority는 대상 하나만 올린다. Systems에 IStencilTransitionSettingService와 Key를 두고 Render의 기존 StencilSettings가 기존 계약과 함께 구현한다. 요청 경계선 색은 비직렬화 덮어쓰기이며 Line Presets 원본은 유지한다. Init의 고정 마스크와 원 설정을 저장한다. 반경 직접 적용·같은 고정 레이어 사용·동시 요청 거부는 기본 구현 정책으로 명시했다. 실제 구도·컬링 레이어·마지막 프레임은 Unity 검증으로 확인한다.

### Phase T1b — Scop·Pool·요청 전달 (완료)

사용자가 System → Pool Scop 대여 → 호출자가 Struct 전달 → Scop이 System에 활성화 통지하는 경로를 재명시했다. T0의 출력 동작과 독립적인 이 경로를 먼저 구현했다. 재요청은 사용자 답변에 따라 새 Data로 0부터 시작한다.

- IScreenTransitionSystemService.CreateScop, IScreenTransitionScop.Play와 Data·진행량·완료 조회를 구현한다.
- CustomPool<ScreenTransitionScop>으로 Scop 자체를 대여·Dispose 반환하고 전용 활성화 서비스와 Pool 반환 서비스를 주입한다.
- 진행량의 증가·목표량 제한·완료 비활성화, 완료 후 대여 유지, 요청 재시작, 반환 전 상태 초기화를 구현한다.
- 대여 목록과 활성 목록을 분리하고 Shutdown에서 준비·재생·완료 Scop을 모두 회수한다.
- RenderViewGroup·서비스 키를 통해 호출자용 대여 서비스를 연결한다. 내부 활성화·반환 서비스는 전역으로 노출하지 않는다.
- 실제 Pool 객체 동일성·중복 반환 방지·Data 복사·입력 검증·재요청·완료 상태·같은 프레임 재대여·Shutdown·Factory 조회를 검증한다.
- 완료 후 보고하고 멈춘다.

### Phase T2 — 스텐실 전환 연출 (코드 완료, Unity 확인 대기)

- 목표 출력 준비 → 스텐실 진행 → Main 컬링 확정 → 정리를 구현한다.
- Init 고정 마스크 원본·대상 마스크 조합, 실제 Camera Handle/Feature Scope 소유권과 정리를 연결한다.
- 가짜 하위 서비스로 순서·마스크·목표량 도달·요청 경계선 색·완료/취소/실패·Dispose/재대여 후 이전 순회 항목의 Tick 차단을 검사한다.
- Unity에서 실제 두 출력·스텐실·완료 경계를 검증한 뒤 보고한다.

Factory가 OutputProvider를 생성·주입한다. 전환 시스템을 출력 의존 서비스보다 먼저 등록하여 기존 Render 그룹의 순방향 Shutdown에서도 전환 Scope·Handle을 먼저 반환한다. 실제 연결 중 확인한 CameraCatalogSystem.PostBindService의 Loading 조회도 루트 Global 문맥을 거치도록 수정했다. 전역 레지스트리 구조나 다른 기능 수명 순서는 바꾸지 않는다.

### Phase T3 — C# 모듈 자동 초기화 (완료)

- 사용자 수정 지시와 C# 모듈 선택에 따라 외부 Init을 제거한다.
- Factory는 ScreenTransitionInitModule을 주입하고 System은 ISystemInitialize로 한 번 읽는다. InitData는 내부 타입으로 한정한다.
- 초기화 시 설정만 저장하고 실제 카메라 Handle은 Play에서 확보한다. 고정 레이어는 코드 모듈 한 곳에서 정의하고 Main의 전환 중 마스크에서 유추하지 않는다.
- 모듈 1회 읽기·공개 Init 제거·설정 보존·현재 프로젝트 레이어 정의 일치·누락 오류·카메라 미대여 초기화를 검증한다.
- 기존 출력·Scop·Tick·Render 계약 회귀를 확인하고 보고한다.

### Phase T4 — After/Before 테스트 러너 (코드 완료, Unity 확인 대기)

- 사용자 요청대로 기존 TestRuner에 AfterRendering ↔ BefalRendering 양방향 테스트를 추가한다. 기본 대상 버추얼 카메라는 각각 Day Camera와 Night Camera다.
- 화면 전환 전용 partial에 요청 Struct 생성·Scop 대여·재생·동일 Scop 재시작·Dispose·Pool 객체 재사용 확인·초기 상태 복원을 모은다. Init은 시스템에 맡긴다.
- 기존 JMHTest의 두 환경·카메라·RT를 재사용하고 Runner 오브젝트와 Catalog 참조를 추가한다. 기존 카메라 이벤트·AreaOutline 테스트는 이 오브젝트에서 끈다.
- 진행량·완료 대기·Main/Render 마스크·Priority를 표시한다. 완료 시 대상 레이어 포함과 반대 레이어 제외를 검사한다. 색·속도·목표 반경은 Inspector에서 조절한다.
- 빌드와 기존 전환 회귀 테스트, 씬 참조를 확인하고 보고한다. 실제 화면·GPU 검증은 Unity Play에서 확인한다.

### Phase T5 — 기존 카메라 이동·줌·흔들림 테스트 제거 (코드 완료)

사용자가 Play Camera Name을 확인한 뒤 카메라 이동·줌·흔들림 제거를 지시했다. 대화 맥락의 대상인 TestRuner의 기존 수동 카메라 테스트를 정리한다.

- TestRuner.CameraTests.cs와 meta, 시작·프레임·종료 연결, 기존 Inspector 필드·컨텍스트 메뉴·패널·코루틴을 제거한다.
- 화면 전환 partial이 OnGUI를 직접 소유해 기존 카메라 테스트 파일에 대한 표시 의존성을 없앤다.
- 현재 JMHTest의 기존 카메라 테스트 직렬화 값을 제거하고 화면 전환 테스트를 켠다. 다른 사용자 씬 변경과 AreaOutline 설정을 보존한다.
- CameraEffectSystem·카탈로그 기능과 그 EditMode 테스트는 유지한다. 문서에서 제거한 러너의 사용 안내를 갱신한다.
- 빌드·삭제 코드의 참조·직렬화 잔여를 확인하고 보고한다. 코드 제거에 대한 구현 복제 테스트는 추가하지 않는다.

## 실행 기록

2026-10-18: 사용자 결정과 실제 하위 API/어셈블리 경계를 조사해 설계와 Task 계획을 작성했다. 화면 전환 런타임 코드는 아직 구현하지 않았다. 기존 두 카탈로그·Pool 변경은 유지한다. Core·FieldCamera·Architecture·씬·셰이더는 이번 정리에서 수정하지 않았다.

같은 날 추가 결정: 상위 Scop 자체를 Pool에서 대여하고 호출자의 Dispose에서 반환한다. 완료 자동 반환은 하지 않는다. Data의 Layer/TargetVirtureCamera/StencileMaskeColor/ChangeSpeed를 확정했고, 색은 원의 경계선 색·속도는 초당 진행량 증가로 정했다. 이어서 요청 Data의 목표 변화량을 완료 기준으로 추가했다. 목표 필드 이름은 TargetChangeAmount로 제안했으며 Data의 현재 진행량을 호출자와 동기화하는 이중 상태는 만들지 않는다. 런타임 구현은 아직 시작하지 않았다.

같은 날 Tick 결정: 그룹에 Tick 전달을 추가하고 System이 Scop 전용 활성화·비활성화 서비스를 제공한다. Scop이 자신의 활성화를 통지하고 System이 활성 Scop만 Tick한다. 기존 DelayService 우선 검토안을 이 결정으로 대체했다. 설계와 T1 검증 항목에 반영했으며, 런타임 코드 구현은 T0 이후 단계로 유지한다.

이후 사용자 구현 지시로 Phase T1a의 Tick 기반을 먼저 구현했다. RenderSystemGroup → ScreenTransitionSystem → 활성 ISystemTick 대상 경로와 내부 Activate/Deactivate 계약을 연결했다. 가짜 Scop으로 14개 테스트를 Unity Mono에서 직접 실행해 14/14 통과했다(Logs/screen-transition-tick-tests.log). VS MSBuild로 ProjectL.Tests.Framework와 의존 어셈블리 빌드 성공, 변경 소스의 새 경고는 0개다(Logs/screen-transition-tick-build.log). 예열 후 자기 비활성화·재활성화를 포함한 256회 Tick의 관리 메모리 할당은 0바이트다. Global/Render 모듈 전달을 포함해 검사했지만 실제 전환 Scop/Pool·카메라·스텐실 렌더링을 실행한 결과는 아니다. 열린 Unity 에디터가 프로젝트 잠금을 보유하므로 별도 Unity EditMode Runner는 실행하지 않았다. Phase 리뷰 지점에서 멈춘다.

같은 날 사용자 경로 재명시로 T1b를 구현했다. System의 CreateScop이 CustomPool에서 실제 Scop 객체를 가져오고, 호출자가 Scop.Play에 Struct를 전달하면 Scop이 System의 내부 서비스로 자신의 활성화를 알린다. 같은 Scop의 재요청은 사용자 선택대로 진행량 0부터 다시 시작한다. 완료 후 대여 유지, Dispose 비활성화·동일 객체 반환·상태 초기화, Shutdown 전체 대여 회수를 연결했다. ScreenTransitionScopTests 13개와 기존 Tick 14개를 Unity Mono에서 직접 실행해 27/27 통과했다(Logs/screen-transition-scop-tests.log). 예열 후 실제 대여→Play→Tick→Dispose 256회의 관리 메모리 할당은 0바이트다. VS MSBuild 빌드 성공, 변경 소스 새 경고 0개다(Logs/screen-transition-scop-build.log). 실제 카메라·Feature·고정 마스크 Init·렌더 합성은 이번 단계에 포함하지 않았다. T1b 리뷰 지점에서 멈춘다.

이후 사용자 '이제 연결' 지시로 T2 출력 코드를 연결했다. 사용자는 버추얼 전환을 Priority로 처리하되 대상 하나만 높이도록 정정했다. CameraCatalogHandle.RaisePriority는 다른 카메라 값을 유지하며 최고값보다 올리고, int.MaxValue에서는 동순위 선택 순서를 올린다. Unity Camera Handle에는 설정 캡처·복원·렌더 순서 API를 추가했다. Scop과 같이 재사용하는 Output이 카메라·Feature 자원을 소유한다. Systems의 좁은 스텐실 계약, 런타임 경계선 색, Main 출력 이름 필터, 마지막 반경의 패스 기록 확인과 최종 컬링을 연결했다.

검증: ProjectL.Tests.Framework와 Assembly-CSharp 빌드 성공. 변경 소스의 새 경고 없음. Unity Mono 직접 실행은 출력 9개 + Scop 13개 + Tick 14개 + 실제 Render 설정의 어셈블리 경계·색 복원 2개, 38/38 통과했다. 실제 Camera·Cinemachine 객체가 필요한 Priority 2개·Unity 출력 복원 1개 테스트는 컴파일만 확인했다. 로그는 Logs/screen-transition-output-build.log, Logs/screen-transition-render-build.log, Logs/screen-transition-output-tests.log다. 초기화 설정은 호출 View의 조립 지점에서 전달하며 운영 씬·사용자 씬·RT 에셋은 수정하지 않았다. 기존 에디터가 프로젝트 잠금을 보유하므로 Unity EditMode Runner와 실제 GPU 화면 확인은 대기 중이다. T2 코드 리뷰 지점에서 보고한다.

같은 날 T3: 사용자가 Init을 시스템 자체 모듈에서 처리하고 사용자는 Play만 하도록 수정했다. 설정 저장소는 사용자 답변대로 C# 초기화 모듈로 정했다. System의 Framework Initialize에서 모듈을 읽고 Provider에 저장하며 공개 서비스에는 CreateScop만 남겼다. 초기화 모듈의 이름·레이어·원 설정은 한 곳에서 정의한다. 부트 시 카메라가 필요하지 않게 실제 대여와 고정 마스크 적용을 Play로 옮겼다. MSBuild로 ProjectL.Tests.Framework·Assembly-CSharp 빌드 성공, 변경 소스 새 경고 없음. 자동 초기화 4개·출력 10개·Scop 13개·Tick 14개·Render 경계 2개를 Mono에서 실행해 43/43 통과했다(Logs/screen-transition-auto-init-tests.log). 빌드 로그는 Logs/screen-transition-auto-init-build.log·Logs/screen-transition-auto-init-render-build.log다. 운영 씬·Core·FieldCamera·Architecture는 수정하지 않았다. 실제 Unity 화면 확인은 계속 대기 중이다.

같은 날 T4: 사용자 요청에 따라 TestRuner 화면 전환 partial과 JMHTest의 Runner 오브젝트를 추가했다. AfterRendering ↔ BefalRendering을 각각 Day Camera·Night Camera에 연결했다. 색·속도·목표 반경 조절, 실제 Scop 대여/Play/재요청/Dispose, 완료 후 마스크 검사, Pool 객체 동일성, 초기 상태 복원을 제공한다. 테스트 요청·표시·진단 수명은 별도 partial에 모았고 조립은 기존 Runner 시작/종료 경로에 연결했다. 카메라 이름의 원본은 기존 C# 모듈 내부 상수로 유지한다(SSOT). 기존 테스트는 플래그로 분리하며 공개 서비스·FW 수명 계약은 변경하지 않았다(SRP/ISP·변경 독립성). 기존 씬 사용자 변경을 보존하고 새 Runner만 추가했으며 Core·FieldCamera·Architecture·운영 씬은 수정하지 않았다. MSBuild 두 어셈블리 성공, 변경 소스 새 경고 없음, 전환 관리 테스트 43/43 통과했다. 씬 문서 ID·카메라 이름·스크립트/Catalog GUID 참조도 확인했다. 로그는 Logs/screen-transition-runner-build.log·Logs/screen-transition-runner-render-build.log·Logs/screen-transition-runner-tests.log다. 실제 Unity Play·GPU 검증은 대기 상태로 T4 코드 리뷰 지점에서 보고한다.

같은 날 T5: TestRuner.CameraTests.cs·meta와 Runner의 기존 호출 3개를 제거했다. 화면 전환 partial에 OnGUI를 두어 After/Before 패널을 독립적으로 유지한다. 현재 JMHTest에서 PlayCamera/CinematicCamera·WASD·줌·흔들림·자동 테스트 목록의 직렬화 값을 지우고 screenTransitionTestsEnabled를 1로 설정했다. CameraEffectSystem·카탈로그·그 테스트와 AreaOutline은 수정하지 않았다. 삭제한 메서드·enum·GUID의 Assets 참조와 씬의 기존 설정 잔여가 없음을 확인했다. MSBuild ProjectL.Tests.Framework 및 의존 어셈블리 성공, 변경 코드 새 경고 없음(Logs/test-runner-camera-cleanup-build.log). Unity가 갱신하기 전 생성 csproj에 삭제 파일이 남아 있어 로컬 Temp targets에서 해당 Compile 항목을 제외했으며 생성 csproj를 직접 수정하지 않았다. 전환 요청/상태의 원본과 공개 FW 서비스·Pool 수명은 유지하고 테스트 표시 의존성만 제거했다(SSOT/SRP·변경 독립성). 실제 Unity Play는 대기 중이다. Core·FieldCamera·Architecture·다른 사용자 변경은 건드리지 않았다.
