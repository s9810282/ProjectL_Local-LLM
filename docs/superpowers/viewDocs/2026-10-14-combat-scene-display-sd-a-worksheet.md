# CombatSceneDisplay SD-A 실습 워크시트

중복 `Attach`가 예외를 던지기 전에 공유 고스트를 지우거나 가시밭 보관 객체를 만들던 경로를 확인한다. 변경과 실행 상태의 원본은 [후속 계획 §7.4](../plans/2026-10-14-combat-scene-display-followup-plan.md)다.

## 1. 코드에서 확인

1. [CombatViewSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs)의 `Attach`를 연다. 기존 검사를 마친 뒤 `ThrowIfDisplayAttached()`를 부르고, 그다음 Display를 만드는지 확인한다.
2. 같은 파일의 `AttachDisplay`가 같은 검사 함수를 쓰는지 확인한다. `_display != null`이 기준이다. `StopInput`은 재생 연결을 끊어 `IsAttached`를 false로 만들지만 Display의 소유권은 남긴다.
3. [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs) 생성자의 `PlacementGhost.Configure`와 `FieldThornPresenter` 생성을 찾는다. 연결 검사가 생성보다 늦으면 거절된 호출도 씬을 바꿀 수 있는 이유다.

## 2. Unity EditMode 확인

Combat 씬이 열려 있으면 사용자의 씬을 보존하기 위해 실제 씬 테스트가 Ignore된다. 저장할 작업을 저장한 뒤 Combat이 아닌 씬에서 실행한다. 테스트는 Combat 씬을 추가로 열고, 끝나면 테스트가 연 씬을 닫고 기존 EventSystem을 복원한다.

1. Test Runner의 EditMode에서 `CombatDisplayReuseTests` 전체를 실행한다.
2. 새 `AuthoredCombatScene_DuplicateAttach_PreservesGhostInputAndOwnedResources`의 두 케이스가 모두 통과하는지 확인한다.
   - `false`: 일반 연결 상태에서 두 번째 Attach를 거절한다.
   - `true`: 입력 정지 후 `IsAttached=false`인 상태에서도 두 번째 Attach를 거절한다.
   - 두 케이스 모두 기존 고스트·입력 연결·가시밭 루트가 보존되고, Detach 후 루트가 남지 않으며, 다시 Attach할 수 있어야 한다.
3. `CombatViewSystemTests`의 다음 세 테스트를 실행한다: `Attach_Twice_IsRejected`, `Attach_AfterFault_DoesNothing`, `Detach_CancelsAndReleases_AndOldCompletionDoesNotFinishTheNextAttach`.
4. 실제 Combat 진입과 종료 후 재진입에서 첫 화면·입력이 정상인지 확인한다.

검증 결과에는 통과·실패·Ignore를 구분해 남긴다. MSBuild 성공은 Unity 씬 회귀의 통과를 뜻하지 않는다.

## 3. 리뷰 지점

- 중복 Attach를 고치면서 자동 Detach·재연결을 추가했는가? 이번 변경은 기존 연결을 보존하고 거절해야 한다.
- 오류 정지 뒤 무동작과 null·초기화·활성 전투 검사의 순서가 유지되는가?
- 새 에이전트의 Phase 검토와 Unity 결과를 확인한 뒤 다음 Phase를 결정한다. SD-B는 재생 Attach 계약을 좁히는 별도 작업이다.
