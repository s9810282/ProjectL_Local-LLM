# CombatSceneDisplay SD-B 실습 워크시트

재생 연결은 사건 재생 계약만 요구하고, 실제로 전달되는 Display가 해제 뒤 새 사건을 거절하는지 확인한다. 변경과 실행 상태의 원본은 [후속 계획 §7.5](../plans/2026-10-14-combat-scene-display-followup-plan.md)다.

## 1. 코드에서 확인

1. `ICombatPresentation.Attach`와 구현 2개(`OrderedCombatPresentation`, `FakeCombatPresentation`)가 `ICombatEventPlayback`을 받는지 확인한다.
2. `CombatViewSystem.AttachDisplay`의 `_presentation.Attach(display)`가 그대로인지 확인한다. Display 내부의 `CombatSceneEventPlayback`을 직접 넘기면 Display의 해제 확인을 우회한다.
3. `CombatViewSystem.Detach`에서 재생 Detach가 Display Release보다 먼저인지 확인한다. 이것이 운영 경로의 실제 방어선이다.

## 2. Unity EditMode 확인

저장할 작업을 저장한 뒤 Combat이 아닌 씬에서 실행한다. 실제 씬 fixture는 Combat 씬이 이미 열려 있으면 사용자 씬을 보존하기 위해 Ignore한다.

1. `CombatViewSystemTests.Attach_PassesSameDisplayToPresentation`을 실행한다. 재생이 받은 객체는 ViewSystem에 연결된 Display와 같은 참조여야 한다.
2. `CombatSceneEventPresenterTests.Explosion_OrderedResultCompletesWhileLegacyParticlesAreStillPlaying`을 실행한다. 정상 재생 완료 후 Display를 Release하되 재생 연결은 남겨 둔다. 다음 Play는 `[CombatSceneDisplay]`로 시작하는 예외를 던지고 완료 콜백을 추가로 부르지 않아야 한다.
3. `CombatPresentationCleanupTests`의 `DelayedEvent_CancelRejectsOldCallback_AndNextResultCompletesOnce`, `Cancel_ReentrantCleanup_DoesNotRecursivelyCancelAgain`, `MovementCancelFailure_StillCancelsOtherPresentation_AndCollectsFailures` 두 케이스를 실행한다.
4. `CombatViewSystemTests`의 `DelayedResult_IsNotAcknowledgedBeforeCompletion_AndIsNotPlayedTwice`, `Finished_ReleasesInputOnly_AndStillAcknowledgesTheLastDelayedResultOnce`, `Detach_CancelsAndReleases_AndOldCompletionDoesNotFinishTheNextAttach`를 실행한다.

통과·실패·Ignore를 구분해 기록한다. MSBuild와 관리 실행기의 4케이스 통과는 Unity 씬·ViewSystem 회귀의 통과를 뜻하지 않는다.

## 3. 리뷰 지점

- 타입 계약만 좁히고 실제 Display 객체와 재생 알고리즘·취소·해제 순서를 유지했는가?
- Release 뒤 Play 회귀가 진행 중 재생이나 미연결 예외로 잘못 통과하지 않는가?
- 선택 항목인 `ICombatMovementPresenter` 삭제는 이번 범위에서 제외했다. 다음 SD-C′는 비용 미리보기 대상 상태를 옮기는 별도 작업이다.
