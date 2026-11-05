# CombatSceneDisplay SD-D4 실습·검증 워크시트

- 실행일: 2026-10-15. Task·완료 상태 원본은 [조립 계획 §8·§10.6](../plans/2026-10-14-combat-scene-display-composition-plan.md)다.
- 기준 HEAD: `6809cefab8a8e775cb6ce958757aa26b585eb123`. D3 새 검사·보고를 마친 뒤 D0~D3 미커밋 변경 28파일을 포함해 시작했다.
- 비교 사본: ignored `Temp/CombatSceneDisplaySDD4/baseline/`의 원래 상대 경로. `preexisting-changes.json`에 시작 해시를 보관했다.

## 1. 구현과 공유 계약

| 지점 | 변경·유지 계약 |
|---|---|
| [CombatAttackFeedback](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatAttackFeedback.cs) | Field/Units만 주입. Range·Affected·비용·HP·벽 표시 9개 API와 벽 칸 작업 목록을 소유 |
| [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs) | 공격 API는 기존 공개 해제 방어 뒤 위임. 일반 Hover·복귀 버튼·공통 수명은 유지 |
| [CombatSceneDisplayFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs) | 같은 Field/Units로 공격 표시 객체를 만들고 Display에 주입 |

ClearAffectedCells는 Field.Affected → Units 비용 슬롯 순서로 정리한다. HP·Range·Hover·BlockedWall·이동 초안은 그대로다. ClearAttackHealthPreview는 비용을 지우지 않는다. 비용/HP 대상은 Units, 층과 중복 처리는 FieldHighlightState가 소유한다.

`_blockedWallCells`는 매 호출 다시 채우는 List이며 게임 사실이나 화면 상태 캐시가 아니다. 입력 contact.WallCells를 순서대로 평탄화하고 기존 Field가 중복과 보드 밖을 처리한다. null contact 목록은 기존 목록을 비우기 전에 예외를 내며, 해제된 Display는 인자 검사에 도달하지 않는다. 새 ClearAll·Release·해제 플래그를 만들지 않았다. 입력 응답 판단은 AttackTargetingInteraction, 실제 애니메이션은 CombatAttackPresenter에 남는다.

## 2. 검증 선택

| 묶음 | 케이스 | 관측 |
|---|---:|---|
| 새 CombatAttackFeedbackTests | 6 | CellPrefab/GridOutline 각각 벽 접촉 합침·교체·빈 결과, 실제 강조 색의 우선순위/복원, 영향 층·비용·HP 독립 정리 |
| WallAttackInteractionTests.AuthoredScene_RealBlockerFeedbackClearsOnReinstallAndTeardown | 1 | 실제 입력 계산의 contact→Display 연결, Clear→Install 뒤 벽 잔존 없음과 handle teardown. 상세 색 검사는 좁은 fixture로 이전 |
| AttackHealthPreviewDisplayTests 전체 | 10 | 실제 HP/AP 대상 교체·복구·0 HP·구조물·풀·삭제 대상 |
| CombatPointerMappingTests 선택 | 3 | 공격→이동 비용 교체, ClearAffectedCells의 HP 보존, 동일 root 새 연결에 대한 늦은 공격 표시 9종 방어 |
| CombatMovementRangeDisplayTests.RepeatedClearAndLateShowAfterRelease_PreserveNewAttackRange | 2 | 오래된 이동 Range 정리가 새 공격 Range를 지우지 않음 |

Unity 선택은 **신규 6 + 기존 16 = 22케이스**다. PointerMapping 선택은 `AttackCostThenOtherActorDraft_ReplacesTheSharedHudCostPreview`, `ClearAffectedCells_ClearsCostAndAffectedLayer_PreservesHealthPreview`, `AfterRelease_LateAttackFeedback_DoesNotChangeReattachedScene`다. 새 표시 fixture도 실제 GameObject·Renderer·TMP를 사용하므로 관리 실행으로 세지 않는다. GridOutline handle 색 관찰은 실제 GPU 렌더링 성공과 구분한다.

WallAttackInteractionTests의 실제 씬 회귀를 제외한 8메서드·12케이스는 관리 실행 대상으로 선택했다.

- `BlockedSingle_HoverUsesRealRangeEvidenceWithoutPreviewOrClick`
- `PartialGroup_PreviewShowsBlockersAndClearEffects_AndKeepsValidClick` — Direction/All
- `Splash_PreviewUsesImpactEvidenceRatherThanRangeOrigin`
- `InvalidToValidHover_ClearsOldBlockersWhileWaitingForNewPreview`
- `LateAnswerAfterHoverChangeExitCancelOrTeardown_CannotRestoreHighlights` — 0/1/2/3
- `OlderAnswerCannotReplaceNewerHoverPreview`
- `BlockerLayer_OverridesEffectsButYieldsToPointerAndRestoresUnderlyingColor`
- `ResultRefresh_ClearsBlockersAndCostUntilMatchingNewAnswersArrive`

이 관리 테스트는 순수 Buffer/Core와 FakeFeedback 및 강조 상태를 확인한다. 새 CombatAttackFeedback·실제 씬 표시를 실행한 증거가 아니다. 변경 전 D3 DLL과 변경 후 D4 DLL에 같은 선택을 Unity Mono로 적용하며 결과는 계획 §10.6에 기록한다.

## 3. 실행 자료와 대기

`Temp/CombatSceneDisplaySDD4/`에 `ManagedWallRegressionRunner.cs/.exe`, `baseline-managed-regression.log`, `final-managed-regression.log`, `build.log`, `NewFiles.targets`를 보관한다. 빌드는 Systems·EditMode 및 참조 어셈블리를 대상으로 하며 새 파일만 Compile에 보충한다. csproj·어셈블리 참조는 수정하지 않는다.

현재 Unity 세션은 유지하며 위 22케이스의 Test Runner 실행은 대기다. 실제 씬 fixture는 열려 있는 사용자 Combat 씬을 보호하기 위해 Ignore할 수 있으므로 다른 저장된 씬에서 실행하고 통과/실패/Ignore를 구분한다. 선행 A/B/C′·D0~D3 검증 대기도 유지한다.

## 4. Phase 경계

코드·가능한 검증·문서 준비 후 D3 검사자·D4 보조와 다른 새 에이전트로 검사한다. 결과와 지적 처리는 계획 §10.6에 기록한다. D4 결과를 보고하고 멈추며 D5 통합 인수·Refresh 평가는 별도 사용자 지시 뒤 진행한다. 부분 Configure·인수 전 실패 복구 한계는 기존 별도 후속 범위다.
