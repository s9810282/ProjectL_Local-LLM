# 상점 스테이지 UI 설계

- 작성자: 호진(이호진)
- 상태: 구현 완료(상점 S1~S5, 혼합 상점 대응, 10-19 이벤트 팝업 UI 정리). 10-19 UI 정리의 Unity 표시 확인과 사람의 수동 Play가 남았다. Q1(Run seed 소유)은 결정 대기다.
- 범위: StageSelect 안 상점 팝업의 진입·구매·나가기·복구·결과 분류·화면 규격. 상점 규칙(후보·가격·가중치·방문당 구매 수)은 천명님 Core 범위다. 회복은 [회복·이벤트 설계](2026-10-11-noncombat-event-recovery-design-Hojin.md), 강화 방문의 표시는 [강화 View 연결 설계](2026-10-19-enhancement-view-integration-design-Hojin.md)를 따른다.
- 진행 전: 상점·회복 "제출 중" 사실 하나와 두 팝업 UI 공통 기반은 [로비·스테이지 검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md) §7에서 다룬다. 이 문서의 UI 제출 경로(§2.1·§2.2, `ShopUI`가 `ShopViewBuffer`에 직접 제출)는 그 작업 전 모습이다.
- 계획: [상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md)
- 정리: 2026-10-27 머리말의 "10-13 상태 — 지금과 다른 곳" 줄을 본문에 반영해 없앴다. 회복 화면·DB·임시 방어 설명은 회복 설계로 넘기고 상점 중심으로 다시 썼다. 계획에 있던 결정 P1~P14도 §3으로 옮겼다. 파일 이름은 그대로다. 옛 판은 git `10914570`.

## 1 목적과 범위

상점 노드는 새 씬 없이 StageSelect 안의 UI 팝업으로 연다(S1). 구매하지 않아도 "선택하지 않는다"로 노드를 완료하고 맵으로 돌아간다(S4). UI는 현강님 UI 구조(`UIBase`·`UIInputBinding`·UISystem 개폐)를 따른다(S7). 상점을 먼저 만들고 같은 틀을 회복에 적용했다(S8).

범위 밖: 진짜 이벤트(선택지·랜덤 보상, Core에 없음), Run seed 도입(§6 Q1), 보스·챕터 완료, 저장·불러오기, 맵당 상점·이벤트 개수(기획 결정 뒤 Core 요청).

## 2 지금 구조

### 2.1 실행 흐름

```text
[맵 클릭]  StageViewBuffer → StageNodeEntryFlow → JobRegister → StagePlan → StageNodeEntryJob
           → Core가 기대 번호의 입력을 다시 읽어 EnterShop(seed) → 상태·처리 결과 게시
[진입 결과] StageMapEntryObserver → ShopPopupController.TryRestore → PopupViewScope → UISystem.OpenUI(Shop)
           → 동기 OpenStateChanged → 팝업 Layer·모델 표시
[구매/나가기] ShopUI → ShopViewBuffer.Submit → ShopFlow → JobRegister → ShopPlan → ShopCommandJob
           → Core가 입력을 다시 읽어 Purchase(+같은 요청에서 Complete) / CompleteShop → 결과 게시
[결과]     ShopPopupController가 번호·방문 문맥을 맞춘다 → 표시·입력 정리
           완료·불일치 → UISystem.CloseUI → Layer 복귀 → 맵 다시 그리기 요청
```

- UI는 의도를 Buffer에 쓰고, Flow는 순서, Plan은 Job 구성, Job은 Core 호출을 맡는다. 입력 조회·규칙 판정·상태와 결과 게시는 Core가 한다. View는 결과를 읽어 표시하고 입력 수명을 정리한다.
- Stage·Shop·Artifact·Combat은 공용 JobRegister·Runner 하나를 쓴다. Stage 실행 계층(Flow)의 Plan·Job 재구성은 보류다([스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md) §11 보류).
- 전투 노드는 따로 `CombatEntryService` 진입 수명으로 간다. 회복 노드 흐름은 회복 설계를 따른다.

### 2.2 구성과 책임

| 구성 | 위치 | 맡는 일 |
|---|---|---|
| `StageFlow`·`StageNodeEntryFlow`·`ShopFlow` | `LifeCycleSystem/LifeCycles/Stage/` | 미처리 번호·대기 결과 확인과 Job 등록 순서 |
| `StagePlan`·`ShopPlan` / `StageNodeEntryJob`·`ShopCommandJob` | `Stage/Plans/`, `Stage/Jobs/` | 기대 번호·Core 서비스를 Job에 넘김. Stage Job은 임시 seed 공급원도 받는다 |
| `StageCommandProcessingService` | `CoreSystem/StageCommandSystem/` | 입력 직접 조회, 상점 서비스 호출, 결과 게시 |
| `StageMapViewSystem`·`StageMapViewScope`·`StageMapEntryObserver` | `ViewSystem/StageMap/` | 씬 연결, 진입 결과 관찰, 상점 Controller 소유, 맵 다시 그리기 |
| `ShopPopupViewFactory` | `ViewSystem/StageMap/Shop/` | Controller·모델 Builder·상품별 내용 Builder 셋 조립 |
| `ShopPopupController` | 같은 폴더 | 결과·방문 문맥 확인, 표시·입력 정리, 완료/불일치 뒤 닫기·맵 갱신, 복구. `PopupViewScope`를 소유 |
| `ShopPopupModelBuilder` + `IShopOfferContentBuilder`(Unit·Item·Enhancement) | 같은 폴더 | 방문·골드·구매 잠금은 모델 Builder, 상품 한 종류의 카드 내용은 종류별 Builder |
| `ShopResultPolicy`·`ShopPopupText` | 같은 폴더 | Core 사유 분류(§4), 문구 |
| `ShopUI`·`IShopPopupDisplay` | `GlobalSystem/UISystem/Screens/Stage/` | 받은 모델 표시, `ShopViewBuffer` 제출, 제출 잠금, 동기 `OpenStateChanged`·`SubmissionFailed` 알림 |
| `StageOfferCardView`·`StageOfferCardContent`·`ShopOfferCardEntry` | 같은 폴더 | 카드 한 장의 표시 값과 재사용 카드 부품 |
| `StageEventOfferDescriptionCard.prefab` | `Prefabs/UI/` | 상점·회복이 함께 쓰는 설명 카드(강화 설계 §4.2) |

- 입력 Layer는 `StageSelectionInputLayers.Popup`("StageMapPopup", Common 맵만)이다. 열 때 `PopupViewScope`가 `PopupInputSession`으로 잡고, 숨길 때 자기 Layer일 때만 StageSelection으로 되돌린다. ESC로 상점을 완료하지 않는다. 바뀐 것: 옛 전용 `ShopPopupInputLayers.Popup`(S5.2에서 루트 Factory에 등록) → 지금 스테이지 맵 공용 팝업 Layer.
- Controller는 `IShopPopupDisplay`의 `OpenStateChanged`·`SubmissionFailed`를 붙고 뗄 때 구독·해제한다. 이 알림은 수명·오류용이다. 버튼은 `UIInputBinding`의 명시적 수신 경로로 받는다.
- `UIId.Shop = 14`.

### 2.3 Core가 주는 것

상점 규칙의 원본은 천명님 [혼합 상점 인계](../handoffs/2026-10-18-mixed-shop-core-view-handoff.md)와 [강화 상점 인계](../handoffs/2026-10-19-enhancement-shop-core-view-handoff.md)다. View가 쓰는 공개 계약만 적는다.

| 계약 | 내용 |
|---|---|
| `IShopService` | `EnterShop(chapterKey, nodeId, seed)`, `Purchase(ShopVisitContext, ShopOfferId)`, `CompleteShop(ShopVisitContext)`. View는 직접 부르지 않고 `ProcessNextShopCommand(expectedRequestNumber)`를 거친다 |
| `ShopVisitContext` | `ChapterKey / NodeId / StageKey / EntryId`. 지금 Buffer 방문의 `EntryId`를 쓴다 |
| `CurrentShopData` | `Offers`(0~3개)·`EntryId`·`VisitKind`·`PurchasedMask`·`HasPurchase` |
| `ShopPurchaseSnapshot` | `Kind`(Unit·ConsumableItem·Enhancement)·`OfferId`·`CharacterKey`·`ItemKey`·`Cost` 등 |
| `ShopCommandResult` | `RequestNumber`·`Context`·`Kind`·`OfferId`·`PurchaseResult`·`CompletionResult` |
| 입력 | `ShopViewCommandData.Purchase(context, offerId)` / `.Complete(context)` |
| Run 골드 | `CurrencyBuffer.Amount` 하나 |

- 혼합 상점(천명님 `a5133b32`, 2026-10-19): 유닛·아이템을 섞어 최대 3개를 고정하고 방문당 한 개만 산다. 구매에 성공하면 Core가 같은 요청 안에서 완료까지 처리한다(`ShopCommandExecutor`). 완료가 실패하면 구매는 남고 Complete만 다시 시도한다. `HasPurchase`면 그 방문의 다른 상품은 모두 잠긴다.
- 바뀐 것: 옛 캐릭터 전용 상점(제안 3개를 각각 살 수 있음, `Purchase(chapterKey, nodeId, offerId)`) → 혼합 상점(10-19).
- 바뀐 것: 옛 "상점·회복 전용 Run 재화와 전투 스테이지 골드(`CombatStateData.StageGold`)를 분리" → Run Gold 통합([Core 전달 문서 §5.8](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#58-user-authorized-run-gold-integration-2026-10-11-not-sent))으로 `CurrencyBuffer.Amount` 하나.
- 진입 뒤 완료 전에는 같은 노드든 다른 노드든 다시 들어갈 수 없다(`CurrentStageUncleared`). 전투가 남아 있으면(`EndBattle` 전) 진입은 예외다.
- 상품은 진입 때 seed로 한 번 뽑혀 Buffer에 고정된다. 숨기거나 거절해도 다시 뽑지 않는다.

## 3 결정 기록

| ID | 결정 | 날짜·근거, 바뀐 것 |
|---|---|---|
| S1 | 상점·이벤트 스테이지는 새 씬 없이 StageSelect 씬 안의 UI로 처리한다 | 2026-10-06 사용자 |
| S2 | 이벤트 스테이지는 Core의 회복 스테이지(`StageType.Event`)로 진행한다 | 2026-10-06 사용자. 상세는 회복 설계 |
| S3 | 판매 후보는 임시값으로 먼저 채운다 | 2026-10-06 사용자. 지금 값은 §5 |
| S4 | 사지 않아도 나가기 버튼으로 노드를 완료한다 | 2026-10-06 사용자. 버튼 문구는 지금 "선택하지 않는다"(10-19) |
| S5 | 상점·회복 모두 InGame DB(규칙)와 View DB(표시)를 갖춘다 | 2026-10-06 사용자 |
| S6 | 화면 규격은 예전 `develop-4week` Shop·Recovery 씬(호진 `d2aa5642`)에 맞춘다 | 2026-10-06 사용자. 바뀐 것: 10-19 이벤트 스테이지 기획서 PPT 기준으로 다시 맞춤(§5) |
| S7 | UI는 현강님 UI 구조·문서를 따른다 | 2026-10-06 사용자 |
| S8 | 상점을 먼저 만들고 같은 틀을 회복에 적용한다 | 2026-10-09 사용자 |
| S9 | 두 화면은 배치가 거의 같고 아이콘·문구만 다르므로 카드 표시를 공용 부품으로 만든다 | 2026-10-09 사용자 |
| S10 | 카드 형식 이벤트는 표시 부품을 재사용하고, 새 이벤트 규칙(종류 식별·선택 처리·결과 게시)은 별도 Core 계약으로 넓힌다. 범용 이벤트 엔진은 만들지 않는다 | 2026-10-09 사용자(평가 반영 수정) |
| Q1 | Run seed가 없는 동안의 상점 seed | **결정 대기**(§6) |
| Q2 | 회복의 정상 거절 일부가 예외로 오는 계약 | 회복 설계로 옮김. 임시 방어(R-A)는 구현하지 않기로 했다(10-13) |
| Q3 | 맵 다시 그리기는 완료·불일치 처리 때 Controller가 한 번 알린다 | 2026-10-10 구현. 같은 맵 재표시는 노드·외형을 보존한다(P5.8) |
| Q4 | 회복 카드 그림 | 회복 설계로 옮김(`RecoveryOptionVisualDataSO.icon`, V3) |
| Q5 | 나가기 실패는 §4 분류를 따른다(정상 거절만 유지·재시도, 불일치는 복구, 예외는 정지) | 2026-10-09 사용자 |
| Q6 | 시작 Run 재화와 후보 임시값 | 2026-10-09 결정(시작 20, Akimbo 2·ShotGun 3·Sniper 5). 바뀐 것: §5 |
| P1 | 상점 노드 클릭 → StageViewBuffer → Flow·Plan·Job → Core `EnterShop` → 결과 관찰 뒤 열기 | S1·SA |
| P2 | 나가기 버튼만 눌러도 완료 → 수락 시 닫기·맵 갱신 | S4 |
| P3 | seed는 진입마다 `System.Random`(주입 함수, 시험은 고정값). Run seed는 Core 요청 R4 대기 | Q1 |
| P4 | 후보·시작 재화 임시값 | S3·Q6, 바뀐 것은 §5 |
| P5 | 카드는 `StageOfferCardView`·`StageOfferCardContent`. ShopUI 안에서 만들고 재사용·배치한다. 별도 카드 목록 클래스는 없다 | S9 |
| P6 | 거절은 Core 사유 값으로만 분류한다(§4) | 2026-10-10 사용자가 분류표 확인 |
| P7 | 씬 연결·맵 표시 뒤 Buffer에 클리어 전 상점이 남아 있으면 진입 요청 없이 다시 연다 | §4.2 |
| P8 | 입력 Buffer → Flow → JobRegister → Plan → Job → Core 직접 조회. UI 제출 잠금과 Flow 대기 확인으로 중복을 막고 오류 상태면 등록하지 않는다 | SA |
| P9 | 상점·회복 StageRoot는 각각 완성된 뒤 등록한다 | 사용자. 상점 S5.0(10-11), 회복 10-13 등록 |
| P10 | 상점 노드 출현 보장 장치는 만들지 않는다. 맵당 개수는 기획이 정한다 | 2026-10-09 사용자 |
| P11 | Claude는 화면에 보이지 않는 동작만 만든다. 보이는 것(UIBase 화면·카드 프리팹·씬)은 사용자와 다른 AI가 만든다 | 2026-10-09 사용자. 실제 ShopUI는 SA P5·P6에서 연결 |
| P12 | 통합 기획서 Ver0.3의 "목록 뽑기"가 아니라 지금 Core 규칙 기준으로 먼저 만든다(A안). 차이는 Core 요청 G1~G5 | 2026-10-09 사용자 |
| P13 | 초기 요청 객체·스케줄러는 SA로 대체. `ShopPlan`이 Job 목록을 돌려주고 Job은 기대 번호·Core 서비스만 받는다 | SA P6 |
| P14 | 성능 규칙(§7)을 코드와 화면 작업 안내에 적용하고 Profiler로 확인한다 | 2026-10-10 사용자 |

## 4 결과 분류와 화면 복구

### 4.1 분류

분류는 Core가 돌려준 사유 값으로만 한다(`ShopResultPolicy`). View가 규칙을 다시 계산하지 않는다.

| 분류 | 사유 | 처리 |
|---|---|---|
| 정상 거절 | `InsufficientFunds`·`AlreadyOwned`·`ItemQuantityOverflow`·`PurchaseAlreadyUsed`·`SoldOut`·`DeckCountOverflow` | 사유 문구를 보이고 Buffer를 다시 읽어 그린다. 팝업을 유지하고 다시 누를 수 있다 |
| 상태 불일치 | 그 밖의 구매 실패(강화의 `TargetNotOwned`·`AlreadyEnhanced` 포함), 진입·완료의 `ChapterProgressionFailureReason` | 같은 요청을 반복하지 않는다. 팝업을 닫고 지금 Buffer 상태로 복구한다. Run 종료 목적지는 Run 종료 정책을 따른다 |
| 내부 이상 | `UnknownOffer`·`DeckRevisionOverflow`·`RunRevisionOverflow`·`Disposed`·`InvalidItem`·`InvalidCharacter`·`IdentityOverflow`·`InvalidEnhancement`·`UnsupportedEnhancementLevel` | `IsUnexpected`. Controller가 예외를 내 공용 오류 경계로 보고하고 런타임을 멈춘다 |

- 골드가 부족해도 버튼을 끄지 않는다. 누르면 Core `InsufficientFunds` 결과로 "골드가 부족합니다."를 보인다.
- 구매에 성공했지만 완료가 수락되지 않으면 모든 구매 잠금을 유지하고 완료 버튼으로 같은 방문의 Complete만 다시 시도한다. 구매를 다시 제출하지 않는다(강화 Phase B).

### 4.2 화면 복구

- 상점 진입 수락은 Buffer에 남는다(지금 선택 노드 + `CurrentProgress.Shop`). `TryRestore`가 지금 맵·노드·StageKey·클리어 여부와 상점 상태를 확인해 화면을 되살린다.
- 씬 연결 뒤 맵을 표시하거나 진입 결과를 볼 때, 지금 선택 노드가 클리어 전이고 상점 상태가 있으면 새 진입 명령 없이 팝업을 연다. 완료·불일치 직후 같은 맵 갱신에서는 바로 다시 열리는 것을 한 번 막는다.
- 진입 Job 완료 뒤 UI를 열기 전에 씬을 떠나도 다음 연결 때 같은 규칙으로 복구된다. UI 열기 자체가 예외면 런타임을 멈춘다.
- `BackCloseUI`·`HideAllUI`가 상점을 숨기면 노드는 완료되지 않은 채 팝업만 닫힌다. 같은 씬 안에서는 맵을 눌러도 Core가 진입을 거절한다(`CurrentStageUncleared`). 다음 연결 때 복구된다. 실제로 생기면 그때 처리한다.

## 5 화면 규격과 DB

### 5.1 화면

지금 규격은 2026-10-19 이벤트 스테이지 기획서 PPT(6~7·9~10쪽)와 사용자 교정이다. 바뀐 것: 옛 `develop-4week` Shop.unity 규격(제목 48pt, 카드 360×430 세 장, 나가기 버튼 오른쪽 아래) → PPT 기준(10-19).

| 항목 | 값 |
|---|---|
| 패널 | 전체 화면 1920×1080, 불투명 배경 (0.08, 0.09, 0.12, 1). 뒤 맵·HUD를 가리고 클릭을 막는다(2026-10-11 사용자) |
| 카드 | `StageEventOfferDescriptionCard.prefab` 480×840, 간격 144(중심 x = −624·0·624). 카드 묶음 배율 0.8, 위치 (0, 5) |
| 카드 안 | 그림·이름·가격·설명·선택 버튼. 배치는 강화 설계 §4.2. 선택 버튼 "선택한다", 산 카드는 "구매 완료" |
| 보유 골드 | `GoldBackground` Image 480×116, 색 (0.25, 0.29, 0.36, 1), Raycast 끔. 중심 (−312, −447) |
| 완료 버튼 | "선택하지 않는다" 480×116, 중심 (312, −447). 별도 나가기 버튼은 없다 |
| 안내(Status) | 560×44, y = −361, 26pt, `StatusBackground` Image. 처음 숨김, 문구가 있을 때만 보인다 |
| 숨긴 것 | 제목(Title)은 지웠다(10-19 Cleanup) |

- 상태 문구로 보이는 것: 골드 부족, 완료 대기·재시도, 정상 거절 사유(강화 Q-E2 C안). 바뀐 것: 2026-10-11 "구매했습니다." 3초 숨김(DelayService) → 혼합 상점(10-19)에서 구매 성공이 바로 완료로 이어져 컨트롤러의 Delay가 빠졌다.
- 카드 내용은 열 때와 요청이 끝났을 때만 다시 만든다. 매 프레임 Buffer를 읽지 않는다.
- Canvas 정렬과 화면 부모(`StageScreenRoot`)는 [스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md) §10(Canvas와 UI 소유권)을 따른다.

### 5.2 DB

| 구분 | 지금 |
|---|---|
| InGame 상점 | `Stage/Shop/Shop_Default.asset`: 유닛 후보 ShotGun 하나, 아이템 4개, 고용가 600, 가중치 아이템 2 : 유닛 1(혼합 상점) |
| InGame 루트 | `Stage_Shop`(1201)이 `StageRootDataTable`에 등록돼 있다(S5.0). 일반 노드 추첨 후보라 출현은 보장되지 않는다 |
| View | 유닛은 `UnitVisual`의 `displayName`·`infoIcon`, 아이템은 소모 아이템 외형, 노드는 `NodeVisual_Shop_Regular` |
| 화면 고정 문구 | `ShopPopupText` 상수와 씬 TMP |
| 시작 Run 재화 | `PlayerLoadout_1.startingRunCurrency` |

- 바뀐 것(Q6): 2026-10-09 임시값 시작 20·Akimbo 2·ShotGun 3·Sniper 5 → 혼합 상점(천명님 `a5133b32`, 10-19)과 Akimbo 정리로 위 값. 시작 Run 재화는 지금 5000이다(`34ee0de4`, 10-20). 이 변경의 기록이 없어 시험용 값인지 확인이 필요하다.
- 상세 문구(가격·보유 수·레벨·수량)는 Buffer 값으로 만든다(P7 DB 규칙). "보유 N"은 `CharacterBuffer.Owned`에서 센다.
- 판매 후보를 바꾸면 InGame DB 변경이므로 `DataBaseAssetTests`를 함께 돌린다.

## 6 보류·결정 대기

| ID | 질문 | 지금 상태 |
|---|---|---|
| Q1 | Run seed가 없는 동안의 상점 seed 소유 | **결정 대기.** 임시 공급원은 구현돼 있다: `JobRegisterSystem`이 한 번 만든 `System.Random.Next`를 Stage Job에 넘기고, Core가 유효 입력 확인·잠금 뒤 한 번 부른다. Run seed는 [Run seed 도입 요청 R4](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md#3부-run-seed-도입-요청-view--core)로 적었고 미전달·미합의다 |
| — | 통합 기획서와 Core 상점 규칙 차이 G1~G5 | [기획서 차이 요청](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md#2부-통합-기획서와-상점회복스테이지-규칙-차이-view--core기획-확인-요청), 회신 대기 |
| — | 맵당 상점·이벤트 개수 | 맵 생성은 등록된 일반 스테이지에서 균등히 고른다. 기획이 개수를 정하면 맵 설정·생성 규칙 변경이 필요하고 Core 요청으로 적는다 |
| — | 아이템 판매 | 혼합 상점으로 들어왔다(천명님). 상점 데이터 구조 변경은 Core 범위다 |

## 7 성능 규칙(P14)

| 번호 | 규칙 |
|---|---|
| R1 | 바뀌었을 때만 다시 그린다. Controller는 열 때·요청 완료 때만 `Render`를 부른다. UI에 Buffer를 읽는 `Update`를 두지 않는다 |
| R2 | 맵은 갱신 요청이 있을 때만 다시 그린다 |
| R3 | Tick 경로에서 할당 0. 대기 요청이 없으면 필드 확인 뒤 바로 돌아간다 |
| R4 | 문자열은 값이 바뀔 때만 만든다(모델을 만들 때 한 번) |
| R5 | 카드와 모델 배열은 재사용한다 |
| R6 | 찾기·조회 준비는 연결 때 한 번 |
| R7 | Profiler로 확인한다. 2026-10-11 측정에서 Stage Flow/View·상점 구간 GC 0B를 확인했다. 게임 전체 프레임 GC 0을 뜻하지 않는다(계획 §3.1) |

## 8 다른 작업자 영향

| 작업자 | 고친 곳 | 기록·공유 |
|---|---|---|
| 천명님(InGame DB) | `Shop_Default.asset`·`PlayerLoadout_1.asset` 임시값(2026-10-09) | [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.14 |
| 천명님(`StagePlan.cs`) | 초기 정적 메서드 추가 → SA에서 번호 기반 Job 등록으로 교체 | §5.14, [SA Core 교체 인계](../handoffs/2026-10-11-stageselect-core-replacement-handoff-Hojin.md) |
| 현강님(UI) | `UIIDs.cs`에 `Shop = 14` 한 줄 | [상점 UI — 현강님 공유](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md) |
| 공용 런타임 조립 | `RunTimeSystemFactory.cs`에 옛 `ShopPopupInputLayers.Popup` 등록 한 줄(S5.2) | [Framework 공유 §9](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md). 지금은 스테이지 맵 공용 팝업 Layer를 쓴다 |

참고: 천명님 원 문서 상점 연결 설계(git history `2026-10-03-shop-stage-core-connection-design.md`)·[인계](../handoffs/2026-10-03-shop-stage-core-connection-handoff.md), [아티팩트 팝업 설계](2026-10-06-artifact-popup-design-Hojin.md)(같은 UI 구조).
