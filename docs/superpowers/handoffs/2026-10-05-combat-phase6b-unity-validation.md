# 전투 골격 6B — Unity 검증 항목과 기대 결과

- 작성일: 2026-10-05
- 대상: `e0b02cc7` 이후 F1 보완 및 F2 정상 종료 변경분
- **기존 커밋 `5078ee0e`는 사용자 검증 완료 확인으로 인수했다.** 아래 F1·정상 F2 기반 항목은 재실행 요청이 아니라 검증 범위 기록이다. 이후 일반 승리 자동 호출자 변경은 MSBuild·관리 회귀만 별도로 확인했으며 Unity를 새로 실행하지 않았다. 실행 상태는 [골격 계획 §6.14](../plans/2026-09-30-combat-flow-skeleton-plan.md#614-phase-6b-일반-승리-복귀-호출자-연결-2026-10-05)가 원본이다.
- F1은 Core 회신·포기 API·운영 DB 준비 완료와 무관하게 검증한다. 개발 fixture를 사용할 수 있다.

## 1. 실행 준비

1. Play를 종료하고 Unity가 변경된 스크립트·새 `.meta`·Combat 씬을 Refresh/컴파일하도록 기다린다. **컴파일 오류 0**을 확인한다.
2. Test Runner의 **EditMode**에서 아래 묶음을 실행한다. 전체 EditMode를 실행해도 된다. 예상 예외 주입은 테스트가 검증하며, 테스트 실패·예상하지 않은 로그가 남으면 실패 내용을 함께 기록한다.
3. Play 검증은 Boot 진입·Live·개발 fixture로 한다. `useDevBattle`을 끄는 것은 운영 데이터 검증으로 대체되지 않는다. 현재 T1/G7 기본 source의 차단과 이번 F1/F2 실패를 구별한다.
4. 이번 변경은 재화 지급을 추가하지 않는다. 유료 드로우나 Run 초기 금액 검증을 위해 View/Buffer에 임의 잔액을 넣지 않는다.

## 2. F1 필수 EditMode

| 테스트 묶음 / 사례 | 기대 결과 |
|---|---|
| `GameBootstrapFaultBoundaryTests` — Job 예외 | 실제 Bootstrap이 최초 오류를 한 번 보고. 실패 Job 호출 1회·정상 완료 처리 없음. 다음 프레임 같은 Runtime/Job을 다시 실행하지 않음 |
| 같은 묶음 — 외부 입력 오류 | Unity 콜백에서 공유 오류가 기록되면 다음 Runtime Tick과 초기 LifeCycle 전환 요청을 모두 차단 |
| 같은 묶음 — 정상 Shutdown·입력 정리 실패·Runtime Shutdown 실패 | 입력 정리 → Runtime 종료 순서. 입력 정리가 실패해도 Runtime 종료 시도. 반복 종료/OnDestroy가 실패 정리나 대기 Job을 재실행하지 않음 |
| `CombatViewSystemTests.ReturnButtonException_UsesSharedFaultBoundary_AndLateRequestsAreIgnored` | Return 콜백 예외가 같은 F1에 한 번 전달. 이후 Return·입력은 처리하지 않음 |
| `CombatViewSystemTests.FocusOrCaptureCleanupException_UsesSharedBoundary_AndStillUnbinds` (2개) | 포커스/캡처 해제 중 실패해도 수신 슬롯·Layer 정리를 시도. 오류 후 입력·명령 추가 없음 |
| `CombatViewSystemTests.SceneDisableCleanupFailure_ReportsThroughSharedBoundary_AfterReleasingDisplay` | 씬 비활성화 중 정리 실패에도 표시 자원을 해제하고 공유 경계로 오류 보고 |
| `CombatPresentationCleanupTests` (4개) | 이동 연출 취소가 실패해도 나머지 연출 취소를 시도. 복수 실패 보존, 재진입 Cancel 중복 없음, 다음 결과에 해제된 이동 수명 재사용 없음 |
| `CombatPathInputCleanupTests` (1개) | Return UI 정리 실패에도 경로 초안을 비움. 늦은 Release가 명령을 제출하지 않음 |

**회귀 묶음:** `RuntimeFaultBoundaryTests`, `CombatViewSystemTests`, `CombatMovementViewTests`, `CombatResultPlaybackTests`, `CombatFlowTests`, `CombatEntryIntegrationTests`, `FrameworkRuntimeCompositionTests`, `JobSystemTests`. 특히 정상 Move/Return·결과 확인 순서·Live/RecordInputOnly 진입이 유지돼야 한다.

## 3. F1 Play 확인

| 순서 | 동작 | 기대 결과 |
|---|---|---|
| 1 | Boot → 개발 전투 진입 | 전투 준비·활성화·최초 표시가 각 한 번. Console에 예상하지 않은 오류 없음 |
| 2 | 경로 Drag 중 포커스를 잃거나 입력을 취소 | 초안·강조·고스트 정리. 제출하지 않은 이동이 뒤늦게 실행되지 않음. 정상 복귀 뒤 입력 가능 |
| 3 | 정상 Move 후 가능한 Return 실행 | 결과 이동 연출 완료 뒤 최종 표시·일반 결과 확인 각 한 번. 중복 이동·중복 Return 없음 |
| 4 | 이동 연출/입력 중 Play 종료 | 연출·표시·입력 연결을 정리. 종료 뒤 새 Job/명령 실행·오류 반복 없음 |
| 5 | Play 재시작 | 이전 세션의 초안·콜백·표시가 새 전투를 변경하지 않음 |

Job/Return/정리 예외의 반복 방지는 §2의 자동 주입 테스트로 확인한다. 정상 Play만 성공한 것을 오류 주입 검증 통과로 기록하지 않는다.

## 4. F2 정상 종료 추가 검증

현재 공개 경계는 `SystemGroupsService.LifeCycle → CombatExit.RequestNormalExit(destination)`이다. 진행 중 전투/자발적 포기는 받지 않는다. 일반 승리의 자동 호출자는 연결됐으며 정책은 [골격 설계 §6.4](../specs/2026-09-30-combat-flow-skeleton-design.md#64-정상-승패-이탈의-구체-계약-2026-10-05)를 따른다. 이번 변경의 재생 기대는 일반 승리 → 마지막 결과 표시 → 확인 버튼 → StageSelect 복귀다. 패배·챕터 완료는 자동 정리/전환 없이 최종 결과를 유지한다.

| 검증 | 기대 결과 |
|---|---|
| 실제 Runtime·Register·Runner·Core를 사용하는 `CombatEntryIntegrationTests` | 확정 승패만 정상 이탈 수락. 직접 Manager.Change/Exit 우회는 로딩 전 거부. Runtime Shutdown은 별도 정리로 동작 |
| 마지막 결과 표시를 지연한 채 이탈 요청 | 새 입력만 중단. 제출된 명령·결과 번호 보존. 일반 결과 표시·확인 전에 Completion UI·EndBattle·전환 없음 |
| Completion 확인 전 여러 프레임 | 승리/패배 확인 UI 1회. 전투와 마지막 결과 유지. 확인 전 EndBattle 없음 |
| 확인 버튼/콜백 중복 | EndBattle·전환 한 번. 최종 결과·처리 번호·Completion 보존, 활성 캐릭터 정리 |
| 확인 콜백 직후 표시 예외·표시 해제·로딩 실패 | F1 한 번 보고 후 정상 전환 중단. 다음 Tick에 정산/정리/로딩 재시도 없음 |
| 이탈 뒤 다른 전투 진입 | 입력 Clear, 진입 요청 1회, 새 대상 준비·활성화 한 번. 지난 결과 재생/늦은 Completion 콜백 영향 없음 |
| `CombatViewSystemTests.ExitSubmissionStop_PreservesPendingResultAndInput_UntilPlaybackCompletes` | 실제 View 제출 정지가 진행 중 Playback/명령을 취소하지 않으며 결과 확인 후 입력을 다시 열지 않음 |
| `CombatViewSystemTests.CompletionConfirmation_IsOnceAndInvalidAfterDetach` (2개) | 실제 View의 확인 통지는 1회, Detach 뒤 통지는 무효. Completion 표시가 일반 결과 번호를 위조하지 않음 |
| Combat 씬 참조 | CombatHUD의 `completionButton`·`completionText` 연결. `CombatCompletion`은 초기 비활성, 일반 결과 확인 뒤 표시. 확인 버튼은 승패를 표시하며 누른 뒤 비활성화 |

Core 포기 API 인수·운영 SO/DB 준비·재화 분리는 별도 조건이다. 이 표의 정상 종료 성공만으로 6B 전체 완료나 포기 성공으로 기록하지 않는다.

## 5. 결과 전달 형식

`Unity 컴파일: 오류 없음/오류 내용`, `EditMode: 실행 묶음·통과/실패/미실행`, `Play: §3·§4 확인 항목과 결과`로 남긴다. 실패 시 테스트명·최초 오류 메시지·해당 스택과 재현 동작을 기록한다. 미실행 항목은 미실행으로 유지한다.
