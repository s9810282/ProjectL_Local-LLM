# 전투 카메라 조작(이동·줌) 설계 초안

- 작성일: 2026-10-26. 코드 변경 없음(설계만).
- 사용자 요청: "WASD 및 화살표, 화면 테두리에 마우스 갖다대기를 통한 카메라 위치 이동을 구현하고자 함."
  1. 이동하면 Day Camera·Night Camera가 함께 움직인다.
  2. 상하좌우 이동에는 최소·최대 제한이 있다.
  3. 줌인·줌아웃도 넣는다. Cinemachine 가상 카메라의 OrthographicSize를 조절하고, 최소·최대가 있다.
  4. 카메라 값은 Visual SO로 만들어 스테이지 전투 데이터에 연결한다.
- 관련 원본: [카메라 연출 조각 설계](2026-10-19-combat-camera-pieces-design.md)(창구 K1·줌 대여 K3·흔들림 겹침 K8), [배경 설계](2026-10-23-combat-backdrop-before-camera-bind-design.md)(배경은 씬 수명, BD3·BD4), [스테이지 입력 안내](../viewDocs/stage-input-guide.md)(입력 추가 절차 §6).
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`.

## 1. 지금 코드에서 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | 맵 카메라는 배경 프랍 안의 가상 카메라 전부다. 일반 스테이지 12곳(보스 포함)은 `Stage.prefab`을 쓰고, 그 안에 `Day Camera`(우선순위 1)·`Night Camera`(0)가 있다. 둘 다 Follow·LookAt이 비어 있어 Transform 위치가 곧 카메라 위치이고, 직교 크기는 둘 다 7이다 | `Prefabs/Stage/MapProp/Stage.prefab`, `C/Presentation/Cameras/CombatMapCameras.Read`(프랍 안 `CinemachineVirtualCamera` 전부) |
| F2 | 보스 스테이지는 `map` 칸(`Assets/Map_Map.asset` → `Assets/Map.prefab`)이 있어 그쪽 프리팹을 세우는데, `Map.prefab`에는 Day·Night Camera가 없다(V3-g 때부터 "임시 세팅이라 그대로") | `StageViewProp_Boss.asset`, `StageViewPropPresenter.Show` |
| F3 | 프랍은 필드 아래에 위치·회전 보정과 **배율 0.5**로 세운다. 카메라 이동은 로컬이 아니라 월드 좌표로 해야 배율에 휘둘리지 않는다 | `StageViewProp_*.asset`의 `scaleOffset` |
| F4 | 맵 카메라 렌즈를 바꾸는 곳은 카메라 창구 `CombatCameraWindow` 하나다(원칙 평가 C2). 창구는 프랍을 세울 때 읽은 **기준 렌즈값**을 기억하고, 연출 줌(포효·기차)은 그 기준 × 배율, 되돌림은 기준값 0초 줌이다. 렌즈는 Framework 카메라 효과(`Dispatch(CameraZoomData)`)로만 바꾼다 | `C/Presentation/Cameras/CombatCameraWindow.cs` |
| F5 | 창구는 표시 연결마다 새로 만들고 배경의 맵 카메라 사실을 받는다. 배경은 씬 수명이라 전투 중간에 표시가 다시 연결돼도 카메라는 남는다 | `CombatSceneDisplayFactory.cs:113-115`, 배경 설계 BD3·BD4 |
| F6 | 키 입력은 액션 에셋 `Assets/ProjectL/Input/PlayerInputActions.inputactions` → `CombatInputController`(맵별) → 수신 계약 `IStageInputReceiver`로 들어온다. 축 값은 `input.Vector2`·`input.Float`로 받는다 | 입력 안내 §6, `ViewSystem/StageInput/` |
| F7 | 입력 Layer는 국면으로만 바뀐다(배치 = Common + Deployment, 전투 = Common + Combat). 결과 재생·적 턴에도 끄지 않는다. 연결이 끝날 때만 비운다 | `CombatInputPolicy.LayerOf`, `CombatInputConnection.cs:116` |
| F8 | **키 충돌:** 배치 맵의 `A`는 캐릭터 뽑기(`DrawCharacter`)다. W·S·D·화살표·마우스 휠은 비어 있다. E(공격 모드)·Space(턴 종료)·G(전투 시작)·Q/E(아티팩트 회전, 다른 화면) | 액션 에셋 |
| F9 | 키 재설정 목록(설정 화면)에는 전투 시작·턴 종료·공격 모드·복귀만 있다 | `StageInputKeyBindings` |
| F10 | 포인터 → 칸 찾기는 출력 Unity 카메라 기준이라 가상 카메라를 옮겨도 그대로 맞는다 | `CombatPointerMapper` |
| F11 | "StageCombatData"라는 형식은 없다. 가까운 것은 둘이다 — InGame `CombatStageData`(규칙 데이터, 순수 C#, View SO를 참조하면 안 됨: 규칙 P1·P5)와 View `ViewStageDataSO`(스테이지별 표시 데이터, 에셋 이름 `StageViewProp_*.asset`, 안에 맵 프랍 칸 `StageViewPropData`) | `DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageData.cs`, `…/View/Table/ViewStage/Item/ViewStageDataSO.cs` |
| F12 | CLAUDE.md에는 "FieldCamera"가 다른 작업자 영역으로 적혀 있다. 지금 저장소에 카메라 이동 코드는 없다 | CLAUDE.md 작업 방식 |

## 2. 설계

### CC1. 책임 나누기

| 부품 | 하는 일 | 모르는 것 |
|---|---|---|
| 값 `CombatCameraSettings`(새 SO가 만듦) | 이동 속도·가장자리 두께·이동 제한·줌 제한·줌 한 칸·줌 시간 | 입력·카메라 |
| 입력 `CombatCameraInput`(새, `C/Input/Camera/`) | 키 축·휠·마지막 포인터 위치를 받아 이번 프레임의 "이동 방향·줌 칸 수"를 만든다. 움직여도 되는지는 정책 함수에 묻는다 | 카메라 위치·렌즈 |
| 시점 `CombatCameraView`(새, `C/Presentation/Cameras/`) | 이동 오프셋을 제한 안으로 맞추고 맵 카메라 전부를 같은 월드 이동으로 옮긴다. 줌 목표 크기를 제한 안으로 맞춰 창구에 넘긴다 | 입력 종류 |
| 창구 `CombatCameraWindow`(있음) | 렌즈의 유일한 작성자. "보기 크기"를 새로 들고, 연출 줌은 보기 크기 × 배율, 되돌림은 보기 크기로 | 이동 |

- 카메라 위치를 쓰는 곳은 시점 하나, 렌즈를 쓰는 곳은 창구 하나다(SSOT).
- 제한 계산은 순수 함수 하나(`CombatCameraLimits`)에 둔다. 이동·줌이 같이 쓴다.

### CC2. 입력

- 액션 에셋에 새 맵 `CombatCamera`를 만든다.
  - `Pan`(Vector2): WASD 2D 묶음 + 화살표 2D 묶음.
  - `Zoom`(Axis): `<Mouse>/scroll/y`.
- 배치·전투 두 Layer에 이 맵을 더한다(`StageInputLayers`). 그래서 배치·전투·적 턴·결과 재생 동안 움직이고, 설정 창 등으로 Layer가 바뀌면 멈춘다.
- 새 컨트롤러 `CombatCameraInputController`가 수신 계약에 `CameraPanChanged(x, y)`·`CameraZoomScrolled(delta)`를 넘긴다(입력 안내 §6 절차).
- 화면 가장자리 이동은 이미 들어오는 `PointerMoved`의 마지막 화면 좌표를 쓴다. 조건은 Q-CC5.
- 키 재설정 목록에는 넣지 않는다(Q-CC7). 2D 묶음 키를 바꾸는 화면이 아직 없다.
- 움직여도 되는지는 정책 함수 `CombatInputPolicy.CanMoveCamera(facts)` 하나다. 막는 때: 확인창 열림, 결과 화면, 연결 없음. 명령 제출 정지·재생 중은 막지 않는다.

### CC3. 이동

- 기준 = 프랍을 세운 순간 각 카메라의 월드 위치(F1).
- 오프셋은 **카메라 화면 축**(카메라 오른쪽·위쪽)으로 센다. 직교 카메라라 이 축으로 옮기면 화면이 그대로 미끄러진다.
- 이동량 = (키 방향 × 키 속도 + 가장자리 방향 × 가장자리 속도) × 시간. 대각선은 정규화한다.
- 속도는 보기 크기에 비례한다(크게 보면 더 빨리) — 화면에서 느끼는 속도가 줌과 무관하게 같다.
- 제한은 오프셋의 최소·최대(x = 좌우, y = 상하, 월드 단위). Q-CC3.
- 모든 맵 카메라에 같은 월드 이동을 더한다. 그래서 낮↔밤 전환에서 튀지 않는다.

### CC4. 줌

- 보기 크기 = 직교 크기. 처음 값은 프리팹 렌즈값(지금 7)이고, 설정은 최소·최대·한 칸·시간만 가진다(기본값 원본은 프리팹 하나). 처음 값이 제한 밖이면 제한 안으로 맞춘다.
- 휠 한 칸마다 보기 크기를 한 칸 바꾸고 창구가 `CameraZoomData`(지금 크기 → 새 크기, 설정 시간)를 보낸다. 연속 칸은 창구가 기록한 크기에서 이어진다.
- 연출 줌(포효·기차)과의 관계(Q-CC6):
  - 연출 배율은 지금 보기 크기에 곱한다(지금은 프리팹 렌즈값에 곱함).
  - 되돌림은 보기 크기로 간다.
  - 연출 줌 대여 중에는 휠 입력을 버린다(권장).
- 줌 기준점은 Q-CC4.

### CC5. 값과 연결

- 새 SO `CombatCameraSettingsSO`(표 밖 일반 SO, `MapDefinitionSO`와 같은 꼴). 위치 `DataBaseSO/ViewGroup/CombatCamera/`.
  - 칸: `keyPanSpeed`, `edgePanSpeed`, `edgeThickness`(화면 비율), `panMin`·`panMax`(Vector2), `zoomMin`·`zoomMax`, `zoomStep`, `zoomSeconds`.
  - 검사(`Validate`): 속도·두께·시간 0 이상, `panMin ≤ 0 ≤ panMax`, `0 < zoomMin ≤ zoomMax`, 한 칸 > 0.
- 연결은 **View 쪽 스테이지 데이터**의 맵 프랍 칸 `StageViewPropData`에 SO 참조 칸 `cameraSettings`를 더한다(Q-CC2). 카메라가 맵 프랍 안에 있으니 제한 값도 프랍 옆이 맞다. 배경이 프랍을 세울 때 같은 조회로 함께 읽는다.
  - InGame `CombatStageData`에는 넣지 않는다. 규칙 데이터는 View SO를 참조하면 안 된다(P1·P5).
- SO가 비어 있으면 Q-CC8.

### CC6. 수명

- 배경이 프랍을 세울 때 맵 카메라 사실에 Transform과 기준 위치를 더해 읽는다(`CombatMapCamera` 칸 둘).
- 표시 연결 때 시점이 오프셋 0·보기 크기 = 처음 값으로 시작한다.
- 표시 해제 때 카메라를 기준 위치·기준 크기로 바로 되돌린다. 배경은 씬 수명이라 카메라가 남기 때문이다(F5). 프랍 해제(씬 끝)는 되돌리지 않는다.
- 매 프레임 조정자 Tick에서 입력 → 시점을 한 번 진행한다(입력 묶음 `CombatSceneInput`에 한 줄).

### CC7. 다른 기능과 겹침

| 상대 | 결과 |
|---|---|
| 낮↔밤 화면 전환(민형님) | 두 카메라가 같이 움직여 튐 없음 |
| 흔들림(임펄스) | 위치 보정이라 Transform 이동과 더해짐 |
| 포인터 칸 찾기·커서 스냅·HUD | 출력 카메라 기준이라 그대로 맞음(F10) |
| 연출 줌 | CC4 |
| 보스 맵(F2) | 맵 카메라가 없으면 조작할 카메라가 없음(지금처럼 진입 자체가 전환 카메라 검사에서 막힘) |

## 3. 정할 것

| # | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-CC1 | 배치 단계 A 키 충돌(F8) | **A** 배치의 캐릭터 뽑기를 다른 키(예: F)로 옮긴다 | B 배치 단계에선 화살표만, WASD는 전투 단계만 / C 둘 다 동작(비권장) |
| Q-CC2 | SO를 어디에 연결하나(F11) | **A** View 스테이지 데이터의 맵 프랍 칸(`StageViewPropData`) | B InGame `CombatStageData`(규칙 P1·P5 위반, 비권장) |
| Q-CC3 | 이동 제한 기준 | **A** 처음 카메라 위치에서 화면 축 오프셋 최소·최대(스테이지마다 SO) | B 필드 격자에서 자동 계산(필드가 화면 밖으로 안 나가게) / C 월드 XZ 상자 |
| Q-CC4 | 줌 기준점 | **A** 화면 가운데 | B 마우스 커서 쪽(커서 아래 점이 고정되게 이동도 함께) |
| Q-CC5 | 가장자리 이동 조건 | **A** 창이 포커스이고 포인터가 창 안, UI 위가 아닐 때 | B UI 위여도 움직임(아래쪽 손패 위에서 계속 내려감) |
| Q-CC6 | 연출 줌 중 휠 | **A** 연출 줌 동안 휠을 버림 | B 보기 크기를 바꾸고 연출은 새 크기 기준으로 다시 보냄 |
| Q-CC7 | 키 재설정 목록 | **A** 넣지 않음(WASD·화살표 고정) | B 넣음(2D 묶음 재설정 화면이 필요) |
| Q-CC8 | 스테이지에 SO가 비었을 때 | **A** 필수 — 부트 DB 검사에서 실패 | B 비면 카메라 조작 없음 |

## 4. 원칙 자체 점검

- **SSOT:** 위치 작성자는 시점 하나, 렌즈 작성자는 창구 하나. 처음 크기 원본은 프리팹 렌즈 하나. 제한 규칙은 순수 함수 하나. 움직여도 되는지는 정책 함수 하나.
- **SOLID·SRP:** 입력(무엇을 눌렀나)·시점(어디로·얼마나)·창구(렌즈)·값이 나뉜다. 창구는 "보기 크기" 하나만 는다.
- **FW 지침·계약:** Framework 코드 변경 없음. 렌즈는 공개 `Dispatch`로만 바꾼다. 위치는 우리가 세운 프랍 인스턴스의 Transform이다. 입력은 기존 컨트롤러 꼴(맵·Layer·수신 계약)을 따른다.
- **변경 독립성:** 스테이지마다 값이 SO 하나. 키는 액션 에셋만. 새 조작(예: 회전)은 입력 칸 하나와 시점 함수 하나.
- 예상 약 8.7/10. 감점 예상: 창구가 보기 크기를 더 들고(C2 그림자 사본이 하나 늘어남), 보스 맵 카메라 부재는 이번에 풀지 않음.

## 5. Phase(결정 뒤 Task 계획으로)

| Phase | 내용 |
|---|---|
| CC-a 값 | `CombatCameraSettingsSO`·검사, `StageViewPropData.cameraSettings`, 기본 에셋 하나, 스테이지 12곳 연결(Q-CC8 A면 필수) |
| CC-b 시점 | 맵 카메라 사실에 Transform·기준 위치, 창구 보기 크기(연출 줌·되돌림 기준), `CombatCameraView`·`CombatCameraLimits`, 연결·해제 되돌림 |
| CC-c 입력 | 액션 맵 `CombatCamera`, Layer 둘, 컨트롤러·수신 계약·슬롯, `CombatCameraInput`, 정책 `CanMoveCamera`, 입력 묶음 Tick 한 줄, (Q-CC1 A면) 뽑기 키 이동 |
| CC-d 문서 | 입력 안내 절, 폴더 배치 안내, 공유(민형님: `StageViewPropData` 칸·Factory 연결 줄, 현강님: 뽑기 키 안내 문구가 있으면), CLAUDE.md |

- 상시 지시대로 MSBuild·새 `[Test]` 없음. 기존 시험은 컴파일이 깨지는 곳만 맞춘다(수신 계약 기록 가짜, 맵 카메라 사실 생성자).

## 6. 구현 메모 (2026-10-27, CC-a~CC-c, 계획 §6)

- 결정: Q-CC1은 "캐릭터 뽑기 기능 삭제됨"(배치 `DrawCharacter`는 받는 곳 없는 은퇴 액션 — A 키 충돌 없음), Q-CC2~Q-CC8 A. 계획에서 정한 P1~P13은 사용자 확인 그대로.
- 설계와 다르게 된 것:
  - 휠 액션은 Value가 아니라 **PassThrough** Axis다(굴릴 때마다 한 번씩 오게).
  - 카메라 입력 폴더는 `Input/Camera/`가 아니라 `Input/Cameras/`다(폴더 안내 이름 규칙).
  - 카메라 요청은 Router가 아니라 연결 입력 묶음(`CombatSceneInput`)이 카메라 입력으로 보낸다. 판정은 `CombatInputRouter.IsCameraRequest` 하나.
  - 입력이 시점에 거는 좁은 계약 `ICombatCameraControl`(`Input/Contracts/`)을 새로 두고, 표시 계약 `ICombatBattleDisplay.CameraControl`로 꺼낸다(입력 Factory가 받는 표시가 계약 형식).
  - 보기 크기는 모든 맵 카메라가 하나를 함께 쓴다 — 처음 렌즈값이 다르면 시점 생성에서 예외(P11). 맵 카메라가 있는데 조작 값이 없으면 시점 생성에서 예외(P12).
- CC4 "기록한 크기 → 새 크기, 설정 시간" 그대로 구현했다. 앞 줌이 끝나기 전에 다음 칸이 오면 렌즈가 앞 목표로 한 번 튈 수 있다(빠른 휠). 거슬리면 다른 안: 시점이 매 프레임 목표로 다가가며 0초 줌.
- 덤: 표시 해제 때 연출 줌 대여가 남아 있으면 렌즈가 연출 값에 남던 틈(배경이 씬 수명이 된 뒤)을 `RestoreView`가 덮는다.
- 남은 것: Unity 확인(계획 §5), 보스 맵 `Map.prefab`에는 맵 카메라가 없어 조작 없음(F2, 이번 범위 밖).
