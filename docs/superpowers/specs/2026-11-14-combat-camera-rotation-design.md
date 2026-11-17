# 전투 카메라 회전(방향 돌리기) 설계 초안

> 상태: **결정 완료(2026-11-14 사용자 "권장안대로 하고 Task 계획 써줘"): Q-CR1~Q-CR3 모두 A, P-CR1~P-CR4 그대로.** Task 계획 [combat-camera-rotation-plan](../plans/2026-11-14-combat-camera-rotation-plan.md)(CR-a~CR-c, TP-1~TP-15 그대로). **CR-a~CR-c 완료(2026-11-14 사용자 "그대로, CR-a 진행해" → "CR-c까지 쭉 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 구현 기록 §8).** 검토 L2·L3 반영(같은 날 사용자 "L2, L3 권장대로 반영해"), **L1 반영 = P-CR3 바뀜(같은 날 사용자 "회전 중심을 현재 줌 상태로" → "돌릴 때 이동 보정", §8)**, 모두 컴파일 미확인. **가운데 버튼 끌기 회전 CR8 추가·구현(2026-11-15 사용자 "A안으로 설계에 추가하고 진행해, Z/C도 유지해야함", 컴파일 미확인, §9)**. 남은 것: Unity 확인 Ua·Ub·Uc(계획 §5·§8), 민형님 공유 §58 전달.

- 작성일: 2026-11-14.
- 사용자 요청: "인게임 중 카메라 회전 넣을려고 하는데".
- 사용자가 정한 것(2026-11-14):
  - 회전 방식: "자유 회전을 먼저 만들면 나머지로 바꾸는 건 쉽지 않나. 내려다보는 뷰는 고정이고 방향만 바꾸는 식으로" → 위 축(Y)으로만 도는 자유 회전을 먼저 만든다. 내려다보는 각은 그대로다.
  - 입력: Z / C.
  - 회전 중심: 화면 가운데가 보는 바닥 점.
  - 전투를 나갔다 다시 들어오면: 처음 방향으로.
- 관련 원본:
  - [씬 공용 카메라 설계](2026-11-08-combat-scene-camera-design.md) — §1이 "새 회전 조작"을 범위 밖으로 적었다. 이 설계가 그 기능이다.
  - [카메라 조작 설계](2026-10-26-combat-camera-control-design.md) — 이동·줌의 책임 나누기 CC1~CC4. 이 설계는 그 틀에 회전을 더한다.
  - [줌 자세 안내](../viewDocs/combat-camera-zoom-pose-guide.md).
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`.

## 1. 지금 코드에서 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | 전투 시점은 Combat 씬의 공용 가상 카메라 하나다. 직교 투영이고 위치 (14.30, 19.95, −13.25)·회전 (45, −45, 0)이다. 화면 가운데는 필드 원점 근처 바닥을 본다 | `Prefabs/Stage/CombatCamera.prefab`, 씬 공용 카메라 설계 §0 |
| F2 | 카메라 자세를 정하는 곳은 시점 `CombatCameraView` 하나다. 이동 오프셋(`_offset`, 처음 카메라의 화면 오른쪽·위 축)과 보기 크기를 갖는다 | `C/Presentation/Cameras/CombatCameraView.cs` |
| F3 | 운영 설정은 줌 자세 모드(`useZoomPose: 1`)다. 이 모드에서는 Transform을 쓰지 않는다. 줌 자세 어댑터가 Cinemachine 단계에서 자세를 덮어쓴다. Play 중 Transform을 고치면 화면이 그 차이만큼 따라간다(씬 공용 카메라 설계 D0.1) | `CombatCameraSettings_Default.asset`, `CombatCameraZoomPose.ApplyPose` |
| F4 | 줌 자세 경로는 기준 자세에 대한 상대 자세를 만든다. 줌인 끝은 내려다보는 각을 10° 더 낮춘다(줌인 회전 오프셋 (−10, 0, 0)) | `CombatCameraZoomPosePath`, 설정 에셋 |
| F5 | 이동 제한 `panMin`·`panMax`((−12, −8)~(12, 8))는 처음 카메라 화면 축 기준이다 | `CombatCameraLimits.ClampOffset` |
| F6 | 칸 집기는 출력 카메라에서 광선을 쏴 찾는다. HUD·몸 위 글자·튜토리얼 테두리는 매 프레임 `WorldToScreenPoint`로 화면에 투영한다. 카메라 방향을 가정한 코드는 찾지 못했다 | `CombatPointerMapper`, `UnitHUDProjection`, `CombatBodyTextWindow`, `TutorialViewFocusLocator` |
| F7 | 흔들림은 카메라 공간으로 받는다(`m_UseCameraSpace: 1`). 회전해도 화면에서 같은 방향으로 흔들린다 | `CombatCamera.prefab`의 ImpulseListener |
| F8 | 연출 줌(포효·기차)과 휠 줌은 렌즈 크기만 바꾼다(카메라 창구 `CombatCameraWindow`) | `CombatCameraWindow` |
| F9 | 카메라 입력은 액션 맵 `CombatCamera`(Pan·Zoom) → `CombatCameraInputController` → 수신 계약 `IStageInputReceiver` → `CombatCameraInput` → 좁은 계약 `ICombatCameraControl` → `CombatCameraSession` → 시점 순서다. 시점이 바뀐 Tick에는 포인터 아래 칸을 다시 집는다(평가 C10-01) | `C/Input/Cameras/CombatCameraInput.cs`, `StageInput/` |
| F10 | 전투 맵 키: E = 공격 모드, Space = 턴 종료, R = 복귀(폐지된 기능), 오른쪽 클릭 = 취소, WASD·화살표 = 이동, 휠 = 줌. Z·C는 비어 있다. 수신 계약을 구현한 시험 가짜는 없다 | `Input/PlayerInputActions.inputactions`, `Assets/Tests` 검색 |
| F11 | 표시를 해제하면 카메라를 기준 자세로 되돌린다. 그래서 다시 들어오면 처음 방향이다(사용자 결정과 같음) | `CombatCameraView.Restore` |
| F12 | 특성 확인 씬도 같은 카메라 바인딩·Factory(`CombatCameraFactory`)를 쓴다 | `CombatTraitPreviewFactory.cs:61` |

## 2. "자유 회전을 먼저 만들면 나머지는 쉬운가"

쉽다. 회전 방식이 달라도 어려운 부분은 같다.

| 부분 | 자유 회전 | 90°·45° 단계 회전 |
|---|---|---|
| 중심점 둘레로 자세 돌리기 | 같음 | 같음 |
| 돌린 뒤 이동 키 방향 맞추기 | 같음 | 같음 |
| 이동 제한·줌 자세와 합치기 | 같음 | 같음 |
| 포인터 다시 집기 | 같음 | 같음 |
| **목표 각을 정하는 규칙** | 누르는 동안 초당 일정 각만큼 | 한 번 누르면 목표 각 += 단계, 짧게 따라감 |

시점은 "지금 방위각"만 갖고, 목표를 정하는 규칙을 입력 쪽 함수 하나로 둔다. 그러면 단계 회전은 나중에 설정 칸 하나와 규칙 함수 하나로 더할 수 있다(Q-CR1).

자유 회전에만 드는 비용은 코드가 아니라 아트 확인이다. 모든 각도에서 배경 뒷면·가장자리를 확인해야 한다(§5).

## 3. 설계

### CR1. 시점의 상태와 책임

- 시점 `CombatCameraView`가 방위각 `_yaw`(도, 0 = 처음 방향)를 하나 더 갖는다. 자세의 원본은 여전히 시점 하나다(SSOT).
- 내려다보는 각·롤은 바꾸지 않는다. 회전은 **월드 위 축(Y)**만이다. 줌 자세의 줌인 각 변화(F4)는 지금처럼 남는다.
- 회전 중심은 "지금 화면 가운데가 보는 바닥 점"이다(사용자 결정).
  - 바닥 점 = 줌아웃 기준 자세(이동 오프셋 포함)의 앞 방향 광선과 필드 평면의 교점.
  - 줌 정도와 무관하게 이 점 하나를 쓴다. 줌인 자세는 위치·각이 조금 달라서, 줌인한 채 돌리면 화면 가운데가 중심 둘레로 조금 움직인다. 돌리는 동안 중심이 흔들리지 않게 하려는 선택이다(P-CR3).
  - **바뀜(2026-11-14, §8 검토 L1):** 자세 계산의 중심은 그대로 줌아웃 기준 바닥 점이지만, 시점이 돌릴 때마다 이동 오프셋을 보정해 **지금 줌의 화면 가운데**를 제자리에 둔다. 돌린 뒤 줌하면 줌 자세의 앞뒤 이동이 화면 기준 그대로다. 이동 제한 끝에서는 제한이 먼저라 다 붙잡지 못한다.
  - 필드 평면 높이는 필드 Cells Root의 월드 높이다. Factory가 시점에 넘긴다(P-CR4).

### CR2. 자세 계산 — 순수 함수 하나

새 정적 클래스 `CombatCameraOrbit`(`C/Presentation/Cameras/`, 상태 없음)에 계산을 모은다.

| 함수 | 하는 일 |
|---|---|
| `GroundPoint(Pose pose, float groundY)` | 자세의 앞 방향 광선과 바닥 평면의 교점. 평면과 나란하면 예외 |
| `Apply(Pose pose, Vector3 pivot, float yaw)` | 자세를 중심점 둘레로 위 축 회전. 위치 = 중심 + R(yaw)·(위치 − 중심), 회전 = R(yaw)·회전 |
| `ToBasePan(Vector2 screenDirection, float yaw, float pitchSin)` | 지금 화면 방향의 이동 입력을 처음 카메라 화면 축의 오프셋 변화로 바꾼다(CR3) |

두 줌 모드가 같은 함수를 부른다.

- **줌 자세 모드(운영):** 어댑터 `CombatCameraZoomPose`가 지금처럼 자세를 계산한 뒤 마지막에 `Apply`를 한 번 더 한다. Transform은 쓰지 않는다. 그래서 D0.1(Play 중 Transform 편집 반영)과 흔들림 보정이 그대로다. 어댑터의 `SetOffset(offset)`을 `SetView(offset, yaw)`로 넓힌다. 중심은 어댑터가 매 프레임 지금 Transform 기준으로 구한다(계획 TP-5).
- **기존 모드:** `Place`가 쓰는 Transform 자세에 `Apply`를 한다.

방위각이 0이면 두 모드 모두 지금과 같은 자세가 나온다. 첫 Phase는 화면 변화가 없다.

### CR3. 이동(팬)과 이동 제한

- 이동 오프셋은 지금처럼 **처음 카메라 화면 축**에 저장하고, 제한도 지금 함수(`ClampOffset`)를 그대로 쓴다.
  - 이 오프셋은 바닥에서 보면 고정된 사각형이다. 카메라를 돌려도 볼 수 있는 바닥 범위는 같다(Q-CR2 A).
  - 방위각 0에서는 지금과 똑같이 움직이고 똑같이 멈춘다. 에셋을 바꾸지 않는다.
- 돌린 뒤 W를 누르면 "지금 화면의 위"로 가야 한다. 시점이 입력 방향을 `ToBasePan`으로 바꾼 뒤 오프셋에 더한다.
  - 바꾸는 식: 화면 방향 (x, y)를 바닥 방향 (x, y ÷ sin(내려다보는 각))으로 늘림 → 방위각만큼 돌림 → 다시 화면 축으로 (x, y × sin(내려다보는 각)).
  - 화면에서 느끼는 속도는 지금과 같다(직교 투영이라 화면 위로 d만큼 = 바닥 앞으로 d ÷ sin 만큼).
- 가장자리 이동도 같은 함수를 지난다(화면 방향 입력이라).

### CR4. 회전 규칙과 값

- 설정 SO `CombatCameraSettingsSO`에 칸 하나: `rotateSpeed`(도/초, 기본 90, 0 이상). 0이면 회전을 끈다.
- Z·C를 누르는 동안 방위각이 초당 `rotateSpeed`만큼 바뀐다. 바로 멈추고 바로 출발한다. 가속·감속은 넣지 않는다(P-CR1).
- 방위각은 −180~180으로 감아 둔다. 범위 제한은 없다(Q-CR3 A).
- 방향: **Z = 카메라가 중심 둘레로 자기 왼쪽으로 돈다**, C = 오른쪽(P-CR2). 흔한 "Q/E로 돌리기"의 Q·E와 같은 느낌이다. 위 축 회전 각의 부호는 계획에서 이 방향에 맞춘다.
- 회전은 줌 대여·연출 줌 중에도 막지 않는다. 렌즈만 바꾸는 연출이라 겹치지 않는다(F8).

### CR5. 입력

이동 입력과 같은 길에 한 줄씩 더한다.

1. 액션 에셋 `CombatCamera` 맵에 `Rotate`(Axis, Value): 1D 묶음 Z = −1, C = +1.
2. `StageInputNames.CombatCamera.Rotate`, `CombatCameraInputController`에 `input.Float(Rotate, _receiver.CameraRotateChanged)`.
3. 수신 계약 `IStageInputReceiver.CameraRotateChanged(float value)`, 슬롯 `StageInputReceiverSlot`, 전투 수신 `CombatInputReceiver`.
4. `CombatCameraInput.RotateChanged(value)`가 축 값을 기억하고, `Tick`이 `_camera.Rotate(axis, deltaTime)`를 부른다. 돌았으면 이동·줌과 같이 포인터를 다시 집는다(F9). `Clear`가 축 값을 비운다.
5. 좁은 계약 `ICombatCameraControl.Rotate(float direction, float deltaTime)` → 세션 → 시점.
6. 움직여도 되는지는 지금 정책 `CanMoveCamera` 하나를 같이 쓴다(확인창·결과 화면에서 막힘).
7. 키 재설정 목록에는 넣지 않는다(이동 키와 같음).

### CR6. 수명

- 표시 연결(`Begin`) 때 방위각 0.
- 표시 해제(`Restore`) 때 방위각 0, 어댑터 해제, 기준 자세로 되돌림. 지금 순서 그대로다. 다시 들어오면 처음 방향이다(사용자 결정, F11).
- 새 GameObject·컬렉션을 만들지 않는다. 매 프레임 계산은 카메라 하나에 상수 시간이다.

### CR7. 다른 기능과 겹침

| 상대 | 결과 |
|---|---|
| 칸 집기·커서 스냅·HUD·몸 위 글자·튜토리얼 테두리 | 출력 카메라 기준이라 그대로 맞음(F6). 돌아가는 동안은 포인터를 다시 집음(CR5) |
| 흔들림 | 카메라 공간이라 화면 기준 방향 그대로(F7) |
| 휠 줌·연출 줌 | 렌즈만 바꿈. 줌 자세 경로가 회전을 뒤따름(CR2) |
| 줌 자세 Play 중 Transform 편집(D0.1) | 어댑터가 마지막에 회전만 더해 그대로 동작 |
| 이동 제한 | 바닥 사각형 고정(CR3) |
| 특성 확인 씬 | 같은 Factory라 회전도 함께 들어감(F12). 그 씬의 입력 연결 여부는 계획 때 확인 |
| 스테이지 편집 도구·FBX 미리보기 | 런타임 카메라를 돌리지 않음. 영향 없음 |
| 스테이지 맵 카메라(호진님, Alt·가운데 버튼) | 다른 씬·다른 맵. 영향 없음 |
| 유닛이 바라보는 방향 | 그대로. 돌리면 아군 뒷모습·적 앞모습이 보일 수 있음 |

## 4. 정할 것

| # | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-CR1 | 단계 회전(90°·45°)을 이번에 함께 넣나 | **A** 자유 회전만. 단계 회전은 나중에 설정 칸 하나와 목표 규칙 함수 하나로(§2) | B 이번에 함께(`rotateStep` 칸, 0 = 자유, 단계면 부드럽게 따라가는 시간 칸도) |
| Q-CR2 | 이동 제한 | **A** 지금 값 그대로, 바닥에 고정된 사각형(방위각 0에서 지금과 같음, 에셋 무변경) | B 필드 축 기준 사각형 칸을 새로 / C 화면 기준 유지(돌리면 볼 수 있는 바닥이 바뀌어 필드 밖이 보일 수 있음) |
| Q-CR3 | 회전 범위 | **A** 제한 없음(한 바퀴) | B 처음 방향 기준 ±각도 제한 칸(배경 뒷면이 비어 있을 때의 안전장치) |

설계에서 정한 것(사용자 확인 대상):

- **P-CR1** 키를 누르는 동안 일정 속도, 가속·감속 없음.
- **P-CR2** Z = 카메라가 중심 둘레로 왼쪽으로, C = 오른쪽으로(Q/E 게임의 Q·E와 같음).
- **P-CR3** ~~회전 중심은 줌아웃 기준 자세의 화면 가운데 바닥 점 하나~~ → **바뀜(2026-11-14 사용자): 지금 줌의 화면 가운데. 구현은 돌릴 때 이동 보정(§8 검토 L1).**
- **P-CR4** 필드 평면 높이는 Factory가 필드 Cells Root에서 읽어 넘긴다(Factory 연결 줄 둘 → 민형님 공유).

## 5. Unity에서 꼭 봐야 할 것(코드로 알 수 없음)

- **배경 뒷면.** 맵 배경 프리팹과 구멍 난 배경 메시가 한 방향에서만 보이게 만들어졌다면, 돌렸을 때 빈 면·잘린 가장자리·뒷면 컬링이 보인다. 가장 큰 위험이다. 결과에 따라 Q-CR3 B를 다시 볼 수 있다.
- **그림자·조명.** 해 방향은 그대로라 돌리면 그림자 방향이 화면에서 바뀐다. 밤 합성 화면도 함께 본다(Boot에는 `Main Camera` 하나만 있어 따로 도는 카메라는 없어 보인다).
- **카메라 거리와 자름.** 돌려도 카메라 높이는 같다. 다만 배경이 높은 쪽이면 가까운 자름면에 걸리는지 본다.

## 6. 원칙 점검

- **SSOT:** 자세의 원본은 시점 하나(방위각을 더 들 뿐), 계산식은 `CombatCameraOrbit` 하나를 두 줌 모드·이동 변환이 같이 쓴다. 이동 제한 규칙·움직여도 되는지 판정은 지금 것 하나씩.
- **SOLID·SRP:** 입력(무엇을 눌렀나)·시점(상태)·계산(순수 함수)·어댑터(Cinemachine 단계 쓰기)가 나뉜다. 회전 목표 규칙을 입력 쪽에 두어 단계 회전을 더해도 시점은 그대로다(OCP).
- **FW 지침·계약:** Framework·GlobalSystem·패키지 무변경. 렌즈는 지금처럼 `Dispatch`만. 입력은 기존 맵·Layer·수신 계약 꼴. Factory는 연결 줄만.
- **변경 독립성:** 방위각 0이면 지금과 같은 자세라 회전을 끄면(`rotateSpeed` 0) 지금 동작 그대로다.
- 예상 약 8.7/10. 감점 예상: 줌인 중 회전 중심이 화면 가운데와 조금 어긋남(P-CR3), 시점의 생성 인자가 하나 늘어남.

## 7. Phase(결정 뒤 Task 계획으로)

| Phase | 내용 | 화면 |
|---|---|---|
| CR-a 계산 | `CombatCameraOrbit`, 시점 방위각·`Rotate`, 어댑터 `SetView`, Factory 필드 높이 연결 | 변화 없음(방위각 0) |
| CR-b 입력·값 | SO 칸 `rotateSpeed`, 액션 `Rotate`, 수신 계약·슬롯·전투 수신, `CombatCameraInput`, 좁은 계약·세션 | Z·C로 돎 |
| CR-c 문서·검토 | 줌 자세 안내·입력 안내·폴더 안내, 씬 공용 카메라 설계 §1에 "대신함" 한 줄, 민형님 Factory 공유, 묶음 끝 검토 | — |

- 상시 지시대로 MSBuild·새 시험 없음, 컴파일 미확인으로 보고한다. 수신 계약을 구현한 시험 가짜는 없어 고칠 시험이 없을 것으로 본다(F10).
- 코드 변경 파일은 모두 우리 영역이다(`C/Presentation/Cameras/`·`C/Input/`·`StageInput/`·카메라 설정 SO). Factory 두 줄만 민형님 연결 줄이다.

## 8. 구현 기록 (2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **CR-a 계산(화면 변화 없음):** 새 `C/Presentation/Cameras/CombatCameraOrbit.cs`(`GroundPoint`·`Apply`·`ToBasePan`·`PitchSinOf`·`FindFrameError`, `.meta`는 Unity가 만듦), 경로 `ZoomOutAt`, 어댑터 `Bind(path, groundY)`·`SetView(offset, yaw)`·방위각이 0이 아닐 때만 회전, 시점 `CombatCameraView`의 생성 인자 `groundY`·이동 기준 축 검사·`_yaw`·`Rotate`·`Pan` 입력 변환·`Place`, 좁은 계약·세션 `Rotate`, 설정 `rotateSpeed`(SO 기본 90, Data 끝 선택 인자 기본 0, 기본 에셋 한 줄), Factory `Create` 넷째 인자·`GroundHeightOf`·호출 두 줄. 설계와 다른 점은 계획 TP-6(기존 모드 `Place`가 회전도 씀) 하나다.
- **CR-b 입력:** 액션 에셋 `CombatCamera` 맵 `Rotate`(Value·Axis, 1D 묶음 `ZC`), `StageInputNames.CombatCamera.Rotate`, 수신 계약 `CameraRotateChanged`(슬롯·컨트롤러·전투 수신), 요청 `CameraRotate`(맨 끝)·`IsCameraRequest`, 연결 입력 묶음 갈래, `CombatCameraInput.RotateChanged`·`Tick`(돌면 포인터 다시 집기)·`Clear`. 이동·줌 이름을 따로 나열한 다른 곳은 없다.
- **CR-c 문서·공유:** 줌 자세 안내 §1.2·§3, 입력 안내 §13, 폴더 배치 안내, 씬 공용 카메라 설계 §1 "대신함", 민형님 Factory 공유 §58(미전달).
- **묶음 끝 검토**(새 읽기 전용 에이전트 하나): 높음·중간 0, 낮음 7, 약 8.9(SSOT 9.0·SOLID 9.0·FW 9.5·독립성 8.5·캡슐화 9.0·C#/Unity 8.5·If/Tick 8.5). 확인된 것: 바뀐 서명의 호출처·구현처 모두 맞음, 우리 시험 그대로 컴파일, 방위각 0에서 HEAD와 같은 수, 운영 프리팹 (45, −45, 0)이 TP-3 검사 통과, 회전 부호·이동 변환식·화면 속도, 흔들림 보정 보존, 수명 초기화, 입력 사슬, Framework·GlobalSystem 무변경.
  - **반영:** L6 문서·주석이 코드와 다른 곳(이 설계 CR2의 `SetView` 서명, 계획 §0 시험 인자 수, 요청 열거 "맨 끝" 이유 = 기존 번호 유지, `CombatCameraOrbit` 경계 주석), L1의 "조금" 표현을 수치로(줌 자세 안내 §1.2-4·P-CR3).
  - **결정 대기(코드 동작):**
    - L1 줌인 끝에서 돌리면 화면 가운데가 크게 밀림(지금 기본값으로 90°에 약 4.9, 화면 너비 1/3쯤, 추정). 안: `Rotate`에서 지금 렌즈의 화면 가운데가 제자리이도록 회전으로 생긴 바닥 어긋남만큼 이동 오프셋을 보정(이동 제한 끝에서는 다 못 맞춤). P-CR3을 바꾸는 것이라 Ub-7 결과를 보고 정한다.
    - L2 위아래가 뒤집힌 카메라(롤 180° 또는 내려다보는 각 90° 초과)를 `FindFrameError`가 거르지 않음 — 위 축 높이 성분 검사 한 줄 권장. 운영 프리팹은 해당 없음.
    - L3 돌린 채 Play 중 Transform을 바닥을 보지 않게 고치면 어댑터의 `GroundPoint` 예외가 Cinemachine 콜백에서 매 프레임 남(개발 중만) — `TryGroundPoint`로 실패하면 회전 없이 쓰기 권장.
  - **기록만:** L4 이동 기준 축 sin을 처음에 한 번 읽음(Play 중 내려다보는 각을 바꾸면 돌린 뒤 이동이 조금 어긋남, 개발 중만), L5 겹친 0 검사·자르기(비용 무시), L7 줄 끝(파일마다 기존 꼴 유지, 섞인 파일 없음).
  - **L2·L3 반영(2026-11-14 사용자 "L2, L3 권장대로 반영해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음):**
    - L2: `CombatCameraOrbit.FindFrameError`에 "위 축이 위를 향하는지" 검사 한 줄. 롤 180°·내려다보는 각 90° 초과 카메라는 표시 연결 때 거절한다. 운영 프리팹(위 축 높이 약 0.707)은 그대로 통과한다.
    - L3: 새 `CombatCameraOrbit.TryGroundPoint`(실패하면 false). 줌 자세 어댑터는 이것을 써서 중심을 못 찾으면 그 프레임은 회전 없이 그린다. `GroundPoint`(던짐)는 이를 감싼 것으로 남기고, 기존 모드 `Place`가 쓴다. 그쪽은 시작 때 검사한 기준 회전으로만 계산해 실패하지 않는다.
    - 둘 다 우리 줄이고 다른 작업자 줄·Factory·에셋 변경은 없다.
  - **L1 반영(2026-11-14 사용자 "회전 중심을 줌 아웃이 아니라 현재 줌 상태로" → 두 안 중 "돌릴 때 이동 보정", 컴파일 미확인, 새 시험 없음, 시험 변경 없음):**
    - 식: 지금 줌의 화면 가운데 = 줌아웃 중심 + R(방위각)·d. d는 줌아웃 대비 지금 렌즈 크기에서 화면 가운데가 바닥 위로 옮겨 간 양이다. 이동 오프셋은 모든 줌 자세를 같이 옮기므로 d는 오프셋과 무관하다. 방위각이 바뀌면 가운데가 (R(새 각) − R(옛 각))·d만큼 움직이므로, 시점 `Rotate`가 그만큼 오프셋을 반대로 옮긴다(`CombatCameraView.HoldCenterShift`, 바닥 이동 → 오프셋 변환 `CombatCameraOrbit.GroundToBasePan`·축 `FlatAxesOf`). 오프셋은 제한 안으로 맞춘다.
    - 렌즈 크기는 첫 카메라의 지금 렌즈 값(연출 줌 포함)을 읽는다. 기존 모드(줌 자세 끔)는 d = 0이라 보정하지 않는다. 어댑터는 그대로다(줌아웃 중심 둘레로 돌림).
    - 버린 안: 매 프레임 지금 렌즈 자세의 화면 가운데를 중심으로 돌리기 — 돌린 채 줌하면 앞뒤 이동이 처음 방향 기준으로 남아 화면에서 옆으로 미끄러진다(90°에서 약 3.5).
    - 남는 것: 이동 제한 끝에서는 다 붙잡지 못해 조금 밀린다. d는 저장된 기준 자세로 계산해 Play 중 Transform 편집(D0.1)을 따라가지 않는다(개발 중만, L4와 같은 성격).

## 9. 가운데 버튼 끌기 회전 (CR8, 2026-11-15)

- 요청: "우클릭한 상태로 드래그로 회전 되나" → 우클릭은 이미 취소라 충돌한다고 답하고 안 셋(A 가운데 버튼 끌기 / B 우클릭 끌기 + 클릭과 구분 / C Z·C만)을 보임 → 사용자 "A안으로 설계에 추가하고 진행해, Z/C도 유지해야함".
- 확인한 사실:
  - 전투 `Deployment`·`Combat` 맵의 `Cancel` = `<Mouse>/rightButton`이고 `Press`라 누르는 순간 실행한다(배치 회수, Drag·공격 모드·경로 취소, 아이템 끌기 취소). HUD 아이템 아이콘 우클릭은 장착 해제다.
  - 전투가 켜는 맵(`Deployment`·`Combat`·`CombatCamera`)에 가운데 버튼 연결이 없다. `StageSelection` 맵의 `CameraDolly`가 가운데 버튼을 쓰지만 다른 씬이다.
  - 포인터 위치는 이미 카메라 입력에 온다(`CombatCameraInput.PointerMoved`, 가장자리 이동용). 포인터 다시 집기(`RefreshPointer`)는 카메라 입력을 지나지 않는다.
  - Framework `InputBindings.Button(이름, Action<bool>)`이 누름 = true·뗌 = false를 준다(입력 Framework 무변경).

### CR8. 설계

1. **입력:** `CombatCamera` 맵에 `RotateDrag`(Button) = `<Mouse>/middleButton`. 누름·뗌만 받는다. 끈 양은 지금 오는 포인터 위치의 가로 차이로 센다(새 델타 액션 없음, P-CR5).
2. **사슬:** `StageInputNames.CombatCamera.RotateDrag` → `CombatCameraInputController`의 `input.Button(RotateDrag, _receiver.CameraRotateDragChanged)` → `IStageInputReceiver.CameraRotateDragChanged(bool held)` → 슬롯 → `CombatInputReceiver` → 요청 `CameraRotateDrag`(맨 끝, `IsCameraRequest`에 포함) → 연결 입력 묶음 → `CombatCameraInput.RotateDragChanged(held)`. Z·C 사슬은 그대로다.
3. **카메라 입력 `CombatCameraInput`:**
   - 누르는 동안 포인터 이동마다 직전 위치와의 가로 차이(픽셀)를 쌓는다. 누른 뒤 첫 이동은 늘 기준만 잡는다(마지막 포인터 위치가 낡았을 수 있음 — 아래 검토 M1).
   - `Tick`이 쌓인 양을 화면 너비로 나눠 `_camera.RotateDrag(너비 비율)`에 넘기고 비운다. 돌았으면 이동·줌·Z·C와 같이 포인터를 다시 집는다.
   - 막힌 동안(`CanMoveCamera` 거짓) 쌓인 양은 버리고 기준점도 버린다. 그래서 확인창이 닫힌 뒤 그동안 움직인 만큼 한꺼번에 돌지 않는다(휠과 같은 규칙).
   - 끄는 동안에는 화면 가장자리 이동을 하지 않는다. 끌다 화면 끝에 닿아 화면이 밀리지 않게 한다(P-CR8). 떼면 바로 다시 한다.
   - 끄는 동안에는 커서 칸 스냅도 하지 않는다(검토 M2 반영). 스냅이 옮긴 커서가 끈 양에 섞이지 않게 한다. 뗀 뒤 첫 포인터 이동에서 다시 스냅한다.
   - `Clear`(포커스 상실·정지)가 끌기 상태를 비운다.
4. **좁은 계약:** `ICombatCameraControl.RotateDrag(float screenWidths)` → 세션 → 시점. 시점은 각 = 비율 × `rotateDragDegrees`로 바꿔 Z·C와 같은 내부 함수 `TurnBy`(방위각 감기·중심 보정 §8 L1·`Place`)를 부른다. 회전 규칙의 원본은 이 함수 하나다.
5. **방향:** 오른쪽으로 끌면 Z와 같은 방향이다. 카메라가 중심 둘레로 자기 왼쪽으로 가서, 화면 아래쪽 바닥이 커서를 따라 오른쪽으로 간다("바닥을 잡고 돌리기", P-CR6). 값이 음수면 반대다.
6. **값:** 설정 `rotateDragDegrees` = 화면 너비만큼 끌면 도는 각(도). 기본 180(P-CR7), 0 = 끄기, 음수 = 반대 방향. Data 생성자 끝 선택 인자(기본 0), 기본 에셋 한 줄. Z·C의 `rotateSpeed`는 그대로다.
7. **Z·C와 함께:** 둘 다 같은 프레임에 더해진다(Z·C 먼저, 끌기 다음).
8. **UI 위:** 가운데 버튼은 UI가 쓰지 않으므로 UI 위에서 눌러도 돈다(휠 줌과 같음, P-CR9).

### 지키는 것·바뀌지 않는 것

- 우클릭(취소·배치 회수·장착 해제)·왼쪽 클릭·Z·C 동작은 그대로다.
- Factory·Framework·GlobalSystem 무변경, 새 파일 없음. 고치는 줄은 모두 우리 카메라·입력 줄 옆에 더하는 것이다(다른 작업자 줄 없음, 민형님 공유 없음).
- 시험: 새 시험 없음. `IStageInputReceiver`·`ICombatCameraControl` 구현은 운영 코드 넷(슬롯·전투 수신·세션·시점)뿐이고 시험 가짜는 없다. 우리 `CombatCameraZoomPoseTests`는 설정 생성자를 끝 선택 인자 없이 불러 그대로 컴파일된다.

### 정한 것(사용자 확인 대상)

- **P-CR5** 끈 양 = 포인터 위치의 가로 차이. 커서가 화면 끝에 닿으면 더 돌지 않는다(다시 잡아 끌면 됨). 마우스 델타 액션을 따로 두면 끝에서도 돌지만 입력이 하나 더 는다.
- **P-CR6** 오른쪽 끌기 = Z와 같은 방향(바닥을 잡고 돌리기). 반대가 편하면 값을 음수로.
- **P-CR7** 기본 180°/화면 너비(화면 가운데에서 끝까지 끌면 약 90°).
- **P-CR8** 끄는 동안 가장자리 이동 없음. 커서 칸 스냅도 없음(2026-11-15 검토 M2, 사용자 "M2, L3 권장대로 반영해"). 설정이 0이어도 누르고 있으면 둘 다 멈춤(L3, 툴팁에 적음).
- **P-CR9** UI 위에서 눌러도 돎.

### Unity 확인 Uc(계획 §8)

계획 §8 Uc-1~Uc-11.

### 구현 기록 (2026-11-15, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 계획 §8 Td.1~Td.7대로. Factory·Framework 무변경, 새 파일 없음. 액션 에셋은 Python으로 줄 끝을 지켜 넣고 JSON을 다시 읽어 확인했다(새 GUID 둘).
- **묶음 끝 검토(읽기 전용 새 에이전트 하나) 약 7.5 — 컴파일 오류 없음, 중간 2·낮음 4:**
  - **M1 반영:** 확인창 동안은 수신 창구가 포인터 이동을 막아 마지막 포인터가 창 열기 전 위치에 머문다. 닫은 뒤 마우스를 그대로 두고 가운데 버튼을 누르면 그 낡은 위치가 기준이 되어 첫 끌기에 크게 튀었다(800px면 약 75°). → 누를 때 기준을 늘 비워 첫 포인터 이동이 기준만 잡는다. 첫 이벤트 한 번의 움직임(몇 픽셀)은 회전에 들어가지 않는다.
  - **M2 반영(2026-11-15 사용자 "M2, L3 권장대로 반영해"):** Router `Route`에 끝 선택 인자 `cameraDragging`, 참이면 포인터 이동에도 커서 스냅을 하지 않음(`IsCursorSnapMode && !cameraDragging`). 연결 입력 묶음이 카메라 입력의 `IsRotateDragging`을 넘긴다(끄는 상태의 원본은 카메라 입력 하나). Factory 무변경. 아래는 찾은 내용 — 커서 스냅(`CombatSceneRoot.snapCursorToGrid`, 기본 켬)이 배치 Drag·경로·공격 모드 중 칸이 바뀌면 OS 커서를 칸 중심으로 옮기고, 그 워프가 다음 포인터 이동으로 와서 끌기 양에 섞인다(손 움직임과 다른 회전, 커서도 튐). 안: 끄는 동안 커서 스냅을 쉬게 함(가장자리 이동 P-CR8과 같은 성격). 새 동작이라 사용자 결정 뒤.
  - **L1 반영:** 카메라 없는 시점(설정 null)에서 `Rotate`·`RotateDrag`가 설정을 먼저 읽어 예외가 나던 것(운영은 카메라가 늘 있어 안 남) → 둘 다 카메라 있음을 먼저 본다(옛 Z·C 순서 그대로).
  - **L2 기록만:** 입력 연결을 풀 때 수신 창구를 먼저 떼어 맵을 끌 때 오는 뗌 신호가 버려진다. 다시 열 때 정지(`Clear`)를 거치지 않으면 끄는 상태가 남을 수 있다. 이동·Z·C 키도 같은 기존 성격이다.
  - **L3 그대로 두고 툴팁에 적음(2026-11-15 사용자 "권장대로"):** `rotateDragDegrees = 0`(끔)이어도 가운데 버튼을 누르는 동안 가장자리 이동이 멈춘다. 그대로 두고 툴팁에 적을지, 끈 설정이면 멈추지 않게 할지.
  - **L4 반영:** 설정 Data 클래스 설명(이동·줌 → 이동·회전·줌)·`FindError` 설명에 회전 조건, 입력 안내 `TickCamera` 서명.
