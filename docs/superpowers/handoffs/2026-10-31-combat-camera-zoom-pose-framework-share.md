# 전투 카메라 줌 자세 — Factory 공유 메모

작성: 2026-10-31. 대상: 민형님. 외부 전달 미실시.

## 변경

`CombatSceneDisplayFactory`의 기존 `cameraView.Begin()`을 HUD 조립 후 기존 try 안으로 이동했다. 표시 생성 실패 시 `cameraView.Restore()`도 호출하고, HUD 정리와 함께 CleanupErrors로 오류를 모은다. 자세 어댑터가 표시 연결 수명 밖에 남지 않게 하기 위한 변경이다. Factory는 생성·연결·실패 정리만 담당한다.

## 계약·소유 경계

카메라 Size는 기존 `ICameraEffectService.Dispatch` 경로 그대로다. View의 `CombatCameraZoomPose`가 공개 CinemachineExtension API로 실제 Lens Size에 맞춰 기초 자세를 적용한다. Impulse correction은 유지한다. Framework/Render/FieldCamera/Architecture/Core, Builder·서비스 등록·Export·조회 계약 변경은 없다. 신규 전역 서비스도 없다.

생성 주체는 CombatSceneDisplayFactory, 소유자는 표시 연결의 CombatCameraView, 자세 해제는 Display.Release의 기존 CameraView.Restore다. 일반/연출 Size의 소유자는 기존 CombatCameraWindow이며 소비 서비스는 기존 Global 서비스 문맥에서 받은 CameraEffects다. 루트 재등록·직접 resolver 조회를 추가하지 않았다.

[설계](../specs/2026-10-31-combat-camera-zoom-pose-design.md) · [구현·검증 기록](../plans/2026-10-31-combat-camera-zoom-pose-plan.md) · [사용자 연결 안내](../viewDocs/combat-camera-zoom-pose-guide.md)

## 사용자 요청 후속: 설정 SO 통합

마커 authoring은 제거하고 CombatCameraSettingsSO 한 곳에 사용 여부와 양 끝점 상대 위치·회전을 모았다. CameraView가 표시 연결 때 필요한 adapter를 자동 추가/재사용하고 해제 때 Unbind한다. Factory의 추가 계약 변경은 없다. 어댑터에는 직렬화 설정이 없고 배경 수명에 함께 정리된다. 자세 모드의 시작 Size는 SO ZoomMax이며 해제 렌즈는 원래 캡처값이다.
