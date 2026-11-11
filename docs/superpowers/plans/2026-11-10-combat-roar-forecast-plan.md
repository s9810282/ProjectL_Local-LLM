# 전투 포효 예고 Task 계획

작성: 2026-11-10 · 설계: [roar-forecast-design](../specs/2026-11-10-combat-roar-forecast-design.md)
결정: 2026-11-10 사용자 "확인하면서 권장안대로 진행해" = Q-RF1~Q-RF6 모두 A(Q-RF1 A = 이 세션이 Core 조회를 구현, Core 전달 문서 §5에 기록).

진행 규칙: Phase마다 멈추고 보고한다. 새 시험은 쓰지 않는다(View 작업 지시 2026-10-27, Core 조회도 이 기능의 일부로 같이 따름 — TP-9). MSBuild는 돌리지 않는다(전투 View 지시 2026-10-20). 보고에는 "컴파일 미확인"을 적는다. 기존 시험은 컴파일이 깨지는 곳만 맞춘다.

## 0. 착수 전 확인(2026-11-10, 코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| C1 | 실제 포효는 `TryRoar`가 카운터를 0으로 되돌린 투영 위에서 반응 공격(`CombatAttackCalculation`, 공개 미리보기 꺼짐)을 돌린다. | `BossRoarCalculator.TryRoar` |
| C2 | 반응 공격은 공격 미리보기 값을 만들지 않는다(`_preview = reaction ? default : …`). 공격 결과(`AttackResult`)와 대상 미리보기 목록은 만든다. | `AttackResultFactory.cs:197` |
| C3 | 공개 미리보기의 환경 투영 관찰자는 공격자 진영으로 고정돼 있다(`VisibleSteps(originalActor.Owner)`, 토네이도 관찰자 `plan.Actor.Owner`). 적 AI가 적 시점 미리보기로 이 길을 쓴다. 보스를 공격자로 그대로 돌리면 적 시점이라, 플레이어가 모르는 지형이 드러날 수 있다(호버 조회 Q-UR2 A 원칙과 어긋남). | `CombatAttackCalculator.Run:83`, `CommandAttackProjection.cs:213-214`, `EnemyAiPolicy.cs:126` |
| C4 | 입력 전체를 플레이어 시점으로 투영해서 돌리는 길은 안 된다. 투영 환경으로 벽 형상을 만들면 예외(§5.52의 끌어오기 미리보기 결함과 같은 원인). | `PullAttackCalculator.RebuildDeliverySteps` 주석 |
| C5 | `CommandAttackPlan` 생성자를 부르는 기존 시험이 여섯 파일 있다. 끝 선택 인자를 더하면 그대로 컴파일된다. | `Assets/Tests/EditMode/*` |
| C6 | 미리보기 Profiler 표시는 종류 열거에서 자동으로 만든다. Buffer 응답 게시는 종류별 검사가 행동 조회뿐이다. | `CoreProfilerMarkers.PreviewOf`, `BattleTransactions.PublishPreview` |

## 1. 계획에서 정한 것(사용자 확인 대상)

- **TP-1** 예고는 실제 포효와 같은 순서(카운터 되돌림 → 반응 공격)를 탄다. 되돌림 함수와 "지금 포효할 수 있나"(유닛 조건·국면 조건)는 `BossRoarCalculator` 안 한 곳이고, 대기 판정(`PendingBosses`)과 예고가 같이 부른다.
- **TP-2** 공개 미리보기 관찰자 인자: `CombatAttackCalculation`·`Run`·`CommandAttackPlan`에 끝 선택 인자 `previewObserver`를 더한다. 비우면 지금처럼 공격자 진영이다. 예고만 `Owner.Player`를 넘긴다. 토네이도 관찰자도 이 값을 쓴다(같은 줄이 두 번 있던 `CommandAttackProjection:213-214`는 한 줄로).
- **TP-3** 반응 공격도 공개 미리보기일 때는 공격 미리보기 값을 만든다(`reaction && !plan.PublicPreview`일 때만 비움). 실제 포효는 공개 미리보기가 꺼져 있어 그대로다.
- **TP-4** Core 예고는 카운터를 보지 않는다(언제 보일지는 View 규칙 RF1). 포효가 없는 유닛을 물으면 계약 위반이라 예외다.
- **TP-5** 실패 이유: 전투 끝 `StageEnded`, 밤 전투 아님 `WrongPhase`, 유닛 없음 `EntityNotFound`, 죽음·필드 밖 `NotInCombat`, 재구성 중 `Reconstructing`, 무장 해제 `Disarmed`.
- **TP-6** 결과 형식 `Contracts/Results/RoarForecastResult`(readonly struct, `UnitAttackRangeResult` 꼴): 성공·실패 이유·보스 ID·범위 칸·대상이 있을 때의 공격 미리보기.
- **TP-7** 범위 칸 원본: 대상이 있으면 공격 미리보기의 영향 칸, 없으면 반응 계산의 빈 범위 칸. 실제 포효 사건(`CombatRoarEvent.AreaCells`)과 같은 두 원본이다.
- **TP-8** Buffer 종류 `RoarForecast = 10`, 요청 값 `CombatViewPreviewData.RoarForecast(boss)`, 미리보기 서비스는 공격 종류 입력으로 조립(`UnitAttackRange`와 같음).
- **TP-9** 새 시험 없음(위 진행 규칙). Core 조회 시험이 필요하면 사용자가 지시한다.
- **TP-10** RF-b 이후(View) 세부는 RF-a 보고 뒤 다시 대조해 이 계획에 덧붙인다(예고 소유자는 `CombatMovementRangeTracker`·`CombatUnitRangeHover` 꼴, 슬롯 규칙 `CombatPreviewSlotRule`).

## 2. Phase

### RF-a Core 조회(화면 변화 없음)

| Task | 파일 | 내용 |
|---|---|---|
| Ta.1 | `CoreCaculator/Contracts/Results/RoarForecastResult.cs`(새, + `.meta`는 Unity) | TP-6 |
| Ta.2 | `CoreCaculator/Contracts/Combat/CombatPreviewResult.cs` | `RoarForecast` 칸·`ForRoarForecast`·내용 하나 검사 |
| Ta.3 | `CommandAttackPlan.cs`·`PullAttackCalculator.cs`·`CommandAttackProjection.cs` | TP-2 관찰자 칸, 끌어오기 재구성이 이어받음, 토네이도 관찰자 |
| Ta.4 | `CombatAttackCalculation.cs`·`CombatAttackCalculator.cs` | TP-2 인자 전달 |
| Ta.5 | `AttackResultFactory.cs` | TP-3 |
| Ta.6 | `BossRoarCalculator.cs` | TP-1 공유 함수, `Forecast(input, rules)` |
| Ta.7 | `CombatCommandCalculator.cs` | 공개 입구 `GetRoarForecast(input, rules)` |
| Ta.8 | `BufferSystems/CombatView/CombatViewPreviewKind.cs`·`CombatViewPreview.cs` | TP-8 |
| Ta.9 | `CoreSystem/CombatSystem/CombatPreviewService.cs` | 종류 → 명령 종류, 계산 갈래 |
| Ta.10 | Core 전달 문서 §5.83(+ 제목) | 바꾼 Core 파일·천명님 줄 |

Unity 확인 Ua: 컴파일, 기존 EditMode 시험(특히 공격·끌어오기·토네이도·포효 시험), 전투 재생에서 바뀐 점 없음.

### RF-b 바닥 범위(View)

규칙 `CombatRoarForecastRule`, 소유자 `CombatRoarForecastTracker`, 강조 층 `RoarForecast`·`FieldView.roarForecastColor`, 정책 `CanPreview` 종류 한 줄, 묶음·Factory 연결(민형님 공유). 세부는 TP-10.

RF-b 착수 때 대조로 정한 것(2026-11-11, 사용자 "Ua 문제 없음, RF-b"):

- **TP-11** 예고 조회는 입력 소유자 목록(`CombatInteraction`)에 넣지 않는다. 포인터와 상관없고 다른 입력 중에도 보여야 해서(Q-RF4 A), HUD 행동 가능 여부 조회(`CombatHudActionAvailability`)와 같은 꼴의 배경 조회로 연결 입력 묶음(`CombatSceneInput`)이 갖고, 입력 분배 Tick 끝(`CombatSceneInputDriver.TickEnd`)이 HUD 조회 바로 앞에서 맞춘다.
- **TP-12** 보이는 기준(RF1)은 조회 객체 안 정적 함수 하나(`IsImminent`)다. 남은 수 = 임계 − 카운터 ≤ 1, 살아 있는 필드 유닛. 공격 제한 여부는 View가 다시 판단하지 않는다(Core 실패 답이면 범위 없음).
- **TP-13** 사실 정체성 = (전투 번호, 조회 원본 버전 `QueryRevision`). 바뀌면 보던 예고를 지우고 보스를 다시 모아 다시 묻는다. 결과 재생 중·입력 닫힘·종료·밤 전투 아님이면 지운다(Q-RF5 A). 요청은 플레이어 입력 국면에서만 — **적 턴에는 보이지 않는다**(요청 조건이 행동 가능 여부 조회와 같은 안정 상태).
- **TP-14** 요청 조건 식 원본은 `CombatInputPolicy.CanQueryRoarForecast` 하나다. 행동 가능 여부 조회와 같은 "안정 상태" 식을 private 도우미 하나로 꺼내 둘이 같이 쓴다. 미리보기 단계 표에 `RoarForecast → Combat` 한 줄.
- **TP-15** 전경 입력 소유자가 활성이면 새로 묻지 않고, 이미 그린 범위는 둔다(공격 조준 중에도 바닥 범위는 남음).
- **TP-16** 그리기: 강조 층 `RoarForecast`(평소에도 그림·채움, 층 표에서 기믹 호버 범위 바로 위 = 공격·이동 범위 층들보다 아래), 색 종류 `RoarForecast`, 팔레트 칸, `FieldView` 씬 색 `roarForecastColor`(기본 진한 주황). 계약 `ICombatRoarForecastFeedback`는 `FieldView`가 명시 구현하고 입력 피드백 묶음이 내놓는다(기믹 호버 범위와 같은 꼴). 보스가 여럿이면 범위 칸을 합쳐 그린다.
- **TP-17** 정리: 연결 입력 묶음의 표현 정리(`CancelPresentation`)·정지(`Stop`)가 지운다. Factory는 `CombatSceneInputFactory`의 생성 줄 둘(조회 만들기·묶음에 넘기기)만, 표시 Factory는 무변경(묶음 생성자가 `FieldView`를 이미 받음).

### RF-c 밀림 미리보기(View)

`CombatKnockbackPreview` 출처 둘(공격 조준 우선, Q-RF4 A).

RF-c 착수 때 대조로 정한 것(2026-11-11, 사용자 "Ub 문제 없음, RF-c"):

- **TP-18** "공격 조준이 있으면 그것" = 공격 조준 응답에 넉백 기록이 하나라도 있을 때다. 조준 응답에 넉백이 없거나 조준을 지우면 포효 예고의 화살표·도착 칸이 다시 보인다. 포효 예고가 없으면 지금과 똑같다(빈 조준 응답 = 지움).
- **TP-19** 포효 예고 계약의 주인을 `FieldView` → 공격 피드백(`CombatAttackFeedback`)으로 옮긴다. 넉백 미리보기(`CombatKnockbackPreview`)를 가진 것이 공격 피드백이라, 범위 칸과 넉백을 한 계약 한 주인이 받아야 조회 객체가 계약 하나만 안다. 계약은 `ShowRoarForecast(칸, 공격 미리보기 목록)`·`ClearRoarForecast()` 둘로 바꾼다. 범위 칸 그리기는 같은 층 호출이라 화면은 RF-b와 같다. 해제 뒤 규칙은 공유 수명(`CombatDisplayLifetime`)으로.
- **TP-20** 조회 객체는 보스별 Core 답(`RoarForecastResult`, 불변)을 그대로 기억하고, 그릴 때 범위 칸을 합치고 대상이 있는 답의 공격 미리보기를 모아 넘긴다. 넉백 미리보기는 받은 목록을 자기 목록에 복사해 둔다(조준을 지운 뒤 다시 그리려고).

### RF-d 문서·공유·묶음 끝 검토

폴더 배치 안내, 민형님 Factory 공유, 설계 구현 메모, 평가 아티팩트 적용 기록은 해당 없음(평가 문제 해결이 아니라 새 기능), 묶음 끝 검토 하나.

## 3. 실행 기록

### 3.1 RF-a Core 조회 코드 완료(2026-11-11, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

Ta.1~Ta.10을 계획대로 했다. 계획과 다른 점 하나: 재구성·무장 해제의 실패 이유 가르기를 `CombatAttackQueries.RestrictionFailure` 하나로 꺼냈다(`IntrinsicUnitReadiness`와 포효가 같이 씀). 포효 쪽에 같은 두 줄을 베끼지 않으려는 것이다.

바꾼 파일

- 새: `CoreCaculator/Contracts/Results/RoarForecastResult.cs`(`.meta`는 Unity가 만듦)
- Core: `BossRoarCalculator`·`CombatAttackQueries`·`CombatAttackCalculation`·`CombatAttackCalculator`·`CommandAttackPlan`·`CommandAttackProjection`·`PullAttackCalculator`·`AttackResultFactory`·`CombatCommandCalculator`·`Contracts/Combat/CombatPreviewResult`·`CombatSystem/CombatPreviewService`
- Buffer: `CombatViewPreviewKind`·`CombatViewPreview`
- 문서: Core 전달 문서 §5.83(+ 제목)

동작: 기존 호출은 모두 같다(관찰자 인자 기본 = 공격자 진영, 반응 공격은 공개 미리보기가 꺼져 있으면 옛 길, 포효 대기 조건·카운터 되돌림은 식을 옮기기만). 아직 아무도 `RoarForecast`를 묻지 않아 화면 변화가 없다.

7기준 자체 점검(RF-a 범위)

| 기준 | 본 것 | 남은 것 |
|---|---|---|
| SSOT | 포효 전투 조건·유닛 조건·카운터 되돌림·전체 지정 검사가 `BossRoarCalculator` 한 곳, 제한 이유가 `RestrictionFailure` 한 곳, 범위 칸 원본이 실제 포효 사건과 같음, 관찰자 기본값은 계획 생성자 한 곳 | 낮음: 밤 전투 조건을 View(요청 여부)와 Core(계약) 둘이 봄 — 역할이 달라 그대로(L1) |
| SOLID | 예고는 포효 지식의 주인(`BossRoarCalculator`)에, 결과 형식은 계약 폴더에 따로, 관찰자는 끝 선택 인자로 확장(기존 호출 무변경) | — |
| FW | 새 서비스·키 없음, 미리보기 서비스 갈래 한 줄·Buffer 종류 한 줄 | — |
| 독립성 | 실제 포효·공격 미리보기·AI 미리보기 경로 무변경 | — |
| 캡슐화 | 공유 함수 private, 새 입구는 internal(`Forecast`·`RestrictionFailure`·`PreviewObserver`), 공개는 `GetRoarForecast`·결과 형식·Buffer 값만 | — |
| C#/Unity | readonly struct·배열 복사·nullable 내용, LF·BOM 없음 | — |
| If/Tick | 새 갈래는 모두 계약 갈래(실패 이유·포효 없음 예외). `ForRoarForecast`의 다시 보기는 `default` 값 때문(다른 결과와 같은 규칙) | 낮음: 예고 한 번에 되돌림 전이(`Finish` + `Project`)를 만듦 — 결과마다 한 번이라 받아들임, GC 측정 때 볼 것(L2) |

위험: 반응 + 공개 미리보기 조합은 처음 쓰는 길이다(`AttackPreview.FromFacts` 검사가 반응 결과에서 통과해야 함). §5.83에 천명님 확인 부탁으로 적었다(L3).

Unity 확인 Ua

1. 컴파일 오류 없음(새 `.cs`의 `.meta` 생성 포함).
2. 기존 EditMode 시험 — 특히 공격·끌어오기·토네이도·포효(`Roar`/`Boss` 이름이 든 시험)·`CommandAttackPlan` 생성 시험 여섯.
3. 전투 재생: 플레이어 공격 미리보기·적 행동·포효 발동이 전과 같음(화면 변화 없음).

Ua 문제 없음(2026-11-11 사용자).

### 3.2 RF-b 바닥 범위 코드 완료(2026-11-11, 사용자 "Ua 문제 없음, RF-b", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

TP-11~TP-17대로 했다. 계획과 다른 점 하나: 보이는 기준은 별도 클래스(설계의 `CombatRoarForecastRule`) 대신 조회 객체 안 private 정적 함수 `IsImminent`다(쓰는 곳이 하나라 원본 하나는 그대로, 공개 면을 줄임).

바꾼 파일

- 새(`.meta`는 Unity가 만듦): `Input/Interaction/CombatRoarForecastTracker.cs`, `Input/Contracts/ICombatRoarForecastFeedback.cs`
- View: `Display/Field/FieldHighlightLayers`(층 `RoarForecast = 1 << 19`·색 종류·층 표 한 줄)·`FieldHighlightPalette`(칸)·`FieldView`(씬 색 `roarForecastColor`·계약 구현)·`Display/CombatInputFeedback`(계약 하나)·`Input/CombatInputPolicy`(`CanQueryRoarForecast`·안정 상태 도우미·단계 표 한 줄)·`Input/CombatSceneInput`(소유·`SyncRoarForecast`·정리 둘)·`Input/CombatSceneInputDriver`(Tick 끝 한 줄)
- Factory: `CombatSceneInputFactory` 생성 줄 둘(민형님 Factory 공유 §55)
- 문서: 폴더 배치 안내 갱신 줄

확인한 사실: `QueryRevision`은 상태가 게시될 때만 오르고 미리보기 응답으로는 오르지 않는다(`CombatStateBuffer`) — 받은 답 때문에 다시 묻는 반복이 없다.

7기준 자체 점검(RF-b 범위)

| 기준 | 본 것 | 남은 것 |
|---|---|---|
| SSOT | 보이는 기준 `IsImminent` 하나, 요청 조건 `CanQueryRoarForecast` 하나(행동 가능 여부 조회와 안정 상태 식 공유), 층 속성은 층 표 한 줄, 색은 씬 값 하나, 범위 칸은 Core 응답만 | — |
| SOLID | 조회 객체는 요청·응답·지우기 수명만, 그리기는 `FieldView`, 계약은 두 동작뿐 | — |
| FW | 새 서비스·키 없음, Factory 생성 줄 둘 | — |
| 독립성 | 입력 소유자 목록·공격 조준·호버 무변경. 미리보기 슬롯만 배경 규칙으로 나눠 씀 | 낮음: 전경 입력 중에는 새로 묻지 않아, 결과 직후 바로 공격 모드로 들어가면 모드를 끝낼 때까지 예고가 늦게 뜰 수 있음(TP-15 그대로, L4) |
| 캡슐화 | 조회 객체 필드·기준 함수 private, 입구는 `Sync`·`Clear` 둘 | — |
| C#/Unity | 보스·범위는 사실이 바뀔 때만 모음(매 Tick 할당 없음), 정렬은 기본 비교자, 씬 값 기본은 코드 값(씬 저장 때 칸 생김) | — |
| If/Tick | 매 Tick은 남겨 둘 사실 검사 + 정체성 비교 둘, 지울 것 없으면 `Clear` 바로 돌아감 | — |

Unity 확인 Ub(보스 스테이지 — Play From Boot "Dev Battle Stage"를 보스 스테이지로)

1. 컴파일 오류 없음(새 `.cs` 둘의 `.meta` 생성 포함).
2. 보스 카운터 3/5 이하일 때는 바닥 표시 없음.
3. 카운터 4/5가 되면(공격으로 맞히거나 플레이어 턴 종료 뒤) 플레이어 턴에 바닥에 포효 범위(주황 테두리·채우기)가 보임. 범위는 실제 포효 범위와 같아야 함.
4. 공격 모드·유닛 호버·이동 중에도 범위가 남고, GridOutline에서는 겹친 칸에서 조준·이동 범위가 안쪽에 그려짐.
5. 결과 재생 중·적 턴에는 범위가 사라지고, 플레이어 입력으로 돌아오면 다시 보임.
6. 포효가 터지면(카운터 0) 범위가 사라짐. 보스가 죽어도 사라짐.
7. 다른 배경 표시(HUD 행동 가능 여부·유닛 호버 범위·이동 모드 범위)가 전과 같음.
8. `FieldView` 인스펙터에 "포효 예고 색" 칸이 보임.

Ub 문제 없음(2026-11-11 사용자).

### 3.3 RF-c 밀림 미리보기 코드 완료(2026-11-11, 사용자 "Ub 문제 없음, RF-c", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uc 대기)

TP-18~TP-20대로 했다. 계획과 다른 점 없음.

바꾼 파일

- `Display/Feedback/CombatKnockbackPreview`: 출처 둘(`ShowAim`·`ClearAim`·`ShowRoar`·`ClearRoar`), 그리기를 `Draw`(조준 하나)·`DrawRoar`(예고 모두)·`Append`(응답 하나를 작업 목록에)로 나눔, 옛 `Show`·`Clear` 삭제(부르는 곳은 공격 피드백뿐)
- `Display/Feedback/CombatAttackFeedback`: 포효 예고 계약 구현(범위 칸 → `RoarForecast` 층, 넉백 → 넉백 미리보기), 넉백 계약은 `ShowAim`·`ClearAim`을 부름
- `Input/Contracts/ICombatRoarForecastFeedback`: `ShowRoarForecast(칸, 공격 미리보기 목록)`·`ClearRoarForecast()`로 바꿈
- `Display/Field/FieldView`: RF-b의 포효 예고 계약 구현 두 줄 삭제(층·색은 그대로)
- `Display/CombatInputFeedback`: 포효 예고 계약의 주인 `field` → `attack`
- `Input/Interaction/CombatRoarForecastTracker`: 보스별 Core 답을 그대로 기억, 그릴 때 범위 칸 합치기 + 대상 있는 답의 공격 미리보기 모으기

동작: 포효 예고가 없으면 넉백 미리보기는 전과 같다(빈 조준 응답 = 지움, 조준 지움 = 지움). 공격 조준 시험 경로·Factory 무변경.

7기준 자체 점검(RF-c 범위)

| 기준 | 본 것 | 남은 것 |
|---|---|---|
| SSOT | 넉백 화살표·도착 칸 규칙(멈춘 이유 → 표지, 점유 칸 → 도착 칸)은 `CombatKnockbackPreview` 한 곳을 두 출처가 같이 씀, 어느 출처를 그릴지도 그 한 곳, 넉백 원본은 Core 응답 | — |
| SOLID | 조회 객체는 수명만, 그리기 결정은 넉백 미리보기, 계약 하나에 주인 하나(범위·넉백을 같이 받음) | — |
| FW | 새 서비스·Factory 변경 없음 | — |
| 독립성 | 공격 조준은 넉백 계약을 전처럼 부르고 포효 예고를 모름 | 낮음: 같은 대상에 조준 넉백과 포효 넉백이 함께 있으면 조준 쪽만 보임(Q-RF4 A대로, L5) |
| 캡슐화 | 출처 상태(`_aimShown`·`_roar`)는 넉백 미리보기 private, 조회 객체는 답만 기억 | — |
| C#/Unity | 답 사전·작업 목록 재사용, 사전 값 열거는 구조체 열거자(할당 없음), 넉백 목록은 응답마다만 복사 | — |
| If/Tick | 새 갈래는 출처 고르기 하나(`_aimShown`)뿐, 매 Tick 경로 무변경 | — |

Unity 확인 Uc(보스 스테이지)

1. 컴파일 오류 없음.
2. 카운터 4/5에서 포효 범위 안에 아군(넉백으로 밀릴 수 있는 유닛)이 있으면, 밀치기 미리보기와 같은 화살표·도착 칸이 보임. 막힘·면역 표지 색도 같음.
3. 범위 안에 밀릴 유닛이 없으면 범위만 보이고 화살표는 없음.
4. 공격 모드에서 넉백이 있는 칸을 호버하면 그 넉백만 보이고, 호버를 옮기거나(넉백 없는 칸)·공격 모드를 끝내면 포효 화살표가 다시 보임.
5. 포효 예고가 없을 때 공격 모드 넉백 미리보기가 전과 같음.
6. 재생 중·적 턴·포효 뒤에는 화살표도 범위와 함께 사라짐.

### 3.4 Uc 보고: 이동한 유닛의 화살표 없음 — 다시 확인하니 정상(2026-11-11)

**결과: 다시 확인하니 이동한 유닛의 화살표가 나온다(사용자 "아 생기네 확인"). 코드 결함 없음, 임시 진단 로그는 지웠다(트래커가 진단 넣기 전과 같음). Uc 문제 없음.** 아래는 진단할 때의 기록이다.

사용자 보고: 4/5에서 내 턴에 유닛을 포효 범위 안으로 옮긴 뒤 범위는 보이는데 그 유닛의 화살표·도착 칸이 없다(처음부터 범위 안에 있던 유닛은 보임). 원하는 동작은 이동을 끝낸 뒤 도착 위치 기준 화살표(드래그 중에는 필요 없음).

코드 대조(변경 없음): 이동도 수락된 명령이라 `QueryRevision`이 오르고(`CombatStateBuffer.PrepareCommand`), 재생 중(`IsPresenting` = 표시 번호 < 결과 번호)에 예고를 지운 뒤 재생이 끝나면 새 위치로 다시 묻는다. 경로 입력은 결과 전에 풀리고(`BeforeResult` → `CancelInputs`) 공격 모드는 자동으로 켜지지 않아 전경 입력도 막지 않는다. 범위가 다시 보이는 것과 맞다. 그래서 Core 답에 그 유닛의 넉백 기록이 없는 쪽으로 본다. Core 넉백 계산(`AttackKnockbackCalculator.ResolveSteps`)이 기록을 빼는 경우는 둘이다: 그 공격으로 죽는 대상(`Health.IsDead` → 건너뜀), 피격 대상이 아님(가림 등으로 전달이 안 됨 — 범위 칸에는 들어 있어도).

임시 진단 로그(지움): `CombatRoarForecastTracker`가 요청 때 `[RoarForecastDiag] 요청 …`, 답 때 `[RoarForecastDiag] 답 …`(대상마다 위치·직접 피격·체력 전후·사망, 넉백마다 시작→끝·멈춤·칸 수)을 남긴다. 컴파일 미확인.

### 3.5 RF-d 문서·공유·묶음 끝 검토(2026-11-11, 사용자 "RF-d 진행", 코드 변경 없음)

- 문서: 설계 §6 구현 메모, 폴더 배치 안내(계약 주인 `CombatAttackFeedback`), 공유 셋(모두 미전달) — Core 전달 문서 §5.83 덧붙임(Buffer 열거의 천명님 줄 쉼표·View 완료), 민형님 Factory 공유 §55(RF-b), 현강님 구조 공유 §28(HUD 행동 가능 여부 조회 `f659532`의 현강님 줄 셋: 연결 입력 묶음 생성자 서명·입력 Factory 생성 순서 주석·반환 줄). 다른 작업자 줄은 blame으로 다시 셈 — 그 밖은 사용자(Droute)·Claude 줄.
- 묶음 끝 검토(읽기 전용 검토자 하나): 컴파일 오류 없음, 기존 경로(포효 대기·제한 이유·실제 포효·관찰자 기본값·끌어오기 재구성) 동작 같음 확인, `FromFacts`는 반응 + 공개 미리보기에서도 예외 없음. 평균 약 8.6(SSOT 8.5·SOLID 8.5·FW 9.5·독립성 7.5·캡슐화 9.0·C#/Unity 9.0·If/Tick 8.5). 높음 0·중간 1·낮음 11.
  - **M1(코드로 확인):** 공격 모드에서 호버 칸을 옮길 때마다 조준 넉백을 지우고(`AttackTargetingInteraction.ClearHoverDisplay` → `ClearKnockbackPreview`), `CombatKnockbackPreview.ClearAim`이 곧바로 포효 넉백을 다시 그려, 새 응답까지 "조준 → 포효 → 조준" 화살표가 바뀌어 보인다. 커서가 판 밖이어도 포효 화살표가 나온다. TP-18이 호버 단위 지우기를 생각하지 않은 것. 제안 (a) 전경 입력 중에는 포효 넉백만 숨김(바닥 범위는 남김) / (b) 모드가 끝날 때만 포효를 다시 그림.
  - 낮음: L1 `IsImminent`가 Core 증가량(피격·턴 끝)을 보지 않음(지금 데이터는 둘 다 1), L2 넉백 도착 칸이 행동 중 층이라 평소 화면에 포효 넉백이 있으면 GridOutline이 행동 중 모양, L3 공격 피드백이 포효 범위 층까지 맡음(TP-19 이유는 있음), L4 `TickEnd`가 사실·마지막 응답을 두 번 읽음, L5 쓸모없는 검사 셋(`characters == null` 조용히 돌아감·`character == null`·`latest != null`), L6 `RoarForecastResult.WithTargets`가 영향 칸을 두 번 복사·범위 칸과 공격 영향 칸이 같은 사실 두 벌, L7 칸 합치기 `List.Contains`(O(n²)), L8 포효 없는 유닛 물음 = 예외(계약 오류 규칙대로), L9 호버 요청이 먼저 슬롯을 잡아 예고가 늦을 수 있음(굶김 없음), L10 팔레트 칸 기본값 투명(기믹 색과 같은 꼴), L11 예고는 "지금 포효하면"이라 카운터를 채우는 공격 뒤 상태와 넉백이 다를 수 있음(설계대로).
  - 반영은 사용자 결정 대기.

### 3.6 검토 권장 반영(2026-11-11, 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, Ud 대기)

- **M1:** 전경 입력(공격 모드·이동 경로·배치·아이템 드래그 — `CombatInteraction.IsInputOwnerActive`) 중에는 조회 객체가 포효 넉백을 빈 목록으로 넘기고 바닥 범위만 남긴다(`CombatRoarForecastTracker._hideKnockback`, 바뀌면 그리는 중일 때만 다시 넘김). 공격 조준이 호버마다 자기 넉백을 지워도 넉백 미리보기의 포효 목록이 비어 있어 끼어들지 않는다. 전경 입력이 끝나면 다음 Tick 끝에서 다시 넘긴다. 계약·넉백 미리보기 무변경. TP-18은 "전경 입력이 아닐 때"로 좁혀짐(사용자 "드래그 중에는 X"와도 맞음).
- **L1:** "1 남음" 기준은 그대로, 증가량을 보지 않는다는 데이터 전제를 `IsImminent` 주석에(증가량 2 이상 보스가 생기면 다시 정함).
- **L2:** 받아들임 — 넉백 도착 칸은 행동 중 층이라, 평소 화면에 포효 넉백이 있으면 GridOutline이 행동 중 모양(겹친 칸 안쪽 줄이기·채우기 깊이)이 된다. 포효 범위와 낙석·기믹 범위가 겹친 칸 모양이 넉백 유무에 따라 다를 수 있다.
- **L4:** 입력 분배 `TickEnd`가 사실·답한 번호·마지막 응답을 한 번 읽어 포효 예고·HUD 조회에 같이 넘김(조회가 슬롯에 써도 그 값들은 안 바뀜). HUD 조회 호출 줄(현강님 줄)의 인자 출처만 바뀜.
- **L5:** `CollectBosses`의 `characters == null`은 예외로, `IsImminent`의 `character == null`·응답 소비의 `latest != null` 검사는 지움.
- **L6:** `RoarForecastResult` 생성자는 복사하지 않고 불변 메모리만 받음 — 대상이 있으면 범위 칸이 공격 미리보기 영향 칸을 그대로 가리킴(같은 사실 한 벌), 대상이 없으면 `WithoutTargets(IReadOnlyList)`가 한 번 복사(Core `Forecast`의 `ToArray` 삭제).
- **L7:** 칸 합치기를 `HashSet` 작업 목록으로.
- **그대로:** L3(계약 주인 TP-19 유지), L8~L11.

Unity 확인 Ud(보스 스테이지, 카운터 4/5)

1. 컴파일 오류 없음.
2. 평소(선택만·아무것도 안 함)에는 범위와 포효 화살표가 함께 보임.
3. 공격 모드에 들어가면 포효 화살표는 사라지고 범위만 남음. 호버를 옮기거나 판 밖으로 나가도 포효 화살표가 끼어들지 않음. 넉백이 있는 칸을 호버하면 조준 화살표만 보임.
4. 공격 모드를 끝내거나 공격이 끝나면 포효 화살표가 다시 보임.
5. 이동 경로를 끄는 동안 포효 화살표가 사라지고, 이동을 끝내면 새 위치 기준으로 다시 보임.
6. HUD 행동 가능 여부 표시가 전과 같음.

함께 받은 질문: 포효 범위 색이 적 공격 범위와 같은가 → 아니다. 칸이 따로다(`roarForecastColor` 코드 기본 (1, 0.35, 0.1), 씬 `enemyAttackRangeColor`는 (1, 0.157, 0)). 같게 할지는 결정 대기.
