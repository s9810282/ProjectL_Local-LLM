# 도발을 디버프 상태로 (설계 초안)

- 날짜: 2026-10-31
- 상태: **결정 완료(2026-10-31 사용자 "권장대로" = A·B·A·A·A), TT-a Core 코드 완료(컴파일 미확인, 새 시험 없음, Core 전달 문서 §5.74), TT-b 최소 View 짝(HUD 표시 변환·상태 외형·설명 갈래) 완료(계획 §7.2, 같은 커밋). 발동 연출 줄 고르기는 에셋 작업으로 남음. 도발당한 적의 유지 이펙트는 [이펙트 구조 2판](2026-10-31-combat-effect-lifetime-design.md) EL2(상태 외형 칸)가 맡는다.** 계획 [특성 이펙트 Core 변경 계획](../plans/2026-10-31-combat-trait-core-changes-plan.md)
- 요청: 사용자 "도발에 당한 유닛이 상태(디버프)인가" → 답: 아니다(AI가 고를 때만 범위 규칙을 봄) → 사용자 "1번으로 우리가 수정하는 방향으로" = **Core에 '도발당함' 디버프 상태를 넣고, 이 작업에서 Core를 고친다**(사용자 명시 지시, Core 전달 문서 §5에 기록할 것).
- 관련: [이펙트 끝 규칙 설계](2026-10-31-combat-effect-lifetime-design.md)(끝 "상태 동안"), [특성 이펙트 정리표](../viewDocs/combat-trait-effect-sheet.md)(도발 줄).

## 0. 요지

도발 범위 안의 적에게 Core가 **'도발당함' 디버프 상태**를 겁니다. 그러면 다른 디버프처럼 다음이 저절로 됩니다.

- HUD 상태 아이콘
- 이펙트 끝 "상태 동안"
- 걸릴 때·풀릴 때 발동 사실(이펙트·글자)

판정 함수는 지금 AI가 쓰는 도발 규칙(`TauntTraitRule.Affects`) 하나를 그대로 씁니다. 상태는 그 판정 결과를 게시한 것입니다. 켜고 끄는 시점은 보호막(단독 행동·결속)과 같은 자리인 **보유 조정(`TraitPresenceReconciler`)**입니다.

## 1. 지금 (코드 확인)

| 항목 | 지금 |
|---|---|
| 도발 보유자 | 보유 특성 표시 `TauntTraitStatusSnapshot`만 있고 값 없음. 상태 목록(`StatusEffects`) 밖. |
| 도발당한 적 | 아무것도 남지 않음. 상태·디버프 없음. |
| 판정 | 적 AI가 공격·추격 대상을 고를 때마다 `TauntTargeting.Sources`가 필드에 살아 있는 보유자 중 `TauntEffectRule.Affects`(적대 + 둘 다 필드 + `SquareNeighbors` 반경 `RadiusCells`)가 맞는 유닛을 찾아 가장 가까운 순으로 우선. |
| 사실 | 도발로 대상이 정해질 때만 `TauntSelected` 발동(발동 = 보유자, 대상 = 그 적). |
| 상태 종류 | `StatusEffectKey` 0~13, 분류 표 `StatusEffectCatalog`. 새 정규 상태의 선례는 `WeaknessExposureStatus`(레거시 투영 없이 `StatusEffects`에만 있음 — `With`가 다른 종류를 보존). |
| 켜짐 조건형 상태의 선례 | 보호막: 보유 조정이 명령 끝·피해 단계·이동 투영·턴 끝마다 다시 계산하고, 바뀌면 `ShieldGranted`/`ShieldRemoved` 발동 + 상태 사건(`TraitState`). |
| 수명 종류 | `StatusEffectLifetimeKind.CurrentEligibility = 6`("조건이 맞는 동안")이 이미 있음. |

## 2. 설계

### TT1 새 상태 종류

- `StatusEffectKey.Taunted = 14`, 분류 **디버프**(`StatusEffectCatalog`).
- 새 정규 상태 `TauntedStatus : AppliedStatus`.
  - 출처 = 도발 보유자의 특성 출처(`TraitEffectOrigin.Create(보유자, TraitKey.Taunt)`).
  - 값은 없습니다. 보유자마다 한 항목이고, 같은 출처의 항목은 하나만 둡니다.
  - 수명 = `CurrentEligibility`.
- 읽기 모델 `StatusEffectReadModel`에 `TauntedEffectSnapshot`(출처 목록)을 더합니다. HUD는 효과 종류마다 아이콘 하나라서, 보유자가 여럿이어도 아이콘 하나에 출처 줄이 여럿 붙습니다.

### TT2 누가 걸리나 (Q-TT1)

- 판정 = `TauntEffectRule.Affects(보유자, 그 유닛)`: AI와 같은 함수이고, 규칙은 한 곳입니다.
- 권장 A: **AI가 행동을 정하는 진영(적)만** 겁니다. 플레이어 유닛은 도발에 따르지 않으므로, 적이 도발을 가져도 플레이어 유닛에 "도발당함"을 보이면 거짓 안내가 됩니다.
- B: 적대 유닛 모두에게 겁니다(규칙 그대로).

### TT3 언제 켜고 끄나

- 보유 조정(`ReconcileAndRecord`)이 보호막과 함께 받는 쪽마다 `TauntedStatus` 목록을 다시 계산합니다. 부르는 자리는 지금과 같습니다: 명령 끝, 피해 단계, 이동 투영, 턴 끝, 희생 계획.
- 바뀌면 사실을 냅니다(Q-TT4 A).
  - 발동 `StatusApplied`(걸림)/`StatusRemoved`(풀림): 특성 = 도발, 발동 유닛 = 보유자, 대상 = 걸리거나 풀린 유닛들, 근원 = 그 조정의 근원.
  - 상태 사건(`TraitState`): 지금처럼 바뀐 유닛마다.
- 정리 길에서는 지웁니다(다른 디버프와 같음).
  - 사망·전투 끝(`CombatTemporaryStateCleanup`)
  - 손패 복귀(`AppliedStatusRules.ClearForReserve`)
  - 보유자가 손패로 가거나 죽은 경우: 다음 조정이 판정 실패로 지웁니다.

### TT4 AI (Q-TT2)

- 권장 B: **AI는 그대로** 규칙 함수로 판정합니다. 상태와 AI가 같은 함수(`TauntEffectRule.Affects`)를 부르므로 규칙이 둘이 되지 않고, 적 행동이 바뀌지 않습니다.
- A: AI가 상태(`TauntedStatus` 출처)를 읽습니다. 이 경우 상태 게시 시점이 AI 결정보다 앞서야 하고(각 명령 끝 조정이 이를 보장하는지 확인 필요), 행동이 바뀔 위험이 있습니다.

### TT5 되돌리기·검증

- 이동 되돌리기(`MovementEffectUndo.ForwardCharactersMatch`)는 "최종 상태에 조정을 다시 돌려도 바뀌는 것이 없고, 되돌린 사실이 이동 전과 같을 것"을 봅니다.
  - 도발당함도 조정에 들어가면 이동 투영 끝 조정이 이미 반영하므로 첫 조건은 지켜집니다.
  - 둘째 조건은 `NormalizeMovementUndo`가 되돌린 자리로 도발당함을 다시 계산해야 맞습니다. 구현 때 확인합니다(P1).
- 사실 검증(`CombatChangeEvidenceReader`)은 `StatusApplied`/`StatusRemoved`에 대상의 특성 상태 사건을 요구합니다. TT3이 같은 조정 안에서 상태 사건을 내므로 맞습니다. 구현 때 확인합니다(P2).

### TT6 View

- HUD 표시 변환 한 줄(`CombatUnitStatusFactory`, 값 없음)을 더합니다.
- 상태 외형 에셋 `UnitStatusVisual_Taunted`를 만들고 표에 등록합니다. 이름 "도발당함", 설명 "도발한 유닛을 우선 공격하거나 추격합니다.", 아이콘은 비워 둡니다(문구 확인 대상).
- 설명 문구(`CombatStatusDescriptionFormatter`)에 `TauntedStatus` 갈래("도발")와 수명 `CurrentEligibility`("조건이 맞는 동안") 갈래를 더합니다. 지금 이 두 갈래가 없으면 예외를 던집니다.
- 켜짐 판정(`CombatTraitStatusPresence.IsOn`)에서 `TauntedStatus`는 늘 켜짐입니다. 그래서 끝 "상태 동안"을 쓸 수 있습니다. (2026-11-01 고침: 켜짐 판정 `CombatTraitStatusPresence`는 이펙트 구조 2판 EL-b에서 걷었습니다. 도발당한 적의 유지 이펙트는 상태 외형 `UnitStatusVisual_Taunted`의 `whileEffect` 칸이 맡고, HUD 아이콘과 같은 규칙으로 켜고 끕니다.)
- 발동 연출 줄은 에셋으로 고를 수 있습니다: 도발 `StatusApplied`·`StatusRemoved`. HUD와 시작을 맞추는 `StateApplied` 시기는 이펙트 끝 규칙 설계의 Q-EL6과 함께 정합니다.

### TT7 문서

- 정리표: 도발을 **[디버프]**로 옮기고 `StatusApplied`·`StatusRemoved` 줄을 더합니다(페이지와 md).
- Core 전달 문서 §5에 변경을 적고, 천명님 공유 문서를 따로 씁니다.

## 3. 시험

- 새 시험은 쓰지 않습니다(View 작업 규칙).
- **깨질 것이 확실한 다른 사람 시험이 하나 있습니다**(Q-TT5): 천명님 `StatusEffectContractTests.Catalog_ReservesStableUniqueKeysAndSeparatesClassification`. 종류 수가 정확히 14(0~13)이고 디버프 목록이 고정이라, 새 종류 14를 넣으면 실패합니다.
- 깨질 수 있는 시험(사건 목록·상태 목록을 그대로 비교하는 것): `TauntTraitTests`·`TauntPublicationTests`·`MovementPassiveUndoTests`·`MovementUndoLifetimeTests`·`StatusEffectReadModelTests` 등. 도발 보유자 주변에 적이 있는 사례에서 새 사건·상태가 더해집니다. 실행에서 깨지면 고치기 전에 묻습니다(작성자가 다른 사람이면).

## 4. 정할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-TT1 | 누가 걸리나 | **A** AI 진영(적)만 |
| Q-TT2 | AI가 무엇을 읽나 | **B** 지금처럼 규칙 함수(상태와 같은 함수) |
| Q-TT3 | 도발 보유자에게도 "도발 중" 버프 아이콘을 띄우나 | **A** 띄우지 않음(보유 특성 아이콘으로 충분) |
| Q-TT4 | 걸림·풀림 발동 사실을 내나 | **A** 낸다(`StatusApplied`/`StatusRemoved` — 이펙트·글자·HUD 맞추기에 필요) |
| Q-TT5 | 천명님 `StatusEffectContractTests` 종류 수·디버프 목록 시험 | **A** 기대값만 맞춤(15개, 디버프 목록에 Taunted) — 고치기 전 허락 필요 |

## 5. Phase(안)

| Phase | 내용 |
|---|---|
| TT-a | Core: 상태 종류·분류·정규 상태·읽기 모델, 보유 조정에서 계산·사실, 정리 길, 되돌리기 확인 |
| TT-b | View: HUD 표시 변환·상태 외형 에셋·설명 갈래·켜짐 판정 |
| TT-c | 정리표·문서·Core 전달 문서 §5·천명님 공유 |

컴파일·시험·재생은 사용자가 Unity에서 확인합니다.

## 6. 구현 때 확인할 것

- P1 `NormalizeMovementUndo`가 도발당함을 되돌린 자리로 다시 계산하는지
- P2 사실 검증이 도발 `StatusApplied`/`StatusRemoved`를 받는지
- P3 보유 조정이 적 차례 시작 전에도 도는지(턴 넘김 `CombatTurnCalculator` 조정). 적 차례 첫 AI 결정 때 상태가 맞는지
- P4 `RequiresOrderedDamage`(보호막 보유 시 피해 순서) 같은 다른 보유 조정 조건에 도발을 넣을 필요가 있는지(없을 것으로 봄 — 피해 계산에 안 쓰임)
