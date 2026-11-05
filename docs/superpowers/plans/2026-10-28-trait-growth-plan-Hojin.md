# 레벨업 특성 성장(증강) 선택 화면 계획

- 작성자: 호진(이호진)
- 상태: TG-0~TG-e 완료(StageSelect 임시 자리, 사용자 재생 확인 Ud 문제 없음 10-29, 커밋 `24c21b3a3`). 남은 것: 확인용 콘텐츠가 커밋에 들어간 것 처리, 화면 자리 Q-TG8, UI 다듬기, 운영 콘텐츠 등록, 공유 전달.
- 설계: [증강 설계](../specs/2026-10-28-trait-growth-design-Hojin.md)
- 정리: 2026-10-29 [강화 View 통합 계획](2026-10-19-enhancement-view-integration-plan-Hojin.md) §5(TG-0~TG-e)를 별도 주제로 옮기고 결과 요약으로 줄였다. Task 단계·U0~Ud 확인 목록 전문·커밋 묶음 목록은 git `a8612321e`.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| TG-0 확인용 콘텐츠 | 특성 정의 셋, 병과 단계 넷, 표 둘, `InGameGroup.asset` 두 줄 | 완료(로컬 전용 계획) | U0 문제 없음(10-28 사용자) | `24c21b3a3`에 들어감(§2) |
| TG-a UI | `UIId.TraitGrowth`, `TraitGrowthUI`·계약 둘·모델, `StageMapCanvasView` 칸, 프리팹 도구 | 완료 | Ua 문제 없음(10-28 사용자) | `24c21b3a3` |
| TG-b 요청 수명·Stage 순서 | `TraitGrowthRequestLifetime`·계약 둘, Stage 증강 Flow·Plan·Job 둘, 연결 줄 | 완료. Stage 쪽은 R-d2가 사실·Plan 꼴로 옮김 | Ub 문제 없음(10-28 사용자) | `24c21b3a3` |
| TG-c View 내부 | 모델 빌더·문구·Controller·Factory·Screen | 완료 | Uc(컴파일) | `24c21b3a3` |
| TG-d StageSelect 연결 | 서비스 묶음·Scene Factory·팝업 묶음 넷째·Scope 여는 규칙·노드 막기, 묶음 끝 검토 반영 | 완료 | Ud 문제 없음(10-29 사용자) | `24c21b3a3` |
| TG-e 문서·공유 | 설계 구현 메모, 공유 메모 셋 | 완료 | — | `24c21b3a3`(공유는 미전달) |

## 2 남은 일

- [x] ~~확인용 콘텐츠 처리~~: `24c21b3a3`에 들어간 확인용 콘텐츠는 신규 22개 연결(10-29)과 기획서 수치 대조(10-31)로 대신했다(§3.10·§3.12). 공유 메모와 [Core 전달 문서 §51](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)의 "커밋하지 않는다"는 그대로 남아 있어 전달 전에 고친다.
- [ ] **화면 자리 Q-TG8:** 전투 씬 안 / StageSelect. StageSelect면 천명님 합의(설계 TG7). 정하면 프리팹·호스트 줄·여는 규칙만 바꾼다.
- [ ] **UI 다듬기:** 버튼·아이콘·배치(10-29 사용자 "나중에"), 키보드 자동 Navigation(검토 L7).
- [ ] **남은 콘텐츠:** 효과량 미정 9개는 수치가 정해지면 SO를 고치고 표·풀에 다시 넣는다. 강화 단계는 강화형 기획 뒤(설계 §4.9). 카탈로그 조건은 설계 §2.1.
- [ ] **공유 전달:** §4 셋 모두 미전달.
- [x] ~~덱 편집 Canvas(100)가 설정 창(99)보다 위~~: 10-30 StageSelect Canvas 하나로 모으면서 없어졌다(§3.11).
- [ ] 보고만: 승리 때마다 `CombatExperienceEvent(ExperienceEarned)`가 전투 사건 형식 표에 없어 "표시 계약이 없는 전투 사건을 건너뜁니다" 경고가 남는다(천명님 `39482fb5c`, 10-18부터, 동작 영향 없음). 증강 범위 밖이고 전투 View 쪽에서 따로 정리한다.

## 3 작업별 결과

<a id="51-기준과-진행-규칙"></a>

- 진행 규칙: Core·Buffer는 바꾸지 않았다. 새 시험 코드는 만들지 않았고 확인은 사용자 재생이다. 연결 줄(민형님 영역)은 기능·계약을 바꾸지 않고 잇는 줄만 더했다. UISystem 폴더(`UIIDs.cs`, `Screens/Stage/`)는 사용자가 허락했다(10-28). Phase마다 멈추고 보고했고, 검토 에이전트는 TG-d 뒤 한 번 돌렸다.
- 착수(10-28 사용자 "이대로 진행"): P-TG1~P-TG9 그대로. 메모리 규칙 "참조·검사·Tick은 필요할 때만"(10-28)을 TG-a부터 따라 P-TG4·P-TG9를 아래처럼 줄였다.

<a id="52-계획에서-정한-것-사용자-확인-대상"></a>

### 3.1 계획에서 정한 것

| ID | 정한 것 | 결과 |
|---|---|---|
| P-TG1 | **설계와 다름.** 노드로 가는 것을 막는 곳은 View다. 증강이 남아 있으면 `StageMapInteraction`이 노드 명령을 제출하지 않고 팝업을 다시 띄운다. 제출을 막지 않고 등록만 막으면 노드 입력이 Buffer에 남아 덱 편집의 "미등록 요청"이 계속 참이 된다. 이미 제출된 노드 명령은 Core가 `PendingTraitGrowth`로 거절한다 | 그대로 |
| P-TG2 | 등록 값 `Register(TraitGrowth, JobType.Next)` 하나, Plan이 처리·준비 Job 중 하나를 만든다 | R-d2가 `StagePlan.Decide`로 옮겨 걷힘 |
| P-TG3 | 숨김 버튼은 패널 안, 다시 여는 버튼은 패널 밖, 둘 다 묶음 프리팹 안. 숨김은 Controller의 View 상태 | 그대로 |
| P-TG4 | 화면 자리 `StageMapCanvasView.traitGrowth`와 구성 검사 | 처음엔 칸만 더하고 검사 줄은 넣지 않음(메모리 규칙) → 10-29 UI 규약 검사 뒤 구성 검사를 넣음(§3.9) |
| P-TG5 | "진행 중" 사실은 `StageLifeCycle`이 `IStageTraitGrowthFacts`로, LifeCycle 그룹이 `StageTraitGrowthFacts`로 노출 | 그대로 |
| P-TG6 | 진행은 바뀜 열쇠가 바뀔 때만 읽는다. 열쇠는 Core `ReadTraitGrowthProgressCore`가 읽는 원본 그대로: `RunProgressReadSink.RunId`·`Outcome`, `CombatStateReadSink.HasBattle`, `CharacterReadSink.Characters.Count`·`RosterRevision`, `TraitGrowthStateReadSink.ActiveOffer` 참조, `HandledCommandNumber`, `AcknowledgedResultNumber` | 그대로(`StageTraitGrowthProgress`) |
| P-TG7 | 확인용 콘텐츠와 `InGameGroup.asset` 두 줄은 로컬에만, 커밋에서 뺀다 | **지켜지지 않음**(§2 첫 줄) |
| P-TG8 | 프리팹은 에디터 도구 `StageTraitGrowthUISetup`(메뉴 `ProjectL/Stage/Ensure StageSelect Trait Growth UI`)가 만들어 StageSelect 씬에 놓고 연결 | 그대로 |
| P-TG9 | 유닛 이름은 View 유닛 외형, 레벨은 보유 기록. 문구는 `TraitGrowthPopupText` 한 곳 | 고정 글자("계속"·"숨김"·"증강 선택")는 프리팹, 바뀌는 글자만 문구 클래스(메모리 규칙) |

<a id="53-phase와-task"></a>
<a id="55-실행-기록"></a>

### 3.2 TG-0 확인용 콘텐츠 (10-28)

- 폴더 `DataBaseSO/InGameGroup/TraitGrowth/`. 특성 정의 셋 `TraitGrowth_Knockback`(50)·`TraitGrowth_DoubleShot`(160)·`TraitGrowth_DoorBreaching`(130), 셋 다 외형 행이 있고 설명 앞에 "(확인용 임시)".
- **계획과 다른 점:** "ShotGun 단계 하나" → 병과 단계 넷. Core 카탈로그가 Main·Heavy·Rider·Cannon을 꼭 하나씩 요구해서다(`TraitGrowthCatalog:18-20`). `CategoryTraitProgression_Heavy`(30)는 단계 10 획득·레벨 2, 단계 20 강화·레벨 2, 후보 셋. Main·Rider·Cannon은 빈 항목이다. 헤비 아군 둘(ShotGun은 사냥꾼의 표식, Shielder는 위압을 처음부터 가짐)에서 후보 셋이 모두 남게 사냥꾼의 표식은 넣지 않았다.
- 표 둘 `TraitGrowthDefinitionTable`·`CategoryTraitProgressionTable`, `InGameGroup.asset` 끝에 두 줄. 사용자 확인: 두 줄은 둘 다 증강 표다(메인 강화 아님).
- U0 문제 없음(10-28 사용자): 레벨 2 뒤 다음 노드가 `PendingTraitGrowth`로 막혔다.

### 3.3 TG-a UI (10-28)

- `UIIDs.cs` `TraitGrowth = 42`(10-29 `feature/JMH/Effect` 병합 때 41을 현강님 `TurnDisplay`가 먼저 써서 옮김).
- 새 `Screens/Stage/` 넷: `TraitGrowthUI`(`UIBase`, ParentID `None`, Parallel, 루트는 늘 켜고 Panel 토글, 카드 원본 복제·재사용, `UIInputBinding` 슬롯 계속 −1·숨김 −2·다시 열기 −3·카드 0~, 필수 참조는 `Prepare` 한 곳에서 예외 — 10-29 §3.9에서 `Validate` 하나로 바꿈), `TraitGrowthPopupModel`, `ITraitGrowthPopupDisplay`, `ITraitGrowthPopupInput`.
- 새 도구 `Assets/ProjectL/Editor/Stage/StageTraitGrowthUISetup.cs`가 `Prefabs/UI/TraitGrowth/TraitGrowthUI.prefab`(자기 Overlay Canvas, 카드는 `StageEventOfferDescriptionCard` 0.85배)을 만들고 StageSelect 씬에 놓아 잇는다.
- Ua 문제 없음(10-28 사용자, 메뉴 실행으로 프리팹·씬 배치·연결 생성).

### 3.4 TG-b 요청 수명·Stage 순서 (10-28, 10-29 R-d2)

- 새 `ViewSystem/TraitGrowth/`: `TraitGrowthRequestLifetime`과 계약 둘. **계획과 다른 점:** 계약 셋 → 둘(`HasPending`을 Submitter에). 회복 수명과 다른 점(자동 확인·취소 없음, 번호 놓는 때, 확인 전 예약 안 함)은 설계 TG4.
- 연결 줄: `InGameViewSystemFactory`·`InGameViewSystemGroup`(`TraitGrowthRequests`·`TraitGrowthExecution`), `LifeCycleSystemServiceKey.StageTraitGrowthFacts`, `LifeCycleSystemGroup.StageTraitGrowthFacts`.
- Stage 쪽은 처음 `TraitGrowthFlow`·`Plans/TraitGrowthPlan`·`JobRootType.TraitGrowth = 7`·`StageFlow` 순서로 만들었다. 10-29 사용자 결정으로 Lobby/StageSelect 리팩토링 R-d2(그쪽 계획 Q-Rd2 B)가 사실·Plan 꼴로 옮겼다: 그 셋과 `StageFlow`·`JobRegisterSystem` 증강 줄을 지우고, 새 `StageTraitGrowthProgress`(옛 바뀜 열쇠 그대로)·`StagePlan.Decide`(노드·상점·회복보다 먼저)·`TraitGrowthCommandJob`(실행 순간 예약, 생성자 `(core, owner)`)·`TraitGrowthPrepareJob`, 각 Job 뒤 확인 Job. 처리 확인은 다음 Tick → 같은 Runner Tick, 전투 진입 시도가 막는 동안에는 증강 진행을 읽지 않는다. 증강 요청 소유자는 이제 필수다.
- 지킬 조건 일곱(증강 판단 먼저·입력 취소 없음·덱 편집 미등록엔 제출된 선택만·처리가 준비보다 먼저·준비 뒤 제안 없으면 멈춤·원본 열쇠가 바뀔 때만 진행 읽기·확인은 화면만)을 코드와 대조해 모두 지켜졌다.
- Ub 문제 없음(10-28 사용자): 모든 노드가 `PendingTraitGrowth`로 막히고 오류 없음.

### 3.5 TG-c View 내부 (10-28)

- 새 `ViewSystem/StageMap/TraitGrowth/` 다섯: `TraitGrowthPopupText`, `TraitGrowthPopupModelBuilder`, `TraitGrowthPopupController`(공용 수명 부품 `StagePopupViewLifetime`, 입력 Layer `StageSelectionInputLayers.Popup`), `TraitGrowthPopupViewFactory`, `TraitGrowthPopupViewScreen`. 동작은 설계 TG2·TG4·TG5.
- 계획과 다른 점 없음. 공용 수명 부품 이름은 리팩토링 세션이 `StagePopupLifetime`에서 바꿨고 이 파일 두 곳도 그쪽이 고쳤다.

### 3.6 TG-d StageSelect 연결 (10-29)

- `StageMapViewServices` 칸 일곱과 `StageMapViewSystem.PostBindService` 채우기, `StageMapSceneFactory`(민형님 영역, 연결 줄)의 Factory 생성, `StageMapViewPopups` 넷째 팝업(덱 편집 다음), `StageMapViewScope`의 `OpenTraitGrowthWhenIdle`·`IsAnyPopupOpen`·`CanOpenDeckEdit`·`BlocksEntryForGrowth`, `StageMapViewInput.Create(scene, blocksEntry)`, `StageMapInteraction` 끝 선택 인자 `blocksEntry`.
- **계획과 다른 점:** 여는 규칙은 팝업 묶음이 아니라 Scope에 둔다(판정 재료가 Scope에 있음).
- 묶음 끝 검토(새 에이전트 하나, TG-a~TG-d + R-d2 증강 부분): Core 계약 위반 없음. 높음 2·중간 1·낮음 7. 반영(10-29 사용자 "권장대로 반영"):
  - H1·H2·L6: 설정 창이 Layer를 덮어쓴 동안 열리면 예외, 팝업 뒤 설정 창이 가려 Layer 복귀가 빠짐, 다시 열기 버튼이 덱 편집 위 → 여는 조건에 "입력 Layer가 선택 화면", Canvas 정렬 110 → 98.
  - M1: 숨김·다시 열기 뒤 강화 성공이 "획득"·기본 이름으로 나옴 → 고른 제안 `_submittedOffer` 보관.
  - L1 마지막 "계속" 뒤 준비 중 한 프레임, L3 Parallel을 생성자에서, L4 대리자를 생성자에서 한 번, L5 노드 클릭 경로 예외를 오류 경계로.
  - 기록만: L2 필수 참조 검사(메모리 규칙, 10-29 §3.9에서 반영), L7 키보드 Navigation(UI 다듬기 때).
- Ud 문제 없음(10-29 사용자, TG-0 콘텐츠로 팝업·카드 셋·결과와 "계속"·다음 강화 제안·숨김과 다시 열기·설정 창 겹침·상점 고용 뒤 열림·재연결·표 없는 상태·덱 편집 반영). 사용자: "나중에 UI적이나 버튼·아이콘 같은 UI 작업은 더할 것".

### 3.7 TG-e 문서·공유 (10-29)

- 설계 구현 메모(지금은 증강 설계 §2·§4에 녹임), 공유 메모 셋(§4).
- 커밋 안내(같은 파일에 리팩토링 줄이 섞여 증강 줄만 떼면 컴파일이 깨질 수 있음, 확인용 콘텐츠는 빼기)를 남겼다. 사용자는 리팩토링과 함께 `24c21b3a3` 한 커밋으로 넣었다(확인용 콘텐츠 포함, §2).

<a id="54-새-파일예정"></a>

### 3.8 새 파일

| 폴더 | 파일 |
|---|---|
| `GlobalSystem/UISystem/Screens/Stage/` | `TraitGrowthUI`, `ITraitGrowthPopupDisplay`, `ITraitGrowthPopupInput`, `ITraitGrowthReopenInput`(10-29 §3.9, 커밋 전), `TraitGrowthPopupModel` |
| `InGameSystem/ViewSystem/TraitGrowth/` | `TraitGrowthRequestLifetime`, `ITraitGrowthRequestSubmitter`, `ITraitGrowthRequestExecution` |
| `InGameSystem/ViewSystem/StageMap/TraitGrowth/` | `TraitGrowthPopupController`, `TraitGrowthPopupModelBuilder`, `TraitGrowthPopupText`, `TraitGrowthPopupViewFactory`, `TraitGrowthPopupViewScreen` |
| `LifeCycleSystem/LifeCycles/Stage/` | `IStageTraitGrowthFacts`, `Jobs/TraitGrowthCommandJob`, `Jobs/TraitGrowthPrepareJob`(+ R-d2의 `StageTraitGrowthProgress`) |
| `Assets/ProjectL/Editor/Stage/` | `StageTraitGrowthUISetup` |
| `Assets/ProjectL/Prefabs/UI/TraitGrowth/` | `TraitGrowthUI.prefab` |

### 3.9 UI 규약 검사 반영 (10-29)

> 2026-11-02: 아래 다시 열기 버튼·`ITraitGrowthReopenInput`은 공통 레이아웃 작업(SO-d)에서 지웠다.

10-29 UI 규약 검사(`Assets/AGENTS.md`, 현강님 UI 문서 기준)에서 나온 낮음 두 건을 사용자 지시("2·3번 고쳐")로 고쳤다. 컴파일 미확인, 새 시험 없음, 프리팹·씬 무변경.

- **다시 열기 버튼 = 명시적 수신 계약:** `Action` 대리자(`BindReopenRequest`) 대신 새 계약 `ITraitGrowthReopenInput.Reopen()`(+ `.meta`)을 `BindReopenInput`으로 한 번 묶는다. 패널이 열린 동안만 묶이는 `ITraitGrowthPopupInput`과 수명이 달라 따로 둔다. Controller가 명시적으로 구현하고 `RequestReopen`(노드 누름과 같은 숨김 풀기)을 부른다. 동작 같음.
- **필수 참조 검사 = 구성 검사:** 검사 원본은 `TraitGrowthUI.Validate()` 하나다. `StageMapCanvasView.ValidateComposition`이 `traitGrowth` 연결·같은 씬을 보고 `Validate`를 부르므로 연결 전(오류 경계 안)에 멈춘다. 첫 사용(`Prepare`)도 같은 `Validate`를 부른다. `Awake`는 빠진 것이 있으면 준비만 미루고 예외를 던지지 않는다(전에는 `Awake` 예외가 오류 경계 밖이었고, 빈 연결은 상점·회복·덱 편집 시작 뒤에 `screen` 이름만 담긴 예외로 드러났다). 연결이 맞으면 동작 같음.
- 이로써 P-TG4의 "검사 줄 넣지 않음"과 TG-d 검토 L2 "기록만"을 바꿨다.
- Unity 확인(사용자): ① 컴파일 ② StageSelect 진입 → 증강 팝업 → 숨김 → 다시 열기 버튼으로 다시 뜸 ③ `StageMapCanvasView.traitGrowth`를 비우고 진입하면 연결 전에 "`traitGrowth` 연결이 필요합니다" 예외로 멈춤(확인 뒤 되돌림).

### 3.10 신규 22개 후보 연결 (10-29)

설계 §4.9. 사용자 승인("네 전부다 하는데 강화 단계는 왜 임시인가요?") 뒤 권장안(획득만)으로 연결했다. 에셋만 바꿨고 C#·Core는 무변경이다.

- 새 에셋 44개(+ `.meta`): `InGameGroup/TraitGrowth/Item/TraitGrowth_{특성}` 22개(강화형 null), `ViewGroup/TraitVisual/Item/TraitVisual_{특성}` 22개(한국어 이름·설명, 아이콘 비움). 표 둘에 등록하고 기존 행은 그대로 뒀다.
- `CategoryTraitProgression_{Main,Heavy,Rider,Cannon}`: 단계는 stepId 10·2레벨 획득 하나만, 풀에 22개를 더함(Main·Heavy 25개, Rider·Cannon 22개).
- 정적 대조: 키·형식·값, 미정 −1 없음, GUID 44개 유일, 표·풀 참조 각 한 번, `git diff --check`. 별도 읽기 전용 검수에서 고칠 것 없음.
- Unity 직렬화·DB 생성·실제 선택·다음 전투 반영은 미확인(Unity Pipeline 연결 없음).

### 3.11 TraitGrowthUI Canvas 통합 (10-30)

- 사용자 승인에 따라 자체 Canvas·CanvasScaler·GraphicRaycaster를 제거하고 StageSelectCanvas의 마지막 자식으로 이동했다. 루트 Stretch·배율 1, 기존 Panel·버튼·카드 참조는 유지했다.
- 구성 검사와 생성 도구를 같은 계층으로 맞췄다. 정렬 SSOT는 공통 Canvas(4)이며 설정(99)·덱(100) 아래다. UIBase 등록·Parallel 정책·입력 수명·Core는 변경하지 않았다.
- 검증: 씬/프리팹 ID·부모 연결·제거 컴포넌트 참조·기존 내용 보존 정적 확인. Unity 컴파일·재생 미확인.

### 3.12 기획서 수치 대조 (10-31)

설계 §4.9(원본·뺀 9개·남긴 13개). 사용자 "기획서를 보고 수치를 반영하고, 없는 것은 넣지 않는다".

- 슬라이드 XML 본문을 직접 읽었다(PowerPoint 렌더 확인은 안 함). 명시된 기본 효과량은 SO와 같아 숫자는 바꾸지 않았다.
- 미정 9개를 성장 표와 네 풀에서 뺐다. SO·`.meta`·외형은 지우지 않았다.
- 확인: 뺀 9개 GUID가 표·풀에 없음, 남은 13개 필드가 원문과 같음, 풀의 모든 참조가 표에 있음, 단계 유지, C# 무변경. Unity 컴파일·DB 생성·실제 선택은 미확인이고 확인은 새 Run에서 한다.

### 3.13 공통 레이아웃으로 화면 교체 (11-02)

[공통 레이아웃 계획](2026-11-02-stage-offer-common-ui-plan-Hojin.md) SO-d. 결과 화면·"계속"·숨김·다시 열기를 걷고, 고르면 바로 다음 제안 또는 닫힘, 글은 SO `StageOfferText_TraitGrowth`. 요청 수명·Plan·제출 길은 그대로다.

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [증강 UI 공유](../handoffs/2026-10-29-trait-growth-ui-hyeongang-share-Hojin.md) — `UIId.TraitGrowth = 42`, 화면·계약·모델, `StageMapCanvasView` 칸, 도구·프리팹, 정렬 98 이유 | 현강님 | 미전달 |
| [증강 연결 줄 공유](../handoffs/2026-10-29-trait-growth-minhyeong-share-Hojin.md) — 연결 줄 다섯, 걷힌 `JobRootType.TraitGrowth`·Plan 줄 | 민형님 | 미전달 |
| [Core 전달 문서 §51](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#51-레벨업-특성-성장증강-viewflow-인수-2026-10-29-호진-core-파일-변경-없음-미전달) — 인계 §9 View·Flow 인수, Q-TG8 합의, 외형 행·병과 넷·후보 셋 규칙, 강화 노드, 경험치 사건 경고(보고). Core 요청 없음 | 천명님 | 미전달 |
