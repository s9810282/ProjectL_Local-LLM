# 전투 카메라 줌 자세 설계

> **2026-11-09 대체 범위:** 기준 자세는 맵 캡처 대신 씬 공용 VirtualCamera의 CombatCameraSnapshot에서 읽고, 설정 SO는 공용 Binding이 참조한다. 자세 수식·실제 렌즈 기반 adapter는 유지하고 연결·복원은 CameraSession이 소유한다. 최신 원본은 [씬 공용 카메라 설계](2026-11-08-combat-scene-camera-design.md)와 [사용 안내](../viewDocs/combat-camera-zoom-pose-guide.md)다. 아래 결정·구현 기록은 이전 시점 이력으로 보존한다.

작성: 2026-10-31. 최신 결정: 사용자 요청으로 모든 줌 설정을 CombatCameraSettingsSO로 통합. CP1~CP3 코드·문서 완료, Unity 확인 대기. 기존 마커 방식은 아래 설계로 대체한다.

## 1. 현재 구조

입력은 CombatCameraInput → ICombatCameraControl → CombatCameraView다. View가 팬·목표 Size를 소유하고 CombatCameraWindow가 일반/연출 줌의 단일 창구다. Render CameraZoomEvent는 실제 렌즈를 SmoothStep으로 보간한다. CombatMapCameras는 배경 배치 후 원래 카메라 위치·회전·렌즈를 캡처한다. 이 원래 값은 해제 복원과 카메라마다의 기준 구도다.

## 2. 모든 authoring 설정의 원본

StageViewPropData.cameraSettings가 참조하는 CombatCameraSettingsSO(현재 기본 에셋 CombatCameraSettings_Default) 한 곳에서 다음을 설정한다.

| 필드 | 의미 |
|---|---|
| Use Zoom Pose | Size·위치·회전 연동 사용 여부 |
| Zoom Min / Zoom Max | 줌인/줌아웃 Size. 자세 모드 시작 Size는 ZoomMax |
| Zoom Step / Zoom Seconds | 기존 휠 단계와 시간 |
| Zoom Out Position / Rotation Offset | 줌아웃 끝점 위치·회전 |
| Zoom In Position / Rotation Offset | 줌인 끝점 위치·회전 |
| 기존 Pan 설정 | 이동 속도·범위·가장자리 두께 |

위치 오프셋은 원래 카메라의 로컬 축 기준 월드 단위, 회전 오프셋은 원래 회전에 곱하는 로컬 Euler 도다. Day/Night 각각 자신의 원래 구도에 같은 SO 값을 적용한다. 절대 월드 좌표를 저장하지 않아 맵 배치 위치가 달라도 재사용할 수 있다. 양 끝점은 모두 같은 원래 카메라 자세 기준이며 줌인 값을 줌아웃 값에 누적하지 않는다.

줌아웃 오프셋이 모두 0이면 원래 구도로 돌아온다. 줌아웃 오프셋을 지정하면 그 설정 구도가 복귀 지점이다. 팬은 별도로 유지한다.

SO는 매번 새 불변 CombatCameraSettingsData를 만든다(P2). 새 필드의 기본값은 사용 꺼짐·오프셋 0이다. 기존 에셋은 수정 없이 Size 전용 모드를 유지한다. 자세 모드에서는 prefab BaseLens=ZoomMax를 요구하지 않고 연결 때 SO ZoomMax로 렌즈를 맞춘다. 해제는 SO 초기값 대신 캡처한 원래 렌즈로 복원한다.

## 3. 수식과 실행

- 끝점 위치 = basePosition + baseRotation * positionOffset.
- 끝점 회전 = baseRotation * Quaternion.Euler(rotationOffset).
- t = Clamp01((ZoomMax - actualSize) / (ZoomMax - ZoomMin)).
- 위치는 Lerp, 회전은 최단 Quaternion.Slerp. 경로 자체는 직선, 시간 속도감은 기존 렌즈 SmoothStep을 따른다.
- 팬은 줌아웃 끝점 회전의 화면 축으로 더하며 기존 제한·속도 정책은 유지한다.
- 연출 줌도 실제 렌즈를 읽어 같은 경로를 사용한다. 범위 밖 Size에서는 자세만 끝점에 고정한다.

렌즈 쓰기는 기존 공개 효과 Dispatch 경로뿐이다. View adapter는 실제 CameraState.Lens.OrthographicSize를 읽고 PrePipeline과 Finalize에 동일 기초 자세를 제공한다. PrePipeline은 Impulse의 거리 감쇠와 카메라 공간 방향 계산을 현재 자세에 맞춘다. Finalize는 raw 위치·회전만 적용하며 correction은 보존한다. Brain이 Day/Night를 blend한 최종 출력은 기존 HUD 투영이 읽는다. 별도 자세 시계·진행률·HUD 줌 상태는 없다.

## 4. 자동 조립·수명·검증

별도 줌인 마커, Transform 참조, 카메라별 Inspector 설정은 없다. UseZoomPose=true이면 CameraView.Begin이 각 VirtualCamera에 CombatCameraZoomPose adapter를 자동 추가하거나 재사용한다. adapter에는 직렬화 설정이 없다. 배경 수명 동안 남아도 Unbind 상태는 아무 자세도 쓰지 않으며 배경 파괴 때 함께 제거된다.

모든 카메라의 설정·값을 먼저 검사하고 연결한다. Orthographic, Follow/LookAt 없음, 실제 동작하는 Body/Aim 없음(IsValid=false인 단계는 허용), 상위 VirtualCamera 없음이 필요하다. ImpulseListener·CinemachineVolumeSettings 외 활성 extension은 충돌 검토 전 거절한다. 자세 모드에서는 카메라 효과 서비스가 있어야 시작 Size를 맞출 수 있다. Begin 실패/Display.Release에서 자세 연결을 먼저 끊고 원래 위치·회전·렌즈를 복구한다. Factory는 Begin을 기존 실패 정리 범위 안에서 호출한다.

0 < ZoomMin <= ZoomMax, 유한 오프셋을 SettingsData.FindError에서 검사한다. 자세 사용 시 Min < Max다. 사용하지 않는 기존 경로는 Min=Max를 계속 허용한다. Quaternion 및 실제 렌즈 유효성은 계산기가 검사한다.

## 5. 원칙·경계 검토

SSOT: authoring 설정은 SO, 원래 카메라 사실은 맵 캡처, 팬은 View, 실제 렌즈는 효과 서비스, 자세 수식은 Path다. SRP는 설정/계산/적용/조립을 분리한다. OCP는 공개 CinemachineExtension을 사용한다. LSP는 입력·거절·복원 계약을 유지한다. ISP는 입력 인터페이스에 authoring을 추가하지 않는다. DIP는 값 계산기가 Cinemachine과 입력을 모르게 한다.

JMH/FW: 새 전역 서비스·Builder 변경·루트 재등록·직접 resolver 조회 없음. 기존 Global 문맥의 CameraEffects를 그대로 사용한다. Core·FieldCamera·Architecture·Render 서비스·패키지 소스는 수정하지 않는다. 기능 변경은 View 카메라와 View 설정 데이터 안에서 끝나며 HUD나 전투 결과 재생을 수정할 필요가 없다. 기존 연속 휠의 From=이전 목표 정책 개선은 범위 밖이다.

## 6. 검증·사용자 작업

시각·통합 검증은 사용자 담당이다. 최초 구현 범위에서는 SO 에셋 값·씬·프리팹을 수정하지 않았으나, 이후 사용자 명시 요청으로 기본 SO 값을 적용하고 2026-11-01 맵 프리팹의 직교 투영을 명시했다. 오류 원인과 향후 함께 갱신할 설정은 [설정 안내 §2.1](../viewDocs/combat-camera-zoom-pose-guide.md#21-orthographic-명시-설정-후속-2026-11-01)을 기준으로 한다. [Task 기록](../plans/2026-10-31-combat-camera-zoom-pose-plan.md)과 [설정·확인표](../viewDocs/combat-camera-zoom-pose-guide.md)를 따른다.
