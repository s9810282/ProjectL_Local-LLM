# 화면 전환 Tick 기반 — Framework 공유 메모

2026-10-18. 사용자 지시로 구현한 Phase T1a·T1b·T2·T3·T4·T5의 공유 기록이다. 외부로 전달하지 않았다.

## 공용 변경

- RenderSystemGroup이 ISystemTick을 구현하고 자식 TickModules에 deltaTime을 그대로 전달한다. GlobalSystemGroup의 기존 전달 경로로 실행된다.
- RenderSystemFactory에서 ScreenTransitionSystem을 생성하고 서비스·모듈로 등록한다. RenderSystemServiceKey.ScreenTransitionSystem과 RenderViewGroup.ScreenTransitionSystemServices로 대여 서비스를 노출한다.
- 이 변경 시점에 Render 그룹 안의 Tick 구현은 새 ScreenTransitionSystem뿐이다. 기존 카메라 효과의 Delay 기반 실행을 함께 변경하지 않았다.

## Scop 내부 협력 계약

ScreenTransitionSystem이 IScreenTransitionScopService를 구현한다. Activate(ISystemTick scop)와 Deactivate(ISystemTick scop)는 Scop에 직접 주입하는 내부 계약이며 전역 서비스로 등록하지 않는다. 같은 참조의 중복 활성화는 무시한다. Tick 중 제거는 즉시 반영하고 새 활성화는 다음 Tick부터 처리한다. 같은 객체가 반환·재대여되어도 이전 순회 슬롯으로 새 대여를 실행하지 않는다.

T1b에서 IScreenTransitionSystemService.CreateScop이 CustomPool<ScreenTransitionScop>에서 Scop 객체를 대여하는 경로를 연결했다. 외부 호출자가 Scop.Play(ScreenTransitionData)에 Struct를 전달하면 Scop이 자신의 활성화를 알린다. System에 외부 Play API나 직접 활성화 API를 두지 않는다. 사용자 결정에 따라 Play 재호출은 새 Data로 진행량 0부터 다시 시작한다.

ScreenTransitionScop은 요청 Data·진행량·완료 상태를 소유한다. 목표량 도달 후 마지막 합성 패스 기록을 기다리고 Main 마스크를 적용한 뒤 완료·비활성화한다. 완료 후에도 대여 목록에 남고 Dispose에서 비활성화 후 Scop 자체를 반환한다. Pool 반환 직전에 요청·진행량·소유자 참조를 비운다. Shutdown은 준비·재생·완료 상태의 대여 Scop을 모두 회수한다.

## T2 출력 연결

- Factory가 ScreenTransitionOutputProvider를 생성·주입한다. 전환 시스템을 출력 의존 서비스보다 먼저 등록하여 Render 그룹의 기존 순방향 Shutdown에서도 전환 자원을 먼저 반환한다. 다른 모듈의 상대 순서는 바꾸지 않았다.
- 시스템이 Framework Initialize에서 C# ScreenTransitionInitModule을 한 번 읽어 Main/Render 이름·FixedLayer·원 중심·경계 폭을 Provider에 저장한다. 공개 Init은 T3에서 제거했다. Scop과 함께 재사용하는 Output이 카메라·Feature 자원을 소유한다.
- CameraCatalogHandle.Priority/RaisePriority를 추가했다. 대상 하나만 높이고 나머지 Priority는 유지한다. CameraCatalogSystem.PostBindService는 Loading을 루트 Global 문맥에서 조회하도록 수정했다. Lobby 카메라 테스트의 위임 Handle도 새 계약을 그대로 전달한다.
- Unity Camera Handle에 Capture/Restore/SetRenderOrder를 추가했다. 전환 중 Render를 Main보다 먼저 렌더하고 종료 시 원래 설정으로 돌린다.
- Systems에 IStencilTransitionSettingService/StencilTransitionFeature.Key를 두고 Render의 기존 StencilSettings가 구현한다. 기존 IStencilSettingService는 유지한다. 스텐실 원본 Line Presets를 바꾸지 않고 런타임 색 덮어쓰기만 적용한다. 설정 복원과 Main 출력 이름 필터를 제공한다.
- 목표 반경 합성 패스의 PresentationVersion 증가를 기다린 다음 Tick에서 최종 마스크를 적용한다. GPU 픽셀 전체가 바뀌었는지 측정하는 계약은 아니다.
- 반경 직접 적용·두 출력의 같은 고정 마스크·소유 중 다른 Scop 거부는 기본 구현 정책이다. 추가 사용자 답변 시 정책을 조정한다. 취소는 이전 출력 설정과 대상 Priority를 복원하고, 성공 후 Dispose는 확정 마스크와 대상 Priority를 유지한다.

## 계약과 변경 독립성

그룹은 전달, System은 Pool·대여 목록·활성 목록과 순회, Scop은 요청·진행·완료 판단을 맡는다. 활성 목록을 원본으로 사용하고 대여 목록 전체를 매 프레임 검색하지 않는다. 요청값은 Scop의 Struct 한 곳에 저장하며 별도 Duration 원본은 없다. 조립 변경은 Factory에 모았다. FW-001~003의 외부 서비스·모듈 등록과 단일 Build, FW-014의 Pool 반환 계약을 유지하며 내부 활성화·반환 서비스를 전역 서비스로 등록하지 않는다. 공개 Scop 계약은 Play·조회·Dispose만 제공하며 Tick은 내부 구현에서 참여한다. Core·FieldCamera·Architecture·씬·셰이더는 수정하지 않았다.

## 검증

- VS MSBuild: ProjectL.Tests.Framework 및 의존 어셈블리 빌드 성공. 변경 파일에 새 경고 없음. 로그: Logs/screen-transition-scop-build.log.
- Unity Mono 직접 실행: ScreenTransitionScopTests 13개 + ScreenTransitionTickTests 14개, 27/27 통과. 로그: Logs/screen-transition-scop-tests.log.
- 실제 Scop Pool 동일 객체 재사용·중복 반환 방지·Struct 복사·Play 활성화·재요청 초기화·목표량 제한·완료 후 대여 유지·Shutdown 회수·Factory 조회를 검증했다. 순회 중 실제 Pool 반환·재대여와 재요청은 다음 Tick부터 진행된다.
- 예열 후 실제 Scop 대여→Play→Tick→Dispose 256회의 관리 메모리 할당은 0바이트다. 가짜 Scop 기반 순회 검증도 유지했으며 실제 화면 전환 렌더링을 검증한 결과로 취급하지 않는다.
- 실행 중인 Unity 에디터의 프로젝트 잠금을 확인했다. 별도 에디터를 실행하거나 기존 에디터를 종료하지 않았으며 Unity EditMode Runner는 미실행이다.

T2 검증: ProjectL.Tests.Framework와 Assembly-CSharp 빌드 성공, 변경 소스 새 경고 없음. Unity Mono로 출력 9개·Scop 13개·Tick 14개·실제 Render 설정 경계/색 복원 2개, 38/38 통과했다. 로그는 Logs/screen-transition-output-build.log, Logs/screen-transition-render-build.log, Logs/screen-transition-output-tests.log다. Priority 2개와 실제 Unity 출력 복원 1개 테스트는 컴파일만 확인했다. 운영 씬·사용자 씬·RT 에셋은 수정하지 않았으며, 열린 Unity 에디터의 잠금으로 EditMode Runner와 실제 GPU 화면 확인은 대기 중이다.

현재 단계와 사용법은 [전환 설계](../specs/2026-10-18-screen-stencil-transition-design.md), [호출자 설정 안내](../../render-screen-transition.md)를 따른다.

## T3 자동 초기화

사용자는 설정 원본을 C# 초기화 모듈에 두도록 선택했다. Factory가 IScreenTransitionInitModule 구현을 주입하고 System이 ISystemInitialize로 한 번 읽는다. 내부 InitData를 공개 서비스에서 제거했고 호출자는 대여한 Scop.Play만 사용한다. 카메라는 초기화 시 빌리지 않고 Play에서 확보하므로 설정 초기화가 카메라 수집 순서에 의존하지 않는다. 고정 레이어는 모듈에서 명시적으로 정의하며 전환 중 컬링 마스크에서 추출하지 않는다. 설정 원본의 변경은 모듈, 수명 변경은 System, 출력 변경은 Output에 모여 기능 변경의 독립성을 유지한다. FW-001/002의 제공 서비스·모듈 경계와 FW-016의 초기화 선행 조건을 유지한다.

현재 기본값은 Main Camera / RenderTexture Camera, Default·BothRendering·DayCameraRender·NightCameraRender, 중심 (0, 0), 경계 폭 5.48이다. 초기화 검증을 포함한 Mono 테스트 43/43 통과, ProjectL.Tests.Framework·Assembly-CSharp 빌드 성공, 변경 소스 새 경고 없음. 로그는 Logs/screen-transition-auto-init-tests.log·Logs/screen-transition-auto-init-build.log·Logs/screen-transition-auto-init-render-build.log다. 실제 Unity 화면 확인은 대기 중이다.

## T4 After/Before 테스트 환경

사용자가 테스트 러너 환경과 After/Before 대상을 지정했다. `TestRuner.ScreenTransitionTests.cs`에 실제 서비스 호출을 분리하고 기존 Runner의 시작·Tick 후 상태 확인·종료·IMGUI에 연결했다. 시스템 직접 생성이나 외부 Init은 사용하지 않는다. 초기화 모듈의 카메라 이름을 내부 상수로 공유해 진단용 Handle 조회에도 같은 원본을 사용한다.

`Assets/Scenes/JMHTest.unity`에 Screen Transition Test Runner 오브젝트와 DatabaseCatalogSO 참조를 추가했다. AfterRendering → Day Camera, BefalRendering → Night Camera이며 기존 두 환경·카메라·RT를 그대로 사용한다. 이 오브젝트에서만 기존 카메라 이벤트·AreaOutline 테스트를 끈다. 씬에 있던 사용자의 Render 카메라 부모·위치 변경은 유지했다. Core·FieldCamera·Architecture와 운영 씬은 수정하지 않았다.

같은 Scop 재생·진행 중 취소·완료 후 반환·Pool 객체 재사용 확인·시작 상태 복원 버튼을 제공한다. 완료 검사는 Main의 대상 비트 포함과 반대 비트 제외를 확인하며, 화면 픽셀 검증을 대신하지 않는다. Runner 종료 시 Scop과 진단 Handle을 Runtime Shutdown보다 먼저 반환한다. 테스트 요청/표시는 별도 partial에 모으고 기존 테스트 기능은 선택 플래그로 분리했다. 공개 서비스·FW 수명 계약은 바꾸지 않았다.

ProjectL.Tests.Framework·Assembly-CSharp MSBuild 성공, 변경 소스 새 경고 없음. 기존 전환 관리 테스트 43/43 통과. 씬 문서 ID 중복·Runner 루트·카메라 이름·스크립트/Catalog GUID도 확인했다. 로그는 Logs/screen-transition-runner-build.log·Logs/screen-transition-runner-render-build.log·Logs/screen-transition-runner-tests.log다. 실제 Unity Play 화면·EditMode Runner 실행은 대기 중이며, 수동 확인 순서는 [사용법](../../render-screen-transition.md#jmhtest-수동-테스트)에 있다.

## T5 기존 수동 카메라 테스트 제거

사용자 요청으로 TestRuner.CameraTests.cs와 meta를 삭제했다. 이동·줌·흔들림·기존 Play/Cinematic 전환 패널, Inspector 설정, Camera 메뉴, 코루틴·자동 목록과 Runner의 시작/Tick/종료 호출을 제거했다. 화면 전환 partial이 OnGUI를 직접 소유하여 After/Before 패널을 유지한다. 현재 JMHTest의 옛 직렬화 필드를 지우고 화면 전환 테스트를 켰다. 기존 CameraEffectSystem·카탈로그·EditMode 테스트와 AreaOutline은 유지한다. 공개 FW 계약·Factory·Core·FieldCamera·Architecture에는 변경이 없다.

ProjectL.Tests.Framework 및 의존 어셈블리 MSBuild 성공, 변경 코드 새 경고 없음(Logs/test-runner-camera-cleanup-build.log). 삭제 코드의 메서드·enum·GUID Assets 참조와 씬 설정 잔여가 없음을 확인했다. Unity Play 확인은 대기 중이다. 씬의 다른 사용자 편집과 기존 Unity 직렬화 공백은 보존했다.
