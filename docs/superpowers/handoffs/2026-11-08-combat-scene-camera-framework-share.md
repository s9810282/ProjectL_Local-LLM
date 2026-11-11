# Combat 씬 공용 카메라·설정 이관 — Framework/View 공유 (2026-11-09 갱신)

- 상태: Phase A·B 구현과 당시 MSBuild 성공, Phase C 문서·인계 완료. 직접 Unity Play 진입·종료 확인, 시각·전체 회귀 확인 대기. 외부 미전달.
- 설계: [씬 공용 카메라](../specs/2026-11-08-combat-scene-camera-design.md).
- 실제 변경·검증·미확인 원본: [Task 계획 §8](../plans/2026-11-08-combat-scene-camera-plan.md#8-실행-기록).

## 연결 변경

Combat 씬과 특성 확인 씬은 `Assets/ProjectL/Prefabs/Stage/CombatCamera.prefab`의 가상 카메라 하나를 사용한다. 기존 Boot의 유지 출력 Camera/Brain과 확인 씬의 출력은 유지한다. `CombatSceneRoot.sceneCamera` 또는 TraitActivationPreviewHost의 같은 필드가 필수다.

기존 `CombatCameraSettings_Default`는 공용 프리팹의 Binding에 연결한다. 이동·줌·줌 자세·Impulse 설정은 기존 값을 유지한다. 맵 프랍의 cameraSettings와 스테이지 편집기의 카메라 선택은 더 이상 사용하지 않는다. 옛 StageViewProp YAML을 지우려고 전체 재직렬화할 필요는 없다.

## 조립·서비스·수명

- 새 CameraFactory가 명시 Binding과 기존 Catalog/Effects로 Session을 조립한다. 운영·특성 확인이 같은 경로를 쓴다.
- Catalog/Effects는 기존 Global → Render → CameraGroup에서 CombatDisplayServices로 제공한다. 기존 CameraGroupFactory·Builder·등록·Export는 변경하지 않았다.
- Session이 선택·입력 연결·자세/렌즈/Priority 복원·줌 대여 해제를 소유한다. 카탈로그 Handle을 씬 재수집 사이에 보관하지 않는다.
- 배경 생성 직후 인스턴스의 옛 가상 카메라/Listener만 끄고 고유 이름을 부여한다. 원본 맵과 GameObject 전체는 보존한다. 기존 배경 생성/카메라 수집 Node 순서는 유지한다.
- 자동 아침 전환은 비활성 그대로다. 전환 요청값의 대상 이름만 씬 Binding에서 받도록 정리했다. Render의 전환 예제·고정 레이어는 변경하지 않았다.
- 이미 발행한 흔들림은 원래 Duration까지 자연 종료하는 기존 계약을 유지한다. 개별 취소나 전역 Clear를 추가하지 않았다.

## 확인 요청

확인 순서·정상 기준·결과의 원본은 [Task 계획 §6](../plans/2026-11-08-combat-scene-camera-plan.md#6-사용자-unity-확인표)의 U01~U10·E01~E03이다. 설정 위치·현재 기본값은 [사용 안내](../viewDocs/combat-camera-zoom-pose-guide.md)를 따른다. 현재 공용 가상 카메라의 이름은 `CombatCamera`이며 Binding의 실제 참조에서 읽는다.

실제 첫 구도·보드 가시성·포인터/HUD 투영, WASD/가장자리·휠·연출 줌·흔들림, 이탈/재진입, 특성 확인 반복 재생과 스테이지 편집 저장을 Unity에서 확인한다. DB 설정 에셋 변경에 따른 기존 DataBaseAssetTests도 사용자 확인 항목이다. 새 Test 코드는 작성하지 않았다.

2026-11-09에는 문서만 갱신했다. Phase B의 빌드 성공은 당시 기록이며 이번에 MSBuild·Unity·Test Runner를 다시 실행하지 않았다.


2026-11-09 직접 검증 후속: 현재 Combat 씬의 Root.sceneCamera 누락을 기존 공용 인스턴스로 복구했다. MSBuild는 오류 0·경고 15로 성공했고 Unity Play 진입·종료 2회에 새 예외가 없었다. 컴퓨터 사용 화면 캡처가 실패해 실제 화면의 이동·줌·흔들림·HUD·구도는 아직 미확인이다. 위 문서만 갱신했던 기록 이후의 명시 요청으로 수행한 후속이며 세부 결과는 Task 계획 §8을 따른다.
