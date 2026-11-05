# 훈련소 View 연결 설계

- 작성자: 호진(이호진) — 본문 초안은 Claude
- 상태: 구현 완료(TV-b·TV-c·TV-d·TV-d2·TU-a~TU-c, 2026-11-02). 컴파일·재생은 사용자 확인 대기, 커밋 전, 공유 미전달.
- 계획: [훈련소 View 연결 Task 계획](../plans/2026-11-02-stage-training-view-plan-Hojin.md)
- 바탕: [공통 레이아웃 설계](2026-11-02-stage-offer-common-ui-design-Hojin.md), [Core → View 인계](../handoffs/2026-11-02-run-gold-facilities-trait-ranks-core-view-handoff.md)
- 이 문서는 3판이다. 1판 페이지 넘김과 2판 Core 세 장 뽑기는 사용자 결정으로 버렸다(§6).

## 1 요구 (11-02 사용자)

- 훈련소는 강화 상점과 비슷하지만 메인과 서브를 모두 강화한다. 지금 가진 유닛을 고른 다음 특성을 강화한다.
- 유닛은 세 장씩이 아니라 **전부** 보여 준다. 훈련할 특성이 없는 유닛은 보이되 잠근다.
- 카드 뒤 프리팹은 재사용한다. 카드가 많으면 스크롤하되 **스크롤만** 한다(스크롤바 없음).
- 훈련 카드에는 `ItemIcon`·`Price`·`TraitNew`가 모두 나오고, 지금 수치에서 오르는 양이 보인다.

## 2 화면 흐름

| 단계 | 카드 | 버튼 | 화면 설명 줄 |
|---|---|---|---|
| 유닛 | 보유 유닛 전부. 특성이 없으면 잠금 | 떠난다 | `eventDescription`. 훈련 대상이 아무도 없으면 `TrainingEmpty`를 덧붙임 |
| 특성 | 고른 유닛의 훈련 가능 특성 전부 | 뒤로(명령 없음) | `TrainingTraitPrompt`(유닛 이름) |
| 구매 뒤 | 산 카드 완료, 나머지 잠김, 자리 그대로 | 떠난다 | 같음 |

방문마다 구매 한 번, 사지 않고 떠나기 가능, 후보 없는 방문도 정상이다(인계 문서 그대로). 구매 뒤 떠난다까지 화면이 남는 규칙은 공통 레이아웃 SO7이다.

## 3 설계

**TV1 상점 길을 그대로 쓴다.** 훈련은 Core에서도 상점 방문의 한 종류다(`ShopVisitKind.Training`). `ShopPopupController`·`StageOfferUI`·SO `StageOfferText_Training`을 쓰고 새 컨트롤러·화면을 만들지 않는다.

**TV2 후보는 `TrainingChoices`이고 View가 유닛별로 묶는다.** 뽑거나 자르지 않는다. 규칙은 Core에만 있다.

**TV3 카드 칸.** 유닛 카드도 같은 `StageEventOfferCard`다. 값이 빈 칸은 끄는 규칙(공통 레이아웃 R2)을 그대로 쓴다.

| 칸 | 유닛 카드 | 특성 카드 |
|---|---|---|
| `Icon` | 유닛 초상화 | 특성 아이콘(없으면 빈 그림) |
| `ItemName` | 유닛 이름 | Core 특성 이름 |
| `ItemIcon`·`Price` | 꺼짐 | SO 가격 꼴 + Core 가격 |
| `TraitNew` | `Lv. {0}` | `{0} → {1}등급` |
| `Details` | 훈련 가능한 특성 이름들 / "훈련할 특성 없음" | 등급 수치 글 / Core 특성 설명 |
| 버튼 | 선택한다 / 훈련 불가 | 선택한다 / 구매 완료 |

**TV4 구매 뒤 화면.** 카드는 특성 키 순이라 자리가 움직이지 않는다. 산 특성은 구매 뒤 `TrainingChoices`에서 등급이 오르거나 빠지므로 `CurrentShopData.TrainingPurchase`(구매 직전 값)로 그린다. 영수증과 함께 Buffer에 있어, 구매는 됐는데 방문 완료가 실패한 경우 다시 열어도 같은 화면이다. 정상 구매 뒤 다시 연결하면 노드가 이미 클리어라 화면을 그리지 않고 닫는다(플레이어는 갇히지 않는다).

**TV5 고른 유닛은 화면 상태다.** `ShopPopupModelBuilder`가 갖고 방문(`EntryId`)이 바뀌면 비운다. 영수증이 있으면 그 유닛의 특성 단계에 머문다. 유닛 고르기와 뒤로는 Core 요청이 없다. 단계가 바뀌면 0.2초 동안 카드·나가기 입력을 버린다(사용자 결정). 더블클릭의 두 번째 클릭이 같은 자리의 특성을 사거나 떠나기가 되지 않게 하려는 것이고, 버튼 모습은 바꾸지 않는다.

**TV6 제출과 결과.** 카드 번호 → 명령은 모델 빌더의 `TryGetCommand`다. `MatchesResult`는 훈련이면 `OwnedId`·`TraitKey`·`ExpectedRank`까지 맞춘다(인계 문서 그대로). 골드 부족은 눌러서 `ErrorPanel` 안내를 본다(Q-TV6). `StaleRank`·`StaleRoster`·`RankLimitReached`·`TraitNotTrainable`은 상태 불일치로 닫고 맵을 갱신한다.

**TV7 스크롤.** `StageOfferUI`의 `Cards`를 새 `CardViewport`(1400×600, `RectMask2D`, 가로 `ScrollRect`, Clamped, 스크롤바 없음)가 품고, 카드를 7장으로 늘렸다. 보유 유닛은 최대 7명(`CharacterRosterRules`: 정의당 1, 서브 병과당 2 × 3 + 메인 1)이라 런타임 생성이 필요 없다(R7). `StageOfferLayoutView.FitScroll`이 카드 폭 합과 칸 폭을 비교해 넘칠 때만 켠다. 세 장은 1248폭이라 꺼지고 네 장(1680)부터 켜진다. 카드 수가 같으면 보던 자리를 유지한다. `TraitUI`는 칸을 비워 그대로다.

**TV8 등급 수치 글.** Core는 등급마다 절대값을 갖는다(1등급 = 기본값, 2·3등급 = `TraitGrowth` 에셋). `TrainingChoiceSnapshot`이 지금·다음 값(`CurrentValues`·`NextValues`)을 주고 View는 빼기만 한다. 낱말·%는 특성 외형 표의 `rankValueFormat`(특성마다)이다. `{0}` 지금, `{1}` 오르는 양, `{2}` 다음, 값이 둘인 특성은 `{3}`·`{4}`·`{5}`. 에셋 글만으로 `보호막 {0} +{1}`처럼 바꿀 수 있다. 글이 비면 그 카드를 그릴 때, 꼴 오류는 부트 DB 생성에서 대상을 적은 예외로 멈춘다.

**TV9 나가기 글.** 떠난다·뒤로는 SO `exitButtonText`·`backButtonText`이고 레이아웃 필수 칸 `exitLabel`이 그린다.

**TV10 켜기(임시 값).** `Enhancement_Default` 300·500, `Stage_Enhancement` 표 등록, `StageMap_Chapter1` 깊이 6·훈련 가중치 7·최소 1. 맵 규칙이 최소 개수 합 ≤ 중간 층 수를 요구해 깊이 5로는 부트가 멈춘다.

## 4 Core·Buffer 변경 (사용자 지시, Core 전달 문서 §5.79·§5.80)

표시용 값만 더했고 규칙·명령·결과 계약은 그대로다.

| 파일 | 변경 |
|---|---|
| `TraitGrowthDefinition` | `ValuesAt(rank)`, `ResolveRank`가 부름(동작 같음) |
| `TrainingChoiceSnapshot` | `CurrentValues`·`NextValues`·`Name`·`Description`(끝 선택 인자) |
| `TrainingRules.Choices` | 위 값을 채움 |
| `CurrentShopData` | `TrainingPurchase`, `WithTrainingPurchase`가 지금 목록에서 찾아 넣고 없으면 첫 쓰기 전에 예외 |

인계 문서 `:72`가 다음 등급 수치는 Core 계약을 요청하라고 했고, 사용자 지시로 이 세션이 넣었다. 인계 문서와 다른 점은 없다.

## 5 남기는 것

- **특성 설명의 숫자:** Core 글은 1등급 값이라 2등급 카드에서는 수치 줄과 다르다. 설명에서 숫자를 뺄지 결정 대기.
- **`Details` 칸:** 348×112에 값 둘인 특성(Cohesion·TrackingShot)이 넘치는지 재생으로 본다. `→` 글꼴도 확인한다.
- **맵 노드 그림:** `StageNodeVisual`의 `stageKey`(1~4)가 실제 키와 달라 훈련소·용병이 같은 그림이다. 결정 대기.
- **다른 화면의 등급:** 유닛 정보창·전투 HUD는 등급 수치를 보이지 않는다.
- **첫 층 훈련소:** 시작 유닛 중 훈련 대상은 시험 값이 있는 ShotGun뿐이다. 그 값을 되돌리면 첫 층에서는 모두 잠길 수 있다(정상 방문).
- **`TrainingChoices` 계산:** 구매 때 목록을 다시 계산한다. 운영 비용 문제는 아니다.

## 6 버린 안 (이력)

- **페이지 넘김(1판):** 사용자가 세 장 제한을 먼저 고름.
- **Core 세 장 뽑기(2판, TV-a):** 방문 전체 세 장 → 유닛마다 세 장 → 삭제. 유닛을 먼저 고르고 모든 유닛의 특성이 3개 이하라(증강 두 번 + 기본 1, 11-02 사용자) 필요 없어졌다. 커밋 전에 넣고 뺐다.
- **유닛 칸 전용 프리팹:** 지금 카드와 스크롤로 대신했다.

## 7 원칙 점검

- **SSOT:** 후보·등급·가격·수치·특성 글은 Buffer의 Core 값 하나다. 특성 아이콘과 수치 낱말만 View 표다. 고른 유닛·스크롤 위치만 표시 상태다.
- **SOLID·기능 독립성:** 카드 내용은 `TrainingCardContentBuilder`, 단계는 `ShopPopupModelBuilder`, 스크롤은 레이아웃 한 곳이다. 글을 바꿀 때 코드를, 스크롤을 바꿀 때 상점 규칙을 보지 않는다. 용병·회복은 나가기 글 칸 외에 바뀌지 않는다.
- **FW·UI 규약:** 등록 화면 추가 없음. 버튼은 `UIInputBinding`, 수신은 레이아웃의 `IUIInputReceiver`. 필수 칸 누락은 예외이고 `cardScroll`은 명시적 선택 칸이다.
- **R7:** 카드는 프리팹에 미리 놓고 런타임에 만들지 않는다.
