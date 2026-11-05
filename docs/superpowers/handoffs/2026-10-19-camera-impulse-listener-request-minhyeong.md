# 전투 맵 카메라 흔들림 수신기 알림 (민형님)

- 작성: 2026-10-19, View(용근) 쪽. **작성 완료, 외부 전달 전.**
- 관련: [카메라 연출 조각 설계](../specs/2026-10-19-combat-camera-pieces-design.md) K7·K8, [계획](../plans/2026-10-19-combat-camera-pieces-plan.md).
- **바뀜(2026-10-19):** 처음엔 요청이었지만, 용근이 직접 `Stage.prefab`의 Day·Night Camera에 Listener를 붙였습니다(커밋 `339efa14`, 채널 1·세기 1·카메라 공간·Noise 단계 뒤). 이 문서는 **알림**입니다. 프리팹을 고치실 때 두 카메라의 Listener를 남겨 주세요.

## 1. 붙인 것 (처음 요청 내용)

`Assets/ProjectL/Prefabs/Stage/MapProp/Stage.prefab`의 `Day Camera`·`Night Camera`에 `CinemachineImpulseListener`를 붙였습니다.

- 채널은 기본값(1)이면 됩니다. 다른 채널을 쓰려면 두 카메라를 같은 값으로 맞춰 주세요. View 쪽은 이 값을 프리팹에서 읽어 흔들림을 보냅니다(채널 값을 코드에 따로 적지 않습니다).
- 이 프리팹은 모든 전투 스테이지(1101~1110, 2101)의 맵 프랍입니다.
- **추가(2026-10-19):** 두 Listener의 Reaction Settings → Secondary Noise에 노이즈 프로필 `Assets/ProjectL/Prefabs/Stage/MapProp/MapCameraShakeNoise.asset`을 넣습니다. 우리 흔들림은 한 축(화면 위아래)이라 좌우 떨림을 더하려는 것입니다.
  - 처음엔 Cinemachine 기본 `6D Shake`를 넣었습니다. 그런데 좌우 위치 진폭(0.022)이 위아래(0.149)의 약 1/7이라 좌우가 보이지 않았습니다.
  - 그래서 위치 X·Y만 같은 진폭(옥타브 0.7 + 0.3, Perlin)이고 Z·회전은 없는 프로필을 따로 만들었습니다.
  - 세기는 Listener의 Amplitude Gain으로 맞춥니다. 프리팹을 고치실 때 이 참조도 남겨 주세요.

## 2. 이유

- 전투 연출(포효 박치기 타격 순간)에서 `CameraEffectSystem.Dispatch(CameraShakeData)`로 흔들림을 보내려고 합니다.
- 카메라 효과 문서대로, 흔들림은 같은 채널의 Listener가 붙은 버추얼 카메라만 받습니다. 지금 두 카메라에는 `CinemachineVolumeSettings`만 있고 Listener가 없어서, 보내도 화면이 흔들리지 않습니다.

## 3. View 쪽에서 하는 것 (Framework 코드는 바꾸지 않음)

- `CameraEffectSystem`, 두 카탈로그, `DelayService`는 수정하지 않고 공개 `Dispatch`만 씁니다.
- 줌은 Day·Night 두 카메라에 같은 배율로 보냅니다. 기준 크기는 맵 프랍을 세울 때 버추얼 카메라 렌즈에서 읽습니다(카탈로그 문서의 "가진 참조에서 설정 복사" 방식).
- 줌의 끝·취소·되돌리기는 View 쪽이 감쌉니다. 끝은 자기 시계로 세고, 취소·되돌리기는 기준값 0초 줌을 보냅니다.
- Listener가 붙기 전에는 흔들림 항목을 넣지 않습니다. 붙으면 View 코드를 바꾸지 않아도 흔들림이 나옵니다.

## 4. 참고 (요청 아님)

View 쪽에서 감싸느라 생긴 점입니다. 효과 쪽에 같은 기능이 생기면 View 쪽 감싸기를 줄일 수 있습니다.

- `Dispatch`에 끝 알림이 없어 조각이 자기 시계로 끝을 셉니다. 효과의 `DelayService` 시계와 최대 한 프레임 어긋날 수 있습니다.
- 줌을 개별 취소할 수 없어, 같은 카메라에 기준값 0초 줌을 보내 덮어씁니다.
- 핸들·효과에 지금 렌즈 값 조회가 없어, 기준값을 프리팹 렌즈에서 읽어 둡니다.
