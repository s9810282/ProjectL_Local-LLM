# 특성 이펙트 작업 Core 변경 Task 계획 (도발당함 디버프·마무리 사격 대상 목록)

- 날짜: 2026-10-31
- 상태: **CT-a·CT-b Core 코드 완료(2026-10-31 사용자 "권장대로 코어 작업 시작해, View 작업은 코어 작업 검토 후 진행할게", 컴파일 미확인, 새 시험 없음), 사용자 검토 대기. View 짝(§2.6)·CT-c 공유는 검토 뒤.** 기준 판 `a93534913`. 실행 기록 §7.
- 요청: 사용자 "이번에 코어 변경할 거 Task 정리".
- 설계 원본: [도발 디버프 설계](../specs/2026-10-31-combat-taunt-debuff-design.md)(TT1~TT7), [마무리 사격 대상 표시 설계](../specs/2026-10-31-combat-finishing-shot-target-mark-design.md)(FS1~FS4). 목록: [바뀐 것·바꿀 것 정리](../handoffs/2026-10-31-combat-trait-effect-change-summary.md) §2.2, Core 전달 문서 §57.
- 이 계획은 두 설계의 권장안(Q-TT1 A·Q-TT2 B·Q-TT3 A·Q-TT4 A·Q-TT5 A, Q-FS1 A·Q-FS2 A)을 전제로 씁니다. 다르게 정하면 고칩니다.

## 0. 요지·순서

| Phase | 내용 | Core 파일 | 함께 커밋할 View |
|---|---|---|---|
| CT-a | 도발당함 디버프 | 9개 + 새 0개 | **TT-b 최소 짝 필수**(§2.6) — 없으면 도발당한 적이 생기는 순간 HUD가 예외로 멈춤 |
| CT-b | 마무리 사격 대상 목록 | 2개 | 없음(View가 아직 안 읽음, FS-b는 따로) |
| CT-c | 기록·공유 | 문서만 | — |

- CT-a와 CT-b는 서로 독립입니다. 순서는 바꿔도 됩니다.
- 새 시험은 쓰지 않습니다(View 작업 규칙). 컴파일·시험·재생은 사용자가 Unity에서 확인합니다.
- Phase마다 멈추고 보고합니다.

## 1. 계획에서 정한 것 (확인 대상)

설계에 없거나 설계와 다르게 정한 것입니다.

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| TP-C1 | 도발당함은 `Owner.Enemy` 유닛에만 겁니다(Q-TT1 A의 "AI 진영"). | AI가 행동을 정하는 진영이 적뿐입니다. |
| TP-C2 | 도발당함은 정규 상태 목록(`StatusEffects`)에만 둡니다. 옛 디버프·특성 효과 투영(`CharacterStatusCompatibility`)에는 넣지 않습니다. | 약점 노출과 같은 방식입니다. 투영은 `OfType`으로 고르므로 새 형식을 그대로 보존합니다. |
| TP-C3 | 보유 조정은 **도발당함 출처 집합이 바뀔 때만** 상태 목록을 바꿉니다. 바꿀 때는 다른 상태의 순서를 그대로 두고 도발당함 항목만 출처 순으로 다시 놓습니다. | `StatusEffectSet.SameValues`가 순서까지 비교합니다. 매번 다시 놓으면 바뀐 것이 없어도 변경이 생겨, 조정마다 헛 상태 사건이 납니다. |
| TP-C4 | **이동 되돌리기 영수증을 새로 만들지 않습니다.** 도발당함은 자리만으로 정해지므로, 되돌린 자리에서 `NormalizeMovementUndo`의 다시 계산이 그대로 복원합니다. 대신 두 곳을 고칩니다. ① 이동 사건 허용 목록(`ForwardEventsSupported`)에 도발 `StatusApplied`·`StatusRemoved`. ② 유닛 하나 되돌림 비교(`SupportsCharacterChange`)는 도발당함 항목을 빼고 비교합니다. | 설계 TT5는 "구현 때 확인"이었습니다. 코드를 대조해 보니 영수증이 없으면 ②에서 막혀, 도발 범위를 바꾸는 이동은 모두 되돌리기가 안 됩니다. 영수증(`MovementShieldReceipt` 꼴)보다 비교에서 빼는 쪽이 작고, 다시 계산이 같은 규칙 함수를 쓰므로 규칙이 둘이 되지 않습니다. |
| TP-C5 | 걸림·풀림 발동의 양(amount)은 0입니다. | 값이 없는 상태입니다. 정리의 `StatusRemoved`도 0을 씁니다. |
| TP-C6 | 판정은 `TauntTargeting.Sources`를 캐릭터 목록을 받는 꼴로 나눠 보유 조정과 AI가 함께 부릅니다. AI의 호출 줄(`EnemyAiPolicy.cs:45`·`:125`)은 그대로입니다. | 지금 `Sources`는 `input.Characters`만 읽어, 보유 조정이 쓰는 이번 투영 목록을 넘길 수 없습니다. |
| TP-C7 | 사망·전투 끝 정리(`CombatTemporaryStateCleanup`)가 내는 `StatusRemoved` 출처 목록에 도발당함 출처를 더합니다. 손패 복귀 정리(`CombatReserveReturnCleanup`)는 이미 모든 특성 출처를 내므로 고치지 않습니다. | 사망 정리는 출처를 종류별로 나열하는 꼴입니다(약점 노출도 따로 한 줄). |
| TP-C8 | 마무리 사격 대상은 `Range`가 이미 만든 사거리 안 대상 목록(`ids`, 캐릭터와 구조물)에서 고릅니다. 자금 부족 갈래(`ActionCostInsufficient`)도 범위를 그대로 내므로 대상 목록도 냅니다. 실패 결과는 빈 목록입니다. | Q-FS1 A. 자금 부족이어도 범위 칸은 보이므로 표시도 맞춥니다. |
| TP-C9 | `AttackRangeResult`의 새 인자 `instantDefeatTargetIds`는 생성자와 `Success`의 **끝 선택 인자**입니다(없으면 빈 목록). | 기존 호출(`CombatAttackQueries`, 시험 `AttackRangeResultTests`·`UnitAttackRangeQueryTests`)이 그대로 컴파일됩니다. |

## 2. CT-a 도발당함 디버프

### 2.1 상태 형식 (Contracts)

- Ta.1 `Contracts/CombatEffects/StatusEffectKey.cs`
  - `StatusEffectKey.Taunted = 14`를 더합니다.
  - `StatusEffectCatalog.ClassificationOf`의 디버프 갈래에 `Taunted`를 더합니다.
- Ta.2 `Contracts/CombatEffects/AppliedStatus.cs`
  - 새 `TauntedStatus : AppliedStatus`: 생성자 `(EffectOriginKey origin)`, 출처 필수(특성 출처, 정의 키 = 도발), `SameValues` = 같은 형식 + 같은 출처.
- Ta.3 `Contracts/Snapshots/StatusEffectSnapshot.cs`
  - `StatusEffectSourceSnapshot`의 수명 갈래: `TauntedStatus => CurrentEligibility`. 지금은 모르는 형식에 예외를 던집니다.
  - 새 `TauntedEffectSnapshot : StatusEffectSnapshot`(출처 목록만, 값 없음).

### 2.2 규칙 (Domain)

- Ta.4 `Domain/CombatEffects/Rules/AppliedStatusRules.cs`
  - 새 함수 `ReplaceTaunted(StatusEffectSet current, IReadOnlyList<EffectOriginKey> origins)`: 출처 집합이 지금과 같으면 같은 참조를 돌려줍니다(TP-C3). 다르면 도발당함이 아닌 항목은 순서대로 두고, 도발당함 항목을 출처 순으로 붙입니다.
  - `Apply`에는 갈래를 더하지 않습니다(도발당함은 부여가 아니라 조정이 씀). 잘못 부르면 지금처럼 "아직 지원하지 않는 효과 부여" 예외가 납니다.
  - 손패 복귀(`ClearForReserve`)·사망(`ClearForDeathOrBattleEnd`)·턴 끝(`EndControllerTurn`)은 고치지 않습니다. 앞의 둘은 이미 방어막 말고 모두 비우고, 턴 끝은 모르는 항목을 그대로 둡니다.

### 2.3 판정·조정 (Application)

- Ta.5 `Application/Combat/EnemyAi/TauntTargeting.cs`
  - `Sources(CombatCommandInput input, IEnumerable<CharacterCombatFacts> characters, CharacterCombatFacts actor)`를 더하고, 지금의 `Sources(input, actor)`는 `input.Characters`를 넘기는 한 줄로 둡니다(TP-C6).
  - 정렬(가까운 순)은 AI만 필요하지만 그대로 둡니다. 조정은 출처만 씁니다.
- Ta.6 `Application/Combat/TraitPresenceReconciler.cs`
  - `Calculate`: 받는 쪽마다 지금의 보호막 계산 뒤에 도발당함 출처를 구합니다. 조건은 `Owner.Enemy`, 죽지 않음, 필드(TP-C1). 출처 = `Sources(input, projected, recipient)`의 각 보유자에 대해 `TraitEffectOrigin.Create(보유자, TraitKey.Taunt)`. 그런 뒤 `ReplaceTaunted`로 상태 목록을 맞춥니다.
    - 변경 판단을 `effects.SameValues(...)`만이 아니라 상태 목록 비교까지 넓힙니다. 결과 유닛 = `recipient.With(traitEffects: effects).WithStatusEffects(ReplaceTaunted(...))`. 특성 효과를 먼저 바꾸고 도발당함을 나중에 맞춥니다(`With(traitEffects:)`가 정규 목록의 특성 효과 항목을 다시 놓기 때문).
  - 새 `RecordTauntActivations(events, changes, origin)`: 바뀐 유닛마다 전·후 도발당함 출처를 비교합니다. 출처(보유자)별로 묶어 새로 생긴 받는 쪽은 `StatusApplied`, 없어진 받는 쪽은 `StatusRemoved`를 냅니다. 발동 유닛 = 보유자, 대상 = 받는 쪽들, 양 0(TP-C5), 근원 = 조정의 근원. `RecordShieldActivations` 꼴입니다.
  - `ReconcileAndRecord`가 `RecordShieldActivations` 다음에 `RecordTauntActivations`를 부릅니다. 상태 사건(`TraitState`)은 지금 반복문이 바뀐 유닛마다 이미 냅니다.
  - `NormalizeMovementUndo`는 고치지 않습니다. 보호막이 아닌 변경은 그대로 받아들이고 다시 계산 결과로 덮습니다(TP-C4).
  - `RequiresOrderedDamage`에는 도발을 넣지 않습니다(설계 P4 — 피해 계산에 안 쓰임).
- Ta.7 `Application/Combat/CombatTemporaryStateCleanup.cs`
  - 사망·전투 끝 `StatusRemoved` 출처 목록에 `traitBefore.StatusEffects.Entries.OfType<TauntedStatus>()`의 출처를 더합니다(약점 노출 줄 옆, TP-C7).
- Ta.8 `Application/Combat/MovementEffectUndo.cs`
  - `ForwardEventsSupported`의 허용 목록: `t.TraitKey == TraitKey.Taunt && t.TraitActivationKind is StatusApplied or StatusRemoved`.
  - `SupportsCharacterChange`: 마지막 비교를 도발당함 항목을 뺀 두 유닛으로 합니다(`ReplaceTaunted(…, 빈 목록)`을 양쪽에). 까닭은 TP-C4.
- Ta.9 `Application/Combat/StatusEffectReadModel.cs`
  - `Capture`에 `Sources<TauntedStatus>` → `TauntedEffectSnapshot` 한 줄.

### 2.4 바꾸지 않는 것

- 적 AI(`EnemyAiPolicy`)의 도발 판정(Q-TT2 B). 사실 검증 `CombatChangeEvidenceReader`: 특성 상태 비교가 정규 목록의 출처 정의 키로 찾으므로 도발당함 변경을 그대로 받습니다(설계 P2 확인됨).
- 턴 넘김: `CombatTurnCalculator.cs:186`이 넘김마다 보유 조정을 돌리므로, 적 차례 첫 AI 결정 전에 상태가 맞춰집니다(설계 P3 확인됨).

### 2.5 시험

- **깨지는 것(확실):** 천명님 `Tests/EditMode/StatusEffectContractTests.cs:144` `Catalog_ReservesStableUniqueKeysAndSeparatesClassification`. 종류가 정확히 0~13이고 디버프 목록이 고정입니다. Q-TT5 A면 기대값만 `Enumerable.Range(0, 15)`·디버프 목록에 `Taunted`로 맞춥니다. **고치기 전 허락이 필요합니다.**
- **깨질 수 있는 것(실행에서):** 도발 보유자 주변에 적이 있는 사례에서 사건·상태 목록을 그대로 비교하는 시험. 예: `TauntTraitTests`·`TauntPublicationTests`·`MovementPassiveUndoTests`·`MovementUndoLifetimeTests`·`StatusEffectReadModelTests`. 사용자 Unity 실행에서 깨지면, 다른 사람이 쓴 시험이라 고치기 전에 묻습니다.

### 2.6 함께 커밋할 View 짝 (TT-b 최소)

Core만 넣으면 도발당한 적이 생기는 순간 HUD가 `[CombatEffectFormatters] 표시 변환이 등록되지 않았습니다: Taunted` 예외로 멈춥니다. 그래서 아래를 CT-a와 같은 커밋에 넣습니다.

- Tb.1 `Systems/Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs`: `new CombatEffectFormatter<TauntedEffectSnapshot>(StatusEffectKey.Taunted)`(값 없음) 한 줄. Factory 줄이라 민형님 공유에 적습니다.
- Tb.2 상태 외형 에셋 `UnitStatusVisual_Taunted`(+ `.meta`, 이름 "도발당함", 설명 "도발한 유닛을 우선 공격하거나 추격합니다.", 아이콘 비움)과 표 등록. Tb.1과 반드시 함께: 표시 변환 묶음(`CombatEffectFormatters`)이 만들 때 등록된 종류마다 외형과 설명 틀 인자 수(0개)를 검사하므로, 한쪽만 있으면 전투 진입 때 멈춥니다.
- Tb.3 `ViewSystem/Combat/Display/Unit/CombatStatusDescriptionFormatter.cs`: 세부 갈래 `TauntedStatus => "도발"`, 수명 갈래 `CurrentEligibility => "조건이 맞는 동안"`. 지금 둘 다 없으면 예외입니다.
- Tb.4 `ViewSystem/Combat/Playback/Support/CombatTraitStatusPresence.cs`: `TauntedStatus`는 늘 켜짐. 이펙트 끝 "상태 동안"이 됩니다. (2026-10-31 확인: 이미 보호막이 아닌 상태는 목록에 있으면 켜짐이라 고칠 것 없음.)
- 발동 연출 줄(도발 `StatusApplied`·`StatusRemoved`)은 에셋으로 고르는 일이라 이 짝에 넣지 않습니다.

### 2.7 Unity 확인 Ua

1. 컴파일.
2. 도발 보유 아군 주변 2칸에 적을 두고 배치·이동 → 그 적 HUD에 "도발당함" 아이콘, 설명 "조건이 맞는 동안".
3. 보유자를 멀리 옮김 → 아이콘이 사라짐.
4. 그 이동을 되돌림 → 되돌리기 버튼이 켜져 있고, 되돌리면 아이콘이 다시 생김.
5. 보유자가 손패로 가거나 죽음 → 아이콘이 사라짐.
6. 적 차례 AI가 지금처럼 보유자를 우선 공격·추격함(행동 바뀜 없음).
7. 플레이어 유닛에는 아이콘이 생기지 않음.
8. Core 시험(위 §2.5) 결과.

## 3. CT-b 마무리 사격 대상 목록

- Tb-c.1 `Contracts/Results/AttackRangeResult.cs`
  - 새 속성 `ReadOnlyMemory<EntityId> InstantDefeatTargetIds`.
  - 생성자와 `Success`에 끝 선택 인자 `EntityId[] instantDefeatTargetIds = null`(null이면 빈 목록, 복사해서 보관, TP-C9). `Failure`는 빈 목록.
- Tb-c.2 `Application/Combat/CombatAttackQueries.cs` `Range`
  - `ids`를 만든 뒤, 캐릭터는 `input.Characters`, 구조물은 `input.State.Structures`에서 찾아 `FinishingShotApplication.Qualifies(input, actor, 캐릭터, 구조물)`이 참인 것만 고릅니다. 순서는 `ids` 순서입니다.
  - 마무리 사격이 없는 공격자면 `Qualifies`가 바로 거짓이라 빈 목록입니다.
  - 자금 부족 갈래에서도 함께 냅니다(TP-C8).
- 시험: 끝 선택 인자라 기존 시험은 그대로 컴파일됩니다. 새 시험 없음.
- Unity 확인 Ub: 컴파일, 마무리 사격 보유자로 공격 모드 진입이 지금과 같음(View는 아직 이 목록을 안 읽음). 목록 값은 FS-b가 생긴 뒤 화면으로 확인합니다.

## 4. CT-c 기록·공유

- Core 전달 문서 §5에 두 절(§5.74 도발당함, §5.75 마무리 사격 대상 목록): 바꾼 파일·동작·그대로인 것·시험.
- §57을 "구현됨"으로 고치고 §5 절을 가리킵니다.
- 천명님 공유 문서 하나(새 파일, 미전달): 두 변경, TP-C3·TP-C4 판단, 고친 천명님 시험(허락분), AI 무변경.
- 민형님 Factory 공유: Tb.1 한 줄.
- 설계 두 문서의 머리 상태, 정리 문서 §2.2, CLAUDE.md 현재 상태.

## 5. 커밋 묶음

| 커밋 | 내용 |
|---|---|
| 1 | CT-a Core 9개 + View 짝(Tb.1~Tb.4, 새 에셋 + `.meta`, 표 등록) + 허락받은 천명님 시험 기대값 |
| 2 | CT-b Core 2개 |
| 3 | CT-c 문서 |

커밋과 push는 사용자가 직접 합니다(사용자가 시키는 경우만 대신). 2026-10-31 사용자 "저거까지 작업해서 커밋하자"로 1·2·3을 한 커밋에 넣었다.

## 6. 착수 전 정할 것

| 번호 | 내용 | 권장 |
|---|---|---|
| Q-TT1~Q-TT5 | 도발 설계 §4 | A·B·A·A·A |
| Q-FS1·Q-FS2 | 마무리 사격 설계 §4 | A·A |
| TP-C1~TP-C9 | 이 계획 §1 | 그대로 |
| 시험 허락 | 천명님 `StatusEffectContractTests:144` 기대값 변경 | 허락 |

## 7. 실행 기록

### 7.1 CT-a·CT-b Core 코드 (2026-10-31, 컴파일 미확인, 새 시험 없음)

- 결정: Q-TT1~Q-TT5·Q-FS1·Q-FS2 권장대로, TP-C1~TP-C9 그대로, 천명님 시험 기대값 변경 허락(사용자 "권장대로").
- 바꾼 Core 파일 11개: §2.1~§2.3 Task 그대로(Ta.1~Ta.9) + CT-b 둘. 새 파일 없음. 기록은 Core 전달 문서 §5.74·§5.75, §57에 "구현됨".
- 계획과 다른 점·구체화:
  - TP-C3을 "도발당함 항목은 늘 목록 맨 앞에 출처 순"으로 정했다. 순서까지 비교하는 `SameValues` 때문에, 뒤에 붙이면 도발당함 뒤에 다른 효과가 생긴 적의 경우 되돌리기 다시 계산 결과가 원래 목록과 순서가 달라 되돌리기가 막힌다. 다른 효과 규칙은 모두 자기 항목만 바꾸거나 뒤에 붙여 맨 앞 자리가 유지된다(코드 대조).
  - 필드에 도발 보유자가 하나도 없으면 도발 판정을 건너뛴다(보유 조정이 피해 단계마다 불려서).
- 시험: 천명님 `StatusEffectContractTests:144` 두 줄(기대값만). 실행에서 바뀔 수 있는 다른 사람 시험은 Core 전달 문서 §5.74 목록.
- 남은 것(이때): 사용자 Core 검토 → View 짝(§2.6) → CT-c.

### 7.2 View 짝 Tb.1~Tb.4 (2026-10-31, 사용자 "저거까지 작업해서 커밋하자", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Tb.1 `Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs`: `new CombatEffectFormatter<TauntedEffectSnapshot>(StatusEffectKey.Taunted)`(값 없음). 민형님 Factory 공유 §44.
- Tb.2 새 `DataBaseSO/ViewGroup/UnitStatusVisual/Item/UnitStatusVisual_Taunted.asset`(+ `.meta`, 효과 키 14, 이름 "도발당함", 설명 "도발한 유닛을 우선 공격하거나 추격합니다.", 아이콘 비움)과 `UnitStatusVisualDataTable.asset` 끝 등록 한 줄.
- Tb.3 `ViewSystem/Combat/Display/Unit/CombatStatusDescriptionFormatter.cs`: 세부 `TauntedStatus => "도발"`, 수명 `CurrentEligibility => "조건이 맞는 동안"`.
- Tb.4: 고칠 것 없음(§2.6 메모).
- 함께 확인한 것(코드 변경 없음): 특성 발동 연출 쪽은 멈추지 않는다 — 지원 표 `CombatTraitFeedbackProfiles.Get`은 없으면 기본값, 발동 연출 줄이 없는 발동은 `CombatTraitFeedbackPlan.Build`가 건너뜀. 상태 외형 수를 세는 시험 없음.
- 보고만: `UnitStatusVisual_Acceleration.asset`(효과 키 1)·`UnitStatusVisual_ArmorBreak.asset`(효과 키 4)은 표에 등록되지 않은 채 남아 있다(같은 키의 `AttackIncrease`·`DefenseDecrease`가 등록됨, 지우지 않음).
- 남은 것: Unity 확인 Ua(§2.7)·Ub(§3), CT-c 중 천명님 공유 문서(새 파일). 실행에서 깨지는 다른 사람 시험은 고치기 전에 묻는다.

