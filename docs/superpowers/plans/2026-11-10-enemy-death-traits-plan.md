# 적 전용 특성 재구성·자폭·공포 Task 계획

> 실행 규칙: Phase 하나씩 진행하고 Phase가 끝나면 멈추고 보고한다. 이 문서는 커밋·push 권한을 주지 않는다. 앞 Phase가 통과했다고 다음 Phase를 시작하지 않는다.

- **목표:** 승인된 설계대로 적 전용 특성 재구성(490)·자폭(500)·공포(510)를 Core에 넣는다. 자폭·공포는 "능력 인터페이스 + 명령 끝 확인"으로, 재구성은 공통 피해 경계 안의 가로채기로 처리한다.
- **구조 방침:** 기존 효과 엔진의 능력/실행자 두 층, 공통 피해 경계, 이동 투영의 명령 완료 정산, 특성 레지스트리, Systems → Buffer 게시를 그대로 쓴다. 따로 담아 두는 큐, 새 이벤트 버스, 특성별 부트스트랩 계층은 만들지 않는다.
- **설계:** [적 전용 특성 설계 3판](../specs/2026-11-10-enemy-death-traits-design.md). Q-ED1~Q-ED8 결정 완료(2026-11-10 사용자).
- **구조 기준:** 사용자가 준 `feature-change-locality-guidelines.md`(Downloads, 2026-11-10에 읽음). 대표 변경 사례로 수정·이해·검증 범위를 평가하고, 게임 규칙상 필요한 연결과 구현이 만든 불필요한 결합을 나눈다(§7). 이 외부 문서는 고치지 않으며 구현 권한을 더 주지 않는다.
- **상태:** 계획 작성 완료, 코드 착수 전. 기준 판은 `1e36c1854`(`feature/Cheon/Core/NewCombatSytem`). 설계 문서만 작업 트리에서 바뀌어 있다.

## 공통 제약

- Framework 계약, Core 경로, 다른 특성의 공개 동작, Buffer·Job 소유권을 지킨다. 이 작업을 특성 전체 재작성으로 넓히지 않는다.
- 이 작업 범위는 Core다. View 코드·씬·VFX·프리팹은 범위 밖이고, View 쪽 할 일은 설계 §9 인계 목록으로만 넘긴다. View 시험 코드는 쓰지 않는다.
- 시험은 `ProjectL.Tests.Core`(`T/`)와 `ProjectL.Tests.CoreIntegration`(`I/`)에만 쓴다. Framework 시험은 고치지 않는다. 새 시험은 시험이 직접 만든 입력을 쓴다.
- 고칠 수 있는 기존 시험은 작성자가 GwonCheonmyeong인 줄만이다. 계획에 적힌 기대값 변경 외에는 기존 단언을 바꾸지 않는다. 기준 실패를 가리지 않는다.
- 검증은 사용자가 Unity Test Runner로 돌린다. Claude는 MSBuild를 쓰지 않고, 실행 뒤 `C:\Users\hyeon2-400\AppData\LocalLow\DefaultCompany\Project_L_v2\TestResults.xml`을 읽어 판정한다. 편집기가 닫혀 있고 사용자가 허락하면 Claude가 배치 모드로 두 시험 어셈블리만 돌릴 수 있다.
- 코드 규칙: Allman, 공백 4칸, 접근자 명시, 블록 네임스페이스, UTF-8·LF. 새 `.cs`는 `.meta`와 함께 둔다. Core 순수 코드에는 `UnityEngine` 참조를 두지 않는다. InGame 네임스페이스 안에서는 `System.Enum.IsDefined`를 쓴다.
- 문서는 한국어, 새 코드 주석·XML 문서도 한국어로 쓴다.
- **운영 데이터는 바꾸지 않는다.** 세 특성을 운영 적 유닛 에셋에 붙이지 않는다. View 등록(설계 §9)이 끝나기 전에 운영 데이터에서 새 사건·상태 키가 나오면 View가 멈출 수 있다. 붙이는 일은 View 대응 뒤 기획·사용자 결정으로 한다.
- 세 특성은 Phase A에서 `DeferredCombatFeatures.IsImplementationPending`으로 막아 둔다. 레지스트리가 대기 중 특성의 생성을 예외로 막으므로(`TraitRegistry.cs:125`), 각 특성의 동작을 구현하는 Task를 시작할 때 그 특성만 푼다. 시험 전용 우회는 운영 코드에 두지 않는다.

## 검토 초점

| 번호 | 사례 | 담당 Task |
|---|---|---|
| R1 | 같은 공격으로 자폭 적이 죽고 처치 연쇄가 이어짐: 처치 연쇄는 폭발 사망을 세지 않고, 원 공격 결과의 피격 체력에 폭발 피해가 섞이지 않는다. | C.1 |
| R2 | 넉백 공격으로 죽은 자폭 적: 넉백 끝 칸에서 터지고, 다른 대상이 밀리다 지뢰를 밟아도 원래 칸에서 먼저 터지지 않는다. 비보유자의 사망 위치는 그대로다. | C.2 |
| R3 | 진영 종료 기믹 창(낙석)에서 공포가 걸림: 걸린 쪽의 다음 자기 턴 한 번을 막고 바로 풀리지 않는다. 무장 해제가 이미 더 길면 긴 쪽이 남는다. | B.1, B.3 |
| R4 | 재구성 적이 치명 피해를 받음: 사망 사건·처치 보상·자폭이 없고, 공격 결과 체력 검사와 변화 근거 판독기가 통과한다. 두 번째 치명 피해에서는 진짜 죽고 자폭·공포가 발동한다. | D.1, D.2 |
| R5 | 낙석으로 HP가 0: 자폭·공포가 모든 낙석 수신자 사건 뒤에 나오고 낙석 근거 검사가 통과한다. 낙석으로 죽지 않는 경우는 바꾸기 전과 결과가 같다. | F.1 |
| R6 | 공개 공격 미리보기·AI 평가: 자폭·공포는 실행하지 않고 재구성은 반영한다. | B.2, D.1 |
| R7 | 같은 공격으로 보스와 자폭 적이 함께 죽음: 승패가 정해져 폭발하지 않는다. | B.2 |

## 1. Phase 표와 멈춤 규칙

| Phase | 결과물 | 앞 Phase | 끝과 검토 지점 |
|---|---|---|---|
| 0 | 기준 시험 결과, 계약·호출자 목록, 기존 시험 작성자·기대값 영향 | 승인된 설계 | 증거와 실제 막힘만 보고. 동작 변경 없음. 멈춤. |
| A | 특성 키·적 전용 규칙·단계 표·DB 입력(Authoring·Data·Spec·어댑터)·적 전용 검사 셋. 세 특성은 대기 목록으로 막고 레지스트리에는 아직 등록하지 않음 | 0 | 동작 변화 없음 확인. 멈춤. |
| B | 사망 능력 인터페이스·실행자·완료 정산 단계·미리보기 스위치, 행동 제한 "긴 쪽" 규칙, 공포(B.2에서 대기 해제) | A | 공포 동작과 확인 구조 검증. 멈춤. |
| C | 자폭 피해·귀속·연쇄, 넉백 시체 밀기(C.1에서 대기 해제) | B | 자폭 검증. 멈춤. |
| D | 재구성 가로채기·상태·보호막·부활(Lv1·Lv2)·행동 입장·판독기(D.1에서 대기 해제) | C(재구성 중 진짜 사망 → 자폭·공포 결합 사례 때문) | 재구성 검증(Lv3 넉백 제외). 멈춤. |
| E | 강제 이동 칸 정산을 이동 투영 입구로 꺼내기(동작 보존) → Lv3 부활 넉백 | D | 공격 넉백 보존 + 부활 넉백 검증. 멈춤. |
| F | 낙석 연결(Q-ED2 A), 합친 시험, 전체 회귀, 변경 독립성 검토, View 인계 절 | B·C·D·E | 증거와 남은 한계 기록. 멈춤. |

Phase 보고에는 끝낸 Task, 바꾼 파일 목록, 시험 결과(기준 실패와 새 실패를 나눔), 남은 일, 다음 Phase, §7 형식의 범위 비교를 담는다. 승인된 규칙·공개 소유권·금지 영역을 바꿔야 하면 그 일을 멈추고 차이를 설명한다.

## 2. 파일 지도

줄임: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`, `S/` = `Assets/ProjectL/Script/Systems/`, `D/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/`, `T/` = `Assets/Tests/EditMode/`, `I/` = `Assets/Tests/EditMode/CoreIntegration/`. 중괄호는 그 파일 이름들만 뜻한다.

| 파일 | 책임 |
|---|---|
| 새 `C/Contracts/Common/TraitOwnerPolicy.cs` | 적 전용 규칙 원본 `IsEnemyOnly(TraitKey)`. |
| `C/Contracts/Common/{TraitKey,TraitActivationKind}.cs`, `C/Contracts/CombatEffects/StatusEffectKey.cs`, `C/Contracts/Combat/CombatEvent.cs`, `C/Contracts/Results/{MovementFailureReason,AttackFailureReason}.cs` | 열거 값 덧붙이기만. 기존 값은 그대로. |
| 새 `C/Contracts/Commands/Traits/EnemyDeathTraitLevels.cs` | 단계(1~3) → 수치 표 한 곳(Q-ED8 A). DB Data(Systems 어셈블리)와 Spec(Contracts)이 같은 범위 검사를 쓰므로 Contracts에 둔다(`TraitRankValues` 선례, Phase A에서 Domain → Contracts로 옮김). |
| 특성 묶음: 재구성·자폭·공포마다 `C/Contracts/Commands/Traits/<이름>TraitSpec.cs`, `C/Contracts/Snapshots/Traits/<이름>TraitStatusSnapshot.cs`, `C/Domain/Traits/Definitions/<이름>TraitDefinition.cs`, `C/Domain/Traits/States/<이름>RuntimeState.cs`, `C/Domain/Traits/Rules/<이름>TraitRule.cs`, `C/Domain/Traits/Effects/{<이름>EffectRule,<이름>EffectStateSnapshot}.cs`, `C/Application/Combat/Traits/<이름>TraitFactories.cs`, `D/Unit/Trait/<이름>Trait{Data,Authoring}.cs` | 재생 특성과 같은 기존 묶음 꼴. 상태 없는 처치 연쇄도 상태·스냅샷 파일을 모두 가지므로 자폭·공포도 같은 묶음을 만든다(TP-4, Phase 0 확인). |
| `C/Application/Combat/Traits/{TraitRegistry,TraitEffectProvider}.cs`, `C/Domain/Traits/Effects/TraitEffectStateCodec.cs`, `C/Application/Combat/CombatCommandEffects.cs`, `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/{TraitSpecAdapter,MainSpecializationDefinitionSnapshot}.cs`, `C/Application/Combat/DeferredCombatFeatures.cs` | 기존 특성 등록 지점. 새 특성 등록에 따른 정상적인 연결 변경이다. `MainSpecializationDefinitionSnapshot`의 특성 정의 switch는 모르는 정의에서 예외를 내므로 세 갈래를 더한다(적 전용이라 닿지 않지만 빠짐없는 분기를 유지). |
| `S/DataBaseSystem/Data/InGame/Table/Unit/Item/UnitData.cs`, `C/Application/Roster/TraitGrowthCatalogValidator.cs`, `C/Application/Enhancements/OwnedUnitSpecResolver.cs`, `C/Application/Combat/UnitEligibilityRules.cs` | 적 전용 검사 셋(설계 §8). DB 단계 검사는 유닛 한 줄 규칙이라 그룹 조립(`InGameDataBaseFactory`) 대신 `UnitData.ValidateTraits`에 둔다(DB 생성 때 멈추는 것은 같고 공용 조립 파일을 건드리지 않음, Phase A 변경). 병과 기본 특성 검사는 기본 특성 원본 `RequireAvailableDefault`에 둔다. |
| `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`, `C/Domain/CombatEffects/{ICombatEffectEngine,CombatEffectEngine}.cs`, 새 `C/Domain/CombatEffects/Evaluations/{EffectDeathTraitContribution,EffectDeathTraitEvaluation}.cs` | 사망 능력 `IEffectDeathTraitRule`과 엔진 입구(처치 연쇄 능력과 같은 꼴). |
| 새 `C/Application/Combat/DeathTraitApplication.cs` | 명령 끝 확인 실행자. 새로 죽은 보유자 찾기, 바퀴 실행, 자폭·공포 갈래. |
| `C/Application/Combat/MovementProjection.cs`, `C/Application/Combat/CommandAttackProjection.cs` | 완료 정산 단계 하나, 사망 특성 실행 여부(생성 입력에서 파생), 공포용 상태 적용 입구. |
| `C/Domain/CombatEffects/Rules/AppliedStatusRules.cs`, 새 `C/Domain/CombatEffects/Rules/ActionRestrictionTiming.cs` | 행동 제한 "긴 쪽" 겹침(P1), 종료 감소가 남은 진영 판단(P2). |
| `C/Contracts/Combat/CombatChangeEvidenceReader.cs` | 새 사건·발동 종류 읽기, 낙석 근거 검사 완화(M4)·재구성 한 줄. |
| `C/Application/Combat/{AttackKnockbackCalculator,TraitKillSettlement}.cs` | 시체 밀기 경로(D5), 죽은 귀속 유닛의 처치 특성 정산 건너뛰기(P7). |
| 새 `C/Contracts/CombatEffects/ReconstructionStatus.cs`, 새 `C/Domain/Traits/Rules/ReconstructionRules.cs`, 새 `C/Application/Combat/ReconstructionTurnApplication.cs` | 재구성 상태 값, 발동 판단·결과 원본, 턴 시작 부활. |
| `C/Contracts/CombatEffects/{AppliedStatus,CharacterStatusCompatibility}.cs`, `C/Contracts/Snapshots/StatusEffectSnapshot.cs`, `C/Application/Combat/StatusEffectReadModel.cs`, `C/Application/Combat/CombatDamageApplication.cs`, `C/Application/Combat/CombatActionAdmission.cs`, `C/Application/Combat/CombatTurnCalculator.cs` | 재구성 상태 등록, 가로채기, 행동 입장, 턴 시작 순서. |
| `C/Application/Combat/StageGimmicks/StageHazardForecast.cs` | 낙석 예보가 행동 입장 함수를 쓰게 함(설계 §4.1). |
| `C/Application/Combat/AttackForcedMovement.cs` | Phase E에서 칸마다 정산을 이동 투영 입구로 꺼냄. |
| `C/Application/Combat/StageGimmicks/RockfallGimmickRule.cs` | Phase F: 끝을 `ForArrival(...).Complete()`로, 재구성 가로채기. |

## 3. 검증 방법

- **실행:** 사용자가 Unity Test Runner(EditMode)에서 Phase가 정한 시험 클래스를 돌린다. 시험 이름은 `ProjectL.Tests.Core.<클래스>`(`T/`), `ProjectL.Tests.CoreIntegration.<클래스>`(`I/`)다.
- **판정:** Claude가 `TestResults.xml`을 읽는다. 파일 수정 시각이 이번 실행과 맞는지, 요청한 클래스가 실제로 돌았는지, 기대하지 않은 실패가 없는지 본다. 결과 파일이 없거나 선택이 비었거나 컴파일 오류면 통과가 아니다.
- **기준 비교:** Phase 0의 전체 실행에서 나온 실패 이름·문구를 기준으로 남기고, Phase 끝마다 그 목록과 비교해 새 실패만 따로 적는다. 기준 실패는 고치지 않는다.
- **Task 순서:** Task마다 가장 작은 의미 있는 새 시험을 먼저 쓰고 실패를 본 뒤 구현하고, 그 Task의 대상 시험을 돌린다. 동작을 보존하는 옮기기는 기존 특성 시험 + 경계를 넘는 확인 하나면 되고, 옮긴 함수마다 시험을 만들지 않는다.
- **전체 실행:** Phase 0과 Phase F 끝에 두 어셈블리 전체를 돌린다. 그 사이 Phase 끝에는 그 Phase 표의 "다시 돌릴 시험"만 돌린다.
- **정적 확인:** `git diff --check`, `.meta` 짝, 새 Core 순수 코드에 `UnityEngine` 없음, 열거 값 재사용 없음, 운영 에셋·FMOD 캐시 변경 없음. 정적 확인으로 컴파일·실행 검증을 대신하지 않는다.

## Phase 0 — 기준과 계약 목록

### Task 0.1: 증거와 바꿀 수 있는 범위 확정

**파일:** 설계, 이 계획, `framework-architecture.md`·`framework-programmer-guide.md`, 아래 호출 경로의 코드. 이 계획의 실행 기록만 고친다.

- [ ] `git status --short`, 브랜치, HEAD를 적는다. 다른 사람 변경은 그대로 둔다.
- [ ] 설계 §11의 "커밋 전 AI 작업"이 지금 커밋돼 있는지 확인한다(2026-11-10 계획 때 `StageHazardForecast`·`EnemyHazardForecastTests`는 저장소에 있음). Phase D 착수 조건 하나가 풀렸는지 기록한다.
- [ ] 경로를 따라가 소유자·순서를 적는다:
  - 공격: 선택 → 직접 피해(`AttackDirectDamageCalculator`) → 넉백 → 피격 체력 고정 → 강제 도착 → 처치 연쇄 → 완료 정산(`CompletionWork.Build`) → `Finish` → Systems 게시 → Buffer.
  - 턴 시작: `BeginControllerTurn` → 특성 수명 → `ForArrival` → 지속 피해 → `Complete`.
  - 낙석: `StageGimmickComposition` → `RockfallGimmickRule.Impact` → `Finish` → `Merge` → `CleanupControllerTurn`.
- [ ] 이동 투영을 만드는 모든 자리(2026-11-10 계획 때 찾은 것: `CombatMovementCalculator.cs:193`, `CombatPlacementCalculator.cs:48`, `CombatTurnCalculator.cs:44,94,139,170`, `CommandAttackProjection.cs:210`, `TornadoGimmickRule.cs:87`)와 각자의 미리보기 여부를 표로 만든다. AI 평가가 `BeginProjectRoute(..., preview: true)`·`BeginPreview`로만 들어오는지 확인한다(TP-3).
- [ ] 특성 레지스트리가 상태 없는 특성에 상태 팩토리·상태 스냅샷을 요구하는지 확인한다. 처치 연쇄 같은 무상태 특성을 선례로 본다(TP-4).
- [ ] 희생 특성이 피해 경계 안에서 자기 상태를 읽고 바꾸는 길(`SacrificeDamageSettlement`)을 재구성 `Used` 갱신의 선례로 확인한다.
- [ ] 기대값이 바뀔 수 있는 기존 시험을 적는다. 계획 때 확인한 작성자는 모두 GwonCheonmyeong이다: `AppliedRestrictionStatusTests`, `StatusEffectContractTests`, `RockfallGimmickTests`, `I/RockfallPublicationTests`, `CombatChangeEvidenceCoverageTests`, `CombatChangeEvidenceVariantTests`, `ObstacleReactionTests`, `KillChainAttackTraitTests`. `DeferredCombatFeatureTests`는 대기 목록이 비어 있다고 단언하지 않는다(계획 때 확인). 새 사례만 더한다(TP-7).
- [ ] 사용자에게 두 어셈블리 전체 실행을 요청하고, `TestResults.xml`에서 기준 실패 이름·문구를 기록한다.
- [ ] 기준과 실제 막힘을 보고한다. **Phase 0 검토 지점에서 멈춘다.**

## Phase A — 기반 (동작 변화 없음)

### Task A.1: 키와 적 전용 규칙

**파일:** `C/Contracts/Common/TraitKey.cs`, 새 `TraitOwnerPolicy.cs`·`EnemyDeathTraitLevels.cs`, 새 `T/EnemyDeathTraitFoundationTests.cs`.

**계약:**
- `TraitKey.Reconstruction = 490`, `SelfDestruct = 500`, `Fear = 510`. 이 Phase에서 더하는 열거 값은 이것뿐이다.
- `public static bool TraitOwnerPolicy.IsEnemyOnly(TraitKey key)`: 세 키만 참.
- `EnemyDeathTraitLevels`: 단계 1~3 밖이면 `ArgumentOutOfRangeException`. 재구성 보호막 % = 0/50/50, 부활 넉백 = 꺼짐/꺼짐/켜짐, 자폭 % = 50/75/110. 단계 범위 검사 함수 하나를 Data·Spec이 함께 쓴다.
- 나머지 열거 값은 처음 내는 Task에서 더한다(Phase 0 범위 변경): `StatusEffectKey.Reconstructing = 19`·`CombatEventKind.Reconstructed = 49`·`TraitActivationKind.ReconstructionStarted = 370`·`ReconstructionEnded = 380`·`MovementFailureReason.Reconstructing = 52`·`AttackFailureReason.Reconstructing = 52`는 D.1·D.2, `SelfDestructExploded = 390`은 C.1, `CorpseDisplaced = 400`은 C.2(TP-1).

- [ ] `EnemyDeathTraitFoundationTests`: 키 값, 적 전용 판단, 단계 표 값과 범위 밖 예외.
- [ ] 구현하고 대상 시험과 `LegacyEnhancementRetirementTests`(TraitKey 중복 없음)를 돌린다.

### Task A.2: DB 입력과 대기 표시

**파일:** `D/Unit/Trait/{Reconstruction,SelfDestruct,Fear}Trait{Data,Authoring}.cs`, `C/Contracts/Commands/Traits/{Reconstruction,SelfDestruct,Fear}TraitSpec.cs`, `TraitSpecAdapter.cs`, `MainSpecializationDefinitionSnapshot.cs`, `DeferredCombatFeatures.cs`, 새 `I/EnemyDeathTraitAuthoringTests.cs`.

**계약:**
- `*TraitAuthoring.ToData()` → 순수 불변 `*TraitData`(단계 하나) → `TraitSpecAdapter` → `*TraitSpec(level)`. 단계 검사는 `EnemyDeathTraitLevels`의 범위 함수 하나를 쓴다.
- `MainSpecializationDefinitionSnapshot`의 정의 switch에 세 Spec 갈래를 더한다(값 = 단계).
- `DeferredCombatFeatures.IsImplementationPending`이 세 키를 참으로 돌려준다. `UnitCatalog`이 대기 특성을 가진 유닛을 거절한다(지금 동작 그대로).
- 레지스트리 등록(정의·런타임 상태·상태 스냅샷·규칙·효과 규칙·효과 상태 스냅샷·팩토리)은 하지 않는다. 대기를 푸는 Task(B.2·C.1·D.1)에서 한다(Phase 0 범위 변경).

- [ ] `EnemyDeathTraitAuthoringTests`: 시험이 만든 SO로 `GetData()`가 매번 새 순수 데이터를 돌려줌, 어댑터 변환이 단계를 그대로 옮김, 범위 밖 단계 거절, 대기 중 특성을 가진 유닛 정의는 `UnitCatalog`이 거절.
- [x] 대기 사례는 `EnemyDeathTraitFoundationTests.DeathTraits_StayImplementationPendingAndUnregistered`에 두었다. `DeferredCombatFeatureTests`는 고치지 않았다(TP-7).
- [x] 구현하고 대상 시험과 `DeferredCombatFeatureTests`를 돌린다.

### Task A.3: 적 전용 검사 셋

**파일:** `UnitData.cs`, `TraitGrowthCatalogValidator.cs`, `OwnedUnitSpecResolver.cs`, `UnitEligibilityRules.cs`, `T/EnemyDeathTraitFoundationTests.cs`·`I/EnemyDeathTraitAuthoringTests.cs`에 사례.

**계약(설계 §8):**
- DB: `UnitData.ValidateTraits`가 적 전용 특성을 가진 유닛의 진영 제한이 `EnemyOnly`가 아니면 예외(`Both`도 거절). DB 생성 때 멈춘다.
- 성장 카탈로그: 성장 정의·병과 후보에 적 전용 키가 있으면 `ValidateAvailability`가, 병과 기본 특성이면 `RequireAvailableDefault`가 예외. 대기 검사보다 먼저 봐서 문구가 구분된다. 병과 기본 특성은 성장 표와 함께만 등록되므로(`TraitGrowthDefinitionAdapter.Build`) 설계 L9의 빠짐은 없다.
- `UnitEligibilityRules.Require`: `owner == Player`이고 최종 정의 특성에 적 전용 키가 있으면 거절.
- Phase A에서는 대기 특성을 가진 `UnitDefinition`을 만들 수 없어(`UnitCatalog`이 거절) `Require` 거절 시험은 B.2(공포 등록 뒤)로 옮긴다.

- [x] 시험: DB 유닛 검사(EnemyOnly 수락, PlayerOnly·Both 거절), 카탈로그 기본 특성·성장 정의 거절. `Require` 거절은 B.2.
- [ ] 구현하고 대상 시험 + `BattlePreparationRuleTests`, `TraitGrowthCatalogTests`를 돌린다. `DataBaseAssetTests`는 운영 에셋이 그대로라 통과해야 하며, 사용자 Unity 확인 목록에 넣는다.

**Phase A 변경 영향:**

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| `TraitKey` 셋, DB 입력·대기 표시, 적 전용 검사 | DB 조립·유닛 카탈로그·전투 준비·성장 카탈로그 | Phase A 새 시험, `DeferredCombatFeatureTests`, `LegacyEnhancementRetirementTests`, `IntimidationTraitTests`, `RegenerationTraitTests`(레지스트리 개수 그대로 40), `BattlePreparationRuleTests`, `TraitGrowthCatalogTests`, `I/DataBaseAssetTests` | 전투 계산(공격·이동·턴·기믹)은 바뀐 코드가 없어 다시 확인하지 않는다. |

**Phase A 끝:** 동작 변화 없음을 보이고 멈춘다.

## Phase B — 공포와 명령 끝 확인

### Task B.1: 행동 제한 "긴 쪽" 규칙과 종료 감소 진영

**파일:** `AppliedStatusRules.cs`, 새 `ActionRestrictionTiming.cs`, `CombatChangeEvidenceReader.cs`(낙석 근거 검사), 새 `T/ActionRestrictionOverlapTests.cs`.

**계약:**
- `AppliedStatusRules.Apply`의 행동 제한 갈래: 같은 키면 남은 횟수가 긴 쪽, 같으면 새 것(P1). 무장 해제·발목 꺾기·낙석 속박·공포 모두 같은 규칙이다.
- `internal static Owner? ActionRestrictionTiming.PendingEndSide(CombatTurnFacts turn)`: 진영 턴 중이면 그 진영, `StartingPlayer`/`StartingEnemy`(진영 종료 기믹 창)이면 방금 끝난 진영, 그 밖이면 없음(P2). `Controller` 값은 읽지 않는다.
- 낙석 근거 검사(`CombatChangeEvidenceReader.cs:175-178`)를 "속박이 있고 남은 횟수가 낙석 값 이상"으로 완화한다(M4).
- 낙석 `BindEndCount`·둔화 `ApplySlow`를 `PendingEndSide`로 바꾸는 일은 이 Task에서 하지 않는다. 같은 판단이 맞는지만 시험으로 대조하고, 바꿀지는 범위 밖 제안으로 남긴다.

- [ ] `ActionRestrictionOverlapTests`: 남은 1 위에 2 → 2, 남은 2 위에 1 → 2(출처는 원래 것), 같으면 새 것. `PendingEndSide` 세 국면. 다른 출처의 더 긴 속박이 있을 때 낙석 근거 검사 통과.
- [ ] 기존 `AppliedRestrictionStatusTests.Restrictions_RefreshRatherThanAdd_…`가 단언을 바꾸지 않고 통과하는지 확인한다(설계 §7).
- [ ] 대상 시험 + `AppliedRestrictionStatusTests`, `RockfallGimmickTests`, `I/RockfallPublicationTests`, `TauntTraitTests`를 돌린다.

### Task B.2: 사망 능력 인터페이스와 명령 끝 확인

**파일:** 능력·엔진·평가 파일(파일 지도), 새 `DeathTraitApplication.cs`, `MovementProjection.cs`, `CommandAttackProjection.cs`, 공포 효과 규칙, `DeferredCombatFeatures.cs`, 새 `T/DeathTraitCheckTests.cs`.

시험용 기여 규칙은 레지스트리를 지나지 못하므로, 확인 구조는 공포 특성으로 시험한다. 이 Task 시작에서 공포 특성 묶음(정의·런타임 상태·상태 스냅샷·규칙·효과 규칙·효과 상태 스냅샷·팩토리)을 레지스트리에 등록하고 `Fear`를 대기 목록에서 뺀다. 같은 변경에서 `Registry_CoversEveryNonPendingTraitExactlyOnce` 두 시험의 개수 40 → 41, `DeferredCombatFeatureTests`의 공포 사례를 맞춘다. 공포 효과 규칙은 기여(공포 = 참)만 돌려준다. 실행 갈래는 발동 사건만 남기는 최소판으로 두고, 상태 적용은 B.3에서 채운다.

**계약:**
- `internal interface IEffectDeathTraitRule : ICombatEffectRule { EffectDeathTraitContribution EvaluateDeath(in EffectRuleInput input); }`, 기여 = (자폭 %, 공포 여부). `ICombatEffectEngine.EvaluateDeathTraits(IReadOnlyList<EffectRuleBinding> bindings)`가 모은다. 상태를 바꾸지 않는다.
- `internal static IEnumerable<string> DeathTraitApplication.ResolveSteps(MovementProjection projection)`: `CompletionWork.Build`에서 약한 지반 붕괴 단계 뒤, `SettleTraitKills` 앞에 부르는 단계 `CompletionDeathTraits`.
  - 대상(설계 §5.3): 투영 입력 때 살아 있음 ∧ 지금 죽음 ∧ 이번 실행에서 아직 처리 안 함 ∧ 같은 투영 사건에 그 유닛 `CombatFallEvent` 없음 ∧ 사망 직전 사실의 바인딩에 사망 기여 있음.
  - 사망 직전 사실 = 그 유닛 `Died` 사건의 맞기 전 사실, 없으면 투영 입력 사실(`CommandDeathSettlement.SettleNewDeaths`와 같은 규칙). 이 규칙을 두 곳에 베끼지 않도록 `CommandDeathSettlement`에서 내부 함수 하나로 꺼내 함께 쓴다(TP-5).
  - 사망 위치 = `DeathAnchors`, 없으면 사망 직전 위치.
  - 바퀴: EntityId 순(P15), 한 유닛은 자폭 → 공포(P8). 바퀴 뒤 다시 찾고 새 대상이 없거나 `projection.Terminal`이면 끝(P6).
- 사망 특성 실행 여부는 새 독립 상태가 아니라 생성 입력에서 파생한다(TP-3): 기본 생성자는 `!preview`, 공유 생성자는 새 선택 인자(기본 켬), `CommandAttackProjection.CreateArrivalProjection`이 `!plan.PublicPreview`를 넘긴다. `ForArrival` 자리(배치·턴·토네이도·낙석)는 켬이다.
- 바퀴 사이 사망 경계 정산(깃발·처치 특성)이 필요한지 이 Task에서 시험으로 확인한다. `ResolveDeathBoundary`는 이동 정지까지 하므로 그대로 부르지 않는다. 필요하면 정지 없는 입구를 나누고 그 사실을 Phase 보고에 적는다.

- [ ] A.3에서 옮긴 시험: `UnitEligibilityRules.Require`가 공포를 가진 정의를 플레이어 진영으로 거절하고 적 진영은 받는다(전투 준비·보유 능력치 조회 경로).
- [ ] `DeathTraitCheckTests`(공포 보유 적으로, 발동 사건 수로 판정): 공격·이동·턴 시작 지속 피해 사망이 한 번씩만 실행, 낙사·약한 지반 붕괴 낙사는 제외, 즉사(`Defeat`)는 대상, 공개 공격·이동 미리보기에서는 실행 없음, 승패 확정 뒤 실행 없음, 여러 보유자의 EntityId 순.
- [ ] 구현하고 대상 시험 + `AttackCommandCharacterizationTests`, `KillChainAttackTraitTests`, `CliffFallTests`, `WeakGroundTurnTests`, `PeriodicStatusTurnTests`, `TraitKillRewardTests`를 돌린다. 사망 기여가 없는 기존 전투는 사건 목록이 바뀌지 않아야 한다.

### Task B.3: 공포

**파일:** 공포 특성 묶음, `DeathTraitApplication.cs`(공포 갈래), `MovementProjection.cs`(상태 적용 입구), 새 `T/FearTraitTests.cs`, 새 `I/FearPublicationTests.cs`.

**계약:**
- 기여와 대기 해제는 B.2에서 끝났다. 이 Task는 실행 갈래를 채운다.
- 대상: 사망 칸 주변 8칸에 걸친 살아 있는 필드 캐릭터(진영 무관, 구조물 제외). 8칸 모양은 `CombatHazardAreas`의 화약통 모양을 일반화한 함수 하나를 함께 쓴다(설계 §11).
- 효과: `Disarm`·`Bind` 둘을 출처 = 공포 특성으로, 기간 n = 1 + (`PendingEndSide == 걸린 쪽` ? 1 : 0).
- 상태 적용 입구: `internal void MovementProjection.ApplyStatus(EntityId targetId, AppliedStatus status)`. `AppliedStatusRules.Apply`로 합치고 특성 상태 사건을 남긴다(`ChangeDebuffs`와 같은 꼴). 발동 사건은 기존 `StatusApplied 160`(대상 목록·사망 칸).
- 출처 유닛이 죽어 있어도 제한은 남고, 만료 사건은 죽은 출처 ID로 남는다.

- [ ] `FearTraitTests`: 플레이어 턴 사망 → 플레이어 유닛 n=2·적 유닛 n=1, 진영 종료 기믹 창 사망(R3), 자기 턴 시작 지속 피해 사망(설계 §7 모서리), 더 긴 무장 해제 유지, 자폭으로 죽은 대상 제외(B 시점에는 자폭 없음 → C.1에서 추가), 만료 사건.
- [ ] `FearPublicationTests`: Systems 게시 경로로 Buffer 상태 스냅샷에 두 제한과 출처가 나옴, 이전 게시 불변.
- [ ] 대상 시험 + Task B.1·B.2 시험, `StatusEffectContractTests`, `CombatChangeEvidenceCoverageTests`, `CombatChangeEvidenceVariantTests`, `EnemyHazardForecastTests`, `StageEnemyAiTurnTests`를 돌린다(무장 해제 + 속박이 함께 걸린 적을 AI가 넘기는지).

**Phase B 변경 영향:**

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 행동 제한 겹침 규칙, 완료 정산 단계 하나, 공포 | 행동 제한을 쓰는 모든 특성·낙석, 명령 완료 순서(처치 특성·`Finish`), 미리보기 경로, AI 넘김 | 위 B.1~B.3 목록 | 공격 피해 계산·넉백·처치 연쇄의 내부는 바꾸지 않아 다시 읽을 필요가 없다. 완료 정산 순서 시험(처치 연쇄·사망 정산)만 돌린다. |

**Phase B 끝:** 공포와 확인 구조를 보이고 멈춘다. 바퀴 사이 사망 경계 정산 결론을 보고에 넣는다.

## Phase C — 자폭

### Task C.1: 폭발 피해·귀속·연쇄

**파일:** 자폭 특성 묶음, `DeathTraitApplication.cs`(자폭 갈래), `TraitKillSettlement.cs`, 새 `T/SelfDestructTraitTests.cs`, 새 `I/SelfDestructPublicationTests.cs`.

**계약:**
- 자폭 효과 규칙이 `EvaluateDeath`에서 단계별 % (50/75/110)를 기여한다.
- 양 = `floor(사망 직전 효과 공격력 × % / 100)`. 효과 공격력은 사망 직전 사실로 `CombatEffectiveStatsCalculator`가 계산한다.
- 대상마다 `MovementProjection.ChangeHealthSteps`를 부른다(Q-ED3 A). 출처 = `CombatDamageOrigin(사망 칸, 자폭 유닛, 새 공격 그룹)`. 구조물·장애물·화약통 연쇄도 이 입구가 처리한다.
- 발동 사건 `SelfDestructExploded 390`(대상 칸 목록)을 피해 사건 앞에 남긴다. 맞은 대상이 없어도 남긴다.
- 귀속 유닛이 죽어 있으면 처치 특성 정산을 건너뛴다(P7, `TraitKillSettlement.cs:28-31`의 광폭화 갈래 포함).
- Task 시작에서 자폭 특성 묶음을 레지스트리에 등록하고 `SelfDestruct`를 대기 목록에서 뺀다. 개수 41 → 42와 대기 사례를 맞춘다. `TraitActivationKind.SelfDestructExploded = 390`을 더하고 판독기 갈래·`CombatChangeEvidenceVariantTests` 표본을 함께 넣는다(C.2의 `CorpseDisplaced = 400`도 같은 방식).

- [ ] `SelfDestructTraitTests`: 단계별 피해, 방어 미적용·보호막 적용·지형 엄폐 적용, 구조물 숫자 피해와 화약통 연쇄, 폭발 처치 보상 없음·플레이어 처치 특성 미발동, 자폭 유닛 자신의 사망 귀속은 원래대로, R1(처치 연쇄 수·피격 체력), 자폭 → 다른 자폭 연쇄, 자폭 + 공포 보유 시 자폭으로 죽은 대상은 공포 제외, R7.
- [ ] `SelfDestructPublicationTests`: 게시 경로에서 사건 순서(원 공격 사건 → 폭발 → 영혼·보상)와 처치 보상 없음.
- [ ] 대상 시험 + `ObstacleReactionTests`, `KillChainAttackTraitTests`, `CombatDamageBoundaryTests`, `TraitKillRewardTests`, `I/TraitKillRewardPublicationTests`, `RampageTraitTests`, `BossRoarTests`, `SacrificeTraitTests`를 돌린다.

### Task C.2: 넉백 시체 밀기

**파일:** `AttackKnockbackCalculator.cs`, 새 `T/CorpseDisplacementTests.cs`.

**계약(설계 §6.3):**
- 직접 피해로 죽은 대상 중 사망 기여가 있는 유닛만(Q-ED4 A) 넉백 경로를 계산한다. 시작 = 이미 `DeathAnchors`에 있는 직접 피격 직전 위치, 진입 판정 = `CanEnterUnit`, 칸 효과 정산 없음, 넉백 면역이면 경로 없음.
- 끝 칸으로 `DeathAnchors`를 덮어쓰고, `CorpseDisplaced 400`(시작 → 끝 칸 목록)을 남긴다. 넉백 장부·Moved 사건은 남기지 않는다.

- [ ] `CorpseDisplacementTests`: 끝 칸에서 폭발(R2), 막힌 경로, 면역, 낭떠러지 끝 칸은 낙사 아님, 비보유자 사망 위치 그대로, 영혼도 끝 칸.
- [ ] 대상 시험 + `KnockbackPathTests`, `KnockbackTraitTests`, `KnockbackImmunityPolicyTests`, `AttackKnockbackSnapshotTests`, `PullAttackTests`를 돌린다.

**Phase C 변경 영향:**

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 자폭 갈래, 죽은 귀속 유닛 정산, 넉백의 시체 경로 | 환경 피해 경계·장애물 연쇄·처치 보상·넉백 | C.1·C.2 목록 | 공포 내부, 재구성, 낙석은 바뀌지 않는다. 실행자의 대상 찾기 규칙은 Phase B 시험이 맡는다. |

## Phase D — 재구성 (Lv1·Lv2)

### Task D.1: 가로채기와 상태

**파일:** 새 `ReconstructionStatus.cs`·`ReconstructionRules.cs`, 재구성 특성 묶음, `AppliedStatus.cs`·`StatusEffectKey.cs`(`StatusEffectCatalog`)·`CharacterStatusCompatibility.cs`·`StatusEffectSnapshot.cs`·`StatusEffectReadModel.cs`, `CombatDamageApplication.cs`, `CombatEvent.cs`(캐릭터 사건 허용 종류), `CombatChangeEvidenceReader.cs`(캐릭터 사건·체력 사슬·발동 370), 새 `T/ReconstructionTraitTests.cs`.

**계약(설계 §4.1~§4.3):**
- `ReconstructionStatus(origin, remainingOwnTurnStarts)`, 수명 `AffectedSideTurnStarts`, 분류 버프(P12).
- `ReconstructionRules.CanTrigger(recipient)`(보유·미사용·필드), `Trigger(recipient, level)` → HP 최대, 상태(남은 2), `Used = true`, Lv2 이상 보호막 `floor(최대 × 50 / 100)`(희생 보호막 꼴, P4).
- `Used` 갱신은 광폭화의 `RampageApplication.WithHistory`처럼 `Stats.TraitStatuses`의 재구성 스냅샷을 바꿔 끼운다(Phase 0 확인).
- `ApplySteps` 체력 적용 자리(`CombatDamageApplication.cs:164-172`, 설계의 `:167-175`는 줄 번호가 밀린 것)에서 치명이면 `Hurt` 대신 `Trigger` 결과. 회복량을 `RecordRecovery`로 기록. 사건은 `Reconstructed 49` + `ReconstructionStarted 370`(크기 = 잃은 HP). 스테이지 기록 피해 = 맞기 전 현재 HP.
- 판독기 `HasHealthChain`이 `Reconstructed`를 "손실 = 맞기 전 HP, 회복 = 최대"로 읽는다.
- `DefeatSteps`(즉사)는 가로채지 않는다(Q-ED1 (1) B).
- Task 시작에서 재구성 특성 묶음을 레지스트리에 등록하고 `Reconstruction`을 대기 목록에서 뺀다. 개수 42 → 43과 대기 사례를 맞춘다. `StatusEffectKey.Reconstructing = 19`(`StatusEffectCatalog` 버프, `StatusEffectContractTests` 범위 0~19), `CombatEventKind.Reconstructed = 49`(`CombatChangeEvidenceCoverageTests` 표본), `ReconstructionStarted = 370`(`CombatChangeEvidenceVariantTests` 표본)을 함께 넣는다. D.2는 `ReconstructionEnded = 380`과 거절 이유 52를 같은 방식으로 넣는다. Phase E 전까지 Lv3는 부활 넉백 없이 동작한다. 운영 데이터에 붙이지 않으므로(TP-8) 이 사이에 드러나지 않는다.

- [ ] `ReconstructionTraitTests`: R4, 전투당 1회, Lv1 보호막 없음·Lv2 50% 내림, 위스키 미발동, 사망 폐기·처치 연쇄·처치 보상 없음, 즉사는 진짜 사망, 재구성 중 치명 → 사망 + 자폭·공포 발동(B·C 결합 사례), 공개 미리보기에 재구성 반영(R6).
- [ ] 대상 시험 + `CombatDamageBoundaryTests`, `AttackCommandCharacterizationTests`, `CombatChangeEvidenceCoverageTests`, `CombatChangeEvidenceVariantTests`, `StatusEffectContractTests`, `ShieldStatusPolicyTests`, `TraitShieldTests`, `SacrificeTraitTests`, `KillChainAttackTraitTests`를 돌린다.

### Task D.2: 행동 입장과 부활

**파일:** `CombatActionAdmission.cs`, `StageHazardForecast.cs`, 새 `ReconstructionTurnApplication.cs`, `CombatTurnCalculator.cs`, `CombatChangeEvidenceReader.cs`(발동 380), 새 `T/ReconstructionReviveTests.cs`, 새 `I/ReconstructionPublicationTests.cs`.

**계약(설계 §4.1·§4.4):**
- `IsAttackRestricted`·`IsMovementRestricted`가 재구성 상태를 보고, 거절 이유는 `Reconstructing`(52). 낙석 예보(`StageHazardForecast:247,251`)는 행동 입장 함수를 쓴다.
- `ReconstructionTurnApplication.BeginSide(projection, side)`를 `BeginControllerTurn`에서 `ForArrival` 뒤, `PeriodicStatusApplication.BeginSide` 앞에 부른다(P3). 남은 횟수 1 감소, 0이면 상태 지우고 `ReconstructionEnded 380`.
- 재구성 중 상태·아이템은 비우지 않는다(P5). 강제 이동은 그대로 받는다.

- [ ] `ReconstructionReviveTests`: 플레이어 턴 발동 → 적 턴 불가 → 다음 적 턴 시작에 부활하고 바로 행동, 자기 턴 중 발동(반사·지속 피해) 셈, 재구성 중 넉백·끌어오기 받음, 낭떠러지로 밀리면 진짜 사망, 보호막은 부활 뒤에도 남음.
- [ ] `ReconstructionPublicationTests`: Buffer에 재구성 상태·`Used`가 나오고 부활 뒤 사라짐.
- [ ] 대상 시험 + `EnemyHazardForecastTests`, `StageEnemyAiTurnTests`, `EnemyAiBaselineTests`, `PeriodicStatusTurnTests`, `TauntTraitTests`, `RegenerationTurnTests`, `PullAttackTests`를 돌린다.

**Phase D 변경 영향:**

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 피해 경계의 치명 갈래, 재구성 상태, 행동 입장, 턴 시작 한 단계 | 모든 피해 결과의 체력 검사·판독기 체력 사슬, 행동 입장을 쓰는 AI·포효·예보, 턴 시작 순서 | D.1·D.2 목록 | 자폭·공포 실행자는 "새로 죽은 유닛"만 보므로 고치지 않는다. 결합 사례 하나(D.1)만 돌린다. |

## Phase E — 부활 넉백 (Lv3)

### Task E.1: 강제 이동 칸 정산 꺼내기 (동작 보존)

> **바뀜(2026-11-10 사용자 결정):** 이동 투영 단위 강제 이동(`MovementEnvironmentCalculator.ResolveForcedMovementSteps`, 타일 효과 경로)이 이미 있고 칸 효과가 공격 넉백과 실질적으로 같아서, 공격 넉백에서 꺼내지 않고 그 경로를 8방향 걸음·이동 원인 입구로 넓혀 쓴다. 공격 넉백은 면역 판정만 함수 하나로 꺼낸다. 아래 원래 계약과 특성화 사례는 하지 않는다. 실행 기록은 §9 Phase E 실행.

**파일:** `AttackForcedMovement.cs`, `MovementProjection.cs`, 새 `T/ForcedMovementEntryTests.cs`.

**계약:** 칸마다 정산(낙사·가시·지뢰·오브젝트·약한 지반·토네이도)을 이동 투영 입구 `internal IEnumerable<string> MovementProjection.ResolveForcedStepSteps(...)`(이름은 Task 0.1에서 기존 명명에 맞춰 확정)로 옮기고, 공격 넉백이 그것을 부른다. 공격 계획(`CommandAttackPlan`) 없이 부를 수 있어야 한다.

- [ ] 옮기기 전에 공격 넉백 특성화 사례를 하나 더한다: 한 공격에서 두 대상이 각각 가시·낭떠러지 칸으로 밀리는 경우의 사건 목록.
- [ ] 옮기고 `ForcedMovementEntryTests` + `KnockbackPathTests`, `KnockbackTraitTests`, `AttackKnockbackSnapshotTests`, `CliffFallTests`, `WeakGroundMovementTests`, `TornadoRuntimePathTests`, `PullTerrainTests`, `PullObstacleTests`를 돌린다. 사건 목록이 바뀌지 않아야 한다.

### Task E.2: Lv3 부활 넉백

**파일:** `ReconstructionTurnApplication.cs`, 새 `T/ReconstructionReviveKnockbackTests.cs`.

**계약(P13):** 부활 순간 주변 8칸 캐릭터를 EntityId 순으로 바깥 8방향 1칸. 면역 판정은 공격 넉백과 같은 함수, 칸 효과는 E.1 입구, 장애물·구조물은 밀지 않음, 승패 확정 시 멈춤. 사건은 강제 이동(원인 = 재구성) + `KnockbackApplied`.

- [ ] 시험: Lv2는 넉백 없음, Lv3 8방향, 면역, 막힌 칸, 밀린 유닛이 낭떠러지·지뢰로 죽으면 자폭·공포 확인을 탐(턴 시작 투영 완료 정산), 승패 확정.
- [ ] 대상 시험 + E.1 목록을 돌린다.

**Phase E 변경 영향:**

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 강제 이동 칸 정산 위치, 부활 넉백 | 공격 넉백·끌어오기·토네이도 착지가 쓰는 칸 효과 | E.1·E.2 목록 | 재구성 가로채기·자폭·공포 내부. 칸 효과를 공유하는 것은 게임 규칙상 필요한 연결이라 그 시험은 돌린다. |

## Phase F — 낙석 연결·전체 검증·인계

### Task F.1: 낙석 연결 (Q-ED2 A)

**파일:** `RockfallGimmickRule.cs`, `CombatChangeEvidenceReader.cs`(`ValidateRockfallRecipients`), 새 `T/RockfallDeathTraitTests.cs`.

- [ ] **먼저 특성화:** 지금 코드로 낙석 사망·생존이 섞인 경우의 사건 목록·게시 상태를 고정하는 사례를 `RockfallDeathTraitTests`에 둔다(TP-2).
- [ ] `Impact` 끝을 `MovementProjection.ForArrival(input.WithActor(EntityId.Empty), projected, events, state).Complete()`로 바꾼다. 사망 위치 사전을 넘길 수 있으면 넘긴다.
- [ ] 특성화 사례가 그대로인지 본다. 다르면(처치 특성 정산·사냥꾼 맞춤 때문) 차이를 적고 사용자 결정 뒤 진행한다.
- [ ] 수신자 반복에서 재구성 가로채기, 판독기 `:166-170`에 `Reconstructed` 허용.
- [ ] 시험: R5, 낙석 사망 자폭·공포가 모든 수신자 사건 뒤, 재구성 적 낙석 생존 + 속박, 기차 사망은 확인을 타지 않음.
- [ ] 대상 시험 + `RockfallGimmickTests`, `I/RockfallPublicationTests`, `TornadoGimmickResolutionTests`, `I/TornadoGimmickPublicationTests`, `EnemyHazardForecastTests`를 돌린다.

### Task F.2: 합친 시험·전체 회귀·변경 독립성 검토

**파일:** 새 `I/EnemyDeathTraitsCombinedTests.cs`, 이 계획의 실행 기록, 설계 문서 구현 상태.

- [ ] `EnemyDeathTraitsCombinedTests.MixedRoundTrip`: 재구성 Lv3·자폭·공포 적이 섞인 전투에서 공격 → 재구성 → 부활 넉백 → 낙석 사망 연쇄 → 승패, 게시 경로 전체와 이전 게시 불변.
- [ ] 두 어셈블리 전체 실행을 요청하고 Phase 0 기준과 비교한다.
- [ ] §7의 사례를 실제 diff·호출자로 다시 평가한다. 입력·출력, 실패 동작, 상태 소유자, 순서, 수명을 확인하고 범위 밖 발견은 기록만 한다.
- [ ] 정적 확인을 한다.

### Task F.3: View 인계 절과 기획 공유

**파일:** Core 전달 문서(`docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md`)에 새 절. 그 문서를 고치면 제목도 최신 주제·날짜로 고친다. 기획 공유는 채팅 문구로 낸다.

- [ ] 설계 §9 표(키·상태·사건·발동·시체 밀기·사건 순서·공포 표시·만료·거절 이유)와 운영 활성화 순서: View 등록 → Unity 확인 → 기획이 적 유닛에 특성·단계 지정 → `DataBaseAssetTests`.
- [ ] 기획 공유 문구: 자폭이 주변 적을 죽이면 플레이어는 처치 보상을 잃음, 낙석 사망도 자폭·공포 발동, 마무리 사격 즉사는 재구성 불가.
- [ ] Core 검증·미실행 확인·사용자 Unity 확인을 나눠 보고한다. **Phase F 검토 지점에서 멈춘다.** 다른 사람에게 보내는 일은 하지 않는다.

## 7. 변경 독립성: 대표 변경 사례

설계 §1의 실제 요구에서 고른 사례다. "구현 뒤 예상"은 코드가 생기기 전까지 예상이다.

| 변경 사례 | 지금(또는 2판) 부담과 원인 | 판단과 조치 | 구현 뒤 알아야 하는 것 | 남는 검증 |
|---|---|---|---|---|
| 자폭 비율·공포 기간 조정 | 2판은 큐 넣는 곳 셋·정산점 넷·넉백 대기를 알아야 했다. 구현이 만든 결합이다. | 큐를 없애고 능력 기여 + 실행자 하나로(B.2). 수치는 `EnemyDeathTraitLevels` 한 곳. | 단계 표와 실행자 계약(대상·순서·위치). 처치 연쇄·화약통·낙석 내부는 몰라도 된다. | 공포는 행동 제한 겹침을 무장 해제·발목 꺾기·낙석 속박과 공유하므로 그 시험은 필요한 연결이다. |
| 재구성 부활 시점(P3) 변경 | 부활은 턴 시작 순서에 끼어 있다. 게임 규칙상 필요한 연결이다. | 유지. 순서 계약을 `BeginControllerTurn` 한 곳에 둔다(D.2). | 턴 시작 순서(특성 수명 → 투영 → 부활 → 지속 피해)와 `ReconstructionTurnApplication`. 자폭·공포는 몰라도 된다. | 행동 입장·AI 넘김·지속 피해 순서 시험. |
| 낙석 피해 규칙 변경 | 2판은 낙석이 자기 정산을 하고 있어, 사망 특성을 붙이려면 낙석 내부와 근거 검사 순서를 다시 짜야 했다. | 낙석을 턴 시작과 같은 `ForArrival(...).Complete()` 계약으로 끝냄(F.1). | 낙석 규칙과 "이동 투영 완료로 끝남" 계약. 자폭·공포가 어떻게 실행되는지는 몰라도 된다. | 낙석 근거 검사와 사망 특성 연동 사례 하나. 사망을 만드는 전이라 필요한 연결이다. |
| 공격 넉백 칸 효과 변경 | Phase E 전에는 칸 효과가 공격 계획에 묶여 있어 부활 넉백이 쓰려면 공격 내부를 알아야 한다. | 칸마다 정산을 이동 투영 입구로 꺼냄(E.1, 동작 보존). | 입구의 입력·출력·정지 조건. 부활 넉백은 공격 계획을 몰라도 된다. | 공격 넉백·끌어오기·토네이도·부활 넉백이 같은 칸 규칙을 공유하므로 함께 돌린다. |
| 적 전용 제한 해제 | 규칙 본문은 진영 중립이다. | 유지. 원본 `TraitOwnerPolicy` + 검사 셋. | 정책 함수와 검사 자리 셋. 특성 본문은 고치지 않는다. | 적 전용 검사 시험만. |

보고는 "변경 사례 → 부담·근거 → 판단 → 조치 → 전후 차이 → 보존·검증 → 확인 수준(예상 / 코드 확인 / 실행 검증 / 미확인)" 순서로 쓴다. 파일·클래스·인터페이스 수는 판단 기준이 아니다. 새 함수가 따라갈 경로만 늘리고 알아야 할 것을 줄이지 못하면 기존 구조를 두고 이유를 적는다.

## 8. 계획에서 정한 것 (확인 대상)

| 번호 | 내용 |
|---|---|
| TP-1 | 거절 이유 값은 이동·공격 모두 52(50·51은 이미 씀). |
| TP-2 | 낙석 `Finish` → `Complete` 전에 특성화 사례를 먼저 고정하고, 결과가 달라지면 멈추고 묻는다. |
| TP-3 | 사망 특성 실행 여부는 새 독립 플래그가 아니라 생성 입력(`preview`, `plan.PublicPreview`)에서 파생한다. AI 평가는 이미 미리보기 경로라 따로 끄지 않는다(Task 0.1에서 확인). |
| TP-4 | 자폭·공포도 상태·스냅샷 파일을 포함한 전체 특성 묶음을 만든다. 상태 없는 처치 연쇄가 선례다(Phase 0 확인, 2026-11-10). |
| TP-5 | 사망 직전 사실 찾기는 `CommandDeathSettlement`의 규칙을 내부 함수로 꺼내 영혼 정산과 사망 특성이 함께 쓴다(같은 사실의 두 번째 사본을 만들지 않음). |
| TP-6 | 대기 해제 시점은 그 특성의 동작을 구현하는 Task 시작이다(공포 B.2, 자폭 C.1, 재구성 D.1). 레지스트리가 대기 특성 생성을 막아서다. 확인 구조(B.2)는 시험용 기여 규칙 대신 공포 특성으로 시험한다. 대기 검사를 끄는 시험 전용 우회는 운영 코드에 두지 않는다. |
| TP-7 | `DeferredCombatFeatureTests`는 대기 목록을 단언하지 않아 기존 기대값을 바꾸지 않는다. A.2에서 세 키 대기 사례만 더하고, 대기를 풀 때 그 사례를 "사용 가능"으로 바꾼다(작성자 본인). |
| TP-8 | 운영 적 유닛 에셋에는 특성을 붙이지 않는다(View 대응 뒤 결정). |
| TP-9 | `BindEndCount`·`ApplySlow`를 `PendingEndSide`로 바꾸는 일은 범위 밖 제안으로 남긴다(B.1은 대조 시험만). |
| TP-10 | 사망 특성 효과 규칙의 AI 평가 선언은 `IEffectAssessmentHarmRule`(추가 즉시 피해 없음)이다. AI는 사망 특성을 모형화하지 않는다(설계 §11). 커버리지 공백을 선언하면 보유 적이 있는 동안 AI 이동 투영이 막힌다(Phase B에서 정함). |

## 9. 실행 기록과 자체 점검

| Phase | 상태 | 증거 / 남은 일 |
|---|---|---|
| 계획 | 완료(2026-11-10) | 설계 3판·관련 코드·구조 지침 확인. 문서만 바꿈. |
| 0 | 완료 | 기준 6,508 중 실패 21(목록은 아래 Phase 0 실행). Phase A 범위 변경 결정. |
| A | 완료 | 전체 6,529(새 21 모두 통과), 실패는 기준 21 그대로. |
| B | 완료 | 전체 6,545. 공포 시험 15개 통과, 기준 21 그대로, 새 실패 1(레지스트리 개수 시험 — C에서 맞춤). |
| C | 완료 | 전체 6,557. 자폭 시험 11개 통과, 기준 21 그대로, 새 실패 1(P7 범위 — 좁혀서 고침, D 실행에서 다시 확인). |
| D | 완료 | 전체 6,570. 재구성 시험 12개 통과, 실패는 기준 21 그대로(C의 새 실패도 풀림). |
| E | 완료 | 전체 6,576. 부활 넉백 시험 6개 통과, 실패는 기준 21 그대로. E.1은 사용자 결정으로 바꿈. |
| F | 완료(검토 지점) | 전체 6,585. 새 시험 모두 통과, 실패는 기준 21 그대로. 아래 Phase F 실행. |

자체 점검:
- 설계 결정 Q-ED1~Q-ED8과 P1~P17이 Task에 대응한다. Q-ED1 → B.2·D.1, Q-ED2 → F.1, Q-ED3 → C.1, Q-ED4 → C.2, Q-ED6·Q-ED8 → A.1·D.1, Q-ED7 → B.2·D.1. P1·P2 → B.1, P3·P5 → D.2, P4·P12 → D.1, P6·P8·P9·P15·P16 → B.2, P7 → C.1, P10 → A.3, P11 → B.3·C.1, P13 → E.2, P14 → 진입 거절 유지(작업 없음), P17 → F.1.
- 검토 초점 R1~R7마다 담당 시험이 있다.
- 새 계층은 능력 인터페이스 하나와 실행자 하나다. 둘 다 처치 연쇄의 기존 꼴이고, 지금 요구(자폭·공포) 둘이 쓴다.
- 필요한 검증은 적었고, 실행했다고 주장하지 않는다. 이 계획은 구현 권한을 주지 않는다.

### Phase 0 실행 — 2026-11-10

**작업 트리:** 브랜치 `feature/Cheon/Core/NewCombatSytem`, HEAD `1e36c1854`(18:55 병합). 바뀐 것은 이 작업 문서 둘(설계 수정, 계획 새 파일)뿐이다.

**AI 작업:** 설계 §11이 "커밋 전"이라 했던 적 AI 파일(`StageHazardForecast`, `EnemyHazardForecastTests` 등)은 `fd9784242`에 커밋돼 있다. Phase D의 착수 조건 하나가 풀렸다.

**경로와 순서(코드 확인):**

| 경로 | 순서 |
|---|---|
| 공격 | `CombatAttackCalculator` → `AttackDeliveryExecutor`: 직접 피해 → 도착 투영 생성(`CreateArrivalProjection`) → 넉백 → 가시밭 → 행동 사용 → 피격 체력 고정(`CaptureHitHealth`) → 강제 도착 → 처치 연쇄(`!plan.PublicPreview`일 때만) → 사냥꾼 마무리 → 공격 결과 조립 → 공격 사건 삽입 → `arrival.BeginComplete`(`CombatAttackCalculator.cs:248`). 포효 반응도 같은 길이고 공격 결과만 넘기지 않는다. |
| 명령 완료 | `CompletionWork.Build`(`MovementProjection.cs:765-816`): 약한 지반 붕괴 → 처치 특성 정산 → 사냥꾼 맞춤 → 최종 관찰 → 구조물 → `Finish` → 봉인. `CompletionDeathTraits`는 첫째와 둘째 사이에 들어간다. |
| 턴 시작 | `BeginControllerTurn`: 특성 수명 → `ForArrival` → 지속 피해 → `Complete` → (적이면) 넥서스 증원 `Merge`. |
| 진영 종료 | `EndPlayerTurn`/`EndEnemyPhase`: 종료 사건 → `ForArrival(...).Complete` → `ResolveEndGimmick`(기믹 단계 `Merge`) → `CleanupControllerTurn`. 진영 종료 기믹 창은 국면 `StartingEnemy`/`StartingPlayer`. |
| 낙석 | `StageGimmickComposition.TryAdvance` → `RockfallGimmickRule.Impact`(수신자마다 `Hurt` → 체력 사건 → 생존자 속박 사건) → `CombatCommandCalculator.Finish` 직접 → `Merge`. 기차도 `Finish` 직접. 토네이도는 `ForArrival`이라 완료 정산을 지난다. |
| 전투 진입 | `EnterCombat`: `ForArrival` → 적 재배치 → `Complete`. |

**이동 투영 생성 자리와 미리보기 여부(TP-3 확인):**

| 자리 | 생성 | 미리보기 판단 | 사망 특성 |
|---|---|---|---|
| `CombatMovementCalculator.cs:193` | 기본 생성자 | `preview` 인자. 이동 미리보기 `:26`, 이동 문맥 `MovementContextCalculator.cs:30`, AI 평가 `CombatActionAssessment.cs:247`은 참, 실제 이동 `:108`은 거짓 | `!preview` |
| `CommandAttackProjection.cs:210` | 공유 생성자 | `plan.PublicPreview`. 공개 미리보기 `CombatCommandCalculator.cs:120`(AI 공격 평가 `BeginPreview` 포함)만 참 | `!plan.PublicPreview`를 새 선택 인자로 |
| `CombatPlacementCalculator.cs:48`, `CombatTurnCalculator.cs:44,94,139,170`, `TornadoGimmickRule.cs:87` | `ForArrival` | 실제 명령만(배치 미리보기는 투영을 만들지 않음) | 켬 |

AI 평가는 이미 미리보기 경로만 써서 따로 끌 곳이 없다.

**특성 등록 계약(TP-4 확인):** 상태 없는 처치 연쇄도 Spec·상태 스냅샷·정의·런타임 상태·규칙·효과 규칙·효과 상태 스냅샷을 모두 갖고, 팩토리는 공용 `TraitFactories.cs`에, 등록은 `TraitRegistry`(`:209-219`)에 있다. 그 밖에 손이 가는 곳: `TraitEffectProvider`(`:74` 꼴), `CombatCommandEffects`(상태 스냅샷 → 효과 상태 스냅샷, `:179` 꼴), `TraitSpecAdapter`, `MainSpecializationDefinitionSnapshot`(모르는 정의면 예외인 switch, `:128`).

**재구성 상태 갱신 선례:** 희생은 피해 경계 안에서 상태 효과(보호막)만 바꾸고 특성 상태는 바꾸지 않는다. 특성 상태를 바꾸는 선례는 광폭화 `RampageApplication.WithHistory`(`Stats.TraitStatuses` 바꿔 끼우기)다. 재구성 `Used`는 이 꼴을 따른다.

**레지스트리 대기 검사:** `TraitRegistry.GetRequiredRegistration`(`:125`)이 대기 중 특성에서 예외를 낸다(TP-6의 근거).

**기존 시험 영향:** 기대값이 바뀔 수 있는 시험 여덟(`AppliedRestrictionStatusTests`, `StatusEffectContractTests`, `RockfallGimmickTests`, `I/RockfallPublicationTests`, `CombatChangeEvidenceCoverageTests`, `CombatChangeEvidenceVariantTests`, `ObstacleReactionTests`, `KillChainAttackTraitTests`)은 모두 GwonCheonmyeong 작성이다. `DeferredCombatFeatureTests`는 대기 목록을 단언하지 않는다(TP-7).

**기준 시험:** 편집기가 닫혀 있어 Claude가 배치 모드로 두 어셈블리를 돌렸다(2026-11-10 19:28~19:35, HEAD `1e36c1854`). 전체 6,508 = 통과 6,476 · 실패 21 · 건너뜀 11. Core 5,454(실패 1), CoreIntegration 1,054(실패 20). 실패 목록은 18:44 Test Runner 실행과 같다. 배치 실행이 바꾼 FMOD 카탈로그·글꼴 에셋 둘은 되돌렸다. 기준 실패(이후 Phase는 이것과 비교):

- Core: `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`
- CoreIntegration: `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)`, `CombatStructureInspectionTests.UnitInfoPrefab_HidesMissingStructureFieldsAndRestoresCharacterFields`, `DataBaseAssetTests` 7개(`CategoryDefaultBattle_ProductionAssignmentsAndGrowthPools`, `CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Rider,Leap)`·`(Heavy,SteadfastWill)`, `GrowthProduction_AkimboKeepsPrePresentationTestNativeTrait`, `GrowthProduction_CannonKeepsPreviouslyLegalTraitWithoutReopeningIt(Taunt)`·`(Acceleration)`, `RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases`), `DeckMainChangePolicyTests` 8개, `DeckSlotInfoPanelTests.Abilities_ShowAllContentsInOrderAndScrollToLast(1)`, `RunGoldFacilityTests.ProductionRestVisit_EntersSelectsAndCompletes` 2개.

**Phase A 범위 변경(2026-11-10 사용자 결정, 권장안):** Phase A에서 열거 값·레지스트리 등록을 모두 넣으면 기존 "빠짐없음" 시험 다섯이 깨진다.
- `IntimidationTraitTests`·`RegenerationTraitTests.Registry_CoversEveryNonPendingTraitExactlyOnce`: 등록된 키 = 대기 아닌 키(개수 40).
- `CombatChangeEvidenceCoverageTests`·`CombatChangeEvidenceVariantTests`: 모든 사건·발동 종류에 표본.
- `StatusEffectContractTests.Catalog_ReservesStableUniqueKeysAndSeparatesClassification`: 상태 키 0~18.

그래서 Phase A는 `TraitKey` 셋·`TraitOwnerPolicy`·단계 표·DB 입력(Authoring·Data·Spec·어댑터)·대기 표시·적 전용 검사만 한다. 상태 키·사건·발동·거절 이유 값과 레지스트리 등록(정의·런타임 상태·규칙·효과 규칙·팩토리)은 그 특성의 대기를 푸는 Task(B.2·C.1·D.1)에서 시험 표본·기대값과 함께 넣는다. A.1·A.2·B.2·C.1·D.1 본문을 이에 맞춰 고쳤다.

**Phase 0 보고 시점:** 사용자 지시로 Phase 0 보고를 기다리지 않고 기준 실행 중에 Phase A를 시작했다(컴파일이 끝난 뒤에 편집 시작, 시험 중 전체 새로고침 호출 없음 확인).

### Phase A 실행 — 2026-11-10

- 바꾼 파일: `TraitKey.cs`, `DeferredCombatFeatures.cs`, `TraitSpecAdapter.cs`, `MainSpecializationDefinitionSnapshot.cs`, `UnitData.cs`, `TraitGrowthCatalogValidator.cs`, `OwnedUnitSpecResolver.cs`, `UnitEligibilityRules.cs`.
- 새 파일(+ `.meta`): `TraitOwnerPolicy.cs`, `EnemyDeathTraitLevels.cs`, `{Reconstruction,SelfDestruct,Fear}TraitSpec.cs`, `{Reconstruction,SelfDestruct,Fear}Trait{Data,Authoring}.cs`, `T/EnemyDeathTraitFoundationTests.cs`, `I/EnemyDeathTraitAuthoringTests.cs`.
- 계획과 다른 점: 단계 표를 Contracts에 둠, DB 검사를 `UnitData.ValidateTraits`에 둠, 병과 기본 특성 검사를 `RequireAvailableDefault`에 둠, `Require` 거절 시험은 B.2로(파일 지도·A.3에 반영).
- 시험(배치, 19:48~): 전체 6,529 = 통과 6,497 · 실패 21 · 건너뜀 11. 새 시험 21개 모두 통과, 실패 목록은 기준과 같다. 배치 실행 부수 파일(FMOD·글꼴)은 되돌렸다.
- 사용자 지시(2026-11-10): Phase마다 시험을 기다리지 않는다. Phase 끝에 백그라운드로 전체 실행을 걸고 다음 Phase를 진행하며 결과는 도중에 확인한다. Phase F에서 한 번 더 전체 확인한다.

### Phase B 실행 — 2026-11-10

- 바꾼 파일: `AppliedStatusRules.cs`(행동 제한 긴 쪽), `CombatChangeEvidenceReader.cs`(낙석 근거 완화), `CombatEffectCapabilities.cs`, `ICombatEffectEngine.cs`, `CombatEffectEngine.cs`, `TraitRegistry.cs`, `TraitEffectProvider.cs`, `CombatCommandEffects.cs`, `DeferredCombatFeatures.cs`(공포 대기 해제), `MovementProjection.cs`(스위치·접근자·`ApplyStatus`·완료 단계), `CommandAttackProjection.cs`, `CommandDeathSettlement.cs`(`PreDeathFact`), `CombatHazardAreas.cs`(`SurroundingCells`), `T/IntimidationTraitTests.cs`·`T/RegenerationTraitTests.cs`(개수 40 → 41), `T/EnemyDeathTraitFoundationTests.cs`(공포 대기 사례 갱신).
- 새 파일(+ `.meta`): `ActionRestrictionTiming.cs`, `EffectDeathTraitContribution.cs`, `EffectDeathTraitEvaluation.cs`, `DeathTraitApplication.cs`, 공포 특성 묶음(`FearTraitDefinition`·`FearRuntimeState`·`FearTraitStatusSnapshot`·`FearTraitRule`·`FearEffectRule`·`FearTraitFactories`), `T/FearTraitTests.cs`.
- 계획에서 정한 대로 한 것: 확인 지점은 완료 정산 한 곳, 대상은 사실 비교, 사망 직전 사실은 `CommandDeathSettlement.PreDeathFact` 하나를 영혼 정산과 함께 씀(TP-5), 미리보기·이동 문맥·AI 평가는 끔(TP-3).
- 계획과 다른 점·구현 중 정한 것:
  - 사망 특성 실행 여부는 생성자 인자가 아니라 `MovementProjection.DeathTraitsEnabled` 속성이다(`TornadoObserver`와 같은 꼴, 공유 생성자 호출자를 바꾸지 않음). 기본 생성자가 `!preview`로, 공격 도착 투영은 `CreateArrivalProjection`이 `!plan.PublicPreview`로 정한다.
  - 실행할 것이 없으면 완료 정산 단계를 내지 않는다(기존 명령의 단계 순서 그대로).
  - 특성이 하나도 없는 사망자는 바인딩을 만들지 않고 건너뛴다(기여는 지금 특성만 낸다, 주석에 걷는 조건 적음).
  - 공포 발동 사건은 실제로 상태가 바뀐 대상만 싣는다. 판독기가 대상마다 공포 출처 상태 변화를 요구하기 때문이다. 바뀐 대상이 없으면 발동 사건을 내지 않는다.
  - 공포 효과 규칙은 `IEffectAssessmentHarmRule`(추가 즉시 피해 없음)을 선언한다(TP-10). 처치 연쇄처럼 커버리지 공백을 선언하면 공포 적이 있는 동안 AI 이동 투영이 막힌다.
  - 낙석 근거 완화는 "충돌 전부터 있던 다른 출처 속박이 출처·남은 횟수 그대로 남음"으로 판정한다. 판독기는 낙석 수치(`BindEndCount`)를 모르므로 설계의 "남은 횟수 ≥ 낙석 값"을 이렇게 바꿨다.
  - `I/FearPublicationTests`는 설정 전투를 꾸리는 비용이 커서 Phase F의 합친 게시 시험으로 옮겼다. 낙사 제외 시험은 넉백 고정물이 생기는 C.2에서 함께 쓴다.
- 시험(배치, 20:01~): 전체 6,545 = 통과 6,512 · 실패 22. `FearTraitTests` 15개 모두 통과, 기준 실패 21개 그대로. 새 실패 1: `LegacyEnhancementRetirementTests.SurvivingRegistry_PreservesAllNewAndDeferredIdentities`(사용 가능 등록 37 → 38). 계획이 놓친 세 번째 레지스트리 개수 시험이다(작성자 본인). Phase C에서 자폭 등록과 함께 39·29로 맞췄다.

### Phase C 실행 — 2026-11-10

- 바꾼 파일: `TraitRegistry.cs`, `TraitEffectProvider.cs`, `CombatCommandEffects.cs`, `DeferredCombatFeatures.cs`(자폭 대기 해제), `DeathTraitApplication.cs`(폭발 갈래), `MovementProjection.cs`(`CurrentStructures`), `TraitActivationKind.cs`(390·400), `CombatChangeEvidenceReader.cs`(390·400 갈래), `TraitKillSettlement.cs`(죽은 귀속 유닛 건너뜀, P7), `AttackKnockbackCalculator.cs`(시체 밀기), `T/CombatChangeEvidenceVariantTests.cs`(표본 둘), `T/IntimidationTraitTests.cs`·`T/RegenerationTraitTests.cs`(41 → 42), `T/LegacyEnhancementRetirementTests.cs`(37 → 39, 27 → 29), `T/EnemyDeathTraitFoundationTests.cs`(대기 사례 갱신).
- 새 파일(+ `.meta`): 자폭 특성 묶음(`SelfDestructTraitDefinition`·`SelfDestructRuntimeState`·`SelfDestructTraitStatusSnapshot`·`SelfDestructTraitRule`·`SelfDestructEffectRule`·`SelfDestructTraitFactories`), `T/SelfDestructTraitTests.cs`.
- 구현 중 정한 것:
  - 폭발은 한 공격 그룹 출처(사망 칸, 자폭 유닛)로 대상마다 `ChangeHealthSteps`를 부른다. 대상은 살아 있는 캐릭터·구조물, EntityId 순.
  - 폭발 발동 사건은 맞은 대상이 없어도 남긴다. 대상 목록은 실제로 바뀐 개체, 칸 목록은 주변 8칸, 선택 칸은 사망 칸, 크기는 폭발 피해다. 판독기는 대상마다 체력·특성 상태·구조물 사건 중 하나를 요구하고, 대상이 없으면 관찰로 읽는다.
  - 자폭의 AI 평가 선언도 추가 즉시 피해 없음이다(TP-10).
  - 시체 밀기는 공개 미리보기에서 하지 않는다. 시작 칸은 직접 피해 단계가 쓴 사망 위치, 방향·면역은 사망 직전 사실로 판단한다. 한 칸도 못 가면 사건이 없다. 발동 사건의 특성 키는 자폭이 있으면 자폭, 아니면 공포다.
  - 죽은 귀속 유닛은 광폭화 처치 보상을 받지 않는다(P7). 피의 부름·사기 진작·사냥꾼 대상 처리는 그대로 둔다(시험 결과로 좁힘, 아래).
- 시험 범위: `SelfDestructTraitTests` — 단계 비율 3, 폭발 처치 귀속, 연쇄 폭발, 공개 미리보기, 승패 확정 뒤 남은 사망 특성 중단(R7), 낙사 제외와 같은 투영의 피해 사망 발동, 넉백 끝 칸 폭발(R2)·막힌 경로·비보유자. 처치 연쇄와 함께 쓰는 사례(R1)는 처치 연쇄의 무작위 대상 때문에 Phase F 합친 시험으로 옮겼다.
- 시험(배치): 전체 6,557 = 통과 6,524 · 실패 22 · 건너뜀 11. `SelfDestructTraitTests` 11개 모두 통과, 기준 실패 21개 그대로.
- 새 실패 1: `TraitKillRewardTests.ThornHeartEnemyAdvance_AwardsActualReflectorExactlyOnce(True)`(기대 4, 실제 0). 처음에는 P7을 "죽은 귀속 유닛은 처치 특성 보상을 모두 받지 않음"으로 넓게 넣어서, 반사 피해로 서로 죽는 경우의 사기 진작까지 막았다. 설계 P7의 뜻(죽은 유닛이 처치 보상으로 다시 강해지지 않음)은 광폭화에 해당하므로, 죽은 귀속 유닛 확인을 광폭화 갈래에만 남기고 피의 부름·사기 진작 갈래는 원래 조건(`!credited.HasValue`)으로 되돌렸다. 다시 확인은 Phase D 실행에서 한다.

### Phase D 실행 — 2026-11-10

- 바꾼 파일: `StatusEffectKey.cs`(`Reconstructing = 19`, 버프), `StatusEffectSnapshot.cs`·`StatusEffectReadModel.cs`(재구성 읽기 모델), `AppliedStatusRules.cs`(재구성 상태는 하나만), `TraitRegistry.cs`·`TraitEffectProvider.cs`·`CombatCommandEffects.cs`, `DeferredCombatFeatures.cs`(대기 목록이 비어 `_ => false`), `CombatEvent.cs`(`Reconstructed = 49`, 캐릭터 사건 검사), `TraitActivationKind.cs`(370·380), `CombatChangeEvidenceReader.cs`(캐릭터 사건·체력 사슬·370·380), `CombatDamageApplication.cs`(치명 갈래), `CombatActionAdmission.cs`·`CombatAttackQueries.cs`(거절 이유), `MovementFailureReason.cs`·`AttackFailureReason.cs`(`Reconstructing = 52`), `StageHazardForecast.cs`, `CombatTurnCalculator.cs`(턴 시작 한 줄).
- 바꾼 시험(모두 작성자 본인 줄): `StatusEffectContractTests`(키 범위 0~19), `EnemyDeathTraitFoundationTests`(대기 사례 → 세 특성 모두 등록·사용 가능), `IntimidationTraitTests`·`RegenerationTraitTests`(42 → 43), `LegacyEnhancementRetirementTests`(39 → 40, 29 → 30), `CombatChangeEvidenceCoverageTests`(`Reconstructed` 표본), `CombatChangeEvidenceVariantTests`(370·380 표본).
- 새 파일(+ `.meta`): `ReconstructionStatus.cs`, 재구성 특성 묶음(`ReconstructionTraitDefinition`·`ReconstructionRuntimeState`·`ReconstructionTraitStatusSnapshot`·`ReconstructionTraitRule`·`ReconstructionEffectRule`·`ReconstructionTraitFactories`), `ReconstructionTurnApplication.cs`, `T/ReconstructionTraitTests.cs`, `T/ReconstructionReviveTests.cs`.
- 계획과 다른 점:
  - 판단·결과 원본은 계획의 `ReconstructionRules`가 아니라 특성 규칙 꼴 `Domain/Traits/Rules/ReconstructionTraitRule`(`CanTrigger`·`Trigger`·`IsReconstructing`·`BeginOwnTurn`)이다. 다른 특성 묶음과 같은 자리다.
  - 상태는 `AppliedStatus.cs`에 더하지 않고 새 파일 `ReconstructionStatus.cs`에 두었다. `CharacterStatusCompatibility.cs`는 고칠 곳이 없었다.
  - 재구성 확인을 무장 해제·발목 꺾기보다 먼저 한다. 둘이 겹치면 거절 이유는 `Reconstructing`이다. `IsAttackRestricted`·`IsMovementRestricted`도 재구성을 보므로 AI·포효·예보가 같은 판단을 따른다.
  - 낙석 예보 `KeepsBindAtNextOpportunity`는 남은 자기 턴 시작이 2 이상인 재구성 유닛을 다음 기회에도 묶인 것으로 본다(다음 자기 턴 시작에 아직 부활하지 않으므로).
  - 부활 발동 사건(380)은 그 유닛의 상태 사건 바로 앞에 낸다. 줄기만 하는 턴 시작은 상태 사건만 낸다.
  - `I/ReconstructionPublicationTests`는 공포와 같은 이유로 Phase F의 합친 게시 시험으로 옮겼다.
- 시험 범위:
  - `ReconstructionTraitTests`: 단계별 보호막(0·50·50), 치명 → 최대 체력·남은 2·`Used`·발동 크기 = 잃은 HP·사망 없음·사건 순서·판독기, 재구성 중 사망 특성 없음과 다음 치명에 진짜 사망 + 자폭(B·C 결합, R4), 전투당 1회, 마무리 사격 즉사는 가로채지 않음, 공개 미리보기 반영(R6), 낙사는 가로채지 않음, 재구성 중 피해·넉백 받음.
  - `ReconstructionReviveTests`: 플레이어 턴 발동 → 다음 적 턴 시작에 1로 줄고 이동·공격 거절(`Reconstructing`) → 그다음 적 턴 시작에 부활·행동 가능·보호막 남음·발동 사건 순서·판독기(Lv1·Lv2), 플레이어 턴 시작은 세지 않음.
  - 계획 목록에서 이번에 쓰지 않은 것: 위스키 미발동(코드로만 확인 — `WhiskeyItemRule`은 피해 뒤 체력이 기준 비율 이하일 때만 발동하고, 재구성은 체력을 최대로 돌린 뒤 아이템 단계를 부른다), 자기 턴 중 발동(반사·지속 피해) 셈, 끌어오기 받음, 낭떠러지로 밀린 진짜 사망(낙사 투영 시험으로 대신), 처치 연쇄·처치 보상 없음의 직접 확인(사망 사건 없음으로만 확인). Phase F 합친 시험에서 다룰지 정한다.
- 시험(배치): 전체 6,570 = 통과 6,538 · 실패 21 · 건너뜀 11. 실패 목록은 기준 21개와 같다. 새 재구성 시험 12개 모두 통과, Phase C의 새 실패(`ThornHeartEnemyAdvance_…`)도 다시 통과한다.

### Phase E 실행 — 2026-11-10

- 사용자 결정(2026-11-10, 착수 때 질문):
  - 칸 정산: 공격 넉백에서 꺼내지 않고, 이미 있는 이동 투영 강제 이동(타일 효과 경로)을 8방향 걸음·이동 원인 입구로 넓혀 재사용한다. 두 경로의 칸 효과는 실질적으로 같다(가시 피해 계산·보유 방어·포효 셈이 같고, 공격 쪽에만 있는 가시 접촉 상태 함수는 빈 함수). 다른 것은 공격 장부 기록과 마지막 칸 오브젝트를 비용 정산 뒤로 미루는 것인데 둘 다 공격 전용이다.
  - 진영 턴 시작 `CombatTurnCalculator.BeginControllerTurn(input, rules)`에 칸 규칙을 필수 인자로 더한다. 시험의 직접 호출 37곳을 빈 규칙으로 맞추고, 그중 다른 작업자(Droute) 파일 `ControllerTurnCountTests` 한 줄도 사용자 허락으로 고친다.
  - 부활 순간 주변 8칸의 여러 칸 유닛은 밀지 않는다(사건 없음). 기존 투영 강제 이동은 여러 칸 유닛을 받지 않고 예외를 던진다.
- 바꾼 파일: `AttackKnockbackCalculator.cs`(면역 판정 두 곳을 새 함수로), `MovementEnvironmentCalculator.cs`(타일 강제 이동이 새 입구 `ResolveForcedStepsSteps`를 부름), `MovementProjection.cs`(`RecordForcedMove` 끝 선택 인자 `movementCause`), `CombatTurnCalculator.cs`(`rules` 필수 인자), `CombatCommandCalculator.cs`(호출 둘), `ReconstructionTurnApplication.cs`(부활 넉백).
- 바꾼 시험: 진영 턴 시작 직접 호출 37곳(작성자 본인 20개 파일 + `ControllerTurnCountTests` 한 줄 — Droute, 허락분). 호출에 빈 규칙 인자만 더했다.
- 새 파일(+ `.meta`): `KnockbackImmunityCheck.cs`(넉백 면역 판정 원본, 공격 넉백·시체 밀기·부활 넉백이 부름), `T/ReconstructionReviveKnockbackTests.cs`.
- 구현 중 정한 것:
  - 부활 넉백 대상은 부활 순간 주변 8칸의 살아 있는 한 칸 캐릭터(진영 무관), EntityId 순. 방향은 공격 넉백과 같은 `ForcedDisplacementGeometry.ResolveBranchStep`, 거리 1, 넉백 지형 규칙으로 진입 검사.
  - 이동 사건 원인은 강제 + 특성 키 `Reconstruction`. 발동 사건 `KnockbackApplied`(출처 = 부활한 유닛, 크기 1)는 실제로 밀린 대상만 싣는다. 판독기는 공격 경로가 없으면 대상마다 실제 이동을 요구한다. 굳건한 의지로 막히면 막힘 발동(`KnockbackPrevented`)만 남긴다. 발동 사건들은 넉백 이동 사건들 앞에 넣는다.
  - 밀린 유닛이 칸 효과로 죽으면 사망 특성은 턴 시작 투영의 완료 정산(Phase B 확인 지점)이 처리한다. 낙사는 설계대로 사망 특성이 없다.
  - 승패가 정해지면 남은 부활·밀기를 하지 않는다. 앞선 부활 넉백으로 죽은 재구성 유닛은 건너뛴다.
- 시험 범위: `ReconstructionReviveKnockbackTests` — Lv3 8방향(플레이어·적 섞임, 사건 원인·발동 대상·판독기), Lv2 넉백 없음, 막힌 칸(목록에서 빠짐), 굳건한 의지 막힘, 가시로 밀려 죽은 유닛의 자폭(턴 시작 완료 정산), 메인 사망으로 승패가 정해지면 남은 밀기 중단. 지뢰·낭떠러지 사례는 쓰지 않았다(지뢰는 오브젝트 규칙 고정물이 필요해 Phase F 합친 시험에서 정한다).
- 시험(배치): 전체 6,576 = 통과 6,544 · 실패 21 · 건너뜀 11. 실패 목록은 기준 21개와 같다. 새 부활 넉백 시험 6개 모두 통과.

### Phase F 실행 — 2026-11-10

- F.1 낙석 연결:
  - 먼저 특성화 사례 둘(사망·생존 섞임, 메인 사망 패배)을 `RockfallDeathTraitTests`에 두고, 바꾸기 전 코드로 골라 돌려 실제 출력(사건·최종 상태·영혼·승패·기믹 상태·보상 유무)을 받아 고정했다(TP-2).
  - `RockfallGimmickRule.Impact` 끝을 `MovementProjection.ForArrival(input.WithActor(EntityId.Empty), projected, events, state).Complete()`로 바꿨다. 사망 위치 사전은 넘기지 않는다. 충돌로 죽은 유닛은 투영 생성 때 이미 죽어 있어 완료 정산이 맞기 전 위치(이전에 넘기던 값과 같음)로 영혼을 정산한다.
  - 수신자 반복에서 재구성 가로채기(치명이고 `CanTrigger`면 `Reconstructed` 사건 + 생존자 속박). 재구성 발동(370)은 모든 수신자 사건 뒤에 둔다. 판독기 `ValidateRockfallRecipients`가 `Reconstructed`를 체력 변화 자리로 받는다.
  - 특성화 사례 둘이 그대로 통과했다. 코드상 차이는 완료 정산의 처치 특성 정산·사냥꾼 대상 맞춤이 낙석에도 도는 것이다(다른 명령·턴 시작과 같아짐). 이를 드러내는 사례는 쓰지 않았다.
  - 낙석은 실제 진영 종료에서만 진행해(`CombatTurnCalculator`의 기믹 진행) AI 미리보기에 영향이 없다.
- F.2 합친 시험:
  - `RockfallDeathTraitTests`(R5): 낙석 사망 자폭이 모든 수신자 사건 뒤, 낙석 사망 공포(낙석 속박이 더 길어 남음), 재구성 적 낙석 생존 + 속박.
  - `SelfDestructTraitTests.KillChain_FinishesBeforeTheExplosion_AndDoesNotCountExplosionKills`(R1): 처치 연쇄 대상이 무작위여도 성립하는 조건으로 확인 — 처치 연쇄 발동이 모두 폭발 앞, 원 공격 피격 결과는 대상 2·체력 0 그대로, 판독기 통과.
  - 새 `I/EnemyDeathTraitPublicationTests`: 공포(이웃 적의 무장 해제·속박 게시), 자폭(이웃 적 체력 게시), 재구성(게시된 상태·`Used` → 적 턴 두 번: 첫 턴은 남은 1·AI가 움직이지 않음, 둘째 턴에 부활). 실제 명령 처리·자동 진행·Buffer 게시를 지난다.
  - 계획과 다른 점: `EnemyDeathTraitsCombinedTests.MixedRoundTrip`(세 특성·부활 넉백·낙석을 한 전투에서)은 특성별 게시 시험과 낙석 단위 시험으로 나눠 대신했다. 지뢰 사례·기차 사망 사례는 쓰지 않았다(기차는 `Finish`를 직접 불러 구조상 확인을 타지 않음 — 코드 확인).
  - 첫 전체 실행은 통합 시험이 Domain의 `TraitEffectOrigin`을 써서 컴파일 오류로 멈췄다(통합 시험 어셈블리는 Domain을 참조하지 않음). 출처를 필드로 비교하게 고친 뒤 다시 돌렸다.
  - 시험(배치): 전체 6,585 = 통과 6,553 · 실패 21 · 건너뜀 11. 실패 목록은 기준 21개와 같다. 새 시험(낙석 5·게시 3·자폭 R1) 모두 통과.
- 변경 독립성 다시 평가(§7, 코드 확인):
  - 자폭 비율·공포 기간: 단계 수치는 `EnemyDeathTraitLevels` 한 곳, 단계 없는 상수는 특성 규칙 한 곳(공포 기간 `FearTraitRule.BaseAffectedSideTurns`, 재구성 대기 `ReconstructionTraitRule.InitialOwnTurnStarts`). 실행자 계약만 알면 된다.
  - 재구성 부활 시점: 순서 계약은 `BeginControllerTurn` 한 곳(특성 수명 → 투영 → 부활·부활 넉백 → 지속 피해). 이 함수는 이제 `rules`를 받는다.
  - 낙석 피해 규칙: 낙석은 "이동 투영 완료로 끝남" 계약만 지킨다. 사망 특성 실행 방식은 몰라도 된다.
  - 칸 효과 변경: E.1을 바꿔서 공격 넉백(`AttackForcedMovement`)과 투영 강제 이동(`ResolveForcedStepsSteps`) 두 경로가 남는다. 낙사·약한 지반·토네이도·오브젝트는 같은 계산기를 부르지만, 가시 피해는 두 구현(공격: 환경 피해 계산 + 보유 방어, 투영: 가시 칸 규칙 + 체력 변경)이라 가시 규칙을 바꾸면 두 곳을 본다. 이 두 벌은 이번 작업 전부터 있던 것이고 이번에 늘지 않았다. 합치는 것은 별도 후보로 남긴다.
  - 적 전용 제한: 원본 `TraitOwnerPolicy.IsEnemyOnly` + 검사 넷(`UnitData`, 성장 후보, 병과 기본 특성, `UnitEligibilityRules`).
- F.3 문서: Core 전달 문서 §67(View 인계, 제목 갱신, 미전달), 설계 상태 줄·§16(구현에서 바뀐 것). 기획 공유 문구는 채팅으로 냈다.
- 남은 것: 사용자 Unity 확인·커밋, View 대응(§67.1, 특히 상태 19 표시 변환), 기획의 적 유닛 특성 지정, 가시 정산 두 벌 합치기(후보).

### 후속: 즉사도 재구성 발동 — 2026-11-10

- 사용자 결정: 마무리 사격 즉사도 재구성이 버텨야 한다(Q-ED1 (1) B → A). 설계 §16.1.
- 바꾼 파일: `CombatDamageApplication.DefeatSteps`(재구성 가로채기 — 370 + `Reconstructed`, 처치 정산 없음, 회복 기록), `AttackDirectDamageCalculator`(살아 있으면 사망 위치를 남기지 않음), `CombatChangeEvidenceReader`(즉사 검사가 `Reconstructed`도 받음).
- 바꾼 시험: `ReconstructionTraitTests.FinishingShotInstantDefeat_IsNotIntercepted` → `…_IsAlsoIntercepted_UntilReconstructionIsUsed`(작성자 본인).
- 문서: 설계 Q-ED1·§4 주석·§16.1, Core 전달 문서 §67 `Reconstructed` 줄.

### 후속: 희생이 마무리 사격 즉사보다 먼저 — 2026-11-10

- 사용자 결정: 희생이 지키는 대상은 피해 자체를 받지 않으므로 즉사가 터지면 안 된다. 수호자도 즉사하지 않고 숫자 피해만 받는다. 설계 §16.1.
- 바꾼 파일: `AttackDirectDamageCalculator`(희생 넘기기를 먼저 정하고, 넘겼으면 즉사 판정 안 함), `SacrificeDamagePlanner.SimulateSteps`(즉사가 걸릴 대상을 치명으로 세고, 그 넘기기를 불필요로 지우지 않음), `KillChainAttackCalculator`(추가 공격도 희생을 먼저 풀고 넘겼으면 즉사 안 함).
- 바꾼 시험(작성자 본인): `SacrificeTraitTests.FinishingDefeat_HasNoSacrificeReaction` → `FinishingTarget_IsProtected_AndNoInstantDefeatFires`, `SacrificeUpgradeTests.InstantDefeatDoesNotRedirectOrGrant` → `FinishingTarget_IsRedirected_AndTheBearerTakesOnlyNumericDamage`, `CannonThresholdTraitTests.Finishing_BypassesShieldWithoutSacrifice` → `Finishing_BypassesShield`(희생 없이 보호막만), `MixedArea_…` → `MixedArea_SacrificeProtectsOneLethalVictim_WhetherFinishingOrNumeric`.
- 문서: 설계 §16.1, Core 전달 문서 §67.6.
- 정리(같은 날, 사용자 "정리해 줘"): 순서 규칙이 세 곳에 따로 있던 것을 판단 함수 하나 `FinishingShotApplication.FiresOnHit(입력, 공격자, 원래 대상, 실제 수신자, 사실)`로 모았다. 공격 직접 피해·처치 연쇄·희생 모의 계산이 부른다. 희생 계획은 "지키지 않으면 즉사로 죽을 대상"을 치명 후보로 셀 때만 `Qualifies`를 따로 쓴다(이름 `finishingTarget`). 동작은 같다.
- 시험 추가: `SacrificeUpgradeTests.ExtraAttackOnAFinishingTarget_IsRedirected_WithoutInstantDefeat`(처치 연쇄 추가 공격 경로).
- 시험에서 드러난 잠복 결함(같은 날): 범위 공격이 희생 수호자 자신도 맞히고, 수호자가 앞선 타격을 대신 맞아 죽으면, 뒤의 자기 타격에서 죽은 유닛의 빈 칸으로 거리를 계산하다 예외가 났다. 희생 계획의 모의 계산은 이미 죽은 대상의 타격을 건너뛰는데 실제 적용은 건너뛰지 않았다. `AttackDirectDamageCalculator`가 같은 공격의 앞선 타격으로 이미 죽은 캐릭터의 숫자 타격을 건너뛰게 맞췄다(강제 즉사 원인 타격은 그대로). 이번 순서 변경으로 수호자가 가까운 즉사 대상을 지키게 되면서 `CannonThresholdTraitTests.MixedArea_…`에서 드러났다.
