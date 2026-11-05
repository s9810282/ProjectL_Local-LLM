# 전투 연출 바탕·개선 계획 (A-1~A-5, B-0~B-4)

- 설계: [전투 사건별 연출 조립 설계](../specs/2026-10-17-combat-presentation-composition-design.md) 2026-10-18 개정. B11 시작 조건 하나, B12 재생기 계약·실행 목록, B13 유닛 표시 자원 창구, B14 겹침 규칙 보존, B15 조각·조립, §9 원인 분석과 개선(I-1~I-6).
- 결정: Q-R7~Q-R12 모두 A(설계 §7.1), Q-R13~Q-R18 모두 A(설계 §9.4). Q-R14가 Q-R8을, Q-R15가 Q-R10을 바꿨다. 보완 1~4 반영.
- 작성: 2026-10-18. 사용자 지시 "보완 4건을 설계에 반영하고, 이어서 A-1~A-3a Task 계획을 써줘", 이어서 "Q-R13~Q-R18 모두 추천안(A)으로 결정. 설계에 기록하고 개선 Phase를 Task 계획에 더해줘."
- **동작 동일이 기본이다.** 완료 조건은 "화면·순서·완료 시점이 지금과 같다"이다. 결정으로 정한 차이만 허용한다.
  - 예외 문구 끝에 막는 이유가 붙는다(Q-R8 A, A-1).
  - 정리·해제가 실행 목록 순서로 한 묶음이 된다. 실패 경로만 달라진다(Q-R7 A, A-2).
  - 지금 도달하지 않는 경로에서 "시작 단정 예외"가 "기다림"이 된다(Q-R14 A, A-2b).
  - 지금 도달하지 않는 같은 유닛의 자세·걸음 배치 겹침이 예외가 된다(Q-R11 A, A-3a).
  - 소리 호출이 같은 프레임 안에서 앞뒤만 바뀐다(Q-R18 A, B-3).
  - 플래시는 새 기능이다(B-4, 요구 1·3).
  - 이 밖에 동작이 달라지는 것을 찾으면 구현하지 않고 묻는다.
- 위치는 **메서드 이름**으로 적는다. 경로 약어: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `D/` = `C/Display/`, `T/` = `Assets/Tests/EditMode/Framework/`, `DB/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/`.

## 0. 착수 조건과 순서

### 0.1 착수 조건

1. **Presenter 분리 커밋과 Unity 결과:** S-1(`ec34d932`) 뒤 S-2~S-5가 미커밋이다. 커밋하고(사용자), S-1~S-5 묶음 Unity Test Runner 결과를 [분리 계획 §8.3](2026-10-18-combat-scene-event-presenter-split-plan.md)에 기록한 뒤 착수한다. A-1이 같은 Presenter를 고치므로, 분리의 Unity 결과가 이 작업의 Unity 기준선이 된다.
2. **관리 실행 기준선:** 분리 계획 §8.3의 최종 트리 결과(5,060개: PASS 2,714 · NATIVE 2,161 · FAIL 138 · UNSUPPORTED 47). 커밋 때 코드가 바뀌면 같은 방법(분리 계획 §7)으로 다시 잡는다.
3. **착수 직전 재대조:** Phase마다 이 계획의 메서드 목록을 코드와 한 번 더 맞춘다. 다르면 계획을 먼저 고친다.

### 0.2 Phase 순서 (Q-R13 A)

| 순서 | Phase | 내용 | 설계 | 이 계획 |
|---|---|---|---|---|
| 1 | A-1 | 시작 조건 하나 | B11 | §2 |
| 2 | A-2 | 재생기 계약·실행 목록 | B12, 보완 1~3 | §3 |
| 3 | A-2b | 질의·단정 정렬 | §9 I-1 | §4 |
| 4 | A-3a | 유닛 표시 자원 창구 | B13, 보완 4 | §5 |
| 5 | A-3b | HUD 피드백 분리 | §9 I-2 | §6 |
| 6 | A-4 | 사건 경로 표 | §9 I-3 | §7 |
| 7 | A-4b | 재생 계약 사슬 줄이기 | §9 I-4 | §8 |
| 8 | A-5 | 전역 연출 값 원본(View DB 설정 표) | §9 I-5 | §9 |
| 9 | B-0 | 조각·조립 상세 설계(코드 없음) | B15, §9 I-6 | §10 |
| 10~13 | B-1~B-4 | 조각·실행 기반 → 포효·소환 재조립 → 소리 조각 → 피격 플래시 | B15, §9 I-6 | §10 |

## 1. 공통 규칙

- **새 클래스:** `C/`(창구·표시 계약·HUD는 `D/`, DB는 `DB/`), 블록 네임스페이스 = 폴더 경로, `internal sealed`(인터페이스·구조체 제외. DB SO는 기존 규칙), Allman, 한국어 XML doc. `.cs`·`.meta` 모두 CRLF, `.meta`는 32자리 hex GUID를 새로 만들고 저장소 안 중복을 확인한다.
- **원본은 그대로:** 재생 세대는 Presenter `_presentationVersion` 하나, Mapper는 Presenter. 새 클래스는 `Func<int>`·`Func<FieldGridMapper>`로 읽는다.
- **시험이 리플렉션으로 읽는 Presenter 필드는 이름을 유지한다:** `_attacks`(`CombatAttackPlayback`), `_reaction`(`CombatUnitReactionSet`), `_motion`(`CombatUnitMotionSet`), `_facts`, `_sound`. 읽는 시험: `CombatBallistaAttackTests`, `CombatRailwayContactSceneTests`, `CombatSceneEventPresenterTests`.
- **바꾸지 않는 것:** Presenter 생성자 서명. `CombatSceneDisplayFactory`는 A-3b·A-5에서만, `CombatViewSystem`은 A-4b에서만 이 계획에 적은 줄을 바꾼다. 그 밖에 바꿔야 하면 멈추고 묻는다.
- **공용 파일:** Factory·ServiceKeys·DB Factory·`ViewGroup.asset`을 바꾸면(A-3b·A-5) 필요한 줄만 바꾸고 민형님 공유 문서에 절을 더한다(인계 §4.5). 다른 작업자가 만든 시험을 고치면(A-4의 `FrenzyViewContractTests`, 천명님) Core 전달 문서 §5에 적는다.
- **구현 방식:** 분리 S-1~S-5처럼 직접 구현한다. 실습 워크시트가 필요하면 Phase 착수 때 말해 주면 그 Phase를 워크시트로 준다.
- **Phase마다 확인(Q-R12 A):**
  - MSBuild 오류 0, 새 경고 0(분리 계획 §8.3의 고유 경고 16개 대비)
  - 관리 실행 메서드 단위 회귀 0(분리 계획 §7). 실행 뒤 `Assets/__TornadoLegacy_*.asset` 정리
  - 새 관리 실행 시험 PASS
  - 검토 에이전트 1개(동작 동일성 + 설계 대조)
  - **Unity Test Runner 묶음 결과를 받은 뒤 다음 Phase로 간다.** 멈추고 보고한다.

## 2. A-1 — 시작 조건 하나 (B11)

### 2.1 새 파일

- `C/CombatStartQuery.cs`
  - `enum CombatStartProfile { Query, Start, Result, ConcurrentDeaths }`
  - `enum CombatStartBlock { None, Unavailable, AttackGate, AttackBusy, AbilityBusy, Reaction, StepLane, StepGate, Waiting }`
  - `readonly struct CombatStartQuery`
    - 만들기: `Query(item)`, `Start(item)`, `Result()`, `ConcurrentDeaths()`. 사건이 필요한 프로필은 null이면 `ArgumentNullException`.
    - 파생 읽기: `Profile`, `Item`, `AllowsOverlap`(Query·Start = true), `AllowsDeaths`(ConcurrentDeaths = true).

### 2.2 Presenter 변경 (`C/CombatSceneEventPresenter.cs`)

- **새 private 멤버:**
  - `FindBlock(in CombatStartQuery query)`: 아래 도우미를 공격 → 능력 → 걸음·대기 → 반응 순서로 보고 첫 이유를 돌려준다.
  - 도우미 넷: `AttackBlock`, `AbilityBlock`, `StepBlock`, `ReactionBlock`. 본문은 설계 §1.2 표의 재생기 행을 **그대로** 옮긴다.

| 도우미 | Query | Start | Result · ConcurrentDeaths |
|---|---|---|---|
| `AttackBlock` | `_attacks.IsUnavailable` → Unavailable · `!_attacks.CanBegin(item)` → AttackGate | `_attacks.BlocksNextEvent(true)` → AttackBusy(사라짐이면 Unavailable) | `_attacks.BlocksNextEvent(false)` 같은 방식 |
| `AbilityBlock` | `_abilities.IsUnavailable` → Unavailable · `IsPlaying && !_attacks.AllowsOverlap` → AbilityBusy | Query와 같음 | 사라짐 · `IsPlaying` |
| `StepBlock` | `MotionBlocks(item, true)` → StepLane · `!_motion.CanBegin(item)` → StepGate | `MotionBlocks(item, true)` → StepLane · `_wait.HasEvent` → Waiting | `MotionBlocks(null, false)` → StepLane · `_wait.HasEvent` → Waiting |
| `ReactionBlock` | `_reactionUnavailable` → Unavailable · `ReactionBlocks(item)` → Reaction | Query와 같음 | Result: 불가 · `_reaction.IsPlaying` / ConcurrentDeaths: 불가 · `!_reaction.CanOverlapDeaths` |

- **바꿀 멤버(본문만):**
  - `CanBeginPresentationEvent(item)` = `FindBlock(Query(item)) == None`
  - `RequireIdle(...)` → `RequireStart(in query)`: 막히면 `InvalidOperationException("[CombatSceneEventPresenter] 이전 사건 연출이 아직 끝나지 않았습니다. 막는 이유: {block}")`
  - `RequireIdleFor(movement)`: Query 실패 시 같은 문구(이유 덧붙임) → `RequireStart(Start(movement))`
  - `BeginEvent`: Query 실패 시 지금 문구 "…접촉 또는 남은 공격 표시를 먼저 기다려야 합니다." + 이유 → `RequireStart(Start(e))`
  - `BeginPresentation`: `RequireStart(Result())`. `BeginConcurrentDeath`: `RequireStart(ConcurrentDeaths())`
- `ReactionBlocks`·`MotionBlocks`는 도우미가 쓰는 private 함수로 남긴다(A-2에서 재생기로 옮김).
- `Refresh` 단정은 시작 조건이 아니라 A-2의 `BlocksFinalFacts`로 간다. A-1에서는 그대로 둔다.

### 2.3 시험

- 새 `T/CombatStartQueryTests.cs`(관리 실행): 프로필별 `AllowsOverlap`·`AllowsDeaths`, 사건 필요 프로필의 null 거절, 기본값.
- 바꿀 시험: 없음(예외 문구를 확인하는 시험이 없음, 2026-10-18 검색).
- **Unity 확인 묶음(재생 묶음):** `CombatSceneEventPresenterTests`, `CombatEnemyPresentationTests`, `CombatUnitDeathTests`, `CombatUnitReactionTests`, `CombatUnitReactionSetTests`, `CombatRailwayContactSceneTests`, `CombatMovementViewTests`, `CombatBallistaAttackTests`, `CombatMortarPresentationTests`, `CombatStandardAttackTests`, `CombatCodeMotionHitTests`.

## 3. A-2 — 재생기 계약과 실행 목록 (B12, 보완 1~3)

### 3.1 새 파일 (`C/`)

| 파일 | 내용 |
|---|---|
| `ICombatPresentationPlayer.cs` | 계약: `BeginResult`, `FindBlock(in CombatStartQuery)`, `BeginFrame(in CombatPlaybackFrame)`, `Tick(in CombatPlaybackFrame)` → bool, `IsComplete`, `BlocksFinalFacts`, `Cancel`, `Release`. 기본 구현 없음 |
| `CombatPlaybackFrame.cs` | readonly struct: `DeltaTime`, `MapperBound` |
| `CombatPresentationPlayers.cs` | 실행 목록. `Create(attack, ability, step, reaction)`(보완 2, 자리 고정). `BeginResult`, `FindBlock`(첫 이유), `Tick`(모두 `BeginFrame` → 순서대로 `Tick`, false에서 멈춤), `IsComplete`(모두), `BlocksFinalFacts`(하나라도), `CancelAll`·`ReleaseAll`(순서대로, `CleanupErrors`로 모음) |
| `ICombatWaitSlot.cs` | 대기 칸 계약(보완 1): `StartGap(seconds)`, `HoldAfterGap(e, completed, shownBeforeGap)`, `HoldShown(e, completed, seconds)` |
| `ICombatSourceOverlap.cs` | 출처 겹침 계약(보완 3): `AllowsOverlap`(지금 값), `AllowedAtFrameStart`(공격 재생기가 기록한 값) |
| `CombatAttackPlayer.cs` | `CombatAttackPlayback` 감쌈 + `ICombatSourceOverlap` 구현 |
| `CombatAbilityPlayer.cs` | `CombatAbilityMotionPlayback` 감쌈. `ICombatSourceOverlap`을 받음 |
| `CombatStepPlayer.cs` | `CombatUnitMotionSet` + `CombatEventWait` 소유, `ICombatWaitSlot` 구현. `Action<CombatEvent, Action>`(Presenter 틀), `Func<int>`(세대), `Func<FieldGridMapper>`를 받음 |
| `CombatReactionPlayer.cs` | `CombatUnitReactionSet` 감쌈 + 반응 불가 상태·`CancelUnavailable` 소유 |

### 3.2 재생기별로 옮길 것 (Presenter → 재생기)

| 재생기 | `FindBlock` | `BeginFrame` | `Tick` | `IsComplete` | `BlocksFinalFacts` | `Cancel` / `Release` |
|---|---|---|---|---|---|---|
| 공격 | A-1 `AttackBlock` | `AllowedAtFrameStart = attacks.AllowsOverlap` | `attacks.Tick(dt, MapperBound)` | `attacks.IsComplete` | `IsPlaying \|\| IsUnavailable` | `attacks.Cancel` / `attacks.Release` |
| 능력 | A-1 `AbilityBlock`(출처는 `ICombatSourceOverlap.AllowsOverlap`) | `_waitingAtFrameStart = abilities.IsWaitingForAttack` | 진행·사라짐이 아니면 true. 아니면 `!_waitingAtFrameStart`일 때 `abilities.Tick(dt)`, `return source.AllowedAtFrameStart` | `!IsPlaying && !IsUnavailable` | `IsPlaying \|\| IsUnavailable` | `abilities.Cancel` / `abilities.Release` |
| 걸음·대기 | A-1 `StepBlock` + `MotionBlocks` | — | 레인·대기가 없으면 true. 아니면 세대 기록 → 지금 `TickMotion` 본문 → 세대가 같으면 true | `!motion.IsActive` | false | `motion.Cancel` → `wait.Clear` / 없음 |
| 반응 | A-1 `ReactionBlock` + `ReactionBlocks` | 불가면 지금 문구로 예외 → `_versionAtFrameStart = reaction.Version` | `reaction.IsPlaying`이면 `reaction.Tick(dt, _versionAtFrameStart)`, true | `!IsPlaying && !_unavailable` | `IsPlaying \|\| _unavailable` | `_unavailable = false` → `reaction.Cancel` / `reaction.Release` |

- 걸음·대기 재생기는 Presenter의 `StartGap`(레인 진행 중이면 예외 + `_wait.StartGap`)과 `TickMotion`을 가져간다. `TickMotion` 첫 줄의 Mapper 확인(`RequireBound`)도 같은 문구로 옮긴다. 꺼낸 대기 사건은 받은 콜백으로 Presenter `PresentEvent`에 넘긴다.
- 예외 문구는 지금 문구를 그대로 쓴다(`[CombatSceneEventPresenter] …`). 위치만 재생기로 옮겨도 로그로 찾는 말이 바뀌지 않게 한다.
- 반응 재생기는 `_reactionUnavailable`과 `CancelUnavailableReaction`을 가져간다.

### 3.3 부품 변경

- `C/CombatAbilityMotionPlayback.cs`: 읽기 속성 `IsWaitingForAttack`(= 상태가 `RoarAttack`)만 더한다. 다른 동작은 바꾸지 않는다.

### 3.4 Presenter 변경

- **생성자(조립 한 곳):** 부품 → 재생기 넷 → `CombatPresentationPlayers.Create(attack, ability, step, reaction)`.
  - `_waitSlot`(= 걸음·대기 재생기의 `ICombatWaitSlot`)를 보관한다.
  - 캐릭터 처리기에는 반응 재생기의 `CancelUnavailable`을 넘긴다.
  - 이동 표시(`CombatMovementPresentation`) 생성자의 `Action<float> startGap`을 `ICombatWaitSlot`으로 바꾼다.
- **멤버 본문:**
  - `BeginPresentation` = `RequireStart(Result())` → `_players.BeginResult(result)` → `_pacing.BeginResult(result)`
  - `IsPresentationComplete` = `_players.IsComplete`
  - `CanBeginPresentationEvent` = `_players.FindBlock(Query(item)) == None`
  - `RequireStart` = `_players.FindBlock(query)`
  - `TickPresentation(dt)` = `_players.Tick(new CombatPlaybackFrame(dt, _mapper != null))`
  - `Refresh` 단정 = `_players.BlocksFinalFacts`
  - `CancelMovement` = `_presentationVersion++` → `RunCleanup(_players.CancelAll, _pacing.Clear, 회전 정리, _highlight.Clear)`
  - `Release` = `RunCleanup(Clear, _players.ReleaseAll)`
  - `BeginEvent`의 간격·대기 = `_waitSlot.StartGap` / `HoldAfterGap`
  - `PresentEvent`의 머묾 = `_waitSlot.HoldShown`
- **없어지는 Presenter 멤버:** A-1 도우미 넷, `ReactionBlocks`, `MotionBlocks`, `StartGap`, `TickMotion`, `CancelUnavailableReaction`, 필드 `_wait`·`_reactionUnavailable`.
- **남는 필드:** `_attacks`·`_reaction`·`_motion`(시험 리플렉션), `_abilities`(처리기 조립), `_facts`·`_sound`·`_highlight`·`_handlers`·`_movement`·`_pacing`.

### 3.5 시험

- 새 `T/CombatPresentationPlayersTests.cs`(관리 실행, 가짜 재생기):
  - `Create` 자리 순서대로 `BeginFrame`·`Tick`·`FindBlock`·`CancelAll`이 불림
  - 모든 `BeginFrame`이 첫 `Tick`보다 먼저
  - `Tick` false에서 뒤 재생기 멈춤, true면 계속
  - `FindBlock` 첫 이유, 모두 None이면 None
  - `IsComplete`는 모두, `BlocksFinalFacts`는 하나라도
  - `CancelAll`은 한 재생기가 던져도 나머지를 정리하고 끝에 모아 던짐. `ReleaseAll`도 같음
  - null 재생기 거절
- 새 `T/CombatAbilityPlayerTests.cs`(관리 실행, 실제 `CombatAbilityMotionPlayback` + 가짜 자세·크기·출처):
  - 프레임 시작에 공격 대기면 그 프레임 Tick 없음, 같은 프레임에 재개돼도 없음(O4)
  - `Tick` 반환 = 기록된 프레임 시작 출처 겹침(O3)
  - `FindBlock`: 사라짐, 진행 중 + 프로필 겹침 × 출처 겹침
- 새 `T/CombatReactionPlayerTests.cs`(관리 실행 가능 여부를 착수 때 확인. 반응 시작에 `ICombatUnitPose` 가짜와 `CombatUnitReactionPresentationData`가 필요):
  - 프로필별 `FindBlock`(Query·Start 주체 기준, Result 전체, ConcurrentDeaths 사망끼리)
  - 불가면 `BeginFrame` 예외, 프레임 시작 Version 뒤 시작한 반응은 시간 0(O12·O13)
- `T/CombatAbilityMotionPlaybackTests.cs`: `IsWaitingForAttack` 시험 추가(대기 전·공격 중·재개 뒤·취소 뒤).
- 바꿀 시험: 없음 예상(리플렉션 필드 유지, 이동 표시는 Presenter 안에서만 생성).
- **Unity 확인 묶음:** 재생 묶음(§2.3) + `CombatPresentationCleanupTests`의 Unity 쪽 + 포효·소환 시험(보스 활성화 묶음).

## 4. A-2b — 질의·단정 정렬 (§9 I-1, Q-R14 A)

- **바꿀 것:**
  - `CombatStartProfile`에서 `Query`를 없앤다. 프로필은 `Start`·`Result`·`ConcurrentDeaths` 셋이다.
  - 재생기별 `FindBlock`의 `Start` 행을 A-2의 Query 행 ∪ Start 행으로 맞춘다.

| 재생기 | 시작(합집합) |
|---|---|
| 공격 | 사라짐 · Gate(`CanBegin(item)`) · 히트 전 공격 중·재장전(`BlocksNextEvent(true)`) |
| 능력 | 그대로(두 행이 같았음) |
| 걸음·대기 | 레인 자원 · 걸음 Gate · 대기 사건 |
| 반응 | 그대로(두 행이 같았음) |

  - Presenter: `CanBeginPresentationEvent` = `_players.FindBlock(Start(item)) == None`. `BeginEvent`·`RequireIdleFor`는 한 번만 확인하고, 막히면 각자 지금 문구에 이유를 붙여 던진다.
- **동작:** 화면 같음. 스케줄러가 대기 사건·재장전 중에 묻는 경로(지금 도달하지 않음)에서 "시작 단정 예외" 대신 "기다림"이 된다.
- **시험:** `CombatStartQueryTests`(Query 프로필 제거), 능력·반응 재생기 시험의 프로필 정리. 공격·걸음 재생기의 합집합은 재생 묶음(Unity)으로 확인한다.
- **Unity 확인 묶음:** A-2와 같음.

## 5. A-3a — 유닛 표시 자원 창구 (B13, 보완 4)

### 5.1 착수 직전 API 확정

창구·표시 계약의 서명을 착수 때 코드와 맞춰 확정하고 이 절에 적은 뒤 구현한다. 아래는 설계 B13의 채널을 옮기는 기준안이다.

**확정(2026-10-18, 착수 직전 코드 대조).** 기준은 "지금과 같은 동작"이다. 설계 B13의 가칭과 다른 곳은 이유를 적는다.

- **창구를 만드는 곳(계획 보정):** `FieldUnitPresenter.Presentation`(읽기 속성)이 처음 불릴 때 한 번 만든다. `Configure`가 아니다.
  - 이유: 시험 셋(`FrenzyViewContractTests`, `CombatMovementViewTests`의 탐침, `CombatSceneEventPresenterTests`)이 `Configure` 없이 사건 Presenter를 만든다. Factory는 `Configure` 전에 `CombatMovementPlanningPresenter`를 만든다. 만드는 곳이 한 곳이고 Factory를 바꾸지 않는다는 목표는 같다.
- **표시 하나 `ICombatUnitPresentationTarget`(`FieldUnitView`가 명시적 구현):**
  - `EntityId`, `EntityKey`, `IsActive`(= `isActiveAndEnabled`), `HasPose`, `Rotation`
  - `BeginPose(cancelled)`, `BeginScale(cancelled)`: 지금 자세·크기 대여 그대로. 표시가 꺼지거나 다시 대여되면 자세 먼저 크기와 함께 취소되는 경로(`Bind`·`OnDisable`)도 `FieldUnitView`에 그대로 남는다.
  - `SetPlacementPosition(position)`, `ApplyFacing(rotation)`, `SetMovementOpacity(opacity)`·`ClearMovementOpacity()`
  - `BallistaWeapon`·`MortarWeapon`(붙은 컴포넌트, 없으면 null)
- **표시 찾기 `ICombatUnitTargets`(`FieldUnitPresenter`가 명시적 구현):**
  - `TryGet(id, out target)`: 지금 대여 중인 표시. 켜짐과 무관하다. Unity null 확인은 구현 안에서 한다.
  - `IsRented(id, target)`·`ReleaseRental(id, target)`: 사망 유지가 쓴다.
  - `AttackSettingsOf(key)`·`ReactionSettingsOf(key)`: 외형 DB 읽기.
  - `FactPosition(facts, mapper)`·`FactRotation(facts, mapper)`: 사실을 그릴 때의 위치·회전(`CenterOf`·`ModelRotation`). 창구가 `FieldUnitPresenter`의 정적 함수를 직접 부르지 않게 한다.
- **대여 계약 셋:**
  - `ICombatUnitPlacement`: `IsCurrent`, `Interpolate(from, to, mapper, progress)`, `Restore()`
  - `ICombatUnitDeathHold`: `IsCurrent`, `Release()`
  - `ICombatUnitAppearance`: `IsCurrent`, `Restore()`
  - 계획 §5.2의 네 파일에 더해 대여 계약 두 파일(`ICombatUnitDeathHold`·`ICombatUnitAppearance`)이 생긴다.
- **창구 `CombatUnitPresentationWindow` 서명:**

| 채널 | 서명 | 지금과 같게 한 점 |
|---|---|---|
| 자세(공격) | `BeginAttackPose(id, cancelled = null)` | 표시가 없으면 예외, 꺼진 표시는 대여가 예외(지금처럼) |
| 자세(반응) | `BeginPose(id, cancelled)` | 반응 설정을 읽은 직후라 표시가 있다. 없으면 예외 |
| 자세(소환 등장) | `TryBeginPose(id, cancelled, out pose)` | 켜진 표시만, 없으면 false |
| 크기 | `TryBeginScale(id, cancelled, out scale)` | 켜진 표시만, 없으면 false |
| 걸음 배치 | `BeginPlacement(id)` | 지금처럼 표시 없이도 시작한다(그래서 `Try`가 아님). 같은 유닛의 새 대여가 이전 대여를 대신한다(레인이 대여 하나만 가짐). `Interpolate`는 지금처럼 그 유닛의 표시가 있으면 위치를 쓴다 |
| 자세 ↔ 배치 | 배치가 있으면 자세 시작이 예외, 자세가 있으면 배치 시작이 예외 | Q-R11 A, 지금 도달하지 않음 |
| 표시 회전 | `TryGetRotation`·`SetFacing`·`ClearFacing(facts, mapper)`·`ClearAllFacing(facts, mapper)` | `TryGetRotation`이 표시가 없으면 그 유닛의 표시 회전을 지우는 지금 부수 효과 유지 |
| 사망 유지 | `HoldDeath(id)` | 지금처럼 켜진 표시가 없거나 이미 유지 중이면 예외(그래서 `Try`가 아님) |
| 이동 투명도 | `BeginMovementOpacity(id, opacity)` | 지금처럼 전투에 하나, 새로 시작하면 앞의 것을 끝냄, 켜진 표시가 없으면 예외. 층 인자는 두 번째 층(플래시, B-4)이 생길 때 더한다 |
| 연출 값 | `TryGetAttackPresentation`·`TryGetMortarPresentation`·`GetBallistaWeapon`·`TryGetBallistaWeapon`·`TryGetReactionSettings` | 같은 이름·같은 조건. 반응 설정만 켜진 표시를 요구한다(지금처럼) |
| 소유 사실(`RenderFacts`용) | `HoldsDeath(id)`·`KeepsPlacement(id)`·`TryGetFacing(id, out facing)`·`ForgetFacing(id)`·`EndMovementOpacityFor(id)` | O19 |
| 수명(`FieldUnitPresenter`용) | `OnReturned(id)`(풀 반환), `EndMovementOpacity()`·`Reset()`(`Release`) | 반환이 회전·투명도 주인을 지우는 지금 동작 |

- **호출자 연결 보정(코드 대조):**
  - 걸음 레인 집합·레인은 창구 + 이동자 몸 조회 함수(`FieldUnitPresenter.BodyOf`)를 받는다. 몸 조회는 이 Phase에서 `FieldUnitPresenter`에 남기 때문이다(§5.4).
  - 캐릭터 처리기는 HUD 피해 숫자(`ShowHudDamage`) 때문에 `FieldUnitPresenter`를 A-3b까지 함께 받는다.
  - `CombatMovementPlanningPresenter`는 생성자 서명을 그대로 두고(Factory 변경 없음) 쓸 때 `units.Presentation`을 읽는다.
  - `FieldUnitView`의 `BeginAttackPose`·`BeginReactionPose`는 남는다(`CombatUnitReactionTests`·`CombatUnitDeathTests`가 표시를 직접 시험). 쓰는 곳이 없어지는 `BeginMotionPose`는 없앤다.
- **바꿀 시험(코드 대조로 확정):** 아래는 모두 호출 대상만 `units.Presentation.…`으로 바꾼다.
  - `BeginAttackPose`: `CombatBallistaAttackTests`, `CombatMortarPresentationTests`(2곳), `CombatStandardAttackTests`(2곳)
  - `HoldDeath`: `CombatMovementPlanningDisplayTests`, `CombatUnitDeathTests`
  - `SetFacing`: `AttackHealthPreviewDisplayTests`, `CombatEnemyPresentationTests`(4곳), `CombatMortarPresentationTests`, `CombatMovementPlanningDisplayTests`
  - §5.3 표의 `CombatUnitReactionTests`는 `FieldUnitView`를 직접 불러 바꾸지 않는다.

### 5.2 새 파일 (`D/`)

| 파일 | 내용 |
|---|---|
| `ICombatUnitPresentationTarget.cs` | 표시 하나: 활성 여부, 자세 대여(`BeginPose`), 크기 대여(`BeginScale`), 배치 위치 쓰기, 현재·표시 회전, 외형 층(이동 투명도). `FieldUnitView`가 구현 |
| `ICombatUnitTargets.cs` | 표시 찾기: `TryGet(id, out target)`, 사망 유지 대여 확인·닫기(`IsRented`·`ReleaseRental`), 연출 값·사실 배치 읽기. `FieldUnitPresenter`가 구현. 켜진 표시 찾기는 창구 안 도우미(§5.1 확정) |
| `CombatUnitPresentationWindow.cs` | 창구. 채널 대여·소유 사실·연출 값 읽기 |
| `ICombatUnitPlacement.cs` | 걸음 배치 대여: `Interpolate(from, to, mapper, progress)`, `Restore`(= 소유 끝) |

### 5.3 창구로 옮길 것 (`D/FieldUnitPresenter.cs` → 창구)

| 지금 `FieldUnitPresenter` 멤버 | 창구 뒤 | 지금 호출자 | 지금 그 멤버를 부르는 시험 |
|---|---|---|---|
| `BeginAttackPose` | `BeginAttackPose(id, cancelled)` | `CombatAttackPlayback` | `CombatBallistaAttackTests`, `CombatMortarPresentationTests`, `CombatStandardAttackTests`, `CombatUnitReactionTests` |
| `TryBeginMotionPose` | `TryBeginPose(id, cancelled, out pose)` | 소환 처리기 | — |
| `TryBeginPresentationScale` | `TryBeginScale(id, cancelled, out scale)` | 포효 처리기 | — |
| `TryGetReactionPresentation`(View를 돌려줌) | `TryGetReactionSettings(id, out settings)` + `TryBeginPose` | 캐릭터 처리기 | — |
| `HoldDeathPresentation(view)` · `DeathPresentationHold` | `HoldDeath(id)`(§5.1 확정, 지금처럼 예외) | 캐릭터 처리기 | `CombatMovementPlanningDisplayTests`, `CombatUnitDeathTests` |
| `BeginStepPlacement`·`EndStepPlacement`·`Interpolate` | `BeginPlacement(id)`(§5.1 확정, 표시 없이도 시작) + 대여의 `Interpolate`·`Restore` | `CombatUnitMotionPlayback` | — |
| `TryGetCurrentRotation`·`SetPresentationRotation`·`ClearPresentationRotation(s)` | `TryGetRotation`·`SetFacing`·`ClearFacing`·`ClearAllFacing` | `CombatUnitMotionPlayback`, `CombatMovementPresentation`, Presenter `CancelMovement` | `AttackHealthPreviewDisplayTests`, `CombatEnemyPresentationTests`, `CombatMortarPresentationTests`, `CombatMovementPlanningDisplayTests` |
| `TryGetAttackPresentation`·`TryGetMortarPresentation`·`GetBallistaWeapon`·`TryGetBallistaWeapon` | 같은 이름(대여 없는 읽기) | `CombatAttackPlayback` | — |
| `SetMovementOpacity`·`ClearMovementOpacity` | 외형 층 대여(이동 투명도) | `CombatMovementPlanningPresenter` | — |

- 창구는 지금 `FieldUnitPresenter`의 세 표(`_steppingActors`·`_deathHolds`·`_presentationRotations`)를 가져간다.
- `RenderFacts`는 세 표 대신 창구에 소유 사실(배치 유지, 회전 덮기)을 묻는다(O19).
- 같은 유닛에 자세 대여가 있으면 배치 대여를 거절하고, 반대도 거절한다(Q-R11 A). 지금 도달하지 않는 조합이다.
- **지킬 성질(A-2 검토):** 표시가 꺼지거나 다시 대여될 때 그 유닛의 자세 대여와 크기 대여가 **함께, 자세 먼저** 취소된다(`FieldUnitView`의 Bind·`OnDisable`). 설계 B14 O4 동등성의 "표시가 사라지면 공격 재생기가 먼저 던진다" 근거가 이 성질이다. 창구로 대여 취소 경로를 옮길 때 그대로 유지하고, 창구 시험에 넣는다.
- `FieldUnitPresenter`에는 `Presentation`(창구) 읽기 속성을 둔다. 창구를 만드는 곳은 그 속성 한 곳이다(Factory 변경 없음, `Configure`가 아닌 이유는 §5.1 확정).

### 5.4 호출자 다시 연결

- `CombatAttackPlayback`, `CombatUnitMotionPlayback`, `CombatUnitMotionSet`, `CombatMovementPresentation`, 소환·포효·캐릭터 처리기, `CombatMovementPlanningPresenter`는 필요한 채널만 창구에서 받는다.
- 사실 그리기·몸 조회(`RenderFacts`·`BodyOf`)와 HUD는 이 Phase에서 `FieldUnitPresenter`에 남는다. HUD는 A-3b에서 뺀다.
- 생성자 서명이 바뀌는 내부 클래스(`CombatAttackPlayback`, `CombatUnitMotionSet` 등)를 직접 만드는 시험이 있으면 착수 때 목록을 만들어 이 절에 적는다.

### 5.5 시험

- 새 `T/CombatUnitPresentationWindowTests.cs`(관리 실행, 가짜 표시):
  - 자세 유닛당 하나, 크기는 자세와 독립
  - 자세 ↔ 배치 배타(Q-R11 A)
  - 배치 대여 중 소유 사실 = 배치 유지, 반납 뒤 해제
  - 표시 회전이 소유 사실의 회전 덮기가 됨, 지우면 해제
  - 사망 유지 중 배치 유지, 해제는 한 번만
  - 표시가 꺼지면 `cancelled` → 이후 `ApplyPose` 예외(지금 규칙)
- 바꿀 시험: §5.3 표의 "시험" 열(호출 대상을 `units.Presentation.…`으로).
- **Unity 확인 묶음:** 재생 묶음 + `CombatMovementPlanningDisplayTests`, `AttackHealthPreviewDisplayTests`, 배치 미리보기 시험, `CombatUnitDeathTests`(사망 유지).

## 6. A-3b — HUD 피드백 분리 (§9 I-2, Q-R15 A)

- **새 파일(`D/`):**
  - `CombatUnitHudFeedback.cs`: 유닛 HUD의 일시 피드백. A-3a의 `ICombatUnitTargets`로 표시를 찾는다.
  - HUD 대상 계약(가칭 `ICombatUnitHudTarget`, `FieldUnitView` 구현): 피해 숫자, 비용 미리보기, 체력 미리보기. 서명은 착수 때 확정한다.
- **옮길 것(`FieldUnitPresenter` → HUD 피드백):** `ShowHudDamage`, `ShowHudCostPreview`, `ClearHudCostPreview`, `ShowAttackHealthPreview`, `ClearAttackHealthPreview`와 그 상태.
- **`FieldUnitPresenter`에 남는 것:** `Configure`, 표시 집합, `Render`·`RenderFacts`(사실에서 파생하는 HUD 상태 칸·타임라인·목표 표시 포함: `SetHudTimeline`·`SetObjectiveTarget`), 숨김(`SetHiddenEntity`), 몸 조회, `Release`, 창구·HUD 피드백 소유.
  - `FieldUnitView.Apply`가 체력 미리보기를 지우는 지금 동작은 그대로다(사실 그리기가 미리보기를 끝냄).
- **호출자:** 캐릭터 처리기(`ShowHudDamage`), `CombatAttackFeedback`, `CombatMovementPlanningPresenter`, `CombatSceneDisplay`, `AttackTargetingInteraction`. Factory는 이들에게 `units` 대신 HUD 피드백을 넘긴다(`CombatSceneDisplayFactory`, 공유 문서 절 추가).
- **바꿀 시험:** `AttackHealthPreviewDisplayTests`, `CombatPlacementFeedbackTests`, `CombatAttackFeedbackTests`, `CombatMovementDraftDisplayTests`, `CombatPointerMappingTests`(호출 대상만).
- **동작:** 없음.
- **확정(2026-10-18, 착수 직전 코드 대조):**
  - HUD 대상 계약 `ICombatUnitHudTarget`(`D/`, `FieldUnitView`가 명시적 구현): `IsActive`, `ShowDamage(damage)`, `SetCostPreview(cost)`(0이면 지움), `SetHealthPreview(health)`(null이면 지움, 체력 숫자 포함). HUD가 없는 표시는 아무 일도 하지 않는다(지금처럼 `view.Hud != null` 확인이 표시 안으로 들어감).
  - 찾기: `ICombatUnitTargets`에 `TryGetHud(id, out target)`를 더한다(지금처럼 대여 중인 표시, 켜짐과 무관).
  - `CombatUnitHudFeedback`(`D/`): `ShowDamage(id, damage)`(켜진 표시·양수만), `ShowCostPreview(actorId, cost)`·`ClearCostPreview()`(대상 하나), `ShowAttackHealthPreview(targets)`·`ClearAttackHealthPreview()`(대상 여럿), `OnReturned(id)`(풀 반환이 체력 미리보기 대상에서 뺌, 비용 대상은 지금처럼 그대로).
  - 만드는 곳: `FieldUnitPresenter.HudFeedback`(읽기 속성)이 처음 읽힐 때 한 번. 이름은 표시의 `FieldUnitView.Hud`(UnitHUD)와 헷갈리지 않게 했다. `RenderFacts` 첫머리의 체력 미리보기 지우기, 풀 반환, `Release`의 두 지우기는 같은 자리에서 HUD 피드백을 부른다.
  - 호출자(코드 대조로 확정):
    - 캐릭터 처리기는 `FieldUnitPresenter` 대신 HUD 피드백을 받는다(A-3a에서 남겨 둔 것).
    - `CombatAttackFeedback(field, hud)`.
    - `CombatMovementPlanningPresenter(field, hud, presentation, ghost, config)`: 비용 미리보기와 이동 투명도(창구)를 함께 쓰므로 둘을 받는다.
    - `CombatSceneDisplay`는 사실 그리기·타임라인·해제 때문에 `units`를 계속 받고, 정리 목록의 두 지우기만 `units.HudFeedback`으로 부른다(생성자 그대로).
    - `AttackTargetingInteraction`은 표시 계약(`ICombatInteractionFeedback`)만 불러 바뀌지 않는다.
    - Factory는 `CombatAttackFeedback`·`CombatMovementPlanningPresenter` 두 줄만 바꾼다(민형님 공유 문서에 절 추가).
  - 설계 §9 I-2는 타임라인·목표 표시도 HUD 피드백에 넣는다고 적었지만, 이 계획대로 `FieldUnitPresenter`에 남긴다. 둘은 사실 그리기(`RenderUnitHud`)가 사실에서 파생해 매번 다시 쓰는 상태이고, 이 Phase가 옮기는 것은 일시 피드백이다.
  - 바꿀 시험(코드 대조로 확정): `AttackHealthPreviewDisplayTests`(호출과 리플렉션 필드), `CombatPlacementFeedbackTests`, `CombatAttackFeedbackTests`, `CombatMovementDraftDisplayTests`, `CombatMovementPlanningDisplayTests`(생성자), `CombatUnitPresentationWindowTests`(가짜 찾기에 `TryGetHud`). `CombatPointerMappingTests`는 표시 계약만 불러 바뀌지 않는다.
  - 새 관리 실행 시험 `CombatUnitHudFeedbackTests`(가짜 HUD 대상): 비용 대상 교체·지우기, 체력 미리보기 대상 추적·지우기·반환, 피해 숫자는 켜진 표시·양수만.
- **Unity 확인 묶음:** 위 바꿀 시험 + 재생 묶음의 피해 숫자 경로(`CombatUnitDeathTests` 등).

## 7. A-4 — 사건 경로 표 (§9 I-3)

- **새 파일:** `C/CombatEventRoutes.cs`
  - `enum CombatEventRoute { Skip, Step, Return, Railway, Event }`
  - `static RouteOf(CombatEvent e)`: 지금 판정 순서 그대로. `Moved` → Step, 필드 안 `Returned` → Return, 기차 발동(`CombatGimmickEvent` + `RailwayGimmickActivationSnapshot`) → Railway, 사건 틀 형식 → Event, 그 밖 → Skip.
  - `static EventTypes`: 사건 틀 형식 19개(지금 `CombatEventHandlerTable`의 목록을 옮김).
- **바꿀 것:**
  - `OrderedCombatPresentation`: `CanPresent`·`IsMovement` → `RouteOf`. 경고(Skip), 시작 분기(Step·Return·그 밖)를 경로로.
  - `CombatSceneEventPlayback`: `IsRailway` → `RouteOf(e) == Railway`. `CanPresent` 제거.
  - `ICombatEventPlayback`: `CanPresent` 제거 → `CombatSceneDisplay`·`CombatSceneEventPresenter`의 `CanPresent` 제거.
  - `CombatEventHandlerTable`: 형식 목록을 `CombatEventRoutes.EventTypes`에서 읽고, `Create`가 Event 경로 형식마다 처리기가 있는지 확인한다. `Contains`는 없앤다.
- **동작:** 없음.
- **시험:**
  - 새 `T/CombatEventRoutesTests.cs`(관리): 형식별 경로, 판정 순서(이동 캐릭터 사건은 Step, 기차 Gimmick은 Railway), 옛 `CanPresent` 19개와 Event 경로의 일치(지금 처리기 표 시험의 해당 시험을 옮김).
  - `CombatEventHandlerTableTests`: `Contains` 시험 정리, 처리기 누락 거절 시험 추가.
  - 가짜 재생 계약에서 `CanPresent` 제거: `OrderedCombatSchedulingTests.FakePlayback`, `CombatViewSystemTests.FakeDisplay`, `FrenzyViewContractTests.RecordingPlayback`(천명님 작성. 웨이브 사건 경고·미표시와 뒤 사건 전달이라는 시험 뜻은 그대로, `presenter.CanPresent` 주입 대신 경로 표 사용. Core 전달 문서 §5에 기록).
- **확정(2026-10-18, 착수 직전 코드 대조):**
  - `CombatEventRoutes.RouteOf`: 지금 세 곳의 판정을 이 순서로 합친다.
    1. `Moved` → Step
    2. 필드 안 `Returned` → Return(지금 스케줄러 `IsMovement`)
    3. 기차 발동 → Railway(지금 재생 조정 `IsRailway`)
    4. 사건 틀 형식 → Event(지금 처리기 표)
    5. 그 밖·null → Skip
    - 지금 건너뛰는 형식은 `CombatFrenzyWaveEvent`·`CombatItemEvent` 둘이다(사건 형식 21개 중 19개 표시).
  - `CombatEventRoutes.EventTypes`가 사건 틀 형식 19개의 유일한 원본이다. 처리기 표는 특별 처리기 다섯(공격·포효·소환·캐릭터·턴)을 맞추고, 나머지 사건 틀 형식에는 사실 처리기를 둔다. 처리기 표의 `FactTypes` 목록은 없어진다(같은 목록이 두 곳에 있지 않게).
    - 그래서 "사건 틀 형식마다 처리기가 있음"은 만드는 방식으로 보장된다.
    - `Create`가 확인하는 것은 특별 처리기의 형식이 사건 틀 형식인지다. 아니면 그 처리기는 쓰이지 않는다.
  - 스케줄러: 시작 분기도 경로로 한다. `Moved` 확인과 `IsMovement`를 Step·Return으로, 경고·건너뜀을 Skip으로 바꾼다.
  - 재생 조정: `IsRailway` → `RouteOf(e) == Railway`. `CanPresent`를 없앤다.
  - 재생 계약·Display·사건 Presenter: `CanPresent`를 없앤다.
  - 바꿀 시험(코드 대조로 확정, 계획 목록보다 넓음):
    - 가짜 재생 계약에서 `CanPresent` 제거:
      - `OrderedCombatSchedulingTests.FakePlayback`
      - `CombatViewSystemTests.FakeDisplay`와 이를 물려받아 `CanPresent`를 덮던 `CombatBallistaAttackTests`·`CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatMovementViewTests.Probe`
      - `FrenzyViewContractTests.RecordingPlayback`
    - `OrderedCombatSchedulingTests.UnsupportedEvents_AreSkipped_WithoutBlocking`: 가짜가 효과 사건을 미표시로 만들던 것을 진짜 미표시 형식(`CombatFrenzyWaveEvent`)으로 바꾼다. 시험 뜻은 같다.
    - `CombatSceneEventPresenterTests`의 `_display.CanPresent(e)` 단정 4곳: `CombatEventRoutes.RouteOf(e)`가 Event인지로 바꾼다.
    - `FrenzyViewContractTests`: `presenter.CanPresent`를 주입하려고 만들던 사건 Presenter와 GameObject를 없앤다(Core 전달 문서 §5에 기록).
    - **사용자 결정(2026-10-18): `CombatMovementViewTests.MissingNonMovementHandler_WarnsSkipsAndAcknowledges`를 삭제한다.**
      - 이 시험은 표시 형식(공격)을 가짜 표시로 미표시처럼 만들어 건너뛰기를 봤다. 경로 표 뒤에는 그 전제가 생길 수 없다.
      - 그 Rig에서 Core가 진짜 미표시 사건을 만들게 할 방법도 없다.
      - 경고·건너뜀·결과 확인은 `FrenzyViewContractTests`(진짜 웨이브 사건)와 `OrderedCombatSchedulingTests`가 지킨다.
      - `Probe.UnsupportedKind`도 함께 없앤다.
  - 처리기 표 시험: `Contains` 시험은 경로 표 시험으로 옮긴다. "처리기 누락 거절"은 만드는 방식으로 보장되므로, 사건 틀 형식마다 기본이 아닌 처리기가 나오고 미표시 형식은 기본 처리기임을 확인하는 시험으로 바꾼다.
- **Unity 확인 묶음:** `FrenzyViewContractTests`, `CombatRailwayContactSceneTests`, 재생 묶음.

## 8. A-4b — 재생 계약 사슬 줄이기 (§9 I-4, Q-R16 A)

- **바꿀 것:**
  - `ICombatBattleDisplay`: `ICombatEventPlayback` 상속을 빼고 `ICombatEventPlayback EventPlayback { get; }`을 더한다.
  - `CombatSceneEventPlayback`이 `ICombatEventPlayback`을 구현한다(지금은 같은 이름 멤버만 있음).
  - `CombatSceneDisplay`: 재생 위임 멤버 14개를 없애고 `EventPlayback => _eventPlayback`. 안에서 쓰던 것(`Refresh` 단정의 `IsPresentationComplete`)은 `_eventPlayback`을 직접 읽는다.
  - `CombatViewSystem.AttachDisplay`: `_presentation.Attach(display.EventPlayback)`.
- **동작:** 없음.
- **시험:** `CombatViewSystemTests.FakeDisplay`가 `EventPlayback`으로 가짜 재생 계약을 돌려준다. 호진님 `HojinCombatAttackDragIntegrationTests`는 표시 계약을 읽기만 해 영향 없음(2026-10-18 확인).
- **착수 때 볼 것(A-2b 검토, §12.3):** `CombatSceneEventPlayback`이 Presenter와 같은 시작 판단을 먼저 확인하고, 이유 없는 문구로 던진다. 이유를 붙일지, 확인을 한 곳으로 줄일지는 착수 때 묻는다(지금 범위 밖).
- **확정(2026-10-18, 착수 직전 코드 대조):**
  - 계획 보정: Display의 재생 위임 중 시작 계열 여섯(`BeginPresentation`·`BeginStep`·`BeginMovement`·`BeginMovementEnd`·`ShowReturn`·`BeginEvent`)에는 "해제 뒤 예외"(리팩토링 설계 D9) 확인이 있다. 위임만 지우면 이 확인이 사라지므로 "동작 없음"이 성립하지 않는다.
  - **사용자 결정(2026-10-18): D9 확인을 재생 조정으로 옮긴다.** `CombatSceneEventPlayback`이 해제 상태를 갖고(`Release()`로 표시), 같은 여섯 멤버의 맨 앞에서 같은 형식(`InvalidOperationException`)으로 던진다. `CombatSceneDisplay.Release`가 자기 해제 표시 직후에 부른다. 판단·Tick·취소는 지금처럼 확인하지 않는다. 예외 문구 앞머리만 `[CombatSceneEventPlayback]`으로 바뀐다.
  - **사용자 결정(2026-10-18): 위 "착수 때 볼 것"(재생 조정의 시작 판단 선확인과 이유 없는 문구)은 지금처럼 둔다.**
  - 지금 위임 멤버는 열하나다(`CanPresent`는 A-4에서 없앰). Display 안에서 쓰던 `Refresh` 단정의 `IsPresentationComplete`는 `_eventPlayback`을 직접 읽는다.
  - 재생 조정의 설명("해제 가드가 없으므로 연출에 직접 연결하지 않습니다", "해제 뒤 사건 시작 거절도 표시가 먼저 합니다")은 이 변경으로 바뀌므로 고친다.
  - 바꿀 시험(코드 대조로 확정):
    - `CombatViewSystemTests.FakeDisplay`: 재생 계약도 구현하고 `EventPlayback => this`를 돌려준다. 그래서 이를 물려받아 재생 멤버를 덮는 가짜들(Ballista·Mortar·Standard·이동 표시 `Probe`)과 연출 연결 단정(`LastAttachedPlayback`이 표시와 같음)은 그대로다.
    - Display의 재생 멤버를 직접 부르는 시험 61곳(`CombatSceneEventPresenterTests` 44, `CombatRailwayContactSceneTests` 15, `CombatMortarAssetTests` 2): 호출 대상만 `_display.EventPlayback.…`으로 바꾼다.
    - `HojinCombatAttackDragIntegrationTests`는 표시 계약의 집기 멤버만 읽어 영향이 없다(재확인).
- **Unity 확인 묶음:** `CombatViewSystemTests`, 재생 묶음.

## 9. A-5 — 전역 연출 값 원본 (§9 I-5, Q-R17 A)

- **새 View DB 설정 표**(`StageFieldView`와 같은 모양, 항목 정확히 하나):
  - `DB/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs`(+ Data 형식), `DB/Data/View/Table/CombatPresentationSettings/CombatPresentationSettingsDataTableSO.cs`
  - `DB/Queries/ViewDataBase/ViewTable/CombatPresentationSettingsTable.cs`(+ query 인터페이스), `DB/Queries/ViewDataBase/ViewDataBase.cs` 속성, ServiceKeys의 `ViewTableKey`, `Factory/DBFactory/Group/ViewDataBaseFactory.cs` 등록
  - 에셋: `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentationSettings/`(표·항목), `ViewGroup.asset` 등록. **코드와 한 커밋**(없으면 DB 생성 실패)
- **옮길 값(`CombatSceneRoot` → 설정 표):** 걸음 시간(`movementStepSeconds`), 회전 시간(`movementTurnSeconds`), 적 행동·턴 간격(`enemyActionGapSeconds`·`enemyTurnGapSeconds`), 소환 등장 4개(`summonRise…`), 포효 3개(`roar…`). 값은 지금 `Combat.unity`의 직렬화 값 그대로 옮긴다(착수 때 씬 값을 읽어 계획에 적음). 입력·배치 값(`innerCellInset`·`cursorSnapThreshold`)은 씬에 남긴다.
- **검증:** `ValidatePresentationTiming`·`ValidateStepSeconds`의 조건을 설정 Data 쪽(`GetData` 또는 표 검증) 한 곳으로 옮긴다(구조 개선 SV1의 "조건 원본 하나" 유지).
- **연결:** `CombatDisplayTables`에 설정 값을 더하고, `CombatSceneDisplayFactory`가 SceneRoot 대신 설정에서 읽는다. `CreateAbilityMotionSettings`는 설정 Data에서 만든다.
- **동작:** 없음(같은 값).
- **시험:** `ViewDataTableTests`(설정 표 항목 정확히 하나, 값 범위 거절), `DataBaseAssetTests`(실제 에셋). SceneRoot 검증을 직접 부르는 시험이 있으면 착수 때 목록을 만들어 설정 시험으로 옮긴다.
- **공유:** ServiceKeys·DB Factory·`ViewGroup.asset` 변경은 민형님 공유 문서에 절 추가. 값 조정 위치가 씬에서 DB 에셋으로 바뀌는 것을 팀에 알린다.
- **확정(2026-10-18, 착수 직전 코드 대조):**
  - **옮길 값(`Combat.unity`의 `CombatSceneRoot` 직렬화 값 그대로):**

| 값 | 씬 값 | 지금 코드 기본값(참고) |
|---|---|---|
| `movementStepSeconds` | 0.2 | 0.12 |
| `movementTurnSeconds` | 0.1 | 0.1 |
| `enemyActionGapSeconds` | 0.2 | 0.2 |
| `enemyTurnGapSeconds` | 0.4 | 0.4 |
| `summonRiseDelaySeconds` | 0.3 | 0.2 |
| `summonRiseSeconds` | 1 | 0.6 |
| `summonRiseDepth` | 1.5 | 1.5 |
| `summonRiseTurns` | 1 | 1 |
| `roarDelaySeconds` | 0.2 | 0.2 |
| `roarPulseSeconds` | 1 | 0.4 |
| `roarPeakScale` | 1.25 | 1.25 |

  - **검증(조건 원본 하나 유지):**
    - 조건은 설정 Data(`CombatPresentationSettingsData`)의 정적 조건 함수 한 곳에 둔다. 걸음 시간은 유한한 양수, 간격·시간은 유한한 0 이상, 깊이·바퀴 수는 유한한 0 이상, 커지기 배율은 유한한 양수다.
    - `Validate()`는 표가 만들 때 부른다(`InputSettings`·`StageFieldView`와 같은 방식).
    - 런타임 검증(`CombatSceneEventPresenter.ValidateStepSeconds`, `CombatEnemyPresentationPacing.ValidateSeconds`, `CombatAbilityMotionSettings` 생성자)은 이 조건 함수를 부른다. 그래서 DB 쪽과 런타임 쪽 조건이 갈라지지 않는다. DB 층은 런타임 클래스를 모른다.
    - Factory는 조립 맨 앞에서 `Validate()`를 한 번 더 부른다. 씬 객체 Configure·소유 GameObject 생성 전에 거절하는 SV1 순서를 유지한다.
  - **연결:**
    - `CombatDisplayTables`에 `PresentationSettings`를 더한다.
    - `CombatAbilityMotionSettings.From(settings)`가 능력 동작 값을 만든다(DB Data가 런타임 클래스를 만들지 않게).
    - `CombatSceneRoot`에서 값 11개·읽기 속성·`CreateAbilityMotionSettings`·`ValidatePresentationTiming`을 없앤다. 입력·배치 값(`innerCellInset`·커서 스냅)은 남는다.
    - `Combat.unity`의 옛 직렬화 값은 Unity가 다음 저장 때 지운다(에셋 삭제 아님).
  - **`IViewQuery`에 속성이 늘어 가짜 구현 7개에 한 줄씩 더한다.** 6개는 호진님 시험(`ArtifactPopupConfigTests`, `GlobalInputCompositionTests`, `LobbyInputTests`, `LobbyViewSystemTests`, `ViewInputCompositionTests`, `ViewPopupCompositionTests`), 1개는 `UnityStageInputTests`다. 가시밭 표 추가 때와 같은 방식이고, 호진님 공유 메모를 남긴다.
  - **시험 값:** 실제 표시를 조립하는 시험(`CombatSceneEventPresenterTests`, `CombatMortarAssetTests`, `CombatPointerMappingTests`, `TerrainOperationalDisplayTests`, `WallAttackInteractionTests`, `CombatDisplayReuseTests`)은 모두 실제 `Combat.unity`의 씬 루트를 연다. 그래서 이미 씬 값으로 돌았고, DB 값도 같아 시간이 바뀌지 않는다.
  - **사용자 결정(2026-10-18):**
    - Factory 시험은 실제 DB 값으로 돈다.
    - `CombatEnemyPresentationTests.MovementTurnSetting_InheritsSerializedEnemyTurnValue`(씬 필드 이름 이전)는 지킬 대상이 없어져 삭제한다. 새 설정 Data에는 옛 이름 이전을 달지 않는다.
  - **바꿀 시험(코드 대조로 확정):**
    - `CombatEnemyPresentationTests`의 씬 루트 검증 세 묶음(간격, 능력 동작, 걸음): 같은 자리에서 설정 Data 검증으로 바꾼다. 사건 Presenter 생성자 거절 확인은 그대로다.
    - `CombatMortarAssetTests.Factory_InvalidMovementStep…`: 잘못된 값을 씬 루트 대신 표의 설정 Data에 넣는다.
    - `CombatSceneEventPresenterTests`·`CombatDisplayReuseTests`: `root.MovementStepSeconds`를 설정 값으로 바꾼다.
    - `new CombatDisplayTables(...)` 3곳: 설정 인자를 더한다.
    - 새 시험: `ViewDataTableTests`(설정 표 항목 하나, 잘못된 값 거절), 관리 실행용 `CombatPresentationSettingsDataTests`(조건·이름·능력 동작 값 복사), `DataBaseAssetTests`(실제 에셋이 있고 값 조건을 지킴).
    - 실제 에셋 값이 씬 값과 같은지는 착수 때 한 번 줄 단위로 대조했다(§12.8). 시험으로 고정하지 않는다. 이 값들은 기획이 조정하는 값이라, 고정하면 조정할 때마다 시험을 고쳐야 한다(구현 중 판단).
- **Unity 확인 묶음:** `DataBaseAssetTests`, `ViewDataTableTests`, 재생 묶음, Combat 진입(Play From Boot).

## 10. B 단계 — 조각·조립 (설계 B15, §9 I-6)

- **B-0 상세 설계(코드 없음, 2026-10-18 작성):** [조각·조립 상세 설계](../specs/2026-10-18-combat-presentation-pieces-design.md) BP1~BP9. 사용자 결정 Q-B1~Q-B7(옛 Q-R2~Q-R5를 다시 물은 넷 포함)을 받은 뒤 B-1에 착수한다. 아래 Task는 모두 **권장안(A) 기준**이고, 다른 답이면 해당 Task를 고친 뒤 착수한다. **결정 완료(2026-10-18): Q-B1~Q-B7 모두 A, 구조 원칙 평가 보완 R2(a)·R3·R5 포함(설계 §6.1).**
- 공통: 새 파일은 `C/`(창구·표시 계약은 `D/`), `internal sealed`, CRLF, `.meta` 새 GUID. Phase마다 MSBuild 오류·새 경고 0, 관리 실행 회귀 0, 검토 에이전트 1개, Unity 묶음 결과 뒤 다음 Phase.

### 10.0 기대값 (2026-10-18, 착수 전 추정)

결정대로 B-1~B-4를 마쳤을 때 예상하는 결과다. Phase마다 실제 값을 §12에 적어 비교한다.

**화면·동작**

| Phase | 화면 | 완료 시점 | 예외·진단 |
|---|---|---|---|
| B-1 | 같음 | 같음 | 같음 |
| B-2 | 같음(포효·소환 순서·시간 그대로) | 같음 | 포효·소환 진행 중 막는 이유 이름이 `AbilityBusy` → `PieceBusy` |
| B-3 | 같음(같은 프레임 안 소리 호출 앞뒤만) | 같음 | 같음 |
| B-4 | 피격 때 플래시(값을 넣은 외형만) | 플래시가 반응보다 길면 그 유닛의 다음 사건이 늦어짐(의도) | 같음 |

**코드 구조**

- 새 파일(시험 제외, `.meta` 별도): B-1 약 7개(조각 계약·맥락·시점·조립·실행·조각 실행 재생기·출처 수명 계약), B-2 4개(크기·등장·강조 끄기·정면 공격 조각), B-3 3~4개(소리 표·소리 조각·반응 조각·막기 규칙 함수), B-4 2개(외형 층 열거형·플래시 조각). 합계 약 16~17개.
- 지우는 파일: B-2에서 `CombatAbilityMotionPlayback`(239줄)·`CombatAbilityPlayer`(57줄)와 그 시험 2개. 시험 31개의 단정은 새 조각·조립 시험으로 옮긴다.
- `CombatAttackPlayback`(497줄): 포효 메서드 넷이 일반 출처 메서드가 된다. 포효 형식을 모르게 된다.
- 포효·소환 처리기: 조립 목록 하나가 된다. `CombatEventAudio`: 표를 읽기만 한다.
- 사건 Presenter(330줄): 능력 동작 필드·능력 재생기 조립이 빠지고 조각 실행 재생기 조립이 들어온다. 크기는 비슷하다.
- 실행 목록: B-1 `Create(attack, ability, pieces, step, reaction)` → B-2 `Create(attack, pieces, step, reaction)`.

**사실 원본(SSOT)**

| 사실 | 지금 | B 뒤 |
|---|---|---|
| 포효 순서 | 다섯 곳 | 포효 처리기의 조립 한 곳 |
| 출처 수명 | 포효 전용 메서드 넷 + Gate | Gate + 조각 실행 한 쌍 |
| 사건 효과음 "무엇을 내나" | `switch` + 사망 처리기 직접 호출 | 소리 표 한 곳 |
| 국면 → 배경음 프리셋 | 세 클래스(사건 소리·표시 갱신·조정자) | 함수 하나 |
| 사건 행위자 | 처리기 `ActorOf` + 간격 계산 | 처리기 `ActorOf` |
| 주체 기준 막기 | 반응 재생기 | 규칙 함수 하나(반응 재생기·조각 실행 재생기 공용) |
| 플래시 값 | 없음 | 반응 연출 SO |

**변경 시나리오**

| 시나리오 | 지금 | B 뒤 |
|---|---|---|
| 포효 순서 바꾸기 | 처리기 + 능력 동작 + 공격 재생 | 포효 조립만 |
| 다른 사건에 플래시 넣기 | 수단 없음 | 그 처리기 조립 한 줄 + 외형 값 |
| 새 시간형 연출(예: 흔들기) | 새 재생 부품 + 재생기 + 조립 | 조각 하나 + 조립 한 줄(기존 채널일 때) |
| 사건 효과음 추가 | `switch` | 소리 표 한 줄 |
| 배경음 국면 규칙 변경 | 세 클래스 | 함수 하나 |
| 행위자 있는 새 형식 | 처리기 + 간격 계산 | 처리기 |

**구조 원칙 평가 점수(평가 §1 기준, 10점)**

| 기준 | View 재생: 지금 → B 뒤 |
|---|---|
| SSOT | 7 → 9 |
| S 단일 책임 | 8 → 9 |
| O 개방-폐쇄 | 7 → 8 |
| L 리스코프 | 9 → 9 |
| I 인터페이스 분리 | 8 → 8 |
| D 의존성 역전 | 8 → 9 |
| FW 계약·지침 | 9 → 9 |
| 변경 독립성 | 7 → 9 |
| **종합** | **8 → 9** |

- View 입력·조정자는 6 그대로다. N1~N5는 범위 밖이고, 배경음 함수(R2)가 조정자의 한 줄을 바꿀 뿐이다. View 전체는 약 7 → 7.5다.
- 전투 골격은 바뀌지 않는다(9).

**검증**

- 새 관리 실행 시험: B-1 약 30개, B-2 약 20개(옛 시험 31개의 단정 이전 포함), B-3 약 10개, B-4 약 10개.
- Unity 묶음은 Phase마다 한 번(네 번)이다. 포효·소환·사망·피격 재생 확인은 B-2·B-3·B-4에서 한다.
- 관리 실행에서 Presenter 경로는 NATIVE라, 화면 동등성은 Unity 결과로만 확인한다.

**위험(기대와 달라질 수 있는 곳)**

- 프레임 단위 시점: 외부 끝 콜백과 "신호 시작은 그 프레임 0" 규칙이 지금 O4와 한 프레임이라도 다르면 포효 작아짐·넉백 겹침이 달라진다. B-1 관리 시험과 B-2 Unity 시험으로 지킨다.
- B-2 시험 이전: 옛 능력 재생 시험 31개를 옮기며 빠지는 단정이 없어야 한다. 지울 시험 목록은 착수 때 보고한다.
- B-4 외형 합성: PropertyBlock 합성은 관리 실행으로 확인할 수 없다. Unity 시험이 유일한 확인이다.
- B-4 시험 값: 플래시 값을 넣을 외형은 B-4 착수 때 묻는다. 값이 없으면 화면 변화도 없다.
- 범위 밖으로 남는 것: 입력·조정자 N1~N5, 포기 표시 계약 R9, 시험 결합 R8, 폴더 R10.

**공유**

- 민형님 Factory 공유 문서: B-1(R5 이름 있는 인자), B-4(반응 SO 필드).
- 현강님 소리 메모: B-3(소리 표·배경음 함수 자리).
- Core 영향 없음.

### 10.1 B-1 — 조각·실행 기반 (설계 BP1~BP5)

- **새 파일(`C/`):**
  - `ICombatPresentationPiece.cs`: `Begin(CombatPieceContext)`, `Tick(float)`, `IsEnded`, `Cancel()`
  - `CombatPieceContext.cs`: 실행이 빌린 대여 조회(크기·자세, 유닛 ID로), 재생 세대, Mapper 읽기, 신호 보내기(조각 실행 안에서만)
  - `CombatPieceStart.cs`: `AtStart(delay)`·`After(item, signal, delay)`, `enum CombatPieceSignal { End }`
  - `CombatComposition.cs`: 항목(조각·시점·기다림) 목록 + 필요 채널(유닛, 채널) + 범위(행동·유닛과 주체) + 완료 방식(출처: 사건·행위자·정책 / 일반). 만들 때 앞 항목 번호·채널을 검사한다
  - `CombatPieceRun.cs`: BP4 규칙(대여 한 번에, 시간 규칙 1~3, 출처·일반 완료, 전체 종료, 취소, 세대, 사라짐)
  - `CombatPieceRunPlayer.cs`: BP5 계약 답(`FindBlock`·`BeginFrame`·`Tick`·`IsComplete`·`BlocksFinalFacts`·`Cancel`·`Release`), `TryBegin(composition, completed, version)`
  - 출처 수명은 계약으로 받는다(`ICombatSourceLifetime`: `BeginSource`·`EndSource`·`AbortSource`). B-1에서 `CombatAttackPlayback`이 이 계약을 구현한다(Gate 호출을 감싼 새 메서드 셋, 지금 `BeginRoar`·`EndRoar`·`AbortRoar`는 이것을 부르게 바꿈 — 동작 없음). Presenter는 이 구현을 조각 실행 집합에 넘긴다.
  - 외부 진행 조각의 끝은 완료 콜백으로 바로 실행에 전달한다(설계 BP1). `CombatPieceContext`는 외부 끝 전달 입구(세대 확인 포함)를 가진다.
- **바꿀 파일:**
  - `CombatStartQuery.cs`: 막는 이유 `PieceBusy`(행동 실행)·`PieceSubject`(유닛 실행)를 더한다. `AbilityBusy`는 B-2에서 없앤다.
  - `CombatPresentationPlayers.cs`: `Create(attack, ability, pieces, step, reaction)`
  - `CombatSceneEventPresenter.cs`: 조각 실행 집합을 조립해 목록에 넣는다. 처리기는 아직 쓰지 않는다.
  - **R3(구조 원칙 평가):** `CombatEnemyPresentationPacing.BeforeEvent(e, actorId, owner)` — 행위자 ID를 처리기 `ActorOf`에서 받고, 형식별 ID 꺼내기(`AttackerId`·`BossId`·`SummonerId`)를 없앤다. 형식별 간격 규칙(공격 성공·포효·소환·턴)은 그대로 간격 계산이 가진다. Presenter `BeginEvent`가 행위자를 한 번 구해 ID와 Owner를 함께 넘긴다. `CombatHitFollowUps.TryGetSource`는 "공격 결과를 가진 출처인가"라는 다른 질문이라 그대로 둔다.
  - **R5(구조 원칙 평가):** `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`의 사건 Presenter 생성 호출을 이름 있는 인자로 바꾼다. 민형님 Factory 공유 문서에 한 줄 적는다.
  - **주체 기준 막기 규칙 함수(B-3에서 옮겨 옴, 2026-10-18 사용자 결정):** 새 파일 `CombatSubjectBlocking.cs`(계약 `ICombatSubjectActivity` + 규칙 `CombatSubjectBlocking.Blocks(query, activity)`). `CombatReactionPlayer.FindBlock`이 이것을 쓴다(동작 없음). 조각 실행 재생기의 유닛 실행도 같은 함수를 쓴다.
- **동작:** 없음(조립 없음, R3·R5도 동작 없음).
- **시험(관리 실행, 새 파일):**
  - `CombatPieceRunTests`: 시간 규칙 1~3(지연 시작은 남은 시간을 받음, 신호 시작은 그 프레임 0, 실행 시작 항목은 다음 프레임부터), 신호 뒤 지연, 같은 순간 항목의 목록 순서, 길이 0 사슬이 시작 안에서 끝남, 외부 끝 콜백이 그 자리에서 다음 항목·완료를 일으킴(세대 확인), 일반 완료(기다림)·출처 완료(가짜 출처: 시작·끝·취소 순서), 취소·세대·사라짐 예외, 대여 실패면 시작하지 않음. "끝 신호가 오지 않는 항목"은 B-1에서 생길 수 없어(신호 끝 하나, 항목 단위 취소 없음) 시험하지 않는다(B-1 검토).
  - `CombatPieceRunPlayerTests`: 행동 실행 막기(겹침 프로필·출처 허용), 유닛 실행 주체 막기(행동 사건·주체 없는 사건 포함)·사망 실행끼리 동시 사망, Tick 반환(Tick 전에 행동 실행이 있었으면 출처의 프레임 시작 값), 완료·최종 사실 막기
  - `CombatPresentationPlayersTests`: 다섯 자리
  - `CombatEnemyPresentationPacingTests`: `BeforeEvent` 호출에 행위자 ID를 넘긴다(36곳, 기대값 그대로)
- **Unity 확인 묶음:** 재생 묶음(§2.3). 동작이 없으므로 회귀만 본다.

### 10.2 B-2 — 포효·소환 재조립 (설계 BP6·BP7)

- **새 파일(`C/`):**
  - `CombatScalePiece.cs`: 배율 from → to, 시간(맥박 절반). 지금 `ApplyScale` 식 그대로
  - `CombatRisePiece.cs`: 대기·올라옴·회전. 지금 `ApplyRise`·`Spin` 식 그대로, 시작 즉시 깊이만큼 아래
  - `CombatHighlightClearPiece.cs`: 재생 강조 끄기(시간 없음)
  - `CombatFacingAttackPiece.cs`: 외부 진행. `TryBeginFacingMotion`을 부르고 그 완료가 끝이다. 시작하지 못하면 바로 끝난다
- **바꿀 파일:**
  - `CombatAttackPlayback.cs`: `BeginRoar`·`EndRoar`·`AbortRoar`·`TryBeginFacing` → `BeginSource`·`EndSource`·`AbortSource`·`TryBeginFacingMotion`(`ICombatSourceLifetime` 구현)
  - `CombatRoarEventHandler.cs`·`CombatSummonEventHandler.cs`: 설계 BP7의 조립을 만들어 조각 실행 집합에 넘긴다. 실패 정리는 실행 취소가 맡는다.
  - `CombatPresentationPlayers.cs`: `Create(attack, pieces, step, reaction)`
  - `CombatSceneEventPresenter.cs`: `_abilities`를 없앤다.
  - `CombatStartQuery.cs`: `AbilityBusy`를 없앤다.
  - 옛 이름을 가리키는 설명·cref: `CombatHitGate.cs`(능력 재생 언급), `CombatAttackPlayback.cs`(포효 수명 설명 여러 곳)
- **지울 파일(우리 코드):** `CombatAbilityMotionPlayback.cs`, `CombatAbilityPlayer.cs`(+ `.meta`). 그 시험 `CombatAbilityMotionPlaybackTests`·`CombatAbilityPlayerTests`의 단정은 아래 새 시험으로 옮긴 뒤 지운다(지울 시험 목록은 착수 때 계획에 적고 보고한다).
  - 지운 시험 31개와 옮긴 곳의 대응표는 §12.11에 있다(B-2 실행 기록).
- **동작:** 없음(같은 순서). 끝에서 강조 끄기와 크기 복원의 앞뒤만 같은 프레임 안에서 바뀐다(그사이 그리기 없음).
- **시험:**
  - 관리 실행: `CombatScalePieceTests`·`CombatRisePieceTests`(옛 능력 재생 시험의 수식·0 시간 단정), `CombatRoarCompositionTests`(가짜 대여·가짜 출처·가짜 정면 공격으로 순서·O4·실패 정리), 소환 조립 순서
  - Unity 묶음: `CombatStandardAttackTests`·`CombatCodeMotionHitTests`(포효·넉백 동시, 히트 타이밍 Q1 회귀), 소환·포효가 있는 `CombatSceneEventPresenterTests`·보스 활성화 묶음, 재생 묶음

### 10.3 B-3 — 사건 소리 (설계 BP8, Q-R18 A)

- **새 파일(`C/`):** `CombatEventSounds.cs`(정적 "무엇을 내나" 표: 사건 + 보인 사실 → 효과음 칸·프리셋 / 배경음 프리셋), `CombatEventSoundPiece.cs`(시간 없는 조각, 표를 읽어 낸다), `CombatReactionPiece.cs`(외부 진행: 반응 재생기의 시작과 끝. 반응 자세·사망 유지는 처리기·반응 재생기가 지금처럼 가진다. 사망 조립이 먼저 쓰고 B-4 피격 조립이 다시 쓴다). 주체 기준 막기 규칙 함수는 B-1에서 만들었다(사용자 결정).
- **바꿀 파일:**
  - `CombatEventAudio.cs`: `switch`만 표로 옮기고, 표를 읽어 내기만 한다(틀 ③·공격 콜백·걸음 콜백이 그대로 부름). 프리셋 함수(`UnitPreset`·`AttackPreset`)는 그대로 둔다(입력 쪽·현강님 시험이 부름).
  - **R2 (a)(구조 원칙 평가):** 국면 → 배경음 프리셋 함수 하나를 소리 표 옆에 둔다(전투 진입·밤 3, 밤 끝·낮 2, 완료 확인 뒤 1). `CombatEventAudio`(진입·밤 끝), `CombatSceneDisplay.Refresh`(밤·낮), `CombatViewSystem.PresentCompletion`(확인 뒤)이 그 함수를 부른다. 값·호출 시점은 그대로다.
  - `CombatCharacterEventHandler.cs`: 사망의 `_sound.Play(death)` 직접 호출을 없애고, 사망 조립(유닛 실행: 0 = 사망 반응(외부 진행), 1 = 사건 소리)을 쓴다. 사망 유지·사실 반영·다시 그리기는 지금처럼 처리기가 한다. 기차 동시 사망은 사망 실행끼리만 겹친다(동시 사망 프로필).
- **동작:** 같은 프레임 안 호출 순서만(Q-R18 A). 값·프리셋은 그대로다.
- **시험:** 관리 실행 `CombatEventSoundsTests`(사건별 단서가 지금 프리셋과 같음, 배경음 프리셋 함수의 세 값), 막기 규칙 함수 시험, 사망 조립 순서(반응 → 소리, 사실 반영 전). Unity 묶음 `GameAudioTests`·사망 시험(`CombatUnitDeathTests`·`CombatUnitDeathLifecycleTests`)·기차 동시 사망(`CombatRailwayContactSceneTests`)·재생 묶음
- **공유:** 현강님 소리 공유 메모(`handoffs/2026-10-18-combat-event-audio-move-hyeongang-share.md`)에 "무엇을 내나" 표와 배경음 프리셋 함수의 자리를 더한다.

### 10.4 B-4 — 피격 플래시 (설계 BP9)

- **새 파일:**
  - `D/`: 플래시 대여 계약 `ICombatUnitFlash`(착수 때 결정으로 외형 층 열거형 대신, 아래 창구 줄)
  - `C/`: `CombatFlashPiece.cs`(색·시간). 반응 조각은 B-3에서 만든 것을 다시 쓴다
- **바꿀 파일:**
  - `D/CombatUnitPresentationWindow.cs`: `CanFlash(id)`·`TryBeginFlash(id, cancelled, out flash)`. 이동 투명도 대여는 그대로다(전투에 하나). 착수 때 사용자 결정 "플래시만 새 함수"로 처음 적은 `TryBeginAppearance(id, layer, out appearance)`를 바꿨다(설계 BP9·§6.1).
  - `D/ICombatUnitPresentationTarget.cs`·`D/FieldUnitView.cs`: 플래시 쓰기와 투명도·플래시 합성 소유자(층마다 값을 기억해 한 번에 씀, 마지막 층이 끝날 때만 원래대로, 공개 Handle만, DR-004). 이 계약의 가짜 구현(`CombatUnitPresentationWindowTests`, `ICombatUnitTargets` 가짜가 있는 `CombatUnitHudFeedbackTests`)도 맞춘다
  - `C/CombatCharacterEventHandler.cs`: 피격 조립(유닛 실행: 반응 + 플래시)
  - 값(Q-B5 A): `CombatUnitReactionPresentationSO`·`Data`에 플래시 색·시간. 비어 있으면 플래시 없음. 시험용 값을 넣을 외형은 착수 때 사용자와 정한다 → 공유 기본 반응 에셋 `CombatUnitReaction_Default`에 빨간색·0.18초(모든 캐릭터, §12.13).
- **동작:** 플래시가 보이고, 플래시가 반응보다 길면 사건 완료가 늦어진다(의도, 요구 1·3).
- **시험:** 관리 실행 `CombatFlashPieceTests`(가짜 외형 층), `CombatUnitPresentationWindowTests`에 외형 층(층마다 하나, 반환), 피격 조립 순서. Unity: 실제 Renderer에서 투명도·플래시 합성과 양쪽 순서 복원 시험(새), `CombatUnitReactionTests`·`CombatUnitDeathTests`·`CombatMovementPlanningDisplayTests`(투명도와 함께)·재생 묶음
- **공유:** 색 Handle을 쓰는 방식이 바뀌면 민형님 공유 문서에 절을 더한다. DB SO 필드가 늘면 같은 문서에 적는다.

## 11. 관리 실행 방법

분리 계획 §7과 같다. 도구는 메모리 `offline-build-test-runner`(이 PC) 또는 `projectl-build-check`(S-0 PC)에 적힌 대로 다시 만든다. 기준선 빌드는 바뀐 파일만 `git show`로 뽑아 바꾸는 방식(분리 계획 §7)을 쓴다.

## 12. 실행 기록

### 12.0 착수 (2026-10-18)

- 사용자: "S 작업 문제 없는거 확인했고 A-1 부터 차례로 진행해". S-1~S-5 Unity 결과는 문제 없음(분리 계획 §8.3에 기록).
- **착수 조건 1(분리 커밋)은 사용자 결정으로 건너뛰었다.** S-2~S-5가 미커밋인 채, 같은 작업 트리에서 A-1을 이어 했다(질문에 "그대로 이어서 진행"). 그래서 `CombatSceneEventPresenter.cs` 한 파일에 S와 A-1 변경이 함께 있다.
  - 나눠 커밋할 때 쓰도록 S-5 최종 상태를 스크래치에 남겼다: HEAD 대비 추적 파일 패치 `s2-s5-tracked.patch`, 새 파일 목록 `s2-s5-untracked.txt`와 사본 `s2-s5-files/`. 스크래치는 이 세션에만 있다.
- 관리 실행 기준선: 분리 계획 §8.3의 최종 트리(5,060개). 그 뒤 코드 변경이 없어 그대로 쓴다.

### 12.1 A-1 (2026-10-18)

- **새 파일:** `C/CombatStartQuery.cs`(GUID `4ce42b25…`: `CombatStartProfile`·`CombatStartBlock`·`CombatStartQuery`), `T/CombatStartQueryTests.cs`(GUID `6ce5f681…`, 6개).
- **Presenter:**
  - `FindBlock(in CombatStartQuery)`와 도우미 넷(`AttackBlock`·`AbilityBlock`·`StepBlock`·`ReactionBlock`)을 만들었다.
  - `CanBeginPresentationEvent`는 `FindBlock(Query)`로 바뀌었다.
  - `RequireIdle`은 `RequireStart(in query)`로 바뀌었다. `BeginPresentation`은 Result, `BeginConcurrentDeath`는 ConcurrentDeaths를 쓴다.
  - `BeginEvent`·`RequireIdleFor`는 Query를 확인한 뒤 Start를 확인한다.
  - 예외 문구 끝에 " 막는 이유: {block}"을 붙인다(Q-R8 A). 단정 문구는 `BusyMessage` 상수 하나다.
  - `ReactionBlocks`·`MotionBlocks`는 도우미가 쓰는 private 함수로 남겼다(A-2에서 재생기로 옮김).
- **계획과 다른 점:** 없음. `CombatStartQuery.Query/Start(null)`이 `ArgumentNullException`을 던진다(계획 §2.1). 호출자는 null을 넘기지 않는다(검토 확인). 분리 전에도 null은 `CombatHitGate.CanBegin`에서 같은 예외였다.
- **빌드:** MSBuild 오류 0, 고유 경고 16개가 같다(새 경고 0).
- **관리 실행(전체 5,066개):** S-5 최종 대비 메서드 단위 차이 0, 새 시험 6개 PASS. 합계 PASS 2,720 · NATIVE 2,161 · FAIL 138 · UNSUPPORTED 47.
- **검토 에이전트 1개:** 다섯 호출 자리(질의, 사건 시작, 걸음·복귀 시작, 결과 시작, 동시 사망) 모두 불리언 결과가 분리 뒤와 같다고 판단했다. 순서가 바뀐 조건은 모두 부수효과 없는 읽기다. 예외 문구를 확인하는 시험은 없다. 지적 3건(모두 낮음):
  - 반영: `ReactionBlock`이 `Profile switch`로 사망 겹침을 따로 적고 `AllowsDeaths`를 쓰지 않던 것. 이제 `AllowsDeaths ? … : Item != null ? … : …`로 분리 전 식과 같은 모양이다. 재빌드·관리 실행 확인은 아래 줄.
  - 기록만: "질의 실패면 예외 → 시작 단정" 두 단계가 `BeginEvent`·`RequireIdleFor`에 같은 모양으로 있다. 분리 전에도 있던 중복이고 A-2b에서 하나로 줄어든다.
  - 기록만: `default(CombatStartQuery)`는 팩토리의 null 거절을 우회한다. `FindBlock`이 private이고 모든 호출이 팩토리를 거쳐 잠재 위험만 있다.
- **검토 반영 뒤:** 재빌드 오류 0·고유 경고 16개 같음, 관리 실행 전체 5,066개가 반영 전과 메서드 단위로 같다(차이 0). 실행이 남긴 `Assets/__TornadoLegacy_*.asset` 한 벌을 지웠다.
- **Unity 확인(사용자, A-1 묶음 §2.3):** `CombatSceneEventPresenterTests`, `CombatEnemyPresentationTests`, `CombatUnitDeathTests`, `CombatUnitReactionTests`, `CombatUnitReactionSetTests`, `CombatRailwayContactSceneTests`, `CombatMovementViewTests`, `CombatBallistaAttackTests`, `CombatMortarPresentationTests`, `CombatStandardAttackTests`, `CombatCodeMotionHitTests`. 이 Phase가 바꾼 경로는 관리 실행에서 모두 NATIVE라 이 결과로만 확인한다.
- **Unity 확인 결과(2026-10-18, 사용자):** "A-1 Unity 묶음 문제 없는 것 확인했어". A-1 끝.
- **커밋 때 함께 넣을 새 파일:** `C/CombatStartQuery.cs`·`T/CombatStartQueryTests.cs`(+ `.meta`).

### 12.2 A-2 (2026-10-18)

- 사용자: "A-1 Unity 묶음 문제 없는 것 확인했어. A-2 진행해."
- **새 파일(`C/`, GUID 앞 8자리):** `ICombatPresentationPlayer`(`c2170a56`), `CombatPlaybackFrame`(`bbb193c4`), `CombatPresentationPlayers`(`48d5f422`), `ICombatWaitSlot`(`eb50d19d`), `ICombatSourceOverlap`(`a4d31aef`), `CombatAttackPlayer`(`a4c1da1b`), `CombatAbilityPlayer`(`995291f7`), `CombatStepPlayer`(`628918f8`), `CombatReactionPlayer`(`16504542`).
- **새 시험(`T/`):** `CombatPresentationPlayersTests`(12, `e7573268`), `CombatAbilityPlayerTests`(11, `69a15c30`), `CombatReactionPlayerTests`(5, `76abfd42`). `CombatAbilityMotionPlaybackTests`에 `IsWaitingForAttack` 시험 3개 추가.
- **부품 변경:** `CombatAbilityMotionPlayback.IsWaitingForAttack`(읽기 속성)만 더했다.
- **Presenter:**
  - 재생기 넷과 실행 목록을 생성자에서 조립한다. 재생기를 이름으로 아는 곳은 조립 한 곳이다.
  - 다음 멤버가 실행 목록을 부른다: `BeginPresentation`·`IsPresentationComplete`·`CanBeginPresentationEvent`·`Refresh` 단정·`TickPresentation`·`CancelMovement`·`Release`.
  - 간격·대기·머묾은 `_waitSlot`(`ICombatWaitSlot`)으로 간다.
  - 없앤 것: A-1 도우미 넷, `ReactionBlocks`·`MotionBlocks`(재생기로 이동), `StartGap`·`TickMotion`(걸음·대기 재생기로), `CancelUnavailableReaction`(반응 재생기로), `RunCleanup`, 필드 `_wait`·`_reactionUnavailable`.
  - 시험 리플렉션 필드 `_attacks`·`_reaction`·`_motion`·`_facts`·`_sound`는 같은 형식으로 남았다.
- **다시 연결:**
  - `CombatMovementPresentation` 생성자가 `Action<float>` 대신 `ICombatWaitSlot`을 받는다.
  - 캐릭터 처리기는 `CombatReactionPlayer.CancelUnavailable`을 받는다.
- **계획과 다른 점:**
  - `CombatReactionPlayerTests`는 반응 불가 상태와 빈 집합 규칙만 관리 실행 시험으로 둔다. 반응을 실제로 시작하려면 Transform이 있는 `FieldGridMapper`가 필요해 진행 중 반응 규칙(O9·O12·O14)은 Unity 재생 시험이 지킨다(계획 §3.5의 "착수 때 확인" 결과).
  - `CancelAll`·`ReleaseAll`은 오류를 호출자의 목록(`ref List<Exception>`)에 모은다. Presenter는 간격 값·회전·강조 정리와 함께 한 번에 던진다(Q-R7 A의 한 묶음 정리).
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(전체 5,092개):** A-1(반영 뒤) 대비 기존 시험 상태 변화 0, 새 시험 26개 PASS. 합계 PASS 2,746 · NATIVE 2,161 · FAIL 138 · UNSUPPORTED 47.
- **검토 에이전트 1개:** 정상 경로에서 동작이 달라진 곳은 없다고 판단했다. 확인한 것:
  - 프레임 순서와 멈춤
  - O4 동등성: `CombatHitGate` 규칙 1, `FieldUnitView` 대여 취소 순서
  - 완료·최종 사실·시작 조건 네 프로필
  - 간격·대기, 반응 불가 콜백 연결
  - 리플렉션 필드, 새 시험 기대값
- **검토 지적 6건 처리:**
  - 반영: 반응 재생기 시험 없음 → `CombatReactionPlayerTests` 추가
  - 반영: 계약 문서의 `BeginFrame` 예외 설명이 구현과 다름 → "반응 불가만 `BeginFrame`, 다른 재생기는 `Tick`에서" 문장으로 고침(예외 순서 보존)
  - 반영: 걸음·대기 재생기 `Cancel`에서 레인 취소가 실패하면 대기 칸이 안 비워짐 → `try/finally`(실패 경로만)
  - 반영: 오래된 주석 2곳(`CombatCharacterEventHandler`, `CombatEventWait`)
  - 반영하지 않음: 인코딩(BOM) 지적. 확인 결과 사실이 아니다. 어느 파일에도 BOM이 없고, 두 파일은 `using` 없이 `namespace`로 시작할 뿐이다
  - 기록만: A-3a에서 대여 취소 순서(자세 먼저, 크기와 함께)를 지킬 것 → §5.3에 적음
- **검토 반영 뒤:** 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,097개에서 반영 전 대비 기존 시험 상태 변화 0, 새 `CombatReactionPlayerTests` 5개 PASS. 합계 PASS 2,751 · NATIVE 2,161 · FAIL 138 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **Unity 확인(사용자, A-2 묶음 §3.5):** 재생 묶음(§2.3) + `CombatPresentationCleanupTests`의 Unity 쪽 + 포효·소환 시험(보스 활성화 묶음).
- **Unity 확인 결과(2026-10-18, 사용자):** "A-2 Unity 묶음 문제 없는 것 확인했어". A-2 끝.
- **커밋 때 함께 넣을 새 파일:** 위 새 파일 9개와 새 시험 3개(+ `.meta`).

### 12.3 A-2b (2026-10-18)

- 사용자: "A-2 Unity 묶음 문제 없는 것 확인했어. A-2b 진행해."
- **새 파일 없음.** 바꾼 파일: `C/CombatStartQuery.cs`, `C/CombatAttackPlayer.cs`, `C/CombatStepPlayer.cs`, `C/CombatSceneEventPresenter.cs`, `C/CombatAttackPlayback.cs`(주석만). 시험은 `T/CombatStartQueryTests.cs`, `T/CombatAbilityPlayerTests.cs`, `T/CombatReactionPlayerTests.cs`.
- **시작 조건:**
  - `CombatStartProfile.Query`와 `CombatStartQuery.Query(...)`를 없앴다. 프로필은 `Start`·`Result`·`ConcurrentDeaths` 셋이고, `AllowsOverlap`은 `Start`만 참이다.
  - 공격 재생기 시작 행: 사라짐 → Gate(`CanBegin(item)`, 사건이 있을 때만) → 히트 전 공격 중·재장전(`BlocksNextEvent`).
  - 걸음·대기 재생기 시작 행: 레인 자원 → 걸음 Gate(사건이 있을 때만) → 대기 사건.
  - 능력·반응 재생기는 그대로다(두 행이 이미 같았다).
- **Presenter:**
  - `CanBeginPresentationEvent`는 `FindBlock(Start(item))`이다.
  - `BeginEvent`는 한 번만 확인하고 지금 문구("접촉 또는 남은 공격 표시를 먼저 기다려야 합니다.")에 이유를 붙여 던진다.
  - `RequireIdleFor`는 `RequireStart(Start(movement))` 하나다.
- **도달하지 않는 경로 확인(코드 대조):** 스케줄러가 대기 사건·재장전 중에 묻는 경로는 지금 없다.
  - 행동 사건은 동시 묶음 밖(-1)이라 장벽이다. 순차 재생은 끝나지 않은 장벽에서 훑기를 멈춘다.
  - 대기 사건(간격 뒤 대기·포효/소환 머묾)과 턴 끝 Ballista 재장전은 행동 사건에서만 생기고, 그 사건 완료는 대기·재장전이 끝날 때 온다.
  - `CombatHitGate.CanBegin`은 출처가 있고 완료 통지 전이면 거짓이라, Gate가 허락하면 공격 겹침도 허용된 상태다.
- **동작:** 화면 같음. 단정의 던짐 여부는 옛 질의 ∪ 단정과 새 시작이 같은 합집합이라 그대로다. 달라진 것은 예외 문구뿐이다.
  - `BeginEvent`에서 옛 단정에만 있던 막힘(대기 사건 등)의 문구가 "이전 사건 연출이 아직 끝나지 않았습니다."에서 "접촉 또는 남은 공격 표시를…"으로 바뀌었다.
  - 막힘이 여럿이면 첫 이유가 달라질 수 있다(예: 공격 재장전과 걸음 Gate가 함께면 옛 StepGate → 새 AttackBusy).
  - 문구를 확인하는 시험은 없다.
- **시험:** `CombatStartQueryTests`에서 Query 시험을 빼고 프로필 목록 시험(`Profiles_AreStartResultAndConcurrentDeaths`)을 더했다. 능력·반응 재생기 시험의 Query 확인은 Start로 바꾸거나 뺐다. 공격·걸음 재생기 합집합은 `FieldUnitPresenter`(MonoBehaviour)가 필요해 재생 묶음(Unity)으로 확인한다.
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(전체 5,097개):** A-2(반영 뒤) 대비 기존 시험 상태 변화 0. 차이는 바꾼 시험 하나(Query 시험 제거, 프로필 목록 시험 PASS)뿐이다. 합계 PASS 2,751 · NATIVE 2,161 · FAIL 138 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **검토 에이전트 1개:** 정상 경로에서 동작이 달라진 곳은 없다고 판단했다. 도달 불가 전제, 단정 결과 동일, `Item != null` 가드, 남은 Query 참조 없음, CRLF·BOM, 새 시험 컴파일을 확인했다.
- **검토 지적 처리:**
  - 반영: `RequireIdleFor` 주석이 두 단계 구조를 설명함 → 같은 시작 판단 하나를 단정한다고 고침
  - 반영: `CombatAttackPlayback.CanBegin` 주석 "반응 대기는 호출자가 먼저 봅니다"가 A-2 실행 목록 뒤 맞지 않음 → 실행 목록이 함께 본다고 고침
  - 반영: 열거형 0번이 `Start`가 되어 `default(CombatStartQuery)`가 겹침 허용으로 조용히 통과할 수 있음 → `Start = 1`로 0번을 비워, 기본값 질문은 겹침 없는 단정처럼 막는다(지금 기본값을 만드는 곳은 없음)
  - 반영: 시험 이름 `ProfilesWithEvent_RejectMissingEvent` → `Start_RejectsMissingEvent`(이제 사건을 받는 프로필은 하나)
  - 기록만: `CombatSceneEventPlayback`이 같은 판단을 먼저 확인하고 이유 없는 문구("이전 사건의 표시가 남아 있습니다.")로 던져, 운영 경로에서는 Presenter의 이유 붙은 예외에 닿지 않는다. 도달하지 않는 경로의 진단 차이라 A-4b(재생 계약 사슬 줄이기) 후보로 둔다
- **검토 반영 뒤:** 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,097개에서 반영 전 대비 기존 시험 상태 변화 0. 차이는 이름을 바꾼 시험 하나(`Start_RejectsMissingEvent` PASS)뿐이다. 합계 PASS 2,751 · NATIVE 2,161 · FAIL 138 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **Unity 확인(사용자, A-2b 묶음 = A-2 묶음 §3.5):** 재생 묶음(§2.3) + `CombatPresentationCleanupTests`의 Unity 쪽 + 포효·소환 시험(보스 활성화 묶음).
- **Unity 확인 결과(2026-10-18, 사용자):** "A-2b Unity 묶음 문제 없는 것 확인했어". A-2b 끝.

### 12.4 A-3a (2026-10-18)

- 사용자: "A-2b Unity 묶음 문제 없는 것 확인했어. A-3a 진행해."
- **착수 직전 재대조:** 서명·만드는 곳·바꿀 시험을 코드와 맞춰 §5.1에 확정으로 적고, §5.2·§5.3의 가칭을 거기에 맞췄다(§0.1-3).
  - 창구는 `Configure`가 아니라 `FieldUnitPresenter.Presentation`이 처음 읽힐 때 만든다. 시험 셋과 Factory의 이동 미리보기가 `Configure` 전에 Presenter를 만들기 때문이다.
  - 사망 유지(`HoldDeath`)와 이동 투명도(`BeginMovementOpacity`)는 지금처럼 예외를 던진다. 걸음 배치(`BeginPlacement`)는 지금처럼 표시 없이도 시작한다. 그래서 셋 다 `Try`가 아니다.
- **새 파일(`D/`, GUID 앞 8자리):**
  - `ICombatUnitPresentationTarget`(`1328fe44`), `ICombatUnitTargets`(`95c16942`)
  - `ICombatUnitPlacement`(`20486f06`), `ICombatUnitDeathHold`(`2992e946`), `ICombatUnitAppearance`(`34e5f6a5`)
  - `CombatUnitPresentationWindow`(`25466e80`)
- **새 시험(`T/`):** `CombatUnitPresentationWindowTests`(26, `d1ee4cfb`). 관리 실행 24개(가짜 표시)와 Unity 전용 2개(실제 `FieldUnitView`가 꺼지거나 다른 유닛으로 다시 대여될 때 자세 먼저, 크기와 함께 취소)로 이뤄진다.
- **`FieldUnitPresenter`(712 → 532줄):**
  - 세 표(`_steppingActors`·`_deathHolds`·`_presentationRotations`)와 이동 투명도 주인, 채널 멤버 20개(사망 유지 클래스 포함)를 창구로 옮겼다.
  - `RenderFacts`는 창구에 소유 사실(`HoldsDeath`·`KeepsPlacement`·`TryGetFacing`)을 묻는다.
  - `Release`는 같은 자리에서 `EndMovementOpacity`와 `Reset`을 부른다. 풀 반환은 `OnReturned`를 부른다.
  - 사실 그리기·몸 조회·HUD는 남는다(§5.4).
- **`FieldUnitView`:** 표시 계약을 명시적으로 구현한다. 쓰는 곳이 없어진 `BeginMotionPose`를 없앴다.
- **다시 연결한 것:**
  - 공격 재생, 걸음 레인 집합·레인(창구 + 이동자 몸 조회 함수). 레인은 유닛 ID 대신 배치 대여를 가진다.
  - 이동 표시, 소환·포효·캐릭터 처리기. 캐릭터 처리기는 HUD 피해 숫자 때문에 A-3b까지 `FieldUnitPresenter`도 받는다.
  - 이동 미리보기(외형 층 대여), 사건 Presenter.
  - 생성자 서명이 바뀐 내부 클래스를 직접 만드는 시험은 없었다.
- **바꾼 시험(호출 대상만):** §5.1 목록 그대로다. 검토에서 나온 리플렉션 셋도 바꿨다(아래).
- **동작:** 화면 같음.
  - 지금 도달하지 않는 같은 유닛의 자세 ↔ 걸음 배치 겹침이 예외가 된다(Q-R11 A). 검토 에이전트가 경로를 대조해 도달하지 않음을 확인했다.
  - 예외 문구 앞머리가 `[FieldUnitPresenter]`에서 `[CombatUnitPresentationWindow]`로 바뀐다. 이 문구를 확인하는 시험은 없다.
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음. 빌드 설정에 `Display/` 새 파일을 더했다(`after-a3a.targets`).
- **관리 실행(전체 5,123개):** A-2b(반영 뒤) 대비 기존 시험 상태 변화 0. 새 창구 시험 24개 PASS, Unity 전용 2개 NATIVE. 합계 PASS 2,775 · NATIVE 2,163 · FAIL 138 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **검토 에이전트 1개:** 정상 경로에서 동작이 달라진 곳은 없고, Q-R11 겹침은 도달하지 않는다고 판단했다. 확인한 것:
  - 옮긴 멤버 전부를 HEAD와 대조했다. 부수 효과, Unity null, `Release` 순서까지 포함한다.
  - 배치 대여의 대체 동작은 실제로 생기지 않는다(레인은 유닛당 하나, 다음 걸음 전에 반납).
  - 창구를 `Configure` 전에 써도 안전하다.
- **검토 지적 처리:**
  - 반영: 옮긴 필드를 리플렉션으로 읽는 시험 셋이 Unity에서 `MissingFieldException`으로 실패할 것이었다. 관리 실행에서는 NATIVE라 드러나지 않았다.
    - `CombatUnitDeathTests.HoldCount`(`CombatUnitDeathLifecycleTests`도 씀)와 `CombatRailwayContactSceneTests.HoldCount`는 창구의 `_deathHolds`를 읽게 했다.
    - `CombatSceneEventPresenterTests`의 `_movementOpacityActor` 두 곳은 창구의 이동 투명도 주인을 읽는 도우미로 바꿨다.
  - 반영: Unity 전용 시험 `FieldUnitView_Disabled_CancelsPoseBeforeScale`. EditMode의 `SetActive(false)`는 `[ExecuteAlways]`가 없는 MonoBehaviour의 `OnDisable`을 보장하지 않는다(`CombatDisplayReuseTests`와 같은 처리). 그래서 `OnDisable`을 직접 부른다. 이미 불렸어도 취소는 한 번만 알린다.
  - 반영: 계획 §5.2·§5.3의 가칭(`TryGetActive`·`TryHoldDeath`·`TryBeginPlacement`·"Configure 한 곳")을 §5.1 확정과 맞췄다.
  - 기록만:
    - `BeginMovementOpacity` 실패 경로. 단일 실패면 관찰되는 호출 순서가 같고, 정리까지 실패할 때만 `AggregateException`을 만드는 곳이 다르다.
    - 레인의 `_stepPrepared`가 배치 대여보다 먼저 정해져, Q-R11 예외가 나면 대여 없이 남는다. 실패 경로에만 있다.
  - 기록만(나중 후보): 사실 배치 규칙에 닿는 길이 둘이다. 레인은 아직 `FieldUnitPresenter.CenterOf`·`ModelRotation`을 정적으로 부르고(걸음 방향·경로 끝 복귀), 창구는 `ICombatUnitTargets.FactPosition`·`FactRotation`을 거친다. 상태 중복은 없다. `ICombatUnitTargets`가 찾기·연출 값·사실 배치를 함께 가지는 것과 같이 다음 정리 때 본다(A-3b에서 HUD 찾기가 더해져 그 항목이 커졌다, §12.5).
- **검토 반영 뒤:** 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,123개에서 반영 전 대비 상태 변화 0(바꾼 시험은 모두 Unity 전용이라 NATIVE 그대로). 합계 PASS 2,775 · NATIVE 2,163 · FAIL 138 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **Unity 확인(사용자, A-3a 묶음 §5.5 + 이번에 바꾼 시험):**
  - 재생 묶음(§2.3)
  - `CombatUnitPresentationWindowTests`의 Unity 전용 2개
  - `CombatMovementPlanningDisplayTests`, `AttackHealthPreviewDisplayTests`, 배치 미리보기 시험
  - `CombatUnitDeathTests`·`CombatUnitDeathLifecycleTests`(사망 유지)
  - 호출 대상을 바꾼 `CombatBallistaAttackTests`·`CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatEnemyPresentationTests`
  - 리플렉션을 바꾼 `CombatRailwayContactSceneTests`·`CombatSceneEventPresenterTests`
- **Unity 확인 결과(2026-10-18, 사용자):** "A-3a Unity 묶음 문제 없는 것 확인했어". A-3a 끝.
- **커밋 때 함께 넣을 새 파일:** 위 새 파일 6개와 새 시험 1개(+ `.meta`).

### 12.5 A-3b (2026-10-18)

- 사용자: "A-3a Unity 묶음 문제 없는 것 확인했어. A-3b 진행해."
- **착수 직전 재대조:** 서명·만드는 곳·호출자·바꿀 시험을 코드와 맞춰 §6에 확정으로 적었다(§0.1-3). 설계 §9 I-2와 다른 점(타임라인·목표 표시는 사실 그리기에 남김)도 거기와 설계 §9.2에 적었다.
- **새 파일(`D/`, GUID 앞 8자리):** `ICombatUnitHudTarget`(`394633e3`), `CombatUnitHudFeedback`(`136f40cf`).
- **새 시험(`T/`):** `CombatUnitHudFeedbackTests`(7, `77f00ce6`, 관리 실행, 가짜 HUD).
- **`FieldUnitPresenter`:**
  - HUD 멤버 다섯(`ShowHudDamage`, `ShowHudCostPreview`·`ClearHudCostPreview`, `ShowAttackHealthPreview`·`ClearAttackHealthPreview`)과 상태 둘(`_attackHealthPreviewTargets`·`_hudPreviewActor`)을 HUD 피드백으로 옮겼다.
  - `HudFeedback` 읽기 속성이 처음 읽힐 때 만든다.
  - `RenderFacts` 첫머리, 풀 반환, `Release`의 두 지우기는 같은 자리에서 HUD 피드백을 부른다.
  - `ICombatUnitTargets.TryGetHud`를 구현한다.
- **`FieldUnitView`:** HUD 계약을 명시적으로 구현한다. HUD null 확인이 표시 안으로 들어갔다.
- **다시 연결한 것:**
  - 캐릭터 처리기: `FieldUnitPresenter` 대신 HUD 피드백을 받는다.
  - `CombatAttackFeedback(field, hud)`, `CombatMovementPlanningPresenter(field, hud, presentation, ghost, config)`.
  - `CombatSceneDisplay` 정리 목록 두 곳(생성자 그대로), 사건 Presenter.
  - Factory는 두 생성자 호출과 주석 한 줄을 바꿨다. 민형님 공유 문서 [§9](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md)에 적었다(미전달).
- **바꾼 시험(호출 대상·생성자만):** §6 확정 목록 그대로다. `CombatPointerMappingTests`는 표시 계약만 불러 바꾸지 않았다.
- **동작:** 없음.
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(전체 5,130개):** A-3a(반영 뒤) 대비 기존 시험 상태 변화 0. 새 HUD 피드백 시험 7개 PASS. 합계 PASS 2,782 · NATIVE 2,163 · FAIL 138 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **검토 에이전트 1개:** 정상 경로에서 동작이 달라진 곳은 없다고 판단했다. 확인한 것:
  - 옮긴 멤버 다섯을 한 줄씩 대조했다. null·Unity null, 피해 숫자만 켜짐 확인, `_views == null`, 반환 때 비용 대상은 그대로를 포함한다.
  - `RenderFacts`·`Release`·반환·Display 정리 목록의 호출 순서.
  - Factory가 `Configure` 전에 읽어도 안전하다.
  - 옛 이름·리플렉션이 `Assets/`에 남지 않았다.
- **검토 지적 처리:**
  - 반영: `ICombatUnitHudTarget` 문서의 "HUD가 없는 표시는 아무 일도 하지 않습니다"가 예상 체력에는 틀렸다(HUD가 없어도 체력 숫자에 보임). 피해 숫자·비용으로 좁혔다.
  - 반영: 설계 §9 I-2가 아직 "타임라인·목표 포함"이라 적고 있었다. 설계 §9.2에 실행 보정 한 줄을 더했다.
  - 기록만: 배치 피드백은 숨김(`SetHiddenEntity`) 때문에 아직 `FieldUnitPresenter`를 받는다. "입력 피드백이 HUD 계약만 받음"은 공격·이동 피드백에 해당한다. 덱 호버 화살표도 입력이 만드는 일시 HUD지만 이 계획 범위 밖이라 `FieldUnitPresenter`에 남았다.
  - 기록만: Factory는 두 줄에 주석 한 줄이 더해졌다(공유 문서에 적음).
  - 기록만(다음 정리 후보, §12.4 마지막 항목에 더함): `ICombatUnitTargets`가 찾기 둘(표시·HUD), 연출 값, 사실 배치를 함께 가져 ISP 냄새가 난다. HUD 시험 가짜의 멤버 7개가 쓰이지 않고, 창구 시험 가짜에 `TryGetHud`를 넣어야 했다. HUD 찾기를 좁은 계약으로 나누는 것도 함께 본다.
  - 기록만: 생성자 null 확인이 Unity null에서 C# null로 바뀐 곳이 있다(`CombatAttackFeedback`·이동 미리보기가 받던 `units`). 파괴된 Presenter가 들어오는 오류 경로에서만 다르고, Factory 앞의 참조 검사가 막는다.
- **검토 반영 뒤:** 바뀐 코드는 주석 한 줄이라 재빌드만 했다(오류 0·고유 경고 16개 같음). 관리 실행은 다시 하지 않았다.
- **Unity 확인(사용자, A-3b 묶음 §6):**
  - `AttackHealthPreviewDisplayTests`, `CombatPlacementFeedbackTests`, `CombatAttackFeedbackTests`, `CombatMovementDraftDisplayTests`, `CombatMovementPlanningDisplayTests`
  - 재생 묶음의 피해 숫자 경로(`CombatUnitDeathTests` 등)
  - `CombatUnitPresentationWindowTests`(가짜 찾기 변경)
- **Unity 확인 결과(2026-10-18, 사용자):** "A-3b Unity 묶음 문제 없는 것 확인했어". A-3b 끝.
- **커밋 때 함께 넣을 새 파일:** 위 새 파일 2개와 새 시험 1개(+ `.meta`).

### 12.6 A-4 (2026-10-18)

- 사용자: "A-3b Unity 묶음 문제 없는 것 확인했어. A-4 진행해."
- **착수 직전 재대조:** 경로 판정 순서, 형식 목록의 원본, 바꿀 시험을 코드와 맞춰 §7에 확정으로 적었다(§0.1-3). 시험 목록은 계획보다 넓었다. `CanPresent`를 덮던 가짜가 셋 더 있었고, Display 단정 4곳, 이동 표시 `Probe`가 있었다.
- **사용자 결정(2026-10-18, 질문 1회):** `CombatMovementViewTests.MissingNonMovementHandler_WarnsSkipsAndAcknowledges`를 삭제한다(§7 확정 블록에 이유).
- **새 파일:** `C/CombatEventRoutes.cs`(GUID 앞 8자리 `2c1774c7`). 내용은 `CombatEventRoute` 다섯과 `RouteOf`·`EventTypes`(19)·`IsEventType`이다.
- **새 시험(`T/`):** `CombatEventRoutesTests`(8, `e5478563`, 관리 실행).
- **바꾼 코드:**
  - `CombatEventHandlerTable`: `FactTypes`·`Contains`를 없앴다. 특별 처리기 다섯을 맞춘 뒤 나머지 사건 틀 형식에 사실 처리기를 둔다. `Add`는 사건 틀 형식이 아니면 거절한다.
  - `OrderedCombatPresentation`: 경고·건너뜀·시작 분기를 경로로 정한다. 사설 `CanPresent`·`IsMovement`를 없앴다.
  - `CombatSceneEventPlayback`: `CanPresent`를 없앴다. 기차 판별은 경로 표를 읽는다.
  - `ICombatEventPlayback`·`CombatSceneDisplay`·`CombatSceneEventPresenter`: `CanPresent`를 없앴다.
- **바꾼 시험:** §7 확정 목록 그대로다. 처리기 표 시험의 "19개 형식" 시험은 경로 표 시험으로 옮겼다. `Contains` 시험은 "사건 틀 형식마다 처리기, 그 밖은 기본 처리기" 시험으로 바꿨다.
- **Core 전달 문서:** [§5.45](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 천명님 작성 `FrenzyViewContractTests` 변경을 적었다. 검토에서 나온 C 단계(`92c2ddd7`)의 같은 시험 변경(걸음 완료 통지 서명)도 그때 빠져 함께 적었다.
- **동작:** 운영 경로는 없다. 옛 판정(이동 ∪ 기차 ∪ 처리기 표 19형식)과 새 경로 표가 같은 형식을 같은 길로 보낸다.
  - 시험 가짜만 달라진다. 모든 사건을 받던 `CombatViewSystemTests.FakeDisplay`와 내용 없는 이동 `Probe`가 이제 `CombatItemEvent`·`CombatFrenzyWaveEvent`를 받지 않는다.
  - 그 가짜를 거치는 Rig 가운데 두 사건을 만드는 것이 없음을 검토에서 확인했다.
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(전체 5,136개):** A-3b 대비 기존 시험 상태 변화는 아래 하나뿐이다.
  - 새 시험은 모두 PASS다. 경로 표 8개, 처리기 표의 새 시험 1개다.
  - 지운 시험은 셋이다. 처리기 표에서 옮긴 시험 둘, 사용자 결정으로 지운 이동 표시 시험 하나다.
  - 상태가 바뀐 것: `FrenzyViewContractTests.UnsupportedWaveMarker…`가 NATIVE에서 FAIL("No log scope is available")로 바뀌었다. GameObject를 없애 관리 실행이 `LogAssert`까지 가게 됐기 때문이다. 관리 실행기의 한계(`LogAssert`는 Unity Test Runner 전용, 같은 이유의 기존 FAIL 3개)이며 Unity 결과로 확인한다.
  - 합계 PASS 2,789 · NATIVE 2,161 · FAIL 139 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **검토 에이전트 1개:** 운영 경로의 동작 변화는 없다고 판단했다. 확인한 것:
  - 형식별 경로·처리기 대응을 A-4 이전 사본과 대조했다.
  - 필드 일부만 걸친 `Returned`와 null.
  - 가짜가 받는 사건 변화의 영향 없음.
  - 지운 시험의 보장이 다른 시험에 남아 있음.
- **검토 지적 처리:**
  - 반영: 계획 밖으로 더했던 "모든 구체 사건 형식 = 사건 틀 형식 + 알려진 건너뜀" 시험은 Core가 새 형식을 더하면 View 시험이 깨진다(기능 변경의 독립성). 옛 뜻("사건 틀 형식은 Core의 구체 형식", `SupersetOf`)의 `EventTypes_AreConcreteCoreEventTypes`로 바꿨다.
  - 반영: 처리기 표 시험의 목록 주석을 "사실 처리기로 가는 형식"으로 고쳤다. 쓰지 않는 using(처리기 표 시험·경로 표·Frenzy 시험·이동 표시 시험)과 빈 줄 하나를 정리했다.
  - 반영: Core 전달 문서 §5.45에 C 단계 변경도 함께 적었다(위).
  - 기록만: `CombatHitFollowUps.IsMovement`(`Moved`·`Returned`·`Relocated`)는 이름이 겹치지만 순서 판단용이라 다른 질문이다.
- **검토 반영 뒤:** 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,136개에서 반영 전 대비 차이는 바꾼 경로 표 시험 하나(`EventTypes_AreConcreteCoreEventTypes` PASS)뿐이다. 합계 PASS 2,789 · NATIVE 2,161 · FAIL 139 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **Unity 확인(사용자, A-4 묶음 §7):**
  - `FrenzyViewContractTests`, `CombatRailwayContactSceneTests`, 재생 묶음(§2.3)
  - 가짜를 바꾼 `OrderedCombatSchedulingTests`(Unity 쪽)·`CombatViewSystemTests`·`CombatBallistaAttackTests`·`CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatMovementViewTests`·`CombatSceneEventPresenterTests`
- **Unity 확인 결과(2026-10-18, 사용자):** "A-4 Unity 묶음 문제 없는 것 확인했어". A-4 끝.
- **커밋 때 함께 넣을 새 파일:** 위 새 파일 1개와 새 시험 1개(+ `.meta`).

### 12.7 A-4b (2026-10-18)

- 사용자: "A-4 Unity 묶음 문제 없는 것 확인했어. A-4b 진행해."
- **착수 직전 재대조:** Display의 재생 위임 열하나 중 시작 계열 여섯에 해제 뒤 예외(D9)가 있었다. 계획은 이것을 빠뜨렸다.
- **사용자 결정(2026-10-18, 질문 1회에 두 항목):**
  - D9 확인을 재생 조정으로 옮긴다.
  - §8 "착수 때 볼 것"(재생 조정의 선확인)은 지금처럼 둔다.
- **새 파일 없음.**
- **바꾼 코드:**
  - `ICombatBattleDisplay`: `ICombatEventPlayback` 상속을 빼고 `EventPlayback`을 더했다.
  - `CombatSceneEventPlayback`:
    - `ICombatEventPlayback`을 구현한다.
    - 해제 표시 `MarkReleased()`를 더했다. 시작 계열 여섯이 맨 앞에서 확인한다(`[CombatSceneEventPlayback] 씬 연결이 해제된 재생입니다.`).
  - `CombatSceneDisplay`:
    - 위임 열하나를 없애고 `EventPlayback => _eventPlayback`을 둔다.
    - `Refresh` 단정은 재생 조정을 직접 읽는다.
    - `Release`는 자기 해제 표시 직후, 정리 목록 전에 재생 조정에 알린다.
  - `CombatViewSystem.AttachDisplay`: `_presentation.Attach(display.EventPlayback)` 한 줄을 바꿨다.
  - 설명을 고쳤다: `ICombatEventPlayback`(구현·방어 위치), `ICombatPresentation.Attach`(넘기는 객체), 재생 조정 remarks.
- **바꾼 시험:**
  - `CombatViewSystemTests.FakeDisplay`: 재생 계약도 구현하고 `EventPlayback => this`를 돌려준다. 물려받은 가짜와 `LastAttachedPlayback` 단정은 그대로다.
  - `CombatSceneEventPresenterTests`·`CombatRailwayContactSceneTests`·`CombatMortarAssetTests`: Display를 재생 계약으로 쓰던 85곳(재생 멤버 호출 61, `Attach(_display)` 24)의 호출 대상을 `_display.EventPlayback`으로 바꿨다.
  - 검토에서 나온 `CombatSceneEventPresenterTests`(해제 뒤 `Play` 거절)의 예외 문구 앞머리 단정을 `[CombatSceneEventPlayback]`로 바꿨다.
- **동작:** 해제 뒤 사건 시작 거절의 예외 문구만 바뀐다. 앞머리 `[CombatSceneDisplay]`가 `[CombatSceneEventPlayback]`으로, 본문 "해제된 표시입니다"가 "해제된 재생입니다"로 바뀐다. 확인 순서·형식·대상 멤버는 같다.
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(전체 5,136개):** A-4(반영 뒤) 대비 상태 변화 0. 합계 PASS 2,789 · NATIVE 2,161 · FAIL 139 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **검토 에이전트 1개:** 예외 문구 말고는 동작 차이가 없다고 판단했다. 확인한 것:
  - 여섯 멤버 모두 해제 확인이 맨 앞이다(인자 null·완료·기차 확인보다 앞).
  - 판단·Tick·취소는 해제 뒤에도 확인하지 않는다.
  - 해제 순서, 연출 연결을 먼저 끊는 순서, 재생에 닿는 객체, 참조 동일성에 기대는 곳 없음.
  - 정규식 수정이 다른 것을 건드리지 않았다.
- **검토 지적 처리:**
  - 반영: `CombatSceneEventPresenterTests`의 해제 뒤 `Play` 시험이 옛 앞머리 `[CombatSceneDisplay]`를 단정했다. 관리 실행에서는 NATIVE라 드러나지 않는 Unity 실패였다. 새 앞머리로 바꿨다.
  - 반영: `ICombatEventPlayback`·`ICombatPresentation.Attach` 설명이 "구현은 Display, 재생 조정은 계약 미구현"으로 지금과 반대였다. 고쳤다.
  - 반영: Display remarks에 재생 멤버는 `EventPlayback` 쪽이라는 한 줄을 더했다. 설계 §9.2에 I-4 실행 보정 한 줄을 더했다.
  - 반영: 재생 조정의 해제 표시 이름을 `Release`에서 `MarkReleased`로 바꿨다. 자원 정리는 하지 않으므로 오해를 막는다.
  - 기록만: 해제 상태가 두 곳에 있다(Display `_isReleased`, 재생 조정 `_released`). 설정하는 곳이 `Display.Release` 하나이고 막는 멤버가 겹치지 않는다.
- **검토 반영 뒤:** 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,136개에서 반영 전 대비 상태 변화 0(바꾼 단정은 Unity 전용 시험). 합계 PASS 2,789 · NATIVE 2,161 · FAIL 139 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **Unity 확인(사용자, A-4b 묶음 §8):**
  - `CombatViewSystemTests`, 재생 묶음(§2.3)
  - 호출 대상을 바꾼 `CombatSceneEventPresenterTests`·`CombatRailwayContactSceneTests`·`CombatMortarAssetTests`
- **Unity 확인 결과(2026-10-18, 사용자):** "A-4b Unity 묶음 문제 없는 것 확인했어". A-4b 끝.

### 12.8 A-5 (2026-10-18)

- 사용자: "A-4b Unity 묶음 문제 없는 것 확인했어. A-5 진행해."
- **착수 직전 재대조:** 씬 값, 검증 위치, 바꿀 시험을 코드와 맞춰 §9에 확정으로 적었다(§0.1-3). 씬 값과 지금 코드 기본값이 넷에서 달랐다(걸음 0.2/0.12, 소환 대기 0.3/0.2, 소환 1/0.6, 포효 1/0.4). 옮긴 것은 씬 값이다.
- **사용자 결정(2026-10-18, 질문 1회에 두 항목):**
  - Factory 시험은 실제 DB 값으로 돈다.
  - `CombatEnemyPresentationTests.MovementTurnSetting_InheritsSerializedEnemyTurnValue`를 삭제한다.
  - 대조 결과, 실제 표시를 조립하는 시험은 모두 실제 `Combat.unity`를 열어 이미 씬 값으로 돌고 있었다. 그래서 시험의 시간이 바뀌지 않는다.
- **새 파일:**
  - 코드(GUID 앞 8자리): `CombatPresentationSettingsDataSO`(`7a591055`), `CombatPresentationSettingsDataTableSO`(`3125ff65`), `CombatPresentationSettingsTable`(`fee49908`), 코드 폴더 meta 2개(`843314df`·`7ad1af38`)
  - 에셋: `DataBaseSO/ViewGroup/CombatPresentationSettings/`(폴더 `3af0cb70`, `Item` 폴더 `abef0604`)의 `CombatPresentationSettingsDataTable.asset`(`0ca5ccef`)과 `Item/CombatPresentationSettings_Default.asset`(`d71a5b40`). Unity 에디터 없이 가시밭 표 에셋과 같은 YAML 모양으로 썼다.
  - `ViewGroup.asset`에 표 한 줄을 더했다.
- **새 시험(`T/`):**
  - `CombatPresentationSettingsDataTests`(관리 실행, 15개, `5073b9f0`)
  - `ViewDataTableTests`에 3개
  - `DataBaseAssetTests`에 확인 2줄
- **바꾼 코드:**
  - 설정 Data가 값 11개와 조건 함수의 원본이다. 표가 만들 때 `Validate()`를 부른다.
  - 런타임 검사(사건 Presenter 걸음 시간, 적 간격, 능력 동작 생성자)는 그 조건 함수를 부른다(조건 원본 하나).
  - `CombatDisplayTables.PresentationSettings`를 더했고, 능력 동작 값은 `CombatAbilityMotionSettings.From`이 만든다.
  - `CombatSceneRoot`에서 값 11개·읽기 속성·`CreateAbilityMotionSettings`·`ValidatePresentationTiming`을 없앴다.
  - Factory는 설정 검사 → 참조 검사 순서로 시작하고, 설정에서 값을 읽는다.
- **공용·다른 작업자 파일:**
  - ServiceKeys·DB Factory·`ViewDataBase`·`ViewGroup.asset`·전투 표시 Factory. 민형님 공유 문서 [§10](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md)에 적었다(미전달, 값 조정 위치 팀 안내 포함).
  - `IViewQuery` 가짜 7개에 한 줄씩 더했다(6개는 호진님 시험). [호진님 공유 메모](../handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md)(미전달).
- **바꾼 시험:** §9 확정 목록 그대로다.
  - `CombatSceneEventPresenterTests`의 걸음·회전 시간 읽기는 착수 때 본 1곳이 아니라 16곳이었다(목록이 잘려 보였음). 모두 설정 값으로 바꿨다.
- **실제 에셋 값 대조:** `Combat.unity`의 `CombatSceneRoot` 11줄과 새 항목 에셋 11줄이 줄 단위로 같음을 확인했다. 시험으로 고정하지 않는 이유는 §9 확정 블록에 적었다.
- **동작:** 유효한 값에서는 런타임 동작이 같다(값·순서·예외 형식·`ParamName`·문구).
  - 잘못된 값의 실패 시점이 전투 진입에서 부트의 DB 생성으로 앞당겨진다. 계획대로이고 다른 설정형 표와 같다.
  - `Combat.unity`의 옛 직렬화 값 11줄은 다음 씬 저장 때 Unity가 지운다.
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음. 빌드 설정에 DB 새 폴더를 더했다(`after-a5.targets`).
- **관리 실행(전체 5,153개):** A-4b 대비 기존 시험 상태 변화 0.
  - 이름 바꾼 검증 시험 14개(NATIVE)와 지운 시험 1개 말고는, 새 시험만 늘었다.
  - 새 Data 시험 15개는 PASS다.
  - 새 `ViewDataTableTests` 3개 가운데 2개는 NATIVE, `…RequiresExactlyOneItem`은 FAIL이다. 같은 모양의 기존 `InputSettings_RequiresExactlyOneItem`·`ConfigTables_RequireExactlyOneItem`과 같은 관리 실행기 한계이며, Unity 결과로 확인한다.
  - 합계 PASS 2,804 · NATIVE 2,162 · FAIL 140 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **검토 에이전트 1개:** 유효한 값에서는 런타임 동작이 바뀌지 않았다고 판단했다. 확인한 것:
  - 씬 값 11개 일치, 에셋 YAML 키·GUID 연결·폴더 meta·CRLF.
  - DB 층이 런타임 View를 모름, 남은 소비자 없음.
  - Factory 시험이 모두 실제 씬을 엶.
- **검토 지적 처리:**
  - 반영: 계획 확정 블록은 "실제 에셋 값이 씬 값과 같음"을 시험한다고 적었으나 구현은 범위만 확인했다. 확정 블록을 고쳐 적었다(1회 대조로 확인, 기획 조정 값이라 시험으로 고정하지 않음).
  - 반영: 값 복사 시험이 기본값으로 비교해, 같은 값 1이 셋이라 인자 자리가 바뀌어도 잡지 못했다. 값마다 다른 수를 넣었다.
  - 반영: 깊이·바퀴 수 오류 문구가 설정과 능력 동작 생성자 두 곳에 있었다. 전용 조건 함수(`ValidateRiseDepth`·`ValidateRiseTurns`) 한 곳으로 모았다.
  - 반영: `Validate()` 안의 검사 순서를 옛 씬 검사·능력 동작 생성자 순서와 맞췄다. 여러 값이 함께 잘못됐을 때 먼저 알리는 값이 같다.
  - 반영: CLAUDE.md의 옛 값 위치 문구를 고쳤다.
  - 기록만: 위임 래퍼(사건 Presenter·적 간격)의 전체 이름 표기가 다른 파일의 `using` 표기와 섞여 있다.
  - 기록만: Mortar 시험이 실제 에셋의 메모리 인스턴스를 바꾼다(finally로 되돌림, dirty 표시 없음).
  - 기록만: 새 폴더 `ViewGroup/CombatPresentationSettings`가 기존 `ViewGroup/CombatPresentation`(공격 프리셋) 옆이라 이름이 헷갈릴 수 있다.
- **검토 반영 뒤:** 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,153개에서 반영 전 대비 상태 변화 0(값 복사 시험은 이름 그대로 PASS). 합계 PASS 2,804 · NATIVE 2,162 · FAIL 140 · UNSUPPORTED 47. 실행이 남긴 에셋은 없었다.
- **Unity 확인(사용자, A-5 묶음 §9):**
  - `DataBaseAssetTests`, `ViewDataTableTests`, 재생 묶음(§2.3)
  - Combat 진입(Play From Boot)
  - 바꾼 시험: `CombatEnemyPresentationTests`·`CombatMortarAssetTests`·`CombatSceneEventPresenterTests`·`CombatDisplayReuseTests`·`TerrainOperationalDisplayTests`
  - 가짜 7개가 있는 시험 파일
- **Unity 확인 결과(2026-10-18, 사용자):** "A-5 Unity 묶음과 Combat 진입 문제 없는 것 확인했어". A-5 끝. A 단계(바탕·개선) 전체 끝.
- **커밋 때 함께 넣을 새 파일:** 위 코드 3개·코드 폴더 meta 2개, 에셋 2개와 에셋 폴더 meta 2개(+ 각 `.meta`), 새 시험 1개(+ `.meta`). 코드와 에셋은 한 커밋이어야 한다.

### 12.9 B-0 (2026-10-18)

- 사용자: "A-5 Unity 묶음과 Combat 진입 문제 없는 것 확인했어. B-0 진행해."
- **작성(코드 변경 없음):** [조각·조립 상세 설계](../specs/2026-10-18-combat-presentation-pieces-design.md)
  - §1 지금 상태를 코드와 대조했다: 포효 순서(능력 재생 안의 상태), 소환 순서, 소리를 정하는 곳 넷, 피격·사망의 대여·사실 반영 순서.
  - BP1~BP9: 조각 계약, 시점·시간 규칙, 코드 조립, 실행, 조각 실행 집합 재생기, 출처 수명 창구, 포효·소환 조립(B-2), 사건 소리(B-3), 피격 플래시(B-4).
  - §3 겹침 규칙에서 바뀌는 행, §4 원칙 점검과 §4.1 변경 독립성 시나리오, §5 위험, §6 결정 Q-B1~Q-B7, §7 Phase 개요.
  - 계획 §10을 B-1~B-4 Task(새 파일·바꿀 파일·동작·시험·Unity 묶음·공유)로 다시 썼다.
- **빌드·관리 실행:** 코드 변경이 없어 돌리지 않았다.
- **검토 에이전트 1개.** 코드 사실, 실행의 대여 시점, 출처 수명의 Gate 동등성, 재생기 자리, B-3 → B-4 순서, 다른 작업자 영역을 건드리지 않음, 시험 분류는 맞다고 했다. 지적 11건은 모두 반영했다.
  - 외부 진행 조각의 끝: 처음에는 Tick에서 확인하게 썼다. 그러면 대상 없는 포효의 완료와 사망·피격 완료가 한 프레임 늦어진다(공격 재생기가 그 프레임에 false를 돌려줌). 완료 콜백으로 바로 전달하게 고쳤다(세대 확인, 시작 안에서 끝나도 됨, BP1·BP2, 계획 B-1).
  - 반응 자세 대여와 사망 유지는 지금처럼 처리기·반응 재생기가 가진다. 그래서 O13이 그대로다. 실행은 지금 반응을 시작하는 자리에서 시작한다(BP1·BP4).
  - §3이 O9·O14·O15를 "그대로"로 적은 것은 틀렸다. 유닛 실행이 주체 막기·동시 사망·결과 시작 규칙을 함께 따라야 한다. 행을 더했다. 조립에 사망 실행 표시를 넣었다. 막는 이유 `PieceSubject`를 더했다. 주체 기준 막기는 규칙 함수 하나를 반응 재생기와 함께 쓴다(B-3).
  - 소리: 프리셋 함수는 `CombatEventAudio`에 두고 `switch`만 옮긴다(입력 쪽·현강님 시험이 부름). 표시 갱신의 배경음 프리셋은 범위 밖이라고 기록했다.
  - 다시 묻는 질문이 초판과 달라진 이유를 Q-B1·Q-B4에 적었다(SO 조립 폐기, Q-R18). Q-B4 A면 Q-B7 A가 함께 정해진다는 것도 적었다. §7 B-3·B-4 행도 고쳤다.
  - DR 번호: 색 Scope 반환은 DR-004다(DR-005는 LightHandle). 새 설계 2곳과 상위 설계 2곳을 고쳤다.
  - B-4 외형 층: 지금 투명도 복원이 PropertyBlock을 통째로 되돌린다. 그래서 합성 소유자가 필요하다고 적었고, 실제 Renderer 합성·양쪽 순서 복원 Unity 시험을 계획에 넣었다. Q-B5 A로는 "구조물은 플래시만"을 바로 표현할 수 없다는 점도 기록했다.
  - BP7 동등 표에 행을 더했다: 대상 없는 포효, 소환의 끝 순서, 소환 `Instant` 값. 소환 순서에 빠졌던 자세 복원과 출처 실행의 기다림 열 "—"도 고쳤다.
  - 시간 규칙을 더 정했다: Tick 반환의 "행동 실행 있음"은 Tick 전에 판정한다. 신호 뒤 지연을 세기 시작하는 때를 적었다. 같은 순간 항목은 목록 순서대로 동기로 시작한다(사망 소리가 사실 반영 전에 남).
  - 계획: B-1에서 공격 재생이 출처 수명 계약을 구현한다(옛 포효 메서드가 그것을 부름, 동작 없음). B-2의 옛 이름 설명 정리 목록과 B-4 가짜 목록(`CombatUnitHudFeedbackTests`)을 더했다.
  - §4.1 변경 독립성 시나리오를 더했다. "조각 하나 + 조립 한 줄"은 기존 채널과 `End` 신호로 되는 경우만이라고 적었다. 막기 규칙이 두 곳에서 답하는 SSOT 주의도 적었다.
- **사용자 결정 대기:** Q-B1~Q-B7(설계 §6, 모두 권장 A). 결정을 받은 뒤 B-1에 착수한다. A가 아닌 답이 있으면 해당 Task를 먼저 고친다.
- **커밋 때 함께 넣을 새 파일:** 설계 문서 `specs/2026-10-18-combat-presentation-pieces-design.md` 1개.

### 12.10 B-1 (2026-10-18)

- 사용자: "Q-B1~B7은 추천대로, R2,3,5 도 추천대로 하는데 이대로 계획 후 진행했을 때의 기대값 먼저 보여주고 시작해"
- **착수 전:** 결정을 설계 §6.1에 기록했다. 기대값(화면·구조·원본·시나리오·평가 점수·검증·위험)은 §10.0에 적고 먼저 보여 드렸다.
- **사용자 결정(착수 중, 질문 1회):** 주체 기준 막기 규칙 함수를 B-3이 아니라 B-1에서 뽑는다. B-1이 유닛 실행을 만들어서, B-3까지 기다리면 같은 규칙이 두 벌 생기기 때문이다. §10.1·§10.3과 설계 BP5를 고쳤다.
- **회귀 기준선을 다시 잡았다.**
  - 착수 뒤 사용자가 S-2~B-0을 커밋했고(`c417e9ee`), 병합이 두 번 있었다: 현강님 "밤 시간 체커기"(`ac96b03`), 천명님 core-system.
  - 그래서 A-5 결과는 기준이 아니다. HEAD `364936a1`에서 B-1 파일만 되돌린 사본으로 기준선을 빌드했다(새 파일 제외, 바꾼 파일 9개는 `git show HEAD:` 사본).
  - 기준선 관리 실행: 전체 5,273개, PASS 2,911 · NATIVE 2,196 · FAIL 117 · UNSUPPORTED 49.
- **새 파일(`C/`, GUID 앞 8자리):**
  - `ICombatPresentationPiece`(`b8344209`), `CombatPieceStart`(`fb4b80fb`, 시점·신호), `ICombatSourceLifetime`(`425d4b09`)
  - `CombatSubjectBlocking`(`517b0c87`, 계약 `ICombatSubjectActivity` + 규칙 함수)
  - `CombatComposition`(`47e54b76`, 채널·범위·완료 방식 포함), `CombatPieceContext`(`cb059305`), `CombatPieceRun`(`a9003075`), `CombatPieceRunPlayer`(`9b79ef6a`)
- **새 시험(`T/`):** `CombatPieceRunTests`(30개, `3275f3f8`), `CombatPieceRunPlayerTests`(13개, `a1bed7e8`).
- **바꾼 코드:**
  - `CombatStartQuery`: 막는 이유 `PieceBusy`·`PieceSubject`.
  - `CombatPresentationPlayers`: `Create(attack, ability, pieces, step, reaction)`.
  - `CombatAttackPlayback`: `ICombatSourceLifetime`을 구현한다. `BeginRoar`·`EndRoar`·`AbortRoar`는 그것을 부른다(Gate 호출과 클로저가 같음).
  - `CombatReactionPlayer`: 주체 기준 막기를 `CombatSubjectBlocking.Blocks`로 바꿨다(식이 같음).
  - `CombatSceneEventPresenter`: 조각 실행 재생기를 조립해 능력 자리 뒤에 넣는다. 아직 조립을 넘기는 처리기는 없다.
  - R3: `CombatEnemyPresentationPacing.BeforeEvent(e, actorId, owner)`. Presenter가 처리기 `ActorOf`로 행위자를 한 번 구해 넘긴다. 형식별 ID 꺼내기를 없앴다.
  - R5: `CombatSceneDisplayFactory`가 사건 Presenter를 이름 있는 인자로 만든다(값·순서 같음). 민형님 Factory 공유 문서 §11.
- **바꾼 시험:** `CombatPresentationPlayersTests`(다섯 자리), `CombatEnemyPresentationPacingTests`(호출 36곳에 행위자 ID, 기대값 그대로).
- **동작:** 없음. 조각 실행 재생기는 쉬고 있다(막지 않음, Tick true, 완료, 정리할 것 없음).
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(전체 5,310개, 검토 반영 전):** HEAD 기준선 대비 기존 시험 상태 변화 0, 새 시험 37개 PASS. 실행이 남긴 에셋은 없었다.
- **검토 에이전트 1개:** 높음 0. 확인한 것:
  - 쉬는 재생기가 기존 결과를 바꾸지 않음.
  - 시간 규칙이 지금 포효와 같음: 대기 → 커짐 남은 시간(같은 float 계산), 커짐 끝은 남은 시간 버림, 공격 Tick 안 박치기 끝 → 작아짐은 그 프레임 0(O4), 박치기 시작 실패 → 작아짐 바로, 길이 0 사슬.
  - Tick 반환 판정 자리, `FindBlock` 순서, BP4 규칙(대여·출처·끝 순서·취소·사라짐·세대·외부 끝), 재진입, 프레임마다 할당 없음.
  - 바꾼 파일의 동작 같음, CRLF·BOM 없음·`.meta`.
- **검토 지적 처리:**
  - 반영(중간): 신호 뒤 지연. 설계는 "조각 Tick 안에서 온 신호면 그 프레임의 남은 시간도 지연에 들어간다"였는데, 조각은 남은 시간을 알리지 않아 코드는 버린다. 지금 포효(커짐 끝 → 박치기)와 같은 쪽인 코드를 기준으로 설계 BP2와 주석을 바로잡았다. B 단계 조립에는 `After(…, d > 0)`가 없어 프레임 차이가 없다. 정확한 남은 시간이 필요한 조립이 생기면 조각 계약에 남은 시간 보고를 더한다.
  - 반영: 시작 실패·취소 정리 순서를 "출처 비우기 → 대여 되돌림"으로 바꿨다. 지금 포효 처리기(`AbortRoar` → `RestoreScale`)와 취소(공격 재생기 Gate 비우기 → 능력 동작 크기 복원) 순서와 같다. Gate 비우기에 콜백이 없어 결과도 같다.
  - 반영: 채널 분기를 `switch`로 바꾸고 모르는 채널은 예외로 막았다(B-4에서 외형 층을 더할 때 자세로 잘못 빌리지 않게).
  - 반영: "끝 신호가 오지 않는 항목"은 지금 생길 수 없다(신호 끝 하나, 항목 단위 취소 없음). 설계·계획·클래스 설명에 그렇게 적고 시험 목록에서 뺐다.
  - 반영: 시험 6개 추가. 출처 실행의 밖 대여 취소, 세대 변경 뒤 일반 완료 막힘·`EndSource` 세대, 완료 콜백 안의 `Cancel`, Tick 중 시작한 실행은 그 프레임 진행 안 함, 다른 항목 시작 중의 외부 끝, 일반 완료가 실행 시작 목록 중간에 가는 순서(설계 BP2 구현 보충에 적음).
  - 반영: 설명 셋(`CombatStartQuery.Unavailable`, Presenter 두 곳에 조각 실행 자리), `CombatPieceContext.EndCallback` 설명.
  - 기록만: 실패 경로 차이 하나. 프레임 시작에 외부 항목(박치기)을 기다리던 실행의 대여가 그 프레임 조각 Tick 전에 밖에서 취소되면, 옛 능력 재생기는 다음 프레임에, 새 재생기는 같은 프레임에 예외를 낸다. 포효가 조립으로 바뀌는 B-2부터 생긴다.
  - 기록만: 실패 경로 개선 하나. 옛 포효는 능력 동작 시작 중 예외가 나면 능력 상태가 남았다. 새 실행은 정리하고 목록에서 뺀다. B-2 기록에도 적는다.
  - 기록만: 능력 재생기와 조각 실행 재생기가 행동 막기·Tick 반환 규칙을 함께 갖는다. 임시 중복이며 B-2에서 능력 자리를 없애며 정리한다.
- **검토 반영 뒤:** 전체 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,316개에서 HEAD 기준선 대비 기존 시험 상태 변화 0, 새 시험 43개 PASS. 합계 PASS 2,954 · NATIVE 2,196 · FAIL 117 · UNSUPPORTED 49. 실행이 남긴 에셋은 없었다.
- **병합 관찰(고치지 않음):**
  - 현강님 `CombatPhaseTransitionPresenter`가 재생 조정(`CombatSceneEventPlayback`)에서 전투 진입·밤 끝 턴 사건을 먼저 받는다. 화면 전환을 재생하고, 기존 사건 처리(`BeginEvent`)를 그대로 불러 두 완료를 합친다. 전환 중에는 모든 사건 시작을 막는다.
  - 기존 처리가 그대로 돌아 동작 문제는 없다. 다만 "사건이 어떻게 보이나"(경로 표)와 시작 조건(실행 목록) 밖에 경로·막기 규칙이 하나 더 생겼다. 화면 전환 조각으로 턴 사건 조립에 넣을 후보다(B 단계 뒤 별도 결정).
- **범위 밖 변경:** `FMODStudioCache.asset`, `TargetRenderTexture.renderTexture`, `LiberationSans SDF - Fallback.asset`은 에디터가 바꾼 것이다(커밋 때 사용자 판단).
- **Unity 확인(사용자, B-1 묶음):**
  - 새 시험·바꾼 시험: `CombatPieceRunTests`, `CombatPieceRunPlayerTests`, `CombatPresentationPlayersTests`, `CombatEnemyPresentationPacingTests`, `CombatReactionPlayerTests`, `CombatAbilityPlayerTests`
  - 재생 묶음(§2.3): `CombatSceneEventPresenterTests`, `CombatEnemyPresentationTests`, `CombatUnitDeathTests`, `CombatUnitReactionTests`, `CombatUnitReactionSetTests`, `CombatRailwayContactSceneTests`, `CombatMovementViewTests`, `CombatBallistaAttackTests`, `CombatMortarPresentationTests`, `CombatStandardAttackTests`, `CombatCodeMotionHitTests`
  - 포효 수명 메서드가 출처 창구를 거치므로 보스 포효·소환 시험(`CombatAbilityMotionPlaybackTests`, `CombatSceneEventPresenterTests`의 포효·소환)
  - Combat 진입(Play From Boot)
- **Unity 확인 결과(2026-10-18, 사용자):** "문제없고". B-1 끝. 다음은 B-2(§10.2)이며 다른 세션에서 이어 한다(사용자).
- **커밋 때 함께 넣을 새 파일:** 위 코드 8개와 시험 2개(+ `.meta`).

### 12.11 B-2 (2026-10-18)

- 사용자: "여기서 할게 B-2 진행해"(다른 세션에서 이어 하려던 것을 이 세션에서 계속).
- **새 파일(`C/`, GUID 앞 8자리):** `CombatScalePiece`(`a5990138`), `CombatRisePiece`(`5598b42e`), `CombatHighlightClearPiece`(`ca1af8a1`), `CombatFacingAttackPiece`(`c32d914b`).
- **새 시험(`T/`):** `CombatScalePieceTests`(4개, `08a28fab`), `CombatRisePieceTests`(4개, `d81ecaa2`), `CombatAbilityCompositionTests`(23개, `62f7e168`).
  - 계획은 `CombatRoarCompositionTests`였으나 소환 조립도 함께 다뤄 이름을 `CombatAbilityCompositionTests`로 했다.
- **바꾼 코드:**
  - `CombatRoarEventHandler`·`CombatSummonEventHandler`: 정적 `Compose`가 설계 BP7의 조립을 만들고, 처리기는 그것을 조각 실행 재생기에 넘긴다. 순서·값 참조는 `Compose` 한 곳이다.
  - `CombatAttackPlayback`: `BeginRoar`·`EndRoar`·`AbortRoar`를 없앴다. `TryBeginFacing(roar, …)`는 `TryBeginFacingMotion(actorId, source, …)`이다. 이 클래스는 포효 형식을 모른다.
  - `CombatPresentationPlayers.Create(attack, pieces, step, reaction)`, `CombatStartQuery`에서 `AbilityBusy` 삭제, `CombatSceneEventPresenter`에서 능력 동작 필드·능력 재생기 삭제.
  - 설명 정리: `CombatHitGate`, `CombatPieceRun`, `CombatPieceRunPlayer`, `CombatAttackPlayer`.
- **지운 파일(계획 범위, 우리 코드):** `C/CombatAbilityMotionPlayback.cs`, `C/CombatAbilityPlayer.cs`, `T/CombatAbilityMotionPlaybackTests.cs`(20개), `T/CombatAbilityPlayerTests.cs`(11개), 각 `.meta`.
- **지운 시험 31개의 대응**(이름 앞 `Comp` = `CombatAbilityCompositionTests`, `Run` = `CombatPieceRunTests`, `Player` = `CombatPieceRunPlayerTests`):
  - 설정 검사 7개 → `Comp` 같은 이름 7개.
  - 포효 대기·남은 시간 → `Comp.Roar_WaitsDelayAfterHighlight_BeforeGrowing_AndCarriesTheRemainder`.
  - 등장 → `CombatRisePieceTests.Rise_StartsAtDepth_WaitsDelay_ThenRisesWhileSpinning_AndRestoresOnce`.
  - 포효 커짐·공격·작아짐 → `Comp.Roar_Grows_AttacksWhileLarge_ThenShrinks_ClearsTheHighlight_RestoresAndClosesTheSource`.
  - 공격 없이 바로 작아짐 → `Comp.Roar_WithoutAFacingMotion_ShrinksRightAfterGrowing`.
  - 음수 시간 → `CombatScalePieceTests.NegativeTime_DoesNotAdvance`.
  - 즉시 값 → `CombatRisePieceTests.InstantSettings_EndInsideBegin`, `Comp.Summon_InstantSettings…`, `Comp.Roar_InstantSettings_Without/WithAFacingMotion…`.
  - 공격 중 취소 → `Comp.Roar_CancelDuringTheMotion…`(취소 뒤 새 실행 시작 포함).
  - 세대 변경 → `Comp.Summon_GenerationChange…`, `Run.LateExternalEnd_AfterTheGenerationChanged_IsIgnored`.
  - 표시 사라짐 → `Run.LeaseCancelledOutside_StopsTheRun_AndTheNextTickThrows`(세 프로필 `Unavailable`·최종 사실 막기), `Run.LeaseCancelledOutside_RestoresTheOtherLeasesOnce`, `Run.SourceRun_LeaseCancelledOutside…`.
  - 진행 중 시작·null 입력 → `Player.TryBegin_RejectsASecondActionRun…`, `Comp.Compose_RejectsMissingParts`, 조각 생성자 시험.
  - 공격 대기 속성 3개(`IsWaitingForAttack`) → 속성이 없어졌다. O4는 `Comp.Roar_FacingEndBeforeThePieceTick_GivesTheShrinkNoTimeThatFrame`과 `Run.ExternalEndBeforeThePieceTick…`, 시작 실패는 `Comp.Roar_WithoutAFacingMotion…`이 지킨다. 등장의 "공격 대기 아님"은 대응이 필요 없다.
  - 능력 재생기 쉼 → `Player.Idle_BlocksNothing_AndTicksThrough`(세 프로필).
  - 히트 전·뒤 Tick 반환(O3) → `Comp.Roar_TickReturn_FollowsTheSourceOverlapAtFrameStart`(2), `Player.Tick_ReturnsTheSourceFrameStartValue…`, `Player.Tick_PassesThrough…`.
  - 프레임 시작 공격 대기(O4) → `Comp.Roar_FacingEndBeforeThePieceTick…`.
  - 겹침 프로필 2개·결과/동시 사망 → `Player.ActionRun_BlocksStarts_UntilTheSourceAllowsOverlap`(4), `Player.ActionRun_BlocksConcurrentDeaths…`.
  - 사라짐 → 위 `Run.LeaseCancelledOutside…`. 취소 → `Comp.Roar_CancelDuringTheMotion…`. 생성자 → `Player.Constructor_RejectsMissingParts`, `Run.Run_Constructor_RejectsMissingParts`.
- **동작:** 설계가 허용한 차이 셋만 있다.
  - 끝에서 강조 끄기와 대여 복원의 앞뒤(같은 프레임). 포효: 옛 크기 복원 → 강조 끄기 → 출처 종료, 새 강조 끄기 → 크기 복원 → 출처 종료. 소환: 옛 자세 복원 → 강조 끄기 → 완료, 새 강조 끄기 → 완료 → 자세 복원.
  - 막는 이유 이름 `AbilityBusy` → `PieceBusy`.
  - 실패 경로: 박치기를 기다리던 실행의 대여가 그 프레임 조각 Tick 전에 밖에서 취소되면 예외가 한 프레임 일찍 난다(§12.10 기록).
  - 개선(실패 경로): 옛 포효는 능력 동작 시작 중 예외가 나면 상태가 남았다. 새 실행은 정리한다.
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음. Unity csproj가 지운 파일을 아직 적고 있어 관리 빌드 설정에서 뺐다(`after-b2.targets`, Unity는 다시 만들 때 반영).
- **관리 실행(전체 5,316개, 검토 반영 전):** B-1 결과 대비 기존 시험 상태 변화 0. 지운 31개가 빠지고 새 31개 PASS. 실행이 남긴 에셋 없음.
- **검토 에이전트 1개:** 높음 0·중간 0·낮음 6. 옛 능력 경로와의 동작 동등(대여 실패 → 머묾, 출처 먼저, 대기 남은 시간 float 순서, 커짐 끝 남은 시간 버림, O4, 박치기 시작 실패, 즉시 값, Tick 반환, `FindBlock`, Gate 완료, 취소·시작 실패 정리, 세대, 람다의 세대·Mapper 시점), 소환 식·완료, 제거 완결성, 원칙(포효 순서·값 참조는 `Compose` 한 곳, B-1 임시 중복 해소), 스타일을 확인했다.
- **검토 지적 처리:**
  - 반영: 옮기며 빠진 단정 보강. 실행 재생기·실행 생성자 null, 사라짐의 세 프로필 `Unavailable`·최종 사실 막기·다른 대여 1회 복원, 겹침이 있어도 결과 시작은 막힘, 쉼 상태의 세 프로필, 취소 뒤 새 실행 시작.
  - 반영: 공격 재생 `BeginSource` 설명에 남은 포효 수명·정책 설명을 빼고 포효 조립을 가리켰다.
  - 반영: 시험 설명의 "지금 능력 재생기"를 "옛"으로 고쳤다.
  - 반영: 등장 조각이 설정 전체 대신 원시 값(대기·시간·깊이·바퀴)을 받는다(ISP). 값 조건은 설정 Data 조건 함수를 부른다(크기 조각도 같음, 조건 원본 하나).
  - 기록(설계 BP2 구현 보충): 일반 완료는 마지막 기다림 항목이 끝난 자리에서 가고 전체 종료는 그 뒤다. 소환 완료 순간에는 재생기가 아직 실행을 가진다. 순차 재생은 상태만 바꿔 화면 차이가 없다. B-3에서 완료 콜백 안에서 시작 조건을 묻는 소비자가 생기면 다시 본다.
  - 기록: 대기 → 커짐 프레임에서 크기 조각 시작이 배율 1을 한 번 쓴 뒤 남은 시간 값을 쓴다. 같은 프레임 안 중간 쓰기라 그리기는 없다.
- **검토 반영 뒤:** 전체 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,320개에서 B-1 대비 기존 시험 상태 변화 0, 지운 31개 빠짐, 새 35개 PASS. 합계 PASS 2,958 · NATIVE 2,196 · FAIL 117 · UNSUPPORTED 49. 실행이 남긴 에셋 없음.
- **공용·다른 작업자 파일:** 없음(Factory·ServiceKeys·DB 변경 없음, 사건 Presenter 생성자 서명 그대로).
- **Unity 확인(사용자, B-2 묶음):**
  - 새 시험·바꾼 시험: `CombatScalePieceTests`, `CombatRisePieceTests`, `CombatAbilityCompositionTests`, `CombatPieceRunTests`, `CombatPieceRunPlayerTests`, `CombatPresentationPlayersTests`
  - 포효·소환: `CombatStandardAttackTests`·`CombatCodeMotionHitTests`(포효·넉백 동시, 히트 타이밍 Q1 회귀), 포효·소환이 있는 `CombatSceneEventPresenterTests`, 보스 활성화 묶음
  - 재생 묶음(§2.3)
  - 보스 스테이지 재생(Play From Boot "Dev Battle Stage = 보스 스테이지"): 소환 등장, 포효(강조 → 대기 → 커짐 → 박치기 → 작아짐 → 강조 끔), 포효 피해·넉백이 작아지기와 겹침
- **커밋 때:** 새 코드 4개·시험 3개(+ `.meta`), 지운 파일 4개(+ `.meta`).

### 12.12 B-3 (2026-10-18)

- 사용자: "B-2 Unity 묶음과 보스 재생 문제 없는 것 확인했어. B-3 진행해." B-2 끝.
- **새 파일(`C/`, GUID 앞 8자리):** `CombatEventSounds`(`8769479f`: "무엇을 내나" 표 `For`·`Movement`, 국면 배경음 `BgmPreset`, 단서 `CombatSoundCue`), `CombatEventSoundPiece`(`f940634a`), `CombatReactionPiece`(`a86e71b0`).
- **새 시험(`T/`):** `CombatEventSoundsTests`(12개, `4763cc27`), `CombatDeathCompositionTests`(6개, `4eb1cedc`). `CombatPieceRunTests`에 2개 추가(검토 반영).
- **바꾼 코드:**
  - `CombatEventAudio`: `switch`를 표로 옮기고 단서를 AudioSystem에 내기만 한다. 프리셋 함수(`UnitPreset`·`AttackPreset`)는 그대로다.
  - `CombatCharacterEventHandler`: 사망은 사망 조립(`ComposeDeath`: 유닛 실행·사망 실행·일반 완료, 0 사망 반응(외부 진행, 기다림) → 1 사건 소리(같은 순간, 기다리지 않음))을 조각 실행 재생기로 진행한다. 사망 유지·반응 자세·사실 반영은 처리기 그대로. 직접 `_sound.Play(death)` 호출을 없앴다. 피격 경로는 그대로(B-4).
  - R2 (a): `CombatSceneDisplay.Refresh`(밤·낮)와 `CombatViewSystem.PresentCompletion`(확인 뒤)이 `CombatEventSounds.BgmPreset`을 부른다. 값 3/2/1 그대로.
  - `CombatSceneEventPresenter`: 조각 실행 재생기를 캐릭터 처리기에 넘긴다.
  - `CombatPieceRun`(검토 반영): 일반 완료가 실행 전체 끝과 겹치면 전체 종료를 먼저 하고 완료를 알린다(아래).
- **동작:**
  - 소리: 사건별 AudioSystem 호출(함수·칸·프리셋·배경음 번호)과 호출 시점이 같다. 사망 소리는 반응 시작 다음, 사실 반영 전으로 옛 순서와 같다.
  - 진단: 죽어 가는 유닛의 사건이 막힐 때 막는 이유가 `Reaction`(또는 `StepLane`·`Waiting`) 대신 `PieceSubject`로 먼저 나온다(조각 자리가 걸음·반응보다 앞). 예외 문구의 이유 이름만 바뀐다.
  - 실패 경로: 반응이 취소·표시 사라짐으로 끝을 알리지 않으면 사망 실행이 남는다. 반응 재생기의 `BeginFrame` 오류로 재생이 먼저 멈추고 사건 Presenter의 취소가 정리해 결과는 같다. 사망 시작에 조각 재생기의 "사라짐" 선검사가 새로 붙었다(정지 시점·문구만 다른 극단 경우).
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(전체 5,338개, 검토 반영 전):** B-2 대비 기존 시험 상태 변화 0, 새 시험 18개 PASS. 실행이 남긴 에셋 없음.
- **검토 에이전트 1개:** 높음 1·낮음 7. 소리 동일성(사건별 조건·조회·호출 오버로드·오디오 없음), 호출 자리 그대로, 사망 순서·완료·세대·시작 실패 정리, 동시 사망, 배경음 세 곳, 할당, 형식을 확인했다.
- **검토 지적 처리:**
  - 반영(높음): 일반 완료가 전체 종료보다 먼저 가서, 사망 완료 콜백 안에서는 조각 재생기가 아직 사망 실행을 가졌다(`IsComplete` false, `BlocksFinalFacts` true). 이 순서에 기대는 기존 Unity 시험 2개(4개 경우)가 실패할 것으로 추적됐다: `CombatUnitDeathTests.CompletionStartingNextReaction_ProtectsNewLeaseEvenWhenCallbackThrows`(콜백 안 `Refresh`), `CombatRailwayContactSceneTests.LastDeathCallbackStartsNextTrain_WithoutOldDeltaTimeOrOldFailure`(콜백 안 다음 기차). 이 시험들은 관리 실행에서 NATIVE라 상태 비교로는 잡히지 않았다. 마지막 항목이 끝나 실행 전체가 끝나면 전체 종료(대여 되돌림·재생기에서 빠짐)를 먼저 하고 완료를 알리게 고쳤다(옛 "정리 뒤 통지"와 같음). 콜백이 예외를 던져도 실행이 남지 않는다. 기다리지 않는 항목이 진행 중이면 완료만 먼저 간다(사망 반응이 시작 안에서 끝나면 옛 순서 "완료 → 소리"). 설계 BP2 구현 보충을 고쳤다.
    - 이 변경으로 소환 끝 순서가 강조 끄기 → 자세 복원 → 완료가 되어 옛(자세 복원 → 강조 끄기 → 완료)과 강조·복원 앞뒤만 다르다. B-2의 `CombatAbilityCompositionTests`·`CombatScalePieceTests` 기대값을 고쳤다.
    - 관리 시험 2개 추가: 완료 콜백 안에서 재생기가 비어 있음, 완료 콜백 예외 뒤 실행이 남지 않음. 사망 조립 시험에도 콜백 안 상태 단정을 더했다.
  - 반영: 반응 조각 설명("끝을 반드시 알림")을 취소·사라짐 경우까지 바로잡았다.
  - 반영: 배경음 국면 열거 값은 프리셋 번호가 아니라고 적었다.
  - 반영: 사망 조립 시험의 "사실 반영 전" 문구를 사실대로 고쳤다. 오디오가 없으면 유닛을 조회하지 않음을 시험에 더했다.
  - 기록: 막는 이유 진단 변화(위 동작).
  - 기록: 소리 표 ↔ `CombatEventAudio` 정적 참조(표 → 프리셋 함수, Audio → 표). 같은 어셈블리라 문제없고, 프리셋 번호만 Audio에 남는다(설계 결정). 맵 복귀 배경음 1이 `StageMapViewSystem`에도 있다(범위 밖).
  - 기록(선택 사항, 반영 안 함): `CombatReactionPiece`와 `CombatFacingAttackPiece`가 거의 같은 본문이다. 차이는 "시작하지 못할 수 있음(false)" 하나다. 뜻이 달라(반응은 시작하면 끝이 오고 시작 실패는 예외) 따로 둔다.
  - 기록: 소리 표의 "예상 사실이 이미 사망" 분기는 사건 생성 검사에 막혀 시험하지 않았다. 처리기 단위의 순서(소리 → 사실 반영)는 Unity 사망 시험이 지킨다.
- **검토 반영 뒤:** 전체 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,340개에서 B-2 대비 기존 시험 상태 변화 0, 새 시험 20개 PASS. 합계 PASS 2,978 · NATIVE 2,196 · FAIL 117 · UNSUPPORTED 49. 실행이 남긴 에셋 없음.
- **공유:** 현강님 소리 메모 §5(`handoffs/2026-10-18-combat-event-audio-move-hyeongang-share.md`, 미전달) — "무엇을 내나" 표와 배경음 프리셋 함수 자리.
- **Unity 확인(사용자, B-3 묶음):**
  - 새 시험·바꾼 시험: `CombatEventSoundsTests`, `CombatDeathCompositionTests`, `CombatPieceRunTests`, `CombatAbilityCompositionTests`, `CombatScalePieceTests`
  - 소리: `GameAudioTests`
  - 사망: `CombatUnitDeathTests`(특히 `CompletionStartingNextReaction…`), `CombatUnitDeathLifecycleTests`, `CombatRailwayContactSceneTests`(특히 `LastDeathCallbackStartsNextTrain…`, 기차 동시 사망)
  - 재생 묶음(§2.3), `CombatViewSystemTests`(완료 확인 뒤 배경음)
  - 재생: 전투 진입·밤 끝 배경음 전환, 사망 소리, 기차 동시 사망
- **커밋 때:** 새 코드 3개·시험 2개(+ `.meta`).

### 12.13 B-4 (2026-10-18)

- 사용자: "B-3 Unity 묶음과 재생 문제 없는 것 확인했어. B-4 진행해." B-3 끝.
- **착수 때 결정(사용자):**
  - 플래시 방식: "컬러 시스템으로 빨간색으로 변했다가 돌아오게 할건데". ColorSystem 색 손잡이로 빨간색이 됐다가 원래 색으로 돌아온다.
  - 창구 API: "플래시만 새 함수 (Recommended)". 계획의 일반 함수 `TryBeginAppearance(id, layer)`와 층 열거형 `CombatUnitAppearanceLayer` 대신 플래시 대여 `TryBeginFlash`만 더한다. 이동 투명도 API(`BeginMovementOpacity`)와 그 호출자는 그대로다. 두 층의 합성은 표시의 외형 소유자가 맡는다(설계 BP9·§6.1, §10.4 고침).
  - 시험 값: 모든 캐릭터가 공유하는 기본 반응 에셋 `CombatUnitReaction_Default`에 빨간색·0.18초(반응 시간과 같음). 구조물은 반응 설정이 없어 플래시가 없다.
- **새 파일(GUID 앞 8자리):** `D/ICombatUnitFlash`(`2ef9dbf7`), `C/CombatFlashPiece`(`58bae3ab`).
- **이름 변경:** `D/MovementModelOpacity.cs` → `D/UnitModelAppearance.cs`(`.meta`도 이름만 바꿈, GUID `69b0c280` 유지).
- **새 시험(`T/`):** `CombatFlashPieceTests`(5개, `8e4413fa`), `CombatHitCompositionTests`(8개, `56e4b8f7`), `UnitModelAppearanceTests`(5개, Unity 전용, `fcb751de`). `CombatUnitPresentationWindowTests`에 1개, `CombatUnitReactionAssetTests`에 플래시 값 단정과 잘못된 플래시 시간 3경우를 더했다.
- **바꾼 코드:**
  - `UnitModelAppearance`(외형 소유자): 층 둘(투명도·플래시)을 기억해 한 손잡이에 합쳐 쓴다. 색 = 원래 색과 플래시 색을 세기로 섞음, 알파 = 원래 알파 × 투명도. 투명 재질은 투명도 < 1일 때만, 바뀔 때만 바꾼다. 마지막 층이 끝날 때만 원래 재질·PropertyBlock으로 되돌린다(DR-004). 투명도만 쓰면 옛 값·호출 순서와 같다.
  - `FieldUnitView`: 외형 소유자 필드, 플래시 대여(`SupportsHitFlash`·`BeginHitFlash`, 크기 대여와 같은 ID·버전·취소 규칙), 다시 대여·풀 반환·비활성에서 플래시 되돌림(자세·크기 복원 뒤, 검토 반영). `ClearMovementOpacity`는 투명도 층만 끝낸다.
  - `ICombatUnitPresentationTarget`: `SupportsHitFlash`·`BeginHitFlash`. 구현은 `FieldUnitView`와 시험 가짜 둘이다.
  - `CombatUnitPresentationWindow`: `CanFlash`·`TryBeginFlash`. `CombatUnitReactionSet.BeginHit`이 반응 Presenter를 돌려준다(정리용).
  - `CombatComposition`(`Flash` 채널)·`CombatPieceRun`(대여·되돌림)·`CombatPieceContext`(`Flash(unit)`).
  - `CombatCharacterEventHandler`: 피격은 피격 조립(`ComposeHit`: 유닛 실행·일반 완료, 0 피격 반응(외부 진행, 기다림) → 1 플래시(값이 있고 외형 층이 될 때만, 같은 순간, 기다림))을 조각 실행 재생기로 진행한다. 시작 실패 때 시작된 반응을 취소하고 자세를 되돌린다.
  - DB View: `CombatUnitReactionPresentationSO`에 `hitFlashColor`(기본 빨강)·`hitFlashDuration`(기본 0 = 없음, 검사), Data에 `HitFlashColor`·`HitFlashSeconds`·`HasHitFlash`. 에셋 `CombatUnitReaction_Default`에 빨강·0.18.
  - `PlacementGhost`: 이름 바뀐 클래스의 `DestroyOwned`를 부른다.
- **동작:**
  - 화면: 맞은 캐릭터 모델이 빨간색으로 바뀌었다가 0.18초 동안 원래 색으로 돌아온다. TMP·Canvas 밑을 뺀 자식 Renderer(무기 부품 등)도 함께 바뀐다.
  - 완료: 반응과 플래시가 둘 다 끝날 때다. 지금 값(둘 다 0.18초)이면 같은 프레임에 끝나 완료 프레임은 옛 피격과 같다. 둘 다 스케줄러 루프에서 시작해 다음 프레임부터 시간을 받고, 프레임 안에서는 조각 자리가 반응 자리보다 먼저 돌며, 누적 계산이 같은 float 값을 낸다(검토 확인). 플래시가 반응보다 길면 완료가 늦어진다(의도).
  - 플래시 값이 없거나 외형 층이 없으면(색 서비스 없음) 반응 항목만 남아 옛 피격과 같다.
  - 진단: 피격 중인 유닛의 사건이 막힐 때 막는 이유가 `Reaction` 대신 `PieceSubject`로 먼저 나온다(B-3 사망과 같은 변화, 예외 문구의 이유 이름만 다름).
  - 실패 경로: 피격이 이제 색 손잡이 생성에 기댄다. 손잡이를 못 만들면(같은 Renderer 중복 등록 등) 피격 실행 시작이 실패하고 재생이 정지한다. 전에는 이동 미리보기에서만 생기던 위험이다. 지금 유닛 Renderer에 손잡이를 거는 다른 시스템은 없다(`CreateColorHandle` 호출처 확인). 손잡이는 맞은 유닛마다 풀 표시 수명 동안 남는다(옛 이동 반투명과 같음).
- **빌드:** MSBuild 오류 0, 고유 경고 16개 같음.
- **관리 실행(검토 반영 전, 전체 5,358개):** B-3 대비 기존 시험 상태 변화 0, 새 시험 18개(14개 PASS, `UnitModelAppearanceTests` 4개는 Unity 전용 NATIVE). 실행이 남긴 에셋 없음.
- **검토 에이전트 1개:** 높음 0·중간 2·낮음 6·정보 1. 투명도만 쓸 때의 동일성, 두 층 합성(양쪽 순서), 플래시가 남지 않는 경로(정상 끝·취소·시작 실패·풀 반환·다시 대여·비활성·씬 해제), 대여 취소 경로, 플래시 없는 피격의 동일성, 완료 프레임, 구현·사용처, FW, DB, 형식을 확인했다.
- **검토 지적 처리:**
  - 반영(중간): 설계 BP9·§6.1·§7, 계획 §10.4를 착수 때 결정으로 고쳤다. 이 기록, 민형님 Factory 공유 문서 §12, CLAUDE.md를 썼다.
  - 반영(중간): `CombatUnitReactionAssetTests`에 실제 에셋의 플래시 값(빨강·0.18·`HasHitFlash`)과 잘못된 `hitFlashDuration`(-1·NaN·무한) 3경우를 더했다.
  - 반영(낮음): `FieldUnitView` 플래시 대여 시험(Unity 전용)을 `UnitModelAppearanceTests`에 더했다. 두 번째 대여 예외, 풀 반환 취소·복원, 늦은 되돌림 안전, 옛 대여가 새 대여를 못 되돌림, 다른 유닛으로 다시 대여 때 취소, 외형 해제 뒤 사용 불가를 본다.
  - 반영(낮음): 피격 조립 시험이 반응 시작 → 플래시 색 순서를 실제로 단정한다(전에는 대여 로그만 봤다).
  - 반영(낮음): `OnDisable`·`ResetForPool`에서 플래시 되돌림을 자세·크기 복원 뒤로 옮겼다. 새 예외 지점이 자세 복원을 막지 않는다.
  - 반영(낮음): 적용 실패가 두 층을 함께 지운다는 설명(다음 Tick에 플래시 조각이 다시 씀), 창구 채널 목록, 처리기 생성자 설명, 플래시 색 알파를 쓰지 않는다는 Tooltip.
  - 반영(검토 중 발견): `FieldUnitView`에서 크기 대여 설명 주석이 플래시 대여 클래스 위에 붙어 있던 것을 제자리로 옮겼다.
  - 기록(반영 안 함): 플래시 시간 검사가 SO `Require`와 조각 `ValidateSeconds` 두 곳에 있다. 각각 그 SO의 다른 필드, 다른 조각과 같은 방식이고 조건도 같다.
  - 기록(반영 안 함): `UnitModelAppearance.Restore()`(두 층 모두 끝냄)는 운영 코드에서 부르지 않고 시험만 쓴다. 외형 소유자의 정리 함수로 둔다.
  - 기록(반영 안 함): 플래시 중 손잡이가 슬롯마다 매 프레임 `Shader.PropertyToID`·`SetPropertyBlock`을 부른다. GC 할당은 없고, 보스(Renderer 15개)도 0.18초 동안이라 문제없다.
  - 기록(반영 안 함): `specs/2026-10-18-view-framework-usage-review.md` G4가 옛 경로를 가리킨다. 다른 브랜치 스냅샷 기준 리뷰라 고치지 않았다.
- **검토 반영 뒤:** 전체 재빌드 오류 0·고유 경고 16개 같음. 관리 실행 전체 5,362개에서 B-3 대비 기존 시험 상태 변화 0, 새 시험 22개(14개 PASS, Unity 전용 8개 NATIVE: `UnitModelAppearanceTests` 5개, 잘못된 플래시 시간 3경우). 합계 PASS 2,992 · NATIVE 2,204 · FAIL 117 · UNSUPPORTED 49. 실행이 남긴 에셋 없음.
- **공유:** 민형님 Factory 공유 문서 §12(색 손잡이를 두 층이 함께 씀, 손잡이 수명, 반응 SO 필드·에셋 값, 미전달).
- **Unity 확인(사용자, B-4 묶음):** 사용자 "B-4 Unity 묶음과 플래시 재생 문제 없는 것 확인했어." B-4 끝, B 단계(B-1~B-4) 끝. 확인 목록:
  - 새 시험·바꾼 시험: `UnitModelAppearanceTests`(Unity 전용 5개), `CombatFlashPieceTests`, `CombatHitCompositionTests`, `CombatPieceRunTests`, `CombatUnitPresentationWindowTests`
  - DB: `CombatUnitReactionAssetTests`, `DataBaseAssetTests`, `ViewDataTableTests`
  - 피격·사망: `CombatUnitReactionTests`, `CombatUnitDeathTests`, `CombatUnitDeathLifecycleTests`
  - 이동 반투명(같은 외형 소유자): `CombatMovementPlanningDisplayTests`, `CombatMovementDraftDisplayTests`, `CombatPlacementFeedbackTests`
  - 실제 DB 값으로 피격이 나는 시험: `CombatSceneEventPresenterTests`, `CombatStandardAttackAssetTests`, `CombatBallistaAssetTests`, `WallAttackInteractionTests`, `TerrainOperationalDisplayTests`, `CombatRailwayContactSceneTests`
  - 재생 묶음(§2.3)
  - 재생: 피격 때 빨간 플래시(무기 부품까지 같이 바뀌는지 포함), 이동 미리보기 반투명 중 피격, 보스 피격·포효, 사망
- **커밋 때:** 새 코드 2개·시험 3개(+ `.meta`), 이름 바꾼 `UnitModelAppearance.cs`(+ `.meta`, 옛 `MovementModelOpacity.cs`·`.meta` 삭제로 보임), 에셋 `CombatUnitReaction_Default.asset`은 SO 코드와 한 커밋.
