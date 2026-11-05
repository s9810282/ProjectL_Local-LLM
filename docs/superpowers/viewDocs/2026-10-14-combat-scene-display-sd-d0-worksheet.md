# CombatSceneDisplay SD-D0 실습·검증 워크시트

- 실행일: 2026-10-15. 파일명은 조립 계획의 Phase별 워크시트 규칙을 따른다.
- Task·완료 상태 원본: [조립 계획 §4·§10.2](../plans/2026-10-14-combat-scene-display-composition-plan.md).
- 읽을 명세: [D0 조립·소유권 계약](../specs/2026-10-15-combat-scene-display-composition-contract.md).
- D0는 조사·명세·현재 코드 검증이다. 런타임·테스트 코드를 수정하거나 D1 Factory를 구현하지 않는다.

## 1. 현재 기준과 실행 증거

HEAD는 `6809cefab8a8e775cb6ce958757aa26b585eb123`이며 작업 시작 시 변경 파일은 없었다. SD-A/B/C′와 커서 스냅 감도 변경이 포함돼 있다. 아래 결과는 이 코드 기준의 새 실행이며, 선행 Unity 대기를 해소한 결과가 아니다.

| 검증 | 실행 환경·방법 | 결과·한계 |
|---|---|---|
| 현재 어셈블리 빌드 | VS 2022 Community MSBuild, `ProjectL.Tests.Framework.csproj`, Debug, 참조 프로젝트 포함 | 성공, 오류 0·경고 16. C′의 기존 경고와 파일·코드·대상 심볼을 대조해 추가 경고 0 |
| 기존 재생 관리 회귀 | Unity 6000.0.73f1의 Mono, 기존 SDB 실행기로 새 D0 DLL 메서드 직접 호출 | 4/4 통과. Unity Test Runner 실행 아님 |
| 기존 격자 상태 관리 회귀 | 같은 Mono, 기존 SDC 실행기로 새 D0 DLL 메서드별 SetUp 후 직접 호출 | 4/4 통과. 실제 씬·HUD·네이티브 자원 회귀 아님 |
| Unity EditMode·실제 씬 | 현재 Unity 프로세스, 프로젝트 `Temp/UnityLockfile`, Editor.log의 Project_L 경로 확인 | 미실행. 현재 세션을 종료하거나 같은 프로젝트의 두 번째 에디터를 실행하지 않음 |

스크래치는 ignored `Temp/CombatSceneDisplaySDD0/`이다. `Baseline.targets`는 프로젝트별 IntermediateOutputPath만 분리하며 Compile 항목·csproj를 수정하지 않는다. 빌드 출력은 `bin/`이고 로그는 `baseline-build.log`, `playback-managed-regression.log`, `highlight-managed-regression.log`다. 빌드 경고의 줄 이동은 새 경고로 세지 않았으며 기존 C′ `final-build.log`와 정규화한 16개 식별자가 같았다.

관리 실행기는 기존 `Temp/CombatSceneDisplaySDB/ManagedRegressionRunner.exe`와 `Temp/CombatSceneDisplaySDC/ManagedRegressionRunner.exe`를 재사용했다. 새 테스트 코드를 작성하지 않았다. 스크래치는 커밋 산출물이 아니므로 실행 목록과 한계는 이 문서에 남긴다.

| 묶음 | 이번에 실행한 메서드 |
|---|---|
| 재생 | `DelayedEvent_CancelRejectsOldCallback_AndNextResultCompletesOnce` |
| 재생 | `Cancel_ReentrantCleanup_DoesNotRecursivelyCancelAgain` |
| 재생 | `MovementCancelFailure_StillCancelsOtherPresentation_AndCollectsFailures(false)` / `(true)` |
| 격자 | `SetLayerCells_ReportsOnlyAddedOrRemovedCells` |
| 격자 | `MovementPath_PreservesOrderAndCopiesInput_ReorderNotifiesRenderer` |
| 격자 | `MovementPath_ClearAndReset_DropPathWithoutOtherLayers` |
| 격자 | `IsActing_OnlyForActionLayersOrPlacement` |

선행 검증 원본은 [A/B/C′ 실행 기록 §7.4~7.6](../plans/2026-10-14-combat-scene-display-followup-plan.md)이다. 그 빌드·관리 회귀는 과거 증거로 인수하며, A의 중복 Attach, B의 실제 씬 재생·같은 Display 연결, C′의 새 비용/HP 회귀와 두 표시 모드 재생은 Unity 대기 상태를 유지한다.

## 2. 코드 읽기 순서

1. ViewSystem.Attach에서 `_display != null` 검사가 생성보다 먼저 실행되는지 본다. StopInput 뒤에도 Display 소유권이 남는 이유를 구별한다.
2. Display의 현 생성자와 [계약 §2·§3](../specs/2026-10-15-combat-scene-display-composition-contract.md)을 대조한다. 참조 24개와 Display 내부 정리 목록·임시 목록은 구별한다.
3. EventPresenter 생성자에서 소비 콜백을 호출하지 않는지 확인하고, ThornPresenter 생성 전에 step 검증이 끝나는 순서를 본다.
4. Display Install/Clear/Release와 AttachDisplay의 `_display` 저장 위치를 읽는다. 조립 위치 변경과 오류 회복 개선은 다른 작업이다.
5. 스냅 두 getter의 실시간 조회와 선택적 WorldCamera를 확인한다. Factory가 Inspector 값을 스냅샷으로 만들지 않도록 한다.

## 3. Unity 기준선·D1 선택 회귀 — 대기

현재 열린 프로젝트의 컴파일 완료 후 Test Runner의 EditMode에서 아래 기존 회귀를 선택한다. D0 기준으로 실행할 수 있으면 결과를 먼저 기록하고, D1 후 같은 계약을 대조한다. D1까지 대기가 이어졌다면 최초 실행을 수정 전 통과 증거로 소급하지 않는다. 코드가 바뀌지 않은 통과 항목을 이유 없이 반복하지 않는다.

| fixture | 선택 메서드 | 확인할 행동 | D0 결과 |
|---|---|---|---|
| `CombatDisplayReuseTests` | `AuthoredCombatScene_DuplicateAttach_PreservesGhostInputAndOwnedResources(false/true)` | StopInput 전·후 중복 생성 거절과 기존 Ghost·입력·자원 보존 | 대기 |
| `CombatSceneEventPresenterTests` | `FirstInstall_BindsAfterBuild_AndLateEventsAfterReleaseCannotRenderOrComplete` | 최초 Build 뒤 연결, 실제 Display 해제 뒤 늦은 사건 차단 | 대기 |
| 같은 fixture | `BuildFailure_DropsOldEventBinding_EvenWhenOldFieldStillExists` | Build 실패가 이전 Mapper로의 사건 재개를 막음 | 대기 |
| 같은 fixture | `ClearDuringMovement_ReinstallUsesNewMapper_AndSameSceneResources` | Clear→재설치 때 현재 Mapper와 같은 씬 자원 사용 | 대기 |
| 같은 fixture | `Reinstall_EndsMovementPlanning_GhostOpacityAndStaleRangeDoNotSurvive` | 이동 편집·Ghost·원본 흐림·Range 정리 | 대기 |
| `CombatViewSystemTests` | `Attach_PassesSameDisplayToPresentation` | Presentation에 실제 전달받은 Display 전달 | 대기 |
| 같은 fixture | `InstallFailure_Propagates_WithoutBindingInput` | 설치 실패 시 입력 미연결, 저장된 Display를 Detach로 반환 | 대기 |
| `CombatMortarAssetTests` | `ActualSceneConsumedAndExpiryFacts_RemoveOnlySuppliedCellsAndReuseNativeModels` | 공급된 가시밭 소비/소멸 사실과 표시 재사용 | 대기 |
| 같은 fixture | `ActualSceneCancelAndClear_ReinstallUsesCurrentFactsAndReleaseDestroysOnlyOwnedClones` | 취소·Clear→현재 사실 재설치, 소유 clone만 반환 | 대기 |

선행 추가 회귀의 상세 순서는 [SD-A 워크시트](2026-10-14-combat-scene-display-sd-a-worksheet.md), [SD-B 워크시트](2026-10-14-combat-scene-display-sd-b-worksheet.md), [SD-C′ 워크시트](2026-10-14-combat-scene-display-sd-c-prime-worksheet.md)를 따른다. 위 목록이 그 미검증 전체를 대체하지 않는다.

## 4. 결과 기록과 다음 경계

Unity를 실행하면 실행 시각·HEAD/dirty 상태·선택 필터·통과/실패 수·로그 위치를 Task 계획에 추가한다. 실패가 D1의 전제나 전후 비교를 막으면 먼저 원인·해결 범위를 정한다. 관리 8/8이나 문서 검사로 Unity 통과를 대신하지 않는다.

D0에서 확정한 명세를 따라 D1 조립 추출을 준비할 수 있지만, 실제 D1 착수는 D0의 새 에이전트 검사·보고 뒤 별도 사용자 지시를 따른다. D0 종료 후 자동으로 D1 코드를 작성하지 않는다.
