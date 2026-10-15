# 샷건 '산탄'·아킴보 '난사' 특성 — View 작업자 공유 메모

**날짜·상태:** 2026-10-06. Core 작업(천명) 완료분 공유. **미전달.** 설계·결정 원본은 [계획 문서](../plans/2026-10-06-akimbo-shotgun-traits-plan.md) §3·§5다. 이 메모는 View·DB 에셋 쪽에서 할 일만 모은다.

## 1 Core에서 바뀐 것 (요약)

| 항목 | 내용 |
|---|---|
| 산탄 | 새 특성 `TraitKey.DistanceDamageScale = 60`. 공격자-대상 체비셰프 거리별 피해 백분율(샷건 `[150, 50]`: 앞 1칸 150%, 뒤 가로 3칸 50%). 반올림은 기존 배율과 같은 half-up. 구조물에도 적용 |
| 난사 ② | 새 특성 `TraitKey.KillChainAttack = 70`. 이 공격 명령으로 처치된 적 1명당 맵 전체의 랜덤한 적 캐릭터를 추가 공격(기본 공격과 같은 피해). 추가 공격으로 처치하면 다시 이어지며 횟수 제한 없음. 구조물·아군 제외 |
| 난사 ① | 기존 `MoveAttackDiscount` 그대로(변경 없음). 아킴보는 특성 두 개(`MoveAttackDiscount` + `KillChainAttack`)를 가진다 |
| 공용 enum 추가 | `TraitKey.DistanceDamageScale = 60`, `TraitKey.KillChainAttack = 70`, `TraitActivationKind.KillChainAttack = 70` |
| DB 입력 | `UnitDataSO.traits`에 `DistanceDamageScaleTraitAuthoring`(`percentByDistance`, 기본 `{150, 50}`), `KillChainAttackTraitAuthoring`(설정 없음)을 넣을 수 있다. `BattleDefinitionAdapter`가 둘 다 변환한다 |

## 2 요청 — DB 에셋

1. `DataBaseSO/InGameGroup/Unit/Item/Unit_ShotGun.asset`의 `traits`에 `DistanceDamageScaleTraitAuthoring` 추가, `percentByDistance = [150, 50]`.
2. `Unit_Akimbo.asset`의 `traits`에 `KillChainAttackTraitAuthoring` 추가(기존 `MoveAttackDiscountTraitAuthoring`은 유지).
3. 에셋 변경 뒤 `DataBaseAssetTests` 실행.

특성 표시(View `TraitVisual` 테이블의 키 60·70 이름·아이콘·설명)는 UI 작업자에게 따로 요청한다. 이 메모의 범위가 아니다.

**주의:** `percentByDistance`는 그 유닛 공격이 닿는 모든 거리를 덮어야 한다(스플래시가 있으면 사거리+1). 목록 밖 거리의 대상을 맞히면 Core가 기본값으로 메우지 않고 `InvalidOperationException`을 던진다(설정 오류를 드러내기 위한 결정). 샷건 사거리를 3으로 늘리면 목록도 3칸 이상으로 늘려야 한다.

## 3 요청 — Dev fixture

`ViewSystem/Dev/DevBattleInitializationSource`의 유닛 사본에 같은 Spec을 더한다.

- ShotGun: `new DistanceDamageScaleTraitSpec(new[] { 150, 50 })`
- Akimbo: 기존 `MoveAttackDiscountTraitSpec(1, 1)`에 더해 `new KillChainAttackTraitSpec()`

## 4 View 표시·연출 계약

**산탄:** 별도 사건이 없다. 배율은 실행과 미리보기 피해에 이미 반영된다(`AttackTargetPreview.ExpectedDamage`·`DamageBeforeMatchup`, `AttackHitResult.DamageDealt`).

**난사 추가 공격:**

- 기본 공격의 `AttackResult`(`Hits`·`TraitActivations`)에는 추가 공격이 **없다**.
- 사건 목록(`CombatCommandTransition.Events`)에서 기본 공격 사건들 뒤에, 추가 공격마다 다음 순서로 온다.
  1. `CombatEffectsEvent`: `Traits`에 `TraitActivationSnapshot(SourceUnitId = 공격자, TargetId = 대상, TraitKey.KillChainAttack, TraitActivationKind.KillChainAttack, Magnitude = 보유 방어 뒤 최종 피해)`. 피해가 0이어도 남는다. **타격 시작 신호로 쓴다.**
  2. 보유 방어(보안관 배지)가 막았으면 `CombatHeldEnvironmentEvent`.
  3. 체력이 바뀌었으면 `CombatCharacterEvent`(HealthChanged 또는 Died, 원점 = 공격자). 피해 0이면 없다.
- 다음 `CombatEffectsEvent`(KillChainAttack)가 나오기 전까지가 한 타격의 결과다.
- 영혼 생성·승패 사건은 기존처럼 명령 끝에 한 번 온다.
- **미리보기에는 추가 공격이 없다**(기획 결정: 랜덤성 유지). 미리보기 피해와 실행 결과가 추가 공격만큼 다를 수 있다.
- 대상은 전투 공용 난수 상태(`EnvironmentProgress.RandomState`)로 고른다. 같은 입력이면 같은 대상이라 기록 재생에서도 같다.

## 5 확인

- Core 쪽 테스트: `DistanceDamageScaleTraitTests`·`KillChainAttackTraitTests`(Unity EditMode 통과), DB 변환 테스트는 `InGameDataTableTests`·`BattleDefinitionAdapterTests`에 추가.
- 위 §2·§3을 반영하기 전까지 실제 게임의 샷건·아킴보에는 새 특성이 붙지 않는다.
