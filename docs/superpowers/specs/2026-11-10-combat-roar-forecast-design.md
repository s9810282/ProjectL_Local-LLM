# 전투 포효 예고(범위·밀림 미리보기) 설계 초안

작성: 2026-11-10 · 상태: **결정 완료(2026-11-10 사용자 "확인하면서 권장안대로 진행해" = Q-RF1~Q-RF6 모두 권장 A, Q-RF1 A = 이 세션이 Core 조회 구현).** Task 계획은 [roar-forecast-plan](../plans/2026-11-10-combat-roar-forecast-plan.md). 계획에서 정한 것(TP-1~TP-10) 중 설계와 다른 것은 TP-2(공개 미리보기의 관찰자 인자)·TP-3(반응 공격의 공개 미리보기 결과)이며, 둘 다 RF2 "지형은 플레이어 시점"·"대상 미리보기"를 이루는 방법이다.

## 0. 요청

사용자(2026-11-10): "포효 발동 카운트가 1이 남았을 때는 바닥의 포효 범위를 보여줬으면 좋겠는데.
1. 바닥의 범위 표기 2. 범위 내에 포효에 밀릴 수 있는 유닛이 존재한다면 기존 밀치기 미리보기와 동일한 방식으로 미리 보기"

## 1. 지금 코드에서 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | 포효 카운터·임계값은 캐릭터 사실의 특성 상태 `RoarTraitStatusSnapshot`(`HitCount`·`HitThreshold`)로 View에 이미 온다. HUD가 `4/5`처럼 쓰고 있다. | `CombatObjectiveDisplay.DescribeTrait` |
| F2 | Stage1Boss 값: 임계 5, 피격 +1, 플레이어 턴 종료 +1, 자기 턴 종료 0. | `Unit_Stage1Boss.asset` |
| F3 | 포효는 명령 정산 뒤 Core가 붙인다. 조건 = 밤 전투 국면·전투 진행 중·필드 생존·공격 제한(무장 해제·재구성) 아님·카운터 ≥ 임계. | `BossRoarCalculator.PendingBosses` |
| F4 | 포효 공격은 보스 프로필(전체 지정 All)로 하는 **반응 공격**이다. 범위 칸은 반응 공격 계산(`CombatAttackCalculator.Reaction`)이 내고, 대상이 없어도 칸(`emptyArea`)을 돌려준다. 넉백도 같은 계산 안이다. | `BossRoarCalculator.TryRoar` |
| F5 | 포효를 미리 계산하는 Core 조회는 **없다**. 공격 미리보기(`Attack`·`AttackRange`)는 지금 차례의 플레이어 행동자만 답한다. `UnitAttackRange`(호버 조회)는 보스의 공격 범위 칸을 주지만, 영향 칸·넉백은 없다. | `CombatAttackQueries.Range`·`UnitRange` |
| F6 | 반응 공격 계산 입구 `Run`에 `publicPreview`·`reactionActor` 인자가 둘 다 있다. 둘을 함께 켠 경로가 맞게 도는지는 Core에서 확인해야 한다. | `CombatAttackCalculator.Run` |
| F7 | View 밀치기 미리보기는 `CombatKnockbackPreview.Show(in AttackPreview)` → 캔버스 `ICombatKnockbackCanvas`(`FieldView`) 하나다. 도착 칸 층 `KnockbackDestination` 하나와 화살표 묶음 하나뿐이라, 동시에 한 벌만 그린다. | `CombatKnockbackPreview`·`FieldView:544` |
| F8 | Core 미리보기는 Buffer 슬롯 하나(`CombatViewPreviewKind`)로 묻는다. 배경 요청(이동 범위·복귀 견적·호버·행동 가능 여부)은 `CombatPreviewSlotRule`로 슬롯을 나눠 쓴다. | `CombatPreviewSlotRule` |
| F9 | 강조 층은 `uint`라 새 층을 더할 자리가 있다. | `FieldHighlightLayers.cs:13` |

결론: **범위 칸과 밀림을 정확히 보이려면 Core 조회가 하나 필요하다**(F4·F5). View가 모양·넉백을 따라 계산하면 SSOT가 깨진다.

## 2. 설계

### RF1 언제 보이나 — View 규칙 하나

`CombatRoarForecastRule.Shows(boss, state)` 한 곳에서 정한다.

- 보스가 포효 상태를 갖고, 필드에 살아 있다.
- 남은 수 = `HitThreshold − HitCount`가 **1 이하**다(Q-RF2).
- 전투 진행 중이고 국면이 밤 전투다(Q-RF3).

카운터 값은 Core 사실을 그대로 읽고 View가 세지 않는다. "1 남음"이라는 보이기 기준은 표시 정책이라 View가 갖는다.

### RF2 무엇을 그리나 — Core 조회 `RoarForecast`

- Buffer 미리보기 종류 `RoarForecast = 10`을 더한다. 입력은 보스 ID 하나다.
- Core는 `TryRoar`와 **같은 반응 공격 계산**을 지금 상태에서 공개 미리보기로 돌린다. 카운터를 0으로 되돌리는 것만 미리보기에서도 똑같이 하고, 결과는 게시하지 않는다.
- 새 결과 `RoarForecastResult`
  - `BossId`
  - `AreaCells`: 포효 범위 칸(대상이 없어도 채움 — `CombatRoarEvent.AreaCells`와 같은 원본)
  - `Attack`: 대상이 있으면 `AttackPreview`(넉백 기록·대상 점유 칸 포함), 없으면 비어 있음
- 지형은 플레이어 시점이다. 호버 조회 `UnitRange`와 같다(Q-UR2 A 선례).
- 공격 제한 등으로 지금 포효할 수 없으면 실패 결과(칸 없음)다.

### RF3 언제 묻나 — 예고 소유자 `CombatRoarForecastTracker`

이동 범위 소유자(`CombatMovementRangeTracker`)와 같은 꼴의 배경 요청이다.

- 결과를 그린 뒤 RF1이 참인 보스가 있으면, 슬롯 규칙(`CombatPreviewSlotRule`)에 따라 한 번 묻는다. 답을 받으면 사실이 바뀔 때까지 다시 묻지 않는다.
- 결과 재생 중(P < S)에는 숨긴다. 재생이 끝나면 새 사실로 다시 묻는다(Q-RF5).
- RF1이 거짓이 되면(포효함·보스 사망·낮) 지운다. 연결 해제 때도 지운다.
- 보스가 여럿이면 EntityId 순으로 하나씩 묻는다. 지금 데이터는 하나다.

### RF4 바닥 범위 — 새 강조 층 `RoarForecast`

- 층 표 `FieldHighlightLayers`에 한 줄을 더한다. 평소에도 그리고, 칸 채움 + 테두리다.
- 색은 `FieldView` 씬 값 `roarForecastColor` 하나다. 기본값은 경고용 진한 주황으로 두고, 씬에서 조정한다.
  - **대신함(2026-11-14):** 이 색은 View DB `StageFieldViewData`의 필드 색 묶음(`Colors`)으로 옮겼다([필드 색 DB 설계](2026-11-14-combat-field-colors-db-design.md) FC1). 기본값은 옮기기 전 씬 값(보라)이다.
- 그리는 순서는 공격 관련 층(공격 범위·영향 칸·넉백 도착)보다 아래에 둔다. 공격 조준이 이 위에 보이게 하려는 것이다.

### RF5 밀림 미리보기 — 같은 캔버스를 공유

밀치기 미리보기의 모양·색·화살표를 그대로 쓴다(사용자 "동일한 방식"). 캔버스가 한 벌뿐이므로(F7) **출처 둘을 하나가 고른다**(Q-RF4).

- `CombatKnockbackPreview`가 출처 둘(공격 조준, 포효 예고)을 기억한다.
  - 공격 조준 미리보기가 있으면 그것을 그린다.
  - 없으면 포효 예고를 그린다.
- 공격 조준 미리보기를 지우면 포효 예고가 다시 보인다.
- 대상 점유 칸·멈춘 이유 → 표지 규칙은 지금 함수 그대로다.

### RF6 바꾸지 않는 것

- Core 포효 규칙·카운터·발동 시점은 바꾸지 않는다.
- 포효 연출(`CombatRoarEventHandler`)과 HUD 카운터 문구도 바꾸지 않는다.

## 3. 정할 것

| 질문 | 선택지 | 권장 |
|---|---|---|
| **Q-RF1** Core 조회는 누가 만드나 | A 이 세션이 구현(사용자 명시 지시 필요, Core 전달 문서 §5에 기록) / B 천명님께 요청 문서만 쓰고, View는 답이 온 뒤 | A(빨리 확인 가능). B면 그동안 **범위만** `UnitAttackRange`로 먼저 보이는 임시안이 가능하다(F5: 보스 범위 칸 = 포효 지정 칸이지만 영향 칸과 다를 수 있음). |
| **Q-RF2** 보이는 기준 | A 남은 수 ≤ 1(카운터가 임계 이상인데 무장 해제 등으로 못 한 경우도 포함 — 제한이 풀리면 바로 포효함) / B 정확히 1일 때만 | A |
| **Q-RF3** 국면 | A 밤 전투 국면만(포효가 일어날 수 있는 때만) / B 낮 배치에도 보임(카운터는 낮에도 남음) | A |
| **Q-RF4** 밀림 화살표가 공격 조준과 겹칠 때 | A 공격 조준 우선, 끝나면 포효 예고로 돌아옴(캔버스 하나) / B 둘 다 동시에(화살표 묶음·도착 층을 하나 더) | A(같은 대상에 화살표 두 벌이 겹치면 헷갈림) |
| **Q-RF5** 결과 재생 중 | A 숨기고 재생 뒤 다시 계산 / B 재생 중에도 옛 예고 유지 | A(재생 중 유닛이 움직여 옛 예고가 틀림) |
| **Q-RF6** 범위 표시 모양 | A 칸 채움 + 테두리(낙석 경고와 같은 꼴) / B 테두리만 | A |

## 4. Phase(안)

1. **RF-a Core 조회**(Q-RF1 A일 때): 미리보기 종류·결과 형식·`CombatAttackQueries.RoarForecast`(반응 계산 공개 미리보기, F6 확인)·Buffer 연결. 화면 변화 없음.
2. **RF-b 바닥 범위**: 규칙 `CombatRoarForecastRule`, 소유자 `CombatRoarForecastTracker`, 강조 층·씬 색, 묶음·Factory 연결(민형님 공유).
3. **RF-c 밀림 미리보기**: `CombatKnockbackPreview` 출처 둘.
4. **RF-d 문서·공유**: Core 전달 문서 §5(Core 파일 변경), 민형님 Factory 공유, 폴더 배치 안내, 묶음 끝 검토 하나.

## 5. 원칙 점검(예상)

- SSOT: 범위·넉백 원본은 Core 반응 계산 하나, 보이기 기준은 View 규칙 하나, 그리기는 기존 캔버스 하나.
- SOLID: 예고 소유자는 수명(요청·보관·지우기)만, 그리기는 기존 부품. 밀림 캔버스의 출처 고르기는 `CombatKnockbackPreview` 안 한 곳.
- FW: 새 서비스 없음. Buffer 종류 한 줄, Factory 조립 줄만.
- 독립성: 포효 연출·HUD·공격 조준 코드는 그대로(공격 조준은 넉백 계약을 지금처럼 부름).
- 예상 7기준 약 8.8(알려진 문제 기준). 남는 위험: 배경 요청이 하나 더 늘어 슬롯 대기가 길어질 수 있음.

## 6. 구현 메모 (2026-11-11, RF-a~RF-c 완료, Ua·Ub·Uc 문제 없음 — 사용자, 새 시험 없음)

실행 기록은 [계획](../plans/2026-11-10-combat-roar-forecast-plan.md) §3. 설계와 다르게 된 곳만 적는다.

- **RF1 보이는 기준의 자리:** 별도 클래스 대신 조회 객체 `Input/Interaction/CombatRoarForecastTracker`의 private 정적 함수 `IsImminent` 하나(쓰는 곳이 하나, TP-12). 공격 제한(무장 해제·재구성)은 View가 다시 보지 않고 Core 실패 답이면 범위 없음.
- **RF2 Core:** 공개 미리보기의 관찰자를 고를 끝 선택 인자 `previewObserver`를 공격 계산 사슬에 더했다(기본 = 공격자 진영 = 옛 동작, 예고만 플레이어, TP-2). 반응 공격도 공개 미리보기일 때는 `AttackPreview`를 만든다(TP-3). 포효 조건·카운터 되돌림·전체 지정 검사는 `BossRoarCalculator` 공유 함수로, 제한 이유는 `CombatAttackQueries.RestrictionFailure`로 모았다. 기록 Core 전달 문서 §5.83.
- **RF3 조회 자리:** 입력 소유자 목록(`CombatInteraction`)이 아니라 연결 입력 묶음(`CombatSceneInput`)이 갖고 입력 분배 Tick 끝이 HUD 행동 가능 여부 조회 바로 앞에서 맞춘다(포인터와 무관하고 다른 입력 중에도 보여야 해서, TP-11). 사실 정체성은 (전투 번호, `QueryRevision`) — 미리보기 응답으로는 오르지 않아 다시 묻는 반복이 없다. 묻는 조건은 행동 가능 여부 조회와 같은 안정 상태라 **적 턴에는 보이지 않는다**(TP-13).
- **RF4·RF5 계약 주인:** 처음(RF-b)엔 `FieldView`가 범위 계약을 구현했으나, 넉백을 함께 받으려고 RF-c에서 넉백 미리보기의 주인인 `CombatAttackFeedback`으로 옮겼다(계약 하나 = 범위 칸 + 공격 미리보기 목록, TP-19). 넉백 출처 고르기는 `CombatKnockbackPreview` 한 곳: 조준 응답에 넉백 기록이 있으면 조준만, 없거나 지우면 포효 예고(TP-18).
- **확인 중 있었던 일:** "이동한 유닛 화살표 없음" 보고는 다시 확인하니 정상(계획 §3.4, 임시 진단 로그는 지움).
- **묶음 끝 검토 반영(계획 §3.5·§3.6):** 전경 입력(공격 모드·이동·배치·아이템 드래그) 중에는 포효 넉백만 숨기고 바닥 범위는 남긴다 — 공격 조준이 호버마다 자기 넉백을 지우는 사이에 포효 화살표가 끼어들던 것(M1). 그래서 RF5의 "조준이 넉백을 그리는 동안 조준 먼저"는 "전경 입력 중에는 포효 넉백 없음"이 됐다. 넉백 도착 칸이 행동 중 층이라 평소 화면에 포효 넉백이 있으면 GridOutline이 행동 중 모양이 되는 것은 받아들였다(L2).
- **공유(모두 미전달):** Core 전달 문서 §5.83(천명님, 확인 부탁 둘), 민형님 Factory 공유 §55, 현강님 구조 공유 §28.
- **정할 것:** 포효 범위 색을 적 공격 범위 색과 같게 할지(지금 따로 — 코드 기본 (1, 0.35, 0.1), 씬 적 공격 범위 (1, 0.157, 0)).
