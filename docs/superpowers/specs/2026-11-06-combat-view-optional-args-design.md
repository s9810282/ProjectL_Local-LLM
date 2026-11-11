# 전투 View 선택 인자 나머지 설계 (OA1~OA5: 입력 계층 필수 협력자·사실 묶음)

- 날짜: 2026-11-06
- 상태: **결정 완료(11-06 사용자 "권장안대로 진행" = Q-OA1 A·Q-OA2 A·Q-OA3 B, P1~P13 그대로). Task 계획 [optional-args-plan](../plans/2026-11-06-combat-view-optional-args-plan.md)(OA-0~OA-b). OA-0 완료, OA-a 코드 완료(11-06, 컴파일 미확인, 계획 §7.2), OA-b 문서·공유·묶음 끝 검토 완료(11-07 사용자 "다음 진행", 계획 §7.3, 구현 메모 §13). Unity 확인 Ua-1~Ua-8 대기.** 독립 검토(11-06, 새 에이전트 하나) 높음 0·중간 4·낮음 6을 모두 반영했다(§12).
- 요청: 사용자 "1,2,3 대로 진행하고 결정 대기건은 뭐지"(11-06). [평가 후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §6.6 구조 단계의 세 묶음(① 선택 인자 나머지 → ② 씬 표시 나누기 → ③ 기믹 공통 계약·조립 옮기기)을 이 순서로 한다. 이 문서는 ①이다. 직전 묶음은 [작은 묶음 셋](2026-11-05-combat-view-small-bundles-design.md)이다.
- 대상 문제([검증된 문제 목록](2026-11-04-combat-view-evaluation-issues.md)): **C5-02 [중간, 불필요/우회] 2단계.** 입력 계층이 운영에서 늘 넘겨받는 협력자를 선택 인자로 받고, 쓰는 곳에 `?.`·`== null ||` 갈래를 둔다. 의존 하나를 빠뜨린 새 조립 경로가 컴파일·실행을 통과하고 기능만 조용히 사라진다.
  - 1단계(평가 후속 3차-a, `fd8b0d2f`)는 생성자 셋을 필수로 바꿨다(`AttackTargetingInteraction`·`CombatInputRouter`·`PlacementDragInteraction`). 근거는 [중간 29개 코드 확인 기록](../plans/2026-11-04-combat-view-evaluation-fixes-assessment.md) C5-02 절이다.
  - 2단계 범위는 그 기록이 적어 둔 나머지 전부다: `CombatPathInput`·`CombatMovementInteraction`, 두 확인창, `CombatInteraction`, `CombatInputFacts`, `CombatActionButtons`·`CombatInputSurfaces.Sync`, `CombatInteraction.Poll` 단일 인자판, 같은 파일 묶음의 Anchor 재확인, `cueSources` 이중 검사.
- 범위: `ViewSystem/Combat/Input/`와 `Playback/CombatSceneEventPresenter.cs` 한 줄, 시험 호출 한 곳. 넣는 기준은 하나다: **C5-02가 꼽은 형식(과 그 형식이 만드는 협력자)에서, 운영이 늘 넘기는 협력자나 값의 기본값·`?.`·`== null` 갈래**. 기본값이 "보통 경우"라는 뜻을 갖는 인자(협력자 누락이 아닌 것)와 C5-02가 꼽지 않은 형식은 Q-OA3로 뺀다. 골격·Core·Framework·GlobalSystem·Factory는 고치지 않는다(Factory는 이미 모든 값을 넘긴다, §1.6).
- 기준 판: HEAD `b3ba1768`. 줄 번호는 이 판 기준이다. OA-0(11-06)에서 원격 `7279a9d5`로 빨리 감았다. 바꿀 파일 가운데 줄이 옮겨진 것은 Presenter 하나(119 → 117)다.
- 규칙: View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. 바꿀 형식을 `Assets/Tests`에서 쓰는 곳은 `StructureHitPresentationTests.cs:168`의 `new CombatInputFacts(...)` 하나다. 우리 시험이다(`Droute` "전투 View 이거저거"). 컴파일이 깨지는 곳이므로 호출만 맞추고 단언은 그대로 둔다(CLAUDE.md 시험 규칙).

## 1. 지금 구조 (확인 사실)

### 1.1 경로 입력 `CombatPathInput`·이동 상호작용 `CombatMovementInteraction`

- `CombatPathInput` 생성자(86-90)는 `ICombatActionRejectionSink rejections = null`, `CombatActionAvailabilityTracker actionAvailability = null`를 받아 그대로 저장한다(92-93).
- 쓰는 곳은 넷이다.
  - 153 `_actionAvailability?.Read(_readFacts())`: 공격 Drag 넘겨받기 후보 판정.
  - 261 `_actionAvailability?.Read(...)`, 264 `_rejections?.MovementRejected(...)`: 경로 지정 시작의 사전 거절.
  - 649 `_actionAvailability?.Sync(...)`: 이동 모드 범위 요청 전에 선택 가용성을 먼저 맞춘다.
- 153·261 바로 뒤의 `availability != null` 검사(154·262)는 다른 뜻이다. `Read`는 Core 응답이 오기 전에 null(Unknown)을 돌려준다. 이것은 계약이라 남는다.
- `CombatMovementInteraction` 생성자(76-77)는 `rejections = null`을 받고, 249·282에서 `_rejections?.MovementRejected(...)`로 쓴다. 이 객체는 경로 입력이 생성자 안에서 같은 `rejections`로 만든다.
- 평가 C5-02가 꼽은 "Anchor·Rotation 재확인"이 경로 입력에 두 곳 있다. 둘 다 `FindActor`(230-237)가 돌려준 유닛을 읽는다. `FindActor`는 `Area == Field`인 살아 있는 아군만 돌려준다.
  - 82 `actor != null && actor.Anchor.HasValue ? RangeFactsOf(actor) : default`: 입력 사실을 읽을 때마다 돈다(`CombatInputFactsReader.cs:73` → `CombatSceneInput.cs:95`, 국면 무관).
  - 398 `actor.Rotation ?? Rotation4.R0`: 이동 지정 시작의 표시 호출이다(`ActiveActor()` → `FindActor`).
  - Core가 둘 다 보장한다. `CharacterCombatFacts` 생성자의 위치 검사(`CharacterCombatFacts.cs:255-258`)가 필드 유닛이면 Anchor·Rotation이 모두 있어야 한다고 막는다. Buffer의 사실 갱신(`CharacterBuffer.BuildFacts` → `WithHealth` → `Rebuild`)도 모두 이 생성자를 거친다. 그래서 두 갈래는 닿지 않는다.
  - `PlacementDragInteraction.cs:283`의 `?? R0`는 손패 유닛도 받으므로 남는다(이 묶음 밖).
  - 표시·재생 쪽 `Anchor.HasValue` 검사는 C1-03(필드 위 유닛 판정식 사본, 낮음)의 일이라 이 묶음 밖이다.

### 1.2 입력 기능 묶음 `CombatInteraction`

- 생성자(75-76): `placement`·`attack`만 필수다. `path`·`attackDrag`·`item`·`rangeHover`는 `= null`이다.
  - `_handover = attackDrag == null ? null : new CombatAttackDragHandover(attackDrag, path, attack)`(81). `_item`·`_handover`는 이미 `private readonly`다(57, 59).
  - 넘겨받기 `CombatAttackDragHandover`는 `target`(= `path`)을 검사 없이 저장하고(33), `IsTargetAttached => _target != null && !_target.IsDetached`(42)로 읽는다. `_target`은 readonly라 정지 뒤에도 null이 되지 않는다. 정지 뒤 상태는 `IsDetached`가 맡는다. 그래서 `path`가 필수가 되면 `_target != null`은 닿지 않는다.
  - 소유자 목록은 넘어온 것만 담고(86-96), 배경 소유자가 있으면 `_hasBackground = true`(103)다. 배경 소유자는 호버 범위 하나뿐이다.
- null 갈래:
  - `_item`: 113 `IsItemDragActive`, 179 `CanSelectItem`, 190 `CancelItemSelection`의 `_item?.`, 194 `RequestItemUnequip`.
  - `_handover`: 210 `TryHandleAttackDrag`, 285·300·319 `_handover?.Invalidate()`.
  - `_hasBackground`: 217 `PointerMoved`, 235 `SelectPressed`, 386 `Poll`.
  - `_path`: 137 `HasCurrentAcceptedEmptyMovementRange`, 270 `Cancel`, 301 `LeaveMoveMode`.
- `_path`만 사정이 다르다. `Stop`(486-494)이 소유자 목록을 돌기 전에 `_path = null`로 놓는다. 정지 콜백 안에서 이름 입구가 다시 들어와도 놓인 경로에 닿지 않게 하려는 것이다(F2). 그래서 경로의 null 갈래 셋은 생성자를 필수로 바꿔도 남는다(평가 기록과 같음).
- `Poll(CombatPreviewResult latestPreview)`(402)는 internal이다. 부르는 곳은 같은 클래스의 세 인자 `Poll`(385) 하나다. 문서 주석은 "조정자는 Tick을 부릅니다"라고 적고 있다. 세 인자 `Poll`(383)도 internal이지만 부르는 곳은 같은 클래스의 `Tick`(373) 하나다.
- `Cancel(characters = null)`(267)·`CancelDragOrAttack(characters = null)`(283): 부르는 곳은 `CombatInputRouter.cs:141`·`:171` 둘이고, 둘 다 `characters`를 넘긴다. 그 목록은 Router의 `Route`가 받은 것이다. `Route`를 부르는 곳(`CombatSceneInputDriver.cs:63`·`:94`, `CombatEndTurnEntry.cs:81`·`:87`, `CombatDeploymentEntry.cs:85`·`:91`)은 모두 `_characters.Characters`를 넘긴다. 이 목록은 `CharacterBuffer.cs:127`이 만들고 null이 아니다.
- `AttackTargetingInteraction.Cancel(characters = null)`(176)도 같다. 부르는 곳은 `CombatInteraction.cs:291` 하나다. 안쪽 `characters == null ||`(178)도 같은 성격이다.
- `PlacementDragInteraction.FindDeckUnit`(103-105)의 `characters == null ||`도 같다. 부르는 곳(`CombatDeckPlacementInput.cs:37`·`:51`)이 모두 `_characters.Characters`를 넘긴다. `PlacementDragInteraction`은 C5-02가 꼽은 형식이다(1단계에서 `sounds`만 필수로 했다).

### 1.3 확인창 둘

- `CombatDeploymentConfirmation`(배치 미배치 확인)과 `CombatEndTurnConfirmation`(턴 종료 확인)은 `IUIConfirmationPresentation presentation`을 검사 없이 저장한다(32, 36).
  - `IsOpen`이 `_presentation?.IsConfirmationOpen(this) ?? false`다(40, 44).
  - 여는 곳은 null이면 `InvalidOperationException`을 던진다(97-98, 74-75).
  - `Close`는 `_presentation?.HideConfirmation(this)`다(118, 95).
- 부르는 곳은 각 진입 객체 하나다(`CombatDeploymentEntry.cs:56`, `CombatEndTurnEntry.cs:48`). 둘 다 `CombatSceneInputSources.Confirmation`을 넘긴다. 이 값은 재료 묶음 생성자(65)가 이미 필수로 막는다.

### 1.4 행동 버튼·입력 표면

- `CombatActionButtons` 생성자(19)와 `Read`(50)는 `bool canUseReturn = false`를 받는다. 생성자를 부르는 곳은 `Read` 안 둘(58, 63)이고, 둘 다 값을 적는다. `Read`를 부르는 곳은 `CombatInputSurfaces.Sync`(66) 하나이고, 값을 넘긴다.
- `CombatInputSurfaces.Sync`(57-58)는 `bool canUseReturn = false, CombatActionAvailability availability = null`을 받는다. 부르는 곳은 `CombatSceneInputDriver.cs:176` 하나이고, 다섯 인자를 모두 넘긴다.
  - `availability` 값 자체는 null일 수 있다. 응답 전에는 `buttons.Availability`가 null이다. 지울 것은 기본값뿐이고, null 값은 그대로 받는다.
- 표면 안 `?.`:
  - `_root?.`(61-63, 73-75): `BindRoot` 전·`UnbindRoot` 뒤의 수명이다. 남는다.
  - 그 안쪽 `BattleChanger?.`·`Pack?.`·`UnitInfo?.`(39, 47-49, 61-63, 73-75): 셋 다 `CombatSceneRoot.FindMissingReference`(93-98)가 필수로 막는다. 이 검사는 `CombatSceneDisplayFactory.cs:92`에서 돌고, `BindRoot`(`CombatViewSystem.cs:515`)보다 먼저다. 38줄 주석도 "씬 루트는 Root.ValidateReferences로 필수 연결을 검사한다"라고 적는다. 안쪽 갈래는 같은 사실을 다시 확인한다. Unity 객체에 쓴 `?.`는 참조 null만 보므로, 파괴된 객체에서도 동작은 같다.
  - `BindRoot`의 `if (root == null) return;`(35-36)도 닿지 않는다. 부르는 곳 `CombatViewConnection.BindRoot`(109-110)가 먼저 `ArgumentNullException`을 던진다.

### 1.5 입력 사실 `CombatInputFacts`

- 생성자 인자 28개 중 앞 다섯만 필수이고 23개가 선택이다(21-46).
- 운영 호출은 `CombatInputFactsReader.Read`(55-87) 하나이고, 28개를 모두 넘긴다.
- 시험 호출은 `StructureHitPresentationTests.cs:168` 하나다. 필수 다섯과 이름 인자 넷(`currentLayer`·`hasPendingCommand`·`isInputAvailable`·`isModalOpen`)을 넘긴다.
- 새 사실을 더할 때 읽기 쪽에 넘기기를 빠뜨려도 컴파일이 통과하고 그 사실은 늘 기본값(false·0·None)으로 읽힌다. 평가 C5-02의 "인자 26개 중 21개 선택"이 이것이다(그 뒤 둘이 더해졌다).

### 1.6 Factory와 Presenter

- `CombatSceneInputFactory.CreateSceneInteraction`(111-161)은 경로 입력에 `notices`·`actionAvailability`를 넘기고(150-151), `CombatInteraction`에 여섯 협력자를 모두 넘긴다(161). 넘기기 전에 `actionAvailability`·`gate`·`sounds`를 이미 검사한다(140-146). **Factory는 고칠 것이 없다.**
- `CombatSceneEventPresenter` 생성자는 `_cueSources = cueSources ?? throw ...`(112)로 막은 뒤, 119에서 `if (cueSources == null) throw ...`로 같은 검사를 한 번 더 한다(11-01 `36a6d913` 이펙트 구조 EL-d 때 들어감).

### 1.7 다른 작업자 줄 (전체 이력 기준, OA-0 확인 2026-11-06)

얕은 복제의 blame은 경계 커밋(`^83fb2767`·`^4782921e`·`^8ed3f8cb`)을 작성자로 보여 틀린다. OA-0에서 원격 전체 이력(옛 평면 경로 포함, 공백·쉼표만 바꾼 커밋은 빼고)으로 다시 셌다([계획 §7.1](../plans/2026-11-06-combat-view-optional-args-plan.md)). 바꿀 줄 기준으로 적는다.

| 작성자 | 파일·줄 | 들어온 커밋 |
|---|---|---|
| 현강님 | `CombatDeploymentConfirmation.cs` 파일 전체(26·32·40·97-98·118) | `fc133bf7`(10-19) |
| 현강님 | `CombatMovementInteraction.cs` 77 `rejections = null`·249·282, `CombatPathInput.cs` 89 `rejections = null` | `80eb7481`(10-19) |
| 현강님 | `PlacementDragInteraction.cs` 103-105 `FindDeckUnit` | `4dc3ba8a`(10-17) |
| 현강님 | `CombatInteraction.cs` 267 `Cancel(characters = null)` | `1b155e02`(10-13) |
| 호진님 | `CombatInteraction.cs` 76 `attackDrag = null` | `32654528`(10-17) |
| 호진님 | `CombatPathInput.cs` 82 `actor.Anchor.HasValue` | `34ee0de4`(10-20) |
| 호진님 | `CombatInputFacts.cs` 37·38(`combatInputRestriction`·`hasCurrentAcceptedEmptyMovementRange`), `CombatActionButtons.cs` 58·19의 `canUseReturn` 인자 | `34ee0de4`(10-20) |
| 호진님 | `CombatInputSurfaces.cs` 49·57-58·63-65·75 | `8339e308`(10-31) |
| 천명님 | `CombatPathInput.cs` 86-87(생성자 첫 줄) | `e0b02cc7`(10-04, 병합에서 파일이 처음 생김) |
| 천명님 | `CombatMovementInteraction.cs` 76(생성자 서명) | `87348b86`(10-04) |
| 천명님 | `CombatInputFacts.cs` 39(`battleInstanceId`·`controllerTurnSequence`), `CombatActionButtons.cs` 19의 기본값 `= false` | `d4a6d6fd`(10-24) |
| 천명님 | `CombatEndTurnConfirmation.cs` 파일 전체 | `57af3244`(11-03, 천명님 계정 커밋. 같은 커밋이 우리 행동 버튼 설계 §8·CLAUDE.md도 고쳐, 우리 세션 변경을 천명님 계정으로 커밋했을 가능성이 있으나 확인 못 함 — 조정자 나누기 CS-a 선례대로 천명님 줄로 셈) |

- 나머지 바꿀 줄은 용근 줄이다: `CombatInteraction`의 나머지(75 `path = null`·80·82·113·179·190·193-194·270·283·301·402, 주석), 경로 입력 88·90·153·261·264·398·649, 넘겨받기, `CombatInputFacts`의 나머지 인자, `AttackTargetingInteraction`, 표면의 나머지, Presenter 117(원격 병합 뒤 줄 번호), 시험 168.
- 다른 작업자 줄은 영역 규칙(CLAUDE.md "작업 영역 = Combat 씬의 전투 View 구조, 작성자와 무관")대로 고치고, 변경 정리를 그 사람에게 넘긴다(§10).

## 2. 목표와 비목표

- 목표
  - C5-02를 다 푼다. 운영이 늘 넘기는 협력자는 생성자에서 `?? throw new ArgumentNullException`으로 막고, 쓰는 곳의 `?.`·`== null ||` 갈래를 지운다. 정말 없을 수 있는 값(경로 정지 뒤, 응답 전 가용성, 루트 연결 전)만 null 갈래를 남기고, 남기는 까닭을 줄마다 적는다.
  - 입력 사실은 새 사실을 더할 때 읽기 쪽 넘기기를 빠뜨리면 컴파일이 막게 한다.
  - 보이는 동작은 바뀌지 않는다(§4).
- 비목표
  - 같은 성격이지만 평가 C5-02가 꼽지 않은 선택 인자(§8 Q-OA3의 목록)는 이번에 고치지 않는다.
  - 필드 위 유닛 판정식 사본(C1-03)은 고치지 않는다.
  - `CombatInteraction`의 배경 소유자 순회(목록에서 `IsBackground`를 찾는 반복)는 W5 공통 수명 계약이라 그대로 둔다. 항상 참인 깃발 `_hasBackground`만 걷는다.
  - Factory·Core·Framework·GlobalSystem은 고치지 않는다.

## 3. 설계

### OA1 경로 입력·이동 상호작용: 거절 창구와 가용성 추적은 필수

- `CombatPathInput` 생성자: `rejections`·`actionAvailability`에서 기본값을 지우고 `?? throw new ArgumentNullException`으로 막는다. 인자 순서는 그대로다(Factory 호출이 위치 인자로 넘긴다).
- 153·261·649의 `_actionAvailability?.` → `_actionAvailability.`, 264의 `_rejections?.` → `_rejections.`.
- `availability != null` 검사(154·262)는 남긴다(P3).
- 82: `actor != null && actor.Anchor.HasValue` → `actor != null`, 398: `actor.Rotation ?? Rotation4.R0` → `actor.Rotation.Value`(P4).
- `CombatMovementInteraction` 생성자: `rejections`를 필수로 하고 막는다. 249·282의 `_rejections?.` → `_rejections.`.

### OA2 입력 기능 묶음: 여섯 협력자 모두 필수

- `CombatInteraction` 생성자: `path`·`attackDrag`·`item`·`rangeHover`에서 기본값을 지우고 막는다.
  - `_handover`는 늘 만든다. 소유자 목록은 고정 순서 여섯(넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 호버, TP-2)을 조건 없이 담는다.
  - `_item`·`_handover`(이미 readonly)의 갈래(113·179·190·194·210·285·300·319)를 지운다.
  - `_hasBackground` 필드와 그 검사 셋(217·235·386)을 지운다(P2).
- `_path`의 null 갈래 셋(137·270·301)은 남긴다. `Stop`이 먼저 놓기 때문이다(P1). 필드 주석에 그 까닭을 한 줄 적는다.
- `Poll(CombatPreviewResult)`(402)와 세 인자 `Poll`(383)은 private으로 내린다(P9). 문서 주석은 "`Tick`이 부르는 감시 단계"·"세 인자 `Poll`이 맨 먼저 부르는 단계"로 고친다.
- 넘겨받기 `CombatAttackDragHandover`: 생성자가 `target ?? throw new ArgumentNullException`으로 막고, `IsTargetAttached`는 `!_target.IsDetached`로 줄인다. `target` 인자 주석의 "없으면 null입니다"를 지운다(P11).
- `Cancel`·`CancelDragOrAttack`·`AttackTargetingInteraction.Cancel`에서 `characters = null` 기본값을 지운다. `AttackTargetingInteraction.Cancel` 안의 `characters == null ||`(178)과 `PlacementDragInteraction.FindDeckUnit`의 `characters == null ||`(105)도 지운다(P5).
- 낡게 되는 주석을 함께 고친다: `CombatInteraction` remarks 31-32, 인자 주석 71-74, 필드 주석 56·58·64, 192, 207, 246("없으면 …" 설명).

### OA3 확인창 둘: 표시 창구는 필수

- 두 생성자가 `presentation ?? throw new ArgumentNullException(nameof(presentation))`로 막는다.
- `IsOpen` → `_presentation.IsConfirmationOpen(this)`, `Close` → `_presentation.HideConfirmation(this)`.
- 여는 곳의 null 검사 갈래(97-98, 74-75)를 지운다. 같은 사실을 생성자가 먼저 막는다.

### OA4 행동 버튼·입력 표면: 기본값을 지우고 루트가 보장한 안쪽 `?.`를 지운다

- `CombatActionButtons` 생성자·`Read`, `CombatInputSurfaces.Sync`에서 `canUseReturn = false`·`availability = null` 기본값을 지운다. `availability` 값 자체는 null을 받는다(응답 전). 문서 주석에 "null = 응답 전"을 적는다(P6).
- 표면의 `_root?.`는 남기고(수명), 안쪽 `BattleChanger?.`·`Pack?.`·`UnitInfo?.`는 `.`로 바꾼다(P7).
- `CombatInputSurfaces.BindRoot`의 `if (root == null) return;`은 `ArgumentNullException`으로 바꾼다. 문서 주석 "루트가 없으면 아무것도 하지 않습니다"와 13·32줄 주석을 고친다(P12).

### OA5 입력 사실: 모든 인자 필수, 이중 검사 하나 걷기

- `CombatInputFacts` 생성자 인자 28개에서 기본값을 모두 지운다. 서명 순서·이름은 그대로다.
  - 운영 호출(`CombatInputFactsReader.Read`)은 이미 28개를 모두 넘겨 바뀌지 않는다.
  - `default(CombatInputFacts)`를 쓰는 곳(`CombatInputSurfaces`의 `Refresh(default, true)` 등)은 영향이 없다.
- 시험 `StructureHitPresentationTests.cs:168`은 인자 28개를 이름 인자로 모두 적는다. 지금 넘기는 넷은 그 값 그대로, 나머지 스물넷은 옛 기본값을 적는다. 열거·구조체·참조 값은 `default`·`null`로 적어 using을 더하지 않는다(`TutorialCombatInputRestriction`·`PointerTarget`의 기본은 모두 `None = 0`이라 `default`와 같다). 단언과 `[TestCase]` 다섯은 바꾸지 않는다(P8).
- `CombatSceneEventPresenter` 119의 두 번째 `cueSources` null 검사를 지운다(112가 이미 막음).

## 4. 지킬 것 (동작 같음)

- 운영 조립(`CombatSceneInputFactory`)은 이미 모든 협력자를 null 아닌 값으로 넘긴다(§1.6). 그래서 새 `throw`는 운영에서 나지 않는다.
- 지우는 갈래는 운영에서 늘 같은 쪽으로 가던 갈래다. 소유자 목록 여섯과 그 순서는 지금 운영과 같다.
- `Read`가 응답 전에 돌려주는 null, 루트 연결 전, 경로 정지 뒤의 null 처리는 그대로다.
- 시험 단언은 바꾸지 않는다. 시험 호출의 값도 옛 기본값 그대로다.

## 5. 바뀌는 것 (보이는 동작 없음)

- 새 조립 경로가 위 협력자 중 하나를 빠뜨리면, 조용히 기능이 빠지는 대신 생성 순간 `ArgumentNullException`(F1)이 난다.
- 입력 사실에 새 인자를 더하면 읽기 쪽과 시험 호출을 함께 고쳐야 컴파일된다.
- 바뀌는 파일은 코드 열둘, 시험 하나다. 줄 수는 약 120~130줄로 어림한다(입력 사실 서명 약 21줄, `CombatInteraction` 약 35줄, 시험 호출 약 30줄).
  - 코드: `CombatPathInput`, `CombatMovementInteraction`, `CombatInteraction`, `CombatAttackDragHandover`, `AttackTargetingInteraction`, `PlacementDragInteraction`, `CombatDeploymentConfirmation`, `CombatEndTurnConfirmation`, `CombatActionButtons`, `CombatInputSurfaces`, `CombatInputFacts`, `CombatSceneEventPresenter`(`AttackTargetingInteraction`·`PlacementDragInteraction`은 한두 줄).
  - 시험: `StructureHitPresentationTests`.

## 6. Phase(안)

| Phase | 내용 | Unity 확인 |
|---|---|---|
| OA-0 | 착수 전 확인: 원격·HEAD, 경계 커밋 줄(§1.7 — `^83fb2767`·`^4782921e`·`^8ed3f8cb`)의 실제 작성자를 GitHub 경로 이력으로 확인, `Assets/Tests` grep 다시(바꿀 형식을 쓰는 시험이 168 하나인지) | 없음 |
| OA-a | 코드 OA1~OA5와 시험 호출 한 곳 | Ua-1~Ua-8(아래) |
| OA-b | 문서·공유 메모·묶음 끝 검토 한 번·평가 아티팩트 9차 | 없음 |

- Ua(OA-a 뒤):
  1. 컴파일, Boot에서 전투 진입(배치·전투 국면 모두) 때 F1(`ArgumentNullException`)이 없는지.
  2. 이동 AP가 없는 유닛을 끌어 이동하려 할 때 거절 안내가 나오는지(경로 입력 264·이동 상호작용 249·282).
  3. 이동한 유닛으로 공격 모드에 들어가 자기 칸을 끌 때 공격 모드가 유지되는지(153). 이동 전 유닛은 지금처럼 이동으로 넘어가고, 이동 지정 시작 때 고스트 방향이 유닛 방향과 같은지(398).
  4. 배치 국면에 미배치 유닛이 있을 때 전투 시작 확인창이 열리고 Y·N·닫기가 전과 같은지.
  5. 행동하지 않은 아군이 있을 때 턴 종료 확인창이 같은지.
  6. 가방 아이템 끌기·우클릭 해제·끌기 취소(우클릭)가 같은지.
  7. 유닛 호버 범위, 공격 모드에서 자기 칸 끌기의 이동 넘겨받기가 같은지.
  8. 행동 버튼(공격·턴 종료·복귀), UnitInfo 행동 가능 표시, BattleChanger·가방 표시의 갱신·숨김이 같은지. EditMode 시험 `StructureHitPresentationTests.InspectionPolicy_DeploymentKeepsPendingAndModalBoundaries` 다섯 사례가 통과하는지.
- 코드가 작아 Phase 하나로 둔다(Q-OA2).

## 7. 7기준 점검 (설계안)

| 기준 | 점검 |
|---|---|
| SSOT | "이 협력자는 늘 있다"는 사실이 생성자 한 곳에 모인다. 지금은 생성자 기본값·쓰는 곳 `?.` 여러 곳·Factory 검사에 흩어져 있다. 루트 필수 연결은 `ValidateReferences` 한 곳만 남는다. |
| SOLID | 책임은 바뀌지 않는다. `CombatInteraction`의 소유자 목록이 고정되어 조건부 조립이 사라진다. |
| FW 계약 | 서비스 등록·수명은 바뀌지 않는다. Factory 무변경. |
| 기능 변경 독립성 | 새 사실·새 협력자를 더할 때 빠뜨리면 컴파일·생성 시점에 드러난다(C5-02의 실패 시나리오가 막힌다). |
| 불필요/우회 | 늘 같은 쪽으로 가던 갈래 약 30곳, 항상 참인 깃발 하나, 밖에서 부르지 않는 입구 둘, 이중 검사 하나, Core가 보장한 위치 재확인 둘이 사라진다. |
| C#/Unity 권장 | `?? throw`, `readonly` 필드, 이름 인자. 28개 인자 생성자는 길지만 호출이 운영 하나·시험 하나라 받아들인다(Q-OA1). |
| 매 프레임 | 매 Tick 도는 `?.` 몇 개와 `_hasBackground` 검사가 줄 뿐 차이는 무시할 만하다. |

- 점수 어림(같은 감점표, 다 풀림으로 셀 때 — 검토 M1·M2를 넣어야 다 풀림으로 셀 수 있다): C5 75 → 82.5. 요청 7기준 79.3 → **80.4**(+1.07), 12기준 84.4 → **85.0**(+0.625). 중간 해결 18 → 19.

## 8. 정할 것

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-OA1 | `CombatInputFacts` 인자 28개를 모두 필수로 할지 | **A** 모두 필수. 운영 호출 하나는 이미 모두 넘기고, 시험 호출 하나는 이름 인자 28개로 적는다 | B 그대로 둔다(C5-02는 "일부"로 남음, 점수 그대로). C 사실을 몇 묶음 값으로 나눈다(구조 변경, 이 묶음 밖) |
| Q-OA2 | Phase 나누기 | **A** 코드 Phase 하나(OA-a) + 문서 Phase(OA-b), Unity 확인 한 번 | B 협력자 필수화(OA1~OA4)와 사실 묶음(OA5)을 Phase 둘로 |
| Q-OA3 | 평가가 꼽지 않은 같은 성격 선택 인자도 이번에 걷을지: `CombatInputFactsReader.Read(includeUnhandledCommand = false, followUp = false)`, `CombatEnvironmentHover.Refresh(..., canSelect = true)`, `CombatMovementInteraction.Begin(..., deferPreview = false)`, `CombatInputRouter.RejectTutorialCommand(..., includeUnhandledCommand = false)`, 표시 계약 셋(`ICombatMovementFeedback.UpdateMovementPointer(frozen = false)`, `ICombatUnitRangeFeedback.ShowHoverRange(source = default)`, `ICombatInteractionFeedback.ShowAffectedCells(pull = null, closeOutline = false)`·`ShowRangeCells(..., basicCells = null)`) | **B** 넣지 않는다. 기본값이 "보통 경우"라는 뜻을 가진 인자이고(협력자 누락과 다름), 표시 계약은 구현 쪽 파일까지 번진다. 그 파일을 고칠 때 함께 본다 | A 이번에 함께 걷는다(파일 약 8개 더, 표시 계약 구현 `CombatSceneDisplay` 등) |

## 9. 설계에서 정한 것 (확인 대상)

- P1 `CombatInteraction`의 `path`는 생성자에서 필수로 막지만, 필드 null 갈래 셋(137·270·301)은 남긴다. `Stop`이 목록을 돌기 전에 `_path`를 놓기 때문이다(F2). 필드는 `readonly`가 되지 않는다.
- P2 `_hasBackground`는 늘 참이 되므로 필드와 검사 셋을 지운다. 배경 소유자 순회 반복은 남긴다(W5 계약).
- P3 `CombatActionAvailabilityTracker.Read`의 null 반환(응답 전 Unknown) 검사는 남긴다. 지우는 것은 추적기 자체의 `?.`뿐이다.
- P4 경로 입력 82의 `actor.Anchor.HasValue`를 지우고 398의 `?? Rotation4.R0`를 `.Value`로 바꾼다. 근거는 Core다: `CharacterCombatFacts` 생성자의 위치 검사(255-258)가 필드 유닛의 Anchor·Rotation을 보장하고, Buffer의 사실 갱신도 모두 그 생성자를 거친다. `FindActor`는 `Area == Field`인 살아 있는 아군만 돌려준다.
- P5 `characters` 기본값 셋을 지우고 `AttackTargetingInteraction.Cancel`·`PlacementDragInteraction.FindDeckUnit`의 안쪽 null 검사도 지운다. 부르는 곳이 모두 Buffer 목록(`CharacterBuffer.cs:127`, null 아님)을 넘긴다.
- P6 `CombatInputSurfaces.Sync`·`CombatActionButtons`는 기본값만 지운다. `availability`의 null 값은 "응답 전"이라는 뜻이라 그대로 받는다.
- P7 표면의 `_root?.`는 수명이라 남기고, 안쪽 셋은 `ValidateReferences`가 막으므로 지운다. 38줄 주석이 이미 그 근거를 적는다.
- P8 시험 호출은 28개를 이름 인자로 적는다. 위치 인자 다섯(필수였던 것)도 이름 인자로 바꿔 한 모양으로 둔다. 값은 옛 기본값 그대로이고, 열거·구조체·참조는 `default`·`null`로 적어 using을 더하지 않는다(검토 M3).
- P9 `CombatInteraction`의 두 `Poll`은 지우지 않고 private으로 내린다. 둘 다 같은 클래스 안에서만 부른다(평가 기록은 한 인자판만 꼽았고, 세 인자판은 검토가 찾음).
- P10 Factory 주석(`CombatSceneInputFactory.cs:156` "모두 필수다(평가 C5-02)")은 고치지 않는다. 이번 변경과 맞는 내용이다.
- P11 넘겨받기의 `target`을 필수로 하고 `IsTargetAttached`의 null 검사를 지운다. 정지 뒤 상태는 `IsDetached`가 맡는다(`_target`은 readonly).
- P12 `CombatInputSurfaces.BindRoot`의 null 반환을 예외로 바꾼다. 부르는 곳이 하나이고 그곳이 먼저 예외를 던지므로 동작은 같다.
- P13 넣는 기준(머리 "범위" 줄)을 하나로 둔다. 이 기준으로 `PlacementDragInteraction` 105를 넣고, `ShowRangeCells` 등 표시 계약 기본값은 Q-OA3로 뺀다.

## 10. 공유 (구현 뒤, 미전달 예정)

§1.7 표 기준이다.

- 현강님 메모(새 절): 배치 확인창 파일(`presentation` 필수, `IsOpen`·`Close`·여는 곳 갈래), 거절 창구 `rejections` 기본값 둘(경로 입력·이동 상호작용)과 쓰는 곳 둘, `FindDeckUnit`의 null 검사, `CombatInteraction.Cancel` 기본값.
- 호진님 메모(새 절): `CombatInputSurfaces` 줄, `attackDrag` 기본값, 경로 입력 82, 입력 사실 37·38, 행동 버튼 `canUseReturn` 기본값.
- 천명님 특성 공유(새 절): 경로 입력·이동 상호작용 생성자 서명 줄, 입력 사실 39, 행동 버튼 기본값, 턴 종료 확인창.
- 민형님: 없음(Factory 무변경).

## 11. 위험

- 낮음. 주로 컴파일 시점에 드러나는 변경이다. 운영 조립이 넘기는 값이 모두 null이 아님을 확인했다(§1.6).
- 하나라도 빠뜨리면 동작은 같고 갈래가 남을 뿐이다.
- 시험 호출에서 값을 옮겨 적다 틀리면 사례 결과가 바뀔 수 있다. 옛 기본값을 표로 대조한다(OA-a 작업 목록).
- `CombatInteraction` 소유자 목록이 늘 여섯이 되므로, 시험이 일부만 넘겨 만들던 경로가 있으면 깨진다. `Assets/Tests`에서 `new CombatInteraction(`을 부르는 곳은 없다(11-06 grep).

## 12. 독립 검토 기록 (2026-11-06)

새 에이전트 하나가 HEAD `b3ba1768` 코드와 대조했다(읽기 전용). 동작이 바뀌는 문제나 운영에서 null에 닿는 갈래는 없었다. 높음 0·중간 4·낮음 6이고 모두 반영했다. 중간 넷은 Claude가 코드로 다시 확인했다.

| 번호 | 지적 | 반영 |
|---|---|---|
| M1 | `CombatPathInput.cs:398` `actor.Rotation ?? Rotation4.R0`가 C5-02의 "Rotation 재확인"이다. §1.1의 "찾지 못했다"는 틀림 | §1.1·OA1·P4에 넣음. Core 위치 검사(`CharacterCombatFacts.cs:255-258`)가 필드 유닛의 Rotation을 보장 |
| M2 | path를 필수로 하면 `CombatAttackDragHandover`의 `_target != null`(42)이 닿지 않는데 목록에 없음 | §1.2·OA2·P11에 넣음, 바꿀 파일에 더함 |
| M3 | 시험 파일에 `BufferSystems.Tutorial` using이 없어, 열거 값을 이름으로 적으면 컴파일이 깨짐 | OA5·P8: 열거·구조체·참조는 `default`·`null`로 적음(기본이 모두 `None = 0`임을 확인) |
| M4 | 작성자 표 누락: `CombatInputFacts` 26-29·31-37, `CombatPathInput` 82(`^83fb2767`), `CombatMovementInteraction` 77·85·249·282(`^8ed3f8cb`), `CombatInteraction` 402 | §1.7 표를 고침, OA-0 확인 대상에 `^8ed3f8cb`를 더함 |
| L1 | `_item`·`_handover`는 이미 readonly | OA2 문구 고침 |
| L2 | P4 근거("310에서 이미 예외")가 약함. 82는 국면 무관, 310은 PlayerInput 뒤에서만 돎 | P4 근거를 Core 생성자 보장으로 바꿈 |
| L3 | 넣고 빼는 기준이 일관되지 않음(`PlacementDragInteraction` 105 누락, `ShowRangeCells` 누락, 세 인자 `Poll`) | 머리에 넣는 기준 한 줄·P13, 105를 넣고 `ShowRangeCells`는 Q-OA3로, 세 인자 `Poll`도 private(P9) |
| L4 | 낡게 될 주석 목록 누락, `BindRoot` null 반환도 닿지 않음 | OA2·OA4에 주석 목록, P12 |
| L5 | 파일 수 "아홉"과 목록 열이 맞지 않음, 줄 수를 작게 잡음 | §5를 코드 열둘·약 120~130줄로 |
| L6 | 줄 번호(`FindMissingReference` 93-98, `Stop` 486-494, Factory 주석 156), `Route` 호출처 넷 더 있음 | 고침(결론은 같음) |

- 검토가 맞다고 확인한 것: 대상 형식을 만드는 곳(Factory 150·159·161, 경로 입력 103, 진입 객체 둘, `CombatActionButtons` 58·63, 사실 읽기 55, 시험 168)뿐이고, 특성 미리보기 Factory는 입력 계층을 만들지 않는다. 입력 사실 28인자를 운영이 모두 넘김(C# 9, `with` 없음). 소유자 순서·`_byTarget` 중복 검사·`Stop` 실패 모음은 그대로. 남기는 갈래(응답 전 null, `Stop`의 `_path` 놓기, `_root?.` 수명)는 맞음. 점수 계산 맞음(M1·M2를 넣어야 다 풀림).

## 13. 구현 메모 (OA-a·OA-b, 2026-11-06~07)

- 기준 판 `7279a9d5`. 코드 열둘 + 우리 시험 호출 하나, +150 −132줄(묶음 끝 검토 주석 셋 포함). 새 파일·`.meta`·에셋 없음, Factory 무변경. 컴파일 미확인(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음. 실행 기록은 [계획 §7.2·§7.3](../plans/2026-11-06-combat-view-optional-args-plan.md).
- 설계와 다른 점은 없다. OA1~OA5와 P1~P13을 쓴 그대로 했다.
- 예외 종류가 바뀐 곳 둘(운영에서는 닿지 않음): 확인창 둘의 표시 창구 누락은 여는 순간 `InvalidOperationException` → 생성 때 `ArgumentNullException`(계획 TP-5), 입력 표면 `BindRoot(null)`은 조용히 돌아가던 것 → `ArgumentNullException`(P12).
- 남긴 갈래(정말 없을 수 있는 값): `CombatInteraction._path` 셋(정지 뒤, P1), 가용성 응답 전 `availability != null` 둘(P3), 표면 `_root?.`(루트 묶기 전·푼 뒤, P7), 포인터 대상 None의 `OwnerOf`.
- 묶음 끝 검토(11-07, 새 에이전트 하나, 읽기 전용): 높음 0·중간 0·낮음 4.
  - 컴파일: 바꾼 서명의 호출처가 모두 값을 넘기고, 시험의 이름 인자 28개가 생성자 매개변수와 이름·개수가 같고, private이 된 `Poll` 둘을 밖에서 부르지 않고, `_hasBackground` 참조가 남지 않았다. `CharacterData.Rotation`은 `Rotation4?`라 `.Value`가 맞다.
  - 동작: Factory·입력 재료 묶음·연결 객체가 필수 인자를 모두 null 아닌 값으로 넘긴다. 소유자 순서와 `_byTarget` 중복 검사는 그대로다. `IsBackground`를 켜는 소유자는 호버 하나라 `_hasBackground`는 운영에서 늘 참이었다. 시험 값은 옛 기본값과 하나씩 같다.
  - 낮음 L1·L2·L4(이번 변경으로 낡은 주석 — 넘겨받기 15·99, 입력 사실 `PointerTarget` 문서, 시험 주석)는 계획 TP-6대로 고쳤다. L3(`PlacementDragInteraction.TryReturnUnderPointer`의 `characters == null` 검사 등 같은 성격의 검사 넷 — 이번에 바꾼 파일 안 하나, 안 바꾼 파일 셋)은 P5가 지울 대상을 둘로 한정해 남겼다. 사용자 결정 대기(계획 §7.3).
- 공유(미전달): 현강님 메모 §26, 호진님 메모 §19, 천명님 특성 공유 §9(View가 기대는 Core 보장 — 필드 유닛 Anchor·Rotation — 을 참고로 적음, 요청 없음). 민형님 없음.
- 점수 어림(같은 감점표, 다 풀림으로 셈): C5 75 → 82.5, 요청 7기준 79.3 → 80.4, 12기준 84.4 → 85.0, 중간 해결 18 → 19(평가 아티팩트 9차).
