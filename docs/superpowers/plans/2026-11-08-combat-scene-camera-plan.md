# Combat 씬 공용 카메라 Task 계획

- 작성: 2026-11-08. 갱신: 2026-11-09, 전투 View 작업.
- 설계 원본: [combat-scene-camera-design](../specs/2026-11-08-combat-scene-camera-design.md).
- 상태: **Phase A·B 구현 완료, Phase C 문서·인계 완료. C1 직접 확인으로 Combat 씬 참조 복구·Play 진입/종료 확인. 시각·전체 회귀 확인 대기. 후속 Phase D0 원점 기준 루트 및 D0.1 라이브 자세 정적 구현 완료·Unity 확인 대기. D1~D3 코드 완료, Tool.Editor·Systems MSBuild 통과. Unity에서 Stage 2201의 16:9 전투 구도 렌더와 저장된 Combat 카메라 읽기 확인; 보정 입력·Undo·배치 저장/재열기·Play 비교 확인 대기.**
- 2026-11-09 후속 방향: 사용자가 씬 직접 편집 방식을 선택했다. 후속 Task와 S1 결과는 [전투 배경 씬 작업 계획](2026-11-09-combat-map-authoring-scene-plan.md)에 기록한다. D1~D3는 구현 이력으로 유지한다.
- 2026-11-08 후속 지시: "Phase 진행하고, 마지막에는 MSBuild도 진행해". 2026-11-09 D3 후에도 사용자가 MSBuild와 PC 사용 확인을 명시 요청했다. 이 카메라 작업의 Phase 마무리 MSBuild는 명시 요청에 따라 허용한다. Test 코드 작성 금지는 유지한다.
- 진행 규칙: 설계 → Task 계획 → Phase 단위 구현. 각 Phase 뒤 멈추고 보고한다. 다음 Phase를 자동 시작하지 않는다. 계획 밖 결정은 먼저 묻는다. 커밋·push하지 않는다.

## 2026-11-09 목적 재확인: Play 없는 배경 배치

공용 카메라 전환의 원래 작업 목적은 `StageViewProp` 배경 주변의 프랍을 배치할 때마다 Play를 반복하는 비용을 없애는 것이다. Phase A~C의 전투 카메라 공급원 전환은 끝났지만, **프랍을 직접 편집하는 맵 작업실 안에서** 실제 공용 시점을 계속 보는 흐름은 아직 없다. 현재 SO 편집 창의 3D 미리보기는 별도 카메라를 쓰고, 그 창으로 이동하면 프랍 배치와 구도 확인을 한 화면에서 할 수 없다. 이를 후속 Phase D로 분리하고 기존 A~C 완료 기록은 변경하지 않는다.

후속 Task의 공통 순서는 다음과 같다.

1. `FbxPlacementWindow`에서 현재 열린 맵 프리팹에 맞는 운영 `ViewStageDataSO`를 미리보기 맥락으로 고르는 UI와 불일치 안내를 둔다. 에셋 이름은 `StageViewProp_*`여도 스크립트와 운영 표 원본은 `ViewStageDataSO`다. 선택을 바꾸려고 `CombatLayoutWindow`를 열 필요가 없어야 한다. 같은 맵을 쓰는 여러 스테이지 중 하나를 자동으로 단정하지 않는다.
2. Combat 씬 인스턴스의 `CombatSceneCameraBinding`에서 실제 기준 자세·직교 렌즈를 읽는 단일 출처를 정한다. 프리팹과 씬 override가 달라질 수 있으므로 프리팹 값만 실제 시점으로 간주하지 않는다. 미리보기 화면의 종횡비를 명시한다.
3. 작업실에서 **프랍을 선택·드래그하는 동안 계속 보이는** 공용 시점 프리뷰를 구성한다. 현재 카메라 미리보기의 “맵 카메라 선택 중에만 표시” 조건을 사용하지 않는다. 작업실의 편집 맵에 런타임 변환을 직접 적용해 저장하지 않는다.
4. 선택 스테이지의 배경 보정·타일 정렬·필드 간격을 미리보기에 재현한다. 런타임과 편집 도구의 배치 계산이 따로 복제되지 않게 공통 계산 경계를 점검한다. 타일 방식에서는 위치 보정 X/Z가 정렬로 상쇄된다는 점을 표시한다.
5. 프랍 변경 → 같은 작업실의 공용 시점에서 다시 보기 → 맵 저장 → 다시 열어 동일 배치 확인을 수락 기준으로 삼는다. 갱신 비용, PreviewScene/렌더 자원 해제, Undo·맵 교체·Play 진입·스크립트 재로딩을 설계한다.
6. Unity 편집 화면과 Play 화면의 구도·배경 위치를 비교한다. 에디터 참고 조명·후처리 차이는 따로 기록한다. View Test 코드는 추가하지 않는다. 이번 D3의 MSBuild는 사용자의 별도 명시 요청으로 수행하고 Unity 컴파일·Play 확인 범위는 결과 기록에 구분한다.

Phase D의 구현을 시작하기 전에 미리보기의 배치 방식(편집 맵에서 읽기 전용 투영 또는 별도 임시 표시), 갱신 비용, 정확한 파일 경계를 확정한다. 특히 작업실의 원본 맵을 변경한 상태로 저장하지 않는 조건이 우선이다. Core·FieldCamera·Architecture 파일은 범위 밖이다.

### D0 — 원점 기준 루트(선행 작은 Phase)

- [x] 사용자 선택대로 `CombatCamera.prefab`에 변환이 항등인 `CombatCameraOrigin`을 추가하고 기존 가상 카메라·Binding을 자식으로 둔다. 기존 카메라 자식의 로컬 위치·회전·렌즈·설정과 객체 fileID를 유지한다.
- [x] Combat·TraitActivationPreview 씬의 프리팹 인스턴스 및 `sceneCamera` Binding 참조를 정적으로 확인한다. 씬의 다른 작업자/Unity 변경은 보존한다.
- [x] 기준 루트가 X/Z 0, 가상 카메라 월드 자세가 변경 전과 같음을 YAML 관계로 확인한다. Unity 컴파일·화면 확인은 사용자에게 인계한다.

D0 후 멈춰 결과를 보고한다. D1~D3 미리보기 구현은 다음 리뷰 지점에서 이어간다.

**D0 실행 기록(2026-11-09):** 공용 프리팹에 `CombatCameraOrigin`(위치 0·회전 0·배율 1)을 부모로 추가하고, 기존 `CombatCamera`의 transform fileID `400100`과 Binding fileID `114103`을 유지했다. 카메라 자식의 로컬 위치 `(14.3025, 19.95, -13.2525)`, 회전, 렌즈, 설정은 변경하지 않았다. 두 씬 모두 같은 프리팹 GUID와 Binding `114103`을 가리키며 새 부모의 씬 override가 없다. Combat 씬에는 이번 작업 전에 이미 다른 변경이 있었으므로 직접 편집하지 않았다. `git diff --check`는 이 Phase의 프리팹·문서에서 통과했다. **Unity 컴파일 미확인, Play 구도 미확인.**

**D0 후 사용자 확인으로 드러난 제약:** Play 중 하이어라키에서 기준 루트나 카메라 Transform을 움직여도 화면은 바뀌지 않았다. `Use Zoom Pose`는 연결 때 snapshot 월드 자세로 경로를 만들고 Cinemachine 확장이 RawPosition·RawOrientation을 매 프레임 덮었다. 사용자가 자세 줌을 유지하며 라이브 조정도 원해 D0.1로 별도 반영한다. D1~D3의 수락 기준인 Play 없는 작업실 미리보기는 그대로다.

### D0.1 — 줌 자세 유지 + 라이브 Transform 반영

- [x] 기존 `CombatCameraZoomPosePath`의 시작 기준 자세를 보관하고, 기존 `Evaluate` 결과를 현재 가상 카메라 Transform 기준으로 변환하는 읽기 전용 평가 경로를 추가한다. 기존 생성자·`Evaluate` 호출은 유지해 기존 시험 호출을 깨지 않는다.
- [x] `CombatCameraZoomPose`가 Cinemachine에 자세를 쓰기 직전 현재 Transform을 읽는다. 별도 mutable 기준 자세를 확장 컴포넌트에 저장하지 않고, 기존 팬·렌즈 줌·Impulse correction·복원 정책을 유지한다.
- [x] 줌아웃/줌인 양 끝, 중간 크기, 팬 중에 루트 또는 가상 카메라를 움직였을 때 같은 강체 이동·회전이 출력에 반영되는지 수식·호출 경계를 정적으로 점검한다. View Test 코드는 추가하지 않고 MSBuild도 실행하지 않는다.

D0.1은 여기서 멈춰 보고한다. 다음 D1~D3 맵 작업실 미리보기는 별도 리뷰 지점이다. Unity에서 Play 중 루트·카메라 이동과 줌·팬·흔들림·표시 해제 복원은 사용자가 확인한다.

**D0.1 실행 기록(2026-11-09):** `CombatCameraZoomPosePath`에 최초 기준 자세를 보관하고 `EvaluateAtReference`로 기존 `Evaluate` 결과를 현재 Transform의 강체 이동·회전만큼 변환했다. 처음과 현재 기준이 같으면 기존 결과와 같고, 현재 기준이 이동/회전하면 줌 양 끝·중간·팬 오프셋도 같은 변환을 받는다. `CombatCameraZoomPose`는 두 Cinemachine callback에서 같은 평가 경로를 쓴다. 패키지의 `CinemachineVirtualCamera.InternalUpdateCameraState`는 Follow/LookAt이 없을 때 Transform에 Raw 자세를 되쓰지 않으며, 기존 `ValidateCamera`가 두 참조를 금지한다. 기존 `CombatCameraZoomPosePath` 생성자·`Evaluate` 시험 호출과 `CombatCameraView`의 복원 계약은 유지했다. `git diff --check` 통과. **컴파일 미확인, Unity Play 동작 미확인.**

### 후속 Phase D의 작은 리뷰 지점

| Phase | 구현·확인 범위 | 끝나면 확인할 것 |
|---|---|---|
| D1 — 작업 맥락 | 맵 작업실 안에서 운영 `ViewStageDataSO` 선택·현재 맵과의 참조 일치·필드/보드 맥락·Combat 씬 카메라 기준값을 읽기 전용으로 확인한다. 임의의 스테이지를 자동 선택하지 않는다. 미리보기 배치 방식과 변경 감지 범위를 설계에 확정한다. | 맵을 편집하거나 저장하지 않은 채 원본·불일치·누락 메시지를 검토하고 멈춘다. |
| D2 — 상시 구도 | 선택한 스테이지의 런타임 배경 변환을 적용한 공용 시점 프리뷰를 작업실에 둔다. 프랍 선택·이동·Undo 중 계속 보이고, 임시 객체가 맵 저장본에 들어가지 않게 한다. | PrefabWithTiles의 타일 기준·프랍 위치와 카메라 프레임을 Unity에서 비교하도록 인계하고 멈춘다. |
| D3 — 수명·사용 흐름 | 맵 교체·창 닫기·Play 진입·재로딩 해제, 변경 시 갱신 비용, 종횡비·참고 렌더 안내와 사용법을 마무리한다. | 작업실에서 배치→확인→저장→다시 열기를 사용자에게 인계하고 멈춘다. |

D1 전에 런타임과 편집 도구가 공유할 배치 계산의 호출 가능 범위와 PreviewScene 렌더 방법을 정적 조사한다. D1에서 정확한 방식이 결정되지 않으면 D2를 진행하지 않고 설계·계획을 먼저 고친다. 각 Phase 뒤에는 다음 Phase를 자동으로 시작하지 않는다.

**D1 실행 기록(2026-11-09):** `ViewDataBaseFactory`가 운영 배경을 `ViewStageTable`에서 공급하는 것을 확인하고 사용자 선택에 따라 계획의 독립 `StageViewPropDataSO`를 운영 `ViewStageDataSO`로 고쳤다. `FbxPlacementWindow` 파일 영역에 운영 ViewStage 선택과 단계별 원본·불일치 안내를 넣었다. `CombatStageSoAssets`의 읽기 경계가 ViewStage/StageRoot/StageFieldView 표 등록, 현재 열린 프리팹의 GUID·local fileID, 전투 필드 크기·공용 필드 간격을 확인한다. `FbxCombatCameraSource`는 저장된 Combat 씬을 버튼 요청 때만 PreviewScene으로 열고 `CombatSceneRoot.sceneCamera` Binding, 가상 카메라의 Transform·직교 렌즈와 Zoom Pose 설정을 읽은 뒤 즉시 닫는다. 저장된 씬 렌즈 크기와 Zoom Pose 사용 시 시작 크기를 구분해 표시한다. 맵 작업실의 저장 경로·씬 변경·카메라 조작·렌더 경로는 수정하지 않았다. D2는 별도 PreviewScene 표시용 복사본과 변경 종류별 갱신, Tool.Runtime의 공통 타일 맞춤 계산으로 설계했다. **컴파일 미확인, Unity 편집 화면 확인 대기.**

**D2 실행 기록(2026-11-09):** `FbxPlacementWindow` 오른쪽 위에 선택 프랍과 무관하게 유지되는 공용 구도 패널을 연결했다. `FbxCombatStagePreview`는 별도 PreviewScene에 현재 작업실 맵을 복사하고 `ViewStageDataSO` 보정·타일을 적용한다. 저장된 Combat 씬 카메라의 시작 자세·직교 크기로 렌더한다. 같은 계층의 프랍 이동·Undo는 10 Hz Transform 동기화, 계층/맵/스테이지 변경은 복사본 재생성으로 반영한다. 배경 타일 회전·기준 타일 이동 계산은 `ProjectL.Tool.Runtime/CombatTileAlignmentMath`로 공유해 런타임 `CombatMapTileAlignment`와 기존 SO 편집 미리보기가 함께 쓴다. 복사본과 RenderTexture는 창 비활성·맵 교체 때 해제한다. 무작위 타일 외형은 편집 참고용 고정 시드이며 조명·베이크·후처리 일치는 D3 확인 범위다. **컴파일 미확인, Unity에서 배경/타일 위치·드래그/Undo·저장본 불변 확인 대기.**

**D3 실행 기록(2026-11-09):** 선택 ViewStage의 배경 보정·타일 기준값을 맵 작업실에서 Undo 가능한 SO 편집으로 노출했다. 전용 SO Undo가 맵을 저장 안 된 것으로 잘못 표시하지 않도록 이벤트 이름을 분기한다. 고정 16:9 렌더와 Game View 비율·참고 조명/타일 안내를 표시한다. 10 Hz Transform 검사는 유지하되 화면 변화·카메라 갱신·프레임 크기 변화 때만 렌더한다. 관련 에셋 종속성 변경을 감지해 Stage/카메라 원본을 다시 읽고, 창 비활성·맵 교체·Play 진입·창 파괴에서 PreviewScene과 RenderTexture를 해제한다. 외부 머티리얼/컴포넌트의 저장 전 변경은 '스테이지·미리보기 다시 읽기'로 반영한다. 사용 흐름: 맵 불러오기 → 운영 ViewStage 선택 → 오른쪽 위 16:9 구도에서 프랍 배치·보정 → 맵 SO 저장 및 프로젝트 저장 → 다시 열어 같은 배치 확인.

**D3 빌드·Unity 직접 확인(2026-11-09):** 생성된 csproj의 `ProjectL.Tool.Editor.csproj`와 `ProjectL.Systems.csproj`를 MSBuild Debug로 각각 빌드해 오류 0을 확인했다. Tool.Editor 빌드 출력에는 경고가 없었고 Systems 출력에는 기존 Core/UI 경고만 있었다. 로그는 `Temp/Codex/CombatSceneCameraD3-20261109/`에 있다. Unity 6000.0.73f1에서 `ProjectL > FBX 배치`를 열고 `StageViewProp_Chapter2_Regular`(Stage 2201)와 그 참조 프리팹 `StageTest`를 연결했다. 저장된 Combat 씬의 `CombatCamera` 월드 자세·직교 크기·Zoom Pose 시작 크기를 읽었고, 같은 창 오른쪽 위 406×228(16:9) 구도에 배경과 타일이 렌더되는 것을 확인했다. 작업 창을 닫아 패널이 사라지는 것까지 확인했고 Editor.log 마지막 구간에서 관련 예외는 없었다. 프리팹/SO 저장을 실행하지 않았다. 원격 입력이 별도 Unity 창의 숫자 필드에 전달되지 않아 오프셋 편집·Undo와 프랍 드래그·저장/재열기, Play 구도 일치 및 Play 진입 해제는 직접 검증하지 못했다. **MSBuild 컴파일 확인; Unity 전체 컴파일·Play 재생 미확인.**

## 1. 구현 전제와 작업 경계

목표는 Combat 씬의 공용 CinemachineVirtualCamera 하나와 기존 Boot 출력 Camera/Brain의 조합이다. 이동·줌·흔들림 알고리즘과 입력 정책은 유지한다. 공용 SO 연결·맵 카메라 간섭 차단·특성 확인·폐기 설정을 쓰는 편집 도구까지 전환해야 완료다.

경로 약어:

- `View`: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem`.
- `Camera`: `View/Combat/Presentation/Cameras`.
- `ViewFactory`: `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View`.
- `ViewData`: `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View`.
- `Editor`: `Assets/ProjectL/Script/Tool/Editor`.

이번 계획 작성 때 이미 수정돼 있던 파일은 다음과 같다. 구현 시작 때 `git status`를 다시 확인하고 아래 작업과 섞지 않는다.

- `Assets/Audio/FMODplus/New Fmod Event Catalog.asset`
- `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset`
- `Assets/ProjectL/DataBaseSO/ViewGroup/StageViewProp/Item/Test.asset`
- `Assets/ProjectL/Resources/Font/SCDream7 SDF.asset`
- `Assets/ProjectL/Resources/Font/Unchained-Halftone SDF.asset`
- `Assets/ProjectL/Sprites/UnitInfo/demo.png`
- `Assets/Scenes/JMHTest/GrayBoxingProp/Map3GrayBoxP.prefab`
- 미추적 `Assets/ProjectL/DataBaseSO/ViewGroup/StageViewProp/Item/Test2.asset` 및 `.meta`

이 파일들을 정리·되돌리기·일괄 재직렬화하지 않는다. 원본 맵의 카메라는 삭제하지 않는다. 런타임 인스턴스에서만 간섭을 차단한다.

## 2. Phase 구성

| Phase | 목표 | 완료 후 상태·리뷰 지점 |
|---|---|---|
| A — 연결 기반 | 명시 카메라 바인딩·snapshot·공용 조립·수명 기반을 추가 | 운영 경로는 기존대로 유지. 새 기반 diff와 서비스 계층·실패 복구 검토 후 멈춤 |
| B — 일괄 전환 | 운영·씬·특성 확인·DB/편집의 카메라 연결을 함께 전환 | 옛 카메라 의존이 제거된 구성. Unity 확인 인계 후 멈춤 |
| C — 확인·문서 인계 | 사용자 Unity 결과에 따른 범위 내 수정, 사용 안내·공유 메모 | 확인 증거와 남은 미확인 사항을 구분해 보고 후 멈춤 |

Phase A에서는 기존 소비자가 쓰는 타입·경로를 먼저 없애지 않는다. 기반이 준비된 뒤 Phase B에서 호출자·씬·폐기 필드를 함께 전환한다. Phase B 도중의 누락 상태를 완료로 보고하지 않는다.

## 3. Phase A — 연결 기반

### A1. 전환 대상·기준값 조사 확정

- [x] 현재 diff·추가 AGENTS 지시를 다시 확인한다.
- [x] `CombatMapCamera`, `CombatMapCameras`, `MapCameras`, `CameraSettings`, `AttachMap/DetachMap`, `morningTransitionCamera`, `HasMapCamera`, `cameraGuid`의 전체 호출자를 정리한다. 기존 시험 호출자도 읽기 전용으로 포함한다.
- [x] StageViewProp의 설정 참조가 계속 같은 SO인지 확인한다. 다른 SO가 발견되면 값 통합은 먼저 묻는다.
- [x] 대표 운영 일반 스테이지의 맵/타일 배치 완료 구도, 렌즈·투영·클립·VolumeSettings·Impulse 채널을 공용 카메라의 이관 출처로 기록한다. 실제 구도 확인은 Unity 사용자 확인으로 남긴다.
- [x] 기존 시험의 작성자·허용된 수정 범위를 확인한다. 새 Test·새 asmdef는 추가하지 않는다. MSBuild는 위 명시 요청 범위에서 수행한다.

산출물: 이 계획 §8에 조사 결과와 정확한 변경 파일 목록 기록. 이미 달라진 동작이나 새 요구가 나오면 설계와 대조해 먼저 결정받는다.

### A2. 명시 바인딩과 snapshot 읽기 추가

대상:

- 새 `Camera/CombatSceneCameraBinding.cs`·`.meta`.
- 명시 카메라 한 개의 snapshot 캡처 경로. Phase A에서는 `CombatMapCameras.cs`의 기존 snapshot 타입을 재사용하고, 타입·파일 이름 변경은 호출자 전환과 함께 Phase B에서 한다. 같은 baseline을 담는 새 타입을 병행하지 않는다.
- `ViewData/CombatPresentation/CombatCameraSettingsSO.cs`의 설명(실제 연결 이전에는 현행 설명을 성급히 교체하지 않음).

Task:

- [x] 가상 카메라·기존 설정 SO 참조와 구조 검사 제공. MonoBehaviour에 입력·서비스 조회·Tick 상태를 넣지 않는다.
- [x] 명시 카메라 하나의 기준 자세·렌즈·이름·활성 Listener 채널을 읽는다.
- [x] null·비활성 공용 대상·잘못된 투영·비어 있는 SO·실제 자세 writer 충돌을 상태 변경 전에 거절한다. 기존 줌 자세 검증을 재사용한다.
- [x] 카메라 실체가 배경/임시 표시 자식이 되지 않는 소유 조건을 정의한다.

### A3. 카메라 조립과 수명 경계 추가

대상:

- 새 `ViewFactory/CombatCameraFactory.cs`·`.meta`.
- 새 `Camera/CombatCameraSession.cs`·`.meta`.
- 필요한 경우 같은 Camera 폴더의 작은 선택/복원 협력 객체·`.meta`.

Task:

- [x] Factory가 바인딩·설정·필요한 Catalog/Effects만 받아 Window·View·선택 책임을 조립한다.
- [x] Session은 조립된 협력 객체의 Begin·복원·해제 순서와 소유권을 맡는다. 생성 코드·입력·사건 분기를 Session에 모으지 않는다.
- [x] Priority는 공개 Handle로 선택·복원한다. Handle을 Enter/AfterLoad 재수집을 가로질러 저장하지 않는다.
- [x] Begin 실패·부분 조립 실패에서 기준 자세·렌즈·Priority·adapter·줌 대여를 정리한다. 반복 Release는 안전하게 만든다. 이미 발행한 Impulse의 개별 취소 기능은 추가하지 않는다(설계 SC4).
- [x] 기존 Window·View 알고리즘을 재사용한다. 이 단계에서는 기존 호출자가 계속 사용할 수 있게 한다. 단지 전환 편의를 위한 영구 이중 API는 남기지 않는다.

**A 완료 조건:** 새 기반은 운영에 연결하지 않은 상태로 검토 가능하고, 기존 소비자는 유지된다. 변경 파일·소유권·서비스 계층을 정적으로 점검하고, 명시 요청에 따라 MSBuild를 수행한다. Unity의 **컴파일 미확인**을 별도로 보고하고 멈춘다. 다음은 Phase B이며 승인된 범위 안에서만 시작한다.

## 4. Phase B — 운영·도구 일괄 전환

### B1. 공용 프리팹과 씬 참조 구성

대상:

- 새 `Assets/ProjectL/Prefabs/Stage/CombatCamera.prefab`·`.meta`.
- `Assets/Scenes/Combat.unity`.
- `View/Combat/CombatSceneRoot.cs`.

Task:

- [x] 공용 VirtualCamera 하나와 Binding을 만들고 기존 `CombatCameraSettings_Default`를 연결한다. 새 출력 Camera·Brain·AudioListener를 추가하지 않는다.
- [x] 대표 구도를 필드 좌표계로 환산해 배경 밖의 안정된 씬 루트 아래에 둔다. 부모·카메라 스케일 1, 직교 ModeOverride 명시.
- [x] 기존 Listener 채널·필요한 VolumeSettings·클립 설정을 이관한다. 공용 카메라 활성 상태와 고유 이름 `Combat Camera`를 확인한다.
- [x] Root에 Binding 참조 하나와 누락 검사를 추가한다. 기존 출력 WorldCamera 계약은 유지한다.
- [x] 바인딩의 OnValidate/런타임 검증은 씬 복원 중 거짓 누락 보고를 피하는 기존 방식과 맞춘다.

### B2. 배경 카메라 간섭 차단·배경 책임 축소

대상:

- 새 `Camera/CombatBackdropCameras.cs`·`.meta`.
- `View/Combat/Display/Map/StageViewPropPresenter.cs`.
- `View/Combat/CombatLoadedScene.cs`의 참조 검사·설명.
- `RuntimeSystem/ErrorHandling/SceneNodeOrder.cs`의 설명만(숫자·순서 유지).

Task:

- [x] 배경 생성 직후, BindCameras 전 인스턴스 가상 카메라/Listener를 비활성화한다. GameObject 전체를 끄지 않는다.
- [x] 비활성 수집의 중복 이름을 피하도록 인스턴스 ID·번호로 고유 런타임 이름을 붙인다. 매핑·명명 규칙은 helper 한 곳에 둔다.
- [x] 원본 프리팹·Map SO·에디터 작업실·다른 씬 카메라는 변경하지 않는다. 지연 Destroy를 정합성의 전제로 쓰지 않는다.
- [x] Presenter의 `_mapCameras`·`MapCameras`·`_cameraSettings`·`CameraSettings`와 수집 호출을 제거한다. 배경·타일·낙석·등장 연출용 값은 유지한다.
- [x] 기존 CombatBackdrop Node 순서와 실패 시 배경 Release를 유지한다. 새 Node/Bootstrap 기능을 추가하지 않는다.

### B3. 운영 카메라 공급원·해제 연결 교체

대상:

- `View/Combat/CombatDisplayServices.cs`.
- `ViewFactory/CombatSceneDisplayFactory.cs`.
- `View/Combat/Display/CombatSceneDisplay.cs` 및 실제 카메라 해제 소유 파일(A1에서 정확한 분리 파일 확인).
- `Camera/CombatMapCameras.cs` → `CombatCameraSnapshot.cs`(파일·`.meta` 함께 이동, GUID 유지).
- `Camera/CombatCameraWindow.cs`, `CombatCameraView.cs`의 타입·공급원 이름·설명.

Task:

- [x] `CombatDisplayServices.From(global)`에서 기존 CameraGroup Catalog를 받는다. Root Builder 재등록 없이 Factory로 전달한다.
- [x] 배경 유무와 무관하게 Root Binding으로 공용 Session을 조립한다. 같은 Window를 모든 기존 사건/등장 연출 소비자에게 전달한다.
- [x] 기존 이동·줌·흔들림 계산은 유지한다. 내부 목록에는 snapshot 하나를 전달한다.
- [x] 맵 수집용 타입·API를 최종 이름으로 정리하고 사용하지 않는 수집기를 제거한다. `.meta` GUID를 유지한다.
- [x] Begin은 기존 Factory 실패 정리 안에서 실행한다. Display가 Session을 인수한 뒤에는 해당 소유자만 반환한다.
- [x] 기존 별도 CameraView Restore/Window Detach 호출을 Session 해제로 모아 이중 해제를 제거한다.
- [x] `backdrop = null`을 카메라 없음과 같은 의미로 취급하지 않는다. 정상 카메라 누락은 명시 실패다.

### B4. 아침 전환의 대상 이름 이관

대상:

- `ViewFactory/CombatSceneDisplayFactory.cs`의 RequireMapCamera.
- `View/Combat/Playback/CombatPhaseTransitionSettings.cs`.
- 호출을 전달하는 `ViewFactory/CombatEventPlaybackFactory.cs` 등 A1 호출 목록.
- `ViewData/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs`.
- `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset`.

Task:

- [x] 맵 Day 카메라 필수 검사를 제거한다.
- [x] 설정 타입·에셋의 아침 대상 이름 필드·검증을 제거한다.
- [x] 전환 설정 생성은 공용 Binding의 실제 이름을 인자로 받는다. 레이어·색·속도·양은 기존 DB 값을 쓴다.
- [x] 자동 전환은 비활성 그대로 유지한다. Render 초기 레이어·JMHTest 전환 예제는 수정하지 않는다.

### B5. 특성 확인 환경 전환

대상:

- `Assets/Scenes/Dev/TraitActivationPreview.unity`.
- `View/Dev/TraitPreview/TraitActivationPreviewHost.cs`.
- `ViewFactory/CombatTraitPreviewFactory.cs`.
- `HasMapCamera`를 사용하는 진단/에디터 호출자(A1에서 확인한 곳만).

Task:

- [x] 같은 공용 프리팹 하나를 배치하고 Host에 Binding을 연결한다. 기존 해당 환경 출력·Brain은 유지한다.
- [x] Host가 공용 Binding을 명시 전달하고 Factory는 운영과 같은 카메라 Session을 조립한다.
- [x] 반복 실행마다 이전 Session의 취소·복원이 끝난 뒤 새 Session을 Begin한다.
- [x] 진단은 맵 카메라 개수가 아닌 공용 Binding 준비 상태를 표시한다.
- [x] 시나리오 합성·특성 계산·프리뷰 외 기능은 수정하지 않는다.

### B6. 설정 소유 위치·편집 도구 정리

대상:

- `ViewData/Table/StageViewProp/Item/StageViewPropDataSO.cs`.
- `ViewData/Table/ViewStage/Item/ViewStageDataSO.cs`.
- `ViewData/CombatPresentation/CombatCameraSettingsSO.cs`의 설명.
- `Editor/CombatStageSoAssets.cs`, `CombatStageSoDocument.cs`, `CombatLayoutWindow.SoMode.cs`.

Task:

- [x] StageViewProp의 카메라 SO 필드·프로퍼티와 ViewStage의 필수 검사를 제거한다. 배경 규칙은 보존한다.
- [x] 스테이지별 카메라 목록·선택 UI·SerializedProperty 읽기/쓰기·작업본 cameraGuid를 함께 제거한다.
- [x] 옛 작업본의 다른 데이터는 보존하고, 폐기 카메라 값만 사용/재저장하지 않는다. 작업실 FbxPlacement/FieldCamera 코드는 수정하지 않는다.
- [x] 기존 StageViewProp 에셋의 폐기 YAML 정리를 위해 전체 재직렬화하지 않는다. 사용자 수정 중인 Test/Test2를 건드리지 않는다.
- [x] 새 유효 설정 참조는 공용 프리팹 한 곳임을 확인한다. 맵별 fallback·중복 설정 캐시는 남기지 않는다.

### B7. 기존 시험 호출 정합성·정적 검토

- [x] 새 Test 코드를 작성하거나 시험의 확인 내용을 바꾸지 않는다. A1에서 확인한 허용 범위의 생성자/호출 정합성만 맞춘다.
- [x] 다른 작성자의 실행 실패를 고칠 필요가 생기면 먼저 묻는다. 기존 시험이 옛 설계만 검사한다고 임의 삭제하지 않는다.
- [x] 서비스 공급 경로를 생성 → 내부 등록 → 상위 Export → 소비 조회까지 추적하고 보고한다.
- [x] 옛 공급원 참조(`MapCameras`, 맵 CameraSettings, Day 필수 검사, cameraGuid)가 활성 경로에 남지 않았는지 검색한다. 독립된 Render/JMHTest 예제는 잔존해도 범위 밖으로 명시한다.
- [x] `git diff --check`, 변경 범위·LF/UTF-8·`.meta`·씬 참조·SO GUID·불필요한 에셋 diff를 확인한다. 정적 검색을 컴파일·재생 성공이라고 보고하지 않는다.

**B 완료 조건:** 운영·특성 확인·편집 도구·필수 씬/프리팹 연결이 함께 전환돼 있고, 카메라 수명 소유자가 하나다. 구현 코드와 에디터 구성을 보고하고 **컴파일 미확인** 및 §6 Unity 확인표를 인계한 뒤 멈춘다.

## 5. Phase C — 확인 결과 반영·사용 안내·공유

### C1. Unity 확인 결과 반영

- [x] 사용자 결과와 직접 실행 결과를 구분했다. 아래 2026-11-09 직접 확인 범위를 기록하고 나머지는 미확인으로 유지한다.
- [x] 직접 확인 중 발견한 CombatSceneRoot.sceneCamera 누락만 기존 카메라 인스턴스에 재연결했다. 새 구도 정책·Render 계약·Core·Test 코드는 수정하지 않았다.
- [ ] 공용 초기 구도의 보드 가시성·줌 끝점·클립을 맞춘다. 최종 값과 기준 스테이지를 기록한다.

### C2. 문서·인계 정리

- [x] `docs/superpowers/viewDocs/combat-camera-zoom-pose-guide.md`를 공용 프리팹·SO·Binding 기준으로 갱신한다.
- [x] `docs/superpowers/viewDocs/runtime-scene-binding-guide.md`의 배경/카메라 순서 설명을 갱신한다. 숫자 원본은 SceneNodeOrder로 유지한다.
- [x] 기존 카메라 조작·줌 자세·배경 선생성 설계에는 새 설계 링크와 대체된 소유/공급원만 짧게 표시한다. 과거 실행 기록은 지우지 않는다.
- [x] 이 계획 §8에 실제 변경 파일·Phase별 결과·사용자 Unity 증거·남은 확인을 기록한다.
- [x] `docs/superpowers/handoffs/2026-11-08-combat-scene-camera-framework-share.md`에 공용 Factory/서비스 묶음 변경·씬 연결·도구 변경을 작성한다. 문서 작성은 외부 발송이 아니다.

**C 완료 조건:** 사용 방법·변경 범위·확인 결과·미확인이 서로 일치한다. 사용자 확인이 없으면 기능 검증 완료로 표시하지 않는다. 커밋·push 없이 보고하고 멈춘다.

## 6. 사용자 Unity 확인표

**2026-11-09: Codex가 MSBuild와 Unity Play 진입·종료를 직접 확인했다. 화면 캡처 실패로 시각 동작·전체 회귀는 미확인이다.** 새 Test 코드는 작성하지 않았다. Phase B의 MSBuild 성공은 Unity 컴파일·씬 직렬화·Cinemachine 재생·시각 동작 확인을 대신하지 않는다. 설정/위치 원본은 [사용 안내 §1·§5.2](../viewDocs/combat-camera-zoom-pose-guide.md)다.

### 기본 확인 — U01부터 순서대로

| ID | 절차 | 정상 기준 | 결과 |
|---|---|---|---|
| U01 컴파일·참조 | 에디터의 import/재컴파일 완료 후 Combat 씬과 특성 확인 씬을 연다. Root/Host의 Scene Camera와 프리팹 Binding의 Virtual Camera·Settings·Listener를 확인한다 | 새 컴파일 오류/Missing Script/필수 참조 누락이 없다. 각 씬에 공용 프리팹 인스턴스가 하나이며 기존 출력 Camera/Brain을 사용한다 | **부분 확인:** Unity 스크립트 DLL·Play 진입 확인, Combat Binding 누락 복구. 특성 확인 씬 미실행 |
| U02 진입·첫 구도 | Boot에서 정상 진행 경로로 Combat에 들어간다. 일반·보스·타일 배경을 비교한다. 기존 카메라가 있는/없는 배경은 해당 기존 에셋이 있을 때 각각 사용한다 | Binding이 가리키는 공용 VirtualCamera가 Live다. 보드·유닛·배경이 보이고 잘림·예상 밖 방향·이름 중복 오류가 없다. Day/Night 유무가 조작 가능 여부를 결정하지 않는다 | **부분 확인:** 기존 개발 설정의 PlacedStage Test(키 10000)로 표시·튜토리얼 연결 단계 도달. 시각/일반 경로/다른 맵 미확인 |
| U03 이동·제한 | WASD, 대각선, 화면 네 가장자리로 이동하고 각 방향의 제한까지 움직인다. 다른 줌 크기에서도 반복한다 | 대각선만 과속하지 않고 설정 범위에서 멈춘다. 줌 비례 속도와 화면 방향에 맞는 이동 축을 유지한다 | 미확인 |
| U04 사용자 줌·자세 | 시작 Size와 줌 양 끝점·중간점, 연속/역방향 휠, 이동 후 줌아웃을 확인한다 | UseZoomPose=true일 때 SO ZoomMax로 시작하고 Min/Max를 지킨다. 크기·위치·각도가 함께 바뀐다. 팬 오프셋은 유지되며 줌아웃 끝점으로 복귀한다 | 미확인 |
| U05 포인터·HUD | 이동·줌한 뒤 칸/유닛 호버·클릭, 배치 고스트, 커서 스냅, 유닛 HUD 위치를 확인한다 | 화면에 보이는 칸/유닛과 입력 대상이 일치하고 고스트·강조·HUD가 출력 카메라와 함께 움직인다 | 미확인 |
| U06 연출 줌·흔들림 | 피격·사망·기차/낙석 등 기존 줌/흔들림이 있는 연출을 재생한다. 사용자 줌/이동 후와 줌+흔들림 동시 실행도 확인한다 | 기존 연출이 공용 시점에 적용된다. 연출 줌 대여 중 사용자 줌이 새 보기 크기를 바꾸지 않고, 종료 후 기존 사용자 보기 크기로 돌아온다. 흔들림 뒤 영구 위치/각도 오프셋이 남지 않는다 | 미확인 |
| U07 입력 차단·복귀 | 포기 확인창·옵션 등 기존 입력 차단 상태를 열고 닫아 카메라 이동/휠 허용 정책을 확인한다 | 기존 카메라 입력 정책을 유지하고, 차단 해제 뒤 정상 입력으로 돌아온다. 닫은 창이나 이전 입력이 뒤늦게 카메라를 움직이지 않는다 | 미확인 |
| U08 이탈·재진입 | 카메라를 이동/줌한 뒤 일반 승리→StageSelect, 포기/패배/최종 승리→새 Run 준비 후 Lobby 등 가능한 정상 이탈을 확인하고 다음 Combat에 들어간다 | 목적지 정책을 유지한다. 새 Combat은 초기 구도로 시작하고 줌/팬/Priority가 누적되지 않는다. 출력 중복·이전 핸들/콜백 오류가 없다. 아직 실행할 수 없는 종료 경로는 미실행으로 기록한다 | **부분 확인:** Play 진입·종료 2회 새 예외 없음. 게임 내 승패 이탈·누적 구도는 미확인 |
| U09 특성 확인 | `ProjectL > Combat > Trait Activation Preview`에서 환경을 열고 줌/흔들림 연출의 Play·Stop·반복 재생·시나리오 교체를 확인한다 | 같은 공용 카메라를 사용하며 이전 연결이 다음 연결을 덮지 않는다. 이미 발행한 흔들림의 원래 Duration 내 자연 종료는 기존 계약이다. Stop 뒤 새 신호가 계속 발행되거나 자세/줌이 누적되면 문제로 기록한다 | 미확인 |
| U10 편집·DB | 스테이지 편집 도구에서 기존 데이터/옛 작업본을 읽고 검증용 작업본을 저장한다. 기존 `DataBaseAssetTests`를 Unity Test Runner에서 실행한다 | 폐기 cameraGuid/cameraSettings 조회·저장 오류가 없고 다른 스테이지 값은 보존된다. DB 에셋 교차 검증 결과를 기록한다 | 미확인 |

### 추가 확인 — 기본 확인 뒤

| ID | 절차 | 정상 기준 | 결과 |
|---|---|---|---|
| E01 설정 두 모드 | 현재 SO 값을 기록한 뒤 UseZoomPose=false, ZoomSeconds=0을 각각 임시로 적용하고 재진입한다. 확인 뒤 원래 값을 복원한다 | 꺼진 모드에서는 위치/각도 자세 줌 없이 Size만 바뀐다. 0초는 즉시 전환한다. 복원 후 원래 공용 설정으로 재진입한다 | 미확인 |
| E02 개발 진입·구도 | 기존 Play From Boot의 개발 Combat/Database 진입을 확인하고, 줌 끝점에서 일반/보스 맵의 가림·Near/Far Clip·후처리를 비교한다 | 개발 진입도 동일 Binding을 쓴다. 조정이 필요하면 스테이지·줌 단계·가려진 대상을 기록한다. 초기 구도 최종 확정은 이 확인 뒤 한다 | 미확인 |
| E03 기존 줌 자세 시험 | 기존 `CombatCameraZoomPoseTests`를 Unity Test Runner에서 실행한다. 현재 시험의 실제 결과를 기록한다 | 기존 검사 내용이 통과하거나, 실패한 시험 이름/예외/stack trace가 기록된다. 이번 작업에서 새 시험은 작성하지 않았다 | 미확인 |

각 확인 결과는 **통과 / 실패 / 미실행**으로 기록한다. 문제 보고에는 `확인 ID · 스테이지 키/배경 · 직전 조작 · 기대/실제 · 첫 Console 예외와 전체 stack trace`를 포함한다. 첫 구도/클립의 시각 조정과 기능 실패를 구분한다. 유효한 초기 Size는 SO 값이며 프리팹의 복원용 원래 Size와 다를 수 있다.

## 7. 변경 금지·결정이 필요한 경우

- Core·FieldCamera·Architecture·Render 내부 수정은 이 계획의 승인 범위가 아니다.
- 기존과 다른 SO 설정 에셋 발견, 원본 맵 카메라 삭제 필요, 새 출력 카메라/카탈로그 계약 필요, 맵별 구도/제한값 override 필요 시 먼저 묻는다.
- 전환 때문에 다른 작성자의 기존 시험이 실행 실패하면 수정 전 먼저 묻는다.
- 설계와 다른 수명·입력·조립 결정은 일반적인 진행 지시로 승인된 것으로 해석하지 않는다.
- Test 코드 작성 제한을 우회하지 않는다. MSBuild는 이번 사용자 명시 지시에 따라 수행하고, Unity 생성 csproj는 수정하지 않는다. 아직 csproj에 없는 새 소스는 스크래치 `CustomAfterMicrosoftCommonTargets`에 명시해 포함한다.

## 8. 실행 기록

### 2026-11-08 설계·계획 작성

- 현재 전투 카메라 공급·조작·줌 자세·효과·카탈로그·씬 Node·특성 확인·편집 도구 경로를 읽었다.
- 설계 SC1~SC10과 Phase A~C를 제안했다. 구현 Task는 모두 미착수다.
- 변경: 이 계획과 설계 Markdown 두 파일만 추가했다.
- 검증: 문서 경로·내용·범위 정적 확인. 코드·씬·에셋·기존 시험 미수정, MSBuild·Unity 미실행. **컴파일 미확인.**

### 2026-11-08 Phase A 완료 — 공용 카메라 연결 기반

- 사용자 진행 지시와 MSBuild 명시 요청을 인수했다. Phase A만 진행했고 운영 전환인 Phase B는 시작하지 않았다.
- 변경: 새 `CombatSceneCameraBinding`, `CombatCameraSelection`, `CombatCameraSession`, `CombatCameraFactory`와 각 `.meta`, 기존 `CombatMapCameras.cs`의 `CombatMapCamera.Capture` 공통 읽기 경로. 기존 SO 설명·운영 조립·씬·프리팹·DB 에셋·시험은 유지했다.
- Binding은 참조·활성 상태·로드된 동일 씬·바인딩 자신/자식 소유 조건·명시 직교 투영·기존 자세 충돌 검사·활성 Listener/채널을 검사한다. snapshot을 보관하거나 입력/서비스 상태를 소유하지 않는다. 배경 밖의 안정된 루트 배치는 Phase B 씬 조립과 Unity 확인에서 적용한다.
- Factory는 Catalog/Effects 인터페이스와 명시 Binding을 받아 기존 Window/View를 조립한다. Session은 선택·Begin·실패 복구·해제만 맡고, 해제 뒤 오래된 Control 참조는 이동/줌을 수행하지 않는다. Window는 Detach로 비워져 추가 줌/흔들림 요청이 거절된다.
- Selection은 Priority만 기억하고 조작 때 Handle을 빌려 즉시 반환한다. 씬 대상 파괴 뒤 Release는 카탈로그 재조회를 하지 않는다. 새 Factory는 아직 소비자에 연결되지 않아 Global 서비스 등록/Export/조회 변경이 없다. 실제 서비스 공급 연결은 B3에서 기존 CameraGroup 경로를 사용한다.
- 기존 맵 수집도 같은 Capture를 재사용한다. 기존 생성자·Read·AttachMap/DetachMap·표시 생성 서명을 바꾸지 않아 기존 시험 호출 수정이 필요하지 않았다. 줌 자세 시험의 마지막 수정은 `8ac16c1ed`, 작성자 Droute, 제목 `[용근] 전투 View, 카메라 줌/아웃 각도 변경 추가`로 확인했다. 다른 시험의 수정 허용을 추정하지 않았고 시험 코드는 모두 미수정이다.

#### 조사한 이관 출처와 남은 씬 확인

- StageViewProp 21개 에셋은 모두 기존 `CombatCameraSettings_Default` GUID `d3d258fc9dc94d5a8297d2f88842ccef`를 사용한다. 공용값을 새로 통합하거나 덮어쓰지 않았다.
- 대표 출처는 `StageViewProp_Chapter2_Regular`(stageKey 2201) → `Prefabs/Stage/MapProp/Test/Stages/StageTest.prefab`이다. 현재 `Day Camera`의 프리팹 로컬 위치는 (-18.67, 26.6, -18.57), 회전 힌트 (45, 45, 0), Scale (1,1,1), Priority 1, 명시 Orthographic, 원래 Size 25.7, Near/Far 0.35/50이다. 위치가 곧 배치 완료 월드 구도라는 뜻은 아니다.
- VolumeSettings Profile GUID `d8f3dad90d142ea4abc3add84b4ea7b5`; Impulse Channel 1, Gain 1, ApplyAfter 2, UseCameraSpace 1, Use2DDistance 0, Reaction Noise GUID `990a0588014e422d86cd7911cf6e6c66`를 확인했다.
- 해당 배경 입력은 Offset (0.4,0,0), Rotation (0,-90,0), Scale (0.75,0.75,0.75), PrefabWithTiles다. 카메라 조상 Transform과 타일 정렬까지 적용한 최종 구도를 Phase B에서 필드 기준으로 이관하고 Unity에서 확인해야 한다. 현재 로컬값만 공용 카메라에 복사하지 않는다.
- 활성 호출 경로: StageViewPropPresenter → 맵 Capture/설정 → 운영 DisplayFactory 및 TraitPreviewFactory, TraitActivationPreviewHost 진단; Window/View와 CombatSceneDisplay의 기존 해제; 아침 전환 설정 타입/Factory; CombatStageSoAssets/Document/LayoutWindow의 cameraGuid다. 이 경로의 전환은 전부 B Task에 남겼다. FbxPlacement의 독립 카메라/시험은 범위 밖이다.

#### 검증 결과

- 정적 검토: 기존 맵 Capture의 렌즈·Listener 판독식 동일, 조립/기능/수명 분리, 새 코드 LF·UTF-8·공백·`.meta` 확인. 기존 사용자 변경은 보존했다.
- VS 2022 Community MSBuild: `ProjectL.Systems.csproj /t:Build /m:1 /p:Configuration=Debug /p:UseSharedCompilation=false` 성공(exit 0). 종속 프로젝트도 일반 Build 경로로 빌드했으며 소스는 수정하지 않았다.
- Unity 생성 csproj에는 새 파일 4개가 없어 `Temp/Codex/CombatSceneCameraPhaseA/NewSources.targets`를 `CustomAfterMicrosoftCommonTargets`로 주입했다. 생성 csproj는 미수정이다. 로그: `Temp/Codex/CombatSceneCameraPhaseA/MSBuild.log`.
- 오류 0. 수정하지 않은 Core `CombatCommandEffects.cs`의 CS8632 1건과 UI 세 파일의 CS0649 7건, 총 경고 8건. 새/변경 카메라 파일의 경고는 없다.
- 기존 수정 파일의 혼합 개행을 LF로 정리한 뒤 최종 소스로 MSBuild를 다시 수행해 성공(exit 0)했다. 최종 로그는 UTF-8 `Temp/Codex/CombatSceneCameraPhaseA/MSBuild-final.log`이며 오류 0, 수정하지 않은 UI의 CS0649 7건이다. Core는 최신 상태로 빌드를 건너뛰어 첫 빌드의 CS8632는 재출력되지 않았다.
- 새 Test 작성·기존 시험 변경·Test Runner·Unity 재생은 수행하지 않았다. **Unity 컴파일 미확인**, 화면/동작 검증 미확인. MSBuild 성공을 Unity 확인으로 대체하지 않는다.
- Phase A 보고 후 멈춤. 다음 Phase B는 공용 프리팹/Root 연결, 배경 간섭 차단, 운영·특성 확인 전환, 폐기 DB/편집 설정 정리를 함께 수행한다.

### 2026-11-08 Phase B — 운영·씬·도구 전환

- 사용자 지시: "Phase B 진행해. 끝나면 MSBuild도 돌리고 보고해줘."
- `Assets/ProjectL/Prefabs/Stage/CombatCamera.prefab`을 만들고 Combat 및 TraitActivationPreview 씬에 각각 한 인스턴스를 연결했다. 기존 출력 Camera/Brain은 유지했다. 공용 프리팹 GUID는 `4c86c5e22a1541199b46eba7c7dcb16d`다.
- 대표 스테이지 2201의 13×13 필드, 간격 1.5, 17×17 타일·크기 2·배율 0.75·Offset (-1,-2,0.5)·Anchor (2,2)·배경 Y 회전 -90°를 현재 정렬식에 대입했다. 배경 배치 완료 후 필드 기준 카메라 위치는 **(14.3025, 19.95, -13.2525)**, 회전은 **(45,-45,0)°**다. 필드 루트가 두 씬 모두 원점·회전 0·배율 1임을 확인했다. 이 값을 공용 프리팹에 적용했으며 실제 구도는 Unity 확인 대기다.
- 원본 Size 25.7, Near/Far 0.35/50, Volume Profile·Impulse Listener/Reaction 설정을 이관했다. 기존 공용 SO의 현재 값(Zoom 4~12, 시작은 UseZoomPose의 ZoomMax 12, 줌인 위치 (0,-3,0.1)·회전 (-10,0,0))은 변경하지 않았다.
- `CombatSceneRoot.sceneCamera` 참조·검사를 추가했다. 기존 Root의 지연 OnValidate가 참조 누락을 확인하고, 런타임에는 같은 씬·필드/배경 밖의 소유 경계와 Binding 구성을 검사한다. 설정 값은 CameraFactory에서 UI/표시 Configure보다 먼저 검사한다.
- `CombatBackdropCameras`가 생성된 배경 인스턴스의 가상 카메라와 Listener만 비활성화하고 인스턴스 ID/번호로 이름 충돌을 피한다. 원본 맵 에셋·GameObject 활성 상태·FieldCamera·MapAssembler는 수정하지 않았다. 기존 CombatBackdrop/BindCameras 순서는 유지했다.
- 배경 Presenter의 카메라 수집·설정 보관을 제거했다. `CombatMapCameras.cs`를 `CombatCameraSnapshot.cs`로 옮기고 기존 `.meta` GUID `e4b907245888b0f4b85c1fefd2372609`를 유지했다. 전체 맵 수집기는 제거했으며 명시 카메라 한 개만 Capture한다.
- 운영 DisplayFactory와 TraitPreviewFactory가 같은 CameraFactory/Session을 사용한다. Display는 Session 하나만 반환한다. Factory의 부분 조립·Configure 실패도 Session을 반환하도록 정리 범위를 맞췄다. 기존 이동·줌·흔들림 계산은 유지했다.
- 서비스 경로: 기존 CameraGroupFactory 생성·내부 Builder 등록 → CameraGroup/Render/Global Context → `CombatDisplayServices.From(global)` → CameraFactory의 Catalog/Effects. 새 전역 등록·상위 재노출·Root resolver 우회는 없다.
- Day Camera 필수 검사와 DB의 아침 대상 이름 필드를 제거했다. 비활성 아침 전환 설정도 Binding의 실제 이름을 받는다. 자동 낮/밤 전환은 비활성 그대로다.
- StageViewProp/ViewStage의 카메라 설정 필수 필드·검사를 제거하고, CombatStageSoAssets/Document/LayoutWindow의 cameraGuid 읽기·쓰기·선택·공유 사용자 집계를 정리했다. 옛 작업본의 다른 필드는 유지된다. 기존 StageViewProp YAML은 무시되는 잔여값으로 남겨 사용자 에셋을 일괄 재직렬화하지 않았다.
- 특성 확인 Host·Window 진단과 **TraitActivationPreviewSceneBuilder**도 공용 카메라를 사용하도록 맞췄다. SceneBuilder 수정은 확인 씬 재생성 때 필수 참조가 빠지지 않도록 한 B5의 직접 소비자 정합성 작업이다. 시나리오·특성 판정은 변경하지 않았다.
- 공용 변경 전달 문서: [Framework/View 공유 메모](../handoffs/2026-11-08-combat-scene-camera-framework-share.md). 외부 미전달.

#### B 검증·한계

- 정적 확인: 두 씬에 동일 프리팹 인스턴스·sceneCamera 참조가 각각 하나이며, 기존 씬 대비 새로 미해결된 로컬 fileID가 없다. 공용 프리팹의 내부 참조, 메타 GUID 보존, 소스 LF/UTF-8/공백, 변경 범위의 `git diff --check`를 확인했다.
- 활성 전투·특성 확인·편집 경로에 옛 MapCameras/AttachMap/DetachMap/cameraGuid/아침 Day 대상 설정이 남지 않았음을 검색했다. 독립 StageMap·FbxPlacement·JMHTest 카메라는 범위 밖으로 유지했다.
- MSBuild: `ProjectL.Systems.csproj` 성공(오류 0, 수정하지 않은 Core/UI 경고 8), `ProjectL.Tool.Editor.csproj` 성공(오류 0·경고 0), `Assembly-CSharp-Editor.csproj` 성공(오류 0·경고 15). 에디터 경고에는 수정하지 않은 UI 경고와 의존성 버전 충돌(MSB3277)이 포함된다. 마지막 정리 범위 보완 후 최종 빌드 결과는 아래 후속 기록을 따른다.
- 새/이동 소스 6개와 옛 소스 제거를 `Temp/Codex/CombatSceneCameraPhaseB/NewSources.targets`로 반영했다. Unity 생성 csproj는 수정하지 않았다. 로그는 같은 폴더의 `Systems-MSBuild.log`, `ToolEditor-MSBuild.log`, `AssemblyEditor-MSBuild.log`, `Final-MSBuild.log`다.
- 기존 사용자 수정·원본 맵·Core·Framework·Render 내부·기존 시험 코드는 미수정이다. 새 Test 작성·Test Runner·Unity 재생은 하지 않았다. **Unity 컴파일 미확인**, 시각/동작 확인 미확인.
- 사용자 확인: Boot→Combat 진입, WASD/가장자리 이동, 휠·연출 줌과 흔들림, 칸 클릭/HUD 투영, 이탈·재진입, 특성 확인 반복 재생, 스테이지 편집 저장, 기존 DataBaseAssetTests. 전체 확인표는 §6이다.
- Phase B에서 멈춘다. Phase C의 시각 조정·사용 안내 전체 갱신·최종 확인 완료 처리는 아직 하지 않았다.
- 최종 후속: 조립 실패 정리 범위를 Configure 이전까지 보완한 소스로 `Assembly-CSharp-Editor.csproj`와 종속 Systems를 다시 빌드해 **exit 0, 오류 0, 경고 15**를 확인했다. 경고는 수정하지 않은 UI/에디터 의존성에서 발생했다. 최종 로그는 `Final-MSBuild.log`, 경과 시간 39.28초다. `git diff --check`도 통과했다. Phase B 구현과 보고 준비를 정상 완료했다.

### 2026-11-09 Phase C — 문서·사용자 확인 인계

- 사용자 지시: "Phase C 진행해. Unity에서 내가 확인해야 할 항목도 정리해서 보고해줘."
- C2 완료: 공용 카메라/Binding/SO 기준으로 줌 자세 안내를 갱신하고, 씬 연결 안내의 배경 생성·옛 카메라 차단·공용 카메라 수집 순서를 맞췄다. 숫자 원본은 SceneNodeOrder에 유지했다. 이전 카메라 조작·줌 자세·배경 선생성 설계에는 대체 범위와 새 원본 링크만 덧붙여 과거 기록을 보존했다.
- 현재 코드·프리팹·SO를 읽어 설명을 대조했다. 현재 공용 프리팹 오브젝트 이름은 `CombatCamera`다. Binding은 실제 참조의 이름을 읽으므로, 최초 Phase B 작성 시점의 `Combat Camera` 이름을 고정 계약으로 안내하지 않는다. 기존 프리팹/씬의 재직렬화·파이프라인 자식 등 사용자/에디터 변경을 보존했다.
- §6을 U01~U10 기본 확인과 E01~E03 추가 확인으로 정리했다. 절차·정상 기준·결과 칸을 한 곳에서 관리하며 안내/공유 문서는 이를 링크한다.
- C1 상태: Unity 결과가 아직 전달되지 않았다. 따라서 오류 수정 필요 여부·실제 보드 가시성·줌 끝점·클립의 최종 조정은 대기다. 모든 실행 결과를 미확인으로 유지하고 Phase C 전체를 검증 완료로 표시하지 않는다.
- 검증: 문서 상대 링크·파일 위치·LF/UTF-8·공백·변경 범위를 정적으로 확인했다. 이번에는 문서만 수정했으며 코드/씬/프리팹/SO/시험은 수정하지 않았다. 새 Test 작성·Unity 실행·MSBuild 재실행은 하지 않았다. Phase B MSBuild 성공은 당시 기록이고 `Temp` 로그는 현재 작업 폴더에 남아 있지 않다. **Unity 컴파일 미확인, 재생 미확인.**
- [공유 메모](../handoffs/2026-11-08-combat-scene-camera-framework-share.md)도 현재 상태와 확인표 링크로 갱신했다. 외부 미전달. 커밋·push하지 않았다. 문서 인계를 보고한 뒤 사용자 확인 결과를 기다린다.

### 2026-11-09 사용자 요청 — MSBuild·Unity 직접 확인 및 씬 참조 복구

- 요청: "MSBuild, Unity 컴파일·재생도 너가 확인해줘 use-compute". 컴퓨터 사용 스킬로 Unity를 조작했다. 기존 검증 제한의 명시 예외로 수행했으며 새 Test 코드는 작성하지 않았다.
- **MSBuild 재확인 성공:** 현재 Unity 생성 csproj에 새 카메라 파일이 포함돼 별도 targets 주입 없이 `Assembly-CSharp-Editor.csproj /t:Build /m:1 /p:Configuration=Debug /p:UseSharedCompilation=false`를 실행했다. 종속 Systems 포함 exit 0, 오류 0·경고 15, 11.94초. 경고는 기존 UI 및 에디터 의존성 버전 충돌이다. 로그: `Temp/Codex/CombatSceneCameraVerify-20261109/MSBuild.log`.
- **실제 누락 발견·복구:** 현재 저장된 Combat 씬의 `CombatSceneRoot.sceneCamera`가 `{fileID: 0}`이었고 Editor.log에 같은 누락으로 진입 실패한 이전 기록이 있었다. 현재 씬에 이미 존재하는 공용 카메라 PrefabInstance `1963127221`의 Binding(source fileID `114103`)을 Root에 재연결했다. 새 카메라를 중복 생성하거나 사용자 인스턴스의 Transform·override를 되돌리지 않았다. 수정 전 씬은 같은 Temp 폴더에 보관했다.
- 수정 뒤 Unity에서 씬을 다시 로드하고 저장했으며, Root의 비어 있지 않은 참조가 유지됨을 확인했다. 참조 보완은 승인 설계 SC1/C1 범위이고 Core·FieldCamera·Architecture·Render·게임 규칙 변경은 없다.
- **Unity 컴파일 상태:** Unity `Library/ScriptAssemblies/ProjectL.Systems.dll`(2026-11-08 23:36:58)과 `Assembly-CSharp-Editor.dll`(23:37:02)이 해당 최종 소스 수정 시각보다 뒤에 생성돼 있다. Unity가 두 번의 Play 진입을 허용했고 실행 구간에 새 compiler error/예외가 없었다. 새 소스 변경 없이 에디터에서 강제 전체 재컴파일한 것은 아니다.
- **Play 직접 실행 2회:** Unity 6000.0.73f1의 기존 Play From Boot 설정(Combat·UseDevProgress·로드아웃 1·챕터 1·노드 2·PlacedStage Test)을 변경하지 않고 재생했다. 두 번 모두 `[GameBootstrap]` 시작 이후 전투 표시 연결 다음의 `[Tutorial] 화면 연결 1: 실행 대상이 아닙니다(NoDefinition)`까지 도달했다. 각각의 실행 구간에 새 Exception/CS 컴파일 오류/RuntimeFaultBoundary 오류가 없었다. `NoDefinition`은 튜토리얼 대상 없음 안내이며 오류가 아니다.
- 두 실행을 종료해 원래 Combat 씬 편집 레이아웃으로 돌아왔다. 최종 종료 로그에서도 새 정리 예외가 없었다. 종료 후 씬 저장 참조도 유지됐다. 실행 로그 발췌는 `Unity-play-first.log`, `Unity-play-second.log`다(같은 Temp 폴더, 임시 산출물).
- **화면 한계:** 컴퓨터 사용의 Unity 스크린샷은 `FrameArrived timed out: timed out waiting on channel` 및 `window capture timed out: timed out waiting on channel`로 실패했다. 창 재선택 후 재시도했으며 접근성 텍스트·키보드와 Editor.log로만 실행 상태를 확인했다. 인덱스 클릭도 `coordinate input geometry is unavailable`로 제한됐다. 따라서 실제 렌더 화면·카메라 이동/줌/흔들림·포인터·HUD·구도는 통과로 표시하지 않는다.
- §6 U01/U02/U08의 직접 확인 부분만 갱신했다. 나머지 시각·입력·게임 내 이탈·특성 확인·Test Runner 항목은 미확인이다. MSBuild/Play 진입 성공을 전체 기능 검증 완료로 확대하지 않는다. 커밋·push하지 않았다.
