# 선인장 캐논 가시 폭탄·가시 장판 삭제 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: 승인 후 `superpowers:executing-plans` 또는 사용자가 선택한 `superpowers:subagent-driven-development`로 Task 단위 실행한다. 현재 사용자 지시 범위는 Phase 4이며, 해당 Phase에서 멈춘다. 각 Phase 종료 시 멈추고 보고한다. 커밋·push 금지.

**Goal:** 기존 공격 중심에 가시 폭탄을 자동 부착하고, 밀린 위치에서 연쇄 폭발하도록 구현하며 기존 가시 장판의 실행·상태·표시·저작 연결을 제거한다.

**Architecture:** `CombatCommandState`의 폭탄 사실과 명령 단위 `ThornBombResolution`을 사용한다. 발리스타의 중심 넉백 계산과 기존 피해/이동 실행 책임을 재사용한다. 부착·연쇄 규칙은 전용 기능에 모으고 기존 공격/턴/사망 경계는 명중·시점 사실과 호출 순서만 연결한다.

**Tech Stack:** Unity 6000.0.73f1, C#, 기존 ProjectL Core.Contracts/Domain/Application 및 Systems, JMH Framework, 기존 DB SO·Buffer·playback.

**Spec:** [2026-11-17-cactus-cannon-thorn-bomb-design.md](../specs/2026-11-17-cactus-cannon-thorn-bomb-design.md). R01~R14는 사용자 확정, P01~P11은 설계 검토 제안이다. 구현자는 둘 다 읽는다.

**상태:** 2026-11-17 Phase 4 정적 확인·집중 코드 리뷰·인계 완료, 결정 대기 항목은 §7 Phase 4 실행 기록. Phase 1~3 결과는 §3~§5에 기록한다. 삭제·보존 범위와 시험 소유권은 [Phase 0 상세 기록](2026-11-17-cactus-cannon-thorn-bomb-phase0-audit.md)을 따른다.

## Global Constraints

- 계획 수립 당시에는 문서만 작성했다. 이후 사용자가 명시적으로 지시한 Phase만 구현한다.
- 커밋과 push는 사용자가 직접 한다. Codex는 커밋·push하지 않는다.
- 설계 → Task 계획 → Phase 단위 구현. 각 Phase 종료 시 멈춰 보고한다. 설계와 다른 결정은 구현 전에 묻는다.
- Core 구현은 Core 담당자 인계 또는 사용자의 명시적 Phase 지시가 필요하다. FieldCamera·Architecture 및 무관한 기능은 수정하지 않는다.
- View Test 코드는 작성하지 않는다. 전투 View MSBuild도 돌리지 않는다. Core 수정 허용을 이 제한의 예외로 해석하지 않는다.
- 기존 시험은 컴파일에 필요한 최소 호출 보정만 가능하다. 실행에서 달라지는 타인 시험의 삭제/기대값 변경은 먼저 묻는다. 이 계획은 시험 코드 신규 작성 지시가 아니다.
- 실제 Unity 컴파일·테스트·재생 결과를 구분한다. 사용자는 Phase 3에서 Core·CoreIntegration EditMode 실행을 승인했다. `DataBaseAssetTests`를 해당 범위에 포함한다.
- 옛 Missing Script 에셋 48개, `InGameEnum.cs`의 `StageEnum`, 기존 오타 식별자는 유지한다. 새 `.cs`와 `.meta`는 한 쌍으로 처리한다.
- 순수 InGame Data/Core에 Unity 타입을 넣지 않는다. 네임스페이스는 폴더 경로, Allman, 4칸, LF, UTF-8, 한국어 주석을 따른다.
- 별도 폭탄 버튼·선택 UI·클릭·입력 명령을 만들지 않는다. 기존 공격 중심에 자동 부착한다.
- `feature-change-locality-guidelines.md`의 수정·이해·검증 범위, SSOT, SOLID 5원칙, FW-001~016을 설계 §8 기준으로 검토한다.

## Review Focus

1. 0피해/희생 대행/전달 실패에서 실제 명중과 원래 중심 대상을 혼동하지 않는다. Task 2.2, V02·V03·V13.
2. 중복 예약과 연쇄 큐가 오래된 위치를 기억해 밀리기 전 폭발하거나 재부착 폭탄을 소모하지 않는다. Task 2.1~2.2, V04~V07·V15.
3. 공격·도착·턴 투영, Buffer/복사/취소/미리보기 중 하나가 폭탄 상태를 잃거나 원본을 수정하지 않는다. Task 1.2·2.3, V08·V10·V11·V16.
4. 한 폭발의 전체 피해 적용 뒤 승패를 판정하며, 종료 뒤에는 넉백·연쇄·재부착이 계속되지 않는다. Task 2.1~2.3, V09.
5. 가시 장판 삭제 후 잔여 GUID·공용 생성자·되돌리기·표시 등록이 깨지지 않고, 무관한 ThornHeart/상태 효과를 삭제하지 않는다. Task 0.2·3.2~3.3, V12·V14·V17·V18.

## 1. 파일 경로 지도

아래 약어는 정확한 저장소 상대 경로다. `C/...`처럼 표기하면 이 접두사를 결합한다. 이름과 서명은 계획상의 계약이다. 실제 반영 여부는 각 Phase의 실행 기록을 따른다.

| 약어 | 경로 |
|---|---|
| `S` | `Assets/ProjectL/Script/Systems` |
| `C` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `A` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat` |
| `K` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem` |
| `V` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem` |
| `F` | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View` |
| `D` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait` |
| `DB` | `Assets/ProjectL/DataBaseSO` |

### 신규 파일의 책임

| 파일 | 책임 |
|---|---|
| `D/ThornBombTraitAuthoring.cs`, `D/ThornBombTraitData.cs` | SO 입력과 순수 불변 수치, 기존 한 파일에 특성 클래스를 더 섞지 않음 |
| `C/Contracts/Commands/Traits/ThornBombTraitSpec.cs` | DamagePercent 및 명세 검증/비교 |
| `C/Domain/Traits/Definitions/ThornBombTraitDefinition.cs` | 검증된 특성 정의 |
| `C/Domain/Traits/States/ThornBombRuntimeState.cs`, `C/Contracts/Snapshots/Traits/ThornBombTraitStatusSnapshot.cs` | 기존 registry가 요구하는 특성 상태/표시 메타데이터. 부착 폭탄 목록을 저장하지 않음 |
| `A/Traits/ThornBombTraitFactories.cs` | 기존 registry에 들어가는 definition/runtime-state 생성 |
| `C/Domain/Traits/Effects/ThornBombEffectRule.cs` | 필수 EffectRuleFactory에 연결하는 부착 능력, 기존 무상태 snapshot 재사용 |
| `C/Domain/Traits/Rules/ThornBombRules.cs` | 순수 비율·만료·부착/폭발 범위 규칙, 기존 ICombatTraitRule 계약 충족 |
| `C/Contracts/Combat/ThornBombSnapshot.cs`, `ThornBombStateSet.cs` | 불변 폭탄 값과 유일한 게시용 상태 집합 |
| `C/Contracts/Combat/ThornBombEvents.cs` | attached/detonated 이벤트와 기폭 이유 enum |
| `C/Contracts/Combat/ThornBombExplosionPreview.cs` | 연쇄 순서와 위치를 보존하는 미리보기 값 |
| `C/Domain/Movement/ImpactCenterGeometry.cs` | 발리스타/폭탄 공용 중심 제외·방향 계산 |
| `A/ForcedMovementExecution.cs`, `A/ForcedMovementRequest.cs` | 기존 공격 강제 이동 중 공통 실행과 그 입력/결과 값. 별도 Framework 서비스 아님 |
| `A/ThornBombs/ThornBombResolution.cs` | 명령 수명의 상태·예약 큐·부착·최종 스냅샷 |
| `A/ThornBombs/ThornBombExplosion.cs` | 폭발 1회 정산과 기존 피해/이동 경계 호출 |
| `A/ThornBombs/ThornBombAttackApplication.cs` | 공격 중심 후보 캡처·부착 능력 조회·연쇄 후 재부착, 공용 실행기에서 특성 규칙 분리 |
| `V/Combat/Playback/Handlers/CombatThornBombEventHandler.cs` | 폭탄 이벤트 소비. 공용 사실 handler에 폭탄 규칙을 추가하지 않음 |
| `V/Combat/Display/Unit/ThornBombAttachmentPresenter.cs` | 대상 ID별 부착 표시·해제·최종 Sync |

신규 파일마다 `.meta`를 함께 생성한다. 표현 데이터는 기존 TraitVisual/CombatPresentation 테이블을 먼저 사용하고 기능별 DB 서비스·Bootstrap을 새로 만들지 않는다. 확정 리소스가 없으면 임의 GUID를 만들거나 장판 리소스를 폭탄으로 조용히 대용하지 않는다.

## 2. Phase 0 — 실행 전 계약·소유권 확인

### Task 0.1: 설계와 현재 기준 고정

**Files:** 설계 문서, 이 계획, `AGENTS.md`, `Assets/AGENTS.md`.

- [x] 사용자 승인 범위를 확인했다: 현재 지시는 이 계획의 Phase 0 조사·보고까지다. R01~R14의 명시적 합의와 P01~P11의 제안 구분을 유지하며, 이번 단계에서 미확정 제안을 구현하거나 전체 설계의 개별 합의로 바꿔 쓰지 않았다.
- [x] 한 폭발의 전체 피해 적용 후 승패를 판정한다는 사용자 답변을 설계 R14·§6.2, Task 2.1, V09에 반영했다. 이 항목은 문서 결정 반영이며 구현 완료가 아니다.
- [x] `git status --short`, `git branch --show-current`, `git rev-parse HEAD`로 기준과 기존 변경을 기록했다. HEAD `1abe42230997f6db0fb9076d7c6a5732b49caf34`, 브랜치 `feature/Cheon/Core/NewCombatSytem`. FMOD Catalog 및 기존 문서 변경을 보존했다.
- [x] Core/View 실행 주체와 Phase 권한을 [Phase 0 기록 §1](2026-11-17-cactus-cannon-thorn-bomb-phase0-audit.md#1-승인-범위와-작업-상태)에 정리했다. Core 전달 요청은 [공동 경계 인계 §75](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#75-선인장-캐논-가시-폭탄기존-가시-장판-퇴역-요청-2026-11-17)를 따른다. 후속 Phase는 미착수다.
- [x] `TraitKey`의 540 미사용과 첨부 지침을 확인했다. 기존 `ThornField = 40` 재사용은 금지한다. 후속 구현 직전 충돌 여부를 다시 확인한다.

### Task 0.2: 삭제와 기존 시험 변경의 구체 목록 확보

**Files:** §6의 가시 장판 삭제 지도 및 그 검색 결과에 있는 현재 소스/에셋/시험.

- [x] 지정한 `rg` 검색으로 191파일(제품 소스 71, SO 4, 시험/fixture 116)을 분류하고 간접 View 소비자 2개를 추가 확인했다. 자동 삭제는 하지 않았다.
- [x] 전용 소스/SO 21개와 역참조로 확인한 전용 프리팹 1개의 `.meta`/GUID 사용처를 기록했다. 발리스타와 공유하는 가시 아이콘은 보존 대상으로 확정했다.
- [x] 전용/혼합 시험과 관련 행의 git 작성자를 조사했다. `ThornFieldDomainTests`는 공통 이동 검증이므로 보존한다. [Phase 0 기록 §4](2026-11-17-cactus-cannon-thorn-bomb-phase0-audit.md#4-기존-시험--무엇을-보존하고-무엇을-먼저-승인받을지)에 구체 변경 검토 목록을 제시했으며 시험 수정·삭제 승인은 아직 받지 않았다.
- [x] 시험을 행동/계약/fixture 검토 59파일, API 보정 후보 56파일, 공통 검증 보존 1파일로 분리했다. 기대값을 바꾸는 작업을 컴파일 보정으로 분류하지 않았다.

**완료/보고:** 승인된 규칙·Phase 담당·삭제 대상·시험 변경 허용 범위. 승인 범위 밖 항목을 실행하지 않고 Phase 종료에서 멈춘다.

**2026-11-17 결과:** Phase 0 조사·기록 완료. 제품 파일·시험·에셋은 수정하지 않았으며 Phase 1로 넘어가지 않는다. `ContractShapeGuardTests`는 Phase 1에서 `ThornBombs` 공개 속성을 추가하는 시점부터 기대 목록 변경이 필요하므로, 시험 관련 승인을 Phase 3까지 미뤄서는 안 된다. 이는 후속 실행 전 확인 항목이며 현재 조사 단계의 미실행을 뜻하지 않는다.

## 3. Phase 1 — 신규 특성 계약과 상태 보존

### Task 1.1: Data/Spec/등록 계약 추가

**Create:** §1의 authoring/data/spec/definition/runtime-state/status/rules/factories/effect-rule 파일.

**Modify:** `C/Contracts/Common/TraitKey.cs`, `K/TraitSpecAdapter.cs`, `A/Traits/TraitRegistry.cs`, `A/Traits/TraitEffectProvider.cs`, `A/CombatCommandEffects.cs`, `C/Domain/Traits/Effects/TraitEffectStateCodec.cs`, `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`, `K/MainSpecializationDefinitionSnapshot.cs`, `A/DeferredCombatFeatures.cs`.

**Interfaces:**

- `ThornBombTraitSpec(int damagePercent)` / `ThornBombTraitData(int damagePercent)` → `DamagePercent` get-only. 음수 거부, 설정값 50. SO `damagePercent = 50`.
- `ThornBombRules.CalculateBaseDamage(int capturedAttackPower, int damagePercent) : int` → checked wide intermediate, 정수 절삭.
- `ThornBombRules.CalculateDueSequence(int attachedControllerTurnSequence) : int` → checked +2.
- `IEffectThornBombRule : ICombatEffectRule`의 `GetDamagePercent(in EffectRuleInput input) : int`를 `ThornBombEffectRule`이 구현한다. 기존 `StatelessEffectStateSnapshot`을 사용하고 `TraitRegistration.EffectRuleFactory`에 연결한다. 범용 engine에 새 전역 특성 분기를 넣지 않는다.
- 기존 TraitRegistration 한 행으로 생성/등록한다. 부착 상태는 runtime-state에 저장하지 않는다.

- [x] 순수 Data/Spec/Rule과 입력 검증을 만든다. 반복된 50/1/+2 수치를 여러 계산기에 넣지 않는다.
- [x] 기존 Data→Spec, 특성 상태/설정 비교 경계에 신규 타입을 한 번씩 연결한다. adapter의 타입 변환 외에는 새 TraitKey 분기를 퍼뜨리지 않는다.
- [x] 기존 SO는 아직 바꾸지 않고 신규 특성을 준비한다. 미구현 상태에서 운영 사용 가능으로 공개하지 않는다. Phase 2 완료 시 활성화한다.
- [x] V01·V08의 수치/시계 예제를 코드 읽기로 대조한다. 런타임 검증 완료로 보고하지 않는다.

### Task 1.2: 명령 상태·전이·Buffer 보존

**Phase 0에서 확인한 선행 조건:** `Assets/Tests/EditMode/ContractShapeGuardTests.cs:19`의 `MovementUndoContractsRequireExplicitComparisonAndCopyDecisions`는 정확한 공개 속성 목록을 검사한다. `ThornBombs` 추가에 따른 기대 목록 갱신은 단순 컴파일 보정이 아니다. 실제 With/복사/비교/게시 연결을 확인한 뒤 해당 시험 수정에 대한 사용자 승인을 받아 반영하며, 일반 Phase 진행 지시를 시험 제한의 예외로 해석하지 않는다. 시험을 비활성화하거나 미확인 통과로 보고하지 않는다.

**Create:** `C/Contracts/Combat/ThornBombSnapshot.cs`, `ThornBombStateSet.cs`, `ThornBombEvents.cs`, `ThornBombExplosionPreview.cs`.

**Modify:** `C/Contracts/Combat/CombatCommandState.cs`, `BattlePreparation.cs`, `CombatChangeEvidenceReader.cs`, `CombatEvent.cs`(필요한 kind 등록만), `A/CommandAttackProjection.cs`, `A/MovementProjection.cs`, `S/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs` 및 명령 상태 생성·복사 호출부(`A/CombatTurnCalculator.cs` 포함).

**Interfaces:**

- `ThornBombSnapshot(long bombId, EntityId sourceId, Owner sourceOwner, EntityId targetId, CellCoord attachmentLocalCell, int capturedAttackPower, int damagePercent, int dueControllerTurnSequence)` → immutable.
- `ThornBombStateSet(long nextBombId, IReadOnlyList<ThornBombSnapshot> bombs)`, `Empty`, `SameValues(ThornBombStateSet other)` → 대상/BombId 중복 거부, 방어 복사, next ID 검증.
- `CombatCommandState.ThornBombs : ThornBombStateSet` 및 `With(..., ThornBombStateSet thornBombs = null)` → null은 기존 상태 유지, 새 전투는 Empty.
- `CombatThornBombAttachedEvent`는 새 snapshot, `CombatThornBombDetonatedEvent`는 소모 snapshot·중심·기폭 이유·AttackGroup을 가진다. 기존 health/movement 이벤트를 대체하지 않는다.

- [x] 순수 계약과 생성자 검증을 만든다. 출처 사망은 유효하며 존재하지 않는 대상·잘못된 로컬 칸은 실제 준비/게시 경계에서 검증한다.
- [x] 상태 복사/비교/준비/현재 입력 투영/Buffer 설치·초기화에 동일 facts를 연결한다. 상태를 새로 만드는 모든 호출 경로에서 폭탄 유실을 검사한다.
- [x] 이벤트 증거 검증에 생성/소모·최종 상태의 일치를 넣는다. 계산 중 임시 queue를 Buffer에 저장하지 않는다.
- [x] V10·V16 상태 보존/취소 시나리오와 직렬화 번호 중복을 정적 대조한다.

**2026-11-17 실행 기록:** 사용자 지시로 Phase 1 제품 코드를 반영했다. `ThornBomb = 540`은 구현 대기 상태이며 기존 `ThornField = 40`과 운영 SO는 유지했다. 부착·기폭 사건 번호는 63·64다. 피해 계산은 공격력 15와 백분율 50에서 7, 만료 순번 계산은 1에서 3임을 코드로 대조했다.

- `CombatCommandState.ThornBombs` 한 곳에 활성 목록과 다음 ID를 보관한다. 새 전투는 빈 값이고, 기존 공격→도착 투영은 이미 원본 상태를 전달한다. 이동·턴 경계는 `With`를 재사용해 폭탄 등 바꾸지 않는 값이 누락되지 않게 했고, 턴 경계의 기존 이동 취소 초기화는 유지했다. 부착·소모 사건과 최종 상태를 대조하고, 게시 시 출처·대상·부착 칸·만료 순번도 확인한다. 미리보기 취소 경로는 원본을 설치하지 않는다.
- SSOT/SOLID/FW: 산식과 설정은 전용 특성 파일, 조립은 기존 TraitRegistry, 공개 수명은 기존 Buffer의 준비/설치 경계를 사용한다. 공통 공격·이동·턴이 폭탄 규칙을 호출하는 작업은 Phase 2다. 설정 검증 변경은 전용 타입과 어댑터를, 공개 상태 변경은 기존 상태/이동 취소/게시 검증을 함께 확인해야 한다. 새 Framework 서비스와 View 연결은 없다.
- Phase 2 연결 시 확인: 계획의 폭탄 만료 순번은 `int`이고, 기존 `CombatTurnFacts.ControllerTurnSequence`는 `long`이다. 이번 Phase는 계획 서명을 유지했다. 실제 턴에 연결하기 전에 계획의 타입을 맞출지 결정한다.
- 기존 시험 변경 승인 대기: `ContractShapeGuardTests.MovementUndoContractsRequireExplicitComparisonAndCopyDecisions`에 `ThornBombs` 공개 속성 추가, `CombatChangeEvidenceCoverageTests.EveryProductionEventTypeHasEvidenceCoverage`에 부착·기폭 사례 두 개 추가, `LegacyEnhancementRetirementTests.SurvivingRegistry_PreservesAllNewAndDeferredIdentities`에 구현 대기 `ThornBomb`과 등록 수 32→33 반영이 필요하다. 세 시험 모두 GwonCheonmyeong 작성이며 승인 전 수정하지 않는다.
- 30개 변경·신규 스크립트의 C# 구문, 신규 .cs/.meta 13쌍, GUID/enum 번호와 diff 공백을 정적으로 확인했다. **컴파일 미확인**. 시험 변경·실행과 Unity 확인 없이 여기서 멈춘다.

## 4. Phase 2 — 공통 실행 재사용과 폭탄 기능

### Task 2.1: 공통 넉백 경계와 폭발 해석기

**Create:** `C/Domain/Movement/ImpactCenterGeometry.cs`, `A/ForcedMovementExecution.cs`, `ForcedMovementRequest.cs`, `A/ThornBombs/ThornBombResolution.cs`, `ThornBombExplosion.cs`.

**Modify:** `C/Domain/Traits/Rules/ImpactBoltTraitRule.cs`, `A/AttackForcedMovement.cs`, `AttackKnockbackCalculator.cs`, `MovementProjection.cs`, `CommandAttackProjection.cs`, `C/Contracts/Combat/CombatDamageKind.cs` 및 그 실제 분류/검증 소비자.

**Interfaces:**

- `ImpactCenterGeometry.TryResolveStep(CharacterCombatFacts target, CellCoord center, out CellCoord step) : bool` → 기존 발리스타의 생존/필드/중심 점유 제외와 방향을 그대로 보존.
- `ForcedMovementRequest(EntityId targetId, CellCoord step, int distance, CombatMovementCause cause, bool deferLastObjects, bool publishMovedEvents)`.
- `ForcedMovementExecution.ExecuteSteps(MovementProjection projection, ForcedMovementRequest request, IReadOnlyList<IMovementTileRule> rules, Action<ForcedMovementResult> completed) : IEnumerable<string>`.
- `ForcedMovementResult`는 `ForcedDisplacementPath Path`, `int NormalizationHealthLoss`, `int TerrainDamage`를 돌려준다. 공격 caller가 기존 ledger에 반영하며 공통 실행이 공격 ledger를 알지 않는다. 실제 경로/HP/도착은 기존 projection에 반영한다.
- `ThornBombResolution(ThornBombStateSet initial)`; `TrySchedule(EntityId targetId, ThornBombTrigger reason) : bool`; `ScheduleDue(Owner owner, long controllerTurnSequence) : void`; `Attach(ThornBombSnapshot bomb) : void`; `Snapshot() : ThornBombStateSet`.
- `ThornBombResolution.ResolveSteps(MovementProjection projection, IReadOnlyList<IMovementTileRule> rules) : IEnumerable<string>` → 동일 projection의 Damage/Characters/Events/환경/사망 칸을 사용하고 완료 시 queue가 비거나 terminal이다. ID 발급은 resolution 내부 `NextBombId()`에서만 한다.
- `ThornBombExplosion.ExecuteSteps(MovementProjection projection, ThornBombSnapshot bomb, ThornBombTrigger trigger, ThornBombResolution bombs, IReadOnlyList<IMovementTileRule> rules) : IEnumerable<string>` → 한 폭발만 실행하고 재귀 drain하지 않는다.

- [x] 발리스타 순수 방향 계산을 추출하고 기존 caller를 위임시킨다. 이동 중 지형/도착 및 attack-only ledger 정산을 분리하되 동작을 바꾸지 않는다.
- [x] 폭탄 사전/중복 예약 집합/FIFO를 구현한다. 예약은 활성 폭탄의 BombId를 붙잡고, 폭발 시작에 상태에서 제거하며, 새 폭탄은 오래된 예약에 걸리지 않게 한다.
- [x] `TraitExplosion = 8`의 원인 분류를 추가하고 IsAttack=false, 공격 횟수/추가 명중 특성 비발동을 유지한다. 순수 기준 피해→보유 방어→기존 incoming/items/HP 경계를 한 번만 적용한다.
- [x] 폭발마다 고유 AttackGroup, 피아 동일 범위, EntityId 중복 제거, 중심/사망자 넉백 제외를 구현한다. 큐에서 꺼낼 때 생존자의 최신 위치를 얻는다. 사망자의 실제 부착 칸은 사망 증거에서 캡처하고 `DisplaceCorpse`가 바꾼 가상 `DeathAnchors`로 대체하지 않는다.
- [x] 폭발 1회의 전체 대상 피해를 처리하는 범위를 명시하고, 이 범위 안에서는 Terminal에 따른 나머지 피해 생략을 하지 않는다. HP·피격 경감·증거 기록은 기존 경계로 적용한다. 전체 피해 뒤 승패를 판정해 종료면 넉백·잔여 연쇄·재부착을 중단한다. 전투가 계속될 때만 넉백을 실행하고 그 이동 중 종료는 기존 경계에서 검사한다. 이 중단 지연을 전역 Terminal 변경으로 구현하지 않는다.
- [x] 한 폭발 내부 대상 순서는 EntityId 오름차순 제안을 사용한다. 메인과 목표 보스가 함께 치명 피해를 받아도 둘의 HP를 모두 적용한 뒤 기존 패배 우선 규칙으로 판정한다. 중단한 예약은 전투 종료 상태에서 부착 표시로 다시 복원하지 않으며 종료 초기화에서 정리한다.
- [x] V04~V07·V09·V12·V15를 순서도/결과 예와 대조한다. 필수 이동 실행 인자가 실제 projection에 없으면 unrelated subsystem을 넣지 말고 필요한 좁은 계약 변경을 먼저 보고한다.

### Task 2.2: 실제 공격 명중과 중심 부착 연결

**Create:** `A/ThornBombs/ThornBombAttackApplication.cs`.

**Modify:** `A/AttackDeliveryExecutor.cs`, `AttackDirectDamageCalculator.cs`, `CommandAttackPlan.cs`, `CommandAttackProjection.cs`, `CommandAttackLedger.cs`(필요한 명중 사실만), `CombatAttackDelivery.cs`/`AttackSelectionEvaluation.cs`(실제 중심 조회 계약이 필요한 부분만).

**Consumes:** Task 2.1의 resolution 및 projection, 실제 `DeliveredAttackHit.Landed`/수신자와 전달 성공 판정.

**Produces:** 기존 공격 1회의 직접 피해·넉백·도착 → 폭탄 drain → 중심 대상 재부착 경계. 별도 command 없음.

**Feature-local interfaces:** `ThornBombAttackApplication.CaptureAttachment(CommandAttackPlan plan, CommandAttackProjection world) : ThornBombAttachmentCandidate?` 및 `CompleteAttachmentSteps(CommandAttackPlan plan, MovementProjection projection, ThornBombAttachmentCandidate? candidate, ThornBombResolution bombs, IReadOnlyList<IMovementTileRule> rules) : IEnumerable<string>`. 같은 파일의 내부 불변 candidate는 SourceId/SourceOwner/TargetId/AttachmentLocalCell/DamagePercent만 보존하고 공격력은 실제 부착 시 읽는다. 공용 실행기는 후보의 내부 필드를 해석하지 않는다.

- [x] `CaptureAttachment`가 부착 능력을 조회하고 실제 도달 중심의 캐릭터 ID/로컬 부착 칸을 한 번 고정하게 한다. 빈 칸/구조물/실제 전달 실패는 후보를 만들지 않는다. 선행 후보를 만든 경우 전달 성공 확인 전 부착할 수 없도록 한다. 모든 원래 타격의 명중 자격을 보존한다.
- [x] 전달 성공한 실제 유닛 접촉에서만 `TrySchedule`을 호출한다. `HealthLoss > 0`, `ledger.HitIds` 존재, 단순 범위 포함을 명중의 대용으로 쓰지 않는다.
- [x] 기본/추가/반응/스킬 공격의 실제 명중 경계를 공유하되 폭발은 그 공격 경계를 호출하지 않게 한다. 희생 실제 수신자와 중심 대상 ID를 별도로 유지한다.
- [x] 기존 원래 공격의 강제 이동·도착 처리가 끝난 뒤 drain한다. 기존 `CaptureHitHealth`, 공격 그룹, 사망/영혼, 추가 공격·행동 소비의 의미를 보존하도록 직접 피해와 폭탄 결과를 분리한다. 실행 순서 변경으로 다른 기능 의미가 달라져야 한다면 구현하지 말고 계획 변경을 요청한다.
- [x] drain 후 terminal이 아니며 중심 대상이 생존하면 재부착을 준비한다. 희생 대행으로 그 대상의 구폭탄이 남아 있으면 교체 사유로 예약→drain→종료/생존 재검사한 뒤 부착 시점 공격력을 읽고 새 ID/만료값을 기록한다. 현재 중심 칸을 재검색해 다른 유닛에 붙이지 않는다.
- [x] V01~V07·V13을 대조한다. 특히 기본 스플래시 70% 유지, 아군 직접 피해, 스플래시 기폭 가능/부착 불가, 공격 도달 실패 시 부착 불가를 확인한다.

### Task 2.3: 턴·사망·미리보기·결과 게시

**Modify:** `A/CombatTurnCalculator.cs`, `MovementProjection.cs`, `CombatAttackCalculation.cs`, `CombatAttackCalculator.cs`, `AttackResultFactory.cs`, `C/Contracts/Combat/CombatChangeEvidenceReader.cs`, 기존 AttackPreview 계약/매핑, `S/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs`, 필요한 AI 평가 입력 소비자.

**Consumes:** 같은 ThornBombResolution/Explosion, `ControllerTurnSequence`, `SourceOwner`, 기존 사망 칸/health 증거 및 취소 수명.

- [x] 턴 시작의 재구성/주기 상태 뒤 `ScheduleDue`→drain을 연결한다. 턴 시작 전이가 끝나기 전에 완료하고, 중간 결과를 PlayerInput으로 먼저 게시하지 않는다.
- [x] 사망 정산의 좁은 경계에서 부착 대상의 사망을 예약한다. 일반 환경 피해/낙사 생존 판정과 무관한 전용 분기를 각 기믹에 추가하지 않는다. 명중으로 이미 예약된 폭탄은 중복 처리하지 않는다.
- [x] 공격·이동·자동 턴 투영이 같은 명령 내에서 하나의 resolution을 공유하고 다른 명령/미리보기와는 격리되도록 연결한다. 폭탄 사망 연쇄 중 사망 정산을 재귀 진입시키지 않는다.
- [x] 미리보기와 실행이 같은 drain과 상태 복사를 사용하도록 연결하고 연쇄별 결과/실제 넉백 경로를 기존 preview에 전달한다. AI가 동일 결과를 이용하게 한다.
- [x] 비가역 이동 중 폭발의 health/effect 증거를 undo 차단에 연결한다. 가역 이동의 부착 위치는 대상 위치에서 파생한다.
- [x] 가용성을 활성화하고 실제 상태/이벤트 일치 검증을 확인한다. 운영 SO는 Phase 3까지 바꾸지 않는다.
- [x] V08~V11·V13·V16 결과를 사용자/Core 담당 검증 항목으로 넘긴다.

**완료/보고:** 신규 기능의 Core 계약·실행·공개 경계가 준비된 상태. 실행 검증과 정적 확인을 구분하고 Phase 3 전 멈춘다.

### 2026-11-17 Phase 2 실행 기록 — 코드 완료·Unity 확인 대기

- **사용자 승인 보완:** 만료 순번·계산·ScheduleDue를 기존 턴과 같은 `long`으로 통일했다. 공용 강제 이동에 `rules`를 명시적으로 전달하며, 공통 사망 정산을 위해 MovementProjection도 생성 때 받은 규칙 목록을 명령 수명 동안 보관한다. 공격·이동·배치·턴·장애물·플랫폼·기믹의 변경은 이 의존성 연결이며 기믹별 폭탄 규칙은 넣지 않았다.
- **2.1:** 발리스타의 방향 계산은 `ImpactCenterGeometry`, 칸별 강제 이동은 `ForcedMovementExecution`으로 공유한다. 기존 공격 caller가 반환된 정규화 손실/지형 피해를 ledger에 반영한다. 경로→넉백 공개 결과 변환도 `ForcedDisplacementPath.ToKnockback` 한 곳이다. 폭탄은 `ThornBombs/`의 예약·단일 폭발·공격 부착 3개 책임으로 구성했다.
- **2.2:** 실제 `Landed`와 희생 수신자로 기폭을 예약하고 원래 중심 ID는 별도 부착 후보로 유지한다. 직접 피해/넉백/도착·HitHealth 고정 뒤 연쇄→교체→새 부착을 처리한다. 추가 공격도 같은 명중/부착 기능을 호출한다. 폭탄으로만 생긴 사망은 원래 공격의 처치 연쇄 횟수에 더하지 않는다. 빈 칸/아군 정책은 기존 효과 능력 계약으로 제공한다.
- **2.3:** 턴 시작 재구성·주기 효과 뒤 만료 큐를 비우고, 명령 완료에서 폭탄/기존 사망 효과의 새 사망을 재귀 없이 소진한다. 사망 칸은 실제 체력 사건의 직전 배치에서 읽어 가상 시체 넉백 좌표와 분리했다. 전투 종료 시 남은 예약과 활성 폭탄을 정리한다. 한 폭발 전체 피해 뒤 승패 확인을 두며 HP 적용 경계를 복제하지 않았다.
- **공개·미리보기:** `AttackPreview.ThornBombExplosions`에 폭발별 중심·명중·실제 경로를 순서대로 보관하고 `ProjectedThornBombs`에 완료된 예상 부착 상태를 공개한다. 완료 정산에서 생긴 폭발도 최종 preview에 포함한다. AI의 정확 평가에는 동일 전이의 실제 HP 변경이 들어가며, 기존 preview 점수의 0피해 대상 수집에도 폭발 명중을 연결했다. 부착/기폭은 Effect 증거라 이동 취소를 막고, 단순 이동에서는 동일 폭탄 상태를 유지한다.
- **그룹 수명:** AttackGroup은 기존 `CommandAttackGroups`의 투영별 수명을 유지하며 한 연쇄의 폭발마다 새 번호를 받는다. 합성 전이는 독립 계산의 사건을 합치므로 전역 그룹 번호 중복 금지를 제거하고 BombId/활성 상태·사건 대조를 유지했다. 폭탄 식별자를 새 그룹 전역 상태로 복제하지 않는다.
- **SO 수치:** 기존 UnitDataSO의 공격력과 `ThornBombTraitAuthoring.damagePercent`를 사용하고 두 값을 부착 시 저장한다. 범위/거리/시점은 현재 합의한 고정 규칙이다. 운영 선인장 SO 연결과 기존 장판 제거·View 소비는 Phase 3이며 이번에는 실행하지 않는다.
- **기존 시험:** 시작 후 확인한 작업 트리에는 Phase 1 공개 속성·사건 사례 갱신이 이미 있었다. 사용자 추가 승인으로 `LegacyEnhancementRetirementTests.SurvivingRegistry_PreservesAllNewAndDeferredIdentities`의 가용 수 42→43, 구현 대기 목록→빈 값 두 기대값만 맞췄다. 등록 수 33 등 나머지 검증은 유지했다. 새 시험 없음, 시험 실행 없음.
- **구조 점검:** 상태 원본은 CombatCommandState, 명령 계산은 투영별 resolver, 게시 수명은 기존 Buffer다(SSOT). 폭탄 규칙/명중 연결/공통 이동/조립을 분리했다(SRP/OCP). 기존 경로·피해 계약을 유지하고 필요한 능력과 인자만 사용한다(LSP/ISP/DIP). Framework 서비스·Builder·전역 이벤트 구독은 추가하지 않아 FW-001/002/003 등록 수명과 Core/View 경계를 유지한다. 범위·피해 규칙 변경은 전용 폭탄 파일을, 강제 이동 변경은 발리스타/지형/도착 공유 경계를 검증한다. 이 공유 회귀는 필요한 연결이다.
- **확인 수준:** 변경 제품 C# 37개와 승인된 기존 시험 1개(합계 38개) Roslyn 구문 파싱 오류 0, `git diff --check` 통과. 구문 파싱은 타입 검사·컴파일이 아니다. **컴파일 미확인**, Unity·MSBuild·EditMode 테스트·재생 미실행. V01~V10·V12~V16은 코드 경로 대조이며 실제 동작 확인은 사용자 Unity 인수 항목이다. V11·V14의 표시 수명과 V17~V18은 Phase 3 이후 확인한다.
- **검토 반영:** 별도 검토에서 Important 1건(새 부착 결과의 공개 preview 누락)을 확인했다. 최종 전이의 불변 ThornBombs를 `ProjectedThornBombs`로 제공하고 공개 게시까지 추적해 보완했다. 수정 후 구문 파싱 38개/오류 0, diff 공백 검사 통과. 실행 증거는 아니다. Minor 1건(공용 이동의 진입 검사가 AttackForcedMovement에 남은 의존)은 동작을 바꾸지 않는 후속 정리로 보류했다.
- **검토 범위 판정:** Phase 3 표시/에셋/퇴역은 승인 범위 밖이다. 반응+추가 특성 조합의 다른 진영 턴 신규 부착은 현재 재현 경로가 확인되지 않아 기존 +2 설계를 유지한다(확장 시 다음 자기 턴 산식 재검토). 기존 공개 preview의 사망 특성 생략 정책은 유지하며 런타임 성능·Unity 재생은 미확인이다.
- **중단 지점:** Phase 2 코드 완료. Phase 3 미착수. 커밋·push 없음.
## 5. Phase 3 — View·DB 전환과 기존 장판 완전 제거

Core 계약을 먼저 지워 View가 참조하는 중간 상태를 완료로 보고하지 않는다. 아래 Task는 한 통합 Phase이며, Core/View 소유자와 동시 편집을 조율한다. 시험 기대값 변경 승인이 필요한 항목은 Phase 0 결과를 따른다.

### Task 3.1: 기존 공격의 부착/폭발 표시 연결

**Create:** `V/Combat/Playback/Handlers/CombatThornBombEventHandler.cs`, `V/Combat/Display/Unit/ThornBombAttachmentPresenter.cs`와 새 TraitVisual 에셋/메타.

**Modify:** `F/CombatSceneDisplayFactory.cs`, `F/CombatEventPlaybackFactory.cs`, `V/Combat/Playback/Ordering/CombatEventTypeTable.cs`, `V/Combat/CombatDisplayTables.cs`, `V/Descriptions/Traits/TraitDescriptionBindings.cs`, 기존 유닛 표시/공격 preview 소비자의 연결부.

**Interfaces:**

- presenter: `Sync(ThornBombStateSet state)`, `Attach(ThornBombSnapshot bomb)`, `Remove(long bombId)`, `Dispose()`; 대상 visual 조회/표현 설정/렌더 scope는 Factory에서 주입한다.
- event handler: 기존 handler 프로토콜로 새 typed 이벤트를 받는다. 부착/폭발 순서는 Core 결과를 따른다. 단독 Update 타이머와 판정 로직 없음.

- [x] 기존 상태 표시와 렌더 scope에 붙이는 기능을 조립한다. 추가 클릭/명령/독립 화면을 만들지 않는다.
- [x] 폭발 중심과 공격 그룹으로 재생을 묶어 A 피해·이동 완료 뒤 B 폭발을 시작한다. 사망 오브젝트가 없어도 event 위치로 재생한다.
- [x] 표시 부착/회수는 사건 순서로, 최종 Sync는 완료 경계로 연결한다. 재부착 폭탄을 이전 폭발의 Remove가 없애지 않도록 BombId를 쓴다.
- [x] Cancel/StopInput/Detach/씬 전환에서 scope를 정리하고 V10·V11·V14를 사용자 Unity 확인으로 인계한다. View Test 코드는 작성하지 않는다.

### Task 3.2: 에셋 연결 이전과 전용 자산 정리

**Modify:** `DB/InGameGroup/Unit/Item/Playerable/Unit_CactusCannon.asset`, `DB/ViewGroup/TraitVisual`의 등록 테이블, ViewGroup/Catalog의 실제 GUID 참조, `S/DataBaseSystem/Queries/ViewDataBase/ViewDataBase.cs`, `S/Factory/DBFactory/Group/ViewDataBaseFactory.cs`, `S/ServiceKeys/DBFactory/Group/ViewTableKey.cs`.

- [x] 선인장 managed reference를 `ThornBombTraitAuthoring`/50으로 바꾼다. 공격력 15·사거리 3·targetingMethod·병과·wall 정책은 유지한다.
- [x] 새 TraitVisual 설명에 자동 부착·피아 피해·50%·방어/상성 무시·다음 자기 턴 시작·주변만 넉백을 반영한다. 표시 수치는 공개 Spec/Buffer에서 읽는다.
- [x] 옛 TraitVisual/ThornFieldVisual의 등록/참조를 제거하고 전용 에셋과 `.meta`를 삭제한다. 공용 아이콘/프리팹까지 연쇄 삭제하지 않는다.
- [x] Catalog의 전용 GUID 참조를 확인했다. 사용자 승인 범위의 `DataBaseAssetTests` 26건이 통과했다.

### Task 3.3: 가시 실행·상태·표시·API 제거

**Files:** §6 삭제 지도. 전용 파일은 `.meta` 포함, 공용 파일은 해당 멤버/호출/등록만 제거.

- [x] 장판 생성/접촉 피해/수명 감소/방어 감소 부여/소모 이벤트·미리보기·AI 장판 평가를 제거한다. 공유 debuff/ThornHeart 기능은 유지한다.
- [x] 모든 ThornFields/Thorns(장판 전용) 상태·생성자·With/비교·현재 입력·이동 context/undo 복사를 제거하고 호출부를 함께 맞춘다. 불필요한 빈 호환 인자를 남기지 않는다.
- [x] 전용 Rule/Factory/Effect/Authoring/Data/표시 설명/DB Query와 FieldThornPresenter 연결을 제거한다. `UnitTraitAuthoring.cs`, `UnitTraitData.cs`, `TraitFactories.cs` 같은 혼합 파일 전체를 삭제하지 않는다.
- [x] `ThornField = 40`은 퇴역 번호만 유지하고 `DeferredCombatFeatures.IsTraitRetired`에 연결한다. 기존 가시 보류 문구는 제거한다. 옛 직렬화 데이터는 명시적으로 거부하고 새 폭탄으로 자동 해석하지 않는다.
- [x] 시험 변경은 Phase 0의 허용 목록만 적용한다. 단순 호출 보정과 장판 전용 검증 폐기를 각각 보고한다. 권한 없는 기대값 삭제로 초록 결과를 만들지 않는다.
- [x] `rg` 잔여 결과를 퇴역 번호/퇴역 검사/이력 문서/보존 에셋과 실제 실행 참조로 구분한다. 실제 실행·저작·표시 참조는 0이어야 한다.

### 2026-11-17 Phase 3 실행 기록

- `CombatStateBuffer`의 폭탄 게시 검증은 이전 세션이 옮긴 `ThornBombPublicationPolicy`를 통해 실행한다. Systems에서 Core 내부 타입을 직접 참조했던 Phase 1의 컴파일 오류를 해결한 경계를 이어받았다.
- 선인장 캐논의 기존 투사체에 쓰는 `CactusBomb.prefab`과 `Boom` 효과를 부착·기폭 표시에 사용한다. 부착물의 대여와 반환은 BombId 기준이며, 표시가 연결된 동안에는 입력이 재생 사이에 멈춰도 회수한다. 기존 재생 순서가 앞 폭발의 피해와 넉백 이후에 다음 폭발을 시작한다. 조준의 폭심지·넉백·최종 체력은 Core 미리보기에서 읽고 재구성 표시도 기존 방식으로 처리한다.
- 운영 SO를 가시 폭탄 피해율 50%로 이전했다. 장판 표시용 SO와 `ThornCactus.prefab` 및 메타를 정리했다. 프리팹이 참조하던 `서브_프랍_꼬마선인장.fbx`, 가시 폭탄 모델과 텍스처, 발리스타 공유 아이콘은 보존했다.
- 기존 실행·상태·이동 접촉·표시·DB 등록에서 장판을 제거했다. TraitKey 40과 기존 소모 사건 번호 20은 퇴역 예약 번호다. 폭탄의 수치와 상태는 Core에 있고 View는 결과만 읽는다. 기존 TraitVisual, 전투 표시 Factory, 재생과 Effect scope를 사용한다. 새 서비스 등록이 없어 FW 등록·수명 경계는 유지된다. 일반 이동·강제 이동과 DB 공개 계약에 걸치는 회귀는 Unity EditMode 사용자 승인 범위로 확인한다.
- 시험 이전은 승인된 범위에서 수행했다. 장판 API를 사용하는 생성자·fixture를 맞췄으며, 장판 자체의 사례는 제거했다. 혼합 검증은 환경 피해·지뢰를 사용해 이동·넉백·피해 순서·반사를 확인한다. 기존 선인장 테스트는 벽에 막힌 경우와 중심 부착에 성공한 경우의 실행·미리보기를 비교한다.
- 기존 캡처 경로로 참조 다섯 파일을 생성했다. 시나리오 ID와 순서를 유지했다. 기존 자료와 비교해 폭탄·퇴역 상태 필드, 기존 병행 작업의 기믹 직렬화 필드와 상태 해시를 제외한 내용이 일치했다.
- Unity 6000.0.73f1 컴파일 성공. Core 6,116 통과·11 건너뜀, CoreIntegration 1,184 통과·8 건너뜀으로 **7,300 통과, 0 실패, 19 건너뜀**이다. `DataBaseAssetTests` 26건과 기존 선인장 벽·부착 테스트 4건을 포함한다.
- 구조 검토에서 계산·등록·표시의 책임을 분리하고(SRP), 기존 등록 행과 재생 처리기 규약을 사용했다(OCP). 기존 이동/피해 소비 규약은 관련 회귀로 확인했다(LSP). View는 필요한 폭탄 스냅샷과 사건만 받고(ISP), 기존 Factory가 표시와 효과 의존성을 주입한다(DIP). 폭탄의 상태 원본은 Core 명령 상태 한 곳이다.
- 장면에서의 폭발 순서와 부착물 위치, 취소·종료·재진입 때의 회수는 V10·V11·V14로 확인한다. Phase 3에서 멈춘다.

## 6. 가시 장판 삭제 지도

2026-11-17 검색에서 확인한 주요 소유 경계다. 실행 직전 검색을 다시 수행하며 GUID 참조를 포함한다.

| 구역 | 제거/변경 파일 |
|---|---|
| 전용 Core 타입 | `C/Contracts/Commands/Traits/ThornFieldTraitSpec.cs`, `C/Contracts/Snapshots/ThornFieldCellSnapshot.cs`, `C/Contracts/Snapshots/Traits/ThornFieldTraitStatusSnapshot.cs`, `C/Domain/Field/ThornFieldCell.cs`, `C/Domain/Traits/Definitions/ThornFieldTraitDefinition.cs`, `States/ThornFieldRuntimeState.cs`, `Rules/ThornFieldTraitRule.cs`, `Effects/ThornFieldEffectRule.cs`, `Effects/ThornFieldEffectStateSnapshot.cs` |
| 전용 효과/실행 | `C/Domain/CombatEffects/Contexts/ThornFieldApplicationEffectContext.cs`, `Evaluations/EffectThornFieldContribution.cs`, `Evaluations/EffectThornFieldEvaluation.cs`, `A/ThornFieldApplicationCalculator.cs`, `A/ThornMovementTileRule.cs` |
| 공용 registry/adapter | `TraitRegistry`, `TraitFactories`, `TraitEffectProvider`, `TraitEffectStateCodec`, `CombatCommandEffects`, `CombatEffectEngine`, `ICombatEffectEngine`, `CombatEffectCapabilities`, `TraitSpecAdapter`, `MainSpecializationDefinitionSnapshot`, `DeferredCombatFeatures`의 전용 부분 |
| 상태/복사/검증 | `CombatCommandState`, `BattlePreparation`, `CombatAttackProfile`, `OwnedCharacterStatsSnapshot`, `StageSnapshot`, `MovementPreviewContext`, `MovementUndoOriginFacts`, `CombatEvent`, `CombatChangeEvidenceReader`, `CombatStateBuffer` |
| 실행/이동 소비 | `AttackDeliveryExecutor`, `AttackForcedMovement`, `CommandAttackProjection`, `MovementProjection`, `CombatCommandCalculator`, `CombatTurnCalculator`, `MovementContextCalculator`, `BossRoarCalculator`, `NexusReinforcementCalculator`, `StageGimmicks/RailwayGimmickRule`, `EnemyAi/EnemyActionCandidates`의 전용 부분 |
| 로스터/저작 | `C/Application/Roster/OwnedCharacterStatsCalculator.cs`, `D/UnitTraitAuthoring.cs`, `D/UnitTraitData.cs`의 전용 부분 |
| 전용 View/DB 코드 | `V/Combat/Display/Map/FieldThornPresenter.cs`, `S/DataBaseSystem/Data/View/Table/ThornField/ThornFieldVisualDataTableSO.cs`, `.../Item/ThornFieldVisualDataSO.cs`, `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/ThornFieldVisualTable.cs` |
| View 소비/조립 | `CombatDisplayTables`, `CombatFieldDisplay`, `CombatFieldEventPorts`, `CombatEventTypeTable`, `CombatFactEventHandler`, `CombatStepContacts`, `TraitDescriptionFormatter`, `TraitDescriptionBindings`, `F/CombatSceneDisplayFactory.cs`, `F/CombatTraitPreviewFactory.cs` 및 연결 Factory |
| 전용 에셋 | `DB/ViewGroup/ThornFieldVisual/ThornFieldVisualDataTable.asset`, `.../Item/ThornFieldVisual_Default.asset`, `DB/ViewGroup/TraitVisual/Item/Cannon/TraitVisual_ThornField.asset`와 `.meta`/참조 |
| 시험 영향 | `ThornFieldDomainTests`, `ThornFieldTraitTests`, `AttackTargetPolicyIntegrationTests`, `EnhancementPoisonThornTests`, `OwnedCharacterStatsCalculatorTests`, `DeferredCombatFeatureTests`, movement/undo/preview 및 shared fixture 생성자. 검색 결과 전체를 실제 변경 성격으로 분류 |

`States/`, `Rules/`, `Effects/`는 앞선 `C/Domain/Traits/`를, `Evaluations/`는 `C/Domain/CombatEffects/`를, `.../Item/`은 같은 행의 직전 디렉터리를 뜻한다. 모든 단축 클래스명은 §2의 검색 결과에서 실제 파일을 확인한 뒤 수정한다.

## 7. Phase 4 — 정적 확인·사용자 검증·인계

### Task 4.1: 잔여 참조와 기능 독립성 확인

- [x] `git diff --check`로 공백 오류를 확인한다.
- [x] `rg -n 'ThornField|ThornConsumed|ThornMovementTileRule|FieldThornPresenter' Assets/ProjectL/Script Assets/ProjectL/DataBaseSO` 결과를 퇴역 예외와 실제 잔여로 나눈다. 문자열이 0개라는 이유만으로 GUID/직렬화 확인을 생략하지 않는다.
- [x] `rg -n 'ThornBomb' Assets/ProjectL/Script/Systems`로 규칙이 전용 기능에 모였는지 검토한다. 호출/등록/공개 계약은 정상이며 여러 피해·이동·턴 caller에 같은 폭탄 판단 코드가 반복되면 되돌려 모은다.
- [x] 새 순수 Data/Core에 UnityEngine 참조가 없는지, 새/삭제 `.cs`의 `.meta`가 짝인지, 삭제 GUID가 남지 않는지 확인한다.
- [x] 설계 §8의 세 대표 사례에 대해 수정·이해·검증 범위를 실제 파일 기준으로 갱신한다. 파일 수가 줄었다는 설명만으로 최적화 완료를 주장하지 않는다. → 설계 §11.
- [x] Core 경계 인계 문서를 수정했다면 제목에 최신 주제·갱신일을 함께 반영한다.

### Task 4.2: 사용자 Unity 인수 항목

아래는 실행해야 할 시나리오 명세다. 이 계획 자체로 Test 코드 추가/타인 시험 변경을 허용하지 않는다. 사용자가 Unity에서 확인하고, Core 담당자의 기존 검증 결과를 별도로 인수한다.

| ID | 조건 | 기대 결과 |
|---|---|---|
| V01 | 공격력 15인 선인장이 중심 적/아군/빈 칸 공격 | 기존 기본·70% 주변 피해 유지. 생존한 중심 캐릭터만 폭탄 1개. 빈 칸 잔류물 없음. 입력/행동 횟수 추가 없음 |
| V02 | 공격 명중, 보호 효과로 체력 피해 0 / 벽에 막혀 미도달 | 전자는 기폭, 후자는 기폭·부착 없음 |
| V03 | 폭발 명중, 보호 효과로 피해 0 | 넉백 계약대로 이동 후 다음 폭탄 기폭 |
| V04 | A 중심 (2,2), B 폭탄 대상 (3,2), (4,2) 비어 있음 | B는 (4,2)로 이동 후 그 칸에서 폭발. A 중심은 자기 폭발로 이동하지 않음 |
| V05 | V04에서 벽/면역 / B가 피해로 사망 | 막힘·면역은 (3,2), 사망도 넉백 없이 사망 칸. 승패 확정이면 후속 폭발 중단 |
| V06 | 폭탄이 붙은 중심 대상 재공격 | 기본 피해→기존/연쇄→생존 시 새 BombId 부착. 옛 큐가 새 폭탄을 소비하지 않음 |
| V07 | A/B/C가 서로 범위 안에 있고 B에 여러 접촉 | 동일 폭탄 1회 기폭, 결정적 FIFO, 무한 재귀 없음. 각 폭발 시작의 실제 위치 사용 |
| V08 | 아군 seq=1 부착, 적 seq=2, 아군 seq=3 시작 | seq=2는 유지, seq=3 행동 전에 폭발. 같은 턴 여러 행동으로 만료 앞당김 없음 |
| V09 | 한 폭발에 메인과 목표 보스가 함께 치명 피해 / 넉백 중 목표 달성·메인 사망 | 한 폭발의 모든 대상 피해 적용 후 판정. 메인·보스의 EntityId 순서가 바뀌어도 둘 다 피해를 받고 기존 메인 사망 우선으로 패배. 종료 뒤 넉백/연쇄·재부착 없음. 이동 중 종료는 기존 경계와 목적지 유지 |
| V10 | 같은 입력으로 preview·실행, preview 반복·취소 | 연쇄 중심/순서/피해/넉백/최종 폭탄 동일. preview가 원본·NextBombId를 변경하지 않음 |
| V11 | B 이동 후 폭발 재생 / 사망 오브젝트 반환 / 씬 해제 | 이동 후 위치에서 폭발. 반환 대상 없어도 이벤트 중심 사용. scope 누수·이중 반환 없음 |
| V12 | 기존 발리스타 중심/주변·면역·벽·지형 | 공통 경계 추출 전과 같은 넉백/도착/ledger 결과 |
| V13 | 희생 대행 / 다칸 유닛 / 출처 사망 | 실제 명중 수신자의 기존 폭탄 기폭. 중심 대상 구폭탄이 남으면 교체 기폭 후 생존 재검사·재부착. 다칸 중복 피해 없음, 로컬 부착 칸 유지, 저장 출처/수치로 처리 |
| V14 | 취소·전투 종료·재진입·최종 Sync | 부착물·폭발 표시가 남지 않고 새 전투 폭탄 상태 Empty |
| V15 | 부착 뒤 공격력 증가/감소, 다른 캐논으로 재공격 | 옛 폭탄은 옛 저장 공격력, 새 폭탄은 새 부착 순간 공격력. 기준 15×50%=7. 방어·상성 변경으로 기준 피해 변화 없음 |
| V16 | 이동 undo / 이동 중 낙사·사망 폭발 / 명령 상태 복사 | 가역 이동은 동일 폭탄 유지, 폭발은 증거와 undo 차단에 반영. 상태 유실 없음 |
| V17 | 실제 Catalog/SO 읽기와 `DataBaseAssetTests` | 선인장 신규 authoring/키 정상, 가시 장판 Query·에셋 등록 잔존 없음, Missing Script 신규 발생 없음 |
| V18 | 장판 없는 이동/강제 이동/기믹/AI, ThornHeart 반사 | 옛 가시 생성·접촉 피해 없음. 공유 상태/무관 특성은 기존 동작 유지 |

관련 기존 회귀 위치: `Assets/Tests/EditMode/ImpactBoltTests.cs`, `AttackPreviewParityTests.cs`, `CombatChangeEvidenceCoverageTests.cs`, `MovementUndoBarrierTests.cs`, `CoreIntegration/ImpactBoltPublicationTests.cs`, `CoreIntegration/DataBaseAssetTests.cs`. 실행하지 않은 결과는 미확인으로 남긴다.

**최종 인계:** 실제 변경 파일·설계 차이·사용자 확인 결과·남은 리소스/시험 승인 항목을 기록한다. 컴파일/재생이 확인되기 전 완료 검증을 주장하지 않는다. 커밋·push는 하지 않는다.

### 2026-11-17 Phase 4 실행 기록 — 정적 확인·집중 코드 리뷰·인계

Phase 0~3은 다른 세션에서 진행했다. 이 기록은 그 결과를 이어받아 확인한 것이다. 사용자가 Phase 4에서 코드 리뷰를 집중적으로 하고 구조·최적화를 챙기라고 지시했다. 작업 트리에는 다른 세션(최단 경로·메테오·용암 사슬)의 변경도 섞여 있어서, 폭탄과 장판 변경만 골라 검토했다.

**4.1 정적 확인**

- `git diff --check` 공백 오류 0(새 파일 포함).
- 장판 이름이 남은 곳은 둘이다. `TraitKey.ThornField = 40`(퇴역 번호)과 `DeferredCombatFeatures.IsTraitRetired`. 실제 실행·저작·표시 참조는 0이다.
- 지운 에셋·스크립트의 GUID 29개는 다른 에셋에서 참조 0이다. 새·삭제 `.cs`의 `.meta`는 모두 짝이 맞다.
- 새 순수 Data/Core에는 UnityEngine이 없다. `ThornBombTraitAuthoring`만 쓰는데, SO 입력 계층이라 규칙 P2에 맞다.
- 폭탄 판단은 `Application/Combat/ThornBombs/`의 예약·폭발·부착 세 파일에 모여 있다. 호출부(직접 피해·처치 연쇄·턴 시작·명령 완료·사망 경계)는 한두 줄로 부르기만 한다. 공격 실행기에 복사돼 있던 처치 연쇄 초기 근거 식은 아래 1번으로 고쳤다.

**집중 코드 리뷰.** 핵심 실행 경로(예약·폭발·부착·강제 이동 공통화·처치 연쇄)는 직접 읽었다. 상태·게시·미리보기·AI·View와 장판 삭제 회귀는 독립 검토자 둘에게 맡겼다. 지적은 모두 코드로 다시 확인한 뒤 반영 여부를 나눴다.

**반영한 것(동작 같음)**

1. **처치 연쇄 초기 근거:** 공격 실행기가 `KillChainAttackCalculator`의 초기 근거 식을 복사하고, 미리보기·AI를 포함한 모든 공격에서 생존자 배열·집합을 만들었다.
   - 이제 해석기가 폭발 중 사망만 `ThornBombResolution.KilledIds`로 기록하고, 처치 연쇄가 자기 식에서 그 사망을 뺀다(`excludedKilledEnemyIds`, 끝 선택 인자). 폭발이 없는 공격은 할당하지 않는다.
   - 실행기 판과 다른 점이 하나 있다. 같은 명령의 앞선 연쇄로 죽은 적도 빠진다. "폭탄으로만 생긴 사망은 처치로 세지 않는다"는 Phase 2 결정에 더 맞는 쪽이다.
2. **폭발 범위·넉백 거리:** 폭발 실행기에 박혀 있던 `1` 두 개를 `ThornBombRules.BlastRadius`·`KnockbackDistance`·`IsInBlast`로 옮겼다. 폭탄 수치는 이제 규칙 파일 한 곳이다.
3. **사망 직전 사실 조회:** 해석기와 부착 쪽에 두 벌이던 것을 `ThornBombResolution.PreDeathFact` 하나로 합쳤다. `CommandDeathSettlement.PreDeathFact`는 `Died` 사건만 보는데, 폭발 피해 직후에는 그 사건이 아직 없어서 따로 둔다(주석에 이유를 적음).
4. **사망 관찰:** `ObserveDeaths`는 모든 사망 경계가 부르므로, 폭탄이 없으면 정렬·할당 없이 바로 끝난다.
5. **미리보기 재조립:** `AttackPreview.WithThornBombConsequences`는 필드 21개를 다시 나열하고 배열 셋을 복사했다. 이제 구조체 복사 생성자로 공유한다. 새 필드를 빠뜨리면 컴파일 오류가 나고, 공격 계산마다 하던 배열 복사도 없어졌다.
6. **빈 호환 인자(계획 Task 3.3에서 남은 것):**
   - `CombatDamageApplication.Apply/ApplySteps`의 `eventOrigins`·`resolvedHeldAbsorptions`(호출자 0, 시험 호출은 모두 위치 인자 4개 이하라 영향 없음).
   - `MovementEnvironmentCalculator.ResolveForcedStepsSteps`의 `input`(null 검사만 남음). 기존 시험 `GimmickOccupancyTests.cs:86` 호출 한 줄을 컴파일 보정으로 맞췄다.
7. **죽은 코드:** 안 쓰는 `MovementProjection.RecordDeathAnchor`와 닿지 않는 `ForcedMovementExecution`의 `movementCause ??=`를 지웠다.
8. **API 함정:** `CombatSceneEventPresenter.Refresh`의 폭탄 인자를 필수로 바꿨다. 생략하면 부착물을 모두 지우던 기본값이다. Dev 특성 미리보기(`TraitPreviewSession` 두 곳)는 `Empty`를 명시한다. 시나리오가 폭탄 상태를 저작하지 않기 때문이다.
9. **오류 문구:** 게시 검증 문구를 실제 검사(폭심지)에 맞췄다. 합성 전이는 그룹 번호를 대조하지 않는다는 Phase 2 기록을 주석으로 남겼다.
10. **낡은 주석·빈 줄:** 사라진 장판을 설명하던 주석 15곳, `CombatFieldDisplay`의 대상 없는 주석 한 줄, `ViewDataBaseFactory`의 빈 줄 둘을 정리했다.

**반영하지 않은 지적.** 검토자가 "`CombatSceneDisplay`의 종료 국면 삼항(`Finished`면 빈 폭탄)이 Core 규칙의 사본"이라고 했다. 하지만 포기는 Buffer 국면만 종료로 바꾸고 명령 상태의 폭탄은 그대로 둘 수 있어서, 이 삼항은 유지한다.

**결정 필요(반영 안 함, 권장안 포함)**

| ID | 심각도 | 문제 | 권장 |
|---|---|---|---|
| D1 | 높음 → **해결(후속 절)** | 기차 즉사(`RailwayGimmickRule:93`)는 `MovementProjection`을 거치지 않고 `CombatCommandCalculator.Finish`로 바로 정산한다. 그래서 폭탄이 붙은 유닛이 치이면 죽은 대상의 폭탄이 상태에 남는다. 게시 검증의 살아 있는 대상 조건에서 예외가 나 F1로 멈추고, 기폭도 일어나지 않는다(설계 P09와 다름). 다른 직접 `Finish` 호출부(포효 카운터·넥서스 증원)는 사망을 만들지 않는다. | 기차 정산을 낙석처럼 `MovementProjection.ForArrival(…, rules)`와 `Complete`로 바꿔, 사망 칸에서 기폭하게 한다. 기차 규칙에 지형 규칙 목록을 넣는 조립 변경과, 기차 재생 뒤 폭발 사건 재생 확인이 필요하다. 대안은 기차 경로에서 죽은 대상의 폭탄만 지우는 것이다(P09와 다름). |
| D2 | 중간 → **해결(후속 2)** | 사건 재생 조립(`CombatEventPlaybackFactory:33-34`)이 TraitVisual의 ThornBomb 줄과 `attachmentPrefab`을 필수로 요구한다. 그런데 DB 검사(`TraitVisualData.Validate`)는 이 칸을 보지 않는다. 줄이 빠지거나 칸이 비면 DB 생성은 통과하고, 선인장이 없는 전투까지 모든 Combat 진입이 멈춘다. `attachmentPrefab`은 특성 하나만 쓰는데 모든 TraitVisual SO에 있는 칸이기도 하다. | 지금 구조는 두고, DB 검사에 "ThornBomb 줄은 프리팹 필수"와 표 등록 확인을 더한다. 대안은 프리팹을 전투 연출 설정 표의 전용 칸으로 옮기는 것이다(에셋 이전 필요). |
| D3 | 낮음 → **해결(후속 2, 소리는 창구 대기)** | 폭발 이펙트가 `CombatThornBombEventHandler:44`에 `EffectType.Boom`으로 고정돼 있고 소리가 없다. 데이터로 바꿀 수 없다(설계 §6.5 "기존 TraitVisual 우선"과 다름). | TraitVisual 큐(기존 발동 큐 형식)로 옮긴다. |
| D4 | 낮음~중간 → **해결(후속 2)** | 폭발 넉백 미리보기는 대상 점유 칸을 시작 칸 하나로 칠한다(`CombatKnockbackPreview:186-194`). 영향 칸에는 폭심지만 더해 3×3 범위가 보이지 않는다(`AttackTargetingInteraction:402-406`). | Core가 `ThornBombExplosionPreview`에 폭발 영향 칸과 대상 점유 칸을 담는다. View가 범위를 베끼지 않게 하기 위해서다. |
| D5 | 낮음 → **View 인계** | `CombatStepContacts:97-98`이 폭탄 사건 두 형식을 직접 적는다. | 사건 형식 표의 열로 판정한다. |
| D6 | 낮음 → **View 인계** | 부착물 풀을 미리 만들지 않아, 첫 부착 때 재생 도중 `Instantiate`가 일어난다. | 선인장이 있는 전투면 설치 때 하나를 미리 만든다. |
| D7 | 낮음 → **해결(후속 3)** | `EnemyActionCandidates`의 `transitLoss`는 장판 제거 뒤 사실상 쓰이지 않는다. `EnemyDecisionSession:395`의 붕괴 착지 경로만 넘기는데, 그때는 `collapseLanding`이 결과를 먼저 정한다. | 결과가 같은지 확인한 뒤 지운다. |
| D8 | 추정 → **Core 보류** | 게시 검증(`ThornBombPublicationPolicy:28-31`)은 최종 턴으로 바로 가는 턴 시작만 순번으로 받아들인다. 한 전이에 턴 시작이 둘 들어오고 그 사이에 부착이 생기면 잘못 판정한다. 지금은 그런 경로가 없다. | 경로가 생길 때 고친다. |

**시험 공백.** 연쇄·만료·종료 중단·희생 교체·이동 중 폭발(V03~V09·V13·V15·V16)을 다루는 자동 시험이 없다. 이 계획은 시험 작성 지시가 아니어서 새 시험은 쓰지 않았다. 지금은 사용자 Unity 확인 항목이고, Core 시험을 더하려면 사용자 지시가 필요하다.

**검증.** Unity 6000.0.73f1 배치 실행(Core·CoreIntegration).

- 1차: 반영 1~4·7 일부 뒤. 7,300 통과·0 실패·19 건너뜀(Core 6,116·CoreIntegration 1,184). Phase 3와 같은 수다.
- 2차: 전체 반영 뒤. 2개 실패(`AttackPreviewParityTests.FrozenMovementContinuationSemanticsMatch`·`FrozenPreviewAndActualSemanticsMatch`).
  - 원인: 복사 생성자가 빈 폭발 목록을 null로 둬서, getter가 빈 배열(`ThornBombExplosionPreview[]`)을 돌려줬다. 고정 기준 직렬화기가 이 형식을 몰랐다.
  - 옛 재조립과 같은 읽기 전용 목록으로 되돌렸다.
- 3차: 7,300 통과·0 실패·19 건너뜀. Phase 3와 같은 수다.
- 시험 실행이 바꾼 글꼴 에셋(`SCDream7 SDF.asset`)은 되돌렸다. FMOD 카탈로그는 실행 전 수정 상태 그대로다.
- 씬 재생(V01~V18)은 미확인이다.

**수정·이해·검증 범위.** 실제 파일 기준 갱신은 [설계 §11](../specs/2026-11-17-cactus-cannon-thorn-bomb-design.md#11-phase-4-실제-변경-범위-2026-11-17)에 있다.

**중단 지점.** Phase 4에서 멈춘다. D1은 후속, D2~D4는 후속 2, D7은 후속 3에서 해결했다. D5·D6은 View 인계, D8은 Core 보류다. 커밋·push 없음.

#### Phase 4 후속 — 낙사·기차·사망 특성 (2026-11-17 사용자 지시)

사용자 지시는 셋이다. "낙사할 때는 안 터지고, D1은 권장대로 진행해", 이어서 "자폭과 공포도 기차가 밀어버리는 건 활성화 해줘. 죽은 그 자리에서".

- **낙사 = 소멸(설계 R15):**
  - `ThornBombResolution.ObserveDeaths`가 죽은 부착 대상이 같은 투영에서 낙사했으면, 폭탄을 예약하지 않고 활성 상태에서 지운다. 그리고 새 사건 `CombatThornBombDiscardedEvent`(`ThornBombDiscarded = 65`)를 남긴다. 앞서 명중으로 예약됐어도 같다. 큐 항목은 꺼낼 때 건너뛴다.
  - 낙사 판정은 사망 특성의 낙사 제외와 같다(같은 투영의 `CombatFallEvent`).
  - 게시 검증(`CombatChangeEvidenceReader`)은 소멸 사건을 분류하고 활성 상태에서 뺀다. 같은 전이에 그 대상의 낙사 사건이 있어야 한다.
  - View는 사건 형식 표에 한 줄을 더했다(주체 = 낙사 유닛, 동시 넉백을 끊지 않음). 처리기는 BombId로 부착물을 거둔다.
  - 기존 시험 `CombatChangeEvidenceCoverageTests`(사용자 작성, Phase 1에 폭탄 두 줄을 더한 곳)에 소멸 사건 줄 하나를 더했다.
- **D1 기차 = 사망 칸 기폭(설계 R16):**
  - `RailwayGimmickRule.TryAdvance`가 `CombatCommandCalculator.Finish` 직접 호출 대신 낙석처럼 `MovementProjection.ForArrival(...).Complete()`로 끝낸다. 지형 규칙 목록은 `StageGimmickComposition`이 넣는다. 생성자 인자는 선택이라 기존 시험의 `new RailwayGimmickRule()`은 그대로다.
  - 피해자 연속 기록·환경 귀속·영혼 자리(치이기 전 위치)는 옛 길과 같다.
- **기차 즉사 사망 특성:**
  - 기차 경로가 같은 완료 정산을 타므로, 자폭·공포가 사망 직전 위치(레일 칸)에서 발동한다.
  - 사망 특성 설계 §1.2·D7·P17의 "기차 제외"를 바꾼 기록은 [사망 특성 설계 §17](../specs/2026-11-10-enemy-death-traits-design.md#17-기차-즉사도-자폭공포-발동-2026-11-17)에 있다. 재구성은 그대로 기차를 버티지 않는다.
- **검증:** Unity 배치 실행(Core·CoreIntegration) 결과는 7,301 통과·0 실패·19 건너뜀이다.
  - 통과가 하나 는 것은 소멸 사건의 검증 범위 사례다.
  - 기존 기차·기믹 시험은 투영 경로로 바꾼 뒤에도 통과했다.
  - 다만 낙사 소멸, 기차에 치인 폭탄, 기차에 치인 자폭·공포를 직접 확인하는 자동 시험은 없다.
  - 시험이 바꾼 글꼴 에셋은 되돌렸다.
- **Unity 확인 추가:**

| ID | 조건 | 기대 결과 |
|---|---|---|
| V19 | 폭탄이 붙은 유닛이 넉백으로 낭떠러지에 떨어짐(앞서 명중으로 예약된 경우 포함) | 폭발 없음. 추락 연출 뒤 부착물이 사라짐. 다른 유닛의 동시 넉백은 끊기지 않음 |
| V20 | 폭탄이 붙은 유닛이 기차에 치임 | 기차 연출 뒤 그 레일 칸에서 폭발·주변 피해·넉백·연쇄. 전투가 멈추지 않음 |
| V21 | 자폭·공포 유닛이 기차에 치임 | 기차 연출 뒤 그 레일 칸에서 자폭 깜빡임·폭발·공포 적용. 재구성 유닛은 버티지 않고 죽음 |

#### Phase 4 후속 2 — D2·D4·D3 (2026-11-17 사용자 지시)

사용자는 "D2, D4, D3"를 지시했다. D3은 방식 질문에 "TraitVisual 발동 줄"을 골랐고, 이어서 "일단 진행하는데 나중에 소리 창구 열거래"라고 했다.

- **D2 부착물 데이터 검사:**
  - "부착물을 붙이는 특성인지"의 원본은 `TraitVisualData.RequiresAttachment` 하나다(지금은 가시 폭탄뿐).
  - 그 줄은 부착물 프리팹이 필수이고(`TraitVisualData.Validate`), 표에 그 줄이 있어야 한다(`TraitVisualTable` 생성).
  - 둘 다 부트의 DB 생성에서 멈춘다. Combat 진입 때 조립에서 멈추던 것을 앞으로 당긴 것이다.
  - 실제 에셋을 쓰는 시험만 이 표를 만들어서 시험 영향은 없다.
- **D4 폭발 미리보기 범위·점유 칸:**
  - Core `ThornBombExplosionPreview`에 `BlastCells`(판 안의 폭발 칸, 규칙 `ThornBombRules.BlastCells`)와 `KnockbackOccupiedCellsOf(대상)`(밀기 직전 점유 칸, 같은 폭발의 앞선 대상이 민 뒤)를 더했다.
  - View는 연쇄 폭발 칸을 영향 칸으로 칠하고(`AttackTargetingInteraction`), 여러 칸 유닛의 넉백 도착 칸을 점유 칸 전체로 칠한다(`CombatKnockbackPreview`). 범위 계산은 View에 두지 않았다.
  - 생성자를 부르는 곳은 폭발 실행기 하나다.
- **D3 폭발 연출을 TraitVisual 발동 줄로:**
  - Core `TraitActivationKind.ThornBombDetonated = 420`을 더했다. View 조회용 열쇠라 Core는 이 발동을 내지 않고, 판독기도 거절한다. 폭발 사실은 그대로 `CombatThornBombDetonatedEvent` 하나다.
  - `CombatThornBombEventHandler`는 기존 특성 발동 줄 보관(`CombatActivationRows.Of(특성, 발동 종류)`)에서 그 줄을 찾는다. 그리고 순간 큐 재생(`CombatMomentCuePlayback.PlayAccent`)으로 폭심지에 낸다(선택 칸·영향 칸 = 폭심지). 줄이 없으면 다른 특성 발동처럼 아무것도 내지 않는다.
  - `TraitVisual_ThornBomb.asset`에 발동 줄 하나를 넣었다(Boom·Start·SelectedCell·자연 끝). 화면은 옛 고정 코드와 같다. 이펙트는 이제 이 줄에서 바꾼다.
  - 기존 시험 `CombatChangeEvidenceVariantTests`(사용자 작성)의 예약 목록에 새 값을 더했다. 모든 발동 종류가 표본 또는 예약이어야 한다는 전수 검사다.
  - **소리는 아직 없다.** 특성 발동 줄에는 소리 칸이 없다. 소리 창구는 나중에 열기로 했다(사용자). 창구가 열리면 그 슬롯을 폭발 연출에 연결한다.
- **검증:** Unity 배치 실행(Core·CoreIntegration)을 두 번 했다.
  - D2·D4 뒤: 7,301 통과·0 실패·19 건너뜀.
  - D3 뒤: 7,301 통과·0 실패·19 건너뜀. `DataBaseAssetTests`가 실제 에셋으로 새 발동 줄과 부착물 검사를 통과했다.
  - 미리보기 범위·점유 칸과 폭발 연출 줄은 자동 시험이 없어 Unity 확인 항목이다(View 시험 없음).
  - 시험이 바꾼 글꼴 에셋은 되돌렸다.
- **Unity 확인 추가:**

| ID | 조건 | 기대 결과 |
|---|---|---|
| V22 | 가시 폭탄 TraitVisual 줄을 표에서 빼거나 프리팹을 비우고 부트 | DB 생성에서 멈춤(Combat 진입 전). 되돌리면 정상 |
| V23 | 연쇄가 생기는 조준 호버(대상에 폭탄 있음, 여러 칸 유닛 포함) | 연쇄 폭발마다 3×3(판 안) 칸이 영향 칸으로 칠해짐. 여러 칸 유닛의 넉백 도착 칸이 점유 칸 전체 모양 |
| V24 | 폭발 재생 | 폭심지에서 Boom 이펙트(옛 화면과 같음). TraitVisual 줄의 이펙트를 바꾸면 그대로 바뀜 |

#### Phase 4 후속 3 — D7, View 인계 (2026-11-17 사용자 지시)

사용자 지시는 "나머지는 View 인계로 넘기자, D7은 그냥 너가 해줘"다.

- **D7 적 AI 치명 판정의 죽은 인자:**
  - `EnemyActionCandidates.IsCertainLethal`·`LethalRank`의 `transitLoss`와 `EnemyDecisionSession.CandidateLethality`의 `entryLoss` 인자를 걷었다. 진입 손실 `entryLoss` 자체는 점수 계산에 그대로 쓴다.
  - 결과가 같은 근거: 간이 평가의 진입 손실은 `EntryEstimateSteps`가 끝 칸 붕괴일 때만 체력 전부로 주고 나머지는 0이다. 같은 조건이 `collapseLanding`이므로, 0이 아닐 때는 붕괴 착지가 먼저 치명·순위를 정한다. 정밀 평가는 원래 0을 넘겼다.
  - 기존 시험 `EnemyExchangeModelTests`(사용자 작성)는 둘을 고쳤다.
    - 호출 인자를 맞췄다.
    - 통과 피해를 더하던 단언 둘(치명 판정 1, 순위 1)을 걷었다. 순위 사례는 같은 의도("덜 치명적인 쪽이 앞")를 확실한 손실 110으로 바꿔 유지했다.
  - 첫 실행에서 이 시험 파일의 4인자 호출 셋을 놓쳐 컴파일 오류가 났고, 맞춘 뒤 다시 돌렸다.
  - 다시 돌린 결과는 7,301 통과·0 실패·19 건너뜀이다. AI 고정 기준 비교 시험(`AttackPreviewParityTests` 등)도 통과해 판단이 그대로다. 시험이 바꾼 글꼴 에셋은 되돌렸다.
- **View 인계:** D5·D6, 폭발 소리 슬롯, View 쪽 Unity 확인(V10·V11·V14·V19~V24)을 [가시 폭탄 View 전용 인계](../handoffs/2026-11-17-cactus-cannon-thorn-bomb-view-handoff.md)로 넘겼다(미전달).
- **D8:** View 몫이 아니라 Core 게시 검증이라 Core 보류로 남긴다.

## 8. 요구사항 추적

| 설계 | Task / 인수 |
|---|---|
| R01~R03, 기존 공격 유지·중심 자동 부착 | 2.2·3.1·3.2 / V01·V02·V13 |
| R04~R05, 0피해 명중 기폭 | 2.1·2.2 / V02·V03 |
| R06, 턴 시작 만료 | 1.1·2.3 / V08 |
| R07, 공격력 스냅샷·방어/상성 무시 | 1.1·2.1·2.2 / V15 |
| R08~R10, 중심 제외·이동 후 연쇄·사망 | 2.1·2.3 / V04·V05·V07·V12·V16 |
| R11~R12, 재부착·승패 중단 | 2.1~2.3 / V06·V09 |
| R14, 한 폭발의 전체 피해 적용 후 승패 판정 | 2.1·2.3 / V09 |
| R13, 장판 삭제 | 0.2·3.2·3.3·4.1 / V17·V18 |
| 설계 §8 구조/SSOT/FW | 전 Task, 4.1에서 실제 변경 범위 재평가 |

## 9. 문서 작성 검증 기록

- 사용자와 합의한 공격 중심 자동 부착, 방어·상성 무시, 한 폭발의 전체 피해 후 승패 판정과 후속 중단을 반영했다.
- 현재 코드를 읽어 장판의 보류 상태, 기본 Radial 70%, 명중 경계, 중심 넉백, 공통 피해/턴/게시 경계를 확인했다.
- 설계의 P항목은 제안으로 구분했다. 신규 계약은 구현 예정이며 현재 API/실행 성공으로 기술하지 않았다.
- 독립 설계 검토 후 중심 대상의 구폭탄 교체 기폭과 실제 사망 칸 캡처를 보완했다. 승패 검사 단위 질문은 2026-11-17 사용자 답변으로 해소됐으며 설계 R14에 기록했다.
- 2026-11-17 사용자 지시로 Phase 0 조사와 권한/삭제/시험 분류 기록을 완료했다. [상세 감사 기록](2026-11-17-cactus-cannon-thorn-bomb-phase0-audit.md) 참조. 제품 구현과 후속 Phase는 시작하지 않았다.
- 위 설계·Phase 0 기록 당시 제품 코드는 변경하지 않았다. 이후 Phase 1 코드와 미실행 검증은 §3에 기록했다.
- 2026-11-17 다른 세션(최단 경로·메테오·사슬 리뷰)에서 사용자 지시로 `CombatStateBuffer.ValidateThornBombPlacement`를 본문 그대로 `Application/Combat/ThornBombs/ThornBombPublicationPolicy.ValidatePlacement`로 옮겼다. 이유는 컴파일 오류다. Systems가 참조하지 않는 Core.Domain의 internal `ThornBombRules`와 Application의 internal `RequiredProfile`을 Buffer가 직접 불렀다. 이제 Buffer는 같은 위치에서 그 함수를 한 줄로 부른다. 이후 폭탄 게시 검증은 이 Application 클래스에서 고친다. 같은 실행에서 Core 시험 8개(`AttackPreviewParityTests`·`AttackPreviewResumptionTests`·`EnemyActionAssessmentTests`·`EnemyDecisionReuseParityTests`의 frozen 비교)가 실패했다. 직렬화된 상태에 `ThornBombs`가 들어가 저장 참조와 달라진 것이므로 이 작업에서 참조를 다시 만들어야 한다. 기록은 [리뷰 §7.6](../specs/2026-11-17-shortest-move-meteor-chain-code-review.md).
