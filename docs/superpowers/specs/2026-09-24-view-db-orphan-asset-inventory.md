# DataBaseSO 고아 에셋 인벤토리

작성일: 2026-09-24 · 기준 브랜치: `develop` · 목적: DB-SO 재구성 작업 전 기존 SO 에셋 현황 보존 기록 · 재구성 설계: [설계](2026-09-24-db-so-ingame-view-group-design.md)

> **⚠ 삭제 금지.** 이 문서의 에셋은 스크립트가 없어 현재 읽을 수 없지만, 값 참고와 Core 작업자 협의를 위해 모두 보존한다.
> 삭제·이동·재직렬화는 담당자 합의 후 별도로 진행한다.

## 1. 요약

| 구분 | 개수 | 상태 |
|---|---|---|
| `Assets/ProjectL/DataBaseSO` 전체 에셋 | 55 | |
| 정상 (스크립트 존재) | 7 | DatabaseCatalogSO, RenderGroup, EffectDataTable, TestEffect, LightDataTableSO, PointLight, SpotLight |
| **고아 (스크립트 삭제됨)** | **48** | 이 문서의 대상 |

- 원인: 커밋 `a18d86d` (JMH, 2026-09-22, "[민형] 폴더 구조 정리")에서 `Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/**` 스크립트 16개가 삭제됨. 에셋과 `.meta`는 남아 Missing Script 상태.
- 이전 대화에서 고아 에셋을 47개로 집계했으나 재집계 결과 48개가 맞다.
- 빈 폴더 `DataBaseSO/StageGroup/StageTable`이 있다. 에셋은 없다.

## 2. 처리 방침

| 묶음 | 에셋 수 | 성격 | 담당 | 처리 |
|---|---|---|---|---|
| FieldPrefab, StageSceneViewConfig | 2 | View | View(본 작업) | ViewGroup `StageFieldViewTable`로 값 통합 (설계 §6.2) |
| StageMapViewConfig | 1 | View | View(본 작업) | ViewGroup `StageMapViewTable`로 값 복구 (설계 §6.3) |
| StageMapVisualCatalog | 1 | View | View(본 작업) | ViewGroup `StageNodeVisualTable`로 값 복구 (설계 §6.4) |
| UnitVisualCatalog | 1 | View | View(본 작업) | ViewGroup `UnitVisualTable`로 값 복구, Unit displayName 포함 (설계 §6.6) |
| FieldCamera | 1 | View | **다른 작업자** | SO로 만들지 않음. 값만 기록 |
| Architecture (Catalog + 정의 5) | 6 | InGame + View 혼합 | **다른 View 작업자** / Core | 그대로 둠 |
| Field_7x6 ~ Field_9x9 | 12 | InGame (+ View 참조 1필드) | View 작업 (D7) | InGameGroup `FieldTable`로 복구 (설계 §5.4) |
| StageTable (Normal A~J, Boss) | 11 | InGame (+ View 1필드) | View 작업 (D7) | InGameGroup `StageTable` + CombatStageData로 복구 (설계 §5.3) |
| StageMapChapter1 | 1 | InGame | View 작업 (D7) | InGameGroup `StageMapTable`로 복구 (설계 §5.2) |
| Unit / Structure 정의 | 5 | InGame (+ displayName) | View 작업 (D7) | InGameGroup `UnitTable`/`StructureTable`로 복구, displayName은 ViewGroup으로 분리 (설계 §5.5, §5.6) |
| Enemy/Player Loadout, UnitRoster | 7 | InGame | View 작업 (D7) | InGameGroup Loadout/Roster 테이블로 복구 (설계 §5.7) |

복구하는 에셋도 원본은 삭제하지 않는다. 값만 새 구조의 새 에셋으로 옮긴다.

### 데이터 경계 원칙

- **InGameGroup(규칙)은 Core가 소유한다.** View는 InGameGroup을 참조하지 않는다.
- 필드 크기, HQ 위치, 유닛 스탯처럼 View도 필요로 하는 규칙 값은 Core가 계산해 쓴 **Buffer(ReadSink)로 받는다.** DB 값은 시작값일 뿐이고 실제 상태는 Core가 바꾸기 때문이다.
- InGameGroup Data는 순수 C#이다(Unity 타입 금지). SO는 기획 입력 계층이고 `GetData()`가 순수 Data를 만든다.
- **ViewGroup(표현)은 View가 소유한다.** 키는 Buffer에 있는 값(`EntityKey`, 노드의 StageType/MapRole 등)만 사용한다.
- 셀 크기와 간격은 별도 필드 없이 cellPrefab 크기로 계산한다.
- 예외: `StageViewPropTable`은 View 데이터지만 InGameGroup에 있다. 이동 여부를 확인해야 한다(§7-1).

Core 작업자 전달 사항은 [View ↔ Core 데이터 경계 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 정리했다.

## 3. 삭제된 원본 스크립트

모든 스크립트의 원래 경로는 `Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/` 아래다. 원래 네임스페이스는 `ProjectL.Unity.Data.ScriptableObjects.*`, 어셈블리는 `ProjectL.Unity`였다.
내용은 `git show a18d86d^:Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/<경로>`로 볼 수 있다.

| 스크립트 (하위 경로) | Script GUID | 최초 작성 | 사용 에셋 수 |
|---|---|---|---|
| Architecture/ArchitectureCatalogSO.cs | 3e24b9b60770494ca439e29f6eed5d7e | Leehojin 09-08 | 1 |
| Architecture/ArchitectureDefinitionSO.cs | 4628d787c1534c7ca801822299a0ac1d | Leehojin 09-08 | 5 |
| Field/FieldCameraSO.cs | 137e749099c489749aa69cf1ee5ae13d | Leehojin 08-08 | 1 |
| Field/FieldVisualSO.cs | 2b81e53b55b67e641881fbe76375ad33 | Leehojin 08-08 | 1 |
| Field/FieldDefinitionSO.cs | 9bb635593b3431b41a98810cedec1de8 | Leehojin 08-08 | 12 |
| Stage/StageSceneViewConfigSO.cs | 057ffbb171ee4905ba3d217a0baae607 | Leehojin 08-29 | 1 |
| Stage/StageDefinitionSO.cs | 84b252bc197279642ada9b42c3294990 | Leehojin 09-04 | 11 |
| StageMap/StageMapDefinitionSO.cs | b0caa440360593b4c8de8ed7df98d038 | Leehojin 09-06 | 1 |
| StageMap/StageMapViewConfigSO.cs | 5733a7a0dd1f78a4fac8f6856c81b395 | Leehojin 09-06 | 1 |
| StageMap/StageMapVisualCatalogSO.cs | 65f6122688e3aed44979c2f058958cb2 | Leehojin 09-06 | 1 |
| Unit/UnitDefinitionSO.cs | 00c55a1185cd6144b9c711995f9566fe | Leehojin 08-10 | 4 |
| Unit/PlayerLoadoutSO.cs | a6e6502dbd786784ca9683803e45f743 | Leehojin 08-10 | 1 |
| Unit/UnitRosterSO.cs | 16aaecc49515b174a9f2b44245cfabe7 | Leehojin 08-10 | 1 |
| Unit/UnitVisualCatalogSO.cs | d9e518ae643d4ce9b508abfd53e823b4 | Leehojin 08-12 | 1 |
| Unit/EnemyLoadoutSO.cs | 36ca0613551012c438148d6b05a03c3d | choi yong geun 08-23 | 5 |
| Structure/StructureDefinitionSO.cs | 7e8a6208eb74d6a4b88772f1b854539e | choi yong geun 09-07 | 1 |

## 4. 에셋 상세

표기: **[I]** InGame(규칙) 필드 · **[V]** View(표현) 필드. 기준은 "값이 바뀌면 게임 결과가 바뀌는가(I), 보이는 모습만 바뀌는가(V)"다.
경로는 모두 `Assets/ProjectL/` 기준이다.

### 4.1 Field / 스테이지 연출 설정 (`DataBaseSO/Stage/`)

#### FieldCamera.asset — FieldCameraSO · 담당: 다른 작업자
| 필드 | 값 | 분류 |
|---|---|---|
| _pitch | 45 | V |
| _yawRelativeToField | -45 | V |
| _fieldOfView | 45 | V |
| _safeViewport | (x 0.125, y 0.125, w 0.75, h 0.75) | V |
| _fieldPadding | 1 | V |
| _minimumDistance / _maximumDistance | 10 / 500 | V |

원본 검증 규칙: 0 < pitch < 90, 0 < FOV < 180, viewport는 0~1 범위 안의 양수 크기, padding ≥ 0, min > 0, max ≥ min.

#### FieldTable/FieldPrefab.asset — FieldVisualSO · → 설계 §6.2
| 필드 | 값 | 분류 |
|---|---|---|
| _cellPrefab | `Resources/Test/Stage/Board/Cube.prefab` | V |

원본 검증 규칙: prefab non-null, 자식 포함 Renderer 존재.

#### StageSceneViewConfig.asset — StageSceneViewConfigSO · → 설계 §6.2
| 필드 | 값 | 분류 |
|---|---|---|
| _handUnitLayerMask | 256 (Layer 8) | V |
| _ghostMaterial | **null** (미연결) | V |
| _validGhostTint | (0.45, 1, 0.55, 0.5) | V |
| _invalidGhostTint | (1, 0.4, 0.4, 0.5) | V |
| _ghostWorldOffset | (0, 0.05, 0) | V |
| _ghostScale | 0.5 | V |
| _fieldUnitScale | 0.5 | V |

#### FieldTable/Field_{7,8,9}/Field_CxR.asset (12개) — FieldDefinitionSO · 담당: Core
12개 모두 공통값: `_reserveCapacity 4`, `_playerDeploymentDepth 2`, `_enemyDeploymentDepth 2`, `_fieldVisual → FieldPrefab.asset`.

| 에셋 | _fieldId [I] | _columnCount [I] | _rowCount [I] | 사용 스테이지 |
|---|---|---|---|---|
| Field_7x6 | field_7x6 | 7 | 6 | Normal_A |
| Field_7x7 | field_7x7 | 7 | 7 | Normal_C |
| Field_7x8 | field_7x8 | 7 | 8 | Normal_B |
| Field_7x9 | field_7x9 | 7 | 9 | Normal_D |
| Field_8x6 | field_8x6 | 8 | 6 | **미사용** |
| Field_8x7 | field_8x7 | 8 | 7 | Normal_E |
| Field_8x8 | field_8x8 | 8 | 8 | Normal_J |
| Field_8x9 | field_8x9 | 8 | 9 | Normal_F |
| Field_9x6 | field_9x6 | 9 | 6 | Normal_G |
| Field_9x7 | field_9x7 | 9 | 7 | Normal_I |
| Field_9x8 | field_9x8 | 9 | 8 | Normal_H |
| Field_9x9 | field_9x9 | 9 | 9 | Boss |

분류: `_reserveCapacity`, `_*DeploymentDepth`는 [I]. `_fieldVisual`은 [V] 참조이며 ViewGroup StageFieldViewTable의 cellPrefab으로 대체된다.

### 4.2 스테이지 / 맵

#### StageTable/*.asset (11개) — StageDefinitionSO · 담당: Core
공통값 (11개 모두):
- `_unitRoster → UnitRoster`, `_headquarters → Headquarters`
- `_initialEnemyPlacements []`, `_structureCatalog []`, `_initialEnemyStructurePlacements []`
- 양측 HQ 회전 R0
- `_railwayEnabled 0`, `_railway { railCells [], firstActivation 3, repeatInterval 3 }`

| 에셋 | _stageKey | _timelineSeed | _fieldDefinition | _enemyLoadout | Player HQ | Enemy HQ | _viewStageType [V] |
|---|---|---|---|---|---|---|---|
| Stage_Nomal_A ※ | 1101 | 1 | Field_7x6 | Enemy_1 | (3,0) | (3,5) | 미기록 → NomalStage(0) |
| Stage_Normal_B | 1102 | 1 | Field_7x8 | Enemy_2 | (3,0) | (3,7) | 미기록 |
| Stage_Normal_C | 1103 | 1 | Field_7x7 | Enemy_3 | (3,0) | (3,6) | 미기록 |
| Stage_Normal_D | 1104 | 1 | Field_7x9 | Enemy_4 | (3,0) | (3,8) | 미기록 |
| Stage_Normal_E | 1105 | 1 | Field_8x7 | Enemy_3 | (4,0) | (4,6) | 미기록 |
| Stage_Normal_F | 1106 | 1 | Field_8x9 | Enemy_4 | (4,0) | (4,8) | 미기록 |
| Stage_Normal_G | 1107 | 1 | Field_9x6 | Enemy_3 | (4,0) | (4,5) | 미기록 |
| Stage_Normal_H | 1108 | 1 | Field_9x8 | Enemy_4 | (4,0) | (4,7) | 미기록 |
| Stage_Normal_I | 1109 | 1 | Field_9x7 | Enemy_3 | (4,0) | (4,6) | 미기록 |
| Stage_Normal_J | 1110 | 1 | Field_8x8 | Enemy_4 | (4,0) | (4,7) | 미기록 |
| StageDefinition_Boss | 2101 | 2 | Field_9x9 | Enemy_5 | (4,0) | (4,8) | **3 (BossStage)** |

※ 파일명 오타 `Nomal`은 원본 그대로다.
`_viewStageType`만 [V] 필드이고 나머지는 모두 [I]다. 이 필드는 폐기 예정이다. 노드 외형 키는 Core가 Buffer(`StageProgressData`)에 함께 쓰는 `StageType` + `StageMapRole`로 대체한다(설계 §6.4, §7 B1).

#### StageSelect/StageMapChapter1.asset — StageMapDefinitionSO · 담당: Core
| 필드 | 값 | 분류 |
|---|---|---|
| _depthCount | 7 | I |
| _widthMax | 3 | I |
| _additionalConnectionChance | 0.25 | I |
| _seed / _randomizeSeedEachRun | 192 / true | I |
| _regularStages | Normal_A ~ Normal_J (10개) | I |
| _bossStage | StageDefinition_Boss | I |

현재 코드의 `StageMapData`(mapKey, depthCount, widthMax, additionalConnectionChance, connectionAlgorithm, 연결 가중치, seed, randomize)와 대응한다. 스테이지 목록은 현재 `StageRootData.mapKey` 역참조 방식으로 바뀌었다.

#### StageSelect/StageMapViewConfig.asset — StageMapViewConfigSO · → 설계 §6.3
| 필드 | 값 | 분류 |
|---|---|---|
| _depthSpacing / _widthSpacing | 4 / 4 | V |
| _nodeScale | 1 | V |
| _lineHeight / _lineWidth | 0.05 / 0.12 | V |
| _currentColor | (0.15, 0.8, 1, 1) | V |
| _selectableColor | (1, 1, 1, 1) | V |
| _hoveredColor | (1, 0.9, 0.4, 1) | V |
| _candidateColor | (0.3, 1, 0.5, 1) | V |
| _unavailableColor | (0.35, 0.35, 0.35, 1) | V |
| _nodePrefab | `Prefabs/StageSelect/Infrastructure/StageMapNode.prefab` (원래 타입 `StageMapNodeView`, 스크립트 삭제됨) | V |
| _connectionPrefab | `Prefabs/StageSelect/Infrastructure/StageMapConnection.prefab` (LineRenderer) | V |
| _startVisualPrefab | null (선택 항목) | V |
| _currentMarkerPrefab | `Prefabs/StageSelect/Visuals/StageVisual_PlayerPlaceholder.prefab` | V |
| _nodeMask | 1 (Default) | V |
| _pickDistance | 1000 | V |

원본 검증 규칙: 간격·스케일·선 두께는 양수 유한값, 색상은 유한값, node/connection/marker prefab은 필수이고 씬 인스턴스가 아닌 프리팹 에셋이어야 함, nodeMask는 노드 HitCollider 레이어 포함, connection에 material 필수, marker에 Renderer 필수, pickDistance는 양수.

#### StageSelect/StageMapVisualCatalog.asset — StageMapVisualCatalogSO · → 설계 §6.4
| _stageType (옛 StageEnum) | _visualPrefab | _icon | _displayName |
|---|---|---|---|
| 0 NomalStage | `Prefabs/StageSelect/Visuals/StageVisual_NormalPlaceholder.prefab` | null | Normal Stage A |
| 3 BossStage | `Prefabs/StageSelect/Visuals/StageVisual_BossPlaceholder.prefab` | null | Boss Stage |

모두 [V]. 옛 키 `StageEnum`(NomalStage/StoreStage/ApicStage/BossStage)은 현재 `InGameEnum.cs`에 정의만 남아 있고 사용처가 없다.

### 4.3 유닛 / 구조물 (`DataBaseSO/Stage/UnitTable/`)

#### Unit 정의 (4개) — UnitDefinitionSO · 담당: Core
| 필드 | Akimbo | ShotGun | Sniper | Demon | 분류 |
|---|---|---|---|---|---|
| _key (EntityKey) | 10 Unit_Akimbo | 11 Unit_ShotGun | 12 Unit_Sniper | 13 Unit_Demon | I |
| _displayName | Akimbo | **ShouGun** (오타) | Sniper | Demon | **V** |
| _ownerEligibility | PlayerOnly | 미기록 → PlayerOnly | PlayerOnly | EnemyOnly | I |
| _footprint W×H | 1×1 | 1×1 | 1×1 | 1×1 | I |
| _maxHealth | 10 | 10 | 10 | 5 | I |
| _moveRange / _moveCost | 5 / 1 | 3 / 1 | 3 / 1 | 3 / 1 | I |
| _appointMethod | RadialSquare(6) | ForwardCone(8) | RadialSquare(6) | RadialSquare(6) | I |
| _attackRange | 1 | 2 | 4 | 1 | I |
| _directionalAttackPattern | [] | (-2,2)(-1,2)(0,2)(1,2)(2,2)(0,1) | (0,0) | [] | I |
| _attackTargetMode | SingleTarget(1) | AllHostilesInArea(2) | SingleTarget(1) | SingleTarget(1) | I |
| _attackPower | 1 | 1 | 1 | 1 | I |
| _maxActionCost / _attackCost | 5 / 2 | 3 / 1 | 3 / 1 | 3 / 1 | I |
| _priority / _delay | 10 / 10 | 10 / 10 | 10 / 10 | 10 / 10 | I |
| _traits (SerializeReference) | MoveAttackDiscount (discount 1, minCost 1) | 없음 | Focus (damagePerStack 1, maxStacks 3) | 없음 | I |

- 경로: `Player/Unit/{Akimbo,ShotGun,Sniper}.asset`, `Enemy/Unit/Demon.asset`.
- Trait의 SerializeReference 타입은 `ProjectL.Unity.Data.ScriptableObjects.Unit.*TraitAuthoring` (asm `ProjectL.Unity`)이다. 이 타입도 삭제되어, 스크립트를 복구하더라도 타입 이름을 맞추지 않으면 trait 값이 유실된다.

#### Structure/Headquarters.asset — StructureDefinitionSO · 담당: Core
| 필드 | 값 | 분류 |
|---|---|---|
| _key | 20 Structure_Headquarters | I |
| _displayName | base | **V** |
| _footprint W×H | 1×1 | I |
| _maxHealth | 3 | I |
| _isAttackable / _attackPower | false / 0 | I |

#### Enemy/Chapter1_Enemy_{1..5}.asset — EnemyLoadoutSO · 담당: Core
모든 에셋의 entries는 `Demon (weight 1)` 1개뿐이다.

| 에셋 | _spawnCount | _randomSeed | 사용 스테이지 |
|---|---|---|---|
| Enemy_1 | 2 | 1 | A |
| Enemy_2 | 4 | 2 | B |
| Enemy_3 | 6 | 1 | C, E, G, I |
| Enemy_4 | 5 | 2 | D, F, H, J |
| Enemy_5 | 8 | 4 | Boss |

#### Player/PlayerLoadout.asset — PlayerLoadoutSO · 담당: Core
| 필드 | 값 |
|---|---|
| _entries | Akimbo ×2, ShotGun ×1, Sniper ×1 |
| _startingHandSize | 4 |
| _randomSeed | 12345 |

StageDefinition에서 참조하지 않는다. 다른 경로(씬 등)에서 쓰였을 수 있다.

#### UnitRoster.asset — UnitRosterSO · 담당: Core
`_unitDefinitions`: Akimbo, ShotGun, Sniper, Demon. 스테이지 11개가 모두 참조한다.

#### Visual/UnitVisualCatalog.asset — UnitVisualCatalogSO · → 설계 §6.6
| _key | _prefab |
|---|---|
| 10 Unit_Akimbo | `Prefabs/Stage/Units/StageUnit_Akimbo.prefab` |
| 11 Unit_ShotGun | `Prefabs/Stage/Units/StageUnit_ShotGun.prefab` |
| 12 Unit_Sniper | `Prefabs/Stage/Units/StageUnit_Sniper.prefab` |
| 13 Unit_Demon | `Resources/Test/Model/Prefab/TestUnit_Base.prefab` |
| 20 Structure_Headquarters | `Prefabs/Default/Cube.prefab` |

현재 `EntityKey`에 있지만 항목이 없는 키: Unit_MainCharacter(14), Unit_Ballista(15), Unit_CactusCannon(16), Unit_Gravekeeper(17).

### 4.4 건축물 (`DataBaseSO/Architecture/`) · 담당: 다른 View 작업자 / Core

#### ArchitectureCatalog.asset — ArchitectureCatalogSO
| 필드 | 값 | 분류 |
|---|---|---|
| _definitions | O, T, L, S, Z (5개) | I |
| _boardCellPrefab | `Resources/Test/Stage/Board/Cube.prefab` | V |
| _boardCellSpacing | 2.2 | V (원본 "Board Visual" 헤더 소속. 배치 판정에 쓰였다면 I) |
| _boardCellVisualScale | 2 | V |

#### Definitions/Architecture_{O,T,L,S,Z}.asset — ArchitectureDefinitionSO
모든 정의의 `_initialCount`는 1이고 `_buildingPrefab`[V]은 null이다.

| 에셋 | _architectureId [I] | _cells [I] (x,y) |
|---|---|---|
| Architecture_O | 1 | (0,0)(1,0)(0,1)(1,1) |
| Architecture_T | 2 | (0,0)(1,0)(2,0)(1,1) |
| Architecture_L | 3 | (0,0)(1,0)(0,1)(0,2) |
| Architecture_S | 4 | (1,0)(2,0)(0,1)(1,1) |
| Architecture_Z | 5 | (0,0)(1,0)(1,1)(2,1) |

## 5. 에셋 참조 관계

```text
StageMapChapter1 ──regular──► Stage_Normal_A..J ─┬─► Field_* ──► FieldPrefab ──► Cube.prefab
                 └──boss────► StageDefinition_Boss┤
                                                  ├─► UnitRoster ──► Akimbo, ShotGun, Sniper, Demon
                                                  ├─► Chapter1_Enemy_1..5 ──► Demon
                                                  └─► Headquarters
PlayerLoadout ──► Akimbo, ShotGun, Sniper                 (스테이지 미참조)
ArchitectureCatalog ──► Architecture_O/T/L/S/Z, Cube.prefab
UnitVisualCatalog ──► 유닛 프리팹 5개 (EntityKey 매칭, 에셋 직접 참조 아님)
```

참조된 프리팹 11개는 모두 현재 경로에 존재한다(2026-09-24 확인).

## 6. Core 데이터에 섞인 View 필드

Core 작업자와 협의할 항목이다. ViewGroup이 생기면 이 필드들은 Core 데이터에서 빠질 수 있다.

| 위치 | 필드 | 제안 |
|---|---|---|
| StageDefinitionSO | _viewStageType | 폐기. View가 Buffer의 `StageType` + `StageMapRole`로 판단 (설계 §6.4, §7) |
| FieldDefinitionSO | _fieldVisual | 폐기. ViewGroup StageFieldViewTable.cellPrefab 사용 (설계 §6.2) |
| UnitDefinitionSO / StructureDefinitionSO | _displayName | ViewGroup UnitVisualTable로 이동 (설계 §6.6) |
| ArchitectureCatalogSO | _boardCellPrefab, _boardCellSpacing, _boardCellVisualScale | 다른 View 작업자와 결정 |
| ArchitectureDefinitionSO | _buildingPrefab | 다른 View 작업자와 결정 |

## 7. 확인 필요 사항

| # | 항목 | 비고 |
|---|---|---|
| 1 | **StageViewPropTable ViewGroup 이동 여부** | 현재 InGameGroup 소속(StageKey → GameObject). View 데이터 성격이지만 보류. JMH 확인 필요 |
| 2 | InGameGroup 에셋 부재 | `DataBaseFactory.Create`에서 예외 발생. 재구성 작업에서 InGameGroup 에셋을 복구 값으로 생성 (설계 §5) |
| 3 | DB 그룹 이름 대칭 | DB `InGameGroup`(규칙)과 런타임 `InGameSystemGroup`(Core+View)의 의미가 다름. JMH와 논의 |
| 4 | FieldCamera 담당 | 다른 작업자. 값은 4.1 참고 |
| 5 | Architecture 담당 | 다른 View 작업자. 값은 4.4 참고 |
| 6 | Trait 데이터 유실 위험 | Akimbo/Sniper trait는 SerializeReference 타입명에 묶여 있음 (4.3) |
| 7 | 데이터 오타 | ShotGun displayName `ShouGun`, 파일명 `Stage_Nomal_A`, enum `NomalStage`/`ApicStage` |
| 8 | 미연결 값 | StageSceneViewConfig._ghostMaterial null, Architecture._buildingPrefab 전부 null, Field_8x6 미사용 |
| 9 | 사용되지 않는 enum | `InGameEnum.cs`의 `StageEnum`은 정의만 있고 사용처 없음 |
| 10 | 빈 폴더 | `DataBaseSO/StageGroup/StageTable` |
| 11 | Core 공유 사항 | 데이터 경계, 기존 코드 수정, Buffer 변경. [전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) 참고 |
