# CombatSceneDisplay SD-D3 실습·검증 워크시트

- 실행일: 2026-10-15. Task·완료 상태 원본은 [조립 계획 §7·§10.5](../plans/2026-10-14-combat-scene-display-composition-plan.md)다.
- 기준 HEAD: `6809cefab8a8e775cb6ce958757aa26b585eb123`. D0~D2의 미커밋 변경 23파일을 포함해 시작했다.
- 비교 사본: ignored `Temp/CombatSceneDisplaySDD3/baseline/`의 원래 상대 경로. `preexisting-changes.json`에 시작 해시를 보관했다.

## 1. 구현과 소유권

| 지점 | 변경·유지 계약 |
|---|---|
| [CombatPlacementFeedback](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatPlacementFeedback.cs) | Field/Ghost/Units/Hand만 주입. 고스트·배치 격자, 원본 숨김/복원, 유효 표시, 셀/월드 위치 적용을 담당 |
| [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs) | 공개 해제 방어 뒤 배치 객체로 전달. 화면 좌표는 기존 PointerMapper로 변환한 월드 위치만 전달 |
| [CombatSceneDisplayFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs) | 같은 Field/Ghost/Units/Hand로 배치 객체를 만들고 Display에 주입 |

ShowGhost는 Ghost.Show → Field.SetPlacementActive(true), HideGhost는 Ghost.Hide → false다. 원본 숨김/복원은 Units → Hand 순서다. 각 View가 원본 대상을 기억하고 배치 객체는 별도 대상·해제 플래그·풀·Mapper 캐시를 갖지 않는다. 셀 이동은 매번 Field의 현재 Mapper와 보드 안 조건을 확인한다.

공통 Install/Clear/Release와 `_fieldBound`는 그대로다. 배치 객체에 Configure·ClearAll·Release를 추가하지 않았다. Ghost.Configure의 기존 Release와 이동 고스트의 풀·외형 전환도 기존 소유자에 남는다. 배치 표시 변경은 이 객체와 해당 View 계약을 중심으로 검토하며 공유 Ghost 자원 계약 변경에는 이동 교차 회귀가 필요하다.

## 2. 검증 선택

| 묶음 | 케이스 | 관측 |
|---|---:|---|
| 새 CombatPlacementFeedbackTests | 6 | CellPrefab/GridOutline 각각 고스트·격자·tint, 필드/손패 원본 전환·복원 및 비용/HP 보존, 보드 밖·Field 해제·재Build 뒤 현재 Mapper 이동 |
| 새 CombatPointerMappingTests 재연결 회귀 | 1 | 같은 root의 새 Display가 표시 중일 때 해제된 Display의 배치 호출이 Ghost·원본·격자를 바꾸지 않음 |
| CombatMovementPlanningDisplayTests.GhostMovesContinuouslyAndKeepsOneRental_HidesUiAndCollider_AndResetsForPlacement | 2 | 이동 고스트 → 배치 전환의 기존 외형·풀 재사용 |
| CombatDisplayReuseTests.AuthoredCombatScene_DuplicateAttach_PreservesGhostInputAndOwnedResources | 2 | StopInput 전/후 중복 Attach 거절이 기존 Ghost·입력·소유 자원을 보존 |

합계 **11케이스**이며 모두 Unity 네이티브 실행이 필요하다. 전체 Combat 씬·Catalog 없는 새 fixture도 GameObject·Renderer·TMP를 사용한다. 실제 씬 fixture는 Combat 씬이 이미 열려 있으면 Ignore하므로 다른 저장된 씬에서 실행하고 통과/실패/Ignore를 구분한다. 기존 DragPlaybackAndReturn 테스트는 이동 입력 회귀이며 배치의 대체 증거로 세지 않는다.

빌드 로그는 `Temp/CombatSceneDisplaySDD3/build.log`, 추가 Compile·중간 출력 설정은 `NewFiles.targets`다. 기존 Factory·D2 테스트와 D3 신규 파일을 Unity csproj에 없을 때만 보충하고 csproj는 수정하지 않는다. 최종 빌드 결과와 새 Phase 검사 결과는 계획 §10.5에 기록한다.

현재 Unity 세션은 유지하며 위 11케이스의 실행은 대기다. 변경 없는 FakeFeedback 입력 관리 테스트는 배치 표시 구현을 거치지 않으므로 새 표시 검증으로 실행·집계하지 않는다. 선행 A/B/C′·D0~D2의 Unity 대기는 그대로 남는다.

## 3. Phase 경계

코드·가능한 검증·문서 준비 후 새 에이전트로 검사하고 지적을 처리한다. 사용자가 이번에 D3·D4의 순차 진행을 함께 지시했으므로 D3 검사 결과를 보고한 뒤 D4로 진행한다. D5는 이번 범위가 아니다. 기존 부분 Configure·Display 인수 전 실패 복구 한계도 별도 후속으로 유지한다.
