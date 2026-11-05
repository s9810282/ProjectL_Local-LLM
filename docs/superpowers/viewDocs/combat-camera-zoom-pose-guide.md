# 전투 카메라 줌 자세 설정 안내

작성: 2026-10-31. 최신: 사용자 요청으로 모든 설정을 CombatCameraSettingsSO로 통합했다. 이전 줌인 마커 연결 절차는 폐기했다. Unity 확인 대기.

## 1. 설정할 곳

해당 스테이지의 StageViewPropData.cameraSettings가 참조하는 **CombatCameraSettings_Default**(또는 해당 맵용 설정 SO)를 연다. 같은 SO를 여러 맵이 참조하면 변경이 모두 적용된다.

1. **Use Zoom Pose**를 켠다. 새 기능의 직렬화 기본값은 꺼짐이다. CombatCameraSettings_Default에는 사용자 요청으로 아래 시도용 값을 적용했다.
2. **Zoom Min / Max**로 Size 양 끝점을 정한다. 예: Min=4, Max=8이면 Size 8로 시작하고 Size 6이 자세 중간점이다. 카메라 프리팹 초기 Size를 Max와 맞출 필요는 없다.
3. **Zoom Out Position Offset / Rotation Offset**을 설정한다. 둘 다 0이면 기존 카메라 구도로 줌아웃한다.
4. **Zoom In Position Offset / Rotation Offset**을 설정한다. 양 끝점 모두 원래 카메라 구도 기준이다. 줌아웃 값에 더하는 값이 아니다.
5. **Zoom Step / Seconds** 및 기존 Pan 설정도 같은 SO에서 조정한다.

위치 오프셋 X/Y/Z는 원래 카메라의 오른쪽/위/전방 축이다. 월드 X/Y/Z가 아니다. 회전은 원래 회전에 곱할 로컬 Euler 도이며 런타임 보간은 Quaternion이다. Day/Night는 각자의 원래 구도에 같은 값을 적용한다.

별도 마커·카메라 컴포넌트 수동 추가·카메라별 SO 연결은 필요 없다. adapter는 실행 시 자동으로 붙고 표시 해제 시 연결이 끊긴다. 이전에 마커를 만들었다면 참조하지 않으므로 제거해도 된다. 기존 adapter를 수동으로 붙여 두었어도 활성 상태라면 재사용한다.

설정은 표시 연결 시 새 값으로 읽는다. 실행 중 SO를 바꿔도 현재 연결에 실시간 반영하지 않으므로 재진입해서 확인한다.

## 2. 카메라 자체의 전제조건

이것은 줌 설정이 아니라 pipeline 구성 조건이다. 출력 Camera는 Orthographic, 각 VirtualCamera의 Lens Mode Override는 Orthographic으로 설정한다. Follow/LookAt은 비우고 Body/Aim은 Do Nothing을 권장하지만, Follow가 없는 Transposer처럼 IsValid=false여서 실제 동작하지 않는 단계는 유지할 수 있다. 활성 여부(enabled)만으로 거절하지 않는다. 부모 VirtualCamera는 허용하지 않는다. ImpulseListener와 CinemachineVolumeSettings(후처리·초점)는 유지할 수 있으며 다른 활성 CinemachineExtension은 충돌 검토 전 거절한다.

### 2.1. Orthographic 명시 설정 후속 (2026-11-01)

비활성 Night Camera에서 `Orthographic 카메라만 지원합니다` 예외가 발생하여, 사용자 요청으로 아래 맵 프리팹의 Day/Night 총 6개 VirtualCamera에 `Lens Mode Override = Orthographic`(`ModeOverride: 1`)을 명시했다.

- `Assets/ProjectL/Prefabs/Stage/MapProp/Stage.prefab`
- `Assets/ProjectL/Prefabs/Stage/MapProp/Stage_Test.prefab`
- `Assets/ProjectL/Prefabs/Stage/MapProp/Test/StageTest.prefab`

기존 `None`은 출력 Camera의 투영 방식을 상속하지만, 처음부터 비활성인 Night Camera는 Cinemachine의 상속 상태가 초기화되지 않아 `m_Lens.Orthographic`이 false로 읽힐 수 있다. 비활성 카메라도 맵 수집·검증 대상이므로 진입 검증에서 거절되며, 기준 렌즈 캡처도 OrthographicSize 대신 FieldOfView를 고를 수 있다. 검증을 우회하지 않고 프리팹에 직교 투영을 명시해 초기화 순서 의존성을 없앴다. Night Camera의 비활성 상태는 유지했다.

**향후 투영 방식이나 맵 카메라 구성을 변경할 때는 출력 Camera와 대상 맵 프리팹의 Day/Night Lens Mode Override를 함께 확인·변경하고, 이 안내도 함께 갱신해야 한다.** 프리팹을 추가·교체할 때도 비활성 카메라를 포함해 같은 명시 설정을 적용한다. 원근 투영 또는 None 상속으로 바꾸려면 설정만 바꾸지 말고 줌 자세의 직교 전용 검증, 기준 렌즈 캡처, 줌 적용 경로의 호환성을 함께 검토·수정해야 한다. SO의 Size·위치·회전 오프셋 조정만으로 프리팹의 투영 설정을 바꿀 필요는 없다.

이번 변경은 프리팹의 ModeOverride 6곳뿐이며 diff 검사를 통과했다. Unity 재생 확인은 대기 중이다.

## 3. 기대 동작과 오류

- 시작은 SO ZoomMax 구도. 줌인/아웃은 Size와 위치·회전이 함께 변한다.
- 팬 없이 줌아웃하면 SO의 줌아웃 구도로 복귀한다. 팬 이동을 했다면 그 팬 오프셋은 남는다.
- 연출 줌도 동일 경로를 따른다. 범위 밖의 연출 Size에서 자세만 끝점에 고정한다.
- 전투 해제 시 캡처한 원래 Transform·렌즈를 복구한다. SO의 줌아웃 오프셋이 0이 아니어도 해제 복원 대상은 원래 프리팹 구도다.
- Use Zoom Pose를 끄면 기존 Size 전용 줌으로 돌아간다.
- Min=Max는 자세 모드에서 오류다. NaN/Infinity 오프셋, 투영·추적·extension 충돌도 원인/카메라 이름을 포함해 거절한다.
- HUD는 최종 출력 카메라를 그대로 읽으므로 추가 연결이 없다.
- 회전으로 화면 중앙의 지면 교점이 바뀔 수 있다. 원하는 대상이 중앙에 남도록 줌인 위치와 회전을 함께 조정한다.
- 연속 휠의 기존 렌즈 효과는 이전 목표 Size를 From으로 쓴다. 이때 기존 Size의 순간 이동이 보일 수 있으며 자세도 그 실제 Size를 따른다. 이 정책 개선은 별도 범위다.

## 4. 사용자 확인표

모두 미확인. 사용자 확인 후 완료로 기록한다.

- [ ] EditMode CombatCameraZoomPoseTests 35건 통과.
- [ ] SO 하나에서 줌인·줌아웃 위치·회전·Size 조정 가능, 마커 없이 동작.
- [ ] 프리팹 초기 Size가 달라도 SO Max 구도로 시작.
- [ ] 양 끝점·중간 직선 위치·최단 회전, 반복 복귀 정상.
- [ ] 연속 휠·역방향·0초 전환에서 세 값 동기화.
- [ ] WASD/가장자리 이동 후 줌과 제한 정상.
- [ ] Day/Night 각각 원래 구도 기준 오프셋 적용·blend 정상.
- [ ] 연출 줌·복원·범위 밖 Size·대여 중 휠 거절 정상.
- [ ] 줌과 흔들림 동시 실행 정상.
- [ ] HUD·유닛/칸 클릭·지면 투영 정상.
- [ ] 회전 시 가림·Near/Far Clip·배경 노출 정상.
- [ ] 중도 해제·재진입·재사용에서 원래 구도와 렌즈 복원.
- [ ] Use Zoom Pose 꺼짐의 기존 Size 전용 줌 정상.

## 5. 시도용 기본 에셋 값 (사용자 요청 반영)

- Use Zoom Pose: 켜짐.
- Zoom Min=4, Max=8, Step=1, Seconds=0.2.
- Zoom Out Position/Rotation Offset: 모두 (0, 0, 0).
- Zoom In Position Offset: (0, -3, 0.1), 원래 카메라 로컬 축 기준.
- Zoom In Rotation Offset: (-5, 0, 0)도.
- 기존 Pan 설정 유지.

확인한 기본 프랍의 Size 8·회전 (45, 45, 0)을 기준으로 줌아웃 구도를 유지하고, 줌인 시 조금 낮고 완만한 각도로 이동하도록 잡았다. 위치 이동은 회전에 따른 중심 이동을 줄이는 시작값이며 자동 중심 고정은 아니다. 공유 SO를 참조하는 모든 맵에 적용된다. 에디터 시각·클립·포인터·흔들림 확인 전의 조정 출발점이다. 시도용 SO 값 적용 당시에는 씬·프리팹을 변경하지 않았으며, 이후 프리팹 투영 명시 설정은 §2.1에 기록했다.
