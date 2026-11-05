# 전투 이동 모드 범위 상시 표시 설계 (2026-10-19)

- **목표:** 지금은 행위자를 Drag하는 동안에만 보이는 이동 가능 범위를, **이동 모드인 동안 계속** 보인다. 이동 모드 = 차례가 와서 아무것도 안 한 상태, 공격 버튼(이동 아이콘)으로 공격 모드를 끈 상태.
- **상태:** 설계·구현 같은 날(MR-1 한 Phase). 실행 기록은 §7, 원칙 평가는 §8(7.3/10, 중간 M1·M2).
- **대신하는 규칙:** [이동 범위·고스트 설계](2026-10-13-movement-planning-feedback-design.md) D1 표의 "취소" 행과 D2 마지막 항목("범위 사본은 Q 지정 수명 안에서만"), [이동 Drag 입력 설계](2026-10-13-combat-move-drag-input-design.md) §1 요구 1·3. 범위의 수명 규칙 원본은 이제 이 문서다.

## 0. 요청과 해석

- 사용자 요청(2026-10-19): "기존에는 드래그 시에만 나오던 이동거리, 버튼 혹은 턴 진입 후 아무것도 안한 상태 = 이동 모드일 때 보였으면 좋겠음."
- 해석: **이동 모드 = 행위자 칸을 눌러 Drag를 시작할 수 있는 상태**. 조건 원본은 `CombatPathInput.PlanningActor(Path)` 하나다(전투 Layer·공격 모드 아님·입력 가용·`CanEditPath`(대기·연출·미처리 명령 없음)·PlayerInput 국면·활성 아군이 필드에 살아 있고 `MoveMethod != None`).
- 그래서 범위가 보이는 때: 차례 시작 직후, 공격 모드를 끈 뒤(버튼·우클릭·포커스 상실), Drag를 취소한 뒤, **한 번 이동한 뒤**(남은 이동이 있으면 새 위치 기준). Core가 빈 범위를 주면(남은 이동·AP 없음) 아무것도 안 보인다.
- 보이지 않는 때: 공격 모드, 결과 재생 중, 턴 종료 대기, 적 차례(Automatic), 배치 국면.

## 1. 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | 범위는 Drag 누름에서만 요청하고(`TryBeginPlanning`) 지정 취소 때 지운다(`CancelInputs` → `ClearMovementRange`). 요청 정체성·응답 1회 소비는 `CombatMovementRangeRequest`, 사실은 `CombatMovementRangeFacts`(전투·행위자·시작 칸·결과 번호). | `CombatPathInput.cs:194-218·409-438`, `CombatMovementRangeRequest.cs` |
| F2 | 미리보기 슬롯은 하나다. 이동 모드에서 쓰는 쪽은 복귀 견적(행위자가 이번 차례에 이동했으면 매 Tick 자기 요청이 아닐 때 다시 요청), Drag(범위·경로 견적), 복귀 버튼(원함 표시)이다. | `CombatPathInput.RefreshReturnEstimate`·`RequestReturn` |
| F3 | 경로 입력은 매 Tick `Poll` → `OnPreview` → `Refresh`, 결과 확인 뒤 `AfterAcknowledged` → `Refresh`를 받는다. 이동 모드의 사실이 바뀌는 순간(결과·차례·공격 모드 출입·Layer)마다 둘 중 하나가 곧 돈다. | `CombatInteraction.Poll`·`AfterAcknowledged` |
| F4 | 공격 모드는 결과 뒤에도 범위를 새로 요청해 모드 동안 범위를 유지한다. 모드 동안 범위가 보이는 선례다. | `AttackTargetingInteraction.AfterResult` |
| F5 | 공격 모드 진입·턴 종료는 Router가 먼저 `CancelPath`(경로 입력 취소)를 부른 뒤 새 의도를 시작한다. | `CombatInputRouter.Route` EndTurn·AttackMode |
| F6 | GridOutline 모드에서 `MoveRange`는 "행동 중" 층이라, 이동 모드 동안 층 테두리·채우기가 켜진다. 격자 데칼은 2026-10-19부터 상시 표기다(2026-10-20 정식 규칙, 격자 아웃라인 설계 머리말). | `FieldHighlightState.IsActing`, `FieldOutlineRenderer.ShowDecal` |
| F7 | [유닛 호버 공격 범위 설계](2026-10-19-combat-unit-hover-attack-range-design.md)(초안) UR4가 같은 슬롯의 유휴 사용 규칙(R-a 답 안 된 요청 기다림, R-b 답 받은 배경 요청은 다시 요청 안 함)을 다룬다. | 그 문서 §2 UR4 |

## 2. 설계

### MR1. 소유자

범위 요청·표시는 지금처럼 `CombatPathInput` 하나가 맡는다(새 클래스 없음). 범위 하나를 두 쓰임 — 이동 모드와 Drag 지정 — 이 같이 쓴다. `CombatMovementRangeRequest`의 책임(요청 정체성·응답 1회 소비)은 그대로다.

- **2026-10-19 바뀜:** [경로 입력 분리](2026-10-19-combat-movement-range-tracker-design.md)(RT1~RT6) 뒤 기계(요청·소비·슬롯 소유·보임·표시)는 `CombatMovementRangeTracker`가 맡고, 언제·무엇을(MR2~MR4)은 `CombatPathInput`에 남았다. 이 문서의 수명 규칙은 그대로다.

### MR2. 보여야 할 범위는 사실에서 파생한다

- 지정 중이면 지정의 사실, 아니면 이동 모드 행위자의 사실(`CombatMovementRangeFacts`, 네 값 그대로).
- 보이는 범위는 그 사실(`_shownRangeFacts`)을 기억한다. 같은 사실이면 다시 요청하지 않는다. 결과 번호가 바뀌면(이동·공격·차례) 사실이 달라져 새로 요청한다.

### MR3. 맞추는 시점

| 시점 | 동작 |
|---|---|
| `Refresh`(매 Tick·결과 확인 뒤·취소 뒤) | 지정 중이 아니면: 이동 모드가 아니면 지운다. 같은 사실로 보이거나 기다리는 중이면 그대로 둔다. 다르면 지우고 요청한다(복귀 버튼 응답을 기다리는 중이면 요청만 미룬다). |
| Drag 시작 | 같은 사실의 범위가 보이거나 기다리는 중이면 그대로 쓴다(다시 요청 안 함, 기다리는 중이면 지금처럼 경로 견적만 유예). 아니면 지우고 요청한다. |
| 지정 취소(`CancelInputs`: 우클릭·놓기 실패·포커스 상실·Capture·복귀 버튼) | 이동 모드 사실과 같으면 남기고 아니면 지운다. 새로 요청하지 않는다. |
| 새 의도 전(`CancelPath` → `LeaveMoveMode`: 공격 모드 진입·턴 종료) | 경로 취소 + 범위 지움. 의도가 거절되면 다음 `Refresh`가 다시 맞춘다. |
| 결과 재생 전·입력 불가·Layer 변경 | 이동 모드가 아니므로 지운다(지금과 같은 결과). |
| 표시 콜백이 던진 실패(범위 표시·편집 시작·고스트 갱신) | 경로 취소 + 범위 지움(`LeaveMoveMode`). 지정 취소 도중의 정리 실패는 위 "지정 취소" 규칙 그대로다. |
| Detach | 지운다(지금과 같음). |

### MR4. 미리보기 슬롯 순서

> 2026-10-19 뒤: 배경 요청(이동 모드 범위·복귀 견적·호버 공격 범위)의 슬롯 규칙 원본은 [호버 공격 범위 설계](2026-10-19-combat-unit-hover-attack-range-design.md) UR4 R-a·R-b이고 코드는 `CombatPreviewSlotRule` 하나다. 아래 순서는 그 규칙과 `Refresh` 호출 순서에서 나온다(복귀 견적도 R-b를 따르게 됨).

- 이동 모드에서는 **범위 → 복귀 견적** 순서다. 복귀 견적은 범위 요청이 답을 기다리는 동안 요청하지 않는다.
- 범위는 같은 사실로 한 번 받으면 슬롯을 잃어도 다시 요청하지 않는다(보이는 사실로 판단). 호버 설계 UR4의 R-b와 같은 규칙이다.
- 복귀 버튼(원함)은 기다리던 범위 요청을 내려놓고 슬롯을 쓴다. 복귀가 끝나면(결과 또는 거절) `Refresh`가 다시 맞춘다.
- 지정 중이 아닐 때 남이 슬롯을 가져가면 기다리던 범위 요청을 내려놓는다(지정 중이면 지금처럼 `ValidatePlanning`이 지정을 취소한다).
- 수렴: 범위(한 번) → 복귀 견적(한 번) → 더 요청하지 않음. 서로 덮어쓰는 반복이 없다.
- 대가: 이동한 뒤 복귀 버튼 상태가 Core 왕복 한 번 늦게 바뀐다(한두 프레임).

### MR5. 바뀌지 않는 것

Core·Buffer·Plan·Job, Drag 경로 그리기·견적·동결·제출(`CombatMovementInteraction`), 공격 모드, 표시 계약(`ICombatMovementFeedback`)·강조 층·팔레트, Router의 대상 판정.

## 3. 고치는 파일

| 파일 | 변경 |
|---|---|
| `ViewSystem/Combat/CombatPathInput.cs` | MR2~MR4: 이동 모드 사실, 보이는 범위 사실, `Refresh`의 이동 모드 범위 맞춤, Drag 시작 재사용, 지정 취소의 남김, `LeaveMoveMode`, 복귀 견적 순서·복귀 버튼의 범위 요청 내려놓기, 슬롯 잃은 범위 요청 정리, 표시 실패 정리 |
| `ViewSystem/Combat/CombatInteraction.cs` | `CancelPath`가 `LeaveMoveMode`를 부른다(문서 주석) |
| 시험 | 규칙이 바뀐 순수 시험 고침, 새 순수 시험, 복귀 견적 왕복이 하나 늘어난 Unity 전용 시험 고침(§4) |
| 문서 | 옛 규칙 원본 두 곳 머리에 대신함 표시, `viewDocs/stage-input-guide.md` §2.2 |

## 4. 시험

- 새 순수 시험(`CombatMoveModeRangeTests`): 이동 모드에서 `Refresh`가 범위를 한 번 요청·표시하고 반복 `Refresh`는 다시 요청하지 않음, 범위 뒤 복귀 견적 순서와 수렴, 결과 뒤 새 사실로 다시 요청, 입력 불가·공격 모드면 지움, Drag 시작이 보이는(기다리는) 범위를 재사용, Drag 취소 뒤 범위 유지, `LeaveMoveMode`가 지움, 복귀 버튼이 기다리던 범위 요청을 내려놓고 복귀 응답을 같은 Tick에 처리, 입력 불가면 한 번 지우고 요청하지 않음.
- 고치는 순수 시험: `CombatMovementRangeFeedbackTests`의 취소·재지정·표시 콜백·정리 실패 사례(옛 규칙 "취소하면 범위를 지운다"를 단언).
- 고치는 Unity 전용 시험: 이동 뒤 복귀 견적을 한 번의 Core 왕복으로 기대하던 `CombatMovementViewTests`(3개)·`CombatDisplayReuseTests`(1개). 이동 모드 범위 왕복을 하나 더 넣는다.
- 회귀: 관리 실행 기준선(관련 시험 1,049개, PASS 653·FAIL 16 기존·NATIVE 379) 대비 상태 변화.
- Unity 확인: 차례 시작 때 범위, 공격 모드 출입, 우클릭·놓기 실패 뒤 유지, 한 번 이동한 뒤 새 범위, 복귀 버튼, 턴 종료, 적 차례, GridOutline 모드 채우기.

## 5. 원칙 점검

| 원칙 | 점검 |
|---|---|
| SSOT | 이동 모드 조건은 Drag 시작 조건(`PlanningActor(Path)`) 하나를 같이 쓴다. 범위 사실은 기존 `CombatMovementRangeFacts` 한 형식. 범위 값은 Core 응답만 그린다(View 계산 없음). 수명 규칙 원본은 이 문서 하나이고 옛 두 문서는 여기를 가리킨다. |
| SOLID | 범위 요청·표시 소유자는 그대로 경로 입력 하나(SRP 유지, 새 협력자 없음). 표시 계약·Interaction 위임 모양은 그대로(OCP·ISP). |
| FW | 서비스·Factory·Buffer 계약 변경 없음. 기존 Poll·Refresh 수명 위에서 동작. |
| 독립성 | 바뀌는 곳은 경로 입력과 `CancelPath` 한 줄. 공격 모드·배치·아이템·Core는 그대로다. 호버 범위 설계와는 슬롯 규칙(R-a/R-b)으로 맞물린다(§6). |

## 6. 남는 영향

- 호버 공격 범위 설계(초안) F5·UR4: 슬롯 요청자에 "이동 모드 범위"가 하나 늘었다. R-a(답 안 된 요청 기다림)대로면 이동 모드 범위 응답 뒤 호버가 요청하므로 그대로 맞물린다. 그 설계의 결정 때 반영할 사실이다(그 문서는 고치지 않음).
- `CombatPathInput`이 637줄에서 더 늘었다. 범위 맞춤을 따로 떼는 것은 폴더 정리(M 계획) 뒤 후보다.

## 7. 실행 기록

### 7.1 MR-1 (2026-10-19, 코드 완료, Unity 확인 대기)

- 코드: `CombatPathInput`(§3 내용 전부), `CombatInteraction.CancelPath` → `LeaveMoveMode`. 표시 콜백 실패 세 곳(범위 표시·편집 시작·고스트 갱신)은 `LeaveMoveMode`로 정리한다.
- 시험:
  - 새 `CombatMoveModeRangeTests` 8개.
  - `CombatMovementRangeFeedbackTests`: 옛 규칙("취소하면 범위를 지운다")을 단언하던 2개를 바꿔 씀 — `CancelAndReDrag_SameFacts_ReusesPendingRangeWithoutRepublish`·`LeaveMoveModeAndReDrag_IgnoresOldResponseAndPreservesNewRangeSlot`(옛 `CancelAndReQ_…`), `CancelFocusCaptureDetachLeaveOrReturn_…_KeepsRangeOnlyInMoveMode`(옛 `CancelFocusCaptureDetachOrReturn_…`, `LeaveMoveMode` 사례 추가). 표시 콜백·정리 실패 사례 2개의 기대를 고침.
  - `CombatMovementFeedbackTests`: 놓기 실패 두 사례의 범위 기대(남음), 시험 도우미 `Rig.Start`가 범위를 다시 쓰는 경우 응답을 기다리지 않게 고침(취소 뒤 다시 Drag하는 시험 4개가 이것으로 통과).
  - `CombatMovementPlanningInputTests`: 고스트 정리 실패는 범위가 남고, 고스트 갱신 실패는 범위까지 지움.
  - Unity 전용(관리 실행 불가, 컴파일만 확인): `CombatMovementViewTests` 3개(`TargetingCancellation_…`·`Return_SnapsWithoutTraversal`·`CancelledDraft_RestoresReturnPreview`)와 `CombatDisplayReuseTests`의 이동·복귀 사례에 이동 모드 범위 왕복을 하나 더 넣음.
- 검증:
  - MSBuild 오류 0·새 경고 0.
  - 관리 실행: 경로 입력·상호작용·진입 통합 등 관련 시험 1,049개 기준선(PASS 653·FAIL 16·NATIVE 379·UNSUPPORTED 1) 대비 기존 시험 상태 변화 0. 바꿔 쓴 6개 대신 새 시험 16개 PASS(최종 PASS 663). FAIL 16은 기준선과 같은 기존 실패(`CombatEntryIntegrationTests` 15·`ShopStageServiceTests` 1).
  - 같은 작업 트리에 다른 세션의 철로·카메라 변경이 진행 중이라, 마지막 검증 빌드는 그 파일 20개를 HEAD 판으로 바꿔 넣고 돌렸다(그쪽 진행 중 코드는 한때 컴파일되지 않았다).
- Unity 확인 목록(사용자): §4 끝줄. 특히 이동한 뒤 복귀 버튼이 한두 프레임 늦게 켜지는지, GridOutline 모드에서 이동 모드 동안 범위 채우기가 보이는지.
- 커밋: 사용자 `c343203d`·`4b6e7320`(뒤이은 병합 `927962fb`는 이 파일들을 바꾸지 않음).

## 8. 원칙 평가 (2026-10-19, 코드 변경 없음)

사용자 요청: "SSOT와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성으로 지금 작업 평가해." 기준은 [설계 세션 인계](../handoffs/2026-09-30-combat-view-design-session-handoff.md) §4(4.1·4.2·4.3·4.6). 대상은 커밋 `4b6e7320`의 MR-1 코드(`CombatPathInput.cs`·`CombatInteraction.cs`)와 시험·문서. 점수는 1~10(10 = 지적할 것 없음).

### 8.1 결론

| 기준 | 점수 | 깎은 이유 |
|---|---|---|
| SSOT | **8** | 값(Core 응답)·이동 모드 조건(`PlanningActor` 하나)·범위 사실 형식·문서 원본은 한 곳씩이다. −1: 슬롯 순서 규칙의 코드 원본이 없다(M2). −1: 범위 정체성이 기다림/보임에 따라 두 객체에 나뉜다(L1). |
| SOLID | **6** | 표시 계약·Interaction 위임·의존성은 그대로다. −3: `CombatPathInput` 637 → 772줄, 상태 필드 29개. 지정과 수명이 다른 범위를 지정 안에 더 깊이 넣었다(M1, 재평가 N3 악화). −1: `CancelInputs`의 뜻이 이름은 그대로 바뀌었고, `CancelPath`가 `CancelInputs`보다 넓게 지운다(L2). |
| FW 지침·계약 | **9** | Framework·Factory·서비스·키·Buffer 계약 변경 0, 입력은 기존 경로, 규칙은 Core 응답(P6). −1: 매 Tick 사실을 최대 세 번 읽는다(R1, 할당은 없음). |
| 변경 독립성 | **6** | 표시 모양·이동 모드 조건 변경은 한 곳에 모인다. −2: 범위 규칙을 바꾸는데 지정·복귀 코드 열 곳과 복귀 시험 4개가 따라왔다(M1). −2: 슬롯을 쓰는 새 기능(호버 범위)이 경로 입력의 순서 코드를 읽고 고쳐야 한다(M2). |
| **종합** | **7.3** | 네 기준 평균 |

발견: 높음 0, 중간 2(M1·M2), 낮음 4, 참고 3. 점수를 가장 많이 올리는 것은 M1(SOLID 6 → 8, 독립성 6 → 7 예상)이고, M2는 호버 설계 Q-UR1과 함께 정해야 한다.

### 8.2 SSOT

| 사실 | 원본 | 다른 곳 | 판정 |
|---|---|---|---|
| 이동 가능 목적지 | Core `CombatMovementCalculator.Range` 응답 | 없음(View 계산 0) | 지킴 |
| 이동 모드 조건 | `CombatPathInput.PlanningActor(Path)`(Router 판정식·`CanEditPath` 위) | 범위 요청·Drag 시작이 같은 함수 | 지킴 |
| 범위가 같은지(정체성) | `CombatMovementRangeFacts`(전투·행위자·시작 칸·결과 번호) | 기다리는 사실은 `CombatMovementRangeRequest._facts`, 보이는 사실은 `CombatPathInput._shownRangeFacts` | 형식은 하나, **보관이 둘(L1)**. 둘은 동시에 차지 않는다 |
| 범위 수명 규칙 | 이 문서 MR2~MR4 | 옛 설계 둘은 "대신함" 표시, 가이드·CLAUDE.md는 요약 | 지킴 |
| 슬롯 순서(범위 → 복귀 견적) | 문서 MR4 | 코드: `RefreshReturnEstimate`의 대기 검사, `RefreshMoveModeRange`의 복귀 대기 검사, `RequestReturn`의 내려놓기, `OnPreview`의 잃은 요청 정리 | **규칙 원본은 문서뿐, 코드에 네 조각(M2)** |
| 범위 표시 여부 | 표시 쪽 강조 상태(`FieldHighlightState`) | 입력 쪽 `_rangeShown`, Presenter `_hasRange` | 기존 그대로(자기가 그린 것만 지우는 표지) |

### 8.3 SOLID

| 원칙 | 판정 | 근거 |
|---|---|---|
| S | **주의(M1)** | `CombatPathInput`의 하위 기능: 지정 수명, 초안 표시 같은 값 생략, 이동 범위, 복귀 견적·복귀 요청, 공격 Drag 넘겨받기. 이번에 범위가 지정과 다른 수명(이동 모드 내내)을 얻었는데 같은 클래스에서 지정 메서드 사이에 섞였다. 재평가 N3(2026-10-15, 584줄)가 "이동 범위 규칙을 바꾸면 계획·초안·복귀 코드와 같은 파일을 읽어야 한다"고 예측했고 그대로 됐다. |
| O | 부분 | 새 동작을 위해 기존 메서드 열 곳(`OnPreview`·`TryBeginPlanning`·`ConsumeMovementRange`·`ClearMovementRange`·`CancelInputs`·`Refresh`·`RefreshReturnEstimate`·`RequestReturn`·표시 실패 catch 둘)을 고쳤다. 입력 소유자 안의 변경이라 예상 범위지만, 범위가 확장 지점이 아니라는 신호다. |
| L | 주의(L2) | `CancelInputs`가 "범위까지 지움"에서 "이동 모드면 남김"으로 바뀌었다. 호출자 다섯 곳(포커스 상실·포기 확인창·정상 종료·Layer 해제/변경·결과 전)은 범위를 사실에서 다시 판단하므로 동작은 맞다(이동 모드가 아니면 지움). 이름과 XML doc만 보면 Interaction의 `CancelPath`(범위까지 지움)가 `CancelInputs`(남김)보다 넓다는 것이 드러나지 않는다. |
| I | 지킴 | `ICombatMovementFeedback`에 멤버를 더하지 않았다. |
| D | 지킴 | 새 의존성 없음. 기존 `CombatReturnRequest.IsWanted`만 더 읽는다. |

### 8.4 FW 지침·계약

| 항목 | 판정 |
|---|---|
| FW-001~003 서비스·등록·Build | 변경 없음(새 Service·Key·`AddModule`·Factory 0, 민형님 공유 불필요) |
| FW-004 내부 상태 우회 | 없음. View는 자기 입력 Buffer의 미리보기 슬롯만 쓴다 |
| FW-005~007 입력 | 바인딩·Configure·Submit 경로 변경 없음 |
| FW-010~012 렌더 | 강조 층·팔레트·대여 자원 변경 없음 |
| DR-009 Layer 전환과 Canceled | `SelectCanceled` → `CancelInputs` → Layer가 전투가 아니면 이동 모드 아님 → 지움 |
| P6 규칙 값은 Core | 지킴(범위 응답으로 이동을 승인하지 않는 D2도 그대로) |
| 할당·성능 | 범위 정리에 할당 없는 조기 반환을 넣었다. 매 Tick `Refresh`가 사실(`CombatInputFacts`, struct)을 최대 세 번 읽는다(R1). 입력 불가 동안 `OnPreview` → `CancelInputs`가 매 Tick 정리 대리자를 만드는 것은 이번 작업 전부터다(R2) |
| 코딩 규칙 | Allman·한국어 주석·`.meta` GUID 32자리·기존 시험 어셈블리 |

### 8.5 변경 독립성 (대표 시나리오)

| 시나리오 | 고칠 파일 | 읽어야 할 코드 | 검증 | 판정 |
|---|---|---|---|---|
| 범위 색·채우기 바꾸기 | 팔레트·`FieldOutlineRenderer` | 표시 쪽만 | 표시 시험 | 좋음(입력 파일 0) |
| 이동 모드 조건 바꾸기(예: 첫 이동 뒤엔 숨김) | `TryGetMoveModeRangeFacts` 한 함수 | `PlanningActor` | 경로 입력 순수 시험 | 좋음 |
| 범위 수명 규칙 바꾸기(이번 작업) | `CombatPathInput` 열 곳 + `CombatInteraction` | 지정·초안·복귀·공격 Drag 넘겨받기 | 순수 시험 3파일 + **복귀를 단언하는 Unity 시험 4개** | **나쁨(M1)** |
| 복귀 견적만 바꾸기 | `CombatPathInput` | 범위 순서 조각(MR4)까지 | 범위 시험까지 | 부분(M1·M2) |
| 슬롯을 쓰는 새 배경 요청(호버 범위) | 새 소유자 + `CombatPathInput`의 순서 조각 | 범위·복귀 순서 전부 | 범위·복귀·호버 통합 | **나쁨(M2)** |
| 다른 작업자 영역 | 없음(Core·Factory·현강님·호진님 파일 0) | — | — | 좋음 |

### 8.6 발견과 권고

| ID | 등급 | 내용 | 권고 |
|---|---|---|---|
| M1 | 중간 | 이동 범위 수명이 지정과 독립됐는데 `CombatPathInput` 안에 섞여 클래스가 772줄·필드 29개가 됐다. 범위 규칙 변경이 지정·복귀 코드와 시험을 끌고 온다. | 재평가 N3(a) 실행: 범위 소유자 하나(가칭 `CombatMovementRangeLease`)에 `CombatMovementRangeRequest`·세대 번호·보이는 사실·표시/정리·`Matches`·`Keep`·`Ensure`·`Drop`을 모은다. 경로 입력은 "지금 원하는 사실"만 넘긴다. 동작 동일 리팩터, 폴더 정리(M 계획) 뒤가 자연스럽다. |
| M2 | 중간 | 한 슬롯의 사용 규칙(누가 언제 덮어써도 되나)에 코드 소유자가 없다. 범위·복귀 순서는 경로 입력 네 조각, 공격·배치·이동 견적은 각자 덮어쓰고, 호버(초안)는 R-a·R-b를 또 다른 클래스에 둘 예정이다. | 호버 설계 Q-UR1 결정 때 함께 정한다. A안이면 배경 요청 규칙("답 안 된 남의 요청이면 기다림", "같은 사실로 답 받았으면 다시 요청 안 함")을 순수 함수 하나로 두고 범위·복귀 견적·호버가 같이 부른다. B안(조회 전용 슬롯)이면 범위도 그쪽으로 옮길지 본다. |
| L1 | 낮음 | 범위 정체성이 기다릴 땐 요청 객체, 보일 땐 경로 입력에 있다. `RangeMatches`가 두 경우를 가른다. | M1과 함께 풀린다. |
| L2 | 낮음 | `CancelInputs` 이름은 그대로인데 범위를 남긴다. Interaction에서는 `CancelPath`가 `CancelInputs`보다 넓게 지운다. | M1 때 이름을 정리하거나(`CancelPath` → `LeaveMoveMode`), Interaction XML doc에 범위 유무를 적는다. |
| L3 | 낮음 | `FieldHighlightState.IsActing` 설명("사용자가 행동 중")이 이제 이동 모드 내내 참이다(격자 아웃라인 설계 U4·F5의 "행동 중"과 뜻이 어긋남). 화면은 의도대로다(데칼 상시 표기). | 주석·격자 아웃라인 설계에 "이동 모드 범위도 행동 중 층"을 한 줄 적는다. |
| L4 | 낮음 | 새 의도가 거절되면(공격 진입 불가·턴 종료 거절) 범위가 지워졌다가 다음 Tick에 다시 요청된다(깜빡임 + Core 왕복 한 번). | 수용(MR3). 버튼은 가능할 때만 눌리고 키 입력만 해당. |
| R1 | 참고 | 매 Tick `Refresh`가 사실을 최대 세 번 읽는다. | M1 때 한 번 읽은 사실을 넘기는 모양으로 줄일 수 있다. |
| R2 | 참고 | 입력 불가 동안 매 Tick 정리 대리자 할당(이번 작업 전부터). | 이번 범위 밖. |
| R3 | 참고 | Unity 전용 시험 4개·재생 확인은 아직 결과가 없다(커밋에 포함됨). | §7.1 확인 목록. |
