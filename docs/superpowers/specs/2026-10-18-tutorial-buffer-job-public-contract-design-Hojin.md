# 튜토리얼 Buffer·Job 공개 계약 설계

- **2026-11-10 후속 설계 확정:** [최신 전투 규칙 교체·공격·턴 종료 실습 설계](2026-11-10-tutorial-current-rules-refresh-design-Hojin.md)를 사용자가 승인했다. 기존 틀 유지와 공격·턴 종료 실습 추가, 적/넥서스 명중(0피해 포함), **설명 자동 진행 제거·클릭으로만 다음 진행**을 적용한다. 아래의 5초·타이머·만료 진행은 교체 전 구현 기록이며 새 정책은 후속 설계 §6.4를 따른다. [Phase별 Task 계획](../plans/2026-11-10-tutorial-current-rules-refresh-plan-Hojin.md)의 A(자동 진행 제거·진입 대기·카드 개폐 소유권 정리)를 코드·에셋에 반영했고 사용자 Unity 확인 대기다. 상세 상태와 검증 근거는 새 계획 §9.2를 따른다. B의 새 실습 판정·입력 제한도 반영했으며 정적 검수와 Unity 미확인은 새 계획 §9.3을 따른다. C~E는 미착수며 아래의 기존 구현·인수 기록을 새 구현 완료로 갱신한 것은 아니다. 현재 로컬 정의의 `targetMapKey: 1`은 기존 사용자 변경으로 보존했다.
- **강조 표현 확정:** UI에는 점선 테두리, 유닛에는 모델 아웃라인을 기존 강조와 함께 표시한다. 표시·정리 수명과 모델 외형 합성은 후속 설계 §6.5를 따른다. 현재 제품 구현에는 아직 반영하지 않았다.
- 작성자: 호진(이호진)
- 상태: 설계 확정(2026-10-19), UI 보완 확정(2026-10-20). T-1~T-7 구현·사용자 커밋(`34ee0de4`·`c9543d5d`까지), 화면·재생은 Unity 확인 대기, T-8 미착수. 2026-10-27 정리 당시 HEAD의 1-1 정의는 `targetMapKey` 10000이라 튜토리얼이 실행되지 않았다(§16). 현재 로컬 값과 후속 설계는 위 2026-11-10 안내를 따른다.
- 범위: 다른 기능이 튜토리얼을 쓰는 공개 계약(Buffer·Job)과 1-1 튜토리얼의 진행·UI·입력 정책. 문구 원본은 정의 에셋, 진행·확인 상태는 계획이 가진다. Core·전투 진행 규칙은 다루지 않는다.
- 계획: [튜토리얼 구현 계획](../plans/2026-10-19-tutorial-implementation-plan-Hojin.md)
- 정리: 2026-10-27 1부(공개 계약 설계)와 2부(기획 대조·UI 보완 검토, 2026-10-20)를 이 문서로 합쳤다. 2부는 [부록 A](#부록-a-기획-대조ui-보완-검토-요약-2026-10-20)로 줄였다. 코드 주석과 다른 문서가 `설계 §8.1`처럼 절 번호를 부르므로 §1~§15 번호는 그대로 두었다. 옛 판은 git `10914570`.

## 1 목적과 통과 기준

사용자 요구(2026-10-18): 다른 작업자가 튜토리얼 내부 코드를 읽지 않고 Buffer와 Job의 공개 계약만으로 기능을 쓸 수 있어야 한다. Core와 통신하는 것처럼 한다.

외부 호출을 Job으로 감싸는 것만으로는 완료가 아니다. 이 문서와 공개 Buffer·Job 선언만으로 다음을 설명할 수 있어야 한다.

1. 제공하는 기능과 입력 데이터.
2. 실행을 요청하는 Job 등록 방법.
3. 어떤 요청이 접수·처리됐고 어느 실행 회차에 적용됐는지.
4. 현재 진행(무엇을 기다리는지 포함), 정상 완료, 취소·중단(전투 승패에 의한 중단 포함), 거절, 실행 오류의 구분.
5. 중복 등록, 늦은 입력, 씬 이탈, 등록 실패, 결과 보존의 의미.

외부 소비자의 사용 예제에 TutorialSystem, 내부 실행 서비스, Presenter, Controller, 변경 가능한 요청 객체, lease, Dispose 콜백이 필요하면 기준 미달이다. 불변 입력·결과 DTO는 Buffer 계약에 든다. 공통 Framework의 서비스 바인딩과 JobRegister 사용은 허용한다.

## 2 지금 구조

### 2.1 구성 요소와 책임

| 구성 요소 | 위치(`Assets/ProjectL/Script/Systems/` 아래) | 책임 |
|---|---|---|
| 공개 계약·상태 Store | `RuntimeSystem/BufferSystems/Tutorial/`(`TutorialBuffer`, DTO·enum, `TutorialStepRule`) | 요청 큐·결과·세션 상태. 공개 Sink 셋(§4). 단계 규칙 보관 |
| 판단·한 걸음 | `RuntimeSystem/LifeCycleSystem/LifeCycles/Tutorial/`(`Plans/TutorialPlan`, `Jobs/`, `TutorialFacts`·`TutorialFactsReader`·`TutorialProgression`) | 다음 한 걸음 판단(`Decide`)과 Job 일곱 종류(§9.1) |
| 실행 소유자 | `RuntimeSystem/InGameSystem/ViewSystem/Tutorial/TutorialViewSystem`(`ITutorialViewExecution` 구현) | 런타임 수명, Tick 등록 트리거, Combat 화면 Attach·Detach, 1-1 판정, 5초 타이머, 정리 |
| 전용 입력 | `ViewSystem/TutorialInput/`(이름·Layer·Controller·Factory), `ViewSystem/Tutorial/TutorialViewInputAdapter`·`TutorialViewInputLease` | 설명 중 “다음” 입력, Layer 빌리기·돌려주기, UI 선택 비우기(§15.4.3) |
| 표시 | `ViewSystem/Tutorial/TutorialViewUI`(`UIBase`, `UIId.Tutorial = 31`)·`TutorialViewUIDisplay`·`TutorialViewOverlay`·`TutorialViewFocusLocator`·`TutorialViewExplanationLayout`·`TutorialViewCardVisibility` | 덮개·설명·실습 안내 그리기, 대상 위치, 설명 배치, 임시 카드 목록 수명(§15.13) |
| 정의 데이터 | `DataBaseSystem/Data/View/Table/Tutorial/`, `Queries/ViewDataBase/ViewTable/TutorialDefinitionTable`, 에셋 `DataBaseSO/ViewGroup/TutorialDefinition/` | 문구·단계 순서·대상·실습 조건·표시 의도(§15.11) |
| 전투 접점 | `ViewSystem/Combat/Input/`(`CombatInputPolicy`, `CombatInputRouter`, `Assist/CombatBlockedInputReport` 등) | 이동 실습 제한 읽기와 막힌 시도 알림(§15.10). 전투 작업자 영역 |
| 프리팹·씬 | `Prefabs/UI/Tutorial/TutorialUI.prefab`, `Assets/Scenes/Combat.unity` 인스턴스 하나 | Screen Space Overlay Canvas 정렬 110 |

조립은 `InGameViewSystemFactory` → `InGameViewSystemGroup` 모듈 보관 → `service.ViewGroup.TutorialExecution` → `TutorialPlan`이다. 등록은 `JobRegisterSystem.Initialize`의 한 줄이다. Combat 씬 연결 순서는 `SceneNodeOrder.TutorialAttach`(전투 View 연결 + 10 = 110)·`TutorialDetach`(전투 View 해제 − 10 = −110)다. 연결은 전투 View가 전투를 붙인 뒤라 1-1 판정이 전투를 본다. 해제는 전투 View가 Layer를 바꾸기 전이라 빌린 입력을 먼저 돌려준다.

### 2.2 흐름

```text
실행 소유자 Attach(1-1이면 Start 제출) ─┐
전용 입력 “다음”(Continue 제출) ────────┼─► 요청 Buffer
전투 View 막힌 시도(ReportBlockedInput) ─┘
실행 소유자 Tick: 오류·로딩 아님 + Runner 빔 → Register(Tutorial, Next)
TutorialPlan.Decide(튜토리얼·전투 Buffer + 실행 소유자 사실) → Job 하나 또는 없음
Job → 상태·결과 Buffer 게시, 실행 소유자에게 표시·입력 지시
전투 View 입력 검사 ◄── CombatInputRestriction 읽기
```

### 2.3 기반 사실(설계 착수 때 확인, 2026-10-27 갱신)

| 사실 | 근거 | 설계에 주는 조건 |
|---|---|---|
| `Register(rootType, jobType)`는 Plan을 즉시 불러 Job 목록을 Runner에 넣는다. 범용 등록에는 요청 번호 인자가 없다 | [JobRegisterSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs) | 등록 API가 요청 번호를 전달한다고 가정하지 않는다 |
| `JobRootType.Tutorial = 6`과 `JobType.Next` 연결은 T-3에서 더했다 | `JobRootType.cs` | 범용 등록으로 TutorialPlan을 부른다. 별도 실행 경로를 만들지 않는다 |
| 미완료 Job은 다음 Tick에도 현재 작업으로 남는다 | [JobRunnerSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRunner/JobRunnerSystem.cs) | 사용자·연출·다른 기능을 기다리며 Runner를 점유하지 않는다 |
| 전투 Plan은 대기할 때 Job을 만들지 않는다 | [CombatPlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs) | 대기는 상태로 표현하고 할 일만 등록한다 |
| Buffer는 좁은 ReadSink/WriteSink로 나누고 내부 Factory·Group에서 조립한다 | [CombatViewBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs), [BufferSystemFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs) | 튜토리얼 전용 공개 Sink를 넘기고 전체 BufferQuery를 기능 안에 보관하지 않는다 |
| InputSystem의 Layer 독점은 겹치지 않으며 옵션창도 이를 쓴다 | [InputSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/InputSystem/View/InputSystem.cs), [OptionUI](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/OptionUI.cs) | 어느 화면에서나 즉시 입력을 독점한다고 약속하지 않는다 |
| 로비 진입과 스테이지 진입·이탈 준비는 Runner가 비어 있어야 하며 아니면 예외다 | [LobbyLifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyLifeCycle.cs), [StageLifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageLifeCycle.cs), [StageFlow](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageFlow.cs) | 할 일이 없으면 Job을 만들지 않고, 화면 해제 마감은 Job 없이 끝낸다(§9·§10) |
| 전투 입력 검사는 이동·턴 종료·미리보기 모두 현재 Layer가 전투 Layer인지 먼저 본다 | [CombatInputPolicy](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatInputPolicy.cs) | 튜토리얼 Layer로 전투 조작을 일부만 허용할 수 없다. “이동만 허용”은 전투 입력 검사를 고친다(§15.10) |

## 3 범위·선택·결정

### 3.1 포함하는 범위

- 다른 기능이 튜토리얼을 쓰는 공개 요청·Job·상태·결과 계약.
- 튜토리얼 전용 입력의 진행 입력(§15.4.3)이 외부와 같은 공개 요청 경로를 쓰는 원칙.
- 실행 회차, 단계 종류(설명·실습)와 대기 이유, 단계 입력의 유효성, 종료·취소·중단·오류, 결과 보존.
- 공용 Runner와 연결하는 실행 규칙, 조립 경계, 계약 검증 항목.
- 1-1 튜토리얼의 진행·UI·입력 정책(§15). 문구·단계 순서·키 목록은 §15와 정의 데이터가 원본이다.

계약의 진행 명령(`Continue`)은 **설명 단계의 사용자 확인**이다. 전투 성공을 선언하거나 실습을 강제로 통과시키는 명령이 아니다.

### 3.2 약속하지 않는 것

§4~§12의 공개 계약은 UI 외형·프리팹 내부 형식을 약속하지 않는다(UI 기준은 §15.4·§15.13). 전투 중 특정 유닛·칸 제한, 게임 자동 진행 정지, 완료 기록의 저장 파일 영속화, 여러 튜토리얼의 동시 실행, 전체 건너뛰기는 범위 밖이다.

바뀐 것: 1차 초안의 전체 건너뛰기(`Skip` 명령·`CanSkip`·`AllowSkip`·`Skipped`·`SkipNotAllowed`·`UserSkipped`) → 삭제(2026-10-19 2차 초안, 실제 튜토리얼에 건너뛰기가 없음). 5초 자동 진행·설명 시작 조건·실습 대기는 명령이 아니라 내부 진행과 공개 상태(`StepKind`·`WaitReason`)로, 전투 승패에 의한 중단은 종료 사유 `BattleEnded`로 표현한다.

### 3.3 검토한 실행 방법

| 방법 | 장점 | 비용·판단 |
|---|---|---|
| **요청 큐 + TutorialPlan(`Next`)** | 기존 범용 등록과 `CombatPlan` 방식을 그대로 쓴다. Plan이 사실을 읽어 다음 한 걸음을 고르고, 할 일이 없으면 Job을 만들지 않는다. 불변 요청 큐를 순서대로 처리해 중복 깨우기가 안전하다 | 요청 기록과 Job 등록이 두 동작이다. 실행 소유자 Tick이 등록을 맡아 누락을 복구한다. **선택** |
| 요청별 번호를 캡처하는 Job | 어느 요청을 실행할지 Job마다 분명하다 | 범용 `Register`에는 번호 인자가 없다. 루트 전용 등록을 더하면 예약·중복 방지·등록 실패 복구 계약이 늘어난다. 도입하지 않는다 |
| 튜토리얼 전용 서비스가 요청·실행 객체를 반환 | 직접 호출과 회차 수명을 묶기 쉽다 | 외부 작업자가 추가 서비스·객체를 알아야 해서 §1 기준에서 제외한다 |

바뀐 것: 옛 판의 “요청 객체를 받는 것은 전투 진입 전용 `RegisterCombatEntry`뿐” → 지금 사실이 아니다. 전투 진입도 범용 `Register(Combat, Entry)`이고 요청은 진입 서비스에서 읽는다(2026-10-23 전투 골격 점검 K-b).

### 3.4 결정 기록

| 결정 | 날짜·근거 | 원본 절 |
|---|---|---|
| 요청 큐 + `TutorialPlan`(`Next`), 기존 Plan·Job 방식(Plan은 Buffer와 `service`만 읽음) | 2026-10-19 설계 확정, 2026-10-20 사용자 “다른 곳에서 쓰는 Job·Plan 방식으로” | §3.3·§9.1 |
| 실습 통과 조건(`TutorialPracticeGoal`)은 단계 규칙 `TutorialStepRule`에 둔다. Plan은 정의 표를 읽지 않는다 | 2026-10-20 사용자(정의 표가 internal이라 View 그룹으로 넘길 수 없음) | §9.1·§15.11 |
| 전체 건너뛰기 없음 | 2026-10-19 | §3.2·§6 |
| 1-1 = 1장에서 처음 들어가는 전투. 진입마다 실행, 중도 이탈 뒤 재진입은 8부터 새 회차 | 2026-10-19 사용자 | §15.2·§15.12 |
| Start는 실행 소유자가 Combat 화면 Attach 때 제출 | 2026-10-19 사용자 | §15.12 |
| 튜토리얼 내용은 View DB 표. 설명 구간은 저장하지 않고, 단계 키는 기획서 슬라이드 번호(실습 12·18) | 2026-10-19 사용자, 세부 형식 2026-10-20 사용자 | §15.11 |
| 설명 중 입력 = 튜토리얼 전용 Layer + 정렬 100번대 화면 막 + UI 선택 비우기 | 2026-10-19 사용자 | §15.4.3 |
| 다른 기능의 입력 독점은 “현재 Layer가 설정 Layer인가”로 판정 | 2026-10-19 사용자 | §10 |
| 설명 구간 시작 조건은 `TutorialPlan` 한 곳에 정의하고, 설명 시작 Job이 입력을 빌리기 직전에 같은 함수로 다시 확인 | 2026-10-19, 재확인 2026-10-20 사용자 | §8.1 |
| 전투 정지 장치를 만들지 않는다. 설정창은 기존 동작으로 화면 기준 멈춤 | 2026-10-19 사용자 | §15.4·§15.9 |
| 배치 검사·확인창은 튜토리얼이 만들지 않고 기존 `CombatDeploymentConfirmation`을 쓴다 | 2026-10-19 사용자 변경 | §15.3 |
| `runInBackground`를 바꾸지 않는다. 타이머는 튜토리얼 컴포넌트의 창 활성 알림으로 멈춘다 | 2026-10-19 사용자 변경 | §15.4.1·§15.4.2 |
| 이동 실습은 “이동만 허용”. 전투 입력 검사를 튜토리얼 작업에서 직접 고친다 | 2026-10-19 사용자, 전투 작업자 동의 | §15.10 |
| 이동 실습의 공격·복귀·턴 종료 버튼은 비활성화. 키 등 실제 시도만 안내 | 2026-10-20 사용자 후속 지시. 바뀐 것: 2부 조사 때 고른 “막힌 버튼을 눌리게 두고 안내” → 버튼 비활성 | §15.10 |
| 턴 종료 예외. 바뀐 것: 갈 칸이 없을 때만 턴 종료 허용(2026-10-19, 10-20 재확인) → MoveOnly에서도 수동 플레이어 턴 종료 허용, 미완료 실습의 배치 국면에서 정상 조건의 전투 진입 허용 | 2026-10-26 전투 View V4-g(사용자 Q-V32 A·Q-V38 A, 새 Core 규칙 대응) | §15.10 |
| 실습 통과의 “현재 행동 유닛” = 이동 결과가 났을 때의 행동 유닛 | 2026-10-20 사용자 | §15.5 |
| 튜토리얼 UI = 전용 프리팹, Combat 씬에 하나, Screen Space Overlay 정렬 100번대 | 2026-10-19 사용자 | §15.4 |
| 덮개 = 검정, 투명도 5%(알파 0.95). 바뀐 것: 알파 13/255 → 0.95 | 2026-10-20 사용자 “약 95% 투명으로 말고 5% 투명으로” | §15.4 |
| UI 보완: 실제 덱 카드 강조, 대상 옆 설명, 상단 실습 띠 | 2026-10-20 사용자 | §15.13 |
| 19는 Attack 버튼만, 14는 체력 아이콘·수치만, 21은 하단 이동·공격 비용 추가, 19 설명 중에도 행동 버튼 패널 표시 | 2026-10-20 사용자 요청 넷 | §15.13.1 |
| 별도 공격 실습 없음. 이동 1회 성공 조건은 유지 | 2026-10-19 | §15.4 |
| 입력·UI 추가는 구조 기준을 따르므로 공유하지 않음. 그 밖의 공용 파일은 Framework 작업자에게 공유 | 2026-10-19 사용자 | §13 |

## 4 외부에 공개하는 목록

| 종류 | 공개 이름 | 외부 역할 |
|---|---|---|
| 요청 쓰기 Sink | `ITutorialRequestBufferWriteSink` | 값 명령을 제출하고 요청 번호를 받는다 |
| 요청 결과 읽기 Sink | `ITutorialResultBufferReadSink` | 요청 번호에 대응하는 처리 결과를 비파괴 조회한다 |
| 상태 읽기 Sink | `ITutorialStateBufferReadSink` | 실행 환경, 사용 가능한 정의, 실행 회차 상태, 전투 입력 제한을 조회한다 |
| Job 식별 | `JobRootType.Tutorial` + `JobType.Next` | 튜토리얼의 다음 처리 기회를 요청한다 |
| 값 DTO·enum | §5~§8의 데이터 | 인수·결과의 뜻. 동작하는 객체를 넘기지 않는다 |

의사 시그니처(실제 선언은 `BufferSystems/Tutorial/`):

```text
ITutorialRequestBufferWriteSink
    TrySubmit(command, out requestNumber, out submitFailure) -> bool
ITutorialResultBufferReadSink
    HandledRequestNumber -> int
    TryGetResult(requestNumber, out result) -> bool
ITutorialStateBufferReadSink
    Context -> TutorialContextSnapshot
    AvailableDefinitions -> 읽기 전용 TutorialDefinitionInfo 목록
    TryGetSession(sessionId, out session) -> bool
    CombatInputRestriction -> TutorialCombatInputRestriction
공용 IJobRegisterSystem
    Register(JobRootType.Tutorial, JobType.Next)
```

- `TrySubmit`의 true는 **요청 Buffer에 불변 데이터가 보관됨**이다. 기능 수락이나 완료가 아니다.
- false면 번호는 0이고 보관하지 않는다. `submitFailure`는 `NotInitialized` 또는 `RuntimeStopped`다. 의미상 거절은 접수 뒤 결과에 게시한다.
- null, 잘못된 명령 형태, 0 이하의 필수 번호 같은 형식 오류는 인자 예외다. 번호를 발급하거나 부분 기록하지 않는다. 번호 소진은 상태 예외이며 재사용하지 않는다.
- Buffer 기록은 Job을 실행하거나 UI를 표시하지 않는다. JobRegister도 요청 인수를 보관하지 않는다.
- 조회는 삭제·확인·취소를 일으키지 않는다. 결과를 정리하거나 처리 번호를 바꾸는 외부 API는 없다.
- **얻는 경로:** 외부 모듈은 `PostBindService`에서 `BufferSystemKey.Buffer`로 받은 `IBufferQuery`의 튜토리얼 Sink 속성과 Global의 `IJobRegisterSystem`을 받는다. `IBufferQuery`는 모든 Sink를 노출하므로 “외부 쓰기는 명령 제출만”은 관례로 지킨다(§13).
- **등록은 처리를 앞당길 뿐이다:** 표준 사용법은 TrySubmit 뒤 `Register(Tutorial, Next)`다. 빠뜨려도 실행 소유자가 Runner가 빌 때마다 등록한다(§9.3). 입력 콜백 안에서는 TrySubmit만 하고 Register를 부르지 않는다.
- 1-1 튜토리얼에서 외부가 쓰는 것은 `Start`·`Cancel` 제출과 상태·결과 조회, 전투 View의 제한 읽기와 `ReportBlockedInput` 제출이다(§15.10). `Continue`는 튜토리얼 전용 입력이 제출한다. 5초 자동 진행·설명 시작·실습 통과·전투 승패 중단은 내부 진행이며 외부는 §8의 상태로 읽는다.

## 5 식별자와 실행 환경

| 필드 | 뜻·발급·유효 범위 |
|---|---|
| `RequestNumber` | 성공한 제출마다 발급하는 양수 int. 런타임 동안 단조 증가하며 씬·Run·세션 변경으로 초기화하지 않는다 |
| `SessionId` | **수락된 Start의 RequestNumber를 그대로 쓴다.** 별도 회차 카운터는 없다. Start가 거절되면 그 번호의 세션은 없다 |
| `ContextId` | 실행 소유자가 지원 화면 연결마다 발급하는 양수. 같은 씬을 다시 연결해도 새 값이다. 연결이 없으면 0 |
| `TutorialKey` | 공개 정의 목록의 양수 키. 외부는 SO나 내부 정의 객체를 넘기지 않는다 |
| `StepKey` | 정의 안에서 안정적인 양수 단계 키. 배열 위치·Presenter 이름이 아니다. 단계 전에는 0 |
| `StepRevision` | 한 세션에서 단계에 들어갈 때마다 느는 입력 유효성 번호. 첫 단계 전 0. 같은 StepKey로 돌아가도 새 값이다. 설명 단계는 시작 조건을 기다리는 동안에도 같은 revision이고, 표시된 뒤의 확인도 그 revision으로 받는다 |

`Context`는 `ContextId`, `IsAvailable`, `UnavailableReason`, `ActiveSessionId`를 준다. `UnavailableReason`은 `None`·`NotInitialized`·`NoContext`·`UnsupportedContext`·`RuntimeStopped`다. `IsAvailable`은 환경의 사용 가능 여부다. 다른 세션 실행 중은 `ActiveSessionId`로 구분하며 Busy를 Unavailable로 감추지 않는다.

정의 목록은 현재 환경에서 요청할 수 있는 `TutorialKey`와 표시 이름이다. 목록에 있다고 실행 시점의 수락이 예약되지는 않는다. 단계 구성(순서·통과 조건·입력 제한·문구)은 정의 데이터가 가지며 공개 목록에 싣지 않는다.

`CombatInputRestriction`은 `None` 또는 `MoveOnly`다. 활성 세션이 `Running`이고 (1) 현재 단계가 “이동만 허용” 실습이거나 (2) 그 실습을 통과한 뒤 바로 다음 설명이 시작 조건을 기다리는 중(`WaitReason.StartCondition`)이면 `MoveOnly`다. 다음 설명이 입력을 빌려 표시를 시작하면 `None`이 된다(그때부터는 튜토리얼 입력이 막는다). 상태 Store가 단계 규칙과 현재 단계·대기 이유로 매번 계산하며 저장하지 않는다.

한 런타임에 Starting/Running/Ending 세션은 **하나**다. 다른 Start는 대기열로 미루지 않고 `Busy`로 거절한다. 종료된 세션의 조회는 다음 Start 뒤에도 된다.

외부는 Start 때 읽은 `ContextId`를 함께 낸다. 처리 전에 화면이 바뀌면 `ContextChanged`로 거절한다. 튜토리얼이 화면을 바꾸거나 과거 화면으로 돌아가 요청을 실행하지 않는다.

## 6 공개 명령 계약

`TutorialCommandData`는 아래 넷 중 하나다. 정적 생성 함수가 enum 값과 필드를 검증한다. delegate·`UnityEngine.Object`·내부 서비스·가변 세션·임의 object payload는 넣지 않는다.

| 명령 | 입력 | 수락 조건 | 수락의 뜻 |
|---|---|---|---|
| `Start` | `TutorialKey`, `ExpectedContextId` | 사용 가능한 환경·같은 Context·알려진 정의·활성 세션 없음 | 그 요청 번호의 세션을 만들고 시작 절차에 들어갔다. 첫 표시 완료는 별개다 |
| `Continue` | `SessionId`, `ExpectedStepRevision` | 활성 세션·같은 revision·확인 준비된 설명(`CanContinue`) | 현재 설명 하나의 확인을 한 번 받았다. 다음 단계 진입·전체 완료와는 다르다 |
| `Cancel` | `SessionId` | 대상이 현재 Starting/Running 세션 | 외부 사유로 중단 정리를 시작했다. revision과 무관하다 |
| `ReportBlockedInput` | `SessionId`, `ExpectedStepRevision` | 활성 세션·같은 revision·현재 단계가 입력 제한 실습 | 제한 때문에 막힌 조작이 있었다. 정의된 안내를 잠시 표시한다. 단계·진행은 바뀌지 않는다 |

- Continue는 `CanContinue`가 true일 때 낸다. 외부가 CanContinue를 만들거나 StepKey를 지정해 강제 이동하지 않는다.
- 실습 단계는 Continue로 통과하지 않는다. 공개 게임 결과로만 통과한다(§8·§15.5).
- `ReportBlockedInput`은 전투 View가 `MoveOnly`로 조작을 막았을 때 입력 처리 안에서 TrySubmit만 한다(§15.10).
- **명령이 아닌 진행:** 5초 자동 진행, 설명 시작, 실습 통과, 전투 승패 중단, 화면 연결 해제는 실행 소유자와 TutorialPlan이 처리한다(§9·§10). 요청 번호·결과를 만들지 않는다.
- Start 처리 전에 중단하려면 Start 번호를 Cancel의 SessionId로 쓴다. FIFO라 Start가 먼저 처리된다. Start가 거절됐으면 Cancel은 `UnknownSession`이다. Start 부작용이 전혀 없는 사전 철회는 보장하지 않는다.
- Ending 세션에는 `SessionEnding`, 종료 세션에는 `SessionEnded`를 게시한다. Cancel을 여러 번 보내도 종료를 다시 하지 않는다.
- 형식은 맞아도 정의·환경·세션·준비 상태가 맞지 않으면 정상 Rejected 결과다.

### 6.1 늦은 입력과 연타

전용 입력 어댑터(§15.4.3)는 “다음”을 **튜토리얼 Layer에서 누른 경우만** 유효로 보고, **누른 순간 읽은 세션·revision**을 뗀 순간 Continue로 낸다. Layer 전환으로 오는 Canceled는 뗌이 아니다. Job 실행 때 최신 revision으로 바꿔 넣지 않는다.

Continue를 수락하면 그 단계는 다음 단계로 가는 전환(같은 StepKey·revision의 `WaitReason.Presentation`)에 들어가 `CanContinue`가 false가 된다. 같은 revision의 추가 Continue는 전환 중이면 `NotReady`, 다음 단계에 들어갔으면 `StepChanged`다. 두 단계를 연달아 넘기지 않는다.

Continue와 5초 만료가 겹치면 먼저 처리된 하나만 넘긴다. Plan은 같은 기회에 둘이 보이면 요청을 먼저 고른다(§9.1). 만료가 먼저면 늦은 Continue는 `NotReady` 또는 `StepChanged`다. Continue가 먼저면 남은 시간은 버린다.

## 7 요청 처리 결과 계약

`TutorialRequestResult`(불변):

| 필드 | 뜻 |
|---|---|
| `RequestNumber`, `CommandKind` | 어느 제출에 대한 결과인가 |
| `Disposition` | `Accepted` / `Rejected` / `Failed` |
| `SessionId` | Start Accepted면 만든 회차, 후속 명령이면 대상. 거절된 Start는 0 |
| `Reason` | 아래 사유 코드. 내부 예외 메시지를 프로토콜로 쓰지 않는다 |

- Accepted: `None`.
- Rejected: `Unavailable`, `ContextChanged`, `UnknownTutorial`, `Busy`, `UnknownSession`, `SessionEnding`, `SessionEnded`, `StepChanged`, `NotReady`.
- Failed: `ExecutionFault`, `RuntimeStopped`.

`NotReady`는 같은 revision이지만 지금 받을 수 없다는 뜻이다. Continue면 설명이 시작 조건을 기다리거나 표시·전환 중이거나 실습 단계다. ReportBlockedInput이면 현재 단계가 입력 제한 실습이 아니다(통과 뒤 유지 구간 포함, §15.10).

검사 순서(고정). 활성 세션은 하나라 존재하고 종료·종료 중이 아닌 세션이 곧 활성 세션이다.

1. Start: 환경 사용 가능 → Context 일치 → 정의 존재 → 활성 세션 없음.
2. 후속 명령: 세션 존재 → 종료/종료 중 여부.
3. Continue: revision 일치 → `CanContinue`.
4. ReportBlockedInput: revision 일치 → 현재 단계가 입력 제한 실습.

요청마다 결과는 하나다. `HandledRequestNumber`는 결과가 게시된 **연속된 마지막 번호**다. 숫자가 늘었다는 것만으로 성공을 판단하지 않는다. 발급된 번호 N에 대해 `TryGetResult(N)`이 false이고 `N > HandledRequestNumber`면 미처리다. `N <= HandledRequestNumber`인데 결과가 없으면 보존 계약 위반이다. 외부는 발급받았거나 상태에 공개된 번호만 조회한다.

## 8 세션 상태와 완료 계약

`TutorialSessionSnapshot`(불변):

| 필드 | 뜻 |
|---|---|
| `SessionId`, `ContextId`, `TutorialKey` | 어느 환경에서 시작한 어느 회차인가 |
| `Status` | `Starting`, `Running`, `Ending`, `Completed`, `Cancelled`, `Faulted`. `Starting`은 Start 수락부터 첫 단계 진입(StepRevision 1)까지 |
| `CurrentStepKey`, `StepRevision` | 현재 단계와 입력 유효성 |
| `StepKind` | `None`, `Explanation`, `Practice`. 첫 단계 전과 단계를 떠난 종료 정리 중에는 `None` |
| `WaitReason` | `None`, `StartCondition`, `Presentation`, `ContinueOrTimeout`, `PracticeGoal`, `Cleanup`. Job이 Runner를 점유한다는 뜻이 아니다 |
| `CanContinue` | 상태·단계에서 계산한 판단. 별도 가변 플래그로 저장하지 않는다 |
| `EndReason` | `None`, `Finished`, `ExternalCancelled`, `ContextDetached`, `BattleEnded`, `RuntimeStopped`, `ExecutionFault`, `CleanupFault` |

세션의 전투 기준점(`BattleInstanceId`·Start 때 결과 순번·실습 진입 때 결과 순번)은 Snapshot 내부 값이며 결과·세션과 같은 경계에서 설치한다.

| WaitReason | 기다리는 것 | 다음 사건 |
|---|---|---|
| `StartCondition` | 설명 구간의 첫 설명이 §8.1 조건을 기다린다. 입력을 빌리지 않은 상태 | 조건 충족 → 입력을 빌리고 표시 시작 |
| `Presentation` | 설명·안내 표시, 또는 Continue·만료 뒤 다음 단계로의 전환 | 표시 완료 → 설명이면 `ContinueOrTimeout`, 실습이면 `PracticeGoal` / 전환 완료 → 다음 단계 |
| `ContinueOrTimeout` | 표시된 설명이 확인 또는 창 활성 시간 5초 만료를 기다린다 | Continue 수락 또는 만료 → 전환. 마지막 단계면 Ending |
| `PracticeGoal` | 실습 단계의 통과 조건(게임 결과, §15.5) | 통과 → 다음 단계 |
| `Cleanup` | 종료 정리 | 정리 완료 → 최종 상태 |
| `None` | 없음. 다음 기회에 할 일이 있거나 최종 상태 | — |

설명의 남은 시간과 창 활성 여부는 공개하지 않는다. 원본은 실행 소유자다(§15.4.1).

### 8.1 설명 구간 시작 조건 — 유일한 정의

설명 구간은 연속된 설명 단계 묶음이다(1-1에서는 8~10, 13~17, 19~21). **구간의 첫 설명만** 아래 조건이 모두 맞을 때 시작한다. 구간 안의 다음 설명은 다시 확인하지 않는다(이미 입력을 빌린 상태). 정의는 `TutorialPlan.CanStartExplanation` 한 곳이다.

1. **사용자 입력 국면:** `ICombatStateBufferReadSink.Phase == CombatFlowPhase.PlayerInput`. 배치 단계도 같은 국면이다.
2. **남은 연출 없음:** `ICombatViewBufferReadSink.PresentedResultSequence >= ICombatStateBufferReadSink.LatestResultSequence`.
3. **처리 안 된 전투 명령 없음:** `ICombatViewBufferReadSink.TryGetNextCommand(ICombatStateBufferReadSink.HandledCommandNumber, out _)`가 false.
4. **이 세션 전투의 승패 없음:** 세션 기준점과 같은 전투(`BattleInstanceId`)의 `Completion`이 없다.
5. **다른 기능의 입력 독점 없음:** 현재 Layer가 설정 Layer가 아니다(한계는 §10).

조건 1~3은 전투 진행이 사용자 명령을 기다리며 스스로 진행하지 않는 상태와 같은 뜻이다. 판정 원본은 `CombatPlan.IsAwaitingPlayerCommand` 하나이고 `CanStartExplanation`이 그것을 부른다. 바뀐 것: 튜토리얼 안의 판정 사본과 “View 재생 중(`IsPlaybackBusy`)을 보지 않는다” 설명 → 사본 삭제·`IsPlaybackBusy` 삭제(2026-10-22 전투 View 구조 개선 Phase 4, 용근님, 뜻은 같음). 같은 뜻인지는 `TutorialPlanTests`의 회귀 시험으로 고정했다(§14.3). 그래서 설명이 떠 있는 동안 적 행동·자동 턴 진행이 일어나지 않고, 튜토리얼 입력이 새 명령을 막는다.

**빌리기 직전 재확인(2026-10-20 확정):** 설명 시작 Job은 입력을 빌리기 직전에 같은 Job 안에서 조건 1~5를 같은 함수로 다시 확인한다. 판단(실행 소유자 Tick)과 실행(다음 Runner Tick) 사이에 명령·연출·독점이 바뀔 수 있어서다. 맞지 않으면 바꾸지 않고 대기를 이어 간다.

알려진 한계: 포기 확인창이 열린 채로 설명이 시작될 수 있다(공개 사실이 없음). 설명이 끝나면 확인창이 그대로 남는다. 포기를 확정한 직후 설명이 잠깐 떠도 승패가 기록되면 Cancelled(BattleEnded)로 마감한다.

### 8.2 상태 전이

```text
Start Accepted → Starting → (첫 단계 진입) Running
Running(설명 구간 첫 설명, StartCondition) + §8.1 충족 → 입력 빌림 → Presentation → ContinueOrTimeout
Running(설명) + Continue Accepted 또는 5초 만료 → Presentation(전환) → 다음 단계
Running(실습, PracticeGoal) + 통과 조건 충족 → 다음 단계
마지막 설명의 Continue Accepted 또는 5초 만료 → Ending → Completed
Cancel Accepted / 전투 승패 확정 → Ending → Cancelled
화면 연결 해제 / 런타임 정지 → (Job 없이 바로 정리) → Cancelled
시작·실행·정리 실패 → Faulted (§8.3)
```

- Starting도 활성 세션이다. **Start Accepted가 표시 성공을 보장하지 않는다.**
- 설명 구간을 시작할 때 입력 Layer를 빌린다. 구간과 실습 사이 전환 순서는 §15.4.3. 다른 기능이 독점 중이면 빌리지 않고 `StartCondition`으로 기다린다. 설정창을 강제로 닫거나 독점을 빼앗지 않는다.
- Completed/Cancelled는 세션이 얻은 Layer·화면 막·UI 선택·UI·임시 표시의 정리가 끝난 뒤 게시한다.
- Ending 진입 때 종료 의도를 고정한다. 화면 해제·전투 승패가 Cancelled로 바꾸는 대상은 Starting/Running이다. Ending은 기존 의도를 유지하고 남은 자원을 정리한다. 늦은 Cancel이나 승패로 Completed 예정을 덮지 않는다.
- **화면 연결 해제는 Job 없이 마감한다(§10).** 그래서 다음 화면 진입의 Runner 검사와 재진입 Start의 `Busy`에 걸리지 않는다.
- **전투 승패:** 명령이 아니라 Plan이 `ICombatStateBufferReadSink.Completion`에서 읽는 사실이다. 세션 기준점과 같은 전투만 본다. 같은 기회에 실습 통과와 승패가 함께 보이면 승패를 먼저 처리해 Cancelled(BattleEnded)로 마감하고 통과·다음 설명을 진행하지 않는다. BattleEnded는 정상 완료가 아니다. 튜토리얼은 승패를 기록하거나 전투 종료를 미루지 않는다.
- **전투 기준점:** 승패·결과 기록은 다음 전투 설치 전까지 남으므로 기준점 이전이나 다른 전투의 기록으로 중단·통과하지 않는다.
- 최종 상태는 세션당 한 번이며 오래된 완료 통지로 다시 활성화하지 않는다.
- `CanContinue`는 `Running` + `Explanation` + `ContinueOrTimeout`일 때만 true다. Starting 중 중단은 Cancel을 쓴다.

### 8.3 실패 분류

| 경우 | 처리 |
|---|---|
| 빌리기 직전 재확인에서 다른 기능이 독점 중 | 실패가 아니다. `StartCondition`으로 계속 기다린다 |
| 재확인한 뒤에도 빌리기 실패(같은 프레임 경합) | Ending(ExecutionFault) → 정리 → Faulted. 런타임은 계속 돈다 |
| 늦은 표시 완료 통지, 지난 세션·화면의 통지 | 무시한다 |
| 튜토리얼 UI·씬 필수 참조 누락, 정의 데이터 오류 | 런타임 오류 경계에 보고한다. UI 규약은 필수 참조가 없으면 예외 |
| 빌린 Layer·화면 막·UI 선택 복원 실패 | 세션 Faulted(CleanupFault) + 오류 경계 보고. `CombatViewSystem.ReleaseInputOrReport`와 같은 방침 |
| 상태 Store 불변식 위반·계약 위반 | 오류 경계에 보고한다 |

Job 안의 예외는 Bootstrap이 오류 경계에 보고하고 런타임을 멈춘다. 세션 실패로 끝낼 경우만 Job이 잡아 게시한다. 모든 `InvalidOperationException`을 입력 점유 실패로 보지 않는다.

## 9 Job 실행·누락 복구 계약

### 9.1 TutorialPlan과 Job — 기존 Plan·Job 방식

`Register(Tutorial, Next)`는 **다음 처리 기회를 요청**한다. Continue를 만들거나 단계를 강제로 진전시키지 않는다. 호출자는 Plan·Job을 직접 만들지 않는다. 구조는 `CombatPlan`과 같다.

- **TutorialPlan:** `LifeCycles/Tutorial/Plans/`의 상태 없는 static 클래스. `JobRegisterSystem.Initialize`의 `_plans[JobRootType.Tutorial] = (service, jobType) => TutorialPlan.Register(service, jobType, _buffers)` 한 줄로 등록한다. `None`은 빈 목록, `Next`는 판단, 그 밖은 인자 예외.
- **사실 묶음(`TutorialFacts`, 실행 시점에 한 번 읽음)**
  - 튜토리얼 상태 Store·요청 Buffer와 전투 공개 Sink(`ICombatStateBufferReadSink`, `ICombatResultHistoryReadSink`, `ICombatViewBufferReadSink`)는 `_buffers`에서 읽는다.
  - 단계 순서·종류·실습 통과 조건은 상태 Store가 초기화 때 받은 단계 규칙(`TutorialStepRule`, 정의에서 만든 불변 값)에서 읽는다. Plan은 정의 표를 읽지 않는다(2026-10-20 사용자).
  - 실행 소유자의 내부 사실(만료, 창 활성, 표시·정리 완료 통지, Context, 입력 독점)과 실행 창구(입력 빌리기·돌려주기, UI 개폐)는 `service.ViewGroup.TutorialExecution`(`ITutorialViewExecution`)으로 얻는다. 선례는 `RecoveryPlan`의 `service.ViewGroup.RecoveryExecution`이다. 실행 소유자가 없으면 Next에서 예외다.
- **Decide(사실) — 판단의 유일한 원본.** 위에서부터 처음 맞는 줄 하나.
  0. 활성 세션도 미처리 요청도 없음 → 없음
  1. 활성 세션 전투의 승패 확정(기준점과 같은 전투) → 종료(BattleEnded)
  2. Ending이고 정리 완료 통지 있음 → 최종 게시
  3. 미처리 요청 N(번호순) → 요청 처리(N)
  4. Starting → 첫 단계 진입(다음 대기 전환)
  5. 설명: 표시·전환 완료 통지 → 다음 대기로 / `ContinueOrTimeout`이고 남은 시간 0 → 만료 진행 / `StartCondition`이고 §8.1 충족 → 설명 시작
  6. 실습: 안내 표시 완료 통지 → `PracticeGoal` / 통과 조건 충족 → 다음 단계
  7. 그 밖 → 없음
- 4와 6의 첫 줄은 §8의 WaitReason 표를 판단 줄로 적은 것이며 “다음 대기 전환” Job이 맡는다(2026-10-20 확정). 설명 표시와 닫기 전환이 같은 revision이라, 실행 소유자는 (세션, revision)마다 마지막으로 요청한 표시의 종류와 완료를 함께 답한다.
- **할 일이 없으면 Job을 만들지 않는다**(`JobType.Next` 공용 계약, `CombatPlan`과 같은 빈 목록). 기다림은 Decide가 “없음”을 돌려주는 것이다.
- **굶주림이 없다:** 내부 진행은 한 방향이고, 요청은 유한하며 Job 하나가 하나씩 소비한다.
- 화면 연결 해제와 런타임 정지는 Decide를 거치지 않고 실행 소유자가 마감한다(§10).
- **Job 종류(7):** 요청 처리(N), 종료(세션, 사유), 최종 게시(세션), 설명 시작(세션, revision), 다음 대기 전환(세션, revision), 만료 진행(세션, revision), 실습 통과(세션, revision). 각 Job은 한 걸음만 하고 실행할 때 (세션, revision) 또는 다음 미처리 번호를 다시 확인한다. 낡은 등록이면 바꾸지 않고 끝내고, 불변식이 깨지면 예외다. 우선순위 0이며 같은 우선순위는 등록 순서를 지켜 전투 이탈 연쇄 사이에 끼어들지 않는다.
- 세션 설치 규칙(revision +1, Ending 의도 고정, Completed는 Ending 경유, 새 세션은 받아들인 Start로만, 결과-요청 관계)은 상태 Store 한 곳이 막는다. Plan·Job은 어떤 전이를 할지만 정한다.

### 9.2 요청 선택과 게시

- 요청 Buffer는 번호가 고정된 불변 큐다. 덮어쓰는 `CurrentRequest`를 읽는 방식은 쓰지 않는다.
- 결과, `HandledRequestNumber`, 그 요청의 세션 상태 변경은 내부 게시 경계에서 함께 설치한다. 설치 전 검증 실패로 부분 상태를 노출하지 않는다.
- 처리한 입력의 물리적 제거는 소유자만 한다. 제거가 늦어도 처리 번호 때문에 재실행하지 않는다.
- 실행 오류 뒤 부작용을 자동 재시도하지 않는다. 튜토리얼은 전투 명령을 제출하지 않는다(§15.5).

### 9.3 트리거와 등록

- **실행 소유자 Tick:** 오류 상태(`IsFaulted`)가 아니고 로딩 중(`IsLoading`)이 아니며 Runner가 비었으면 조건 없이 `Register(Tutorial, Next)`를 부른다. 할 일 판단은 Plan만 한다. `CombatLifeCycle`·`StageFlow`와 같은 가드다.
- **Tick 순서:** Runner(Global) → 튜토리얼 실행 소유자(InGame) → 전투 LifeCycle. Plan이 할 일이 없으면 Runner가 빈 채로 남아 같은 프레임에 전투가 자기 Next를 등록한다. 튜토리얼 Job은 할 일이 있을 때만 전투를 한 프레임 늦춘다.
- 트리거를 LifeCycle·Flow가 아니라 View 쪽 런타임 수명 소유자에 두는 것은 새 형태다. 튜토리얼은 특정 LifeCycle에 속하지 않고 씬보다 오래 사는 마감이 필요해서다(§10).
- 외부의 TrySubmit 뒤 Register는 처리를 앞당길 뿐이다. 빠뜨리거나 실패해도 요청은 보관되고 소유자 Tick이 다시 등록한다. 같은 내용을 다시 내서 새 번호를 만들지 않는다.
- 입력 콜백 안에서는 Register를 부르지 않는다(입력 콜백 예외는 Framework가 잡지 않음).
- 정상 조립·활성 런타임에서 복구한다. 미등록 루트·바인딩 실패·종료된 런타임 같은 구성 오류를 고친다는 보장은 없고 외부 등록 예외를 숨기지 않는다.
- 다른 기능이 Runner를 오래 점유하면 튜토리얼도 늦어질 수 있다.
- 공용 `Runner.Clear()`로 튜토리얼만 취소하는 구현은 금지한다.

## 10 입력·화면 수명과 상태 소유권

- 전용 입력의 “다음”(§15.4.3)은 내부 입력 어댑터다. Continue를 TrySubmit만 하고, Register를 부르지 않으며, 예외는 잡아 오류 경계에 보고한다(`CombatInputReceiver`와 같은 형태). 세션 상태를 직접 바꾸지 않는다.
- Controller·Layer는 Configuration 생성 때 조립하고 실행 중 다시 Configure하지 않는다. 튜토리얼 UI 버튼은 `UIInputBinding`·명시적 receiver 규약을 따른다.
- 설명 구간 동안 빌린 Layer, 화면 막, 저장한 UI 선택은 세션 소유 자원이다. 구간이 끝날 때, 그리고 중단·화면 이탈·오류·런타임 정지에서 반드시 돌려준다.
- **화면 연결(Attach):** ContextId를 게시하고 창 활성 알림 컴포넌트를 구독한다. 외부 작업자는 별도 lease를 보관하지 않는다.
- **화면 해제(Detach)는 Job 없이 바로 마감한다:** Layer 반환, 화면 막·UI 닫기, 선택 복원, 알림 구독 해제. Starting/Running은 Cancelled(ContextDetached), Ending은 고정된 의도로 최종 게시한다. 마감이 실패해도 연결 해제는 한다.
- 실행 소유자는 런타임 수명이고 표시 Context와 씬 Scope는 더 짧다. 씬 Scope와 함께 실행 소유자를 없애는 조립은 이 계약에 맞지 않는다.
- 표시·자원 완료 콜백과 창 활성 알림은 사실만 기록한다. 단계·종료 결정은 다음 기회에 Plan이 한다. 세대가 다른 콜백은 무시한다.
- 오류 때는 `InGameViewFaultCleanup`에 연결한 정리 콜백이 빌린 Layer·화면 막을 돌려준다(입력·표시만, 세션은 Shutdown이 마감).
- 런타임 정지 때는 Shutdown 경계에서 활성 세션의 자원을 정리하고, 미처리 요청은 Failed(RuntimeStopped), Starting/Running은 Cancelled(RuntimeStopped), Ending은 고정된 의도로 마감한다.
- **입력 독점 판정의 한계:** §8.1 조건 5는 현재 Layer가 설정 Layer인지로 판정한다. InputSystem에 독점 여부를 묻는 공개 API가 없고 독점 사용처가 옵션창 하나뿐이라서다(2026-10-19 사용자). 다른 기능이 독점을 쓰기 시작하면 Framework 작업자에게 조회 API를 요청한다.

| 사실 | 단일 원본 |
|---|---|
| 명령 데이터·발급 번호 | 튜토리얼 요청 Buffer |
| 처리 결과·마지막 처리 번호 | 튜토리얼 결과 Buffer |
| 회차·Context·단계·대기 이유·종료 사실·전투 기준점 | 튜토리얼 상태 Buffer의 내부 Store |
| 이동 실습 입력 제한(`MoveOnly`) | 상태 Store에서 계산해 공개. 전투 입력 검사는 읽기만 함(§15.10) |
| 다음 할 일 판단, 설명 구간 시작 조건 | `TutorialPlan` 한 곳(`Decide`, `CanStartExplanation`). 조건 1~3은 `CombatPlan.IsAwaitingPlayerCommand` |
| 설명의 남은 시간·창 활성 여부 | 실행 소유자. 공개하지 않음 |
| 문구·단계 순서·종류·실습 통과 조건·입력 제한·덮개 색 | 정의 데이터(View DB 표, §15.11). 상태 Store는 정의에서 만든 단계 규칙을 초기화 때 받아 보관 |
| 실제 표시 재생·빌린 Layer·화면 막·저장한 UI 선택 | 내부 표시·입력 실행 객체. 공개 상태는 그 완료 사실을 반영 |
| 설명 기준 대상·선호 방향 / 공통 크기·여백·막힌 안내 시간 / 현재 화면 좌표 | View 정의 / 튜토리얼 프리팹 / 표시 계층의 계산. 좌표를 정의·Buffer에 저장하지 않음(§15.13) |
| 임시로 연 카드 목록의 정리 의무 | 튜토리얼 표시 계층. 자기가 연 현재 인스턴스만, 외부 닫힘을 보면 의무를 버림(§15.13.2) |
| 전투 진입·국면·명령 처리 번호·이동 결과·연출 완료·승패 | 전투 Buffer. 튜토리얼은 읽기만 함 |

## 11 결과 보존과 조회 수명

- **같은 런타임 동안 모든 요청 결과와 종료 세션을 보존**한다. 씬 이탈·새 Run·다음 튜토리얼 시작으로 지우지 않는다.
- 여러 소비자가 독립적으로 조회한다. 외부 acknowledge·Clear·Remove API는 없다.
- 런타임 종료 뒤 저장·복원은 보장하지 않는다.
- 보존은 요청 수에 비례한 메모리를 쓴다. 조회 의미를 분명히 하는 쪽을 골랐다. 용량 제한이 필요하면 조회 불가 사유와 보존 범위를 공개 계약 변경으로 설계한다. 적정 용량을 검증했다고 주장하지 않는다.

## 12 외부 소비자의 사용 예

공개 Sink, 값 DTO, 공용 JobRegister만 쓴다.

### 12.1 시작하고 완료를 확인

1. 상태 Sink에서 `Context`를 읽는다. `IsAvailable`이 false면 공개 사유로 요청 시점을 정한다.
2. `AvailableDefinitions`에서 `TutorialKey`를 고른다.
3. `Start(TutorialKey, ExpectedContextId)`를 TrySubmit한다. 실패하면 `submitFailure`를 처리하고 등록하지 않는다.
4. 번호를 보관하고 `Register(Tutorial, Next)`를 부른다.
5. 결과 Sink에서 그 번호를 조회한다. Rejected/Failed면 Reason으로 처리하고, Accepted면 `SessionId`를 쓴다.
6. 상태 Sink에서 세션이 진행 중인지 최종인지 읽는다. **Start Accepted만 보고 완료 처리하지 않는다.** Cancelled면 EndReason으로 이유를 구분한다(`BattleEnded`는 정상 완료가 아님).

1-1에서는 실행 소유자가 이 절차로 Start를 낸다(§15.12).

### 12.2 계속·취소

1. 대상 세션 Snapshot을 읽는다.
2. 확인은 `CanContinue`가 true일 때 `Continue(SessionId, StepRevision)`를 낸다(1-1에서는 전용 입력이 함).
3. 화면 밖 사유로 중단하려면 `Cancel(SessionId)`를 낸다.
4. 성공한 제출마다 `Register(Tutorial, Next)`하고 결과를 확인한다.
5. Cancel Accepted 뒤 세션이 Ending이면 정리 중이다. 최종 상태까지 지켜본다.

### 12.3 진행 상태와 입력 제한 읽기

- `StepKind`와 `WaitReason`으로 무엇을 기다리는지 안다. 예: `Practice` + `PracticeGoal` = 사용자의 게임 조작 결과 대기, `Explanation` + `StartCondition` = 전투가 사용자 입력을 기다릴 때 대기.
- 전투 View는 입력 검사마다 `CombatInputRestriction`을 읽는다. `MoveOnly`면 허용 밖 조작을 막고, 실제로 막은 시도에 활성 세션의 `SessionId`·`StepRevision`으로 `ReportBlockedInput`을 TrySubmit한다(§15.10).
- 실습을 외부에서 통과시키거나, 시작 조건을 바꾸거나, 5초를 줄이는 API는 없다.

외부가 JobRunner의 `IsIdle`을 세션 완료로 읽거나, UI의 `activeSelf`·내부 요청 상태를 읽거나, 콜백 인수를 넘길 필요가 없어야 한다.

## 13 조립 경계와 공유 대상

| 위치 | 연결 |
|---|---|
| `BufferSystems/Tutorial/` | 공개 DTO·Sink와 내부 요청/결과/상태 Store. 외부 쓰기는 명령 제출만 |
| `BufferSystems/Core/BufferSystemGroup`·`IBufferQuery`, `BufferSystemFactory`, `ServiceKeys/Runtime/Buffer/TutorialBufferKey` | 튜토리얼 Sink 속성과 조립. Buffer별 키를 루트에 공개하지 않음 |
| `LifeCycles/Tutorial/` | `TutorialPlan`과 Job들(§9.1) |
| `JobRootType`·`JobRegisterSystem` | `JobRootType.Tutorial = 6`, `Initialize`의 Plan 등록 한 줄 |
| `ViewSystem/Tutorial/`·`ViewSystem/TutorialInput/` | 실행 소유자·입력 어댑터·표시·창 활성 알림. 협력 객체를 모두 Framework 서비스로 등록하지 않음 |
| `IInGameViewSystemGroupService`·`InGameViewSystemGroup`, `InGameViewSystemFactory` | 실행 창구 속성(`TutorialExecution`), 생성·Export, 튜토리얼 입력 결합 |
| `InGameViewFaultCleanup` | 오류 때 Layer·화면 막을 돌려주는 정리 콜백 |
| `PlayerInputActions.inputactions`, `InputRegistrationSet` | `Tutorial` map·Layer·Controller를 Configure 전에 조립. **에셋과 코드는 한 커밋**(§15.4.3) |
| `UIIDs`(`UIId.Tutorial = 31`), 튜토리얼 프리팹·Combat 씬 | Parallel 정책, Canvas 정렬 110, 씬에는 프리팹 인스턴스 하나. `CombatSceneRoot`는 바꾸지 않음 |
| View DB 표 코드·`DataBaseSO/ViewGroup/TutorialDefinition/`·`ViewGroup.asset`, `ViewDataBase`·`ViewTableKey`·`ViewDataBaseFactory` | 정의 표(§15.11). **코드와 에셋은 한 커밋**(없으면 DB 생성 실패) |
| 전투 View(`CombatInputPolicy` 등) | 전투 작업자 동의 뒤 튜토리얼 작업에서 직접 수정(§15.10), 수정 뒤 알림 |

튜토리얼 입력(map·Layer·Controller·조립)과 UI(UIId 추가·기존 프리팹 재사용·Canvas 정렬)는 기존 구조 기준을 따르는 추가라 따로 공유하지 않는다(2026-10-19 사용자). 그 밖의 공용 파일(`JobRootType`·`JobRegisterSystem`, Buffer·View 그룹과 Factory, 서비스 키, `ViewDataBaseFactory`)은 `CLAUDE.md` 규칙대로 Framework 작업자에게 공유한다(공유 메모는 T-8, 아직 없음 — 계획 §2). 이 설계 때문에 Core, FieldCamera, Architecture, 전투 진입 요청, 전투 진행(`CombatPlan`), `BattleChangerUI`, `CombatSceneRoot`, `ProjectSettings`를 바꾸지 않는다. 실습에 Core 사실이 부족하면 Core 전달 문서에 요청한다.

## 14 설계 원칙 점검과 검증 기준

### 14.1 SSOT·SOLID·JMH·FW

- **SSOT:** §10 표의 원본을 지킨다. 판단과 시작 조건은 `TutorialPlan` 한 곳이고 실행 소유자 Tick은 판단하지 않는다. 이동 실습 제한은 튜토리얼이 소유하고 전투 입력 검사는 읽기만 한다.
- **SRP:** Buffer는 데이터, Plan은 판단, Job은 한 걸음, 실행 소유자는 수명·트리거·자원, 표시 객체는 UI, Factory는 조립.
- **OCP:** 새 튜토리얼 내용은 정의와 기능 내부에서 더한다. 실습 조건·입력 제한은 정의 데이터가 정하고, 새 조건 종류는 실제 요구가 생기면 설계한다.
- **LSP:** 할 일이 없으면 Job을 만들지 않고 만든 Job은 한 걸음으로 끝난다. 조회는 비파괴다.
- **ISP/DIP:** 소비자에게는 필요한 Sink만 준다. 전투 View는 제한 값과 명령 제출만 쓴다.
- **JMH:** 기능·수명·조립 분리, Plan은 상태 없는 static, 대기는 Plan 조건. Build 뒤 구조 변경·전역 검색·해제 불가능 구독·미래용 범용 Signal 계층을 더하지 않는다.
- **FW:** FW-001/002/003(인터페이스 등록, Tick/Shutdown 모듈 등록, Builder 1회 Build), FW-004~007(공개 상태 경계, Configure 중 바인딩, 입력 Submit 경로, Layer는 공개 `OverrideLayer`만), FW-008/009/016·FW-101/103(UIBase·UISystem 등록, Parallel, 좁은 의존, 명시적 자원 해제).

### 14.2 기능 변경 독립성

| 변경 | 바뀌는 범위 | 함께 바뀌면 안 되는 범위 |
|---|---|---|
| 문구·단계 순서 | 정의와 내용 검증 | 외부의 시작·완료 확인 코드, 공용 JobRegister |
| UI 애니메이션 교체 | 표시 구현·표시 완료/정리 검증 | Buffer DTO·게임 계산 |
| 시작 위치 추가 | 그 소비자의 공개 Sink/Job 사용·조립 | 기존 진행 내부 |
| 이동 실습 제한 변경 | 정의의 제한 값, 제한 종류가 늘면 전투 View 검사 | 전투 진행·Core, 튜토리얼 밖 전투 입력 |
| 새 외부 명령 | 공개 명령/결과 계약·실행·소비자·계약 회귀 | 무관한 전투·상점 내부 |
| 공통 취소·정리 계약 | 상태·수명과 영향받는 입력·표시 통합 검증 | 무관한 게임 규칙 회귀를 관성으로 더하지 않음 |

기준 원본: [프로젝트 적용 원칙 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리), [JMH 지침](../framework/Framework%20Development%20Guidelines.md), [FW 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md).

### 14.3 구현 뒤 확인할 계약

T-8이 아래와 §15.8 사례를 시험 이름 또는 재생 증거로 채운다(계획 §2).

1. 공개 Sink·JobRegister만 쓰는 외부 소비자가 시작·상태·최종 결과를 확인한다. 내부 참조가 없다.
2. FIFO, 중복 Next, 명령 없는 Next에서 할 일이 없으면 Job이 없다. 같은 revision의 연타와 Continue·만료 겹침이 한 번만 진행한다. Decide가 §9.1 순서로 고른다.
3. 제출 뒤 등록 누락·실패에도 번호가 유지되고 소유자 Tick이 복구한다. 오류·로딩 중에는 등록하지 않는다.
4. 정상 거절(UnknownTutorial·Busy·ContextChanged·StepChanged·NotReady 등)과 실행 오류를 구분한다.
5. Start Accepted·Continue Accepted·Job 완료·세션 Completed가 서로 다른 사실이다.
6. Cancel, 화면 이탈(Job 없이 마감), 전투 승패, 시작·정리 오류, 지난 통지가 새 세션을 바꾸지 않는다. 화면 이탈 직후 로비·스테이지 Runner 검사와 재진입 Start가 실패하지 않는다.
7. 여러 소비자의 비파괴 조회와 다음 세션·새 Run 뒤 보존.
8. 튜토리얼 Layer에서 누른 “다음”만 Continue가 되고 Layer 전환 취소는 뗌이 아니다. 화면 막이 설정·가방 버튼을 막고 Enter·Space로 선택된 버튼이 눌리지 않는다. 설정창이 열려 있으면 설명을 시작하지 않고 예외가 없다.
9. 튜토리얼 대기 중 다른 Job과 튜토리얼 취소를 처리할 수 있다. `Runner.Clear`를 쓰지 않는다.
10. §8.1 조건 1~3이 `CombatPlan.IsAwaitingPlayerCommand`와 같은 뜻이다. `StartCondition` 대기 중에는 입력을 빌리지 않는다. 실습의 Continue는 `NotReady`다. 실습 통과와 승패가 함께면 Cancelled(BattleEnded)다. 이전 전투 기록으로 중단·통과하지 않는다.
11. 이동 실습 중과 통과 뒤 19 표시 전까지 `MoveOnly`이고(17 → 실습 전환 순간에도 틈 없음), 허용 밖 조작이 막히며, 실습 중 실제 시도에는 안내가 뜬다. 턴 종료·재진입 예외는 §15.10의 지금 정책대로다.

## 15 실제 튜토리얼 진행·UI·입력 기획

### 15.1 기획 출처

- 기획 원본: Dropbox `Project L/기획 브레인스토밍/안재령의고뇌/튜토리얼 기획서.pptx`(21장, 슬라이드 번호는 1부터). 본문·발표자 노트·이미지를 읽기 전용으로 확인했다(부록 A).
- 슬라이드 내용을 바탕으로 하되 진행 순서와 문구는 이 절의 사용자 확정을 따른다. 1~3은 표지·목차·공통 정책, 4~7은 로비·맵·전투 UI 참고이며 게임 안 튜토리얼은 8부터다.
- 2026-10-19 단순화(§15.9)와 검토 뒤 결정(기존 Plan·Job 방식, 시작 조건 단일 정의, Canvas 100번대·선택 비우기, 설정창 화면 기준 멈춤, “이동만 허용”)으로 다시 적은 판이다.

### 15.2 전체 흐름

튜토리얼 전용 스테이지를 만들지 않고 **1-1 스테이지에서 실제 배치와 전투를 진행**한다. 1-1은 **1장에서 처음 들어가는 전투**다(§15.12).

- **실행 시점(2026-11-11 바뀜):** 1-1에 진입할 때 실행하되, **이번 게임 실행에서 그 정의를 마지막 단계까지 마친(Completed) 회차가 있으면 실행하지 않는다**(사용자: 패배·포기 뒤 새 Run의 1-1에서는 안 나오게). 기록은 §11의 보존된 종료 세션에서 계산하며(`ITutorialStateBufferReadSink.HasCompleted`) 따로 저장하지 않는다. 그래서 게임을 다시 실행하면 다시 나온다. 패배·포기로 중간에 끊긴 회차(Cancelled·Faulted)는 세지 않아 다음 1-1에서 처음부터 다시 실행한다. 처음 판(2026-10-19 사용자 확정)은 "1-1 진입마다 실행, 이전 완료 기록·보존 세션으로 막지 않음"이었다.
- **중도 이탈 뒤 재진입(사용자 확정):** 8번 설명부터 새 회차로 시작한다. 이전 단계·남은 시간을 이어 쓰지 않고 8번의 대기도 표시 시점부터 5초다. 이전 회차는 화면 해제 때 Job 없이 마감한다.

| 순서 | 원본 슬라이드 | 진행과 다음 단계 조건 |
|---|---|---|
| 1 | 8~10 | 1-1 진입 뒤 시작 조건(§8.1)이 맞으면 배치 설명을 차례로 보인다. 설명마다 5초 뒤 자동 진행, 그 전에 클릭하면 즉시 진행. 10이 끝나면 배치 단계 |
| 2 | 10의 버튼 안내 / 12는 구분 표시 | 사용자가 직접 배치하고 기존 전투 진입 버튼(`BattleChanger`) 또는 G키로 시작한다. 배치 검사·확인·제출·거절은 기존 기능 그대로(§15.3). 튜토리얼은 상단 배치 안내만 보이고 **실제 전투 진입 성공**을 기다린다 |
| 3 | 13~17 | 전투 진입 성공 뒤 **처음 맞는 시작 조건**에서 13부터 승리·패배 조건, 현재 행동 유닛, 드래그 이동과 행동 코스트를 설명한다. 적이 먼저 행동하면 그 행동은 13 전에 기존대로 재생된다. 17이 끝나면 이동 실습 |
| 4 | 18은 구분 표시 | 이동 실습은 **처음 움직일 수 있는 아군 차례**에 한 번만 한다. **이동만 허용**(§15.10). 이동 가능한 칸으로 한 칸 이상 실제 이동하고 **그 연출이 끝나면** 통과한다. 부분 이동도 통과하며 목적지는 지정하지 않는다 |
| 5 | 19~21 | 통과 뒤 처음 맞는 시작 조건에서 19부터 공격 모드 전환, 대상 클릭 공격, 남은 행동 코스트를 설명한다. 21이 끝나면 안내·덮개를 닫고 입력을 돌려줘 완료한다. 공격 실습·공격 성공 검사는 없다 |
| 6 | 완료 뒤 | 사용자가 일반 입력으로 같은 1-1 전투를 이어 직접 클리어한다. 밤이 끝나 다시 배치·전투를 해도 이동 실습을 다시 하지 않는다. 클리어는 완료의 선행 조건이 아니다 |

- 12·18의 본문(“여기서 드래그 했다” 등)은 진행 구분 표시라 설명 화면으로 띄우지 않는다. 11은 순차 설명이 아니며 그 안내·확인창은 일반 전투 기능이 띄운다(§15.3).
- 실습 도중 승패가 먼저 정해지면 남은 순서보다 중단 정책(§15.4)을 우선한다.

### 15.3 배치 단계의 전투 진입

**사용자 확정(2026-10-19 변경):** 튜토리얼은 전투 진입 입력·배치 검사·확인창을 만들지 않는다. 사용자는 기존 전투 진입 버튼 또는 G키로 시작하고, 튜토리얼은 상단 배치 안내를 유지한 채 공개 전투 Buffer에서 **실제 전투 진입 성공**(§15.5)을 확인한 뒤 넘어간다. 버튼 클릭·확인창 선택·Job 완료로 성공을 대신하지 않는다.

같은 날 UI 작업자(현강님)가 슬라이드 11의 동작을 일반 전투 기능 [CombatDeploymentConfirmation](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatDeploymentConfirmation.cs)([설계 기록](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatDeploymentConfirmation.md))으로 넣었고 튜토리얼은 그대로 쓴다.

| 전투 진입 입력 때 상태 | 기존 동작 |
|---|---|
| 메인 캐릭터 미배치 또는 사망 | `CombatActionNotice`의 “메인 캐릭터가 배치되지 않았습니다” 안내. 명령·확인창 없음 |
| 메인 배치, 다른 편성 캐릭터 일부 미배치 | 전역 `NoticePannel`(`UIId.Notice`)의 예/아니요 확인창. 예 → 다시 검사 뒤 시작 요청, 아니요 → 배치 계속(기본 선택 아니요) |
| 편성 캐릭터 모두 배치 | 확인 없이 시작 요청 |

- 확인창이 열린 동안의 필드·덱·가방 입력 막기는 Layer가 아니라 전투 입력의 “확인창 열림” 검사라 튜토리얼 Layer·시작 조건 5와 겹치지 않는다(2026-10-19 코드 확인).
- 확인창은 전역 UI(`GlobalUICanvas`, 정렬 5·99)다. 배치 단계에는 튜토리얼 화면 막이 없어 눌린다. 설명 중에는 전투 진입 입력이 막혀 확인창이 열리지 않는다.
- 최종 진입 규칙은 Core가 검증한다([CombatTurnCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs)의 `EnterCombat` 등). 튜토리얼은 복제하지 않는다.
- 8이 뜨기 전 짧은 틈에는 메인 캐릭터가 아직 필드에 없어 G키·버튼을 눌러도 메인 미배치 안내만 뜬다. 실제 전투가 8보다 먼저 시작되지 않는다.
- 전투 진입 버튼은 `Combat.unity`의 `Canvas/BattleChanger`([BattleChangerUI](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/BattleChangerUI.cs), `UIId.BattleChanger`, Parallel)다. 클릭과 G키는 같은 요청 경로로 합류해 배치 확인을 거친 뒤 전투 입력 Buffer로 제출된다. 클릭 가능 여부는 `CombatInputPolicy.CanSubmit(EnterCombat)`이다(2026-10-18 확인).

### 15.4 UI 기반과 화면 가림

- 안내 박스와 덮개는 **UI 작업자가 만든 기존 UI**(공용 `NoticePannel` 외형)를 기반으로 한다.
- 설명 대상은 보이게 두고 그 밖을 검정 덮개로 가린다. 대상 강조 효과는 필수가 아니다.
- **덮개 색(2026-10-20 사용자 정정):** “약 95% 투명으로 말고 5% 투명으로 해주세요.” → **검정, 투명도 5%·불투명도 95%**, Unity 값 `Color(0, 0, 0, 0.95f)`(근삿값 `#000000F2`). 바뀐 것: 알파 13/255(`#0000000D`) → 0.95. 실행 색의 원본은 View 정의 에셋이고 프리팹 초기색·신규 기본값도 맞춘다.
- **실습 중 덮개(사용자 확정):** 배치 단계·이동 실습으로 넘어가면 덮개를 닫고 상단의 짧은 실습 안내만 남긴다. 다음 설명 구간에서 다시 덮는다.
- 덮개 표시와 입력 제한은 따로 설계한다. 설명 단계에서는 게임 조작을 막고 설명을 넘기는 입력만 허용한다(§15.4.3).
- **ESC(사용자 확정):** 설명 중에는 ESC·설정 버튼으로 설정창을 열 수 없다. 배치 단계·이동 실습 중에는 열 수 있다. ESC를 설명 넘기기로 쓰지 않는다.
- **설정창 개방 중(2026-10-19 사용자 확정):** [OptionUI](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/OptionUI.cs)가 `Time.timeScale = 0`·오디오 정지를 걸어 **화면 기준 모든 진행이 멈춘다.** Job은 시간 배율과 무관해 연출 없는 계산 한 걸음은 진행될 수 있지만 결과는 닫은 뒤 재생돼 화면 차이가 없다. 튜토리얼은 전투 정지를 만들지 않고 설정창이 열려 있는 동안 다음 설명 구간을 시작하지 않는다(§8.1 조건 5). 닫으면 그대로 이어 간다.
- **설명 진행(사용자 확정):** 8~10, 13~17, 19~21의 각 설명은 표시 뒤 **5초가 지나면 자동으로**, 그 전에 **클릭하면 즉시** 다음으로 간다. 다음 설명은 표시 때부터 다시 5초다. 클릭과 자동 진행이 겹쳐도 한 번만 끝내고, 같은 클릭을 다음 설명·게임 조작에 넘기지 않는다.
- **창 비활성화(사용자 확정):** 타이머를 멈추고 남은 시간을 보존한다. 돌아오면 남은 시간부터 이어 간다(§15.4.1). 2026-10-19 변경으로 `runInBackground`는 바꾸지 않는다(§15.4.2). 바뀐 것: “실습 중 창 밖에서도 적 행동·자동 턴 진행 계속”, “창 밖에서 진입·이동이 끝나면 복귀 뒤 13·19 표시” → 철회(§15.9).
- **전체 건너뛰기(사용자 확정):** 없음.
- **사용자가 직접 하는 단계:** 배치 단계와 드래그 이동 실습 둘. 배치는 기존 입력 그대로, 이동 실습은 “이동만 허용”(§15.10).
- **실습 안내:** 해야 할 조작을 짧은 문구로 **통과할 때까지** 계속 보인다. 5초·클릭으로 없애거나 통과시키지 않는다. 통과·중단 때 닫는다. 기존 텍스트 박스를 **화면 상단 중앙에 고정**하고 HUD·카드·유닛을 가리지 않는다(배치는 §15.13.4).
  - 배치 안내: **“메인 캐릭터를 포함해 유닛을 배치한 뒤, 전투 진입 버튼을 누르세요.”**
  - 이동 실습 안내: **“현재 행동 유닛을 드래그해 한 칸 이상 이동하세요.”**
  - 막힌 조작 안내(2026-10-19): **“먼저 유닛을 이동하세요.”** 시도 판정은 §15.10, 표시는 §15.13.4.
- **이동 실습 입력:** 이동만 허용한다. 허용 범위·버튼·턴 종료 예외는 §15.10이 원본이다. 통과 뒤에도 **19번 설명이 실제로 뜰 때까지** 제한을 유지하며 이 사이에는 안내를 띄우지 않는다.
- **이동 뒤 공격 안내:** 한 칸 이상 실제 이동하고 그 연출이 끝나면 통과하고, 그 뒤 처음 맞는 시작 조건에서 19를 보인다. 부분 이동도 통과, 실제 이동이 없으면 통과하지 않는다. 연출이 끝나기 전에 19나 그 타이머를 시작하지 않는다.
- **공격:** 19~21 뒤 별도 공격 실습은 없다. 진행을 공격 성공에 걸지 않는다.
- **완료:** 21이 5초 또는 클릭으로 끝나면 안내·덮개를 닫고 입력을 돌려주며, 자원 정리 뒤 `Completed`를 게시한다. 이후 다시 배치·전투를 해도 튜토리얼은 진행하지 않는다. 전투 클리어까지 완료를 미루거나 별도 전투를 만들지 않는다.
- **실습 도중 승패:** 튜토리얼을 중단하고 기존 승리·패배 처리를 진행한다. 안내·덮개를 닫고 빌린 입력을 돌려준다. 같은 결과에 이동 성공과 승패가 함께 있어도 중단을 우선하며 정상 완료로 기록하지 않는다. 중단 뒤 늦은 이동 연출 완료로 설명을 다시 열지 않는다.
- **설명 시작 조건과 전투(2026-10-19):** 설명은 §8.1 조건에서만 시작한다. 그동안 전투는 사용자 명령을 기다리므로 튜토리얼은 전투 정지 장치를 만들지 않는다. 대가로 진입 직후 적의 선행 행동은 13 전에 재생된다. 같은 구간 안에서는 입력을 계속 빌린다. 설명 중에도 5초 타이머·클릭·Job·필수 정리는 돈다. 런타임·Runner·`Time.timeScale`을 멈추는 것으로 대신하지 않는다.
- **튜토리얼 UI 위치(2026-10-19):** **튜토리얼 전용 프리팹**을 **Combat 씬에 하나** 둔다. Canvas는 Screen Space Overlay, **정렬 100번대**(구현 110). 안의 박스는 UI 작업자의 기존 프리팹을 쓴다. [UISystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/UISystem.cs)이 씬 연결 때 찾아 등록한다. `GlobalUICanvas`에는 넣지 않는다. Overlay Canvas는 정렬 순서로만 위아래가 정해지므로 Combat 씬의 튜토리얼 Canvas가 전역 UI 버튼을 막을 수 있다.
- 튜토리얼 UI는 `UIBase`·UISystem에 등록하고 Parallel 정책을 쓴다. 버튼이 있으면 `UIInputBinding`·명시적 receiver를 쓴다. UI가 입력 장치를 직접 읽거나 진행 상태를 따로 소유하지 않는다.

#### 15.4.1 창 비활성화 중 5초 타이머

1. 튜토리얼 UI에 붙은 튜토리얼 컴포넌트(`TutorialViewFocusNotifier`)가 `OnApplicationFocus(bool)`를 받는다. 실행 소유자가 Attach 때 구독하고 Detach 때 끊는다. 비활성 오브젝트는 알림을 못 받으므로 설명 표시를 시작할 때마다 `Application.isFocused`를 다시 읽는다. 근거: [Unity 6 OnApplicationFocus](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnApplicationFocus.html).
2. **남은 시간의 원본은 실행 소유자 한 곳**(`TutorialViewTimer`)이다. 활성일 때 지난 시간만 깎고 비활성이면 멈춘다. UI가 따로 세지 않는다.
3. 시간 배율 없는 실제 시간으로 센다. 복귀 때 측정 기준만 지금으로 다시 잡아 창 밖 시간을 빼고, 남은 값은 그대로 둔다. 타이머를 위해 `Time.timeScale`이나 Runner를 멈추지 않는다.
4. 0이 되면 Plan이 다음 기회에 만료 진행 Job을 고른다(§9.1). 만료를 Continue로 위장하지 않고, revision으로 클릭과 겹쳐도 한 번만 처리한다.

[CombatSceneRoot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneRoot.cs)의 `FocusLost`(비활성화만 알림)는 바꾸지 않는다(2026-10-19 변경, §15.9).

#### 15.4.2 백그라운드 실행 — 바꾸지 않음

[ProjectSettings.asset](../../../ProjectSettings/ProjectSettings.asset)의 `runInBackground`는 0이다([Application.runInBackground](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-runInBackground.html)). 2026-10-19 결정으로 바꾸지 않는다. 대상 PC 빌드에서 창을 벗어나면 게임 전체가 멈춘다. 에디터 재생은 다를 수 있어 타이머는 §15.4.1의 알림 기준으로 멈춘다.

#### 15.4.3 튜토리얼 전용 입력 — 2026-10-19

**사용자 확정:** 설명 구간에서는 **튜토리얼 전용 입력**을 쓰고 그 밖에는 각 기능의 입력(배치·전투·설정)을 쓴다.

- **구성:** Input Actions에 `Tutorial` map과 액션 `Next`(Button, `Press(behavior=1)` = ReleaseOnly, `<Mouse>/leftButton`) 하나를 두고, 이 map만 담은 `Tutorial` Layer를 등록한다(이름 2026-10-20 사용자). 기존 배치·전투 Layer는 공통 map(`Common`, ESC 설정창)을 함께 담지만([StageInputLayers](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageInputLayers.cs)) 튜토리얼 Layer는 공통·배치·전투 map을 담지 않는다. 그래서 설명 중에는 G키·드래그·턴 종료·공격·ESC가 들어오지 않는다.
- **빌리고 돌려주기:** 설명 구간 시작 때 `OverrideLayer`로 빌리고(옵션창과 같은 방식) 끝날 때 돌려준다. InputSystem은 빌린 동안의 Layer 변경 요청을 기억했다가 돌려줄 때 적용하므로, 빌린 사이 배치 → 전투로 바뀌어도 돌려준 뒤 그 Layer로 간다.
- **전환 순서:** 설명 → 이동 실습(17 → 실습)은 한 Job 안에서 ① 실습 단계에 들어가 이동만 허용을 켜고 ② Layer를 돌려준다. 실습 → 다음 설명(통과 → 19)은 ① 통과 뒤에도 이동만 허용을 유지하고 ② 19의 시작 조건이 맞으면 Layer를 빌려 표시를 시작하며 그때 제한이 풀린다. 제한이 없는 전환(10 → 배치, 21 → 완료)은 Layer만 돌려준다.
- **다음 입력:** §6.1과 같다. Unity 동작(Input System 1.19.0 `PressInteraction`): 누르면 Started, 떼면 Performed 뒤 Canceled, 누른 채 map이 꺼지면 Canceled만 온다. Button 액션은 처음 상태를 읽지 않아 map이 켜지기 전부터 누르던 버튼은 뗄 때 아무것도 오지 않는다.
- **화면 버튼:** 전투 진입·턴 종료·공격·복귀 버튼은 전투 입력 검사가 Layer를 보므로 튜토리얼 Layer에서 이미 막힌다. 설정·가방 버튼은 Layer를 거치지 않는 `GlobalUICanvas`(정렬 5·99, 현강님 999→99 변경 병합 확인 2026-10-19)에 있어, 설명 중에는 **정렬 100번대 Canvas**에 강조 구멍 위까지 덮는 투명한 전체 화면 막을 둔다. 막은 클릭을 막기만 한다. 막만으로는 드래그·키가 막히지 않으므로([CombatInputRouter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatInputRouter.cs)) Layer와 함께 쓴다.
- **키보드로 눌리는 버튼:** Unity UI의 Enter·Space는 Layer와 막을 우회해 선택된 버튼을 누른다. 설명 구간 시작 때 EventSystem 선택을 저장하고 비우며 끝날 때 되돌린다(옵션창과 같은 조건: 선택이 비어 있고 대상이 살아 켜져 있을 때만).
- **설정창과의 순서:** Layer 빌리기는 겹칠 수 없다. 설정창이 열려 있으면 설명 구간을 시작하지 않고 기다린다. 튜토리얼이 설정창을 닫지 않는다.
- **정리:** 중단·씬 이탈·오류·정지 때 Layer·막·선택을 반드시 돌려준다. 화면 이탈 때는 Job 없이.
- **조립:** Controller·Layer는 입력 설정 조립 때 기존 목록과 함께 등록한다. [InputConfigurationValidator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/InputSystem/UnityBridge/InputConfigurationValidator.cs)는 Layer가 가리키는 map이 에셋에 없으면 실패하므로 **에셋과 코드는 한 커밋**이다. 따로 공유하지 않는다(2026-10-19 사용자).

### 15.5 Buffer·Job 사용 원칙과 판정식

사용자는 **Buffer와 Job을 쓰는 구조를 유지할 것**을 재확인했다(§9). 실습이 더해졌다는 이유로 System 직접 호출이나 UI 콜백 중심 진행으로 바꾸지 않는다.

| 경계 | 원칙 |
|---|---|
| 튜토리얼 명령·진행 | §4~§12의 요청 Buffer → `Tutorial/Next` → 결과·상태 Buffer |
| 전투 진입·드래그·공격·턴 종료 | 기존 입력·전투 명령 Buffer·전투 Job 경로 그대로. 튜토리얼은 전투 명령을 내지 않고 Core 내부를 부르거나 결과를 대신 쓰지 않는다. 이동 실습 제한은 전투 입력 검사가 공개 값을 읽어 적용한다(§15.10) |
| 실제 전투 진입 성공 | 세션 기준점 뒤 결과(`ICombatResultHistoryReadSink.Results`) 중 `CommandKind == EnterCombat`이고 `Accepted`인 결과가 있으면 성공. 연출 완료는 13의 시작 조건이 확인한다 |
| 이동 실습 통과 | 실습 진입 때 결과 순번 뒤, 세션 기준점과 같은 전투의 결과 중 플레이어 명령 처리(`Kind == Command`)이고 `CommandKind == Move`, `Accepted`, `PathMovement.EnteredCells.Count >= 1`인 결과가 있고, `ICombatViewBufferReadSink.PresentedResultSequence`가 그 순번 이상이면 통과. 움직인 유닛은 **이동 결과가 났을 때의** 유닛이다(2026-10-20 확정). Core가 받은 플레이어 이동이 곧 그 유닛의 이동이라서다. 실습 진입 때 유닛으로 고정하지 않는 것은 다음 아군 차례로 넘어간 경우에도 통과해야 해서다. 적 이동·목적지 도착은 보지 않는다 |
| 설명 확인과 실습 통과 | `Continue`는 설명 확인이다. 실습은 게임 결과로만 통과(실습의 Continue는 `NotReady`). 5초 자동 진행은 내부 진행이며 공개 상태는 `ContinueOrTimeout` |
| 창 활성·안내 시간 | §15.4.1. 만료에 따른 단계 변경은 Plan·Job 경계를 따른다 |
| 설명 중 입력 | §15.4.3. 기존 배치·전투 입력의 Controller·Router·Gate와 전투 진입 버튼은 바꾸지 않는다 |
| 전투 진행 정지 | 만들지 않는다(§15.4). Runner 점유·정지·Clear, `Time.timeScale` 변경으로 구현하지 않는다 |
| 실습 도중 승패 | `ICombatStateBufferReadSink.Completion`을 같은 전투에 대해서만 보고 중단·정리를 시작한다. 승패를 대신 기록하거나 목적지를 정하지 않는다. 공개 상태는 Cancelled(`BattleEnded`) |
| 결과 소비·재생 확인 | 튜토리얼은 읽기 관찰자다. 결과 소비·삭제·`AcknowledgePresented`는 기존 소유자에게 남긴다 |
| 사용자·전투 결과 대기 | Runner를 점유하지 않는다. 무엇을 기다리는지는 `StepKind`·`WaitReason`, 기다림은 Plan의 “없음”(§9.1) |

튜토리얼이 읽는 전투 사실은 모두 공개 전투 Buffer에 있다. 부족해지면 Core 규칙을 복제하지 않고 Core 전달 절차로 요청한다.

### 15.6 설계 원칙 적용

- **SSOT:** 기획 순서·문구는 §15와 정의, 공개 계약은 §4~§12. 판단과 시작 조건은 `TutorialPlan`. 배치·전투 결과는 전투 Buffer, 회차·단계·기준점은 상태 Store. 메인 캐릭터 배치 규칙은 Core 한 곳.
- **SRP:** 판단(Plan), 한 걸음(Job), 수명·트리거·자원(실행 소유자), 표시, 설명 중 입력, 조립을 변경 이유별로 나눈다. 공용 JobRegister·Factory에 튜토리얼 분기를 넣지 않는다.
- **OCP·LSP·ISP/DIP·JMH·FW:** §14.1과 같다.
- **기능 변경 독립성:** 문구는 정의, 덮개는 표시·레이아웃, 실습 조건은 결과 관찰·단계 검증에 머문다. 튜토리얼은 Core·전투 진행·`BattleChangerUI`·`CombatSceneRoot`·프로젝트 설정·FieldCamera·Architecture를 바꾸지 않는다. 전투 입력 검사에는 제한을 읽는 추가를 전투 작업자 동의 아래 넣는다(§15.10).

### 15.7 기획 확인 상태

현재 범위에서 더 물을 기획 질문은 없다(2026-10-20). 막힌 조작 문구는 2026-10-19, 카드 강조·대상 옆 설명·상단 띠·버튼 비활성은 2026-10-20에 확정했다. 확정 항목을 다시 질문으로 돌리지 않는다. 새 Core 규칙 뒤의 문구 재검토는 §16이다.

### 15.8 확인할 사례(T-8)

- 완료·중도 이탈 뒤 다음 1-1 진입에서 8부터 새 회차. 이전 회차는 화면 해제 때 마감돼 새 Start가 `Busy`가 아니다. 8의 대기는 새 표시 시점부터 5초다.
- 1장 1층이 상점·이벤트면 그 뒤 첫 전투에서 시작. 1장 두 번째 전투·2장 이후·0층 시작 칸에서는 시작하지 않는다.
- 1-1 진입 직후 설정창이 열려 있으면 8은 닫힐 때까지 기다리고 예외가 없다.
- 8~10 중 G키·배치 드래그·ESC가 막히고, 화면 막으로 전투 진입·설정·가방 버튼이 안 눌리며, Enter·Space로 선택된 버튼이 안 눌린다. 10 뒤 배치 입력과 UI 선택이 돌아온다.
- 배치: 메인 없이 진입하면 미배치 안내만, 메인만 배치하면 확인창(아니요면 배치 계속·안내 유지, 예면 시작), 전원 배치면 바로 시작. 실제 진입 성공 뒤 13 대기. 8 전 짧은 틈에도 시작되지 않는다.
- 첫 행동자가 적이면 적 행동 재생 뒤 13. 처리 안 된 명령이 있으면 기다린다. 13~17 동안 적 행동·자동 턴 진행이 없다.
- “다음”은 튜토리얼 Layer에서 누른 것만, 누른 순간 revision으로 한 번. 클릭·5초 겹침에도 두 설명을 넘기지 않는다. 17·21을 닫는 클릭이 드래그·공격으로 이어지지 않는다.
- 이동 실습: 1칸 이상 실제 이동(부분 이동 포함)의 연출 완료 뒤 통과. 드래그 취소·이동 거절·0칸은 통과하지 않는다. 연출 중 19와 그 타이머를 시작하지 않는다.
- 이동 실습 중 공격 전환·턴 종료 버튼 상태, 키로 들어온 금지 시도의 “먼저 유닛을 이동하세요.” 안내, ESC 설정창은 §15.10의 지금 정책대로다(수동 턴 종료·배치 국면 재진입 회복 포함).
- 17 → 실습 전환 순간과 통과 뒤 19 전까지 공격이 실행되지 않는다. 유지 구간의 막힌 조작에는 안내가 없다.
- 실습 중 설정창이 열려 있으면 화면 기준 멈추고, 설정창 뒤에서 19를 보이거나 타이머를 시작하지 않는다. Layer 빌리기가 설정창과 겹쳐 예외가 나지 않는다.
- 설명 중 ESC는 설정창·설명 넘기기가 아니며 타이머·클릭은 계속 돈다. 배치·이동 실습 중에는 ESC로 설정창이 열린다.
- 실습 중 승패가 먼저면 안내·덮개를 닫고 입력을 돌려준 뒤 기존 승패 처리. 정상 완료로 바꾸지 않고, 늦은 연출 완료가 설명을 다시 열지 않는다. 이전 전투 기록으로 중단·통과하지 않는다. 이후 로비·스테이지 Runner 검사가 실패하지 않는다.
- 실습 안내는 확정 문구, 5초·클릭으로 사라지지 않고 통과 때 닫힌다. 상단 중앙 고정, HUD·카드·유닛을 가리지 않는다.
- 2초 센 뒤 창을 오래 벗어나도 복귀하면 남은 3초부터. 여러 번 오가도 중복 차감 없음. 이전 회차 타이머를 가져오지 않는다.
- 대상 PC 빌드에서 창을 벗어나면 게임 전체가 멈추고, `runInBackground`는 그대로다.
- 전체 건너뛰기 없음. 설명을 넘기는 클릭이 이동 실습 통과로 해석되지 않는다.
- 공격 설명 뒤 공격 성공 대기가 없다.
- 21 종료 → 안내·덮개 닫기·입력 반환·제한 해제 → 정리 뒤 `Completed` → 같은 전투 계속. 밤 뒤 다시 배치·전투해도 이동 실습이 없다.
- 할 일이 없을 때 Job을 만들지 않아 전투가 늦어지지 않는다.
- 취소·씬 이탈·오류 정리에서 Layer·막·선택·안내·임시 카드 목록이 남지 않는다.
- 늦은 입력·과거 전투 결과·UI 표시 여부가 현재 단계 완료를 대신하지 않는다.

### 15.9 2026-10-19 단순화 결정과 다른 영역 의존

**사용자 확정(2026-10-19):** 다른 작업자 코드·Core·프로젝트 전역 설정을 가능한 한 바꾸지 않도록 아래를 바꿨고, 설명 중 입력은 튜토리얼 전용 입력으로 막는다.

| 항목 | 이전 확정(철회) | 바뀐 결정 | 없어진 다른 영역 변경 |
|---|---|---|---|
| 1. 설명 시점·전투 정지 | 진입 성공 즉시 13, 설명 중 적 행동·자동 턴 진행 정지 | §8.1 조건에서만 시작, 전투 정지 장치 없음, 설정창은 화면 기준 멈춤 | 전투 진행(`CombatPlan`) 수정. 대가: 진입 직후 적의 선행 행동은 13 전에 재생 |
| 2. 배치 검사·확인창 | 튜토리얼이 미배치 안내(5초·클릭)·일부 미배치 확인창을 끼움 | 만들지 않음. 같은 날 현강님이 같은 동작을 일반 전투에 넣어 그대로 씀(§15.3) | `BattleChangerUI`·전투 진입 경로 수정 |
| 3. 이동 실습 입력 | 공격·턴 종료 차단 + 갈 칸 없으면 튜토리얼이 자동 턴 종료 → (1차 단순화) 막지 않음 | **(재변경) 이동만 허용.** 자동 턴 종료 없음(지금 예외 정책은 §15.10) | Core 사실 요청, 미리보기 칸 충돌, 튜토리얼의 턴 종료 제출. **남는 것: 전투 입력 검사 수정(전투 작업자 동의)** |
| 4. 창 밖 진행 | 실습 중 창 밖에서도 진행, 복귀 뒤 13·19 표시 | `runInBackground` 유지, 복귀 표시 규칙 철회, 타이머는 튜토리얼 컴포넌트 알림 | 전역 설정, `CombatSceneRoot` 창 복귀 알림 추가 |

남는 다른 영역 변경과 공유 처리는 §13·§17이다. 1장 1층을 늘 전투로 고정하는 안은 협의 후보로 남는다(§15.12).

### 15.10 전투 View 수정 — 이동 실습 “이동만 허용”(전투 작업자 동의)

**사용자 결정(2026-10-19):** 이동 실습 중에는 이동만 허용한다. **전투 작업자 협의 결과(2026-10-19): 튜토리얼 작업에서 전투 View 코드를 직접 고쳐도 된다.** 고친 뒤 변경 내용을 알린다([T-7 전달 메모](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md), 미전달).

**왜 필요한가:** `CombatInputPolicy`는 이동·턴 종료·미리보기 모두 현재 Layer가 전투 Layer인지 먼저 본다. 튜토리얼 Layer를 켜면 이동까지 막히고, 전투 Layer를 켜면 턴 종료·공격도 통과한다. 이동 기능을 튜토리얼에 복사하면 SSOT 위반이라, 전투 입력 검사가 튜토리얼의 제한 값을 읽는다.

**튜토리얼이 주는 것**

- `ITutorialStateBufferReadSink.CombatInputRestriction`: 이동 실습 중과 통과 뒤 19가 실제로 뜨기 전까지 `MoveOnly`(§5).
- `ReportBlockedInput(SessionId, ExpectedStepRevision)`(§6): 막힌 시도를 알리면 “먼저 유닛을 이동하세요.”를 잠시 보인다. 통과 뒤 유지 구간에서는 이미 이동했으므로 `NotReady`로 거절하고 안내하지 않는다.

**전투 View가 하는 것**

1. 명령·미리보기 공통 검사가 `MoveOnly`일 때 이동 경로 지정·이동 명령·이동 미리보기(와 아래 예외)만 허용하고, 공격·아이템 등 그 밖의 명령과 모드 전환을 막는다. 버튼과 키가 모두 이 검사를 지나 한 곳에서 처리된다.
2. 제한 때문에 막은 **실제 시도**만 그때 읽은 활성 세션·revision으로 `ReportBlockedInput`을 TrySubmit한다. 입력 처리 안이라 Register는 부르지 않는다. 비활성 버튼 클릭·호버·버튼 상태 갱신·미리보기 검사·최종 Gate 재검사는 알림을 만들지 않는다.
3. 차단 검사는 이동 모드 종료·경로 취소·공격 모드 전환보다 먼저 한다. 막힌 시도 때문에 편집 중 경로를 잃지 않는다.
4. 제한 값은 검사마다 읽고 사본을 두지 않는다. `None`이면 지금과 같은 동작이다.

**버튼 정책(2026-10-20 후속 사용자 지시):** 이동 실습의 **공격 전환·복귀·턴 종료 버튼은 비활성화**한다. 버튼 표시와 실제 명령·모드 검사에 같은 판단을 쓰고, 기존 전투 조건(처리·연출 대기, 코스트 등)의 비활성 사유도 그대로 적용한다. 숨겨진 아이템 UI를 안내용으로 노출하지 않는다. 바뀐 것: “막힌 버튼을 눌러 안내” → 버튼 비활성, 안내는 키 등 Router에 도착한 시도만.

**턴 종료·재진입 예외(지금 정책):** 바뀐 것: 현재 행동 유닛을 잡았을 때 받은 **수락된 최신 이동 범위가 비었을 때만** 그 차례에 턴 종료 허용(2026-10-19, 10-20 재확인) → **MoveOnly에서도 평상시 조건을 통과한 수동 플레이어 턴 종료를 허용**하고(Q-V32 A), 이동 없이 밤이 끝나 배치 국면이 되면 **같은 전투의 미완료 실습(Running·Practice·PracticeGoal 대기)일 때만 정상 조건의 전투 진입을 허용**한다(Q-V38 A). 2026-10-26 전투 View 4라운드 V4-g(용근님 구현, 사용자 결정)로 새 Core 규칙(플레이어 턴·공유 행동 코스트)에 맞췄다. 원본은 [4라운드 설계 §7](2026-10-25-combat-view-stage-round4-design.md#7-이동-실습의-회복-경로-v4-g)이다. 실습 통과는 여전히 수락된 플레이어 이동의 표시 완료뿐이고, 턴 종료·재진입·적 이동으로 통과하지 않는다. 통과 뒤 19 대기 구간은 재진입 예외를 받지 않는다. 옛 빈 범위 추적 API는 후속 정리 부채로 남았다.

**지금 코드:** 전투 View 2라운드(2026-10-24, V2-f)부터 제한 읽기·막힘 보고는 `Combat/Input/Assist/CombatBlockedInputReport` 한 곳이 두 Sink를 갖고, Router는 알림 대리자 하나를 받는다. 새 Core(2026-10-24 `d4a6d6fd`)에서 복귀(체크포인트) 기능이 폐지돼 복귀 버튼·키는 숨김·무동작이다.

**바뀌지 않는 것:** 전투 진행(`CombatPlan`), Core, 튜토리얼 밖 전투 입력, 설정창(ESC) 열기.

### 15.11 정의 데이터 — View DB 표

**사용자 결정(2026-10-19):** 튜토리얼 내용은 코드가 아니라 **View DB 표**로 둔다. DB 규칙 P2(SO가 기획 입력, `GetData()`가 순수 Data)·P5(ViewGroup은 Unity 타입 허용, 키로 조회)를 따르며 `ThornFieldVisual`·`CombatPresentationSettings` 표와 같은 형태다.

| 담는 값 | 내용 |
|---|---|
| 정의 | 튜토리얼 키(공개 정의 목록과 같은 키), 이름, 대상 맵 키(1장 = 1, §15.12), 자동 진행 초(5), 덮개 색(검정·알파 0.95) |
| 단계 목록(목록 순서 = 단계 순서) | 단계 키, 종류(설명/실습), 설명 문구, 덮개에서 보일 대상 목록, 설명 기준 대상·선호 방향(§15.13.3), 실습 통과 조건(전투 진입 성공 / 한 칸 이상 이동), 입력 제한(없음 / 이동만), 실습 안내 문구, 막힌 조작 안내 문구 |

**세부 형식(2026-10-20 사용자 확정)**

- **설명 구간은 저장하지 않는다.** 이어진 설명 단계 묶음이 한 구간이고, 구간의 첫 단계는 앞 단계가 설명이 아닌 설명 단계다. 계산할 수 있으므로 적지 않는다(DB 규칙 P7). 판정 도우미는 `TutorialDefinitionRules`(`Find`·`NextAfter`·`StartsExplanationSegment`) 하나를 Plan과 Store가 같이 쓴다.
- **단계 키 = 기획서 슬라이드 번호.** 실습은 구분 슬라이드 번호(배치 12, 이동 18). 1-1 단계는 `[8,9,10,12,13,14,15,16,17,18,19,20,21]` 13개다.
- 종류·제한·통과 조건은 상태 Buffer의 enum(`TutorialStepKind`, `TutorialCombatInputRestriction`, `TutorialPracticeGoal`)을 그대로 쓴다. 그 사이 규칙은 `TutorialStepRule` 한 곳이다. 정의는 규칙을 만들어(`CreateInfo`·`CreateRules`) 검사하고, 실행 소유자가 초기화 때 Store에 넘긴다(통과 조건을 규칙에 둔 것은 2026-10-20 사용자).
- **검사 규칙:** 통과 조건과 제한은 끝낼 수 있는 조합만(전투 진입+없음, 이동+없음, 이동+이동만). 막힌 조작 문구는 제한이 있을 때만. 종류에 맞지 않는 칸은 비운다(공백도 거절). 한 맵에는 정의 하나(`TryGetByTargetMap`).
- 설명 문구는 슬라이드 8~10, 13~17, 19~21 본문이 원본이다. 예: 8 “카드를 드래그 하여 필드에 배치할 수 있습니다.”
- **보일 대상**은 enum `TutorialFocusTarget`(None 밖 11가지: `Hand`·`MainCharacterCard`·`EnterCombatButton`·`EnemyNexus`·`MainCharacterUnit`·`MainCharacterHealth`·`CurrentActorUnit`·`ActionCost`·`AttackButton`·`EnemyUnits`·`UnitInfoActionCosts`)로 적고 표시 코드가 화면 위치로 바꾼다(§15.13.1). 단계별 대상은 슬라이드의 덮개 구멍 위치로 정했다.

| 단계 | 보일 대상 |
|---|---|
| 8 | Hand |
| 9 | MainCharacterCard |
| 10 | EnterCombatButton |
| 13 | EnemyNexus |
| 14 | MainCharacterUnit, MainCharacterHealth |
| 15 | CurrentActorUnit |
| 16 | CurrentActorUnit, ActionCost |
| 17 | ActionCost |
| 19 | CurrentActorUnit, AttackButton |
| 20 | CurrentActorUnit, EnemyUnits, ActionCost |
| 21 | ActionCost, UnitInfoActionCosts |

  16·20의 행동 코스트는 슬라이드 그림에서 초록 코스트 칸이 덮개 위에 그려져 있어 넣었다. 기획 의도가 아니면 에셋 값만 빼면 된다.
- 정의 데이터는 런타임 상태를 저장하지 않는다. 표를 더하면 `ViewGroup.asset`·카탈로그에 등록해야 하고 `DataBaseFactory.Create`는 표가 모두 있어야 성공하므로 코드와 에셋은 한 커밋이다. DB 값을 바꾸면 `DataBaseAssetTests`를 돌린다.

### 15.12 1-1 판정과 Start 제출

**사용자 결정(2026-10-19):** “1-1”은 **1장에서 처음 들어가는 전투**다.

**이유(코드 확인):** 1장 맵은 매 Run 무작위다(`StageMap_Chapter1`의 `randomizeSeedEachRun`). 0층은 스테이지 없는 시작 칸, 1층부터 마지막 앞 층은 일반 스테이지(전투·상점·이벤트) 중 무작위, 마지막 층은 보스다([ChapterGraphGenerator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/StageMap/ChapterGraphGenerator.cs)). 그래서 고정 스테이지 키가 없고 1층이 전투가 아닐 수 있다. 일반 전투의 승리 조건은 모두 넥서스 파괴라 13은 어느 첫 전투에서도 맞다.

**판정(모두 공개 Buffer, `TutorialViewTarget`):**

1. 현재 맵이 정의의 대상 맵이다: `ICurrentProgressBufferReadSink.CurrentMapKey`.
2. 현재 칸이 전투다: `CurrentStage.NodeId`의 `IStageProgressBufferReadSink` 칸이 `StageType.Combat`.
3. 이번 장에서 클리어한 전투가 없다: 같은 맵의 `StageType.Combat`이고 `IsClear`인 칸이 없다.
4. 전투가 설치돼 있다(없으면 대상이 아님 — 기준점 0으로 Start를 받는 것을 막음).
5. 이번 게임 실행에서 그 정의를 끝까지 마친 회차가 없다: `ITutorialStateBufferReadSink.HasCompleted(정의 키)`가 거짓(2026-11-11 추가, 아니면 `AlreadyCompleted`).

- 1층이 상점·이벤트면 그 뒤 처음 들어가는 전투에서 실행한다. 두 번째 전투부터는 실행하지 않는다. 패배·포기 뒤 새 Run의 1장 첫 전투에서는 **앞 회차를 끝까지 마쳤으면 실행하지 않고**, 중간에 끊겼으면 다시 실행한다(2026-11-11 바뀜, 처음 판은 늘 다시 실행).
- 대상 맵 키는 정의 데이터에 두고 코드에 박지 않는다.

**Start 제출(2026-10-19 사용자 확정):** 실행 소유자가 Combat 화면 Attach 때 위 판정을 하고 맞으면 자기 요청 Buffer로 `Start(TutorialKey, ContextId)`를 TrySubmit한다. 등록은 소유자 Tick이 한다. 전투 기준점은 Start를 받아들이는 Job이 세션과 함께 설치한다.

**채택하지 않은 대안 — 0층 시작 칸을 튜토리얼 전투로:** Core 맵 생성·규칙, 장 시작 흐름, DB, 스테이지 맵 화면을 모두 바꿔야 한다. 슬라이드 3(“튜토리얼 스테이지는 따로 개발하지 않으며 1-1 스테이지에서 진행”)과 다르고 1장이 전투 한 번만큼 길어진다.

**협의 후보(미결) — 1장 1층을 늘 전투로 고정:**

- 기획자: 1장 1층에 상점·이벤트가 안 나와도 되는지.
- Core 작업자: 1층에서는 전투만 뽑도록 맵 생성 변경(`ChapterGraphGenerator`·`ChapterGraphSpec`·`StageGeneratorSystem`, `ChapterGraphGeneratorTests` 기대값 일부). Core 전달 문서에 요청으로 적는다.
- DB 담당: 1장에만 적용하려면 `StageMapData`에 맵별 설정값.
- 튜토리얼은 바꿀 것이 없다. 필드까지 매번 같게 하려면 “1층 = 특정 전투 하나”로 정한다.

### 15.13 UI 보완 설계 — 기획 원본 대조 후 확정(2026-10-20)

**사용자 확정:** 현재 덱 카드 강조, 대상 옆 설명, 상단 실습 안내로 보완한다. 설명·실습 순서, 5초·클릭, 넓은 강조 영역, 실습 판정은 유지한다. 11은 일반 배치 확인, 12·18은 구분 표시이며 새 설명 화면이나 공격 실습을 만들지 않는다. 대조 근거는 부록 A.

#### 15.13.1 실제 표시 대상

- `Hand`는 **화면에 표시된 유지 덱의 `UnitCardUI` 카드 영역 합집합**이다. `MainCharacterCard`는 Character Buffer의 Main 보유 기록과 `UnitCardUI.OwnedCharacterId`가 일치하는 카드다. Entity ID·화면 중앙 집기·슬롯 번호로 추측하지 않는다. 옛 `HandPresenter`·`HandCardHoverView`는 조회하지 않는다. 정상 진입과 개발 직행이 같은 카드 UI를 강조한다.
- **`AttackButton`(2026-10-20 사용자 요청):** 행동 패널의 **Attack 버튼과 그 자식 그래픽만**. 복귀·턴 종료 버튼은 덮개 아래에 남긴다. `CombatActionPanel`이 기존 공격 버튼 참조를 읽기 전용으로 주고 대상 조회가 하위 그래픽 영역을 구한다. 이름 검색·중복 참조·형제 숨기기는 쓰지 않는다. 설명 박스가 피할 영역은 행동 패널 전체다.
- **`MainCharacterHealth`(2026-10-20 사용자 요청):** `MainCharactorUI.healthBarRoot` 아래 **체력 아이콘과 그 옆 수치만**(`MainCharactorUI.HealthBarTransform` 읽기 전용). 얼굴·시너지·골드는 덮개 아래. 설명 박스 회피 영역은 상단 HUD 전체.
- `ActionCost`는 행동 유닛 HUD 전체, 유닛·넥서스는 켜진 Renderer의 화면 투영 영역, `EnemyUnits`는 표시된 적 하나다.
- **`UnitInfoActionCosts`(2026-10-20 사용자 요청, 값 11):** 21에서 `ActionCost`와 함께 하단 `CombatUnitInfo`의 이동 아이콘·마름모 배경·숫자와 공격 아이콘·마름모 배경·숫자만 각각 밝힌다(정보창·Cost 행 전체 아님). `CombatUnitInfo`에 두 하위 영역(`moveCostRoot`·`attackCostRoot`)을 명시적으로 연결해 읽기 전용으로 주고, 회전한 마름모를 위해 네 모서리 합집합으로 구한다.
- **19 행동 버튼 패널 표시(2026-10-20 사용자 요청):** 실제 전투 단계에서 설명 Layer를 빌린 동안에도 행동 버튼 패널을 보인다. 전투 View의 표시 값이 보임과 입력 가능을 나눠 공격·턴 종료·복귀는 비활성이다. 실제 전투 여부는 `CombatInputPolicy.GetDesiredLayer`를 다시 쓴다. 배치 단계·설정 Layer·입력 해제·제출 정지에는 이 예외가 없다. 단계 번호를 전투 코드에 넣거나 튜토리얼이 버튼을 직접 켜지 않는다.
- **`CurrentActorUnit`(확인 필요):** 새 Core(2026-10-24)에는 “현재 행동 유닛”이 없어 천명님이 이 대상의 조회를 필드 메인 캐릭터로 바꿨다(`TutorialViewFocusLocator.ActiveActor`, `d4a6d6fd`). 의미 재정의는 §16이다.
- 필수 UI·씬 연결 누락은 설정 오류다. 화면에 잠시 없는 대상에는 구멍을 만들지 않으며 다른 카드를 메인 카드로 대신 강조하지 않는다.

#### 15.13.2 개발 직행 카드 목록의 임시 개방과 정리

일반 진입은 기존 전투 UI가 카드 목록을 열지만 개발 직행은 닫혀 있을 수 있다(`GameBootstrap`의 Combat 직행은 자동 열기를 생략). 튜토리얼은 첫 카드 설명 전에 `IUISystemService.OpenChildUI(UIId.Deck, UIId.UnitCard)`로 **UnitCard 자식만** 연다(`TutorialViewCardVisibility`). Deck 부모는 기존 Combat 조립이 열므로 활성 부모를 확인만 하고 열거나 닫지 않는다. Bootstrap·덱 구현을 고치거나 카드를 복제하지 않는다.

1. 로딩 뒤 첫 카드 설명 직전에 실제 `DeckCardList` 인스턴스의 활성 상태를 읽는다. 이미 열려 있으면 그대로 쓰고 정리 소유권을 갖지 않는다.
2. 닫혀 있을 때만 한 번 열고 그 인스턴스의 정리 의무를 기록한다. 8 → 9 → 10 → 배치 실습까지 유지하며 다시 열지 않는다.
3. 배치 실습(`PracticeGoal.EnterCombat`)의 통과 전환, 취소·오류·Close·Detach에서 자기가 연 목록만 `CloseUI(UIId.UnitCard)`로 정리한다. 여러 번 정리해도 무해하다.
4. **외부에서 먼저 닫으면 정리 의무를 즉시 버린다.** 인스턴스 수명 관찰자(`TutorialViewCardVisibilityObserver`)가 `OnDisable` 사실만 전한다. 같은 프레임에 닫았다 다시 열어도 이후 정리가 새 표시를 닫지 않는다.
5. `CombatUnitInfo.Render`의 Combat 전환 시 닫기·다음 Deployment 열기를 존중하고, 과거 상태를 다시 적용하지 않으며, 구독·임시 컴포넌트를 해제한다.

이것은 UI 자원 수명이며 세션 진행 상태가 아니다.

#### 15.13.3 대상 옆 설명과 위치 갱신

단계마다 **기준 대상과 선호 방향**만 View 정의에 둔다. 실제 좌표는 표시 계층(`TutorialViewExplanationLayout`)이 대상 영역·박스 실측 크기로 계산한다. 슬라이드 절대 좌표나 단계 번호별 분기를 만들지 않는다.

- `TutorialStepData`에 `explanationAnchor`(`TutorialFocusTarget`)와 `explanationPlacement`(`TutorialViewExplanationPlacement`: None / Above / Below / Left / Right)가 있다. 설명은 anchor가 자기 대상 목록에 있어야 하고 방향이 None이면 안 된다. 실습은 둘 다 None이다. `TutorialStepRule`·Buffer에는 복사하지 않는다.

| 설명 | 기준 대상 | 선호 방향 |
|---|---|---|
| 8 | Hand | Above |
| 9 | MainCharacterCard | Above |
| 10 | EnterCombatButton | Above |
| 13 | EnemyNexus | Below |
| 14 | MainCharacterUnit | Right |
| 15·16 | CurrentActorUnit | Right |
| 17 | ActionCost | Right |
| 19 | AttackButton | Above |
| 20 | CurrentActorUnit | Right |
| 21 | ActionCost | Right |

- 공통 폭·최소 높이·글자 크기·간격·화면 여백은 프리팹이 가진다. 본문 길이로 박스 높이를 재고, 21의 긴 문장을 자르거나 작게 숨기지 않는다.
- 선호 방향 → 반대 → 나머지 두 방향 순으로 화면 안이고 강조 대상·상단 HUD·하단 조작부와 겹치지 않는 위치를 고른다. 기준 대상이 없으면 화면 안 빈 곳. 모두 겹치면 교차 면적이 가장 작은 후보를 화면 안으로 보정한다.
- 설명 중 화면 크기·Canvas 배율·대상 위치·텍스트 크기가 바뀌면 **강조 구멍과 설명 박스를 함께** 다시 계산한다. 표시 전용 입구 `ITutorialViewDisplay.RefreshLayout()`을 연결된 화면이 있을 때 실행 소유자 Tick에서 부른다. 로딩 중에는 건너뛰지만 Runner가 바쁘다는 이유로 막지 않는다.
- 위치 갱신은 다시 Present가 아니다. 완료 콜백·Continue·revision·타이머 시작·실습 통과를 만들지 않는다. 닫힌 UI·Detach 뒤에는 아무것도 하지 않는다.

#### 15.13.4 실습 안내와 입력

- 설명 박스·실습 띠·막힌 안내는 튜토리얼 프리팹 안에 두고 공용 `NoticePannel` 외형을 재사용한다. 원본을 고치거나 그 UIBase·버튼 동작을 복제 등록하지 않는다.
- 실습 띠는 상단 중앙의 짧고 넓은 형태다(1920×1080 기준 폭 약 760·최소 높이 약 88·상단 여백 약 40에서 시작, 구현 설명 520×120·실습 760×88·막힌 안내 520×64, 실측 높이 적용, 글자 자동 축소 없음). 문구는 정의 값이다.
- 막힌 안내는 실습 띠 아래, 프리팹 값 **2초**. 다시 시도하면 시간을 갱신하고 겹쳐 쌓지 않는다. 구멍 여백 8도 프리팹 값이다.
- 설명 때는 강조 구멍 위까지 투명 막이 입력을 막는다. 실습 때는 덮개·막을 닫고 두 안내 아래 그래픽의 `raycastTarget`을 끈다. 막힌 시도의 입력 정책은 §15.10 한 곳이다.
- 바뀐 것: 초기 T-6의 “모든 박스 300×200·설명 고정 위치·강조 위치 1회 계산” → 위 정책(2026-10-20). 생성 도구의 신규 생성 결과도 맞추되 기존 프리팹은 덮어쓰지 않는다.

#### 15.13.5 구조 검토

| 기준 | 적용 |
|---|---|
| SSOT | 문구·순서·목표·기준 대상·방향은 정의, 공통 외형은 프리팹, 좌표는 표시 계산. 실습 통과·제한은 Buffer/Plan |
| SRP | 대상 조회·좌표 계산·그리기·카드 목록 수명·진행 판단을 분리. Bootstrap·Factory에 기능 분기 없음 |
| OCP | 단계별 위치는 정의로 바꾼다. 단계 번호 분기·별도 카드·이동 기능 없음 |
| LSP | 표시 완료·취소·정리 계약 유지, `RefreshLayout`은 진행에 영향 없음, 제한 None은 기존 동작 |
| ISP·DIP | 카드 공개 ID·RectTransform·필요한 ReadSink·UI 서비스만. 전투 결과는 공개 Buffer, 덱 개폐는 기존 UI 계약 |
| JMH·FW | Factory → ViewGroup → TutorialExecution 조립, 런타임 수명, Attach/Detach 유지. UIBase 1개, Parallel·Canvas 110 |
| 변경 독립성 | 레이아웃은 튜토리얼 정의·표시·프리팹, 입력 제한은 T-7 전투 입력 경계. Core·FieldCamera·Architecture·덱 소스 변경 없음 |

인수 순서는 T-6R(표시·수명) → T-7(입력 제한) → T-8(실제 정의로 전체 진행)이다. 카드·박스 보완만으로 입력까지 끝났다고 기록하지 않는다.

## 16 보류·결정 대기·확인 필요

| 항목 | 내용 | 다음 |
|---|---|---|
| 1-1 정의가 꺼져 있음 | HEAD의 `TutorialDefinition_1-1.asset` `targetMapKey`가 10000이라 1장 첫 전투에서 튜토리얼이 실행되지 않는다. 용근님 `3eb6be57`(“튜토리얼 이슈 Targetkey = 10000으로 임시처리”) → 호진 `c9543d5d`에서 1 → 호진 `83e53dac`(정산 UI 커밋)에서 다시 10000. `34ee0de4` 커밋 메시지는 “보고 싶지 않으면 큰 숫자”를 안내한다 | 의도(시험용 임시인지) 사용자 확인. 되돌리면 1 |
| 새 Core 규칙과 문구·대상 | 새 Core(2026-10-24 천명님 `d4a6d6fd`)는 유닛 차례 대신 플레이어 턴·선택·공유 행동 코스트다. 15 “현재 이 유닛의 턴입니다.”, 16·17·20·21의 유닛별 행동 코스트 설명, 이동 안내 “현재 행동 유닛”, 대상 `CurrentActorUnit`(지금은 필드 메인 캐릭터를 가리킴)이 새 규칙과 맞는지 재검토가 필요하다. [플레이어 턴 설계 §5.2](2026-10-23-player-turn-shared-actioncost-design.md#52-view-handoff-specification-only)가 튜토리얼 단계·문구 재작성을 요구한다 | 기획·사용자 결정 뒤 정의·필요하면 대상 enum 수정 |
| 클릭 이동 | 2026-10-27 클릭 이동 설계(용근님)에서 `MoveOnly`는 클릭 이동도 Move라 그대로 허용한다. 문구가 Drag만 말하면 호진님께 공유 예정(그 설계 CM-d) | 클릭 이동 구현 뒤 16·이동 안내 문구 검토 |
| 이동 불가능한 실습 데이터 | 정상 데이터는 수동 턴 종료·배치 국면 재진입으로 회복하지만, 늘 이동할 수 없는 구성의 재시도·안내 정책은 정하지 않았다(전투 View 4라운드 잔여) | 기획 결정 |
| 1장 1층 전투 고정 | 협의 후보(§15.12) | 기획자·Core·DB 협의 |
| 입력 독점 판정 | 설정 Layer로만 판정(§10) | 다른 독점이 생기면 Framework 작업자에게 조회 API 요청 |
| 포기 확인창 | 열린 채로 설명이 시작될 수 있음(§8.1) | 공개 사실이 생기면 조건에 추가 |
| 결과 보존 용량 | 런타임 동안 전부 보존(§11) | 필요하면 공개 계약 변경 |

## 17 다른 작업자 영향

### 17.1 튜토리얼 작업이 고친 다른 작업자 파일

| 누구 영역 | 파일(지금 위치) | 무엇을 | 공유 |
|---|---|---|---|
| 민형님(Framework·공용 조립) | `JobRootType`, `JobRegisterSystem`, `BufferSystemGroup`(`IBufferQuery`), `BufferSystemFactory`, `ServiceKeys/Runtime/Buffer/TutorialBufferKey`, `ViewDataBase`(`IViewQuery`), `ViewTableKey`, `ViewDataBaseFactory`, `ViewGroup.asset`, `InGameViewSystemGroup`(`TutorialExecution`), `InGameViewSystemFactory`, `UIIDs`(`Tutorial = 31`) | 튜토리얼 연결 줄 | 공유 메모 미작성(T-8, 계획 §2). 입력·UI 추가는 공유 안 함(§13) |
| 전투 View(용근님 영역) | `Combat/Input/CombatInputFacts`·`CombatInputPolicy`·`CombatInputRouter`, `Input/ActionButtons/CombatActionButtons`, `CombatViewSystem`, `Input/MovePath/CombatMovementRangeTracker`·`CombatPathInput`, `Input/Interaction/CombatInteraction`(T-7), `Display/Hud/CombatActionPanel`(19 버튼 표시·Attack 참조) | 이동만 허용, 19 표시 예외 | [T-7 전달 메모](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md)(미전달, 용근님이 V4-g·V4-m·V5-d 기록을 덧붙임) |
| 현강님(UI) | `MainCharactorUI`(`HealthBarTransform`), `Combat/Display/Hud/CombatUnitInfo`(이동·공격 비용 영역 참조) | 읽기 전용 참조 노출 | 따로 없음(구조 기준을 따르는 추가) |
| 공용 입력 에셋 | `PlayerInputActions.inputactions`(`Tutorial` map) | map 추가, 기존 줄·ID 그대로 | 공유 안 함(2026-10-19 사용자) |
| 다른 작업자 시험 | 가짜 대역·기대값 한 줄씩(계획 §3) | 컴파일·조립 맞춤 | 계획 §3 |

### 17.2 다른 작업자가 고친 튜토리얼 코드

| 누가 | 커밋·날짜 | 무엇을 |
|---|---|---|
| 용근님 | `81354f33`(2026-10-21) | 전투 View 폴더 정리에 따른 using |
| 용근님 | `44c4cdc2`·`8ed3f8cb`(2026-10-22~24) | `TutorialPlan`의 판정 사본 → `CombatPlan.IsAwaitingPlayerCommand`, 씬 순서 상수 → `SceneNodeOrder.TutorialAttach/Detach`([호진님 공유 메모](../handoffs/2026-10-22-combat-view-structure-hojin-share.md)) |
| 용근님 | 전투 View 2라운드 V2-f(2026-10-24) | 튜토리얼 Sink 둘을 `CombatBlockedInputReport`로 모음 |
| 용근님 | `11c07868`(2026-10-26, V4-g) | `TutorialFactsReader` 옛 활성 유닛 주석, MoveOnly 수동 턴 종료·배치 국면 재진입(§15.10) |
| 천명님 | `d4a6d6fd`(2026-10-24, 새 Core) | `TutorialViewFocusLocator`의 활성 유닛 조회 → 필드 메인 캐릭터 |

## 부록 A 기획 대조·UI 보완 검토 요약 (2026-10-20)

- **요청:** 기획서 전체를 확인하고 8부터의 설명과 실제 배치·전투 진입·드래그 이동을 잇는 UI를 보완한다. 결과는 §15.10·§15.13에 통합했고 이 부록은 조사 근거의 요약이다.
- **확인 범위:** 원본 `C:/Users/Hyeon2-400/Dropbox/Project L/기획 브레인스토밍/안재령의고뇌/튜토리얼 기획서.pptx` 21장(8~21의 발표자 노트에는 추가 본문 없음), 설계·계획, 정의 에셋, 튜토리얼 Buffer·Plan·Job·View·입력, 프리팹·생성 도구·Combat 씬, 전투 카드 표시와 UI 개폐 경로. 원본을 읽기 전용으로 렌더링한 자료는 `.superpowers/sdd/2026-10-20-tutorial-audit/slides/`(git 무시)에 있다.
- **찾은 어긋남과 처리**

| 어긋남 | 처리 |
|---|---|
| 8·9가 지금 게임이 쓰지 않는 옛 손패(`HandCardHoverView`)를 찾아 실제 덱 카드를 강조하지 못함(개발 직행은 카드 목록이 닫혀 있기도 함) | §15.13.1·§15.13.2(T-6R) |
| 모든 설명이 같은 300×200 고정 박스라 대상 옆 설명을 재현하지 못함 | §15.13.3·§15.13.4(T-6R) |
| `MoveOnly`를 읽는 전투 입력이 없어 이동 실습 중 공격·턴 종료가 가능 | §15.10(T-7) |
| 설명 시작 때 대상 영역을 한 번만 구해 창 크기·배율 변화에 안 따라감 | `RefreshLayout`(§15.13.3) |
| 계획 기록이 프리팹·씬 인스턴스 존재와 Unity 확인을 섞음 | 계획에서 “배치됨·코드 완료·Unity 확인”을 나눔 |

- **방법 비교:** 프리팹 크기만 조정(카드·배치·입력 미해결) / **기존 구조 유지, 표시 보완 뒤 T-7 연결(추천, 채택)** / 전용 전투·카드 UI 재구현(1-1 재사용 정책과 맞지 않음).
- 바뀐 것: 조사 때 고른 “막힌 버튼을 눌리게 두고 안내” → 같은 날 후속 지시로 버튼 비활성(§15.10).
