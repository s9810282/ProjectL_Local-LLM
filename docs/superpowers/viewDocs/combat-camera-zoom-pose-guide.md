# 전투 공용 카메라·줌 자세 설정 안내

작성: 2026-10-31. 갱신: **2026-11-15, 가운데 버튼 끌기 회전 추가 — §1.2**. 그 전: **2026-11-14, 카메라 회전(Z·C) 추가 — §1.2·§3**([회전 설계](../specs/2026-11-14-combat-camera-rotation-design.md)·[계획](../plans/2026-11-14-combat-camera-rotation-plan.md), 컴파일 미확인). 그 전: **2026-11-09, 씬 공용 카메라 Phase C 문서 인계**. MSBuild 재실행과 Unity Play 진입·종료 2회는 직접 확인했다. 현재 Combat 씬의 Binding 참조 누락을 복구했으며 화면 캡처 실패로 시각 동작 확인은 남아 있다.

설계는 [씬 공용 카메라 설계](../specs/2026-11-08-combat-scene-camera-design.md), 실행 상태와 **사용자 확인표의 원본**은 [Task 계획 §6](../plans/2026-11-08-combat-scene-camera-plan.md#6-사용자-unity-확인표)이다. 아래 §2.1과 §5.1은 이전 방식·시도 값의 이력이며 현재 설정 절차와 구분한다.

## 1. 현재 설정할 곳

| 대상 | 위치·설정 책임 |
|---|---|
| 전투 시점·원래 구도·렌즈·클립·Listener·후처리 | `Assets/ProjectL/Prefabs/Stage/CombatCamera.prefab` |
| 카메라와 조작 설정 연결 | 프리팹의 `CombatSceneCameraBinding`: **Virtual Camera**, **Settings** |
| 이동·회전·줌·줌 자세의 공용 값 | `Assets/ProjectL/DataBaseSO/ViewGroup/CombatCamera/CombatCameraSettings_Default.asset` |
| 운영 씬 참조 | `Assets/Scenes/Combat.unity`의 `CombatSceneRoot.sceneCamera` → 씬의 공용 Binding |
| 특성 확인 씬 참조 | `Assets/Scenes/Dev/TraitActivationPreview.unity`의 Host `sceneCamera` → 같은 프리팹의 Binding |
| 실제 화면 출력 | 기존 Boot의 유지 Unity Camera/Brain. 특성 확인 환경은 그 씬의 기존 출력 Camera/Brain |

각 씬에는 공용 가상 카메라 프리팹 인스턴스 하나를 둔다. 카메라는 배경·필드의 임시 표시 자식이 아닌 안정된 씬 루트에 배치한다. 두 씬 인스턴스의 프리팹 override도 확인한다. 현재 프리팹의 오브젝트 이름은 `CombatCamera`이며, 이름 선택은 Binding이 가리키는 실제 가상 카메라에서 읽는다.

StageViewProp의 옛 `cameraSettings`와 스테이지 편집기의 맵별 카메라 선택은 사용하지 않는다. 옛 YAML 값은 무시되는 이관 잔여값이므로 이를 지우기 위한 일괄 재직렬화는 필요 없다. 맵의 Day/Night 카메라는 생성된 배경 인스턴스에서만 비활성·고유 이름으로 바뀐다. 원본 맵 에셋은 보존한다.

### 1.1. 이동·줌 조정

1. 공용 Settings SO에서 **Key/Edge Pan Speed**, **Edge Thickness**, **Pan Min/Max**를 조정한다. 이동 제한은 원래 카메라 화면 축 기준 오프셋이다.
2. **Zoom Min/Max**, **Zoom Step/Seconds**를 조정한다. 작은 Size가 줌인이다.
3. **Use Zoom Pose**를 켜면 Size와 위치·회전을 연동하며 SO **ZoomMax** 구도로 시작한다. 프리팹의 원래 Size와 Max가 달라도 된다.
4. **Zoom Out Position/Rotation Offset**, **Zoom In Position/Rotation Offset**을 설정한다. 양 끝점 모두 같은 원래 카메라 자세 기준이며 줌아웃 값에 줌인 값을 누적하지 않는다.
5. 설정은 표시 연결마다 새 값으로 읽는다. SO 편집 후 재진입해 확인한다. 공유 SO의 변경은 운영과 특성 확인 환경 모두에 적용된다.

위치 오프셋 X/Y/Z는 원래 카메라의 오른쪽/위/전방 축이며 월드 축이 아니다. 회전은 원래 회전에 곱하는 로컬 Euler 도이고 런타임은 Quaternion으로 보간한다. 별도 줌인 마커는 필요 없다. 자세 adapter는 실행 시 연결되고 표시 해제 시 Unbind된다.

### 1.2. 회전 (2026-11-14)

1. 공용 Settings SO의 **Rotate Speed**(초당 도, 기본 90)를 조정한다. 0이면 회전을 끈다.
2. Z를 누르는 동안 카메라가 화면 가운데가 보는 바닥 점 둘레로 자기 왼쪽으로 돈다(화면 속 바닥은 시계 반대 방향으로 도는 것처럼 보인다). C는 반대다. 떼면 바로 멈추고, 둘 다 누르면 돌지 않는다.
3. 위 축(Y)으로만 돈다. 내려다보는 각·롤은 프리팹 그대로이고, 줌 자세의 줌인 각 변화(Zoom In Rotation Offset)도 그대로 남는다.
4. 회전 중심은 **지금 줌의 화면 가운데 바닥 점**이다. 줌인한 채 돌려도 화면 가운데가 제자리이고, 돌린 뒤 줌하면 줌 자세의 앞뒤 이동이 화면 기준 그대로(화면 위쪽으로 나아감)다. 구현은 자세를 줌아웃 중심 둘레로 돌리고, 돌릴 때마다 줌인 자세 때문에 생기는 어긋남만큼 이동 오프셋을 보정한다(`CombatCameraView.HoldCenterShift`). 이동 제한 끝에서는 제한이 먼저라 조금 밀릴 수 있다. 바닥 높이는 필드 Cells Root의 월드 높이다.
5. 이동 제한(Pan Min/Max)은 그대로 원래 카메라 화면 축 기준이라, 돌려도 볼 수 있는 **바닥 범위가 같다**(제한 사각형이 바닥에 고정). 돌린 뒤 WASD·가장자리 이동은 지금 화면 방향대로 간다.
6. 범위 제한은 없다(한 바퀴 이상 돌 수 있다). 전투를 나갔다 다시 들어오면 처음 방향이다.
   - **가운데 버튼(휠)을 누른 채 좌우로 끌어도 돈다**(2026-11-15, 회전 설계 CR8). 오른쪽으로 끌면 Z와 같은 방향(화면 아래쪽 바닥이 커서를 따라감)이다. 세기는 Settings SO의 **Rotate Drag Degrees**(화면 너비만큼 끌면 도는 각, 기본 180)이고, 음수면 반대 방향, 0이면 끈다. 끄는 동안은 화면 가장자리 이동과 커서 칸 스냅을 하지 않는다(값이 0이어도 누르는 동안은 멈춤). 회전 중심·보정은 Z·C와 같다(시점 `TurnBy` 하나).
7. 원래 카메라 회전에 롤이 있거나(오른쪽 축이 수평이 아님), 위아래가 뒤집혔거나(위 축이 아래를 향함), 바닥을 내려다보지 않으면 표시 연결 때 거절한다. 줌 자세 모드는 줌아웃 끝 자세의 회전으로 검사한다. 돌린 채 Play 중 Transform을 고쳐 바닥을 보지 않게 되면 그동안은 회전 없이 그린다(예외 없음).
8. 계산 원본은 `Combat/Presentation/Cameras/CombatCameraOrbit.cs` 하나다. 줌 자세 모드는 adapter가 계산한 자세에 회전만 더하고 Transform은 쓰지 않는다(Play 중 Transform 편집·흔들림 보정 그대로). 줌 자세를 끈 모드는 시점이 Transform 위치와 회전을 함께 쓴다.

## 2. 공용 카메라의 전제조건

- Binding과 VirtualCamera는 활성 상태이고 같은 로드된 씬에 있어야 한다. VirtualCamera는 Binding 자신 또는 자식에 있어야 한다.
- Settings 참조, 활성 CinemachineImpulseListener와 0이 아닌 채널이 필수다. 현재 프리팹 채널은 1이다.
- VirtualCamera의 **Lens Mode Override = Orthographic**을 명시한다. 출력 Camera도 기존 직교 구성을 유지한다.
- Follow/LookAt은 비우고 상위 VirtualCamera를 두지 않는다. 실제 동작하는 Body/Aim은 사용하지 않는다. 대상 없는 Transposer처럼 IsValid=false인 단계는 기존 검증에서 허용한다.
- ImpulseListener·CinemachineVolumeSettings·자동 줌 자세 adapter는 허용한다. 그 외 자세·렌즈를 쓰는 확장은 충돌 검토가 필요하다.
- 카메라 이름은 로드된 씬들의 가상 카메라 카탈로그 안에서 유일해야 한다. 같은 씬에 공용 프리팹을 중복 배치하지 않는다.

연결 누락·잘못된 구성은 카메라/필드 이름을 포함한 예외로 거절한다. Binding의 씬 소유·활성 검증은 실제 연결 시점의 조건이며, Project 창의 프리팹 에셋 자체를 실행 대상으로 삼는 검증은 아니다.

### 2.1. Orthographic 명시 설정 후속 (2026-11-01)

비활성 Night Camera에서 `Orthographic 카메라만 지원합니다` 예외가 발생하여, 사용자 요청으로 아래 맵 프리팹의 Day/Night 총 6개 VirtualCamera에 `Lens Mode Override = Orthographic`(`ModeOverride: 1`)을 명시했다.

- `Assets/ProjectL/Prefabs/Stage/MapProp/Stage.prefab`
- `Assets/ProjectL/Prefabs/Stage/MapProp/Stage_Test.prefab`
- `Assets/ProjectL/Prefabs/Stage/MapProp/Test/StageTest.prefab`

기존 `None`은 출력 Camera의 투영 방식을 상속하지만, 처음부터 비활성인 Night Camera는 Cinemachine의 상속 상태가 초기화되지 않아 `m_Lens.Orthographic`이 false로 읽힐 수 있다. 비활성 카메라도 맵 수집·검증 대상이므로 진입 검증에서 거절되며, 기준 렌즈 캡처도 OrthographicSize 대신 FieldOfView를 고를 수 있다. 검증을 우회하지 않고 프리팹에 직교 투영을 명시해 초기화 순서 의존성을 없앴다. Night Camera의 비활성 상태는 유지했다.

**당시 맵 카메라 방식에서 투영 방식이나 맵 카메라 구성을 변경할 때는 출력 Camera와 대상 맵 프리팹의 Day/Night Lens Mode Override를 함께 확인·변경하고, 이 안내도 함께 갱신해야 한다.** 프리팹을 추가·교체할 때도 비활성 카메라를 포함해 같은 명시 설정을 적용한다. 원근 투영 또는 None 상속으로 바꾸려면 설정만 바꾸지 말고 줌 자세의 직교 전용 검증, 기준 렌즈 캡처, 줌 적용 경로의 호환성을 함께 검토·수정해야 한다. SO의 Size·위치·회전 오프셋 조정만으로 프리팹의 투영 설정을 바꿀 필요는 없다.

이번 변경은 프리팹의 ModeOverride 6곳뿐이며 diff 검사를 통과했다. Unity 재생 확인은 대기 중이다.

**2026-11-09 현재 적용 범위:** 위 기록은 맵의 Day/Night를 전투 시점으로 사용하던 때의 조치다. 현재 전투는 씬의 공용 VirtualCamera에 직교 투영을 명시하며, 원본 맵의 Day/Night 수정·수집을 진입 조건으로 삼지 않는다. 직교 줌 자세의 투영 검증 자체는 유지한다.

## 3. 기대 동작과 오류

- UseZoomPose=true이면 시작 Size는 SO ZoomMax다. 현재는 **12**이며 프리팹 원래 Size **25.7**을 그대로 화면에 쓰는 것이 아니다.
- 팬 없이 줌아웃하면 SO의 줌아웃 구도로 돌아간다. 팬 이동이 있었다면 그 오프셋은 유지된다.
- 연출 줌도 실제 렌즈에 따른 같은 자세 경로를 사용한다. 범위 밖 연출 Size에서는 자세만 끝점에 고정한다. 연출 줌 대여 중 새 사용자 줌은 반영하지 않는다.
- 표시 해제 시 Session이 자세 adapter를 끊고 캡처한 원래 Transform·렌즈·Priority를 복원한 뒤 창구·대여를 해제한다. 재진입은 새 연결의 초기 구도로 시작한다.
- UseZoomPose=false이면 Size만 바꾼다. 공용 Binding의 직교·추적·extension·Listener 조건은 계속 적용된다.
- 자세 모드에서 Min=Max, 유효하지 않은 오프셋, 추적/extension 충돌은 거절한다.
- 이미 발행한 흔들림은 원래 Duration까지 자연 종료한다. Stop 직후 같은 씬에서 다시 시작할 때 남은 짧은 신호와 새 요청이 계속 생기는 문제를 구분한다.
- HUD·칸 집기·고스트는 기존 출력 Camera를 사용한다. 이동·줌·흔들림 중 표시와 포인터가 함께 맞는지 확인한다.
- 줌 자세의 회전 오프셋 때문에 줌 정도에 따라 화면 중앙의 지면 교점이 변할 수 있다. 구도 조정은 줌 위치와 회전을 함께 본다. 맵별 자동 맞춤·override는 현재 범위 밖이다.
- Z·C 회전(§1.2) 중에도 HUD·칸 집기·흔들림(카메라 공간)은 화면 기준으로 맞아야 한다. 돌아가는 동안 마우스 아래 칸을 다시 집는다. 회전은 연출 줌·흔들림 중에도 받고, 확인창·결과 화면에서는 막힌다.
- 방위각이 0이면(돌리지 않음) 회전 전과 같은 자세가 나온다. 돌린 화면에서 배경 뒷면·잘린 가장자리·그림자 방향·가까운 자름면은 아트 확인 대상이다.
- 연속 휠은 기존 렌즈 효과의 From=이전 목표 정책을 유지한다. 중간 Size가 순간 이동하는 느낌의 개선은 별도 범위다.
- 자동 낮/밤 화면 전환은 비활성 그대로다. 맵의 Day/Night가 없어도 공용 카메라 조작은 가능해야 한다.

## 4. Unity에서 확인할 순서

**세부 절차·정상 기준·결과 칸은 [Task 계획 §6](../plans/2026-11-08-combat-scene-camera-plan.md#6-사용자-unity-확인표) 한 곳에 모았다.** 기본 확인 U01~U10을 먼저 하고 추가 확인 E01~E03을 이어서 진행한다. 직접 확인한 실행 진입/종료 부분과 남은 미확인을 확인표에서 구분한다.

문제가 있으면 확인 ID, 스테이지 키/배경, 직전 조작, 기대 동작과 실제 동작, 첫 Console 예외와 전체 stack trace를 기록한다. 기존 시험 실패는 시험 이름·실패 내용을 함께 기록한다. 시각 확인은 MSBuild 성공으로 대신하지 않는다.

## 5. 값의 출처와 현재 기본값

### 5.1. 이전 시도용 값 기록 (2026-10-31, 현재 기본값 아님)

- Use Zoom Pose: 켜짐.
- Zoom Min=4, Max=8, Step=1, Seconds=0.2.
- Zoom Out Position/Rotation Offset: 모두 (0, 0, 0).
- Zoom In Position Offset: (0, -3, 0.1), 원래 카메라 로컬 축 기준.
- Zoom In Rotation Offset: (-5, 0, 0)도.
- 기존 Pan 설정 유지.

확인한 기본 프랍의 Size 8·회전 (45, 45, 0)을 기준으로 줌아웃 구도를 유지하고, 줌인 시 조금 낮고 완만한 각도로 이동하도록 잡았다. 위치 이동은 회전에 따른 중심 이동을 줄이는 시작값이며 자동 중심 고정은 아니다. 공유 SO를 참조하는 모든 맵에 적용된다. 에디터 시각·클립·포인터·흔들림 확인 전의 조정 출발점이다. 시도용 SO 값 적용 당시에는 씬·프리팹을 변경하지 않았으며, 이후 프리팹 투영 명시 설정은 §2.1에 기록했다.

### 5.2. 현재 공용 값 (2026-11-09 파일 확인)

| 값 | 현재 설정 |
|---|---|
| Use Zoom Pose | 켜짐 |
| Zoom Min / Max / Step / Seconds | 4 / 12 / 1 / 0.2초 |
| Zoom Out Position / Rotation Offset | 모두 (0,0,0) |
| Zoom In Position / Rotation Offset | (0,-3,0.1) / (-10,0,0)° |
| Key / Edge Pan Speed | 10 / 10 |
| Edge Thickness | 0.02 |
| Pan Min / Max | (-12,-8) / (12,8) |
| 공용 프리팹 원래 위치 / 회전 | (14.3025,19.95,-13.2525) / (45,-45,0)° |
| 프리팹 원래 Size / Near / Far | 25.7 / 0.35 / 50 |

기준 구도는 스테이지 2201의 13×13 필드·타일 정렬 결과를 필드 좌표계로 환산한 Phase B 값이다. 실제 보드 가시성·가림·클립·줌 끝점의 시각 확인과 최종 조정은 사용자 결과 대기다. 이 표를 갱신하려고 이번 Phase C에서 SO·프리팹 값을 변경하지 않았다.
