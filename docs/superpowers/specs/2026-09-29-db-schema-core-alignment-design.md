# DB 스키마를 현재 Core 입력 타입에 맞추기 — 설계

> InGameGroup·ViewGroup 스키마를 현재 Core Contracts의 입력 Spec에 맞춘다. 값은 develop-4week 에셋에서 옮긴다(develop-4week는 합치지 않는다).
> 상태: **설계 확정(2026-09-29).** §7 결정은 §8에 기록했다. 계획: [Task 계획](../plans/2026-09-29-db-schema-core-alignment-plan.md)

- 작성일: 2026-09-29
- 작성: View 작업
- 기준 Core: 현재 브랜치 `Contracts/Commands`·`Common`. Core 작업자 브랜치(`feature/Cheon/refactor/core-system`)와 **두 폴더 모두 같다**(2026-09-29 확인).
- 값 출처: `origin/develop-4week` `ae07af37`의 `Assets/DataBase/SO` ([차이 문서](2026-09-29-develop-4week-so-diff.md))
- 참고한 변환 규칙: develop-4week의 옛 어댑터(`UnitSpecAdapter`, `StageEntryDataBuilder`, `CategoryCatalogSO.BuildSpecs`, `PlayerLoadoutAdapter`, `RecoverySpecAdapter`, `ShopSpecAdapter`, `StageMapCatalog`)
- 선행 설계: [DB-SO 재구성 설계](2026-09-24-db-so-ingame-view-group-design.md) 규칙 P1~P7은 그대로 따른다.

## 1. 원칙

| # | 원칙 |
|---|---|
| A1 | InGame Data 필드는 **Core Spec 생성자 인자와 같은 타입**을 쓴다(Contracts enum, `CellCoord`, `EntityKey`, int/long). Core는 Data에서 Spec을 1:1로 만든다 |
| A2 | Spec 중 **다른 값에서 계산되는 인자는 필드로 만들지 않는다**(P7). 예: `footprintLocalCells`(가로×세로로 채움), `MoveMethod`(항상 LiftAndPlace), `hasTimelineConfiguration`(항상 true), 비전투 바인딩(스테이지 종류에서 계산) |
| A3 | Spec 객체 자체(`UnitSpec`, `AttackPatternSpec` 등)는 Data에 넣지 않는다. Spec 조립은 Core 쪽 일이다(Core는 Systems의 Core 시스템에서 DB를 읽는다) |
| A4 | 표시용 값(이름, 아이콘, 설명, 프리팹, 연출 수치)은 ViewGroup으로 간다. 키는 InGame과 같은 키(P5) |
| A5 | 에셋에 있던 필드 이름을 바꿀 때는 `[FormerlySerializedAs]`로 기존 값을 살린다 |

## 2. Core 입력 Spec ↔ DB 대응 한눈에

| Core Spec | 채우는 곳 | 변경 |
|---|---|---|
| `UnitSpec` (+ `AttackPatternSpec`, `ImpactPatternSpec`, `TraitSpec`) | InGame `UnitTable` | **필드 추가·제거** (§3.1) |
| `StructureSpec` | InGame `StructureTable` | 검증 1개 추가 |
| `StageConfig` | `FieldTable` + `CombatStageDetail` | 밤 길이 추가 (§3.3) |
| `StageStartSpec` | Core가 조립 (Stage + Roster의 유닛 + 구조물 + 카테고리 상성) | – |
| `CategorySpec[]`, `CategoryMatchupSpec` | InGame **`CategoryConfig`** 🆕 | 상성은 `CreateDefault()`라 DB 없음 |
| `StageEconomySpec` | InGame **`StageEconomyTable`** 🆕 | |
| `MainCharacterEntrySpec` | `CombatStageDetail`(앵커·회전) + `PlayerLoadout`(메인 캐릭터 키) | **본부 앵커 → 메인 캐릭터 앵커** (§3.3) |
| `InitialEnemyPlacementSpec`, `InitialEnemyStructurePlacementSpec` | `CombatStageDetail` | 변경 없음 |
| `RailwayGimmickSpec` + railCells | `CombatStageDetail.Railway` | 변경 없음 |
| `RunCharacterDeckSpec` | InGame `PlayerLoadoutTable` | 메인 캐릭터 추가 (§3.5) |
| `StageMapGenerationConfig` | InGame `StageMapTable` | 변경 없음 |
| `RunNonCombatSpec` (바인딩 + `shopBaseSeed`) | 바인딩은 StageRoot 종류에서 계산. `shopBaseSeed`는 **출처 미정**(§7-6) | |
| `RecoveryStageSpec` (+ Option, Heal) | InGame 회복 상세 + **`RecoveryOptionTable`** 🆕 | §3.4 |
| `ShopStageSpec` (+ Candidate) | InGame 상점 상세 | §3.4 |
| Run 지갑 초기 골드 (옛 RunEconomy) | InGame **`RunEconomyConfig`** 🆕 | Core Spec이 없음. §7-7 |

## 3. InGameGroup 변경

### 3.1 UnitTable

| 필드 | 타입 | 처리 | Spec 대응 | 검증 |
|---|---|---|---|---|
| `Role` | `UnitRole` | 🆕 | `role` | 정의된 값 |
| `Category` | `CategoryKey` | 🆕 | `categoryKey` | None 금지 |
| `PhysicalDefense` | int | 🆕 | `physicalDefense` | ≥ 0 |
| `CannonAreaType` | `CannonAreaType` | 🆕 | `ImpactPatternSpec.CreateCannon` | Category가 Cannon일 때만 의미. 정의된 값 |
| `CannonAreaRange` | int | 🆕 | 〃 | ≥ 0 |
| `PierceRayCount` | int | 🆕 | 〃 | 1 또는 3. 3은 Pierce일 때만 |
| `DirectionalAttackPattern` | – | **제거** | `AttackPatternSpec`에 셀 목록이 없음 | – |
| `AppointMethod` | `AppointMethod` | 유지 | `AttackPatternSpec.Create*` | **Cross, None 거부**(Core에 생성 함수가 없음) |
| 나머지 | – | 유지 | – | – |

- 충격 패턴 규칙: Category == Cannon이면 `CreateCannon(type, range, pierce)`, 아니면 `CreateSingleCell()`. develop-4week는 캐논이 아닌 유닛에 `cannonAreaType -1`을 저장했다. 새 SO는 캐논이 아니면 캐논 필드를 무시한다(값 -1은 옮기지 않고 기본값으로 둔다).
- Role이 MainCharacter인 유닛은 Owner가 Player 계열이어야 하는지 Core에 확인(§7-2).

### 3.2 StructureTable

스키마 변경 없음. Core `StructureSpec`의 규칙 "공격 가능하면 공격력 > 0"을 `Validate()`에 추가한다.

### 3.3 Stage — Root·CombatStageDetail

| 필드 | 타입 | 처리 | Spec 대응 |
|---|---|---|---|
| `NightLength` | long | 🆕 | `StageConfig.nightLength` |
| `EconomyKey` | int | 🆕 (SO는 `StageEconomyDataSO` 참조, P3) | `StageEconomySpec` |
| `MainCharacterAnchor`, `MainCharacterRotation` | `CellCoord`, `Rotation4` | **이름 변경** (← `PlayerHeadquartersAnchor/Rotation`, `FormerlySerializedAs`) | `MainCharacterEntrySpec` |
| `HeadquartersKey`, `EnemyHeadquartersAnchor/Rotation` | – | 유지 | 적 본부만 남는다(develop-4week와 같음) |
| `CategoryCatalog` 참조 | – | **만들지 않음** | 전역 설정형 `CategoryConfig`로(§3.6). develop-4week도 모든 스테이지가 같은 에셋을 참조했다 |
| 배경 소품·offset, `railway.visuals` | – | **ViewGroup으로** (§4) | – |

### 3.4 비전투 스테이지 (회복·상점)

Core 구분은 `StageExecutionKind`(Combat/NonCombat) + `NonCombatStageKind`(Recovery/Shop)다. 우리 `StageType`은 `Event/Combat/Shop`이다(§7-1).

| 대상 | 필드 | Spec 대응 |
|---|---|---|
| 회복 상세 (현 `EventStageData` 자리) | `RecoveryOptionKeys` (SO는 `RecoveryOptionDataSO` 목록 참조) | `RecoveryStageSpec(stageKey, options)` |
| 🆕 `RecoveryOptionTable` (키 OptionId) | `OptionId`, `Cost`, `HealMode`(`RecoveryHealMode`), `HealValue` | `RecoveryOptionSpec` + `RecoveryHealSpec` |
| 상점 상세 (현 `ShopStageData` 자리) | `Candidates` (EntityKey, Cost) — SO는 `UnitDataSO` 참조 | `ShopStageSpec(stageKey, candidates)` |

- 옛 상점 데이터의 `shopType`(1)은 Core Spec에 없다. 옮기지 않는다(P7). 필요하면 §7-5에서 다시 본다.
- 회복 옵션의 표시 이름("체력 10 회복")은 ViewGroup으로 간다(§4).

### 3.5 Loadout

| 테이블 | 변경 | Spec 대응 |
|---|---|---|
| PlayerLoadout | 🆕 `MainCharacterKey` (SO는 `UnitDataSO` 참조, Role == MainCharacter 검증) | `StageEntryRequest.mainCharacterSpec` |
| PlayerLoadout | `StartingHandSize` 유지 | `RunCharacterDeckSpec.initialDrawCount = StartingHandSize - 1`(옛 어댑터 규칙. 메인 캐릭터가 한 칸을 차지) |
| PlayerLoadout | 옛 `unitRoster` 참조는 **옮기지 않음** | Spec에 없음. §7-5 |
| EnemyLoadout, UnitRoster | 스키마 변경 없음 | – |

### 3.6 새 InGame 테이블

| 테이블 | 형태 | 필드 | Spec 대응 |
|---|---|---|---|
| `CategoryConfig` | 설정형 | `HeavyDamageReductionPercent`(1~100), `HeavyInitialFacing`(`Rotation4`) | `CategorySpec[]`: Base(규칙 없음), Rider(ManeuverRefund), Heavy(DirectionalArmor), Cannon(SafeBombardment). 조립은 Core |
| `StageEconomyTable` | 목록형 (키 EconomyKey) | `InitialGold`, `ManualDrawGoldCost` | `StageEconomySpec` |
| `RecoveryOptionTable` | 목록형 (키 OptionId) | §3.4 | `RecoveryOptionSpec` |
| ~~`RunEconomyConfig`~~ | 설정형 | `InitialGlobalGold` | Core Spec 없음. **E7로 제외** |

## 4. ViewGroup 변경

| 테이블 | 변경 | 값 출처 |
|---|---|---|
| `UnitVisual` | 🆕 `timelineIcon`, `infoIcon` (Sprite). 한글 표시 이름 | UnitVisualCatalog, 유닛 displayName |
| `StageFieldView` | 🆕 `alternateCellPrefab`, `cellPaddingX`, `cellPaddingY`(0.15) | FieldPrefab. 선행 설계의 "간격은 cellPrefab 크기로 계산"을 이 두 필드로 보정 |
| `StageMapView` | 🆕 `markerMoveDuration`(0.6), `markerJumpHeight`(0.8) | StageMapViewConfig |
| `StageNodeVisual` | 비전투 항목 추가(회복·상점). 키 문제는 §7-3 | StageMapVisualCatalog |
| `StageViewProp` | 🆕 `positionOffset`, `rotationOffset` | Stage의 background props |
| 🆕 `TraitVisual` (목록형, 키 `TraitKey`) | `displayName`, `icon`, `description` | TraitDescriptions |
| 🆕 `RecoveryOptionVisual` (목록형, 키 OptionId) | `displayName` | RecoveryOption |
| 🆕 `RailwayVisual` (설정형) | 열차·선로·타일 프리팹, 애니메이터·클립, 이펙트 타입, 이동 곡선 수치 | RailwayVisualConfig |

범위에서 뺀 것: 전투 연출 설정 9개(호진 소유), FieldCamera(다른 작업자), 아티팩트(Equipment), Render Material 테이블.

## 5. 값 이전

| 대상 | 방법 |
|---|---|
| 유닛 4종 (Akimbo·ShotGun·Sniper·Demon) | develop-4week 값으로 덮는다(차이 문서 §2.1) |
| 유닛 4종 추가 (CactusCannon·MainCharacter·Ballista·Gravekeeper) | 새 에셋. Trait은 새 Authoring 타입으로 직접 입력 |
| 스테이지 1101 | 필드 9x9, 적 본부 (4,7), 레일웨이 켬(5/4, y=4 9칸) |
| 스테이지 11개 공통 | NightLength 500, EconomyKey 1, 메인 캐릭터 앵커(= 기존 플레이어 본부 좌표) |
| 스테이지 3101·3201 (새) | 회복·상점 Root + 상세. 맵 1 소속(옛 regularStages에 들어 있었음) |
| Enemy 1~5 / PlayerLoadout / UnitRoster | 차이 문서 §2.5 값 |
| 필드 12개, 나머지 스테이지 좌표 | 값이 같아서 그대로 |

**참조 에셋 문제:** develop-4week SO가 참조하는 아트 에셋 46개가 현재 저장소에 없다(차이 문서 §6). InGame 값은 모두 옮길 수 있지만, ViewGroup의 프리팹·아이콘 참조는 채울 수 없다(§7-4).

## 6. 영향

| 대상 | 영향 |
|---|---|
| Core 코드 | 현재 DB에서 Spec을 만드는 Core 코드는 없다(두 브랜치 모두 확인). 컴파일 영향 없음. **Core 전달 문서에 새 필드와 변환 규칙을 적는다** |
| 기존 에셋 | 이름 변경 필드는 `FormerlySerializedAs`로 유지. 제거 필드 `DirectionalAttackPattern`은 ShotGun·Sniper에 옛 값이 있지만, Core `AttackPatternSpec`이 셀 목록을 받지 않고 develop-4week도 이 필드를 없앴으므로 버린다 |
| 테스트 | `InGameDataTableTests`(새 필드 검증), `ViewDataTableTests`, `DataBaseAssetTests`(새 테이블·유닛 8종·비전투 스테이지 교차 검증) |
| 문서 | 선행 설계 §5·§6 표, 인계 문서, SO 검토 ①② 해결 표시 |

## 7. 결정해야 할 것

1. **`StageType`과 Core 구분의 관계.** 우리 `StageType`은 `Event/Combat/Shop`, Core는 `Combat/NonCombat` + `Recovery/Shop`이다.
   - (a) `Event`를 회복으로 쓴다. enum 값이 그대로라 에셋·Buffer B1·노드 외형 키 영향이 없다. 이름만 어색하다 **(권장)**
   - (b) `Recovery`를 새로 추가하고 `Event`는 남긴다.
   - (c) Core enum을 그대로 쓴다. `StageType`을 없애면 B1과 노드 외형 키를 다시 설계해야 한다.
2. **Role·Category 등 새 유닛 필드의 검증 규칙**을 Core에 확인한다. 예: 메인 캐릭터의 Owner, 카테고리 None 허용 여부. 확인 전에는 옛 `UnitAuthoringValidator` 규칙을 따른다.
3. **노드 외형 키.** develop-4week는 StageKey별 13개였지만 실제 외형은 일반·보스·회복·상점 4종뿐이고, 스테이지별로 다른 것은 표시 이름("Normal Stage A~J")뿐이다.
   - (a) 키 `(StageType, MapRole)`를 유지하고 비전투 2개를 추가한다. 스테이지별 이름은 잃는다 **(권장)**
   - (b) 키를 StageKey로 바꾼다. B1의 StageType·MapRole 필드가 노드 외형에는 필요 없어진다.
4. **없는 아트 에셋 처리.** 새 유닛 4종의 프리팹, 아이콘, 배경 소품, 레일웨이 프리팹이 없다.
   - (a) InGame만 먼저 옮기고, 해당 ViewGroup 항목은 에셋이 들어오면 채운다. 그 사이 `EveryUnitAndStructure_HasUnitVisual` 테스트가 실패한다.
   - (b) 없는 프리팹은 임시로 기존 프리팹(`TestUnit_Base` 등)을 넣고, 아이콘은 비워 둔다 **(권장)**
   - (c) 필요한 에셋만 develop-4week에서 골라 가져온다(합치지는 않음). 아트 담당 확인 필요.
5. **옮기지 않는 옛 필드.** 상점 `shopType`, PlayerLoadout의 `unitRoster` 참조. Core Spec에 없어서 빼는 것으로 제안한다. 쓰던 곳이 있으면 알려 달라.
6. **`shopBaseSeed` 출처.** `RunNonCombatSpec`이 요구하는데 develop-4week SO에 값이 없다(호출하는 쪽에서 넣었다). StageMap에 필드로 둘지, Core가 정할지.
7. **Run 지갑 초기 골드(`RunEconomyConfig`).** 대응하는 Core Spec이 없다. InGame에 두고 Core가 쓰게 할지, 이번 범위에서 뺄지.
8. **범위.** 이번에 전투 연출 설정 9개, 아티팩트, Render Material 테이블은 뺀다. 맞는지.

## 8. 결정 기록 (2026-09-29, 사용자 "권장대로")

| # | 결정 |
|---|---|
| E1 | `StageType.Event`를 **회복 스테이지**로 쓴다. enum 값은 바꾸지 않는다. `EventStageData`/`EventStageDetail` 이름도 유지하고 주석으로 "회복"임을 밝힌다 |
| E2 | 새 유닛 필드 검증은 옛 `UnitAuthoringValidator`와 Core Spec 생성자 검증을 따른다. 추가 규칙은 Core 확인 뒤(Core 전달 문서에 요청) |
| E3 | 노드 외형 키 `(StageType, MapRole)` 유지. 회복 `(Event, Regular)`, 상점 `(Shop, Regular)` 2개 추가. 스테이지별 표시 이름은 옮기지 않는다 |
| E4 | 없는 아트 에셋: **필수 프리팹은 기존 프리팹으로 임시 대체**, 아이콘(Sprite)은 비운다. 배경 소품(`StageViewProp`)은 필수가 아니라 항목을 만들지 않는다 |
| E5 | 옛 `shopType`, PlayerLoadout의 `unitRoster` 참조는 옮기지 않는다 |
| E6 | `shopBaseSeed`는 이번 범위에서 뺀다. Core가 정하도록 Core 전달 문서에 적는다 (권장 없던 항목 — 계획에서 정함) |
| E7 | `RunEconomyConfig`는 이번 범위에서 뺀다. 대응 Core Spec이 생기면 추가 (권장 없던 항목 — 계획에서 정함) |
| E8 | 전투 연출 설정 9개, 아티팩트, Render Material 테이블, FieldCamera는 범위 밖 |
