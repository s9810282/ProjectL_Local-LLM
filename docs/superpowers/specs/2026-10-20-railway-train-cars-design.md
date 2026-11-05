# 레일웨이 열차 칸 분리 설계 (Head + Body N개)

작성일: 2026-10-20. 상태: **결정 완료(2026-10-20 사용자 "모두 권장안으로") — Q-T1~Q-T5 모두 A, 코드 변경 없음.** Task 계획 [railway-train-cars-plan](../plans/2026-10-20-railway-train-cars-plan.md)(TC-a·TC-b).

사용자 요청: "지금 기차 Prefab 방식을 바꾸려 하는데, Train Head와 Train Body로 나누고 Train Body를 지정한 수만큼 늘려서 Train Head 뒤에 붙이는 방식으로."

관련: [Railway View 설계](2026-10-12-railway-view-design.md)(§6 열차 이동·애니메이션), [접촉 사망 설계](2026-10-13-railway-contact-death-design.md), [히트 타이밍 설계](2026-10-16-combat-hit-timing-design.md) H4·H7(앞면 접촉), [카메라 연출 조각 설계](2026-10-19-combat-camera-pieces-design.md) K9·K10(기차 진입·이탈), 안내 [railway-view-guide](../viewDocs/railway-view-guide.md). 아래 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `R/` = `Assets/ProjectL/Prefabs/Stage/Railway/`.

## 1. 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| T-F1 | `Train.fbx`가 10-19 민형님 커밋으로 바뀌었다. 옛 파일(516KB)은 긴 열차 모델 하나(원본 폭 1·길이 약 30.9), 새 파일(100KB)은 모델 둘 `TrainHead`·`TrainBody`, 애니메이션 테이크 없음. 두 메시 모두 원점 근처에 겹쳐 있다(cm 단위 → 가져오면 폭 1.43×길이 2.80, 폭 1.20×길이 2.70. 어느 쪽이 Head인지는 Unity에서 확인) | `303a73cc`, `Resources/Environment/Railway/Models/Train.fbx` 꼭짓점 범위 |
| T-F2 | 운영 래퍼 `R/Train.prefab` → 원본 `R/Source/Train.prefab` → `Train.fbx`의 옛 메시(fileID `-5127836520423650012`)를 가리킨다. FBX가 바뀌어 **그 메시가 없어졌을 가능성이 높다**(지금 기차가 안 보이거나 `DataBaseAssetTests.RailwayVisual_Restored…`가 실패할 수 있음, Unity 확인 필요) | 프리팹 YAML |
| T-F3 | 사용자가 만든 `R/New/Train Head.prefab`·`Train Body.prefab`은 `Train.fbx`의 변형이고 서로 다른 자식 하나씩을 지웠다. Head 변형에 남은 Transform z = −2.62 덮어쓰기는 지운 쪽(Body)의 흔적으로 보인다 — Body를 Head 뒤 2.62에 놓아 본 값이면 앞이 +Z다. 아직 바인딩·앞면·크기 보정·머티리얼은 없다 | 두 프리팹 YAML |
| T-F4 | 이동·접촉·카메라는 **Head의 앞면(`FrontContact`) 하나만** 쓴다: 경로 `RailwayTravelPath.FrontStart`, 접촉 사망·접촉 예약(`TryGetContactTime`), 카메라 진입·이탈(`LeaveTime`). 칸 수가 바뀌어도 이 시각들은 같다 | `C/Display/RailwayTravelPath.cs`, `C/Display/FieldRailwayPresenter.cs:166` |
| T-F5 | 선로는 직선만 지원한다(꺾임·교차는 설치 오류). 그래서 칸들은 늘 한 줄이고, 설치 때 한 번 놓아 이동 루트 아래 자식으로 두면 매 프레임 일이 없다 | Railway View 설계 §5, `RailwayTrackLayout` |
| T-F6 | 열차 구조는 `이동 루트 → 모델 보정 루트(Yaw·폭 배율) → TrainPrefab`이고, 프리팹 루트의 `RailwayTrainVisualBinding`이 ModelRoot·Animator·FrontContact를 알려 준다. 애니메이션은 열차당 Animator 하나 | `FieldRailwayPresenter.AddTrain:134`, `RailwayTrainVisualBinding.cs` |
| T-F7 | **열차 이동은 이미 코드다.** `FieldRailwayPresenter.Tick` → `TrackVisual.Evaluate`가 `이동 루트 위치 = Path.PositionAt(경과)`(시작 → 끝 등속 보간, Duration 고정)로 놓는다. 설정의 Clip `RoadRoller`는 Animator 루트(경로 "")의 위치(Z가 뒤로 감)·회전·크기 곡선뿐이고 Controller `TrainSkill`은 그 클립 하나를 담는다 — 그런데 코드가 클립 평가마다 그 루트 값을 되돌리므로(`RestoreRoot`) **지금도 화면에 보이는 효과가 없다**. 새 FBX에도 테이크가 없다 | `FieldRailwayPresenter.cs:376·393`, `Resources/Environment/Railway/Animations/RoadRoller.anim` |
| T-F8 | 화면 밖 시작·끝은 설정 값(OutsideCells 16 + TravelPaddingCells 18)이 원본이고, 운영 에셋 시험이 시작·끝에서 열차 렌더러 **하나**의 Bounds가 보드 밖인지 본다. 옛 열차는 이동 루트가 모델 가운데였다(앞뒤 약 12.4칸) | `FieldRailwayPresenterTests.cs:576·586`, 계획 RW4 기록 |
| T-F9 | 설정 SO·바인딩·Presenter·래퍼 프리팹은 모두 우리 작업 파일이다(공유 대상 없음). `Train.fbx`·가져오기 설정은 민형님 파일이라 고치지 않는다 | git 이력 |

## 2. 설계

### TC1. 값 자리 — 레일웨이 외형 설정

`RailwayVisualData`(View DB, 설정형 표 항목 하나):

- `trainPrefab` → **`trainHeadPrefab`** (`[FormerlySerializedAs("trainPrefab")]`로 지금 에셋 참조 유지).
- 새 **`trainBodyPrefab`**(GameObject), **`trainBodyCount`**(int, `Min(0)`, 기본 0).
- 검사: Head 필수(지금과 같음), 수는 0 이상, **수 > 0이면 Body 필수**. Body를 넣고 수 0은 허용(설정만으로 끄기).
- 수 0이면 지금과 똑같다 — 코드가 먼저 들어가도 화면 변화 없음.

### TC2. 프리팹 계약 — 연결점은 프리팹이 원본

| 프리팹 | 바인딩 | 칸 | 규칙 |
|---|---|---|---|
| Head | 기존 `RailwayTrainVisualBinding` | ModelRoot·Animator·FrontContact(그대로) + **`rearJoint`(뒤 연결점)** | 루트의 자식, ModelRoot 밖(FrontContact와 같은 자리 규칙). **몸통을 붙일 때(수 > 0)만 필수** — 옛 Train.prefab도 수 0이면 그대로 통과 |
| Body | 새 `RailwayTrainBodyBinding` | **`frontJoint`·`rearJoint`** | 둘 다 루트의 자식(루트 자신 아님). Animator 칸 없음 |

연결 규칙은 하나다: **뒤 칸의 `frontJoint`를 앞 칸의 `rearJoint` 위치에 맞춘다.** Head→Body와 Body→Body 간격이 달라도(T-F3의 2.62와 Body 길이 2.70) 프리팹에서 연결점만 옮기면 된다. 모델이 바뀌면 프리팹만 고친다(FrontContact와 같은 방식).

크기: 옛 래퍼처럼 프리팹 안에서 폭을 맞추되(설계 §6.1 "기본 폭 1"), **Head·Body는 같은 배율**(가장 넓은 칸 = 폭 1)로 둔다. 칸마다 따로 폭 1로 맞추면 Body가 Head보다 커 보인다.

### TC3. 조립 — 칸 조립 규칙 한 곳

새 정적 클래스 `C/Display/RailwayTrainCars`(가칭):

```csharp
/// 모델 보정 루트 아래에 Head와 Body N개를 놓고 Head 바인딩을 돌려준다. 설치 때 한 번.
internal static RailwayTrainVisualBinding Build(Transform correction, RailwayVisualData config)
```

1. Head를 `correction` 아래 로컬 0에 만든다 → 바인딩 없으면 예외, `binding.Validate(config)`.
2. `i = 1..N`: Body를 같은 `correction` 아래에 만든다(이름 `TrainBody_i`) → 바인딩 없으면 예외, `Validate()` → `body.position += 앞 칸 rearJoint.position − body.frontJoint.position`.
3. Head 바인딩을 돌려준다.

`FieldRailwayPresenter.AddTrain`은 `Instantiate`·`GetComponent`·`Validate` 세 줄을 이 호출 하나로 바꾸고, 콜라이더 끄기를 `correction` 전체에 한 번 한다. 나머지(경로·앞면 오프셋·`TrackVisual`)는 그대로다. Presenter(469줄)에 칸 규칙을 넣지 않고 따로 두는 이유: 설치 수명과 칸 배치 규칙은 바뀌는 이유가 다르고, 배치 규칙만 시험할 수 있다.

### TC4. 이동·접촉·카메라 — 무변경

- 칸들은 이동 루트 아래 자식이라 따라간다. 매 프레임 추가 일 0, 재생 시작·정리의 `SetActive`도 이동 루트 하나로 모든 칸에 미친다.
- 이동 루트(경로 위치) = Head 루트. 앞면 오프셋은 지금처럼 Head의 `FrontContact`에서 설치 때 잰다(T-F4) → 접촉 사망·카메라 시각 그대로.

### TC5. 애니메이션 — Head만(지금 코드 그대로)

Clip·Controller는 지금처럼 Head 바인딩의 Animator에만 건다. Body는 애니메이션이 없다. 옛 설정 값(`RoadRoller`·`TrainSkill`)을 어떻게 할지는 Q-T3.

### TC6. 꼬리 화면 밖

이동 루트가 Head라 꼬리는 `Head 뒤 + N × 칸 길이`만큼 뒤에 있다. 끝 위치(경계 밖 34.5칸)에서 꼬리가 보드 안에 남지 않아야 한다. 지금 값(Head 배율 약 0.7 × 폭 0.8 기준 칸당 약 1.5칸)이면 몸통 약 20개까지는 보드 밖이다. 보장 방법은 Q-T4.

### 하지 않는 것

- 칸마다 따로 움직이기(굽은 선로 따라가기) — 직선만이라 필요 없음(T-F5).
- 칸 사이 흔들림·연결부 애니메이션.
- 옛 `Train.prefab`·`Train_Placeholder.prefab`·`Source/Train.prefab` 삭제 — 지우지 않는다. 참조가 끊기면 보고만 한다.
- `Train.fbx` 가져오기 설정 변경(민형님 파일).

## 3. 바뀌는 파일

| 파일 | 내용 |
|---|---|
| `DataBaseSystem/Data/View/Table/Railway/Item/RailwayVisualDataSO.cs` | 칸 셋(이름 변경 + 둘), 검사 |
| `C/Display/RailwayTrainVisualBinding.cs` | `rearJoint` + 검사(수 > 0일 때) |
| 새 `C/Display/RailwayTrainBodyBinding.cs` (+ `.meta`) | Body 연결점 둘·검사 |
| 새 `C/Display/RailwayTrainCars.cs` (+ `.meta`) | TC3 조립 |
| `C/Display/FieldRailwayPresenter.cs` | `AddTrain` 몇 줄 |
| `Tests/.../FieldRailwayPresenterTests.cs`·`CombatRailwayContactSceneTests.cs` | 반사로 쓰는 필드 이름 `trainPrefab` → `trainHeadPrefab`, 운영 에셋 시험이 **모든 렌더러** Bounds를 봄 |
| 새 `Tests/.../RailwayTrainCarsTests.cs` (+ `.meta`) | 칸 수 0·N, 연결점 맞춤, Yaw·배율 아래에서도 맞음, Body·바인딩 없음 거절, 수 > 0인데 Head 뒤 연결점 없음 거절 |
| `Tests/.../DataBaseAssetTests.cs` | 운영 에셋 두 시험(원본 메시 비교·Clip 이름·앞면 위치)을 새 Head·Body 기준으로 |
| 에셋 `R/New/Train Head`·`Train Body`, `RailwayVisual_Default.asset` | 바인딩·연결점·앞면·배율·머티리얼, 설정 연결(Q-T5) |
| `docs/superpowers/viewDocs/railway-view-guide.md` | 리소스 표·값 표·칸 연결 안내 |

## 4. Phase

| Phase | 내용 | 화면 |
|---|---|---|
| TC-a | 코드(TC1~TC3) + 시험. 설정 수 0이라 지금 Train.prefab 그대로 | 변화 없음 |
| TC-b | 에셋: Head·Body 래퍼 완성, 설정에 연결·수 지정, 운영 에셋 시험 고침, 안내 문서 | Head + Body N개 |

## 5. 정할 것 — 결정 완료(2026-10-20, 모두 A)

| # | 질문 | 권장 A(결정) | 다른 안 | 이유 |
|---|---|---|---|---|
| Q-T1 | 칸 연결 간격을 어디서 정하나 | **A 프리팹 연결점**(TC2) | B 설정에 간격 숫자 하나 / C 렌더러 Bounds로 자동 | 간격은 모델의 사실이라 FrontContact처럼 프리팹이 원본(SSOT). B는 모델이 바뀌면 두 곳을 고치고 Head→Body·Body→Body 간격이 다를 때 못 맞춤. C는 겹침·틈을 조절할 수 없음 |
| Q-T2 | 몸통 수를 어디에 두나 | **A 레일웨이 외형 설정 하나**(모든 스테이지 같음) | B 스테이지마다(스테이지 필드 표시 설정 등) | 지금 열차 외형 값이 모두 이 설정 하나에 있다. 스테이지별이 필요하면 그때 옮김 |
| Q-T3 | 옛 애니메이션 설정(`RoadRoller`·`TrainSkill`) | **A 코드는 Head만 그대로, 에셋 Clip·Controller는 비움**(+ 이름을 단정하던 운영 시험 고침) | B 그대로 둠 | 이동은 코드이고 클립은 루트 곡선뿐이라 지금도 보이는 효과가 없다(T-F7). 비워도 화면이 같고, 비워야 "외형 애니메이션 없음"이 에셋에서 보인다. 클립·컨트롤러 파일은 지우지 않음 |
| Q-T4 | 꼬리 화면 밖 보장 | **A 설정 값이 원본 + 운영 에셋 시험이 열차 전체 Bounds 확인**(몸통 늘리면 Padding 같이 봄, 안내 문서에 적음) | B 경로 끝을 열차 길이만큼 자동으로 늘림 | B는 몸통 수에 따라 속도가 바뀐다(전체 시간 고정). 지금 값으로 여유가 크다(TC6) |
| Q-T5 | Head·Body 래퍼 프리팹 작업 | **A 사용자가 에디터에서**(TC-a 끝에 절차·자리 표 제공) | B Claude가 YAML로 | FBX 변형에 자식·컴포넌트를 더하는 YAML 손편집은 깨지기 쉽고, 연결점·앞면은 화면 보며 맞추는 값이다 |

## 6. 원칙 자체 점검(7기준, 권장안 기준 예상)

| 기준 | 점수 | 근거 |
|---|---|---|
| SSOT | 9 | 연결점은 프리팹, 수는 설정, 경로·앞면은 Head 한 곳 |
| SOLID | 8.5 | 칸 조립 규칙을 Presenter 밖 한 곳, Head·Body 계약 분리. Head 바인딩이 앞면과 뒤 연결점 둘을 안다 |
| FW | 9 | View DB Unity 타입 허용(P5), 설정형 표 그대로, Factory·서비스 무변경 |
| 독립성 | 9 | 접촉·카메라·재생 수명 무변경, 수 0이면 지금과 같음 |
| 캡슐화 | 8.5 | 바인딩은 읽기 전용 노출, 배치는 설치 때만 |
| C#/Unity | 8.5 | `FormerlySerializedAs`, 설치 때 한 번 배치 |
| If/Tick | 9 | 매 프레임 0, 조건은 "수 > 0" 둘(Body 필수·Head 뒤 연결점 필수) |

**예상 약 8.8/10.**
