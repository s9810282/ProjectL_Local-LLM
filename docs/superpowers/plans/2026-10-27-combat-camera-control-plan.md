# 전투 카메라 조작(이동·줌) Task 계획

- 작성일: 2026-10-27. 설계: [camera-control-design](../specs/2026-10-26-combat-camera-control-design.md)(CC1~CC7).
- 결정(2026-10-27, 사용자):
  - Q-CC1: "캐릭터 뽑기 기능 삭제됐음." 확인 — 배치 맵 `DrawCharacter` 액션은 에셋에만 남은 은퇴 액션이고 받는 곳이 없다(시험 `StageInputTests.RetiredDrawAction_HasNoReceiverBinding`). A 키는 충돌하지 않으므로 WASD를 그대로 쓴다. 은퇴 액션은 지우지 않는다.
  - Q-CC2~Q-CC8: 모두 A("나머지는 추천대로 진행해").
- 상시 지시: MSBuild를 돌리지 않고 `[Test]` 시험 코드를 새로 만들지 않는다. 기존 시험은 컴파일이 깨지는 곳만 최소로 맞춘다. 우리 변경으로 실행에서 깨지는 다른 사람 시험은 고치기 전에 묻는다. 보고에는 "컴파일 미확인"을 적는다.
- Phase마다 멈추고 보고한다. 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/`.

## 0. 계획에서 정한 것 (설계의 빈칸, 사용자 확인 대상)

| # | 내용 | 이유 |
|---|---|---|
| P1 | 휠은 이벤트마다 부호만 센다(한 이벤트 = 한 칸). 위로 굴리면 줌인(크기 작아짐) | Windows 휠은 칸마다 ±120 같은 값이라 크기로 세면 기기마다 다름 |
| P2 | 이동 축은 카메라마다 자기 오른쪽·위쪽(프랍을 세운 때 회전) | Day·Night 회전이 같으면 결과가 같고, 다르면 각자 화면 기준으로 같은 거리 |
| P3 | 이동 속도에 "지금 보기 크기 ÷ 처음 크기"를 곱한다 | 줌과 무관하게 화면에서 느끼는 속도가 같음(설계 CC3) |
| P4 | 처음 보기 크기 = 프리팹 렌즈값, 제한 밖이면 연결 때 제한 안으로 맞춤(0초) | 기본값 원본은 프리팹 하나(설계 CC4) |
| P5 | 카메라 키·휠은 수신 창구의 확인창 막기를 지나 상태로만 저장하고, 실제로 움직일지는 Tick에서 정책 함수가 정한다 | 확인창 동안 키를 떼면 "뗌"이 버려져 확인창이 닫힌 뒤 계속 움직이는 일을 막음 |
| P6 | 표시 연결이 끝날 때(표시 해제) 카메라 위치·크기를 처음 값으로 바로 되돌린다. 씬 끝(프랍 해제)은 되돌리지 않는다 | 배경은 씬 수명이라 전투 중간 재연결 때 카메라가 남음(설계 CC6) |
| P7 | 값 SO는 `DV/CombatPresentation/CombatCameraSettingsSO.cs`(같은 폴더의 `CombatUnitAnimationSO` 꼴), 에셋은 `DataBaseSO/ViewGroup/CombatCamera/CombatCameraSettings_Default.asset` 하나를 12곳이 함께 씀 | 표 밖 일반 SO 선례 |
| P8 | 시점 `CombatCameraView`는 표시 Factory가 창구·배경 카메라 사실·값으로 만들고 표시가 갖는다(연결 수명). 맵이 없으면(시험 조립, 배경 없음) 조작 없음 | 창구의 "효과 서비스 없으면 연출 없음"과 같은 꼴, Factory는 우리 조립 줄 |
| P9 | 키 입력은 수신 계약 `IStageInputReceiver`에 메서드 둘(`CameraPanChanged`·`CameraZoomScrolled`)을 더한다. 구현하는 시험 가짜 셋(`StageInputTests`·`ViewInputCompositionTests`·`BattleChangerUITests`)은 빈 몸으로 컴파일만 맞춤 | 별도 수신 슬롯은 Factory·서비스 키(민형님 영역) 변경이 필요해서 |
| P10 | 스테이지 값 필수 검사는 `ViewStageData` 생성(부트 DB 생성)에서 한다. 민형님 시험 `MapSoDatabaseAssemblyTests`가 값 없는 항목으로 DB를 만들면 실행에서 깨질 수 있다 — 그때는 고치기 전에 묻는다 | 상시 지시 |

## 1. CC-a 값

| Task | 내용 | 파일 |
|---|---|---|
| a1 | 값 형식 `CombatCameraSettingsData`(이동 속도·가장자리 속도·가장자리 두께·이동 최소·최대·줌 최소·최대·한 칸·시간, `Validate`)와 SO `CombatCameraSettingsSO`(`GetData()`가 새 값) | `DV/CombatPresentation/CombatCameraSettingsSO.cs`(+ `.meta`) |
| a2 | 맵 프랍 칸 `StageViewPropData.cameraSettings`(SO 참조)와 읽기 속성, `ViewStageData` 생성에서 필수·검사 | `DV/Table/StageViewProp/Item/StageViewPropDataSO.cs`, `DV/Table/ViewStage/Item/ViewStageDataSO.cs` |
| a3 | 기본 에셋 하나(값은 시작값, 재생으로 조정) | `DataBaseSO/ViewGroup/CombatCamera/CombatCameraSettings_Default.asset`(+ `.meta`, 폴더 `.meta`) |
| a4 | 스테이지 12곳(`StageViewProp_*.asset`)에 기본 에셋 연결 | `DataBaseSO/ViewGroup/StageViewProp/Item/*.asset` |

- 화면 변화 없음(읽는 곳이 아직 없음). 값이 비거나 잘못되면 부트 DB 생성에서 실패한다.

## 2. CC-b 시점

| Task | 내용 | 파일 |
|---|---|---|
| b1 | 맵 카메라 사실에 Transform·처음 월드 위치(끝 선택 인자, 시험 생성 그대로 컴파일), 배경이 프랍을 세울 때 값도 읽어 둠 | `C/Presentation/Cameras/CombatMapCameras.cs`, `C/Display/Map/StageViewPropPresenter.cs` |
| b2 | 창구 "보기 크기": 카메라마다 지금 보기 크기(처음 = 렌즈값), `TrySetViewSize(size, seconds)`(줌 대여 중·효과 없음·맵 없음이면 false), 연출 줌 = 보기 크기 × 배율, 되돌림 = 보기 크기, `RestoreView()`(처음 크기 0초) | `C/Presentation/Cameras/CombatCameraWindow.cs` |
| b3 | 제한 순수 함수 `CombatCameraLimits`(이동 오프셋·줌 크기 맞추기), 시점 `CombatCameraView`(`Pan(방향, 시간)`·`Zoom(칸)`·`Restore()`) | `C/Presentation/Cameras/`(새 둘 + `.meta`) |
| b4 | 표시가 시점을 갖고(`CameraView`), 표시 해제 때 `Restore()`. Factory가 만든다(P8) | `C/Display/CombatSceneDisplay.cs`, `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` |

- 화면 변화 없음(아직 아무도 부르지 않음). 연출 줌은 보기 크기 = 처음 크기라 지금과 같다.

## 3. CC-c 입력

| Task | 내용 | 파일 |
|---|---|---|
| c1 | 액션 에셋 새 맵 `CombatCamera`: `Pan`(Value Vector2, WASD 2D 묶음 + 화살표 2D 묶음), `Zoom`(Value Axis, `<Mouse>/scroll/y`) | `Assets/ProjectL/Input/PlayerInputActions.inputactions` |
| c2 | 이름 `StageInputNames.CombatCamera`, 두 Layer에 맵 추가 | `ViewSystem/StageInput/StageInputNames.cs`·`StageInputLayers.cs` |
| c3 | 수신 계약 메서드 둘(P9)·슬롯 전달·컨트롤러 `CombatCameraInputController`·`StageInputFactory`에 등록, 시험 가짜 셋 컴파일 맞춤 | `ViewSystem/StageInput/`(새 컨트롤러 + `.meta`), 시험 셋 |
| c4 | 입력 `CombatCameraInput`(누른 방향·휠 칸·마지막 포인터 저장, Tick에서 가장자리 방향 계산 — 창 포커스·창 안·UI 위 아님, 정책 통과 때만 시점 호출, 포커스 잃으면 비움) | `C/Input/Camera/CombatCameraInput.cs`(+ `.meta`, 폴더 `.meta`) |
| c5 | 수신 창구·Router가 카메라 요청 둘을 상태로 넘김(P5), 포인터 이동도 카메라 입력에 | `C/Input/CombatInputReceiver.cs`·`CombatInputRouter.cs` |
| c6 | 정책 `CombatInputPolicy.CanMoveCamera(facts)`(확인창·결과 화면·연결 없음이면 거짓) | `C/Input/CombatInputPolicy.cs` |
| c7 | 입력 Factory가 카메라 입력을 만들어 입력 묶음이 갖고, 조정자 Tick 끝에서 한 번 진행 | `C/Input/…Factory`, `C/Input/CombatSceneInput.cs`, `C/CombatViewSystem.cs` |

## 4. CC-d 문서

- 입력 안내 새 절(카메라 맵·Layer·수신 메서드 둘·P5), 폴더 배치 안내(새 파일), 설계 구현 메모.
- 공유(미전달로 작성): 민형님 — Factory 줄·`StageViewPropData`/`ViewStageData` 칸·입력 에셋 새 맵, 시험 가짜(다른 사람 파일) 목록.
- CLAUDE.md 현재 상태 줄.

## 5. Unity 확인 목록(사용자)

- 배치·전투 단계에서 WASD·화살표 이동, 화면 가장자리 이동(손패·HUD 위에서는 안 움직임), 이동 제한.
- 휠 줌인·줌아웃과 제한, 줌 크기에 따라 이동 속도가 화면 기준으로 같은지.
- 낮↔밤 전환 때 튐 없음, 포효·기차 연출 줌이 지금 줌 크기 기준으로 들어갔다 돌아오는지.
- 확인창·결과 화면·설정 창에서 멈춤, 확인창 동안 키를 떼도 계속 움직이지 않음.
- 이동·줌 뒤에도 칸 찾기·커서 스냅·유닛 HUD가 맞는지, 전투를 나갔다 들어오면 처음 위치·크기.
- EditMode 시험 전체(특히 `StageInputTests`·`UnityStageInputTests`·`CombatCameraWindowTests`·`DataBaseAssetTests`·`MapSoDatabaseAssemblyTests`).

## 6. 실행 기록

### 6.1 CC-a 값 (2026-10-27, 컴파일 미확인, 새 시험 없음, 기존 시험 변경 없음)

- a1: 값 `DV/CombatPresentation/CombatCameraSettingsData.cs`(불변 값, 조건 원본 `FindError` 하나 — 생성자와 SO가 같이 부름)와 SO `CombatCameraSettingsSO.cs`(`CreateData()`가 매번 검사한 새 값, 메뉴 `ProjectL/Database/View/Combat Camera Settings`), 각 `.meta`. 조건: 속도·시간 0 이상, 가장자리 두께 0~0.5, `panMin` 두 축 ≤ 0 ≤ `panMax` 두 축, 0 < `zoomMin` ≤ `zoomMax`, 한 칸 > 0, 숫자 아님·무한 거절.
- a2: `StageViewPropData.cameraSettings`(SO 참조, 블록 끝 칸)·읽기 `CameraSettings`(`UnitVisualData.AnimationPresentation` 꼴 — 비면 null, 있으면 `CreateData()`), `ViewStageData` 생성에서 비면 거절(값 검사는 읽기가 함).
- a3: `DataBaseSO/ViewGroup/CombatCamera/CombatCameraSettings_Default.asset`(+ `.meta`, 폴더 `.meta`). 시작값: 키·가장자리 속도 10, 두께 0.02, 이동 (−6, −4)~(6, 4), 줌 4~10, 한 칸 1, 0.15초. 맵 프리팹 `Stage.prefab` Day·Night 렌즈는 둘 다 7이라 범위 안.
- a4: `StageViewProp_*.asset` 12곳 모두 `cameraSettings` 한 줄(보스 포함 — 보스 맵 `Map.prefab`에는 카메라가 없어 CC-b 뒤에도 조작 없음).
- 계획과 다른 점: 값 형식과 SO를 한 파일이 아니라 두 파일로 나눴다(같은 폴더 `CombatUnitAnimationData`/`SO` 선례). 동작 차이 없음.
- 확인: 민형님 `MapSoDatabaseAssemblyTests`는 `StageViewPropTable(IDataProvider)`가 SO를 직접 읽어 `ViewStageData`를 거치지 않으므로 필수 검사에 걸리지 않는다(P10 걱정 해소). `ViewStageDataSO` 에셋은 이 12개뿐(스크립트 GUID 검색). 시험 `TerrainOperationalDisplayTests:341`은 에셋을 복제하므로 참조가 따라간다.
- 화면 변화 없음. 값이 비거나 잘못되면 부트 DB 생성에서 실패한다.

### 6.2 CC-b 시점 (2026-10-27, 사용자 "P1~P9 그대로, CC-b 진행해", 컴파일 미확인, 새 시험 없음, 기존 시험 변경 없음)

- b1: 맵 카메라 사실 `CombatMapCamera`에 끝 선택 인자 `transform`과 `Transform`·`BasePosition`·`BaseRotation`(넘긴 순간의 월드 자세). `CombatMapCameras.Read`가 카메라 Transform을 넘긴다(프랍 배치 뒤라 월드 값). 배경 `StageViewPropPresenter`가 같은 조회에서 `CameraSettings`를 한 번 읽어 두고 `Release`에서 비운다(DB를 거치지 않은 시험 조회는 null — 우리 `CombatMapCameraTests`·민형님 `MapSoDatabaseAssemblyTests` 그대로 돎).
- b2: 창구 `CombatCameraWindow`에 카메라마다 보기 크기 `_viewLens`(맵을 받을 때 기준 렌즈값), `TrySetViewSize(size, seconds)`(`CanZoom`이 거짓 — 효과·맵 없음·연출 줌 대여 중 — 이면 false, 보내기 전에 기록), `RestoreView()`(바뀐 카메라나 대여가 남았을 때만 기준값 0초, 정리 규칙). 연출 줌 `DispatchZoom`은 보기 크기 × 배율, 대여 되돌림 `RestoreAllZoom`은 보기 크기로. 보기 크기 = 기준값이면 보내는 값이 지금과 같다(기존 창구·조각·기차 시험 기대값 그대로). 계약 `ICombatCameraZoom`·줌 조각 인자 주석을 "보기 크기"로.
- b3: 새 `Presentation/Cameras/CombatCameraLimits.cs`(오프셋·크기 맞추기 둘)·`CombatCameraView.cs`(+ 각 `.meta`). 시점은 `Begin()`(기준 위치·처음 크기, P4) · `Pan(키 방향, 가장자리 방향, 시간)`(방향 길이 1로 자름, 속도 × 보기 크기 ÷ 처음 크기 P3, 제한) · `Zoom(칸)`(양수 = 줌인 P1, 창구가 거절하면 버림 Q-CC6 A) · `Restore()`(기준 위치 + `RestoreView`, 정리 규칙).
- b4: 표시 생성자 새 필수 인자 `cameraView`(창구 바로 뒤)·읽기 `CameraView`, 해제 목록에서 `_camera.DetachMap` 바로 앞에 `_cameraView.Restore`. Factory가 배경이 있으면 그 맵 카메라·조작 값으로, 없으면 빈 목록으로 시점을 만들고 `Begin()`을 부른다. 표시를 직접 만드는 곳은 Factory뿐이라 시험 고침 없음.
- 계획에서 정하지 않았던 것(사용자 확인 대상):
  - P11 보기 크기는 모든 맵 카메라가 하나를 함께 쓴다. 그래서 맵 카메라의 처음 렌즈값이 서로 다르면 시점을 만들 때 예외(지금 `Stage.prefab` Day·Night는 둘 다 7). 다르면 첫 줌에서 한쪽이 튀기 때문.
  - P12 맵 카메라가 있는데 조작 값이 없으면 시점을 만들 때 예외(운영 DB는 필수 검사로 막음 — 이중 안전).
  - P13 `Begin()`은 Factory가 시점을 만든 직후 부른다. 그 뒤 Factory가 실패하면 되돌리지 않는다(정상 값이면 `Begin`은 위치를 그대로 두고 줌도 보내지 않아 남는 것이 없음).
- 설계대로 둔 것과 걱정(보고): 휠 한 칸 = 창구가 "기록한 크기 → 새 크기, 설정 시간" 줌을 보냄(설계 CC4). 앞 줌이 끝나기 전에 다음 칸이 오면 효과가 새 줌의 시작값(앞 목표)으로 바로 옮겨 렌즈가 한 번 튈 수 있다(빠르게 굴릴 때). 다른 안: 시점이 매 프레임 목표로 다가가며 0초 줌을 보냄(효과 보간을 쓰지 않음).
- 덤으로 고쳐진 것: 표시 해제 때 연출 줌 대여가 남아 있으면 지금까지는 렌즈가 연출 값에 남았다(배경이 씬 수명이 된 뒤 생긴 틈). 이제 `RestoreView`가 기준값으로 덮는다.
- 화면 변화 없음(시점을 부르는 입력은 CC-c). 연출 줌은 보기 크기 = 처음 크기라 지금과 같다.

### 6.3 CC-c 입력 (2026-10-27, 사용자 "P11~P13 그대로, CC-c 진행해", 컴파일 미확인, 새 시험 없음)

- c1: 액션 에셋에 새 맵 `CombatCamera`(Combat 맵 뒤): `Pan`(Value Vector2, 2D 묶음 WASD·화살표), `Zoom`(**PassThrough** Axis, `<Mouse>/scroll/y`). 휠은 굴릴 때마다 한 번씩 오게 PassThrough로 했다(Value는 같은 값이 이어지면 다시 안 올 수 있음). C# 래퍼 생성은 꺼져 있다(`generateWrapperCode: 0`).
- c2: `StageInputNames.CombatCamera`(Map·Pan·Zoom), 배치·전투 Layer 둘 다 이 맵을 켬(두 Layer 사이 전환 때 끄지 않음).
- c3: 수신 계약 `IStageInputReceiver`에 `CameraPanChanged(x, y)`·`CameraZoomScrolled(delta)`, 슬롯 전달 두 줄, 새 `StageInput/CombatCameraInputController.cs`(+ `.meta`, `input.Vector2`·`input.Float`), `StageInputFactory`에 등록. 계약을 구현한 시험 가짜 셋(`BattleChangerUITests` 현강님·`StageInputTests` 우리·`ViewInputCompositionTests` 호진님)에 빈 메서드 둘(P9, 컴파일만).
- c4: 새 `Input/Cameras/CombatCameraInput.cs`(+ `.meta`, 폴더 `.meta`). 키 방향·휠 칸(부호만, P1)·마지막 포인터를 상태로 저장, `Tick(canMove, 시간)`은 휠 칸을 늘 비우고 허용될 때만 줌 → 이동. 가장자리 = 포인터가 창 안·띠 안(두께 × 화면 짧은 변)·UI 위 아님(Router와 같은 가림 판정), 포커스 상실이면 모두 잊음(Q-CC5 A).
- c5: Router 요청 종류에 `CameraPan`·`CameraZoom`, 판정 하나 `CombatInputRouter.IsCameraRequest`. 수신 창구가 확인창 동안에도 카메라 요청을 통과시킴(P5). 조정자 `RouteInput`은 카메라 요청을 선택·가용성·버튼 갈래 없이 묶음에 바로 넘김. 묶음 `CombatSceneInput.Route`가 카메라 요청을 Router로 보내지 않고 카메라 입력에 넘기고, 포인터 이동·포커스 상실도 카메라 입력에 전달.
- c6: 정책 `CombatInputPolicy.CanMoveCamera(facts)` = 입력 연결·전투 그림·종료 아님·확인창 닫힘·실제 Layer가 배치/전투.
- c7: 입력 Factory가 `CombatCameraInput(display.CameraControl, display, 화면 크기)`를 만들어 묶음 생성자(새 필수 인자 끝)에 넘김, 묶음 `TickCamera(facts, 시간)`·정지 때 놓음, 조정자 `Tick` 끝(행동 버튼·환경 호버 다음)에서 한 번.
- 계획과 다른 점(보고):
  - 폴더를 `Input/Camera/`가 아니라 `Input/Cameras/`로 — 폴더 안내 규칙(네임스페이스 `Camera`는 UnityEngine 형식을 가림).
  - 카메라 요청을 Router가 아니라 연결 입력 묶음이 받는다(Router는 입력 기능 의도만, `Route`로 오면 지금처럼 예외).
  - 표시 계약 `ICombatBattleDisplay`에 `CameraControl`(좁은 계약 새 `Input/Contracts/ICombatCameraControl.cs` + `.meta`)을 더했다. 입력 Factory가 받는 표시가 계약 형식이라서. 시점 `CombatCameraView`가 이 계약을 구현(`EdgeThickness` 추가, `Pan`·`Zoom` public). CC-b의 `CombatSceneDisplay.CameraView`(구체 형식) 읽기는 `CameraControl`로 바꿈.
  - 그래서 계약을 구현한 우리 시험 가짜 `CombatViewSystemTests.FakeDisplay`에 한 줄(맵 카메라 없는 실제 시점, 컴파일 맞춤).
  - 우리 `UnityStageInputTests.CreateStageAsset`(메모리 에셋)에 `CombatCamera` 맵을 더했다. Layer 런타임이 Layer의 맵을 찾지 못하면 예외라 실행에서 깨지는 것을 미리 맞춤(우리 시험, 단언 그대로).
- 다른 사람 시험 실행 영향 예상: 없음(호진님 `ViewInputCompositionTests`는 실제 프로젝트 에셋을 읽음).
- 작업 트리에 이번 작업이 아닌 `StageViewProp/Item/Test.asset`(stageKey 10000)과 두 표 등록이 생겼다. 카메라 값이 연결돼 있어 필수 검사에 걸리지 않는다.

### 6.4 CC-d 문서 (2026-10-27, 코드 변경 없음, 공유는 모두 미전달)

- 입력 안내 `viewDocs/stage-input-guide.md`: §4 수신 메서드 표에 두 줄, §5 Layer 표에 `CombatCamera`, §11 파일 표에 두 줄, 새 §13(액션·흐름·P5·정책·휠·가장자리 규칙).
- 폴더 배치 안내 `viewDocs/combat-view-folder-guide.md`: 갱신 줄, 트리·2단 표에 `Input/Cameras/`, `Presentation/Cameras/`에 시점·제한, `Input/Contracts/` 예에 `ICombatCameraControl`.
- 설계 §6 구현 메모(설계와 다르게 된 것 다섯, 빠른 휠 걱정, 덤으로 고친 틈, 남은 것).
- 공유: 민형님 Factory 공유 §32(View DB 칸·Factory 줄 둘·창구 동작 변화·맵 카메라 위치·입력), 현강님 메모 §17(`BattleChangerUITests` 가짜), 호진님 메모 §14(입력 에셋 새 맵·`ViewInputCompositionTests` 가짜·새 스테이지 프랍 필수 칸).
- 사용자 지시 "작업 내용들 스테이지에 올려줘"로 이번 작업(CC-a~CC-d) 파일만 스테이지했다(커밋은 사용자). 다른 작업과 섞인 `CLAUDE.md`·호진님 메모는 우리 줄만 스테이지했다.
