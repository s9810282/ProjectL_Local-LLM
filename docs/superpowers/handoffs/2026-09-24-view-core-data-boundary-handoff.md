### 2026-10-04 머지 후 View 연결 복원
사용자 요청으로 상대방 Core·Combat 조립을 유지하며 StageMapViewSystem을 함께 등록했다. BufferSystemGroup.cs에는 StageView·ArtifactView 네임스페이스 using만 복원했고 계산·발행 로직은 수정하지 않았다. Buffer Factory·서비스 키에도 같은 참조를 복원했다. 진행 기록과 실제 검증 결과는 stage-map-view-plan.md §7을 참조한다.

### 2026-10-04 카메라 병합 충돌 해결
사용자 명시 지시로 b5d14d7 병합 시 MapProgressBuffer의 최신 IDataBaseQueryContext 및 Store 계약을 유지했다. 자동 병합으로 바뀐 StageGeneratorSystem·StageServiceManager 초기화도 현재 Core 계약으로 유지했다. Core 계산 변경 없음. 렌더 API 이동에 따른 전투 View 참조를 새 경로·Handle 명칭에 맞췄다. 검증 상태는 stage-map-view-plan.md §7 참조.
# DB-SO 재구성 — Core 작업자 전달 문서

> View 작업에서 DB-SO를 새 Framework/Systems 구조에 맞게 재구성한다. 이 문서는 그중 **Core 작업에 영향을 주는 내용**과 **Core 작업자에게 확인받을 사항**을 전달한다.
> 상세 설계는 [DB-SO 재구성 설계](../specs/2026-09-24-db-so-ingame-view-group-design.md)에, 옛 에셋 값은 [고아 에셋 인벤토리](../specs/2026-09-24-view-db-orphan-asset-inventory.md)에 있다.

- 작성일: 2026-09-24 (갱신 2026-09-30, 전투 Flow 요청 §13 추가)
- 작성: View 작업
- **2026-10-05 최신 Core 요청은 [별도 요청 문서](2026-10-05-combat-core-requests-phase6b.md)가 원본이다.** `e0b02cc7` 병합본과 골격 6B F1 작업 기준으로 남은 요청·담당·검증 조건을 정리했다. 아래 예전 요청의 해결 여부를 현재 미완료 상태로 읽지 않는다. 외부 전달·답변은 아직 확인하지 않았다.
- 기준 브랜치: `develop`
- 상태: **공유 및 확인 요청.** §4·§5는 View 작업에서 구현을 마쳤다(2026-09-24, Phase A~C). §6(Buffer B1)은 원래 Core 작업자에게 요청하려 했으나, **사용자 지시로 View 작업에서 구현했다(2026-09-25, Phase D).** Core 작업자는 그 변경을 확인해 주세요.
- 2026-09-30 추가: **§13은 전투 Flow Phase 0의 전달용 요청이며 전달·답변·합의는 미확인이다.** 기존 DB-SO 구현 내역과 구분한다. 이번 확인 브랜치는 `feature/Yonggeun/Stage`, HEAD `3a2b6777`이다.

## 1. 요약

| # | 내용 | 성격 | Core 확인 필요 |
|---|---|---|---|
| 1 | 데이터 흐름: InGameGroup → Core 시스템 → Buffer → View | 공유 | ✅ |
| 2 | InGame Data는 순수 C#. SO는 기획 입력 계층 | 공유 | – |
| 3 | InGameGroup 테이블을 View 작업에서 복구·구현 | 공유 | API 확인 |
| 4 | View 작업에서 Core 영역에 손댄 내역 (using 3줄, 데이터 API 변경, Buffer B1) | 공유 | ✅ |
| 5 | **Buffer B1 구현 확인 요청** (StageProgressData, StageGenerator). View 작업에서 구현함 | **확인 요청** | ✅ |
| 6 | 공용 타입은 옛 Core.Contracts 재사용 | 공유 | 정리 계획 |
| 7 | Core 데이터에서 빠지는 View 필드 | 공유 | – |
| 8 | 데이터 주의사항 | 정보 | – |
| 9 | 옛 에셋 보존 | 정보 | 삭제 시점 |
| 10 | 전투 필드 Buffer(B2) | 향후 협의 | ✅ |
| 11 | 전투 Flow 소유 경계, 처리 서비스 네 개, F3 결과 읽기·등록·보존, F2 종료 수명(§13) | 전달용 요청 준비 | ✅ |

## 2. 데이터 흐름

새 Core는 기존 `CoreCaculator`(Contracts/Snapshot)가 아니라 **새 Framework/Systems 위의 시스템**으로 동작한다는 전제로 설계했다.

```text
InGameGroup (규칙) ──InitData──► Core 시스템 (InGameCoreSystemGroup)
                                   │  Table을 바로 참조 (변환 계층 없음)
                                   ▼ WriteSink
                                 Buffer (런타임 상태)
                                   │ ReadSink
ViewGroup (표현) ──InitData──►  View 시스템 / UI
```

- **Core는 InGameGroup Table을 바로 참조한다.** 현재 `StageGeneratorSystem`처럼 `InitData(IDataBaseQueryContext)`로 `InGameQuery`를 받는다.
- 예전에는 View(`ProjectL.Unity`의 `FieldDefinitionAdapter`, `StageEntryDataBuilder`)가 SO를 Core Spec으로 바꿔 넣었다. 이제 **View는 Core에 데이터를 넣지 않는다.**
- **View는 InGameGroup을 읽지 않는다.** View에 필요한 규칙 값(크기, 위치, 노드 종류 등)은 **Core가 Buffer에 써 주어야 한다.**

## 3. InGame Data 규칙

| # | 규칙 |
|---|---|
| P1 | InGame Data(Core가 받는 것)는 **순수 C#만** 사용한다. `GameObject`, `Vector2Int`, `Color`, SO 참조 등 UnityEngine 타입 금지 |
| P2 | SO는 **기획 입력 계층**이다. Item SO가 직렬화 필드를 갖고, `GetData()`가 순수 C# 불변 Data를 **매번 새로** 만들어 반환한다 |
| P3 | SO끼리의 참조는 입력 계층에서만 허용한다. `GetData()`에서 **키**로 바꾼다. Core가 받는 Data에는 SO 참조가 없다 |

Core 쪽에서 체감하는 변화:

- Table에서 꺼낸 Data는 **불변**이고 Unity 타입이 없다.
- 다른 데이터는 키로 가리킨다. 예: `CombatStageDetail.EnemyLoadoutKey` → `EnemyLoadoutTable.Get(key)`

## 4. InGameGroup 테이블 (View 작업에서 구현)

옛 SO 데이터를 복구하는 방식으로 아래 테이블을 만든다. 상세 필드와 복구 값은 설계 §5에 있다.

| 테이블 | 키 | 주요 필드 | 상태 |
|---|---|---|---|
| StageMapTable | MapKey (int) | depth, width, 연결 확률·알고리즘·가중치, seed | 기존. P2 적용 |
| StageTable (StageRoot) | StageKey (int) | mapKey, mapRole, stageType, 상세 Data | 기존. P2 적용 |
| (상세) CombatStageDetail | StageRoot가 보유 | timelineSeed, fieldId, rosterKey, enemyLoadoutKey, HQ 키·위치·회전, 초기 배치, 구조물, 레일웨이 | 기존 SO 확장 |
| FieldTable | FieldId (string) | 열·행, reserveCapacity, 양측 배치 깊이 | 신규 |
| UnitTable | EntityKey | 소유 가능, footprint, 체력, 이동, 공격(패턴은 `CellCoord[]`), 우선순위·딜레이, Trait | 신규 |
| StructureTable | EntityKey | footprint, 체력, 피격 가능, 공격력 | 신규 |
| EnemyLoadoutTable | LoadoutKey (int) | (EntityKey, weight)[], spawnCount, seed | 신규 |
| PlayerLoadoutTable | LoadoutKey (int) | (EntityKey, count)[], 시작 손패 수, seed | 신규 |
| UnitRosterTable | RosterKey (int) | EntityKey[] | 신규 |

- **Trait 5종**을 구현한다: Focus, MoveAttackDiscount, PiercingFalloff, ThornField, Knockback. 필드와 검증은 옛 Contracts `*TraitSpec`과 같다.
- Loadout과 Roster의 키는 옛 에셋에 없던 값이라 새로 부여한다: Enemy 1~5, Player 1, Roster 1.
- 쿼리 인터페이스는 기존 패턴(`IXxxTableQuery`, `Get`/`TryGet`)을 따르고 `IInGameQuery`에 추가한다.

## 5. View 작업에서 Core 영역에 손댄 내역

규칙상 View 작업은 Core 로직을 수정하지 않는다. 아래는 DB 재구성 과정에서 **Core 코드 또는 Core가 쓰는 데이터 API에 닿은 변경 전체**다. 5.1·5.2는 로직 변경이 없다. **5.3(Buffer B1)은 사용자 지시로 한 Core 로직 변경이다.**

### 5.1 Core 파일을 직접 수정한 것 (Phase A)

| 파일 | 변경 | 이유 |
|---|---|---|
| `RuntimeSystem/InGameSystem/CoreSystem/StageSystem/StageGeneratorSystem.cs` | `using ProjectL.Systems.DataBaseSystem.Data.Common;` 1줄 추가 | `StageType`/`StageMapRole`이 `Data/Common/`으로 이동해 컴파일에 필요 |
| `.../StageSystem/StageServiceCollection.cs` | 같은 using 1줄 추가 | 〃 |
| `.../StageSystem/StageServiceManager.cs` | 같은 using 1줄 추가 | 〃 |

`CoreCaculator`(Contracts/Domain/Application)는 수정하지 않았다. `BufferSystems`는 Phase D에서 수정했다(5.3).

### 5.2 Core가 쓰는 데이터 API가 바뀐 것 (DB 폴더에서 수정)

| # | 변경 | Core 영향 |
|---|---|---|
| M1 | `StageType`, `StageMapRole` 위치가 `Data.InGame.Table.Stage.Item` → `DataBaseSystem.Data.Common` | using만 바뀜(5.1) |
| M2 | `StageMapData`가 순수 불변 클래스가 됨(값은 `StageMapDataSO`에서 입력) | 프로퍼티 이름·타입 동일. 수정 불필요 |
| M3 | `StageRootData`가 순수 불변 클래스가 됨. **`StageData` 프로퍼티 타입: `StageBaseData`(SO) → `IStageData`(순수)** | `.StageData.StageType`, `.StageData.Validate()`는 그대로 동작 |
| M4 | `StageBaseData.GetData()`가 SO 자신이 아니라 순수 상세 Data(`CombatStageDetail`/`ShopStageDetail`/`EventStageDetail`)를 반환. SO 쪽 `Validate()`는 제거 | 상세 SO를 직접 다루던 코드가 있다면 영향 |
| – | `CombatStageDetail`에 전투 필드 추가. **Field·Roster가 없으면 검증에서 거부**(옛 StageDefinitionSO 규칙과 동일) | 빈 전투 스테이지 데이터가 더 이상 통과하지 않음 |
| – | `IInGameQuery`에 테이블 6개 추가(Field, Unit, Structure, EnemyLoadout, PlayerLoadout, UnitRoster) | 추가만 |
| – | `IInGameQuery.StageViewPropTableQuery` 제거 → `IViewQuery`로 이동 | InGame 쪽에서 쓰던 곳 없음 |
| – | `IDataBaseQueryContext`에 `IHasViewQuery`(ViewQuery) 추가 | `InitData(IDataBaseQueryContext)` 구현체 수정 불필요 |
| – | `DataBaseFactory.Create`가 Catalog에 ViewGroup도 요구 | Phase E에서 InGameGroup·ViewGroup 에셋을 만들고 Catalog에 등록함 |

**확인 요청:** M3/M4로 Core가 받는 스테이지 상세가 SO에서 순수 Data로 바뀌었다. 진행 중인 Core 작업에서 상세 SO를 직접 쓰고 있다면 알려 주세요.

### 5.3 Buffer B1 (Phase D, 사용자 지시로 구현)

| 파일 | 변경 |
|---|---|
| `RuntimeSystem/BufferSystems/Stage/StageProgressData.cs` | `StageType StageType`, `StageMapRole MapRole` 프로퍼티 추가. 생성자 끝에 `stageType = default`, `mapRole = default` 인자 추가 |
| `RuntimeSystem/BufferSystems/Stage/StageProgressBuffer.cs` | `SetClear`에서 Data를 다시 만들 때 `StageType`, `MapRole`도 복사 |
| `RuntimeSystem/InGameSystem/CoreSystem/StageSystem/StageGeneratorSystem.cs` | 스테이지 수집 루프에서 `stageKey → StageRootData` 사전을 함께 만들고, 노드를 기록할 때 해당 스테이지의 `StageType`, `MapRole`을 전달(시작 노드는 기본값) |

상세와 이유는 §6.

### 5.4 전투 준비 Job 병합 통합 (2026-09-30, 사용자 지시)

`1a51234c` 병합 시 `CharacterBattlePreparationTests.cs`의 `new CombatJob(...)`를 `new CombatPrepareJob(...)`로 수정했다. 검증 내용은 그대로 두고 현재 View 측 타입 이름에 맞춘 변경이다. Core 서비스·계산기 로직은 들어온 커밋을 유지한다. 준비 API 호출 연결과 충돌 해결은 View의 Job·Plan에서 수행했다. 상세 범위와 검증 결과는 [전투 진행 계획 §6.5](../plans/2026-09-30-combat-flow-skeleton-plan.md#65-1a51234c-병합-통합-기록-2026-09-30)를 따른다. 담당자 간 소유 경계 합의 완료로 간주하지 않는다.

### 5.5 진행 테스트의 InGame 그룹 조립 (2026-10-02, 사용자 결정)

View Phase C에서 `InGameSystemFactory`의 View 그룹에 `CombatViewSystem`이 들어갔다. 이 시스템은 `PostBindService`에서 Global(입력 Layer·로딩 등록)과 입력 수신 슬롯을 받는다. 그래서 Global 없이 Buffer·Job·InGame만 조립하는 `ChapterGraphPreparationTests`의 `Prepare(withJobRuntime: true)`가 바인딩에서 실패했다(`Runtime_ChapterProgressionRecordsBossCompletionAndRejectsCompletedEntry`, `Runtime_DefeatThenExplicitNewRun_RebuildsGraphAndPreservesArtifact`). 사용자 결정으로 그 조립만 바꿨다. InGame 그룹을 `InGameCoreSystemFactory` + 빈 `InGameViewSystemGroup`으로 직접 만든다. 검증 내용과 Core 코드는 그대로다. 앞으로 InGame 그룹 전체를 Global 없이 조립하는 테스트를 만들 때도 같은 방식이 필요하다. 상세는 [View 계획 §7.11](../plans/2026-10-01-combat-view-plan.md#711-phase-c-실행-기록-2026-10-02-코드-완료검증-대기).

### 5.6 View A2 — 공격 지정 방식 DB 제공 (2026-10-04, 미전달)

사용자 요청으로 View 계획 A2의 데이터·조회·에셋을 구현했다. **Core 생산 코드는 수정하지 않았다.** 구현·검증 상태의 원본은 [View 계획 §7.19](../plans/2026-10-01-combat-view-plan.md#719-phase-a2-실행-기록-2026-10-04-코드-완료unity-검증-대기)다.

**새 데이터 API:**

- `Data.InGame.Table.Targeting.Item.TargetingMethodData`: `MethodKey`, `Shape`, `ChoiceMode`, `Splash`, `SplashPercent`를 가진 순수 불변 데이터. SO·TableSO는 `Data/InGame/Table/Targeting/`에 있다.
- `IInGameQuery.TargetingMethodTableQuery`: `GetTargetingMethod(int)`, `TryGetTargetingMethod(int, out TargetingMethodData)`, `GetAllTargetingMethods()`. 서비스 키는 `InGameTableKey.TargetingMethod`다.
- `UnitDataSO.targetingMethod` 참조를 `GetData()`에서 `UnitData.TargetingMethodKey`로 변환한다. `UnitData` 생성자의 마지막에 **필수 `int targetingMethodKey`**가 추가됐다. 옛 공격 필드는 유지한다.
- `AttackPower > 0`이면 양수 키 필수, 공격력이 0이면 미지정 키 0 허용, 음수 키 거부. 양수 키는 공격력과 관계없이 그룹 내 테이블에 있어야 한다. Factory 조립 시 한 번 교차 검사한다.
- 데이터 검증은 `TargetingMethodSpec`의 enum·선택 방식/추가 범위 조합 규칙을 재사용한다. 저작 규칙으로 `Splash.None`이면 0, 그 외에는 1 이상의 비율을 요구하며 상한은 없다. 생성한 Spec을 보관하거나 전투에 전달하지는 않는다.

| MethodKey | Shape | ChoiceMode | Splash / Percent | 연결 유닛 |
|---|---|---|---|---|
| 1 | RadialSquare | Single | None / 0 | Akimbo, Demon, Sniper, MainCharacter |
| 2 | Fan | Direction | None / 0 | ShotGun |

`InGameGroup/Targeting`에 두 방식과 테이블 에셋을 추가하고 InGameGroup에 등록했다. **Ballista·CactusCannon·Gravekeeper용 3~5는 해당 유닛 에셋을 만들 때 추가**한다. 현재 테이블에는 1·2만 있으며 사거리는 계속 `UnitData.AttackRange`가 소유한다.

**Core 후속 요청:** `DatabaseBattleInitializationSource`·`BattleDefinitionAdapter`에서 유닛의 키로 지정 방식 데이터를 조회하여 `TargetingMethodSpec`을 조립한다. 옛 `AppointMethod`/`AttackTargetMode`를 새 enum으로 캐스팅하거나 추정하지 않는다. 나머지 준비 계약을 포함해 기본 DB source를 완성한 뒤 T1/G7 예외를 제거하고, `useDevBattle=false` 실제 데이터로 진입·공격·미리보기까지 검증한다. A2의 테이블 생성 성공은 운영 전투 준비 성공이 아니다. View는 계속 Buffer의 `AttackAims`를 사용한다.

**기존 Core 관련 테스트의 기계적 변경:**

- `ChapterGraphPreparationTests`: 수동 InGameGroup에 빈 지정 방식 테이블 추가.
- `CharacterRosterDatabase`, `BattleDefinitionAdapterTests`: 새 UnitData 생성자 인자 추가. Ballista의 3은 해당 단위 테스트용 키이며 DB 에셋 등록을 의미하지 않는다.
- 위 두 파일과 `CoreStageCombatCycleTests`, `CombatFlowIntegrationTests`, `CombatEntryIntegrationTests`: `IInGameQuery` 테스트 대역에 새 프로퍼티 추가. 해당 테스트의 현재 경로에서 사용하지 않는 조회는 `NotSupportedException`으로 명시했다. Core가 새 조회를 소비할 때 대역도 실제 지정 방식 데이터를 공급하도록 갱신해야 한다.
- 기존 Core 검증 내용은 유지했다. `InGameDataTableTests`와 `DataBaseAssetTests`에는 새 조회·참조 누락·키 매핑 검증을 추가했다.

### 5.7 아티팩트 보드 처음 크기 설정 테이블 (2026-10-05, 사용자 지시, 미전달)

InGame DB에 설정형 테이블 `ArtifactBoardConfig`(항목 1개, 기본 4×4)를 추가했다([아티팩트 팝업 계획](../plans/2026-10-05-artifact-popup-plan.md) Phase A, D6·D7). `IInGameQuery`에는 넣지 않고 별도 인터페이스 `IHasArtifactBoardConfig`로 노출하므로 Core 서비스와 테스트용 `IInGameQuery` 구현체는 바뀌지 않는다. 실행 계층인 `StageLifeCycle`이 이 값으로 StageSelect 진입 때 `InitializeBoard`를 한 번 부른다(이미 초기화돼 있으면 Core가 `false`를 돌려주고 상태를 보존).

| 파일 | 변경 | 이유 |
|---|---|---|
| `Assets/Tests/EditMode/Framework/ChapterGraphPreparationTests.cs` | `Prepare`의 InGame 그룹 테이블 목록에 `ArtifactBoardConfigDataTableSO`(기본 항목 1개) 한 줄 추가 | `InGameDataBaseFactory`가 그룹의 모든 테이블을 필수로 찾으므로(`GetDataBase`) 목록에 없으면 조립이 실패한다. 검증 내용은 그대로다 |

앞으로 InGame 그룹을 테이블 목록으로 직접 조립하는 테스트는 이 테이블도 포함해야 한다. 보드 크기가 늘어나는 보상 처리(`ExpandBoard`)와 Run 재시작 시 초기화 정책은 이번 변경에 포함되지 않는다.

## 6. Buffer B1 (View 작업에서 구현, Core 작업자 확인 요청)

`StageGeneratorSystem`의 노드 기록 로직과 `StageProgressBuffer`를 고쳐야 해서 원래는 Core 작업자에게 요청하려 했다(설계 D20). 사용자 지시로 **View 작업에서 구현했다(설계 D21, 2026-09-25).** 아래 내용이 Core 작업 방향과 맞는지 확인해 주세요.

### 6.1 이유

스테이지 맵 View는 `StageProgressBuffer`의 노드 목록을 읽어 그린다. 노드 외형(일반/보스 프리팹)을 고르려면 노드의 종류가 필요한데, 현재 `StageProgressData`에는 `StageKey`만 있다. View는 InGameGroup을 읽지 않으므로 **Core가 노드를 쓸 때 종류를 같이 써 주어야 한다.** ViewGroup의 `StageNodeVisualTable`은 이미 `(StageType, StageMapRole)` 키로 준비되어 있다.

### 6.2 구현 내용

```csharp
// StageProgressData — 필드 2개 추가. 기존 호출(5·6인자)이 깨지지 않도록 기존 isClear 뒤에 기본값 인자로 둔다.
public StageProgressData(int nodeId, int stageKey, int depth, int widthIndex,
    IEnumerable<int> nextNodeIds, bool isClear = false,
    StageType stageType = default, StageMapRole mapRole = default)
```

```csharp
// StageGeneratorSystem — 결과 루프
StageRootData root = node.StageKey == 0 ? null : stagesByKey[node.StageKey];
result.Add(new StageProgressData(node.Id, node.StageKey, node.Depth, node.Width, node.Next,
    stageType: root?.StageType ?? default, mapRole: root?.MapRole ?? default));
```

- `StageGeneratorSystem`: 기존 스테이지 수집 루프에서 `stagesByKey`를 함께 만들고, 노드를 기록할 때 해당 `StageRootData`의 `StageType`, `MapRole`을 전달한다. 생성 알고리즘과 난수 사용은 바꾸지 않았다.
- `StageProgressBuffer`: 클리어 상태를 갱신하면서 Data를 다시 만들 때 새 필드도 복사한다.
- 시작 노드(StageKey 0)는 두 필드가 기본값(`StageType.Event`, `StageMapRole.Regular`)이다. 의미가 없는 값이므로 **`StageKey == 0`을 먼저 확인**해야 한다. View는 시작 노드를 별도 외형(`StageMapViewData.StartVisualPrefab`)으로 그린다.
- 테스트 영향: `MapProgressBufferTests.cs`의 5인자 호출 8곳은 기본값 덕분에 수정하지 않았다.
- 검증(에디터 밖, Phase D·E): 새 에셋으로 만든 InGameGroup에서 맵 1을 생성해 모든 스테이지 노드에 (Combat, Regular)/(Combat, Boss)가 기록되고, 클리어 갱신 뒤에도 유지되며, 해당 키의 노드 외형이 ViewGroup에 있음을 확인했다.

## 7. 공용 타입

- 아래 타입은 **옛 Core.Contracts의 것을 그대로 재사용**한다. `ProjectL.Systems`가 이미 Core.Contracts를 참조하고 있기 때문이다.
  - `EntityKey`, `Rotation4`, `AppointMethod`, `AttackTargetMode`, `UnitOwnerEligibility`, `TraitKey`, `CellCoord`
- `CellCoord`는 좌표와 오프셋(공격 패턴)을 모두 표현한다. SO 입력은 직렬화 가능한 `CellInput`으로 받아 변환한다.
- `StageType`, `StageMapRole`만 새 공용 위치(`DataBaseSystem/Data/Common/`)로 옮긴다.

**확인 요청:** 옛 CoreCaculator(특히 Contracts)를 삭제하거나 정리할 계획이 있으면 알려 주세요. 그때 위 타입을 `Data/Common/`으로 옮기겠습니다.

## 8. Core 데이터에서 빠지는 View 필드

옛 Core SO에 섞여 있던 표현 필드다. InGameGroup으로 복구할 때 **넣지 않는다.** ViewGroup이 대신 맡는다.

| 옛 위치 | 필드 | 대체 |
|---|---|---|
| StageDefinitionSO | `_viewStageType` | View가 Buffer의 StageType + MapRole로 판단 |
| FieldDefinitionSO | `_fieldVisual` | ViewGroup StageFieldViewTable.cellPrefab |
| UnitDefinitionSO / StructureDefinitionSO | `_displayName` | ViewGroup UnitVisualTable.displayName |
| (InGameGroup) StageViewPropTable | StageKey → GameObject | ViewGroup으로 이동 (GameObject는 P1 위반) |

Architecture(Catalog + 정의 5)는 다른 View 작업자가 맡는다. 이번 재구성 범위가 아니다.

## 9. 데이터 주의사항

| 항목 | 내용 |
|---|---|
| Trait 값 | 옛 에셋의 Trait은 SerializeReference 타입명(`ProjectL.Unity...*TraitAuthoring`)에 묶여 있다. 새 타입명과 달라 자동으로 옮겨지지 않으므로 새 에셋에 직접 입력한다. Akimbo: MoveAttackDiscount(1, 1), Sniper: Focus(1, 3) |
| 누락값 | ShotGun, Headquarters 에셋에는 `_ownerEligibility` 값이 없다. 기본값 PlayerOnly로 복구한다 |
| 오타 수정 | ShotGun displayName `ShouGun` → `ShotGun` |
| 미사용 | Field_8x6(스테이지 미참조), PlayerLoadout(StageDefinition 미참조), `StageEnum`(테스트의 어셈블리 기준 타입으로만 사용하므로 유지) |
| 데이터 없음 | EntityKey 14~17(MainCharacter, Ballista, CactusCannon, Gravekeeper)은 유닛 정의 에셋이 없다. [묘지기 인계 문서](2026-09-21-shared-knockback-gravekeeper-view-handoff.md)의 `Gravekeeper.asset`은 이 저장소 이력에 없다 |
| 연결 알고리즘 | 옛 StageMapChapter1에는 연결 알고리즘·가중치 값이 없다. 기본값(Nearest, 10, 1)으로 복구한다 |

## 10. 옛 에셋 보존

- 커밋 `a18d86d`(2026-09-22)에서 삭제된 스크립트 16개 때문에 `Assets/ProjectL/DataBaseSO`의 옛 에셋 48개가 Missing Script 상태다.
- 값은 새 구조의 새 에셋으로 옮기고, **옛 에셋은 삭제하지 않는다.**
- 옛 스크립트는 `git show a18d86d^:Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/<경로>`로 볼 수 있다.

**확인 요청:** 옛 에셋을 언제 삭제할지 알려 주세요.

## 11. 향후 협의: 전투 필드 Buffer (B2)

전투 View가 필요로 하는 값이다. 전투 View 작업 때 같이 정의한다.

- 맵 크기, 엔티티 목록(EntityKey, 소유자, 위치, 회전, 점유 셀), 양측 배치 깊이(배치 가능 영역 표시)
- 배치 미리보기(가능/불가) 결과를 View가 받는 경로

## 12. 응답 양식

| # | 확인 사항 | 답변 | 담당 | 날짜 |
|---|---|---|---|---|
| 2 | 데이터 흐름 전제(새 Core는 Systems 시스템, View는 Buffer) | | | |
| 5 | Stage 데이터 API 변경(M3/M4, Combat 검증) 영향 | | | |
| 6 | Buffer B1 구현(View 작업에서 완료) 확인. 시작 노드 기본값 처리 방식 포함 | | | |
| 7 | 옛 CoreCaculator/Contracts 정리 계획 | | | |
| 10 | 옛 에셋 삭제 시점 | | | |
| 11 | 전투 필드 Buffer 협의 시점 | | | |

## 13. 전투 진행 골격: Core 서비스·결과·수명 요청 (2026-09-30)

> §13.1~§13.5는 Phase 0 당시 요청 이력이다. `83f485d1` 병합 후 현재 인수 상태와 추가 요청은 **§13.6**, 실제 API·검증 상태는 [골격 계획 §3.1·§6.7](../plans/2026-09-30-combat-flow-skeleton-plan.md)을 따른다.

**상태: 요청 문서 준비 완료, 담당자 전달·답변·합의 미확인. Core 코드 수정 없음.** 상세 기준은 [전투 진행 설계 4차](../specs/2026-09-30-combat-flow-skeleton-design.md), 일정·인수 조건은 [구현 계획 §3](../plans/2026-09-30-combat-flow-skeleton-plan.md#3-선행-계약과-인수-조건)이다. R 번호는 [기존 Core 요청](2026-09-29-combat-view-buffer-core-requests.md)을 사용한다. F1~F3는 이번 Flow 설계의 조건이며 이전 Core 계획의 G 번호와 구분한다.

### 13.1 소유 경계 확인 요청

| 영역 | 이번 설계의 역할 분담 |
|---|---|
| CombatLifeCycle·CombatFlow | View 작업이 수명 연결과 다음 작업의 등록 조건을 구현 |
| CombatPlan·전투 Job 네 개 | **준비 Start까지 포함해 View 작업이 구현.** Core 서비스가 준비되면 Job 몸통의 호출 연결도 View 작업이 담당 |
| JobType | View 작업이 ProcessCommand(3), ProcessPreview(4), AdvanceCombat(5)를 추가하고 Framework 담당자에게 공유 |
| Core 서비스·규칙·상태/결과 기록 | Core 담당자가 제공. Flow·Job에서 규칙 계산이나 Buffer 쓰기를 대신하지 않음 |
| F3 결과 읽기 계약·서비스 등록 | 실제 타입·소유 위치·공개 조회 경로를 Core/Framework 담당자와 확정한 뒤 연결 |
| F2 마지막 결과·EndBattle·중도 이탈 | Core·Framework·View가 수명과 호출 주체를 합의 |

이전 Core 계획 Task 4·8에 등장한 CombatPlan·CombatJob·JobRootType 변경은 위 분담에 맞춰 중복 작업을 피하도록 확인을 요청한다. 옛 계획 자체를 현재 API의 근거로 삼지는 않는다.

### 13.2 처리 서비스 네 개와 기록 요청

Phase 0의 소스 확인 결과, `IInGameCoreSystemGroupService`에는 StageGeneratorSystem·StageServiceManager·ChapterProgression만 있고 전투 서비스 노출은 없다. `CombatStateBuffer`에는 읽기용 순번·처리 번호가 있지만 갱신 구현은 없다. 아래는 **제공 요청**이다.

| 처리 | 필요한 서비스 계약·동작 | 실제 API 상태 |
|---|---|---|
| 준비 | Core가 준비 재료의 출처와 조립을 책임지고 초기 전투 상태를 설치. 정상 반환 후 HasBattle 확인 가능 | 메서드·입력 출처 확인 필요. 기존 BattleWriteSink.PrepareBattle만으로 처리 서비스가 완성된 것은 아님 |
| 명령 | HandledCommandNumber 기준으로 다음 명령 하나를 처리. 수락·거절 모두 처리 번호와 결과를 기록 | `CoreGroup.CombatCommands.TryExecuteNext()`는 설계에서 유지하려는 이름이며 현재 코드에는 없음 |
| 미리보기 | 현재 요청을 읽어 계산하고 응답 번호 기록. 게임 상태·결과 순번은 변경하지 않음 | 서비스 이름·반환 의미·그룹 노출 미정 |
| 자동 진행 | 한 호출의 종료 결과 하나를 상태·결과 순번과 함께 기록. 수락/거절·이유 조회 가능 | 서비스 이름·반환 의미·그룹 노출 미정 |

확인할 사항:

- 네 API의 파일·네임스페이스·어셈블리·정확한 멤버와 CoreGroup 조회 경로
- R1 결과 순번, R2 명령 처리 번호, R3 미리보기 응답 번호, R4 국면 갱신 시점
- 명령 서비스의 bool이 처리 여부를 뜻하는지 확인. 게임 규칙의 수락 여부와 분리
- 한 처리는 한 번의 동기 호출에서 끝나고, Flow가 등록한 경로 외에서 상태·최신 결과를 덮어쓰지 않는 전제
- 준비 재료의 출처(Core 이전 계획 G7 관련), 정상 종료와 예외의 보장

예외는 Job이 원인을 보존해 전파한다. 자동 진행 거절은 정상 반환해도 Flow 정지 대상이며 명령 거절과 구분한다.

### 13.3 F3: 타입·읽기 서비스 등록·실제 결과 기록을 구분

| 제공 단계 | 요청 내용 | 필요한 시점 |
|---|---|---|
| 읽기 타입 | 결과 순번, 처리 종류, 수락/거절, 이유와 조회 방식. 가칭 ICombatResultReadSink의 실제 이름·값 타입·파일·어셈블리 확정 | Phase 4 Flow 단위 구현·검증 준비 전 |
| 읽기 서비스 구현·등록 | 실제 인스턴스 제공, Export/서비스 키 또는 그룹 속성, LifeCycle에서 조회할 수 있는 경로 | **Task 4.4 및 Phase 4 생산 코드 연결 완료 전** |
| 실제 기록·보존 | 자동 진행 결과·상태·순번의 일관된 기록, View 확인 후에도 다음 결과로 교체될 때까지 최신 레코드 조회 가능 | Phase 5 실제 처리 검증 전 |

Runtime 순서는 Runner → View → Flow다. View가 먼저 결과를 확인·정리해도 Flow가 자동 진행 수락 여부를 읽을 수 있어야 한다. 결과 목록을 비우는 정책과 최신 레코드 보존이 충돌하지 않는지 확인을 요청한다. 순번 증가만으로 자동 진행 성공을 판정하지 않는다.

CombatLifeCycle은 전투 진입 전에도 PostBindService를 받는다. 따라서 **타입만 정의하고 실제 읽기 서비스를 등록하지 않은 상태는 LifeCycle 바인딩 완료가 아니다.** Flow 단위 검증에는 가짜를 쓸 수 있지만 생산 바인딩을 통과시키려고 빈 결과 서비스를 임의로 만들지 않는다.

### 13.4 F2: 결과 확인·전투 종료 수명 요청

Framework와 협의할 전환 순서는 새 Flow 등록·View 입력 중단 → 기존 Job 종료 → 마지막 결과 표시·확인과 Core 정리 → 최종 Idle → Change/Exit다. 입력 Clear는 기존 Job이 입력을 읽은 뒤에 수행한다.

Core 쪽 확인 요청:

- 마지막 결과 표시·확인과 `EndBattle` 중 어느 호출이 먼저이며 누가 책임지는가
- 정상 전투 종료와 중도 이탈에서 결과 반영·취소·정리의 차이
- 다음 전투에 이전 상태·미확인 결과가 남지 않도록 번호·결과·준비 수명을 어떻게 보장하는가
- 정리에 별도 Job이 필요한가. 필요하다면 전환 전에 완료를 확인할 경로는 무엇인가

F1의 실행 예외 뒤 Runtime 중단은 별도 Framework 작업이다. 정상 이탈 절차로 치명적 실패를 복구했다고 처리하지 않는다. [Framework 전달 문서](2026-09-30-combat-flow-framework-share.md)에 F1·F2 요청을 함께 적었다.

### 13.5 답변·인수 기록

| 항목 | 요청 문서 | 담당자 전달 | 답변·합의(담당/날짜) | 실제 파일·멤버·등록 경로 | 구현·검증 증거 |
|---|---|---|---|---|---|
| Flow/Plan/Job 소유 경계 | 준비 완료 | 미확인 | 미수신 | 우리 예정 파일은 구현 계획 §4 | 코드 미착수 |
| 준비·명령·미리보기·진행 서비스 / R1~R4 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F3 읽기 타입 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F3 읽기 서비스 구현·등록 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F3 결과 기록·보존 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F2 결과 수명·EndBattle | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |

Phase 0은 이 요청을 전달 가능한 파일로 준비하는 단계다. 답변을 받기 전 Core 인터페이스·저장 구조·처리 서비스를 View 작업에서 추측해 추가하지 않는다.

### 13.6 병합 후 인수와 F2 추가 요청 (2026-10-02)

**현재 인수:** `83f485d1`의 `ICombatProcessingService` 준비·명령·미리보기·자동 진행 API와 실제 결과 발행·보존. Flow·Plan·Job은 View 소유를 유지한다. View의 Phase 5는 명시적 준비 fixture로 Core 수정 없이 진행할 수 있다. 병합 후 사용자 확인은 컴파일 오류 없음·EditMode 미실행이다.

**추가 요청 초안 — 미전달·미합의:** 현재 `AbandonBattle(ArtifactLoadoutSnapshot)`는 호출자가 정산 입력을 제공해야 한다. View는 Core의 DB·규칙 입력을 조립하지 않으므로, Core가 현재 로드아웃을 읽어 포기 정산까지 수행하는 공개 경계(예: 인자 없는 호출 가능 여부)를 확정해 주기 바란다. 정확한 API 이름·반환·중복 호출 의미를 받기 전 View에서 가칭 구현을 만들지 않는다.

| 확인할 계약 | 현재 코드와 제안 |
|---|---|
| 포기 결과 | 현재는 패배 Completion을 기록하며 새 CombatResult·결과 순번·Finished 전환을 만들지 않는다. 이 의미를 유지하는지 확인 요청 |
| 기존 완료 | 이미 Completion이 있으면 정상 승패 정산을 포기로 덮어쓰지 않는다. View 전환 소유자도 중복 정산을 요청하지 않음 |
| 결과 표시 수명 | 남은 일반 CombatResult 확인 → 필요 시 포기 정산 → Completion 표시 완료 → EndBattle → 전환. 표시 완료는 View 수명 콜백이며 가짜 결과 순번을 만들지 않음 |
| EndBattle | Completion 전에는 거부하고, 정리 뒤 결과·번호와 Completion은 유지하며 다음 준비에서 전투 Completion을 초기화하는 현재 보존 의미 확인 |
| 운영 준비 | 기본 DB 소스는 등록됐지만 T1/G7의 타게팅 Method_Key·유닛 참조/query 계약이 부족함. Core가 출처·조립을 완성하고 운영 DB 성공 증거를 제공하는 별도 인수 필요 |

F1은 View의 GameBootstrap·단일 RuntimeFaultBoundary로 구현하고 골격 Phase 6에서 인수한다. F2 전환 요청 소유자·공개 정지 경로는 Framework와 별도 협의한다. 이번 작업은 문서 요청 준비이며 Core 코드 수정·외부 메시지 전송·담당자 합의 완료가 아니다. 상세 순서와 검증은 [설계 §6.2·§12.4](../specs/2026-09-30-combat-flow-skeleton-design.md)와 [인수 안내 §2.7](../plans/2026-09-30-combat-flow-core-intake-guide.md)를 따른다.

### 13.7 전투 View 착수 메모 (2026-10-02)

전투 View Phase 0에서 Core에 보낼 답변·확인 요청을 [전투 View Core 전달 메모](2026-10-02-combat-view-core-notes.md)에 따로 정리했다. 내용: 준비 인덱스·리비전은 당장 필요 없음(답변), 거절 `Reason` 코드 목록 확인, 시작 재화를 채우는 공개 경로(운영 전 결정), 시작 서브 수 규칙(고정 3 vs 덱 상황에 따라), 개발 전용 진행 준비 경로 알림. 모두 미전달이며 급하지 않다.

### 13.8 골격 6B 진행에 따른 Core 요청 정리 (2026-10-05, 미전달)

최신 요청·담당 경계·인수 조건을 날짜별 독립 문서인 [2026-10-05 Combat Core 요청사항](2026-10-05-combat-core-requests-phase6b.md)으로 옮겼다. 이 절은 기존 링크를 위한 이동 안내이며, 요청 내용은 새 문서에서 관리한다.

### 13.9 전투 타임라인 예측 Buffer 게시 요청 (2026-10-06, 미전달)

View가 `GetTimeline`을 직접 부르는 대신 Core가 예측을 Buffer에 게시하도록 요청한다. 계약 원본은 [구조 개선 설계 VR0](../specs/2026-10-06-combat-view-refactor-design.md#vr0--타임라인-게시-요청), 전달용 요약은 [타임라인 요청 메모](2026-10-06-combat-timeline-core-request-memo.md)다. 미전달·미합의·미구현이며 View는 합의 전까지 현재 호출을 유지한다.

- **2026-10-09 갱신(미전달):** Buffer에 이미 `CommandState.Timeline`이 있고 `TimelineUI`가 읽는다. HUD는 여전히 `GetTimeline()`을 직접 부른다. 두 값의 관계(같은 예측인지, 길이·갱신 시점)를 묻는 질문을 메모 §5에 더했다. 근거는 [SOLID·SSOT 평가](../specs/2026-10-09-combat-view-solid-ssot-evaluation.md) §4.1.

### 13.10 전투 진행 Job·Plan 재구성 알림 (2026-10-10, 미전달)

전투 진행 판단을 `CombatPlan` 한 곳으로 옮기고 Core 호출을 모두 전투 Job으로 옮긴다([재구성 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md)). **Core 요청은 없다.** Core 처리 API의 의미·서명은 그대로이고, `AbandonBattle`·`EndBattle`·`CompleteChapter`의 호출 위치만 `CombatExitSequence`에서 Job으로 바뀐다(K22 A2 변경). 진행 사실은 우리 영역의 읽기 객체가 Buffer에서 읽는다.

### 2026-10-04 머지 후 View 연결 복원
사용자 요청으로 상대방 Core·Combat 조립을 유지하며 StageMapViewSystem을 함께 등록했다. BufferSystemGroup.cs에는 StageView·ArtifactView 네임스페이스 using만 복원했고 계산·발행 로직은 수정하지 않았다. Buffer Factory·서비스 키에도 같은 참조를 복원했다. 진행 기록과 실제 검증 결과는 stage-map-view-plan.md §7을 참조한다.

### 2026-10-04 카메라 병합 충돌 해결
사용자 명시 지시로 b5d14d7 병합 시 MapProgressBuffer의 최신 IDataBaseQueryContext 및 Store 계약을 유지했다. 자동 병합으로 바뀐 StageGeneratorSystem·StageServiceManager 초기화도 현재 Core 계약으로 유지했다. Core 계산 변경 없음. 렌더 API 이동에 따른 전투 View 참조를 새 경로·Handle 명칭에 맞췄다. 검증 상태는 stage-map-view-plan.md §7 참조.

