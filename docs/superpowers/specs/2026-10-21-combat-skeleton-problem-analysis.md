# 전투 골격 문제점 분석 (2026-10-21, 개선안 작성 전)

- 작성: View 작업, 2026-10-21. 사용자 요청: "개선안을 작성할건데 개선안을 쓰기 전에 현재 구조에서의 문제 점을 찾는거부터 진행하자, 에이전트 3개 같이 돌려".
- **범위 기준 반영판(10-21 사용자 지시, 10-22 정정):** 처음 지시 "어디까지나 작업 영역은 전투 View로 한정하고, 다른 사람의 작업물은 수정하지 않는다가 기본. 특히 Framework 및 GlobalSystem은 건드리면 안 됨", 보충 "GlobalSystem 안에서 우리가 수정할 수 있는 방향은 View 측에서 Framework나 GlobalSystem을 이용하기 위해서 해야 하는 작업 정도". 10-21 판은 이것을 `git blame` 작성자로 나눴는데 **잘못 읽은 것이었다.** 10-22 사용자 정정: "Framework 및 GlobalSystem으로 짜여 있는 것들과 Core, 사실상 게임 내의 계산기를 이용해서 View 측에서 최적으로 구조를 구상해야 한다. 우리가 쓴 줄이 아니더라도 그게 Combat 씬 내의 전투 View 구조라면 포함해야 한다." → §0을 **영역 기준(작성자 무관)**으로 다시 쓰고 §1·§2를 다시 나눴다.
- **문제만 적는다. 코드 변경 없음, 개선 방향은 다음 문서에서 정한다.** 항목의 "더 단순한 모양"은 에이전트가 적은 힌트이며 결정이 아니다.
- 기준 코드: HEAD `067b53ad`. 범위는 [흐름 지도](https://claude.ai/artifact/4sFcucyiPtQX5UzpTNxvA7)·[UML](https://claude.ai/artifact/3QmuZYMTBHP3XPf3H7ZQ8a)과 같은 `.cs` 148개.
- 방법: 읽기 전용 에이전트 셋을 관점별로 동시에 돌렸다 — A(SSOT·변경 독립성), B(SOLID·FW 계약·기존 기능 재사용), C(불필요·우회·수명·정확성). 세 보고를 합치고, 영향이 큰 근거는 직접 코드와 대조했다(§3 "대조" 칸). 작성자는 `git blame -w`의 현재 줄 기준이다(이름 대응: Droute·choi yong geun = 용근, Leehojin·CCRC_Hojin = 호진, GwonCheonmyeong = 천명, JMH·RKFK = 민형, Twinsgm = 현강).
- 선행: [골격 원칙 평가](2026-10-21-combat-skeleton-principles-evaluation.md)(SK1~SK8, 종합 7.3, 개선 묶음 A~D 반영 시 8.7). 사용자 판단 "개선하고도 8.7이면 안 된다"에 따라 SK 증상 아래의 **원인**을 찾았다. 그 평가의 묶음 A~D 중 Framework·Core·다른 씬을 고치는 부분만 빼면, 전투 View 구조 안의 것은 다시 쓸 수 있다(§4, 개선안에서 다시 묶음).

## 0. 작업 범위 기준 (영역 기준, 2026-10-22)

**목표:** Framework·GlobalSystem·Core(계산기)는 주어진 기반으로 두고, 그 위에서 **Combat 씬의 전투 View 구조**를 가장 좋게 짠다. 구조 안의 코드는 작성자가 누구든 범위 안이고 평가 점수에도 들어간다. 범위 안의 다른 사람 줄을 고치면 변경 사항을 정리해 그 사람에게 넘긴다. 기반은 기능·계약을 바꾸지 않고, 우리 구조가 그것을 이용하기 위한 **연결 줄**만 고친다(바꾸면 민형님 공유 문서).

| 구분 | 위치 | 파일 | 처리 |
|---|---|---|---|
| **범위 안 — 전투 골격** | `LifeCycleSystem/LifeCycles/Combat/` | 18 | 전부. 호진님 보스 선정리(`CombatPlan` 0행·보스 분기·`CombatCompletionCheckJob`·`CombatRunOutcomeJob`)·착지 대기(`MapCombatPresentationLease`·`Ticket`, `CombatEntryTransitionJob` 대기 분기)·맵 진입 분기(`CombatPrepareJob`), 민형님 원래 줄 포함 |
| **범위 안 — 전투 View** | `InGameSystem/ViewSystem/Combat/`, `BufferSystems/CombatView/CombatViewBuffer.cs` | 16 | 전부. `ICombatViewService`의 천명·현강님 줄, `CombatViewSystem` 보스 유지 분기의 호진님 줄 포함 |
| **범위 안 — 전투 결과 화면** | `GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs` | 1 (+) | 폴더는 GlobalSystem이지만 Combat 씬 화면이라 포함(10-22 사용자 결정). UI 시스템(`UISystem`) 자체는 기반 |
| **범위 안 — 튜토리얼의 전투 접점** | `LifeCycles/Tutorial/TutorialFactsReader.cs`, `Plans/TutorialPlan.cs`(전투 사실 판정·`NoJobs`), `ViewSystem/Tutorial/TutorialViewSystem.cs`(Runner 선점) | 3 (+, 부분) | 전투 사실을 읽는 곳과 Runner 선점만(10-22 사용자 결정). 튜토리얼 자체 흐름(설명·실습)은 범위 밖 |
| **범위 안 — 부트·개발 진입, 공용 오류 경계** | `ViewSystem/Boot/`·`Dev/`, `Editor/Boot/`, `RuntimeSystem/Common/` | 9 + 2 | 전부. 오류 경계는 Stage·Lobby도 쓰므로 계약을 바꾸면 공유 |
| 기반 — Framework | `Script/Framework/` | 5 | 그대로. 그 위에서 구조로 흡수 |
| 기반 — GlobalSystem | `GlobalSystem/JobSystem`·`LoadingSystem`, `OptionUI` | 11 | 기능·계약은 그대로, **연결 줄만**(Combat Plan 등록·`RegisterCombatEntry`, `JobType` 전투 값). `OptionUI`(현강)는 공용 옵션 화면이라 기반(전투 포기 연결이 접점) |
| 기반 — LifeCycle 기반·조립·키 | `LifeCycleSystem/Base/`, `LifeCycleSystemGroup`, `Factory/`, `ServiceKeys/` | 5 + 15 | 그대로, **연결 줄만**(그룹 노출, `SceneIndices`, Factory에서 우리 시스템·오류 경계를 조립하는 줄) |
| 기반 — Core·Buffer | `CoreSystem`, `BufferSystems`(CombatView 제외), InGame 그룹 | 30 | Core는 계산기로 주어진 것. 요청하지 않고 바뀐 점만 정리해 Core 전달 문서에(§6 5번) |
| 다른 영역 — Stage·Lobby·StageMap | `LifeCycles/Stage`·`Lobby`, `ViewSystem/StageMap`, `BufferSystems/LobbyView` | 17 | 고치지 않음. 우리 변경에 묶인 곳만 사용자 지시로(§6 5번: Stage 보류 장치) |
| 다른 영역 — View 공용 | `InGameViewSystemGroup`, `InGameViewFaultCleanup` | 2 | 고치지 않음 |
| 시험 | `Assets/Tests/EditMode/Framework/` | 18 | 새 `[Test]` 금지. 컴파일이 깨지면 최소로, 실행이 깨지면 우리가 쓴 시험은 확인 내용 그대로 호출만 맞추고 다른 사람 시험은 먼저 묻는다(CLAUDE.md) |

(+) = 흐름 지도의 148개 밖에서 새로 넣은 파일(4개). 범위 안은 49개다.

처리 구분의 뜻:
- **범위 안** — 전투 View 구조 안에서 끝난다. 다른 사람 줄이면 변경 정리를 넘긴다.
- **범위 안+기반** — 범위 안에서 고치되 원인 일부가 기반(Framework·GlobalSystem·Core)에 있다. 그 부분은 주어진 것으로 두고 구조로 흡수한다.
- **범위 안+다른 영역** — 범위 안을 고치면 줄고, 남는 부분이 다른 씬(Stage·Lobby·StageMap)에 있다. 그 부분은 고치지 않고 정리 문서에 적는다(우리 변경에 묶인 곳은 사용자 지시로).
- **연결** — 기반 파일 안의 우리 연결 줄. 고치고 민형님 공유 문서에 적는다.
- **기반 제약** — 기반의 기능·계약 자체. 그대로 두고 구조로 흡수한다.
- **다른 영역** — 다른 씬의 구조. 고치지 않고 정리 문서에만.
- **규칙 제약** — 새 시험 금지 때문에 메울 수 없다.

## 1. 결론 (영역 기준)

**36개 중 범위 안 몫이 있는 것은 26개(범위 안 11, 범위 안+기반 5, 범위 안+다른 영역 10)이고, 연결 2개를 더하면 28개다. 범위 밖만 남는 것은 8개(기반 제약 4, 다른 영역 2, 규칙 제약 2)다.**

| 원인 | 범위 판정 | 범위 안에서 할 것 | 범위 밖에 남는 것 |
|---|---|---|---|
| G1 실행 조정자·완료 신호 없음 | 기반 제약 + 범위 안 | 진입 요청 단계 단순화(P10), 착지 대기가 Runner를 붙들지 않게(P3 — Lease·Ticket·진입 전환 대기 분기는 골격 폴더), 튜토리얼 Runner 선점 정리(P1 접점), 바쁨 예외 계약(P4) | Runner·`IsIdle`·등록 결과 없음(Framework) |
| G2 세션 수명·전환 허가의 주인 없음 | 범위 안 + 다른 영역 | 이탈 완료를 나가는 순간의 사실로(P6, Stage 보류 장치는 §6 5번으로 직접), 세션·전환 사실(P5·P7), 요청 단계 검사·같은 대상 검증 하나로(P9·P11) | LifeCycle·Manager·Core 표현(기반), Lobby 진입 조건·Stage Flow 모델(다른 영역) |
| G3 판단·실행 분리 | 범위 안 | 실행 순간 판단(§6 1번), 보스 Job 재확인까지 정리(P13·P15) | — |
| G4 결정 대신 재료 공개 | 범위 안 + 다른 영역 | 입력 정책·View·결과 화면·튜토리얼 접점이 골격 결정을 쓰게(P16), `IsPlaybackBusy`(P17) | Lobby 직접 읽기(다른 영역) |
| G5 Global JobRegister가 루트 지식 보관 | 기반 제약 + 연결 | `AdvanceCombat` 정리(P19, §6 4번) | Plan 표 구조·등록 계약(Framework) |
| G6 규칙 원본이 흩어짐 | 범위 안 + 기반 | Core의 Run 종료 확정 비대칭(P20)은 주어진 것으로 두고 **보스 경로·결과 화면을 View 쪽에서 가장 단순한 구조로**(SK1), 전투 이탈 새 Run(P21, §6 3번), 진입 종류 이름(P22), 개발 진입·문구(P23), 순서 규약 이름·가이드 §10(P24) | Core 계약 자체(기반), Stage 노드 판정(다른 영역) |
| G7 의존 범위·조립 경계 | 범위 안 + 연결 + 다른 영역 | 전투 Job이 Core 서비스 하나씩(P26, 보스 Job 포함), View를 얻는 곳 하나(P25), Bootstrap(P28), Factory 대체 생성 줄인 안(P29) | View 그룹 타입·조립 순서(기반), StageLifeCycle·Stage/Lobby 생성자(다른 영역) |
| G8 대기·오류 빈틈 | 범위 안 + 규칙 제약 | View 분리 대기를 오류로(P30), 보스 경로를 단순하게 만들어 시험 없는 위험 줄이기(P31) | 새 시험 금지(P31·P33), Runner 오류 정책(기반) |

- **범위 안 지렛대는 G3·G4·G2, 그리고 G6의 보스 경로(SK1)다.** SK1·SK4 코드가 골격 폴더·전투 View·결과 화면 안이라 범위 안이다. P20(Core가 보스 승리의 Run 종료를 `CompleteChapter` 뒤에 확정)은 바꾸지 않는 전제로, 그 위에서 이탈·표시 순서를 어떻게 짜는 것이 가장 단순한지를 개선안에서 정한다.
- **G1·G5는 Framework 제약이다.** 그 위에서 덜 흔들리게(Runner를 붙드는 대기 없음, 판단 한 곳) 만드는 것이 최대다.
- **목표 점수:** 기반을 주어진 것으로 두고 범위 안(전투 View 구조, 작성자 무관)을 6기준으로 매긴다(§6 7번).

## 2. 문제별 재분류 (영역 기준)

| ID | 문제(짧게) | 원인 | 구분 | 범위 안에서 할 것 | 범위 밖에 남는 것 |
|---|---|---|---|---|---|
| P1 | `IsIdle`이 루트 사이 유일한 조정 | G1 | 범위 안+기반 | 튜토리얼이 전투보다 먼저 Runner를 잡는 접점 정리 | Runner(Framework) |
| P2 | Job 등록이 결과를 안 돌려줌 → 요청마다 상태 기계 | G1 | 기반 제약 | (P10으로 단순화) | 등록 계약(Framework), 로비 출발·회복·보드 준비의 상태 기계(다른 영역) |
| P3 | 착지 대기 Job이 Runner를 여러 Tick 붙듦(J4 위반), 신호가 다섯 객체 경유 | G1 | 범위 안+다른 영역 | 진입 전환 Job 대기 분기·Lease·Ticket(골격 폴더) 정리 | StageMap 이동 쪽 신호(StageMap) |
| P4 | 진입 서비스의 바쁨 예외가 계약에 없음 | G1 | 범위 안 | 계약 주석, Bootstrap 호출 전 확인 | — |
| P5 | 전투 세션 표현 여섯이 서로 검사 | G2 | 범위 안+기반 | 골격이 세션 단계를 한 곳에서(쓰는 곳이 범위 안에 있으면, §6 2번 ③) | LifeCycle·Manager·Core 표현(기반), Stage 검사(다른 영역) |
| P6 | 이탈 완료 사실 없음 → 목적지마다 추론 | G2 | 범위 안+다른 영역 | 나가는 순간 요청 닫기(§6 2번 ②), Stage 보류 장치 정리(§6 5번, 사용자 지시) | Lobby 진입 조건(다른 영역) |
| P7 | 전환 허가 없음 → Stage가 Manager 내부 순서로 추론 | G2 | 범위 안+다른 영역 | 우리 전환 Job, "수락된 전환 중" 사실 | Stage 추론·Lobby Flow `Change`(다른 영역), Manager(기반) |
| P8 | 진입 요청이 두 진행 모델 사이 어댑터 | G2 | 다른 영역 | (P10) | Stage·Lobby Flow 모델 |
| P9 | 요청 상태를 6클래스·3층이 바꿈 | G2 | 범위 안+다른 영역 | `Close`·`Fail` 단계 검사, Lease·Ticket 전이 정리(골격 폴더). `RegisterCombatEntry`는 그대로(§6 4번 ③) | Stage 줄(다른 영역) |
| P10 | 볼 수 없는 단계·늘 참인 조건을 소비자가 분기 | G2 | 범위 안+다른 영역 | 요청에 이름 있는 질문 | Stage·StageMap 소비자(다른 영역) |
| P11 | "같은 대상" 검증 사본 6곳 | G2 | 범위 안 | 진입·활성화·완료·맵 표시 검증을 판정 하나로(Ticket 포함) | — |
| P12 | "같은 전투" = 완료 객체 참조 동일성(암묵 계약) | G2 | 범위 안+기반 | 우리 Job·View의 기대를 계약 문서로 | Core 계약(기반, 요청 안 함) |
| P13 | 판단·실행이 프레임으로 갈라져 Job이 조건 재증명 | G3 | 범위 안 | 실행 순간 판단(§6 1번), 보스 Job 재확인 걷기 | — |
| P14 | 설계 J3 전제가 실제 Tick 순서와 다름 | G3 | 범위 안 | 설계 문서 | — |
| P15 | Job이 Core 내부 보존 규칙까지 재증명 | G3 | 범위 안 | EndBattle·포기·새 Run·보스 Job의 확인 범위 줄이기 | — |
| P16 | Decide 결과를 버려 소비자가 다시 만듦 | G4 | 범위 안+다른 영역 | 입력 정책·View·결과 화면·튜토리얼 접점이 골격 결정을 씀 | Lobby 직접 읽기(다른 영역) |
| P17 | `IsPlaybackBusy`가 정보 없음 | G4 | 범위 안 | View 계약 정리 | — |
| P18 | Global JobRegister가 Plan 표·루트 의존 보관, 대상 전달 방식 넷 | G5 | 기반 제약 | (연결 줄 그대로) | 등록 구조(Framework), Lobby·Stage 전달(다른 영역) |
| P19 | 공용 `JobType` 뜻이 루트마다 다름 | G5 | 연결 | `AdvanceCombat` 삭제(§6 4번 ①) | 공용 열거형 구조(Framework) |
| P20 | Run 종료 확정 시점 비대칭이 보스 순서를 강제(SK1 근본) | G6 | 범위 안+기반 | 보스 선정리 경로(0행·보스 Job·View 유지 분기)와 결과 화면 선택을 View 쪽에서 가장 단순한 구조로 | Core 확정 시점(기반, 주어진 것) |
| P21 | Run 수명 주인 없음(시작 넷·판단 셋) | G6 | 범위 안+다른 영역 | 전투 이탈 새 Run 빼기(§6 3번), 개발 진입 | Stage·Lobby 시작(다른 영역) |
| P22 | 진입 경로 종류가 "번호 0/양수" 값 | G6 | 범위 안+다른 영역 | 요청·서비스·준비 Job 맵 분기의 진입 종류를 이름 있는 값으로 | StageMap(다른 영역) |
| P23 | 노드 전투 판정 셋 + Core 예외(F1) | G6 | 범위 안+다른 영역 | Bootstrap 개발 탐색, "거절" 문구 고침 | Core 예외(기반), Stage 판정(다른 영역) |
| P24 | 실행 순서 규약이 정수·주석에 흩어짐, 문서 낡음 | G6 | 범위 안+다른 영역 | View·튜토리얼·Bootstrap Node에 이름, `runtime-scene-binding-guide.md`, 프로그래머 가이드 §10(§6 5번, 사용자 지시로 직접) | LifeCycleBase 0(기반), Stage Node(다른 영역) |
| P25 | 골격이 View 전체 계약을 그룹에서 꺼냄 | G7 | 범위 안+기반 | View를 얻는 곳을 Reader 하나로(§6 4번) | View 그룹 타입·조립 순서(기반) |
| P26 | Job이 InGame 그룹 전체를 받음(FW-101) | G7 | 범위 안 | 전투 Job 전부(보스 Job 포함)가 Core 서비스 하나씩 | — |
| P27 | StageLifeCycle 역할 셋(307줄) | G7 | 다른 영역 | — | Stage |
| P28 | Bootstrap이 캐스트로 수명 단계 호출·수동 PostBind | G7 | 범위 안 | UI 수명 연결 캐스트 정리 검토, 개발 소스 어댑터(Framework 대체 수단 없음 → 수용) | — |
| P29 | 오류 경계 대체 생성 일곱 곳 | G7 | 연결 | Factory 줄인 안(§6 4번 ②) | Stage·Lobby·LobbyView 생성자(다른 영역) |
| P30 | View 분리 시 오류 없는 영구 대기 | G8 | 범위 안 | 활성 중 Detach를 오류로 보고 | — |
| P31 | 보스 경로 실제 View 시험 없음 | G8 | 규칙 제약 | (보스 경로를 단순하게 만들어 위험 줄이기, P20) | 새 시험 금지 |
| P32 | 오류 정책 셋, Runner가 실패 Job 보유 | G8 | 기반 제약 | — | Runner(Framework), Stage·Lobby(다른 영역) |
| P33 | 운영 조립 Tick 순서를 고정하는 시험 없음 | G8 | 규칙 제약 | — | 새 시험 금지 |
| P34 | 죽은 칸(Retained, 목적지 인자, 우선순위 0, 닿지 않는 가드) | 작은 것 | 범위 안 | 정리 | — |
| P35 | 공유 빈 목록 `NoJobs`가 가변 정적 필드 | 작은 것 | 범위 안 | `CombatPlan`·`TutorialPlan`(접점) | — |
| P36 | Shutdown 중 `OnExit`가 ExitCheck 등록 | 작은 것 | 기반 제약 | (수용) | `LifeCycleBase.Shutdown` 순서(기반) |

## 3. 원인별 문제 (근거)

근거 표다. 각 항목의 범위 구분은 §2에 있다.

표기: 출처 = 에이전트 보고 번호(A·B·C). 대조 = 제가 직접 코드로 확인한 것(✔) / 에이전트 근거만 있는 것(–). 경로 약어: `C/` = `LifeCycleSystem/LifeCycles/Combat/`, `S/` = `…/Stage/`, `L/` = `…/Lobby/`, `V/` = `InGameSystem/ViewSystem/`, `JOB/` = `GlobalSystem/JobSystem/`(모두 `Assets/ProjectL/Script/Systems/RuntimeSystem/` 아래).

### G1 실행 조정자와 완료 신호가 없다

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P1 | **`IsIdle` 하나가 루트 사이 유일한 조정 수단.** Runner는 루트별 소유·취소·우선을 모르고, 14곳(10파일)이 "비면 내 일을 건다"를 따로 구현한다. 같은 프레임 경쟁은 그룹 Tick 순서가 정한다 — 튜토리얼(InGame Tick)이 전투(LifeCycle Tick)보다 늘 먼저 Runner를 잡는다 | B1·A3·C1 | 높음 | `JOB/JobRunner/JobRunnerSystem.cs:41`, `V/Tutorial/TutorialViewSystem.cs:211-231`, `C/CombatLifeCycle.cs:54-55` | ✔ |
| P2 | **Job 등록이 결과를 돌려주지 않는다.** 등록자는 자기 Job이 끝났는지 볼 수 없어 "Runner가 비었다 = 내 일 끝남"으로 대신한다. 그래서 요청마다 완료 플래그가 달린 상태 기계를 따로 만든다(진입 요청·로비 출발·회복·보드 준비 — 넷). 보드 준비는 아예 JobRegister를 우회해 Runner에 직접 넣는다 | C1·B3 | 높음 | `JOB/JobRegister/JobRegisterSystem.cs:23-24`(void), `C/CombatEntryRequest.cs:54`, `BufferSystems/LobbyView/LobbyDepartureRequest.cs:26-30`, `LifeCycleSystem/Common/ArtifactBoardPreparation.cs:80-82`, `L/LobbyDepartureFlow.cs:86-88` | – |
| P3 | **착지 대기 Job이 Runner를 여러 Tick 붙든다(설계 J4 위반).** 대기 동안 Stage Flow·튜토리얼·덱 편집 판단이 모두 멈춘다. 착지 신호는 다섯 객체(Movement → Lease → Service → Request → Ticket)를 거쳐 값 하나를 세우고, 폴링 Job이 다시 읽는다 | B1·C12·A3 | 중간 | `C/Jobs/CombatEntryTransitionJob.cs:43-45`, 설계 `2026-10-10-combat-job-plan-restructure-design.md:133` | – |
| P4 | **일시적 조건(Runner 바쁨)이 계약 위반과 같은 예외(F1)다.** `RequestEntry`는 바쁘면 예외인데 계약 주석에 사전 조건이 없고, Bootstrap은 확인 없이 부른다(지금은 경쟁 루트가 없어 잠복) | B14·C1 | 중간~낮음 | `C/CombatEntryService.cs:129-158`, `V/Boot/GameBootstrap.cs:547` | – |

### G2 전투 세션 수명과 전환 허가의 주인이 없다

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P5 | **"지금 전투 세션인가"의 표현이 여섯.** Core `HasBattle`·`BattleInstanceId`, 진입 요청 Active→Closed, 진입 서비스 `IsActive`, LifeCycle `_isActive`, Manager `_activeModule`, 로딩 씬 번호·View `IsAttached`가 따로 살며 "같은 순간에 바뀐다"는 가정으로 서로 검사한다. 보스 선정리가 이 가정을 깨서 0행·View 분기·Job 재검사가 생겼다 | B2 | 높음 | `C/CombatProgressReader.cs:78-79`, `C/CombatEntryRequest.cs:145-151`, `LifeCycleSystem/Base/LifeCycleBase.cs:23`, `Base/LifeCycleManager.cs:53-56`, `V/Combat/CombatViewSystem.cs:158·291-302` | – |
| P6 | **"이탈 완료"라는 사실이 없다.** 원본은 ExitCheck Job이 요청을 `Close()`하는 부수 효과뿐이라, 목적지 구간마다 다르게 추론한다 — Stage는 보류 장치(조건 9개, 시험 4개), Lobby는 같은 상황에서 예외, Stage 정상 경로는 확인 생략 | A6·C2 | 중간 | `C/Jobs/CombatExitJobs.cs:357-367`, `S/StageLifeCycle.cs:114-163`, `L/LobbyLifeCycle.cs:54-57` | – |
| P7 | **전환 허가가 없다.** `Change`를 부르는 주체가 셋(Job·Flow·Bootstrap)이고, 구간마다 이탈 검사 방식이 다르다(Stage 사전 검사, Lobby 사전 검사, Combat 사후 Job). Stage는 "요청이 Activating이고 Manager 현재 키가 Combat"으로 전환을 추론하는데, 이는 전환 Job의 두 줄 순서와 Manager가 `Enter()` 전에 키를 바꾸는 내부 순서에 기댄다 | B6·C3·A5 | 중간 | `S/StageLifeCycle.cs:222-231`, `C/Jobs/CombatEntryTransitionJob.cs:48-49`, `Base/LifeCycleManager.cs:68-71`, `L/LobbyDepartureFlow.cs:79-84` | – |
| P8 | **진입 요청이 두 진행 모델 사이 어댑터가 됐다.** 전투는 무상태 Decide인데 Stage·Lobby는 "번호 기억 후 확인" Flow라서, 요청이 실행 단계(Queued·Running·IsJobCompleted)를 밖에 공개한다 | B7 | 중간 | `S/StageNodeEntryFlow.cs:17-20·48-85`, `S/StageFlow.cs:51-90` | – |
| P9 | **요청 상태를 바꾸는 곳이 6클래스·3층**(Global JobRegister의 `MarkQueued`·`Fail` 포함). `Close()`·`Fail()`은 단계 검사가 없다 | A9 | 중간 | `JOB/JobRegister/JobRegisterSystem.cs:95·102`, `C/CombatEntryRequest.cs:151-152`, `S/StageLifeCycle.cs:239` | – |
| P10 | **소비자가 볼 수 없는 단계·늘 함께 참인 조건을 분기한다.** 실효 단계는 일곱인데 소비자는 열 단계 + 플래그 둘을 해석한다. Created·Running은 바깥에서 관찰 불가, Prepared면 Accepted ∧ IsJobCompleted는 늘 참. Stage 흐름·맵 관찰자의 "Active까지 기다림" 분기는 운영에서 도달하지 않는다 | C8·A16 | 낮음~중간 | `S/StageNodeEntryFlow.cs:56·73-77`, `V/StageMap/StageMapEntryObserver.cs:41-42·80`, `V/StageMap/StageMapNodeMovement.cs:57-59` | – |
| P11 | **"이번 요청과 같은 대상" 검증 사본 6곳**이 번호·챕터·노드·스테이지·참조 동일 중 서로 다른 조합을 본다 | A14 | 낮음~중간 | `C/CombatEntryService.cs:57-62·131-136·189-192`, `C/MapCombatPresentationTicket.cs:32-38`, `C/Jobs/CombatExitJobs.cs:85-91` | – |
| P12 | **"같은 전투"를 완료 객체의 참조 동일성으로 판단하는 것이 Core와의 암묵 계약이다.** Job 넷·View 셋이 기댄다. 계약은 골격 주석에만 있다 | A15 | 낮음~중간 | `C/Plans/CombatExitContext.cs:24`, `C/Jobs/CombatExitJobs.cs:129·166·174·255`, `V/Combat/CombatViewSystem.cs:292·850` | – |

### G3 판단과 실행이 프레임으로 갈라져 있다

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P13 | **판단은 LifeCycle Tick(프레임 N), 실행은 Runner Tick(N+1).** 그 사이를 이유로 Job이 Decide 조건을 다시 쓴다(선정리 확인 = 1~4행, Run 결과 읽기 = 0행 등). 그런데 그 한 프레임에 판단을 무효로 만드는 변화는 미리보기 교체뿐이고 이미 따로 처리한다. Advance·ExitBattle은 되돌릴 수 없는 상태라 재검사는 경합 대비가 아니라 식의 사본이다. 걸음마다 한 프레임 지연도 여기서 온다 | B4·C4·A12 | 중간~높음 | `C/Plans/CombatPlan.cs:83-102·143-169`, `C/Jobs/CombatExitJobs.cs:127-132·254-258`, `C/Jobs/CombatPreviewJob.cs:47` | – |
| P14 | **설계 J3의 전제가 실제와 다르다.** 설계는 "등록과 실행 사이에 Runtime 모듈이 돌지 않는다"고 적었지만, Global Tick 순서가 Render → Utility(Delay) → Audio → Input → Loding → UI → … → Job이라 그 사이에 Delay 콜백·로딩 Enter Node·UI가 돈다. 재확인 범위의 기준이 없어 Job마다 제각각이다(보스 Job은 전부 재확인, 포기 Job은 안 함) | A4·C4·A12 | 중간 | `Factory/Runtime/Global/GlobalSystemFactory.cs:45-62`, 설계 `…-design.md:129` | ✔ |
| P15 | **Job 사후 확인이 Core 내부 보존 규칙까지 재증명한다.** 판단에 쓰는 사실(H≥N, S 증가, 완료 존재)을 넘어 "EndBattle은 최신 결과 참조를 지우지 않는다" 같은 Core 규칙을 단언한다. Core가 바꾸면 골격도 바꿔야 하고, 놓치면 런타임이 F1로 멈춘다 | C7 | 중간 | `C/Jobs/CombatExitJobs.cs:172-178·303-304`, `C/Jobs/CombatAbandonJob.cs:41-48` | – |

### G4 골격이 결정이 아니라 재료를 공개한다

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P16 | **Decide 결과(`CombatStep`)는 만들고 버린다.** 같은 뜻을 소비자가 원시 사실에서 다시 만들고 정의가 이미 다르다. 튜토리얼 `IsAwaitingPlayerCommand`(주석 "같은 뜻인지는 회귀 시험이 고정"), 두 번째 Reader `TutorialFactsReader`(J13 "읽는 유일한 자리" 위반), 입력 정책 "종료 = Finished ∨ Completion"(Plan은 Completion만), View의 "재생 중" 정의, Lobby의 Buffer 직접 읽기 | A2·B10 | 높음 | `LifeCycles/Tutorial/Plans/TutorialPlan.cs:141-158`, `LifeCycles/Tutorial/TutorialFactsReader.cs:21-25`, `V/Combat/Input/CombatInputPolicy.cs:25-28`, `V/Combat/CombatViewSystem.cs:943-946`, `L/Plans/LobbyPlan.cs:36` | – |
| P17 | **View 계약의 `IsPlaybackBusy`는 정보가 없다.** 재생은 S = P+1일 때만 시작하고 확인(P 기록)과 재생 끝이 한 호출 안이라, 골격이 읽는 모든 시점에서 busy면 P<S다. 시험 가짜는 늘 false인데도 통과한다 | C9 | 낮음 | `V/Combat/CombatViewRefresh.cs:42`, `V/Combat/CombatResultPlayback.cs:53-69`, `V/Combat/CombatViewSystem.cs:769-771` | ✔ |

### G5 등록 계약과 Plan 구조가 Global에 루트 지식을 모은다

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P18 | **Plan이 정적 함수라 Global JobRegister가 모든 루트의 Plan 표와 의존(상점 시드까지)을 보관한다.** Global이 LifeCycle 타입을 알고(가이드 §8 위반), LifeCycle은 Global을 알아 순환이 생긴다. 등록 계약이 열거형 두 개뿐이라 데이터를 실을 수 없어, 대상 전달 방식이 넷이 됐다(전투 전용 입구, Buffer에 둔 로비 요청, Plan의 Buffer 재읽기, Runner 직접 등록) | B3·A17 | 높음 | `JOB/JobRegister/JobRegisterSystem.cs:7-15·24·41-46·65-71·87-105`, `L/Plans/LobbyPlan.cs:32-34`, `S/Plans/StagePlan.cs:29-31` | – |
| P19 | **공용 `JobType`의 뜻이 루트마다 다르고 잘못된 조합은 실행 시 예외다.** `NewRun`이 Stage에선 "새 Run + 맵 준비", Lobby에선 "출발 요청 처리". 연쇄 순서 관례도 둘(같은 우선순위 + 등록 순서 / 우선순위 0·1) | B18 | 낮음 | `JOB/JobRegister/JobRootType.cs:15-27`, `S/Plans/StagePlan.cs:33-37`, `L/Plans/LobbyPlan.cs:29-37` | – |

### G6 규칙·정책의 원본이 골격 밖이나 값에 흩어져 있다

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P20 | **SK1의 근본: Run 종료가 확정되는 시점이 결과마다 다르다.** Core는 Run 종료 영수증을 패배·포기 완료 때만 만든다. 보스 승리는 `CompleteChapter` 뒤에야 확정된다. 그런데 완료 UI는 Run 상태(`Outcome == Victory`, `LastSummary`)로 최종 승리 화면을 고른다. 그래서 골격이 "정리 먼저, 표시 나중"을 끼워 넣었다 | A1 | 높음 | `CoreCaculator/Contracts/Combat/BattleCompletionChanges.cs`(`RunCompletion = progression.EndAttempt ? … : null`), `GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs:93-105`, `C/Plans/CombatPlan.cs:171-190` | ✔ |
| P21 | **Run 수명의 주인이 없다.** `StartNewRun` 호출 넷(Stage·Lobby·전투 이탈·Bootstrap 개발 진입), "Run이 시작됐나" 판단 셋(로스터 수·Core Outcome·결과와 로스터 대조), 새 Run + 챕터 진입 순서 사본 넷. 목적지를 Stage로 바꾸면 전투 쪽 새 Run을 반드시 남겨야 한다(SK2 (a)는 목적지가 로비일 때만 성립) | B11·A7·C13 | 중간 | `S/Jobs/StageJob.cs:74`, `L/Jobs/LobbyRunStartJob.cs:41`, `C/Jobs/CombatExitJobs.cs:299`, `V/Boot/GameBootstrap.cs:462`, `S/StageLifeCycle.cs:300-304` | – |
| P22 | **진입 경로 종류가 "맵 명령 번호 0/양수"라는 값으로 숨어 9곳에 흩어진다.** 어느 Core API를 부를지, 착지를 기다릴지, 맵 입력과 대조할지가 숫자 해석이다. 개발 직행(번호 0)은 운영과 다른 Core 입구·준비 순서를 써서, 개발 직행으로 확인할 수 없는 경로가 는다 | A8·B15·C13 | 중간 | `C/Jobs/CombatPrepareJob.cs:41-52`, `C/CombatEntryService.cs:131-136·155-160`, `C/CombatEntryRequest.cs:56-58`, `V/StageMap/StageMapNodeMovement.cs:57` | – |
| P23 | **"이 노드는 전투인가"를 Core 밖 두 곳(Stage·Bootstrap)이 Buffer 값으로 먼저 판정하고, Core는 거절이 아니라 예외로 강제한다.** Core `PrepareStageEntry`는 노드 이동 규칙만 보고 스테이지 종류를 거르지 않아, 비전투 노드가 `EnterBattle`에 들어오면 수락 뒤 `ValidateCombatTarget`에서 `InvalidOperationException` → 준비 Job → F1. Bootstrap 주석과 CLAUDE.md의 "Core가 거절"은 틀린 설명이다 | B16·A11 | 중간 | `CoreSystem/CombatSystem/BattleEntryService.cs:100·125-133`, `CoreSystem/ChapterProgressionCoordinator.cs:294-301`, `S/StageLifeCycle.cs:268-280`, `V/Boot/GameBootstrap.cs:490-518` | ✔ |
| P24 | **실행 순서 규약이 코드 계약 없이 정수와 주석에 있다.** 그룹 Tick 순서와 씬 Node 우선순위(-200·-190·0·90·100·±110·200·-100)가 7개 파일 리터럴이고, J6(활성화가 View 연결보다 먼저) 같은 정합성이 숫자 사이 간격에 있다. 순서가 틀리면 재생 중 예외로만 드러난다. 문서도 낡았다: 프로그래머 가이드 §10의 씬 번호(0/1/2/3)는 코드(Lobby 1·Stage 2·Combat 3·Artifact 4)와 다르고, `runtime-scene-binding-guide.md` 표에 90·±110이 없다 | B5·A4·A10 | 중간 | `LifeCycleSystem/Base/LifeCycleBase.cs:55-58`, `V/Combat/CombatViewSystem.cs:70-72`, `V/Boot/GameBootstrap.cs:286-299`, `V/Tutorial/TutorialViewSystem.cs:51-53`, `LifeCycleSystem/Base/SceneIndices.cs:10-19` | ✔(씬 번호·Global 순서) |

### G7 의존 범위와 조립 경계가 넓다

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P25 | **골격이 자기 계약 `ICombatProgressView`를 주입받지 않고 View 그룹에서 꺼낸다.** 받는 타입은 View 전체 계약(`ICombatViewService` — Attach·가방 입력·덱 호버 포함)이고, 순환(골격 → InGame 그룹 → View 그룹 → 골격 계약)이 생긴다. 무관한 View 기능이 늘 때마다 골격 시험 가짜를 고친다. 평가의 "D 양호"를 일부 반박 | B8 | 중간 | `C/Plans/CombatPlan.cs:234-238`, `C/CombatProgressReader.cs:47`, `V/Combat/ICombatViewService.cs:12-23`, `Assets/Tests/EditMode/Framework/CombatProgressTestDriver.cs:76-115` | – |
| P26 | **Job이 InGame 그룹 전체(Core 서비스 22 + View 6)를 받아 Core 메서드 하나를 부른다(FW-101).** 실제 의존이 생성자에 드러나지 않는다. 같은 층의 `LobbyRunStartJob`은 좁게 받는다 | B9 | 중간 | `C/Jobs/CombatCommandJob.cs:13·42`, `C/Jobs/CombatExitJobs.cs:143·170`, `L/Jobs/LobbyRunStartJob.cs:13·20` | – |
| P27 | **StageLifeCycle이 전투 진입·복귀 규약의 절반과 덱 편집 사실 11개까지 맡는다**(역할 셋, 307줄) | B12 | 중간 | `S/StageLifeCycle.cs:28·128-163·187-231·252-280` | – |
| P28 | **Bootstrap이 Framework 수명 단계를 캐스트로 직접 부르고 PostBind를 다시 만든다**(`(ISystemBindScene)` 캐스트 둘, 개발 소스용 `RuntimeServices` 어댑터). 가이드 §8 "캐스트로 꺼내 수동 조작" | B13 | 중간 | `V/Boot/GameBootstrap.cs:291-302·436-444` | – |
| P29 | **"런타임당 오류 경계 하나"를 타입이 보장하지 않는다.** 시험 편의용 `?? new RuntimeFaultBoundary`가 운영 생성자 일곱 곳에 있어, 조립 경로 하나가 인자를 빠뜨리면 별도 경계가 조용히 생긴다(지금은 잠복). 평가의 "오류 경계 하나"를 일부 반박 | B17 | 낮음~중간 | `S/StageLifeCycle.cs:53`, `L/LobbyLifeCycle.cs:24`, `Factory/Runtime/RunTimeSystemFactory.cs:28` 외 4 | ✔ |

### G8 대기·오류의 빈틈과 시험 공백

| ID | 문제 | 출처 | 심각도 | 대표 근거 | 대조 |
|---|---|---|---|---|---|
| P30 | **조건을 참으로 만들 주체가 사라진 대기를 감지하지 못한다.** Combat 활성 중 `CombatSceneRoot`가 꺼지면 View는 Detach만 하고(오류 보고 없음, 다시 붙지 않음, D9), Plan은 "부착 대기"·"P<S 대기"로 영원히 Nothing을 돌려준다. Fault·로그가 없다 | C6 | 낮음~중간 | `V/Combat/CombatSceneRoot.cs:23`, `V/Combat/CombatViewSystem.cs:686-698`, `C/Plans/CombatPlan.cs:97-98`, `Assets/Tests/EditMode/Framework/CombatPlanDecideTests.cs:40` | ✔ |
| P31 | **보스 선정리 경로가 실제 View로 시험되지 않았고 개발 직행으로도 닿지 않는다.** 보스 연쇄 네 Job은 View Tick 전 한 Runner Tick 안에 끝나야 한다(사이에 View Tick이 끼면 Detach 후 `PresentCompletion` 예외). 통합 시험의 View는 가짜이고, 개발 직행 보스는 노드 스테이지로 정산돼 이 경로를 타지 않는다. `CombatPlanDecideTests`는 이름으로 옛 불변식("전투 없으면 아무것도 안 함")을 단언한다 | C5 | 중간 | `C/Plans/CombatPlan.cs:181-188`, `Assets/Tests/EditMode/Framework/CombatEntryIntegrationTests.cs:1039-1040`, `V/Dev/DevBossStageBattleSource.cs:24` | – |
| P32 | **처리 확인 실패의 오류 정책이 경로마다 셋**(전투 Job 예외 → F1, Stage·Lobby 로그 + 도메인 정지, 로비 출발 직접 Report). Runner는 예외 난 Job을 붙든 채라 회복 정책을 바꾸려면 Framework Runner까지 손대야 한다 | A13 | 중간~낮음 | `V/Boot/GameBootstrap.cs:230-234`, `S/StageLifeCycle.cs:283`, `L/LobbyDepartureFlow.cs:98-103`, `JOB/JobRunner/JobRunnerSystem.cs:80-85` | – |
| P33 | **정확성이 Tick 순서 가정 여럿에 기대는데 운영 조립 순서를 시험이 고정하지 않는다.** 시험은 루트를 손으로 조립해 순서를 흉내 낸다. Global Export 순서를 바꾸면 Stage·Lobby 진입 F1이나 보스 표시 깨짐을 잡을 시험이 없다 | C11 | 낮음~중간 | `Factory/Runtime/RunTimeSystemFactory.cs:37-40`, `Assets/Tests/EditMode/Framework/CombatEntryIntegrationTests.cs:880-893` | – |

### 작은 것

| ID | 문제 | 출처 | 근거 |
|---|---|---|---|
| P34 | `CombatExitContext.RetainedResult/Sequence`는 EndBattle Job 한 Execute 안에서만 쓰이고(다른 읽기는 시험), 목적지는 늘 같은 상수인데 생성자 인자, 모든 Job의 priority가 0, Execute 15곳의 `if (IsCompleted) return;`은 닿지 않음 | C10 | `C/Jobs/CombatExitJobs.cs:168·175-177`, `C/Plans/CombatExitContext.cs:16-22` |
| P35 | 공유 빈 목록 `NoJobs`가 가변 `List<IJobService>` 정적 필드다. 누가 Add하면 이후 모든 "할 일 없음"이 그 Job을 등록하고, 도메인 리로드를 끈 재생에선 재생을 넘어 남는다 | C10 | `C/Plans/CombatPlan.cs:36`, `LifeCycles/Tutorial/Plans/TutorialPlan.cs:25` |
| P36 | Shutdown 중 `OnExit`가 ExitCheck Job을 등록하고 Runner Shutdown이 버리는 것에 기댄다(실패는 안 함, 지침 §15 경미) | B(FW 표)·C | `LifeCycleSystem/Base/LifeCycleBase.cs:64-67`, `C/CombatLifeCycle.cs:59-66` |

## 4. 평가 문서(SK1~SK8)에 대한 정정

| 평가 항목 | 정정 | 근거 |
|---|---|---|
| SK1 "다섯 곳" | View의 화면 유지 분기(`CombatViewSystem.cs:289-293`)는 조건에 보스 지식이 없다(완료·인스턴스가 EndBattle 뒤에도 남음). 보스를 아는 것은 주석뿐. 근본 원인은 골격 밖(P20) | A |
| SK3 "상태 기계 두 벌" | 넷(진입·로비 출발·회복·보드 준비), 원인은 P2 | C1 |
| SK4 "세 층" | 다섯 객체, 그리고 설계 J4 위반(P3) | C12 |
| SK5 "두 방식" | 넷(P18) | B3 |
| §3 "오류 경계 하나" | 타입이 보장하지 않음(P29) | B17 |
| §5.2 D "양호" | 골격이 View 전체 계약을 꺼내 쓰고 순환이 있다(P25) | B8 |
| CLAUDE.md "Core가 비전투 진입을 거절" | 거절이 아니라 예외(P23). 개선안 단계에서 문서 고칠 것 | 직접 대조 |
| 개선 묶음 A~D(목표 8.7) | Framework·Core·다른 씬(Stage·Lobby)을 고치는 부분만 빼면 전투 View 구조 안의 것은 다시 쓸 수 있다(10-22 영역 기준). 개선안에서 다시 묶는다(§6) | 범위 기준 |

## 5. 검토했지만 문제 아님 (세 에이전트 공통)

- `CombatPlan.Decide` 자체는 순수 함수이고 한 화면에 모여 시험 가능하다.
- 이탈 연쇄의 등록 순서 의존: `JobQueue`가 같은 우선순위면 등록 순서를 문서로 보장한다(FW-015 PASS). 연쇄 중 끼어드는 등록은 같은 우선순위로 뒤에 붙는 ExitCheck뿐이다.
- ExitCheck는 전환 Job 실행 중 Exit Node에서 등록돼도 같은 Runner Tick에 실행된다.
- 예외 난 Job이 Runner에 남는 것: Bootstrap이 오류 뒤 Tick을 멈추므로 다시 실행되지 않는다(설계 F1).
- 할 일이 없을 때 Tick 할당은 0이다. 결과 순번 S·확인 번호 P는 전투를 넘어 단조 증가한다.
- 비최종 보스: `CompleteChapter`가 다음 챕터를 설치해도 Completion 참조는 남아 0행이 성립한다(시험이 없는 것은 P31).
- 가변 struct Job: 운영에선 곧바로 박싱돼 Runner가 상자 하나만 쥐므로 사본 함정이 실제로 일어나지 않는다.
- Framework 부품(LifeCycleBase·LodingNode·JobRunner·JobQueue·PostBind)은 재사용하고 있다. 자체 스케줄러·타이머·이벤트 버스는 없다. EventSystem 대신 상태를 읽는 방식은 DR-008(개별 구독 해제 없음) 때문에 맞다.

## 6. 개선안을 쓰기 전에 정해야 할 것 (범위 기준)

첫 판의 질문(Global Job·Core까지 이번 개선에 넣을지)은 범위 기준으로 답이 정해졌다 — 넣지 않는다. 남은 결정은 아래다. 지금은 기록만 한다.

1. **G3을 골격 안에서 풀지:** `Register(Combat, Next)`가 Job 하나를 돌려주고, 그 Job이 실행 순간 Read → Decide → 호출을 하는 방식(P13). JobRegister 계약은 그대로이고 설계 J1·J3을 고친다. 보스 행·보스 Job은 호진님 줄이라 그대로 감싸 둘지, 호진님과 같이 바꿀지 정해야 한다. (10-22 범위 정정: 보스 행·Job도 범위 안 — 6번)
   - **등록 방식은 지금대로 유지(2026-10-22 사용자):** 정해진 순서(진입·이탈·보스 완료)는 Plan 목록으로 한 번에, 끝이 정해지지 않은 진행 반복은 Runner가 비면 `Next`(설계 U5). 진행 반복을 미리 등록하려면 Runner를 붙잡는 대기 Job이 필요하고, Runner는 현재 Job이 안 끝나면 큐 전체를 멈추며(`JobRunnerSystem.cs:84-85`) 특정 Job을 뺄 수 없다. 종료 구간 전체를 승패 순간 한 번에 등록하는 안은 채택하지 않음.
   - **1번 A 채택(2026-10-22 사용자):** 등록 때는 `Decide`로 할 일이 있는지만 거르고(없으면 지금처럼 빈 목록), 있으면 `CombatNextJob` 하나를 넣는다. 그 Job이 실행 순간 `BuildStep`(지금의 `Next()`)으로 단계 Job을 만들어 바로 차례로 실행한다. 기존 시험 `CombatProgressJobTests`의 `Next_BuildsOneJobForTheDecidedStep`·`Next_ExitBattle_BuildsTheChainInOrder_…`는 실행에서 깨지므로 `BuildStep`을 부르게 고치는 것을 허락받음(새 시험은 만들지 않음).
2. **골격이 공개할 사실의 범위(G2·G4):** 세션 단계, 이탈 완료, 수락된 전환 중, 명령 대기 중, 종료 처리 중 가운데 무엇을 읽기 계약으로 낼지. 우리 소비자(입력 정책·View)는 바로 맞추고, 다른 소비자(Stage·Lobby·튜토리얼)에게는 제안한다.
   - **결정(2026-10-22 사용자, 대조 [아티팩트](https://claude.ai/artifact/BZJmyWSak9bFjK1nww1L13)):**
     - ① 종료 처리 중 = **(a)** 코드 그대로, 같은 뜻인 근거만 `CombatInputPolicy.IsTerminal` 주석으로. 근거: 명령·자동 진행 모두 `CombatTransitionPublisher.Publish` → `BattleTransactions.Apply` 한 길이고, 종료된 명령은 완료와 같은 거래로만 설치된다(`BattleTransactions.cs:218-219`) — "Finished인데 완료 없음"은 생기지 않는다.
     - ② 이탈 완료 = **(b)** → **5번 결정으로 바뀜: 우리가 직접 고친 뒤 변경 사항을 정리해 넘긴다(호진님 보류 장치·시험 4개 포함).** 처음 결정: 우리 코드(`OnExit`에서 정상 이탈이면 `CombatEntryService.TryCloseExited()`로 바로 닫고, 전투가 남으면 지금처럼 `ExitCheck`)는 설계만 해 두고, 호진님 제안서에 "Stage 보류 장치를 지우는 대신 이렇게"로 넣어 답을 받은 뒤 한다. 하면 호진님 시험 4개(`CombatEntryIntegrationTests.Stage.cs` 434·490·504·523, 공용 준비 546-560이 "32번째 전환 뒤 ExitCheck가 남아 있다"를 단언)가 실행에서 깨진다. 정정: 보통은 `ExitCheck`가 전환 Job 바로 뒤 같은 Runner Tick에 돌아 Lobby 예외 위험은 없고, 전환 Job이 그 Tick의 32번째 + 씬 로드가 한 프레임 안에 끝나는 경우만 위험하다.
     - ③ 세션 단계·④ 수락된 전환 중·⑤ 명령 대기 중 = **보류.** 지금 조합하는 곳이 호진님 코드뿐이라 제안서에 "낼 수 있다"만 적는다.
     - **10-22 범위 정정으로 다시 정할 것:** ③을 쓰는 `CombatViewSystem` 보스 유지 분기와 ⑤를 쓰는 튜토리얼 접점이 이제 범위 안이다. ④(Stage만 씀)는 그대로 보류.
     - **다시 정함(2026-10-22 사용자, 권장안대로):** ⑤ 명령 대기 중 = **공개.** `Decide` 옆 판정 하나를 튜토리얼 접점이 쓰고 사본(`TutorialPlan.IsAwaitingPlayerCommand`)과 두 번째 사실 읽기를 걷는다. ③ 세션 단계 = **개선안에서 보스 경로(SK1) 설계와 함께 정한다.**
     - 기존 시험 규칙(같은 날, CLAUDE.md 작업 방식에 반영): 우리 변경으로 실행이 깨지는 **우리** 기존 시험은 확인 내용을 그대로 두고 호출만 맞춘다. **다른 사람** 시험은 먼저 묻는다.
3. **전투 이탈의 새 Run(P21):** "Run이 끝나면 로비, 로비 출발이 늘 새 Run을 만든다"는 지금 정책을 전제로 전투 쪽 새 Run을 뺄지. 목적지가 다시 Stage가 되면 되돌려야 한다.
   - **결정(2026-10-22 사용자): A(빼기).** 처음에는 "호진님 제안서에 넣고 답을 받은 뒤, 거절되면 B(우리 상수 `NewRunLoadoutKey`가 `StagePlan.DefaultLoadoutKey`를 가리키게)"였으나 **5번 결정으로 우리가 211줄까지 직접 고친 뒤 정리해 넘긴다.** 이유: 이탈 연쇄에 새 Run Job을 넣는 줄 `CombatPlan.cs:211`이 보스 커밋 `cb76bd2c` 이후 호진님 줄이다. 확인: 지금은 Run 종료 이탈에서 새 Run 1(`StartNewRun(1)`)을 만들고 로비 출발(`LobbyRunStartJob:41`, 조건 없음)이 새 Run 2를 또 만들어 1은 쓰이지 않는다. 빼도 로비 화면 차이는 찾은 범위에서 없다 — `MainCharactorUI`는 로비에서 초상만 보이고 병과는 유닛 정의에서 와 Run 중 바뀌지 않음, 로비 덱 팝업은 Run을 읽지 않음, 명성·`LastSummary`는 새 Run이 지우지 않음, `StartNewRun`은 끝난 Run에서도 시작 가능. 하면 우리 `CombatNewRunJob`·`NewRunLoadoutKey`를 지우고, 우리 시험(새 Run Job 시험 셋, `Next_ExitBattle_…` 연쇄 단언, 이탈 뒤 새 Run을 단언하는 통합 시험)을 규칙대로 맞추고, 설계 J7과 CLAUDE.md "Run 종료 복귀 정책"을 고친다. 로드아웃 상수 `StagePlan.DefaultLoadoutKey`는 천명님 줄.
4. **연결 작업의 범위:** 위 변경에 따라 고칠 우리 연결 줄(JobRegister의 Combat 등록·`RegisterCombatEntry`, `JobType` 전투 값, Factory의 오류 경계·View 계약 연결)을 어디까지 할지. 고치면 민형님 공유 문서에 적는다.
   - **결정(2026-10-22 사용자, 권장안대로):**
     - ① `JobType.AdvanceCombat` 삭제(`JobRootType.cs`, 우리 줄). 운영 사용 0. 우리 시험 `CombatPlanTests` 두 줄은 같은 번호로 맞춤(`[TestCase((JobType)5)]`, 가짜 기록 키 `Call((JobType)5)`) — 확인 내용 그대로.
     - ② 오류 경계 대체 생성(P29)은 **줄인 안:** `LifeCycleSystemFactory.CreateComposition`과 `InGameSystemFactory.Compose`(운영 조립이 지나가는 가운데 두 자리)는 경계를 반드시 받고(없으면 `ArgumentNullException`), 대체 생성은 시험용 입구(`LifeCycleSystemFactory.Create`, `InGameSystemFactory.Create`·`CreateWithSources`)로 옮긴다. 뿌리 `FrameworkRuntimeFactory`(민형님 `TestRuner.cs:91`·호진님 시험이 인자 없이 부름)와 `InGameViewSystemFactory`(호진님 시험 셋이 인자 없이 부름)는 그대로. 시험 영향 없음. 네 곳 모두 필수로 하면 인자 없이 부르는 곳이 운영 1·시험 10(호진 7·민형 1·천명 1·우리 1)이라 채택하지 않음.
     - ③ `RegisterCombatEntry`의 `MarkQueued`·`Fail`은 **그대로.** 등록이 결과를 돌려주지 않는 Framework(G1)에서 "등록 순간 요청을 차지"하는 확인 장치이고, 옮기면 JobRegister가 중복 등록을 막지 못하며 우리 시험 `EntryRegistration_CapturesTarget_RejectsDuplicate_…`의 확인 내용이 바뀐다. P9의 우리 몫은 `Close`·`Fail` 단계 검사(우리 파일)로 개선안 직접 항목.
     - P25는 연결 작업이 필요 없다(조립 순서가 LifeCycle → InGame이라 Reader 생성 때 View가 없음). `CombatPlan.RequireView`가 Reader가 보관한 View를 쓰게 하는 것은 개선안 직접 항목. JobRegister의 Combat Plan 등록 줄은 바꿀 일 없음.
5. **제안·요청 문서를 어디까지 쓸지:**
   - 호진님: 보스 선정리 사본(SK1)·착지 대기(SK4·P3)·Stage 이탈 추론(P6·P7)·요청 단계 소비(P10)·튜토리얼 사본(P16)·StageLifeCycle(P27)·`NoJobs`(P35)
   - 천명님(Core 전달 문서): 보스 승리 완료에서 Run 종료·요약 게시(P20), 비전투 노드 진입을 예외 대신 거절로(P23), 완료 객체 참조 동일성 계약 명시(P12)
   - 민형님: 연결 줄 변경 공유 + Framework 제약으로 생긴 영향 알림(J4 전제 위반, `IsIdle` 공유, 등록 결과 없음, 프로그래머 가이드 §10 씬 번호). 고쳐 달라는 요청인지 알림인지 정해야 한다
   - **결정(2026-10-22 사용자):** 제안서를 먼저 보내 답을 기다리는 방식은 쓰지 않는다.
     - **호진님:** 우리 변경에 묶인 호진님 줄은 **우리가 직접 고치고**(사용자 지시 예외), 변경 사항을 정리해 넘긴다. 범위는 셋 — 2-② Stage 보류 장치(`StageLifeCycle` `TryDeferCompletedCombatReturn`·`_pendingCombatReturn`)와 그 시험 4개, 3 이탈 연쇄 새 Run 줄(`CombatPlan.cs:211`), 1번 뒤 늘 참이 되는 보스 Job 재확인(`CombatCompletionCheckJob`, `CombatRunOutcomeJob` 앞쪽). 그 밖의 구조 항목(SK1 보스 경로·SK4·P3 착지 대기·P10·P16·P27·P29 생성자 셋·P35, 2-③④⑤)은 고치지 않고 정리 문서에 "이렇게 할 수 있다"로만 적는다.
     - **천명님:** Core 구조 변경 요청(P20·P23·P12)은 하지 않는다. 우리 변경 뒤 Core와 관련된 바뀐 점만 정리해 Core 전달 문서 새 절에 넘긴다(미전달). P20이 근본인 보스 "정리 먼저, 표시 나중" 경로는 그대로 남는다.
     - **민형님:** 프로그래머 가이드 §10의 씬 번호(`framework-programmer-guide.md:466`, "Lobby/Stage/Artifact/Combat = 0/1/2/3" → 코드 `SceneIndices` Boot 0·Lobby 1·Stage 2·Combat 3·Artifact 4 예약)는 **우리가 직접 고친다**(문서만, 코드 무변경). 연결 줄 변경 셋(`AdvanceCombat` 삭제, Factory 대체 생성 이동, `CombatNextJob` 사용 방식)은 기존 10-10 Job·Plan 공유 문서에 짧은 절로 남긴다(미전달). Framework 제약 알림 항목은 쓰지 않는다.
     - **10-22 범위 정정으로 바뀜(호진님):** 골격 폴더·전투 View·결과 화면·튜토리얼 접점 안의 호진님 줄은 범위 안이라 개선 대상이다(SK1 보스 경로, SK4·P3 Lease·Ticket·진입 전환 대기 분기, P16·P35 튜토리얼 접점 포함). 범위 밖(Stage·Lobby·StageMap)에서는 우리 변경에 묶인 Stage 보류 장치와 그 시험 4개만 고친다(사용자 지시). 나머지 범위 밖 항목(P8·P27·P29 생성자 셋·Lobby 진입 조건·StageMap 신호)은 정리 문서에 적는다. 바꾼 다른 사람 줄은 모두 변경 정리를 넘긴다(현강·천명님 줄도 같음).
6. **우리 파일 속 다른 사람 줄과 맞물리는 변경:** G3·P26 개편은 보스 Job(호진님 줄)과 같은 파일·같은 흐름에 닿는다. 그 줄을 건드리지 않고 둘 수 있는 모양으로 설계할지 정해야 한다.
   - **5번 결정으로 정해짐(2026-10-22):** 1번 A는 보스 함수(`PresentCompletion`·`ExitBattle`)를 본문 그대로 부른다. 우리 변경에 묶인 호진님 줄 셋(5번 목록)만 직접 고치고, 그 밖의 호진님 줄은 건드리지 않는다. P26(Job이 Core 서비스 하나씩만 받게)은 `ExitBattle` 안 호진님 줄이 우리 Job 생성자를 부르므로 이번엔 보류한다.
   - **10-22 범위 정정으로 바뀜:** 보스 함수·Job도 범위 안이라 1번 A에서 그대로 감쌀 이유가 없고, P26 보류도 풀린다(보스 Job 포함 전투 Job 전부).
7. **목표 기준:** 제약(G1·G5)과 다른 사람 몫을 뺀 "범위 안 상한"을 목표로 평가를 다시 매길지.
   - **결정(2026-10-22 사용자 정정):** "Framework 및 GlobalSystem으로 짜여 있는 것들과 Core, 사실상 게임 내의 계산기를 이용해서 View 측에서 최적으로 구조를 구상" — Framework·GlobalSystem·Core를 주어진 기반으로 두고, **Combat 씬의 전투 View 구조 전체(§0 범위 안, 작성자 무관)**를 6기준으로 매긴다. 기반 자체의 한계로 생긴 감점은 따로 표시하고, 그 위에서 View 쪽 구조가 얼마나 잘 흡수했는지를 본다. 목표 수치는 개선안에서 제안한다(사용자 판단 "8.7이면 안 된다").
