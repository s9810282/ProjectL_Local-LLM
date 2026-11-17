# 전투 View 새 SSOT 셋 Task 계획 (2026-11-11)

> **Merge compatibility update (2026-11-15):** `CombatViewPreviewKind.ActionAvailabilities` now uses value **14**, because the merged player-skill preview kinds occupy 11–13. References to value 11 below record the original implementation. The named request/result factories and batch-query behavior are unchanged. See [boundary handoff §5.93](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#593-develop-merge-skill-and-batch-availability-previews-2026-11-15).

> 설계: [new-ssot-design](../specs/2026-11-11-combat-view-new-ssot-design.md)(SN1~SN3).
> 결정(2026-11-11 사용자 "권장안대로 하고 Task 계획 써줘, Core 변경 우리가 해도 괜찮아"): Q-SN1~Q-SN6 모두 A. Q-SN3 A의 Core 파일·Buffer의 Core 쓰기 거래 변경은 이 작업이 한다.
> 기준 판 `ec0148266`. **코드 착수 전.**
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `Core/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/`, `B/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/`.

## 진행 규칙

- Phase가 끝나면 멈추고 보고한다(CLAUDE.md).
- 새 시험은 쓰지 않는다. MSBuild는 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
- 기존 시험은 바꾸지 않는다. 바뀌는 Core 함수를 지나는 Core 시험(SN-0 T0.3: 직접 부르는 11개 파일, 행위자 검사를 지나는 36개 파일)은 동작이 같아야 하는 확인 대상이다(§7 U-12).
- Unity 확인은 모든 Phase 뒤 한 번에 한다(TP-5).
- 다른 작업자 줄은 고친 뒤 공유 메모로 넘긴다(§9). 공유는 모두 미전달로 쓴다.

## 0. 계획에서 정한 것 (TP, 사용자 확인 대상)

설계 §8의 TP-1~TP-9에 계획에서 정한 것을 더했다. **설계와 다르거나 설계에 없던 것은 굵게** 적었다.

| 번호 | 내용 |
|---|---|
| TP-1 | 표지 `UnitModelBoundsExclusion`은 이름을 바꾸지 않는다. 문서만 고친다 |
| TP-2 | `UnitModelParts`는 `V/Display/Unit/`에 둔다 |
| TP-3 | (검토로 답함) Core 조립기는 행위자 없는 요청을 받는다. 대안 경로는 두지 않는다 |
| TP-4 | (검토로 답함) 계측 표지는 열거에서 자동으로 생긴다. HUD의 고정 `Apply(ActionAvailability)` 표지만 새 종류로 바꾼다 |
| TP-5 | Unity 확인은 모든 Phase 뒤 한 번에 한다 |
| TP-6 | HUD 문지기는 `CombatInputPolicy.CanQueryActionAvailability` 그대로 두고, 새 종류는 `StageOf`에만 한 줄 더한다 |
| TP-7 | 첫 동작 리허설에 묶음 계산 한 줄을 더한다 |
| TP-8 | SN-c 뒤 Profiler로 행동 한 번의 추가 비용을 잰다(사용자 Unity 확인 때). 무거우면 보고하고 정한다 |
| TP-9 | 선택 유닛은 선택 추적기와 묶음 답 양쪽에서 계산되는 것을 받아들인다 |
| **TP-10** | 새 열거 값은 `CombatViewPreviewKind.ActionAvailabilities = 11`(맨 끝)이다 |
| **TP-11** | Core 자격 도우미는 `CombatActionAdmission.EligibilityFailure(CharacterCombatFacts actor, Owner owner)`다. `MovementFailureReason`을 돌려주고, 검사 순서는 지금 `Actor`와 같다(진영 → 필드 → 생존). 그래서 `Actor`의 실패 이유는 바뀌지 않는다 |
| **TP-12** | 묶음 답의 순서는 Core 입력의 캐릭터 순서다. View는 순서에 기대지 않는다 |
| **TP-13** | Buffer 게시 검증은 지금 단일 답 검증 식을 도우미 하나(가칭 `IsCurrentAvailability`)로 꺼내 단일·묶음이 같이 부른다. 묶음은 항목마다 그 도우미 + 개체가 비지 않음. 개체 중복은 결과 형식이 막는다(TP-14) |
| **TP-14** | `CombatPreviewResult.ForActionAvailabilities`는 목록을 배열로 복사하고, null 항목·빈 개체·중복 개체를 막는다(모양 검사). "지금 판인가"는 Buffer가 본다(TP-13). 두 검사는 다른 사실이라 사본이 아니다 |
| **TP-15** | HUD `Sync`에서 `characters` 인자를 뺀다. `CombatSceneInput.SyncHudActionAvailability`와 `CombatSceneInputDriver.cs:150` 호출도 같이 고친다 |
| **TP-16** | 판이 바뀐 뒤 자격을 잃은 유닛(죽음 등)의 아이콘은 묶음 답이 올 때 숨긴다. 지금은 판이 바뀌는 순간 숨긴다(§5 보이는 차이에 더함) |
| **TP-17** | 선택 답 거름: HUD가 유닛마다 "마지막으로 보인 답"을 기억하고, 같은 객체면 다시 그리지 않는다 |
| **TP-18** | 넉백 미리보기의 `SetForegroundInput`은 값이 바뀔 때만 일한다. 조준 넉백을 그리는 중이면 기억만 하고, 아니면 포효 넉백을 다시 그리거나 지운다 |
| **TP-19** | 포효 예고 추적기가 정리(`Clear`)되면 미리보기의 전경 사실은 마지막 값으로 남는다. 다음 `Sync`가 다시 그리기 전에 맞추므로 해가 없다 |
| **TP-20** | 배치 고스트는 지금 반복문을 그대로 두고 조건만 `!UnitModelParts.Is(renderer)`로 바꾼다 |

## 1. Phase 목록

| Phase | 내용 | 영역 | 커밋 묶음 |
|---|---|---|---|
| SN-0 | 착수 전 확인 | 코드 없음 | — |
| SN-a | SN1 모델 부분 규칙 | View | 하나 |
| SN-b | SN3 넉백 출처 한 곳 | View | 하나 |
| SN-c | SN2 묶음 조회 | Buffer·Core·View | 하나(나누면 컴파일이 깨짐) |
| SN-d | 문서·공유·묶음 끝 검토·평가 아티팩트 14차 | 문서 | 하나 |

## 2. SN-0 착수 전 확인 (코드 변경 없음)

- **T0.1 원격:** `git fetch origin feature/Yonggeun/Refector-Stage develop`. 원격 브랜치가 HEAD보다 앞서면 빨리 감기로 받는다. develop이 앞서면 고칠 파일과 겹치는지 본다.
- **T0.2 작성자 줄:** 고칠 줄의 작성자를 `git blame`으로 다시 센다. 경계 커밋(`^`) 줄은 GitHub 파일 이력으로 확인한다(11-05 SB-0과 같은 방법). 대상:
  - `V/Display/Unit/FieldUnitView.cs:386-393`(호진님 예상), `CartoonModelMaterialFallback.cs:20-29`(민형님 예상), `UnitModelBoundsExclusion.cs`, `UnitStatusAppearance.cs:103-111`(우리), `V/Display/Feedback/PlacementGhost.cs:204`(경계 커밋)
  - `V/Input/Interaction/CombatHudActionAvailability.cs`(현강님 예상), `CombatActionAvailabilityTracker.cs:167-168`
  - `B/Core/BattleTransactions.cs:195-204`(천명님 `df7b5d20`), `Core/CombatSystem/CombatPreviewService.cs:61-99`
  - `Core/CoreCaculator/Contracts/Combat/CombatPreviewResult.cs`, `Core/CoreCaculator/Application/Combat/CombatActionAdmission.cs:22-39·67-72`, `CombatCommandCalculator.cs:36`, `CombatFirstUseRehearsal.cs:149`
  - `V/Input/CombatInputPolicy.cs:315-331`, `V/Input/CombatSceneInput.cs:161-164`, `V/Input/CombatSceneInputDriver.cs:150`
  - 포효 예고 셋(우리 RF): `CombatKnockbackPreview.cs`, `ICombatRoarForecastFeedback.cs`, `CombatAttackFeedback.cs:89-107`, `CombatRoarForecastTracker.cs`
- **T0.3 시험 참조:** `Assets/Tests`에서 바뀌는 형식·함수를 부르는 곳을 다시 센다. 11-11 계획 때 센 결과는 이렇다.
  - 0곳: `ICombatRoarForecastFeedback`·`CombatHudActionAvailability`·`CombatKnockbackPreview`·`CartoonModelMaterialFallback`·`UnitStatusAppearance`·`PlacementGhost`·`CombatRoarForecastTracker`·`IsEligibleActor`
  - 고치지 않고 그대로 돌아야 하는 곳(천명님):
    - `ReconstructionReviveTests.cs:43·45·71` — `CombatActionAdmission.Actor`
    - `MovementUndoPublicationTests.cs:89·106` — `PublishPreview` + `ForActionAvailability`
- **T0.4 Core 전달 문서 번호:** 지금 마지막은 §5.86·§71이다. 새 절은 §5.87·§72로 예상하고, 쓸 때 다시 확인한다.

## 3. SN-a 모델 부분 규칙 (SN1)

- **Ta.1 새 `V/Display/Unit/UnitModelParts.cs`**(+ `.meta`, Unity 기본 형식·새 GUID):
  - `internal static class UnitModelParts`에 `internal static bool Is(Renderer renderer)` 하나를 둔다. 식은 지금 `FieldUnitView.IsUnitModelPart`(390-393) 그대로다. null이면 `ArgumentNullException`.
  - 요약 문서에는 이 규칙을 쓰는 곳 다섯(외형 층·몸 경계·Cartoon 대체·상태 모습·배치 고스트)과, 일부러 쓰지 않는 곳 셋(진입 숨김·준비 숨김·랙돌 화면 밖 갱신)을 적는다.
  - 네임스페이스는 폴더 경로 `…ViewSystem.Combat.Display.Unit`이다.
- **Ta.2 `FieldUnitView`:** `IsUnitModelPart`(386-393)를 지우고, 두 자리(`PrepareMovementAppearance` 381줄·`TryGetModelBounds` 508줄)를 `UnitModelParts.Is`로 바꾼다.
- **Ta.3 `CartoonModelMaterialFallback` 생성자(25-28):** 두 반복문에 `if (UnitModelParts.Is(renderer))` 조건을 넣는다. 종류 조건(Mesh·Skinned)은 "바꿀 재질이 있는 렌더러"라는 자기 규칙이라 그대로 둔다. 클래스 remarks에 "유닛 모델 부분만(체력 숫자·커서 제외)" 한 줄을 더한다.
- **Ta.4 `UnitStatusAppearance.CaptureOriginals`(103-111):** 조건을 `UnitModelParts.Is(renderer)`로 바꾸고, 주석 "외형 층과 같은 규칙"을 규칙 이름으로 고친다. 더는 쓰지 않는 `TMP_Text`·`Canvas` using이 생기면 지운다.
- **Ta.5 `PlacementGhost`(204):** 조건을 `!UnitModelParts.Is(renderer)`로 바꾼다(TP-20). 필요하면 using을 더한다.
- **Ta.6 `UnitModelBoundsExclusion` 문서:** "판정은 `UnitModelParts.Is` 한 곳"으로 고친다.
- **확인:** `V/` 안에서 `GetComponentInParent<TMP_Text>`와 `GetComponentInParent<UnitModelBoundsExclusion>`를 다시 찾는다. 남은 것이 "일부러 안 쓰는 곳"인지 본다.

## 4. SN-b 넉백 출처 한 곳 (SN3)

- **Tb.1 `CombatKnockbackPreview`:**
  - 필드 `_foreground`(전경 입력 중인가)를 더하고, `internal void SetForegroundInput(bool active)`를 만든다. 값이 같으면 돌아가고, 다르면 기억한 뒤 `_aimShown`이 아닐 때만 `DrawRoar()`를 부른다(TP-18).
  - `DrawRoar`는 `_foreground || _roar.Count == 0`이면 캔버스를 지운다.
  - `ShowAim`·`ClearAim`·`ShowRoar`·`ClearRoar`는 그대로 둔다. `DrawRoar`가 전경을 보므로 규칙이 한 곳에서 나온다.
  - 클래스 remarks의 출처 문단을 "규칙 = 조준 넉백이 있으면 조준 > 전경 입력 중이면 없음 > 포효"로 고치고, 전경 사실을 받는 까닭(RF-d 검토 M1)을 한 줄 적는다.
- **Tb.2 `ICombatRoarForecastFeedback`:** `void SetForegroundInput(bool active);`를 더한다. remarks의 "부르는 쪽은 Core 응답을 그대로 넘깁니다"를 "Core 응답과 전경 입력 사실을 그대로 넘깁니다"로 고친다.
- **Tb.3 `CombatAttackFeedback`:** 명시 구현 `void ICombatRoarForecastFeedback.SetForegroundInput(bool active)`를 만든다. 해제 뒤면 돌아가고, 아니면 `_knockback.SetForegroundInput(active)`를 부른다. 자리는 포효 예고 region(89-107) 안이다.
- **Tb.4 `CombatRoarForecastTracker`:**
  - `_hideKnockback` 필드와 뒤집기 블록(106-113)을 지운다.
  - 그 자리에서 매 Sync `_display.SetForegroundInput(hasForeground)`를 부른다. `CanKeep`과 판 바뀜 처리 뒤, 응답 소비 앞이다.
  - `Redraw`는 `_attacks`를 그대로 넘긴다(209-213의 빈 목록 갈래를 지움).
  - 클래스 remarks(25-27)의 "넉백 화살표는 숨깁니다"를 "넉백을 그릴지는 넉백 미리보기가 정합니다(전경 입력 사실을 넘김)"로 고친다. `Sync`의 `hasForeground` 문서도 고친다.
- **확인(코드 대조):** 설계 §2 SN3의 동작 표(조준 있음 / 없음 + 전경 / 그 밖) 셋이 바뀐 코드로 같게 나오는지 본다. 한 프레임 안 순서는 입력 분배 → 입력 Tick 응답 → Tick 끝 포효 Sync다. 공격 모드가 끝나는 프레임도 본다(`ClearAim` 때 전경은 아직 참 → 같은 프레임 Tick 끝에서 거짓 → 포효를 그림).

## 5. SN-c 묶음 조회 (SN2)

순서는 Buffer 요청 → Core → Buffer 쓰기 거래 → View다. 모두 한 커밋이다.

**Buffer 요청(우리 영역):**

- **Tc.1 `B/CombatView/CombatViewPreviewKind.cs`:** 끝에 `ActionAvailabilities = 11`을 더한다(TP-10). 문서는 "자격 아군 모두의 행동 가능 여부(행위자 없음, 새 SSOT 설계 SN2)"다.
- **Tc.2 `B/CombatView/CombatViewPreview.cs`:** `public static CombatViewPreviewData ActionAvailabilities()`를 더한다. 메인 배치 요청(37-41)처럼 `EntityId.Empty`로 만들고, `RequireEntity`는 부르지 않는다.

**Core(사용자 허락):**

- **Tc.3 `CombatPreviewResult.cs`:**
  - 속성 `public IReadOnlyList<CombatActionAvailability> ActionAvailabilities { get; }`를 더한다.
  - 생성자 끝에 선택 인자 `actionAvailabilities`를 더하고, "내용은 하나" 개수 식(27-30)에 넣는다.
  - `ForActionAvailabilities(int number, IReadOnlyList<CombatActionAvailability> availabilities)`를 만든다. null 목록·null 항목·빈 개체·중복 개체를 `ArgumentException`으로 막고, 배열로 복사한다(TP-14). 빈 목록은 받는다.
- **Tc.4 `CombatActionAdmission.cs`:**
  - `internal static MovementFailureReason EligibilityFailure(CharacterCombatFacts actor, Owner owner)`를 만든다. 진영이 다르면 `WrongController`, 필드가 아니면 `NotInCombat`, 죽었으면 `Dead`, 아니면 `None`이다.
  - `Actor`의 36-39줄은 `var eligibility = EligibilityFailure(actor, automatic ? Owner.Enemy : Owner.Player); if (eligibility != MovementFailureReason.None) return eligibility;`로 바꾼다. `actor == null` 검사는 그 앞에 그대로 둔다(TP-11).
  - `internal static IReadOnlyList<CombatActionAvailability> GetAvailabilities(CombatCommandInput input)`를 만든다. `input.Characters` 중 `EligibilityFailure(c, Owner.Player) == None`인 유닛마다 `GetAvailability(input.WithActor(c.EntityId))`를 모은다(TP-12).
  - 요약 문서에 "행동 가능 여부를 물을 수 있는 유닛의 자격 원본"이라고 적는다.
- **Tc.5 `CombatCommandCalculator.cs`(36 근처):** `public static IReadOnlyList<CombatActionAvailability> GetActionAvailabilities(CombatCommandInput input) => CombatActionAdmission.GetAvailabilities(input);`를 더한다. 문서는 "자격 아군 모두의 일반 행동 가능 여부를 한 번에(새 SSOT 설계 SN2)"다.
- **Tc.6 `Core/CombatSystem/CombatPreviewService.cs`:**
  - 명령 종류 switch(68): `CombatViewPreviewKind.ActionAvailability or CombatViewPreviewKind.ActionAvailabilities => CombatCommandKind.Attack`.
  - 결과 switch(84 뒤): `ActionAvailabilities => CombatPreviewResult.ForActionAvailabilities(request.Number, WithRevision(CombatCommandCalculator.GetActionAvailabilities(input), revision))`. `WithRevision`은 이 파일 안 private 도우미로, 항목마다 `WithSourceRevision`을 붙인 배열을 만든다.
  - 판 검사(96): `request.Data.Kind is CombatViewPreviewKind.ActionAvailability or CombatViewPreviewKind.ActionAvailabilities && revision != _combat.QueryRevision`.
- **Tc.7 `CombatFirstUseRehearsal.cs:149`:** `_ = CombatCommandCalculator.GetActionAvailabilities(attack);` 한 줄을 더한다(TP-7). 주석 S4 줄에 "묶음"을 더한다.

**Buffer의 Core 쓰기 거래(천명님 줄, 사용자 허락):**

- **Tc.8 `B/Core/BattleTransactions.cs` `PublishPreview`(195-204):**
  - 단일 답 검증 식에서 "원본 판·전투·턴이 지금과 같은가"를 private 도우미 `IsCurrentAvailability(CombatActionAvailability a)`로 꺼낸다. 단일 검증은 그 도우미 + `ActorId == request.EntityId`다(예외·문구 그대로).
  - 그 아래에 묶음 검증을 더한다. `request.Kind == ActionAvailabilities || availabilities != null`이면 `request.Kind`가 맞고, 목록이 있고, 모든 항목이 `IsCurrentAvailability`이고 개체가 비지 않아야 한다. 아니면 `InvalidOperationException("행동 조회 묶음의 전투·턴·원본 버전이 현재 요청과 다릅니다.")`.

**View:**

- **Tc.9 `V/Input/CombatInputPolicy.cs` `StageOf`(323 근처):** `case CombatViewPreviewKind.ActionAvailabilities:`를 전투 단계 줄에 더한다(TP-6).
- **Tc.10 `V/Input/Interaction/CombatHudActionAvailability.cs` 다시 씀:**
  - 걷는 것: `_dirty`·`_observed`·`_next`·`_hasNonSingleCellUnit`·`ObserveCharacters`·`_requestedActor`·113줄 Core 식 사본·`IsEligibleActor` 호출.
  - 새 기억: `Dictionary<EntityId, CombatActionAvailability> _shown`(유닛마다 마지막으로 보인 답), `HashSet<EntityId> _seen`(답 적용 때 쓰는 재사용 목록), `long _answeredRevision`(그 판의 묶음 답을 받았는지).
  - `Sync(in facts, bool hasForeground, int answeredNumber, CombatPreviewResult latest, CombatActionAvailability selected)`(TP-15):
    - 닫힘·종료·재생 중·국면 아님·문지기 판정은 지금 그대로다.
    - 전투·턴이 바뀌면 `Clear()`.
    - 판이 바뀌면 `ClearRequest()`만 하고 아이콘은 둔다(Q-SN4 A).
    - 자기 요청이면 답을 소비한다: `latest.RequestNumber == _request`이고 `ActionAvailabilities`가 있고 항목이 지금 전투·턴·판이면, 항목마다 보이고, 목록에 없는 유닛은 숨기고(TP-16), `_answeredRevision = _revision`, `ClearRequest()`. 아니면 지금과 같은 "슬롯을 잃었으면 잊음" 갈래다.
    - `selected`가 있으면 보인다. 같은 객체면 건너뛴다(TP-17).
    - 전경 입력 중이거나, 이 판 답을 받았거나, 배경 요청 규칙이 막으면 돌아간다. 아니면 `ActionAvailabilities()` 요청 하나를 낸다.
  - `Show(id, answer)` 한 곳이 `_shown`을 맞추고 `_show`를 부른다. `Clear()`는 `_shown`의 유닛을 모두 숨긴다.
  - 계측: 요청은 `CombatInputProfiler.SetPreview`, 적용은 `CombatInputProfiler.Apply(CombatViewPreviewKind.ActionAvailabilities)`로 바꾼다(TP-4).
  - 클래스 요약은 "판이 바뀌면 Core에 자격 아군 모두의 답을 한 번 묻습니다. 무엇에 기대는지는 Core가 정합니다"로 고친다.
- **Tc.11 `V/Input/CombatSceneInput.cs:161-164`·`CombatSceneInputDriver.cs:150`:** `SyncHudActionAvailability`에서 `characters` 인자를 빼고 호출을 맞춘다(TP-15).
- **Tc.12 `CombatActionAvailabilityTracker.IsEligibleActor`(167-168):** 쓰는 곳이 선택 추적기 하나만 남는다. `internal` → `private`로 바꾸고, 문서에 "Core 원본은 `CombatActionAdmission.EligibilityFailure`, 선택 추적기가 묻기 전에 거르는 용도"라고 적는다.
- **Tc.13 Core 전달 문서:** §5.87(예상)에 바꾼 Core 파일 넷·쓰기 거래·리허설 한 줄·시험 영향 없음을 적고, §72(예상)에 천명님께 드리는 요약을 쓴다. 파일 제목을 바꾼다(2026-11-02 규칙).
- **확인:**
  - `CombatPreviewResult`를 만드는 곳에서 개수 식이 하나만 참인지 본다.
  - Core·View 안 `CombatViewPreviewKind` switch를 다시 찾는다. 새 값이 빠진 switch가 없는지 본다(T0.3과 같은 grep).

## 6. SN-d 문서·공유·검토·아티팩트

- **Td.1 묶음 끝 검토(새 에이전트 하나):** 바꾼 파일 전부를 기준 판 diff로 본다. 컴파일 오류 대조, 동작 같음(SN1·SN3), 묶음 조회의 수명·슬롯·검증, 7기준 점검이다. 지적은 높음·중간이면 반영하고, 낮음은 권장과 함께 보고한다.
- **Td.2 공유 메모(모두 미전달):**
  - 호진님: `IsUnitModelPart`가 `UnitModelParts.Is`로 옮겨짐, 표지 문서 문장. 배치 고스트 줄이 호진님 줄이면 함께.
  - 민형님: Cartoon 대체 생성자가 모델 부분 규칙을 부름(체력 숫자·커서 제외), Factory 무변경. 민형님 공유 문서의 다음 절이다.
  - 현강님: HUD 행동 가능 조회가 묶음 조회로 바뀜(아이콘 유지·자격·인자), 구조 공유 메모의 다음 절이다.
  - 천명님: Tc.13과 같다.
  - 배치 고스트 줄 작성자가 다른 사람이면 그 사람 메모에 넣는다(T0.2 결과).
- **Td.3 폴더 배치 안내:** `Display/Unit/UnitModelParts` 한 줄, §2 규칙 "유닛 모델 부분 판정은 `UnitModelParts.Is` 하나"·"행동 가능 HUD는 Core 묶음 답(View는 의존을 짐작하지 않음)"을 적는다.
- **Td.4 기록:** 설계 §10 구현 메모, 이 계획 §10 실행 기록, CLAUDE.md 상태 줄.
- **Td.5 평가 아티팩트 14차:** 재평가 판정 기준이다. C1-N01·C1-N02·C1-N03이 다 풀리면 C1 37.5 → 60, 요청 7기준 79.3 → 82.5, 12기준 83.1 → 85.0(추정)이다. 문제마다 바꾼 것·파일·남은 단계를 적는다.

## 7. Unity 확인 (모든 Phase 뒤 한 번, 사용자)

| 번호 | 확인 | 볼 것 |
|---|---|---|
| U-1 | 컴파일 | 오류·새 경고 없음 |
| U-2 | 적 진입·HUD | 적 모델·HUD가 전처럼 보임, 이미 Cartoon인 적의 외형이 같음 |
| U-3 | 적 사망 | 디졸브·랙돌이 전과 같음 |
| U-4 | 아군 선택 커서 | 커서가 보임. 커서가 붙은 아군의 랙돌 사망 때 커서 재질이 바뀌지 않음 |
| U-5 | 재구성 적 | 잔해 모습으로 바뀌고 돌아옴(상태 모습) |
| U-6 | 배치 고스트 | 고스트 재질·그림자 없음이 전과 같음 |
| U-7 | 몸 경계 | 목표 강조·접촉 자리가 전과 같음 |
| U-8 | 포효 예고 | 1 남은 보스의 바닥 범위·넉백 화살표가 보임. 공격 모드에서 포효 넉백만 숨고 범위는 남음. 호버를 옮겨도 포효 넉백이 깜빡이지 않음. 조준 넉백 칸 호버에서는 조준 넉백. 공격 모드를 끝내면 포효 넉백이 다시 보임 |
| U-9 | HUD 행동 가능 아이콘 | 턴 시작에 모든 아군 아이콘. 이동·공격 뒤 그 유닛 아이콘이 갱신되고 다른 아이콘이 깜빡이지 않음. 다음 턴에 갱신 |
| U-10 | 선택 유닛 | UnitInfo 가용성과 선택 유닛 HUD 아이콘이 맞음 |
| U-11 | 튜토리얼 | 이동 연습·공격 연습에서 HUD 아이콘이 나옴 |
| U-12 | 기존 시험 | Core 시험이 그대로 통과. 특히 행위자 검사(`CombatActionAdmission.Actor`)·단일 행동 가능 조회·게시 검증을 지나는 시험(SN-0 T0.3 목록) |
| U-13 | Profiler(TP-8) | `Core.Combat.Preview.ActionAvailabilities` 표지의 행동 한 번 비용, 첫 이동·공격 뒤 큰 끊김 없음 |

## 8. 커밋 묶음 (사용자가 커밋)

- SN-a: 새 `UnitModelParts.cs` + `.meta`, 바꾼 다섯 파일.
- SN-b: 넉백 미리보기·계약·공격 피드백·추적기 넷.
- SN-c: Buffer 둘·Core 다섯(결과·입장·계산기·조회 서비스·리허설)·쓰기 거래·View 넷(정책·HUD·연결 입력 묶음·입력 분배·선택 추적기). 나누면 컴파일이 깨진다.
- SN-d: 문서.

## 9. 공유 목록 (구현 뒤 확정)

| 사람 | 줄 | 문서 |
|---|---|---|
| 호진님 | `FieldUnitView` 판정·표지 문서 | 호진님 공유 메모 다음 절 |
| 민형님 | `CartoonModelMaterialFallback` 생성자 | 민형님 공유 문서 다음 절 |
| 현강님 | `CombatHudActionAvailability` | 현강님 구조 공유 메모 다음 절 |
| 천명님 | Core 넷·`BattleTransactions.PublishPreview`·조회 서비스 | Core 전달 문서 §5.87·§72(예상) |
| (공유 없음) | `PlacementGhost.cs:204` | SN-0에서 우리 줄로 확인(용근 `c5c8c2c9e`) |

## 10. 실행 기록

### 10.1 SN-0 착수 전 확인 (2026-11-11 사용자 "그대로, SN-0 진행해", 코드 변경 없음)

- 결정: TP-10~TP-20은 계획 그대로다.
- **T0.1 원격:** 원격 브랜치 = HEAD `ec0148266`. develop은 `424242ab8`로 HEAD의 바로 앞 커밋이라 받을 것이 없다.
- **T0.2 작성자 줄(`git blame`, 경계 커밋 줄은 GitHub 파일 이력):**

| 고칠 곳 | 작성자 | 공유 |
|---|---|---|
| `FieldUnitView.cs:381·386-393·508`, `UnitModelBoundsExclusion.cs:5-8` | 호진님 `62f8f2c9`(11-11) | 호진님 |
| `CartoonModelMaterialFallback.cs:20-29` | 민형님 `7eb7bdf3`·`89e43fce` | 민형님 |
| `UnitStatusAppearance.cs:103-111` | 우리 `0bff6b23` | 없음 |
| `PlacementGhost.cs:200-205` | 경계 커밋 `^83fb2767`(Leehojin 표시) → GitHub 이력으로 **우리 줄**(용근 `c5c8c2c9e`, 10-13 "이동 시 고스트 프리펩 연결"에서 생김) | 없음 |
| `CombatHudActionAvailability.cs` | 현강님(Twinsgm) `f6595326` 158줄, 계측 12줄 우리 `2f065c9f` | 현강님 |
| `CombatActionAvailabilityTracker.cs:167-168`(`IsEligibleActor`) | 현강님 `f6595326` | 현강님 |
| `BattleTransactions.cs:195-204` | 천명님 `df7b5d20` | 천명님 |
| `CombatPreviewService.cs:68·95-96` | 천명님 `d4a6d6fd`·`df7b5d20` | 천명님 |
| `CombatPreviewResult.cs:21-31` | 천명님(경계 커밋 `^4782921e`·`d4a6d6fd`) + 우리 `173a153a`(RF) | 천명님 |
| `CombatActionAdmission.cs:22-39·67-72`, `CombatCommandCalculator.cs:33-36` | 천명님 `d4a6d6fd`·`8ece0bbc`·`df7b5d20`·경계 커밋 | 천명님 |
| `CombatFirstUseRehearsal.cs:145-150` | 우리 `d39de2c1`(FU) | 천명님(Core 파일이라 Core 전달 문서에) |
| `CombatInputPolicy.cs:315-331` | 경계 커밋 `^4782921e` 15줄 + 우리 둘 | 줄을 더하기만 해 공유 없음 |
| `CombatSceneInput.cs:160-164`, `CombatSceneInputDriver.cs:145-151` | 현강님 `f6595326` + 우리 `173a153a`·`fedd5627` | 현강님 |
| `B/CombatView/CombatViewPreviewKind.cs`·`CombatViewPreview.cs:37-71` | 경계 커밋 `^4782921e` + 천명님 `d4a6d6fd` + 우리 | 줄을 더하기만 해 공유 없음(Core 전달 문서에는 적음) |
| 포효 예고 넷(`CombatKnockbackPreview`·`ICombatRoarForecastFeedback`·`CombatAttackFeedback:89-107`·`CombatRoarForecastTracker`) | 우리(RF) | 없음 |

- **T0.3 시험 참조(`Assets/Tests`):**
  - 0곳: `ICombatRoarForecastFeedback`·`CombatHudActionAvailability`·`CombatKnockbackPreview`·`CartoonModelMaterialFallback`·`UnitStatusAppearance`·`PlacementGhost`·`CombatRoarForecastTracker`·`IsEligibleActor`·`IsUnitModelPart`·`UnitModelBoundsExclusion`·`SyncHudActionAvailability`·`CombatViewPreviewKind`. 인터페이스 멤버를 더해도 깨지는 가짜 구현은 없다.
  - 바뀌는 Core 함수를 지나는 시험 11개 파일(고치지 않음, 그대로 통과해야 함): `CombatActionCostRetirementTests`·`MoveThenAttackTests`·`MovementPassiveUndoTests`·`MovementUndoPublicationTests`·`MovementUndoRulesTests`·`NewTraitMovementContextTests`·`PlayerTurnAttackTests`·`ReconstructionReviveTests`·`TraitRestrictionTests`·`TraitTrailTests`·`VoluntaryMovementAllowanceTests`. 단일 `GetActionAvailability`·`CombatActionAdmission.Actor`·`PublishPreview` 단일 검증을 부른다.
  - 그 밖에 이동·공격 행위자 검사를 지나는 시험이 36개 파일이다. `Actor`의 검사 순서·실패 이유를 그대로 두므로(TP-11) 결과가 같아야 한다. 계획 때 "다섯 자리"로 센 것은 직접 부르는 곳만 센 것이라 고친다.
- **T0.4 Core 전달 문서 번호:** 마지막은 §5.86·§71이다. 새 절은 §5.87·§72다.
- 계획과 다른 점: 없음. §7 U-12·§9 공유 표를 위 결과대로 고쳤다.

### 10.2 SN-a 모델 부분 규칙 (2026-11-11 사용자 "SN-a 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **Ta.1:** 새 `V/Display/Unit/UnitModelParts.cs` + `.meta`(Unity 기본 형식, 새 GUID). `internal static bool Is(Renderer)` 하나, null이면 `ArgumentNullException`. 식은 옛 `FieldUnitView.IsUnitModelPart` 그대로(TMP·Canvas·`UnitModelBoundsExclusion` 아래 아님). 요약 문서에 부르는 곳 다섯과 일부러 안 쓰는 곳 셋(진입 숨김 `CombatEntryVisibility`·준비 숨김 `CombatUnitPreparationLease`·랙돌 화면 밖 갱신 `CombatUnitRagdoll`)을 적었다.
- **Ta.2:** `FieldUnitView`의 `IsUnitModelPart`를 지우고 두 자리(외형 층 수집·몸 경계)를 `UnitModelParts.Is`로. `using TMPro`는 체력 숫자 때문에 남는다.
- **Ta.3:** `CartoonModelMaterialFallback` 생성자 두 반복문에 `UnitModelParts.Is` 조건. 종류 조건(Mesh·Skinned)은 그대로. remarks 한 줄.
- **Ta.4:** `UnitStatusAppearance.CaptureOriginals`가 `UnitModelParts.Is`를 부름, 주석을 규칙 이름으로, 쓰지 않게 된 `using TMPro` 삭제.
- **Ta.5:** `PlacementGhost:204` 조건을 `!UnitModelParts.Is(renderer)`로(TP-20). using은 이미 있음(`Display.Unit`). 같은 생성자의 Canvas·TMP 오브젝트 끄기(232-233)는 HUD를 숨기는 다른 일이라 그대로 둔다.
- **Ta.6:** `UnitModelBoundsExclusion` 문서를 "판정은 `UnitModelParts.Is` 한 곳"으로. 이름 그대로(TP-1).
- **확인:** `Assets/ProjectL`·`Assets/Tests`의 `.cs`에서 `GetComponentInParent<TMP_Text>`·`GetComponentInParent<TMPro.TMP_Text>`·`GetComponentInParent<UnitModelBoundsExclusion>`·`IsUnitModelPart`를 찾으면 `UnitModelParts.cs` 한 곳만 남는다. 유닛 뿌리를 훑는 다른 곳은 위 "일부러 안 쓰는 곳" 셋과 유닛이 아닌 것(토네이도·이펙트·필드 칸·칸 표지·커서 자기 렌더러)이다.
- 옛 이름 `IsUnitModelPart`는 지난 문서(호진님 튜토리얼 강조 계획·11-11 재평가 둘·이 설계·계획)에만 남는다. 기록이라 고치지 않는다.
- 바뀐 파일: 새 둘(`UnitModelParts.cs` + `.meta`), 고친 다섯(`FieldUnitView`·`CartoonModelMaterialFallback`·`UnitStatusAppearance`·`PlacementGhost`·`UnitModelBoundsExclusion`). 다른 작업자 줄: 호진님(`FieldUnitView` 판정 지움·표지 문서), 민형님(Cartoon 생성자) — 공유는 SN-d.
- 계획과 다른 점: 없음.

### 10.3 SN-b 넉백 출처 한 곳 (2026-11-12 사용자 "SN-b 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **Tb.1 `CombatKnockbackPreview`:** 필드 `_foreground`, `SetForegroundInput(bool)`(같으면 돌아감, 다르면 기억하고 조준 넉백이 없을 때만 `DrawRoar`), `DrawRoar`는 `_foreground || _roar.Count == 0`이면 지움. 클래스 remarks에 규칙 "조준 > 전경 입력 중이면 없음 > 포효"와 전경 사실을 받는 까닭. `ShowAim`·`ClearAim` 요약도 새 규칙에 맞게 고침(계획에 없던 문서 두 줄).
- **Tb.2 `ICombatRoarForecastFeedback`:** 멤버 `SetForegroundInput(bool)`, remarks "Core 응답과 전경 입력 사실을 넘김". 구현은 `CombatAttackFeedback` 하나, 시험 가짜 없음(SN-0 T0.3).
- **Tb.3 `CombatAttackFeedback`:** 포효 예고 region 안에 명시 구현 하나(해제 뒤면 무시, 아니면 넉백 미리보기에 넘김).
- **Tb.4 `CombatRoarForecastTracker`:** `_hideKnockback`·뒤집기 블록·`Redraw`의 빈 목록 갈래 삭제, 그 자리(`CanKeep`·판 바뀜 뒤, 응답 소비 앞)에서 매 Sync `SetForegroundInput(hasForeground)`, `Redraw`는 `_attacks`를 그대로 넘김, 클래스 remarks·`hasForeground` 문서 고침.
- **확인(코드 대조):**
  - 조준 넉백 있음: `ShowAim`이 조준을 그림. 전경 바뀜·포효 답은 기억만(`_aimShown`).
  - 조준 넉백 없음 + 전경 입력 중: `DrawRoar`가 지움. 옛 코드는 추적기가 빈 목록을 넘겨 지웠음. 전경 중에 받은 답도 같은 Sync에서 전경 사실을 먼저 넘긴 뒤 소비하므로 지움.
  - 그 밖: 포효를 그림.
  - 한 프레임 순서(입력 분배 → 입력 Tick 응답 → Tick 끝 포효 Sync): 공격 모드가 시작하는 프레임은 입력 단계에서 미리보기의 전경이 아직 거짓이라 옛 코드와 같이 포효를 그렸다가 Tick 끝에서 지움(옛 코드도 그 프레임에는 숨김 깃발이 아직 거짓). 공격 모드가 끝나는 프레임은 `ClearAim` 때 전경이 아직 참이라 지우고, Tick 끝에서 거짓으로 바뀌어 포효를 그림. 둘 다 옛 동작과 같음.
  - 차이 둘(화면 같음): 전경이 바뀔 때 바닥 범위 층을 다시 넘기지 않음(같은 칸이라 안 바뀜). 그린 것이 없을 때 전경이 바뀌면 캔버스 비우기를 한 번 부름(이미 비어 있음, 전경 시작·끝에만).
- 시험 참조: `Assets/Tests`에 이 네 형식·필드를 부르는 곳 0.
- 바뀐 파일 넷(넉백 미리보기·계약·공격 피드백·추적기), 모두 우리 줄(RF) — 공유 없음.
- 계획과 다른 점: 위 문서 두 줄 외 없음.

### 10.4 SN-c 행동 가능 여부 묶음 조회 (2026-11-12 사용자 "SN-c 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **Tc.1·Tc.2 Buffer 요청:** `CombatViewPreviewKind.ActionAvailabilities = 11`, `CombatViewPreviewData.ActionAvailabilities()`(행위자 `EntityId.Empty`, `RequireEntity` 안 부름).
- **Tc.3 `CombatPreviewResult`:** 칸 `ActionAvailabilities`, 생성자 끝 선택 인자, 개수 식 한 항, `ForActionAvailabilities` — null 목록은 `ArgumentNullException`, null 항목·빈 개체·중복 개체는 `ArgumentException`, 배열로 복사, 빈 목록 받음(TP-14). `using …Contracts.Ids`·`System.Collections.Generic` 추가.
- **Tc.4 `CombatActionAdmission`:** `EligibilityFailure(actor, owner)`(진영 → 필드 → 생존, TP-11), `Actor`의 세 줄을 이것으로(`actor == null` 검사는 앞에 그대로), `GetAvailabilities(input)`(입력 유닛 순 `WithActor`, TP-12), 클래스 요약에 자격 원본 한 줄.
- **Tc.5 `CombatCommandCalculator.GetActionAvailabilities`**, **Tc.6 `CombatPreviewService`:** 명령 종류 switch·결과 갈래·판 검사에 새 종류, private `WithRevision`(항목마다 `WithSourceRevision`). 판 검사는 `is … or …`로 묶음(우선순위는 옛 `==`·`&&`와 같음).
- **Tc.7 리허설:** S4에 `GetActionAvailabilities` 한 줄, 주석 "단일·묶음"(TP-7).
- **Tc.8 `BattleTransactions.PublishPreview`(천명님 줄):** private `IsCurrentAvailability`(원본 판 ≠ 0·지금 판·전투·턴), 단일 검증 = 도우미 + 개체 일치(예외·문구 그대로), 묶음 검증 = 종류 맞음 ∧ 목록 있음 ∧ 모든 항목이 null 아님·개체 있음·도우미 참(TP-13).
- **Tc.9 `StageOf`:** 전투 단계 줄에 새 종류(TP-6). 튜토리얼 허용 표·`CanPreview`에는 넘기지 않음(문지기는 `CanQueryActionAvailability` 그대로).
- **Tc.10 `CombatHudActionAvailability` 다시 씀(현강님 `f6595326` 파일):**
  - 걷음: `_dirty`·`_observed`·`_next`·`_hasNonSingleCellUnit`·`ObserveCharacters`·`_requestedActor`·113줄 Core 식 사본·`IsEligibleActor` 호출·`facts.SelectedActorId` 건너뛰기.
  - 새 기억: `_shown`(유닛마다 마지막으로 보인 답), `_seen`·`_gone`(답 적용 때 재사용 목록 — `_gone`은 계획에 없던 것, `_shown`을 돌며 지울 수 없어 숨길 유닛을 모음), `_answeredRevision`.
  - 판이 바뀌면 `ClearRequest`만(아이콘 유지, Q-SN4 A). 자기 답이면 항목마다 `Show`, 목록에 없는 유닛은 숨김(TP-16). 선택 답은 같은 객체면 건너뜀(TP-17). 요청은 `ShouldRequestBackground(_answeredRevision == _revision, …)`(전경 입력 중 안 물음).
  - 계측: 요청 `SetPreview`, 적용 `Apply(ActionAvailabilities)`(TP-4).
- **Tc.11:** `SyncHudActionAvailability`에서 `characters` 인자 삭제, 입력 분배 호출 맞춤(TP-15). 입력 분배 Tick 끝 주석 "유닛 수만큼 묻는 HUD 조회"를 "HUD 묶음 조회(판마다 한 번)"로(계획에 없던 주석 한 줄, 우리 RF 줄).
- **Tc.12:** `CombatActionAvailabilityTracker.IsEligibleActor` `internal` → `private`, 주석 "Core 원본은 `EligibilityFailure`, 선택 추적기가 묻기 전에 거르는 용도".
- **Tc.13:** Core 전달 문서 §5.87·§72, 제목·머리 요약 갱신.
- **확인:**
  - `CombatPreviewResult`를 만드는 곳은 모두 `For…` 정적 입구 하나씩이라 개수 식이 하나만 참(새 입구는 `actionAvailabilities:`만 넘김).
  - `CombatViewPreviewKind` switch: Core 조회 서비스 둘(명령 종류·결과)·View `StageOf` 하나에 모두 새 값이 있음. 계측 표지(Core `CoreProfilerMarkers`·View `CombatInputProfiler`)는 열거에서 만듦.
  - 시험 참조: 바뀐 서명(`SyncHudActionAvailability`·`CombatHudActionAvailability.Sync`·`IsEligibleActor`)을 부르는 시험 0. `ForActionAvailability`(단일)를 부르는 천명님 `MovementUndoPublicationTests:90·107`은 그대로 컴파일됨(게시 검증 결과 같음 — 원본 판 0 = "unbound" 사례도 예외 그대로).
- **동작 차이(설계 §5 그대로):** 판이 바뀐 뒤 답이 올 때까지 옛 아이콘이 남음(전경 입력 중 내내). 행동마다 조립 1 + 자격 아군 수만큼 계산(TP-8 측정 대상). 선택 유닛은 묶음·선택 추적기 양쪽에서 계산(TP-9). 턴이 바뀌면 지금처럼 모두 비우고 다시 물음.
- 바뀐 파일 열둘: Buffer 요청 둘, Core 다섯(결과·입장 규칙·계산기·리허설·조회 서비스), Buffer 쓰기 거래 하나(천명님 줄), View 넷(정책·HUD(현강님 파일)·연결 입력 묶음·입력 분배)+ 선택 추적기 하나. Factory 무변경.
- 계획과 다른 점: `_gone` 재사용 목록, 입력 분배 주석 한 줄.

### 10.5 SN-d 문서·공유·검토·아티팩트 (2026-11-12 사용자 "SN-d 진행해", 코드 변경 없음)

- **Td.1 묶음 끝 검토(새 에이전트 하나, 읽기 전용, 기준 `ec0148266`):** 높음 0 · 중간 0 · 낮음 2 · 개선 제안 7, 7기준 약 8.7(C1 8.5·C2 8.5·C3 9.5·C4 9.0·C5 8.0·C6 8.5·C7 9.0). 정독 컴파일 오류 없음, SN1은 설계 §5 차이 말고 같음, SN3은 프레임마다 대조해 같음, SN2 수명·슬롯·검증 맞음(단일 게시 검증은 옛 다섯 조건과 진리값 같음, 튜토리얼 동작 같음).
  - 낮음(결함, 사용자 결정 대기 — 계획 규칙대로 고치지 않고 보고):
    - **L-D1** HUD `Show`의 적용 계측 표지 `Apply(ActionAvailabilities)`가 선택 추적기의 단일 답 쓰기에도 붙어 TP-8 측정에 선택 유닛 1~2회가 섞임. 권장: 표지를 `ShowAnswers` 바깥 한 번으로.
    - **L-D2** `BattleTransactions.PublishPreview` 묶음 검증 예외 문구가 종류 불일치·null 항목·빈 개체도 "전투·턴·원본 버전이 다릅니다"로 말함. 권장: 문구를 넓힘.
  - 개선 제안: I1 HUD `IsCurrent`는 지금 불변식에서 늘 참(방어 주석 권장), I2 Buffer 묶음 검증의 null·빈 개체 검사는 결과 형식 검사의 사본(TP-13·TP-14 나눔대로 줄이기 권장), I3 Core 묶음 계산 할당 약 3N+4번·인터페이스 `foreach` 박싱(판 번호를 계산에 넘기고 for로 — TP-8 측정 뒤), I4 문지기 `CanQueryActionAvailability`가 단일 종류로 튜토리얼 허용 표를 봄(문서에 "단일·묶음 공통" 또는 허용 표에 새 종류), I5 `CombatPreviewSlotRule` 문서의 배경 요청 목록이 낡음(이번 전부터), I6·I7 받아들임(View 자격 사본, 선택 유닛 두 번 계산).
  - 권장 반영 순서(검토): L-D1 → I3(TP-8 결과 보고) → L-D2·I2·I4.
- **Td.2 공유(모두 미전달):** 호진님 구조 공유 §21, 민형님 Factory 공유 §57, 현강님 구조 공유 §29, 천명님 Core 전달 문서 §72(SN-c 때). 배치 고스트 줄은 우리 줄이라 공유 없음.
- **Td.3 폴더 배치 안내:** 갱신 줄, `Display/Unit/` 줄에 `UnitModelParts`, §2 규칙 줄 둘(모델 부분 판정·HUD 행동 가능).
- **Td.4:** 설계 §10 구현 메모(검토 요약 포함), 이 절, CLAUDE.md.
- **Td.5 평가 아티팩트 14차(버전 16):** 재평가 판정 기준으로 C1-N01·N02·N03 다 풀림 → C1 37.5 → 60.0, 요청 7기준 79.3 → 82.5, 12기준 83.1 → 85.0, 중간 11 → 8(추정). 14차 카드·`applied-data` 14번째 차수(`basis` 재평가)·`reevaluations[0].applied_after`, 재평가 절 남은 중간 목록에 "14차 풀림" 칩·점수 표 아래 한 줄, 머리 요약 둘.
- 계획과 다른 점: 없음.

### 10.6 검토 L-D1 반영 (2026-11-12 사용자 "L-D1만 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- `CombatHudActionAvailability`: 적용 계측 표지 `CombatInputProfiler.Apply(ActionAvailabilities)`를 `Show`(모든 그리기)에서 `ShowAnswers` 맨 앞(묶음 답 하나에 한 번)으로 옮겼다. 선택 추적기의 답을 HUD에 그릴 때는 표지를 붙이지 않는다(그 답의 적용은 선택 추적기 `Consume`가 `Apply(ActionAvailability)`로 잰다). U-13에서 묶음 표지의 호출 수 = 묶음 답 수다.
- 그대로 둔 것: L-D2(Buffer 묶음 검증 예외 문구), 개선 제안 I1~I7.

### 10.7 커밋·push (2026-11-12 사용자 "지금까지 한거 커밋하고 푸쉬해")

- §8 묶음대로 네 커밋: SN-a `5a4791e6d`(모델 부분 규칙), SN-b `f89eca35b`(넉백 출처), SN-c `40b17a6e0`(묶음 조회, Buffer·Core·쓰기 거래·View 한 커밋, L-D1 포함), SN-d(문서 — 이 기록이 든 커밋).
- 원격 develop은 `360ab06e9`로 앞섰지만 병합은 지시 밖이라 하지 않았다. 브랜치만 push.
- 컴파일 미확인 그대로, Unity 확인 U-1~U-13 대기.
