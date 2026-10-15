# CameraGroup / CameraEffectSystem

```text
Render Core
└─ CameraGroup                     서비스 묶음 + 모듈 수명 전달만
   ├─ CameraCatalogSystem          이름별 수집 / 풀 Node / 제어 핸들
   └─ CameraEffectSystem           Type → ICameraEvent 등록/호출
      └─ Event
         ├─ CameraZoomEvent        카탈로그 핸들로 줌 보간
         └─ CameraShakeEvent       C# Impulse 신호 생성
```

새 MonoBehaviour나 이벤트용 GameObject를 만들지 않는다. 그룹은 효과 데이터를 해석하거나 카메라를 조작하지 않는다. Factory가 두 시스템을 그룹 내부에 등록하고 Render에는 그룹 모듈만 내보낸다. CameraGroup/CameraEffectSystem에는 Tick이 없다. 그룹은 BindScene/PostBindService/Shutdown만 전달한다.

## 호출

```csharp
var cameras = global.RenderServiceCommand.CoreGroup.CameraGroup;

cameras.CameraEffectSystem.Dispatch(new CameraZoomData
{
    CameraName = "PlayCamera",
    From = 6.46f,
    To = 4f,
    Duration = 0.35f
});

var shake = CameraShakeData.Default;
shake.Velocity = UnityEngine.Vector3.down * 0.5f;
shake.Duration = 0.2f;
cameras.CameraEffectSystem.Dispatch(shake);
```

Dispatch(ICameraEventData)는 data.GetType()으로 Dictionary<Type, ICameraEvent>에 등록된 이벤트 인스턴스를 찾아 Execute(data)를 호출한다. 각 이벤트는 ICameraEvent : IDisposable을 구현하는 클래스이며, 요청 struct는 ICameraEventData를 구현한다. 이벤트 내부에서 전달된 인터페이스 데이터를 해당 struct로 캐스팅한다. 미등록 타입은 NotSupportedException, null은 ArgumentNullException이다. Unity 메인 스레드에서 사용한다.

PostBindService에서 Global.UtilityServiceCommand.delayService를 가져온 뒤 카탈로그와 함께 CameraEventServiceContext를 구성한다. 이벤트 생성자에 이 컨텍스트를 주입하여 events[typeof(CameraZoomData)] = new CameraZoomEvent(serviceContext) 형태로 등록한다. 씬 및 서비스 바인딩 이전 또는 Shutdown 이후 요청은 InvalidOperationException이다. 씬 재바인딩/서비스 재바인딩/Shutdown은 기존 이벤트에 Dispose를 호출하고 등록을 제거한다. 재바인딩 시에는 새 이벤트 인스턴스를 생성한다.

## 이벤트별 처리 정책

- Zoom: From/To는 호출자가 정하는 절대값이다. 직교는 OrthographicSize, 원근은 FieldOfView다. Duration > 0이면 From부터 SmoothStep 보간하고, Duration == 0이면 To를 즉시 적용한다(From은 적용하지 않는다). 같은 카메라의 다음 줌 요청이 이전 요청을 대체한다. 다른 카메라의 줌은 동시에 진행한다. 시작/끝 값 검증 후 변경하며, 완료·재바인딩·종료·카메라 파괴 시 핸들을 반환한다. 취소 시 원래 값으로 복구하지 않는다.
- Shake: Uniform Impulse를 순수 C# CinemachineImpulseDefinition으로 생성한다. Velocity의 방향과 크기가 흔들림 방향/강도이며 Duration, Channel(비트 마스크), Shape(Recoil/Bump/Explosion/Rumble)를 지정한다. 여러 요청은 Cinemachine에서 합산한다. 카메라 이름으로 특정 대상을 고르는 것이 아니라 같은 채널의 Listener가 수신한다. Source 컴포넌트는 필요 없지만 가상 카메라의 CinemachineImpulseListener와 실제 출력의 Brain은 필요하다. 재바인딩·종료에는 이 처리기가 발행한 신호만 취소한다. 다른 시스템의 신호를 지우는 전역 Clear는 호출하지 않는다.

줌은 DelayService의 TickAction/EndAction으로 진행하고 읽기 전용 IDelayService.CurrentTime을 기준으로 보간한다. 직접 Time.time/Time.deltaTime을 누적하지 않는다. 흔들림 신호 자체의 진행 시간은 CinemachineImpulseManager 설정을 따르고 요청 정리는 DelayService의 EndAction에서 처리한다. 효과 종료가 Priority 복원이나 카메라 전환을 뜻하지는 않는다.

각 이벤트 인스턴스가 자신의 취소 저장소를 관리한다. 같은 카메라의 새 줌은 이전 줌을 취소하고, 이벤트 Dispose는 모든 진행 중인 요청을 취소한다. Dispose는 여러 번 호출해도 안전하며 이후 Execute는 ObjectDisposedException이다. 핸들/Impulse 참조를 즉시 정리하며 기존 Delay 콜백은 완료 여부 검사로 더 이상 대상을 변경하지 않는다. 현재 DelayService에는 개별 예약 제거 API가 없어서 취소된 예약 자체는 원래 종료 시점까지 큐에 남는다. 이 변경에서 DelayService의 다른 동작이나 전체 큐를 초기화하지 않는다.

## 새 효과 추가

1. CameraEffectSystem 폴더에 ICameraEventData를 구현하는 효과 요청 struct를 정의한다.
2. Event 폴더에 ICameraEvent 구현 클래스를 만들고 생성자에서 CameraEventServiceContext를 받는다.
3. Execute(ICameraEventData)에서 해당 struct로 캐스팅한다. 지속 처리는 컨텍스트의 DelayService에 TickAction/EndAction을 등록한다. Dispose에서 예약 콜백을 무효화하고 참조를 정리한다.
4. CameraEffectSystem.RegisterEvents에서 events[typeof(새 데이터)] = new 새이벤트(serviceContext)로 등록한다. Dispatch API와 그룹은 변경하지 않는다.

struct를 ICameraEventData 인터페이스로 전달하면 박싱이 발생하고, 이벤트에서 원래 struct로 캐스팅하면 언박싱된다. 이 API를 무박싱/무할당이라고 가정하지 않는다. 이벤트는 바인딩 때 생성하고 요청마다 재사용한다. 요청 실행에는 Delay 콜백/캡처, 줌 핸들 래퍼, Cinemachine Impulse Definition/SignalSource 할당이 존재한다. static 전역 상태에 카메라를 보관하지 않는다.

CameraEffectSystemTests에는 실제 DelayService를 통한 줌 보간·즉시 적용·중복 요청 대체·잘못된 값·씬 재바인딩·종료·대상 파괴·이벤트 클래스/컨텍스트 생성자·인터페이스 디스패치·미등록 타입·Dispose/잘못된 데이터 타입·C# 흔들림 신호·외부 신호 보존·그룹 구성/서비스 바인딩·오래된 Delay 콜백 차단 검사를 추가했다.

## 씬 테스트러너

JMHTest 씬을 Play하면 Runtime Test Runner가 프레임워크를 시작하고 Camera Event Tests 패널을 표시한다. 기존 TestRuner만 사용하며 별도 테스트 Mono를 추가하지 않았다. 두 가상 카메라에는 채널 1, Gain 1, Noise 단계, Camera Space 설정의 CinemachineImpulseListener를 붙였다. ImpulseSource는 없다.

패널 또는 TestRuner의 Camera 컨텍스트 메뉴에서 Zoom In, Shake, Zoom + Shake, Shake Burst, Replace Running Zoom, Dispose During Effects를 실행할 수 있다. Last data에 실제 전달한 ICameraEventData의 타입이 표시된다. Inspector에서 Shake Velocity/Duration/Channel/Shape/Burst Count 및 기존 줌 값들을 변경한다. 채널을 변경하면 Listener의 Channel Mask도 맞춰야 하며, 없거나 비활성/채널 불일치/Gain 0인 경우 러너가 오류를 표시한다.

Run Inspector Test List는 이동/복구, 줌, 흔들림, 동시 효과, 반복 흔들림, 줌 요청 대체, Dispose 검사, 시네마틱 전환과 동시 효과, 최종 복구를 순서대로 실행한다. Dispose 검사는 2초 효과를 시작하고 0.25초 뒤 효과 시스템을 재바인딩하여 기존 이벤트를 Dispose한다. 남아 있던 Delay 예약의 원래 종료 시점까지 기다린 후 줌 값이 유지되면 PASS, 바뀌면 FAIL을 패널/Console에 기록한다. 새 이벤트도 재등록되므로 검사 후 다시 효과를 실행할 수 있다. 검사 중 수동 조작과 WASD는 잠시 제한한다.

Stop Test List는 진행 중인 Dispose 검사도 중단하고 그 효과를 정리한다. Reset All은 이벤트들을 Dispose/재등록한 뒤 줌과 플레이 카메라 포즈를 복원하고 플레이 카메라로 전환한다. 기존 CameraTest enum 값(0~6)은 유지하고 새 테스트는 7~11로 추가하여 기존 직렬화 값을 보존했다.
