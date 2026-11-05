# 스테이지 선택 View 평가 설계 — 13기준 100점

- 작성자: 호진(이호진)
- 상태: 초안. 결정 대기(§8 Q-SE1~Q-SE5). 코드는 바꾸지 않는다.
- 요청: 2026-11-05 사용자 "스테이지 선택 부분을 리팩토링" → "평가 설계부터".
- 틀: [로비 View 평가 설계](2026-11-05-lobby-view-evaluation-design-Hojin.md)와 같은 13기준·감점표·절차를 쓴다. 이 문서는 범위와 슬라이스·렌즈만 스테이지 선택에 맞게 바꾼다. 로비 쪽 점수와 견줄 수 있게 기준과 감점은 바꾸지 않는다.

## 1 목적

- 스테이지 선택 코드가 SSOT·SOLID·JMH Framework 지침·UI 규약 등을 지키는지 점수로 잰다. 그 문제 목록으로 스테이지 선택 리팩토링 설계를 쓴다.
- 같은 기준 판·같은 판정 자료로 다시 계산하면 같은 점수가 나와야 한다. 리팩토링 전후를 비교하기 위해서다.
- **코드는 바꾸지 않는다.** 정적 검토이고, 컴파일·Unity 실행·Profiler 측정은 하지 않는다.

## 2 범위

### 2.1 점수에 들어가는 것 (133개 파일, 12,276줄)

호진이 만들거나 손댄 스테이지 선택 코드다. 로비와 같은 규칙으로 고른다(Q-SE1). 판정은 파일 단위다. 호진 커밋(`CCRC_Hojin`·`Leehojin`)이 한 번이라도 닿은 파일이면 넣고, 다른 사람이 뒤에 고친 줄도 그 파일 안이면 범위다. 줄 수는 2026-11-05 작업 트리 기준이고, 기준 판이 정해지면 다시 센다.

| 묶음 | 파일 | 줄 |
|---|---|---|
| 맵 View 바탕 `ViewSystem/StageMap/*`(표시·입력 쪽 제외) | 18 | 2,084 |
| 맵 표시·입력 `ViewSystem/StageMap/*`(노드·연결선·카메라·선택·호버) + `Background/`·`Editor/` | 20 | 2,206 |
| 상점·회복·증강 팝업 `StageMap/Shop/**`·`Recovery/**`·`TraitGrowth/**`, 요청 수명 `ViewSystem/Recovery/**`·`ViewSystem/TraitGrowth/**` | 22 | 2,256 |
| 덱 편집 팝업 `StageMap/Deck/**` | 5 | 711 |
| 스테이지 선택 입력 `ViewSystem/StageSelectionInput/**` + 서비스 키 `StageSelectionInputKey` | 7 | 363 |
| UI 화면 `UISystem/Screens/Stage/*`(맵 캔버스·선택 패널·제안 카드·덱 편집·출전 확인·증강) | 17 | 1,705 |
| View DB 입력·조회 `Data/View/Table/{Recovery,StageMap,StageMapBackground,StageNode,StageOfferText}/**`·조회 표 | 19 | 1,062 |
| LifeCycle `LifeCycles/Stage/**`(Plan·Job·진입·증강 진행) | 17 | 1,374 |
| View 입력 Buffer `BufferSystems/{StageView,ShopView,RecoveryView}/*` | 7 | 472 |
| 연결: `StageMapSceneFactory` | 1 | 43 |

맵 View 바탕 줄 수에는 Factory 연결 줄이 따로 빠져 있다(위 마지막 줄). 파일 하나하나는 부록 A에 있다.

### 2.2 범위 밖

- **호진이 손대지 않은 파일**(감점 안 함, Q-SE1 A일 때). 스테이지 선택 기능에 속하지만 다른 사람만 쓴 파일이다. 목록은 부록 B(27개).
  - 용근님: 덱 편집 창 `StageDeckEditWindow`·`IStageDeckEditFacts`, `DeckEditText`·`DeckEditPopupViewScreen`, `RecoveryDescriptionBindings`, 조회 표 둘·표 SO 셋 등.
  - 현강님: 덱 편집 슬롯·정보 위젯(`DeckEditSlotView`·`OwnedUnitSlot`·`DeckSlotInfo*` 등).
  - 민형님·천명님: Buffer 키 셋, `StageUI`, `TraitGrowthViewBuffer`.
- **로비 평가에서 이미 본 공용 부품**(Q-SE2 A): `ViewSystem/Common/*`(팝업 수명 `PopupViewScope`·`PopupInputSession`, 체스 말, 가림 판정 등). 스테이지 선택 코드가 이것을 **잘못 쓰는 몫**은 범위 안 코드의 문제로 센다.
- **Core 쪽 결과 Buffer**: `BufferSystems/Current/*`·`Recovery/*`·`Stage/*`·`Character/*`. Core가 쓰는 결과라 기반으로 본다.
- **전투 쪽 입력** `ViewSystem/StageInput/**`: 전투 View 영역이다.
- **기반**(감점 안 함): Framework, GlobalSystem 내부(UISystem Base·InputSystem·Render·Audio), Core, 공용 Factory 조립, 로비·전투 등 다른 씬 코드, 시험, 에디터 도구 `Tool/Editor/**`.
- 기반이 강제한 한계는 감점하지 않는다. 범위 안 코드가 그 한계를 흡수하면서 사본이나 우회를 만들면, 그 몫만 깎는다.

## 3 기준 13개

로비 평가 §3과 같다(C1~C12 전투 평가 기준 + C13 UI 규약). 표는 그 문서를 원본으로 하고 여기 다시 적지 않는다.

- C13을 그대로 두는 까닭: 스테이지 선택도 UI 화면·팝업 비중이 크다(UI 화면 1,705줄, 팝업 2,967줄).
- 문제 하나는 주된 기준 하나에만 둔다. UI 서비스 등록·Export 경로 문제는 C3, UI 화면 쪽 규약은 C13이다.

## 4 감점표·점수

로비 평가 §4와 같다.

- 기준마다 100점에서 깎는다. 높음 −20, 중간 −7.5, 낮음 −2.5.
- 낮음은 기준마다 −10까지, 같은 근본 원인은 기준 안에서 −15까지. 높음 하나는 그대로 −20이다.
- 평균 셋: 요청 7기준(C1~C7), 12기준(C1~C12), 13기준(C1~C13, 대표 점수).

## 5 절차

로비는 찾기 12·합치기 1·검증 7·정리 1로 21회였다. 스테이지 선택은 범위가 로비의 약 1.4배라 슬라이스를 하나 늘린다. 실행은 약 25~35회로 본다(Q-SE5).

1. **찾기 13명**
   - **슬라이스 7명:** 범위 파일을 묶음별로 나눠 **끝까지 읽는다**. 다 읽었는지 파일 목록으로 확인한다.

     | 슬라이스 | 묶음 | 줄 |
     |---|---|---|
     | S1 | 맵 View 바탕(시스템·Scope·서비스·씬·팝업 묶음·진입·출전·팝업 수명) + Factory | 2,127 |
     | S2 | 맵 표시·입력(표시·상호작용·노드·연결선·현재 위치·카메라·호버) + 배경·에디터 | 2,206 |
     | S3 | 상점·회복·증강 팝업 + 요청 수명 | 2,256 |
     | S4 | 덱 편집 팝업 + 스테이지 선택 입력 | 1,074 |
     | S5 | UI 화면 | 1,705 |
     | S6 | View DB 입력·조회 표 + View 입력 Buffer | 1,534 |
     | S7 | LifeCycle | 1,374 |

   - **렌즈 6명:** 범위 전체를 관점 하나로 훑는다.
     - L1 SSOT: 규칙·상태·상수 사본(맵 진입 가능 판정, 노드 상태, 팝업 열림 조건 등)
     - L2 SOLID·변경 독립성: 변경 시나리오로 확인한다. 예: 노드 종류 하나 더하기, 제안 팝업(상점 종류) 하나 더하기, 맵 진입 규칙 바꾸기, 맵 배치·배경 바꾸기, 카메라 구도 바꾸기, 덱 편집 규칙 바꾸기
     - L3 FW 계약 + UI 규약
     - L4 정확성·수명·오류 경계(결함 사냥). 씬 전환·팝업 겹침·요청 수명·끊긴 연출
     - L5 매 프레임·핫패스 할당·불필요한 반복 검사(카메라 끌기·노드 호버·말 이동)
     - L6 캡슐화·C#/Unity 관례·주석 정합성·죽은 코드
   - 모두 위치·내용·실패 시나리오·고칠 방향·기준·심각도를 적는다.

2. **합치기 1명.** 같은 문제를 묶고, 기준을 넘는 중복은 주된 기준 하나로 정한다.

3. **적대적 검증**
   - 중간 이상 후보는 증거·심각도 검증자와 반론 검증자가 따로 판정한다. 둘이 갈린 것에만 판정자 한 명을 더 두고, 셋의 가운데 값을 쓴다(로비 실행 방식).
   - 낮음 후보는 묶어서 검증자 1~2명이 판정한다. 중간 이상으로 올린 것은 위의 검증을 다시 거친다.

4. **점수.** §4 감점표로 계산한다. 판정 자료(문제 목록과 최종 심각도)를 문서에 남겨 다시 계산할 수 있게 한다.

**독립성**
- 검토자에게 `CLAUDE.md`의 현재 상태 서술, 과거 평가·검토 문서, git 기록을 근거로 쓰지 못하게 한다. 11-04 [스테이지 맵 동작 위험 정리 설계](2026-11-04-stage-map-behavior-risk-design-Hojin.md)도 여기에 든다(Q-SE4).
- 근거는 지금 코드와 규약 문서다: `framework-programmer-guide.md`, `Assets/AGENTS.md`, `CLAUDE.md`의 코딩 규칙·DB 규칙 절.
- 로비 실행 때 검토자 프롬프트에 `CLAUDE.md`가 자동으로 들어갔다. 이번에도 그럴 것이라 보고서 §방법에 적는다.
- 모든 검토자는 읽기 전용이다.

## 6 기준 판

- **R-M-03 수정(로비 재평가 마지막 중간)을 커밋한 뒤의 커밋 하나**다(Q-SE3). 지금은 그 수정이 커밋 전이라 실행을 기다린다.
- 커밋 해시를 보고서 머리에 적는다. 실행 전에 범위 파일이 기준 판과 같은지 `git diff`로 확인한다.
- 작업 트리의 `StageMap_Chapter2`·배경 표 등 에셋 변경은 범위 코드가 아니라 영향이 없지만, 커밋 여부와 상관없이 검토자는 기준 판 코드만 본다.

## 7 산출물

- 보고서: `docs/superpowers/specs/<실행일>-stage-select-view-evaluation-Hojin.md`
  - 결과 표: 기준별 점수와 중간·낮음 수, 깎인 까닭
  - 방법(실행 횟수·독립성 확인 포함)
  - 중간 이상 문제 전체
  - 기반 한계(감점 없음)
  - 11-04 동작 위험 설계 R1~R4와의 대조(§8 Q-SE4)
- 문제 목록: `docs/superpowers/specs/<실행일>-stage-select-view-evaluation-issues-Hojin.md`
  - 검증을 통과한 문제 전부
  - 위치·내용·실패 시나리오·고칠 방향·기준·검증 판정
- 평가 끝에 Phase 끝 Codex 검수를 받는다. 검수 대상은 보고서의 근거와 점수 계산이다.
- 평가 결과로 스테이지 선택 리팩토링 설계를 쓴다. 11-04 동작 위험 설계의 Q-BR1~Q-BR4도 그 설계에서 함께 정한다. 평가 단계에서는 코드를 바꾸지 않는다.

## 8 결정 대기

| # | 묻는 것 | 권장(A) | 다른 안 |
|---|---|---|---|
| Q-SE1 | 범위 규칙 | **A 로비와 같게, 호진이 닿은 파일만**(133개). 다른 사람만 쓴 27개는 부록 B에 남기고 감점하지 않는다 | B 스테이지 선택 기능 파일은 작성자와 상관없이 모두(160개). 덱 편집 창 등 용근님 줄까지 점수에 들어가 로비와 규칙이 달라진다 |
| Q-SE2 | 공용 부품 `ViewSystem/Common/*` | **A 빼기.** 로비 평가·2차 리팩토링에서 이미 보고 고쳤다. 잘못 쓰는 몫만 범위 안으로 센다 | B 다시 넣기. 같은 문제를 두 번 세게 된다 |
| Q-SE3 | 기준 판 | **A R-M-03 커밋 뒤 커밋 하나** | B 지금 HEAD `cc3ccdf96`(범위 파일은 R-M-03과 겹치지 않아 결과는 같음, 기록만 덜 깔끔) |
| Q-SE4 | 이미 아는 문제(11-04 R1~R4) | **A 검토자에게 알리지 않고, 끝난 뒤 대조**(로비 §8과 같음). 찾지 못한 것은 보고서에 따로 적는다 | B 미리 알려 확인시키기. 독립성이 떨어진다 |
| Q-SE5 | 규모 | **A 찾기 13(슬라이스 7·렌즈 6), 실행 약 25~35회** | B 슬라이스 6으로 줄이기(S1·S2를 합쳐 4,300줄 한 명). 다 읽기가 어려워 빠질 수 있다 |

## 부록 A 범위 파일 (133개)

경로 머리는 `Assets/ProjectL/Script/Systems/`다. 줄 수는 2026-11-05 작업 트리 기준이다.

| 파일 | 줄 |
|---|---|
| `DataBaseSystem/Data/View/Table/Recovery/Item/RecoveryOptionVisualData.cs` | 31 |
| `DataBaseSystem/Data/View/Table/Recovery/Item/RecoveryOptionVisualDataSO.cs` | 27 |
| `DataBaseSystem/Data/View/Table/StageMap/Item/StageMapViewData.cs` | 107 |
| `DataBaseSystem/Data/View/Table/StageMap/Item/StageMapViewDataSO.cs` | 67 |
| `DataBaseSystem/Data/View/Table/StageMapBackground/Item/StageMapBackgroundData.cs` | 33 |
| `DataBaseSystem/Data/View/Table/StageMapBackground/Item/StageMapBackgroundDataSO.cs` | 18 |
| `DataBaseSystem/Data/View/Table/StageMapBackground/StageMapBackgroundDataTableSO.cs` | 11 |
| `DataBaseSystem/Data/View/Table/StageNode/Item/StageNodeVisualData.cs` | 46 |
| `DataBaseSystem/Data/View/Table/StageNode/Item/StageNodeVisualDataSO.cs` | 35 |
| `DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferErrorKind.cs` | 20 |
| `DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferLineKind.cs` | 23 |
| `DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferTextData.cs` | 223 |
| `DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferTextDataSO.cs` | 108 |
| `DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferTextTarget.cs` | 15 |
| `DataBaseSystem/Data/View/Table/StageOfferText/StageOfferTextDataTableSO.cs` | 11 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/IStageMapBackgroundTableQuery.cs` | 15 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/StageMapBackgroundTable.cs` | 70 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/StageNodeVisualTable.cs` | 93 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/StageOfferTextTable.cs` | 109 |
| `Factory/Runtime/InGame/View/StageMapSceneFactory.cs` | 43 |
| `RuntimeSystem/BufferSystems/RecoveryView/RecoveryViewBuffer.cs` | 124 |
| `RuntimeSystem/BufferSystems/RecoveryView/RecoveryViewCommand.cs` | 23 |
| `RuntimeSystem/BufferSystems/ShopView/ShopViewBuffer.cs` | 69 |
| `RuntimeSystem/BufferSystems/ShopView/ShopViewCommand.cs` | 90 |
| `RuntimeSystem/BufferSystems/StageView/StageViewBuffer.cs` | 104 |
| `RuntimeSystem/BufferSystems/StageView/StageViewCommand.cs` | 53 |
| `RuntimeSystem/BufferSystems/StageView/StageViewCommandKind.cs` | 9 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckCombatConfirmUI.cs` | 65 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs` | 659 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckSlotInfoUI.cs` | 110 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IDeckCombatDeparture.cs` | 20 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IStageOfferDisplay.cs` | 18 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IStageOfferInput.cs` | 10 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageLegendUI.cs` | 18 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageMapCanvasView.cs` | 71 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageOfferCardContent.cs` | 41 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageOfferCardView.cs` | 85 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageOfferLayoutView.cs` | 264 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageOfferPopupModel.cs` | 42 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageOfferScreenUI.cs` | 136 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageOfferUI.cs` | 20 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageScreenRootUI.cs` | 57 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageSelectPanelUI.cs` | 64 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/TraitGrowthUI.cs` | 25 |
| `RuntimeSystem/InGameSystem/ViewSystem/Recovery/IRecoveryRequestExecution.cs` | 17 |
| `RuntimeSystem/InGameSystem/ViewSystem/Recovery/IRecoveryRequestStatus.cs` | 13 |
| `RuntimeSystem/InGameSystem/ViewSystem/Recovery/IRecoveryRequestSubmitter.cs` | 11 |
| `RuntimeSystem/InGameSystem/ViewSystem/Recovery/RecoveryRequestLifetime.cs` | 259 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Background/StageMapBackgroundDisplay.cs` | 128 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditDisplayModel.cs` | 95 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditDraft.cs` | 247 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditModelBuilder.cs` | 93 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditPopupController.cs` | 244 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditPopupViewFactory.cs` | 32 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Editor/StageMapCameraFramingPending.cs` | 141 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Editor/StageMapCameraFramingReceiver.cs` | 202 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Editor/StageMapCameraFramingSettings.cs` | 98 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Editor/StageMapSceneRootEditor.cs` | 78 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageMapCameraDrag.cs` | 15 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageMapEntryPresentation.cs` | 13 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageMapInteractionFeedback.cs` | 22 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageMapPointerBlocker.cs` | 9 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageMapSceneFactory.cs` | 16 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageNodePicker.cs` | 13 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Recovery/RecoveryPopupController.cs` | 239 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Recovery/RecoveryPopupModelBuilder.cs` | 111 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Recovery/RecoveryPopupViewFactory.cs` | 50 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/EnhancementShopOfferContentBuilder.cs` | 33 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/IShopOfferContentBuilder.cs` | 14 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/ItemShopOfferContentBuilder.cs` | 29 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/ShopPopupController.cs` | 400 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/ShopPopupModelBuilder.cs` | 207 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/ShopPopupViewFactory.cs` | 80 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/ShopResultPolicy.cs` | 91 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/TrainingCardContentBuilder.cs` | 79 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/UnitShopOfferContentBuilder.cs` | 53 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageCombatDeparture.cs` | 172 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageCurrentMarkerView.cs` | 128 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageEntryIdentity.cs` | 46 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageLinkView.cs` | 76 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapCameraDrag.cs` | 195 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapEntryObserver.cs` | 112 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapInteraction.cs` | 206 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapNodeLayout.cs` | 51 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapNodeMovement.cs` | 128 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapPointerBlocker.cs` | 15 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapSceneRoot.cs` | 39 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapScreenPresentation.cs` | 90 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapSelectionPresentation.cs` | 128 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewDisplay.cs` | 348 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewEntry.cs` | 110 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewInput.cs` | 87 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewPopups.cs` | 111 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewReader.cs` | 75 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewScene.cs` | 101 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewScope.cs` | 311 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewServices.cs` | 139 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewSystem.cs` | 283 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageNodeHoverInfo.cs` | 150 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageNodePicker.cs` | 37 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageNodeView.cs` | 204 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageOfferPopupViewScreen.cs` | 25 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/StagePopupViewLifetime.cs` | 188 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/TraitGrowth/TraitGrowthPopupController.cs` | 216 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/TraitGrowth/TraitGrowthPopupModelBuilder.cs` | 102 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/TraitGrowth/TraitGrowthPopupViewFactory.cs` | 52 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/IStageSelectionInputReceiver.cs` | 28 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputController.cs` | 114 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputFactory.cs` | 31 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputLayers.cs` | 20 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputNames.cs` | 20 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionReceiverSlot.cs` | 135 |
| `RuntimeSystem/InGameSystem/ViewSystem/TraitGrowth/ITraitGrowthRequestExecution.cs` | 15 |
| `RuntimeSystem/InGameSystem/ViewSystem/TraitGrowth/ITraitGrowthRequestSubmitter.cs` | 14 |
| `RuntimeSystem/InGameSystem/ViewSystem/TraitGrowth/TraitGrowthRequestLifetime.cs` | 171 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/IStageEntryAttemptRead.cs` | 11 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/IStageProgress.cs` | 35 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/IStageTraitGrowthFacts.cs` | 17 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/RecoveryCommandJob.cs` | 61 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/ShopCommandJob.cs` | 36 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageCheckJob.cs` | 38 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageJob.cs` | 75 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageNodeEntryJob.cs` | 36 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/TraitGrowthCommandJob.cs` | 61 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/TraitGrowthPrepareJob.cs` | 34 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs` | 200 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StageStep.cs` | 40 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageEntryAttempts.cs` | 80 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageLifeCycle.cs` | 237 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageProgress.cs` | 194 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageProgressFacts.cs` | 104 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageTraitGrowthProgress.cs` | 115 |
| `ServiceKeys/Runtime/StageSelectionInputKey.cs` | 15 |

## 부록 B 범위 밖: 호진이 닿지 않은 스테이지 선택 파일 (27개)

Q-SE1 A일 때 감점하지 않는다. 작성자는 git 기록의 커밋 작성자다.

| 파일 | 줄 | 작성자 |
|---|---|---|
| `DataBaseSystem/Data/View/Table/Recovery/RecoveryOptionVisualDataTableSO.cs` | 11 | 용근 |
| `DataBaseSystem/Data/View/Table/StageMap/StageMapViewDataTableSO.cs` | 11 | 용근 |
| `DataBaseSystem/Data/View/Table/StageNode/StageNodeVisualDataTableSO.cs` | 11 | 용근 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/RecoveryOptionVisualTable.cs` | 57 | 용근 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/StageMapViewTable.cs` | 44 | 용근 |
| `RuntimeSystem/BufferSystems/TraitGrowthView/TraitGrowthViewBuffer.cs` | 96 | 천명 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditCancelBinding.cs` | 21 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditSlotView.cs` | 151 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckOwnedFilter.cs` | 77 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckSlotInfoAbilities.cs` | 151 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckSlotInfoExperience.cs` | 29 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckSlotInfoStats.cs` | 93 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IDeckEditDisplay.cs` | 21 | 용근 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IDeckEditDragReceiver.cs` | 13 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IDeckEditInput.cs` | 21 | 용근,현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IDeckEditInteractionReceiver.cs` | 17 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/OwnedUnitSlot.cs` | 181 | 현강 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageUI.cs` | 39 | 민형 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageSelectInput.cs` | 80 | 용근 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditPopupViewScreen.cs` | 25 | 용근,민형 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditText.cs` | 69 | 용근 |
| `RuntimeSystem/InGameSystem/ViewSystem/StageMap/Recovery/RecoveryDescriptionBindings.cs` | 47 | 용근 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/IStageDeckEditFacts.cs` | 36 | 용근 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageDeckEditWindow.cs` | 63 | 용근 |
| `ServiceKeys/Runtime/Buffer/RecoveryViewBufferKey.cs` | 12 | 민형 |
| `ServiceKeys/Runtime/Buffer/ShopViewBufferKey.cs` | 12 | 민형 |
| `ServiceKeys/Runtime/Buffer/TraitGrowthViewBufferKey.cs` | 12 | 천명 |
