# DB-SO 재구성 — 로컬 세션 인계 문서

> 클라우드 세션(2026-09-24~25)에서 진행한 DB-SO 재구성 작업을 로컬 Claude 세션이 이어받기 위한 문서다.
> 새 세션은 **이 문서 → 저장소 루트 `CLAUDE.md` → 설계 문서** 순서로 읽고 시작한다.

- 작성일: 2026-09-25
- 작성: View 작업 (클라우드 세션)
- 대상 브랜치: `feature/Yonggeun/DB_SO` (최신 `f32dea2` "[용근] SO 이전 1차")
- 상태: **DB-SO 이전 1차 완료.** 사용자가 에디터에서 EditMode 테스트 전체 통과와 인스펙터 값을 확인했다(2026-09-25).

## 1. 한눈에 보기

| 항목 | 내용 |
|---|---|
| 목표 | 삭제된 옛 SO 스크립트 때문에 Missing Script가 된 DataBaseSO 에셋 48개의 값을 새 Framework/Systems 구조로 복구 |
| 결과 | InGameGroup(규칙, 순수 C#) 8개 테이블 + ViewGroup(표현, Unity 타입) 5개 테이블, 새 에셋 71개, Buffer B1, EditMode 테스트 32개 |
| 남은 일 | §6 (§6.1·§6.2 완료, §6.3부터) |
| 기준 문서 | [설계](../specs/2026-09-24-db-so-ingame-view-group-design.md) · [계획·실행 기록](../plans/2026-09-24-db-so-restructure-plan.md) · [옛 에셋 인벤토리](../specs/2026-09-24-view-db-orphan-asset-inventory.md) · [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) |

## 2. 작업 방식 (로컬 세션에서도 그대로)

- 규칙 전체는 저장소 루트 `CLAUDE.md`에 있다. 요점만 다시 적는다.
- **커밋·push는 사용자가 한다.** 사용자가 명시적으로 시킬 때만 예외(`f32dea2`가 그 예).
- 큰 작업은 설계 → Task 계획 → Phase 단위 구현. Phase가 끝나면 멈추고 보고한다.
- 설계·계획과 다른 결정이 필요하면 **구현하지 말고 먼저 묻는다.**
- 옛 에셋 48개(`Assets/ProjectL/DataBaseSO/Stage`, `Architecture` 등)는 삭제하지 않는다.
- Core 로직, FieldCamera, Architecture는 다른 작업자 영역이다. Core 수정이 필요하면 Core 전달 문서에 요청으로 적는다.
- GwonCheonmyeong 작성 문서는 기준으로 삼지 않는다.
- 클라우드 세션은 사용자 PC에 직접 쓸 수 없어서 **패치 파일**로 전달했다. 로컬 세션은 파일을 직접 고치면 되므로 패치가 필요 없다.

## 3. 브랜치 이력

`d852362`(develop, 2026-09-24)에서 분기했다.

| 커밋 | 내용 |
|---|---|
| `53d4860`, `7c3fbd2` | 사용자 정리: `RenderGroup/ColorTable.meta`, `docs/superpowers/viewDocs` 문서 3개와 txt 1개 삭제 |
| `723c6ab` | Phase A·B (기존 코드 P2 분리, InGame 신규 테이블) + 설계·계획·인벤토리·Core 전달 문서, `CLAUDE.md`. 패치 전달용 `db-so-phaseA/AB/B.patch`가 저장소 루트에 같이 커밋됨 |
| `2cba1ef` | 원격 브랜치 merge |
| `39b634f` | Phase C·D·E (ViewGroup 코드, Buffer B1, 에셋 71개). `db-so-phaseC.patch`가 같이 커밋됨. **루트의 패치 4개는 삭제 필요(§6.2)** |
| `f32dea2` | Phase F (EditMode 테스트 4개 파일, 설계·계획 문서 마무리) |

## 4. 완료한 작업 (Phase별)

상세는 [계획 문서의 실행 기록](../plans/2026-09-24-db-so-restructure-plan.md#실행-기록)에 있다.

### Phase A — 기존 코드 정리

- `StageType`, `StageMapRole`을 `DataBaseSystem/Data/Common/`으로 이동.
- StageMap, StageRoot, Stage 상세(Combat/Shop/Event)를 **P2 구조**로 분리: SO는 입력 필드만, `GetData()`가 순수 불변 Data를 매번 새로 만든다. `StageRootData.StageData` 타입은 `IStageData`.
- Core 파일 3개(`StageGeneratorSystem`, `StageServiceCollection`, `StageServiceManager`)에 using 1줄씩 추가(Core 전달 문서 §5.1).

### Phase B — InGameGroup 신규 테이블

- `CellInput`(SO 입력용 셀 구조체) → `CellCoord`로 변환.
- Field(키 FieldId), Unit(키 EntityKey, Trait 5종 SerializeReference), Structure(키 EntityKey), EnemyLoadout/PlayerLoadout/UnitRoster(키 int).
- `CombatStageData`에 전투 필드 추가. SO 참조는 `GetData()`에서 키로 바꾼다(P3). Field·Roster 없으면 거부.

### Phase C — ViewGroup

- `ViewGroup`, `IViewQuery`, `ViewDataBaseFactory`, `DataBaseGroupKey.View`, `ViewTableKey`, `IHasViewQuery`.
- StageViewProp을 InGame에서 View로 이동(GUID 유지, `MovedFrom`).
- StageFieldView(설정형), StageMapView(설정형), StageNodeVisual(키 `(StageType, StageMapRole)`), UnitVisual(키 EntityKey).

### Phase D — Buffer B1 (사용자 지시로 Core 로직 수정, 설계 D21)

- `StageProgressData`에 `StageType`, `MapRole` 추가. `StageGeneratorSystem`이 노드 기록 때 채우고 `StageProgressBuffer.SetClear`가 복사한다.
- 시작 노드(StageKey 0)는 기본값(`Event`/`Regular`)이므로 View는 `StageKey == 0`을 먼저 확인해야 한다.
- Core 작업자 확인 요청 중(Core 전달 문서 §5.3, §6, §12).

### Phase E — 에셋 71개

- `Assets/ProjectL/DataBaseSO/InGameGroup/` 56개, `ViewGroup/` 15개. `DatabaseCatalogSO.asset`에 두 그룹 등록.
- 값은 옛 에셋 인벤토리에서 옮겼다. Trait은 Akimbo MoveAttackDiscount(1, 1), Sniper Focus(1, 3).
- 새로 부여한 키: StageMap 1, Stage 1101~1110 / 2101(Boss), EnemyLoadout 1~5, PlayerLoadout 1, UnitRoster 1.

### Phase F — 테스트와 마무리

- `Assets/Tests/EditMode/Framework/`: `DataBaseTestAssets`(도우미), `InGameDataTableTests`(20), `ViewDataTableTests`(7), `DataBaseAssetTests`(5, 실제 에셋 교차 검증).
- 설계 문서 §11.2 에디터 체크리스트, §14 구현 결과(설계와 달라진 점).

## 5. 알아 둘 것

| 항목 | 내용 |
|---|---|
| 데이터 흐름 | InGameGroup → Core 시스템(`InitData`) → Buffer → View. **View는 InGameGroup을 읽지 않는다.** 규칙 값이 필요하면 Buffer 필드를 정의한다 |
| DB 에셋을 고칠 때 | `DataBaseAssetTests`를 돌린다. Unit/Structure ↔ UnitVisual, 스테이지 ↔ NodeVisual, 전투 스테이지 키 참조를 확인한다 |
| 새 테이블을 만들 때 | 설계 §10 코딩 규칙과 기존 테이블(`UnitTable`, `UnitVisualTable`)을 따른다. InGame은 순수 Data 파일과 SO 파일을 분리, View는 `[Serializable] XxxData` + `XxxDataSO` 한 파일 |
| `Enum.IsDefined` | `Data.InGame` 아래에서는 `System.Enum.IsDefined`로 쓴다(네임스페이스 이름 충돌) |
| Trait 인스펙터 | `[SerializeReference]` 목록에 항목을 추가하는 UI가 없다. 기획자가 Trait을 추가하려면 PropertyDrawer가 필요하다 |
| 유닛 프리팹 | UnitVisual의 유닛 프리팹 4개는 Prefab Variant다 |
| 파일 이동 | 파일을 옮기거나 지울 때 옛 파일이 디스크에 남지 않게 한다. 패치 적용(`git checkout -- .`)이 삭제를 반영하지 않아 옛 StageViewProp 파일이 남아 CS0104(ambiguous reference) 에러가 난 적이 있다 |
| 에셋 재직렬화 | Unity가 에셋을 열면서 형식을 다시 써 diff가 생길 수 있다. 값이 같으면 커밋해도 된다 |

## 6. 남은 일 (우선순위 순)

### 6.1 develop 동기화 — 완료 (2026-09-25, 로컬 세션)

> **결과:** develop(`8a5e703`)을 이 브랜치로 merge했다(방향: develop → `feature/Yonggeun/DB_SO`, 폴더 구조는 develop 기준).
> - `ViewDataBaseFactory.cs`와 `.meta`는 `Factory/DBFactory/Group/`으로 옮겼다. GUID `4f1a80cd…`는 유지했고 옛 `Factory/Loading/` 폴더는 남지 않았다.
> - 네임스페이스 3파일은 `ProjectL.Systems.Factory.DBFactory`(`.Group`)로 바꿨다. `LodingSystemFactory`와 `Factory.Loading` 검색 결과는 0건이다.
> - 자동 merge 9파일을 확인했다. develop 쪽 변경은 `#region` 구획, LodingFactory 경로, 로딩 테스트 교체뿐이고 이 브랜치의 ViewGroup·Buffer B1 코드는 그대로 남았다.
> - `CLAUDE.md`와 설계 §8 경로를 고쳤다.
> - merge 전 지점은 로컬 브랜치 `backup/db-so-before-develop-sync`에 있다.
> - 남은 결정: `#region` 관례를 새 코드에 맞출지는 아직 정하지 않았다.
>
> 아래는 merge 전에 적은 사전 조사 기록이다.

develop이 분기 뒤 4커밋 앞서 있다(`8a5e703`, JMH 로딩 시스템 개선·시네머신 등). **이 브랜치를 develop에 merge하기 전에 반드시 맞춰야 한다.** 클라우드 세션에서 시험 merge한 결과:

- **폴더·네임스페이스 이동:** develop이 `Factory/Loading/LodingSystemFactory/DataBaseSystemFactory/`를 `Factory/DBFactory/`로 옮겼다. 네임스페이스도 `ProjectL.Systems.Factory.DBFactory`(`.Group`)로 바뀌었다.
  - Git이 멈추는 곳: 이 브랜치에서 새로 만든 `ViewDataBaseFactory.cs`(+`.meta`)의 위치 충돌. `Factory/DBFactory/Group/`으로 옮기면 된다(GUID 유지).
  - Git은 통과하지만 **컴파일이 깨지는 곳:** 옛 네임스페이스를 쓰는 파일. `ViewDataBaseFactory.cs`(namespace), `DataBaseAssetTests.cs`, `InGameDataTableTests.cs`(using). merge 뒤 `LodingSystemFactory.DataBaseSystemFactory`를 검색해 모두 바꾼다. `DataBaseFactory.cs`의 ViewGroup 생성 코드가 merge 결과에 남았는지도 확인한다.
- **자동 merge되는 파일:** `DataBaseGroupSystem.cs`, `InGameDataBase.cs`, `StageViewPropTable.cs`, `InGameDataBaseFactory.cs`, `StageProgressBuffer.cs`, `StageGeneratorSystem.cs`, `StageServiceManager.cs`, `FrameworkRuntimeCompositionTests.cs`. develop 쪽 변경은 대부분 `#region 서비스` / `#region 시스템 모듈` 구획 추가다. merge 뒤 내용이 맞는지 눈으로 확인한다.
- **새 코드 관례:** develop 코드에 `#region 서비스`(인터페이스) / `#region 시스템 모듈`(구현 클래스) 구획이 생겼다. 문서에는 아직 없다. 이번에 만든 새 테이블 파일에도 맞출지 사용자에게 묻는다.
- 문서: develop의 `framework-architecture.md`, `framework-programmer-guide.md`가 로딩 흐름과 DB 팩토리 경로를 바꿨다. merge 뒤 `CLAUDE.md` 기준 문서 목록과 설계 §8 경로를 새 경로로 고친다.
- 방법은 사용자와 정한다(develop을 이 브랜치로 merge할지, 이 브랜치를 develop에 merge할지). 커밋 제목 `723c6ab`에 "머지 금지"가 있으니 **사용자 확인 없이 merge하지 않는다.**

### 6.2 정리 — 완료 (2026-09-25)

- ~~저장소 루트의 `db-so-phaseA.patch`, `db-so-phaseAB.patch`, `db-so-phaseB.patch`, `db-so-phaseC.patch` 삭제(전달용이었고 저장소에 필요 없음).~~ merge 커밋 `2447692` 뒤 삭제.

### 6.3 다른 작업자와 협의

- Core 작업자: [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §12 응답 받기. 특히 Stage 데이터 API 변경(M3/M4)의 영향, Buffer B1 구현 확인, 옛 CoreCaculator/Contracts 정리 계획, 옛 에셋 삭제 시점.
- 옛 에셋 48개 삭제는 Core 작업자 답을 받은 뒤 사용자가 정한다.

### 6.4 후속 작업 후보 (사용자가 정하면 설계부터)

| 후보 | 내용 |
|---|---|
| Trait PropertyDrawer | UnitDataSO의 Trait 목록에 타입을 골라 추가하는 인스펙터 UI |
| 스테이지 맵 View | Buffer(StageProgress) + ViewGroup(StageMapView, StageNodeVisual)로 맵을 그리는 View 시스템 |
| 전투 필드 Buffer(B2) | 전투 View에 필요한 맵 크기·엔티티·배치 깊이. 전투 View 작업 때 Core와 함께 정의(Core 전달 문서 §11) |
| FieldCamera, Architecture | 다른 작업자 영역. 건드리지 않는다 |

## 7. 검증 방법 (로컬)

- Unity 에디터 컴파일 → Test Runner → EditMode → `ProjectL.Tests.Framework` 전체 실행. 체크가 모두 초록이면 된다(테스트 중 예상하지 않은 `Debug.LogError`도 실패로 처리됨).
- 새 순수 Data 파일에 `using UnityEngine`이 없는지 확인(P1).
- 클라우드 세션에서는 Unity가 없어 Mono + Roslyn + UnityEngine stub으로 부분 컴파일하고 NUnit을 돌렸다(설계 §11.1). 로컬에서는 에디터가 있으니 필요 없다.

## 8. 새 세션 시작 예시

```text
docs/superpowers/handoffs/2026-09-25-db-so-local-session-handoff.md 읽고 현재 상태 파악해줘.
다음 작업은 §6.1 develop 동기화야. 바로 merge하지 말고 먼저 계획부터 보여줘.
```
