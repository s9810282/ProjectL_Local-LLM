# Audio 연결과 수명

2026-10-12 변경 기준. Boot의 사용자 작성 AMBGM/SFX 슬롯과 프리셋을 그대로 사용한다.

## 게임 재생 연결

`AudioSystemFactory → IAudioSystemService(AudioSystem) → AudioManager → FmodEventPlayer`
경로를 사용한다. Player ID는 Manager에 정의된 `AMBGM`, `SFX` 이름으로 연결하며
새 Player·프리셋·슬롯을 자동 생성하지 않는다.

```csharp
var audio = resolver.GetService(SystemGroupsService.Global).AudioServiceCommand;
audio.SetPreset(AmbgmEvent.BGM, 3);  // Night
audio.Play(SfxEvent.UnitPick, 2);    // Rider
audio.Play(SfxEvent.UnitAttack, 6);  // ShotGun
```

| 호출/시점 | Player / Slot | 프리셋 |
| --- | --- | --- |
| Title / StageSelect / 배치 / 전투 | AMBGM / BGM | 0 / 1 / 2 / 3 |
| StageSelect 진입·이탈 | AMBGM / AMB | 없음, 재생·정지 |
| 유효한 UI 왼쪽 클릭·Submit / Hover 기능이 있는 UI 진입 | AMBGM / UIClick, UIHover | 없음 |
| 전투 승리 결과 표시 | AMBGM / UIReward | 없음 |
| 손패·필드 유닛 선택, 실제 턴 시작 | SFX / UnitPick | 직군 |
| 선택한 배치 유닛·공격 조준 취소 | SFX / UnitCancel | 직군 |
| 자발적 이동의 걸음 시작 / 사망 사건 표시 | Unit / UnitMovement, UnitDead | 아군 외형 / 적 공용 |
| 성공한 공격 사건 표시 | SFX / UnitAttack | 유닛 종류 |
| 장애물 Hit / Destroyed 사건 표시 | SFX / ObsDamaged, ObsBroken | 없음 |

BGM은 현재 인스턴스를 유지한 채 프리셋만 바꾸고, 정지 상태일 때만 Play한다.
전투 결과 확인 버튼이 수락되면 StageSelect 프리셋(1)으로 전환한다.
단순 화면 Refresh는 SFX를 재생하지 않는다. SFX의 MultiInstance 및 기존 인스턴스
프리셋 유지 정책은 Inspector 설정을 따른다. 코드에서 재생 모드를 덮어쓰지 않는다.

직군 프리셋은 MainCharacter=0, Base=1, Rider=2, Heavy=3, Cannon=4, Enemy=5다.
전투 View가 적 소유권을 우선 판정하고, 메인 캐릭터 외 직군은 사건/캐릭터 스냅샷의
`Stats.Category.Key`로 인덱스를 계산해 AudioSystem에 전달한다. 일반 직군은
`(int)CategoryKey / 10`이다. None은 재생하지 않는다. GlobalUI 덱은 공통 UI Click/Hover만 재생한다.
이동음은 아군과 적 모두 방향 회전을 마치고 각 칸으로 움직이기 시작할 때 재생한다.
뛰어넘기는 도약 구간 시작에 한 번 재생한다. 넉백·끌어오기 등 강제 이동은 이동음을 호출하지 않는다.

공격 프리셋은 MainCharacter=0(Akimbo), Akimbo=1, Sniper=2, ShotGun=6,
Gravekeeper=7, CactusCannon=9, Ballista=11이다. Demon은 사용자 요청으로 보류한다.
이 매핑은 CombatSceneEventPresenter에 있다. FMOD label 값과 프리셋 배열 인덱스는 구분한다.
AudioSystem의 Play·SetPreset은 성공 시 true를 반환하며, 존재하지 않거나 비활성인 프리셋,
누락된 Player·슬롯·이벤트, 실제 재생 실패는 예외로 알린다. 초기화 전·종료 후 요청도 예외다.
인자 오류는 ArgumentException 계열, 구성·재생 상태 오류는 InvalidOperationException이며
실패한 Player·슬롯·이벤트·프리셋 정보를 포함한다. 기본 Play도 같은 검사를 거친다.
Play의 프리셋 -1은 프리셋을 변경하지 않고 재생한다는 뜻이며 무음 값이 아니다.
PlayAll은 모든 대상의 슬롯 구성을 먼저 검사하고, 대상이 없거나 하나라도 재생에 실패하면
예외를 던진다. 실제 재생 실패 전에 시작한 소리는 되돌리지 않는다.
정상적인 조회 실패(IsPlaying)·정지·Try API의 반환 계약은 그대로다.
현재 Object 전용 슬롯은 없으므로 Object 재생은 슬롯 구성 후 추가 연결한다.

UIInputBinding은 UIAudioScope에서 서비스를 받아 유효 입력을 직접 전달한다.
UISystem이 씬 Canvas와 유지 Global Canvas에 Scope를 연결하고, 동적 자식 입력은 Bind/OnEnable/
부모 변경 때 이를 상속한다. 포인터 콜백에서는 검색·구독·클로저를 만들지 않는다.
Hover 중복 진입, 비활성·입력 불가 상태에서는 재생하지 않으며 덱 핀 동작은 유지한다.
Hover 사운드는 기본적으로 꺼져 있으며 실제 Hover 기능이 있는 슬롯만
`UIInputBinding.Attach(..., hasHover: true)`로 연결한다. 현재 덱 유닛 항목,
유닛 카드 시너지·기능 버튼, 상태 아이콘, 아티팩트 항목이 대상이다.

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
2026-10-26: AudioPlaybackFailureTests와 초기화 전·종료 후 요청, UI 입력 오디오 회귀를
Unity EditMode에서 실행해 15건 통과·실패 0·스킵 0을 확인했다. 기본 재생과 프리셋 변경,
객체 지정·그룹 재생의 실패 예외를 검사하며, 실제 음향 장치 출력 검증과는 구분한다.
실제 오디오 출력·Bank 로드·씬 전환·Pause 중첩과 GUI 조작은 Unity PlayMode에서 확인한다.
