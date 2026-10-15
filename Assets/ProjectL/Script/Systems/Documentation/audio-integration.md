# Audio 연결과 수명

2026-09-27 변경 기준. View/Bootstrap 조립은 이 변경 범위에 포함하지 않는다.

## 서비스 접근

GlobalSystemFactory가 AudioSystem을 서비스와 모듈로 등록한다. 외부 호출자는
`resolver.GetService(SystemGroupsService.Global).AudioServiceCommand`로 접근한다.
`AudioSystemServiceKey.Audio`는 Global 내부 Reader용 키이며 루트에서는 조회하지 않는다.
Framework와 Core에는 FMOD 의존성을 추가하지 않는다.

```csharp
var audio = resolver.GetService(SystemGroupsService.Global).AudioServiceCommand;
audio.Play(playerId, requesterObject, "Attack");
audio.Stop(playerId, requesterObject, "Attack");
```

객체 지정 Play/Stop은 해당 GameObject에 부착된 등록 Player의 ID가 일치해야 성공한다.
ID가 하나만 등록되어 있어도 다른 객체나 null 요청으로 대신 재생하지 않는다.
자식 Player 자동 검색은 하지 않는다. ID 단독 호출은 해당 ID의 등록 Player가 정확히
하나일 때 사용한다. 그룹 재생은 PlayAll을 사용한다. 외부 Handle API는 추가하지 않았다.
이 객체 지정 규칙은 기존의 'ID가 하나면 requester를 무시'하는 동작에서 변경된 계약이다.

## AudioBase 소비자

- PostBindService: 씬 소비자에게 IAudioSystemService를 직접 전달한다.
- OnAudioServiceBound: 참조를 연결하는 단계. 초기 씬에서는 아직 재생할 수 없다.
- OnAudioServiceReady: AudioSystem.Initialize 완료 후 활성 소비자에게 등록 수명마다
  한 번 호출한다. 이는 전체 Runtime이나 다른 서비스의 초기화 완료 알림은 아니다.
- 비활성 소비자는 활성화될 때까지 Ready 호출을 미룬다.
- OnAudioServiceUnbound: 서비스 참조가 지워진 뒤 호출한다.
- 초기화 이후 BindScene과 sceneLoaded는 새 씬 소비자를 찾아 연결한다.

동적 생성과 풀 반환은 생성/풀 관리자가 명시적으로 연결한다.

```csharp
var consumer = Instantiate(prefab);
audio.RegisterConsumer(consumer); // 초기화가 끝났다면 활성 소비자에 Ready도 호출

audio.UnregisterConsumer(consumer); // 풀에 반환하기 전에 해제
consumer.gameObject.SetActive(false);

consumer.gameObject.SetActive(true);
audio.RegisterConsumer(consumer); // 재대여 시 새 등록 수명
```

단순 비활성화는 자동 등록 해제가 아니다. AudioBase.OnEnable/OnDestroy를 재정의하는
파생 클래스는 base 구현을 호출해야 한다. Destroy 시 소비자 등록은 자동 해제된다.
FmodEventPlayer의 OnEnable/OnDisable 등록은 별도의 FMODPlus 등록이며 그대로 유지한다.
바인딩 실패는 등록 완료로 기록하지 않아 다시 RegisterConsumer를 호출할 수 있다.

## 종료 정책

이 구성에서 하나의 Framework Runtime이 전역 AudioManager 재생 세션을 소유한다.
Shutdown은 Player 재생을 즉시 정지하고 PauseAll/GamePause를 해제하며 소비자의 서비스
참조와 sceneLoaded 구독을 정리한다. Manager GameObject와 Inspector 설정은 씬 소유로
남긴다. FMODPlus에서 직접 설정한 Manual Bus Pause는 변경하지 않는다.
Shutdown은 중복 호출을 허용하지만 종료한 AudioSystem은 재사용할 수 없다.
동일 AudioManager를 여러 Runtime이 공유하는 구성은 이 종료 정책의 지원 범위가 아니다.

## 어셈블리

- FMODPlus: AudioManager, Player, Catalog 등 런타임 코드.
- FMODPlus.Editor: 기존 Inspector, ID Drawer, Event Browser, Bank Scanner.
- 런타임에서 FMODUnityEditor 참조를 제거했다. 현재 GUI는 그 API를 직접 사용하지 않아
  Editor 어셈블리에도 해당 참조가 필요하지 않다.
- Editor가 사용하는 기존 internal API는 InternalsVisibleTo("FMODPlus.Editor")로
  제한적으로 공유한다. 공개 런타임 API로 확장하지 않는다.
- 이동한 스크립트의 .meta GUID를 유지한다.

## 검증

AudioSystemTests에 Global 조회 경계, 실패 후 바인딩 재시도, 소비자 해제·종료,
비활성 소비자의 Ready 처리, ID 중복 및 객체 지정 조회 회귀 테스트를 추가했다.
실제 오디오 출력·Bank 로드·씬 전환·Pause 중첩과 GUI 조작은 Unity PlayMode에서 확인한다.
