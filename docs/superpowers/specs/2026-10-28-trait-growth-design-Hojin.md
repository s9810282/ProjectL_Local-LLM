# 레벨업 특성 성장(증강) 선택 화면 설계

- 작성자: 호진(이호진)
- 상태: 구현 완료(StageSelect 임시 자리, 사용자 재생 확인 Ud 문제 없음 10-29, 커밋 `24c21b3a3`, 공유 미전달). 화면 자리 Q-TG8은 미정이다.
- **2026-11-02 바뀜(사용자, [공통 레이아웃 설계](2026-11-02-stage-offer-common-ui-design-Hojin.md) SO 절):** 아래 내용 중 화면 부분은 그 설계가 대신한다. 결과 화면·"계속"(Q-TG7)과 숨김·다시 열기(Q-TG12)는 없어졌고, 고르면 바로 다음 제안 또는 닫힘이다. 글은 코드가 아니라 SO `StageOfferText_TraitGrowth`(Q-TG6). 계약 `ITraitGrowthPopupDisplay`·`ITraitGrowthPopupInput`·`ITraitGrowthReopenInput`, 모델·글·`TraitGrowthPopupViewScreen`은 지웠고 프리팹은 `TraitGrowthUI` 자식 `TraitUI`다. 요청 수명·Plan·제출 길은 그대로다.
- 범위: 레벨업으로 생긴 증강(특성 획득·강화) 제안을 보이고, 고르게 하고, 결과를 확인하는 화면과 그 요청·순서 연결. 성장 규칙·후보 뽑기·진입 막기(Core), 단계·후보 풀 콘텐츠(기획), 메인 강화 상점, 전투 View(용근님)는 다루지 않는다.
- 계획: [증강 계획](../plans/2026-10-28-trait-growth-plan-Hojin.md)
- 정리: 2026-10-29 [강화 View 설계](2026-10-19-enhancement-view-integration-design-Hojin.md) §14(3판, TG1~TG10)를 별도 주제로 옮겼다. 메인 강화 상점과 증강은 다른 기능이다. 옛 판은 git `a8612321e`.

## 1 목적과 범위

<a id="141-요구와-출처"></a>

- 사용자 요청(10-28): 레벨업 증강 선택 화면 설계. 사용자 지시 둘이 바탕이다.
  - 화면 자리는 아직 정하지 않는다.
  - 화면은 묶음 프리팹 하나로 만든다.
- Core 원본(천명님): [Core → View/Flow 인계](../handoffs/2026-10-27-category-trait-progression-handoff.md), [Core 설계](2026-10-27-category-trait-progression-design.md), 커밋 `d48542034`(10-28 병합 `d78dd64a`).
  - 이 인계가 어제 인계 `2026-10-27-sub-trait-growth-core-handoff.md`를 대신한다. 옛 API·서브 전용·획득 1·강화 1 모델은 지원하지 않는다.
- 바뀐 것: 1·2판(10-28 오전)은 옛 Core API(`GetTraitGrowthChoices`·`ChooseTraitGrowth`, 서브 전용) 기준이었다 → 같은 날 천명님 `d48542034`가 계약을 바꿔 3판(이 문서)으로 다시 썼다.
- 이름: 증강은 Core 이름 `TraitGrowth` 그대로다. 메인 강화 상점은 `MainEnhancement…`로 나눈다(10-28 사용자, [강화 View 설계 §15](2026-10-19-enhancement-view-integration-design-Hojin.md#15-메인-강화-상점-me1me8-2026-10-29-초안)).

## 2 지금 구조

화면 묶음·Controller·요청 수명은 화면 자리와 무관하다. 자리마다 다른 것은 호스트 줄과 여는 규칙뿐이다(TG1). 실행 흐름은 다음과 같다.

1. 전투 정산에서 레벨이 오르고, `EndBattle` 뒤 Core 진행이 "선택 필요"가 된다.
2. StageSelect의 Stage Plan이 준비 Job을 걸어 제안을 만든다.
3. 호스트(`StageMapViewScope`)가 "열 수 있음"을 보고 팝업을 연다. Controller가 제안을 그린다.
4. 카드를 고르면 요청 수명이 번호를 받아 들고, Stage Plan이 처리 Job을 건다.
5. Controller가 결과를 받는 대로 확인한다(11-02 공통 레이아웃 SO8. 10-28 판은 결과를 보이고 "계속" 뒤 확인했다).
6. Stage Plan이 진행을 다시 읽고 다음 제안을 준비하거나 끝낸다.

| 수명 | 대상 | 하는 일 |
|---|---|---|
| UI | `TraitGrowthUI`(`StageOfferScreenUI` 하위, `UIId.TraitGrowth = 42`, ParentID `None`, Parallel), 자식 레이아웃 프리팹 `TraitUI` | 11-02부터 공통 레이아웃(`StageOfferLayoutView`·`IStageOfferDisplay`·`IStageOfferInput`·`StageOfferPopupModel`)을 쓴다. 골드·나가기·밑줄은 꺼지고 첫 카드에 초점을 둔다([공통 레이아웃 설계](2026-11-02-stage-offer-common-ui-design-Hojin.md) SO8) |
| 화면 연결(자리와 무관) | `TraitGrowthPopupController` | "보일 것 있음" 계산(TG2), 열기 입구 `TryOpen`, 제안 그리기, 카드 입력 → 요청 수명에 제출, 결과가 오면 바로 확인하고 다음 제안을 그리거나 닫음(실패면 안내 뒤 지금 제안). 언제 여는지는 정하지 않는다. 쓰기 싱크는 확인에만 쓴다 |
| 화면 연결(자리와 무관) | `TraitGrowthPopupModelBuilder` | 제안 + 특성 외형 + 유닛 이름·레벨 + 글 SO `StageOfferText_TraitGrowth` → `StageOfferPopupModel`(순수 변환). 화면 글은 코드에 두지 않는다 |
| 화면 연결(자리와 무관) | `TraitGrowthPopupViewFactory` | 고정 의존성(Buffer 싱크 넷, 요청 수명, Stage 사실, 캐릭터 ReadSink, 특성·유닛 외형 조회, 글 표, UI, 오류 경계)으로 연결마다 Controller를 만든다. 화면 어댑터는 공통 `StageOfferPopupViewScreen` |
| 씬과 무관 | `TraitGrowthRequestLifetime`(View 그룹 모듈) | 계약 둘 `ITraitGrowthRequestSubmitter`(`HasPending`·`Submit`)·`ITraitGrowthRequestExecution`(예약·시작·종료 통지). 회복 수명의 제출·번호 보관·실행 통지 꼴이고, 자동 확인·취소는 없다(TG4) |
| LifeCycle(Stage) | `StageTraitGrowthProgress`(`StageProgress`가 가짐)·`StagePlan.Decide`·`TraitGrowthCommandJob`·`TraitGrowthPrepareJob` | 진행을 바뀜 열쇠가 바뀔 때만 읽는다. Plan이 노드·상점·회복보다 먼저 증강을 판단한다(처리가 준비보다 먼저). 각 Job 뒤 확인 Job이 같은 Runner Tick에 확인한다. Stage 사실 `IStageTraitGrowthFacts.IsPending`을 `StageLifeCycle`이 낸다 |
| 호스트(지금 자리) | `StageMapViewScope`·`StageMapInteraction` | 여는 규칙 `OpenTraitGrowthWhenIdle`, 노드 막기 `BlocksEntryForGrowth`(TG2). 팝업 묶음 `StageMapViewPopups`의 넷째 팝업이다 |

- 연결 줄(민형님 영역, 연결만): `InGameViewSystemFactory`의 요청 수명 모듈 등록, `InGameViewSystemGroup.TraitGrowthRequests`·`TraitGrowthExecution`, `LifeCycleSystemServiceKey.StageTraitGrowthFacts`, `LifeCycleSystemGroup.StageTraitGrowthFacts`, `StageMapSceneFactory`의 Factory 생성. 새 이벤트 버스는 없다. Buffer 싱크는 `BufferSystemGroup`이 이미 노출한다(인계 §1).
- 소스 루트 `Assets/ProjectL/Script/Systems/RuntimeSystem/`: UI `GlobalSystem/UISystem/Screens/Stage/`, 화면 연결 `InGameSystem/ViewSystem/StageMap/TraitGrowth/`, 요청 수명 `InGameSystem/ViewSystem/TraitGrowth/`, Stage 실행 `LifeCycleSystem/LifeCycles/Stage/`. 프리팹 도구 `Assets/ProjectL/Editor/Stage/StageTraitGrowthUISetup.cs`.
- 바뀐 것: 3판은 Stage 쪽을 `TraitGrowthFlow`·`TraitGrowthPlan`·`JobRootType.TraitGrowth = 7`과 `StageFlow` 순서 변경으로 그렸다 → 10-29 Lobby/StageSelect 리팩토링 R-d2(사용자 Q-Rd2 B)가 Stage Flow를 사실·Plan·Decide 꼴로 옮기면서 증강도 위 표처럼 옮겼다. 계약 `IStageTraitGrowthFacts`·요청 수명 둘과 지킬 조건 일곱은 그대로 지켜졌다([계획 §3](../plans/2026-10-28-trait-growth-plan-Hojin.md#3-작업별-결과)).
- 바뀐 것: 노드 막기는 처음에 `StageFlow`가 등록하지 않는 것으로 그렸다 → View가 막는다(계획 P-TG1). 요청 수명 계약은 셋(Submitter·Execution·Status) → 둘(잠금 원본 `HasPending`을 Submitter에).

<a id="142-확인-사실-2026-10-28-head-d78dd64a"></a>

### 2.1 Core 계약 확인 사실 (2026-10-28, HEAD `d78dd64a`)

| 항목 | 지금 |
|---|---|
| 저작 | `CategoryTraitProgressionDataSO`: 카테고리마다 단계 목록(`stepId`·`unlockLevel` 2 이상·`kind` 획득/강화)과 후보 풀(`TraitGrowthDefinitionDataSO` 목록). `TraitGrowthDefinitionDataSO`: 특성마다 `displayName`·`description`·`enhancedName`·`enhancedDescription`. 표 둘을 InGameGroup에 등록한다 |
| 표가 없을 때 | 표가 둘 다 없으면 카탈로그가 비어 단계가 없고 진행은 늘 "계속 가능"이다(증강 꺼짐). 하나만 있으면 DB 생성에서 예외다 |
| 카탈로그 검사 | Main·Heavy·Rider·Cannon 단계를 꼭 하나씩 요구한다(비어 있어도 됨, `TraitGrowthCatalog`). 획득 단계는 그 병과 아군 유닛마다 원래 특성을 뺀 후보가 셋 이상이어야 한다(`TraitGrowthCatalogValidator`) |
| 제안 | 한 번에 하나다. 대상은 보유 ID 순서, 그다음 단계 순서다. 획득은 늘 3개, 강화는 1~3개다. 뽑힌 제안은 고정되고 다시 열어도 다시 뽑지 않는다 |
| 메인·서브 대상 | 둘 모두 병과별 성장 단계의 `unlockLevel`에 도달하면 특성을 선택해 획득·강화할 수 있다. 제안·선택·적용·화면은 같은 경로이며 메인이라는 이유로 화면을 숨기지 않는다. `MainGrowthRules`의 능력치 성장 및 메인 강화 상점과는 별개다 |
| 현재 메인·서브 콘텐츠 | §4.9. 네 병과 모두 2레벨 획득 1회, Main·Heavy 풀 14개, Rider·Cannon 13개, 강화형 없음 |
| 설정 단위 | 단계와 후보 풀은 유닛별이 아니라 병과별이다. 메인은 Main 병과로 독립 설정할 수 있다. 같은 병과 유닛별 단계·후보 풀이나 단계마다 특정 특성을 지정하는 칸은 현재 없다. 최종 후보는 보유 특성·획득/강화 종류·조합 가능 조건에 따라 Core가 거른다 |
| View 계약 | 읽기: `TraitGrowthStateReadSink.ActiveOffer`, `TraitGrowthResultReadSink.HandledCommandNumber`·`LatestResult`, `TraitGrowthViewReadSink.AcknowledgedResultNumber`·`TryGetCommand(number)`. 쓰기: `TraitGrowthViewWriteSink.Submit(TraitGrowthChoiceRequest) → number`·`AcknowledgeResult(runId, number)`. 모두 `BufferSystemGroup`에 노출돼 있다 |
| 입력 하나 | 대기 입력은 하나다. 대기 중 `Submit`은 예외다. 번호를 모르면 대기 입력을 찾을 수 없다(열거 API 없음). 비우기·취소 API는 없고, Run을 다시 시작할 때 Buffer 내부 `Clear`가 대기 입력을 지운다 |
| View 금지 | 로스터 서비스 호출, InGame 표 읽기, 성장 상태 직접 쓰기. 이름·설명·강화형은 제안에서만 읽는다 |
| Flow 계약 | `Characters.ReadTraitGrowthProgress()`(부작용 없음), `Characters.PrepareNextTraitGrowthOffer()`(제안이 없고 선택이 필요할 때만 만듦), `TraitGrowthCommands.ProcessNextTraitGrowthCommand(number)`. 모두 동기다. 바꾸는 둘은 Job이 부른다. 진행 읽기는 표가 있으면 카탈로그를 매번 새로 만든다 |
| 진행 상태 넷 | 사용 불가(Run 끝·전투 중) / 확인 대기(처리 번호 > 확인 번호) / 선택 필요 / 계속 가능. 확인 대기와 선택 필요 동안 Core가 노드 진입·상점·회복·직접 전투 진입을 `PendingTraitGrowth`로 거절한다(`ChapterProgressionCoordinator.EvaluateStageEntry`) |
| 결과 | 성공이면 선택 기록 하나를 더하고 제안을 지우고 번호 붙은 결과를 남긴다. 실패도 결과를 남기고 제안은 그대로다. 어느 쪽이든 View가 보여 준 뒤 확인해야 다음으로 간다. 마지막 선택 뒤에도 확인 대기다 |
| 건너뛰기 | 닫기·뒤로로 필수 선택을 건너뛸 수 없다(인계 §7). 화면을 숨겨도 Core 진입 막기는 그대로다 |
| 레벨업 시점 | Core는 증강 화면을 스스로 띄우지 않는다. 레벨은 전투 정산(`CombatExperienceSettlement`)에서 오르고 `EndBattle` 뒤 "선택 필요"가 될 뿐이다. 전투가 살아 있는 동안(`HasBattle` 또는 전투 캐릭터가 남음)은 "사용 불가"다 |
| 상점 안 획득 | 상점 구매는 성장으로 막히지 않는다(`ShopService` 구매 경로에 성장 검사 없음). 높은 레벨 유닛을 고용하면 상점이 열린 채로 제안이 생길 수 있다 |
| 인계가 제안한 순서 | 계속되는 승리: 재생 → EndBattle → 챕터 정리 → 결과 확인 → 성장 반복 → 전환 한 번. 비전투 획득 뒤에도 노드 진입 전에 같은 반복 |
| UI 정책 | Exclusive UI를 열면 UISystem이 조상이 아닌 다른 Exclusive UI를 닫는다(`UISystem.CloseUnrelatedHistory`). 상점·회복(부모 `StageScreenRoot`)·덱 편집(부모 `StageSelect`)이 모두 Exclusive다 |
| 카드·외형 부품 | 카드 `StageOfferCardView`(`UIWidgetBase`)·값 `StageOfferCardContent`(`Icon`·`Title`·`PrimaryText`·`SecondaryText`·`ButtonText`)를 다시 쓴다. 특성 아이콘은 `TraitVisualTableQuery.TryGetTraitVisual(TraitKey, out)`. 특성 키 140·150·170·180은 외형 행이 없다 |

## 3 결정 기록

<a id="144-정할-것"></a>

| ID | 결정 | 날짜·근거 |
|---|---|---|
| 자리 | 화면 자리는 정하지 않는다. 확인용 임시 자리만 둔다 | 10-28 사용자 |
| 묶음 | 화면은 묶음 프리팹 하나 `TraitGrowthUI` | 10-28 사용자 |
| Q-TG2 | **A** 확인용 임시 자리는 StageSelect | 1·2판 결정 유지 |
| Q-TG4 | **A** 특성 외형 행이 없으면 예외로 멈춘다 | 1·2판 결정 유지 |
| Q-TG6 | 화면 문구는 기획 확인. 그때까지 초안(머리줄 "새 특성"·"특성 강화", 버튼 "선택한다"·"계속", 준비 줄 "다음 선택 준비 중") | 1·2판 결정 유지 |
| Q-TG7 | **A** 결과를 카드에 보이고 "계속"을 누르면 확인. 안 B는 정해진 시간 뒤 자동 확인. 확인은 보여 준 뒤라는 인계 순서와 맞고, 바로 확인하면 마지막 결과가 한 프레임도 안 보인다 | 10-28 사용자 |
| Q-TG8 | **미정인 채 구현하고 확인은 StageSelect 기준.** 후보: 전투 씬 안(결과 확인 뒤·떠나기 전) / StageSelect. StageSelect로 정해지면 천명님 합의가 필요하다(TG7) | 10-28 사용자 |
| Q-TG9 | **옮김.** "들어갈 수 없는 강화 노드(1202)를 맵에 잠시 안 나오게 할지"는 증강과 무관해 메인 강화 상점 주제로 옮겼다(TG10) | 10-28 사용자 |
| Q-TG10 | **A** 확인용 콘텐츠는 로컬 임시 에셋(커밋 안 함). 안 B 운영 등록, C 대기 | 10-28 사용자 |
| Q-TG11 | **A** View는 메인 제안을 거르지 않는다. 메인에 안 나오게 하려면 메인 단계를 비워 둔다. 나중에 바꿀 수 있다 | 10-28 사용자 |
| Q-TG12 | **C** StageSelect에서 숨긴 팝업은 다음 노드를 누르거나 다시 여는 버튼(묶음 프리팹 안)으로 다시 띄운다 | 10-28 사용자(동작 정리 뒤 질문을 바꿈) |
| StageSelect 동작 | 다음 노드로 가는 것만 막고 증강 팝업으로 고르게 한다. 다음 노드가 메인 강화 상점이어도 증강이 먼저다. 숨김을 둔다. 설정·호버·덱 편집은 막지 않는다 | 10-28 사용자 |
| 열기·닫기 | 열기(자동·버튼), 닫기·"나중에", 뒤를 막는 방식, 다른 팝업과 겹침은 화면 자리와 함께 정한다. Controller는 "보일 것 있음"만 계산한다 | 10-28 사용자 정정(3판 검토 뒤) |

- 무효(새 계약 때문): Q-TG1 A(직접 호출 → Buffer + Job으로 바뀜), Q-TG3 A(한 유닛씩 → Core가 대상 순서를 정함), Q-TG5 A(레벨 칸 둘 → 카테고리 단계 SO, Q-TG10).
- 계획에서 정한 것 P-TG1~P-TG9는 [계획 §3.1](../plans/2026-10-28-trait-growth-plan-Hojin.md#31-계획에서-정한-것)이다.

<a id="143-설계"></a>

## 4 기능별 상세

### 4.1 TG1 화면 묶음과 자리

- 지금 만드는 것은 화면 묶음, 제안 표시·선택·결과 소비, 요청 수명, Stage 순서다. 자리와 상관없이 만든다. 자리를 정하면 프리팹·호스트 줄·여는 규칙만 바꾼다.
- 2026-10-30 사용자 승인: `TraitGrowthUI`는 자체 Canvas·CanvasScaler·GraphicRaycaster 없이 `StageSelectCanvas`의 마지막 자식으로 배치한다. 루트는 전체 Stretch이며 배율은 1이다. UISystem 독립 등록과 Panel 개폐는 유지한다.
- `UIBase`로 독립 등록하고 **Parallel**이다. Exclusive면 열 때 UISystem이 상점·회복·덱 편집을 그 Scope를 거치지 않고 닫고, 반대로 그쪽이 열릴 때 성장 화면이 닫혀 서로 닫기를 되풀이한다(§2.1 UI 정책). Parallel은 생성자에서 정한다(`UIWidgetBase`와 같음, 검토 L3).
- 카드 부품 `StageOfferCardView`와 그 프리팹(`StageEventOfferDescriptionCard`)만 다시 쓴다. `StageOfferPopupUI`는 상속하지 않는다. 그 기반은 ParentID가 `StageScreenRoot`로 고정돼 있고, 그 루트가 자식을 상점·회복으로 검사한다.
- 카드 버튼은 `UIInputBinding`·`IUIInputReceiver`로 받고, 키보드·컨트롤러 EventSystem Submit도 같은 수신자로 온다(인계 §7). 열릴 때 첫 카드(결과면 "계속")에 초점을 둔다.
- `UIIDs.cs`와 UISystem 폴더의 새 화면 파일은 현강님 영역이다. 사용자 허락(10-28)을 받고 현강님 규칙(`Assets/AGENTS.md`)대로 만들었다.

### 4.2 TG2 보일 것과 여는 규칙

"보일 것이 있다"는 Buffer 상태와 Stage 사실로 계산한다. 하나라도 참이면 있다.

- 확인 안 한 결과가 있다(`HandledCommandNumber > AcknowledgedResultNumber`).
- 현재 제안이 있다(`ActiveOffer != null`).
- Stage 사실 `IStageTraitGrowthFacts.IsPending`이 참이다. 마지막으로 읽은 진행이 선택 필요 또는 확인 대기면 참이다(덱 편집의 `IStageDeckEditFacts`와 같은 꼴). 그래서 확인과 다음 제안 준비 사이(한두 프레임)에도 "보일 것 있음"이 이어지고, 열려 있으면 "다음 선택 준비 중"을 보인다. 남은 성장이 없으면 준비 중을 그리지 않는다(검토 L1).

셋 다 거짓이면 화면이 열려 있어도 닫는다(자리와 무관). 다시 연결(씬 재진입·호스트 교체)해도 Buffer 상태에서 같은 제안·결과를 그린다.

Controller는 호스트용으로 `IsPending`(숨김과 무관, 노드 막기에 씀)·`WantsOpen`(숨기지 않았고 보일 것 있음)·`TryOpen`·`RequestReopen`을 낸다. 숨김은 Controller의 View 상태이고 보일 것이 없어지면 풀린다.

**StageSelect(지금 자리)의 여는 규칙**

- 막는 것은 다음 노드로 가는 것 하나다. 증강이 남아 있으면 `StageMapInteraction`이 노드 명령을 쓰지 않고(`blocksEntry`) 팝업을 다시 띄운다. 이미 제출된 노드 명령이 있으면 Core가 `PendingTraitGrowth`로 거절한다. 노드 클릭 경로의 진행 읽기 예외는 `BlocksEntryForGrowth`가 오류 경계에 보고하고 진입을 막는다(검토 L5).
- 여는 조건(`StageMapViewScope.OpenTraitGrowthWhenIdle`, 팝업 Tick 다음): 맵을 그렸고, 진입 표시 중이 아니고, 처리 전 노드 요청이 없고, 상점·회복·덱 편집이 닫혀 있고, 그 결과를 기다리지 않고, **입력 Layer가 선택 화면**이다. 상점 고용으로 상점 안에서 제안이 생긴 경우도 상점이 닫힌 뒤 연다.
- `IsAnyPopupOpen`에 증강 팝업이 들어가고, `CanOpenDeckEdit`는 증강 팝업이 열려 있지 않을 때만 참이다.
- 숨김 버튼은 패널 안, 다시 여는 버튼("증강 선택")은 패널 밖이고 둘 다 묶음 프리팹 안이다. 다시 여는 버튼은 성장이 남아 있으면서 숨겼을 때만 보인다. 그래서 `StageSelectPanelUI` 등 다른 화면 파일은 고치지 않는다.
- 숨겨도 다음 노드 진입은 막힌다. 설정·호버·덱 편집은 막지 않는다(Core도 덱 편집을 막지 않음).
- 팝업이 보이는 동안 뒤는 묶음 안의 화면 전체 배경(raycast 받음)과 입력 Layer `StageSelectionInputLayers.Popup`(호스트의 `PopupInputSession`, 공용 수명 부품 `StagePopupViewLifetime`)으로 막는다.
- 정렬은 공통 `StageSelectCanvas`의 Order 4를 따른다(2026-10-30 통합). 마지막 자식이므로 같은 Canvas의 화면 위에 표시하고, 설정 창(99)보다 아래다. 이후 덱도 공통 Canvas로 통합되어 같은 Canvas 내 형제 순서로 정렬한다. 덱 편집과 동시 개방을 막는 기존 정책은 유지한다.

### 4.3 TG3 대상과 순서

- Core가 정한다. View는 `ActiveOffer.OwnedId`의 유닛을 보인다.
- 머리줄: 유닛 이름(View 유닛 외형)·레벨(`CharacterReadSink.TryGetOwned(OwnedId)`의 `Level`)·"새 특성" 또는 "특성 강화".
- 메인 제안도 같은 화면으로 보인다(Q-TG11).

### 4.4 TG4 명령과 결과

1. 카드 선택 → Controller가 `TraitGrowthRequestLifetime.Submit(new TraitGrowthChoiceRequest(offer.RunId, offer.OfferId, offer.OwnedId, offer.StepId, choice.TraitKey))`를 부른다. 그린 제안이 지금 제안과 같을 때만, 먼저 잠그고 제출한다. 요청 수명이 쓰기 싱크의 `Submit`을 부르고 받은 번호를 같은 호출 안에서 보관한다. 화면이 그 사이 사라져도 번호를 잃지 않는다.
2. 카드 잠금의 원본은 요청 수명의 `HasPending`이다. 다시 연결된 화면도 이것으로 잠근다.
3. Stage Plan이 처리 Job을 걸고, Job이 실행 순간에 예약(`TryReserveExecution` → `TryBeginExecution`)한 뒤 `ProcessNextTraitGrowthCommand(number)`를 부른다. 예외 때도 통지한다.
4. Controller는 `HandledCommandNumber > AcknowledgedResultNumber`이면 `LatestResult`를 보인다. 자기 제출이든 다시 연결 뒤 남은 결과든 같은 규칙이다. 결과 문구·카드는 고른 제안(`_submittedOffer`, 숨김·다시 열기와 무관하게 보관)으로, 없으면 그린 제안으로 그린다(검토 M1). 다시 연결 뒤라 그 제안이 없으면 카드 없이 안내만 보인다.
5. "계속"에서 `AcknowledgeResult(receipt.RunId, receipt.Number)`를 부른다. Run은 결과의 Run이다(요청의 Run이 아님, 인계 §3).
6. Stage Plan이 진행을 다시 읽고 다음 제안을 준비하거나 끝낸다.

| 결과 | 처리 |
|---|---|
| 성공 | 고른 카드에 "<이름> 획득" 또는 "<이름> 강화"와 **계속**. 누르면 확인한다(Q-TG7 A). 다음 제안이 오면 같은 화면에 다시 그리고, 성장이 끝나면 닫는다 |
| `NotCandidate`·`StaleOffer`·`NotOwned`·`NotUnlocked`·`SourceChanged` | 안내 → 확인 → 현재 제안으로 다시 그림. 옛 요청을 자동으로 다시 보내지 않는다 |
| `InBattle`·`RunEnded` | 확인하고 닫는다. 이후는 기존 전투·Run 정책을 따른다 |
| `InvalidRequest`·`UnknownDefinition`·`AwaitingAcknowledgement` | 나오면 안 되는 결과다. 확인한 뒤 오류 경계에 보고한다 |
| 확인이 false | 지우거나 성공으로 보지 않는다. 다음 그리기가 Buffer를 다시 읽는다 |
| 처리기가 false | 결과를 위조하지 않는다. 결과도 없고 입력이 남아 있으면 요청 수명이 오류 경계에 보고하고 멈춘다(회복과 같음) |

요청 수명이 회복과 **다른 점**(인계 §5):

- 결과를 자동으로 확인하지 않는다. 화면이 없으면 결과는 확인 대기로 남고, Core가 진입을 막으며, 다음 연결 때 화면이 보여 준다.
- 정지·씬 이탈·Detach 때 입력을 취소하지 않는다(취소 API가 없다, `StopInput`·오류 정리 묶음에 넣지 않음).
- 번호를 놓는 때: 확인 번호 ≥ 들고 있는 번호, 또는 그 번호의 입력도 결과도 없음(Run 재시작 `Clear`).
- 앞선 결과를 아직 확인하지 않았으면 예약하지 않는다(빈 Job 되풀이 막기).

### 4.5 TG5 카드 내용

| 칸 | 값 |
|---|---|
| 아이콘 | `TraitVisual`의 `TraitKey` 아이콘(비어도 됨) |
| 제목 | `choice.Name`(강화 제안이면 강화형 이름이 이미 들어 있음) |
| 첫 줄 | `choice.Kind`에 따라 "새 특성" 또는 "특성 강화" |
| 설명 | `choice.Description` |
| 버튼 | 선택한다 |

- 카드 수와 순서는 제안 그대로다(1~3). 채우거나 거르거나 다시 고르지 않는다.
- 외형 행이 없으면 예외로 멈춘다(Q-TG4 A). 후보 풀에 넣는 특성마다 외형 행이 필요하다(Core 공유에 적음).
- 고정 글자("계속"·"숨김"·"증강 선택")는 프리팹에, 바뀌는 글자(종류 줄·유닛 줄·결과·실패 안내 셋·준비 중·카드 버튼)는 `TraitGrowthPopupText`에 둔다.

### 4.6 TG6 소유와 조립

§2 표가 원본이다. 진행 읽기는 Core `ReadTraitGrowthProgressCore`(`CharacterRosterService`)가 읽는 원본 열쇠(Run·결과·전투·보유 수·로스터 개정·제안 참조·처리/확인 번호)가 바뀔 때만 한다(계획 P-TG6). 카탈로그는 DB 고정이라 열쇠가 아니다. 준비 Job 뒤 열쇠가 그대로면 증강 도메인을 멈추고 오류 로그를 남긴다. 처리 Job 뒤 예약이 남았으면 멈춘다. 덱 편집이 읽는 "미등록 요청"에는 요청 소유자의 `CanReserveExecution`만 넣고 "선택 필요"는 넣지 않는다(증강 중에도 덱 편집 가능).

### 4.7 TG7 전투 뒤 성장 자리(Q-TG8 미정)

- 이번 구현은 확인용 임시 자리 StageSelect에 붙였다. 전투 골격 순서는 바꾸지 않았다. 전투에서 돌아온 뒤 Stage Plan이 성장 반복을 돌린다.
- 전투 씬 안이라면 `EndBattle` 뒤인 결과 표시·확인 다음, 떠나기 전 구간만 된다. 전투 이탈 순서(정리 → 결과 표시 → 확인 → 떠나기)에 이 구간은 이미 있다.
- 어느 자리든 건너뛸 수 없다. 비전투 획득(상점 고용 등) 뒤에도 같은 길로 처리된다.
- StageSelect로 정해지면 인계 §7 Flow 체크리스트("결과 확인 → 성장 반복 → 전환 한 번")와 순서가 달라 인계 §9 인수 장부에 걸린다. 그때 **천명님의 명시적 합의가 필요하다**. 전투 씬 안으로 정해지면 이 차이는 없다.

### 4.8 TG8 유닛 정보 창

- 코드 변경이 없다. 덱 편집이 열 때마다 능력치 조회의 `TraitKeys`를 다시 읽으므로 얻은 특성이 보인다.
- 강화형은 같은 `TraitKey`라 정보 창은 기본 특성 외형의 이름·설명을 보인다(§8 보류).

### 4.9 TG9 콘텐츠(Q-TG10)

- 처음(10-28)에는 운영 표 두 개를 켜지 않고 로컬 확인용 콘텐츠(병과 넷 단계 + 특성 셋, 계획 TG-0)로만 확인하기로 했다. 인계가 "Core 시험이 통과했다고 운영 단계 목록을 켜지 말라"고 해서다. 실제로는 `24c21b3a3`에 들어갔다.
- **신규 22개 연결(10-29 사용자 승인):** 천명님 신규 특성 22개를 메인·서브 모두의 후보로 넣었다. 22개에는 승인된 강화형이 없고, 획득만 연결하면 강화 단계에서 후보가 모자라 `TraitGrowthCatalogValidator`가 거절한다. 그래서 네 병과 모두 **2레벨 획득 1회(stepId 10)만** 두고 강화 단계는 뺐다. 강화 기능이 임시인 것이 아니라, 강화형이 정해질 때까지 이 콘텐츠에서만 뺀 것이다. 수치가 없는 여덟 칸은 확인용 값(1·10%·20%)을 넣었다. 성장 정의는 병과마다 복제하지 않고 한 벌을 네 풀이 참조한다.
- **기획서 수치 대조(10-31 사용자 "기획서에 없는 수치는 넣지 않는다"):** 원본은 `Dropbox/Project L/기획 브레인스토밍/안재령의고뇌/특성 기획서.pptx`(29장, SHA-256 앞자리 `9014D34B`). 명시된 효과량은 SO와 같았다. 효과량이 미정인 9개는 성장 표와 네 풀에서 참조만 뺐다(SO·`.meta`·외형은 보존, 남은 확인용 값은 기획값이 아니다). 강화형은 원문에 없어 더하지 않았다. 지속 시간·발동 정책은 Core 그대로다.

| 구분 | 특성(원문 슬라이드) |
|---|---|
| 뺀 9개(미정 항목) | 선봉장(5, 공격력 n) · 깔끔한 뒷정리(6) · 사기 증진(9, 비율 `%`) · 결속(13, 방어력·보호막 n) · 진지 구축(16, 감소율) · 가속(21) · 방어구 부수기(24) · 추적 사격(26, 추가 피해율 n) · 사냥꾼(27, 공격력 보상 n) |
| 남긴 13개(대조한 값) | 단독 행동(7, 반경 1·보호막 10) · 피의 부름(8, 10%) · 무장 해제(10) · 발목 부수기(11) · 희생(14, 반경 2·턴당 1회) · 도발(15, 반경 2) · 굳건한 의지(17) · 땅 다지기(18, 이동 +1) · 먼지 연막(19, 사거리 −1·피해 −20%) · 뛰어넘기(20) · 고정 사격(23, 100%) · 마무리 사격(25, 20% 미만) · 일점 포격(28, 50%) |

- 지금 풀: Main·Heavy = 넉백 + 13개(14개), Rider·Cannon = 13개. 위치 교대(12장)·정찰대(22장)는 22개 범위 밖이고, 퇴역한 2연발·도어 브리칭은 되살리지 않았다.
- 11-02 사용자 지시로 병과 에셋의 `legacySteps`·`legacyPool`과 대조 코드를 걷었다. 증강 레벨은 병과 에셋 `Steps`의 `Unlock Level` 한 곳만 고치면 된다.
- 재생 확인은 **새 Run**에서 한다. 이전 획득·강화 두 단계를 이미 고른 Run은 이력 길이가 맞지 않아 `TraitGrowthRules.NextStep`이 거절할 수 있다(이력 이전은 범위 밖).

### 4.10 TG10 강화 노드는 다루지 않는다

- 강화 노드(1202)를 Core가 `EnhancementShopUnavailable`로 거절하는 것은 증강과 무관하다. 원인은 서브 강화 판매가 걷히고 메인 강화 상점이 아직 없는 것이다.
- 증강 작업은 맵 SO·강화 노드·강화 SO를 고치지 않는다. 노드를 맵에서 잠시 빼고 되돌리는 결정과 메인 강화 상점 설계는 [강화 View 설계 §14·§15](2026-10-19-enhancement-view-integration-design-Hojin.md#15-메인-강화-상점-me1me8-2026-10-29-초안)에 있다.
- 메인 강화 상점이 생겨도 증강이 남아 있으면 노드 진입이 먼저 막힌다(`PendingTraitGrowth`). 둘은 서로를 소모하지 않는다.

<a id="146-원칙-점검"></a>

## 5 원칙 점검

| 기준 | 적용 |
|---|---|
| SSOT | 대상·단계·후보·결과·진행은 Core·Buffer가 갖는다. 열림 여부는 Buffer 상태와 Stage 사실에서 계산하고, 잠금은 요청 수명의 대기 번호에서 읽는다. View가 갖는 것은 지금 그린 제안 참조, 고른 제안, 숨김 상태, "결과 표시 중"뿐이다 |
| SRP | 그리기(UI), 화면 값(모델 빌더), 제출·결과 소비(Controller), 번호 보관(요청 수명), 순서(Stage 사실·Plan·Job), 여는 규칙(호스트)이 나뉜다 |
| OCP | 새 특성은 SO·외형 행만 더한다. 코드 분기가 없다 |
| ISP·DIP | Controller는 좁은 Buffer 싱크·외형 조회만 받는다. 로스터 서비스는 Stage 쪽만 부른다 |
| JMH·FW | Plan·Job·Buffer 번호 계약(상점·회복과 같은 꼴). UI 개폐는 `IUISystemService`, 입력은 `UIInputBinding`(현강님 규칙) |
| 변경 독립성 | 자리를 바꾸면 프리팹·호스트 줄·여는 규칙만. Controller는 언제 여는지를 모른다. 성장 기능을 빼면 화면·Controller·요청 수명·Stage 증강 줄·연결 줄만. 상점·회복·덱 편집은 그대로 |

<a id="147-phase안과-확인"></a>

## 6 확인 항목

새 시험 코드는 만들지 않았다. 확인은 사용자 재생이다(결과는 계획 TG-d).

- 레벨 2가 된 유닛으로 전투에서 돌아오면 팝업이 열리고 카드가 나오며, 다음 노드로는 갈 수 없다. 키보드·컨트롤러 Submit으로도 고를 수 있다.
- 고르면 결과와 "계속"이 보이고, 누르면 다음 제안으로 바뀌거나 닫힌다. 덱 편집에 새 특성이 보이고 다음 전투에 반영된다.
- 숨기면 맵·설정·호버·덱 편집을 쓸 수 있고, 다음 노드 또는 다시 여는 버튼으로 다시 뜬다. 다시 여는 버튼은 성장이 끝나면 사라진다.
- 결과가 보이는 중 숨겼다 다시 열어도 같은 결과 문구다. 팝업 위로 설정 창이 덮이고, 닫으면 팝업으로 돌아온다.
- 상점에서 높은 레벨 유닛을 고용하면 상점을 나간 뒤 열린다.
- 씬 재진입·고르기 전 재연결에서 같은 카드, 처리 전 재연결에서 결과가 한 번만 보이고 확인된다.
- 표가 없는 데이터에서는 팝업이 열리지 않고 진행이 그대로다.

<a id="148-3판-검토-반영-2026-10-28"></a>

## 7 검토 기록

- 3판 설계 검토(10-28, 새 에이전트 하나, 인계·HEAD 대조): 높음 3·중간 6·낮음 6을 모두 반영했다. 요지는 요청 수명이 `Submit`을 감싸 번호 보관, 회복 수명과 다른 점(자동 확인·취소 없음) 명시, "계속" 뒤 확인(Q-TG7 A)과 `IsPending`으로 준비 중 화면 유지, Parallel과 묶음 안 배경·입력 Layer, 첫 카드 초점·Submit, 잠금 원본 `HasPending`, 바뀜 열쇠, 상점이 닫힌 뒤 열기, 천명님·기획 합의 표시다.
- 검토 뒤 사용자 정정(10-28): "열기·닫기·나중에는 화면을 어디에 놓을지가 정해져야 결정된다." 3판의 "자동으로 열림, 닫기 없음" 확정을 자리 결정으로 미루고, 여는 규칙을 호스트로 옮겼다. 임시 자리 동작은 Q-TG12로 정했다.
- 구현 묶음 끝 검토(10-29)의 지적과 반영은 [계획 §3](../plans/2026-10-28-trait-growth-plan-Hojin.md#3-작업별-결과)이다.

<a id="145-보류와-범위-밖"></a>

## 8 보류·결정 대기

| 항목 | 상태 |
|---|---|
| 화면 자리(Q-TG8)와 그에 딸린 것 | 10-28 사용자 "나중에". 열기(자동·버튼), 닫기·"나중에", 뒤를 막는 방식, 다른 팝업과 겹침은 자리가 정해져야 정한다. 정하면 프리팹·호스트 줄·여는 규칙만 바꾼다. StageSelect로 정해지면 천명님 합의 |
| 증강 화면 다듬기 | 10-29 사용자 "나중에 UI적이나 버튼·아이콘 작업은 더할 것". 지금 프리팹은 에디터 도구가 만든 임시 모양이다. 고정 글자·위치·크기는 프리팹, 바뀌는 글자는 `TraitGrowthPopupText`, 카드 모양은 공용 카드 원본에서 고친다. 키보드 자동 Navigation이 팝업 뒤 버튼으로 넘어가는지(검토 L7)도 이때 본다 |
| 운영 콘텐츠 등록 | 운영 값·후보 풀은 기획이 정하고 승인 뒤 등록한다. 카탈로그 조건은 §2.1 |
| 강화형 이름·설명을 정보 창에 | 정보 창은 `TraitKeys`만 받는다. 필요하면 Core 읽기 사실 요청 |
| 전투 결과 화면의 레벨업 알림 | 전투 View(용근님) 영역 |
| 메인 강화 상점 | 범위 밖. [강화 View 설계 §14·§15](2026-10-19-enhancement-view-integration-design-Hojin.md#145-보류와-범위-밖)(SO 재사용·이름 `MainEnhancement…`·1202 맵에서 빼기와 되돌리기) |

## 9 다른 작업자 영향

| 작업자 | 고친 곳 | 기록 |
|---|---|---|
| 현강님(UI) | `UIIDs.cs` `TraitGrowth = 42`(10-29 병합 때 41을 현강님 `TurnDisplay`가 먼저 써서 옮김), UISystem `Screens/Stage/`의 새 화면 파일 넷, `StageMapCanvasView`의 `traitGrowth` 칸. 사용자 허락분(10-28) | [현강님 공유](../handoffs/2026-10-29-trait-growth-ui-hyeongang-share-Hojin.md) |
| 민형님(Framework 연결 줄) | `InGameViewSystemFactory`·`InGameViewSystemGroup`·`LifeCycleSystemServiceKey`·`LifeCycleSystemGroup`·`StageMapSceneFactory`의 연결 줄. 걷힌 `JobRootType.TraitGrowth`·Plan 등록 줄은 리팩토링 쪽 공유를 따른다 | [민형님 공유](../handoffs/2026-10-29-trait-growth-minhyeong-share-Hojin.md) |
| 천명님(Core) | Core 파일 변경 없음. 인계 §9의 View·Flow 쪽 항목을 이 작업이 맡는다. Q-TG8 합의, 외형 행 필수·병과 넷·후보 셋 규칙을 알린다 | [Core 전달 문서 §51](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#51-레벨업-특성-성장증강-viewflow-인수-2026-10-29-호진-core-파일-변경-없음-미전달) |
| 용근님(전투 View) | 없음(Q-TG8을 전투 씬 안으로 정하면 생김) | — |

모든 공유는 미전달이다(전달 여부는 [계획 §4](../plans/2026-10-28-trait-growth-plan-Hojin.md#4-공유-메모)).
