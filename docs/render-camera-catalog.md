# CameraCatalogSystem

일반 C# 시스템이다. 별도 MonoBehaviour, CameraSceneBinding, Inspector 참조 연결이 필요 없다. Render Core의 CameraGroup에서 CameraCatalogSystem과 CameraEffectSystem 서비스 및 모듈을 묶는다. 그룹에는 카메라 기능이 없으며 BindScene/PostBindService/Shutdown을 자식 모듈에 전달한다. 효과 시간 진행은 기존 DelayService가 담당한다.

## 역할

BindScene에서 로드된 씬의 CinemachineVirtualCamera를 직접 찾는다. 비활성 오브젝트와 비활성 컴포넌트도 포함하며, 오브젝트 이름을 대소문자 구분 키로 사용한다. 모든 로드된 씬에서 이름이 유일해야 한다. 중복 이름이면 실패하며 기존 카탈로그와 핸들은 유지한다.

수집 결과는 CameraCatalogNode에 저장한다. Node는 기존 Framework CustomPool을 사용한다. GetHandle 요청 시 풀에서 제어용 Node를 꺼내 해당 카메라 이름과 참조를 넣고 핸들로 제공한다. 카탈로그 등록용 Node와 제어 대여용 Node 모두 같은 풀을 재사용한다.

시스템의 외부 기능은 GetHandle(objectName) 하나다. 핸들에는 다음 세 기능만 있다.

- SetPriority(int): 해당 가상 카메라의 우선순위 변경.
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

JMHTest의 Main Camera와 Brain, PlayCamera, CinematicCamera를 사용한다. CameraBinding/Impulse Source 없이 PlayCamera와 CinematicCamera에 채널 1 CinemachineImpulseListener를 설정했다. TestRuner의 Camera Event Tests에서 이름으로 핸들을 받아 WASD 이동, 커스텀, Priority 전환을 확인한다. 줌/흔들림 요청은 CameraEffectSystem의 디스패처를 사용한다. 테스트용 원위치·줌 복구값은 TestRuner가 관리하고 줌 보간은 CameraZoomEvent가 처리한다.

CameraCatalogSystemTests에는 비활성 카메라 수집, Priority/줌, struct 설정 적용·값 복사·타깃 해제·활성 상태 변경, Node 재사용, 씬 재바인딩 후 오래된 핸들 차단, 중복 이름·잘못된 입력 검사가 있다. 노드는 재사용하지만 대여마다 안전한 핸들 래퍼를 생성하므로 GetHandle 호출이 무할당인 것은 아니다. 핸들은 매 프레임 받지 말고 제어 기간 동안 보관한다.

이전 카메라 구현과 제거한 씬 컴포넌트 데이터는 Temp/CameraCatalogMigrationBackup에 보관했다. Temp는 임시 폴더이므로 Unity 정리나 수동 삭제 전까지 복구 자료로 사용할 수 있다.
