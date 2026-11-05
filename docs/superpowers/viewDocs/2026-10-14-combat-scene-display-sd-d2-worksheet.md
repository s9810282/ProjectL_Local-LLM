# CombatSceneDisplay SD-D2 실습·검증 워크시트

- 실행일: 2026-10-15. 파일명은 조립 계획의 Phase별 규칙을 따른다.
- Task·완료 상태 원본: [조립 계획 §6·§10.4](../plans/2026-10-14-combat-scene-display-composition-plan.md).
- 기준 HEAD: `6809cefab8a8e775cb6ce958757aa26b585eb123`. D0·D1의 미커밋 변경 18파일을 포함한 디스크 상태에서 시작했다.
- D2 전 비교 사본: ignored `Temp/CombatSceneDisplaySDD2/baseline/`의 원래 상대 경로. D1 코드와 아직 커밋되지 않은 Factory를 포함한다. HEAD만으로 D2 차이를 판단하지 않는다.

## 1. 구현 읽기와 변경 범위

| 읽을 지점 | 이번 변경·유지 계약 |
|---|---|
| [CombatMovementPlanningPresenter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatMovementPlanningPresenter.cs) | ShowDraft/ClearDraft/SetPathPlanning 구현을 모았다. 전체 생성자는 `(field, units, ghost, config, hud)`이며 HUD 필수. Field-only는 기존 범위 전용 경로 |
| [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs) | 위 세 API는 공개 해제 방어 뒤 이동 Presenter로 전달. UpdateMovementPointer의 screen→world 변환과 frozen 시 Raycast 생략은 유지 |
| [CombatSceneDisplayFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs) | 같은 씬 HUD를 기존 전체 생성자 마지막 인자로 전달. 다른 조립·검증·콜백 순서는 유지 |
| [CombatMovementDraftDisplayTests](../../../Assets/Tests/EditMode/Framework/CombatMovementDraftDisplayTests.cs) | Field/Units/Hud 중심의 좁은 표시 fixture. 실제 씬·Catalog 로딩·Core 계산 없이 GO/HUD/MovePreview를 준비 |
| 기존 fixture | PlanningDisplayTests의 전체 생성자에 HUD 추가. PointerMappingTests의 상세 색상 1건·전용 도우미를 새 fixture로 이전. 공유 비용·HP·늦은 호출 통합 회귀는 유지. RangeDisplayTests는 무수정 |

이동 초안의 후보/끝점 색·HUD·비용 표시 조건을 바꾸려면 이동 Presenter와 관련 Field/Hud/Units 계약을 중심으로 읽는다. 배치·공격 구현을 함께 수정할 필요는 없다. 공용 비용 슬롯·공개 해제 방어를 바꾸는 경우에는 기존 통합 회귀도 필요하다.

## 2. 정리와 상태 소유권

- `ShowDraft`는 BeginEditing 전에도 표시한다. `_editing` 전제를 추가하지 않았고, 기존 `_released`만 재사용한다.
- 비용 대상은 계속 Units, 격자 층은 FieldHighlightState가 소유한다. 별도 초안 캐시·비용 대상·해제 플래그는 추가하지 않는다.
- `ClearDraft`는 MovementDraft → Hover → 이동 HUD → 공용 비용 순서로 지운다. Range·HP·경로 지정 모드·고스트 편집 종료는 별도다.
- `SetPathPlanning(false)`는 경로 지정의 격자 표시 상태만 바꾼다. 초안·비용·HUD를 자동 정리하지 않는다.
- `Clear()`는 여전히 `RunAll(EndEditing, ClearRange)`다. EndEditing의 원본 흐림 복원·Ghost.Hide, Release의 플래그 기록 순서를 유지한다.
- Display의 Install 전·실패·Clear가 공유하는 `_fieldBound`에 ClearDraft를 추가하지 않는다. CombatPathInput의 독립 정리 호출도 바꾸지 않는다.

## 3. 실행 증거

| 검증 | 결과 | 의미·한계 |
|---|---|---|
| MSBuild | Systems·EditMode 및 참조 어셈블리 빌드 성공, 오류 0·추가 경고 0 | 최종 증분 빌드는 기존 경고 15건. 앞선 빌드의 Core 경고 1건과 합친 식별자 16개가 D1과 같음 |
| 경로 입력 정리 관리 회귀 | D1 DLL 1/1, D2 DLL 1/1 통과 | Unity Mono 직접 호출. 새 표시 fixture나 Unity Test Runner 실행 아님 |
| 코드·범위 대조 | 기존 이동 BeginEditing 이후 메서드 본문 동일, Field-only 유지, 범위 밖 선행 변경 14파일 해시 동일 | 동작 테스트를 대신하지 않는 변경 범위 검사 |
| Unity 네이티브 | 아래 33케이스 대기 | 현재 Unity 프로세스·프로젝트 잠금 유지. 세션 종료·프로젝트 중복 실행하지 않음 |

첫 빌드에서 이전한 `MovementPath`의 using 누락(CS0246)을 발견해 이동 Presenter에 기존 Contracts.Combat using을 추가하고 재빌드했다. Core 코드는 수정하지 않았다. 첫 로그는 `build-initial.log`, 수정 뒤 로그는 `build.log`다. 기존 경고 1건은 첫 빌드 때 출력된 Core CS8632이며, 수정 뒤에는 해당 참조 프로젝트가 증분 처리됐다.

`Temp/CombatSceneDisplaySDD2/NewFiles.targets`는 중간 산출물을 분리하고 Unity csproj에 아직 없는 D1 Factory·새 초안 테스트만 추가한다. 이미 포함된 파일은 중복 추가하지 않고 csproj는 수정하지 않는다. 관리 실행기와 `baseline-managed-regression.log`·`final-managed-regression.log`도 같은 스크래치에 있다.

관리 회귀는 `CombatPathInputCleanupTests.ReturnCleanupFailure_StillClearsDraft_AndIgnoresLateRelease`다. 반환 버튼 표시 정리에서 예외가 나도 초안·입력을 정리하고 이후 늦은 확정 입력을 무시하는 기존 테스트다. FakeDisplay와 순수 Buffer를 사용하며 화면 오브젝트를 만들지 않는다. D1의 재생 관리 회귀 등 변경과 무관한 기존 성공은 반복 실행하지 않았다.

## 4. Unity 선택 묶음 — 대기

### 4.1 좁은 초안 표시: 5케이스

| 메서드 | 케이스 | 관측 |
|---|---:|---|
| `DraftHighlight_ChangesPathAndEndpoint_AndClearPreservesUnderlyingRange` | 1 | 미응답 후보/경로, 유효·무효 끝점 색, 경로 축소, Clear 뒤 하위 Range. 기존 PointerMapping 상세 검사를 이전 |
| `DraftBeforeBeginEditing_UpdatesHudAndCost_WhenResponseOrPointerChanges` | 2 | 두 표시 모드에서 편집 전 성공·실패·미응답·포인터 무효에 따른 실제 HUD 텍스트·비용 이미지 |
| `ClearDraft_ClearsHudAndCost_PreservesRangeHealthAndPlanning` | 2 | 두 표시 모드에서 비용·초안만 정리, Range·HP·경로 지정 모드 보존. 계획 모드 종료의 초안 보존 |

새 fixture는 임시 Field/Hud/Units와 표시용 Ghost를 조립하고 Renderer의 MaterialPropertyBlock, 텍스트·이미지 상태를 관찰한다. 실제 Combat 씬이나 전체 Catalog를 열지 않지만 Unity GameObject·Renderer·TMP를 사용하므로 관리 회귀로 실행한 것으로 쓰지 않는다.

### 4.2 기존 기능·공유 계약: 네이티브 28케이스

| fixture·선택 | 케이스 | 이유 |
|---|---:|---|
| `CombatMovementPlanningDisplayTests` | 18 | 같은 전체 생성자의 HUD 인수 뒤 기존 고스트·원본 흐림·풀·실패 정리 계약 |
| `CombatMovementRangeDisplayTests` | 6 | Field-only 경로, 초안/호버 공존, 실패·빈 범위, 반복 정리·해제 뒤 새 공격 Range 보존 |
| `CombatPointerMappingTests.AttackCostThenOtherActorDraft_ReplacesTheSharedHudCostPreview` | 1 | 공격→다른 행위자 이동의 공용 비용 슬롯 교체 |
| `CombatPointerMappingTests.ClearDraft_ClearsCostAndDraftLayer_PreservesHealthPreview` | 1 | 실제 Display를 통한 비용·초안 정리와 HP 보존 |
| `CombatPointerMappingTests.AfterRelease_LateDraftAndReturnCalls_AreIgnored_AndUnbindStillAllowed` | 1 | 실제 Display의 늦은 초안·복귀 호출 방어 |
| `CombatSceneEventPresenterTests.Reinstall_EndsMovementPlanning_GhostOpacityAndStaleRangeDoNotSurvive` | 1 | 재설치에서 기존 편집·공유 Ghost·원본 흐림·Range 정리 |

새 5 + 기존 28 = Unity 대기 33케이스다. 관리 실행한 정리 회귀 1건은 별도다. 이전한 상세 색상 회귀를 PointerMappingTests에 남겨 이중 집계하지 않는다. 이 목록은 A/B/C′·D0·D1의 아직 남은 검증을 완료 처리하거나 대체하지 않는다.

실제 씬 fixture는 사용자 Combat 씬을 보호하기 위해 Ignore할 수 있다. 다른 저장된 씬에서 EditMode를 실행하고 통과·실패·Ignore를 구분해 기록한다. 새로운 실패가 나오면 다음 Phase 변경 전에 원인·해결 범위를 정한다.

## 5. Phase 경계

D2의 새 에이전트 검사 결과와 현재 판정은 계획 §10.4에 기록한다. D2에서 보고 후 멈추며 D3 배치 구현 분리는 별도 사용자 지시 뒤 시작한다. D1의 부분 Configure·Display 인수 전 실패 복구 한계도 기존 상태로 남는다.
