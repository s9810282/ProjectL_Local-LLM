# 전투 View 단계 2라운드 Task 계획 (2026-10-24)

- 설계: [round2-design](../specs/2026-10-24-combat-view-stage-round2-design.md) §3~§5(W5·W7·W6).
- **결정(2026-10-24 사용자 "모두 A로 하고 2라운드 Task 계획 써줘"):** Q-V10~Q-V18·Q-T4·Q-T5 모두 A, P8~P17은 쓴 그대로다. Q-V9는 사용자 커밋 `23436b00`으로 충족했다.
- **검증(2026-10-24 사용자 "계획 세우기 끝나면 에이전트로 검증해"):** 1판을 읽기 전용 하위 에이전트 둘(W5 / W7·W6)이 코드와 대조했다. 높음 넷·중간 열여섯·낮음 열여섯을 찾았고 이 2판에 반영했다(맨 아래 "검증 반영 기록").
- **계획에서 정한 것은 TP-1~TP-18이다(아래).** 그중 설계와 다른 것이 아홉이다(TP-2·TP-3·TP-4·TP-6·TP-7·TP-8·TP-9·TP-10·TP-15, 각 항목에 "설계와 다름").
- **결정(2026-10-24 사용자 "모두 A로 하고 V2-a 진행해"):** TP-1~TP-18 쓴 그대로(TP-15는 a), Q-T6 a~d 모두 A(다른 사람 시험 넷 허락).
- 기준 코드: HEAD `23436b00`(작업 트리 = HEAD, 커밋 전 변경은 문서뿐). 줄 번호는 이 커밋 기준이다. 앞 Phase가 줄을 바꾸므로 Phase마다 다시 찾는다.
- **진행 규칙(1라운드와 같음)**
  - Phase 하나가 끝나면 멈추고 보고한다. 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인한 뒤 다음 Phase로 간다.
  - Phase 코드가 끝나면 보고 전에 새 에이전트(읽기 전용)로 그 Phase 변경을 검토하고, 지적을 반영한 뒤 보고한다. 검토 결과와 반영 내용은 실행 기록에 적는다.
  - 전투 View 작업이라 **MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다.** 보고에 "컴파일 미확인"을 적는다.
  - 시험 처리(Q-T4 A):
    - 걷은 기능을 보던 우리 시험(사례)은 지운다.
    - 남는 내용을 보는 우리 시험은 호출·입력만 맞춘다. 바뀐 동작의 단언은 TP-4대로 기대값만 맞춘다.
    - 깨지지 않는 시험은 손대지 않는다.
    - 고친 시험은 모두 실행 기록과 공유 메모에 적는다.
  - **다른 사람 시험 — 고칠 수 있는 곳은 아래뿐이다.**
    - Q-T5 A(허락됨): 천명님 `T/CombatMovementViewTests.cs:647` 가짜 `CountingInput`(멤버 한 줄), 민형님 `T/CombatSceneEventPresenterTests.cs:173`, 현강님 `T/CombatSceneEventPresenterTests.cs:1449`(둘 다 `CombatDisplayServices` 인자). 천명님 `BindServices` 두 곳(`CombatMovementViewTests.cs:423`·`CombatViewSystemTests.cs:1338`)도 허락됐지만, 인자는 그 호출의 우리 줄(`:427`·`:1341`) 끝에 붙여 천명님 줄은 바꾸지 않는다(TP-12).
    - Q-T6 a~d(허락 대기): 아래 "결정 대기" 표.
  - 우리 영역 안의 다른 사람 운영 줄을 고치면 Phase V2-h 공유 메모에 적는다. 이번에 닿는 줄은 각 Phase에 적었다.
  - Framework·GlobalSystem·Core·다른 씬은 고치지 않는다. 연결 줄은 `F/InGameViewSystemFactory.cs:60` 하나다(민형님 공유 문서에 적음).
  - 새 파일은 `.meta`와 함께 만든다. 지우는 파일은 `.meta`와 함께 지운다. 이름을 바꾸는 파일은 `git mv`로 `.cs`와 `.meta`를 함께 옮긴다(GUID 유지).
  - 새 파일 위치는 설계 P15다. 루트에는 두지 않는다.
- 경로 약어:
  - `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `B/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/`
  - `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`
  - `T/` = `Assets/Tests/EditMode/Framework/`

## 0. 점수 흐름 (1라운드 측정 8.3 기준, 알려진 문제 기준, 누적)

| Phase 끝 | 8.3에서 | 바뀌는 것 |
|---|---|---|
| V2-a (W5 작은 셋) | **8.46** | 있는 기능 중간(V-M8 비우기 여섯 곳), 독립성 낮음 둘(V-L14 층 배타, 아이템 기본 인자) |
| V2-b (W5 계약) | **8.54** | SOLID 중간(V-M5 입력 묶음 허브). 대신 어댑터 낮음 하나를 예상해 SOLID 낮음이 상한(−1.0)에 걸린다 |
| V2-c (W5 넘겨받기) | 8.54 | V-L7 일부(점수 변화 없음으로 셈) |
| V2-d (W7) | **8.67** | SOLID 중간(V-M7 사건 형식 분류기) |
| V2-e~V2-g (W6) | **8.92** | SOLID 중간(V-M4 조정자), FW 낮음(V-L10), 불필요 중간 → 낮음(V-M12 남은 것 중 TP-17의 남기는 null 허용만 남음, 2026-10-25 셋) |
| V2-h·V2-i | 8.92 | 문서·측정 |

- 기준별 2라운드 뒤: SSOT 9.25 · SOLID 8.25 · FW 9.75 · 있는 기능 9.5 · 독립성 8.5 · 불필요 8.25 → 평균 8.92.
- 설계 §8의 SOLID 8.5보다 0.25 낮게 잡았다. 어댑터 낮음을 더하면 SOLID 낮음이 상한에 걸리기 때문이다(V-L7·V-L8·층 다섯 곳·표시의 전역 상태·어댑터).
- 조정자가 크기 때문에 V-M4를 중간으로 남기면 SOLID 7.5, 합 약 8.8이다(설계 §8).
- **알려진 문제 기준이다.** 실제 점수는 V2-i의 A/B 측정으로 확인한다. 기준 판 B는 `23436b00`이다.

## Phase V2-a — W5 작은 셋: Buffer 번호 비우기·아이템 드래그 사실·아군 호버 층 (V-M8·V-L14·새로 찾은 낮음)

**목표:** 묶음 구조를 바꾸기 전에, 서로 독립인 작은 변경 셋을 먼저 끝낸다(TP-1).

### Va2-A Buffer의 번호 있는 비우기 (W5-4, V-M8)

| Task | 파일 | 내용 |
|---|---|---|
| A.1 | `B/CombatView/CombatViewBuffer.cs` | 쓰기 계약(`:33-51`)에 `bool ClearPreview(int number)`를 더한다. 구현은 `number > 0 && _hasPreview && _preview.Number == number`일 때만 지금 `ClearPreview()`를 부르고 true다. 조건 없는 `ClearPreview()`는 남긴다(P13). |
| A.2 | `V/Input/Interaction/PlacementDragInteraction.cs` | `ClearOwnedPreview`(`:338-343`)는 `_input.ClearPreview(_previewRequestNumber)`만 부른다. 그러면 읽기 Sink `_requests`(`:61` 대입, `:341` 읽기)를 아무 데서도 읽지 않으므로 필드는 지우고 생성자의 null 검사는 남긴다(생성자 서명 그대로). |
| A.3 | `V/Input/Interaction/AttackTargetingInteraction.cs` | `InvalidateHoverPreview`(`:483-489`)는 `_input.ClearPreview(_hoverRequestNumber)`, `ClearOwnedPreview`(`:492-501`)는 범위 번호·호버 번호를 차례로 `ClearPreview(n)`에 넘긴다(슬롯에 있는 하나만 비워짐). `_requests`는 `PreviewAnswered`(`:153`)가 쓰므로 남는다. |
| A.4 | `V/Input/Interaction/CombatUnitRangeHover.cs` | `ClearOwnedRequest`(`:252-258`)는 `_input.ClearPreview(_requestNumber)`. |
| A.5 | `V/Input/MovePath/CombatMovementInteraction.cs` | `ClearOwnedPreview`(`:391-395`)는 `_input.ClearPreview(_requestNumber)`. **천명님 줄**(`87348b86`)이다. |
| A.6 | `V/Input/MovePath/CombatMovementRangeTracker.cs` | **번호는 요청을 지우기 전에 잡는다(F1).** `TryShowAnswer`(`:85-99`)는 `TryConsume`이 안에서 `Cancel()`로 번호를 0으로 만들므로, 소비가 확인한 같은 번호인 `preview.RequestNumber`로 `_write.ClearPreview(n)`을 부른다. `Clear`(`:120-140`)는 `ownsSlot`을 잡는 자리(`:124`)에서 `int number = _request.Number`도 잡고, 정리 대리자가 그 값을 쓴다. 요청 객체 `CombatMovementRangeRequest`에 `internal int Number` 읽기만 더한다. `OwnsPreview`는 응답 소비 검사에 그대로 쓴다. |
| A.7 | `V/Input/MovePath/CombatReturnEstimateTracker.cs` | `Clear`(`:84-91`, 비우기 `:87-88`)는 `_request.Reset()` 전에 `_write.ClearPreview(_request.Number)`를 부른다(지금도 Reset 앞). `CombatReturnRequest`에 번호 읽기가 없으면 `internal int Number`를 더한다. |
| A.8 | 시험 | 계약 구현 가짜 둘에 한 줄씩: 우리 `T/CombatInteractionTests.cs:1112` `RejectingSubmitSink`, 천명님 `T/CombatMovementViewTests.cs:647` `CountingInput`(Q-T5 허락분). 둘 다 안쪽 Sink로 넘긴다. 우리 `T/CombatMovementRangeTrackerTests.cs:85`("소비한 자기 슬롯은 비운다")는 A.6을 지키면 그대로 통과한다. |

**동작:** 같다. 번호는 `Submit`·`SetPreview`가 같은 카운터로 하나씩 발급하고 `Clear`가 카운터를 유지하므로, 번호가 같으면 같은 요청이다(종류·행위자 검사는 군더더기).

### Va2-B 아이템 드래그 여부를 사실로 (W5-2, 새로 찾은 낮음, TP-3)

| Task | 파일 | 내용 |
|---|---|---|
| B.1 | `V/Input/CombatInputFacts.cs` | 생성자 끝에 선택 인자 `isItemDragActive = false`, 속성 `IsItemDragActive`를 더한다(1라운드 P1). |
| B.2 | `V/CombatViewSystem.cs` | `ReadFacts`(`:997-1024`)가 `isItemDragActive: _interaction != null && _interaction.IsItemDragActive`를 채운다. |
| B.3 | `V/Input/CombatInputRouter.cs` | `ResolvePointerTarget`(`:83`)의 셋째 인자 기본값을 지운다. Router의 두 곳(`:118`·`:210`)은 지금처럼 묶음의 실시간 상태를 넘긴다(TP-3). `:118`에는 아이템 인자를 새로 넘긴다(`:110`이 아이템 중이면 먼저 돌아가므로 결과 같음). |
| B.4 | `V/Input/MovePath/CombatPathInput.cs` | `AllowsInput`(`:104`)은 `facts.IsItemDragActive`를 넘긴다. 경로는 Path만 보므로 결과 같다. |
| B.5 | `V/Input/Interaction/CombatInteraction.cs` | `Poll`(`:347`)은 지금처럼 실시간 상태를 넘긴다(이미 셋째 인자를 넘김). 확인만 한다. |
| B.6 | `V/CombatViewSystem.cs` | `SyncEnvironmentHover`(`:1181`, 현강님 줄)는 `facts.IsItemDragActive`를 넘긴다. 환경 호버는 None인지만 보므로 결과 같다. |
| B.7 | 시험 | 우리 둘: `T/CombatInputRouterTests.cs:62`·`T/CombatItemDragInteractionTests.cs:192`(두 인자 → 셋째 `false`). `:190`·`:194`는 이미 셋째 인자를 넘긴다. |

### Va2-C 아군 호버 층 분리 (W5-6, V-L14, P12)

| Task | 파일 | 내용 |
|---|---|---|
| C.1 | `V/Display/Field/FieldHighlightLayers.cs` | 열거에 `AllyHoverRange = 1 << 10`(요약은 설계 W5-6)을 더한다. `DrawOrder`(`:75-87`)에 `AttackRange` 바로 다음 자리로, `ColorOf`(`:126-142`)에 `FieldHighlightColor.AttackRange`로, `FillsCell`(`:148-151`)에 더한다. 고칠 주석: 우선순위 요약(`:67`), `EnemyAttackRange` 요약의 "아군 유닛·공격 모드 범위는 AttackRange"(`:33-35`), `FillsCell` 요약(`:145`). |
| C.2 | `V/Display/Field/FieldHighlightState.cs` | **행동 중 마스크 `ActingLayers`(`:68-70`)에 `AllyHoverRange`를 더한다(TP-4).** 아군 호버 층은 `IdleLayers`(`FieldOutlineRenderer.cs:36`)에도 없어서, 빼면 GridOutline에서 아군 호버 범위 테두리·채우기가 사라진다. |
| C.3 | `V/Display/Feedback/CombatAttackFeedback.cs` | `LayerOf`(`:70-79`)의 Ally를 `AllyHoverRange`로 바꾼다. `ShowHoverRange` 요약(`:47-50`)을 고친다. |
| C.4 | `V/Input/Interaction/CombatInteraction.cs` | `AttackModeRequested` 요약의 "호버 범위는 공격 범위와 같은 층이라"(`:320-321`)를 고친다(층이 나뉨, 배타는 전경 규칙). |
| C.5 | 시험(우리, TP-4) | `T/FieldOutlineRendererTests.cs`: 손잡이 수 `:84` 12 → 13, `:312` 24 → 26, `:575` 13 → 14, `:593` 12 → 13. `FillNames`(`:537-540`)에 `"FieldFill_AllyHoverRange"`를 `"FieldFill_AttackRange"` 다음에 넣는다(`:527`·`:533`은 그 길이를 따라 맞음). `T/CombatAttackFeedbackTests.cs:125·128`의 아군 호버 단언은 `AllyHoverRange`로 맞춘다. **`:140`은 공격 모드 범위(`:138` `ShowRangeCells`) 단언이라 그대로 둔다(F7).** `T/CombatUnitRangeHoverTests.cs:301`의 단언 문구("같은 층")만 고친다(깨지지 않음). |

**동작:** 화면은 같다. 색이 공격 범위 색과 같고, 두 층이 함께 켜지지 않는다. 팔레트·`FieldView`·셀 색 모드·아웃라인은 `DrawOrder`·`ColorOf`·`FillsCell`을 돌므로 따로 고칠 것이 없다. 다른 시험의 `FieldHighlightLayer.AttackRange` 단언(`CombatDisplayReuseTests:216-217`·`CombatObjectiveDisplayTests:62` 등)은 공격 모드·정적 강조를 보는 것이라 그대로다.

**다른 사람 줄(V2-a):** 천명님 `CombatMovementInteraction.cs:391-395`(A.5)·`AttackTargetingInteraction.cs:483-489`(A.3, 우리 줄은 `:486`뿐이고 바뀐 `:487-488`은 천명님 줄), 현강님 `CombatViewSystem.cs:1181`(인자 하나). 호진님 `CombatMovementRangeTracker`의 빈 범위 근거 줄(`:48-63`·`:90-92`)은 닿지 않는다.

## Phase V2-b — W5 공통 수명 계약과 목록 하나 (V-M5, W5-1·W5-3)

**목표:** 입력 기능 다섯이 공통 계약에 어댑터로 참여하고, 묶음의 수명 메서드는 목록을 돈다. 전경 전환 규칙을 한 메서드로 모은다. 조정자는 Tick에서 입구 하나만 부른다.

| Task | 파일 | 내용 |
|---|---|---|
| Vb2.1 | 새 `V/Input/Interaction/CombatInputOwner.cs`(+ `.meta`) | 추상 클래스(설계 W5-1 스케치). 멤버: `Target`·`IsActive`·`IsBackground`, `PointerMoved`·`SelectPressed`·`SelectReleased`(`in CombatPointerInput`)·`SelectCanceled`, `PreviewAnswered(in CombatInputTick)`·`Poll(in CombatInputTick)`, `BeforeResult`·`AfterResult`·`AfterAcknowledged`, `YieldToForeground`·`CancelPresentation`·`CancelInputs`(기본 = `CancelPresentation`)·`Stop`(기본 = `CancelInputs`). |
| Vb2.2 | 새 `V/Input/Interaction/CombatPointerInput.cs`·`CombatInputTick.cs`(+ `.meta`) | `readonly struct` 둘(P11). 포인터: 사실·캐릭터·x·y·대상. Tick: 사실·캐릭터·마지막 응답. **배경 소유자에게 줄 "다른 소유자 쉼" 값은 struct에 넣지 않고, 전경 처리 뒤에 묶음이 계산해 배경 단계 인자로 넘긴다(F4).** 배경 단계는 Vb2.10의 식을 쓴다. |
| Vb2.3 | `PlacementDragInteraction.cs` | 끝에 private 어댑터 + `internal CombatInputOwner Owner { get; }`(생성자에서 한 번). 맡는 단계: 대상 Placement, 활성 = `IsDragging`, 포인터 셋, `SelectCanceled` = `Cancel`, `PreviewAnswered` = `PreviewAnswered(t.Facts.PreviewAnswer)`, `AfterResult`, `CancelPresentation`. |
| Vb2.4 | `AttackTargetingInteraction.cs` | 대상 Attack, 활성 = `IsActive`, `PointerMoved`·`SelectReleased`(누름 없음), `PreviewAnswered(facts, chars)`, `BeforeResult`, `AfterResult(facts)`, `CancelPresentation` = `Exit`. `SelectCanceled`는 없음. |
| Vb2.5 | `CombatItemDragInteraction.cs` | 대상 Item, 활성 = `IsDragging`, `PointerMoved(chars, x, y)`·`SelectReleased`, `SelectCanceled`·`CancelPresentation` = `Cancel`, `AfterResult(facts)`. |
| Vb2.6 | `CombatUnitRangeHover.cs` | 배경 소유자(`IsBackground` 참, 대상 None). 배경 단계 셋: 포인터 이동(`PointerMoved(facts, chars, x, y, isTarget)`), 누름 뒤 내려놓기(`Suspend`, 활성 전경이 생겼을 때), Tick 감시(`Poll(facts, chars, ownersIdle)`). `PreviewAnswered(t.Facts)`, `YieldToForeground`·`BeforeResult`·`CancelPresentation` = `Suspend`, `CancelInputs`·`Stop`·`SelectCanceled` = `Clear`(지금 `Suspend` 뒤 `Clear`와 같음). |
| Vb2.7 | `V/Input/MovePath/CombatPathInput.cs` | 대상 Path, 활성 = `IsPlanning`. 포인터 셋, `Poll(t)` = `OnPreview(t.LatestPreview)`(있을 때) → `Refresh()`(지금 묶음 `Poll(preview)` 본문, 재진입 검사 포함), `AfterAcknowledged` = `Refresh`, `BeforeResult`·`CancelInputs`·`SelectCanceled` = `CancelInputs`, `Stop` = `Detach`. `CancelPresentation`은 없음(경로 제외). **정지 뒤 무시는 새 읽기 `IsDetached`(= `!_attached`) 하나로 한다(F13).** `Detach`가 콜백 전에 `_attached = false`를 세우므로(`:154-157`) 재진입에서도 같은 뜻이다. 어댑터의 모든 단계는 `IsDetached`면 돌아간다. |
| Vb2.8 | `CombatInteraction.cs` | 생성자(`:63-72`)는 서명 그대로 받은 기능들로 목록을 만든다. 순서 = 넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 호버(TP-2, 설계와 다름). 대상별 표도 만들고 같은 대상이 둘이면 예외(P10). 넘겨받기는 V2-c 전까지 지금 코드 그대로 두고, 목록에는 V2-c에서 넣는다. **`_path` 필드는 남긴다.** 이름 입구(`LeaveMoveMode`·`RequestReturn`·`Cancel`·넘겨받기)는 지금처럼 `_path`를 통해 부르고, `Stop`은 목록을 돌기 **전에** `_path = null`을 한다(F2, 지금 `:450-451`과 같음). |
| Vb2.9 | 같은 파일 | 수명 메서드를 목록 돌기로 바꾼다: `BeforeResult`·`AfterResult`·`AfterAcknowledged`·`CancelPresentation`·`CancelInputs`·`Stop`(소유자마다 `CleanupErrors.Try`, 모아 던짐, 지금 문구 그대로)·`SelectCanceled`·`PreviewAnswered`(쓴 소유자에서 멈춤, 답이 없으면 돌지 않음)·`Poll`. 각 메서드에서 V2-c 전까지 남는 `_attackDrag?.Invalidate()`는 맨 앞에 그대로 둔다. |
| Vb2.10 | 같은 파일 | 포인터 셋(`:208-247`)은 대상 표에서 소유자를 찾아 부른다. 서명은 그대로다(P8). 배경 소유자는 **전경 처리 뒤에** 지금 식 그대로 판단한다(F4). 포인터 이동: `IsRangeHoverTarget(target)`(`:220`). 누름: `IsInputOwnerActive`면 내려놓기(대상 검사 없음, `:232-233`). Tick: 감시 단계의 전경(경로) 뒤에 `IsRangeHoverTarget(ResolvePointerTarget(layer, 실시간 공격, 실시간 아이템))`(`:347-348`). 1인자 `Poll(preview)`는 사실이 없으므로 배경 소유자를 돌지 않는다(지금과 같음). |
| Vb2.11 | 같은 파일 | `BeginForeground(next)`(설계 W5-1 스케치)를 더하고 `SelectDeckUnit`(`:113-120`)·`SelectItem`(`:128-135`)·`AttackModeRequested`(`:324-333`)의 정리 줄을 그것으로 바꾼다. 활성이 아닌 소유자를 부를 때의 차이는 설계 W5-1 끝(세대 번호만 오름)과 같다. `AttackModeRequested`의 호버↔배치 정리 순서가 바뀌지만 슬롯 번호가 달라 결과는 같다. |
| Vb2.12 | 같은 파일 | `IsInputOwnerActive`(`:99`)는 "배경 아닌 소유자 중 활성이 있나" 돌기로 바꾼다. `IsItemDragActive`·`IsAttackActive`·`IsPathPlanning`·`Drag`·`Mode`는 지금처럼 기능에 묻는다(P8). |
| Vb2.13 | 같은 파일 | 새 입구 `Tick(in CombatInputFacts facts, chars, CombatPreviewResult latest, bool answered, Func<CombatInputFacts> readAnswerFacts)`: 감시 단계(`Poll` 돌기) → `answered`이면 **그때** `readAnswerFacts()`로 응답 사실을 읽어 응답 단계(`PreviewAnswered` 돌기)(F3). 지금 순서(경로 Poll 뒤에 응답 사실을 읽음)와 같다. 기존 `Poll(preview)`·`Poll(preview, facts, chars)`·`PreviewAnswered(facts, chars)`는 남긴다(시험이 부름, P8). |
| Vb2.14 | `V/CombatViewSystem.cs` | Tick의 두 호출(`:359-370`)을 `_interaction.Tick(ReadFacts(), chars, latest, signal.PreviewAnswered, _readFollowUpFacts)` 하나로 바꾼다. `_readFollowUpFacts`는 생성자에서 한 번 만드는 대리자다(지금 `_readPathFacts`와 같은 꼴). `CanContinue()` 검사는 지금처럼 응답이 있었을 때만 한다(`if (signal.PreviewAnswered && !CanContinue()) return;`, F3). |
| Vb2.15 | 시험 | 바꿀 것이 없을 것으로 본다(묶음·기능의 바깥 서명 그대로). 순서가 바뀌는 두 단계(`AfterResult`·`PreviewAnswered`)를 단언하는 시험이 있는지 구현 때 찾는다. 있으면 멈추고 묻는다. |

**동작:**
- 같다. 순서가 바뀌는 두 단계는 결과가 같다. `AfterResult`는 배치·아이템 Drag가 서로 배타라서(`:117`·`:125`), `PreviewAnswered`는 번호가 유일하고 거짓을 돌려주기 전 부작용이 없어서다.
- 덱 카드·아이템을 집을 때 쉬고 있는 공격·넘겨받기의 세대 번호가 오르는 것만 다르다(보이지 않음).
- `Stop`은 지금 "아이템·배치·공격·호버 내려놓기"를 시도 하나로 묶는데(`:455`), 목록으로 바꾸면 소유자마다 시도한다. 하나가 실패해도 나머지 소유자의 정리가 더 돌고, 모은 예외 수가 바뀔 수 있다(F11). 단언하는 시험은 없다.

**다른 사람 줄:** 현강님 `CombatInteraction.cs:109-123`(덱 입구 셋, 몸통만 `BeginForeground`로), `PlacementDragInteraction`(어댑터만 더함), 호진님 묶음의 넘겨받기 줄(V2-c까지 그대로), `CombatPathInput`(우리 줄 538/720, 천명님·호진님 줄은 어댑터와 `IsDetached`만 더함).

## Phase V2-c — W5 공격 Drag 넘겨받기 소유자 (W5-5, V-L7 일부)

| Task | 파일 | 내용 |
|---|---|---|
| Vc2.1 | 새 `V/Input/Interaction/ICombatAttackDragTarget.cs`(+ `.meta`) | 넘겨받는 쪽 좁은 계약: `TryGetAttackDragOrigin`·`IsAttackDragOriginCurrent`·`TryBeginTransferredDrag`·`PointerMoved`·`SelectReleased`·`CancelInputs`·`IsDetached`. |
| Vc2.2 | `CombatPathInput.cs` | **명시적 인터페이스 구현으로 단다(F5).** 출발점 셋(`:117`·`:132`·`:141`)과 `CancelInputs`(`:457`)는 `internal`이라 암시적 구현이 안 된다(CS0737). `bool ICombatAttackDragTarget.TryGetAttackDragOrigin(...) => TryGetAttackDragOrigin(...)` 꼴로 넘긴다. 호진님 줄(`:109-150`)은 그대로다. |
| Vc2.3 | 새 `V/Input/Interaction/CombatAttackDragHandover.cs`(+ `.meta`) | 몸짓(`CombatAttackDragGesture`, 호진님 파일 그대로)·넘겨받는 쪽(없을 수 있음)·공격 기능을 갖는 소유자. `TryHandlePointer(request, x, y, blockedByUi)`는 묶음 `TryHandleAttackDragPointer`(`:152-206`) 본문을 옮긴 것이다. **경로 null·정지 검사 셋 모두(`:162`·`:172`·`:180`의 `path != null`·`path == null`, `:165`·`:195`·`:203`의 `ReferenceEquals(path, _path)`)를 `_target == null \|\| _target.IsDetached` 꼴로 바꾼다(F6).** 경로 없이 몸짓만 넘긴 조립(`CreateInteraction` `:88-90`)에서는 `_target`이 null이다. 어댑터: 대상 None, `Poll` = 후보 유효성 검사(`:356-358`), `BeforeResult`·`CancelPresentation`·`CancelInputs`·`Stop`·`SelectCanceled` = `Invalidate`. |
| Vc2.4 | `CombatInteraction.cs` | 생성자가 `attackDrag`(몸짓)가 있으면 넘겨받기 소유자를 만들어 목록 맨 앞에 넣는다(서명 그대로). `TryHandleAttackDragPointer`는 이름을 남기고 위임한다. 수명 단계의 무효화 다섯(`BeforeResult` `:393`·`CancelPresentation` `:422`·`CancelInputs` `:436`·`Stop` `:449`·`SelectCanceled` `:265`)과 `Poll`의 조건 블록(`:356-358`)을 지운다. **의도 입구 넷(`RequestReturn` `:252`·`CancelDragOrAttack` `:294`·`LeaveMoveMode` `:309`·`AttackModeRequested` `:326`)은 `_handover?.Invalidate()` 한 줄씩을 남긴다(F10).** `AttackModeRequested`의 무효화는 지금처럼 `CanEnter` 앞이다. |
| Vc2.5 | 시험 | 바꿀 것이 없을 것으로 본다. 호진님 `HojinCombatAttackDragTests`는 묶음 공개 멤버·`CreateInteraction`만 쓴다. 정지 뒤 사례(`:203`·`:236`·`:296`)는 `Owns(generation)`로도 막혀 결과가 같다(검증 확인). 구현 때 다시 대조하고, 다르면 멈추고 묻는다. |

**동작:**
- 같다(같은 문장을 옮김, 재진입·정지 검사만 같은 뜻의 다른 식).
- `CancelInputs`는 지금 몸짓을 두 번 무효화한다(`:436`, 그리고 `:438` → `:422`, 경로 취소 뒤). 목록으로 바꾸면 경로 취소 앞에서 한 번이다. 경로 취소 콜백 안에서 새로 누른 경우만 다르다(F11, 운영에서 닿기 어려움).

**다른 사람 줄:** 호진님 묶음 `:51`·`:63`·`:69`·`:152-206`·`:356-358`과 무효화 열 곳(수명 다섯·`Poll` 하나는 옮김, 의도 넷은 남김), 호진님 `CombatPathInput.cs:109-150`(그대로, 명시적 구현만 더함).

## Phase V2-d — W7 사건 형식 표 하나 (V-M7)

| Task | 파일 | 내용 |
|---|---|---|
| Vd2.1 | `V/Playback/Ordering/CombatEventRoutes.cs`(+ `.meta`) → `CombatEventTypeTable.cs`(+ `.meta`) | `git mv`로 이름을 바꾸고(GUID 유지, TP-8) 클래스를 `CombatEventTypeTable`로 넓힌다. 경로 열거 `CombatEventRoute`는 같은 파일에 남긴다(P14). 줄(Row)은 형식마다 하나: 처리기 칸, 경로 함수(기본 Event), 행동 시작, 행위자, 주체, 피해 근원, 공격 출처, 이동 사건, 간격 종류, "사실만 보이면 머묾", 화면 전환. |
| Vd2.2 | 같은 파일 | 새 열거 `CombatEventHandlerSlot`(Attack·Roar·Summon·Character·Turn·Facts), `CombatEventPacing`(None·Attack·AttackLike·Action·Turn), `CombatPhaseTransitionKind`(None·Night·Morning). 표의 열이라 같은 파일에 둔다(배치 안내 "파일 하나에 형식이 여럿"). |
| Vd2.3 | 같은 파일 | 질문 입구: `RouteOf`·`EventTypes`·`IsEventType`(이름 그대로)·`IsActionStart`·`ActorOf`·`TryGetSubject`·`TryGetOrigins`·`TryGetAttackSource`·`IsMovement`·`PacingOf`·`HoldsWhenShownOnly`·`TransitionOf`·`HandlerSlotOf`. 값은 아래 대응표를 그대로 옮긴다. **지킬 규칙 셋(M7):** (1) 모든 질문은 null 사건에 예외 없이 "없음"으로 답한다. 표를 찾기 전에 null을 거른다(시험 `CombatEventRoutesTests.cs:45` `IsEventType(null)` = false, `:101` `RouteOf(null)` = Skip). (2) `TryGetSubject`는 고른 값이 `EntityId.Empty`면 false다(`CombatEventSubjects.cs:43-44·50`). (3) `TryGetOrigins`는 목록이 null이거나 비면 false다(`:77`). `CombatConcurrentGroups.cs:96`의 회복 사건 붙이기가 이 규칙에 기댄다. |
| Vd2.4 | `V/Playback/Ordering/CombatActionBoundary.cs`(+ `.meta`) | 지운다. 부르는 9곳: `CombatSceneEventPresenter.cs:304`, `CombatConcurrentGroups.cs:50`, `OrderedCombatPresentation.cs:271`, `CombatStepPlayer.cs:129`, `CombatSubjectBlocking.cs:46`, `CombatHitFollowUps.cs:90`, `CombatHitGate.cs:135`, `CombatStepContactGate.cs:134`, `CombatStepContacts.cs:101` → `CombatEventTypeTable.IsActionStart`. 이 클래스를 가리키는 주석: `CombatStepContacts.cs:31`, `CombatHitFollowUps.cs:25`, `OrderedCombatPresentation.cs:263`, `CombatConcurrentGroups.cs:12`. |
| Vd2.5 | `V/Playback/Ordering/CombatEventSubjects.cs` | `TryGetSubject`(`:39-51`)·`TryGetOrigins`(`:67-78`)를 지우고 표로 옮긴다. 남는 것은 `OriginKey`·`TryGetOriginKey`·근원 합치기(`:80-99`)다. 부르는 곳(`CombatConcurrentGroups.cs:56·95·96`, `CombatHitFollowUps.cs:99-100`, `CombatStepPlayer.cs:129`, `CombatSubjectBlocking.cs:46`, 같은 파일 `:60`)을 표로 바꾼다. 요약의 "한 곳" 설명을 고친다. |
| Vd2.6 | `V/Presentation/Attack/CombatHitFollowUps.cs` | `IsMovement`(`:52-54`)·`TryGetSource`(`:62-79`)를 표로 옮긴다. 부르는 곳(`CombatConcurrentGroups.cs:158·176·186·190`, `CombatHitGate.cs:137`, `CombatStepContactGate.cs:134`, `CombatStepContacts.cs:101`, 같은 파일 `:92·97`)을 바꾼다. |
| Vd2.7 | `V/Playback/Support/CombatEnemyPresentationPacing.cs` | `BeforeEvent`(`:61-105`)의 형식 검사를 `PacingOf(e)` switch로 바꾼다. 턴 안의 갈래는 `e.Kind`(Core 값)로 그대로다. `HoldAfter`(`:111-117`)는 `HoldsWhenShownOnly(e)`. 서명은 그대로다. 요약 `:58`의 "처리기 `ActorOf`가 원본"을 "형식 표가 원본"으로 고친다. |
| Vd2.8 | `V/Playback/Handlers/` | `CombatEventHandler.ActorOf`(`:20`)와 넷의 재정의(공격 `:55`·포효 `:50`·소환 `:38`·턴 `:19`)를 지운다. 요약 `CombatEventHandler.cs:11`도 고친다. `CombatSceneEventPresenter.cs:267`은 `CombatEventTypeTable.ActorOf(e)`를 부르고, 주석 `:266`을 고친다. `CombatEventHandlerTable.Create`(`:23-41`)는 `HandlerSlotOf`로 처리기를 고른다(인자 그대로). **`Add`의 `CombatEventRoutes.IsEventType`(`:55`)을 새 이름으로 바꾸고, `Add`의 기존 확인 두 개(`:52-56`, sealed·표 소속)는 남긴다(M6, 낮음 L4).** 주석 참조 `:9`·`:22`를 고친다. |
| Vd2.9 | `V/Playback/CombatSceneEventPlayback.cs`·`OrderedCombatPresentation.cs` | `CombatEventRoutes.RouteOf` 부르는 곳(`CombatSceneEventPlayback.cs:233`, `OrderedCombatPresentation.cs:77·107·214`)을 새 이름으로. `CombatSceneEventPlayback`의 민형님 줄(`:35·53·62·72·75·128·164·172·186·188`)은 닿지 않는다. |
| Vd2.10 | `V/Playback/CombatPhaseTransitionPresenter.cs:33-37`(민형님) | 설계 W7-3 diff대로 `TransitionOf`를 읽는다(Q-V17 A). `using`이 필요하면 하나 더한다. |
| Vd2.11 | 시험(우리) | `CombatActionBoundaryTests`(`:46-59`) → `CombatEventTypeTable.IsActionStart`(확인 내용 그대로, 파일 이름은 그대로). `CombatEventRoutesTests`(16곳)·`CombatSceneEventPresenterTests.cs:184·494·543·570`·`CombatEventHandlerTableTests.cs:51` → 새 이름. `CombatHitFollowUpsTests.cs:34·118` → `CombatEventTypeTable.IsMovement`. `CombatEventHandlerTableTests.cs:70`의 `none.ActorOf(attack)` 단언은 걷은 기능이라 지운다(TP-9, 설계와 다름). 같은 파일 요약 `:13`을 고친다. `CombatEnemyPresentationPacingTests`·`CombatPresentationSettingsDataTests`는 그대로다. 민형님 `CombatPhaseTransitionPresenterTests`는 그대로다. |

**값 대응표(옮기기만 함, 동작 같음):**

| 질문 | 지금 식(원본) |
|---|---|
| 행동 시작 | 공격·포효·소환·턴 사건(`CombatActionBoundary.cs:20-21`) |
| 행위자 | 공격 `Attack.AttackerId`, 포효 `BossId`, 소환 `Record.SummonerId`, 턴 `ActorId`, 그 밖 Empty(처리기 넷) |
| 주체 | 캐릭터·구조물 `Final?.EntityId ?? Expected?.EntityId ?? Empty`, 보유 효과 `CharacterId`, 장애물 반응 `ObstacleId`, 아이템 `HolderId`. Empty면 false(`CombatEventSubjects.cs:41-50`) |
| 피해 근원 | 캐릭터·구조물 `DamageOrigins`, 보유 효과·장애물 반응 `Origin`(null 아니면 하나). null이거나 비면 false(`:69-77`) |
| 공격 출처 | 공격 사건 전부, 포효는 `Attack.HasValue`일 때(`CombatHitFollowUps.cs:62-79`) |
| 이동 사건 | 캐릭터 사건 `Moved`·`Returned`·`Relocated`(`:52-54`) |
| 경로 | null → Skip, 캐릭터 `Moved` → Step, 캐릭터 `Returned`이고 전후 모두 필드 → Return, 기믹 레일 발동 → Railway, 표에 있음 → Event, 없음 → Skip(`CombatEventRoutes.cs:63-74`) |
| 간격 종류 | 성공한 공격 → Attack, 포효 → AttackLike, 소환 → Action, 턴 → Turn(`CombatEnemyPresentationPacing.cs:66-102`) |
| 머묾 | 포효·소환(`:116`) |
| 화면 전환 | 턴 `CombatEntered` → Night, 턴 `NightEnded`·결과 `Victory` → Morning(`CombatPhaseTransitionPresenter.cs:33-35`) |

**다른 사람 줄:** 민형님 `CombatPhaseTransitionPresenter.cs:33-37`(Q-V17 A, 공유 문서 §24), 그 밖은 우리 파일.

## Phase V2-e — W6 연결 입력 묶음·입력 표면 (W6-1·W6-2, V-M4 일부)

**Q-T6 a 허락이 있어야 시작한다.**

| Task | 파일 | 내용 |
|---|---|---|
| Ve2.1 | 새 `V/Input/CombatSceneInput.cs`(+ `.meta`) | 연결 한 번의 입력 객체 일곱을 갖는다: 대기 관문, 입력 기능 묶음, Router, 덱 배치 입구, 아이템 입구, 환경 호버, 거절 안내(TP-6). **수명은 둘로 나뉜다(H2).** `Stop()`은 안내·환경 호버·덱 배치·아이템 넷만 정리하고 비운다(지금 `StopInteraction` `:754-765`). 관문·묶음·Router는 계속 쓸 수 있게 둔다. 입력 해제(`ReleaseInput`)의 `Unbind` 도중 슬롯이 보내는 `SelectCanceled`를 Router·묶음이 받아야 하기 때문이다(`CombatInputReceiver.cs:58-69·102-117`). 관문 비우기·묶음 정지는 지금처럼 `Stop()` 안에서 한다(`:766-768`). 멤버: `Gate`·`Interaction`·`DeckPlacement`·`ItemInput`·`Notices`, `Route(request, x, y, characters)`(Router + 환경 호버의 포인터·지우기, 지금 `RouteInput` `:1165-1169`), `RouteCommand(request, characters)`(Router만, 배치 확인창 콜백용, M4), `SyncEnvironmentHover(current, seq, allowed)`, `OnResult(result)`, `CancelPresentation()`(환경 호버 지우기 + 묶음 `CancelInputs`, 지금 `CancelInputPresentation` `:1139-1144`), `Stop()`. |
| Ve2.2 | `F/CombatSceneInputFactory.cs` | `CreateSceneInput(...)`을 더한다. 지금 조정자 `CreateInput`(`:640-659`, 현강님 줄 섞임)이 하던 생성과 연결을 옮긴다. **HUD 아이템 해제 연결(`BindItemUnequip` `:655`)은 생성이 모두 끝난 뒤 조정자가 마지막에 건다(L11).** 지금은 환경 호버 생성(`:658`)보다 먼저라, 생성이 실패하면 `_input`이 null이 되어 이 연결을 풀지 못한다. 기존 `CreateInteraction`은 시험이 부르므로 남긴다. `CreateSceneInteraction`·`CreateCursorSnap`·`CreateEnvironmentHover`는 시험이 부르지 않는다(L5). 새 입구 안에서 쓰고 이름은 남긴다. |
| Ve2.3 | 새 `V/Input/ActionButtons/CombatInputSurfaces.cs`(+ `.meta`) | 지금 조정자의 `SyncActionButtons` 몸통(`:1197-1203`)·`HideActions`(`:744-749`)와 BattleChanger 입력 연결(`:527`)·해제와 Pack 숨김(`:693-694`)을 옮긴다. **연결 수명 객체다(M5).** 표시를 인수할 때(`TakeDisplay` 성공 뒤) 표시와 함께 만들고, 씬 루트는 구독할 때(`AttachCreated` `:525`) `BindRoot(root, receiver)`로 받는다. 시험 조립처럼 루트가 없으면 BattleChanger·Pack 갈래는 지금처럼 건너뛴다(`_subscribedRoot?.`). 멤버: `BindRoot`·`UnbindRoot`·`Sync(facts, canEnterAttack, isAttackActive)`·`Hide()`. 배치 확인창 맞춤(`_deploymentConfirmation?.Sync()` `:1192`, 현강님 줄)은 조정자가 그 앞에서 부른다(확인창은 서비스 수명). |
| Ve2.4 | `V/CombatViewSystem.cs` | 필드 일곱 → `_input` 하나, 표면 → `_surfaces` 하나. `CreateInput`은 `_input = CombatSceneInputFactory.CreateSceneInput(...)`(V2-g에서 주입으로 바뀜). `RouteInput`(`:1154-1173`)은 배치 확인 갈래·정보창 선택·`_input.Route`·표면 맞춤만 남는다(TP-7, 설계와 다름). 배치 확인창 콜백 셋(`:1215-1229`)은 `_input.RouteCommand`·`_input.Notices`를 쓴다. `CancelDeploymentInputs`가 `_input.Route`가 아니라 `RouteCommand`를 써서 환경 호버 지우기가 더해지지 않게 한다(M4). `StopInteraction`은 환경 호버·덱 호버 정리(덱 호버는 조정자 소유, TP-6) → `_input.Stop()`. `_input = null`은 지금 `_interaction = null`·`_gate = null`·`_router = null`의 자리(`:714-716`, `ReleaseInput` 뒤)에서 한다(H2). 덱 호버 `Release`와 환경 호버 지우기의 순서가 바뀌는 것은 서로 다른 대상이라 무해하다. `CancelInputPresentation`·`OnInputClosed`는 첫 그리기 전이나 Detach 뒤에도 불리므로 `_input?.`로 견딘다(L7, 지금 `:1141-1142`와 같음). `DeckPlacement`·`ItemInput` 서비스 멤버(`:123-126`)는 `_input`에서 읽는다(서명 그대로). |
| Ve2.5 | 시험 | 우리 `T/CombatViewSystemTests.cs:59`(`StopInput` 뒤 `View.DeckPlacement`가 null)는 Ve2.1의 `Stop()`이 덱 배치를 비우므로 그대로 성립한다. **호진님 `T/HojinCombatAttackDragIntegrationTests.cs:128`이 조정자의 `_interaction` 필드를 리플렉션으로 읽는다.** 이 Phase에서 그 필드가 없어지므로 Q-T6 a 허락이 있어야 시작한다(TP-13). A안이면 `using` 한 줄도 더해야 한다(L6). |

**동작:** 같다(같은 문장을 같은 순서로 옮김). `Detach`의 정리 순서(확인창 닫기 → 구독 해제 → 재생 중단 → 입력 정지 → 입력 해제 → 참조 비우기)는 그대로다.

**다른 사람 줄:** 현강님 조정자 줄 약 60줄(덱 입력·환경 호버·확인창·`RouteInput`·`HideActions`·`SyncEnvironmentHover`, 옮김), 호진님 `:648-649`(Router 생성, 옮김), 천명님 둘레 줄.

## Phase V2-f — W6 표시 값 읽기·튜토리얼 보고·완료 배경음 (W6-3·W6-4·W6-5)

**Q-T6 b 허락이 있어야 시작한다.** 이 Phase에 TP-10(타임라인 필수 인자)을 함께 한다(M1).

| Task | 파일 | 내용 |
|---|---|---|
| Vf2.1 | 새 `V/Display/CombatDisplayValueReader.cs`(+ `.meta`) | 지금 조정자의 `ReadDisplayValues`(`:878-887`)·`CurrentStageKey`(`:893`)·타임라인 길이 상수(`:73-75`)·재고 읽기(`ReadItemStock` `:1092`)를 옮긴다. 상태·캐릭터·재화·재고·현재 진행·타임라인 조회를 갖는다. 배경 세우기(`OnCombatSceneLoaded` `:399-417`)도 스테이지 키를 이것에서 읽고, 그 서비스 확인(`:403`)을 읽기 객체 확인으로 바꾼다(L8). |
| Vf2.2 | `V/CombatViewSystem.cs` | `BindServices`(`:218-266`)에 타임라인 조회를 필수 인자로 더한다(TP-10, 설계와 다름). `PostBindService`는 `inGame.CoreGroup.CombatTimeline`(지금 `:189`)을 그 인자로 넘긴다. 필드 `_combatQueries`(`:104`, 천명님 줄)를 지운다. 이 Phase에서는 다른 선택 인자 넷은 아직 그대로 둔다(V2-g). |
| Vf2.3 | 새 `V/Input/Assist/CombatBlockedInputReport.cs`(+ `.meta`) | 지금 `ReportBlockedInput`(`:1027-1038`, 호진님 줄)을 옮긴다. 튜토리얼 상태 읽기·요청 쓰기를 갖는다. 입력 제한 사실(`ReadFacts`의 `combatInputRestriction` `:1018`)도 이것에서 읽는다. |
| Vf2.4 | `V/Display/ICombatBattleDisplay.cs`·`V/Display/CombatSceneDisplay.cs` | `ShowCompletion(completion, Func<bool> confirm)`(설계 W6-5). 표시는 결과 화면 확인 때 `confirm()`이 참이면 완료 뒤 배경음(지금 조정자 `:955-956`)을 낸다. |
| Vf2.5 | `V/CombatViewSystem.cs` | `_currency`·`_items`·`_current`·`_tutorialState`·`_tutorialRequests`·`_audio` 필드를 위 둘과 표시로 넘긴다. 입력 Factory에 넘기던 `_audio`는 `_displayServices.Audio`로 바꾼다. 호스트 `ShowCompletion`(`:947-958`)은 `confirm` 판단만 넘긴다. |
| Vf2.6 | 시험 | 우리 `T/CombatViewSystemTests.cs`의 `FakeDisplay.ShowCompletion`(`:1600-1604`)은 `Func<bool>`를 받아 `CompletionConfirmed = () => confirm();`로 지금 필드를 채운다(그 필드를 쓰는 다섯 곳은 그대로). `BindServices` 셋에 빈 타임라인 가짜를 더한다(우리 줄 `CombatDisplayReuseTests.cs:580` 끝, `CombatMovementViewTests.cs:427` 끝, `CombatViewSystemTests.cs:1341` 끝, TP-12). **현강님·천명님 `T/FrameworkRuntimeCompositionTests.cs:228-231`(`:228-229` 현강님, `:230-231` 천명님)이 조정자의 `_combatQueries` 필드를 리플렉션으로 읽는다.** 이 Phase에서 그 필드가 없어지므로 Q-T6 b 허락이 있어야 시작한다(TP-13, M2). |

**동작:** 같다. 완료 뒤 배경음은 같은 때(확인이 참일 때) 같은 값으로 난다. 시험의 타임라인은 null → 빈 배열인데, 표시 값이 null을 빈 배열로 바꾸므로(`CombatDisplayValues.cs:23`) 결과가 같다. 실제 조회는 전투가 없으면 예외지만(`CombatTimelineQuery.cs:38-41`), 부르는 두 곳 모두 전투가 있을 때만 부른다.

**다른 사람 줄:** 현강님 `:885`(타임라인 한 줄)·`:956`(배경음 한 줄, `:955`는 우리 줄)·`:103`·`:192`(오디오 필드·받기), 천명님 `:104`(`_combatQueries` 필드)·`:882-887`(표시 값 읽기 몸통), 호진님 `:99-100`·`:188-189`·`:213-214`·`:249-250`·`:1017-1037`(튜토리얼·재고·타임라인 받기).

## Phase V2-g — W6 생성 주입·운영 입구 필수 (W6-6·W6-7, V-L10·V-M12 남은 것)

**Q-T6 c·d 허락이 있어야 시작한다.**

| Task | 파일 | 내용 |
|---|---|---|
| Vg2.1 | 새 `V/Display/ICombatSceneFactory.cs`·`V/Input/ICombatSceneInputFactory.cs`(+ `.meta`) | 생성 계약 둘(설계 W6-6). 입력 쪽은 `CreateSceneInput(...)` 하나다. |
| Vg2.2 | 새 `F/CombatSceneFactory.cs`(+ `.meta`) | 두 계약을 함께 구현하는 상태 없는 `sealed` 클래스, `Instance` 하나(P16, TP-14). 정적 Factory 둘에 위임한다. |
| Vg2.3 | `V/CombatViewSystem.cs` | 생성자에 필수 인자 둘(`ICombatSceneFactory`·`ICombatSceneInputFactory`)을 더하고, 정적 Factory 호출(`:415`·`:476`, V2-e의 `CreateSceneInput`)을 계약 호출로 바꾼다. |
| Vg2.4 | `F/InGameViewSystemFactory.cs:60`(연결 줄, 호진님 줄) | `CombatSceneFactory.Instance`를 두 번 넘긴다(설계 W6-6 diff). |
| Vg2.5 | `V/CombatViewSystem.cs` | `BindServices`의 선택 인자 넷(텍스트 표시·재고·튜토리얼 둘)을 필수로 한다. 튜토리얼 "둘 다 또는 없음" 검사(`:237-238`)와 `_textPresentation != null`(`:657`) 갈래를 지운다. 재고 `_items`의 null 갈래(`:886`·`:1092`)도 지운다(L10). |
| Vg2.6 | `V/CombatDisplayServices.cs` | 선택 인자 넷(`:35-38`)을 필수로. 요약의 "null 허용" 설명을 지운다. |
| Vg2.7 | `F/CombatSceneDisplayFactory.cs`·`F/CombatSceneInputFactory.cs`·`V/Display/CombatSceneDisplay.cs` | 화면 전환 갈래 `services.ScreenTransitions != null ? … : null`(`:147-150`, 민형님 줄)을 지운다. 입력 Factory의 알림 생략(`:64`, 현강님 줄)을 지운다. 표시 쪽 `_audio?.`(`CombatSceneDisplay.cs:203-204·274`, `:204`·`:274` 현강님 줄)도 지운다. |
| Vg2.8 | 시험 도우미(우리, TP-11) | 우리 `T/CombatViewSystemTests.cs` 안에 도우미를 모은다. **(1) 화면 전환 가짜는 `CreateScop()`이 Play 즉시 `IsCompleted = true`인 Scope를 돌려준다(H3).** null을 돌려주면 예외이고(`CombatPhaseTransitionPresenter.cs:46-47`), 끝나지 않으면 국면 사건이 끝나지 않는다(`CombatSceneEventPlayback.cs:71-76`). (2) 오디오·UI·카메라 효과는 아무것도 하지 않는 가짜(카메라는 맵 카메라가 없으면 줌·흔들림이 나지 않음, `CombatCameraWindow.cs:51·54`). (3) 텍스트 표시는 확인창을 구현하지 않는 가짜(지금 `CombatDeploymentConfirmation.cs:90-94`의 동작이 같게, L9). (4) 튜토리얼·재고는 각 조립이 가진 실제 Buffer 창구를 쓴다(기본 제한 None, `TutorialBuffer.cs:146`). (5) View DB 표는 실제 DB를 한 번 읽어 정적으로 둔다(TP-15). |
| Vg2.9 | 시험(우리 + 허락분) | `new CombatViewSystem(` 일곱 곳(`CombatViewSystemTests:183·184·206·878·1336`, `CombatDisplayReuseTests:578`, `CombatMovementViewTests:585-586`)에 `CombatSceneFactory.Instance`를 두 번 넘긴다. `CombatMovementViewTests:586`은 천명님 줄이다(Q-T6 c). `BindServices` 셋에 넷을 더한다(우리 줄 끝, TP-12). `new CombatDisplayServices(` 열둘(우리 열, 민형님 `:173`, 현강님 `:1449`)은 도우미로 넷을 채운다. **환경 호버가 이제 늘 만들어지므로, `InitData`를 부르지 않던 조립이 View DB 표를 받게 한다(H1, TP-15).** 우리 `CombatViewSystemTests` 조립(`:1336-1352`)에 `View.InitData(…)` 한 줄, 천명님 `CombatMovementViewTests.BindView`(`:420-428`)나 조립에 한 줄(Q-T6 d). |
| Vg2.10 | 확인 대상 시험(우리) | 화면 전환 Presenter가 이제 시험 조립에도 생기므로 국면 사건 완료가 다음 `TickPresentation`으로 밀린다(`OrderedCombatPresentation.cs:99`). 영향 후보 둘을 구현 때 확인한다: `CombatMortarAssetTests.cs:546-566`(Tick을 돌며 기다리므로 견딜 가능성이 높음), `CombatSceneEventPresenterTests.cs:737-758`(실제 Core로 도는 기차 시험, 같은 결과에서 국면 사건이 기차 앞에 오면 `:741` 단언이 깨질 수 있음). 깨지면 멈추고 묻는다. |

**동작:** 운영은 같다(운영 조립은 이미 모두 넘김). 시험 조립에서 꺼져 있던 기능(오디오·화면 전환·카메라 효과·튜토리얼 보고·환경 호버)이 가짜로 켜진다. 화면 전환만 사건 완료 시점에 닿는다(Vg2.10).

**남기는 null 허용(TP-17):** `CombatSceneEventPresenter` 생성자의 선택 인자(V-L8, P17), `CombatUnitInfo.cs:94·96`의 `_ui?.`(GlobalSystem UI 쪽 컴포넌트라 이번 범위 밖), 조정자 `AttachCreated`의 시험 입구 null 허용(`root`·`inspection`, Q-V2g-2 A). V2-g 검토가 더 찾은 다른 하나(`CombatSceneEventPlayback` 선택 인자)는 Q-V2g-1 A로 필수가 됐다(실행 기록 V2-g 끝).

**다른 사람 줄:** 호진님 `InGameViewSystemFactory.cs:60`(연결 줄, 민형님 공유 §24·호진님 메모 §11), 호진님 `BindServices` 튜토리얼·재고 줄, 현강님 `BindServices`의 텍스트·표시 묶음 줄·`CombatSceneInputFactory.cs:64`·`CombatSceneDisplay.cs:204·274`, 민형님 `CombatSceneDisplayFactory.cs:147-150`, 시험 허락분.

## Phase V2-h — 문서·공유 (모두 미전달로 작성, 코드 변경 없음)

| Task | 내용 |
|---|---|
| Vh2.1 | 현강님 메모 §9: 조정자 줄 옮김(V2-e: HEAD `:121·124·131·134` 필드, `:526-527` BattleChanger 연결 → 표면 `BindRoot`, `CreateInput` `:645-646·650-651·656-658` → Factory `CreateSceneInput`, `HideActions` `:744-746·749` → 표면 `Hide`, `StopInteraction` `:754-756·761-762` → 묶음 `Stop`, `:847` → `_input.OnResult`, `:1141` → `CancelPresentation`, `:1166-1169` → 묶음 `Route`(`:1165` Router 넘기기는 우리 줄), `:1178·1182` → `HasEnvironmentHover`·`SyncEnvironmentHover`(`:1181`은 조정자에 남음), 확인창 콜백 `:1217·1220 뒤·1222·1226·1228` → `RouteCommand`·`Notices`; V2-f: `:4` 쓰지 않게 된 `using …AudioSystem` 삭제·`:103` `_audio` 필드·`:192` `_audio =` 삭제·`:646` 입력 Factory 오디오 인자 → `_displayServices.Audio`·`:885` 타임라인 줄 → 표시 값 읽기·`:956` 완료 배경음 줄 → `CombatSceneDisplay.ShowCompletion`, 시험 `FrameworkRuntimeCompositionTests.cs:228-229` 리플렉션 경로(Q-T6 b)), 덱 입구 몸통(V2-b), `:1181` 인자(V2-a), 완료 배경음 위치(V2-f), `CombatSceneInputFactory.cs:64`·`CombatSceneDisplay.cs:204·274`(V2-g), 고친 시험(허락분), Q-T6 결과. V2-g(blame 대조): `CombatSceneInputFactory.cs:63-64` 알림 생략 갈래·주석 삭제, `CombatSceneDisplay.cs:204·274` `_audio?.` 삭제, `CombatViewSystem.cs:256` `_textPresentation` 필수, V2-e가 Factory로 옮긴 환경 호버 갈래(HEAD `:656-657`)를 늘 만들도록 바꿈, `CombatDisplayServices.cs:49` `UI` 필수, 시험 `CombatSceneEventPresenterTests.cs:1449`(Q-T5). |
| Vh2.2 | 호진님 메모 §11: 넘겨받기 옮김(V2-c: 묶음 `:51`·`:69`·`:152-206`·`:356-358`, 무효화 수명 다섯 `:265·393·422·436·449` 삭제, 의도 입구 넷 `:252·294·309·326` → `_handover?.Invalidate()`, 생성자 서명 `:63` 그대로), V2-f: `:99-100` 튜토리얼 필드·`:249-250` 대입 삭제, `:189` `_combatQueries =` → `BindServices` 타임라인 인자로 넘김, 클래스 설명 `:58`, `:236-238` 짝 검사 → `CombatBlockedInputReport` 생성자, `:649` 막힘 알림 인자 → `_blockedInput.Report`, `:1018` → `_blockedInput.Restriction`, `:1025-1037` `ReportBlockedInput` → 보고 객체 `Report()`(`:188`·`:213-214`·`:232-234`는 그대로), Router 생성 HEAD `:648-649` → Factory `CreateSceneInput`·`:1017` `isAttackActive`가 묶음의 입력 기능을 읽음·통합 시험 `HojinCombatAttackDragIntegrationTests.cs:128`(+ `using`, Q-T6 a)(V2-e), 연결 줄 `:60`(V2-g), 튜토리얼 보고·재고·타임라인 받기 줄(V2-f·V2-g), Q-T6 결과. V2-g: 연결 줄 `InGameViewSystemFactory.cs:60`이 `CombatSceneFactory.Instance` 둘을 넘김, `CombatViewSystem.cs:232-234` 선택 인자(재고·튜토리얼 둘) → 필수, 제안만: `PostBindService` `:213-214`의 `?? throw`가 보고 객체 생성자 검사와 겹침(지우지 않음). |
| Vh2.3 | 천명님 Core 전달 문서 §34(요청 없음): `CombatMovementInteraction.cs:391-395`·`AttackTargetingInteraction.cs:487-488`(V2-a), `CombatMovementViewTests.cs` 가짜 한 줄(V2-a), `CombatPathInput.cs:24` 클래스 선언에 `: ICombatAttackDragTarget`(V2-c), V2-f: `CombatViewSystem.cs:94` `_currency` 필드·`:104` `_combatQueries` 필드 삭제, `:244` currency 검사 꼴 바뀜, `:879·882·884·887` `ReadDisplayValues` 몸통 → 표시 값 읽기, 시험 `FrameworkRuntimeCompositionTests.cs:230`(Q-T6 b), 고친 시험(허락분). V2-g: `CombatDisplayServices.cs:36` `screenTransitions` 선택 → 필수, 시험(Q-T6 c·d) `CombatMovementViewTests.cs:586` 생성자 인자 둘, `BindView`(`:420-428`) 인자 다섯(V2-f 빈 타임라인 + V2-g 넷, 우리 줄 끝) + `InitData` 한 줄·주석 한 줄. |
| Vh2.4 | 민형님 Factory 공유 문서 §24: 연결 줄, 생성 주입 계약, `CombatDisplayServices` 필수, 화면 전환 갈래 삭제(`CombatSceneDisplayFactory.cs:147-150`), 화면 전환 사건 고르기(V2-d: `CombatPhaseTransitionPresenter.cs:4` `using …Contracts.Common` → `using …Playback.Ordering`으로 바꿈, `:33-36` 형식 표 `TransitionOf`를 읽음, `:37`은 그대로), 고친 시험 한 줄. V2-g(blame 대조): 새 `CombatSceneFactory.cs`(+ `.meta`)·계약 둘(`Display/ICombatSceneFactory`·`Input/ICombatSceneInputFactory`), V2-e의 `CreateSceneInput` 추가와 V2-g의 알림·환경 호버 갈래 삭제, `CombatDisplayServices.cs:27·35·48` 선택 → 필수·`:88` 요약 고침, `CombatSceneDisplayFactory.cs:55` 인자 설명·`:147-150` 갈래 삭제, 시험 `CombatSceneEventPresenterTests.cs:173`(Q-T5), `CombatSceneEventPlayback`의 선택 인자 셋 필수(Q-V2g-1 A, 민형님 줄 `:62·72·75·128·164·172·188`). |
| Vh2.5 | 폴더 배치 안내: 새 파일 위치, `CombatEventRoutes` → `CombatEventTypeTable`, `CombatActionBoundary` 삭제. 설계 §3~§5 구현 메모(설계와 다른 TP 아홉). CLAUDE.md. |

## Phase V2-i — 2라운드 측정 (코드 변경 없음)

- 1라운드 V-f와 같은 방법이다. B 판 = `23436b00`, A 판 = 2라운드 뒤 작업 트리(사용자가 먼저 커밋하면 그 커밋).
- 범위·평가자 셋·감점 규칙·블라인드는 1라운드 §10과 같다.
- 결과는 설계 §11(새 절)에 적는다.

## 계획에서 정한 것 (사용자 확인 대상)

- **TP-1 Phase 순서는 W5 작은 셋 → W5 계약 → 넘겨받기 → W7 → W6 셋이다.** 설계 §2의 W5 → W7 → W6을 지키되, W5 안의 서로 독립인 작은 변경(Buffer 비우기·사실·층)을 먼저 떼어 계약 변경의 확인 범위를 줄인다.
- **TP-2 소유자 목록은 `CombatInteraction` 생성자 한 곳이 만든다(설계 P9 "입력 Factory 한 곳"과 다름).** 묶음은 이름 입구(덱·아이템·공격·복귀·취소) 때문에 기능을 이름으로 받아야 한다. 순서까지 Factory가 따로 넘기면 같은 기능을 두 번(이름으로, 목록으로) 받게 된다. 생성자를 부르는 곳은 `CombatSceneInputFactory.cs:102` 한 곳뿐이라 시험 영향은 없다(검증 F9: 1판의 "시험이 생성자를 직접 부른다"는 근거는 틀렸다).
- **TP-3 포인터 판정식은 사실 하나만 받게 바꾸지 않는다(설계 W5-2 스케치와 다름).** 셋째 인자의 기본값만 지우고, 사실을 쓰는 곳은 `facts.IsItemDragActive`를 넘긴다. Router는 지금처럼 묶음의 실시간 상태를 넘긴다. 시험 여럿이 Router에 공격 모드 값이 없는 고정 사실을 준다(`CombatInteractionTests.Combat()` `:1216-1224`, `:456-466`, `CombatInputRouterTests.cs:182-191`). 판정을 사실만 보게 바꾸면 `:466`의 공격 제출이 경로로 간다(검증 확인). 새로 찾은 낮음(기본 인자에 기댐)은 이것으로도 풀린다.
- **TP-4 바뀐 동작을 보는 우리 단언은 지우지 않고 기대값만 맞춘다(Q-T4 A 글과 다름).** Q-T4 A 글은 "바뀐 동작은 지운다"다. 하지만 이번에 바뀌는 단언은 같은 규칙(층 목록에서 나오는 손잡이 수, 아군 호버가 쓰는 층)을 보는 것이라, 지우면 그 규칙을 보는 시험이 없어진다. 해당: 아웃라인 손잡이 수 넷, `FillNames` 한 줄, `CombatAttackFeedbackTests` 아군 호버 둘. 또 설계 W5-6에 빠졌던 행동 중 마스크(`FieldHighlightState.ActingLayers`)를 더한다(빼면 GridOutline에서 아군 호버 범위가 사라짐).
- **TP-5 경로 정지는 `IsDetached` 하나로 본다.** 묶음은 이름 입구용 `_path`를 지금처럼 `Stop` 앞에서 놓는다(F2). 목록의 경로 어댑터와 넘겨받기는 경로의 `IsDetached`(= `!_attached`)를 본다(F13: 같은 사실을 두 곳에 두지 않음). 정지 뒤 막히지 않던 경로 메서드(`CancelInputs`·`LeaveMoveMode`·`OnPreview`·`TryGetAttackDragOrigin`의 NRE 길, 검증 F2)에 닿는 길이 없다.
- **TP-6 연결 입력 묶음은 `CreateInput`이 만들던 일곱만 갖는다(설계 W6-1과 다름: 설계는 덱 호버도 넣음).** 덱 호버 입구는 표시를 인수할 때(`AttachCreated` `:519-520`) 만들어 첫 그리기 전에도 있으므로 조정자에 남긴다. 일곱 안에서도 수명이 둘이다(H2): 넷은 입력 정지 때 비우고, 관문·묶음·Router는 입력 해제 뒤 참조를 비운다.
- **TP-7 `RouteInput`은 나눠 옮긴다(설계 W6-1과 다름: 설계는 `Route`가 확인창 갈래도 맡음).** 배치 확인 갈래·정보창 선택·표면 맞춤은 조정자의 사실(확인창·표시·재생 중)을 써서 조정자에 남고, Router·환경 호버 부분만 묶음으로 간다. 환경 호버 허용 맞춤(`SyncEnvironmentHover`)도 환경 호버를 가진 묶음에 둔다(설계 W6-2는 입력 표면에 둠).
- **TP-8 `CombatEventRoutes.cs`는 지우지 않고 `git mv`로 이름을 바꾼다(설계 P14 "지운다"와 다름).** 기록이 이어지고 GUID가 같다. `.meta`도 함께 옮긴다. `CombatEventRoute` 열거는 그 파일에 남는다. `CombatActionBoundary.cs`는 `.meta`와 함께 지운다.
- **TP-9 처리기의 `ActorOf`를 걷으므로 `CombatEventHandlerTableTests.cs:70`의 None 처리기 행위자 단언은 지운다(설계 W7-2 "호출만 맞춤"과 다름).** 같은 시험의 나머지는 남긴다.
- **TP-10 `BindServices`에 타임라인 조회를 필수 인자로 더하고 V2-f에서 한다(설계 W6-7 서명과 다름).** 지금 시험 입구는 그것을 받지 않아 시험 조립에서 늘 null이었다. 표시 값은 null을 빈 배열로 바꾸므로 시험에는 빈 배열 가짜를 넘긴다(결과 같음). V2-f가 필드를 옮기므로 그때 함께 한다(M1).
- **TP-11 시험 가짜는 새 시험 파일을 만들지 않고 우리 `CombatViewSystemTests.cs` 안 도우미로 모은다.** `[Test]`가 아닌 도우미라 규칙 밖이다. 화면 전환 가짜는 즉시 끝나는 Scope를 돌려준다(H3).
- **TP-12 `BindServices` 인자는 그 호출의 우리 줄 끝에 붙인다.** 천명님 줄(`CombatMovementViewTests.cs:423`·`CombatViewSystemTests.cs:1338`)은 바꾸지 않는다(M3). Q-T5에서 받은 허락은 쓰지 않게 된다.
- **TP-13 다른 사람 시험 넷이 W6에서 깨진다(설계에 없던 것).** 아래 Q-T6.
- **TP-14 생성 계약 둘의 구현은 클래스 하나이고 같은 `Instance`를 두 번 넘긴다(설계 P16).**
- **TP-15 시험 조립도 View DB 표를 받는다(설계와 다름: 설계는 이 영향을 몰랐음).** 텍스트 표시가 필수가 되면 환경 호버가 늘 만들어지는데, 그 생성은 View DB 표가 필요하다(`CombatSceneInputFactory.cs:33`). 지금은 `InitData`를 부르는 조립이 `CombatDisplayReuseTests.cs:579` 하나뿐이고, 나머지는 `_textPresentation != null` 갈래(`CombatViewSystem.cs:657`) 덕에 건너뛴다(H1). 고칠 길은 셋이었다. 권장한 (a)는 시험이 실제 운영 조립을 그대로 쓰게 한다.
  - **(a, 권장) 실제 DB를 한 번 읽어 두고 조립마다 `InitData`를 부른다.** `CombatDisplayReuseTests.cs:572`의 `DataBaseFactory.Create` 선례를 쓰고, 정적으로 한 번만 읽는다.
  - (b) "표가 없으면 환경 호버 없음" 갈래를 남긴다(V-M12 낮음이 남음).
  - (c) 시험용 입력 Factory를 허용한다(P16을 풂).
- **TP-16 처리기 표의 형식 확인은 `Add` 안에 남긴다.** 표의 정적 초기화로 옮기면 실패가 `TypeInitializationException`으로 바뀌어 원인을 읽기 어렵다(L4).
- **TP-17 남기는 null 허용을 적는다(처음 둘, 2026-10-25 셋).** `CombatSceneEventPresenter` 생성자 선택 인자(P17, 3라운드), `CombatUnitInfo`의 `_ui?.`(UI 컴포넌트, 범위 밖). V-M12가 낮음으로 남을 수 있는 까닭이다. **(2026-10-25 셋째를 더함: 조정자 `AttachCreated`의 시험 입구 `root`·`inspection`, Q-V2g-2 A.)**
- **TP-18 공유 메모에 다른 사람 운영 줄을 빠짐없이 적는다.** 검증이 찾은 빠진 줄(M8)을 각 Phase "다른 사람 줄"과 V2-h에 넣었다.

## 결정 대기 — 다른 사람 시험 (설계에 없던 것)

| # | 시험 | 지금 하는 일 | 깨지는 까닭 | A(권장) | B |
|---|---|---|---|---|---|
| **Q-T6 a** | 호진님 `T/HojinCombatAttackDragIntegrationTests.cs:128` | `Read<CombatInteraction>(view, "_interaction")`로 조정자 필드를 읽어 공격 범위 응답을 기다림 | V2-e에서 그 필드가 연결 입력 묶음 안으로 감 | 리플렉션 경로만 `_input` → `Interaction`으로 맞추고 `using` 한 줄(확인 내용 그대로) | 조정자에 시험용 `Interaction` 읽기 멤버를 둠(시험 무변경, 시험 전용 멤버가 하나 늘어남) |
| **Q-T6 b** | 현강님(`:228-229`)·천명님(`:230-231`) `T/FrameworkRuntimeCompositionTests.cs:228-231` | `GetField("_combatQueries")`로 조정자가 Core 그룹의 타임라인 서비스를 쓰는지 확인 | V2-f에서 그 필드가 표시 값 읽기로 감 | 리플렉션 경로만 표시 값 읽기의 필드로 맞춤(확인 내용 그대로) | 조정자에 `_combatQueries`를 남김(쓰지 않는 필드, 불필요 낮음) |
| **Q-T6 c** | 천명님 `T/CombatMovementViewTests.cs:586` | `new CombatViewSystem(` 인자 줄 | V2-g에서 생성자 인자 둘이 더해짐 | 그 줄에 `CombatSceneFactory.Instance` 둘을 더함 | — (생성자를 바꾸면 피할 수 없음) |
| **Q-T6 d** | 천명님 `T/CombatMovementViewTests.cs:420-428`(`BindView`)·조립 | View DB 표 없이 조정자를 붙임 | V2-g에서 환경 호버가 늘 만들어져 표가 필요(TP-15) | 조립이나 `BindView`에 `InitData` 한 줄(TP-15 a) | TP-15 b·c로 바꿈 |

## 커밋 묶음 (사용자가 함)

- 새 파일은 `.meta`와 함께 넣는다. `CombatEventRoutes.cs` → `CombatEventTypeTable.cs`는 `.meta`와 함께 이름 바꾸기로 넣는다. 지운 `CombatActionBoundary.cs` + `.meta`를 함께 넣는다.
- `CombatViewBuffer.cs`와 시험 가짜 둘은 한 커밋에 넣는다(계약 멤버 추가).
- `CombatViewSystem.cs`·`InGameViewSystemFactory.cs`와 `CombatViewSystem`을 만드는 시험 일곱 곳은 한 커밋에 넣는다(생성자 인자).
- **(2026-10-25 V2-h에서 더함)** 새 파일 열하나를 `.meta`와 함께 넣는다.
  - V2-b `V/Input/Interaction/CombatInputOwner.cs`·`CombatPointerInput.cs`
  - V2-c `V/Input/Interaction/ICombatAttackDragTarget.cs`·`CombatAttackDragHandover.cs`
  - V2-e `V/Input/CombatSceneInput.cs`·`V/Input/ActionButtons/CombatInputSurfaces.cs`
  - V2-f `V/Display/CombatDisplayValueReader.cs`·`V/Input/Assist/CombatBlockedInputReport.cs`
  - V2-g `V/Display/ICombatSceneFactory.cs`·`V/Input/ICombatSceneInputFactory.cs`·`F/CombatSceneFactory.cs`
- `CombatEventTypeTable.cs`는 이름 바꾸기만 staged이고 내용은 unstaged다. 커밋 때 `git add`로 내용도 넣는다.
- 필수 인자로 묶인 것은 한 커밋에 넣는다. `CombatDisplayServices.cs`·두 Factory·`CombatSceneDisplay.cs`·`CombatSceneEventPlayback.cs`, 시험 도우미(`CombatViewSystemTests.cs`), 표시 서비스 묶음을 만드는 시험 열둘이다.
- 새 계획 문서 `docs/superpowers/plans/2026-10-24-combat-view-stage-round2-plan.md`(이 문서, untracked)도 넣는다. CLAUDE.md·설계·공유 넷·배치 안내가 이 문서를 가리킨다.
- 에디터가 바꾼 `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset`은 이번 범위가 아니다.

## 검증 반영 기록 (2026-10-24, 1판 → 2판)

하위 에이전트 둘(W5 / W7·W6)이 1판을 코드와 대조했다. 두 에이전트의 중간 이상 주장 가운데 판단이 갈리는 것(F1·F7·F8·F9, H1·M3)은 다시 코드로 확인했다(모두 맞음).

**W5(V2-a~V2-c) 검증**
- 높음 F1: 이동 범위의 번호를 소비·취소 뒤에 읽으면 0이라 슬롯이 비지 않음 → A.6을 "지우기 전에 잡음"으로 고침.
- 중간 F2: 정지 뒤 이름 입구가 놓인 경로에 닿음(NRE 길 포함) → `_path = null`을 `Stop` 앞에 남김(Vb2.8·TP-5).
- 중간 F3: 응답 사실을 감시보다 먼저 읽게 되는 diff → 지연 읽기·`CanContinue`는 응답 때만(Vb2.13·14).
- 중간 F4: "다른 소유자 쉼"의 시점·식이 없었음 → 전경 처리 뒤, 단계별 식(Vb2.2·Vb2.10).
- 중간 F5: `internal` 멤버의 암시적 인터페이스 구현 불가 → 명시적 구현(Vc2.2).
- 중간 F6: 넘겨받기의 null 검사도 바꿔야 같음 → 세 곳 모두(Vc2.3).
- 중간 F7: `CombatAttackFeedbackTests:140`은 공격 모드 단언 → 바꾸지 않음(C.5·TP-4).
- 중간 F8: 다른 사람 줄을 잘못 셈 → 천명님 줄 둘 바로잡음(A.3·A.5·Vh2.3).
- 낮음 F9(TP-2 근거 틀림)·F10(공격 진입 무효화 순서)·F11(정리 단계 미세 차이)·F12(줄 번호·주석·`_requests`·무효화 열 곳·`CombatPathInput` 줄 주인)·F13(깃발 하나로) 반영.

**W7·W6(V2-d~V2-g) 검증**
- 높음 H1: 환경 호버가 늘 만들어지면 표 없는 시험 조립이 깨짐 → TP-15·Q-T6 d.
- 높음 H2: 연결 입력 묶음 안 수명이 둘 → Ve2.1·Ve2.4·TP-6.
- 높음 H3: 화면 전환 가짜는 즉시 끝나야 함, 시험 둘 확인 대상 → Vg2.8·Vg2.10.
- 중간 M1(TP-10을 V2-f로)·M2(Q-T6 b는 두 사람)·M3(천명님 `:586`, `BindServices`는 우리 줄 끝)·M4(확인창 콜백 셋)·M5(입력 표면의 표시 받기)·M6(`CombatEventHandlerTable.cs:55`)·M7(대응표의 규칙 셋)·M8(공유에 빠진 줄)·M9(설계와 다른 점을 모두 표시) 반영.
- 낮음 L1~L11 반영(L2의 "`CombatHitGate` 규칙 1" 주석은 실제로 없어 지움).

## 실행 기록

### Phase V2-a — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

사용자 "모두 A로 하고 V2-a 진행해"(TP-1~TP-18 그대로, TP-15는 a, Q-T6 a~d 모두 A).

**바꾼 것**
- Va2-A Buffer 번호 비우기:
  - 쓰기 계약·구현에 `bool ClearPreview(int number)`를 더했다(`B/CombatView/CombatViewBuffer.cs`). 조건 없는 `ClearPreview()`에는 "경계 정리·시험용" 요약을 달았다(P13).
  - 소유자 여섯을 바꿨다: 배치(`_requests` 필드 삭제, 생성자 null 검사는 남김), 공격(호버 하나·범위/호버 둘 중 하나), 유닛 호버, 이동 견적(천명님 줄), 이동 범위(`TryShowAnswer`는 응답 번호, `Clear`는 취소 전에 잡은 번호, F1), 복귀 견적(`Reset` 전 번호).
  - 요청 객체 둘(`CombatMovementRangeRequest`·`CombatReturnRequest`)에 `Number` 읽기를 더했다.
- Va2-B 아이템 드래그 사실:
  - `CombatInputFacts.IsItemDragActive`를 더하고 조정자 `ReadFacts`가 채운다.
  - `ResolvePointerTarget` 셋째 인자의 기본값을 지웠다.
  - 경로 입력·환경 호버(현강님 줄 `CombatViewSystem.cs:1183`)는 사실 값을, Router(`:120`·`:212`)는 묶음의 실시간 값을 넘긴다(TP-3).
- Va2-C 아군 호버 층:
  - `FieldHighlightLayer.AllyHoverRange = 1 << 10`을 더했다(`DrawOrder`는 `AttackRange` 다음, 색은 공격 범위, `FillsCell`, `FieldHighlightState.ActingLayers`).
  - 아군 호버 색조가 그 층에 그린다(`CombatAttackFeedback.LayerOf`).

**시험(모두 우리 줄 + Q-T5 허락분 하나)**
- 계약 가짜 둘에 한 줄씩: 우리 `CombatInteractionTests.cs` `RejectingSubmitSink`, 천명님 `CombatMovementViewTests.cs` `CountingInput`(허락분).
- 판정식 두 인자 호출 둘에 `false`: `CombatInputRouterTests.cs:62`, `CombatItemDragInteractionTests.cs:192`.
- 기대값만 맞춤(TP-4):
  - `FieldOutlineRendererTests` 손잡이 수 넷(12→13, 24→26, 13→14, 12→13)
  - `FillNames`에 `FieldFill_AllyHoverRange`
  - `CombatAttackFeedbackTests.cs:125·128`(아군 호버 층). `:140`은 공격 모드 단언이라 그대로다.
- 단언 문구만: `CombatUnitRangeHoverTests.cs:301`.

**계획과 다른 점:** 없다.

**검토(새 에이전트, 읽기 전용):** 높음·중간 0, 낮음 10.
- 반영(주석·기록):
  - 강조 층 요약의 우선순위 설명 둘
  - 아직 없는 규칙(`BeginForeground`)을 가리키던 주석 → 지금 방식으로
  - 낡은 범위 목록 셋(`FieldOutlineRenderer.cs:543`, `FieldView.cs:33·79`)
  - `OwnsPreview` 요약
  - 판정식 주석 둘(`CombatInputFacts`·`CombatInputRouter`)
  - 번호 없는 `ClearPreview()` 요약
  - 계획의 줄 주인 표기(`AttackTargetingInteraction.cs`는 `:486`만 우리 줄)
  - CLAUDE.md·실행 기록
- 남김:
  - 배치 생성자의 안 쓰는 인자 `requests`(계획 A.2대로). 지우려면 `CombatSceneInputFactory.cs:99` 한 줄을 바꿔야 해서 V2-b에서 Factory를 고칠 때 함께 본다. V2-i에서 불필요 낮음으로 잡힐 수 있다.
  - 직접 시험이 없는 두 경로(`ClearPreview(int)`, `ActingLayers`의 새 층). 새 `[Test]`를 만들지 않는 규칙이라 Unity 재생으로 확인한다.

**Unity에서 볼 것**
- 컴파일.
- EditMode 시험: `FieldOutlineRendererTests`·`CombatAttackFeedbackTests`·`CombatMovementRangeTrackerTests`·`CombatReturnEstimateTrackerTests`·`CombatInteractionTests`·`CombatUnitRangeHoverTests`·`CombatInputRouterTests`·`CombatItemDragInteractionTests`.
- 재생: 아군 유닛 호버 범위가 공격 범위 색으로 보이는지(GridOutline에서 테두리·채우기 포함), 공격 모드에 들어가면 호버 범위가 사라지고 공격 범위가 보이는지, 이동 Drag·이동 모드 범위·복귀 견적·배치 미리보기가 전처럼 도는지.

### Phase V2-b — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

사용자 "Unity 확인 문제 없음, V2-b 진행해"(V2-a Unity 확인 문제 없음).

**바꾼 것**
- 새 파일 둘(`Input/Interaction/`, `.meta`와 함께):
  - `CombatInputOwner.cs`: 공통 수명 계약(추상 기본, 기본 구현 = 그 단계에서 할 일 없음).
    - 멤버: `Target`·`IsActive`·`IsBackground`, 포인터 셋·`SelectCanceled`, 배경 `ObservePointer`·`ObserveTick`, 감시 `Poll(latest)`, 응답 `PreviewAnswered(facts, chars)`, `BeforeResult`·`AfterResult`·`AfterAcknowledged`, `YieldToForeground`·`CancelPresentation`·`CancelInputs`(기본 = `CancelPresentation`)·`Stop`(기본 = `CancelInputs`).
  - `CombatPointerInput.cs`: 포인터 하나의 값(사실·캐릭터·x·y, `readonly struct`).
- 어댑터 다섯: 기능 파일 끝에 `private sealed class OwnerAdapter`를 두고, 창구 `internal CombatInputOwner InputOwner { get; }`는 생성자에서 한 번 만든다.

| 기능 | 대상·활성 | 맡는 단계 |
|---|---|---|
| 경로 | Path · `IsPlanning` | 포인터 셋, 감시(`OnPreview` → `Refresh`), 확인 뒤 `Refresh`, 결과 전·입력 정리·강제 종료 = `CancelInputs`, 정지 = `Detach`. 모든 단계는 `IsDetached`면 돌아간다(TP-5). 표현 정리는 없다(경로 제외). |
| 아이템 | Item · `IsDragging` | 이동·뗌, 강제 종료·표현 정리 = `Cancel`, 결과 후 |
| 배치 | Placement · `IsDragging` | 포인터 셋, 강제 종료 = `Cancel`, 응답, 결과 후, 표현 정리 |
| 공격 | Attack · `IsActive` | 이동·뗌, 응답, 결과 전·후, 표현 정리 = `Exit` |
| 호버 | 배경 | 배경 포인터·배경 감시, 응답, 내려놓기·결과 전·표현 정리 = `Suspend`, 입력 정리·정지·강제 종료 = `Clear` |

- 경로 입력에 `IsDetached`(= `!_attached`)를 더했다.
- 묶음 `CombatInteraction`:
  - 생성자가 목록(경로 → 아이템 → 배치 → 공격 → 호버, TP-2)과 대상 표를 만든다. 같은 대상이 둘이면 예외다(P10).
  - 수명 메서드 아홉(`SelectCanceled`·`BeforeResult`·`AfterResult`·`AfterAcknowledged`·`CancelPresentation`·`CancelInputs`·`Stop`·`Poll`·`PreviewAnswered`)은 목록을 돈다. `Stop`은 소유자마다 `CleanupErrors.Try`를 하고 모아 던진다(문구 그대로). `PreviewAnswered`는 쓴 소유자에서 멈춘다.
  - 포인터 셋은 대상 표로 소유자를 찾는다. 배경 소유자는 전경 처리 뒤 지금 식으로 판단한다(F4).
  - `IsInputOwnerActive`는 배경 아닌 소유자의 `IsActive`를 돈다.
  - 전경 전환 규칙 `BeginForeground` 하나를 덱 카드 집기·아이템 집기·공격 진입이 부른다.
  - 새 입구 `Tick`: 감시 단계 → 새 응답이면 그때 응답 사실을 읽어 응답 단계(F3).
  - `_path`는 이름 입구용으로 남고, `Stop`이 목록을 돌기 전에 놓는다(F2).
  - 넘겨받기 줄과 무효화 줄(호진님)은 그대로다(V2-c).
  - 이름 입구·읽기 멤버·옛 `Poll` 둘·`PreviewAnswered`의 서명은 그대로다(P8).
  - 지운 것: `_rangeHover` 필드, private `StopPlacementAndAttack`.
- 조정자 Tick:
  - 두 호출이 `_interaction.Tick(ReadFacts(), chars, latest, signal.PreviewAnswered, _readFollowUpFacts)` 하나가 됐다.
  - `CanContinue`는 지금처럼 응답이 있었을 때만 본다.
  - 대리자 `_readFollowUpFacts`는 생성자에서 한 번 만든다.

**동작 차이**(계획 "동작"에 적은 것과 같고, 넷째만 시점이 다르다)
- 순서가 바뀐 단계 둘은 결과가 같다(검토가 코드로 확인). `AfterResult`는 아이템 → 배치 → 공격이고, 두 Drag는 함께 켜지지 않는다. `PreviewAnswered`는 배치 → 공격 → 호버이고, 번호가 하나뿐이며 각 기능은 부작용 없이 거짓을 돌려준다.
- 덱 카드·아이템을 집을 때 쉬고 있는 공격의 세대 번호가 오른다(보이지 않음).
- `Stop`은 소유자마다 시도한다(F11).
- `CancelInputs`의 몸짓 무효화가 두 번에서 맨 앞 한 번으로 줄었다. 옛 코드는 안에서 `CancelPresentation()`을 불러 경로 취소 뒤에 한 번 더 무효화했다.
  - 계획은 이것을 V2-c의 차이로 적었지만 V2-b에서 생긴다.
  - 다른 결과가 나는 것은 경로 취소 콜백 안에서 새로 누른 경우뿐이다.

**시험:** 바꾼 것 없음(Vb2.15). 순서·무효화 횟수·정지 예외 수를 단언하는 시험은 없다(검토 확인).

**계획과 다른 점(셋, 사용자 확인 대상)**
1. `CombatInputTick`(+ `.meta`)을 만들지 않았다(Vb2.1·Vb2.2). 사실 없이 부르는 1인자 `Poll(preview)`(P8, 시험이 부름)가 남아서, 한 Tick 값을 쓰려면 가짜 사실을 넣어야 하기 때문이다. 대신 단계를 셋으로 나눴다: 전경 감시 `Poll(latest)`, 배경 감시 `ObserveTick(facts, chars, canShow)`, 응답 `PreviewAnswered(facts, chars)`.
2. `CombatPointerInput`에 대상이 없다(Vb2.2). 대상으로 소유자를 고르는 일은 묶음이 하고, 배경 허용 값은 묶음이 계산해 넘긴다.
3. 창구 이름 `Owner` → `InputOwner`, 어댑터 이름 `InputOwner` → `OwnerAdapter`(Vb2.3~Vb2.7).
   - 검토 높음 H1이 찾았다. 같은 클래스의 열거형 이름 `Owner.Player`·`Owner.Enemy`가 속성으로 읽혀 컴파일 오류가 셋 난다(`PlacementDragInteraction.cs:110`(다른 사람 줄)·`CombatUnitRangeHover.cs:190`·`CombatPathInput.cs:222`).
   - 속성 형식이 `CombatInputOwner`라서 C#의 같은 이름 예외도 걸리지 않는다.
- 1·2는 구현 전에 물었어야 했는데 구현 뒤에 보고한다. 계획대로 되돌리라고 하면 바꾼다. 설계 §3 W5-1에 구현 메모를 남겼다.
- **결정(2026-10-24 사용자 "Unity 확인 문제 없음, 다른 점 셋 그대로 두고 V2-c 진행해"):** 셋 모두 그대로 둔다. V2-b Unity 확인 문제 없음.

**검토(새 에이전트, 읽기 전용):** 높음 1·중간 1·낮음 4.
- 반영:
  - H1 이름 바꾸기
  - M1 설계 구현 메모·P11·P15 표기·이 기록
  - 낮음: `CombatPathInput.Refresh` 요약의 cref를 `CombatInteraction.Tick`으로, 공격 어댑터 주석 "(지금 묶음과 같음)" → "(V2-b 전 묶음과 같음)"
- 남김:
  - 호버 기능 인자 이름 `ownersIdle`. 넘어오는 값은 대상 검사를 포함한 허용 값이다. 우리 시험 `CombatUnitRangeHoverTests.cs:280`이 이름 있는 인자로 불러서, 바꾸면 시험을 고쳐야 한다.
  - 창구 속성 자리가 파일마다 다름(사소).
  - 배치 생성자의 안 쓰는 인자 `requests`. V2-b는 Factory를 고치지 않아 그대로이고, V2-g 생성 주입 때 본다.

**다른 사람 줄:**
- 묶음의 호진님 줄(넘겨받기 `TryHandleAttackDragPointer`, 무효화 줄, 생성자 서명)은 글자 그대로 남겼다.
- 묶음의 현강님 줄(`CanSelectDeckUnit`, `SelectDeckUnit`의 `_placement.SelectDeckUnit` 줄, `CancelDeckSelection`, `Cancel`, `CancelDragOrAttack`의 `_attack.Cancel`)도 그대로다. `SelectDeckUnit` 몸통에서 바뀐 두 줄은 우리 줄이다.
- 기능 파일에는 어댑터·창구만 더했다(`PlacementDragInteraction`은 현강님 파일). `CombatPathInput`에는 `IsDetached` 한 줄을 더했다.
- V2-h 공유 메모에 적을 것:
  - 현강님: 배치 파일 어댑터, 덱 입구가 `BeginForeground`를 부름
  - 호진님: `CancelInputs`의 무효화 횟수

**Unity에서 볼 것**
- 컴파일. 특히 열거형 `Owner`를 쓰는 세 줄이 이름 바꾸기로 풀렸는지 본다.
- EditMode 시험: `CombatInteractionTests`·`HojinCombatAttackDragTests`·`HojinCombatAttackDragIntegrationTests`·`CombatUnitRangeHoverTests`·`CombatItemDragInteractionTests`·`CombatInputRouterTests`·`CombatCursorSnapTests`·`AttackTrajectoryInteractionTests`·`WallAttackInteractionTests`·`CombatCommandGateTests`·`CombatPathInputCleanupTests`·`CombatViewSystemTests`·`CombatMovementViewTests`.
- 재생:
  - 배치: 덱 카드·필드 유닛 Drag, 우클릭 회수, 배치 미리보기 색
  - 아이템 Drag 장착
  - 공격 모드 진입·조준·취소
  - 이동 경로·이동 모드 범위·복귀
  - 유닛 호버 범위: 공격 모드 진입·Drag 중에는 사라지고 재생 뒤에는 다시 보이는지
  - 공격 Drag 넘겨받기: 공격 모드에서 아군을 끌면 이동이 되는지

### Phase V2-c — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

사용자 "Unity 확인 문제 없음, 다른 점 셋 그대로 두고 V2-c 진행해"(V2-b Unity 확인 문제 없음).

**바꾼 것**
- 새 파일 둘(`Input/Interaction/`, `.meta`와 함께):
  - `ICombatAttackDragTarget.cs`: 넘겨받는 쪽의 좁은 계약. `IsDetached`, 출발점 셋(`TryGetAttackDragOrigin`·`IsAttackDragOriginCurrent`·`TryBeginTransferredDrag`), `PointerMoved`·`SelectReleased`·`CancelInputs`.
  - `CombatAttackDragHandover.cs`: 넘겨받기 소유자.
    - 몸짓(호진님 `CombatAttackDragGesture`, 그대로)·받는 쪽(없을 수 있음)·공격 기능을 갖는다.
    - `TryHandlePointer`는 옛 묶음 `TryHandleAttackDragPointer` 본문을 옮긴 것이다. 경로 null·정지 검사 여섯 곳(`path != null`·`path == null`·`ReferenceEquals(path, _path)`)은 `IsTargetAttached`(= 받는 쪽 있음 && 끊기지 않음) 하나로 바꿨다(F6).
    - 낡은 후보 검사는 `InvalidateStaleCandidate` 하나다. 포인터 처리와 매 Tick 감시가 같이 쓴다.
    - 어댑터: 대상 None, 감시 = 낡은 후보 검사, 결과 전·표현 정리·강제 종료 = `Invalidate`, 입력 정리·정지는 기본(= 표현 정리).
- 경로 입력이 `ICombatAttackDragTarget`을 명시적 구현으로 단다(F5). 출발점 셋(호진님 줄)은 그대로다.
- 묶음:
  - `_attackDrag` 필드가 `_handover`가 됐다. 생성자가 몸짓이 있으면 소유자를 만들어 목록 맨 앞에 둔다(서명 그대로).
  - `TryHandleAttackDragPointer`는 이름을 남기고 위임한다.
  - 수명 단계의 무효화 다섯(`SelectCanceled`·`BeforeResult`·`CancelPresentation`·`CancelInputs`·`Stop`)과 `Poll`의 후보 블록을 걷었다.
  - 의도 입구 넷(`RequestReturn`·`CancelDragOrAttack`·`LeaveMoveMode`·`AttackModeRequested`)은 `_handover?.Invalidate()` 한 줄씩이 남는다(F10). 공격 진입은 지금처럼 `CanEnter` 앞이다.

**동작 차이**
- 몸짓 무효화가 새로 불리는 곳: 덱 카드 집기·아이템 집기(`BeginForeground`), 공격 진입(진입 판단 앞 한 번 + `BeginForeground` 한 번).
  - 보이는 차이는 없다. 후보는 전투 국면 PlayerInput의 공격 대상에서만 생긴다. 덱·아이템 집기는 배치 국면 정책을 거친다. 그래서 그때 몸짓은 Idle이거나 이미 Suppress이고, Idle이면 세대 번호만 오른다(검토 확인). 계획 V2-b "동작"의 "세대 번호만"은 이 전제를 깔고 있다.
- `CancelInputs`·`Stop`의 무효화가 HEAD의 두 번에서 맨 앞 한 번이 됐다. 이 차이는 V2-b에서 생겼다(V2-b 기록).
  - `Stop`의 두 번째 호출 때는 경로가 이미 끊겨 후보가 생길 수 없으므로 결과가 같다.
  - `_path = null`과 무효화의 순서가 바뀐 것도 그 사이에 콜백이 없어 같다.
- 경로를 묶음 밖에서 직접 `Detach`한 뒤 UI 위에서 떼면, HEAD는 끊긴 경로에 `CancelInputs`를 불렀고 지금은 부르지 않는다. 그런 호출은 운영에 없다(운영의 `Detach`는 묶음 어댑터뿐이고, `CombatPathInputCleanupTests`는 묶음·몸짓을 쓰지 않음).

**시험:** 바꾼 것 없음(Vc2.5). 호진님 `HojinCombatAttackDragTests`의 사례를 검토가 하나씩 대조했고 모두 결과가 같다: 정지·취소·재진입·새 몸짓·예외, 경로 시작 콜백, 새 경로, 후보 정체성 바뀜, 매핑·초안 콜백, 소유한 뗌, 기능 없음. 통합 시험 `:128`은 조정자의 `_interaction`만 리플렉션으로 읽는다(V2-e, Q-T6 a).

**계획과 다른 점:** 하나(좋은 쪽). 계획은 후보 검사를 포인터 처리와 감시에 각각 옮긴다고 적었다. 구현은 `InvalidateStaleCandidate` 하나로 합쳐 두 벌이 생기지 않았다.

**검토(새 에이전트, 읽기 전용):** 높음·중간 0, 낮음 9.
- 반영:
  - 실행 기록(무효화가 늘어난 곳과 그 전제, `Stop` 무효화 횟수, 다른 점)
  - 설계 W5-5 구현 메모(의도 입구 넷, `IsDetached`)
  - `InvalidateStaleCandidate` 요약 고침(후보가 아니면 출발점 값은 의미 없음)
  - `CombatInputOwner` 분류에 셋째 꼴(넘겨받기), 넘겨받기 클래스 설명에 전경 전환
  - `AttackModeRequested` 요약("들어갈 수 없으면 넘겨받기 몸짓만 끝냄")과 주석의 "지금" → "옛 묶음"
  - `CombatPathInput` 명시적 구현 주석(public 둘도 같은 꼴로 넘기는 이유)
- 공유 목록에 더함(V2-h): 천명님 줄 `CombatPathInput.cs:24` 클래스 선언에 `: ICombatAttackDragTarget`(Vh2.3, 천명님 문서 §34).

**다른 사람 줄:**
- 호진님: 묶음 `:51`·`:63`·`:69`·`:152-206`·`:356-358`과 무효화 줄. 수명 다섯·`Poll` 블록은 넘겨받기 소유자로 옮겼고, 의도 입구 넷은 `_handover?.Invalidate()`로 남겼다. 호진님 `CombatPathInput.cs:109-150`은 그대로다.
- 천명님: `CombatPathInput.cs:24`(클래스 선언에 인터페이스).

**새 파일(커밋 때 `.meta`와 함께):** `Input/Interaction/ICombatAttackDragTarget.cs`, `Input/Interaction/CombatAttackDragHandover.cs`.

**Unity에서 볼 것**
- 컴파일.
- EditMode 시험: `HojinCombatAttackDragTests`·`HojinCombatAttackDragIntegrationTests`·`CombatInteractionTests`·`CombatInputRouterTests`·`CombatPathInputCleanupTests`·`CombatUnitRangeHoverTests`·`CombatViewSystemTests`·`CombatMovementViewTests`·`AttackTrajectoryInteractionTests`.
- 재생:
  - 공격 모드에서 아군을 누른 채 끌면 이동 Drag로 넘어가는지
  - UI 위에서 떼면 취소되는지
  - 같은 칸에서 떼면 공격 클릭으로 가는지
  - 결과 재생·턴 종료·복귀 버튼 뒤 남은 몸짓이 없는지

### Phase V2-d — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

사용자 "Unity 확인 문제 없음, V2-d 진행해"(V2-c Unity 확인 문제 없음).

**바꾼 것**
- 형식 표:
  - `Playback/Ordering/CombatEventRoutes.cs`(+ `.meta`)를 `git mv`로 `CombatEventTypeTable.cs`(+ `.meta`, GUID `2c1774c7…` 유지)로 바꾸고 형식 표로 넓혔다(TP-8). 이름 바꾸기만 staged이고 내용은 unstaged다(커밋 때 `git add`).
  - 같은 파일에 경로 열거 `CombatEventRoute`(그대로)와 열 열거 셋(`CombatEventHandlerSlot`·`CombatEventPacing`·`CombatPhaseTransitionKind`)을 둔다.
  - 줄 20개(옛 사건 틀 형식 목록과 같은 순서). 줄은 생성자로 한 번에 만들고 바꾸지 않는다. 함수 칸은 `Wrap`으로 형식별 함수를 감싼다.
  - 질문 열셋: `RouteOf`·`EventTypes`·`IsEventType`·`IsActionStart`·`ActorOf`·`TryGetSubject`·`TryGetOrigins`·`TryGetAttackSource`·`IsMovement`·`PacingOf`·`HoldsWhenShownOnly`·`TransitionOf`·`HandlerSlotOf`.
  - 규칙 M7 셋을 지킨다:
    - 사건 질문은 표를 찾기 전에 null을 거른다.
    - 주체가 Empty면 false다.
    - 근원이 null이거나 비면 false다.
  - `HandlerSlotOf`만 표에 없는 형식에 예외다.
  - 표를 만들 때 sealed·중복을 검사한다.
- `Playback/Ordering/CombatActionBoundary.cs`(+ `.meta`)를 지웠다. 부르는 9곳은 `IsActionStart`로 바꿨고, 가리키던 주석 넷은 cref를 고쳤다.
- `CombatEventSubjects`: `TryGetSubject`·`TryGetOrigins`를 지웠다. 근원 열쇠·합치기만 남고, 요약을 고쳤다(쓰지 않게 된 `using` 하나 삭제).
- `CombatHitFollowUps`: `IsMovement`·`TryGetSource`를 지웠다. 그 자리는 `IsMovement`·`TryGetAttackSource`가 맡고, 요약은 형식 표를 가리킨다.
- `CombatEnemyPresentationPacing`: `BeforeEvent`는 `PacingOf` switch(턴 안의 갈래는 `Kind` 그대로), `HoldAfter`는 `HoldsWhenShownOnly`다. 서명은 그대로다.
- 처리기:
  - 바탕·넷의 `ActorOf`를 지웠다(쓰지 않게 된 `using` 둘 삭제). 사건 Presenter는 `CombatEventTypeTable.ActorOf`를 부른다.
  - 처리기 표 `Create`는 `HandlerSlotOf` switch로 고른다(인자 그대로). 맞추지 않은 칸은 예외다(검토 낮음 2).
  - `Add`의 기존 확인 둘은 남겼다(이름과 문구만 "형식 표").
- 부르는 곳의 이름을 바꿨다: 재생 조정·순차 재생 셋·동시 묶음·걸음 재생·주체 막기·공격 Gate·걸음 접촉 Gate·걸음 접촉(쓰지 않게 된 `using` 하나 삭제).
- 민형님 `CombatPhaseTransitionPresenter.cs`(Q-V17 A): `:33-36`이 `TransitionOf`를 읽는다. `:4`의 `using …Contracts.Common`(더 쓰지 않음)은 `using …Playback.Ordering`으로 바꿨다.

**동작:** 같다. 검토가 HEAD 원본과 줄 20개의 값을 하나씩 대조했다. 간격 갈래(실패한 공격 → 0)와 화면 전환(결과의 `Outcome`은 struct)도 같다.

**시험(모두 우리 줄)**
- 호출만 바꿨다:
  - `CombatActionBoundaryTests` 일곱(`IsActionStart`, 파일 이름 그대로)
  - `CombatEventRoutesTests` 열여섯
  - `CombatSceneEventPresenterTests` 넷
  - `CombatEventHandlerTableTests` `EventTypes`와 요약 cref
  - `CombatHitFollowUpsTests` 둘(+ `using` 하나)
- 지웠다(TP-9): `CombatEventHandlerTableTests`의 `none.ActorOf(attack)` 단언 하나.
- 그대로: `CombatEnemyPresentationPacingTests`·`CombatPresentationSettingsDataTests`·민형님 `CombatPhaseTransitionPresenterTests`.

**계획과 다른 점:** 하나. 계획 Vd2.10은 "`using`이 필요하면 하나 더한다"였는데, 실제로는 민형님 줄 `:4`의 `using`을 바꿨다(옛 using을 더 쓰지 않아서). Vh2.4 공유 표에 적었다.

**검토(새 에이전트, 읽기 전용):** 높음 0·중간 1·낮음 7.
- 반영:
  - 중간 1(민형님 `:4` 바꿈을 기록·공유 표에)
  - 낮음 1(표 설명의 "모든 질문" → 사건 질문, `HandlerSlotOf`만 예외)
  - 낮음 2(처리기 칸 갈래를 빠뜨리면 예외)
  - 낮음 3(`IsActionStart`·`IsMovement` 요약에 걸음 접촉 Gate)
  - 낮음 4 일부(`CombatHitFollowUps` remarks가 근원 목록을 다시 나열하지 않고 `TryGetOrigins`를 가리킴)
- 남김(보고만):
  - `CombatEventSubjects` 이름(근원 열쇠만 남음, 계획대로 이름 그대로)
  - 표 밖 형식 지식. `CombatEventSounds.cs:109-114`(턴 진입·밤 끝·기차 배경음)와 `CombatTurnEventHandler`의 턴 행위자 읽기가 남아 있다. V2-i에서 낮음으로 잡힐 수 있다.
  - 시험 주석의 낡은 표현. `CombatEventRoutesTests.cs:16`("사건 경로 표… 세 곳"), `OrderedCombatSchedulingTests.cs:215`, `CombatActionBoundaryTests.cs:10`("네 곳"), 천명님 파일 `FrenzyViewContractTests.cs:21`. 시험은 깨지는 곳만 맞추는 규칙이라 두었다.
  - `ActorOf`를 직접 보는 순수 시험이 없어진 것(TP-9로 받아들인 결정).

**다른 사람 줄:** 민형님 `CombatPhaseTransitionPresenter.cs:4·:33-36`(Vh2.4). 그 밖에 바꾼 HEAD 줄은 모두 우리 줄이다(검토 확인).

**파일:**
- 이름 바꾸기: `Playback/Ordering/CombatEventRoutes.cs` → `CombatEventTypeTable.cs`(+ `.meta`, staged).
- 지움: `Playback/Ordering/CombatActionBoundary.cs` + `.meta`(unstaged).

**Unity에서 볼 것**
- 컴파일.
- EditMode 시험:
  - 형식 표·처리기: `CombatActionBoundaryTests`·`CombatEventRoutesTests`·`CombatEventHandlerTableTests`·`CombatHitFollowUpsTests`·`CombatSceneEventPresenterTests`
  - 묶음·재생 순서·간격·화면 전환: `CombatConcurrentGroupsTests`·`OrderedCombatSchedulingTests`·`CombatEnemyPresentationPacingTests`·`CombatPhaseTransitionPresenterTests`
  - 그 밖: `FrenzyViewContractTests`·`CombatHitGateTests`·`CombatStepContactsTests`·`CombatStepContactGateTests`·`CombatMovementViewTests`·`CombatEnemyPresentationTests`
- 재생:
  - 적 턴 간격·같은 적 연속 행동 간격
  - 포효·소환 강조와 머묾
  - 여러 대상 피격·넉백 동시 재생
  - 기차
  - 전투 진입·밤 끝·승리 때의 화면 전환

### Phase V2-e — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

사용자 "Unity 확인 문제 없음, V2-e 진행해"(V2-d Unity 확인 문제 없음). Q-T6 a는 앞서 허락됨(A).

**바꾼 것**
- 새 파일 둘(`.meta`와 함께):
  - `Input/CombatSceneInput.cs`: 연결 입력 묶음. 객체 일곱을 갖는다: 대기 관문·입력 기능 묶음·Router·덱 배치 입구·아이템 입구·환경 호버·거절 안내.
    - 멤버: `Gate`·`Interaction`·`DeckPlacement`·`ItemInput`·`Notices`, `Route`(Router + 환경 호버 지우기·포인터)·`RouteCommand`(Router만)·`SyncEnvironmentHover`·`HasEnvironmentHover`·`OnResult`·`CancelPresentation`·`Stop(ref errors)`.
    - `Stop`은 안내·환경 호버·덱 배치·아이템 넷을 비운 뒤 관문 비우기·묶음 정지를 한다. 관문·묶음·Router는 남긴다(H2).
  - `Input/ActionButtons/CombatInputSurfaces.cs`: 입력 표면.
    - 멤버: `BindRoot`(BattleChanger 입력 연결)·`UnbindRoot`(입력 연결 끊기·가방 숨김)·`Sync`(막힘 판단 하나로 BattleChanger·가방·행동 버튼)·`Hide`.
    - 표시와 함께 만들고 함께 놓는다(M5).
- 입력 Factory `CreateSceneInput`: 옛 조정자 `CreateInput`의 생성을 같은 순서로 옮겼다(관문 → 입력 기능 묶음·안내 → Router → 덱 배치 → 아이템 → 환경 호버). 클래스 설명과 `using` 순서도 고쳤다.
- 조정자:
  - 필드: 일곱 → `_input`, 표면 → `_surfaces`. 덱 호버는 조정자에 남긴다(TP-6). 서비스 멤버 `DeckPlacement`·`ItemInput`은 `_input`에서 읽는다(서명 그대로).
  - `CreateInput`은 `_input = CreateSceneInput(...)` 뒤 HUD 아이템 해제를 마지막에 건다(L11).
  - `TakeDisplay`는 표시 저장 바로 앞에서 `_surfaces`를 만든다(생성자는 예외가 없어 "표시 저장이 마지막 실패 지점" 규칙 그대로).
  - `AttachCreated`는 `_surfaces.BindRoot`를 부른다.
  - `Detach`는 `_surfaces?.UnbindRoot()`를 구독 해제 앞에서 부르고, 표시와 함께 `_surfaces`를 비우며, 입력 해제 뒤 `_input`을 비운다(H2).
  - `StopInteraction`은 덱 호버 해제 → HUD 아이템 해제(조정자가 걸었으니 조정자가 품) → `_input.Stop(ref errors)` 순서다.
  - `StopInput`·`OnInputClosed`는 `_surfaces.Hide`를 부른다. `HideActions`는 지웠다.
  - `RouteInput`에는 배치 확인 갈래·정보창 선택·`_input.Route`·환경 호버 허용 맞춤·표면 맞춤이 남는다(TP-7).
  - 배치 확인창 콜백은 `RouteCommand`(M4)·`Notices`를 쓴다.
  - `SyncActionButtons`는 확인창 맞춤 뒤 `_surfaces.Sync`를 부른다.
  - 고친 주석: 클래스 설명, `IsInputReady`, `_subscribedRoot`(구독 전용).
- 낡은 주석 셋을 고쳤다: `CombatCommandGate`(만드는 곳), `CombatInteraction`(Gate 만드는 곳·정지 때 대기 지우는 곳), `ICombatActionDisplay`(버튼 연결은 조정자, 맞춤은 표면).

**동작:** 같다. 옮긴 문장의 순서는 검토가 HEAD와 대조했다. 순서가 바뀐 곳은 셋이고 모두 대상이 달라 해가 없다:
- 정지 때 덱 호버 해제가 환경 호버 지우기보다 먼저다(계획에 적은 것).
- HUD 아이템 해제가 환경 호버 지우기·덱 배치 해제보다 먼저다.
- `SyncActionButtons`가 공격 진입 가능·공격 모드 값을 BattleChanger·가방 갱신보다 먼저 읽는다. 가방을 닫으면 아이템 Drag만 취소되고 두 값은 그것을 읽지 않는다.

**시험:** 호진님 `HojinCombatAttackDragIntegrationTests.cs:128`의 리플렉션 경로만 `_input` → `Interaction`으로 맞추고 `using` 한 줄을 더했다(Q-T6 a, 확인 내용 그대로). 우리 시험 `CombatViewSystemTests.cs:59`는 그대로 성립한다. 그 밖에 바꾼 시험은 없다.

**계획과 다른 점(넷, 모두 세부)**
1. `Stop()` → `Stop(ref List<Exception> errors)`. HEAD처럼 실패를 조정자의 같은 목록에 모으기 위해서다.
2. `HasEnvironmentHover`를 더했다. 환경 호버가 없을 때 사실을 읽지 않는 HEAD의 조기 반환을 지키기 위해서다.
3. HUD 아이템 해제도 조정자에 남겼다. 거는 쪽(조정자, L11)이 푼다. 그래서 위 순서 바뀜 둘째가 생겼다.
4. `_surfaces`를 `TakeDisplay` 성공 뒤가 아니라 그 안의 표시 저장 바로 앞에서 만든다. 표시와 늘 함께 서고 함께 비게 하려는 것이다.

**검토(새 에이전트, 읽기 전용):** 높음·중간 0, 낮음 9.
- 반영:
  - 다른 점·순서 바뀜을 이 기록에 적었다(낮음 1·2·7).
  - `_subscribedRoot` 주석을 고치고, `UnbindRoot`를 구독 검사 밖으로 꺼냈다(낮음 3, 동작 같음).
  - `ICombatActionDisplay` 주석(낮음 4 일부)과 낡은 소유자 주석 넷(낮음 5)을 고쳤다.
  - Factory 클래스 요약과 `using` 순서를 고쳤다(낮음 6).
  - 공유 표 Vh2.1·Vh2.2에 다른 사람 줄을 더했다(낮음 9).
- 남김:
  - `ContinueAttach`의 `display.SetActionsAvailable(default)`(낮음 4). 행동 버튼을 숨긴 채 연결하는 줄이고, 그때는 루트가 없어 표면 `Hide`와 결과가 같다. 계획 범위 밖이라 두었다.
  - 묶음 생성 중 예외가 나면 이미 만든 객체가 정지되지 않는다(낮음 7). 생성자들이 바깥 연결을 하지 않아 GC 대상만 남는다. L11이 노린 HUD 연결은 남지 않는다.
  - 새 `.meta` 끝 줄바꿈 없음(낮음 8). V2-b·V2-c와 같은 꼴이고 Unity가 받아들인다.

**다른 사람 줄:**
- 현강님(Vh2.1에 줄 번호):
  - 조정자의 필드·BattleChanger 연결·`CreateInput`·`HideActions`·`StopInteraction`·거절 안내·입력 정리·`RouteInput`의 Router·환경 호버 부분·환경 호버 맞춤·확인창 콜백 줄을 묶음·표면·Factory로 옮겼다.
  - 현강님 `CreateEnvironmentHover`는 그대로다.
- 호진님(Vh2.2): Router 생성 `:648-649` → Factory, `:1017`, 통합 시험 한 줄.
- 천명님: 바뀐 줄 없음(검토 확인, 계획의 "천명님 둘레 줄"은 해당 없음).

**새 파일(커밋 때 `.meta`와 함께):** `Input/CombatSceneInput.cs`, `Input/ActionButtons/CombatInputSurfaces.cs`.

**Unity에서 볼 것**
- 컴파일.
- EditMode 시험:
  - `CombatViewSystemTests` 전체(덱 배치 입구 수명·아이템 해제 연결·정지 부분 실패·AttachCreated 계열)
  - `CombatDisplayReuseTests`·`CombatMovementViewTests`·`CoreStageCombatCycleTests`·`HojinCombatAttackDragIntegrationTests`·`TutorialInputTests`·`CombatInputReceiverTests`·`CombatInputRouterTests`·`CombatDeploymentConfirmationTests`
- 재생:
  - 배치 확인창(진입·취소·메인 미배치 안내)
  - 환경 호버 설명
  - 행동 버튼·BattleChanger·가방 보임과 막힘
  - 포기 확인 뒤 입력 닫힘
  - 결과 재생 중·종료 뒤 버튼 숨김
  - 덱 카드·아이템 입구

### Phase V2-f — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

사용자 "Unity 확인 문제 없음, V2-f 진행해"(V2-e Unity 확인 문제 없음). Q-T6 b는 앞서 허락됨(A).

**바꾼 것**
- 새 파일 둘(`.meta`와 함께):
  - `Display/CombatDisplayValueReader.cs`: 표시 값 읽기.
    - 갖는 것: 상태·캐릭터·재화·현재 진행·타임라인 조회(필수)·재고(선택).
    - 멤버: `Read()`(옛 `ReadDisplayValues`), `CurrentStageKey`, `ItemStock(int)`(옛 `ReadItemStock`).
    - 타임라인 길이 상수도 여기로 옮겼다.
  - `Input/Assist/CombatBlockedInputReport.cs`: 튜토리얼 막힘 보고.
    - 갖는 것: 튜토리얼 상태 읽기·요청 쓰기 둘.
    - 멤버: `Restriction`(입력 제한 사실), `Report()`(옛 `ReportBlockedInput`).
    - 짝 검사(둘 중 하나만 오면 `ArgumentException`, 인자 이름 `tutorialRequests`)를 생성자로 옮겼다.
- 표시 계약·구현 `ShowCompletion(completion, Func<bool> confirm)`: 확인이 참이면 표시가 완료 뒤 배경음을 낸다(옛 조정자 두 줄, W6-5). `confirm`이 null이면 인자 예외다.
- 조정자:
  - 필드 일곱(`_currency`·`_items`·`_current`·`_tutorialState`·`_tutorialRequests`·`_audio`·`_combatQueries`)과 타임라인 상수, `ReadDisplayValues`·`CurrentStageKey`·`ReportBlockedInput`·`ReadItemStock`을 지웠다. 대신 `_displayValues`·`_blockedInput`이다.
  - `BindServices`는 `displayServices` 바로 뒤에 타임라인 조회를 필수로 받는다(TP-10). `PostBindService`가 `inGame.CoreGroup.CombatTimeline`을 넘기고, 더는 `_audio`·`_combatQueries`를 직접 두지 않는다.
  - 입력 Factory에는 `_displayServices.Audio`·`_blockedInput.Report`·`_displayValues.ItemStock`을 넘긴다(Vf2.5).
  - 배경 세우기의 서비스 확인은 `_displayValues` 확인이고, 스테이지 키도 그것에서 읽는다(L8).
  - 완료 호스트는 `() => CanContinue() && 같은 표시 && confirm()`만 넘긴다.
  - `ReadFacts`의 입력 제한은 `_blockedInput.Restriction`이다.
  - 쓰지 않게 된 `using` 둘(`GlobalSystem.AudioSystem`(현강님 줄)·`Playback.Support`)을 지웠다. 클래스 설명을 고쳤다.
- 낡은 주석을 고쳤다(검토 낮음 1·2·3):
  - 조정자 설명 둘, `_displayServices` 주석
  - `CombatDisplayValues`, `CombatEventSounds`, `CombatViewProgress` 호스트 계약, 입력 Factory 설명·인자 문서

**동작:** 같다.
- 타임라인: 시험은 예전에 null(→ 빈 배열)이었고 지금은 빈 가짜다. 운영은 같은 서비스 객체다(검토 확인: `GetService`가 없으면 예외라 null이 될 수 없음).
- 오디오: 운영은 같은 `global.AudioServiceCommand`이고, 시험의 조정자 조립 셋은 오디오 없는 묶음이라 예전과 같이 null이다.
- 완료 배경음: 검사 순서가 같다(연결 → 같은 표시 → 확인 → 배경음).
- `BindServices` 예외 순서도 같다(짝 → state·results·characters·current·currency → 새 timeline → 나머지).

**시험**
- 우리:
  - `CombatViewSystemTests.FakeDisplay.ShowCompletion`이 `Func<bool>`를 받아 `CompletionConfirmed = () => confirm();`로 채운다(쓰는 세 곳 그대로).
  - 빈 타임라인 가짜 `EmptyTimeline`을 더했다(TP-11).
  - `BindServices` 셋에 빈 타임라인을 우리 줄 끝에 붙였다: `CombatViewSystemTests`·`CombatDisplayReuseTests`·`CombatMovementViewTests.BindView`. 천명님 `:423`·`:1338`은 그대로다(TP-12).
- 허락분(Q-T6 b): `FrameworkRuntimeCompositionTests.cs:228-230`(현강님 `:228-229`, 천명님 `:230`). 리플렉션 경로만 조정자 `_displayValues` → 읽기 `_timeline`으로 맞췄다. 같은 객체 비교·같은 문구다.

**계획과 다른 점(둘, 세부)**
1. 튜토리얼 짝 검사를 보고 객체 생성자로 옮겼다(예외 형식·인자 이름·시점 같음).
2. 표시 `ShowCompletion`에 `confirm` null 인자 검사를 더했다. null을 넘기는 곳은 없다.

**검토(새 에이전트, 읽기 전용):** 높음 0·중간 1·낮음 6.
- 반영:
  - 중간 1: 다른 사람 줄 목록이 blame보다 적었다. 아래와 공유 표 Vh2.1~Vh2.3을 blame대로 맞췄다. 계획에 있던 호진님 `:188`·`:213-214`는 실제로 바뀌지 않았다.
  - 낮음 1·2·3: 주석. 단 `CombatDisplayServices.cs:17-22`는 V2-g(Vg2.6)에서 고친다.
  - 낮음 6: 다른 점 2.
- 남김:
  - 새 `.meta` 끝 줄바꿈 없음(낮음 4, 앞 Phase와 같음).
  - 폴더 배치 안내의 `Display/`·`Assist/` 예시에 새 형식 더하기(낮음 5, V2-h).

**다른 사람 줄(HEAD 줄 번호)**
- 현강님(`CombatViewSystem.cs`):
  - 지움: `:4`(using)·`:103`·`:192`
  - 바꿈·옮김: `:646`(V2-e가 옮긴 오디오 인자) → `_displayServices.Audio`, `:885` → 읽기 객체, `:956` → `CombatSceneDisplay`
  - 시험: `FrameworkRuntimeCompositionTests.cs:228-229`
- 천명님(`CombatViewSystem.cs`):
  - 지움: `:94`·`:104`
  - 바꿈·옮김: `:244` 꼴 바뀜, `:879·882·884·887` → 읽기 객체
  - 시험: `FrameworkRuntimeCompositionTests.cs:230`
- 호진님(`CombatViewSystem.cs`):
  - 지움: `:99-100`·`:189`·`:249-250`
  - 바꿈·옮김: `:236-238` → 보고 객체 생성자, `:649` → `_blockedInput.Report`, `:1018` → `Restriction`, `:1025-1037` → 보고 객체 `Report()`
- `CombatSceneDisplay.cs`의 바뀐 줄은 우리 줄이다(현강님 `:274`·`:276`은 그대로).

**새 파일(커밋 때 `.meta`와 함께):** `Display/CombatDisplayValueReader.cs`, `Input/Assist/CombatBlockedInputReport.cs`.

**Unity에서 볼 것**
- 컴파일.
- EditMode 시험: `CombatViewSystemTests`(완료 표시·확인 계열 포함)·`FrameworkRuntimeCompositionTests`·`CombatDisplayReuseTests`·`CombatMovementViewTests`·`CoreStageCombatCycleTests`·`TutorialInputTests`·`TutorialViewSystemTests`·`CombatEndUITests`.
- 재생:
  - 전투 진입 때 배경 맵(스테이지 키)
  - HUD 골드·타임라인·가방 재고
  - 튜토리얼에서 막힌 입력 안내
  - 승리·패배 결과 화면 확인 뒤 완료 배경음

### Phase V2-g — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

사용자 "Unity 확인 문제 없음, V2-g 진행해"(V2-f Unity 확인 문제 없음), 진행 중 "해당 Phase는 큰 작업이니 검증에이전트 3개 돌리자". Q-T6 c·d는 앞서 허락됨(A).

**바꾼 것(운영)**
- 생성 주입(W6-6):
  - 새 계약 둘 `Display/ICombatSceneFactory.cs`(`CreateBackdrop`·`CreateDisplay`)와 `Input/ICombatSceneInputFactory.cs`(`CreateSceneInput`), 새 구현 `Factory/Runtime/InGame/View/CombatSceneFactory.cs`를 만들었다(+ `.meta` 셋). 구현은 상태 없는 `sealed`이고 `Instance` 하나이며, 정적 Factory 둘에 위임한다(TP-14).
  - 조정자 생성자가 두 계약을 필수로 받는다. 정적 Factory 호출 셋(배경·표시·입력 묶음)은 계약 호출이 됐다. Factory 네임스페이스 `using`도 지웠다(View 층에서 Factory 의존이 없어짐).
  - 연결 줄 `InGameViewSystemFactory.cs:60`(호진님 줄)이 `CombatSceneFactory.Instance`를 두 번 넘긴다.
- 운영 입구 필수(W6-7):
  - `BindServices`의 선택 인자 넷(텍스트·재고·튜토리얼 둘)을 필수로 했다. `_textPresentation`도 null 검사한다.
  - 보고 객체는 두 Sink를 각각 필수로 받는다. 짝 검사·`?.`·null 반환은 지웠다.
  - 표시 값 읽기는 재고를 필수로 받는다(`?.`·null 갈래 삭제, L10).
  - `CombatDisplayServices`의 선택 인자 넷(오디오·화면 전환·UI·카메라 효과)을 필수로 했다. 설명의 null 허용 문구도 고쳤다(클래스·인자·속성).
  - 표시 Factory는 화면 전환 Presenter를 늘 만든다(민형님 `:147-150`).
  - 입력 Factory는 거절 안내(현강님 `:63-64`)와 환경 호버를 늘 만든다. 연결 입력 묶음은 둘을 필수로 받는다.
  - 표시의 `_audio?.`를 지웠다(국면 배경음·승리 효과음·완료 뒤 배경음). 생성자는 오디오를 필수로 검사한다.

**동작:** 운영은 같다. 검토 1이 확인했다: 새 필수 인자는 모두 `GetService`로 채워지고, 그것은 없으면 예외를 던지므로 null이 될 수 없다. 같은 객체를 같은 순서로 만든다. 시험 조립에서는 꺼져 있던 기능이 가짜로 켜진다: 오디오·화면 전환·UI·카메라 효과·튜토리얼·안내·환경 호버.

**시험**
- 도우미(우리 `CombatViewSystemTests`, TP-11):
  - 가짜 넷: `SilentAudio`·`InstantTransitions`/`InstantScope`(Play 즉시 끝남, H3)·`SilentUI`·`SilentCameraEffects`
  - 텍스트 가짜 `SilentText`(확인창 계약을 구현하지 않음, L9)
  - `DisplayServices(...)`: 넘기지 않은 넷을 가짜로 채움
  - `Database`: 실제 운영 DB를 한 번 읽어 둠(TP-15 (5))
- rig는 `View.InitData(Database)`를 부른다(H1·TP-15 a). 재고·튜토리얼은 rig의 실제 Buffer 창구를 쓴다.
- 조정자 생성 일곱 곳에 `CombatSceneFactory.Instance` 둘을 넘겼다. 그중 천명님 `CombatMovementViewTests.cs:586`은 Q-T6 c 허락분이다.
- `BindServices` 셋에 넷을 더했다(우리 줄 끝, TP-12).
- 천명님 `BindView`에 `InitData` 한 줄을 더했다(Q-T6 d).
- 표시 서비스 묶음 생성 열두 곳은 도우미로 바꿨다. 다른 사람 줄은 민형님 `CombatSceneEventPresenterTests.cs:173`·현강님 `:1449`이다(Q-T5).

**Vg2.10 분석(변경 없음, Unity에서 확인)**
- 화면 전환 Presenter가 시험 조립에도 생겨 국면 사건(전투 진입·밤 끝·승리)은 다음 연출 Tick에 끝나고, 그동안 다음 사건이 시작되지 않는다. 지연은 사건당 1 Tick을 넘지 않는다(검토 2).
- `CombatMortarAssetTests.cs:525-610`: 결과마다 끝날 때까지 Tick을 사건 수 + 3번까지 돌리므로 견딘다.
- `CombatSceneEventPresenterTests.cs:705-790`(실제 Core 기차):
  - `:741`은 `Play` 직후 기차가 켜졌는지 단언한다. 기차 결과는 자동 진행 `TryAdvance(기차) ?? BeginNext(턴)`의 기차 갈래에서만 나오고 첫 사건이 기차 사건이다. 국면 사건(`CombatEntered`·`NightEnded`)은 Core의 턴 계산 두 곳에서만 나오므로 기차 결과에 섞이지 않아, 이 단언은 성립한다.
  - 다른 결과는 Tick 예산(사건 수 + 1) 안에 끝난다.
- 그 밖에 실제 표시로 국면 사건을 재생하는 곳(`CombatRailwayContactSceneTests`는 패배라 전환 없음, `PhaseTransition_*`은 자기 가짜, `CombatDisplayReuseTests`는 이동·복귀만)도 영향이 없다(검토 2 전수 확인).

**계획과 다른 점(넷)**
1. 우리 시험 `AttachRoot_WithoutViewDatabase_IsRejected`: rig가 이제 View DB를 받으므로 rig 생성자에 `withViewDatabase`(기본 참)를 두고, 이 사례만 DB 없는 rig를 따로 만든다. 확인 내용(Throws·연결 안 됨)은 같다. 계획은 이 영향을 보지 못했다.
2. 표시 생성자에 오디오 null 검사를 더했다(Vg2.7 글에 없음, 겹치는 방어).
3. 조정자의 Factory `using`을 지웠다(정적 호출이 없어져 쓰지 않음).
4. `BindServices` 예외가 바뀌었다(시험에만 드러남). 튜토리얼 둘이 모두 null이면 예전에는 통과했고 지금은 `ArgumentNullException`이다. 하나만 null이면 `ArgumentException` → `ArgumentNullException`이다. 이것을 단언하는 시험은 없다.

**검토(새 에이전트 셋, 읽기 전용 — 사용자 지시)**
- 1(운영 코드): 높음·중간 0, 낮음 9.
- 2(시험 영향): 높음·중간 0, 낮음 3.
- 3(다른 사람 줄·문서·SSOT): 높음 0, 중간 3, 낮음 6.
- 반영:
  - 낡은 null 허용 설명: `CombatDisplayServices` 속성 셋, 표시 생성자 인자, 표시 Factory `services` 인자(민형님 줄 `:55`), 입력 Factory `tables`·`audio` 인자, 조정자 주석, `CombatSceneFactory` 설명.
  - 공유 표 Vh2.1~Vh2.4에 V2-g 다른 사람 줄을 blame대로 더했다.
  - Vg2.10 분석 기록(이 절).
  - 계획과 다른 점 넷.
- 남김:
  - `PostBindService`의 튜토리얼 `?? throw`(호진님 줄)와 보고 객체 검사가 겹친다. 해는 없고, 공유 메모에 제안만 한다.
  - 정적 Factory의 `CreateSceneInteraction`·`CreateEnvironmentHover`를 private으로 좁히는 것(선택).
  - 메인 미배치 안내 예외 문구가 이제 "입력 정지 뒤"만 뜻한다(동작 같음).
  - rig 시험 전체가 운영 DB 에셋에 묶인다(TP-15 a로 받아들인 비용).
  - 도우미가 null 인자를 가짜로 바꿔, 녹음 가짜를 실수로 빼도 드러나지 않는다.
  - 새 `.meta` 끝 줄바꿈 없음.

**결정 대기(계획에 없던 것)**
- **Q-V2g-1** `CombatSceneEventPlayback.cs:53-55`의 선택 인자(`phaseTransition`·`camera`·`cameraMotion` = null)와 `_phaseTransition?.`·`_trainCamera?.`(`:62·72·75·128·164·172·188`, 민형님 줄 `e18c8313`). 부르는 곳이 표시 Factory 하나이고 이제 늘 넘기므로 닿지 않는 갈래다. 설계 W6-7과 같은 꼴이라 V2-i 평가자가 V-M12의 남은 것으로 셀 수 있다.
  - A(권장): 필수로 바꾸고 `?.`를 걷는다. 민형님 줄이라 공유 문서 §24에 적는다.
  - B: TP-17 남기는 null 허용에 더한다.
- **Q-V2g-2** 조정자 `AttachCreated(created, root, inspection = null)`의 시험 입구 null 허용(우리 줄). `root`는 null을 받고, `inspection`은 기본값이 null이다. 가짜 표시로 붙이는 시험이 쓴다.
  - A(권장): 그대로 두고 TP-17에 적는다. 시험 입구라 운영 조립 null 허용이 아니다.
  - B: 시험 입구를 따로 나눈다(시험 여러 곳 수정).

**다른 사람 줄:** Vh2.1~Vh2.4에 적었다.
- 민형님: `CombatSceneDisplayFactory.cs:55·147-150`, `CombatDisplayServices.cs:27·35·48·88`, 시험 `:173`.
- 호진님: `InGameViewSystemFactory.cs:60`, `CombatViewSystem.cs:232-234`.
- 현강님: `CombatSceneInputFactory.cs:63-64`, `CombatSceneDisplay.cs:204·274`, `CombatViewSystem.cs:256`, `CombatDisplayServices.cs:49`, 시험 `:1449`.
- 천명님: `CombatDisplayServices.cs:36`, 시험 `CombatMovementViewTests.cs:586`·`BindView`.
- Framework·GlobalSystem·Core 변경은 없다.

**새 파일(커밋 때 `.meta`와 함께):** `Display/ICombatSceneFactory.cs`, `Input/ICombatSceneInputFactory.cs`, `Factory/Runtime/InGame/View/CombatSceneFactory.cs`.

**Unity에서 볼 것**
- 컴파일. 특히 시험 어셈블리를 본다: 새 `using`, 가짜 구현, `UnityEditor.AssetDatabase`.
- EditMode 시험은 전체를 권장한다. 조정자·표시를 만드는 시험 파일 여덟과 그것을 빌려 쓰는 시험이 모두 닿는다. 특히 볼 것:
  - `CombatViewSystemTests`(+ `AttachRoot_WithoutViewDatabase_IsRejected`)
  - `CombatSceneEventPresenterTests`(기차 `OperationalDatabaseLive_…`·`PhaseTransition_*`)
  - `CombatMortarAssetTests`·`CombatDisplayReuseTests`·`CombatMovementViewTests`·`CoreStageCombatCycleTests`
  - `CombatPointerMappingTests`·`TerrainOperationalDisplayTests`·`WallAttackInteractionTests`·`CombatRailwayContactSceneTests`·`FrameworkRuntimeCompositionTests`
- 재생:
  - Boot부터 전투 진입·밤·아침 화면 전환
  - 배경음·효과음
  - 정보창·환경 호버
  - 튜토리얼 막힘 안내
  - 결과 화면

**결정 뒤 반영(2026-10-25 사용자 "권장안으로 하고 유니티 확인했어 V2-h 진행해")**
- V2-g Unity 확인 문제 없음.
- **Q-V2g-1 A(코드, 그 확인 뒤에 바꿔 컴파일 미확인):** `V/Playback/CombatSceneEventPlayback.cs`
  - 생성자의 선택 인자 셋(`phaseTransition`·`camera`·`cameraMotion`, 우리 줄 `:53-55`)이 필수다. 셋 중 하나라도 null이면 `ArgumentNullException`이다.
  - 기차 카메라를 늘 만든다(`:63`, 우리 줄). 낼 값이 없으면 지금처럼 진입·이탈 알림을 받지 않는다(`HasMotion`).
  - 닿지 않게 된 갈래를 걷었다.
    - 민형님 줄 일곱: `:62` 대입(null 검사), `:72`·`:75`·`:128`·`:164`·`:172`·`:188`의 `?.`
    - 우리 줄 넷: `:65`·`:149`·`:170`·`:198`의 `?.`
  - 주석: 필드 주석 `:36`, 인자 설명 `:44-45`, `phaseTransition` 인자 설명 새로.
  - 부르는 곳은 표시 Factory 하나(`CombatSceneDisplayFactory.cs`의 재생 조정 생성)이고 이미 셋을 넘긴다. 시험은 이 생성자를 직접 부르지 않아 시험 변경은 없다.
  - 남긴 것:
    - `_thorns?.Clear()`. 가시밭 표시 프로필이 없으면 null인 데이터 갈래다.
    - `CombatCameraMotionSettings.None`. 처리기 둘(`CombatCharacterEventHandler`·`CombatRoarEventHandler`, Presenter 선택 인자 쪽이라 TP-17)과 시험이 계속 쓴다.
- **Q-V2g-2 A:** `AttachCreated`의 시험 입구 null 허용(`root`·`inspection`)을 남기고 TP-17에 더했다(코드 변경 없음).
- 공유: 민형님 Factory 공유 §24(V2-h).

### Phase V2-h — 2026-10-25, 문서 완료(모두 미전달). 코드는 V2-g 끝의 Q-V2g-1 한 곳뿐

사용자 "권장안으로 하고 유니티 확인했어 V2-h 진행해".

**쓴 것**
- **Vh2.1** 현강님 메모 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` §9
  - 조정자에서 옮긴 줄 표(V2-e·V2-f)와 바꾼 줄(V2-a·V2-b·V2-g)
  - 고친 시험 둘(Q-T5·Q-T6 b), 화면·동작 차이, blame 안내
- **Vh2.2** 호진님 메모 `handoffs/2026-10-22-combat-view-structure-hojin-share.md` §11
  - 넘겨받기 소유자(V2-b·V2-c)와 동작 차이 셋
  - 조정자 줄 표(V2-e·V2-f·V2-g)와 연결 줄
  - `:213-214` 겹침 제안, 고친 시험(Q-T6 a)
- **Vh2.3** 천명님 Core 전달 문서 §34(요청 없음)
  - Buffer 번호 있는 비우기와 천명님 줄 둘, `CombatPathInput.cs:24`
  - 조정자 V2-f 줄, `CombatDisplayServices.cs:36`
  - 고친 시험 넷
- **Vh2.4** 민형님 Factory 공유 §24
  - 연결 줄 diff, 새 `CombatSceneFactory`와 계약 둘, 입력·표시 Factory
  - `CombatDisplayServices`의 민형님 줄, 재생 조정의 민형님 줄 일곱(Q-V2g-1), 화면 전환 사건 고르기(V2-d)
  - 고친 시험, 시험 조립의 화면 전환 1 Tick
- **Vh2.5**
  - 폴더 배치 안내:
    - 갱신 줄, §0 트리 설명 다섯, §1.1 표 여섯 줄(새 파일 열하나, `CombatEventTypeTable`)
    - §2 규칙 셋: 생성 계약 자리, 공통 수명 어댑터, 사건 형식 지식(아직 표 밖인 소리·폭발 칸 포함)
  - 설계:
    - 머리에 구현 상태
    - 구현 메모 넷: W5-1 TP-2, W5-2 TP-3, W5-6 TP-4, W7-2 TP-8·TP-9
    - W6-7 결정(Q-V2g-1·2)과 TP-15 이름, §9 작성 완료
    - TP-6·TP-7(W6-1)과 TP-10(W6-5)은 앞 Phase 메모에 이름이 있다.
  - 계획: TP-17 셋, §0 표 한 줄, Vh2.1~Vh2.4 행, 커밋 묶음(새 파일 열하나·계획 문서·이름 바꾸기 내용·필수 인자 묶음)
  - CLAUDE.md
- 공유에 적은 다른 사람 줄은 모두 기준 커밋 `23436b00`의 blame으로 다시 확인했다.

**계획과 다른 점:** Vh2.4에 V2-g 뒤 결정(Q-V2g-1)의 민형님 줄 일곱을 더했다(계획을 쓸 때는 없던 변경).

**검토(새 에이전트, 읽기 전용):** 높음 0·중간 2·낮음 11, 모두 반영했다.
- 중간 1: 현강님 §9.2의 "V2-a 판정 줄이 V2-e에서 묶음으로 옮겨짐"은 틀렸다. `:1181`은 조정자 `SyncEnvironmentHover`에 남고, `:1178`·`:1182`만 묶음을 부른다. 고쳤다.
- 중간 2: 커밋 묶음에 untracked 계획 문서가 빠졌다. 더했다.
- 낮음:
  - 호진님 §11
    - `:63`(생성자 서명)은 그대로다. 옮긴 목록에서 뺐다.
    - 무효화 줄 아홉에 줄 번호를 붙였다.
    - `:189`는 "지움"이 아니라 타임라인 인자로 넘김이다.
    - 클래스 설명 `:58`을 더했다.
  - 천명님 §34
    - `BindView` 인자는 다섯이다(V2-f 빈 타임라인 포함).
    - 우리 줄 `:426-427` 도우미 변경을 적었다.
    - `:391-395`는 다섯 줄 메서드다.
  - 현강님 §9
    - Router 넘기기 `:1165`는 우리 줄이다.
    - `:1227`은 그대로다.
    - `PlacementDragInteraction`의 V2-a 변경(우리 줄)을 적었다.
  - 배치 안내: 표 밖에 남은 형식 지식(소리·폭발 칸)을 적었다.
  - 계획
    - TP-17 머리와 §0 표가 "둘"이었다. 셋으로 고쳤다.
    - Q-V2g-2 서명: `root`는 null을 받고, `inspection`만 기본값이 null이다.
    - "셋 모두 null" → "셋 중 하나라도 null"
  - 설계: TP-15를 이름으로 적었다.
- 맞음으로 확인된 것:
  - Q-V2g-1 코드: 멤버·반환형이 맞고, 부르는 곳 하나가 늘 null 아닌 값을 넘긴다. 시험은 직접 부르지 않고 새 null 길도 없다.
  - 민형님 줄 전부
  - 현강님 조정자 지운 줄 41개
  - 천명님 줄
  - 다른 사람 시험은 허락분뿐
  - 새 파일 열하나의 자리·네임스페이스
  - 이름 바꾸기 GUID
  - CLAUDE.md 두 줄

**남은 것**
- Q-V2g-1 코드의 Unity 컴파일·시험 확인(`CombatSceneEventPresenterTests` 기차·`PhaseTransition_*`, `CombatMortarAssetTests`, 재생 중 기차·화면 전환)
- V2-i 측정(B 판 `23436b00`)
- 사용자 커밋, 공유 전달(사용자)

### Phase V2-i — 2026-10-25, 측정 완료(코드 변경 없음)

- 사용자 "V2-i 진행해". Q-V2g-1 코드의 Unity 확인 여부는 듣지 못했다(측정은 코드만 읽음).
- 두 판(M·N)을 스크래치패드 `view-ab2/`에 꺼냈다.
  - 배정표는 `view-ab2-mapping.json`(M = 작업 트리, N = `23436b00`)이다.
  - 꺼낸 스크립트는 `make_ab2.py`, 지시문은 `vi_prompts.json`이다. 1라운드 지시문을 기록에서 되살려 판 이름·파일 수·범위 한 줄만 바꿨다.
- 하위 에이전트 셋(SSOT·독립성 / SOLID·FW·있는 기능 / 불필요·우회)이 블라인드로 매겼다. 원점수는 N 7.2 / M 8.2다.
- 합친 결과는 **N 8.1 → M 8.7**이고, 예상은 8.92였다.
  - 기준별 표·문제 상태·새로 찾은 것은 [설계 §11](../specs/2026-10-24-combat-view-stage-round2-design.md)에 있다.
  - 같은 코드인 1라운드 측정 Q(8.3)보다 N이 0.2 낮다. 강조 층 문제가 중간으로 올랐기 때문이다.
- 직접 대조한 것:
  - `CombatAttack_Ballista`를 참조하는 외형은 `UnitVisual_Ballista` 하나다(클립 길만 탄다).
  - 강조 층 속성은 다섯 곳에 있다(`FieldHighlightLayers` 열거·`DrawOrder`·`ColorOf`·`FillsCell`, `FieldHighlightState.ActingLayers`, `FieldOutlineRenderer` 평소 층, `CombatAttackFeedback` 색조).
  - Ballista 관통 접촉 일정은 `CombatBallistaAnimation.cs:99-109`와 `CombatAttackPlayback.cs:406-425` 두 벌이다.
  - `Combat.unity`의 `attackActiveIndicator`(`:1339`)·`worldCamera`(`:8817`)는 비어 있다.
  - `CombatPathInput.BeginPlanning`·`FieldUnitView.BeginReactionPose`는 시험만 부른다(시험 전용 멤버로 합침).
  - 배치 생성자의 `requests`는 검사만 하고 보관하지 않는다.
- **2라운드 끝.** 남은 것:
  - 사용자 커밋(커밋 묶음 위)
  - 공유 넷 전달(사용자)
  - Q-V2g-1 코드의 Unity 확인
  - 3라운드 상세 설계(W8·W9). 강조 층 다섯 곳을 넣을지 정하기
