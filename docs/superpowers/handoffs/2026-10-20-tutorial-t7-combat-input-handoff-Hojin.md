# T-7 튜토리얼 이동 실습 입력 — 전투 View 전달 메모

- 작성자: 호진(이호진)
- 작성: 2026-10-20. 전달용 문서이며 외부 발송은 하지 않았다.
- 정책 원본: [튜토리얼 설계 §15.10](../specs/2026-10-18-tutorial-buffer-job-public-contract-design-Hojin.md#1510-전투-view-수정--이동-실습-이동만-허용전투-작업자-동의).
- Task·검증 원본: [구현 계획 §9·§12.12](../plans/2026-10-19-tutorial-implementation-plan-Hojin.md).

- **현재 정책은 2026-10-26 V4-g, 현재 입력 구조 인계는 문서 끝의 V5-d를 따른다(미전달).** 아래 10-20의 턴 종료/빈 범위 제한과 각 Phase의 미확인은 과거 이력이다. 새 Core의 이동 실습은 수동 턴 종료로 회복하며, 미완료 실습의 Deployment에서는 정상 조건의 전투 재진입을 허용한다. V4-n과 V5-a/b/c는 각각 사용자 정상 보고로 인수했고 실행/정적 근거·확인 한계는 [5라운드 계획 §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)에 둔다.

## 적용 동작

이동 실습과 실습 통과 뒤 19번 설명이 시작되기 전까지 `MoveOnly`를 적용한다. 공격 전환·복귀·턴 종료 버튼은 비활성화하며 키 입력도 실행 전에 차단한다. 현재 요청에 대한 성공한 빈 이동 범위 응답이 있을 때만 기존 전투 자격을 만족하는 턴 종료를 허용한다. 이 예외는 사용자에게 재확인했다. (→ 바뀜: 2026-10-26 V4-g — 빈 범위 응답 없이도 MoveOnly의 수동 턴 종료를 허용한다. 아래 V4-g 절.)

금지된 키 입력 등 Router에 도착한 실제 시도는 기존 `ReportBlockedInput` 요청으로 안내한다. 비활성 버튼 클릭·호버·상태 갱신은 안내 요청을 만들지 않는다. 실습 통과 뒤 유지 구간의 안내 거절(`NotReady`)은 기존 Tutorial 계약을 따른다.

## 전투 코드 연결

경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/` 기준이다.

| 파일 | 변경 |
|---|---|
| `CombatInputFacts.cs` | 현재 제한과 현재 정체성에 맞는 성공한 빈 범위 사실 전달 |
| `CombatInputPolicy.cs` | 명령·미리보기 제한 판단을 한 곳에 두고 기존 전투 자격과 함께 검사 |
| `CombatActionButtons.cs` | 같은 정책으로 공격·복귀·턴 종료의 활성 상태 결정 |
| `CombatInputRouter.cs` | 복귀·공격 전환·턴 종료를 경로·모드 정리 전에 차단, 실제 시도만 한 번 보고 |
| `CombatViewSystem.cs` | 기존 Buffer 조회 경로에서 Tutorial 상태/요청 Sink 연결, 검사마다 현재 값 조회 |
| `CombatMovementRangeTracker.cs` | 자기 요청이 소비한 성공한 빈 범위의 정체성 보관·무효화 |
| `CombatPathInput.cs` | 현재 정체성 조회·해제, 복귀 견적 요청과 늦은 응답의 직접 제출 차단 |
| `CombatInteraction.cs` | 빈 범위 조회를 경로 입력 소유자에게 위임 |

`CombatReturnEstimateTracker`는 바꾸지 않았다. 호출 측에서 최신 제한을 확인하고 이미 요청한 복귀도 폐기한다. 다른 소유자의 미리보기 슬롯은 지우지 않는다. 전투·행위자·시작 칸·결과 순번이 달라지면 빈 범위 근거는 무효이며, 단순 범위 표시 정리에서는 보존하여 합법적인 턴 종료가 최종 Gate까지 통과하도록 한다.

## 구조 확인

- **SSOT:** Tutorial Buffer가 제한을 소유한다. 전투 Policy가 허용식을 소유하고, 이동 범위 Tracker만 소비한 응답 근거를 보관한다. View에서 이동 가능 규칙을 계산하지 않는다.
- **SOLID:** Policy는 값만 판단하고 Router는 전달·차단, Path/Tracker는 각 기능 수명, ViewSystem은 연결을 맡는다. 기존 공개 Sink로 의존하며 새 범용 서비스·인터페이스를 만들지 않는다. `None`의 기존 허용 조건은 유지한다.
- **FW/JMH:** 생성은 기존 `BufferSystemFactory`의 Tutorial Buffer 한 개, 내부 Sink 등록·그룹 보관은 기존 경로 그대로다. 소비자는 `IBufferQuery`에서 필요한 두 Sink만 받는다. 입력 처리는 기존 receiver→Router→Buffer 경로를 쓰며 입력 안에서 Job을 등록하거나 UI를 직접 호출하지 않는다.
- **변경 독립성:** 입력 제한의 변경은 전투 View 8개 파일에 국한했다. Tutorial UI 표시, Core, FieldCamera, Architecture, Input Actions, 씬·프리팹·DB 에셋 변경은 없다.

## 검증과 Unity 확인 항목

- MSBuild 제품·에디터 및 기존 `ProjectL.Tests.Framework` 어셈블리 빌드 종료 코드 0, 오류 0, 기준선 대비 새 경고 0.
- 기존 경고는 제품·에디터 빌드 8건, 시험 어셈블리 빌드 10건. 테스트 파일 1,355개는 Phase 시작 해시와 동일하며 추가·수정하지 않았다.
- 테스트 실행과 Unity 화면·조작 확인은 수행하지 않았다. 실제 확인 결과와 코드 리뷰 결과는 구현 계획 §12.12에 기록한다.

Unity에서는 다음을 확인한다.

1. 17 설명 뒤 이동 실습에서 공격·복귀·턴 종료 버튼이 비활성이고, 키로 시도해도 편집 중 경로가 남으며 안내만 뜨는지.
2. 실제 이동·연출 완료 뒤 19가 시작될 때까지 제한이 유지되는지. 이후 19에서는 Attack만 검정 덮개 밖에 보이고 설명 중 조작은 차단되는지.
3. 성공한 현재 이동 범위가 비었을 때 턴 종료가 가능하고, 다음 아군 차례에는 빈 범위 근거를 새로 확인하는지. 실패·미도착·지난 응답으로는 열리지 않는지. (→ 바뀜: 2026-10-26 V4-g — 이 확인 항목은 V4-g 절의 Unity 확인으로 바뀌었다.)
4. 일반 전투와 튜토리얼 완료 뒤에는 공격·복귀·턴 종료, 이동·ESC 설정이 기존대로 동작하는지.

현재 정의 에셋의 사용자 조정 값 `autoAdvanceSeconds: 999`는 보존했다. 설명을 클릭해 진행할 수 있으며 이 메모 때문에 값을 바꾸지 않는다. T-8 전체 통합 확인은 이번 Phase 범위에 포함하지 않는다.

## 2026-10-26 V4-g — 새 Core 이동 실습 회복 (미전달)

- 사용자 Q-V32·38 A/Q-T8 c 및 **"V4-g 진행해"** 지시로 구현했다. **변경 후 컴파일/기존 시험/Unity 확인은 미확인**이다. W15(f/g) 독립 검토 결과는 4라운드 계획 §6에 기록한다. 10-20의 실행 증거를 이번 성공 증거로 재사용하지 않는다.
- MoveOnly는 빈 이동 범위/선택/남은 AP 없이도 수동 EndPlayerTurn을 허용한다. 버튼·키는 기존 Policy와 Gate·PlayerInput·Layer·표시 완료·입력 닫힘 검사를 따른다. 자동 종료는 추가하지 않았고 공격/일반 배치 제한과 설명 Layer 독점은 유지한다.
- 이동 없이 수동 종료를 이어 8번째 적 응답 뒤 Deployment에 왔을 때, **같은 전투의 Running/Practice/PracticeGoal 대기**인 MoveOnly만 EnterCombat으로 재진입할 수 있다. 기존 전투 진입 확인창/Router/Gate를 사용한다. 실습 통과 뒤 다음 설명 시작을 기다리는 MoveOnly 유지 구간은 이 재진입 예외에서 제외한다.
- 이를 위해 기존 CombatBlockedInputReport가 공개 세션의 현재 전투/상태/단계/대기 사실을 읽고 Facts의 `IsTutorialPracticeGoalPending`으로 전달한다. 새 Buffer·DB 조회·FactsReader/서비스는 만들지 않았다. TutorialFactsReader는 옛 활성 유닛 주석만 정정하고, 수락된 플레이어 Command Move의 실제 진입 칸과 결과 표시 완료만 이동 통과로 읽는 코드는 유지했다. EndPlayerTurn/재진입/자동 적 이동으로 목표를 통과시키지 않는다. Jobs/Store/목표/에셋 값은 수정하지 않았다.
- 기존 CombatInputPolicyTests의 무선택 수동 종료 기대 한 곳과 사례 이름만 승인 범위에서 맞췄다. 새 사례/단언은 없다. 기존 빈 범위 추적 API는 후속 구조 정리 부채이며 광범위하게 삭제하지 않았다.
- Unity 확인: 무선택/AP 부족/이미 행동함/보류 상태→수동 종료→다음 턴 실제 이동, 미이동 상태의 8번째 적 응답→Deployment→정상 전투 진입→실제 이동, 이동 결과 표시 전 통과 없음, 통과 뒤 제한 유지 구간의 재진입 차단, 설명 Layer 독점과 금지 공격 안내를 확인해야 한다. 항상 이동이 불가능한 기획 데이터는 별도 잔여다. 외부 메시지·커밋/push는 하지 않았다.

## 2026-10-26 V4-m — 4라운드 전투 입력/기존 시험 인계 정리 (미전달)

- 현재 T-7 정책은 위 V4-g 기록이며, **사용 방법·전체 인수 항목의 원본은 [4라운드 계획 §2·§7](../plans/2026-10-25-combat-view-stage-round4-plan.md)**이다. 과거 10-20의 턴 종료/빈 범위 조건·MSBuild 기록은 이번 성공 증거가 아니다. W15(f/g) 새 에이전트 1개 검토는 완료했으며 컴파일·기존 EditMode·실제 회복은 미확인이다.
- 변경 파일: `CombatInputPolicy.cs`·`CombatInputFacts.cs`·`Input/Assist/CombatBlockedInputReport.cs`, `LifeCycleSystem/LifeCycles/Tutorial/TutorialFactsReader.cs`. MoveOnly 수동 종료와 같은 전투의 미완료 PracticeGoal 대기에서만 정상 Deployment 재진입을 허용한다. 목표·Jobs/Store·DB/에셋은 바꾸지 않았다. 실제 플레이어 Move의 진입/표시 완료 조건은 유지한다.
- 공격 Drag 시험(Q-T8 e): `Assets/Tests/EditMode/Framework/HojinCombatAttackDragTests.cs`의 Rig가 운영과 같은 Tracker를 공유하고 Enter에서 현재 Ready를 실제 Core 조회로 준비한다. 이미 Ready이면 불필요한 Calculate를 하지 않는다. `HojinCombatAttackDragIntegrationTests.cs`는 두 공격 진입 전에 현재 Ready를 기다린다. 기존 클릭/Drag·소유권·명령/결과 단언을 약화하거나 Gate를 우회하지 않았다. 새 사례를 추가하지 않았다.
- 관련 Policy 시험(Q-T8 c): `CombatInputPolicyTests`의 무선택 EndPlayerTurn 기대 1개와 사례 이름을 맞췄다. `TutorialInputTests`에는 이번 정책의 직접 기대가 없어 수정하지 않았다. UI에는 공격 확인 중 새 진입 없음/모드 끄기 유지, 공용 AP 부족 문구, 폐지된 R 복귀의 미소비가 적용된다. 설명 Layer 독점과 배치 회수/취소는 유지한다.
- Unity 확인: M08의 공격 진입/모드 끄기와 기존 Drag 시험, M11의 무선택/AP부족/행동사용/보류→수동 종료→실제 이동 및 8번째 적 응답→Deployment→정상 재진입→실제 이동을 확인한다. 이동 결과 표시 전 통과와 통과 뒤 제한 유지 구간의 재진입은 없어야 한다. 항상 이동 불가능한 데이터의 재시도/안내 정책은 별도 기획/튜토리얼 후속이다.
- 기존 빈 범위 전용 사실/API의 삭제는 후속 구조 작업이며 이번 회복 정책의 선행조건이 아니다. 코드/정적 검토 완료를 기능 인수 완료로 표시하지 않았고, 외부 발송·커밋/push는 하지 않았다.

## 2026-10-26 V5-d — 입력 사실·조립과 기존 Drag 고정물 (미전달)

- 선택은 Display 소유 CombatSelectionState 한 곳, 입력 사실은 CombatInputFactsReader가 현재 연결/Buffer에서 매번 읽는다. View Facts 명칭은 SelectedActorId이며 일반 Path도 같은 Facts를 사용한다. 빈 범위 진단만 선택 ID를 값으로 받아 Facts 역호출을 피한다. 선택은 정보창·강조·입력이 공유하고, 선택 변경이 Core의 자격/가용성 조회를 대신하지 않는다.
- Q-T9 범위로 HojinCombatAttackDragIntegrationTests는 명시적 Selection/Inspection 준비와 Facts 인자 이름을 맞췄다. HojinCombatAttackDragTests는 Facts 이름, 부분 생성 호출을 시험 CombatInputTestFactory로 이동하고 Path의 별도 선택 공급자를 없앴다. 실제 Core Ready를 준비하는 기존 고정물과 클릭/Drag·소유권·명령/결과 단언은 유지했다. 가용성/공격 Gate를 우회하거나 새 사례를 추가하지 않았다.
- 운영은 typed 확인창과 전체 필수 조립, 격리 시험은 시험 어셈블리 부분 조립 도우미를 사용한다. 새 서비스/슬롯/Layer·설명 입력 구독을 추가하지 않았다. 확인창 Fake는 명시적 receiver를 보관하며 자동 승인하지 않는다.
- MoveOnly 수동 종료/미완료 PracticeGoal 대기의 정상 재진입 정책은 유지한다. 실습 통과는 기존 수락된 플레이어 Move의 실제 진입 칸과 표시 완료로만 판단한다. Tutorial Jobs/Store/목표·기획 에셋/autoAdvanceSeconds는 수정하지 않았다. 항상 이동 불가능한 데이터의 안내/재시도 판단과 빈 범위 호환 API 정리는 별도 후속이다.
- V4-n M01~M14 및 V5-a/b/c 각각 사용자 컴파일·기존 시험·Unity 정상 보고로 기능 인수했다. W17~19 정적 검토와 V5-d 통합 정리를 완료했으며 d에는 코드/시험 수정이 없다. [최종 결과·한계/잔여 §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)이 원본이다. 실제 로그/개별 실행을 직접 검증했다고 표시하지 않는다. 외부 발송·커밋/push는 하지 않았다.
