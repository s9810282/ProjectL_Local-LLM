# StageSession View 인계 가이드

> Current Framework/Buffer integration (2026-10-04): combat movement uses the exported View group service and existing input/result Buffers. See [current drag-path integration](unit-movement-attack-core-handoff.md#8-current-drag-path-movement-integration) for exact route/Return, Attach and complete-result acknowledgement contracts, and the [verification record](../handoffs/2026-10-04-drag-path-movement-verification.md) for production gates. The StageSession examples retained below are legacy reference; they do not authorize a second runtime state owner or endpoint movement path.

이 문서는 Unity View 작업자가 Core의 내부 구조를 몰라도 Stage 생성과 Unit 배치를 연결할 수 있도록 만든 사용 안내서다.

가장 중요한 규칙은 하나다.

> Unity View는 `ProjectL.Core.Application`과 `ProjectL.Core.Contracts`만 사용한다. `ProjectL.Core.Domain`은 직접 참조하지 않는다.

현재 [ProjectL.Unity.asmdef](../../../Assets/Scripts/ProjectL/Unity/ProjectL.Unity.asmdef)는 이미 Application과 Contracts를 참조한다. Domain 참조는 추가하지 않는다.

## 1. 어떤 스크립트를 사용하면 되는가

| 스크립트 | 하는 일 | View에서 사용하는 시점 |
| --- | --- | --- |
| `StageConfig` | 맵 크기와 Reserve 슬롯 수를 전달한다. | Stage를 만들기 전 |
| `UnitSpec` | ScriptableObject의 Unit 설정을 Core가 이해할 수 있는 값으로 전달한다. | Stage를 만들기 전 |
| `StageSessionFactory` | Map, Reserve, Phase, 배치 Service 등을 한 번에 조립해서 `StageSession`을 만든다. | Stage 시작 시 한 번 |
| `StageSession` | Unit 생성, 배치 Preview, Combat 배치, Reserve 복귀, 현재 Snapshot 조회를 처리한다. | Stage 진행 중 계속 |
| `EntitySnapshot` | Entity 하나를 그리는 데 필요한 현재 상태를 제공한다. | Unit View 생성·갱신 시 |
| `StageSnapshot` | Map 크기, 전체 Entity, Reserve 슬롯, 현재 Phase를 한 번에 제공한다. | 초기 화면 및 Drop 후 동기화 시 |
| `CombatPlacementPreview` | 드래그 중 표시할 셀과 배치 가능 여부를 제공한다. | Drag 중 |
| `DeploymentResult` | Drop 성공 여부와 처리 후의 전체 Stage 상태를 제공한다. | Combat 또는 Reserve Drop 후 |

View가 주로 호출할 입구는 `StageSessionFactory`와 `StageSession` 두 개다. 나머지 Contracts 타입은 입력값 또는 반환값으로 사용한다.

필요한 namespace는 다음과 같다.

```csharp
using ProjectL.Core.Application.StageRun;
using ProjectL.Core.Contracts.Commands;
using ProjectL.Core.Contracts.Common;
using ProjectL.Core.Contracts.Ids;
using ProjectL.Core.Contracts.Results;
using ProjectL.Core.Contracts.Snapshots;
```

`StageSession` 메서드는 다음 기준으로 선택한다.

| 메서드 | 사용 시점 |
| --- | --- |
| `CreateReserveUnit` | Reserve 슬롯에 새 Unit을 만들 때 |
| `GetSnapshot` | 초기 화면처럼 Stage 전체 상태가 필요할 때 |
| `TryGetEntitySnapshot` | 특정 `EntityId` 하나의 최신 상태만 다시 읽을 때 |
| `PreviewCombatPlacement` | Combat Grid 위에서 Drag 중일 때 |
| `TryPlaceOnCombat` | Combat Grid에 Drop할 때 |
| `TryReturnToReserve` | Combat의 Entity를 Reserve 슬롯에 Drop할 때 |

## 2. Stage 생성

Stage를 시작할 때 다음 순서로 진행한다.

```text
1. Unity Data를 StageConfig로 변환
2. Unit ScriptableObject 목록을 UnitSpec 목록으로 변환
3. StageSessionFactory.Create 호출
4. 생성된 StageSession을 Stage 진행 동안 보관
```

예시는 다음과 같다.

```csharp
StageConfig config = new StageConfig(
    mapWidth: 5,
    mapHeight: 5,
    reserveCapacity: 4);

UnitSpec[] unitCatalog =
{
    new UnitSpec(
        EntityKey.Unit_Akimbo,
        footprintWidth: 2,
        footprintHeight: 1,
        footprintLocalCells: new[]
        {
            new CellCoord(0, 0),
            new CellCoord(1, 0)
        },
        maxHealth: 10,
        moveRange: 3,
        attackRange: 2,
        attackPower: 10,
        actionPoint: 5)
};

StageSession session = StageSessionFactory.Create(config, unitCatalog);
```

실제 Unity 코드에서는 하드코딩한 값 대신 ScriptableObject 값을 읽어 `StageConfig`와 `UnitSpec`을 만든다.

`StageSession`은 한 Stage의 상태 전체를 보관한다. Stage가 진행되는 동안 Bootstrap이나 Stage Controller가 같은 인스턴스를 가지고 있어야 한다. 전역 Singleton으로 만들 필요는 없다.

## 3. Reserve Unit 생성

Unit을 Reserve 슬롯에 만들 때는 `StageSession.CreateReserveUnit`을 사용한다.

```csharp
EntitySnapshot unit = session.CreateReserveUnit(
    EntityKey.Unit_Akimbo,
    Owner.Player,
    reserveSlotIndex: 0);
```

호출 흐름은 다음과 같다.

```text
1. EntityKey, Owner, Reserve 슬롯을 전달
2. 반환된 EntitySnapshot에서 EntityId 확보
3. EntityId ↔ View GameObject 또는 Presenter 연결 저장
4. 프리팹 종류는 EntityKey로 판단
```

`EntityId`와 `EntityKey`는 용도가 다르다.

- `EntityId`: 이번 Stage에 실제로 생성된 Entity 하나를 찾는 번호
- `EntityKey`: 어떤 종류의 Unit인지 나타내는 값

따라서 View는 `unit.Id`를 GameObject 또는 Presenter와 연결해서 보관하고, 프리팹이나 이미지는 `unit.Key`로 선택한다. `EntityId`가 1이라는 이유로 특정 Unit 프리팹을 선택하면 안 된다.

## 4. 초기 화면 동기화

Stage와 Reserve Unit을 준비한 뒤 전체 상태를 한 번 받는다.

```csharp
StageSnapshot stage = session.GetSnapshot();
```

View는 다음 값을 사용한다.

- `stage.MapWidth`, `stage.MapHeight`: Grid 생성
- `stage.Entities`: Unit View 생성 또는 현재 위치·체력 갱신
- `stage.ReserveSlots`: Reserve UI의 빈 슬롯과 점유 슬롯 갱신
- `stage.CurrentPhase`: 배치 입력과 관련 UI의 활성화 여부 표시

`Entities`는 다음처럼 순서대로 읽을 수 있다.

```csharp
for (int i = 0; i < stage.Entities.Length; i++)
{
    EntitySnapshot entity = stage.Entities.Span[i];
    // entity.Id로 기존 View를 찾고, entity.Key와 위치 정보로 화면을 갱신한다.
}
```

View가 `CurrentPhase`를 보고 Combat 단계에서 배치 버튼이나 Drag를 막는 것은 괜찮다. 다만 이것은 화면 조작을 막는 용도다. 최종 규칙 판정은 계속 Core가 담당하므로 Session과 Domain의 Phase 검증을 제거하거나 View 판단으로 대신하면 안 된다.

## 5. EntitySnapshot에서 위치 읽기

`EntitySnapshot.Area`를 먼저 확인한다.

- `EntityArea.Reserve`: `ReserveSlotIndex`를 사용한다.
- `EntityArea.Combat`: `CombatAnchor`, `CombatRotation`, `OccupiedCells`를 사용한다.

`OccupiedCells`는 Core가 회전된 footprint까지 계산한 실제 맵 셀이다. View가 footprint 회전 공식을 다시 만들 필요가 없다.

체력 UI는 `MaxHealth`, `CurrentHealth`, `IsDead`를 사용한다. Unit 전용 수치는 `UnitStats`가 있을 때만 사용한다.

## 6. Drag Preview

드래그 중에는 전체 `StageSnapshot`을 매번 요청하지 않는다. `PreviewCombatPlacement`만 호출한다.

```csharp
CombatPlacementPreview preview =
    session.PreviewCombatPlacement(entityId, anchor, rotation);
```

View가 할 일은 다음 네 가지다.

- `preview.OccupiedCells`에 해당하는 셀을 Highlight한다.
- `preview.CanPlace`가 `true`면 가능한 색, `false`면 불가능한 색으로 표시한다.
- 드래그 중인 Ghost 위치만 바꾼다.
- 실제 Entity View의 확정 위치는 바꾸지 않는다.

Highlight할 셀은 다음처럼 읽는다.

```csharp
for (int i = 0; i < preview.OccupiedCells.Length; i++)
{
    CellCoord cell = preview.OccupiedCells.Span[i];
    // cell 위치의 Highlight만 갱신한다.
}
```

Preview는 화면 표현을 위한 예상 결과다. `CanPlace == true`여도 Drop 명령의 성공을 미리 확정하면 안 된다.

## 7. Combat 영역에 Drop

Combat Grid에 놓을 때는 다음 메서드를 사용한다.

```csharp
DeploymentResult result =
    session.TryPlaceOnCombat(entityId, anchor, rotation);
```

`result.Succeeded`는 Drop 성공 여부다. 성공과 실패 모두 `result.CurrentStage`에는 명령 처리 후 Core의 전체 상태가 들어 있다.

## 8. Reserve 영역에 Drop

Reserve 슬롯으로 되돌릴 때는 다음 메서드를 사용한다.

```csharp
DeploymentResult result =
    session.TryReturnToReserve(entityId, slotIndex);
```

Combat Drop과 마찬가지로 성공 여부만 보고 View 위치를 임의로 결정하지 않는다.

## 9. Drop 후 화면 동기화

Combat Drop과 Reserve Drop 모두 `result.CurrentStage`를 화면의 기준으로 삼는다.

```csharp
StageSnapshot currentStage = result.CurrentStage;
```

- 성공하면 Ghost와 Highlight를 제거하고 `CurrentStage`대로 Entity와 Reserve UI를 갱신한다.
- 실패해도 Ghost와 Highlight를 제거하고 `CurrentStage`대로 원래 화면을 복구한다.

이 방식이면 View가 예상한 위치와 실제 Core 상태가 달라지는 문제를 줄일 수 있다.

## 10. View에서 하지 말아야 할 것

- `CombatEntityState`를 저장하지 않는다.
- `StageMapState`를 직접 읽거나 변경하지 않는다.
- `ReserveSlotState`를 직접 변경하지 않는다.
- `EntityDeploymentService`를 직접 생성하지 않는다.
- `UnitFactory`를 직접 생성하지 않는다.
- `EntityId` 숫자로 Unit 종류를 추측하지 않는다.
- Preview 결과만 보고 확정 상태를 바꾸지 않는다.
- `ProjectL.Unity.asmdef`에 Domain 참조를 추가하지 않는다.

Map, Reserve, Entity 위치를 직접 바꾸면 서로 다른 상태가 될 수 있다. 모든 생성과 배치 변경은 `StageSession`을 통해 요청한다.

## 11. 오류를 처리하는 기준

배치할 수 없는 셀, 점유된 Reserve 슬롯, 존재하지 않는 Entity 같은 정상적인 게임 실패는 `DeploymentResult.Succeeded == false`로 처리한다. 실패는 오류창을 띄워야 하는 예외라기보다 View를 원래 상태로 되돌려야 하는 결과다.

잘못된 Map 크기, 잘못된 Unit 설정, 범위를 벗어난 슬롯 번호처럼 개발 또는 설정이 잘못된 경우에는 예외가 발생할 수 있다. 이런 값은 Unity Data를 Contracts로 변환하는 Bootstrap 단계에서 확인한다.

## 12. 전체 호출 순서 요약

```text
Unity Data / ScriptableObject
→ StageConfig / UnitSpec 변환
→ StageSessionFactory.Create
→ StageSession 보관
→ CreateReserveUnit
→ GetSnapshot으로 초기 화면 생성
→ PreviewCombatPlacement로 Drag 표시
→ TryPlaceOnCombat 또는 TryReturnToReserve
→ DeploymentResult.CurrentStage로 화면 전체 동기화
```

View는 입력과 화면 표현을 담당하고, 최종 상태와 규칙 판정은 Core가 담당한다.
