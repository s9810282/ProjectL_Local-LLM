# 전투 경로 입력 분리 설계 — 이동 범위·복귀 견적·초안 표시 (2026-10-19)

- **목표:** `CombatPathInput`에 섞여 있는 하위 기능 셋을 정리한다. **화면 동작은 바꾸지 않는다.**
  - 이동 범위의 요청·응답·표시 수명 → 소유자 `CombatMovementRangeTracker`(RT1~RT6)
  - 복귀 견적·복귀 요청 → 소유자 `CombatReturnEstimateTracker`(RT7)
  - 초안 표시의 "같은 값 생략" 거울 → 표시를 멱등으로 만들고 입력 쪽 거울을 없앰(RT8)
- **상태:** 결정 완료(2026-10-19, Q-R1~Q-R5 모두 A), R-1~R-3 코드 완료, 원칙 평가 §8(9.0/10). Task·실행 기록: [path-input-split-plan](../plans/2026-10-19-combat-path-input-split-plan.md) §9.
- 처음 판(범위만)에 사용자 요청(2026-10-19: "저 3가지 넣어서 계획 다시 짜")으로 RT7·RT8을 더했다. 세 가지 중 슬롯 규칙 함수(M2)는 호버 U-2가 이미 `CombatPreviewSlotRule`로 넣었다(작업 트리, 커밋 전). 이 설계는 그 함수를 쓰는 쪽만 다룬다.
- **근거:** [이동 모드 범위 설계](2026-10-19-combat-move-mode-range-design.md) §8 원칙 평가의 M1(중간)·L1·L2, [구조 재평가](2026-10-15-combat-view-structure-reevaluation.md) N3 방향 (a). 범위 수명 규칙(언제 보이나)의 원본은 그대로 이동 모드 범위 설계 MR2~MR4다. 이 문서는 그 규칙을 **어느 클래스가 어떻게 들고 있나**만 정한다.
- 경로 약칭 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`. 줄 번호는 HEAD `927962fb` 기준. 호버 U-2(작업 트리)가 `RangeMatches`·`RefreshMoveModeRange`·`RefreshReturnEstimate`·`HandleReturnPreview`를 슬롯 규칙 함수로 바꿨다(F10). 분리는 그 판을 보존 대상으로 한다.

## 0. 요청과 범위

- 사용자 요청(2026-10-19): "M1 범위 소유자 분리 설계부터 해줘."
- 범위: `C/CombatPathInput.cs`의 이동 범위 부분, 범위 표시 계약, 새 소유자와 그 시험.
- 범위 밖: 미리보기 슬롯 조정 규칙의 일반화(M2, 호버 설계 U-2 뒤), 복귀 견적 분리(재평가 N3 ④), 초안 표시 거울(N3 ②), 매 Tick 사실 읽기 횟수(R1). §7에 남긴다.

## 1. 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | 범위 상태는 `CombatPathInput`의 필드 넷이다: 요청 값 객체 `_rangeRequest`, 응답 세대 `_rangeGeneration`, 보임 `_rangeShown`, 보이는 사실 `_shownRangeFacts`. 클래스 전체 상태 필드는 29개다. | `C/CombatPathInput.cs:48-52` |
| F2 | 범위를 만지는 곳은 14군데(참조 27개)다. 기계 쪽(어떻게): `RangeMatches`·`RequestMovementRange`·`DropRangeRequest`·`ClearMovementRange`·`ConsumeMovementRange`의 소비·표시. 정책 쪽(언제·무엇을): `TryGetWantedRangeFacts`·`TryGetMoveModeRangeFacts`·`RangeFactsOf`, 그리고 수명 훅 `OnPreview`·`TryBeginPlanning`·`ValidatePlanning`·`PointerMoved`(경로 견적 유예)·`CancelInputs`·`KeepRangeForMoveMode`·`RefreshMoveModeRange`·`RefreshReturnEstimate`·`RequestReturn`·`Detach`. | `:148·162-165·219-233·262-277·290-291·300-312·341-364·382·503-505·533-540·674-686·725·742` |
| F3 | `CombatMovementRangeRequest`는 요청 정체성·응답 1회 소비만 맡는 값 객체이고, 순수 시험 30개(`CombatMovementRangeRequestTests`)가 있다. 소비하면 자기 사실을 지운다. 그래서 "보이는 범위의 사실"은 경로 입력이 따로 들고 있다(L1). | `C/CombatMovementRangeRequest.cs`, `C/CombatPathInput.cs:308-309` |
| F4 | 범위 표시는 `ICombatMovementFeedback`의 `ShowMovementRange`·`ClearMovementRange` 둘만 쓴다. 이 계약을 명시 구현(`ICombatMovementFeedback.X`)하는 곳은 없다. 구현은 `CombatSceneDisplay`(`ICombatBattleDisplay` 경유)와 시험 가짜 셋(`CombatMovementFeedbackTests.Probe`·`HojinCombatAttackDragTests`·`CombatViewSystemTests.FakeDisplay`)이다. | `C/ICombatMovementFeedback.cs`, `C/ICombatBattleDisplay.cs:20` |
| F5 | 재진입 검사는 경로 입력의 지정 세대(`_planningVersion`)로 한다. 범위를 지울 때 표시 콜백이 경로 입력을 다시 부를 수 있어(시험 `RangeCallbackCancelOrNewQ_…`), 지운 뒤 세대를 다시 보고 나서 요청한다. | `C/CombatPathInput.cs:220-224·681-686` |
| F6 | 표시 실패 규칙: 보임 상태를 먼저 켜고 표시를 부른다. 표시가 던지면 경로 입력이 `LeaveMoveMode`로 정리하고, 그때 보임 상태가 켜져 있어 표시 지우기까지 간다. | `C/CombatPathInput.cs:308-327` |
| F7 | 시험이 리플렉션으로 읽는 경로 입력 필드는 `_shownPath` 하나다(범위 필드는 읽지 않음). `CombatPathInput`을 직접 만드는 곳은 Factory 한 곳과 시험 조립 셋이다. 생성자 서명은 바꾸지 않아도 된다. | `CombatMovementFeedbackTests.cs:356`, `CombatSceneInputFactory.cs:56` |
| F8 | 같은 파일을 고칠 예정인 작업: [호버 공격 범위 계획](../plans/2026-10-19-combat-unit-hover-attack-range-plan.md) U-2 T12(`CombatPathInput.HandleReturnPreview`·`RefreshReturnEstimate`·`CombatReturnRequest`)·T14(`CombatInteraction`). 결정 완료, 코드 착수 전. U-1(Core 조회)은 이 작업과 파일이 겹치지 않는다. 그 계획 §0.3 **P1 결정(사용자, A):** 배경 요청 셋(이동 모드 범위·복귀 견적·호버)의 슬롯 규칙을 순수 함수 하나(가칭 `CombatPreviewSlotRule`)로 모은다(M2 해소). 같은 곳에 "M1은 하지 않고 **폴더 정리 뒤 따로 한다**"고 적혀 있다. | 그 계획 §0.3·§1·§4 |
| F9 | 폴더 정리 계획(M, 착수 전)의 대응표에서 경로 입력 묶음은 `Input/MovePath/`, 입력 계약은 `Input/Contracts/`다. 비슷한 선례 `CombatHoverTracker`(호버 칸 상태 + 표시 호출 소유)는 `Input/Interaction/`에 있다. | [폴더 정리 계획](../plans/2026-10-19-combat-folder-restructure-plan.md) §3 |
| F10 | 호버 U-2(작업 트리, 커밋 전, Unity 확인 대기)가 슬롯 규칙 `CombatPreviewSlotRule`(R-a `IsIdle`·R-b `IsSettled`)을 넣고 경로 입력을 바꿨다: `RangeMatches`가 `IsSettled`, 범위·복귀 견적의 대기가 `IsPreviewSlotIdle`(R-a), 복귀 견적이 `CombatReturnRequest.IsAnswered`(R-b, `Begin`·`Reset`이 지움). `CombatPathInput`은 781줄이다. | `C/CombatPreviewSlotRule.cs`, `git diff` |
| F11 | 복귀 견적 부분(약 72줄): `RefreshReturnEstimate`(언제: 지정 중 아님·허용·이동 상호작용 비활성·PlayerInput·행위자의 이번 차례 이동·슬롯 한가)·`RequestReturn`(복귀 버튼)·`HandleReturnPreview`(답 받음 표시·원함이면 제출)·`ClearReturnRequest`(자기 슬롯 비움·초기화·표시 지움). 표시는 `ICombatMovementFeedback.SetReturnState` 하나다. 값 객체 `CombatReturnRequest`(97줄)와 시험이 있다. | `C/CombatPathInput.cs:708-779`, `C/CombatReturnRequest.cs` |
| F12 | 초안 표시 거울: 필드 다섯(`_shownPath`·`_shownPreview`·`_shownPointerValid`·`_shownCandidate`·`_draftDisplayVersion`)과 `InvalidateDraftDisplay`. 같은 값이면 `ShowDraft`를 부르지 않는다. `_draftShown`은 "그린 것이 있으면 지운다"는 소유 표지라 거울과 다르다. | `C/CombatPathInput.cs:36-40·576-649` |
| F13 | 표시 쪽은 이미 대부분 멱등이다: `FieldHighlightState.SetHover`는 같은 칸·상태면 바뀐 칸 0, `SetLayerCells`는 같은 칸·같은 순서면 바뀐 칸 0이고 렌더러는 바뀐 칸이 없으면 다시 그리지 않는다(할당 없음). **멱등이 아닌 곳은 HUD 비용 표시 하나다**: `CombatUnitHudFeedback.ShowCostPreview`가 매번 지운 뒤(0) 다시 쓴다. 입력 쪽 거울이 실제로 막는 것은 이 깜빡임이다. | `Display/FieldHighlightState.cs:162-223`, `Display/CombatUnitHudFeedback.cs:35-50` |
| F14 | 입력 쪽 거울을 단언하는 순수 시험이 10개다(표시 호출 수·리플렉션 `_shownPath`): `CombatMovementFeedbackTests`의 `SameAcceptedPreview_SkipsDisplay_…`·`BlockedCandidate_OnlyChangesMarker_…`·`SamePathNewQuote_Renders_…`·`UnacceptedOldResponse_DoesNotReplaceDisplayCache`·`InputDisabledWithCachedPreview_ClearsDraftBeforeDeduplication`·`FailedDisplay_IsRetriedForSameValues`·`CancelOrDetachDuringDisplay_DoesNotRestoreOldCache`(두 사례)·`NewGestureDuringDisplay_KeepsInnerDisplayAsLastApplied`, `CombatMovementPlanningInputTests.QWaitingDoesNotStartAppearance_SameCellPointerMovesWithoutQuoteOrDraftRedisplay`. | 각 시험 파일 |

## 2. 설계

### RT1. 나누는 기준 — "어떻게"는 소유자, "언제·무엇을"은 경로 입력

| 책임 | 소유 | 이유 |
|---|---|---|
| 범위 하나의 요청 게시·정체성·응답 세대·1회 소비, 슬롯 소유 판단, 보이는 사실, 표시 켜기·지우기, 정리 실패 모으기 | **`CombatMovementRangeTracker`(새)** | 지정 수명과 다른 자기 수명(요청 → 기다림 → 보임 → 지움)을 가진다. 바뀌는 이유가 "범위를 어떻게 받아 보이나"로 하나다. |
| 지금 원하는 사실(지정 중이면 지정, 아니면 이동 모드 행위자) | `CombatPathInput` | 입력 허용(`AllowsInput`)·행위자(`PlanningActor`)·지정 상태를 아는 곳이 경로 입력뿐이다. 이동 모드 조건 원본을 Drag 시작과 같이 쓴다(SSOT 유지). |
| 맞추는 시점(MR3 표: `Refresh`·Drag 시작·지정 취소·새 의도 전·결과 전·Detach)과 재진입 검사 | `CombatPathInput` | 수명 훅이 모두 경로 입력의 메서드이고, 재진입 기준이 지정 세대다(F5). |
| 슬롯 순서(범위 → 복귀 견적, 복귀 원함이면 범위 요청 미룸) | `CombatPathInput`(그대로) | 두 배경 요청 사이의 순서다. 일반화는 M2에서 따로 다룬다. |

### RT2. 소유자 계약 `CombatMovementRangeTracker`

순수 C#, `internal sealed`. 경로 입력이 생성자에서 하나 만든다(지금 `CombatMovementInteraction`처럼). Factory·서비스 등록 없음.

```csharp
internal sealed class CombatMovementRangeTracker
{
    internal CombatMovementRangeTracker(ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write,
        ICombatMovementRangeFeedback display);

    internal bool IsPending { get; }   // 답을 기다리는 요청이 있다(경로 견적 유예·복귀 견적 순서가 읽음)
    internal bool IsEmpty { get; }     // 기다리는 요청도 보이는 범위도 없다

    // 보이거나 기다리는 범위가 이 사실의 것인가. 기다리는 요청은 아직 슬롯을 가져야 한다
    // (지금 RangeMatches, 배경 요청 규칙 R-b = CombatPreviewSlotRule.IsSettled).
    internal bool Matches(in CombatMovementRangeFacts facts);

    // 이 사실로 범위를 요청한다. 앞 요청·보임이 있으면 호출자가 먼저 Clear한다(지금 RequestMovementRange).
    internal void Request(in CombatMovementRangeFacts facts);

    // 이 사실의 응답이면 한 번 소비하고 자기 슬롯을 비운 뒤 보인다. 소비했으면 true.
    // 보임 상태를 먼저 켜고 표시를 부르며, 표시 예외는 그대로 던진다(호출자가 정리, F6).
    internal bool TryShowAnswer(CombatPreviewResult preview, in CombatMovementRangeFacts facts);

    // 기다리는 요청만 내려놓는다. 슬롯·보이는 범위는 건드리지 않는다(지금 DropRangeRequest).
    internal void Drop();

    // 기다리는 요청이 슬롯을 잃었으면 내려놓는다(지금 OnPreview 첫 줄).
    internal void DropIfSlotLost();

    // 자기 슬롯이면 비우고, 보였으면 표시를 지운다. 둘 다 시도하고 실패를 모아 던진다.
    // 할 일이 없으면 대리자를 만들지 않고 돌아간다(지금 ClearMovementRange).
    internal void Clear();
}
```

- 소유자가 갖는 상태: `CombatMovementRangeRequest`(값 객체, 그대로 재사용), 응답 세대, 보임, 보이는 사실. 경로 입력의 범위 필드 넷(F1)이 모두 옮겨 간다.
- `CombatMovementRangeRequest`는 바꾸지 않는다(시험 30개 그대로). 머리 주석의 "CombatPathInput이 한 개를 소유"만 "Tracker가 한 개를 소유"로 고친다.
- 소유자는 지정·이동 모드·복귀를 모른다. 사실은 호출자가 값으로 넘긴다.

### RT3. 경로 입력에 남는 호출 (동작 동일)

| 경로 입력 위치 | 지금 | 바뀐 뒤 |
|---|---|---|
| `OnPreview`(:162-169) | 잃은 요청 내려놓기 → 기다리면 소비 | `_range.DropIfSlotLost()` → `_range.IsPending`면 `ConsumeMovementRange` |
| `ConsumeMovementRange`(:297-339) | 소비·슬롯 비움·보임·표시·실패 정리·지정 재개 | 원하는 사실 구하기 → `try { if (!_range.TryShowAnswer(...)) return; } catch { LeaveMoveMode… }` → 지정 재개(그대로) |
| `TryBeginPlanning`(:217-233) | `RangeMatches` → 아니면 `ClearMovementRange` → 세대 검사 → 지정 상태 → `RequestMovementRange` | `_range.Matches` → 아니면 `_range.Clear()` → 세대 검사 → 지정 상태 → `_range.Request` |
| `ValidatePlanning`(:290-291) | 기다리면 사실·슬롯 일치 | `!_range.IsPending \|\| _range.Matches(CurrentRangeFacts())` |
| `PointerMoved`(:382) | 견적 유예 = `_rangeRequest.IsPending` | `_range.IsPending` |
| `CancelInputs`(:503-505)·`KeepRangeForMoveMode`(:533-540) | 이동 모드 사실과 맞으면 남김, 아니면 지움 | `_range.IsEmpty`·`_range.Matches`·`_range.Clear`로 같은 판단 |
| `RefreshMoveModeRange`(:674-686) | 사실 없음 → 지움 / 맞음 → 그대로 / 다름 → 지움 → 세대·슬롯 한가(R-a, 호버 U-2) 검사 → 요청 | 같은 순서를 소유자 기본 동작으로. 슬롯 한가 검사(`IsPreviewSlotIdle`)는 경로 입력에 남는다(R-a는 "언제") |
| `RefreshReturnEstimate`(:725) | 슬롯 한가(R-a)면 요청(호버 U-2가 범위 대기 검사를 R-a로 바꿈) | 변경 없음(범위 소유자를 읽지 않는다). RT7이 이 메서드의 기계 쪽을 옮긴다 |
| `RequestReturn`(:742) | 범위 요청 내려놓기 | `_range.Drop()` |
| `Detach`(:148) | `ClearMovementRange` | `_range.Clear` |

"지우고 → 세대 검사 → 요청" 모양이 두 곳(`TryBeginPlanning`·`RefreshMoveModeRange`)에 남는다. 재진입 기준(지정 세대)이 경로 입력의 것이라 소유자 안으로 넣지 않는다. 대리자를 넘기는 "바꿔 요청" API는 매 Tick 할당이 생겨 쓰지 않는다.

### RT4. 표시 계약을 좁힌다

- 새 계약 `ICombatMovementRangeFeedback`: `ShowMovementRange(MovementRangeResult)`·`ClearMovementRange()` 둘. 두 멤버의 XML doc(범위만 표시, 이동 승인은 별도 견적)을 이리로 옮긴다.
- `ICombatMovementFeedback : ICombatMovementRangeFeedback`로 두 멤버를 상속으로 바꾼다. 명시 구현이 없어(F4) `CombatSceneDisplay`와 시험 가짜 셋은 고치지 않아도 컴파일된다.
- 소유자는 좁은 계약만 받는다(ISP). 경로 입력은 지금처럼 `ICombatMovementFeedback`을 받고 그 참조를 소유자에게 넘긴다.

### RT5. 동작 동일 보존 표

| 지금 동작 | 보존 방법 |
|---|---|
| 같은 사실이면 다시 요청하지 않음(보임은 사실로, 기다림은 사실 + 슬롯으로) | `Matches`가 지금 `RangeMatches` 식 그대로 |
| 소비는 한 번, 소비 직전 자기 슬롯만 비움 | `TryShowAnswer`가 지금 순서(TryConsume → ClearPreview → 보임 켜기 → 표시) 그대로 |
| 표시가 던지면 지정 취소 + 범위 지움 | 보임을 먼저 켜는 순서 유지, catch는 경로 입력에 그대로(F6) |
| 지울 때 자기 슬롯만 비우고 보였을 때만 표시 지움, 둘 다 시도 | `Clear`가 지금 `ClearMovementRange`의 정리 둘을 그대로 |
| 할 일 없으면 할당 없이 돌아감 | `Clear`의 조기 반환 유지 |
| 지운 뒤 재진입이면 요청하지 않음 | 경로 입력의 세대 검사 위치 그대로(RT3) |
| 범위 → 복귀 견적 순서, 복귀 원함이면 범위 요청 미룸, 복귀 버튼은 기다리던 범위 요청 내려놓음 | 경로 입력의 세 줄이 `IsPending`·`Drop`으로 같은 판단 |
| 정리 예외 문구 | 소유자 문구 `[CombatMovementRangeTracker] 이동 범위 정리 중 실패했습니다.`가 경로 입력 정리 예외 안에 들어간다. 시험은 형식(`AggregateException`)만 본다(문구만 다름). |

### RT6. 이름·위치

- 이름 `CombatMovementRangeTracker`: "상태 + 표시 호출 소유" 선례 `CombatHoverTracker`와 같은 꼴.
- 위치: 폴더 정리(M)가 먼저면 `Input/MovePath/`(계약은 `Input/Contracts/`), 아니면 지금 평면 폴더에 두고 M 계획 §3 두 줄에 더한다.
- 새 파일 둘(소유자·계약)은 `.meta`와 함께. MonoBehaviour·SO가 아니라 `MovedFrom`은 필요 없다.

### RT7. 복귀 견적 소유자 `CombatReturnEstimateTracker`

나누는 기준은 RT1과 같다.

| 책임 | 소유 |
|---|---|
| 견적 요청 게시(배경)·복귀 요청 게시(원함, 전경), 답 받음 기억과 R-b 판단, 자기 응답 처리(답 받음 표시 → 원함이면 승인·정체성 확인 뒤 제출 → 정리), 정리(자기 슬롯 비움·초기화·표시 지움), "복귀할 이동 없음" 표시 | **`CombatReturnEstimateTracker`(새)**. 값 객체 `CombatReturnRequest`를 품는다(시험 그대로) |
| 언제 견적을 낼지(지정 중 아님·허용·이동 상호작용 비활성·PlayerInput·행위자 이번 차례 이동 여부·슬롯 한가 R-a), 복귀 버튼 입구 검사(허용·원함 아님·PlayerInput·행위자), 복귀 버튼의 순서(지정 취소 → 범위 요청 내려놓기 → 복귀 요청), 응답 처리 직전 입력 허용 다시 읽기 | `CombatPathInput` |

```csharp
internal sealed class CombatReturnEstimateTracker
{
    internal CombatReturnEstimateTracker(ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write,
        ICombatReturnFeedback display, long battleInstanceId);

    internal bool IsWanted { get; }
    internal bool IsSettled { get; }              // R-b: 이 요청이 답을 기다리거나 이미 받음(지금 RefreshReturnEstimate 앞부분)
    internal void Request(EntityId actor);        // 배경 견적(지금 RefreshReturnEstimate 끝 줄)
    internal void RequestWanted(EntityId actor);  // 복귀 버튼(지금 RequestReturn 끝 두 줄)
    internal void ShowUnavailable();              // 복귀할 이동이 없음 = 표시 null(지금 RefreshReturnEstimate 중간)
    internal void HandleAnswer(CombatPreviewResult preview);  // 지금 HandleReturnPreview(허용 판단은 호출자)
    internal void Clear();                        // 지금 ClearReturnRequest
}
```

- 표시 계약: `ICombatReturnFeedback`(`SetReturnState` 하나). `ICombatMovementFeedback`이 상속한다(RT4와 같은 방식, 가짜 수정 0).
- 보존할 순서: 응답 처리에서 지금은 `HasRequest`·`IsAnswer`·`AllowsInput()` 순으로 본다. 경로 입력이 `AllowsInput()`을 먼저 보고 소유자를 부르게 바뀐다. `AllowsInput`은 사실을 읽기만 하므로 결과는 같다. 이동 제출 뒤 사실을 다시 읽는 지금 규칙("Return 처리는 자기 입구에서 사실을 다시 읽는다")은 그대로다.
- 정리가 매 Tick 표시를 null로 부르는 지금 동작(입력 불가 동안)도 그대로 둔다(동작 동일 범위).

### RT8. 초안 표시 — 표시를 멱등으로, 입력 쪽 거울 삭제

- 근거: 표시 쪽은 HUD 비용 표시만 빼면 이미 멱등이다(F13). 같은 값을 다시 보내도 화면이 바뀌지 않으면, 입력이 "무엇을 그렸나"를 따로 기억할 이유가 없다. 화면 상태의 원본은 표시 쪽(`FieldHighlightState`·HUD 피드백) 하나가 된다(재평가 N3 ②의 SSOT 잔여 해소).
- 바꾸는 것:
  1. `CombatUnitHudFeedback.ShowCostPreview(actor, cost)`: **같은 HUD 대상·같은 비용이면 아무것도 하지 않는다.** 대상은 행위자 ID만이 아니라 지금 HUD 객체로 비교한다. 유닛 표시가 다시 만들어져 HUD가 바뀐 경우는 다시 그리게 하려는 것이다. HUD 비용에 쓰는 곳은 이 클래스뿐이다. 공격 모드 비용 표시(`CombatAttackFeedback.ShowAttackCostPreview`)도 같은 함수를 지나므로 함께 멱등이 된다(화면 결과 같음).
  2. `CombatPathInput`: 거울 필드 다섯·`InvalidateDraftDisplay`·같은 값 비교를 지운다. `ShowDraft()`는 지정 중이면 매번 표시를 부른다. 재진입 검사는 지금처럼 지정 세대로 한다. `_draftShown`(소유 표지)과 `ClearDraft`의 "그린 게 있을 때만 지움"은 남긴다. 다른 기능의 호버 칸을 지우지 않으려는 것이다.
- 보존: 화면에 그려지는 값(경로 칸·끝점 상태·후보 칸·비용)은 같다. 같은 값의 반복 호출은 표시 쪽에서 아무 변화가 없다(F13 + 1). 표시 실패 뒤 같은 값 재시도는 거울이 없으니 자동으로 된다.
- 대가:
  - 지정 중 포인터 이벤트마다 표시 호출이 한 번씩 늘어난다(보드 칸 순회, 할당 없음).
  - F14의 시험 10개는 "표시 호출 수"가 아니라 "최종 표시 값"(Probe의 Path·Candidate·PointerValid·Preview)을 단언하도록 바꾼다. 리플렉션 `_shownPath` 단언은 지운다.
  - 멱등 보장은 표시 쪽 순수 시험에 더한다(`CombatUnitHudFeedback` 같은 대상·비용 재호출 무변화·대상이 바뀌면 다시 그림, `FieldHighlightState` 같은 입력 재호출 무변화). 둘 다 관리 실행으로 돈다.
- 다른 안(Q-R5):
  - B: 거울을 입력 쪽 소유자 하나(가칭 `CombatDraftDisplayTracker`)로 묶는다. 시험은 리플렉션 하나만 고치면 되지만, 거울이 남아 SSOT는 그대로다.
  - C: Presenter가 입력 값을 기억한다. 표시 계약이 `MovePreview?` 값이라 지금의 참조 비교를 할 수 없다. 계약을 넓히거나 값 비교로 뜻이 바뀌고, 확인이 Unity 전용(FieldView)이 된다. 권하지 않는다.

## 3. 변경 시나리오 전후 비교 (인계 §4.6)

| 시나리오 | 지금: 고칠 곳 / 읽을 코드 | 분리 뒤 |
|---|---|---|
| 범위를 받는 방식 변경(예: 같은 사실 범위를 캐시해 공격 모드 뒤 바로 다시 보이기, 조회 전용 슬롯으로 옮기기) | 경로 입력 6~8곳 / 지정·복귀 코드 사이 | **소유자 한 파일** / 소유자만, 경로 입력은 호출 줄 |
| 범위 표시 실패·정리 규칙 변경 | 경로 입력 `ClearMovementRange`·`ConsumeMovementRange` / 지정 재개 코드와 섞임 | 소유자 `Clear`·`TryShowAnswer` |
| 이동 모드 시점·조건 변경(MR3) | 경로 입력 훅 4곳 | 경로 입력 훅 4곳(같음). 범위 기계는 읽지 않아도 된다 |
| 복귀 견적 처리 변경(예: 답 받음 규칙, 승인 뒤 제출 조건) | 경로 입력 메서드 넷, 범위 필드·기계 사이 | **복귀 소유자 한 파일**(RT7). 경로 입력은 "언제" 조건만 |
| 복귀 견적을 내는 조건 변경(예: 이동하지 않아도 견적) | 경로 입력 `RefreshReturnEstimate` | 같음(조건은 경로 입력). 기계는 읽지 않아도 된다 |
| 초안 표시 모양·비용 표시 변경 | Presenter + 입력 거울이 무엇을 비교하는지까지 | **표시 쪽만**(RT8). 입력은 값을 넘기기만 한다 |
| 범위·복귀 시험 | 경로 입력 전체 조립(Core 미리보기 포함) | 소유자 순수 시험(Buffer만) + 기존 통합 시험 |
| 새 배경 요청 추가 | 경로 입력 순서 조각을 읽고 고침(M2) | 슬롯 규칙 함수만 부른다(호버 U-2, 이미 작업 트리) |
| 남는 공통 경계 | — | 미리보기 슬롯(전경 요청은 바로 덮어씀), 지정 세대 재진입 검사, `Refresh` 안의 배경 요청 순서(범위 → 복귀 견적) |

## 4. 원칙 점검과 예상 점수

| 원칙 | 점검 |
|---|---|
| SSOT | 범위 상태·복귀 견적 상태가 각자 한 객체에 모인다(L1 해소). 화면에 그려진 초안의 원본은 표시 쪽 하나가 된다(입력 거울 삭제, N3 ②). 슬롯 규칙은 함수 하나(호버 U-2). 이동 모드 조건·원하는 사실은 경로 입력 한 곳. Buffer 사실은 복제하지 않는다(매번 값으로 넘김). |
| SOLID | S: 범위·복귀 견적이 자기 클래스로, 거울이 사라진다. 경로 입력은 "경로 입력 수명 조정"(입력 게이트·지정 수명·공격 Drag 넘겨받기·협력 객체 셋 + `CombatMovementInteraction` 호출 순서)으로 좁아진다. 필드 29 → 21(범위 넷·복귀 하나·거울 다섯이 빠지고 소유자 둘이 들어옴), 781 → 약 680줄. I: 소유자는 표시 계약 한두 멤버만 받는다. O·L: 기존 계약의 서명은 그대로다. 보장은 하나 더해진다(HUD 비용 표시 멱등). D: 새 의존성 없음. |
| FW | 서비스·키·`AddModule`·Factory·Buffer 계약 변경 0. 내부 협력 객체를 Framework System으로 등록하지 않는다(인계 §4.6). 할당 규칙 유지(RT5, RT8은 포인터마다 표시 호출이 늘지만 할당 없음). |
| 독립성 | §3. 범위·복귀 기계는 각자 한 파일, 초안 표시는 표시 쪽만, 시점 변경은 경로 입력에 남는다. |

예상 점수(이동 모드 범위 설계 §8 기준, 세 가지 모두 A안):

| 기준 | 지금 | RT만 | RT + RT7 + RT8(+ 호버 U-2 슬롯 규칙) |
|---|---|---|---|
| SSOT | 8 | 9 | **10** |
| SOLID | 6 | 7 | **8** |
| FW | 9 | 9 | 9 |
| 독립성 | 6 | 7 | **9** |
| 종합 | 7.3 | 8.0 | **9.0** |

- **앞 판 §4.1의 "약 450줄, SOLID 9, 종합 9.3"은 과했다.** 줄을 세어 보면, 옮기는 양은 범위 약 55줄·복귀 기계 약 40줄·거울 약 27줄이다. 지정 수명·입력 게이트·Drag 입구는 그대로 남아 약 680줄이 된다.
- SOLID 9에 닿으려면 입력 게이트(`AllowsInput`·`PlanningActor`·`ActiveActor`)를 순수 판정으로 빼고, 공격 Drag 넘겨받기도 정리해야 한다. 얻는 독립성이 작아 이번 계획에는 넣지 않는다.
- FW 9의 1점은 매 Tick 사실을 여러 번 읽는 것(R1)이다. "외부 표시 콜백 뒤에는 사실을 새로 읽는다"는 의도된 규칙이라 지금은 바꾸지 않는다.

### 4.1 더 올리려면 (사용자 질문 2026-10-19: "더 올릴 수 없나, 큰 뭉텅이를 고쳐야 하나")

- 답: 남는 감점은 `CombatPathInput`의 나머지 하위 기능 둘(복귀 견적·초안 거울)과 슬롯 규칙에서 왔다. 슬롯 규칙은 호버 U-2가 넣었고, 나머지 둘은 RT7·RT8로 다룬다(위 표).
- **고칠 큰 덩어리는 `CombatPathInput` 하나다.** 조정자 `CombatViewSystem`(1,039줄)·`AttackTargetingInteraction`(462)·`PlacementDragInteraction`(374)·`CombatMovementInteraction`(397)·`CombatInteraction`(460)은 이 영역 점수와 관계없다. 그쪽 문제(재평가 N1·N2·N4·N5)는 별개다.
- 10까지 못 가는 곳:
  - SOLID: 경로 입력이 입력 게이트와 수명 훅 시점을 함께 쥔다(위 표 아래 설명).
  - 독립성: 전경 요청(Drag·공격 모드·배치·복귀 버튼)이 한 슬롯을 덮어쓰고, 배경 순서가 `Refresh` 호출 순서에 기댄다.
  - 이것을 없애려면 슬롯을 나눠야 한다(호버 설계 UR4 B안, Buffer·Core·Plan·Job 약 15파일, Core 변경). 비용에 비해 얻는 것이 작아 권하지 않는다.

## 5. 정할 것

| ID | 질문 | 선택지 |
|---|---|---|
| Q-R1 | 소유자의 범위 | **A(권장) 경로 입력 안의 협력 객체**: 기계만 옮기고 시점·원하는 사실은 경로 입력에 둔다(RT1). / B 형제 입력 기능: `CombatInteraction`이 경로 입력과 나란히 부른다. 그러면 범위가 답을 받았을 때 경로 견적 유예를 풀라고 경로 입력에 알리는 연결, 묶음 호출 순서, Factory 변경이 생긴다. 지정과 범위가 유예로 묶여 있어 권하지 않는다. |
| Q-R2 | 표시 계약 | **A(권장) 좁은 계약 둘 — `ICombatMovementRangeFeedback`(범위 두 멤버)·`ICombatReturnFeedback`(`SetReturnState`) — 을 `ICombatMovementFeedback`이 상속**(RT4·RT7, 가짜 수정 0) / B 소유자가 `ICombatMovementFeedback` 전체를 받음(파일 둘 덜 생김, ISP 약함) |
| Q-R3 | 순서(F8·F10) | 호버 U-1·U-2는 이미 작업 트리에 있다(커밋 전, Unity 확인 대기). 같은 파일이라 **그 커밋은 반드시 먼저**다. 그 뒤: **A(권장) 이 분리 → 폴더 정리(M).** M은 아직 Q-M1~Q-M4 결정 전이고, 분리는 동작 동일이라 M 대응표에 새 파일 줄만 더하면 된다. / B 폴더 정리(M) → 이 분리(호버 계획 P1의 "M1은 폴더 정리 뒤" 문구 그대로. 다만 M 결정을 기다려야 한다) |
| Q-R5 | 초안 표시 거울(RT8) | **A(권장) 표시 멱등화 + 입력 거울 삭제**(SSOT 원본이 표시 쪽 하나, 시험 10개를 "최종 표시 값" 단언으로, 멱등 시험은 순수) / B 입력 쪽 소유자로 묶기(시험 거의 그대로, SSOT 9 유지) / C Presenter에 거울(권하지 않음) |
| Q-R4 | 이름 정리 L2(`CombatInteraction.CancelPath`가 `CancelInputs`보다 넓게 지움) | **A(권장) 같은 Phase에서 `CancelPath` → `LeaveMoveMode`로 바꾼다.** 호출은 Router 두 곳뿐이고 시험 호출은 없다. 문서 주석에 범위 유무를 적는다. / B 이름은 두고 XML doc만 고친다 |

## 6. Phase

Task·순서·검증·실행 기록은 [경로 입력 분리 계획](../plans/2026-10-19-combat-path-input-split-plan.md)에 둔다. 이 문서는 규칙만 둔다. Phase는 다섯이다.

1. **R-0 기준선**
2. **R-1 범위 소유자**(RT1~RT6, Q-R4 이름 정리 포함): 기존 시험 무수정
3. **R-2 복귀 견적 소유자**(RT7): 기존 시험 무수정
4. **R-3 초안 표시 멱등화·거울 삭제**(RT8): 기존 시험 10개를 바꾸는 유일한 Phase
5. **R-4 문서·재평가**

## 7. 남는 것 (이번 범위 밖)

- **입력 게이트 순수 판정·공격 Drag 넘겨받기 정리:** SOLID 9로 가는 다음 후보(§4 표 아래).
- **R1 매 Tick 사실 여러 번 읽기:** "콜백 뒤 다시 읽는다" 규칙과 부딪혀 지금은 두고 본다.
- **슬롯 나누기(호버 설계 UR4 B안):** 독립성 10의 조건. 비용이 커 권하지 않는다.

## 8. 원칙 평가 (R-1~R-3 코드 뒤, 2026-10-19)

- 사용자 요청: "SSOT(한 사실은 한 곳에만)와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성 평가 진행해."
- 기준: [설계 세션 인계](../handoffs/2026-09-30-combat-view-design-session-handoff.md) §4. 점수는 1~10(10 = 지적할 것 없음)이다. 비교 기준은 [이동 모드 범위 설계](2026-10-19-combat-move-mode-range-design.md) §8(7.3/10)이다.
- 대상(미커밋 작업 트리 중 이 작업 파일만):
  - 새 파일: `CombatMovementRangeTracker`(120줄)·`CombatReturnEstimateTracker`(92)·`ICombatMovementRangeFeedback`·`ICombatReturnFeedback`
  - 고친 파일: `CombatPathInput`(781 → 676줄, 필드 29 → 22)·`CombatUnitHudFeedback`(94 → 103)·`ICombatMovementFeedback`·`CombatInteraction`·`CombatInputRouter`·값 객체 둘의 주석, 시험

### 8.1 결론

| 기준 | 전 | 예상(§4) | 실제 | 판정 |
|---|---|---|---|---|
| SSOT | 8 | 10 | **10** | 범위 정체성·복귀 견적 기억이 각자 한 객체에 있다. 그려진 초안의 원본은 표시 쪽 하나이고(입력 거울 삭제), 슬롯 규칙은 함수 하나, 이동 모드 조건은 함수 하나다. HUD 비용 기억은 비용을 쓰는 유일한 곳(`CombatUnitHudFeedback`)의 자기 기록이다(grep: `SetCostPreview` 호출 두 곳 모두 이 클래스). |
| SOLID | 6 | 8 | **8** | 소유자 둘은 자기 수명 하나씩만 갖고 좁은 계약만 받는다. `CancelInputs`/`LeaveMoveMode` 이름이 두 층에서 같은 뜻이 됐다(L2 해소). −2: 경로 입력이 아직 허브다(입력 게이트·공격 Drag 넘겨받기·지정 수명·범위/복귀 시점 훅, 676줄, E1). |
| FW 지침·계약 | 9 | 9 | **9** | 서비스·키·`AddModule`·Factory·Buffer·Framework 파일 변경 0. 내부 협력 객체는 등록하지 않았다. 할당이 늘지 않았다. −1: 매 Tick 사실을 여러 번 읽는 것(R1)과 입력 불가 동안 정리 대리자 할당(이전부터)은 그대로다. |
| 변경 독립성 | 6 | 9 | **9** | 범위 기계·복귀 기계는 각자 한 파일, 초안 표시는 표시 쪽만, 새 배경 요청은 슬롯 규칙 함수만 부르면 된다. −1: 배경 요청 순서(범위 → 복귀 견적 → 호버)가 `Refresh`·`CombatInteraction.Poll`의 호출 순서에 기댄다(E2). |
| **종합** | 7.3 | 9.0 | **9.0** | |

발견: 높음 0, 중간 0(평가 중 찾은 중간 1건 E0은 바로 고침), 낮음 4, 참고 2.

### 8.2 SSOT

| 사실 | 원본 | 다른 곳 | 판정 |
|---|---|---|---|
| 이동 가능 목적지 | Core 응답 | 없음 | 지킴 |
| 범위가 어느 사실의 것인가(기다림·보임) | `CombatMovementRangeTracker` | 없음(전에는 요청 객체와 경로 입력에 나뉨, L1) | **해소** |
| 복귀 견적의 원함·답 받음·요청 번호 | `CombatReturnEstimateTracker`(+ 값 객체 `CombatReturnRequest`) | 없음 | 지킴 |
| 지금 그려진 초안(경로·끝점·후보·비용) | 표시 쪽(`FieldHighlightState`·`CombatUnitHudFeedback`) | 없음(입력 거울 필드 다섯 삭제, N3 ②) | **해소** |
| 슬롯 사용 규칙(R-a·R-b) | `CombatPreviewSlotRule` | 범위·복귀 소유자는 R-b를, 경로 입력·호버는 R-a를 이 함수로 판단 | 지킴 |
| 이동 모드 조건 | `CombatPathInput.PlanningActor(Path)` | Drag 시작·범위·공격 Drag 넘겨받기가 같은 함수 | 지킴 |
| 배경 요청 순서 | 이동 모드 범위 설계 MR4·호버 설계 UR4(문서) | 코드는 호출 순서로만 나타남 | 지킴(코드 원본 없음은 E2로 독립성에서 감점) |

### 8.3 SOLID

| 원칙 | 판정 | 근거 |
|---|---|---|
| S | 주의(E1) | 소유자 둘은 바뀌는 이유가 하나다(범위를 받는 방식, 복귀 견적을 받는 방식). 경로 입력에는 입력 게이트(`AllowsInput`·`PlanningActor`·`ActiveActor`), 공격 Drag 넘겨받기(세 메서드), 지정 수명(누름·이동·뗌·취소·Detach), 범위·복귀 시점 훅, 초안·고스트 표시 호출이 남는다. |
| O | 지킴 | 새 배경 요청은 소유자를 하나 만들고 `Refresh`에 한 줄 더하면 된다. 기존 소유자는 고치지 않는다. |
| L | 지킴 | `ICombatMovementFeedback`의 멤버 집합은 그대로다(상속으로 옮김). `ShowCostPreview`는 "대상 교체" 보장을 지키면서 "같은 값이면 그대로"가 더해졌다. 공격 비용 표시도 화면 결과가 같다. |
| I | 지킴 | 소유자는 표시 계약 한두 멤버만 받는다. `ICombatMovementFeedback`은 좁은 계약 둘의 합성이다. |
| D | 지킴 | 소유자는 Buffer 싱크와 좁은 계약을 받는다. 순수 C#이라 Unity 없이 시험한다(새 시험 27개). |

### 8.4 FW 지침·계약

| 항목 | 판정 |
|---|---|
| FW-001~004 서비스·등록·Build·내부 상태 | 변경 없음. Factory 변경 0이라 민형님 공유 불필요 |
| FW-005~007 입력 | 변경 없음 |
| FW-010~012 렌더 | 강조 층·팔레트 변경 없음. 표시 쪽 멱등은 `FieldHighlightState`(이미)와 HUD 비용(이번) |
| P6 규칙 값은 Core | 지킴 |
| 할당 | 포인터 이벤트·지정 중 매 Tick 표시 호출이 늘었다(초안 거울 삭제). 경로는 `FieldHighlightState` 칸 비교·HUD 비교뿐이라 할당이 없다(코드 확인). 측정 기준이 없어 Profiler 측정은 하지 않았다. |
| 코딩 규칙 | Allman·`sealed`·한국어 XML doc·`.meta` GUID 32자리·짧은 `.meta` 꼴(이웃 파일과 같음) |

### 8.5 변경 독립성 (대표 시나리오)

| 시나리오 | 고칠 곳 | 읽을 곳 | 판정 |
|---|---|---|---|
| 범위를 받는 방식(캐시·조회 전용 슬롯 등) | `CombatMovementRangeTracker` | 그 파일 + 시험 | 좋음 |
| 복귀 견적 처리(답 받음·제출 조건) | `CombatReturnEstimateTracker` | 그 파일 + 시험 | 좋음 |
| 초안·비용 표시 모양 | 표시 쪽(Presenter·HUD 피드백) | 표시 쪽만. 비용 표시는 공격 모드와 공유하는 경계 | 좋음 |
| 이동 모드 시점·조건 | `CombatPathInput` 훅 넷 | 범위 소유자의 공개 다섯 멤버 | 좋음 |
| 새 배경 요청 | 새 소유자 + `CombatPreviewSlotRule` 호출 + 호출 순서 한 줄 | 순서 문서(MR4·UR4) | 부분(E2) |
| 입력 허용 규칙 | `CombatPathInput.AllowsInput` | 지정 수명 코드와 같은 파일 | 부분(E1) |

### 8.6 발견

| ID | 등급 | 내용 | 처리 |
|---|---|---|---|
| E0 | 중간(**고침**) | R-3에서 넣은 HUD 비용 기억이 표시 반환(`OnReturned`)을 몰랐다. 같은 풀 객체가 같은 유닛에 다시 대여되면 참조가 같아, 정리된 HUD에 같은 비용을 다시 쓰지 않을 수 있었다. 설계 RT8의 "HUD가 바뀌면 다시 그림"과 어긋났다. | `OnReturned`가 그 유닛의 비용 대상 기억을 버리게 고쳤다(행위자 기억은 남겨 뒤의 지우기는 지금처럼). 시험 `CostPreview_ReturnedAndReRentedSameHud_IsWrittenAgain` 추가, PASS. |
| E1 | 낮음 | 경로 입력이 허브로 남는다(입력 게이트·공격 Drag 넘겨받기·수명 훅, 676줄·필드 22). | SOLID 9 다음 후보: 입력 게이트를 순수 판정으로(§7). |
| E2 | 낮음 | 배경 요청 순서의 코드 원본이 없고 호출 순서(경로 `Refresh` 안 범위 → 복귀 견적, `CombatInteraction.Poll`의 경로 → 호버)에 기댄다. | 수용. 슬롯 규칙(R-a)이 덮어쓰기는 막으므로 순서가 바뀌어도 응답이 늦을 뿐 반복은 없다. |
| E3 | 낮음 | `ConsumeMovementRange`의 try가 이제 소비·슬롯 비움까지 감싼다(전에는 표시만). | 둘은 던지지 않아 동작이 같다. 참고로만 둔다. |
| E4 | 낮음 | 지정 중 포인터 이벤트·매 Tick마다 표시 호출이 늘었다(RT8의 대가). | 할당 없음. 문제가 보이면 표시 쪽에서 거른다(입력 거울은 되살리지 않는다). |
| R1 | 참고 | 검증 범위: MSBuild 오류 0·새 경고 0(다른 세션 미커밋 파일 18개는 HEAD 판으로 고정하고 새 파일은 뺀 빌드). 관리 실행은 사용자 지시로 전체를 멈췄다. 그 전 R-1 빌드 전체 5,790개 중 4,790개가 끝났고 참조(호버 U-2 최종 실행) 대비 상태 변화 0이었다. 바뀐 클래스 묶음 315개 PASS, R-3 뒤 57개(PASS 51·NATIVE 6), 새 시험 31개 PASS. Unity 컴파일은 사용자 확인. Unity Test Runner·재생은 미확인. | 계획 §8 확인 목록 |
| R2 | 참고 | 같은 시각에 다른 세션 둘(공격 궤적 TJ-3·클립 AC-1)이 Combat 폴더를 고치고 있었다. 이 작업과 파일이 겹치지 않는다(`FieldUnitPresenter`는 그쪽이 고쳤지만 이 작업은 그 파일을 바꾸지 않고, `OnReturned` 호출은 HEAD에 이미 있음). | 커밋 때 파일 목록(계획 §9) |
