# 전투 피격 외곽선 — 호진님 공유 (2026-11-11, 미전달)

전투 피격 플래시에 외곽선을 더하면서 호진님이 튜토리얼 강조로 넣은 `UnitModelAppearance`의 외곽선 층 줄을 고쳤습니다. 강조 동작은 그대로입니다.

## 바꾼 줄 (호진님 `feabb4484`)

| 자리 | 바뀐 것 |
|---|---|
| `UnitModelAppearance.Apply()`의 `state.Apply(...)` 호출 | 둘째 인자가 플래시 색(`Color`)에서 플래시 모습(`CombatFlashLook`)으로 |
| `RendererState.Apply(...)` 서명 | 같은 인자 하나만 바뀜(외곽선 인자 셋은 그대로) |
| `RendererState.Apply`의 외곽선 쓰기(`if (_supportsOutline[i])`) | 바탕(강조 외곽선 또는 재질 외곽선)을 먼저 구하고, 피격 플래시가 외곽선을 가지면 세기만큼 섞어 씀 |

## 강조와 같이 있을 때

- 강조 대여(`FieldUnitView.BeginOutline`)와 피격 플래시는 다른 층입니다. 강조 중인 유닛이 맞아도 예외가 없습니다.
- 피격 동안은 강조 외곽선 위에 피격 빨강이 섞이고, 플래시가 끝나면 강조 외곽선으로 돌아옵니다.
- 피격 외곽선은 외곽선 칸이 없는 재질 슬롯을 건너뜁니다. 강조의 `RequireOutlineSupport`(없으면 멈춤)는 그대로입니다.

## 확인 부탁

- 튜토리얼 강조 중인 유닛이 공격받을 때 강조색으로 잘 돌아오는지.

정책 원본: [조각·조립 설계 BP9 보충](../specs/2026-10-18-combat-presentation-pieces-design.md) "피격 외곽선".
