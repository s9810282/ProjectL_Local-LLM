# 전투 View 새 SSOT 셋 설계 (2026-11-11)

> 사용자 요청: "푸쉬하고, 새 SSOT 셋 설계부터 진행해".
> 대상: [재평가](2026-11-11-combat-view-reevaluation.md) §3.2의 새 중간 셋 C1-N01·C1-N02·C1-N03. 원문은 [재평가 문제 목록](2026-11-11-combat-view-reevaluation-issues.md) §2에 있다.
> 기준 판 `ec0148266`. **코드 변경 없음.** 정할 것은 §3이다.
> **결정 완료(2026-11-11 사용자 "권장안대로 하고 Task 계획 써줘, Core 변경 우리가 해도 괜찮아"):** Q-SN1~Q-SN6 모두 A. Q-SN3 A의 Core 파일·Buffer의 Core 쓰기 거래 변경은 사용자 허락으로 이 작업이 한다. Task 계획은 [new-ssot-plan](../plans/2026-11-11-combat-view-new-ssot-plan.md).
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `Core/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/`.

## 0. 요약

| 문제 | 지금 | 바꿀 것(권장) | 고칠 파일 | 다른 작업자 줄 |
|---|---|---|---|---|
| C1-N01 유닛 모델 Renderer 판정 세 벌(검토에서 넷째 찾음) | 외형 층·몸 경계는 TMP·Canvas·커서 표지를 빼고, Cartoon 대체 재질은 아무것도 안 빼고, 상태 모습과 배치 고스트는 TMP·Canvas만 뺀다 | 판정을 정적 규칙 하나(`UnitModelParts.Is`)로 올려 넷이 같이 부른다 | 새 파일 1 + 4 | 호진님(판정 원본), 민형님(Cartoon 대체), 고스트 줄은 작성자 확인 필요(경계 커밋) |
| C1-N02 HUD 행동 가능 다시 묻기가 Core 식 사본 | 자기 사실이 바뀐 유닛 + "여러 칸 유닛 유무"(Core 식 사본)가 바뀔 때만 다시 묻는다 | Core가 한 번에 모든 아군의 답을 주는 묶음 조회. View는 판이 바뀌면 한 번 묻는다 | Core 4 + Buffer 쓰기 거래 1 + Buffer 1 + View 2 | 현강님(HUD 조회), 천명님(Core 조회 서비스·미리보기 게시 검증) |
| C1-N03 조준·포효 넉백 우선 결정 두 곳 | 넉백 미리보기가 "조준이 이김"을, 포효 예고 추적기가 "전경 입력 중엔 숨김"을 따로 정한다 | 넉백 미리보기 한 곳이 정한다. 추적기는 Core 답과 "전경 입력 중" 사실만 넘긴다 | 3 | 없음(우리 줄) |

- 셋을 다 고치면 C1이 37.5 → 60이 되어, 요청 7기준 79.3 → 82.5, 12기준 83.1 → 85.0이다(같은 감점표, 다 풀린 것만 뺀 추정).
- C1-N02의 권장안은 Core 파일과 Core 쓰기 거래를 고친다. CLAUDE.md 규칙상 사용자 지시가 있어야 하므로 Q-SN3에서 묻는다. 고치지 않는 안(B·C)도 있고, 점수는 셋 다 같다(§3 Q-SN3).
- 독립 검토(새 에이전트 하나, 코드 대조) 결과는 §9에 있다. 이 판은 그 지적을 반영한 2판이다.

## 1. 지금 코드 (확인 사실)

### 1.1 C1-N01

- 원본이라 적힌 판정은 `V/Display/Unit/FieldUnitView.cs:386-393` `IsUnitModelPart`다(호진님 `62f8f2c9`). TMP·Canvas·`UnitModelBoundsExclusion` 아래 Renderer를 뺀다.
  - 쓰는 곳은 외형 층 수집 `PrepareMovementAppearance`(373-384)와 몸 경계 `TryGetModelBounds`(498-515) 둘이다.
  - 표지 `UnitModelBoundsExclusion.cs:5-8`의 문서는 "판정은 FieldUnitView 한 곳"이라고 적는다.
- Cartoon 대체 재질 `V/Display/Unit/CartoonModelMaterialFallback.cs:20-29`(민형님 `89e43fce`·`7eb7bdf3`)는 뿌리 아래 MeshRenderer·SkinnedMeshRenderer를 거르지 않고 모은다(25-28).
  - 적은 처음 그릴 때 `FieldUnitPresenter.cs:490`에서, 적 구조물은 `:517`에서 켜진다. 그때 체력 숫자(3D TextMeshPro, `FieldUnitPresenter.cs:937`에서 첫 대체보다 먼저 붙음)의 재질도 Cartoon 복제본으로 바뀐다.
  - 모든 캐릭터·적 구조물이 HUD를 받아(`:521`·`:559`) 체력 숫자가 꺼져 있어 지금 화면은 깨지지 않는다. 남는 비용은 적마다 쓰이지 않는 Material 복제 하나다.
  - 체력 숫자가 늘 비 Cartoon이라 지금은 모든 적이 "바꿀 렌더러 있음"(`NeedsConversion`)이다. 모델이 이미 Cartoon인 적도 대체를 켠다.
  - 생성자는 처음 켤 때 한 번만 돈다(`FieldUnitView.cs:401` `??=`). 커서는 아군에만 생기고(`CombatUnitActionCursorPresenter.cs:37-41`), 아군은 랙돌 사망 디졸브 때 임시로 대체를 켠다(`FieldUnitView.cs:430-433`). 그때 커서가 붙어 있으면 커서 재질도 바뀐다.
- 상태 모습 `V/Display/Unit/UnitStatusAppearance.cs:103-111`(우리 RE-b)은 TMP·Canvas만 뺀다. 주석은 "외형 층과 같은 규칙"이라고 적는다.
  - 커서(`CombatUnitActionCursorPresenter.cs:103-111`, 표지를 붙이고 유닛 아래에 둠)가 있을 때 모습이 바뀌면 커서 렌더러도 꺼져, 커서 오브젝트를 켜도 보이지 않는다.
  - 지금 모습을 바꾸는 상태는 적 전용 재구성 하나이고 커서는 아군에만 생겨 드러나지 않는다.
- 배치 고스트 `V/Display/Feedback/PlacementGhost.cs:200-205`도 유닛 프리팹 복제본에서 TMP·Canvas만 빼는 같은 식을 따로 갖는다(검토에서 찾은 넷째 사본, 경계 커밋 `^83fb2767` 줄이라 작성자 확인 필요). 고스트에는 커서·체력 숫자가 붙지 않아 지금 결과는 같다.
- 유닛 뿌리를 훑지만 일부러 모든 Renderer를 다루는 곳: `CombatEntryVisibility`(진입 숨김)·`CombatUnitPreparationLease`(준비 숨김)·`CombatUnitRagdoll.cs:233`(랙돌 SkinnedMeshRenderer 화면 밖 갱신). "모델 부분" 규칙과 상관없어 이번 범위가 아니다. `CombatTornadoAppearance`는 유닛이 아니라 토네이도 몸을 훑는다(`FieldTornadoPresenter.cs:316`).

### 1.2 C1-N02

- `V/Input/Interaction/CombatHudActionAvailability.cs`(현강님 `f6595326`, 계측 줄만 우리 `2f065c9f`)는 판(`QueryRevision`)이 바뀌면 `ObserveCharacters`(107-135)에서 다시 물을 유닛을 고른다.
  - 고르는 조건은 둘이다. 그 유닛의 사실이 바뀌었는가(`SameValues`), 그리고 "필드에 여러 칸 유닛이 있는가"가 바뀌었는가(113줄 `Area == Field && OccupiedCells.Count != 1`)이다.
  - 113줄 식은 Core `Core/CoreCaculator/Application/Combat/CombatAttackQueries.cs:63-64` `DefersMultiCellAttack`과 글자 그대로 같다.
- Core의 답(`CombatActionAdmission.GetAvailability`, 67-72)은 행위자 사실만 보지 않는다.
  - 공격 조건이 다른 유닛 전체를 본다(`UnitReady`: `input.Characters.Any(DefersMultiCellAttack)`).
  - 이동 허용량이 환경을 본다(`MovementAllowance`).
  - 이동 되돌리기가 다른 유닛의 점유를 본다(`MovementUndoCalculator.Evaluate`).
- 그래서 View는 "Core 답이 무엇에 기대는지"를 짐작해 옮겨 적은 셈이다. 지금은 맞는다. Core가 다른 유닛·환경에 기대는 조건을 더하면, 자기 사실이 그대로인 유닛의 HUD 아이콘이 턴 끝까지 옛 값으로 남는다.
- 다시 물을 때는 그 유닛의 아이콘을 먼저 비운다(`_show(id, null)`, 126줄). 판이 바뀌면 전경 입력과 상관없이 비우지만, 전경 입력 중에는 묻지 않는다(88줄).
- 답은 한 번에 하나씩 온다(미리보기 슬롯 하나, 배경 요청 규칙). 선택 유닛은 건너뛰고(96줄) UnitInfo와 같은 선택 추적기의 답을 쓴다(`Apply(selected)`, 85줄 — 매 Tick 불리지만 `_dirty`에 있을 때만 그려 중복을 거른다).
- 그래서 요청 수는 턴 시작 때 "자격 아군 수 − 선택 1"이고, 행동 뒤에는 보통 0이다(움직인 유닛은 대개 선택 유닛이고, 다른 유닛 사실은 그대로라서). 요청마다 Core가 입력을 새로 조립한다.
- 다시 물을 수 있는 유닛의 자격(`CombatActionAvailabilityTracker.IsEligibleActor`, 167-168줄: 아군·필드·살아 있음)도 View 사본이다. 쓰는 곳은 HUD(115줄)와 선택 추적기(161줄) 둘이다. Core 쪽 원본은 행위자 검사 `CombatActionAdmission.Actor`(22-39줄) 안에 있다.
- Core 입력에는 행위자만 바꾸는 `CombatCommandInput.WithActor`(`Core/CoreCaculator/Contracts/Combat/CombatCommandInput.cs:119`)가 있다. 순수 복사이고, 행동 가능 조회에서 행위자마다 다른 것은 요청의 `EntityId`뿐이라(조준·칸·번호·기대 값이 같음) 조립을 한 번 하고 `WithActor`로 돌아도 같은 답이 나온다(검토 확인).
- 조립기 `CombatInputAssembler.Assemble`(79-117)은 행위자를 넘기기만 하고 읽지 않는다. 메인 배치 미리보기가 이미 행위자 없이(`EntityId.Empty`) 돈다.
- 조회 서비스 `Core/CombatSystem/CombatPreviewService.cs`에는 종류를 다루는 곳이 셋이다.
  - 종류 → 명령 종류 switch(61-70). 모르는 종류면 예외라 Core 미리보기 Job이 F1로 멈춘다.
  - 결과 switch(77-92). 기본 갈래가 배치 계산이라, 빠뜨려도 예외 없이 엉뚱하게 돈다.
  - "판이 바뀌었으면 게시하지 않음" 조건(96줄).
- 결과 `CombatPreviewResult.cs:27-30`은 "내용은 하나" 개수를 검사한다.
- 게시 검증은 Buffer의 Core 쓰기 거래 `BufferSystems/Core/BattleTransactions.cs:195-204` `PublishPreview`(천명님 `df7b5d20`)에 있다. 행동 가능 답의 원본 판·전투·턴·개체를 요청과 맞춰 본다.

### 1.3 C1-N03

- 넉백 미리보기 `V/Display/Feedback/CombatKnockbackPreview.cs`(우리 KB·RF)의 규칙:
  - `ShowAim`에 넉백이 있으면 그것을 그리고, 없으면 포효 넉백을 그린다(43-50).
  - `ClearAim`이 불리면 포효 넉백을 다시 그린다(53-57).
  - 포효 넉백은 `_roar`에 복사해 둔다.
  - 문서(19-20줄)와 계약 `ICombatRoarForecastFeedback.cs:11-13`은 "어느 출처를 그릴지는 이 객체 한 곳이 정한다"고 적는다.
- 포효 예고 추적기 `V/Input/Interaction/CombatRoarForecastTracker.cs`도 넉백을 정한다.
  - 전경 입력 중(`hasForeground`, `CombatSceneInput.cs:159`의 `IsInputOwnerActive`)이면 넉백을 빈 목록으로 넘긴다(53-54, 106-113, 209-213).
  - 까닭은 이렇다. 공격 조준은 호버 칸을 옮길 때마다 자기 넉백을 지우고(`ClearShotPreview`), 미리보기의 `ClearAim → DrawRoar`가 그 사이에 포효 넉백을 끼워 그린다(RF-d 검토 M1).
- 그래서 한 결정이 서로를 보정하는 두 기억(`_aimShown`·`_roar`와 `_hideKnockback`)으로 나뉘어 있다. 한쪽만 고치면 깜빡임이나 넉백 누락이 생긴다.

## 2. 설계

### SN1 유닛 모델 부분 규칙 하나 (C1-N01)

- 새 `V/Display/Unit/UnitModelParts.cs`(정적 클래스)에 `internal static bool Is(Renderer renderer)`를 둔다. 식은 지금 `IsUnitModelPart` 그대로다(TMP·Canvas·`UnitModelBoundsExclusion` 아래 아님).
  - 문서에는 이 규칙을 쓰는 곳(외형 층·몸 경계·Cartoon 대체·상태 모습·배치 고스트)과, 유닛 뿌리를 훑지만 일부러 쓰지 않는 곳(진입 숨김·준비 숨김·랙돌 화면 밖 갱신)을 적는다.
- `FieldUnitView`는 `IsUnitModelPart`를 지우고 두 자리에서 `UnitModelParts.Is`를 부른다.
- `CartoonModelMaterialFallback` 생성자는 Mesh·Skinned 렌더러 가운데 `UnitModelParts.Is`인 것만 모은다(종류 조건은 "바꿀 재질이 있는 렌더러"라는 자기 규칙이라 그대로 둠).
- `UnitStatusAppearance.CaptureOriginals`는 `UnitModelParts.Is`를 부른다. "외형 층과 같은 규칙" 주석은 규칙 이름을 가리키게 고친다.
- `PlacementGhost`(200-205)도 `UnitModelParts.Is`를 부른다(Q-SN6). 고스트는 유닛 프리팹 복제본이라 같은 "모델 부분" 사실을 쓴다.
- `UnitModelBoundsExclusion` 문서는 "판정은 `UnitModelParts.Is` 한 곳"으로 고친다. 이름은 바꾸지 않는다(TP-1).

### SN2 HUD 행동 가능 묶음 조회 (C1-N02, 권장 A)

**Buffer 요청(우리 영역):**

- 새 미리보기 종류 `CombatViewPreviewKind.ActionAvailabilities`를 열거 끝에 둔다(행위자 없음).
- 요청 만들기 `CombatViewPreviewData.ActionAvailabilities()`(`BufferSystems/CombatView/CombatViewPreview.cs`)는 메인 배치 요청(37-41)처럼 `EntityId.Empty`로 만든다. 행동 가능 요청(67-71)의 `RequireEntity`는 부르지 않는다.

**Core(사용자 지시가 있을 때만):**

- 결과 칸 `CombatPreviewResult.ActionAvailabilities`(`IReadOnlyList<CombatActionAvailability>`)와 `ForActionAvailabilities(number, list)`. "내용은 하나" 개수 검사(27-30)에 새 칸을 더한다. 빈 목록도 정상 답이다(자격 아군 0명).
- `CombatCommandCalculator.GetActionAvailabilities(input)`은 Core 자격 규칙에 맞는 유닛마다 `GetAvailability(input.WithActor(id))`를 모은다.
  - 자격 규칙은 행위자 검사 `CombatActionAdmission.Actor`(36-39)의 진영·필드·생존 세 검사를 꺼낸 도우미 하나로 둔다. 도우미는 실패 이유(`MovementFailureReason`)를 돌려주고 진영을 인자로 받는다(가칭 `EligibilityFailure(actor, owner)`).
  - `Actor`는 자기 진영(자동 진행이면 적)으로 불러 지금과 같은 실패 이유를 내고, 묶음 계산은 아군으로 불러 이유가 없는 유닛만 모은다. 그래야 Core 안에 사본이 생기지 않는다.
  - 행동을 이미 쓴 유닛·보류 유닛도 목록에 든다(답이 "못 함"이라 HUD가 그것을 보임). 지금 View 자격(`IsEligibleActor`)과 같은 범위다.
- 조회 서비스 `CombatPreviewService`의 종류를 다루는 셋에 모두 새 종류를 더한다: 명령 종류 switch(61-70, 행동 가능과 같은 값), 결과 switch(77-92), 판 검사(96).
- 조립기는 행위자 없는 요청을 받는다(검토 확인, TP-3). 따로 대안 경로를 두지 않는다.
- 기록은 Core 전달 문서 §5에 새 절로 남기고 제목을 고친다(2026-11-02 규칙).

**Buffer의 Core 쓰기 거래(천명님 줄, 사용자 지시가 있을 때만):**

- `BattleTransactions.PublishPreview`(195-204)에 새 종류의 검증을 더한다. 답 목록의 모든 항목이 원본 판·전투·턴이 지금과 같은지 본다(개체는 요청에 없으므로 보지 않음). 이것을 빠뜨리면 새 종류는 검증 없이 게시된다.
- Core 전달 문서와 같은 절에 적는다.

**View:**

- `CombatHudActionAvailability`는 판이 바뀌면 묶음 조회를 한 번 낸다(배경 요청 규칙·전경 입력 중 안 묻기는 그대로).
  - 문지기는 지금 이름 있는 판정 `CombatInputPolicy.CanQueryActionAvailability`를 그대로 쓴다(TP-6). 새 종류는 `CanPreview`·튜토리얼 허용 표(198-209)에 넘기지 않는다.
  - 대신 종류 → 단계 표 `CombatInputPolicy.StageOf`(315-331)에는 새 종류를 전투 단계로 한 줄 더한다. 표가 모든 종류를 알게 하기 위해서다(모르는 종류면 예외).
- 답이 오면 받은 유닛마다 보이고, 받은 목록에 없는 유닛은 숨긴다.
- `ObserveCharacters`·`_observed`·`_next`·`_dirty`·`_hasNonSingleCellUnit`과 113줄 Core 식 사본을 걷는다. HUD 쪽 자격 사본(`IsEligibleActor` 호출)도 이 클래스에서는 빠진다. 선택 유닛 추적기는 그대로 쓴다.
- 답이 오기 전에는 이전 아이콘을 그대로 둔다(Q-SN4 A). 지금은 다시 물을 유닛의 아이콘을 비우는데, 묶음 조회에서 비우면 모든 유닛 아이콘이 매 행동마다 한 번 깜빡이기 때문이다.
- 선택 유닛의 답(`selected`, UnitInfo와 같은 추적기)은 지금처럼 바로 보인다. 같은 판의 같은 Core 함수 답이라 묶음 답과 값이 같다.
  - `Apply(selected)`는 매 Tick 불린다. 지금은 `_dirty`가 중복을 걸렀으므로, 걷은 뒤에는 "마지막으로 보인 답"과 같으면 그리지 않는 거름을 둔다. 없으면 매 프레임 HUD 쓰기(`FieldUnitPresenter.cs:574`)가 생긴다.
  - 묶음 답 안의 선택 유닛 항목은 선택 추적기 답과 겹친다. 같은 값이라 둘 중 늦게 온 것을 그대로 쓴다(TP-9).
- 첫 동작 준비 리허설 `CombatFirstUseRehearsal.cs:149`(우리 FU 줄, Core)에 묶음 계산 한 줄을 더한다. 첫 묶음 조회의 JIT를 로딩 가림 안으로 옮기기 위해서다(TP-7).

### SN3 넉백 출처 고르기 한 곳 (C1-N03, 권장 A)

- `CombatKnockbackPreview`가 규칙 하나로 정한다. 조준 넉백이 있으면 조준을, 없고 전경 입력 중이면 아무것도 그리지 않고, 둘 다 아니면 포효 넉백을 그린다.
- 이를 위해 사실 하나를 받는다: `SetForegroundInput(bool active)`. 바뀔 때만 다시 그린다.
- 포효 예고 계약 `ICombatRoarForecastFeedback`에 `SetForegroundInput(bool)`을 더한다. 구현은 `CombatAttackFeedback`이고, 미리보기에 넘기기만 한다.
- 추적기는 Core 답을 그대로 넘긴다(`_hideKnockback`과 빈 목록 갈래를 걷음). 바닥 범위는 지금처럼 전경 입력 중에도 남긴다.
- 추적기는 `CanKeep` 확인 뒤(지금 `_hideKnockback`을 맞추는 108줄 자리) **매 Sync** `SetForegroundInput(hasForeground)`를 부른다. 그린 것이 있을 때만 넘기지 않는다.
  - 까닭: `_shown`일 때만 넘기면, 전경 입력 중에 처음 받은 답이 포효 넉백을 그려 RF-d M1 깜빡임이 돌아온다.
  - 바뀜 거르기는 미리보기가 한다. 추적기에는 전경 사실의 사본을 두지 않는다.
- 문서는 둘을 고친다. 미리보기 remarks는 "규칙 = 조준 > (전경 입력 ? 없음 : 포효)"로, 계약 remarks는 "부르는 쪽은 Core 응답과 전경 입력 사실을 넘긴다"로 적는다.
- 보이는 동작은 지금과 같다. 지금 동작을 셋으로 나눠 맞춰 보면 이렇다.
  - 조준 넉백이 있으면 조준을 그린다.
  - 조준 넉백이 없고 전경 입력 중이면, 추적기가 비운 목록이라 아무것도 그리지 않는다.
  - 그 밖에는 포효를 그린다.

## 3. 정할 것

> 2026-11-11 결정: 아래 여섯 모두 권장(A). Q-SN3 A는 사용자가 Core 변경을 허락했다.

| 질문 | 선택지 | 권장 |
|---|---|---|
| **Q-SN1** 모델 부분 규칙의 자리 | A 새 정적 클래스 `UnitModelParts` / B `FieldUnitView.IsUnitModelPart`를 `internal static`으로 열어 다른 둘이 부름 | **A** — Cartoon 대체·상태 모습이 `FieldUnitView`에 기대지 않고, 규칙 문서를 한 곳에 둔다 |
| **Q-SN2** 체력 숫자·커서가 Cartoon 대체·상태 모습에서 빠지는 차이 | A 받아들임 / B 지금처럼 둘 다 포함(규칙 둘 유지) | **A** — 체력 숫자는 TMP 재질이 맞고, 커서는 몸이 아니다. 지금 화면에서 보이는 차이는 없다(§5) |
| **Q-SN6** 배치 고스트(넷째 사본)도 같은 규칙을 부를지 | A 부름(커서 표지 아래도 빠짐) / B 그대로 두고 "고스트는 자기 규칙"이라고 주석 | **A** — 고스트도 유닛 프리팹 복제본의 "모델 부분"을 고르는 같은 사실이다. 고스트에는 커서·체력 숫자가 없어 결과는 같다. 줄 작성자는 확인이 필요하다(경계 커밋) |
| **Q-SN3** C1-N02 고치는 방법 | A Core 묶음 조회 — **이 세션이 Core 파일 넷과 Buffer의 Core 쓰기 거래(천명님 줄)를 고침, 사용자 지시 필요** / B View만 — 판이 바뀌면 모든 아군을 하나씩 다시 물음 / C 지금 구조 + Core가 "다른 유닛에 영향을 주는 조건" 판정을 공개 함수로 줌(Core 변경, View는 계속 짐작) | **A** — Core 식 사본과 "무엇에 기대는지 짐작"이 함께 없어진다. 대신 비용이 바뀐다(아래). B는 Core를 안 건드리지만 판마다 조립이 아군 수만큼 생겨 가장 비싸고, 답이 하나씩 와 아이콘이 차례로 바뀐다. C는 비용이 지금과 같지만 짐작이 남는다. **점수는 셋 다 C1 60으로 같다**(C는 중간이 낮음으로 줄고, C1 낮음은 이미 상한 −10이라서). 그래서 고르는 기준은 점수가 아니라 정확성·비용·Core 변경 여부다 |
| **Q-SN4** 다시 물을 때 아이콘 | A 답이 올 때까지 이전 값 유지 / B 지금처럼 비움 | **A** — 묶음 조회(보통 1~2 Tick)에서 비우면 행동마다 모든 아이콘이 깜빡인다. 대신 그 틈 동안 옛 값(틀릴 수 있음)이 남는다. 전경 입력 중에는 묻지 않으므로, 결과가 온 뒤 바로 공격 모드·끌기에 들어가면 그 입력이 끝날 때까지 옛 값이 남는다(§5). B를 고르면 Q-SN3 B와 함께여야 자연스럽다 |
| **Q-SN5** C1-N03 고치는 방법 | A 미리보기 한 곳 + 전경 입력 사실 전달(동작 같음) / B 공격 조준이 "조준 시작·끝"을 알려 미리보기가 정함 — 이동·배치·아이템 끌기 중에도 포효 넉백이 보이게 바뀜 / C 문서만 고침(두 곳이 서로 보정함을 양쪽에 적음) | **A** — 동작이 같고, 사실을 넘기는 통로는 이미 있는 계약 하나에 멤버 하나다. B는 원래 까닭(조준 호버 깜빡임)에 더 가깝지만 보이는 차이가 생긴다. C는 중간이 그대로다 |

**Q-SN3 비용 비교(검토 반영):**

| | 턴 시작 | 행동 뒤(결과 하나) | 선택 유닛 |
|---|---|---|---|
| 지금 | 조립·계산 (자격 아군 − 1)번, 한 Tick에 하나씩 | 보통 0번(사실이 바뀐 비선택 유닛만) | 선택 추적기가 따로 물음 |
| A | 조립 1번 + 계산 N번(N = 자격 아군) | 조립 1번 + 계산 N번 | 선택 추적기도 따로 물어 두 번 계산 |
| B | 조립·계산 (N − 1)번 | 조립·계산 (N − 1)번 | 같음 |
| C | 지금과 같음 | 지금과 같음 | 지금과 같음 |

- 계산 한 번(`GetAvailability`)에는 이동 행위자 검사, 이동 허용량, 이동 되돌리기 평가(`MovementEffectUndo.Evaluate` — 보스 포효 대기·효과 되돌림 포함)가 들어 있다.
- 11-10 기준 기록에서 Core 미리보기가 GC 출처의 46%였다. A를 고르면 SN-c 뒤 Profiler로 행동 한 번의 추가 비용을 잰다(TP-8). 미리보기 종류별 계측 표지는 열거에서 자동으로 생긴다(TP-4).

## 4. 7기준 점검

| 기준 | 지금 | 바꾼 뒤 | 근거 |
|---|---|---|---|
| C1 SSOT | 중간 7 | 중간 4 | 세 사실이 한 곳씩이 된다(모델 부분 규칙은 넷째 사본까지). View의 자격 사본 `IsEligibleActor`는 선택 추적기에 남는다(낮음 그대로). Core 쪽 자격은 `Actor`와 새 함수가 같은 도우미를 불러 한 곳이다 |
| C2 SOLID | — | 같음 | `UnitModelParts`는 정적 규칙 하나다. HUD 조회는 "무엇에 기대는지 짐작"이 빠져 하는 일이 줄어든다. 묶음 조회는 Core 계약에 결과 칸 하나를 더한다(종류마다 갈래인 지금 구조 그대로). 종류를 다루는 곳이 Core 셋·Buffer 게시 검증 하나·View 단계 표 하나라, 새 종류는 다섯 곳을 함께 고쳐야 한다(지금 구조의 성질, 새 문제 아님) |
| C3 FW 계약 | — | 같음 | Buffer 요청 → Core 서비스 → Buffer 게시 흐름 그대로다. Factory·서비스 키는 바뀌지 않는다 |
| C4 독립성 | — | 나아짐(낮음) | Core가 행동 가능 조건을 바꿔도 View를 고칠 일이 없다 |
| C5 불필요 | — | 나아짐(낮음) | 적마다 쓰이지 않던 Material 복제가 없어진다. Q-SN3 A면 다시 물을 유닛 고르기 반복이 없어지지만, 행동마다 Core 계산이 늘어난다(위 비용 비교) |
| C6 C#·Unity | — | 같음 | — |
| C7 매 프레임 | — | 같음 | 묶음 조회는 판이 바뀔 때만 돈다. 선택 답의 매 Tick 중복 그리기는 거름을 둔다(SN2 View) |

- 새로 생길 수 있는 문제:
  - Q-SN3 A의 Core 결과 칸이 미리보기 결과 형식에 하나 더 늘어난다(지금도 종류마다 칸이 하나씩이라 같은 꼴이다).
  - 게시 검증을 빠뜨리면 새 종류가 검증 없이 게시된다. SN2에서 `PublishPreview`를 함께 고치는 것으로 막는다.
  - 선택 답 거름을 빠뜨리면 매 프레임 HUD 쓰기가 생긴다(C7). SN2에 넣었다.
- 예상: 요청 7기준 79.3 → 82.5, 12기준 83.1 → 85.0(재평가 §5 순서 1).

## 5. 보이는 차이

- SN1:
  - 적·적 구조물의 체력 숫자 재질이 Cartoon 복제본으로 바뀌지 않는다. HUD를 쓰는 동안은 숫자가 꺼져 있어 지금 화면은 같다.
  - 모델이 이미 Cartoon인 적은 대체를 켜고 끄는 일 자체가 없어진다(지금은 체력 숫자 때문에 모든 적이 켬, 외형 층을 놓고 다시 잡는 일도 없어짐).
  - 아군 랙돌 사망 때 임시 대체가 커서 재질을 바꾸지 않는다. 상태 모습이 바뀌어도 커서는 꺼지지 않는다. 지금 모습을 바꾸는 상태는 적 전용이고 커서는 아군에만 생겨 화면은 같다.
  - 배치 고스트는 커서·체력 숫자가 없어 같다.
- SN2(A):
  - 행동 뒤 HUD 아이콘이 비었다가 다시 차는 일이 없어지고, 모든 아군의 답이 한 번에 바뀐다. 결과 값은 같다.
  - 판이 바뀐 뒤 답이 올 때까지(보통 1~2 Tick) 옛 아이콘이 남는다. 결과 직후 공격 모드·끌기에 들어가면 그 입력이 끝날 때까지 남는다(Q-SN4). 지금은 사실이 바뀐 유닛의 아이콘이 그동안 비어 있다.
  - 행동마다 Core 조립 1번 + 계산 N번이 늘어난다(§3 비용 비교).
- SN3(A): 없음.

## 6. 다른 작업자 공유 (구현 뒤, 미전달로 작성)

- 호진님: `IsUnitModelPart`가 `UnitModelParts.Is`로 옮겨짐, 표지 문서 문장.
- 민형님: Cartoon 대체 생성자가 모델 부분 규칙을 부름(체력 숫자·커서 제외). Factory 변경은 없다.
- 배치 고스트 줄(`PlacementGhost.cs:204`): 경계 커밋 줄이라 작성자를 GitHub 파일 이력으로 확인한 뒤 그 사람에게 알린다(계획 착수 전 확인, 11-05 SB-0과 같은 방법).
- 현강님: HUD 행동 가능 조회가 묶음 조회로 바뀜(Q-SN3 A) 또는 다시 묻기 규칙이 바뀜(B·C).
- 천명님(Q-SN3 A일 때): Core 미리보기 결과 칸·계산 함수·자격 도우미·조회 서비스 셋, Buffer 쓰기 거래 `PublishPreview` 검증, 첫 동작 리허설 한 줄. Core 전달 문서 §5 새 절과 제목. C일 때는 공개 판정 함수.

## 7. Phase(안)

| Phase | 내용 | 영역 |
|---|---|---|
| SN-a | SN1 모델 부분 규칙(외형 층·몸 경계·Cartoon 대체·상태 모습·배치 고스트) | View만 |
| SN-b | SN3 넉백 출처 한 곳 | View만 |
| SN-c | SN2 묶음 조회(Q-SN3 A면 Buffer 요청 → Core·게시 검증 → View 순서, B면 View만, C면 Core 공개 함수 → View) | Core·Buffer·View |
| SN-d | 문서·공유 메모·묶음 끝 검토·평가 아티팩트 14차 | 문서 |

- 새 시험은 쓰지 않는다(View 작업 규칙). 컴파일은 사용자가 Unity에서 확인한다.

## 8. 계획에서 정할 것 (TP, 확인 대상)

- **TP-1:** 표지 `UnitModelBoundsExclusion`은 이름을 바꾸지 않는다(런타임에 붙이는 컴포넌트지만, 바꿀 이득이 이름뿐이라서).
- **TP-2:** `UnitModelParts`는 `Display/Unit/`에 둔다(폴더 배치 안내 "유닛 표시 부품").
- **TP-3(검토로 답함):** Core 조립기는 행위자 없는 요청을 받는다(`CombatInputAssembler.Assemble` 79-117은 행위자를 읽지 않고, 메인 배치 미리보기가 이미 `EntityId.Empty`로 돈다). 대안 경로는 두지 않는다.
- **TP-4(검토로 답함):** 계측 표지는 고칠 필요가 없다. Core `CoreProfilerMarkers.CreatePreviewKinds`(35-48)와 View `CombatInputProfiler`(17-18)가 열거에서 표지를 만든다. HUD의 고정 `Apply(ActionAvailability)` 표지(143줄)만 새 종류로 바꾼다.
- **TP-5:** Unity 확인은 모든 Phase 뒤 한 번에 한다(최근 묶음과 같은 방식).
- **TP-6:** HUD 문지기는 `CanQueryActionAvailability` 그대로 두고, 새 종류는 `StageOf`에만 한 줄 더한다(튜토리얼 허용 표·`CanPreview`에는 넘기지 않음).
- **TP-7:** 첫 동작 리허설에 묶음 계산 한 줄을 더한다(Q-SN3 A일 때).
- **TP-8:** Q-SN3 A면 SN-c 뒤 Profiler로 행동 한 번의 추가 비용을 잰다. 무거우면 사용자에게 보고하고 정한다(되돌림 또는 C).
- **TP-9:** 선택 유닛은 선택 추적기와 묶음 답 양쪽에서 계산되는 것을 받아들인다(같은 값, 선택 추적기는 UnitInfo도 씀).

## 9. 독립 검토 기록 (2026-11-11, 새 에이전트 하나, 읽기 전용, 기준 `ec0148266`)

높음 0 · 중간 3 · 낮음 6. 모두 반영했다.

| 번호 | 지적 | 반영 |
|---|---|---|
| M1 | SN2 고칠 곳이 빠짐: 조회 서비스 switch 둘·판 검사, 결과 개수 검사, Buffer 쓰기 거래 게시 검증(천명님 줄), 요청 만들기의 `RequireEntity`, View 단계 표·튜토리얼 허용 표 | §1.2·SN2에 모두 적음, §0 고칠 파일 수 고침, TP-6 |
| M2 | Q-SN3 A의 비용 설명 "판마다 한 번(지금과 비슷)"이 틀림 — 지금은 행동 뒤 보통 0번 | §3에 비용 비교 표, §5 차이, TP-8 측정 |
| M3 | 넷째 사본 `PlacementGhost.cs:204` 빠짐, 제외 목록의 토네이도는 유닛이 아님, 랙돌이 빠짐 | §1.1·SN1·Q-SN6, 제외 목록 고침 |
| L1 | TP-3 대안 경로 필요 없음(조립기가 받음), 자격 0명이면 대안이 못 돎 | TP-3 답, 빈 목록 = 정상 답 |
| L2 | 새 Core 자격 함수가 `Actor` 검사와 사본이 되고, View 자격 사본은 남음 | `Actor`의 세 검사를 실패 이유 도우미로 꺼내 둘이 같이 부르게(이유를 잃지 않게), §4 C1 문장 고침 |
| L3 | Q-SN4 A는 틈 동안 옛 값이 남음(전경 입력 중 내내), 선택 답 매 Tick 중복 그리기 | Q-SN4·§5에 적음, SN2 View에 거름 |
| L4 | SN3 전경 사실 전달 자리·조건을 못 박아야 함(`_shown`일 때만이면 깜빡임이 돌아옴) | SN3에 "매 Sync, `CanKeep` 뒤" |
| L5 | 줄 번호·사실 보정(적 구조물도 Cartoon 대체, 커서는 아군만 → 커서 재질 경로는 아군 랙돌 사망 임시 대체) | §1 고침 |
| L6 | Q-SN3 B·C도 C1 60으로 점수가 같음 | Q-SN3 권장 칸에 적음 |

확인된 사실(검토): C1-N01 판정·작성자, Cartoon 대체가 거르지 않음, 체력 숫자가 3D TextMeshPro이고 첫 대체보다 먼저 붙음 / C1-N02 113줄 식 = Core `DefersMultiCellAttack`, Core 답이 다른 유닛·환경을 봄, `WithActor`가 순수 복사라 같은 답 / C1-N03 지금 동작 행렬을 SN3 규칙이 그대로 재현함(한 프레임 순서: 입력 분배 → 입력 Tick 응답 → Tick 끝 포효 Sync, 모드 끝 `ClearAim` 때는 전경 사실이 아직 참이고 같은 프레임 Tick 끝에서 포효를 그림) / 점수 C1 37.5 → 60, 7기준 79.3 → 82.5, 12기준 83.1 → 85.0.

## 10. 구현 메모 (2026-11-12, SN-a~SN-c 코드 완료, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

실행 기록 원본은 [계획](../plans/2026-11-11-combat-view-new-ssot-plan.md) §10이다. 설계와 다른 점과 구현에서 정한 것만 적는다.

- **SN1(SN-a):** 규칙 `Display/Unit/UnitModelParts.Is(Renderer)` 하나. 부르는 곳은 외형 층·몸 경계(`FieldUnitView`, 옛 `IsUnitModelPart` 지움)·Cartoon 대체 생성자 두 반복·상태 모습 `CaptureOriginals`·배치 고스트. 일부러 부르지 않는 곳(등장 가시성·준비 대여·랙돌)은 규칙 클래스 문서에 적었다. 설계와 다른 점 없음.
- **SN3(SN-b):** 넉백 미리보기에 새 입구 `SetForegroundInput(bool)`(같으면 돌아감). 추적기는 `CanKeep`·판 바뀜 처리 뒤, 응답 소비 앞에서 매 Sync 넘긴다(검토 L4대로). 옛 `_hideKnockback`·빈 목록 갈래를 걷었다. 프레임 순서 대조로 동작이 같음을 확인했다(계획 §10.3). 설계와 다른 점 없음.
- **SN2(SN-c):**
  - Core 자격 원본 `CombatActionAdmission.EligibilityFailure(actor, owner)`를 `Actor`와 묶음 계산 `GetAvailabilities`가 함께 부른다(검토 L2대로). 결과 `ForActionAvailabilities`는 배열 복사·빈 항목/빈 개체/중복 개체 거절·빈 목록 받음.
  - 조회 서비스는 항목마다 원본 판을 붙이는 private `WithRevision`을 둔다. 게시 검증은 단일·묶음이 private `IsCurrentAvailability`를 함께 쓴다(단일 결과 같음).
  - View HUD는 유닛마다 마지막으로 보인 답 `_shown`을 기억하고, 답 적용 때 재사용 목록 둘(`_seen`·`_gone`)을 쓴다. `_gone`은 설계에 없던 것으로, 기억을 돌면서 지울 수 없어 숨길 유닛을 모은다.
  - 문지기는 `CanQueryActionAvailability` 그대로, 새 종류는 `StageOf`에만 한 줄(TP-6).
  - 기록은 Core 전달 문서 §5.87·§72(제목 갱신).
- 공유(모두 미전달): 호진님 구조 공유 §21, 민형님 Factory 공유 §57, 현강님 구조 공유 §29, 천명님 Core 전달 문서 §72.
- **묶음 끝 검토(2026-11-12, 새 에이전트 하나):** 높음 0 · 중간 0 · 낮음 2 · 개선 제안 7, 약 8.7. 낮음 L-D1(HUD 적용 계측 표지에 선택 답이 섞임)은 사용자 "L-D1만 진행해"로 반영(표지를 묶음 답 적용 한 번으로, 계획 §10.6), L-D2(Buffer 묶음 검증 예외 문구가 좁음)는 그대로. 자세한 내용은 계획 §10.5.
