# DB-SO 재구성 구현 계획 — InGameGroup / ViewGroup

- 작성일: 2026-09-24
- 설계: [DB-SO 재구성 설계](../specs/2026-09-24-db-so-ingame-view-group-design.md) (이하 "설계")
- 참고: [고아 에셋 인벤토리](../specs/2026-09-24-view-db-orphan-asset-inventory.md), [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)
- 기준 브랜치: `develop`

**목표:** 옛 SO 데이터를 새 Framework/Systems 구조의 InGameGroup(순수 C# 규칙)과 ViewGroup(표현)으로 복구한다.

**진행 규칙**

- Phase 단위로 진행한다. **각 Phase가 끝나면 멈추고 보고한다(리뷰 지점).** 사용자가 확인하고 직접 커밋한 뒤 다음 Phase로 넘어간다.
- 커밋과 push는 하지 않는다(D17).
- 옛 에셋 48개는 삭제하지 않는다.
- 설계와 다른 결정이 필요하면 구현하지 않고 먼저 묻는다.
- `.cs`를 새로 만들거나 옮길 때는 `.meta`를 같이 만들거나 옮긴다. 옮길 때는 GUID를 유지한다.

## 0. 검증 방법 (모든 Task 공통)

작업 환경에서 할 수 있는 검증이다(설계 §11.1). Unity 에디터 확인은 사용자가 한다.

| 검증 | 내용 | 적용 |
|---|---|---|
| **V1** | 순수 Data 파일 + Contracts 타입 파일 + `IData` stub만으로 컴파일. UnityEngine 없이 컴파일되면 P1 충족 | 순수 Data를 만든 Task |
| **V2** | UnityEngine·Framework stub + 변경한 실제 소스를 함께 컴파일해서 타입·시그니처·using 오류 확인 | 코드 Task 전부 |
| **V3** | 에셋의 `m_Script` GUID ↔ 스크립트 `.meta`, 에셋 값 ↔ 인벤토리 교차 확인 | 에셋 Task |
| **V4** | 옛 이름·네임스페이스 참조가 남았는지 grep | 이동·이름 변경 Task |

도구: Mono(`mono-devel`) + Roslyn(NuGet `Microsoft.Net.Compilers.Toolset` 4.8.0, `-langversion:9.0`), 참조 어셈블리는 .NET Standard 2.1(NuGet `NETStandard.Library.Ref` 2.1.0, Unity와 같은 계열). 테스트는 NUnit 3.13.3 + NUnitLite로 Mono에서 실행한다. 작업 환경이 재시작되면 다시 설치한다(T0).

| 검증 | 추가 |
|---|---|
| **V5** | Unity 없이 실행 가능한 EditMode 테스트를 Mono에서 실제로 실행한다. stub의 `ScriptableObject.CreateInstance`가 객체를 생성하므로 SO 기반 테이블 테스트도 실행된다. Unity 네이티브 객체가 필요한 테스트는 제외한다. |

## 전체 순서

```text
Phase 0  준비            T0
Phase A  기존 코드 정리   T1 → T2 → T3                       ─ 리뷰 A
Phase B  InGame 신규 테이블 T4 → T5 → T6 → T7 → T8 → T9      ─ 리뷰 B
Phase C  ViewGroup 코드   T10 → T11 → T12 → T13 → T14 → T15  ─ 리뷰 C
Phase D  Buffer           T16                                 ─ 리뷰 D
Phase E  에셋             T17 → T18                           ─ 리뷰 E
Phase F  테스트·마무리    T19 → T20                           ─ 리뷰 F (최종)
```

런타임 주의: `DataBaseFactory.Create`는 Catalog에 InGameGroup/ViewGroup 에셋과 필수 테이블이 모두 있어야 성공한다. **실제 DB 생성은 Phase E 이후에야 동작한다.** (2026-09-25 Phase E에서 에셋을 추가했다. 에디터 확인 필요)

---

## Phase 0. 준비

### T0. 검증 도구 준비

- [x] `mono-devel` 설치, Roslyn 패키지를 scratchpad에 받아 풀기
- [x] V2용 stub 파일 작성 (scratchpad, 저장소에 넣지 않음)
  - UnityEngine: `ScriptableObject`, `MonoBehaviour`, `GameObject`, `Sprite`, `Material`, `LineRenderer`, `Renderer`, `Color`, `Vector3`, `LayerMask`, `SerializeField`, `SerializeReference`, `Min`, `Range`, `Header`, `Tooltip`, `CreateAssetMenu`, `MovedFrom` 등
  - Framework: stub 없이 실제 소스 전체 사용
- [x] 수정 전 기준 컴파일: 현재 코드의 Stage/Map 관련 파일로 V2를 돌려 stub이 충분한지 확인

완료 기준: 현재 코드(수정 전)로 V2가 통과한다.

---

## Phase A. 기존 코드 정리 (설계 §9.1 M1~M4, M7 일부)

### T1. StageType / StageMapRole 이동 (M1)

- [x] `Data/Common/StageType.cs`, `StageMapRole.cs` 생성 (네임스페이스 `ProjectL.Systems.DataBaseSystem.Data.Common`)
- [x] `StageRootDataSO.cs`에서 두 enum 제거
- [x] using 추가: `StageBaseData.cs`, `Combat/Shop/EventStageData.cs`, `StageServiceCollection.cs`, `StageServiceManager.cs`, `StageGeneratorSystem.cs`

검증: V2, V4(옛 위치 참조 없음). 완료 기준: 두 enum이 Common에만 있고 모든 사용처가 컴파일된다.

### T2. StageMap P2 분리 (M2, M7a)

- [x] `Map/Item/StageMapData.cs` 신규: 순수 불변 클래스 + `StageConnectionAlgorithm` enum + `Validate()`
- [x] `Map/Item/StageMapDataSO.cs`: 입력 필드를 SO로 옮기고 `GetData()`에서 새 Data 생성
- [x] `StageDataTableTests.cs`: 값을 SO 필드에 넣고, `SameAs` 대신 키 값으로 비교

검증: V1(StageMapData), V2. 완료 기준: `StageMapTable`과 `StageGeneratorSystem`이 수정 없이 컴파일된다(프로퍼티 이름 유지).

### T3. StageRoot / StageBase P2 분리 (M3, M4)

- [x] `Stage/Item/StageRootData.cs` 신규: `IStageData : IData`, 순수 불변 `StageRootData`(`StageData` 프로퍼티 타입 = `IStageData`)
- [x] `Stage/Item/StageRootDataSO.cs`: 입력 필드(stageKey, mapKey, mapRole, stageData SO 참조), `GetData()`에서 상세를 순수 Data로 변환
- [x] `Stage/StageBaseData.cs`: `GetData() => CreateStageData()`, `abstract IStageData CreateStageData()`
- [x] `Combat/Shop/EventStageDetail.cs` 신규(순수), 각 SO의 `CreateStageData()` 구현

검증: V1(StageRootData, *Detail), V2(`StageTable`, `StageServiceCollection`, `StageServiceManager` 포함). 완료 기준: `StageServiceCollection` 수정 없이 컴파일된다.

> **리뷰 A** — 기존 코드 변경 보고. 사용자 확인·커밋 후 진행.

---

## Phase B. InGameGroup 신규 테이블 (설계 §4.3, §5)

모든 테이블 공통 산출물: 순수 Data(`Item/XxxData.cs`), 입력 SO(`Item/XxxDataSO.cs`), Table SO(`XxxDataTableSO.cs`), Query(`Queries/InGameDataBase/InGameTable/XxxTable.cs`), 키(`InGameTableKey.Xxx`), `IInGameQuery` 프로퍼티, `InGameDataBaseFactory` 등록.

### T4. CellInput

- [x] `Data/InGame/Common/CellInput.cs`: `[Serializable]` struct(x, y), `ToCellCoord()` (설계 §4.3)

검증: V2.

### T5. FieldTable (설계 §5.4)

- [x] 키 `FieldId`(string). 필드: 열·행, reserveCapacity, 양측 배치 깊이
- [x] 검증: FieldId 비어 있음 금지, 크기·수량 ≥ 1, 중복 금지

검증: V1, V2.

### T6. UnitTable + Trait 5종 (설계 §5.5)

- [x] `Unit/Trait/`: `UnitTraitData`(abstract, `TraitKey`), 5종 순수 Data, `UnitTraitAuthoring`(abstract, `[Serializable]`), 5종 Authoring(`ToData()`)
- [x] `UnitData`(순수), `UnitDataSO`(`[SerializeReference] List<UnitTraitAuthoring>`, 공격 패턴 `CellInput[]`)
- [x] 검증: None 키 금지, 중복 금지, Trait 중복 금지, 각 Trait 값 범위(설계 §5.5 표)

검증: V1(순수 Data와 Trait Data), V2.

### T7. StructureTable (설계 §5.6)

- [x] 키 `EntityKey`. 필드: footprint, 체력, 피격 가능, 공격력

검증: V1, V2.

### T8. EnemyLoadout / PlayerLoadout / UnitRoster (설계 §5.7)

- [x] 각 SO에 int 키 필드(`loadoutKey`, `rosterKey`) 추가
- [x] 엔트리 입력은 `UnitDataSO` 참조, `GetData()`에서 EntityKey로 변환
- [x] 검증: 키 ≥ 1, 중복 금지, 엔트리 null 금지, weight/count ≥ 1

검증: V1, V2.

### T9. CombatStageData 확장 (설계 §5.3)

- [x] `CombatStageDetail`에 필드 추가: timelineSeed, fieldId, unitRosterKey, enemyLoadoutKey, headquartersKey, 양측 HQ 위치·회전, 초기 적/구조물 배치, 구조물 목록, 레일웨이
- [x] `CombatStageData` SO: 입력 필드(Field/Roster/Loadout/Structure/Unit SO 참조, `CellInput`)와 `CreateStageData()` 변환
- [x] 검증: 필수 참조 누락 금지, 레일웨이 턴 값 ≥ 1

검증: V1, V2.

> **리뷰 B** — InGameGroup 테이블 보고.

---

## Phase C. ViewGroup 코드 (설계 §6)

### T10. ViewGroup 뼈대 (설계 §6.1)

- [x] `Data/View/ViewGroup.cs`(`DatabaseGroup`, CreateAssetMenu)
- [x] `Queries/ViewDataBase/ViewDataBase.cs`(`IViewQuery`, `SystemGroup`)
- [x] `Group/ViewDataBaseFactory.cs`
- [x] `DataBaseGroupKey.View`, `ViewTableKey`
- [x] `DataBaseGroupSystem`: `IHasViewQuery`를 `IDataBaseQueryContext`에 추가
- [x] `DataBaseFactory.Create`: ViewGroup 생성 추가

검증: V2.

### T11. StageViewProp 이동 (M5, M7b, 설계 §6.5)

- [x] `.cs`와 `.meta`를 함께 이동: `Data/InGame/Table/Stage/ViewProp/**` → `Data/View/Table/StageViewProp/**`, `StageViewPropTable.cs` → `Queries/ViewDataBase/ViewTable/`
- [x] 네임스페이스 변경, SO 타입에 `MovedFrom` 추가
- [x] 키를 `InGameTableKey` → `ViewTableKey`로, 쿼리를 `IInGameQuery` → `IViewQuery`로, 등록을 `InGameDataBaseFactory` → `ViewDataBaseFactory`로 이동
- [x] `FrameworkRuntimeCompositionTests.cs`의 쿼리 타입 경로 수정

검증: V2, V4(옛 네임스페이스·키 참조 없음, GUID 유지).

### T12. StageFieldViewTable (설정형, 설계 §6.2)
### T13. StageMapViewTable (설정형, 설계 §6.3)
### T14. StageNodeVisualTable (목록형, 키 (StageType, StageMapRole), 설계 §6.4)
### T15. UnitVisualTable (목록형, 키 EntityKey, 설계 §6.6)

각 Task:

- [x] Data + SO(한 파일, Unity 타입 허용), Table SO, Query, `ViewTableKey`, `IViewQuery` 프로퍼티, `ViewDataBaseFactory` 등록
- [x] 설정형은 아이템 정확히 1개 검증 + `Config` 프로퍼티. 목록형은 `Get`/`TryGet`
- [x] 설계에 적힌 검증 규칙 구현

검증: V2.

> **리뷰 C** — ViewGroup 코드 보고.

---

## Phase D. Buffer (설계 §7)

### T16. B1: StageProgressData에 StageType / StageMapRole 추가 (M6)

> 2026-09-24 결정(D20)은 Core 작업자에게 요청하는 것이었다. **2026-09-25 사용자 지시로 View 작업에서 구현했다(D21).** 변경 내역은 [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.3, §6에 기록해 Core 작업자 확인을 요청한다.

- [x] `StageProgressData`: 필드 2개, 생성자 기본값 인자 추가
- [x] `StageProgressBuffer.cs` 53행: 클리어 갱신 시 새 필드 복사
- [x] `StageGeneratorSystem`: 노드 생성 시 해당 `StageRootData`의 값 전달 (시작 노드는 기본값)

검증: V2(`MapProgressBufferTests` 5인자 호출 포함). 완료 기준: 기존 테스트 수정 없이 컴파일된다.

> **리뷰 D** — Buffer 변경 보고.

---

## Phase E. 에셋 (V3)

에셋과 `.meta`는 YAML로 직접 작성한다. `m_Script`는 각 스크립트 `.meta`의 GUID를 쓰고, 값은 인벤토리에서 가져온다.

### T17. InGameGroup 에셋 (`DataBaseSO/InGameGroup/`)

| 테이블 | 아이템 |
|---|---|
| StageMap | Chapter1 ×1 (mapKey 1) |
| StageRoot | 11 (1101~1110 Regular, 2101 Boss) + CombatStageData 상세 11 |
| Field | 12 |
| Unit | 4 (Trait 값 직접 입력) |
| Structure | 1 |
| EnemyLoadout / PlayerLoadout / UnitRoster | 5 / 1 / 1 |

- [x] 그룹 에셋, 테이블 에셋, 아이템 에셋 생성
- [x] `DatabaseCatalogSO.asset`에 InGameGroup 등록

### T18. ViewGroup 에셋 (`DataBaseSO/ViewGroup/`)

| 테이블 | 아이템 |
|---|---|
| StageFieldView | 1 |
| StageMapView | 1 |
| StageNodeVisual | 2 ((Combat, Regular), (Combat, Boss)) |
| StageViewProp | 0 (테이블만) |
| UnitVisual | 5 |

- [x] 그룹, 테이블, 아이템 에셋 생성
- [x] 프리팹 참조는 인벤토리의 GUID와 fileID를 그대로 사용
- [x] Catalog에 ViewGroup 등록

검증: V3(모든 에셋의 `m_Script` GUID가 존재하는지, 참조 프리팹 GUID가 존재하는지, 값이 인벤토리와 일치하는지).

> **리뷰 E** — 에셋 보고. **이 시점부터 에디터에서 `DataBaseFactory.Create` 확인이 가능하다.**

---

## Phase F. 테스트와 마무리

### T19. EditMode 테스트 추가 (설계 §11.3)

- [x] 신규 InGame 테이블: 조회, 중복 키 거부, 필수 값 누락 거부 (기존 `StageDataTableTests` 방식)
- [x] Trait 중복 거부, Trait 값 범위
- [x] ViewGroup 설정형: 아이템 0개/2개 거부
- [x] 교차 검증: Unit/Structure의 EntityKey가 UnitVisual에 모두 있는지

검증: V2(테스트 어셈블리 포함).

### T20. 최종 검증과 문서

- [x] V1~V4 전체 재실행
- [x] 설계 문서 상태 갱신 (구현 결과, 설계와 달라진 점)
- [x] 이 계획 문서에 실행 기록(Execution ledger) 작성
- [x] 사용자용 에디터 확인 체크리스트 작성 (설계 §11.2)

> **리뷰 F (최종)**

---

## 범위 밖

- FieldCamera (다른 작업자), Architecture (다른 View 작업자)
- 전투 필드 Buffer B2
- View 시스템(ViewGroup 데이터를 실제로 쓰는 코드)
- 옛 에셋 48개 삭제
- JMH 프레임워크 문서(`framework-architecture.md` 등) 수정. 필요하면 T20에서 제안만 한다.

## 실행 기록

### T0 (2026-09-24) — 완료

- 설치: `mono-devel`(apt), NuGet 패키지 `Microsoft.Net.Compilers.Toolset` 4.8.0, `NETStandard.Library.Ref` 2.1.0, `NUnit`/`NUnitLite` 3.13.3. Microsoft 다운로드 서버(.NET SDK)는 네트워크 정책으로 막혀 있어 NuGet 경로를 사용했다.
- Mono 4.7.2 참조 어셈블리로는 C# 8 기본 인터페이스 구현을 쓰는 코드가 컴파일되지 않았다. 그래서 .NET Standard 2.1 참조로 바꿨다(Unity와 같은 계열).
- 스크립트(scratchpad `v/`): `run_v1.sh`(V1), `run_v2.sh`(Systems 부분 컴파일), `run_tests.sh`(테스트 컴파일), `verify.sh`(V2 + V5 한 번에 실행)
- V2 대상 258개 파일: Framework 전체, Core.Contracts 전체, `Systems/DataBaseSystem/**`, `ServiceKeys/DataBaseGroupServiceKey.cs`, `RuntimeSystem/BufferSystems/**`, `CoreSystem/StageSystem/**`, `Factory/Loading/**`(단 `LodingFactory.cs` 제외), `Factory/Runtime/Buffer/**`, `Systems/AssemblyInfo.cs`
  - 제외: `ServiceKeys/SystemGroupsService.cs`, `ServiceKeys/InputSystemServiceKey.cs`, `Factory/Loading/LodingFactory.cs` — DB와 관계없는 Global/UI/Render 시스템을 참조한다
- **기준 결과(수정 전):** Systems 부분 컴파일 에러 0, 경고 0. 테스트 컴파일 성공. V5 실행: `MapProgressBufferTests` 6 + `StageDataTableTests` 2 = **8개 전부 통과**

### Phase A (T1~T3, 2026-09-24) — 완료, 리뷰 A 대기

- **T1** `Data/Common/StageType.cs`, `StageMapRole.cs` 생성(+ 폴더·파일 meta). `StageRootDataSO.cs`에서 두 enum 제거. using 추가 7개 파일(`StageRootDataSO`, `StageBaseData`, `Combat/Shop/EventStageData`, `StageServiceCollection`, `StageServiceManager`, `StageGeneratorSystem`). V4: 두 enum 정의는 Common에만 있고 사용처 누락 없음.
- **T2** 순수 `StageMapData.cs` 신규(`StageConnectionAlgorithm` 포함), `StageMapDataSO.cs`는 입력 필드와 `GetData()`만. `StageDataTableTests.cs`는 SO 필드에 값을 넣고 키 값으로 비교하도록 수정.
  - V2에서 발견: `ProjectL.Systems.DataBaseSystem.Data.InGame.Enum` 네임스페이스 때문에 `Data.InGame` 아래 코드에서 `Enum.IsDefined`의 `Enum`이 `System.Enum`으로 해석되지 않는다. `System.Enum`으로 전체 이름을 쓴다(원래 코드와 동일). 설계 §10.4에 기록.
- **T3** 순수 `StageRootData.cs`(`IStageData : IData` 포함), `Combat/Shop/EventStageDetail.cs` 신규. `StageRootDataSO`는 입력 필드(stageKey, mapKey, mapRole, stageData SO 참조)와 `GetData()` 변환. `StageBaseData`는 `GetData() => CreateStageData()`. SO 쪽의 `Validate()`는 제거되었다(순수 Data의 `Validate()`로 이동). 옛 API 사용처 없음(V4).
- 검증
  - V1: 순수 파일 7개(Common 2, StageMapData, StageRootData, Detail 3)가 UnityEngine 없이 컴파일
  - V2: Systems 부분 컴파일 에러 0, 경고 0 (대상 265개 파일)
  - V5: 기존 테스트 8개 통과(`MapProgressBufferTests` 6, `StageDataTableTests` 2) + 임시 테스트 4개 통과(저장소에 넣지 않음: 순수·새 객체 반환, StageTable 맵별 조회, 상세 누락 거부, StageServiceCollection 타입별 분기)
- 참조 에셋: 수정한 스크립트를 참조하는 에셋은 없다(GUID 검색). 기존 스크립트 GUID는 모두 유지.

### Phase B (T4~T9, 2026-09-24) — 완료, 리뷰 B 대기

- **T4** `Data/InGame/Common/CellInput.cs`: `[Serializable]` 입력 구조체, `ToCellCoord()`, `ToCellCoords()`.
- **T5** Field: `FieldData`(순수), `FieldDataSO`, `FieldDataTableSO`, `FieldTable`(키 `FieldId` string, Ordinal 비교).
- **T6** Unit: `UnitData`(순수, 공격 패턴 `CellCoord[]`), `UnitDataSO`(`[SerializeReference] List<UnitTraitAuthoring>`), `UnitDataTableSO`, `UnitTable`(키 EntityKey).
  - Trait: `Unit/Trait/UnitTraitData.cs`(순수: 추상 + 5종), `Unit/Trait/UnitTraitAuthoring.cs`(입력: 추상 + 5종). 계획은 종류별 파일을 예시로 들었지만, 한 파일에 모으는 편이 옛 `TraitAuthoring.cs`와 같고 추가도 쉬워서 2개 파일로 만들었다.
  - 한 유닛에 같은 TraitKey 중복 거부.
- **T7** Structure: `StructureData`(순수), `StructureDataSO`, `StructureDataTableSO`, `StructureTable`(키 EntityKey).
- **T8** Loadout: `EnemyLoadoutData`/`PlayerLoadoutData`/`UnitRosterData`(순수, 엔트리는 `readonly struct`), 각 SO(`loadoutKey`/`rosterKey` int 필드 추가, 유닛은 `UnitDataSO` 참조 → EntityKey), Table SO 3개, Table 3개.
- **T9** `CombatStageDetail`에 전투 필드 추가(`EntityPlacement`, `RailwayData` 포함). `CombatStageData` SO는 Field/Roster/Loadout/Structure/Unit SO 참조와 `CellInput`으로 입력받아 키로 변환. 검증은 옛 규칙과 같다(Field와 Roster만 필수, HQ는 비우면 없음, 적 구성은 0이면 없음).
- 조립: `InGameTableKey`에 키 6개, `IInGameQuery`/`InGameDataBase`에 쿼리 6개, `InGameDataBaseFactory`에 테이블 6개 등록.
- 공통: 순수 Data는 `public sealed`, SO·Table SO·Query는 `internal`. SO에는 다른 SO가 키를 꺼낼 `internal` 프로퍼티(`FieldId`, `Key`, `LoadoutKey`, `RosterKey`)를 둔다.
- **동작 변화:** 비어 있는 `CombatStageData`(Field·Roster 없음)는 이제 검증에서 거부된다. 옛 StageDefinitionSO와 같은 규칙이다.
- 검증
  - V1: 순수 파일 14개가 UnityEngine 없이 컴파일
  - V2: Systems 부분 컴파일 에러 0, 경고 0
  - V5: 기존 8 + 임시 11 = 19개 통과. Phase B 임시 통합 테스트(저장소에 넣지 않음)는 SO를 메모리에서 조립해 `InGameDataBaseFactory.Create`까지 실행하고, 6개 신규 테이블 조회, SO 참조 → 키 변환, 공격 패턴·Trait 변환, 레일웨이 on/off, Field 누락·Trait 중복·Trait 범위·Loadout 유닛 누락·키 중복 거부를 확인했다.
- **알려진 제한:** Unity 기본 인스펙터에는 `[SerializeReference]` 리스트에 항목을 추가하는 버튼이 없다. 옛 코드도 같은 구조였고 전용 에디터가 없었다. 기획자가 인스펙터에서 Trait을 추가하려면 타입 선택 드롭다운(PropertyDrawer)이 필요하다(범위 밖, 후속 제안).

### Phase C (T10~T15, 2026-09-24) — 완료, 리뷰 C 대기

- **T10** `Data/View/ViewGroup.cs`, `Queries/ViewDataBase/ViewDataBase.cs`(`IViewQuery`), `Group/ViewDataBaseFactory.cs`. `DataBaseGroupKey.View`, `ViewTableKey` 추가. `DataBaseGroupSystem`에 `IHasViewQuery`, `IDataBaseQueryContext` 확장. `DataBaseFactory.Create`에서 ViewGroup 생성.
- **T11** StageViewProp 이동(`git mv`, meta와 폴더 meta 함께 이동해 GUID 유지): `Data/InGame/Table/Stage/ViewProp/**` → `Data/View/Table/StageViewProp/**`, `StageViewPropTable.cs` → `Queries/ViewDataBase/ViewTable/`. 네임스페이스 변경, SO 2종에 `MovedFrom` 추가, 메뉴를 `ProjectL/Database/View/…`로 변경. `InGameTableKey`/`IInGameQuery`/`InGameDataBaseFactory`에서 제거하고 View 쪽에 등록. `FrameworkRuntimeCompositionTests.cs`의 쿼리 타입 경로 수정.
- **T12** StageFieldViewTable(설정형): cellPrefab(+Renderer 검사), 손패 레이어, 고스트 머티리얼·색·오프셋·스케일, 필드 유닛 스케일.
- **T13** StageMapViewTable(설정형): 레이아웃·선·색 5종·프리팹 4종·피킹. 검증은 옛 `ValidateOrThrow`를 따르되, 삭제된 `StageMapNodeView.HitCollider` 대신 **노드 프리팹 자식의 `Collider`** 레이어가 nodeMask에 포함되는지 검사한다.
- **T14** StageNodeVisualTable(목록형, 키 `(StageType, StageMapRole)`): visualPrefab, icon, displayName(비면 프리팹 이름).
- **T15** UnitVisualTable(목록형, 키 EntityKey): prefab, displayName(비면 프리팹 이름).
- 공통: View Data는 기존 View 방식대로 `[Serializable] XxxData`와 `XxxDataSO`를 한 파일에 둔다(Unity 타입 허용). 설정형은 아이템이 정확히 1개가 아니면 거부.
- 검증
  - V2: Systems 부분 컴파일 에러 0, 경고 0
  - V4: 옛 `Data.InGame.Table.Stage.ViewProp`, `InGameTable.IStageViewPropTableQuery` 참조 없음(테스트 경로만 새 경로로 수정)
  - V5: 25개 통과(기존 8 + 임시 A 4 + 임시 B 7 + 임시 C 6). 임시 C 테스트는 ViewGroup을 메모리에서 조립해 `ViewDataBaseFactory.Create`를 실행하고 5개 테이블 조회, displayName 대체, 설정형 0개/2개 거부, cell Renderer 누락 거부, nodeMask와 Collider 레이어 불일치 거부, 목록형 중복·None 거부, StageViewProp 이동(새 경로 존재·옛 경로 없음·MovedFrom)을 확인했다. stub에 `GameObject.AddComponent`/컴포넌트 조회를 추가했다.
- **Core 전달 문서 갱신:** §5를 "View 작업에서 Core 영역에 손댄 내역"(using 3줄, 데이터 API 변경 전체)으로, §6을 "Buffer B1 구현 요청"으로 바꿨다.

### Phase D (T16, 2026-09-25) — 완료, 리뷰 D 대기

- 사용자 지시("PhaseD도 진행하고 E까지 한번에 진행해")로 D20을 바꿔 View 작업에서 구현했다(설계 D21).
- `StageProgressData`: `StageType`, `MapRole` 프로퍼티와 생성자 기본값 인자(`isClear` 뒤) 추가.
- `StageProgressBuffer.SetClear`: 새 필드 복사.
- `StageGeneratorSystem`: 기존 스테이지 수집 루프에서 `stagesByKey`를 함께 만들고, 결과 루프에서 스테이지 노드에 `StageType`/`MapRole` 전달. 시작 노드는 기본값(`Event`/`Regular`로 읽히므로 View는 `StageKey == 0`을 먼저 확인). 생성 알고리즘·난수 사용은 그대로.
- Core 전달 문서 §1, §5.3, §6, §12 갱신. 설계 §7.2 B1 범위, §7.3 구현 메모, D21 추가.
- 검증
  - V2: Systems 부분 컴파일 에러 0, 경고 0. `MapProgressBufferTests` 기존 호출 수정 없음
  - V5: 26개 통과(기존 8 + 임시 A 4 + 임시 B 8 + 임시 C 6). 임시 B에 추가한 테스트는 메모리에서 조립한 InGameGroup으로 맵을 생성해 모든 스테이지 노드의 (StageType, MapRole)과 `SetClear` 뒤 유지 여부를 확인한다.

### Phase E (T17~T18, 2026-09-25) — 완료, 리뷰 E 대기

- 에셋 71개와 `.meta`, 폴더 `.meta` 24개를 YAML로 직접 작성했다. `DatabaseCatalogSO.asset`에 InGameGroup, ViewGroup 등록(RenderGroup 유지).
- **T17** `DataBaseSO/InGameGroup/` 56개: 그룹 1, 테이블 8, StageMap Chapter1 1, StageRoot 11(`Stage/Root/`), CombatStageData 11(`Stage/Combat/`), Field 12, Unit 4(Trait은 `references` 블록에 SerializeReference로 입력: Akimbo MoveAttackDiscount(1, 1), Sniper Focus(1, 3)), Structure 1, EnemyLoadout 5, PlayerLoadout 1, UnitRoster 1.
- **T18** `DataBaseSO/ViewGroup/` 15개: 그룹 1, 테이블 5, StageFieldView_Default 1, StageMapView_Default 1, NodeVisual 2((Combat, Regular) "Normal Stage A", (Combat, Boss) "Boss Stage"), StageViewProp 0, UnitVisual 5.
- 프리팹 참조는 각 프리팹의 루트 GameObject fileID를 쓴다. 유닛 프리팹 4개는 프리팹 Variant라 루트가 stripped GameObject다(옛 카탈로그와 같은 값).
- 옛 에셋 48개는 건드리지 않았다.
- 검증 (V3, 에디터 밖)
  - 참조: 72개 에셋(Catalog 포함)의 참조 211개. 모든 `m_Script` GUID와 에셋·프리팹 GUID가 존재하고, 프리팹 fileID가 GameObject를 가리킨다.
  - 필드 이름: 에셋 YAML을 컴파일된 `ProjectL.Systems.dll` 타입에 Unity 직렬화 규칙(public 또는 `[SerializeField]`/`[SerializeReference]`, readonly 제외)대로 채웠다. 중첩 데이터와 SerializeReference `data`까지 YAML 키와 직렬화 필드가 정확히 일치한다(이름을 일부러 틀리게 바꾸면 잡히는 것도 확인).
  - 값: 로드한 에셋으로 `InGameDataBaseFactory.Create`, `ViewDataBaseFactory.Create`를 실행해 모든 테이블 검증을 통과했다. 스테이지 11·필드 12·유닛 4·HQ·Loadout·Roster·View 설정 값이 인벤토리와 일치한다.
  - 연결: 새 InGameGroup으로 `StageGeneratorSystem.Generate(1)`을 실행해 Buffer에 노드 11개가 기록되고, 모든 스테이지 노드의 (StageType, MapRole)에 대응하는 ViewGroup 노드 외형이 있다.
  - V5: 27개 통과(Phase D 26 + 에셋 로드 임시 테스트 1).
- **에디터에서 확인할 것:** 컴파일, EditMode 테스트, `DataBaseFactory.Create(catalog)`, 인스펙터 값(특히 Unit Trait, 프리팹 링크). Unity가 에셋을 처음 열면 직렬화 형식을 다시 써서 diff가 생길 수 있다(값 변화 없음).

### Phase F (T19~T20, 2026-09-25) — 완료

- 기준: 사용자가 push한 `feature/Yonggeun/DB_SO` `39b634f`(Phase A~E 포함).
- **T19** `Assets/Tests/EditMode/Framework/`에 4개 파일(+`.meta`) 추가.
  - `DataBaseTestAssets.cs`: 테스트 도우미(SO·GameObject 생성과 정리, private 직렬화 필드 입력, Loadout Entry 배열).
  - `InGameDataTableTests.cs` (20개): 신규 테이블 6종 조회·키 없음·중복 키·필수 값 누락, 전투 스테이지 Field/Roster 필수, 레일웨이 변환, `GetData` 새 인스턴스(P2), Trait 중복·null·값 범위(5종 9경우).
  - `ViewDataTableTests.cs` (7개): 설정형 0개/2개 거부, cellPrefab·Renderer 필수, NodeVisual·UnitVisual 조회·중복·None·프리팹 누락, displayName 대체.
  - `DataBaseAssetTests.cs` (5개): 실제 에셋을 `AssetDatabase`로 불러와 Catalog DB 생성, Unit/Structure → UnitVisual, 스테이지 → NodeVisual, 전투 스테이지 키 참조, StageMapView 2개 거부. StageMapView는 씬 인스턴스를 거부하므로 2개 거부는 실제 프리팹이 있는 에셋으로 확인한다.
  - 계획의 "기존 `StageDataTableTests` 방식"은 리플렉션으로 타입을 찾는 방식이었지만, `InternalsVisibleTo("ProjectL.Tests.Framework")`가 있어 타입은 직접 쓰고 private 직렬화 필드만 리플렉션으로 넣었다.
- **T20**
  - V1: 순수 Data 15개 파일이 UnityEngine 없이 컴파일
  - V2: Systems·테스트 부분 컴파일 에러 0, 경고 0
  - V3: 에셋 72개 참조 211개 오류 0
  - V4: 옛 경로 참조 없음(새 경로, `MovedFrom`만 남음)
  - V5: 59개 중 58개 통과(기존 8 + 신규 32 + 임시 19). 실패 1개는 `Catalog_CreatesAllDataBaseGroups`로, 작업 환경의 `AssetDatabase` stub이 RenderGroup 에셋을 불러오지 못해서다(에디터 확인 필요). 신규 에셋 테스트는 Phase E 에셋 로더로 채운 stub에서 실행했고, UnitVisual·NodeVisual·Unit·Field 항목을 일부러 빼면 해당 테스트가 실패하는 것도 확인했다.
  - 설계 문서: 상태, §11.2 에디터 확인 체크리스트(E1~E7), §11.3 추가한 테스트, §14 구현 결과(설계와 달라진 점, 남은 일).
- **에디터 확인 (2026-09-25, 사용자):** `f32dea2`에서 컴파일, EditMode 테스트 전체 통과(`Catalog_CreatesAllDataBaseGroups` 포함), 인스펙터(Trait, ViewGroup 프리팹 링크) 확인 완료. 과정에서 패치 적용 방식 때문에 로컬에 남은 옛 StageViewProp 파일이 CS0104 에러를 냈고, 옛 파일을 지워 해결했다.
- 남은 일과 로컬 세션 인계는 [로컬 세션 인계 문서](../handoffs/2026-09-25-db-so-local-session-handoff.md).
