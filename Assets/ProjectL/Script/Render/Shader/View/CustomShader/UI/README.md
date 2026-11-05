# 모래바람 여정 UI

실사용 프리팹: `Prefabs/SandstormJourney.prefab`

모래가 왼쪽에서 오른쪽으로 화면을 덮고, 측면 마차와 말이 계속 움직이는 동안 대기한 뒤, 전체가 서서히 투명해지는 UGUI 시각 위젯이다. 로딩·씬 전환·UI 등록은 호출자가 담당한다.

## 시스템 로딩 연결

운영 Boot가 생성하는 `Assets/ProjectL/Prefabs/UI/GlobalUICanvas.prefab`에 요청형 `SandstormLoadingUI`를 연결했다. 기존 Global Context의 `LodingServiceCommand.LodingService.Load(sceneIndex)`와 이를 사용하는 LifeCycle 전환마다 자동으로 재생한다. 호출자에서 별도의 UI Open/Hide나 타이머를 추가하지 않는다.

흐름은 덮기·여정 페이드인 완료 → 한 프레임 가림 유지 → 기존 씬 전환/등록 노드 → 전체 페이드아웃 완료 → UI 닫기다. 로딩 완료 후 기본 1.05초 동안 모래·마차·말·문구가 함께 사라진다. `SandstormTransitionVisual.revealDuration`으로 시간을 조절한다. `IsLoading`은 이 전체 구간 동안 true다. UI는 `UIId.Loading`, GlobalUIBase, Parallel이며 시작 때 자동으로 열리지 않고 씬 교체 때 재사용된다. 모래바람은 unscaled 시간으로 동작한다. 저수준 구성에서 표현을 연결하지 않으면 기존 로딩 동작을 유지한다.

조립 메뉴: `ProjectL > Render > UI > Install Journey Loading In Global Canvas`. 기존 공용 프리팹에 한 번만 추가하고 중복 생성하지 않는다. 실제 씬 로딩 검증은 `Verify Journey Actual Scene Loading`(Ctrl+Shift+Alt+J)이며 검증용 씬/시작 설정만 사용하고 종료 후 Build Settings와 시작 씬을 복원한다.

다른 코드가 `SceneManager.LoadScene*`를 직접 호출하면 이 시스템을 거치지 않는다. 운영 전환은 기존 LodingService 경로를 사용한다. 아래 API는 위젯 자체를 다른 등록 화면에서 재사용할 때의 계약이다.

## 사용 계약

1. 기존 UISystem에 등록된 화면의 Canvas 아래에 프리팹을 자식으로 둔다. 프리팹 자체에는 Canvas나 EventSystem, UIId가 없다. 등록 화면의 Open/Hide 수명 안에서 사용한다.
2. `SandstormTransitionVisual` 참조를 직렬화해서 소유한다. `Cover()`로 가리기를 요청한다.
3. `IsCovered`가 참이 된 뒤 호출자가 로딩한다. 가려진 상태에는 자동 종료 시간이 없고 말·마차·모래는 계속 움직인다.
4. 호출자가 준비를 마치면 `Reveal()`을 요청한다. `IsHidden`이 참이면 전환이 끝난 것이다.
5. 취소·실패 시 `ResetVisual()`로 즉시 숨긴다. Disable도 같은 초기화를 수행한다. 필요하면 다음 Open에서 재사용한다.

```csharp
// 이미 등록된 화면이 가지고 있는 시각 위젯 참조에 호출한다.
visual.Cover();
// 이후 Tick/코루틴에서 visual.IsCovered를 확인한다.
// 로딩은 기존 소유자가 실행한다.
visual.Reveal();
// 이후 visual.IsHidden을 확인한다.
```

- 기본 시간원은 `Time.unscaledDeltaTime`이다. timeScale이 0이어도 움직임과 전환이 진행된다.
- 외부 Tick으로 구동하려면 `UseExternalClock = true`를 먼저 설정하고 `Advance(비음수 초)`를 전달한다. 자동 시간원에서 직접 Advance하면 중복 Tick을 막기 위해 예외를 던진다.
- `ShowCovered()`는 즉시 완전 가림·여정 표시 상태로 들어간다.
- Covering/Covered의 중복 Cover, Revealing/Hidden의 중복 Reveal은 진행을 재시작하지 않는다.
- Covering 중 Reveal, Revealing 중 Cover는 예외다. 방향을 강제로 바꿔야 한다면 Reset 후 시작한다.
- 상태를 바꾸는 호출은 활성화된 위젯에서만 한다. 누락된 참조와 무효 시간은 상태 변경 전 거부한다.
- 표시 중 루트 CanvasGroup은 하부 UI 포인터를 막고, Hidden/Disable에서는 해제한다. 키보드·게임 입력 Layer 변경은 하지 않는다.

## 조절 위치

| 대상 | 에셋/컴포넌트 |
|---|---|
| 모래 색·난기류·속도·작은 모래 농도 | `Materials/Sandstorm.mat` |
| 한 걸음 시간·보폭·접지 비율·발 높이·차체/캐릭터 흔들림 | `Materials/JourneyMotion.asset` |
| 덮기/걷기 시간·하위 참조·외부 시간원 | 프리팹 루트 `SandstormTransitionVisual` |
| 마차·말·글자 배치 | 프리팹 `Journey` 하위 RectTransform |
| 글자·폰트 | `Journey/JourneyLabel` |

모래 머티리얼은 재생 시 인스턴스별로 복제된다. 프로젝트 머티리얼 에셋에 시간/진행을 쓰지 않는다. 두 전환을 동시에 생성해도 서로의 시간을 바꾸지 않는다. 복제한 머티리얼은 Destroy에서 해제된다.

`Journey`는 1000×563 기준 배치를 화면 안에 균등 축소/확대한다. 모래층은 부모 RectTransform 전체를 채운다. UGUI Mask/RectMask2D와 CanvasGroup 알파를 지원한다. RectMask2D의 가장자리 부드러움은 별도 연출로 보장하지 않는다.

## 포함 리소스와 의존성

- `SourceArt`의 생성 PNG를 원본으로 `Textures/JourneyAtlas.png`를 빌드했다. 런타임에서 원본 3D 모델·브라우저·이미지 생성 서비스·사용자 임시 폴더를 사용하지 않는다.
- 아틀라스에는 마차와 분할된 말 몸통/다리, 바닥용 흰 픽셀이 들어 있다. 걷기는 메시를 연속 변형하고 바퀴는 회전 UV를 사용한다.
- Runtime 어셈블리는 UnityEngine.UI만 참조한다. 기존 Core/Systems/Factory 서비스에 의존하지 않는다.
- 한국어 글자는 기존 `Assets/ProjectL/Resources/Font/Hahmlet-Bold SDF.asset`와 그 머티리얼을 참조한다. 이 기존 폰트는 수정하지 않았다.
- 현재 Unity 6000.0.73f1, URP 17.0.4, UGUI 2.0.0 기준이다.

## 재생성·검증

- `ProjectL > Render > UI > Build Sandstorm Journey Assets`: 아틀라스/노이즈/프리팹 재생성. 프리팹 배치를 Builder 정의로 다시 작성하므로 수동 배치 변경은 먼저 별도 프리팹 변형으로 보관한다. 기존 MotionSettings 및 머티리얼의 수동 외형 값은 유지한다.
- `… > Validate Sandstorm Journey Assets`: 재생성 후 임시 검증용 씬에서 가로·세로·숨김을 렌더하고 셰이더 오류를 검사한다. 결과는 `Verification`에 쓴다. 기존 씬을 저장하거나 게임 로딩에 연결하지 않는다.
- `… > Run Journey EditMode Tests`: 씬 교체 없이 이 패키지 NUnit 테스트를 에디터 메인 스레드에서 동기 실행한다. Unity Test Runner 창의 실행과는 별도이며, 같은 테스트는 EditMode Test Runner에서도 발견된다. 결과는 `Verification/EditMode.xml`과 요약 파일이다.

설계·Task·검증 사실은 `IMPLEMENTATION.md`를 따른다.
