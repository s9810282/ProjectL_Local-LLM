# 언데드 방패병(참호, 88) 외형 프리팹 교체 — 재령님 공유 (2026-11-17, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 재령님(`UnitVisual_UndeadHeavy_Entrenchment` 작성자).
> 기록: [새 클립 연결 계획](../plans/2026-11-17-combat-unit-new-clips-wiring-plan.md) §5.6.

## 바꾼 것

- `DataBaseSO/ViewGroup/UnitVisual/Item/EnemyTrait/UnitVisual_UndeadHeavy_Entrenchment.asset`의 `prefab`을 옛 `Prefabs/Characters/Colored/Enemy/HeavyEnemy.prefab` → 새 `Prefabs/Characters/New/Enemy/EnemyHeavy/EnemyHeavy.prefab`으로 바꿨습니다. 언데드 방패병(26) `UnitVisual_Heavy`와 같은 프리팹입니다.
- 다른 칸(키 88, 이름·아이콘, 공격·반응 표시, 클립 묶음 `CombatUnitAnimation_Heavy`, 소리 칸 `attackSoundSlotIndex` −1)은 그대로입니다.

## 이유

- 이 외형은 11-11부터 옛 프리팹에 **새** Heavy 컨트롤러·클립 묶음을 붙여 쓰고 있었습니다. 옛 프리팹(`Scull`·`구체` 계층)과 새 클립(`EnemyHeavy/...` 계층)은 뼈 경로가 하나도 맞지 않아 Idle·공격 동작이 나오지 않았습니다.
- 새 프리팹은 새 클립과 경로가 100% 맞습니다(이번 작업에서 대조).

## 알아 두실 것

- 옛 `Colored/Enemy/HeavyEnemy.prefab`은 이제 어느 외형도 참조하지 않습니다. 지우지 않았습니다.
- 소리 칸이 −1이라 공격 소리는 Heavy(26)의 슬롯 10과 다릅니다. 의도하신 값인지 확인 부탁드립니다.
