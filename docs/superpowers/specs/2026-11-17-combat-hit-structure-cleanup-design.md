# 전투 피격 구조 정리 설계 (초안)

- 날짜: 2026-11-17
- 요청: 사용자 "지금 피격 구조 7기준 검사 해볼래" → 약 8.1점 → "반사 피격 먼저 확인"(결함 아님) → "설계 진행"
- 상태: **결정 완료(2026-11-17 사용자 "권장안대로" = Q-HC1~Q-HC3 모두 A, P2 "Instance 하나 공유")**. 코드 변경 없음. Task 계획은 [hit-structure-cleanup-plan](../plans/2026-11-17-combat-hit-structure-cleanup-plan.md).
- 범위: 전투 View의 피격·사망 반응 연출 구조(작성자와 무관, CLAUDE.md 작업 영역). Core·Framework·Factory는 바꾸지 않는다.

## 0. 한 줄 요약

피격·사망의 코드 움직임(젖힘·밀림·쓰러짐)을 모두 **조립 안의 스스로 진행하는 조각 하나**로 돌리고, 반응 부품·반응 재생기·외부 진행 조각을 걷는다.
피격 시작은 캐릭터 처리기에서 떼어 내고, 조각 실행 재생기가 아는 피격 전용 통과 규칙은 조립이 들고 오게 한다.
보이는 동작은 바꾸지 않는다(§4).

## 1. 지금 구조

### 1.1 피격 길 넷과 사망 길 셋

| 길 | 시작하는 곳 | 뼈대 조각 | 자세 대여 | 진행하는 쪽 |
|---|---|---|---|---|
| 보통 피격 | `CombatCharacterEventHandler.TryBeginHit` | `CombatReactionPiece`(외부 진행) | 처리기가 사실 반영 **전**에 `BeginPose` | 반응 부품 `CombatUnitReactionSet` + 반응 재생기 `CombatReactionPlayer` |
| 클립만 피격(11-17) | `BeginClipOnlyHit` | `CombatWaitPiece(HitDuration)` | 없음 | 조각 실행 재생기 |
| 넉백과 함께 피격 | `BeginPushHit` | `CombatWaitPiece(HitDuration)` | 없음(`AllowSubjectPush`) | 조각 실행 재생기, 사건은 바로 완료 |
| 연발 앞 탄 접촉 피격 | `TryBeginContactHit` | `CombatHitMotionPiece`(스스로 진행) 또는 `CombatWaitPiece` | 조립이 `Lease(Pose)` | 조각 실행 재생기, `YieldToNextHit` |
| 보통 사망 | `TryBeginDeath` | `CombatReactionPiece`(외부 진행) | 처리기가 `BeginPose` | 반응 부품 + 반응 재생기 |
| 낭떠러지 추락 | `BeginFallDeath` | 가라앉기·작아지기 | 조립이 `Lease(Pose)`·`Lease(Scale)` | 조각 실행 재생기 |
| 자폭 깜빡임 사망 | `BeginSelfDestructDeath` | `CombatFlashPiece` | 조립이 `Lease(Flash)` | 조각 실행 재생기 |

움직임 식은 모두 `CombatUnitReactionPresenter` 하나다(피격 = 밀렸다 돌아오며 젖힘, 사망 = 쓰러짐·랙돌·디졸브). 진행하는 쪽만 둘로 갈린다.

### 1.2 막기 규칙

- 조각 실행 재생기 `CombatPieceRunPlayer.FindBlock`에 피격 전용 통과 갈래가 둘 있다.
  - `PassesPushAlongsideHit`: 조립 표지 `AllowsSubjectPush` + 밀림 걸음·비캐릭터 사건 판정(`CombatMovementKinds`).
  - `PassesYieldingHit`(11-17): 조립 표지 `YieldsToNextHit` + `CombatCharacterHits.RestartsHit`.
- 반응 재생기 `CombatReactionPlayer`도 주체 막기(`CombatSubjectBlocking`)를 따로 답한다.

### 1.3 프레임 순서(확인한 사실)

- 한 프레임 = 재생기 모두 Tick(공격 → 조각 실행 → 걸음 → 반응 → …) → 그 뒤 순차 재생이 새 사건을 시작(`OrderedCombatPresentation.Tick`).
- 그래서 사건이 연 반응·실행은 모두 다음 프레임부터 시간을 받는다. 반응 재생기의 "이 프레임에 시작한 반응은 시간 안 받음"(B14 O12) 규칙은 재생기 Tick 안의 콜백에서 시작한 반응만 막는다.
- 행동 실행(공격·포효)이 히트 전이면 조각 실행 재생기가 뒤 재생기(걸음·반응)의 Tick을 멈춘다. 조각 실행 재생기 자신의 실행은 계속 돈다. 연발 앞 탄이 스스로 진행하는 조각을 쓰는 이유다.

## 2. 문제(7기준 검사, 2026-11-17)

| 번호 | 심각도 | 기준 | 내용 |
|---|---|---|---|
| H1 | 중간 | SOLID·독립성 | 같은 젖힘 식을 두 갈래(반응 부품+재생기 / 스스로 진행 조각)가 돌린다. 자세를 빌리는 때·취소·표시 사라짐 처리가 갈래마다 다르고, 한 번 바꿀 때 두 곳을 고친다(11-17 "코드 움직임 끄기"가 그랬다) |
| H2 | 중간 | SOLID | `CombatCharacterEventHandler` 590줄에 피격 넷·사망 셋·토네이도가 함께 있다 |
| H3 | 중간 | SOLID(OCP)·독립성 | 범용 조각 실행 재생기가 피격 전용 통과 규칙 둘을 직접 안다. 규칙이 늘 때마다 `FindBlock`과 조립에 표지·함수를 하나씩 더한다 |
| H4 | 낮음 | SSOT | `CombatKnockbackLeadIns.IsHit`가 `CombatCharacterHits.IsHitLike`의 체력 식 사본이다(재구성만 뺌). 문서는 "처리기와 같은 판정"이라고 틀리게 적혀 있다 |
| H5 | 낮음 | SSOT | 피격 뼈대 고르기(반응 / 기다림 / 스스로 진행)가 네 곳에 흩어져 있고 `CombatWaitPiece(HitDuration)`을 세 곳에서 만든다 |
| H6 | 낮음 | SSOT | "사실을 반응 뒤에 반영"(재구성) 감싸기가 두 곳에 같은 모양으로 있다 |
| H7 | 낮음 | 캡슐화 | `CombatReactionPiece.Cancel()`이 비어 있어, 조립을 끊어도 반응은 사건 Presenter가 따로 취소해야 한다 |

반사 피격 대여 충돌 후보는 결함이 아니다(공격 Gate 규칙 1이 공격자 자신의 사건을 전체 종료까지 붙잡음, `CombatHitGate.cs:148-151`).

## 3. 설계

### HC1 반응 움직임은 조각 하나로 (H1·H7)

- `CombatHitMotionPiece`를 `CombatReactionMotionPiece`로 넓힌다(`git mv`, GUID 유지). 만들기 입구 둘:
  - `Hit(unit, settings, damage, mapper)` — 지금과 같다.
  - `Death(unit, settings, death, mapper, launchRagdoll, releaseModel)` — `CombatUnitReactionPresenter.BeginDeath`를 감싼다.
- 조각이 반응 부품(`CombatUnitReactionPresenter`)을 하나 갖고 Tick·취소한다. 자세는 조립이 `Lease(id, Pose)`로 빌린다(연발 앞 탄과 같은 꼴).
- 보통 피격·보통 사망을 이 조각으로 바꾼다. 처리기의 `BeginPose`·`_cancelUnavailableReaction`·반응 취소 정리를 걷는다.
- 걷는 것(Q-HC1 A): `CombatUnitReactionSet`, `CombatReactionPlayer`, `CombatReactionPiece`, 실행 목록의 반응 자리(`CombatPresentationPlayers.Create(reaction:)`), `CombatStartBlock.Reaction`.
- 같은 유닛 배타는 "유닛 실행은 주체마다 하나"가, 기차 동시 사망의 "사망끼리만 겹침"은 조각 실행 재생기의 `CanOverlapDeaths`가 이미 맡는다.
- 표시 사라짐: 자세 대여를 밖에서 잃으면 실행이 사라짐이 되고 다음 Tick 맨 앞에서 예외다. 반응 재생기의 `CancelUnavailable`과 같은 결과다(B14 O13).

### HC2 피격 시작을 처리기에서 떼어 냄 (H2·H5·H6)

- 새 `Playback/Support/CombatHitStart.cs`(공격의 `CombatAttackStart` 꼴). 피격 길 넷과 피격 조립 `ComposeHit`을 옮긴다.
- 뼈대 고르기는 함수 하나 `SkeletonOf(...)`: 넉백과 함께면 기다림(자세 없음, 사건 바로 완료), 코드 움직임을 안 내면(`PlaysHitMotion`) 기다림, 아니면 `CombatReactionMotionPiece.Hit`(자세 대여). 보통 피격·연발 앞 탄이 같은 함수를 부른다.
- "사실을 반응 뒤에 반영" 감싸기는 함수 하나.
- 캐릭터 처리기에는 갈래(토네이도 → 사망 → 피격)와 사망 셋, 끊기 한 줄만 남는다. 예상 590 → 약 380줄, 피격 시작 약 230줄.
- 주체 몸 자리(`SubjectBodyOf`)는 피격·사망이 함께 쓰므로 `CombatHitStart`의 정적 함수로 두고 처리기가 부른다.

### HC3 주체 사건 통과 규칙은 조립이 들고 옴 (H3)

- 새 계약 `Presentation/Pieces/ICombatRunSubjectPass.cs`:
  - `bool Passes(CombatEvent item)` — 이 실행이 그 주체의 이 사건을 막지 않는지.
  - `bool IsCutBy(CombatEvent item)` — 이 사건이 시작하며 이 실행을 끊는지.
  - `bool ForbidsPose` — 자세 채널을 빌리면 안 되는지(지금 `AllowSubjectPush`의 검사).
- 조립은 `PassSubject(ICombatRunSubjectPass)` 하나를 받는다. 표지 `AllowsSubjectPush`·`YieldsToNextHit`와 입구 `AllowSubjectPush()`·`YieldToNextHit()`는 걷는다.
- 구현 둘은 `Playback/Support/`에(사건 지식이 필요해서):
  - `CombatPushAlongsidePass` — 지금 `PassesPushAlongsideHit` 판정, `ForbidsPose = true`, 끊지 않음.
  - `CombatNextHitPass` — 지금 `PassesYieldingHit` 판정(`RestartsHit`), 끊음, 완료 통지 있는 시작은 재생기가 거절(지금과 같음).
- 조각 실행 재생기는 `FindUnitRun(subject)?.Composition.SubjectPass?.Passes(item)`만 묻고, 끊기는 `CutRunFor(item)` 하나(그 주체 실행의 규칙이 `IsCutBy`면 취소). 재생기는 `CombatCharacterHits`·`CombatMovementKinds`를 모르게 된다(11-17에 더한 Support 참조도 걷음).
- 두 규칙은 필드가 없는 객체라, 피격마다 새로 만들지 않고 클래스 안의 `internal static readonly Instance` 하나를 함께 쓴다. 이미 있는 꼴이다 — 진입 연출의 상태 없는 출처 수명 구현 `CombatEntryRiseAppearance.EntrySources.Instance`(11-02), 씬 생성 계약 구현 `CombatSceneFactory.Instance`. 값을 들고 바뀌는 관리자 싱글턴(어디서나 `Instance`로 꺼내 쓰는 전역 상태)이 아니다. 쓰는 곳은 조립을 만드는 피격 시작 한 곳뿐이다.

### HC4 판정식 사본 하나로 (H4)

- `CombatCharacterHits.IsPushLeadInHit(e) => e.Kind != Reconstructed && IsHitLike(e)`를 두고 `CombatKnockbackLeadIns`가 부른다. `CombatKnockbackLeadIns.IsHit`는 걷고, 잘못된 문서 줄을 고친다.

## 4. 바뀌는 동작

- 보이는 동작은 같게 한다. 따져 본 차이:
  - **첫 프레임 시간**: 사건은 재생기 Tick 뒤에 시작하므로(§1.3) 조각으로 옮겨도 첫 시간은 다음 프레임부터다. 같다.
  - **자세를 빌리는 때**: 보통 피격은 지금 사실 반영 **전**에 빌리고, 옮긴 뒤에는 조립 시작(사실 반영 뒤)에 빌린다. 체력 사건은 자리가 바뀌지 않아 화면은 같다. 사망은 지금도 조립 시작 전에 빌리고 사실은 조립 시작 뒤 반영이라 같다.
  - **행동 실행 히트 전**: 조각 실행은 행동 실행이 히트 전이어도 돈다. 그러나 행동 시작 사건은 모든 유닛 실행이 끝날 때까지 기다리므로(주체 막기), 히트 전 행동 실행과 보통 피격·사망이 함께 있는 일은 없다. 연발 앞 탄만 예외이고 그것은 지금도 조각이다.
  - **예외 문구**: 표시가 사라졌을 때 문구가 반응 재생기 것에서 조각 실행 재생기 것으로 바뀐다.
- 프로파일 표지(피격·사망 진행 계측)는 반응 부품 안에 있어 그대로 남는다.

## 5. 정할 것

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-HC1 | 반응 움직임을 조각 하나로 옮길 범위 | **A 피격·사망 모두**(반응 부품·반응 재생기·외부 진행 조각을 걷음, 진행 갈래 1개) | B 피격만(사망은 반응 부품에 남아 갈래가 "피격 = 조각, 사망 = 반응 부품"으로 나뉨, 걷는 것 없음) |
| Q-HC2 | 통과 규칙 자리 | **A 조립이 규칙 객체를 들고 옴(HC3)** | B 지금 표지 둘 유지, 문서만 정리 |
| Q-HC3 | 피격 시작 떼어 내기 | **A 새 `CombatHitStart`로(HC2)** | B 처리기 안에서 함수만 모음(파일 그대로) |

## 6. 설계에서 정한 것(확인 대상)

- P1 `CombatHitMotionPiece`는 새 파일이 아니라 이름을 바꿔 넓힌다(`git mv`, GUID 유지).
- P2 통과 규칙 구현 둘은 필드가 없는 객체이고 `static readonly Instance` 하나를 함께 쓴다(HC3, 전역 상태 싱글턴이 아님). 다른 안이던 "조립마다 `new`"는 쓰지 않는다. **정함(2026-11-17 사용자 "Instance 하나 공유").**
- P3 `CombatStartBlock.Reaction` 값은 걷는다(쓰는 곳이 반응 재생기 하나).
- P4 사망 유지(`HoldDeath`)·랙돌·디졸브 규칙은 반응 부품 안 그대로이고 조각은 감싸기만 한다.
- P5 `ComposeHit`·`ComposeDeath`의 `Action<Action> beginReaction` 입구는 걷고 조각을 받는 입구 하나로 한다.
- P6 Factory·Core·시험 무변경(시험 참조 0을 확인함). 새 시험은 쓰지 않는다(View 작업 규칙).

## 7. 7기준 예상(알려진 문제 기준)

| 기준 | 지금 | 권장안 뒤 |
|---|---|---|
| SSOT | 8.0 | 9.25 |
| SOLID | 7.0 | 8.75 |
| FW 계약 | 9.5 | 9.5 |
| 변경 독립성 | 7.5 | 9.0 |
| 캡슐화 | 8.0 | 8.75 |
| C#/Unity 권장 | 8.5 | 8.5 |
| 무의미한 If/Tick | 8.5 | 9.0 |
| 평균 | 약 8.1 | 약 8.9 |

남는 것: 유닛 표시 창구(542줄) 멤버가 많음(지난 평가 V4), 피격마다 람다·조각 객체를 만듦(작음).

## 8. Phase(안)

> 계획 [§1](../plans/2026-11-17-combat-hit-structure-cleanup-plan.md)이 대신한다. 7기준 검사(계획 §5)로 바뀐 것: HC-b를 피격·사망·걷기 셋으로 나눔(R3), 끊기 호출을 `CombatHitStart.CutPrevious` 하나로(R1), 연발 앞 탄의 반응 부품 검사 걷기(R5). 예상 약 8.96.

- HC-0 착수 전 확인: 바꿀 줄 작성자(blame), 11-17 두 작업(코드 움직임 끄기·끊고 다시)의 커밋 여부와 클립 칸 다시 짜기(CS) 작업과의 순서.
- HC-a 판정식·통과 규칙(HC4·HC3): 화면 변화 없음.
- HC-b 반응 조각 하나로(HC1): 보통 피격·사망을 조각으로, 반응 부품·재생기 걷기.
- HC-c 피격 시작 떼어 내기(HC2).
- HC-d 문서(폴더 배치 안내 Pieces·Reaction·Players·Support 줄, 공유)·묶음 끝 검토.
- Unity 확인은 모든 Phase 뒤 한 번(보통 피격·연발·넉백·재구성·사망·랙돌·기차 동시 사망·추락·자폭).

## 9. 다른 작업자 줄

- 고칠 파일의 작성자 이력: 캐릭터 처리기(호진님·천명님·현강님·민형님 줄 있음), 사건 Presenter(호진님·현강님·천명님·민형님), 실행 목록·조각 실행 재생기·조립(호진님·천명님), 반응 부품·반응 표시(민형님).
- 실제로 바뀌는 줄의 작성자는 HC-0에서 blame으로 세고, 바뀐 줄이 있는 작업자에게 변경 정리를 넘긴다.
- 구현 뒤 실제로 바뀐 다른 작업자 줄(HEAD blame): 천명님 8줄(캐릭터 처리기의 피격 조립 호출·몸 자리 함수와 그 문서·자폭 사망 몸 자리·생성 인자 줄, 사건 Presenter 처리기 생성 줄), 민형님 2줄(보통 사망의 랙돌 반응 시작 인자) + 지운 반응 부품 목록의 4줄, 호진님 1줄(캐릭터 처리기의 쓰지 않게 된 `ErrorHandling` using). 공유 메모는 [hit-structure-cleanup-share](../handoffs/2026-11-17-combat-hit-structure-cleanup-share.md)(미전달).

## 10. 구현 메모 (2026-11-17, HC-a~HC-c 코드, 컴파일 미확인)

- 기록 원본은 계획 §6.1~§6.6. 새 파일 넷(`ICombatRunSubjectPass`·`CombatPushAlongsidePass`·`CombatNextHitPass`·`CombatHitStart`, `.meta` 직접), 이름 바꿈 하나(`CombatHitMotionPiece` → `CombatReactionMotionPiece`, GUID 유지), 지운 파일 셋(`CombatUnitReactionSet`·`CombatReactionPlayer`·`CombatReactionPiece`, `.meta` 함께, 스테이지됨).
- 설계와 같게 한 것: HC1~HC4, P1~P6, 보이는 동작 같음(§4). 계획 §5 R12(첫 프레임 시간)·R13(자세를 그리기 뒤에 빌림 — `FieldUnitView` 그리기가 빌린 자세·서는 자리를 덮지 않음)은 코드로 확인했다.
- 설계·계획과 다른 점
  - HC-b1에서 11-17에 더한 "클립만 피격" 길을 보통 피격 길에 합쳤다(반응을 조각으로 옮기자 두 길이 뼈대·자세 대여 말고 같아짐). 그래서 HC2의 "사실을 반응 뒤에 반영 감싸기 하나"는 사본이 없어져 따로 함수로 빼지 않았다.
  - HC3 계약의 `Cuttable`은 설계에 없던 멤버다(계획 TP-2·R2 — 끊길 수 있는지를 시작 때 알아야 해서).
  - `CombatVolleyContactHits.cs`는 바꾸지 않았다(입구가 이미 대리자라 사건 Presenter 한 줄만 바뀜).
- 예외 문구 셋이 바뀜: 조립의 자세 거절, 재생기의 완료 통지 거절, 사망 조립 시작 실패("자세 대여 실패"). 표시가 사라졌을 때의 오류도 반응 재생기 문구에서 조각 실행 재생기 문구로 바뀐다.
- 같은 시간에 다른 세션의 유닛 소리 작업이 캐릭터 처리기·사건 Presenter·`CombatEventCues`를 고쳤다. 피격 소리 줄(`HitSoundsOf`)은 `CombatHitStart.Compose`로 그대로 옮겼고, 그 밖의 소리 줄은 건드리지 않았다.
