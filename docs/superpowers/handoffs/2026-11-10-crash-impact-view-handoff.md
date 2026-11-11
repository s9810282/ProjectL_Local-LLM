# 작렬(CrashImpact) Core → View 인계

- 작성: 2026-11-10, Core(천명)
- 받는 사람: 전투 View 담당
- 상태: 미전달
- 설계: [crash-impact-trait-design](../specs/2026-11-10-crash-impact-trait-design.md) · 계획: [crash-impact-trait-plan](../plans/2026-11-10-crash-impact-trait-plan.md)

## 1. 무엇이 바뀌었나

다이너마이트 해머(`Unit_DynamiteHammer`, 유닛 키 24)에 새 특성 **작렬**(`TraitKey.CrashImpact = 520`)이 붙었습니다.

- 기본 공격으로 피해를 입힌 대상이 공격자 반대쪽으로 일직선으로 날아갑니다(기본 3칸). 방향·대상은 기존 넉백과 같습니다.
- 다 날아가기 전에 막히면 **부딪힘**입니다.
  - 벽·맵 끝·고지대 턱·구조물: 날아간 대상만 피해를 받습니다(날아간 대상 최대 체력의 10%).
  - 유닛: 날아간 대상과 부딪힌 유닛이 **둘 다** 피해를 받습니다(날아간 대상 최대 체력의 10%). 편은 가리지 않습니다.
  - 피해에는 받는 쪽 각자의 방어력이 적용됩니다(최소 1). 보호막도 먼저 깎입니다.
- 3칸을 다 날아간 뒤 그 다음 칸에 벽이 있는 것은 부딪힘이 아닙니다. 바로 옆이 막혀 한 칸도 못 날아가도 부딪힘입니다.
- 넉백 면역이면 날지 않고 부딪힘도 없습니다.

## 2. View가 받는 신호 (재생 순서대로)

| 순간 | 무엇으로 오나 | 새것? |
|---|---|---|
| 날기 시작 | 발동 사건 `CombatEffectsEvent` 안 `TraitActivationSnapshot` — 특성 키 `CrashImpact`, 종류 `KnockbackApplied`. 출처 = 해머, 대상 = 날아간 유닛들, 칸 = 시작 → 경로. 이동 사건들보다 앞에 옵니다. | 기존 종류(키만 새로) |
| 날아가는 동안 | 걸음마다 `Moved` 사건. `MovementCause` = `Forced` + 특성 키 `CrashImpact`. → **이 원인이 붙은 걸음을 "날아가는 중"으로 보면 됩니다.** 따로 상태 효과는 만들지 않았습니다. | 기존 사건(키만 새로) |
| 부딪힘 | 발동 사건 — 특성 키 `CrashImpact`, **새 종류 `TraitActivationKind.CrashImpactCollided = 410`**. 출처 = 해머, 대상 = 날아간 유닛(+ 부딪힌 유닛), 칸 = 막은 칸 하나(맵 밖일 수 있음), `TargetCell` = 날아간 유닛이 멈춘 칸, `Magnitude` = 방어 전 기준 피해, `ResultOrigin` = 부딪힘 피해 근원. 그 대상의 마지막 걸음 뒤, 부딪힘 피해 사건 앞에 옵니다. | **새것** |
| 부딪힘 피해 | 보통 체력 사건(보호막이면 특성 상태 사건). 근원 `CombatDamageOrigin`: 물리 칸 = 막은 칸, 공로자 = 해머, 공격 그룹 = **새 그룹**(직접 타격과 다른 순간). 날아간 유닛 → 부딪힌 유닛 순서. | 기존 사건 |
| 공격 결과·미리보기 | `AttackKnockbackSnapshot.Collision`(새 선택 값 `AttackKnockbackCollision`): `Kind`(`Wall`/`Structure`/`Character`), `Cell`(막은 칸), `EntityId`(구조물·캐릭터일 때). 막혀 멈춘 캐릭터 넉백이고 부딪힘 피해를 내는 넉백일 때만 값이 있습니다. 일반 넉백은 늘 null입니다. | **새것** |

미리보기(공격 호버):
- 날아간 대상의 예상 체력(`TargetHealthAfter`)에 부딪힘 피해가 들어갑니다.
- 부딪힌 유닛이 공격 대상이 아니면 대상 목록(`AttackPreview.Targets`)에 **보조 행**으로 들어갑니다(`IsDirectHit = false`, 역할 Additional, `ExpectedDamage` = 부딪힘 피해). 넉백 보조 행과 같은 꼴입니다.
- 넉백 화살표(기존 KB 작업)의 도착 칸 앞에 `Collision.Cell`로 "여기서 부딪힘" 표시를 더할 수 있습니다.

여러 대상이 날아가면 ID 순서로 하나씩 처리합니다. 먼저 날아간 대상이 아직 날지 않은 대상에게 부딪힐 수 있고, 그 대상은 부딪힘 피해를 받은 뒤 자기 차례에 날아갑니다.

## 3. View에서 할 일 (제안)

| 할 일 | 없으면 어떻게 되나 (코드 확인) |
|---|---|
| 특성 외형 `TraitVisual_CrashImpact`(키 520, 이름 "작렬")를 만들어 `TraitVisualDataTable`에 등록 | HUD·정보창의 특성 칸이 이 특성을 건너뜁니다. 외형 조회가 모두 `TryGetTraitVisual`이라 멈추지는 않습니다(`CombatEnvironmentText.GetTraitVisual`은 흔적 전용이라 해당 없음). |
| 설명 토큰 `TraitDescriptionBindings`에 `CrashImpact` 줄 — `CrashImpactTraitSpec`의 `DistanceCells`·`WallDamagePercent`·`UnitDamagePercent` | 토큰 없는 설명 문구면 그대로 보입니다. 문구에 토큰을 쓰면 줄이 필요합니다. |
| 특성 외형의 발동 연출 줄 둘: (`KnockbackApplied`) 날기 시작, (`CrashImpactCollided`) 부딪힘 | 줄이 없으면 연출 없이 지나갑니다(`CombatCueSources.TryGetActivation`). |
| (선택) 날아가는 걸음 모양 — 원인 특성 키가 `CrashImpact`인 강제 이동 걸음 | 지금은 일반 밀림 걸음으로 보입니다. |
| (선택) 미리보기에서 `Collision`·보조 행 표시 | 화살표는 막힌 칸 앞까지 그려지고, 부딪힘 표시는 없습니다. |

사건 형식 표(`CombatEventTypeTable`)는 고칠 필요가 없습니다. 새 것은 기존 `CombatEffectsEvent` 안의 새 발동 종류뿐입니다.

## 4. 확인해 주실 것

- 부딪힘 피해의 공격 그룹이 직접 타격과 달라서, 동시 묶음(`CombatConcurrentGroups`)에서는 날아간 뒤의 별도 순간으로 묶일 것으로 봅니다. 재생 순서가 "타격 → 날아감 → 부딪힘 피해"로 보이는지 확인 부탁드립니다.
- 바로 옆이 막힌 경우에는 이동 사건이 없고 발동 사건(`KnockbackApplied`, Magnitude 0)과 부딪힘 사건만 옵니다.
