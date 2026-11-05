# 전투 View 단계 3라운드 Task 계획 (2026-10-25)

- **설계:** [round3-design](../specs/2026-10-25-combat-view-stage-round3-design.md) §3~§7(W10·W11·W12·W8·W9).
- **상태:** 계획 초안. 코드 변경은 없다. 설계와 함께 썼다(사용자 "라운드3에 해당 내용 포함해서 계획 작성해").
  - 설계 정할 것(Q-V19~Q-V28·Q-T7)은 **모두 권장안 A를 전제**로 썼다. B를 고르면 그 Phase를 다시 쓴다.
  - 계획에서 정한 것은 TP-19~TP-30이다(아래).
- **결정(2026-10-25 사용자 "커밋했고 추천안대로 진행해, Phase 별 검토는 W 단위로 맞춰서 하는게 낫겠다"):**
  - Q-V19~Q-V27 모두 A, Q-T7 a~e 모두 A, TP-19~TP-29 그대로다.
  - Q-V28·TP-30은 바뀌었다: **검토는 W 묶음이 끝날 때 한 번 한다.**
    - W10(V3-a)·W12(V3-d)는 새 에이전트 하나다.
    - W11(V3-b·V3-c 끝)·W8(V3-e·V3-f 끝)·W9(V3-g~V3-i 끝)는 셋이다.
    - Phase마다 멈추고 Unity 확인을 받는 규칙은 그대로다. 묶음 중간 Phase(V3-b·V3-e·V3-g·V3-h)는 검토 없이 보고한다.
- **기준 코드:** 2라운드 뒤 작업 트리(커밋 전)다. 줄 번호는 지금 작업 트리 기준이다. V3-0에서 사용자 커밋을 확인한 뒤 Phase마다 다시 찾는다.
- **진행 규칙(1·2라운드와 같음)**
  - Phase 하나가 끝나면 멈추고 보고한다. 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인한 뒤 다음 Phase로 간다.
  - 코드 검토는 W 묶음이 끝날 때 새 에이전트(읽기 전용)로 한다(2026-10-25 결정). W10·W12는 하나, W11·W8·W9는 셋이다. 검토는 그 묶음의 모든 Phase 변경을 본다.
  - 전투 View 작업이라 **MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다.** 보고에 "컴파일 미확인"을 적는다.
  - 시험 처리:
    - 걷은 기능을 보던 우리 시험은 지운다.
    - 남는 내용을 보는 우리 시험은 호출·입력만 맞춘다. 바뀐 값의 단언은 기대값만 맞춘다(2라운드 TP-4).
    - 다른 사람 시험은 "결정 대기" 표(Q-T7)의 허락분만 고친다.
  - 우리 영역 안의 다른 사람 운영 줄을 고치면 V3-j 공유 메모에 적는다. Framework·GlobalSystem·Core·다른 씬은 고치지 않는다.
  - 새 파일은 `.meta`와 함께 만든다. 지우는 파일은 `.meta`와 함께 지운다. 이름을 바꾸면 `git mv`로 `.cs`와 `.meta`를 함께 옮긴다(GUID 유지).
- **경로 약어:**
  - `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`
  - `D/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
  - `DB/` = `Assets/ProjectL/DataBaseSO/ViewGroup/`

## 0. 점수 흐름 (2라운드 측정 M 8.7 기준, 알려진 문제 기준, 누적)

| Phase 끝 | 8.7에서 | 바뀌는 것 |
|---|---|---|
| V3-a (W10 강조 층 표) | **8.83** | SOLID 중간(강조 층 다섯 곳) |
| V3-b·V3-c (W11 조정자) | 8.83 | 조정자 낮음·씬 루트 두 필드(낮음, 상한이라 점수 변화 없음) |
| V3-d (W12 작은 정리) | **8.88** | 독립성 낮음 하나(14인자 서명). SSOT 낮음 셋은 상한이라 변화 없음 |
| V3-e (Ballista 코드 길) | **9.0** | 불필요 중간(Ballista 두 경로) |
| V3-f (W8 공격 종류 전략) | **9.13** | SOLID 중간(V-M6) |
| V3-g (W9 화면 전환·폭발) | **9.21** | 있는 기능 낮음 둘(V-M9·`Boom`) |
| V3-h (W9 소리 프리셋) | **9.33** | 독립성 중간(V-M10) |
| V3-i (W9 Presenter 묶음) | 9.33 | 불필요 낮음(상한) |
| V3-j·V3-k | 9.33 | 문서·측정 |

- 기준별 3라운드 뒤: SSOT 9.0 · SOLID 9.0 · FW 9.75 · 있는 기능 10.0 · 독립성 9.25 · 불필요 9.0 → 평균 9.33.
- 지난 측정은 예상보다 0.2 낮았다. 이번에도 새로 생기는 낮음(전략 기반·빈 `BallistaWeaponView`·연결 객체 양방향 호출)을 생각하면 실제는 약 9.1~9.2로 본다(설계 §10).

## Phase V3-0 — 기준 판 고정 (Q-V19 A, 코드 변경 없음)

| Task | 내용 |
|---|---|
| V0.1 | 사용자가 2라운드를 커밋했는지 확인한다(`git log`·`git status`). 그 커밋을 3라운드 A/B의 B 판으로 적는다. |
| V0.2 | 커밋 뒤 작업 트리가 깨끗한지 본다. 남은 변경이 있으면 무엇인지 보고하고 묻는다. |
| V0.3 | 설계 §11 다른 사람 줄을 그 커밋 기준으로 다시 센다(blame). 바뀌면 계획 표를 고친다. |

## Phase V3-a — W10 강조 층 표 (설계 §3)

| Task | 파일 | 내용 |
|---|---|---|
| Va3.1 | `V/Display/Field/FieldHighlightLayers.cs` | 층 표 `Rows`(층·색·채움·평소에도 그림)를 그리는 순서대로 둔다. `DrawOrder`·`IdleLayers`·`ActingLayers`는 표에서 만든다. `ColorOf`·`FillsCell`은 표를 찾는다(층이 아니거나 여럿이면 지금처럼 `ArgumentOutOfRangeException`). |
| Va3.2 | 같은 파일 | 표를 만들 때 열거의 단일 비트 값(None 제외)이 모두 한 번씩 있는지 검사한다(P20). 실패는 `InvalidOperationException`이다(형식 초기화 예외 안쪽에 원인 문구). |
| Va3.3 | `V/Display/Field/FieldHighlightState.cs:68-70` | `ActingLayers` 상수를 지우고 `FieldHighlightLayers.ActingLayers`를 읽는다. |
| Va3.4 | `V/Display/Field/FieldOutlineRenderer.cs:35-37` | `IdleLayers` 상수를 지우고 `FieldHighlightLayers.IdleLayers`를 읽는다. 클래스 설명의 층 목록 문장은 표를 가리키게 고친다. |
| Va3.5 | 주석 | 열거 요약의 "우선순위는 … 바로 위" 문장을 표 한 곳을 가리키게 줄인다. |

- **동작:** 같다. 값·순서를 그대로 옮긴다. 두 마스크의 합이 지금과 같은지(행동 중 여덟, 평소 셋) 검토가 대조한다.
- **시험:** 바꿀 것이 없을 것으로 본다(손잡이 수·채우기 이름은 같은 순서·같은 층). 깨지면 그때 적는다.
- **다른 사람 줄:** `FieldHighlightLayers.cs`의 천명님 두 줄은 열거 쪽이라 그대로 둔다(구현 때 blame으로 확인).
- **새 파일:** 없다.

## Phase V3-b — W11 작은 셋: 입력 연결·배치 진입·불러온 씬 (설계 §4.2 W11-1~3)

| Task | 파일 | 내용 |
|---|---|---|
| Vb3.1 | 새 `V/Input/CombatInputConnection.cs`(+ `.meta`) | 입력 연결. 생성자: 입력 슬롯·Layer 서비스·오류 경계·수명 판정 둘(`isSceneActive`·`isModalOpen`)·`route`. 수신 창구를 만들고 갖는다. 멤버는 `Receiver`, `IsBound`, `CurrentLayer`, `Apply(InputLayer desired, Action cancelPresentation)`, `Release(ref errors)`, `ReleaseOrReport()`, `FocusLost()`, 버튼 입구 셋(`AttackPressed`·`EndTurnPressed`·`ReturnPressed`, 수신 창구 메서드)이다. |
| Vb3.2 | `V/CombatViewSystem.cs` | 필드 `_layers`·`_receiver`·`_onAttackButton`·`_onEndTurnButton`·`_onReturnButton`과 생성자 람다 셋, `SyncInputLayer` 몸통, `ReleaseInputOrReport`·`ReleaseInput`·`ClearLayerIfSet`, `IsInputBound`, `FocusLost`·`RequestReturn` 몸통을 입력 연결로 옮긴다. `SyncInputLayer`는 정책으로 목표 Layer를 구해 `_inputConnection.Apply(desired, CancelInputPresentation)`를 부르는 두 줄이 된다. `ContinueAttach`의 버튼 연결은 입력 연결의 입구 셋을 넘긴다. `ReadFacts`의 `_layers.CurrentLayer`는 `_inputConnection.CurrentLayer`다. |
| Vb3.3 | 같은 파일 | 시험 입구 `Input`(수신 창구)·`FocusLost`·`RequestReturn`은 서명 그대로 입력 연결에 위임한다(P22). |
| Vb3.4 | 새 `V/Input/ActionButtons/CombatDeploymentEntry.cs`(+ `.meta`) | 배치 진입. 현강님 `CombatDeploymentConfirmation`을 만들고 갖는다. 콜백 넷의 몸통은 조정자에서 옮긴다: 진입 가능 판단(입력 준비·사실·정책 `CanSubmit`), 진입 제출(묶음 `RouteCommand` + 행동 버튼 맞춤 콜백), 취소(`RouteCommand(FocusLost)`), 메인 미배치 안내(묶음 `Notices`). 멤버는 `IsOpen`, `Request()`, `Sync()`, `Close()`다. |
| Vb3.5 | `V/Input/CombatSceneInput.cs`, `F/CombatSceneInputFactory.cs`, `V/Input/ICombatSceneInputFactory.cs`, `F/CombatSceneFactory.cs` | 배치 진입을 연결 입력 묶음이 갖는다(연결 수명, P23). Factory가 묶음과 함께 만든다. 묶음의 `Stop`이 닫기를 맨 앞에서 부른다(지금 `Detach`·`StopInput`의 닫기 순서와 같게). 생성 인자에 확인창 표시·편성 읽기·진입 가능 판단·행동 버튼 맞춤 콜백을 더한다(V3-d에서 재료 묶음으로 합침). |
| Vb3.6 | `V/CombatViewSystem.cs` | `_deploymentConfirmation` 필드와 `BindServices`의 생성 줄, 콜백 넷, `IsModalOpen`(→ `_input?.Deployment.IsOpen ?? false`), `RouteInput`의 진입 갈래(→ `_input.Deployment.Request()`), `SyncActionButtons`의 `Sync`, `OnInputClosed`·`Detach`·`StopInput`의 `Close`를 맞춘다. `_textPresentation`은 확인창 표시 캐스트와 입력 Factory에 넘기려고 남긴다. |
| Vb3.7 | 새 `V/CombatLoadedScene.cs`(+ `.meta`, 루트, Q-V22 A) | 불러온 씬. 생성자: 장면 생성 계약·표시 표·오류 경계. 멤버는 `Load(int stageKey)`(루트 하나 찾기·배경 세우기, 옛 배경 먼저 놓기), `Root`, `BackdropFor(CombatSceneRoot root)`, `Release()`다. |
| Vb3.8 | `V/CombatViewSystem.cs` | `#region 씬 수명`의 몸통을 불러온 씬으로 옮긴다. `OnCombatSceneLoaded`는 검사 뒤 `_scene.Load(_displayValues.CurrentStageKey)`, `OnCombatSceneEntered`는 `Attach(_scene.Root)`, `ReleaseScene`은 `Detach` → `_scene.Release()`다. `Attach`의 배경 고르기는 `_scene.BackdropFor(root)`다. |

- **동작:** 같다.
  - 수신 창구의 생성 시점(`BindServices`), Unbind → ClearLayer 순서, Layer가 바뀔 때만 표현 정리하는 규칙, 버튼의 대상(수신 창구 메서드)이 그대로다.
  - 배치 확인창은 서비스 수명에서 연결 수명으로 바뀐다(P23). 확인창이 연결 밖에서 열릴 수 없어서, 보이는 차이는 없다. 연결 없을 때 `IsModalOpen`은 지금도 거짓이다(확인창이 열린 채 연결이 끝나면 `Detach`가 닫음).
  - 진입 가능 판단의 "오류 정지·입력 준비" 검사와 정책 호출 순서가 그대로다.
- **시험:**
  - 조정자를 만드는 시험은 서명이 그대로라 바뀌지 않는다.
  - 배치 확인창 시험(현강님 `CombatDeploymentConfirmationTests`)은 클래스가 그대로라 바뀌지 않는다.
  - 조정자의 확인창 흐름을 보는 우리 시험(`CombatViewSystemTests`)은 입구가 같아 그대로일 것으로 본다. 리플렉션으로 `_deploymentConfirmation`을 읽는 시험은 없다(2026-10-25 grep).
- **다른 사람 줄(지금 작업 트리):**
  - 현강님: 조정자 `:128-129`(필드), `:264-265`(생성), `:679-680`·`:722-723`·`:881`(닫기), `:1090-1095`(진입 갈래), `:1122`(맞춤), `:1132-1157`(콜백 넷)
  - 천명님: `:100`(`_layers`), 입력 해제 메서드의 중괄호 줄(`:856`~`:939`)
- **새 파일:** `Input/CombatInputConnection.cs`, `Input/ActionButtons/CombatDeploymentEntry.cs`, `CombatLoadedScene.cs`.

## Phase V3-c — W11 연결 객체 (설계 §4.2 W11-4, 검토 셋)

**정리 순서 표(지금 → 바꾼 뒤, P24):**

| 단계 | 지금 `Detach` | 바꾼 뒤 |
|---|---|---|
| 1 | 확인창 닫기 | 연결 입력 묶음의 배치 진입 닫기(V3-b) |
| 2 | 입력 표면 루트 해제 → 씬 루트 구독 해제 | `_connection.UnbindRoot()`(같은 순서) |
| 3 | 표시 없으면 여기서 끝 | `_connection == null`이면 여기서 끝 |
| 4 | 재생 끊기 | 그대로(조정자) |
| 5 | 입력 정지(덱 호버 → HUD 아이템 해제 → 묶음 `Stop`) | `_connection.StopInput(ref errors)`(같은 순서) |
| 6 | 입력 해제(수신 창구 Unbind·Layer 비우기) | `_inputConnection.Release(ref errors)` |
| 7 | 참조 비우기(`_display`·`_surfaces`·`_inspection`·`_input`) | `_connection = null`(표시는 지역 변수로 잡아 둠) |
| 8 | 연결 상태 되돌리기(첫 그리기·진행 사실·관측·입력 Buffer) | 그대로(조정자가 `EndConnectionState`) |
| 9 | 연출 끊기 → 표시 반환 | 그대로 |

| Task | 파일 | 내용 |
|---|---|---|
| Vc3.1 | 새 `V/CombatViewConnection.cs`(+ `.meta`, 루트, Q-V22 A) | 연결 하나의 상태. 생성자는 표시·씬 루트(없을 수 있음)를 받는다. 갖는 것: `Display`, `Surfaces`(표시와 루트로 만듦), `Input`(첫 그리기 뒤 설정), `Inspection`, `DeckHover`, `Shown`(첫 그리기 성공), `BattleInstance`. 멤버: `BindRoot(receiver, focusLost, disabled)`, `UnbindRoot()`, `AttachInput(CombatSceneInput)`(HUD 아이템 해제 연결 포함), `StopInput(ref errors)`. |
| Vc3.2 | `V/Input/ActionButtons/CombatInputSurfaces.cs` | 루트를 생성자(또는 `BindRoot`)에서 한 번 받는다. `_root`는 연결 객체의 루트와 같은 것 하나만 남는다(2라운드 새 낮음 "씬 루트 두 필드" 해소). |
| Vc3.3 | `V/CombatViewSystem.cs` | 연결 수명 필드 여덟(`_display`·`_surfaces`·`_input`·`_inspection`·`_deckHover`·`_subscribedRoot`·`_battleShown`·`_battleInstance`)을 `_connection` 하나로 바꾼다. `TakeDisplay`의 표시 저장 자리(마지막 문장)에서 연결 객체를 만든다. `AttachCreated`·`ContinueAttach`·`ShowBattle`·`CreateInput`·`Detach`·`StopInput`·`StopInteraction`은 위 순서 표대로 연결 객체를 부른다. |
| Vc3.4 | 같은 파일 | 판정식(`IsAttached`·`IsSceneActive`·`CanContinue`·`HasPendingPresentation`)의 "표시 연결"을 `_connection != null`로 바꾼다. `ReadFacts`의 첫 그리기·공격 모드·빈 범위·아이템 Drag는 연결 객체에서 읽는다(연결 없으면 지금과 같은 기본값). |
| Vc3.5 | 같은 파일 | 서비스 멤버 `DeckHover`·`DeckPlacement`·`ItemInput`과 시험 입구는 서명 그대로 연결 객체를 읽는다(P22·P25). |
| Vc3.6 | 시험(허락분, Q-T7 a) | 호진님 `T/HojinCombatAttackDragIntegrationTests.cs:129-130`: 리플렉션 경로를 조정자 `_input`·`_display` → `_connection`의 `Input.Interaction`·`Display`로 맞춘다(확인 내용 그대로). |

- **동작:** 같다. 순서 표의 단계를 그대로 지키고, 첫 그리기 안의 재진입 `Detach`(연결이 이미 비었으면 구독하지 않음) 규칙도 같다.
- **시험:** 조정자를 만드는 시험은 서명이 그대로다. 리플렉션으로 연결 수명 필드를 읽는 시험은 위 호진님 한 곳뿐이다(2026-10-25 grep). 우리 `CombatRailwayContactSceneTests.cs:57`의 `_display`는 시험 고정물의 필드라 해당하지 않는다(구현 때 다시 확인).
- **다른 사람 줄(지금 작업 트리):** 현강님 덱 호버 `:520-521`·`:739-740`, 천명님 연결 수명 메서드의 중괄호 줄(`:461`·`:530`·`:573`·`:677`·`:713`·`:720`)
  - **고침(V3-c 검토, HEAD `35562e96` blame):** 현강님 `:119-120`(덱 호버 필드·서비스 멤버)·`:520-521`·`:739-740`, 천명님 `:85`(`_battleInstance`)다. 위 천명님 중괄호 여섯은 바뀌지 않는다.
- **검토:** 새 에이전트 셋으로 한다(순서·재진입·시험 영향).
- **새 파일:** `CombatViewConnection.cs`.

## Phase V3-d — W12 작은 정리 (설계 §5)

| Task | 파일 | 내용 |
|---|---|---|
| Vd3.1 | 새 `V/Input/CombatSceneInputSources.cs`(+ `.meta`) | 입력 생성 재료 묶음(서비스 수명). 표시 표·상태·캐릭터·입력 Buffer 둘·사실 읽기 둘·UI 수신 판단·막힘 보고·오류 경계·오디오·재고 읽기·텍스트 표시, 그리고 V3-b가 더한 배치 진입 재료를 갖는다. 조정자가 첫 입력 생성 때 한 번 만든다. |
| Vd3.2 | `V/Input/ICombatSceneInputFactory.cs`, `F/CombatSceneFactory.cs`, `F/CombatSceneInputFactory.cs`, `V/CombatViewSystem.cs` | 서명을 `CreateSceneInput(ICombatBattleDisplay display, CombatSceneInputSources sources)` 하나로 바꾼다. 정적 Factory는 시험이 부르는 다른 입구(`CreateSceneInteraction` 등)를 그대로 둔다. |
| Vd3.3 | `V/Input/Interaction/PlacementDragInteraction.cs:52-57`, `F/CombatSceneInputFactory.cs:148` | 쓰지 않는 `requests` 인자와 그 검사를 걷는다(현강님 파일, 서명 줄 `:57`은 현강님 줄). |
| Vd3.4 | `V/Playback/Players/CombatSubjectBlocking.cs`, `V/Playback/Players/CombatStepPlayer.cs:124-130` | 공유 함수가 겹침 허용 판단을 받게 하고, 걸음 재생기의 `MotionBlocks`가 그것을 부른다. 걸음 Gate 규칙(`FindBlock`)은 그대로다. |
| Vd3.5 | `V/Playback/Support/CombatEventSounds.cs:113` | 기차 판별을 `CombatEventTypeTable.RouteOf(e) == CombatEventRoute.Railway`로 바꾼다(현강님 줄). |
| Vd3.6 | `D/Table/ThornField/…` 검사 | 가시밭 외형 표 검사에서 프리팹 필수로 한다. 운영 에셋(`DB/ThornFieldVisual/Item/ThornFieldVisual_Default.asset`)에는 이미 있다. 표시 Factory의 "없으면 null" 갈래(`F/CombatSceneDisplayFactory.cs`의 가시밭 줄)와 표시의 null 검사도 걷는다. |

- **동작:** 같다. Vd3.6만 프리팹 없는 DB가 부트에서 실패한다(지금은 가시밭 전투에서 실패).
- **시험:** Vd3.6 때문에 프리팹 없이 가시밭 외형 표를 만드는 시험이 있으면 깨진다. 구현 때 찾고, 다른 사람 시험이면 멈추고 묻는다.
- **다른 사람 줄:** 현강님 `PlacementDragInteraction.cs:57`, `CombatEventSounds.cs:113`
- **새 파일:** `Input/CombatSceneInputSources.cs`.

## Phase V3-e — Ballista 코드 모션 길 걷기 (설계 §6.3, Q-V24 A)

W8 전략보다 먼저 한다. 그래야 전략으로 옮길 갈래가 줄어든다(TP-20).

| Task | 파일 | 내용 |
|---|---|---|
| Ve3.1 | `V/Presentation/Attack/CombatAttackPlayback.cs` | Ballista 무기 상태(`_ballistaWeapons`·`_reloadingWeapon`·`_reloadCompleted`), `HasBallistaWeapons`, `TryBeginReload`, `TryBeginBallista`, `ResetBallistaWeapons`, Tick의 무기·재장전 갈래, `IsComplete`·`BlocksNextEvent`의 재장전 항목을 걷는다. Ballista는 코드 모션을 시작하지 않는다(클립 길만, P27). |
| Ve3.2 | `V/Presentation/Attack/CombatBallistaAnimation.cs`(+ `.meta`) | 지운다. |
| Ve3.3 | `V/Playback/Handlers/CombatTurnEventHandler.cs:19-27` | 재장전 대기 갈래를 걷는다(턴 끝은 늘 간격 전에 보임). 처리기가 공격 재생을 더 받지 않으면 생성 인자도 맞춘다. |
| Ve3.4 | `V/Display/Unit/ICombatUnitPresentationTarget.cs:58`, `V/Display/Unit/FieldUnitView.cs:210·419`, `V/Display/Unit/CombatUnitPresentationWindow.cs:249-271` | 표시 계약의 `BallistaWeapon`, 풀 반환의 무기 초기화, 창구의 무기 조회 둘을 걷는다. 시험 가짜의 `BallistaWeapon` 멤버(우리 `CombatPieceRunTests.cs:897`·`CombatUnitPresentationWindowTests.cs:503`)도 걷는다. |
| Ve3.5 | `V/Display/Feedback/CombatAttackTrajectory.cs:238-252` | Ballista 출발점은 유닛 위치 + 발사 높이 하나다(지금의 클립 갈래). 무기 갈래·"클립도 무기도 없으면" 예외를 걷는다. |
| Ve3.6 | `V/Display/Unit/BallistaWeaponView.cs` | 직렬화 칸 둘(`bolt`·`bowString`)만 남긴 빈 컴포넌트로 줄인다. 프리팹 `EnemyUnit_Ballista.prefab`에 Missing Script가 생기지 않게 하려는 것이다(Q-V24 A). 클래스 요약에 "쓰지 않음, 프리팹 컴포넌트라 남김"을 적는다. |
| Ve3.7 | `D/CombatPresentation/CombatAttackPresentationSO.cs`·`CombatAttackPresentationData` | 시위·재장전 값 칸 넷과 그 검사·Data 멤버를 걷는다. 에셋 YAML의 옛 키는 Unity가 다시 저장할 때 지운다(지금은 남아도 무해). |
| Ve3.8 | `D/CombatPresentation/CombatUnitAnimationRules.cs`, 외형 검사 | "Ballista 공격 표시면 공격 클립 필수"를 더한다(P27). `CanUseAttackClip`·`FiresProjectile`의 "임시 예외" 문장을 고친다. 운영 Ballista 외형에는 클립이 있다. |
| Ve3.9 | `V/Playback/Handlers/CombatAttackEventHandler.cs`, 공격 재생의 `FirePiercingShot` 요약 | "임시 예외" 문장을 "Ballista의 유일한 길"로 고친다. |
| Ve3.10 | 시험(우리 + 허락분 Q-T7 b) | Ballista 코드 길만 보던 시험을 지운다: `T/CombatBallistaAttackTests.cs`(코드 길 사례, 천명님 줄 `:63-64` 포함), `T/CombatBallistaLifecycleTests.cs`, `T/CombatBallistaAssetTests.cs`의 시위·재장전 값 단언. 클립 Ballista 시험(`CombatClipShotTests`)은 그대로다. 파일째 지우면 `.meta`와 함께 지운다. |

- **동작:** 운영은 같다(Ballista 외형은 늘 클립 길). 클립 없는 Ballista 외형은 부트의 DB 생성에서 실패한다.
- **보고만:** `EnemyUnit_Ballista.prefab`은 참조 없는 에셋이라 지우지 않는다. `BallistaWeaponView`는 빈 컴포넌트로 남는다(불필요 낮음, 손패 파일과 같은 꼴).
- **다른 사람 줄:** 천명님 시험 `CombatBallistaAttackTests.cs:63-64`(Q-T7 b). 운영 줄은 모두 우리 줄이다(2026-10-25 blame).
- **지우는 파일:** `CombatBallistaAnimation.cs`, 그리고 시험 파일(구현 때 사례별로 확정).

## Phase V3-f — W8 공격 종류 전략 (설계 §6.2, Q-V23 A, 검토 셋)

| Task | 파일 | 내용 |
|---|---|---|
| Vf3.1 | 새 `V/Presentation/Attack/ICombatAttackStyleContext.cs`(+ `.meta`) | 공격 재생이 전략에 내주는 좁은 맥락이다. 투사체 풀·발사(`PoolOf`·`FireArc`·`FireLinear`·`FireThrough`), Gate(시작·해제·모두 해제), 접촉 이펙트 감싸기(`ImpactThen`), 접촉 진행률(직선·관통), 자세 대여(`BeginAttackPose`), 곡사 연출 값·무기 조회, 애니메이션 시작(`BeginAnimation`), 쓸 수 없음 취소(`CancelUnavailable`)를 담는다. |
| Vf3.2 | 새 `CombatAttackStyle.cs`·`CombatMeleeStyle.cs`·`CombatLinearStyle.cs`·`CombatMortarStyle.cs`·`CombatBallistaStyle.cs`·`CombatAttackStyles.cs`(+ `.meta` 여섯, `V/Presentation/Attack/`) | 설계 §6.2 표의 멤버다. 각 본문은 지금 갈래를 그대로 옮긴다: Melee·Linear는 `TryBeginDirect`, Mortar는 `TryBeginMortar`, Ballista는 `FirePiercingShot`, 목표점은 `CombatShotPath.TryTarget`의 각 case, 궤적은 `Segment`·`TryResolveShot`의 각 case. 표 `Of(kind)`는 열거 값이 모두 있는지 만들 때 검사한다(P20). |
| Vf3.3 | `V/Presentation/Attack/CombatAttackPlayback.cs` | 시작·발사 분기를 전략 호출로 바꾼다. `TryBeginFacingMotion`은 `SupportsFacingMotion`이 참인 전략만 쓴다. 맥락을 명시적 구현으로 단다. 수명(Gate·진행·Tick·취소)과 접촉 진행률 계산은 남는다. |
| Vf3.4 | `V/Presentation/Attack/CombatShotPath.cs` | `TryTarget` switch를 지운다. 종류와 무관한 기하(`Raise`·`LinearStart`·`ArcPoint`·`ArcHeight`)만 남긴다. 부르던 곳(공격 재생·방향·궤적)은 전략 표를 부른다. |
| Vf3.5 | `V/Presentation/Attack/CombatAttackFacing.cs` | `For`가 전략의 목표점을 쓴다(서명 그대로). |
| Vf3.6 | `V/Display/Feedback/CombatAttackTrajectory.cs` | `Segment`·`TryResolveShot`의 switch를 전략 호출로 바꾼다. 곡사 판정(`arc`)도 전략이 답한다. |
| Vf3.7 | `V/Playback/Handlers/CombatAttackEventHandler.cs` | 클립 해제 입구를 전략의 `ClipRelease`로 바꾼다(Ballista 갈래 삭제). |
| Vf3.8 | `V/Display/Unit/CombatUnitPresentationWindow.cs` | `TryGetMortarPresentation`의 종류 검사를 걷고 곡사 무기 조회만 남긴다(종류는 전략이 이미 앎). |

- **동작:** 같다. 각 갈래 본문을 그 전략으로 옮기기만 한다. 검토가 전후 값(목표점·출발점·회전·완료 정책·해제 시점)을 갈래마다 대조한다.
- **시험:**
  - `CombatShotPath.TryTarget`을 직접 부르는 우리 시험(`CombatShotPathTests`)은 호출을 전략 표로 맞춘다. 확인 내용은 그대로다.
  - 그 밖의 공격 시험(`CombatStandardAttackTests`·`CombatMortarPresentationTests`·`CombatClipShotTests`·`AttackTrajectoryInteractionTests` 등)은 입구가 같아 그대로일 것으로 본다.
  - 다른 사람 시험이 깨지면 멈추고 묻는다.
- **다른 사람 줄:** 없음(2026-10-25 blame, 대상 파일이 모두 우리 줄).
- **새 파일:** 전략 일곱(`ICombatAttackStyleContext`·`CombatAttackStyle`·종류 넷·`CombatAttackStyles`).

## Phase V3-g — W9 화면 전환 설정·폭발 이펙트 (설계 §7 W9-1, Q-V25 A)

| Task | 파일 | 내용 |
|---|---|---|
| Vg3.1 | `D/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs`(+ Data) | 칸을 더한다: 아침·밤 전환 레이어 이름(`AfterRendering`·`BefalRendering`), 낮·밤 카메라 이름(`Day Camera`·`Night Camera`), 아침·밤 색(cyan·magenta), 바뀌는 속도(30)·양(40), 환경 폭발 이펙트(`Boom`). 기본값은 지금 상수다(P28). 검사: 이름 비어 있지 않음, 속도·양 유한 양수, 이펙트는 `CombatPresentationEffectRules`. |
| Vg3.2 | `V/Playback/CombatPhaseTransitionSettings.cs`(민형님 파일) | `CreateDefault(findLayer)` → `From(settings, findLayer)`. 레이어가 없으면 예외인 지금 검사는 그대로다. |
| Vg3.3 | `F/CombatSceneDisplayFactory.cs` | 화면 전환 설정을 `From(presentation)`으로 만든다. 배경이 있으면 카메라 이름 둘이 맵 카메라 사실(`backdrop.MapCameras`)에 있는지 검사한다(없으면 `InvalidOperationException`, 이름을 문구에). 폭발 이펙트는 설정 값을 넘긴다. |
| Vg3.4 | 시험(허락분, Q-T7 c) | 민형님 `T/CombatPhaseTransitionPresenterTests.cs:33·276`: `CreateDefault(…)` → 기본 설정 값으로 `From(…)`(확인 내용 그대로). 설정 값은 시험 도우미가 실제 DB에서 읽는다(2라운드 `CombatViewSystemTests.Database`). |

- **동작:** 운영은 같다(같은 값). 맵 카메라 이름이 다르면 이제 진입 때 실패한다(지금은 조용히 전환이 안 걸림).
- **다른 사람 줄:** 민형님 `CombatPhaseTransitionSettings.cs`(42줄 중 생성 함수 쪽), 시험 두 줄. 민형님 Factory 공유 §25에 적는다.
- **새 파일:** 없다.

## Phase V3-h — W9 소리 프리셋 (설계 §7 W9-2, Q-V26 A)

**외형 에셋에 넣을 값(지금 switch와 같게, 구현 때 InGame 유닛 분류로 다시 계산):**

| 외형 | 유닛 소리 프리셋 | 공격 소리 프리셋 |
|---|---|---|
| Akimbo | 0(메인 특례) | 0 |
| Sniper | 분류/10 | 2 |
| Cowboy | 분류/10 | 4 |
| ShotGun | 분류/10 | 6 |
| Gravekeeper | 분류/10 | 13 |
| CactusCannon | 분류/10 | 9 |
| Ballista | 분류/10 | 11 |
| Demon | 분류/10 | 12 |
| Cannon(UndeadCannonCart) | 분류/10 | 14 |
| StageBoss1 | 분류/10 | 15 |
| Heavy(UndeadHeavy) | 분류/10 | 16 |
| 그 밖(본부·보급·화약통·등록 안 된 외형) | −1 | −1 |

- 적은 외형 값과 상관없이 설정 표의 적 공용 프리셋(5)을 쓴다(지금 규칙 그대로).
- 분류가 Main·Rider·Heavy·Cannon이 아니면 −1이다(지금 규칙 그대로).

| Task | 파일 | 내용 |
|---|---|---|
| Vh3.1 | `D/Table/Unit/Item/UnitVisualDataSO.cs`(+ Data) | 칸 둘(유닛 소리 프리셋·공격 소리 프리셋, −1 이상). |
| Vh3.2 | `DB/UnitVisual/Item/**/UnitVisual_*.asset` 열넷 | 위 표의 값을 넣는다(YAML 칸 두 줄씩). |
| Vh3.3 | 설정 표 SO(+ Data) | 적 공용 유닛 프리셋(5), 배경음 프리셋 셋(밤 3·낮 2·완료 뒤 1). 기본값은 지금 상수다. |
| Vh3.4 | 새 `V/Playback/Support/CombatSoundPresets.cs`(+ `.meta`) | 외형 표·설정 표로 만드는 값 객체다. `Unit(key, owner)`·`Attack(key)`·`Bgm(phase)`. 표시 표(`CombatDisplayTables`)에서 만든다. |
| Vh3.5 | `V/Playback/Support/CombatEventSounds.cs`·`CombatEventAudio.cs` | 정적 `UnitPreset`·`AttackPreset`·`BgmPreset`을 지우고 값 객체를 받는다. 포효 소리는 포효한 유닛의 공격 프리셋이다(지금 데이터에서는 보스라 같음). |
| Vh3.6 | `V/Input/Interaction/AttackTargetingInteraction.cs:136`, `PlacementDragInteraction.cs:273`, `V/Display/CombatSceneDisplay.cs:203·281`, 입력·표시 Factory | 부르는 곳이 값 객체를 받는다(입력 재료 묶음·표시 생성). |
| Vh3.7 | 시험(우리 + 허락분 Q-T7 d) | 프리셋을 정적 함수로 읽던 단언을 값 객체(실제 DB로 만든 것)로 맞춘다. 확인 내용(어느 유닛이 어느 프리셋인가)은 그대로다. 대상: 우리 `CombatEventSoundsTests`(현강님 50줄 포함)·`CombatSceneEventPresenterTests:296·305`·`CombatStandardAttackAssetTests:125`·`GameAudioTests`(우리 다섯 줄, 현강님 `:80`), `StageMapCombatEntryBootSmokeTests.cs:129-138`(우리 `:129-130`, 현강님 `:131`, 천명님 `:137-138`). |

- **동작:** 운영은 같다(같은 값). 외형을 새로 더할 때 프리셋을 칸에 넣으면 되고, 넣지 않으면 −1(소리 없음)이다. 지금 switch 빠뜨림과 결과는 같지만, 고칠 곳이 데이터 한 곳이 된다.
- **다른 사람 줄:** 현강님 `CombatEventAudio` 여덟 줄·`CombatEventSounds` 프리셋 줄, 천명님 `CombatEventAudio` 네 줄(구현 때 blame으로 확정), 시험 위 목록.
- **새 파일:** `Playback/Support/CombatSoundPresets.cs`.

## Phase V3-i — W9 사건 Presenter 필수 묶음 (설계 §7 W9-3, Q-V27 A)

| Task | 파일 | 내용 |
|---|---|---|
| Vi3.1 | 새 `V/Playback/CombatEventPresentationValues.cs`(+ `.meta`) | 걸음 시간·적 행동/턴 간격·이동 회전 시간·능력 연출 값·카메라 연출 값·걸음 뜀 높이. 설정 표에서 `From`으로 만든다. 지금 생성자의 값 검사(걸음 시간·간격·뜀 높이)를 여기로 옮긴다. |
| Vi3.2 | 새 `V/Playback/CombatFieldEventPorts.cs`(+ `.meta`) | 필드 창구 묶음(필수 대리자 여덟): 가시 소비·환경 효과·재생 강조·가시 몸·오브젝트 몸·오브젝트 숨김·토네이도 숨김·토네이도 켜기. |
| Vi3.3 | `V/Playback/CombatSceneEventPresenter.cs` | 생성자를 `(units, values, audio, camera, effects, projectileRoot, ports)`로 바꾼다(모두 필수). 시험용 대체(`camera ?? new CombatCameraWindow(null, …)`, `abilityMotion ?? Instant`)를 걷는다. |
| Vi3.4 | 처리기·공격 재생·사건 소리 | `CombatAttackPlayback`·`CombatAttackEventHandler`·`CombatCharacterEventHandler`·`CombatRoarEventHandler`의 `effects = null`·`cameraMotion ?? None`, `CombatEventAudio`의 `audio == null` 갈래를 걷는다(필수). `CombatCameraMotionSettings.None`은 "연출 값 없음"이라는 값으로 남는다(시험이 씀). |
| Vi3.5 | `F/CombatSceneDisplayFactory.cs` | 묶음 셋을 만들어 넘긴다. |
| Vi3.6 | 시험(우리 + 허락분 Q-T7 e) | 사건 Presenter를 만드는 열셋(우리 열하나, 현강님 `T/CombatSceneEventPresenterTests.cs:270·515`)은 우리 `CombatViewSystemTests` 안의 도우미(값 묶음 기본값·빈 필드 창구·조용한 오디오·맵 없는 카메라 창구)를 쓴다. 확인 내용은 그대로다. 생성자 인자 검사 시험(`CombatEnemyPresentationTests.cs:275·305`)은 값 묶음 검사로 호출만 맞춘다. |

- **동작:** 운영은 같다(운영은 이미 모두 넘김). 시험 조립에서 꺼져 있던 것(오디오·이펙트·카메라)이 가짜로 켜진다(2라운드 V2-g와 같은 꼴).
- **다른 사람 줄:** `CombatSceneEventPresenter.cs`의 현강님 열넷·호진님 여섯·천명님 하나 중 생성자·필드 줄(구현 때 blame으로 확정), 시험 두 곳.
- **새 파일:** `Playback/CombatEventPresentationValues.cs`, `Playback/CombatFieldEventPorts.cs`.

## Phase V3-j — 문서·공유 (모두 미전달로 작성, 코드 변경 없음)

| Task | 내용 |
|---|---|
| Vj3.1 | 현강님 메모 §10: 배치 진입(확인창 수명 변경, 콜백 넷), 덱 호버 줄, `PlacementDragInteraction` 서명, 기차 판별, 소리 프리셋, 고친 시험. |
| Vj3.2 | 호진님 메모 §12: 연결 객체(리플렉션 경로), 프리셋 시험, 사건 Presenter의 호진님 줄. |
| Vj3.3 | 천명님 Core 전달 문서 §35(요청 없음): 입력 연결로 옮긴 줄, `CombatEventAudio` 줄, Ballista 시험 두 줄. |
| Vj3.4 | 민형님 Factory 공유 §25: 화면 전환 설정(`From`·카메라 검사), 표시 Factory 줄(묶음 셋·폭발 이펙트·가시밭), 입력 생성 서명, 시험 두 줄. |
| Vj3.5 | 폴더 배치 안내: 새 파일 위치(루트 둘 포함), §2 규칙(수명별 객체·공격 종류 전략·층 표). 설계 구현 메모. CLAUDE.md. |

## Phase V3-k — 3라운드 측정 (코드 변경 없음)

- 1·2라운드와 같은 방법이다. B 판 = V3-0의 커밋, A 판 = 3라운드 뒤 작업 트리다.
- 범위·평가자 셋·감점 규칙·블라인드는 같다. 지시문은 2라운드 `vi_prompts.json`을 쓰고, 판 이름·파일 수·범위 한 줄만 바꾼다.
- 결과는 설계 §13(새 절)에 적는다.

## 계획에서 정한 것 (사용자 확인 대상)

- **TP-19 W11을 두 Phase로 나눈다(작은 셋 / 연결 객체).** 연결 객체는 `Attach`~`Detach` 순서를 건드리는 위험한 변경이라 따로 확인받는다.
- **TP-20 Ballista 코드 길 걷기(V3-e)를 공격 종류 전략(V3-f)보다 먼저 한다.** 걷을 갈래를 전략으로 옮기지 않으려는 것이다.
- **TP-21 배치 진입의 행동 버튼 맞춤은 콜백으로 받는다.** 진입 제출 뒤 버튼을 바로 숨기는 지금 동작을 지키려는 것이다(Tick 끝 맞춤만 두면 한 프레임 늦음).
- **TP-22 입력 연결은 서비스 수명이다.** 수신 창구가 `BindServices`에서 생기는 지금 시점을 지킨다. 연결 객체에 넣지 않는다.
- **TP-23 연결 객체는 표시 저장 자리에서 만든다.** `TakeDisplay`의 "마지막 문장이 소유권 이전" 규칙을 그대로 지킨다.
- **TP-24 V3-d의 재료 묶음은 서비스 수명이다.** 표시(연결 수명)는 따로 넘긴다.
- **TP-25 빈 `BallistaWeaponView`의 직렬화 칸은 남긴다.** 프리팹 YAML의 값이 그대로 읽히게 하려는 것이다.
- **TP-26 소리 프리셋 값 객체는 표시 표에서 만든다.** 입력과 표시가 같은 객체를 받는다.
- **TP-27 Presenter 시험 도우미는 2라운드 도우미 파일(`CombatViewSystemTests.cs`)에 더한다.** 새 시험 파일을 만들지 않는다.
- **TP-28 표 검사 실패는 `InvalidOperationException`이다.** 형식 초기화 예외로 감싸여도 안쪽 문구로 원인을 읽게 한다.
- **TP-29 Phase 시작 때 다른 사람 줄을 blame으로 다시 센다.** 2라운드 커밋 뒤 줄 번호가 바뀌기 때문이다.
- **TP-30 검토 에이전트 수는 Q-V28 A대로다.** V3-c·V3-f만 셋이다. **(2026-10-25 사용자 결정으로 바뀜: W 묶음 끝마다 검토, W11·W8·W9는 셋.)**

## 결정 대기 — 다른 사람 시험 (Q-T7)

| # | 시험 | 지금 하는 일 | 깨지는 까닭 | A(권장) |
|---|---|---|---|---|
| **a** | 호진님 `T/HojinCombatAttackDragIntegrationTests.cs:129-130`(호진님 줄은 `:130`, `:129`는 우리 줄 — V3-c 검토) | 조정자 `_input`·`_display`를 리플렉션으로 읽음 | V3-c에서 연결 객체 안으로 감 | 경로만 `_connection`으로 맞춤(확인 내용 그대로) |
| **b** | 천명님 `T/CombatBallistaAttackTests.cs:63-64`(SetUp) | Ballista 코드 길 시험의 준비 | V3-e에서 코드 길을 걷어 시험을 지움 | 파일(또는 그 사례)과 함께 지움 |
| **c** | 민형님 `T/CombatPhaseTransitionPresenterTests.cs:33·276` | `CreateDefault`로 설정을 만듦 | V3-g에서 `From(settings)`로 바뀜 | 기본 설정 값으로 `From`(확인 내용 그대로) |
| **d** | 현강님 `GameAudioTests.cs:80`·`StageMapCombatEntryBootSmokeTests.cs:131`·`CombatEventSoundsTests`의 현강님 줄, 천명님 `StageMapCombatEntryBootSmokeTests.cs:137-138` | 정적 프리셋 함수로 값을 단언 | V3-h에서 값 객체로 바뀜 | 값 객체(실제 DB)로 읽게 맞춤(확인 내용 그대로) |
| **e** | 현강님 `T/CombatSceneEventPresenterTests.cs:270·515` | 선택 인자로 사건 Presenter를 만듦 | V3-i에서 필수 묶음으로 바뀜 | 우리 도우미로 묶음을 넘김(확인 내용 그대로) |

## 커밋 묶음 (사용자가 함)

- 새 파일은 `.meta`와 함께 넣고, 지운 파일은 `.meta`와 함께 넣는다.
- Phase마다 커밋하면 A/B 측정이 쉽다. 한 번에 커밋해도 된다.
- 필수 인자로 묶인 것(V3-i의 Presenter·Factory·시험 열셋, V3-h의 SO·에셋 열넷·값 객체·시험)은 한 커밋에 넣는다. 에셋 없이 코드만 넣으면 프리셋이 −1이 된다.
- 에디터가 바꾼 FMOD 캐시는 범위가 아니다.

## 실행 기록

### Phase V3-0 — 2026-10-25, 기준 판 고정(코드 변경 없음)

- B 판은 사용자 커밋 `35562e96`("[용근] 전투 View 개선 2차 측정 · 3차 설계 문서", 107파일)이다. 2라운드 코드·시험·문서와 3라운드 설계·계획이 들어 있다.
- 작업 트리는 그 위에서 깨끗하다(`git status` 0줄).
- 2라운드 새 파일 열하나가 들어 있다. 지운 `CombatActionBoundary.cs`도 반영됐다.
- 커밋 뒤 줄 번호는 이 커밋 기준이다. 작업 트리가 커밋과 같아서, 계획에 적은 "지금 작업 트리" 줄 번호가 그대로 맞는다. blame의 "커밋 전" 줄은 이제 우리 줄(`s9810282`)로 나온다.

### Phase V3-a — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W10 끝

사용자 "커밋했고 추천안대로 진행해, Phase 별 검토는 W 단위로 맞춰서 하는게 낫겠다".

**바꾼 것**
- `V/Display/Field/FieldHighlightLayers.cs`
  - 층 표 `Rows`(층·색·채움·평소에도 그림)를 그리는 순서대로 둔다. 줄은 `readonly struct LayerRow`다.
  - 정적 생성자가 표에서 `DrawOrder`·`IdleLayers`·`ActingLayers`를 만든다. 표를 만들 때 검사한다:
    - 줄마다 단일 비트
    - 중복 없음
    - 열거 값 누락 없음
    - 실패는 `InvalidOperationException`이다(형식 초기화 예외 안쪽, TP-28).
  - `Resolve`는 표를 뒤에서부터 돌며 색을 바로 돌려준다.
  - `ColorOf`는 표를 찾고, 층이 아니면 지금처럼 `ArgumentOutOfRangeException`이다.
  - `FillsCell`은 표를 찾고, 층이 아니면 지금처럼 거짓이다(검토 낮음 1).
  - 열거 요약의 순서·색 문장은 "층 표가 원본"으로 줄였다.
- `V/Display/Field/FieldHighlightState.cs`: `ActingLayers` 상수를 지우고 표의 값을 읽는다. `IsActing` 요약의 층 목록도 표를 가리킨다.
- `V/Display/Field/FieldOutlineRenderer.cs`: `IdleLayers` 상수를 지우고 표의 값을 읽는다. 클래스 설명의 층 목록도 표를 가리킨다.

**동작:** 같다.
- 검토가 HEAD와 하나씩 대조했다: 순서 11, 층→색 11쌍, 채움 여섯, 평소 셋, 행동 중 여덟(비트까지), `Resolve` 결과, 예외 형식.
- 새 할당은 없다. `Resolve`는 인터페이스 대신 배열을 돌아 조금 가볍다.

**시험:** 바꾼 것 없음.
- 공개 멤버 서명이 그대로다.
- 그대로 성립하는 시험: `CombatDisplayReuseTests`·`CombatObjectiveDisplayTests`·`WallAttackInteractionTests`·`FieldHighlightStateTests`·`FieldOutlineRendererTests`(검토 확인).

**계획과 다른 점:** 없다. 계획 Va3.1의 "층이 아니면 지금처럼 예외"는 `ColorOf`에만 해당한다. `FillsCell`은 HEAD처럼 거짓을 돌려준다.

**검토(새 에이전트 하나, W10 묶음):** 높음·중간 0, 낮음 5.
- 반영:
  - 낮음 1: `FillsCell`의 계약을 HEAD와 같게 맞췄다(`TryRowOf`).
  - 낮음 2: 층 목록 사본 셋을 표를 가리키게 고쳤다(`FieldOutlineRenderer` 클래스 설명, `IsActing` 요약, `IdleLayers` 요약).
  - 낮음 3: 열거 요약의 색 사실 둘을 고쳤다.
  - 낮음 5 일부: "평소에도 그림" 칸 하나가 두 사실을 정한다는 표 주석을 더했다.
- 남김:
  - 낮음 4: 이동 경로 테두리 색이 표를 거치지 않는다. `FieldOutlineRenderer.cs:428`이 `_palette.MovementDraft`를 바로 쓴다. 기존 코드이고 민형님 줄이라, V3-j 민형님 공유에 제안으로 적는다.
  - 낮음 5 나머지: `FillColorOf`의 영향 칸 특례(주석 있음), 색 칸 검사 추가(선택).
  - P21: 호버 색조 → 층 대응은 표 밖에 둔다(검토 동의).

**다른 사람 줄:** 없다. 바꾼 줄은 모두 우리 줄이다. 천명님 열거 두 줄과 민형님 줄은 그대로다(검토 확인).

**새 파일:** 없다.

**Unity에서 볼 것**
- 컴파일
- EditMode 시험: `FieldOutlineRendererTests`·`FieldHighlightStateTests`·`CombatDisplayReuseTests`·`CombatObjectiveDisplayTests`·`WallAttackInteractionTests`·`CombatAttackFeedbackTests`
- 재생(GridOutline):
  - 평소: 현재 행위자·전투 목표 테두리
  - 이동·공격 범위, 아군·적·위험 호버 범위 채우기
  - 예정 경로·차단 벽
  - 포효·소환 재생 강조

### Phase V3-b — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W11 앞 Phase(검토는 V3-c 끝)

사용자 "다음 작업 진행해". V3-a의 Unity 확인 여부는 듣지 못했다.

**바꾼 것**
- 새 `V/Input/CombatInputConnection.cs`(+ `.meta`, 서비스 수명, TP-22)
  - 수신 창구를 만들어 갖는다.
  - `Apply(desired, cancelPresentation)`: 옛 `SyncInputLayer` 몸통이다. 목표가 없으면 표현 정리 → 해제, 있으면 Bind → Layer가 다를 때만 표현 정리 → 변경.
  - `Release`·`ReleaseOrReport`·`ClearLayerIfSet`: 옛 조정자 셋, 문구 그대로.
  - `CurrentLayer`·`IsBound`·`FocusLost`
  - 버튼 입구 셋: 수신 창구 메서드를 한 번 만든 대리자다.
- 새 `V/Input/ActionButtons/CombatDeploymentEntry.cs`(+ `.meta`, 연결 수명, P23)
  - 현강님 `CombatDeploymentConfirmation`을 만들어 갖는다(클래스는 그대로).
  - 옛 조정자 콜백 넷을 옮겼다: 진입 가능 판단, 진입 제출(Router + 행동 버튼 맞춤 콜백, TP-21), 입력 정리, 메인 미배치 안내.
  - 멤버: `IsOpen`·`Request`·`Sync`·`Close`.
- 새 `V/CombatLoadedScene.cs`(+ `.meta`, 루트, Q-V22 A)
  - 옛 조정자 씬 수명 몸통이다: 옛 배경 놓기 → 루트 하나 찾기 → 배경 세우기 → 스테이지 키로 보이기, 문구 그대로.
  - 멤버: `Load`·`Root`·`BackdropFor`·`Release`.
- 연결 입력 묶음 `CombatSceneInput`이 배치 진입(`Deployment`)을 갖는다. `RouteCommand`·`Notices`는 쓰는 곳이 없어져 지웠다.
- 입력 Factory·생성 계약·`CombatSceneFactory`
  - `CreateSceneInput`에 인자 둘(`isInputReady`·`afterDeploymentSubmit`)을 더했다. 재료 묶음은 V3-d에서 한다.
  - 배치 진입을 환경 호버 뒤에 만든다. 확인창 캐스트(`text as IUIConfirmationPresentation`)도 여기로 왔다.
- 조정자 `CombatViewSystem.cs`: 1159 → 1058줄, 필드 39 → 33.
  - 필드 여덟(`_loadedRoot`·`_backdrop`·`_layers`·`_deploymentConfirmation`·`_receiver`·버튼 대리자 셋)이 `_scene`·`_inputConnection` 둘이 됐다.
  - 지운 것: `ReleaseBackdrop`, `ReleaseInputOrReport`·`ReleaseInput`·`ClearLayerIfSet`, 콜백 넷, 쓰지 않게 된 `using …InputSystem.Data`
  - 시험 입구 `Input`·`FocusLost`·`RequestReturn`은 서명 그대로 입력 연결에 위임한다(P22).
  - 확인창 닫기는 지금과 같은 자리(분리·입력 정지·입력 닫힘의 맨 앞)에서 `_input?.Deployment.Close()`로 부른다.

**동작:** 같다.
- 수신 창구 생성 시점(`BindServices`)과 `BindServices`의 예외 순서(slot → layers → 로딩 → …)가 그대로다.
- Unbind → ClearLayer 순서, 해제 실패 보고 문구, 버튼의 대상이 그대로다.
- 배치 확인창의 수명은 서비스에서 연결로 바뀌었다(P23). 보이는 차이는 없다.
  - 확인창은 연결 입력 묶음(첫 그리기 뒤)이 있어야 열린다.
  - 분리는 묶음을 놓기 전에 닫는다.
  - 그래서 연결이 없을 때 `IsModalOpen`이 거짓인 것도, 분리 때 닫는 것도 같다.
- 닿지 않는 차이 하나: 옛 메인 미배치 안내는 묶음 정지 뒤 안내가 없으면 예외였다. 지금은 배치 진입이 안내 객체를 직접 갖는다. 정지 전에 확인창을 닫고 수신 창구를 놓으므로 정지 뒤 진입 요청·응답은 오지 않는다.

**시험:** 바꾼 것 없음.
- 조정자·확인창 클래스·수신 창구 서명이 그대로다.
- `CreateSceneInput`·`new CombatSceneInput`을 직접 부르는 시험이 없다(2026-10-25 grep).
- 옮긴 예외 문구를 단언하는 시험도 없다.

**계획과 다른 점(넷, 세부)**
1. Vb3.5의 "묶음 `Stop`이 확인창을 닫는다" 대신, 닫기를 조정자의 지금 자리에 남겼다. 분리 때 확인창 닫기가 재생 끊기보다 앞인 순서를 지키려는 것이다.
2. 확인창 캐스트는 조정자가 아니라 입력 Factory에 있다(배치 진입을 거기서 만들어서). Vb3.6에 적은 "`_textPresentation`은 캐스트에 쓰려고 남김" 대신 입력 Factory에 넘기려고 남긴다.
3. 묶음의 `RouteCommand`·`Notices`를 지웠다. 쓰던 곳이 조정자의 콜백 넷뿐이었다.
4. `CombatLoadedScene.Load`는 스테이지 키 값 대신 표시 값 읽기를 받는다. 옛 순서(배경을 세운 뒤 키를 읽음)를 지키려는 것이다.
   - 보충(V3-c 검토): 생성자는 장면 생성 계약 하나만 받는다. 표시 표는 `Load`가 받고, 오류 경계는 받지 않는다(계획은 셋을 생성자로).

빠뜨린 다른 점 둘(V3-c 검토가 찾음, 세부):
5. `CreateSceneInput`에 더한 인자는 둘(`isInputReady`·`afterDeploymentSubmit`)이다. 계획 Vb3.5는 넷(확인창 표시·편성 읽기·진입 판단·맞춤 콜백)이었다. 확인창 표시는 이미 넘기던 텍스트 표시에서, 편성은 이미 넘기던 Buffer에서 꺼내고, 진입 판단은 배치 진입 안에서 만든다.
6. 입력 연결의 수명 판정 인자 이름이 `isSceneActive`·`isModalOpen` → `canReceive`·`isConfirmOpen`이고, 버튼 입구는 `Action` 속성이다.

**다른 사람 줄(HEAD `35562e96` blame, `CombatViewSystem.cs`)**
- 현강님
  - 필드 `:129`
  - 확인창 생성 `:264-265`
  - 닫기 `:679-680`·`:722-723`·`:881`
  - 진입 갈래 `:1092`
  - 맞춤 `:1122`
  - 콜백 넷 `:1131-1157`(지움)
- 천명님
  - `_layers` 필드 `:100`
  - 옮긴 메서드의 중괄호 줄 `:856`·`:873`·`:910`·`:915`·`:922`·`:934`

**새 파일(커밋 때 `.meta`와 함께):** `Input/CombatInputConnection.cs`, `Input/ActionButtons/CombatDeploymentEntry.cs`, `CombatLoadedScene.cs`.

**Unity에서 볼 것**
- 컴파일
- EditMode 시험: `CombatViewSystemTests`(입력 연결·확인창·씬 연결 계열), `CombatInputReceiverTests`, `CombatDeploymentConfirmationTests`, `CombatDisplayReuseTests`, `CombatMovementViewTests`, `HojinCombatAttackDragIntegrationTests`, `FrameworkRuntimeCompositionTests`, `TutorialInputTests`, `StageMapCombatEntryBootSmokeTests`
- 재생:
  - Boot부터 전투 진입: 배경 맵이 보이는지
  - 배치 국면: 진입 키·버튼, 미배치 확인창(예·아니오), 메인 미배치 안내
  - 전투 국면: 공격·턴 종료·복귀 버튼, 포커스 잃기
  - 포기 확인 뒤 입력 닫힘
  - 결과 뒤 Layer 전환

### Phase V3-c — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W11 끝

사용자 "V3-c 진행해". V3-a·V3-b의 Unity 확인 여부는 듣지 못했다.

**바꾼 것**
- 새 `V/CombatViewConnection.cs`(+ `.meta`, 루트, Q-V22 A): 씬 연결 한 번의 상태다.
  - 생성자(표시·전투 번호): 표시, 입력 표면, 전투 번호
  - `BeginUse`: 정보창 창구·덱 호버(첫 그리기 전)
  - `AttachInput`: 연결 입력 묶음 + HUD 아이템 해제 연결
  - `MarkShown`·`Shown`: 첫 그리기 성공
  - `BindRoot`·`UnbindRoot`: BattleChanger 입력 연결과 루트 구독
  - `StopInput`: 옛 `StopInteraction` 순서(덱 호버 → HUD 아이템 해제 → 묶음 정지)
- `V/Input/ActionButtons/CombatInputSurfaces.cs`: `Root` 읽기 하나를 더했다. 씬 루트는 표면 하나가 갖는다.
- 조정자 `CombatViewSystem.cs`: 1058 → 1039줄(HEAD 1159), 필드 33 → 26(HEAD 39).
  - 연결 수명 필드 여덟(`_display`·`_surfaces`·`_input`·`_inspection`·`_deckHover`·`_subscribedRoot`·`_battleShown`·`_battleInstance`)이 `_connection` 하나가 됐다.
  - `TakeDisplay`의 마지막 문장이 연결 객체 만들기다(소유권 이전, TP-23).
  - 판정식(`IsAttached`·`IsSceneActive`·`HasPendingPresentation`)의 표시 연결은 `_connection != null`이다.
  - `ReadFacts`는 연결 쪽 사실을 연결 객체에서 읽는다. 연결이 없으면 지금과 같은 기본값이다.
  - 서비스 멤버 `DeckHover`·`DeckPlacement`·`ItemInput`과 시험 입구는 서명 그대로다(P22·P25).
  - 지운 것: `StopInteraction`·`UnbindItemUnequip`(연결 객체로), `EndConnectionState`의 첫 그리기·전투 번호 되돌리기(연결과 함께 버림)
- 시험(Q-T7 a): `HojinCombatAttackDragIntegrationTests.cs:129-132`의 리플렉션 경로를 `_connection`으로 맞췄다(`Input.Interaction`·`Display`). 확인 내용은 그대로다.

**정리 순서:** 계획의 순서 표 1~9 그대로다. 단계마다 `_connection`을 다시 읽는다. 정리 콜백 안의 재진입 `Detach`에서 옛 조정자(단계마다 필드를 다시 읽음)와 같게 행동하려는 것이다.

**동작:** 같다. 검토 셋이 HEAD와 문장 단위로 대조했다. 보이지 않는 차이 넷이 있다.
1. 완료 확인·루트 구독의 "같은 연결" 판정이 표시 참조에서 연결 참조로 바뀌었다. 같은 가짜 표시를 다시 붙이는 시험 조립에서만 다르고, 지난 확인을 무시하는 쪽이다(더 엄격). `CompletionConfirmation_IsOnceAndInvalidAfterDetach`의 기대와 맞는다.
2. 입력 생성 안의 재진입 `Detach` 뒤에는 첫 그리기 성공을 세우지 않고 멈춘다. 옛 코드는 분리된 상태에 깃발을 세우고 Layer를 맞췄다. 닿지 않는 경로이고, 검토 낮음을 반영한 것이다.
3. 확인창 닫기가 예외로 실패해 창이 남으면, 옛 코드는 다음 연결에서도 그 창을 모달로 봤다. 지금은 다음 연결이 그 창을 모른다. 정리 실패 경로에서만 생겨 받아들였다.
4. 버튼 대리자는 서비스를 받을 때의 수신 창구에 고정된다. 옛 람다는 누르는 순간의 필드를 읽었다. `BindServices`는 한 번만 불리므로 차이가 없다.

**검토(새 에이전트 셋, W11 묶음 = V3-b + V3-c):** 높음 0, 중간 3(모두 문서), 낮음 열여덟(셋이 겹친 것 포함).
- 반영(코드)
  - 수신 창구 생성을 `BindServices` 맨 끝(옛 자리)으로 되돌렸다. slot·layers 검사는 옛 순서다. 중간 인자가 없어 실패하면 입력 연결을 남기지 않는다(셋 공통 낮음, V3-b 몫).
  - 입력 연결이 표현 정리 입구를 생성 때 받는다. `Apply(desired)`가 호출마다 대리자를 만들지 않는다.
  - `ShowBattle`의 재진입 판단(위 차이 2)
  - `Detach`는 연결이 없으면 바로 끝난다. 두 번째 null 판단은 확인창 닫기 안의 재진입용으로 남기고 주석을 달았다.
  - `SyncActionButtons`는 연결을 한 번만 읽는다.
  - 낡은 주석 열한 곳: 조정자(객체 수·`Detach` 설명·구독 주석), 연결 객체(`UnbindRoot`·`BeginUse`의 까닭), 연결 입력 묶음 넷, 입력 표면 둘, 입력 Factory 하나
- 반영(문서)
  - 중간 M1 루트 소유 방향 → 설계 §4.2 구현 메모
  - 중간 M2 다른 사람 줄 → 이 Phase 계획 줄, Q-T7 a, 설계 §11
  - 중간 M3 조정자 크기 → 설계 §4.2 "실제"·§10
  - V3-b 기록 보충: 다른 점 둘 더, 4번 보충, "필드 여덟"
- 남김
  - `Detach`의 `_connection?.`·`_inputConnection?.`: 단계마다 다시 읽는 재진입 의미를 지키려는 것이다(주석).
  - `CombatLoadedScene.Load`가 값 읽기 전체를 받음(V3-b 다른 점 4)
  - 확인창 캐스트 지식 두 곳(조정자 검사·입력 Factory 캐스트): V3-d에서도 남겼다(V3-d 기록 "남김").
  - 입력 생성 서명 16인자 세 벌 → V3-d 재료 묶음(해결)
  - 배치 진입 호출 자리 여섯
  - 정지 뒤 메인 미배치 안내 경로, 배치 진입의 판단(조정자의 지금 사실)·제출(자기 연결 Router): 모두 닿지 않는다.

**계획과 다른 점**
1. **루트 소유 방향(설계 §4.2 구현 메모, 결정 A — 10-25 사용자 "추천대로 하고 V3-d 진행해").**
   - 설계·Vc3.1~3.2: 연결 객체가 루트를 갖고, 표면은 만들 때 루트를 받는다.
   - 구현: 표면이 루트를 갖고(`Root`), 연결 객체는 구독을 풀 때 그것을 읽는다. 표면은 지금처럼 구독 순간에 루트를 받는다.
   - 까닭: 만들 때 받으면 첫 그리기 실패 때의 `Hide`·`Sync`가 아직 묶지 않은 루트 UI를 건드려 동작이 달라진다. 루트 필드가 하나가 되는 것은 같다.
2. 연결 생성자는 (표시, 전투 번호)를 받는다. 루트는 `BindRoot(root, receiver, focusLost, disabled)`로 받는다.
3. 새 멤버 `BeginUse`·`MarkShown`을 더했다. 덱 호버·정보창은 조정자가 만들어 넘긴다. 덱 호버를 만들려면 조정자의 Buffer·오류 경계가 필요하다.
4. `StopInteraction`을 지웠다(연결 객체의 `StopInput`).
5. 조정자 크기가 예상보다 크다: 1039줄·필드 26(예상 약 850·약 18). 원인은 설계 §4.2 "실제".

**다른 사람 줄(HEAD `35562e96` blame, 이 Phase 몫)**
- 현강님
  - 덱 호버 필드·서비스 멤버 `:119-120`
  - 생성 `:520-521`
  - 해제 `:739-740`
- 천명님: `_battleInstance` `:85`
- 호진님: 시험 `HojinCombatAttackDragIntegrationTests.cs:130`(`:129`는 우리 줄)
- V3-b 몫은 V3-b 기록과 같다. 천명님 `}` `:934`는 diff가 `:1130`의 같은 `}`로 맞춘다.

**새 파일(커밋 때 `.meta`와 함께):** `CombatViewConnection.cs`. W11 새 파일은 V3-b 셋과 합쳐 넷이다.

**Unity에서 볼 것**
- 컴파일
- EditMode 시험
  - `CombatViewSystemTests`(Attach·Detach·첫 그리기 중 재진입·루트 이벤트 구독/해제·완료 확인·`StopInput`)
  - `HojinCombatAttackDragIntegrationTests`
  - `CombatDisplayReuseTests`, `CombatMovementViewTests`
  - `CombatInputReceiverTests`, `CombatDeploymentConfirmationTests`
  - `FrameworkRuntimeCompositionTests`, `TutorialInputTests`
  - `StageMapCombatEntryBootSmokeTests`, `LobbyBootSmokeTests`
- 재생
  - 전투 진입
  - 덱 카드 호버·배치 Drag
  - 가방 아이템 장착·HUD 우클릭 해제
  - BattleChanger 버튼, 창 포커스 잃기
  - 결과 화면 확인 → 이탈 → 다시 전투 진입(두 번째 연결)

### Phase V3-d — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W12 끝

사용자 "추천대로 하고 V3-d 진행해"(V3-c 루트 방향 A). V3-a~V3-c의 Unity 확인 여부는 듣지 못했다.

**바꾼 것**
- W12-1 입력 생성 재료 묶음
  - 새 `V/Input/CombatSceneInputSources.cs`(+ `.meta`): 서비스 수명 재료 열다섯이다. 표시 표·상태·캐릭터·입력 Buffer 둘·사실 공급자 둘·UI 수신 조건·막힘 알림·오류 경계·오디오·재고 읽기·텍스트 표시·배치 진입 재료 둘이고, 모두 필수다.
  - 서명 세 벌(생성 계약·운영 어댑터·정적 Factory)이 `CreateSceneInput(display, sources)` 하나가 됐다.
  - 조정자는 첫 입력 생성 때 한 번 만든다(`??=`, 이름 있는 인자). `InitData`·`BindServices`가 다시 불리면 묶음을 버린다.
  - 조정자 필드 `_readPathFacts`·`_readInputFacts`를 지웠다. 두 공급자는 재료를 만드는 자리의 람다가 됐다.
  - 조정자: 1041 → 1057줄(이름 있는 인자 열다섯 줄), 필드 26 → 25.
- W12-2: 배치 Drag 생성자에서 쓰지 않는 `requests` 인자와 그 검사를 걷었다. 입력 Factory 한 줄을 맞췄다.
- W12-3: 주체 막기 규칙은 `CombatSubjectBlocking.Blocks` 하나다.
  - 겹침 답은 함수의 셋째 인자로 받는다. 좁은 계약 `ICombatSubjectOverlap`(`AllowsOverlap`·`AllowsOverlapFor`)이고, 걸음 재생기만 넘긴다.
  - 반응·조각 유닛 실행은 지금처럼 두 인자로 부른다(사건 완료 뒤 겹침 없음). 활동 계약과 두 재생기는 바뀌지 않았다.
  - 걸음 재생기가 활동 계약(레인 상태, 동시 사망은 `!IsActive`)과 겹침 계약을 구현한다. `MotionBlocks`를 지웠다.
  - 처음에는 활동 계약에 멤버 둘을 더했다가, 사용자 지시("W12-3 고친 후에 V3-e 진행해")로 계획대로 인자로 받게 고쳤다.
- W12-4: 기차 판별은 사건 형식 표의 `CombatEventTypeTable.IsRailway` 하나다. 소리 표와 재생 조정이 함께 부른다.
- W12-5: 가시밭 외형의 프리팹이 필수다.
  - 검사는 `ThornFieldVisualData.Validate()`이고, 표 생성 때 부른다(레일웨이 표와 같은 꼴).
  - 표시 Factory는 늘 가시밭 표시를 만든다. 표시·재생 조정의 null 허용과 "프로필 없음" 예외를 걷었다.

**동작:** 운영은 같다.
- 검토가 HEAD와 대조했다:
  - W12-3: 프로필 셋 × 사건 넷 표
  - W12-4: 기믹 줄 경로 함수
  - W12-1: 재료 열다섯의 순서·쓰는 자리
- W12-5만 다르다: 프리팹 없는 DB가 가시밭이 나오는 전투가 아니라 DB를 만들 때 실패한다(계획대로). 운영 에셋에는 `ThornCactus.prefab`이 있다.

**시험:** 우리 `DataBaseAssetTests:619` 주석 한 줄만 고쳤다. W12-3을 인자로 고친 뒤 `CombatPieceRunPlayerTests`의 가짜는 HEAD와 같다. 가시밭 필수로 깨지는 시험은 없다. 모두 실제 DB 값을 쓰고, 표시·재생을 Factory 없이 만드는 시험이 없다.

**검토(새 에이전트 하나, W12 묶음):** 높음 0, 중간 1, 낮음 11.
- 반영
  - 중간 M1: V3-c 기록에 "확인창 캐스트 지식 두 곳 → V3-d"라고 적었으나 V3-d가 줄이지 않았다. 기록을 고쳤다(아래 남김).
  - 재료 생성을 이름 있는 인자로 바꿨다. 같은 형식 인자 두 쌍을 바꿔 넣어도 컴파일러가 잡지 못하기 때문이다.
  - 두 입구가 다시 불리면 재료를 버리게 했다.
  - 기차 판별 두 곳을 표 하나로 모았다.
  - 가시밭 검사를 Data `Validate`로 옮기고, 문구를 표 파일과 같은 영어로 맞췄다.
  - 가시밭 감싸개 셋을 메서드 그룹으로 바꿨다.
  - 쓰지 않는 using을 지웠다.
  - 낡은 주석 다섯 곳을 고쳤다.
- 남김
  - 확인창 캐스트 지식 두 곳(조정자의 운영 입구 검사 `:173`, 입력 Factory 캐스트): 검사는 서비스를 받을 때 일찍 실패하려는 것이고, 캐스트는 쓰는 자리 하나다.
  - (해소) 반응·조각·가짜가 늘 false인 멤버 둘을 구현하던 ISP 낮음은 W12-3을 인자로 고쳐 없어졌다.
  - `CreateSceneInteraction` 10인자: 시험이 부르지 않는다는 지적이 있었다. 계획대로 정적 입구를 두었고, W9 뒤에 다시 본다.
  - 사건 Presenter의 가시밭 선택 인자: W9-3 몫이다.
  - `CombatMortarAssetTests:152` 단언 문구의 "선택적"은 문구라 두었다.
  - 새 `.meta`에 MonoImporter 절이 없다(V3-b 파일들과 같음). Unity가 가져온 뒤 다시 쓸 수 있으니 그 뒤에 커밋한다.

**계획과 다른 점**
1. Vd3.2: 조정자 필드 둘을 지웠다. 재료 null 검사를 더했다. 계약·어댑터의 쓰지 않는 using 일곱 줄을 지웠다.
2. Vd3.4: 계획대로 공유 함수가 겹침 판단을 인자로 받는다. 인자는 좁은 계약(`ICombatSubjectOverlap`)이다. 겹침 답이 유닛마다라 불리언으로는 못 넘기고, 대리자로 넘기면 매 질문 할당이 생기기 때문이다.
   - 처음 구현(활동 계약에 멤버 둘)은 사용자 지시로 고쳤다.
3. Vd3.5: 현강님 using `:6`도 바꿨다(계획은 `:113`만 적음). 판별은 표의 새 `IsRailway`를 부른다.
4. Vd3.6
   - 검사 자리는 Data `ThornFieldVisualData.Validate()`이고, 그것을 표(Queries)가 부른다.
   - SO 툴팁·doc을 고쳤다.
   - 재생 조정 `CombatSceneEventPlayback`의 null 허용도 걷었다.

**다른 사람 줄(HEAD `35562e96` blame):** 현강님 `CombatEventSounds.cs:6`(using 바꿈)·`:113`(판별식). `PlacementDragInteraction.cs:57`(현강님)은 글자가 그대로다. 지운 인자 줄 `:54`와 검사 `:59-61`은 우리 줄이다.

**새 파일(커밋 때 `.meta`와 함께):** `Input/CombatSceneInputSources.cs`.

**Unity에서 볼 것**
- 컴파일
- EditMode 시험
  - `CombatPieceRunPlayerTests`(주체 막기)
  - `CombatSceneEventPresenterTests`·`OrderedCombatSchedulingTests`·`CombatStepContactGateTests`(걸음·반응 겹침)
  - `CombatEventSoundsTests`·`CombatEventRoutesTests`(기차 소리·경로)
  - `DataBaseAssetTests`·`CombatMortarAssetTests`(가시밭)
  - `CombatViewSystemTests`·`CombatDisplayReuseTests`·`CombatMovementViewTests`(입력 생성)
  - `PlacementDragInteraction` 계열(`CombatInteractionTests` 등)
- 재생
  - 배치 Drag
  - 넉백 여러 대상·관통 피격·기차 동시 사망(겹침)
  - 기차 소리
  - 가시밭이 생기는 공격(CactusCannon)

### Phase V3-e — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W8 앞(검토는 V3-f 끝)

사용자 "W12-3 고친 후에 V3-e 진행해" → (가시밭 문의는 사용자가 원인을 찾음) → "문제 찾았으니까 기존 작업 이어서 진행해". V3-a~V3-d의 Unity 확인 여부는 듣지 못했다.

**바꾼 것(운영)**
- 공격 재생 `CombatAttackPlayback.cs`(636 → 551줄)
  - 걷은 것: Ballista 무기 상태 셋, `HasBallistaWeapons`, `TryBeginReload`, `TryBeginBallista`, `ResetBallistaWeapons`
  - Tick의 무기·재장전 갈래, `IsComplete`·`BlocksNextEvent`의 재장전 항목, `TryBegin`의 Ballista 갈래도 걷었다.
  - Ballista가 `TryBegin`에 오면 false였다(코드 모션 없음, DB 검사가 클립을 필수로 봐서 오지 않음). **고침(W8 검토):** "지금처럼"은 틀렸다. HEAD는 무기가 없으면 예외였다. V3-f에서 Ballista 전략이 예외를 던지게 해 HEAD처럼 실패를 드러낸다.
- `Presentation/Attack/CombatBallistaAnimation.cs`(+ `.meta`)를 지웠다.
- 턴 처리기 `CombatTurnEventHandler`: 재장전 대기 갈래와 공격 재생 인자를 걷었다. 턴 끝은 늘 간격 전에 보인다. 사건 Presenter의 생성 줄 하나도 맞췄다.
- 표시 계약 `BallistaWeapon`, `FieldUnitView`의 풀 반환 무기 초기화·계약 구현, 창구의 무기 조회 둘(`GetBallistaWeapon`·`TryGetBallistaWeapon`)을 걷었다.
- 궤적: Ballista 출발점은 유닛 위치를 발사 높이로 올린 점 하나다. 무기 갈래와 "클립도 무기도 없으면" 예외를 걷었다.
- `BallistaWeaponView`: 직렬화 칸 둘(`bolt`·`bowString`)만 남긴 빈 컴포넌트다(TP-25). 경고 억제를 달고, 요약에 "쓰지 않음, 프리팹 컴포넌트라 남김"을 적었다.
- 공격 표시 SO·Data: 시위·재장전 칸 넷과 그 검사·Data 멤버를 걷었다. 에셋 YAML의 옛 키는 Unity가 다시 저장할 때 지운다.
- 클립 규칙 `CombatUnitAnimationRules.ValidateUnit`: "Ballista 공격 표시면 공격 클립 필수"를 더했다(P27). 운영 Ballista 외형은 `CombatUnitAnimation_Ballista`에 공격 클립이 있어 통과한다.
- "임시 예외" 문장 고침: 클립 규칙 둘, 공격 처리기 셋, 클립 조립, `FirePiercingShot`, 시작 질문 `AttackBusy`, 공격 재생기 주석 셋, 사건 Presenter 주석 둘

**동작:** 운영은 같다.
- 운영 Ballista 외형은 늘 클립 길이다. `BallistaWeaponView`가 붙은 프리팹은 참조 없는 `EnemyUnit_Ballista.prefab` 하나뿐이다.
- 그래서 무기를 찾지 못해 재장전 대기·턴 끝 간격 뒤 표시가 일어나지 않던 지금 동작과 같다.
- 클립 없는 Ballista 외형은 부트의 DB 생성에서 실패한다(계획대로).

**시험(우리 시험만, 다른 사람 줄 없음)**
- `CombatBallistaAttackTests.cs`(668 → 129줄, 사용자 결정 "사례만 지우고 고정물 남김")
  - 시험 사례 23개를 지웠다. 반응·사망·피격 이펙트 시험이 빌려 쓰는 장면 준비(SetUp·TearDown·도우미)만 남겼다.
  - 공격자는 공격 표시 없이 둔다. Ballista 공격 표시는 클립을 요구하고, 빌려 쓰는 시험은 공격을 재생하지 않기 때문이다.
  - 무기 컴포넌트 줄, 쓰지 않게 된 도우미(`Attack`·`Damage` 등)·필드 셋(`_bullet`·`_settings`·`_visual`)도 지웠다.
  - 천명님 줄 `:63-64`는 글자가 같아 그대로 남았다(Q-T7 b는 쓰지 않음).
- `CombatBallistaLifecycleTests.cs`(+ `.meta`)를 지웠다(코드 길만 봄).
- `CombatBallistaAssetTests.cs`(사용자 결정 "값 단언만 남김"): 시험 하나 `NativeBallista_ProfileBinding`이다. 시위·재장전 값 단언과 코드 길 발사·재장전 부분, `tilted` 사례를 지웠다.
- 의존 시험의 코드 길 사례 넷을 지웠다(사용자 결정 "지움"):
  - 반응 `OrderedBallista_ContactStartsHit_…`
  - 사망 `BallistaAttackerDeath_…`·`OrderedBallista_DeathStartsAtContact_…`
  - `ConcurrentDeath_RejectsHitMovementAndAttackOverlap`의 `TestCase(2)`와 그 갈래, 사망 시험의 `Attack` 도우미
- `CombatAttackPresentationSettingsTests`: 걷은 칸을 보던 `TestCase` 넷과 "Ballista 아니면 재장전 칸 무시" 두 줄을 지웠다(계획이 놓침).
- `CombatUnitPresentationWindowTests`: 무기 조회 단언 셋과 가짜 멤버 하나를 지웠다. `CombatPieceRunTests`의 가짜 멤버 하나도 지웠다.

**계획과 다른 점**
1. Ve3.10은 "`CombatBallistaAttackTests`를 파일째 지움"이었다. 반응·사망·피격 이펙트 시험 셋이 그 클래스를 고정물로 빌려 써서, 사례만 지우고 고정물을 남겼다(사용자 결정).
2. 의존 시험의 코드 길 사례 넷을 지웠다(계획에 없음, 사용자 결정).
3. 계획이 놓친 시험 둘을 맞췄다: `CombatAttackPresentationSettingsTests`(걷은 칸), `CombatUnitPresentationWindowTests`(무기 조회 단언).
4. 고정물의 `Attack` 도우미도 지웠다. 결정 때는 남긴다고 했으나 지운 넷이 유일한 사용처였다.
5. 에셋 시험 이름 `NativeBallista_ProfileBindingAndFiniteRepeatedShots` → `NativeBallista_ProfileBinding`(사용자 결정 "값 단언만 남김"의 결과).
6. Fired 완료 정책(`CombatHitCompletion.Fired`·`CombatHitGate.NotifyFired`)을 운영에서 쓰는 곳이 없어졌다. 사용자 결정으로 남기고 보고한다(W8 전략 단계에서 다시 봄, 쓰는 곳은 `CombatHitGateTests`뿐).

**보고만**
- `CombatBallistaAssetTests`의 값 단언 둘은 이미 맞지 않는다. 민형님 `ca414aa1`(이펙트 바인딩)이 `CombatAttack_Ballista.asset`을 바꿨기 때문이다(속도 12 → 30, 투사체 프리팹 `e93bf59f…` → `48189758…`). → 사용자 지시("test 기대값 맞추고, V3-f 진행해")로 기대값을 지금 에셋 값(속도 30, 프리팹 `Render/Effect/Arrow/Arrow.prefab` `48189758…`)으로 맞췄다. 새 프리팹에도 MeshFilter·MeshRenderer가 있고 MonoBehaviour가 없어 나머지 단언은 그대로 성립할 것으로 본다.
- `EnemyUnit_Ballista.prefab`은 참조 없는 에셋이라 지우지 않는다. 빈 `BallistaWeaponView`가 남는다(불필요 낮음, 손패 파일과 같은 꼴).

**다른 사람 줄(HEAD `35562e96` blame):** 없다.

**지운 파일(커밋 때 `.meta`와 함께):** `Presentation/Attack/CombatBallistaAnimation.cs`, 시험 `CombatBallistaLifecycleTests.cs`.

**Unity에서 볼 것**
- 컴파일(특히 `BallistaWeaponView`의 경고 억제, 지운 멤버를 부르는 곳)
- EditMode 시험
  - `CombatUnitReactionTests`·`CombatUnitDeathTests`·`CombatHitEffectSceneTests`(고정물)
  - `CombatClipShotTests`(클립 Ballista)
  - `CombatUnitAnimationSettingsTests`(Ballista 클립 필수)
  - `CombatAttackPresentationSettingsTests`·`CombatUnitPresentationWindowTests`
  - `CombatAttackTrajectoryTests`·`AttackTrajectoryInteractionTests`(궤적)
  - `DataBaseAssetTests`(부트 DB)
  - `CombatBallistaAssetTests`: 기대값을 맞춘 뒤 통과할 것이다.
- 재생
  - Ballista 공격(관통 대상마다 피격·접촉 이펙트), 공격 모드 궤적
  - Ballista 턴 끝(간격 전에 표시)

### Phase V3-f — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W8 끝

사용자 "test 기대값 맞추고, V3-f 진행해". 먼저 `CombatBallistaAssetTests`의 기대값을 지금 에셋(속도 30, `Arrow.prefab` `48189758…`)에 맞췄다(V3-e 보고분). V3-a~V3-e의 Unity 확인 여부는 듣지 못했다.

**바꾼 것**
- 새 전략 일곱(`V/Presentation/Attack/`, + `.meta`, 형식은 여덟)
  - 맥락 `ICombatAttackStyleContext`: 자세 대여, 곡사 무기 조회, 투사체 풀·발사 셋, 애니메이션 시작(Gate), 모두 해제·대상 해제, 접촉 이펙트 감싸기 둘·내기, 접촉 진행률 둘, 쓸 수 없음 취소
  - 기반 `CombatAttackStyle`: `Kind`, `TryTarget`(비가상 입구 + `ResolveTarget`), `DrawsArc`, `TryResolveTrajectoryOrigin`, `Segment`, `SupportsFacingMotion`, `TryBeginCodeMotion`, `FireShot`, `ClipRelease`, 자세 복원 도우미 `StartWithPose`
  - 같은 파일의 `CombatDirectAttackStyle`: 근접·직선 공통 코드 모션(옛 `TryBeginDirect`)
  - 종류 넷 `CombatMeleeStyle`·`CombatLinearStyle`·`CombatMortarStyle`·`CombatBallistaStyle`
  - 표 `CombatAttackStyles.Of(kind)`: 만들 때 음수·중복·누락을 검사한다(P20, TP-28).
- 공격 재생 `CombatAttackPlayback.cs`(551 → 446줄, HEAD 636): 시작·정면 모션·출처 발사가 전략을 부른다. `TryBeginDirect`·`TryBeginMortar`·`FirePiercingShot`을 지웠다. 맥락을 명시적으로 구현한다. 수명과 접촉 진행률 계산은 남았다.
- 발사 기하 `CombatShotPath`(81 → 55줄): 목표점 switch를 지웠다. 종류와 무관한 기하 넷만 남았다.
- 공격 방향 `CombatAttackFacing.For`: 전략의 목표점을 쓴다(서명 그대로).
- 궤적 `CombatAttackTrajectory`(318 → 270줄): 출발 기준점·`Segment`·곡선 여부를 전략에 묻는다. `Shot`은 종류 대신 전략을 갖는다.
- 공격 처리기: 클립 해제 입구는 전략의 `ClipRelease`다(Ballista 갈래·`ReleaseNone` 삭제).
- 창구: `TryGetMortarPresentation` → `TryGetMortarWeapon(id, out weapon)`. 종류 검사와 연출 값을 걷어 곡사 무기 조회만 남겼다(계획 Vf3.8, 검토 중간 반영). 부르는 곳은 곡사 전략 둘이고, 연출 값은 이미 갖고 있다.

**동작:** 운영은 같다. 검토 1이 종류 넷 × 코드 모션·정면 모션·출처 발사·궤적·방향·해제를 문장 단위로 대조했다. 대조 항목은 풀 → 자세 순서, 곡사 자세의 취소 콜백 없음, 완료 정책, 직선 위쪽 `Vector3.up`, Ballista 대상별 정렬·첫 해제 뒤 hit다. 닿지 않는 차이 셋:
1. 모르는 종류 값은 이제 `ArgumentOutOfRangeException`이다. 옛 코드는 갈래마다 false·직선 해석·예외였다. SO가 열거 값만 받아 오지 않는다.
2. 클립 없는 Ballista의 코드 모션은 예외다(새 전략 override). 옛 코드는 무기가 없어 예외였고, V3-e 직후는 false였다. 클립 없는 Ballista의 궤적은 올린 유닛 위치에서 그린다(옛 코드는 무기 없음 예외). 둘 다 DB 검사(P27)가 막는다.
3. 창구의 곡사 무기 조회는 표시가 있는데 무기가 없으면 예외다(옛: 연출 값이 없으면 false).

**시험(우리 시험만)**
- `CombatShotPathTests`·`CombatAttackFacingTests`: `CombatShotPath.TryTarget` 호출을 `CombatAttackStyles.Of(kind).TryTarget`으로 바꿨다. 확인 내용은 그대로다.
- `CombatUnitPresentationWindowTests`: 첫 조회는 `TryGetMortarWeapon`(표시 없음 → false)으로 바꿨다. "표시는 있고 연출 값 없음 → false" 단언 하나는 지웠다. 무기 조회가 연출 값을 보지 않아 동작이 바뀌었기 때문이다(10-22 Q1 방침).
- 이름·주석: `ConcurrentDeath_RejectsHitMovementAndAttackOverlap` → `ConcurrentDeath_RejectsHitAndMovementOverlap`(V3-e에서 공격 사례를 지움). `CombatClipShotTests`·`CombatUnitAnimationSettingsTests`의 "임시 예외" 주석·단언 문구도 고쳤다.

**검토(새 에이전트 셋, W8 묶음 = V3-e + V3-f):** 높음 0, 중간 2, 낮음 27(셋 겹침 포함).
- 반영
  - 중간 M1(검토 3): 창구를 곡사 무기 조회만으로 바꿨다(위).
  - 맥락의 `HitAll`을 아무도 쓰지 않았다(검토 1·2·3). `ClipRelease`가 맥락을 받아 `HitAll`을 쓰게 했다. 설계 표의 `ClipRelease(context, …)`와도 맞는다.
  - Ballista 코드 모션은 조용한 false 대신 예외다. V3-e 기록의 "지금처럼 false"도 고쳤다(검토 1·3).
  - 직선 꼴 `Segment`를 기반에서 직선 전략으로 옮겼다. 기반은 "궤적 없음" 예외다(검토 3).
  - 자세 복원 try/catch 두 벌을 `StartWithPose` 하나로 묶었다(검토 3).
  - 곡사 코드 모션: 늘 참인 `Succeeded` 검사를 걷고, 인자 `attackerId`·연출 값을 그대로 쓴다(검토 2·3).
  - 낡은 주석 `CombatProjectileFlights:72` "임시 예외"(Ve3.9가 놓침), 시험 주석 셋, 쓰지 않는 using 하나. 설계 §11 천명님 줄 문구도 고쳤다.
- 남김(보고)
  - 중간(검토 2): V3-e에서 지운 넷 때문에, 클립 Ballista를 Ordered로 재생할 때 접촉 순간 반응·사망 겹침과 공격 중 동시 사망 거절을 통합 수준에서 보는 시험이 없다. 남은 것은 `CombatClipShotTests`의 발사 단위 시험이다. 새 `[Test]` 금지라 보고만 한다.
  - 맥락 멤버 15개 중 근접은 셋만 쓴다. 종류 이름이 붙은 멤버(곡사 무기·직선/관통 진행률·곡사/관통 발사)가 있어, 종류를 더하면 맥락도 바뀔 수 있다(설계 §10 예상 "맥락 계약이 두꺼워질 수 있다").
  - 전략이 협력자를 두 갈래로 받는다: 맥락(공격 재생), 창구·배치 읽기(궤적).
  - "투사체를 쏘는 종류"가 DB 함수 `FiresProjectile`(처리기 분기)와 전략의 `FireShot` 기본값(근접은 예외) 두 곳에 있다. 기반 가상 기본값(궤적 없음·`Segment`·`FireShot` 예외)은 근접이 쓰지 못하는 멤버를 물려받는 꼴이다(LSP 낮음).
  - `CanUseAttackClip`은 정의된 종류에서 늘 참이다(HEAD부터).
  - 주석에 종류 목록을 베낀 곳이 있다(공격 재생 셋·궤적 하나).
  - Fired 완료 정책(`CombatHitCompletion.Fired`·`NotifyFired`)과 `AnimationEnd`는 운영에서 쓰는 곳이 없고 `CombatHitGateTests`만 쓴다. V3-e 사용자 결정대로 남긴다(전략에 옮길 갈래가 없음).
  - 새 DB 규칙("Ballista면 공격 클립 필수")을 확인하는 시험이 없다(새 시험 금지).
  - `Arrow.prefab`의 `Body` 재질은 민형님 에셋 `rock_003_final.fbx`의 내장 재질이다(보고만).

**계획과 다른 점(세부)**
1. 같은 파일에 중간 추상 클래스 `CombatDirectAttackStyle`를 더했다. 근접·직선 공통 코드 모션이다. 파일은 일곱, 형식은 여덟이다.
2. `TryTarget`은 비가상 입구(Mapper 검사)와 `protected abstract ResolveTarget`으로 나눴다. `Kind`를 더했다. `DrawsArc`는 계획 Vf3.6대로다(설계 표에는 없음).
3. `TryBeginCodeMotion` 인자는 `(맥락, 공격자, 사건, 연출 값, Mapper, hit, 소리, 완료, 세대)`다. 사건이 null이면 정면 모션이고, hit은 공격 재생이 만든다.
4. `FireShot`은 공격 재생이 계산한 위쪽·공격 방향 회전을 받는다.
5. `TryResolveTrajectoryOrigin`은 맥락이 아니라 창구·배치 읽기를 받고, 곡사 높이도 돌려준다(궤적은 공격 재생이 아니라 표시 쪽이라).
6. 맥락에 계획 목록 밖의 `PlayImpact`·`ImpactThen` 둘·`Release`·`HitAll`이 있다(Ballista 관통·클립 해제).
7. 창구는 `TryGetMortarWeapon`으로 이름을 바꿨다(위).
8. 기반 `Segment`는 예외이고, 직선 꼴은 직선 전략에 있다.

**다른 사람 줄(HEAD `35562e96` blame):** 없다. W8이 바꾸거나 지운 줄은 모두 우리 줄이다(검토 3). 천명님 `CombatBallistaAttackTests:63-64`는 글자 그대로다.

**새 파일(커밋 때 `.meta`와 함께):** `Presentation/Attack/ICombatAttackStyleContext.cs`·`CombatAttackStyle.cs`·`CombatMeleeStyle.cs`·`CombatLinearStyle.cs`·`CombatMortarStyle.cs`·`CombatBallistaStyle.cs`·`CombatAttackStyles.cs`.

**커밋에서 뺄 것(W8 변경 아님, 검토 2):** 작업 트리의 `Prefabs/Combat/*.prefab` 레이어 변경(0 → 20, `BallistaBolt.prefab` 포함), `PlayerLoadout_1`

**Unity에서 볼 것**
- 컴파일(전략 일곱, 명시적 구현 15)
- EditMode 시험
  - `CombatShotPathTests`·`CombatAttackFacingTests`·`CombatUnitPresentationWindowTests`
  - `CombatStandardAttackTests`·`CombatMortarPresentationTests`·`CombatClipShotTests`·`CombatClipAttackCompositionTests`
  - `AttackTrajectoryInteractionTests`·`CombatAttackTrajectoryTests`·`CombatAttackFeedbackTests`
  - `CombatRoarEventHandler`·포효 시험(정면 모션)
  - `CombatUnitReactionTests`·`CombatUnitDeathTests`·`CombatHitEffectSceneTests`
  - `DataBaseAssetTests`·`CombatBallistaAssetTests`
- 재생
  - 근접(Gravekeeper 코드 / Demon 클립), 직선(Sniper 코드 / Akimbo·ShotGun 클립)
  - 곡사(CactusCannon 코드·가시밭 / Cannon 클립), Ballista 클립
  - 공격 모드 궤적 넷, 보스 포효 박치기

### Phase V3-g — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W9 앞(검토는 V3-i 끝)

사용자 "V3-g,h,i 순차적으로 다 진행하고 검토는 한번에 하자, 시험은 안채워도 괜찮아".

**바꾼 것**
- 설정 표 `CombatPresentationSettingsData`에 칸 아홉을 더했다. 기본값은 지금 상수다(P28).
  - 아침 전환 레이어·카메라·색(`AfterRendering`·`Day Camera`·cyan)
  - 밤 전환 레이어·카메라·색(`BefalRendering`·`Night Camera`·magenta)
  - 바뀌는 속도 30·양 40
  - 환경 폭발 이펙트 `Boom`
  - 검사: 이름 비어 있지 않음(`ValidateName`), 속도·양 유한 양수(`ValidateTransitionRate`), 이펙트 `CombatPresentationEffectRules.IsPlayable`. 두 검사 함수는 이 클래스 안에서만 쓰여 `private`이다(검토 반영).
  - 에셋 `CombatPresentationSettings_Default.asset`에도 같은 값을 적었다(적지 않아도 기본값으로 같다).
- 민형님 `V/Playback/CombatPhaseTransitionSettings.cs`: `CreateDefault(findLayer)` → `From(settings, findLayer)`. 레이어가 없으면 예외인 검사는 그대로다.
- 표시 Factory `CombatSceneDisplayFactory.cs`
  - 전환 설정은 설정 검사 바로 뒤, 씬 객체를 건드리기 전에 만든다.
  - 배경이 있으면 카메라 이름 둘이 맵 카메라 사실에 있는지 본다(`RequireMapCamera`, 없으면 `InvalidOperationException`, 이름을 문구에).
  - 폭발 이펙트는 설정 값을 넘긴다(`EffectType` using 삭제).
- 민형님 시험 `CombatPhaseTransitionPresenterTests.cs:33·276`(Q-T7 c): 실제 DB 설정 값(도우미 `Presentation`)으로 `From`을 부른다. 확인 내용은 그대로다.

**동작:** 운영 값은 같다. 차이 둘:
1. 레이어 검사 실패가 `root.ValidateReferences`보다 먼저 난다(객체를 만들기 전).
2. 배경 맵에 전환 카메라가 없으면 이제 표시를 만들 때 실패한다. 이전에는 첫 전환(전투 진입) 때 카메라 카탈로그 조회(`CameraCatalogSystem.GetHandle`)에서 실패했다(검토 1 확인). 계획 V3-g의 "조용히 안 걸림"은 틀렸다.
   - 보스 스테이지(2101)의 배경 `StageViewProp_Boss`는 맵 정의(`Assets/Map_Map.asset` → `Assets/Map.prefab`)이고, 그 프리팹에는 버추얼 카메라가 없다. 그래서 이 스테이지는 이제 표시를 만들 때 실패한다.
   - **사용자 결정(10-25): "지금 세팅된게 임시 세팅이라 그냥 그대로 둬".** 코드도 에셋도 그대로다.

**다른 사람 줄(HEAD `35562e96` blame):** 민형님 `CombatPhaseTransitionSettings.cs:7·19·23-24·28·38-39`, 시험 `CombatPhaseTransitionPresenterTests.cs:33·276`(허락분). 민형님 Factory 공유 §25에 적는다.

**새 파일:** 없다.

### Phase V3-h — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W9 가운데

**바꾼 것**
- 유닛 외형 `UnitVisualData`에 칸 둘: `unitSoundPreset`·`attackSoundPreset`(기본 −1 = 소리 없음, 검사 −1 이상).
- 외형 에셋 열넷(외형 표에 등록된 것)에 값을 넣었다. 유닛 프리셋은 InGame 유닛 분류로 다시 계산했다(계획 표와 같다, 검토 1이 옛 switch·분류 규칙과 열넷 모두 대조).
  - 미등록 외형 둘(`UnitVisual_MainCharacter` 14·`UnitVisual_Shielder` 23)은 고치지 않았다(기본 −1).
- 설정 표에 칸 넷: 적 공용 유닛 프리셋 5, 배경음 밤 3·낮 2·완료 뒤 1(검사: 효과음 −1 이상, 배경음 0 이상). 에셋에도 적었다.
- 새 값 객체 `V/Playback/Support/CombatSoundPresets.cs`(+ `.meta`): `Unit(key, owner)`·`Attack(key)`·`Bgm(phase)`, 소리 없음 `None`·판정 `IsSound`. 표시 표 `CombatDisplayTables`가 생성자에서 한 번 만든다(`SoundPresets`, TP-26).
- 정적 `CombatEventAudio.UnitPreset`·`AttackPreset`, `CombatEventSounds.BgmPreset`을 지웠다. 소리 표·사건 소리·표시(배경음 둘)는 값 객체를 받는다.
- 포효 소리는 포효한 유닛(`BossId`)의 공격 프리셋이다. 보인 사실에서 그 유닛을 못 찾으면 소리가 없다(옛: 늘 15).
- 입력 둘(배치 Drag·공격 조준): 검토 반영으로 새 소리 창구 `Input/Interaction/CombatInputSounds.cs`(+ `.meta`, 오디오 + 프리셋, `PresetOf`·`Play`) 하나만 받는다(아래 "검토").

**동작:** 운영은 같다(같은 값). 차이:
1. 유닛 프리셋은 실행 중 분류가 아니라 DB 외형 값이다. Core는 분류 키를 유닛 정의에서 가져와 운영 DB에서는 같다(검토 1).
2. 개발 Fixture(`DevBattleInitializationSource`)는 모든 유닛을 Main으로 만들어, 그 진입에서 ShotGun 유닛 소리가 1 → 3(DB의 Heavy)이 된다. DB를 원본으로 삼는 의도와 맞다.
3. 외형 표에 없는 유닛은 −1이다. 그런 유닛은 외형 조회에서 이미 예외라 필드에 나오지 않는다.
4. 적 전용 유닛 넷(Demon·Gravekeeper·Cannon·Heavy)의 유닛 프리셋 값은 적 공용 값이 대신해 읽히지 않는다(계획 표대로 채움, 검토 낮음 — 남김).

**시험(우리 + 허락분)**
- 공용 도우미 `CombatViewSystemTests.SoundPresets`(실제 DB, 표시 표와 같은 방법).
- 우리: `CombatEventSoundsTests`(우리 줄), `CombatSceneEventPresenterTests:296·305`, `CombatStandardAttackAssetTests:125`, `CombatClipAttackCompositionTests:52`, `CombatDeathCompositionTests:47`, `StageMapCombatEntryBootSmokeTests:129-130`.
- 허락분(Q-T7 d): 현강님 `CombatEventSoundsTests` 줄, `GameAudioTests:80`, `StageMapCombatEntryBootSmokeTests:131`, 천명님 `StageMapCombatEntryBootSmokeTests:137-138`.
- 사용자 결정(10-25, 묻고 정함)
  - 현강님 `CombatEventSoundsTests.Roar_WithoutTargets_PlaysBossAttackSound`: "포효한 유닛을 보스로" — 포효 유닛 사실을 Stage1Boss 키로 둔 도우미 `Boss(id)`, 기대값 15 그대로.
  - 현강님 `GameAudioTests`의 분류 규칙 시험 넷: "분류 → 그 분류 실제 유닛" — FMOD 반복은 `분류/10`을 바로 넘김, `MainCharacter_UsesPresetZero` = Akimbo 0, `ClassPreset_MapsCoreValues` = Sniper 1·Cowboy 2·ShotGun 3·CactusCannon 4·본부 −1, `Enemy_OverridesMainAndClass` = 적 Akimbo 5. 바뀐 줄은 현강님 일곱·천명님 둘(`:52·56`)이다(검토 3, 계획 목록보다 많음).
    - 본부 사례는 처음에 없는 열거 값 `Unit_Headquarters`를 써 컴파일 오류였다(검토 1 높음) → `Structure_Headquarters`로 고침.
    - HEAD에서 이미 실패하던 `MainCharacter_UsesPresetZero`·`ClassPreset_MapsCoreValues`(Akimbo 특례 때문)는 이제 통과하는 꼴이다. `AttackPreset_UsesAuthoredOrder`의 MainCharacter 0·Akimbo 1 두 사례는 HEAD처럼 계속 실패한다(확인 내용 그대로).
  - 현강님 `CombatInteractionTests.cs:76`: 처음 결정 "건드리지 않음" 뒤 사용자 "CombatInteractionTests 고쳐주셈" → 소리 창구 인자 하나(`sounds: new CombatInputSounds(_audio, CombatViewSystemTests.SoundPresets)`). 고르는 손패 유닛 Cowboy는 프리셋 2라 확인 내용(고르기·취소 한 번씩)이 그대로 맞다.
  - 현강님 `CombatSceneEventPresenterTests.cs:529`(검토 2 높음): "기대값을 포효 유닛 값으로" — `SoundPresets.Attack(_initial.CharacterKey)`. 대기 중 소리 없음·모션 중 한 번이라는 시점 확인은 그대로다.

**다른 사람 줄(HEAD `35562e96` blame, 검토 3 + 반영 뒤 다시 셈)**
- 현강님
  - `CombatEventAudio.cs`: 공격 프리셋 switch 항목·기본값(`:76·78·81-85`)을 지움
  - `CombatEventSounds.cs:104-105`(포효 소리), `:110`(밤 배경음 호출)
  - `PlacementDragInteraction.cs:34`(`_audio` 필드 → 소리 창구), `:35`(−1 → `None`), `:57`(서명), `:204`·`:283`(소리 내기)
  - `AttackTargetingInteraction.cs:43`(필드), `:60-61`(서명, 병합 `d0ac086a` 귀속), `:133-135·138`(취소 소리)
  - `CombatSceneInputFactory.cs`: `CreateSceneInteraction` 서명(병합 귀속)·호출
  - `CombatSceneDisplayFactory.cs:162`(표시 생성 인자)
- 천명님: `CombatEventAudio.cs:63-64·67·74`(Akimbo 특례·분류/10 식을 지움)

**새 파일(커밋 때 `.meta`와 함께):** `Playback/Support/CombatSoundPresets.cs`, `Input/Interaction/CombatInputSounds.cs`.

**커밋:** 외형 SO 코드·에셋 열넷·설정 에셋·값 객체·시험은 한 커밋(에셋 없이 코드만 넣으면 프리셋이 −1).

### Phase V3-i — 2026-10-25, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음) — W9 끝

**바꾼 것**
- 새 값 묶음 `V/Playback/Support/CombatEventPresentationValues.cs`(+ `.meta`): 걸음 시간·적 간격 둘·이동 회전 시간·능력 동작 값·카메라 연출 값·뜀 높이. `From(settings)`가 설정 표에서 만들고, 값 검사(걸음 → 뜀 → 회전 → 행동 간격 → 턴 간격, 옛 Presenter 생성자 순서)를 생성자 한 곳에서 한다. 걸음 시간 조건 입구 `ValidateStepSeconds`도 Presenter에서 여기로 옮겼다.
- 새 필드 창구 `V/Playback/CombatFieldEventPorts.cs`(+ `.meta`): 필수 대리자 여덟(가시 소비·환경 효과·재생 강조·가시 몸·오브젝트 몸·오브젝트 숨김·토네이도 숨김·켜기).
- 사건 Presenter 생성자: `(units, values, audio, sounds, camera, effects, projectileRoot, ports)`, 모두 필수.
  - 걷은 것: 선택 인자 열아홉, `camera ?? new CombatCameraWindow(null, …)`, `abilityMotion ?? Instant`, 투사체 루트 대체(유닛 루트), `_effects != null` 둘, `_presentEnvironmentEffect?.`.
- 처리기 셋(공격·캐릭터·포효)·공격 재생: 카메라·카메라 연출 값·이펙트가 필수다(`?? None`·`_effects == null` 갈래 삭제). 조립 정적 함수(`ComposeHit`·`ComposeDeath`·포효 `Compose`·`CombatClipAttack`)의 선택 인자는 시험이 직접 불러 남겼다.
- 사건 소리 `CombatEventAudio`: 오디오도 필수다(`_audio == null` 갈래 삭제).
- 표시 Factory: 값 묶음·필드 창구를 만들어 넘기고, 기차 재생은 같은 카메라 연출 값(`eventValues.CameraMotion`)을 쓴다.

**계획과 다른 점**
1. 생성자에 소리 프리셋 인자 `sounds`가 더 있다(여덟 인자). 사건 Presenter가 사건 소리를 직접 만들어서다. 처음에는 값 묶음에 넣었다가 검토(둘)의 "표 조회 객체라 값 묶음과 책임이 다르다"를 듣고 **사용자 결정 "생성자 별도 인자로"**로 옮겼다. 그래서 값 검사 시험이 DB를 읽지 않는다.
2. 투사체 루트가 필수다(옛: 없으면 유닛 루트). 시험 조립은 유닛 루트를 넘긴다(같은 값).
3. 값 묶음의 자리는 계획 Vi3.1의 `Playback/` 바로 아래가 아니라 `Playback/Support/`다(네임스페이스 `…Playback.Support`). 폴더 배치 안내 §2 "설정 표에서 꺼낸 연출 값 묶음 → `Playback/Support/`"를 따랐다(V3-j에서 안내와 대조하다 찾음). 필드 창구는 사건 Presenter의 입력 계약이라 계획대로 `Playback/` 바로 아래다.

**동작:** 운영은 같다(검토 2가 옛 이름 인자 열아홉의 출처를 하나씩 대조). 시험 조립에서 꺼져 있던 것(이펙트·카메라·재생 강조)이 가짜로 켜진다. 실제 반응 에셋(`hitEffect: 4`)을 쓰는 사망 사건은 가짜 이펙트로 조각이 돈다. 단언이 바뀌는 시험은 없다(검토 2).

**시험**
- 우리 도우미(`CombatViewSystemTests`, TP-27): `EventValues`(넘기지 않은 값 = 옛 생성자 기본값), `FieldPorts`(필드 쪽은 아무것도 하지 않고 걸음 접촉 몸 조회는 예외 — 그 조립의 시험은 걸음 뒤 가시·오브젝트 사건을 만들지 않음, 검토 2 확인), `NoMapCamera`, `EventEffects(root)`.
- 사건 Presenter 조립 열둘: 우리 열(`CombatBallistaAttackTests`·`CombatClipAttackSceneTests`·`CombatEnemyPresentationTests` 셋(도우미 `Presenter`)·`CombatMortarPresentationTests`·`CombatStandardAttackAssetTests`·`CombatStandardAttackTests`), 현강님 둘 `CombatSceneEventPresenterTests:270·515`(Q-T7 e).
- 생성자 인자 검사 `CombatEnemyPresentationTests:275·305` → 값 묶음 검사로 호출만.
- `CombatClipShotTests:57`: 공격 재생에 이펙트 창구.
- `CombatPresentationSettingsDataTests:61-62`: `ValidateStepSeconds` 입구를 값 묶음으로.
- 지움(10-22 Q1, 우리 시험): `CombatEventSoundsTests.Audio_WithoutAService_DoesNothing_AndDoesNotLookUpUnits`(오디오 없는 사건 소리를 걷음).

**다른 사람 줄(HEAD `35562e96` blame):** `CombatSceneEventPresenter.cs`의 현강님 `:127`(포효 처리기 생성), 호진님 `:78·122-124·132`(토네이도 선택 인자·null 갈래·사실 처리기 생성), 천명님 `:77`(병합 귀속). 처리기·공격 재생은 우리 줄이다.

**새 파일(커밋 때 `.meta`와 함께):** `Playback/Support/CombatEventPresentationValues.cs`, `Playback/CombatFieldEventPorts.cs`.

**커밋:** Presenter·Factory·시험 열둘은 한 커밋(필수 인자로 묶임).

### W9 검토(새 에이전트 셋, V3-g + V3-h + V3-i) — 2026-10-25

높음 2, 중간 3, 낮음 아홉(겹침 포함).
- 반영
  - 높음(검토 1): `GameAudioTests:61`의 없는 열거 값 → `Structure_Headquarters`.
  - 높음(검토 2): 현강님 포효 시험 `CombatSceneEventPresenterTests:529` → 사용자 결정대로 기대값을 포효 유닛 값으로(위 V3-h).
  - 중간(검토 3): 입력 소리 경로 두 벌 — 사용자 결정 "작은 객체 하나로 묶음". 소리 창구 `CombatInputSounds`를 입력 Factory가 한 번 만들어 두 입력에 넘긴다. "오디오가 있으면 프리셋도 필수" 짝과 `preset >= 0` 판정 셋이 사라졌다. 판정은 `CombatSoundPresets.IsSound` 하나이고 사건 소리 단서 `CombatSoundCue.Effect`도 이것을 부른다. `CreateInteraction`은 오디오 대신 소리 창구를 같은 자리로 받는다(선택, 없으면 소리 없음).
  - 낮음: 프리셋을 값 묶음에서 생성자 인자로(사용자 결정, 위 V3-i), 검사 함수 둘 `private`, 필드 창구 주석이 시험 도우미의 예외를 말하게, Factory 주석의 "조용히" 고침.
- 사용자 결정으로 그대로
  - 중간(검토 1): 보스 배경(`Map.prefab`)에 전환 카메라가 없어 보스 스테이지 진입이 표시 생성에서 실패한다. "지금 세팅된게 임시 세팅이라 그냥 그대로 둬".
- 남김(보고)
  - 화면 전환 값 검사가 세 곳에 나뉨: 이름(설정 Data)·레이어(`From`)·카메라(Factory `RequireMapCamera`). 카메라 이름 비교는 `CombatMapCameras.Read`의 같은 이름 규칙과 같은 꼴이다.
  - "−1 = 소리 없음"이 DB 층(검사·`Min(-1)`·기본값)과 View 층(`CombatSoundPresets.None`·`IsSound`)에 있다. 효과음 프리셋 검사가 설정 Data 안에 있어 외형 Data가 그 형식을 부른다.
  - 적 전용 유닛 넷의 유닛 프리셋 값이 읽히지 않는다(위 V3-h 차이 4).
  - 설정 Data의 영역이 일곱이 됐다(소리·전환을 바꿀 때 같은 클래스를 고침, 관찰).
  - 조립 정적 함수의 `effects = null` 갈래(처리기는 늘 넘김).
- 참조 없는 에셋(삭제 안 함): `UnitVisual_MainCharacter`·`UnitVisual_Shielder`(외형 표 미등록), `Unit_DynamiteHammer`(24, 외형 없음).

**Unity에서 볼 것(W9 전체)**
- 컴파일(새 파일 다섯, 바뀐 서명: 사건 Presenter·처리기 셋·공격 재생·사건 소리·입력 둘·입력 Factory 둘·표시)
- EditMode 시험
  - `CombatPhaseTransitionPresenterTests`, `CombatPresentationSettingsDataTests`, `DataBaseAssetTests`·`ViewDataTableTests`(설정·외형 칸)
  - `CombatEventSoundsTests`, `GameAudioTests`, `StageMapCombatEntryBootSmokeTests`, `CombatInteractionTests`
  - `CombatSceneEventPresenterTests`, `CombatEnemyPresentationTests`, `CombatStandardAttackTests`·`CombatStandardAttackAssetTests`, `CombatMortarPresentationTests`, `CombatClipAttackSceneTests`, `CombatBallistaAttackTests`, `CombatClipShotTests`
  - `CombatClipAttackCompositionTests`, `CombatDeathCompositionTests`, `CombatRailwayContactSceneTests`, `CombatViewSystemTests`
- 재생
  - 일반 전투 진입: 밤 전환(Night Camera)·배치 복귀 아침 전환(Day Camera), 화약통·지뢰 폭발 이펙트
  - 소리: 유닛 고르기·취소·이동·사망, 공격(유닛별), 밤·낮·완료 뒤 배경음
  - 보스 스테이지는 지금 배경 맵에 전환 카메라가 없어 진입 때 실패한다(사용자 결정으로 그대로).

### Phase V3-j — 2026-10-25, 문서 완료(코드 변경 없음, 모두 미전달)

사용자 "다 끝나면 V3-j·k 까지 진행해".

| Task | 한 것 |
|---|---|
| Vj3.1 | 현강님 메모 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` §10: 배치 확인창·진입 콜백(V3-b), 덱 호버(V3-c), 기차 판별(V3-d), 입력 소리·배치 Drag 서명(V3-d·V3-h), 소리 프리셋(V3-h), 사건 Presenter 줄(V3-i), 고친 시험 표, 이미 실패하던 `AttackPreset_UsesAuthoredOrder` 사례 둘 |
| Vj3.2 | 호진님 메모 `handoffs/2026-10-22-combat-view-structure-hojin-share.md` §12: 연결 객체·리플렉션 시험, 사건 Presenter 토네이도 줄, 부트 스모크의 프리셋 줄, 보스 배경 카메라 |
| Vj3.3 | 천명님 Core 전달 문서 §35(요청 없음): 입력 연결·연결 객체로 옮긴 줄, `CombatEventAudio` 줄, 사건 Presenter 줄, 시험 `StageMap…:137-138`·`GameAudioTests:52·56`, Ballista 시험 두 줄(그대로) |
| Vj3.4 | 민형님 Factory 공유 §25: 화면 전환 설정 `From`, 표시 Factory 줄(전환 설정·카메라 검사·폭발 이펙트·가시밭·소리 프리셋·Presenter 묶음), 입력 생성 서명(재료 묶음·소리 창구), 시험 두 줄, 제안 `FieldOutlineRenderer.cs:428`(V3-a 남김), 보스 배경 카메라 |
| Vj3.5 | 폴더 배치 안내(새 파일 열여섯의 자리·루트 둘, §2 규칙 넷: 수명별 객체·공격 종류 전략·강조 층 표·소리 프리셋), 설계 §7 구현 메모·§11 작성 완료, CLAUDE.md |

- 안내와 대조하다 값 묶음 `CombatEventPresentationValues`의 자리를 `Playback/Support/`로 옮겼다(V3-i 계획과 다른 점 3).
- 다른 사람 줄은 각 메모가 원본이다(Phase 기록의 줄 목록을 blame으로 다시 셈). 설계 §11 표와 다른 점은 §11 "작성 완료"에 적었다.

### Phase V3-k — 2026-10-25, 3라운드 측정(코드 변경 없음)

- 블라인드 A/B, 하위 에이전트 셋, 지시문은 2라운드 `vi_prompts.json`에서 판 이름·파일 수·범위 한 줄만 바꿈(`vk_prompts.json`). 스냅숏 `view-ab3/`(S = `35562e96`, T = 작업 트리, 무작위 배정).
- **합친 점수: S 7.75 → T 8.8**(예상 9.33). 기준별 T: SSOT 9.0 · SOLID 8.25 · FW 9.75 · 있는 기능 9.25 · 독립성 8.25 · 불필요 8.25. 결과·해석은 설계 §13.
- 해결: 강조 층 다섯 곳, V-M6, Ballista 두 경로, V-M10, V-M9·`Boom`, V-L8, 2라운드의 우리 낮음 셋.
- 남음·새로 찾음: 조정자(중간), 입력 조립 null 허용(중간), 칸 높이 함수 선택(중간, 두 판 공통), 낮음 여럿.
- S의 높음(가시밭 레이어)은 사용자가 직접 고친 변경이 T에 있어 풀려 있다. 그 변경(`FieldThornPresenter.cs` 레이어 20 + `Prefabs/Combat/*.prefab` 레이어)은 함께 커밋해야 한다(V3-f 기록의 "뺄 것"을 고쳐 읽음).
- **3라운드 끝.**
