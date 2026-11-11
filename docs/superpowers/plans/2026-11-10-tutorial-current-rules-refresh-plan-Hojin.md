# 최신 규칙 튜토리얼 교체 Implementation Plan

> **For agentic workers:** 실행 시 `superpowers:executing-plans`를 적용해 승인된 Phase의 Task를 순서대로 수행한다. 별도 위임이 명시되면 `superpowers:subagent-driven-development`를 적용한다. 체크박스는 실제 수행 증거가 있을 때만 갱신한다. 프로젝트의 역할·Phase 종료·시험/커밋 금지 규칙이 스킬의 일반 절차보다 우선한다.

**Goal:** 기존 튜토리얼 구조를 유지하면서 최신 전투 규칙의 17단계, 공격·턴 종료 실습, 클릭 전용 진행, UI 점선과 모델 아웃라인을 연결한다.

**Architecture:** Tutorial Buffer가 단계·기준점을 소유하고 FactsReader → Plan → Job이 공개 전투 사실로 진행한다. CombatInputPolicy가 입력 허용을 결정하며, 표시 계층은 한 번 해석한 강조 대상에서 덮개·점선·모델 대여를 구성한다. 렌더 속성 합성은 UnitModelAppearance 한 곳, 조립은 기존 Factory/Group 경로에 유지한다.

**Tech Stack:** Unity 6000.0.73f1, C#, uGUI/MaskableGraphic, 기존 JMH Framework·Buffer·Plan/Job, Cartoon/EntityCartoon 외형 합성.

**Spec:** [2026-11-10 확정 설계](../specs/2026-11-10-tutorial-current-rules-refresh-design-Hojin.md). 아래의 ‘설계 §’는 이 문서다. 단계 문구·허용표·성공 의미의 원본은 설계이며 이 계획에서 따로 바꾸지 않는다.

- 작성: 2026-11-10, Codex. 사용자가 설계를 확정하고 Task 계획 작성을 요청했다.
- 상태: **Phase A·B·C 코드 및 D의 17단계 정의 반영·정적 확인 완료, 사용자 Unity 확인 대기. E 미착수.** 아군 4종의 Cartoon/Opaque·외곽선 속성은 사용자가 확인했다. 실제 Outline Pass 출력은 미확인으로 남긴다. 사전 조사는 §9.1, A/B는 §9.2/§9.3, C의 부분 이력은 §9.4, C 재개 기록은 §9.5, D 구현 기록은 §9.6에 있다.
- 조사 기준: 로컬 `f7b842aad`와 작업 파일. 사용자/다른 작업자의 기존 변경은 보존한다. 이 문서는 이전 T-1~T-7의 완료를 이번 변경의 완료로 계승하지 않는다.

## Global Constraints

- 각 Phase 끝에는 변경·정적 확인·Unity 미확인·잔여 항목을 보고하고 멈춘다. 다음 Phase 실행은 사용자 지시 범위에 따른다. 설계와 다른 결정은 구현 전에 묻는다.
- 기본 작성 역할은 기존 개인 규칙대로 Claude(C#·내부 기능), Codex(검수·승인된 표현 에셋)다. 2026-11-10 사용자의 각 Phase 직접 구현 지시로 튜토리얼 A와 B의 코드·관련 기록을 Codex가 작성한다. A의 에셋 변경은 A 승인 범위이며 B에서는 정의/프리팹을 변경하지 않는다. 개인 규칙의 전역 역할 변경이나 C~E 실행 승인으로 확대하지 않는다. 같은 파일을 동시에 작성하지 않는다.
- 새 Test 코드·시험 도우미를 만들지 않고 MSBuild·EditMode를 실행하지 않는다. 컴파일·재생은 사용자 Unity 확인이며 보고에 **컴파일 미확인**을 적는다. 커밋·push는 사용자가 한다.
- 기존 시험은 컴파일 파손의 최소 호출 수정만 허용한다. 우리 시험의 실행 파손은 검증 의미를 유지한 호출 조정만, 다른 작성자의 실행 파손은 수정 전 사용자 확인이다. 자동 진행 폐지처럼 검증 의미 자체가 사라지는 시험은 무단 삭제/무력화하지 않고 따로 기록한다.
- Core·FieldCamera·Architecture, 공용 셰이더/SelectOutlineSystem 확장, 입력 Action 에셋 변경은 제외한다. Core 사실이 부족하면 기존 전달 문서에 요청하며 그 파일을 수정할 때 제목·갱신일도 바꾼다.
- `targetMapKey: 1`, 덮개 알파 `0.95`, 기존 정의/프리팹 GUID를 보존한다. 단계는 17개(설명 13·실습 4), 설명은 클릭만, 실습은 결과만으로 진행한다.
- 목표 번호 `None=0, EnterCombat=1, MoveAtLeastOneCell=2, AttackEnemyOnce=3, EndPlayerTurnOnce=4`; 제한 번호 `None=0, MoveOnly=1, AttackPractice=2, EndTurnOnly=3`을 사용한다.
- `ContinueOrTimeout=3`은 `Continue=3`으로 교체한다. PlanStep의 옛 `Expire=6`은 결번으로 남기고 `PassPractice=7`을 유지한다. Focus의 7·8·삭제된 11을 재사용하지 않고 `PlayerUnits=12, EndTurnButton=13`을 추가한다.
- View는 InGame DB·Core Application을 읽거나 Core Buffer에 쓰지 않는다. 실제 적/넥서스 Hits는 0피해도 인정하며, 전투 종료는 실습 성공보다 우선한다.
- 기존 `UIId.Tutorial`·Parallel·Canvas와 Global UI Context를 유지한다. 점선은 내부 위젯이며 새 서비스/UIId를 등록하지 않는다. 입력은 기존 receiver/Layer/UIInputBinding 경로를 따른다.
- 새/제거 `.cs`는 `.meta`를 함께 다룬다. LF·UTF-8·Allman·폴더 대응 namespace·기본 sealed·한국어 주석을 따른다. Missing Script 에셋은 삭제하지 않는다.

## Review Focus

| 조건 | 기대 | 소유 Task·Unity 확인 |
|---|---|---|
| 공격 결과 직후 승패/EndBattle로 대상 기록이 정리됨 | 대상 조회 전에 종료 수명을 읽고 BattleEnded로 정리 | B1, E1-2 |
| 실습 안내 중·통과 직후 늦은 포인터/미리보기 응답 | 추가 제출만 차단, 수락된 결과·적 자동 진행은 계속 | B2, E1-3 |
| 모든 아군 행동 사용 또는 밤 경계로 배치 복귀 | 이동/공격 목표 유지, 정상 재배치·턴 종료로 회복 | B2, D1, E1-3 |
| 모델 재바인딩/풀 반환과 기존 투명도·플래시·디졸브가 겹침 | 튜토리얼 층만 반환하고 새 개체·다른 효과는 보존 | C2~C3, E1-4 |
| 창 비활성·해상도 변경·진입 연출 중 설명 시작 | 같은 설명 유지, 대상/테두리 정렬, 진입 완료 뒤 입력 독점 | A1~A2, C1~C3, E1-1/4 |

---

## 1. Phase와 파일 경계

| Phase | 산출물 | 선행 | 리뷰 지점 |
|---|---|---|---|
| 0 | 실제 대상·재질·접점·시험 영향 확인 기록 | 실행 지시 | 승인 설계로 구현 가능한지 확인 |
| A | 자동 진행 제거, 진입 완료 대기, 카드 소유권 정리 | 0 | 기존 13단계의 수동 진행/진입 확인 |
| B | 공격·턴 종료 실습 판정과 입력 제한/회복 경로 | A | 새 계약·결과 판정·제출 경계 검수 |
| C | UI 점선·유닛/넥서스 아웃라인과 정리 수명 | B | 기존 대상에서도 표현/효과 복원 확인 |
| D | 확정한 17단계 정의 교체 | C | 실제 튜토리얼 한 바퀴 확인 |
| E | 통합 인수·관련 옛 미확인 항목 정리·인계 | D | 사용자 확인 근거와 남은 제한 보고 |

중간 Phase는 출시 가능한 전체 기능 완료가 아니다. 특히 B의 새 목표/제한은 D에서 실제 정의에 연결하므로, B의 정적 검수를 실습 재생 성공으로 기록하지 않는다. 막힌 Phase에서 후속 기능을 임의로 추가하지 않는다.

다음 약어는 정확한 경로 접두사다. 파일 목록의 같은 접두사는 반복하지 않고 묶었다.

| 약어 | 경로 |
|---|---|
| `S/` | `Assets/ProjectL/Script/Systems/` |
| `B/` | `S/RuntimeSystem/BufferSystems/Tutorial/` |
| `L/` | `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Tutorial/` |
| `V/` | `S/RuntimeSystem/InGameSystem/ViewSystem/` |
| `T/` | `V/Tutorial/` |
| `D/` | `S/DataBaseSystem/Data/View/Table/Tutorial/Item/` |
| `P` | `Assets/ProjectL/Prefabs/UI/Tutorial/TutorialUI.prefab` |
| `A` | `Assets/ProjectL/DataBaseSO/ViewGroup/TutorialDefinition/Item/TutorialDefinition_1-1.asset` |
| `E` | `Assets/ProjectL/Editor/Tutorial/TutorialUISetup.cs` |

신규 파일은 아래 C Task에만 둔다. 기존 Buffer/Plan/Policy 확장에 범용 목표 레지스트리·새 진행 Runner·전역 이벤트를 만들지 않는다. `TutorialViewUI`는 위젯 표시, `TutorialViewUIDisplay`는 기존 표시 수명 연결만 맡는다.

## 2. Phase 0 — 구현 전제 확인

### Task 0-1: 대상과 기존 의존성 확인

**Files:** 조회만: `A`, `P`, `E`, `T/TutorialViewFocusLocator.cs`, `V/Combat/Display/Unit/FieldUnitView.cs`, `UnitModelAppearance.cs`, `EntityCartoonAppearance.cs`, 실제 대상 모델/재질 및 관련 기존 시험. 결과는 이 계획 §9에 기록한다.

**Interfaces:** 기존 Character/Combat/Result ReadSink, `ICombatViewService.Progress`, UnitModelAppearance의 ColorSystem 합성 경로를 인수한다. 새 실행 API는 만들지 않는다.

- [x] 0-1a. 변경 파일과 실제 HEAD를 확인해 다른 작업자의 작성 중인 파일을 기록한다. 전투 View 접점은 단일 작성자로 조율한다. — §9.1의 작업 트리·역할 기록. 현재 전투 C# 변경은 없고 구현 작성자 배정/외부 연락은 하지 않았다.
- [x] 0-1b. 실행 가능한 첫 전투 후보가 넥서스 목표이고 이동 가능 아군·공격 가능한 적/넥서스를 제공하는지 확인한다. 보스 후보/불가능한 구성이 있으면 자동 건너뛰기로 우회하지 않고 해당 콘텐츠 결정을 분리한다. — 일반 새 Run의 1120 고정과 정적 콘텐츠 전제 확인. 실제 조작 성공은 D/E의 사용자 재생 확인이다.
- [ ] 0-1c. 메인·서브 아군·적·넥서스의 실제 모델 렌더러, 무기/다중 재질, Cartoon/EntityCartoon 외곽선 속성과 Pass 지원을 표로 기록한다. `MeshRenderer`·`SkinnedMeshRenderer` 각각의 적용 가능성을 확인한다. 미지원 필수 모델은 설계 변경 판단 대상으로 보고하고 C 구현을 강행하지 않는다.
- [x] 0-1d. `TutorialViewTimer`, `TutorialViewFocusNotifier`, `TutorialViewCardVisibility`/Observer의 전체 참조를 확인한다. 제품의 타이머/카드 전용 참조와 기존 시험 참조를 분리하고, 삭제/시그니처 변경으로 영향을 받는 기존 시험 파일을 기록한다. — §9.1 참조표. 현재 직접 참조 시험은 없다.
- [x] 0-1e. A~E의 수정 경계가 설계와 맞는지 보고한다. 이 사전 확인을 실행한 기록과 Unity 재질/화면 확인 여부를 구별한다. — A2 생성 호출 파일 누락을 보완했고, C의 실물 재질 확인을 완료로 처리하지 않았다.

초기 0-1c는 프리팹/재질 수명 목록까지 조사했으나 아군 FBX 내장 재질의 실제 shader/Pass를 읽지 못해 체크하지 않았다. 이후 사용자 확인으로 C 코드를 재개한 근거는 §9.5다. 실제 Pass 출력은 아직 미확인이므로 이 항목의 전체 완료로 기록하지 않는다.

**완료 조건:** 구현을 막는 콘텐츠·재질·소유권 문제가 식별됐고, 미확인 사항이 표에서 사라지지 않는다. 전제가 설계와 다르면 해당 구현 전에 사용자 판단을 받는다.

## 3. Phase A — 수동 진행과 시작 수명

### Task A1: 자동 진행 계약을 한 묶음으로 제거

**Files:** 수정 `B/TutorialWaitReason.cs`, `TutorialSessionSnapshot.cs`; `L/TutorialFacts.cs`, `TutorialFactsReader.cs`, `Plans/TutorialPlan.cs`, `Plans/TutorialPlanStep.cs`, `Jobs/TutorialStepJobs.cs`; `T/ITutorialViewExecution.cs`, `TutorialViewSystem.cs`, `TutorialViewUIDisplay.cs`, `TutorialViewUI.cs`; 주석 정리 `T/ITutorialViewDisplay.cs`, `TutorialViewPresentationKind.cs`; `D/TutorialDefinitionDataSO.cs`; `S/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs`; `A`, `P`, `E`. 제거 대상 `T/TutorialViewTimer.cs`와 `.meta`, 타이머 전용임을 확인한 `TutorialViewFocusNotifier.cs`와 `.meta`.

**Interfaces:** `Continue=3`, 기존 `CanContinue`, `Continue` 요청/session/revision 검사를 유지한다. `ITutorialViewExecution`의 `StartTimer`·`IsTimerExpired`만 제거한다. 생성자는 `TutorialViewSystem(RuntimeFaultBoundary faults, Action<string> log = null, ITutorialInputReceiverSlot inputSlot = null)`로 바꾸고 해당 Factory 호출을 맞춘다.

- [x] A1a. WaitReason/Snapshot/표시 완료 Job을 `Continue`로 맞추고 Plan의 만료 분기와 `TutorialExpireJob`을 제거한다. `Expire=6` 재사용 없이 다른 번호를 유지한다.
- [x] A1b. Facts·실행 인터페이스·ViewSystem에서 만료 사실, 시간 시작/중단/읽기, 전용 시계·포커스 의존성을 제거한다. `Tick`의 Runner 등록·표시 갱신·로그 등 비타이머 처리는 유지한다.
- [x] A1c. `TutorialViewUIDisplay`의 포커스 콜백과 UI/프리팹/Setup의 타이머 전용 notifier 참조를 함께 정리한다. 실제 입력/표시 복귀에 쓰는 별도 수명은 제거하지 않는다. 파일 삭제는 Phase 0에서 전용 사용을 확인한 대상만 한다.
- [x] A1d. 정의 데이터의 `autoAdvanceSeconds`/`AutoAdvanceSeconds`·시간 검증과 `A`의 해당 직렬화 항목을 제거한다. 기존 13단계와 targetMapKey는 이 Task에서 바꾸지 않는다. 막힌 입력 안내의 표시 시간은 보존한다.
- [x] A1e. 제품 폴더에서 옛 타이머/만료 식별자의 실행 참조가 남지 않았는지 검색한다. 기존 시험의 컴파일 영향만 허용 범위에서 정리하고, 삭제된 기능의 옛 기대값은 별도 미해결 기록으로 남긴다. — 직접 참조 시험은 현재 checkout에 없으며 시험 파일은 수정하지 않았다(§9.2).

**사용자 Unity 확인:** 첫·중간·마지막 설명에서 기존 5초를 넘겨 기다려도 유지, 한 클릭 한 단계, 마지막 클릭 전 Finished 없음, 실습은 클릭으로 통과하지 않음, 창을 나갔다 돌아와도 같은 설명. 막힌 입력 안내는 기존처럼 사라짐.

### Task A2: 진입 대기를 연결하고 카드 강제 개폐 제거

**Files:** 수정 `L/TutorialPlanSources.cs`, `TutorialFacts.cs`, `TutorialFactsReader.cs`, `Plans/TutorialPlan.cs`, `Jobs/TutorialStepJobs.cs`; `T/TutorialViewUIDisplay.cs`, `TutorialViewSystem.cs`(UIDisplay 생성 호출); 전용 참조 제거 뒤 `TutorialViewCardVisibility.cs`, `TutorialViewCardVisibilityObserver.cs`와 각 `.meta` 정리.

**Interfaces:** `TutorialPlanSources.From(IBufferQuery buffers, ITutorialViewExecution execution, ICombatProgressView progress)`로 확장한다. Sources는 `Progress`만 보관한다. Facts에는 `IsCombatViewAttached`, `IsEntryPresentationPending`, `IsSubmissionSuspended`, `IsAbandonRequested` 네 bool을 둔다. `TutorialPlan.CanStartExplanation(in TutorialFacts facts)`가 시작 조건의 유일한 판정이다.

- [x] A2a. Plan 조립에서 기존 ViewGroup의 Combat 서비스 → Progress를 꺼내 전달한다. 필수 누락은 조립 오류로 보고하며 루트 서비스 재등록·전체 ViewGroup 보관은 하지 않는다.
- [x] A2b. FactsReader에 네 사실을 옮기고, 기존 안정된 PlayerInput 조건에 연결됨/진입 완료/제출 정지 아님/포기 요청 없음 조건을 더한다. 시작 Job도 입력 Layer를 빌리기 직전에 같은 함수를 재검사한다.
- [x] A2c. UIDisplay의 카드 Prepare/Release/Detach 호출과 전용 의존성을 제거한다. 이후 생성자는 `TutorialViewUIDisplay(ICharacterBufferReadSink characters, ICombatStateBufferReadSink combat)`로 정리한다. UI의 UISystem 바인딩/외부 개폐 경로는 그대로 유지한다.
- [x] A2d. Start 요청을 지연시키는 새 타이머/Job 대기를 만들지 않았는지, 기존 진입 연출 Tick과 자동 전투 진행이 계속되는지 정적으로 대조한다. — 대기는 Plan의 Nothing 반환이며 Combat 진입 연출 Tick과 카드 개폐 구현은 수정하지 않았다. 실제 재생은 사용자 확인 대기다.

**사용자 Unity 확인:** 맵→배치 안내→카드 등장 완료 뒤 8 표시, 8·9에서 이미 보이는 실제 카드 강조, 튜토리얼 종료가 카드를 닫지 않음. 진입 도중 포기/오류/씬 이탈 시 독점 Layer 잔류 없음.

**Phase A 종료:** 기존 정의로 수동 진행·진입 수명을 확인하고 보고한다. 새 실습/강조 구현은 다음 Phase다.

## 4. Phase B — 실습 판정과 입력 제한

### Task B1: 실제 공격·턴 종료 결과를 실습 사실로 읽기

**Files:** 수정 `B/TutorialPracticeGoal.cs`, `TutorialCombatInputRestriction.cs`, `TutorialStepRule.cs`; `L/TutorialPlanSources.cs`, `TutorialFacts.cs`, `TutorialFactsReader.cs`, `Plans/TutorialPlan.cs`. `L/TutorialSessionChanges.cs`는 실습 기준점 설치 계약의 확인 대상이며 중복 기준점은 추가하지 않는다.

**Interfaces:** Sources에 `ICharacterBufferReadSink Characters`를 추가하고 `From`은 BufferQuery에서 좁은 Sink만 꺼낸다(A2 시그니처 유지). Facts에 `bool HasPresentedEnemyAttack`, `bool HasPresentedPlayerTurnEnd`를 추가한다. `IsPracticeGoalMet(in TutorialFacts facts)`가 새 목표와 이 사실을 대응한다.

- [x] B1a. 목표/제한 enum과 StepRule의 허용 조합을 설계 §5.1대로 추가한다. 기존 EnterCombat+None, MoveAtLeastOneCell+None/MoveOnly 허용은 유지한다.
- [x] B1b. FactsReader에서 같은 전투·HasBattle·Current 존재·Completion 없음 확인을 결과 대상 조회보다 먼저 수행한다. 종료/다른 전투/해제에서는 목표 사실을 읽지 않고 기존 중단 사실만 전달한다.
- [x] B1c. 실습 기준점 뒤 수락된 일반 Attack의 Succeeded/Hits와 표시 완료를 읽는다. 실제 TargetId를 Character 또는 Current.Structures에서 확인해 적 유닛/적 Structure_Headquarters면 인정한다. 피해량·생존·선택 대상·Attack.CurrentStage를 조건으로 쓰지 않는다. 활성 미종료 전투의 미확인 ID는 ID/결과 순번을 포함한 오류로 드러낸다.
- [x] B1d. 같은 기준점/표시 조건 아래 수락된 EndPlayerTurn 명령과 같은 결과의 PlayerTurnEnded 사건을 읽는다. 회복용 이전 종료, 자동 적 종료, 버튼 클릭을 성공 근거로 쓰지 않는다.
- [x] B1e. Plan의 목표 대응만 확장하고 BattleEnded 우선, EnterCombat의 StartResultSequence, 이동의 실제 한 칸 이상/부분 이동 의미를 유지한다. 적 턴 완료는 새 목표로 저장하지 않고 다음 설명 시작 조건에 맡긴다.

**확인:** 코드 대조로 결과 종류·기준점·P 순번·종료 우선순위를 확인한다. 실제 0피해/희생/처치/넥서스 파괴와 EndBattle 재생은 D/E에서 확인하며 이 Phase의 정적 검수로 대체하지 않는다.

### Task B2: 명령·미리보기·버튼을 같은 제한 정책에 연결

**Files:** 수정 원본 `V/Combat/Input/CombatInputPolicy.cs`. 적용 확인 후 필요한 접점만 수정: 같은 폴더의 `CombatInputFacts.cs`, `CombatInputFactsReader.cs`, `CombatInputRouter.cs`, `CombatCommandGate.cs`, `ActionButtons/CombatActionButtons.cs`, `ActionButtons/CombatInputSurfaces.cs`, `ActionButtons/CombatEndTurnEntry.cs`, `ActionButtons/CombatDeploymentEntry.cs`, `Interaction/AttackTargetingInteraction.cs`, `MovePath/CombatPathInput.cs`. 되돌리기/배치 제출 호출자는 이 정책의 실제 호출 경로에서 추적한다.

**Interfaces:** 기존 `AllowsTutorialCommand(in CombatInputFacts facts, CombatViewCommandKind kind)` / `AllowsTutorialPreview(in CombatInputFacts facts, CombatViewPreviewKind kind)`가 제한 원본이다. `CanSubmit`/`CanPreview`와 `IsTutorialPracticeGoalPending`을 재사용한다. UI/Router에 별도 허용표를 복제하지 않는다.

**조립 접점 보완:** B2e의 금지 이동 제스처 안내를 `CombatPathInput`에 연결하기 위해 `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`도 기존 `sources.ReportBlockedInput` 전달만 수정한다. 안내 정책이나 새 서비스 등록을 Factory로 옮기지 않는다.

- [x] B2a. 설계 §5.1의 전투 명령·미리보기 표를 반영한다. 제한된 실습의 Pending=false에는 모두 거절하고, None에는 기존 정상 입력 조건을 유지한다.
- [x] B2b. 미완료 MoveOnly/AttackPractice의 Deployment에서 PlaceMainCharacter·PlaceUnit·ReturnUnit·EnterCombat 및 대응 배치 미리보기를 정상 Gate 아래 허용한다. EndTurnOnly에는 이 회복 경로를 열지 않는다.
- [x] B2c. 버튼 활성화·키·포인터·되돌리기·배치·늦은 공격/이동 응답의 최종 제출을 같은 정책에 대조한다. 실습 안내 전/성공 뒤 추가 제출은 차단하고 이미 수락된 결과와 적 자동 진행은 소비한다.
- [x] B2d. 정보 확인·선택·카메라·취소·설정·포기 경로를 보존한다. 정보창 열기와 금지된 새 사거리 미리보기는 구분한다(현재 CanInspectUnit의 UnitAttackRange 결합 확인). 취소/모드 해제가 제한에 막혀 남지 않게 한다.
- [x] B2e. 실제 금지 입력 시도만 기존 ReportBlockedInput으로 안내한다. 비활성 버튼·호버·매 프레임 검사에서 반복 안내하지 않는다. 제한 중 기존 턴 종료 확인창 생략, 일반 전투의 확인창 유지.
- [x] B2f. `None/MoveOnly/AttackPractice/EndTurnOnly × 전투/배치 × Pending true/false`를 설계 표와 정적으로 대조한다. T-7 입력 인계 문서에는 새 정책과 미검증 상태를 기록한다.

**사용자 Unity 확인:** 기존 이동 실습의 이동/턴 종료/배치 복구와 일반 전투 입력 회귀. 새 공격/턴 종료 실습의 실제 입력 확인은 D/E에서 수행한다.

**Phase B 종료:** 판정과 입력 허용을 함께 검수해 보고한다. Core 계산과 전투 진행 Plan은 수정하지 않는다.

## 5. Phase C — 점선과 모델 아웃라인

### Task C1: 강조 대상 자료와 UI 점선 위젯

**Files:** 수정 `D/TutorialFocusTarget.cs`, `T/TutorialViewFocusLocator.cs`, `TutorialViewOverlay.cs`, `V/Combat/Display/Hud/CombatActionPanel.cs`. 생성 `T/TutorialFocusFrame.cs`, `TutorialHighlightStyle.cs`, `TutorialFocusBorderGraphic.cs`, `TutorialFocusGeometry.cs` 및 각 `.meta`.

**Interfaces:** 패널은 `internal RectTransform EndTurnButtonRect`를 노출한다. Locator는 `void Locate(TutorialStepData step, TutorialFocusFrame output)`로 한 번 해석한다. 재사용 Frame은 `IReadOnlyList<Rect> Holes`, `UiRects`, `IReadOnlyList<TutorialModelFocus> Models`, `Rect? Anchor`를 제공한다. 같은 파일의 readonly `TutorialModelFocus`는 `FieldUnitView View`, `EntityId EntityId`, `int BodyTrackingVersion`을 가진다. 보유 참조와 ID/모델 버전을 함께 비교한다.

- [x] C1a. Focus 번호 12·13을 추가하고 PlayerUnits는 살아 있는 플레이어 필드 유닛 전체, EndTurnButton은 패널의 실제 버튼을 찾는다. 기존 필수 UI 참조 오류와 순간적인 비표시/유닛 부재를 구분한다.
- [x] C1b. 같은 대상 해석에서 UI 영역·모델 참조·덮개 구멍·anchor를 만들고 중복 개체를 제거한다. 기존 앵커 전용 재검색은 이 결과를 재사용하도록 바꾼다. FocusTargets 비움/Anchor None은 정상 빈 결과다. EnemyUnits 등 기존 대상 선정 의미는 요청 범위 밖에서 확대하지 않는다.
- [x] C1c. `TutorialHighlightStyle`은 직렬화 가능한 표현 값 묶음으로 만들고 TutorialUI 프리팹의 한 필드만 소유하게 한다. 공통 색, 점선 두께/길이/간격, 모델 폭을 담되 문구·대상·진행 상태는 넣지 않는다. 초기 조정값은 흰색, Canvas 단위 두께 2·길이 10·간격 6, 모델 폭 0.015로 두고 C3의 Unity 확인에서 조정한다. 기존 강조 여백은 한 원본에서 공유한다.
- [x] C1d. 점선 위젯은 `MaskableGraphic`, `raycastTarget=false`로 만들고 `SetScreenRects(IReadOnlyList<Rect> rects, float padding, TutorialHighlightStyle style)`와 `Clear()`를 제공한다. 버퍼를 재사용하고 대상/좌표/스타일/Canvas 변화 시에만 메시를 갱신한다. — C3 연결 포함 코드 반영·정적 검수 완료. 실제 화면은 Unity 미확인(§9.5).
- [x] C1e. Overlay와 점선이 `TutorialFocusGeometry.ToLocal(RectTransform canvasRect, Rect screenRect, float padding)`의 동일 변환을 쓰게 한다. 좌표는 화면 픽셀, 여백/점선 치수는 Canvas 단위로 구분한다. 모델 구멍 확장과 점선 외곽을 설명 회피 영역에 반영한다.

**확인:** UI 점선에 입력 receiver/리스너가 없고, Hand는 한 테두리·MainCard는 해당 카드·월드 모델은 UI 점선 없음. 화면 확인은 C3에서 함께 수행한다.

### Task C2: 모델 외형의 아웃라인 층과 대여

**Files:** 수정 `V/Combat/Display/Unit/UnitModelAppearance.cs`, `FieldUnitView.cs`. 생성 같은 폴더의 `ICombatUnitOutline.cs` 및 `.meta`. 기존 `EntityCartoonAppearance.cs`는 재질 교체/복원 수명의 확인 대상이며 강제 전환 로직을 추가하지 않는다.

**Interfaces:** `internal interface ICombatUnitOutline : IDisposable`는 `bool IsAvailable { get; }`를 제공한다. `FieldUnitView.BeginOutline(Color color, float width)`가 이 대여를 반환한다. UnitModelAppearance에는 `SetOutline(Color color, float width)` / `ClearOutline()`을 추가한다. 모델 경계 읽기는 FieldUnitView의 `bool TryGetModelBounds(out Bounds bounds)`로 좁히고 실제 외형 렌더러의 월드 bounds만 제공한다. 대여는 기존 표시 Slot 패턴으로 현재 소유자만 해제한다.

- [x] C2a. Phase 0에서 확인한 실제 모델 렌더러만 사용한다. 다중 슬롯의 유효 원래 `_OutlineColor`·`_OutlineWidth`를 보존하고 기존 ColorSystem 작성 경로에서 아웃라인 층을 합성한다. 새 ColorHandle·sharedMaterial 직접 변경·별도 PropertyBlock 작성자는 만들지 않는다.
- [x] C2b. 다른 투명도/피격 플래시/디졸브 층이 활성인 상태에서 강조 적용·해제해도 현재 층들이 유지되도록 Apply/Restore 조건을 확장한다. 핸들 Dispose만으로 원래 값이 복원된다고 가정하지 않는다.
- [x] C2c. FieldUnitView 대여에 EntityId·현재 Slot·모델 수명 검사를 적용한다. Bind, ResetForPool, OnDisable/OnDestroy, 외형 교체/해제 시 무효화하고 늦은 Dispose가 새 개체/새 대여를 지우지 않게 한다. 일반 선택·호버는 바꾸지 않는다.
- [x] C2d. 모델 외곽선 확장분을 현재 카메라 투영/모델 영역에 반영해 덮개에 잘리지 않도록 C1의 모델 영역 산출에 연결한다. 새 카메라·관통 셰이더·숨은 대체 모델은 만들지 않는다.

**확인:** 실제 Mesh/Skinned 애니메이션과 무기/다중 슬롯에서 외곽선을 확인한다. 미지원 모델은 성공으로 생략하지 않는다. 수명/다른 효과 회귀 확인은 C3/E1-4에서 수행한다.

### Task C3: 표시 수명·프리팹 연결

**Files:** 생성 `T/TutorialModelHighlight.cs` 및 `.meta`; 수정 `T/TutorialViewUIDisplay.cs`, `TutorialViewUI.cs`, `P`, `E`. 기존 `TutorialViewExplanationLayout.cs`는 회피 영역 전달로 해결되지 않는 경우에만 수정한다.

**Interfaces:** `TutorialModelHighlight.Sync(IReadOnlyList<TutorialModelFocus> targets, Color color, float width)` / `Clear()`가 현재 대여만 소유한다. TutorialUI의 `RefreshExplanationLayout`은 C1 Frame과 기존 placement/obstacles를 받아 덮개·점선·설명 위치를 함께 갱신한다. 스타일은 UI에서 읽고 모델 협력 객체에 필요한 값만 전달한다.

- [x] C3a. UIDisplay의 RefreshLayout에서 Frame을 한 번 갱신해 UI와 모델 협력 객체에 전달한다. 같은 대상은 대여를 유지하고 사라진/재바인딩된 대상은 반환한다. 진행 판단·전투 선택 변경을 넣지 않는다.
- [x] C3b. 설명 전환·실습 표시·Close/Detach·정상 완료·승패/포기·오류 정리에서 점선과 해당 대여만 비운다. 일부 반환이 실패해도 다른 정리를 시도하는 기존 CleanupErrors 경로를 따른다. 창 포커스 복귀는 단계 진행 없이 현재 표시를 유지/복구한다.
- [x] C3c. 기존 Canvas 안에서 Overlay 위/설명 박스 아래에 점선 위젯을 연결하고 스타일을 한 곳에 직렬화한다. Blocker/정렬 110/기존 UISystem 개폐를 유지한다. Setup 재실행도 동일 참조를 만들도록 갱신하며 무관한 씬/프리팹을 재생성하지 않는다.
- [x] C3d. UI 필수 참조·스타일 값 검증을 Attach 이전 경로에 연결한다. 빠진 점선 위젯/버튼은 필드와 UIId를 표시한 구성 예외로 보고한다.

**사용자 Unity 확인:** 카드·버튼·체력 점선, 유닛/넥서스 실루엣, UI+모델 혼합, 1280×720/1920×1080/2560×1440·창 크기 변경, 문구 가림/외곽선 잘림/입력 누출 없음. 단계 전환·실습·종료·풀 반환 후 잔상 없음, 투명도/플래시/디졸브/원래 외곽선 복원. 스타일 조정은 이 Phase의 표현 범위다.

**Phase C 종료:** 표현과 정리 수명만 검수해 보고한다. 17단계 내용 교체는 D에서 한다.

## 6. Phase D — 정의 에셋 교체

### Task D1: 확정된 17단계 콘텐츠 연결

**Files:** 수정 `A`. `D/TutorialStepData.cs`, `TutorialDefinitionDataSO.cs`와 기존 Table은 정의 검증 확인 대상이며 문구를 C#에 복제하지 않는다. 프리팹의 스타일 보정은 C 결과를 인수한다. D 구현에서 승인된 25번의 빈 강조/중앙 배치를 기존 코드가 지원하지 않는 점을 확인해 `D/TutorialStepData.cs`와 `T/TutorialViewExplanationLayout.cs`를 최소 보완했다(§9.6). 새 진행 정책이나 표시 방식을 추가한 것은 아니다.

**Interfaces:** 설계 §3 단계 표와 §5.3 막힌 입력 문구가 콘텐츠 원본이다. 순서는 `[8,9,10,12,13,14,15,16,17,18,19,20,21,22,23,24,25]`, 실습은 `[12,18,22,24]`다.

- [x] D1a. 기존 13개 키를 유지해 문구/Focus를 교체하고 22~25를 추가한다. PlayerUnits/EndTurnButton을 연결하고 CurrentActorUnit/ActionCost/삭제된 11 참조는 새 정의에서 제거한다. AP·유닛별 턴·고정 밤 턴 수·미구현 클릭 이동 안내를 넣지 않는다.
- [x] D1b. 22=AttackEnemyOnce+AttackPractice, 24=EndPlayerTurnOnce+EndTurnOnly를 지정한다. 18은 기존 이동 목표/MoveOnly, 12는 EnterCombat/None을 유지하고 각 실습 안내/차단 문구를 채운다.
- [x] D1c. 25는 FocusTargets 비움·Anchor None·중앙 배치다. 적 응답 또는 밤 경계 배치 복귀 뒤 A2의 안정된 시작 조건으로 나타나게 하고 새 대기 시간/목표를 만들지 않는다. 코드 연결 확인이며 실제 표시 시점은 D1e 사용자 확인 대기다.
- [x] D1d. 단계 수·키·목표/제한 조합·참조·targetMapKey=1·알파=0.95·autoAdvanceSeconds 없음과 필수 문자열을 정적으로 확인한다. 정의 GUID·테이블/Catalog 연결을 유지한다.
- [ ] D1e. 사용자 Unity에서 일반 진입과 개발 직행의 배치→이동→공격→턴 종료→마지막 클릭 흐름을 확인한다. 실제 에셋 변경에 대한 기존 DataBaseAssetTests는 사용자 확인 항목으로 인계하며, 옛 기대값 실패를 새 구현 통과로 숨기거나 임의 수정하지 않는다.

**Phase D 종료:** 17단계 실사용 연결 결과와 미확인 예외를 보고한다. 한 바퀴 성공이 E의 모든 인수를 대신하지 않는다.

## 7. Phase E — 통합 인수와 인계

### Task E1: 경계 사례와 문서 마감

**Files:** 이 계획 §9, 확정 설계의 실행 상태, [기존 구현 계획](2026-10-19-tutorial-implementation-plan-Hojin.md)의 후속 안내, [T-7 입력 인계](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md). 게임 파일은 인수에서 발견한 이번 설계 범위 내 결함만 해당 Task로 되돌려 수정한다.

**Interfaces:** 앞 Phase의 공개 계약을 검수한다. 새 기능/API·시험 코드·임시 Core 조작 도구를 추가하지 않는다. 기존 T-6R/T-7/T-8의 관련 미확인만 현재 동작으로 인수한다.

- [ ] E1-1. **진입/수동 진행:** 일반 StageSelect→첫 전투, 개발 Combat 직행, 상점/이벤트 뒤 첫 전투, 실행 비대상 전투를 확인한다. 진입 연출 대기, 한 클릭 한 설명, 마지막 클릭, 창 비활성/복귀, 완료/중단 후 재진입 정책을 대조한다.
- [ ] E1-2. **실습 증거/종료:** 실제 한 칸/부분 이동, 칸·방향·광역 공격, 0피해·대신 맞음·처치, 빈 Hits·아군/중립만 피격·거절·취소를 확인한다. 넥서스 파괴/턴 종료 중 패배와 EndBattle 뒤 이력만 남는 경우는 Cancelled(BattleEnded), 대상 누락 오류/잔여 설명/Finished 없음이어야 한다.
- [ ] E1-3. **입력/회복:** 사거리 밖·행동 소진에서 다른 아군/이동/되돌리기/턴 종료, 미완료 이동/공격의 배치 복귀, 회복용 종료가 24를 선통과시키지 않음, 24의 무선택/미행동 유닛 종료를 확인한다. 전환 틈/늦은 응답의 추가 제출 차단과 적 자동 진행·정리 허용을 확인한다.
- [ ] E1-4. **표현/수명:** C3의 해상도/혼합 대상, 모델 애니메이션·무기/다중 재질, 다른 외형 효과 중 해제, 대상 비활성·파괴·풀 반환·같은 View 재바인딩을 확인한다. 일반 선택/호버와 원래 외형을 보존하고 중복 대여·잔상·외곽선 잘림이 없어야 한다.
- [ ] E1-5. **일반 전투/인계:** 완료 후 제한 해제·턴 종료 확인창·이동/공격/되돌리기·설정/포기·씬 이탈을 확인한다. 관련 기존 시험의 컴파일 문제와 옛 정책 기대값을 구분해 남기며 실행하지 않은 항목은 미확인으로 둔다. 설계/계획/입력 인계의 상태와 외부 전달 여부를 각각 기록한다.

**완료 조건:** 설계 §8의 각 인수 항목에 실제 사용자 확인 근거 또는 구체적인 미확인 사유가 대응한다. 재현되지 않은 0피해/밤 경계 등은 통과로 체크하지 않는다. 미확인 필수 항목이 남으면 전체 인수 완료로 쓰지 않는다. 후속 기능은 별도 목록으로 분리하고 이번 범위에 자동 추가하지 않는다.

## 8. 구조 검수와 확인 방법

| 기준 | 확인할 경계 |
|---|---|
| SSOT | 단계/기준점=Tutorial Buffer, 규칙/결과=Core 공개 Buffer, 성공/시작=Plan, 표시 완료=P, 진입=Progress, 문구=정의 에셋, 강조 대상=Frame, 스타일=TutorialUI 프리팹 |
| S | Reader 사실 관찰·Plan 판단·Job 실행·Policy 허용·Locator 대상 해석·Graphic 메시·Highlight 대여·Appearance 합성을 분리 |
| O | 승인한 두 목표/제한과 두 Focus만 확장, 범용 규칙 엔진/새 Runner 없음 |
| L | Accepted≠통과, 표시 완료 전 통과 없음, Cancelled≠Finished, 기존 Job/Layer 반환 계약 유지 |
| I | 좁은 Character/Combat/Result Sink와 Progress만 전달, 튜토리얼에 전체 Core/Global/BufferQuery 보관 없음 |
| D | 공개 결과/읽기 계약 의존, Core Application·InGame DB·가변 Store 우회 없음 |
| JMH/FW | Factory/Group은 생성·등록·전달, 기능은 각 소유자. Global 하위 루트 재등록 금지. UIBase 등록/UISystem 개폐·입력 Configure/수명·발급 렌더 자원 반환 경로 유지 |
| 적용 범위 | 새 MaskableGraphic을 FW-010 MeshSystem 등록 대상으로 오인하지 않음. FW-011의 자원 종류별 반환과 Color의 복원 차이를 구분. 새 Effect/Light/DB 등록이 없으면 FW-012 확장을 요구하지 않음 |
| 변경 독립성 | 문구=D, 성공 의미=B1, 입력 허용=B2, 점선=C1/C3, 모델 합성=C2, 진입=A2. 공통 수명/API가 바뀔 때만 영향받는 접점까지 확인 |

구조 기준 원본: [View 인계 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리), [JMH 지침](../framework/Framework%20Development%20Guidelines.md), [FW 계약 §6](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md#6-framework-contract).

각 Phase는 `git diff --check -- <해당 파일>`과 변경 diff/관련 호출자의 정적 대조를 수행한다. 새 미추적 파일은 별도로 경로·인코딩·공백·참조를 확인한다. 제품 소스 검색에서 자동 진행 잔여가 없는지 확인할 때 옛 설계 이력과 기존 시험 참조는 별도로 분류한다. 이를 컴파일/재생/시험 통과로 보고하지 않는다.

서비스 경로 확인은 기존 BufferSystemFactory → Buffer 그룹/IBufferQuery → TutorialPlanSources, InGameViewSystemFactory → ViewGroup의 TutorialExecution·Combat.Progress → Plan, Global UI Context → 등록된 TutorialUI 순으로 기록한다. 새로운 서비스 등록은 계획에 없다.

## 9. 상태와 실행 기록

| Phase | 상태 | 정적 확인 | 사용자 Unity 확인 | 잔여 |
|---|---|---|---|---|
| 0 | 정적 조사·사용자 재질 구성 확인 | §9.1·§9.5 | 실제 Pass 출력/화면 미확인 | 0-1c의 실제 출력은 C/E 인수에 남음 |
| A | 코드 반영·정적 검수 완료 | §9.2 | 컴파일·재생 미확인 | 수동 진행·진입/카드 수명 사용자 확인 |
| B | 코드 반영·정적 검수 완료 | §9.3 | 컴파일·재생 미확인 | 새 목표 실사용 연결은 D |
| C | 코드 반영·정적 검수 완료 | §9.5 | 컴파일·재생·실제 Pass 출력 미확인 | 강조/효과 복원 사용자 확인, 새 대상 실사용은 D |
| D | 17단계 정의·필수 코드 보완·정적 확인 완료 | §9.6 | 컴파일·재생 미확인 | D1e 일반/개발 진입·기존 DB 시험 사용자 확인 |
| E | 미착수 | 미수행 | 미확인 | 통합 인수·인계 |

- 2026-11-10 계획 작성: 사용자 설계 승인 반영. 기존 소스의 타이머/자동 진행 데이터, Sources/Facts/Plan, 입력 정책 접점, Focus/UI/모델 외형 경계를 조회해 Task로 나눴다. 코드·프리팹·정의 에셋은 수정하지 않았다. **컴파일 미확인, Unity 재생 미확인.**
- 계획 작성 당시 자체 검토: 설계 §3→D1, §4→B1, §5→B2, §6.1/6.3→A2, §6.2→C1, §6.4→A1, §6.5→C1~C3, §7→이 계획 §8, §8→E1로 대응했다. 당시 Task 10개·실행 체크 48개이며 수행 체크는 없었다. 새 계획/확정 설계의 로컬 링크 경로와 문서 공백·인코딩을 확인했다. 구설계의 StageFlow.cs 링크 1건은 HEAD부터 존재한 끊어진 역사 링크로 이번 변경에서 보존했다. 실제 정의 에셋 해시는 조사 전과 동일했다.
- 향후 각 Phase 기록에는 변경 파일, 정적 확인 결과, 사용자 확인 근거, 미확인/설계 이탈 여부, 다음 승인 대상 Phase를 남긴다. 사용자 커밋/전달과 문서 작성 사실을 구분한다.

### 9.1 Phase 0 사전 확인 — 2026-11-10

**범위와 결과:** 파일·코드 조회 및 이 실행 기록만 작성했다. 일반 새 Run의 콘텐츠 전제와 A/B의 기존 연결은 확인했다. 아군 모델의 실제 shader/Pass는 미확인이므로 0-1c와 Phase C 사전 조건을 남긴다. A~E 구현·컴파일·Play·EditMode·MSBuild·커밋·push는 수행하지 않았다. **컴파일 미확인, Unity 재생 미확인.**

#### 작업 트리와 작성 경계

- HEAD `f7b842aadece5edb52f4fe8299cbb25d57b256ca`, 브랜치 `feature/Hojin/11_03_StageSelect`에서 확인했다. 이번 Phase는 현재 로컬 상태의 조회가 목적이어서 새 worktree/브랜치를 만들지 않았다.
- 시작 시 기존 변경: `Assets/AddressableAssetsData/AddressableAssetSettings.asset`, `A`, `Assets/ProjectL/Prefabs/UI/StageEventOfferCard.prefab`, `StageOfferUI.prefab`, 이번 대화의 설계/계획 문서 4개. 제품 파일을 덮어쓰거나 정리하지 않았다.
- 현재 작업 트리의 전투/Tutorial C# 변경은 없었다. 이 사실은 다른 작업자의 미저장 작업까지 확인했다는 뜻은 아니다. 구현은 기존 역할대로 단일 작성자가 맡으며, 외부 작업자에게 연락하거나 작성자를 새로 배정하지 않았다.
- `A`의 SHA-256은 `1D6BE841B55BC2D82B29E20BB343A711C40054CB1860E8CA50C901F2D40E7A83`로 이전 조사와 동일하다. targetMapKey=1·기존 13단계·autoAdvanceSeconds=5는 아직 제품 상태다.

#### 콘텐츠 전제

| 확인 대상 | 현재 사실 | 판단 |
|---|---|---|
| 일반 새 Run 첫 전투 | `StageMap_Chapter1.asset`: mapKey=1, singleFirstLayer=1, firstStageKey=1120. StageGeneratorSystem이 FirstStageKey를 ChapterGraphSpec에 전달하고 Generator가 깊이 1의 단일 노드 2에 고정한다 | 일반 경로의 첫 전투는 무작위 보스 후보가 아니라 `Stage_LD_P20` |
| 전투 목표 | `Stage/Root/Stage_LD_P20.asset` → `Stage/Combat/Combat_LD_P20.asset`, objectiveKind=1 = EnemyHeadquartersDestroyed. 넥서스 key=20, 배치 (6,11) | 설계 13의 넥서스 승리 안내와 일치 |
| 전장·이동 여지 | `Field_13x13_P3E9.asset`: 13×13, 플레이어 배치 깊이 3. 실제 배치 가능 칸은 y=0~2에 29개. 초기 벽/장애물 목록은 비어 있고 (5,2)→(5,3)→(5,4) 구간에 지형 배치 없음 | 이동 가능한 정적 공간 존재. 임의 배치·행동 뒤에도 성공을 보장한다는 의미는 아님 |
| 초기 로드아웃 | `PlayerLoadout_1.asset`: 메인 역할=Unit_Akimbo(key 10), 서브=ShotGun(11)·Ballista(15)·Cowboy(19), 각 1명. 초기 덱 인덱스 0~3/손패 4 | 과거 문서의 MainCharacter+Sniper 조합을 현재값으로 복사하지 않음 |
| 아군 행동 | 위 4명 모두 canMoveVoluntarily=1, canBasicAttack=1, 1×1. 사거리는 각각 2/2/4/1 | 이동·공격 실습의 기본 능력 조건 충족 |
| 공격 대상 | Heavy(26)=(5,5), Demon(13)=(8,7), Cannon(25)=(2,8), Gravekeeper(17)=(6,9), 넥서스=(6,11) | 적이 실제 존재. 예: 메인 (5,2) 배치 후 (5,3) 이동 시 Heavy는 거리 2에 있음. 경로·범위의 실제 승인/연출은 Unity 미확인 |
| 개발 직행·다른 전투 | TutorialViewTarget은 map=1·현재 전투 노드·클리어 전투 없음만 검사하고 objectiveKind를 제한하지 않는다. 개발 pin은 첫 고정을 대체할 수 있음 | D/E의 기본 확인은 운영 DB 1120을 사용. 보스/별도 fixture를 첫 전투로 여는 경우 콘텐츠 전제를 별도로 확인하며 대상 필터를 이번 조사에서 바꾸지 않음 |

근거 파일: `Assets/ProjectL/DataBaseSO/InGameGroup/StageMap/Item/StageMap_Chapter1.asset`, `Stage/Root/Stage_LD_P20.asset`, `Stage/Combat/Combat_LD_P20.asset`, `Field/Item/Field_13x13_P3E9.asset`, `Loadout/Item/PlayerLoadout_1.asset`(뒤 네 경로도 같은 InGameGroup 아래); `V/Tutorial/TutorialViewTarget.cs`; Core의 `StageSystem/StageGeneratorSystem.cs`, `CoreCaculator/Contracts/StageMap/ChapterGraphSpec.cs`, `CoreCaculator/Application/StageMap/ChapterGraphGenerator.cs`는 읽기만 했다. 이 조회를 View 런타임의 InGame DB 접근으로 옮기지 않는다.

#### 모델·재질 조사

아래 프리팹 경로는 `Assets/ProjectL/Prefabs/Characters/` 아래다. `Mesh`는 MeshRenderer, `Skinned`는 SkinnedMeshRenderer다. FBX 인스턴스 내부 렌더러는 텍스트 프리팹에 완전히 펼쳐져 있지 않아 수를 추측하지 않았다.

| 역할/키 | 실제 프리팹 | 정적으로 확인한 모델 구성 | 아웃라인 적용 근거/남은 확인 |
|---|---|---|---|
| 메인 Akimbo 10 | `New/Playerable/Akimbo.prefab` | 같은 이름의 New FBX 인스턴스, 외부 재질 remap/프리팹 재질 override 없음. 총/총구 참조 존재 | 평상시 아군은 EntityCartoon으로 바꾸지 않음. FBX 내장 재질의 실제 shader/Pass 미확인 |
| ShotGun 11 | `New/Playerable/ShotGun.prefab` | New ShotGun FBX 인스턴스, 외부 재질 remap/재질 override 없음 | 위와 같음 |
| Ballista 15 | `New/Playerable/Ballistar.prefab` | Skinned 2개, 같은 Ballistar FBX의 서로 다른 내장 재질 슬롯 참조 | 두 실제 재질 모두 확인 필요 |
| Cowboy 19 | `New/Playerable/CowBoy.prefab` | New CowBoy FBX 인스턴스, 외부 재질 remap/재질 override 없음 | 실제 shader/Pass 미확인 |
| Heavy 26 | `Colored/Enemy/HeavyEnemy.prefab` | Colored HeavyEnemy FBX 인스턴스 | 정상 적 외형을 EntityCartoon으로 전환하는 코드 경로 확인, 실제 화면 미확인 |
| Demon 13 | `New/Enemy/EnemyMiner.prefab` | New EnemyMiner FBX 인스턴스 | 위와 같음 |
| Cannon 25 | `New/Enemy/EnemyCannon.prefab` | Skinned 2개, 서로 다른 FBX 내장 재질 슬롯 | 두 렌더러 모두 기존 EntityCartoon 교체 대상, 실제 화면 미확인 |
| Gravekeeper 17 | `New/Enemy/EnemyShabow.prefab` | Skinned 1개+Mesh 2개 | 몸체와 별도 부품을 같은 모델 강조에 포함할 필요. 기존 교체 대상, 실제 화면 미확인 |
| 넥서스 20 | `New/Enemy/EnemyNexus.prefab` | Colored EnemyNexus FBX 인스턴스와 별도 ParticleSystemRenderer 4개 | 본체는 적 구조물 외형 교체 대상. 파티클 4개는 모델 실루엣/경계에서 제외 |

- `UnitVisualDataTable`의 실제 키→프리팹 참조를 따라 위 표를 작성했다. `UnitVisual_MainCharacter`와 `UnitVisual_Akimbo`는 같은 Akimbo 프리팹을 참조하지만, 현재 로드아웃의 메인 역할은 key 10이다. 메인 강조는 보유 역할 사실을 읽는 기존 방식을 유지한다.
- `FieldUnitPresenter`의 캐릭터/구조물 표시 모두 `Owner.Enemy && MaxHealth > 0`에 `SetEntityCartoon(true)`를 호출한다. `EntityCartoonAppearance`는 Mesh/Skinned의 모든 재질 슬롯을 복제해 `Resources/Shared/Shaders/EntityCartoonTemplate`의 shader로 바꾼다. 공유 원본과 ParticleSystemRenderer는 이 교체 대상이 아니다.
- Cartoon와 EntityCartoon 모두 `_OutlineColor`, `_OutlineWidth` 및 `Outline`/`SRPDefaultUnlit` Pass가 있다. EntityCartoonTemplate은 해당 shader를 참조하고 disabledShaderPasses가 비어 있다. 이것은 적용 경로의 정적 근거이며 실제 렌더 성공 증거는 아니다.
- `OutlineNormalPostprocessor`는 MeshFilter와 SkinnedMeshRenderer 양쪽에 UV3 외곽선 노멀을 생성한다. 기존 임포트 캐시/애니메이션 화면까지 이번에 확인하지는 않았다.
- **C2에서 주의:** `FieldUnitView.PrepareMovementAppearance`는 현재 TMP/Canvas를 제외한 모든 Renderer를 수집하므로 파티클도 포함될 수 있다. 새 아웃라인 층과 모델 bounds는 Mesh/Skinned로 좁히되, 기존 투명도/플래시가 처리하던 다른 Renderer까지 이번에 일괄 제거하지 않는다. UnitModelAppearance가 기존 ColorHandle로 색/float를 쓰고 원래 슬롯 PropertyBlock을 복원하는 한 작성 경로는 재사용 가능하다.
- `SetEntityCartoon`은 기존 UnitModelAppearance를 해제하고 재생성한다. C2 대여는 EntityId뿐 아니라 이 외형 교체에도 무효화돼야 한다. 기존 SelectOutline Scope는 MeshFilter 복사이므로 Skinned 대체 수단으로 바로 사용할 수 없다.
- 기타 로스터로 범위를 넓힐 경우도 별도 확인이 필요하다. 예를 들어 `UnitVisual_Shielder.asset`는 prefab이 0이다. 기본 로드아웃 1에는 없으며, 이를 이유로 이번에 다른 유닛 에셋을 수정하지 않았다.

**미확인 원인과 닫는 조건:** `unity` CLI 1.0.0-beta.13의 `status --format json --project-path ...`는 `STATUS_NO_INSTANCES`를 반환했고 에디터 탐색 제한 경고도 포함했다. manifest에 `com.unity.pipeline`도 없다. 따라서 ‘Unity가 반드시 꺼져 있다’고 단정하지 않고 연결 가능한 조회 경로가 없었다고 기록한다. 패키지 설치/CLI 갱신/에디터 실행/재임포트는 하지 않았다. C 시작 전 사용자가 Unity에서 위 아군 4종의 모든 실제 모델 재질에 `_OutlineColor`·`_OutlineWidth`와 활성 Outline Pass가 있는지 확인해야 한다. 미지원이면 설계 §6.5대로 표현 방식 변경을 먼저 결정한다. 현재는 **미지원 확정이 아니라 지원 여부 미확인**이다.

#### 제거 대상·조립·기존 시험 영향

| 대상 | 실제 참조/소유 | A에서의 처리 경계 |
|---|---|---|
| TutorialViewTimer | 제품 생성자는 TutorialViewSystem 한 곳. clock/readFocus도 여기서 자동 진행에만 사용. 직렬화 GUID 참조 없음 | Timer/만료 API·Facts·Job·정의 시간 항목을 함께 제거 가능 |
| TutorialViewFocusNotifier | UIDisplay Attach/Detach 구독 → ViewSystem.NotifyFocusChanged → Timer.SetFocused. Setup 생성과 TutorialUI 필수 참조. GUID 참조는 TutorialUI.prefab 한 곳 | UI 참조/구독/Setup/프리팹 컴포넌트를 같은 A1에서 제거. 다른 창 포커스 입력 기능과 공유되지 않음 |
| TutorialViewCardVisibility | UIDisplay만 생성·Prepare/Release/Detach 호출 | A2에서 튜토리얼 카드 개폐 소유권 제거 가능 |
| TutorialViewCardVisibilityObserver | 위 helper만 런타임 AddComponent·구독·해제. 직렬화 GUID 참조 없음 | helper와 같이 제거 가능. DeckCardList 자체는 유지 |
| ViewSystem 생성 | InGameViewSystemFactory 한 곳, 기존 ExportServices 경로 | A1의 생성자 인수 제거를 이 호출에 반영 |
| UIDisplay 생성 | TutorialViewSystem.PostBindService 한 곳 | A1 포커스 인수/A2 UI 인수 제거를 이 호출에도 반영. A2 Files 목록의 누락을 보완했으며 설계 변경은 아님 |
| Sources.From | TutorialPlan.Register 한 곳. 기존 좁은 Store/Sink·Execution 묶음 | A2의 Progress, B1의 CharacterReadSink 추가를 같은 조립 경계에서 처리 |

- 서비스 경로 확인: 기존 `InGameViewSystemFactory → InGameViewSystemGroup.TutorialExecution/Combat → Combat.Progress`가 존재하며 `ICombatProgressView`의 실제 선언은 `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/ICombatProgressView.cs`다. 필요한 네 상태가 이미 공개되어 새 서비스 등록·Core 수정 요청은 필요하지 않다. UIDisplay의 UI 서비스는 기존 `GlobalGroup`의 UI Context에서 받는다.
- 검색은 `Assets` 전체 제품 참조와 `.prefab/.unity/.asset`의 스크립트 GUID를 확인했다. 시험은 ignored 파일까지 `rg -uu`로 확인했다. 현재 EditMode C#은 487개지만 `Tutorial*Tests` 파일, 타이머/포커스/임시 카드 helper, 삭제 API/enum, TutorialViewSystem/UIDisplay/Sources 생성자의 직접 시험 참조는 없었다. 옛 문서에 적힌 튜토리얼 시험을 현재 존재하는 시험으로 간주하지 않는다.
- 관련 직접 접점은 `Assets/Tests/Editor/CombatField/StructureHitPresentationTests.cs`의 `InspectionPolicy_DeploymentKeepsPendingAndModalBoundaries`다. CombatInputFacts를 생성하고 `CanSelectUnit`을 확인하며 튜토리얼 제한은 default(None)다. 현재 계획은 해당 생성자 변경이 필요 없으므로 사전 수정하지 않는다. B에서 일반 정보/선택 경로를 유지하는 확인 항목으로 남긴다.
- `Assets/Tests/EditMode/CoreIntegration/DataBaseAssetTests.cs`는 현재 존재하나 Tutorial 참조는 없다. 에셋 변경 시 기존 DB 교차 검증의 사용자 확인 항목은 유지하되, 이 시험이 새 17단계 콘텐츠까지 검증한다고 보고하지 않는다. 이번 조사에서 시험 코드 추가/수정/실행은 없었다.

#### Phase 경계 판단

| 후속 범위 | 사전 판단 | 남긴 조건 |
|---|---|---|
| A 수동 진행·진입/카드 수명 | 현재 공개 계약과 제거 대상 참조로 계획대로 진행 가능한 근거 확보 | 사용자 Phase 실행 지시와 기존 작성 역할 유지. 이번에 구현하지 않음 |
| B 실습/입력 | Character/Combat/Result/Progress 공개 경계 재사용, Core 직접 수정 필요 없음 | 실제 적중·종료·회복/늦은 제출 재생은 D/E에서 확인 |
| C UI/모델 강조 | UI 위젯과 기존 모델 합성 소유권은 설계와 일치 | **아군 4종 실제 재질 지원 확인 전 모델 적용을 확정하지 않음**. 외형 교체·VFX 제외·다중 슬롯을 확인표에 유지 |
| D 콘텐츠 | 일반 새 Run의 1120·로드아웃 1이 설계의 정적 전제를 만족 | 개발 보스/fixture와 실제 배치/이동/공격 결과는 별도 확인 |
| E 인수 | 옛 튜토리얼 시험 목록 대신 현재 파일과 사용자 재생 근거로 인수 | 미확인 재질/조작을 통과로 기록하지 않음 |

SSOT·SOLID·JMH/FW·변경 독립성 검수에서는 계획의 책임 배분을 유지했다. A2의 생성 호출 파일 보완 외에 구현 정책을 바꾸지 않았고, 미확인 재질을 이유로 공용 셰이더/SelectOutlineSystem·Core·FieldCamera·Architecture 작업으로 확대하지 않았다. 이번 결과는 정적 자기 검수이며 독립 코드 리뷰/사용자 인수 완료를 의미하지 않는다.

### 9.2 Phase A 구현·정적 검수 — 2026-11-10

**범위와 상태:** 사용자의 “Phase A 구현부터 진행해주세요” 지시에 따라 A1·A2를 코드·에셋에 반영했다. 같은 HEAD/브랜치에서 작업했고 기존 사용자 변경을 보존했다. 아래 체크 완료는 구현·정적 대조의 완료이며 사용자 재생 인수가 아니다. **컴파일 미확인, Unity 재생 미확인.** Test 코드·시험 도우미 작성/수정, MSBuild·EditMode·Play 실행, 커밋·push, 외부 메시지 전송은 하지 않았다. Phase B~E는 시작하지 않았다.

#### 반영 내용

- **자동 진행 제거:** `Continue=3`으로 이름만 바꾸고 Snapshot/표시 완료 Job을 맞췄다. Plan 만료 전이, `TutorialExpireJob`, 실행 인터페이스의 시간 API, Facts의 만료 사실, ViewSystem의 시계·포커스 의존성, 정의의 자동 진행 시간 필드를 제거했다. `Expire=6`은 결번이며 `PassPractice=7`을 보존했다. 사용자 Continue의 session/revision 검사와 기존 실습 결과 통과는 유지한다.
- **진입 완료 대기:** Plan 조립에서 `service.ViewGroup.Combat.Progress`를 좁은 `ICombatProgressView`로 Sources에 전달한다. 필수 서비스가 없으면 조립 오류를 낸다. FactsReader가 연결·진입 연출·제출 정지·포기 요청을 읽고, `CanStartExplanation` 한 곳에서 판단한다. 기존 시작 Job의 입력 대여 직전 재검사도 이 함수를 사용한다.
- **카드 표시 소유권:** UIDisplay의 Prepare/Release/Detach 호출과 UI 서비스 의존성을 제거했다. 정상 배치 안내·카드 등장/닫기는 기존 `CombatDeploymentPresentation`이 맡는다. Tutorial UI의 기존 UISystem 바인딩·개폐와 입력 Layer 반환/오류 정리는 유지했다.
- **전용 파일 정리:** `TutorialViewTimer`, `TutorialViewFocusNotifier`, `TutorialViewCardVisibility`, `TutorialViewCardVisibilityObserver`의 `.cs`와 `.meta`를 함께 제거했다. 프리팹의 notifier 필드·컴포넌트와 Setup 생성 코드도 정리했다. Missing Script 에셋은 건드리지 않았다.
- **공용 조립 접점 인계(미전달):** `InGameViewSystemFactory`의 TutorialViewSystem 생성자 인수에서 시계/포커스 인수만 제거했다. 서비스 키·등록 순서·Builder·Group·다른 시스템 생성은 변경하지 않았다. 생성자와 실제 호출 지점은 A1/A2의 Interfaces를 따른다.
- 코드의 옛 시간/포커스 설명을 실제 계약에 맞췄다. `ITutorialViewDisplay`와 `TutorialViewPresentationKind`는 주석만 수정했으며 기능 범위 추가는 없다.

#### 정적 확인 근거

1. `git diff --check` 대상 변경의 공백 오류 없음. 수정 C#은 UTF-8/LF로 정리했다. 변경 경로에 Core·FieldCamera·Architecture·시험 파일이 없음을 확인했다.
2. `rg -uu`로 Assets의 C#·meta·prefab·unity·asset을 검색했다. 삭제한 네 helper 이름/GUID, `ContinueOrTimeout`, `AutoAdvanceSeconds`/`autoAdvanceSeconds`, `TutorialExpireJob`, `IsTimerExpired`, `StartTimer`, `TutorialPlanStepKind.Expire`의 잔여 참조 없음. 현재 checkout에 관련 직접 시험 참조가 없으므로 시험 호출 수정도 없다. 옛 문서의 시험 통과 기록을 이번 확인으로 계승하지 않는다.
3. TutorialViewSystem·UIDisplay·Sources.From·Sources·Facts 생성 호출은 각각 기존 한 곳이며 새 인수와 대응한다. 기존 `TutorialStartExplanationJob`은 `FactsReader.Read → CanStartExplanation → TryBorrowInput` 순서를 유지한다.
4. 시작 조건 미충족 시 Plan은 Nothing/빈 Job 목록을 반환한다. 별도 대기 Job·타이머·수동 카드 열기 없이 `CombatViewSystem.Tick → CombatEntryPresentation.Tick → CombatDeploymentPresentation.Tick`이 기존 맵→안내→카드 수명을 진행한다. 실제 화면 타이밍은 아래 사용자 확인 대상이다.
5. A 시작 시 보관한 원문과 비교해 정의 에셋은 `autoAdvanceSeconds: 5` 한 줄만 제거됐음을 확인했다. 기존 13단계 전체, `targetMapKey: 1`, 덮개 알파 `0.95`는 그대로다. 프리팹도 notifier 필드·루트 컴포넌트 참조·해당 MonoBehaviour 블록만 제거됐으며 나머지 레이아웃/직렬화 값은 동일하다. 두 파일의 GUID와 막힌 입력 안내 `blockedNoticeSeconds: 2`를 보존했다.
6. 독립 읽기 전용 코드 검수 결과 Critical/Important 지적 없음. Minor 1건(Attach의 삭제된 창 활성 구독 설명)은 수용해 해당 주석과 인터페이스의 같은 옛 설명을 수정했다. 검수자가 제외한 실행/컴파일 확인은 아래 사용자 확인 항목으로 유지하며, B~E·기존 사용자 변경은 승인 범위 밖이라 이번에 구현/검증하지 않는다.

#### 구조 검수

| 기준 | 이번 변경의 근거 |
|---|---|
| SSOT | 단계/진행 원본은 Tutorial Store, 진입 연출 사실은 Combat.Progress, 설명 시작 판정은 CanStartExplanation, 카드 개폐는 CombatDeploymentPresentation 한 곳이다. 파생 사실을 별도 상태로 저장하지 않는다. |
| S / O | Sources·FactsReader는 의존성과 사실, Plan은 판단, Job은 실행, UIDisplay는 표시를 맡는다. 기존 공개 Progress 계약으로 연결하며 새 Runner/범용 레지스트리/전역 이벤트를 추가하지 않았다. |
| L / I / D | 기존 Job 완료·session/revision·표시 완료/정리 계약을 유지한다. 전체 ViewGroup을 보관하지 않고 기존 읽기 인터페이스만 주입한다. 제품 구현체와 생성 호출을 함께 맞췄다. |
| JMH/FW | 기존 Factory 생성·ExportServices/AddModule·Builder 수명을 보존한다. 입력은 기존 receiver/Layer/Submit, UI는 기존 UIId/Global Context 경로다. Core 계산/DB 경계를 우회하지 않는다. |
| 기능 변경의 독립성 | 카드 등장 순서를 조정할 때 튜토리얼 전용 카드 개폐 helper를 함께 고칠 필요가 없어졌다. 공통 경계는 Progress 읽기와 Factory 생성자뿐이며 전투 진입 연출·카드 내부를 수정하지 않았다. 자동 진행 제거는 튜토리얼 계약/표시/정의에 한정했다. |

#### 사용자 Unity 확인 대기

- 첫·중간·마지막 설명에서 5초 이상 기다려도 유지되는지, 한 클릭에 한 단계만 진행하는지, 마지막 클릭 전 완료되지 않는지, 실습이 클릭만으로 통과하지 않는지 확인한다. 창을 나갔다 돌아와도 같은 설명이어야 한다.
- 일반 새 Run의 첫 전투에서 맵→배치 안내→카드 등장 완료 뒤 설명 8이 표시되는지 확인한다. 8·9는 이미 보이는 실제 카드를 강조하고 튜토리얼 정리가 정상 카드를 닫지 않아야 한다.
- 막힌 입력 안내는 기존 2초 뒤 사라져야 한다. 진입 도중 포기·오류·씬 이탈과 튜토리얼 취소 시 설명 UI/독점 Layer가 남지 않는지 확인한다.
- 삭제된 스크립트의 Missing Script·참조 오류와 컴파일 오류가 없는지 확인한다. 정의 에셋을 수정했으므로 기존 `DataBaseAssetTests`도 사용자 확인 항목에 포함한다. 이 시험에 Tutorial 전용 검증이 없다는 §9.1의 한계는 그대로다.

**남은 작업:** Phase A 재생 인수, Phase 0의 아군 실제 재질/Pass 확인(C 사전 조건), 승인 순서에 따른 B~E다. 새 공격/턴 종료 실습·점선/모델 아웃라인·17단계 문구는 이번 변경에 포함하지 않았다. 여기서 Phase A 보고 후 멈춘다.

### 9.3 Phase B 구현·정적 검수 — 2026-11-10

**범위와 상태:** 사용자 “Phase B 구현 진행해주세요” 지시로 B1/B2의 코드 반영·정적 검수를 완료했다. A 및 기존 사용자 변경을 보존하며 같은 HEAD `f7b842aadece5edb52f4fe8299cbb25d57b256ca`에서 이어 작업했다. **컴파일 미확인, Unity 재생 미확인.** A의 추가 재생 증거도 전달받지 않았으므로 미확인을 유지한다. 새 Test/도우미 작성·기존 시험 수정·MSBuild·EditMode·Play·커밋·push·외부 전송은 하지 않았다. C~E는 시작하지 않았다.

#### 구현·정적 대조

- 목표 `AttackEnemyOnce=3`·`EndPlayerTurnOnce=4`, 제한 `AttackPractice=2`·`EndTurnOnly=3`과 StepRule 조합 검증을 추가했다. 기존 배치/이동 조합과 실습 안내·제한 시 막힌 입력 문구 필수 검증을 유지한다.
- Sources는 기존 BufferQuery에서 `ICharacterBufferReadSink`만 추가로 받으며 Facts에는 새 성공 사실 두 개를 더한다. `TutorialSessionChanges.Enter`의 실습 기준점을 재사용한다. 별도 기준점·적 턴 완료 상태·서비스 등록을 만들지 않는다.
- FactsReader는 같은 활성 전투·Current 존재·Completion 없음·View 연결을 확인한 뒤만 실습 결과를 읽고 결과 자체의 BattleInstanceId도 대조한다. `CombatStateBuffer.PrepareCommand`가 현재 BattleInstanceId를 결과에 넣는 것을 확인했다. 종료 뒤 정리된 대상을 조회하지 않고 기존 BattleEnded 우선을 유지한다.
- 공격은 수락된 일반 Attack·Succeeded·기준점 뒤·표시 완료에 실제 Hits의 적 유닛/적 넥서스 ID가 있어야 한다. 피해량/생존/선택/CurrentStage 필터는 없다. 활성 전투의 미확인 ID는 결과 순번·대상 ID를 포함한 오류다. 한 Hit가 인정돼도 나머지 ID의 오류를 숨기지 않는다.
- 턴 종료는 수락된 EndPlayerTurn에 CombatTurnEvent.PlayerTurnEnded가 있고 표시까지 끝나야 한다. 회복 중 이전 종료는 새 실습 기준점에서 제외한다. EnterCombat의 StartResultSequence와 이동의 실제 진입 칸/부분 이동 의미는 유지한다.
- `CombatInputPolicy`가 명령/미리보기 허용표를 소유한다. None의 기존 공통 Gate를 보존하고, 제한 실습의 Pending=false는 모든 새 제출을 차단한다. Pending=true의 MoveOnly/AttackPractice는 배치 복귀 후 메인/서브 배치·회수·전투 진입과 배치 Preview로 회복할 수 있다.
- Router의 취소/공격 토글과 ActionButtons의 가용성을 맞춰 이미 켜진 공격 모드는 제한 중에도 끌 수 있다. 새 공격 모드 진입은 제한을 따른다. 정보 선택은 기존 `CombatSceneInputDriver → CanSelectUnit → Inspection` 경로이며 `CanInspectUnit`의 공격 범위 Preview와 분리돼 있어 유지했다.
- 최종 공격/배치/회수/되돌리기는 Gate의 CanSubmit, 이동의 늦은 응답은 `CombatPathInput.OnPreview → AllowsInput/ValidatePlanning → CombatMovementInteraction`의 현재 CanEditPath를 따른다. 기존 직접 이동 Submit 앞의 검사도 유지한다. 이미 수락된 결과와 적 자동 진행은 변경하지 않는다.
- 행동 버튼·정보창 행동 표시와 범위/가용성 조회는 같은 Policy를 사용한다. 카메라·정보 선택·설정·포기 경로, 제한 중 턴 종료 확인창 생략/일반 전투 확인창은 기존대로다. 실제 금지 공격/되돌리기 키 등은 기존 ReportBlockedInput을 사용하며 정책 조회·버튼 가용성·호버는 보고하지 않는다.

#### 허용표 대조

4 제한 × 2 국면 × 2 Pending의 16개 조합을 정적으로 대조했다. 아래는 튜토리얼 제한만 나타내며 국면·Layer·입력/명령 대기·표시/종료·Core 가용성은 추가로 검사한다. 정책 원본은 설계 §5.1이다.

| 제한 | Pending | Combat 명령 / Preview | Deployment 명령 / Preview |
|---|---|---|---|
| None | true·false | 기존 그대로 | 기존 그대로 |
| MoveOnly | false | 없음 / 없음 | 없음 / 없음 |
| MoveOnly | true | Move·EndPlayerTurn / Move·MoveRange·ActionAvailability | PlaceMainCharacter·PlaceUnit·ReturnUnit·EnterCombat / MainCharacterPlacement·UnitPlacement |
| AttackPractice | false | 없음 / 없음 | 없음 / 없음 |
| AttackPractice | true | Move·Attack·UndoMove·EndPlayerTurn / Move·MoveRange·AttackRange·Attack·UnitAttackRange·ActionAvailability | PlaceMainCharacter·PlaceUnit·ReturnUnit·EnterCombat / MainCharacterPlacement·UnitPlacement |
| EndTurnOnly | false | 없음 / 없음 | 없음 / 없음 |
| EndTurnOnly | true | EndPlayerTurn / 없음 | 없음 / 없음 |

**검증 한계:** Assets/Tests를 포함해 생성자/새 enum 참조를 검색했고 직접 호출 수정이 필요한 기존 시험은 없었다. StructureHitPresentationTests의 CanSelectUnit은 변경하지 않았다. 새 테스트/컴파일/재생은 수행하지 않았다. 기존 13단계 정의·프리팹은 B에서 바꾸지 않아 새 공격·턴 종료 실습의 재생은 D의 17단계 연결 뒤 확인한다.

#### 독립 검수와 보완

- 읽기 전용 독립 코드 검수는 Critical 0·Important 0·Minor 1이었다. 리뷰어의 별도 sandbox에서 B 시작 기준 차이 파일에 접근할 수 없어 현재 파일과 HEAD diff를 읽고 A의 타이머/수동 진행/Progress 변경을 제외해 검수했다.
- 지적은 EndTurnOnly에서 이동 Drag가 차단되지만 안내가 없는 것이었다. **처리 결정:** 진행을 막는 결함은 아니지만 B2e에 명시된 동작 누락이므로 이번 완료 보완으로 수용했다. 정보 클릭을 금지 시도로 취급하지 않고 실제 다른 칸으로 놓는 시도만 안내한다. 의도 구분이 어긋나면 정상 정보 클릭에 불필요한 안내가 생기므로 아래 재생 확인을 필수로 남긴다.
- `CombatPathInput`은 제한 중 선택 아군 앵커 누름의 개체/칸/결과 순번만 보관하고, 다른 정상 필드 칸에 release할 때 최신 전투·선택·정책·실습 대기를 재검사해 기존 ReportBlockedInput을 한 번 부른다. 새 경로/미리보기/명령은 만들지 않는다. 같은 칸 클릭·호버·UI/필드 밖·취소·포커스 상실·Detach·중복 release는 안내하지 않는다.
- 기존 Poll은 새 응답이 없어도 마지막 Preview를 매 프레임 전달하므로, 유효한 금지 제스처를 그 과거 응답으로 취소하지 않게 했다. 정상 경로의 Preview 소비/최종 제출 검사는 그대로다. 이 보완은 작성자가 정적으로 재대조했으며 독립 리뷰를 다시 실행하거나 Unity에서 검증한 것은 아니다.
- 새 안내 의존성은 기존 `CombatSceneInputSources.ReportBlockedInput → CombatSceneInputFactory → CombatPathInput`으로 전달한다. 공용 조립 파일은 이 인수 전달·필수 null 검사만 바뀌었으며 외부 공유는 미전달이다. 정책/아키텍처 변경 없이 계획의 B2 Files에 실제 연결 파일을 보완했다.
- 리뷰가 판단에서 제외한 A 과거 변경과 C/D 후속 구현은 그대로 경계를 유지한다. 희생·처치·밤 경계·연속 입력·컴파일은 아래 사용자 확인으로 남긴다. 금지된 시험 작성/실행은 하지 않는다. 문서 검수 보류 항목은 이번 실행 기록과 T-7 인계 갱신으로 처리한다.

#### 구조·최종 정적 확인

- **SSOT:** Tutorial Buffer는 목표·기준점·제한, Policy는 허용표, FactsReader는 공개 결과 사실, Plan은 목표 대응을 소유한다. 이동 안내 후보는 입력 제스처 정체성만 보관하며 성공 사실/진행 상태를 복제하지 않는다.
- **SOLID S/O:** 기존 FactsReader/Plan/입력 소유자의 역할로 두 목표만 확장했다. 범용 레지스트리·새 Runner를 추가하지 않고 표시나 Core 계산을 진행 판정에 섞지 않는다. **L:** 기존 Job 완료·수명·session/revision/취소 계약을 유지한다. **I/D:** Character 읽기 Sink와 기존 안내 대리자만 주입하며 Core 구현/전체 서비스 그룹에 결합하지 않는다.
- **JMH/FW:** `BufferSystemFactory → CharacterBuffer.ExportServices → BufferSystemGroup.CharacterReadSink → Sources.From`의 기존 생성/등록/조회 경로를 사용한다. Tutorial은 기존 View 모듈/Group 경로를 유지한다. 루트 서비스·UIId·Builder·입력 Action·UnityEvent를 추가하지 않았다.
- **변경 독립성:** 목표 의미를 바꾸는 곳은 Tutorial Facts/Plan, 제한을 바꾸는 곳은 CombatInputPolicy다. 금지 이동 제스처만 이동 입력 소유자에 두고 Factory는 조립만 한다. 일반 전투·카메라·UI 프리팹 내부를 함께 수정하지 않는다. 공통 경계는 기존 Policy/입력·결과 Sink와 안내 대리자다.
- B 변경은 기존 C# 12개이며 신규/삭제 C#·meta는 없다. B 시작 원문과 비교해 A 변경을 보존했다. 생성자 호출 검색·UTF-8/LF·공백 및 변경 경로를 확인했다. 최종 `git diff --check` 종료 코드 0, 12개 C#의 인코딩/줄바꿈 문제 0, Core/Framework/Render/시험 변경 0이다. 정의·프리팹의 SHA-256도 A 종료 값과 각각 동일했다. Core·FieldCamera·Architecture·시험·씬·프리팹·정의 에셋은 B에서 변경하지 않았다. 정적 확인을 컴파일 성공으로 기록하지 않는다.

#### 사용자 Unity 확인 대기

1. 컴파일 확인 후 기존 13단계 이동 실습에서 수동 턴 종료→배치 복귀→메인/서브 재배치·회수→전투 재진입→실제 이동을 확인한다. 실습 안내 전/통과 뒤 추가 이동·턴 종료가 차단돼야 한다.
2. 일반 정보 선택·카메라·설정·포기·공격 모드 취소와 튜토리얼 종료 뒤 일반 전투/턴 종료 확인창을 확인한다.
3. D의 17단계 연결 후 공격의 0피해/다중 대상/희생/사망 적·빈 Hit·아군/중립만 Hit, 넥서스 파괴의 BattleEnded 우선, 턴 종료 실습의 회복용 과거 종료 제외·표시 전 통과 없음·적 자동 진행·밤 경계를 확인한다.
4. D 연결 후 EndTurnOnly에서 아군을 다른 칸으로 드래그하면 명령 없이 안내 1회, 같은 칸 클릭·호버·UI/필드 밖 놓기·취소·창 복귀·중복 release에는 안내 없음, 표시 전/통과 뒤에는 추가 제출 없음과 늦은 Preview 응답 차단을 확인한다.

**Phase 종료:** B1/B2 코드·정적 검수 지점에서 보고하고 멈춘다. C의 아웃라인, D의 17단계 콘텐츠, E 통합 인수는 미착수다. Phase 0의 아군 실제 재질/Pass 확인은 C의 선행 확인으로 남는다.

### 9.4 Phase C 부분 작업 — 점선 기반 코드·재질 확인 대기 (2026-11-10)

**상태:** 사용자의 Phase C 구현 요청으로 착수했으나 **Phase C 완료가 아니다.** 0-1c/§9.1에 남긴 아군 4종의 실제 재질 지원 확인이 없어 모델 아웃라인을 구현하거나 지원을 가정하지 않았다. 이 확인과 독립적인 점선 위젯·스타일 자료형·좌표 변환만 반영했다. 아직 튜토리얼 화면에 새 점선이 표시되는 단계는 아니다. **컴파일 미확인, Unity 재생 미확인.**

#### 승인·확인 경계

- 사용자의 직접 구현 요청을 이번 Phase 범위의 Codex 작성 지시로 적용했다. 기존 개인 역할 규칙을 전역 변경하거나 D/E 실행 승인으로 넓히지 않는다. A/B와 사용자 변경이 함께 있는 기존 checkout에서 이어갔으며 커밋·push하지 않았다.
- `unity status --format json --project-path ...`를 다시 조회했으나 `STATUS_NO_INSTANCES`와 에디터 탐색 제한 경고가 반환됐다. Unity가 반드시 꺼졌다는 뜻으로 해석하지 않는다. 패키지 설치·CLI 갱신·에디터 실행·재임포트는 하지 않았다.
- 사용자에게 Akimbo·ShotGun·Ballista·Cowboy의 모든 실제 Mesh/Skinned 재질에 `_OutlineColor`·`_OutlineWidth`와 활성 `Outline` Pass가 확인됐는지 질문했고, **2026-11-10 “아직 확인하지 않았음” 답변을 받았다.** 지원 확인 근거는 아직 없으며 미지원으로 판정한 것도 아니다. 모델 방식 변경이나 강제 EntityCartoon 전환은 구현하지 않았다. 답변 이후에는 이 기록만 갱신하고 모델 구현·프리팹 연결을 진행하지 않았다.

#### 이번 변경

| 파일 | 변경 | 남은 연결 |
|---|---|---|
| `T/TutorialHighlightStyle.cs` + `.meta` | 공통 색·점선 두께/길이/간격·모델 폭의 직렬화 자료형과 값 검증 | TutorialUI의 단일 필드 소유·Attach 검증·프리팹 직렬화(C1c/C3) |
| `T/TutorialFocusBorderGraphic.cs` + `.meta` | 입력을 받지 않는 정적 점선, 화면 영역/스타일/로컬 좌표 비교, 재사용 목록, Clear | Frame UI 영역 전달·프리팹 배치·표시 수명(C1b/C3) |
| `T/TutorialFocusGeometry.cs` + `.meta` | 화면 픽셀 → Overlay Canvas 로컬 좌표 변환, Canvas 단위 여백 | Frame의 모델 확장 영역/점선 외곽 회피 영역(C1e/C2d) |
| `T/TutorialViewOverlay.cs` | 기존 private 좌표 변환을 위 공통 함수로 이전 | 기존 덮개 동작은 사용자 Unity 확인 필요 |

- C1a/b(Frame·대상 추가), C2(모델 층·대여), C3(UI/프리팹/수명)는 미구현이다. C1c/e는 일부만 반영했으므로 체크하지 않는다. 기존 enum/버튼 패널/Locator/UI/UIDisplay/Setup/모델 외형은 이번 시작 상태를 유지한다.
- 대상/문구/진행/전투 입력을 새 그래픽에 넣지 않았다. UI 점선은 기존 UIId.Tutorial 안에 연결할 내부 위젯이며 별도 UIBase·서비스·receiver·UnityEvent를 만들지 않는다.
- SSOT: 좌표 변환만 공유하고 스타일 소유는 C3에서 TutorialUI 한 곳에 연결할 예정이다. SOLID: 그래픽=메시, Geometry=변환, Style=표현 값으로 분리; 기존 MaskableGraphic 계약을 사용하며 진행/모델/서비스 의존성을 추가하지 않는다. JMH/FW: 생성·등록·Group Export·UISystem 개폐 경로는 수정하지 않는다. 변경 독립성: 모델 재질/합성과 독립적으로 점선 기반 코드만 검수한다.

#### 정적 확인·재개 조건

- 변경 C#은 LF/UTF-8, 새 `.cs` 3개에 `.meta`를 함께 추가했다. 이번 변경 파일의 공백/인코딩 및 Overlay 한정 `git diff --check`를 확인했다. 전체 checkout에 `core.autocrlf=false`를 적용한 광역 확인에서는 이번 작업 밖 Font 에셋·기존 파일의 CRLF 공백 경고가 발생했으므로 전체 저장소 통과로 기록하지 않는다. 해당 파일은 수정하지 않았다. 새 시험/도우미·MSBuild·EditMode·컴파일·Play는 수행하지 않았다.
- 기존 TutorialUI 프리팹 SHA-256 `D0811BD3FEC8B95C9EA5EA103FA0E4807E3551AD34D65C6C1587044F3CA4C764`, 13단계 정의 SHA-256 `A141BA8E8391D26C9EDEC339446D555FD2EF255BBF72A7F6362117A5184B1E5C`를 유지한다. 17단계 내용은 여전히 D 범위다.
- 별도 실행 문맥의 정적 검수는 Critical 0 / Important 0 / Minor 1이었다. Minor(매우 짧은 점선/간격에서 VertexHelper 65,000 정점 한도 초과)를 수용해, 전체 영역의 예산을 `SetScreenRects`의 상태 변경 전 및 메시 재생성 전에 확인하고 UIId/필드명을 포함한 구성 예외로 거절한다. 점선 반복은 계산된 개수의 정수 반복을 사용한다. 기본 스타일은 유지했다. 수정 후에는 자체 정적 대조만 했으며 두 번째 외부 검수/실행 검증을 했다고 기록하지 않는다. 프리팹에 연결하지 않았으므로 시각 품질·배율 변경·입력 차단·정리 수명 통과로 기록하지 않는다.
- **재개 조건:** §9.1의 실제 재질 지원 확인을 받은 뒤 C1의 Frame/대상 해석 → C2 합성/대여 → C3 연결/정리 순서로 진행한다. 미지원이면 설계 §6.5의 방식 변경을 먼저 사용자와 결정한다.

### 9.5 Phase C 재개·코드 반영 완료 — 사용자 재질 구성 확인 (2026-11-10)

**결과:** C1~C3 코드와 TutorialUI 프리팹 연결, 정적 검수를 완료했다. **컴파일 미확인, Unity 재생 미확인, 실제 Outline Pass 출력 미확인.** 사용자에게 Phase C 검수 지점으로 보고하고 멈춘다. D의 17단계 정의 교체와 E 통합 인수는 진행하지 않았다.

#### 재개 근거와 확인 수준

- 사용자 확인: “아군 4종 모두 Cartoon/Opaque이고 외곽선 색과 폭 항목이 있어요. Pass는 아직 확인 못 했는데, 이 상태로 Phase C를 이어서 진행할 수 있나요?” 기존 Phase C 구현 지시에 이어 이 구성 확인을 근거로 코드를 진행했다. 앞 §9.4의 재질 확인 대기 기록은 당시 이력이다.
- 확인된 사실을 분리한다. **실제 아군 재질의 Shader/표면/속성 존재는 사용자 확인**, **Cartoon/EntityCartoon의 Outline Pass 선언과 CartoonMaterialGUI의 Opaque → Pass 활성 처리는 소스 확인**, **실제 개별 재질 Pass 상태·렌더러 실행·화면 출력은 미확인**이다. 마지막 항목을 완료로 바꾸거나 0-1c 전체를 체크하지 않는다.
- 모델 강조 시작 때 실제 원래 재질의 두 속성, `FindPass("Outline")`, `GetShaderPassEnabled("Outline")`를 모두 확인한다. 조건이 부족하면 모델/재질 슬롯을 포함한 구성 예외로 드러내며 성공으로 생략하거나 Pass/셰이더를 강제 변경하지 않는다. 이 검사는 실제 렌더 출력 검증을 대체하지 않는다.
- 재개 기준 HEAD는 `e53040cea`, 브랜치는 `feature/Hojin/11_03_StageSelect`였고 작업 트리는 깨끗했다. 앞 A/B/C 일부가 포함된 현재 checkout에서 이어갔다. 사용자의 직접 Phase C 구현 범위만 수행했으며 개인 역할 규칙을 전역 변경하지 않았다. 커밋·push하지 않았다.

#### 구현과 소유권

| 작업 | 반영 내용 |
|---|---|
| C1 대상·Frame | PlayerUnits=12·EndTurnButton=13. 생존 Player/Field 전체와 실제 턴 종료 버튼을 읽음. EnemyUnits는 첫 생존 적 유지. 한 Frame에서 UI/모델/구멍/앵커를 산출하고 대상·개체 중복을 제거. 비어 있는 대상/None 앵커 정상 처리 |
| C1/C3 표현 | 스타일 소유는 TutorialViewUI 한 필드. 공통 여백은 holePadding 한 필드. 모델 폭은 월드 bounds를 확장해 투영하고, UI 점선 외곽과 여백까지 설명 회피 영역에 포함. 손패 전체는 한 점선, 모델에는 점선을 만들지 않음 |
| C2 합성 | UnitModelAppearance의 기존 ColorHandle에서 아웃라인 색/폭을 합성. 슬롯 MPB → 슬롯이 비어 있을 때 전체 MPB → 원래 Material의 유효 값을 캡처. 다른 층이 남으면 원래 외곽선 값과 현재 효과를 함께 쓰고, 마지막 층이 끝나면 원래 슬롯 MPB/재질로 복원 |
| C2 수명 | ICombatUnitOutline 대여와 기존 Slot 패턴. EntityId·Bind 버전·현재 Slot·외형 참조 검사. Bind/풀 반환/OnDisable/외형 교체·해제/OnDestroy에 반환. 실제 모델 bounds는 Mesh/Skinned만 포함하고 HUD/TMP/파티클 제외. 기존 파티클 등의 투명도/플래시 처리는 보존 |
| C3 조립·정리 | UIDisplay가 Frame과 TutorialModelHighlight를 소유. 같은 대여는 유지하고 사라진 대상/재바인딩/무효 대여/스타일 변경만 반환·교체. UI는 명시적 BindHighlightCleanup 접점과 UIBase.OnUIHidden으로 대여를 정리. 설명 전환·실습·Close/Detach·오류에서 일부 반환이 실패해도 나머지 정리/닫기를 시도 |
| C3 에셋·Setup | 기존 TutorialUI 프리팹에 FocusBorder 하나와 스타일 연결. 순서는 Overlay → Blocker → FocusBorder → Explanation. 정렬 110·덮개 0.95·Blocker 입력 유지. Setup은 신규 생성에도 같은 참조를 만들고, 기존 프리팹은 점선 참조가 없을 때만 보완 |

- **기존 Bind 버전 의미를 보존:** `BodyTrackingVersion`은 FieldUnitPresenter의 추적 대여와 CombatUnitPreparationLease에서도 개체 재바인딩 판단에 쓴다. 외형 교체 때 이 값을 증가시키지 않는다. Frame은 기존 View/EntityId/BodyTrackingVersion을 사용하고, 외형 교체는 Outline Slot 무효화와 대여가 캡처한 UnitModelAppearance 참조로 별도 구분한다. 따라서 같은 모델 대여가 사라지면 다음 Sync에서 다시 취득하면서 다른 전투 추적을 끊지 않는다.
- **단일 표시 설정:** Locator는 스타일 객체를 소유/복제하지 않고 Attach에서 받은 좁은 폭 공급자로 매 Locate의 현재 값을 한 번 읽는다. UI가 직렬화로 스타일 객체를 교체해도 같은 현재 폭을 모델과 구멍에 사용한다. 승인된 `Locate(step, frame)` 호출 계약은 유지한다.
- 공통 등록 경로는 변경하지 않았다. 기존 씬 prefab → UIBase(UIId.Tutorial, Parallel) → UISystem 씬 등록·바인딩·개폐 그대로이며 새 위젯/협력 객체를 서비스로 등록하거나 Group/루트 Export하지 않는다. 버튼 receiver·입력 Action·Core·FieldCamera·Architecture는 변경하지 않는다.

#### 정적 검수와 제한

- 별도 실행 문맥 검수: **Critical 0 / Important 0 / Minor 1**. Minor는 스타일 객체 교체 시 Locator의 오래된 모델 폭 참조였고 수용해 위 폭 공급자로 수정했다. 수정 후에는 호출 경로·필드 제거·공백을 자체 정적으로 확인했다. 두 번째 외부 검수/실행 검증을 수행한 것으로 기록하지 않는다.
- SSOT: 단계/문구/제한 정책 변경 없음, 강조 대상은 Frame, 스타일/여백은 UI, 렌더 속성 작성은 UnitModelAppearance 한 곳. S는 대상 해석/메시/모델 대여/합성/조립으로 분리, O는 기존 enum·표시·Slot 확장, L은 기존 효과/늦은 반환 계약 보존, I는 좁은 대여·폭 공급자, D는 기존 Buffer 읽기·ColorSystem 계약에 의존한다.
- JMH/FW: 새 등록/Build/전역 구독/직접 입력 호출 없음. UIDisplay는 표시 조립·갱신 순서, UI는 내부 위젯과 UISystem 개폐, 외형은 기존 ColorSystem 작성 경로를 사용한다. 변경 독립성: 점선 표현 변경은 Graphic/Style, 대상 변경은 Locator/Frame, 모델 합성 변경은 Appearance, 공통 수명 변경만 FieldUnitView/UIDisplay/UI 접점까지 대조한다. 관련 없는 전투 정책을 함께 수정하지 않았다.
- `.cs` 3개(ICombatUnitOutline, TutorialFocusFrame, TutorialModelHighlight)와 `.meta`를 추가했다. 기존 시험의 제거/변경 API 참조를 검색했으며 영향 참조는 없었다. 새 Test 코드·시험 도우미·MSBuild·EditMode·컴파일·Play를 작성/실행하지 않았다.
- 기존 13단계 정의는 변경하지 않았다. 새 PlayerUnits/EndTurnButton 대상의 실제 단계 연결은 D에서 한다. 여기의 코드 체크박스는 사용자 재생/화면 인수를 뜻하지 않는다.
- 최종 정적 확인: 이번 C#·meta 14개 LF/UTF-8·공백 확인, `git diff --check` 종료 코드 0. 기존 Bind 버전 증가 위치는 Bind 한 곳으로 유지되고 Locator의 오래된 스타일 참조는 제거됐다. Tests/Framework/CoreSystem/Scenes/DataBaseSO 경로의 변경 목록은 비어 있다. 프리팹 연결/GUID 정적 검수와 실제 Unity 재생 검증은 구분한다.

#### 사용자 Unity 확인

1. 컴파일 후 기존 튜토리얼에서 손패 한 테두리·메인 카드·전투 진입/공격 버튼·체력 UI 점선과 메인/적/넥서스 모델 실루엣을 확인한다. 원래 외곽선 값과 실제 Pass 출력도 확인한다.
2. UI+모델 혼합, 창 크기 변경 및 1280×720/1920×1080/2560×1440에서 테두리 정렬·설명 가림·외곽선 잘림·입력 누출이 없는지 확인한다.
3. 설명 전환·실습·정상 완료·승패/포기·씬 해제·UI 비활성/복귀·풀 반환/재바인딩·외형 교체 뒤 잔상이 없는지 확인한다. 창 포커스 복귀는 같은 단계여야 한다.
4. 투명도·피격 플래시·디졸브가 겹칠 때 강조 적용/해제로 다른 효과가 사라지지 않고 원래 외곽선/재질/MPB로 복원되는지 확인한다. 다중 재질·무기·Mesh/Skinned 애니메이션을 포함한다. 미지원/비활성 Pass 예외가 나오면 해당 Renderer·슬롯을 기록하고 임의로 셰이더를 교체하지 않는다.
5. D 연결 후 PlayerUnits의 모든 생존 아군과 EndTurnButton 강조, 마지막 단계의 강조 없음까지 확인한다.

### 9.6 Phase D — 17단계 정의 교체·정적 확인 (2026-11-10)

**결과:** D1a~D1d의 에셋 연결과 승인된 마지막 설명을 수용하기 위한 최소 코드 보완을 반영했다. **컴파일 미확인, Unity 재생 미확인.** D1e의 일반/개발 진입 한 바퀴와 기존 DataBaseAssetTests는 사용자 확인 대기다. E 통합 인수는 진행하지 않았다.

#### 기준과 변경

- 사용자 “Phase D 구현 진행” 지시 범위다. 시작 HEAD `cf1b17e3b`, 브랜치 `feature/Hojin/11_03_StageSelect`에서 A/B/C 반영분을 인수했다. 기존 FMOD·UI·Font·렌더 설정 변경은 이번 범위에 포함하지 않았다. 개인 역할 규칙을 전역 변경하지 않았으며 커밋·push하지 않는다.
- `TutorialDefinition_1-1.asset`의 기존 13개 키를 유지하고 22~25를 추가했다. 순서는 `[8,9,10,12,13,14,15,16,17,18,19,20,21,22,23,24,25]`, 설명 13개·실습 4개다. 메시지는 설계 §3, 제한된 실습의 차단 문구는 §5.3에서 그대로 가져왔다. 문구를 C#에 복제하지 않았다.
- 실습 연결은 12=`EnterCombat/None`, 18=`MoveAtLeastOneCell/MoveOnly`, 22=`AttackEnemyOnce/AttackPractice`, 24=`EndPlayerTurnOnce/EndTurnOnly`다. 12는 제한이 없으므로 차단 문구를 비워 기존 검증 계약을 따른다. 공격 적중·턴 종료 판정과 회복 경로는 B의 기존 계약을 사용한다.
- 기존 `CurrentActorUnit`·`ActionCost`·삭제된 11 참조를 제거했다. 아군 설명은 `PlayerUnits`, 23번은 `EndTurnButton`을 사용한다. 강조 없는 25번은 FocusTargets 비움·Anchor/Placement `None`이다. 강조가 있는 설명의 배치 방향은 기존 값을 유지하거나 해당 기준 대상 옆으로 지정했다.
- 시작 에셋의 `targetMapKey`는 10000이었다. 확정 계획 D1d대로 **1**로 맞췄다. 일반 맵 1의 첫 전투는 기존 `StageMap_Chapter1`의 1120으로 유지된다. 덮개 알파 0.95, 수동 설명 진행, 정의 키/이름은 유지한다. `autoAdvanceSeconds`나 새 대기 시간은 추가하지 않았다.

#### 마지막 설명의 호환 보완

- **기존 지원 가정과 실제 코드의 차이:** 계획은 25번의 빈 강조·중앙 배치를 데이터로 연결할 수 있다고 전제했지만, `TutorialStepData.ValidateExplanation`은 빈 강조 목록에서 앵커 검증을 실패시켰고 `TutorialViewExplanationLayout.Place`의 앵커 없음 경로는 중앙이 아닌 9칸 후보 탐색이었다. 에셋만 바꾸면 승인된 25번을 사용할 수 없어 아래 두 경계를 최소 보완했다. 표시/진행 정책을 새로 결정하거나 특정 단계 키를 하드코딩하지 않았다.
- `TutorialStepData`: 설명 문구·실습 문구 혼입·강조 대상 유효성/중복 검사 후, 빈 강조는 Anchor와 Placement가 모두 `None`일 때만 허용한다. 강조가 있으면 기존의 앵커 포함·방향 검증을 유지한다. 실습 검증은 변경하지 않았다. Inspector 툴팁도 네 실습과 빈 강조 규약에 맞췄다.
- `TutorialViewExplanationLayout`: `anchor` 없음과 Placement `None` 조합만 크기/여백을 적용한 화면 중앙에 배치한다. 강조 대상이 일시적으로 사라져 앵커가 없지만 방향 선호가 남은 설명은 기존 9칸 회피 배치를 그대로 사용한다.
- 마지막 단계도 기존 `TutorialPlan.CanStartExplanation`의 안정된 입력 시점 확인과 시작 Job의 재확인을 사용한다. 적 처리·표시 결과·진입 연출·제출 정지·입력 독점·포기 경계의 새 우회나 타이머를 만들지 않았다. 실제 적 응답/밤 배치 복귀 뒤 표시 시점은 D1e에서 확인한다.

#### 정적 검수와 구조

- 실제 에셋 문자열과 packed enum 목록을 해석해 설계의 17행/§5.3 문구와 일치함을 확인했다. 순서·유일한 키·13/4 분류·목표/제한 조합·빈 실습 설명·필수 안내·25번의 빈 강조/None·제거 대상 7/8/11 참조 없음·map 1·알파 0.95·자동 진행 필드 없음도 대조했다. 이는 Unity 역직렬화나 실행 검증이 아니다.
- 정의 GUID `1c6ab84a4a11d75a40f45a3cb2b9de37`와 Script GUID `e2dfcdd73a4b06d408f97697f9fb7089`는 유지했다. 기존 TutorialDefinitionDataTable → ViewGroup → DatabaseCatalog 참조를 유지한다. DefinitionDataSO의 단계 검증 → Table의 정의/중복 키·맵 검증 경로는 그대로다. 프리팹·씬·재질·Table/Catalog·meta는 변경하지 않았다.
- **SSOT:** 단계/문구는 에셋, 목표/제한 의미는 기존 Rule/Facts/Policy, 시작 시점은 Plan, 중앙 배치는 Layout 한 곳이다. **SOLID:** S는 콘텐츠·데이터 검증·위치 계산으로 분리, O는 기존 데이터/배치 계약 확장, L은 강조 있는 설명과 실습의 기존 유효성/배치 보존, I는 새 서비스/API 없이 기존 입력 사용, D는 Core 구현/DB 규칙 우회 없이 기존 정의와 표시 계약 사용이다.
- **JMH/FW:** 기존 DefinitionDataSO → Table/Factory → ViewGroup 등록·Export → Tutorial 소비 경로를 변경하지 않는다. 기존 UIId.Tutorial의 생성·등록·바인딩·UISystem 개폐·정리도 그대로다. 새 Builder·서비스·입력 구독·렌더 자원은 없다. **변경 독립성:** 문구 변경은 에셋, 빈 강조 허용은 StepData, 중앙 위치는 Layout에 한정했다. B 성공 판정/C 렌더 합성이나 다른 전투 기능을 수정하지 않았다.
- 새 Test 코드/시험 도우미·MSBuild·EditMode·컴파일·Play는 작성/실행하지 않았다. 기존 시험의 API를 제거하거나 변경하지 않았다. 정적 확인 결과와 사용자 Unity 인수를 구분한다.
- 별도 실행 문맥의 최종 제품 파일 3개 정적 검수는 **Critical 0 / Important 0 / Minor 0**이었다. 빈 Frame → UI/Layout 전달과 빈 모델 목록의 기존 강조 반환 경로까지 확인했다. 외부 검수에서 유보한 A/B/C 전체 재검수는 이번 연결 접점으로 한정하고, 실제 효과·해상도·풀/포커스 복귀와 D1e는 사용자 Unity 확인으로 유지한다. 무관한 기존 변경 및 작성 중인 이 실행 기록은 외부 검수 대상이 아니며, 기록은 작성자가 최종 diff와 대조한다. 실행하지 않은 사항을 성공으로 취급하지 않는다.
- 최종 자체 확인: 변경한 제품 3개와 문서 2개는 LF/UTF-8·BOM/대체 문자 없음, 한정 `git diff --check` 종료 코드 0이다. 줄바꿈 정리 후에도 에셋 내용은 검수한 17단계와 동일하다. 시작 시 기록한 기존 내용 변경 4개(FMOD Catalog·StageOfferUI·SCDream7 Font·PC_RPAsset)의 SHA-256이 그대로이며, 나머지 기존 작업도 편집하지 않았다.

#### 사용자 Unity 확인 — D1e

1. Unity 컴파일 및 기존 `DataBaseAssetTests`를 확인한다. DB 로드 시 마지막 단계의 빈 강조가 검증 오류를 만들지 않는지 확인한다. 기존 시험 실패가 있으면 내용과 작성자를 확인하기 전 임의 수정하지 않는다.
2. 일반 맵 1 진입과 개발 직행에서 배치 → 이동 → 공격 → 턴 종료 → 마지막 설명 클릭까지 확인한다. 개발 직행의 맵/전투 정의가 해당 튜토리얼 전제를 만족하는지도 구분한다.
3. 공격 실습은 적/넥서스 적중으로 완료되고, 이동·턴 종료로 재시도할 수 있는지 확인한다. 턴 종료 실습 뒤 적 응답 또는 밤의 배치 복귀가 안정된 다음 25번이 중앙에 표시되는지 확인한다.
4. 설명은 기다려도 넘어가지 않고 마지막 안내도 클릭 전 유지되는지 확인한다. PlayerUnits의 모든 생존 아군 아웃라인, EndTurnButton 점선, 25번의 점선/아웃라인 없음과 이전 강조 정리를 확인한다. C의 실제 Pass 출력·시각 품질 확인도 여전히 별도 미확인이다.

Phase D는 위 사용자 확인 대기를 명시해 보고하고 멈춘다. E의 전체 경계 사례 인수를 완료한 것으로 간주하지 않는다.
