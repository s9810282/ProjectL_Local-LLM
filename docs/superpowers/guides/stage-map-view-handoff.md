# ProjectL Stage Map View Handoff

이 문서는 현재 Core 공개 API를 기준으로 Unity View가 Stage Map을 생성하고, 노드와 연결선을 표시하고, 선택 결과로 실제 Stage를 시작하는 방법을 설명한다. 구현 계획이나 변경 이력이 아니라 **현재 동작만 설명하는 인계 문서**다.

> View는 `ProjectL.Core.Application`과 `ProjectL.Core.Contracts`만 사용한다. `ProjectL.Core.Domain`을 참조하거나 연결·선택 가능 여부를 View에서 다시 계산하지 않는다.

실제 Stage의 초기 Enemy, Railway Gimmick, Timeline, 배치·이동·공격 연결은 [StageSession View 통합 인계 가이드](unit-movement-attack-core-handoff.md)를 따른다.

## 1. 두 Session의 책임과 수명

| 객체 | 수명 | 책임 |
|---|---|---|
| `StageMapSession` | Stage Map 한 런 | 생성된 노드/Edge, 현재 노드, 다음 선택 가능 노드 보관 |
| `StageSession` | 선택한 실제 Stage 전투 하나 | Grid, Entity, 초기 Enemy, Rail, Gimmick Timeline, 전투 상태 보관 |

`StageMapSession`은 `StageConfig`를 사용하지 않는다. Stage Map은 어떤 `StageKey`를 선택했는지만 결정하고, 선택된 Key를 실제 전투 데이터로 해석하는 일은 Unity Data/Bootstrap 책임이다.

```text
StageMapSession.TrySelectNode
→ StageSelectionResult.SelectedStageKey
→ Unity Stage Data Catalog 조회
→ StageConfig + UnitSpec Catalog 구성
→ StageSessionFactory.Create
→ 선택한 Stage의 View 진입
```

`StageMapSession`이 `StageSessionFactory`를 직접 호출하거나 둘을 내부에서 소유하지 않는다. Scene 전환, 로딩, 선택한 Stage 데이터 조회, 전투 종료 뒤 맵 복귀는 Unity 흐름이 조정한다.

## 2. Assembly와 namespace

View asmdef가 참조할 Core Assembly는 다음 둘뿐이다.

```text
ProjectL.Core.Contracts
ProjectL.Core.Application
```

Stage Map View에서 주로 필요한 namespace는 다음과 같다.

```csharp
using System;
using ProjectL.Core.Application.StageMap;
using ProjectL.Core.Application.StageRun;
using ProjectL.Core.Contracts.Commands;
using ProjectL.Core.Contracts.Ids;
using ProjectL.Core.Contracts.Results;
using ProjectL.Core.Contracts.Snapshots;
```

## 3. Stage Map 생성

```csharp
StageMapGenerationConfig config = new StageMapGenerationConfig(
    depthCount: 6,
    widthMax: 3,
    additionalConnectionChance: 0.25,
    seed: 7301);

StageKey[] regularStageKeys =
{
    new StageKey(100),
    new StageKey(100),
    new StageKey(200),
    new StageKey(300),
};

StageMapSession mapSession = StageMapSessionFactory.Create(
    config,
    regularStageKeys,
    bossStageKey: new StageKey(900));
```

| 입력 | 의미 |
|---|---|
| `DepthCount` | 시작과 Boss Depth를 포함한 전체 Depth 수. 최소 2 |
| `WidthMax` | 일반 Depth 하나의 최대 노드 수. 최소 1 |
| `AdditionalConnectionChance` | nearest 연결 외 second-nearest Edge를 추가할 확률. 0..1 |
| `Seed` | 같은 입력으로 같은 맵을 재현하기 위한 seed |
| `regularStageKeys` | 일반 노드에 배정할 Stage 후보. 중복 Key는 가중치로 사용할 수 있음 |
| `bossStageKey` | 마지막 Boss 노드에 배정할 Stage |

`DepthCount > 2`라서 일반 Depth가 있다면 `regularStageKeys`에는 최소 하나의 비어 있지 않은 `StageKey`가 필요하다. 일반 목록에 Boss Key를 섞을 수 없고 `bossStageKey`도 비어 있을 수 없다. 같은 일반 Key를 여러 번 넣으면 추첨 목록에서 그 Stage의 비중이 커진다. 저장/재현이 필요하면 `StageMapGenerationConfig`와 Stage Key 목록을 같은 순서로 보관한다.

## 4. 생성된 맵의 보장과 View 책임

Core가 생성한 맵은 다음을 보장한다.

- Depth 0에는 `StageKey.Empty`인 시작 노드 하나가 있다.
- 마지막 Depth에는 지정한 `bossStageKey`의 Boss 노드 하나가 있다.
- Edge는 다음 Depth 방향으로만 향하므로 핵심 그래프는 DAG다.
- 각 non-Boss 노드는 다음 Depth의 nearest 노드와 연결된다.
- 확률 조건을 만족하면 second-nearest 노드와 추가 연결될 수 있다.
- incoming이 없는 다음 Depth 노드는 이전 Depth에서 보정 연결을 받는다.
- 모든 경로는 최종 Boss 노드로 수렴한다.
- self Edge와 중복 Edge는 없다.

이 보장은 View가 검증하거나 보정할 규칙이 아니다. View는 Snapshot의 `Depth`, `WidthIndex`, `NextNodeIds`를 레이아웃과 연결선 입력으로만 사용한다.

`Depth`와 `WidthIndex`는 논리 좌표다. 실제 픽셀/월드 위치, 곡선 모양, 애니메이션은 View 책임이지만 노드 재정렬 때문에 Edge 의미나 선택 가능 상태를 바꾸면 안 된다.

## 5. 최초 Snapshot과 렌더링

```csharp
StageMapSnapshot map = mapSession.GetSnapshot();
RenderStageMap(map); // View helper 예시
```

최초 Snapshot은 다음 상태다.

- 시작 노드가 `IsStart == true`, `IsCurrent == true`다.
- 시작 노드의 `StageKey`는 `StageKey.Empty`이므로 실제 전투 Stage가 아니다.
- 시작 노드에서 연결된 Depth 1 노드가 `IsSelectable == true`다.
- `IsAtFinalNode == false`다.

### 5-1. StageMapSnapshot

| 필드 | View 사용처 |
|---|---|
| `DepthCount`, `WidthMax` | 전체 레이아웃 크기 |
| `CurrentNodeId` | 현재 위치 표시와 View 캐시 Key |
| `IsAtFinalNode` | Boss 노드 선택 완료 상태 |
| `Nodes` | 노드와 연결선 전체 재조정 |

### 5-2. StageMapNodeSnapshot

| 필드 | View 사용처 |
|---|---|
| `NodeId` | 버튼 클릭 때 Core에 되돌려줄 런 내부 식별자 |
| `StageKey` | 표시 Asset 조회 및 선택 성공 후 실제 Stage Data 조회 |
| `Depth`, `WidthIndex` | 논리 배치 위치 |
| `IsStart`, `IsBoss` | 시작/Boss 전용 외형 |
| `IsCurrent` | 현재 위치 강조 |
| `IsSelectable` | 현재 Snapshot 기준 버튼·외형 표시 |
| `NextNodeIds` | 현재 노드에서 다음 Depth로 향하는 연결선 |

`StageNodeId`는 이번에 생성한 Map 안의 노드 인스턴스 ID이고, `StageKey`는 Unity Data Catalog에서 실제 Stage 종류를 찾는 Key다. 둘을 서로 대체하지 않는다.

연결선은 각 노드의 `NextNodeIds`를 그대로 순회해 그린다. View가 거리로 nearest를 다시 구하거나 양방향 Edge를 추가하지 않는다.

## 6. Query와 Command

| 종류 | API | 상태 변경 | 용도 |
|---|---|---|---|
| Query | `GetSnapshot()` | 없음 | 전체 맵 초기 렌더·명시적 재동기화 |
| Query | `CanSelectNode(nodeId)` | 없음 | 클릭 직전 빠른 UI 확인 |
| Command | `TrySelectNode(nodeId)` | 성공 시 현재 노드 이동 | 선택 확정 및 최신 Snapshot 수신 |

`CanSelectNode`는 호출해도 현재 노드나 선택 가능 목록을 바꾸지 않는다. 다만 true가 이후 Command 성공을 예약하지는 않는다. 실제 선택 권한은 항상 `TrySelectNode`가 호출되는 순간의 Core 상태가 판정한다.

## 7. 선택 처리와 stale Snapshot

```csharp
StageSelectionResult result = mapSession.TrySelectNode(clickedNodeId);

ApplyStageMapSnapshot(result.CurrentMap); // View helper 예시

if (result.Succeeded == false)
{
    ShowStageSelectionFailure(result.FailureReason); // View helper 예시
    return;
}

StageKey selectedStageKey = result.SelectedStageKey;
OpenSelectedStage(selectedStageKey); // Unity Bootstrap helper 예시
```

성공하면 다음이 한 번에 확정된다.

```text
clicked Node가 현재 노드의 Next인지 재검증
→ CurrentNodeId를 clicked Node로 변경
→ 새로운 다음 노드들을 IsSelectable로 계산
→ SelectedStageKey와 변경 후 CurrentMap 반환
```

실패해도 `CurrentMap`은 호출 시점의 최신 Snapshot이다. 먼저 이를 적용해 UI를 실제 상태와 다시 맞춘다. 실패 시 `SelectedStageKey == StageKey.Empty`다.

과거 Snapshot의 `IsSelectable == true`는 선택 권한이 아니다. 예를 들어 최초 Snapshot에 A와 B가 모두 선택 가능해도 A 선택 후 과거 B Snapshot을 클릭하면, B가 새 현재 노드에서 직접 연결되지 않은 경우 `NotConnected`로 거절된다. View는 버튼 표시에는 Snapshot을 쓰되 Command 결과를 최종 판정으로 사용한다.

## 8. 현재 진행 모델: 선택 즉시 다음 경로 활성화

현재 Core에는 Stage clear를 보고하거나 다음 노드를 unlock하는 별도 Command가 없다. 노드 선택 성공 시 `StageMapSession`의 현재 위치가 즉시 선택 노드로 이동하고, 그 노드의 다음 연결들이 내부적으로 선택 가능해진다.

권장 Unity 흐름은 다음과 같다.

```text
맵 화면
1. 최신 StageMapSnapshot 렌더
2. 클릭 후보의 StageKey로 StageConfig/Unit Catalog 구성
3. StageSessionFactory.Create와 Player Reserve 구성을 먼저 완료
4. TrySelectNode로 맵 진행 확정
5. 성공 Result.CurrentMap을 보관하고 준비된 StageSession으로 진입

인게임
6. Stage Map UI는 숨김
7. 전투 진행

맵 복귀
8. 같은 StageMapSession.GetSnapshot 호출
9. 현재 노드와 이미 활성화된 다음 노드 렌더
```

즉, 사용자는 선택한 Stage를 플레이하는 동안 맵을 보지 않고, 전투 뒤 복귀했을 때 다음 경로가 열린 상태를 본다. 현재 구현에서 `IsAtFinalNode`는 Boss 노드를 **선택하여 현재 위치가 됐음**을 뜻한다. Boss clear나 런 성공을 별도로 증명하는 값은 아니다.

Stage 진입 실패나 중도 포기 때 선택을 되돌리는 공개 API도 현재 없다. 따라서 클릭 후보의 `StageSessionFactory.Create`와 필요한 Player Reserve 구성을 선택 전에 완료하고, 성공적으로 준비된 Session이 있을 때만 `TrySelectNode`를 호출한다. clear 기반 잠금, rollback, 저장/불러오기가 필요하면 View 임시 상태가 아니라 별도 Core 계약으로 확장한다.

## 9. Stage Map에서 실제 Stage로 연결

아래 코드는 Core API가 아니라 Unity Bootstrap의 책임 구조를 보여 주는 예시다. `UnityStageData`, `FindNode`, `ResolveStageData`, `BuildStageConfig`, `BuildUnitCatalog`, `PopulatePlayerReserve`, `EnterStageScene`은 모두 **Unity 프로젝트 측 타입/helper 예시**다.

중요한 순서는 실제 `StageSessionFactory.Create`까지 먼저 성공시키고 그다음 Map 선택을 확정하는 것이다. 별도 View 검증식으로 초기 적 배치나 Railway 규칙을 복제하지 않는다.

```csharp
void OnStageNodeClicked(StageNodeId nodeId)
{
    StageMapNodeSnapshot candidate = FindNode(_latestMap, nodeId); // View helper 예시
    if (candidate == null || _stageMapSession.CanSelectNode(nodeId) == false)
        return;

    UnityStageData data = ResolveStageData(candidate.StageKey);
    StageConfig stageConfig = BuildStageConfig(data);
    UnitSpec[] unitCatalog = BuildUnitCatalog(data);

    StageSession preparedStageSession = StageSessionFactory.Create(
        stageConfig,
        unitCatalog);
    PopulatePlayerReserve(preparedStageSession); // Unity Bootstrap helper 예시

    StageSelectionResult selection = _stageMapSession.TrySelectNode(nodeId);
    ApplyStageMapSnapshot(selection.CurrentMap); // View helper 예시

    if (selection.Succeeded == false)
    {
        ShowStageSelectionFailure(selection.FailureReason); // View helper 예시
        // preparedStageSession은 사용하지 않고 폐기한다.
        return;
    }

    if (selection.SelectedStageKey != candidate.StageKey)
        throw new InvalidOperationException("선택 결과와 준비한 Stage가 다릅니다.");

    EnterStageScene(preparedStageSession); // Unity Bootstrap helper 예시
}
```

`CanSelectNode`는 불필요한 Stage 준비를 줄이는 Query일 뿐 선택 예약이 아니다. 준비 중 상태가 달라졌거나 stale 입력이면 최종 `TrySelectNode`가 실패할 수 있으며, 이때 준비한 Session은 폐기하고 `selection.CurrentMap`으로 화면을 맞춘다. 반대로 팩토리가 초기 적 겹침·배치 구역 위반·누락 Unit Key·Railway의 빈 Rail Cell 등을 거절하면 Map 선택은 아직 일어나지 않았다.

`BuildStageConfig(data)`는 최소한 다음 데이터를 포함한다.

```text
EnemyEncounterData
→ InitialEnemyPlacementSpec[]

Stage 공간 데이터
→ CellCoord[] RailCells

Stage 기믹 데이터
→ StageGimmickSpec[]

공통 Stage 설정 + 위 세 배열
→ StageConfig
```

초기 Enemy와 Railway 구성 및 `AdvanceTimeline()` 연결은 [StageSession View 통합 인계 가이드](unit-movement-attack-core-handoff.md)의 4~6절을 따른다.

## 10. 실패 사유

| `StageSelectionFailureReason` | 의미 | View 처리 |
|---|---|---|
| `NodeNotFound` | 현재 맵에 없는 `StageNodeId` | 최신 `CurrentMap` 적용, 입력/바인딩 오류 로그 |
| `NotConnected` | 노드는 있지만 현재 노드의 다음 Edge가 아님 | 최신 `CurrentMap` 적용, 버튼 상태 갱신 |
| `FinalNodeReached` | 현재 노드가 이미 Boss라 더 진행할 수 없음 | 최신 `CurrentMap` 적용, 추가 노드 선택 차단 |

`FinalNodeReached`는 Boss clear나 런 성공 결과가 아니다. Boss 전투 완료와 런 종료 UI는 Stage 전투 결과를 소유하는 별도 Unity/Core 흐름에서 판정한다.

생성 시 잘못된 `DepthCount`, `WidthMax`, 확률, 빈 Stage Key 목록, 빈 Boss Key는 플레이 입력 실패 Result가 아니라 Bootstrap/Data 오류 예외다.

## 11. Snapshot 보관과 호출 빈도

- `StageMapSnapshot`과 내부 배열은 반환 시 복사되며 이후 Command로 과거 Snapshot이 변하지 않는다.
- 화면 진입, 선택 Result 수신, 맵 복귀 때 전체 Snapshot을 적용한다.
- `TrySelectNode`가 반환한 `CurrentMap` 뒤에 즉시 `GetSnapshot()`을 중복 호출하지 않는다.
- `CanSelectNode`를 매 프레임 반복 호출하지 않는다. 클릭 가능 외형은 최신 Snapshot의 `IsSelectable`로 그리고, 클릭 순간 필요할 때만 Query 또는 Command를 호출한다.
- Presenter/GameObject 연결은 배열 index가 아니라 `StageNodeId`를 기준으로 한다.
- Session 자체는 메모리 상태다. 현재 공개 계약에는 직렬화된 Map Snapshot을 다시 Session으로 복원하는 API가 없다.

## 12. View 구현 완료 체크리스트

### Assembly와 수명

- [ ] View asmdef는 `ProjectL.Core.Application`, `ProjectL.Core.Contracts`만 참조한다.
- [ ] View 코드에 `ProjectL.Core.Domain` 참조가 없다.
- [ ] 한 런 동안 같은 `StageMapSession`을 유지한다.
- [ ] 전투마다 선택된 `StageKey`로 별도 `StageSession`을 만든다.
- [ ] `StageMapSession`에 `StageConfig`나 전투 상태를 넣으려 하지 않는다.

### 생성과 렌더링

- [ ] seed와 Stage Key 목록을 재현 가능하게 관리한다.
- [ ] `StageNodeId`와 `StageKey`를 구분한다.
- [ ] 노드는 `Depth`, `WidthIndex`, 연결선은 `NextNodeIds`로 그린다.
- [ ] nearest, second-nearest, incoming 보정, Boss 수렴을 View에서 다시 계산하지 않는다.
- [ ] `IsStart`, `IsBoss`, `IsCurrent`, `IsSelectable`을 Snapshot 그대로 표시한다.

### 선택과 전환

- [ ] `CanSelectNode`를 상태 변경 없는 Query로 취급한다.
- [ ] Snapshot의 `IsSelectable`이나 이전 `CanSelectNode` 결과를 Command 허가증으로 취급하지 않는다.
- [ ] `TrySelectNode` 성공/실패 모두 `CurrentMap`을 적용한다.
- [ ] 클릭 후보의 `StageKey`로 실제 `StageSession`을 먼저 준비하고, 선택 성공 뒤 `SelectedStageKey`가 후보와 일치하는지 확인한다.
- [ ] 선택 성공 직후 Core 내부에서는 다음 노드가 활성화된다는 현재 모델을 반영한다.
- [ ] 맵 복귀 시 같은 Session의 최신 Snapshot을 다시 렌더한다.
- [ ] `IsAtFinalNode`를 Boss clear 완료로 오해하지 않는다.

### Stage Bootstrap 연결

- [ ] `SelectedStageKey`에서 실제 Stage Data를 찾는 Catalog가 있다.
- [ ] `StageSessionFactory.Create`와 필요한 Player Reserve 구성을 `TrySelectNode`보다 먼저 완료한다.
- [ ] `InitialEnemyPlacementSpec[]`, `RailCells`, `StageGimmickSpec[]`를 `StageConfig`에 포함한다.
- [ ] 초기 Enemy를 View가 직접 생성·배치하지 않는다.
- [ ] Railway 일정이나 처치 대상을 View에서 계산하지 않는다.
- [ ] 팩토리/구성 실패 시 Map 선택이 일어나지 않고, 선택 실패 시 준비한 StageSession을 폐기한다.

이 체크리스트와 연결된 StageSession 가이드를 함께 만족하면 Stage Map 선택부터 초기 Enemy와 Railway Timeline이 있는 실제 Stage 진입까지 Core 내부 규칙 없이 View에서 연결할 수 있다.
