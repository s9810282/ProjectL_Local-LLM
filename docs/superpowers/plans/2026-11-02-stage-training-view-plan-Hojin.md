# 훈련소 View 연결 Task 계획

- 작성자: 호진(이호진) — 본문 초안은 Claude
- 설계: [훈련소 View 연결 설계](../specs/2026-11-02-stage-training-view-design-Hojin.md)
- 상태: TU-a~TU-c 코드 완료, TU-d 문서·공유 완료(2026-11-02). 컴파일·재생은 사용자 확인 대기, 커밋 전, 공유 미전달.

## 1 진행 규칙

Phase마다 멈추고 보고했다. 새 시험 코드 없음, MSBuild 없음, 보고에는 컴파일 미확인을 적었다. 커밋·push는 사용자가 한다.

## 2 Phase 요약

| Phase | 내용 | 비고 |
|---|---|---|
| TV-a | Core 세 장 뽑기 | TU-a에서 지움 |
| TV-b | 가격·표 등록·맵 분포·깊이 6 | 임시 값 |
| TV-c·TV-d | 훈련 카드·구매 결과 | 지금 구조의 바탕 |
| TV-d2 | 등급 수치 글 | Core 스냅샷 + 특성 외형 표 17개 |
| TU-a | Core 세 장 뽑기 지우기 | 유닛 먼저, 특성 3개 이하 |
| TU-b | `CardViewport`·카드 7장·`FitScroll` | 스크롤만 |
| TU-c | 유닛 → 특성 두 단계·뒤로, 검토 M1·M2·L1·L4·L7·L8 | Core 특성 글·`TrainingPurchase` |
| TU-d | 문서·공유·재검토 | 아래 |

**커밋 때 함께:**
- 새 `Shop/TrainingCardContentBuilder.cs` + `.meta`.
- 레이아웃 코드와 `StageOfferUI`·`TraitUI`·`StageEventOfferCard` 프리팹(필수 칸 `exitLabel`이 없으면 StageSelect 진입이 멈춘다).
- 글 표 코드와 `StageOfferText_*` 에셋.
- 특성 외형 표 코드와 수치 글 에셋 17개. 그 에셋들에는 이번 작업 전부터 다른 변경이 있었다.
- 지금 트리의 `Tool/Editor/`·`SO모드 사용법.md` 변경은 이 작업이 아니다.

## 3 실행 기록

### TU-d 문서·공유 (2026-11-02, 사용자 "TU-d 진행", 코드 변경 없음)

- 설계를 최종 구조로 다시 씀. 공유 셋 고침(천명님 공유 다시 씀, UI 메모, 민형님 메모). 전달은 사용자.
- 재검토는 새 에이전트 하나, 대상 TU-a~TU-c 코드. 약 8.8/10, 높음 0·중간 1·낮음 7. 실행 확인은 없음. 코드 대조로 확인한 것은 F1·F2·F3·F4·F6이다. 코드는 고치지 않았다(결정 대기).
  - **F1 중간, 반영(사용자 "0.2초 입력 잠금", 컴파일 미확인, 새 시험 없음):** `ShopPopupController.SelectCard`가 유닛 선택 뒤 바로 `Redraw`하고 잠금이 없어, 더블클릭의 두 번째 클릭이 같은 자리의 특성을 샀다. `Redraw`가 0.2초 잠금을 걸고 `SelectCard`·`Exit`가 그동안 입력을 버린다. 뒤로 버튼 더블클릭이 떠나기가 되는 것도 함께 막는다. 일반 상점·회복은 단계 이동이 없어 그대로다. 값은 코드 상수다.
  - **F2 낮음:** 단계가 바뀌면 키보드·패드 초점이 꺼진 카드에 남을 수 있고, 스크롤이 초점을 따라가지 않는다.
  - **F3 낮음:** 작업 트리에서 `StageEventOfferCard`의 `Icon` 폭이 275 → 220이다. 용병·회복·증강 카드에도 적용된다. 이번 작업에서 넣은 값이 아니고 사용자 확인 대상이다.
  - **F4 낮음:** "다시 연결돼도 같은 화면"은 완료 실패 때만 맞다. 설계 TV4는 고쳤고 `ShopPopupModelBuilder.cs:145` 주석은 남았다.
  - **F5 낮음:** `TrainingReceipt`·`TrainingPurchase`가 따로이고 둘이 함께 있는지 검사하지 않는다. 지금은 닿지 않는 경로다.
  - **F6 낮음:** 낡은 글 둘(`StageOfferCardView.cs:17` 툴팁, `ShopPopupModelBuilder.cs:22` 주석).
  - **F7 낮음:** 카드 7장은 보유 한도의 사본이라, 한도가 늘면 플레이 중 예외로 멈춘다.
  - **F8 낮음:** 모델 생성과 단계 상태가 `ShopPopupModelBuilder` 한 클래스에 있다. 카드 사이 빈틈은 드래그가 먹지 않는다.

### TV-a 코드 완료 (2026-11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

- 사용자 "바로 TV-a". Ta.1~Ta.5·Ta.7 완료, Ta.6은 Q-TC3 A라 하지 않았다.
- 바꾼 파일: `TrainingRules`·`ShopService`·`ShopEntryPreparation`·`CurrentProgressBuffer`·`CurrentShopData`. 기록은 Core 전달 문서 §5.78.
- 계획과 다른 점: 복사·검사 함수를 `CurrentShopData.CopyTrainingOffers` 하나로 두고 준비 값이 그것을 부른다(`CopyOffers`와 같은 꼴).
- 번호: CLAUDE.md는 증강 Legacy 삭제를 Core 전달 문서 §5.77로 적었는데 그 문서에 §5.77이 없다. 겹치지 않게 §5.78로 적었다.
- 화면 변화 없음. View는 아직 `TrainingOffers`를 읽지 않는다(TV-c).
- Ua(사용자): 실패 셋 `RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases`·`ProductionRestVisit_EntersSelectsAndCompletes` 두 사례. TV-a와 무관하다고 판단(코드·에셋 대조, 오류 문구는 못 봄): 시험은 회복 `2, 4`·용병 ShotGun 하나를 기대하는데 에셋은 회복 `1, 2, 4`·용병 넷이다. 사용자 결정으로 시험·에셋 모두 그대로 둔다.

### TU-c 유닛 → 특성 두 단계 코드·에셋 완료 (2026-11-02, 사용자 "TU-c 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uc 대기)

- 유닛 단계: `Owned` 전부를 카드로(초상화·이름·`Lv`·훈련 가능 특성 이름). 특성이 없으면 "훈련 불가"로 잠긴다.
- 특성 단계: 고른 유닛의 `TrainingChoices` 전부. 버튼은 "뒤로"(명령 없이 유닛 단계로). 고른 유닛은 모델 빌더의 화면 상태이고 방문이 바뀌면 비운다.
- 구매 뒤: 카드는 특성 키 순이라 자리가 그대로이고 산 카드는 `CurrentShopData.TrainingPurchase`(구매 직전 값)로 그린다. 버튼은 "떠난다". 영수증이 Buffer에 있어 다시 연결돼도 같은 화면이다.
- 카드 `Details`는 수치 줄 + 설명 두 줄로 줄였다(유닛 이름은 화면 설명 줄 "{0}의 특성을 강화합니다"로 옮김, L3 완화).
- 검토 반영: M1 특성 이름·설명을 Core 글로(`TrainingChoiceSnapshot.Name`·`Description`), M2 증강 멈춤을 화면이 오류로 보고(`IStageTraitGrowthFacts.IsGrowthStopped`, 컨트롤러 Tick), L1 떠난다·뒤로 글을 SO로(`exitButtonText`·`backButtonText`, 레이아웃 필수 칸 `exitLabel`, 두 프리팹 연결), L4 구매 뒤 일반 상점 명령 비움, L7 `secondaryText` 필수, L8 툴팁. L6 등급 갈래도 없어졌다.
- 그대로: L2·L5·L9, L10(맵 노드 그림, 사용자 결정 대기).
- 계획과 다른 점: 산 카드 값을 View가 기억하지 않고 Buffer `TrainingPurchase`로 둬 Core·Buffer가 조금 늘었다. 기록은 Core 전달 문서 §5.80.
- 남은 것: 설명 글의 숫자는 Core 글도 1등급 값이라 2등급 카드에서는 수치 줄과 다르다. 숫자를 뺄지 사용자 결정 대기.

### TU-c 재생 보고: 그림 위에서만 끌림 (2026-11-02, 프리팹 수정, Unity 확인 대기)

- 보고(사용자): 아이콘 쪽을 눌러야 스크롤이 된다. 원인: 카드 루트 Image의 `Raycast Target`이 꺼져 있어 `IconBackground`·`ItemIcon`·버튼만 포인터를 받았다.
- 고침: `StageEventOfferCard.prefab` 루트 Image `Raycast Target` 켬. 카드 전체에서 끌기·휠이 된다. 카드 사이 빈틈은 그대로다(`CardViewport`에 투명 Image를 넣으면 겹치는 `ExitButton` 위쪽을 가린다).

### TU-b 스크롤·카드 7장 코드·프리팹 완료 (2026-11-02, 사용자 "TU-b 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- `StageOfferUI.prefab`: 새 `CardViewport`(1400×600, 옛 `Cards` 자리, `RectMask2D`·가로 전용 `ScrollRect` Clamped, 스크롤바 없음, 휠 감도 40)가 `Cards`를 품는다. `Cards`는 `(0, 0)`이 되고 배율 0.8은 그대로다. 카드 `OfferCard3`~`OfferCard6`을 더해 7장이고 레이아웃 `cards`에 이었다. `Panel`·글·골드·떠난다·`ErrorPanel`은 무변경이다.
- `StageOfferLayoutView`: 선택 칸 `cardScroll`(`TraitUI`는 비움), `FitScroll`이 카드 폭 합과 Viewport 폭을 비교해 넘칠 때만 스크롤을 켜고, 안 넘치면 가운데로 되돌린다. 카드 수가 같으면 보던 자리를 유지한다. 구성 검사에 Content·Viewport·카드 부모.
- 세 장 폭은 1248(480×3 + 60×2, 배율 0.8)이라 Viewport 1400 안에 들어가 스크롤이 꺼진다. 네 장(1680)부터 켜진다.
- 계획과 다른 점: `CardViewport`에 투명 Image를 넣지 않았다. 카드 위에서만 끌기·휠이 먹고 카드 사이 빈틈은 안 먹는다(떠난다 버튼을 가리지 않게).

### TU-a Core 뽑기 지우기 코드 완료 (2026-11-02, 사용자 "TU-a", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 지움: `TrainingRules.PickOffers`, `ShopEntryPreparation.TrainingOffers`·인자, `CurrentShopData.TrainingOffers`·`CopyTrainingOffers`, `ShopService`·`CurrentProgressBuffer`의 전달 줄. 등급 수치(§5.79)는 그대로다.
- `ShopPopupModelBuilder`는 임시로 `TrainingChoices`를 그대로 그린다. **TU-c 전까지 훈련 쌍이 세 개를 넘는 방문은 카드 넘침 예외로 멈추고, 구매 뒤 카드 자리·산 카드 수치도 맞지 않는다.** 화면 확인은 TU-c 뒤에 한다.
- 사용자 지시: 프리팹은 Claude가 넣는다. 스크롤은 스크롤만이다(스크롤바 등 다른 것을 더하지 않음).

### TV-b 코드·에셋 완료 (2026-11-02, 컴파일 미확인, 새 시험 없음, Ub 대기)

- 사용자 "임시 300·500으로 TV-b". Tb.1 `Enhancement_Default` 300·500(임시), Tb.2 `Stage_Enhancement` 표 등록 완료.
- **Tb.3 계획 값이 잘못됐다.** `StageMapData.Validate`가 최소 개수 합 ≤ `depthCount - 2`를 요구한다. 지금 깊이 5(중간 층 3)에 전투·상점·회복이 1씩이라 훈련 최소 1이면 합 4로 부트 DB 생성이 멈춘다. 설계 때 이 검사를 보지 못했다. 그래서 가중치 7·**최소 0**으로 두었고, 그 값의 훈련소 등장은 Run의 약 19%였다(중간 칸 수 분포와 가중치 7/103으로 계산).
- Q-TV7 A(사용자 "깊이 6, 최소 1"): `StageMap_Chapter1` `depthCount` 5 → 6(임시), 훈련소 가중치 7·최소 1(임시). 네 종류가 매 Run 보장된다. Tb.3 완료. 깊이 값에 기대는 View·Core 코드는 찾지 못했고, 맵 화면에 다 들어오는지는 미확인이다.
- Tb.4: 훈련 가능 특성 17개 모두 특성 외형 표에 있다. `PositionSwap`만 아이콘이 없어 빈 그림으로 나온다.
- 사용자 재생: 훈련소 노드가 나온다. 맵 크기·다음 노드 항목의 결과는 아직 받지 못했다.

### TV-c 코드·에셋 완료 (2026-11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uc 대기)

- 사용자 "TV-c 진행". Tc.1~Tc.6 완료. 새 `Shop/TrainingCardContentBuilder.cs`(`.meta`는 Unity가 만든다, 커밋 때 함께).
- 계획과 다른 점: 훈련 대상 유닛이 보유 목록에 없으면 대상을 적은 예외로 멈춘다(계획은 특성 외형 행만 적음).
- `OfferIds`는 사라졌고 일반 상점도 `TryGetCommand`로 제출한다. 일반 상점 동작은 같다.
- `StageMapSceneFactory` 생성 줄 인자 하나(민형님 공유는 TV-e).
- **TV-d 전이라 훈련 카드를 누르면 결과를 받지 못해 화면이 제출 대기로 멈춘다.** 확인 때 누르지 않는다.

### TV-d 코드 완료 (2026-11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ud 대기)

- 사용자 "TV-d 진행". Td.1 `MatchesResult`가 훈련이면 `OwnedId`·`TraitKey`·`ExpectedRank`까지 대조한다. Td.2 `IsMismatch`·`HandleResult`는 완료가 아닌 결과를 구매로 함께 받는다. Td.3 `ShopResultPolicy`에 훈련 사유 넷을 상태 불일치로 적었다(동작은 기본 갈래와 같음).
- 계획과 다른 점 없음. 구매 뒤 떠난다까지 남는 규칙(SO7)은 일반 상점과 같은 길이다.
- 남은 것은 TV-e로 넘겼다.

### TV-d2 등급 수치 표시 코드·에셋 완료 (2026-11-02, 계획 밖 사용자 요청, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ud 대기)

- 사용자 "현재 수치 +a" → 권장 카드 찬성(지금 카드 칸만, 추가는 상의) → "A 특성마다 글". 설계 TV8.
- Core: `TraitGrowthDefinition.ValuesAt`(`ResolveRank`도 이것을 부름), `TrainingChoiceSnapshot.CurrentValues`·`NextValues`(끝 선택 인자), `TrainingRules.Choices`가 채움, `CurrentShopData` 구매 복사. 기록은 Core 전달 문서 §5.79.
- View DB: `TraitVisualData.rankValueFormat`·`RankValueFormat`·꼴 검사, `TrainingDetails` 값 셋, `StageOfferText_Training` 꼴.
- 카드: `TrainingCardContentBuilder.ValueLine`. 프리팹 변경 없음.
- 에셋: 훈련 가능 특성 17개에 글 초안(사용자 확인 대상). 낱말은 설명 글의 용어를 따랐다(방어막이 아니라 보호막).
- 미확인: `Details` 칸 348×112에 세 줄이 들어가는지, 글꼴에 `→`가 있는지.
