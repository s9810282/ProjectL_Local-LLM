# UnitCardUI 표시/조회 분리와 손패 카드 사용 안내 (2026-10-06)

- **받는 분:** 현강님(`UnitCardUI`·`UnitCard.prefab`·`DeckButtonUI` 작성). **상태:** 메모 준비만 했다. 미전달. 발송은 사용자가 한다.
- **왜:** 전투 손패를 3D 유닛 대신 현강님의 정보 카드 외형으로 보여 주기로 했다. 카드를 여러 장 띄우려면 `UIBase`(`UIId.UnitCard`)가 손패 카드마다 붙으면 안 된다. `UISystem.BindScene`이 씬 로드마다 비활성까지 UIBase를 찾아 ID로 등록하므로 중복 등록 예외가 난다. 그래서 표시와 조회를 나눴다.
- **설계 원본:** `docs/superpowers/specs/2026-10-06-hand-card-design.md`(사용자 결정 U1~U6, 구조 C1~C9).

## 1. 바뀐 구조

| 대상 | 역할 |
|---|---|
| `UnitCardView`(신규, `UISystem/Screens/Global/`) + `Prefabs/UI/UnitCard.prefab` | **표시 전용.** 기존 Inspector 참조 14개(이름 그대로)·`UnitCardVisualsSO`·Synergy/Function 패널 Hover/고정을 가진다. UIBase가 아니다. 이름 결정 순서(Visuals SO → UnitVisual 표 이름 → 키)와 능력치 문구 형식은 여기 한 곳에만 있다 |
| `UnitCardUI` + `Prefabs/UI/UnitCardUI.prefab`(UnitCard.prefab의 **Prefab Variant**) | **덱 정보 카드 조회.** Buffer·DB·보유 카드·영혼 보너스 조회와 `Update` 변경 비교는 그대로이고, 그린 값을 같은 오브젝트의 `UnitCardView`에 넘긴다(`RequireComponent`) |
| `DeckButtonUI` | 코드 변경 없음. `GlobalUICanvas`의 **Unit Card** 필드가 Variant `UnitCardUI.prefab`을 가리킨다 |
| 전투 `HandPresenter` | `UnitCard.prefab`(원본)을 여러 장 빌려 Buffer 값만 넘긴다. 패널 버튼은 숨기고, 배치 단계에서만 보인다 |

## 2. 그대로인 것

- `UnitCardUI` 공개 API: `SetCharacter`, `SetOwnedCharacter`, `SetLevel`, `Open`, `Hide`, `Refresh`, `ClosePanels`, Hover/Toggle 메서드, `CharacterId`, `OwnedCharacterId`.
- 덱 Hover 미리보기·Click 고정·패널 Hover/Click 동작. 패널을 열기 직전에 최신 값으로 다시 그리는 동작도 유지된다(`UnitCardView.PanelOpening` 이벤트).

## 3. 앞으로 작업할 때

- **카드 외형(레이아웃·이미지·글꼴)은 `UnitCard.prefab`만 고친다.** 덱 카드와 손패 카드에 함께 반영된다.
- 덱 카드에만 필요한 것은 Variant `UnitCardUI.prefab`에 더한다.
- 새 표시 필드는 `UnitCardView`에 직렬화 필드와 `Show…` 메서드로 더하고, 값은 `UnitCardUI`(덱)·`HandPresenter`(손패)가 각자 넘긴다. `UnitCardView`에서 Buffer·DB를 읽지 않는다.
- 테스트 `DeckButtonUITests`는 표시 필드를 `card.View`(UnitCardView)에서 설정하도록 바꿨다.

## 4. 아직 정하지 않은 것

- 손패 카드 Hover 때 정보 카드(덱과 같은 `UnitCardUI`)를 띄울지는 나중에 정한다(설계 U6).
- 손패 카드의 특성·시너지 내용은 패널을 숨겼으므로 채우지 않는다.
