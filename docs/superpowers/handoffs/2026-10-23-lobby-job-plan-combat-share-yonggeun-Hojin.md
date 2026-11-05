# 로비 실행 계층 Job·Plan 재구성 — 용근님 전투 공유 (2026-10-23)

- 작성자: 호진(이호진)
- **받는 사람:** 용근님(전투 View·전투 골격)
- **보내는 쪽:** View 작업(로비 담당)
- **상태:** 작성 완료·미전달. 사용자가 직접 전달한다.
- **코드 기준:** Phase 0~1 `e46398d4`, Phase 2~4는 이 문서를 쓸 때 커밋 전.
- **원본:** [로비 설계](../specs/2026-10-11-lobby-design-Hojin.md)의 Job·Plan 재구성 절(옛 재구성 설계, §2가 전투 개선안과의 관계), [로비 계획](../plans/2026-10-11-lobby-plan-Hojin.md)(옛 Task·실행 기록). 둘은 2026-10-27에 옛 재구성 설계·계획을 합친 문서이며 § 번호는 합치기 전 판 기준이다(옛 판은 git `10914570`). 같은 날 [민형님 Framework 공유](2026-10-23-lobby-job-plan-framework-share-minhyeong-Hojin.md)도 있다.

## 1. 무엇이 바뀌었나

로비를 전투와 같은 흐름으로 바꿨다. `LobbyLifeCycle`은 Runner가 비면 `Register(Lobby, Next)`만 걸고, 판단은 `LobbyPlan.Decide` 한 곳, 이탈 조건은 `LobbyPlan.CanLeave` 한 곳, Core 호출과 호출 직후 확인은 Job이다. 옛 `LobbyFlow`·`LobbyDepartureFlow`·`ArtifactEditFlow`는 지웠다. 출발은 `[LobbyRunStartJob, LobbyTransitionJob]` 연쇄이고, Stage 전환은 마지막 Job 안에서 한다(전투 J7과 같은 방식).

용근님 전투 View 구조 개선안 2판(원격 `feature/Yonggeun/Refector-Stage`의 `9575001`, `docs/superpowers/specs/2026-10-22-combat-view-structure-improvement-design.md`)을 기준 형태로 대조했다. **흐름은 같고, 디테일은 로비에 맞게 줄였다**(사용자 지시 "디테일까지 같을 필요는 없다").

| 전투 개선안의 디테일 | 로비 | 이유 |
|---|---|---|
| 실행 순간 판단(`CombatNextJob`), JobRegister Combat 줄이 Runner를 넘김 | 두지 않음. 등록 때 판단해 단계 Job을 바로 건다. 로비 줄은 Runner를 넘기지 않는다 | 등록과 실행 사이 한 프레임 틈이 로비에서는 결과를 바꾸지 않는다(설계 §4.2) |
| 걸음 재료 `CombatStepContext` | 두지 않음. `LobbyPlan`이 Job마다 자기 서비스만 생성자로 넘긴다 | 로비 Job은 넷뿐이다. "Job은 자기 서비스만 받는다"는 같다 |
| 이탈은 사후 확인 Job(`ExitCheck`) | 사전 검사 `CanLeave`(-200 이탈 노드) | 남은 편집을 버리는 이탈을 먼저 막는다 |
| 진입 요청에 이름 있는 질문·단계 전이 표 하나(개선안 C) | **따랐다.** 출발 요청에 질문(`IsPending`·`CanResubmit`·`IsRejected`·`IsFaulted`)과 전이 표 하나, `Queued`·`IsJobCompleted` 삭제 | 로비 View는 질문만 읽는다 |
| 사후 확인은 "다음 판단이 기대는 사실만"(개선안 A·P15) | **같은 방향.** 확인은 호출 직후 같은 Job 안, 그 호출의 계약만(새 Run 결과와 게시된 로스터 대조 포함) | 오류가 원인 자리에서 throw되게 |

## 2. 전투 쪽이 기댈 수 있는 것

- **로비 진입 때 Runner가 바빠도 예외가 아니다(설계 B5).** 옛 로비는 진입 때 Runner가 비어 있지 않으면 예외였다(전투 분석 P6의 "Lobby는 같은 상황에서 예외"). 이제는 빌 때까지 다음 판단을 걸지 않을 뿐이다. 전투 이탈 연쇄의 Job 개수·순서를 바꿔도 로비 진입이 깨지지 않는다.
- **로비 이탈 검사는 Runner를 보지 않는다(B4).** 로비 자기 사실(처리 안 된 아티팩트 편집, 진행 중인 출발)만 본다. 다른 루트의 Job은 그 루트가 책임진다.
- **로비 출발은 조건 없이 새 Run을 만든다(설계 §3.2 약속 2).** 개선안 B("전투 이탈 연쇄에서 새 Run 빼기 → Run 종료 뒤 새 Run은 로비 출발 한 곳") 뒤에는 이것이 Run 종료 흐름의 전제가 된다. 로비 쪽은 이 약속을 바꾸지 않았고, 출발에 "Run 진행 중이면 새 Run 생략" 같은 조건을 더하지 않는다. `LobbyDepartureIntegrationTests`의 "로비 복귀 뒤 다음 출발이 새 Run을 한 번 더 만든다" 사례가 이것을 확인한다.
- 남은 전투가 있으면 로비 새 Run Job이 Core 호출 전에 throw한다(공용 오류 경계로, 옛 Flow의 등록 전 예외와 같은 결과).

## 3. 겹치는 파일 (병합 때)

용근님 `feature/Yonggeun/Refector-Stage`(1차 `44c4cdc2`, 이 문서를 쓸 때 원격 끝 `4c328d07`)와 겹치는 코드 파일은 셋이다. 줄이 달라 손으로 합칠 수 있다.

| 파일 | 전투 쪽 | 로비 쪽 |
|---|---|---|
| `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | `CreateComposition`의 오류 경계를 필수로 | 같은 메서드에 `LobbyArtifactDomain`·`LobbyProgress` 생성·등록 줄, `new LobbyLifeCycle(faults, lobbyProgress, lobbyArtifacts)` |
| `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | Combat 줄이 `_runner`도 넘김 | Lobby 줄이 `_lifeCycles`도 넘김, `_lifeCycles` 필드 주석 한 줄, Artifact Plan `using` 경로 |
| `Tests/EditMode/Framework/LobbyDepartureIntegrationTests.cs` | `ICombatViewService` 가짜 컴파일 맞춤 | `StageLoadObservedIdleRunner` 단언 삭제(전환이 Job 안이라 Stage 로드 때 Runner가 비어 있지 않음), `IsJobCompleted` 단언 둘 삭제 |

Core 전달 문서(`2026-09-24-view-core-data-boundary-handoff.md`)에도 로비 쪽이 절을 더했다. develop 병합(10-23)에서 천명님 §5.50과 번호가 겹쳐 로비 쪽을 §5.51로 바꿨다. 전투 쪽이 같은 번호를 쓰면 병합 때 번호만 맞춘다.

## 4. 고친 시험 중 전투와 닿는 것

- `CombatEntryIntegrationTests.Stage.cs`의 `LobbyLifeCycle_PendingEditRejectsExitBeforeViewDetach_ThenReceiptAllowsExit`는 손대지 않았다(같은 프레임에 같은 결과, Unity 통과).
- `TestLifeCycleGroup`(공용 시험 대역)에 `ILifeCycleSystemGroupService`의 새 속성 둘(`LobbyProgress`·`LobbyArtifactChecks`)을 `null`로 더했다. 전투 시험이 이 대역을 쓰므로 알린다.
- `FrameworkRuntimeCompositionTests`의 `new LobbyLifeCycle()` 한 줄을 새 필수 인자로 바꿨다.

## 5. 검증과 전달

MSBuild는 돌리지 않았고 새 시험 코드는 쓰지 않았다(사용자 결정). Phase 0~4마다 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인했다(모두 문제 없음). 이 문서를 외부로 발송하지 않았다.
