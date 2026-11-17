# 가시 폭탄 Phase 0 — 계약·삭제 범위·시험 소유권 조사

- 일자: 2026-11-17, 작성: Codex.
- 상태: **Phase 0 조사·목록화 완료. Phase 1 미착수.** 구현/삭제/시험 변경 승인을 대신하는 문서가 아니다.
- [설계](../specs/2026-11-17-cactus-cannon-thorn-bomb-design.md) / [구현 계획](2026-11-17-cactus-cannon-thorn-bomb-implementation-plan.md).
- 기준 브랜치: `feature/Cheon/Core/NewCombatSytem`, HEAD: `1abe42230997f6db0fb9076d7c6a5732b49caf34`.
- 실제 확인: 코드/에셋 텍스트, GUID 역참조, git blame/log. 컴파일 미확인. Unity·Test Runner·MSBuild는 실행하지 않았다.

## 1. 승인 범위와 작업 상태

사용자는 현재 계획의 **Phase 0부터 진행하고 끝나면 멈출 것**을 지시했다. 이 지시로 조사·목록화·문서 갱신을 수행했다. 설계 R01~R14의 명시적 합의와 P01~P11의 설계자 제안을 구분하며, P항목을 대화에서 개별 확정한 사실로 다시 표기하지 않는다. 이번 단계에서 제안을 구현하거나 규칙을 바꾸지 않았다.

| 범위 | 실행 주체·권한 |
|---|---|
| Phase 0 | 이 세션 Codex: 조사와 문서만. 완료 뒤 중단 |
| Phase 1~2 Core | Core 담당자 또는 사용자가 해당 Phase 실행을 명시적으로 맡긴 작업자. 이번 Phase 0 지시로 선행 실행하지 않음 |
| Phase 3 View/DB 및 Core 삭제 | 각 소유자 연결 지점 조율. 사용자의 다음 Phase 지시 범위에서 수행 |
| 컴파일·재생 | 사용자 Unity 확인. View Test 코드 작성·전투 View MSBuild 금지 유지 |
| 기존 시험 | 컴파일에 필요한 최소 호출 보정과 행동/계약 기대값 변경을 구분. 후자는 아래 구체 목록에 대한 승인 없이 수정하지 않음 |

시작 시 작업 트리에는 사용자의 `Assets/Audio/FMODplus/New Fmod Event Catalog.asset` 수정과 이 채팅에서 만든 설계·계획·공동 인계 문서 변경이 있었다. 모두 보존했다. HEAD 변경·커밋·push·branch 변경은 하지 않았다. `.git`과 common-dir가 같은 일반 checkout이며, 이번 작업은 제품 구현을 시작하지 않는 Phase 0이므로 기존 문서가 있는 checkout에서 진행했다.

`TraitKey.cs`의 신규 번호 **540은 현재 미사용**이며 기존 최대 활성 번호는 `DeadHand = 530`이다. `ThornField = 40`은 새 특성 번호로 재사용하지 않는다. 이후 Phase 시작 때 다시 검사한다.

사용자 첨부 `feature-change-locality-guidelines.md`와 루트/Assets AGENTS.md를 다시 확인했다. 조사 목록은 자동 삭제 입력이 아니다. 동일한 사실의 원본, 조립/기능 분리, 공유 피해·이동·게시 계약은 기존 설계 §8을 기준으로 보존한다.

## 2. 조사 결과 요약

기본 검색은 `ThornField|ThornConsumed|ThornMovementTileRule|FieldThornPresenter`, 대상은 Assets의 `.cs/.asset/.prefab/.unity`였다.

| 분류 | 수량 | 의미 |
|---|---:|---|
| 기본 검색 결과 | 191파일 | 최종 수정/삭제 파일 수가 아니라 텍스트 의존 후보 |
| 제품 소스 | 71파일 | 전용 구현과 공용 타입/호출/등록 포함 |
| 기본 검색 에셋 | 4개 | 선인장 SO 1개 이전, 장판 전용 SO 3개 삭제 후보 |
| 시험/fixture | 116파일 | 아래 59/56/1로 분류 |
| 행동·공개 계약·fixture 검토군 | 59파일 | 변경/삭제 허가가 필요한지 본문과 호출자를 검토해야 함. 전부 삭제하는 목록이 아님 |
| API·복사 인자 보정 후보 | 56파일 | 빈 배열/기존 상태 전달/빈 사전. 기대값을 바꾸지 않는 최소 호출 보정으로 한정 |
| 이름만 장판인 공통 시험 | 1파일 | `ThornFieldDomainTests` 보존 |
| 간접 View 소비자 | 추가 2파일 | `CombatFieldEventPorts`, `CombatSceneEventPresenter`의 delegate/port 연결 |
| 전용 삭제 후보 | 22개 + 각 `.meta` | 전용 C# 18개, SO 3개, 전용 프리팹 1개. 아직 삭제하지 않음 |

`CombatTraitTestFixtures.Thorn*`, `WithFields`, `ThornInput` 등 알려진 fixture 호출도 추가 검색했다. 이 검색에서는 위 116파일 바깥의 추가 시험 파일이 나오지 않았다. 이는 실제 컴파일/회귀 완료 증거가 아니며, 생성자 변경 후 사용자 Unity 확인은 여전히 필요하다.

## 3. 삭제·보존 판단

### 3.1 전용 파일 삭제 후보와 GUID 역참조

아래 파일과 `.meta`는 Phase 3에서 등록/소비자 이전 뒤 삭제하는 후보다. GUID 참조가 없다는 것은 C# 타입 참조도 없다는 뜻이 아니다. 순수 C# 타입 참조는 §3.3을 함께 처리한다. 폴더 `.meta`는 폴더가 실제로 비었을 때만 함께 정리한다.

| 파일 | GUID | 역참조 |
|---|---|---|
| `Assets/ProjectL/DataBaseSO/ViewGroup/ThornFieldVisual/Item/ThornFieldVisual_Default.asset` | `f91b54de0b13433590096756cf2fcd94` | `Assets/ProjectL/DataBaseSO/ViewGroup/ThornFieldVisual/ThornFieldVisualDataTable.asset:16` |
| `Assets/ProjectL/DataBaseSO/ViewGroup/ThornFieldVisual/ThornFieldVisualDataTable.asset` | `2ff468128b63412ea8759bc7205501f4` | `Assets/ProjectL/DataBaseSO/ViewGroup/ViewGroup.asset:31` |
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Cannon/TraitVisual_ThornField.asset` | `faeeb6b5d52a53f9dd138dc0f80ca77e` | `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/TraitVisualDataTable.asset:18` |
| `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/ThornField/Item/ThornFieldVisualDataSO.cs` | `6eda941779e94313a3eb565ceaf0ffc6` | `Assets/ProjectL/DataBaseSO/ViewGroup/ThornFieldVisual/Item/ThornFieldVisual_Default.asset:12` |
| `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/ThornField/ThornFieldVisualDataTableSO.cs` | `549938481dd64bacb0470382dad70513` | `Assets/ProjectL/DataBaseSO/ViewGroup/ThornFieldVisual/ThornFieldVisualDataTable.asset:12` |
| `Assets/ProjectL/Script/Systems/DataBaseSystem/Queries/ViewDataBase/ViewTable/ThornFieldVisualTable.cs` | `187b1276f8b7483a816a3a0bbc66a743` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/ThornFieldApplicationCalculator.cs` | `714fe2bfde4baa14683dbd4c42b52b09` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/ThornMovementTileRule.cs` | `ca6c493666504873b153232ba9fc815e` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/Traits/ThornFieldTraitSpec.cs` | `e4af18de2abc4407934545ab59dc4e0d` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/ThornFieldCellSnapshot.cs` | `65b7344240c446bbaf4dcc48aa5a27f2` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/Traits/ThornFieldTraitStatusSnapshot.cs` | `478eddc8aad24f3db6b685587f8e5ea2` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/Contexts/ThornFieldApplicationEffectContext.cs` | `071fb3e88d724adf915c0d15d5ca6325` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/Evaluations/EffectThornFieldContribution.cs` | `978d66ec7ae74b709ac2dc165df8ff97` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/Evaluations/EffectThornFieldEvaluation.cs` | `24fe179271fc4268b907a5b5e2308a23` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Field/ThornFieldCell.cs` | `9ae49a4752d44957b847190c44204584` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Definitions/ThornFieldTraitDefinition.cs` | `a5d7221941d247fb8ea7a7742d260d10` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Effects/ThornFieldEffectRule.cs` | `1b1a63829647461395c86f08c5f06636` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Effects/ThornFieldEffectStateSnapshot.cs` | `6dd6d6422e3144aba1c6844ab950105a` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Rules/ThornFieldTraitRule.cs` | `df5db144935c4100a8fc4bdbba48a717` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/States/ThornFieldRuntimeState.cs` | `ea2d59c314e24b92bc0fb0959412c80b` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/FieldThornPresenter.cs` | `62782844e40e42d98815873c1f9f8987` | Assets 텍스트 GUID 역참조 없음 |
| `Assets/ProjectL/Prefabs/Combat/ThornCactus.prefab` | `09fc9d774997b0349b84c5298e5ceca1` | `Assets/ProjectL/DataBaseSO/ViewGroup/ThornFieldVisual/Item/ThornFieldVisual_Default.asset:16` |

### 3.2 보존·이전할 공유 자원

- `Assets/ProjectL/Sprites/AbilityIcon/가시.png`와 `.meta`: **보존**. GUID `5b7681cdc68f04245ab7eb9361e9d7b0`를 `TraitVisual_ImpactBolt.asset:18`도 참조한다. 옛 TraitVisual을 삭제해도 아이콘을 삭제하면 안 된다.
- `Assets/ProjectL/Prefabs/Combat/ThornCactus.prefab`: GUID `09fc9d774997b0349b84c5298e5ceca1`의 사용처는 현재 `ThornFieldVisual_Default.asset:16`뿐이다. Assets/ProjectSettings/Packages의 GUID·이름 참조를 확인했으며 독립 삭제 후보로 분류했다. 프리팹이 참조하는 모델/하위 자원은 삭제 대상에 포함하지 않는다.
- `ViewGroup.asset:31`과 `TraitVisualDataTable.asset:18`: 전용 SO 삭제 전에 각각 장판 테이블/옛 특성 외형 등록을 제거한다. Catalog가 ViewGroup을 읽는 기존 연결 자체는 유지한다.
- `Unit_CactusCannon.asset`: 삭제하지 않고 managed-reference 특성을 이전한다. 공격력/사거리/기본 스플래시/벽 정책은 유지한다.
- `UnitTraitAuthoring.cs`, `UnitTraitData.cs`, `TraitFactories.cs`: 여러 특성이 함께 있는 파일이므로 장판 부분만 제거한다.
- `TraitKey.ThornField = 40`: 퇴역 번호 보존. `CharacterCategoryContractTests`의 동일 숫자·다른 origin 구분 검증도 그대로 유지 가능하다.
- `CombatEventKind.ThornConsumed`: 뒤에 이어지는 사건 번호가 현재 암시적으로 증가하며, `WeakGroundStateTests`가 번호 호환성을 검사한다. 소모 이벤트 실행/구체 타입을 지우더라도 뒤 번호를 당겨서는 안 된다. 기존 번호를 남겨 예약하거나 이후 값을 명시하는 처리는 Core 계약 검토 항목이며 시험을 꺼서 우회하지 않는다.
- `ThornHeart` 반사, 기존 공용 상태/피해/이동 규칙, 옛 Missing Script 에셋 48개는 보존한다. 이번 전용 SO 두 종류의 m_Script GUID는 실제 지정한 스크립트에 연결돼 있다. 옛 Missing Script 에셋을 별도 삭제하는 작업은 없다.

### 3.3 공용 제품 파일 — 해당 기능의 멤버/호출/등록만 변경

다음은 기본 검색의 제품 소스에서 전용 삭제 파일을 제외한 목록과 간접 View 소비자다. 이 목록 전체를 리팩터링하라는 뜻이 아니다. 신규 등록, 옛 상태/매개변수/소모 경계 제거에 필요한 부분만 다룬다.

<details>
<summary>공용 제품 소스 전체 목록</summary>

- `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait/UnitTraitAuthoring.cs`
- `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait/UnitTraitData.cs`
- `Assets/ProjectL/Script/Systems/DataBaseSystem/Queries/ViewDataBase/ViewDataBase.cs`
- `Assets/ProjectL/Script/Systems/Factory/DBFactory/Group/ViewDataBaseFactory.cs`
- `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/CombatSystemFactory.cs`
- `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs`
- `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`
- `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/MainSpecializationDefinitionSnapshot.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/TraitSpecAdapter.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/AttackDeliveryExecutor.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/AttackForcedMovement.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/BossRoarCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandEffects.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CommandAttackProjection.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/DeferredCombatFeatures.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnemyAi/EnemyActionCandidates.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementContextCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementEnvironmentCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementProjection.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/NexusReinforcementCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/StageGimmicks/RailwayGimmickRule.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Traits/TraitEffectProvider.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Traits/TraitFactories.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Traits/TraitRegistry.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster/OwnedCharacterStatsCalculator.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/BattlePreparation.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatAttackProfile.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatChangeEvidenceReader.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatCommandState.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/MovementPreviewContext.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/MovementUndoOriginFacts.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Common/TraitKey.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/StageSnapshot.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/CombatEffectEngine.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/ICombatEffectEngine.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Effects/TraitEffectStateCodec.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatDisplayTables.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/CombatFieldDisplay.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/CombatFieldEventPorts.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/CombatSceneEventPlayback.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/CombatSceneEventPresenter.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatFactEventHandler.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Ordering/CombatEventTypeTable.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Movement/CombatStepContacts.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Movement/CombatUnitMotionPlayback.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Descriptions/Traits/TraitDescriptionBindings.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Descriptions/Traits/TraitDescriptionFormatter.cs`
- `Assets/ProjectL/Script/Systems/ServiceKeys/DBFactory/Group/ViewTableKey.cs`

</details>

## 4. 기존 시험 — 무엇을 보존하고 무엇을 먼저 승인받을지

### 4.1 우선 확인할 구체 변경

1. **Phase 1 전: `ContractShapeGuardTests.MovementUndoContractsRequireExplicitComparisonAndCopyDecisions`** (`Assets/Tests/EditMode/ContractShapeGuardTests.cs:19`). `CombatCommandState`의 정확한 공개 속성 목록을 검사하므로 `ThornBombs` 추가만으로도 기대 목록이 달라진다. 추가한 상태의 With/복사/비교/게시 계약을 확인한 뒤 기대 목록에 `ThornBombs`를 추가해야 한다. Phase 3의 `ThornFields`/`Thorns` 삭제 때도 대응 목록 변경이 필요하다. 이것은 컴파일용 호출 보정이 아닌 **계약 기대값 변경**이다. 현재 관련 행 작성자는 `GwonCheonmyeong`, 근거 commit `df7b5d2093092f99c1291e61242a33ae3b2ab720`. 이번에 수정하지 않았다.
2. **보존: `ThornFieldDomainTests.ResolvePathCells_UsesDiagonalFirstOrder_AndDeduplicatesFootprints`** (`:17`). 장판 생성을 검사하지 않고 `MovementPathCalculator`의 대각 우선·회전 footprint·경로 중복 제거를 검사한다. 현재 공통 시험과 6개 TestCaseData를 유지한다. 구현 본문 관련 이력은 `3682d7fc8dd05dad92c7fce3b4d0ed7eec12db94`, 작성자 `GwonCheonmyeong`. 파일명 때문에 전체 삭제하지 않는다. 불필요한 개명도 이번 범위에 넣지 않는다.
3. **Phase 3 폐기 후보: `ThornFieldTraitTests`의 아래 5개 시험과 그 전용 fixture**. 장판 실행 계약을 직접 검증하므로 특성 삭제에 따라 폐기할 구체 대상이다. 삭제 승인 전에는 그대로 둔다. 폭탄 시험으로 이름/기대값만 갈아끼우지 않는다.
   - `DefaultRegistry_RoundTripsDefinitionRuntimeCodecAndStatus`
   - `StaticContracts_RejectNonPositiveValues`
   - `ApplicationCells_ExcludeCenter_AreOrdered_AndUseCheckedArithmetic`
   - `ApplicationCapability_EmptyBindings_ReturnsNoContribution`
   - `ApplicationCapability_TwoContributors_PreserveValuesInOriginOrder`
4. **혼합 시험은 파일 통째로 삭제하지 않는다.** 예: `EnhancementPoisonThornTests`의 장판 보류/접촉 시나리오와 사망 시 기존 debuff 제거 검증은 목적이 다르다. `ThornHeartReflectionCommandTests`, `ImpactBoltTests`, `KnockbackPathTests`, `ReconstructionReviveKnockbackTests` 역시 장판을 피해 fixture로 쓰는 부분과 유지해야 하는 반사/넉백/사망 순서 계약을 구분한다. 다른 피해 fixture로 교체해도 동일 검증인지 판단이 필요하므로 자동 호출 보정으로 취급하지 않는다.
5. **보류→퇴역 사유 검증은 별도 승인 대상이다.** `DeferredCombatFeatureTests`, `TraitDefinitionContractTests`, `StatusEffectRetirementTests` 및 성장 후보 검증의 `ThornField` 케이스를 삭제 기능의 존재만으로 일괄 제거하지 않는다. 예약 키를 계속 거부한다는 검증은 유지할 수 있다.

### 4.2 소유권 판정 방법과 변경 허용 범위

`git blame --line-porcelain`로 관련 참조 행과 공통 이동 시험 본문의 작성자를 확인하고 `git log -1`로 파일의 최신 변경도 확인했다. 아래 59파일의 관련 참조/계약 행은 모두 `GwonCheonmyeong`으로 기록되어 있다. 다만 파일 최신 작성자는 `StatusEffectRetirementTests`가 `Droute`, `TraitGrowthCatalogTests`가 `CCRC_Hojin`인 경우도 있다. 파일의 최신 작성자만으로 시험 소유자를 판정하지 않았다.

Git 작성자만으로 이 채팅에서 작성한 시험임을 증명할 수 없다. 따라서 이 조사에서 **우리 시험이라고 임의 분류해 실행 기대값을 수정할 수 있는 항목은 없다.** 루트 AGENTS.md의 “다른 사람이 쓴 시험이면 고치기 전에 먼저 묻는다”를 적용해 실제 변경 전에 필요한 범위를 제시한다. Phase 0은 그 구체 목록을 만드는 작업이며 변경 승인/시험 실행을 완료한 단계가 아니다.

### 4.3 행동·계약·fixture 검토군 59파일

아래는 변경 승인 검토 목록이다. 반드시 기대값을 바꿔야 한다는 뜻은 아니며, 예약 키/일반 계약은 보존 가능하다. 위치는 조사 시점 기준이고, 함수명은 관련 참조의 소속 함수/fixture다. TestCase 속성처럼 선언 앞의 참조는 별도로 표시했다. 최신 commit은 파일 이력, 행 commit은 실제 관련 참조 행의 이력이다.

| 시험 파일·위치 | 관련 함수/fixture | 행 작성자·대표 commit / 파일 최신 commit | 처리 범위 |
|---|---|---|---|
| `Assets/Tests/EditMode/AttackCommandCharacterizationTests.cs:66` | `C3_Knockback_ThroughThornOntoObjectCell`<br>`C6_NightAttack_DirectDeathAndKnockbackThornDeath`<br>`Signature` | GwonCheonmyeong `fd9784242` / `0d92812d0` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/AttackProfileRuleTests.cs:54` | `PiercingWithThornField_IsRejected`<br>`ThornCannon` | GwonCheonmyeong `e58fb5175` / `f6a18f3f7` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/AttackTargetPolicyIntegrationTests.cs:39` | `PersistentThornField_MovementStillCausesEnvironmentalDamage`<br>`ThornField_ActorInsideApplicationRingIsExcluded`<br>`ThornField_AlliedStructureStillPreventsFieldCreation`<br>`ThornInput` | GwonCheonmyeong `680f309ff` / `0d92812d0` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/BossHitCounterTests.cs:60` | `SplashWithDeferredThornTrait_CountsOnce` | GwonCheonmyeong `504877022` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CactusLobbedAttackTests.cs:49` | `Cactus_DeliveryUsesExplicitPolicy_InRangePreviewAndExecution`<br>`Input` | GwonCheonmyeong `97f2815f5` / `98fd0c642` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CharacterCategoryContractTests.cs:220` | `TraitAndCategoryOrigins_StayDistinctWithSameNumericDefinitionKey` | GwonCheonmyeong `4bd6b574f` / `98fd0c642` | 키 40 보존 시 검증 유지; 삭제 대상 아님 |
| `Assets/Tests/EditMode/CliffFallTests.cs:152` | `AttackKnockback_ResolvesTerrainBeforeThornsAndObjects_AndPreviewAgrees` | GwonCheonmyeong `fd9784242` / `0d92812d0` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CombatChangeEvidenceCoverageTests.cs:193` | `Samples` | GwonCheonmyeong `df7b5d209` / `1abe42230` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CombatCommandEffectTests.cs:102` | `Cannon_EmptyNearAimIsRejectedBeforeAnyEffects`<br>`Cannon_NewSplashDoesNotStackLegacySquare_AndIndirectNearVictimRemainsEligible`<br>`DeferredThornLifetime_DoesNotTickOnControllerBoundaries`<br>`DeferredThorns_DoNotAddDamageOrFieldsAfterOrdinaryCasualties`<br>`ExtremeThornDamage_StopsBeforeSuffixWithoutChangingSourceFacts`<br>`ForcedOrdinaryPlusThornOverflow_ThrowsBeforeAnyInputChanges`<br>`Knockback_BlockedDestinationPreservesPositionResourcesAndStoredField`<br>`Knockback_ReconcilesPositionModifiers_WithoutCountingHpCapAsDamage`<br>`Knockback_ReleasesAllOrdinaryCasualtiesBeforeDestinationsAndConsumesForcedFields`<br>`LethalRoute_AcceptsMovementWithoutAttackOrCountdown`<br>`MainArrivalCollection_PreservesGrownMaximumAndPersistentHealth`<br>`NightForcedDeath_SpawnsAtDestinationAfterCollectionAndDoesNotCountDown`<br>`NoFundingGate_PreventsLethalTraversalFromBeingSkipped`<br>`RejectedSecondRoute_DoesNotConsumeUnenteredThorns`<br>`SouthArrival`<br>`ThornApplication_ExcludesCasterAndClipsFieldsAtBoardEdge`<br>`ThornApplication_StructuresReceiveOnlyOrdinaryDamage_AndBlockFieldCreation`<br>`ThornRefresh_PreservesStoredDamageAndLongerLifetime` | GwonCheonmyeong `fd9784242` / `0e2a5ee06` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CombatDamageBoundaryTests.cs:53` | `ThornDamage_IsUnchangedBesideHeavy` | GwonCheonmyeong `fd9784242` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CombatItemDamageSourceTests.cs:119` | `KnockbackThorns_ActivateAfterOrdinaryDamageWithoutRetestingOrdinaryRules`<br>`MovementThorns_ObserveLatestSlotAndHealth` | GwonCheonmyeong `fd9784242` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CombatTraitTestFixtures.cs:107` | `Stats`<br>`ThornMovement` | GwonCheonmyeong `fd9784242` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/ContractShapeGuardTests.cs:29` | `MovementUndoContractsRequireExplicitComparisonAndCopyDecisions` | GwonCheonmyeong `df7b5d209` / `0d92812d0` | Phase 1 공개 속성 목록 변경부터 승인 필요 |
| `Assets/Tests/EditMode/CoreIntegration/EnemyAdvancePreparationMeasurements.cs:70` | `CaptureStructuralAtoms`<br>`CaptureYieldedOpenEncounters` | GwonCheonmyeong `17bca7b46` / `8f01d993b` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CoreIntegration/EnemyAdvancePreparationTests.cs:448` | `AssertValues` | GwonCheonmyeong `17bca7b46` / `17bca7b46` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CoreIntegration/EnemyAssessmentBoundaryPublicationTests.cs:37` | TestCase/규칙 목록/클래스 fixture — 본문 확인 | GwonCheonmyeong `04c9e3a87` / `04c9e3a87` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CoreIntegration/EnemyEngagementFollowupPublicationTests.cs:38` | TestCase/규칙 목록/클래스 fixture — 본문 확인 | GwonCheonmyeong `7503b8a79` / `7503b8a79` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CoreIntegration/EnemyStageBehaviorCorpus.cs:51` | TestCase/규칙 목록/클래스 fixture — 본문 확인 | GwonCheonmyeong `6185a55b1` / `d84e05bf9` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CoreIntegration/EnemyUtilityPublicationTests.cs:61` | `MovementRules` | GwonCheonmyeong `17bca7b46` / `17bca7b46` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/CoreIntegration/TraitGrowthAuthoringTests.cs:377` | `CategoryDefaults_RejectMissingOrUnavailableDefinition` | GwonCheonmyeong `9bef9e0b0` / `c995dd455` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/DeferredCombatFeatureTests.cs:15` | `UnitClockTraits_AreExplicitlyUnavailable` | GwonCheonmyeong `d4a6d6fd8` / `6144785d1` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnemyActionCandidateTests.cs:271` | `EscapeInput` | GwonCheonmyeong `17bca7b46` / `8f01d993b` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnemyAiBenchmarkFixtures.cs:24` | TestCase/규칙 목록/클래스 fixture — 본문 확인 | GwonCheonmyeong `17bca7b46` / `d84e05bf9` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnemyExchangeModelTests.cs:198` | `LethalTransitRouteExcluded` | GwonCheonmyeong `6185a55b1` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnemyFollowupEstimateTests.cs:197` | `NoDoubleChargeForPreciseMoveAndNoFollowupAfterDeath` | GwonCheonmyeong `04c9e3a87` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnemyLeapAssessmentTests.cs:155` | `NonMineIntermediateEffectsRemainVisible` | GwonCheonmyeong `04c9e3a87` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnemyMovementPathTests.cs:21` | TestCase/규칙 목록/클래스 fixture — 본문 확인 | GwonCheonmyeong `87348b86e` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnhancementLifecycleTests.cs:25` | `DeferredTimers_DoNotTickAtPlayerEnd`<br>`SideStart_ResetsEligibilityWithoutChangingRetainedTimerPayloads`<br>`Turn` | GwonCheonmyeong `d4a6d6fd8` / `0e2a5ee06` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnhancementPoisonThornTests.cs:29` | `DeferredPoisonThorn_DoesNotCreateFieldsOrArmDefenseTimer`<br>`MovementContextRejectsChangedPoisonPayload` | GwonCheonmyeong `d4a6d6fd8` / `98fd0c642` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/EnvironmentDamageTests.cs:73` | `LegacyThorn_DoesNotGainCategoryOrHeavyRules` | GwonCheonmyeong `fd9784242` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/HeldEnvironmentEffectTests.cs:188` | `Badge_ThornsInOneRouteAreIndependentAttacks_AndPreventReturn` | GwonCheonmyeong `a7e2ffd01` / `0d92812d0` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/HighGroundMovementTests.cs:55` | `LethalEntry_StopsAtActualCell` | GwonCheonmyeong `d4a6d6fd8` / `98fd0c642` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/ImpactBoltTests.cs:303` | `MineKillsObjective_StopsLaterTargetsBeforeTheirThorns`<br>`ThornAndMineTraversal_PreservesDeferredFinalArrival` | GwonCheonmyeong `e413008c6` / `0d92812d0` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/JudgmentInteractionTests.cs:52` | `ThornsAndMineAreConsumedOnTheActualForcedPath` | GwonCheonmyeong `1b76f69db` / `1b76f69db` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/KnockbackPathTests.cs:80` | `Heavy_TileForcedMovementUsesOnlyExplicitImmunity`<br>`HeavyTarget_PreviewMatchesCommittedKnockbackAndThorns`<br>`HeavyTarget_TraversesThorns`<br>`LethalThornMidway_InterruptsAtThatCell`<br>`MineDuringKnockback_KillsBoss_StopsRemainingTargetsBeforeThorns`<br>`ThornMidway_DamagesWhilePassing_AndContinues`<br>`TileForcedMovement_PushesNCells_UnlessImmune` | GwonCheonmyeong `9721e138f` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/LeapTraitTests.cs:165` | `NonMineHazard_InterruptsAndRecordsOnlyCompletedPath` | GwonCheonmyeong `f848f1f57` / `cda838f68` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/MovementEnvironmentTests.cs:22` | `ThornEntry_ConsumesAndDamagesInTheMoveCommand`<br>`TileForcedThornContact_DamagesWithoutArmingDeferredPoison` | GwonCheonmyeong `87348b86e` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/NexusReinforcementGeometryTests.cs:101` | `Candidates_ExcludeActualBlockersButNotUnitInfluence` | GwonCheonmyeong `892f4968d` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/ObjectArrivalOrderTests.cs:314` | `Progress_RejectsNextOrdinalThatCanReuseAnExistingEntry` | GwonCheonmyeong `fd9784242` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/OwnedCharacterStatsCalculatorTests.cs:56` | `OwnedThornDescription_ConstructorRejectsTraitPayloadMismatch`<br>`OwnedThornDescription_NoTraitHasNoPayload`<br>`OwnedThornDescription_UsesConfiguredValuesInsteadOfConstants`<br>`OwnedTraitAvailability_MatchesBattleTraitStatus`<br>`QueryMatchesBattleCreationBeforeEnvironment` | GwonCheonmyeong `80a8e184b` / `2fbdac5b6` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/OwnedEnhancementResolutionTests.cs:47` | `OwnedThornDescription_UsesEachResolvedInstance` | GwonCheonmyeong `d48542034` / `2fbdac5b6` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/PlatformMotionTests.cs:78` | `EarlierPlatformDestructionReleasesTheNextPlatformsObstacleOccupancy`<br>`PreludeFearDoesNotReceiveEndCleanupCompensation`<br>`TerminalPreludeKeepsAllPassengerPositionsAndNeverBeginsTurn` | GwonCheonmyeong `0d92812d0` / `8d66c7788` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/PullTerrainTests.cs:40` | `TraversedThorns_ApplyBeforeAttackAndConsumeOnce` | GwonCheonmyeong `4bd6b574f` / `98fd0c642` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/ReconSquadMovementTests.cs:54` | `ArrivalDeath_StopsScanButPreservesStartingGrant` | GwonCheonmyeong `2bd95ad34` / `dc4614886` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/ReconSquadUndoTests.cs:208` | `PullDamage_UsesActualReachedRecipientFacts` | GwonCheonmyeong `2bd95ad34` / `360ab06e9` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/ReconstructionReviveKnockbackTests.cs:26` | `NeighborPushedOntoAThornAndKilled_SelfDestructsAtTheTurnStartEnd`<br>`StageEnd_StopsTheRemainingPushes` | GwonCheonmyeong `0e2a5ee06` / `360ab06e9` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/StaticTerrainIntegrationTests.cs:128` | `Preview_SandstormDefenseClassificationMustRemainCurrent`<br>`SandstormTransit_UsesCurrentCellBeforeDestinationSettlement` | GwonCheonmyeong `fd9784242` / `0e2a5ee06` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/StatusEffectRetirementTests.cs:71` | `Availability_RetainsSurvivorsDeferredFeaturesAndEveryReservedKey` | GwonCheonmyeong `a0ec23c0e` / `de16709b8` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/TargetingPreviewParityTests.cs:197` | `AttackPreview_DoesNotReplaySettledMovementHazards` | GwonCheonmyeong `87348b86e` / `98fd0c642` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/ThornFieldTraitTests.cs:30` | `ApplicationCapability_EmptyBindings_ReturnsNoContribution`<br>`ApplicationCapability_TwoContributors_PreserveValuesInOriginOrder`<br>`ApplicationCells_ExcludeCenter_AreOrdered_AndUseCheckedArithmetic`<br>`DefaultRegistry_RoundTripsDefinitionRuntimeCodecAndStatus`<br>`EvaluateThornFieldApplication`<br>`StaticContracts_RejectNonPositiveValues` | GwonCheonmyeong `3682d7fc8` / `d4a6d6fd8` | 장판 전용 5시험·fixture 폐기 승인 후보 |
| `Assets/Tests/EditMode/ThornHeartReflectionCommandTests.cs:63` | `ReactionForcedArrivalThorns_DoNotReflect`<br>`ReflectionDeath_StopsOffensiveStages_ButPiercingCompletesDirectDelivery`<br>`SurvivingAttacker_ContinuesKnockbackWithoutDeferredThornCreation` | GwonCheonmyeong `ebf26246e` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/TornadoEnvironmentTests.cs:15` | TestCase/규칙 목록/클래스 fixture — 본문 확인 | GwonCheonmyeong `cb3f2ec63` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/TraitDefinitionContractTests.cs:166` | `ExistingIdentityAndAvailability_ArePreserved` | GwonCheonmyeong `a0ec23c0e` / `a0ec23c0e` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/TraitGrowthCatalogTests.cs:31` | `CategoryDefault_CoreValidationRejectsDeferredAssignmentWithoutAnyUnits`<br>`Eligibility_ExcludesDuplicatesUnavailableAndAlreadyEnhancedKeys` | GwonCheonmyeong `9bef9e0b0` / `f672bb337` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/TraitGrowthRulesTests.cs:29` | `ValidatorAndRuntime_UseEquivalentEligibility` | GwonCheonmyeong `d48542034` / `98fd0c642` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/TraitMovementAttackTests.cs:205` | `Death_ClearsHeldOwnerAtExistingCommandCompletion` | GwonCheonmyeong `f848f1f57` / `0e2a5ee06` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/WallAttackEffectsTests.cs:73` | `PersistentThorn_RemainsEnvironmentalOnEntryBehindWall`<br>`ThornInput` | GwonCheonmyeong `fd9784242` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |
| `Assets/Tests/EditMode/WeakGroundStateTests.cs:24` | `TerrainAndEventNumericValues_PreserveExistingCompatibility` | GwonCheonmyeong `2d0509dff` / `fd9784242` | 사건 번호 호환성 유지; 기대값을 줄여 우회 금지 |
| `Assets/Tests/EditMode/WeaknessExposureDamageTests.cs:143` | `SecondaryDamage_UsesReceivingOutcomeForPreviewAndLiveHealth` | GwonCheonmyeong `a0ec23c0e` / `fd9784242` | 장판 케이스/fixture만 검토, 나머지 검증 보존 |

### 4.4 API·복사 인자 보정 후보 56파일

현재 검색된 참조는 빈 `ThornFieldCellSnapshot` 배열/사전 또는 기존 `ThornFields` 전달이다. 제거된 인자·타입을 최소 보정하되 검증 내용·기대 수치·다른 상태의 복사 보장은 유지한다. `CombatCommandTestData` 같은 공용 fixture는 많은 시험이 사용하므로 실제 보정 뒤 사용자 회귀 확인이 필요하다. 수정 중 실행 기대값 변경이 필요해지면 §4.2 범위로 재분류한다.

<details>
<summary>API 보정 후보 전체 목록</summary>

- `Assets/Tests/EditMode/AttackTargetAdmissionTests.cs:353`
- `Assets/Tests/EditMode/BossDeathPathTests.cs:66`
- `Assets/Tests/EditMode/BossObjectiveTests.cs:162`
- `Assets/Tests/EditMode/BossRoarTests.cs:320`
- `Assets/Tests/EditMode/BossTurnTests.cs:105`
- `Assets/Tests/EditMode/CellEnvironmentTestData.cs:18`
- `Assets/Tests/EditMode/CombatCommandCalculationTests.cs:125`
- `Assets/Tests/EditMode/CombatCommandInputCopyTests.cs:204`
- `Assets/Tests/EditMode/CombatCommandTestData.cs:48`
- `Assets/Tests/EditMode/CombatCommonCommandTests.cs:57`
- `Assets/Tests/EditMode/CombatCreationCompositionTests.cs:158`
- `Assets/Tests/EditMode/CombatExperienceSettlementTests.cs:321`
- `Assets/Tests/EditMode/CombatItemCopyTests.cs:69`
- `Assets/Tests/EditMode/CombatItemEquipmentTests.cs:106`
- `Assets/Tests/EditMode/CombatItemLifetimeTests.cs:82`
- `Assets/Tests/EditMode/CombatKillRewardTests.cs:80`
- `Assets/Tests/EditMode/CombatRewardConsistencyTests.cs:223`
- `Assets/Tests/EditMode/CombatRewardParticipationTests.cs:80`
- `Assets/Tests/EditMode/CombatStageRecordTests.cs:95`
- `Assets/Tests/EditMode/CombatSummonTests.cs:142`
- `Assets/Tests/EditMode/DeadHandSacrificeTests.cs:96`
- `Assets/Tests/EditMode/DistanceDamageScaleTraitTests.cs:179`
- `Assets/Tests/EditMode/EnemyEngagementProgressTests.cs:383`
- `Assets/Tests/EditMode/EnemyPursuitEquivalenceTests.cs:236`
- `Assets/Tests/EditMode/EnemyRedeploymentResolverTests.cs:87`
- `Assets/Tests/EditMode/EnemyTraitTrailAssessmentRegressionTests.cs:106`
- `Assets/Tests/EditMode/EnhancementActionCostTests.cs:91`
- `Assets/Tests/EditMode/EnvironmentTerrainRuleTests.cs:136`
- `Assets/Tests/EditMode/ExperienceAttributionPathTests.cs:32`
- `Assets/Tests/EditMode/HaboobGimmickTests.cs:128`
- `Assets/Tests/EditMode/HighGroundWallBypassTests.cs:225`
- `Assets/Tests/EditMode/HuntersMarkStateTests.cs:124`
- `Assets/Tests/EditMode/IntimidationAttackTests.cs:200`
- `Assets/Tests/EditMode/IntimidationScheduleTests.cs:88`
- `Assets/Tests/EditMode/IntimidationStatePreservationTests.cs:116`
- `Assets/Tests/EditMode/KillChainAttackTraitTests.cs:400`
- `Assets/Tests/EditMode/MineEnvironmentTests.cs:278`
- `Assets/Tests/EditMode/MovementUndoBarrierTests.cs:34`
- `Assets/Tests/EditMode/MovementUndoRulesTests.cs:23`
- `Assets/Tests/EditMode/NexusReinforcementRewardTests.cs:76`
- `Assets/Tests/EditMode/ObstacleReactionTests.cs:85`
- `Assets/Tests/EditMode/PeriodicStatusTurnTests.cs:61`
- `Assets/Tests/EditMode/PiercingShotCommandTests.cs:214`
- `Assets/Tests/EditMode/PositionTileModifierTests.cs:176`
- `Assets/Tests/EditMode/PullTestData.cs:42`
- `Assets/Tests/EditMode/RailwayGimmickTests.cs:72`
- `Assets/Tests/EditMode/SelfDestructTraitTests.cs:96`
- `Assets/Tests/EditMode/ThornHeartReflectionDirectTests.cs:179`
- `Assets/Tests/EditMode/TornadoGimmickResolutionTests.cs:276`
- `Assets/Tests/EditMode/TraitStateContractTests.cs:109`
- `Assets/Tests/EditMode/UnitAttackRangeQueryTests.cs:66`
- `Assets/Tests/EditMode/WallAttackAdmissionTests.cs:264`
- `Assets/Tests/EditMode/WallMovementTests.cs:105`
- `Assets/Tests/EditMode/WallStateLifecycleTests.cs:127`
- `Assets/Tests/EditMode/WeakGroundMovementTests.cs:135`
- `Assets/Tests/EditMode/WeaknessExposureLifecycleTests.cs:63`

</details>

## 5. Phase 경계와 구조 평가

- **Phase 0 완료 범위:** 현재 기준·권한·키 충돌 여부 확인, 삭제/보존 후보와 GUID 사용처 목록, 실제 시험 행 작성자 및 변경 성격 분류, 후속 Phase의 승인 필요 항목 기록.
- **Phase 1 전 준비 항목:** 사용자의 다음 Phase 지시와 그 범위 확인. `ContractShapeGuardTests`의 공개 속성 기대 목록 변경은 별도 시험 제한을 따르며, 일반 “진행해”를 이 제한의 예외로 간주하지 않는다.
- **Phase 3 전 준비 항목:** 위 전용/혼합 시험의 실제 삭제·대체·기대값 변경 범위를 사용자가 확인한 뒤 수행. 코드 삭제 허용을 시험 기대값 변경의 포괄 승인으로 간주하지 않는다.
- **수정 범위:** 초기 장판 삭제의 넓은 호출/상태 이전은 실제 계약 연결에 따른 비용이다. 현재 조사에서 발견한 파일 전체의 일반 리팩터링은 포함하지 않는다.
- **이해 범위:** 이후 폭탄 변경자는 전용 부착/연쇄 규칙과 공용 피해·이동·게시 계약을 이해하면 되도록 유지한다. 발리스타 전용 공격 방식이나 다른 기믹 내부를 새 폭탄 규칙에 끌어들이지 않는다.
- **검증 범위:** 공통 넉백·미리보기·상태 복사/undo·DB 등록·View 해제는 실제 공유 경계이므로 인수 항목을 유지한다. 공유 아이콘/공통 이동 시험의 삭제는 불필요한 회귀를 만들므로 제외했다.
- **SSOT/SOLID/FW:** 상태 원본/표시 복사 경계를 변경하지 않았고 신규 서비스/일반 이벤트 버스/전역 특성 분기를 만들지 않았다. 실제 구현의 준수 여부는 각 구현 Phase에서 확인하며 이번 조사를 성능/실행 검증으로 주장하지 않는다.

## 6. 검증과 실행 기록

- 검색/메타/역참조/git 이력을 읽었으며 삭제나 제품 파일 쓰기는 없었다. 전용 후보 22개의 `.meta`가 존재한다.
- 문서의 표와 계획 진행 상태, 상대 링크, UTF-8/LF 및 공백을 확인한다. 코드/에셋/시험 변경은 Phase 0에 없다.
- Python 실행 별칭은 사용할 수 없어 PowerShell/rg/git으로 조사했다. 외부 패키지를 설치하지 않았다.
- 작업 중 임시 자료는 git-ignore된 `.superpowers/sdd/2026-11-17-cactus-cannon-thorn-bomb-implementation-plan/`에 두었다. 이 보고서가 인계용 기록이며 임시 자료가 없어도 주요 결정을 확인할 수 있다.
- **컴파일 미확인. 테스트 미실행. Phase 1로 진행하지 않고 중단한다.**
