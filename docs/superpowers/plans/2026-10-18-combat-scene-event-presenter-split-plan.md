# CombatSceneEventPresenter 분리 계획

- 설계: [presenter-split-design](../specs/2026-10-18-combat-scene-event-presenter-split-design.md)(S1~S8, 결정 Q-S1 B·Q-S2 A·Q-S3 A).
- 작성: 2026-10-18 S-0(사용자 지시 "S-0은 진행해").
- **동작 동일 분리다.** 모든 Phase의 완료 조건은 "화면·순서·완료 시점이 지금과 같다"이다. 동작이 달라지는 것을 찾으면 구현하지 않고 묻는다.
- 위치는 **메서드 이름**으로 적는다. 줄 번호는 C 작업이 계속 바꾸고 있어서 쓰지 않는다.
- 경로 약어: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- **사용자 결정(2026-10-18, S-1 확인 뒤):** "Unity 결과는 나중에 몰아서 줄게. S-2 진행해" → "그냥 S-5까지 진행하고 마지막에 검토 에이전트 돌려서 보완 수정해줘". §1의 "Phase마다 Unity 결과를 받은 뒤 다음 Phase"와 "Phase마다 멈추고 보고"를 이번에 한해 바꿨다. S-2~S-5를 이어서 구현하고, Phase마다 빌드·관리 실행 비교만 한 뒤, 마지막에 검토 에이전트 2개를 돌려 반영했다(§8.3). Unity 결과는 S-1~S-5를 한 번에 받는다. 실패가 나오면 어느 Phase 탓인지 가르기 어렵다는 점을 알고 정한 것이다.

## 0. 착수 조건

1. **동시 처리 C 작업 마무리:** C-4 문서·통합 검토 반영·사용자 커밋. 2026-10-18 S-0 시점에 다른 세션이 C-4 검토로 `CombatSceneEventPresenter`·`CombatConcurrentGroups` 등을 고치는 중이었다(§8.1).
2. **회귀 기준선:** §8.1의 관리 실행 기준선은 C-4 완료 코드와 같다. 커밋 때 코드가 바뀌었으면 관리 실행 전체를 다시 돌려 바꾼다. 방법은 §7.
3. **Unity Test Runner 결과(사용자):** C 커밋 때 EditMode 전체를 한 번 돌려 결과를 준다. 관리 실행은 네이티브 호출 시험(Presenter·Display 시험 대부분)을 돌리지 못하므로, 이 결과가 Unity 전용 시험의 기준선이다.
4. 착수 직전 이 계획의 메서드 목록을 코드와 한 번 더 대조한다. C 검토로 바뀐 메서드가 있으면 계획을 먼저 고친다.

## 1. 공통 규칙

- **새 클래스:** `C/` 아래, 네임스페이스 `ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat`, `internal sealed`, Allman, 한국어 XML doc. `.meta`는 32자리 hex GUID로 새로 만든다.
- **재생 세대(D10):** 원본은 Presenter의 `_presentationVersion` 하나다. 새 클래스는 `Func<int>`로 읽거나 호출마다 `version`을 받는다. 콜백 뒤의 세대 확인(`version != _presentationVersion`)은 옮긴 자리에서 그대로 한다.
- **Mapper:** 원본은 Presenter(`Bind`·`Clear`)다. 새 클래스는 `Func<FieldGridMapper>`로 읽는다. 사본을 갖지 않는다.
- **조립(Q-S3 A):** Presenter 생성자 안에서 새 클래스를 만든다. Presenter 생성자 서명·`CombatSceneDisplayFactory`·시험 생성자는 바꾸지 않는다.
- **남기는 필드 이름:** 시험이 리플렉션으로 읽는 `_attacks`·`_reaction`·`_motion`은 Presenter에 그대로 둔다.
- **Phase마다 확인:**
  - MSBuild 오류 0, 새 경고 0(기준선 경고 목록과 비교)
  - 관리 실행 회귀 0: 기준선 대비 메서드 단위로 PASS → 다른 상태가 없을 것
  - 바꾼 시험 목록과 이유(읽는 위치만 바꿨는지)
  - Unity 확인이 필요한 시험 목록
- **관리 실행의 한계(S-0에서 확인):** Presenter를 거치는 시험(`CombatSceneEventPresenterTests`·`CombatEnemyPresentationTests`·`CombatUnitDeathTests`·`CombatMovementViewTests`·공격 시험들 등)은 관리 실행에서 **전부 NATIVE**다(§8.1 표). 관리 실행은 순수 부품(간격 값·Gate·스케줄러·능력 동작·결과 재생·정리)만 지킨다.
  - 그래서 **Phase마다 Unity Test Runner 결과(그 Phase의 확인 묶음)를 받은 뒤 다음 Phase로 간다.** 몰아서 확인하면 어느 Phase가 깨뜨렸는지 가를 수 없다.
  - 새 클래스 중 순수한 것(S-2 대기, S-4 강조·처리기 표의 형식 대응)은 관리 실행되는 순수 시험을 함께 더한다. 분리한 동작을 Unity 없이도 지키기 위해서다.
- Phase마다 멈추고 보고한다.

## 2. S-1 — 표시 사실 `CombatShownFacts`

- **새 파일:** `C/CombatShownFacts.cs`
- **옮길 것(Presenter → 새 클래스):**
  - 상태: `_shownCharacters`, `_shownStructures`
  - `ApplyCharacter`, 구조물 반영(지금 `PresentEvent` 안의 `RemoveAll` + `Add`), `FindCharacter`, `TryGetShownBody`, `RenderEvents`(→ `Render`), `ShowCharacter`(반영 + 다시 그리기)
  - `Refresh`의 목록 교체 부분, `Clear`의 목록 비우기 부분
- **의존:** `FieldUnitPresenter`(`RenderFacts`·`BodyOf`), `Func<FieldGridMapper>`
- **Presenter에 남는 것:** `Refresh`·`Clear`의 진입과 가드(공격·반응·능력 진행 중이면 거절, 강조 지우기)
- **다시 연결:**
  - `CombatAttackPlayback` 생성자의 몸 조회 콜백 → `_facts.TryGetBody`
  - `CombatUnitMotionSet` 생성자의 `ShowCharacter` 콜백 → `_facts.Show`
  - `CancelMovement`의 `_units.ClearPresentationRotations(_shownCharacters, …)` → 새 클래스의 읽기 전용 목록
- **시험:** `T/CombatUnitDeathTests.cs`의 `_shownCharacters` 리플렉션을 새 클래스 경유로 바꾼다.
- **Unity 확인:** `CombatUnitDeathTests`, `CombatSceneEventPresenterTests`, `CombatBallistaAttackTests`(몸 조회), `CombatStepContactGateTests`·H-4 접촉

## 3. S-2 — 간격·머묾 대기 `CombatEventWait`

- **새 파일:** `C/CombatEventWait.cs`
- **옮길 것:** 상태 `_gapRemaining`, `_waitingEvent`, `_waitingCompleted`, `_eventShownBeforeGap`, 그리고 `ClearWaitingEvent`
- **연산:**
  - 간격 시작
  - 대기 사건 걸기(간격 전에 보였는지 포함)
  - 머묾 걸기(지금 `PresentEvent` 끝의 `HoldAfter` 분기)
  - 프레임 진행: 남은 시간을 소비하고, 끝나면 대기 사건을 꺼내 준다
  - 비우기, "대기 사건이 있는가"
- **Presenter에 남는 것:**
  - `StartGap`의 전제 확인(걸음 레인이 진행 중이면 예외)
  - `TickMotion`의 순서: 대기 진행 → 꺼낸 사건 표시(간격 전에 보였으면 완료만) → 걸음 레인 진행. 순서는 그대로다.
  - `RequireIdle`의 "대기 사건이 있으면 막음" 조건
- **간격 값**은 지금처럼 `CombatEnemyPresentationPacing`이 원본이다.
- **시험:** 바꿀 시험 없음(리플렉션 없음).
- **Unity 확인:** `CombatEnemyPresentationTests`(적 행동·턴 간격, 머묾, Ballista 재장전 턴 끝), 포효·소환 머묾 시험

## 4. S-3 — 사건 소리 `CombatEventAudio` (Q-S2 A)

- **새 파일:** `C/CombatEventAudio.cs`
- **옮길 것:** `PlayAudio`, `PlayUnit`, `PlayMovementSound`, 정적 `UnitPreset`·`AttackPreset`, 포효용 빈 콜백 `NoAudio`. **값과 호출 자리는 바꾸지 않는다**(R4).
- **의존:** `IAudioSystemService`(null 허용), 표시 사실 찾기(S-1의 `FindCharacter`)
- **다시 연결:**
  - 공격 소리 콜백(`TryBeginAttack`), 사망 소리(`TryBeginDeath`), 일반 경로 소리 → 새 클래스
  - `CombatUnitMotionSet` 생성자의 이동 소리 콜백 → 새 클래스
- **시험(정적 호출 대상 변경):**
  - `T/GameAudioTests.cs`, `T/StageMapCombatEntryBootSmokeTests.cs`, `T/CombatStandardAttackAssetTests.cs`, `T/CombatSceneEventPresenterTests.cs`: `CombatSceneEventPresenter.UnitPreset`·`AttackPreset` → `CombatEventAudio.…`
  - `T/CombatRailwayContactSceneTests.cs`: `SetTemporary(_events, "_audio", …)`를 새 클래스의 필드로 바꾼다.
- **공유:** 현강님 메모 `docs/superpowers/handoffs/2026-10-18-combat-event-audio-move-hyeongang-share.md`. 위치만 옮겼고 값·시점은 같다는 것, 바뀐 정적 호출 위치를 적는다. `GameAudioTests`·`StageMapCombatEntryBootSmokeTests`를 고친 사실도 적는다.
- **Unity 확인:** `GameAudioTests`, `CombatSceneEventPresenterTests`의 소리 시험, `CombatRailwayContactSceneTests`

## 5. S-4 — 사건 처리기 표와 재생 강조

### 5.1 처리기 계약 (S-0 대조로 구체화)

설계 S3의 "처리기 = 한 사건 형식의 표시 지식"을, 지금 `PresentEvent`의 순서(설계 §1.4)를 바꾸지 않는 모양으로 정했다. 처리기는 아래 단계에 답하고, **순서는 Presenter의 틀 하나가 정한다.**

| 단계 | 처리기가 답하는 것 | 지금 코드 |
|---|---|---|
| 행위자 | 간격 계산용 행위자 | `BeginEvent`의 행위자 해석 |
| 간격 전에 보임 | 간격을 기다리기 전에 사건을 먼저 보일지 | `BeginEvent`의 턴 끝 + Ballista 예외 |
| 강조 칸 | 재생 강조에 둘 칸 | `ShowHighlight`의 포효·소환 분기 |
| 사실 전 시작 | 사실 반영 전에 연출을 시작했는가(시작했으면 처리기가 완료를 알림) | `TryBeginAttack`·`TryBeginRoar`·`TryBeginDeath`·`TryBeginHit` |
| 사실 반영 | 표시 사실·가시·오브젝트 숨김 | `PresentEvent` 일반 경로의 종류별 `if` |
| 사실 뒤 시작 | 사실 반영 뒤에 연출을 시작했는가 | 턴 끝 재장전, 소환 등장 |

Presenter 틀(지금 `PresentEvent`의 순서 그대로):

1. 행동 사건이면 강조 지우기 → 처리기의 강조 칸 보이기
2. 사실 전 시작 → 시작했으면 끝
3. 소리(S-3) → 세대 확인 → 환경 이펙트 → 세대 확인
4. 사실 반영 → 다시 그리기 → 세대 확인
5. 사실 뒤 시작 → 시작했으면 끝
6. 머묾(S-2) 또는 완료

- 사망·피격처럼 "사실 전 시작" 안에서 소리를 내는 것도 지금 자리 그대로 처리기 안에 둔다.
- 시작하지 못한 공격(연출 설정 없음)의 소리는 3번의 일반 소리에서 난다(지금과 같음).

### 5.2 처리기 목록과 의존성

| 처리기 | 형식 | 받는 의존성 |
|---|---|---|
| 공격 | `CombatAttackEvent` | 공격 재생, Mapper, 사건 소리 |
| 포효 | `CombatRoarEvent` | 유닛 표시(크기 대여), 능력 재생·값, 공격 재생, Mapper, 재생 강조, 표시 사실(보스 칸) |
| 소환 | `CombatSummonEvent` | 표시 사실, 유닛 표시(자세 대여), 능력 재생·값, Mapper, 재생 강조 |
| 캐릭터 | `CombatCharacterEvent` | 유닛 표시(반응 설정·사망 모델 유지), 반응 집합, 반응 불가 콜백, Mapper, 표시 사실, 사건 소리 |
| 턴 | `CombatTurnEvent` | 공격 재생(재장전·Ballista 여부) |
| 사실 | 그 밖의 표시 형식 14개(구조물·영혼·효과·아티팩트·기믹·결과·원본 변경·가시 소비·장애물·보유 환경·오브젝트 발동·오브젝트 생성·토네이도·낙하) | 표시 사실, 가시 소비 콜백, 오브젝트 숨김 콜백 |

- **셋 이상 처리기가 받는 의존성:** Mapper, 표시 사실. 둘 다 원본이 따로 있는(Presenter·S-1) 좁은 읽기라 묶지 않는다(설계 §5 위험 대응).
- 반응 불가 콜백(`CancelUnavailableReaction`)과 그 상태 `_reactionUnavailable`은 시작 조건이 읽으므로 Presenter에 남기고, 콜백만 캐릭터 처리기에 넘긴다.
- **표:** `Dictionary<Type, 처리기>`. 모든 `CombatEvent` 하위 형식은 `sealed`라 형식 동일 비교가 지금의 `is` 검사와 같다. `CanPresent` = 표에 있는가.
- 기차 사망(`BeginConcurrentDeath`)도 같은 틀로 캐릭터 처리기를 거친다.

### 5.3 재생 강조 `CombatPlaybackHighlight`

- **새 파일:** `C/CombatPlaybackHighlight.cs`
- **옮길 것:** `_highlightCells`·`_highlightShown`, `ClearHighlight`, `ShowHighlight`의 "칸 보이기" 부분, `AddOccupiedCells`(칸 수집 도우미)
- **칸 계산**(포효: 영향 칸 → 포효 범위 → 보스 칸, 소환: 생성 칸)은 포효·소환 처리기로 간다.
- 강조 콜백이 없으면(`presentHighlight == null`) 아무것도 하지 않는다(지금과 같음).
- 부르는 곳: Presenter 틀, 이동 표시(S-5, 자발적 이동), 포효 끝, 소환 끝, `Refresh`, `CancelMovement`

### 5.4 새 파일·시험

- **새 파일:** 처리기 인터페이스 1개 + 구현 6개(파일 나눔은 계획 착수 때 정함: 형식별 파일 또는 한 파일), `CombatPlaybackHighlight`
- **시험:** 바꿀 시험 없음 예상(처리기는 내부, Presenter 공개 멤버 그대로).
- **Unity 확인:** 재생 시험 전체(§6 목록)

## 6. S-5 — 이동 표시 `CombatMovementPresentation`

- **새 파일:** `C/CombatMovementPresentation.cs`
- **옮길 것:** `BeginStep`·`BeginMovement`·`BeginMovementEnd`·`ShowReturn`의 본문(시작 조건 확인 뒤 부분)
  - 자발적 이동 판정, 자발적 이동의 강조 지우기, 걸음 전 간격, 레인에 걸음 넘기기
  - 경로 끝 방향 복귀 판단(보간 중이면 미룸)
  - 복귀 표시(회전 정리·사실 반영·다시 그리기)
- **Presenter에 남는 것:** 공개 진입점과 시작 조건(`RequireBound`, `RequireIdleFor`). 진입점은 확인 뒤 새 클래스에 넘긴다.
- **의존:** 걸음 레인 집합(`_motion`), 간격 값(pacing), 재생 강조, 간격 시작(Presenter의 `StartGap`, 콜백), 표시 사실, 유닛 표시(회전 조회·정리), Mapper
- **시험:** 바꿀 시험 없음 예상
- **Unity 확인:** `CombatMovementViewTests`, `CombatEnemyPresentationTests`, `CombatSceneEventPresenterTests`(넉백 동시 걸음), `CombatStepContactGateTests`, `CombatPresentationCleanupTests`

## 7. 관리 실행 방법 (세션 스크래치 도구)

- **빌드:** VS 2022 MSBuild로 `ProjectL.Tests.Framework.csproj`를 스크래치패드에 빌드한다(메모리 `projectl-build-check`). 새 파일이 Unity csproj에 아직 없으면 `extra.targets`로 더한다.
- **실행:** 스크래치의 반사 기반 실행기(`Runner.exe`, NUnit `[Test]`·`[TestCase]`·`[TestCaseSource]`·`[SetUp]`·`[TearDown]`·`[OneTimeSetUp]`)를 Unity 내장 Mono로 메서드마다 별도 프로세스로 돌린다(6개 병렬, 60초 제한).
- **판정:** `RESULT PASS` = PASS. 실패가 `MissingMethodException`·`SecurityException`·internal call이거나 결과 줄 없이 끝나면 NATIVE(Unity 전용). 그 밖의 실패는 FAIL. `[UnityTest]`와 매개변수 생성 시험은 UNSUPPORTED.
- **순번 실행:** 시험은 이름이 아니라 목록 순번(`@n`)으로 실행한다. 한국어 문자열 인자가 있는 `[TestCase]` 이름이 Windows 명령줄에서 깨져 이름으로는 찾지 못했다(S-0 첫 실행에서 24개).
- **비교:** 기준선 대비 메서드 이름 단위로 PASS → 다른 상태 = 회귀.
- 도구는 세션 스크래치에 있어 다음 세션에는 없다. 다시 만드는 방법은 이 절과 메모리 `projectl-build-check`(S-0 PC)·`offline-build-test-runner`(S-1 확인 PC)다.
- **기준선 빌드(S-1에서 씀):** worktree를 만들지 않는다. 같은 트리에서 Phase가 바꾼 파일만 `git show <기준 커밋>:경로`로 스크래치에 뽑고, 기준선용 after.targets에서 지금 파일을 `Compile Remove`·사본을 `Include`, 새 파일은 `Remove`한다. 출력 폴더는 `-p:ScratchTree=base|s1`로 나눈다. Unity csproj가 디스크와 같은지(빠진·남은 `.cs` 없음) 먼저 확인한다.
- **실행 뒤 정리:** 관리 실행은 `Assets/__TornadoLegacy_<guid>.asset`을 트리마다 하나씩 남긴다(§8.2 발견). 실행이 끝나면 `git status`로 새 untracked 파일을 확인하고 지운다. 커밋하기 전에 Unity가 `.meta`를 만들면 함께 들어간다.

## 8. 실행 기록

### 8.1 S-0 (2026-10-18)

- **대상 트리:** HEAD `fc98408f` + C-1~C-3 미커밋 변경. 다른 세션(「전투 View」)이 C-4 검토로 같은 파일들을 고치는 중이었다. 첫 빌드는 편집 도중의 `CombatConcurrentGroups.cs`를 잡아 구문 오류가 났고, 다시 빌드해 통과했다.
- **설계 대조:** 설계 §1의 책임 묶음은 C-2·C-3 반영 뒤에도 그대로다. 바뀐 점:
  - 776줄. `_motion` = `CombatUnitMotionSet`(유닛별 걸음 레인), `BeginStep(step, completed)`, `IsMovementComplete` 없어짐
  - 새 시작 조건 `MotionBlocks`·`ReactionBlocks`(주체 기준), 간격 시작 전제 `StartGap`(걸음 레인 진행 중이면 예외)
  - `TickPresentation`은 공격 → 능력 → (걸음·대기) → 모든 반응 순서
  - 이 변화는 ②(시작 조건·Tick)와 ⑧(이동)에 있고, 분리 경계는 바뀌지 않는다. 처리기 계약은 §5.1로 구체화했다.
- **빌드(임시 기준선 `probe`):** MSBuild 오류 0, 고유 경고 32개(목록은 스크래치 `probe/warnings.txt`).
- **관리 실행(임시 기준선, 전체 4,939개, 약 17분):**

| 상태 | 수 |
|---|---|
| PASS | 2,667 |
| NATIVE(Unity 전용) | 2,124 |
| FAIL | 101(메서드 52개) |
| UNSUPPORTED(`[UnityTest]` 47개) | 47 |

- **분리 대상과 닿는 시험의 상태:**
  - NATIVE(전부 Unity 전용): `CombatSceneEventPresenterTests` 46, `CombatEnemyPresentationTests` 58, `CombatUnitDeathTests` 35, `CombatUnitReactionTests` 17, `CombatUnitReactionSetTests` 7, `CombatRailwayContactSceneTests` 23, `CombatMovementViewTests` 24, `CombatBallistaAttackTests` 28, `CombatMortarPresentationTests` 20, `CombatStandardAttackTests` 25, `CombatCodeMotionHitTests` 15, `CombatViewSystemTests` 73, `CombatEnvironmentEffectPresenterTests` 14, `CombatThornPresentationTests` 18, `CombatStandardAttackAssetTests` 5, `FrenzyViewContractTests` 1
  - PASS(관리 실행이 지킴): `CombatPresentationCleanupTests` 6, `OrderedCombatSchedulingTests` 17(+NATIVE 1), `CombatConcurrentGroupsTests` 10, `CombatStepContactGateTests` 10, `CombatStepContactsTests` 10, `CombatHitGateTests` 18, `CombatHitFollowUpsTests` 8, `CombatAbilityMotionPlaybackTests` 17, `CombatEnemyPresentationPacingTests` 22, `CombatResultPlaybackTests` 14, `GameAudioTests` 17(+NATIVE 2)
- **기존 FAIL(분리 범위 밖, 모두 Core·진입·로비·상점 통합 시험):** 메시지는 대부분 `Nullable object must have a value`(50)와 `InvalidMainCharacter`(36)로, 병합된 메인 캐릭터 덱·초기 배치 변경 계열이다. C-1 기록의 `CombatProgressIntegrationTests` 실패 5개(메서드)와 같다.
  - `BossCombatIntegrationTests`: BossBattle_SummonActDayReturnAndWin
  - `ChapterContinuationServiceTests`: BossCleanup_ReentryRejected_ContinuationStillSucceeds, Composition_InitAndBindOrdersUseSameCoordinator, ConfiguredTarget_IsTheOnlyAllowedSuccessor, ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter, FinalContinuationFault_RetriesWithoutSettlementReplay, InvalidDefinitionIdentity_RejectsBeforePublication, LateLookupMutation_AddsNoContinuationWrites, MissingDefinition_DoesNotMeanFinal, RegisteredTarget_IsValidatedAndReused, RetainedBattle_BlocksContinuationUntilCallerEndBattle
  - `CombatEntryIntegrationTests`: Abandon_IsIgnored_AfterARecordedOutcome, Abandon_WhenQueuedAttackWinsFirst_PresentsVictoryAndKeepsRun, ChapterVictory_WithoutContinuationDefinition_StartsNewRunThenReturnsToStage, CompletionDisplayException_StopsWithoutEndBattleOrRetry, Defeat_EndsRun_StartsNewRunThenReturnsToLobby, DeferredCombatReturn_SharedFaultDoesNotRegisterStagePreparation, DeferredCombatReturn_ShutdownClearsPendingPreparationWithoutRegisteringJobs, DetachedCompletion_CannotAcknowledgeOrTransition, FinalChapterVictory_RunCompleted_StartsNewRunThenReturnsToStage, NextEntry_UsesNextTargetAndRetainsOldResultWithoutTreatingItAsAcceptance, NormalCombatReturn_FastLoadAfterRunnerBudget_StillDrainsExitCheckAndEntersStage, NormalExit_WaitsForActualRunnerToDrain_WithoutClearingRegisteredWork, NormalExit_WaitsForFinalResultBeforeCompletionDisplay_WithoutClearingInput, Shutdown_DuringPendingCombatReturn_DetachesManagerAndDropsExitCheck, TransitionFailure_AfterEndBattle_DoesNotRetryOrDropCompletion
  - `CombatProgressIntegrationTests`: Automatic_InvalidReadAfterRealPublication_ThrowsInTheJob, Automatic_WaitsForEnterCombatPresentation_ThenConfirmsResultInTheSameExecution, Command_AcceptanceAndRejection_BothWaitForPresentationBeforeNextCommand, Completion_EndBattleAndNextPreparation_PreserveAcknowledgedResultAndNumbers, Preview_ReplacedOrClearedBeforeExecution_UsesLatestRequestWithoutGameplayPublication
  - `CombatServiceCompositionTests`: InitAndBind_OrderDoesNotChangeBehavior, Register_ExportsSixContractsAndSingleSharedLifetime
  - `EnvironmentEntryPublicationTests`: 12개(Badge·Barrel·EndBattle·Entry×2·Flag·InitialPresence·Metadata·Mine·Publication·StaticTerrain·Tornado)
  - `LobbyDomainStopTests`: LobbyLifeCycle_MissingRecord_LogsErrorWithoutRuntimeFault
  - `ShopStageServiceTests`: DefinitionLookup_OriginReplacement_InvalidatesOldNumber
  - `TemporaryCoreChapterContinuationTests`: DefeatInSecondChapter_TerminatesWithoutClearingChapter, NewScenario_DoesNotMutateAnotherRuntime, TwoChapters_KeepOwnedFacts_ThenFinishRun
  - `TemporaryCoreRuntimeTests`: TemporaryDefinitions_RunTwoBattlesThroughRecoveryAndChapterClear
  - `TemporaryCoreShopRuntimeTests`: BattleShopNextBattle_PreservesOwnedFacts
- **기준선과 C 완료 코드:** 빌드(01:21) 뒤 01:42까지 `Assets/ProjectL/Script`·`Assets/Tests`의 `.cs` 변경이 없었다. 그 사이 C 세션이 C-4(통합 검토 반영)를 끝냈으므로, 이 기준선은 **C-4 완료 코드(커밋 전)**와 같다. 커밋 때 코드가 바뀌지 않으면 이 기준선을 그대로 쓴다. 바뀌면 같은 방법으로 다시 돌린다(§0-2).
- 결과 파일은 이 세션 스크래치(`probe/baseline.tsv`)에만 있다. 다른 세션에서 이어 하려면 위 표·FAIL 목록과 비교하거나 다시 돌린다.
- **남은 S-0 항목:** C 커밋(사용자), Unity Test Runner 결과(사용자), 착수 직전 메서드 목록 재대조.
- S-1부터의 관리 실행 기준선은 §8.2(C 커밋 뒤 병합이 들어와 이 기준선은 쓰지 않는다).

### 8.2 S-1 (2026-10-18)

- **코드:** 다른 PC 세션이 구현했고 사용자가 `ec34d932`로 커밋했다(부모 `9d3cc811`). 그 세션의 이 절 기록과 CLAUDE.md 갱신은 커밋에 들어오지 않아, 확인을 이어 받은 세션이 다시 썼다.
  - 새 `C/CombatShownFacts.cs`(+ `.meta`, GUID `775c353e…`): 상태 `_characters`·`_structures`, `ApplyCharacter`·`ApplyStructure`·`ShowCharacter`·`Replace`·`Clear`·`FindCharacter`·`Render`·`TryGetBody`, 읽기 전용 `Characters`. Mapper는 `Func<FieldGridMapper>`로 읽는다(§1).
  - Presenter에서 없앤 것: 두 목록, `ApplyCharacter`, `ShowCharacter`, `TryGetShownBody`, `RenderEvents`, 구조물 반영의 `RemoveAll` + `Add`. `FindCharacter`는 소리(S-3 전까지)를 위한 한 줄 전달로 남았다.
  - `Refresh`: 가드·강조 지우기 뒤 `_facts.Replace` → `_facts.Render`. `Clear`: `_mapper = null` 뒤 `_facts.Clear()`. 순서는 그대로다.
  - 다시 연결: 공격 재생 몸 조회 `_facts.TryGetBody`, 걸음 레인 `_facts.ShowCharacter`, `CancelMovement` 회전 정리 `_facts.Characters`.
  - §2와 다른 이름: 걸음 레인 콜백이 `_facts.Show`가 아니라 `_facts.ShowCharacter`다. 동작과 무관하다.
- **대조(확인 세션):** diff로 순서·조건이 그대로인지 봤다. 생성자의 `units` null 확인이 `_facts` 생성보다 먼저라 예외가 같다. 지운 멤버를 코드·리플렉션으로 읽는 곳은 없다(검색).
- **바꾼 시험:** `T/CombatUnitDeathTests.cs` 1줄. `_shownCharacters` 리플렉션을 `_facts` → `_characters` 경유로 바꿨다(읽는 위치만).
- **빌드:** 기준선(`9d3cc811` = 지금 트리에서 S-1 세 파일을 되돌린 것, §7)과 S-1 모두 MSBuild 오류 0이고 고유 경고 16개가 같다(새 경고 0). 기준선 Systems DLL에 `CombatShownFacts`가 없는 것을 확인했다.
- **관리 실행(전체 5,039개, 두 트리 동시 6병렬씩 약 11분):**

| 상태 | 기준선 `9d3cc811` | S-1 `ec34d932` |
|---|---|---|
| PASS | 2,693 | 2,693 |
| NATIVE(Unity 전용) | 2,161 | 2,161 |
| FAIL | 138(메서드 78개) | 138(메서드 78개) |
| UNSUPPORTED(`[UnityTest]` 46 + `[Values]` 조합 1) | 47 | 47 |

  - **메서드 단위 비교 차이 0**(상태와 실패 메시지까지 같다). 회귀 0.
  - S-0(4,939)보다 100개 많은 것은 C 커밋 뒤 병합으로 들어온 시험이다.
  - 분리 대상과 닿는 시험 상태는 §8.1 표와 같다. NATIVE: `CombatUnitDeathTests` 35, `CombatSceneEventPresenterTests` 46, `CombatBallistaAttackTests` 28, `CombatEnemyPresentationTests` 58, `CombatMovementViewTests` 24, `CombatRailwayContactSceneTests` 23. PASS: `CombatStepContactGateTests` 10, `CombatStepContactsTests` 10, `CombatPresentationCleanupTests` 6, `OrderedCombatSchedulingTests` 17(+NATIVE 1), `CombatConcurrentGroupsTests` 10, `CombatHitGateTests` 18, `CombatAbilityMotionPlaybackTests` 17, `CombatEnemyPresentationPacingTests` 22, `CombatResultPlaybackTests` 14, `GameAudioTests` 17(+NATIVE 2).
  - **FAIL(기준선에서도 같음, 분리 범위 밖):** §8.1의 52개 메서드(11개 클래스)는 그대로다. 그 밖에 9개 클래스 26개 메서드가 더 있다: `ArtifactBoardConfigTests` 3, `ArtifactPopupConfigTests` 1, `AudioSystemTests` 1, `BattleDefinitionAdapterTests` 1, `CoreTableQueryTests` 1, `FrameworkRuntimeCompositionTests` 1, `KeyBindingTests` 13(NullReference), `LobbyInputTests` 1, `ViewDataTableTests` 4. 병합으로 들어온 변경이거나, 실행기를 다시 만들어서 생긴 차이(S-0 실행기와 같은 코드가 아님)로 보인다. 원인은 보지 않았다. Unity 전체 결과로 가린다.
- **도구·결과:** 확인 세션 스크래치(`runner/Runner.cs`, `rt.sh`, `run-all.sh`, `collect.sh`, `results-base.tsv`, `results-s1.tsv`). 다른 PC에서 돌던 기준선 결과는 쓰지 않았다.
- **발견(보고만, 지우지 않음):** `Assets/__TornadoLegacy_*.asset`(+ `.meta`) 두 벌이 커밋돼 있다. `5228…`은 `92c2ddd7`(동시 처리 커밋), `f854…`은 `ec34d932`(S-1 커밋)에 들어갔다. `EnvironmentDefinitionAuthoringTests.TornadoSO_LegacySerializedAssetWithoutRepeatLoadsThreeTurnDefault`의 임시 에셋이다. 이 시험은 관리 실행(Mono)에서 파일을 쓴 뒤 `AssetDatabase.ImportAsset`에서 NATIVE로 멈추고, `finally`의 `DeleteAsset`도 네이티브라 파일이 남는다. 이번 확인 실행도 두 벌(기준선·S-1)을 남겨 바로 지웠다. 커밋된 두 벌도 앞선 관리 실행이 남긴 것으로 보인다. 지울지는 사용자가 정한다.
- **Unity 확인(사용자, S-1 묶음):** `CombatUnitDeathTests`, `CombatSceneEventPresenterTests`, `CombatBallistaAttackTests`(몸 조회), `CombatStepContactGateTests`·H-4 접촉. S-1이 바꾼 경로는 관리 실행에서 전부 NATIVE라 S-1의 실제 확인은 이 결과로만 한다.
- **남은 것:** Unity Test Runner 결과(사용자). S-2는 사용자 지시로 이 결과 전에 착수했다(머리말 사용자 결정, §8.3). 결과는 S-1~S-5 묶음으로 §8.3에 기록한다.

### 8.3 S-2~S-5와 마무리 검토 (2026-10-18)

- **진행:** 머리말의 사용자 결정대로 S-2~S-5를 이어서 구현했다. Phase마다 MSBuild와 관리 실행 비교만 하고 멈추지 않았다. 마지막에 검토 에이전트 2개(동작 동일성 / 설계 원칙·계획 대조)를 돌려 반영했다. 커밋은 하지 않았다.
- **결과 크기:** `CombatSceneEventPresenter` 727줄(S-1 커밋) → 420줄. 새 파일 12개(+ `.meta`), 새 순수 시험 3개(+ `.meta`), 현강님 공유 메모 1개.

#### S-2 대기 `CombatEventWait`

- 새 `C/CombatEventWait.cs`(GUID `3890cd61…`): 남은 간격, 대기 사건·완료·"간격 전에 보임". 연산은 `StartGap`, `HoldAfterGap`(간격 뒤 사건), `HoldShown`(머묾), `Consume(ref seconds)`(간격에 시간을 쓰고 남은 시간을 돌려줌), `TryTake`(대기 사건 꺼내기), `Clear`, `HasEvent`.
- §3과 다른 점: "프레임 진행"을 `Consume`·`TryTake` 두 메서드로 나눴다. 순서는 Presenter `TickMotion`에 그대로 있다(간격 → 꺼낸 사건 표시 또는 완료 → 남은 시간은 레인에 주지 않음).
- Presenter에 남은 것: `StartGap`의 전제 확인(레인 진행 중이면 예외), `TickMotion` 순서, `RequireIdle`의 `_wait.HasEvent`. 머묾은 지금처럼 전제 확인을 거치지 않는다.
- 새 시험 `T/CombatEventWaitTests.cs`(9개, GUID `56efcb74…`).

#### S-3 소리 `CombatEventAudio`

- 새 `C/CombatEventAudio.cs`(GUID `1fbeaf1c…`): `Play`(옛 `PlayAudio`), `PlayMovement`(옛 `PlayMovementSound`), `PlayUnit`, 정적 `UnitPreset`·`AttackPreset`·`NoAudio`. 값과 호출 시점은 그대로다. Presenter 필드는 `_sound`다.
- 의존: `IAudioSystemService`(null 허용), 유닛 찾기 `Func<EntityId, CharacterCombatFacts>`(= `_facts.FindCharacter`, 검토 반영으로 좁힘).
- §4에 없던 운영 호출자 2곳도 정적 호출 대상만 바꿨다: `C/AttackTargetingInteraction.cs`(`Cancel`의 취소 소리), `C/PlacementDragInteraction.cs`(`BeginDrag`). 시험은 §4대로 바꿨다(`GameAudioTests` 5곳, `StageMapCombatEntryBootSmokeTests` 5곳, `CombatStandardAttackAssetTests` 1곳, `CombatSceneEventPresenterTests` 2곳, `CombatRailwayContactSceneTests`의 소리 주입을 `_sound`의 `_audio`로).
- 현강님 메모: `docs/superpowers/handoffs/2026-10-18-combat-event-audio-move-hyeongang-share.md`(미전달). 두 인터랙션 파일은 현강님이 최근 고친 파일이라 메모 §3에 적었다.

#### S-4 처리기 표·재생 강조

- 새 파일(모두 `C/`):
  - `CombatEventHandler.cs`(`52156a83…`): 처리기 기반. §5.4의 "인터페이스" 대신 단계별 기본 구현("할 일 없음")이 있는 추상 클래스로 만들었다. 표에 없는 사건용 `None`이 있다(옛 일반 경로와 같음).
  - `CombatEventHandlerTable.cs`(`f1554732…`): `Create(attack, roar, summon, character, turn, facts)`, 사실 형식 14개 목록, `Contains`(= `CanPresent`), `For`. 등록할 때 sealed 사건 형식만 받는다.
  - 처리기 6개: `CombatAttackEventHandler.cs`(`75d4b723…`), `CombatRoarEventHandler.cs`(`5f49a148…`), `CombatSummonEventHandler.cs`(`d7381641…`), `CombatCharacterEventHandler.cs`(`68f4b0f5…`), `CombatTurnEventHandler.cs`(`7ce3aee5…`), `CombatFactEventHandler.cs`(`87bc23d0…`). 파일은 형식별로 나눴다(§5.4 미정 항목).
  - `CombatPlaybackHighlight.cs`(`cc580d16…`): `Show(e, handler)`, `Clear`, `AddOccupiedCells`.
- `PresentEvent`는 §5.1의 틀 하나다: 강조 → 사실 전 시작 → 소리 → 환경 효과 → 사실 반영 → 다시 그리기 → 사실 뒤 시작 → 머묾 또는 완료. 세대 확인 위치는 그대로다.
- §5.2와 다른 점:
  - 캐릭터 처리기는 사망·피격 콜백 뒤 세대를 다시 읽으려고 `Func<int>` 세대를 더 받는다.
  - 공격 재생(`CombatAttackPlayback`)도 공격·포효·턴 처리기 셋이 받는다. 원본은 Presenter가 소유하고, 처리기는 참조로만 쓴다.
  - 정리 실패 문구는 Presenter의 `CleanupFailureMessage` 하나를 함께 쓴다(검토 반영).
- "간격 전에 보임": 기본은 false이고, 턴 끝 규칙은 턴 처리기 한 곳에만 둔다(검토 반영). `TurnEnded`는 `CombatTurnEvent` 생성자만 만들 수 있어서(다른 형식은 Kind가 고정이거나 검증됨) 동작이 같다.
- 강조가 처리기 기반 형식을 아는 것(`Show(e, handler)`)은 할당이 없어서 그대로 뒀다. 검토 선택 항목이다.
- 새 시험: `T/CombatPlaybackHighlightTests.cs`(6개, `37b6a8a7…`), `T/CombatEventHandlerTableTests.cs`(6개, `1dc4bb25…`). 표 시험은 Contracts 어셈블리의 모든 `CombatEvent` 하위 형식을 돌며 표에 있는 형식이 옛 `CanPresent` 19개와 정확히 같은지 본다.

#### S-5 이동 표시 `CombatMovementPresentation`

- 새 `C/CombatMovementPresentation.cs`(`f8d61b2a…`): `BeginStep`, `BeginMovement`, `EndMovement(lastStep, interpolating, completed)`(옛 `BeginMovementEnd` 본문), `ShowReturn`.
- Presenter에 남은 것: 공개 진입점, 시작 조건(`RequireBound`, `RequireIdleFor`), `BeginMovementEnd`의 보간 중 판정(`_motion.IsInterpolating`, 그에 따른 조건부 `RequireIdleFor`). 간격 시작은 Presenter `StartGap`을 콜백으로 넘긴다.

#### 검증

- **빌드:** Phase마다 MSBuild 오류 0, 고유 경고 16개가 S-1과 같다(새 경고 0).
- **관리 실행:** 기준은 §8.2의 S-1 결과다.

| 트리 | 전체 | PASS | NATIVE | FAIL | UNSUPPORTED | S-1 대비 |
|---|---|---|---|---|---|---|
| S-1 `ec34d932` | 5,039 | 2,693 | 2,161 | 138 | 47 | — |
| S-2 | 5,048 | 2,702 | 2,161 | 138 | 47 | 차이 0 + 새 PASS 9 |
| S-3 | 5,048 | 2,702 | 2,161 | 138 | 47 | S-2와 같음 |
| S-4 | 5,060 | 2,714 | 2,161 | 138 | 47 | 차이 0 + 새 PASS 12 |
| 최종(S-5 + 검토 반영) | 5,060 | 2,714 | 2,161 | 138 | 47 | 차이 0 + 새 PASS 21(S-4 대비 이름 바꾼 시험 1개뿐) |

- S-3·S-4 실행은 두 트리를 동시에 6병렬씩, S-2와 최종은 12병렬로 돌렸다.
- **검토 에이전트 2개:**
  - 동작 동일성: 동작 차이 없음. 사소한 차이 5건 중 `ShowReturn`의 Mapper 읽는 순서는 원본과 같게 고쳤다. 나머지는 영향이 없다: 반응 불가 콜백 위임을 한 번만 만듦, "간격 전에 보임" 평가와 대기 등록 순서, sealed 검사라는 새 예외 경로(지금 형식으로는 생기지 않음), 사전 조회.
  - 설계·계획 대조: 동작 같음. 고칠 것 4건 중 3건을 반영했다(기본 "간격 전에 보임"의 턴 지식 → 턴 처리기 한 곳, 틀린 "읽기만" 주석, 이 기록). 남은 1건은 `FMODStudioCache.asset` 변경으로, 에디터가 바꾼 것이라 이번 범위가 아니다(커밋 때 사용자 판단). 선택 항목 중 반영한 것: 정리 문구 공유, 표 주석 정리(D1 중복 제거), `Create` 이름 붙은 인자, 소리의 좁은 의존, 강조 시험의 차지 칸 두 칸. 반영하지 않은 것: 강조↔처리기 형식 결합, 프리셋 전용 클래스 분리(후보).
- **Unity 확인(사용자, S-1~S-5 묶음):** EditMode 전체를 돌린다. 특히 볼 묶음은 아래다. 관리 실행에서는 모두 NATIVE라 이 결과로만 확인한다.
  - S-1: `CombatUnitDeathTests`, `CombatSceneEventPresenterTests`, `CombatBallistaAttackTests`, `CombatStepContactGateTests`
  - S-2: `CombatEnemyPresentationTests`(적 행동·턴 간격, 머묾, Ballista 재장전 턴 끝), 포효·소환 머묾
  - S-3: `GameAudioTests`, `CombatSceneEventPresenterTests`의 소리 시험, `CombatRailwayContactSceneTests`, `StageMapCombatEntryBootSmokeTests`
  - S-4: 재생 시험 전체 — `CombatMortarPresentationTests`, `CombatStandardAttackTests`, `CombatCodeMotionHitTests`, `CombatThornPresentationTests`, `CombatEnvironmentEffectPresenterTests`, `CombatUnitReactionTests`, `CombatUnitReactionSetTests`, `CombatViewSystemTests`, `FrenzyViewContractTests`
  - S-5: `CombatMovementViewTests`, `CombatPresentationCleanupTests`
  - Unity 기준선이 없다(§0-3 결과를 받지 못함). 실패가 나오면 `ec34d932`(S-1) 또는 `9d3cc811`(분리 전)에서 같은 시험을 돌려 원래 실패인지 가른다.
- **재생 확인(사용자):** 일반 전투 한 판(적 이동·공격·넉백·사망), 보스 스테이지(포효·소환 강조와 등장), Ballista 재장전 턴 끝, 철도. 계획 §0의 C 확인 목록(동시 처리 계획 §7.4)과 함께 보면 된다.
- **커밋 때 함께 넣을 새 파일(.meta 포함):** `C/`의 `CombatEventWait`, `CombatEventAudio`, `CombatEventHandler`, `CombatEventHandlerTable`, `CombatPlaybackHighlight`, `CombatAttackEventHandler`, `CombatRoarEventHandler`, `CombatSummonEventHandler`, `CombatCharacterEventHandler`, `CombatTurnEventHandler`, `CombatFactEventHandler`, `CombatMovementPresentation`, `T/`의 `CombatEventWaitTests`, `CombatPlaybackHighlightTests`, `CombatEventHandlerTableTests`, 그리고 현강님 메모.
- **Unity 확인 결과(2026-10-18, 사용자):** "S 작업 문제 없는거 확인했고". S-1~S-5 묶음 문제 없음. 시험별 수치는 받지 않았다.
- **다음:** 분리는 여기서 끝난다. 이어지는 작업은 [연출 바탕·개선 계획](2026-10-18-combat-presentation-foundation-plan.md) A-1부터다. 새 동시 경우 추가 안내(viewDocs)는 분리 뒤에 쓰기로 했다(동시 처리 계획 §7.4).
