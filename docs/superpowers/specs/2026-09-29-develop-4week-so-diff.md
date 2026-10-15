# develop-4week SO ↔ 새 DB 구조 차이

> 팀이 실제로 플레이하던 `develop-4week`의 SO 데이터와, DB-SO 재구성으로 만든 새 InGameGroup/ViewGroup을 비교한다.
> DB-SO 재구성은 더 옛 develop `d852362`의 에셋 55개를 기준으로 했기 때문에 둘 사이에 차이가 크다.

- 작성일: 2026-09-29
- 작성: View 작업
- 비교 대상
  - develop-4week: `origin/develop-4week` `ae07af37`(2026-09-22), `Assets/DataBase/SO` **109개**
  - 새 구조: `feature/Yonggeun/DB_SO` `f860909`, `Assets/ProjectL/DataBaseSO`의 InGameGroup·ViewGroup (새 에셋 71개)
- 방법: 에셋 YAML의 필드 키와 값을 직접 비교했다. 참조는 GUID를 경로로 풀었다. 스크립트 소스가 아니라 **실제 저장된 값** 기준이다.

## 1. 요약

| 묶음 | 에셋 수 | 새 구조 대응 | 상태 |
|---|---|---|---|
| A. 대응 테이블이 있는 Stage SO | 48 | 있음 | **스키마 뒤처짐 + 값 다름** (§2) |
| B. 대응이 없는 Stage SO | 4 | 없음 | 카테고리, 특성 설명, 스테이지 경제, 레일웨이 외형 (§3) |
| C. 전투 연출 설정 SO | 9 | 없음 | 모션·발사체·무기 설정. 호진 작성 (§4) |
| D. 옛 Framework DB 그룹 | 48 | 일부만 | Stage(전투·회복·상점)·Equipment(아티팩트)·InGame(로드아웃·경제)·Render (§5) |
| 합계 | 109 | | |

가장 큰 문제 두 가지:

1. **스키마가 뒤처졌다.** 유닛에 6개 필드, 스테이지에 7개 이상 필드가 새 구조에 없다. SO 검토 ①(UnitData에 Role·Category·방어력 없음)이 여기서 나온 것이다.
2. **참조하는 아트 에셋 46개가 현재 저장소에 없다**(§6). develop-4week가 develop에 합쳐지지 않았기 때문이다. 이것 없이는 외형 값을 옮길 수 없다.

## 2. 대응 테이블이 있는 SO (48개)

표기: **추가** = develop-4week에만 있는 필드, **제거** = develop-4week에서 없어진 필드.

### 2.1 Unit (8개, 새 구조 4개) → `UnitTable` + `UnitVisualTable`

스키마:

| 필드 | develop-4week | 새 구조 | 분류 |
|---|---|---|---|
| `role` | 추가 (0 일반, 1 메인 캐릭터) | 없음 | I |
| `category` | 추가 (10/20/30/40) | 없음 | I |
| `physicalDefense` | 추가 (모두 0) | 없음 | I |
| `cannonAreaType`, `cannonAreaRange` | 추가 (캐논·발리스타 범위) | 없음 | I |
| `pierceRayCount` | 추가 (발리스타 3) | 없음 | I |
| `directionalAttackPattern` | **제거** | 있음 | I |
| `displayName` | 한글로 바뀜 | UnitVisual로 분리됨 | V |

값 (★ = 새 구조 값과 다름, 새 = 새 구조에 없는 유닛):

| 필드 | Akimbo | ShotGun | Sniper | Demon | CactusCannon 새 | MainCharacter 새 | Ballista 새 | Gravekeeper 새 |
|---|---|---|---|---|---|---|---|---|
| key | 10 | 11 | 12 | 13 | 16 | 14 | 15 | 17 |
| 표시 이름 | 아킴보★ | 샷건★ | 저격수★ | Demon | 선인장 캐논 | 주인공 | Ballista | Gravekeeper |
| role | 0 | 0 | 0 | 0 | 0 | **1** | 0 | 0 |
| owner | Player | Player | Player | Enemy | Player | Player | Enemy | Enemy |
| maxHealth | 10 | **45**★ | 10 | **10**★ | 20 | 50 | 10 | 10 |
| moveRange / moveCost | 5/1 | **5**★/1 | **5**★/1 | **5**★/1 | 5/1 | 5/1 | 5/**3** | 5/1 |
| appointMethod | 6 | 8 | 6 | 6 | 6 | 6 | 4 | 2 |
| attackRange | 1 | 2 | **3**★ | 1 | 3 | 1 | 99 | 1 |
| attackTargetMode | 1 | 2 | 1 | 1 | 3 | 1 | 3 | 2 |
| category | 10 | 30 | 10 | 10 | 40 | 10 | 40 | 20 |
| attackPower | **6**★ | **7**★ | **20**★ | **10**★ | 15 | 1 | 10 | 10 |
| cannonArea type/range | -1/0 | -1/0 | -1/0 | -1/0 | 0/1 | -1/0 | 2/99 | -1/0 |
| pierceRayCount | 1 | 1 | 1 | 1 | 1 | 1 | 3 | 1 |
| maxActionCost / attackCost | 5/2 | **5/2**★ | **5/4**★ | **5/2**★ | 5/4 | 5/1 | 5/4 | 5/2 |
| priority / delay | 10/10 | 10/**35**★ | **5/50**★ | **30/35**★ | 9/50 | 30/20 | 30/35 | 30/35 |
| Trait | MoveAttackDiscount(1,1) | – | Focus(1,3) | – | ThornField(5,5) | – | PiercingFalloff(30) | Knockback(1) |

### 2.2 Structure (1개) → `StructureTable`

스키마 같음(`displayName`만 UnitVisual로 분리). 값 같음(체력 3, 공격 불가).

### 2.3 Field (12개) → `FieldTable`

스키마 같음(`fieldVisual`만 제거). **값 12개 모두 같음.**

### 2.4 Stage (13개, 새 구조 11개) → `StageTable`(Root + CombatStageData)

스키마:

| 필드 | 분류 | 비고 |
|---|---|---|
| `executionKind`, `nonCombatKind`, `shopType` | I | 전투/비전투 구분, 회복/상점 구분. 새 구조는 `StageType`(Event/Combat/Shop)과 상세 SO로 나눴다 |
| `categoryCatalog` | I | CategoryCatalog SO 참조 (§3) |
| `economy` | I | StageEconomy SO 참조 (§3) |
| `nightLength` | I | 모두 500 (상점 1000) |
| `mainCharacterAnchor`, `mainCharacterRotation` | I | **`playerHeadquartersAnchor`를 대체**. 좌표 값은 같다 |
| `backgroundPrefab`, `backgroundPropsPrefab`, `backgroundPropsPositionOffset/RotationOffset` | V | 전투 11개 모두 `MapProp/MAP 1.prefab`, offset 0 |
| `railway.visuals` | V | RailwayVisualConfig 참조 (§3) |

값 차이:

| 스테이지 | 차이 |
|---|---|
| 1101 (A) | 필드 7x6 → **9x9**, 적 본부 (3,5) → **(4,7)**, 레일웨이 **켜짐**(첫 발동 5턴, 반복 4턴, 선로 y=4 9칸) |
| 1102~1110, 2101 | 기존 값 같음. 새 필드만 추가 |
| 3101 Recovery_A (새) | 비전투(회복). 필드·로드아웃 없음, nightLength 1 |
| 3201 Shop_A (새) | 비전투(상점), shopType 1, nightLength 1000 |

### 2.5 Loadout·Roster (7개)

| 에셋 | 차이 |
|---|---|
| Enemy_1~5 | 적 구성에 **Gravekeeper, Ballista 추가**(Demon과 가중치 1씩). Enemy_1 시드 1 → 2. 나머지 spawnCount·시드 같음 |
| PlayerLoadout | **`mainCharacter`(MainCharacter), `unitRoster` 참조 추가**. 구성 Akimbo 2 → **1**, **CactusCannon 1 추가** |
| UnitRoster | **Ballista, CactusCannon, Gravekeeper 추가**(MainCharacter는 없음) |

### 2.6 StageMap (1개) → `StageMapTable`

값 같음. 옛 `regularStages` 목록에 **Recovery_A, Shop_A가 들어 있다.** 새 구조는 StageRoot의 `mapKey`로 역참조하므로, 3101·3201도 Root 에셋(mapKey 1)이 있어야 맵에 나온다.

### 2.7 View 설정 (5개)

| develop-4week | 새 구조 | 차이 |
|---|---|---|
| FieldPrefab + StageSceneViewConfig | `StageFieldView` | **추가:** `alternateCellPrefab`(`Board/Cube (1).prefab`), `cellPaddingX/Y` 0.15 |
| StageMapViewConfig | `StageMapView` | **추가:** `markerMoveDuration` 0.6, `markerJumpHeight` 0.8 |
| StageMapVisualCatalog | `StageNodeVisual` | **키가 다름.** develop-4week는 **StageKey별** 13개(회복·상점 전용 프리팹 포함). 새 구조는 설계 D에 따라 `(StageType, MapRole)` 2개 |
| UnitVisualCatalog | `UnitVisual` | **추가:** `timelineIcon`, `infoIcon`. 항목 5 → 9개. 프리팹 변경: Demon `TestUnit_Base` → `Enemy.prefab`, 본부 `Default/Cube` → `Units/Base.prefab`, 새 유닛 4종 프리팹 |
| FieldCamera | (다른 작업자) | safeViewport 높이 0.75 → 0.65, fieldPadding 1 → 0.5 |

## 3. 대응이 없는 Stage SO (4개)

| 에셋 | 작성 | 내용 | 분류 → 제안 위치 |
|---|---|---|---|
| CategoryCatalog | 용근·천명 | heavyDamageReductionPercent 50, heavyInitialFacing 0 | I → InGame 설정형 테이블 |
| StageEconomy_Default | 용근 | initialGold 200, manualDrawGoldCost 40 | I → InGame (Stage가 참조) |
| TraitDescriptions | 용근 | TraitKey별 이름·아이콘·설명 5개(집중, 이동 후 공격 할인, 관통 볼트, 가시밭, 넉백) | V → ViewGroup 목록형(키 TraitKey) |
| RailwayVisualConfig | 호진 | 열차·선로·타일 프리팹, 애니메이터·클립, 이펙트 2, 이동 곡선 값 | V → ViewGroup 설정형 |

## 4. 전투 연출 설정 SO (9개, 모두 호진 작성)

| 에셋 | 내용 |
|---|---|
| UnitMotionSettings, BallistaMotionSettings, CactusCannonMotion | 반동·발사·피격 시간과 각도 (유닛별 3종) |
| BulletPresentation, BallistaBulletPresentation | 발사체 프리팹, 속도, 높이 |
| BallistaWeaponSettings | 시위 당김·재장전 시간 |
| MortarWeaponSettings, MortarPresentation | 박격포 각도·시간, 포탄 프리팹·비행 |
| ThornFieldPresentation | 가시밭 프리팹 |

모두 V다. ViewGroup에 둘지, 프리팹·컴포넌트 설정으로 둘지는 호진과 정한다.

## 5. 옛 Framework DB 그룹 (48개)

develop-4week는 새 구조와 **별도의** Framework DB를 쓰고 있었다. Catalog에 그룹 4개가 있다: Render, Stage, InGame, Equipment.

| 그룹 | 에셋 | 작성 | 내용 | 새 구조와의 관계 |
|---|---|---|---|---|
| **InGame** | 6 | JMH·호진 | StageLoadoutTable(키 "default" → PlayerLoadout), RunEconomyTable(initialGlobalGold 10) | **이름 충돌.** 새 `InGameGroup`과 이름이 같고 내용이 다르다 |
| **Stage** (NewStage) | 20 | 호진 | Battle: StageContent 11개(StageDefinition + UnitVisualCatalog + ViewConfig + battleType 1/3) / Event: 회복 옵션 3개(체력 10 회복 2골드, 최대 체력 30% 5골드, 모두 회복 10골드) / Shop: Shop_1(아킴보 2, 샷건 3, 저격수 5) | 새 구조에 회복·상점 데이터 자리가 없다(Shop/Event 상세 SO는 필드가 비어 있음) |
| **Equipment** (NewArtifact) | 7 | 호진 | 아티팩트 5개: 회복 성배(1001), 가시심장(1002), 영혼 드랍(1003), LastResistance(1004), Rider(1011, 테스트). 효과 트리거·조건 구조 포함 | 새 구조에 없음 |
| **Render** | 14 | – | Effect(+TrainEffect), Light(+DirLight), **MaterialTable**(Outline + 아티팩트 머티리얼 4) | develop의 Render에는 Material 등록이 주석 상태(JMH 문서 DR-003). develop-4week는 이미 채워 둠 |
| Catalog | 1 | – | 위 4개 그룹 | 새 Catalog는 Render·InGame·View |

## 6. 현재 저장소에 없는 참조 에셋 (46개)

develop-4week의 SO가 참조하지만 현재 브랜치(develop 기준)에 **없는** 에셋이다. 값을 옮기려면 이 에셋이 먼저 들어와야 한다.

| 종류 | 예 |
|---|---|
| 유닛 프리팹 7 | `Prefabs/Stage/Units/` Base, Enemy, EnemyUnit_Ballista, EnemyUnit_UnderTaker, StageUnit_CatusCannon 등 |
| 아이콘 스프라이트 20 | `Sprites/TimelineIcon/*`, `Sprites/AlphaIcon/*`, `Sprites/Icon/*`(특성 아이콘) |
| 스테이지 외형 | `MapProp/MAP 1.prefab`, `StageSelect/Visuals/StageVisual_Recovery/Shop`, `Board/Cube (1).prefab` |
| 연출 | Bullet, BallistaBolt, CactusBomb, ThornCactus, Train·TrainLoad·TrainTile, TrainEffect, 열차 애니메이션 2 |
| 아티팩트 | `Prefabs/Artifacts/*` 3, `Materials/Artifacts/*` 4 |

## 7. 결정해야 할 것

1. **develop-4week를 develop에 어떻게 합칠지.** §6의 아트 에셋과 §5의 옛 DB가 develop에 없다. 이게 먼저 정해져야 값을 옮길 수 있다. 팀 차원의 결정이다(JMH·호진).
2. **InGame 스키마 확장.** Unit 6필드, Stage 7필드(경제·밤·카테고리·메인 캐릭터 앵커·비전투 구분), PlayerLoadout의 메인 캐릭터. 의미와 검증 규칙은 Core가 정해야 한다(Core 전달 문서에 추가할 항목).
3. **본부 → 메인 캐릭터.** develop-4week는 플레이어 본부 앵커를 메인 캐릭터 앵커로 바꿨다. 새 구조의 `CombatStageDetail.PlayerHeadquartersAnchor`를 어떻게 할지.
4. **회복·상점 스테이지.** 새 구조의 Event/Shop 상세 SO가 비어 있다. 옛 NewStage의 Event·Shop 테이블을 어디로 옮길지(InGame 상세 SO에 필드 추가 vs 별도 테이블).
5. **노드 외형 키.** develop-4week는 StageKey별, 새 구조는 (StageType, MapRole). 스테이지마다 다른 외형이 필요하면 설계 D를 다시 봐야 한다.
6. **아티팩트, 연출 설정, 옛 Stage/Equipment 그룹의 소유자.** 대부분 호진이 작성했다. 새 구조로 옮길 주체를 정한다.
7. **새 구조에 이미 옮긴 값은 버리고 develop-4week 값으로 덮을지.** 필드 12개와 스테이지 10개는 값이 같아서 그대로 둬도 된다.
