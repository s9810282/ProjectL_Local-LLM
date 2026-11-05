# 전투 이펙트 구조 2판 — View DB 칸 메모 (호진님께, 2026-11-01, 미전달)

전투 View 이펙트 구조 2판([설계](../specs/2026-10-31-combat-effect-lifetime-design.md) EL1~EL11, [계획](../plans/2026-10-31-combat-effect-structure-plan.md))에서 View DB 칸이 바뀌었습니다. SO 모드 도구·에셋을 만질 때 참고해 주세요. 코드와 에셋은 같은 커밋이어야 합니다(모르는 칸은 Unity가 조용히 버림).

## 1. 바뀐 칸

| 에셋 | 칸 | 뜻 | 기본값 |
|---|---|---|---|
| 이펙트 큐 항목(모든 큐 칸) | `end` | 끝: `0` Natural(프리팹이 스스로 멈춤) · `1` Seconds(`endSeconds` 뒤 거둠) · `2` UnitAction(그 발동이 연 유닛 행동이 끝나면 거둠) | 0 |
| 같은 곳 | `endSeconds` | `end`가 Seconds일 때 초(0보다 커야 함) | 0 |
| 상태 외형 `UnitStatusVisualData` | `whileEffect` | 그 상태가 HUD에 보이는 동안 몸에 붙는 이펙트(`effect`·`positionOffset`·`rotationOffset`). `effect` Empty면 없음 | 없음 |
| 같은 곳 | `restartOn` | 이 발동 종류가 그 유닛에 오면 유지 이펙트를 거두고 다시 냄(예: 보호막 `ShieldReset` 240). 유지 이펙트 없이 적으면 DB 생성 실패 | 빈 목록 |
| 특성 외형 `TraitVisualData` | `alwaysEffects` | 조건이 맞는 동안 늘 보이는 이펙트 목록. 항목 = `condition`(`0` WhileOwned 보유 동안 · `1` AttackModeTarget 공격 모드 대상) + `effect`(위 `whileEffect`와 같은 모양). 이펙트가 Empty인 항목은 DB 생성 실패 | 빈 목록 |
| 특성 발동 줄 `TraitActivationVisual` | `direction` | 받는 값의 방향: `0` Any · `1` Up(오를 때) · `2` Down(내릴 때). 결속 방어력(`DefenseChanged` 340) 줄에서만 Up/Down. 같은 (종류, 방향) 두 줄은 실패 | 0 |
| 같은 줄의 유닛 행동 | `place` = `4` Relocate | 위치 이동: 미뤄 둔 사실 자리(위치 교환 승객의 새 자리)로 한 번 감, 돌아오지 않음. 거리·머묾·돌아오는 시간 0, 보는 쪽 Keep이어야 함(바깥 시간·걸음 모양만 씀) | — |

- 걷은 칸: 발동 줄의 `keepWhileStatus`·`recallKinds`(EL-b). 상태 동안 이펙트는 상태 외형이, 보유 동안은 특성 외형 상시 목록이 맡습니다.
- 끝 `UnitAction`은 **유닛 행동이 있는 특성 발동 줄의 큐에서만** 고를 수 있습니다. 다른 큐 칸(공격·피격·사망·사실 순간·토네이도)이나 행동 없는 줄, 대신 맞기(`DamageRedirected` 110, 희생) 줄에서 고르면 DB 생성 실패입니다(대신 맞기는 공격 조립 안에서 행동해 행동 끝을 세지 않음).

## 2. 지금 들어 있는 값(정리표 2026-10-31 18:09 기준)

- 상태 외형: 보호막 `whileEffect` 8 · `restartOn` [240], 약점 노출 `whileEffect` 12.
- 특성 상시: 희생 [보유 동안 · 11], 굳건한 의지 [보유 동안 · 10]. 굳건한 의지의 옛 290 줄은 걷음.
- 순간 줄 15(모두 끝 자연, 자리 = 발동 유닛 몸 또는 대상 몸): 포효 360 → 6, 재생·피의 부름 100 → 5, 위압·무장 해제·발목 부수기 160 → 14, 방어구 부수기 120 → 14, 선봉장·깔끔한 뒷정리·사기 증진 90 → 13(대상), 가속 90 → 13(발동 유닛), 고정 사격 160 → 14 · 200 → 13, 결속 340 Up → 13 · Down → 14.
- 위치 교환 440 `MovementChanged` 250: 큐 9 · 대상 몸 · 끝 UnitAction, 행동 Targets · Relocate · 뛰기 · 바깥 0.5초.
- 희생 행동 거리 0.5 → 0.4(둘 다).
- 마무리 사격(400) 공격 모드 대상 상시 이펙트는 정리표에 이펙트가 없어 넣지 않았습니다. 고르면 `alwaysEffects`에 [AttackModeTarget · 이펙트]만 넣으면 됩니다.

## 3. SO 모드 도구에 닿는 것

- 도구가 특성 외형·상태 외형을 필드 이름으로 읽는 곳이 있으면, 위 새 칸은 없어도 되는 칸이라 읽기에는 영향이 없습니다. 쓰기(복제)는 필드를 통째로 복사하면 그대로 따라갑니다.
