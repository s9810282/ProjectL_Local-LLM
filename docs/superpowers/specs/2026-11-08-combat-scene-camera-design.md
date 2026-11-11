# Combat 씬 공용 카메라 설계

- 작성: 2026-11-08. 갱신: 2026-11-09, 전투 View 작업.
- 상태: **Phase A·B 구현 완료. 운영·특성 확인·편집 도구 전환 및 MSBuild 완료. Phase C 문서·인계 완료, 직접 Unity Play 진입/종료·씬 참조 복구 확인. 시각·전체 회귀 확인 대기. 2026-11-09 원점 기준 루트 D0 및 라이브 자세 D0.1 정적 구현 완료·Unity 확인 대기. 맵 작업실 D1~D3 코드와 MSBuild 완료. Unity에서 Stage 2201의 저장된 Combat 카메라 읽기와 16:9 프리뷰 렌더 확인, 보정·Undo·저장/재열기·Play 비교는 미확인.**
- 2026-11-09 후속 방향: 사용자가 씬 직접 편집과 Game 뷰 확인을 선택했다. D1~D3 구현 이력은 유지하고, 다음 편집 흐름의 설계 원본은 [전투 배경 씬 작업 설계](2026-11-09-combat-map-authoring-scene-design.md)로 옮긴다.
- 사용자 확정 요구: Combat 씬에 공용 카메라 하나를 두고 기존 이동·줌·흔들림을 유지한다. 최초 요청은 설계·Task 계획이며, 2026-11-08 후속 요청으로 Phase 진행과 마지막 MSBuild를 인수했다.
- Task 계획: [combat-scene-camera-plan](../plans/2026-11-08-combat-scene-camera-plan.md).
- 이 문서는 아래의 구현 선택을 제안한다. 승인 후에도 Phase가 끝나면 멈추고 보고하며, 계획과 다른 결정은 먼저 묻는다.

## 2026-11-09 목적 재확인 및 후속 설계 경계

사용자가 공용 카메라 전환을 시작한 실무 목적은 **`StageViewProp`으로 생성되는 배경의 주변 프랍을 배치하면서, Play를 반복하지 않고 실제 전투 구도를 확인하는 것**이다. Phase A~C는 전투 시점과 설정의 소유권을 Combat 씬으로 옮겼지만, 이 작업 흐름 자체를 완성하지는 않았다. 공용 카메라 전환 완료와 작업 중 배경 구도 확인 완료를 별도 완료 조건으로 기록한다.

현재 `CombatLayoutWindow` SO 모드의 3D 미리보기는 배경·타일·위치 보정을 Play 없이 표시하고 저장할 수 있다. 그러나 `CombatStageSoPreview`는 독립적인 회전·거리·투영 카메라를 사용하므로 실제 Combat 공용 카메라 프레임을 보여주지 않는다. `StageViewPropPresenter`는 Play 중 배경을 세우며, `PrefabWithTiles`에서는 타일 기준점 정렬로 위치 보정 X/Z가 상쇄된다. 따라서 이 방식의 가로 위치를 단순히 X/Z 보정으로 조절할 수 있다고 안내해서는 안 된다.

후속 편집 작업의 수락 기준은 **맵 프리팹의 주변 프랍을 선택·배치하는 같은 작업 화면에서** 선택 스테이지의 실제 배경 배치 결과를 공용 카메라 기준 프레임으로 Play 없이 계속 확인하는 것이다. 프랍을 선택하거나 움직일 때 미리보기가 사라지면 안 된다. 필요할 때 편집 가능한 원본 값(`StageViewProp` 보정 또는 타일 배치 기준)을 참조·조정할 수 있어야 하지만, 별도 스테이지 편집 창을 매번 열고 닫는 흐름을 기본 조작으로 만들지 않는다. 미리보기 결과와 런타임의 배경 배치 계산은 한 계약을 공유해야 한다. 프리뷰 전용 복사 계산을 추가하지 않으며, 미리보기 객체는 원본 프리팹·저장본·열린 씬을 훼손하지 않고 정리한다. 에디터의 참고 조명·후처리 차이는 별도로 표시하고 픽셀 단위 동일성을 완료 조건으로 주장하지 않는다.

현재 프랍을 직접 배치하는 화면은 `ProjectL/FBX 배치`의 `FbxPlacementWindow`다. 이 창은 별도 PreviewScene의 맵 프리팹을 편집·저장하며, 현재 카메라 미리보기는 **맵 안의 가상 카메라가 선택된 동안에만** 나타난다. 공용 카메라 전환 후 이 조건은 목적과 맞지 않는다. 후속 설계는 이 작업실의 상시 전투 구도 미리보기를 우선한다. 기존 전투 스테이지 편집 창(`CombatLayoutWindow`)에 시점만 추가하는 안은 프랍 배치 화면을 오가야 하므로 채택하지 않는다. 이 후속 범위는 기존 SC9의 폐기 카메라 설정 정리와 구별한다.

작업실에는 선택한 스테이지의 **운영 `ViewStageDataSO`**를 미리보기 맥락으로 명시한다. 에셋 파일이 `StageViewProp_*`이고 `StageViewProp` 폴더에 있어도 실제 스크립트는 `ViewStageDataSO`다. `ViewDataBaseFactory`는 `ViewStageTable`을 만들고 `StageViewPropTable(viewStageTable)`로 배경을 공급하므로, 독립 `StageViewPropDataSO`는 현재 운영 조회의 원본이 아니다. 같은 맵 프리팹이 여러 스테이지에서 다른 보정·타일 기준을 쓸 수 있으므로 파일 이름만으로 임의의 스테이지를 고르지 않는다. 선택 SO가 운영 ViewStage 표에 등록되지 않았거나 현재 열린 맵 프리팹을 참조하지 않거나 필드/보드 맥락을 만들 수 없으면 실제 구도라고 표시하지 않고 원인을 알린다. 공용 시점의 원본은 저장된 Combat 씬의 `CombatSceneRoot.sceneCamera` → `CombatSceneCameraBinding`과 씬 인스턴스 값이며, 맵 프리팹에 카메라를 복제·저장하지 않는다. 맵 작업실의 오래된 카메라 편집 기능은 별도 범위에서 정리한다.

### 2026-11-09 기준 루트 결정

배경 작업의 좌표 기준을 보기 쉽게 하려고 사용자는 **기준 루트 X/Z = 0, 현재 전투 구도 유지**를 선택했다. 현재 가상 카메라는 `(14.3025, 19.95, -13.2525)`에서 45°/−45°로 필드 원점을 향한다. 가상 카메라 자체의 X/Z만 0으로 바꾸면 같은 회전에서 화면 중심이 원점에서 벗어난다. `CombatCamera.prefab`에 위치 `(0, 0, 0)`, 회전 0, 배율 1인 `CombatCameraOrigin` 루트를 추가하고 기존 `CombatCamera`와 Binding을 자식으로 둔다. 자식의 로컬 변환과 렌즈는 그대로 두어 월드 구도를 유지한다. Combat 씬과 특성 확인 씬의 기존 프리팹 인스턴스·Binding 참조는 유지한다. 기준 루트는 좌표 표식이며 팬·줌·효과·설정의 새 소유자가 아니다.

**Play 중 하이어라키 조정 제약(D0 시점):** 공용 설정은 `Use Zoom Pose`가 켜져 있다. `CombatCameraView`가 연결 시 월드 기준 자세를 snapshot으로 읽어 `CombatCameraZoomPosePath`의 줌 양 끝점을 만들고, `CombatCameraZoomPose`가 Cinemachine의 RawPosition·RawOrientation을 해당 끝점에서 매 프레임 계산했다. 따라서 D0만으로는 Play 중 `CombatCameraOrigin` 또는 가상 카메라 Transform을 움직여도 게임 화면의 시점이 따라오지 않았다. 다음 D0.1 결정으로 이 제약을 보완한다. D1~D3는 별도로 **Play 전 편집 상태의** 공용 시점과 배경 배치를 맵 작업실에 보여준다.

**D0.1 후속 결정:** 사용자는 `Use Zoom Pose`를 유지하며 Play 중 Transform 조정도 화면에 반영하기를 요청했다. 경로의 양 끝점과 렌즈 기반 보간은 기존 계약 그대로 두고, 경로가 만든 기준 자세를 현재 가상 카메라 Transform의 이동·회전 차이만큼 옮겨 Cinemachine에 전달한다. 최초 기준 자세는 기존 snapshot 한 곳에서 온다. 현재 Transform은 라이브 편집 기준이며 별도 누적 오프셋을 저장하지 않는다. 팬은 기존 `CombatCameraView` 소유 오프셋을 경로가 적용하고, 흔들림 correction과 줌 렌즈 대여는 유지한다. Follow·LookAt이 없다는 기존 바인딩 검사를 전제로 가상 카메라 Transform은 Cinemachine 출력 자세의 되먹임을 받지 않는다. 표시 해제 시 원래 snapshot 자세로 복원하는 기존 정책은 유지한다. 매 프레임 계산은 한 카메라에 대해 상수 시간이며 새 GameObject·컬렉션을 만들지 않는다.

미리보기 수명은 작업실의 편집 맵과 분리한다. 편집 맵을 변형해 런타임 보정을 적용한 채 저장하지 않는다. 배경 배치·타일 맞춤·필드 간격은 기존 View 계약을 재사용하고, 스테이지 맥락/프랍 변경 시에만 필요한 표시를 갱신한다. 드래그 중 매 프레임 전체 맵을 다시 생성하지 않으며, 창 닫기·맵 교체·Play 진입·스크립트 재로딩에서 임시 표시와 렌더 자원을 정리한다. 미리보기 영상의 종횡비를 실제 비교 기준과 함께 명시한다.

**D1 경계와 D2 렌더 방식:** `FbxPlacementWindow`는 선택·상태 표시만 맡고, `CombatStageSoAssets`가 ViewStage/StageRoot/Field의 직렬화 경로를 읽는다. `FbxCombatCameraSource`는 저장된 Combat 씬을 명시적 새로고침 때만 PreviewScene으로 열어 카메라와 설정을 읽고 `finally`에서 닫는다. Tool.Editor가 Systems 내부 SO 타입을 새 어셈블리 참조로 직접 가져오지 않는다. D2는 편집 중인 작업실 맵을 변형하지 않고 **별도 PreviewScene의 표시용 복사본**을 공용 시점으로 렌더한다. 복사본은 맵 교체·구조 변경 때 다시 만들고, 프랍 Transform 변경은 대응 복사본에 반영해 드래그 중 전체 맵 재생성을 피한다. 타일 맞춤의 순수 계산은 Systems와 Tool.Editor가 모두 참조하는 `ProjectL.Tool.Runtime`의 작은 함수로 추출하고 기존 런타임·편집 호출자가 공유한다. 이 공유 함수에는 DB 조회·렌더·카메라·서비스 책임을 넣지 않는다. D2의 실제 규모에서 갱신 비용을 확인하고 조정한다.

**D2 구현 경계:** `FbxCombatStagePreview`가 작업실 맵 복사본·바닥 타일·렌더 카메라·RenderTexture를 소유하고, `FbxPlacementWindow`는 운영 스테이지와 저장된 Combat 씬 카메라를 선택·표시한다. 미리보기는 프랍 선택과 무관하게 같은 창 오른쪽 위에 유지한다. 구조가 같으면 10 Hz로 자식 Transform·활성 상태만 맞추고, 계층·작업 맵·ViewStage 맥락이 바뀌면 복사본을 다시 만든다. 런타임과 편집 미리보기의 회전·기준 타일 이동은 공통 `CombatTileAlignmentMath`를 사용한다. 시작 자세는 Zoom Pose의 줌아웃 오프셋과 최대 크기(일반 줌은 제한된 프리팹 렌즈)를 적용한다. 타일 후보 및 무작위 회전은 에디터 고정 시드이므로 런타임의 무작위 외형과 달라질 수 있으며, 배치 좌표·기준점 비교가 수락 대상이다. 미리보기 조명·후처리·베이크 적용 차이는 D3의 비교 안내에서 명시한다.

**D3 수명·작업 흐름:** 작업실은 선택한 운영 ViewStage의 위치·회전·배율과 타일 오프셋·기준 타일을 같은 창에서 Undo 가능한 SO 편집으로 노출한다. 타일 방식에서 위치 X/Z는 기준 타일 정렬로 덮이므로 그 사실을 바로 안내한다. 프리팹 배치는 기존 맵 저장 동작, ViewStage 보정은 프로젝트 에셋 저장으로 확정한다. 별도 PreviewScene/RenderTexture는 창 비활성·교체·Play 진입·파괴에서 해제한다. 에셋 재임포트 뒤 관련 운영 SO·표·맵·Combat 씬 종속성만 다시 읽는다. 계층이 같을 때는 10 Hz로 Transform 변화를 검사하지만 렌더는 변화·카메라·크기 갱신에만 수행한다. 화면은 고정 16:9 프레임이며 Game View 비율이 다르면 가장자리 구도가 달라진다. 조명·베이크·후처리 및 런타임 무작위 타일 외형은 참고용으로 명시한다. 머티리얼/컴포넌트의 저장 전 외부 변경은 명시적 '다시 읽기'로 갱신한다.

## 1. 목표와 경계

맵 프리팹의 `Day Camera`·`Night Camera` 존재 여부가 전투 진입·카메라 조작·연출을 결정하지 않게 한다. 전투 시점의 실체와 기준 구도는 Combat 씬이 소유한다.

여기서 **공용 카메라 하나는 전투용 CinemachineVirtualCamera 하나**다. 화면을 출력하는 Unity Camera는 기존 Boot의 `Main Camera`와 CinemachineBrain을 그대로 사용한다. Unity 출력 카메라를 Combat 씬에 추가하지 않는다. 현재 출력 카메라 카탈로그는 최초 바인딩에서만 수집하고 루트를 유지하므로, 출력 카메라까지 씬 소유로 바꾸는 것은 별도 Render 계약 변경이다.

포함:

- Combat 씬 공용 시점, 기존 WASD·가장자리 이동·휠 줌·줌 자세·사건 줌·Impulse 흔들림.
- 배경에 남아 있는 가상 카메라의 런타임 간섭 차단.
- 공용 조작 설정 연결, 사용하지 않는 Day 이름 의존 제거.
- 같은 전투 표시를 쓰는 특성 확인 환경과 스테이지 편집 도구의 카메라 연결 정합성.
- 실패 복구·표시 재연결·씬 이탈·재진입 수명.

범위 밖:

- Core, FieldCamera, Architecture, MapAssembler, Render 카탈로그·효과 시스템·패키지 소스 수정.
- 원본 맵 프리팹의 카메라 삭제, 맵 작업실 카메라 도구 재구성, Missing Script 에셋 삭제.
- 낮/밤 자동 화면 전환 재활성화, 새 회전 조작, 연속 휠 보간 정책 개선, 맵별 구도 override 기능.
- 카메라 배치에 맞춘 HUD·전투 규칙·입력 정책 변경.

## 2. 현재 코드에서 확인한 사실

경로 약어: `View` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem`, `ViewFactory` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View`, `ViewData` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View`.

| 사실 | 코드·에셋 근거 | 영향 |
|---|---|---|
| 출력 카메라는 Boot에 있고 Combat의 `worldCamera`는 비어 있다 | `Assets/Scenes/Boot.unity`, `Assets/Scenes/Combat.unity`, `View/Combat/CombatSceneRoot.cs` | 출력·포인터 투영 경로와 가상 시점을 구분해야 한다 |
| Unity 출력 카탈로그는 최초 등록 후 새 씬 카메라를 추가하지 않는다 | `docs/render-camera-catalog.md`, `UnityCameraCatalogSystem` | 새 출력 카메라를 놓는 방식은 현재 서비스 계약과 맞지 않는다 |
| 배경 배치 뒤 내부 가상 카메라·기준 자세·렌즈·채널을 수집한다 | `View/Combat/Display/Map/StageViewPropPresenter.cs`, `Presentation/Cameras/CombatMapCameras.cs` | 맵 내부 카메라가 없으면 현재 조작·효과 창구가 비어 있다 |
| 이동·목표 크기는 View, 렌즈 요청·연출 줌 대여·흔들림 겹침은 Window가 담당한다 | `CombatCameraView.cs`, `CombatCameraWindow.cs` | 알고리즘을 유지하고 공급원·수명 연결을 바꿀 수 있다 |
| 줌 자세는 실제 렌즈를 읽어 위치·회전을 맞추며 Impulse correction을 보존한다 | `CombatCameraZoomPose.cs`, `CombatCameraZoomPosePath.cs` | 새 카메라에도 같은 adapter를 사용한다 |
| Factory는 자동 전환이 꺼져 있어도 아침 전환 대상 카메라를 요구한다 | `ViewFactory/CombatSceneDisplayFactory.cs`의 `RequireMapCamera`, `CombatPhaseTransitionPresenter.TryBegin` | 맵의 Day 카메라 필수 검사를 제거해야 한다 |
| 아침 대상 이름 기본값·운영 에셋 값은 `Day Camera`다 | `CombatPresentationSettingsDataSO.cs`, `CombatPresentationSettings_Default.asset` | 검사만 지우면 비활성 전환 설정에 옛 대상이 남는다 |
| 카메라 설정은 StageViewProp에 있고 ViewStage DB 생성에서 필수 검사한다 | `StageViewPropDataSO.cs`, `ViewStageDataSO.cs` | 공용 설정으로 옮기면 DB 검사·편집 도구도 함께 맞춰야 한다 |
| 현재 StageViewProp 아래 확인한 21개 에셋의 설정 GUID가 같다 | `d3d258fc9dc94d5a8297d2f88842ccef` = `CombatCameraSettings_Default` | 현재 공용 SO의 값은 유지하고 연결 위치를 옮긴다. 구현 전 재조사한다 |
| 특성 확인 표시도 배경의 카메라 목록을 받는다 | `ViewFactory/CombatTraitPreviewFactory.cs`, `View/Dev/TraitPreview/TraitActivationPreviewHost.cs` | 운영 경로만 전환하면 도구의 줌·흔들림이 누락된다 |
| 스테이지 편집 도구가 cameraGuid를 읽고 쓴다 | `CombatStageSoAssets.cs`, `CombatStageSoDocument.cs`, `CombatLayoutWindow.SoMode.cs` | 폐기 필드의 SerializedProperty 조회·저장을 같이 제거해야 한다 |
| 카탈로그는 비활성 가상 카메라도 수집하고 이름 중복을 거절한다 | `CameraCatalogSystem.BindCamera`, `docs/render-camera-catalog.md` | 옛 맵 카메라를 끄기만 해서는 이름 충돌을 해결하지 못한다 |

현황은 현재 코드 기준이다. 이전 문서의 당시 브랜치·테스트·빌드 성공 기록을 이번 검증으로 사용하지 않는다. GwonCheonmyeong 작성 문서는 설계 근거로 사용하지 않는다.

## 3. 결정 제안

### SC1. 씬에 전투용 가상 카메라 하나를 명시적으로 연결

- `Assets/ProjectL/Prefabs/Stage/CombatCamera.prefab`을 만들고 Combat 씬에 한 인스턴스를 둔다. 이름은 Binding의 실제 가상 카메라에서 읽는다. 최초 제안은 `Combat Camera`였고 2026-11-09 현재 프리팹 이름은 `CombatCamera`다. 카탈로그 안의 고유 이름 조건은 유지한다.
- 기존 유지 출력 카메라·Brain을 사용한다. 이 프리팹에 Unity Camera·Brain·AudioListener는 추가하지 않는다.
- `CombatSceneRoot`에 공용 카메라 바인딩 참조 하나를 둔다. Tick·입력에서 이름/태그/Find로 전투 시점을 다시 찾지 않는다.
- `worldCamera`는 기존 출력·좌표 변환 용도다. 공용 가상 카메라와 같은 필드로 합치지 않는다. Boot의 유지 객체를 Combat 씬에 직렬화 연결하려고 하지 않는다.
- 카메라는 배경 인스턴스의 자식으로 두지 않는다. 씬의 안정된 전투 루트 아래에 두고, 맵 타일 맞춤·배경 회전·스케일을 상속받지 않는다.
- 씬 바인딩은 정확히 한 전투 카메라를 요구한다. 누락·잘못된 참조는 연결 전에 원인과 객체를 포함한 예외로 거절한다.

### SC2. 카메라 참조·조작 설정은 한 바인딩에서 제공

새 `CombatSceneCameraBinding`은 `View/Combat/Presentation/Cameras/`의 작은 MonoBehaviour로 둔다.

역할:

- 직렬화된 `CinemachineVirtualCamera`와 `CombatCameraSettingsSO` 참조를 가진다.
- 구조·렌즈·Listener·설정의 준비 조건을 검사하고, 연결 때 읽을 참조·새 불변 설정 값을 제공한다.
- 입력 처리·Tick·카탈로그 수집·서비스 조회·효과 실행·팬 상태를 가지지 않는다.

`CombatSceneRoot`는 이 바인딩 하나를 참조하고, Factory가 필요한 값만 소비자에게 넘긴다. 모든 설정 값을 Root에 복사하거나 기능 코드를 Root로 모으지 않는다.

공용 설정은 기존 `CombatCameraSettings_Default` SO를 그대로 연결한다. SO의 이동 속도·이동 제한·가장자리 두께·Size 범위·Step·Seconds·줌 자세 오프셋과 `CreateData` 검증은 유지한다. 맵별 설정 우선순위·fallback은 새로 만들지 않는다.

최종 상태에서는 `StageViewPropData.cameraSettings`·`CameraSettings`, `ViewStageData`의 카메라 설정 필수 검사, 편집 도구의 `cameraGuid` 사용을 제거한다. 배경 DB는 배경 유효성만 검사한다. SO 에셋 자체는 삭제하거나 새로 복제하지 않는다.

이 선택은 **현재 공용 설정을 씬에 연결하는 제안**이다. 향후 맵별 이동 범위·구도가 필요하면 별도 설계한다. 구현 전 다른 설정 SO를 쓰는 에셋이 새로 발견되면 공용값으로 덮지 말고 먼저 사용자에게 묻는다.

### SC3. 기준 사실만 일반화하고 이동·줌 알고리즘은 유지

- `CombatMapCamera`를 `CombatCameraSnapshot`으로 이름을 바꾸고 `.meta` GUID를 유지한다. 사실은 Name·BaseLens·ShakeChannel·Transform·BasePosition·BaseRotation이다.
- 맵 하위 전체 수집기 대신 **명시적으로 받은 가상 카메라 한 개**에서 snapshot을 만드는 경로를 둔다. 연결 전 기준 자세·렌즈를 읽는다.
- Window·View 내부의 목록 기반 계산은 이번에 단일 객체 전용으로 재작성하지 않는다. Factory가 같은 snapshot 하나가 든 읽기 전용 목록을 두 소비자에게 전달한다. 운영·특성 확인 바인딩은 정확히 하나를 요구한다.
- `AttachMap`·`DetachMap`처럼 공급원을 가리키는 메서드는 `AttachCameras`·`DetachCameras`로 맞춘다. `CombatCameraWindow`의 보기 크기·줌 대여·흔들림 겹침 규칙은 유지한다.
- 기준 자세는 표시 연결마다 **변경 전 한 번** 읽는다. 해제 시 복원하고 재연결 때 새로 읽는다. View와 별도 baseline 저장소를 동시에 유지하지 않는다.

### SC4. 기존 조작·연출 계약을 보존

| 기능 | 유지할 계약 |
|---|---|
| WASD·가장자리 이동 | 기존 InputSystem·Layer·슬롯·`CombatCameraInput`·`ICombatCameraControl`, 입력 허용·차단 정책, 대각선 보정·이동 제한·줌 비례 속도 |
| 사용자 줌 | 기존 Step·Seconds·Min/Max, 연출 줌 대여 중 휠 거절 |
| 줌 자세 | `UseZoomPose` 두 모드, 실제 렌즈 기반 보간·팬 유지·끝점 제한, 자동 adapter 연결·해제 |
| 사건 줌 | Window 한 곳의 대여·배율·반환, 현재 사용자 보기 크기로 복원 |
| 흔들림 | 공개 CameraEffect Dispatch, 기존 강도·시간·채널·Shape·겹침 규칙, CinemachineImpulseListener 수신 |
| 해제 | adapter 해제 → 기준 자세·렌즈 복원 → 대여 취소·참조 해제. 반복 해제 안전 |

새 카메라는 직교 투영을 **명시**한다. `ModeOverride=None`의 출력 상속에 기대지 않는다. Follow/LookAt 없음, 실제 자세를 쓰는 Body/Aim 없음, 상위 VirtualCamera 없음 등 기존 `CombatCameraZoomPose.ValidateCamera`의 조건을 적용한다. ImpulseListener와 VolumeSettings의 기존 허용은 유지한다.

기준 채널은 대표 운영 카메라의 현재 채널에서 옮기고 활성 Listener를 필수로 검사한다. 후처리 Profile·클립·Culling/Layer 등 시각 설정은 대표 카메라에서 필요한 항목을 명시적으로 이관한다. 카메라를 `new` 기본값으로만 대체하지 않는다. 실제 렌즈 쓰기는 계속 `ICameraEffectService.Dispatch`만 사용한다.

이미 발행한 Impulse는 현재 공개 API로 개별 취소할 수 없다. 표시 해제는 새 요청과 줌 대여를 막고, 기존 흔들림 신호는 원래 Duration까지 자연 종료한다. 효과 시스템의 씬 재바인딩/Shutdown은 해당 시스템이 발행한 신호를 정리하는 기존 동작을 유지한다. 이 전환에서 전역 Impulse Clear나 Render 내부 접근을 추가하지 않는다. 같은 씬에서 즉시 표시/특성 확인을 다시 시작하면 남은 신호가 잠깐 보일 수 있는 기존 계약도 유지한다.

### SC5. 조립과 연결 수명은 카메라 단위로 묶음

`ViewFactory/CombatCameraFactory`가 공용 바인딩·필요한 Render 서비스로 다음을 조립한다.

- snapshot 하나와 설정 값.
- 기존 `CombatCameraWindow`, `CombatCameraView`.
- 카메라 선택·복원 책임.
- 이들을 소유하는 내부 C# `CombatCameraSession`.

Session은 Factory가 만든 협력 객체를 받고 Window·Control을 좁게 제공하며, 연결/복원/해제 순서만 소유한다. 조립 자체·입력·사건 해석을 구현하지 않는다. 전역 서비스나 System으로 등록하지 않는다.

`CombatSceneDisplayFactory`와 `CombatTraitPreviewFactory`는 같은 카메라 Factory를 사용한다. Display는 카메라 Session을 한 번 소유한다. View·Window를 다른 해제 경로에서 다시 정리하지 않는다. Factory의 나머지 조립이 실패해도 Session을 반환한다. Begin은 기존 실패 정리 범위 안에서 호출한다.

표시 연결 동안 카메라 선택은 공개 `ICameraCatalogService.GetHandle(binding의 실제 이름)` → `RaisePriority()`로 한다. 원래 Priority만 이 책임이 기억하고 해제 때 복원한다. 카메라 이름 문자열을 여러 파일에 복사하지 않는다.

핸들은 조작 시 잠깐 빌리고 바로 Dispose한다. Enter의 BindCameras 이후와 AfterLoad의 재수집 사이를 가로질러 보관하지 않는다. 원래 Priority 복구도 정상 Exit/Shutdown의 카탈로그 해제 전에 수행한다. 이미 파괴된 씬 대상에는 새 핸들을 요청하지 않는다. 정상 상태에서 이름 조회가 실패하면 조용히 무시하지 않는다.

서비스 공급은 `CombatDisplayServices.From(global)`에 `ICameraCatalogService`를 추가하는 기존 Global → Render → CameraGroup 경로를 쓴다. Factory에는 필요한 인터페이스만 전달한다. 루트 Builder 재등록이나 Root resolver 직접 조회는 추가하지 않는다.

### SC6. 맵의 옛 카메라는 생성된 인스턴스에서만 간섭 차단

원본 맵 에셋은 보존한다. `StageViewPropPresenter.Show`가 만든 **소유 배경 인스턴스**에만 `CombatBackdropCameras` 도우미를 적용한다.

1. 배경 생성 직후, `BindCameras` 전에 가상 카메라들을 한 번 찾는다.
2. 해당 인스턴스의 가상 카메라와 카메라 Listener를 비활성화한다. 공용 카메라는 배경 밖이므로 대상이 아니다.
3. 비활성 카메라도 카탈로그 수집 대상이므로, 각 카메라 GameObject 이름을 배경 인스턴스 ID와 수집 번호를 포함하는 고유 런타임 이름으로 바꾼다. 원래 이름은 진단용 접미사로 남길 수 있다. 형식 정의는 도우미 한 곳이다.
4. 배경에서 카메라 snapshot·조작 설정을 읽거나 보관하지 않는다.

GameObject 전체를 끄거나 삭제하지 않는다. 같은 객체의 배경·라이트·셰이더·자식 표시까지 끄지 않기 위해 컴포넌트만 끈다. 컴포넌트는 다음 프레임의 Destroy 완료를 기다리는 방식으로 제거하지 않는다. 원본 프리팹·MapDefinitionSO·에디터 미리보기의 카메라에는 손대지 않는다.

단순 Priority 내리기는 채택하지 않는다. 비활성 수집과 이름 충돌도 처리해야 하며, 옛 카메라가 다시 선택될 여지를 남기지 않아야 한다. 카탈로그의 수집 규칙은 변경하지 않는다.

### SC7. 아침 전환의 Day 이름 의존 제거

- `RequireMapCamera`와 호출을 제거한다.
- `CombatPresentationSettingsDataSO` 안 설정 타입의 `morningTransitionCamera` 필드·프로퍼티·이름 검증을 제거하고, 운영 설정 에셋의 해당 직렬화 값도 정리한다.
- `CombatPhaseTransitionSettings.From`은 대상 이름을 호출자로부터 명시적으로 받는다. 운영·특성 확인 Factory 모두 공용 바인딩의 실제 카메라 이름을 전달한다. 레이어·색·속도·양은 기존 View DB에 남긴다.
- `CombatPhaseTransitionPresenter.TryBegin`의 자동 전환 비활성 상태는 그대로다. 이 작업이 전환 재활성화나 낮/밤 렌더 정책 변경을 뜻하지 않는다.
- JMHTest의 Day/Night 전환 실험과 Render 초기 레이어 설정은 별도 기능이므로 수정하지 않는다.

### SC8. 특성 확인 환경도 같은 공용 시점 사용

- `Assets/Scenes/Dev/TraitActivationPreview.unity`에도 같은 공용 카메라 프리팹 하나를 두고 Host에 바인딩한다. 해당 환경의 기존 출력 Camera·Brain은 유지한다.
- Host는 배경의 `MapCameras` 대신 명시된 공용 바인딩을 Factory에 전달한다. `HasMapCamera` 표시도 공용 카메라의 준비 상태를 뜻하도록 이름·호출자를 함께 맞춘다.
- 배경은 운영과 같은 간섭 차단을 거친 뒤 런타임이 BindScene한다.
- 반복 Play·Stop·시나리오 교체에서 이전 Session이 먼저 완전히 해제돼, 줌 대여·추가 흔들림 요청·Priority·자세 변경이 다음 실행으로 새지 않게 한다. 이미 발행한 흔들림의 자연 종료는 SC4를 따른다.
- 특성 합성·판정·시나리오·게임 진입 정책은 바꾸지 않는다.

### SC9. 스테이지 편집 데이터의 폐기 설정 정리

- `CombatStageSoAssets`의 카메라 SO 목록·SerializedProperty 읽기/쓰기, `CombatStageSoDocument`의 `cameraGuid`, `CombatLayoutWindow.SoMode`의 스테이지별 카메라 선택 UI를 함께 제거한다.
- 편집 화면은 필요하면 공용 카메라 설정 위치를 설명하는 정적 안내만 둔다. 새 화면·별도 설정 저장소를 만들지 않는다.
- 옛 작업본에 직렬화된 `cameraGuid`가 있어도 다른 필드는 그대로 읽고 저장한다. 카메라 선택값만 더 이상 적용하지 않는다. 옛 작업본을 불러왔다고 폐기 필드를 에셋에 다시 쓰지 않는다.
- 기존 StageViewProp 에셋의 옛 `cameraSettings` YAML은 소비자 제거 후 무시되는 이관 잔여값이다. 사용자 작업 중인 에셋을 포함한 일괄 재직렬화는 하지 않는다. 현재 유효한 연결은 공용 프리팹의 SO 참조 하나다.

### SC10. 초기 구도와 맵별 차이

기능 유지와 모든 맵의 과거 첫 화면이 완전히 같다는 보장은 구분한다. 과거 카메라는 맵 배치 회전·스케일·타일 정렬을 상속했고, 새 카메라는 필드 기준 공용 시점이다.

- 대표 운영 일반 스테이지의 **배경 배치 완료 뒤 구도**를 필드 기준으로 환산해 공용 프리팹 초기 자세의 출처로 삼는다. 원본 맵의 로컬 Transform을 그대로 복사하지 않는다.
- 기존 SO 값은 그대로 유지한다. 카메라/부모 스케일은 1로 하고 공용 시점은 배경 변환을 상속하지 않는다.
- 다른 일반 맵·보스 맵·타일 맵에서 보드 가시성, 화면 방향, Pan 축, 줌 끝점·클립을 Unity에서 확인한다. 공용 초기 구도 조정은 이 작업의 시각 확인 범위다.
- 맵별 override나 자동 보드 맞춤이 필요해지면 새 기능이므로 구현하지 말고 먼저 결정받는다.

## 4. 수명 순서

순서 값의 원본은 `Assets/ProjectL/Script/Systems/RuntimeSystem/ErrorHandling/SceneNodeOrder.cs`다. 새 순서 상수·Node는 추가하지 않는다.

| 시점 | 카메라와 배경 |
|---|---|
| 씬 로드 | 공용 가상 카메라·바인딩이 씬에 이미 있음. 기존 출력은 유지됨 |
| Enter CombatBackdrop(-195) | 기존 루트 찾기·배경 배치. 옛 배경 카메라 간섭 차단. 공용 바인딩의 구조 조건 확인 |
| Enter BindCameras(-190) | 기존 카탈로그·효과 바인딩이 공용 카메라 수집. 옛 카메라는 비활성·고유 이름 |
| Enter CombatViewAttach(100) | 같은 루트의 공용 바인딩에서 snapshot·설정 생성. Session 조립·선택·Begin. 입력·사건 표시가 같은 창구 사용 |
| AfterLoad 재수집(10) | 카탈로그 내부 핸들은 다시 수집되지만 View는 핸들을 보관하지 않음 |
| 표시 Detach | 입력 정리·진행 중 연출 취소 후 Session 복원·반환. 배경과 공용 카메라 실체는 씬에 남음 |
| 같은 씬에서 표시 재연결 | 원래 자세·렌즈·Priority가 복원된 동일 카메라를 새 Session으로 연결 |
| Exit CombatViewRelease(-100) / Shutdown | 기존 ReleaseScene의 표시 해제 후 배경 해제. 카메라 참조를 놓고 씬 언로드가 실체를 파괴 |
| 조립 실패 | 소유권 인수 전에는 Factory, 인수 후에는 Display 해제 경로가 Session 반환. 원래 예외와 정리 예외를 기존 CleanupErrors 정책으로 보존 |

카메라 어댑터는 표시 수명에서 Unbind하고 씬 수명에서 파괴된다. Session 해제 뒤 Delay/연출 콜백이 다시 카메라를 쓰지 않도록 기존 Window의 취소·대여 무효화 계약을 유지한다. 씬 언로드 후 카탈로그 이름 조회로 복구하려고 하지 않는다.

## 5. 구조·원칙 검토

기준: [전투 View 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), 특히 §4.6, JMH Framework 개발 지침, Framework 개발 가이드 FW 계약.

### 5.1 SSOT와 SOLID

| 기준 | 적용 |
|---|---|
| SSOT | 카메라 실체·기준 구도는 씬/공용 프리팹, 조작 authoring 값은 기존 SO 한 곳, 연결 전 snapshot은 Session 한 벌, 팬·목표 크기는 View, 보기 크기·연출 대여·겹침은 Window, 실제 렌즈 반영은 Render 효과. 맵의 폐기 직렬화 값은 읽지 않음 |
| S | Binding은 참조·검사, Factory는 조립, Session은 연결 수명, View는 조작, Window는 효과 창구, backdrop helper는 소유 배경 인스턴스의 옛 카메라 차단 |
| O | 기존 Input 계약·카메라 효과 API·줌 자세 확장을 사용. 카메라 공급원 변경으로 사건별 분기나 Render 내부를 수정하지 않음 |
| L | 입력 허용/거절·연출 줌 대여·취소·복원 보장 유지. 공용 카메라 누락을 정상 무조작으로 숨기지 않음 |
| I | 입력에는 기존 ICombatCameraControl만, 카메라 Factory에는 Catalog/Effects와 바인딩 값만. UI·전투 상태 전체를 카메라 객체에 넘기지 않음 |
| D | 효과·선택은 기존 서비스 인터페이스로 수행. 씬 참조는 명시 연결, 값/자세 계산은 기존 계산기에 유지. 내부 카탈로그 Node·Pool에 접근하지 않음 |

### 5.2 JMH/FW와 서비스 계층

- 생성: 기존 `CameraGroupFactory`가 Render 카탈로그·효과 시스템을 생성한다. 이번 작업은 그 Factory를 수정하지 않는다.
- 내부 등록: 기존 CameraGroup Builder의 각 인터페이스 등록·모듈 등록을 유지한다.
- 상위 Export: 기존 CameraGroup → Render Core → Render → Global Context 계층을 유지한다. 루트에 하위 서비스를 재등록하지 않는다.
- 소비: `CombatDisplayServices.From(global)` → `global.RenderServiceCommand.CoreGroup.CameraGroup` → 필요한 Catalog/Effects → 카메라 Factory·협력 객체다.
- FW-001~003: 새 전역 서비스·Builder 없음. 내부 Binding·Session을 서비스로 등록하지 않는다.
- FW-004·011: 카탈로그는 공개 Handle, 렌즈·흔들림은 공개 Dispatch로만 사용하고 대여는 반환한다. 소유 배경 인스턴스의 초기 구성은 카탈로그 바인딩 전에 수행한다.
- FW-005~007: 입력 Configuration·Submit·Layer 경로를 유지한다.
- FW-008~010·012~015: UI 등록·Mesh·DB Render DTO·이벤트·Pool·Queue 계약 변경 없음. 카메라를 위해 새 UI를 만들지 않는다.
- FW-016: 기존 BindScene/서비스 바인딩/씬 Node 순서를 유지하고 새 조작은 카메라 수집 뒤에 시작한다.

### 5.3 기능 변경의 독립성

| 변경 시나리오 | 현재 | 변경 뒤 | 남는 확인 |
|---|---|---|---|
| 공용 카메라 구도·Listener 조정 | 맵 프리팹 내부 카메라와 배경 배치 변환을 이해해야 함 | 공용 프리팹·SO·카메라 안내에서 조정 | 필드 투영·줌/흔들림 시각 확인 |
| 배경 프리팹·타일 배치 변경 | 카메라 존재·이름·렌즈·채널까지 확인 | 배경·타일 기능에서 변경 | 배경 간섭 차단·공용 시점의 가시성 확인 |
| 사건 줌/흔들림 정책 변경 | Window와 맵 카메라 공급 경로를 함께 추적 | Window 및 해당 연출 소비자 중심 | Session 취소·복원이라는 공통 경계 |
| 카메라 의존성 추가 | 대형 DisplayFactory의 조립과 기능을 함께 읽음 | 카메라 Factory·Session에서 연결 | 운영/특성 확인의 동일 조립 경로 |

최초 전환은 DB·편집·특성 확인까지 영향을 받으므로 통합 확인을 줄였다고 주장하지 않는다. 이후 배경 변경에 카메라 설정 수정이 관성적으로 따라붙지 않는 것이 개선점이다.

## 6. 완료 기준과 검증 제한

1. 맵에 Day/Night가 없어도 Combat 진입·이동·줌·사건 흔들림이 동작한다.
2. 옛 카메라가 있는 맵에서도 공용 카메라만 선택되고 비활성 카메라 이름 중복으로 진입이 멈추지 않는다.
3. `UseZoomPose` 두 모드, 사용자 줌과 사건 줌·흔들림 동시 실행, 입력 차단·복원 정책을 유지한다.
4. HUD·칸/유닛 클릭·고스트가 실제 출력과 일치한다.
5. 중도 취소·Detach/재연결·다음 Combat 재진입에서 구도·크기·Priority·대여가 누적되지 않는다.
6. 특성 확인 반복 실행과 스테이지 편집 읽기·저장이 폐기 카메라 필드를 요구하지 않는다.
7. 원본 맵·다른 작업자 코드·사용자 미커밋 변경을 보존한다.

View Test 코드는 새로 작성하지 않는다. MSBuild는 상시 제한이지만, 2026-11-08 사용자가 "Phase 진행하고, 마지막에는 MSBuild도 진행해"라고 명시해 이 카메라 작업의 Phase 마무리에 수행한다. 기존 시험은 소유자와 영향 확인 후 AGENTS.md에서 허용한 호출/컴파일 정합성 범위만 다룬다. 다른 작성자의 실행 실패 시험은 먼저 묻는다. Unity 컴파일·재생·시각 확인은 사용자가 수행한다. Phase B의 DB 설정 에셋 변경에 따라 `DataBaseAssetTests`를 사용자 확인 항목에 포함한다.

**현재 검증 상태: Phase A·B 구현·정적 검토 완료. 런타임 및 두 에디터 프로젝트 MSBuild 성공. 조립 실패 정리 범위를 보완한 최종 소스도 MSBuild 성공(exit 0, 오류 0, 수정하지 않은 UI/에디터 의존성 경고 15). 공용 프리팹·두 씬·설정/편집 소비자를 전환했으며 기존 시험 코드는 미변경이다. Unity 컴파일 미확인, 재생 미확인.** 상세 증거는 Task 계획 §8을 따른다.


## 7. 2026-11-09 Phase C 문서 인계 상태

사용 안내·씬 연결 안내·이전 설계의 대체 범위·공유 메모를 현재 구현에 맞췄다. 공용 카메라 설정과 실제 에셋 이름은 [사용 안내](../viewDocs/combat-camera-zoom-pose-guide.md), 순서가 있는 Unity 확인표는 [Task 계획 §6](../plans/2026-11-08-combat-scene-camera-plan.md#6-사용자-unity-확인표)을 따른다. 현재 Binding이 읽는 실제 가상 카메라 이름은 `CombatCamera`이며 SC1의 최초 제안 이름 문자열을 필수 계약으로 삼지 않는다.

코드·씬·에셋은 이번에 수정하지 않았다. 사용자 Unity 결과가 없어 C1의 기능 확인·구도 최종 조정은 남아 있다. Phase B의 MSBuild 성공 기록과 Unity 확인은 구분하며, **Unity 컴파일 미확인·재생 미확인**을 유지한다.


### 2026-11-09 직접 검증 후속

사용자 명시 요청으로 MSBuild를 재실행해 오류 0으로 통과했고, Unity에서 Combat 씬의 비어 있던 sceneCamera를 기존 공용 인스턴스에 재연결했다. Unity Play 진입·종료를 2회 수행해 새 예외 없이 전투 표시 다음 튜토리얼 연결 단계까지 도달했다. 화면 캡처 도구가 실패해 이동·줌·흔들림·실제 구도 등 시각 확인은 미완료다. 전체 기능 검증 완료가 아니라 이 실행 범위만 확인한 것으로 기록한다. 정확한 증거·한계는 Task 계획의 마지막 실행 기록을 따른다.
