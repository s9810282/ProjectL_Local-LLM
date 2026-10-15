# DB 스키마 Core 정렬 — Task 계획

- 작성일: 2026-09-29
- 설계: [DB 스키마 Core 정렬 설계](../specs/2026-09-29-db-schema-core-alignment-design.md) (이하 "설계", 결정 E1~E8은 설계 §8)
- 값 출처: [develop-4week SO 차이](../specs/2026-09-29-develop-4week-so-diff.md) (이하 "차이 문서")
- 기준 브랜치: `feature/Yonggeun/DB_SO` (`f860909`)

**목표:** InGameGroup·ViewGroup 스키마를 현재 Core Contracts 입력 Spec에 맞추고, develop-4week 값으로 에셋을 갱신한다.

**진행 규칙**

- Phase 단위로 진행하고 **Phase가 끝나면 멈추고 보고한다.** 사용자가 확인·커밋한 뒤 다음 Phase로 간다.
- 커밋·push는 사용자가 한다.
- 설계와 다른 결정이 필요하면 구현하지 않고 먼저 묻는다.
- Core 로직(`CoreCaculator`, Core 시스템)은 건드리지 않는다. Core에 필요한 것은 Core 전달 문서에 적는다.
- `.cs`·에셋을 새로 만들 때 `.meta`를 같이 만든다. 이름을 바꾸는 직렬화 필드는 `[FormerlySerializedAs]`를 붙인다.
- 새 순수 Data 파일에는 `using UnityEngine`이 없어야 한다(P1).
- **작업 방식:** 코드 Task(Phase A·B·D의 테스트)는 기본이 **워크시트**(사용자가 직접 친다)다. 사용자가 "진행해줘"라고 하면 직접 고친다. 에셋 Task(Phase C)는 직접 한다.

## 0. 검증

| 검증 | 내용 | 누가 |
|---|---|---|
| V1 | Unity 에디터 컴파일 에러 0 | 사용자 |
| V2 | Test Runner → EditMode → `ProjectL.Tests.Framework` 전체 통과 | 사용자 |
| V3 | 새 순수 Data 파일에 `UnityEngine` 참조가 없는지 grep | Claude |
| V4 | 에셋 `m_Script` GUID ↔ 스크립트 `.meta`, 참조 GUID가 모두 해석되는지, 값 ↔ 차이 문서 | Claude |
| V5 | 인스펙터에서 새 필드·새 에셋 값 확인 | 사용자 |

`build-check.ps1`은 저장소에 없다(2026-09-29 확인). 컴파일 확인은 에디터로 한다.

## 전체 순서

```text
Phase A  InGame 스키마   T1 → T2 → T3 → T4 → T5 → T6 → T7   ─ 리뷰 A (V1, V2)
Phase B  View 스키마     T8 → T9 → T10                      ─ 리뷰 B (V1, V2)
Phase C  에셋 값         T11 → T12                          ─ 리뷰 C (V1, V2, V4, V5)
Phase D  테스트·문서     T13 → T14                          ─ 리뷰 D (최종)
```

**주의 — Phase A·B 동안 실제 에셋 테스트가 실패한다.** `DataBaseRegistry`는 Factory가 요구하는 테이블 에셋이 그룹에 없으면 실패한다. T7·T10에서 새 테이블을 Factory에 넣는 순간부터, 에셋을 만드는 Phase C까지 `Catalog_CreatesAllDataBaseGroups` 등 실제 에셋을 읽는 테스트는 실패한다. 리뷰 A·B에서는 컴파일과 **그 밖의** 테스트만 보고, 전체 통과는 리뷰 C에서 확인한다.

---

## Phase A. InGame 스키마 (설계 §3)

경로 접두사: `Assets/ProjectL/Script/Systems/DataBaseSystem/`

### T1. Unit (설계 §3.1)

- 파일: `Data/InGame/Table/Unit/Item/UnitData.cs`, `UnitDataSO.cs`
- [ ] `UnitData`: `Role`(`UnitRole`), `Category`(`CategoryKey`), `PhysicalDefense`, `CannonAreaType`(`CannonAreaType`), `CannonAreaRange`, `PierceRayCount` 추가
- [ ] `DirectionalAttackPattern` 제거 (Data·SO 모두)
- [ ] `Validate()`: Role·Category 정의 값, Category ≠ None, PhysicalDefense ≥ 0, AppointMethod가 Cross·None이면 거부, Category == Cannon일 때 CannonAreaType 정의 값·CannonAreaRange ≥ 0·PierceRayCount ∈ {1, 3}, PierceRayCount 3은 Pierce일 때만
- [ ] `UnitDataSO`: 같은 필드. 캐논 필드에 `[Tooltip]`("Category가 Cannon일 때만 사용")
- 주의: `Data.InGame` 아래라 `System.Enum.IsDefined`를 쓴다

### T2. Structure (설계 §3.2)

- [ ] `StructureData.Validate()`: 공격 가능하면 공격력 > 0

### T3. CategoryConfig 🆕 (설정형, 설계 §3.6)

- [ ] `Data/InGame/Table/Category/Item/CategoryConfigData.cs`(순수): `HeavyDamageReductionPercent`(1~100), `HeavyInitialFacing`(`Rotation4`)
- [ ] `.../Item/CategoryConfigDataSO.cs`, `.../CategoryConfigDataTableSO.cs`
- [ ] `Queries/InGameDataBase/InGameTable/CategoryConfigTable.cs`: 아이템 정확히 1개, `Config` 프로퍼티(설정형, View의 `StageFieldViewTable`과 같은 형태)

### T4. StageEconomy 🆕 + CombatStageDetail (설계 §3.3, §3.6)

- [ ] `Data/InGame/Table/Economy/Item/StageEconomyData.cs`(순수): `EconomyKey`, `InitialGold`(≥ 0), `ManualDrawGoldCost`(≥ 0)
- [ ] `StageEconomyDataSO.cs`(키 필드 `economyKey`), `StageEconomyDataTableSO.cs`, `Queries/.../StageEconomyTable.cs`(목록형, `Get`/`TryGet`)
- [ ] `CombatStageDetail`: `NightLength`(long, > 0), `EconomyKey`(≥ 1) 추가. `PlayerHeadquartersAnchor/Rotation` → `MainCharacterAnchor/Rotation`
- [ ] `CombatStageData`(SO): `nightLength`, `economy`(`StageEconomyDataSO` 참조 → 키, P3) 추가. `playerHeadquartersAnchor/Rotation` → `mainCharacterAnchor/Rotation`에 `[FormerlySerializedAs]`
- [ ] 검증: Economy 필수(develop-4week `StageEntryDataBuilder`와 같음)

### T5. 회복·상점 상세 + RecoveryOption 🆕 (설계 §3.4, E1)

- [ ] `Data/InGame/Table/Recovery/Item/RecoveryOptionData.cs`(순수): `OptionId`(≥ 1), `Cost`(≥ 0), `HealMode`(`RecoveryHealMode`), `HealValue`(Flat ≥ 1, PercentOfMax 1~100, Full은 무시)
- [ ] `RecoveryOptionDataSO.cs`, `RecoveryOptionDataTableSO.cs`, `Queries/.../RecoveryOptionTable.cs`(목록형)
- [ ] `EventStageData`(SO): `recoveryOptions`(`RecoveryOptionDataSO[]`) 추가, 클래스 주석에 "회복 스테이지(E1)"
- [ ] `EventStageDetail`: `RecoveryOptionKeys`(`IReadOnlyList<int>`), 검증: 1개 이상, 중복 없음
- [ ] `ShopStageData`(SO): `candidates`(`UnitDataSO` + `cost`) 추가
- [ ] `ShopStageDetail`: `Candidates`(`EntityKey`, `Cost`), 검증: 1개 이상, None 없음, Cost ≥ 0, 같은 유닛 중복 없음

### T6. PlayerLoadout (설계 §3.5)

- [ ] `PlayerLoadoutData`: `MainCharacterKey`(`EntityKey`) 추가, None 거부
- [ ] `PlayerLoadoutDataSO`: `mainCharacter`(`UnitDataSO` 참조 → 키)
- [ ] 메인 캐릭터의 Role == MainCharacter인지는 테이블 사이 검사라 T13의 에셋 교차 테스트에서 확인

### T7. InGame 등록

- [ ] `ServiceKeys/DataBaseGroupServiceKey.cs` `InGameTableKey`: `CategoryConfig`, `StageEconomy`, `RecoveryOption`
- [ ] `Queries/InGameDataBase/InGameDataBase.cs` `IInGameQuery`: 세 Query 추가
- [ ] `Factory/DBFactory/Group/InGameDataBaseFactory.cs`: 세 테이블 생성
- [ ] 기존 테스트가 새 필드 때문에 깨지는 곳 수정(`InGameDataTableTests`의 Unit·Combat·PlayerLoadout 생성 헬퍼)

**리뷰 A:** V1, V2(단, `Catalog_CreatesAllDataBaseGroups` 등 실제 에셋 테스트는 Phase C 전까지 실패 가능), V3

---

## Phase B. View 스키마 (설계 §4)

### T8. 기존 View 테이블 필드 추가

- [ ] `UnitVisualData`: `timelineIcon`, `infoIcon`(Sprite, 선택)
- [ ] `StageFieldViewData`: `alternateCellPrefab`(선택), `cellPaddingX`, `cellPaddingY`(≥ 0)
- [ ] `StageMapViewData`: `markerMoveDuration`(> 0), `markerJumpHeight`(≥ 0)
- [ ] `StageViewPropData`: `positionOffset`, `rotationOffset`(Vector3)

### T9. 새 View 테이블

- [ ] `Data/View/Table/Trait/`: `TraitVisualData` + `TraitVisualDataSO` + `TraitVisualDataTableSO`(목록형, 키 `TraitKey`, `displayName`, `icon`, `description`). None·중복 거부
- [ ] `Data/View/Table/Recovery/`: `RecoveryOptionVisualData`(키 OptionId, `displayName`)
- [ ] `Data/View/Table/Railway/`: `RailwayVisualData`(설정형: `trainPrefab`, `railPrefab`, `railTilePrefab`, `controller`, `clip`, `effect`(EffectType), `clipEndZ`, `duration`, `trainWidthInCells`, `outsideCells`, `travelPaddingCells`, `trainModelYaw`, `reverse`). 프리팹 3개 필수
- [ ] `Queries/ViewDataBase/ViewTable/`: `TraitVisualTable`, `RecoveryOptionVisualTable`, `RailwayVisualTable`

### T10. View 등록

- [ ] `ViewTableKey`, `IViewQuery`(`ViewDataBase.cs`), `ViewDataBaseFactory`에 세 테이블 추가

**리뷰 B:** V1, V2(Phase C 전 실패 가능 테스트는 A와 같음)

---

## Phase C. 에셋 값 (설계 §5, 차이 문서) — 직접 작업

경로 접두사: `Assets/ProjectL/DataBaseSO/`

### T11. InGameGroup 에셋

- [ ] 유닛 4종 값 덮기(Akimbo·ShotGun·Sniper·Demon), 4종 추가(CactusCannon 16·MainCharacter 14·Ballista 15·Gravekeeper 17). Trait은 새 Authoring 타입으로(ThornField 5/5, PiercingFalloff 30, Knockback 1) — **2026-10-02 View A.2에서 MainCharacter(14)만 추가.** 나머지 3종 추가와 4종 값 덮기는 미반영
- [ ] UnitRoster: Ballista·CactusCannon·Gravekeeper 추가
- [ ] EnemyLoadout 1~5: Gravekeeper·Ballista 추가(가중치 1), Enemy_1 시드 2
- [ ] PlayerLoadout_1: 메인 캐릭터 MainCharacter, 구성 Akimbo 1·ShotGun 1·Sniper 1·CactusCannon 1 — **2026-10-02 View A.2: 메인 MainCharacter 지정, 구성은 Akimbo·ShotGun·Sniper 각 1(3명)로 반영.** Core 규칙 `StartingSubCount = 3`과 맞추려고 CactusCannon은 넣지 않았다. 시작 서브 수 규칙은 Core 문의 중(`handoffs/2026-10-02-combat-view-core-notes.md` §3a)
- [x] `Category/`: CategoryConfig_Default(50, R0) + 테이블 (2026-10-02, View 계획 Phase A.1)
- [x] `Economy/`: StageEconomy_1(200, 40) + 테이블 (2026-10-02, View A.1)
- [x] `Recovery/`: RecoveryOption 1·2·3(Flat 10 비용 2 / Percent 30 비용 5 / Full 비용 10) + 테이블 (2026-10-02, View A.1)
- [ ] Combat 11개: `nightLength` 500, `economy` StageEconomy_1. Combat_Normal_A: 필드 9x9, 적 본부 (4,7), 레일웨이 켬(5/4, (0..8, 4)) — **앞의 두 값(11개 모두)은 2026-10-02 View A.1a에서 반영.** Combat_Normal_A의 필드·본부·레일웨이 변경은 미반영
- [ ] 새 스테이지: `Stage/Root/Stage_Recovery_A`(3101, 맵 1, Regular, → Event 상세) + `Stage/Event/Event_Recovery_A`(옵션 1·2·3), `Stage/Root/Stage_Shop_A`(3201, → Shop 상세) + `Stage/Shop/Shop_Shop_A`(Akimbo 2·ShotGun 3·Sniper 5). StageRootDataTable에 등록
- [x] InGameGroup에 새 테이블 3개 등록 (2026-10-02, View A.1. Unity 테스트 미실행)
- [ ] V4

### T12. ViewGroup 에셋 (E4: 없는 프리팹은 임시 대체)

- [ ] UnitVisual: 한글 이름(아킴보·샷건·저격수·Demon·선인장 캐논·주인공·Ballista·Gravekeeper), 새 유닛 4종 추가. 프리팹이 없는 유닛은 `Resources/Test/Model/Prefab/TestUnit_Base.prefab`, 아이콘은 비움 — **2026-10-02 View A.2에서 MainCharacter 외형만 추가(임시 프리팹, 영문 이름).** 한글 이름·나머지 3종은 미반영
- [ ] StageFieldView: `cellPaddingX/Y` 0.15, `alternateCellPrefab` 비움(`Cube (1)` 없음)
- [ ] StageMapView: marker 0.6 / 0.8
- [ ] StageNodeVisual: `(Event, Regular)` "Recovery Stage", `(Shop, Regular)` "Shop Stage". 프리팹은 `StageVisual_NormalPlaceholder`로 임시
- [ ] 🆕 TraitVisual 5개(집중·이동 후 공격 할인·관통 볼트·가시밭·넉백 + 설명), 아이콘 비움
- [ ] 🆕 RecoveryOptionVisual 3개(체력 10 회복 / 최대 체력 30% 회복 / 체력 모두 회복)
- [ ] 🆕 RailwayVisual 1개: 수치는 차이 문서 값, 프리팹 3개는 `Prefabs/Default/Cube.prefab` 임시, 컨트롤러·클립 비움
- [ ] StageViewProp: 항목 없음(E4)
- [ ] ViewGroup에 새 테이블 3개 등록
- [ ] V4

**리뷰 C:** V1, V2, V4, V5. 이때 `Catalog_CreatesAllDataBaseGroups`까지 통과해야 한다.

---

## Phase D. 테스트·문서

### T13. 테스트

- [ ] `InGameDataTableTests`: 새 Unit 검증(Cross 거부, Cannon 필드, Pierce 3), Structure 공격력, CategoryConfig 설정형, StageEconomy·RecoveryOption 목록형, Combat의 Economy·NightLength 필수, 회복·상점 상세 검증, PlayerLoadout 메인 캐릭터
- [ ] `ViewDataTableTests`: TraitVisual·RecoveryOptionVisual 목록형, RailwayVisual 설정형
- [ ] `DataBaseAssetTests`: 유닛 8종 모두 UnitVisual 있음, 비전투 스테이지도 노드 외형 있음, Combat의 EconomyKey·회복 옵션·상점 후보 키가 존재, PlayerLoadout 메인 캐릭터 Role == MainCharacter, 모든 TraitKey 사용 유닛의 TraitVisual 존재
- [ ] `DataBaseTestAssets` 헬퍼 갱신

### T14. 문서

- [ ] 설계 문서에 구현 결과(달라진 점)
- [ ] 선행 DB-SO 설계 §5·§6 표에 "Core 정렬 설계로 갱신" 표시
- [ ] Core 전달 문서: 새 필드·변환 규칙(설계 §2·§3), 확인 요청(E2 검증 규칙, E6 `shopBaseSeed`, E7 Run 지갑, 본부 → 메인 캐릭터 앵커)
- [ ] 로컬 세션 인계 문서, SO 검토 ①② 해결 표시

**리뷰 D (최종):** V1, V2 전체 통과

---

## 범위 밖 (E8)

전투 연출 설정 9개, 아티팩트(Equipment), Render Material 테이블, FieldCamera, `shopBaseSeed`(E6), Run 지갑 초기 골드(E7), 없는 아트 에셋 가져오기(E4).

## 실행 기록

### Phase A (T1~T7, 2026-09-29) — 완료, 리뷰 A 대기

- 작업 방식: 사용자 "PhaseA 진행해"로 직접 구현.
- 시작 전 확인: 호진 커밋 `8f38e2c1`(Artifact DB 이전 1차)이 이 브랜치에 들어와 있다. 겹치는 파일은 등록 3개와 `InGameDataTableTests`뿐이라 줄 추가로 합쳤다.
- T1 `UnitData`/`UnitDataSO`: Role·Category·PhysicalDefense·CannonAreaType·CannonAreaRange·PierceRayCount 추가, DirectionalAttackPattern 제거. 검증은 옛 `UnitAuthoringValidator` 규칙을 옮겼다: 메인 캐릭터 키↔역할 짝, 메인 캐릭터 제한(PlayerOnly, 1x1, RadialSquare(1)/SingleTarget, Trait 없음), 캐논이 아니면 PierceRayCount 1, 그리고 **Contracts의 `AttackPatternSpec.Create*`·`ImpactPatternSpec`·`AttackProfileCompatibility.GetSupportedAimKind`로 Core가 받는 조합인지 확인**. develop-4week 유닛 8종은 모두 이 조합을 통과한다(설계 단계에서 확인).
  - 현재 Contracts의 `CannonAreaType`에는 None이 없다(develop-4week 에셋은 -1). 새 SO는 기본값 Square이고 캐논이 아니면 무시한다.
  - 새 필드는 SO 초기값(Regular, Base, 0, Square, 0, 1)이 있어서 **기존 유닛 에셋 4개도 그대로 검증을 통과한다.**
- T2 `StructureData`: 공격 가능하면 공격력 ≥ 1.
- T3 🆕 `Table/Category/` + `CategoryConfigTable`(설정형).
- T4 🆕 `Table/Economy/` + `StageEconomyTable`(목록형). `CombatStageDetail`에 `EconomyKey`(필수)·`NightLength`(long, ≥ 1) 추가, `PlayerHeadquartersAnchor/Rotation` → `MainCharacterAnchor/Rotation`(SO는 `[FormerlySerializedAs]`). Core를 보니 플레이어 본부는 배치 단계에서 플레이어가 놓고(`TryPlaceInitialPlayerHeadquarters`), 데이터에서 받는 위치는 `MainCharacterEntrySpec`이라 이름 변경이 맞다. `nightLength`는 `long`이라 `[Min]`을 붙이지 않았다(Unity 미지원).
- T5 🆕 `Table/Recovery/` + `RecoveryOptionTable`(목록형). Flat 회복량은 Core 기준 ≥ 0(계획의 ≥ 1에서 Core에 맞춤). `EventStageData`(회복, E1)에 `recoveryOptions`, `ShopStageData`에 `candidates`. 상세 검증은 Core `RecoveryStageSpec`·`ShopStageSpec`과 같다.
- T6 `PlayerLoadoutData`: `MainCharacterKey`(필수), 같은 유닛 중복 금지(Core `RunCharacterDeckSpec`과 같음).
- T7 `InGameTableKey`·`IInGameQuery`·`InGameDataBaseFactory`에 3개 등록. `InGameDataTableTests` 수정(방향 패턴 제거, 메인 캐릭터·경제·카테고리 추가, 새 테이블을 그룹에 포함).
- `.meta`: 새 `.cs` 12개, 폴더 6개.
- V3: 새·수정 순수 Data 9개에 `UnityEngine` 0건. 옛 이름(`PlayerHeadquartersAnchor`, `DirectionalAttackPattern`) 코드 잔여 0건.
- **Phase C까지 실패가 예상되는 테스트:** 실제 에셋을 읽는 `DataBaseAssetTests` 전체와 `ArtifactDataTableTests`의 Catalog 테스트(새 테이블 에셋 없음, 전투 스테이지 Economy 없음).

### Phase B (T8~T10, 2026-09-29) — 완료, 리뷰 B 대기

- 작업 방식: 사용자 "Phase B 진행해"로 직접 구현. Phase A는 아직 커밋 전이다(작업 트리에 같이 있음).
- T8 기존 View 테이블
  - `StageFieldViewData`: `alternateCellPrefab`(선택, 있으면 Renderer 필수), `cellPaddingX/Y`(≥ 0). 클래스 주석의 "간격은 cellPrefab 크기로 계산"을 "크기는 cellPrefab, 간격은 padding"으로 고쳤다.
  - `StageMapViewData`: `markerMoveDuration`(> 0, 기본 0.6), `markerJumpHeight`(≥ 0, 기본 0.8).
  - `UnitVisualData`: `timelineIcon`, `infoIcon`(Sprite, 선택).
  - `StageViewPropData`: `positionOffset`, `rotationOffset`(Vector3).
  - 새 필드는 모두 기본값이 있어서 기존 View 에셋은 그대로 검증을 통과한다.
- T9 🆕 `Table/Trait/`(`TraitVisualData`: 키 TraitKey, 이름·아이콘·설명), `Table/Recovery/`(`RecoveryOptionVisualData`: 키 OptionId, 이름 필수), `Table/Railway/`(`RailwayVisualData`, 설정형) + Query 3개.
  - **계획과 달라진 점:** `RailwayVisualData.effect`는 **Empty를 허용**한다. 현재 Render `EffectType`에는 `Empty`·`TestFire`만 있고 develop-4week의 `Train`이 없다(Render 이펙트는 E8로 범위 밖). 컨트롤러·클립은 계획대로 선택 항목이다. 수치 검증은 develop-4week `RailwayVisualConfigSO`와 같다.
- T10 `ViewTableKey`·`IViewQuery`·`ViewDataBaseFactory`에 3개 등록. `ViewDataTableTests`는 테이블을 하나씩 만들어서 수정할 곳이 없었다.
- `.meta`: 새 `.cs` 9개, 폴더 6개.
- **작업 중 발견(내 작업 아님):** 작업 트리에서 옛 에셋 폴더 `Assets/ProjectL/DataBaseSO/Stage/` 전체(에셋 42개 + meta, 98개 항목)가 삭제된 상태다. 커밋되지 않았다. 되돌리지 않고 두었다.

### Phase C T12 일부 — ViewGroup 새 테이블 3개 (2026-09-30)

- 작업 방식: 입력 작업 중 사용자 요청("ViewGroup에 빠진 테이블 3개 채워줘")으로 직접 작성했다. 에셋 YAML을 직접 썼다.
- `DataBaseSO/ViewGroup/TraitVisual/`: 테이블과 아이템 5개(Focus 10, MoveAttackDiscount 20, PiercingFalloff 30, ThornField 40, Knockback 50).
  - 이름·설명은 develop-4week `TraitDescriptions` 값을 그대로 옮겼다.
  - 아이콘은 비웠다(E4).
- `DataBaseSO/ViewGroup/RecoveryOptionVisual/`: 테이블과 아이템 3개(1 "체력 10 회복", 2 "최대 체력 30% 회복", 3 "체력 모두 회복").
- `DataBaseSO/ViewGroup/RailwayVisual/`: 테이블과 아이템 1개.
  - 수치는 develop-4week `RailwayVisualConfig`에서 가져왔다: clipEndZ -250, duration 3, trainWidthInCells 0.8, outsideCells 16, travelPaddingCells 18, trainModelYaw 180, reverse 0.
  - 프리팹 3개는 `ProjectL/Prefabs/Default/Cube.prefab`으로 임시 대체했다.
  - 컨트롤러·클립은 비웠다. 이펙트는 Empty다(옛 값 Train이 현재 `EffectType`에 없음).
- `ViewGroup.asset` 테이블 목록: 6 → 10개. 입력 설정 테이블을 포함하며, 팩토리 요구 수와 같다.
- **남은 T12 항목은 하지 않았다:** UnitVisual 한글 이름·새 유닛 4종, StageFieldView·StageMapView 새 필드 값, StageNodeVisual 비전투 2개.
- **T11(InGameGroup)도 아직이다.** `InGameGroup.asset`은 테이블 9개이고 팩토리는 12개를 요구한다(CategoryConfig·StageEconomy·RecoveryOption 없음). 그래서 `Catalog_CreatesAllDataBaseGroups`는 여전히 실패한다.
