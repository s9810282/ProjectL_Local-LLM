# 아킴보 '난사'·샷건 '산탄' 특성 — 설계와 계획

**날짜·상태:** 2026-10-06. 설계 승인(K1~K12 확정). 리뷰 반영. Phase 1 완료(Unity EditMode 통과). Phase 2 완료(Unity EditMode Core 테스트 통과). Phase 3 완료. 전체 Phase 완료(에셋·fixture·View·UI는 인계). 브랜치 `feature/Cheon/refactor/core-system`, Claude는 커밋하지 않는다. Phase마다 멈추고 보고한다.

**범위(사용자 결정):** Core 계산 + EditMode 테스트 + DB 변환(UnitTraitData·Authoring·`BattleDefinitionAdapter`). 유닛 에셋(`Unit_ShotGun`·`Unit_Akimbo`)에 특성 붙이기, Dev fixture(`DevBattleInitializationSource`), View 표시·연출은 하지 않고 §6에 요청으로 남긴다.

## 1 기획 규칙 (사용자 확정)

| 특성 | 규칙 |
|---|---|
| 난사 ① | 첫 1회 이동 뒤 바로 다음 공격 코스트 -1. 공격 코스트가 이미 1 이하이면 적용하지 않음 → **기존 `MoveAttackDiscount`로 충족, 변경 없음** |
| 난사 ② | 적 캐릭터를 처치하면 맵 전체의 랜덤한 적 캐릭터 하나를 추가 공격한다. 처치 1명당 1회. 추가 공격으로 처치해도 다시 이어지며 횟수 제한은 없다. 피해는 기본 공격과 같다. 장애물·구조물은 대상이 아니다. 남은 적이 없으면 남은 추가 공격은 취소한다. 미리보기에는 넣지 않는다 |
| 산탄 | 공격 범위는 그대로(ForwardCone, 사거리 2). 앞 1칸(거리 1)은 150%, 그 뒤 가로 3칸(거리 2)은 50%. 반올림은 기존 배율과 같은 half-up |

## 2 현재 구조에서 필요한 것

- 특성 하나는 `TraitKey` → Spec(Contracts) → Definition·RuntimeState·Rule·EffectRule·EffectStateSnapshot(Domain) → Registry·Factory·Provider·Codec·`CombatCommandEffects.Capture`(Application) → StatusSnapshot(Contracts)로 이어진다. 기존 Knockback·PiercingFalloff와 같은 모양으로 더한다(O: 등록 추가).
- 공격 효과는 `ICombatEffectRule`의 capability 인터페이스와 `CombatEffectEngine`의 집계 메서드 한 쌍으로 노출된다. 지금 있는 capability로는 두 특성을 표현할 수 없다.
  - 공격력 보정(`IEffectAttackDamageRule`)은 모든 대상 공통의 **고정 가산**이다. 대상별 배율이 없다.
  - 처치를 계기로 하는 capability가 없다.
- 공격 처리(`CombatCommandCalculator.Calculate`의 Attack 경로)는 대상 선택 → 직접 피해 → 장애물 정산 → 밀치기 → 가시밭 → 성공 후처리 → 피격 체력 고정 → 강제 도착 순이다.
- 캐릭터에는 "보이지 않음" 개념이 없다. `VisibleTo`는 타일(지뢰 등)에만 적용된다. 따라서 "안 보이는 적 제외"는 현재 별도 처리가 필요 없다(§5 참고).

## 3 설계

### 3.1 산탄 = 새 특성 `DistanceDamageScale` (TraitKey 60)

| 항목 | 내용 |
|---|---|
| 설정 | `PercentByDistance` (int 배열, 0번 = 거리 1). 샷건 값 `[150, 50]`. 각 값 0 이상, 길이 1 이상. 목록은 공격이 닿는 모든 거리(스플래시가 있으면 사거리+1)를 덮어야 한다. 목록 밖 거리의 대상을 맞히면 `InvalidOperationException`(K8, 기본값으로 메우지 않는다) |
| 거리 | 공격자 Anchor와 대상 칸의 체비셰프 거리. 공격은 단일 칸 캐릭터만 허용하므로(`DeferredMultiCellUnit`) 캐릭터는 Anchor, 구조물은 점유 칸 중 최소 거리. ForwardCone 사거리 2에서 앞 1칸 = 거리 1, 가로 3칸 = 거리 2이고, 원래 기획의 3×3/5×5 표현과도 같다 |
| capability | `IEffectTargetDistanceDamageRule.EvaluateTargetDistanceDamage(input, TargetDistanceDamageEffectContext(distance))` → `EffectTargetDistanceDamageContribution(origin, percent)`. 엔진 `EvaluateTargetDistanceDamage`는 기여를 곱해 `Numerator/Denominator`를 돌려준다(기여 없음 = 1/1). 평가는 상태를 바꾸지 않는다 |
| 적용 위치 | 대상별 피해 반복문(일반·관통 두 곳)에서 `CombatDamageCalculator.Attack`에 넘기기 직전. 관통 감쇠가 `commonDamage`를 줄이는 자리와 같다. 반올림은 `CategoryMatchupDamageCalculator.Apply`(half-up)를 그대로 쓴다(SSOT: 배율 반올림 규칙은 한 곳) |
| 조합 순서 | 관통: 감쇠(half-up) → 거리 배율(half-up). 스플래시: 거리 배율(half-up) → 스플래시 비율(기존 버림). 관통 대상 수 예산은 기존대로 거리 배율 전 공통 피해로 정한다 |
| 적용 대상 | 이 공격의 모든 직접 대상(지정·추가, 캐릭터·구조물). 피해 모양에 붙는 규칙이라 대상 종류로 나누지 않는다 |
| 미리보기 | 실행과 같은 계산이므로 `AttackTargetPreview.DamageBeforeMatchup`에 배율 적용값이 그대로 보인다 |
| 상태 | 없음. StatusSnapshot은 PiercingFalloff처럼 설정값(`PercentByDistance`)을 싣는다 |

예시(상성·방어 없음): 공격력 1 → 앞 1칸 2, 뒤 3칸 1. 공격력 2 → 3, 1. 공격력 3 → 5(4.5 half-up), 2(1.5 half-up).

### 3.2 난사 ② = 새 특성 `KillChainAttack` (TraitKey 70)

난사 ①(`MoveAttackDiscount`)과 키를 나눈다. 두 효과는 계기·상태·계산이 모두 달라 한 규칙에 묶으면 단일 책임이 깨진다. 아킴보는 특성 두 개를 갖는다(데이터 조합). View는 두 키를 '난사' 하나로 묶어 보여 줄 수 있다(§6).

| 항목 | 내용 |
|---|---|
| 설정 | 없음(무제한·기본 공격 피해가 확정 규칙). capability `IEffectKillChainAttackRule.EvaluateKillChainAttack(input)` → `EffectKillChainAttackContribution(origin)`. 엔진 `EvaluateKillChainAttack`은 기여 목록을 돌려주고, 하나 이상이면 활성. 여러 기여가 있어도 처치 1명당 1회(중첩 안 함) |
| 계산 위치 | 새 `Application/Combat/KillChainAttackCalculator`(internal static). `CombatCommandCalculator`에는 호출 한 줄과 인자 전달만 더한다(God method 확대 방지) |
| 기본 결과 고정 | 기본 공격의 `AttackHitResult`는 지금처럼 피격 체력(`hitHealth`) 고정 시점에 만든다. 추가 공격 피해는 섞지 않는다 |
| 호출 시점(K10) | 강제 도착 처리 뒤, `AttackResult` 생성 전. 밀려난 칸의 지뢰 등 도착 효과로 죽은 적도 처치로 센다. 공격 중 강제 도착은 밀치기에서만 생기므로, 밀치기가 없는 아킴보는 이 결정과 무관하다(밀치기+난사 조합 유닛에만 영향) |
| 처치 수 | 명령 시작 때 살아 있던 적대 캐릭터 중 호출 시점에 죽은 수 |
| 대상 후보 | 투영 상태에서 필드에 있고 살아 있는 적대 캐릭터(`HostilityPolicy`), EntityId 순 정렬. 구조물·장애물·아군·중립 제외 |
| 반복 | `남은 횟수 = 처치 수`. 1회마다 남은 횟수 -1, 그 추가 공격으로 처치하면 +1. 적 수가 유한하므로 반드시 끝난다 |
| 중단 조건(P1) | 시작 전과 매 타격 전에 확인하고, 하나라도 해당하면 남은 횟수를 모두 취소한다: 공격자가 죽었거나 필드에 없음, 전투 종료(`BattleOutcomeCalculator.Resolve`가 종료), 후보 없음. 공격자 사망 뒤 공격은 허용하지 않는다. 비용은 타격당 공격자 조회 1회 + 유닛·구조물 수에 비례하는 승패 판정 1회이고, 타격 수는 적 수 이하다 |
| 피해 | 기본 공격과 같은 경로: `commonDamage`(특성 가산·영혼 공격력 포함) → `CombatDamageCalculator.Attack`(역할 Designated, 100%, 충격 원점 = 공격자 Anchor, 상성·피격 경감·지형 포함) → `ApplyHeldDefense`(새 공격 그룹). 산탄 거리 배율·관통 감쇠는 적용하지 않는다 |
| 기록(P3) | 추가 공격마다 그 시점에 순서대로 남긴다: ① `CombatEffectsEvent`(발동 `TraitActivationSnapshot(공격자, 대상, KillChainAttack, TraitActivationKind.KillChainAttack, 최종 피해)`, 피해 0이어도 남김) → ② 보유 방어 소모 시 `CombatHeldEnvironmentEvent`(기존 `ApplyHeldDefense`) → ③ 체력이 바뀌면 `CombatCharacterEvent`(HealthChanged/Died, 원점 = 공격자). 추가 공격 발동은 `AttackResult.TraitActivations`에 넣지 않는다(`CombatAttackEvent`가 기본 피해 사건보다 앞에 삽입되므로 한꺼번에 노출되는 것을 막는다. 발동 기록은 이벤트 한 곳). 사망 위치는 `deathAnchors`, 영혼·사망·승패는 기존 `Finish`가 투영 상태로 한 번 처리 |
| 랜덤(K12) | 기존 전투 공용 난수 상태 `EnvironmentProgress.RandomState`(고정 LCG `EnvironmentRandom`, 토네이도·깃발 드롭과 공유)를 `MovementProjection.ChooseRandom`으로 쓴다. 상태는 Buffer에 저장되므로 실행·기록 재생에서 같은 대상이 나오고, 난수 출처는 하나로 유지한다(SSOT). 후보가 1명이면 난수를 쓰지 않는다. 난사가 난수를 쓰면 이후 토네이도·깃발 결과 순서가 바뀐다(공유 난수의 정상 동작). 계획 초안의 명령별 `System.Random` 시드는 사용하지 않는다 |
| 미리보기 | `publicPreview`(View 미리보기·적 AI 평가)에서는 추가 공격을 계산하지 않는다. 미리보기 결과와 실행 결과는 추가 공격만큼 다르다(사용자 결정) |

### 3.3 DB 변환

| 항목 | 내용 |
|---|---|
| `UnitTraitData` | `DistanceDamageScaleTraitData(int[] percentByDistance)`(복사 보관, 검증 = 길이 ≥1·값 ≥0), `KillChainAttackTraitData()`(검증 없음) |
| Authoring | `DistanceDamageScaleTraitAuthoring`(`[SerializeField, Min(0)] int[] percentByDistance = { 150, 50 }`), `KillChainAttackTraitAuthoring`(필드 없음) |
| 변환 | `BattleDefinitionAdapter`의 trait switch에 두 줄 |

## 4 SSOT·SOLID·FW 점검

| 항목 | 판단 |
|---|---|
| SSOT | 배율 반올림은 기존 `CategoryMatchupDamageCalculator.Apply` 하나. 거리 배율 값은 DB → Spec → Definition 한 경로. 추가 공격 피해는 기존 `CombatDamageCalculator.Attack`을 재사용하고 새 산식을 만들지 않는다. 런타임 상태는 Buffer가 계속 소유(새 상태 없음) |
| S | 특성 규칙(Domain)은 값만 내고, 추가 공격 진행은 `KillChainAttackCalculator`, 공격 전체 흐름은 기존 계산기가 맡는다. 난사 두 효과는 키를 나눈다 |
| O | 새 TraitKey·Registration·capability 추가. 기존 특성 코드는 바꾸지 않는다. 공용 enum(`TraitKey`, `TraitActivationKind`) 추가는 §6에 공유 |
| L·I | capability 인터페이스는 각 1메서드. 평가 메서드는 상태 불변을 지킨다 |
| D | Domain은 Application·Buffer를 모른다. 난수는 기존 전투 난수 상태 하나만 쓴다(K12) |
| P1 | 새 InGame Data는 순수 C#(`using UnityEngine` 없음). Authoring만 Unity 직렬화 |
| 과한 추상화 | 새 난수 도구를 만들지 않고 기존 `ChooseRandom`을 재사용한다 |

## 5 결정 기록 / 남은 확인

| ID | 내용 | 상태 |
|---|---|---|
| K1 | 반올림 = 기존 half-up | ✅ 사용자 |
| K2 | 처치 1명당 1회, 연쇄 무제한 | ✅ 사용자 |
| K3 | 범위 = Core + 테스트 + DB 변환 | ✅ 사용자 |
| K4 | 난사 ②를 별도 TraitKey로 분리(§3.2) | ✅ 사용자 |
| K5 | 추가 공격은 피격 체력 고정 뒤에 계산하고, 기본 공격 `AttackHitResult`에는 넣지 않으며 Health 이벤트와 TraitActivation으로만 기록(§3.2) | ✅ 사용자 |
| K6 | "안 보이는 적 제외"는 현재 캐릭터 은신 개념이 없어 별도 처리 없음. 은신이 생기면 후보 필터에 더한다 | ✅ 사용자 |
| K7 | 산탄 배율은 구조물 대상에도 적용(§3.1). 리뷰에서 기획 결정으로 다시 열었고 사용자가 적용으로 확정 | ✅ 사용자 |
| K8 | 거리 목록 밖 대상은 100%로 메우지 않고 예외(설정 오류를 드러냄) | ✅ 사용자 (b) |
| K9 (리뷰 P1) | 연쇄 시작 전·매 타격 전에 공격자 생존·필드·전투 종료 확인, 사후 공격 없음 | ✅ 사용자 |
| K10 (리뷰 P2) | 연쇄 호출 시점 = 강제 도착 처리 뒤(안 A, 도착 효과 사망 포함). 현재 유닛 구성에서는 안 B와 결과 차이 없음 | ✅ 사용자 |
| K12 | 난사 난수 = 기존 전투 난수 상태(`EnvironmentProgress.RandomState`). 초안의 명령별 시드 대신 | ✅ 사용자 |
| K11 (리뷰 P3) | 추가 공격별 발동 → 방어 소모 → 체력 이벤트 순서 기록, `AttackResult`에는 넣지 않음 | ✅ 사용자 |

## 6 Core 밖 요청 (구현하지 않음, Phase 3에서 전달 문서에 기록)

- **DB 에셋:** `Unit_ShotGun`에 `DistanceDamageScaleTraitAuthoring [150, 50]`, `Unit_Akimbo`에 `KillChainAttackTraitAuthoring` 추가. View `TraitVisual` 테이블에 두 키 외형.
- **Dev fixture:** `DevBattleInitializationSource`의 ShotGun·Akimbo에 같은 Spec 추가.
- **View:** 추가 공격 연출은 이벤트 목록의 `CombatEffectsEvent`(`KillChainAttack`, 대상·피해)를 타격 시작으로 보고, 뒤따르는 방어 소모·체력 이벤트를 그 타격의 결과로 재생한다. `AttackResult`에는 추가 공격이 없다. 미리보기에는 나오지 않는다. 산탄 배율은 미리보기 피해에 이미 반영된다. 공용 enum 값 추가: `TraitKey.DistanceDamageScale = 60`, `TraitKey.KillChainAttack = 70`, `TraitActivationKind.KillChainAttack = 70`.

## 7 Phase

각 Phase 끝에 Unity EditMode 테스트 실행을 요청하고 멈춘다. 새 `.cs`는 `.meta`와 함께 만든다.

컴파일 확인은 두 단계로 나눠 기록한다. ① MSBuild 컴파일: Unity csproj는 소스를 `Compile Include`로 열거하므로, Unity가 프로젝트를 다시 생성해 새 파일이 들어갔거나 `CustomAfterMicrosoftCommonTargets`로 새 파일을 명시적으로 더한 빌드여야 한다. 어느 쪽인지 기록한다. ② Unity EditMode 통과: ①과 별개이며 사용자 실행 결과로만 기록한다.

### Phase 1 — 산탄 Core

1. `TraitKey.DistanceDamageScale = 60`.
2. Contracts: `DistanceDamageScaleTraitSpec`, `DistanceDamageScaleTraitStatusSnapshot`.
3. Domain: Definition, RuntimeState, EffectStateSnapshot + `TraitEffectStateCodec`, Rule, EffectRule.
4. CombatEffects: `TargetDistanceDamageEffectContext`, `EffectTargetDistanceDamageContribution`/`Evaluation`, `IEffectTargetDistanceDamageRule`, `ICombatEffectEngine`·`CombatEffectEngine.EvaluateTargetDistanceDamage`.
5. Application: Factory 2개, `TraitRegistry.CreateDefault`, `TraitEffectProvider`, `CombatCommandEffects.Capture`, `CombatCommandCalculator` 대상별 적용(일반·관통).
6. 테스트 `DistanceDamageScaleTraitTests`: 계약 왕복(Registry·Factory·Codec·Status), 엔진 집계(거리 1/2/3, 기여 없음), 통합(ForwardCone 사거리 2, 공격력 1·2·3에서 앞/뒤 피해, 미리보기 = 실행), 특성 없는 유닛 회귀.

### Phase 2 — 난사 ② Core

1. `TraitKey.KillChainAttack = 70`, `TraitActivationKind.KillChainAttack = 70`.
2. 특성 계층(Phase 1과 같은 목록) + `IEffectKillChainAttackRule`, `EffectKillChainAttackContribution`/`Evaluation`, 엔진 `EvaluateKillChainAttack`.
3. `KillChainAttackCalculator` + `CombatCommandCalculator` 호출.
4. 테스트 `KillChainAttackTraitTests`: 계약 왕복, 처치 시 추가 공격 1회와 피해 = 기본 공격, 연쇄 처치, 한 번에 2명 처치 → 2회, 적 없음 → 취소, 미처치 → 추가 공격 없음, 구조물·아군 제외, 같은 입력 → 같은 대상(결정성), 미리보기에는 없음, 특성 없는 유닛 회귀. 리뷰 추가: 공격자 사망·전투 종료 시 중단, 타격별 이벤트 순서(발동 → 방어 소모 → 체력), 피해 0이어도 발동 기록, 추가 공격의 보유 방어 소모, AP 추가 소모 없음, 영혼 중복 생성 없음, `AttackResult.Hits`·`TraitActivations`에 추가 공격 없음, K10 결정에 맞는 도착 효과 사망 집계.

### Phase 3 — DB 변환·인계

1. `UnitTraitData`·`UnitTraitAuthoring`에 두 타입, `BattleDefinitionAdapter` 변환.
2. 테스트: Data 검증, Adapter 변환(기존 `InGameDataTableTests` 형식).
3. §6의 DB 에셋·Dev fixture·View 요청을 View 작업자에게 전달할 공유 메모로 정리한다(이 세션 사용자는 Core 작업자라 Core 변경을 View→Core 요청 문서에 "요청"으로 적지 않는다). 이 문서 §8에 실행 기록.

## 8 실행 기록

### 8.1 Phase 1 — 산탄 Core (2026-10-06, 코드 완료, Unity EditMode 대기)

- K4~K7은 사용자가 제안대로 승인했다.
- 새 파일(각 `.meta` 포함):
  - Contracts: `Commands/Traits/DistanceDamageScaleTraitSpec.cs`(Spec과 백분율 목록 검증·복사·비교 `DistanceDamageScalePercents`를 함께 둔다. Spec·Definition·Status 세 곳이 같은 검증을 쓰도록 한 곳에 모았다), `Snapshots/Traits/DistanceDamageScaleTraitStatusSnapshot.cs`
  - Domain: `Traits/Definitions/DistanceDamageScaleTraitDefinition.cs`, `Traits/States/DistanceDamageScaleRuntimeState.cs`, `Traits/Effects/DistanceDamageScaleEffectStateSnapshot.cs`, `Traits/Effects/DistanceDamageScaleEffectRule.cs`, `Traits/Rules/DistanceDamageScaleTraitRule.cs`, `CombatEffects/Contexts/TargetDistanceDamageEffectContext.cs`, `CombatEffects/Evaluations/EffectTargetDistanceDamageContribution.cs`, `…/EffectTargetDistanceDamageEvaluation.cs`
  - 테스트: `Assets/Tests/EditMode/DistanceDamageScaleTraitTests.cs`(계약 왕복 1, 엔진 거리 1/2/3 TestCase 3, 엔진 빈 기여·잘못된 스냅샷 1, ForwardCone 통합 공격력 1/2/3/20 TestCase 4(미리보기 = 실행 포함), 특성 없음 회귀 1)
- 수정: `TraitKey`(+`DistanceDamageScale = 60`), `TraitEffectStateCodec`, `CombatEffectCapabilities`(+`IEffectTargetDistanceDamageRule`), `ICombatEffectEngine`·`CombatEffectEngine`(+`EvaluateTargetDistanceDamage`), `TraitFactories`(+Factory 2), `TraitRegistry.CreateDefault`, `TraitEffectProvider`, `CombatCommandEffects.Capture`, `CombatCommandCalculator`(일반·관통 대상 반복문에서 `ScaleByTargetDistance` 적용, private helper 1개).
- 컴파일: VS 18 MSBuild로 `ProjectL.Tests.Core.csproj`(Core Contracts·Domain·Application 포함)를 새 파일을 더해 빌드했다. 오류 0, 새 경고 0(산출물은 스크래치패드). Unity EditMode 테스트는 아직 실행하지 않았다.
- 남은 확인: Unity에서 `DistanceDamageScaleTraitTests`와 기존 전투 테스트(`CombatCommandEffectTests`, `CombatCommandCalculationTests`, `PiercingFalloffTraitTests`, `TargetingPreviewParityTests`) 회귀.

### 8.2 리뷰 반영 (2026-10-06)

- 외부 정적 리뷰(P1~P3, 컴파일 문구, 조합 테스트)를 반영했다. 결정은 §5 K7~K11.
- K8 코드 반영: `DistanceDamageScaleTraitRule.EvaluatePercent`가 목록 밖 거리에서 100%를 돌려주던 것을 `InvalidOperationException`으로 바꿨다. Spec·Definition XML doc도 맞췄다.
- 테스트 갱신·추가(`DistanceDamageScaleTraitTests`): 엔진 거리 3 TestCase를 예외 테스트로 교체, 공격 경로에서 목록 밖 대상이면 실행·미리보기 모두 예외, 구조물 배율(K7), 스플래시 조합(공격력 7: 주 11 / 추가 거리 1 → 7 / 추가 거리 2 → 2, 순서가 반대면 다른 값), 관통 조합(공격력 3, 감쇠 30%, 거리 3 배율 150% → 3, 반대 순서면 4).
- 컴파일 ①: Unity가 열려 있어 csproj가 다시 생성됐고 새 파일 11개가 모두 들어가 있었다. 추가 Compile 없이 `ProjectL.Tests.Core.csproj`를 MSBuild(VS 18, 출력은 스크래치패드)로 빌드해 오류 0·새 경고 0. 컴파일 ② Unity EditMode는 아직 실행하지 않았다.
- **Unity EditMode(사용자 실행, 2026-10-06): 전체 통과.** `DistanceDamageScaleTraitTests` 14건(TestCase 포함, 앞서 보고한 13건은 오기)과 회귀 대상(`CombatCommandEffectTests`, `CombatCommandCalculationTests`, `PiercingFalloffTraitTests`, `TargetingPreviewParityTests`) 포함. Phase 1 완료.

### 8.3 Phase 2 — 난사 ② Core (2026-10-06, 코드 완료, Unity EditMode 대기)

- 결정 K12(난수 = 기존 전투 난수 상태)를 구현 직전 사용자에게 확인받았다. 나머지는 §3.2·§5 그대로.
- 새 파일(각 `.meta` 포함):
  - Contracts: `Commands/Traits/KillChainAttackTraitSpec.cs`, `Snapshots/Traits/KillChainAttackTraitStatusSnapshot.cs`
  - Domain: `Traits/Definitions/KillChainAttackTraitDefinition.cs`, `Traits/States/KillChainAttackRuntimeState.cs`, `Traits/Effects/KillChainAttackEffectStateSnapshot.cs`, `Traits/Effects/KillChainAttackEffectRule.cs`, `Traits/Rules/KillChainAttackTraitRule.cs`, `CombatEffects/Evaluations/EffectKillChainAttackContribution.cs`, `…/EffectKillChainAttackEvaluation.cs`
  - Application: `Combat/KillChainAttackCalculator.cs`(연쇄 진행·중단 조건·타격별 사건 기록)
  - 테스트: `Assets/Tests/EditMode/KillChainAttackTraitTests.cs`(16건: 계약 왕복, 엔진, 처치 1 → 추가 1·피해 20·`AttackResult` 불변·AP 3, 사건 순서, 연쇄 계속, 동시 2처치 → 2회, 미처치, 특성 없음, 아군·구조물만 남음 → 취소, 기본 공격의 본부 파괴 → 중단, 공격자 사망 → 중단(계산기 직접 호출, 생존 대조 포함 2), 배지 방어 → 피해 0 발동 + 방어 사건·체력 사건 없음, 결정성·난수 상태 진행, 미리보기 제외·난수 불변, 밤 영혼 사망당 1개·각 사망 위치)
- 수정: `TraitKey`(+`KillChainAttack = 70`), `TraitActivationKind`(+`KillChainAttack = 70`), `TraitEffectStateCodec`, `CombatEffectCapabilities`(+`IEffectKillChainAttackRule`), `ICombatEffectEngine`·`CombatEffectEngine`(+`EvaluateKillChainAttack`), `TraitFactories`(+2), `TraitRegistry.CreateDefault`, `TraitEffectProvider`, `CombatCommandEffects.Capture`, `CombatCommandCalculator`(강제 도착 루프 뒤 `publicPreview`가 아닐 때 `KillChainAttackCalculator.Resolve` 호출 3줄).
- 컴파일 ①: Unity가 아직 csproj를 다시 만들지 않아 새 파일 11개를 `CustomAfterMicrosoftCommonTargets`로 명시해 `ProjectL.Tests.Core.csproj`를 MSBuild(VS 18, 출력 스크래치패드)로 빌드했다. 오류 0·새 경고 0.
- 보조 실행(참고용, Unity 통과를 대신하지 않음): 스크래치패드의 NUnitLite 3.13.3 실행기(.NET 10)로 빌드된 테스트 DLL을 돌렸다. `KillChainAttackTraitTests` 16 + `DistanceDamageScaleTraitTests` 14 = 30건 통과. 전체 1378건 중 1341 통과, 37건은 모두 `MissingMethodException`(Unity NUnit 3.5와 3.13의 `Has.Member`·`Does.Contain`·`Is.InRange` 서명 차이)로 실행 환경 문제이며 로직 실패가 아니다.
- **Unity EditMode(사용자 실행, 2026-10-06): Core 테스트(`ProjectL.Tests.Core`) 전체 통과.** Phase 2 완료.
- 참고: 전체 실행 때 `ProjectL.Tests.Framework`의 `ArtifactPopupBootSmokeTests`(커밋 `5376b174`, 아티팩트 팝업)가 의도적으로 Boot부터 Play 모드에 들어가 실제 게임을 실행한다(`[UnityTest]` + `EnterPlayMode`). 이 테스트가 도중에 멈춘 원인은 이번 작업 범위 밖이라 조사하지 않았다.

### 8.4 Phase 3 — DB 변환·인계 (2026-10-06, 코드 완료, Unity EditMode 대기)

- 수정(새 파일 없음):
  - `DataBaseSystem/Data/InGame/Table/Unit/Trait/UnitTraitData.cs`: `DistanceDamageScaleTraitData`(입력 사본 보관, `Validate` = 목록 1개 이상·값 0 이상, 위반 시 `InvalidOperationException`), `KillChainAttackTraitData`(검증 없음). 순수 C#(P1).
  - `…/UnitTraitAuthoring.cs`: `DistanceDamageScaleTraitAuthoring`(`[SerializeField, Min(0)] int[] percentByDistance = { 150, 50 }`), `KillChainAttackTraitAuthoring`(필드 없음).
  - `CoreSystem/CombatSystem/BattleDefinitionAdapter.cs`: trait switch에 두 줄.
- 테스트: `InGameDataTableTests`(기본값 유효·샷건 기본값 `[150, 50]`, null·빈 목록·음수 거부 TestCase 3, 입력 사본 보관), `BattleDefinitionAdapterTests`(두 특성 변환 1).
- 인계: [View 작업자 공유 메모](../handoffs/2026-10-06-akimbo-shotgun-traits-view-share.md)(미전달). DB 에셋 부착, `TraitVisual` 키 60·70, Dev fixture, 추가 공격 연출 계약.
- 컴파일 ①: Unity가 Phase 2 파일까지 csproj에 반영한 상태에서 `ProjectL.Tests.Framework.csproj`(Systems·Core 포함)를 MSBuild로 Rebuild. 오류 0, 변경 파일에서 새 경고 0. Framework 테스트는 Unity 에셋에 의존해 Unity 밖 실행은 하지 않았다.
- **Unity EditMode(사용자 실행, 2026-10-06): `InGameDataTableTests`·`BattleDefinitionAdapterTests`·`DataBaseAssetTests` 통과.** Phase 3 완료.
- 인계 정리: UI(특성 표시 `TraitVisual` 키 60·70)는 사용자가 UI 작업자에게 따로 요청하기로 해 공유 메모에서 빼고, 요청 문구를 대화로 전달했다.
