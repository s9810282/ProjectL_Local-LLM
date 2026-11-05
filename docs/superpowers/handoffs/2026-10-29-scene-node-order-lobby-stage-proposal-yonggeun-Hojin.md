# 로비·스테이지 씬 노드 순서를 SceneNodeOrder 표에 — 용근님 제안

- 작성자: 호진(이호진)
- 상태: 미전달·미합의. 코드 변경 없음(제안만).
- 출처: [검사 후속 정리 계획 §6.9](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#69-phase-r-f--묶음-8-작은-정리-묶음-7-요청) R-f 묶음 7, 지적 원본은 [설계 §1.2](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md#12-10-27-점수의-지적-목록) F3·F6.
- 대상: 용근님 파일 `RuntimeSystem/ErrorHandling/SceneNodeOrder.cs`(골격 점검 K-4), `ServiceKeys/Runtime/LifeCycle/LifeCycleSystemServiceKey.cs`의 한 줄(`38869ec1`).

## 1. 씬 노드 순서 (F3)

지금 `SceneNodeOrder`는 Bootstrap 씬 연결과 Combat 씬 시스템의 노드만 담고, 로비·스테이지 시스템의 자기 노드는 각 시스템의 상수입니다(표의 remarks 그대로).

| 노드 | 지금 상수 | 값 | 표 기준으로 쓰면 |
|---|---|---|---|
| 로비·스테이지 이탈 준비 | `LobbyLifeCycle.ExitPreparationPriority`, `StageLifeCycle.ExitPreparationPriority` | −200 | `LifeCycle − 200` |
| 로비·스테이지 View 연결 | `LobbyViewSystem.AttachPriority`, `StageMapViewSystem.AttachPriority` | 100 | `LifeCycle + 100`(전투 `CombatViewAttach`와 같은 값) |
| 로비·스테이지 View 해제 | `LobbyViewSystem.DetachPriority`, `StageMapViewSystem.DetachPriority` | −100 | `LifeCycle − 100`(전투 `CombatViewRelease`와 같은 값) |
| 카메라 핸들 빌리기(AfterLoad) | `LobbyViewSystem.CameraHandlePriority`, `StageMapViewSystem.CameraHandlePriority` | 100 | AfterLoad 단계. 카메라 카탈로그 재수집(10) 뒤 |

- 같은 값이 두 시스템에 따로 있고, 전투 쪽 같은 뜻의 값과도 따로라 기준(`LifeCycle`)을 옮기면 함께 따라가지 않습니다.
- 제안: 표에 위 넷을 더해 주시면(이름은 용근님이 정함), 호진 쪽 네 시스템이 표 값을 읽도록 바꾸겠습니다. 표에 넣지 않기로 하면 지금처럼 각 시스템 상수로 두고, 표 remarks만 그대로 둡니다.
- 값은 바꾸지 않습니다. 순서도 그대로입니다.

## 2. 쓰지 않는 서비스 키 (F6)

- `LifeCycleSystemServiceKey.DeckEditAvailability`(`38869ec1`)로 등록·조회하는 곳이 없습니다. 덱 편집 가능 시점은 `LifeCycleSystemFactory`가 형식(`IDeckEditAvailabilityRead`)으로 등록하고 Core 조립에 넘깁니다.
- 걷어도 되는지 정해 주세요. 호진 쪽은 고치지 않았습니다.
