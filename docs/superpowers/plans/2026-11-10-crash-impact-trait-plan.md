# 작렬(CrashImpact) 특성 Task 계획

- 작성: 2026-11-10, Core(천명) 세션
- 설계: [crash-impact-trait-design](../specs/2026-11-10-crash-impact-trait-design.md)
- 사용자 결정: 설계 P1~P12 그대로(2026-11-10 "그래"). View 쪽은 담당자에게 넘길 인계 문서로 처리한다(설계 §5).
- 진행 방식: CI-a → CI-b → CI-c를 이어서 한다. Phase가 끝날 때마다 Core·CoreIntegration 전체 시험을 백그라운드로 걸어 두고 다음 Phase로 넘어간다. 마지막에 한 번 더 전체를 확인한다.
- 구조 기준: [기능별 변경 독립성 지침](사용자 제공 `feature-change-locality-guidelines.md`). 공통 공격 엔진(`CombatEffectEngine` 넉백 평가)과 기존 넉백 실행기·피해 적용 입구를 그대로 쓰고, 작렬만의 규칙은 한 곳에 모은다.

## 0. 대표 변경 사례로 본 구조 (지침 §2)

| 변경 사례 | 고치는 곳 | 알아야 하는 것 | 알 필요 없는 것 |
|---|---|---|---|
| 작렬 % 기본값·거리를 바꾼다 | 해머 에셋 값만 | 없음 | 코드 |
| 부딪힘 피해 식을 바꾼다(예: 방어 적용 방식) | `CrashImpactCollisionRules` 한 파일 | 피해 적용 입구 `ApplySteps`의 계약(보호막·사망 처리) | 넉백 경로 계산, 이동 칸 효과 |
| 넉백을 막는 지형 종류를 더한다 | `ForcedMovementBlockers` 한 함수 | 강제 이동 진입 규칙 | 작렬 피해 식 (부딪힘 종류는 그 결과를 읽기만 함) |
| 다음 넉백 변형 특성(예: 부딪히면 기절)을 더한다 | 그 특성의 효과 규칙 + 부딪힘 값 소비 한 곳 | 넉백 기여 계약(`EffectKnockbackContribution`)·부딪힘 사실 | 작렬 내부 |

판단:
- **필요한 연결**: 부딪힘은 넉백이 "막혀서 멈춘 순간"에만 생기므로 넉백 실행기 한 줄(대상 하나를 민 뒤 부르기)은 피할 수 없다.
- **불필요한 결합을 만들지 않을 것**:
  - 막은 이유를 부딪힘 코드가 따로 다시 판정하면, 진입 규칙이 바뀔 때 두 곳을 고쳐야 한다. → 진입 판정을 "막은 것" 값을 돌려주는 함수 하나로 바꾸고, 진입 허용과 부딪힘 종류가 둘 다 이것을 읽는다.
  - 피해 식을 넉백 실행기 안에 쓰면, 넉백만 고쳐도 피해를 다시 확인해야 한다. → 부딪힘 처리는 새 파일로 둔다. 넉백 실행기는 부르기만 한다.
  - 보유 카드 표시 스냅샷(`OwnedCharacterStatsSnapshot`)에 특성마다 설명 칸을 늘리는 패턴은 따르지 않는다. 설명 값은 이미 `TraitSettingsSet`(Spec)으로 나가고, View 설명 토큰도 Spec을 읽는다.

## 1. 바뀌는 곳 · 같이 확인할 경로 · 다시 돌릴 시험 · 확인하지 않아도 되는 것

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 특성 값·연결(CI-a) | 특성 등록·정의·상태 코덱·DB 변환 | `IntimidationTraitTests`·`RegenerationTraitTests`의 등록 수(43 → 44), 특성 계약 시험 | 공격 계산 |
| 진입 판정 함수화(CI-b) | 넉백·충격 볼트·끌어오기 사전 이동·시체 밀기(같은 `CanEnterUnit`) | `KnockbackPathTests`·`KnockbackTraitTests`·`ImpactBoltTests`·`PullAttackTests`·`ForcedDisplacementTests` | 일반 이동(다른 규칙 `BlocksVoluntaryEntry`) |
| 넉백 기여에 부딪힘 값(CI-b) | 넉백 평가(`EvaluateKnockback`)·적 AI 넉백 거리 | `CombatEffectEngineTests`·`EnemyForcedMovementAssessmentTests` | 다른 효과 평가 |
| 부딪힘 처리(CI-b) | 피해 적용 입구·사망 위치·전투 종료 | 새 `CrashImpactTests` | 이동 칸 효과(가시·지뢰) |
| 발동 종류·변경 증거(CI-c) | `CombatChangeEvidenceReader` | `CombatChangeEvidenceVariantTests` | — |
| 넉백 요약에 부딪힘(CI-c) | 공격 결과·미리보기 조립 | `AttackKnockbackSnapshotTests`·`AttackPreviewParityTests` | — |

공용 등록 지점(동시 작업 충돌 주의): `TraitKey`, `TraitRegistry`, `TraitSpecAdapter`, `TraitEffectStateCodec`, `CombatCommandEffects`, `TraitEffectProvider`, `MainSpecializationDefinitionSnapshot`, `TraitActivationKind`. 모두 한 줄씩 더하는 등록 계약이다.

## 2. Phase CI-a — 특성 값·연결 (부딪힘 없음)

끝나면: 해머가 기본 3칸 넉백을 한다. 부딪힘 피해는 없다.

| Task | 내용 |
|---|---|
| a1 | `TraitKey.CrashImpact = 520` |
| a2 | DB: `CrashImpactTraitAuthoring`(직렬화 `distanceCells` 3, `wallDamagePercent` 10, `unitDamagePercent` 10)·`CrashImpactTraitData`(검사: 거리 ≥ 1, % 0~100) |
| a3 | Core 계약: `CrashImpactTraitSpec`(같은 검사, `SameValuesCore`)·`CrashImpactTraitStatusSnapshot` |
| a4 | Core 도메인: `CrashImpactTraitDefinition`·`CrashImpactTraitRule`·`CrashImpactEffectStateSnapshot`·`CrashImpactRuntimeState` |
| a5 | 효과 규칙 `CrashImpactEffectRule`: `IEffectKnockbackRule` + `IEffectAssessmentHarmRule`(`ActingAttackOnly`, 기존 넉백과 같음). CI-a에서는 거리만 낸다 |
| a6 | 조립: `CrashImpactTraitFactories`, `TraitRegistry` 등록, `TraitEffectProvider`·`TraitEffectStateCodec`·`CombatCommandEffects`·`TraitSpecAdapter`·`MainSpecializationDefinitionSnapshot` 한 줄씩 |
| a7 | `CombatAttackProfile`: 작렬 + 충격 볼트 조합 거절(넉백 기여의 방향 모드가 달라 넉백 평가가 예외를 낸다. 지금 충격 볼트 + 넉백을 거절하는 줄에 작렬을 더함) |
| a8 | 해머 에셋 `traits`에 작렬 한 줄 |
| a9 | 시험: 등록 수 43 → 44(두 곳), Spec/Data 검사·DB 변환 |

## 3. Phase CI-b — 부딪힘 판정·피해

끝나면: 막혀서 멈추면 부딪힘 피해가 난다.

| Task | 내용 |
|---|---|
| b1 | `AttackForcedMovement.CanEnterUnit`의 조건을 `FindBlocker`(새 값 `ForcedMovementBlocker` — 없음/벽/구조물(ID)/캐릭터(ID))로 옮김. `CanEnterUnit`은 `FindBlocker(...).IsNone`. 맵 밖·벽·고지대 턱 → 벽 |
| b2 | 넉백 기여에 선택 값 `EffectKnockbackCollision`(벽 %, 유닛 %). 작렬 효과 규칙만 채움. 평가가 기여들 중 % 최댓값과 출처를 냄(`EffectKnockbackEvaluation.Collision`) |
| b3 | 넉백 계산기: 가장 긴 거리 기여가 여럿이면 부딪힘 값이 있는 것을 출처로 고름(CI7) |
| b4 | 새 `CrashImpactCollisionRules`(순수 식: 기준 피해·방어 뒤 피해) |
| b5 | 새 `CrashImpactCollision.ResolveSteps`: `Blocked`면 부딪힘 칸·막은 것 → 날아간 대상 피해 → (캐릭터면) 부딪힌 유닛 피해. 피해는 `world.Damage.ApplySteps`(`EnvironmentDamage`, 근원 = 부딪힘 칸 + 공격자 + 새 공격 그룹, 보유 방어 그룹 전달), 장부 `AddDamage`, 포효 피격 수, 죽으면 사망 위치, 전투가 끝나면 멈춤 |
| b6 | 넉백 계산기가 대상 하나를 민 뒤(`Record` 다음) `CrashImpactCollision.ResolveSteps`를 부름 |
| b7 | 장부: 부딪힌 유닛이 공격 대상이 아니면 미리보기 보조 행에 들어가게(`AddCollisionVictim`, 넉백 보조 행과 같은 꼴, 피격 체력 고정 포함) |
| b8 | 시험 `CrashImpactTests`(설계 §7 통합 사례)·`ForcedMovementBlocker` 판정 사례 |

## 4. Phase CI-c — View 신호·미리보기·문서

| Task | 내용 |
|---|---|
| c1 | `TraitActivationKind.CrashImpactCollided = 410`. 부딪힘 처리에서 피해 앞에 발동 사건(출처 = 해머, 대상 = 날아간 유닛 + 부딪힌 유닛, 칸 = 부딪힘 칸, 근원 = 부딪힘 근원, Magnitude = 날아간 대상 기준 피해) |
| c2 | `CombatChangeEvidenceReader`에 410 갈래(대상 체력·보호막 변화가 있으면 Effect, 없으면 Observation — 자폭과 같은 꼴) |
| c3 | `AttackKnockbackSnapshot`에 선택 값 `Collision`(`AttackKnockbackCollision`: 종류 Wall/Structure/Character, 부딪힌 개체 ID, 부딪힘 칸). 생성자 끝 선택 인자, `Blocked`일 때만 허용 |
| c4 | 시험: 변경 증거 샘플, 넉백 요약 계약, 미리보기에 부딪힘·예상 피해 |
| c5 | View 인계 문서 `docs/superpowers/handoffs/2026-11-10-crash-impact-view-handoff.md`(받을 신호, 특성 외형·설명 토큰·발동 연출 줄, 확인할 것) |
| c6 | 설계 문서 구현 메모 |

## 5. 커밋 묶음 (커밋은 사용자)

- 새 `.cs`는 `.meta`와 함께(Unity가 만듦 — 배치 실행 뒤 생긴 `.meta` 포함).
- CI-a의 DB 코드(`CrashImpactTraitAuthoring`)와 해머 에셋은 한 커밋(없으면 에셋 직렬화가 깨짐).
- 배치 시험 실행이 바꾼 부수 파일(FMOD 카탈로그·글꼴 에셋 등)은 되돌린다.

## 6. 실행 기록

### 6.0 기준 시험 (HEAD `4a9fb2d70`, 배치 실행)

- Core 5524 중 실패 1, CoreIntegration 1066 중 실패 112.
- 112 중 101은 이 실행 도중 해머 에셋을 고쳐 섞인 것이다("[UnitData] Trait is null. Key: Unit_DynamiteHammer" — 그 시점 컴파일본에 작렬 형식이 없음). 기준에서 뺐다.
- 실제 기준 실패 12개: Core `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`, CoreIntegration `CombatCameraZoomPoseTests`(Cinemachine 형식) 1, `CombatStructureInspectionTests` 1, `DeckMainChangePolicyTests` 8, `DeckSlotInfoPanelTests` 1. 모두 작렬과 무관한 영역이다.

### 6.1 CI-a (코드 완료)

- 계획 a1~a9대로. 계획과 다른 점: a5의 효과 규칙이 처음부터 부딪힘 값까지 내도록 썼다(넉백 기여에 `EffectKnockbackCollision`을 같이 더함 — b2를 앞당김). CI-a 시점에는 아무도 그 값을 읽지 않아 동작은 "3칸 넉백"뿐이다.
- 배치 실행 컴파일 오류 없음.
- 시험: 새 실패는 `LegacyEnhancementRetirementTests.SurvivingRegistry_PreservesAllNewAndDeferredIdentities` 하나(사용 가능 특성 40 → 41, 220 이상 30 → 31 — 특성이 하나 늘어난 직접 결과). 기대값을 맞췄다. 나머지 실패 21개는 2026-11-10 22:23 편집기 실행에도 있던 것과 이름이 같다.

### 6.2 CI-b·CI-c (코드 완료)

- 계획 b1~b8·c1~c6대로. 계획과 다른 점:
  - 방어 식을 `TargetDamagePipeline.ApplyPhysicalDefense`로 꺼냈다(공격과 부딪힘이 같은 식을 쓰게, 계획 b4에 없던 SSOT 정리).
  - 부딪힘 종류 열거는 공개 `AttackKnockbackCollisionKind` 하나를 내부 `ForcedMovementBlocker`도 쓴다.
  - `CombatChangeEvidenceReader`에 410 갈래를 더하면서 `CombatChangeEvidenceVariantTests`에 샘플 하나를 더했다.
- 시험 1차(전체): 작렬 29개 중 28개 통과. 실패 둘을 고쳤다.
  - `CrashImpactTests.LongerKnockback…`: 시험이 틀렸다(넉백 5칸이면 (6,4)까지 완주라 (7,4) 벽은 부딪힘이 아님). 벽을 (6,4)로 옮겼다.
  - 고정 비교 시험 5개(`AttackPreviewParityTests` 둘, `AttackPreviewResumptionTests` 셋): 넉백 요약에 공개 속성 `Collision`이 생겨 직렬화 문자열에 `"Collision":null`이 끼었다. 동결 기준 JSON 두 파일(`docs/superpowers/reports/2026-11-07-enemy-utility-ai/phase-2-preview-reference.json`·`phase-2-movement-reference.json`)의 넉백 요약 객체 75곳(기록 8개)에 그 한 칸만 넣었다. 다른 내용은 그대로다.
- 시험 2차(전체, 마지막): Core 5553 중 실패 1, CoreIntegration 1066 중 실패 20. 작렬 29개 모두 통과. 실패 21개는 모두 작업 전(11-10 22:23 편집기 실행)과 이름이 같다(`TraitActivationOutcomeTests.Taunt…(True)`, `DataBaseAssetTests` 7, `RunGoldFacilityTests` 2, `DeckMainChangePolicyTests` 8, 카메라·구조물·덱 정보 UI 각 1).
- 새 `.cs` 열넷 모두 `.meta`가 생겼다(배치 실행이 만듦). 배치 실행이 바꾼 FMOD 카탈로그·글꼴 에셋은 되돌렸다.
- View 인계 [crash-impact-view-handoff](../handoffs/2026-11-10-crash-impact-view-handoff.md) 작성(미전달).

### 6.3 남은 것

- 사용자 커밋(코드·`.meta`·해머 에셋·시험·동결 기준 JSON 둘·문서). 해머 에셋과 `CrashImpactTraitAuthoring.cs`는 한 커밋.
- View 인계 전달, View 쪽 특성 외형·설명 토큰·발동 연출 줄.
- 실제 게임 재생 확인(해머로 벽·유닛·구조물 쪽 적을 쳐 보기)은 하지 않았다.
