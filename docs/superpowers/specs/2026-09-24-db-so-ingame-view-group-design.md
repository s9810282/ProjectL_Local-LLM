# DB-SO 재구성 설계 — InGameGroup / ViewGroup

- 작성일: 2026-09-24
- 작성: View 작업
- 기준 브랜치: `develop`
- 상태: **구현 완료, 에디터 확인 대기** (2026-09-25, Phase A~F. 구현 결과와 설계와 달라진 점은 §14)
- 관련 문서
  - [DataBaseSO 고아 에셋 인벤토리](2026-09-24-view-db-orphan-asset-inventory.md): 옛 에셋의 모든 필드 값. 복구 값의 출처
  - [구현 계획](../plans/2026-09-24-db-so-restructure-plan.md): Task T0~T20
  - [ProjectL Framework / Systems 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md): FW 계약
  - [ProjectL Framework / Systems 구조](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md)

## 1. 목적과 범위

`a18d86d`(2026-09-22 폴더 구조 정리)에서 옛 SO 스크립트 16개가 삭제되어 DataBaseSO 에셋 48개가 Missing Script 상태가 되었다. 이 설계는 **옛 SO 데이터를 새 Framework/Systems 구조에 맞게 복구**하는 방법을 정한다.

- **복구가 우선이다.** 옛 SO 구성과 값을 최대한 그대로 옮긴다.
- **§3의 규칙에 어긋나는 경우에만** 필드를 분리·삭제·변환한다.
- InGameGroup과 ViewGroup 모두 이번 작업에서 직접 구현한다.
- 옛 에셋 48개는 삭제하지 않는다. 새 에셋은 새 경로에 만든다.

범위 밖:

| 항목 | 담당 |
|---|---|
| FieldCamera | 다른 작업자 |
| Architecture (Catalog + 정의 5) | 다른 View 작업자 |
| RenderGroup (Effect, Light, Material) | 기존 유지 |
| Core 시스템 로직, Buffer 전체 설계 | Core 작업. 단, View에 필요한 Buffer 필드는 §7에서 정의 |

## 2. 전체 구조

새 Core는 기존 `CoreCaculator`(Contracts/Snapshot)가 아니라 새 Framework/Systems 위에서 시스템으로 동작한다.

```text
InGameGroup (규칙) ──InitData──► Core 시스템 (InGameCoreSystemGroup)
                                   │  Table을 바로 참조한다 (Spec 변환 없음)
                                   ▼ WriteSink
                                 Buffer (런타임 상태)
                                   │ ReadSink
ViewGroup (표현) ──InitData──►  View 시스템 / UI  = Buffer 상태 + 표현 데이터
```

| 구분 | InGameGroup | ViewGroup | Buffer |
|---|---|---|---|
| 담는 것 | 게임 규칙 (값이 바뀌면 게임 결과가 바뀜) | 표현 (값이 바뀌면 보이는 모습만 바뀜) | 런타임 상태 |
| 쓰는 쪽 | Core 시스템 | View 시스템 / UI | Core가 쓰고 View가 읽음 |
| 허용 타입 | **순수 C#만** | Unity 타입 허용 | 순수 C# |
| View 참조 | ❌ 금지 | ✅ | ✅ ReadSink |

예전에는 View(`ProjectL.Unity`의 `FieldDefinitionAdapter`, `StageEntryDataBuilder`)가 SO를 Core Spec으로 바꿔 넣었다. 새 구조에서는 **View가 Core에 데이터를 넣지 않는다.**

## 3. 설계 규칙

| # | 규칙 |
|---|---|
| **P1** | **InGame Data는 순수 C#만 사용한다.** 허용: `int/float/bool/string`, enum, 직접 정의한 struct, 이들의 배열·읽기 전용 목록. 금지: `GameObject`, `Vector2Int`, `Color`, `Rect`, `LayerMask`, `Material`, ScriptableObject 등 UnityEngine 타입 |
| **P2** | **SO는 입력(Authoring) 계층이다.** 기획이 값을 바꾸는 곳은 SO다. Item SO가 직렬화 필드를 갖고, `GetData()`가 순수 C# 불변 Data를 새로 만들어 반환한다. `[SerializeField]`, `[Min]`, `[Range]`, `[Header]`, `[CreateAssetMenu]` 같은 Unity 속성은 SO 쪽에만 둔다. |
| **P3** | **SO끼리의 참조는 입력 계층에서만 허용한다.** 기획 편의(드래그 앤 드롭)를 위해 SO가 다른 SO를 참조할 수 있다. `GetData()`에서 참조 대상의 **키**로 바꾼다. Core가 받는 Data에는 SO 참조가 없다. |
| **P4** | Core와 View가 같이 쓰는 키와 값 타입은 순수 C#이어야 한다. **옛 Core.Contracts에 이미 있는 타입은 그대로 재사용**하고, 없는 타입만 `DataBaseSystem/Data/Common/`에 둔다(§4). |
| **P5** | ViewGroup은 Unity 타입을 쓸 수 있다. 조회 키는 InGame과 같은 키를 쓴다(§5). |
| **P6** | View가 규칙에서 나온 값이 필요하면 InGameGroup을 읽지 않고 **Buffer ReadSink로 받는다.** 필요한 Buffer 필드는 우리가 정의한다(§7). |
| **P7** | 다른 값에서 계산할 수 있는 표현 필드는 분리하지 않고 삭제한다(예: `_viewStageType`). |

### 3.1 P2 코드 형태

InGame 테이블 하나는 파일 두 개로 나눈다. 순수 C# 파일에는 `using UnityEngine`이 없어야 한다.

```csharp
// FieldData.cs — 순수 C#. UnityEngine 참조 없음
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item
{
    /// <summary>필드 한 종류의 크기와 배치 규칙입니다.</summary>
    public sealed class FieldData : IData
    {
        public string FieldId { get; }
        public int ColumnCount { get; }
        // ...

        public FieldData(string fieldId, int columnCount, /* ... */)
        {
            FieldId = fieldId;
            ColumnCount = columnCount;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FieldId))
                throw new InvalidOperationException("FieldId is required.");
            // ...
        }
    }
}

// FieldDataSO.cs — Unity 입력 계층
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item
{
    [CreateAssetMenu(fileName = "NewFieldData", menuName = "ProjectL/Database/InGame/FieldData")]
    internal sealed class FieldDataSO : DatabaseItem
    {
        [SerializeField] private string fieldId;
        [SerializeField, Min(1)] private int columnCount = 5;
        // ...

        public override IData GetData() => new FieldData(fieldId, columnCount /* ... */);
    }
}
```

- 참조 SO가 있으면 `GetData()`에서 키만 꺼낸다. 예: `enemyLoadout.LoadoutKey`
- Table은 생성 시 `GetData()`를 한 번 호출하고 `Validate()`로 검증한 뒤 Dictionary에 보관한다(기존 `StageMapTable` 방식).
- `GetData()`는 **호출할 때마다 새 객체**를 만든다. 캐시하지 않는다. 캐시하면 에디터에서 SO 값을 바꿔도 반영되지 않기 때문이다. 따라서 "같은 객체인지"(`SameAs`)로 비교하는 테스트는 키 값 비교로 바꿔야 한다(§9.1).
- ViewGroup은 기존 방식을 따른다: 같은 파일에 `[Serializable] XxxData` + `XxxDataSO`. Unity 타입을 써도 된다.

`IData`는 `Framework/DataBase/DatabaseCatalogSO.cs`에 있는 빈 마커 인터페이스다. 파일은 Unity 어셈블리에 있지만 인터페이스 자체는 Unity 타입을 노출하지 않으므로 P1을 위반하지 않는다.

## 4. 공용 타입

**결정(D12): 옛 Core.Contracts에 있는 순수 C# 타입을 그대로 재사용한다.** `ProjectL.Systems`는 이미 `Core.Contracts`를 참조하고 있다. 나중에 옛 CoreCaculator를 정리하면서 문제가 생기면 그때 `Data/Common/`으로 옮긴다.

### 4.1 재사용 (옛 Core.Contracts)

네임스페이스 접두사: `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts`

| 타입 | 위치 | 값 |
|---|---|---|
| `EntityKey` | `.Ids` | None 0, Unit_Akimbo 10, Unit_ShotGun 11, Unit_Sniper 12, Unit_Demon 13, Unit_MainCharacter 14, Unit_Ballista 15, Unit_CactusCannon 16, Unit_Gravekeeper 17, Structure_Headquarters 20 |
| `Rotation4` | `.Common` | R0 0, R90 1, R180 2, R270 3 |
| `AppointMethod` | `.Common` | None 0, ForwardLine 1, ForwardLineWithDiagonal 2, Cross 3, Queen 4, ForwardExpanding 5, RadialSquare 6, ForwardCone 8 |
| `AttackTargetMode` | `.Common` | None 0, SingleTarget 1, AllHostilesInArea 2, AllDamageablesInArea 3 |
| `UnitOwnerEligibility` | `.Common` | PlayerOnly 0, EnemyOnly 1, Both 2 |
| `TraitKey` | `.Common` | None 0, Focus 10, MoveAttackDiscount 20, PiercingFalloff 30, ThornField 40, Knockback 50 |
| `CellCoord` | `.Common` | `int X, Y`. `+`/`-` 연산자가 있어 **좌표와 오프셋을 모두 표현**한다 |

- enum 값이 옛 에셋의 직렬화 숫자와 같아서 인벤토리 값을 그대로 옮길 수 있다.
- **`CellOffset`은 만들지 않는다.** 옛 SO의 `Vector2Int`(공격 패턴 오프셋)도 `CellCoord`로 표현한다.

### 4.2 신규 이동 (`DataBaseSystem/Data/Common/`)

네임스페이스: `ProjectL.Systems.DataBaseSystem.Data.Common`

| 타입 | 값 | 현재 위치 |
|---|---|---|
| `StageType` | Event, Combat, Shop | `StageRootDataSO.cs` 안 |
| `StageMapRole` | Regular, Boss | `StageRootDataSO.cs` 안 |

옛 Contracts에 없는 타입이라 새 공용 위치로 옮긴다. 영향을 받는 파일은 §9.1에 있다.

### 4.3 SO 입력용 셀 구조체

Contracts의 `CellCoord`는 `{ get; private set; }` 프로퍼티라서 Unity가 직렬화할 수 없다. 그래서 SO 쪽에는 입력용 구조체를 따로 둔다.

```csharp
// SO 입력 계층 전용. GetData()에서 CellCoord로 변환한다.
[Serializable]
internal struct CellInput
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    public CellCoord ToCellCoord() => new CellCoord(x, y);
}
```

위치: `DataBaseSystem/Data/InGame/Common/CellInput.cs` (UnityEngine 참조가 있으므로 순수 공용 폴더에 두지 않는다).

## 5. InGameGroup 설계 (Core 소유, 순수 C#)

### 5.1 테이블 목록

| 테이블 | 키 | 복구 원본 | 신규/기존 |
|---|---|---|---|
| StageMapTable | MapKey (int) | StageMapChapter1 | 기존 (P2 적용) |
| StageTable (StageRoot) | StageKey (int) | StageDefinition ×11 | 기존 (P2 적용, 상세 연결 변경) |
| (상세) CombatStageData | StageRoot가 참조 | StageDefinition ×11 | 기존 SO 확장 |
| FieldTable | FieldId (string) | FieldDefinition ×12 | 🆕 |
| UnitTable | EntityKey | UnitDefinition ×4 | 🆕 |
| StructureTable | EntityKey | Headquarters | 🆕 |
| EnemyLoadoutTable | LoadoutKey (int) | Chapter1_Enemy ×5 | 🆕 |
| PlayerLoadoutTable | LoadoutKey (int) | PlayerLoadout | 🆕 |
| UnitRosterTable | RosterKey (int) | UnitRoster | 🆕 |

StageViewPropTable은 ViewGroup으로 옮긴다(§6.5).

### 5.2 StageMapTable (기존)

| Data 필드 | 타입 | 복구 값 (Chapter1) |
|---|---|---|
| MapKey | int | **1** (신규 부여) |
| DepthCount | int | 7 |
| WidthMax | int | 3 |
| AdditionalConnectionChance | float | 0.25 |
| ConnectionAlgorithm | enum | Nearest (옛 데이터에 없음 → 기본값) |
| NearestConnectionWeight / OtherConnectionWeight | float | 10 / 1 (기본값) |
| Seed | int | 192 |
| RandomizeSeedEachRun | bool | true |

- 변경점: 현재 `StageMapData`의 `[SerializeField]` 필드를 `StageMapDataSO`로 옮기고, `StageMapData`는 순수 불변 클래스로 바꾼다(P2).
- 옛 `_regularStages`/`_bossStage` 목록은 복구하지 않는다. 현재 구조처럼 StageRoot의 `MapKey`, `MapRole`로 역참조한다.

### 5.3 StageTable (StageRoot, 기존) + CombatStageData

**결정: StageRoot와 상세 데이터 테이블을 분리하지 않는다.** 현재처럼 `StageRootDataSO`가 상세 SO(`StageBaseData`)를 참조한다. P3에 따라 SO 참조는 입력 계층에만 남는다.

```text
[입력 계층]  StageRootDataSO ──참조──► CombatStageData (SO, StageBaseData)
                                          ├─► FieldDataSO
                                          ├─► EnemyLoadoutDataSO
                                          ├─► UnitRosterDataSO
                                          └─► StructureDataSO (HQ, 구조물 목록)
                 │ GetData()
                 ▼
[순수 Data]  StageRootData { StageKey, MapKey, MapRole, StageType, StageData: IStageData }
                                                              └─ CombatStageDetail { FieldId, EnemyLoadoutKey, ... }
```

`StageRootData` (순수):

| 필드 | 타입 | 복구 값 |
|---|---|---|
| StageKey | int | 1101~1110, 2101 |
| MapKey | int | 1 |
| MapRole | StageMapRole | 1101~1110 Regular, 2101 Boss |
| StageType | StageType | Combat (상세 SO가 결정) |
| StageData | IStageData | CombatStageDetail |

`CombatStageDetail` (순수, 입력은 `CombatStageData` SO):

| 필드 | 타입 | 입력 SO 필드 | 복구 값 |
|---|---|---|---|
| TimelineSeed | int | int | 1 (Boss 2) |
| FieldId | string | FieldDataSO 참조 | 인벤토리 §4.2 표 |
| UnitRosterKey | int | UnitRosterDataSO 참조 | 1 |
| EnemyLoadoutKey | int | EnemyLoadoutDataSO 참조 | 인벤토리 §4.2 표 |
| HeadquartersKey | EntityKey | StructureDataSO 참조 | Structure_Headquarters |
| PlayerHeadquartersAnchor / Rotation | CellCoord / Rotation4 | CellInput / Rotation4 | 인벤토리 §4.2 표, R0 |
| EnemyHeadquartersAnchor / Rotation | CellCoord / Rotation4 | 〃 | 〃 |
| InitialEnemyPlacements | (EntityKey, CellCoord, Rotation4)[] | (UnitDataSO, CellInput, Rotation4)[] | [] |
| StructureCatalog | EntityKey[] | StructureDataSO[] | [] |
| InitialEnemyStructurePlacements | (EntityKey, CellCoord, Rotation4)[] | (StructureDataSO, CellInput, Rotation4)[] | [] |
| RailwayEnabled | bool | bool | false |
| Railway | { CellCoord[] RailCells, int FirstActivationAfterUnitTurns, int RepeatIntervalUnitTurns } | 〃 | [], 3, 3 |

규칙 위반 처리:

| 옛 필드 | 처리 | 규칙 |
|---|---|---|
| `_viewStageType` | **삭제**. View는 `StageType` + `MapRole`로 판단 | P7 |
| `_data` (StageDataSO), "Legacy Combat Data" 헤더 | 옛 코드의 이행 중 구조. 복구하지 않고 CombatStageData로 통합 | – |
| SO 참조 (field, roster, loadout, HQ, structure) | 입력 계층에만 남기고 Data에는 키 | P3 |
| `Cell` | 입력은 CellInput, Data는 CellCoord | P1 |

영향: `StageBaseData.GetData()`가 현재 `this`(SO)를 반환한다. P2에 따라 순수 상세 Data를 반환하도록 바꾼다(§9.1). `StageRootData.StageData` 프로퍼티는 이름을 유지하고 타입만 `StageBaseData`(SO)에서 `IStageData`(순수)로 바꾼다. 그러면 `StageServiceCollection`은 수정할 필요가 없다. Shop/Event 상세는 현재처럼 비어 있는 상태로 유지한다(필드 없음).

상세 순수 Data 클래스 이름은 `CombatStageDetail`, `ShopStageDetail`, `EventStageDetail`이다. SO 클래스(`CombatStageData` 등)는 기존 이름과 GUID를 유지한다.

### 5.4 FieldTable 🆕

| 필드 | 타입 | 복구 값 |
|---|---|---|
| FieldId (키) | string | `field_7x6` … `field_9x9` (12개) |
| ColumnCount / RowCount | int | 7~9 / 6~9 |
| ReserveCapacity | int | 4 |
| PlayerDeploymentDepth / EnemyDeploymentDepth | int | 2 / 2 |

- `_fieldVisual`은 **삭제**. ViewGroup의 `StageFieldViewTable.cellPrefab`이 대체한다(P1, P7).

### 5.5 UnitTable 🆕

| 필드 | 타입 | 복구 값 |
|---|---|---|
| Key | EntityKey | 10, 11, 12, 13 |
| OwnerEligibility | UnitOwnerEligibility | 인벤토리 §4.3 |
| FootprintWidth / Height | int | 1 / 1 |
| MaxHealth | int | 10 / 10 / 10 / 5 |
| MoveRange / MoveCost | int | 인벤토리 §4.3 |
| AppointMethod | AppointMethod | 〃 |
| AttackRange | int | 〃 |
| DirectionalAttackPattern | CellCoord[] (오프셋) | 〃 (입력은 `CellInput[]`) |
| AttackTargetMode | AttackTargetMode | 〃 |
| AttackPower, MaxActionCost, AttackCost, Priority, Delay | int | 〃 |
| Traits | UnitTraitData[] | Akimbo: MoveAttackDiscount(1, 1), Sniper: Focus(1, 3), 나머지 없음 |

#### Trait (5종 모두 구현, D14)

- 입력: SO에 `[SerializeReference] List<UnitTraitAuthoring>`를 둔다. 각 Authoring 클래스(`[Serializable]`, 입력 계층)가 `ToData()`로 순수 Data를 만든다.
- 순수 Data: `abstract UnitTraitData { TraitKey TraitKey }`와 아래 5종. 필드와 검증 규칙은 옛 Contracts의 `*TraitSpec`과 같다.

| TraitKey | 순수 Data | 필드 | 검증 |
|---|---|---|---|
| Focus (10) | `FocusTraitData` | DamagePerStack, MaxStacks | 둘 다 > 0 |
| MoveAttackDiscount (20) | `MoveAttackDiscountTraitData` | DiscountAmount, MinimumAttackCost | 둘 다 > 0 |
| PiercingFalloff (30) | `PiercingFalloffTraitData` | ReductionPercentPerTarget | 1~100 |
| ThornField (40) | `ThornFieldTraitData` | ThornDamage, LifetimeUnitTurns | 둘 다 ≥ 1 |
| Knockback (50) | `KnockbackTraitData` | DistanceCells | ≥ 1 |

- 한 유닛에 같은 TraitKey가 두 번 들어가면 UnitTable 검증에서 거부한다.
- 옛 SerializeReference 타입명(`ProjectL.Unity...*TraitAuthoring`)과 새 타입명이 달라서 옛 에셋의 trait 값은 자동으로 옮겨지지 않는다. 새 에셋에 값을 직접 입력한다.
- 위치: `Data/InGame/Table/Unit/Trait/`에 순수 Data 파일(`*TraitData.cs`)과 입력 파일(`*TraitAuthoring.cs`)을 나눠 둔다.

`_displayName`은 **ViewGroup `UnitVisualTable`로 분리**한다(P1 성격 기준: 표현).

### 5.6 StructureTable 🆕

| 필드 | 타입 | 복구 값 (Headquarters) |
|---|---|---|
| Key | EntityKey | Structure_Headquarters (20) |
| FootprintWidth / Height | int | 1 / 1 |
| MaxHealth | int | 3 |
| IsAttackable | bool | false |
| AttackPower | int | 0 |

`_displayName`(`base`)은 ViewGroup `UnitVisualTable`로 분리한다.

### 5.7 EnemyLoadoutTable / PlayerLoadoutTable / UnitRosterTable 🆕

옛 에셋에는 식별자가 없었다. 스테이지가 SO를 직접 참조했기 때문이다. 새 구조에서는 Core가 받는 Data에 SO 참조를 넣을 수 없어서(P3), 대신 가리킬 번호가 필요하다.

**"키를 부여한다" = 새 타입이 아니라 SO와 Data에 `int` 필드 하나를 추가하는 것이다.** 현재 `StageMapData.mapKey`와 같은 방식이다.

```csharp
// EnemyLoadoutDataSO (입력)
[SerializeField, Min(1)] private int loadoutKey = 1;   // 추가되는 필드
```

- 기획자는 에셋마다 번호만 적는다. 스테이지 SO에서는 지금처럼 Loadout SO를 끌어다 놓는다.
- Core에는 번호만 전달된다.
- StageMap(§5.2)은 이미 `mapKey` 필드가 있으므로 코드 변경 없이 값 1만 넣는다.
- UnitRosterTable은 만든다(D13).

| 테이블 | 키 (int 필드) | 필드 | 복구 값 |
|---|---|---|---|
| EnemyLoadoutTable | LoadoutKey 1~5 (Enemy_1~5 순) | Entries (EntityKey, Weight)[], SpawnCount, RandomSeed | 전부 (Demon, 1). SpawnCount 2/4/6/5/8, Seed 1/2/1/2/4 |
| PlayerLoadoutTable | LoadoutKey 1 | Entries (EntityKey, Count)[], StartingHandSize, RandomSeed | (Akimbo 2)(ShotGun 1)(Sniper 1), 4, 12345 |
| UnitRosterTable | RosterKey 1 | UnitKeys EntityKey[] | Akimbo, ShotGun, Sniper, Demon |

입력 SO의 엔트리는 `UnitDataSO` 참조로 받고 `GetData()`에서 EntityKey로 바꾼다(P3).

## 6. ViewGroup 설계 (View 소유, Unity 허용)

### 6.1 구조

- `ViewGroup : DatabaseGroup` SO를 Catalog에 등록한다.
- 쿼리 `IViewQuery`를 `DataBaseGroupSystem`(`IDataBaseQueryContext`)에 `IHasViewQuery`로 추가한다.
- `ViewDataBaseFactory`를 만들고 `DataBaseFactory.Create`에서 호출한다.
- 키: `DataBaseGroupKey.View`, `ViewTableKey.*`

테이블 형태는 두 가지다.

- **설정형**: 아이템이 정확히 1개다. 0개나 2개 이상이면 예외. Query는 `Config` 프로퍼티만 노출한다.
- **목록형**: Key → Data. 기존 `LightTable` 방식으로 `Get`/`TryGet`을 제공한다.

### 6.2 StageFieldViewTable (설정형)

FieldPrefab과 StageSceneViewConfig를 통합한다. FieldCamera는 제외(다른 작업자).

| 필드 | 타입 | 복구 값 |
|---|---|---|
| cellPrefab | GameObject | `Resources/Test/Stage/Board/Cube.prefab` |
| handUnitLayerMask | LayerMask | 256 (Layer 8) |
| ghostMaterial | Material | null (옛 값) |
| validGhostTint / invalidGhostTint | Color | (0.45,1,0.55,0.5) / (1,0.4,0.4,0.5) |
| ghostWorldOffset | Vector3 | (0, 0.05, 0) |
| ghostScale / fieldUnitScale | float | 0.5 / 0.5 |

셀 크기와 간격은 cellPrefab 크기로 계산한다(별도 필드 없음). 검증: cellPrefab non-null + Renderer 존재, scale > 0.

### 6.3 StageMapViewTable (설정형)

StageMapViewConfig를 복구한다. 값은 인벤토리 §4.2와 같다.

| 필드 | 타입 |
|---|---|
| depthSpacing, widthSpacing, nodeScale, lineHeight, lineWidth, pickDistance | float |
| currentColor, selectableColor, hoveredColor, candidateColor, unavailableColor | Color |
| nodePrefab, connectionPrefab, currentMarkerPrefab, startVisualPrefab | GameObject (옛 `StageMapNodeView`, `LineRenderer` 타입은 스크립트가 없어서 GameObject로 받는다) |
| nodeMask | LayerMask |

검증: 옛 `ValidateOrThrow` 중 GameObject로 확인할 수 있는 부분(필수 프리팹, 씬 인스턴스 금지, 양수 값, Renderer 존재, connection의 LineRenderer와 material).

표시 계약 보완(2026-10-03):

- `nodePrefab`의 직접 자식 `ClearedIndicator`에는 완료 표시용 Renderer를 둔다. 기존 `StageMapNode`에는 Collider가 없는 체크 모양의 자식으로 구성하며, 원본에서는 비활성화한다. 노드 표시 시 실제 스테이지(`StageKey != 0`)의 `IsClear`가 true일 때만 인스턴스에서 활성화한다. 클리어 여부의 별도 View 상태·DB 필드는 만들지 않는다(P6/P7). 선택·호버 강조 색과 완료 표시는 독립적이다.
- `nodePrefab`의 직접 자식 `HitTarget`에는 피킹용 Collider를 두고, 그 Layer는 `nodeMask`에 포함한다(2026-10-04, 스테이지 선택 View 계획 4번). 피킹은 맞은 Collider에서 부모를 따라 노드 Root를 찾으므로 외형 프리팹의 Collider도 같은 노드로 판별된다.
- `StageMapViewData.Validate`는 `ClearedIndicator`와 그 Renderer를 검사한다. 노드 프리팹을 교체할 때 이 자식 계약을 유지한다. SO·프리팹 원본의 상태를 런타임 표시 코드에서 수정하지 않는다.
- `StageMapViewDisplay.Show`는 `Func<StageProgressData, GameObject>` 외형 조회 함수를 받는다. 호출자는 기존 `StageNodeVisualTable.TryGetNodeVisual`에 연결해 `(StageType, MapRole)`에 해당하는 `VisualPrefab`을 반환하며, 외형 미지정은 null을 반환한다. Display/NodeView에서 별도 외형 사전이나 중복 키 검사를 만들지 않는다. 시작 노드는 조회 함수를 호출하지 않고 `StartVisualPrefab`을 사용한다.
- 위 계약은 표시 부품의 범위다. Factory 등록, SceneRoot, Attach/Detach, 데이터 대기·재조회는 다음 화면 수명 연결 단계에서 구현한다. 그 시스템은 씬에 연결된 동안만 준비 여부를 확인하고, Detach 뒤에는 조회와 표시를 중단한다. 현재 표시 코드만으로 맵 생성·화면 진입·선택 승인을 수행하지 않는다.

### 6.4 StageNodeVisualTable (목록형)

| 키 | 필드 |
|---|---|
| (StageType, StageMapRole) | visualPrefab (GameObject), icon (Sprite), displayName (string) |

| 복구 항목 | 키 | 값 |
|---|---|---|
| 옛 `NomalStage` | (Combat, Regular) | StageVisual_NormalPlaceholder, null, "Normal Stage A" |
| 옛 `BossStage` | (Combat, Boss) | StageVisual_BossPlaceholder, null, "Boss Stage" |

displayName이 비어 있으면 프리팹 이름을 쓴다. 키 값은 View가 Buffer(§7 B1)에서 받는다.

### 6.5 StageViewPropTable (InGameGroup에서 이동)

- `StageViewPropData`는 GameObject를 가지므로 P1에 따라 InGameGroup에 둘 수 없다. **ViewGroup으로 옮긴다.**
- 키: StageKey (int), 필드: prop GameObject. 기존 코드 구조와 검증은 유지한다.
- 이동 대상: `Data/InGame/Table/Stage/ViewProp/**` → `Data/View/Table/StageViewProp/**`, `StageViewPropTable.cs` → `Queries/ViewDataBase/ViewTable/`, `InGameTableKey.StageViewProp` → `ViewTableKey.StageViewProp`, `IInGameQuery.StageViewPropTableQuery` → `IViewQuery`
- 영향: `FrameworkRuntimeCompositionTests.cs`(StageViewProp 조회 테스트), `InGameDataBaseFactory.cs`
- 기존 에셋이 없어서 에셋 이동은 없다. `.cs` 이동 시 `.meta`(GUID)를 같이 옮긴다.

### 6.6 UnitVisualTable (목록형)

| 키 | 필드 |
|---|---|
| EntityKey | prefab (GameObject), displayName (string) |

| EntityKey | prefab | displayName |
|---|---|---|
| Unit_Akimbo | Prefabs/Stage/Units/StageUnit_Akimbo | Akimbo |
| Unit_ShotGun | Prefabs/Stage/Units/StageUnit_ShotGun | **ShotGun** (옛 값 `ShouGun` 오타 수정) |
| Unit_Sniper | Prefabs/Stage/Units/StageUnit_Sniper | Sniper |
| Unit_Demon | Resources/Test/Model/Prefab/TestUnit_Base | Demon |
| Structure_Headquarters | Prefabs/Default/Cube | base |

검증: None 키 금지, 중복 금지, prefab non-null. displayName이 비어 있으면 프리팹 이름을 쓴다.

## 7. Buffer 요구사항 (View에 필요한 것만)

### 7.1 Buffer의 역할

Buffer는 **Core와 View 사이의 공유 게시판**이다. Core가 계산 결과를 WriteSink로 쓰고, View가 ReadSink로 읽는다. Core와 View는 서로 직접 호출하지 않는다. 필요한 Buffer 필드는 우리가 정의한다(P6).

스테이지 맵의 흐름:

```text
① StageGeneratorSystem (Core)
   ├ InGameGroup에서 StageRoot 목록을 읽음 (각 스테이지의 StageType, MapRole 포함)
   ├ 맵 노드를 생성
   └ StageProgressBuffer에 노드 목록을 씀 (WriteSink.RegisterStages)

② 스테이지 맵 View
   └ StageProgressBuffer에서 노드 목록을 읽음 (ReadSink.GetStages)
      → 노드마다 위치(Depth, WidthIndex), 연결선(NextNodeIds), 클리어 여부를 얻어 그림
```

**문제:** 노드 외형(일반/보스 프리팹)을 고르려면 노드의 종류가 필요하다. 그런데 현재 `StageProgressData`에는 `StageKey`만 있다. View는 InGameGroup을 읽지 않으므로 StageKey로 종류를 찾아볼 수 없다.

**해결:** Core가 Buffer에 노드를 쓸 때 종류도 같이 쓴다. Core는 이미 그 정보(StageRootData)를 들고 있다.

### 7.2 요구사항

| # | Buffer | 추가 | 이유 | 쓰는 쪽 | 범위 |
|---|---|---|---|---|---|
| B1 | `StageProgressData` | `StageType`, `StageMapRole` | 노드 외형 조회 키(§6.4) | `StageGeneratorSystem` | **View 작업에서 구현 (D21). Core 작업자 확인 요청** |
| B2 | (전투 필드 Buffer) | 맵 크기, 엔티티(EntityKey, 소유자, 위치, 회전, 점유 셀), 배치 깊이 | 전투 View | Core 전투 시스템 | 이번 범위 아님. 전투 View 작업 때 정의 |

### 7.3 B1 상세

```csharp
// StageProgressData — 필드 2개 추가. 기존 호출(5·6인자)이 깨지지 않도록 기존 isClear 뒤에 기본값 인자로 둔다.
public StageProgressData(int nodeId, int stageKey, int depth, int widthIndex,
    IEnumerable<int> nextNodeIds, bool isClear = false,
    StageType stageType = default, StageMapRole mapRole = default)

// StageGeneratorSystem.cs 73행
// 현재: new StageProgressData(node.Id, node.StageKey, node.Depth, node.Width, node.Next)
// 변경: 노드가 가리키는 StageRootData의 StageType, MapRole을 함께 전달
```

- **시작 노드**(NodeId 1, StageKey 0)는 스테이지가 아니다. 두 필드는 기본값이고 의미가 없다. View는 시작 노드에 StageMapView의 `startVisualPrefab`을 쓴다.
- `StageProgressBuffer.cs` 53행에서 클리어 상태를 갱신할 때 `StageProgressData`를 다시 만든다. 새 필드도 복사해야 한다.
- 영향을 받는 테스트: `MapProgressBufferTests.cs`(5인자 호출 8곳). 기본값이 있으므로 수정할 필요는 없다.
- 구현(D21): `StageGeneratorSystem`은 기존 스테이지 수집 루프에서 `stageKey → StageRootData` 사전을 함께 만들고, 결과 루프에서 `StageKey == 0`이 아닌 노드에만 `StageType`, `MapRole`을 채운다. 시작 노드의 기본값은 `StageType.Event`/`StageMapRole.Regular`로 읽히므로, View는 두 필드보다 `StageKey == 0`을 먼저 확인해야 한다.

## 8. 파일 / 폴더 배치

```text
Systems/DataBaseSystem/
├─ Data/
│  ├─ Common/                                  🆕 StageType, StageMapRole (순수 C#)
│  ├─ InGame/
│  │  ├─ InGameGroup.cs
│  │  ├─ Common/                               🆕 CellInput (SO 입력용, UnityEngine 사용)
│  │  └─ Table/
│  │     ├─ Map/        {StageMapDataTableSO, Item/StageMapData🆕, Item/StageMapDataSO}
│  │     ├─ Stage/      {StageRootDataTableSO, StageBaseData, Item/StageRootData🆕, Item/StageRootDataSO,
│  │     │               Combat/{CombatStageData(SO), CombatStageDetail🆕}, Shop/…, Event/…}
│  │     ├─ Field/      🆕 {FieldDataTableSO, Item/FieldData, Item/FieldDataSO}
│  │     ├─ Unit/       🆕 {UnitDataTableSO, Item/UnitData, Item/UnitDataSO,
│  │     │                  Trait/{UnitTraitData, *TraitData ×5, UnitTraitAuthoring, *TraitAuthoring ×5}}
│  │     ├─ Structure/  🆕 {StructureDataTableSO, Item/StructureData, Item/StructureDataSO}
│  │     └─ Loadout/    🆕 {EnemyLoadout…, PlayerLoadout…, UnitRoster…}
│  ├─ View/                                    🆕
│  │  ├─ ViewGroup.cs
│  │  └─ Table/{StageField, StageMap, StageNode, StageViewProp, Unit}/
│  └─ Render/                                  기존
├─ Queries/
│  ├─ InGameDataBase/InGameTable/              + Field, Unit, Structure, EnemyLoadout, PlayerLoadout, UnitRoster 테이블
│  ├─ ViewDataBase/                            🆕 ViewDataBase.cs, ViewTable/…
│  └─ RenderDataBase/                          기존
└─ DataBaseGroupSystem.cs                      + IHasViewQuery

Systems/Factory/DBFactory/                     develop 동기화로 경로 변경 (옛 Factory/Loading/LodingSystemFactory/DataBaseSystemFactory/)
├─ DataBaseFactory.cs                          + ViewGroup
└─ Group/{InGameDataBaseFactory, ViewDataBaseFactory🆕, RenderDataBaseFactory}

Systems/ServiceKeys/DataBaseGroupServiceKey.cs + DataBaseGroupKey.View, ViewTableKey, InGameTableKey 추가 키

Assets/ProjectL/DataBaseSO/
├─ DatabaseCatalogSO.asset                     + InGameGroup, ViewGroup 등록
├─ InGameGroup/                                🆕 테이블별 폴더
├─ ViewGroup/                                  🆕 테이블별 폴더
├─ RenderGroup/                                기존
└─ (옛 Stage/, Architecture/ 에셋)              보존. 삭제 금지
```

## 9. 옛 에셋 → 새 에셋 매핑 요약

| 옛 에셋 | 새 위치 | 처리 |
|---|---|---|
| StageMapChapter1 | InGame / StageMap | 복구 (mapKey 1) |
| StageDefinition ×11 | InGame / StageRoot + CombatStageData | 복구. `_viewStageType` 삭제 |
| FieldDefinition ×12 | InGame / Field | 복구. `_fieldVisual` 삭제 |
| UnitDefinition ×4 | InGame / Unit + View / UnitVisual(displayName) | 복구 + 분리 |
| Headquarters | InGame / Structure + View / UnitVisual(displayName) | 복구 + 분리 |
| Chapter1_Enemy ×5, PlayerLoadout, UnitRoster | InGame / Loadout, Roster | 복구 (키 필드 추가) |
| FieldPrefab + StageSceneViewConfig | View / StageFieldView | 통합 |
| StageMapViewConfig | View / StageMapView | 복구 |
| StageMapVisualCatalog | View / StageNodeVisual | 복구 (키 변경) |
| UnitVisualCatalog | View / UnitVisual | 복구 |
| FieldCamera, Architecture ×6 | – | 범위 밖 |

### 9.1 기존 코드 수정 목록

설계를 적용하려면 기존 코드도 고쳐야 한다. 구현 Task에서 진행한다.

| # | 수정 | 대상 파일 | 이유 |
|---|---|---|---|
| M1 | `StageType`, `StageMapRole`을 `Data/Common/`으로 이동하고 using 추가 | `StageRootDataSO.cs`, `StageBaseData.cs`, `Combat/Shop/EventStageData.cs`, `StageServiceCollection.cs`, `StageServiceManager.cs`, `StageGeneratorSystem.cs` | §4.2 |
| M2 | StageMap P2 분리: 순수 `StageMapData.cs`(StageConnectionAlgorithm 포함) 신규, `StageMapDataSO.cs`는 입력 필드만 | `Map/Item/*` | P2 |
| M3 | StageRoot P2 분리: 순수 `StageRootData.cs`(`IStageData : IData` 포함) 신규, `StageRootDataSO.cs`는 입력 필드만. `StageRootData.StageData` 타입을 `IStageData`로 변경 | `Stage/Item/*` | P2, P3 |
| M4 | `StageBaseData.GetData()`가 순수 상세 Data를 반환. `Combat/Shop/EventStageDetail.cs` 신규 | `Stage/StageBaseData.cs`, `Stage/{Combat,Shop,Event}/*` | P2 |
| M5 | StageViewProp을 ViewGroup으로 이동 (ViewGroup 뼈대 이후) | §6.5 목록 | P1 |
| M6 | Buffer B1 | `StageProgressData.cs`, `StageProgressBuffer.cs`, `StageGeneratorSystem.cs` | §7.3 |
| M7 | 테스트 수정 | `StageDataTableTests.cs`: 값을 SO 필드에 넣고, `SameAs` 대신 키 값으로 비교. `FrameworkRuntimeCompositionTests.cs`: StageViewProp 쿼리 타입 경로 변경 | M2, M5 |

주의:

- `InGameEnum.cs`의 `StageEnum`은 사용하지 않지만 **삭제하지 않는다.** 테스트 2개(`StageDataTableTests`, `FrameworkRuntimeCompositionTests`)가 Systems 어셈블리를 찾는 기준 타입으로 쓴다.
- 수정 대상 스크립트를 참조하는 에셋은 현재 없다(GUID 검색 확인). 직렬화 필드 구조를 바꿔도 깨지는 데이터가 없다.

## 10. 코딩 규칙

현재 코드(Framework, Systems 161개 파일, CoreCaculator 제외)에서 파악한 규칙과 이번 설계에서 정한 규칙이다.

### 10.1 구조 / 아키텍처

- 네임스페이스는 `ProjectL.<Script 아래 폴더 경로>`, 블록 네임스페이스(file-scoped 미사용).
- 어셈블리: Framework는 Systems·Core·FMOD를 모른다. Core 3종 asmdef는 Unity를 참조하지 않는다.
- 기존 오타 식별자는 유지한다: `CoreCaculator`, `Loding`, `Scop`, `Metrial`, `EffecDataTableSO`, `EventBuse`, `IMeshCraeteData`.
- 서비스는 `IXxx : IServiceModule` 인터페이스로 등록하고, 생명주기가 있으면 `AddModule`도 한다(FW-001/002).
- 키: `internal static class XxxKey { public static readonly SystemGroupKey<IXxx> Name = new(nameof(Name)); }`를 ServiceKeys에 둔다.
- 팩토리: `static class XxxFactory.Create()`에서 `SystemBuilderHelper` → `BuildSystemReader()` → `new Group(reader)`. Builder는 한 번만 Build한다(FW-003).
- 그룹은 생성자에서 `ServiceStoreReader.GetService(Key)`로 하위 서비스를 조회해 프로퍼티로 노출한다.

### 10.2 DB

- Catalog → Group SO → Table SO → Item SO. Registry는 구체 타입으로 조회하므로 그룹마다 같은 Table 타입은 하나만 둔다.
- Table SO는 빈 클래스 `XxxDataTableSO : DatabaseTable`.
- Query: `XxxTable : IXxxTableQuery, IExportSystemServices`, 생성자 `internal XxxTable(IDataProvider<IDatabaseItem>)`, `.OfType<XxxDataSO>()`로 순회하며 검증 → Dictionary.
- 조회: `Get(key)`는 없으면 `KeyNotFoundException`, `TryGet(key, out)`를 함께 제공한다.
- 메뉴: `[CreateAssetMenu(menuName = "ProjectL/Database/InGame/…")]`, View는 `"ProjectL/Database/View/…"`.
- **(신규) InGame은 P2: 순수 Data 파일과 SO 파일을 분리한다.** 순수 Data 파일에는 `using UnityEngine`이 없어야 한다. View와 Render는 기존처럼 한 파일에 둔다.
- **(신규) `GetData()`는 캐시하지 않고 매번 새로 만든다.**

### 10.3 스타일

- Allman 중괄호, 공백 4칸, LF, UTF-8.
- 클래스는 `sealed`가 기본이고 `internal`/`public`을 명시한다.
- 직렬화 필드는 `camelCase`, 일반 private 필드는 `_camelCase`. 기존 코드에 혼용이 있지만 새 코드는 이 기준을 따른다.
- 프로퍼티는 식 본문(`=> field`) 또는 get-only 자동 프로퍼티.
- `new()` 대상 형식을 사용한다.
- 인자 검사는 `ArgumentNullException(nameof(x))`, 상태·데이터 오류는 `InvalidOperationException`, 조회 실패는 `KeyNotFoundException`. 메시지에 `[ClassName]` 접두사를 붙인다.
- 주석과 XML doc은 한국어로 쓴다.
- 커밋은 사용자가 직접 한다.

### 10.4 주의

- 한국어 주석이 깨진 파일(U+FFFD)이 10개 있다(`DatabaseCatalogSO.cs`, Render DB 파일, `CustomPool.cs`). 새 코드에 복사하지 않는다.
- `.cs`를 옮길 때는 `.meta`를 같이 옮겨 GUID를 유지한다. MonoBehaviour/SO 타입의 네임스페이스를 바꾸면 `MovedFrom`을 붙인다.
- 에셋과 `.meta`는 Unity 에디터 밖에서 YAML로 직접 작성한다. 처음 임포트할 때 Unity가 재직렬화할 수 있다.
- `ProjectL.Systems.DataBaseSystem.Data.InGame.Enum` 네임스페이스가 있어서 `Data.InGame` 아래 코드에서 `Enum.IsDefined`의 `Enum`이 네임스페이스로 해석된다. **`System.Enum`으로 전체 이름을 쓴다.**

## 11. 검증 방법

### 11.1 작업 환경에서 할 수 있는 것

작업 환경에 Mono와 Roslyn 컴파일러(NuGet `Microsoft.Net.Compilers.Toolset`, C# 9)를 설치해 부분 컴파일을 할 수 있다. Unity DLL은 없다.

| 검증 | 방법 | 확인하는 것 |
|---|---|---|
| V1 순수 Data 컴파일 | 순수 Data 파일 + Contracts 타입 파일 + `IData` stub만으로 컴파일 | **P1**: UnityEngine 없이 컴파일되는지 |
| V2 SO / Table 부분 컴파일 | UnityEngine, Framework 인터페이스 stub + 실제 소스 | 타입, 시그니처, using 오류 |
| V3 문서·에셋 교차 확인 | 스크립트 GUID ↔ 에셋 `m_Script`, 값 ↔ 인벤토리 | 에셋 연결과 값 |

### 11.2 Unity 에디터에서 확인할 것 (사용자)

작업 환경에는 Unity가 없어서 아래 항목은 에디터에서만 확인할 수 있다.

| # | 확인 | 방법 | 기대 결과 |
|---|---|---|---|
| E1 | 컴파일 | 에디터를 열고 Console 확인 | 에러 0. `ProjectL.Systems`, `ProjectL.Tests.Framework` 모두 |
| E2 | EditMode 테스트 | Test Runner → EditMode → `ProjectL.Tests.Framework` 전체 실행 | 모두 통과. 새 테스트 4개 파일(§11.3)과 기존 `StageDataTableTests`, `FrameworkRuntimeCompositionTests`, `MapProgressBufferTests` 포함 |
| E3 | DB 생성 | E2의 `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups` | 통과. Render·InGame·View 그룹이 모두 만들어짐. **이 테스트만 작업 환경에서 돌리지 못했다**(RenderGroup 에셋을 stub으로 불러올 수 없음) |
| E4 | Trait 인스펙터 | `InGameGroup/Unit/Item/Unit_Akimbo`, `Unit_Sniper` 선택 | Traits 목록에 MoveAttackDiscount(1, 1), Focus(1, 3)가 보임 |
| E5 | 프리팹 링크 | `ViewGroup` 아래 StageFieldView, StageMapView, NodeVisual, UnitVisual 에셋 선택 | 프리팹 칸이 비어 있지 않음(Missing 아님). 유닛 4개는 Prefab Variant |
| E6 | 옛 에셋 | `DataBaseSO/Stage`, `Architecture` 등 | 그대로 있음(Missing Script 상태 유지, 삭제하지 않음) |
| E7 | 재직렬화 diff | 에셋을 연 뒤 git 변경 확인 | Unity가 형식을 다시 쓰면서 diff가 생길 수 있다. 값이 바뀌지 않았으면 커밋해도 된다 |

### 11.3 테스트 (Phase F에서 추가)

`Assets/Tests/EditMode/Framework/`(어셈블리 `ProjectL.Tests.Framework`)에 추가했다.

| 파일 | 내용 |
|---|---|
| `DataBaseTestAssets.cs` | 테스트 도우미. SO·GameObject를 메모리에서 만들고 정리, private 직렬화 필드 입력 |
| `InGameDataTableTests.cs` | InGame 신규 테이블 6종 조회·키 없음·중복 키·필수 값 누락, 전투 스테이지 Field/Roster 필수, 레일웨이 변환, `GetData` 새 인스턴스(P2), Trait 중복·null·값 범위(5종) |
| `ViewDataTableTests.cs` | 설정형 0개/2개 거부(StageFieldView, StageMapView 0개), cellPrefab·Renderer 필수, NodeVisual·UnitVisual 조회·중복 키·None·프리팹 누락, displayName 대체 |
| `DataBaseAssetTests.cs` | **실제 에셋**을 `AssetDatabase`로 불러와 확인: Catalog로 DB 생성, Unit/Structure의 EntityKey가 UnitVisual에 모두 있는지, 모든 스테이지의 (StageType, MapRole)에 NodeVisual이 있는지, 전투 스테이지가 가리키는 Field·Roster·Loadout·HQ·유닛 키가 모두 있는지, StageMapView 2개 거부 |

## 12. 결정 기록

| # | 결정 | 날짜 |
|---|---|---|
| D1 | 새 Core는 Framework/Systems 시스템으로 InGameGroup을 직접 참조. View는 Buffer로 받음 | 2026-09-24 |
| D2 | InGame Data는 순수 C# (P1), SO는 입력 계층 (P2). 기획 변경은 SO에서 | 2026-09-24 |
| D3 | StageRoot와 상세 SO는 분리하지 않음. SO 참조는 입력 계층에서만 (P3) | 2026-09-24 |
| D4 | StageViewProp은 ViewGroup으로 이동 | 2026-09-24 |
| D5 | 새 공용 타입 위치는 `DataBaseSystem/Data/Common/` | 2026-09-24 |
| D6 | FieldId는 기존 문자열 유지 | 2026-09-24 |
| D7 | InGameGroup도 View 작업에서 직접 구현 | 2026-09-24 |
| D8 | 기존 SO 복구 중심, 규칙 위반만 분리 | 2026-09-24 |
| D9 | 셀 크기는 프리팹 크기로 계산 | 2026-09-24 |
| D10 | 노드 외형 키는 (StageType, StageMapRole), View는 Buffer로 받음 | 2026-09-24 |
| D11 | displayName은 ViewGroup UnitVisualTable로 분리 | 2026-09-24 |
| D12 | 공용 타입은 옛 Core.Contracts 것을 재사용. 없는 StageType/StageMapRole만 Common으로. 문제가 생기면 그때 대응 | 2026-09-24 |
| D13 | UnitRosterTable 제작 | 2026-09-24 |
| D14 | Trait 5종 모두 구현 (필드는 Contracts `*TraitSpec` 기준) | 2026-09-24 |
| D15 | `CellOffset` 만들지 않음. 좌표·오프셋 모두 `CellCoord`. SO 입력은 `CellInput` | 2026-09-24 |
| D16 | Loadout/Roster 키는 새 타입이 아니라 int 필드 추가 | 2026-09-24 |
| D17 | 커밋은 사용자가 직접 | 2026-09-24 |
| D18 | Loadout/Roster 키 값: Enemy 1~5, Player 1, Roster 1 | 2026-09-24 |
| D19 | Core 전달 문서를 이 설계 기준으로 다시 작성 ([전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)) | 2026-09-24 |
| D20 | Buffer B1(§7.3)은 Core 로직 수정이라 View 작업에서 구현하지 않고 Core 작업자에게 요청. View 작업에서 Core 영역에 닿은 변경(using 3줄, 데이터 API)은 전달 문서 §5에 정리해 넘김 | 2026-09-24 |
| D21 | D20을 바꿔 Buffer B1을 View 작업에서 구현(사용자 지시 "PhaseD도 진행하고 E까지 한번에 진행해"). 변경 내역은 전달 문서 §5에 기록하고, §6은 Core 작업자 확인 요청으로 바꿈 | 2026-09-25 |

## 13. 열린 질문

설계 쪽 열린 질문은 없다. Core 작업자 확인 사항은 [전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §12에 있다.

## 14. 구현 결과 (2026-09-25)

Phase A~F를 모두 구현했다. 실행 기록은 [구현 계획](../plans/2026-09-24-db-so-restructure-plan.md)의 "실행 기록"에 있다.

### 14.1 설계와 달라진 점

| # | 설계 | 구현 | 이유 |
|---|---|---|---|
| 1 | Buffer B1은 Core 작업자에게 요청(D20) | View 작업에서 구현(D21) | 사용자 지시. Core 전달 문서 §5.3·§6에 기록 |
| 2 | §8: Trait을 종류별 파일(`*TraitData ×5`, `*TraitAuthoring ×5`)로 배치 | `UnitTraitData.cs`, `UnitTraitAuthoring.cs` 두 파일에 모음 | 옛 `TraitAuthoring.cs`와 같은 구성이고 추가가 쉬움 |
| 3 | §6.3: nodeMask는 노드 HitCollider 레이어를 포함 | 노드 프리팹 **자식의 `Collider`** 레이어로 검사 | 옛 `StageMapNodeView.HitCollider` 스크립트가 삭제됨 |
| 4 | – | 빈 `CombatStageData`(Field·Roster 없음)는 검증에서 거부 | 옛 StageDefinitionSO 규칙과 같음. 빈 전투 스테이지로 테스트하던 코드가 있다면 Field·Roster를 넣어야 함 |
| 5 | – | 시작 노드(StageKey 0)의 B1 필드는 `Event`/`Regular`로 읽힘 | 기본값. View는 `StageKey == 0`을 먼저 확인(§7.3) |
| 6 | – | 유닛 프리팹 참조 4개는 Prefab Variant의 루트(stripped GameObject) | 옛 UnitVisualCatalog와 같은 값 |

### 14.2 남은 일

- 에디터 확인(§11.2).
- `[SerializeReference]` Trait 목록에 항목을 추가하는 인스펙터 UI(PropertyDrawer)가 없다. 기획자가 Trait을 추가하려면 후속 작업이 필요하다.
- Core 작업자 확인([전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §12).
- 전투 필드 Buffer(B2)는 전투 View 작업 때 정의한다.
