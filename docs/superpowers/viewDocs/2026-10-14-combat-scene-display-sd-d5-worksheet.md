# CombatSceneDisplay SD-D5 통합 인수 워크시트

- 실행일: 2026-10-15. Task·완료 상태 원본은 [조립 계획 §9·§10.7](../plans/2026-10-14-combat-scene-display-composition-plan.md)이다.
- 구조·변경 지도·26개 참조·Refresh 평가는 [인수 평가](../specs/2026-10-15-combat-scene-display-composition-acceptance.md)를 따른다.
- 기준 HEAD: `6809cefab8a8e775cb6ce958757aa26b585eb123`와 D0~D4 미커밋 변경 33파일. ignored `Temp/CombatSceneDisplaySDD5/baseline/`과 `preexisting-changes.json`에 작업 전 사본·해시를 보관했다.

## 1. 증거를 구분한 인수

| 출처 | 확보한 근거 | 뜻하지 않는 것 |
|---|---|---|
| D0~D4 MSBuild·관리 로그 | 아래 Phase별 성공. 마지막 D4 빌드 오류 0·기존 경고 16·새 경고 0, Wall 관리 전후 12/12 | 실제 씬·새 표시 객체·Unity Test Runner를 관리 실행한 것은 아님 |
| 현재 Editor.log·Library DLL | D4 중간 오류 뒤 후속 컴파일·Mono reload 성공. 현재 D4 관련 파일 수정 시각보다 DLL 생성 시각이 늦음 | 소스 해시와 DLL의 완전 일치나 개별 게임 동작 성공 증명은 아님 |
| **사용자 확인** | D4 이후 현재 미커밋 코드의 EditMode와 GridOutline·CellPrefab 실제 전투 재생 **모두 통과, 실패·Ignore 없음**이라는 명시 답변 | 에이전트가 직접 실행한 결과, 자동 수집한 69/69, 수정 전 기준선 통과가 아님 |

사용자에게 먼저 D4 이후 현재 코드의 확인 결과를 물었고 "전체 확인함"이라는 답변을 받았다. 이어 EditMode와 두 표시 모드의 실제 전투 재생이 모두 통과했으며 실패·Ignore가 없는지 확인해 **"모두 통과, 실패·Ignore 없음"**이라는 답변을 받았다. 현재 통합 검증은 이 확인을 출처로 인수한다. 전체 실행 건수·선택 필터·XML·개별 프레임 로그는 전달받지 않았다.

Editor 로그의 위치는 `C:/Users/Hyeon2-400/AppData/Local/Unity/Editor/Editor.log`다. 조사 당시 15139행의 Tundra 성공, 15152행의 Mono reload 성공, 15228·15238행의 Play Mode reload가 확인됐다. Systems DLL은 2026-10-14 18:16:00 UTC, Tests.Framework DLL은 18:19:40 UTC였다. 관련 런타임 최종 수정은 18:14:20 UTC 이하, 테스트는 18:16:52 UTC 이하였다. 로그는 이후 바뀔 수 있으며 D5 기록 자체가 테스트 결과 파일은 아니다.

에디터는 이미 열려 있었다. 이번 D5에서는 프로세스를 종료하거나 두 번째 에디터를 실행하지 않았고, 런타임·테스트 코드 변경도 없어 성공한 빌드·관리 회귀를 다시 반복하지 않았다.

## 2. Phase별 기존 증거와 현재 인수

| Phase | 빌드·관리 근거 | 당시 새 검사자 | 현재 결과 |
|---|---|---|---|
| D0 | 빌드 오류 0/기존 경고 16, 관리 재생 4+강조 상태 4 | `sdd0_phase_review` | 명세 인수. 당시 Unity 기준선 미확보 이력 유지 |
| D1 | 빌드 오류 0/기존 경고 16, 관리 재생 4/4 | `sdd1_phase_review` | 선택 11케이스의 현재 검증은 D5 사용자 전체 확인으로 인수 |
| D2 | 최종 빌드 오류 0/기존 경고 15, 첫 로그 Core 1건과 합친 식별자 16; 관리 정리 전후 1/1 | `sdd2_phase_review` | 선택 33케이스의 현재 검증을 같은 출처로 인수 |
| D3 | 최종 빌드 오류 0/기존 경고 8, 최초 로그와 합친 식별자 16; 관리 불필요 반복 없음 | `sdd3_phase_review` | 선택 11케이스의 현재 검증을 같은 출처로 인수 |
| D4 | 빌드 오류 0/기존 경고 16, Wall 관리 전후 12/12 | `sdd4_phase_review` | 선택 22케이스의 현재 검증을 같은 출처로 인수 |

기존 각 검사에는 조치할 지적이 없었다. D2 using 누락과 D3 테스트 URP 직접 참조 오류는 각 Phase 빌드에서 수정된 이력이다. D4의 중간 테스트 API 이름 오류도 최종 수정·컴파일 뒤 현재 코드에 남지 않는다. D0~D4의 당시 대기 기록은 당시 사실로 보존하며 현재 판정은 계획 §10.7이 원본이다.

## 3. D1~D4의 최종 고유 검증 선택

Phase별 합계 11+33+11+22=77에서 재설치 1, 이동→배치 Ghost 2, 중복 Attach 2, 비용 교체 1, 오래된 이동 Range 2의 중복을 빼면 **고유 선택은 69케이스**다. 이 숫자는 코드에서 집계한 필수 선택 수이며 사용자가 실행한 전체 Test Runner 건수를 뜻하지 않는다.

| fixture | 현재 선택 | 케이스 |
|---|---|---:|
| CombatDisplayReuseTests | AuthoredCombatScene_DuplicateAttach_PreservesGhostInputAndOwnedResources(false/true) | 2 |
| CombatSceneEventPresenterTests | FirstInstall_BindsAfterBuild_AndLateEventsAfterReleaseCannotRenderOrComplete; BuildFailure_DropsOldEventBinding_EvenWhenOldFieldStillExists; ClearDuringMovement_ReinstallUsesNewMapper_AndSameSceneResources; Reinstall_EndsMovementPlanning_GhostOpacityAndStaleRangeDoNotSurvive | 4 |
| CombatViewSystemTests | Attach_PassesSameDisplayToPresentation; InstallFailure_Propagates_WithoutBindingInput | 2 |
| CombatMortarAssetTests | ActualSceneConsumedAndExpiryFacts_RemoveOnlySuppliedCellsAndReuseNativeModels; ActualSceneCancelAndClear_ReinstallUsesCurrentFactsAndReleaseDestroysOnlyOwnedClones; Factory_InvalidMovementStep_RejectsBeforeCreatingThornStorage | 3 |
| CombatMovementDraftDisplayTests | 현재 3메서드 전체 | 5 |
| CombatMovementPlanningDisplayTests | 현재 10메서드 전체 | 18 |
| CombatMovementRangeDisplayTests | 현재 3메서드 전체 | 6 |
| CombatPointerMappingTests | 아래 6메서드 | 6 |
| CombatPlacementFeedbackTests | 현재 3메서드 전체 | 6 |
| CombatAttackFeedbackTests | 현재 3메서드 전체 | 6 |
| AttackHealthPreviewDisplayTests | 현재 9메서드 전체 | 10 |
| WallAttackInteractionTests | AuthoredScene_RealBlockerFeedbackClearsOnReinstallAndTeardown | 1 |
| **합계** | **12 fixture** | **69** |

PointerMapping 선택은 다음과 같다.

- `AttackCostThenOtherActorDraft_ReplacesTheSharedHudCostPreview`
- `ClearDraft_ClearsCostAndDraftLayer_PreservesHealthPreview`
- `AfterRelease_LateDraftAndReturnCalls_AreIgnored_AndUnbindStillAllowed`
- `AfterRelease_LatePlacementFeedback_DoesNotChangeReattachedScene`
- `ClearAffectedCells_ClearsCostAndAffectedLayer_PreservesHealthPreview`
- `AfterRelease_LateAttackFeedback_DoesNotChangeReattachedScene`

ViewSystem의 두 메서드도 fixture의 Rig가 GameObject/CombatSceneRoot를 생성·파괴하므로 네이티브 선택이다. FakeDisplay라는 이유로 관리 실행으로 재분류하지 않는다. 이전 Wall 이름과 PointerMapping에서 이동한 상세 초안 색 테스트도 중복 집계하지 않는다.

## 4. 실제 재생과 모드 연결 인수

| 계획의 확인 범위 | 인수 근거 |
|---|---|
| GridOutline·CellPrefab의 배치 drag/취소/원본 복원 → 이동 초안·비용·취소/복귀 → 공격 영향·벽 → 사건 종료 최종 표시 → 이탈/재진입 | 사용자의 두 표시 모드 실제 전투 재생 전체 통과 확인. 각 단계의 자동 관측·스크린샷은 수집하지 않음 |
| RecordInputOnly 개발 Layer, Live의 포기·행동 버튼 연결 | 기존 모드 계약·연결 코드를 대조하고 사용자 전체 EditMode 통과 확인을 인수. 모드별 별도 수동 로그를 확보했다고 쓰지 않음 |

연결 근거는 ViewSystem Attach의 `_devLayer.Attach`, `_abandon.Attach`, 행동 Bind 및 SyncInputToCore 이후 가용성 갱신이다. 관련 기존 회귀는 `Live_DoesNotShowDevLayerPanel`, `RecordInputOnly_DevButtons_*`, `Abandon_RecordInputOnly_HidesButton_AndIgnoresOpen`, `Abandon_LiveIdle_OpenBlocksInput_ConfirmRequestsOnce`, `ActionButtons_*`다. 이 선택은 위 69케이스와 별개이며 D5에서 다시 실행한 목록이 아니다.

## 5. 선행 이력과 후속 인계

- A/B/C′의 현재 Unity 대기는 사용자 **전체 EditMode·실제 재생 확인**으로 인수한다. 69개 선택만으로 선행 전체를 덮는 것은 아니다. 특히 B의 `Explosion_OrderedResultCompletesWhileLegacyParticlesAreStillPlaying`, A의 DisplayReuse 전체·추가 ViewSystem, B의 지연/종료/재연결, C′의 Pointer/HP 회귀는 69개 밖의 항목을 포함한다. 출처는 [선행 계획의 현재 인수 기록](../plans/2026-10-14-combat-scene-display-followup-plan.md)이다.
- A의 수정 전 실패 재현, C′의 수정 전 Unity 통과, D0 당시 Unity 기준선은 이번 확인으로 소급 확보하지 않는다. 이후 수정의 기준은 현재 D4 코드와 이번 사용자 인수다.
- 부분 Configure·Display 인수 전 예외의 정리 누락은 해결하지 않은 기존 위험이다. 검증 통과가 오류 회복 개선을 뜻하지 않는다.
- Refresh 후보의 구현·Audio 이전·새 수명 객체 도입은 별도 설계와 Task가 필요하다. D5는 구조 평가·인수·문서화에서 마친다.
- 새 D5 검사자의 결과와 최종 판정은 계획 §10.7에 기록한다. 커밋·push·외부 문서 전달은 사용자가 한다.
