# 이벤트 스테이지 공통 화면·훈련소 — 연결 줄 공유 (민형님)

- 작성자: 호진(이호진)
- 작성: 2026-11-02, 호진(이호진) — 본문 초안은 Claude
- 상태: 미전달. 기능·계약은 바꾸지 않고 우리 화면을 잇는 줄만 고쳤습니다.
- 설계: [공통 레이아웃 설계](../specs/2026-11-02-stage-offer-common-ui-design-Hojin.md), [훈련소 View 연결 설계](../specs/2026-11-02-stage-training-view-design-Hojin.md)

## 1 View DB 새 표 `StageOfferText`

| 파일 | 줄 |
|---|---|
| `ViewTableKey` | 새 키 |
| `ViewDataBase` | 필드·노출·등록 셋 |
| `ViewDataBaseFactory` | 표 생성·연결 둘 |
| `DataBaseSO/ViewGroup/ViewGroup.asset` | 표 한 줄 |

코드와 `DataBaseSO/ViewGroup/StageOfferText/`, `ViewGroup.asset`은 한 커밋이어야 합니다. 에셋이 없으면 부트의 DB 생성이 멈춥니다.

## 2 StageMap 조립

| 파일 | 줄 |
|---|---|
| `StageMapViewServices` | `OfferTexts` |
| `StageMapViewSystem` | 표를 읽어 서비스 묶음에 넘김 |
| `Factory/Runtime/InGame/View/StageMapSceneFactory.cs` | `ShopPopupViewFactory`·`RecoveryPopupViewFactory`·`TraitGrowthPopupViewFactory`에 `services.OfferTexts`, `ShopPopupViewFactory`에 `services.TraitVisuals` |
| `LifeCycles/Stage/IStageTraitGrowthFacts`·`StageLifeCycle`·`StageProgress` | 읽기 사실 `IsGrowthStopped` 한 줄씩. `LifeCycleSystem/Base`는 안 건드렸습니다 |

기존에 있던 특성 외형 표를 상점 쪽에서도 읽을 뿐이고, 새 키는 없습니다.

## 3 Render·GlobalSystem

고치지 않았습니다.

## 4 데이터 (임시 값)

- `StageMap_Chapter1`: `depthCount` 5 → 6, `enhancementDistribution` 가중치 7·최소 1. 최소 개수 합이 중간 층 수를 넘을 수 없어 깊이를 늘렸습니다. 맵 화면에 한 층이 더 들어오는지는 Unity 확인 대기입니다.
- `StageRootDataTable`: `Stage_Enhancement`(1202) 등록.
- `Enhancement_Default`: 가격 300·500.

맵 카메라나 노드 간격을 만지신다면 깊이 6 기준으로 봐 주세요.

## 5 UI

`StageOfferUI.prefab`에 `ScrollRect`·`RectMask2D`를 쓴 칸이 생겼습니다(GlobalSystem 코드 변경은 레이아웃 부품뿐). 자세한 것은 [UI 공유](2026-11-02-stage-offer-ui-share-Hojin.md).
