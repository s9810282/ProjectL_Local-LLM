# 전투 View 구현 계획

- 작성일: 2026-10-01 (12차 View 내부 책임 분리 2026-10-05)
- 현재 코드 UML: [통합 UML](../specs/2026-10-05-combat-current-uml.md) / [오프라인 HTML](../specs/2026-10-05-combat-current-uml.html). 이전 구현 예상 도표는 당시 설계 이력으로 보존한다.
- 기준: [전투 View 설계 12차](../specs/2026-09-30-combat-view-design.md), W1~W23, [골격 계획 §0.1·Phase 6A](2026-09-30-combat-flow-skeleton-plan.md). `e0b02cc7` 병합 후 최신 실행 증거는 §7.20, 현재 경로 이동 계약은 [Core/View 연동 가이드 §8](../guides/unit-movement-attack-core-handoff.md#8-current-drag-path-movement-integration)을 따른다. W7은 유지하고 확장 조건은 §8에 둔다.
- 상태: **진행·검증 상태는 이 문서 §7이 단일 기록이다.** 설계·UML에는 상태를 적지 않는다. 체크박스는 실제 작업과 검증 뒤에만 완료로 바꾼다.
- 방식: 설계 → 이 계획 → Phase별 구현·검증·보고. **각 Phase가 끝나면 멈추고 보고한다.** 새 Phase는 워크시트로 할지 직접 할지 묻는다. 보고 끝에 다음 Phase의 목표·파일·핵심 동작·주의점·확인 방법을 적는다.
- 커밋·push·외부 전달은 사용자가 한다. Claude는 Unity를 실행할 수 없으므로 컴파일·테스트·재생 확인은 **실행할 항목을 적어 사용자에게 요청**하고 받은 결과를 §7에 적는다.

## 0. 완성 목표와 Phase

완성 목표: **Boot → Combat 진입, Core가 준비한 전투의 필드·구조물·유닛·손패·HUD 표시와 입력 연결.** `RecordInputOnly`로 정적 표시·입력을 진단하고, 새 개발 `Live` 런타임에서 실제 Core 처리 → 연출 완료 → 다시 그림 → 입력 정리 → 결과 확인, 밤 종료 후 재배치와 다음 밤 진입, 전투 종료 후 입력 차단·마지막 결과 확인까지 검증한다. Core Task 7과 골격 Phase 5는 완료됐으며 운영 DB 준비·F2 이탈은 별도 조건이다.

| Phase | 목표 | 착수 조건 | 끝나면 볼 수 있는 것 |
|---|---|---|---|
| 0 | 기준 확인, 공유 문서 | 지금 | 공용 변경 범위와 외부 공유가 문서로 준비됨 |
| A | DB 선행: 새 테이블 3개 + 기존 스테이지 Economy 참조(T11 일부) | Phase 0 | 실제 카탈로그로 DB 생성 성공 |
| B1 | 수명·씬: 씬 상수, Factory 인자, `GameBootstrap`·`RuntimeFaultBoundary`, Boot·Combat 씬 | Phase A | Live + 개발 source 없음: Combat 진입 → 기본 준비 소스 실패가 1회 보고되고 Tick·전환 정지 |
| B2 | 기존 실행 모드·개발 준비(W12의 최초 구현) | Phase B1 | 당시 검증 결과 보존. 새 Core 진입 전제 대응은 B3 |
| B3 | 새 진입 계약 대응(W23): Bootstrap·두 모드·활성화 연결 | 기존 B1/B2 + 골격 6A 전달 계약. 6A와 공동 완료 | 선행 Combat 선택 확정 없이 요청 전달, 수락 후 한 번만 활성화 |
| C | ViewSystem 연결·후처리, Playback 재생 수명, 연출 완료/실패·취소 계약 | Phase B2 | 재생: 씬 연결·신호 로그. EditMode: 결과 수명·자원 정리·정상 종료 보존 |
| D | 표시: 필드·레일·배치 구역·구조물·유닛·손패·HUD (L 이식) | Phase C | 재생: 샘플 전투가 그려짐 |
| E | 입력 Policy·Interaction·고스트, 시작/턴 종료 단일 대기, 국면별 Layer, 개발 Live 통합 | Phase D | 기록 모드 진단 + 복수 밤 진행·전투 종료의 입력/결과 계약 |
| E2 | 미리보기 응답 사용(9차, W21): 공격 조준 종류별 입력, 이동·공격 범위·대상·호버 가능 여부·고스트 색·현재 행위자 칸 | Phase E | Live에서 Single·Direction 공격 수락, 범위 표시 |
| A2 | 공격 지정 방식 DB(9차, W22): 지정 방식 테이블, UnitDataSO 참조, 에셋, Core 연결 요청 | Phase A | DB 생성·자산 교차 검증 통과, Core 전달 문서 갱신 |
| F | 인계, 공유 결과, Core에 쓰임새 전달 | 완료한 Phase의 증거 | 새 세션이 이어서 할 수 있는 인계 |

**두 계획의 통합 실행 순서는 [골격 계획 §0.1](2026-09-30-combat-flow-skeleton-plan.md#01-병합-후-실행-순서와-계획-간-소유권), 구현 차단 사유·담당·해소 기준은 같은 문서 §0.2가 원본이다.** 아래에는 View Phase별 작업만 연결한다. 이미 구현한 진입·DB·입력·이동 연출을 다시 만들지 않으며, fixture 성공과 운영 DB 성공은 별도 증거로 마감한다.

**세션 간 경계:** 골격 세션이 6A.0에서 대상/결과 전달·활성화 호출자를 확정하고 B3는 그 좁은 계약을 사용한다. `GameBootstrap` 등의 수정 담당 파일을 공유해 동시 편집을 피한다. 작업 상세와 API 확정 기록은 골격 계획, View 수정/검증 상태는 이 문서 §7이 각각 원본이다.

### 0.1 View 잔여 Phase와 골격 연결

**2026-10-05 최신 인계:** B3·E/E2/A2와 F1·정상 F2 기반의 완료/검증 인수는 §7.21~23, Phase F 문서 완료는 §7.24를 따른다. 사용자 지시로 View 후속 작업은 다른 세션에서 이어가며 시작 안내는 §7.25다. 아래 표에서 완료한 검증을 새 미완료 작업으로 다시 열지 않는다.

| 기존 Phase / Task | 남은 View 작업 | 필요한 조건과 완료 기준 |
|---|---|---|
| **B3 / B3.4~5** | 기존 완료 인수 유지(§7.21) | 새 변경이 진입/활성화에 영향을 줄 때 해당 회귀만 확인. 운영 준비 인수는 별도 |
| **E·E2 / E.7·E.9** | 경로 이동·Return·공격·결과 소비의 완료 범위 유지(§7.21) | 추가 입력/표현 작업은 X1·X3~5에서 범위 결정. 기록 모드에서 가짜 이동 context를 만들지 않음 |
| **A2 / A2.6** | 데이터/query·기존 검증 인수 완료 유지(§7.21) | 운영 준비 source 조립 완료로 확대하지 않음 |
| **E / E.8 + X3** | 기존 F1 보완 및 검증 인수 유지(§7.23) | 새로운 입력·연출·정리 변경에 필요한 회귀만 추가. Core 답변 대기 아님 |
| **X6 / 골격 6B F2 연결** | 정상 이탈 기반·일반 승리 호출자 연결은 §7.22~23. 남은 것은 포기와 보류한 결과 화면 연결 | 포기는 Core 공개 API 인수, 패배·챕터 완료는 목적지 결정 필요. 정책 원본은 골격 §6.4 |
| **A2 후속 / 운영 DB 공동 인수** | Core source 인수 후 실제 데이터의 진입·표시·공격·미리보기·재화 사용 검증 | [골격 계획 §0.2](2026-09-30-combat-flow-skeleton-plan.md#02-구현-차단-사유와-해소-작업) 해소 필요. F2와 병행 가능하며 View가 Core DB 정의를 대신 조립하지 않음 |
| **F / F.1~4** | 현재 코드 범위의 문서 인계 완료(§7.24). 최신 구조·UML·공유 문서·새 공동 인계 정리 | 골격 7과 함께 마감. 운영 전투 전체 완료와 구분하며 후속 구현 인수 때 갱신 |
| **X1·X3·X4 후속 보완 후보** | 구조 평가의 책임 집중·표시 계약·입력 반복 비용 검토 | Core 없이 계획 가능. 아직 구현 착수 아님. §7.25의 View 전용 인계 참조 |

## 1. 지킬 경계와 원칙

설계·구현·리뷰마다 아래를 점검한다. 근거는 설계 §3·§13, `CLAUDE.md`, [설계 세션 인계](../handoffs/2026-09-30-combat-view-design-session-handoff.md) §4.

**SSOT**
- 전투 상태의 원본은 Core가 쓰는 Buffer다. **View는 Core 소유 Buffer에 쓰지 않는다.** View가 쓰는 것은 `CombatViewBuffer`뿐이다. 개발용 상태도 Core가 계산·기록한다(W7).
- 결과 발행 순번의 원본은 `LatestResultSequence`, 완료 기준은 입력 Buffer의 `PresentedResultSequence`. View는 결과 사본이나 같은 의미의 완료 카운터를 따로 만들지 않는다.
- View 자신의 상태는 Refresh의 표시·응답 이력, Playback의 재생 번호·세대·완료/실패·후처리 상태, Interaction의 Drag·모드·단일 명령 대기다. ViewSystem은 재생 상태를 복제하지 않는다. Core 국면·행위자·종료 여부·실제 Layer도 별도 필드로 복제하지 않는다. 오류 여부는 런타임 공통 영역의 `RuntimeFaultBoundary` 하나가 소유한다.
- 외형 값(프리팹·색·간격·크기)은 ViewGroup DB나 씬 설정에서 읽는다. 코드에 박지 않는다.
- 씬 인덱스는 모든 LifeCycle이 `SceneIndices`를 참조한다. 런타임 수명은 `GameBootstrap` 한 곳. 실행 모드는 조립 때 고정한다.
- View가 계산할 수 있는 표시값은 저장하지 않고 그때 계산한다(P7).

**SOLID**
- 한 타입 한 책임: 부트스트랩=수명, 오류 경계=최초 오류 정지, Flow=등록 판단, ViewSystem=연결·후처리 순서, Playback=재생 수명, Refresh=신호, Policy=입력 허용·목표 Layer, Interaction=입력 해석·대기, Presentation=실제 연출·자기 자원, Presenter=그림, SceneRoot=참조. ViewSystem에 재생 상태·명령별 허용 분기·그림·규칙 코드를 넣지 않는다.
- 확장은 새 Presenter·연출 구현으로, 새 명령은 해석과 허용 정책의 변경을 Interaction·Policy에 국한한다. 작은 유한 분기를 피하려고 범용 등록 프레임워크를 추가하지 않는다.
- 계약 준수: `IStageInputReceiver`(`SelectCanceled`에서 확정 금지), `ICombatPresentation`(완료/실패 1회·취소로 완료 위조 금지·자기 자원 정리), 숨김 API. 실제 연출 추가 때도 같은 계약 테스트를 적용한다.
- Presenter는 Buffer를 모른다. `CombatViewSystem`은 필요한 싱크만 보관한다.
- 인터페이스에 의존하고 조립은 Factory·부트스트랩이 한다. 정적 서비스 찾기·싱글턴 접근을 쓰지 않는다(부트스트랩 중복 방지 외).
- 쓰임이 하나뿐인 인터페이스나 계층은 만들지 않는다(지침 §27).

**Framework 계약·경계**
- FW-001·002(서비스·모듈 등록), FW-003·지침 §11(Build 뒤 구조 불변), FW-005~007(입력은 슬롯으로), FW-010~012(Render Scope 반환), FW-014(`CustomPool`), 지침 §20·21·DR-008(전역 EventSystem 구독 금지).
- Tick·입력 경로에서 `Find`·`GetComponent`·LINQ 금지. 씬 객체 찾기는 전투 씬 진입 때 한 번(W2).
- K14: 명령은 처리 번호 확인 뒤에만 지운다. 입력 전체 비우기(`Clear`)는 `CombatLifeCycle`의 진입·이탈에서만 한다. View는 `Clear`를 부르지 않는다.
- K1: 거절·연출 없는 결과도 반드시 확인한다.
- 정상 전투 종료는 `SuspendSubmissions`로 새 입력만 해제하며 결과 소비를 유지한다. 세대·완료 예약을 지우는 `StopInput`은 F1·종료 수명용이다. 종료 판단은 Core의 Finished/Completion을 읽는다.
- 입력 허용 정책의 단일 기준은 설계 §7.1, 단일 대기는 §7.2, Layer는 §5.5, 정상 종료 수명은 §5.6이다. 설계 §13.4의 외부 경계·유지보수·확장·성능 항목을 Phase C·E 리뷰에 적용한다.
- Core 코드는 고치지 않는다. 공용 파일(Factory, Build Settings, `CombatLifeCycle` 외 LifeCycle)은 필요한 줄만 바꾸고 공유한다.

**코딩·에셋**
- 블록 네임스페이스(`ProjectL.<Script 아래 경로>`), Allman, 공백 4칸, UTF-8/LF, 한국어 XML doc, `sealed` 기본, 직렬화 필드 camelCase·private 필드 `_camelCase`.
- 새 `.cs`는 `.meta`와 함께 다룬다. Unity가 만든 `.meta`도 커밋 목록에 넣는다.
- 씬·프리팹·Build Settings 편집은 에디터 작업이다. Claude가 YAML을 직접 고치지 않고 사용자에게 절차를 준다.
- 테스트는 기존 `ProjectL.Tests.Framework`에 순수 C# 부분만 둔다. 새 테스트 asmdef를 만들지 않는다. Unity 표현은 재생으로 확인한다.

## 2. 출발점 (2026-10-02, `ede27fef` 코드 기준)

| 사실 | 계획에 미치는 영향 |
|---|---|
| 런타임 호출자 없음, Build Settings는 `Stage.unity`(0번)만 | Phase B1이 부트스트랩·씬·Build Settings를 만든다 |
| `InGameGroup.asset` 9/12 테이블, 기존 스테이지의 Economy 참조도 검증 대상 | 새 테이블 등록만으로 완료하지 않고 카탈로그 전체 생성 검증 |
| `InGameViewSystemGroup` 비어 있음, 수명 전체 전달 | `CombatViewSystem`을 팩토리에 등록만 하면 수명에 참여 |
| 입력 수신 슬롯·Controller·Layer 있음. Unity 콜백은 Tick과 별도 | View 수신 입구에 오류 경계, 종료 시 Unbind·ClearLayer·Drag 정리 필요 |
| `ICombatResultReadSink.TryGetLatest`, `CombatResult` 있음(Core Task 4A) | 결과 처리·Layer 전환을 실제 타입으로 작성 |
| Core Task 7 인수, 골격 Phase 5 완료(세 Job 연결, 사용자 결정 2026-10-02) | 기록 모드 진단과 개발 Live 통합 증거 분리. `CombatFlowIntegrationTests` 실패는 테스트 코드 문제로 View 진행을 막지 않는다 |
| `IBattleInitializationSource`(internal), `InGameCoreSystemFactory.Create(source)` | 준비 입력 전달. 실행 모드·오류 경계는 Core에 넘기지 않는다 |
| `ICharacterRosterService.InitializeRoster(loadoutKey)` | 개발 설정에서 보유 로스터 생성. 현재 PlayerLoadout·유닛 에셋이 이 호출을 통과하는지는 Phase A에서 확인 |
| `PrepareBattle`은 로스터만 필요, 스테이지 종료는 `PlanStageResolution`으로 현재 챕터·노드 필요 | Live 개발 설정은 `ChapterProgression.EnterChapter` → `ConfirmStageEntry`도 호출(W12, B2.3). RecordInputOnly는 로스터만 |
| `ConfirmStageEntry`가 실제 DB Stage를 `Validate()`(Economy 필수) | Phase A.1a가 Live 진행 준비의 선행 조건 |
| 재화를 채우는 Core 공개 경로 없음, `InitialGold` 미반영. 재화를 쓰는 전투 계산은 수동 드로우뿐 | 개발 fixture의 `ManualDrawGoldCost = 0`. Buffer에 재화를 쓰지 않는다. Core 전달 후보(0.3). **2026-10-06 해소:** Core가 스테이지 골드를 전투 상태로 분리했고 fixture는 DB 경제를 쓴다(§7.54) |
| 옛 View 코드는 `origin/develop-4week`의 `Assets/Scripts/ProjectL/Unity/`(View/Field·HandQueue·PlacementPreview·Combat, Adapter/Coordinate) | `StageSnapshot`·옛 SO·`ProjectL.BasicSystem.Pooling` 의존을 떼고 옮긴다 |
| Framework `CustomPool<T>`, Render `ColorScop` | 풀·셀 색에 사용 |
| Artifact 임시 인덱스 2, 새 Combat도 2가 될 예정 | 모든 LifeCycle의 getter를 공유 상수로 정렬. Lobby·Artifact는 예약 번호로 분리 |
| 리플렉션 Factory 호출: `FrameworkRuntimeCompositionTests`는 `Invoke(null, null)`, `ChapterGraphPreparationTests` 도우미는 매개변수 기본값 배열로 호출(Phase 0에서 확인) | 선택 인자 추가와 `FrameworkRuntimeCompositionTests` 수정은 같은 작업. ChapterGraph 도우미는 그대로 동작할 것으로 보되 B1.5에서 실행으로 확인(enum 기본값 전달 포함) |
| Core 브랜치 `069327c3`(옛 구조 삭제, 8R6)가 Stage 브랜치에 미병합. 병합 충돌 없음, View가 쓰는 API 변경 없음(Phase 0 확인) | 병합 시점은 사용자 결정. 공용 Factory·테스트를 바꾸는 B1 전에 병합하면 충돌 범위가 줄어든다 |

## 3. 외부 조건

| 조건 | 필요한 Phase | 상태 |
|---|---|---|
| 민형님: 부트스트랩·F1, Factory·테스트 호출, 전체 LifeCycle 씬 상수, 개발용 등록 정책 공유 | B1 전 공유(승인 대기로 막지 않음) | Phase 0에서 문서 준비, 외부 전달 미실시 |
| 전투 진행 골격 Phase 5 실제 Job 연결 | Live 통합 E.9 | **완료** 유지. 이전 fixture 실패는 병합에서 수정됐으며 최신 인수는 골격 계획 §6.11 |
| 재화를 채우는 Core 공개 경로 | 운영 재화 초기화·유료 드로우 인수 | 운영 초기화 정책/경로 대기. 개발 전용 재화 주입과 구분하며 사유·해소 작업은 골격 계획 §0.2. **2026-10-06:** 스테이지 골드는 전투 준비 때 DB `InitialGold`로 시작(§7.54). Run 시작 재화는 Core 미정 |
| R7·R10·R11 표시 확장 | 이번 범위 밖 | 데이터·응답·조회 API 도착과 UI 승인은 별개. 타임라인 등은 읽기 경계·표시 설계 필요 |
| F2의 전환 소유자·Core 포기 입력 조립·Completion 표시 수명 | 골격 6B + View X6 | 패배·EndBattle 구현은 있음. 부족한 계약과 담당은 골격 계획 §0.2, View는 표시/입력 연결 담당 |
| 운영 준비 T1/G7 | A2 후속: 기본 DB 소스로 운영 전투 시작 | A2 제공 완료, Core 기본 source 조립 대기. 사유·해소 기준은 골격 계획 §0.2. E.9는 명시적 fixture 사용 |

## 4. 변경 파일 지도

`View/`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`, `Tests/`는 `Assets/Tests/EditMode/Framework/`다. 파일명은 제안이며 구현 때 확정해 §7에 적는다.

| 파일 | 내용 | Phase | 영역 |
|---|---|---|---|
| `Assets/ProjectL/DataBaseSO/InGameGroup/…` 새 테이블 3개, 그룹 등록, 기존 CombatStageDetail의 Economy 참조 | T11 일부 | A | 우리(DB 계획) |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/Base/SceneIndices.cs` | Boot 0·Stage 1·Combat 2·Lobby 3·Artifact 4 | B1 | 공용 기준 |
| `…/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs` | B1: 씬 상수, 실행 모드·오류 경계 보관. B2: Flow 전달 | B1, B2 | 우리 |
| `…/LifeCycleSystem/LifeCycles/{Lobby,Stage,Artifact}/…LifeCycle.cs` | SceneIndex getter만 공유 상수로 변경 | B1 | **공용**, 다른 Flow 로직 유지 |
| `…/LifeCycleSystem/LifeCycles/Combat/CombatExecutionMode.cs` | 불변 실행 모드(Factory 인자에 필요하므로 B1에서 정의) | B1 | 우리 |
| `…/LifeCycleSystem/LifeCycles/Combat/CombatFlow.cs` | 준비 확인 뒤 개발 등록 차단, 오류 상태 검사 | B2 | 우리(View 소유 Flow) |
| `View/Boot/GameBootstrap.cs` | B1: 부트스트랩 MonoBehaviour(수명·Tick·종료). B2: 개발 설정 연결 | B1, B2 | 우리 |
| `View/Boot/GameBootstrapSettings.cs`(또는 직렬화 필드) | B1: 시작 LifeCycle·실행 모드. B2: 개발 설정(`loadoutKey`·`chapterKey`·`nodeId`) | B1, B2 | 우리 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/Common/RuntimeFaultBoundary.cs` | W17: 공유 오류 상태·정리 콜백, View/LifeCycle/Unity 구체 참조 없음. `Common`은 새 폴더(폴더 `.meta` 포함), 네임스페이스 `ProjectL.Systems.RuntimeSystem.Common` | B1 | 우리, 런타임 공통 |
| `View/Dev/DevBattleInitializationSource.cs` | 개발용 준비 입력(fixture, `ManualDrawGoldCost = 0`) | B2 | 우리, 개발 전용 |
| `Assets/ProjectL/Script/Systems/Factory/Runtime/RunTimeSystemFactory.cs`, `…/InGame/InGameSystemFactory.cs`, `…/LifeCycle/LifeCycleSystemFactory.cs` | 준비 입력·실행 모드·단일 오류 경계 전달 | B1 | **공용** |
| `Assets/Scenes/Boot.unity`, `Assets/Scenes/Combat.unity`, `ProjectSettings/EditorBuildSettings.asset` | 씬, Build Settings | B1 | 에디터 작업, **공용**(Build Settings) |
| `View/Combat/CombatViewSystem.cs` | 연결·순서의 주인 | C | 우리 |
| `View/Combat/CombatViewRefresh.cs` | 변경 신호(순수 C#) | C | 우리 |
| `View/Combat/CombatResultPlayback.cs` | W16: 세대·재생 번호·통지·후처리 토큰·취소 수명(순수 C#) | C | 우리 |
| `View/Combat/ICombatPresentation.cs`, `CombatPresentationOutcome.cs`, `ImmediateCombatPresentation.cs` | W18: 완료/실패·Cancel 계약, 불변 통지 값, 즉시 완료 | C | 우리 |
| `View/Combat/CombatSceneRoot.cs` | 씬 참조 묶음. E에서 개발 Layer 패널(버튼 두 개·기록 모드 표시) 참조 추가(W20) | C, E | 우리 |
| `Assets/Scenes/Combat.unity`의 Canvas 개발 Layer 패널 | 배치/전투 버튼·"입력 기록 모드" 텍스트, 기본 비활성(W20) | E | 에디터 작업 |
| `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | B1: 모드·경계 전달 자리, C: CombatViewSystem 등록·StopInput 연결 | B1, C | 우리(View 팩토리) |
| `View/Field/…`(좌표 변환, 셀 풀·핸들·간격, 셀 강조, 유닛 View·Presenter) | 옛 L 이식 | D | 우리 |
| `View/Hand/…`(손패 루트·View·Presenter) | 옛 L 이식 | D | 우리 |
| `View/Hud/CombatHud.cs` | 재화·남은 드로우 | D | 우리 |
| `View/Combat/CombatInteraction.cs` + 좌표 변환·선택 인터페이스 | 입력 해석(순수 C#) | E | 우리 |
| `View/Combat/CombatInputPolicy.cs` | C: 종료·목표 Layer 판단, E: 명령별 제출 조건(무상태 순수 C#, 새 서비스 등록 없음) | C, E | 우리 |
| `View/Placement/…`(고스트) | 옛 L 이식 | E | 우리 |
| `Tests/CombatViewRefreshTests.cs`, `Tests/CombatViewResultTests.cs`, `Tests/CombatInteractionTests.cs`, `Tests/CombatInputPolicyTests.cs` | 결과 수명·입력 허용·대기 계약 EditMode | C, E | 테스트 |
| `Tests/CombatResultPlaybackTests.cs` | 재생 토큰·중복/실패 통지·Abort 뒤 늦은 통지 | C | 테스트 |
| `Tests/CombatPresentationContractTests.cs` | 연출 자원 해제 계약(실제 비동기 연출과 함께) | §8 X3 | 후속 테스트 |
| `Tests/FrameworkRuntimeCompositionTests.cs`, `Tests/ChapterGraphPreparationTests.cs` | 리플렉션 시그니처·인자 배열, 기본/개발 조립 확인 | B1 | 기존 테스트 수정 |
| `Tests/RuntimeFaultBoundaryTests.cs` | 오류 재진입·정리 실패·1회 보고 | B1 | 테스트 |
| `Tests/CombatFlowTests.cs` | 준비 이후 등록 차단·기본 Live 회귀·오류 상태 등록 0 | B2 | 테스트 |
| `Tests/CombatViewLifecycleTests.cs` | Node 순서·씬 충돌 방지, Attach/입력 정지/Detach, 늦은 완료 무시 | C | 테스트 |
| `docs/superpowers/handoffs/2026-10-02-combat-view-framework-share.md` | Framework 공유(새 파일, Phase 0 작성) | 0, F | 전달 |
| `docs/superpowers/handoffs/2026-10-02-combat-view-core-notes.md` | Core 전달 메모(새 파일, Phase 0 작성), 기존 Core 전달 문서 §13.7에서 연결 | 0, F | 전달 |
| `docs/superpowers/handoffs/2026-10-01-combat-view-local-session-handoff.md` | 인계(새 파일) | F | 전달 |

바꾸지 않는 것: Core 전부, Buffer 전부, `FrameworkRuntime`, Runner·Register, 공용 입력 시스템, Combat Plan·Job. `CombatFlow`는 W8의 준비 이후 등록 차단과 W1의 오류 상태 검사만 추가하고 기존 Live 판단은 유지한다. 새 실행 모드와 F1의 정의는 설계에 두고, 실제 변경·검증은 이 계획에서 추적한다.

## Phase 0 — 기준과 공유

- [x] **0.1 기준 확인.** 브랜치·HEAD·작업 트리와 병합된 Core 범위, InGameGroup·기존 Economy 참조, Build Settings를 기록한다. 기준 `ede27fef`(골격 Phase 5 완료, 사용자 Unity 컴파일 오류 없음). 기존 테스트 실패(통합 fixture 15, Economy 5)는 알려진 항목으로 적고 착수 조건으로 삼지 않는다. 착수 때 HEAD가 바뀌었으면 차이를 다시 인수한다.
- [x] **0.2 Framework 공유 문서(새 파일).** 부트스트랩·오류 경계(W1), Factory 인자와 리플렉션 호출(W11), 모든 LifeCycle의 씬 상수(W3), 준비 이후 등록 모드(W8)를 설명한다. `FrameworkRuntime`·Runner·Core는 바꾸지 않는다는 점과 공용 파일 범위를 적는다. F1 변경과 개발 모드의 한계를 천명님께도 전달할 수 있게 준비한다.
- [x] **0.3 Core 전달 준비.** 준비 인덱스·리비전은 "당장 필요 없음"(설계 §14)과 이유, 거절 `Reason` 코드 목록을 알려 달라는 요청(HUD 문구용, 급하지 않음), 재화를 채우는 공개 경로 부재·`InitialGold` 미반영(설계 §14, 운영 전 결정, 급하지 않음). 기존 요청 총정리에 붙이지 않고 필요하면 새 파일로.
- [x] **0.4 8차 기준 점검.** 설계 §12·§13·§15~§20과 골격 K15~K18에 맞게 F1 소유권·개발 Live·F2·T1/G7, W13~W19의 입력/재생 수명·표시 비용을 확인한다. 후속 확장 조건은 §8에 연결한다. W7을 다시 질문하지 않는다. 실제 코드 작업과 외부 합의는 별도 상태로 남긴다.

**점검:** 공용 변경이 문서로 드러났는가(FW 경계). 외부 전달·합의를 완료로 표시하지 않았는가.

**보고:** 기준 결과, 공유 문서, 8차 설계와 달라진 조건 유무. 다음 Phase A의 파일·에디터 작업을 설명하고 멈춘다.

## Phase A — DB 선행: InGameGroup 새 테이블 3개

**원칙:** 범위와 값의 원본은 [DB 스키마 정렬 계획](2026-09-29-db-schema-core-alignment-plan.md) T11이다. 이 Phase는 그중 부트스트랩에 필요한 부분만 하고, 결과를 DB 계획 쪽에 기록한다(SSOT: 같은 작업을 두 계획이 따로 추적하지 않는다).

- [x] **A.1 새 테이블 3개.** T11 값대로 `Category/`(CategoryConfig_Default + 테이블), `Economy/`(StageEconomy_1 + 테이블), `Recovery/`(RecoveryOption 1~3 + 테이블)를 만들고 `InGameGroup.asset`에 등록한다(에디터 작업, 절차 제공).
- [x] **A.1a 기존 스테이지 경제 참조.** 모든 기존 `CombatStageDetail`의 Economy 키가 유효하고 등록된 StageEconomy로 해석되는지 확인한다. Phase 0 확인: `Stage/Combat`의 11개 에셋(Boss, Normal_A~J) 모두 `economy` 직렬화 필드 자체가 없어 키 0으로 읽힌다. T11 값은 11개 모두 `economy` = StageEconomy_1(200, 40), `nightLength` 500이다. 누락 참조를 T11 기준으로 채운다. 값·연결 기준이 미확정이면 임의 숫자로 통과시키지 않고 해당 데이터 결정만 미완으로 기록한다. 새 테이블 수가 맞아도 카탈로그 생성 실패면 A는 완료가 아니다.
- [x] **A.2 로스터 초기화 조건 확인.** (2026-10-02 결정·반영, §7.8) `InitializeRoster(loadoutKey)`가 현재 PlayerLoadout·Unit 에셋으로 성공하는지 코드로 확인한다(메인 캐릭터 정의 필요 여부). 부족하면 필요한 T11 항목(예: MainCharacter 유닛, PlayerLoadout_1)을 이 Phase에 더할지 사용자에게 묻는다.
- [x] **A.3 검증.** (2026-10-02 사용자 실행, 통과) 사용자에게 `DataBaseAssetTests`(특히 `Catalog_CreatesAllDataBaseGroups`) 실행을 요청한다. DB 계획 T11 체크 상태를 갱신한다.

| 검증 | 기대 |
|---|---|
| `Catalog_CreatesAllDataBaseGroups` | 통과 |
| 다른 `DataBaseAssetTests` | 새 실패 없음(기존 알려진 실패는 구분해 기록) |

**점검:** P1~P7(InGame Data 순수 C#, SO는 입력 계층). T11 나머지를 이 계획에서 따로 추적하지 않는다.

**보고:** 만든 에셋, 테스트 결과, A.2 결론. 다음 Phase B1을 설명하고 멈춘다.

## Phase B1 — 수명·씬·오류 경계

> 5차에서 옛 Phase B를 B1·B2로 나눴다. 골격 설계·계획 등 다른 문서의 "View B"는 B1·B2를 합친 이름이다. F1(`GameBootstrap`·`RuntimeFaultBoundary`)은 B1, Flow의 실행 모드·오류 검사(골격 §4.1의 "View B 예정" 행)는 B2.1이다.

**착수 조건:** Phase A. 런타임을 실제로 띄우고, 실패하면 한 번 보고하고 멈추는 것까지 확인한다. 개발 준비·실행 모드의 Flow 사용은 B2, View 입력 수신·표현 정지는 C·E에서 확인한다.

- [x] **B1.1 `SceneIndices`.** 설계 §11대로 Boot 0·Stage 1·Combat 2·Lobby 3·Artifact 4. 네 LifeCycle의 getter 모두 상수로 바꾸고, Boot에 LifeCycle을 배정하지 않는다. Lobby·Artifact 씬은 예약만 하며 현재 시작 대상으로 거부한다. 공용 변경은 getter에 한정한다.
- [x] **B1.2 Factory 인자와 기존 호출.** `CombatExecutionMode`를 정의하고, 설계 §8.3의 준비 입력·실행 모드·오류 경계를 루트에서 생성/확정해 InGame·View·LifeCycle에 전달한다. Core에는 준비 입력만 전달한다. 모드 기본값은 Live. 인자를 한 번에 확정해 리플렉션 호출을 두 번 고치지 않는다. `FrameworkRuntimeCompositionTests`의 `Invoke(null, null)`을 정확한 인자 배열로 갱신한다. `ChapterGraphPreparationTests` 도우미는 이미 기본값 배열로 호출하므로 원칙적으로 그대로 두고, B1.5에서 실행해 확인한다(Phase 0 확인). `CombatLifeCycle`은 모드·경계를 보관만 하고 Flow 전달은 B2.1에서 한다.
- [x] **B1.3 `GameBootstrap`·`RuntimeFaultBoundary`.** 설계 §10. 오류 객체는 §4의 RuntimeSystem/Common(새 폴더, 폴더 `.meta`·네임스페이스 `ProjectL.Systems.RuntimeSystem.Common`)에 두고 Bootstrap이 생성·소유한다(W17). View·Flow는 같은 인스턴스를 주입받으며 Flow에서 View/Boot 참조, 공통 객체에서 View/LifeCycle/Unity 구체 참조는 0이어야 한다. 새 서비스/인터페이스는 만들지 않는다. 이것이 골격 Phase 6에서 인수할 단일 F1이다. 초기화·Tick·시작 전환을 같은 경계로 처리한다. 최초 오류를 고정하고 정리 후 1회 보고한다. 종료는 입력/연출 정리 → runtime.Shutdown 1회 요청 → finally로 소유 참조 정리다. 부분 초기화·하위 Shutdown 실패를 정상 종료로 기록하지 않는다. 개발 설정은 B2.3, 정리 콜백은 C에서 연결한다.
- [x] **B1.4 씬과 Build Settings(에디터 작업).** Boot 0·Stage 1(`StageSelect.unity`)·Combat 2(새 `Combat.unity`). 옛 `Stage.unity`(옛 전투 씬)는 Build Settings에서 빼고 삭제하지 않는다(설계 §11, 8차 정정). Boot에 GameBootstrap·카탈로그 연결, Combat에 빈 루트·카메라·조명·Canvas·EventSystem(필요하면 옛 `Stage.unity`의 Global Volume·조명 설정을 옮김). 부팅 시 실제 활성 경로/번호와 선택한 시작 씬을 검증한다. 예약 번호의 빈 씬은 만들지 않는다.
- [x] **B1.5 EditMode·의존 확인.** 오류 경계의 재진입·정리 실패·1회 보고, 리플렉션 인자 전달(기본/개발 조립), 씬 대응 중복·예약 씬 진입 거부. Factory가 같은 경계를 전달하는지 검증하고 B1.3의 금지된 구체 타입 참조가 없는지 확인한다.
- [x] **B1.6 재생 확인(사용자).** Live + 개발 source 없음: Boot → Combat → 준비 Job 실패(기본 준비 소스의 T1/G7 또는 보유 메인 상태 없음)가 1회 보고되고 Tick·전환 정지. 실패 위치를 기록하며 준비 실패를 Live 성공으로 기록하지 않는다.

| 검증 | 기대 |
|---|---|
| 컴파일 | 오류 없음 |
| 기존 EditMode(`FrameworkRuntimeCompositionTests`, `ChapterGraphPreparationTests`, `CombatPlanTests`, `CombatFlowTests`, `JobSystemTests`) | 새 실패 없음. 사전부터 실패하던 항목은 별도 기록. 리플렉션 인자 전달 확인 |
| `RuntimeFaultBoundaryTests` | 최초 오류 1회, 재진입 거부, 정리 실패 수집 |
| 재생(Live·개발 source 없음) | 준비 실패 위치 기록, 오류 보고 1회·Tick 정지 |

**점검:** 런타임 수명은 부트스트랩 한 곳. 씬 인덱스 한 곳. 오류 상태는 런타임당 경계 하나. 정적 서비스 찾기 없음(부트스트랩 중복 방지 외).

**보고:** 파일, 재생 결과, 공용 변경(Factory·LifeCycle getter·Build Settings). 다음 Phase B2를 설명하고 멈춘다.

## Phase B2 — 실행 모드·개발 준비

> 아래 완료 항목은 최초 구현 이력이다. B2.3의 Live 전용 진행 준비와 사전 `ConfirmStageEntry`는 새 진입 경로에서 B3로 대체한다. 기록 모드의 과거 재생 성공을 병합 후 성공 증거로 사용하지 않는다.

**착수 조건:** Phase B1. 실제 준비와 등록 정책, 개발 설정의 Core 호출을 검증한다.

- [x] **B2.1 골격 실행 정책 연결.** [골격 설계 §4.1](../specs/2026-09-30-combat-flow-skeleton-design.md#41-한-tick의-판단-위에서부터-처음-맞는-줄-하나만)의 View B 확장만 구현한다. 모드·오류 검사 순서를 여기서 재정의하지 않는다. 같은 문서 §6·§10에 따라 Flow 생성자·LifeCycle·테스트 호출을 함께 갱신한다. 비개발 빌드의 RecordInputOnly 조립 거부도 여기서 한다. B2.4·C.6에서 같은 Tick의 콜백 오류 뒤 등록 0과 다음 Runtime Tick 차단을 별도로 검증한다.
- [x] **B2.2 `DevBattleInitializationSource`.** 개발 전용 폴더, `UNITY_EDITOR || DEVELOPMENT_BUILD`. 샘플 `BattleInitialization`을 fixture로 만든다. 준비뿐 아니라 이동·공격·드로우·종료 검증에 필요한 공격 프로필·경제 정의를 명시한다. `StageConfig`는 Core 규칙(배치 깊이 합 < 맵 높이)을 지킨다. 재화 경로가 없으므로 `ManualDrawGoldCost = 0`(설계 §8). Core 테스트 값을 참고하되 **DB를 읽어 운영 준비를 조립하지 않는다**(G7은 Core 몫). Core 소유 Buffer에 직접 쓰지 않는다.
- [x] **B2.3 개발 설정 연결(W12).** 설계 §8.2. 공통: `InitializeRoster(loadoutKey)` 성공 확인. Live만: `ChapterProgression.EnterChapter(chapterKey)` → `ConfirmStageEntry(chapterKey, nodeId)` 각각 `Accepted` 확인. 실패·예외는 오류 경계로 보고하고 `Change(Combat)`를 호출하지 않는다. 기본 노드를 정하는 방법(설정값 또는 Buffer 읽기)을 여기서 확정하고 §7에 적는다. 설계와 다른 결정이 필요하면 구현 전에 묻는다.
- [x] **B2.4 EditMode.** 준비 성공/실패 확인은 기록 모드에서도 유지되는지, 준비 뒤 입력이 있어도 처리 Job 등록이 0인지, 오류 상태에서 등록 0인지, Live 기존 테스트가 유지되는지 확인한다.
- [x] **B2.5 재생 확인(사용자).** RecordInputOnly + fixture: Boot → Combat → 준비 성공(`HasBattle` true), Tick 지속. Live + fixture + 개발 설정: 챕터·노드 선택 성공 뒤 Combat 진입·준비 성공. 실제 처리 통합은 E.9, 입력/Drag 정지는 C·E에서 검증한다.

| 검증 | 기대 |
|---|---|
| 컴파일 | 오류 없음 |
| 기존 EditMode(`CombatFlowTests`, `CharacterBattlePreparationTests`, `FrameworkRuntimeCompositionTests`) | 새 실패 없음 |
| 개발 Flow 테스트 | 준비 검증 유지, 준비 뒤 처리 등록 0, Live 회귀 없음, 오류 상태 등록 0 |
| 재생(RecordInputOnly) | Combat 씬 진입, 준비 성공, Tick 지속 |
| 재생(Live·개발 설정) | 로스터·챕터·노드 성공, 준비 성공. 실패하면 위치와 거절 사유 기록 |

**점검:** SSOT — 개발 상태도 Core가 기록(View·부트스트랩이 Buffer에 쓰지 않음, 맵·노드·재화 포함). DIP — 개발 입력은 Core 인터페이스 구현으로 꽂음. 개발 단축 경로는 조건부 컴파일로 격리.

**보고:** 파일, 재생 결과, B2.3에서 정한 기본 노드 방식. 다음 Phase C를 설명하고 멈춘다.

## Phase B3 — 새 전투 진입 계약 대응 (골격 6A와 공동 작업)

**착수 조건:** 골격 6A.0이 대상 전달·결과 수신·활성화 호출자를 확정한다. 기존 Bootstrap·단일 F1·fixture·씬을 재사용한다. 설계 §22와 골격 설계 §6.3을 따르며 View 안에 별도 진입 실행기를 만들지 않는다.

- [x] **B3.1 호출자 이전.** `GameBootstrap.PrepareDevProgress`의 로스터 준비를 유지하고 Live/RecordInputOnly 모두 개발 챕터 준비와 진입 대상 전달을 수행한다. Combat `ConfirmStageEntry`를 제거한다. `EnterBattle`은 등록된 진입 Job이 호출하며 ViewSystem·Interaction에서 Core를 직접 호출하지 않는다. 거절·취소·오류 뒤 무조건 `Change(Combat)`하지 않는다. 비전투 시작 경로는 보존한다.
- [x] **B3.2 활성화·입력 수명.** 이번 요청 수락과 Job 완료 뒤 준비된 전투를 한 번 활성화한다. `OnEnter`에서 중복 준비하지 않는다. 씬 선로딩 여부와 관계없이 수락 전 입력·일반 처리 0, 최초 그리기 성공 뒤 입력 연결을 보장한다. 이전 입력/결과 정리는 골격 수명 계약을 따르며 Core 번호 초기화·확인값 위조를 금지한다.
- [x] **B3.3 개발 설정·fixture 정렬.** Editor override·Tooltip·개발 문서에 두 모드의 유효 챕터/노드 전제를 반영한다. source는 명시적 chapterKey/stageKey를 받는다. 고정 정의로 여러 대상의 일치를 입증하지 않으며 골격 6A.6은 대상별 fixture를 사용한다. 실제 DB 성공은 T1/G7까지 별도 검증한다.
- [x] **B3.4 호출·표시 회귀.** 수락/정상 거절/예외, 중복 활성화 없음, 수락 전 입력 0, Live의 실제 처리, RecordInputOnly의 준비 후 처리 등록 0을 골격 6A와 함께 검사한다. 기록 모드에서도 View Tick·입력 기록은 유지한다. 일반 결과·미리보기 응답을 진입 결과 저장소로 사용하지 않는다.
- [x] **B3.5 재생·인계.** 두 모드의 Boot → 진입 → 최초 표시와 오류 경계를 검증하고 §7에 기록한다. 실제 등록 경로 증거는 골격 6A.6을 링크한다. 이후 E.7·E.8·E.9·E2.7을 실행한다. F2 이전에는 실제 이탈/재진입 성공으로 보고하지 않는다.

**파일:** `ViewSystem/Boot/GameBootstrap.cs`, 필요 시 `GameBootstrapEditorOverrides.cs`·`CombatViewSystem.cs`·입력 허용 연결 및 해당 테스트. LifeCycle·Flow·Plan·Job·Register는 골격 6A 소유다. 공유 타입은 런타임의 적절한 경계에 두고 Flow가 View/Boot 구체 타입을 참조하지 않게 한다.

## Phase C — `CombatViewSystem` 골격

> C의 최초 Immediate 구현 이력은 보존한다. 현재 병합본은 이동에 시간 기반 `CombatMovementPlayback`과 `OrderedCombatPresentation`을 사용한다(§7.20). X3의 실제 자원/취소 검증 중 이동 관련 부분은 이제 F1/F2 인수 대상이며 미래 작업으로만 남기지 않는다.

- [x] **C.1 등록.** CombatViewSystem을 View Factory에 서비스·모듈로 등록하고 모드·단일 오류 경계를 생성자로 받는다. 경계의 입력 정리 콜백에 StopInput을 연결한다. PostBindService에서 상태·결과·캐릭터·재화·입력 읽기/쓰기, 슬롯·Layer·로딩 등록만 보관한다. 기존 읽기 싱크의 Phase·Completion·Current.CommandState·S/P·TryGetNextCommand, Layer 서비스의 CurrentLayer를 사용하며 새 Core API는 만들지 않는다. InitData에서 View DB 표 두 개.
- [x] **C.1a `CombatInputPolicy`의 국면 판단.** 내부 무상태 타입에 설계 §5.5·§5.6의 IsTerminal·GetDesiredLayer(가칭)를 정의한다. 종료 조건은 이 한 함수로 공유하며 ViewSystem에 같은 조건식을 복제하지 않는다. 새 서비스 등록 없이 호출 범위의 값으로 단위 검증한다. 명령별 허용 조건은 E.1a에서 이어 붙인다.
- [x] **C.2 씬 수명(W2).** Enter priority 100, Exit -100(설계 §11). Enter는 씬 내부 SceneRoot 하나를 찾아 Attach하고 Playback.Attach로 연결 세대를 활성화한다. 준비 상태의 최초 표시 성공 뒤 Policy가 정한 Layer와 Bind를 활성화한다(통상 Deployment). 종료 상태면 Bind하지 않는다. C에서는 표시 호출 자리로 확인한다. 정상 Exit와 Shutdown은 C.4b의 연출 취소 → 표현 정리·Unbind·ClearLayer·Detach를 멱등적으로 수행한다. OnValidate와 런타임 Attach에서 참조 누락을 검사한다.
- [x] **C.3 `CombatViewRefresh`.** 설계 §5.3 표 전체를 적용한다. S/P 비교, 설치·정리·응답 신호, 미확인 결과 유실·순번 불일치·건너뜀·역행은 오류 경계로 보고한다. S == P > 0 + 결과 없음은 정상이며 재생·확인을 하지 않는다.
- [x] **C.3a `CombatResultPlayback`·Presentation 계약(W16·W18).** 설계 §9. Playback에 번호·세대·완료/실패·후처리 상태를 모으고 Begin·TryBeginFinalization·IsCurrent·CompleteFinalization·Abort를 구현한다. 새 서비스 등록·Buffer/씬 의존 없음. Presentation에는 Completed/Failed 통지와 멱등 Cancel을 정의한다. Immediate는 동기 완료·취소 no-op, 지연/실패/취소는 가짜 구현으로 검증한다. 실제 애니메이션은 추가하지 않는다.
- [x] **C.4 결과 처리 순서.** ViewSystem은 Playback.Begin으로 재생을 시작하고 현재 Tick에서 실패를 관측하면 F1에 보고한다. 완료된 토큰을 한 번 얻어 표시 반영 → 일치 명령 대기 해제 → Layer·미리보기 후처리 → 처리 번호까지 삭제 → 마지막 Acknowledge → Playback.CompleteFinalization 순서로 처리한다. 외부 호출 뒤 오류와 토큰 유효성을 재확인한다. 후처리 중 외부 입력은 차단하며 ViewSystem에 재생 상태 필드를 복제하지 않는다. 실제 표시·입력 연결은 D·E에서 붙인다.
- [x] **C.4a 정상 종료 입력 해제(W15).** 설계 §5.6. Tick에서 결과 처리 전에 Policy.IsTerminal로 판정해 SuspendSubmissions를 호출한다. Playback·Attach·명령 대기를 보존하고 결과 처리를 계속한다. Abort/Cancel·StopInput·EndBattle·씬 전환을 대신 호출하지 않는다. Unbind·ClearLayer는 멱등적이며 부분 실패는 F1. 새 결과가 없어도 입력만 해제하고 확인값을 만들지 않는다. C에서는 표현 정리 자리를 두고 E.5에서 실제 정리를 연결한다.
- [x] **C.4b 오류·Detach의 실제 연출 정리.** StopInput·Detach·Shutdown에서 Playback.Abort로 세대/토큰을 먼저 무효화하고 Presentation.Cancel로 실제 임시 자원을 정리한다. 이후 입력·씬 자원을 해제한다. 하나가 실패해도 나머지 해제를 시도하며 오류를 모아 보고한다. Callback 참조·풀 대여가 남거나 취소가 Acknowledge를 만드는 경로는 허용하지 않는다.
- [x] **C.5 EditMode.** Refresh 표의 정상/위반 전부, 거절 확인, 후처리 전 확인 금지, 즉시/지연 완료, 같은 결과 재생 방지, 중복/오래된 완료, Attach 후 최초 그리기, Node 순서·충돌 방지를 검증한다. 콜백 재진입으로 오류/Detach가 발생한 뒤 외부 호출이 정상 반환해도 결과를 확인하지 않는지 포함한다. 정상 종료 입력 해제 뒤 지연 완료는 살아 있고 최종 그리기·확인은 1회, S == P에서 종료만 관측하면 추가 확인 0인지 확인한다. 가짜 읽기와 실제 CombatViewBuffer를 사용한다.
- [x] **C.5a Playback 수명 회귀.** 가짜 Presentation으로 Playback이 쓰는 부분만 검증한다: 동기/지연 완료, Failed 뒤 Tick의 F1 1회·확인 0, 완료 통지 후 Play 예외, Play 중 재진입 Abort, Abort 뒤 늦은 통지 무시, 중복 통지, 반복 Abort·Detach, 정상 전투 종료에서 Cancel 호출 0. 가짜 자원의 잔여 수·해제 순서·부분 시작/정리 실패 같은 **자원 해제 계약 테스트는 §8 X3**(실제 비동기 연출 도입 때)로 옮겼다(설계 §9.2, 8차).
- [x] **C.6 입력 오류 경계.** IStageInputReceiver의 모든 외부 콜백을 경계로 감싼다. StopInput은 오류 플래그 이후에도 취소 정리를 실행하며 실패한 해제 하나 때문에 나머지 정리를 생략하지 않는다. 빈 View에서도 입력 예외 주입 후 수신 해제·Layer 없음·다음 Tick 없음·새 Flow 등록 없음·오류 1회를 확인한다. Drag·숨김의 실제 복구는 E에서 검증한다.
- [x] **C.7 재생 확인(사용자).** SceneRoot Attach → 준비 신호 → 입력 Bind 순서, 오류 뒤 입력 차단, 종료 때 Detach·Unbind. F2가 없는 현재 단계에서 다른 전투 재진입 성공까지 주장하지 않는다.

| 검증 | 기대 |
|---|---|
| 컴파일, 새 EditMode | 통과 |
| `FrameworkRuntimeCompositionTests` | 통과(`CombatViewSystem` 바인딩이 전투 밖에서도 성공) |
| 재생 | Attach·설치 신호 1회, 준비 전 입력 없음, 오류/종료 뒤 수신 없음 |

**점검:** S — ViewSystem에 그림·규칙·명령별 분기·재생 상태 사본 없음. Playback은 Buffer·씬·Layer를 모른다. K14 — Clear는 LifeCycle만, 삭제는 처리 번호 뒤. K1 — 마지막 결과도 확인. 정상 종료는 재생 유지, 오류/Detach는 토큰 무효화와 실제 자원 정리. 확장 시 §8 기준을 따른다.

**보고:** 파일, 테스트·재생 결과. 다음 Phase D를 설명하고 멈춘다.

## Phase D — 상태 표시 (L 이식)

**이식 규칙:** 옛 파일을 옮길 때 `StageSnapshot` → Buffer 값(`CharacterData`·`CombatStateData`), 옛 SO → ViewGroup DB(`UnitVisual`·`StageFieldView`), `ProjectL.BasicSystem.Pooling` → `CustomPool<T>`, 직접 색 변경 → `ColorScop`. Presenter는 Buffer 인터페이스를 모르고 값만 받는다. 옮긴 파일과 바꾼 의존을 §7에 적는다.

- [x] **D.1 필드.** 좌표 변환·셀 간격·셀 풀/핸들·셀 강조를 이식한다. 설계 §6.1(W19)대로 보드·레일·배치 구역의 정적 구성은 최초 설치/Attach에서 수행한다. 결과 처리마다 셀을 재생성하지 않고 호버·선택 강조는 해당 영역만 갱신한다.
- [x] **D.2 필드 유닛·구조물.** `FieldUnitPresenter`·`FieldUnitView` 이식. 앵커·방향·점유 칸·진영, `SetHiddenEntity`/`ClearHiddenEntity`. 풀 반환 때 숨김·콜백 초기화. 체력 표시는 최소한(숫자 또는 막대).
- [x] **D.3 손패.** `HandPresenter`·`HandUnitView`·`HandRoot` 이식. 슬롯·용량, 숨김 API. 필드 크기·카메라 기준 Content Root 자동 배치는 §7.14에서 추가했다.
- [x] **D.4 HUD.** 씬 Canvas의 `CombatHud`: 재화, 남은 드로우(`DrawOrder.Count − DrawCursor`).
- [x] **D.5 표시 반영 연결(W19).** ViewSystem이 Presenter에 값만 넘긴다. 유닛·구조물·손패는 ID별 기존 View를 재사용하고 새 대상만 대여·사라진 대상만 반환한다. 동일 Transform/텍스트/색 재설정을 생략하며 결과마다 풀 전체 반환/재대여를 하지 않는다. 동적 대상 전체 순회는 허용하고 별도 dirty/리비전 시스템은 측정 전 추가하지 않는다. 실제 변경이 없는 결과도 입력 후처리·확인은 그대로 수행한다.
- [x] **D.6 재생 확인(사용자).** RecordInputOnly에서 개발용 샘플 전투가 그려지고 유지되는지 확인한다. 씬 편집 절차 제공. 표시 중 예외는 동일 오류 경계로 정지하며 설치 그리기 실패 시 입력을 활성화하지 않는다.
- [x] **D.6a 표시 재사용 확인.** 가짜 표시/풀로 같은 데이터 재적용 때 추가 대여/반환·속성 쓰기 0, 대상 추가/제거 때 해당 대상만 변경됨을 확인한다. 숨김·풀 재사용으로 실제 속성이 바뀌었으면 Core 값이 같아도 최종 표시가 복구되는지 포함한다. 변경 없는 거절 결과의 확인은 C 결과 테스트로 검증한다. 목표 기기·규모·허용 기준을 정한 CPU·GC·UI 측정은 **§8 X4**로 옮겼다(설계 §6.1, 8차).

| 검증 | 기대 |
|---|---|
| 컴파일, 기존 EditMode | 새 실패 없음 |
| 재생 | 셀·레일·배치 구역·구조물·유닛·손패·HUD가 샘플 값대로 |

**점검:** SSOT — 색·간격·프리팹은 DB·씬 설정에서, 표시값은 계산(P7). 소유자 API. Hot Path 검색 없음, 풀 사용. FW-010~012·FW-014.

**보고:** 이식 목록과 바꾼 의존, 재생 결과. 다음 Phase E를 설명하고 멈춘다.

## Phase E — 입력 → 명령·미리보기

> 병합 후 E.7~E.9 재생은 골격 6A + View B3 뒤 수행한다. 기존 코드/테스트 작성과 §7의 실행 검증 상태는 구분한다. E.8 오류 증거는 골격 6B의 F1 인수에 연결한다.

> 2026-10-04 사용자 기능 재생 확인은 §7.18에 기록했다. E.7·E.9의 화면·조작 기대값은 확인됐으며, 내부 번호·Job 등록 횟수·지연 완료 회귀의 실행 결과와 E.8 예외 주입은 별도로 남긴다.

- [x] **E.1 `CombatInteraction`(순수 C#).** 설계 §7의 입력을 의도로 바꾼다. 좌표 변환·선택은 좁은 인터페이스로 받는다. Drag·모드·`PendingInputCommand` 하나(EnterCombat/EndUnitTurn 종류와 반환 번호)를 소유한다. Policy 판단 뒤 ICombatViewBufferWriteSink로만 기록한다. 일치 Command 결과를 그린 뒤에만 대기 해제, 취소·다른 명령·Advance는 대기 보존(설계 §7.2).
- [x] **E.1a `CombatInputPolicy` 확장(W14).** C.1a의 타입에 설계 §7.1의 명령별 제출·미리보기 조건을 구현한다. 호출 범위의 값만 받고 Buffer·Core 서비스·Unity 표현을 참조하지 않는다. Live Submit은 PlayerInput·단계/Layer 일치·S == P·진행 중 연출/후처리 없음, EndTurn은 활성 행위자와 선행 미처리 명령 없음도 필요하다. 큐 조회는 EndTurn 요청 시에만 기존 TryGetNextCommand(HandledCommandNumber)로 수행한다. 허용값·Core 국면을 캐시하거나 거부 입력을 다음 턴으로 예약하지 않는다. 새 서비스·인터페이스를 추가하지 않는다.
- [x] **E.2 Drag 고스트.** 옛 `PlacementGhost*` 이식. Drag 중 원본 숨김은 소유자 API. 취소 경로에서 숨김 해제.
- [x] **E.3 미리보기 요청 규칙.** 칸이 바뀔 때만 `SetPreview`, 상태 변경 결과 후 무효화, 국면·대기·선택이 유효할 때만 재요청한다. 일반 연출 중 미리보기는 허용하되 Automatic·단일 대기·종료·후처리 중 외부 입력은 차단한다(설계 §7.1). 행위자·영역·Layer 변경으로 무효가 된 Drag·선택·모드를 정리하며 이전 행위자로 재요청하지 않는다. 응답 번호는 관측하되 결과 표시는 후속 범위다. **9차:** 응답 사용(조준 선택·범위·가능 여부 표시)은 E2로 확장했다(설계 §21.1).
- [x] **E.4 Layer 동기화(W13).** 초기 그리기와 모든 Command/Advance 결과 그리기 성공 뒤 Policy가 현재 Core StagePhase에서 정한 Layer를 적용한다. CurrentLayer와 다를 때만 변경한다. 밤 종료의 Deployment 복귀, 시작 수락 뒤 Combat, 종료 시 Layer 없음이 대상이다. 대기 해제는 E.1의 일치 번호 조건이며 Layer 적용과 혼동하지 않는다. RecordInputOnly의 개발 Layer 선택은 대기·오류·종료가 없을 때만 허용하고 설계 §8.1의 Live 검사 예외를 적용한다. 공용 키 바인딩을 추가하지 않는다.
- [x] **E.4a 개발 Layer 패널(W20).** 설계 §8.1. Combat 씬 Canvas에 배치/전투 버튼 두 개와 "입력 기록 모드" 텍스트를 만들고 기본 비활성으로 둔다(에디터 작업, 절차 제공). `CombatSceneRoot`에 참조 필드를 더하고 OnValidate에서 누락을 검사한다(조건부 컴파일 없음). ViewSystem은 Attach 때 모드가 RecordInputOnly일 때만 패널을 켜고 `onClick`에 개발 Layer 입구를 등록하며, Detach·StopInput·Shutdown에서 해제·비활성화한다(멱등). 입구는 오류 경계로 감싸고, 판단은 Policy의 RecordInputOnly 예외로 하며, 허용될 때만 Drag·미리보기 정리 뒤 `ChangeLayer`를 호출한다. 새 스크립트·서비스·인터페이스를 만들지 않는다.
- [x] **E.5 `CombatViewSystem`에 연결.** 오류 경계 안에서 현재 사실 읽기 → Interaction/Policy 호출 → 표시 의도 전달. 결과 완료 Tick에서 그리기 뒤 대기·Layer·미리보기 후처리를 연결하며 재진입 오류/Detach 뒤에는 확인하지 않는다. SuspendSubmissions는 실제 고스트·숨김·Drag·모드를 정리하되 명령 대기·세대·완료 예약을 보존한다. StopInput·Detach·Shutdown의 무효화 경로와 구분한다. 종료 뒤 반복 Tick·늦은 입력에도 재Bind·Layer 재활성·미리보기 제출이 없어야 한다.
- [x] **E.6 EditMode.** `CombatInteractionTests`와 `CombatInputPolicyTests`, C의 결과 수명 테스트로 아래 회귀 표를 검증한다. 기본 Drag·이동·공격·미리보기·취소 계약도 유지한다. Policy는 값 기반 표 검증, Interaction은 실제 입력 Buffer의 번호·목록 검증, 결과 소비는 가짜 읽기·즉시/지연 Presentation으로 검증한다. ViewSystem에 Policy 조건을 중복 구현한 테스트를 만들지 않는다.
- [x] **E.7 재생 확인(사용자, RecordInputOnly).** 실제 Buffer에 배치 Drag·공격/모드 입력이 기록되고 반복 Tick의 등록은 골격 §4.1 정책을 따르는지 확인한다. 결과·처리 번호·응답 번호·확인 번호는 준비 이후 그대로여야 한다. 개발 Layer 패널의 전투 버튼으로 바꾼 Combat Layer에서도 공격 기록은 가능하지만 Core는 Deployment에 머무는지 확인한다. 경로 Move·Return은 Live의 행위자/검증 context가 필요하며 기록 모드용 가짜 context를 만들지 않는다(§7.20). 시작/턴 종료 중 하나를 기록하면 대기가 유지되고 다음 검증은 새 런타임에서 한다. 대기 중 패널 버튼을 눌러도 Layer가 바뀌지 않아야 한다. Live로 실행하면 패널이 보이지 않아야 한다.
- [x] **E.8 오류 재생 확인(사용자).** Drag 중 입력/그림 예외를 주입해 원본 숨김·고스트 복구, Unbind·Layer 해제, 후속 입력 제출 0, 다음 Tick·전환 없음, 원인 로그 1회를 확인한다. 정지 뒤 버튼을 눌러도 입력 목록이 늘지 않아야 한다.

  E.8은 골격 6B의 F1 인수와 같은 증거를 쓴다. 기존 실제 경계 EditMode 테스트부터 확인하고, 시간 기반 이동의 취소·Detach 실패·늦은 콜백은 X3와 대조한다. 수동으로만 보이는 정리 문제가 남을 때 개발 재현 수단을 보강하며, 별도 오류 소유자를 추가하지 않는다.
- [x] **E.9 개발 Live 통합.** B1·B2·B3·C·D·E 연결 + 유효 fixture + 개발 진행 준비(B3.1)를 확인하고 새 Live 런타임으로 시작한다(골격 Phase 5는 완료). 실제 명령 수락/거절·미리보기·자동 진행 결과의 소비를 검증한다. **첫 밤 종료 → Deployment 복귀 → 재배치·드로우 → 다음 EnterCombat**까지 진행 가능한 fixture를 사용한다. 시작/턴 종료 연속 입력, 선행 Move/Attack 대기 중 EndTurn, Automatic 입력 거부로 다음 행위자의 턴이 뜻밖에 종료되지 않는지 확인한다. 승리·패배는 별도 새 런타임으로 각각 검증한다. Completion 기록 후에도 마지막 지연 연출·그리기·확인은 1회 완료하고, 이후 새 명령/미리보기·Job 등록은 0이어야 한다. 일반 연출 중 미리보기 교체·취소, 후처리 실패 시 확인값 불변, 미리보기와 일반 결과 순번 분리도 포함한다. 완료 기록은 §7, Core 경로 증거는 골격 Phase 5로 연결한다. 실제 이탈·재진입은 F2 후속 범위다.

**E.6 필수 회귀 시나리오** — 구현 후 실행할 항목이며 현재 통과 기록이 아니다.

| 시나리오 | 관측할 결과 |
|---|---|
| EnterCombat 두 번, 대기 중 Draw/Preview/Cancel | 시작 명령 1개. 취소는 표현만 정리하며 대기 번호 불변 |
| EndTurn 같은 프레임 두 번, 대기 중 Move/Preview | 종료 명령 1개. 추가 Submit/SetPreview 0 |
| 미처리 Move/Attack이 있는데 EndTurn | 종료 명령 0. 기존 명령 보존, 실행 이후 행위자에게 예약하지 않음 |
| PlayerInput이지만 S > P 또는 후처리 중 콜백 재진입 | 새 명령 0. 일반 연출 중의 허용된 Preview와 구분 |
| Automatic, 잘못된 단계/Layer, 활성 행위자 없는 EndTurn | 새 명령 0. 국면을 View에서 변경하지 않음 |
| 일치 결과의 수락/거절, 다른 Command/Advance | 일치 결과를 그린 뒤만 대기 해제. 실제 재개는 P == S와 현재 국면을 따름 |
| 밤 종료가 Command/Advance로 각각 게시 | 그리기 후 Deployment. 이전 선택/Drag/미리보기 정리, 다음 배치 입력 가능 |
| Finished 또는 Completion만 관측, HasBattle은 true | 다음 View Tick 전 직접 호출도 Submit/SetPreview 0. Tick에서 Unbind·ClearLayer |
| 마지막 결과의 지연 연출 중 정상 종료 차단 | 세대·완료 예약 보존, 완료 뒤 마지막 그림/확인 1회, Layer 재활성 0 |
| 종료 반복 Tick, S == P이며 새 결과 없음 | 연출/확인 중복 0. 미처리 명령·P를 임의로 변경하지 않음 |
| 정상 종료 정리 실패 또는 후처리 재진입 오류 | F1 1회, 남은 정리 시도, 추가 확인 없음. 정상 종료와 오류 수명 구분 |
| RecordInputOnly에서 개발 Combat Layer·EndTurn | 기록 1개 후 대기, Core 국면/결과/확인값 불변, 개발 Layer 변경도 차단 |
| 개발 Layer 버튼(W20): Live 조립, 대기·오류·종료 중 클릭, Detach 뒤 클릭 | Live는 등록 0. 차단 조건에서 ChangeLayer 0. Detach 뒤 콜백 없음. 버튼 예외는 F1 1회 |

| 검증 | 기대 |
|---|---|
| 새 EditMode | 통과 |
| 입력 관련 기존 테스트(`StageInputTests`, `UnityStageInputTests`) | 새 실패 없음 |
| 재생(RecordInputOnly) | 입력 기록·정적 상태 유지, 처리 Job 등록 0, 결과 확인 위조 없음 |
| 오류 재생 | Drag·숨김 정리, 정지 뒤 제출·Tick·전환 없음 |
| 개발 Live(E.9) | 실제 결과 소비·복수 밤 진행·턴 입력 이월 방지. 승리/패배의 진행 판정 성공·Completion, 새 입력 0·최종 확인 1회 |

**점검:** 설계 §13의 다섯 원칙과 §13.4 평가 표. Drag/대기 소유자 하나, 허용 정책 한 곳, Core/Buffer/Flow의 정상 규칙 변경 없음. Layer 변경·큐 조회·미리보기는 필요한 시점에만 수행한다. Profiler 측정은 §8 X4에서 하며, 측정 전에는 성능 통과로 표시하지 않는다.

**보고:** 파일, 테스트·재생 결과. 다음 Phase F를 설명하고 멈춘다.

## Phase E2 — 미리보기 응답 사용 (9차, W21)

> E2.7 재생의 진입 전제는 골격 6A + View B3다. 응답 사용 계약은 유지하며 신규 진입 결과와 혼용하지 않는다.

> 아래 체크 항목은 최초 E2 구현/재생 이력이다. 이동 앵커 직접 기록은 `e0b02cc7`의 검증된 경로·context 계약으로 대체됐다. 현재 이동/Return 입력은 `CombatPathInput`·`CombatMovementInteraction`, 배치·공격은 `CombatInteraction`이 맡는다. 공격의 `AttackAims` 응답 사용은 유지하며 최신 이동 인수는 E.9·§7.20을 따른다.

**착수 조건:** Phase E 코드. 계기와 규칙은 설계 §21.1. 사용자 결정(2026-10-03): "항상 칸 조준" 임시 수정 없이 이 Phase로 고친다.

- [x] **E2.1 현재 요청의 응답.** ViewSystem이 사실 묶음에 "현재 요청의 응답"을 넣는다(입력 Buffer `Preview.Number` == Core `RequestNumber`일 때만). 따로 저장하지 않는다.
- [x] **E2.2 Interaction.** 모드 범위 응답(공격 조준 목록·범위 칸, 이동 앵커)을 모드 동안만 보관한다. 범위 응답 전에는 호버 요청·기록을 하지 않는다. 공격은 조준 종류(칸·방향·전체)별로 호버·기록한다. 방향은 행위자 → 포인터 각도에 가장 가까운 목록 방향(Up = +Y)이다. 이동은 응답 앵커만 기록한다. 결과 후처리·모드 종료·Stop에서 보관값과 표시를 버린다. 호버·배치 응답으로 가능 여부를 표시한다.
- [x] **E2.3 표시.** FieldView 칸 강조 층: 호버(가능·불가) > 영향 > 범위 > 현재 행위자 > 정적. 높은 층이 지워지면 다음 층 색으로 돌아간다. 고스트 가능·불가 색. 현재 행위자 칸은 표시 갱신(설치·결과)마다 Buffer의 `ActiveActorId`로 계산한다. 색은 씬 설정이다.
- [x] **E2.4 ViewSystem 연결.** Tick의 미리보기 응답 신호에서 Interaction에 응답을 전달한다(외부 입력이 아니므로 후처리 차단과 무관, 오류 경계는 Tick). 피드백 계약에 범위·영향·호버 상태·고스트 색을 더한다.
- [x] **E2.5 개발 fixture.** ShotGun을 DB 결정값(Fan/Direction, 사거리 2)으로 바꿔 방향 공격을 Live에서 검증한다. 나머지는 Single 그대로다.
- [x] **E2.6 EditMode.** 응답 일치 판정, 범위 응답 전 차단, 칸·방향·전체 조준 선택, 이동 앵커 제한, 결과 뒤 보관값 폐기·재요청, 층 우선순위(순수 계산), 기존 E 테스트의 개체 조준 기대 정정.
- [x] **E2.7 재생(사용자, Live).** 이동 모드 범위 표시 → 범위 안 이동 수락. 공격 모드 범위·대상 표시 → Single 유닛이 적 칸을 공격하면 수락. ShotGun 방향 공격 수락. 호버 가능·불가 색, 배치 고스트 색, 현재 행위자 칸. 사용자 기능 확인: §7.18.

**점검:** P6(규칙 값은 Buffer로만, InGame DB 읽기 0), SSOT(응답 원본은 Core Buffer, 보관은 모드 수명 동안의 표시·입력 캐시), Policy 조건 중복 없음.

**보고:** 파일, 테스트·재생 결과. 다음 A2를 설명하고 멈춘다.

## Phase A2 — 공격 지정 방식 DB (9차, W22)

**착수 조건:** Phase A. DB-SO 규칙(P1~P7, 코딩 규칙)과 설계 §21.2를 따른다. Core 코드는 수정하지 않는다.

A2는 6A의 fixture 진입 검증 선행 조건이 아니며 F2 전체 완료도 기다리지 않는다. 완료 범위는 DB 제공이며, Core의 기본 source 조립·T1/G7 예외 해소 및 실제 데이터 검증까지 마쳐야 운영 DB 준비가 완료된다.

- [x] **A2.1 데이터.** `TargetingMethodData`(순수 C#: 키·Shape·ChoiceMode·Splash·SplashPercent, Validate)와 SO·TableSO. UnitDataSO에 지정 방식 SO 참조를 더하고, `UnitData.TargetingMethodKey`로 바꾼다(P3). 옛 공격 필드는 유지한다.
- [x] **A2.2 조회.** `ITargetingMethodTableQuery`(`Get`/`TryGet`), 키 등록, InGame 조회 묶음·Factory 연결. 유닛의 지정 방식 키가 테이블에 있는지 교차 검사한다.
- [x] **A2.3 에셋.** 테이블과 지정 방식 2개(1 = RadialSquare/Single/None, 2 = Fan/Direction/None), InGameGroup 등록, 유닛 5개의 참조(Akimbo·Demon·Sniper·MainCharacter → 1, ShotGun → 2).
- [x] **A2.4 테스트.** 데이터 검증·조회 테스트, `DataBaseAssetTests` 교차 검증. 테이블 목록을 직접 조립하는 테스트(`ChapterGraphPreparationTests` 등)에 새 테이블을 더하고 Core 전달 문서 §5에 적는다. 작성·컴파일 완료이며 Unity 실행은 A2.6에 남긴다.
- [x] **A2.5 Core 전달.** Core 전달 문서에 "DB 지정 방식 키 → `TargetingMethodSpec` 조립, T1/G7 예외 해소" 요청과 키 표를 적는다. Ballista·CactusCannon·Gravekeeper는 유닛 에셋을 만들 때 3~5를 더한다고 적는다. 문서 작성 완료·외부 미전달.
- [x] **A2.6 검증(사용자).** Unity 컴파일, `DataBaseAssetTests`, `InGameDataTableTests`, Phase A 묶음, 재생에서 DB 생성 성공. 병합본의 자동 실행 기록은 §7.20에서 인수하며, 남은 fixture Boot 수동 재생까지 연결해 마감한다. 운영 source 성공은 A2 DB 제공 완료와 별도다.

**점검:** P1(순수 데이터에 UnityEngine 없음), P3(SO 참조는 GetData에서 키로), 옛 에셋 48개 보존, `System.Enum.IsDefined`.

**보고:** 파일, 테스트 결과, Core 요청. 다음 Phase를 설명하고 멈춘다.

## Phase F — 인계와 전달

- [x] **F.1 설계·계획 갱신.** 실제 파일·타입, 설계와 달라진 점, Phase별 결과. 병합된 경로 이동·Return·연출 구성과 서비스 노출을 기존 설계/UML에 정렬하고 옛 직접 Move/Immediate 전용 구조와의 차이를 기록한다.
- [x] **F.2 Framework 공유 결과.** 공용 변경의 실제 diff 요약(Factory·리플렉션 호출, 모든 LifeCycle의 씬 상수, Build Settings, 오류 경계·개발 등록 정책). 전달 준비와 실제 외부 전달을 구분한다.
- [x] **F.3 Core 전달.** 준비 인덱스·리비전 쓰임새(당장 불필요), `Reason` 코드 목록 요청, 실제 결과 소비 검증 결과와 남은 T1/G7·F2 입력 조립 요청.
- [x] **F.4 로컬 인계(새 파일).** [2026-10-05 공동 인계](../handoffs/2026-10-05-combat-view-skeleton-session-handoff.md)에 기록했다. 완료 범위, RecordInputOnly·개발 Live·운영 경로별 증거, F1의 골격 Phase 6 인수 링크, §8의 확장 조건과 미검증 항목을 연결한다. 반영된 이동 연출과 미구현 비이동 VFX·표시 확장·포기·이탈을 구분한다. 기록 모드와 fixture Live 성공을 운영 전투 성공으로 표시하지 않는다.

**Live 인수는 E.9에서 추적한다.** 미실행이면 F에서 부분 인계할 수 있지만 View Live 완료로 표시하지 않는다. F2와 운영 T1/G7은 이 계획 밖의 후속 조건이며 골격 Phase 6·운영 인수 기록을 연결한다.

## 5. 원칙 → Task 대응

| 원칙 | Task |
|---|---|
| SSOT: 전투 상태는 Core가 쓴다 | B1.2·B2.2·B2.3(개발 상태·진행 위치도 Core 계산), C.4·E.1(View는 입력 Buffer만 씀) |
| SSOT: 외형은 DB·씬 설정 | D.1~D.4 |
| SSOT: 씬 인덱스·런타임 수명 한 곳 | B1.1·B1.3 |
| SSOT: 결과 완료 기준·오류 상태 한 곳 | B1.3·C.3·C.4 |
| SSOT: 진행·검증 상태 기록 한 곳 | §7(설계·UML에는 적지 않음) |
| S: 연결·신호·입력·연출·그림 분리 | C.1~C.4, D.5, E.1 |
| S·I·D: 무상태 Policy, 값 전달, 좁은 경계 | C.1a·E.1a·E.5·E.6 |
| O: 새 표시·입력·연출은 추가로 | D, E.1, C.4 |
| L: 입력·연출 계약 | C.5, E.6 |
| I: 필요한 싱크만, Presenter는 Buffer 모름 | C.1, D.5 |
| D: 인터페이스 의존, 정적 찾기 없음 | B1.2·B1.3, C.2, E.1 |
| FW-001~003·§11 | C.1 |
| FW-005~007 | C.2, E.5 |
| FW-010~012·FW-014 | D |
| DR-001·DR-008 | B1.3, 전 Phase(전역 구독 없음) |
| K1·K14 | C.4·C.5 |
| W8: 실제 준비 + 이후 등록 차단 | B2.1·B2.4·E.7 |
| W5: 시작 명령 대기 보존 | E.1·E.6 |
| W9·W10: 일관성 위반·늦은 완료 | C.3~C.5 |
| W11: 리플렉션 호출 호환 | B1.2·B1.5 |
| W12: 개발 Live 진행 준비 | B2.3·E.9 |
| W13: Core 국면별 Layer·밤 종료 배치 복귀 | E.3·E.4·E.6·E.9 |
| W14: Live 입력 시점·선행 명령 없는 단일 EndTurn | E.1·E.1a·E.6·E.9 |
| W15: 종료 후 입력 해제·마지막 결과 소비 유지 | C.4a·C.5·E.5·E.6·E.9 |
| W16: 재생 수명 상태의 단일 소유자 | C.3a·C.4·C.5a |
| W17: 공유 오류 객체의 공통 영역 배치 | B1.2·B1.3·B1.5 |
| W18: 완료/실패·Cancel·자원 정리 계약 | C.3a·C.4b·C.5a(Playback 수명), 자원 해제 계약 테스트는 §8 X3 |
| W19: 정적/동적 표시 분리·View 재사용 | D.1·D.5·D.6a(재사용 구조), 규모별 측정은 §8 X4 |
| W20: RecordInputOnly 개발 Layer 패널 버튼 | E.4a·E.6·E.7 |
| Hot Path 검색 금지 | C.2, D |

## 6. 위험

| 위험 | 대응 |
|---|---|
| `InitializeRoster`가 현재 에셋으로 실패 | A.2에서 확인, 필요한 T11 항목을 사용자 결정으로 추가 |
| 샘플 `BattleInitialization`이 Core 검증을 통과하지 못함 | Core 테스트 fixture 값을 따른다. 그래도 막히면 Core 규칙을 고치지 않고 천명님께 묻는다 |
| 옛 코드 이식 중 옛 Core 의존이 숨어 있음 | 이식 파일마다 `using` 점검, `StageSessionHost`·`StageSnapshot` 참조 0 확인 |
| 결과 소비자·F1 완성 전 입력이 실제 처리를 시작 | 독립 진단은 RecordInputOnly. Live는 F1·결과 소비자·fixture·진행 준비 확인 뒤 E.9에서 실행 |
| 선택과 준비의 불일치·기록 모드 진입 실패 | B3에서 두 모드의 로스터·챕터 준비 후 골격 6A 진입 경로 사용. Current 선택은 EnterBattle이 게시 |
| DB 노드·Stage와 fixture 전투 내용이 서로 다른 스테이지 | 개발 전용 한계로 기록(설계 §8.2). 개발 Live 정산을 운영 결과로 기록하지 않음 |
| 시작 재화 0으로 드로우 거절 | fixture `ManualDrawGoldCost = 0`. 재화 경로는 Core 전달 후보. **2026-10-06 해소(§7.54)** |
| 세 테이블 등록만으로 DB 성공을 가정 | A.1a의 기존 스테이지 Economy 참조와 카탈로그 전체 생성 검증 |
| 개발 fixture Live를 운영 성공으로 오인 | T1/G7·F2·실제 씬 연동 증거 분리 |
| 기록 모드를 실제 전투로 오인 | 개발 빌드 한정·모드 표시·모드 전환은 새 런타임. Buffer 결과·확인을 위조하지 않음 |
| Tick 정지 후 Unity 입력 계속 도착 | 공통 오류 상태 + StopInput·Unbind·ClearLayer, 입력 입구 자체도 정지 검사 |
| 씬 번호가 다른 LifeCycle과 충돌 | 전체 getter의 상수 통일·중복 검증, 예약 씬 시작 거부 |
| 시작/턴 종료 연속 입력으로 대기 번호 소실 | 종류·번호의 단일 대기, 추가 제출 차단, 취소로 대기/명령 삭제 금지 |
| 선행 Move/Attack 뒤 EndTurn이 다음 행위자에게 적용 | EndTurn 제출 전 미처리 명령 없음·현재 PlayerInput·S == P 확인. 거부 입력을 예약하지 않음 |
| 밤 종료 뒤 Combat Layer에 머묾 | Command/Advance 그리기 후 현재 StagePhase로 동기화. 두 번째 밤 진입까지 검증 |
| 전투 종료 차단이 마지막 연출까지 취소 | SuspendSubmissions와 StopInput의 수명 구분. 지연 마지막 결과 확인 테스트 |
| Policy 분리가 상태 복제·과도한 계층으로 커짐 | 내부 무상태 타입 하나, 호출 범위의 값만, 새 서비스/인터페이스 없음 |
| 순번 불일치·미확인 결과 유실로 영구 대기 | 원자적 게시 계약 위반은 오류 정지. 이미 확인한 결과 없음과 구분 |
| Factory 기본값을 유지해도 리플렉션 실패 | 두 기존 테스트 파일의 정확한 시그니처·인자 배열 수정, 기본/개발 조립 각각 확인 |
| Detach 뒤 연출 완료가 새 씬에 반영 | Attach 세대 무효화·콜백은 완료 표시만·Tick에서 검사 |
| 부분 초기화 또는 하위 Shutdown 실패 | 입력 우선 정리, runtime 종료 요청 1회, Bootstrap 정리는 finally. Framework 전체 정리 성공을 임의로 주장하지 않음 |
| ViewSystem과 Playback이 같은 재생 상태를 각각 보관 | 번호·세대·통지·후처리 상태는 Playback만, ViewSystem은 토큰을 조회하고 절차 실행 |
| 늦은 콜백 무시만 구현하고 실제 연출·대여 자원은 남음 | 이동 연출은 이미 병합됐다. §8 X3의 이동 수명·Detach/실패 정리를 F1/F2에서 인수하고, 향후 비이동 VFX에도 같은 계약을 적용한다 |
| 결과마다 객체·정적 필드·UI를 전부 재구성 | D.5의 ID별 View 재사용, D.6a의 재사용 구조 확인. 규모별 측정은 §8 X4. 결과 확인은 생략하지 않음 |
| 개발 Layer 버튼이 Live나 대기 중에 Layer를 바꿈 | RecordInputOnly일 때만 등록, 버튼 입구도 오류 경계와 Policy 예외 검사(E.4a·E.6) |

## 7. 검증과 실행 기록

| Phase | 코드/문서 | 컴파일 | 테스트 | 재생 | 남은 조건 |
|---|---|---|---|---|---|
| 0 | **완료**(2026-10-02, §7.7). 공유 문서 2개 작성 | — | 코드 대조로 확인 | — | 외부 전달은 사용자 몫. 당시 Core 미병합 조건은 이후 병합으로 해소 |
| A | **완료**(2026-10-02, §7.8). A.1·A.1a·A.2 반영, Artifact 외형 테이블 정정 | 오류 없음(사용자) | 정정 뒤 `DataBaseAssetTests`·`ArtifactDataTableTests` 통과(사용자) | — | 시작 서브 수 규칙은 Core 문의(메모 §3a) |
| B1 | **완료**(2026-10-02, §7.9) | Unity 컴파일 오류 없음(사용자), MSBuild 오류 0·새 경고 0 | B1.5 6묶음 통과(사용자) | B1.6: Combat 진입 → G7 준비 실패 1회 보고·정지(사용자) | Render DB 임시 연결은 Shader 브랜치 병합 때 정리(공유 문서 §6.1) |
| B2 | **완료**(2026-10-02, §7.10, 당시 기준) | Unity 컴파일 오류 없음(사용자), MSBuild 개발/비개발 오류 0·새 경고 0 | EditMode 통과(사용자) | B2.5 확인(사용자) | 병합 후 진입 전제 변경은 B3 |
| B3 | **완료**(§7.21 사용자 확인) | 병합 검증 인수 + 사용자 완료 확인 | 기존 병합 전체 통과 기록(§7.20), 추가 수치/로그 미수신 | 잔여 검증 마감 사용자 확인 | 신규 F1 보완 검증은 별도 |
| C | **완료**(§7.11), 이동 연출 확장 반영(§7.20) | 병합 Unity 검증 기록 인수 | Playback·순서 있는 이동 연출/Detach 회귀 기록(§7.20) | 최초 C.7 사용자 확인 유지 | 이동 자원/실패 수명은 골격 6B F1·F2 인수에 연결 |
| D | **완료**(§7.12), 견적·Return UI 반영(§7.20) | 병합 Unity 검증 기록 인수 | 실제 Combat 씬 자동 검증 기록(§7.20) | 기존 D.6 사용자 확인 유지 | 카메라 줌·프레임 변화·UI 겹침 수동 확인 |
| E | **E.7~9 완료 확인**(§7.23) | `5078ee0e` 검증 완료 사용자 확인 인수 | F1 보완 포함 기존 커밋 인수 | 개별 새 로그/수치는 미수신 | 새 일반 승리 호출자 변경은 골격 §6.14 |
| E2 | **완료**(§7.21 사용자 확인) | 병합 Unity 검증 기록 인수 | 기존 응답 및 새 경로/Return 회귀 기록(§7.20) | 잔여 검증 마감 사용자 확인 | 신규 F1 보완 회귀는 E.8과 연결 |
| A2 | **완료**(§7.21 사용자 확인) | 병합 검증 인수 + 사용자 완료 확인 | Framework 전체 통과 및 실제 DB 로드 기록(§7.20) | A2.6 마감 사용자 확인 | 운영 source 조립·T1/G7·시작 재화는 Core 후속 |
| F | **문서 인계 완료(§7.24)** | 문서 작업으로 재실행 없음 | 기존 구현 증거 인수 | 문서 링크·HTML 도표 확인 | F.1~4 완료, 골격 7과 공동 인계 |

**과거 외부 기준(2026-10-02):** 골격 Phase 5 완료 결정과 `ede27fef` 당시 실패 20건은 이력으로 보존한다. 현재 기준은 §7.20의 병합 전체 통과 기록이며 과거 실패를 현재 차단 사유로 재사용하지 않는다.

### 7.1 재설계 기록 (2026-10-01)

- 사용자 요청에 따라 설계 3차와 이 계획을 수정했다. 리뷰 6건의 대응은 설계 §15를 기준으로 추적한다.
- 채택: W7 준비 입력 유지, W8 개발 기록 모드, F1 입력 오류 경계, 전체 씬 상수, 전투 시작 단일 대기, 결과 원자성 검사, Tick에서 최종 확인, 리플렉션 호출 갱신.
- 기존 기록: 리뷰 때 선택 인자를 인자 없이 리플렉션 호출하면 `TargetParameterCountException`이 발생하는 것을 별도 .NET 예제로 확인했다. Unity 테스트 통과 증거는 아니다.
- 이번 변경은 문서뿐이다. Phase 체크박스 완료, 코드 구현, Unity 컴파일·EditMode·재생 통과, 외부 합의, 커밋을 주장하지 않는다.

### 7.2 4차 재설계 기록 (2026-10-02)

- `83f485d1` 기준 Core Task 7 API·결과 발행 도착을 반영했다. 사용자 확인은 컴파일 오류 없음·EditMode 미실행이다. View 구현은 미착수다.
- Phase A에 기존 Economy 참조 검증, B에 Live용 fixture 전제, E.9에 실제 Core·View 통합을 추가했다. F1은 B·C·E에서 한 번 구현하고 골격 Phase 6으로 인계한다.
- 운영 T1/G7과 F2는 독립 조건이다. 포기는 Completion 경로를 사용하고 View가 Core 포기 입력을 조립하지 않는다. F2의 이탈 중 입력 차단은 결과 소비를 유지해야 한다.
- 이번 작업은 설계·계획·UML 정렬이다. 테스트 실행·에셋 편집·코드 구현·외부 메시지 전송은 하지 않았다.

### 7.3 5차 보완 기록 (2026-10-02)

- 사용자 요청으로 설계·계획을 코드와 다시 대조해 보완했다(설계 §17). 사용자 결정: 골격 Phase 5 완료로 보고, `CombatFlowIntegrationTests` 실패는 테스트 코드 문제로 View 진행을 막지 않는다.
- W12 추가: Live 개발 설정은 로스터 뒤 `EnterChapter`·`ConfirmStageEntry`로 진행 위치를 준비한다(B2.3). 재화는 fixture 드로우 비용 0으로 피하고 Core 전달 후보로 남긴다(0.3).
- Phase B를 B1(수명·씬·오류 경계)과 B2(실행 모드·개발 준비)로 나눴다. `CombatExecutionMode` 정의와 Factory 인자는 리플렉션 호출을 한 번만 고치도록 B1에 둔다.
- 진행·검증 상태를 이 §7 한 곳으로 모았다. 설계·UML 머리말의 상태 문구는 링크로 바꿨다.
- 문서 수정만 했다. 코드·에셋·테스트 실행·외부 전달은 하지 않았다.

### 7.4 6차 재설계 기록 (2026-10-02)

- 리뷰 문제를 수정하라는 사용자 요청과 책임경계·외부경계·유지보수성·확장성·SOLID 기준을 반영했다. 설계 §18, UML §2·§7·§8에 같은 계약을 연결했다.
- W13: Core StagePhase를 따르는 양방향 Layer 동기화와 밤 종료 후 재배치. W14: 무상태 입력 Policy, 선행 미처리 명령 없는 EndTurn, 시작/턴 종료의 단일 대기. W15: 새 입력 차단과 마지막 결과 소비 수명 분리.
- C.4a·C.5에 정상 종료 수명 검증, E.1a·E.6에 입력 정책/대기 회귀 표, E.9에 복수 밤과 승리/패배·최종 확인을 추가했다. 미리보기 응답 미도착 문구도 정정했다.
- 이번 변경은 설계·계획·UML과 현재 기준 안내뿐이다. 런타임 코드·에셋은 변경하지 않았고 Unity 컴파일·EditMode·재생·Profiler는 미실행이다. Phase 체크박스·진행 상태를 완료로 바꾸지 않는다.

### 7.5 품질 평가 반영·확장 기준 기록 (2026-10-02)

- 사용자 요청으로 평가에서 제시한 보완을 설계 7차(W16~W19)·계획·UML에 반영했다. 확장 시 조심할 점과 필요한 작업도 아래 §8에 기록했다.
- 결과 재생 상태는 CombatResultPlayback으로 분리하고 공통 오류 객체의 위치를 RuntimeSystem/Common으로 확정했다. 완료/실패 통지·Cancel·연출 자원 해제 계약과 정적/동적 표시 갱신 기준을 추가했다.
- B1의 배치/의존 검증, C.3a·C.4b·C.5a의 수명 계약, D.6a·E.9a의 표시 재사용·성능 검증으로 연결했다. 실제 애니메이션·새 표시·F2 구현을 이번 범위에 추가하지 않았다.
- 문서만 수정했다. 코드·에셋 변경, Unity 테스트·재생·Profiler 실행, 외부 전달·커밋은 하지 않았으며 Phase 완료 상태는 그대로다.

### 7.6 8차 정리 기록 (2026-10-02)

- 사용자 결정 (나): 7차 문서를 코드와 대조해 나온 수정 4건을 반영하고, 검증할 실제 대상이 없는 항목을 확장 시점으로 옮겼다(설계 §20).
- 옮긴 항목: C.5a의 자원 해제 계약 테스트 → §8 X3. D.6a의 기기·규모별 측정과 E.9a 전체 → §8 X4. C.5a·D.6a는 Playback 수명·재사용 구조 확인으로 줄였다. 인터페이스 모양(완료/실패 통지·Cancel)은 그대로 확정이다.
- W20 추가: RecordInputOnly의 개발 Layer 선택은 Combat Canvas 버튼(E.4a). 공용 키 바인딩·새 스크립트·서비스 없음.
- 정정: UML §8 상태도의 실제로 없는 전이 2개, 설계 §4 구조도의 Live 진행 준비 누락, `RuntimeSystem/Common` 새 폴더·네임스페이스 명시.
- 설계는 여기서 고정하고 Phase 0으로 넘어간다. 문서만 수정했으며 코드·에셋·Unity 실행·외부 전달은 하지 않았다.

### 7.7 Phase 0 실행 기록 (2026-10-02)

**0.1 기준 확인** (git·파일 대조. Unity 컴파일·테스트는 이번에 실행하지 않음)

| 항목 | 결과 |
|---|---|
| 브랜치·HEAD | `feature/Yonggeun/Stage`, `ede27fef`. 원격 `origin/feature/Yonggeun/Stage`와 같음 |
| 작업 트리 | 문서 7개 수정(스테이징됨: CLAUDE.md, 골격 계획·설계·인수 안내, View 설계·계획·UML). Phase 0에서 공유 문서 2개 추가, Core 전달 문서 §13.7 추가. 코드·에셋 변경 없음 |
| 병합된 Core 범위 | `83f485d1`까지. Core 브랜치에 **`069327c3`(옛 프레임워크 전 구조 삭제, 8R1~8R6)**이 새로 있음 — 344개 파일, 대부분 `CoreCaculator` 옛 구조·옛 테스트 삭제 |
| `069327c3` 영향 | `CombatProcessingService`·Buffer·Factory·LifeCycle·View·Job·DataBaseSO 변경 없음. 설계가 인용한 `CombatTurnCalculator`·`CombatCommandCalculator`도 삭제·수정 없음. `git merge-tree` 결과 충돌 없음. Stage 브랜치에만 있는 두 커밋의 `.cs`는 삭제된 타입을 참조하지 않음. Core 쪽 실행 결과는 1185/1190(실패 5건은 Economy 누락, 우리와 같음) |
| 8R6 문서의 View 관련 사실 | Runner는 예외 Job을 멈추지 않음 → F1 필요(우리 설계와 같음). "세 Job 미연결" 표는 Core 브랜치 기준이며 우리는 이미 연결. `EndBattle`은 Completion·최신 결과·번호를 유지(설계 §5.6과 같음) |
| InGameGroup | 테이블 9개: StageMap, StageRoot, Field, Unit, Structure, EnemyLoadout, PlayerLoadout, UnitRoster, ArtifactDefinition. Category·Economy·Recovery 폴더·에셋 없음 |
| 기존 Economy 참조 | `Stage/Combat` 11개(Boss, Normal_A~J) 모두 `economy` 필드 자체가 없음 → 키 0 → `Validate()` 예외. T11 값: 11개 모두 StageEconomy_1 |
| Build Settings | `Assets/Scenes/Stage.unity`(0번) 하나. Boot·Combat 씬 없음. 그 밖의 씬 파일(HMUTest, JMHTest, StageSelect, Title)은 Build Settings에 없음 |
| 테스트 기준 | 사용자 실행 EditMode 1622/1642(`ede27fef`). 실패 20건(통합 fixture 15, Economy 5)은 알려진 항목이며 착수 조건이 아님. `DataBaseAssetTests`는 이번에 다시 실행하지 않음(Phase A.3에서 실행) |

**0.2 Framework 공유 문서:** [2026-10-02-combat-view-framework-share.md](../handoffs/2026-10-02-combat-view-framework-share.md). 공용 파일 변경표, 씬 번호 변경표(지금 Lobby 0·Artifact 2가 새 번호와 겹침), F1 근거, 개발 모드·개발 Layer 버튼, 바꾸지 않는 것, 확인 요청 Q1~Q5, 천명님께 알릴 것. 외부 전달 없음.

**0.3 Core 전달 준비:** [2026-10-02-combat-view-core-notes.md](../handoffs/2026-10-02-combat-view-core-notes.md), 기존 Core 전달 문서 §13.7에서 연결. 준비 인덱스·리비전 불필요(답변), 코드에서 모은 거절 `Reason` 목록 확인 요청(직접 문자열 11종 + Attack·Movement·Draw 실패 enum), 시작 재화 공개 경로 질문, 개발 전용 진행 준비 경로 알림. 외부 전달 없음.

**0.4 8차 기준 점검**

| 점검 | 결과 |
|---|---|
| 골격 §4.1과 View 입력 정책 | 일치. Flow 5행(PlayerInput·S≤P·미처리 명령)과 View Submit 조건, 6행(연출 중 미리보기)과 §7.1 미리보기 허용, 9행(Finished 등록 없음)과 W15가 서로 맞음 |
| K15~K18 | 일치. K16 F1 단일 구현(B1·C·E → 골격 Phase 6), K17 포기는 Completion 경로(View는 포기 입력을 조립하지 않음), K18 개발 Live와 운영 분리 |
| W17 위치 | 골격 문서는 F1의 소유만 말하고 폴더를 정하지 않아 충돌 없음. Flow → `RuntimeSystem/Common` 참조는 의존 방향상 문제없음 |
| W13 국면 전이 | 코드와 일치(UML §8은 8차에서 정정) |
| F2·T1/G7 | 범위 밖 유지. 8R6 남은 일 표와도 같음 |
| 발견 1 | `ChapterGraphPreparationTests` 도우미는 이미 기본값 배열로 Factory를 호출한다. 리플렉션 수정은 `FrameworkRuntimeCompositionTests` 하나로 줄어든다(§2·B1.2·설계 §8.3 정정) |
| 발견 2 | 기존 전투 스테이지 11개에 `economy` 필드가 없다(A.1a에 기록) |

설계 결정(W1~W20)과 달라진 조건은 없다. 외부 전달·답변·Core 병합은 사용자 몫이다.

### 7.8 Phase A 실행 기록 (2026-10-02, 진행 중)

사용자가 직접 진행을 요청해 에셋 YAML을 직접 작성했다(DB-SO 이전 Phase E와 같은 방식). Unity가 열면 형식을 다시 쓸 수 있으며 값이 같으면 그대로 커밋해도 된다.

**A.1 새 테이블 3개** (`Assets/ProjectL/DataBaseSO/InGameGroup/`, 폴더 `.meta` 포함)

| 파일 | 값 |
|---|---|
| `Category/CategoryConfigDataTable.asset`, `Category/Item/CategoryConfig_Default.asset` | heavyDamageReductionPercent 50, heavyInitialFacing R0 |
| `Economy/StageEconomyDataTable.asset`, `Economy/Item/StageEconomy_1.asset` | economyKey 1, initialGold 200, manualDrawGoldCost 40 |
| `Recovery/RecoveryOptionDataTable.asset`, `Recovery/Item/RecoveryOption_1~3.asset` | 1: Flat 10·비용 2 / 2: PercentOfMax 30·비용 5 / 3: Full·비용 10 |
| `InGameGroup.asset` | 새 테이블 3개 등록(9 → 12개) |

**A.1a 기존 스테이지 경제 참조:** `Stage/Combat`의 11개 에셋에 `economy: StageEconomy_1`, `nightLength: 500`을 추가했다(T11 값).

**정적 확인:** 새·수정 에셋의 모든 GUID 참조가 실제 `.meta`로 해석되고(미해석 0), 새 GUID는 프로젝트에서 유일하다. 스크립트 GUID는 각 SO `.cs.meta`에서 가져왔다.

**A.2 로스터 초기화 조건 — 결정 대기.** 코드 대조 결과, 지금 에셋으로는 **DB 생성 자체가 실패**한다(로스터 이전 단계).

| 막는 것 | 근거 |
|---|---|
| `PlayerLoadout_1`에 메인 캐릭터가 없음 | `PlayerLoadoutTable` 생성자가 `Validate()`를 부르고, `MainCharacterKey == None`이면 예외. 그래서 Economy를 고쳐도 `Catalog_CreatesAllDataBaseGroups`는 이 예외로 실패한다 |
| MainCharacter 유닛 에셋이 없음 | Unit 에셋은 Akimbo·Demon·ShotGun·Sniper뿐. `EntityKey.Unit_MainCharacter = 14`는 있음. 값은 [차이 문서](../specs/2026-09-29-develop-4week-so-diff.md) §2.1에 있음 |
| 시작 서브 캐릭터 수 | Core `CharacterRosterRules.StartingSubCount = 3`이고 정확히 3명이어야 한다. 지금 구성은 Akimbo 2·ShotGun 1·Sniper 1 = 4명, T11 구성(Akimbo·ShotGun·Sniper·CactusCannon 각 1)도 4명이라 둘 다 `InvalidRequest` |
| UnitVisual | 새 유닛을 넣으면 `EveryUnitAndStructure_HasUnitVisual`을 위해 ViewGroup `UnitVisual`도 필요(T12: 임시 프리팹 `TestUnit_Base.prefab`) |

**A.2 결정(2026-10-02, 사용자):** 메인 캐릭터는 유닛·외형·로드아웃까지 이번에 추가한다. 시작 서브는 Akimbo·ShotGun·Sniper 각 1(3명)로 하되, 시작 서브는 이후 덱 상황에 따라 달라질 예정이므로 Core에 규칙을 문의한다([Core 메모 §3a](../handoffs/2026-10-02-combat-view-core-notes.md)).

| 반영 파일 | 내용 |
|---|---|
| `InGameGroup/Unit/Item/Unit_MainCharacter.asset`(+ `.meta`) | 차이 문서 §2.1 값: key 14, role MainCharacter, PlayerOnly, 1x1, 체력 50, 이동 5/1, RadialSquare 1·SingleTarget, Base, 공격 1, 행동 5/1, 우선 30·지연 20, Trait 없음 |
| `InGameGroup/Unit/UnitDataTable.asset` | MainCharacter 등록(4 → 5개) |
| `ViewGroup/UnitVisual/Item/UnitVisual_MainCharacter.asset`(+ `.meta`) | key 14, 임시 프리팹 `TestUnit_Base`(Demon과 같은 참조), 표시 이름 "MainCharacter"(기존 외형과 같이 영문. 한글화는 T12) |
| `ViewGroup/UnitVisual/UnitVisualDataTable.asset` | 등록(5 → 6개) |
| `InGameGroup/Loadout/Item/PlayerLoadout_1.asset` | `mainCharacter` = Unit_MainCharacter, Akimbo 2 → 1. 시작 손패 4·시드 그대로 |

**코드로 따라간 결과(실행 아님):** `UnitData.Validate`의 메인 캐릭터 조건(PlayerOnly·1x1·RadialSquare 1·SingleTarget·Trait 없음)과 공격 프로필 조건을 만족한다. `PlayerLoadoutData.Validate`(메인 지정·중복 없음)를 만족한다. `InitializeRoster`의 서브 수(정확히 3), 메인 자격(Role MainCharacter·PlayerOnly), 서브 자격(Regular·PlayerOnly), 초기 메인 체력(현재 = 최대)을 만족한다. GUID 참조는 모두 해석되고 새 GUID는 유일하다.

**A.3 검증 요청(사용자 실행):** Unity 컴파일 후 EditMode에서 `DataBaseAssetTests` 전체, `ArtifactDataTableTests`, `InGameDataTableTests`, `ViewDataTableTests`, `CharacterBattlePreparationTests`. 기대: Economy 누락 실패 5건이 사라지고 새 실패 없음.

**A.3 결과(2026-10-02, 사용자 실행):** 요청한 테스트 모두 통과. Phase A 완료.
**A.3 이후 발견·정정(2026-10-02, B1.6 재생 중):** 부트스트랩이 `[ArtifactViewData] 아이콘이 필요합니다. ID: 1001`로 DB 생성에 실패했다. `ViewGroup/Artifact/ArtifactViewDataTable.asset`에 아이콘이 빈 외형 3개(1001~1003)가 등록돼 있었기 때문이다. Artifact 인계 문서와 `ArtifactDataTableTests`의 의도는 "등록 보류(테이블 비움)"였고, `8f38e2c1`부터 어긋나 있었으나 Economy 예외에 가려 보이지 않았다. 사용자 결정 (가): 테이블 등록만 비우고 SO 3개는 보존했다([호진님 전달 메모](../handoffs/2026-10-02-artifact-view-table-note.md)). 같은 이유로 A.3의 `Catalog_CreatesAllDataBaseGroups`·`ArtifactDataTableTests` 카탈로그 테스트는 정정 전 HEAD에서 통과할 수 없었으므로, **정정 뒤 다시 실행해 Phase A 완료를 확인한다.** 나머지 View 테이블(특성·회복 옵션·레일웨이·입력 설정)은 현재 에셋 값이 검증을 통과함을 코드 대조로 확인했다.

### 7.9 Phase B1 실행 기록 (2026-10-02, 완료)

사용자가 직접 진행을 요청했다. Core `069327c3`은 병합하지 않은 상태에서 작업했다(병합 충돌 없음은 Phase 0에서 확인).

**새 파일** (모두 `.meta` 포함. 경로는 `Assets/ProjectL/Script/Systems/` 기준)

| 파일 | 내용 |
|---|---|
| `RuntimeSystem/LifeCycleSystem/Base/SceneIndices.cs` | Boot 0·Stage 1·Combat 2·Lobby 3·Artifact 4 상수와 실제 등록 씬 경로 3개(Boot·Stage·Combat) |
| `RuntimeSystem/Common/RuntimeFaultBoundary.cs` (새 폴더 `Common`) | `IsFaulted`·`FirstError`, `ConnectStopInput`(하나만), `Report`(정지 먼저 고정 → 입력 정리 → 원인·정리 실패를 1회 로그, 재진입·후속 보고 무시), `StopInputForShutdown`(정상 종료용, 오류 상태 불변). `using System`만 참조 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatExecutionMode.cs` | `Live = 0`, `RecordInputOnly = 1` |
| `RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs` (새 폴더 `Boot`) | MonoBehaviour. 직렬화: `databaseCatalog`, 시작 구간(Combat·Stage만 선택 가능 → 예약 씬은 시작 대상에서 제외), `executionMode`. Awake: 중복 파괴·`DontDestroyOnLoad`·경계 생성. Start: Boot 씬·Build Settings 경로 대조 → DB 생성 → 런타임 조립(경계 전달) → 수명 4단계 → 시작 구간 `Change`. 실패는 경계에 보고하고 Tick을 시작하지 않음. Update: 실행 중·오류 없음·종료 전일 때만 Tick, 예외는 경계에 보고. 종료: 플래그 → 입력 정리 → `Shutdown` 최대 1회 → finally 참조 정리, 하위 Shutdown 실패는 종료 실패로 로그 |
| `Assets/Tests/EditMode/Framework/RuntimeFaultBoundaryTests.cs` | 8개: 생성자 null, 최초 오류 고정·정리 1회·로그 1회, 정리 중 재진입 무시·정리 전 정지 고정, 정리 실패를 원인과 함께 수집, 정리 콜백 없이 동작, null 보고 거부, 콜백 하나만 연결, 종료용 정리는 오류 상태 불변 |

**수정 파일**

| 파일 | 내용 |
|---|---|
| `LifeCycles/{Lobby,Stage,Artifact,Combat}/…LifeCycle.cs` | `SceneIndex` getter를 `SceneIndices` 상수로(TODO 주석 제거). 공용 변경은 getter 한 줄 |
| `LifeCycles/Combat/CombatLifeCycle.cs` | 생성자로 실행 모드·오류 경계를 받아 `ExecutionMode`·`FaultBoundary`로 보관. Flow 전달은 B2.1 |
| `Factory/Runtime/RunTimeSystemFactory.cs` | `Create(battleSource = null, executionMode = Live, faultBoundary = null)`. 경계가 없으면 `Debug.LogException` 기본 경계 하나를 만들어 InGame·LifeCycle에 같은 인스턴스 전달 |
| `Factory/Runtime/InGame/InGameSystemFactory.cs` | 같은 세 인자. Core에는 `battleSource`만, View에는 모드·경계만 |
| `Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | 모드·경계 인자 자리(Phase C에서 CombatViewSystem에 전달). 지금은 미사용 |
| `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | 모드·경계 인자, `new CombatLifeCycle(mode, faults)` |
| `Assets/Tests/EditMode/Framework/FrameworkRuntimeCompositionTests.cs` | `CreateRuntime`을 정확한 서명·인자 배열로 호출. 추가 3개: 같은 경계·모드가 CombatLifeCycle까지 전달, 무인자 조립은 Live·기본 경계, LifeCycle 씬 번호가 상수와 같고 중복·Boot 배정 없음 |

**정적 확인**
- MSBuild(VS 2022)로 `ProjectL.Tests.Framework`와 참조 프로젝트를 빌드: **오류 0, 새 경고 0.** (첫 로그에는 경고가 빠져 "경고 0"으로 적었으나, 다시 빌드한 로그에서 `ProjectL.Systems`의 기존 경고 5건(골격 계획 §6.8과 같은 CS0105 4건·CS8632 1건)을 확인했다. B1 파일에서 나온 경고는 없다.) Unity가 아직 csproj를 다시 만들지 않아 새 `.cs`는 임시 targets로 Compile에 넣었다(산출물은 세션 스크래치패드, 프로젝트 csproj는 수정하지 않음). 새 타입·테스트가 산출 DLL에 들어간 것을 확인했다. Unity 컴파일 통과를 뜻하지는 않는다.
- `ChapterGraphPreparationTests` 도우미(기본값 배열 호출): .NET 리플렉션에서 enum 선택 인자의 `DefaultValue`가 enum 값으로 넘어가 정상 호출되는 것을 별도 예제로 확인했다. Unity(Mono)에서는 B1.5 실행으로 확인한다.
- 의존 방향: `RuntimeFaultBoundary`는 `System`만 참조. LifeCycle 폴더에 View·Boot 참조 없음. 새 네임스페이스 `RuntimeSystem.Common`과 기존 `Contracts.Common` 부분 참조 사이 충돌 없음.

**정정(2026-10-02, 사용자 질문으로 발견):** 처음 작성한 `SceneIndices.StagePath`는 `Stage.unity`였으나, 그 씬은 옛 전투 씬이고 스테이지 맵은 `StageSelect.unity`다. 사용자 결정 (가): Stage(1) = `StageSelect.unity`, Combat(2) = 새 `Combat.unity`, 옛 `Stage.unity`는 Build Settings에서만 뺀다. `SceneIndices.StagePath`·설계 §11·W3·Framework 공유 문서 §2.1을 고쳤다. 상수 문자열만 바뀌어 재빌드는 생략했다. 확인 중 런타임에 들어 있지 않은 `AudioSystem`(AudioManager 필수)은 영향이 없고, `UISystem`은 씬에 UI가 없어도 경고만 남김을 확인했다.

**남은 것**
- B1.4 에디터 작업(사용자): Boot·Combat 씬 생성, Build Settings 순서 Boot 0·StageSelect 1·Combat 2(옛 Stage 제외), Boot 씬에 GameBootstrap 배치.
- B1.5 실행(사용자): 컴파일, `RuntimeFaultBoundaryTests`, `FrameworkRuntimeCompositionTests`, `ChapterGraphPreparationTests`, `CombatPlanTests`, `CombatFlowTests`, `JobSystemTests`.
- B1.6 재생(사용자): Live·개발 source 없음으로 Boot 재생.
**추가(2026-10-02, 사용자 요청):** Bootstrap 역할을 설계 §10.0에 표로 고정했다(Framework는 구동 계약 FW-016만 두고 구동자는 비워 둠, DR-001). 다른 작업자가 씬에 붙는 시스템을 만들 수 있도록 [런타임과 씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md)를 썼다. 안내서의 예시 코드(스테이지 맵 SceneRoot·View 시스템)는 저장소에 넣지 않았고, 스크래치패드에서 `ProjectL.Systems`와 함께 MSBuild로 컴파일해 오류·새 경고가 없음을 확인했다.
**추가(2026-10-02, 사용자 요청): Play From Boot 토글.** 매번 Boot 씬을 열어 Play하는 불편을 덜기 위한 에디터 전용 편의 기능이다. `Assets/ProjectL/Script/Tool/Editor/PlayFromBootToggle.cs`(+ `.meta`, `Assembly-CSharp-Editor`, 네임스페이스 `ProjectL.Tool.Editor`). 메뉴 `ProjectL > Play From Boot`, EditorPrefs(이 PC만, 기본 꺼짐), 켜면 `EditorSceneManager.playModeStartScene`을 Build Settings의 첫 번째 켜진 씬으로 지정하고 Build Settings 변경 때 다시 적용한다. 게임 코드·설계 결정은 바꾸지 않는다. MSBuild로 `Assembly-CSharp-Editor`와 함께 컴파일해 오류 0을 확인했다(Unity 미실행).
**B1.6 재생 중 발견 2 — Render DB(DR-003) 임시 연결(2026-10-02, 사용자 결정 나):** Artifact 테이블을 정정한 뒤 재생하자 `OutlineSystem.Initialize`에서 NullReferenceException이 났다(Material 테이블 미연결, Framework DR-003). 부트스트랩은 초기화 실패를 보고하고 시작 전환·Tick을 하지 않았다(F1 초기화 실패 경로 동작). 민형님 `origin/feature/JMH/Shader`의 같은 변경(코드 두 줄 주석 해제, `MetrialDataSO.cs`→`MaterialDataSO.cs` GUID 유지)을 따라 하고, `_Outline` 키가 그 브랜치에 없어 `DataBaseSO/RenderGroup/TempMaterialTable`(키 `_Outline` → `SelectionOutline.mat`)을 임시로 만들어 `RenderGroup`에 등록했다. MSBuild(`ProjectL.Systems`, 옛 경로 제외·새 경로 포함) 오류 0·새 경고 0. Shader 브랜치 병합 때 정리 방법과 Q6은 [Framework 공유 문서 §6.1](../handoffs/2026-10-02-combat-view-framework-share.md).
**B1 검증 결과(2026-10-02, 사용자 실행):** Unity 컴파일 오류 없음. EditMode: Phase A 재확인(`DataBaseAssetTests`·`ArtifactDataTableTests`)과 B1.5 6묶음(`RuntimeFaultBoundaryTests`·`FrameworkRuntimeCompositionTests`·`ChapterGraphPreparationTests`·`CombatPlanTests`·`CombatFlowTests`·`JobSystemTests`) 모두 통과. B1.6 재생(Live·개발 source 없음): Boot → Combat 진입 → 준비 Job에서 `G7: 현재 챕터와 스테이지 선택이 필요합니다.` **1회 보고 후 정지**(반복 없음). 앞선 두 재생에서 초기화 실패(Artifact 외형 테이블, Render DB) 때도 1회 보고·전환 없음이 관찰됐다. **Phase B1 완료.** 남은 정리: Render DB 임시 연결(공유 문서 §6.1, Shader 브랜치 병합 때), 공유 문서·메모 전달(사용자).
**View 작업자 공유 문서(2026-10-02, 사용자 요청):** B1과 그 과정의 변경이 다른 View 작업자에게 주는 영향을 [View 작업자 공유](../handoffs/2026-10-02-view-workers-runtime-share.md)로 정리했다(Boot 시작·Play From Boot, 씬 번호·옛 `Stage.unity` 제외, 두 층 구조, `BindScene` 1회와 `UISystem`, `AudioSystem` 미등록, F1, Factory 서명·리플렉션, DB 변경, Render DB 임시 연결, 담당별 확인표). 전달은 사용자.

### 7.10 Phase B2 실행 기록 (2026-10-02, 코드 완료·검증 대기)

사용자가 직접 진행을 요청했다. 설계와 다른 결정은 없다.

**B2.3 기본 노드 방식(확정):** 설정값. `GameBootstrap`의 직렬화 필드 `devNodeId`의 기본값은 2다. Core 챕터 그래프 생성기는 시작 노드를 1로, 깊이 1의 첫 노드를 2로 만들고, 시작 노드를 깊이 1의 모든 노드와 연결한다. 그래서 어떤 맵에서도 2는 `ConfirmStageEntry`가 받는 노드다. Buffer를 읽어 고르는 방식은 쓰지 않았다(부트스트랩이 Core 상태 Buffer를 읽는 경로를 새로 만들지 않음). 개발 Live에서 진행 판정은 DB 맵의 노드 2를, 전투 내용은 fixture를 따르므로 둘은 같은 스테이지가 아닐 수 있다(설계 §8.2).

**새 파일** (`.meta` 포함, 경로는 `Assets/ProjectL/Script/Systems/` 기준)

| 파일 | 내용 |
|---|---|
| `RuntimeSystem/InGameSystem/ViewSystem/Dev/DevBattleInitializationSource.cs` (새 폴더 `Dev`) | 파일 전체가 `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 안에 있다. `IBattleInitializationSource` 구현으로, DB는 읽지 않고 처음 읽을 때 정의를 한 번 만들어 보관한다. 맵 8×7, 예비 5, 배치 깊이 2/2(합 4 < 높이 7), 시드 42, 밤 500. 메인 (3,0), Demon (3,5), 적 본부 (4,6)(체력 3, 공격 안 함). 유닛 5종은 로스터 키 4종과 적 Demon이다. 모두 1칸·LiftAndPlace 이동 5/1·RadialSquare·SingleTarget·Base·특성 없음. 카테고리 4종 정의, 공격 프로필은 유닛마다 단일 대상. 경제 `(0, 0)`. 수치는 흐름 검증용이다 |

**수정 파일**

| 파일 | 내용 |
|---|---|
| `LifeCycles/Combat/CombatFlow.cs` | 생성자에 실행 모드·오류 경계 추가(null·정의되지 않은 값 거부, 비개발 빌드의 RecordInputOnly 거부). 골격 §4.1: 0행 `IsFaulted`면 `BeginPreparation`·`Tick` 즉시 반환, 4a행 RecordInputOnly는 준비 확인 뒤(`HasBattle` 뒤) 처리 Job을 등록하지 않음. 그 밖의 분기 순서는 그대로 |
| `LifeCycles/Combat/CombatLifeCycle.cs` | 보관한 모드·경계를 Flow 생성자에 전달 |
| `ViewSystem/Boot/GameBootstrap.cs` | 개발 설정 필드 `useDevBattle`(기본 켬)·`devLoadoutKey` 1·`devChapterKey` 1·`devNodeId` 2. 개발 정의에서 시작 구간이 Combat이고 설정이 켜져 있으면 fixture를 Factory에 넘긴다. 수명 4단계 뒤, 시작 전환 앞에서 `PrepareDevProgress`를 부른다. 공통으로 `InitializeRoster`, Live만 `EnterChapter` → `ConfirmStageEntry`를 부르고, 거절은 사유를 담은 예외로 경계에 보고해 `Change`를 부르지 않는다. 비개발 정의에서는 설정을 읽지 않고, 켜져 있으면 경고 1회만 남긴다 |
| `Assets/Tests/EditMode/Framework/CombatFlowTests.cs` | 공용 경계·`CreateFlow(mode)` 도우미. 생성자 검사에 경계 null·잘못된 모드를 추가했다. 새 테스트 5개: 오류 상태의 `BeginPreparation` 등록 0·예외 없음, 오류 상태의 `Tick` 등록 0·확인/로그 없음, `Reset`은 공유 오류를 풀지 않음, RecordInputOnly는 준비 확인 뒤 명령·미리보기·자동 진행 입력이 있어도 등록 0, RecordInputOnly도 준비 실패를 보고 |
| `Assets/Tests/EditMode/Framework/CombatFlowIntegrationTests.cs` | Flow 생성자 호출만 갱신(Live·로그 없는 경계). 기존 fixture 실패 15건은 그대로다(보류 항목) |

**정적 확인**
- MSBuild(VS 2022), `ProjectL.Tests.Framework`와 참조 프로젝트: 오류 0, 새 경고 0(기존 `ProjectL.Systems` 5건과 테스트 CS0649 1건만 남음). 새 타입·테스트가 산출 DLL에 들어 있다.
- 비개발 정의: `ProjectL.Systems` 하나만 `UNITY_EDITOR*`·`DEVELOPMENT_BUILD`를 빼고 빌드했다(임시 targets, 참조 프로젝트는 원래 정의를 유지). 오류 0, 새 경고 0. 산출 DLL에 `DevBattleInitializationSource`가 없고, `#else` 경고 분기와 Flow의 기록 모드 거부 분기가 컴파일된다. Unity 플레이어 빌드를 대신하지 않는다.
- SSOT: 부트스트랩·fixture는 Buffer에 쓰지 않는다. 로스터·진행 위치·전투 상태는 Core 공개 서비스가 기록한다. DIP: fixture는 Core의 `IBattleInitializationSource` 구현으로 꽂힌다.

**검증 요청(사용자)**
- B2.4 EditMode: `CombatFlowTests`(새 5개 포함), `FrameworkRuntimeCompositionTests`, `CombatPlanTests`, `JobSystemTests`, `CharacterBattlePreparationTests`. `CombatFlowIntegrationTests`는 컴파일만 확인한다(기존 15건 실패는 보류).
- B2.5 재생(Boot 씬, Start Life Cycle = Combat, Use Dev Battle 켬): (1) RecordInputOnly는 Combat 진입, 오류 로그 없음, 준비 뒤 처리 Job 없이 Tick이 계속된다. (2) Live는 로스터·챕터·노드 수락, G7 없이 준비가 성공하고 오류 로그가 없다. 실패하면 로그의 거절 사유를 §7에 적는다.

### 7.11 Phase C 실행 기록 (2026-10-02, 코드 완료·검증 대기)

사용자가 직접 진행을 요청했다. B2의 Unity 검증 전에 이어서 작성했다.

**새 파일** (모두 `.meta` 포함. 새 폴더 `RuntimeSystem/InGameSystem/ViewSystem/Combat/`, 네임스페이스 `…ViewSystem.Combat`)

| 파일 | 내용 |
|---|---|
| `CombatViewSystem.cs` | 조정자. 생성자는 (실행 모드, 오류 경계, `ICombatPresentation`)이다. `PostBindService`는 상태·결과·입력 읽기/쓰기 싱크, 수신 슬롯, Layer, 로딩 등록을 꺼내 `BindServices`(테스트 입구)에 넘긴다. `Initialize`는 Combat 씬 Enter 100(`OnCombatSceneEntered` → SceneRoot 정확히 1개 확인 → `Attach`)과 Exit -100(`Detach`)을 등록한다. Tick 순서: 종료면 `SuspendSubmissions` → Refresh 관측(위반은 F1) → 전투 정리/설치(설치면 그리기 자리 → Policy Layer로 Bind) → 결과 소비(Playback.Begin → 실패면 F1 → 완료 토큰이면 그리기 자리 → Layer 동기화 → 처리 번호까지 삭제 → Acknowledge → CompleteFinalization). 외부 호출 뒤마다 오류·토큰을 다시 확인한다. `StopInput`·`Detach`는 Abort → 입력 해제 순서이며, 한 단계가 실패해도 나머지를 계속하고 실패를 모아 던진다. `Shutdown`은 Detach 후 경계의 콜백을 해제한다. `IStageInputReceiver` 콜백 10개는 한 입구에서 오류 경계로 감싼다. 정지·해제 뒤 입력은 무시하고, 취소류는 정리 자리만 둔다. 요청류는 후처리 중·종료면 무시한다(해석은 E) |
| `CombatResultPlayback.cs` | 연결 세대·재생 순번·통지·후처리 상태의 단일 소유자. `Attach`·`Begin`·`TryGetFailure`·`TryBeginFinalization`·`IsCurrent`·`CompleteFinalization`·`Abort`. 오래된 통지는 무시한다. 중복·잘못된 통지는 실패로 기록한다. Abort는 무효화를 먼저 하고, 재생 중일 때만 Cancel을 부른다 |
| `CombatPlaybackToken.cs`, `CombatPresentationOutcome.cs` | 세대+순번 토큰. 완료/실패 값(기본값은 완료가 아닌 잘못된 통지) |
| `ICombatPresentation.cs`, `ImmediateCombatPresentation.cs` | 연출 계약(Play·Cancel)과 1단계 즉시 완료 구현 |
| `CombatViewRefresh.cs`, `CombatRefreshSignal.cs` | 설계 §5.3 표: 설치·정리 신호, 미확인 결과 하나, 미리보기 응답 관측, 유실·순번 불일치·건너뜀·역행을 위반으로 돌려준다(위반이면 표시 이력 불변) |
| `CombatInputPolicy.cs`, `CombatInputFacts.cs` | 무상태 `IsTerminal`(Finished 또는 Completion)과 `GetDesiredLayer`(표시 전·전투 없음·종료면 없음, 아니면 Core StagePhase의 Layer). 명령별 허용은 E.1a |
| `CombatSceneRoot.cs` | Combat 씬 참조 묶음(`public sealed MonoBehaviour`, 동작 없음) |

**수정 파일**

| 파일 | 내용 |
|---|---|
| `Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | CombatViewSystem 생성(즉시 연출), `faults.ConnectStopInput(view.StopInput)`, 모듈 등록. 경계를 생략하면 기본 경계를 만든다 |
| `RuntimeSystem/Common/RuntimeFaultBoundary.cs` | `DisconnectStopInput(Action)` 추가. 연결된 콜백과 같을 때만 놓는다. View Shutdown이 정리 콜백 참조를 놓는 데 쓴다(설계 §10.1) |

**테스트** (`Assets/Tests/EditMode/Framework/`, `.meta` 포함)

| 파일 | 내용 |
|---|---|
| `CombatInputPolicyTests.cs` | 종료 판정(Finished·Completion만, HasBattle 무시), 단계별 Layer, Automatic에서 Combat 유지, 표시 전·종료 우선 |
| `CombatViewRefreshTests.cs` | 정상 3종(S==P 결과 없음·이미 확인한 결과, S==P+1), 위반 5종, 설치/정리 1회, Reset 뒤 다시 그림, 미리보기 관측 |
| `CombatResultPlaybackTests.cs`, `FakeCombatPresentation.cs` | C.5a: 즉시/지연 완료, 재생 중 Begin 거부, 미연결 Begin 거부, 실패 통지, 중복·기본값 통지, Abort 뒤 늦은 통지 무시, 반복 Abort, Play 중 Abort, 완료 뒤 Play 예외 전파, 새 Attach의 이전 세대 완료 무시, 오래된 토큰 거부 |
| `CombatViewSystemTests.cs` | C.2·C.4·C.4a·C.4b·C.5·C.6. 실제 CombatViewBuffer와 실제 Buffer에서 준비한 배치 단계 상태를 쓴다. 검증 항목: Node 우선순위, Exit Node로 Detach, 준비 전 입력 없음, 최초 표시 뒤 Bind·Layer 1회, 종료 전투는 Bind 안 함, 전투 정리 때 해제, 처리 번호까지만 삭제 → 확인, 거절·자동 진행 결과 확인, 지연 완료 전 미확인·중복 재생 없음, 연출 실패·게시 위반은 F1 1회·미확인, Play 안 오류/Detach 재진입 뒤 미확인, Play 예외 전파, 정상 종료는 입력만 해제(Cancel 0)하고 마지막 지연 결과를 1회 확인·재연결 없음, 새 결과 없는 종료는 P·명령 보존, Unbind의 SelectCanceled 무시, 입력 예외 1회 보고 뒤 입력·Tick 정지, 부분 정리 실패 계속·합쳐 보고, 오류 시 Abort·Cancel, Detach 멱등·재연결 뒤 이전 완료 무시, Shutdown 콜백 해제 |
| `RuntimeFaultBoundaryTests.cs` | `DisconnectStopInput` 1개 추가 |
| `FrameworkRuntimeCompositionTests.cs` | 1개 추가: 루트 조립이 View의 StopInput을 같은 경계에 연결하고, 전투 밖에서도 `PostBindService`가 성공 |

**계획과 다르게 한 것(사용자 확인 필요)**
1. **서비스 등록 없이 모듈로만 등록했다.** 계획 C.1과 설계 §13.3은 "서비스·모듈"이라고 적었지만, 지금 CombatViewSystem을 부르는 다른 시스템이 없다. 빈 서비스 인터페이스는 설계 §13.2(지침 §27, 쓰임이 하나뿐인 인터페이스 금지)와 맞지 않고, FW-001은 서비스 등록 형식만 정한다. F2 등에서 호출자가 생기면 그때 `ICombatViewXxx : IServiceModule`을 더한다.
2. **캐릭터·재화 읽기 싱크와 View DB 표 두 개(InitData)는 Phase D로 미뤘다.** C에는 그리기가 없어 보관만 하면 쓰이지 않는 필드(CS0414 경고)가 된다. D에서 Presenter와 함께 받는다.
3. **CombatSceneRoot에 참조 필드가 아직 없다.** 참조할 Presenter·패널이 D·E에서 생기므로 누락 검사(OnValidate·Attach)도 그때 함께 둔다. C에서는 씬에 정확히 하나 있는지만 연결 때 확인한다.
4. **오류 경계에 `DisconnectStopInput`을 더했다.** 설계 §10.1의 "View Shutdown이 정리 콜백 참조를 해제"를 구현하는 데 필요했다(B1에서 만든 우리 공통 파일).

**정적 확인**
- MSBuild(VS 2022), `ProjectL.Tests.Framework`와 참조 프로젝트: 오류 0, 경고는 기존 6건뿐(Systems 5, 테스트 CS0649 1). Unity csproj는 B1·B2 파일을 이미 포함하고 있어 새 파일만 임시 targets로 더했다. 새 타입·테스트가 산출 DLL에 들어 있다. Unity 컴파일·테스트 통과를 뜻하지는 않는다.
- SSOT: 결과 완료 기준은 Buffer의 P만 쓴다. 재생 상태는 Playback에만 있다. ViewSystem에 국면·종료 플래그·적용 Layer 사본이 없다(실제 Layer는 `CurrentLayer`, 표시 이력은 Refresh). 쓰는 Buffer는 CombatViewBuffer의 삭제·확인뿐이고 `Clear`는 부르지 않는다.
- 경계: Policy·Playback·Refresh는 Unity·Buffer를 참조하지 않는다. Core 서비스 호출 0. 씬 검색은 Enter Node 한 곳뿐이다.

**검증 요청(사용자)**
- EditMode: `CombatInputPolicyTests`, `CombatViewRefreshTests`, `CombatResultPlaybackTests`, `CombatViewSystemTests`, `RuntimeFaultBoundaryTests`, `FrameworkRuntimeCompositionTests`, `ChapterGraphPreparationTests`(Factory 호출 회귀). B2 묶음(`CombatFlowTests` 등)도 함께 실행한다.
- 씬 작업: `Combat.unity`에 빈 GameObject `CombatSceneRoot`를 만들고 같은 이름의 컴포넌트를 하나 붙인다. 없거나 둘 이상이면 Combat 진입 때 오류로 정지한다.
**1차 실행 뒤 정정(2026-10-02, 사용자 보고):**
- `ChapterGraphPreparationTests`의 `Runtime_` 두 테스트가 실패했다. Global 없이 InGame 그룹 전체를 조립하는데, 이제 그 그룹의 CombatViewSystem이 Global 서비스를 받다가 실패했기 때문이다. 사용자 결정으로 테스트 조립만 Core + 빈 View 그룹으로 바꾸고 [Core 전달 문서 §5.5](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 적었다.
- `RuntimeFaultBoundaryTests`는 테스트끼리 같은 `_logs` 목록을 공유한다(NUnit은 Fixture 인스턴스 하나를 공유한다). 그래서 새 테스트가 남긴 로그가 다른 테스트의 로그 개수를 어긋나게 했다. `[SetUp]`에서 로그를 비우도록 고쳤다.
- `CombatFlowIntegrationTests`의 실패가 기존 fixture 실패 15건인지는 사용자 메시지로 확인한다. 이 파일은 View 그룹을 조립하지 않는다.
- 다시 빌드한 결과 MSBuild 오류 0, 새 경고 0이다.

- C.7 재생(B2.5와 같은 설정): 오류 로그 없이 Combat에 들어가면 준비가 끝난 뒤 Deployment 입력이 연결된다(배치 단계 키가 반응해도 아직 아무것도 기록하지 않는 것이 정상). Live에서 명령 결과가 없으면 확인값도 그대로다. 재생을 멈출 때 오류가 없어야 한다.

### 7.12 Phase D 실행 기록 (2026-10-02, 코드 완료·검증 대기)

사용자가 B2·C 검증 완료(테스트·재생 통과)를 알리고 D 진행을 요청했다.

**이식 목록과 바꾼 의존** (원본 `origin/develop-4week` `Assets/Scripts/ProjectL/Unity/…`. 새 위치 `ViewSystem/Combat/Display/`, 네임스페이스 `…ViewSystem.Combat.Display`)

| 옛 파일 | 새 파일 | 바꾼 것 |
|---|---|---|
| `Adapter/Coordinate/FieldGridMapper` | `FieldGridMapper.cs` | 옛 `CellCoord`를 Core 계약의 `CellCoord`로 바꿨다. `RotationToWorld(Rotation4)`을 추가했다 |
| `View/Field/FieldCellFootprint`·`FieldCellSpacing`·`FieldCellSpacingPolicy` | `FieldCellSpacing.cs`(하나로 합침) | 계산은 같다(틈 1% + DB 간격) |
| `View/Field/FieldCellHandle` | `FieldCellHandle.cs` | 옛 `Framework.Systems…ColorSystem`을 새 Render `IColorSystemService`·`ColorScop`으로 바꿨다. 새 키에 BaseMap이 없어 텍스처 끄기는 빼고 MainColor만 쓴다. 같은 색은 다시 칠하지 않는다 |
| `View/Field/FieldCellPool` | `FieldCellPool.cs` | 옛 `ProjectL.BasicSystem.Pooling`을 Framework `CustomPool<T>`(FW-014)로 바꿨다 |
| `View/Field/FieldRoot`·`FieldRuntime` | `FieldView.cs`(MonoBehaviour) | 옛 `FieldDefinitionSO`·`StageSnapshot`·`FrameworkRuntime.Require()`를 DB `StageFieldViewData`·보드 크기 값·주입받은 색 서비스로 바꿨다. 레일·배치 구역 정적 강조를 추가했다(색은 씬 설정). 카메라용 `FieldFrame`은 FieldCamera 담당 영역이라 옮기지 않았다. 집기(`TryRayToCell`)는 E에서 붙인다 |
| `View/Field/FieldUnitPresenter`·`FieldUnitView` | `FieldUnitPresenter.cs`·`FieldUnitView.cs` | 입력을 `StageSnapshot`에서 `CharacterData`(필드·생존)·`StructureCombatFacts`로 바꾸고, 외형을 `UnitVisualCatalogSO`에서 DB `IUnitVisualTableQuery`로 바꿨다. 애니메이션·피격·사망 유지·HUD 오버레이는 뺐다(범위 밖). 체력은 숫자 하나(TextMeshPro 템플릿)다. 실제 표시값과 비교해 다를 때만 쓴다 |
| `View/HandQueue/HandPresenter`·`HandRoot`·`HandUnitView` | `HandPresenter.cs`(MonoBehaviour, HandRoot 배치 포함)·`HandUnitView.cs` | 입력을 플레이어·손패·생존 `CharacterData`로 바꿨다. 슬롯은 Core의 `ReserveSlotIndex`를 용량 기준으로 가운데 정렬한다. Drop 영역·집기는 E에서 붙인다 |
| `View/UI/StageDrawStatusUI` | `CombatHud.cs` | 재화(`CurrencyBuffer.Amount`)와 남은 드로우(`DrawOrder.Count − DrawCursor`)를 표시한다. 같은 값이면 텍스트를 다시 쓰지 않는다 |
| (새로 만듦) | `EntityViewSet.cs` | ID → View 재사용. 보인 대상은 그대로 두고 새 대상만 빌리고 사라진 대상만 돌려준다. 순수 C# |

**연결** (`ViewSystem/Combat/`)
- `CombatViewSystem`: `ISystemInitData<IHasViewQuery>`로 `StageFieldView` 설정과 `UnitVisual` 표를 받는다. `BindServices`에 캐릭터·재화 읽기와 ColorSystem을 더했다. 전투 설치 신호에서 `Install`, 결과 후처리에서 확인 전에 `Refresh`, 전투 정리에서 `Clear`, Detach에서 `Release`를 부른다. `Attach(root)`는 표시 구현을 만들어 `AttachDisplay`로 넘긴다.
- `ICombatBattleDisplay`·`CombatDisplayValues`·`CombatSceneDisplay`: ViewSystem이 값 묶음(상태·캐릭터·재화)을 넘기면 `CombatSceneDisplay`가 SceneRoot의 Presenter에 나눠 준다. 정적 필드는 설치 때 한 번, 동적 값은 결과마다 반영한다.
- `CombatSceneRoot`: `field`·`fieldUnits`·`hand`·`hud` 참조를 더했다. 각 Presenter의 하위 참조까지 OnValidate(경고)와 연결 시점(예외)에 검사한다.
- §7.11의 계획과 다르게 한 것 중 2번(캐릭터·재화·View DB)과 3번(SceneRoot 참조)은 여기서 해소했다.

**계획·설계와 다르게 한 것(사용자 확인 필요)**
1. **`ICombatBattleDisplay` + `CombatSceneDisplay`를 더했다.** 설계 UML은 ViewSystem이 Presenter를 직접 부르는 그림이다. 그렇게 하면 ViewSystem이 손패·필드를 고르는 그리기 지식을 갖게 되고(설계 §13.4 "ViewSystem에 그림을 추가하지 않음"과 충돌), C 테스트도 실제 씬·DB 없이는 돌릴 수 없다. 구현이 둘(씬 표시, 테스트 가짜)이라 §13.2의 인터페이스 기준을 만족한다.
2. **체력 표시는 씬의 TextMeshPro 3D 텍스트를 템플릿으로 복제한다.** 숫자 "현재/최대"이며 템플릿의 월드 방향을 유지한다. 옛 UnitHUD 오버레이는 옮기지 않았다.
3. **Core 방향은 R0 = 필드 +Z(Core +Y), 90°씩 시계 방향으로 가정했다.** Core에서 정의를 확인하지 못했으므로 재생에서 어긋나면 `FieldGridMapper.RotationToWorld` 한 곳만 고친다.
4. **셀의 ColorScop은 옛 코드와 같이 ColorSystem.Shutdown이 정리한다.** Combat 씬에 다시 들어가면 새 셀의 Scope가 쌓인다. 재진입은 F2 범위이므로 그때 반환 경로를 정한다(계획 §8 확장 조건).

**테스트**
- `CombatDisplayReuseTests.cs`(새 파일): `EntityViewSet` 재사용·추가/제거·종류 변경·반환 실패 뒤 계속·범위 밖 호출 거부. `FieldUnitView`·`HandUnitView`는 같은 값이면 쓰지 않고, 바깥에서 바뀐 값은 되돌린다. `FieldGridMapper` 왕복 변환, `FieldCellSpacing`.
- `CombatViewSystemTests.cs`: `AttachDisplay`(가짜 표시)로 바꾸고 6개를 추가했다. 설치는 1회이며 Buffer 값을 전달한 뒤에 Bind한다. 설치 실패면 Bind 0이다. 거절 결과도 그린 뒤 확인한다. 갱신 실패면 확인하지 않는다. 전투 정리 때 Clear, Detach 때 Release를 1회 부른다. View DB 없이 SceneRoot 연결은 거부한다.

**정적 확인**
- MSBuild(VS 2022), `ProjectL.Tests.Framework`와 참조 프로젝트: 오류 0, 경고는 기존 것뿐. Unity csproj에 없는 D 파일 15개는 임시 targets로 더했다. 새 타입·테스트가 산출 DLL에 들어 있다.
- 부분 조립 테스트 점검(§7.11 교훈): InitData를 부르는 다른 테스트는 Core만 조립하므로 View의 `IHasViewQuery` 요구와 겹치지 않는다.
- 종료 순서: FrameworkRuntime은 등록의 역순(LifeCycle → InGame(View) → Global(ColorSystem))으로 정리하므로, View가 셀을 돌려줄 때 색 Scope가 아직 살아 있다.

**D.6 씬 편집 절차(사용자)** — `Combat.unity`
1. `Field`(빈 오브젝트) + `FieldView`. 자식 `Cells`(위치·회전 0, 크기 1)를 Cells Root에 연결한다. 구역 색은 기본값을 써도 된다.
2. `FieldUnits` + `FieldUnitPresenter`. 자식 `Units`를 Units Root에 연결한다. `GameObject > 3D Object > Text - TextMeshPro`로 체력 템플릿을 만들고(글자 크기 3~5, 가운데 정렬, 카메라를 향하도록 회전) **비활성화**한 뒤 Health Label Template에 연결한다.
3. `Hand` + `HandPresenter`. 자식 `Content`를 Content Root에 연결한다. 위치는 필드 구성 뒤 자동 계산한다(§7.14). World Camera를 지정하거나 비워 두고 전투 카메라에 MainCamera 태그를 둔다. Slot Spacing은 2 정도이며, 필드와의 거리는 Field Edge Gap, 좌우·앞뒤 보정은 Field Plane Offset으로 조정한다. Content의 회전·크기는 저작한 값을 유지한다.
4. 기존 Canvas(없으면 `UI > Canvas`, EventSystem 포함)에 TMP 텍스트 두 개(재화, 남은 드로우)와 `CombatHud`를 두고 연결한다.
5. `CombatSceneRoot`의 Field·Field Units·Hand·Hud 칸에 위 컴포넌트를 연결한다. 비어 있으면 콘솔 경고가 나오고, 재생 때 Combat 진입에서 오류로 정지한다.
6. 카메라가 필드를 비스듬히 내려다보도록 둔다(FieldCamera는 다른 작업자 영역이라 수동 배치).

**검증 요청(사용자)**
- EditMode: `CombatDisplayReuseTests`, `CombatViewSystemTests`, `FrameworkRuntimeCompositionTests`, `ChapterGraphPreparationTests`, C·B2 묶음 회귀.
- D.6 재생(RecordInputOnly + 개발 설정): 8×7 셀(구역·레일 색), 필드 개체(메인 캐릭터 (3,0)·Demon (3,5)·적 본부 (4,6))의 위치·체력 숫자, Core가 손패에 둔 캐릭터, HUD(재화 0, 남은 드로우)가 보이고, 시간이 지나도 유지되는지 본다. Live도 같은 화면에서 오류 없이 유지되는지 본다.

### 7.13 전투 외형 리소스 복원 (2026-10-03)

사용자 요청으로 `ae07af37`의 유닛 외형·아이콘을 현재 구조로 복원했다. [리소스 인계](../handoffs/2026-10-03-combat-resource-restore.md), [원본/대상·GUID·해시 명세](2026-10-03-combat-resource-restore-manifest.json)를 참조한다. Phase D의 Unity 검증 완료를 의미하지 않는다.

- **파일 반영 완료:** 에셋 62개(이미지 45개 중 아이콘·카드·HUD 42개, FBX 5개, 프리팹 8개, 재질 2개, 충돌 Mesh 에셋 2개)와 `.meta`를 복원. 기존 모델·프리팹 11개는 같은 GUID로 재사용했다.
- **외형 DB 반영 완료:** 기존 6개 항목의 원본 외형/아이콘 연결을 복구하고 Ballista·CactusCannon·Gravekeeper 3개를 추가해 9개를 등록했다. MainCharacter는 원본 카탈로그의 TestUnit_Base를 유지한다. CactusCannon의 원본 Sniper 아이콘 연결은 전용 Cactus 이미지로 정정했다. Core·로드아웃·씬 구성 변경은 없다.
- **프리팹 정리:** 복원 프리팹의 옛 View/공격 연출 컴포넌트 14개와 기존 TestUnit_Base의 Missing Script 3개를 제거했다. 무기 충돌 에셋의 없는 도구 스크립트 컨테이너 2개를 제거하고 Mesh fileID를 보존했다. 현재 연출이나 입력 구현을 추가한 것은 아니다.
- **정적 검증 통과:** PNG·FBX 50개의 LFS 원본 크기/SHA-256, 변경 파일 158개의 기록 해시, 복원 GUID 유일성, Sprite 가져오기 설정, 프리팹 내부 fileID, 의존 에셋 83개·YAML 외부 fileID 참조 165건, 외형 9개 등록/키 중복 없음을 확인했다. URP 패키지 참조 3종도 설치된 메타데이터와 대조했다. 기존 Combat 씬·FMOD 캐시의 작업 내용은 해시로 보존을 확인했다.
- **Unity 검증 미실행:** 이 세션에서는 열려 있는 Unity에 테스트를 실행할 연결 도구가 없어 `DataBaseAssetTests`와 실제 임포트·재생 결과를 확인하지 못했다. Unity에서 Refresh 후 해당 EditMode 테스트, 새 외형 프리팹의 Missing Script/Missing Mesh/재질 누락, D.6의 메인·Demon·본부 표시를 확인해야 한다. 아이콘 리소스/DB 연결 완료와 손패 카드·타임라인 UI 구현 완료를 구분한다.

### 7.14 손패 Content Root 자동 배치 (2026-10-03)

사용자 요청으로 `ae07af37`의 `HandFieldAnchor` 동작을 현재 `HandPresenter`에 연결했다. 필드 네 모서리의 화면상 왼쪽 아래 경계에서 뷰포트 오프셋을 빼고 최소 여백을 적용한 뒤, 필드 평면으로 투영해 플레이어 쪽 변과의 간격을 맞춘다. 맵 크기에는 실제 셀 간격과 패딩이 포함된다.

- **책임:** `HandFieldLayout`은 받은 FieldGridMapper·Camera·설정으로 위치만 계산하는 내부 보조 타입이다. `HandPresenter`는 Content Root에 적용하고 손패 슬롯·View 수명을 유지한다. Buffer·Core·카메라 제어 코드에 의존하지 않는다. `FieldGridMapper.LocalToWorld`는 셀 중심 밖의 필드 외곽 좌표 변환을 제공한다.
- **연결:** `CombatSceneDisplay.Install`에서 필드 Build 뒤 `HandPresenter.SetField`를 호출한다. World Camera 설정이 비어 있으면 이때만 MainCamera를 찾는다. 현재 Combat 씬에 MainCamera 태그가 있어 씬 파일을 바꾸지 않았다. 카메라가 없으면 설치 경계 안에서 명시적 예외를 낸다.
- **수명·비용:** LateUpdate(실행 순서 100)에서 카메라 변화를 반영한다. 매 프레임 네 모서리 투영과 평면 교차만 계산하며 씬 검색·배열/컬렉션 생성·셀 순회는 없다. 같은 위치는 쓰지 않는다. Release 때 필드·카메라 참조를 해제해 후속 이동을 막는다. 필드가 카메라 뒤에 있거나 평면 투영이 불가능하면 기존 위치를 유지한다.
- **저작 설정:** `fieldViewportOffset=(0.14,0.14)`, `minimumViewportMargin=(0.08,0.08)`, `fieldEdgeGap=2.28`, `fieldPlaneOffset=(0,0)`은 원본 기본값이다. 실제 이동 대상은 Content Root이며 부모 Transform·Content의 회전/크기는 유지한다. 최소 뷰포트 여백은 원본과 같이 투영 기준점에 적용하며, 모든 손패 슬롯이 화면 안에 들어오도록 보장하는 기능은 아니다.
- **검증:** `CombatDisplayReuseTests`에 5개 사례를 추가했다(직교/원근 맵 크기·셀 간격, 이동/회전한 필드의 오프셋, Content만 이동·중복 쓰기 생략·줌/맵 교체·Release, 카메라 뒤 필드). MSBuild로 테스트 프로젝트와 참조 프로젝트를 컴파일해 오류 0·변경 파일 경고 0을 확인했다(기존 경고 6개). 새 파일만 임시 targets로 포함했고 Unity 생성 csproj는 수정하지 않았다. **Unity EditMode 및 재생은 미실행**이며 실제 화면에서 맵 크기 변경·카메라 줌·Inspector 간격 보정을 확인해야 한다.

**추가 — 3D 손패 크기 통일(2026-10-03, 사용자 요청):** 카드 손패로 전환하기 전까지 `HandPresenter.Configure`에도 `StageFieldViewData.FieldUnitScale`을 전달한다. 현재 값은 0.5이며, 필드 유닛과 같이 프리팹 생성 시 원래 localScale에 한 번 곱한다. 풀 재사용 때는 다시 곱하지 않고 Content Root·슬롯 간격은 유지한다. 별도 손패 크기 설정은 추가하지 않았다. MSBuild 테스트 프로젝트·참조 프로젝트 컴파일 오류 0·변경 파일 경고 0, Unity 재생은 미확인이다.

### 7.13 Phase E 실행 기록 (2026-10-03, 코드 완료·검증 대기)

사용자가 D 검증 완료를 알리고 E 진행을 요청했다. D 커밋(`710bf0b9`)과 사용자가 보강한 손패 위치 계산(`HandFieldLayout`, `FieldGridMapper.LocalToWorld`)은 그대로 두었다.

**새 파일** (`ViewSystem/Combat/`, `.meta` 포함)

| 파일 | 내용 |
|---|---|
| `CombatInteraction.cs` | 순수 C#. 외부 입력 10종을 의도로 해석한다. 배치 Drag는 손패·필드의 아군 유닛을 집어 칸에 놓거나 손패로 되돌린다. 제자리 클릭이면 집기만 하고, 다음 누름·뗌에서 놓는다(옛 동작). 메인 캐릭터는 PlaceMainCharacter로 기록하며 손패로 되돌리지 않는다. 이동/공격 조준 모드는 범위 미리보기를 요청한다. 호버한 칸이 바뀔 때만 이동·공격 미리보기를 요청한다. 뗀 칸에 Move·Attack을 기록하고, 공격은 칸에 개체가 있으면 개체 조준, 없으면 칸 조준이다. 이동 모드에서 행위자를 끌 수 있다. 단일 대기(`PendingInputCommand`)를 소유하며 Submit 성공 뒤에만 기록하고, 일치하는 Command 결과에서만 해제한다. 결과 뒤 처리(`AfterResult`): 미리보기 무효화 → 무효해진 Drag·모드 정리(행위자·영역·Layer) → 유효하게 남은 것만 재요청. `CancelPresentation`(대기 보존)과 `Stop`(로컬 대기 종료)을 구분한다. 기록 직전마다 Policy로 확인한다  **정정(E2):** 개체 조준은 새 Core가 받지 않아 `InvalidAttackAim`이 났다. E2에서 범위 응답의 조준 목록으로 바꿨다(§7.14). |
| `PendingInputCommand.cs` | 대기 하나(종류 EnterCombat/EndUnitTurn + 번호) |
| `ICombatPointerPicker.cs` | 칸·손패 유닛·손패 Drop 영역 집기(좁은 계약) |
| `ICombatInteractionFeedback.cs` | 고스트·원본 숨김·호버 칸 표시 의도 |
| `Display/PlacementGhost.cs` | 옛 `PlacementGhost*` 이식. 종류별 풀(`CustomPool`), DB의 고스트 재질·크기·높이를 쓴다. 색은 ColorScop의 유효 색이다. 충돌·그림자를 끈다 |

**수정 파일**

| 파일 | 내용 |
|---|---|
| `CombatInputFacts.cs` | 실행 모드·실제 Layer·S/P·후처리 여부·활성 행위자·선행 미처리 명령을 더했다. 기존 5인자 호출은 그대로 쓸 수 있다 |
| `CombatInputPolicy.cs` | `CanSubmit`·`CanPreview`·`CanChangeDevLayer`·`StageOf`·`LayerOf`를 더했다. 공통(표시 뒤·전투·종료 아님·대기 없음·후처리 아님)에 Live 조건(PlayerInput, 단계·Layer 일치, Submit은 S == P, EndTurn은 행위자 있음·선행 명령 없음)을 더한다. RecordInputOnly는 Live 조건 대신 실제 Layer만 본다 |
| `ICombatBattleDisplay.cs` | 집기·표시 의도 계약을 상속하고 개발 Layer 패널 켜기/끄기를 더했다 |
| `CombatSceneDisplay.cs` | 카메라 Ray로 칸·손패를 집는다. 고스트·원본 숨김·호버를 Presenter에 전달한다. 패널은 버튼 연결·해제와 켜기·끄기를 맡고 Release에서도 끊는다 |
| `CombatSceneRoot.cs` | `ghost`·`devLayerPanel`·`devDeploymentButton`·`devCombatButton`(필수), `worldCamera`(선택, 비면 MainCamera) |
| `CombatViewSystem.cs` | Attach마다 Interaction을 만든다. 모드가 RecordInputOnly면 패널을 켜고, 아니면 끈다. 입력 콜백은 좌표와 함께 현재 사실을 읽어 Interaction에 넘긴다(EndTurn일 때만 선행 명령 조회). C의 후처리·종료 조기 반환은 Policy와 겹쳐 지웠다. 결과 후처리: 그리기 → `ResolvePending` → Layer 동기화(바뀌면 표현 정리) → `AfterResult` → 삭제 → 확인. 종료·전투 정리·Layer 변경에서 `CancelPresentation`, StopInput·Detach에서 `Stop` + 패널 끄기. 개발 패널 입구는 오류 경계로 감싸고 `CanChangeDevLayer`가 허용할 때만 표현 정리 뒤 `ChangeLayer` |
| `Display/FieldView.cs` | 호버 칸 강조(이전 칸은 정적 색으로 복귀), `TryRayToCell`·`TryRayToFieldPlane` |
| `Display/HandPresenter.cs` | 슬롯 → 개체 연결, `TryPickUnit`(Collider 없이 손패 평면과 슬롯 위치 비교, 숨긴 유닛 제외), `ContainsDropPoint`(Drop 영역, Gizmo 표시). 사용자가 넣은 위치 계산은 그대로 두었다 |

**테스트**
- `CombatInteractionTests.cs`(새 파일). 실제 CombatViewBuffer와 가짜 집기·표시를 쓴다. 검증 항목: 시작 2회 → 1개·대기, 대기 중 Draw·Drag·취소, 턴 종료 2회 → 1개, 선행 명령·행위자 없음·Automatic·S>P·Layer/단계 불일치 거부, 연출 중 미리보기 허용·제출 거부, 후처리 중 차단, 종료 차단, 일치 결과만 대기 해제(수락·거절), CancelPresentation/Stop 구분, 손패 Drag → PlaceUnit과 표시 복구, 칸이 바뀔 때만 미리보기, SelectCanceled 기록 0, 필드 → 손패 ReturnUnit(메인 불가), 메인 PlaceMainCharacter, 클릭 집기, 이동 모드 범위 → 호버 → Move, 공격 조준, 결과 뒤 행위자 변경/동일/배치 복귀, 기록 모드 EndTurn·행위자 선택.
- `CombatInputPolicyTests.cs`: Live 제출 조건 표, EndTurn 조건, 미리보기 조건, 기록 모드 예외, 개발 Layer 허용, 명령 → 단계 대응.
- `CombatViewSystemTests.cs`: 가짜 표시가 새 계약을 구현한다. 추가: View 경유 시작 1회와 일치 결과 뒤 재허용, Tick 전 종료 입력 0, Live 패널 미표시, 기록 모드 패널 버튼 → 실제 Layer만 변경, 대기·종료 중 버튼 무시, StopInput·Detach에서 패널 해제.

**계획·설계와 다르게 한 것(사용자 확인 필요)**
1. **`ICombatInteractionFeedback`을 더했다.** 설계는 집기만 좁은 인터페이스로 받는다고 적었다(§7). 고스트·숨김·호버까지 Interaction이 Presenter를 직접 부르면 순수 C#·테스트 가능 조건이 깨진다. 구현은 씬 표시와 테스트 가짜 둘이다.
2. **Interaction은 씬 연결(Attach)마다 새로 만든다.** 소유자는 ViewSystem 그대로다. Detach에서 로컬 대기를 끝내는 설계(§5.6 표)와 같은 수명이다.
3. **손패 집기는 Physics Raycast가 아니라 손패 평면·슬롯 위치 비교다.** 프리팹에 Collider가 없어도 동작한다. 모형 윗부분을 누르면 평면 기준으로는 조금 뒤로 잡힐 수 있으므로 `Pick Half Depth`로 조정한다.
4. **배치 방향은 회전 입력이 없어 현재 방향(손패는 R0)을 쓴다.** 회전 입력을 추가할 때는 stage-input-guide §6 절차를 따른다.
5. **기록 모드 행위자:** Core가 배치 단계라 행위자가 없으므로, Combat Layer에서 누른 아군 유닛을 기록용 행위자로 쓴다(설계 §8.1의 "실제 Layer로 기록 허용"을 Move·Attack에 적용).
6. **고스트 색은 항상 유효 색이다.** 배치 가능 여부는 미리보기 응답 표시가 후속 범위(설계 §1)라 바꾸지 않는다.
7. **ViewSystem 입력 입구의 후처리·종료 조기 반환을 지웠다.** 같은 조건이 Policy에 있어 중복이다(설계 §13.4 "ViewSystem에 Policy 조건을 중복 구현하지 않음"). 동작은 같고, 취소류는 후처리·종료 중에도 표현 정리를 한다.

**정적 확인**
- MSBuild(VS 2022), `ProjectL.Tests.Framework`와 참조 프로젝트: 오류 0, 경고는 기존 것뿐. Unity csproj에 없는 새 파일 6개는 임시 targets로 더했다.
- Interaction·Policy·Pending·두 계약은 UnityEngine을 참조하지 않는다. Core 서비스 호출 0. Buffer 쓰기는 Submit·SetPreview·ClearPreview(Interaction)와 삭제·확인(ViewSystem)뿐이고 Clear는 없다.

**E 씬 편집 절차(사용자)** — `Combat.unity`. 아래가 없으면 CombatSceneRoot 연결 단계에서 오류로 정지한다.

현재 씬의 Ghost·개발 패널·버튼 참조는 연결돼 있다(§7.18). 아래는 구성 절차 기록이며 미완료 작업 목록이 아니다.

1. 빈 오브젝트 `Ghost` + `PlacementGhost`를 만들고 Ghost Root에 자신(또는 자식)을 연결한 뒤, CombatSceneRoot의 Ghost에 연결한다.
2. Canvas 아래 `DevLayerPanel`(Panel)을 만든다. 버튼 두 개(배치, 전투)와 TMP 텍스트 "입력 기록 모드"를 두고, **패널을 비활성화**한 뒤 CombatSceneRoot의 Dev Layer Panel·Dev Deployment Button·Dev Combat Button에 연결한다.
3. (선택) CombatSceneRoot의 World Camera에 전투 카메라를 연결한다. 비우면 MainCamera 태그 카메라를 쓴다.
4. `Hand`를 선택하면 Drop 영역이 하늘색 Gizmo로 보인다. 손패 앞쪽을 덮도록 Drop Zone Size·Pick Half Depth를 맞춘다.
5. (선택) `FieldView`의 Hover Color.

**검증 요청(사용자)**
- EditMode: `CombatInteractionTests`, `CombatInputPolicyTests`, `CombatViewSystemTests`, `CombatDisplayReuseTests`, `StageInputTests`, `UnityStageInputTests`, `FrameworkRuntimeCompositionTests`, 그리고 C·B2 묶음 회귀.
- E.7(RecordInputOnly): 패널이 보인다. 손패 Drag·필드 이동·Drop이 고스트와 함께 동작하고 Buffer 명령이 쌓인다(결과·확인 번호는 그대로). 전투 버튼 → Combat Layer에서 유닛을 누르고 이동/공격 모드로 기록한다. 시작 또는 턴 종료를 한 번 기록하면 대기 때문에 버튼·입력이 더 기록되지 않는다(다음 검증은 재생 재시작).
- E.8(오류): 지금은 재생 중 예외를 주입할 수단이 없다. EditMode `InputException_IsReportedOnce_ThenInputAndTickStop`·`StopInput_*`로 대신할지, 개발 전용 예외 주입 키를 둘지 사용자 결정이 필요하다.
- E.9(개발 Live): 배치 → 전투 시작 → 이동·공격·턴 종료 결과가 그려지고 다음 입력이 열리는지 본다. 현재 개발 fixture(밤 500, 유닛 5종)로 첫 밤 종료·배치 복귀·승패까지 갈 수 있는지는 재생에서 확인하며, 부족하면 fixture 조정이 필요하다.

**추가(2026-10-03, 사용자 요청): Play From Boot 설정 창.** Boot 씬을 열지 않고 GameBootstrap 값을 바꾸려는 요청이었고, 사용자 결정으로 "이 PC 전용 덮어쓰기"를 택했다. 메뉴 `ProjectL > Play From Boot 설정...`(`Tool/Editor/PlayFromBootSettingsWindow.cs`)에서 Play From Boot 토글과 덮어쓰기를 함께 다룬다. 덮어쓰는 값은 시작 구간·실행 모드·Use Dev Battle·로드아웃·챕터·노드다. 값은 EditorPrefs에 저장된다(`ViewSystem/Boot/GameBootstrapEditorOverrides.cs`, 파일 전체 `UNITY_EDITOR`). GameBootstrap은 에디터에서 덮어쓰기가 켜져 있을 때만 Start 처음에 이번 재생의 인스턴스 값을 바꾸고 콘솔에 한 번 알린다. Boot 씬 값과 빌드는 바뀌지 않는다. `PlayFromBootToggle`의 상태·적용 메서드는 창에서 쓰도록 internal로 바꿨다. MSBuild(`Assembly-CSharp-Editor`와 참조 프로젝트) 오류 0·새 경고 0.

### 7.14 Phase E2 실행 기록 (2026-10-03, 코드 완료·검증 대기)

E의 Live 재생에서 공격이 `InvalidAttackAim`으로 거절됐다(사용자 보고). 원인은 View가 개체 조준을 보낸 것이며, 새 Core는 개체 조준을 받지 않는다(설계 §21). 사용자 결정으로 설계 9차(§21)와 이 Phase·A2를 더하고 진행했다.

**새 파일:** `Combat/CombatHoverState.cs`(호버 상태 3종), `Combat/Display/FieldHighlightLayers.cs`(층 비트와 순수 우선순위 계산).

**수정 파일**

| 파일 | 내용 |
|---|---|
| `CombatInputFacts.cs` | `PreviewAnswer`: 현재 요청 번호와 같은 Core 응답 |
| `CombatViewSystem.cs` | `ReadCurrentPreviewAnswer`(입력 `Preview.Number` == Core `RequestNumber`). Tick의 응답 신호에서 `Interaction.PreviewAnswered`(후처리 판단 제외 사실) |
| `CombatInteraction.cs` | 범위 요청 번호를 기억하고 응답을 모드 동안 보관한다(공격 조준 목록, 이동 앵커). 응답 전에는 호버 요청·기록 0. 공격은 조준 종류별(칸: 목록 칸만, 방향: 행위자 → 칸 각도에 가장 가까운 목록 방향, 전체: 하나)로 호버·기록하고, 이동은 앵커만 기록한다. 응답이 오면 올려 둔 칸을 바로 요청한다. 호버 응답으로 가능·불가 색과 공격 영향 칸, 배치 응답으로 고스트 색을 정한다. 결과 후처리에서 보관값을 버리고 범위를 다시 요청하며, 모드 종료·Stop에서 표시를 비운다. **RecordInputOnly는 Core가 답하지 않으므로 응답 없이 칸 기준으로 기록한다** |
| `ICombatInteractionFeedback.cs` | `SetHoverCell(칸, 상태)`, `SetGhostPlaceable`, 범위·영향 칸 표시/지우기 |
| `CombatSceneDisplay.cs` | 새 표시 의도 전달, 결과마다 현재 행위자 칸 층 갱신 |
| `Display/FieldView.cs` | 층 비트 배열과 `SetLayerCells`·`ClearLayer`. 호버 상태 3색, 영향·범위·행위자 색(씬 설정). 바뀐 칸만 다시 칠한다 |
| `Display/PlacementGhost.cs` | Scope를 보관하고 `SetPlaceable`(DB 유효·무효 색, 바뀔 때만) |
| `Dev/DevBattleInitializationSource.cs` | ShotGun 지정 방식 = Fan/Direction(설계 §21.2), 나머지 RadialSquare/Single |

**테스트:** `CombatInteractionTests`의 바뀐 기대와 새 테스트. 범위 응답 전 차단, 응답 뒤 자동 요청과 범위 표시, 앵커 외 이동 0, Single 칸 조준(적이 있는 칸도 칸 조준), Direction 최근접 방향, All, 거절·미응답 범위에서 기록 0, 이전 번호 응답 무시, 배치 응답의 불가 색·고스트 색, 공격 호버 응답의 불가·영향 칸 비움. `CombatDisplayReuseTests`에 층 우선순위. `CombatViewSystemTests` 가짜 표시를 새 계약에 맞췄다.

**정적 확인:** MSBuild 오류 0, 새 경고 0. View의 InGame DB 읽기 0(지정 방식은 응답의 조준 목록으로만 판단).

**검증 요청(사용자)**
- EditMode: `CombatInteractionTests`, `CombatDisplayReuseTests`, `CombatViewSystemTests`, `CombatInputPolicyTests`와 기존 묶음.
- E2.7(Live, 개발 설정):
  - Q(이동): 범위 칸이 하늘색으로 표시되고, 범위 칸 클릭만 수락된다.
  - E(공격): 범위가 표시되고, 메인·Akimbo·Sniper로 적 칸을 클릭하면 수락된다(`InvalidAttackAim` 없음).
  - ShotGun: 적이 있는 쪽을 가리키면 그 방향으로 수락된다.
  - 호버 색(가능 초록·불가 빨강), 영향 칸(주황), 현재 행위자 칸(노랑), 배치 Drag 고스트의 불가 색.
  - 색은 `FieldView` 인스펙터에서 바꿀 수 있다.

### 7.15 Core `81c8c38` 병합 반영 (2026-10-03)

사용자 요청으로 `638cb6e9` 위에 `81c8c38ca91d51e432fb37a218d6dbdc1d49e835`를 `--no-commit --no-ff`로 병합했다. 미반영 선행 커밋 `3a6b4cd3`(아티팩트 이전), `acc85190`(Core 통합 검증), `a9db1e5a`(전투 서비스 정리)도 포함한다. 커밋·push는 사용자가 수행한다.

이후 사용자 병합 커밋 `491cae4d`를 확인했다. 아래는 병합 당시 작업·검증 기록이며 진입 이전 구현 완료를 뜻하지 않는다.

- **충돌 해결:** `CombatPlanTests.cs`의 기존 `CombatProbe` 호출 횟수 검증을 유지하고 새 `EnterBattle` 멤버를 추가했다.
- **컴파일 호환:** `DevBattleInitializationSource`와 `CombatFlowIntegrationTests.DefinitionSource`를 `Read(database, chapterKey, stageKey)` 서명으로 맞췄다. 개발 fixture의 고정 정의 동작은 유지한다.
- **검증:** 현재 소스 목록을 반영한 임시 MSBuild targets로 `ProjectL.Tests.Framework`, `ProjectL.Tests.Core`, `ProjectL.Tests.CombatViewBoundary`와 의존 코드를 빌드했다. 세 빌드 모두 종료 코드 0, 컴파일 오류 0. Unity EditMode 테스트·Play 검증은 실행하지 않았다. 빌드 로그는 로컬 `Temp/merge-81c8c38-check/`에 있으며 Git 관리 대상이 아니다.
- **진입 이전 미완료:** `Register → Plan → Job` 대상 전달, 진입 결과 전달, `EnterBattle` 수락 후 한 번만 활성화하는 LifeCycle 연결은 아직 구현하지 않았다. 기존 Bootstrap의 Combat 선택 확정과 기존 `PrepareBattle()` 호출을 함께 이전해야 한다. 상세 계약과 V1~V5는 [Core 진입 인계 문서](../handoffs/2026-10-03-stage-entry-battle-preparation-handoff.md)를 따른다.
- **기록 모드 호환 문제:** 현재 `RecordInputOnly` Bootstrap은 로스터만 준비하지만 병합된 호환용 `PrepareBattle()`은 확정된 맵·선택을 요구한다. 따라서 이 모드의 기존 진입 경로는 준비 단계에서 실패한다. 진입 이전 시 기록 모드도 함께 수정·검증해야 하며, 이번 병합을 기록 모드 재생 검증 완료로 보지 않는다.
- **남은 조건:** 실제 DB 조립(T1/G7), A2 연결, F1/F2 및 운영 씬 수락은 별도 미완료다. Core 브랜치의 기존 테스트 수치를 현재 통합 브랜치의 테스트 통과 기록으로 사용하지 않는다.

### 7.16 골격 우선 작업과 View B3 계획 반영 (2026-10-03)

사용자가 골격 구현을 다른 세션에서 이어가도록 기존 문서를 갱신했다. 골격 6A는 대상/결과 전달·Job·활성화 수명, View B3는 Bootstrap·두 모드·입력 활성화를 맡는다. B3는 미착수이며 E/E2 검증 대기를 해소하지 않았다. §7.15의 기록 모드 준비 실패는 B3 대상이다.

다음은 골격 6A.0의 계약 확정이며 View 단독 새 진입 구현을 시작하지 않는다. 이후 E/E2와 F1 검증, A2/Core 조립 및 F2, 실제 DB·씬 인수 순서다. 이번 수정은 문서만이며 코드·씬·에셋과 Unity 테스트 결과를 변경하지 않았다. Core 인계 원본과 과거 코드 UML은 시점 기록으로 보존한다.

### 7.17 Phase B3 실행 기록 (2026-10-04, 코드 완료·검증 대기)

골격 6A와 같은 작업에서 B3.1~3을 연결했다. 골격의 대상/결과/활성화 규칙은 [골격 설계 §6.3](../specs/2026-09-30-combat-flow-skeleton-design.md#63-명시적-전투-진입-계약-k20)이 원본이며 실제 등록 경로 검사는 [골격 계획 §6.10](2026-09-30-combat-flow-skeleton-plan.md#610-phase-6a-실행-기록-2026-10-04-코드-완료unity-검증-대기)을 따른다.

| 파일 | 반영 |
|---|---|
| GameBootstrap.cs | 두 모드 모두 InitializeRoster → EnterChapter → `LifeCycle.CombatEntry.RequestEntry(chapterKey, nodeId)`. 사전 Combat ConfirmStageEntry·무조건 Change 제거. Runtime Tick 유지, 정상 진입 거절은 이유 한 번 기록 |
| CombatViewSystem.cs | 좁은 ICombatEntryRead를 받아 씬 Enter·Tick·입력·늦은 연출 콜백을 활성 요청에 한정. 기존 최초 그리기 뒤 Bind 유지 |
| PlayFromBootSettingsWindow.cs | 두 모드 공통 개발 대상 설정 안내. EditorPrefs 키/기존 저장값 그대로 |
| CombatViewSystemTests.cs | 준비 Buffer가 있어도 활성화 전 입력 Bind·제출 0 회귀 추가. 기존 가짜에는 활성 읽기 주입 |

**개발 재생 조건:** Start=Combat, Use Dev Battle=true, 유효한 로드아웃·챕터·노드가 필요하다(기본 1/1/2). 두 모드에 동일 적용한다. Use Dev Battle=false의 Combat 직접 시작은 대상이 없으므로 요청 부족 오류로 정지하며 임의 노드/Current 선택으로 보정하지 않는다. Stage 시작은 기존 경로다. B1의 “source 없음 → G7” 재생은 과거 기록이고 새 운영 검증은 명시적 진입 요청을 갖춘 호출자로 해야 한다. 개발 source의 고정 정의는 유지했으며 운영 DB 조립 성공을 뜻하지 않는다.

- 정적 검사: Framework·CombatViewBoundary 테스트 및 에디터 코드 MSBuild 오류 0·새 경고 0. Systems의 비개발 조건부 컴파일도 오류 0(플레이어 빌드는 아님). Core·Buffer 직접 쓰기 추가 없음.
- 관리 코드 검사: 골격 계획 §6.10의 두 모드 진입/등록 경로를 포함한 53건 통과. Unity 씬 표시·입력 대역을 사용했으므로 실제 재생과 구분한다.
- 사용자 확인 대기: Unity 컴파일·EditMode 전체(기존 보류 15건 별도), **Live와 RecordInputOnly 각각 새 런타임으로 Boot → 최초 표시 → 입력 기록**. Live만 실제 명령 처리, 기록 모드는 처리 번호/결과 증가 0. 존재하지만 연결되지 않은 노드로 정상 거절 시 Combat 로드/입력 0, 잘못된 데이터 예외 시 F1 보고 1회·재Tick 없음.
- B3.4~5는 위 검증 뒤 완료 처리한다. 이후 E.7~9/E2.7·골격 6B F1 인수로 진행한다. F2·A2는 구현하지 않았다. 커밋·push·외부 전달 없음.

**View 후속 작업(기준 `360e6253`, 2026-10-04):** B3.1~3의 런타임 연결이 골격 커밋에 포함된 것을 확인하고 중복 구현하지 않았다. `CombatViewSystemTests`의 B3 전용 검증을 기존 1개에서 6개 사례로 보강했다.

| 회귀 | 확인 조건 |
|---|---|
| 활성화 전, Live/RecordInputOnly 각각 | 준비 Buffer가 있어도 최초 표시·Bind·Layer·명령·미리보기·결과 확인 0. 활성화 후 최초 표시·Bind 1회, 이후 입력 기록 가능 |
| 최초 표시 호출 중, 두 모드 각각 | Install 안에서 입력·개발 버튼이 재진입해도 기록·Layer 변경 0. Install 반환 뒤 입력 기록 가능 |
| 최초 표시 중 진입 종료 | 그리기가 반환해도 입력을 뒤늦게 연결하지 않음 |
| 지연 연출 중 진입 종료 | 늦은 완료·입력·Tick이 결과 갱신·확인·명령 삭제·추가 기록을 수행하지 않음. F2 이탈 정리 자체의 검증과 구분 |

- `GameBootstrap`·개발 source 주석과 설계 §4·§10.0에 남은 Live 전용 준비/직접 Change 설명을 실제 두 모드 진입 경계로 정정했다. source의 고정 정의와 ShotGun 조준 설명도 현재 코드에 맞췄다. 게임 동작·Core·골격 소유 코드는 변경하지 않았다.
- 후속 검증: Framework·CombatViewBoundary·에디터 MSBuild 오류 0, 변경 파일의 새 경고 0. 현재 산출물로 관리 코드 53/53 재확인. 이는 **Unity EditMode 실행 결과가 아니다**. 로그·로컬 실행 보조 코드는 `Temp/CodexB3/`에 보관한다.
- Unity 에디터의 소스 Refresh가 아직 반영되지 않아 B3.4의 Unity 실행과 B3.5 재생은 대기한다. 체크박스는 실행 결과를 받은 뒤 닫는다.

**남은 재생 절차:** 매 항목은 재생을 종료한 뒤 새 런타임으로 시작한다. `ProjectL > Play From Boot 설정`에서 덮어쓰기를 켜고 개발 설정을 사용한다. 검증 후 원래 설정으로 복구한다.

| 시작 설정 | 완료 증거 |
|---|---|
| Combat / Live / 로드아웃 1·챕터 1·노드 2 | Boot → 수락 → Combat 필드·손패·HUD 최초 표시 → 입력. 드로우 명령을 넣으면 Core 처리 번호·결과 증가, View 결과 확인과 처리 명령 제거 |
| Combat / RecordInputOnly / 같은 대상 | 같은 최초 표시와 개발 패널. 입력은 Buffer에 남고 후속 Tick에서도 처리 번호·결과 증가 0 |
| Combat / 유효 챕터의 진입 불가 노드 | 정상 거절 이유 1회, Combat 로드·표시·입력 0, 오류 정지 없음. 유효하지 않은 노드 번호로 데이터 예외를 만드는 검사와 구분 |
| Combat / 존재하지 않는 개발 로드아웃 | 시작 오류 F1 보고 1회, 전투 표시·입력·재Tick 없음 |
| Stage 시작 | 기존 비전투 시작 경로 보존, Combat 활성화·입력 0 |

### 7.18 E/E2 사용자 기능 확인 (2026-10-04)

기준 커밋은 `5d9496e1`이다. E/E2 구현과 씬 연결을 확인한 뒤 사용자에게 실행 설정·키 바인딩·조작별 기대값을 전달했고, 사용자가 **"문제 없는거 확인"**이라고 회신했다. 이를 사용자 기능 재생 확인으로 기록한다.

- 전달한 기능 확인 범위: 두 모드의 Boot → Combat 최초 표시, 손패·필드 드래그와 취소·고스트 복구, 기록 모드의 상태 유지·개발 Layer·시작/턴 종료 대기, Live 배치·반환·드로우·전투 시작·이동·공격·턴 종료, 밤 종료 후 배치 복귀와 다음 밤, 종료 후 입력 차단, E2 Single/Direction 조준과 범위·영향·호버·행위자·고스트 색상.
- E2.7을 사용자 확인으로 완료 표시했다. E.7·E.9의 기능 재생도 확인됐지만, 해당 Task가 요구하는 내부 번호·Job 등록 횟수·지연 완료까지 측정한 실행 기록은 별도다. 일반 기능 확인을 모든 정밀 회귀 테스트의 통과 수로 바꾸지 않는다.
- `Combat.unity`의 Ghost·DevLayerPanel·버튼 2개 참조는 `638cb6e9`에 포함돼 있으며 현재 씬에도 연결돼 있다. 기존 상태표의 "씬 편집 필요"를 정정했다.
- 사용자 회신에는 EditMode 실행 묶음·개수·로그가 첨부되지 않았다. 해당 결과는 "미실행"으로 단정하지 않고 **실행 결과 미수신**으로 기록한다. 이번 응답에서 에이전트가 Unity 테스트를 실행한 것은 아니다.
- 이전 안내에서 별도라고 명시한 E.8 실제 Drag 중 예외 주입, 지연 연출·늦은 콜백 회귀는 완료로 올리지 않는다. B3의 정상 거절·잘못된 개발 데이터·Stage 시작 검사와 운영 DB(T1/G7)·F2 이탈/재진입도 이번 기능 확인에 포함하지 않는다.
- 다음 View 신규 구현 후보는 A2(공격 지정 방식 DB·UnitDataSO 연결)이며, 골격 6B F1 인수 때 E.8과 관련 회귀 증거를 연결한다. 이 기록으로 A2나 F2를 착수한 것은 아니다. 코드·씬·에셋 변경과 커밋·push 없음.

### 7.19 Phase A2 실행 기록 (2026-10-04, 코드 완료·Unity 검증 대기)

기준 HEAD는 `5d9496e1`이며 사용자의 **"A2 바로 진행해줘"** 요청으로 구현했다. A2.1~A2.5의 코드·에셋·테스트·인계 문서 작업을 마쳤다. A2.6의 Unity 실행 검증은 남아 있다.

**구현:**

- `Data/InGame/Table/Targeting/`에 순수 `TargetingMethodData`, 입력 SO, TableSO 추가. enum·선택 방식/추가 범위 조합은 기존 `TargetingMethodSpec`으로 검증하고, 키·비율 저작 제약은 데이터에서 검사한다. 사거리는 유닛이 소유한다.
- `TargetingMethodTable` 및 서비스 키·`IInGameQuery`·Factory 연결. 조회는 사전 기반이며, 유닛 참조 검사는 DB 생성 시 한 번 순회한다. View 입력·프레임 경로에 DB 조회나 새 상태를 추가하지 않았다.
- `UnitDataSO.targetingMethod`를 순수 키로 바꾼다. 공격력이 양수면 키 필수, 공격력 0이면 키 0 허용. 지정된 양수 키가 테이블에 없으면 DB 생성 시 유닛 이름과 키를 포함한 오류를 낸다. 생성자의 마지막 필수 인자 추가에 맞춰 기존 테스트 호출 두 곳을 갱신했다.
- `DataBaseSO/InGameGroup/Targeting/`의 방식 1·2와 테이블 에셋을 등록하고 유닛 5개를 연결했다. 키 표·새 API·테스트 대역 변경·Core 후속 요청은 [DB 경계 인계 §5.6](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달)을 원본으로 한다.
- `TargetingMethodDataTests` 추가, `InGameDataTableTests`에 조회·중복·잘못된 항목·참조 누락·값 복사 검증, `DataBaseAssetTests`에 실제 유닛/지정 방식 연결 검증 추가. `ChapterGraphPreparationTests`의 수동 그룹과 `IInGameQuery` 대역도 새 계약에 맞췄다.

**실행한 검증:**

- 현재 소스 전체를 포함한 MSBuild: `ProjectL.Tests.Framework`, `ProjectL.Tests.CombatViewBoundary`, `Assembly-CSharp-Editor` 모두 오류 0, 변경 파일 경고 0. 기존 Audio using·Core nullable·미할당 필드·System.Net.Http 참조 충돌 경고는 별도다. 로그는 로컬 `Temp/CodexA2/*.log`.
- Unity 밖 관리 코드 실행: `TargetingMethodDataTests` **26건**과 기존 `CombatEntryIntegrationTests`·`CombatPlanTests`·`JobSystemTests` **53건**, 합계 **79건 통과**. 로그는 `Temp/CodexA2/smoke.log`. Unity Test Runner 결과는 아니다.
- 에셋 정적 검사: 지정 방식 2개의 값, SO 스크립트/테이블/InGameGroup GUID 연결, 유닛 5개 참조, 새 파일의 meta 및 GUID 중복 없음 확인. 유닛의 새 참조를 제외한 기존 값은 HEAD와 동일하다.
- Core 생산 코드와 View 런타임 코드는 변경하지 않았다. 기존 FMOD 캐시 변경은 보존했다. 커밋·push 및 외부 전달 없음.

**A2.6 사용자 확인:**

1. Unity Refresh 후 컴파일 오류가 없는지 확인한다.
2. EditMode에서 `TargetingMethodDataTests`, `InGameDataTableTests`, `DataBaseAssetTests`, `ChapterGraphPreparationTests`와 Phase A 묶음(`ArtifactDataTableTests`, `ViewDataTableTests`, `CharacterBattlePreparationTests`)을 실행한다. 기대값: 새 테이블 누락·유닛 지정 방식 참조 오류 없이 통과한다.
3. 기존 개발 설정(`useDevBattle=true`)으로 Boot부터 재생하여 DB 생성과 Combat 최초 표시가 유지되는지 확인한다. 손패·배치·공격의 동작은 기존 E/E2와 같다. 개발 fixture는 여전히 별도 source다.
4. `useDevBattle=false`의 운영 전투 성공은 A2 완료 기준으로 주장하지 않는다. Core의 기본 source 조립과 T1/G7 해소 뒤 실제 DB 기반 진입·공격·미리보기를 다시 검증한다.

다음 연결은 Core의 지정 방식 키 해석·기본 source 완성과 골격 6B/F1 인수·F2 수명 작업이다. A2에서 이를 대신 구현하지 않는다. 시작 재화·포기/재진입 계약과 기존 E/E2 정밀 회귀의 잔여 조건도 유지한다.

### 7.20 e0b02cc 병합 인수와 잔여 검증 (2026-10-05)

**검증 근거:** [Stage 병합 검증 기록](../handoffs/2026-10-04-stage-merge-verification.md#verification)의 Unity 6000.0.73f1 Core·Framework·CombatViewBoundary **2,255/2,255 통과**, 실패/건너뜀 0, 종료 코드 0을 인수한다. 이번 세션에서 재실행한 결과가 아니며 Git에 없는 원본 XML/log를 새로 확인했다고 주장하지 않는다. §7.17·§7.19의 당시 Unity 미실행 상태는 이 인수 기록으로 갱신한다. 전체 통과를 모든 수동 재생·운영 경로의 완료로 확대하지 않는다.

**현재 연결:**

- `CombatViewSystem`은 전체 결과의 소비·마지막 다시 그리기·Acknowledge를 소유하고 `ICombatViewService`로 노출된다. 최초 C의 서비스 미등록/Immediate 전용 설명은 과거 이력이다.
- `CombatPathInput` → `CombatMovementInteraction`은 경로 Drag·Core 견적·검증 context·Return 입력을 연결한다. 배치·공격은 기존 `CombatInteraction`에 남는다.
- `OrderedCombatPresentation`이 기존 View Tick 안에서 결과 이벤트를 순서대로 재생하고 `CombatMovementPlayback`이 이동 시간을 처리한다. 이동 뒤 다른 이벤트까지 끝난 다음 전체 결과를 한 번 확인한다. 비이동 VFX는 아직 추가하지 않았다.
- 실제 Combat 씬의 견적 표시·Return 버튼과 자동 씬 테스트가 포함됐다. 실제 DB·Render·Presenter를 로드한 이동/Return 자동 테스트도 전투 준비 상태는 fixture를 사용하므로 운영 기본 source 성공의 증거는 아니다.
- RecordInputOnly는 행위자나 이동 검증 context를 만들어 내지 않는다. 기존 배치·공격 기록은 유지하며, 검증된 경로 Move/Return 인수는 Live에서 수행한다. 예전 E.7/E2의 직접 이동 기록 기대는 현재 계약에 적용하지 않는다.

**Phase별 잔여 증거:**

| Phase / Task | 인수한 근거 | 추가로 확인할 것 |
|---|---|---|
| B3.4~5 / 골격 6A.7 | 진입·Bootstrap 경계 테스트를 포함한 병합 전체 회귀. 골격 등록 경로는 골격 §6.11 | 두 모드의 실제 Boot·정상 거절·잘못된 개발 설정·Stage 시작 중 미확인 수동 사례와 인계 마감 |
| E.7 | 기록 모드 경계 회귀와 이전 사용자 기능 확인 | 병합 후 배치·공격 기록, 처리/응답/확인 번호 유지, 개발 Layer와 대기 동작. 검증 context 없는 경로 Move는 기대하지 않음 |
| E.9 / E2 | 실제 씬의 Drag → Core 견적 → Move → 시간 기반 연출 → 확인 → Return/환불 자동 검증. 공격 취소 후 이동 복구·Draft 취소 후 Return 복구·오래된 응답 회귀 포함 | 카메라 줌·가변 프레임·UI 겹침, 배치/공격과 새 이동의 조합, 복수 밤·승패·마지막 결과 1회 확인의 누락 증거 대조. 새 결함/변경이 없으면 전체 테스트를 반복하지 않음 |
| E.8 / X3 / 골격 6B F1 | 콜백 중 Detach·Detach 예외 뒤 표시 자원 해제 시도 등의 병합 회귀 | 실제 Bootstrap/입력 경계와 오류 보고 1회·재Tick/전환 차단·이동 취소/늦은 콜백의 요구사항별 증거 연결. 부족한 사례만 보강 |
| A2.6 | 지정 방식/유닛 참조가 포함된 Framework 전체 통과와 실제 씬 DB 로드 기록 | 실제 카탈로그 + `useDevBattle=true` Boot 최초 표시 수동 확인. `useDevBattle=false` 준비는 골격 §0.2 해소 뒤 별도 |
| X6 / F2 | 기존 정상 전투 종료에서 마지막 결과를 소비하는 구조 | 공개 전환/포기/Completion 표시 계약과 실제 이탈·재진입 구현·검증. 병합에서 완료되지 않음 |

§7.19의 DB 생성 테스트와 운영 source 성공은 다른 완료 조건이다. Core의 기본 source는 여전히 T1/G7에서 중단되며, 임시 Core 런타임의 재화·전투 정의 주입도 운영 경로가 아니다. 차단 사유·담당·해소 기준은 [골격 계획 §0.2](2026-09-30-combat-flow-skeleton-plan.md#02-구현-차단-사유와-해소-작업)만 갱신한다.

이번 작업은 잔여 Phase·상태·계약 대기 사항의 문서 정리다. Unity 재실행, View/Core 코드 구현, 커밋·push·외부 전달은 하지 않았다.

### 7.21 잔여 검증 완료 확인과 6B F1 보완 (2026-10-05)

- 사용자가 통합 순서의 1·2번(골격 6A/View B3 마감 및 View E/E2/A2 잔여 검증)이 끝났다고 확인하고 6B 진행을 요청했다. B3.4~5·E.7·E.9·A2.6을 해당 확인과 §7.20의 병합 증거로 마감했다. 추가 개별 로그/테스트 수치를 받은 것은 아니며 운영 T1/G7·F2나 이후 수정한 코드의 통과 증거로 확대하지 않는다.
- E.8·X3는 골격 6B F1과 공동 검증한다. Return·포커스/캡처 상실·씬 비활성화 콜백을 공통 오류 경계에 연결하고, 이동 입력·연출 정리의 부분 실패/재진입을 보완했다. 상세 파일·테스트·실행 결과는 [골격 계획 §6.12](2026-09-30-combat-flow-skeleton-plan.md#612-phase-6b-f1-보완과-core-요청-2026-10-05)에 한 번 기록한다. **보완 코드의 Unity 결과는 요청 후 대기 중**이며 E.8을 완료로 올리지 않았다.
- 새 경계·Core 서비스·Buffer·씬 편집은 없다. 공개 F2 소유자·Completion 표시 완료·포기 API 연결은 이번 F1 수정에 포함하지 않았다. Core에 전달할 요청은 [2026-10-05 Core 요청사항](../handoffs/2026-10-05-combat-core-requests-phase6b.md)이 원본이다.
- 운영 DB 인수는 개발 source 미주입과 정상 로스터/챕터/진입 호출 경로가 모두 필요하다. 현재 Bootstrap의 Combat 단축 시작은 개발 설정을 끄면 직접 진입을 거절하므로 이전 `useDevBattle=false` 안내를 토글 하나만으로 완료되는 절차로 읽지 않는다. 해당 호출자 정렬은 운영 인수 시 골격·View 작업이다.

### 7.22 골격 6B 정상 종료 표시 연결 (2026-10-05)

사용자 지시로 F2의 정상 종료 부분부터 연결했다. 이탈 순서·API의 원본은 [골격 설계 §6.4](../specs/2026-09-30-combat-flow-skeleton-design.md#64-정상-승패-이탈의-구체-계약-2026-10-05), 통합 실행 기록은 [골격 계획 §6.13](2026-09-30-combat-flow-skeleton-plan.md#613-phase-6b-정상-f2-선행-구현재화-요청-정정-2026-10-05)이다.

- `ICombatViewService`가 좁은 `ICombatExitPresentation`을 제공한다. 새 제출 정지와 오류용 StopInput을 구분해 기존 Playback/명령을 보존한다. 일반 결과 후처리에서 정지한 입력을 다시 연결하지 않는다.
- `CombatViewSystem.PresentCompletion` → `CombatSceneDisplay` → `CombatHud`의 승패 확인 버튼을 연결했다. 확인은 1회, Detach/오류 뒤 오래된 콜백은 무효다. 일반 결과 Acknowledge 값을 Completion 확인에 사용하지 않는다.
- Combat 씬 Canvas에 초기 비활성 `CombatCompletion` 버튼과 두 HUD 참조를 추가했다. 최초 표시/정리에서 숨기며 결과 확인 뒤에만 표시한다. 승패별 화면 목적지·자동 요청 호출자는 답변 대기이며 공개 종료 API는 목적지를 인자로 받는다.
- 실제 View의 제출 정지/지연 연출 보존, 확인 중복/Detach 수명 테스트 3건을 추가했다. MSBuild 컴파일 통과, Unity 실행·씬 시각 검증은 미확인이다. 상세 실행 항목은 [Unity 검증 문서](../handoffs/2026-10-05-combat-phase6b-unity-validation.md)를 따른다.
- Stage 골드·Run 재화는 Core 요청 문서 §1.1에서 구분했다. View의 현재 공통 잔액 표시를 새 Stage 골드 읽기에 연결하는 일은 실제 Core API 인수 뒤 수행한다. 지급·초기화·잔액 변경 코드는 추가하지 않았다.

X6 전체, 포기, 운영 DB·재화·실제 화면 인수는 아직 완료하지 않았다.

### 7.23 커밋 검증 인수와 일반 승리 복귀 연결 (2026-10-05)

- 사용자 확인으로 `5078ee0e`의 F1 보완·정상 F2 표시/정리 기반을 인수했다. E.8을 마감하며 §7.21~22의 Unity 대기는 과거 기록이다. 이미 검증한 커밋의 확인을 반복 요청하지 않는다.
- 골격의 일반 승리 복귀 호출자가 기존 Completion 확인 UI를 사용한다. 사용자 결정·호출 정책은 [골격 설계 §6.4](../specs/2026-09-30-combat-flow-skeleton-design.md#64-정상-승패-이탈의-구체-계약-2026-10-05), 코드/실행 기록은 [골격 계획 §6.14](2026-09-30-combat-flow-skeleton-plan.md#614-phase-6b-일반-승리-복귀-호출자-연결-2026-10-05)가 원본이다. 이번 작업에서 View 생산 코드·씬은 수정하지 않았다.
- 패배·최종 승리 목적지는 보류, 자발적 포기는 Core API 인수 후 연결한다. StageSelect 선택 UI·Run 종료 화면은 별도 작업이며 View가 Run 재시작·재화 지급/초기화를 수행하지 않는다.

### 7.24 Phase F·골격 7 공동 문서 인계 (2026-10-05)

사용자 지시로 동일 세션에서 F.1~4를 완료했다. 코드 기준은 `bfe63a27`이며 View 설계 11차·현재 통합 UML·공동 Framework 공유·Core 메모·새 로컬 인계를 갱신했다. 실제 파일과 확인 내역은 [골격 계획 §6.15](2026-09-30-combat-flow-skeleton-plan.md#615-phase-7view-f-공동-문서-인계-2026-10-05)에 한 번 기록한다.

이 문서 Phase의 완료는 6B 전체, 운영 DB 준비, 두 재화 분리, 포기·보류한 결과 화면 완료를 뜻하지 않는다. 기존 완료 검증은 유지하며 이번 문서 작업에서 Unity를 재실행하지 않았다. 외부 담당자에게 발송하거나 새 API·승인을 받았다고 표시하지 않는다. 다음 시작점은 [공동 인계](../handoffs/2026-10-05-combat-view-skeleton-session-handoff.md)와 골격 계획 §0.1·§0.2다.

### 7.25 View 후속 세션 인계 (2026-10-05)

사용자는 View 후속 작업을 다른 세션에서 진행하도록 인계 방법 정리를 요청했다. 시작 HEAD는 문서 정리 커밋 `c3bc5ee1`, 작업 트리는 깨끗했다. [View 전용 다음 세션 인계](../handoffs/2026-10-05-combat-view-next-session-handoff.md)에 읽을 순서, 완료 상태 유지, Core/목적지 대기, 파일 소유 경계, 직전 구조 평가와 새 세션 시작 문구를 기록했다.

구조 평가의 보완 후보는 X1·X3·X4에 연결하며 구현 완료나 전체 리팩터링 착수로 표시하지 않는다. §0.1의 옛 검증 대기 표현도 기존 §7.21~24의 완료 인수에 맞췄다. 이번 변경은 문서뿐이며 생산 코드·씬·DB·검증 결과를 변경하지 않았다. 기존 테스트·Unity를 재실행하지 않았고 커밋·push·다른 세션 생성/메시지 전송은 하지 않았다.

### 7.26 View 후속 보완 설계·계획 (2026-10-05, 구현 미착수)

사용자가 View 전용 인계 확인 후 Core 없이 가능한 책임 분리·반복 할당 개선의 작은 Phase 계획과 SOLID 5원칙 준수를 요청했다. `feature/Yonggeun/Stage`, HEAD `c3bc5ee1`에서 현재 코드·인계·설계·UML을 대조했다. 시작 로컬 변경은 §7.25의 인계 관련 문서뿐이며 이를 보존했다.

[후속 보완 설계·구현 계획](2026-10-05-combat-view-maintenance-plan.md)에 V1 포인터 검사 재사용 → V2 불변 초안 Snapshot 재사용 → V3 중복 표시 억제 → V4a 소비자 계약 분리 → V4b 사건 표현 상태 추출을 정리했다. 각 단위의 수정 파일·유지 계약·기대 효과·영향 회귀와 SOLID 적용 기준을 명시했다. ViewSystem 입력 분배 추출과 사건별 증분 렌더링은 확장/측정 조건이 생길 때의 후속 후보로 남긴다.

§7.21~24의 완료 구현·검증 상태는 유지한다. Core T1/G7·재화 분리·포기 실제 API 인수와 골격/화면 소유 작업은 별도로 연결했다. **이번에는 계획 문서만 작성했으며 신규 Phase 구현, 빌드·테스트·Profiler·Unity 재실행은 하지 않았다.** 현재 UML도 구현된 구조를 그대로 유지한다. 커밋·push·외부 메시지 전송 없음.

### 7.27 V1 포인터 UI 검사 재사용 (2026-10-05, 코드 완료·Unity 검증 대기)

사용자가 변경 후 설계 재평가 보완점 기록과 V1 착수를 지시했다. 기준 HEAD는 `c3bc5ee1`이며 이전 문서 변경을 보존했다. [후속 계획 §3.1·V4b](2026-10-05-combat-view-maintenance-plan.md)에 Mapper가 Field.Build 이후 생기는 점과 연결·Clear/Release·재설치 순서를 명시했다. V4b 추출과 V2/V3/V4a는 구현하지 않았다.

**구현:** `CombatSceneDisplay`에 EventSystem별 Raycast 전용 PointerEventData를 재사용한다. 호출마다 used·position·displayIndex를 갱신하며 Raycast·카메라·필드 좌표 변환은 계속 수행한다. UI hit의 LINQ Any를 인덱스 순회로 바꿨고 손패 취소 영역과 GraphicRaycaster hit의 OR 의미를 유지했다. Raycast 결과 List는 finally에서 비워 예외 때도 씬 참조를 놓는다. EventSystem 없음/교체와 Release에서 캐시를 폐기하며 Release 뒤 입력은 새 데이터를 만들지 않고 false를 반환한다. 공개 계약·Core·골격·씬·DB 변경은 없다.

**추가 테스트:** `CombatPointerMappingTests.cs`와 meta. 실제 Combat 씬·EventSystem·GraphicRaycaster를 사용해 입력 객체 재사용/위치 갱신, UI·비UI 혼합 및 같은 좌표의 UI 변경, EventSystem 교체/없음/파괴, 카메라 변경/손패 영역/격자 밖, Raycast 예외, Release/late call, 워밍업 후 반복 호출을 확인하는 8건이다. 테스트는 기존 EventSystem 활성 상태와 current를 복원한다. Combat 씬이 이미 열려 있으면 사용자 씬을 닫지 않도록 새 fixture는 skip한다.

**실행한 검증과 한계:**

- 현재 소스와 새 테스트를 포함한 `ProjectL.Tests.Framework` 및 의존 어셈블리 MSBuild 성공, 오류 0·변경 파일 경고 0. 기존 Core nullable·Audio using·테스트 미할당 필드 경고는 남아 있다. 로그: `Temp/CodexViewV1/ProjectL.Tests.Framework.log`.
- `MovementDraftTests.UIReentry_RequiresEndpointOrPredecessor_AndNeverBridgesGap` 관리 코드 실행 **1/1 통과**. PowerShell 7 실행 결과이며 Unity Test Runner 결과는 아니다. Windows PowerShell 5 시도는 reference assembly 로드 제한으로 실행하지 못했고, 정상 런타임에서 재실행했다. 최종 로그: `Temp/CodexViewV1/managed-smoke.log`.
- Unity 새 테스트·실제 씬 Drag/Playback/Return 회귀·할당 수치는 **미확인**. 기존 에디터는 새 소스를 자동 반영하지 않았고, 별도 batch 시도는 동일 프로젝트가 다른 Unity 인스턴스에서 열려 있다는 이유로 테스트 시작 전에 종료됐다. 로그: `Temp/CodexViewV1/unity-batch.log`. 기존 에디터를 닫거나 씬을 바꾸지 않았고 임시 실행 보조 파일은 제거했다.
- 남은 V1 검증: Unity Test Runner/EditMode에서 `CombatPointerMappingTests` 8건, `CombatDisplayReuseTests.AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters`, `MovementDraftTests.UIReentry_RequiresEndpointOrPredecessor_AndNeverBridgesGap`을 실행한다. Combat 이외의 씬에서 시작하며, WarmMapping 테스트의 512회 할당 출력은 Raycaster 전체 비용과 구분해 해석한다. 수치가 없으므로 무할당/FPS 개선을 완료 주장하지 않는다.

기존 완료 구현·검증 상태는 유지한다. V1은 코드 완료·Unity 검증 대기이며 V2 이후로 자동 진행하지 않았다. 커밋·push 없음.

### 7.28 V2 불변 초안 Snapshot 재사용 (2026-10-05)

사용자가 V3까지 진행하도록 지시했다. 기준 HEAD는 `c3bc5ee1`이며 V1과 기존 로컬 변경을 보존하고 V2를 먼저 구현·검증했다.

- `MovementDraft`가 활성 초안의 불변 `MovementPath`를 한 번 만들어 재사용한다. Begin·셀 추가/삭제·접두 절단·Cancel처럼 Revision을 바꾸는 경로에서 캐시를 비운다. 유효성만 바뀌면 경로는 유지하고 새 초안에는 같은 행위자·원점이라도 이전 객체를 돌려주지 않는다.
- Core의 MovementPath 불변 복사와 Interaction의 요청/context/전투 수명 검증은 그대로다. 이전 Snapshot은 이후 편집에도 유지되고 Cancel 뒤 Snapshot은 기존처럼 예외다.
- `MovementDraftTests`에 재사용·편집/이전 값 보존·새 초안/취소·반복 할당 4건 추가. Framework/의존 어셈블리 MSBuild 오류 0·변경 파일 경고 0, 관리 코드 **29/29 통과**. 워밍업 후 같은 Snapshot 1,000회 읽기 **관리 힙 할당 0바이트**. 이는 PowerShell 7/.NET 실행 측정이며 Unity 프레임/Player 측정이 아니다.
- 증거: `Temp/CodexViewV2V3/v2-compile-summary.log`, `v2-smoke.log`. V2 코드·관리 코드 검증을 마친 뒤 V3로 진행했다.

### 7.29 V3 초안·견적 중복 표시 억제 (2026-10-05, 코드 완료·Unity 표시 검증 대기)

- `CombatPathInput`은 Snapshot·Interaction이 수락한 응답·PointerValid가 모두 같을 때만 ShowDraft를 생략한다. 응답/입력 허용 검증과 Return 처리는 그대로 실행한다. Clear/Cancel/Detach/새 초안에 표시 캐시를 비우며, 표시가 성공하고 표시 버전이 유지된 경우만 캐시를 저장한다. 실패 재시도·Cancel/Detach/새 드래그 재진입 때 이전 캐시가 복원되지 않는다. 표시 버전은 Core/결과 수명의 원본이 아니다.
- `CombatHud`는 이동/복귀 표시값만 값 튜플로 보관해 문자열 생성 전에 비교한다. BindReturn은 복귀 캐시를 무효화하고, Clear 및 OnEnable/OnDisable은 표시 캐시를 무효화한다. Return 버튼 활성 상태는 매번 반영하므로 같은 견적이어도 재바인딩 뒤 복구된다. 문구·Completion 동작·명령 제출·결과 확인 순서는 유지했다.
- 새 `CombatMovementFeedbackTests` 11사례: 같은 응답 반복, 포인터 유효성, 입력 차단, 수락되지 않은 옛 응답, 같은 경로의 새 비용/context 거절, 거절 접두, 취소/재시작, 표시 실패, Cancel/Detach/새 초안 재진입, 중복 Return 제출. `CombatDisplayReuseTests`에 실제 HUD 문구/상태 변경·재바인딩·Clear/재활성화·반복 문자열 할당 4건 추가.
- 최종 Framework/의존 어셈블리 MSBuild 오류 0·변경 파일 경고 0. 관리 코드 **45/45 통과**: MovementDraft 29 + MovementFeedback 11 + PathInputCleanup 1 + PresentationCleanup 4. V2의 29건은 이 합계에 포함되므로 두 결과를 더해 고유 사례 수로 보고하지 않는다. 증거: `Temp/CodexViewV2V3/v3-compile-summary.log`, `v3-smoke.log`.
- **Unity 검증 대기:** 새 HUD 4건, `CombatMovementViewTests`, 실제 씬 Drag/Playback/Return 회귀, V1 포인터 테스트 8건. V1에서 확인한 동일 프로젝트 Unity 중복 실행 제한은 해소됐다고 가정하지 않았으며 이번에 batch를 다시 띄우지 않았다. Unity Test Runner 결과·HUD 할당 수치·프레임 성능은 새로 확인하지 않았다. 새 HUD 테스트는 테스트 데이터 생성·워밍업을 제외한 동일 견적 512쌍 호출을 측정한다.

기존 완료 검증은 유지한다. 후속 계획 체크는 코드 반영 상태이며 Unity 실행 대기를 완료로 올리지 않는다. V4a/V4b·R1/M1과 Core/골격/씬/DB는 변경하지 않았다. UML 비교 자료는 V1~V3 코드 반영과 V4 예정 상태를 구분했다. 커밋·push 없음.

### 7.30 V4a 소비자별 표시 계약 분리 (2026-10-05)

사용자 요청으로 V4까지 진행하되 앞선 재평가의 Mapper 수명 보완도 적용했다. 기준 HEAD `c3bc5ee1`, V1~V3와 기존 로컬 변경을 보존했다.

- `ICombatMovementFeedback`, `ICombatMovementPlaybackPresenter`, `ICombatPresentationControl`과 meta 추가. PathInput은 피드백, MovementPlayback은 이동 재생, Ordered는 이동 재생·결과 표시·Tick/Cancel의 좁은 참조만 보관한다.
- 기존 `ICombatMovementPresenter`와 `ICombatBattleDisplay`는 상속 묶음으로 유지한다. `ICombatPresentation.Attach`, View 서비스·골격 API와 실제 메서드 의미는 그대로다. 별도 서비스·기본 no-op 구현·adapter는 추가하지 않았다.
- V4a 단계에서 현재 소스/새 파일을 포함한 Framework 및 의존 어셈블리 MSBuild 오류 0, 관리 코드 **46/46 통과** (`Temp/CodexViewV4/v4a-smoke.log`). Feedback 테스트의 대역은 좁은 피드백/매핑 계약만 구현하도록 전환했고, 좁은 이동 대역의 완료 관측·Return·멱등 Detach 회귀를 추가했다.
- 최종 확인에서는 지연 사건의 취소·옛 콜백·다음 결과·중복 완료 테스트까지 추가해 **47/47 통과**했다. Unity 기반 MovementView/ViewSystem 회귀는 아래 대기 항목이며 관리 코드 실행으로 대체하지 않는다.

### 7.31 V4b 사건 표시 상태 추출과 Mapper 수명 보완 (2026-10-05, 코드 완료·Unity 검증 대기)

- `CombatSceneEventPresenter`(internal sealed, meta 포함)가 표시 캐릭터/구조물 목록·이동 사건·경과 시간·보간과 사건 반영을 소유한다. SceneDisplay는 같은 상태를 보관하지 않고 사건 메서드만 위임한다. 씬 구성·입력/UI·HUD/손패·고스트·풀 반환은 기존 소유자에 남겼다.
- **Mapper 보완 적용:** 생성/Attach 시 Mapper를 캡처하지 않는다. Install 진입에 이전 사건 연결 해제 → Field.Build 성공 → 현재 Mapper Bind → 사실 Refresh 순서다. 설치 중 어느 단계가 실패해도 사건 연결은 해제한다. Clear/Release는 보간·사건 참조·목록·Mapper를 비우고, 재설치는 새 필드 Mapper를 받는다. 이전 필드가 남은 Build 실패에도 사건을 재개할 수 없다.
- Install 전·Clear/Release 후 BeginStep/ShowReturn/BeginEvent는 상태 예외로 거절한다. 유휴 Tick/Cancel은 안전하게 종료한다. Cancel은 보간만 중단하며 결과 완료를 호출하지 않는다. 표시 중 Unity 활성 상태 콜백이 취소하면 이동을 되살리거나 비이동 완료를 호출하지 않는다. 내부 표시 버전은 해당 동기 호출의 재진입 방어이며 결과 순번·확인 토큰을 복제하지 않는다.
- Refresh는 사실 목록을 한 번 채운 뒤 기존 `RenderFacts`에 넘긴다. SceneDisplay 경로의 Select/ToArray 중복 배열이 제거됐다. 사건별 전체 순회·ID 목록 검색은 유지하고 M1의 Dictionary/증분 렌더링을 도입하지 않았다. Release의 Return 버튼 해제도 나머지 정리와 같은 예외 집계에 넣었다.
- 새 `CombatSceneEventPresenterTests` **9사례**: 최초 Install/Release 후 호출 차단, 이동 중 Clear/재설치/풀 재사용, 이전 필드가 남은 Build 실패와 재시도, 이동→공격→사망 순서/완료 1회/최종 Refresh, Cancel/Return, Soul/구조물 사건, 워밍업 사실 Refresh 할당 비교, 활성 상태 변경 중 Cancel 재진입 2사례.
- **실행 증거:** 최종 Framework/의존 어셈블리 MSBuild 오류 0·변경 파일 경고 0. 관리 코드 **47/47 통과**(Draft 29 + Feedback 11 + PathInputCleanup 1 + PresentationCleanup 6). `Temp/CodexViewV4/v4-compile-summary.log`, `v4-smoke.log`. 이는 Unity 밖 PowerShell 7/.NET 실행이다. V4a 46건과 중복이므로 합산하지 않는다.
- **Unity 실행 대기:** 새 9사례, 기존 `CombatMovementViewTests`·`CombatViewSystemTests`·`CombatDisplayReuseTests.AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters`, V1 포인터 8건·V3 HUD 4건. 앞서 동일 프로젝트 Unity 중복 실행이 거절돼 batch를 반복하지 않았다. Test Runner는 Combat 외 씬에서 실행한다. 새 fixture는 이미 열린 사용자 Combat 씬을 닫지 않고 Ignore한다. Unity 결과 XML·실측 할당 차이·FPS 수치는 아직 없다.

View 설계 §24, 현재 UML과 전후 비교를 실제 구현에 맞춰 갱신했다. 기존 완료 검증은 유지하며 Core/Buffer 생산 코드·골격·씬·DB는 수정하지 않았다. 커밋·push 없음. 후속은 Unity 회귀 확인이며 R1/M1은 조건이 생길 때 별도 범위로 진행한다.

### 7.32 드래그 예정 경로 강조 및 클릭 이동 계약 확인 (2026-10-06)

- 기준 `525e3975`. 사용자 요청으로 기존 Range와 분리한 `MovementDraft` 강조 층과 `FieldView.movementDraftColor`(기본 청록)를 추가했다. 기존 호버/공격 영향 우선순위를 유지하고 초안은 일반 범위보다 우선한다. `ShowDraft`는 받은 경로를 표시하고 끝점은 대기 노랑·유효 초록·무효 빨강으로 구분한다. `ClearDraft`는 일반 범위를 지우지 않으며 V3 표시 캐시는 유지한다.
- 취소 조작은 변경하지 않았다. 현재 PlayerInputActions의 Combat Cancel은 우클릭이며 드래그/미제출 경로를 취소한다. 취소 영역·필드 밖 Release도 취소다. 제출 뒤 명령은 취소하지 않고, Core가 허용할 때 Return으로 체크포인트 복귀한다. Escape 취소 바인딩을 추가한 것은 아니다.
- Core 조사: `CombatViewPreviewData.Move(unit, path)`와 `CombatViewCommandData.Move(unit, path, context)`는 전체 경로를 요구한다. `EnemyMovementPathfinder`는 적 AI의 공격 위치 탐색 전용 내부 구현이다. `CombatMovementCalculator.Range`의 BFS는 범위/비용을 반환하며 실행 경로를 제공하지 않는다. 클릭 목적지 자동 경로를 추가할 경우 Core의 플레이어용 경로 제안·견적/context 계약과 최단/위험 회피/동률 우선순위를 먼저 확정해야 한다. 이번에는 해당 API나 클릭 입력을 구현하지 않았다. 설계 §21.1의 옛 이동 클릭 설명은 현재 계약으로 정정했다.
- 기존 강조 우선순위 회귀를 확장하고 `CombatPointerMappingTests.DraftHighlight_ChangesPathAndEndpoint_AndClearPreservesUnderlyingRange`를 추가했다. 실제 씬에서 대기/승인/포인터 무효 끝점색, 이전 경로색 제거, 취소 후 일반 범위 복원을 확인하는 테스트다.
- 현재 소스 Framework/의존 어셈블리 MSBuild 오류 0. 관리 코드 **48/48 통과**(V4의 기존 47 + 강조 우선순위 1). 증거: `Temp/CodexMovementHighlight/compile-summary.log`, `smoke.log`. 추가한 실제 씬 테스트 1건과 색 가독성은 Unity 실행 대기다. V1~V4의 기존 대기 상태는 유지한다.
- Core/Buffer·골격·씬/DB·입력 바인딩 변경 없음. 기존 FMOD 캐시 변경은 보존했다. 커밋·push 없음.

### 7.33 체크포인트 복귀 R 단축키 (2026-10-06)

- 사용자 요청으로 `Combat/Return` Button 액션에 `<Keyboard>/r`을 연결했다. Controller → 수신 슬롯 → `IStageInputReceiver.RequestReturn()`으로 전달하며, 실제 View는 UI 버튼에 쓰던 `CombatViewSystem.RequestReturn()`을 그대로 사용한다.
- 기존 이동 모드·입력 허용 검사와 최신 Core 복귀 미리보기 승인 뒤 제출을 유지한다. Core/Buffer·골격·환불 규칙 변경은 없다.
- 기존 입력 전달 테스트에 Return을 포함하고 Unity 입력 fixture에도 액션을 추가했다. 입력 사용 안내에 R과 공용 수신 경로를 기록했다.
- 검증: 현재 소스 MSBuild 성공(기존 경고만), 순수 `StageInputTests` 18/18 통과(.NET 별도 실행), 실제 입력 에셋 JSON의 `Combat/Return` → `<Keyboard>/r` 연결 확인, `git diff --check` 통과. Unity 실제 키 입력·복귀/환불 재생은 미검증이다. 검증 도구/빌드 로그: `Temp/CodexReturnShortcut/`.
- 수동 확인: 전투 이동 모드(Q)에서 이동 연출이 끝난 뒤 R → 체크포인트 복귀 및 미확정 이동 비용 환불. 복귀 불가 상태에서는 기존 검증으로 명령 제출이 막혀야 한다. 커밋·push 없음.

### 7.34 Q 진입·마우스 경로 지정·좌클릭 확정 (2026-10-06)

사용자가 승인한 조작: Q → 활성 유닛 시작 칸 진입 → 버튼을 누르지 않은 마우스 이동으로 경로 지정 → 좌클릭 확정 / 우클릭 취소. 실행한 이동의 복귀는 R이다.

- 설계: 이동 모드와 경로 지정 수명을 구분한다. `CombatPathInput`이 Q 이후 시작 칸 대기·지정 수명을 소유하고, `CombatMovementInteraction`은 기존 경로 편집·견적·승인 후 제출을 유지한다. Controller·슬롯은 기존 입력 사실을 전달하며 Core/Buffer·골격은 변경하지 않는다.
- Q 이전에는 마우스 이동/클릭으로 경로를 만들지 않는다. Q 시 활성 행위자와 시작 앵커를 고정하고 실제 시작 칸에 진입해야 수집한다. UI/필드 밖을 통과한 좌표로 자동 연결하지 않는다. UI/필드 밖 클릭은 확정하지 않는다.
- 좌클릭 누름으로 경로를 고정하고 최신 Core context 승인 뒤 1회 제출한다. 놓기는 이동을 제출하지 않는다. 확정·취소 뒤 자동 재시작하지 않고 다음 Q를 기다린다. 모드/턴/행위자 변경·포커스 상실·입력 해제는 기존 정리 경계에서 지정 수명을 끝낸다.
- 작업 순서: 입력 수명과 View 연결 수정 → 기존 이동/표시 테스트를 새 조작으로 전환하고 시작 대기·중복 확정·취소/늦은 응답 회귀 확인 → 컴파일·실행 가능한 관리 테스트 → 설계/조작 문서 갱신. Unity 실제 입력 재생은 별도로 상태를 기록한다.

실행 결과:

- PathInput에 시작 칸 대기와 행위자/앵커 수명 검사를 추가했다. Q는 `BeginPlanning`, 포인터는 경로 수집, 좌클릭 누름은 `Confirm`으로 연결했다. 놓기는 무동작이다. 확정·취소·모드/입력 수명 종료 뒤 Q 없이 재시작하지 않는다.
- 기존 경로·표시 테스트와 실제 View/씬 통합 테스트의 조작을 바꿨다. 신규 회귀 9건: Q 전 무동작, 시작 칸 대기, 놓기 무동작/단일 제출/재시작 방지, UI/필드 밖 클릭 2건, 대기/확정 후 취소 2건, 입력 차단 중 대기/편집 정리 2건.
- 현재 소스 MSBuild 성공(기존 경고만), 관리 회귀 57/57 통과. `Temp/CodexHoverPath/`에 빌드 도구/로그와 `smoke.log`를 남겼다. 실제 Unity EditMode/키 입력 재생은 미검증이며 기존 검증 대기 항목을 완료로 바꾸지 않는다.
- 설계 §25에 전후 조작과 상태 UML, 입력 안내 §2.2에 현재 조작을 반영했다. Core/Buffer·골격·씬/DB 변경과 커밋·push는 하지 않았다.

### 7.35 불가 후보 제외와 인접 칸 재지정 (2026-10-06)

- 사용자 승인: 유지 경로와 검증 중 후보를 분리한다. Core 응답 전에는 후보만 대기 색으로 표시하고, 거절 칸은 빨강으로 표시하되 경로 강조에 넣지 않는다. 거절 뒤 유지 경로 끝점에 인접한 다른 칸으로 바로 재지정한다. 먼 칸 건너뛰기/자동 우회는 하지 않는다.
- 책임: MovementInteraction이 응답으로 확인된 표시 경로와 후보/거절 상태를 관리한다. MovementDraft는 인접 편집만, PathInput은 표시값 전달/중복 생략만 담당한다. Core의 유효 접두 길이는 편집 정보이며 최종 실행은 전체 경로의 최신 context가 필요하다.
- UI/필드 밖 이탈은 기존 끝점/직전 칸 재진입 규칙을 유지한다. Core 거절 후 확정 대기를 풀고 경로 지정은 유지한다. 불가 후보를 클릭해도 이미 유지한 경로를 실행하지 않는다. 취소/Q 재시작/전투 교체 시 후보와 늦은 응답을 모두 무효화한다.
- 작업: 표시 계약에 후보 칸 전달 → 유지 경로 캐시/거절 후 인접 재시도 → 실제 Core 장애물·빠른 입력·늦은 응답·클릭 대기/취소 회귀 → 컴파일/관리 검증 → 문서 갱신. Core/Buffer·골격 규칙은 수정하지 않는다.

실행 결과:

- MovementInteraction에 확인된 표시 경로/후보 칸을 분리하고, 거절 후 다른 인접 후보 재개를 추가했다. 같은 거절 칸의 반복 요청과 먼 칸 연결을 차단하며, 불가 클릭/확정 후 거절에도 경로 지정이 유지된다. UI 이탈은 거절 응답 전후 모두 끝점 재진입 조건을 보존한다.
- `ShowDraft`에 후보 칸 인자를 추가해 SceneDisplay가 유지 경로 층과 후보 호버를 따로 표시한다. PathInput의 표시 캐시는 후보 위치까지 비교한다. 기존 Snapshot 재사용과 Core 전체 경로/context 검증을 유지한다.
- 현재 소스 MSBuild 성공(기존 경고만), 관리 회귀 **63/63 통과**. 신규 6건은 후보 분리/오래된 응답, 같은 거절 칸 반복/먼 칸 차단/대안, UI 이탈 전후 거절 2건, 되돌리기/분기 재검증, 유지 경로가 같아도 후보 표시 갱신이다. 기존 불가 확정·거절 이후 재개 테스트의 기대값을 승인된 새 동작으로 바꿨다.
- 실제 씬 표시 테스트도 후보 대기/불가 칸이 경로 층에 포함되지 않는 조건으로 보완했지만 Unity 실행은 미검증이다. 기존 View·씬 통합 테스트는 컴파일만 확인했다. 빌드/관리 실행 로그는 `Temp/CodexPathCandidate/`에 있다.
- 수동 확인: Q → 시작 칸 → 장애물 칸은 노랑 대기 뒤 빨강, 청록 경로에는 미포함 → 유지 끝점의 다른 인접 칸으로 커서를 옮기면 바로 후보 지정 → Core 승인 뒤 좌클릭으로 그 경로만 실행. 장애물 뒤 먼 칸을 가리키거나 불가 후보를 클릭해도 경로를 건너뛰어 실행하지 않아야 한다.
- 설계 §26·입력 안내 §2.2·현재 UML Markdown을 갱신했다. Core/Buffer·골격·씬/DB 수정 및 커밋·push 없음.

### 7.36 개발 fixture 수치·특성을 UnitDataSO에 맞춤, AP 부족 표시 (2026-10-06)

- 원인: Live에서 Sniper가 공격 1회 뒤 이동 1칸만 표시되고 두 번째 공격이 `ActionCostInsufficient`로 거절됐다. Core 규칙(턴 시작 AP = 최대 AP, 이동은 칸당 MoveCost만큼 AP 소모, 공격 코스트 > 남은 AP면 거절)대로이며, fixture 수치(AP 5·공격 4·이동 5)가 UnitDataSO(AP 3·공격 1·이동 3)와 달랐다. fixture는 B2.2 결정대로 DB를 읽지 않는다.
- 변경: `DevBattleInitializationSource`의 유닛 5종 체력·이동 거리/비용·사거리·공격력·최대 AP·공격 코스트·우선도·지연을 `Unit_*.asset` 값으로 옮겨 적고 출처 주석을 남겼다. `Unit` 도우미가 이동 거리/비용을 인자로 받는다. DB 읽기는 추가하지 않았다(B2.2 유지, 값 사본은 T1/G7 연결 때 fixture와 함께 사라진다).
- 특성(사용자 지시로 추가): 에셋과 같게 Akimbo `MoveAttackDiscountTraitSpec(1, 1)`, Sniper `FocusTraitSpec(1, 3)`. `Unit` 도우미가 특성을 `params`로 받는다. 지정 방식 외 옛 공격 필드(appointMethod·attackTargetMode 등)는 기존처럼 옮기지 않았다.
- **AP 부족 표시(사용자 지시):** `CombatInteraction`이 공격 범위 응답의 `AttackRangeResult.CanAffordAttack`을 보관한다. false면 범위 칸은 표시하되 호버는 미리보기 요청 없이 Invalid, 클릭은 제출하지 않는다. 모드 종료·결과 후 재요청 때 판정을 버리고 새 응답을 따른다. RecordInputOnly 무응답 경로는 기존처럼 막지 않는다. 설계 §21.1 표에 한 줄 추가. 기존 호버 공격 미리보기도 Core 전체 계산이라 AP 부족 시 Invalid였지만 제출은 막지 못했다.
- 테스트: `CombatInteractionTests`에 `AttackMode_CannotAfford_ShowsRangeAsInvalid_AndSubmitsNothing`, `AttackMode_CanAffordAgainAfterResult_SubmitsWithNewRangeAnswer` 추가(`AnswerAttackRange`에 판정 인자 오버로드).
- 검증: `ProjectL.Systems`·`ProjectL.Tests.Framework` MSBuild 성공(기존 CS0105·CS0649 경고만). fixture를 쓰는 테스트는 없다. Unity EditMode 실행·Live 재생은 미확인. 커밋·push 없음.

### 7.37 스테이지 배경 프랍 연결 (2026-10-06)

- 사용자 결정: 옛 develop-4week `Assets/Prefabs/MapProp/Map.prefab`을 모든 전투 스테이지에 같게 둔다. 설계 §27.
- 에셋: `Assets/ProjectL/Prefabs/Stage/MapProp/Map.prefab`(원본 GUID `eec185ea…` 유지, 폴더 meta 새로 생성). 참조 모델 rock_001~003·test.fbx는 이미 있다. **Ground·Cube 3개가 쓰는 머티리얼(`31321ba1…`)은 어느 브랜치에도 없어 빠진 채로 들어왔다**(Unity에서 머티리얼 지정 필요). `ViewGroup/StageViewProp/Item`에 `StageViewProp_Boss`·`_Normal_A~J` 11개(키 2101·1101~1110, 위치 보정 y -0.1), 표에 등록. 표에 남아 있던 대상 없는 참조 1개(`e0e2f9df…`, 에셋 없음)는 목록에서 뺐다.
- 코드: `StageViewPropTable`에 `TryGetStageViewProp` 추가. `CombatDisplayValues.StageKey`(선택 인자, 기본 0). `CombatViewSystem`이 InitData에서 StageViewProp 표, `BindServices`에서 `ICurrentProgressBufferReadSink`를 받는다(서명 변경, 테스트 3곳 갱신). 새 `Display/StageViewPropPresenter.cs`(+meta). `CombatSceneDisplay` 생성자에 표 인자 추가(테스트 2곳 갱신), Install에서 Show, Clear/Release에서 제거.
- 재생 정정: 처음엔 프랍 부모를 FieldView 오브젝트 Transform으로 잡아, Combat.unity에서 셀 기준(`cellsRoot` = `Cell`, 원점)과 떨어진 `Field`(873.8, 551.6, -7.0)에 생성돼 화면에 보이지 않았다. `FieldView.CellsRoot`를 노출하고 프랍을 셀 기준 Transform 아래에 둔다(보정값도 셀 기준).
- 검증: 새 파일을 포함한 `ProjectL.Systems`·`ProjectL.Tests.Framework` MSBuild 성공(새 경고 없음). Unity 컴파일·EditMode(`DataBaseAssetTests`, `FrameworkRuntimeCompositionTests`, Combat View 테스트)·재생 미확인. 커밋·push 없음.

### 7.38 전투 View 구조 UML·평가 (2026-10-06, 문서만)

- 사용자 요청으로 HEAD `b23758ac` 기준 전투 View 코드의 UML과 책임 경계·외부 경계·유지보수성·확장성·성능 평가를 [구조 평가 문서](../specs/2026-10-06-combat-view-structure-review.md)에 정리했다. 코드 변경·실행·측정 없음.
- 주요 지적: View의 Core 서비스 직접 호출(`GetTimeline`, `10f88c49`), 이동 입력 허용 조건 3곳 중복, 입력 분배·모드 소유 이원화, `CombatInteraction`·`CombatSceneDisplay` 책임 집중. 개선 착수는 사용자 결정.

### 7.39 전투 View 구조 개선 설계 VR0~VR6 (2026-10-06, 설계만)

- §7.38 평가의 개선 권고를 SOLID 기준으로 [구조 개선 설계](../specs/2026-10-06-combat-view-refactor-design.md)에 정리했다. VR0 타임라인 Core 요청, VR1 대기 Gate·이동 허용 규칙 Policy 통합, VR2 입력 분배 Router(기존 R1), VR3 Interaction 배치/공격 분리, VR4 PointerMapper 추출, VR5 의존성·스타일, VR6 타임라인 소비자 교체(Core 인수 후).
- 사용자 검토로 Gate 관계를 정정했다: 소유자는 ViewSystem(생성·Resolve·Clear), 제출은 Router(시작·턴 종료·드로우)·배치·공격, 경로 입력과 개발 패널은 Gate 대신 `facts.HasPendingCommand`를 본다(현재 설계 §2.3·VR1). 당시 비교 아티팩트 갱신 기록은 유지하되 이번 §7.40의 보완은 로컬 문서에만 반영했다.
- 최초 설계 당시 구현·Task 계획은 미착수였다. 이후 문서/설계 보완 요청에 따른 구체적인 Task 계획은 §7.40이며 생산 코드 구현은 아직 진행하지 않았다.

### 7.40 VR 설계 재검토 반영과 Task 계획 (2026-10-06, 문서만)

사용자가 개선안 평가의 보완 사항을 문서와 설계에 반영하도록 요청했다. 기준 HEAD `b23758ac`에서 현재 입력 처리 순서를 대조했다. [구조 개선 설계](../specs/2026-10-06-combat-view-refactor-design.md)를 계약 원본으로 하고, [구조 평가](../specs/2026-10-06-combat-view-structure-review.md)의 단정적인 SOLID·성능 판단을 정정했다. 기존 V1~V4 구현·검증 기록과 Unity 대기는 유지한다.

**보완한 계약**

- 공통 이동 정책과 연결·활성·오류·대상·전투 정체성 검사를 구분했다. Tick 응답/Return 갱신에도 검사하며 재진입 뒤 facts를 재수집한다.
- Router의 책임을 입력 의도별 순서 조정까지 명시했다. EndTurn은 거절되어도 경로를 먼저 취소한다. EnterCombat/Draw에 같은 취소를 일괄 추가하지 않는다.
- VR3의 선행을 VR2로 수정했다. Router → 명시적 대상 위임 → 하위 Interaction의 단일 경로, 이전 요청 무효화/표시 정리 → 새 소유자 활성 순서를 정했다.
- 정상 종료 SuspendSubmissions와 오류/Detach 정리를 구분하고 pending·마지막 결과 재생을 보존한다.
- Mapper의 생성·Bind·Clear/재설치·부분 실패·최종 Release 계약을 구분했다.
- 타임라인 게시 요청에 최초 값·갱신/무효화·버전 일치·빈 값·종료/재진입·길이/절단·계산 빈도를 추가했다. 요청 초안은 설계 VR0이며 미전달·미합의·미구현이다.
- UML을 입력 → 처리 → Buffer → 결과 표시의 세로 흐름과 객체 소유 관계로 나눴다. 기존 현재 코드 UML은 구현 전 상태로 보존했다.

아래 `C`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`다. 파일·타입 이름은 현재 파일 또는 예정 신설 파일이며, 테스트 이름의 신규 항목은 계획이다.

| Phase | 파일별 작업 | 완료에 필요한 검증 |
|---|---|---|
| VR0 | 설계 VR0 요청 초안 준비. 실제 전달/합의 시 API·소유자·증거 링크 기록 | 최초/갱신/빈 값/종료/재진입/길이/버전/빈도 항목 누락 없음. 문서 준비를 Core 인수로 처리하지 않음 |
| VR1 | `C/CombatCommandGate.cs` 신설, `CombatInputFacts`·`CombatInputPolicy`·`CombatViewSystem`·`CombatInteraction`·`CombatPathInput`·`CombatMovementInteraction` 연결. 기존 조건을 먼저 회귀로 고정 | Policy/Gate 표 테스트, `CombatViewSystemTests` 정상 종료·재진입, PathInput 정리/이동 피드백·초안 회귀. Tick 공격 중 Return 요청 0, 비활성/오류/Unbind 후 제출 0 |
| VR2 | `C/CombatInputRouter.cs` 신설. ViewSystem의 enum/Handle 의도 처리를 이동하고 사실/수명은 유지. 기본 명령 테스트를 Router/Gate로 이동 | Layer×모드×요청 분배, 성공/거절/중복 입력의 취소 순서, RecordInputOnly, F1 1회 보고, 입력별 할당 증가 없음 확인 |
| VR3 | `C/PlacementDragInteraction.cs`, `AttackTargetingInteraction.cs` 신설. Interaction은 명시적 대상 위임/전환 조합으로 축소 | 기존 Interaction 행동 회귀를 책임별 이동. 늦은 응답·AfterResult·inactive Cancel이 다른 표시/요청을 지우지 않음. 전환/재진입 시 중복 제출 없음 |
| VR4 | `C/CombatPointerMapper.cs` 신설, SceneDisplay의 집기/매핑 위임. SceneEventPresenter의 설치 후 Mapper 연결 계약 보존 | `CombatPointerMappingTests`, `CombatSceneEventPresenterTests`, `CombatDisplayReuseTests`: 재설치·부분 실패·Release·EventSystem/카메라 교체. 실제 Unity Drag/Playback/Return 별도 확인 |
| VR5 | SceneDisplay 필수 의존성 호출부, DisplayValues 선택 값 의미, 이름 있는 상수. 이동 파일 스타일 diff 분리 | 영향 컴파일, 필수 의존성 누락 확인, 스타일 diff 의미 변경 없음. 불필요한 새 행동 테스트는 추가하지 않음 |
| VR6 | Core 게시 구현 인수 후 ViewSystem의 ReadDisplayValues 교체·직접 Core 조회 제거. 필요한 HUD/DisplayValues 타입 연결 | 타임라인 계약 전체 회귀와 `FrameworkRuntimeCompositionTests`. 기존 HUD 동작 보존. 직접 호출 제거와 전체 예측 비용 측정을 별도 기록 |

**착수·의존 순서:** VR1 → VR2 → VR3, VR4는 독립 순서로 수행 가능, VR5는 VR1~VR4 뒤. VR0 초안은 View 작업을 차단하지 않으며 VR6만 실제 Core 게시 구현 인수를 기다린다. M1은 측정 근거가 생길 때 별도 범위다.

- [x] 개선안 평가의 보완 사항을 로컬 설계·평가·후속 계획에 반영하고 구현 Task/검증 기준 작성.
- [x] VR1 코드 구현·관리 코드 회귀(§7.41). 사용자 Unity 확인 완료.
- [x] VR2 코드 구현·관리 코드 회귀(§7.45). 사용자 Unity 확인 완료.
- [x] VR3 코드 구현·관리 코드 회귀(§7.45). 사용자 Unity 확인 완료.
- [x] VR4 코드 구현·빌드(§7.46). 사용자 Unity 확인 완료.
- [x] VR5 의존성·상수·스타일 정리(§7.47). 사용자 Unity 확인 완료, 공동 파일 공유 메모 미전달.
- [ ] VR0 외부 계약 전달·합의·실제 구현 인수 후 VR6 소비자 교체. (전달 메모 준비 완료·미전달, §7.41)

**이번 검증 범위:** 문서 링크·Phase 의존성·현재 코드의 입력 순서와 문서 일치 및 `git diff --check`. 생산 코드 변경이 없어 빌드·테스트·프로파일링은 실행하지 않았다. Core/Buffer/골격/씬/DB 및 기존 FMOD 에셋 변경은 건드리지 않았다. 커밋·push·외부 메시지·Claude 아티팩트 수정 없음.

### 7.41 VR0 전달 메모·VR1 Gate와 이동 허용 정책 구현 (2026-10-06, 코드 완료·Unity 검증 대기)

사용자가 Task 계획 작성과 VR0·VR1 진행을 지시했다. Task 계획은 §7.40(다른 세션 작성)을 원본으로 쓴다. 이 세션이 같은 내용으로 따로 만든 계획 파일은 중복이라 지웠다. 기준 HEAD `b23758ac`, 다른 세션의 동시 코드 변경은 없었다(git diff로 확인).

**VR0 (문서):** [타임라인 요청 메모](../handoffs/2026-10-06-combat-timeline-core-request-memo.md)를 만들고 DB 경계 인계 §13.9에 링크했다. 계약은 설계 VR0이 원본이고 메모는 현재 호출 위치·이유·View가 할 일/하지 않을 일·현강님 확인 사항만 담는다. 미전달·미합의·미구현.

**VR1 구현** (`C` = `ViewSystem/Combat/`)

| 파일 | 변경 |
|---|---|
| `C/CombatCommandGate.cs`(+meta, 신규) | 대기 원본 하나. `HasPending`·`Pending`·`TrySubmit`(제출 직전 `CanSubmit(facts, 현재 HasPending, kind)`)·`SubmitPending`(번호를 받을 때만 대기 기록, 표시 정리는 호출자)·`Resolve`(Command·번호 일치만)·`Clear` |
| `C/CombatInputFacts.cs` | `HasPendingCommand`·`IsPresenting`·`IsInputAvailable`·`IsPathTarget` 추가(선택 인자). 판단 시점 사본이라는 주석 |
| `C/CombatInputPolicy.cs` | `CanEditPath(facts)` = `CanSubmit(facts, facts.HasPendingCommand, Move) && !IsPresenting && !HasUnhandledCommand`. 가용성·대상은 보지 않음 |
| `C/CombatInteraction.cs` | 생성자에 Gate. `_pending`·`ResolvePending` 제거, 제출은 Gate 위임. `HasPendingCommand`·`Pending`은 Gate 값 읽기만. `Stop`은 대기를 지우지 않음 |
| `C/CombatMovementInteraction.cs` | `PresentationAllowsInput` 삭제. `Begin`·`PointerMoved`·`OnPreview`·`Confirm`이 호출자가 그 호출에 평가한 `allowed`를 받음(저장 안 함). 전투·요청 소유권 검사는 유지 |
| `C/CombatPathInput.cs` | 사실 공급자(`Func<CombatInputFacts>`)를 한 번 주입. 입구 = 수명·전투 일치 → `IsInputAvailable && IsPathTarget` → `Policy.CanEditPath`. 표시 콜백 뒤 확정 직전·Return 제출 직전은 다시 읽음 |
| `C/CombatViewSystem.cs` | Gate 생성(AttachDisplay)·`Resolve`(FinalizeResult)·`Clear`(StopInput/Detach, `SuspendSubmissions`는 보존)·해제. `ReadFacts`가 새 4개 값 수집. `AllowsPathInput` 삭제. 대상 판정식은 `IsPathPointerTarget()` 한 곳(입력 분배·facts 공용, VR2에서 Router로). 개발 패널은 `facts.HasPendingCommand`. 읽기 전용 `HasPendingCommand` |

**테스트 변경**

- 신규 `CombatCommandGateTests`(+meta) 5건: 대기 기록·두 번째 제출 차단(옛 facts 재사용 시점 규칙 포함), 일반 제출은 대기 없음, 정책 거부, Resolve(옛 `CombatInteractionTests.ResolvePending_*`를 기대값 그대로 이전), Clear.
- `CombatInputPolicyTests`: `CanEditPath` 표 12사례, 가용성·대상 무관 1건.
- `CombatViewSystemTests`: `StopInput_ClearsPendingStart_ButSuspendSubmissionsKeepsIt`. 옛 Interaction 테스트의 "Stop이 대기 해제, 명령 Buffer 보존" 기대값을 소유자로 옮기고 정상 종료 보존을 추가.
- `CombatInteractionTests`: 생성자에 Gate. `CancelPresentation_KeepsPending_StopClearsIt` → `CancelPresentationAndStop_DoNotClearGatePending`(소유권 이동에 따른 계약 변경. 해제 기대값은 위 ViewSystem 테스트로 이전).
- `MovementDraftTests`: 호출 55곳에 `Allowed` 인자만 추가(Assert 변경 없음, diff로 확인). Rig의 `Allowed`는 옛 `PresentationAllowsInput`과 같은 조건(P ≥ S, 미처리 명령 없음)을 호출마다 평가하는 호출자 대역.
- `CombatMovementFeedbackTests`·`CombatPathInputCleanupTests`: 생성자 인자를 신규 `PathInputTestFacts`(+meta, ViewSystem.ReadFacts와 같은 값을 실제 Buffer에서 읽고 가용성만 테스트 값)로 교체. Assert 변경 없음.

**검증**

- MSBuild: 새 파일을 포함한 `ProjectL.Systems`·`ProjectL.Tests.Framework` 오류 0. 변경 파일의 새 경고 0(남은 경고는 다른 작업자 UI 파일의 CS0649, Core CS8632, CS0105).
- 관리 코드 실행(Unity 내장 Mono + 반사 기반 실행기, 저장소 밖 스크래치패드): `CombatCommandGateTests`·`CombatInputPolicyTests` **43/43**, `CombatInteractionTests`·`MovementDraftTests`·`CombatMovementFeedbackTests`·`CombatPathInputCleanupTests` **87/87** 통과. Unity Test Runner 결과가 아니다.
- `GameObject`를 쓰는 `CombatViewSystemTests`(새 1건 포함)·`CombatMovementViewTests`·`CombatDisplayReuseTests`는 Unity 밖에서 실행할 수 없어 **Unity EditMode 대기**.
- 완료 기준 grep: `CombatPathInput`·`CombatMovementInteraction`에 `CombatInteraction`·Gate·S/P·미처리 명령 계산 없음.

**Unity에서 확인할 것:** EditMode 전체(특히 위 Unity 대기 3개 클래스), Play From Boot Live에서 Q 경로 지정·좌클릭 확정·R 복귀·E 공격·Space 턴 종료·시작(G)·드로우(A)가 이전과 같은지, 시작/턴 종료 대기 중 경로 입력이 막히는지.

Core/Buffer·골격·씬/DB 변경 없음. 기존 FMOD 에셋 변경은 보존. 커밋·push·외부 메시지 없음.

### 7.42 StageViewProp 크기 보정 (2026-10-06)

- 사용자 요청으로 `StageViewPropData`에 Inspector 편집용 `scaleOffset`과 읽기용 `ScaleOffset`을 추가했다. 기본값은 `Vector3.one`이며 프리팹 원본 로컬 크기에 곱하는 축별 배율이다.
- `StageViewPropPresenter.Show`에서 생성한 인스턴스에 `Vector3.Scale(프리팹 원본 localScale, ScaleOffset)`을 적용한다. 위치·회전 보정과 같은 설치 시점이며 같은 키 재호출로 배율을 누적하지 않는다. 실제 표시 확인은 값을 바꾼 뒤 전투 재진입/재설치로 한다.
- 설계 §27에 배율 의미와 적용 시점을 기록했다. 기존 프랍 SO 에셋의 사용자 수정(프리팹·위치·회전)은 수정하지 않았다. Core/Buffer 변경·커밋·push 없음.
- 검증: `ProjectL.Systems.csproj` MSBuild 성공(오류 0, 출력 경고는 변경 범위 밖 Core/UI 파일). `git diff --check` 통과. Unity Inspector의 신규 필드와 실제 크기 표시는 미확인이다. 작은 표시 설정 추가이므로 별도 새 테스트는 만들지 않았다.

### 7.43 셀 크기 배율과 StageMapView 사용처 조사 (2026-10-06)

- 사용자 선택에 따라 `StageFieldViewDataSO`의 데이터에 `cellOffsetSize`/`CellOffsetSize`를 추가했다. 기본값 `(1, 1, 1)`, 축별 유한한 양수 검증. 셀 외형뿐 아니라 간격·클릭 판정도 함께 조정한다.
- `FieldView.Build`가 원본 셀 크기에 X/Z 배율을 적용해 기존 간격 계산과 Mapper에 전달한다. `FieldCellHandle.ApplyLayout`은 Anchor의 절대 배율을 지정하여 기본/교대 셀의 풀 재사용 시 누적 확대를 방지한다. 원본 크기 캐시와 프리팹·SO 에셋 값은 변경하지 않았다. 설계 §28에 적용 시점과 경계를 기록했다.
- StageMapView 조사: `StageMapView_Default.asset` → `StageMapViewDataTable.asset` → `ViewGroup.asset` 등록 확인. ViewDataBaseFactory가 Table을 만들고 StageMapViewSystem.InitData가 Config를 읽는다. 실제 소비자는 StageNodeView(프리팹·간격·크기·시작 외형), StageLinkView(선 프리팹·높이·굵기), StageNodePicker(마스크·거리)다.
- StageMapView의 강조색·Hover/Select 이펙트·현재 마커 프리팹/이동 시간/점프 높이는 현재 생산 표시 코드의 소비 참조가 없다. 데이터 정의·DB 검증 참조만 남아 있어 SO 전체가 미사용인 것은 아니다. 이 조사에서 StageMap 코드는 수정하지 않았다.
- Unity 확인 항목: `(1,1,1)` 기존 크기, `(0.5,1,2)` 기본/교대 셀의 크기와 중심 간격, 셀 중심 클릭·유닛 위치 일치, 재진입 후 배율 비누적. 실행 결과는 아래 검증 기록과 구분한다.
- 검증: `ProjectL.Systems.csproj` MSBuild 성공(오류 0, 경고는 변경 범위 밖 UI 파일의 CS0649), `git diff --check` 통과. Unity 실제 표시·클릭 검증은 미실행이다. 사용자 에셋 변경/이동은 보존했고 커밋·push 없음.

### 7.44 Play From Boot 개발 필드 설정 (2026-10-06)

- 사용자 요청으로 설정 창에 필드 가로/세로·아군/적군 배치 깊이를 추가했다. 스크롤을 제공하며 Combat 시작과 개발 전투를 선택한 경우 편집한다. EditorPrefs에 저장해 다음 재생에 적용한다.
- `GameBootstrapEditorOverrides` → `GameBootstrap` → `DevBattleInitializationSource` 생성자로 네 값을 전달한다. 기본값은 fixture의 8×7·2/2 상수를 공유한다. Boot 씬·SO 에셋·Core·Buffer는 수정하지 않았다.
- 맵 축소 시 고정 배치가 범위를 벗어나지 않도록 메인/적/본부 좌표를 새 필드에 맞춘다. 기본 좌표는 유지한다. 모든 값 양수, 깊이 합 < 높이, 적/본부용 2칸 확보를 검사하고 잘못된 입력은 설정 창 안내와 source 생성 예외로 드러낸다.
- 검증: `Assembly-CSharp-Editor.csproj` 및 의존 Systems MSBuild 성공(오류 0, 변경 범위 밖 UI CS0649·System.Net.Http 버전 충돌 경고). 실제 빌드 DLL을 Unity 내장 Mono의 임시 반사 실행기로 호출해 유효 조합 **1,925건**의 전달값·배치 범위·적/본부 비중첩·정의 캐시, 잘못된 조합 **7건**의 거부를 확인했다. 기본 배치 좌표 유지도 통과했다. Unity Test Runner/실제 Play 결과와 구분한다.
- Unity 설정 창 조작·재생 시작 후 필드 표시 확인은 미실행이다. 설계 §29 갱신, `git diff --check` 확인. 기존 사용자 에셋 변경은 보존하며 커밋·push 없음.

### 7.45 VR2 입력 Router·VR3 배치/공격 분리 구현 (2026-10-06, 사용자 Unity 확인 완료)

사용자가 VR1 Unity 확인 완료를 알리고 VR3까지 진행을 지시했다. 기준 HEAD `0b51f59d`, 시작 시 작업 트리는 FMOD 캐시 외 깨끗했다. 설계 VR2·VR3와 §7.40 Task를 따른다(`C` = `ViewSystem/Combat/`, 신규는 .meta 포함).

| 파일 | 변경 |
|---|---|
| `C/CombatInputRouter.cs`(신규) | `Request`·`PointerTarget`, 포인터 대상 단일 판정 `ResolvePointerTarget(layer, attackActive)`(배치→Placement, 전투→공격 모드면 Attack 아니면 Path, 그 외 None). 입력별 순서: EnterCombat=Gate 대기 제출 성공 시만 Interaction 정리(경로 취소 없음), EndTurn=경로 취소→사실 재수집→Gate 대기 제출→성공 시 정리, Draw=일반 제출, Q=Interaction 정리→`BeginPlanning`, E=경로 취소→최신 사실로 공격 진입, FocusLost/SelectCanceled/Cancel/R은 기존 순서. 사실 공급자 주입, 입력별 대리자 없음 |
| `C/PlacementDragInteraction.cs`(신규) | 배치 Drag·고스트·원본 숨김·배치(중립) 호버·배치 미리보기 번호. 제출은 Gate `TrySubmit` |
| `C/AttackTargetingInteraction.cs`(신규) | 공격 모드·행위자·기록 행위자·범위 응답(조준·AP)·공격 호버·호버 미리보기 번호. `CanEnter`/`Enter`/`Exit`. 제출은 Gate |
| `C/CombatHoverTracker.cs`, `C/CombatCharacterQueries.cs`(신규) | 소유자별 호버 표시, 캐릭터 찾기(직접 순회) |
| `C/CombatInteraction.cs` | 위 둘의 조합. Router가 준 대상에만 위임(자체 판정 없음). 공격 진입은 가능할 때만 배치 정리→공격 활성. 시작·턴 종료·드로우·대기 속성 제거, 생성자에 `ICombatViewBufferReadSink` 추가. `Drag`·`Mode`·`HasRangeAnswer`는 하위 값 읽기만 |
| `C/CombatViewSystem.cs` | `InputRequest`·`Handle`·`IsPathPointerTarget` 삭제. `Receive`는 F1·연결 검사 후 `_router.Route`. Router는 Attach 때 생성·Detach 때 해제, 경로 입력 생성/정리 때 `SetPathInput`. facts의 `IsPathTarget`은 Router 판정식 사용 |

- **미리보기 소유권:** 배치·공격은 Buffer의 최신 요청이 자기 번호일 때만 `ClearPreview`한다. 비활성 소유자의 정리는 다른 소유자의 호버·요청을 건드리지 않는다. 배치 종료가 공격 영향 칸을 지우던 옛 동작은 제거했다(배치는 영향 칸을 그리지 않음). Interaction이 요청하지 않는 Move 응답 분기(옛 이동 모드 잔재)는 지웠다.
- **테스트:** `CombatInteractionTests`의 입력 호출을 Router 경유 도우미 `Input(...)`로 바꾸고 대기 확인은 `_gate`로 바꿨다(Assert 변경 없음, 줄 단위 대조로 확인). 시작·턴 종료·드로우 6건은 신규 `CombatInputRouterTests`(+meta)로 기대값 그대로 옮겼다. 신규: 대상 판정 표 6, 드로우 대기 없음, 턴 종료 거절에도 경로 먼저 취소, 턴 종료 수락 시 경로 취소·대기 기록, 시작은 경로 취소 없음. 재사용을 위해 `CombatInteractionTests`의 Fake·사실 도우미와 `CombatMovementFeedbackTests.Rig/Probe`를 internal로 바꿨다(동작 변경 없음).
- **기존 라우팅 공백(결정 필요):** RecordInputOnly 전투 Layer에서 공격 모드 전 클릭은 경로 입력으로 가므로 기록용 공격 행위자를 고를 수 없다. 경로 입력 병합 이후의 기존 동작이며 이번 변경으로 생긴 것이 아니다. `RecordInputOnly_PressedPlayerUnit_BecomesAttackActor`는 공격 소유자에 대상을 명시해 단위 검증으로 유지했다. → **해소(§7.48):** 사용자 결정으로 기록 모드 전투 Layer 포인터를 공격 소유자로 보낸다.
- **검증:** MSBuild `ProjectL.Systems`·`ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. 관리 코드(Unity 내장 Mono 실행기) `CombatInputRouterTests`·`CombatInteractionTests`·`CombatCommandGateTests`·`CombatInputPolicyTests`·`MovementDraftTests`·`CombatMovementFeedbackTests`·`CombatPathInputCleanupTests` **140/140**. `CombatViewSystemTests`·`CombatMovementViewTests`·`CombatDisplayReuseTests`는 Unity EditMode 대기.
- **Unity에서 확인할 것:** EditMode 전체. Live 재생: 배치 Drag(손패→칸, 필드→손패, 클릭 집기), G 시작, A 드로우, Q 경로·좌클릭 확정·R 복귀, E 공격(칸·방향)·AP 부족 빨강, Space 턴 종료(경로 지정 중이면 먼저 취소), 우클릭 취소, 결과 뒤 공격 모드 범위 재요청.
- 현재 UML §3을 구현에 맞췄다. Core/Buffer·골격·씬/DB 변경 없음. 커밋·push 없음.

### 7.46 VR4 PointerMapper 추출과 해제 방어 통일 (2026-10-06, 사용자 Unity 확인 완료)

사용자가 VR3 Unity 확인 완료를 알리고 VR4 진행을 지시했다. 설계 VR4 수명 표를 따른다(`C` = `ViewSystem/Combat/`).

| 파일 | 변경 |
|---|---|
| `C/CombatPointerMapper.cs`(+meta, 신규) | `ICombatPointerPicker`·`ICombatPointerMapping` 구현. `TryGetRay`·칸/손패 집기·손패 Drop 영역·`TryMapPointer`(UI Raycast 재사용 데이터, V1 동작 그대로)·`TryRayToFieldPlane`·`InnerCellInset`. 수명: 생성(Unbound, Try=false) → `Bind(FieldGridMapper)` → `Unbind`(격자·UI 재사용 참조 해제, 재연결 허용) → `Release`(필드·손패·카메라·EventSystem·PointerEventData·Raycast 결과 해제, 재연결 거부). 카메라는 호출마다 지정 카메라 또는 Main Camera |
| `C/CombatSceneDisplay.cs` | 좌표 변환 코드를 Mapper로 옮기고 위임. Install 시작·실패에 `Unbind`, Field.Build 성공 뒤 `Bind`, Clear에 `Unbind`, Release에 `Release`. `ShowDraft`·`ClearDraft`·`SetReturnState`도 해제 뒤 무시. `BindReturn`은 해제 뒤 새 연결만 무시하고 연결 해제(null)는 그대로 전달. 공개 계약(`ICombatBattleDisplay`)·V1 캐시·V4b 사건 Presenter 연결은 유지 |

- **동작 차이(의도):** 전투가 설치되기 전·정리된 뒤에는 칸·손패 집기와 포인터 매핑이 false다. 이전에는 FieldView의 남은 격자로 집을 수 있었다. 입력 연결은 설치 뒤에만 되므로 정상 흐름의 사용자 동작은 같다.
- **테스트:** `CombatPointerMappingTests`의 반사 도우미가 Mapper 필드를 읽도록 바꿨다(기존 기대값 변경 없음). 신규 3건: Clear 뒤 집기·매핑 false와 UI 캐시 해제 → 재설치 후 같은 칸, 이전 필드가 남은 Build 실패 뒤 집기 false → 정리·재설치 후 복구, Release 뒤 Draft·Return 호출 무시와 `BindReturn(null)` 허용.
- **검증:** MSBuild `ProjectL.Systems`·`ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. 관리 코드 회귀(VR1~VR3 대상) 140/140 유지. VR4 대상인 `CombatPointerMappingTests`·`CombatSceneEventPresenterTests`·`CombatDisplayReuseTests`는 실제 Combat 씬이 필요해 **Unity EditMode 대기**(Combat 씬이 아닌 씬에서 실행).
- **Unity에서 확인할 것:** 위 세 클래스. Live 재생: 배치 Drag(손패→칸, 필드→손패 Drop 영역, 고스트가 필드 밖에서 포인터를 따라감), Q 경로 지정 중 UI 위·필드 밖 이동의 무효 표시, R 복귀 버튼, 전투 종료 후 StageSelect 복귀·재진입 뒤 집기 정상.
- 현재 UML §3 갱신. Core/Buffer·골격·씬/DB 변경 없음. 커밋·push 없음.

### 7.47 VR5 의존성·상수·스타일 정리 (2026-10-06, 사용자 Unity 확인 완료)

사용자가 VR4 Unity 확인 완료를 알리고 VR5 진행을 지시했다(`C` = `ViewSystem/Combat/`).

| 파일 | 변경 |
|---|---|
| `C/CombatSceneDisplay.cs` | 생성자의 `traitVisuals`를 필수 인자로 바꿨다(기본값 제거, null이면 `ArgumentNullException`). 유닛 HUD 특성 표시에 필요한 의존성이 조용히 빠지지 않게 한다 |
| `C/CombatViewSystem.cs` | `InitData`에서 TraitVisual 표가 없으면 `InvalidOperationException`, Attach 사전 검사에 같은 조건 추가. 타임라인 표시 수 매직 넘버를 `MinTimelineEntries = 10`·`TimelineEntriesPerCharacter = 4` 상수로 바꿨다(값 변경 없음). 한 줄 여러 문장 몇 곳을 Allman 형식으로 풀었다 |
| `C/CombatPathInput.cs`·`C/CombatMovementInteraction.cs` | 형식만 정리(Allman, 한 줄 한 문장, 묶은 필드 선언 분리). `CombatMovementInteraction`에 클래스 요약 주석 추가 |
| 테스트 `CombatPointerMappingTests`·`CombatSceneEventPresenterTests` | `CombatSceneDisplay` 생성에 `database.ViewQuery.TraitVisualTableQuery`를 넘긴다 |

- **형식만 바꿨다는 근거:** 정리 전후를 최적화 빌드(`-p:Optimize=true -p:DebugType=none`)로 컴파일해 세 클래스(중첩·람다 포함) 메서드 112개의 IL 바이트 해시를 비교했고 모두 같았다. Debug 빌드는 NOP 차이가 있어 비교에 쓰지 않았다.
- **검증:** MSBuild `ProjectL.Systems`·`ProjectL.Tests.Framework` 오류 0. 관리 코드 회귀 140/140. `CombatPointerMappingTests`·`CombatSceneEventPresenterTests`는 실제 DB·씬이 필요해 **Unity EditMode 대기**.
- **하지 않은 것:** `FieldUnitPresenter`의 선택 인자(`traitVisuals`·`hudCamera`)는 현강님 HUD·Timeline 코드와 겹쳐 그대로 뒀다. 필수화하려면 현강님과 맞춘다.
- **공유:** 천명님 이동 파일 형식 정리 안내 [메모](../handoffs/2026-10-06-combat-movement-input-format-memo.md)(미전달, 사용자가 보낸다).
- **Unity에서 확인할 것:** 위 두 테스트 클래스와 전체 EditMode. Live 재생에서 유닛 HUD 특성 아이콘, 타임라인 칸 수(캐릭터 수×4, 최소 10), Q 경로 지정·R 복귀가 VR4와 같은지.
- Core/Buffer·골격·씬/DB 변경 없음. 커밋·push 없음.

### 7.48 기록 모드 공격 행위자 선택 라우팅 (2026-10-06, 사용자 Unity 확인 완료)

사용자가 VR5 Unity 확인 완료를 알리고 §7.45의 기록 모드 라우팅 공백만 고치도록 지시했다(`C` = `ViewSystem/Combat/`).

- **원인:** `ResolvePointerTarget`이 전투 Layer의 공격 모드 전 포인터를 Path로 보냈다. 기록 모드는 Core가 배치 단계라 행위자·이동 응답이 없어 경로 지정(`BeginPlanning`)이 시작되지 않는다. 그 결과 클릭이 아무 데도 쓰이지 않았고, 공격 소유자의 기록 행위자 선택(`AttackTargetingInteraction.SelectPressed`)에 도달하지 못했다.
- **결정:** RecordInputOnly의 전투 Layer 포인터는 모드와 관계없이 Attack으로 보낸다. Live 판정은 그대로다. 판정은 여전히 `ResolvePointerTarget` 한 곳(SSOT)이고, 실행 모드는 facts의 관측값으로 받는다.

| 파일 | 변경 |
|---|---|
| `C/CombatInputRouter.cs` | `ResolvePointerTarget(layer, attackActive, executionMode)`. 기록 모드 + 전투 Layer → Attack. `RoutePointer`는 `facts.ExecutionMode`를 넘긴다 |
| `C/CombatViewSystem.cs` | `ReadFacts`의 `isPathTarget` 계산에 `ExecutionMode`를 넘긴다(기록 모드 전투 Layer에서 경로 입력 입구가 닫힘) |
| 테스트 `CombatInputRouterTests` | Live 대상 표에 실행 모드 인자 추가, 기록 모드 대상 표 4건 신규 |
| 테스트 `CombatInteractionTests` | `RecordInputOnly_PressedPlayerUnit_BecomesAttackActor`가 공격 소유자를 직접 부르지 않고 Router를 거쳐 누름 → E → 놓기로 Attack 기록까지 검증 |

- **영향:** 기록 모드에서 Q 경로 지정·R 복귀는 원래 시작되지 않았으므로 동작 손실이 없다(W20). 공격 모드가 아닐 때의 공격 소유자 포인터 이동·놓기는 아무것도 하지 않는다.
- **검증:** MSBuild `ProjectL.Tests.Framework`(Systems 포함) 오류 0. 관리 코드 회귀 144/144(140 + 신규 4).
- **Unity에서 확인할 것:** EditMode `CombatInputRouterTests`·`CombatInteractionTests`. RecordInputOnly 재생: 개발 패널로 전투 Layer → 아군 유닛 클릭 → E → 칸 클릭 → Attack 기록. Live 재생: Q 경로 지정·E 공격이 이전과 같은지.
- 구조 개선 설계 VR2 대상 판정 문단과 현재 UML §3에 예외를 적었다. Core/Buffer·골격·씬/DB 변경 없음. 커밋·push 없음.

### 7.49 구조 개선 마감 시점의 남은 View 작업 (2026-10-06, §7.56으로 갱신됨)

VR1~VR5와 §7.48은 사용자 Unity 확인을 마쳤다. 이후 사용자는 손패 카드화(새 작업)로 넘어간다. 이 절은 그 시점의 남은 일 목록이며, 각 항목의 원본 문서를 대신하지 않는다.

| 구분 | 항목 | 막힌 이유·원본 |
|---|---|---|
| VR | VR6 타임라인 소비자 교체 | VR0 메모(`handoffs/2026-10-06-combat-timeline-core-request-memo.md`, 줄 번호는 VR5 기준으로 갱신) 전달·합의·Core 게시 구현 뒤. §7.40 |
| 골격 6B | 포기 이탈 연결 | Core 포기 입력 공개 API. 골격 계획 §6.15 |
| 골격 6B | 패배·최종 승리 목적지 | 목적지 결정 보류 |
| Core | 운영 DB 전투 시작(T1/G7) | Core 조립·인수. DB 경계 인계 §5.6, 골격 계획 §0.2. 두 재화 분리는 2026-10-06 인수(§7.54) |
| 협의 | `FieldUnitPresenter`의 `traitVisuals`·`hudCamera` 필수화 | 현강님 HUD·Timeline 코드와 겹침(§7.47) |
| 협의 | `TempMaterialTable` 삭제 | 민형님 Shader 브랜치 병합 때 |
| 확장 | X3 비이동 VFX, X4 규모 측정, X5 새 명령, X6 포기·재진입 | §8. 착수 조건이 아니라 해당 확장의 완료 증거 |
| 전달 | 문서상 미전달 메모 | 2026-10-06 VR0·이동 형식(천명님), 2026-10-05 Core 요청 6B·공동 Framework 공유, 2026-10-02 Framework 공유·Core 메모·Artifact 표 메모·View 작업자 공유. 실제 발송 여부는 사용자 확인 필요 |

### 7.50 손패 카드화 Task 계획 (2026-10-06, 설계 완료·구현 전)

설계 원본은 [손패 카드화 설계](../specs/2026-10-06-hand-card-design.md)(사용자 결정 U1~U6, 구조 C1~C9). `origin/feature/La/AudioSystem_R` 병합(`97ce9c7a`) 뒤 기준이다. 상태는 이 절에만 적는다.

| Phase | 내용 | 확인 |
|---|---|---|
| H1 | `UnitCardView` 신설(표시·이름 결정·능력치 문구·패널 Hover/고정·패널 사용 여부). `UnitCardUI`는 조회만 하고 View에 위임, 공개 API 유지. `DeckButtonUITests` 표시 필드 설정 변경. **에디터(사용자):** `UnitCard.prefab` 루트 스크립트를 `UnitCardView`로 교체(필드 이름 같음), Variant `UnitCardUI.prefab` 생성·`UnitCardUI` 추가, `GlobalUICanvas`의 `DeckButtonUI.unitCard` 재연결 | MSBuild·관리 회귀. Unity: `DeckButtonUITests`·`GlobalUITests`, 덱 Hover/Click/패널 동작이 이전과 같음 |
| H2 | `HandPresenter` 카드화: `cardRoot`·카드 프리팹(`UnitCardView`)·크기·간격·아래 여백, 단일 풀 + `EntityViewSet`, 가운데 정렬·간격 축소, 패널 숨김, 배치 단계만 표시(C8), 화면 좌표 집기·Drop 영역(C7). `CombatSceneDisplay`가 국면·외형 표를 넘김, `CombatPointerMapper` 손패 두 메서드를 화면 좌표로. 3D 전용 값 제거. 관련 테스트(`CombatPointerMappingTests` 손패 집기·Drop, `CombatDisplayReuseTests`) 갱신 | MSBuild·관리 회귀. Unity EditMode 씬 기반 테스트 |
| H3 | **에디터(사용자):** Combat.unity Canvas 아래 손패 카드 루트, HandPresenter 참조·값 지정, 옛 3D `Hand Content` 정리. 현강님 공유 메모, 현재 UML·인계 갱신. `HandUnitView`·`HandFieldLayout` 삭제 여부 확인 | Live 재생: 배치 단계 카드 표시, 카드 → 칸 Drag 배치, 필드 → 손패 영역 되돌리기, 전투 단계 숨김, 재진입 |

- [x] H1 UnitCard 표시/조회 분리(§7.51). 사용자 확인 완료.
- [x] H2 HandPresenter 카드 손패(§7.52). 사용자 확인 완료.
- [x] H3 씬 연결·공유·문서(§7.53). Unity 확인 대기, 현강님 메모 미전달.

### 7.51 H1 UnitCard 표시/조회 분리 (2026-10-06, 사용자 에디터 작업·Unity 확인 완료)

사용자가 손패 카드화 설계(§7.50)를 확인하고 H1 진행을 지시했다. 현강님 UI 코드에 닿는 변경이며 범위는 설계 §3이다(`G` = `GlobalSystem/UISystem/Screens/Global/`).

| 파일 | 변경 |
|---|---|
| `G/UnitCardView.cs`(+meta, 신규) | 표시 전용 MonoBehaviour. 직렬화 참조 14개(이름은 기존 `UnitCardUI`와 같음)·`UnitCardVisualsSO`. `ShowCharacter(key, fallbackName, portrait, level)`(이름: Visuals SO → fallback → 키), `ShowDetails(CharacterData)`/`ShowDetails(값 7개)`(문구 형식 한 곳), `ClearDetails`, `ShowPanelContent`, `SetLevel`, `Clear`, `SetPanelsEnabled`(손패용), 패널 Hover/고정·`PanelOpening` 이벤트. 같은 값이면 다시 쓰지 않음 |
| `G/UnitCardUI.cs` | 조회만 남기고 표시를 `View`(같은 오브젝트, `RequireComponent`, EditMode 대비 지연 조회)에 위임. 공개 API·`Update` 비교·덱 동작 유지. 패널 열기 전 `Refresh`는 `PanelOpening` 구독으로 유지. `IUIInputReceiver`는 View로 이동 |
| `Prefabs/UI/UnitCard.prefab` | 루트 스크립트 GUID만 `UnitCardUI` → `UnitCardView`로 교체(필드 참조 그대로) |
| `Screens/Stage/UnitCardUI.md` | 구조 표 추가 |
| 테스트 `DeckButtonUITests` | `detailsText`를 `card.View`(UnitCardView)에 설정 |

- **검증:** MSBuild `ProjectL.Tests.Framework`(Systems 포함, 새 파일 포함) 오류 0, 변경 파일 새 경고 0. GameObject 테스트라 관리 실행기로는 돌리지 않았다.
- **에디터 작업(사용자, 이 순서):**
  1. Unity 포커스 → 컴파일 완료 확인. `UnitCard.prefab` 루트 Inspector에 `UnitCardView`가 보이고 참조 14개가 채워져 있는지 확인.
  2. Project 창에서 `UnitCard.prefab` 우클릭 → Create → Prefab Variant → 이름 `UnitCardUI`(같은 폴더).
  3. Variant 루트에 `UnitCardUI` 컴포넌트 추가(필드 없음).
  4. `GlobalUICanvas.prefab`을 열어 `DeckButtonUI`의 **Unit Card** 필드에 `UnitCardUI.prefab`(Variant) 연결. 이 단계 전까지 이 필드는 타입이 맞지 않아 비어 보인다.
- **Unity에서 확인할 것:** EditMode `DeckButtonUITests`·`GlobalUITests`. 재생: 덱 버튼 Hover 미리보기·Click 고정·Synergy/Function 패널 Hover/Click이 이전과 같음.
- Core/Buffer·전투 View·씬 변경 없음. 커밋·push 없음.

### 7.52 H2 HandPresenter 카드 손패 (2026-10-06, 사용자 확인 완료)

사용자가 H1 확인 완료를 알리고 H2 진행을 지시했다(`C` = `ViewSystem/Combat/`, `D` = `C/Display/`).

| 파일 | 변경 |
|---|---|
| `D/HandPresenter.cs` | 3D 손패 → 화면 공간 카드. 직렬화: `cardRoot`(Canvas 아래 RectTransform, 전체가 Drop 영역), `cardPrefab`(`UnitCardView`), `cardScale`(0.35), `cardGap`, `bottomMargin`, `sideMargin`. 단일 풀 + `EntityViewSet`, 슬롯 순서 정렬·가운데 정렬·폭 부족 시 간격 축소(옛 `CardHandPresenter.Layout`), 형제 순서 = 슬롯 순서. 카드 생성 때 앵커 가운데·배율 적용·`SetPanelsEnabled(false)`. `Render(characters, visible)`(false면 영역 숨김, 내용은 갱신). `TryPickUnit(Vector2)`(앞 카드 우선·숨긴 카드 제외)·`ContainsDropPoint(Vector2)`(영역 꺼지면 false). 이름·초상화는 UnitVisual 표(`DisplayName`·`InfoIcon`), 능력치는 `UnitCardView.ShowDetails(CharacterData)`. 3D 위치 계산(`SetField`·`RefreshPosition`·`LateUpdate`)과 키별 풀 제거 |
| `C/CombatSceneDisplay.cs` | `Configure(unitVisuals)`, Install의 `SetField` 제거, Refresh가 `StagePhase == Deployment && Phase != Finished`일 때만 손패 표시(설계 C8) |
| `C/CombatPointerMapper.cs` | 손패 두 메서드는 Ray 없이 화면 좌표로 판정(연결 수명 `IsBound`는 같음) |
| 테스트 `CombatDisplayReuseTests` | 옛 3D 위치 테스트 1건을 카드 테스트 2건으로 교체: 슬롯 순서·가운데 정렬·배율·같은 카드 재사용·영역 숨김 / 화면 좌표 집기(겹침 시 앞 카드)·숨긴 카드 제외·Drop 영역·Release 뒤 false |
| 테스트 `CombatPointerMappingTests` | 손패 취소 영역 검사를 `cardRoot` 화면 중심으로 변경(전투 단계라 영역을 켜고 확인) |

- **검증:** MSBuild `ProjectL.Tests.Framework`(Systems 포함) 오류 0, 변경 파일 새 경고 0. 관리 코드 회귀 144/144. 카드 테스트·씬 기반 테스트는 GameObject가 필요해 **Unity EditMode 대기**.
- **주의:** H3에서 Combat.unity의 HandPresenter에 `cardRoot`·`cardPrefab`을 연결하기 전까지 `CombatSceneRoot.ValidateReferences`가 실패해 Combat 진입과 씬 기반 테스트(`CombatPointerMappingTests`·`CombatSceneEventPresenterTests`·`AuthoredCombatScene_…`)가 실패한다. 이 테스트들은 H3 뒤에 확인한다.
- **남은 결정:** `D/HandUnitView.cs`·`D/HandFieldLayout.cs`(+ `CombatDisplayReuseTests`의 해당 테스트 4건)는 더 이상 실행 코드에서 쓰지 않는다. 삭제 여부는 사용자 결정 대기.
- Core/Buffer·입력(Router·Interaction·Policy)·골격 변경 없음. 커밋·push 없음.

### 7.53 H3 씬 연결·정리·공유 (2026-10-06, 문서·삭제 완료·Unity 확인 대기)

사용자가 H2 확인 완료를 알리고, 3D 손패 전용 파일 삭제와 H3 진행을 지시했다. 씬의 옛 `Hand Content` 오브젝트는 지우지 않아도 된다고 했다.

- **씬(사용자 에디터 작업, 확인):** Combat.unity의 HandPresenter에 `cardRoot`(Canvas 아래 손패 영역)·`cardPrefab`(`UnitCard.prefab` 원본) 연결, `cardScale` 0.35·`cardGap` 12·`bottomMargin` 20·`sideMargin` 20. 옛 `Hand Content`는 남아 있으며 코드에서 참조하지 않는다.
- **삭제:** `Display/HandUnitView.cs`·`Display/HandFieldLayout.cs`(+meta). 에셋 참조 없음(GUID 검색). `CombatDisplayReuseTests`의 `HandUnitView_SameValues_WriteNothing`과 `HandFieldLayout` 테스트 3건·도우미 2개 삭제.
- **문서:** 현재 UML §3에 손패 카드 관계와 설명 추가(HTML판은 갱신하지 않음). 현강님 [공유 메모](../handoffs/2026-10-06-unit-card-split-share.md)(미전달, 사용자가 보낸다).
- **검증:** MSBuild `ProjectL.Tests.Framework`(Systems 포함) 오류 0.
- **Unity에서 확인할 것:** EditMode `CombatDisplayReuseTests`·`CombatPointerMappingTests`·`CombatSceneEventPresenterTests`·`DeckButtonUITests`. Live 재생: 배치 단계 카드 표시, 카드 → 칸 Drag 배치(고스트·배치 판정), 필드 유닛 → 카드 영역 Drop 되돌리기, 전투 단계에서 카드 숨김, StageSelect 복귀 뒤 재진입.
- 커밋·push 없음.

### 7.54 Core 재화 분리 인수와 개발 fixture 골드 (2026-10-06, 코드 완료·Unity 확인 대기)

사용자가 Core 브랜치(`origin/feature/Cheon/refactor/core-system`, `da3a22db`)를 병합(`459a69e3`)했다. Core 원본은 `plans/2026-10-06-stage-gold-run-currency-split-plan.md`.

- **Core 변경(인수):** 스테이지 골드는 `CombatStateData.StageGold`(전투 준비 때 `Economy.InitialGold`로 시작, 드로우 결과로만 변함, `EndBattle`과 함께 끝남). `CurrencyBuffer`는 Run 재화(상점·회복)만 맡는다. 개발 `TemporaryCoreRuntimeFactory`는 Run 재화에 값을 넣지 않는다.
- **View 쪽(Core가 같이 고침, 확인):** `CombatViewSystem.ReadDisplayValues`가 HUD 골드로 `_state.Current.StageGold`를 넘긴다. `BindServices`의 `ICurrencyBufferReadSink` 인자·필드 제거, 테스트 3개 호출 수정. 병합 결과에 VR5 타임라인 상수 유지. View 코드에서 `CurrencyReadSink`를 읽는 곳 없음. 전역 `MainCharactorUI`는 Run 재화를 그대로 표시(정상).
- **이번 변경(사용자 지시):** `ViewSystem/Dev/DevBattleInitializationSource`가 고정 `StageEconomySpec(0, 0)` 대신 요청 스테이지의 DB 경제를 쓴다. 조회 경로는 DB 전투 소스(`BattleDefinitionAdapter`)와 같다: `StageTableQuery.GetStage(stageKey)` → `CombatStageDetail.EconomyKey` → `StageEconomyTableQuery.GetStageEconomy` → `Validate`. 전투 스테이지가 아니거나 경제가 없으면 준비를 멈춘다. 경제를 뺀 전투 내용은 여전히 개발 정의이고, 정의는 전투마다 새로 만든다(이전에는 처음 한 번 캐시).
  - 현재 DB: 전투 스테이지 11개가 모두 `StageEconomy_1`(시작 200, 수동 드로우 40)을 쓴다. 개발 Live에서 드로우 5번 뒤 골드 부족 거절을 볼 수 있다.
- **문서:** CLAUDE.md, §7.49 표, 계획 상단 위험 표(재화 3행), View 설계 §8 관련 3곳, View 다음 세션 인계 표의 낡은 "재화 분리 대기"·"fixture 비용 0" 문장에 해소 표시를 달았다(과거 기록은 지우지 않음).
- **검증:** MSBuild `ProjectL.Tests.Framework`(Systems·Core 포함) 오류 0. Unity가 csproj를 다시 만들기 전이라 병합으로 생긴 새 파일 5개(Core ChapterGraph 4개, `CombatNewRunRosterJob`)는 targets로 더했다. 관리 코드 회귀 144/144.
- **Unity에서 확인할 것:** 개발 Live 재생 — 전투 HUD 골드가 200에서 시작, 드로우마다 40 감소, 5번 뒤 드로우 거절(사유 코드 확인), 상점/전역 UI의 Run 재화는 드로우로 바뀌지 않음, 다음 전투에서 골드가 다시 200. RecordInputOnly 진입도 같은 fixture로 준비 성공.
- **알려진 기존 실패:** Core 전체 EditMode 결과의 `CombatDisplayReuseTests.HudFeedback_ClearAndNewVisibleLifetime_InvalidateCachedValues`는 재화 변경 전부터 실패 목록에 있었다. 원인 미조사.
- Core 파일 변경 없음. 커밋·push 없음.

### 7.55 Core 병합 뒤 새 Run Job 컴파일 오류 해결 (2026-10-06, 코드 완료·Unity 확인 대기)

사용자가 Core 브랜치(`5f2d0381`)를 병합(`35f247a9`)한 뒤 `CombatPlan.cs(46,34): CS0246 'ResetRunProgressJob'` 해결을 요청했다.

- **원인:** Core `5f2d0381`이 `ResetRunProgressJob`을 지우고 `NewRunJob`(`CoreGroup.RunStart.StartNewRun(loadoutKey)` 한 번으로 이전 Run 정리·시작 로스터·시작 Run 재화)으로 바꿨다. 같은 시기 골격 커밋 `b7f41128`의 `CombatPlan` `JobType.NewRun`이 지운 Job(초기화)과 `CombatNewRunRosterJob`(로스터 지급)을 쓰고 있었다. Core 기록의 "다른 호출자 없음"은 병합 전 기준이다.
- **수정(골격 코드, K2: Plan·Job은 View):**
  - `LifeCycles/Combat/Plans/CombatPlan.cs`: NewRun = `NewRunJob(service, NewRunLoadoutKey, 0)` 하나(StagePlan과 같은 Job). 쓰지 않게 된 `IBufferQuery buffers` 인자와 using 제거.
  - `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs`: Combat 계획 등록을 `b7f41128` 이전처럼 `CombatPlan.Register`로 되돌림.
  - 테스트 `CombatPlanTests`: NewRun 테스트를 `NewRunJob` 1개·RunStart 로드아웃 1 호출로 교체, 거절 시 예외·미완료 테스트 추가, `RunStartProbe`·FakeCoreService `RunStart` 주입 추가, NewRun의 buffers null 검사 단언 제거.
- **남은 결정:** `LifeCycles/Combat/Jobs/CombatNewRunRosterJob.cs`와 그 테스트(`NewRunRoster_RejectedGrant_…`)는 이제 실행 코드에서 쓰지 않는다. 삭제 여부는 사용자 결정.
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0. 관리 실행기 `CombatPlanTests` 39/39, 입력 회귀 144/144.
- **Unity에서 확인할 것:** 컴파일, EditMode `CombatPlanTests`·`CombatEntryIntegrationTests`. 재생: 로스터가 없는 첫 진입(새 Run)에서 Combat이 정상 준비되는지.
- Core 파일 변경 없음. 커밋·push 없음.

### 7.56 Core 계약 변경 인수와 남은 골격·View 작업 (2026-10-06, 개발 새 Run 코드 완료·Unity 확인 대기)

사용자가 Core 공유 문서(`2026-10-06-core-contract-changes-and-status-share.md`, Core 작성·사용자 경유)를 주고 병합 사항 확인을 요청했다. 이어서 개발 진행 준비를 `StartNewRun`으로 바꾸고 문서를 갱신하며 남은 골격·View 작업을 정리하라고 지시했다. 이 절이 §7.49를 대체하는 현재 목록이다.

**Core 계약 인수 확인(병합 `35f247a9` 기준):**

| 항목 | 현재 트리 | 영향 |
|---|---|---|
| T1/G7 운영 전투 준비 | `DatabaseBattleInitializationSource`가 예외 없이 `BattleDefinitionAdapter.Read` 결과를 돌려준다 | 운영 DB 전투 가능. 운영 진입 경로는 아래 S2 뒤 |
| 스테이지 골드/Run 재화 | 반영(§7.54) | 끝 |
| 새 Run `RunStart.StartNewRun` | `StagePlan`·`CombatPlan`이 `NewRunJob`(§7.55). 개발 준비도 이번에 같은 경로 | 끝 |
| `StageLifeCycle` 진입 분기 | 로스터 없으면 `NewRun`, 있으면 `Start` | 충돌 없음 |
| `AbandonBattle()` 무인자·결과 enum | View·골격 호출자 없음 | S1에서 연결 |
| `EnterBattle` `RosterNotInitialized` 거절 | `CombatPrepareJob`이 결과를 그대로 전달 | 끝 |
| `PlayerLoadout_1.startingRunCurrency = 0`(임시) | 에셋 반영 | 상점·회복 결제 안 됨(기획 값 대기) |

**이번 변경:** `ViewSystem/Boot/GameBootstrap.PrepareDevProgress`가 `Characters.InitializeRoster` 대신 `RunStart.StartNewRun(devLoadoutKey)` → `EnterChapter(devChapterKey)`를 호출한다(운영 새 Run과 같은 경로, Run 재화까지 설치. `StartNewRun`이 맵 선택을 비우므로 순서 고정). 거절 문구를 "Dev new run was rejected"로 바꿨다. MSBuild `ProjectL.Tests.Framework` 오류 0. Unity에서 확인할 것: 개발 Combat 시작(Live·RecordInputOnly) 정상 진입.

**남은 골격 작업(K2: Flow·Plan·Job은 View, 골격 담당 세션 우선):**

| ID | 작업 | 상태·근거 |
|---|---|---|
| S1 | 자발적 포기를 F2에 연결: 입력 중단·진행 작업 정리 → 남은 결과 표시 → `AbandonBattle()` → 결과별 처리(`Abandoned`: 완료 표시 → `EndBattle` → 전환, `AlreadyCompleted`: 기존 완료 표시, `RunEnded`: 종료 흐름, `NoActiveBattle`: 불필요, 예외: 오류 경계). 일반 `CombatResult`는 새로 게시되지 않음. 검증은 §8 X6 | **코드 완료(2026-10-07, 골격 설계 §6.5 K22·계획 §6.17).** `RequestAbandon()` 공개. Unity 테스트 통과 |
| S2 | StageSelect 노드 클릭 → 진입: 다른 브랜치 `StageMapEntryFlow` 인수·통합(전투 `EnterBattle`, 상점 `Shop.EnterShop`, 회복 `Recovery.EnterRecovery`) | 운영 DB 전투를 실제로 확인하는 경로 |
| S3 | 보스 승리 뒤 챕터 계속 | **골격 호출자는 있음**(2026-10-07 정정): `CombatExitSequence`가 EndBattle 뒤 `CompleteChapter`를 불러 최종/다음 챕터/정의 없음을 나눈다(골격 설계 §6.4 K21). 남은 것은 Core 챕터 연결 정의 원본(현재 `UnavailableChapterContinuationSource` → 정의 없음 → 임시로 Run 종료) |
| S4 | Run 종료(패배·최종 승리) 목적지 | `b7f41128`의 `CombatExitSequence`는 Run 종료 → `EndBattle` → Combat `NewRun` → StageSelect로 연결. Core 공유 문서는 "자동 새 Run 없음, 목적지 보류"로 적고 있어 결정 공유 필요 |
| S5 | `LifeCycles/Combat/Jobs/CombatNewRunRosterJob.cs`·테스트 삭제 | **완료(2026-10-07, 골격 계획 §6.16 정정·후속).** 같은 작업에서 병합으로 빠졌던 K21 테스트 4개를 복원 |
| S6 | 기존 실패 `CombatEntryIntegrationTests` NextEntry 확인 | **완료(2026-10-07).** 원인은 `5376b174`의 StageSelect 아티팩트 보드 설정 필수화. 테스트 DB fixture에 설정(3×3) 추가. Mono 실행기 59/59, Unity 확인 대기 |
| S7 | (선택) 개발 설정에 "진행 준비는 개발, 전투 정의는 DB 소스" 선택지 | **완료(2026-10-07, 골격 세션).** `useDevBattle` → `useDevProgress`(`FormerlySerializedAs`로 씬 값 유지) + `devBattleDefinition`(Fixture/Database). Database면 null 소스 = 운영 DB 정의, 노드 그래프도 DB 맵 설정. 에디터 덮어쓰기(EditorPrefs 키 유지)·설정 창에 "Battle Definition" 추가, Database에서는 필드 크기 칸 비활성. 진입 거절 로그에 DB 노드 안내. MSBuild 에디터·테스트·비개발 정의 오류 0. Unity 재생 확인 대기 |

**남은 View 작업:**

| ID | 작업 | 상태·근거 |
|---|---|---|
| V1 | VR6 타임라인 Buffer 소비 | VR0 메모 미전달 → 합의·Core 게시 구현 뒤(§7.40) |
| V2 | 손패 카드 Hover 정보 카드(설계 U6)·Hover 들림 | 미정, 사용자 판단 |
| V3 | (선택) HUD 골드 이름 `Currency` → `StageGold`(`currencyText`는 `FormerlySerializedAs`) | 사용자 판단 |
| V4 | 기존 실패 테스트: `CombatDisplayReuseTests.HudFeedback_ClearAndNewVisibleLifetime_InvalidateCachedValues` | 원인 미조사 |
| V5 | `FieldUnitPresenter`의 `traitVisuals`·`hudCamera` 필수화 | 현강님 협의(§7.47) |
| V6 | 확장 X3 비이동 VFX·X4 규모 측정·X5 새 명령 | §8, 확장 시점 |

**진행 순서(2026-10-07 사용자 확정, 이 표가 순서의 원본):** 골격은 2026-10-07 마무리: [골격 최종 인계](../handoffs/2026-10-07-combat-skeleton-final-handoff.md).

| 순서 | 작업 | 하는 곳 | 시작 조건 / 막는 것 |
|---|---|---|---|
| 0 | Unity 확인 묶음: §7.53 손패 카드, §7.54 스테이지 골드, §7.55 새 Run Job, 개발 새 Run(Live·RecordInputOnly), 골격 K21(패배·최종 승리 → 새 Run → StageSelect), S5·S6 결과 | 사용자 | 바로 가능 |
| 1 | S5 정리 + 골격 문서 정정 | 골격 세션 | **완료(2026-10-07)** |
| 2 | S6 fixture 수정 | 골격 세션 | **완료(2026-10-07)** |
| 3 | S4 결정 공유([메모](../handoffs/2026-10-07-combat-run-end-destination-memo.md), 2026-10-07 작성) + 미전달 메모(VR0·이동 형식·UnitCard 분리) | 사용자 | 바로 가능 |
| 4 | S1 자발적 포기: 설계 → 구현 → 테스트, 서비스 API 공개 | 골격 세션 | **완료(2026-10-07, 골격 계획 §6.17). Unity 컴파일·EditMode 통과(사용자 확인)** |
| 4-V | 포기 버튼·확인창이 `LifeCycles.CombatExit.RequestAbandon()` 호출. `ExitStatus == Idle`이고 전투 중일 때만 활성, false면 무시 | View 세션 | **착수 가능**(API 확정, 골격 설계 §6.5 K22) |
| 5 | S2 StageSelect 노드 클릭 진입(`StageMapEntryFlow` 인수) | **다른 작업자**(2026-10-07 사용자 결정, 골격 세션 범위 밖) | 해당 작업자 진행. 골격은 기존 진입 API(`CombatEntry.RequestEntry`)를 그대로 제공 |
| 5 이전(선택) | S7 개발 옵션(진행 준비는 개발, 전투 정의는 DB) | 골격 세션(2026-10-07) | **완료.** 재생 확인: 설정 창 Battle Definition = Database로 Combat 시작 |
| View | V4 → V3(선택) → V2(결정) | View 세션 | V1은 Core, V5는 현강님 협의, V6은 확장 시점 |
| 대기 | S3 챕터 계속(Core 연결 정의 원본), Title 화면이 생기면 Run 종료 목적지 교체 | 골격 | 외부 조건 |

**Unity 확인 대기:** §7.53(H3 손패 카드 재생), §7.54(스테이지 골드 200/40), §7.55(새 Run Job), 이번 개발 새 Run. Core 기존 실패 목록의 UI·렌더·오디오·부팅 12건(DeckButtonUI ×3, GlobalUI, UnitHUD, CombatLogUI ×2 등)은 Core 브랜치에 우리 H1~H3(`95768de6`)이 없는 상태의 결과라 우리 트리에서 따로 확인한다.

**공유·전달(문서상 미전달, 실제 발송은 사용자 확인):** VR0 타임라인 요청·이동 형식 메모(천명님), UnitCard 분리 메모(현강님), S4 Run 종료 목적지 결정(천명님, [메모](../handoffs/2026-10-07-combat-run-end-destination-memo.md)), 2026-10-05·10-02 공유 문서들(§7.49).

- Core 파일 변경 없음. 커밋·push 없음.

### 7.57 V4 HUD 수명 테스트 수정과 4-V 포기 UI 설계 (2026-10-07, V4 코드 완료·4-V 설계 확정·구현 §7.58)

사용자가 V4 진행과 4-V 설계를 지시했다.

- **V4 원인:** `CombatDisplayReuseTests.HudFeedback_ClearAndNewVisibleLifetime_InvalidateCachedValues`는 `hud.gameObject.SetActive(false/true)`로 `CombatHud.OnDisable`/`OnEnable`의 캐시 무효화를 기대했다. EditMode에서는 `[ExecuteAlways]`가 없는 MonoBehaviour의 이 콜백이 불리지 않아 캐시가 남고 "disabled marker" 문구가 그대로였다(코드 결함 아님, 테스트 가정 문제).
- **V4 수정:** 같은 테스트에서 `SetActive` 뒤 `OnDisable`/`OnEnable`을 반사로 직접 호출(`InvokeLifecycle` 도우미). 실행 코드 변경 없음. MSBuild 오류 0. **Unity EditMode 확인 대기.**
- **4-V 설계:** [포기 UI 설계](../specs/2026-10-07-combat-abandon-ui-design.md). 버튼·확인창은 별도 `CombatAbandonPanel`, 가능 조건은 `CombatInputPolicy.CanRequestAbandon` 한 곳, 최종 판단·순서는 골격 `RequestAbandon()`. 결정 P1~P5(패널 위치, 골격 좁은 계약 추가, 확인창 동안 입력 차단, 완료 문구, 단축키 범위) 사용자 확인 대기. 구현 전.

### 7.58 4-V 포기 버튼·확인창 구현 (2026-10-07, 사용자 씬 작업·Unity 확인 완료)

사용자가 [포기 UI 설계](../specs/2026-10-07-combat-abandon-ui-design.md)의 결정 P1~P5를 추천대로 확정하고 진행을 지시했다. 한 Phase로 구현했다(`C` = `ViewSystem/Combat/`, `D` = `C/Display/`).

| 파일 | 변경 |
|---|---|
| `LifeCycles/Combat/CombatExitSequence.cs`(골격) | P2: 좁은 계약 `ICombatAbandonRequest { ExitStatus; RequestAbandon(); }` 추가, `ICombatExitService`가 상속(`RequestNormalExit`만 남김). 동작 변경 없음 |
| `D/CombatAbandonPanel.cs`(+meta, 신규) | P1: 포기 버튼·확인창(확인/취소). `Bind(open, confirm, cancel)`(모두 null이면 해제), `SetAvailable(visible, interactable)`, `ShowConfirm`/`HideConfirm`, 같은 값이면 쓰지 않음, `FindMissingReference` |
| `C/CombatSceneRoot.cs` | `abandonPanel` 참조·검증 |
| `C/ICombatBattleDisplay.cs`·`C/CombatSceneDisplay.cs` | `BindAbandon`·`SetAbandonAvailable`·`ShowAbandonConfirm`·`HideAbandonConfirm`. 해제 뒤 새 연결 무시·해제(null)는 전달, Release에서 패널 해제·확인창 닫기 |
| `C/CombatInputPolicy.cs` | `CanRequestAbandon(facts, exitIdle)`: Live·전투 있음·최초 표시 뒤·종료 아님·이탈 Idle(C2). 대기·연출·배치 단계는 막지 않음 |
| `C/CombatViewSystem.cs` | LifeCycle `CombatExit`를 `ICombatAbandonRequest`로 보관(`BindServices` 인자 추가). Attach 때 패널 연결·RecordInputOnly 숨김(C7), Tick마다 `SyncAbandon`(값이 바뀔 때만 쓰기는 패널이 판단, 조건이 깨지면 확인창 닫음 C3·C6), 열기 때 Drag·조준·경로 표시 정리, 확인창 열린 동안 `Receive`가 FocusLost·SelectCanceled 외 입력 무시(P3), 확인 때 가능하면 `RequestAbandon()` 1회·false 무시(C4), StopInput·Detach·SuspendSubmissions에서 확인창 닫기 |
| `D/CombatHud.cs` | P4: `Reason == VoluntaryAbandonment`이면 "포기 · 결과 확인" |
| 테스트 | `CombatInputPolicyTests` 가능 조건 10건(관리 실행 통과), `CombatViewSystemTests` 4건(열기→입력 차단→확인 1회, 취소·입력 복구·거절 무시, 이탈 진행 중 버튼 비활성·확인창 닫힘, RecordInputOnly 숨김) + `AbandonProbe`·FakeDisplay 확장, `CombatDisplayReuseTests` 완료 문구 2건, `BindServices` 호출 3곳에 인자 추가 |

- **검증:** MSBuild `ProjectL.Tests.Framework`(Systems 포함, 새 파일 포함) 오류 0, 변경 파일 새 경고 0. 관리 실행기 192/192(입력 회귀·Policy·CombatPlan). GameObject 테스트는 **Unity EditMode 대기**.
- **씬 작업(사용자, 필수):** `CombatSceneRoot.abandonPanel`이 비어 있으면 `ValidateReferences`로 Combat 진입이 멈춘다. Combat.unity의 Canvas에 ① 포기 버튼 ② 확인창 루트(기본 비활성, 문구 "전투를 포기할까요? 패배로 기록되고 새 Run이 시작됩니다." 등) ③ 확인·취소 버튼을 만들고, 오브젝트 하나에 `CombatAbandonPanel`을 붙여 네 참조를 연결한 뒤 `CombatSceneRoot`의 **Abandon Panel**에 연결한다. 확인창은 다른 HUD 위에 그려지도록 형제 순서를 뒤로 둔다.
- **Unity에서 확인할 것:** EditMode `CombatViewSystemTests`·`CombatDisplayReuseTests`·`CombatEntryIntegrationTests`(골격 포기 4건). Live 재생: 배치 단계·전투 단계·자동 진행 중 포기 → 확인창(열린 동안 Q·E·턴 종료 무시) → 확인 → 남은 처리 표시 → "포기 · 결과 확인" → StageSelect·새 로스터. 취소하면 계속. RecordInputOnly는 버튼 없음.
- 골격 파일은 인터페이스 분리만 했다(골격 담당 확인 필요). Core 파일 변경 없음. 커밋·push 없음.

### 7.59 V3 HUD 골드 이름 정리 (2026-10-07, 사용자 Unity 확인 완료, 중복 정리는 §7.60)

사용자가 4-V 확인 완료를 알리고 V3 진행을 지시했다. 동작 변경 없는 이름 정리다(Core 재화 분리 뒤 "재화"가 Run 재화로 읽히는 혼동 제거).

| 파일 | 변경 |
|---|---|
| `ViewSystem/Combat/CombatDisplayValues.cs` | 생성자 인자 `currency` → `stageGold`, 속성 `Currency` → `StageGold`(요약 주석: 드로우 전용, Run 재화 아님) |
| `ViewSystem/Combat/CombatSceneDisplay.cs` | `_hud.Render(values.StageGold, …)` |
| `ViewSystem/Combat/Display/CombatHud.cs` | 직렬화 필드 `currencyText` → `stageGoldText`(`[FormerlySerializedAs("currencyText")]`, Tooltip), `Render(int stageGold, …)`, 캐시 `_shownStageGold`, 클래스 요약 문구 |
| 테스트 | `CombatViewSystemTests` FakeDisplay `LastStageGold`, `CombatDisplayReuseTests` HUD 필드 이름 |

- 씬 YAML(`Combat.unity`의 `currencyText:` 키)은 고치지 않았다. `FormerlySerializedAs`로 읽히고, 사용자가 씬을 저장하면 새 이름으로 바뀐다.
- **남긴 것(제안):** `CombatDisplayValues.StageGold`는 `State.StageGold`와 같은 값을 따로 받는다(P7·SSOT상 중복). `StageGold => State.StageGold` 계산 속성으로 바꾸고 생성자 인자를 없애면 원본이 하나가 된다. 생성자 서명이 바뀌어 테스트 호출부도 바뀌므로 사용자 결정 대기.
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. Unity: 컴파일 뒤 Combat.unity의 `CombatHud` Inspector에 Stage Gold Text 참조가 유지되는지, 재생에서 골드 표시(200→드로우마다 −40).
- 커밋·push 없음.

### 7.60 표시 값의 스테이지 골드 중복 정리 (2026-10-07, 코드 완료·Unity 확인 대기)

사용자가 V3 확인 완료를 알리고 §7.59에서 제안한 중복 정리를 지시했다.

- `ViewSystem/Combat/CombatDisplayValues.cs`: 생성자의 `stageGold` 인자를 없애고 `StageGold => State.StageGold` 계산 속성으로 바꿨다. 같은 값을 두 번 받지 않는다(P7·SSOT, `RemainingDraws`와 같은 방식).
- `ViewSystem/Combat/CombatViewSystem.ReadDisplayValues`: 골드 인자 제거, 주석 갱신.
- 테스트: `CombatPointerMappingTests`·`CombatSceneEventPresenterTests`의 생성 4곳에서 `0` 인자 제거. `CombatViewSystemTests`의 HUD 골드 검증(`LastStageGold == Rig.StageGold`)은 그대로 통과 조건이다.
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0. Unity EditMode·재생(골드 200→−40) 확인 대기.
- 커밋·push 없음.

### 7.61 누락된 적 UnitDataSO 복원 (2026-10-07)

- 사용자 요청으로 `ae07af3`의 Enemy/Unit/Ballista·Gravekeeper를 현재 `InGameGroup/Unit/Item/Unit_Ballista.asset`, `Unit_Gravekeeper.asset`으로 복원하고 UnitDataTable에 등록했다. 이미 있는 Unit_Demon과 사용자가 수정 중인 Akimbo·ShotGun·적 프리팹은 보존했다.
- 원본 에셋 GUID `ace2f670939ac704180dd35f1fa0e40b`·`e3fff6ce2cc044c7a30d09eb7b151c36`을 유지한다. UnitDataSO 스크립트 GUID, 직렬화 필드명, SerializeReference 특성 타입의 네임스페이스/어셈블리는 현재 형식으로 변환했다. 각 유닛의 수치·특성 20개 항목이 원본과 일치함을 비교했다(Ballista 관통 피해 감소 30%, Gravekeeper 밀치기 1칸 포함).
- 공격 방식 변환은 사용자에게 설명하고 승인받았다. Ballista는 신규 TargetingMethod_3(EightDirectionLine/Direction/None), Gravekeeper는 기존 TargetingMethod_2(Fan/Direction/None)를 참조한다. 신규 지정 방식도 TargetingMethodDataTable에 등록했다. 옛 회전 기반 공격과 완전히 동일한 규칙 복원으로 주장하지 않는다.
- 검증: Systems MSBuild 성공. 실제 YAML 수치를 읽어 빌드 DLL의 UnitData.Validate·특성 검증·TargetingMethodData.Validate를 호출하는 임시 Mono 검사에서 두 유닛 통과, 지정 방식 GUID 참조 일치 확인. Unity DataBaseAssetTests batch 실행은 같은 프로젝트를 연 에디터 때문에 중단되어 Unity 직렬화/에셋 통합 검증은 대기다. 관리 코드 검사는 이를 대체하지 않는다.
- 기존 외형 SO는 두 EntityKey를 이미 제공한다. 이 작업은 규칙 SO 복원이며 개발 fixture의 적 출현 목록이나 스테이지 배치는 바꾸지 않았다. Core 코드·기존 사용자 변경 수정·커밋·push 없음.

### 7.62 필드 격자 아웃라인 표시 모드 (2026-10-09, O1~O3 코드 완료·Unity 확인 대기)

설계 원본은 [필드 격자 아웃라인 모드 설계](../specs/2026-10-09-field-grid-outline-mode-design.md)(U1~U7, F1~F11)다. 사용자가 F7·F8을 추천안대로 확정했다. O1은 워크시트를 전달했으나 사용자가 직접 구현을 요청해 Claude가 반영했다. Phase마다 멈춘다(`D` = `ViewSystem/Combat/Display/`).

- **배경:** 2026-10-08 `4f245ea9`에서 셀 프리팹 모드에 상시 격자 Decal(`D/FieldGridDecal.cs`)을 붙였다. 2026-10-09 Shader 브랜치 병합의 GUID 충돌 정리로 우리 쪽 셰이더 복사본(`Assets/ProjectL/GridDecal.shadergraph`·머티리얼)을 지우고 민형님 셰이더·머티리얼을 쓰며, `_GridOrigin`·`_MaskCenter` 설정 코드를 뺐다(코드 반영, 커밋 대기).

| Phase | 범위 | 끝난 뒤 확인 |
|---|---|---|
| **O1 데이터** | `FieldDisplayMode` enum(신규), `StageFieldViewData`에 모드·`outlineCellSpacing`·`gridLineWidth`·`outlineThickness`·`outlineYOffset`, 모드별 `Validate`(F10). 테스트 헬퍼 `CreateMaterial`, `ViewDataTableTests` 2건 | 컴파일, `ViewDataTableTests`. 기존 에셋은 `CellPrefab` 그대로라 재생 동작이 바뀌지 않는다 |
| **O2 상태 분리** | `D/FieldHighlightState`(층 비트·정적 구역 종류·호버·배치 드래그, F2·F3), `D/IFieldHighlightRenderer`(F4), `D/FieldCellColorRenderer`(셀 풀·칠하기 이전), `FieldView` 위임. 셀 프리팹 모드 Decal 제거(F8)는 선반영됨. 상태 테스트 | 동작 보존: 셀 프리팹 모드 재생에서 구역·레일·호버·범위·영향·경로·차단 벽 색이 전과 같다(격자선만 사라짐). `CombatDisplayReuseTests` |
| **O3 아웃라인 모드** | `D/FieldOutlineRenderer`(F5), `FieldGridDecal` 선 두께 인자, AreaOutline 서비스 전달(F9), `SetPlacementActive`(F6), 층 순서(`FieldHighlightLayers`), 회전 경고(F11), 행동 중 판정 테스트 | 에셋 모드를 `GridOutline`으로 바꿔 재생: 평소 현재 행위자 테두리만, 이동·공격 조준·배치 드래그 때 격자와 층 테두리. 끝나면 모드를 되돌릴지 사용자 결정 |

- **O1 실행 기록:** `DataBaseSystem/Data/View/Table/StageField/Item/FieldDisplayMode.cs`(+meta, 신규), `StageFieldViewDataSO.cs`(모드·`outlineCellSpacing`·`gridLineWidth`·`outlineThickness`·`outlineYOffset`, 프로퍼티 `DisplayMode` 등, `Validate` → `ValidateCellPrefab`/`ValidateGridOutline`, `gridDecalDepth` 검사는 GridOutline 쪽으로 이동), 테스트 `DataBaseTestAssets.CreateMaterial`, `ViewDataTableTests` 2건(GridOutline은 셀 프리팹 불필요, 머티리얼·간격·선 두께·아웃라인 두께 누락 거부). `gridDecalMaterial`·`gridDecalDepth` 직렬화 이름은 그대로라 에셋 연결이 유지되고, 에셋에 없는 모드 값은 기본 `CellPrefab`으로 읽힌다.
  - **검증:** MSBuild `ProjectL.Tests.Framework`(Systems 포함, 산출물 스크래치패드) 오류 0, 변경 파일 새 경고 0. **Unity 대기:** EditMode `ViewDataTableTests`·`DataBaseAssetTests`, `StageFieldView_Default` Inspector에서 Display Mode = CellPrefab, Combat 재생 화면 변화 없음.
- **F8 선반영(사용자 요청, O2에서 하려던 것):** `D/FieldView.Build`가 `DisplayMode == GridOutline`일 때만 `FieldGridDecal`을 만들고 보인다. CellPrefab 모드는 데칼 오브젝트를 만들지 않는다. MSBuild 오류 0. Unity 확인: Combat 재생에서 격자선이 사라지고 셀·색은 그대로, Hierarchy에 `FieldGridDecal`이 없음.
- **O2 실행 기록(사용자 지시로 Claude 구현):** 동작 보존 리팩터.
  - 신규 `D/FieldHighlightState.cs`(+`FieldStaticZone` enum): 칸별 층 비트·정적 구역 종류·호버·배치 드래그의 유일한 원본. 변경 메서드는 다시 그릴 칸 index만 돌려준다(같은 층 다시 설정 시 유지 칸·격자 밖·중복 제외, 정적 구역은 종류가 바뀐 칸만).
  - 신규 `D/FieldHighlightPalette.cs`: `FieldView` 직렬화 색 묶음과 색 종류 → 색 선택(두 표시기 공용, SSOT).
  - 신규 `D/IFieldHighlightRenderer.cs`: `CanReuse`·`MeasureSpacing`·`Build(mapper, state)`·`Repaint(changed)`·`Release`·`Dispose`.
  - 신규 `D/FieldCellColorRenderer.cs`: 셀 풀·체스판 배치·칸 간격 측정·우선순위 칠하기를 `FieldView`에서 옮김. 테스트용 `CellAt`.
  - `D/FieldView.cs`: 상태와 표시기만 갖고 층 API는 상태 변경 → `Repaint`. 공개 API·직렬화 색은 그대로. `HighlightState` 읽기 프로퍼티 추가. GridOutline 모드는 O3 전까지 진입 때 `InvalidOperationException`(이전에도 셀 프리팹 없이 실패했다). `FieldGridDecal`은 O3에서 아웃라인 표시기가 쓴다(지금 호출자 없음).
  - 테스트: 신규 `FieldHighlightStateTests` 4건. `CombatPointerMappingTests`·`WallAttackInteractionTests`의 리플렉션(`_layers`·`_cells`·`_hoverIndex`·`_hoverState`)을 `HighlightState`·`FieldCellColorRenderer.CellAt`으로 바꿈.
  - **검증:** MSBuild `ProjectL.Tests.Framework`(새 파일은 `CustomAfterMicrosoftCommonTargets`로 추가, 산출물 스크래치패드) 오류 0, 변경 파일 새 경고 0. `FieldHighlightStateTests` 4/4를 Unity Mono로 직접 실행해 통과. **Unity 대기:** EditMode `CombatPointerMappingTests`·`WallAttackInteractionTests`·`CombatDisplayReuseTests`·`FieldHighlightStateTests`, Combat 재생에서 구역·레일·호버·범위·영향·경로·차단 벽 색이 전과 같음.
- **O3 실행 기록(사용자 지시로 Claude 구현):**
  - 신규 `D/FieldOutlineRenderer.cs`: 슬롯 7개(아군 배치 구역 → `DrawOrder` 층 5개 → 호버, 낮은 것부터)마다 AreaOutline 핸들 하나를 Build 때 받아 전투 동안 유지. 평소에는 현재 행위자만, `IsActing`이면 격자 데칼·층·호버, 배치 드래그 중이면 아군 배치 구역(레일 칸 제외). 칸 목록·색이 같으면 `Update`하지 않음, 층마다 높이 +0.002, `isDashed: false`. 필드 루트가 회전돼 있으면 경고 한 번 뒤 그리지 않음(F11). Release에서 핸들 Dispose, Dispose에서 데칼 파괴.
  - `D/FieldHighlightState`: `IsActing`(범위·영향·경로·차단 벽 층 또는 배치 드래그, U4)·`HasAnyCell`. `D/IFieldHighlightRenderer`: `Refresh()`(배치 드래그처럼 칸과 무관한 변화). `D/FieldCellColorRenderer.Refresh`는 할 일 없음.
  - `D/FieldHighlightLayers`: 층 우선순위를 `DrawOrder` 목록 하나로 두고 `Resolve`가 그 목록을 따른다(셀 색과 테두리 높이의 SSOT). `HoverColor`·`ColorOf` 추가. 동작 같음.
  - `D/FieldCellSpacing.FromCenterSpacing`, `D/FieldGridDecal.Show(mapper, spacing, lineWidth, material, depth)`(선 두께는 DB `gridLineWidth`, 넓은 칸 간격 기준 비율).
  - `D/FieldView`: `Build`에 `IAreaOutlineSystemService` 인자, GridOutline이면 `FieldOutlineRenderer`, `SetPlacementActive(bool)`.
  - `C/CombatSceneDisplay`: 생성자에 `IAreaOutlineSystemService`(필수), `ShowGhost`/`HideGhost`에서 `SetPlacementActive`(F6). `C/CombatViewSystem`: `BindServices`에 `IAreaOutlineSystemService`(`global.RenderServiceCommand.ViewGroup.AreaOutlineSystemServices`, F9), Attach 때 전달.
  - 테스트: `CombatViewSystemTests.FakeAreaOutlines`·`FakeAreaOutlineHandle`(기록용 대역), 신규 `FieldOutlineRendererTests` 5건(시작 상태·평소 행위자만·행동 중 격자·층·호버와 높이 순서·배치 드래그 구역·같은 칸 재사용과 해제), `FieldHighlightStateTests`에 `IsActing` 1건. `BindServices` 호출 3곳·`CombatSceneDisplay` 생성 3곳에 인자 추가.
  - **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. `FieldHighlightStateTests` 5/5 Unity Mono 직접 실행 통과. **Unity 대기:** EditMode `FieldOutlineRendererTests`와 O2 대상 테스트·`CombatViewSystemTests`·`CombatMovementViewTests`, 재생은 아래.
  - **Unity 재생 확인 순서:** `StageFieldView_Default`의 Display Mode를 `GridOutline`으로 바꾸고 Outline Cell Spacing을 지금 셀 간격에 맞게 넣는다(유닛 위치·클릭 판정이 이 값을 따른다). 평소 현재 행위자 테두리만 → 손패 드래그 때 격자·아군 구역·호버 → 이동 경로 드래그·공격 조준 때 격자·범위·영향·경로·차단 벽·호버. 확인 뒤 기본 모드를 어느 쪽으로 둘지는 사용자 결정. 에셋 값은 Claude가 바꾸지 않았다.
- **격자 원점·마스크 중심 복원(2026-10-09, 사용자 지시, U6 변경):** 민형님 `Render/Shader/Decal/GridDecal.shadergraph`(GUID `c64749d8…` 유지)에 `4f245ea9`의 추가분만 다시 넣었다: `_GridOrigin`(위치 XZ − 원점 뒤 칸 크기로 나눔), `_MaskCenter`(고정 Vector2 노드 대신 마스크 거리 기준). 그 밖의 노드·`GridEdgeSize` 설정은 민형님 것 그대로(노드 비교로 확인). `D/FieldGridDecal.Show`가 필드 모서리 월드 XZ를 `_GridOrigin`, 필드 중심을 `_MaskCenter`로 머티리얼 인스턴스에 넣는다. 민형님 머티리얼의 `_RadiusMaskSize`는 작업 사본에서 이미 10이라 바꾸지 않았다. MSBuild `ProjectL.Systems` 오류 0. **Unity 확인:** 셰이더 재임포트 오류 없음, GridOutline 재생에서 격자선이 AreaOutline 테두리·칸 경계와 맞음. 민형님 공유 메모 [2026-10-09-grid-decal-shader-share-minhyeong](../handoffs/2026-10-09-grid-decal-shader-share-minhyeong.md)(GUID 정리·속성 추가·사용 방식·점선/흐름 미적용·캐릭터 덮임 질문, **미전달**).
- **Q 경로 지정 중 격자 켜기(2026-10-09, 사용자 요청, U4 확장):** Q를 누른 순간(시작 칸 대기 포함)부터 확정·취소·이탈까지 격자 아웃라인 모드의 "행동 중"으로 본다.
  - `ICombatMovementFeedback.SetPathPlanning(bool)`(값이 바뀔 때만). `CombatPathInput.SyncPlanning`이 `_attached && 경로 행위자 있음`을 BeginPlanning·CancelInputs·ShowDraft(제출·종료로 행위자 해제)·Detach에서 알린다.
  - `CombatSceneDisplay` → `FieldView.SetPathPlanning` → `FieldHighlightState.IsPathPlanning`, `IsActing`에 포함. 아군 배치 구역은 배치 드래그 때만 그대로. 셀 색 모드는 화면 변화 없음.
  - 테스트: `CombatMovementFeedbackTests` 1건(Q부터 켜짐·취소·제출·이탈 때 꺼짐·같은 값 재알림 없음), `FieldHighlightStateTests` 보강, `FieldOutlineRendererTests` 1건(경로 지정 중 격자·호버, 배치 구역 없음). 테스트 대역 `FakeDisplay`·`Probe`에 메서드 추가.
  - **검증:** MSBuild 오류 0. Unity Mono 직접 실행 `CombatMovementFeedbackTests` 15/15·`FieldHighlightStateTests` 5/5·`MovementDraftTests` 25/25·`CombatPathInputCleanupTests` 1/1 통과. **Unity 대기:** `FieldOutlineRendererTests`, GridOutline 재생에서 Q만 눌러도 격자가 켜지고 우클릭·확정 뒤 꺼짐.
- **알려진 차이(설계 §5):** Decal의 캐릭터 덮임(U5), AreaOutline 점선·흐름 값 미적용(민형님 영역).
- 커밋·push 없음.

### 7.63 이동 경로 시작 칸 확장 (2026-10-09, 코드 완료·Unity 확인 대기)

사용자 요청: Q 뒤 유닛 칸에 먼저 커서를 넣어야 경로가 시작되던 것을 주변 칸에서도 시작하게 한다. 대각선은 Core 경로 규칙(한 걸음 = 상하좌우 1칸, `MovementRouteValidator`)상 중간 칸이 필요해 사용자가 **상하좌우 4칸만**으로 정했다.

- `ViewSystem/StageInput/MovementDraft.cs`: `IsStep(from, to)`(상하좌우 1칸, 기존 `Accept`·`CanResumeAt`의 같은 식을 한 곳으로)·`CanStartAt(origin, cell)`(행위자 칸 또는 상하좌우 이웃).
- `ViewSystem/Combat/CombatMovementInteraction.Begin`: 시작 칸이 이웃이면 경로 `[행위자 칸, 이웃]`으로 시작하고 곧바로 Core 견적을 요청한다. 칸 경계 떨림으로 시작되지 않게 이웃은 안쪽 영역(`InnerCellInset`)에 들어왔을 때만 받는다. 확정 경로(청록)는 Core 승인 전까지 행위자 칸만이다(기존 규칙 그대로).
- `ViewSystem/Combat/CombatPathInput.PointerMoved`: 시작 대기 판정을 `CanStartAt`으로.
- 테스트: `MovementDraftTests` 2건(`CanStartAt` 규칙, 이웃 시작·경계 거절·승인), `CombatMovementFeedbackTests` 1건(대각 무시 → 이웃 시작 → 승인 → 확정 제출). 기존 `Q_WaitsForOrigin_…`은 옛 규칙(이웃에서 시작 안 함)을 검사하던 것이라 두 칸 떨어진 칸으로 바꾸고 이름을 `Q_WaitsForStartCell_…`로 고쳤다.
- 문서: `viewDocs/stage-input-guide.md` §2.2 2단계.
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. Unity Mono로 `MovementDraftTests` 25/25·`CombatMovementFeedbackTests` 14/14 직접 실행 통과(매개변수 없는 `[Test]`만, `[TestCase]`는 Unity에서). **Unity 대기:** EditMode 위 두 클래스 전체·`CombatMovementViewTests`, 재생에서 Q → 유닛 위·아래·좌·우 칸에 커서 → 경로 시작, 대각선 칸은 반응 없음.
- 커밋·push 없음.

### 7.64 지나온 경로 칸으로 되돌리기·이동 HUD 문구 (2026-10-09, 사용자 재생 확인 완료: 경계선 위 빨간 표시 해결)

사용자 제보: 경로 지정 뒤 이미 지난 칸에 커서를 올리면 우측 상단 "이동 취소 영역"과 함께 빨간 칸이 보였다. 원인은 직전 칸만 되돌리는 규칙(대각선으로 가로질러 더 앞의 칸에 닿으면 자기 경로와 겹침으로 거절·정지)과, HUD가 포인터 무효면 이유와 상관없이 "이동 취소 영역"을 띄우던 것. 사용자가 **그 칸까지 경로 되돌리기**로 정했다.

- `ViewSystem/StageInput/MovementDraft.cs`: `Accept`가 자기 경로의 어느 칸이든 그 칸까지 자른다(`Removed`). `CanResumeAt`은 경로의 어느 칸 또는(허용 시) 끝점에 붙은 칸. 경로 밖 칸·빈 구간은 여전히 잇지 않는다.
- `ViewSystem/Combat/Display/CombatHud.ShowMovement`: Core 거절 이유("이동 불가: …")를 먼저, 포인터 무효는 "끝점에서 이어 그리세요". 예전에는 Core 거절도 "이동 취소 영역"에 가려졌다.
- 테스트: `MovementDraftTests` — `Backtrack_PopsOnlyImmediatePredecessor` → `Backtrack_TrimsToAnyEarlierPathCell`, `FastSampleWithLocalRejection_…`(옛 규칙: 원점을 만나 거부) → `FastSampleBackToOrigin_TrimsToOrigin_AndDropsOwnedPreview`, 신규 `SuspendedDraft_ResumesAtAnyPathCell_ButNotAtGap`·`PointerCuttingBackToEarlierPathCell_TrimsInsteadOfSuspending`. `CombatDisplayReuseTests.HudMovement_…` 문구와 거절 우선 1줄.
- **정정(같은 날, 사용자 재현):** 되돌리기·문구 수정 뒤에도 같은 현상이 남았다. 사용자가 "경로 수와 상관없이 커서가 칸 경계선에 걸치면 생긴다"고 짚었다. 실제 주원인은 칸 경계 히스테리시스였다. 안쪽 영역(`InnerCellInset`) 밖이라 아직 받지 않은 칸이 있으면 `CombatMovementInteraction.PointerMoved`가 `Draft.MarkPointerInvalid()`로 포인터 무효를 표시해 빨간 칸·문구가 떴다. 이제 미정 칸은 무효로 보지 않고 받은 끝점을 그대로 가리킨다(`CandidateCell = Draft.Endpoint`). 확정은 `_unaccepted.Count > 0` 검사로 계속 막힌다. 테스트 `PointerOnCellBoundary_IsPendingNotInvalid_AndCannotConfirm` 추가. `MovementDraft.MarkPointerInvalid`는 이제 테스트에서만 쓴다. 지나온 칸 되돌리기는 사용자 결정대로 유지한다.
- 문서: `viewDocs/stage-input-guide.md` §2.2.
- **검증:** MSBuild 오류 0. Unity Mono 직접 실행(이번부터 `[TestCase]` 포함) `MovementDraftTests` 39/39(정정 포함)·`CombatMovementFeedbackTests` 23/23·`CombatPathInputCleanupTests` 1/1·`FieldHighlightStateTests` 5/5. **Unity 대기:** `CombatDisplayReuseTests`(HUD, GameObject 필요)·`CombatMovementViewTests`, 재생에서 경로를 그린 뒤 앞 칸으로 커서를 되돌리면 그 칸까지 잘리고, 칸 경계선 위에 커서를 둬도 빨간 표시·문구가 나오지 않음.
- 커밋·push 없음.

### 7.65 AreaOutline 점선을 점선 길이 기준으로 (2026-10-09, 사용자 지시, 코드 완료·Unity 확인 대기)

사용자 확인: 테두리 점선 개수가 칸 수와 상관없이 고정이었다. 원인은 메시 U가 루프마다 0 → 1이고 셰이더가 그 구간에 `_DashCount`(공유 머티리얼 34.1)개를 그리며, `AreaOutlineHandle.Update`가 점선·흐름 값을 넣지 않았기 때문이다. 사용자가 "1번 방식"(핸들 값 반영 + 둘레 기준 개수)으로 **Render(민형님 영역) 코드를 직접 고치라고 지시**했다.

- Render: `AreaOutlineData.dashLength`(선택, 기본 0), `GridOutlineCreateData.DashLength`, `MeshHightLevelBuilder.AddOutlineLoop` U = `거리 ÷ 둘레 × round(둘레 ÷ DashLength)`(최소 1), `AreaOutlineHandle.Update`가 점선 켜기·개수(DashLength 있으면 1)·채움·흐름 방향·속도를 핸들마다 넣는다. 셰이더 변경 없음. 기존 호출 호환(DashLength 0이면 이전 동작). 공유 머티리얼 값은 이제 핸들 값에 덮인다.
- View: `StageFieldViewData.outlineDashLength`(기본 0.25)·`outlineDashFill`(기본 0.65)와 GridOutline 검사, `D/FieldOutlineRenderer`가 `isDashed: true`와 두 값을 넘긴다.
- 테스트: 신규 `AreaOutlineMeshTests` 3건(둘레 비례·정수 반올림·최소 1·DashLength 0이면 0 → 1), `FieldOutlineRendererTests` 점선 값 단언, `ViewDataTableTests` 거부 2건.
- 민형님 공유 메모 §6에 추가(미전달).
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. `AreaOutlineMeshTests` 3/3 Unity Mono 직접 실행 통과. **Unity 대기:** `FieldOutlineRendererTests`·`ViewDataTableTests`, GridOutline 재생에서 칸 하나 테두리와 범위 테두리의 점선 길이가 같은지.
- 커밋·push 없음.

### 7.66 거절 뒤 이동 유닛 주변에서 다시 잇기 (2026-10-09, 사용자 재생 확인 완료)

사용자 제보: 다른 유닛이 있는 칸까지 경로를 그리면 `DestinationOccupied`로 거절된 뒤, 이동 유닛 주변 4칸에 커서를 둬도 계속 막혔다. 유닛 칸으로 가면 정상이었다. 원인: Core는 막힌 칸 앞까지만 유효(`validStepCount`)로 답하고 View는 그 접두 경로를 남긴 채 멈춘다(`RetainPrefix` → `Suspend`). 멈춘 경로는 경로 칸 또는 **끝점**에 붙은 칸에서만 다시 이어졌다. 그래서 끝점과 떨어진 유닛 주변 칸은 이어지지 않았고, HUD의 거절 이유가 그대로 남았다.

- `ViewSystem/StageInput/MovementDraft.cs`: `CanResumeAt(cell, allowAdjacent)`가 붙은 칸 허용 시 **경로의 어느 칸에 붙은 칸이든** 받는다(`LatestNeighborIndex`, 여럿이면 경로를 가장 많이 남기는 뒤쪽 칸). `ResumeAt`은 그 경로 칸까지 자른 뒤 한 걸음 더한다. 경로 칸 위 재개·UI 이탈 뒤 경로 칸만 허용·거절 칸 재요청 금지·빈 구간 미연결은 그대로.
- 테스트: `MovementDraftTests` 2건(`SuspendedDraft_ResumesNextToAnyPathCell_ByTrimmingToIt`, `OccupiedDestination_ThenCursorBesideMovingUnit_RestartsFromUnit` — Core `DestinationOccupied` 응답을 그대로 넣어 재현).
- 문서: `viewDocs/stage-input-guide.md` §2.2 거절 단락.
- **검증:** MSBuild 오류 0. Unity Mono 직접 실행 `MovementDraftTests` 41/41·`CombatMovementFeedbackTests` 23/23·`CombatPathInputCleanupTests` 1/1. **Unity 대기:** 재생에서 다른 유닛 칸까지 그려 거절된 뒤 이동 유닛 주변 칸에 커서 → 그 칸으로 경로가 다시 시작.
- 커밋·push 없음.

### 7.67 적 유닛 모델 180도 표시 보정 (2026-10-09, 코드 완료·Unity 확인 대기)

사용자 요청: 적 유닛은 생성 때 180도 돌아 있어야 한다. Core는 적을 `R0`(+Y)으로 만들고(`EnemySpawnCalculator`) 전투 중 바꾸지 않는다. 그래서 적 모델이 아군 반대쪽을 봤다. Core 수정·프리팹 회전·View 보정 중 사용자가 **View 로직**을 골랐다(한 곳에서 관리, 공유 프리팹 영향 없음, 새 적 유닛도 자동).

- `D/FieldUnitPresenter.cs`: `ModelRotation(owner, rotation, mapper)` — Core 회전의 월드 값에 적이면 Y 180도를 곱한다. 캐릭터 표시 한 곳에서만 쓴다(이동 보간은 위치만). 구조물(본부)은 요청 범위 밖이라 그대로.
- **주의:** Core 값(R0)과 화면 방향이 다르다. Core가 적 방향을 직접 주게 되면(생성 R180, 방향 전환) 이 보정을 지운다. 방향 방어 등 규칙은 Core 값을 따른다.
- 테스트: 신규 `FieldUnitFacingTests` 1건(아군 R0 = +Z, 적 R0 = -Z, 적 R90 = -X). GameObject가 필요해 Unity EditMode에서만 실행된다.
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. **Unity 대기:** `FieldUnitFacingTests`, 재생에서 적 유닛이 아군 쪽을 보는지.
- 커밋·push 없음.

### 7.68 SOLID·SSOT 평가 반영 개선 Task 계획 (2026-10-09~10, P1 완료·P2~P7 코드 완료, Unity 확인 대기)

사용자가 [SOLID·SSOT 평가](../specs/2026-10-09-combat-view-solid-ssot-evaluation.md)대로 코드를 개선하기로 했다. 평가 항목을 모두 진행하고 Phase를 나누며, 버튼 입력 규칙(`Assets/AGENTS.md`)을 전투 버튼에도 적용하고, 구현은 Claude가 한다(2026-10-09 결정). 설계·결정 D1~D8·리뷰 질문 Q1~Q3은 [개선 설계](../specs/2026-10-09-combat-view-solid-improvement-design.md)가 원본이다.

| Phase | 내용 | 상태 |
|---|---|---|
| P1 | 문서: 타임라인 Core 메모 갱신(D6), 평가·UML 문서 오류 수정 | **완료** |
| P2 | 표시기 안전: 강조 상태 읽기 계약(D4), 해제 실패 수집(D5) | **코드 완료·Unity 확인 대기** |
| P3 | `CombatViewSystem`에서 포기 UI·개발 패널 컨트롤러 분리(D2·D3) | **코드 완료·Unity 확인 대기** |
| P4 | Tick·변경 신호 정리: 도달 불가 `BattleCleared` 분기 제거(D9), 포기 버튼 동기화·종료 판정을 매 Tick에서 이벤트 시점으로·재진입 확인 헬퍼(D12), 첫 그리기를 Attach로 옮기고 설치 신호 제거(D13) | **코드 완료·Unity 확인 대기** |
| P5 | 입력 수신 분리: `CombatInputReceiver`(D11, R1), Unbind 중 `SelectCanceled`가 버려지는 문제 수정(R2). `ICombatViewService`의 `FocusLost`·`CaptureLost`·`RequestReturn`은 조정자에 남아 수신 객체로 넘김 | **코드 완료·Unity 확인 대기** |
| P6 | 전투 버튼 7개를 `UIInputBinding`으로 교체, 테스트 EventSystem 전달(D1, 옛 P4) | **코드 완료·Unity 확인 대기** |
| P7 | 가독성 정리(D7, 옛 P5) + `SyncInputToCore` 반복 정리·클래스 주석을 최종 구조로(R7) | **코드 완료·Unity 확인 대기** |
| 철회 | D10 `ICombatViewService` 축소(사용자 결정 Q5: 그대로 둔다) | 철회 |
| 보류 | R6 경로 입력 수명·응답 전달 일관화(매 Tick 폴링 포함, 선택) | 보류 |
| 보류 | 평가 항목 5(사건별 연출 분배·최적화): 착수 조건 미충족, 측정 절차만 설계 D8 | 보류 |
| 보류 | VR6 타임라인 소비자 교체(+ R5 타임라인 UI 제어): Core 답변 뒤 | 보류 |

- 리뷰: Q1~Q3 추천안으로 확정(D1 해석 유지, D3 진행, `CombatSceneEventPresenter` 형식 정리는 제외).
- **2026-10-09 재계획:** P3 뒤 `CombatViewSystem` 집중 검토(R1~R7, [개선 설계 §5](../specs/2026-10-09-combat-view-solid-improvement-design.md#5-2차-검토-combatviewsystem-집중-검토와-재계획-2026-10-09)). 사용자 결정: R3은 지금 동작(전투가 사라지면 Detach) 유지, P4 이후를 다시 계획. 옛 P4(버튼)·P5(가독성)는 P6·P7로 밀렸다. Q4 = 테스트 입구 `View.Input.X()`, Q5 = `ICombatViewService` 그대로(D10 철회). 이어서 "Tick에서 검사가 너무 많다"·"첫 그리기를 왜 Tick에서 하나" 질문으로 D12(이벤트 시점 동기화)·D13(Attach에서 첫 그리기)을 P4에 더했다. D13 근거: Combat 씬 Enter 노드에서 LifeCycle `OnEnter`(전투 설치 확인)가 View Attach보다 먼저 돌아 Attach 시점엔 항상 전투가 있고, 지금의 설치 신호는 Attach 같은 프레임 첫 Tick에 1회만 나온다(K20 이전 흐름의 흔적).
- 모든 Phase는 동작 변경 없는 리팩터링이다. Phase마다 멈추고 보고한다.
- Core 파일 변경 없음. 골격 파일은 P7의 주석 한 줄(`CombatLifeCycle.cs`)만.

**P1 실행 기록(2026-10-09):**

- [타임라인 요청 메모](../handoffs/2026-10-06-combat-timeline-core-request-memo.md): 줄 번호를 `2b25f64a` 기준으로 고치고 §5 추가 — Buffer의 `CommandState.Timeline`(`TimelineUI`가 읽음, Core가 명령 처리 때 이어받고 죽은 유닛을 거름)과 HUD의 `GetTimeline()` 예측이 같은 의미인지, 길이·갱신 시점·빈 값, `GetTimeline` 제거 가능 여부를 묻는다. 미전달.
- [Core 경계 인계](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §13.9에 갱신 사실 한 줄.
- [상세 UML](../specs/2026-10-09-combat-skeleton-view-detailed-uml.md): 머리에 "격자 아웃라인 이전 기준" 주의, §3.1의 `CombatLogUI` 설명 정정(기록 수가 같으면 반환).
- [골격·View 평가](../specs/2026-10-09-combat-skeleton-view-evaluation.md): E5 제목·본문, §6.1 성능 표의 `CombatLogUI` 설명 정정.
- [SOLID·SSOT 평가](../specs/2026-10-09-combat-view-solid-ssot-evaluation.md): §7 줄 번호(890), 머리에 개선 설계 링크.
- 코드 변경 없음. 커밋·push 없음.

**P2 실행 기록(2026-10-09):** (`D` = `ViewSystem/Combat/Display/`)

| 파일 | 변경 |
|---|---|
| `D/IFieldHighlightStateReader.cs`(+meta, 신규) | 강조 상태 읽기 계약: 크기·호버·배치/경로 여부·`IsActing`·`LayersAt`·`ZoneAt`·`HasAnyCell`·`TryGetIndex` |
| `D/FieldHighlightState.cs` | 읽기 계약 구현(읽기 멤버만 `public`, 클래스가 internal이라 접근 범위는 같음). 변경 메서드는 그대로 internal |
| `D/IFieldHighlightRenderer.cs` | `Build(mapper, IFieldHighlightStateReader)`, `Release` 계약에 "실패를 모아 끝까지" 명시 |
| `D/FieldView.cs` | `HighlightState`가 읽기 계약을 돌려줌. `Release`는 상태·좌표 변환을 먼저 비운 뒤 표시기 해제(표시기가 실패해도 "구성 안 됨"으로 남아 다음 `Build`가 생략되지 않음) |
| `D/FieldOutlineRenderer.cs` | `ReleaseHandles(ref errors)`: 슬롯마다 참조를 먼저 비우고 Dispose, 실패를 모음. `Release`: 핸들 → 데칼 숨김을 각각 시도, 상태 초기화는 항상, 마지막에 `AggregateException` 1회. `Build`의 핸들 생성 실패 정리도 같은 방식(정리 실패가 있으면 원인과 함께). `Dispose`는 `finally`로 데칼 파괴 |
| `D/FieldCellColorRenderer.cs` | 상태를 읽기 계약으로 보관. `Release`: 기본·대체 풀 반환을 각각 시도, 상태 초기화는 항상, 실패를 모아 1회 |
| 테스트 | `FakeAreaOutlineHandle`에 `ThrowOnDispose`·`DisposeCalls`. `FieldOutlineRendererTests.Release_WhenOneHandleFails_ReturnsOthersResetsStateAndThrowsOnce`(신규: 중간 핸들 실패 → 나머지 반환·데칼 숨김·예외 1회, 재해제 시 중복 Dispose 없음, 재구성 가능). `FieldHighlightStateTests.FieldViewAndRenderers_ExposeOnlyTheReadContract`(신규: 노출 타입·`Build` 인자·읽기 계약에 변경 메서드 없음). `CombatPointerMappingTests` 지역 변수 타입만 변경 |

- **검증:** MSBuild `ProjectL.Tests.Framework`(Systems 포함, 새 파일은 `CustomAfterMicrosoftCommonTargets`) 오류 0, 변경 파일 새 경고 0. Unity Mono 직접 실행 `FieldHighlightStateTests` 6/6. **Unity EditMode 대기:** `FieldOutlineRendererTests`(신규 1건 포함, GameObject 필요), `CombatPointerMappingTests`, `WallAttackInteractionTests`, `CombatDisplayReuseTests`. 재생: CellPrefab·GridOutline 두 모드에서 Combat 진입·이탈이 전과 같은지(동작 변경 없음).
- **범위 밖으로 남긴 것:** `FieldCellPool.ReturnAll`은 풀 안에서 한 셀의 반환이 실패하면 나머지 셀을 건너뛴다. 설계 D5 범위(표시기 단위)를 넘으므로 고치지 않았다. 필요하면 별도 Task.
- Core·골격 파일 변경 없음. 커밋·push 없음.

**P3 실행 기록(2026-10-09):** (`C` = `ViewSystem/Combat/`)

| 파일 | 변경 |
|---|---|
| `C/ICombatAbandonDisplay.cs`·`C/ICombatDevLayerDisplay.cs`(+meta, 신규) | `ICombatBattleDisplay`에서 포기 4개·개발 패널 2개 메서드를 뽑은 좁은 표시 계약. `ICombatBattleDisplay`가 둘을 상속하므로 `CombatSceneDisplay`·테스트 가짜는 그대로다 |
| `C/CombatAbandonController.cs`(+meta, 신규) | 확인창 열림 상태 소유. `Attach`(입구 연결·확인창 닫기·버튼 표시/비활성), `Sync`(매 Tick 가능 여부, 깨지면 닫기), `Close`(열려 있을 때만), `Stop`(오류 정지: 닫고 비활성), `Detach`. 버튼 콜백 `Open`·`Confirm`·`Cancel`은 예외를 오류 경계로 보고. 활성 판단·입력 표시 정리·사실은 조정자가 대리자로 준다 |
| `C/CombatDevLayerController.cs`(+meta, 신규) | 상태 없음. `Attach`(RecordInputOnly면 패널 켜고 연결, Live면 끔), `Hide`, `Detach`, 버튼 → `CanChangeDevLayer` 통과 시 표시 정리 후 Layer 변경 |
| `C/ICombatBattleDisplay.cs` | 위 두 계약을 상속하고 해당 메서드 선언을 뺐다 |
| `C/CombatViewSystem.cs` | 1015 → 895줄. 대리자 5개·확인창 상태·두 region(약 120줄) 삭제, 두 컨트롤러 생성(생성자)·`Attach`·`Sync`(Tick)·`Close`(`SuspendSubmissions`)·`Stop`/`Hide`(`StopInput`, 각각 따로 try)·`Detach`(`Detach`) 호출로 교체. `Receive`의 확인창 입력 차단은 조정자에 남기고 `_abandon.IsConfirmOpen`을 읽는다. 결과 후처리 순서는 바꾸지 않았다. 입력 표시 정리를 `CancelInputPresentation()`으로 묶어 포기 열기·제출 정지에서 같이 쓴다. 골격 계약 필드 이름은 `_abandon` → `_abandonRequest` |
| 테스트 | 신규 `CombatPanelControllerTests` 8건(Attach 상태, 열기 → 표시 정리 1회 → 확인 요청 1회, 이탈 시작 시 닫힘, Stop·Detach 뒤 늦은 콜백 무시, 요청 예외 → 오류 경계, 개발 패널 Layer 변경·Live 숨김·대기/미연결/해제 뒤 무시). **기존 `CombatViewSystemTests`는 고치지 않았다**(동작 보존 증거로 쓴다) |

- **동작 차이:** 정상 경로는 같다. `StopInput`에서 패널 숨김이 실패하면 예전에는 포기 확인창 닫기를 건너뛰었지만, 지금은 둘을 따로 시도한다(실패 경로만 다름).
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. Unity Mono 직접 실행 `CombatPanelControllerTests` 8/8, `CombatInputPolicyTests` 48/48, `CombatInputRouterTests` 20/20. `CombatViewSystemTests`는 `Rig`가 `GameObject`를 만들어 Unity 밖에서 실행되지 않는다. **Unity EditMode 대기:** `CombatViewSystemTests`(포기 4건·개발 패널 4건 포함, 수정 없이 통과해야 함), `CombatPanelControllerTests`, `CombatDisplayReuseTests`. 재생: Live 포기(열기·취소·확인, 확인창 동안 입력 무시), RecordInputOnly 개발 패널.
- Core·골격 파일 변경 없음. 커밋·push 없음.

**P4 실행 기록(2026-10-10, D9·D12·D13):** (`C` = `ViewSystem/Combat/`)

| 파일 | 변경 |
|---|---|
| `C/CombatViewSystem.cs` | **D13:** `AttachDisplay`가 `RequireActiveBattle()`(진입 활성·전투 설치, 아니면 `InvalidOperationException`) 뒤 끝에서 `ShowBattle()`을 부른다: Install → (재진입 확인) → `_battleShown = true` → 경로 입력 생성·Router 연결 → `SyncSubmissionState()` → `SyncInputToCore()`. `Attach(root)`도 표시 구현을 만들기 전에 같은 확인. `_battleInstance`는 Attach 때 현재 전투 번호. **D9:** `BattleCleared`·`BattleInstalled` 분기 삭제, 전투가 사라지거나 바뀌면 Detach(기존 동작). **D12:** 매 Tick `_abandon.Sync()`·`IsTerminal(ReadFacts())` 삭제. `SyncSubmissionState()`(종료면 `SuspendSubmissions`, 그다음 포기 버튼 `Sync`)를 첫 그리기와 **새 결과를 처음 관측할 때**(`UnpresentedResult != null && !_playback.IsBusy`, 결과당 1회) 부른다. `SuspendSubmissions`는 `_abandon.Close()` 대신 `_abandon.Sync()`(골격 Begin이 이탈 상태를 바꾼 뒤 부르므로 버튼이 꺼지고 확인창이 닫힘). Tick 첫 줄은 `CanContinue()`(진입 null 안전). facts의 `IsBattleDisplayed` ← `_battleShown` |
| `C/CombatViewRefresh.cs`·`C/CombatRefreshSignal.cs` | 설치·정리 신호와 표시 이력(`_displayedBattle`·`IsBattleDisplayed`) 삭제. `Observe(S, P, latest, A)`는 결과 순번 일관성과 미리보기 응답 번호만 본다 |
| 테스트 | `CombatViewSystemTests`: `Rig.AttachAndShowBattle()` 순서를 "전투 설치 → Attach → Tick"으로(49곳 사용처 무변경). 의미가 바뀐 테스트: `PreparedBuffer_BeforeEntryActivation_…` → `AttachBeforeEntryActivation_IsRejected_AndDrawsOnlyAfterActivation`, `BeforePreparation_NoInputIsBound` → `AttachWithoutBattle_IsRejected_AndBindsNoInput`, `InstallFailure_Propagates_WithoutBindingInput`(Attach에서 전파, 이어지는 Tick·Detach까지 확인 추가), `Attach_Twice_IsRejected`·`FirstDisplay_OfFinishedBattle_DoesNotBindInput`(전투를 먼저 설치), `BattleCleared_*` 2건 → `BattleRemoved_*`(이름만). **자극만 실제 계약으로 바꾼 4건(단언 그대로, 사용자 결정 "계약대로 이벤트만"):** `Finished_ReleasesInputOnly_…`(Finished를 결과와 함께 게시), `Finished_WithoutNewResult_…`·`SelectCanceledFromUnbind_DuringRelease_…`(이탈 경로 `SuspendSubmissions`), `Abandon_ExitNotIdle_…`(Begin처럼 상태 변경 뒤 `SuspendSubmissions`). `CombatViewRefreshTests`: 새 서명, 설치·정리 테스트 삭제, `Violation_DoesNotConsumeThePreviewAnswer`·`Reset_ObservesTheCurrentPreviewAnswerAgain`로 교체 |
| 문서 | View 설계 §5.3 신호 표·정정 문단, 상세 UML §4.4(Tick 흐름도·클래스)·§5.3 시퀀스·§6.6·§7·§8 |

- **근거 확인(구현 중):** Core는 일반 명령·자동 진행의 Completion을 결과와 함께 게시한다(`CombatProcessingService.Publish` → `WithCompletion`). 결과 없이 Completion을 만드는 공개 API는 `AbandonBattle`(골격이 먼저 `SuspendSubmissions`)과 `ResolveBattle`·`FinalizeTurn`(운영 호출자 없음, 불러도 골격이 Completion을 감지해 이탈 → `SuspendSubmissions`)이다. 이탈 전에도 Policy가 종료 상태의 새 명령·미리보기를 막는다.
- **동작 차이:** 정상 흐름에서 사용자에게 보이는 차이 없음. 첫 그리기가 Attach한 프레임의 첫 View Tick에서 Attach 호출 안으로 앞당겨졌다(같은 프레임). 진입 비활성·전투 없음 상태의 Attach는 이제 기다리지 않고 예외(정상 흐름에서는 생기지 않음).
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. Unity Mono 직접 실행 `CombatViewRefreshTests` 12/12, `CombatPanelControllerTests` 8/8, `CombatInputPolicyTests` 48/48, `CombatInputRouterTests` 20/20, `CombatResultPlaybackTests` 14/14. **Unity EditMode 대기:** `CombatViewSystemTests`(GameObject 필요), `CombatMovementViewTests`, `CombatDisplayReuseTests`, `CombatEntryIntegrationTests`, `StageMapCombatEntryBootSmokeTests`. 재생: Combat 진입 첫 화면·입력, 승리/패배 이탈, 포기(버튼 비활성 시점), 이탈 중 빈 필드(의도한 동작).
- `CombatViewSystem.cs`는 주석·헬퍼가 늘어 895 → 929줄. Core·골격 파일 변경 없음. 커밋·push 없음.

**P5 실행 기록(2026-10-10, D11 = R1·R2):** (`C` = `ViewSystem/Combat/`)

| 파일 | 변경 |
|---|---|
| `C/CombatInputReceiver.cs`(+meta, 신규) | `IStageInputReceiver` 구현. 소유: 슬롯 연결 상태 `IsBound`, `Bind()`(멱등)·`Unbind()`, 입구 검사(오류·연결·`canReceive`[진입 활성·Router 있음]·포기 확인창 열림이면 `FocusLost`·`SelectCanceled`만 통과), 분배 예외 → 오류 경계. `FocusLost()`(internal)도 같은 입구 |
| `C/CombatViewSystem.cs` | 929 → 886줄. `IStageInputReceiver` 구현·입력 메서드 10개·`Receive`·`_isInputBound`·`_slot` 삭제. `BindServices`가 수신 창구를 만들고 분배를 `_router.Route(request, x, y, _characters.Characters)`로 연결. `SyncInputToCore`는 `_receiver.Bind()`, `ReleaseInput`은 `_receiver.Unbind()`(실패는 모아 보고). facts의 입력 가용성·개발 패널의 수신 가능 판단은 `IsInputBound`(= 수신 창구 값). **`ICombatViewService` 멤버 `FocusLost`·`CaptureLost`·`RequestReturn`은 남기고 수신 창구로 넘긴다(D10 철회).** 테스트 입구 `internal CombatInputReceiver Input` |
| 테스트 | `View.X()`·`view.X()` 입력 호출 44곳 → `View.Input.X()`(`CombatViewSystemTests` 20, `CombatMovementViewTests` 19, `CombatDisplayReuseTests` 5, Q4). `FocusLost`·`CaptureLost`·`RequestReturn` 호출은 서비스 멤버라 그대로. `FirstDisplay_BindsInputAndCoreStageLayerOnce`의 슬롯 수신자 단언 → `View.Input`. `CombatMovementViewTests`의 종료 뒤 슬롯 재연결 확인 → `r.View.Input`. 신규 `CombatInputReceiverTests` 6건(연결 전 무시·멱등 Bind, 오류·비활성 무시, 확인창 필터, 분배 예외 1회 보고, **해제 중 `SelectCanceled`가 Router로 감(R2)**, 해제 실패해도 `IsBound == false`) |

- **동작 차이(R2):** 입력 연결을 끊을 때 진행 중인 선택이 있으면 슬롯이 보내는 `SelectCanceled`가 이제 Router까지 가서 경로·배치 표현을 정리한다(예전에는 연결 표시를 먼저 내려 버려졌다). 해제를 부르는 5곳이 모두 먼저 표현을 정리하므로 화면 차이는 없다. 오류 정지(`StopInput`) 중에는 오류 검사로 여전히 무시된다. `SelectCanceledFromUnbind_DuringRelease_DoesNothing`은 이 경로에서도 명령을 기록하지 않음을 확인한다.
- **검증:** MSBuild `ProjectL.Tests.Framework`(새 파일은 `CustomAfterMicrosoftCommonTargets`) 오류 0, 변경 파일 새 경고 0. Unity Mono 직접 실행 `CombatInputReceiverTests` 6/6, `CombatInputRouterTests` 20/20, `CombatInputPolicyTests` 48/48, `CombatPanelControllerTests` 8/8, `CombatViewRefreshTests` 12/12, `StageInputTests` 18/18. **Unity EditMode 대기:** `CombatViewSystemTests`, `CombatMovementViewTests`, `CombatDisplayReuseTests`(GameObject·씬 필요). 재생: 배치 Drag·공격·이동·복귀(R키·HUD 버튼)·포커스 잃기, 포기 확인창 동안 입력 무시.
- Core·골격 파일 변경 없음. 커밋·push 없음.

**P6 실행 기록(2026-10-10, D1, Assets/AGENTS.md 버튼 규칙):** (`C` = `ViewSystem/Combat/`, `D` = `C/Display/`)

| 파일 | 변경 |
|---|---|
| `D/CombatAbandonPanel.cs` | `IUIInputReceiver`(명시적 구현). 슬롯 0 포기·1 확인·2 취소. `Bind`는 입구(`Action`)만 보관하고, 처음 연결할 때 한 번 `UIInputBinding.Attach(button, this, slot)` 3개. `Click(slot)` → 해당 입구. 해제는 입구를 비움. `UnityAction`·`onClick` 삭제 |
| `D/CombatHud.cs` | `IUIInputReceiver`. 슬롯 0 복귀·1 완료 확인. `BindReturn`(null 아니면 처음 한 번 연결)·`ShowCompletion`이 입구만 보관. 완료 확인은 `ConfirmCompletion()`: 입구를 꺼내 비우고 버튼을 끈 뒤 한 번 호출(예전 세대 번호·클로저 대신, `HideCompletion`이 입구를 비워 늦은 클릭 무시). `ShowCompletion`의 `confirmed` null 검사 추가 |
| `C/CombatSceneDisplay.cs` | `IUIInputReceiver`. 개발 패널 슬롯 0 배치·1 전투. `ShowDevLayerPanel`이 처음 한 번 연결하고 입구만 보관, `HideDevLayerPanel`·`Release`가 비움, `Click`은 Release 뒤 무시 |
| 테스트(`CombatDisplayReuseTests`) | `onClick.Invoke()` 2곳 → `ExecuteEvents` pointerClick(`Click` 도우미). 신규 3건: 포기 패널(클릭·Submit이 슬롯 입구로, `UIInputBinding` 부착, 영구 리스너 0·`onClick.Invoke()`는 무동작, 비활성 버튼 무시, 재연결 시 이전 입구 무시, 해제 뒤 무시), HUD 완료 확인(한 번만, 버튼 꺼짐, 숨긴 뒤 이전 입구 무시), 실제 Combat 씬 개발 패널(버튼 → 입구, 영구 리스너 0, 숨김·Release 뒤 무시) |

- 바깥 계약(`BindAbandon`·`BindReturn`·`ShowCompletion`·`ShowDevLayerPanel`)은 그대로라 `CombatViewSystem`·컨트롤러·`FakeDisplay`는 바뀌지 않았다. **Combat.unity 씬 변경 없음**(`UIInputBinding`은 실행 중 붙는다. 씬 버튼 7개는 Inspector 리스너가 없음을 확인함).
- **동작 차이:** 없음. `UIInputBinding`은 버튼이 활성·누를 수 있을 때만 왼쪽 클릭과 Submit을 전달한다(기존 `onClick`과 같은 조건).
- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. 바뀐 테스트는 모두 GameObject·씬이 필요해 **Unity EditMode 대기**: `CombatDisplayReuseTests`(신규 3건 포함), `UIInputBindingTests`, `CombatViewSystemTests`. 재생: 포기·확인·취소, 복귀 버튼, 결과 확인, RecordInputOnly 개발 패널 버튼, 키보드·게임패드 Submit.
- Core·골격 파일 변경 없음. 커밋·push 없음.

**P7 실행 기록(2026-10-10, D7 + R7, 동작 변경 없음):** (`C` = `ViewSystem/Combat/`)

| 파일 | 변경 |
|---|---|
| `C/CombatViewSystem.cs` | 클래스 주석을 최종 구조로(맡는 것: 수명·Attach 첫 그리기·Detach·오류 정지·결과 소비 순서·Layer 동기화·facts·골격 이탈 계약·종료/포기 상태 맞춤 / 맡기는 것: Playback·Refresh·Policy·InputReceiver·Router·Gate·Interaction·PathInput·Abandon/DevLayer 컨트롤러·Display, 타임라인은 VR6 대기). `SyncInputToCore`의 "경로 취소 + 표시 정리" 2곳 → `CancelInputPresentation()` |
| `C/OrderedCombatPresentation.cs`·`C/CombatMovementPlayback.cs` | 한 줄 다중 문장·중괄호 없는 블록을 Allman으로 풀어 씀, 클래스 요약 주석 추가. 로직·순서 그대로(`Play`의 인자 검사도 더하지 않음) |
| `C/ICombatPresentation.cs` | `Attach`에 붙어 있던 "결과 하나를 재생합니다" 요약을 `Play`로 옮기고 `Attach`·`Tick`·`Detach`에 각자 요약 |
| `C/CombatPathInput.cs` | 생성자 인자 7개 모두 `ArgumentNullException` 검사(기존 호출자·테스트는 모두 null이 아님 확인) |
| `LifeCycles/Combat/CombatLifeCycle.cs`(**골격, 주석 한 줄만**) | "포기는 Core 포기 API 인수 뒤 별도 경로로 연결한다" → 포기 Completion은 `RequestAbandon` 경로(K22)가 처리 중이라 제외한다는 현재 사실로 |

- **검증:** MSBuild `ProjectL.Tests.Framework` 오류 0, 변경 파일 새 경고 0. Unity Mono 직접 실행 `CombatPresentationCleanupTests` 6/6, `CombatMovementFeedbackTests` 23/23, `CombatPathInputCleanupTests` 1/1, `MovementDraftTests` 41/41, `CombatInputReceiverTests` 6/6, `CombatInputPolicyTests` 48/48, `CombatViewRefreshTests` 12/12, `CombatPanelControllerTests` 8/8. **Unity EditMode 대기:** `CombatSceneEventPresenterTests`·`CombatMovementViewTests`(이동 연출 재생, GameObject 필요). 재생: 이동 한 걸음·복귀 연출, 공격 등 비이동 사건 순서.
- **§7.68 코드 작업 완료.** 남은 것: P2~P7 Unity 확인, 보류(VR6+R5·R6·평가 항목 5), 문서 정리(상세 UML·프로그래머 인계서를 P3~P6 구조로, CLAUDE.md 현재 상태).
- Core 파일 변경 없음. 커밋·push 없음.

### 7.69 전투 진행 Job·Plan 재구성 (2026-10-10, R1 코드 완료·Unity 확인 대기)

제작자 계획대로 `CombatLifeCycle`을 트리거로 줄이고 판단을 `CombatPlan` 한 곳으로 모은다. 설계는 [재구성 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md), Task·실행 기록은 [재구성 Task 계획](2026-10-10-combat-job-plan-restructure-plan.md)에만 적는다. 착수 조건은 §7.68 P2~P7 Unity 확인 완료다. VR6은 이 재구성 뒤로 둔다.

## 8. 확장 시 주의사항과 필요한 작업

아래는 기능 확장 시 확인할 조건과 증거다. **W16~W19의 구조·계약 보완은 B1·C·D·E에 반영**됐다. 이동 연출은 이미 병합됐으므로 X3의 해당 범위를 지금 F1/F2 인수에서 대조한다. 비이동 VFX·새 표시·이탈의 추가 구현은 각 확장 작업에서 수행한다. 8차에서 옮겨 온 검증(자원 해제 계약 테스트 → X3, 규모별 측정 → X4)은 해당 확장의 **착수 조건이 아니라 완료 증거**다. 각 계약의 원본은 설계 §6.1·§9·§10.1이며 같은 규칙을 별도 정책으로 복제하지 않는다.

| 항목 | 확인 시점 | 해야 할 일 | 완료 증거 |
|---|---|---|---|
| X1 책임 집중 방지 | C 구현과 새 입력/표시/연출 추가 때 | ViewSystem은 연결·후처리 순서, Playback은 재생 수명, Interaction은 입력 해석·대기, Policy는 허용 판단을 유지. 새 기능의 조건·상태를 담당 객체에 두고 전체 Runtime/CoreGroup을 넘기지 않음 | 담당 객체 단위 검증 가능, 재생/국면 상태 중복 0. 클래스가 커졌다는 이유만으로 범용 서비스 계층을 추가하지 않음 |
| X2 공통 오류 경계 | B1과 오류 처리 참여 모듈 추가 때 | RuntimeSystem/Common의 같은 경계를 주입. 공통 객체에 View·LifeCycle·Unity 구체 참조를 추가하지 않고 생성·소유는 Bootstrap에 유지 | 같은 인스턴스 조립 확인, 최초 오류 고정·정리 실패 수집·보고 1회, Flow의 View/Boot 의존 0 |
| X3 실제 비동기 연출 | 이동은 병합 완료: 6B F1/F2에서 인수. 비이동 VFX는 도입 시 | 설계 §9.2의 단일 재생·메인 스레드 완료/실패·멱등 Cancel·통지 전 자원 정리를 실제 구현에 적용. 대여 Scope·구독·트윈의 소유자/해제 시점을 명시 | 기존 Playback·이동·Detach 테스트와 §7.20의 실행 증거부터 대조해 부족한 계약 테스트만 추가: 완료·취소·연출 실패마다 잔여 자원 0·해제 1회·통지 전 해제 순서, 재진입 Cancel, 늦은 통지, 반복 Cancel, 부분 시작/정리 실패. 해제 실패 시 나머지 정리·실패 자원 진단을 확인. 정상 전투 종료는 Cancel 0·마지막 확인 1회 |
| X4 표시·전투 규모 확장 | 새 HUD/Presenter·보드 변경·유닛 수 증가 전후 | 정적 구성 변경의 명시적 신호를 정의하고 기존 View를 재사용. 동일 값 쓰기를 생략. D.6a 기준으로 비용을 먼저 측정한 뒤 필요할 때만 증분 갱신 추가 | **(8차에서 옮김)** 목표 기기·최대 필드/유닛/손패 규모·허용 기준을 먼저 정하고, 유휴·Drag·연속 자동 진행·밤 종료·전투 종료 구간의 CPU 시간·GC 할당·UI 갱신·풀 대여/반환 수와 결과 지연을 기록. 풀 전체 재대여·정적 필드 재구성·UI 재설정 없음. 최적화 전후 모두 결과 확인 순서·입력 대기 회귀 유지. 변경 없는 결과도 확인 1회, 유휴에 객체 재생성 없음 |
| X5 새 명령·미리보기·규칙 표시 | 타임라인·대상 표시·새 행동 추가 시 | 해석/허용 조건은 Interaction/Policy, 게임 규칙은 Core. 승인된 읽기 계약을 사용하고 Core를 매 프레임 직접 호출하거나 규칙을 View에서 재계산하지 않음. 단일 대기를 바꿔야 하면 새 명령의 대기/취소/거절 수명을 먼저 정의 | 번호·단계·행위자·중복·거절·늦은 응답 회귀 추가. RecordInputOnly 예외가 Live 조건을 우회하지 않음 |
| X6 포기·이탈·재진입(F2) | 골격 Phase 6의 F2 연결 전 | 전환 소유자·Core 포기 입력 조립·Runner 종료·남은 일반 결과·Completion 표시 완료·EndBattle 순서를 골격 계약과 맞춤. 입력만 닫는 동안 Playback을 보존하고 실제 해제 시 Cancel 후 Presenter 자원 반환 | 중복 이탈 요청·진행 중 연출·취소 실패·늦은 콜백·재진입 검증. 일반 결과 Acknowledge와 Completion 표시 완료를 별도로 확인 |

확장 작업을 시작하면 해당 X 항목을 그 작업의 Task·회귀 검증에 연결하고, 구현이 끝난 결과만 §7 또는 해당 확장 계획의 실행 기록에 적는다. 이미 반영된 이동 연출은 현재 인수 범위다. 나머지 비이동 VFX·X5·X6의 후속 기능은 기존 상태 표시 완료와 구분한다. 성능 결과가 없거나 계약 검증을 하지 않았으면 그 항목은 미검증으로 인계한다.
