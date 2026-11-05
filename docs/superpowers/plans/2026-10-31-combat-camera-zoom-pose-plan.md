# 전투 카메라 줌 자세 Task 계획·실행 기록

작성: 2026-10-31. [최신 설계](../specs/2026-10-31-combat-camera-zoom-pose-design.md). 상태: CP1~CP3 및 사용자 요청 SO 통합 수정 완료. Unity 확인 대기.

## Phase와 실행 결과

- CP1: 값 계산기·설정 검증·카메라 원래 자세 인수 완료.
- CP2: 실제 렌즈 기반 PrePipeline/Finalize 적용, 팬 전달, Begin/Restore 및 실패 복구 완료.
- CP3: [설정 안내·사용자 확인표](../viewDocs/combat-camera-zoom-pose-guide.md), 회귀 테스트, 정적 검증 완료.
- 최초 구현은 마커 참조 방식이었다. 사용자가 설정을 CombatCameraSettings_Default에 모두 모으도록 지시하여 최신 설계로 대체했다. 아래 통합 기록이 현재 기준이다.

## SO 통합 후속

1. 기존 CombatCameraSettingsSO/Data에 useZoomPose 및 양 끝점의 위치·회전 오프셋 4개를 추가. 유효값 검사는 Data.FindError 한 곳이다. SO.CreateData는 매번 새 불변 사본을 만든다.
2. CombatCameraZoomPose의 모든 직렬화 필드·마커 캡처를 제거. CameraView가 자세 모드에서 각 VirtualCamera에 adapter를 자동 추가/재사용한다. adapter는 배경 수명 동안 유지하고 표시 해제 시 Unbind한다.
3. 모든 끝점은 각 카메라 원래 자세 기준 상대값. 시작 Size는 SO ZoomMax, 프리팹 렌즈 일치 조건 제거. 해제 시 원래 카메라별 렌즈로 복원한다.
4. 기존 에셋은 useZoomPose=false 기본값으로 호환. 씬·프리팹·SO 에셋 값은 수정하지 않았다. 사용자 값 변경·에디터 확인 대기.

## 검증

- SO 통합 후 ProjectL.Systems 정적 MSBuild 성공. 기존 UI CS0649 경고 7건, 신규 카메라 경고 없음.
- 빌드된 Systems.dll 관리 코드 23건 통과: 기존 진행률/범위/자세 16 + SettingsData 사용 여부/같은 크기 범위/4개 오프셋 유효성 7. Unity의 Mono 사용, 에디터/native camera 실행 아님.
- EditMode 테스트를 기존 21건에서 31건으로 확장: 상대 좌표·양 끝점·공유 설정·기존 모드 호환·4개 필드 유효값·SO 불변 사본. ProjectL.Tests.CoreIntegration 정적 MSBuild도 성공했다. 테스트 실행은 사용자 담당이다.
- 새 소스는 Temp의 CustomAfterMicrosoftCommonTargets로 MSBuild에 포함. 생성 csproj는 수정하지 않았다.
- Core·FieldCamera·Architecture·Render 서비스·패키지 수정 없음. 신규 전역 서비스·Builder/등록/Export 계약 변경 없음. 기존 사용자 변경 유지. 커밋·push·스테이징 없음.
- [Factory 공유 메모](../handoffs/2026-10-31-combat-camera-zoom-pose-framework-share.md)는 외부 미전달.

## 사용자 요청: 기본 에셋 값 적용

CombatCameraSettings_Default에 사용 켜짐, Size 4~8, Step 1, Seconds 0.2, 줌아웃 오프셋 0, 줌인 위치 (0,-3,0.1)·회전 (-5,0,0)을 적용했다. 이전 에셋 미수정 기록 이후 사용자가 명시적으로 요청한 후속이다. Pan·씬·프리팹은 수정하지 않았다. 실제 YAML 값을 읽어 빌드된 SettingsData.FindError 및 생성자 검사를 통과했다(기존 관리 코드 23건 포함 총 24건). DataBaseAssetTests 및 Unity 시각 확인은 사용자 담당으로 미실행이다.

## 사용자 재생 오류 수정: 대상 없는 CinemachineTransposer

사용자가 Day Camera의 Transposer 충돌 예외를 보고했다. 설치 Cinemachine 소스에서 Transposer.IsValid는 enabled && FollowTarget != null이고, MutateCameraState도 IsValid일 때만 위치를 변경한다. 현재 프랍 Follow가 비어 있는데 검증이 enabled만 검사하여 과하게 거절한 것이 원인이다.

Body/Aim 검증을 part.IsValid 기준으로 수정했다. Follow 없는 Transposer는 유지 가능하며 실제 Follow/LookAt/활성 자세 제어 충돌 거절은 유지한다. 실제 VirtualCamera·Transposer 구성으로 대상 없음 허용/Follow 있음 거절 테스트 2건을 추가했다(총 33건). 테스트는 기존 어셈블리 참조를 변경하지 않고 설치된 Cinemachine 공개 API를 reflection으로 호출한다. 씬·프리팹·에셋 변경 없음. 에디터 테스트는 사용자 담당으로 미실행이다.

수정 후 ProjectL.Systems 및 ProjectL.Tests.CoreIntegration 정적 MSBuild 모두 성공했다. 기존 UI CS0649 7건 외 신규 경고·오류 없음. 추가된 Unity 구성 회귀 테스트는 컴파일만 확인했으며 실행하지 않았다.

## 사용자 재생 오류 수정: CinemachineVolumeSettings

설치 패키지의 VolumeSettings pipeline은 Finalize에서 후처리 profile·focus distance·CustomBlendable을 적용하며 raw 위치·회전은 쓰지 않는다. 따라서 줌 자세와 충돌하지 않는 확장으로 허용했다. 현재 StageTest Day/Night의 직접 확장은 VolumeSettings와 ImpulseListener임을 읽기 전용으로 확인했으며 둘 다 허용 대상이다. 어댑터는 PrePipeline에서 먼저 자세를 제공하므로 후처리 Finalize의 초점 계산도 현재 자세를 읽는다.

VolumeSettings 허용/CameraOffset(자세 writer) 거절 회귀 2건 추가, 총 35건. 기존 무조건 허용으로 검사를 제거하지 않았으며 실제 자세·렌즈 소유권 충돌은 계속 거절한다. 씬·프리팹·SO·Render·패키지 소스 수정 없음. 에디터 실행은 사용자 담당이다.

VolumeSettings 수정 후 Systems 및 CoreIntegration 테스트 코드의 정적 MSBuild 모두 성공했다. 기존 UI CS0649 경고 외 신규 경고 없음. 추가 구성 테스트는 컴파일만 확인했으며 실행하지 않았다.

## 사용자 재생 오류 수정: 비활성 Night Camera의 투영 명시 (2026-11-01)

비활성 Night Camera의 None 상속 상태가 초기화되기 전에 Orthographic 검증이 실행되어 진입이 중단됐다. 사용자가 명시 설정을 지시하여 Stage.prefab·Stage_Test.prefab·Test/StageTest.prefab의 Day/Night 총 6개 VirtualCamera를 `ModeOverride: 0`에서 `1`(Orthographic)로 변경했다. 카메라 활성 상태·다른 프리팹 값·코드는 변경하지 않았다. diff 검사 통과, Unity 재생 확인 대기.

오류 원인, 대상 경로, 향후 투영·카메라 구성 변경 시 함께 갱신할 설정의 기준은 [설정 안내 §2.1](../viewDocs/combat-camera-zoom-pose-guide.md#21-orthographic-명시-설정-후속-2026-11-01)에 기록했다. 기존 프리팹 미수정 기록은 이 후속 이전 이력이다.
