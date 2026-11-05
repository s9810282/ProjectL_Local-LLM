# CombatSceneEventPresenter 분리 설계

- 작성: 2026-10-18. 사용자 지시 "CombatSceneEventPresenter를 먼저 나누는 게 낫지 않겠냐" → "일단 설계부터 진행해".
- **코드 변경 없음.** 결정과 계약만 적는다. Task·실행 기록은 결정 뒤 계획 문서에 둔다.
- **목표:** 동작을 바꾸지 않고 책임을 나눈다. 새 연출(플래시·클립·이펙트·소리 창구)은 이 분리 위에서 따로 설계한다([사건별 연출 조립 설계](2026-10-17-combat-presentation-composition-design.md), SO 방향 폐기 뒤 개정 대기).
- 줄 번호는 2026-10-18 작업 트리 기준이다. 이 시점에 **동시 처리 C-2가 같은 파일에서 진행 중**이었다(`CanBeginPresentationEvent`·`RequireIdle`·`ReactionBlocks`·`TickPresentation`·`TickMotion` 변경, 미커밋). 착수 때 다시 대조한다(§7 S-0).
- **결정 완료(2026-10-18, §6):** "C 관련 작업 마무리 후 진행할거고, 나머지는 추천대로". 같은 날 C-3도 코드 완료됐다(776줄, `_motion` = `CombatUnitMotionSet`, `BeginStep(step, completed)`). 그래서 분리는 **C-4·검토·커밋까지 끝난 뒤 한 번에** 한다. Task 계획은 그 시점의 코드로 S-0에서 쓴다.
- **S-0 진행(2026-10-18, 사용자 지시 "S-0은 진행해"):** C 커밋 전에 설계 대조·Task 계획·임시 기준선을 먼저 했다. [분리 계획](../plans/2026-10-18-combat-scene-event-presenter-split-plan.md) §8.1. 처리기 계약은 계획 §5.1로 구체화했다(순서는 Presenter 틀 하나, 처리기는 단계별로 답함). 기준선은 C 커밋 뒤 다시 확정한다.

## 1. 현재 상태 (코드 대조)

### 1.1 크기와 위치

- 759줄. 2026-10-15 재평가 때 492줄이었고, 그때는 SV 범위 밖이라 크기만 보고 내부 책임 혼합은 따로 판정하기로 남겼다([reevaluation](2026-10-15-combat-view-structure-reevaluation.md) "남은 확인"). 히트 타이밍 H-1~H-4, 보스 활성화, 동시 처리 C-2가 모두 이 파일에 쌓였다.
- 재생 사슬: `OrderedCombatPresentation` → `CombatSceneDisplay`(가드) → `CombatSceneEventPlayback`(기차 가로챔) → **`CombatSceneEventPresenter`** → 공격·능력·걸음·반응 재생 부품.
- 재생 계약(`ICombatEventPlayback`)은 구현하지 않고 같은 이름의 멤버만 갖는다(SV3).

### 1.2 책임 묶음

| 묶음 | 상태 | 메서드(줄) | 바깥 의존 |
|---|---|---|---|
| ① 공통 수명 | `_presentationVersion`, `_mapper` | 생성자 :54, `Bind` :123, `Clear` :148, `Release` :162, `CancelMovement` :283, `CancelPresentation` :295, `BeginPresentation` :97, `RequireBound` :753 | — |
| ② 시작 조건·Tick 조합 | `_reactionUnavailable` | `CanBeginPresentationEvent` :110, `IsPresentationComplete` :107, `RequireIdle` :605, `ReactionBlocks` :620, `RequireIdleFor` :632, `TickPresentation` :219, `CancelUnavailableReaction` :582 | 공격·능력·걸음·반응 부품 상태, `CombatActionBoundary`, `CombatEventSubjects` |
| ③ 간격·머묾 대기 | `_waitingEvent`, `_waitingCompleted`, `_gapRemaining`, `_eventShownBeforeGap` | `BeginEvent` :307의 간격, `TickMotion` :252의 간격·대기 사건, `PresentEvent`의 머묾, `ClearWaitingEvent` :591 | `CombatEnemyPresentationPacing`(값) |
| ④ 표시 사실 | `_shownCharacters`, `_shownStructures` | `Refresh` :130, `ApplyCharacter` :648, `ShowCharacter` :640, `RenderEvents` :751, `FindCharacter` :744, `TryGetShownBody` :719 | `FieldUnitPresenter.RenderFacts`·`BodyOf` |
| ⑤ 재생 강조 | `_highlightCells`, `_highlightShown` | `ShowHighlight` :462, `AddOccupiedCells` :488, `ClearHighlight` :505 | 강조 층 콜백 |
| ⑥ 사건 종류 분기·연출 시작 | — | `CanPresent` :299, `PresentEvent` :348, `TryBeginAttack` :515, `TryBeginRoar` :414, `TryBeginRise` :446, `TryBeginDeath` :518, `TryBeginHit` :556, `BeginConcurrentDeath` :336, 일반 경로 사실 반영·가시·오브젝트·재장전 | 재생 부품, 가시·오브젝트·환경 이펙트 콜백 |
| ⑦ 사운드 | `_audio` | `PlayAudio` :654, `PlayUnit` :684, `PlayMovementSound` :646, `UnitPreset` :691, `AttackPreset` :702, 상수 `NoAudio` | `IAudioSystemService` |
| ⑧ 이동 진입점 | — | `BeginStep` :164, `BeginMovement` :178, `BeginMovementEnd` :184, `ShowReturn` :209, `IsMovementComplete` :95 | 걸음 재생, pacing, 강조 |

### 1.3 사건 종류 지식이 흩어진 곳

같은 사건 형식 확인이 여러 묶음에 있다. 새 사건 표시를 더하면 각각 고친다.

| 사건 | 나오는 곳 |
|---|---|
| 공격 | ⑥ 분기, ③ 간격(행위자 해석 :315), ⑦ 공격 소리, ② 공격 Gate |
| 포효 | ⑥ 분기, ⑤ 강조 칸(영향 칸 → 포효 범위 → 보스 칸), ③ 간격·머묾, ⑦ 빈 소리 |
| 소환 | ⑥ 분기·사실 반영 :372, ⑤ 강조 칸, ③ 간격·머묾 |
| 캐릭터(사망·피격·사실) | ⑥ 사망 → 피격 → 사실 순서, ⑦ 사망 소리, ④ 사실 반영 |
| 턴 | ③ 간격(턴 간격, Ballista 재장전 때문에 "간격 전에 보임" 예외 :329), ⑥ 재장전, ⑦ 턴 시작 소리·BGM |
| 구조물·영혼·가시·오브젝트·환경 이펙트 | ⑥ 일반 경로의 종류별 `if` |

### 1.4 사건 하나 안의 순서 (보존 대상)

`PresentEvent`의 순서는 동작이다. 분리 뒤에도 그대로 지킨다.

1. 행동 사건이면 앞 강조를 지운다 → 이 사건의 강조를 켠다.
2. 공격 → 포효 → 사망 → 피격 순으로 시도한다. 시작했으면 끝(그 부품이 완료를 알림).
3. 아니면: 소리 → 환경 이펙트 → 사실 반영(캐릭터·소환 생성·영혼·구조물) → 가시 소비 → 오브젝트 숨김 → 다시 그리기 → 턴 끝 재장전 → 소환 등장 → 머묾 → 완료.
4. 콜백 뒤마다 재생 세대(`version != _presentationVersion`)를 확인하고, 바뀌었으면 멈춘다.

### 1.5 묶어 둔 시험

- Presenter를 직접 만드는 시험 9곳(생성자 인자 `(units, stepSeconds, audio, gap…)`), 이름을 쓰는 시험 파일 17개.
- 리플렉션으로 읽는 필드: `_attacks`(`CombatBallistaAttackTests`), `_reaction`(`CombatRailwayContactSceneTests`·`CombatSceneEventPresenterTests`), `_audio`(`CombatRailwayContactSceneTests`가 임시로 바꿔 끼움, `CombatUnitDeathTests`), `_shownCharacters`(`CombatUnitDeathTests`).
- 정적 호출: `CombatSceneEventPresenter.UnitPreset`·`AttackPreset`(`GameAudioTests`, `StageMapCombatEntryBootSmokeTests`, `CombatStandardAttackAssetTests`, `CombatSceneEventPresenterTests`).
- `CombatSceneDisplay`는 리플렉션으로 `_events`를 읽힌다(Presenter 바깥, 영향 없음).

## 2. 범위

- **한다:** ③~⑧을 기능 클래스로 떼어 내고, Presenter에는 ①·②와 사건 처리기 표만 남긴다. 동작·재생 계약·Factory 생성자는 바꾸지 않는다(§3 S5).
- **하지 않는다:**
  - 새 연출(플래시·클립·이펙트·소리 창구), 연출 조립. 이 분리 뒤 별도 설계다.
  - 동시 처리 규칙 변경(C-2·C-3의 몫), 재생 계약(`ICombatEventPlayback`) 변경
  - 사운드 값·호출 시점 변경(R4). 위치 이동만 §6 Q-S2로 묻는다.
  - `CombatSceneEventPlayback`(기차 가로챔)·`CombatAttackPlayback`·`CombatAbilityMotionPlayback`·`CombatUnitMotionPlayback`·`CombatUnitReactionSet` 내부 변경. 콜백 연결 대상만 바뀐다.
  - 새 Framework Service·ServiceKey

## 3. 결정

### S1. 표시 사실 — `CombatShownFacts` (가칭)

- **원본:** 결과 재생 중 "지금 화면에 보인 사실"(캐릭터·구조물) 하나. 지금 ④.
- 하는 일: 캐릭터·구조물 사실 반영, 최종 사실로 바꾸기(`Refresh`), 비우기, 찾기, 몸 조회(`TryGetShownBody`), 다시 그리기(`FieldUnitPresenter.RenderFacts`).
- 읽는 쪽: 사건 처리기, 걸음 재생(`ShowCharacter` 콜백), 공격 재생(몸 조회 콜백), 사운드(프리셋용 유닛 찾기), 간격(행위자 진영).
- 최신 Buffer를 읽지 않는다는 규칙(사건 스냅샷 우선)은 이 클래스의 계약으로 옮긴다.

### S2. 간격·머묾 대기 — `CombatEventWait` (가칭)

- **원본:** 대기 칸 하나(적 행동 간격·턴 간격·걸음 전 간격·머묾). 행동은 순서대로라 칸은 하나다(동시 처리 설계 C5 "하나 유지").
- 하는 일: 대기 시작(남은 시간, 대기 사건, "간격 전에 보임" 여부), 프레임 진행(남은 시간 소비, 끝나면 대기 사건을 꺼내 줌), 비우기.
- 간격 **값**은 지금처럼 `CombatEnemyPresentationPacing`이 원본이다. 대기 클래스는 시간과 대기 사건만 갖는다.
- C-3 기대치("적 간격은 자발적 경로의 첫 걸음에만 쓰고 레인과 섞지 않는다")와 맞는다. 걸음 레인은 대기 칸을 갖지 않는다.

### S3. 사건 처리기 — 표로 분기

- 처리기(가칭 `ICombatEventPresentation`, 내부)는 **한 사건 형식의 표시 지식**을 한 곳에 둔다.
  - 표시하는가(지금 `CanPresent`의 그 형식 줄)
  - 행위자(간격용, 지금 :315)
  - 강조 칸(지금 `ShowHighlight`의 그 형식 줄)
  - 시작: 맡을 재생 부품 호출, 사실 반영, 소리 호출 위치, "간격 전에 보임" 여부(턴), 시작 못 했을 때 머묾 여부(포효·소환)
- 처리기 목록(형식 단위, 지금 분기와 1:1):

| 처리기 | 형식 | 지금 코드 |
|---|---|---|
| 공격 | `CombatAttackEvent` | `TryBeginAttack` |
| 포효 | `CombatRoarEvent` | `TryBeginRoar`, 강조 칸, 머묾 |
| 소환 | `CombatSummonEvent` | 생성 사실 반영, `TryBeginRise`, 강조 칸, 머묾 |
| 캐릭터 | `CombatCharacterEvent` | 사망 → 피격 → 사실 순서(`TryBeginDeath`·`TryBeginHit`·일반 경로) |
| 턴 | `CombatTurnEvent` | 재장전(`TryBeginReload`), "간격 전에 보임" 예외, 턴 소리·BGM |
| 사실 | 나머지 표시 형식(구조물·영혼·가시·오브젝트·토네이도·보유 환경·장애물·원본 변경·기믹·결과·효과·아티팩트·낙하) | 일반 경로의 종류별 `if` |

- 기차(`CombatGimmickEvent` 중 레일)는 지금처럼 `CombatSceneEventPlayback`이 먼저 가로챈다. 표는 그 뒤의 사건만 받는다.
- `CanPresent` = 표에 형식이 있는가. 표에 없으면 지금처럼 경고 후 건너뜀(R0).
- 처리기는 **필요한 의존성만 생성자로** 받는다. 모든 부품을 담은 맥락 객체는 만들지 않는다(인계 §4.6 "의존성을 통째로 담은 객체로 감추는 것은 개선 근거가 아니다").
  - 호출마다 받는 값: 사건, 완료 콜백, 재생 세대(시작 시점의 `version`)
  - 재생 세대는 Presenter 하나가 원본이고 처리기는 `Func<int>`로 읽기만 한다(리팩토링 설계 D10 유지).
- §1.4의 사건 안 순서는 처리기 안으로 그대로 옮긴다. 강조 지우기·켜기(1번)와 머묾·완료(3번 끝)는 Presenter가 처리기 앞뒤에서 한다.
- 사망 처리기는 `BeginConcurrentDeath`(기차 동시 사망) 경로도 같은 시작 코드를 쓴다.

### S4. 재생 강조 — `CombatPlaybackHighlight` (가칭)

- **원본:** 재생 강조 층에 보인 칸과 "보였는가" 하나. 지금 ⑤의 상태.
- 하는 일: 칸 목록 보이기, 지우기(보인 것이 없으면 표시를 부르지 않음).
- 칸 **계산**(포효 영향 칸 → 포효 범위 → 보스 칸, 소환 생성 칸)은 포효·소환 처리기로 간다(S3). 강조 클래스는 사건 종류를 모른다.

### S5. 사건 소리 — `CombatEventAudio` (가칭, §6 Q-S2)

- 지금 ⑦을 값·호출 시점 그대로 옮긴다. 프리셋 표(`UnitPreset`·`AttackPreset`)도 함께 옮긴다.
- 처리기가 지금과 같은 자리에서 부른다. 걸음 재생의 이동 소리 콜백도 이 클래스로 연결한다.
- Audio 로직 범위 밖(R4)이므로 값은 손대지 않는다. 위치를 옮기는 것만으로도 현강님 공유가 필요하다.

### S6. 이동 표시 — `CombatMovementPresentation` (가칭)

- ⑧(`BeginStep`·`BeginMovement`·`BeginMovementEnd`·`ShowReturn`, 걸음 전 간격, 자발적 이동의 강조 지우기, 경로 끝 방향 복귀 판단)을 옮긴다.
- C-3이 이 부분을 걸음 레인(`CombatUnitMotionSet`)·사건별 완료 콜백(`BeginStep(step, completed)`)으로 다시 썼다. 분리는 C 작업이 끝난 뒤 하므로(§6 Q-S1) C-3 결과를 한 번만 옮긴다.

### S7. Presenter에 남는 것

- ① 공통 수명: 재생 세대 원본, Bind·Clear·Release·취소, 결과 시작
- ② 시작 조건·Tick 조합: 부품 상태(공격 Gate·걸음 Gate·능력·반응)와 사건 주체로 "지금 시작해도 되나"를 답하고, 프레임마다 부품 진행 순서를 정한다. 이것이 조정자의 일이다(재평가 "조정자는 수명·순서").
- 사건 진입: 간격 대기(S2)에 맡기거나 처리기 표(S3)로 넘긴다.
- 부품 소유: 공격·능력·걸음·반응 재생 부품은 지금처럼 Presenter가 만들고 소유한다.
- 예상 크기: 250~300줄(① 약 80, ② 약 150, 진입·표 약 40).

### S8. 조립 — 지금처럼 Presenter 생성자 안에서 (§6 Q-S3)

- Presenter가 새 클래스(사실·대기·강조·소리·처리기)를 생성자 안에서 만든다. 지금 공격·능력·걸음 부품을 안에서 만드는 방식(D10)과 같다.
- 그래서 **Presenter 생성자·`CombatSceneDisplayFactory`·시험 생성자 9곳이 바뀌지 않는다.** Factory 공유 문서도 필요 없다.
- 처리기·부품을 Factory가 주입하는 방식은 연출 조립 작업에서 처리기를 바꿔 끼울 필요가 생길 때 다시 본다.

## 4. 원칙 점검

- **SSOT:**
  - 보인 사실 = `CombatShownFacts` 하나. 걸음·공격·소리·간격이 그것을 읽는다(사본 없음).
  - 재생 세대 = Presenter 하나(D10). 처리기·부품은 읽기만 한다.
  - 대기 칸 = `CombatEventWait` 하나, 간격 값 = `CombatEnemyPresentationPacing`
  - 강조 상태 = `CombatPlaybackHighlight` 하나, 칸 계산 = 사건 처리기
  - 사건 형식의 표시 지식 = 처리기 하나. 다만 `CombatEnemyPresentationPacing`(간격 값)과 `CombatActionBoundary`(4곳 공유 분류)는 형식 확인을 계속 가진다. 값·분류의 원본이 따로 있어서다.
- **SOLID:**
  - SRP: 수명·순서(Presenter) / 사실(S1) / 대기(S2) / 형식별 표시(S3) / 강조(S4) / 소리(S5) / 이동(S6)
  - OCP: 새 사건 표시 = 처리기 + 표 한 줄. `PresentEvent`·`CanPresent`·간격 행위자·강조를 열지 않는다.
  - LSP: 처리기는 "완료를 정확히 한 번, 세대가 바뀌면 멈춤"을 지킨다.
  - ISP: 처리기는 필요한 부품만 받는다.
  - DIP: 지금처럼 Presenter가 조립한다(S8). 바깥 의존은 기존 콜백·서비스 인터페이스다.
  - 과추상화 점검(지침 §27): 처리기 인터페이스 하나와 구현 6개는 지금 분기와 1:1이다. 새 계층·범용 Factory는 만들지 않는다.
- **Framework:** 내부 클래스만 생긴다. Service·ServiceKey·AddModule·Builder 변경 없음. Factory 생성자도 그대로다(S8).
- **기능 변경의 독립성(대표 시나리오):**

| 변경 | 지금 고치는 곳 | 분리 뒤 |
|---|---|---|
| 피격에 플래시·클립 | `TryBeginHit` + 시작 조건 + Tick이 한 파일 | 캐릭터 처리기(피격) + 반응 부품. 시작 조건은 그대로 |
| 영혼 사건 재생(SDV3, [영혼 View 계획](../plans/2026-10-15-soul-drop-plan-Hojin.md#33-sdv1sdv4-미구현-전투-view로-이관)) | `PresentEvent` 일반 경로 + 사실 반영 분기 | 영혼 처리기 하나 + 표 한 줄 |
| 아이템·광란 사건 표시 시작 | `CanPresent`·`PresentEvent`·행위자·강조·소리 각각 | 처리기 하나 + 표 한 줄 |
| 소리 프리셋 변경(현강님) | Presenter 안 switch | `CombatEventAudio`만 |
| 포효 강조 칸 규칙 | `ShowHighlight` | 포효 처리기 |
| 동시 처리 C-3(걸음 레인) | Presenter의 이동·Tick·시작 조건 | 이동 표시(S6) + Presenter 시작 조건 |

- 남는 공통 경계: 재생 세대·취소 규칙, 처리기 계약, 시작 조건. 이것이 바뀌면 재생 시험 전체를 확인한다.

## 5. 위험

| 위험 | 대응 |
|---|---|
| C-2·C-3이 같은 파일을 고친 채 미커밋(2026-10-18) | C 작업 커밋을 기다리고 다시 대조한다(S-0). 이 문서의 줄 번호는 착수 때 바꾼다 |
| 사건 안 순서(§1.4)가 처리기로 옮기며 바뀜 | 순서를 처리기별 표로 계획에 적고, 기존 재생 시험을 회귀 기준으로 한다. 순서가 다른 것을 찾으면 구현하지 않고 묻는다 |
| 세대 확인(`version != _presentationVersion`)을 빠뜨림 | 처리기 계약에 "콜백 뒤마다 세대 확인"을 넣고, 지금 시험(취소 중 재진입·늦은 콜백)을 그대로 돌린다 |
| 리플렉션·정적 호출 시험이 깨짐 | 시험이 실제 원본(사실·소리 클래스)을 읽게 같은 Phase에서 고친다. `_attacks`·`_reaction`은 Presenter에 남아 그대로다 |
| 사운드 이동이 다른 작업자 영역 | Q-S2. 옮기면 현강님 공유 메모, `GameAudioTests`·`StageMapCombatEntryBootSmokeTests`의 정적 호출 변경을 함께 적는다 |
| 회귀 기준선 없음(H-1 이후 Unity Test Runner 결과 미수신, C-1 기준선에서 `CombatProgressIntegrationTests` 5개 기존 실패) | S-0에서 Unity Test Runner 결과를 받아 기준선으로 쓴다. 받지 못하면 관리 실행 회귀만으로 진행하고 그 사실을 기록한다 |
| 처리기가 맥락 덩어리로 커짐 | 처리기마다 받는 의존성을 계획에 표로 적는다. 셋 이상 처리기가 같은 묶음을 받으면 그 묶음이 따로 원본을 가진 클래스인지 다시 본다 |

## 6. 사용자 결정 (2026-10-18 완료)

- **결정:** "C 관련 작업 마무리 후 진행할거고, 나머지는 추천대로".
  - Q-S1 = **B**: C 작업(C-4 문서·검토·커밋)이 모두 끝난 뒤 분리 전체를 한 번에 한다. 같은 날 C-3이 이미 코드 완료돼 A의 "C-3 전 분리" 구간이 없어졌다.
  - Q-S2 = **A**: `CombatEventAudio`로 위치만 옮기고 현강님께 공유한다.
  - Q-S3 = **A**: 지금처럼 Presenter 생성자 안에서 조립한다.
- 아래 선택지는 결정 근거로 남긴다.

- **Q-S1. 착수 순서:**
  - A(권장): C-2 커밋 → 분리 S-1~S-4(이동 제외) → C-3 → 분리 S-5(이동). C-3이 작아진 Presenter를 고치고, 이동은 C-3의 레인 위에서 한 번만 옮긴다.
  - B: C-2·C-3 모두 끝난 뒤 분리 전체를 한 번에. 순서는 단순하지만 C-3이 큰 파일에 레인을 더 쌓는다.
- **Q-S2. 사운드 코드:**
  - A(권장): `CombatEventAudio`로 위치만 옮긴다(값·호출 시점 그대로). 정적 프리셋 호출 시험을 고치고 현강님께 공유한다.
  - B: Presenter에 그대로 둔다(R4를 엄격히). 처리기가 Presenter의 소리 메서드를 콜백으로 받는다.
- **Q-S3. 조립 위치:**
  - A(권장): 지금처럼 Presenter 생성자 안(S8). Factory·시험 생성자 변경 없음.
  - B: Factory가 새 클래스를 만들어 주입한다. 의존 방향은 깔끔해지지만 Factory·시험 9곳이 바뀌고 Factory 공유 문서가 필요하다.

## 7. Phase 개요 (결정 뒤 계획에서 상세화)

| Phase | 내용 | 확인 |
|---|---|---|
| (선행) | 동시 처리 C-4·검토·사용자 커밋 | C 계획 §4·§6 |
| S-0 | C 커밋 뒤 이 문서를 코드와 대조·줄 번호 갱신, **Task 계획 작성**, 회귀 기준선(관리 실행 + Unity Test Runner 결과) | 기준선 기록, 계획 승인 |
| S-1 | 표시 사실 `CombatShownFacts`(S1). 걸음 `ShowCharacter`·공격 몸 조회 콜백을 새 원본에 연결 | 동작 동일. `_shownCharacters` 리플렉션 시험 갱신 |
| S-2 | 간격·머묾 대기 `CombatEventWait`(S2) | 동작 동일. 적 간격·턴 간격·머묾 시험(`CombatEnemyPresentationTests`) |
| S-3 | 사건 소리 `CombatEventAudio`(S5). 현강님 공유 메모 | 동작 동일. 소리 시험·정적 호출 갱신 |
| S-4 | 사건 처리기 표 + 재생 강조(S3·S4). `PresentEvent`·`CanPresent`·`ShowHighlight` 정리 | 동작 동일. 재생 시험 전체 |
| S-5 | 이동 표시 `CombatMovementPresentation`(S6, C-3 결과 기준) | 동작 동일. 이동·경로 끝·접촉·걸음 레인 시험 |

- 각 Phase는 동작 동일이 완료 조건이다. MSBuild 오류·새 경고 0, 관리 실행 회귀 0, 바뀐 시험 목록을 보고한다.
- Phase마다 멈추고 보고한다.
- 분리가 끝나면 [사건별 연출 조립 설계](2026-10-17-combat-presentation-composition-design.md)를 처리기 위에서 개정한다.
