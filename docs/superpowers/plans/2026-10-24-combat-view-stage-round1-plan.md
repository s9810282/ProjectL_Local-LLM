# 전투 View 단계 1라운드 Task 계획 (2026-10-24)

- 설계: [view-stage-design](../specs/2026-10-24-combat-view-stage-design.md) §3(W1~W4).
- **결정(2026-10-24 사용자 "모두 A로 하고 1라운드 Task 계획 써줘"):** Q-V1~Q-V8·Q-T1~Q-T2 모두 A. 설계에서 정한 것 P1~P7은 쓴 그대로다.
- **계획을 쓰며 코드와 대조해 새로 정한 것은 TP-1~TP-15다(맨 아래). 그중 셋은 다른 사람 시험이라 허락이 필요하다(Q-T3 a·b·c).** 허락 전에는 그 Phase를 시작하지 않는다.
- 기준 코드: 작업 트리. HEAD `4c328d07`에 커밋 전 C 단계·골격 점검이 더해진 상태다. 줄 번호는 지금 작업 트리 기준이다. 앞 Phase가 줄을 바꾸므로 Phase마다 다시 찾는다.
- **진행 규칙**
  - Phase 하나가 끝나면 멈추고 보고한다. 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인한 뒤 다음 Phase로 간다.
  - Phase 코드가 끝나면 보고 전에 새 에이전트(읽기 전용)로 그 Phase 변경을 검토하고, 지적을 반영한 뒤 보고한다. 검토 결과와 반영 내용은 실행 기록에 적는다.
  - 전투 View 작업이라 **MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다.** 보고에 "컴파일 미확인"을 적는다.
  - 시험 처리(Q-T1 A):
    - 걷은 기능이나 바뀐 동작을 보던 우리 시험(사례)은 지운다.
    - 남는 내용을 보는 우리 시험은 호출·입력만 맞춘다.
    - 깨지지 않는 시험은 손대지 않는다.
    - 고친 시험은 모두 실행 기록과 공유 메모에 적는다.
  - **다른 사람 시험 — 고칠 수 있는 곳은 아래뿐이다.**
    - Q-T2 A(허락됨, 호진님): `T/StageMapNodeMovementBootSmokeTests.cs:523`, `T/StageMapNodeMovementBootSmokeTests.Victory.cs:207`의 `Read<Button>(…Hud, "completionButton")`
    - Q-T3(허락 대기): 현강님 `T/CombatEndUITests.cs` SetUp·TearDown(a), 현강님 `T/CombatInteractionTests.cs:198·212` 두 시험의 입력(b), 천명님 `T/CombatDisplayReuseTests.cs:829·832·833`(c)
  - 우리 영역 안의 다른 사람 운영 줄을 고치면 Phase V-e 공유 메모에 적는다. 이번에 닿는 줄은 각 Phase에 적었다.
  - Framework·GlobalSystem·Core·다른 씬은 고치지 않는다. 예외는 영역 안인 `CombatEndUI`다. `PackUI`·`BattleChangerUI`는 서명을 바꾸지 않고 넘기는 값만 바꾼다(설계 P2).
  - 새 파일은 없다. 지우는 파일은 `.meta`와 함께 지운다.
  - **씬에 붙은 컴포넌트의 스크립트는 지우지 않는다**(설계 P5). `HandPresenter.cs`·`HandCardHoverView.cs`는 남는다.
- 경로 약어:
  - `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `G/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/`
  - `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`
  - `T/` = `Assets/Tests/EditMode/Framework/`

## 0. 점수 흐름 (설계 §1 기준선, 알려진 문제 기준, 누적)

| Phase 끝 | 7.4에서 | 바뀌는 것 |
|---|---|---|
| V-0 | 7.4 | 측정 기준 판 고정(코드 변경 없음) |
| V-a (W3) | **7.6** | SSOT 중간 둘(V-M2 대여 정리 세 벌, V-M3 강조 층 두 벌) 해소 |
| V-b (W2) | **7.8** | SSOT 중간(V-M1 입력 수신 조합)·낮음(V-L3), 독립성 낮음(V-L13 포기 빈틈) 해소 |
| V-c (W4) | **8.1** | 불필요 중간 둘(V-M11 옛 손패, V-M12 HUD 완료 대체·선택 인자), SOLID 낮음(V-L6), 독립성 낮음(V-L16), 불필요 낮음(V-L17·V-L19) 해소 |
| V-d (W1) | **8.5** | 불필요 높음(V-H1 최종 승리 화면) 해소 |
| V-e·V-f | 8.5 | 문서·측정 |

- 기준별 1라운드 뒤: SSOT 9.0 · SOLID 6.5 · FW 9.5 · 있는 기능 8.0 · 독립성 8.75 · 불필요 9.0 → 평균 8.46. 산술은 Phase 순서대로 7.63 / 7.79 / 8.13 / 8.46이다.
- **설계 §7의 불필요 9.25(Q-V8 A면 9.5)보다 낮게 잡았다.** 아래 둘을 낮음으로 남겨 셌기 때문이다.
  - V-M12 중 시험 입구의 null 허용(TP-7)
  - 쓰는 곳 없이 남는 손패 스크립트 두 파일(TP-10)
- 그래서 남는 낮음은 V-L18·V-L20과 이 둘, 모두 넷이고 상한(−1.0)에 걸린다.
- **알려진 문제 기준이다.** 지난 두 번은 실제 측정이 예상보다 0.5~1.8 낮았다(설계 §0). 실제 점수는 V-f의 A/B 측정으로 확인한다.

## Phase V-0 — 측정 기준 판 고정 (코드 변경 없음)

**목표:** 1라운드 뒤 A/B 측정(V-f)의 B 판(라운드 전 판)을 지금 상태로 고정한다(Q-V2 A).

| Task | 내용 |
|---|---|
| V0.1 | **권장(TP-14 A):** 사용자가 지금 작업 트리(커밋 전 C 단계·골격 점검)를 커밋하고 해시를 알려 준다. B 판 = 그 커밋이다. |
| V0.2 | 커밋하지 않으면(TP-14 B): V-a 착수 직전에 평가 범위 파일을 스크래치패드 `view-round1-baseline/`로 복사한다. 범위는 `V/` 전체, `R/BufferSystems/CombatView/CombatViewBuffer.cs`, `G/UISystem/Screens/Stage/CombatEndUI.cs`, `F/`의 셋이다. 평가자는 B 판을 그 사본에서 읽는다. |

## Phase V-a — W3: 대여 정리 하나·강조 층 원본 하나 (V-M2·V-M3)

**목표:**
- 유닛 표시의 대여 칸 정리를 한 메서드로 모은다. 풀 반환 때 크기 대여도 되돌린다(결함 수정).
- GridOutline 채우기의 우선순위·색을 `FieldHighlightLayers`에서 읽는다. 채우기 이름·순서·색은 그대로다.

| Task | 파일 | 내용 |
|---|---|---|
| Va.1 | `V/Display/Unit/FieldUnitView.cs` | private `ReleasePresentation()` 하나를 만든다. 순서는 지금 `Bind` 순서(`:130-135`)대로 자세 → 크기 → 플래시 → 클립 → 투명도 → 체력 미리보기다(TP-2). |
| Va.2 | 같은 파일 | `Bind`(`:130-135`)는 여섯 줄 대신 `ReleasePresentation()`을 부른다. |
| Va.3 | 같은 파일 | `OnDisable`(`:264-274`)은 `SyncHudActive()` → `ReleasePresentation()` → `_clipChannel?.Pause()` 순서로 바꾼다. 체력 미리보기 지우기가 클립 멈춤보다 앞으로 온다. 서로 다른 대상이라 결과는 같다. |
| Va.4 | 같은 파일 | `ResetForPool`(`:211-226`)은 `ReleasePresentation()` → Ballista 무기 정리(그대로) → HUD 정리(현강님 줄 둘, 그대로) → `EntityId` 비우기 → 끄기 순서로 바꾼다. 크기 정리가 새로 들어가고, `EntityId`보다 먼저 정리하므로 대여의 `IsCurrent`가 통과한다(V-M2 결함). |
| Va.5 | `V/Display/Field/FieldHighlightLayers.cs` | 둘을 더한다(TP-3). `FillsCell(layer)`는 GridOutline이 칸을 채우는 층(이동·공격·적 공격·위험·영향)인지 답한다. `FillColorOf(layer, hoverState)`는 영향이면 `HoverColor(hoverState, Attack)`, 아니면 `ColorOf(layer)`다. 영향 칸 특례의 근거 주석은 지금 `FillColor` 요약(`FieldOutlineRenderer.cs:563-566`)에서 옮긴다. |
| Va.6 | `V/Display/Field/FieldOutlineRenderer.cs` | `FillKind` 열거(`:124-133`)를 지운다. 채우기 칸 표는 다음 셋을 이어 만든다: 배치 구역, `DrawOrder` 가운데 `FillsCell`인 층(낮은 것부터), 호버. 이름은 `FieldFill_PlayerZone`·`FieldFill_<층 이름>`·`FieldFill_Hover`로 지금과 같고, 순서도 지금 열거 순서와 같다. |
| Va.7 | 같은 파일 | `TryGetFillKind`(`:539-561`)는 이렇게 고른다: 호버 → `DrawOrder`를 위에서부터 돌며 처음 만나는 채우기 층 → 배치 중 배치 구역. |
| Va.8 | 같은 파일 | `FillColor`(`:569-580`)는 층이면 `_palette.TryGet(FieldHighlightLayers.FillColorOf(…))`, 배치 구역은 `_palette.PlayerZone`, 호버는 지금처럼 읽는다. 알파를 바꾸는 뒷부분은 그대로다. |
| Va.9 | 같은 파일 | `:20`·`:55` 주석의 `FillKind`를 고친다. 민형님 줄(`:7·15·41·84·165-170·435-441·466-476·492·711`)에는 닿지 않는다. |
| Va.10 | 시험 | 바꿀 곳 없음. `FieldOutlineRendererTests`의 `FillNames`(`:537-540`)는 이름·순서가 같다. 손잡이 수(`:312`)는 채우기와 무관하다. `ResetForPool`을 부르는 시험 여섯은 크기 대여가 없으므로 결과가 같다. |

**동작:**
- 같은 칸 안 채우기의 우선순위는 위험 > 적 공격 > 공격 > 이동으로 지금과 같다(두 사슬 대조, `DrawOrder` `:75-87`).
- 크기 대여가 남은 채 풀로 돌아가던 유닛이 원래 크기로 돌아간다. 정상 흐름에서는 재생 취소가 먼저 되돌리므로 보이는 차이는 없다.

**다른 사람 줄:** `FieldUnitView`의 현강님 HUD 줄(`ResetForPool` 둘·`OnDisable`의 `SyncHudActive`)은 자리만 그대로다. 공유할 것이 없다.

## Phase V-b — W2: 입력 수신 판정 하나 (V-M1·V-L3·V-L13)

**목표:** 제출 정지·포기 요청·확인창을 입력 사실에 넣고, 입력 입구가 Policy 판정 둘(`IsInputClosed`·`IsInputBlocked`)만 읽게 한다.

| Task | 파일 | 내용 |
|---|---|---|
| Vb.1 | `V/Input/CombatInputFacts.cs` | 생성자 끝에 선택 인자 셋을 더한다: `isSubmissionSuspended`, `isAbandonRequested`, `isModalOpen`(설계 P1). 속성 셋과 요약도 더한다. 기존 생성 16곳(시험)은 그대로 컴파일된다. |
| Vb.2 | `V/Input/CombatInputPolicy.cs` | `IsInputClosed(facts)` = 정지 ∨ 포기 요청, `IsInputBlocked(facts)` = 닫힘 ∨ 확인창(설계 W2 스케치). |
| Vb.3 | 같은 파일 | `PassesCommon`(`:222-229`)에 `!IsInputClosed(facts)`를 더한다. |
| Vb.4 | 같은 파일 | `GetDesiredLayer`(`:40-46`)는 종료와 함께 `IsInputClosed`면 기본값을 돌려준다(TP-4). |
| Vb.5 | 같은 파일 | `CanSubmit`의 `LatestResultSequence != PresentedResultSequence`(`:63`)를 `CombatProgressFacts.IsUnpresented(P, S)`로 바꾼다. 서명은 `(presented, latest)`이고 `using …LifeCycles.Combat`을 더한다. |
| Vb.6 | 같은 파일 | `CanEditPath`의 겹친 `!facts.IsPresenting`(`:141`)을 지운다. `CanShowEnvironmentHover` remarks(`:118-119`)의 "수신 가능(…)"은 "`IsInputBlocked`"로 고친다. |
| Vb.7 | `V/Input/ActionButtons/CombatActionButtons.cs` | `Read`(`:47`)에서 `isSubmissionSuspended` 인자를 지우고 `CombatInputPolicy.IsInputBlocked(facts)`를 본다(TP-6). `Visible` 요약(`:27`)을 고친다. |
| Vb.8 | `V/Input/CombatViewProgress.cs` | 호스트 콜백 `OnSubmissionsSuspended`(`:18-19`)의 이름을 `OnInputClosed`로 바꾼다. 요약은 "새 입력이 막 닫혔습니다(제출 정지 또는 포기 요청)"다. `SuspendSubmissions`(`:65`)와 `MarkAbandonRequested`(`:83-88`)가 둘 다 부른다(TP-5). |
| Vb.9 | `V/Input/UiEntry/CombatAbandonController.cs` | 생성자 인자 `cancelInputPresentation`과 그 필드·호출을 지운다. 정리는 호스트가 한다(TP-5). 머리 요약(`:7-8`)을 고친다. |
| Vb.10 | `V/CombatViewSystem.cs` | `ReadFacts`(`:988-1010`)가 새 사실 셋을 채운다: `_progress.IsSubmissionSuspended`, `_progress.IsAbandonRequested`, `IsModalOpen`. 호진님 줄 셋(`:1008-1010`) 뒤에 더한다. |
| Vb.11 | 같은 파일 | `SyncInputLayer`(`:885-898`)의 정지 갈래를 지운다. `GetDesiredLayer`가 같은 사실을 본다. |
| Vb.12 | 같은 파일 | `CanReceiveDeckInput`(`:1072`)은 `IsInputReady && !CombatInputPolicy.IsInputBlocked(ReadFacts())`가 된다. |
| Vb.13 | 같은 파일 | `SyncActionButtons`(`:1177-1190`)는 `bool blocked = CombatInputPolicy.IsInputBlocked(facts)` 하나를 BattleChanger·Pack·행동 버튼에 넘긴다. |
| Vb.14 | 같은 파일 | `CanEnterFromDeployment`(`:1192-1198`)는 사실을 먼저 읽고 `IsInputClosed(facts)`를 본다. 확인창은 보지 않는다(설계 P3). |
| Vb.15 | 같은 파일 | 호스트 구현(`:914-923`)의 이름을 바꾸고 주석 "제출 정지는 …"을 "제출 정지·포기 요청은 …"으로 고친다. 포기 입구 생성(`:155`)에서 인자 하나를 뺀다. |
| Vb.16 | 시험 | 아래 |

**Vb.16 시험(모두 우리 줄):**
- `T/CombatInteractionTests.cs:490-494·504-510`: `CombatActionButtons.Read(f, s, c, a)`에서 둘째 인자를 뺀다.
  - 정지 사례(`:492`)는 정지 사실이 든 facts로 같은 단언을 한다.
  - 도우미 `WithInput(f, available)`(`:527-533`)에 선택 인자 `suspended = false`를 더해 `isSubmissionSuspended:`로 넘긴다.
- 그 밖의 시험은 바꿀 것이 없을 것으로 본다.
  - `ICombatProgressHost`는 조정자만 구현한다.
  - `CombatAbandonController`는 조정자만 만든다.
  - 포기 뒤 Tick에서 공격 강조가 꺼짐을 보는 시험(`T/CombatViewSystemTests.cs` `ActionButtons_AttackHighlight_…_ClearedAtTickAfterAbandonRequest`)은 Tick 뒤 단언이라 그대로 성립한다(이제 그 전에 이미 꺼진다).

**동작 차이(설계 W2, 결정 Q-V3 A·Q-V4 A):**
1. 포기를 확인한 그 순간 입력이 닫힌다. 배치 확인창 닫기, Drag·조준 정리, 입력 해제, 행동 버튼·BattleChanger·가방 숨김이 그 순간 일어난다. 지금은 골격의 제출 정지 Job이 도는 다음 Tick부터다.
2. 배치 확인창이 열린 동안 가방도 숨는다.
3. 행동 버튼은 확인창 판정을 새로 보지만 보이는 차이는 없다. 확인창은 배치 국면에서만 열리고, 그때 행동 버튼은 이미 숨겨져 있다(`CombatActionButtons.Read`의 `CurrentLayer != Combat` 갈래).
4. 포기 입구의 정리 순서가 "정리 → 깃발"에서 "깃발 → 정리(호스트)"로 바뀐다. 정리(`CancelInputs`) 안의 이동 모드 범위 유지 판단은 그 깃발을 읽는다(V-b 검토 L2). 그래서 이동 모드 범위가 그 순간 지워진다. 예전에는 다음 Poll이 지웠다. 최종 상태는 같다.

**다른 사람 줄:**
- 현강님: `CanEnterFromDeployment` 몸통(`:1192-1197`, 우리 줄 하나만 바꿈 + 사실 읽기를 위로), 호스트 구현의 `_deploymentConfirmation?.Close()`(그대로)
- 호진님(V-e 검토로 더함): `CombatActionButtons.cs:28`(`Visible` 요약)·`:49`(숨김 조건이 `IsInputBlocked`로 넓어짐, `34ee0de4`)
- 호진님: `ReadFacts`의 셋(그대로, 뒤에 더함)
- 천명님: 중괄호만

## Phase V-c — W4: 죽은 경로·조용한 실패 걷기 (V-M11·V-M12·V-L6·V-L16·V-L17·V-L19)

**목표:**
- 운영에서 닿지 않는 옛 손패 경로와 HUD 완료 대체 경로를 코드·계약·필수 참조에서 걷는다.
- 표시의 선택 인자를 필수로 바꾼다.
- 형식 검사와 늦게 드러나는 캐스트를 연결 때로 옮긴다.
- 쓰지 않는 코드를 지운다.

**Q-T3 b·c 허락이 있어야 시작한다**(없으면 해당 Task를 빼고 진행할지 다시 묻는다).

### Vc-A 옛 손패 (V-M11, Q-V5 A)

| Task | 파일 | 내용 |
|---|---|---|
| Vc.1 | `V/Input/Contracts/ICombatPointerPicker.cs` | `TryPickHandUnit`·`IsOverHandDropZone`(`:18·21`)과 요약을 지운다. |
| Vc.2 | `V/Display/Feedback/CombatPointerMapper.cs` | `_hand` 필드·생성자 인자·검사(`:28·35·38·72`)와 손패 두 메서드(`:83-92`)를 지운다. 취소 영역(`:112`)은 `HitsUi(screen)`만 본다. 손패 영역은 늘 숨겨져 있어 지금도 거짓이다. |
| Vc.3 | `V/Display/Feedback/CombatPlacementFeedback.cs` | `_hand`·생성자 인자(`:18·20·25`)와 손패 숨김 두 줄(`:54·60`)을 지운다. |
| Vc.4 | `V/Display/CombatSceneDisplay.cs` | `_hand`·생성자 인자(`:56·97·129`), 옛 손패 그리기(`:219-220`, 현강님 줄), 해제 목록의 `_hand.Release`(`:255·272`), 손패 두 메서드(`:328-334`)를 지운다. |
| Vc.5 | `V/Input/Interaction/PlacementDragInteraction.cs` | 손패 집기(`:249-250`)와 손패 영역에 놓아 회수(`:318-323`)를 지운다. 필드 집기·덱 카드 선택은 그대로다. 현강님 줄(`:107-108`)에는 닿지 않는다. |
| Vc.6 | `V/CombatSceneRoot.cs` | `hand` 필드·`Hand` 접근자·필수 검사(`:35·56·81-82·99`)를 지운다. 씬의 직렬화 값은 Unity가 무시한다. Missing Script는 생기지 않는다. 컴포넌트는 그대로 붙어 있다. |
| Vc.7 | `F/CombatSceneDisplayFactory.cs` | `hand` 지역 변수·`Configure`·인자 셋(`:82·87·96·100·165`)을 지운다(연결 줄, 민형님 Factory 공유). |

- **씬 상태:** 옛 손패의 `CardRoot`는 씬에서 켜져 있다. 하지만 RectTransform뿐이라 그리기를 멈춰도 보이는 것이 없다(`Combat.unity` `&332209998`).
- **파일:** `HandPresenter.cs`·`HandCardHoverView.cs`는 남긴다(TP-10). 그 자체를 보는 우리 시험도 남긴다: `HandCardHoverTests`, `CombatDisplayReuseTests`의 `HandCards_…` 둘(`:292·321`).

### Vc-B HUD 완료 대체 경로·표시 선택 인자 (V-M12, Q-V6 A·Q-V7 A, TP-7)

| Task | 파일 | 내용 |
|---|---|---|
| Vc.8 | `V/Display/Hud/CombatHud.cs` | 완료 확인을 모두 지운다: `CompletionSlot`·`completionButton`·`completionText`·`_completionConfirmed`·`_isInputAttached`, `ShowCompletion`·`HideCompletion`·`ConfirmCompletion`·`AttachInputOnce`, `IUIInputReceiver` 구현과 세 메서드, 쓰지 않게 된 `using`. `Clear`는 완료를 숨기지 않는다. `FindMissingReference`는 `runGoldText`만 본다. 머리 remarks(`:20-21`)를 고친다. 씬의 버튼 객체는 결과 화면 확인 버튼이라 그대로 남는다. |
| Vc.9 | 같은 파일(Q-T3 c, TP-15) | 옛 드로우 문구 `remainingDrawText`·`HideLegacyDrawText`와 그 호출 셋(`:31-32·43·110·124·127-131`, 천명님 줄)을 지운다. 씬의 그 텍스트 객체는 이미 꺼져 있다(`&1825410295` `m_IsActive: 0`). |
| Vc.10 | `V/Display/CombatSceneDisplay.cs` | 생성자 선택 인자 넷(`unitInfo`·`unitHudLayer`·`pack`·`completion`, `:117-120`)을 필수로 바꾸고 null이면 `ArgumentNullException`을 낸다. 시험 직접 생성이 없고 Factory만 만든다(TP-7). |
| Vc.11 | 같은 파일 | `ShowCompletion`(`:275-281`)은 `_completion.Present`만, `HideCompletion`(`:283-287`)은 `_completion.Dismiss`만 부른다. |
| Vc.12 | 같은 파일 | 늘 거짓이 된 검사를 지운다: `_pack?.`(`:223·392·397·402`), `_unitInfo?.`·`_unitInfo == null`(`:224·317`), `_unitHudLayer == null`(`:617`), 주석 `:131-132`. |
| Vc.13 | `V/CombatSceneRoot.cs` | `FindMissingReference`의 `pack` 다음에 `unitInfo` 필수 검사를 더한다(씬에는 연결됨, `Combat.unity:8597`). |
| Vc.14 | `F/CombatSceneDisplayFactory.cs` | `root.UnitInfo?.Configure`(`:79`)는 `?.`를 지운다. 가방 삼항과 주석(`:89-92`)은 `ICombatPackDisplay pack = root.Pack;`로 바꾼다. 씬 루트 검사가 둘을 이미 막는다. |

**남기는 null 허용(TP-7, 2라운드 W6에서 주입과 함께 다시 봄):**
- 조정자 `BindServices`의 선택 인자 넷(시험 입구)
- `CombatDisplayServices` 생성자의 선택 인자 넷(시험 입구)
- 조정자의 가짜 루트용 `?.`(`_subscribedRoot?.BattleChanger?.` 등)
- 입력 Factory의 `text == null` 갈래

운영 입구 `CombatDisplayServices.From`·`PostBindService`는 서비스가 없으면 `GetService`가 이미 `KeyNotFoundException`을 낸다(`SystemServiceStore.cs:83-91`). 그래서 운영이 조용히 빠뜨리는 길은 없다.

### Vc-C 조용한 형식 검사·캐스트 (V-L6·V-L16)

| Task | 파일 | 내용 |
|---|---|---|
| Vc.15 | `V/CombatViewSystem.cs` | `AttachCreated(created, root)`(`:482`)에 셋째 선택 인자 `ICombatUnitInspection inspection = null`을 더한다. 인수에 성공하면 `_inspection`에 두고, 표시를 놓는 자리(`_display = null`)에서 함께 비운다. |
| Vc.16 | 같은 파일 | 운영 연결(`:467`)은 `CombatSceneDisplay created = CombatSceneDisplayFactory.Create(…); AttachCreated(created, root, created);`가 된다(TP-9). |
| Vc.17 | 같은 파일 | `RouteInput`의 `_display is ICombatUnitInspection inspection`(`:1149-1151`, 현강님 줄)은 `_inspection?.InspectUnit(…)`이 된다. 표시 계약 `ICombatBattleDisplay`는 그대로다(설계 P6). |
| Vc.18 | 같은 파일 | `PostBindService`(`:178-207`)에서 `global.UITextPresentation`이 `IUIConfirmationPresentation`이 아니면 `InvalidOperationException`을 낸다(TP-8). `BindServices`(시험 입구)의 `as` 캐스트(`:252`)는 그대로다. |

### Vc-D 쓰지 않는 코드 (V-L17·V-L19)

| Task | 파일 | 내용 |
|---|---|---|
| Vc.19 | `V/Display/Hud/CombatAbandonPanel.cs` (+ `.meta`) | 지운다(Q-V8 A). 코드·씬·프리팹·에셋 참조가 0이다(GUID `d7d223788a7e4a29a74314e2086114df`로 확인). 우리 파일이다. |
| Vc.20 | `V/Display/Unit/FieldUnitPresenter.cs` | `TryGetRelocationBody`(요약 포함 `:138-163`, 호진님 줄)를 지운다. 참조가 0이다. |
| Vc.21 | `V/Display/Unit/CatusCannonWeaponView.cs` | `PoseVersion` 접근자(`:23`)만 지운다. 필드는 안에서 쓴다. |
| Vc.22 | `V/Display/Hud/CombatActionPanel.cs` | 주석 처리된 `returnText`(`:43`)·문구(`:161-163`)와, 그 문구만 위하던 캐시를 지운다: `_hasReturn`·`_shownReturn`(`:55-56`)과 무효화(`:70`)·비교(`:156-160`)·저장(`:164-165`). `ShowReturn`은 `_canReturn`과 누름 가능만 맞춘다. |

### Vc-E 시험

**우리 시험:**
- `T/CombatInteractionTests.cs`
  - 지운다(손패 전용 동작, TP-11): `FieldUnit_DroppedOnHand_RecordsReturnUnit_ButMainCannotReturn`(`:353`), `ClickToPick_ThenClickCell_Places`(`:427`).
  - 입력만 바꾼다(TP-11). 손패 누름 `Input(SelectPressed, Deployment(), HandX(0), HandY)`를 덱 카드 선택으로 바꾼다. 형태는 `:92-93`과 같고, 도우미 `SelectHandSub(facts)`가 `_interaction.SelectDeckUnit(facts, _characters, HandSub의 보유 ID)`를 부른다.
    - 대상 일곱: `WhilePending_DrawPreviewAndCancel_DoNotRecord_AndPendingStays`(`:221`), `Terminal_BlocksEverything_ButCancelStillWorks`(`:287`), `HandDrag_ToCell_RecordsPlaceUnit_AndRestoresDisplay`(`:304`), `Drag_RequestsPlacementPreview_OnlyWhenCellChanges`(`:323`), `SelectCanceled_DuringDrag_RecordsNothing_AndRestoresDisplay`(`:339`), `HoverAnswer_ShowsValidity_AndPlacementTintsGhost`(`:818`), `HoverKind_PlacementDragIsDefault`(`:847`).
    - 덱 선택도 `BeginDrag`를 거치므로 고스트·원본 숨김·미리보기·소리 단언은 같다(`PlacementDragInteraction.cs:270-285`).
  - 가짜 집기의 손패 멤버·`HandSlots`(`:1262·1274-1280`)와 쓰지 않게 된 `HandX`·`HandY`·`DropY`를 지운다. `HandUnit`은 덱 선택 대상이라 남긴다.
- `T/CombatPointerMappingTests.cs`: 손패 부분만 지운다. 나머지 단언은 그대로다.
  - `AfterRelease_LatePlacementFeedback_…`(`:220`)의 손패 카드 블록(`:248-262`)
  - `SamePosition_UsesCurrentCamera_AndKeepsHandAndOutsideBoardMeaning`(`:426`)의 손패 취소 영역 블록(`:432-441`, EventSystem 끄기 포함)
  - `AfterRelease_LateDraftAndReturnCalls_…`(`:535`)의 두 줄(`:549-550`)
- `T/CombatPlacementFeedbackTests.cs`
  - 손패 준비(`:36·39·42·50·92-106`)와 생성자 인자(`:107`)를 지운다.
  - `ChangingOriginalBetweenFieldAndHand_RestoresBoth_…`(`:136`)를 지운다. 손패 카드 숨김을 보던 시험이다.
- `T/CombatDisplayReuseTests.cs`
  - 지운다: `HudCompletion_DefeatByAbandonment_…`(`:724`), `HudCompletion_Victory_…`(`:741`), `HudCompletion_ConfirmsOnce…`(`:798`), `AbandonPanel_ButtonsReachEntries…`(`:758`).
  - 도우미 `CreateFeedbackHud`(`:860-869`)에서 `remainingDrawText`·`completionText`·`completionButton` 준비를 지운다.
  - **이미 깨진 도우미를 고친다(TP-12).** `CreateActionPanel`(`:872-885`)이 지금 없는 필드 `returnText`를 리플렉션으로 넣어 실행에서 `NullReferenceException`이다. `out TMP_Text` 인자와 그 준비를 지우고, 부르는 넷(`:614·645·694·712`)을 맞춘다. 문구 단언(`:619·631·633·704`)과 문구 표시용 줄(`:699`)은 지운다. 버튼 누름 가능 단언은 그대로다.

**다른 사람 시험:**
- 호진님(Q-T2 A): `T/StageMapNodeMovementBootSmokeTests.cs:523`·`T/StageMapNodeMovementBootSmokeTests.Victory.cs:207`의 `Read<Button>(…Hud, "completionButton")` → `Read<Button>(…Completion, "confirmButton")`. 씬에서 같은 버튼(`2100000302`)이라 확인하는 내용은 같다. 둘째 파일은 `Object.FindFirstObjectByType<CombatSceneRoot>().Completion`이다.
- 현강님(Q-T3 b): `T/CombatInteractionTests.cs` `PickAndCancel_ArePlayedOnce_WhenAUnitIsActuallySelected`(`:198`)·`PlacementCompletionAndPresentationCleanup_DoNotPlayCancel`(`:212`)의 손패 누름을 위의 `SelectHandSub`로 바꾼다. 단언은 그대로다.
- 천명님(Q-T3 c): `T/CombatDisplayReuseTests.cs` `Hud_UsesRunGold_AndLegacyDrawReferenceIsOptionalAndHidden`(`:825`)에서 드로우 문구 줄 셋(`:829·832·833`)을 지운다. 골드·참조 검사·Clear 단언은 그대로다.

**손대지 않는 것(컴파일됨):** 계약에서 빠진 멤버를 공개 메서드로 가진 가짜 넷은 그대로 둔다. 명시적 구현이 아니라서다.
- `CombatViewSystemTests:1674-1680`(우리)
- `CombatEnvironmentHoverTests:70-71`(현강님)
- `HojinCombatAttackDragTests:454-455`(호진님)
- `TutorialViewFocusLocatorTests:92`는 `HandCardHoverView` 형식이 남으므로 그대로다.

**동작:**
- 화면은 같다. 손패 집기·손패 영역 회수·HUD 완료는 운영에서 닿지 않았다.
- 운영 조립이 씬의 `unitInfo`를 빠뜨리면 진입 때 이름을 담은 예외가 난다.
- 확인창 서비스가 맞지 않으면 서비스를 받을 때 예외가 난다.

**다른 사람 줄:**
- 현강님: `CombatSceneDisplay` 생성자·완료·정보창 줄(`:58·116-120·128·131-134·219-220·224·269·271·278-286·315-319`), `CombatSceneRoot` 검사, Factory `:78-79·165`, 조정자 `:1150-1151`
- 호진님: `TryGetRelocationBody`
- 천명님: `CombatHud` 드로우 문구 줄
- 민형님: 없음. Factory 변경은 연결 줄로 민형님 공유 문서에 적는다.

## Phase V-d — W1: 최종 승리 화면 (V-H1, Q-V1 A)

**사용자 씬 편집을 먼저 마쳐야 시작한다.** 검사를 먼저 넣으면, 씬을 고치기 전까지 모든 Combat 진입과 Combat 씬을 여는 시험이 연결 검사에서 멈춘다. 씬 편집이 V-a보다 먼저 끝나면 이 Phase를 앞당겨도 된다(다른 Phase와 파일이 겹치지 않음, TP-1). **Q-T3 a 허락도 필요하다.**

| Task | 누가·파일 | 내용 |
|---|---|---|
| Vd.0 | 사용자, `Assets/Scenes/Combat.unity` | `Assets/ProjectL/Prefabs/UI/EndingPanel.prefab`을 **항상 켜진 새 루트 캔버스** 아래에 둔다(현강님 메모 §5 권장: `EndingCanvas`, Screen Space - Overlay, 정렬 7). 그리고 `CombatEndUI.ending`(`CombatEndCanvas`의 `CombatEndUI`)에 연결한다. UISystem은 씬의 `UIBase`를 찾아 등록하므로 따로 등록할 곳은 없다(`UISystem.cs:108`). 다른 작업자(호진님 엔딩 작업)와 겹치는지 먼저 확인한다(설계 W1). |
| Vd.1 | `G/UISystem/Screens/Stage/CombatEndUI.cs` | `ValidateReferences`(`:258-286`)에 `Require(ending, nameof(ending))`를 더한다. 이어서 엔딩이 이 화면 아래에 있으면(`ending.transform.IsChildOf(transform)`) 예외를 낸다. 문구는 "결과 화면이 열리지 않는 최종 승리에서도 열리도록 항상 켜진 캔버스에 두어야 합니다"다(TP-13). |
| Vd.2 | 같은 파일 | 검사를 지난 자리의 null 검사만 지운다: `Present`의 `ending != null &&`(`:90`)와 `if (ending == null) throw …`(`:96-97`). `Present`는 `:87`에서 검사를 먼저 한다. `Dismiss`의 `if (ending != null)`(`:183`)는 남긴다. 연결 전 정리에서도 불리는 자리다. |
| Vd.3 | 시험(Q-T3 a, 현강님) | `T/CombatEndUITests.cs` SetUp(`:40-68`)에 엔딩 가짜를 더한다. `_root` 밖에 꺼 둔 GameObject(RectTransform + `EndingRunSummaryUI`)를 만들어 `Set("ending", …)`으로 넣는다. 꺼 둔 객체라 `Awake`가 돌지 않는다. TearDown(`:71`)에서 함께 지운다. 단언은 그대로다. |

**동작:**
- 최종 승리(지금 데이터에서는 첫 보스 승리)에서 F1 대신 엔딩 요약이 열리고, 확인하면 로비로 간다. 다른 결과는 같다.
- 엔딩이 다시 빠지거나 결과 캔버스 아래로 들어가면, Combat 진입 때 씬 루트 검사(`CombatSceneRoot.cs:95` → `CombatEndUI.ValidateReferences`)가 이름을 담아 멈춘다.
- **재생 확인(사용자):** 보스 승리 → 엔딩 요약 → 확인 → 로비.

**다른 사람 줄:** `CombatEndUI.cs`는 현강님 파일(현강님 254·천명님 25·호진님 17줄)이다. 검사 줄을 더하고 `:90·96-97`(호진님 줄, `cb76bd2c` — V-e 검토로 바로잡음)을 지운다. `EndingRunSummaryUI`(호진님)는 고치지 않는다.

## Phase V-e — 문서·공유 (모두 미전달로 작성, 코드 변경 없음)

| Task | 문서 | 내용 |
|---|---|---|
| Ve.1 | 현강님 10-22 메모 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` §8 | `CombatSceneDisplay` 바꾼 줄, `CombatEndUI` 검사·지운 줄, `CombatSceneRoot` `unitInfo` 필수, 조정자 `CanEnterFromDeployment`·정보창 줄, 가방이 확인창 동안 숨는 차이(Q-V4), 고친 시험(Q-T3 a·b), §5 "고치지 않음"에 해결 표시 |
| Ve.2 | 호진님 10-22 메모 `handoffs/2026-10-22-combat-view-structure-hojin-share.md` §10 | Boot 시험 두 줄(Q-T2), `TryGetRelocationBody` 삭제, 입력 사실·Policy에 더한 사실, 엔딩 씬 연결(엔딩 작업과의 관계), `TutorialViewFocusLocatorTests`는 그대로, §7에 해결 표시 |
| Ve.3 | 천명님 Core 전달 문서 `handoffs/2026-09-24-view-core-data-boundary-handoff.md` §33 | `CombatHud` 옛 드로우 문구 줄 삭제, 고친 시험 줄(Q-T3 c). 요청 없음 |
| Ve.4 | 민형님 Factory 공유 `handoffs/2026-10-15-combat-scene-display-factory-framework-share.md` 새 절 | `CombatSceneDisplayFactory`: 손패 인자 삭제, 정보창·가방 필수 |
| Ve.5 | `viewDocs/combat-view-folder-guide.md:78` | `Hud/` 예시에서 `HandPresenter`를 빼거나 "옛 손패, 쓰지 않음"을 표시한다 |
| Ve.6 | `viewDocs/placement-drag-interaction.md` 머리 | 손패 집기·손패 영역 회수가 걷혔다는 대신함 한 줄(옛 구조 문서) |
| Ve.7 | 설계 문서·CLAUDE.md | 설계 상태 "1라운드 구현 완료", 계획과 다른 점(TP), 이 계획의 실행 기록, CLAUDE.md 상태 줄 |
| Ve.8 | 보고 | 사용자 할 일: 씬에서 옛 손패 `Hand` 객체의 `HandPresenter` 컴포넌트를 뗀 뒤 스크립트 두 파일과 그 시험을 지울지 정하기. Missing Script를 만들지 않는 순서다 |

## Phase V-f — 1라운드 측정 (Q-V2 A, 코드 변경 없음)

| Task | 내용 |
|---|---|
| Vf.1 | 하위 에이전트 3개(SSOT·독립성 / SOLID·FW·있는 기능 / 불필요·우회)가 A 판(지금)과 B 판(V-0)을 함께 매긴다. 어느 쪽이 새것인지는 알리지 않는다. 우리 문서·메모리·git 기록은 주지 않는다. 범위·기준·감점은 10-24 평가 지시문과 같다. |
| Vf.2 | 합칠 때 같은 문제는 한 기준에서만 센다. 설계 §1 목록과 대조해 "해결 / 남음 / 새로 찾음(기존 코드) / 새로 생김(우리 변경)"으로 나눈다. 중간 이상 주장은 코드로 직접 대조한다. |
| Vf.3 | 결과를 설계 문서 새 절과 CLAUDE.md 상태 줄에 적는다. 2라운드 상세 설계는 그 결과를 보고 쓴다(설계 P7). |

## 계획에서 정한 것 (사용자 확인 대상)

- **TP-1 Phase 순서는 W3 → W2 → W4 → W1이다.** 설계 §2 표의 순서(W1 먼저)와 다르다.
  - W1은 사용자 씬 편집이 먼저라서 마지막에 둔다. 편집이 먼저 끝나면 앞당겨도 된다(겹치는 파일 없음).
  - W2와 W4는 둘 다 `CombatViewSystem.cs`를 고친다. 그래서 차례로 한다.
- **TP-2 대여 정리의 표준 순서는 지금 `Bind` 순서다.** `OnDisable`·`ResetForPool`의 순서가 그쪽으로 바뀐다. 칸들은 서로 다른 대상이다(자세·크기는 Transform의 다른 값, 플래시·투명도는 외형 두 층이고 마지막 층이 끝날 때만 색을 되돌림, 클립, HUD). 구현 때 자세 복원과 크기 복원이 같은 Transform 값을 쓰지 않는지 확인하고, 겹치면 멈추고 묻는다.
- **TP-3 강조 층 쪽 이름은 `FillsCell`·`FillColorOf`다.** 채우기 객체 이름·순서는 지금과 같다(시험 `FillNames` 그대로).
- **TP-4 `GetDesiredLayer`도 "입력 닫힘"을 본다.** 그래서 `SyncInputLayer`의 정지 특례가 없어진다. 정지 때 입력 표시 정리가 한 번 더 불리지만, 호스트가 이미 정리한 뒤라 결과는 같다.
- **TP-5 호스트 콜백 하나(`OnInputClosed`)를 제출 정지와 포기 요청이 함께 부른다.** 포기 입구의 정리 인자는 지운다. 정리 순서는 "깃발 → 정리"가 된다.
- **TP-6 `CombatActionButtons.Read`에서 정지 인자를 지운다.** 사실 묶음이 같은 값을 가지므로 둘을 두지 않는다. 우리 시험 두 곳의 호출을 맞춘다.
- **TP-7 V-M12의 범위를 좁힌다.**
  - 이번에 하는 것: 표시 생성자의 선택 인자 넷을 필수로, 씬 루트에 `unitInfo` 필수 검사, Factory 가방 삼항 삭제, HUD 완료 대체 경로 삭제.
  - 시험 입구의 null 허용(`BindServices`·`CombatDisplayServices` 선택 인자, 가짜 루트용 `?.`, 입력 Factory의 알림 생략)은 2라운드 W6(주입)에서 함께 본다.
  - 운영 입구는 이미 서비스가 없으면 예외라 조용히 빠지는 길이 없다.
  - 그래서 V-M12는 중간에서 낮음 하나로 남는 것으로 셌다(§0).
- **TP-8 확인창 캐스트 검사는 운영 입구(`PostBindService`)에만 둔다.** 시험 입구는 그대로다.
- **TP-9 유닛 정보 창구는 `AttachCreated`의 셋째 선택 인자로 넘긴다.** 운영은 같은 표시 객체를 넘긴다. 표시 계약은 그대로다(설계 P6).
- **TP-10 손패 스크립트 두 파일과 그 자체를 보는 우리 시험(`HandCardHoverTests`, `CombatDisplayReuseTests`의 `HandCards_…` 둘)은 남긴다.**
  - 설계 §3 W4의 "`HandCardHoverTests`는 지운다"와 다르다.
  - 이유: 파일이 남는 동안 이 시험들은 깨지지 않는다(CLAUDE.md "깨지는 곳만").
  - 사용자가 씬 컴포넌트를 뗀 뒤 파일과 함께 지운다. 그때까지 쓰는 곳 없는 파일 둘은 낮음 하나로 셌다.
- **TP-11 손패로 Drag를 시작하던 시험은 덱 카드 선택 입구로 입력만 바꾼다.** 우리 일곱 + 현강님 둘(Q-T3 b)이다. 운영 배치 입구가 덱 카드라 시험이 운영 길을 보게 된다. 손패 전용 동작만 보던 우리 둘은 지운다(손패 영역에 놓아 회수, 손패 클릭 집기). 다른 시험의 손패 부분(포인터 변환 셋, 배치 피드백 하나)도 지운다.
- **TP-12 이미 깨진 우리 시험을 찾았다.** 행동 패널 복귀 문구를 주석으로 걷을 때(`CombatActionPanel`, 우리 커밋) 시험 도우미 `CreateActionPanel`은 고치지 않았다. 그래서 이 도우미를 쓰는 시험 넷(`:614·645·694·712`)이 지금 실행에서 `NullReferenceException`이다. V-c에서 남은 잔재(V-L19)와 함께 고친다. 문구 단언은 지우고, 버튼 단언은 남긴다.
- **TP-13 W1에 "엔딩은 결과 화면 밖" 검사를 더한다.** 설계는 `Require(ending)`만 적었다. 현강님 메모 §5 권장이다. 결과 캔버스 아래로 되돌리면 다시 막히는 결함을 진입 때 잡는다.
- **TP-14 측정 기준 판은 커밋으로 고정하기를 권한다(A).** 커밋하지 않으면 범위 파일을 스크래치패드에 복사한다(B).
- **TP-15 옛 드로우 문구(천명님 줄)를 이번에 지운다.** 설계 V-L19의 범위다. 천명님 시험 세 줄이 닿으므로 Q-T3 c 허락이 필요하다. 허락이 없으면 코드를 남긴다.

## 결정 대기 — 다른 사람 시험 (설계에 없던 것)

| # | 시험 | 왜 | A(권장) | B |
|---|---|---|---|---|
| **Q-T3 a** | 현강님 `T/CombatEndUITests.cs` SetUp·TearDown | `ending` 필수 검사(Vd.1)를 넣으면 이 시험 묶음이 SetUp의 검사에서 모두 실패한다 | 엔딩 가짜를 결과 화면 밖에 만들어 넣는다(단언 그대로) | 검사를 `CombatEndUI` 대신 씬 루트(`CombatSceneRoot.FindMissingReference`)에 둔다. 시험은 그대로지만, 결과 화면의 참조 목록이 두 곳으로 나뉜다 |
| **Q-T3 b** | 현강님 `T/CombatInteractionTests.cs:198·212` | 손패 집기 분기를 걷으면 두 시험의 누름이 아무것도 집지 못해 실행에서 실패한다 | 덱 카드 선택 입력으로 바꾼다(단언 그대로) | 손패 집기 분기를 남긴다(V-M11이 일부 남음) |
| **Q-T3 c** | 천명님 `T/CombatDisplayReuseTests.cs:829·832·833` | 옛 드로우 문구 필드를 지우면 리플렉션 읽기가 실행에서 실패한다 | 드로우 줄 셋을 지운다(골드 단언 그대로) | 옛 드로우 문구 코드를 남긴다(TP-15 취소) |

## 커밋 묶음 (사용자가 함)

- **V-0:** 지금 작업 트리(C 단계·골격 점검). 남은 커밋 안내는 CLAUDE.md 상태 줄에 있다.
- **V-a:** `FieldUnitView.cs`, `FieldHighlightLayers.cs`, `FieldOutlineRenderer.cs`
- **V-b:** `CombatInputFacts.cs`, `CombatInputPolicy.cs`, `CombatActionButtons.cs`, `CombatViewProgress.cs`, `CombatAbandonController.cs`, `CombatViewSystem.cs`, `T/CombatInteractionTests.cs`
- **V-c:**
  - 코드: 위 Vc 표의 파일들
  - 시험: 시험 여섯 파일(호진님 Boot 둘 포함)
  - 지운 파일: `CombatAbandonPanel.cs` + `.meta`
- **V-d:** `CombatEndUI.cs`, `T/CombatEndUITests.cs`, `Assets/Scenes/Combat.unity`(사용자 씬 편집)을 한 커밋에 넣는다. 씬 없이 코드만 들어가면 Combat 진입이 멈춘다.

## 실행 기록

- **확인(2026-10-24 사용자 "모두 A로 하고 커밋했어, V-0부터 진행해"):** TP-1~TP-15 그대로, Q-T3 a·b·c 모두 A(허락), TP-14 A(커밋).

### Phase V-0 — 2026-10-24, 기준 판 고정(코드 변경 없음)

- B 판 = 사용자 커밋 `8ed3f8cb`("[용근] 전투 View 골격 개선 1차", 70파일)다. C 단계·골격 점검·View 설계·이 계획이 들어 있다. 작업 트리는 그 위에서 깨끗하게 시작했다(`git status` 0줄).

### Phase V-a (W3) — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것(3파일, 모두 우리 줄):**
- `V/Display/Unit/FieldUnitView.cs`
  - private `ReleasePresentation()`을 만들었다. 순서는 이동 투명도 → 자세 → 크기 → 플래시 → 클립 → 예상 체력이다(검토 반영 뒤, 아래 "계획과 다른 점").
  - `Bind`·`OnDisable`·`ResetForPool`이 이것을 부른다. `OnDisable`은 `SyncHudActive` → 정리 → 클립 멈춤 순서다.
  - `ResetForPool`은 `EntityId`를 비우기 전에 정리하고(주석 한 줄), 그 뒤 Ballista 무기·HUD를 지금처럼 정리한다.
- `V/Display/Field/FieldHighlightLayers.cs`: `FillsCell`·`FillColorOf`를 더했다(TP-3). 영향 칸이 공격 호버 상태 색을 쓰는 이유도 여기로 옮겼다.
- `V/Display/Field/FieldOutlineRenderer.cs`
  - `FillKind` 열거를 지웠다.
  - `CreateFills`는 배치 구역 → `DrawOrder` 가운데 `FillsCell` 층(낮은 것부터) → 호버 순서로 칸을 만든다. `FillSlot`이 그 층(`Layer`)을 가진다.
  - 고르기 `TryGetFill`, 색 `FillColor(int)`, 상수 `PlayerZoneFill`, `HoverFill`을 새로 두었다.
  - 머리 요약·필드 주석을 고쳤다. 민형님 줄에는 닿지 않았다.

**계획과 다른 점:**
- Va.7은 "`DrawOrder`를 위에서부터 돈다"고 적었다. 구현은 `DrawOrder`에서 만든 층 채우기 칸을 뒤에서부터 돈다. 칸 순서가 `DrawOrder`와 같으므로 결과도 같다. 칸마다 층 열 개를 돌지 않는다.
- TP-2는 "옛 `Bind` 순서"였다. 검토 반영으로 이동 투명도 해제를 맨 앞으로 옮겼다(옛 `OnDisable`·`ResetForPool`의 자리). 그래서 `Bind`에서만 투명도 해제가 대여 넷 앞으로 왔다. 투명도는 대여 칸이 아니라 취소 콜백이 없고, 플래시와는 외형의 두 층이라 최종 외형이 같다.
- TP-2의 "칸들은 서로 다른 대상이라 순서는 결과에 영향이 없다"는 복원 결과에만 맞는다. 대여 칸 `Reset`은 취소 콜백을 바로 부르고(`UnitPresentationLeaseSlot.cs:66-67`), 조각 실행의 콜백이 같은 실행이 빌린 다른 칸을 되돌린다. 그래서 대여 넷의 순서는 지켜야 한다. 시험 `CombatUnitPresentationWindowTests.FieldUnitView_Disabled_CancelsPoseBeforeScale`·`…RentedToOtherUnit_CancelsPoseBeforeScale`이 "자세 → 크기"를 지킨다. 지금 순서는 이를 지킨다.

**검토(새 에이전트, 읽기 전용, 2026-10-24):** 높음 0 · 중간 1 · 낮음 5.
- 반영 — 중간: `ReleasePresentation` 주석 "순서는 결과에 영향이 없다"가 취소 알림 순서를 빠뜨렸다. 고쳤다. 에이전트가 근거로 든 "설계 B14 O4"는 다른 규칙(포효 작아지기)이라, 근거를 위 시험 둘로 바꿨다.
- 반영 — 낮음 2: `OnDisable`에서 투명도 해제가 대여 정리 뒤로 밀려, 취소 콜백이 예외를 내면 건너뛰었다. 투명도를 맨 앞으로 옮겼다(위 "계획과 다른 점").
- 반영 — 낮음 5: 상수 `PlayerZoneFill`을 인스턴스 필드 사이에서 다른 상수 옆(`FieldOutlineRenderer.cs:39-40`)으로 옮겼다.
- 기록만:
  - 낮음 1: 크기 대여의 취소 콜백이 `OnDisable` 안(Unity가 예외를 로그로만 남김)에서 `ResetForPool` 안으로 옮겨 왔다. 그래서 콜백의 정리 예외가 `ResetForPool` 밖으로 전파된다. 자세 칸은 예전부터 그랬고, 정리 실패일 때만이다. 크기 대여를 쓰는 곳은 포효 조립 하나(`CombatRoarEventHandler.cs:98`)라 고친 결함은 실제로 있던 것이다.
  - 낮음 3: Animator가 표시 루트 자신에 있으면 클립 채널의 휴식 Transform 목록에 체력 글자가 들어가 자세 복원과 대상이 겹친다. 자세 → 클립 순서는 예전 세 경로와 같아 동작 차이는 없다.
- 받지 않음 — 낮음 4: "바뀐 세 파일만 CRLF"는 사실이 아니다. 바꾸지 않은 파일(`FieldCellFill.cs`)도 작업 트리에서 CRLF다(`core.autocrlf=true`).
- 대조 확인(검토): 채우기 이름·생성 순서·칸 우선순위·색, 채우기가 없을 때 경로, C# 9 문법, `ResetForPool`·`Bind`를 부르는 기존 시험 결과가 모두 같다.

**확인한 사실:**
- 자세 복원은 위치·회전·체력 글자 위치(`FieldUnitView.cs` `PresentationUnitPose.RestorePose`)만, 크기 복원은 `localScale`(`RestoreScale`)만 쓴다. 겹치지 않는다(TP-2 조건 충족).
- 대여 칸 `Reset`(`UnitPresentationLeaseSlot.cs:53-68`)은 복원이 아무것도 하지 않아도 칸을 비우고 취소 콜백을 부른다. 그래서 옛 `ResetForPool` → `OnDisable` 흐름에서도 크기 칸은 비워졌고, 복원만 `IsCurrent`(EntityId)에 걸려 건너뛰었다. 이번 변경은 그 복원을 실제로 하게 하는 것뿐이다. 표시가 이미 꺼져 있어 `OnDisable`이 돌지 않던 경우에는 칸이 다음 `Bind`까지 남았는데, 이제는 바로 비운다.
- 채우기 이름·순서는 옛 열거와 같다(`MoveRange`·`AttackRange`·`EnemyAttackRange`·`HazardRange`·`Affected`가 `DrawOrder` 낮은 것부터의 순서와 같음).
- 범위 색은 `_palette.TryGet(…)`이 옛 `_palette.MoveRange` 등과 같은 값을 돌려준다(`FieldHighlightPalette.cs:83-104`).
- 시험: 바꾼 것 없음(Va.10).
- **Unity 확인 문제 없음(2026-10-24 사용자).**

### Phase V-b (W2) — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것:**
- `V/Input/CombatInputFacts.cs`: 생성자 끝 선택 인자 셋(`isSubmissionSuspended`·`isAbandonRequested`·`isModalOpen`)과 속성 셋.
- `V/Input/CombatInputPolicy.cs`
  - `IsInputClosed`(정지 ∨ 포기 요청), `IsInputBlocked`(닫힘 ∨ 확인창).
  - `PassesCommon`·`GetDesiredLayer`가 `IsInputClosed`를 본다.
  - `CanSubmit`의 결과 순번 비교가 `CombatProgressFacts.IsUnpresented(P, S)`다(`using …LifeCycles.Combat`).
  - 요약 둘(`CanSubmit`, `CanShowEnvironmentHover` remarks)을 고쳤다.
- `V/Input/ActionButtons/CombatActionButtons.cs`: `Read(facts, canEnterAttack, isAttackActive)`. 정지 인자를 지우고 `IsInputBlocked`를 본다.
- `V/Input/CombatViewProgress.cs`: 호스트 콜백 `OnInputClosed`(옛 `OnSubmissionsSuspended`). `SuspendSubmissions`와 `MarkAbandonRequested`가 부른다.
- `V/Input/UiEntry/CombatAbandonController.cs`: 정리 인자·필드·호출을 지웠다.
- `V/CombatViewSystem.cs`
  - 포기 입구 생성 인자에서 정리 인자를 뺐다.
  - `ReadFacts`가 사실 셋을 채운다. 호진님 줄 뒤에 더했다.
  - `SyncInputLayer`의 정지 갈래를 지웠다.
  - 호스트 구현의 이름과 주석을 바꿨다. `_deploymentConfirmation?.Close()`는 현강님 줄 그대로다.
  - `CanReceiveDeckInput`는 `IsInputReady && !IsInputBlocked(ReadFacts())`다.
  - `SyncActionButtons`는 `blocked` 하나를 BattleChanger·가방·행동 버튼에 넘긴다.
  - `CanEnterFromDeployment`의 정지 검사를 지웠다. `StopInteraction` 주석 한 줄을 고쳤다.
- 시험(우리 줄): `T/CombatInteractionTests.cs`
  - `CombatActionButtons.Read` 호출 열에서 둘째 인자를 뺐다.
  - 정지 사례는 정지 사실이 든 facts로 같은 단언을 한다.
  - `WithInput`에 선택 인자 `suspended`를 더했다.

**계획과 다른 점:**
- **Vb.6 앞쪽(`CanEditPath`의 겹친 `!facts.IsPresenting` 지우기)은 하지 않았다.** 우리 시험 `CombatInputPolicyTests.CanEditPath_CombinesMoveSubmitWithPresentationAndQueue`의 `"presenting"` 사례가 순번을 같게(1, 1) 두고 `isPresenting`만 켠다(`:208-215`). 그래서 지우면 그 사례가 실행에서 실패한다. 계획 Vb.16에 없던 시험 변경이라 하지 않고 사용자에게 묻는다(운영에서는 `IsPresenting` = P &lt; S라 결과가 같다). V-L3 중 `CanSubmit`의 순번 비교는 고쳤다.
- Vb.14는 "사실을 먼저 읽고 `IsInputClosed`를 본다"였다. 구현은 그 검사도 두지 않는다. 뒤에서 부르는 `CanSubmit`의 공통 판정(`PassesCommon`)이 이미 `IsInputClosed`를 보기 때문이다. 결과는 같다.

**검토(새 에이전트, 읽기 전용, 2026-10-24):** 높음 0 · 중간 0 · 낮음 7.
- 반영 — L1: 포기 입구는 이제 깃발을 먼저 세우고 호스트를 부른다. 그래서 호스트 정리가 예외를 내면 요청이 선 채 `false`가 된다. 요약을 고쳤다(`CombatAbandonController.TryRequestAbandon`). 곧 F1이고 `SuspendSubmissions`와 같은 꼴이다.
- 반영 — L3(코드 쪽): `Tick` 끝 주석과 `CancelInputPresentation` 주석이 "포기 요청은 Tick 끝에서 맞춘다"고 했다. 지금은 호스트가 바로 숨기므로 고쳤다. 시험 `CombatViewSystemTests`의 `…ClearedAtTickAfterAbandonRequest`(`:1020`) 이름·주석은 단언이 그대로 성립하므로 고치지 않았다(깨지지 않는 시험은 손대지 않음).
- 반영 — L4: `CanSubmit` 요약의 "(S == P)"를 "P &lt; S 아님(`IsUnpresented`)"으로 고쳤다. P &gt; S는 `CombatViewRefresh.FindViolation`(`:51`)이 계약 위반으로 보고한다(확인).
- 기록 — L2: 위 "동작 차이 4"의 "정리(`CancelInputs`)는 그 깃발을 읽지 않는다"는 틀렸다.
  - 경로: `CancelInputs` → `CombatPathInput.CancelInputs` → `KeepRangeForMoveMode`(`MovePath/CombatPathInput.cs:508-516`) → `CanEditPath` → `PassesCommon` → `IsInputClosed`.
  - 그래서 이동 모드 범위가 포기 확인(·제출 정지) 순간 바로 지워진다. 예전에는 다음 Poll이 지웠고, 최종 상태는 같다. 차이 1에 포함된다.
- 기록 — L5: 차이 2의 부수 효과다. 확인창이 열리면 `PackUI.Hide`(`PackUI.cs:108-116`)가 펼침을 접고 설명을 숨긴다. 그래서 "아니오" 뒤 가방은 접힌 채 다시 보인다. 현강님 공유(Ve.1)에 적는다.
- 기록 — L6: `SyncEnvironmentHover`가 사실을 두 번 읽는다(자기 `ReadFacts`와 `CanReceiveDeckInput` 안). 비용은 작다. W6(조정자 나누기)에서 입력 표면 동기화를 뺄 때 함께 본다.
- 기록 — L7: 수신 창구는 계속 자기 확인창 조건(`() => IsModalOpen`, `CombatViewSystem.cs:257`)을 쓴다. 확인창 동안에도 FocusLost·SelectCanceled는 통과해야 하고, 닫힘은 Unbind가 맡기 때문이다. 정당한 예외 입구로 둔다. 설계 W2에 한 줄 더한다(Ve.7).
- 검토가 확인한 것:
  - 남은 직접 조합은 L7뿐이다.
  - 옛 이름·인자·서명을 쓰는 곳은 운영·시험 모두 0이다. `IsUnpresented` 인자 순서도 맞다.
  - 호스트는 두 번 불러도 결과가 같다(Close·Unbind·ClearLayer 무동작). Option 중 포기의 Layer 처리도 맞다.
  - 정지 갈래를 지워도 Gate 대기는 보존된다. `ReadFacts`는 null 상태에서 불리지 않고 재귀도 없다.
  - 실행 결과가 바뀌는 기존 시험을 찾지 못했다. P &gt; S를 만드는 시험은 View 정책을 쓰지 않는 Core 시험뿐이다.
- **Unity 확인 문제 없음(2026-10-24 사용자). `CanEditPath`의 겹친 검사는 A(그대로 두고 2라운드에서 사실 묶음의 "재생 중"과 함께 봄, 사용자 결정).**

### Phase V-c (W4) — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것 — 운영 코드:**
- Vc-A 옛 손패
  - `ICombatPointerPicker`: 손패 멤버 둘을 지웠다.
  - `CombatPointerMapper`: 손패 필드·인자·메서드를 지웠다. 취소 영역은 `HitsUi`만 본다. 쓰지 않게 된 `using` 둘도 지웠다.
  - `CombatPlacementFeedback`: 손패 인자와 손패 숨김을 지웠다. 원본 숨김은 필드만 한다.
  - `CombatSceneDisplay`: 손패 필드·인자, 옛 손패 그리기(현강님 줄), 해제 목록 둘, 손패 메서드 둘을 지웠다.
  - `PlacementDragInteraction`: 손패 집기와 손패 영역 회수를 지웠다. 포인터로 집는 것은 필드 유닛뿐이다.
  - `CombatSceneRoot`: `hand` 칸·접근자·검사를 지웠다.
  - Factory: 손패 지역 변수·`Configure`·인자 셋을 지웠다.
- Vc-B HUD 완료 대체 경로·필수 인자
  - `CombatHud`: 완료 확인 전부와 옛 드로우 문구(천명님 줄, Q-T3 c)를 지우고 다시 썼다. `MovedFrom`·`FormerlySerializedAs`는 그대로다.
  - `CombatSceneDisplay`: 생성자 인자 넷을 필수로 바꿨다. 완료는 결과 화면만 부르고, `_pack?.`·`_unitInfo?.`·`_unitInfo == null`(현강님 줄)을 지웠다.
  - `CombatSceneRoot`: `unitInfo` 필수 검사를 더했다.
  - Factory: `root.UnitInfo.Configure`, 가방 삼항을 지웠다.
- Vc-C
  - `CombatViewSystem.AttachCreated(created, root, inspection = null)`. 운영 연결은 만든 표시를 셋째 인자로도 넘긴다.
  - `_inspection`은 인수 뒤에 두고, `Detach`가 `_display`와 함께 비운다.
  - `RouteInput`은 `_inspection?.InspectUnit`이다(현강님 줄의 형식 검사 대신).
  - `PostBindService`는 텍스트 서비스가 확인창 기능이 아니면 예외를 낸다(운영 텍스트 서비스 `UITextPresentation`은 구현함, 확인).
- Vc-D
  - `CombatAbandonPanel.cs` + `.meta`를 지웠다. GUID 참조 0을 다시 확인했다.
  - `FieldUnitPresenter.TryGetRelocationBody`(호진님 줄)를 지웠다.
  - `CatusCannonWeaponView.PoseVersion`을 지웠다.
  - `CombatActionPanel`: 주석 처리된 `returnText`와 문구, 그 문구만 위한 캐시를 지웠다. 주석 끝 줄도 지웠다.
- 주석: 손패 화면 영역을 가리키던 우리 주석 다섯 곳(`CombatCursorSnap` 둘, `ICombatPointerMapping`, `ICombatInteractionFeedback`, `CombatInteraction` 배치 Drag 요약, `ICombatBattleDisplay`)과 조정자 `AttachCreated` remarks.

**바꾼 것 — 시험:**
- `T/CombatInteractionTests.cs`
  - 손패 누름 아홉 곳(현강님 둘 포함, Q-T3 b)을 `SelectHandSub(facts)`로 바꿨다. 이 도우미는 덱 카드 선택이다.
  - `FieldUnit_DroppedOnHand_…`를 지웠다.
  - 가짜 집기의 손패 멤버와 `HandX`·`HandY`·`DropY`·`HandSlots`를 지웠다.
- `T/CombatPointerMappingTests.cs`: 손패 블록 셋을 지웠다(나머지 단언 그대로).
- `T/CombatPlacementFeedbackTests.cs`: 손패 준비를 지우고 `ChangingOriginalBetweenFieldAndHand_…`를 지웠다.
- `T/CombatDisplayReuseTests.cs`
  - HUD 완료 시험 셋과 포기 패널 시험을 지웠다.
  - HUD 도우미에서 걷은 칸 준비를 지웠다.
  - 천명님 시험의 드로우 줄 셋을 지웠다(Q-T3 c).
  - 행동 패널 도우미를 고쳤다(아래 TP-12).
- 호진님 Boot 시험 두 줄(Q-T2): `…Completion, "confirmButton"`.

**계획과 다른 점:**
- **`ClickToPick_ThenClickCell_Places`는 지우지 않고 필드 유닛 입력으로 바꿨다**(계획 Vc-E·TP-11은 지움). "제자리 클릭은 집기만 하고 다음 클릭에 놓는다"는 필드 유닛 집기에서도 살아 있는 동작이라, 남는 내용을 보는 시험이다(Q-T1). 입력만 바꿨다: FieldSub 칸 누름, 1px 옆 뗌, (5, 1) 누름·뗌.
- **TP-12 보완:** 행동 패널 도우미는 없는 `returnText` 말고도, 지금 필수인 `attackImage`·`attackSprite`·`moveSprite`를 넣지 않고 있었다. 그래서 `SetAvailable`에서도 예외가 났다. 이 필수 검사도 우리 커밋("CombatActionPanel Action 버튼 추가")이 넣었다. 도우미가 세 참조를 채우게 했다(`Texture2D.whiteTexture` 스프라이트, `MainCharactorUITests` 선례). 단언은 그대로다.
- **Vc.12의 `_unitHudLayer == null`(`:617`)과 `ClearUnitInfo`의 `_unitInfo != null`은 남겼다.** 정리 경로는 씬이 내려가며 이미 파괴된 Unity 객체를 만날 수 있어서, Unity의 null 비교가 그 경우를 막는다. 생성자에서는 필수로 받는다. 주석 한 줄로 이유를 적었다.

**확인한 사실:**
- 씬의 옛 손패 `CardRoot`는 RectTransform뿐이다. 그리기를 멈춰도 보이는 것이 없다(계획 Vc-A).
- 결과 확인 버튼(`2100000302`)은 평소 꺼진 `CombatEndCanvas` 아래 `CombatCompletion`에 있다. HUD가 더는 이 버튼을 끄지 않아도 결과 화면이 열 때 직접 켜므로(`CombatEndUI.cs:157-158`) 보이는 차이가 없다.
- 덱 카드 선택도 `BeginDrag`를 거친다(고스트·원본 숨김·소리). Router는 누름 상태를 기억하지 않아, 덱 선택 뒤 뗌만으로 놓는다(`CombatInputRouter.cs:237-240`). 그래서 바꾼 시험의 단언은 그대로 성립한다.
- 시험에 남은, 걷은 멤버 참조는 0이다. 손패 멤버를 공개 메서드로 가진 가짜 넷은 컴파일된다.

**검토(새 에이전트, 읽기 전용, 2026-10-24):** 높음 0 · 중간 1(절차) · 낮음 4.
- **중간 — 사용자에게 물음:** 지운 `CombatDisplayReuseTests.HudCompletion_DefeatByAbandonment_…`·`HudCompletion_Victory_…`는 본문이 우리 줄이다. 하지만 `[TestCase(…, "확인")]` 네 줄(기준 판 `:722-723·739-740`)은 현강님 커밋 `8c34b864`("전투 결과 UI를 위한 사전작업")이다(blame 확인). 계획은 이 둘을 "우리 시험"으로 셌고, Q-T3 허락에 없었다. 이 시험들은 걷은 HUD 완료(`CombatHud.ShowCompletion`)를 봐서 그대로 둘 수 없다. 그래서 삭제를 사후 확인받고 현강님 메모(Ve.1)에 적는다.
- 반영 — 낮음:
  - `CombatHud` 머리 주석의 "견적 문구"를 지웠다.
  - 남긴 `HandPresenter.cs` 머리에 "쓰지 않음, 컴포넌트를 뗀 뒤 지움" 문단을 더했다(우리 파일).
  - 시험 스프라이트를 TearDown에서 지우게 했다(`CreateSprite`·`_sprites`).
  - `HideCompletion`에도 다른 정리처럼 Unity null 검사를 되돌렸다(정리 목록에서 불리는 자리).
  - `FakePicker` 끝 빈 줄과 `CombatActionPanel`의 쓰지 않는 `using …Contracts.Common`을 지웠다.
- 기록만:
  - 다른 영역·다른 사람 줄이라 고치지 않았다.
    - 현강님 `G/…/DeckButtonUI.md:29` "CombatSceneDisplay가 빈 목록·숨김을 전달"은 이제 사실이 아니다.
    - 호진님 Boot 시험의 Record 문구 "actual HUD completionButton"(`:534`)과 Victory의 "실제 HUD"·"actual HUD" 문구(`:198·214`)가 낡았다. Q-T2 허락은 `Read` 두 줄뿐이다.
  - 시험 이름이 내용과 조금 어긋난다(`SamePosition_…KeepsHand…`, 천명님 `Hud_UsesRunGold_AndLegacyDraw…`, `ActionPanelReturn_…Quote…` 둘). 깨지지 않는 시험은 손대지 않는 규칙대로 이름은 두었다.
- 검토가 확인한 것:
  - 걷은 멤버의 남은 참조는 운영·시험 모두 0이다.
  - 바꾼 시험 아홉의 단언 결과는 같다(옛 손패 누름 자리는 칸 밖이라 포인터·호버 차이가 결과에 안 닿음).
  - 씬 `unitInfo`가 연결돼 있다. `CardRoot`·`HandPresenter`는 아무것도 하지 않는다.
  - 결과 확인 버튼은 꺼진 결과 캔버스의 자식이다.
  - `_inspection` 수명이 맞다. `PostBindService` 검사에 걸리는 시험은 없다. `UITextPresentation`이 null인 가짜 Global을 쓰는 시험은 이 시스템을 묶지 않는다.
- 다른 사람 줄(공유 메모용, 검토 대조):
  - 현강님: Factory `:79·165`, `CombatSceneDisplay` `:117·119-120·128·131-134·219-220·224·279-280·283-286·317`, `CombatHud` `:58`, 조정자 `:1151`, 시험 `CombatInteractionTests:202·214`(허락), `CombatDisplayReuseTests:722-723·739-740`(사후 확인).
  - 천명님: `CombatHud`의 드로우 문구·using 셋·`OnEnable` 블록·`_shownRunGold` 위치, 시험 `CombatDisplayReuseTests:829·832·833`(허락).
  - 호진님: `FieldUnitPresenter:138-164`, Boot 시험 `:523`·Victory `:207`(허락).
  - 민형님: Factory 연결 줄.
- **Unity 확인 문제 없음(2026-10-24 사용자). 현강님 `[TestCase]` 줄이 든 HUD 완료 시험 둘의 삭제는 A(확정, 현강님 메모 Ve.1에 적음, 사용자 결정).**

### Phase V-d (W1) — 2026-10-24, 대기(Vd.0 사용자 씬 편집 전)

- 사용자 "V-d 진행해" 때 디스크의 `Assets/Scenes/Combat.unity`는 아직 `ending: {fileID: 0}`(`:11752`)이었다. 씬에 `EndingPanel`·새 캔버스도 없었다.
- 계획대로 Vd.1~Vd.3 코드는 넣지 않고 씬 편집 안내를 드렸다. 검사를 먼저 넣으면 Combat 진입과 Combat 씬을 여는 시험이 연결 검사에서 멈춘다.
- 확인한 사실: UISystem은 꺼진 UI까지 찾아 등록한다(`UISystem.cs:107-109`, `FindObjectsInactive.Include`). 그래서 엔딩 패널을 씬에 두기만 하면 따로 등록할 곳이 없다.
- 사용자 질문 "EndingPanel이 승리·패배 모두 지원하나": 아니다. 최종 승리 전용이다.
  - 일반 승리는 결과 화면의 승리 패널, 패배·포기는 결과 화면의 패배 패널, 최종 승리는 엔딩이 보여 준다.
  - 항목도 다르다. 패배 패널은 스테이지·Run 명성·Run 사용 골드를, 엔딩은 Run 사용 골드·Run 명성·처치 수를 보여 준다(`CombatEndUI.cs:92-170`, `EndingRunSummaryUI.cs:47-64`).

**2026-10-24 사용자 "씬 편집 저장했어, V-d 진행해" — 코드 완료, 컴파일 미확인(MSBuild 안 돌림, 새 `[Test]` 없음):**
- 씬 확인:
  - `ending`이 `EndingPanel` 프리팹 인스턴스(`&1470310029`)에 연결됐다.
  - 그 부모는 결과 캔버스 밖 새 루트 `EndingCanvas`(Overlay, Sort Order 7)다.
  - **하지만 `EndingCanvas`가 꺼진 채(`m_IsActive: 0`) 저장돼 있다.** `EndingRunSummaryUI.Open`은 부모 계층이 꺼져 있으면 예외를 낸다(`:99-100`). 그래서 이대로는 최종 승리 순간 여전히 멈춘다. 사용자에게 켜 달라고 요청한다.
  - 패널은 `Awake`의 `EnsureInitialized`가 스스로 끄므로(`:189-198`), 캔버스를 켜도 처음에 보이지 않는다.
- Vd.1 `CombatEndUI.ValidateReferences`: `ending` null 검사와 "결과 화면 밖" 검사(`IsChildOf(transform)`이면 예외)를 맨 앞에 더했다.
  - **계획과 다른 점:** 기존 도우미 `Require`는 "결과 화면 안에 있어야 한다"까지 검사한다(`:293-299`). 그래서 엔딩에는 쓰지 않고 null 검사를 따로 썼다. 계획은 `Require(ending)`였다.
- Vd.2 `Present`의 `ending != null &&`와 `if (ending == null) throw`를 지웠다(호진님 줄 `cb76bd2c`, 처음에 현강님 줄로 잘못 적었다 — V-e 검토). `Dismiss`의 null 검사는 정리 경로라 남겼다.
- Vd.3(현강님 시험, Q-T3 a): `T/CombatEndUITests.cs`
  - SetUp에서 `_root` 밖에 꺼 둔 엔딩 가짜(`EndingPanel`)를 만들어 `ending`에 넣었다. `BindUIService`의 검사보다 앞이다.
  - TearDown이 함께 지운다. 단언은 그대로다.
- `CombatEndUI`가 붙은 씬·프리팹은 `Combat.unity`뿐이다(GUID `5933938a…` 확인). `CombatEndUI`를 직접 만드는 시험도 이 파일뿐이다.
- **사용자 씬 다시 저장(2026-10-24 "EndingCanvas 켜고 저장했어"):** 캔버스 이름이 `BossEndingCanvas`가 됐다. 이 문서의 "EndingCanvas"는 이 캔버스를 가리킨다.
  - 캔버스는 켜짐(`m_IsActive: 1`, `Combat.unity:4778`)이다.
  - 엔딩 패널 인스턴스는 그 아래에서 꺼짐 덮어쓰기(`m_IsActive: 0`)다. 정상이다. `Open`이 켜고, 부모가 켜져 있어 열린다.
  - `ending` 연결은 그대로다.

**검토(새 에이전트, 읽기 전용, 2026-10-24):** 높음 0(지금 판 기준) · 중간 1 · 낮음 4.
- 처음 읽은 판(캔버스 꺼짐)이었다면 최종 승리 때 `EndingRunSummaryUI.Open:99`에서 또 멈췄다. 지금 판에서는 풀렸다.
- 흐름: 씬 연결 때 UISystem 숨김 처리가 엔딩을 초기화한다. `Open`이 켤 때 `Awake`는 이미 초기화돼 지나간다.
- 씬 구성(GraphicRaycaster·EventSystem 하나·정렬 7)도 문제없다.
- **중간 — 사용자에게 물음:**
  - 지금 검사는 "결과 화면 밖"만 본다. 엔딩을 다른 꺼진 캔버스 아래에 두면 진입 때 걸리지 않고, 보스 승리 순간에 다시 멈춘다.
  - `Open`과 같은 조건("부모가 있으면 부모 계층이 켜져 있어야 함")을 `ValidateReferences`에 더하기를 권한다.
  - 부르는 곳(씬 연결·씬 루트 검사·`Open`·`Present`)은 모두 캔버스가 켜진 시점이다. 캔버스를 끄는 코드는 없고, 시험 가짜는 부모가 없어 지난다.
  - 계획(TP-13)에 없던 검사라 묻는다.
- 반영 — 낮음: 시험 SetUp 주석의 "(항상 켜진 캔버스)"가 검사보다 넓게 말해서 고쳤다. 계획 기록의 캔버스 이름도 맞췄다(위).
- 기록만 — 낮음:
  - `CombatSceneRoot.OnValidate`(`:103-107`)는 경고를 의도하지만, `completion.ValidateReferences()`가 던져 예외 로그가 남는다. 원래 있던 꼴이고, 엔딩 검사로 그런 경우가 하나 늘었다.
  - `BossEndingCanvas`의 크기 맞춤은 고정 픽셀(800×600)이다. 결과 캔버스와 같은 설정이지만 기본 캔버스와는 다르다. 해상도별 모양은 UI 담당이 정할 일이다(현강님·호진님 메모에 적음).
- 검토가 확인한 것:
  - `CombatEndUITests` 모든 사례가 통과할 것으로 본다. 승리 사례는 Run이 승리가 아니라 일반 결과 화면으로 간다. 참조 빠짐 사례는 엔딩 검사를 지난 뒤 `victoryGold`에서 던진다.
  - Combat 씬을 여는 시험들도 지금 판에서 씬 루트 검사를 지난다.
  - 최종 승리까지 가는 Boot 시험은 없다.
- **Unity 확인 문제 없음(2026-10-24 사용자). 검토 중간 건은 A(사용자 결정):** `ValidateReferences`에 "엔딩의 부모 계층이 켜져 있어야 함"을 더했다. 조건은 `EndingRunSummaryUI.Open`과 같다. 엔딩 자신은 평소 꺼져 있는 것이 정상이라 부모만 본다. 컴파일 미확인이다. 시험 가짜는 부모가 없어 지난다.

### Phase V-e — 2026-10-24, 문서 완료, 코드 변경 없음, **모두 미전달**

- Ve.1 현강님 10-22 메모 §8: 바꾼 줄(결과 화면·표시·HUD·조정자·Factory·씬 루트), 고친 시험 셋(사후 확정분 포함), 화면 차이(포기 즉시 닫힘·확인창 동안 가방 숨김과 펼침 접힘·UI 둘째 인자 값·최종 승리 엔딩), 알릴 것(`DeckButtonUI.md:29` 낡음, 엔딩 캔버스 크기 맞춤). §5 제목에 "해결" 표시.
- Ve.2 호진님 10-22 메모 §10: `TryGetRelocationBody` 삭제, 입력 사실·정책에 더한 것, 고친 Boot 시험 두 줄과 그대로 둔 기록 문구, 엔딩 연결(`BossEndingCanvas`), 옛 손패 경로. §7 제목에 "해결" 표시.
- Ve.3 천명님 Core 전달 문서 §33: `CombatHud` 드로우 문구 줄, 고친 시험 세 줄. 요청 없음.
- Ve.4 민형님 Factory 공유 §23: 표시 조립 줄(손패 삭제, 정보창·가방 필수).
- Ve.5 배치 안내 `viewDocs/combat-view-folder-guide.md` `Hud/` 줄: 옛 손패는 "삭제 예정"으로 적었다. 예시를 `CombatActionPanel`로 바꿨다.
- Ve.6 `viewDocs/placement-drag-interaction.md` 머리: 손패 집기·손패 영역 회수가 걷혔다는 "대신함(일부)" 문단을 더했다.
- Ve.7
  - 설계 상태를 "1라운드 구현 완료, V-f 측정 대기"로 바꿨다.
  - §3 W1·W2·W4에 구현 메모를 더했다. W2에는 수신 창구의 확인창 조건이 예외 입구라는 것을 적었다(V-b 검토 L7).
  - CLAUDE.md 상태 줄을 고쳤다.
- **검토(새 에이전트, 읽기 전용, 2026-10-24):** 높음 0 · 중간 3 · 낮음 8. 모두 문서 사실이었고 반영했다(V-d 검사 한 줄의 주석 하나 포함).
  - 중간 1: `CombatEndUI.cs:90·96-97·183`은 현강님이 아니라 호진님 줄이었다. 현강님 메모에서 바로잡고 호진님 메모 §10.1에 옮겼다. 계획 Vd 표·기록도 고쳤다.
  - 중간 2: 호진님 줄 `CombatActionButtons.cs:28·49`(V-b, 숨김 조건이 넓어짐)가 빠져 있었다. 호진님 메모 §10.1과 위 V-b "다른 사람 줄"에 더했다.
  - 중간 3: 부모 켜짐 검사는 사용자 Unity 확인 뒤에 더해 컴파일 미확인이다. 메모 둘·설계·CLAUDE.md의 확인 문구에 그 단서를 붙였다. 메모의 "컴파일·EditMode 시험"은 사용자가 말한 대로 "Unity 확인"으로 맞췄다.
  - 낮음:
    - 현강님 메모 줄 범위를 고쳤다(`:1151`만 현강님, `:118`은 우리 줄, 지운 것은 `:285`, 대입 `:128·133·134`).
    - `FieldUnitView.cs:267`(현강님 `SyncHudActive`) 이동을 현강님 메모에 더했다.
    - Victory 시험 줄 번호(`:198·214`)를 고쳤다.
    - `Dismiss`를 남긴 이유를 "정리 경로(파괴된 객체)"로 바로 적었다.
    - "`Open`과 같은 조건"을 "`Open`의 부모 계층 조건"으로 좁혔다(코드 주석 포함).
    - 캔버스 크기 맞춤 설명을 Constant Pixel Size(배율 1, 기준 해상도 안 씀)로 고쳤다(씬 확인).
    - CLAUDE.md V-c "지운 것"에 옛 드로우 문구를 더했다.
  - 기록만: `CombatSceneRoot.OnValidate`가 예외를 로그로 남기는 경우가 하나 늘었다(부모 꺼짐). Unity에서 씬을 열 때 콘솔에 예외가 없는지 사용자 확인 목록에 넣는다.
- Ve.8 사용자 할 일(보고): 씬 `Hand` 객체의 `HandPresenter` 컴포넌트를 뗀 뒤, 스크립트 두 파일과 그 시험(`HandCardHoverTests`, `CombatDisplayReuseTests`의 `HandCards_…` 둘)을 지울지 정한다.

### Phase V-f — 2026-10-24, 측정 완료(코드 변경 없음)

- 사용자 "Unity 확인 문제 없음, V-f 진행해".
- 두 판(P·Q)은 스크래치패드 `view-ab/`에 꺼냈다. 배정표는 `view-ab-mapping.json`(P = `8ed3f8cb`, Q = 작업 트리)이다. 꺼낸 스크립트는 `make_ab.py`다.
- 하위 에이전트 3개(SSOT·독립성 / SOLID·FW·있는 기능 / 불필요·우회)가 블라인드로 매겼다.
- 합친 결과는 **P 7.4 → Q 8.3**이다. 예상은 8.5였다. 기준별 표·문제 상태·새로 찾은 것은 [설계 §10](../specs/2026-10-24-combat-view-stage-design.md)에 있다.
- 직접 대조한 것:
  - `EnemyUnit_Ballista.prefab`(`BallistaWeaponView` 붙음)을 참조하는 DB 에셋·씬이 0이다(참조 없는 에셋이라 보고만 함).
  - `CombatEndUI.lastHit`는 끄기만 한다(`:239`, 켜는 곳 없음).
  - `HandPresenter.Update`는 `_views == null`이면 바로 빠져나온다(`:186-188`).
- **1라운드 끝.** 남은 것: 사용자 커밋, 손패 컴포넌트·파일 정리 결정, 2라운드 상세 설계(설계 P7).
