# 카메라 카탈로그

2026-10-17 C1: 버추얼 카메라는 기존 `CameraCatalogSystem`, Unity 출력 카메라는 새 `UnityCameraCatalogSystem`으로 나눈다. 두 카탈로그 모두 GameObject 이름으로 핸들을 제공하며 별도 역할 Key 컴포넌트를 요구하지 않는다. 서로 다른 카탈로그의 이름 공간은 독립적이다.

```csharp
var cameras = global.RenderServiceCommand.CoreGroup.CameraGroup;
var shot = cameras.CameraCatalogSystem.GetHandle("PlayCamera");
var output = cameras.UnityCameraCatalogSystem.GetHandle("Main Camera");
```

## 버추얼 카메라 — CameraCatalogSystem

일반 C# 시스템이다. 별도 MonoBehaviour, CameraSceneBinding, Inspector 참조 연결이 필요 없다. Render Core의 CameraGroup에서 CameraCatalogSystem과 CameraEffectSystem 서비스 및 모듈을 묶는다. 그룹에는 카메라 기능이 없으며 BindScene/PostBindService/Shutdown을 자식 모듈에 전달한다. 효과 시간 진행은 기존 DelayService가 담당한다.

## 역할

BindScene에서 로드된 씬의 CinemachineVirtualCamera를 직접 찾는다. 비활성 오브젝트와 비활성 컴포넌트도 포함하며, 오브젝트 이름을 대소문자 구분 키로 사용한다. 모든 로드된 씬에서 이름이 유일해야 한다. 중복 이름이면 실패하며 기존 카탈로그와 핸들은 유지한다.

수집 결과는 CameraCatalogNode에 저장한다. Node는 기존 Framework CustomPool을 사용한다. GetHandle 요청 시 풀에서 제어용 Node를 꺼내 해당 카메라 이름과 참조를 넣고 핸들로 제공한다. 카탈로그 등록용 Node와 제어 대여용 Node 모두 같은 풀을 재사용한다.

시스템의 외부 기능은 GetHandle(objectName) 하나다. 핸들은 다음 기능을 제공한다.

- SetPriority(int): 해당 가상 카메라의 우선순위 변경.
- Priority / RaisePriority(): 현재값 조회와 대상 하나만 최고 우선순위로 올리기. 다른 카메라의 값은 유지한다. int.MaxValue에서는 동일값의 선택 순서를 올린다. RaisePriority의 대상은 활성 상태여야 한다.
- SetZoom(float): 직교 카메라는 OrthographicSize, 원근 카메라는 FieldOfView의 절대값 변경. 작은 값이 줌인이다.
- Customize(CameraCatalogData): struct에 담은 위치·회전·렌즈·Follow·LookAt·Priority·활성 상태·StandbyUpdate·Transitions 설정을 적용한다. 콜백이나 카메라 참조를 반환하지 않는다.

CameraCatalogData는 전체 설정 교체용 값이다. 생략한 필드를 유지하는 부분 업데이트가 아니다. 새 데이터를 만들 때는 CameraCatalogData.Default에서 시작하고 필요한 값을 채운다. Follow/LookAt에 null을 넣으면 타깃 연결을 해제한다. Body/Aim 파이프라인 컴포넌트나 Extension 추가·제거는 이 데이터의 범위에 포함하지 않는다. 호출자가 카메라 참조를 이미 가진 경우 new CameraCatalogData(camera)로 현재 설정을 복사한 뒤 일부 값을 바꿀 수 있다. 이 복사값은 시스템이 보관하거나 자동 복구하는 State가 아니다.

Dispose는 제어 Node를 풀에 반납하는 수명 관리다. 카메라를 비활성화하거나 변경한 값을 원복하지 않는다. 씬 재바인딩과 Shutdown도 모든 Node를 반납하며 풀은 유지한다. 이전 핸들은 무효화되고 재사용된 Node를 제어할 수 없다. 새 씬에서는 이름으로 새 핸들을 받아야 한다.

## 사용

```csharp
var catalog = global.RenderServiceCommand.CoreGroup.CameraGroup.CameraCatalogSystem;
var play = catalog.GetHandle("PlayCamera");
var cinematic = catalog.GetHandle("CinematicCamera");

play.SetZoom(4f); // 현재 테스트 씬은 직교 카메라

var data = CameraCatalogData.Default;
data.Position = position;
data.Rotation = rotation;
data.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
data.Lens.OrthographicSize = 4.5f;
data.Priority = 10;
cinematic.Customize(data);
play.SetPriority(0);

// 더 이상 제어하지 않을 때 반환. 지정한 카메라 설정은 유지된다.
play.Dispose();
cinematic.Dispose();
```

순수 Priority 변경은 활성화나 다른 카메라의 Priority 변경을 대신하지 않는다. 전환 정책은 호출자가 정한다. 실제 전환은 출력 카메라의 CinemachineBrain이 씬에 설정한 Default/Custom Blend대로 처리한다. 별도 카메라 생성, Brain 제어, 흔들림, 타이머, 줌 보간, 원본 저장·복구는 이 시스템에 없다.

오브젝트 이름이 바뀌거나 카메라가 추가·제거된 경우 BindScene을 다시 호출해야 한다. 여러 핸들이 같은 카메라를 제어할 수 있으며 충돌 정책은 호출자 책임이다.

## 테스트

JMHTest의 TestRuner는 현재 Day Camera·Night Camera를 대상으로 After/Before 화면 전환을 확인한다. 사용자가 요청하여 기존 Camera Event Tests의 이동·줌·흔들림·PlayCamera/CinematicCamera 설정과 컨텍스트 메뉴는 제거했다. 현재 수동 실행 방법은 [화면 전환 테스트](render-screen-transition.md#jmhtest-수동-테스트)를 따른다.

CameraCatalogSystemTests에는 비활성 카메라 수집, Priority/줌, struct 설정 적용·값 복사·타깃 해제·활성 상태 변경, Node 재사용, 씬 재바인딩 후 오래된 핸들 차단, 중복 이름·잘못된 입력 검사가 있다. 노드는 재사용하지만 대여마다 안전한 핸들 래퍼를 생성하므로 GetHandle 호출이 무할당인 것은 아니다. 핸들은 매 프레임 받지 말고 제어 기간 동안 보관한다.

이전 카메라 구현과 제거한 씬 컴포넌트 데이터는 Temp/CameraCatalogMigrationBackup에 보관했다. Temp는 임시 폴더이므로 Unity 정리나 수동 삭제 전까지 복구 자료로 사용할 수 있다.

## Unity 출력 카메라 — UnityCameraCatalogSystem

`IUnityCameraCatalogService.GetHandle(objectName)`으로 Unity `Camera`를 조회한다. 최초 `BindScene`에서 로드된 유효한 씬의 게임용 Camera를 수집하며, 비활성 오브젝트·비활성 Camera도 포함한다. 이름은 대소문자를 구분하고 같은 카탈로그 안의 중복 이름은 오류다. 중복이 있으면 목록을 설치하지 않으므로 수정 후 최초 바인딩을 다시 시도할 수 있다.

에디터에서는 `EditorSceneManager.IsPreviewSceneObject`로 미리보기 씬의 Camera를 이름 검사 전에 제외한다. 미리보기 Camera가 Game 타입이거나 실제 출력과 이름이 같아도 등록·중복 검사에 참여하지 않는다. 실제 게임 씬의 이름 중복 검사는 유지한다. 이 필터와 UnityEditor 참조는 `UNITY_EDITOR`에서만 포함한다.

중복 오류에는 두 Camera의 씬 경로·Transform 계층 경로·Instance ID를 표시한다. FBX에 포함된 Camera는 게임 씬의 실제 Camera로 수집되므로 배경 모델에 불필요하게 포함된 경우 Model Importer의 Import Cameras를 끈다. JMHTest에서 확인한 6개 FBX와 변경 기록은 [C4 기록](superpowers/specs/2026-10-17-two-camera-catalogs-design.md#c4--jmhtest-fbx-카메라-임포트-정리-2026-10-18)을 참고한다.

사용할 출력 카메라는 최초 BindScene 전에 준비한다. 최초 등록에서 이름 검증을 모두 통과하면 플레이 중 각 Camera의 `transform.root.gameObject`에 DontDestroyOnLoad를 적용한다. 같은 루트는 한 번만 처리한다. 자식 카메라를 분리하지 않으므로 Main 아래 Render 카메라의 부모·로컬 Transform·활성 상태가 유지되고, 같은 루트 아래의 다른 오브젝트도 함께 유지된다. EditMode에서는 영속 처리를 호출하지 않는다. 중복 이름으로 등록에 실패하면 카메라 루트를 이동하지 않는다.

첫 바인딩 후의 `BindScene`은 목록과 핸들을 유지한다. 이후 씬에서 생성한 Camera는 자동으로 추가하지 않는다. 실행 중 이름을 바꿔도 최초 수집 이름은 바뀌지 않는다. 카메라 오브젝트 생성은 Bootstrap/씬 구성의 책임이며 카탈로그는 생성·파괴하지 않는다. Shutdown은 등록과 Handle만 정리하고 영속 카메라를 파괴하거나 원래 씬으로 되돌리지 않는다.

핸들은 출력 설정만 제어한다.

```csharp
using var output = cameras.UnityCameraCatalogSystem.GetHandle("RenderTexture Camera");
output.SetCullingMask(mask);
output.SetTargetTexture(texture); // null이면 targetTexture를 해제한다.
output.SetEnabled(true);         // Camera 컴포넌트만 켠다. GameObject를 활성화하지 않는다.
UnityCameraRenderState before = output.Capture();
output.SetRenderOrder(-1f);      // Camera.depth로 출력 순서를 설정한다.
output.Restore(before);         // 마스크·targetTexture·enabled·출력 순서 복원.
```

Dispose는 해당 Handle 객체 자체를 풀에 반환하며 카메라 설정을 복원하거나 카메라를 끄지 않는다. 같은 카메라를 보는 다른 대여 핸들은 계속 사용할 수 있다. 반환 시 Camera·반환 소유자 참조를 비우고, 재대여 전 조작은 ObjectDisposedException이다. 대상 파괴 시에도 조작을 거부한다. Shutdown은 카메라를 파괴하지 않고 미반환 Handle을 일괄 회수한다. 반환한 참조는 이후 사용하지 않는다. 동일 객체를 재대여하므로 재대여된 뒤에는 이전 참조와 새 참조를 구분할 수 없다. SetTargetTexture는 참조만 연결하고 RenderTexture 생성·해제·크기 조정은 하지 않는다.

Unity 카탈로그는 유지 출력 카메라 참조를 한 번 보관한다. 2026-10-18 C2부터 UnityCameraCatalogHandlePool이 Framework CustomPool<UnityCameraCatalogHandle>을 사용한다. GetHandle은 풀의 Handle을 그대로 바인딩해서 반환하고 Dispose는 자기 자신을 반환한다. 별도 Node나 Handle 래퍼는 없다. 동시 대여 수가 풀 보유량을 넘을 때만 추가 Handle을 생성하고, 워밍업한 범위의 반복 조회·반환은 같은 객체를 재사용한다. Tick·Loading 등록은 없다. Camera/Brain 참조, Transform·렌즈·구도 선택, URP 후처리 효과 값은 이 핸들에서 제공하지 않는다. 같은 카메라의 출력 설정을 여러 핸들이 변경할 때의 정책은 호출자가 소유한다.

UnityCameraCatalogSystemTests에는 출력 설정 분리, 비활성 수집, 반복 씬 바인딩의 핸들 보존, 새 씬 카메라 미수집, Dispose/Shutdown/파괴, 중복 검증과 재시도, 이름 비교, 두 카탈로그 타입·이름 공간과 그룹 노출 검사가 있다. C2는 Handle 동일 객체 재사용·동시 대여 분리·반복 조회 할당 검사를 추가하고 UnityCameraCatalogHandlePoolTests에서 실제 Handle 대여/반환을 별도 검사한다. 검증 결과와 기존 버추얼 로딩 연결의 주의 사항은 [설계·기록](superpowers/specs/2026-10-17-two-camera-catalogs-design.md)을 따른다.

UnityCameraCatalogPersistenceTests는 PlayMode에 진입하여 원래 씬 언로드 후 루트·비활성 자식·비활성 루트 Camera 및 Handle 유지와 부모 관계 보존, 중복 이름 실패 시 영속화가 발생하지 않는지를 검증한다. 현재 작성·컴파일 확인 상태이며 Unity 실행은 대기 중이다.

2026-10-18 전환 연결에서 CameraCatalogSystem의 Loading 조회는 루트 Global 문맥을 거치도록 수정했다. 기존 AfterLoad/BindScene의 중복 재수집 동작은 바꾸지 않았으므로 씬 재수집 전 대여 수명 종료는 여전히 호출자 책임이다. 상위 전환의 설정 복원·완료 정책은 [화면 전환 사용법](render-screen-transition.md)을 따른다.
