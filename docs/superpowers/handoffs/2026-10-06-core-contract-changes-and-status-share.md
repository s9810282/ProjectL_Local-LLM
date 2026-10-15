# Core 계약 변경·해결 현황 공유 (View·골격 담당께)

작성 2026-10-06 · Core(천명) · 브랜치 `feature/Cheon/refactor/core-system` · 상태: **미전달**

상세 기록: `plans/2026-10-06-stage-gold-run-currency-split-plan.md`, `plans/2026-10-06-new-run-start-contract-plan.md`(§5 새 Run, §6 포기), `plans/2026-10-06-core-buffer-responsibility-cleanup-plan.md`(T1/G7·그래프 생성기)

## 1 해결된 문제

| 문제 | 해결 |
|---|---|
| **운영 전투 준비 T1/G7** | `DatabaseBattleInitializationSource`가 더 이상 예외를 던지지 않는다. DB 공격 지정 방식으로 공격 프로필을 붙여 전투 정의를 돌려준다. 실제 SO DB의 모든 전투 스테이지로 정의가 만들어지는 것을 테스트로 확인했다. |
| **스테이지 골드 / Run 재화 분리** | 드로우용 스테이지 골드는 전투 상태(`CombatStateData.StageGold`)에 있다. 전투 준비 때 스테이지 경제의 `InitialGold`로 시작하고 전투와 함께 끝난다. `CurrencyBuffer`는 상점·회복용 Run 재화만 맡는다. 두 재화는 서로 영향이 없다. |
| **새 Run 시작 계약** | `CoreGroup.RunStart.StartNewRun(loadoutKey)` 한 번으로 이전 Run 정리, 시작 로스터, 시작 Run 재화가 한 번에 설치된다. 실패하면 아무것도 바뀌지 않고, 다시 호출해도 중복 지급되지 않는다. |
| **자발적 포기 공개 API** | `AbandonBattle()`은 이제 인자가 없다. Core가 정산 입력(Artifact 배치 효과)을 직접 조립하고, 결과는 enum으로 구분한다. |

## 2 계약 변경 (View·골격 코드에 영향)

| 변경 | 이전 | 이후 | 영향 |
|---|---|---|---|
| 포기 API | `bool AbandonBattle(ArtifactLoadoutSnapshot)` | `BattleAbandonResult AbandonBattle()` | 호출부 수정 필요. 아래 §3 참고 |
| 새 Run 시작 | (없음) | `IInGameCoreSystemGroupService.RunStart` → `RosterAcquireResult StartNewRun(int loadoutKey)` | Core 그룹 인터페이스에 속성 추가. 대역(fake) 구현이 있으면 속성 추가 필요 |
| 진입 거절 사유 | 로스터 없이 진입하면 예외 | `ChapterProgressionFailureReason.RosterNotInitialized` 거절 | `EnterBattle` 결과로 받는다. 기존 값의 숫자는 바뀌지 않았다 |
| `StagePlan` `NewRun` | 초기화(리셋)만 하고 챕터 준비 | `NewRunJob`(StartNewRun, 로드아웃 `StagePlan.DefaultLoadoutKey = 1`) → `StageJob` | `ResetRunProgressJob`은 삭제됨 |
| `StageLifeCycle` 진입 등록 | 항상 `JobType.Start` | 로스터가 없으면 `JobType.NewRun`, 있으면 `Start` | `BindServices`에 `ICharacterBufferReadSink` 인자 추가 |
| 전투 HUD 골드 | `CurrencyReadSink.Amount` | `CombatStateReadSink.Current.StageGold` | `CombatViewSystem.BindServices`에서 `ICurrencyBufferReadSink` 인자 제거(이미 수정됨) |
| 회복 소비 Buffer 경로 | `BattleWriteSink.PublishRecovery` | `RecoveryStageWriteSink.PublishRecovery` | Core 내부 호출만 해당. View 영향 없음 |
| `PlayerLoadoutData` | `StartingHandSize`·`RandomSeed`가 실제로는 쓰이지 않음(첫 드로우는 3장 고정) | `StartingRunCurrency` 추가(임시). `StartingHandSize`를 실제로 연결: 새 Run 때 Run에 기록되고, 매 스테이지 **첫 손패만** 메인 1 + 서브(값 − 1)장. 에셋 값 4 = 메인 1 + 서브 3(현재와 같음). `RandomSeed` 삭제(셔플은 스테이지 `TimelineSeed`) | 생성자 인자 변경. `IRunProgressBufferReadSink.StartingHandSize`(`int?`) 추가 |
| `CombatAttackProfile` 생성 | 다칸 유닛만 거절 | 관통 특성 + 맞지 않는 지정 방식도 생성 때 거절(`SupportsPierce`) | 잘못된 조합이 공격 순간이 아니라 전투 준비 때 드러남 |
| 공격하지 않는 유닛(지정 방식 키 0) | 프로필 없이 전투에 들어간 뒤 배치 때 실패 | 전투 준비 때 명시적으로 거절 | Core가 유닛 정의를 프로필로 조회하는 구조라 지원은 후속 과제 |

`IBufferQuery`의 속성과 등록 서비스는 바뀌지 않았다. `BufferSystemGroup`은 내부만 거래 클래스로 나눴다.

## 3 사용 계약

**새 Run 시작 순서:** `StartNewRun(loadoutKey)` → `EnterChapter(시작 챕터)`.

- `StartNewRun`은 맵 선택까지 비우므로 챕터 진입은 반드시 그 뒤에 한다. `StagePlan` `NewRun`이 이 순서로 되어 있다.
- 끝난 Run(승리·패배)에서도 호출할 수 있다.
- 거절(`InvalidDefinition` 등)이면 아무것도 바뀌지 않는다.

**포기 결과의 의미:**

| 결과 | 의미 | 호출자가 할 일 |
|---|---|---|
| `Abandoned` | 이번 호출로 패배(자발적 포기) 완료가 기록됨 | 완료 표시 → `EndBattle` → 전환 |
| `AlreadyCompleted` | 이미 완료 기록이 있음(승리·패배·포기). `EndBattle` 뒤에도 기록은 결과 표시용으로 남아 이 값이 나온다 | 새 포기로 해석하지 않고 기존 완료를 표시 |
| `RunEnded` | Run이 이미 끝남 | 종료 흐름으로 |
| `NoActiveBattle` | 활성 전투 없음 | 포기 불필요 |
| (예외) | DB 정의 오류(정의 없는 Artifact 배치) | 오류 경계로 보고. 아무것도 바뀌지 않음 |

포기는 완료 기록만 남기고 **일반 전투 결과(`CombatResult`)를 새로 게시하지 않는다.** 순서는 다음과 같다: 입력 중단·진행 작업 정리 → 남은 결과 표시 → `AbandonBattle()` → 완료 표시 → `EndBattle` → 전환.

## 4 남은 작업

### View·골격

1. **새 Run 시작 등록: 해결됨(이번 변경, §6).** `StageLifeCycle`이 로스터가 없는 첫 진입에서 `NewRun`을 등록한다. 그래서 타이틀 화면 없이 Boot → StageSelect만으로 새 Run이 시작된다.
   - 타이틀의 "새 게임"이 생기면 그 버튼에서 `NewRun`을 등록하면 된다. 그 뒤 StageSelect에 들어올 때는 로스터가 있으므로 `Start`만 등록되어 중복 시작이 없다.
   - 끝난 Run(패배·최종 승리)은 로스터가 남아 있어 자동으로 새로 시작하지 않는다. 그 목적지는 기존 보류 결정이다.
2. **StageSelect 노드 클릭 → 진입.** 다른 브랜치의 `StageMapEntryFlow`를 인수하고 통합 검증해야 한다. 전투는 `EnterBattle`, 상점은 `Shop.EnterShop`, 회복은 `Recovery.EnterRecovery`로 진입한다.
3. **포기를 F2에 연결.** §3 순서대로 연결한다.
4. **보스 승리 뒤 챕터 계속(`ContinueChapter`)의 호출자.** 아직 View·LifeCycle에 없다.
5. **패배·최종 승리 목적지.** 사용자·기획 결정 대기(기존 보류 유지).

### Core (남은 것, 진행 가능)

- **챕터 계속 정의 원본.** 운영 조립은 `UnavailableChapterContinuationSource`를 쓴다. 그래서 보스 승리 뒤 다음 챕터 이동이나 최종 승리(Run 종료 기록)가 거절된다. 실제 DB는 챕터가 1개뿐이라 운영에서는 "보스 클리어 = 최종 승리"가 기록되지 않는다. 다음 챕터 키(0 = 마지막)를 임시 SO 필드로 두고 DB 원본을 연결하는 작업이 필요하다. Excel DT의 `Chapter_NextChapter`로 옮길 수 있는 형태로 만든다.
- 같은 구조 정리 후보: `ResolveBattle(loadout)`, `FinalizeTurn(..., loadout)`도 호출자가 로드아웃을 넘긴다.

### 데이터·기획

- **Run 시작 재화.** `PlayerLoadout_1.startingRunCurrency = 0`(임시)이다. 실제 DB의 상점 가격과 회복 비용이 2~10이라 운영 경로에서는 상점 구매·회복이 결제되지 않는다. 기획 값이 정해지면 에셋 값만 바꾸면 된다.
- **Excel DT 전환.** 위 임시 필드(`startingRunCurrency`, 이후 챕터 연결)는 Excel 시트로 옮길 대상이다.

## 5 다른 담당 영역을 변경한 목록 (확인 부탁)

Core 작업 중 다른 담당 영역 파일을 사용자 결정에 따라 직접 바꿨다. 각 담당은 자기 브랜치와 충돌이 없는지, 방향에 이견이 없는지 봐 주면 된다.

| 변경 | 원래 담당 | 결정 | 확인할 것 |
|---|---|---|---|
| `StagePlan` `NewRun` → `NewRunJob`, `ResetRunProgressJob` 삭제 | 골격(합의 K2: Flow·Plan·Job은 View) | 사용자 결정 D2 | 같은 파일 수정 여부. 삭제한 Job을 쓰는 곳이 없는지 |
| `StageLifeCycle` 진입 등록 분기(로스터 없으면 `NewRun`), `BindServices` 인자 추가 | 골격 | 사용자 요청 | StageMapFlow(6번) 작업과의 충돌. `RegisterEntryJobs` 수정 여부 |
| `CombatViewSystem` HUD 골드 → `StageGold`, `BindServices`에서 재화 Sink 제거 | View(용근님) | 사용자 결정 D1 | HUD가 `CurrencyReadSink`를 다시 읽지 않도록 병합 |
| View 테스트(`CombatViewSystemTests`, `CombatDisplayReuseTests`, `CombatMovementViewTests`, `CombatPlanTests` 대역, `ArtifactBoardConfigTests`) | View·골격 | 위 변경으로 컴파일을 맞춤 | 각 브랜치 테스트와 병합 |
| `PlayerLoadoutData`/SO `startingRunCurrency` 추가, `startingHandSize` 실제 연결(기본 4), 쓰이지 않던 `randomSeed` 삭제, `PlayerLoadout_1` 정리 | DB-SO 구조(용근님) → Excel DT(민형님) | 2번 설계 승인, 외부 리뷰 반영 | Excel DT로 옮길 항목에 포함(시작 Run 재화, 첫 손패 수) |
| `BufferSystemGroup` 내부 분할 | Framework 공유 Group(민형님) | 사용자 결정 D3 | 별도 공유 문서 `2026-10-06-buffer-system-group-split-framework-share.md` |
| `CombatViewCommand.RequireAim` 오류 문구 1줄 | View 입력 Buffer | B11 계획 | 없음(문구만) |

## 6 StageLifeCycle 진입 분기 (이번 변경)

`RegisterEntryJobs`는 `ICharacterBufferReadSink.Owned`가 비어 있으면 `JobType.NewRun`(새 Run 시작 → 챕터 진입), 아니면 `JobType.Start`(챕터 진입)를 등록한다. 아티팩트 보드 초기화 Job 등록은 이전과 같다.

| 상황 | 로스터 | 등록 |
|---|---|---|
| 첫 Boot → StageSelect(타이틀 없음) | 없음 | `NewRun` |
| 타이틀 "새 게임"이 `NewRun`을 등록한 뒤 StageSelect | 있음 | `Start` |
| 전투 승리 후 StageSelect 복귀·이어하기 | 있음 | `Start` |
| Run 종료 후 StageSelect | 있음 | `Start`(자동 새 Run 없음, 목적지 결정 대기) |

## 7 검증

- 전체 EditMode(최종, 첫 손패 연결 포함): **2918개 중 2889 통과, 28 실패, 판정 불가 1** (`.utmp/HandSize-review/full.xml`). 판정 불가 1개는 View 테스트 `StageMapViewSystemTests.EnterNode_WithoutMainCamera_Throws`의 전제(테스트 씬에 MainCamera 없음)가 배치 실행 환경에서 맞지 않은 것이다. 실패 28개는 작업 전부터 있던 실패(E10 기준 16개, 병합으로 들어온 UI·렌더·오디오·부팅 12개)와 이름까지 같다. 이번 변경으로 새로 깨진 테스트는 없다.
- 병합으로 들어온 실패 12개는 Core 변경을 되돌려도 똑같이 실패하는 것을 확인했다. 해당 담당자 확인이 필요하다: AreaOutline, ArtifactPopupBootSmoke ×2, AudioSystem, CombatEntryIntegration NextEntry, CombatLogUI ×2, DeckButtonUI ×3, GlobalUI, UnitHUD.
