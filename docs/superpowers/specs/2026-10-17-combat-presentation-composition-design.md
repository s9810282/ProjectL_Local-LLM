# 전투 사건별 연출 조립 설계

> **2026-10-18 개정(코드 구조 기준):** 사용자 지시 "평가의 정리 방향대로 10-17 연출 조립 설계를 코드 구조 기준으로 개정해줘. 1번 시작 조건 통합부터 2·3번 재생기 계약·유닛 자원 창구까지, 겹침 규칙 보존 방법을 포함해서."
> - 본문은 조각·조립을 받칠 **바탕 셋**이다: 시작 조건 하나(B11), 재생기 계약과 실행 목록(B12), 유닛 표시 자원 창구(B13). 겹침 규칙 보존은 B14다. 셋 모두 **동작 동일**이 완료 조건이다.
> - 조각·시점·실행기(옛 B1·B2·B4)와 소리 창구·Gate 경계·구조물(옛 B7·B8·B10)은 바탕 위의 다음 단계로 B15에 모았다. 조립은 코드(처리기)에서 한다.
> - 평가: [전투 연출 재생 구조 평가](https://claude.ai/artifact/BakhQ6UaWPhRTAaChkLvDX)(2026-10-18, P1~P8). 이 개정은 평가 "정리 방향" 1~3을 설계로 옮긴 것이다.
> - **코드 변경 없음.** 결정과 계약만 적는다. Task·실행 기록은 결정 뒤 계획 문서에 둔다.

## 개정 이력

| 날짜 | 내용 |
|---|---|
| 2026-10-17 | 초판. 조각(B1)·시점(B2)·SO 조립(B3)·실행기(B4)·처리기(B5)·유닛당 자원(B6)·소리 창구(B7)·Gate 경계(B8)·값 원본(B9)·구조물(B10) |
| 2026-10-17 | 사용자 결정: SO 조립 폐기(B3·B9·Q-R1 A·Phase R-5 폐기), Presenter 코드 구조 개선으로 |
| 2026-10-18 | Presenter 분리 S-1~S-5로 처리기(B5)·보인 사실·대기·강조·소리 위치가 정리됨([분리 설계](2026-10-18-combat-scene-event-presenter-split-design.md), [분리 계획](../plans/2026-10-18-combat-scene-event-presenter-split-plan.md) §8.3) |
| 2026-10-18 | **이번 개정.** 바탕 셋(B11~B13)과 겹침 규칙 보존(B14)을 본문으로, 옛 B1·B2·B4·B6~B8·B10을 B15로 정리 |
| 2026-10-18 | 결정 Q-R7~Q-R12 모두 A. 원칙 재평가에서 나온 보완 4건 반영: 대기 칸 계약(B12), 실행 목록 자리 고정(B12), 프레임 시작 출처 겹침 한 번 기록(B12), 유닛 표시 계약을 A-3a에 포함(B13). §5를 결정 반영 뒤 평가로 다시 씀. Task 계획 작성 |
| 2026-10-18 | 사용자 지적("평가 결과가 저러면 원인 분석과 개선을 해야 하지 않을까")으로 §9 원인 분석과 개선(근본 원인 C1~C5, 개선 I-1~I-6) 추가 |
| 2026-10-18 | 결정 Q-R13~Q-R18 모두 A. 개선 Phase(A-2b·A-3b·A-4·A-4b·A-5·B-0~B-4)를 Task 계획에 더함 |

## 요구 (2026-10-17, 그대로 유효)

1. "피격 시 외부에서 히트 플래쉬, 사운드 출력, 이펙트 출력 등에 대한 연결이 열려있는 지"를 본다.
2. "애니메이션 클립이랑 지금 피격 모션이랑 둘 다 쓸 거"다.
3. 플래시·클립 길이는 사건 완료에 **영향을 준다.** 구조물·장애물은 유닛과 같을 수도 있고 **일부만** 쓸 수도 있다. 사운드는 **창구만** 열어 둔다.
4. "피격뿐이 아니라 **행동에 따른 연출점을 다 열어놔야**" 한다.
5. **"결국 이벤트 별로 내가 원하는 연출들을 조립식으로 만들어서 실행한다"**가 목표다.

관련 문서: [히트 타이밍 설계](2026-10-16-combat-hit-timing-design.md)(H1·H3), [이동 접촉 설계](2026-10-17-combat-movement-contact-design.md)(M3~M5), [동시 처리 설계](2026-10-17-combat-concurrent-presentation-design.md)(C1~C7), [보스 활성화 설계](2026-10-15-stage1-boss-view-activation-design.md)(D6b).

## 1. 현재 상태 (코드 대조, 2026-10-18 S-5 뒤)

경로 약어: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`.

### 1.1 연출 플로우

```text
CombatViewSystem.Tick
  └ OrderedCombatPresentation.Tick ─ 사건 순서·같은 순간 묶음·시작 판단(CanStart + 표시의 CanBeginPresentationEvent)
      └ CombatSceneEventPlayback ─ 기차 발동만 가로챔
          └ CombatSceneEventPresenter ─ 시작 조건 · PresentEvent 틀 · TickPresentation
              ├ 처리기 표(6) · 이동 표시 · 보인 사실 · 대기 칸 · 강조 · 소리
              └ 재생기 4개: CombatAttackPlayback · CombatAbilityMotionPlayback · CombatUnitReactionSet · CombatUnitMotionSet
                  └ FieldUnitPresenter / FieldUnitView (자세·크기 대여, 걸음 배치·회전 직접 호출)
```

사건 축(이 사건에 무엇을 하나)은 처리기 표와 틀 하나로 정리됐다. 남은 문제는 아래 세 가지다(평가 P1·P2·P4).

### 1.2 시작 조건: 같은 규칙이 두 판으로 적혀 있다 (평가 P4)

Presenter에는 시작 판단이 둘 있다.

- **질의판** `CanBeginPresentationEvent(item)`: 스케줄러가 "지금 시작해도 되나"를 묻는다.
- **단정판** `RequireIdle(allowOverlap, allowDeaths, item)`: 시작 직전에 확인하고, 막히면 예외를 던진다.

부르는 곳은 네 가지 모양이다. 이 문서는 이를 **프로필**이라 부른다.

| 프로필 | 지금 호출 | 사건 | 겹침 허용 | 사망 겹침 | 실패 |
|---|---|---|---|---|---|
| 질의 | `CanBeginPresentationEvent(item)` | 있음 | 예 | — | false |
| 시작 단정 | `BeginEvent`·`RequireIdleFor`(걸음·복귀·경로 끝)의 `RequireIdle(true, item)` | 있음 | 예 | 아니요 | 예외 |
| 결과 시작 | `BeginPresentation`의 `RequireIdle()` | 없음 | 아니요 | 아니요 | 예외 |
| 동시 사망 | `BeginConcurrentDeath`의 `RequireIdle(allowDeaths: true)` | 없음 | 아니요 | 예 | 예외 |

사건·걸음 시작은 질의가 실패하면 예외를 던지고(`BeginEvent` 문구 "접촉 또는 남은 공격 표시를…", 그 밖 "이전 사건 연출이…"), 이어서 시작 단정을 본다.

재생기별로 펼치면 아래와 같다. **굵은 칸이 질의판과 단정판이 다른 곳**이다.

| 재생기 | 질의 | 시작 단정 | 결과 시작 | 동시 사망 |
|---|---|---|---|---|
| 공격 | 표시 사라짐 · 공격 Gate(`gate.CanBegin(item)`) | **`BlocksNextEvent(true)`: 히트 전 공격 중 · 재장전 중 · 사라짐** | `BlocksNextEvent(false)`: 공격 중 · 재장전 중 · 사라짐 | 결과 시작과 같음 |
| 능력 동작 | 사라짐 · 진행 중이고 출처 겹침 불가 | 같음 | 사라짐 · 진행 중 | 같음 |
| 반응 | 반응 불가 · 주체 반응 중(`ReactionBlocks`) | 같음 | 반응 불가 · 반응 중 | 반응 불가 · 사망끼리 겹칠 수 없음(`!CanOverlapDeaths`) |
| 걸음·대기 | 레인 자원(`MotionBlocks(item, true)`) · **걸음 Gate(`motion.CanBegin`)** | 레인 자원 · **대기 사건 있음** | 레인 진행 중 · 대기 사건 있음 | 결과 시작과 같음 |

- 두 판의 차이(대기 사건, 재장전, 걸음 Gate)는 지금 **스케줄러 순서 덕분에 도달하지 않는다.** 대기 사건·재장전이 있는 동안 스케줄러는 다음 사건을 묻지 않고(턴 끝·행동 사건은 묶음 장벽), 사건·걸음 시작은 질의를 먼저 통과해야 한다.
- 문제는 한쪽만 고치기 쉽다는 점이다. 어긋나면 "질의는 된다고 했는데 시작에서 예외"가 난다.

### 1.3 프레임 순서 (`TickPresentation`)

```text
0. 반응 불가면 예외 → 반응 Version, 공격 진행 여부, 출처 겹침 허용을 프레임 시작 값으로 기록
1. 공격: _attacks.Tick(dt, mapper 연결) 이 false면 프레임 끝
      (재장전 완료 통지 · 세대 변경 · 히트 전(겹침 불가) 공격 진행 중)
2. 능력 동작(진행 중·사라짐일 때): 프레임 시작에 공격이 진행 중이 아니었으면 Tick
      → 프레임 시작에 출처 겹침 불가였으면 프레임 끝 (포효 히트 전엔 능력만 진행)
3. 걸음·대기(레인 진행 중이거나 대기 사건 있음): TickMotion → 세대가 바뀌었으면 프레임 끝
      TickMotion = 대기 칸 시간 소비(남으면 레인 정지) → 꺼낸 대기 사건 표시(이 프레임 레인 시간 0) → 모든 레인 진행
4. 반응: 프레임 시작 Version까지 시작한 반응만 진행 (이 프레임에 시작한 반응은 시간 0)
```

### 1.4 재생기 수명이 Presenter에 이름으로 박혀 있다 (평가 P1)

| Presenter 멤버 | 공격 | 반응 | 능력 | 걸음 | 대기 |
|---|---|---|---|---|---|
| `BeginPresentation` | ● | | | ● | 간격 값 |
| `IsPresentationComplete` | ● | ● | ● | ● | |
| `CanBeginPresentationEvent` | ● | ● | ● | ● | |
| `RequireIdle` | ● | ● | ● | ● | ● |
| `TickPresentation` | ● | ● | ● | ● | ● |
| `CancelMovement` | ● | ● | ● | ● | ● |
| `Release` | ● | ● | ● | Clear 경유 | |
| `Refresh`(최종 사실 전 단정) | ● | ● | ● | | |

새 재생기(예: 플래시를 포함한 조각 실행)를 더하면 이 8곳을 고친다. 반응 불가 상태(`_reactionUnavailable`)와 그 취소 콜백도 Presenter에 있다.

### 1.5 유닛 표시 자원 (평가 P2·P3)

| 채널 | 지금 원본 | 쓰는 곳 | 규칙 |
|---|---|---|---|
| 자세(루트 위치·회전) | `FieldUnitView` 자세 대여(버전·사라짐 통지) | 공격(`BeginAttackPose`), 반응(`view.BeginReactionPose`), 등장(`TryBeginMotionPose`) | 유닛당 하나. 대여 중 `Apply`가 위치·회전을 쓰지 않음 |
| 크기 | `FieldUnitView` 크기 대여 | 포효 | 유닛당 하나, 자세와 독립 |
| 걸음 배치 | `FieldUnitPresenter._steppingActors` + `Interpolate`가 Transform 직접 씀 | 걸음 레인 | **대여 없음.** `RenderFacts`가 집합을 보고 배치를 건너뜀 |
| 표시 회전 | `FieldUnitPresenter._presentationRotations` + `ApplyRotation` 직접 | 걸음 레인(걸음 전 회전·복귀), 취소, 복귀 표시 | **대여 없음.** `RenderFacts`가 표를 보고 회전을 덮음 |
| 사망 모델 유지 | `FieldUnitPresenter._deathHolds` | 캐릭터 처리기 | 유닛당 하나 |
| 외형(PropertyBlock) | `FieldUnitView._movementOpacity`(`MovementModelOpacity`) | 입력의 이동 계획 미리보기만 | **재생 쪽 소유자 없음.** 플래시를 넣을 자리가 없다 |
| Animator | 없음 | — | 클립(요구 2) 자리 없음 |
| HUD 피해 숫자·미리보기 | `FieldUnitPresenter` 직접 | 캐릭터 처리기, 입력 피드백 | 배타 아님 |

- 반응 경로는 `TryGetReactionPresentation`이 **`FieldUnitView` 자체를 돌려줘** 처리기가 View에 직접 대여를 건다.
- `FieldUnitPresenter`(712줄·37개 메서드)가 사실 그리기, HUD 5종, 대여 3종, 걸음 배치, 회전, 연출 값 조회, Ballista 무기, 몸 조회를 모두 가진 허브다. 재생기·처리기가 모두 이 구체 MonoBehaviour에 의존해 Unity 없이 시험할 수 없다.

## 2. 목표 그림

```text
Presenter (공통 수명 · PresentEvent 틀 · 공개 진입점)
  ├─ 시작 조건: 질문 하나(CombatStartQuery) → 재생기 목록이 "막는 이유" 하나를 답함      ← B11
  ├─ 프레임:   재생기 목록.Tick(frame)  (등록 순서, 재생기가 "이 프레임 멈춤"을 답함)     ← B12
  ├─ 완료·최종 사실·취소·해제: 재생기 목록에 한 번씩                                    ← B12
  └─ 재생기 목록 [공격, 능력 동작, 걸음·대기, 반응, (다음 단계) 조각 실행]
          └─ 유닛 표시 자원 창구 (채널: 자세·크기·걸음 배치·표시 회전·사망 유지·외형 층·Animator)  ← B13
                 └─ FieldUnitView (채널 구현) · 사실 그리기는 창구에 "표시가 소유한 것"을 물음
```

- Presenter는 재생기를 **조립(생성자) 한 곳에서만** 이름으로 안다. 그 밖의 멤버는 목록만 돈다.
- 겹침 규칙은 지금 들어 있는 자리(Gate, 레인, 반응 집합, Tick 순서)를 그대로 각 재생기 안으로 옮긴다(B14).

## 3. 범위

- **한다(이번 개정의 구현 범위):**
  - B11 시작 조건 하나: 프로필·막는 이유·재생기별 판단, 질의판·단정판을 한 판단에서 파생
  - B12 재생기 계약과 실행 목록: 지금 재생기 넷을 계약 뒤로, Presenter 8개 멤버를 목록 호출로
  - B13 유닛 표시 자원 창구: 채널 대여 한 곳, 사실 그리기와의 소유 경계, View 누출 제거, 외형 층 소유자(이동 투명도만 옮김)
  - B14 겹침 규칙 보존: 규칙별 옮길 자리와 지키는 시험
- **하지 않는다:**
  - 조각·조립 실행(B15)의 구현. 바탕이 끝난 뒤 별도 설계·계획으로 한다.
  - 플래시·클립·소리 창구·포효 재조립 구현(B15의 소비자).
  - 스케줄러(`OrderedCombatPresentation`)·Gate(`CombatHitGate`·`CombatStepContactGate`)·기차 전용 재생의 규칙 변경. 읽는 자리만 바뀐다.
  - 재생 계약(`ICombatEventPlayback`) 전달 사슬(재평가 N7, 수용)과 `CombatViewSystem`.
  - Core, Framework 공용 파일, Render(Color) 시스템 변경.

## 4. 결정

### B11. 시작 조건 — 질문 하나, 막는 이유 하나 (평가 P4)

- **질문(가칭 `CombatStartQuery`, readonly struct):** 프로필(질의·시작 단정·결과 시작·동시 사망) + 사건(없을 수 있음). 겹침 허용·사망 겹침은 프로필에서 파생한다(§1.2 표). 호출자가 불리언 인자를 조합하지 않는다.
- **답(가칭 `CombatStartBlock`, enum):** `None`, `Unavailable`(표시 사라짐), `AttackGate`, `AttackBusy`(히트 전 공격·재장전), `AbilityBusy`, `Reaction`, `StepLane`, `StepGate`, `Waiting`(대기 사건).
- **판단 하나:** `FindBlock(query)`가 재생기를 정한 순서(공격 → 능력 → 걸음·대기 → 반응)로 보고 첫 이유를 돌려준다. 모든 조건은 부수효과 없는 읽기라 순서는 "어느 이유를 보고하나"만 바꾼다.
- **파생:**
  - `CanBeginPresentationEvent(item)` = `FindBlock(질의(item)) == None`
  - 시작 단정 = 질의가 막히면 지금 문구의 예외 → `FindBlock(시작 단정(item))`이 막히면 "이전 사건 연출이…" 예외
  - 결과 시작·동시 사망도 같은 판단의 프로필이다.
  - 예외 문구는 지금 그대로 둔다. 막는 이유는 `Data`나 문구 끝에 덧붙일 수 있다(Q-R8).
- **질의판·단정판 차이(§1.2 굵은 칸):** 첫 단계에서는 **차이를 그대로 프로필 규칙으로 옮긴다.** 하나로 맞출지는 Q-R8이다. 맞추면 지금 "단정 예외"가 날 자리가 "기다림"이 되고, 도달하지 않는 경로라 화면은 같다.
- **위치:** A-1에서는 Presenter 안에 재생기별 판단 도우미 넷(지금 조건식을 그대로 나눈 것)으로 둔다. A-2에서 각 재생기의 `FindBlock`으로 옮긴다. 조건식을 두 번 고쳐 쓰지 않도록 A-1의 도우미 경계를 재생기 경계와 같게 자른다.

### B12. 재생기 계약과 실행 목록 (평가 P1)

**계약(가칭 `ICombatPresentationPlayer`, 내부 인터페이스):**

| 멤버 | 뜻 | 지금 원본(옮겨 올 곳) |
|---|---|---|
| `BeginResult(result)` | 결과 하나의 준비 | 공격 `BeginResult`, 걸음 `BeginResult` |
| `FindBlock(in query)` | 이 재생기가 시작을 막는 이유(B11) | §1.2 표의 재생기 행 |
| `BeginFrame(in frame)` | 프레임 시작 사실을 기록. 이 재생기가 이미 실패 상태면 여기서 예외 | 반응 불가 예외·반응 Version, 출처 겹침 허용 기록 |
| `Tick(in frame)` → bool | 한 프레임 진행. false면 목록이 이 프레임의 남은 재생기를 진행하지 않음 | §1.3의 단계별 이른 반환 |
| `IsComplete` | 결과 완료 판단에 이 재생기가 끝났는지 | `IsPresentationComplete`의 항 |
| `BlocksFinalFacts` | 최종 사실을 그리면 안 되는 상태인지 | `Refresh` 단정의 항 |
| `Cancel()` / `Release()` | 진행을 버리고 자원을 되돌림 / 최종 해제 | `CancelMovement`·`Release`의 항 |

- 인터페이스에 기본 구현을 두지 않는다(Unity 런타임의 기본 인터페이스 멤버 지원에 기대지 않음). 재생기마다 모든 멤버를 명시한다.
- **프레임 맥락(가칭 `CombatPlaybackFrame`, readonly struct):** 경과 시간, Mapper 연결 여부. 재생기끼리의 사실(공격 진행 여부 등)은 넣지 않는다. 필요한 재생기가 자기 출처에서 `BeginFrame`에 기록한다.

**실행 목록(가칭 `CombatPresentationPlayers`, 순수 C#):**

- **순서는 서명으로 고정한다(보완 2).** `Create(attack, ability, step, reaction)`처럼 자리마다 이름 있는 인자로 받는다. 순서가 곧 겹침 규칙(O1·O3·O5·O7)이라 배열·가변 인자로 받지 않는다. 다음 재생기(B15 조각 실행 집합)는 자리를 정해 인자를 하나 더한다. 만든 뒤에는 바뀌지 않는다(지침 §11).

- `BeginResult` → 순서대로
- `FindBlock` → 첫 이유
- `Tick(frame)` → 모두 `BeginFrame` → 순서대로 `Tick`, false에서 멈춤
- `IsComplete` → 모두, `BlocksFinalFacts` → 하나라도
- `CancelAll`·`ReleaseAll` → 순서대로, 실패를 모아 끝에 던짐(`CleanupErrors`)
- Unity 의존이 없어 가짜 재생기로 관리 실행 시험을 둔다.

**재생기 넷(A-2):** 지금 부품을 고치지 않고 감싸는 얇은 재생기다(Q-R9). 부품의 기존 API와 시험은 그대로 남고, Presenter에 있던 규칙만 재생기로 옮긴다.

| 재생기(가칭) | 감싸는 부품 | `FindBlock` | `BeginFrame` | `Tick` 반환 | `IsComplete` | `BlocksFinalFacts` |
|---|---|---|---|---|---|---|
| `CombatAttackPlayer` | `CombatAttackPlayback` | 질의: 사라짐·Gate / 단정: `BlocksNextEvent(겹침)` | 출처 겹침의 프레임 시작 값 기록(보완 3) | `attacks.Tick(dt, mapper 연결)` | `attacks.IsComplete` | 공격 중·사라짐 |
| `CombatAbilityPlayer` | `CombatAbilityMotionPlayback` | 사라짐 · 진행 중이고 (겹침 허용 프로필 ∧ 지금 출처 겹침)이 아님 | 공격 대기 상태(`IsWaitingForAttack`, 부품에 더할 읽기 속성) 기록 | 진행·사라짐이 아니면 true. 아니면 프레임 시작에 공격 대기가 아니었을 때만 Tick, 공격 재생기가 기록한 프레임 시작 출처 겹침 반환 | 진행·사라짐 아님 | 진행·사라짐 |
| `CombatStepPlayer` | `CombatUnitMotionSet` + `CombatEventWait` | 질의: 레인 자원·걸음 Gate / 단정: 레인 자원·대기 사건 | — | 레인·대기가 없으면 true. 아니면 `TickMotion` 뒤 세대가 같으면 true | 레인 진행 아님(대기 칸 제외) | false |
| `CombatReactionPlayer` | `CombatUnitReactionSet` + 반응 불가 상태 | 반응 불가 · 프로필별 반응 판단 | 반응 불가면 예외 · Version 기록 | 기록한 Version으로 Tick, true | 반응 없음·불가 아님 | 반응 중·불가 |

- **출처 겹침 허용**은 `CombatHitGate.AllowsOverlap`이 원본이다(공격 부품이 Gate를 소유).
  - **프레임 시작 값은 공격 재생기가 `BeginFrame`에 한 번 기록한다(보완 3).** 지금은 `TickPresentation`이 읽는 값과 공격 부품 `Tick` 안에서 읽는 값이 따로다. 능력 재생기는 공격 재생기의 기록을 읽는다.
  - 시작 조건(`FindBlock`)은 기록이 아니라 지금 값을 읽는다. 지금 질의·단정이 그 순간 값을 보기 때문이다.
  - 능력 재생기는 두 값을 좁은 계약(가칭 `ICombatSourceOverlap`: 지금 값, 프레임 시작 값)으로만 받는다. 공격 재생기가 구현하고, 조립(Presenter 생성자)이 잇는다.
- **반응 불가 상태**(`_reactionUnavailable`)와 취소 콜백(`CancelUnavailableReaction`)은 반응 재생기로 옮긴다. 캐릭터 처리기는 재생기의 콜백을 받는다. 반응 항목은 부품(`CombatUnitReactionSet`)에, 불가 상태는 재생기에 있어 원본은 각각 하나다.
- **걸음·대기 재생기**는 대기 칸과 레인을 함께 가진다. 간격 동안 레인만 멈추고 반응은 진행해야 해서(O6·O7), "프레임 멈춤" 반환 하나로는 둘을 나눌 수 없기 때문이다. 대기 칸이 꺼낸 사건은 Presenter 틀로 넘기는 콜백(`PresentEvent`)으로 보낸다. 간격 값(`CombatEnemyPresentationPacing`)은 Presenter에 남는다.
- **대기 칸 계약(보완 1, 가칭 `ICombatWaitSlot`):** `StartGap(seconds)`(레인 진행 중이면 예외, 지금 `StartGap`의 전제), `HoldAfterGap(e, completed, shownBeforeGap)`, `HoldShown(e, completed, seconds)`.
  - 걸음·대기 재생기가 구현한다. Presenter(`BeginEvent`의 간격, `PresentEvent`의 머묾)와 이동 표시(걸음 전 간격)는 이 역할 계약만 받는다.
  - 그래서 Presenter가 재생기를 이름으로 아는 곳은 조립 하나로 남는다. 보완 전에는 간격·머묾 세 곳에서 걸음·대기 재생기를 직접 불러야 했다.
- **Presenter에 남는 것:** 재생 세대, Mapper, 보인 사실, 강조, 처리기 표, 이동 표시, 간격 값, 공개 진입점과 그 순서. 시험이 리플렉션으로 읽는 필드(`_attacks`·`_reaction`·`_motion`·`_facts`·`_sound`)는 부품 참조로 남긴다(분리 계획 §1과 같은 규칙).

A-2 뒤의 Presenter 멤버는 이렇게 된다.

```text
BeginPresentation      = RequireStart(결과 시작) → _players.BeginResult(result) → _pacing.BeginResult(result)
IsPresentationComplete = _players.IsComplete
CanBeginPresentationEvent(item) = _players.FindBlock(질의(item)) == None
TickPresentation(dt)   = _players.Tick(frame(dt, mapper 연결))
Refresh                = _players.BlocksFinalFacts면 예외 → 강조 지움 → 사실 교체·그리기
CancelMovement         = 세대++ → 한 묶음 정리(Q-R7 A): _players.CancelAll()(공격 → 능력 → 걸음·대기 → 반응), 간격 값 비움, 회전·강조 정리
Release                = Clear → _players.ReleaseAll()
```

### B13. 유닛 표시 자원 창구 (평가 P2·P3)

**창구 하나(가칭 `CombatUnitPresentationWindow`):** 재생 쪽이 유닛 표시를 건드리는 유일한 입구다. `FieldUnitPresenter`가 소유하고(View 대여 집합을 알아야 함), 재생기·처리기·이동 표시는 `FieldUnitPresenter` 대신 이 창구를 받는다.

| 채널 | 개정 뒤 | 배타 | 지금과 다른 점 |
|---|---|---|---|
| 자세 | `TryBeginPose(id, cancelled, out pose)` → View 자세 대여 | 유닛당 하나(지금과 같음) | 반응 경로도 창구로. `FieldUnitView`를 밖에 돌려주지 않음 |
| 크기 | `TryBeginScale(...)` | 유닛당 하나, 자세와 독립 | 없음 |
| 걸음 배치 | `TryBeginPlacement(id, out placement)` → 대여가 보간 쓰기(`Interpolate`)를 가짐, 반납이 `EndStepPlacement` | 유닛당 하나, **자세와 배타**(Q-R11) | 집합 등록·직접 쓰기 → 대여 |
| 표시 회전 | `SetFacing`·`ClearFacing`(창구 메서드, 유닛별 표) | 유닛당 값 하나 | 소유가 창구로 이동. 동작 같음 |
| 사망 모델 유지 | `TryHoldDeath(id, out hold)` | 유닛당 하나 | View 대신 ID로 |
| 외형 층 | `TryBeginAppearance(id, layer, out appearance)`. 층: 이동 투명도(입력 미리보기), 플래시(B15에서 구현) | 층마다 하나. 층은 합성 | `MovementModelOpacity`를 외형 소유자의 첫 층으로 옮김. 화면 같음 |
| Animator | 자리만(B15·H-5) | 유닛당 하나, 자세와 독립(루트 이동 끔) | 새로 |

- **사실 그리기와의 경계:** `RenderFacts`는 지금 세 가지 비공개 표(`_steppingActors`·`_deathHolds`·`_presentationRotations`)를 직접 읽는다. 개정 뒤에는 창구에 "이 유닛에서 표시가 소유한 것"(배치 유지, 회전 덮기)을 묻는다. 그 사실의 원본은 창구 하나다. 자세 대여 중 위치·회전을 쓰지 않는 규칙은 지금처럼 `FieldUnitView.Apply`에 남는다.
- **대여의 공통 모양:** 지금 자세·크기 대여의 규칙을 모든 채널에 쓴다. 버전으로 이전 대여가 다음 대여를 못 건드림, 표시가 꺼지거나 반납되면 `cancelled`로 "표시 사라짐"을 알림, `Restore`는 여러 번 불러도 한 번만.
- **연출 값 조회**(`TryGetAttackPresentation`·`TryGetMortarPresentation`·반응 설정·Ballista 무기)는 창구에서 "대여 없이 읽기"로 함께 제공한다. 반응 경로는 설정을 읽고 자세를 빌리는 두 호출이 된다.
- **`FieldUnitPresenter` 나머지 분리(A-3b, Q-R10):** HUD 피드백(피해 숫자·비용·체력 미리보기)과 사실 그리기를 나눌지는 입력 쪽 호출자(`CombatAttackFeedback`·`CombatMovementPlanningPresenter`·배치 피드백)까지 닿으므로 따로 정한다.
- **유닛 표시 계약(보완 4, A-3a에 포함):** 창구는 `FieldUnitView`·`FieldUnitPresenter`를 직접 알지 않는다.
  - 표시 하나: 가칭 `ICombatUnitPresentationTarget`. 활성 여부와 자세·크기·배치 위치·회전·외형 층의 구현이다. `FieldUnitView`가 구현한다.
  - 표시 찾기: 가칭 `ICombatUnitTargets`. ID로 (활성) 표시를 찾고 사망 유지 대여를 잡는다. `FieldUnitPresenter`가 구현한다.
  - 그래서 배타 규칙과 소유 사실(배치 유지·회전 덮기)을 가짜 표시로 관리 실행 시험할 수 있다. 지금 이 경로의 시험은 모두 Unity 전용이다.
- **Framework:** 외형 층은 지금처럼 `IColorSystemService`의 공개 Handle로만 색을 바꾼다(FW-203·FW-004). Color 해제 동작은 DR-004 확인 대상이라 지금 `MovementModelOpacity`의 복원 방식을 그대로 쓴다.

### B14. 겹침 규칙 보존

아래 규칙은 지금 화면·순서·완료 시점을 정한다. 개정은 **규칙을 바꾸지 않고 자리만 옮긴다.** "지키는 시험"의 관리 = Unity 없이 도는 시험, Unity = Unity Test Runner 전용.

| # | 규칙 | 지금 자리 | 개정 뒤 자리 | 지키는 시험 |
|---|---|---|---|---|
| O1 | 히트 전 공격은 프레임을 독점(뒤 재생기 정지) | `_attacks.Tick` false | 공격 재생기 `Tick` 반환(그대로) | 실행 목록 시험(관리), `CombatSceneEventPresenterTests`(Unity) |
| O2 | 재장전 완료를 알린 프레임은 거기서 끝, 재장전 중 다음 시작 금지(단정) | 공격 `Tick`, `BlocksNextEvent` | 공격 재생기 | `CombatBallistaAttackTests`(Unity) |
| O3 | 포효 히트 전에는 능력 동작만 진행 | `TickPresentation` 2단계 | 능력 재생기 `Tick` 반환 = 공격 재생기가 기록한 프레임 시작 출처 겹침 | 실행 목록 시험, 능력 재생기 시험(관리), 포효 시험(Unity) |
| O4 | 포효 공격 완료로 재개된 작아지기는 그 프레임 시간 0 | `!attackWasPlaying` | 능력 재생기: 프레임 시작에 공격 대기 상태였으면 Tick 안 함 | 능력 재생기 시험(관리), `CombatAbilityMotionPlaybackTests`에 `IsWaitingForAttack` 시험 추가 |
| O5 | 히트 뒤 포효 작아지기와 피해 반응·넉백 걸음이 겹침 | 출처 겹침 → 3·4단계 진행 | 같은 반환 규칙 | 포효·넉백 동시 시험(Unity) |
| O6 | 간격 동안 모든 레인 정지 | `TickMotion` | 걸음·대기 재생기 | `CombatEventWaitTests`(관리), `CombatEnemyPresentationTests`(Unity) |
| O7 | 간격 뒤 대기 사건을 처리한 프레임은 레인 시간 0 | `TickMotion` | 걸음·대기 재생기 | 같음 |
| O8 | 대기 사건이 있으면 시작 단정 실패 | `RequireIdle` | 걸음·대기 재생기 `FindBlock`(단정) | 프로필 시험(관리) |
| O9 | 같은 유닛의 반응·걸음은 겹치지 않음, 행동·주체 없는 사건은 모두 끝난 뒤 | `ReactionBlocks`·`MotionBlocks` | 반응·걸음 재생기 `FindBlock` | `CombatSceneEventPresenterTests`, `CombatUnitReactionSetTests`(Unity) |
| O10 | 걸음 Gate 규칙 1·2(이동자 자신, 자발적 이동 중 이동 후속, 접촉 묶음 해제 전) | `_motion.CanBegin` | 걸음 재생기 `FindBlock`(질의) → `CombatStepContactGate` 그대로 | `CombatStepContactGateTests`(관리) |
| O11 | 걸음 사건 완료 뒤 남은 보간·미룬 방향 복귀와 겹침 허용 | `MotionBlocks(…, allowOverlap)` | 걸음 재생기 | `CombatMovementViewTests`(Unity) |
| O12 | 이 프레임에 시작한 반응은 시간 0 | 프레임 시작 Version | 반응 재생기 `BeginFrame`에서 기록 | `CombatUnitReactionSetTests`(Unity), 실행 목록 시험 |
| O13 | 반응 불가는 다음 Tick 맨 앞에서 예외 | `TickPresentation` 0단계 | 반응 재생기 `BeginFrame`(모든 Tick 전) | 실행 목록 시험 |
| O14 | 기차 동시 사망은 사망끼리만 겹침 | `RequireIdle(allowDeaths)` | 동시 사망 프로필 → 반응 재생기 | `CombatRailwayContactSceneTests`(Unity) |
| O15 | 결과 시작은 모든 재생기 완전 정지 | `RequireIdle()` | 결과 시작 프로필 | 프로필 시험 |
| O16 | 최종 사실 전 공격·반응·능력 끝(재장전·걸음은 허용) | `Refresh` 단정 | `BlocksFinalFacts` | 실행 목록 시험 |
| O17 | 결과 완료 = 공격(재장전 포함)·반응·능력·걸음 끝, 사라짐 없음(대기 칸 제외) | `IsPresentationComplete` | `IsComplete` | 실행 목록 시험, `CombatPresentationCleanupTests`(관리) |
| O18 | 표시 사라짐은 모든 시작 금지 | 질의 첫 줄 | 재생기마다 `Unavailable` | 프로필 시험 |
| O19 | 걸음 중 `RenderFacts`는 배치를 쓰지 않음, 사망 유지 모델은 자세 유지, 표시 회전이 사실 회전을 덮음 | `RenderFacts`의 세 표 | 창구가 답하는 소유 사실 | `CombatMovementViewTests`·`CombatUnitDeathTests`(Unity) |
| O20 | 순서·같은 순간 묶음·기차 배타·세대 무효화 | 스케줄러·재생 조정·세대 | 바뀌지 않음 | `OrderedCombatSchedulingTests`·`CombatConcurrentGroupsTests`(관리) |

**동등성 근거가 필요한 곳 둘:**

- **O4:** 지금은 "프레임 시작에 공격이 진행 중이었으면 능력 Tick 안 함"이다. 능력이 진행 중이면서 공격이 진행 중인 경우는 포효의 정면 공격(능력 상태 `RoarAttack`)뿐이다. 근거: 능력 진행 중에는 질의가 출처 겹침 없이 시작을 막고, 히트 뒤에도 Gate 규칙 1이 행동 사건을 전체 종료까지 막는다. 그 상태의 능력 Tick은 지금도 아무것도 하지 않는다. 반대로 능력이 공격 대기 상태면 정면 공격이 진행 중이다(공격 완료 콜백이 작아지기로 넘기고, 취소는 둘을 함께 비움). 공격 완료 콜백으로 같은 프레임에 재개된 작아지기도 프레임 시작에는 공격 대기 상태였다. 그래서 "프레임 시작에 공격 대기 상태였으면 Tick 안 함"과 같다. 표시가 사라진 경우에는 공격 재생기가 같은 프레임에 먼저 예외를 던진다(자세·크기 대여가 함께 취소됨).
- **정리 순서:** 지금 `CancelMovement`는 걸음을 먼저, 정리 묶음 밖에서 취소한다. 걸음 취소가 실패하면 나머지 정리를 하지 않는다. 그 뒤 공격 → 반응 → 능력 순서로 묶음 정리한다. 목록 순서(공격 → 능력 → 걸음·대기 → 반응)로 한 묶음 정리하면 정상 경로는 같고(각 재생기는 다른 채널·유닛을 되돌린다) 실패 경로만 달라진다. Q-R7로 묻는다.

### B15. 다음 단계 — 조각·조립의 자리 (옛 B1·B2·B4·B6~B8·B10 정리)

바탕(B11~B13)이 끝난 뒤 별도 설계·계획으로 한다. 이 개정은 그 자리가 생기는지만 확인한다.

> **상세 설계(2026-10-18, B-0):** [조각·조립 상세 설계](2026-10-18-combat-presentation-pieces-design.md) BP1~BP9. 아래 Q-R2~Q-R5는 거기서 Q-B1~Q-B4로 다시 묻는다. 아래와 다른 점(외부 진행 조각, 실행의 자원 소유, 소리 포트를 만들지 않음 등)은 상세 설계가 원본이다.

- **조립은 코드다**(2026-10-17 사용자 결정). 사건 처리기가 맥락(주체·행위자·출처·칸)으로 "이 사건에 이 조각들을 이 시점에" 목록을 만든다. 데이터 조립(옛 B3·B9)은 하지 않는다.
- **조각 실행 집합**(옛 B4 실행기를 사건마다 하나씩 담는 재생기 하나)을 실행 목록에 등록한다. Presenter 멤버는 바뀌지 않는다(B12의 목적).
- **조각**(옛 B1): 시작·진행·끝·취소·신호. 자원은 B13 창구 채널로만 빌린다(옛 B6을 B13이 대신함).
- **시점**(옛 B2): 사건 시작, 다른 조각의 신호, 지연.
- **완료**(옛 B8): 출처(공격·포효)는 H3 Gate 정책 그대로, 그 밖은 "기다림" 조각이 모두 끝날 때.
- **소리 창구**(옛 B7, Q-R5 대기)·**구조물·장애물**(옛 B10)은 초판 결정 그대로 이어 쓴다.
- **옮길 순서(안):** 피격(코드 반응 모션 조각 + 플래시 조각, 외형 층) → 포효·소환(크기·등장 조각으로 재조립, 평가 P6) → 공격(공격 모션 조각) → 소리 창구.

**확인 예 — 바탕 뒤 히트 플래시 넣기:**

| | 지금 | 바탕 뒤 |
|---|---|---|
| 고칠 곳 | 반응 Presenter 또는 새 재생기 + Presenter 8곳 + View 외형 직접 처리 + 투명도와 PropertyBlock 다툼 | 플래시 조각 하나(외형 층 대여) + 피격 처리기의 조립 목록 한 줄. 조각 실행 집합이 처음 한 번 등록돼 있으면 Presenter는 안 고침 |
| 이해할 코드 | Presenter Tick·시작 조건·완료, 대여 규칙, 투명도 구현 | 조각 계약, 외형 층 |
| 검증 | 반응·이동 미리보기·취소·동시 피격 | 플래시 조각 시험 + 피격 재생 |

## 5. 원칙 점검 (결정 Q-R7~Q-R12 A·보완 1~4 반영 뒤)

기준은 설계 세션 인계 §4다. "지금"은 2026-10-18 평가(P1~P8), "개정 뒤"는 A-1~A-3a를 마친 상태다. 남는 항목의 원인 분석과 개선안은 §9에 있다.

| 기준 | 지금 | 개정 뒤 | 한 줄 근거 |
|---|---|---|---|
| SSOT | 주의 | 유지(남는 것 있음) | 시작 규칙·재생기 수명·유닛 자원 소유·프레임 시작 출처 겹침이 각각 한 곳. 경로 분류·포효·소리 시점·연출 값은 그대로 |
| SOLID | 주의 | 유지(S 일부 주의) | Presenter는 목록과 역할 계약만 앎. 처리기·재생기는 창구만 받음. `FieldUnitPresenter`의 HUD·사실 그리기는 남음 |
| FW 지침·계약 | PASS(WARNING 2) | PASS(WARNING 1) | 새 Service 없음, Handle식 대여로 View 누출 제거. §26은 줄지만 남고, §27은 근거 있는 추상화 |
| 변경 독립성 | 주의 | 좋음(포효·소리 제외) | 새 재생기·시작 규칙·걸음 배치·외형 층이 한 곳. 포효 순서·피격 소리는 B15까지 그대로 |

### 5.1 SSOT

- **한 곳이 되는 것:**
  - 시작 규칙 = 재생기마다 `FindBlock` 하나(B11). 질의·단정 차이는 같은 함수 안의 프로필 행으로 보존한다(Q-R8 A). "두 판을 따로 고치다 어긋남"은 없어지고, 차이는 표로 드러난다.
  - 재생기 수명 = 실행 목록 하나(B12). Presenter는 재생기를 조립에서만 이름으로 안다(보완 1·2).
  - 프레임 시작 출처 겹침 = 공격 재생기의 기록 하나(보완 3).
  - 유닛 표시 소유 = 창구 하나(B13). `RenderFacts`는 소유 사실을 창구에 묻는다.
  - 세대 = Presenter 하나(D10 유지). 출처 겹침 지금 값 = `CombatHitGate` 하나.
- **나뉘지만 복제는 아닌 것:** 반응 항목(부품)과 반응 불가 상태(재생기)는 같은 반응의 두 상태가 두 객체에 있다. 각 원본은 하나이고, 불가 상태를 바꾸는 곳도 재생기 하나다.
- **그대로 남는 것:** 경로 분류(P5: 스케줄러 `IsMovement`, 재생 조정 `IsRailway`), 포효 지식(P6), 소리 시점(P7), 연출 값 세 계층(P8), 간격 값의 사건 형식 분기(`CombatEnemyPresentationPacing`, 분리 S2에서 값 원본으로 수용).

### 5.2 SOLID

- **S:** Presenter = 공통 수명·틀·진입점. 실행 목록 = 순서와 합성. 재생기 = 자기 부품의 수명 규칙. 창구 = 유닛 표시 소유. 남는 것: `FieldUnitPresenter`의 HUD 피드백·사실 그리기(Q-R10 A로 미룸), `CombatAttackPlayback`의 포효 정면 공격 지식(P6, B15).
- **O:** 새 재생기 = 계약 구현 + `Create`에 자리 하나. 새 시작 규칙 = 그 재생기의 `FindBlock` 한 곳. 새 채널은 창구·표시 계약을 고쳐야 하지만 드물게 늘어난다(지금 계획은 외형 층·Animator 둘).
- **L:** 지켜야 할 뜻이 생긴다. `Tick` false는 "이 프레임의 뒤 재생기를 멈춤"만 뜻하고 실패가 아니다(실패는 예외). `BeginFrame`은 진행하지 않는다. `IsComplete`·`BlocksFinalFacts`는 부수효과가 없다. 실행 목록 시험이 이 뜻을 가짜 재생기로 고정한다.
- **I:** 처리기·재생기·이동 표시는 창구와 역할 계약(`ICombatWaitSlot`·`ICombatSourceOverlap`)만 받는다. 처리기는 시작 API를 부품에서, 수명은 재생기에서 받아 같은 부품에 두 경로가 생긴다(Q-R9 A의 대가). 두 경로 모두 좁고, 상태를 복제하지 않는다.
- **D:** 실행 목록·능력 재생기·창구 규칙은 계약에만 의존해 관리 실행 시험이 된다. 공격·걸음 부품은 투사체·Transform 때문에 여전히 Unity 시험이 필요하다.

### 5.3 JMH Framework 지침·FW 계약

| 항목 | 판정 | 근거 |
|---|---|---|
| FW-001~003, 지침 §11 | PASS | 새 Service·ServiceKey·AddModule·Build 없음. 실행 목록은 만든 뒤 바뀌지 않음(보완 2) |
| FW-004 | PASS | 외형 층은 Color 시스템의 공개 Handle만 씀(FW-203), Framework 내부 상태를 건드리지 않음 |
| FW-011 | PASS(주의) | 외형 층이 Color Handle을 쓰는 방식은 지금 `MovementModelOpacity` 그대로. Color 해제는 DR-004 확인 대상이라 바꾸지 않음 |
| FW-016·지침 §15 | PASS | Tick 소유자 사슬 그대로(`CombatViewSystem` → 스케줄러 → Presenter → 실행 목록) |
| FW-101 | PASS | 소비자는 필요한 계약만 받음(창구, 대기 칸, 출처 겹침) |
| 지침 §8 Handle | 개선 | 반응 경로가 `FieldUnitView`를 받던 것을 채널 대여로 바꿈. 외부 코드가 Unity 객체를 직접 다루지 않음 |
| 지침 §19 할당 | PASS | 목록은 고정 배열 순회, 프레임 맥락·질문은 구조체(`in`), 재생기·창구는 재사용. 프레임마다 새 할당 없음 |
| 지침 §20·21 | PASS | 이벤트 구독 없음. 대여 취소는 지금처럼 버전·콜백 |
| 지침 §26 God Class | WARNING(줄어듦) | Presenter 420줄 → 더 줄어듦. `FieldUnitPresenter`는 대여·회전·배치·값 조회가 빠지지만 HUD·사실 그리기가 남음 |
| 지침 §27 과도한 추상화 | PASS | 계약 하나에 구현 넷(B15로 다섯째). 얇은 재생기 넷은 Presenter의 규칙을 옮길 자리라 근거가 있음. 기본 구현·제네릭 계층 없음 |

### 5.4 기능 변경의 독립성

| 변경 | 지금 | 개정 뒤 |
|---|---|---|
| 새 재생기(조각 실행 집합 등) | Presenter 8개 멤버 + 시작 조건 두 판 | 계약 구현 + `Create` 자리 하나. 실행 목록 시험이 순서를 지킴 |
| 시작 규칙 하나 바꾸기 | 질의판·단정판 두 곳 | 그 재생기 `FindBlock` 한 곳 + 프로필 시험 |
| 걸음 배치·회전 규칙 | 레인 + `FieldUnitPresenter` 세 표 + `RenderFacts` | 창구 배치·회전 채널과 소유 사실 + 창구 시험(관리) |
| 대기·간격 동작 | Presenter `StartGap`·`TickMotion`·`BeginEvent` | 걸음·대기 재생기 + 대기 칸 계약. 간격 값은 그대로 |
| 플래시(외형 층) | View 외형 직접 처리, 투명도와 다툼 | 외형 층 대여. 조각으로 쓰는 것은 B15 |
| 반응 모션 값 | 반응 Presenter | 같음(이미 좁음) |
| 포효 순서 | 처리기·능력 상태·공격 재생·Gate·머묾 | **같음.** B15 재조립 전까지 넓음 |
| 피격 소리 | 소리 클래스 + 캐릭터 처리기 + AudioSystem 슬롯 | **같음.** B15 소리 창구 전까지 |

- 남는 공통 경계: 재생기 계약의 뜻(`Tick` 반환·`BeginFrame`), `Create`의 자리 순서, 프로필 정의, 창구 채널 규칙. 이것이 바뀌면 실행 목록·프로필·창구 시험(관리)과 Unity 재생 시험 전체를 확인한다.

### 5.5 개정 뒤에도 남는 것

| 항목 | 다룰 곳 |
|---|---|
| 경로 분류 세 곳(P5) | 바탕 뒤 별도(평가 정리 방향 5) |
| 포효 지식 다섯 곳(P6) | B15 포효·소환 재조립 |
| 소리 시점 네 곳(P7) | B15 소리 창구(Q-R5) |
| 연출 값 세 계층(P8) | 결정만 필요(평가 정리 방향 "-") |
| `FieldUnitPresenter` HUD·사실 그리기 | A-3b(Q-R10 B로 바꿀 때) |
| 재생 계약 전달 사슬(N7) | 수용 유지 |
| 공격·걸음 부품의 Unity 전용 시험 | 투사체·Transform 의존. 순수 규칙은 Gate·레인 Gate·실행 목록 시험이 지킴 |

## 6. 위험

| 위험 | 대응 |
|---|---|
| 시작 조건을 옮기다 조건 하나가 빠지거나 프로필이 섞임 | A-1에서 조건식을 재생기 경계로 **그대로** 나눈다. 순수 부품만 감싸는 능력·반응 재생기의 프로필별 `FindBlock`과 실행 목록의 합성은 관리 실행 시험으로, 공격·걸음 재생기는 Unity 시험으로 지킨다(부품이 투사체·Transform에 의존) |
| 프레임 순서·이른 반환이 미묘하게 달라짐 | 실행 목록 시험: 멈춤 반환, `BeginFrame` 선행, `Create` 자리 순서. O3·O4는 능력 재생기 시험(관리) |
| 정리·해제 순서 변경 | Q-R7. 정상 경로는 같음(각 재생기가 다른 채널을 되돌림). `CombatPresentationCleanupTests`(관리)가 실패 모음 동작을 지킴 |
| 창구 배타 검사가 새 예외 경로가 됨(자세와 걸음 배치) | Q-R11. 지금 도달하지 않는 조합이라 화면은 같음. 어기면 조용히 겹치는 대신 예외 |
| `RenderFacts` 소유 사실 이동으로 배치·회전 회귀 | O19 시험(Unity)과 걸음·사망 재생 확인 |
| 외형 층 이동으로 이동 계획 미리보기 투명도 회귀 | 입력 쪽 이동 계획 시험과 재생 확인. 첫 단계는 층 하나(이동 투명도)만 |
| 시험이 리플렉션으로 읽는 Presenter 필드 | 부품 필드 이름 유지(`_attacks`·`_reaction`·`_motion`·`_facts`·`_sound`). 바꾸면 계획에 시험 경로 변경을 적음 |
| Presenter·처리기 경로 시험이 관리 실행에서 모두 Unity 전용 | 실행 목록·프로필·창구 배타 규칙을 순수 클래스로 빼 관리 실행 시험을 둠. Phase마다 Unity 묶음 결과(Q-R12) |
| Unity 결과를 Phase마다 받지 못하면 회귀 원인을 가르기 어려움 | Q-R12 |

## 7. 사용자 결정

### 7.1 이번 개정에서 정할 것

> **결정(2026-10-18):** 사용자 "추천안대로 가는데". Q-R7~Q-R12 모두 **A**다.
> **변경(2026-10-18, §9.4):** Q-R8은 Q-R14 A로, Q-R10은 Q-R15 A로 바뀌었다. 질의·단정 차이는 A-1·A-2에서 보존하고 A-2b에서 맞춘다. A-3b(HUD 분리)를 한다.

- **Q-R7. 정리·해제 순서:**
  - A(권장): 실행 목록 순서로 한 묶음 정리(실패를 모아 끝에 던짐). 정상 경로는 같고, 걸음 취소가 실패해도 다른 정리를 계속한다.
  - B: 지금 순서 그대로(걸음 먼저·묶음 밖, 그다음 공격 → 반응 → 능력). 실행 목록에 정리 순서를 따로 둔다.
- **Q-R8. 질의판·단정판 차이(대기 사건·재장전·걸음 Gate):**
  - A(권장): A-1·A-2에서는 차이를 프로필 규칙으로 그대로 보존하고, 막는 이유를 예외 문구 끝에 덧붙인다(문구 앞부분은 그대로). 맞추는 것은 바탕 뒤 별도 결정.
  - B: 지금 맞춘다. 질의에도 대기 사건·재장전을 넣는다. 도달하지 않는 경로라 화면은 같고, "단정 예외"가 "기다림"이 된다.
- **Q-R9. 재생기 구현 형태:**
  - A(권장): 지금 부품을 감싸는 얇은 재생기 넷. 부품 API·시험은 그대로이고 Presenter의 규칙만 옮긴다.
  - B: 부품이 계약을 직접 구현. 클래스는 적지만 부품 시험·API가 바뀐다.
- **Q-R10. 유닛 창구 범위:**
  - A(권장): A-3a만(대여 창구·소유 사실·외형 층 첫 층·연출 값 읽기). HUD 피드백·사실 그리기 분리(A-3b)는 입력 쪽 호출자에 닿으므로 바탕 뒤 따로.
  - B: A-3a와 A-3b를 함께.
- **Q-R11. 같은 유닛의 자세와 걸음 배치 배타 검사:**
  - A(권장): 창구가 검사한다. 지금 도달하지 않는 조합이고, 어기면 예외로 드러난다.
  - B: 지금처럼 검사하지 않는다.
- **Q-R12. Unity 확인 간격:**
  - A(권장): 분리 계획 §1 원칙대로 Phase마다 Unity 묶음 결과를 받은 뒤 다음 Phase.
  - B: S-2~S-5처럼 마지막에 몰아서.

### 7.2 초판 결정의 상태

| 결정 | 상태 |
|---|---|
| Q-R1 조립 위치 | **결정됨(2026-10-17): 코드.** SO 조립 폐기 |
| Q-R2 조립 선택 열쇠(사건 종류 × 주체 외형) | B15 설계 때 다시 묻는다 |
| Q-R3 코드 반응 모션과 클립 겹침 | B15(클립) 때 |
| Q-R4 조각 실패 처리 | B15 때 |
| Q-R5 지금 소리 호출 | B15(소리 창구) 때. 지금은 R4 유지 |
| Q-R6 착수 시점 | **해소.** 동시 처리 C-1~C-4와 Presenter 분리 S-1~S-5 완료 뒤 |

## 8. Phase 개요

Task 계획: [연출 바탕 계획](../plans/2026-10-18-combat-presentation-foundation-plan.md). 순서(Q-R13 A): A-1 → A-2 → A-2b → A-3a → A-3b → A-4 → A-4b → A-5 → B-0(B15 상세 설계) → B-1 → B-2 → B-3 → B-4. 아래 표는 A-1~A-3a이고, 개선 Phase(§9.2)의 상세는 계획에 있다.

| Phase | 내용 | 동작 변화 | 새 관리 실행 시험 |
|---|---|---|---|
| A-1 | 시작 조건 하나(B11): 질문·막는 이유·프로필, 재생기별 판단 도우미 넷, 질의판·단정판 파생 | 없음(예외 문구 끝에 막는 이유만 붙음) | 프로필 정의(질문 → 겹침·사망 겹침) |
| A-2 | 재생기 계약·실행 목록(B12): 얇은 재생기 넷, `Create` 자리 고정(보완 2), 대기 칸 계약(보완 1), 출처 겹침 계약(보완 3), Presenter 8개 멤버를 목록으로, 반응 불가 상태 이동 | 없음(정리 실패 경로만 Q-R7 A대로) | 실행 목록(가짜 재생기), 능력 재생기, 반응 재생기, `IsWaitingForAttack` |
| A-3a | 유닛 표시 자원 창구(B13): 유닛 표시 계약(보완 4)·채널 대여·소유 사실·View 누출 제거·외형 층(이동 투명도)·연출 값 읽기 | 없음(도달하지 않는 자세·배치 겹침만 예외, Q-R11 A) | 창구 배타 규칙·소유 사실(가짜 표시) |
| A-3b | `FieldUnitPresenter` HUD 피드백 분리(Q-R15 A, §9 I-2) | 없음 | — |
| 다음 | 조각·조립(B15) 별도 설계: 피격·플래시 → 포효·소환 재조립 → 공격 → 소리 창구 | 조립에 넣은 만큼 | — |

- 동작 동일 Phase다. 동작이 달라지는 것을 찾으면 구현하지 않고 묻는다.
- Phase마다 MSBuild 오류·새 경고 0, 관리 실행 메서드 단위 회귀 0(분리 계획 §7 방법), Unity 묶음(Q-R12), 검토 에이전트, 멈추고 보고.
- Unity 확인 묶음: A-1·A-2 = 재생 시험 전체(`CombatSceneEventPresenterTests`, `CombatEnemyPresentationTests`, `CombatUnitDeathTests`, `CombatUnitReactionSetTests`, `CombatBallistaAttackTests`, `CombatMovementViewTests`, `CombatRailwayContactSceneTests`, 공격 시험들), A-3a = 그에 더해 이동 계획·배치 미리보기 시험.
- 공유 문서: Factory가 바뀌면 민형님 Factory 공유 문서에 절 추가. 외형 층이 Color Handle 사용 방식을 바꾸면 민형님께 공유(지금 방식 유지 목표).

## 9. 원인 분석과 개선 (2026-10-18)

§5의 평가는 "개정 뒤에도 남는 것"을 다음 일로 미뤄 두었다. 사용자 지적("평가 결과가 저러면 원인 분석과 개선을 해야 하지 않을까")에 따라, 남는 항목이 왜 생겼는지부터 따지고 고칠 방법을 정한다.

### 9.1 남는 항목과 근본 원인

| 증상(§5) | 근본 원인 | 고칠 문제인가 |
|---|---|---|
| 사건 경로 분류 세 곳(P5): 스케줄러 `IsMovement`, 재생 조정 `IsRailway`, 처리기 표 | **C1.** 표시 지식을 Presenter 층에서만 정리했다. 위층은 기능을 더할 때마다(걸음 레인 C-3, 기차 H-3·C7) 자기 형식 검사를 붙였다. "이 사건은 어느 경로로 표시되나"라는 사실의 원본이 없다 | 예 |
| 재생 계약 전달 사슬(N7): 계약 멤버 하나에 네 곳 | **C1.** 같은 원인. 표시 전체 계약(`ICombatBattleDisplay`)이 재생 계약을 상속해서, Display가 재생 조정의 멤버 14개를 그대로 위임한다 | 예(이전 "수용" 결정을 다시 봄) |
| 포효 지식 다섯 곳(P6), 공격 재생이 포효를 앎(SOLID S) | **C2.** 여러 단계·여러 자원을 엮는 합성 연출을 표현할 수단(조각·시점)이 없다. 그래서 순서를 부품 상태 기계(`CombatAbilityMotionPlayback`의 `RoarDelay → RoarGrow → RoarAttack → RoarShrink`)와 콜백 사슬(`BeginRoar`·`TryBeginFacing`·`EndRoar`, 강조 끄기)에 넣었다 | 예 |
| 소리 시점 네 곳(P7) | **C2.** 같은 원인. 틀이 ②에서 연출을 시작하면 ③ 소리를 건너뛰므로, 사망은 처리기가 소리를 직접 부르고, 공격은 연출 콜백으로, 걸음은 레인 콜백으로 부른다. "이 사건에 무슨 소리를 내나"가 틀의 switch와 처리기 호출로 나뉜다 | 예. 단, "언제"를 그 시계를 가진 부품이 정하는 것은 의도다(초판 원칙). 고칠 것은 "무엇을 내나"의 분산이다 |
| `FieldUnitPresenter` HUD·사실 그리기 남음(§26 WARNING, P3) | **C3.** 유닛 표시 집합의 입구가 `FieldUnitPresenter` 하나뿐이라, 유닛에 닿는 기능이 생길 때마다 거기에 붙었다. A-3a는 대여만 뺀다 | 예 |
| 연출 값 세 계층(P8) | **C4.** 값을 어디에 둘지 규칙이 없었다. 유닛별 값은 DB(UnitVisual), 전역 시간 값은 씬(`CombatSceneRoot`, 조정 편의), 소리 프리셋은 코드(R4로 범위 밖). 인계 §4.1 "기획 입력 값의 원본 = DB SO"와 어긋난다 | 예(소리 프리셋은 Audio 작업자 몫) |
| 질의·단정 차이 보존(Q-R8 A) | **C5.** 동작 동일 원칙을 지키려고, 도달하지 않는 차이를 일부러 남겼다 | 예. 바탕을 옮긴 직후에 맞추면 위험이 작다 |
| 공격·걸음 부품의 Unity 전용 시험 | 투사체·Transform을 다루는 부품이다. 순수 규칙(Gate·레인 Gate·묶음·간격)은 이미 분리돼 관리 실행 시험이 지킨다 | 아니요(정상) |

### 9.2 개선안

| # | 원인 | 개선 | 결과 | 동작 변화 |
|---|---|---|---|---|
| I-1 | C5 | **A-2b 질의·단정 정렬:** A-2 뒤, 재생기 `FindBlock`의 질의 행과 시작 단정 행을 합집합 하나(시작 프로필)로 맞춘다. 사건·걸음 시작은 "시작 프로필이 막으면 예외" 한 번으로 줄어든다 | 시작 규칙이 재생기마다 판 하나. 프로필은 시작·결과 시작·동시 사망 셋 | 화면 같음. 도달하지 않는 경로에서 "단정 예외"가 "기다림"이 됨 |
| I-2 | C3 | **A-3b HUD 분리:** A-3a의 `ICombatUnitTargets`를 써서 HUD 피드백(피해 숫자·비용 미리보기·체력 미리보기·타임라인·목표 표시)을 가칭 `CombatUnitHudFeedback`으로 뺀다. `FieldUnitPresenter`는 표시 집합·사실 그리기·몸 조회만 남는다 | §26 WARNING 해소. 입력 피드백이 HUD 계약만 받음 | 없음 |
| I-3 | C1 | **A-4 사건 경로 표:** 가칭 `CombatEventRoutes`(정적, 순수)가 사건 → {걸음, 복귀, 기차, 사건 틀, 건너뜀}을 답한다. 스케줄러(`IsMovement`·`CanPresent`), 재생 조정(`IsRailway`), 처리기 표(형식 목록)가 이것 하나를 읽는다. 처리기 표는 "사건 틀" 형식마다 처리기가 있는지 만들 때 확인한다 | "이 사건은 어떻게 보이나"의 원본 하나. 재생 계약에서 `CanPresent`가 빠짐 | 없음. 가짜 재생 계약 시험 2개(`OrderedCombatSchedulingTests`, 천명님 작성 `FrenzyViewContractTests`)에서 `CanPresent`를 뺌 |
| I-4 | C1 | **A-4b 재생 계약 사슬 줄이기:** `ICombatBattleDisplay`가 `ICombatEventPlayback`을 상속하지 않고 `EventPlayback` 속성으로 재생 조정을 넘긴다. 스케줄러는 그것을 받는다 | Display의 위임 14개가 없어짐. 계약 멤버 추가가 두 곳(계약·재생 조정) | 없음. 가짜 표시 시험 1개 수정(`CombatViewSystemTests`). 호진님 `HojinCombatAttackDragIntegrationTests`는 표시 계약을 읽기만 해 영향 없음(2026-10-18 확인) |
| I-5 | C4 | **A-5 전역 연출 값 원본:** 걸음·회전 시간, 적 행동·턴 간격, 포효·소환 값을 View DB 설정 표 하나(가칭 `CombatPresentationSettings`, 항목 정확히 하나 — `StageFieldView`·`InputSettings`와 같은 모양)로 옮긴다. `CombatSceneRoot`에서 값이 빠지고 Factory가 표에서 읽는다. 검증(`ValidatePresentationTiming`)은 `GetData`로 | 기획 값 원본이 DB 하나(유닛별은 UnitVisual, 전역은 설정 표) | 없음(같은 값으로 옮김). DB 에셋·`ViewGroup.asset`을 코드와 한 커밋으로 |
| I-6 | C2 | **B 단계를 바탕 바로 뒤로:** B-1 조각·실행 기반(조각 계약·시점·조각 실행 집합 재생기, 순수 시험) → B-2 포효·소환 재조립(능력 상태 enum과 공격 재생의 포효 메서드를 크기·등장·정면 공격 조각과 포효 조립으로, Gate 출처 수명은 실행이 알림) → B-3 소리 조각(틀 ③과 처리기의 소리 호출을 "사건 소리" 조각 하나로, 값·프리셋은 그대로) → B-4 피격 플래시(외형 층 조각) | 포효 순서 = 포효 조립 한 곳. "무엇을 내나" = 소리 표 한 곳 | B-2 없음(같은 순서). B-3은 같은 프레임 안 호출 순서만 바뀜(Q-R18). B-4는 플래시를 넣은 만큼 |

- I-1~I-5는 바탕(A) 단계를 마무리하는 일이고, I-6은 초판 목표(요구 5)를 푸는 일이다.
- **I-4 실행 보정(2026-10-18, A-4b):** 표시의 재생 위임 시작 계열 여섯에 있던 해제 뒤 예외(리팩토링 설계 D9)는 위임과 함께 없애지 않고 재생 조정(`CombatSceneEventPlayback`)으로 옮겼다(사용자 결정). 위임은 열하나였고(`CanPresent`는 A-4에서 없앰), 바꾼 시험은 가짜 하나가 아니라 Display를 재생 계약으로 쓰던 시험 85곳이다. 예외 문구는 `[CombatSceneEventPlayback] 씬 연결이 해제된 재생입니다.`로 바뀐다. 기록은 [계획 §8·§12.7](../plans/2026-10-18-combat-presentation-foundation-plan.md).
- **I-2 실행 보정(2026-10-18, A-3b):** 타임라인·목표 표시는 HUD 피드백으로 옮기지 않고 `FieldUnitPresenter`의 사실 그리기에 남겼다. 둘은 사실에서 매번 다시 쓰는 상태이고, HUD 피드백(`CombatUnitHudFeedback`)은 일시 피드백(피해 숫자·비용·예상 체력)만 맡는다. 배치 피드백은 숨김(`SetHiddenEntity`) 때문에 아직 `FieldUnitPresenter`를 받아, "입력 피드백이 HUD 계약만 받음"은 공격·이동 피드백에 해당한다. 기록은 [계획 §6·§12.5](../plans/2026-10-18-combat-presentation-foundation-plan.md).
- 소리 프리셋 값(코드 상수)은 Audio 작업자가 소리 창구(옛 B7) 방식을 정할 때까지 그대로 둔다. 우리 쪽에서 고칠 수 있는 것은 "무엇을 내나"를 한 곳으로 모으는 데까지다.

### 9.3 개선 뒤 예상 평가

| 기준 | §5(A-1~A-3a 뒤) | 개선 뒤(I-1~I-6) | 남는 것 |
|---|---|---|---|
| SSOT | 유지, 남는 것 있음 | 유지 | 소리 프리셋 값(Audio 작업자 결정 대기) |
| SOLID | 유지, S 일부 주의 | 유지 | `CombatViewSystem` 크기(연출 밖, 재평가 N1) |
| FW 지침·계약 | PASS, WARNING 1 | PASS | 연출 범위 밖의 §26(`CombatViewSystem`) |
| 변경 독립성 | 좋음, 포효·소리 제외 | 좋음 | 새 채널(예: Animator)은 창구·표시 계약 수정이 필요(드묾) |

### 9.4 사용자 결정

> **결정(2026-10-18):** 사용자 "Q-R13~Q-R18 모두 추천안(A)으로 결정". Q-R14는 Q-R8을, Q-R15는 Q-R10을 바꾼다. Q-R16은 재평가 N7의 "수용"을 바꾼다. 개선 Phase는 [Task 계획](../plans/2026-10-18-combat-presentation-foundation-plan.md)에 더했다.

- **Q-R13. 개선 범위와 순서:**
  - A(권장): I-1~I-6 모두. 순서 A-1 → A-2 → A-2b → A-3a → A-3b → A-4 → A-4b → A-5 → B-1 → B-2 → B-3 → B-4. Phase마다 지금 규칙(동작 동일, Unity 결과 뒤 다음, 멈추고 보고).
  - B: A 단계 보완(I-1·I-2·I-3)만 먼저 계획에 넣고, I-4·I-5·I-6은 A 단계가 끝난 뒤 다시 정한다.
- **Q-R14. 질의·단정 정렬(Q-R8 변경):** A(권장) A-2b에서 맞춘다 / B 지금처럼 보존한다.
- **Q-R15. HUD 분리(Q-R10 변경):** A(권장) A-3b를 한다 / B 미룬다.
- **Q-R16. 재생 계약 사슬(재평가 N7 "수용" 변경):** A(권장) A-4b로 줄인다 / B 수용을 유지한다.
- **Q-R17. 전역 연출 값 원본:** A(권장) View DB 설정 표 / B 씬에 두고 "전역 값은 씬" 규칙만 문서에 적는다(조정 편의 우선).
- **Q-R18. 소리 조각(B-3)의 같은 프레임 안 호출 순서:** A(권장) 허용한다(소리·화면은 같고, 한 프레임 안에서 반응 시작·소리 호출의 앞뒤만 바뀜) / B 지금 순서를 보존한다(소리 조각을 사건마다 지금 자리에 맞춰 둠).
