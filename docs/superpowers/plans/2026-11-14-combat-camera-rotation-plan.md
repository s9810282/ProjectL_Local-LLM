# 전투 카메라 회전 Task 계획

> 상태: **TP-1~TP-15 그대로(사용자 "그대로, CR-a 진행해"). CR-a~CR-c 완료(2026-11-14 사용자 "CR-c까지 쭉 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 기록 §7).** 검토 L2·L3 반영(§7.4), L1 반영(§7.5). **가운데 버튼 끌기 회전 CR-d 완료(2026-11-15 사용자 "A안으로 설계에 추가하고 진행해, Z/C도 유지해야함", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, §8·§7.6)**. 남은 것: Unity 확인 Ua·Ub(§5)·Uc(§8), 민형님 공유 §58 전달.

- 작성일: 2026-11-14.
- 설계: [전투 카메라 회전 설계](../specs/2026-11-14-combat-camera-rotation-design.md)(CR1~CR7).
- 결정(2026-11-14 사용자 "권장안대로 하고 Task 계획 써줘"): Q-CR1~Q-CR3 모두 A, P-CR1~P-CR4 그대로.
  - Q-CR1 A: 자유 회전만 만든다. 단계 회전은 나중에 설정 칸 하나와 목표 규칙 함수 하나로 더한다.
  - Q-CR2 A: 이동 제한은 지금 값 그대로 쓴다. 바닥에 고정된 사각형이 된다.
  - Q-CR3 A: 회전 범위는 제한하지 않는다(한 바퀴).
- 상시 지시: MSBuild·새 시험 없음, 컴파일 미확인으로 보고, 커밋·push는 사용자가 한다.
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `SI/` = `…/ViewSystem/StageInput/`, `DVC/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`.

## 0. 착수 조건과 확인한 것

- 고칠 파일은 모두 우리 줄이다(`git blame` 작성자 Droute). `CombatCameraFactory`와 그 호출 두 줄도 우리가 썼지만 Factory 폴더라 민형님 공유에 적는다.
- 고칠 파일에 커밋 전 변경이 없다(2026-11-14 `git status`). 스테이지에 올린 필드 색 작업과 겹치는 파일도 없다.
- 시험:
  - `ICombatCameraControl`·`IStageInputReceiver`를 구현한 시험 가짜는 없다.
  - 우리 `Assets/Tests/EditMode/CoreIntegration/CombatCameraZoomPoseTests.cs`는 `CombatCameraZoomPosePath.Evaluate`와 `CombatCameraSettingsData` 생성자(인자 열 개·열네 개)만 부른다. 둘 다 그대로 컴파일되게 한다(TP-7).
- 입력 액션 에셋은 C# 래퍼를 만들지 않는다(`generateWrapperCode: 0`). JSON만 고친다.
- `InputController.Float`는 Performed·Canceled 때 값을 넘긴다. 키를 떼면 0이 와서 회전이 멈춘다.
- 요청 열거 `CombatInputRouter.Request`의 계측 마커 표(`CombatInputProfiler`)는 모든 값을 훑어 값으로 색인하므로 자리와 무관하다. 새 값은 기존 번호가 바뀌지 않게 맨 끝에 둔다.

## 1. 계획에서 정한 것

설계와 다른 것은 ★로 표시했다.

| # | 정한 것 | 까닭 |
|---|---|---|
| TP-1 | 회전 각 부호: Z(축 −1)는 Unity 위 축의 **양의** 각이다. 이때 카메라는 보던 점 둘레로 자기 왼쪽으로 간다. 식은 `_yaw -= direction × rotateSpeed × deltaTime`이다 | P-CR2. Unity에서 위 축 양의 회전은 앞(+Z)을 오른쪽(+X)으로 돌리므로, 카메라 뒤쪽 벡터는 카메라 왼쪽으로 돈다 |
| TP-2 | 이동 입력 변환의 기준 축은 지금 오프셋이 쓰는 축과 같다. 줌 자세 모드는 **줌아웃 끝 자세**, 기존 모드는 **기준 자세**다. 내려다보는 정도(sin)는 시점을 만들 때 첫 카메라에서 한 번 읽는다 | 오프셋·제한을 그대로 두려면 같은 축이어야 한다(CR3) |
| TP-3 | 기준 축에 롤이 있으면(오른쪽 축이 수평이 아니면, \|right.y\| > 0.001) 시점을 만들 때 예외. 아래를 보지 않으면(sin ≤ 0.01) 예외 | 이동 변환이 롤 0을 전제로 한다. 지금 프리팹은 (45, −45, 0)이고 줌아웃 회전 오프셋은 0이다 |
| TP-4 | 방위각이 0이면 회전 중심 계산을 아예 건너뛴다. 그래서 지금과 같은 수가 비트 단위로 나온다 | CR-a의 "화면 변화 없음"을 수로 보장하려고 |
| TP-5 | 줌 자세 모드의 회전 중심은 어댑터가 매 프레임 구한다. 지금 Transform 기준의 줌아웃 자세(이동 포함)에서 바닥 점을 찾는다. 경로에 입구 `ZoomOutAt` 하나를 더한다 | Play 중 Transform 편집(씬 공용 카메라 설계 D0.1)을 중심도 따라가게 |
| TP-6 ★ | 기존 모드(`useZoomPose` 끔)의 `Place`는 위치와 회전을 함께 쓴다. 지금은 위치만 쓴다 | 회전하려면 회전을 써야 한다. 운영은 줌 자세 모드라 화면 차이가 없다. 차이는 기존 모드에서 Play 중 회전 편집이 이동 때 덮이는 것뿐이다 |
| TP-7 | `rotateSpeed`: SO 기본 90. `CombatCameraSettingsData` 생성자·`FindError`에는 **끝 선택 인자**(기본 0 = 회전 끔)로 붙인다. 기본 에셋에 `rotateSpeed: 90` 한 줄을 넣고, 코드와 한 커밋에 넣는다 | 우리 시험의 생성자 호출을 그대로 컴파일되게 |
| TP-8 | 방위각은 `Mathf.DeltaAngle(0f, yaw)`로 −180~180에 감아 둔다 | 오래 돌려도 수가 커지지 않게 |
| TP-9 | 액션 `Rotate`는 Value·Axis에 1D Axis 묶음 `ZC`(negative = `<Keyboard>/z`, positive = `<Keyboard>/c`)로 둔다. 둘 다 누르면 0이다 | 이동 `Pan`과 같은 Value 꼴이라 떼면 Canceled 0이 온다 |
| TP-10 | 요청 열거에 `CameraRotate`를 맨 끝에 더하고 `IsCameraRequest`에 넣는다. 그러면 확인창 막기 통과·조정자 가벼운 경로·카메라 입력 전달이 이동·줌과 같아진다 | 판정 하나를 세 곳이 이미 같이 쓴다 |
| TP-11 | 필드 평면 높이는 `FieldView.CellsRoot`의 월드 y다. Factory가 읽어 `CombatCameraFactory.Create` 넷째 인자로 넘기고, 시점 생성 인자가 된다. CellsRoot가 없으면 Factory가 원인 문구로 예외 | P-CR4. 운영·특성 확인 두 호출 모두 필드가 이미 준비돼 있다 |
| TP-12 | 회전은 줌 대여·연출 줌·흔들림 중에도 받는다 | 설계 CR4. 렌즈만 바꾸는 연출이라 겹치지 않는다 |
| TP-13 | 새 `.cs`는 `CombatCameraOrbit` 하나다. `.meta`는 Unity가 만들고 커밋 때 함께 넣는다 | — |
| TP-14 | 검토는 CR-c에서 묶음 끝에 한 번(새 읽기 전용 에이전트). Unity 확인은 Phase마다(Ua·Ub) | 최근 작업과 같은 방식. 메모리 규칙("Phase마다 새 에이전트 검토")대로 Phase마다 하길 원하면 바꾼다 |
| TP-15 | 평가 아티팩트 '적용 기록'에는 차수를 더하지 않는다 | 평가 목록 밖의 새 기능이다(피격 외곽선과 같은 처리) |

## 2. Phase CR-a 계산 (화면 변화 없음)

### Ta.1 새 `C/Presentation/Cameras/CombatCameraOrbit.cs`

상태 없는 `internal static class`. 한국어 XML 문서를 단다.

- `GroundPoint(Pose pose, float groundY)`
  - `forward = pose.rotation * Vector3.forward`
  - `forward.y`가 −0.0001 이상이면(바닥을 보지 않음) `InvalidOperationException`
  - `t = (groundY − pose.position.y) / forward.y`, 결과 `pose.position + forward × t`
- `Apply(Pose pose, Vector3 pivot, float yaw)`
  - `yaw == 0f`면 `pose`를 그대로 돌려준다(TP-4)
  - `q = Quaternion.AngleAxis(yaw, Vector3.up)` → `new Pose(pivot + q * (pose.position − pivot), q * pose.rotation)`
- `ToBasePan(Vector2 screen, float yaw, float pitchSin)` — 지금 화면 축의 이동량을 기준 화면 축 오프셋 변화로 바꾼다
  - `yaw == 0f`면 `screen` 그대로(TP-4)
  - `a = screen.x`, `b = screen.y / pitchSin`(바닥 앞 방향 거리)
  - `bx = a·cos + b·sin`, `by = −a·sin + b·cos`(cos·sin은 yaw 라디안). 위 축 양의 회전이 앞을 오른쪽으로 돌리는 Unity 규칙에서 나온 식이다
  - 결과 `(bx, by × pitchSin)`
- `FindFrameError(Quaternion rotation)` — TP-3 검사. 문제가 없으면 null, 있으면 문구
- `PitchSinOf(Quaternion rotation)` = `−(rotation * Vector3.forward).y`

### Ta.2 `C/Presentation/Cameras/CombatCameraZoomPosePath.cs`

- `internal Pose ZoomOutAt(Vector2 offset, Pose currentReference)` = `EvaluateAtReference(_maxSize, offset, currentReference)`. 진행률 0이라 줌아웃 끝 자세다(TP-5).
- `Evaluate`·`EvaluateAtReference`는 바꾸지 않는다(우리 시험이 부름).

### Ta.3 `C/Presentation/Cameras/CombatCameraZoomPose.cs`

- 필드 `_yaw`(float), `_groundY`(float).
- `Bind(path)` → `Bind(CombatCameraZoomPosePath path, float groundY)`. 유한한 수인지 검사하고, `_yaw = 0`.
- `SetOffset(Vector2)` → `SetView(Vector2 offset, float yaw)`.
- `ApplyPose`: 지금 계산한 `pose` 뒤에, `_yaw != 0f`일 때만 다음을 한다.
  - `pivot = CombatCameraOrbit.GroundPoint(_path.ZoomOutAt(_offset, reference), _groundY)`
  - `pose = CombatCameraOrbit.Apply(pose, pivot, _yaw)`
  - Raw 자세만 덮고 흔들림 보정은 그대로다.
- `Unbind`: `_yaw = 0`.
- 부르는 곳은 `CombatCameraView` 하나다(`SetOffset` 검색 결과, 스테이지 맵의 같은 이름은 다른 클래스).

### Ta.4 설정 `rotateSpeed`

- `DVC/CombatCameraSettingsSO.cs`: `[Header("회전")]` 묶음을 이동·줌 사이에 둔다.
  - `[SerializeField, Min(0f)] private float rotateSpeed = 90f;`
  - 툴팁: "Z·C를 누르는 동안 카메라가 화면 가운데 바닥 점 둘레로 도는 빠르기(초당 도)입니다. 0이면 회전을 끕니다. 내려다보는 각은 바뀌지 않습니다."
  - `CreateData`·`FindError` 호출 끝에 `rotateSpeed`.
- `DVC/CombatCameraSettingsData.cs`:
  - 생성자·`FindError` 끝 선택 인자 `float rotateSpeed = 0f`(TP-7), 속성 `RotateSpeed`.
  - 검사 "rotateSpeed는 0 이상이어야 합니다"(`IsNonNegative`).
- 기본 에셋 `Assets/ProjectL/DataBaseSO/ViewGroup/CombatCamera/CombatCameraSettings_Default.asset`에 `rotateSpeed: 90` 한 줄(직렬화 순서 = 필드 순서).

### Ta.5 `C/Presentation/Cameras/CombatCameraView.cs`

- 생성자 끝 인자 `float groundY`. 유한한 수가 아니면 예외.
- 카메라가 있으면:
  - 이동 기준 축 자세를 구한다. 줌 자세 모드면 `_posePaths[0].Evaluate(ZoomMax, 0)`의 회전, 아니면 `BaseRotation`(TP-2).
  - `CombatCameraOrbit.FindFrameError` 검사(TP-3). 문제가 있으면 카메라 이름을 넣은 예외.
  - `_panPitchSin = PitchSinOf(...)`를 기억한다.
- 필드 `_yaw`. `Begin`·`Restore`에서 0으로 둔다.
- `Begin`: 어댑터 `Bind(_posePaths[i], _groundY)`.
- `Pan`: 지금 이동량 `velocity × (deltaTime × (_size / _initialSize))`를 `CombatCameraOrbit.ToBasePan(이동량, _yaw, _panPitchSin)`으로 바꾼 뒤 오프셋에 더하고, 제한은 지금 그대로(`ClampOffset`). 방위각 0이면 지금과 같은 수다.
- 새 `public bool Rotate(float direction, float deltaTime)`:
  - 카메라 없음, `deltaTime` 0 이하, `direction == 0`, `RotateSpeed == 0`이면 false.
  - `_yaw = Mathf.DeltaAngle(0f, _yaw − Mathf.Clamp(direction, −1f, 1f) × _settings.RotateSpeed × deltaTime)`(TP-1·TP-8).
  - `Place()` 뒤 true.
- `Place`:
  - 줌 자세 모드: `_poseAdapters[i].SetView(_offset, _yaw)`.
  - 기존 모드: `panned = new Pose(BasePosition + BaseRotation × local, BaseRotation)`. 방위각이 0이 아니면 `Apply(panned, GroundPoint(panned, _groundY), _yaw)`. 그 뒤 `SetPositionAndRotation`(TP-6).
- 클래스 요약에 회전(CR1·CR3)을 한 줄 더한다.

### Ta.6 계약·세션

- `C/Input/Contracts/ICombatCameraControl.cs`: `bool Rotate(float direction, float deltaTime);`
  - 문서: "−1 = 카메라가 왼쪽으로, +1 = 오른쪽으로(Z·C). 실제로 돌았으면 true. 내려다보는 각은 바꾸지 않습니다."
- `C/Presentation/Cameras/CombatCameraSession.cs`: `public bool Rotate(float direction, float deltaTime) => _begun && _view.Rotate(direction, deltaTime);`

### Ta.7 Factory 연결

- `F/CombatCameraFactory.cs`: `Create(binding, catalog, effects, float groundY)` → `new CombatCameraView(window, snapshots, settings, groundY)`.
- `F/CombatSceneDisplayFactory.cs:87`: 넷째 인자로 `root.Field.CellsRoot`의 월드 y. CellsRoot가 없으면 `InvalidOperationException("[CombatSceneDisplayFactory] 필드 Cells Root가 없어 카메라 회전 중심 높이를 정할 수 없습니다.")`(TP-11).
- `F/CombatTraitPreviewFactory.cs:61`: 넷째 인자로 같은 방식의 `field.CellsRoot` 월드 y(위에서 만든 사본).
- 읽는 도우미가 두 파일에 같은 식으로 생기면 `CombatCameraFactory` 안의 작은 정적 함수 `GroundHeightOf(FieldView field)` 하나로 둔다.

### Ta.8 자체 확인 (코드 대조, 빌드 없음)

- `Rotate`를 부르는 곳이 아직 없다(CR-b 전).
- `Create`·`Bind`·`SetView`를 부르는 곳이 모두 바뀌었다.
- 우리 시험이 부르는 `Evaluate`·설정 생성자 서명이 그대로다.
- 방위각 0인 갈래가 지금 식과 같은 수를 내는지 다시 읽는다(TP-4).

## 3. Phase CR-b 입력·값

### Tb.1 액션 에셋 `Assets/ProjectL/Input/PlayerInputActions.inputactions`

- `CombatCamera` 맵 `actions`에 `Rotate` 추가: `type: Value`, `expectedControlType: Axis`, `initialStateCheck: true`, 새 GUID.
- `bindings`에 1D Axis 묶음 `ZC`(`path: 1DAxis`, `isComposite: true`)와 부분 둘(`negative` = `<Keyboard>/z`, `positive` = `<Keyboard>/c`, `isPartOfComposite: true`)을 추가한다. 새 GUID를 쓴다.
- Layer(`StageInputLayers`)는 바꾸지 않는다. `CombatCamera` 맵이 이미 배치·전투 Layer에 들어 있다.

### Tb.2 `SI/StageInputNames.cs`

- `CombatCamera.Rotate = "Rotate"`.

### Tb.3 수신 계약 사슬

- `SI/IStageInputReceiver.cs`: `void CameraRotateChanged(float value);`
  - 문서: "배치·전투 단계: 카메라 회전 키의 값이 바뀌었습니다(−1 = Z, +1 = C, 떼면 0). 누르고 있는 동안 반복해서 오지 않으므로 받는 View가 마지막 값을 기억합니다."
- `SI/StageInputReceiverSlot.cs`: `public void CameraRotateChanged(float value) => _receiver?.CameraRotateChanged(value);`
- `SI/CombatCameraInputController.cs`: `input.Float(StageInputNames.CombatCamera.Rotate, _receiver.CameraRotateChanged);`

### Tb.4 `C/Input/CombatInputRouter.cs`

- `Request` 맨 끝에 `CameraRotate`(TP-10), 문서는 이동·줌 줄과 같은 꼴.
- `IsCameraRequest`에 `|| request == Request.CameraRotate`.

### Tb.5 `C/Input/CombatInputReceiver.cs`

- `public void CameraRotateChanged(float value) => Receive(CombatInputRouter.Request.CameraRotate, value, 0f);`

### Tb.6 `C/Input/CombatSceneInput.cs` `Route`

- 이동·줌 갈래 옆에 `if (request == CombatInputRouter.Request.CameraRotate) { _camera?.RotateChanged(screenX); return; }`.
- 요약 주석의 "카메라 입력" 문장은 세 요청을 다 덮으므로 그대로 둔다.

### Tb.7 `C/Input/Cameras/CombatCameraInput.cs`

- 필드 `_rotateAxis`(float).
- `internal void RotateChanged(float value)`: 값을 −1~1로 잘라 기억한다. 계측 표지 `Combat.Action.Interaction.Camera.Rotate`를 이동·줌 표지와 같은 꼴로 단다.
- `Tick`의 `canMove` 갈래, 이동 줄 뒤: `changed |= _camera.Rotate(_rotateAxis, deltaTime);`. 돌았으면 포인터를 다시 집는 규칙은 그대로 탄다.
- `Clear`: `_rotateAxis = 0f`(포커스 상실 때 멈춤).
- 클래스 요약에 회전 한 줄.

### Tb.8 자체 확인

- `IStageInputReceiver` 구현은 둘뿐이다(슬롯·전투 수신).
- 열거 새 값은 맨 끝에 있다.
- `IsCameraRequest`를 쓰는 세 곳(수신 창구·입력 분배·연결 입력 묶음)이 새 값을 같이 탄다.

## 4. Phase CR-c 문서·공유·검토

- Tc.1 문서
  - [줌 자세 안내](../viewDocs/combat-camera-zoom-pose-guide.md): §1.1 뒤에 "회전" 절(값 `rotateSpeed`, Z·C, 중심, 내려다보는 각 고정, 줌인 중 중심 어긋남 P-CR3, 재진입 처음 방향). §3 기대 동작에 두 줄.
  - [스테이지 입력 안내](../viewDocs/stage-input-guide.md) §13에 `Rotate` 줄.
  - [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md): `Presentation/Cameras/` 줄에 `CombatCameraOrbit`, `Input/Cameras/` 설명에 회전, 갱신 줄 하나.
  - [씬 공용 카메라 설계](../specs/2026-11-08-combat-scene-camera-design.md) §1 범위 밖의 "새 회전 조작"에 "대신함(2026-11-14, 카메라 회전 설계)" 한 줄.
- Tc.2 공유: 민형님 [Factory 공유](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) 다음 절(지금 마지막은 §57). `CombatCameraFactory.Create` 넷째 인자와 호출 두 줄을 적는다(미전달). 다른 작업자 줄은 없다.
- Tc.3 기록: 설계 §8 구현 메모, 이 계획 §7 실행 기록, CLAUDE.md.
- Tc.4 묶음 끝 검토(새 읽기 전용 에이전트 하나, TP-14). 볼 것:
  - 컴파일(서명·호출처·using·`.meta`)
  - 방위각 0에서 HEAD와 같은 수
  - 회전 부호(TP-1)와 이동 변환식
  - 줌 자세 어댑터의 흔들림 보정 보존
  - 수명(Begin·Restore·Unbind·Clear)
  - 입력 사슬 빠짐
  - 7기준(SSOT·SOLID·FW·독립성·캡슐화·C#/Unity·If/Tick)

## 5. Unity 확인

### Ua (CR-a 뒤, 화면이 바뀌면 안 됨)

1. 전투 진입 첫 화면이 지금과 같다.
2. WASD·화살표·가장자리 이동과 이동 제한이 지금과 같다.
3. 휠 줌·줌 자세(줌인 때 각이 낮아짐)가 지금과 같다.
4. 포효·기차 연출 줌과 피격 흔들림이 지금과 같다.
5. 전투를 나갔다 다시 들어와도 같다.
6. 특성 확인 씬(`Assets/Scenes/Dev/TraitActivationPreview.unity`)의 카메라가 같다.
7. 콘솔에 새 오류가 없다.

### Ub (CR-b 뒤)

1. Z를 누르면 카메라가 보던 점 둘레로 자기 왼쪽으로 돈다. 화면 속 바닥은 시계 반대 방향으로 도는 것처럼 보인다. C는 반대다.
2. 떼면 바로 멈추고, Z·C를 같이 누르면 돌지 않는다.
3. 줌아웃 상태에서 화면 가운데 바닥 점이 제자리에 있다.
4. 돌린 뒤 W·A·S·D와 가장자리 이동이 화면 방향대로 간다.
5. 돌린 뒤에도 이동 제한이 같은 바닥 범위에서 걸린다.
6. 돌리는 동안 마우스 아래 칸의 호버·커서가 따라온다.
7. 줌인한 채 돌려도 화면 가운데가 제자리다(이동 제한 끝에서는 조금 밀릴 수 있음). 돌린 뒤 줌인·줌아웃하면 화면 위쪽으로 나아가고 물러나며, 옆으로 미끄러지지 않는다(설계 §8 검토 L1).
8. 흔들림·포효 줌 중에도 돌고, 흔들림이 화면 기준 방향 그대로다.
9. 확인창·결과 화면에서는 돌지 않는다.
10. 배치 끌기·공격 모드 중에도 돈다.
11. 한 바퀴 넘게 돌려도 이상 없다.
12. 전투를 나갔다 들어오면 처음 방향이다.
13. Play 중 카메라 Transform을 고쳐도 화면이 따라간다(D0.1).
14. **배경 뒷면·잘린 가장자리·그림자 방향·가까운 자름면**(설계 §5). 결과에 따라 Q-CR3 B(범위 제한)를 다시 본다.

## 6. 커밋 묶음

| 묶음 | 파일 |
|---|---|
| CR-a | 새 `CombatCameraOrbit.cs`(+ `.meta`), `CombatCameraZoomPosePath.cs`, `CombatCameraZoomPose.cs`, `CombatCameraView.cs`, `ICombatCameraControl.cs`, `CombatCameraSession.cs`, 설정 SO·Data, **기본 설정 에셋(코드와 한 커밋)**, Factory 셋 |
| CR-b | 액션 에셋, `StageInputNames`·`IStageInputReceiver`·`StageInputReceiverSlot`·`CombatCameraInputController`, `CombatInputRouter`·`CombatInputReceiver`·`CombatSceneInput`·`CombatCameraInput` |
| CR-c | 문서·공유·CLAUDE.md |

- 묶음 셋을 한 커밋으로 해도 된다.
- 스테이지에 올라가 있는 필드 색 작업과는 파일이 겹치지 않는다.

## 7. 실행 기록

(착수 뒤 Phase마다 적는다.)

### 7.1 CR-a 계산 (2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Ta.1~Ta.7 계획대로. 새 `CombatCameraOrbit.cs`(`.meta`는 Unity가 만듦). 계획과 다른 점: Factory의 바닥 높이 읽기를 처음부터 `CombatCameraFactory.GroundHeightOf(FieldView)` 하나로 두었다(Ta.7의 "두 곳에 생기면"을 앞당김).
- Ta.8: `Create`·`new CombatCameraView`·`Bind`·`SetView`·`Rotate`의 호출처·구현처 확인, 우리 `CombatCameraZoomPoseTests`가 부르는 경로·설정 생성자 서명 그대로, 바뀐 파일의 줄 끝 그대로.

### 7.2 CR-b 입력 (2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Tb.1~Tb.7 계획대로. 액션 에셋은 Python으로 CRLF를 지켜 넣었고 JSON 구조를 다시 읽어 확인했다(새 GUID 넷).
- Tb.8: `IStageInputReceiver` 구현 둘(슬롯·전투 수신), 이동·줌 이름을 따로 나열한 다른 곳 없음, 액션 이름을 쓰는 곳은 컨트롤러·Layer 둘뿐.

### 7.3 CR-c 문서·공유·검토 (2026-11-14)

- Tc.1~Tc.3 계획대로(공유 §58 미전달). Tc.4 묶음 끝 검토: 높음·중간 0, 낮음 7, 약 8.9. 반영 L6(문서·주석 넷)·L1 표현, 결정 대기 L1·L2·L3, 기록만 L4·L5·L7. 내용은 설계 §8.
- 평가 아티팩트 차수는 더하지 않았다(TP-15).

### 7.4 검토 L2·L3 반영 (2026-11-14 사용자 "L2, L3 권장대로 반영해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- L2: `CombatCameraOrbit.FindFrameError`에 위 축 검사 한 줄(뒤집힌 카메라 거절). TP-3이 넓어진 것이다.
- L3: `CombatCameraOrbit.TryGroundPoint` 새 함수, 어댑터 `ApplyPose`가 이것을 쓰고 실패하면 그 프레임은 회전 없이 그린다. `GroundPoint`는 감싼 함수로 남았다(기존 모드 `Place` — 시작 때 검사한 기준 회전이라 실패하지 않음).
- Unity 확인: Ub-13(Play 중 Transform 편집)에서 돌린 채 카메라를 위로 젖혀도 콘솔 예외가 없고 회전만 풀리는지 함께 본다.

### 7.5 검토 L1 반영 — 회전 중심 = 지금 줌의 화면 가운데 (2026-11-14 사용자 "회전 중심을 줌 아웃이 아니라 현재 줌 상태로" → "돌릴 때 이동 보정", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- `CombatCameraOrbit`: `GroundToBasePan`(바닥 이동 → 기준 오프셋 변화)·`FlatAxesOf`(이동 기준 축의 바닥 위 오른쪽·앞).
- `CombatCameraView`: 생성 때 바닥 축 둘·자세 모드 렌즈 카메라를 기억, `Rotate`가 자세 모드면 `HoldCenterShift(옛 각, 새 각)`만큼 오프셋을 옮긴 뒤 제한 안으로 맞춤. 어댑터·Factory·에셋 무변경.
- 설계 P-CR3이 바뀌었다(설계 §3 CR1·§4·§8). Unity 확인 Ub-7을 새 기대로 고쳤다.

### 7.6 CR-d 가운데 버튼 끌기 회전 (2026-11-15, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- §8 Td.1~Td.7대로. 설계와 다른 점 없음. Factory·Framework 무변경, 새 파일 없음.
- 묶음 끝 검토 약 7.5(컴파일 오류 없음): M1(확인창 뒤 낡은 기준점으로 튐)·L1(카메라 없는 시점 예외)·L4(문서·주석) 반영, L2 기록만. M2(커서 스냅 워프가 끌기에 섞임 → 끄는 동안 스냅 쉬기)·L3(끈 설정에서도 가장자리 이동 멈춤 → 그대로·툴팁)는 2026-11-15 사용자 "M2, L3 권장대로 반영해"로 반영(Router `Route` 끝 선택 인자 `cameraDragging`·연결 입력 묶음 한 줄·카메라 입력 `IsRotateDragging`·SO 툴팁, 컴파일 미확인). 내용은 설계 §9 구현 기록.

## 8. Phase CR-d 가운데 버튼 끌기 회전 (설계 §9 CR8)

### Td.1 액션 에셋

- `CombatCamera` 맵에 액션 `RotateDrag`(Button, 상호작용 없음)·연결 `<Mouse>/middleButton`(새 GUID 둘). 줄 끝은 파일 꼴 그대로.

### Td.2 이름·수신 계약 사슬

- `StageInputNames.CombatCamera.RotateDrag`.
- `IStageInputReceiver.CameraRotateDragChanged(bool held)`, `StageInputReceiverSlot` 한 줄, `CombatCameraInputController`의 `input.Button(...)` 한 줄.
- `CombatInputReceiver.CameraRotateDragChanged` → `Receive(Request.CameraRotateDrag, held ? 1 : 0, 0)`.

### Td.3 요청·분배

- `CombatInputRouter.Request.CameraRotateDrag`(맨 끝, 기존 번호 유지)·`IsCameraRequest`에 포함.
- `CombatSceneInput.Route` 갈래 하나 → `_camera?.RotateDragChanged(screenX > 0f)`.

### Td.4 카메라 입력 `CombatCameraInput`

- 상태 넷: 끄는 중·기준 있음·직전 가로 위치·쌓인 픽셀.
- `RotateDragChanged(held)`: 바뀔 때만, 누르면 기준 = 마지막 포인터(받은 적 있을 때).
- `PointerMoved`: 끄는 중이면 기준이 있을 때 차이를 쌓고 기준을 옮김.
- `Tick`: 쌓인 양을 비우고, 움직여도 되면 Z·C 다음에 `RotateDrag(픽셀 ÷ 화면 너비)`, 끄는 동안 가장자리 방향 0, 막혔으면 기준 버림.
- `Clear`: 끌기 상태 비움.

### Td.5 계약·세션·시점

- `ICombatCameraControl.RotateDrag(float screenWidths)`, 세션 한 줄.
- 시점: `Rotate`·`RotateDrag`가 공용 `TurnBy(degrees)`를 부름(Z·C 결과 수는 옛 식과 같음 — 부호 뒤집기는 정확).

### Td.6 설정 `rotateDragDegrees`

- SO 칸(기본 180, 음수 허용 — `Min` 없음)·Data 끝 선택 인자(기본 0)·속성·검사(유한)·기본 에셋 한 줄. SO 코드와 에셋은 한 커밋.

### Td.7 문서

- 입력 안내 §13, 줌 자세 안내 §1.2, CLAUDE.md. 민형님 공유 없음(Factory 무변경).

### Uc (CR-d 뒤)

1. 가운데 버튼을 누른 채 오른쪽으로 끌면 Z를 누를 때와 같은 방향으로 돈다. 왼쪽이면 C 방향.
2. 떼면 바로 멈춘다(관성 없음). 위아래로만 끌면 돌지 않는다.
3. 화면 가운데에서 끝까지 끌면 약 90° 돈다(기본 180°/화면 너비).
4. 끌다 화면 끝(가장자리 띠)에 닿아도 화면이 밀리지 않는다. 떼면 가장자리 이동이 다시 된다.
5. 줌인한 채 끌어도 화면 가운데가 제자리다(§7.5와 같음).
6. 돌고 나면 호버 칸·커서 스냅이 커서 아래 칸과 맞다.
7. Z·C는 그대로 돈다. 끌면서 Z·C를 눌러도 튀지 않는다.
8. 우클릭(배치 단계 유닛 회수, 공격 모드·이동 Drag 취소)·왼쪽 클릭·HUD 아이템 우클릭 해제가 그대로다.
9. 확인창·결과 화면 동안 끌어도 돌지 않고, 닫힌 뒤에도 그동안 끈 만큼 한꺼번에 돌지 않는다. 확인창 버튼을 누른 자리에서 마우스를 그대로 두고 가운데 버튼을 눌러 끌어도 튀지 않는다(검토 M1).
10. 끄는 중 창 포커스를 잃었다 돌아오면 끌기가 풀려 있다(다시 눌러야 돎).
11. 설정 `rotateDragDegrees`를 음수로 바꾸면 반대 방향, 0이면 끌어도 돌지 않는다(설정은 표시 연결 때 읽으므로 다음 전투 진입부터). 0이어도 누르는 동안 가장자리 이동은 멈춘다(L3).
12. 커서 칸 스냅이 켜진 채(기본) 공격 모드·이동 경로·배치 Drag 중에 끌어도 커서가 칸 중심으로 튀지 않고 손 움직임대로 돈다. 떼고 커서를 움직이면 다시 칸 중심으로 붙는다(검토 M2).
