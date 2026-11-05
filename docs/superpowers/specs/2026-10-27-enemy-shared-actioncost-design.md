# 적 진영 공용 행동 코스트 제한 설계

작성일: 2026-10-27. 상태: **사용자 승인 / Phase E1 코드 반영, 검증 생략**.

## 1. 범위와 결정 상태

- 사용자 확정: 이번 기능에 필요한 Core 수정 허용. 적 진영 공용 코스트 제한만 추가하고 기존 행동 순서는 유지한다. 설계부터 리뷰한다.
- 기존 정책 교체: `2026-10-23-player-turn-shared-actioncost-design.md` §1.1 7번·D2의 적 무예산 정책만 이번 승인 범위에서 대체한다. 플레이어 턴 정책은 유지한다.
- 제안: 적 최대 코스트는 독립 규칙 `EnemyTurnRules.MaximumActionCost` 한 곳에 둔다. 최초 값 **10으로 사용자 확정**했다. 플레이어 상수를 참조하지 않는다.
- 범위 밖: AI 정렬·대상 점수·경로 우선순위 변경, 플레이어 예산 개편, 스테이지별 SO 설정, 적 코스트 HUD, 신규 보스 행동·콘텐츠 밸런싱.
- 기존 EntityId 순서는 유지한다. 앞선 적이 예산을 먼저 쓰며 뒤의 적은 행동하지 못할 수 있다. 공평 분배·다음 턴 순서 회전은 넣지 않는다.

## 2. 조사 결과와 변경 이유

소스 기준 경로: `Assets/ProjectL/Script/Systems/RuntimeSystem/` 아래 Core는 `InGameSystem/CoreSystem/CoreCaculator/`, Buffer는 `BufferSystems/`다.

| 현재 소유자 | 확인한 동작 | 필요한 변경 |
|---|---|---|
| `Application/Combat/CombatActionAdmission` | 적 Advance는 Unbudgeted, 자금 검사 통과, 정산 지출 0 | 적 예산 검사·정산·적용 |
| `Application/Combat/EnemyAi/EnemyAiTurnService` | Available 적을 EntityId 순으로 순회, 공격 우선·실패 시 추격 이동, 수락 행동 하나씩 반환 | 순회 그대로 두고 공통 자금 검사에 맡김 |
| `Application/Combat/EnemyAi/EnemyAiPolicy` | 실제 공격 미리보기로 조준 후보 평가 | 비용 부족 후보가 공통 계산에서 거절되도록 연결 |
| `Application/Combat/CombatTurnCalculator.BeginControllerTurn` | 자기 진영 행동 기회 초기화, 플레이어 시작만 예산 충전 | 적 시작 때 적 예산만 충전 |
| `Contracts/Combat/CombatCommandState` | PlayerBudget만 보유, With·SameValues로 보존·비교 | EnemyBudget 추가·전체 상태 복사 보존 |
| `Application/Combat/MovementProjection.CurrentInput` | 상태를 직접 재생성, 플레이어 정산만 적용 | 적 정산·상태 보존 누락 방지 |
| `Contracts/Snapshots/ActionCostSettlementSnapshot` | PlayerBudget 외는 무예산 검증 | EnemyBudget 정산 검증 추가 |
| `BufferSystems/Combat/CombatStateBuffer` | 준비·행동·턴 시작의 예산/행동권 검증 | 두 진영 예산의 독립성·설치 불변식 검증 |

`CombatMovementCalculator`의 표시 비용도 현재 플레이어만 유료로 구분한다. 적의 실제 지출과 이동 결과의 비용이 다르게 남지 않도록 함께 수정한다. `CombatAttackCalculator`의 반응 공격은 명시적 Unbudgeted로 남긴다.

## 3. 게임 규칙 제안

1. 준비된 새 전투의 적 잔액은 0. 적 진영 시작이 수락될 때 최대치로 정확히 한 번 충전한다. 미사용분은 이월하지 않는다.
2. 모든 적이 하나의 예산을 공유한다. 유닛별 예산은 만들지 않는다.
3. 일반 이동은 현재 `Stats.MoveCost`, 일반 공격은 기존 공통 공격 비용 계산의 유효 비용을 쓴다. 적 전용 가격표를 복제하지 않는다. 현재 기본 이동 2, 일반 공격 3, Cannon 공격 5다.
4. 자금이 부족한 행동은 거절한다. 거절 시 잔액·행동권·세계 상태는 바뀌지 않는다. 수락 시 해당 진영 잔액과 행동권을 기존 전이로 함께 설치한다.
5. 각 적은 기존처럼 한 턴에 일반 이동 또는 공격 한 번만 한다. 이동 거리·지형·대상 선택 규칙도 그대로다.
   → **2026-10-27 대신함:** [이동 후 공격 설계](2026-10-27-move-then-attack-design.md) Q2 B로 적도 이동 → 공격을 둘 다 한다(공격 → 이동은 안 됨). 비용은 이 설계대로 적 공용 예산에서 행동마다 낸다. 이동 거리·지형·대상 선택 규칙은 그대로다.
6. 공격 비용이 부족하면 기존 실패 후 이동 시도 흐름을 따른다. 예: 잔액 2, 공격 3·이동 2라면 이동을 시도할 수 있다. 이것은 새 AI 우선순위가 아니라 기존 공격 실패 후 이동 흐름의 결과다.
7. 앞선 적이 행동할 수 없으면 다음 적을 확인한다. 남은 적 모두에게 수락 가능한 행동이 없을 때 기존 EndEnemyPhase를 호출한다. 잔액 0이라는 이유만으로 루프 앞에서 즉시 종료하지 않는다. 합법적인 무료 행동과 대기 중 반응을 누락하지 않기 위해서다.
8. 보스 포효 등 기존 반응·강제 이동·공격 내부 추가 타격은 별도 일반 행동으로 세지 않는다. 원래 무료인 반응은 계속 무료이며 일반 행동의 정산을 중복 실행하지 않는다.
9. 적 종료 후 기믹·사망 정산·8번째 적 응답 후 낮 전환·Run 종료는 기존 경계를 유지한다. 종료 시 잔액은 보존하고 다음 적 시작에서 덮어쓴다.

예시(최대 10, 모두 이동 가능): EntityId 순으로 이동 비용 2를 다섯 번 지출하면 0이다. 여섯 번째 이후 적은 유료 이동/공격을 하지 못한다. 첫 적이 Cannon 공격 5를 하면 남은 예산은 5이며 이후 행동은 같은 순서에서 가능한 것만 실행한다.

## 4. 상태·정산 계약

- 순수 불변 `EnemyActionBudget`를 `Contracts/Combat/`에 추가한다. Current, Maximum, Empty, Full, Spend 계약은 기존 PlayerActionBudget과 같은 형태로 두되 최대치의 원본은 EnemyTurnRules다. 음수·최대 초과·잔액 초과 지출은 거부한다.
- `CombatCommandState.EnemyBudget`가 런타임 잔액의 유일한 사실이다. 준비·With·SameValues·직접 생성 호출 모두 이를 보존한다. 기존 생성 호출 호환용 후미 선택 인자 기본값은 Empty를 제안하며, 상태 재구성에서 누락되는 기본값 의존은 허용하지 않는다.
- `ActionFundingKind.EnemyBudget`를 기존 enum 뒤에 추가한다. 기존 PlayerBudget=0, Unbudgeted=1은 유지한다. Unbudgeted를 삭제하거나 일반 적 행동에 계속 쓰지 않는다.
- `CombatActionAdmission`이 출처·소유자로 부담 주체를 결정한다. 호출자·View가 임의로 지정하지 않는다. CanFund/Settlement/ApplyBudget은 같은 판정 기준을 공유한다.
- `ActionCostSettlementSnapshot`은 적 예산 정산에도 Before/Final/Spent를 기록하고 `Before - Spent == Final`을 검사한다. 해당 최대치 범위도 검사한다.
- `MovementProjection.CurrentInput` 등 직접 생성 경로는 공통 ApplyBudget과 상태 With를 재사용하는 방향으로 고친다. 상태 복사 때 적 예산을 버리는 별도 구현을 만들지 않는다.
- 공격 비용의 기존 유효 비용과 정산 단계는 유지한다. 순수 계산이 반환한 전이에 비용과 행동권을 함께 담고 기존 BattleTransactions/Publisher를 통해 설치한다. 별도 서비스나 잔액 캐시를 만들지 않는다.
- 플레이어용 `CombatActionAvailability`와 HUD 공개 계약은 이번에 일반화하지 않는다. 적 예산 사실은 현재 CommandState를 통해 읽을 수 있으며 별도 View 미러를 만들지 않는다.

### Buffer 설치 불변식

| 전이 | 플레이어 예산 | 적 예산 |
|---|---|---|
| 전투 준비 | 0 | 0 |
| 플레이어 턴 시작 | 최대치로 충전 | 보존 |
| 적 턴 시작 | 보존 | 최대치로 충전 |
| 플레이어 일반 행동 | 해당 정산과 일치 | 보존 |
| 적 일반 행동 | 보존 | 해당 정산과 일치 |
| 거절·반응·그 외 경계 | 기존 규칙에 따라 보존 | 보존 |

적 정산은 Enemy 소유 행위자·Advance·Enemy 단계에서만 허용한다. 정산 없는 잔액 변조, 다른 진영 예산 변경, 잘못된 잔액, 같은 행동의 중복 설치는 거부한다. 전투 종료를 일으킨 마지막 적 행동도 정상 비용을 정산한다. 기존 상태 동등성 비교에 적 잔액을 포함하여 오래된 전이 설치를 막는다.

## 5. 구조 검토

- **SSOT:** 최대치는 EnemyTurnRules, 런타임 잔액은 Buffer의 CommandState, 가격·자금 판정은 기존 Core 계산 소유자다. AI·View에 별도 잔액/가격을 두지 않는다.
- **S:** Budget은 값 검증, Admission은 자금 판정, TurnCalculator는 충전 시점, AI는 후보 순회, Buffer는 설치 검증을 담당한다.
- **O:** 기존 funding enum에 진영 구분 하나를 추가한다. 신규 전략 시스템이나 일반화된 예산 프레임워크는 필요하지 않다.
- **L:** 무료 반응·거절 불변·수락 행동당 한 번 정산 계약을 보존한다.
- **I:** 적 예산 때문에 플레이어 UI 계약이나 서비스 인터페이스를 넓히지 않는다.
- **D:** 순수 계산은 Buffer 구현을 참조하지 않고 불변 입력/전이만 사용한다. 조립은 기존 서비스·Factory가 유지한다.
- **JMH/FW:** 신규 System·Service·Builder·모듈 등록이 필요 없다(FW-001~003). 기존 전이 설치 경계를 우회하지 않는다(FW-004). 입력·UI·렌더·이벤트·큐 계약(FW-005~016)에 신규 경로를 만들지 않는다.
- **독립성 비교:** 이후 적 최대치 조정은 EnemyTurnRules와 예산 검증 테스트에서 끝난다. AI 우선순위 수정은 EnemyAi 내부에서 끝나고 충전/Buffer를 다시 구현하지 않는다. 이번 최초 도입은 공통 상태·정산 계약 변경이므로 플레이어 예산 회귀와 반응 공격 회귀는 필요하다. 관련 없는 손패·카메라·연출·DB 조립은 수정하지 않는다.

## 6. 구현 시 검증 기준

1. 적 시작 충전·다음 적 시작 재충전·잔여 미이월, 플레이어 잔액 불변.
2. 예산에 정확히 맞는 이동/공격 수락, 부족하면 무변경 거절. 이동 거리와 코스트를 혼동하지 않음.
3. 여러 적이 예산 공유, EntityId 순서 유지, 한 유닛이 이동 뒤 공격하지 않음.
4. 공격 불가/비용 부족 후 이동 시도, 앞선 적 불가 시 뒤의 저비용 적 처리, 모두 불가 시 자동 종료.
5. 무료 반응과 대기 포효가 예산 0에서도 기존대로 처리되며 중복 차감 없음.
6. 이동 중 사망·공격으로 승리한 마지막 행동의 정산, 8번째 적 응답 후 낮 전환 유지.
7. State.With/직접 복사/SameValues에서 적 잔액 보존·차이 검출. 위조 정산·다른 진영 변경·중복 게시를 Buffer 통합 테스트로 거절.
8. 기존 무예산 적 테스트의 기대값을 적 예산 정책으로 교체하고, 플레이어 10·수동 턴 종료 회귀를 확인.

Unity 컴파일과 관련 EditMode 테스트로 검증한다. DB/SO를 변경하지 않으므로 실제 에셋 값 변경 검증은 이번 범위에 추가하지 않는다. 새 .cs는 .meta와 함께 생성한다.

## 7. 리뷰 지점과 후속 단계

사용자가 최대 코스트 10과 공격비 부족 후 이동 시도를 확정하고 Task 계획 작성 및 검증 생략 실행을 지시했다. [Task 계획](../plans/2026-10-27-enemy-shared-actioncost-plan.md)에 따라 계약·계산·Buffer를 실행 가능한 한 Phase E1로 묶어 반영했다. Phase E2 검증은 사용자 요청으로 생략했다. E1 종료 리뷰 지점에서 멈춘다.

조사 당시 CombatTurnCalculator, CombatStateBuffer와 기존 플레이어 턴 설계 및 Core 경계 인계 문서 등에 다른 미커밋 변경이 있었다. 이번에는 해당 파일을 수정하지 않았다. 구현 착수 전에 최신 변경을 재확인하고 보존한다. Core 경계 인계에는 실제 구현 Phase 완료 시 사용자 허용 범위·변경 파일·검증 결과를 기록한다.

## 8. 2026-10-27 실행 기록

EnemyTurnRules·EnemyActionBudget(.meta 포함), CommandState 생성/With/SameValues, EnemyBudget funding·정산 검증, 공통 자금 검사/적용, 이동 결과 비용, 적 시작 충전, MovementProjection 상태 보존, Buffer 준비/지출/충전 불변식을 반영했다. AI 파일·행동 순서·무료 반응은 수정하지 않았다. 기존 직접 상태 복사 두 곳(TurnCalculator.State, MovementProjection.CurrentInput)에 적 예산 보존을 연결했다. 테스트·빌드·Unity 확인은 실행하지 않았다. 기존 무예산 적 fixture/기대값 갱신은 E2에 남아 있다.
