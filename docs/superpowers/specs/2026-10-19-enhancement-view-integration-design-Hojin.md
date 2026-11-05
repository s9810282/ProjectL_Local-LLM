# 1레벨 강화 UI와 View 연결 설계

- 작성자: 호진(이호진)
- 상태: Phase A~D·E0와 기존 상점 외형 3종 등록 완료(2026-10-20). 실제 Play로 2연발·도어 브리칭·독 가시 각각의 구매 → 다음 전투 반영을 확인했다(10-20). 보류: H(마지막 구매 읽기, Core 계약 대기), 전용 노드 자산, E1~E4 전체 인수. 레벨업 특성 성장(증강) 선택 화면(옛 §14)은 2026-10-29 [증강 설계](2026-10-28-trait-growth-design-Hojin.md)로 옮겼다. §15 메인 강화 상점은 보류다(11-02 사용자: 1202는 훈련소가 쓰고 메인 캐릭터 강화는 일단 무시). 지금 작업은 §16 훈련소 내부 연결이다(11-02 설계 초안, Q-TV1~Q-TV3 결정 대기).
- 범위: StageSelect의 기존 상점 팝업에서 보유 캐릭터의 1레벨 강화를 고르거나 사지 않고 끝내는 화면·연결. 강화 효과·후보 샘플링·가격 규칙과 반복 레벨 강화는 다루지 않는다(Core 범위).
- 계획: [강화 View 통합 계획](../plans/2026-10-19-enhancement-view-integration-plan-Hojin.md)
- 정리: 2026-10-27 착수 전 상태 표, 코드 대조 정정(§12.4~12.5), Phase별 구현 결과(§13~§20)를 지금 구조·결정 중심으로 다시 썼다. 구현 결과는 계획 §3으로 옮겼다. §4.4 절 제목은 그대로다. 옛 판은 git `10914570`.

## 1 목적과 범위

기존 상점 화면과 Core의 강화 구매 기능을 이어, StageSelect에서 보유 캐릭터의 1레벨 강화를 고르거나 구매 없이 이벤트를 마치게 한다. UI 작업자가 만든 상점·회복의 시각 구성을 쓰고, 정식 UI 등록·팝업 수명·번호 있는 입력 Buffer·기존 Job·결과 확인 경계를 지킨다. 임시 호출이나 가짜 완료로 잇지 않는다.

| 기준 | 역할 |
|---|---|
| Core 강화 설계(천명님, git history `2026-10-19-enhancement-shop-trait-design.md`) | 강화 소유, 1레벨 범위, 개별 효과의 원본 |
| [Core와 View 인계](../handoffs/2026-10-19-enhancement-shop-core-view-handoff.md) | 공개 입력·결과 계약, 레시피 등록, 인수 조건 |
| [상점 스테이지 UI 설계](2026-10-06-stage-shop-recovery-ui-design-Hojin.md) | 상점 팝업 구조·결과 분류·화면 규격 |
| [기능 변경 독립성 기준 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리) | 조립과 기능의 분리 |
| [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md) | 서비스·입력·UI·생성·해제 계약 |

기획 출처는 `C:/Users/Hyeon2-400/Dropbox/Project L/기획 문서/이벤트 스테이지 기획서.pptx`(2026-10-13, Ver0.1) 3쪽(이벤트 공통)과 12~16쪽(강화)이다. 16쪽의 이름 없는 카우보이 항목은 Core가 뺐으므로 대상은 8종이다. 기획서 대조는 §11.1이다.

Core 강화 설계·인계는 천명님 문서라 바뀔 수 있다. 문서와 코드가 다르면 코드의 공개 계약을 확인하고 차이를 보고한다. 제공되지 않은 사실을 View에서 만들어 대신하지 않는다.

## 2 지금 구조

### 2.1 방식

기존 상점 화면을 넓혀 강화 방문도 표시한다. Core가 강화를 상점의 방문·구매·완료 계약으로 주므로(`ShopVisitKind.Enhancement`, Shop/Regular 스테이지) 화면 수명과 요청 처리는 상점 소유자에게 두고 강화 표현만 더했다. 독립 강화 화면(별도 UIId·표시·입력·복구 수명)과 범용 이벤트 프레임워크는 택하지 않았다.

### 2.2 책임

| 책임 | 소유자 | 하는 일 |
|---|---|---|
| 후보·가격·선택 가능 여부·강화 효과·골드 반영 | Core와 Buffer | 공개 계약으로 소비한다. View에 규칙을 복제하지 않는다 |
| 방문 표시 모델과 상품 종류 선택 | `ShopPopupModelBuilder` | 방문·골드·순서·구매 잠금·모델 재사용. Kind로 내용 Builder를 고르고, 누락·중복·미지원 종류는 거절한다 |
| 상품 카드 내용 | `IShopOfferContentBuilder` 구현 셋 | `UnitShopOfferContentBuilder`(보유 목록 읽기), `ItemShopOfferContentBuilder`(아이템 외형), `EnhancementShopOfferContentBuilder`(대상 이름·얼굴 조회) |
| 표시 객체 생성·연결 | `ShopPopupViewFactory` | 필요한 Sink와 View 조회 함수만 주입한다. 표시·거래 계산은 없다 |
| 요청·결과 확인·복구·닫기 | `ShopPopupController` | 요청 번호와 방문 식별자를 맞추고 완료·이탈 계약을 지킨다 |
| 카드 생성·표시·입력 제출 | `ShopUI`·`StageOfferCardView` | 값과 OfferId를 보관하고 기존 수신자 경로로 제출한다 |
| UI 열기·닫기와 입력 Layer | `PopupViewScope`·`PopupInputSession`·StageMap 호스트 | 자기 연결만 열고 닫는다 |
| 지도 노드 외형 선택 | View DB `StageNodeVisualTable` | StageKey 지정 행 우선, 없으면 (StageType, StageMapRole) 공통 행 |

- 내부 계약: `IShopOfferContentBuilder`는 `ShopProductKind Kind`와 `StageOfferCardContent Build(ShopPurchaseSnapshot offer, bool purchased, bool hasPurchase)`를 준다. `ShopPopupModelBuilder(ICurrentProgressBufferReadSink current, ICurrencyBufferReadSink currency, IReadOnlyList<IShopOfferContentBuilder> contentBuilders)`. 내부 Builder는 Framework 서비스로 등록하지 않는다.
- `ShopVisitKind`는 Builder가 Buffer에서 읽어 강화 내용과 빈 후보 처리를 고르는 데 쓴다. ShopUI에 방문 상태를 따로 더하지 않는다. 첫 카드로 방문 종류를 추측하지 않는다.
- 공통 설명 카드 `StageEventOfferDescriptionCard.prefab`을 상점·회복·강화가 함께 쓴다(§4.2).
- 소스 루트 `Assets/ProjectL/Script/Systems/RuntimeSystem/`: 표시 코드 `InGameSystem/ViewSystem/StageMap/Shop/`, 화면·카드 `GlobalSystem/UISystem/Screens/Stage/`, 입력·결과 `BufferSystems/`.

### 2.3 운영 데이터 지금 상태(2026-10-27 확인)

| 항목 | 지금 |
|---|---|
| 강화 스테이지 | `Stage/Root/Stage_Enhancement.asset`(StageKey 1202, mapKey 1, Regular, 상세 → Shop)과 `Stage/Enhancement/Enhancement_Default.asset`(2연발·도어 브리칭·독 가시 3종). 루트는 `StageRootDataTable`에 등록돼 있다 → **10-29 등록을 뺐다(§14.5)** |
| 레시피 표 | `Enhancement/EnhancementDataTable.asset` 4행: DoubleShot·DoorBreaching·PoisonThorn·ChainExplosion. 모두 레벨 1·가격 700·가중치 1 |
| 지도 외형 | `NodeVisual_Shop_Enhancement`(StageKey 1202) 행이 등록돼 있다. 건물·아이콘 프리팹은 일반 상점과 같고 표시 이름만 "Enhancement"다 |
| 맵 등장 | 노드마다 맵의 일반 스테이지 키 하나를 균등 무작위로 고른다(`ChapterGraphGenerator`). 가중치·최소/최대·연속 조절은 없다(Q-E4 보류) |

바뀐 것: 2026-10-20 D에서 "연쇄 폭발은 SO만, 표 등록 보류" → 같은 날 `34ee0de4`에서 표에 추가(대상 DynamiteHammer는 여전히 Unit 표 미등록, 확인 필요). 정밀 사격(Ballista)은 천명님 `d586bbf1`(10-27)에서 SO·표 행이 지워졌다. 강화 노드 전용 View 행은 `34ee0de4`(10-20)에서 만들었다(전용 아트는 아직 없음).

## 3 결정 기록

| ID | 결정 | 날짜·근거 |
|---|---|---|
| 범위 | 1레벨 강화 선택·구매만. 다음 방문의 레벨 상승은 넣지 않는다 | 2026-10-19 사용자 |
| 방식 | 기존 상점 화면을 넓힌다(§2.1). 새 UIId·Job·Flow·범용 이벤트 서비스는 만들지 않는다 | 2026-10-19 |
| Q-E1 | **C: 상점·회복·강화 세 이벤트 공통으로 카드에 추가 정보·설명을 표시한다.** 강화는 대상 캐릭터 이름·효과 설명을 포함하고, 다른 이벤트는 각 모델의 정보를 쓴다 | 2026-10-19 사용자 |
| Q-E2 | **C: 실패·완료 대기 안내를 세 이벤트 공통으로 표시한다.** 카드 설명과 결과 안내는 다른 표시 책임이다 | 2026-10-19 사용자 |
| Q-E3 | **지도에서 강화 노드를 일반 상점과 구분한다.** 기존 StageKey와 View 외형 등록·조회로 잇고 Core 종류·진입 규칙은 그대로 둔다. 전용 자산은 인수 뒤 연결한다 | 2026-10-19 사용자, 자산 대기는 10-20 사용자 |
| Q-E4 | **이벤트 등장 가중치·최소/최대·연속 등장의 Core 요청은 보류한다.** 기존 맵 생성 규칙을 유지한다 | 2026-10-19 사용자 |
| 후보 소진 | 실제 후보가 없으면 현재 Run의 마지막 구매 강화를 구매완료 상태로 표시한다(§4.4). 새 구매·레벨 강화가 아니다 | 2026-10-19 사용자 |
| 실패 분류 | `TargetNotOwned`·`AlreadyEnhanced`는 상태 불일치, `InvalidEnhancement`·`UnsupportedEnhancementLevel`은 `IsUnexpected`(오류 경계) | 2026-10-19 설계, Phase B 구현 |
| StageKey 외형 | `StageNodeVisualData.StageKey` 0 = 공통 행, 양수 = 지정 스테이지. 지정 우선·공통 대체. 지정 키의 종류/역할 불일치·중복·음수는 거절 | Phase C |
| E 분리 | E를 준비(E0)와 운영 등록·최종 인수(E1~E4)로 나눈다. 기존 상점 외형으로 1202 등록까지는 H·전용 자산과 따로 진행한다 | 2026-10-20 사용자 승인 |
| 1202 | 챕터 1·mapKey 1·StageKey 1202에 2연발·도어 브리칭·독 가시를 넣는다 | 2026-10-20 사용자 승인 |

## 4 화면 설계

### 4.1 기본 구성

- 기존 상점의 배경·팝업·카드 배치·보유 골드·완료 버튼을 쓴다. 새 씬이나 입장 전 화면을 만들지 않는다.
- 선택 버튼은 **선택한다**, 완료 버튼은 **선택하지 않는다**다. 별도 나가기 버튼은 없다. 크기·정렬 원본은 [상점 설계 §5.1](2026-10-06-stage-shop-recovery-ui-design-Hojin.md#51-화면)이다.
- 구매 가능한 카드 수는 Core가 게시한 실제 후보 수(0~3)다. 없는 자리에 가짜 후보나 입력 가능한 빈 슬롯을 만들지 않는다.
- 실제 후보가 0개면 §4.4를 따른다. 어느 경우에도 구매 없이 지금 방문을 완료할 수 있다.
- 예: 시작 덱(메인·샷건·선인장 캐논·스나이퍼)에서 첫 방문 후보는 독 가시·2연발·도어 브리칭 셋이다. 하나를 사면 다음 방문은 1~2개, 남은 대상도 사면 0개다. 미선택·구매 거절만으로는 후보가 줄지 않는다.

### 4.2 강화 카드

| 표시 항목 | 원본과 표시 |
|---|---|
| 캐릭터 얼굴 | Offer의 `CharacterKey`로 View 유닛 외형 아이콘 |
| 강화명 | Offer의 `EnhancementName`(InGame 강화 SO의 문자열이 Core를 거쳐 그대로 옴) |
| 대상 캐릭터 이름 | View의 캐릭터 표시 이름(Q-E1) |
| 효과 설명 | Offer의 `EnhancementDescription`. View가 강화 키로 수치를 다시 만들지 않는다 |
| 가격 | `Offer.Cost`. 기획값 700을 UI 상수로 두지 않는다 |
| 선택 | 해당 `OfferId`와 캡처한 `ShopVisitContext`를 제출한다. 화면 순번을 보유 개체 ID로 쓰지 않는다 |

- 강화 레벨은 구매 결과의 강화 레벨 1을 따른다. 유닛 고용용 `InitialLevel`이나 캐릭터 일반 Level을 강화 레벨로 표시하지 않는다.
- 같은 종류 보유 개체가 있어도 Offer의 `TargetOwnedId`를 지킨다. 지금은 상점이 이미 보유한 캐릭터 고용을 거절하므로(`AlreadyOwned`) 같은 종류 개체가 둘 생기지 않는다. 그래서 Core의 개체 단위 판단은 기획서 12쪽 "같은 캐릭터의 다른 강화는 등장하지 않는다"와 같은 결과를 낸다.
- 공통 설명 카드: `StageEventOfferDescriptionCard.prefab`은 원본 `StageEventOfferCard.prefab`(480×720, 비활성 Details 유지)의 Variant다. 루트 480×840에서 상속 Details와 `secondaryText` 연결을 쓴다. 강화 전용 프리팹으로 나누지 않는다.
- 카드 안 배치: ImageArea 348×300 @ (0,175), ItemInfo 180×64 @ (0,−160)(이름·가격 각 180×28 @ (0,16)·(0,−16)), 설명 348×112 @ (0,−50), 선택 버튼 348×84 @ (0,−275). 설명·가격 자동 크기 18~24, 이름 18~28, 글자색 (0.13, 0.08, 0.04, 1). 선택 버튼에는 Raycast를 받는 Graphic이 있어야 하며 기존 입력 바인딩으로만 제출한다.
- ShopUI·RecoveryUI가 같은 카드를 참조한다. 렌더할 때마다 설명·완료 표시·입력 허용을 지금 모델 값으로 모두 갱신해 이전 방문 값이 남지 않게 한다. 이름 검색이나 자식 인덱스로 설명 영역을 찾지 않는다.
- 상점은 아이템 설명·수량이나 유닛 레벨·보유 정보를, 회복은 회복 모델의 설명·예상 회복량·선택 불가 사유를 쓴다. 없는 효과·수치를 만들어 채우지 않는다.

### 4.3 안내와 입력

- 입력은 `UIInputBinding` → `IUIInputReceiver` → ShopView 명령 제출이다. `Button.onClick` 거래 경로를 만들지 않는다.
- 제출이 시작되면 대기 잠금으로 카드와 완료 버튼의 중복 입력을 막고, 결과 확인·소유 정리 경계에서 푼다.
- 골드 부족은 Core 거절 결과를 받은 뒤 안내하고 같은 후보를 유지한다. 표시 계층이 구매 가능 여부를 다시 계산하지 않는다.
- Q-E2 C안: 실패·완료 대기 안내는 기존 `ShowStatus`(문구·텍스트·배경 가시성 함께 갱신)로 세 이벤트 모두 실제로 보인다. 로그에만 남기고 안내로 치지 않는다.
- 팝업 뒤 지도 조작 차단과 설정창·숨김·복귀는 기존 팝업 입력 수명을 따른다. 강화 전용 ESC·타이머·자동 선택은 없다.

### 4.4 후보가 없을 때 마지막 구매 표시

- **사용자 확정:** 새 강화 방문에서 실제 후보가 0개면 현재 Run에서 마지막으로 구매에 성공한 강화 한 개를 **구매완료** 상태로 표시한다. 후보가 1개 이상이면 실제 후보만 보이고 이력 카드로 빈자리를 채우지 않는다.
- 이력 카드는 캐릭터 얼굴·강화명·대상 이름·효과 설명을 보이고 구매완료·비활성이다. 제출할 OfferId가 없고 구매 명령을 만들지 않는다. 표시 종류를 구매 후보와 명시적으로 나눈다.
- 실제 Offers는 0개 그대로다. 이력 표시를 위해 Core 후보를 더하거나 지금 방문의 `HasPurchase`·`PurchasedMask`를 바꾸지 않는다.
- 마지막 성공 구매는 현재 Run 범위다. 일반 상점 구매·미선택·거절·실패가 덮어쓰지 않는다. 새 Run이 정상 설치되면 이전 Run 기록을 쓰지 않는다. 현재 Run에 구매 이력이 없으면 빈 상태 안내와 완료 버튼을 보인다(새 문구·연결·수명 검증 필요).
- 완료 버튼으로 구매 없이 노드를 완료한다. 이력 표시는 자동 완료·반복 강화·추가 과금으로 이어지지 않는다.
- **Core 계약 인수 필요·보류:** 지금 Owned 기록에는 구매 순서가 없고, 최근 상점 명령 결과 한 건은 다른 구매·Complete로 덮이고 새 Run에서도 남는다. 그래서 마지막 성공 구매의 안정된 저장소가 아니다. 필요한 읽기 사실과 수명은 [Core 전달 문서 §26](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#26-강화-후보-소진-시-마지막-구매-표시용-읽기-계약-요청-2026-10-19-view--core-미전달)에 요청으로 적었다(미전달). Core DTO·Sink 형식을 미리 만들지 않고, View 구매 장부나 InGame DB 직접 조회로 대신하지 않는다. 인수 전에는 Offers 0개를 "구매 이력 없음"으로 바꿔 읽지 않는다. 지금 코드는 빈 강화 방문에서 카드 없이 완료 버튼만 보인다.

## 5 진입과 구매 흐름

1. StageMap 노드 선택 소유자가 번호 있는 입력을 제출한다. 강화 노드는 등록된 Shop/Regular 정의를 쓴다.
2. Core 진입 결과와 `CurrentShopData`를 확인한 뒤 지금 방문을 표시한다. `ShopVisitKind.Enhancement`로 강화 표현을 고른다.
3. 표시한 `ChapterKey / NodeId / StageKey / EntryId`를 `ShopVisitContext`로 잡는다. 후보 생성·재추첨은 Core 진입 책임이다.
4. 선택은 `ShopViewCommandData.Purchase(context, offerId)`, 미선택은 `.Complete(context)`를 기존 WriteSink에 제출하고, 돌려받은 요청 번호와 방문 문맥을 추적한다.
5. `ShopPlan`·`ShopCommandJob`이 `IStageCommandProcessingService.ProcessNextShopCommand(expectedRequestNumber)`를 부른다. View는 `IShopService`를 직접 부르지 않는다. 구매 요청은 Core가 영수증을 먼저 남기고 성공하면 같은 요청 안에서 완료까지 한다(`ShopCommandExecutor`). 기획서 12쪽 "선택하면 그대로 UI 종료"를 이 경로로 맞춘다.
6. 결과의 `RequestNumber`·`Context`·명령 종류·구매 `OfferId`를 모두 맞춘다. Job 완료나 처리 번호만으로 성공을 선언하지 않는다.
7. 구매 성공과 완료 성공을 각각 확인한 뒤 팝업 소유자가 UISystem으로 닫고 지도 갱신을 요청한다. UI가 닫힌 것 자체가 노드 완료가 아니다.

## 6 결과별 처리와 수명

| 결과·상황 | 표시와 입력 | 상태 원본 |
|---|---|---|
| 구매 성공 + 완료 수락 | 닫기, 지도 갱신·입력 복귀 | 구매 결과와 `CompletionResult` |
| 골드 부족 | 안내, 같은 후보 유지, 다시 입력 가능 | `ShopFailureReason.InsufficientFunds` |
| 구매 성공 + 완료 실패·대기 | 모든 구매 잠금 유지. 완료 버튼으로 같은 방문의 Complete만 재시도. 안내 표시 | `CurrentShopData.HasPurchase`와 완료 결과 |
| 구매 없는 완료 수락 | 재화·보유 강화 변경 없이 닫기 | Complete 결과와 진행 Buffer |
| 후보 0개 + 현재 Run 마지막 강화 구매 있음 | 마지막 강화 한 장을 구매완료·비활성으로(H 보류) | 실제 Offers 0개 + 마지막 성공 구매 읽기 사실 |
| 후보 0개 + 현재 Run 구매 이력 없음 | 빈 상태 안내와 완료 버튼(H 보류) | 실제 Offers와 이력 없음 사실 |
| 대상 미보유·이미 강화 | 상태 불일치. 팝업을 닫고 지도 갱신·로그 | `TargetNotOwned`·`AlreadyEnhanced` |
| 잘못된 강화·지원하지 않는 레벨 | `IsUnexpected` → 오류 경계 보고 | `InvalidEnhancement`·`UnsupportedEnhancementLevel` |
| 이전 방문·화면의 늦은 결과 | 새 방문의 표시·잠금을 바꾸지 않음 | 요청 번호와 방문 식별값 넷 |
| 같은 방문 숨김·복귀 | 고정 후보와 구매 잠금으로 복원 | `CurrentShopData`. 재추첨 없음 |
| 씬 이탈·호스트 교체·오류 | 소유 요청·표시·구독·Layer를 기존 정리 계약으로 해제 | StageMap 호스트·`PopupViewScope`·오류 경계 |

구매가 이미 반영됐으면 Purchase를 다시 제출해 완료를 재시도하지 않는다. View가 재화를 되돌리거나 강화를 취소하지 않는다. 앞의 두 강화 실패는 방문 고정 뒤 보유 상태가 바뀐 경우이고, 뒤의 둘은 DB 강화 정의와 고정 후보가 다르거나 1레벨 밖 값이 들어온 경우다.

## 7 조립과 Framework 계약

- 기존 `UIBase` 화면 ShopUI와 `UIId.Shop`을 쓴다. 같은 UIId의 강화 화면을 따로 등록하지 않는다.
- 열기·닫기는 `IUISystemService`와 기존 팝업 Scope로 한다. 바깥에서 Panel 활성만 바꿔 개폐를 대신하지 않는다(FW-008/009).
- UI·Job 서비스는 Global 접근 경계를 지킨다. 기능 안에서 루트 서비스나 Core 구현을 찾지 않는다.
- 표시 구현의 생성·의존 연결은 `ShopPopupViewFactory`에 모은다. 렌더·구매·오류 처리를 Factory로 옮기지 않는다.
- 입력 바인딩·Configure 수명·Submit 경로를 지킨다. Build 이후 바인딩·서비스 등록을 더하지 않는다(FW-001~007).
- 창 대기를 Job에 붙들거나 공용 Runner를 멈추거나 Clear하지 않는다.
- 방문 변경·씬 해제에서 자기 카드와 구독만 정리한다(FW-016, FW-103).
- 마지막 강화 구매 읽기 계약은 Core 인수 대상이다. 추가 Sink·DTO·등록이 필요하면 기존 Buffer 공개·조립 경계에서 설계하고 공유 범위를 적는다.

## 8 운영 데이터와 외부 인수 조건

강화 효과 정의와 필수 레시피의 원본은 Core 인계 §4다.

| 항목 | 조건 |
|---|---|
| 강화 항목과 표 | 승인된 1레벨 레시피를 `EnhancementDataSO`로 만들고 `EnhancementDataTableSO`를 InGameGroup에 등록한다. 가격·가중치는 입력 데이터에서 관리한다 |
| 마지막 강화 구매 읽기 | §4.4의 현재 Run 범위·실제 구매 순서·불변 표시값을 Core·Buffer가 줘야 한다(§26, 보류) |
| 강화 스테이지 | 고유 승인 키로 `EnhancementStageData`·Shop/Regular `StageRoot`를 등록한다. 기존 일반 상점·회복 노드를 바꿔 쓰지 않는다 |
| 맵 등장 | 맵 일반 후보에 강화 루트를 등록한다. 후보 N개 중 하나로 같은 확률을 받고 다른 일반 스테이지 비율도 바뀐다. 출현 보장·횟수 제한은 만들지 않는다(Q-E4) |
| 대상 유닛과 획득 | 실제 Unit 표 등록과 획득 경로가 필요하다. 2026-10-27 기준 Shielder(23)·DynamiteHammer(24)는 SO만 있고 Unit 표 미등록이다. Akimbo는 이제 SO가 있고 등록돼 있다(천명님 메인 전환). 강화 SO의 `character`는 실제 `UnitDataSO` 참조가 필요하다 |
| 얼굴·이름·전투 표현 | 대상의 View 외형·아이콘·특성 표현을 인수한다. 누락 자산을 무관한 캐릭터·빈 문자열로 대신해 완료로 처리하지 않는다 |
| 스테이지 외형 | 강화 노드는 StageKey 지정 행으로 구분한다(§8.1). 전용 외형·아이콘이 오기 전에는 지도 구분 완료로 보고하지 않는다 |

누락된 능력치·타기팅·운영 키·이미지를 시험 정의에서 가져와 채우지 않는다. 기획서 15쪽이 "개발 코스트상 가능한 영역만 개발"을 허용하므로 준비된 레시피부터 단계적으로 연다. 빠진 종류와 이유는 인수 상태에 적는다. 시험 fixture는 시험 경계 안에서만 쓰고 제품 진입의 대체 경로로 잇지 않는다.

### 8.1 지도에서 강화 노드 구분

- `StageNodeVisualData`에 StageKey별 명시 외형을 둔다. 지정 행이 있으면 먼저 쓰고, 그 밖의 노드는 (StageType, StageMapRole) 공통 조회를 쓴다. 일반 상점 공통 행을 강화 외형으로 바꾸지 않는다.
- 조회 계약: 기존 `GetNodeVisual(StageType, StageMapRole)`·`TryGetNodeVisual(...)`은 공통 조회로 둔다. `GetNodeVisual(int stageKey, StageType, StageMapRole)`와 같은 인자의 `TryGetNodeVisual(..., out StageNodeVisualData)`를 더한다. 음수 입력은 `ArgumentOutOfRangeException`, Get 미존재는 `KeyNotFoundException`, TryGet 미존재는 false다. `GetAllNodeVisuals()`는 두 종류를 모두 준다.
- 외형 선택은 View 데이터·Query가 한다. `StageMapViewSystem`은 Buffer 노드 키를 전달하고 노드 표시 객체는 받은 외형만 그린다. Core의 InGame 상세를 읽어 강화 여부를 판정하지 않는다.
- StageKey와 자산의 대응은 View 데이터 한 곳에만 둔다. 노드 생성·아이콘·이름·캐시에 StageKey 분기를 복제하지 않는다.
- 노드 방문 가능 여부·연결·샘플링·완료는 바꾸지 않는다.

## 9 SSOT와 변경 독립성

| 기준 | 적용 |
|---|---|
| SSOT | 후보·가격·강화 선택·Run 골드·진행·마지막 성공 구매는 DB/Core/Buffer가 갖는다. 표시 모델과 화면 문맥은 파생값이다 |
| SRP | 상품 내용·카드 표현·요청 결과·의존 조립을 변경 이유별로 나눈다 |
| OCP | 강화 문구·아이콘 변경은 강화 Builder에 머문다. Core 효과 종류가 늘어도 Controller·Job에 키별 분기를 넣지 않는다 |
| LSP | 강화 방문도 기존 구매·완료·표시 계약을 지킨다. 빈 방문도 정상 표시·완료한다 |
| ISP·DIP | 표시 객체에는 필요한 ReadSink와 좁은 조회 함수만 준다. 구체 생성은 Factory가 한다 |
| JMH·FW | 등록·Global 접근·생성·입력·UI 이력·정리 경계를 지킨다 |

| 변경 | 고칠 곳 | 확인 |
|---|---|---|
| 강화 카드 문구 | 강화 Builder와 문구 | 강화 카드 내용만 |
| 공통 설명 크기·배치 | 설명 카드 Variant와 상점·회복 연결 | 세 이벤트의 긴 한글·해상도·아이콘·가격·클릭 영역 |
| 강화 표시 의존 추가 | 강화 Builder와 Factory | 조립과 해당 표시 |
| 공통 모델·카드 입력 계약 | 모델·화면 공통 렌더·후보 구분 | 세 이벤트, 일반 상점·강화 교차, 0~3개, 이력 카드 입력 차단 |
| 강화 노드 외형 | StageKey View 외형 데이터·조회 | 강화·일반 상점 구분과 공통 외형 호환 |
| 공통 안내 표시 | 상점·회복 화면 안내와 강화 연결 | 세 이벤트 안내 가시성과 숨김·복귀·닫기 수명 |
| 강화 효과·샘플링 규칙 | Core 소유자 | Core 인수 기준 |

## 10 인수 기준

계획 §2의 남은 일과 짝을 이룬다. 기본 진입·구매·다음 전투 반영은 2026-10-20 실제 Play로 3종 각각 확인했다(계획 §3.7). 표시는 운영 강화 노드의 전체 인수 기준이다.

1. **정식 진입:** 등록된 강화 노드를 실제 지도 입력으로 골라 Node·Shop Job을 거쳐 팝업이 열린다. 운영 진입에 직접 Core 호출·강제 Buffer 주입이 없다.
2. **카드와 빈 후보:** 실제 후보 1/2/3에 맞는 카드, 강화명·가격·얼굴·대상 이름·효과 설명. 후보 0개면 §4.4(이력 카드 또는 빈 안내). 이력 카드는 구매 명령·과금·가짜 Offer를 만들지 않는다.
3. **거래 일치:** 고른 `OfferId`의 `TargetOwnedId`에만 1레벨 강화가 저장되고 게시 가격만큼 Run 골드가 한 번 빠진다. 다른 개체와 일반 Level/Experience는 그대로다.
4. **미선택:** 골드·강화 변경 없이 노드가 완료되고 지도로 돌아간다.
5. **거절·중복:** 세 이벤트에서 실패·완료 대기 안내가 보인다. 골드 부족으로 후보가 바뀌지 않고, 강화 실패 사유 넷이 §6 분류대로다. 연타·동시 입력·이전 방문 입력이 중복 구매를 만들지 않는다.
6. **완료 재시도:** 구매 반영 뒤 완료 실패에서 잠금·영수증을 유지하고 Complete만 재시도한다.
7. **수명과 표시 전환:** 일반 상점 → 강화 → 일반 상점, 숨김·복귀·씬 이탈·호스트 교체에서 설명·완료 표시·잠금·구독이 섞이지 않는다.
8. **다음 전투:** 구매한 개체의 강화가 전투 준비에 반영되고 미강화 개체는 그대로다.
9. **데이터 연결:** 실제 에셋·표·키·대상·외형 참조를 `DataBaseAssetTests`로 확인한다. fixture 통과와 운영 인수를 나눈다.
10. **표현 확인:** 카드의 긴 한글·아이콘 비율·이름/가격 독립·효과 설명·실패 안내·버튼과 하단 영역 겹침·입력 차단.
11. **마지막 구매 수명:** 강화 A 구매 뒤 일반 상점 구매·미선택·거절을 거쳐도 빈 강화 방문에 A가 보인다. 강화 B를 사면 B로 바뀐다. 새 Run 뒤에는 이전 Run 이력이 보이지 않는다.
12. **지도 구분:** 같은 Shop/Regular라도 강화와 일반 상점이 StageKey에 따라 다른 외형으로 보인다. 지정이 없는 노드는 공통 외형을 유지한다.

## 11 기획서·Core 대조 (2026-10-19)

### 11.1 기획서

| 기획서 | 이 설계·Core | 판정 |
|---|---|---|
| 3쪽: 입장 전 UI 없음, 플레이어를 따로 옮기지 않음 | 기존 상점 팝업만 연다 | 맞음 |
| 3쪽: 세 이벤트가 같은 UI 구성 | 공통 설명 카드·안내를 세 이벤트에 쓴다 | Q-E1·Q-E2 C안 |
| 3쪽: 가중치·챕터 안 최소/최대·연속 등장 조절 | 맵 생성에 없음, 균등 무작위 | Q-E4 보류 |
| 12쪽: 선택지 3개, 선택 안 해도 됨, 중복 없음 | Core가 서로 다른 (보유 개체, 강화) 쌍을 최대 3개 고정. 0개면 마지막 구매 표시 | 1레벨 범위에 따른 후보 감소와 후속 결정 |
| 12쪽: 한 캐릭터 강화를 사면 같은 캐릭터의 다른 강화 미등장 | 강화한 개체는 후보에서 빠짐. 재고용 거절로 결과가 같음 | 맞음 |
| 12쪽: 다음 방문부터 레벨 강화 | 제외(사용자 확정, Core도 1레벨만) | 의도한 차이 |
| 12쪽: 가격 비례 가중치 / 15쪽: 같은 가중치, 모두 700골드 | 강화 SO의 가격·가중치를 Core가 씀. UI 상수 없음 | 맞음(15쪽 기준) |
| 12쪽: 고르면 그대로 UI 종료 | 구매 성공 뒤 같은 요청에서 완료, 완료 수락 때 닫음 | 맞음 |
| 13쪽 ③ 얼굴, ②④ 강화명·골드 | 유닛 외형 아이콘, 강화명·가격 + 이름·설명 | 맞음 + Q-E1 추가 |
| 13~14쪽 ⑤⑥⑦ 선택한다·보유 골드·선택하지 않는다 | 기존 하단 배치·문구 | 맞음 |
| 15~16쪽 강화 목록 | Core 8종(이름 없는 카우보이 항목 제외) | 맞음 |
| 15쪽 가능한 영역만 개발 | 준비된 레시피부터 단계적으로, 빠진 종류 보고 | 맞음 |

### 11.2 Core 계약

| 계약 | 확인한 것 |
|---|---|
| 방문·구매·완료 | `IShopService.EnterShop / Purchase / CompleteShop`. 상세가 `EnhancementStageDetail`이면 `ShopVisitKind.Enhancement` 방문, `EnhancementOfferPlanner`가 최대 3개(`ShopOfferLimits.Maximum`) |
| 지금 방문 | `CurrentShopData`의 `Offers·EntryId·VisitKind·PurchasedMask·HasPurchase`. 후보 0개여도 `VisitKind`로 구분 |
| 후보 값 | `ShopPurchaseSnapshot`의 `Kind·OfferId·CharacterKey·TargetOwnedId·Enhancement(Level 1)·EnhancementName·EnhancementDescription·Cost` |
| 명령·처리 | `ShopViewCommandData.Purchase / Complete`, `IShopViewBufferWriteSink.Submit`, `ShopFlow`·`ShopPlan`·`ShopCommandJob` → `ProcessNextShopCommand(int)` |
| 구매 뒤 완료 | `ShopCommandExecutor`가 영수증을 먼저 남기고 같은 요청에서 `CompleteShop`. 완료가 실패해도 구매는 남는다 |
| 결과 | `ShopCommandResult`의 `RequestNumber·Context·Kind·OfferId·PurchaseResult·CompletionResult` |
| 실패 사유 | `ShopFailureReason`의 `InsufficientFunds·TargetNotOwned·AlreadyEnhanced·InvalidEnhancement·UnsupportedEnhancementLevel` |
| 마지막 성공 강화 구매 | 미제공(§4.4) |
| 지도 외형 | 노드 StageKey는 Buffer에 있다. View Query 확장으로 구분(§8.1). Core 종류 enum·계산은 바꾸지 않는다 |

## 12 보류·결정 대기

| 항목 | 상태 |
|---|---|
| H 마지막 성공 구매 읽기 | [Core 전달 문서 §26](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#26-강화-후보-소진-시-마지막-구매-표시용-읽기-계약-요청-2026-10-19-view--core-미전달) 미전달·인수 대기. 코드 리뷰 ES03도 같은 항목이다 |
| 전용 노드 외형·아이콘 | 인수 대기(2026-10-20 사용자). 지금 1202 행은 일반 상점과 같은 프리팹 |
| E1~E4 전체 인수 | H·전용 자산·남은 레시피 대상(Shielder·DynamiteHammer·Akimbo 레시피)의 운영 인수 뒤 |
| 쓸 수 없는 강화 | 새 Core 규칙에서 가시밭이 꺼져 독 가시가 사도 효과가 없다. Core 요청 CR2(후보에서 빼기)와 Q-NC6은 [새 Core 대응 설계](2026-10-26-new-core-lobby-stage-response-design-Hojin.md) D6에서 결정 대기다 |
| ES04 | 후보에 덱 밖 예비 보유 유닛도 들어간다. 정책 확인 항목이다([강화 상점 코드 리뷰](2026-10-23-enhancement-shop-code-review.md)) |

## 13 다른 작업자 영향

| 작업자 | 고친 곳 | 기록 |
|---|---|---|
| 천명님 Core | 없음. 강화 효과·샘플링은 Core 인계를 소비만 한다 | [Core 전달 문서 §26](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#26-강화-후보-소진-시-마지막-구매-표시용-읽기-계약-요청-2026-10-19-view--core-미전달) 요청(미전달) |
| 공유 시험 | `DataBaseAssetTests`에 실제 에셋 교차검증 추가(D·E0·§17), StageKey Query 대역 overload(`StageMapViewSystemTests`·`CombatEntryIntegrationTests`·`RecoveryStageMapViewTests`·`ViewPopupCompositionTests`) | 계획 §3 |
| 후속 표시 고침 | 산 강화 특성이 정보창에서 사라지던 것(ES01)과 독 가시 설명이 기본값으로 남던 것(ES02)은 2026-10-23 따로 고쳤다 | [강화 상점 코드 리뷰](2026-10-23-enhancement-shop-code-review.md), [강화 특성 표시 수정 계획](../plans/2026-10-23-enhanced-trait-display-fix-plan.md) |

Core·FieldCamera·Architecture·AudioSystem R·Title·튜토리얼은 이 작업으로 바꾸지 않았다.

## 14 레벨업 특성 성장(증강) 선택 화면 (TG1~TG10, 2026-10-28 3판)

2026-10-29 별도 주제로 옮겼다 → [증강 설계](2026-10-28-trait-growth-design-Hojin.md), [증강 계획](../plans/2026-10-28-trait-growth-plan-Hojin.md). 옛 3판 본문(§14.1~§14.9)은 git `a8612321e`.

<a id="145-보류와-범위-밖"></a>

메인 강화 상점과 관련된 증강 쪽 사실(옛 §14.5 줄, §15가 부른다):

- 증강은 메인 강화 상점과 다른 기능이고 Core 이름 `TraitGrowth` 그대로다. 증강이 남아 있으면 강화 노드 진입도 먼저 막힌다(`PendingTraitGrowth`). 둘은 서로를 소모하지 않는다.
- 10-28 사용자: 강화 상점 SO(`EnhancementDataSO`·`EnhancementData`·표·후보 에셋)는 **지우지 않고 고쳐서** 메인 강화 상점에 쓴다. 이름은 `MainEnhancement…`(`MainEnhancementDataSO`·`MainEnhancementData`·`MainEnhancementDataTableSO`·조회 `MainEnhancementTable`)로 바꾼다. `Enhanced…`는 증강이 이미 "특성 강화형"(`TraitGrowthKind.Enhance`·`enhancedName`·`TraitGrowthDefinitionData.Enhanced`) 뜻으로 써서 피했고, 키 `EnhancementKey`와 낱말 꼴을 맞췄다.
- 10-29 사용자: 메인 강화 상점 전까지 강화 노드(1202)를 맵에서 뺀다. `StageRootDataTable`에서 1202 등록을 빼고 `StageMap_Chapter1` 강화 분포를 가중치 0·최소 0으로 껐다(분포가 남은 채 후보가 없으면 `ChapterStagePool`이 Run 시작 때 예외). 메인 강화 상점 때 둘을 되돌린다(등록, 가중치 4·최소 1 — 천명님 `ccaf47c8` 값). 에셋 `Stage_Enhancement.asset`·노드 외형 1202 행은 지우지 않았다. 바꾸는 범위(Data·표·조회만인지, Core 키까지인지)와 담당은 §15에서 정한다.

## 15 메인 강화 상점 (ME1~ME8, 2026-10-29 초안)

- 상태: 초안이고 Q-ME1~Q-ME7 결정을 기다린다. 코드·에셋 변경은 없다.
- 요청: 10-29 사용자 "메인 강화 상점을 정상 작동하게 고치는 설계 초안". 강화 노드(1202)는 지금 맵에서 빠져 있다(§14.5).
- 11-01 사용자 결정: 강화 상점은 **메인 전용**이다(천명님 Core 설계 §1·§8과 같음). 서브 특성은 상점에서 팔지 않고 레벨업 특성 선택으로만 얻는다. Q-ME1~Q-ME7은 그대로 대기다.
- 11-01 §15.2 정정: `EnhancementDataTable`과 `Enhancement_Default`의 후보는 지금 비어 있다(10-29 기록의 4행·후보 셋은 낡음). 같은 날 일반 상점을 유닛(`Stage_Shop` 1201 → `Shop_Unit`)과 아이템(`Stage_Shop_Item` 1203 → `Shop_Item`)으로 나눴고, 두 상점은 같은 상점 종류라 상점 노드마다 균등 추첨한다. `Shop_Default`는 참조가 없어졌지만 지우지 않았다.

### 15.1 요구와 출처

| 출처 | 내용 |
|---|---|
| 사용자 결정(10-28·10-29, §14.5) | 강화 상점 SO(`EnhancementDataSO`·`EnhancementData`·표·후보 에셋)는 지우지 않고 고쳐 쓴다. 이름은 `MainEnhancement…`로 바꾸고, 증강은 `TraitGrowth` 그대로 둔다. Core 진입 거절을 풀고, 빼 둔 둘(1202 등록, 맵 강화 분포 4·1)을 되돌린다 |
| 천명님 Core 설계 [§8](2026-10-27-category-trait-progression-design.md#8-enhancement-shop-boundary-and-migration) | 강화 상점은 메인 성장만 판다. 메인 상품·양·가격·회복과의 관계·재구매 제한·스테이지 경제는 따로 정해야 하고, 그 문서는 추측한 상품을 허락하지 않는다. 가격 고정과 "준비 뒤 설치" 거래를 지킨다. 레벨업 선택과 상점 구매가 서로를 소모한다고 가정하지 않는다. 메인 상품이 없으면 노드를 열지 않는다(빈 상점·공짜 능력치 금지) |
| 천명님 인계 [§9](../handoffs/2026-10-27-category-trait-progression-handoff.md#9-phase-e-inspection-and-acceptance-ledger) | 메인 상점은 증강과 별개인 출시 조건이다 |
| 기획서(Dropbox `기획 문서`) | 이벤트 스테이지 기획서(Ver0.1)의 강화는 서브 유닛 강화(지금 걷힘)뿐이고 메인 상품은 없다. 이벤트 공통 규칙(선택지 3장, 안 골라도 됨, 하나 고르면 끝)은 있다. 캐릭터 레벨 밸런스 기획서의 메인 레벨업 성장은 체력 +3·공격 +1·방어 +1 중 하나다. 옛 통합 기획서에는 "메인 캐릭터 성장 갈림길"이라는 비전투 스테이지 이름만 있다 |
| 기획 메모(Dropbox `기획 브레인스토밍` txt, 10-29 확인) | 정한 규칙이 아니라 고민 메모다. `새로운 4.txt`(10-17): 중간 시연 루프 "엘리트 전투 → **메인 캐릭터 성장 보상** → 이벤트 → 보스", 후보 "1. 메인 캐릭터의 카테고리 부여 2. 메인 캐릭터 그냥 강화 3. ???". `남은거.txt`(10-01): 메인 차별점(이벤트 스테이지) "무기 강화? / 레벨 비례 능력치 강화 / 태그 선택 / 특성 선택". `안재령의고뇌Ver3.txt`(10-26 회의 메모): 메인은 "올라운더, 안정적, 작은 육각형이 점점 커짐", 메인의 이타(아군 체력·공격)·이기(피흡) 형태는 고민 중. 재구매·현재 체력 규칙은 어느 메모에도 없다 |
| 천명님 구현 계획 [`:55`](../plans/2026-10-27-category-trait-progression-implementation-plan.md) | "기존 서브 강화 레시피를 메인 상품으로 재해석하지 않는다." 설계 §8은 옛 에셋을 적절하면 역사 자료로 보존하라고 한다. Phase F에 Core 검사 `ProductionMaps_HaveNoObsoleteReachableSubEnhancementShop`이 계획돼 있다. 10-29 확인: `d48542034`(10-28) 뒤 천명님 Core·문서 변경은 없다 |

상품 내용은 어느 문서에도 없다. 이 초안은 상품의 **모양**과 연결을 정하고, 값은 기획이 정한다(ME-0).

### 15.2 확인 사실 (2026-10-29, HEAD `a8612321e`)

| 곳 | 지금 |
|---|---|
| 진입 거절 | `ChapterProgressionCoordinator.PrepareStageEntry`(`:316-317`)와 `ShopService.EnterShop`(`:118-119`)이 상세가 `EnhancementStageDetail`이면 `EnhancementShopUnavailable`로 거절한다 |
| 게시·구매 거절 | `ShopTransactions.PrepareShop`(`:52-53`)은 강화 방문이면 예외를 낸다. 구매는 `ShopService.Purchase`(`:162`)·`ShopPurchaseRules.Plan`·`ShopTransactions.PublishPurchase`(`:84`)가 `SubTraitPurchaseUnavailable`로 거절한다 |
| 걷힌 것 | 서브 강화 후보·구매 규칙(`EnhancementOfferPlanner`·`EnhancementPurchaseRules`·`EnhancementShopDefinitionAdapter`)은 천명님 `d48542034`(10-28)에서 지워졌다 |
| 맵 생성 | `StageGeneratorSystem:124`가 `EnhancementStageDetail`을 강화 종류로 센다. 분포는 맵 SO `enhancementDistribution`이고 지금 0·0이다 |
| DB | `EnhancementDataSO`(천명님, 10-19)의 칸은 `key`(`EnhancementKey`)·`character`·`level`(1만)·`cost`·`weight`·`displayName`·`description`·추가/교체 특성 목록이다. `EnhancementData`는 Core 계약 `EnhancementKey`·`OwnedEnhancement`로 검사한다. 조회 `EnhancementTable`(`InGameDataBase.Enhancements`)은 등록돼 있지만 **읽는 곳이 없다**. 스테이지 상세는 `EnhancementStageData` → `EnhancementStageDetail(CandidateKeys)`다 |
| 에셋 | 표 `EnhancementDataTable` 4행(DoubleShot·DoorBreaching·PoisonThorn·ChainExplosion, 모두 서브 강화), 상세 `Enhancement_Default`(후보 셋), 루트 `Stage_Enhancement`(1202, Regular, 표에서 뺌), 노드 외형 `NodeVisual_Shop_Enhancement`(표시 이름 "Enhancement") |
| Core 계약 | `EnhancementKey` 값은 서브 강화 이름이다. `EnhancementDefinition`은 쓰는 곳이 없다. `ShopPurchaseSnapshot.ForEnhancement`·`ShopPurchaseResult.ForEnhancement`는 "대상 보유 ID + `OwnedEnhancement`" 모양(서브용)이다 |
| 메인 상태 | `MainCharacterPersistentState`에 현재 체력·지속 최대 체력·공격·방어가 있다. 레벨업 성장은 `MainGrowthRules.Apply(state, kind, spec)`가 종류(`MainGrowthKind` MaxHealth·Attack·Defense)마다 레벨 설정의 양을 더한다. 최대 체력이 올라도 현재 체력은 그대로다(X4). 상점 구매 입력은 이미 메인 상태를 읽는다(`ShopPurchaseInput.Main`, 메인이 죽었으면 거절) |
| View | 상점 팝업은 상품 종류(`ShopProductKind`)마다 카드 내용 Builder를 고른다. 강화 Builder `EnhancementShopOfferContentBuilder`(호진, 10-20)는 `CharacterKey`로 이름·얼굴을 찾고 강화 이름·설명을 그린다. 실패 분류 `ShopResultPolicy`에는 서브 강화 이유 넷이 있다. StageSelect에서 메인 능력치를 그리는 곳은 덱 편집 상세와 회복 창이다 |
| 문서 정정 | §2.2의 "`ShopVisitKind`는 Builder가 Buffer에서 읽는다"는 지금 코드와 다르다. `ShopPopupModelBuilder`는 `VisitKind`를 읽지 않는다(10-26 정리 뒤, 정리 계획 `:454`) |
| 시험 | 이름이 바뀌는 타입을 부르는 시험은 천명님 `CoreIntegrationTestData.cs` 하나다 |

### 15.3 설계

**ME1 상품은 메인의 지속 능력치 성장이다(Q-ME1 A 기준).** 상품 하나는 성장 종류(`MainGrowthKind`)·양·가격·가중치·이름·설명이다. 사면 메인 지속 값(최대 체력·공격·방어)에 양을 더한다. 레벨업 성장과 같은 값에 쌓이므로 전투·덱 편집·HUD에는 지금 길로 반영되고, 새 Buffer 상태 칸이 필요 없다. 메인 특성 부여는 증강의 메인 단계와 같은 사실을 두 길에서 바꾸게 되므로 이번 범위에서 뺀다(나중에 따로 설계).

**ME2 방문·구매는 지금 상점 규칙 그대로다(Q-ME2·Q-ME3).** 진입 때 후보 중 가중치로 3장(한 방문 안 중복 없음)을 뽑아 고정한다. 한 번 사거나 사지 않고 끝낸다(`HasPurchase`·`CompleteShop`). 다음 방문에 같은 상품이 다시 나올 수 있고, 산 만큼 쌓인다. 후보가 3장보다 적으면 있는 만큼 보이고, 0장인 상세는 DB 검사에서 막는다(빈 상점 금지).

**ME3 성장을 더하는 규칙은 한 곳이다.** "메인 지속 값에 종류·양을 더하고 현재 체력은 그대로 둔다"를 레벨업과 상점이 함께 쓴다. 지금 `MainGrowthRules.Apply`는 양을 레벨 설정에서 꺼내므로, 양을 인자로 받는 함수 하나로 나눠 두 길이 부르게 Core에 요청한다(SSOT). 최대 체력을 샀을 때 현재 체력도 오르는지는 Q-ME4다.

**ME4 DB: 강화 SO를 메인 상품 SO로 고친다(Q-ME5).**

| 지금 | 바뀐 뒤 | 칸·검사 |
|---|---|---|
| `EnhancementDataSO` | `MainEnhancementDataSO` | `key`(정수 ≥ 1)·`growthKind`·`amount`(≥ 1)·`cost`(≥ 0)·`weight`(≥ 1)·`displayName`·`description`. `character`·`level`·추가/교체 특성은 걷는다 |
| `EnhancementData` | `MainEnhancementData` | 순수 C#(P1), 같은 검사 |
| `EnhancementDataTableSO`, `EnhancementTable`·`IEnhancementTableQuery`, `InGameDataBase.Enhancements` | `MainEnhancementDataTableSO`, `MainEnhancementTable`·`IMainEnhancementTableQuery`, `MainEnhancements` | 키가 겹치면 DB 생성 실패 |
| `EnhancementStageData`·`EnhancementStageDetail` | `MainEnhancementStageData`·`MainEnhancementStageDetail` | 후보 = 상품 SO 목록, 1개 이상·중복 없음 |

- 클래스 이름과 `.cs` 파일 이름을 함께 바꾸고 `.meta` GUID는 그대로 둔다. 그래야 에셋의 스크립트 연결이 유지된다. 둘 중 하나만 바꾸면 Missing Script가 된다.
- 상품 에셋 넷·상세·표는 지우지 않고 값만 고친다. 파일 이름을 상품 이름으로 바꿀 때도 `.meta`를 함께 옮겨 GUID를 지킨다(표·상세 참조 유지).
- 키를 정수로 두면 상품 하나는 에셋 하나와 표 한 줄이고 코드는 바뀌지 않는다. `EnhancementKey`·`OwnedEnhancement`·`EnhancementDefinition`과 서브 실패 이유의 정리는 Core 판단으로 넘긴다(지우지 않는다).

**ME5 Core 계약(요청할 것).**

- 진입 거절 두 곳을 메인 상품 진입으로, 게시·구매 거절 셋을 메인 상품 길로 바꾼다.
- 진입: 상세가 메인 강화면 후보를 읽어 가중치로 3장을 고정하고, 강화 방문으로 게시한다(종류 값은 Q-ME6).
- 스냅샷: 메인 상품 모양 하나(상품 키·성장 종류·양·가격·이름·설명). `CharacterKey`는 메인 정의 키이고 View가 얼굴을 찾는 데 쓴다. 서브 모양(대상 보유 ID·`OwnedEnhancement`)은 쓰지 않는다.
- 구매: 고정 가격·잔액·메인 생존을 보고, 골드와 메인 지속 상태를 지금 거래 경계(준비 뒤 설치) 안에서 함께 바꾼다. 같은 방문의 두 번째 구매는 `PurchaseAlreadyUsed`다.
- 레벨업 증강과 서로를 소모하지 않는다. 증강이 남아 있으면 지금처럼 노드 진입이 먼저 막힌다(`PendingTraitGrowth`).
- 실패 이유는 있는 값을 쓴다. 새 값이 필요하면 Core가 정해 알려 주고, View는 분류(`ShopResultPolicy`)만 맞춘다.

**ME6 View: 상점 팝업은 그대로 두고 카드 Builder 하나만 바꾼다.** 새 UIId·Controller·Layer·Job은 만들지 않는다. `EnhancementShopOfferContentBuilder`를 `MainEnhancementShopOfferContentBuilder`로 바꾼다(얼굴 = `CharacterKey`의 메인 외형, 제목 = 상품 이름, 설명 = 상품 설명). 서브 대상 이름 줄(`ShopPopupText.EnhancementDetails`)은 메인 문구로 바꾼다. 구매 결과 처리·완료 대기는 지금 상점과 같다. 노드 표시 이름 "Enhancement"는 기획 문구로 바꾼다(에셋 값, 전용 아트는 계속 대기).

**ME7 맵 되돌리기는 마지막이다(출시 게이트).** Core·DB·View가 모두 들어오고 운영 상품 에셋이 생긴 뒤에만 `StageRootDataTable`에 1202를 다시 넣고, `StageMap_Chapter1` 강화 분포를 가중치 4·최소 1(천명님 `ccaf47c8` 값)로 되돌린다. 그 전에는 노드가 나오지 않으므로 중간 커밋이 게임을 깨지 않는다.

**ME8 담당과 순서(Q-ME7).** 상품 표(기획) → Core 요청·구현(천명님) → DB 이름·칸(계약과 한 묶음) → View → 되돌리기 순이다. DB Data가 Core 계약 타입을 쓰므로 DB와 Core 변경은 같은 커밋 묶음이어야 컴파일된다.

### 15.4 정할 것

| ID | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q-ME1 | 메인 상품 내용 | A 지속 능력치(최대 체력·공격·방어 +양) / B 메인 특성 부여 / C 둘 다 | **A.** 지금 Core 상태로 표현되고 새 칸이 없다. B는 증강 메인 단계와 같은 사실을 두 길에서 바꾸므로 따로 설계해야 한다 |
| Q-ME2 | 한 방문 구성 | A 지금 상점 규칙(3장·한 번 구매·안 사도 됨) / B 여러 번 구매 | **A.** 기획서 이벤트 공통 규칙과 같고 코드가 이미 있다 |
| Q-ME3 | 재구매 | A 다음 방문에 같은 상품이 다시 나오고 상한 없음 / B Run당 상품마다 한 번 / C 종류별 상한 | **A.** 가장 단순하다. 상한은 밸런스에서 필요할 때 B·C로 더한다(그때 상태 칸이 생긴다) |
| Q-ME4 | 최대 체력을 사면 현재 체력은 | A 그대로(레벨업 X4와 같음, 회복은 회복 스테이지) / B 오른 만큼 함께 오름 | **A.** 규칙이 레벨업과 하나로 남는다 |
| Q-ME5 | 이름 바꾸는 범위와 키 | A DB(Data·SO·표·조회·스테이지 상세)만 `MainEnhancement…`, 키는 정수, Core 옛 계약 정리는 Core가 / B Core 키까지 새 enum `MainEnhancementKey` | **A.** 상품을 더할 때 Core enum을 고치지 않아도 된다 |
| Q-ME6 | Core 종류 값 | A `ShopVisitKind.Enhancement`·`ShopProductKind.Enhancement`를 메인 강화 뜻으로 다시 씀(번호 그대로) / B 새 값 `MainEnhancement`를 더하고 옛 값은 거절로 남김 | **A**(Core 판단). 서브 강화 방문은 이미 게시될 수 없어 옛 사본이 남아 있지 않다. B로 가도 View는 Builder의 `Kind` 한 줄만 바뀐다 |
| Q-ME7 | Core 구현 담당 | A 천명님께 요청 문서로 / B 사용자 지시로 이 세션이 Core까지 구현(Core 전달 문서 §5에 기록) | **A.** 저장소 규칙(Core는 요청)과 천명님 §8 "별도 설계"에 맞다 |

### 15.5 원칙 점검

| 기준 | 적용 |
|---|---|
| SSOT | 상품 값은 DB SO 하나, 성장 더하기 규칙은 Core 함수 하나(ME3), 메인 능력치는 Buffer 하나에 있다. View는 스냅샷을 그리기만 한다 |
| SRP | 상품 값(SO), 뽑기·구매 판정(Core), 거래(Buffer), 카드 내용(Builder), 팝업 수명(Controller)이 나뉜다 |
| OCP | 상품 추가·값 조정은 에셋만 바꾼다(Q-ME5 A). 새 상품 종류(특성 부여 등)일 때만 Core·Builder가 바뀐다 |
| LSP·ISP·DIP | 카드 Builder는 `IShopOfferContentBuilder` 계약을 그대로 지키고, 이름·얼굴 조회 함수만 받는다 |
| JMH·FW | 새 System·서비스·UIId가 없다. 조회 이름만 바뀌고 등록 자리(`InGameDataBaseFactory`)는 그대로다. 거래는 준비 뒤 설치다 |
| 변경 독립성 | 아래 표 |

| 변경 | 고칠 곳 | 확인 |
|---|---|---|
| 상품 하나 추가·가격 조정 | 상품 에셋·표 | 상점 진입 재생 |
| 맵 등장 빈도 | 맵 SO 분포 | 맵 생성 |
| 카드 모양 | Builder·공용 카드 | 상점 화면 |
| 새 상품 종류(특성 등) | Core 규칙·스냅샷·Builder | 새 설계 |

### 15.6 Phase(안)과 확인

1. ME-0 상품 표(사용자·기획): 상품마다 종류·양·가격·가중치·이름·설명. 참고 값은 메인 레벨업 성장량(체력 +3·공격 +1·방어 +1)과 옛 강화 가격 700G이고, 정하는 것은 기획이다.
2. ME-a Core 요청: [StageSelect Core 요청](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md)에 새 절을 쓴다(ME3·ME5·Q-ME5·Q-ME6). 천명님 답을 받은 뒤 계획을 쓴다.
3. ME-b Core·DB(한 묶음): 계약·진입·뽑기·구매·거래(Core), SO·Data·표·조회·스테이지 상세의 이름과 칸, 에셋 값(담당은 Q-ME7). 천명님 시험은 호출만 맞춘다.
4. ME-c View: 카드 Builder·문구·실패 분류, 노드 표시 이름.
5. ME-d 되돌리기와 확인: 1202 등록, 맵 분포 4·1. 재생 확인 — 노드가 맵에 나옴 → 진입하면 메인 상품 3장 → 사면 골드가 줄고 메인 능력치가 오름(덱 편집 상세·다음 전투) → 완료 → 다음 노드. 증강이 남았을 때 진입이 막힘, 골드 부족 안내, 다음 방문에 같은 상품이 다시 나옴.
6. ME-e 문서·공유.

새 시험 코드는 만들지 않는다. 컴파일과 재생은 사용자 Unity 확인이다.

### 15.7 다른 작업자 영향

| 사람 | 닿는 곳 | 처리 |
|---|---|---|
| 천명님 | Core 상점·진행·거래·계약, `EnhancementDataSO`·`EnhancementStageData`(작성자), `CoreIntegrationTestData.cs` | 요청 문서(ME-a). DB 이름 바꾸기를 이 세션이 하면 변경 정리를 넘긴다 |
| 현강님 | 상점 팝업 UI·카드 프리팹 | 바꾸지 않는다 |
| 민형님 | `InGameDataBaseFactory`(DB Factory) | 조회 이름 줄만 바뀐다(Q-ME5 A). 바꾸면 공유한다 |

## 16 훈련소 내부 연결 (TV1~TV8, 2026-11-02 초안)

> **대신함(2026-11-02):** 이 절은 구현되지 않았다. 훈련소는 [훈련소 View 연결 설계](2026-11-02-stage-training-view-design-Hojin.md)로 구현했다. 그 설계의 TV 번호는 이 절의 번호와 다르다.

- 상태: 초안이고 Q-TV1~Q-TV3 결정을 기다린다. 코드·에셋 변경은 없다.
- 요청: 11-02 사용자 "UI 작업자가 UI를 만질 수 있으니 연결·내부 구현을 먼저" → "설계 작성".
- 11-02 사용자 결정: 훈련소는 메인·서브 구분 없이 특성을 강화하는 시설이다. §15의 11-01 결정("강화 상점은 메인 전용")을 대신한다. 그래픽 쪽 목록의 나머지(메인 캐릭터 강화·메인 스킬 강화·서브 캐릭터 성장·랜덤 이벤트)는 일단 무시한다.
- Core 인계: [골드 시설·특성 랭크 인계](../handoffs/2026-11-02-run-gold-facilities-trait-ranks-core-view-handoff.md). Core 설계: [골드 시설 설계](2026-11-01-run-gold-facilities-and-trait-ranks-core-design.md).

### 16.1 범위

| 넣음 | 뺌 |
|---|---|
| 훈련 방문의 카드 값 만들기 | 프리팹 변경(스크롤·배치·제목) |
| 카드 누름 → `PurchaseTraining` 제출 | 탭(사용자 결정으로 목록 하나) |
| 훈련 결과 받기·완료 재시도 | 유닛·덱 카드의 랭크 표시(`UnitCardUI`) |
| 로컬 확인 절차 | 운영 가격·1202 등록·출현 값 커밋 |

회복 표시 수정은 파일이 겹치지 않아 따로 진행한다.

### 16.2 확인 사실 (2026-11-02, HEAD `9f2e6fbf7`)

- Core·Buffer 쪽은 끝나 있다. 진입 `EnterShop`, 게시 `CurrentShopData.VisitKind`·`TrainingChoices`·`TrainingReceipt`, 명령 `ShopViewCommandData.PurchaseTraining`, 실행 `ShopCommandExecutor`, 결과 `ShopCommandKind.PurchaseTraining`.
- 실행기는 구매 결과를 먼저 게시하고 이어서 완료를 시도한다. 일반 구매와 같은 순서라 "구매 성공 + 완료 실패" 재시도 경로를 그대로 쓸 수 있다.
- `HasPurchase`는 훈련 영수증도 포함한다. 그래서 `ShopPopupModelBuilder`의 나가기 문구와 `Show`의 "구매 완료" 안내는 고치지 않아도 맞다.
- 지금 코드에서 훈련이 막히는 곳은 넷이다.
  - `ShopPopupModelBuilder.cs:66`이 `Offers`만 돈다. 훈련 방문은 `Offers`가 비어 카드가 없다.
  - `ShopOfferCardEntry`와 `ShopUI`가 상품 번호만 기억한다.
  - `IShopPopupInput`에 훈련 입력이 없다.
  - `ShopPopupController.MatchesResult`(`:226`)가 Purchase가 아니면 모두 완료로 본다. 훈련을 제출하면 결과가 맞지 않아 입력이 잠긴 채로 남는다. `IsMismatch`(`:231`)와 `HandleResult`(`:238`)도 Purchase만 구매로 본다.
- 다시 연결될 때 쓰는 `Tick`의 명령 넘겨받기는 명령 종류를 보지 않아 그대로 동작한다.
- `RenderCards`는 카드를 개수만큼 만들어 가운데 정렬로 옆으로 늘어놓는다. 스크롤이 없어 카드가 많으면 화면 밖으로 나간다.
- 시험 코드는 `ShopPopupController`·`ShopPopupModelBuilder`·`ShopUI`·`ShopPopupViewFactory`를 쓰지 않는다. 고칠 시험이 없다.
- 작업 트리에 Core 특성 성장 쪽 커밋 전 변경이 있다(`TraitGrowthHistory`·`EffectiveTraitProgression` 등, `TrainingGround*` 시험 포함). 이 설계가 고칠 파일과는 겹치지 않지만, 랭크 계산 쪽이 바뀌는 중일 수 있어 커밋 뒤 계약을 다시 본다.

### 16.3 설계

**TV1 방문 종류로 카드 원본을 고른다.** `ShopPopupModelBuilder.Build`가 `shop.VisitKind == ShopVisitKind.Training`이면 `TrainingChoices`를, 아니면 지금처럼 `Offers`를 돈다. 목록을 자르거나 거르지 않고 Core 순서 그대로 그린다.

**TV2 카드 값은 새 클래스 하나가 만든다.** 새 `ViewSystem/StageMap/Shop/TrainingChoiceContentBuilder.cs`(+ `.meta`). 훈련 선택지는 상품이 아니라서 `IShopOfferContentBuilder`를 구현하지 않는다.

| 카드 칸 | 값 | 출처 |
|---|---|---|
| 그림 | 유닛 얼굴 | `OwnedId` → `ICharacterBufferReadSink.TryGetOwned` → `UnitVisual.InfoIcon` |
| 둘째 그림(새 칸) | 특성 아이콘(Q-TV1) | `TraitVisual.Icon` |
| 제목 | 특성 이름 | `TraitVisual.DisplayName` |
| 첫 줄 | 가격 | `Price` |
| 둘째 줄 | "유닛 이름\n랭크 1 → 2" | `CurrentRank`·`NextRank` |
| 버튼 | "선택한다" / 영수증과 같은 카드면 "구매 완료" | `TrainingReceipt`의 `OwnedId`·`TraitKey` |
| 누를 수 있나 | `!shop.HasPurchase` | Q-TV3 |

특성 외형 행이 없으면 예외로 멈춘다(증강 팝업 Q-TG4 A와 같음). 문구는 `ShopPopupText`에 둔다. 다음 랭크의 효과 수치는 계약에 없어 보여 주지 않는다.

카드 프리팹은 `StageEventOfferDescriptionCard`다(11-02 사용자). `ShopUI.prefab`의 `cardPrefab`이 이미 이것을 가리키고 그림·제목·첫 줄·설명 칸이 모두 연결돼 있어 프리팹 변경이 없다. 회복·증강도 같은 카드다. `completedMarker`가 비어 있어 완료 표시는 버튼 문구 "구매 완료"만 나온다. 설명 칸에 `TraitVisual.Description`은 넣지 않는다. 표의 고정 문구라 랭크가 오른 수치와 어긋날 수 있다. `ShopUI`의 카드 간격은 144이고 카드 폭 480이라 네 장부터 넘칠 수 있다(스크롤은 §16.7).

**TV3 카드 항목이 훈련 대상을 기억한다.** `ShopOfferCardEntry`에 `OwnedId`·`TraitKey`·`ExpectedRank`와 생성 함수 `ForTraining`을 더한다. 일반 상품은 지금 생성자를 그대로 쓴다. `ShopUI`는 `_renderedOfferIds`를 항목 목록으로 바꾸고 `RequestCard`에서 갈라 부른다. `IShopPopupInput`에 `PurchaseTraining(context, ownedId, traitKey, expectedRank)`를 더한다. `expectedRank`는 그린 카드의 `CurrentRank`다.

비교한 안: 화면이 카드 번호만 넘기고 컨트롤러가 명령으로 바꾸는 안은 `ShopUI` 분기가 없어지지만, 컨트롤러가 그린 목록의 사본을 들어야 하고 "화면이 그린 문맥·ID를 넘긴다"는 지금 계약을 바꾼다. 카드 종류가 더 늘면 다시 본다.

**TV4 컨트롤러는 결과 갈래만 넓힌다.** `IShopPopupInput.PurchaseTraining`은 지금 `Submit`으로 제출한다. `MatchesResult`는 대기 명령 종류별로 맞추고, 훈련은 `Kind`·`OwnedId`·`TraitKey`·`ExpectedRank`·`PurchaseResult != null`을 본다. `IsMismatch`와 `HandleResult`의 구매 판단은 `result.Kind != ShopCommandKind.Complete` 하나로 바꾼다. 제출 잠금·영수증·닫기 흐름은 그대로 쓴다.

**TV5 거절 분류는 고치지 않는다.** `InsufficientFunds`·`PurchaseAlreadyUsed`는 이미 정상 거절이다. `StaleRank`·`StaleRoster`·`RankLimitReached`·`TraitNotTrainable`은 `ClassifyPurchase`의 기본값인 상태 불일치로 간다. 팝업을 닫고 맵을 새로 고치며 로그를 남긴다. 이 넷은 게시된 목록이 낡았다는 뜻이고 Core가 방문 중 목록을 다시 게시하지 않아, 정상 거절처럼 팝업에 남기면 같은 거절이 반복된다. `IsUnexpected`에도 넣지 않는다(F1로 멈출 일이 아니다).

**TV6 나가기와 재시도는 지금 경로다.** 사지 않고 나가기는 `Complete`, 구매 성공 뒤 완료 실패는 "구매 완료" 안내와 나가기 버튼 재시도다. 다시 결제하지 않는다. 목록이 비어도 카드 없이 나가기만 보인다.

**TV7 조립은 두 줄이다.** `ShopPopupViewFactory` 생성 인자에 `ITraitVisualTableQuery`를 더하고 `TrainingChoiceContentBuilder`를 만들어 모델 빌더에 넘긴다. `StageMapSceneFactory`는 이미 있는 `services.TraitVisuals`를 넘긴다. 새 서비스 등록은 없다.

**TV8 확인은 로컬 값으로 한다.** 재생 확인 때만 `Enhancement_Default`에 임시 가격(10 / 20), `StageRootDataTable`에 `Stage_Enhancement`, `StageMap_Chapter1` 강화 분포에 최소 1을 넣는다. 이 셋은 **커밋하지 않는다.** 스크롤 없는 화면이 운영 맵에 나오면 안 되고 가격은 밸런스 결정 전이다. 노드 외형은 `NodeVisual_Shop_Enhancement`가 이미 있다.

### 16.4 정할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-TV1 | 카드 그림 자리 | 11-02 사용자: 유닛 얼굴과 특성이 둘 다 보여야 한다. 카드 그림은 지금 한 칸이다. **A 큰 그림 = 유닛 얼굴, 새 작은 칸 = 특성 아이콘.** `StageOfferCardContent`에 그림 칸 하나, `StageOfferCardView`에 선택 참조 `secondaryIcon` 하나를 더하고(비어 있으면 안 그림, 다른 카드 화면 그대로), Image 추가·연결은 UI 작업자가 한다. 연결 전에는 유닛 얼굴과 특성 이름만 보인다. B 훈련 전용 카드 프리팹을 새로 만든다(칸 구성이 자유롭지만 카드가 둘이 되고 `ShopUI`가 방문 종류별로 프리팹을 골라야 한다) |
| Q-TV2 | 구매 성공 표시 | **A 일반 상점처럼 바로 닫는다.** 안내는 UI 작업과 함께 정한다. B 공용 안내 글자로 "랭크 1 → 2, 10G" 표시(StageMap에 새 의존이 생김). 인계 문서는 영수증 표시를 요구하므로 A여도 UI 단계에서 답해야 한다 |
| Q-TV3 | 골드가 부족할 때 버튼 | **A 켜 두고 Core 거절 안내.** 일반 상점 규칙과 같다(§4). B `CanPurchase`로 끈다 |

### 16.5 원칙 점검

자체 점검이고 측정값이 아니다. 예상 약 8.6.

| 기준 | 점검 |
|---|---|
| SSOT | 가격·랭크·구매 가능·영수증은 Core 게시값만 읽는다. 이름·얼굴은 View 표다. 랭크 계산과 가격 복사가 없다. 대상 값은 Core 선택지 → 카드 항목 → 명령으로 흘러 사본이 아니라 "그린 값"이다 |
| SOLID | 카드 값 만들기는 새 클래스, 판단은 Core, 제출은 컨트롤러로 나뉜다. 남는 점: 카드 종류가 늘면 `ShopOfferCardEntry`·`ShopUI` 분기를 함께 고친다(비교한 안 참고). `IShopPopupInput` 구현자는 하나다 |
| JMH/FW | 새 서비스·모듈·Tick·UI 등록이 없다. 명령은 기존 Buffer·Job, 화면은 `UISystem`에 등록된 `ShopUI` 그대로다. View는 InGame SO를 읽지 않는다(P6). 입력은 기존 `IUIInputReceiver` 경로다 |
| 기능 독립성 | 일반 상점 동작은 변경 없음이 목표다. 닿는 곳은 빌더 분기, 항목 필드, `ShopUI` 두 곳, 컨트롤러 세 곳이다. 회복·증강·덱은 읽지 않아도 된다. 훈련 카드 문구 변경은 새 클래스와 `ShopPopupText`만 본다 |

### 16.6 Phase(안)과 확인

| Phase | 내용 | 끝나면 |
|---|---|---|
| TV-a | 결과 경로: 입력 계약, 항목 필드, `ShopUI`, 컨트롤러 | 일반 상점이 전과 같은지 확인 |
| TV-b | 카드 값: 새 클래스, 빌더 분기, Factory 두 줄, 문구 | 훈련 방문에 카드가 나오는지 확인(TV8 값) |
| TV-c | 문서: UI 작업자 공유 메모, 천명님 인계 답, 계획 결과 요약 | 전달은 사용자 |

컴파일과 재생은 사용자가 Unity에서 확인한다. 새 시험 코드는 없다.

재생 확인(TV8 값으로):

1. 일반 유닛·아이템 상점의 구매와 나가기가 전과 같다.
2. 훈련 방문에 메인과 덱 밖 서브의 특성이 모두 나온다.
3. 구매하면 골드가 줄고 랭크가 오르며 노드가 완료된다.
4. 골드가 부족하면 거절 안내가 나오고 팝업이 남는다.
5. 사지 않고 나갈 수 있다. 선택지가 없어도 나갈 수 있다.
6. 오른 랭크가 다음 전투에 반영된다.
7. 랭크 3이 된 특성은 다음 방문 목록에 없다.
8. 낡은 목록 거절(TV5)로 팝업이 닫힌 뒤 노드를 다시 눌러 들어갈 수 있다. 코드만으로 확인하지 못해 확인 대상이다. 지금 화면 흐름에서 방문 중 보유 목록이 바뀌는 길은 찾지 못해 재현이 어려울 수 있다.

### 16.7 UI 작업자에게 넘길 것

- 훈련 목록은 길이 제한이 없어 상점 카드 영역에 스크롤이 필요하다. `StageOfferPopupUI`를 회복과 함께 쓰는 점을 고려한다.
- 특성 아이콘 칸: `StageEventOfferDescriptionCard`에 Image를 더하고 `secondaryIcon`에 연결한다(Q-TV1 A). 특성 외형 42개 중 8개는 아이콘이 비어 있다(`ChainExplosion`·`HuntersMark`·`PositionSwap`·`Sacrifice`·`Pull`·`DoorBreaching`·`DoubleShot`·`LuckyBullet`). 그 특성이 훈련 목록에 나오면 작은 칸이 비고 이름만 보인다.
- 시설 제목: `ShopUI.prefab`에 제목 글자가 없다. 용병 영입과 훈련소를 구분할지 정한다.
- 구매 성공 표시(Q-TV2), 카드 문구·배치, 유닛·덱 카드의 랭크 표시.
- `ShopUI.cs`·`ShopOfferCardEntry.cs`·`IShopPopupInput.cs`·`StageOfferCardContent.cs`·`StageOfferCardView.cs`를 조금 고친다. 커밋 순서를 맞춘다.

| 사람 | 닿는 곳 | 처리 |
|---|---|---|
| UI 작업자(현강님) | 위 세 파일 | 공유 메모(TV-c). 프리팹은 바꾸지 않는다 |
| 천명님 | 없음(Core·Buffer 무변경) | 인계 문서의 View 작업 진행 상태만 알린다 |
| 민형님 | 없음 | `StageMapSceneFactory`는 스테이지 맵 조립이고 Framework 계약을 바꾸지 않는다 |
