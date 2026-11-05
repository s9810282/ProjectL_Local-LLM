# CombatSceneDisplay SD-D1 실습·검증 워크시트

- 실행일: 2026-10-15. 파일명은 조립 계획의 Phase별 규칙을 따른다.
- Task·완료 상태 원본: [조립 계획 §5·§10.3](../plans/2026-10-14-combat-scene-display-composition-plan.md).
- 비교 명세: [D0 조립·소유권 계약](../specs/2026-10-15-combat-scene-display-composition-contract.md).
- 기준 HEAD: `6809cefab8a8e775cb6ce958757aa26b585eb123`. 시작 시 미커밋 변경은 D0 계획·명세·워크시트 3개였고, 운영·테스트 코드는 D0와 같았다.

## 1. 구현 확인 순서

1. [CombatSceneDisplayFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs)를 연다. root/tables/services 검사부터 SceneRoot 참조 취득, 생성·Configure, 콜백, Display 반환까지 모였다. D0 명세의 순서를 그대로 따른다.
2. [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs)의 생성자는 준비된 참조 24개를 보관하고 `_fieldBound`를 만든다. Install/Refresh/Clear/Release, 스냅 설정 조회, 입력 표시·집기·사건의 공개 방어는 이전과 같다.
3. Factory의 `thorns` 지역 참조를 본다. EventPresenter는 소비 콜백을 저장하고, step 검증 뒤에 ThornPresenter를 만든다. 그 객체를 Display·EventPlayback과 공유한다. 선택적 prefab·Audio·WorldCamera 의미를 유지한다.
4. [CombatViewSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs)의 공개 Attach는 기존 사전 검사 뒤 Factory를 호출한다. `_display` 인수와 실제 Display의 Presentation.Attach, 입력 연결, root 구독 순서는 같다.
5. 실제 씬 fixture 6파일 8곳이 Factory를 거치는지 확인한다. FakeDisplay 기반 ViewSystem 단위 테스트는 그대로다. 생성자 직접 호출은 Factory 내부 1곳만 남는다.

생성 인자·Configure·구현 연결을 바꿀 때의 편집 지점은 Factory로 모였다. 긴 주입 생성자와 공통 수명 참조는 D1의 의도된 중간 상태다. 이동·배치·공격 표시 구현은 다음 D2~D4의 작업이다.

## 2. 이번 실행 증거

| 검증 | 실행·결과 | 한계 |
|---|---|---|
| Systems·EditMode 어셈블리 | VS 2022 MSBuild, `ProjectL.Tests.Framework.csproj` Debug 및 참조 빌드 성공. 오류 0·경고 16 | D0 경고와 파일·코드·대상 심볼이 같은 16건, 추가 경고 0 |
| 기존 재생 관리 회귀 | Unity 6000.0.73f1 Mono로 새 DLL의 기존 회귀 4/4 통과 | Unity Test Runner·실제 Factory/씬 검증 아님 |
| D0 기준선 | [D0 워크시트](2026-10-14-combat-scene-display-sd-d0-worksheet.md)의 빌드·관리 8/8을 수정 전 증거로 인수 | 이번에 격자 상태 회귀를 반복 실행하지 않음 |
| Unity EditMode | 미실행. Unity 프로세스와 프로젝트 잠금이 계속 존재 | 현재 세션을 종료하거나 같은 프로젝트를 중복 실행하지 않음 |

이번 관리 실행은 `CombatPresentationCleanupTests`의 지연 완료/오래된 콜백, 재진입 취소, 이동 정리 실패의 두 경우를 사용했다. 구체 메서드는 다음과 같다.

- `DelayedEvent_CancelRejectsOldCallback_AndNextResultCompletesOnce`
- `Cancel_ReentrantCleanup_DoesNotRecursivelyCancelAgain`
- `MovementCancelFailure_StillCancelsOtherPresentation_AndCollectsFailures(false)` 및 `(true)`

스크래치 `Temp/CombatSceneDisplaySDD1/`에 `build.log`, `playback-managed-regression.log`, `bin/`을 남겼다. `NewFiles.targets`는 프로젝트별 중간 산출물을 분리하고 Systems 컴파일에 빠진 새 Factory 파일만 추가한다. 이미 csproj에 포함됐다면 중복 추가하지 않는다. 생성된 csproj는 수정하지 않았다. 기존 SDB 관리 실행기를 현재 DLL에 재사용했다.

## 3. Unity에서 남은 검증

사용자 Combat 씬이 열려 있으면 해당 fixture가 보호 목적으로 Ignore할 수 있다. 저장한 다른 씬을 연 상태에서 컴파일을 마친 뒤 Test Runner/EditMode로 실행하고, Ignore를 성공으로 기록하지 않는다.

[D0 워크시트 §3](2026-10-14-combat-scene-display-sd-d0-worksheet.md)의 기존 D1 선택 묶음 **10케이스**를 실행한다. 중복 Attach 2케이스, 사건 최초 설치·Build 실패·Clear 재설치·이동 편집 정리 4케이스, ViewSystem 같은 Display 전달·Install 실패 반환 2케이스, 가시밭 소비·소멸·취소·재설치·소유 clone 반환 2케이스다. D0 Unity 기준선은 확보하지 못했으므로 D1 이후 통과를 수정 전 통과로 소급하지 않는다.

여기에 다음 **새 회귀 1케이스**를 더한다.

| fixture·메서드 | 관찰할 결과 | 실행 상태 |
|---|---|---|
| `CombatMortarAssetTests.Factory_InvalidMovementStep_RejectsBeforeCreatingThornStorage` | 가시밭 prefab이 있는 운영 설정에서 step=0이면 ArgumentOutOfRangeException, `FieldThorns_Inactive` 미생성 | Unity 대기 |

새 회귀는 기존 Display를 반환한 후 다시 조립한다. 예상 밖 생성 성공도 `_display`에 보관해 teardown이 반환하며, 변경한 step은 finally에서 복원한다. 실패한 부분 조립의 전체 Configure 복구를 보장하는 테스트는 아니다. fixture는 기존 사용자 씬 보호·additive 씬 닫기·EventSystem 복원을 유지한다.

가시밭의 동일 인스턴스 연결은 기존 `ActualSceneConsumedAndExpiryFacts_RemoveOnlySuppliedCellsAndReuseNativeModels`가 Refresh 표시→소비 사건을, `ActualSceneCancelAndClear_ReinstallUsesCurrentFactsAndReleaseDestroysOnlyOwnedClones`가 재생 취소→같은 표시 정리·재설치·최종 clone 반환을 관찰한다. 동일 필드를 reflection으로 비교하는 별도 테스트는 추가하지 않았다.

## 4. 다음 경계

부분 Configure 실패와 Factory 반환 후 `_display` 인수 전 예외의 해제 누락은 [계획 §3](../plans/2026-10-14-combat-scene-display-composition-plan.md)에 남은 기존 위험이다. D1은 생성 위치를 옮겼으며 오류 회복 정책을 변경하지 않았다.

다음은 D2의 이동 초안 구현 집중이다. D1의 새 Phase 검사·보고 뒤 멈추며 별도 지시 전 D2를 시작하지 않는다. Unity 결과는 실행 시각·HEAD/dirty 상태·통과/실패/Ignore 수와 함께 계획에 기록한다.
