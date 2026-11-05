# 상점 UI — 현강님 공유

- 작성자: 호진(이호진)

현강님께 드리는 상점 화면 작업 안내(현재 계약)와 상점 팝업 UI 연결 공유(이전 단계)를 모았다. 원래 문서를 부(部)로 나눠 내용 그대로 담았고, 각 부의 결정 번호와 절 번호는 원래 문서와 같다.

| 부 | 원래 문서 |
|---|---|
| [1부](#1부-상점-화면shopui-작업-안내--u1-sa-p5p6) | 상점 화면(ShopUI) 작업 안내 — U1, SA P5/P6 |
| [2부](#2부-상점-팝업-ui-연결-공유-호진--현강님) | 상점 팝업 UI 연결 공유 (호진 → 현강님) |
| [3부](#3부-상점회복-공통-기반과-수신자-계약-2026-10-27-미전달) | 상점·회복 공통 기반과 수신자 계약 (2026-10-27, 4번 S-a·S-b) — **지금 계약은 3부가 원본**, 1부 §2~§4의 제출 관련 줄은 3부가 대신함 |

---

## 1부. 상점 화면(ShopUI) 작업 안내 — U1, SA P5/P6

- 작성: 2026-10-10 / 현재 계약 갱신: 2026-10-11
- 상태: 실제 ShopUI·카드·입력 잠금·Controller·운영 씬 연결은 병합 후에도 유지한다. 상점 S5의 DB 등록·자동 Play·전체 테스트 비교·무입력 Profiler는 [상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md)의 완료 이력이다. 병합 `9d4c76e5`와 연결 보완의 최신 실행 증거는 [스테이지 맵 계획 §4.2](../plans/2026-10-10-stage-map-plan-Hojin.md#42-병합-후-연결-보완)(옛 병합 검토 §7)를 따른다. 이번 문서 정리에서 코드·에셋·테스트·Unity 실행을 하지 않았다. 사람이 직접 확인한 미관 승인·아티팩트 잔여 Play/Profiler·외부 인수는 별도다. (→ 바뀜: 2026-10-19 이후 화면과 흐름이 바뀐 곳 — 화면 규격·버튼 문구([상점 계획 §3.3](../plans/2026-10-09-stage-shop-plan-Hojin.md#33-이벤트-팝업-ppt-ui-정리-2026-10-19)), 혼합 상점·방문당 한 개 구매·구매 뒤 자동 완료·"구매했습니다." 3초 숨김 제거([§3.4](../plans/2026-10-09-stage-shop-plan-Hojin.md#34-혼합-상점-대응-천명님-2026-10-19)). 아래 §5·§7과 2부 §10은 10-11 기준이다.)
- 처음 읽을 문서: [View 구조 설계 §9](../specs/2026-10-13-view-structure-design-Hojin.md#9-흐름-시퀀스)(옛 현재 시퀀스 UML과 코드 탐색). 이 문서는 화면 구현/Inspector 계약만 담당한다.
- 원본: [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)(옛 SA 설계 §0.4(병합 후 실행)·§2.5·§2.6(화면 계약)), [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획 P5·P6, 구현 이력).
- 함께 읽기: [상점·회복 UI 설계](../specs/2026-10-06-stage-shop-recovery-ui-design-Hojin.md) §4, [UI 공유 메모](#2부-상점-팝업-ui-연결-공유-호진--현강님).
- 이 자료는 미전달이다. 외부 담당자 검토·인수·합의 기록은 아니다.

### 0. 역할과 현재 전환 범위

ShopUI는 받은 값을 그리고 ShopView 입력 Buffer에 의도를 제출한다. 게임 규칙과 영구 결과는 Core, 결과 분류·문구·팝업 전환·완료 관찰은 View의 ShopPopupController가 담당한다. 입력 Layer의 소유·복귀는 StagePopupSession이 담당한다. **UI 버튼 → 입력 Buffer는 이 프로젝트의 결정**이며 기존 UI 문서 전체의 의무 규칙으로 확대하지 않는다.

이전 W1의 “P6 전에는 shopUI를 비우고 Submit을 연결하지 않는다”는 사전 전환 조건이었다. P5/P6에서 기존 Host/Screen·스케줄러 소비자를 교체했고 StageMapSceneRoot의 shopUI 및 ShopScreen은 구체 ShopUI 타입을 사용하며 운영 StageSelect 씬에 연결되어 있다. 새 화면에 옛 IShopPopupScreen/IShopPopupHost를 다시 구현하지 않는다. 최초 에셋 연결·검증은 SA 계획 P6와 상점 계획 S5의 이력으로 보존한다.

ShopViewBuffer 뒤의 실행은 `StageLifeCycle → StageFlow/ShopFlow → 공용 JobRegister/Runner → ShopPlan/ShopCommandJob → Core의 입력 직접 조회`다. 화면은 이 실행 경로와 별도 Runner를 소유하지 않는다. 전투의 독립 CombatEntryService·CombatPlan.Decide/Next/ExitCheck와 공용 오류 소유를 포함한 전체 연결은 시퀀스 UML을 따른다.

UIBase·UISystem·UIInputBinding은 변경하지 않았다. 별도 Contracts 폴더, UI Tick/Update, Core 서비스 직접 호출을 추가하지 않는다.

### 1. 현재 코드와 직렬화 필드

경로의 Screens/Stage/는 GlobalSystem/UISystem/ 아래다.

| 파일 | 책임 |
|---|---|
| ShopUI.cs | UIBase·IUIInputReceiver·IShopPopupDisplay. 표시, 동기 수명, 공통 제출 잠금 |
| StageOfferCardView.cs | 카드 문구·아이콘·완료 표시·모델 허용 값만 복사하는 재사용 부품. 입력은 연결된 수신자와 slot으로 전달 |
| IShopPopupDisplay.cs | UI 소유 표시·수명·제출 오류·허용/완료 계약. View Host·입력 명령 이벤트·pending 조회 없음 |
| ShopPopupModel.cs | GoldText·Cards·ChapterKey·NodeId. Cards는 ShopOfferCardEntry 목록이며 Builder가 내부 Reset으로 재사용 |
| StageOfferCardContent.cs | 아이콘·문구·버튼 허용·완료 여부의 공용 표시값. 게임 ID를 보관하지 않음 |
| ShopOfferCardEntry.cs | 실제 ShopOfferId와 공용 Content를 묶는 상점 항목. ShopUI는 Render 때 ID를 화면 소유 목록에 복사 |

카드 ID 분리는 [이벤트·회복 계획 §3.1 V1](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#31-v1-공용-카드의-상점-id-분리-2026-10-11)의 범위다. ShopUI는 입력 slot으로 복사해 둔 실제 OfferId를 찾으며, 재사용 모델을 나중에 다시 읽거나 slot을 상품 번호로 변환하지 않는다. 카드 프리팹의 직렬화 필드와 GUID는 유지한다.

ShopUI Inspector 필드:

| 이름 | 타입 | 연결 |
|---|---|---|
| panel | GameObject | 루트와 다른 자식 전체 화면 Panel |
| cardArea | RectTransform | Panel 내부의 카드 가운데 배치 영역 |
| cardPrefab | StageOfferCardView | 복제할 카드 템플릿/프리팹 |
| goldText / statusText | TMP_Text | 보유 골드 / 결과 문구 |
| exitButton | Button | Panel 내부 나가기 버튼, OnClick 0개 |
| cardSpacing | float | 카드 사이 간격. 기본 60, 카드 폭 360이면 중심 간격 420 |

StageOfferCardView Inspector 필드: Image icon, TMP_Text titleText·primaryText·secondaryText·buttonText, Button actionButton은 필수다. GameObject completedMarker는 별도 완료 표시가 필요한 카드만 연결하는 선택 참조이며, 운영 상점 카드에는 연결하지 않는다. 카드 루트는 RectTransform이다. Validate는 필수 참조와 Panel/카드 영역/나가기 부모 관계를 확인한다.

### 2. 공개 표시·수명 API

이름공간: ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage.

| API | 의미 |
|---|---|
| Render(ShopPopupModel model) | 전달된 골드·카드·chapter/node를 복사. 모델 참조를 보관하지 않음 |
| ShowStatus(string message) | 결과 문구 표시. null/빈 문자열은 지움 |
| SetCanSubmit(bool canSubmit) | View의 수명/진행 허용. true로 pending을 지우지 않음 |
| ~~CompleteSubmission(int requestNumber, bool canSubmit)~~ | (→ 3부가 대신함: 삭제. "제출 중"은 Controller 영수증이 가지며 완료 뒤 `SetCanSubmit`만 부름) 자기 pending과 같은 양수 번호만 완료. false면 완료 뒤에도 차단 |
| ~~ResetSubmissionState()~~ | (→ 3부가 대신함: 삭제) 허용 false·pending 없음. 처음 연결 또는 소유 입력 정리 성공 뒤 재연결/Detach 전용 |
| IsOpen | 현재 상태. 최초 연결 동기화에 사용 |
| event Action<bool> OpenStateChanged | 실제 Open/Hide 상태 변화마다 한 번 동기 알림 |
| ~~event Action<Exception> SubmissionFailed~~ | (→ 3부가 대신함: 삭제. 제출은 Controller가 하고 예외는 Controller가 오류 경계에 바로 보고) Submit 자체의 원래 예외를 동기 보고. 입력 명령·게임 결과 이벤트가 아님 |
| internal Validate() | 필수 참조/계층 오류를 기존 UI 방식으로 예외 보고 |

UIId.Shop은 14다. 외부 전환은 UISystem OpenUI/CloseUI/HideAllUI를 통한다. 루트는 켜 두고 자식 Panel만 켜고 끈다. 나가기 입력 자체는 화면을 닫지 않는다.

(→ 3부가 대신함: UI는 서비스를 읽지 않고 `BindInput`으로 받은 수신자만 갖는다. 아래 두 문단의 Buffer 쓰기·제출 팩터리는 이제 Controller 쪽이며 문맥은 `ShopVisitContext`다.) BindUIService는 resolver.GetService(BufferSystemKey.Buffer).ShopViewWriteSink만 보관한다. UI는 상태 Buffer·DB·View 서비스를 읽지 않는다. GetUIData는 별도 데이터 조회를 하지 않는다.

실제 제출은 문맥이 있는 팩터리만 사용한다:

- ShopViewCommandData.Purchase(int chapterKey, StageNodeId nodeId, ShopOfferId offerId)
- ShopViewCommandData.Complete(int chapterKey, StageNodeId nodeId)

문맥 없는 팩터리는 제거되었다. 호출자는 현재 상점의 챕터·노드와 해당 상품 키를 함께 제출한다.

### 3. 입력과 즉시 공통 잠금

1. 버튼은 UIInputBinding → IUIInputReceiver.Click으로 전달한다. 나가기는 Awake에서, 카드는 생성 때 한 번 연결한다. Button.onClick.AddListener·입력용 delegate·Inspector UnityEvent는 사용하지 않는다.
2. 연결됨·열림·View 허용·pending 없음·모델의 카드 허용을 모두 만족할 때만 입력을 받는다. UI는 가격/골드/구매 규칙을 계산하지 않는다.
3. (→ 3부가 대신함: 잠금은 Controller 수신자가 제출 **전에** `SetCanSubmit(false)`로 건다. 규칙 "제출 전에 함께 잠금"은 같다.) 첫 유효 입력은 **Submit 호출 전에 모든 구매 버튼과 나가기를 함께 잠근다.** 번호 반환 전 재진입·예외에도 잠금이 유지되도록 bool 잠금과 반환 번호를 따로 보관한다. Click 수신부도 같은 조건으로 차단한다.
4. (→ 3부가 대신함: UI에 pending이 없다. 제출 중은 Controller 영수증 하나다.) Render/Open/SetCanSubmit(true)/Hide는 pending을 지우지 않는다. 새 카드가 늘어나도 잠긴다. Submit 예외는 SubmissionFailed로 소유자에게 전달한다. 관찰자가 없으면 원래 예외를 다시 던지고 잠금을 유지한다.
5. Controller는 현재 연결 표식·정확한 번호·명령 종류·원래 chapter/node·상품 키·공용 fault를 확인한다. 성공·정상 거절은 현재 상점 표시 문맥까지 같아야 표시와 완료 허용을 적용한다. 반면 소유 요청과 정확히 일치하는 상태 불일치 거절은 Current가 비워지거나 다른 선택으로 바뀌어도 입력 소비→CompleteSubmission(N,false)→닫기→맵 다시 그리기를 수행한다. 번호가 다르면 잠금을 풀지 않는다.
6. 정상 Hide는 취소가 아니다. 숨김 중에도 같은 연결의 일치 결과는 반영하고 입력을 정리하되 화면을 열지 않는다. 완료/상태 불일치로 닫은 뒤 맵 재표시가 요청되면, 원래 열려 있었는지와 관계없이 바로 뒤 TryRestore 한 번을 억제한다. 정상 숨김 구매 이후의 명시적 복구는 가능하다.
7. Detach/fault는 먼저 허용과 수명 구독을 끈다. 소유 ShopView 입력 Clear가 성공한 뒤에만 UI 임시 제출 상태를 Reset한다. 입력 발급 번호와 Core 처리 기록은 초기화하지 않는다. 같은 fault 런타임의 재연결은 거부한다.

UIInputBinding의 좌클릭·키보드/패드 Submit·Hover 해제 규약을 유지한다. 화면은 Hover 해제도 기존 Binding에 전달한다.

### 4. 동기 수명과 오류 정리

OpenStateChanged는 버튼 이벤트가 아니다. Controller는 Attach마다 새 연결 표식으로 한 번 구독하고, 이미 열린 화면도 IsOpen을 읽어 같은 호출 안에서 Layer와 허용 상태를 맞춘다. 이전 연결에서 캡처된 알림은 같은 UI를 재연결해도 무시한다.

실제 Open/Hide 변화만 알리며 반복 Hide는 중복 알림이 없다. UI는 관찰자를 각각 호출하고 예외를 모아 전달한다. Hide는 finally에서 상태 문구와 Panel을 정리한다. Controller의 알림 처리 오류는 기존 공용 RuntimeFaultBoundary로 보고한다. 알림 처리 중 UISystem을 재귀로 다시 열거나 닫지 않는다.

(→ 3부가 대신함: `SubmissionFailed`는 지웠다. 제출을 Controller가 하므로 예외를 Controller가 오류 경계에 바로 보고한다.) EventSystem은 입력 수신부의 예외를 기록하고 잡으므로, Submit 실패를 그 호출 경계에만 던져서는 런타임을 멈추지 못한다. SubmissionFailed는 UI가 소유하는 좁은 오류 알림이고 Controller가 동일 연결 표식으로 구독·해제해 기존 RuntimeFaultBoundary.Report에 원래 예외를 전달한다. UI는 오류 관찰자를 모두 호출하며 관찰자도 실패하면 원래 원인을 첫 항목으로 둔 AggregateException을 던진다. 자동 재제출이나 별도 UI 오류 서비스를 만들지 않는다.

Controller는 Open 때 `StagePopupSession.Begin(ShopPopupInputLayers.Popup)`의 소유 표식을 보관하고 Hide/Detach 때 놓는다. 정상 숨김은 자기 Layer와 활성·정상 Stage 연결이 유지될 때만 StageSelection으로 돌아간다. 씬 이탈/Detach는 `End(token, false)`로 자기 Layer만 지우며 맵 Layer를 되살리지 않는다. 오래된 표식의 종료가 새 소유자나 다른 화면의 Layer를 덮지 않으며, 일부 정리에 실패해도 나머지 정리를 시도한다.

Session은 StageMapViewSystem이 공개 `IInputLayerService`와 연결 상태 조건을 넣어 조립하는 일반 C# 부품이다. 상점의 CommonMap은 유지하고, 입력 전환 도중의 동기 Canceled·fault 뒤에는 소유와 상태를 다시 확인한다. 상점 요청·결과·문구 Delay는 계속 Controller가 처리한다. `Begin(null)`의 게임 Layer 해제는 후속 회복 모달에서 사용할 수 있지만 다른 HUD의 EventSystem 입력 차단까지 제공하는 것은 아니다. 아티팩트 3D 세션과 카메라 구현도 이 부품으로 옮기지 않는다.

Begin은 목표 Layer가 동기적으로 확립되어야 성공한다. 다른 Layer 전환의 콜백 안에서 새 Begin을 호출하지 않는다. 아직 보류된 요청을 장기 소유로 추정하지 않고 즉시 실패·정리하며, 이미 관찰되는 다른 Layer는 유지한다. 기존 상점처럼 Stage Tick과 UISystem의 정상 Open/Hide 수명에서 호출한다.

현재 상점 표시 문맥은 chapter/node/StageKey와 불변 상품 스냅샷의 동일성으로 구분한다. CurrentShopData.WithPurchase가 구매 표시 컨테이너를 교체해도 같은 상품 객체를 유지한다는 구현 계약을 이용하며, 새 게임 규칙이나 별도 Run 번호를 만들지 않는다. 같은 노드 번호를 재사용한 새 상점에 이전 결과가 적용되지 않도록 한다.

### 5. Render와 화면 규격

- GoldText, Icon, Title, PrimaryText, SecondaryText, ButtonText, IsCompleted를 전달값 그대로 그린다.
- 카드 수가 늘 때만 생성한다. 줄어들면 숨기고 다음 Render에 재사용한다.
- null 아이콘은 비우고 끈다. 선택 완료 표시가 연결된 카드는 IsCompleted만 따른다. 운영 상점은 비활성 구매 버튼의 “구매 완료”로 상태를 표시하며 아래에 같은 문구를 붙이지 않는다.
- 카드 버튼은 IsButtonEnabled와 공통 수명/잠금을 함께 적용한다.
- Render 밖에서 모델을 읽거나 매 프레임 문자열·카드를 만들지 않는다.

기존 설계의 1920×1080 기준 화면 규격:

| 요소 | 규격 |
|---|---|
| 전체 Panel | 1920×1080, 상점 배경 (0.08, 0.09, 0.12, 1), 뒤 스테이지 선택 화면을 완전히 가리고 맵/아티팩트 클릭 차단 |
| 제목 | “캐릭터 상점”, 48pt, (0, 330), 700×80 |
| 보유 골드 | 23pt, 하단 기준 (0, 180), 260×48 |
| 결과 문구 | (0, −265), 900×60 |
| 카드 3장 | 360×430, 중심 x=−420/0/420, y=20 |
| 카드 내부 | 이름 32pt, 가격 28pt, 보유 수 25pt, 구매 버튼 240×70(y=−135) |
| 나가기 | “나가기”, 260×70, 오른쪽 아래 기준 (−200, 100) |

2026-10-11 사용자 요청으로 상점 배경만 불투명으로 바꿨다. 전체 화면 Panel의 Image로 뒤 맵/HUD를 가리며, 기존 Hide에서 Panel을 끄면 맵이 다시 보인다. 맵·카메라의 수명이나 Core 상태를 UI가 변경하지 않는다. 나머지 규격은 기존 설계 기준이며 이번 동작 검증이 사용자의 미관 검토를 대신하지 않는다.

### 6. 인수와 남은 범위

실제 UI·UISystem·EventSystem fixture로 입력 키 매핑, 첫 구매/나가기 공통 잠금, 번호 반환 전 재진입, Submit 예외, Render/Open/새 카드, 잘못된 번호, Hide/늦은 완료, 재사용·가운데 배치, 관찰자 예외를 검증한다. Controller는 실제 Core fixture로 구매·정상 거절·완료·상태 불일치·이전 연결/새 표시 문맥·공용 fault를 확인한다.

**P5/P6 당시 검증 이력:** 2026-10-11 p56-related-minor-red.xml에서 이 범위의 **ShopUI 15개와 실제 Core/UI Controller 18개, 총 33개가 통과했다.** 이전 중간 실행의 새 문맥 테스트 준비 오류(SetShopOffers 덮어쓰기 금지)는 Current.Clear→SelectMap→실제 Core 재진입으로 바로잡아 통과했고, 알림 오류 중 비재귀 전환·일부 Detach 실패·다른 Layer 보존도 포함했다. 후속 리뷰에서 실제 포인터/Submit 오류 경계와 Current 변경 후 상태 불일치 거절의 소비를 추가 검증했고 p56-review-red.xml에서 새 12개 사례의 기대 실패를 확인한 뒤 수정했다. 해당 단계의 GREEN 및 에셋/전체 회귀는 SA 계획 P6에 보존하며 병합 후 최신 실행은 상단의 병합 검토 §7을 따른다.

프리팹·StageSelect 연결의 UIId/부모/필수 참조/OnClick 0개 검증은 화면 변경 시 유지할 인수 항목이다. 상점 노드 등록·자동 Play(S5)와 아티팩트 실행 경로 전환(P7)은 완료 이력이 있고, 전체 실패·미관·Profiler 등의 잔여 범위는 각 계획과 병합 검토에 구분되어 있다. 이 문서는 외부 담당자에게 보내지 않았다.

### 7. 구매 표시 후속 변경 (2026-10-11 사용자 승인, 검증 완료)

상점 카드의 하단 Completed 문구를 프리팹과 StageShopUISetup 생성기에서 제거했다. 구매 여부의 원본은 계속 CurrentShopData이며, 모델의 ButtonText/IsButtonEnabled/IsCompleted와 구매 완료 버튼의 비활성화는 유지한다. 공용 카드 부품의 completedMarker는 선택 참조이므로 별도 완료 표시가 필요한 다른 카드의 동작은 유지할 수 있다. 프리팹 GUID와 UIInputBinding·Inspector OnClick 0개 계약은 바꾸지 않았다.

“구매했습니다.”는 ShopPopupController가 주입받은 공용 IDelayService의 런타임 시간으로 3초 뒤 비운다. (→ 바뀜: 10-19 천명님 혼합 상점 `a5133b32`에서 이 3초 숨김과 Controller의 Delay가 빠졌다.) UI Update/Coroutine이나 별도 시간 누적을 추가하지 않았다. 새 성공은 같은 문구여도 새 3초를 받으며, 정상 거절 문구는 기존처럼 남는다. 새 제출 관찰·새 결과·Open/Hide·Detach/fault는 이전 문구 예약을 무효화한다. UI Submit 뒤 Controller Tick보다 Delay가 먼저 실행돼도 입력 Buffer에 남은 요청을 확인해 새 요청의 문구를 지우지 않는다. 별도 대기 문구나 UI 입력 이벤트를 추가한 것은 아니다.

DelayService는 개별 예약 제거 API가 없으므로 연결 표식·문구 버전·열림·현재 표시 문맥 검사로 오래된 콜백을 막는다. 예약은 원래 만료 시각까지 큐에 남으며, 만료 콜백의 표시 예외는 기존 RuntimeFaultBoundary로 전달한다. 숨김 중 일치 결과는 기존대로 소비하고 pending을 정리하되 성공 알림은 새로 예약하지 않는다. 다시 열 때는 빈 상태 문구로 시작한다. 재화·구매 상태·명령 처리·잠금은 타이머가 변경하지 않는다.

관련 테스트는 실제 UI/Core/DelaySystem을 사용한 ShopPopupControllerTests의 PopupPurchaseStatus_*와 ShopUITests의 선택 marker, StageShopUIAssetTests의 운영 프리팹 중복 표시 검사다. ShopStagePlaySmokeTests에는 실제 Framework 시간 3초 경과 후 문구가 비고 구매 상태가 유지되는 검사와 캡처를 추가했다. 자동 테스트 실행 결과는 아래와 전용 실행 기록에 남긴다. 배치 실행의 캡처는 생략되며, 이 문서는 외부 미전달이다.

검증 완료: 관련 EditMode 918/918과 별도 실제 Boot 상점 Play 1/1이 통과했고 컴파일 오류는 0이다. 실제 3초 만료와 구매 상태 유지·부족 거절·현재 캡슐·다음 전투 진입을 확인했다. 상세 증거와 배치 실행의 시각 검증 제한은 [상점 계획 §3.1](../plans/2026-10-09-stage-shop-plan-Hojin.md#31-상점-스테이지-구현-s0s5-2026-10-0911)(옛 구현 계획의 실행 기록)을 따른다.

---

## 2부. 상점 팝업 UI 연결 공유 (호진 → 현강님)

- 작성일: 2026-10-10
- 작성: View 작업(스테이지 선택 담당, 호진)
- 갱신: 2026-10-11. **미전달**. 현재 상점 계약은 **§7의 UI 계약과 §9의 병합 후 실행 경계** 및 [화면 작업 안내](#1부-상점-화면shopui-작업-안내--u1-sa-p5p6)를 따른다. §1~§6은 이전 단계의 경과 기록이므로 그 안의 “미구현/유지/P6 전 비움”을 현재 작업 지시로 사용하지 않는다. §7·§8의 테스트 수치도 각 단계의 과거 이력이며 최신 실행 증거는 §9의 병합 검토를 따른다. 최초 에셋 연결·검증 이력은 [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획)에 보존한다.
- 관련: [상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md) S3·S4, [아티팩트 팝업 공유](2026-10-06-artifact-popup-ui-hyeongang-share-Hojin.md)(같은 방식)

### 1. 현강님 파일에서 바뀐 것 (P2 당시 기록)

| 파일 | 변경 | 담당 |
|---|---|---|
| `GlobalSystem/UISystem/UIIDs.cs` | 기존 S3의 `Shop = 14` 한 줄 유지 | 호진 |
| `Screens/Stage/ShopPopupModel.cs`, `StageOfferCardContent.cs` | View에서 이동, meta GUID 유지, public 표시 타입. 카드에 실제 상품 `OfferId` 추가, View using/cref 제거 | 호진(P2) |
| `Screens/Stage/ArtifactPoolItemView.cs` | View에서 UI 표시 부품으로 이동, meta GUID 유지, 기존 namespace의 MovedFrom 메타데이터 추가 | 호진(P2) |
| `Screens/Stage/ArtifactPoolItemContent.cs` | ID·인스턴스·로컬 모양·이름·설명만 담는 공개 표시 값 추가 | 호진(P2) |
| `Screens/Stage/ArtifactPopupUI.cs` | Update·Buffer·DB 조회 제거. View가 전달한 목록·상세 표시, 공개 열기 허용/칸 판정/IsOpen·동기 수명 알림 준비. 기존 Host 유지 | 호진(P2) |

경로의 `Screens/Stage/`는 `GlobalSystem/UISystem/` 아래이다. 연결된 View·Builder·옛 Screen·테스트 소비자도 새 값 타입/표시 인자에 맞췄다. UI 표시 타입은 서비스나 Core 계산을 호출하지 않는다.

`UIBase`·`UISystem`·`UIInputBinding` 등 기반 코드는 고치지 않았습니다.

### 2. UISystem을 쓰는 방식 (P2/P4 당시 기록)

| 사용 | 위치 |
|---|---|
| 상점 노드 진입이 Core에서 수락되면 `OpenUI(UIId.Shop)` | `StageMapViewSystem`이 만든 `ShopPopupController` |
| 나가기(노드 완료) 수락, 상태 불일치, StageSelect 씬 이탈 때 `CloseUI(UIId.Shop)` | 같은 곳 |

- 상점 화면 `ShopUI`(`UIBase`)는 아직 없습니다. 사용자가 화면 작업(상점 계획 S4V)으로 만들 예정이며, 현강님 화면 폴더(`Screens/Stage/`)에 추가됩니다.
- 상점 팝업은 **뒤로가기(ESC)로 닫히면 안 됩니다.** 닫는 것이 곧 노드 완료이기 때문입니다. 상점이 열려 있는 동안 입력 Layer는 `ShopPopup`(공용 맵만)이라 ESC 액션이 없습니다. `BackCloseUI`·`HideAllUI`가 상점을 숨기면 노드는 완료되지 않고, 다음 씬 연결 때 Buffer 기준으로 다시 열립니다.

### 3. SA 4차에 따른 후속 변경 (당시 미구현 기록, 현재는 §7)

- 원본: [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)(옛 SA 설계 §2.5), [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획), [화면 담당 안내](#1부-상점-화면shopui-작업-안내--u1-sa-p5p6).
- 실제 ShopUI·StageOfferCardView·카드/화면 프리팹은 S4V가 구현한다. P2는 §1의 표시 타입 이동과 API 목표 안내까지다. `Render`·`ShowStatus`·`SetCanSubmit`·`CompleteSubmission`·`ResetSubmissionState`·동기 수명 알림과 구매 전체+나가기 즉시 잠금은 실제 ShopUI 구현/검증이 남았다. 화면 규격은 유지한다.
- U1의 표시 분리는 기존 UI 사례를 참고했고, **UI 버튼 → 입력 Buffer 제출은 이번 프로젝트 결정**이다. 새 ShopUI는 옛 View Host/Screen을 구현하지 않는다. UI는 값 표시·의도 제출·임시 중복 입력 잠금, View는 결과·문구·전환/Layer를 담당한다.
- UIBase·UISystem·UIInputBinding은 변경하지 않는다. UI 등록·바인딩 후 UISystem으로 열고 닫고, 버튼은 OnClick 0개·UIInputBinding 직접 수신을 유지한다.
- 아티팩트 UI의 OpenStateChanged(bool)는 P3.10에서 Session의 운영 구독/해제로 전환했고 옛 아티팩트 Host/Screen을 제거했다(§6). P3.9 공용 오류 소유자는 유지했다. 상점은 S4V/P6에서 같은 계약을 구현·연결한다. 버튼 입력 이벤트가 아니며 UnitCardView.PanelOpening 선례를 따른다. IsOpen 다음 Tick 감시에 맡기지 않는다.
- 상점은 첫 Submit 즉시 구매 전체+나가기를 잠그고 해당 결과/현재 연결 확인 뒤 해제한다. Render/Open은 pending을 초기화하지 않는다.
- **P6 전 운영 StageMapSceneRoot.shopUI는 비워 둔다.** 현재 옛 Screen 캐스트가 남아 있어 U1 화면을 연결하면 예외가 난다. S4V는 표시/프리팹 준비만 하고 P6에서 접근자·소비자·Submit을 함께 바꾼 뒤 연결한다.
- 아티팩트는 P2 표시 준비→P3 Session 이전→P3.9 공용 fault 정리→P3.10 동기 수명/옛 계약 교체 순서다. P3.9는 사용자 승인으로 구현했으며 전투 작업자 합의가 완료됐다는 뜻은 아니다.
- 이 문서는 실제 변경과 미구현 후속 작업을 구분한 공유 자료다. 현강님에게 발송하거나 공용 오류 정리 소유권을 합의한 기록은 없다.

### 4. P3.1~P3.8 View 책임 분리 (당시 기록, P3.10 후속은 §6)

- UI 스크립트와 기존 Host/Screen 계약은 P2 상태를 유지한다. 새 `OpenStateChanged`의 운영 구독은 연결하지 않는다.
- `ArtifactViewSystem`은 조립·Framework Tick 전달만 맡는다. `ArtifactPopupSession`이 기존 Host를 구현하고 씬·열림·Layer·입력 슬롯·카메라 구독을 관리한다. 입력은 `ArtifactDragController`가 받고 기존 스케줄러를 사용한다.
- `ArtifactPointerMapper`가 UI 영역과 보드 대상 판정을 한 곳에서 수행한다. `ArtifactBoardRefresher`가 3D/UI 목록·상세 값을 전달하고 `ArtifactPopupStatus`가 문구 원본을 소유한다. 3D View에는 별도 `IArtifactDragPresenter`만 추가했다.
- Session의 자체 정리는 일부 자원 정리가 실패해도 나머지를 수행한다. 공용 오류 시 Tick 없이 전투·상점·아티팩트를 함께 정리하는 연결은 **P3.9 구현**이며, 당시 옛 계약 삭제/새 이벤트 구독은 **P3.10 미구현**이었다. 현재 구현은 §6을 따른다.

### 5. P3.9 공용 오류 연결과 UI 경계 (당시 기록, 아티팩트 후속은 §6)

View 그룹이 단일 경계 콜백을 소유하고 전투→아티팩트→맵/상점의 StopInput을 즉시 호출합니다. 각 View의 UI/카메라 구독·입력 슬롯·드래그·Layer 정리 책임은 유지합니다. 오류 뒤 기존 Host와 이전 입력 수신자가 새 요청을 제출하지 못하도록 막고 대기 입력 Buffer를 비웁니다. 한 화면 정리 실패가 다른 화면 정리를 막지 않으며 그룹 종료 finally에서 콜백을 해제합니다.

이번 단계의 UI 스크립트·Host/Screen·입력 기반은 변경하지 않았고 새 OpenStateChanged 구독도 없습니다. 실제 ShopUI가 없으므로 **U1 구매 버튼/EventSystem Submit의 오류 후 거부는 검증하지 않았습니다.** S4V 화면과 P6/P7 제출/소비 경로 연결 때 실제 버튼의 허용 차단·이전 수신자 거부·임시 상태 정리 검사를 다시 실행해야 합니다. 세부 소유권/미전달 기록은 [공용 오류 변경 기록](2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md)을 따릅니다.

### 6. P3.10 아티팩트 계약 교체 (2026-10-11, 사용자 승인, 미전달)

| 파일 | 실제 변경 |
|---|---|
| Screens/Stage/ArtifactPopupUI.cs | View using/XML cref/Host 필드·Connect/Disconnect 제거, UI 소유 표시 계약 구현. 실제 상태 변경만 동기 알림. 열기·닫기 모두 SetCanOpen의 interactable/Click/Submit 차단 적용 |
| Screens/Stage/IArtifactPopupDisplay.cs + meta | UI 쪽 테스트 대체 계약. 표시·칸 판정·IsOpen/동기 수명/허용 상태만 제공. View·Core·Buffer 서비스/입력 이벤트 없음 |
| ViewSystem/ArtifactPopup/ArtifactPopupSession.cs | Attach false→한 번 구독/현재 상태 동기화→허용. Detach/fault false→해제→자체 정리. 연결 표식으로 이전 알림 무시. 관찰자 오류를 기존 공용 경계에 보고 |
| 같은 View 폴더의 Mapper/Refresher/Status/ViewSystem/Presenter 설명 | UI 소유 표시 계약 소비로 갱신. 기존 스케줄러·입력·카메라 소유권 유지 |
| IArtifactPopupHost.cs/.meta, IArtifactPopupScreen.cs/.meta | 아티팩트 옛 계약 삭제. 상점의 옛 Host/Screen은 유지 |

실제 CloseUI·BackCloseUI·HideAllUI·이력 교체에서 동일 호출 안에 드래그·프리뷰·슬롯·Layer/3D를 정리합니다. 일반 숨김은 이미 제출된 편집을 보존하며 씬 이탈은 맵 Layer를 되살리지 않습니다. UI는 관찰자별 예외를 모으면서 모두 호출하고 Hide finally에서 Panel/문구를 지웁니다. Session 알림 실패는 공용 경계로 보고하고 UISystem 전환을 재귀 호출하지 않습니다. 오류/정지 즉시 두 버튼과 수명 구독을 차단하며 남은 안정된 3D 보드는 소유자 Detach/Shutdown에서 반환합니다.

IArtifactPopupDisplay 선택은 설계 §2.5가 허용한 UI 쪽 테스트 대체 예외입니다. 기존 작은 화면 대체로 표시 변환/3D 판정을 검사하면서 실제 UI·UISystem·EventSystem·Boot 검사도 유지합니다. UIBase·UISystem·UIInputBinding·Inspector OnClick 0개·UI ID/등록·서비스 바인딩은 변경하지 않았습니다.

당시 관련 회귀 127/127 통과, 전체 추가 실패 0개이며 기존 실패 34개/판정 불가 1개는 유지했습니다. 상세 증거는 SA 계획 P3.10 기록을 원본으로 봅니다. **이 단계 당시 P5·P6은 미착수였고, 실제 ShopUI·Buffer Submit·새 Flow/Core 운영 연결은 후속 작업이었습니다.** 이후 전환은 §7~§9를 따릅니다. 이 자료는 미전달이며 담당자 인수/합의를 뜻하지 않습니다.

### 7. P5/P6 상점 UI·Controller 전환 (2026-10-11, 사용자 승인, 미전달)

| 파일/경로 | 현재 변경 |
|---|---|
| Screens/Stage/ShopUI.cs + meta | 실제 UIBase·IUIInputReceiver 화면. 표시/동기 수명/공통 pending 잠금, ShopView Write Sink만 서비스 바인딩 |
| Screens/Stage/StageOfferCardView.cs + meta | 카드 문구·아이콘·완료·실제 OfferId 복사. 늘어날 때만 생성, 숨김/재사용, 가운데 배치 |
| Screens/Stage/IShopPopupDisplay.cs + meta | UI 소유의 좁은 표시·수명·제출 오류·허용/완료 계약. 입력 명령 이벤트·pending 조회·View Host 없음 |
| Screens/Stage/ShopPopupModel.cs | ChapterKey·NodeId 추가. Builder의 내부 Reset으로 채우고 UI는 값만 복사 |
| ViewSystem/StageMap/Shop/ShopPopupController.cs | 동기 UI 구독과 연결 표식, 입력/실제 Core 결과의 정확한 번호·문맥 대조, 결과 문구·완료·닫기·복구·소유 Layer 정리 |
| StageMapSceneRoot / StageMapViewSystem | 구체 ShopUI 접근자로 전환하고 새 Controller를 조립. 옛 Screen 캐스트를 사용하지 않음 |
| 옛 IShopPopupHost/IShopPopupScreen·ShopScheduler/ShopRequest/ShopJob·StageMapEntryFlow | P5/P6 소비자 전환에서 제거. 새 UI에 우회 구현하지 않음 |

버튼은 UIInputBinding→IUIInputReceiver이며 Inspector OnClick은 0개입니다. UI는 바인딩된 ShopViewWriteSink에 문맥이 포함된 Purchase(chapterKey, nodeId, offerId) 또는 Complete(chapterKey, nodeId)를 제출합니다. 클릭 슬롯을 상품 ID로 추측하거나 가격·골드 규칙을 다시 계산하지 않습니다.

첫 Submit **호출 전**에 모든 구매 버튼과 나가기를 함께 잠급니다. 반환 번호를 받기 전의 재진입과 예외도 별도 bool 잠금으로 막습니다. Render/Open/SetCanSubmit(true)/Hide는 잠금을 지우지 않고, 일치하는 양수 번호의 CompleteSubmission만 정상 완료합니다. 나가기/상태 불일치는 CompleteSubmission(N,false) 뒤 View가 닫습니다. UI에는 View·Core 실행 서비스나 상태 Buffer·DB 조회가 없습니다.

Controller는 Attach false→소유 이전 입력 정리/Reset→새 연결 표식으로 한 번 구독→현재 IsOpen 동기화→유효 상태 허용 순서입니다. Detach/fault에서는 먼저 허용·구독을 끄고, 소유 입력 Clear가 성공한 뒤 UI pending을 Reset합니다. 이전 연결의 알림/결과는 같은 UI를 재연결해도 무시하며 같은 fault 런타임에는 재연결하지 않습니다.

정상 Hide는 제출 취소가 아닙니다. 숨김 중 일치 구매 결과도 반영해 pending을 정리하며 화면은 열지 않습니다. 완료/상태 불일치로 닫은 뒤 맵을 다시 그릴 때는 원래 열려 있었는지와 관계없이 바로 뒤 TryRestore 한 번을 억제합니다. 평상시 숨겨진 구매 완료 화면은 이후 명시적으로 복구할 수 있습니다.

표시 문맥은 chapter/node/StageKey와 불변 상품 객체 동일성을 확인합니다. 구매 시 CurrentShopData 컨테이너가 바뀌어도 같은 상품 객체를 유지하는 기존 Buffer 계약을 사용하여, 동일 노드 번호로 만든 새 상점에 오래된 결과가 잠금을 풀지 않게 합니다. 별도 게임 규칙이나 Run 카운터를 만들지 않았습니다.

상태 불일치 거절은 별도로 처리합니다. 현재 연결과 소유 요청의 번호·종류·원래 chapter/node·상품 키가 일치하면 Current.Clear, 맵 재선택, 다른 노드 선택 뒤 도착한 거절도 소비하고 CompleteSubmission(N,false)→닫기→맵 다시 그리기를 수행합니다. 성공·정상 거절은 표시 문맥이 달라졌을 때 새 화면을 갱신하거나 잠금을 풀지 않습니다.

UI는 모든 수명 관찰자를 호출하고 예외를 모으며 Hide finally에서 Panel/문구를 정리합니다. Controller 알림 오류는 공용 경계에 보고하고 그 알림 안에서 UISystem 전환을 재귀 호출하지 않습니다. 정리 일부가 실패해도 입력·구독·자기 Layer 정리를 계속 시도하고, 다른 화면 Layer는 보존합니다.

Submit 자체의 예외는 UI 소유 SubmissionFailed(Exception) 알림으로 같은 RuntimeFaultBoundary에 도달합니다. EventSystem이 수신부 예외를 잡고 로그만 남기는 경로를 보완하며 입력 명령 이벤트는 아닙니다. Controller는 연결 표식을 넣어 구독·해제하고, 옛 연결에서 캡처한 오류 알림을 무시합니다. UI는 모든 오류 관찰자에게 원래 예외를 전달하며 관찰자 실패도 원인 뒤에 모아 다시 던집니다. 관찰자가 없으면 원래 예외와 잠금을 유지합니다.

ShopUI 필드는 panel, cardArea, cardPrefab, goldText, statusText, exitButton, cardSpacing입니다. 카드 필드는 icon, titleText, primaryText, secondaryText, buttonText, actionButton, completedMarker입니다. [화면 안내](#1부-상점-화면shopui-작업-안내--u1-sa-p5p6)에 타입/부모 관계/기존 1920×1080 규격을 정리했습니다.

p56-related-minor-red.xml에서 이 범위의 실제 ShopUI 15개와 Core/UI Controller 18개, **총 33개가 통과했습니다.** 이전 새 문맥 테스트 준비 오류는 Current.Clear→SelectMap→실제 Core 재진입으로 바로잡아 통과했습니다. 추가 알림 오류·비재귀 전환·부분 Detach 실패·다른 Layer 보존도 포함합니다. 후속 리뷰의 실제 포인터/Submit 오류 경계와 선택 변경 후 상태 불일치 거절 12개는 p56-review-red.xml에서 기대 실패를 확인한 뒤 수정했습니다. 후속 GREEN 및 에셋/전체 회귀의 최종 결과는 SA 계획의 최신 P6 기록을 확인합니다. 프리팹/씬 작업을 동작 코드 확인만으로 완료라 판단하지 않으며, 미관 검토·S5 상점 노드 Play는 별도 범위입니다. P7 아티팩트 실행 전환은 이후 완료했고 현재 H3/H4 후속 검증과도 구분합니다.

UIBase·UISystem·UIInputBinding은 변경하지 않았습니다. 이 자료는 **미전달**이며 현강님 또는 전투/Framework 담당자의 인수·합의를 뜻하지 않습니다.

### 8. H3/H4 Core 직접 조회와 화면 수명 (2026-10-11 병합 전 구현·검증 기록)

UI의 §7 표시·입력 잠금·동기 알림 계약은 유지합니다. 변경은 요청 내용을 읽는 실행 위치입니다. Shop UI → ShopViewBuffer → Flow/Plan/Job(기대 번호만) → `StageCommands.ProcessNextShopCommand(N)`에서 Core가 구매/완료를 직접 조회합니다. Job은 입력/결과 Sink나 요청 내용을 보관하지 않으며 Core의 정상 true/false 뒤 완료합니다. 직접 조회는 프로젝트 선택이고 UI/Framework 문서의 의무 규정이 아닙니다.

입력 없음·기대 번호 불일치·이미 처리됨은 Core 상태/번호 변경 없는 false입니다. Detach의 소유 입력 Clear 후 이전 Job이 새 연결의 입력을 실행하지 못하게 하며, ShopFlow도 사라진 기대 입력의 대기를 끝내 새 요청을 받습니다. 같은 입력이 남고 실제 기록이 없으면 해당 도메인만 정지합니다. 일반 Hide는 계속 제출 취소가 아니며 UI pending을 풀지 않습니다.

기존 동기 구매/완료·노드 진입 서비스가 실행을 시작한 뒤에는 수락/정상 거절 결과를 반드시 게시합니다. DB 콜백의 입력 Clear는 이미 설치한 거래의 롤백이나 commit 이후 취소가 아닙니다. 오래된 연결의 결과를 새 UI에 표시하지 않는 책임은 계속 Controller/View에 있습니다.

맵 요청 `StageViewCommandData`에는 필수 ChapterKey를 넣습니다. 맵 View는 실제 표시한 맵의 챕터로 제출하고 표시 전에는 입력을 보내지 않습니다. 다른 챕터의 결과는 해당 입력만 정리하고 현재 상점 화면을 열거나 문구를 쓰지 않습니다. 전투의 Active/씬 전환·취소 Job 배출 계약은 보존하되 병합 후 실행 소유자는 §9를 따릅니다.

API·번호 있는 전투 경계는 [P56 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md), 당시 검증 항목/결과는 [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획 P6.6~P6.10)을 따릅니다. §7의 `p56-*` 수치는 최초 구현의 과거 기록입니다. H3/H4 후속 자동 테스트는 `Logs/StageSelectSAVerification/stage-shop-pull-related.xml` 365/364 통과/실패 0/기존 판정 불가 1, `stage-shop-pull-all.xml` 3769/3734 통과/기존 실패 34/기존 판정 불가 1/skip 0이었습니다. 전체 실패 fullname은 `p7-pull-final-all.xml`과 같아 추가/해소 0이었으며 Unity exit 2를 전체 통과로 표현하지 않습니다. 당시 최종 빌드·독립 검토도 완료했습니다. 이 단계 당시 미관·수동 Play/Profiler·상점 S5·Run seed R4·P8/P9와 외부 전달/합의는 별도였으며 이후 상태는 §9를 따릅니다.

### 9. 병합 후 현재 실행 경계와 검증 출처 (2026-10-11, 미전달)

병합 `9d4c76e5`와 연결 보완 뒤에도 §7의 UI·입력 잠금·동기 수명 계약을 사용합니다. 운영 StageMapSceneRoot.shopUI는 구체 ShopUI로 연결되어 있으며 P6 전 비움·옛 Host/Screen·ShopRequest/ShopScheduler는 현재 연결 방식이 아닙니다. 상점 실행은 `ShopViewBuffer → StageFlow/ShopFlow → 공용 JobRegister/Runner → ShopPlan/ShopCommandJob → StageCommands.ProcessNextShopCommand(N)`이며 Core가 입력 내용을 직접 조회하고 결과를 게시합니다.

전투 진입은 독립 CombatEntryService와 Prepare/Transition Job이 맡습니다. CombatLifeCycle은 EntryService 제공자가 아니라 Enter의 동기 활성화와 `Next`/`ExitCheck` 등록을 맡고, 진행 판단은 CombatPlan.Decide가 소유합니다. Stage·Shop·Artifact·Combat의 Runner는 하나이며 RuntimeFaultBoundary도 Bootstrap·LifeCycle·View가 공유합니다. InGameViewFaultCleanup 하나가 StopInput 콜백의 연결·해제를 소유하고 각 View는 자기 입력·구독·표시를 정리합니다. 세부 수명은 [View 구조 설계 §9](../specs/2026-10-13-view-structure-design-Hojin.md#9-흐름-시퀀스)(옛 시퀀스 UML)과 [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)(옛 SA 설계 §0.4)를 따릅니다.

상점 S5의 DB·씬·자동 Play·전체 비교·Profiler는 [상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md)의 완료 이력이고 최신 병합 후 실행 증거는 [스테이지 맵 계획 §4.2](../plans/2026-10-10-stage-map-plan-Hojin.md#42-병합-후-연결-보완)(옛 병합 검토 §7)를 따릅니다. 이전 단계 수치를 이번 실행으로 옮기지 않습니다. 이번 문서 정리에서는 코드·에셋·테스트·Unity 실행을 하지 않았습니다. 기존 실패, 사람의 수동 입력·미관, 아티팩트 P8 잔여 Play/Profiler와 외부 전달·합의는 별도입니다. 상점 seed는 JobRegisterSystem의 임시 공급원을 사용하며 운영 Run seed R4는 미완료입니다.

### 10. 상점 구매 표시 후속 변경 (2026-10-11 사용자 승인, 미전달)

운영 StageOfferCard 프리팹의 하단 Completed 문구와 StageShopUISetup의 같은 생성 코드를 제거했다. 완료는 비활성 구매 버튼의 “구매 완료” 한 곳에 표시한다. 공용 StageOfferCardView.completedMarker는 선택 참조로 완화했으며, 모델의 IsCompleted·ButtonText·IsButtonEnabled 및 실제 구매·제출 잠금은 유지한다. 새 프리팹을 만들거나 GUID를 교체하지 않았다.

성공 결과 “구매했습니다.”의 수명은 View의 ShopPopupController가 소유한다. StageMapViewSystem에서 기존 Global.UtilityServiceCommand.delayService를 주입하고, 실제 표시한 성공만 공용 런타임 시간 3초 후 비운다. (→ 바뀜: 10-19 혼합 상점 `a5133b32`에서 빠졌다.) 새 성공은 새 3초, 정상 거절은 기존 유지 정책이다. 새 입력·결과·Open/Hide·Detach/fault 뒤의 예약은 연결 표식과 문구 버전으로 무효화하며, Controller Tick 전 제출도 Buffer의 남은 요청으로 보호한다. 늦은 콜백은 새 요청의 문구를 지우지 않는다. Delay 예약 자체는 취소 API가 없어 원래 시각까지 남는다.

UI에는 시간 진행·대기 문구·새 이벤트를 추가하지 않았다. 만료 콜백의 표시 오류도 기존 공용 오류 경계로 보고한다. 숨김 중 결과 소비·일치 번호 pending 해제·닫기/복구·Layer 정리는 기존 계약이다. 자세한 Inspector 계약과 테스트 범위는 [화면 작업 안내 §7](#7-구매-표시-후속-변경-2026-10-11-사용자-승인-검증-완료)을 따른다. 코드 반영과 Unity 검증 결과는 구분하며 이 자료는 미전달이다.

후속 Unity 검증: 관련 918건 및 별도 실제 Boot 상점 Play 1건이 모두 통과했다. 컴파일 오류 0. 실행 증거는 [상점 계획 §3.2](../plans/2026-10-09-stage-shop-plan-Hojin.md#32-상점-안내와-스테이지-위치-표시-2026-10-11)(옛 상점·노드 표시 구현 기록)에 있으며 이 문서는 여전히 외부 미전달이다.

---

## 3부. 상점·회복 공통 기반과 수신자 계약 (2026-10-27, 미전달)

- 작성: 호진 / 원본: [검사 후속 정리 설계 §7](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md#7-4번-제출-중-하나와-팝업-공통-기반), 구현 기록 [계획 §3.9·§3.10](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#39-s-a-공통-기반-4번). 커밋 `354bd8d7d`. Unity: S-a 사용자 확인, S-b 미확인.
- 한 줄 요약: **상점·회복 UI는 그리기와 "의도 전달"만 하고, 제출과 "제출 중" 사실은 Controller의 영수증 하나가 갖는다.** 두 UI의 같은 부분은 추상 기반 하나로 모았다. 1부 §2~§4의 제출 관련 줄(`CompleteSubmission`·`ResetSubmissionState`·`SubmissionFailed`·UI pending·UI의 Buffer 쓰기)은 이 부가 대신한다.

### 1. 공통 기반 `StageOfferPopupUI`

- 새 `Screens/Stage/StageOfferPopupUI.cs`: `public abstract class StageOfferPopupUI : UIBase, IUIInputReceiver`. `ShopUI`·`RecoveryUI`가 상속한다.
- 직렬화 칸 여덟(`panel`·`cardArea`·`cardPrefab`·`goldText`·`statusText`·`statusBackground`·`exitButton`·`cardSpacing`)을 이름·툴팁·기본값 그대로 기반으로 옮겼다. Unity는 상속한 칸을 이름으로 저장하므로 **프리팹·씬 값은 그대로이고 다시 연결할 것이 없다.**
- 기반이 갖는 것: `Open`·`Hide`(자식 Panel 켜고 끄기, 닫힘 알림 안 재열림 보호), 열림 상태의 원본 하나(`IsOpen`·`OpenStateChanged`), `ShowStatus`, `SetCanSubmit`, `Validate`, 카드 그리기(`RenderCards`), 카드 연결·잠금 다시 계산·`Click`.
- 하위가 답하는 것: `HasRendered`·`CanExit`·`CanAcceptInput`·`CardContentAt`·`RequestExit`·`RequestCard`. `ShopUI` 253 → 49줄, `RecoveryUI` 272 → 51줄.
- 현강님 줄을 옮긴 곳: `ShopUI`의 카드 `BindOwner`·`Open`·`Hide`·null 검사 줄 → 기반 `RenderCards`(같은 동작).
- 바뀐 동작(설계 §7.6): 닫힘 알림 안에서 다시 열리지 않게 보호(D1), 나가기 문구가 빈 값이면 프리팹 문구(전에는 직전 문구 유지, D4), 상점 예외 문구 한국어(D7), 회복도 그리기 전에는 입력을 받지 않음(D6, 닿는 경로 없음).

### 2. 표시 계약과 수신자 계약

| 계약 | 멤버 |
|---|---|
| `IShopPopupDisplay`·`IRecoveryPopupDisplay` | `IsOpen`·`OpenStateChanged`·`BindInput(수신자)`·`Render(model)`·`ShowStatus`·`SetCanSubmit` (여섯, 같은 꼴) |
| 지운 멤버 | `SubmissionFailed`·`CompleteSubmission`·`ResetSubmissionState`, 회복은 `PendingNumber`·`PendingCommand`까지 |
| `IShopPopupInput`(새) | `Purchase(ShopVisitContext, ShopOfferId)`·`Complete(ShopVisitContext)` |
| `IRecoveryPopupInput`(새) | `Select(RecoveryEntryContext, RecoveryOptionId)`·`Complete(RecoveryEntryContext)` |

- UI는 `BindInput`으로 받은 수신자 하나만 갖고, 버튼이 눌리면 **자기가 그린 문맥·ID로 수신자를 부르기만** 한다. 번호·명령·세대 값·서비스 참조·`BindUIService` 읽기가 없다. 수신자가 없을 때 입력은 무시한다(바인딩 전 대기, AGENTS 규약).
- Controller가 수신자 계약을 구현하는 유일한 제출자다: 허용·문맥 확인 → **제출 전에** `SetCanSubmit(false)`·상태 문구 비우기(1부 §3-3 "Submit 전에 함께 잠금" 규칙 그대로) → 제출 → 영수증. 결과를 처리하면 영수증을 지우고 `SetCanSubmit(…)`. 제출 예외는 Controller가 오류 경계에 바로 보고한다.
- 같은 폴더 선례 `DeckEditUI`·`IDeckEditInput`과 같은 꼴(명시적 수신자)이다. UISystem·`UIBase`·`UIInputBinding`은 바꾸지 않았다.

### 3. 판단 부탁 — 루트를 켜 둔 채 Panel만 켜고 끄는 꼴(U4)

- UI 규칙(`UI-lifecycle.md`)은 "활성 상태 변경과 정리는 `UIBase.Open/Hide`와 `OnUIOpened/OnUIHidden`에서"라고 한다.
- 상점·회복(덱 편집 `DeckEditUI`·아티팩트 `ArtifactPopupUI`도 같음)은 루트를 켜 둔 채 자식 Panel만 켜고 끄며 `base.Open/Hide`를 부르지 않는다. 열림 상태를 자기 값으로 들고 있어서 부모 Canvas가 꺼지면 "열림"으로 남는다.
- 이번에는 꼴을 유지하고 열림 상태만 기반 한 곳으로 모았다. 이 꼴이 UI 규칙상 허용인지, 고칠 것인지 정해 주세요(고치면 네 화면이 같이 바뀜).

### 4. 재생 확인(남음)

- 상점: 구매 성공·거절·나가기, **빠른 연타·키보드 Submit으로 두 번 제출되지 않음**.
- 회복: 선택·완료, **제출 중 닫았다 다시 열기**.
