# 이벤트 스테이지·증강 공통 화면 — UI 공유 (재령님·현강님)

- 작성자: 호진(이호진)
- 작성: 2026-11-02, 호진(이호진) — 본문 초안은 Claude
- 상태: 미전달. 코드는 일부 커밋됐고 훈련소 연결은 커밋 전이다.
- 설계: [공통 레이아웃 설계](../specs/2026-11-02-stage-offer-common-ui-design-Hojin.md), [훈련소 View 연결 설계](../specs/2026-11-02-stage-training-view-design-Hojin.md)
- 대신함: [증강 선택 화면 UI 공유(10-29)](2026-10-29-trait-growth-ui-hyeongang-share-Hojin.md)의 화면 설명

## 1 요지

용병·회복·훈련소는 `StageOfferUI.prefab`, 증강은 `TraitUI.prefab`을 씁니다. 코드는 넷 다 같은 부품 `StageOfferLayoutView`·`StageOfferScreenUI`입니다. **두 프리팹을 고칠 때 아래 칸 연결을 지켜 주세요.** 빠지면 StageSelect 진입이 예외로 멈춥니다.

## 2 프리팹이 어떻게 쓰이나

| 화면 | 프리팹 | 등록 | 골드·떠난다 |
|---|---|---|---|
| 용병·상점 | `StageOfferUI`(`screenId` Shop) | 씬 빈 오브젝트 + 자식 프리팹 | 보임 |
| 회복 | `StageOfferUI`(`screenId` Recovery) | 같음 | 보임 |
| 훈련소 | 상점과 같은 화면, 유닛 → 특성 두 단계 | 같음 | 보임 |
| 증강 | `TraitUI`(`TraitGrowth/TraitGrowthUI.prefab`의 자식) | `UIId.TraitGrowth`, Parallel | 꺼짐 |

- 카드는 **프리팹에 미리 놓고** 런타임에 만들지 않습니다. `StageOfferUI`는 7장(보유 유닛 최대 7명), `TraitUI`는 3장이고, 놓인 수를 넘으면 예외입니다. 레이아웃의 `cards` 목록이 모두 이어져 있어야 합니다.
- `StageOfferUI` 구조: `CardViewport`(1400×600, `RectMask2D`·가로 `ScrollRect`·스크롤바 없음) 아래 `Cards`(배율 0.8) 아래 `OfferCard0~6`. 코드가 카드 폭 합과 칸 폭을 비교해 **넘칠 때만** 스크롤을 켜고, 안 넘치면 가운데로 놓습니다. 세 장(1248폭)까지는 지금과 같습니다. `CardViewport` 폭을 1248보다 줄이면 세 장 화면이 잘립니다.
- 레이아웃 새 필수 칸 `exitLabel`(나가기 버튼 글), `TraitUI`에도 이어져 있습니다. 선택 칸 `cardScroll`(`TraitUI`는 비움).
- 레이아웃 선택 칸 `unitFace`(Image, 11-02 추가)는 `TraitUI`의 `Panel/UnitFace`에만 이어져 있습니다. 증강 대상 유닛의 얼굴(유닛 외형 `InfoIcon`, `Sprites/AlphaIcon/UI_head_*`)이 들어가고, 제안을 준비하는 동안이나 얼굴이 없으면 꺼집니다. `StageOfferUI`는 비워 둡니다. `TraitUI`에서 이 칸이 빠지면 증강 화면을 그릴 때 예외로 멈춥니다.
- 카드 루트 Image의 `Raycast Target`을 켰습니다. 카드 어디서든 끌어서 스크롤하려고입니다.
- `StageEventOfferCard.prefab`의 카드 칸: `icon`, `titleText`(ItemName), `primaryText`(Price), `secondaryText`(Details), `buttonText`, `actionButton`, `completedMarker`, **새 필수 칸 `kindText`(TraitNew)·`priceIcon`(ItemIcon)**.

## 3 카드 칸 규칙

값이 빈 칸은 코드가 끕니다. 화면 종류를 묻지 않습니다.

| 화면 | `ItemIcon`·`Price` | `TraitNew` | `Details` |
|---|---|---|---|
| 용병·회복 | 보임 | 꺼짐 | 상세 글 |
| 증강 | 꺼짐 | "신규 특성"·"특성 강화" | 특성 설명 |
| 훈련소 유닛 카드 | 꺼짐 | `Lv. 2` | 훈련 가능한 특성 이름들 |
| 훈련소 특성 카드 | 보임 | `1 → 2등급` | 수치 줄 / 특성 설명 |

훈련소 특성 카드의 `Details`는 값이 둘인 특성(Cohesion·TrackingShot)에서 길어집니다. 칸이 348×112이고 글자가 24에서 18까지 줄어듭니다. 넘치면 칸 크기를 조정하거나 호진과 상의해 주세요. `→` 글자가 글꼴에 있는지도 확인해 주세요.

## 4 글은 프리팹이 아니라 SO에서 옵니다

- 화면 이름·설명·버튼(떠난다·뒤로 포함)·가격 꼴·안내 글은 `DataBaseSO/ViewGroup/StageOfferText/`입니다. 프리팹에 글을 적어도 덮어써집니다. 예: `TraitUI` 프리팹의 "특성 선택"은 SO의 "증강 선택"으로 나옵니다.
- 훈련 카드의 수치 글은 특성 외형 표의 새 칸 `rankValueFormat`(훈련 가능 특성 17개, 초안)입니다. 특성 이름·설명은 Core 글입니다.
- 코드에 화면 글을 두지 않습니다. 새 글이 필요하면 SO 글 종류를 더합니다.

## 5 동작이 바뀐 것

- 구매·회복 선택 뒤 화면이 닫히지 않고 떠난다를 누를 때까지 남습니다. 카드는 잠기고 산 카드는 "구매 완료"입니다.
- 거절 안내는 `ErrorPanel`에 나오고 사라집니다. 코드의 옛 안내 글은 없습니다.
- 증강은 고르면 바로 다음 제안 또는 닫힘입니다. **결과 화면, "계속", 숨김, 다시 열기 버튼이 없어졌습니다.** 노드를 눌러도 증강이 남아 있으면 진입만 막힙니다.
- 증강에는 첫 카드에 키보드 초점이 갑니다. 상점·회복은 그대로입니다.

## 6 지운 것

- 프리팹: `ShopUI`, `RecoveryUI`(+ 빈 폴더 둘).
- 코드: `StageOfferPopupUI`, `RecoveryUI`, `ITraitGrowthPopupDisplay`, `ITraitGrowthPopupInput`, `ITraitGrowthReopenInput`, `TraitGrowthPopupModel`, `TraitGrowthPopupText`, `TraitGrowthPopupViewScreen`, `ShopPopupText`, `RecoveryPopupText` 등.
- 에디터 도구: `Editor/Stage/StageShopUISetup.cs`, `StageTraitGrowthUISetup.cs`. 프리팹은 이제 손으로 관리합니다.
- 옮김(GUID 유지): `ShopUI.cs` → `StageOfferUI.cs`.

## 7 값

- 카드 너비는 세 프리팹 모두 480으로 맞췄습니다(`StageOfferUI`의 `OfferCard0` 크기 덮어쓰기 제거, `StageEventOfferDescriptionCard` 489 → 480). `TraitUI` 카드는 재령님 값 560 그대로입니다.
- 필수 참조가 빠지면 조용히 넘어가지 않고 대상을 적은 예외로 멈춥니다.
