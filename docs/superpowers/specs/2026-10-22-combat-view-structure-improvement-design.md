# 전투 View 구조 개선안 (2026-10-22, 2판)

- 작성: View 작업, 2026-10-22. 사용자 요청: "개선안 작성해, 개선안 작성 시 평가 기준에 대해서 검토하고 개선안이 적용되었을 때의 점수로 정리해줘. 개선안의 경우 기존 평가 및 구조를 모르는 완전히 새로운 세션을 이용해서 검토 진행해."
- **설계만 적는다. 코드 변경 없음.** 결정 뒤 Task 계획 → Phase 구현(CLAUDE.md). 전투 View 작업이라 MSBuild·새 `[Test]`는 쓰지 않는다.
- 기준 코드: HEAD `067b53ad`. 선행: [문제점 분석](2026-10-21-combat-skeleton-problem-analysis.md)(P1~P36, 영역 기준 §0, 결정 §6), [골격 원칙 평가](2026-10-21-combat-skeleton-principles-evaluation.md)(7.3), 1·2번 대조 [아티팩트](https://claude.ai/artifact/BZJmyWSak9bFjK1nww1L13).
- 목표(사용자, 분석 §6 7번): Framework·GlobalSystem·Core(계산기)는 주어진 기반으로 두고, 그 위에서 **Combat 씬의 전투 View 구조**(작성자 무관)를 가장 좋게 짠다.
- **판 이력:** 1판(같은 날) → 이 문서와 코드만 읽는 새 에이전트(이전 평가·분석을 모름)가 독립 검토 → 2판에서 평가 기준·설계·점수를 고쳤다. 검토 결과와 반영 여부는 §10.

## 1. 평가 기준 (검토 반영)

### 1.1 평가 범위

분석 §0의 **범위 안 49개**만 점수에 넣는다: 전투 골격(`LifeCycles/Combat/`) 18, 전투 View 중 골격과 맞닿는 16(`CombatViewSystem`·`ICombatViewService`·`CombatSceneRoot`·`CombatResultPlayback`·입력 정책·입력 기능·`CombatViewBuffer` 등), 전투 결과 화면 `CombatEndUI` 1, 튜토리얼의 전투 접점 3(부분), 부트·개발 진입 9, 공용 오류 경계 2.

- 전투 View의 연출·입력 내부(Display·Playback·Presentation·Input의 나머지 약 200개)는 이 평가 밖이다(각 설계 문서의 평가가 따로 있다).
- 기반(Framework·GlobalSystem·LifeCycle 기반·조립·키·Core·Buffer)과 다른 영역(Stage·Lobby·StageMap·View 공용)의 코드는 점수에 넣지 않는다.
- **기반 파일 안의 우리 연결 줄은 고칠 수 있다(CLAUDE.md 작업 방식).** 우리 Plan 등록·입구, 우리 전용 열거 값(`JobType`의 전투 값), 우리 서비스 키·그룹 노출, Factory에서 우리 시스템·오류 경계를 조립하는 줄이 해당하고, 바꾸면 민형님 공유 문서에 적는다. 독립 검토는 `JobType` 값과 Factory 변경을 "범위 모순"으로 봤지만, 이 규칙 안의 연결 작업이다(§10.2 Q-R2).

### 1.2 여섯 기준 — 무엇을 보고, 겹치지 않게 어디에 두나

사용자 기준 여섯을 그대로 쓴다. 문제 하나는 **주된 기준 하나에만** 둔다.

| # | 기준 | 보는 것 | 다른 곳으로 보내는 것 |
|---|---|---|---|
| 1 | SSOT | 같은 사실·규칙·결정·상수가 두 곳 이상에 적혀 **서로 어긋날 수 있는가** | 어긋날 일은 없고 헛일만 하는 사본 → 6 |
| 2 | SOLID | 책임 수(SRP), 계약 폭(ISP), 계약에 기대는가(DIP), 계약을 지키는가(LSP), 넓힐 때 고치지 않아도 되는가(OCP) | Framework 계약 위반 → 3, 변경이 번지는 범위 → 5 |
| 3 | JMH Framework 지침·FW 계약 | 프로그래머 가이드 FW-001~016·§8 금지 패턴, Runner·Loading·LifeCycle 사용 계약, 오류 경계(F1) 계약 | 우리 설계 문서의 전제 오류 → 보조 점검 "문서 정확성"(점수 밖) |
| 4 | 있는 기능 사용 | Framework·Core가 이미 주는 기능·보장을 다시 만들지 않는가 | 같은 일을 두 번 하는 것 → 6 |
| 5 | 기능 변경의 독립성 | 기능 하나를 바꿀 때 고치는 파일·작업자 수, 다른 영역이 우리 내부 순서·단계에 기대는가 | 같은 규칙이 여러 곳에 적힌 것 자체 → 1 |
| 6 | 불필요·우회 | 늘 참인 확인, 쓰이지 않는 칸·값, 두 번 하는 일, 폴링·한 번 돌아가는 경로 | — |

### 1.3 기반을 주어진 것으로 보는 규칙

- (a) 기반의 기능·계약 자체의 한계는 **감점하지 않고 "기반 한계"로 따로 적는다.** 예: Runner에 완료 신호 없음, 등록이 결과를 돌려주지 않음, Core의 Run 종료 확정 시점, 조립 순서.
- (b) 그 한계를 흡수하는 View 쪽 구조가 사본·우회·넓은 의존을 만들면 그 문제의 주된 기준에서 감점한다.
- (c) 다른 영역의 코드 자체는 점수에 넣지 않는다. 다른 영역이 **우리 타입의 내부 단계·순서를 해석해야 하는 이유가 우리 쪽에 있으면** 5에서 우리 몫만 감점한다.

### 1.4 점수 계산

기준마다 10점에서 감점을 뺀다. 종합은 여섯 기준의 평균이다.

| 심각도 | 뜻(결과로 정의) | 감점 |
|---|---|---|
| 높음 | 지금 잘못 동작하거나 계약 위반이 드러나 있음, 또는 바꿀 때 여러 영역·작업자를 함께 고쳐야 함 | 1.2 |
| 중간~높음 | 위와 같은 결과가 조건부로 생김 | 0.9 |
| 중간 | 바꿀 때 실수하기 쉬운 사본·우회·넓은 의존, 또는 오류가 조용히 묻히는 경로 | 0.6 |
| 낮음~중간 | 실수 가능성은 작지만 바꿀 때 여러 곳을 봐야 함 | 0.4 |
| 낮음 | 읽기·유지 비용만 있음 | 0.2 |
| 작은 것 | 정리 수준 | 0.1 |

- 범위 안 몫이 일부뿐인 문제는 **범위 안 몫의 심각도**로 잡는다.
- **상한:** 한 기준 안에서 같은 근본 원인(분석의 G1~G8)에서 온 문제들의 감점 합은 1.5를 넘지 않는다. 근본 원인 하나가 증상 여럿으로 쪼개져 감점이 부풀지 않게 하려는 것이다(독립 검토 권고).
- **적용 뒤 점수에는 "개선이 새로 만드는 문제" 칸을 반드시 둔다**(독립 검토 권고, §9).
- 1판은 감점 크기를 10-21 평가 점수(7.3)에 맞춰 정했다. 독립 검토가 "순환 보정"이라고 지적해서, 2판은 위처럼 심각도를 결과로 정의했다.

### 1.5 보조 점검 (점수 밖)

여섯 기준에 들지 않지만 이번 작업의 위험을 보는 관점이다. 점수에는 넣지 않고 묶음마다 적는다(§9.3).
- 정확성·견고성: 오류 없는 대기, 부분 실패, 두 번 불렸을 때
- 범위·소유권 준수: 바꾸는 다른 사람 줄, 연결 줄
- 검증 위험: 새 시험·빌드 금지 아래에서 바뀌는 동작의 위험 등급
- C#/Unity 관례, Tick당 할당
- 문서 정확성: 우리 설계 문서가 실제 실행과 맞는가(P14)

## 2. 지금 점수

이번에 새로 찾은 문제 둘:
- **P37 (중간)** `CombatViewSystem`이 1,137줄이고 역할이 여럿이다: 씬 연결 수명(`:372-436`), 입력 Layer(`:797-820`), 결과 재생(`:704-775`), 완료 표시(`:847-864`), 포기(`:149-151·1023-1026`), 소리(`:861-862`), 튜토리얼 가림(`:924·929-940`). 골격이 보는 부분(`ICombatProgressView` 구현)도 그 안에 섞여 있다.
- **P38 (낮음)** 제출 정지의 주인이 둘이다. View가 종료를 처음 보면 스스로 정지하고(`CombatViewSystem.cs:992-997`), Plan도 정지 단계를 가진다(`CombatPlan.cs:95-96·114-115`). 입력 정책 `CanSubmit`도 종료면 막는다(`CombatInputPolicy.cs:48-53`).

2판에서 심각도를 고친 것(독립 검토 의견 반영):
- P16 1.2 → 0.9: 튜토리얼 판정이 Decide와 같은 뜻인지 시험이 고정하고 있다.
- P26 0.6 → 0.4: FW-101은 SHOULD(경고 수준)다.
- P22·P23 0.6 → 0.4: 숫자 규약, 개발 전용.
- P30 0.4 → 0.6: 오류 없는 영구 대기는 F1 계약 정면 위반이다.
- P13 0.9 → 0.6: 지금 코드에서는 재확인이 "등록과 실행 사이의 변화"를 잡는 역할이 있어 늘 참은 아니다.
- P14: 기준 3에서 빼고 보조 점검의 문서 정확성으로 옮겼다.

| 기준 | 문제(감점) | 감점 합 | 점수 |
|---|---|---|---|
| 1 SSOT | P16 결정 재생성(0.9), P5 세션 표현 여럿(범위 안 몫 0.6), P24 순서 정수 7파일(0.6), P22 진입 종류 숫자(0.4), P23 노드 전투 판정 사본(0.4), P11 같은 대상 검증 6곳(0.4), P38 제출 정지 주인 둘(0.2) | 3.5 | **6.5** |
| 2 SOLID | P9 요청 상태를 여러 곳이 바꿈(0.6), P25 골격이 View 전체 계약에 기댐(0.6), P37 조정자 비대(0.6), P4 바쁨 예외가 계약 밖(0.4) | 2.2 | **7.8** |
| 3 FW 지침·계약 | P30 오류 없는 영구 대기(F1, 0.6), P26 Job이 그룹 전체를 받음(FW-101, 0.4), P28 Bootstrap 캐스트(§8, 0.4), P29 오류 경계 대체 생성(0.4), P35 바꿀 수 있는 정적 목록(0.1) | 1.9 | **8.1** |
| 4 있는 기능 사용 | P15 Core가 보장하는 보존 규칙을 Job이 다시 증명(0.6), P12 완료 정체성을 참조 동일성으로 따로 판단(0.4) | 1.0 | **9.0** |
| 5 독립성 | P20 보스 조건이 판단·Job·View에 퍼짐(1.2), G2 묶음 — P6 이탈 완료 추론(0.6)·P7 전환 허가 없음(0.6)·P10 내부 단계 해석(0.4) — 합 1.6 → 상한 1.5, P1 튜토리얼과 Tick 순서로 Runner를 나눔(0.2) | 2.9 | **7.1** |
| 6 불필요·우회 | P13 판단 재확인(0.6), P3 착지 대기 폴링 Job·다섯 객체 경유(0.6), P21 Run 종료 때 새 Run 두 번(0.6), P17 정보 없는 `IsPlaybackBusy`(0.2), P19 안 쓰는 `AdvanceCombat`(0.2), P34 죽은 칸·닿지 않는 가드(0.1) | 2.3 | **7.7** |
| **종합** | | | **7.7** |

- 독립 검토의 지금 점수는 7.6이다(§10.1). 차이는 P14를 기준 3에 둔 것과 P30을 0.4로 본 것이다.
- 기반 한계(감점 안 함): P2·P18·P32·P36, G1의 `IsIdle` 공유, Core의 Run 종료 확정 시점(P20의 원인 쪽), 조립 순서(LifeCycle → InGame).
- 다른 영역: P8·P27. 규칙 제약: P31·P33.

## 3. 개선 묶음과 순서

| Phase | 묶음 | 내용 | 다루는 문제 |
|---|---|---|---|
| 1 | B | 이탈 순서를 모든 결과에 하나로(정리 → 표시 → 확인 → 떠나기) | P20·P21·P6·P34(일부)·③ |
| 2 | A | 판단을 실행 순간으로(판단 뒤 Runner에 넘김) | P13·P15 |
| 3 | C | 진입: 착지 대기를 폴링 대신 사건으로, 진입 종류·요청 단계를 이름으로 | P3·P22·P9·P10·P11·P4 |
| 4 | D | View 쪽: 골격이 보는 계약 분리, 판정 함수 공개(⑤), `IsPlaybackBusy` 정리 | P25·P37(일부)·P16·P38·P17·P5 |
| 5 | E | 의존·조립·연결 | P26·P25·P28·P29·P19·P24 |
| 6 | F | 오류·작은 것·문서 | P30·P35·P12·P23·P1·P14 |
| 7 | G | 넘길 변경 정리(호진·현강·천명·민형님) | — |

- **B를 A보다 먼저 한다(독립 검토 권고).** 정리와 표시를 한 걸음에 두는 것은 Runner가 같은 Tick에 Job을 이어 실행하는 것만으로 된다. 지금 보스 경로가 이미 그렇다(`CombatPlan.cs:175-190`). B를 먼저 하면 보스 Job이 먼저 없어져 A가 걷을 범위가 줄어든다.
- D의 계약 분리는 B 다음이 자연스럽다. B가 결과 유지 판정을 이름 있는 하나로 만든 뒤 그 판정을 계약 쪽으로 옮긴다.

## 4. 묶음 B — 이탈 순서를 모든 결과에 하나로 (Phase 1)

### 4.1 왜

지금 보스 승리만 "정리 먼저, 표시 나중"이고 나머지는 "표시 먼저, 정리 나중"이다. 결과 화면(`CombatEndUI.cs:93-105`)이 최종 승리를 Core의 Run 상태(`Outcome == Victory`, `LastSummary`)로 고르는데, Core는 보스 승리의 Run 종료를 `CompleteChapter` 뒤에야 확정한다(기반 한계). 그래서 보스만 특수 경로가 생겼다.

특수 경로가 퍼진 곳:
- `Decide` 0행(`C/Plans/CombatPlan.cs:83-90`)
- `PresentCompletion` 보스 분기(`:171-190`), `ExitBattle` 두 갈래(`:195-214`)
- `CombatCompletionCheckJob`(`C/Jobs/CombatExitJobs.cs:102-136`), `CombatRunOutcomeJob`(`:229-264`)
- `RequiresChapterCompletion`을 쓰는 세 곳(`CombatPlan.cs:85·181`, `CombatExitJobs.cs:217`)
- View 유지 분기(`CombatViewSystem.cs:289-294`)

**기반을 바꾸지 않는 가장 단순한 구조는 모든 결과를 같은 순서로 두는 것이다: 남은 연출 → 정리(보스면 챕터 완료까지) → 표시 → 사용자 확인 → 떠나기.** 그러면 결과 화면은 어떤 결과든 Run 상태가 확정된 뒤에 열린다.

### 4.2 바꿔도 되는 근거 (코드 대조, 독립 검토도 확인)

- `EndBattle`은 전투 캐릭터·전투 저장만 비운다(`BufferSystems/Core/BattleTransactions.cs:329-333`).
  - 전투 저장에서는 현재 전투·영혼·미리보기만 비우고, `BattleInstanceId`·완료·결과 순번 S는 남긴다(`CombatStateBuffer.cs:167-172`).
  - 현재 맵 선택은 건드리지 않는다.
  - 그래서 `CombatEndUI`가 읽는 값(`:93-179`)은 정리 뒤에도 그대로다: 완료, 보상 영수증, Run 요약, `CurrentMapKey`로 찾는 스테이지 좌표.
- View 유지 분기의 조건(`:291-293`)에는 보스 지식이 없다. 보스 언급은 주석뿐이다.
- 튜토리얼의 "세션 전투 끝남"은 완료 + 인스턴스 번호로 본다(`TutorialFacts.cs:105-111`). 둘 다 정리 뒤에도 남는다.

### 4.3 흐름

| 단계 | 지금 · 일반 | 지금 · 보스 | 바꾼 뒤 · 모든 결과 |
|---|---|---|---|
| 승패 기록 | 제출 정지 | 제출 정지 | 제출 정지 |
| 남은 연출 | 기다림 | 기다림 | 기다림 |
| 한 걸음 | 표시 | 확인 Job → 정리 → 챕터 완료 → 표시 | **정리 → (보스면) 챕터 완료 → 표시** |
| 사용자 확인 뒤 | 정리 → Run 끝 판정 → 새 Run → 전환 | (0행) Run 결과 읽기 → 새 Run → 전환 | **전환**(목적지 = Run 결과) |

### 4.4 코드

```diff
  // C/Plans/CombatPlan.cs Decide — 0행을 없애고 승패 줄을 맨 앞에
  internal static CombatStep Decide(in CombatProgressFacts facts)
  {
-     // 0. 보스는 최종 Run 여부를 알기 위해 먼저 정리한다. …
-     if (!facts.HasBattle)
-     {
-         if (CombatExitContext.RequiresChapterCompletion(facts.Completion) && facts.IsViewAttached && …)
-             return CombatStep.Of(CombatStepKind.ExitBattle);
-         return CombatStep.Nothing;
-     }
-
-     // 1~6. 승패가 기록됐다: 제출 정지 → 남은 연출 → 완료 표시 → 사용자 확인 → 정리·전환.
+     // 1~5. 승패가 기록됐다: 제출 정지 → 남은 연출 → 정리·표시 → 사용자 확인 → 떠나기. 모든 결과가 같은 순서다.
      if (facts.HasCompletion)
      {
          if (!facts.IsSubmissionSuspended)
              return CombatStep.Of(CombatStepKind.SuspendSubmissions);
          if (facts.IsPresenting || !facts.IsViewAttached)
              return CombatStep.Nothing;
          switch (facts.CompletionDisplay)
          {
              case CombatCompletionDisplay.None:
-                 return CombatStep.Of(CombatStepKind.PresentCompletion);
+                 // 정리는 전투가 있을 때 한 번만. 표시 없이 전투가 사라진 상태는 판단하지 않는다(보스 챕터 완료 두 번 방지).
+                 return facts.HasBattle ? CombatStep.Of(CombatStepKind.Settle) : CombatStep.Nothing;
              case CombatCompletionDisplay.Confirmed:
-                 return CombatStep.Of(CombatStepKind.ExitBattle);
+                 return CombatStep.Of(CombatStepKind.Leave);
              default:
                  return CombatStep.Nothing;
          }
      }
+
+     if (!facts.HasBattle)
+         return CombatStep.Nothing;
      … 포기·입력·자동 진행 그대로
  }
```

```diff
  // C/Plans/CombatPlan.cs BuildStep — 정리와 표시는 한 걸음(같은 Runner Tick에 이어 실행돼 그 사이 View Tick이 끼지 않는다)
- case CombatStepKind.PresentCompletion:
-     return PresentCompletion(service, lifeCycles, progress, facts.Completion);
- case CombatStepKind.ExitBattle:
-     return ExitBattle(service, lifeCycles, progress, in facts);
+ case CombatStepKind.Settle:
+ {
+     var jobs = new List<IJobService> { new CombatEndBattleJob(context.BattleCompletion, context.Progress, facts.LatestResultSequence) };
+     if (RequiresChapterCompletion(facts.Completion))       // 보스 승리 판정은 이 한 곳
+         jobs.Add(new CombatChapterCompletionJob(context.Chapters, facts.Completion));
+     jobs.Add(new CombatCompletionJob(context.Progress, context.View, context.Entry));
+     return jobs;
+ }
+ case CombatStepKind.Leave:
+     return new List<IJobService> { new CombatTransitionJob(context.Manager, DestinationOf(facts.RunOutcome)) };

+ /// <summary>Run이 이어지면 Stage, 끝났으면(패배·포기·최종 승리) 로비입니다(설계 J7). 원본은 이 한 곳입니다.</summary>
+ internal static LifeCycleKey DestinationOf(RunOutcome outcome) =>
+     outcome == RunOutcome.InProgress ? LifeCycleKeys.Stage : LifeCycleKeys.Lobby;
```
- `context`는 묶음 A의 좁은 걸음 재료다(§5). B를 먼저 하면 그때는 지금처럼 그룹에서 꺼낸다.
- 사실 묶음에 `RunOutcome`을 더한다. 사실 읽기가 Run 진행 읽기 싱크를 하나 더 받는다(`Bind` 서명이 바뀐다, §8).
- 지우는 것:
  - `Decide` 0행, `PresentCompletion`·`ExitBattle` 함수
  - `CombatCompletionCheckJob`, `CombatRunOutcomeJob`
  - `CombatNewRunJob`, `CombatPlan.NewRunLoadoutKey`(분석 §6 3번)
  - `CombatExitContext` 전체(Run 끝 판정 상태, 두 목적지, `Retained*`)
- `CombatRunEndJob` → `CombatChapterCompletionJob`: 보스 승리일 때만 등록되고, `CompleteChapter`를 한 번 부르며, 거절은 예외다. 아무것도 하지 않는 Job은 만들지 않는다.

### 4.5 View: 결과 유지 판정에 이름 (③ 결정)

```diff
  // CombatViewSystem.TickCore (289-294) — 보스 주석을 걷고 이름 있는 판정 하나로
- // 보스 결과는 EndBattle·챕터 완료 뒤 표시한다. 같은 완료의 확인을 기다리는 동안 마지막 화면만 유지한다.
- if (!_state.HasBattle && _battleInstance == _state.BattleInstanceId && _submissionsSuspended &&
-     _shownCompletion != null && ReferenceEquals(_shownCompletion, _state.Completion) &&
-     CompletionDisplay != CombatCompletionDisplay.None)
-     return;
+ // 모든 결과는 정리 뒤 표시한다. 이 전투의 완료를 보이는 동안 마지막 화면만 유지한다.
+ if (IsHoldingResult())
+     return;
```
**③ 세션 단계 = 골격이 따로 공개하지 않는다.** 바꾼 뒤 그 판정을 쓰는 곳은 View 안 한 곳뿐이라 이름 있는 판정 하나로 충분하다.

### 4.6 이탈 완료 (분석 §6 2번 ②)

```diff
  // C/CombatLifeCycle.cs OnExit
- if (!FaultBoundary.IsFaulted)
-     _register.Register(JobRootType.Combat, JobType.ExitCheck);
+ if (FaultBoundary.IsFaulted)
+     return;
+ // 정상 이탈이면 이 자리에서 요청을 닫는다. 도착 구간의 OnEnter 전에 "이탈 완료"가 참이다(OnEnter의 동기 활성화 J6와 같은 방식).
+ if (!_entry.TryCloseExited())
+     _register.Register(JobRootType.Combat, JobType.ExitCheck);   // 전투가 남은 이탈은 지금처럼 확인 Job이 F1로
```
- Stage 보류 장치(`StageLifeCycle.cs:114·130-145·149-163`)는 Runner 32개 예산을 넘는 경우에 대비한 것이다(`CombatEntryIntegrationTests.Stage.cs:488-500`). `ExitCheck`가 정상 이탈에서 없어지면 그 경우도 생기지 않으므로 걷는다.
- 다른 영역이지만 우리 변경에 묶인 곳이라 사용자 지시로 고친다(분석 §6 5번). 그 시험 4개(434·490·504·523)도 함께 정리한다.

### 4.7 보이는 차이 (독립 검토로 넓힘)

- **결과 화면 동안:** 일반 승리·패배·포기도 화면을 열기 전에 전투가 정리된다. 화면 뒤 전투 장면은 결과 유지 판정으로 그대로 남는다. 다만 `HasBattle`을 직접 보는 곳은 "전투 없음"으로 동작한다.
  - `NightCheckerUI.cs:57`: 현재 전투 정보가 null이 된다.
  - `TutorialViewTarget.cs:49`: 대상 찾기가 실패한다(결과 화면에서 튜토리얼이 대상을 찾는 경우에만 해당).
  - `MainCharactorUI`: 체력이 보유 캐릭터 값으로 바뀐다.
  - `BattleChangerUI.cs:52`·`PackUI.cs:90`: 이미 "제출 정지면 숨김"이라 차이가 없다.
  - 지금 보스 승리에서는 이 모두가 이미 이렇게 동작한다.
- **로비 출발 전까지:** 전투 쪽 새 Run을 빼므로(분석 §6 3번), 로비에 있는 동안 Core는 끝난 Run 상태다. `OwnedCharacterStatsReader`(`:40-60`)로 보유 로스터를 읽는 Global UI(`MainCharactorUI`·`UnitCardUI`)가 새 Run의 시작 로스터 대신 끝난 Run의 로스터를 본다.
  - 3번 결정 때 확인한 범위에서는, 로비가 보이는 것은 주인공 초상뿐이라 그림이 같다.
  - 재생 확인 항목으로 둔다(§11 Q3).

## 5. 묶음 A — 판단을 실행 순간으로 (Phase 2, 분석 §6 1번 결정을 검토 반영해 다듬음)

### 5.1 바꾸는 점 — 판단한 뒤 Runner에 넘긴다

> **대신함(2026-10-23, [골격 점검 설계](2026-10-23-combat-skeleton-check-design.md) K-1, 사용자 결정):** 단계 Job은 Runner에 넘기지 않고 `CombatNextJob` 안에서 차례로 실행한다(1판의 꼴로 돌아감). 아래 1판 지적 셋은 이렇게 됐다.
> - (b)는 좁은 걸음 재료 `CombatStepContext`가 그대로라 다시 생기지 않는다.
> - (c)는 "한 번 실행으로 끝나지 않으면 예외(F1)"로 정했다.
> - (a) "Runner의 연속 실행을 Job 안에서 다시 만든다"는 남는 맞바꿈이다. 10-23 독립 재평가는 반대쪽을 문제로 봤다. Job 안에서 Runner에 다시 넣는 것은 FW 중간, "같은 Tick에 이어 돈다" 가정은 독립성 중간이었다. 그래서 두 중간을 푸는 쪽을 골랐다.
> - 이 이력은 골격 점검 설계 §9에도 적었다.

1판은 `CombatNextJob`이 단계 Job을 **자기 안에서** 차례로 실행했다. 독립 검토의 지적은 셋이다.
- (a) Runner의 연속 실행을 Job 안에서 다시 만드는 셈이다.
- (b) 두 그룹을 통째로 들고 다녀 P26이 한 곳에서 다시 생긴다.
- (c) 단계 Job이 한 번에 안 끝날 때의 규칙이 없다.

2판은 **판단한 뒤 단계 Job을 Runner에 넘기고 자신은 끝난다.**

```csharp
// C/Jobs/CombatNextJob.cs (새 파일)
public void Execute()
{
    if (IsCompleted)
        return;

    // 실행 순간 판단한다. 넘긴 단계 Job은 같은 Runner Tick에서 이 Job 바로 뒤에 이어 실행되고, 그 사이에 다른 모듈은 돌지 않는다.
    _runner.RegisterRange(CombatPlan.BuildStep(_context));
    IsCompleted = true;
}
```

| 언제 | 지금 | 바꾼 뒤 |
|---|---|---|
| 프레임 N LifeCycle | Runner가 비면 `Register(Combat, Next)` → 사실 읽기 → `Decide` → 단계 Job 등록 | 같은 자리에서 `Decide`로 **할 일이 있는지만** 봄 → 있으면 `CombatNextJob` 하나 등록 |
| 프레임 N+1 Global(Render~UI) | 사실이 바뀔 수 있음, 판단은 이미 끝남 | 아직 판단 전 |
| 프레임 N+1 Global Job | 프레임 N 판단대로 실행 | `CombatNextJob`: 사실 읽기 → `Decide` → 단계 Job을 Runner에 넘김 → **같은 Tick에 Runner가 이어 실행** |

- **좁은 걸음 재료 `CombatStepContext`(가칭):** 사실 읽기, 골격이 보는 View 계약, 진입 서비스, 전환 서비스, Job이 쓰는 Core 서비스 여섯(전투 준비·명령·미리보기·자동 진행·완료·챕터 진행)만 담는다. Plan이 등록 때 그룹에서 한 번 꺼내 만든다. `CombatNextJob`과 `BuildStep`은 그룹을 받지 않는다(P26).
- **Runner 넘기기:** Combat Plan을 등록하는 JobRegister 줄(`JobRegisterSystem.cs:70`, 우리 연결 줄)이 Runner를 함께 넘긴다.
- **단계 Job 규칙:** 진행 단계의 Job은 한 번 실행으로 끝나거나 예외를 낸다(지금 12종이 모두 그렇다). `CombatStepKind` 설명에 적는다.
- **Tick 상한 32:** `CombatNextJob`은 Runner가 빌 때만 등록되므로, 다음 Tick에 첫 Job이고 단계 Job(최대 3개)은 상한 안에서 바로 이어진다. 다른 루트도 Runner가 빌 때만 등록하므로 같은 Tick에 앞에 쌓이지 않는다. 상한에 걸리는 경우는 생기지 않는 것으로 보고, 보조 점검의 위험으로 적는다.

### 5.2 함께 걷는 확인

B 뒤에는 보스 Job이 이미 없어서 남는 것은 우리 줄뿐이다.
- `CombatEndBattleJob` 앞쪽 "전투·완료가 이탈 대상과 같은가"(`C/Jobs/CombatExitJobs.cs:165-167`)
- `CombatPreviewJob`의 "그 사이 요청이 바뀌었으면 통과"(`C/Jobs/CombatPreviewJob.cs:47`)

**Core 보존 규칙 재증명 줄이기 (P15):** 사후 확인은 **다음 판단이 기대는 사실**만 본다.
- `CombatEndBattleJob`
  - 남김: 전투 없음, 결과 순번 S 그대로(다음 판단의 "남은 연출" 조건이 S·P에 기댄다)
  - 뺌: 최신 결과 참조 같음, 활성 캐릭터 0, P ≥ S
- `CombatAbandonJob`
  - 남김: 포기 완료 기록
  - 뺌: 결과·순번 보존

## 6. 묶음 C — 진입 (Phase 3)

### 6.1 착지 대기를 폴링 대신 사건으로 (P3)

**지금:** 진입 전환 Job이 착지 전까지 Runner를 여러 Tick 붙든다(`CombatEntryTransitionJob.cs:43-45`의 `Pending` → `return`, `JobRunnerSystem.cs:80-83`). 그동안 공용 Runner를 쓰는 모든 판단이 멈춘다(설계 J4 위반).

**바꾼 뒤:**
- 진입 연쇄는 지금처럼 [준비, 전환 시도]다.
- 전환 시도가 `Pending`이면 Runner를 붙들지 않고 **끝나며 요청에 "착지 대기"만 남긴다**. 직행 진입은 표시가 늘 준비돼 있어 바로 전환한다.
- 착지가 게시되면 서비스가 전환을 한 번 등록한다(`Register(Combat, JobType.EntryTransition)`).

```diff
  // C/Jobs/CombatEntryTransitionJob.cs
  MapCombatPresentationState presentation = _request.ReadMapPresentationState();
  if (presentation == MapCombatPresentationState.Pending)
-     return;                                   // Runner를 붙든다
+ {
+     _request.MarkAwaitingLanding();           // 착지 게시가 전환을 다시 등록한다
+     IsCompleted = true;
+     return;
+ }
  if (presentation != MapCombatPresentationState.Ready)
      throw …;
```
```diff
  // C/CombatEntryService.cs
  internal bool TryCompleteMapPresentation(MapCombatPresentationLease owner, CombatEntryRequest request)
  {
      … 지금 확인 그대로 → Ticket Ready
+     if (request.IsAwaitingLanding)
+         _register.Register(JobRootType.Combat, JobType.EntryTransition);
  }

  internal void ReleaseMapPresentationOwner(MapCombatPresentationLease owner)
  {
      …
+     // 착지를 기다리던 요청의 표시 소유권이 사라졌다. 전환 Job이 다시 오지 않으므로 여기서 실패로 보고한다(조용한 영구 대기 방지).
+     if (_current != null && _current.IsAwaitingLanding && _current.UsesPresentationOwner(owner))
+     {
+         _current.Fail();
+         _faults.Report(new InvalidOperationException("[CombatEntryService] 착지를 기다리던 전투 진입의 맵 표시 소유권을 잃었습니다."));
+     }
  }
```
- **(독립 검토 지적 반영) 소유권 상실 경로:**
  - 지금은 폴링 Job이 Ticket `Abandoned`를 보고 예외로 F1에 넘긴다.
  - 폴링이 없어지면 이 경로가 사라져 요청이 오류 없이 영구 대기할 수 있다.
  - 그래서 Lease를 풀 때 서비스가 직접 F1로 보고한다.
- **연결 줄:** `JobType.EntryTransition` 값을 하나 더한다. Combat Plan은 "현재 요청의 전환 Job 하나"를 만든다.
  - `IJobRegisterSystem` 인터페이스는 바꾸지 않는다(구현하는 시험 가짜가 14개다).
  - 독립 검토는 대안으로 Combat이 쓰지 않는 `JobType.Start`를 다시 쓰자고 했다. 그러면 "같은 값의 뜻이 루트마다 다름"(P19)이 나빠지므로 새 값을 쓴다(§11 Q-R2).
- StageMap 쪽 API(`IMapCombatPresentationOwnerSource`, `Lease.TryComplete`)는 그대로다.
- **다른 영역 확인(독립 검토 대조):**
  - Stage 흐름(`StageNodeEntryFlow.cs:73-77`, `StageFlow.cs:59-62`)과 덱 편집 창(`StageDeckEditWindow.cs:45`)은 요청 단계로 기다린다.
  - 다만 착지 대기 중 Runner가 비므로 **튜토리얼이 그 사이 Next를 걸 수 있다**(`TutorialViewSystem.cs:226-230`). 튜토리얼은 Combat 씬에서만 붙으므로(`:203`) StageMap 착지 대기 중에는 할 일이 없을 것으로 본다(추정). Phase 3 시작 때 대조한다.

### 6.2 진입 종류를 이름으로 (P22)

"맵 명령 번호 0 = 직행, 양수 = 맵" 해석을 요청의 `CombatEntryKind Kind { Direct, Map }` 하나로 바꾼다. 골격 안 7곳이 대상이다(`CombatEntryRequest.cs:56·63·70`, `CombatEntryService.cs:119·131·155`, `CombatPrepareJob.cs:41`).

### 6.3 요청 단계 (P9·P10·P11·P4)

- 단계 전이 표 하나를 요청 안에 둔다. `Close`도 표를 따른다. **`Fail`은 모든 비종료 단계에서 허용한다** — catch 블록들이 원래 예외를 가리지 않게 하기 위해서다(`CombatEntryService.cs:166-170·196-200`, 준비·전환 Job, `JobRegisterSystem.cs` 진입 등록).
- 이름 있는 질문을 더한다: `IsInProgress`(배출 전), `IsAwaitingLanding`, `HasExited`(= Closed). 우리 소비자는 바로 쓰고, Stage·StageMap 소비자에게는 정리 문서에 "이것으로 바꿀 수 있다"로 적는다.
- "이번 요청과 같은 대상인가"를 요청의 판정 하나(`IsTargetOf(chapter, node, stage)`)로 모은다. 활성화 확인, 완료 표시 확인, 맵 입력 대조, Ticket 소유 확인이 이것을 부른다.
- 진입 요청의 "Runner가 바쁘면 예외"를 계약 주석에 사전 조건으로 적고, Bootstrap은 부르기 전에 확인한다(P4).

## 7. 묶음 D·E·F

### 7.1 D — View 쪽 (Phase 4)

**D1 골격이 보는 계약 분리 (P25, P37 일부):**
- `ICombatViewService`가 `ICombatProgressView`를 상속하지 않고 `ICombatProgressView Progress { get; }`를 낸다. 골격은 이 작은 계약만 받는다.
- 이번에는 **계약만 나눈다.** `CombatViewSystem` 안의 클래스 분리는 하지 않는다.
- 독립 검토 지적: `_submissionsSuspended`를 읽는 곳이 여럿이고(`:151·799·976-977·1108-1111`), 정지의 부수효과(입력 해제·버튼 숨김, `:826-840`)가 조정자 안에 있다. 그대로 떼면 양쪽이 서로 부른다. 의존면을 먼저 정리한 뒤 따로 정한다(§11 Q4).

**D2 판정 함수 공개 (⑤, P16) — 원재료를 받는 함수로:**

튜토리얼 Plan의 재료는 JobRegister의 튜토리얼 줄(`JobRegisterSystem.cs:71`, 호진님 연결 줄)에서 정해진다. 거기에 사실 읽기를 넣으려면 다른 사람 연결 줄을 고쳐야 한다. 또 `CombatProgressReader.Read()`는 전투 View가 없으면 예외다(`:72-73`). 그래서 독립 검토 권고대로 **판정 규칙만 하나로** 한다. 사실 읽기는 그대로 둔다.

```diff
  // C/Plans/CombatPlan.cs, Decide 옆
+ /// <summary>
+ /// 입력 국면에서 남은 연출도 처리할 명령도 없어 사용자 명령을 기다리는 상태입니다. Decide가 이때 할 일을 고르지 않습니다.
+ /// 튜토리얼 설명 시작 조건이 이 함수 하나를 부릅니다(규칙 원본).
+ /// </summary>
+ internal static bool IsAwaitingPlayerCommand(CombatFlowPhase phase, int presentedResultSequence,
+     int latestResultSequence, bool hasUnhandledCommand) =>
+     phase == CombatFlowPhase.PlayerInput && presentedResultSequence >= latestResultSequence && !hasUnhandledCommand;
```
```diff
  // LifeCycles/Tutorial/Plans/TutorialPlan.cs (호진님 줄 — 변경 정리로 넘김)
- return IsAwaitingPlayerCommand(facts.Phase, …)
+ return CombatPlan.IsAwaitingPlayerCommand(facts.Phase, …)
  …
- internal static bool IsAwaitingPlayerCommand(…) { … }       // 사본 삭제
```
- 판정의 뜻은 지금 튜토리얼 것과 같다. 1판은 `!IsAbandonRequested`를 더해 튜토리얼 시작 조건을 바꿨는데, 2판은 그 조건을 더하지 않는다.
- 남는 것: 튜토리얼의 두 번째 사실 읽기(P16 일부, 낮음~중간).
- 입력 정책 종료 판정은 분석 §6 2번 ①(a)대로 코드 그대로 두고, 같은 뜻이라는 근거만 주석으로 단다.

**D3 제출 정지 — 1판 안을 바꿈 (P38):**
- 1판은 View의 자체 정지를 걷는 안이었다. 독립 검토가 둘을 지적했다.
  - 첫 관측(View Tick N)부터 Plan 정지(Global Job N+1)까지 입력 연결이 유지되는 틈이 생긴다.
  - 기존 시험 `CombatViewSystemTests.cs:611-634`가 깨진다.
- 2판은 **둘 다 남기고, 둘이 같은 판정을 부르게 한다.** View의 `SyncSubmissionState`와 Plan 1행이 같은 "승패 기록됨" 판정을 쓰고, 주석으로 서로를 가리킨다.

**D4 `IsPlaybackBusy` 정리 (P17):**
- 재생은 S = P+1일 때만 시작하고(`CombatViewSystem.cs:349-350`, `CombatViewRefresh.cs:42`), P 기록과 재생 끝은 같은 호출 안이다(`CombatViewSystem.cs:769-771`). 그래서 골격이 읽는 모든 시점에서 "재생 중이면 P < S"다.
- `ICombatProgressView.IsPlaybackBusy`와 사실 묶음의 칸을 지우고 `IsPresenting = HasUnpresentedResult`로 둔다.

### 7.2 E — 의존·조립·연결 (Phase 5)

- **E1 (P26):** 전투 Job 전부가 그룹 대신 Core 서비스 하나씩 받는다. 묶음 A의 `CombatStepContext`가 그 서비스들을 담는다.
- **E2 (P25):** 골격이 View를 얻는 곳은 `CombatStepContext`를 만들 때와 사실 읽기 PostBind 두 곳이다. 둘 다 D1의 작은 계약(`Combat.Progress`)을 받는다.
  - 1판의 "사실 읽기가 View를 내놓는다"는 버린다. 읽기 계약에 명령 대상이 섞이고, 호진님 시험 가짜 셋이 깨지기 때문이다(독립 검토).
  - 조립 순서(LifeCycle → InGame) 때문에 생성 때 넘길 수 없는 것은 기반 한계다.
- **E3 연결(분석 §6 4번 결정):**
  - `JobType.AdvanceCombat` 삭제. 우리 시험 두 줄은 같은 번호로 맞춘다.
  - `JobType.EntryTransition` 추가(묶음 C).
  - Factory 대체 생성 줄인 안: `LifeCycleSystemFactory.CreateComposition`·`InGameSystemFactory.Compose`는 경계 필수, 대체 생성은 시험용 입구로.
  - `MarkQueued`는 그대로.
- **E4 (P28):** Bootstrap의 씬별 UI·카메라 연결 노드를 이름 있는 표 하나로 모은다. 캐스트 둘은 그 표 안 한 곳에 두고, Framework가 씬마다 다시 연결하는 수단을 주지 않는다는 이유를 적는다(기반 한계, 수용).
- **E5 (P24, 독립 검토로 줄임):** 상수 한 곳으로 모으는 대신, **우리 파일의 숫자만** 이름 있는 상수로 만든다(Bootstrap −200·−190·90·200, View 100/−100은 이미 상수).
  - LifeCycle 0은 기반에 숫자로만 있고, 튜토리얼 ±110은 튜토리얼 파일에 있다. 다시 적으면 사본이 된다.
  - 전체 순서는 `runtime-scene-binding-guide.md` 표 한 곳에 적는다. 프로그래머 가이드 §10 씬 번호는 코드(`SceneIndices`)대로 고친다(사용자 지시).

### 7.3 F — 오류·작은 것·문서 (Phase 6)

- **F1 (P30):** Combat이 활성인 동안 씬 루트가 꺼지면 조용히 분리하지 않고 F1으로 보고한다. 런타임 종료·앱 종료 중은 제외한다(정상 이탈은 그 전에 구독을 푼다, `CombatViewSystem.cs:588`).
- **F2 (P34, 독립 검토로 줄임):** 죽은 칸은 B에서 `CombatExitContext`와 함께 없어진다. 닿지 않는 `if (IsCompleted) return;` 가드는 **남긴다**. Execute를 두 번 불러 Core 호출이 한 번인지 확인하는 시험이 6곳 있고(`CombatProgressJobTests.cs:65-66·141-142·197-198·365-366·405-406`, `CombatPlanTests.cs:56-57`), 얻는 것은 0.1점이다.
- **F3 (P35):** `NoJobs`는 Plan 서명(`List<IJobService>`, Global)이 정해져 있어 읽기 전용으로 못 바꾼다. "바꾸지 않는다" 주석을 두고 수용한다(기반 한계).
- **F4 (P12):** "같은 전투의 완료인가" 판정을 함수 하나(`CombatCompletionIdentity.IsSame`)로 모으고, Core와의 약속(완료 참조가 정리 뒤에도 같다)을 계약 주석에 적는다. 요청하지 않는다.
- **F5 (P23):** Bootstrap 주석과 CLAUDE.md의 "Core가 비전투 진입을 거절"을 "예외"로 고친다. 개발 진입 노드 보정은 개발 전용이라 그대로다.
- **F6 (P1):** 튜토리얼이 같은 프레임에서 전투보다 먼저 Runner를 잡는 것은 Tick 순서(InGame → LifeCycle)에서 온다. 동작은 그대로 두고, E5 순서 표에 의도된 규약으로 적는다.
- **문서 (P14):** 재구성 설계 J1·J3·J7, CLAUDE.md "Run 종료 복귀 정책" 줄.

## 8. 시험 영향 (독립 검토로 보완)

| 묶음 | 우리 시험 | 다른 사람 시험(먼저 물음) |
|---|---|---|
| B | `CombatPlanDecideTests`(0행·"전투 없으면 아무것도 안 함" 이름, `isPlaybackBusy:` 인자 `:143·152`는 D4), `CombatProgressJobTests`(이탈 연쇄·새 Run Job 셋), `CombatEntryIntegrationTests` 550·567·603(정리 전 `HasBattle` 참 단언)·626-660·763(`…StartsNewRun…`), `CombatProgressReaderTests:47·148·157-165`·`CombatProgressTestDriver:28`(`Bind` 서명) | 호진: `CombatEntryIntegrationTests.Stage.cs` 4(사용자 지시됨), `LobbyDepartureIntegrationTests`·`StageMapNodeMovementBootSmokeTests.Victory`(이탈 순서를 단언하는지 대조) |
| A | `CombatProgressJobTests`의 `Next_*` 두 메서드 호출 맞춤(허락됨), 걷는 확인의 시험(`EndBattle_OtherCompletionThanTheExit_…`, `Preview_…` 바뀜 사례, `EndBattle_BrokenCleanupOrRetention_Throws`의 `resultChanged`·`charactersRemain`·`unpresented`) | — |
| C | `CombatPlanTests`(진입 연쇄 모양), `CombatProgressJobTests:400-439`(진입 전환), `CombatEntryIntegrationTests` | 호진: `MapCombatPresentationTests`(대기 상태 24곳), `CombatEntryIntegrationTests.MapMovement.cs`, `StageMapNodeMovementTests` |
| D | `CombatProgressReaderTests:127`(`isPlaybackBusy:`), `ICombatViewService` 가짜 셋(`CombatEntryIntegrationTests.cs:1023`, `CombatProgressJobTests.cs:700`, `CombatProgressTestDriver.cs:76` — D1 `Progress`) | 호진: `LobbyDepartureIntegrationTests.cs:469`(D1 가짜), `TutorialPlanTests.cs:180`(`isPlaybackBusy:`), `TutorialPlanTests`의 `IsAwaitingPlayerCommand` 호출(D2) |
| E | Job 생성자 → `CombatProgressJobTests`·`CombatPlanTests` 생성 줄(약 30곳, 컴파일 맞춤), `CombatPlanTests` `AdvanceCombat` 두 줄 | — |
| F | — (F2 가드는 남겨 6곳 영향 없음) | — |

## 9. 적용 뒤 점수

### 9.1 점수 (2판 설계 기준)

| 기준 | 남는 옛 문제 | 개선이 새로 만드는 문제 | 감점 합 | 점수 |
|---|---|---|---|---|
| 1 SSOT | P16 튜토리얼 두 번째 사실 읽기(0.4), P5 우리 쪽에 남는 요청 단계·View 연결(0.2), P24 기반·튜토리얼 숫자(0.2), P23 개발 진입 탐색(0.2), P38 정지 둘(같은 판정, 0.1) | — | 1.1 | **8.9** |
| 2 SOLID | P37 `CombatViewSystem` 클래스는 그대로(0.6), P25 그룹에서 찾는 방식(타입은 좁아짐, 0.2) | — | 0.8 | **9.2** |
| 3 FW | P28 캐스트 둘(한 곳·이유 명시, 0.2), P29 뿌리·View Factory 시험 편의 대체 생성(0.2), P35 바꿀 수 있는 정적 목록(0.1) | — (C의 영구 대기는 소유권 상실 보고로 막음) | 0.5 | **9.5** |
| 4 있는 기능 | P12 완료 정체성은 여전히 참조 동일성(함수 하나·계약 명시, 0.2) | — (`CombatNextJob`은 Runner에 넘김) | 0.2 | **9.8** |
| 5 독립성 | P7 Stage가 전환 순서를 추론(④를 안 냄, 0.4), P10 Stage·StageMap 단계 해석(우리 몫 0.2), P1 Tick 순서 규약(0.2) | N1 끝난 Run 상태가 로비 출발 전까지 Global UI에 보임(0.2) | 1.0 | **9.0** |
| 6 불필요·우회 | P3 Lease·Ticket 객체는 남음(0.2), P34 닿지 않는 가드(0.1) | N2 등록 때·실행 때 판단 두 번(0.1), N3 전환 Job의 닿지 않는 불변식 예외(0.05) | 0.45 | **9.6** |
| **종합** | | | | **9.3** |

- D1 안 클래스 분리까지 하면(§11 Q4) P37이 0.2로 줄어 SOLID 9.6, 종합 9.4가 된다.
- 남는 기반 한계: Runner 완료 신호·등록 결과 없음(G1), Global JobRegister 구조(G5), Core의 Run 종료 확정 시점(P20 원인), 조립 순서(P25), 씬별 재연결 수단 없음(P28), Plan 서명(P35).
- 남는 다른 영역: Stage의 전환·단계 해석(P7·P10 소비자), P8·P27, Lobby 진입 조건, Stage·Lobby 생성자 대체 생성(P29).

### 9.2 1판 점수와의 차이

1판 자체 평가 9.5 → 2판 9.3. 내려간 이유는 셋이다.
- 독립 검토가 지적한 "개선이 새로 만드는 문제"를 세었다(N1~N3).
- D2·D3·E5·F2를 위험이 작은 쪽으로 줄여서 남는 옛 문제가 늘었다(P16 0.4, P38 0.1, P24 0.2, P34 0.1).
- C의 영구 대기는 막았다.

### 9.3 보조 점검 (점수 밖)

| 묶음 | 정확성·견고성 | 범위·소유권 | 검증 위험 |
|---|---|---|---|
| B | 정리는 전투가 있을 때 한 번(Settle 조건), 보스 챕터 완료 두 번 방지 | 호진님 줄: `CombatPlan` 보스 줄, 보스 Job 둘, View 유지 분기. Stage 보류 장치·시험 4개(사용자 지시) | **높음** — 보스 경로는 실제 View 시험이 없다(P31). 재생 확인 필수: 일반 승리 → Stage, 패배·포기 → 로비, 비최종 보스 → Stage, 최종 보스 → 결과 요약 → 로비 |
| A | 단계 Job은 한 번에 끝남(규칙 명시), 상한 32 위험은 이론상만 | 우리 줄 + JobRegister Combat 줄(연결) | 중간 — 판단 시점만 바뀌고 순서는 같음 |
| C | 소유권 상실 → F1, 중복 등록은 요청 단계로 막음, 거절·취소는 지금처럼 전환 없이 끝남 | 호진님 줄: Lease·Ticket·전환 Job·준비 Job 맵 분기. 연결: `JobType.EntryTransition` | **중간~높음** — StageMap·Stage와의 상호작용, 호진님 시험 셋 |
| D | 정지 둘이 같은 판정 | 호진님 줄: `TutorialPlan` 호출 한 줄. 현강·천명님 줄: `ICombatViewService` | 낮음~중간 |
| E | — | 연결: `JobType`·Factory 두 조립. 우리 시험 생성 줄 약 30곳 | 낮음(기계적) |
| F | F1 보고에서 앱 종료 중 제외 | 민형님 문서(가이드 §10, 사용자 지시) | 낮음 |

- C#/Unity: 전투 Job이 가변 struct이고 `IJobService`로 박싱해야 상태가 유지된다. Framework 관례("Job은 struct", architecture 문서)라 수용한다. 새 `CombatNextJob`은 struct로 맞춘다.
- Tick 할당: 할 일 없는 프레임은 지금처럼 0. 할 일 있는 걸음마다 걸음 재료 하나·목록 하나를 만든다.
- 문서 정확성: P14(J3 전제)는 F 문서 항목에서 고친다.

## 10. 독립 검토

### 10.1 결과 요약

- 사실 주장은 대부분 맞다고 확인됐다. §4 판단·실행 시점, Runner 위치, §5.2 정리 뒤 남는 값, §6.1 Runner 점유, D4.
- 틀리거나 빠진 것:
  - D3의 한 프레임 틈
  - §8 시험 목록(여러 곳 누락)
  - D4 인용 줄(시작 쪽 파일을 인용함)
  - 진입 종류 해석 수(9곳 → 7곳)
  - P13 "늘 참"은 지금 코드에는 과한 표현
- 설계 문제 12개(중간 4, 낮음~중간 3, 낮음 5). 가장 큰 것은 C의 소유권 상실 시 영구 대기.
- 독립 점수: **지금 7.6, 1판 적용 뒤 9.1.** 권고를 반영하면 약 9.3.

### 10.2 반영 여부

| 검토 지적 | 반영 |
|---|---|
| C 소유권 상실 → 영구 대기 | 반영(§6.1, Lease 해제 때 F1 보고) |
| 범위 모순(`JobType`·Factory) | **반영 안 함.** CLAUDE.md가 연결 줄 변경을 허용한다(§1.1). `JobType.Start`를 다시 쓰는 대안은 P19를 나쁘게 해서 새 값을 쓴다. §11 Q-R2로 확인 |
| A 설계 공백(미완료 규칙·그룹·Runner 재구현) | 반영(§5.1, 판단 뒤 Runner에 넘김 + 좁은 걸음 재료 + 규칙 명시) |
| A가 B의 바탕이라는 것은 과장, B 먼저 | 반영(§3 순서) |
| D3 틈·시험 깨짐 | 반영(§7.1 D3, 둘 다 남기고 같은 판정) |
| Settle에 `HasBattle` 조건 | 반영(§4.4) |
| 보이는 차이 목록 누락 | 반영(§4.7, §11 Q3 넓힘) |
| D2 연결 경로가 범위 안에서 성립 안 함, `!IsAbandonRequested`로 뜻 바뀜 | 반영(§7.1 D2, 원재료 판정 함수, 조건 추가 안 함) |
| F2 가드 제거가 시험 6곳을 깸 | 반영(§7.3 F2, 남김) |
| C 세부(준비 Job에 서비스 참조 없음, `Fail` 허용 단계) | 반영(§6.1 [준비, 전환 시도] 유지, §6.3) |
| E2 읽기 계약이 넓어짐 | 반영(§7.2 E2) |
| E5 상수 사본 | 반영(§7.2 E5, 우리 숫자만) |
| D1 양방향 콜백 | 반영(§7.1 D1, 계약만 먼저) |
| 평가 기준: 근본 원인 상한, 결과로 정의한 심각도, 새 문제 칸, 보조 점검, P14는 FW가 아님, 심각도 조정 | 반영(§1.4·§1.5·§2) |
| 놓친 문제: 가변 struct Job, 사실 읽기가 View 없이 PostBind를 받아들이고 `Read()`에서 늦게 실패 | 가변 struct는 보조 점검(§9.3, Framework 관례로 수용). 늦은 실패는 낮음으로 보고 Phase 4(D1)에서 PostBind 때 계약 존재를 확인할지 정한다 |

## 11. 정할 것

**결정(2026-10-22 사용자, "Q1 지우고, Q2 해도 되고, 추천대로 진행"):**
- Q1 지운다 — 걷는 확인·기능, 바뀌어서 더는 성립하지 않는 동작을 확인하던 **우리** 시험.
- Q2 허락 — 다른 사람 시험(주로 호진님)을 우리 변경에 맞게 고친다. 바꾼 시험은 변경 정리에 적는다.
- Q3 A 받아들임 — 밤 진행 `0/0`·체력 숫자는 재생 확인 목록에 둔다.
- Q4 A 계약만 — 클래스 분리는 Phase 1~6 뒤 따로 계획.
- Q5 이번에 한다.
- Q-R2 ① `JobType.EntryTransition` 새 값, ② JobRegister Combat 줄이 Runner를 넘김(필수 인자).
- Task 계획: [improvement-plan](../plans/2026-10-22-combat-view-structure-improvement-plan.md).

- **Q1** 걷는 확인·기능을 시험하던 **우리** 시험(§8의 A·B 행)은 확인 내용이 사라지므로 지운다. 승인 여부.
- **Q2** 다른 사람 시험(§8 오른쪽)을 우리 변경에 맞게 고쳐도 되는지. 호진님 Stage 시험 4개는 이미 지시됨. 컴파일만 깨지는 곳(`isPlaybackBusy:` 인자, D1 가짜)도 함께.
- **Q3** 보이는 차이(§4.7) 허용 여부.
  - 결과 화면 동안 `NightCheckerUI`·`TutorialViewTarget`·`MainCharactorUI` 체력이 "전투 없음"으로 동작한다.
  - 로비 출발 전까지 Global UI가 끝난 Run의 로스터를 본다.
- **Q4** D1의 `CombatViewSystem` 안 클래스 분리를 이번에 할지(권장: 계약만 먼저, 클래스 분리는 의존면 정리 뒤 별도).
- **Q5** 묶음 C(착지 대기 사건화)를 이번에 할지. Stage·튜토리얼 동작 대조는 Phase 3 시작 때 다시 한다.
- **Q-R2** 연결 줄 셋 확인.
  - `JobType.EntryTransition` 추가(새 결정)
  - JobRegister의 Combat Plan 등록 줄이 Runner를 함께 넘기는 것(새 결정)
  - 이미 정한 것: `JobType.AdvanceCombat` 삭제·Factory 줄인 안(분석 §6 4번)
