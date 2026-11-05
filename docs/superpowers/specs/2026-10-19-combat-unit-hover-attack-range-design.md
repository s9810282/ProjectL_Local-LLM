# 전투 유닛 호버 공격 범위 설계 (2026-10-19)

상태: **구현 완료·Unity 확인 문제 없음(2026-10-19). 이동 모드 행위자 호버 제외 §9(2026-10-30, 코드 완료·컴파일 미확인·Unity 확인 대기). 오브젝트·장애물 위험 범위 확장 §8(2026-10-20, Q-H1~Q-H4 모두 A, HZ-a·HZ-b 코드 완료·Unity 확인 대기, Task·실행 기록 [hazard-hover-range-plan](../plans/2026-10-20-combat-hazard-hover-range-plan.md)). 결정 Q-UR1·Q-UR2 A, Q-UR3 B(U-2 뒤 A에서 바꿈, §4), 계획 P1 A. 기록은 계획 §7.2~§7.4.** Task·실행 기록은 [호버 공격 범위 계획](../plans/2026-10-19-combat-unit-hover-attack-range-plan.md). 결정 뒤 [이동 모드 범위 상시 표시](2026-10-19-combat-move-mode-range-design.md)(MR-1, `4b6e7320`)가 같은 슬롯을 쓰게 되어 F5·UR4를 그 코드 기준으로 고쳤다(§4.1).

> 2026-10-29 사용자 요청("적 호버링 시에는 이동 범위 + 그 위에 공격 범위", "아군 호버도 이동 및 공격 둘다"): 유닛 호버는 **이동 칸 + 그 위 공격 범위 칸**을 그린다. 이동 칸은 같은 유닛 공격 범위 조회 응답의 `MoveCells`(Core `UnitMoveCells`, 차례·비용 무시·플레이어 시점 지형, 공격 실패여도 담김)이고, 층은 아군 `AllyHoverMoveRange`·적 `EnemyMoveRange`(모든 공격 범위 층 아래), 색은 `FieldView.allyHoverMoveRangeColor`·`enemyMoveRangeColor`다. 요청·슬롯 규칙은 그대로다. 정책 원본은 [호버 이동 범위 설계](2026-10-29-combat-overlap-inset-enemy-move-range-design.md)(EM1·EM2).

## 0. 요청과 범위

- 사용자 요청(2026-10-19): 지금은 유닛이 있는 셀을 클릭하면 유닛 정보(UnitInfo)가 뜬다. **유닛에 호버하면 그 유닛의 공격 범위가 보였으면 좋겠다.**
- 사용자 결정(2026-10-19): 대상은 **모든 필드 유닛**(적·차례가 아닌 아군 포함)이고, 필요한 **Core 조회도 이번에 직접 구현**한다. CLAUDE.md의 "사용자가 명시적으로 지시한 경우" 예외이며, 데이터 경계 전달 문서(`handoffs/2026-09-24-view-core-data-boundary-handoff.md`) §5에 기록한다.
- 범위 밖: 클릭 UnitInfo 동작, 공격 모드 표시·조준, 구조물·장애물(발리스타 등)의 범위, 적 행동 예고.

## 1. 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | 클릭은 `CombatViewSystem.RouteInput`이 SelectPressed에서 `ICombatUnitInspection.InspectUnit`을 부른다. 호버에 대한 유닛 반응은 없다. 포인터 이동은 배치 Layer면 배치 입력(Drag가 아니면 호버 칸 강조만), 전투 Layer면 공격 모드일 때 공격 조준, 아니면 경로 입력으로 간다. | `CombatViewSystem.cs:997-999`, `CombatInputRouter.ResolvePointerTarget`, `PlacementDragInteraction.PointerMoved` |
| F2 | View는 공격 범위를 계산하지 않는다. 공격 모드에 들어가면 `AttackTargetingInteraction`이 Core 미리보기 `AttackRange(actor)`를 요청하고, 응답의 `RangeCells`를 `ShowRangeCells`로 `AttackRange` 강조 층에 그린다. | `AttackTargetingInteraction.RequestRange`·`ApplyRangeAnswer` |
| F3 | Core `CombatAttackQueries.Range`의 행위자 검사는 전투 국면·PlayerInput·**현재 행위자**·플레이어 소유를 요구한다. 적·차례가 아닌 아군·배치 국면은 `NotActiveUnit`·`WrongPhase`로 실패한다. | `CombatAttackQueries.Actor` |
| F4 | Buffer에는 사거리 숫자(`UnitStatsSnapshot.AppointRange`)만 있고 모양·셀은 없다. 셀 계산에는 지정 방식 모양(`AppointRangeCalculator`), 모래폭풍 사거리 보정(`EnvironmentTerrainRules.AppointRange`), 전달 검사(`CombatAttackDelivery.ValidateAim`), 대포 인접 금지(`CannonAppointExclusionCalculator`)가 들어간다. View가 다시 만들면 Core 규칙의 사본이 된다(P6·SSOT 위반). | `CombatAttackQueries.Range` 본문 |
| F5 | 미리보기 슬롯은 하나이고 새 요청이 덮어쓴다. 요청자는 배치 Drag(누름~뗌), 경로 지정(누름 뒤), 공격 모드, 그리고 경로 입력이 매 Tick(`Refresh`) 맞추는 배경 요청 둘이다(MR-1 뒤, 순서는 범위 → 복귀 견적). **이동 모드 범위**는 같은 사실로 한 번 받으면 슬롯을 비우고(`ConsumeMovementRange`의 `ClearPreview`) 다시 요청하지 않지만, 답을 기다리는 중에 슬롯을 잃으면 내려놓고 다음 `Refresh`에서 다시 요청한다. **복귀 견적**은 행위자가 이번 차례에 이동했으면 슬롯이 자기 요청이 아닐 때마다 다시 요청한다(답을 받았어도). 호버 요청을 그냥 같은 슬롯에 넣으면 둘 중 하나가 다음 Tick에 덮어써 호버 응답이 오지 않는다. | `CombatPathInput.RefreshMoveModeRange`·`RefreshReturnEstimate`·`OnPreview`, `CombatReturnRequest.OwnsPreview` |
| F6 | Core 미리보기 처리는 Plan이 PlayerInput 국면에서 "요청 번호 > 응답 번호"일 때 `CombatPreviewJob` → `CombatPreviewService.ProcessLatestPreview`. 종류 switch는 모르는 종류에서 예외를 던지고, Job은 응답이 기록되지 않으면 예외를 던진다. 새 종류도 실패를 포함해 **항상 응답을 게시**해야 한다. | `CombatPlan.Decide` 10~11행, `CombatPreviewJob.Execute`, `CombatPreviewService` |
| F7 | 강조 층 `FieldHighlightLayer`는 byte 8비트를 모두 쓴다. 층을 더하려면 형식을 넓혀야 한다. | `FieldHighlightLayers.cs` |
| F8 | 캐릭터는 한 칸만 점유한다(발자국은 장애물·구조물만). `CombatCharacterQueries.FindAnyAt`가 칸의 살아 있는 필드 캐릭터를 찾는다. | `CombatCharacterQueries.FindAnyAt` |
| F9 | `Range`는 지형을 행위자 소유자 시점(`Environment.VisibleTo(actor.Owner)`)으로 투영한다. 적 유닛을 같은 식으로 계산하면 플레이어가 모르는 지형(적만 아는 칸)이 사거리 차이로 드러날 수 있다. | `MovementEnvironmentFacts.VisibleTo` |
| F10 | 입력 규칙: 호버는 나갈 때·비활성·재연결·상호작용 불가 때 한 번 지운다. 포인터 콜백에서 할당·컨트롤별 raycast를 하지 않는다. | `Assets/AGENTS.md` |

## 2. 설계

### UR1. 동작 정의

| 항목 | 정의 |
|---|---|
| 언제 | 입력 국면(PlayerInput), 배치·전투 Layer 모두. 결과 재생·후처리·대기·종료 중이 아님. 다른 입력 소유자(배치 Drag·아이템 Drag·공격 모드·경로 지정)가 비활성. 포인터가 UI 위가 아님. |
| 무엇 | 포인터 칸의 살아 있는 필드 캐릭터(아군·적·메인·보스). 같은 유닛 위에서 움직이면 다시 요청하지 않는다. 유닛 밖·빈 칸이면 지운다. |
| 보임 | Core 응답의 `RangeCells`를 그린다. 아군은 공격 모드와 같은 `AttackRange` 층·색, 적은 `EnemyAttackRange` 층·색(Q-UR3, U-2 뒤 B로 바꿈). 기본 공격이 불가능한 유닛은 Core가 실패로 답하므로 표시하지 않는다. |
| 공격 모드 | 공격 모드 중에는 호버 범위를 그리지 않는다. 공격 모드에 들어가기 전에 호버 범위를 먼저 지운다. |
| 클릭 | UnitInfo와 독립. 클릭해도 호버 범위·UnitInfo 둘 다 지금처럼 동작한다. |
| 결과 | 결과 재생 전에 지운다. 결과 확인 뒤 마지막 포인터 위치로 다시 판단한다(유닛이 움직였을 수 있음). |

### UR2. Core 조회 `UnitRange` (사용자 지시 구현)

- 계약(Contracts): 새 결과 `UnitAttackRangeResult`(Succeeded·FailureReason·UnitId·RangeCells). `CombatPreviewResult`에 `UnitRange` 칸과 `ForUnitAttackRange`를 더한다(내용은 하나만이라는 불변식 유지).
  - `AttackRangeResult`를 다시 쓰지 않는다. 조준 목록·AP 판정은 "지금 이 행위자가 공격하면"이라는 행동 질문의 답이라, 조회에서 빈 값으로 채우면 뜻이 흐려진다.
- 계산(Application): `CombatAttackQueries.UnitRange(input)`.
  - 검사: 유닛 존재·필드·생존·기본 공격 가능·여러 칸 보류(지금 `Actor`의 유닛 쪽 검사). 차례·국면·소유자 검사는 하지 않는다. 끝난 전투는 `StageEnded`.
  - 시점: 항상 플레이어(`VisibleTo(Owner.Player)`, Q-UR2). 플레이어 유닛은 지금 `Range`와 같은 값이다.
- 공유(SSOT): `Range`와 `UnitRange`가 같은 함수 둘을 부른다.
  - 유닛 검사: `Actor`에서 유닛 쪽을 `UnitReady`로 분리한다. `Actor` = 차례·국면 검사 + `UnitReady`.
  - 범위 칸: `Range` 본문의 칸 계산(모양·사거리 보정·보드 안·전달 검사·대포 인접 금지)을 `RangeCells`로 분리한다.
- 진입: `CombatCommandCalculator.GetUnitAttackRange`, `CombatPreviewService`의 종류 switch 두 곳(명령 종류 매핑·결과 생성).
- Buffer(View 정의 칸): `CombatViewPreviewKind.UnitAttackRange = 8`, `CombatViewPreviewData.UnitAttackRange(EntityId unit)`.

### UR3. 입력 정책

- `CombatInputPolicy.CanPreview`에서 `UnitAttackRange`는 **국면 무관 종류**다: 공통 조건 + PlayerInput + 실제 Layer가 지금 국면의 Layer. `StageOf(CombatViewPreviewKind)`는 이 종류에 단계를 돌려주지 않고, 국면 무관 판별은 `CanPreview` 한 곳에서 한다.
- `CombatInputPolicy.CanInspectUnit(facts, hasPending)` = `CanPreview(UnitAttackRange)` && 재생 중 아님. 호버 범위 허용의 유일한 식이다.

### UR4. 미리보기 슬롯 공유 (Q-UR1)

**A (권장) 같은 슬롯 + 유휴 사용 규칙 두 줄**

- R-a: 호버 요청은 슬롯에 **답을 받지 않은 다른 요청**이 있으면 기다린다(Tick마다 다시 본다). 슬롯이 비었거나 들어 있는 요청이 이미 답을 받았으면(`CombatInputFacts.PreviewAnswer`가 있음) 요청한다. 자기 요청을 잃어도 다시 덮어쓰지 않고, 슬롯이 다시 한가해질 때 요청한다. 자기 응답을 받으면 슬롯을 비운다(이동 모드 범위와 같은 방식).
- R-b: 화면 갱신용 배경 요청은 **지금 사실로 답을 받았으면** 슬롯을 잃어도 다시 요청하지 않는다. 이동 모드 범위는 MR-1에서 이미 이 규칙이다(보이는 사실로 판단). 이번에 **복귀 견적**에 더한다: `CombatReturnRequest`에 "답 받음"을 두고 `Reset` 때 지운다. 사실이 바뀌는 길(결과 전·입력 불가·Drag 취소·이동 모드 떠남·복귀 버튼)은 모두 `CancelInputs` → `ClearReturnRequest` → `Reset`을 거친다.
- 수렴: 이동 모드 범위 → 복귀 견적 → 호버 순서로 각각 한 번씩 답을 받는다. 범위가 답을 기다리는 동안에는 복귀 견적·호버가 기다리고, 복귀 견적이 기다리는 동안에는 호버가 기다린다. 서로 덮어쓰는 반복이 없다.
- 바뀌지 않는 것: Buffer 계약, Plan·Job, Core 처리 경로.
- 대가: 호버 기능이 복귀 견적 조건 한 곳(`RefreshReturnEstimate`)에 닿는다.

**B 조회 전용 둘째 슬롯**

- CombatViewBuffer에 조회 요청, CombatStateBuffer에 조회 응답·응답 번호, Core 게시, Plan 단계와 새 Job, 진행 사실 두 칸, 조정자의 응답 전달을 더한다.
- 입력 미리보기와 완전히 독립이다.
- 대가: 공유 Buffer 계약·Plan·읽기 계약 시험 가짜 여러 곳을 바꾼다(파일 15개 안팎).

### UR5. View 호버 소유자 `CombatUnitRangeHover`

- 소유: 마지막 포인터 좌표, 호버 유닛 ID, 자기 요청 번호, 표시 중 여부. 모두 표시 캐시이며 원본이 아니다.
- 입구: 포인터 이동 / 폴링(R-a 재시도, 결과 확인 뒤 재판단) / 응답(자기 번호·유닛일 때만 반영, bool) / 결과 전(표시·요청 정리, 포인터는 유지) / 지우기(포인터도 잊음: 포커스 상실·Layer 변경·정지).
- 그리기: 좁은 계약 `ICombatUnitRangeFeedback.ShowUnitRange(cells, enemy)`·`ClearUnitRange`(Q-UR3 B 뒤, 아군 = 공격 모드와 같은 층, 적 = 적 공격 범위 층). 자기가 그렸을 때만 지운다.
- 슬롯: Buffer의 최신 요청이 자기 번호일 때만 비운다(`AttackTargetingInteraction`과 같은 방식).
- 할당: 포인터 콜백은 칸 집기와 `FindAnyAt`만 한다. 요청은 유닛이 바뀔 때만, 셀 배열은 응답 때 한 번 만든다.

### UR6. 연결

- `CombatInteraction`: 다섯째 입력 기능(선택 인자, 없으면 아무것도 하지 않음).
  - 포인터 이동이 Placement·Path 대상이고 다른 소유자가 비활성이면 호버로 넘긴다.
  - 다른 소유자가 활성되는 길(누름 뒤·공격 진입·손패/아이템 집기)에서 호버를 지운다. 공격 진입은 공격 쪽 `Enter` 전에 지운다.
  - 응답·폴링·결과 전·확인 뒤·표현 정리·정지에 한 줄씩 더한다.
  - "다른 소유자 활성" 판단은 지금 `IsCursorSnapMode`와 같은 식이다. 이름 있는 속성 하나로 두고 둘이 같이 쓴다.
- `CombatSceneInputFactory`: 생성·주입(민형님 Factory 공유 문서에 절 추가).
- 조정자(`CombatViewSystem`): 폴링(`Poll`)·확인 뒤(`AfterAcknowledged`)에 사실·캐릭터를 넘기는 인자 정도만 바뀐다. 조정자는 지금처럼 Interaction 수명 메서드만 부른다.
- 새 파일 위치: 폴더 정리(M 계획)가 먼저 들어가면 `Input/Interaction/`(호버 칸 표시와 같은 곳), 아니면 지금 평면 폴더에 두고 M 계획 §3 대응표에 한 줄 더한다.

### UR7. 시험

- Core(관리 실행 가능):
  - 적·차례가 아닌 아군·배치 국면에서 `UnitRange` 성공.
  - 현재 행위자는 `Range`와 같은 칸(공유 함수 회귀).
  - 손패·사망·없는 ID·기본 공격 불가는 실패, 끝난 전투는 `StageEnded`.
  - 미리보기 서비스가 새 종류에 응답을 게시(실패 응답 포함).
- View 순수:
  - 정책 표(배치·전투 Layer, Automatic·재생·대기·종료 거절).
  - 호버 소유자: 유닛이 바뀔 때만 요청, 빈 칸·유닛 밖에서 지움, 남의 응답 무시, 답 안 된 슬롯 기다림(R-a), 결과 전 정리, 확인 뒤 재판단, 자기 요청만 비움, 공격 진입 전 지움.
  - 복귀 견적 R-b(답 받은 뒤 슬롯을 잃어도 다시 요청하지 않음, Reset 뒤에는 다시 요청).
- Unity 확인: 배치·전투에서 적·아군 호버, 이동한 뒤 호버, 공격 모드 들어가기·나오기, UI 위, 결과 재생 중.

## 3. 원칙 점검

| 원칙 | 점검 |
|---|---|
| SSOT | 범위 칸 규칙은 Core 한 곳(`Range`·`UnitRange`가 같은 함수). View는 응답만 그린다. 호버 허용 식은 Policy 한 곳. 슬롯 유휴 규칙(R-a·R-b)의 원본은 이 문서 UR4, 코드 주석은 여기를 가리킨다. |
| SOLID | 호버 소유자는 호버 범위 하나만 맡고 Interaction은 위임만 한다(SRP). 조회 답에 행동 답 칸을 섞지 않는다(ISP). 기존 계약은 서명 그대로 두고 종류·멤버만 더한다(OCP). |
| FW | 서비스 등록 없음. 기존 Buffer → Plan → Job → Core 경로와 기존 Router·InputSystem을 쓴다. |
| 독립성 | 호버 범위 변경은 호버 소유자·정책·Core 조회에 한정된다. A안은 복귀 견적 조건 한 곳에 닿는다(B안은 0). 클릭 UnitInfo·공격 모드·경로 입력 동작은 그대로다. |

## 4. 정할 것

| # | 질문 | 선택지 |
|---|---|---|
| Q-UR1 | 미리보기 슬롯 | **A 같은 슬롯 + 유휴 규칙(권장)** / B 조회 전용 둘째 슬롯 |
| Q-UR2 | 적 유닛 범위의 지형 시점 | **A 플레이어 시점(권장, 숨은 지형이 드러나지 않음)** / B 그 유닛 소유자 시점(적이 실제로 쓰는 값) |
| Q-UR3 | 색 | **A 공격 모드와 같은 AttackRange 색(권장)** / B 적 범위는 다른 색(강조 층 byte → ushort, 팔레트 칸 추가) |

**결정(2026-10-19, 사용자): Q-UR1 A · Q-UR2 A · Q-UR3 A.** **Q-UR3은 U-2 확인 뒤 B로 바꿈(2026-10-19, 사용자 "AttackRangeColor 적이랑 아군이랑 색 다르게"):** 아군 유닛 호버·공격 모드는 지금 공격 범위 층·색(`attackRangeColor`), 적 유닛 호버는 새 층 `EnemyAttackRange`(강조 층 byte → ushort)·새 색 `enemyAttackRangeColor`(`FieldView` 씬 값, 기본 주황빛 빨강). 호버는 좁은 표시 계약 `ICombatUnitRangeFeedback`(`ShowUnitRange(cells, enemy)`·`ClearUnitRange`)으로 그리고, 진영은 집을 때 Buffer의 소유자로 정한다. 기록은 계획 §7.4.

### 4.1 결정 뒤 보정 (2026-10-19, 코드 대조)

- MR-1(`4b6e7320`)이 이동 모드 동안 범위를 계속 요청·표시하게 되어, 전투 Layer 이동 모드에서는 배경 요청이 둘(범위 → 복귀 견적)이다. F5와 UR4 A안을 그 코드 기준으로 고쳤다. R-b는 이동 모드 범위가 이미 따르므로 이번에 더하는 곳은 복귀 견적 하나다.
- 이동 모드 범위(`MoveRange` 층)와 호버 범위(`AttackRange` 층)는 함께 보일 수 있다. 그리기 순서에서 공격 범위가 위다(`FieldHighlightLayers.DrawOrder`). 이동 모드는 "다른 입력 소유자 활성"이 아니다(지정 중일 때만 `IsPathPlanning`).
- R-a·R-b의 코드 자리(MR 평가 M2: 슬롯 사용 규칙의 코드 소유자 없음)는 계획 §0.3 P1에서 정한다.
- 호버는 씬 조립(`CombatSceneInputFactory.CreateSceneInteraction`)에서만 만든다. 배치·공격만 검증하는 조립(`CreateInteraction` 직접 호출)에는 넣지 않아, 그 조립을 쓰는 기존 시험은 그대로다. 씬 경로 시험의 영향은 계획 U-2에서 조사한다.

## 5. Phase (결정 뒤 Task 계획에서 자세히)

1. **U-0 기준선**: 관리 실행으로 지금 작업 트리의 기존 시험 상태를 잡는다. 카메라 K 변경이 커밋 전이지만 이 작업과 파일이 겹치지 않는다.
2. **U-1 Core 조회·Buffer 종류**: UR2 + Core 시험. 화면 동작 변화 없음. 데이터 경계 문서 §5 기록.
3. **U-2 View**: UR3~UR6 + View 시험. Factory 공유 문서 절.

Phase마다 멈추고 보고한다.

## 6. 공유·기록

- Core 파일 변경(`CombatAttackQueries`·`CombatCommandCalculator`·`CombatPreviewService`·계약 두 개): 데이터 경계 전달 문서 §5, 천명님.
- Factory(`CombatSceneInputFactory`): 민형님 Factory 공유 문서.
- 다른 작업자 단독 파일(`CombatEnvironmentHover` 등)은 건드리지 않는다.

## 7. 원칙 평가 (2026-10-19, 코드 변경 없음)

사용자 요청: "이번 작업 범위 SSOT와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성으로 평가 진행해." 기준은 [설계 세션 인계](../handoffs/2026-09-30-combat-view-design-session-handoff.md) §4(4.1·4.2·4.3·4.6). 대상은 U-1(Core 조회·Buffer 종류)·U-2(View 호버·슬롯 규칙)·§4 Q-UR3 B(적·아군 범위 색)의 코드·시험·문서다(모두 커밋 전 작업 트리). 근거는 코드 대조와 계획 §7.2~§7.4의 검증(관리 실행 9,001개 기존 상태 변화 0, Unity 확인 두 번 문제 없음). 점수는 1~10(10 = 지적할 것 없음).

### 7.1 결론

| 기준 | 점수 | 깎은 이유 |
|---|---|---|
| SSOT | **8** | 규칙·값의 원본은 한 곳씩이다(범위 칸 `RangeCells`, 유닛 조건 `UnitReady`, 허용 `CanInspectUnit`, 슬롯 규칙 `CombatPreviewSlotRule`, 색 `FieldView`). −1: 공격 범위 층을 공격 모드와 아군 호버가 함께 쓰고 배타는 호출 순서에 기댄다(L1). −1: "슬롯 요청에 답이 났나"를 두 식으로 판단한다(L5). |
| SOLID | **7** | 새 클래스는 일이 하나씩이고 좁은 계약을 받는다(I·D 지킴). −1: 입력 묶음 `CombatInteraction`의 수명 갈래 열두 곳에 호버를 한 줄씩 더했다(L6, 재평가 수용 범위 안이지만 늘었다). −1: 호버 표시가 공격 표시 `CombatAttackFeedback` 안에 들어갔다(L2). −1: `CombatPathInput` 781줄은 그대로다(MR 평가 M1, 이번 작업은 조건 두 곳을 규칙 함수 호출로 바꿔 +9줄). |
| FW 지침·계약 | **9** | 서비스·키·`AddModule`·등록 변경 0, 조립은 입력 Factory 한 곳(공유 §17), Buffer는 View 정의 칸만 더함, Core 변경은 사용자 지시·전달 문서 §5.46, 규칙 값은 Core 응답(P6). −1: 마우스가 움직이는 프레임에 UI 가림 Raycast가 최대 네 번 돈다(L4, 할당은 없음, 이번 작업이 더한 것은 한 번). |
| 변경 독립성 | **8** | 색 값·호버 동작·범위 규칙·새 배경 요청은 각각 한 곳에서 바뀐다(MR 평가 M2 해소). −1: 새 입력 기능을 더하면 `CombatInteraction` 열두 곳을 같이 읽고 고친다(L6). −1: 강조 층을 하나 더하면 층 순서와 GridOutline 채우기 순서 두 표를 고친다(L3, 기존 구조). |
| **종합** | **8.0** | 네 기준 평균. MR-1 평가(7.3)의 M2(슬롯 규칙 코드 소유자 없음)가 풀렸다. |

발견: 높음 0, 중간 0, 낮음 6(L1~L6), 참고 4(R1~R4). 이번 작업 안에서 바로 고친 것은 문서 두 줄(§7.6).

### 7.2 SSOT

| 사실 | 원본 | 다른 곳 | 판정 |
|---|---|---|---|
| 공격 범위 칸(모양·사거리 보정·전달 검사·대포 인접 금지) | Core `CombatAttackQueries.RangeCells` | 행동 범위 `Range`·조회 `UnitRange`가 같은 함수, View 계산 0 | 지킴 |
| 유닛이 공격할 수 있는 상태 | Core `UnitReady` | `Actor`·`UnitRange`가 같은 함수 | 지킴 |
| 적 범위의 지형 시점 | Core `UnitRange`(플레이어 시점) | 없음 | 지킴(숨은 지형 노출 막음) |
| 호버 허용 | `CombatInputPolicy.CanInspectUnit` | 국면 무관 종류 판별은 `CanPreview` 안 한 곳 | 지킴 |
| 슬롯 배경 요청 규칙 | 문서 UR4 · 코드 `CombatPreviewSlotRule` | 범위·복귀 견적·호버가 같은 함수, MR4는 여기를 가리킴(§7.6) | 지킴 |
| 배경 요청의 "답 받음"·"기다림" | 요청자마다 자기 사실(범위 = 보이는 사실, 복귀 = `IsAnswered`, 호버 = 답 받은 유닛) | 함수는 받기만 함 | 지킴(사실이 서로 다름) |
| 슬롯 요청에 답이 났나 | `CombatPreviewSlotRule.IsIdle`(슬롯 번호 ≤ Core 응답 번호) | `CombatViewSystem.ReadCurrentPreviewAnswer`(최신 응답 번호 == 슬롯 번호) | **원본 둘(L5)**. 묻는 것(한가함·응답 객체)이 달라 지금은 맞다 |
| 유닛 진영(적·아군) | Buffer `CharacterData.Owner` | 호버가 집을 때 읽음, 표시는 층·색만 정함 | 지킴 |
| 범위 색 | `FieldView` 씬 값(`attackRangeColor`·`enemyAttackRangeColor`) | 팔레트를 거쳐 칸 색·테두리·채우기가 모두 읽음 | 지킴 |
| 공격 범위 층의 주인 | 공격 모드(`AttackTargetingInteraction`)와 아군 호버(`CombatUnitRangeHover`) 둘 | 배타는 묶음이 공격 진입 전에 호버를 내려놓는 순서 + `CombatAttackFeedback._unitRangeLayer` 기억 | **주인 둘(L1)** |

### 7.3 SOLID

| 원칙 | 판정 | 근거 |
|---|---|---|
| S | 부분 | 새 `CombatUnitRangeHover`(호버 범위 하나)·`CombatPreviewSlotRule`(순수 규칙)·`UnitAttackRangeResult`(조회 답만)는 일이 하나다. 반면 공격 표시 `CombatAttackFeedback`이 호버 표시와 그 기억(`_unitRangeLayer`)을 같이 갖고(L2), `CombatPathInput`은 781줄 그대로다(MR M1). |
| O | 부분 | 새 미리보기 종류·새 층·새 입력 기능을 "더하기"로 넣었지만, 입력 기능 수명 갈래(L6)와 층 우선순위 두 표(L3)는 기존 파일을 여러 곳 고쳐야 한다. Core 결과 switch의 기본 갈래가 배치 미리보기라 새 종류를 빠뜨리면 예외 없이 틀린다(R1, 이번엔 명시로 피함). |
| L | 지킴 | `ClearUnitRange`는 `ShowUnitRange`가 그린 층만 지운다. 한 인자 `Poll(preview)`는 "경로만"으로 문서화했다. `MarkAnswered`는 요청 없이 부르면 예외다. |
| I | 지킴 | 호버는 18멤버 `ICombatInteractionFeedback` 대신 두 멤버 `ICombatUnitRangeFeedback`을 받는다(재평가 N5 방향). 공용 계약에 멤버를 더하지 않아 다른 작업자 시험 가짜를 건드리지 않았다. |
| D | 지킴 | 호버는 Buffer 읽기·쓰기 계약, 집기·가림·표시 계약, Gate만 받고 Factory가 넣는다. 정책·규칙은 정적 순수 함수다. |

### 7.4 FW 지침·계약

| 항목 | 판정 |
|---|---|
| FW-001~003 서비스·등록·Build | 변경 없음. 새 객체는 모두 내부 협력 객체이고 입력 Factory가 만든다(공유 §17) |
| FW-004 내부 상태 우회 | 없음. View는 자기 입력 Buffer 슬롯만 쓰고, Core는 기존 미리보기 서비스·Job 경로로 답한다 |
| FW-005~007 입력 | 바인딩·Configure·Submit 경로·Router 변경 없음 |
| FW-010~012 렌더 | 새 렌더 자원 없음. GridOutline 채우기 칸 하나가 기존 생성·해제 반복에 들어간다 |
| 경계(§4.5) | Core 파일 변경은 사용자 명시 지시이고 전달 문서 §5.46에 적었다. Framework 공용 파일 변경 없음 |
| P6 규칙 값은 Core | 지킴(View 범위 계산 0, Buffer에는 요청 종류만) |
| 할당·성능 | 포인터 콜백은 칸 집기·유닛 찾기·가림 판정만 하고 할당이 없다. 다만 가림 Raycast가 Router·호버·환경 설명(입력 직후와 Tick 끝)에서 따로 돈다(L4). 매 Tick 사실 읽기가 한 번 늘었다(R2, struct) |
| 코딩 규칙 | Allman·한국어 XML doc·`sealed`·`.meta` GUID 32자리(새 파일 일곱) |

### 7.5 변경 독립성 (대표 시나리오)

| 시나리오 | 고칠 파일 | 읽어야 할 코드 | 검증 | 판정 |
|---|---|---|---|---|
| 적·아군 범위 색 바꾸기 | `Combat.unity` FieldView 값 | 없음 | 화면 | 좋음 |
| 호버 동작 바꾸기(예: 적만 보임, 지연 표시) | `CombatUnitRangeHover` | 슬롯 규칙 함수 | 호버 순수 시험 | 좋음 |
| 공격 범위 규칙 바꾸기(새 지형 보정 등) | Core `RangeCells` | Core 범위 계산 | Core 조회·범위 시험(공격 모드·호버가 함께 따라감) | 좋음 |
| 슬롯을 쓰는 새 배경 요청 | 새 소유자 + `CombatPreviewSlotRule` 호출 | 규칙 함수(경로 입력 순서 코드는 안 읽음) | 새 소유자 시험 + 수렴 시험 | 좋음(M2 해소) |
| 결과 전후처리에 참여하는 새 입력 기능 | 기능 + `CombatInteraction` 수명 갈래 여러 곳 + 입력 Factory | `CombatInteraction` 전체 | 상호작용 시험 | 부분(L6) |
| 강조 층 하나 더하기 | 층·팔레트·`FieldView`·아웃라인 채우기 순서 | 층 순서와 채우기 순서 두 표 | 아웃라인 시험(슬롯 수) | 부분(L3) |
| 공격 모드 범위 표시 바꾸기 | `CombatAttackFeedback` | 호버 범위 기억까지 | 공격·호버 시험 | 부분(L1·L2) |
| 다른 작업자 영역 | Core(사용자 지시, 기록함)·Factory(공유) | — | — | 경계 지킴 |

### 7.6 발견과 권고

| ID | 등급 | 내용 | 권고 |
|---|---|---|---|
| L1 | 낮음 | 공격 범위 층을 공격 모드와 아군 호버가 함께 쓴다. 배타는 `CombatInteraction`이 공격 진입 전에 호버를 내려놓는 순서와 표시 쪽 기억에 기댄다. 지금 모든 공격 진입 길이 `AttackModeRequested`를 지나 맞다. | 아군 호버 전용 층(색은 `attackRangeColor` 그대로)을 둔다. ushort라 비트 여유가 있다. 아웃라인 슬롯·채우기 칸이 하나 늘어난다. |
| L2 | 낮음 | 호버 표시(`ShowUnitRange`·`ClearUnitRange`·그린 층 기억)가 공격 표시 `CombatAttackFeedback` 안에 있다. | L1과 함께 작은 표시 부품(가칭 `CombatUnitRangeDisplay`)으로 떼면 공격 표시 변경이 호버를 읽지 않는다. |
| L3 | 낮음 | GridOutline 채우기 우선순위(`FieldOutlineRenderer.TryGetFillKind` 사슬)가 층 순서(`FieldHighlightLayers.DrawOrder`)와 따로다. 이번에 둘 다 고쳤다(기존 구조). | 채우기 종류를 층 순서에서 파생하거나, 채우기 표를 층 정의 옆에 둔다. 다음에 층을 더할 때 같이 정리. |
| L4 | 낮음 | 마우스가 움직이는 프레임에 UI 가림 Raycast가 최대 네 번 돈다 — Router 1·호버 1(이번 작업)·환경 설명 2(입력 직후 `RouteInput`의 동기화와 Tick 끝 동기화, 기존). 마우스가 멈춰 있어도 Tick 끝 환경 설명 1번은 매 프레임 돈다(기존). 자료 재사용으로 할당은 없고, 한 번의 비용은 씬의 Raycast 대상 UI 수에 비례한다(Combat 씬 GraphicRaycaster 2개). 측정 전이라 실제 ms는 모른다. | 판정을 한 곳에서 한 번만 한다: `CombatPointerMapper`가 같은 프레임·같은 화면 좌표의 UI 판정을 기억하면(부르는 쪽 변경 없음) 프레임당 1번이 된다. |
| L5 | 낮음 | "슬롯 요청에 답이 났나"를 `CombatPreviewSlotRule.IsIdle`(번호 비교)과 `CombatViewSystem.ReadCurrentPreviewAnswer`(응답 객체 대조) 두 곳에서 판단한다. | 규칙 함수가 "답 났음"도 함께 내게 하고 조정자가 그것을 쓰게 한다. 급하지 않다. |
| L6 | 낮음 | `CombatInteraction`의 수명 갈래(포인터·누름·손패/아이템 집기·공격 진입·응답·폴링·결과 전·표현 정리·입력 취소·강제 선택 종료·정지) 열두 곳에 호버 한 줄씩을 더했다. 재평가(2026-10-15) 시나리오 표가 "새 입력 기능 = 기능 + `CombatInteraction` + 입력 Factory"를 수용했지만 갈래 수가 늘었다. | 입력 기능의 공통 수명 계약(내려놓기·지우기·결과 전·폴링)을 두고 묶음이 목록을 돈다. 폴더 정리 뒤 별도 리팩터 후보. |
| R1 | 참고 | Core `CombatPreviewService`의 결과 switch 기본 갈래가 배치 미리보기라 새 종류를 빠뜨려도 예외가 없다(기존). | Core 작업자 몫. 기본 갈래를 예외로 바꾸는 것을 전달 문서에 한 줄 제안할 수 있다. |
| R2 | 참고 | 조정자 Tick이 `Poll` 오버로드에 사실을 넘기려고 `ReadFacts()`를 한 번 더 부른다(struct, 할당 없음). | 이동 모드 범위 평가 R1(사실 여러 번 읽기)과 함께 정리. |
| R3 | 참고 | `CombatPathInput` 781줄(MR 평가 M1). 이번 작업은 범위·복귀 조건을 규칙 함수 호출로 바꿨다. | 범위 소유자 분리 설계(RT, Q-R3 B = 이 작업 → 폴더 정리 → 분리)대로. |
| R4 | 참고 | 카메라가 움직여도 포인터가 그대로면 호버 칸을 다시 집지 않는다(환경 설명과 같은 한계). | 필요해지면 카메라 이동 알림에서 다시 집기. |

이번 평가 중 바로 고친 문서: 설계 UR5 "그리기" 줄(옛 `ShowRangeCells` 계약 → `ICombatUnitRangeFeedback`), 이동 모드 범위 설계 MR4 머리에 슬롯 규칙 원본(UR4·`CombatPreviewSlotRule`)을 가리키는 줄. 코드는 바꾸지 않았다.

## 8. 확장: 오브젝트·장애물 호버 위험 범위 (2026-10-20, 결정 완료·HZ-a·HZ-b 코드 완료)

### 8.0 요청과 범위

- 사용자 요청(2026-10-20): "유닛이 아니라 오브젝트, 옵티클 등에도 호버링 시 공격 범위 표시. 공격을 하는 타입의 경우에만."
- 이 절이 §0의 범위 밖 "구조물·장애물의 범위"를 대신한다. 유닛 호버(UR1~UR7)는 그대로다.

### 8.1 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| H-F1 | 필드 원본 중 남을 다치게 하는 것은 **지뢰**와 **화약통** 둘뿐이다(아래 표). | 아래 표의 근거 열 |
| H-F2 | View에 필요한 사실은 이미 Buffer에 있다: `CombatStateData`의 `Environment.Tiles`(지뢰 칸·활성·`KnownTo`), `EnvironmentBindings.ObjectSourceIds`·`ObstacleEntityIds`, `ObstacleContents`(장애물 종류), `Structures`(생사·기준 칸), `BoardWidth`·`BoardHeight`. 환경 설명 `CombatEnvironmentText`가 같은 사실을 읽는다(플레이어에게 알려진 활성 원본만, 분류된 장애물만). | `CombatStateData`, `CombatEnvironmentText.Build`·`IsKnownToPlayer` |
| H-F3 | 범위 모양은 Core 안의 internal 함수 둘뿐이라 View가 부를 수 없다. Core 시험(`MineEnvironmentTests`·`ObstacleReactionTests`)이 그 함수를 직접 부른다. | `MineMovementTileRule.Cells`(public 클래스·internal 함수), `ObstacleReactionCalculator.BarrelCells`(internal 클래스) |
| H-F4 | View가 Core의 공개 순수 함수를 직접 부르는 선례가 있다. | `CombatFactEventHandler` → `ItemCharacterChanges.WithItems`(가방 P-3) |
| H-F5 | 호버 소유자 `CombatUnitRangeHover`는 칸을 집어 캐릭터만 찾고(`FindAnyAt`), 표시 계약은 `ShowUnitRange(cells, bool enemy)`다. 결과 재생 전에 내려놓고 끝나면 마지막 포인터로 다시 집으므로, 지뢰가 터지거나 화약통이 부서진 뒤에는 새 사실로 다시 판단한다. 생성자가 이미 상태 Buffer를 받는다. | `CombatUnitRangeHover.Pick`·`Suspend`, `ICombatUnitRangeFeedback` |

H-F1 표:

| 원본 | 남을 다치게 하나 | 피해 칸 | 언제 | 범위 함수(Core) | 호버 표시 |
|---|---|---|---|---|---|
| 지뢰 `MineSourceFact`(오브젝트) | 예 | 중심 포함 3×3(보드 밖 제외), 유닛·구조물 모두 | 누가 그 칸에 들어설 때 | `MineMovementTileRule.Cells` | **대상** |
| 화약통(장애물 = 구조물 + 프로필 `PowderBarrel`) | 예 | 기준 칸 둘레 8칸(자기 칸 제외), 유닛·구조물 모두, 다른 화약통으로 연쇄 | 부서질 때 | `ObstacleReactionCalculator.BarrelCells` | **대상** |
| 보급품(장애물)·깃발·보안관 배지(오브젝트) | 아니오 | — | — | — | 안 함 |
| 회오리 `TornadoSourceFact` | 아니오(위의 유닛을 옮김) | — | — | — | 안 함 |
| 가시밭 | 그 칸에 들어선 유닛만 | 자기 칸 | 진입 때 | — | 안 함(더할 정보 없음) |
| 기차(레일 기믹) | 예 | 일정에 따라 선로 위 | 기믹 발동 때 | 기믹 계산 | 범위 밖(§8.6) |
| 구조물(본부 등) | 아니오 — `StructureDefinition.IsAttackable`·`AttackPower` 칸은 있지만 DB의 모든 구조물 에셋에서 꺼져 있고(`isAttackable: 1` 0개) 전투 계산이 읽지 않는다 | — | — | — | 안 함(공격 규칙이 생기면 Core 조회부터) |

### 8.2 설계

**HZ1. 동작.** 호버 칸에 살아 있는 필드 유닛이 있으면 지금 그대로다(유닛 우선). 없으면 그 칸의 위험 원본을 찾는다: 활성이고 플레이어에게 알려진 지뢰, 살아 있는 화약통 장애물. 있으면 그 피해 칸을 **바로** 그린다(Core 왕복·미리보기 슬롯 없음). 없으면 지운다. 한 칸에 위험 원본이 여럿이면 피해 칸의 합집합이다.

**HZ2. 범위의 원본(Q-H1).** Core에 공개 입구 `CombatHazardAreas`(`Application/Combat/`, 새 파일)를 둔다 — `MineCells(origin, width, height)`·`PowderBarrelCells(anchor, width, height)`. 본문은 지금 두 함수에서 옮기고 옛 두 함수는 한 줄 위임으로 남긴다(동작 동일, 기존 Core 시험 무수정). View는 이 입구만 부른다(H-F4 선례). Core 파일을 고치므로 사용자 지시가 있어야 하고, 데이터 경계 전달 문서 §5.47에 기록한다.

**HZ3. 찾기 규칙.** View 순수 함수 `CombatHazardLookup.Collect(state, cell, into)` 하나가 칸의 위험 원본을 찾아 피해 칸을 채운다. "플레이어에게 알려진 원본인가"·"분류된 장애물의 프로필은 무엇인가"는 환경 설명과 같은 규칙이므로 공유 도우미로 떼어 환경 설명과 찾기 규칙이 함께 부른다(사본 없음).

**HZ4. 호버 소유자.** `CombatUnitRangeHover` 하나가 유닛·위험 둘 다 맡는다. 같은 표시·같은 수명 갈래를 쓰기 때문이다(소유자를 따로 두면 `CombatInteraction` 수명 갈래 열두 곳의 호출이 두 배가 되고(L6), 같은 층을 두 소유자가 쓴다). `Pick`: 유닛 → 지금 길(Core 요청) / 아니면 위험 → 바로 표시하고 자기 요청은 내려놓는다. 그린 출처(유닛 ID 또는 위험 칸)를 기억해 같은 출처면 다시 그리지 않는다. 클래스 이름은 그대로 둔다(폴더 정리 때 이름 바꾸기 후보).

**HZ5. 표시 계약.** `ShowUnitRange(cells, bool enemy)` → `ShowHoverRange(cells, CombatRangeTone tone)`(`Ally`·`Enemy`·`Hazard`), `ClearUnitRange()` → `ClearHoverRange()`(이름은 계획 P2). 위험 색은 Q-H2.

**HZ6. 정책.** 유닛과 같은 `CombatInputPolicy.CanInspectUnit`(배치·전투 국면, 결과 재생 아님, 대기 명령 없음)을 쓴다. 호버 규칙을 하나로 둔다.

**HZ7. 시험.** Core 입구(옛 함수와 같은 칸, 모서리·작은 보드) / 찾기(알려진·모르는·비활성 지뢰, 화약통 생사, 보급품은 없음, 같은 칸 둘의 합집합) / 호버(위험은 요청 없이 즉시·슬롯 그대로, 유닛 칸 → 위험 칸 이동, 결과 뒤 다시 집기에서 터진 지뢰 지움, 유닛 우선) / 표시(색조별 층). 기존 시험 고침: 표시 계약 가짜 셋 서명, Q-H2 A면 팔레트 생성자 두 곳·아웃라인 슬롯 수.

### 8.3 원칙 점검 (예상)

| 기준 | 예상 | 이유 |
|---|---|---|
| SSOT | 9 | 피해 칸 모양 원본은 Core 한 곳, 공개·분류 판단은 환경 설명과 공유 도우미 하나 |
| SOLID | 7 | 호버 소유자가 출처 둘(유닛·위험)을 고른다(바뀌는 이유가 하나 늘어남). 찾기 규칙은 따로 떼어 소유자는 고르기만 한다 |
| FW 지침·계약 | 9 | 서비스·키·Factory 변경 없음(호버 생성자 그대로), Core 입구는 상태 없는 순수 함수 |
| 변경 독립성 | 8 | 위험 종류 추가 = Core 입구 함수 하나 + 찾기 한 갈래. 표시 색 변경은 표시 쪽만 |

예상 약 8.3/10. 남는 것: L1·L2(공격 범위 층·호버 표시 위치, §7.6) 그대로.

### 8.4 정할 것 — 결정 완료(2026-10-20, 사용자 "Q-H1~Q-H4 모두 A로 정해. Core 공개 입구는 내가 지시하는 거야.")

Q-H1~Q-H4 모두 **A**. Q-H1 A의 Core 파일 변경(공개 입구 `CombatHazardAreas`)은 CLAUDE.md의 "사용자가 명시적으로 지시한 경우" 예외이며 데이터 경계 전달 문서 §5.47에 기록한다. Task·실행 기록은 [위험 범위 계획](../plans/2026-10-20-combat-hazard-hover-range-plan.md).

| Q | 질문 | 권장 | 다른 안 | 이유 |
|---|---|---|---|---|
| Q-H1 | 피해 칸 모양을 View가 어디서 얻나 | **A** Core 공개 입구 `CombatHazardAreas`(Core 파일 변경 — 사용자 지시 필요) | B Core 미리보기 새 종류(왕복·슬롯 경쟁) / C View에 같은 모양 사본 | 공개 사실만으로 정해지는 순수 값이라 왕복은 이득이 없고, 사본은 SSOT 위반 |
| Q-H2 | 위험 범위 색 | **A** 새 층 `HazardRange` + `FieldView.hazardRangeColor`(기본 보라 — 계획 P3에서 노랑을 바꿈(노랑은 호버·행위자가 씀), 씬에서 조정) | B 적 공격 범위 색 재사용(층·팔레트 변경 없음) | 지뢰·화약통은 아군·적을 가리지 않아 "적" 색과 뜻이 섞인다 |
| Q-H3 | 화약통 연쇄 폭발 | **A** 그 화약통 자신의 8칸만 | B 연쇄로 닿는 칸까지 합침(Core 연쇄 규칙 조회가 더 필요) | Core 규칙 함수 그대로라 사본이 없다. 연쇄는 이웃 화약통에 호버하면 보인다 |
| Q-H4 | 평가 L1·L2를 같이 고치나 | **A** 이번엔 안 함 | B 표시 계약을 바꾸는 김에 호버 표시를 작은 부품으로 뗌(L2) | 기능 변경과 구조 정리를 나눈다(변경 독립성) |

### 8.5 Phase (결정 뒤 계획에서 자세히)

- **HZ-a** Core 입구 `CombatHazardAreas`(동작 동일, 옛 함수 위임) + Core 시험 + 데이터 경계 §5.47.
- **HZ-b** View 공유 도우미·찾기 규칙·표시 계약 색조·호버 연결(Q-H2 A면 강조 층 하나) + 시험.

### 8.6 범위 밖

기차 선로 위험(일정·진행 방향까지 보여야 해서 따로), 지뢰 연쇄 효과, 구조물 공격(규칙 없음), 클릭 정보.

## 9. 이동 모드 행위자는 호버하지 않음 (2026-10-30, 코드 완료·컴파일 미확인·Unity 확인 대기)

- 사용자 요청(2026-10-30): "유닛 이동 모드에서는 선택된 유닛은 호버링 안 되게 해야 함". 이동 모드에서는 고른 행위자의 이동 범위가 이미 보이는데, 그 유닛에 포인터를 올리면 호버가 이동 칸 + 공격 범위를 그 위에 또 그렸다.
- 규칙: **이동 모드인 행위자는 유닛 호버 대상에서 뺀다.** 그 칸에 포인터가 있어도 호버 범위를 그리지 않고 조회도 내지 않는다(그 칸의 위험 칸도 찾지 않음). 다른 유닛(적·차례가 아닌 아군) 호버는 그대로다.
- 이동 모드 판정의 원본은 경로 입력의 `PlanningActor(Path)` 하나다(이동 모드 범위·Drag 시작과 같은 조건: 입력 가능·포인터 대상 경로·`CanEditPath`·`PlayerInput` 국면·고른 행위자·이동 방식 있음). 호버는 좁은 계약 `ICombatMoveModeReader.MoveModeActorId`(새 `Input/Interaction/ICombatMoveModeReader.cs`, `CombatPathInput` 명시 구현)로만 읽고, 씬 조립(`CombatSceneInputFactory`)이 경로 입력을 넘긴다. 공격 모드는 지금처럼 호버 전체가 쉰다(공격 조준이 입력 소유자).
- 낡은 판단 다시 보기: 호버 중인 유닛을 클릭해 고르면 그 유닛이 이동 모드 행위자가 되므로 다음 Tick에 내려놓고, 건너뛴 행위자가 이동 모드를 벗어나면(선택이 바뀜 등) 포인터가 그대로여도 다시 판단한다(`IsMoveModeStale`, 건너뛴 유닛 하나만 기억). 응답이 그 사이에 와도 이동 모드 행위자 것이면 그리지 않는다.
- 정한 것(확인 대상): 이동 모드 조건은 남은 행동력을 보지 않아(이동 방식만 봄), 이동·공격을 마친 뒤에도 고른 채로 두면 그 행위자는 계속 호버에서 빠진다. 이동 모드 범위와 같은 조건을 쓰기 때문이다(그때 이동 모드 범위는 비어 보임).
- 바뀐 파일: `CombatUnitRangeHover.cs`(생성자 인자 하나·`Pick` 건너뛰기·`Poll` 다시 보기·응답 거르기), `CombatPathInput.cs`(계약 구현 두 줄), 새 `ICombatMoveModeReader.cs`(+ `.meta`), Factory `CombatSceneInputFactory.cs` 생성 줄 인자 하나(민형님 줄 아님 — 이 줄은 호버 U-2 때 우리가 넣음).
- Unity 확인: ① 이동 모드에서 고른 유닛 위에 포인터 → 이동 모드 범위만 보이고 호버 이동·공격 범위 없음 ② 다른 아군·적 위 → 지금처럼 호버 ③ 고른 유닛 위에 포인터를 둔 채 공격 모드 켜고 끄기 → 끈 뒤에도 호버 없음 ④ 다른 아군 위에서 호버가 보이는 중 그 유닛 클릭(선택) → 호버가 꺼지고 이동 모드 범위만 ⑤ 배치 국면이나 아무 유닛도 고르지 않았을 때는 지금처럼 모든 유닛 호버.
