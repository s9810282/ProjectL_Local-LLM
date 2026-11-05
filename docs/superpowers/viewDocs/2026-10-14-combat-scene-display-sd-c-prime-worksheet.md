# CombatSceneDisplay SD-C′ 실습 워크시트

비용 미리보기 대상을 HUD View의 주인인 `FieldUnitPresenter`로 옮긴 뒤 기존 표시 동작이 유지되는지 확인한다. 변경·검증 상태의 원본은 [후속 계획 §7.6](../plans/2026-10-14-combat-scene-display-followup-plan.md)이다.

## 1. 코드에서 확인

1. `_hudPreviewActor`는 `FieldUnitPresenter`에만 있고, `CombatSceneDisplay`의 `_blockedWallSet`은 없어야 한다.
2. `ShowHudCostPreview`가 이전 대상의 비용을 먼저 지운다. `ClearHudCostPreview()`는 인자가 없고 체력 예상 표시를 지우지 않는다.
3. Presenter의 `Release`도 비용을 정리하며 하나의 정리 실패가 나머지 반환을 막지 않는다. Display의 Clear·Release 목록에서는 비용 정리 위치를 유지한다.
4. Display의 공격·이동 표시가 같은 비용 슬롯을 사용하고, 유효한 이동 견적이 없을 때도 비용을 지우는지 확인한다.

## 2. Unity EditMode 확인

작업 중인 씬을 저장한 뒤 Combat이 아닌 씬에서 실행한다. 실제 씬 fixture는 Combat이 이미 열려 있으면 사용자 씬 보호를 위해 Ignore한다.

1. `CombatPointerMappingTests` 전체를 실행한다. 이번에 추가한 네 테스트는 다음과 같다.
   - `AttackCostThenOtherActorDraft_ReplacesTheSharedHudCostPreview`
   - `ClearAffectedCells_ClearsCostAndAffectedLayer_PreservesHealthPreview`
   - `ClearDraft_ClearsCostAndDraftLayer_PreservesHealthPreview`
   - `AfterRelease_LateAttackFeedback_DoesNotChangeReattachedScene`
2. `AttackHealthPreviewDisplayTests` 전체를 실행한다. 새 `HudCostPreview_ReplacesPreviousActor_AndClearsWithoutChangingHealth`와 기존 HP/AP 독립 정리 두 케이스를 포함한다.
3. `WallAttackInteractionTests`를 실행한다. 실제 씬 테스트 `AuthoredScene_BlockerFeedbackUnionsCellsRestoresColorAndClearsOnTeardown`은 같은 접촉을 두 번 전달해도 벽 한 칸만 강조되고, 정리 뒤 기존 표시가 복원되는지 확인한다.
4. `CombatViewSystemTests.AttackHealthPreview_ResultBeginRestoresBeforePlay_AndDelayedTickDoesNotClearAgain`을 실행한다.

회귀는 기존 동작 보존을 위해 운영 코드 수정 전에 작성·컴파일했다. 수정 전 실제 씬 통과는 미확인이다. MSBuild와 필드 강조 관리 회귀 4건은 실제 HUD 테스트 통과를 대신하지 않는다. 실행 결과에는 통과·실패·Ignore를 구분한다.

## 3. 재생 확인

- 공격 비용 표시 후 다른 유닛의 이동 초안을 표시하면 앞선 유닛의 비용 예상 표시가 지워지고 새 대상에만 표시되어야 한다.
- 이동 초안을 취소하면 비용이 복원되어야 한다. 영향 칸·초안 정리만으로 공격 체력 미리보기가 지워지면 안 된다.
- GridOutline과 CellPrefab 양쪽에서 벽 차단 표시, 호버 우선순위, 차단 정리 뒤 기존 표시 복원을 확인한다. 테스트를 위해 표시 설정을 바꿨다면 원래 값으로 돌린다.
- Combat 종료 후 재진입에서도 비용·체력 표시가 이전 전투에서 남지 않는지 확인한다.

## 4. 리뷰 지점

새 에이전트 검토와 Unity 결과를 확인한 뒤 다음 Phase를 결정한다. SD-C0는 남은 직접 구현을 평가하는 문서 작업이며, SD-C1 새 입력 피드백 클래스는 계속 보류한다.
