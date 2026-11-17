# 전투 피격 구조 정리 Task 계획

- 날짜: 2026-11-17
- 설계: [hit-structure-cleanup-design](../specs/2026-11-17-combat-hit-structure-cleanup-design.md) HC1~HC4
- 결정: Q-HC1~Q-HC3 모두 A(2026-11-17 사용자 "권장안대로"), P2 "Instance 하나 공유"
- 상태: **계획 작성, 코드 착수 전.** 계획에서 정한 TP-1~TP-14는 사용자 확인 대상이다. §5에 설계·계획 7기준 검사와 반영을 적었다.
- 작업 규칙(CLAUDE.md): View 작업이라 새 시험 없음·MSBuild 없음(컴파일 미확인으로 보고), Phase마다 멈추고 보고, 커밋·push는 사용자, 검토는 묶음 끝에 한 번.

## 0. 착수 조건

1. 같은 파일을 고친 11-17 작업 둘(코드 피격 움직임 켜고 끄기·연발 피격 끊고 다시)은 지금 **스테이지에만** 있다. 그 작업은 클립 칸 다시 짜기(CS-a~CS-d, 커밋 전) 위에 있어 따로 커밋하면 컴파일되지 않는다.
2. 그래서 착수 전에 CS 작업과 11-17 두 작업이 커밋되어 있어야 한다(권장). 커밋 전에 착수하면 이 계획의 변경이 세 작업과 같은 파일에 섞여 커밋을 나누기 어렵다.
3. 고칠 파일: `CombatCharacterEventHandler.cs`, `CombatSceneEventPresenter.cs`, `CombatPieceRunPlayer.cs`, `CombatPresentationPlayers.cs`, `CombatStartQuery.cs`, `CombatComposition.cs`, `CombatHitMotionPiece.cs`(이름 바꿈), `CombatKnockbackLeadIns.cs`, `CombatCharacterHits.cs`, `CombatVolleyContactHits.cs`(입구 이름만, TP-11). 걷는 파일: `CombatUnitReactionSet.cs`, `CombatReactionPlayer.cs`, `CombatReactionPiece.cs`(+ `.meta`). 새 파일 넷(+ `.meta`, 아래).

## 1. Phase·Task

### HC-0 착수 전 확인 (코드 변경 없음)

- T0.1 HEAD에 CS 작업·11-17 두 작업이 있는지 확인한다. 없으면 멈추고 보고한다.
- T0.2 바꿀 줄의 작성자를 blame으로 센다. 대상: 사건 Presenter의 반응 부품·반응 재생기·처리기 생성 줄(`CombatSceneEventPresenter.cs` 49·162·171·197 근처), 실행 목록 `Create`, `CombatStartBlock.Reaction`, 조각 실행 재생기 `FindBlock`·통과 갈래, 조립 표지 둘, 캐릭터 처리기의 피격·사망 길. 다른 작업자 줄이면 HC-d 공유 목록에 넣는다.
- T0.3 `Assets/Tests`에서 바뀌는 형식·멤버 참조 0을 다시 확인한다(설계 때 0).

### HC-a 판정식·통과 규칙 (HC4·HC3, 화면 변화 없음)

- Ta.1 `CombatCharacterHits.IsPushLeadInHit(e)` = `e.Kind != Reconstructed && IsHitLike(e)`. `CombatKnockbackLeadIns`가 부르고 `IsHit`는 걷는다. "처리기와 같은 판정" 문서 줄을 고친다.
- Ta.2 새 계약 `Presentation/Pieces/ICombatRunSubjectPass.cs`(TP-2):
  - `bool Passes(CombatEvent item)` — 이 실행이 그 주체의 이 사건을 막지 않는지
  - `bool IsCutBy(CombatEvent item)` — 이 사건이 시작하며 이 실행을 끊는지
  - `bool Cuttable` — 끊길 수 있는 규칙인지(끊기면 완료를 못 알리므로 재생기가 완료 통지 있는 시작을 거절)
  - `bool ForbidsPose` — 자세 채널을 빌리면 안 되는지
- Ta.3 `CombatComposition`: 표지 `AllowsSubjectPush`·`YieldsToNextHit`와 입구 `AllowSubjectPush()`·`YieldToNextHit()`를 걷고 `SubjectPass`(읽기) + `PassSubject(ICombatRunSubjectPass)` 하나로. 조건: 사망이 아닌 유닛 실행만(TP-3), 이미 자세를 빌렸고 규칙이 `ForbidsPose`면 예외, 한 번만. `Lease`의 자세 검사는 `SubjectPass?.ForbidsPose`를 본다.
- Ta.4 새 `Playback/Support/CombatPushAlongsidePass.cs`·`CombatNextHitPass.cs`(TP-4): 필드 없는 `sealed` 클래스, private 생성자, `internal static readonly Instance`.
  - 넉백과 함께: `Passes` = 지금 `PassesPushAlongsideHit` 본문(행동 시작·출처 여는 사건 아님, 비캐릭터 사건 또는 밀림 걸음), `IsCutBy` = 거짓, `Cuttable` = 거짓, `ForbidsPose` = 참.
  - 다음 피격: `Passes` = `IsCutBy` = `item is CombatCharacterEvent c && CombatCharacterHits.RestartsHit(c)`, `Cuttable` = 참, `ForbidsPose` = 거짓.
- Ta.5 `CombatPieceRunPlayer`:
  - `FindBlock`의 두 갈래를 하나로: 사건 주체(`CombatEventTypeTable.TryGetSubject`)의 유닛 실행이 있고 그 규칙이 `Passes(item)`면 통과.
  - `TryBeginCore` 검사: `SubjectPass?.Cuttable == true && completed != null`이면 예외.
  - `CutYieldingRun(EntityId)` → `CutRunFor(CombatEvent item)`: 사건 주체의 실행 규칙이 `IsCutBy(item)`면 취소.
  - 11-17에 더한 `Playback.Support` using을 걷는다(재생기는 사건 지식을 모름).
- Ta.6 캐릭터 처리기: 넉백과 함께 조립은 `.PassSubject(CombatPushAlongsidePass.Instance)`, 접촉 피격 조립은 `.PassSubject(CombatNextHitPass.Instance)`, 끊기는 `_pieces.CutRunFor(e)`.
- Ha 확인: 컴파일 미확인으로 보고(MSBuild 없음). 화면 변화 없음.

### HC-b 반응 움직임을 조각 하나로 (HC1)

나눠서 진행한다. 사망이 문제면 HC-b2만 되돌릴 수 있게 한다(§5 R3).

- **HC-b1 피격**
  - Tb.1 `CombatHitMotionPiece.cs` → `CombatReactionMotionPiece.cs`(`git mv`, GUID 유지, MonoBehaviour 아님이라 `MovedFrom` 없음). 생성자 private, 만들기 입구 `Hit(unit, settings, damage, mapper)`·`Death(unit, settings, death, mapper, launchRagdoll, releaseModel)`(TP-6). 표시 사라짐 콜백은 지금처럼 끝나기만(실행이 사라짐을 알림).
  - Tb.2 `ComposeHit`의 `Action<Action> beginReaction` 입구를 걷고 조각 입구 하나만(TP-7).
  - Tb.3 보통 피격: `BeginPose`·반응 부품 시작·`_cancelUnavailableReaction`·반응 취소 정리를 걷고, 조립에 `CombatReactionMotionPiece.Hit` + `Lease(id, Pose)`. 사실 반영은 조립 시작 **전**(지금과 같은 자리), 자세는 조립 시작 때 빌린다(설계 §4).
  - Tb.4 연발 앞 탄 접촉 피격: `_reaction.IsPlayingFor(id)` 검사를 걷는다(`HasUnitRun`이 이미 덮음, §5 R5).
- **HC-b2 사망**
  - Tb.5 보통 사망: 조립에 `CombatReactionMotionPiece.Death(…, launchRagdoll, hold.Release)` + `Lease(id, Pose)`. 순서는 지금과 같다 — 사망 유지 → 조립 시작(자세 대여·반응 시작) → 사실 반영. 시작 실패는 지금 문구로 예외이고 사망 유지를 놓는다. 시작한 뒤의 예외는 사건 Presenter의 오류 정리가 실행을 취소하고, 조각 취소가 반응·자세·사망 유지를 함께 정리한다(반응 부품 `Cancel` → `ClosePose`가 `releaseModel`을 부름 — 사망 유지 놓기는 지금처럼 여러 번 불려도 됨).
  - Tb.6 `ComposeDeath`도 조각 입구 하나로(TP-7).
- **HC-b3 걷기**
  - Tb.7 걷는 파일: `CombatUnitReactionSet.cs`, `CombatReactionPlayer.cs`, `CombatReactionPiece.cs`(+ `.meta`, TP-8). 사건 Presenter의 `_reaction`·`reactionPlayer`, 실행 목록 `Create`의 `reaction` 인자, `CombatStartBlock.Reaction`.
  - Tb.8 캐릭터 처리기 생성자에서 `reaction`·`cancelUnavailableReaction` 인자를 걷는다.
- Hb 확인: 컴파일 미확인. Unity 확인 U-1~U-10(§2)은 HC-d 뒤 한 번.

### HC-c 피격 시작을 처리기에서 떼어 냄 (HC2)

- Tc.1 새 `Playback/Support/CombatHitStart.cs`(TP-10). 인자: 유닛 표시 창구, 보인 사실, 조각 실행 재생기, 재생 세대, Mapper 읽기, 카메라·이펙트 창구, 사건 소리, 넉백 앞 피격 목록.
  - `TryBegin(damage, original, completed, version)`: 넉백과 함께 → 그 길, 아니면 보통 길 하나(뼈대 고르기 `SkeletonOf` → 반응 조각 + 자세 대여 / 기다림). "사실을 반응 뒤에 반영" 감싸기는 함수 하나(`AfterReaction`).
  - `TryBeginContact(damage, version)`: 같은 `SkeletonOf`.
  - `CutPrevious(CombatEvent e)`: `_pieces.CutRunFor(e)` 한 줄. 처리기(Core 사건 시작)와 다음 탄 접촉 피격이 이것을 부른다(§5 R1).
  - 정적 `Compose(...)`(옛 `ComposeHit`), 정적 `SubjectBodyOf(facts, mapper, id, e, facing)`(사망도 부름, TP-12).
- Tc.2 캐릭터 처리기: 피격 길 넷·`ComposeHit`·`SubjectBodyOf` 본문을 옮기고, `TryBeginBeforeFacts`는 `_hits.CutPrevious` → 토네이도 → 사망 → `_hits.TryBegin`. 생성자 인자 `knockbackLeadIns`를 걷고 `hits`를 받는다(TP-9).
- Tc.3 사건 Presenter: `CombatHitStart`를 만들어 처리기와 연발 앞 탄 피격(`CombatVolleyContactHits`)에 넘긴다(TP-11).
- Hc 확인: 컴파일 미확인. 예상 처리기 590 → 약 380줄, 피격 시작 약 230줄.

### HC-d 문서·공유·묶음 끝 검토

- Td.1 폴더 배치 안내: `Presentation/Pieces/` 줄(`CombatReactionMotionPiece`·`ICombatRunSubjectPass`, `CombatHitMotionPiece` 이름 바꿈), `Presentation/Reaction/` 줄(반응 부품 걷음 — `CombatUnitReactionPresenter`만), `Playback/Players/` 줄(반응 재생기 걷음), `Playback/Support/` 줄(`CombatHitStart`·통과 규칙 둘), §2 규칙 줄 둘 — "유닛 실행이 그 주체의 특정 사건을 막지 않거나 끊기면 조립에 통과 규칙 하나를 준다(재생기에 갈래를 더하지 않음)", "피격·사망 코드 움직임은 조립 안의 반응 조각 하나가 진행한다".
- Td.2 설계 §10 구현 메모(계획과 다른 점).
- Td.3 공유 메모: T0.2에서 센 작성자에게(미전달). Core 요청 없음.
- Td.4 묶음 끝 검토(새 에이전트 하나, 메모리 "검토는 마지막에 한 번").
- Td.5 CLAUDE.md 현재 상태 줄.

## 2. Unity 확인 목록 (모든 Phase 뒤 한 번, 사용자)

- U-1 보통 피격(피격 클립 없는 유닛): 밀렸다 돌아오며 젖힘·플래시·이펙트·흔들림이 지금과 같다.
- U-2 보통 피격(피격 클립 있는 유닛, 움직임 칸 꺼짐): 클립만, 플래시 그대로.
- U-3 같은 유닛, 반응 SO `Hit Motion With Clip` 켬: 클립 + 코드 움직임.
- U-4 아킴보 2연발: 둘째 탄에서 끊고 다시(11-17과 같음).
- U-5 넉백 피격: 맞는 순간 함께 밀림·젖힘(클립 있는 유닛은 젖히지 않음).
- U-6 재구성 치명타: 피격 뒤 잔해 모습·체력이 반응 끝에 바뀜.
- U-7 보통 사망: 쓰러짐·디졸브, 공격 처치면 랙돌.
- U-8 기차 동시 사망: 여럿이 함께 죽음.
- U-9 대신 맞기(수호자가 서는 자리에서 피격): 그 자리에서 피격 반응, 끝나고 돌아옴(§5 R13).
- U-10 낭떠러지 추락·자폭 깜빡임 사망: 지금과 같음(길은 안 바꿨지만 같은 처리기).

## 3. 계획에서 정한 것 (사용자 확인 대상)

- TP-1 순서는 HC-a → HC-b1 → HC-b2 → HC-b3 → HC-c → HC-d. 화면 변화 없는 정리를 먼저, 사망은 따로 되돌릴 수 있게 나눈다.
- TP-2 통과 규칙 계약은 멤버 넷(`Passes`·`IsCutBy`·`Cuttable`·`ForbidsPose`).
- TP-3 통과 규칙은 사망이 아닌 유닛 실행에만 준다(지금 두 쓰임과 같음).
- TP-4 통과 규칙 구현 둘은 `Playback/Support/`(사건 지식이 필요), 계약은 `Presentation/Pieces/`(조립이 들고 있음).
- TP-5 `CombatKnockbackLeadIns.IsHit`는 걷고 `CombatCharacterHits.IsPushLeadInHit`로.
- TP-6 반응 조각은 이름을 바꿔 넓히고 만들기 입구 둘(`Hit`·`Death`), 생성자 private.
- TP-7 `ComposeHit`·`ComposeDeath`는 조각을 받는 입구 하나씩.
- TP-8 걷는 파일 셋은 `.meta`와 함께 지운다(코드 파일, 에셋 아님). `CombatStartBlock.Reaction` 값도 걷는다.
- TP-9 캐릭터 처리기 생성자: `reaction`·`cancelUnavailableReaction`·`knockbackLeadIns`를 걷고 `hits` 하나를 받는다. `isAttackHit`(사망 랙돌)·`selfDestruct` 등은 그대로.
- TP-10 `CombatHitStart`는 `Playback/Support/`(공격의 `CombatAttackStart`와 같은 자리).
- TP-11 연발 앞 탄 피격 입구는 처리기 메서드가 아니라 `CombatHitStart.TryBeginContact`를 받는다(`CombatVolleyContactHits` 생성 인자 하나).
- TP-12 `SubjectBodyOf`는 `CombatHitStart`의 정적 함수이고 사망도 부른다(§5 R7).
- TP-13 새 시험 없음, 기존 시험 변경 없음(참조 0), Factory·Core 무변경.
- TP-14 검토 에이전트는 HC-d에서 한 번.

## 4. 커밋 묶음 (사용자)

- 묶음 1 HC-a: 새 `.cs` 셋(+ `.meta`)과 바뀐 파일.
- 묶음 2 HC-b: 이름 바꿈 하나, 지운 파일 셋(+ `.meta`), 바뀐 파일. b1~b3를 한 커밋으로 해도 된다(따로 하면 b1만으로도 컴파일됨 — 반응 부품은 사망이 아직 씀).
- 묶음 3 HC-c: 새 `CombatHitStart.cs`(+ `.meta`)와 바뀐 파일.
- 묶음 4 HC-d: 문서.

## 5. 설계·계획 7기준 검사 (2026-11-17, 사용자 요청)

설계 초안과 이 계획 1판을 같이 봤다. 읽고 판단한 정적 검사다.

### 5.1 찾은 것과 반영

| 번호 | 기준 | 심각도 | 내용 | 반영 |
|---|---|---|---|---|
| R1 | SSOT | 낮음 | 끊기 호출이 처리기(Core 사건)와 접촉 피격(다음 탄) 두 곳이다. 규칙은 하나지만 호출 자리가 둘 | `CombatHitStart.CutPrevious` 하나를 둘 다 부름(Tc.1) |
| R2 | SOLID(ISP) | 낮음 | 통과 규칙 계약 멤버 넷 중 `Cuttable`은 "끊길 수 있음"을 따로 들고 있다. `IsCutBy`만으로는 시작 때 미리 알 수 없어서 필요 | 그대로(TP-2) |
| R3 | 독립성 | 중간 | 1판은 피격·사망을 한 Phase로 옮겨, 사망 쪽(랙돌·디졸브·사망 유지·기차 동시 사망)에서 문제가 나면 피격까지 함께 되돌려야 했다 | HC-b를 b1 피격 / b2 사망 / b3 걷기로 나눔(TP-1). b1만으로 컴파일됨 |
| R4 | C#/Unity | 낮음 | 반응 조각이 피격마다 반응 부품을 새로 만든다 | 지금 반응 부품 목록도 항목마다 새로 만들어(`Entry.Presenter = new()`) 할당 수는 같음. 그대로 |
| R5 | If/Tick | 낮음 | 연발 앞 탄의 `_reaction.IsPlayingFor(id)` 검사는 `HasUnitRun`과 겹친다(피격·사망 반응은 늘 유닛 실행 안) | 걷음(Tb.4) |
| R6 | SOLID | 낮음 | `CombatHitStart` 생성 인자 9개 | 처리기에서 옮기는 것이라 늘지 않음, 처리기 인자는 16 → 14. 그대로 |
| R7 | SSOT·캡슐화 | 낮음 | 사망이 `CombatHitStart.SubjectBodyOf`를 부르면 사망이 피격 시작 파일에 기댄다 | 자리 대안(`CombatHitDirection`)은 Presentation → Playback 의존이 생겨 안 됨. 정적 함수로 두고 문서에 이유(TP-12) |
| R8 | FW 계약 | — | Framework·서비스 키·Factory를 건드리지 않음 | 문제 없음 |
| R9 | 캡슐화 | 낮음 | 조립의 `SubjectPass` 읽기는 재생기만 쓴다 | `internal` 읽기만, 쓰기는 `PassSubject` 한 번 |
| R10 | 독립성 | 중간 | 같은 파일에 커밋 안 된 작업 셋(CS·11-17 둘)이 섞여 있다 | 착수 조건으로(§0) |
| R11 | If/Tick | 개선 | 재생기 `FindBlock`이 지금은 갈래 둘에서 주체·실행을 두 번 찾는다 | 하나로 합쳐 한 번(Ta.5) |
| R12 | 정확성 | 확인 | 반응 재생기의 "이 프레임에 시작한 반응은 시간 안 받음"(B14 O12)이 사라진다 | 사건은 재생기 Tick 뒤(순차 재생 루프)·걸음 Tick 안·레일 Tick(재생기 뒤)에서만 시작해 조각 실행 Tick 스냅숏에 들지 않음을 코드로 확인. 첫 시간 같음 |
| R13 | 정확성 | 확인 | 보통 피격의 자세 대여가 사실 그리기 뒤로 옮겨진다. 서는 자리(대신 맞기)·표시 회전(젖힌 채)이 있는 유닛도 같은 자세를 잡는지 | 서는 자리·표시 회전은 자세 대여와 다른 칸(`UnitPresentationLeaseSlot` 채널별)이라 같은 자세를 잡을 것으로 보이나, 그리기가 그 둘을 어떻게 다루는지는 끝까지 따라가지 않았다. HC-b1 때 `FieldUnitView` 그리기를 대조하고 Unity 확인 U-9에 넣음 |

### 5.2 점수 (알려진 문제 기준)

| 기준 | 지금 코드 | 설계 초안 그대로 | 반영 뒤 |
|---|---|---|---|
| SSOT | 8.0 | 9.0 | 9.25 |
| SOLID | 7.0 | 8.5 | 8.75 |
| FW 계약 | 9.5 | 9.5 | 9.5 |
| 변경 독립성 | 7.5 | 8.5 | 9.0 |
| 캡슐화 | 8.0 | 8.75 | 8.75 |
| C#/Unity 권장 | 8.5 | 8.5 | 8.5 |
| 무의미한 If/Tick | 8.5 | 8.75 | 9.0 |
| 평균 | 약 8.1 | 약 8.8 | 약 8.96 |

남는 것: 유닛 표시 창구 멤버가 많음(지난 평가 V4), 피격마다 람다·조각 할당(작음), 통과 규칙 계약 멤버 넷(R2).

## 6. 실행 기록

### 6.1 HC-0 (2026-11-17 사용자 "TP 그대로, HC-0 진행해", 코드 변경 없음)

- **T0.1 착수 조건 — 반만 풀림, HC-a 착수 멈춤.**
  - 11-17 두 작업은 사용자 커밋 `7c41f54ab`("피격 연출 조정", 스테이지했던 9개 파일 그대로)로 HEAD에 있다. 그 뒤 `50f9e1a87`(약한 지반 가라앉기)도 있다.
  - 클립 칸 다시 짜기(CS) 작업은 아직 커밋 전이다(`CombatClipMotion.cs` 등 추적 안 됨, 작업 트리 변경 132개). 그래서 **HEAD 단독으로는 컴파일되지 않는다** — HEAD의 캐릭터 처리기가 `CombatClipMotion`을 두 곳에서 쓴다.
  - 이 계획이 고칠 파일 중 CS 변경이 커밋 안 된 채 섞여 있는 것: `CombatCharacterEventHandler.cs`, `CombatSceneEventPresenter.cs`. 나머지(재생기·실행 목록·시작 질의·조립·반응 조각·판정식·반응 부품·반응 재생기·외부 진행 조각·연발 앞 탄)는 깨끗하다.
- **T0.2 바뀌는 줄의 다른 작업자(blame, 커밋 이름 기준).** 사용자 계정(Droute)·Claude 줄은 뺐다. `55021fc0f`·`d998181ad`(이름 천명님, 11-11 "[천명] 전투 View, 자폭 깜빡임…"·develop 병합)은 CLAUDE.md 자폭 깜빡임 기록의 커밋과 같다 — 그 작업을 천명님 계정으로 올렸는지, 천명님이 직접 했는지는 이력만으로는 알 수 없어 천명님 줄로 센다.
  - 캐릭터 처리기
    - 생성자 인자: 천명님 3줄(자폭 `selfDestruct` 등)·호진님 1줄(토네이도)·민형님 1줄(`isAttackHit`, 랙돌) — 인자 목록을 바꾸면 닿을 수 있음.
    - `TryBeginBeforeFacts`: 호진님 1줄(토네이도 갈래)·민형님 2줄(사망 갈래) — 끊기·피격 호출 줄을 바꿀 때 닿을 수 있음.
    - `TryBeginDeath`(HC-b2가 고침): 민형님 4줄(랙돌 `launchRagdoll`·반응 시작 인자)·천명님 5줄(자폭 갈래, 그대로 둠).
    - 옮기는 것(HC-c): `TryBeginHit` 천명님 1줄, `TryBeginContactHit` 천명님 1줄(병합), `SubjectBodyOf` 천명님 2줄.
    - 그대로 두는 것: 토네이도·폭풍 따라가기(호진님 37줄·현강님 3줄), 자폭 사망·추락 조립(천명님).
  - 사건 Presenter: 처리기 생성 줄 둘(198·199)이 천명님(병합 `d998181ad`) — 인자를 바꿈.
  - 실행 목록 `CombatPresentationPlayers`: 호진님 2줄(외형 재생기, 그대로 둠). 바꾸는 `reaction` 줄은 사용자 줄.
  - 반응 부품 `CombatUnitReactionSet`(HC-b3에서 지움): 민형님 4줄(랙돌 `BeginDeath` 인자).
  - 시작 질의 `CombatStartBlock.Reaction`·조각 실행 재생기 통과 갈래·조립 표지·넉백 앞 피격 목록·반응 조각: 모두 사용자·Claude 줄.
  - 공유 대상(HC-d): 천명님(처리기 피격 줄 넷·Presenter 두 줄), 민형님(사망 랙돌 줄·반응 부품 삭제), 호진님(처리기 생성자·갈래 줄 — 닿으면). 현강님은 닿지 않을 것으로 보임.
- **T0.3 시험 참조:** `Assets/Tests`에서 바뀌는 형식 12개 참조 0(설계 때와 같음).
- 계획과 다른 점: §0 3번의 `CombatVolleyContactHits.cs`는 파일을 바꾸지 않는다(생성 인자가 이미 대리자라 사건 Presenter 줄만 바뀜, TP-11).
- 사용자 결정(11-17 "섞인 채 진행"): CS 작업 커밋을 기다리지 않고 진행한다. 캐릭터 처리기·사건 Presenter의 이번 변경은 나중에 CS 변경과 조각 단위로 나눠 커밋한다.

### 6.2 HC-a (2026-11-17, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 화면 변화 없음)

- Ta.1 `CombatCharacterHits.IsPushLeadInHit`(재구성 뺀 `IsHitLike`)를 `CombatKnockbackLeadIns`가 부르고 `IsHit` 사본을 걷음. 옛 식과 같은지 대조함(재구성이 아니면 `IsHitLike`의 체력 갈래 = 옛 `IsHit`). 처리기 주석 한 줄 이름 고침.
- Ta.2 새 `Presentation/Pieces/ICombatRunSubjectPass.cs`(+ `.meta`): `Passes`·`IsCutBy`·`Cuttable`·`ForbidsPose`.
- Ta.3 `CombatComposition`: 표지 둘·입구 둘을 걷고 `SubjectPass` + `PassSubject(pass)`(사망 아닌 유닛 실행만·한 번만·자세를 막는 규칙이면 이미 빌린 자세 거절), `Lease`의 자세 검사는 `SubjectPass?.ForbidsPose`.
- Ta.4 새 `Playback/Support/CombatPushAlongsidePass.cs`·`CombatNextHitPass.cs`(+ `.meta`): 필드 없는 `sealed`, private 생성자, `Instance`. 본문은 옛 재생기 갈래 그대로(넉백과 함께 = 행동 시작·출처 여는 사건 아님 + 비캐릭터 사건 또는 밀림 걸음, 다음 피격 = `RestartsHit`).
- Ta.5 `CombatPieceRunPlayer`: `FindBlock` 갈래 둘 → `SubjectPassOf(item)?.Passes(item)` 하나(주체 = `CombatEventTypeTable.TryGetSubject`, 캐릭터 사건은 `Final.EntityId`로 옛 `PassesYieldingHit`과 같음), `TryBeginCore`는 `SubjectPass?.Cuttable`, `CutYieldingRun(id)` → `CutRunFor(item)`. 쓰지 않게 된 using 둘(`Playback.Support`·`Presentation.Movement`)을 걷어 재생기가 사건 지식(`CombatCharacterHits`·`CombatMovementKinds`)을 모르게 됨.
- Ta.6 캐릭터 처리기: 넉백과 함께 `.PassSubject(CombatPushAlongsidePass.Instance)`, 접촉 피격 `.PassSubject(CombatNextHitPass.Instance)`, 끊기 둘 `_pieces.CutRunFor(character)`·`CutRunFor(damage)`(옛 `RestartsHit` 검사는 규칙 안으로), 주석 셋.
- 옛 이름 남은 곳 0(코드 검색). 새 파일 셋은 `.meta`를 직접 만듦(Unity 기본 MonoImporter 형식, 새 GUID).
- 동작: 같은 사건을 같은 조건으로 통과·끊기. 바뀐 것은 예외 문구 둘(조립의 자세 거절·재생기의 완료 통지 거절)뿐.

### 6.3 HC-b1 (2026-11-17 사용자 "HC-b1 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- R13 대조(코드): `FieldUnitView` 그리기는 자세나 서는 자리를 빌린 동안 자리·방향을 쓰지 않는다(`HasLeasedPoseOrStand`, 자리 `ApplyStandingPosition`·방향 `ApplyRotation`). 체력 사건은 자리가 그대로라 빌리는 때가 그리기 뒤로 가도 같은 자세를 잡는다. 대신 맞기의 서는 자리는 그대로 빌린 채라 그리기가 덮지 않는다. U-9는 그대로 확인 항목.
- Tb.1 `CombatHitMotionPiece.cs` → `CombatReactionMotionPiece.cs`(`git mv`, `.meta` GUID 유지). 생성자 private, 만들기 `Hit`·`Death`(사망은 HC-b2에서 씀), 끝 콜백은 조각마다 한 번 만듦.
- Tb.2 `ComposeHit`의 `Action<Action>` 입구를 걷음(조각 입구 하나). 문서도 뼈대 = 반응 조각 또는 기다림으로.
- Tb.3 보통 피격: `BeginPose`·반응 부품 시작·반응 취소 정리·try/catch를 걷고, 사실 반영 → 다시 그리기 → 뼈대(코드 움직임이면 `CombatReactionMotionPiece.Hit` + `Lease(Pose)`, 아니면 `CombatWaitPiece(HitDuration)`) → 조립 시작. 맞은 방향 회전은 지금 표시 회전(`TryGetRotation`).
- Tb.4 연발 앞 탄: `_reaction.IsPlayingFor(id)` 검사를 걷고(R5) 반응 조각을 `CombatReactionMotionPiece.Hit`로.
- 계획과 다른 점: **11-17에 더한 `BeginClipOnlyHit`을 보통 피격 길에 합쳤다.** 반응을 조각으로 옮기자 두 길이 뼈대·자세 대여 말고 같아져서다. HC-c의 뼈대 고르기 하나(`SkeletonOf`)를 일부 앞당긴 것이고, HC-c에서는 이 갈래를 `CombatHitStart`로 옮기며 연발 앞 탄과 함께 함수 하나로 묶는다.
- 아직 남은 것: 사망은 반응 부품·반응 재생기·외부 진행 조각을 쓴다(HC-b2). 반응 부품의 `BeginHit`은 쓰는 곳이 없어졌다(HC-b3에서 파일째 지움).
- 같은 시간에 다른 작업(유닛 소리 — 처리기 문서의 "유닛 소리 설계 US1", `sound?.HitSoundsOf(subject)` 등)이 같은 처리기 파일을 고치고 있다. 이번 변경은 그 줄들을 건드리지 않았고, 고칠 자리를 매번 다시 읽고 좁게 바꿨다.

### 6.4 HC-b2 (2026-11-17 사용자 "HC-b2 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Tb.5 보통 사망: `BeginPose`·반응 부품 `BeginDeath`·반응 취소·자세 되돌리기 정리를 걷고, 조립에 `CombatReactionMotionPiece.Death(…, launchRagdoll, hold.Release)` + `Lease(id, Pose)`. 순서는 그대로 사망 유지 → 조립 시작 → (세대 같으면) 사실 반영·다시 그리기. 맞은 방향 회전은 지금 표시 회전(`TryGetRotation`, 옛 빌린 자세의 회전과 같은 값).
- 예외 정리는 사망 유지 놓기 하나. 시작한 뒤의 예외는 사건 Presenter 오류 정리가 실행을 취소하고, 조각 취소 → 반응 부품 `Cancel` → `ClosePose`가 자세 되돌리기와 `releaseModel`(사망 유지 놓기)을 부른다. 사망 유지 놓기는 지금 대여가 아니면 아무것도 하지 않아(`DeathHold.Release`) 두 번 불려도 된다(코드 확인).
- 시작 실패 문구: "빌릴 채널이 없는 사망 조립은 항상 시작해야 합니다" → "사망 조립을 시작하지 못했습니다(자세 대여 실패)"(이제 자세를 빌림).
- Tb.6 `ComposeDeath`가 조각을 받는 입구 하나(`ICombatPresentationPiece reaction`). 랙돌·디졸브·사망 유지 규칙은 반응 부품 안 그대로(TP-4).
- 처리기 클래스 문서: 피격·사망 반응 모두 반응 조각, 표시 사라짐은 조각 실행 재생기가 알림.
- 이제 처리기는 반응 부품·`_cancelUnavailableReaction`·`RunCleanup`을 쓰지 않는다(필드·생성 인자는 HC-b3에서 걷음 — 그 전까지 "쓰지 않는 필드" 경고가 날 수 있음).
- 바뀐 다른 작업자 줄: `TryBeginDeath`의 랙돌 줄(민형님 `2ddb5f342`) — 반응 시작 인자 줄이 조각 만들기로 바뀜. HC-d 민형님 공유에 넣는다. 자폭·추락 사망 길은 건드리지 않음.

### 6.5 HC-b3 (2026-11-17 사용자 "HC-b3 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Tb.7 지운 파일(`git rm`, 스테이지됨): `Presentation/Reaction/CombatUnitReactionSet.cs`, `Playback/Players/CombatReactionPlayer.cs`, `Presentation/Pieces/CombatReactionPiece.cs`(각 `.meta` 함께). 지우기 전 코드·문서 주석의 참조 0을 확인했다.
  - 실행 목록 `CombatPresentationPlayers.Create`: `reaction` 인자·배열 줄을 걷음. 순서는 외형 → 몸 위 글자 → 공격 → 조각 실행 → 걸음.
  - 막는 이유 `CombatStartBlock.Reaction`을 걷고 `PieceSubject` 문서에 합침. 숫자로 저장하거나 비교하는 곳은 없다(검색).
  - 사건 Presenter: 반응 부품 필드·반응 재생기 생성 줄·`Create`의 `reaction:` 인자·처리기 생성의 반응 인자 둘을 걷음(처리기 생성 둘째·셋째 줄은 천명님 병합 줄 — 첫 줄만 바뀌어 그 줄들은 그대로).
- Tb.8 캐릭터 처리기: 필드 둘·생성 인자 둘(`reaction`·`cancelUnavailableReaction`), 쓰지 않게 된 `RunCleanup`·`ErrorHandling` using을 걷음. 생성 인자 16 → 14(HC-c에서 `knockbackLeadIns` → `hits`로 바뀌어 그대로 14).
- 낡은 문서 줄: 조각 실행 재생기·주체 막기 규칙·주체 상태 계약·반응 조각에서 "반응 재생기"를 걷고 "유닛 실행 안의 반응 조각"으로.
- 반응 부품(`CombatUnitReactionPresenter`)의 `Release`는 이제 쓰는 곳이 없다(반응 조각은 `Cancel`만 씀). 지우지 않고 둔다(작은 내부 함수, HC-d 검토에서 판단).
- 실행 목록이 하나 줄어, 프레임마다 반응 재생기의 `BeginFrame`·`Tick`·`FindBlock` 호출이 빠진다.

### 6.6 HC-c (2026-11-17 사용자 "HC-c 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Tc.1 새 `Playback/Support/CombatHitStart.cs`(+ `.meta` 직접, 새 GUID). 처리기에서 옮긴 것: 보통 피격 `TryBegin`, 밀림과 함께 가는 피격 `BeginPushHit`(private), 연발 앞 탄 `TryBeginContact`, 피격 조립 `Compose`(옛 `ComposeHit`), 정적 `SubjectBodyOf(facts, mapper, id, e, facing)`. 새로 둔 것: 끊기 `CutPrevious(e)`(R1 — 처리기와 다음 탄이 부름), 뼈대 고르기 `SkeletonOf(…, out leasesPose)`(보통 피격·연발 앞 탄이 부름). "사실을 반응 뒤에 반영" 감싸기는 보통 피격 한 곳에만 남아 따로 함수로 빼지 않음(HC-b1에서 클립만 길을 합쳐 사본이 없어짐).
  - 옮긴 본문은 HC-b1까지의 코드 그대로(동작 같음). 유닛 소리 작업의 피격 소리 줄(`sound?.HitSoundsOf(subject)`)도 `Compose`에 그대로 옮김.
- Tc.2 캐릭터 처리기: 피격 블록 둘(보통·밀림·연발 앞 탄·몸 자리 / 피격 조립)을 지우고 `TryBeginBeforeFacts` = 피해 숫자 → `_hits.CutPrevious` → 토네이도 → 사망 → `_hits.TryBegin`. 생성 인자 `knockbackLeadIns` → `hits`(14개 그대로). 사망·자폭의 몸 자리는 `CombatHitStart.SubjectBodyOf`. 클래스 문서를 갈래·사망 중심으로 다시 씀, 쓰지 않게 된 `Presentation.Reaction` using 걷음. 처리기 590 → 360줄(유닛 소리 작업의 추가 줄 포함), 피격 시작 253줄(문서 포함).
- Tc.3 사건 Presenter: `CombatHitStart`를 만들어 처리기 생성 인자와 연발 앞 탄 피격(`CombatVolleyContactHits`의 입구 = `hits.TryBeginContact`)에 넘김. 처리기 생성 줄의 둘째 줄(천명님 병합 줄)은 마지막 인자 하나가 `_knockbackLeadIns` → `hits`로 바뀜.
- 옛 이름(`TryBeginContactHit`·`ComposeHit`·`TryBeginHit`·처리기의 `SubjectBodyOf`) 남은 곳 0.

### 6.7 HC-d (2026-11-17 사용자 "HC-d 진행하고 7기준 검토 돌려")

- 문서: 폴더 배치 안내(11-17 갱신 줄, 표 `Players/`·`Support/`·`Pieces/`·`Reaction/` 칸, §2 규칙 줄 둘), 설계 §9(실제로 바뀐 다른 작업자 줄)·§10(구현 메모), 공유 메모 [hit-structure-cleanup-share](../handoffs/2026-11-17-combat-hit-structure-cleanup-share.md)(천명님·민형님·호진님, 미전달). 공유 대상 줄은 HEAD blame으로 다시 셈: 천명님 8줄, 민형님 2줄 + 지운 반응 부품 목록 4줄, 호진님 1줄(쓰지 않게 된 using).
- **묶음 끝 검토(새 에이전트 하나, 읽기 전용 정적 검토): 컴파일 오류 후보 없음, 높음·중간 0, 낮음 6, 7기준 약 8.89 → 권장 반영 뒤 약 9.14.**
  - 점수: SSOT 9.0 · SOLID 8.5 · FW 9.5 · 독립성 9.0 · 캡슐화 8.5 · C#/Unity 8.75 · If/Tick 9.0.
  - 낮음: D1 보통 피격 자세 대여가 그리기 뒤(화면 같음, U-1·U-9) · D2 연발 앞 탄 끊기가 `RestartsHit` 규칙에 기댐(지금 Core와 같음, 기록만) · D3 `CombatComposition.PausesOtherPlayers` 주석이 "반응도 멈춤"으로 낡음(도달하는 길 없음) · D4 B14 O12 없어진 것은 동작 같음(확인) · D5 사망 시작 뒤 예외 정리는 바깥 `Abort`에 맡김(그대로) · D6 반응 조각이 "표시 사라짐"을 정상 끝으로 받음(도달 안 함, 주석 권장).
  - `CombatUnitReactionPresenter.Release`는 쓰는 곳 0, 지워도 안전(`_released` 검사 둘·`Version` 속성도).
  - 권장 반영: ① 반응 부품 `Release`·`_released`·`Version` 지움 ② 창구 `BeginPose(EntityId, Action)` 쓰는 곳 0 → 지움·낡은 문서 줄 ③ 반응 조각의 자세 대여 요구를 한 곳으로(조립 함수가 스스로 `Lease(Pose)`, `out leasesPose`·호출자 `Lease` 세 줄 없앰) ④ `IsPushLeadInHit` = `!AppliesFactsAfterReaction && IsHitLike` ⑤ 몸 자리 식 세 곳 → 도우미 하나 ⑥ `HitMotionWithClip` private, "코드 피격 움직임을 내나" 질의 하나를 `SkeletonOf`·`CombatPushLean`이 같이 ⑦ 낡은 주석(조립 `PausesOtherPlayers`, 주체 막기·걸음 재생기·시작 질의·사건 형식 표·재생 조정의 "반응" 말) ⑧ 사건 Presenter의 쓰지 않는 `Presentation.Reaction` using ⑨(선택) `SubjectPassOf` 실행 0개면 바로 null.
  - 그대로 둘 것: 통과 규칙 `Instance`, `Cuttable`·`IsCutBy` 분리, D5, D2, `CombatHitStart` 생성 인자 9개.
  - 보고만: `FieldUnitView.BeginReactionPose`는 HEAD 때부터 쓰는 곳 없음.
- 반영함(11-17 사용자 "권장대로 반영", §6.8). ②·⑦은 이번에 안 고친 파일(`CombatUnitPresentationWindow`·`CombatStepPlayer`·`CombatEventTypeTable`·`CombatSceneEventPlayback`)도 닿는다 — `CombatUnitPresentationWindow`에는 커밋 안 된 CS 변경이 있다.

### 6.8 검토 권장 반영 (2026-11-17 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- ① 반응 부품 `CombatUnitReactionPresenter`: `Release`·`_released`·`Version` 지움, 시작 검사는 `IsPlaying || _cleaning`, Tick 완료는 판 번호가 같을 때만.
- ② 창구 `CombatUnitPresentationWindow.BeginPose(EntityId, Action)` 지움(쓰는 곳 0), 그 문서는 `TryBeginPose`로 합침, 가리키던 cref 둘 고침.
- ③ 뼈대 값 `CombatHitStart.Skeleton`(조각 + 자세를 빌리나, `Wait(settings)`)·`SkeletonOf` 하나. 조립 함수 `Compose`(private)가 `skeleton.LeasesPose`면 스스로 `Lease(Pose)`, 사망 조립 `ComposeDeath`도 스스로 빌림 → 호출자의 `Lease` 줄·`out leasesPose` 없어짐.
- ④ `CombatCharacterHits.IsPushLeadInHit` = `!AppliesFactsAfterReaction(e) && IsHitLike(e)`.
- ⑤ 몸 자리 식은 `CombatHitStart.SubjectBodyOf`(정적) 하나 + 인스턴스 입구 `BodyOf(id, e)`. 보통 피격·밀림과 함께 가는 피격·사망이 `BodyOf`, 연발 앞 탄·자폭은 회전을 이미 들고 있어 `SubjectBodyOf`.
- ⑥ 반응 값 `_hitMotionWithClip` private 필드, 규칙 원본은 그대로 `PlaysHitMotion(bool)`(속성 뒤로 옮김). 표시 쪽 질의는 창구 `PlaysHitMotion(EntityId)` 하나 — `SkeletonOf`·`CombatPushLean`이 부름.
- ⑦ 낡은 주석: 조립 `PausesOtherPlayers`·`ForFieldAction`(호진님 줄, 공유 메모 §3에 더함), 조각 실행 재생기 Tick 주석, 주체 막기 셋, 걸음 재생기·시작 질의 `Unavailable`·사건 형식 표 `OpensAttackSource`·재생 조정 판 번호, 재생기 계약 `BeginFrame`(지금 여기서 던지는 재생기 없음).
- ⑧ 사건 Presenter의 쓰지 않는 `using …Presentation.Reaction;` 지움.
- ⑨ `CombatPieceRunPlayer.SubjectPassOf`: 실행이 없거나 사건이 없으면 바로 null.
- D6: `CombatReactionMotionPiece` 생성자에 "쓸 수 없음도 끝으로 받는 까닭"(실행의 대여 취소가 사라짐을 먼저 알림) 주석.
- 남은 곳 확인(grep): 옛 이름(`ComposeHit`·`TryBeginContactHit`·`CombatReactionPlayer`·`CombatUnitReactionSet`·`CombatHitMotionPiece`·표지 둘·`CutYieldingRun`·`BeginClipOnlyHit`) 0, `HitMotionWithClip` 밖에서 읽는 곳 0, 창구 `BeginPose(EntityId…)` 0.
- 그대로: D2·D5, 통과 규칙 `Instance`, `Cuttable`·`IsCutBy` 분리, `CombatHitStart` 생성 인자 9개. 보고만: `FieldUnitView.BeginReactionPose`(HEAD 때부터 쓰는 곳 없음, 지우지 않음).
- 예상 7기준 약 8.89 → 약 9.14(검토자 추정, 다시 재지 않음).
- 남은 것: Unity 컴파일·재생 확인 U-1~U-10(§2), 공유 메모 전달, 커밋(§4 — CS 작업 커밋 뒤, 같은 처리기·창구 파일에 CS·유닛 소리 변경이 섞여 있음).
