# ProjectL StageSession View 통합 인계 가이드

> Current integration contract (2026-10-04): the registered Framework/Buffer route below supersedes the legacy StageSession examples in this guide for production combat movement. Do not reconnect endpoint movement, geometric checkpoint billing, or a movement confirmation button. See the [confirmed drag-path design](../specs/2026-10-04-drag-path-movement-design.md) and [verification/dependency record](../handoffs/2026-10-04-drag-path-movement-verification.md). The other StageSession examples are retained legacy reference, not evidence that those APIs are registered runtime owners.

이 문서는 현재 Core 공개 API를 기준으로 Unity View가 Stage 생성, 초기 적 배치, Railway 기믹이 포함된 Timeline, Unit 턴, 이동, 공격, Combat Trait를 연결하는 방법을 설명한다. 변경 이력이 아니라 **현재 동작만 설명하는 인계 문서**다.

Stage Map 생성·렌더링·노드 선택은 별도 [Stage Map View Handoff](stage-map-view-handoff.md)를 따른다. 두 흐름은 Bootstrap의 `StageSelectionResult.SelectedStageKey → StageConfig → StageSessionFactory.Create(...)`에서만 연결한다.

> View는 `ProjectL.Core.Application`과 `ProjectL.Core.Contracts`만 사용한다. `ProjectL.Core.Domain`을 참조하거나 Core 규칙을 View에서 다시 계산하지 않는다.

문서의 Core 호출 예제는 `StageSessionFactory`, `StageSession`, `StageSession.Combat`, Contracts의 Command/Result/Snapshot만 사용한다. 예제에 등장하는 `ApplySnapshot`, `RenderTraitActivation` 같은 메서드는 반드시 **View helper 예시**라고 표시한다.

## Stage Outcome 및 Night Cycle View 연동

이 절은 현재 Core 공개 계약과 EditMode View Boundary/통합 회귀에서 확인된 동작을 설명한다. View는 아래 API와 Snapshot을 확정 계약으로 사용할 수 있다.

### 1. View Assembly 경계

View는 `ProjectL.Core.Application`과 `ProjectL.Core.Contracts`의 공개 타입만 사용한다. `ProjectL.Core.Domain`이나 Application의 `internal` 타입을 참조하지 않는다. Timeline, Night, Outcome 규칙을 View에서 다시 계산하지 않고 `StageSession` 명령 결과의 `StageSnapshot`을 렌더링한다.

View Boundary asmdef는 `ProjectL.Core.Contracts`와 `ProjectL.Core.Application`만 참조한다. `StageOutcomeSnapshot`은 `ProjectL.Core.Contracts`, 나머지 Stage/Timeline Snapshot은 `ProjectL.Core.Contracts.Snapshots`, Command와 Result는 각각 `ProjectL.Core.Contracts.Commands`, `ProjectL.Core.Contracts.Results`에 있다.

### 2. Stage 생성 입력

Stage 생성 시 Unit Catalog와 Structure Catalog를 함께 넘긴다. Enemy HQ의 최초 위치는 `StageConfig`의 `initialEnemyStructurePlacements`에 포함한다.

```csharp
var headquartersKey = EntityKey.Structure_Headquarters;

var structureCatalog = new[]
{
    new StructureSpec(
        headquartersKey,
        footprintWidth: 1,
        footprintHeight: 1,
        footprintLocalCells: new[] { new CellCoord(0, 0) },
        maxHealth: 100,
        isAttackable: true,
        attackPower: 1),
};

var enemyHeadquarters = new[]
{
    new InitialEnemyStructurePlacementSpec(
        headquartersKey,
        new CellCoord(9, 4),
        Rotation4.R0),
};

var config = new StageConfig(
    mapWidth: 10,
    mapHeight: 8,
    reserveCapacity: 8,
    playerDeploymentDepth: 3,
    enemyDeploymentDepth: 3,
    timelineSeed: 1234,
    initialEnemyPlacements: Array.Empty<InitialEnemyPlacementSpec>(),
    railCells: Array.Empty<CellCoord>(),
    stageGimmicks: Array.Empty<StageGimmickSpec>(),
    nightLength: 120L,
    initialEnemyStructurePlacements: enemyHeadquarters);

StageSession session = StageSessionFactory.Create(
    config,
    unitCatalog,
    structureCatalog);
```

위 숫자는 호출 형태를 설명하기 위한 예시일 뿐 밸런스 값이 아니다. 실제 조립 계층은 Stage 데이터에서 Catalog와 배치 정보를 읽어 전달한다.

### 3. 최초 Player HQ 배치와 Combat 진입

Player HQ 배치는 Preview와 Commit을 분리한다.

```csharp
CombatPlacementPreview preview =
    session.PreviewInitialPlayerHeadquartersPlacement(anchor, rotation);

if (preview.CanPlace)
{
    HeadquartersPlacementResult placement =
        session.TryPlaceInitialPlayerHeadquarters(anchor, rotation);

    ApplySnapshot(placement.CurrentStage); // View helper 예시
}
```

Combat 진입은 결과 객체를 반환하는 `TryEnterCombat()`을 사용한다.

```csharp
CombatEntryResult result = session.TryEnterCombat();

ApplySnapshot(result.CurrentStage); // View helper 예시

if (result.Succeeded == false)
{
    ShowCombatEntryFailure(result.FailureReason); // View helper 예시
    return;
}
```

의도된 성공 조건은 다음과 같다.

- Phase가 Deployment이고 Stage가 종료되지 않았다.
- 살아 있으며 Combat에 배치된 Player HQ와 Enemy HQ가 각각 정확히 하나다.
- Combat 후보 Unit의 정적 Timeline 설정이 유효하다.
- Player 전투 Unit이 0명이어도 Combat 진입은 허용한다.
- 최초 Combat 진입 성공 뒤 Player HQ 배치는 잠긴다.

View는 이 조건을 재구현해 최종 성공 여부를 정하지 않는다. 사전 표시는 Preview/Snapshot으로 만들 수 있지만 권위 있는 판정은 Core 결과다.

### 4. Night와 Outcome 표시

Night 진행률은 `StageSnapshot.Night`만 사용한다.

```csharp
void RenderNight(StageSnapshot stage) // View helper 예시
{
    long length = stage.Night.NightLength;
    long current = stage.Night.CurrentNightTime;
    long remaining = stage.Night.RemainingNightTime;

    // 마지막 Unit Delay 때문에 current가 length를 넘을 수 있다.
    // 게이지 표현에만 필요한 clamp는 View에서 수행한다.
}
```

- `CurrentNightTime`은 Unit 턴이 끝날 때 그 Unit의 원래 Delay만큼 증가한다.
- 경계를 넘긴 마지막 턴 때문에 `CurrentNightTime`은 `NightLength`보다 클 수 있다.
- `RemainingNightTime`은 0 아래로 내려가지 않는다.
- Timeline 정렬 시간이나 예상 턴 수로 Night 진행도를 다시 계산하지 않는다.

Stage 종료 화면과 입력 잠금은 `StageSnapshot.Outcome`을 사용한다.

| Outcome | Reason |
|---|---|
| `InProgress` | `None` |
| `Victory` | `EnemyHeadquartersDestroyed` |
| `Defeat` | `PlayerHeadquartersDestroyed` |
| `Defeat` | `NoPlayerUnitsRemaining` |

종료 뒤에도 `GetSnapshot()`, Preview, `TryGetEntitySnapshot()` 같은 Query는 허용된다. 최종 전장 화면과 연출은 계속 Snapshot을 읽을 수 있다.

### 5. Timeline의 Night 종료 예측

Timeline 공개 DTO는 다음 정보를 제공한다.

- `CombatTimelineEntryKind.NightEnd`
- Unit 턴의 `ProjectedNightTimeAfterTurn`
- Unit 턴의 `EndsNightAfterTurn`

최종 View 렌더링 형태는 다음과 같다.

```csharp
foreach (CombatTimelineEntrySnapshot entry in timeline.Entries)
{
    if (entry.Kind == CombatTimelineEntryKind.UnitTurn)
    {
        RenderUnitTurn(
            entry,
            entry.ProjectedNightTimeAfterTurn,
            entry.EndsNightAfterTurn); // View helper 예시
    }
    else if (entry.Kind == CombatTimelineEntryKind.NightEnd)
    {
        RenderNightEndMarker(entry); // View helper 예시
    }
}
```

- 경계를 넘기는 Unit 턴은 `EndsNightAfterTurn == true`다.
- 표시 공간이 있으면 그 직후 `NightEnd` 마커가 온다.
- 최대 항목 수가 1이라 마커가 생략되어도 Unit 턴의 종료 플래그는 `true`다.
- `NightEnd` 뒤에는 Unit 턴이나 Stage Gimmick을 더 표시하지 않는다.
- View는 Delay로 경계를 자체 계산하거나 가짜 `NightEnd`를 만들지 않는다.

### 6. Night 종료 후 Entity 반영

턴 종료가 Night 경계를 넘으면 Core가 한 명령 안에서 Night 종료 전이를 수행한다. View는 `EndUnitTurn` 성공 결과의 최신 `CurrentStage`를 먼저 적용한 뒤 다음 입력 상태를 판단한다.

- Phase는 Deployment로 돌아간다.
- 살아 있는 Player/Enemy Unit은 Reserve로 회수된다.
- 죽은 Unit은 회수하지 않는다.
- HQ를 포함한 Structure는 Combat에 남는다.
- 낮 동안 `CurrentNightTime`은 마지막 턴이 도달한 값으로 유지된다.
- 다음 `TryEnterCombat()` 성공 시 Night 시간이 0으로 초기화된다.
- Enemy Unit은 다음 Night에 설정된 최초 위치로 자동 복귀한다.
- Player Unit은 Player가 새로 배치한 위치를 사용한다.

Entity View는 `EntityId`로 기존 객체와 새 Snapshot을 대조한다. Reserve로 이동한 Unit은 전장에서 숨기되 Structure까지 제거하지 않는다. 생산 예약/생산 중 Unit은 자동 회수 대상이 아니다.

### 7. Enemy AI의 Player HQ fallback

- Combat에 살아 있는 Player Unit이 하나라도 있으면 Unit만 표적으로 삼는다.
- Combat에 살아 있는 Player Unit이 없으면 살아 있고 Combat에 있는 Player HQ만 fallback 표적으로 삼는다.
- Reserve의 Player Unit은 Combat 표적으로 계산하지 않는다.
- 일반 Structure는 HQ fallback 표적이 아니다.
- 공격 도중 Stage가 끝나면 AI 연쇄 처리에서 추가 `EndTurn`을 호출하지 않는다.

View는 거리 등을 근거로 AI 표적을 교체하지 않고 AI 결과와 최신 Snapshot만 반영한다.

### 8. Stage 종료 후 명령 처리

Stage가 끝난 뒤 변경 명령은 상태를 바꾸지 않고 실패해야 한다.

| 명령 경계 | 예상 실패 표현 |
|---|---|
| `CreatePlayerReserveUnit()` | `InvalidOperationException`과 변경 없는 Stage 상태 |
| `TryPlaceInitialPlayerHeadquarters()` | `HeadquartersPlacementFailureReason.StageEnded` |
| `TryEnterCombat()` | `CombatEntryFailureReason.StageEnded` |
| `TryPlaceOnCombat()`, `TryReturnToReserve()` 등 `DeploymentResult` 경계 | `Succeeded == false`와 변경 없는 `CurrentStage` |
| 이동, 공격, 턴 종료, AI 명령 | 각 공개 Result의 `StageEnded` 실패와 변경 없는 Snapshot |

View는 `Outcome.IsStageEnded`로 입력을 미리 비활성화할 수 있다. 이미 큐에 들어간 입력이 있을 수 있으므로 명령 결과의 성공 여부도 확인한다. 최종 계약에서는 `PlacementLocked`나 `WrongPhase`보다 `StageEnded`가 우선한다.

### 9. 권장 View 처리 흐름

```text
StageSession 명령 호출
  → Result.Succeeded 확인
  → 성공/실패와 관계없이 Result.CurrentStage 반영
  → CurrentStage.Outcome.IsStageEnded 확인
  → 종료라면 입력 차단과 결과 연출
  → 종료가 아니면 CurrentPhase와 Timeline에 맞는 다음 입력 활성화
```

Snapshot을 먼저 반영하면 Night 종료 Unit 회수, Phase 전환, HQ 상태, 승패 결과를 서로 다른 시점의 데이터로 섞는 일을 피할 수 있다.

### 10. 후속 범위

다음 항목은 원본 Plan에서도 후속 시스템으로 미룬다.

- 승리 보상 지급
- `StageMap` 완료 상태 기록
- 다음 Stage 해금 및 저장

현재 View는 Outcome을 표시할 수 있지만 보상이나 진행도 저장까지 완료되었다고 가정해서는 안 된다.

### 11. 검증된 경계

- View Boundary는 Domain 참조 없이 3-인자 Factory, Player HQ Preview/Commit, `TryEnterCombat()`, Night/Outcome/Timeline Snapshot을 사용한다.
- Night 종료 전후 Entity는 `EntityId`로 대조하며 Unit 회수와 Structure 유지를 확인했다.
- Timeline 최대 항목 수가 1이어도 종료 Unit의 `EndsNightAfterTurn`은 유지된다.
- 종료 후 Query는 가능하고 변경 Command는 `StageEnded` 또는 `Succeeded == false`로 상태 변경 없이 거부된다.
- 보상 지급과 StageMap 완료 연동은 후속 범위다.

## 1. 문서 기준과 책임 경계

이 문서는 다음 우선순위로 현재 동작을 정리했다.

1. `Assets/Scripts/ProjectL/Core`의 현재 프로덕션 코드
2. `Assets/Tests/EditMode`의 현재 테스트
3. 최신 Trait 설계·구현 계획
4. 이 문서의 이전 내용

현재 `ProjectL.Unity.asmdef`가 참조하는 Core Assembly는 다음 둘이다.

```text
ProjectL.Core.Contracts
ProjectL.Core.Application
```

View 코드에서 주로 필요한 namespace는 다음과 같다.

```csharp
using System;
using ProjectL.Core.Application.StageRun;
using ProjectL.Core.Contracts.Commands;
using ProjectL.Core.Contracts.Common;
using ProjectL.Core.Contracts.Ids;
using ProjectL.Core.Contracts.Results;
using ProjectL.Core.Contracts.Snapshots;
```

역할은 다음처럼 나눈다.

| 계층 | 책임 |
|---|---|
| Unity Data/Bootstrap | 선택한 `StageKey`를 조회하고 ScriptableObject 값을 초기 적·Rail·기믹을 포함한 `StageConfig`, `UnitSpec`, `TraitSpec`으로 변환 |
| Core | 초기 적 배치, 배치 가능 여부, 이동 거리·비용, 공격 범위·방향·피해, Trait 상태 전이, Unit/기믹 Timeline 순서와 상태 변경 |
| View | 입력 전달, Highlight, 애니메이션, 사운드, 로컬라이징, 반환 Snapshot으로 화면 동기화 |

View가 하면 안 되는 계산은 특히 다음과 같다.

- footprint 회전·점유 가능 여부
- 이동 거리, 남은 이동 거리, 이동 행동력 비용
- 공격 범위, 방향, 영향 셀, 적대 대상 판정
- Focus 보너스 피해와 다음 스택 예약 조건
- 이동 후 공격 할인 비용과 소비·만료 조건
- 다음 행동 Unit과 Timeline 시간·정렬
- Railway 발동 시점, 영향 Rail Cell, 처치 대상
- 사망 대상의 Map 점유 및 Timeline 제거

UI를 미리 비활성화하는 것은 가능하지만 최종 판정은 항상 Core Result를 따른다.

## 2. 현재 지원 범위와 명시적 제한

### 2-1. 현재 지원하는 기능

- 이동은 `MoveMethod.LiftAndPlace`만 지원한다.
- 이동 거리는 Manhattan 거리이며, 비용은 `거리 × MoveCost`다.
- 이동과 공격은 같은 `CurrentActionCost`를 소비한다.
- Radial 공격은 `AppointMethod.Radial + AttackTargetMode.SingleTarget` 조합이다.
- 방향성 공격은 `AppointMethod.DirectionalPattern + AttackTargetMode.AllHostilesInArea` 조합이다.
- DirectionalPattern 공격자는 `(0,0)` 한 칸을 점유하는 1×1 footprint여야 한다.
- Player와 Enemy만 서로 적대적이다. 같은 Owner끼리는 공격할 수 없고 Neutral은 누구와도 적대적이지 않다.
- 현재 등록된 Combat Trait는 `Focus`, `MoveAttackDiscount` 두 가지다.
- 두 Trait가 없는 Unit도 기존 이동·공격·Timeline 동작을 유지한다.
- `StageConfig.InitialEnemyPlacements`의 적은 `StageSessionFactory.Create` 중 Enemy 진영에 즉시 배치된다.
- Timeline Forecast는 Unit과 Stage Gimmick을 함께 반환한다.
- 현재 Stage Gimmick은 `Railway`를 지원한다. 완료된 Unit 턴 수를 기준으로 발동하며 모든 Rail Cell 위의 살아 있는 배치 Unit을 제거한다.

### 2-2. 현재 공개 API의 제한

- View가 호출하는 Unit 생성 API는 Player용 `CreatePlayerReserveUnit`이다. Enemy Unit은 `StageConfig.InitialEnemyPlacements`, Enemy Structure는 `InitialEnemyStructurePlacements`와 Structure Catalog로 초기화한다. Player HQ는 `TryPlaceInitialPlayerHeadquarters`가 최초 생성과 전투 전 재배치를 담당하며, 임의 Structure 생성 Command는 없다.
- `TryEnterCombat`은 현재 배치 Unit과 재배치 예정 Enemy Unit으로 Timeline을 초기화한다. Player Reserve Unit은 자동 배치하거나 참가시키지 않는다.
- Timeline의 행동 Unit은 Unit만 가능하다. Structure/Headquarters는 참가하지 않지만 Stage Gimmick은 별도 Timeline Entry로 함께 예고된다.
- View가 다음 Unit이나 기믹을 지정하는 API는 없다. `AdvanceTimeline()`이 지금 처리할 Unit 시작 또는 기믹 발동을 선택한다.
- 현재 공개 Snapshot은 Rail Cell 목록 자체를 제공하지 않는다. Rail 바닥 표시는 `StageConfig`를 만든 Unity Stage Data에서 구성하고 발동 연출은 Result의 `AffectedRailCells`를 사용한다.
- Combat에서 Deployment로 돌아가는 공개 Phase API는 없다.
- Line of Sight, 명중, 회피, 방어력, 크리티컬, 지형별 이동 비용, 피해 감쇠는 구현 범위가 아니다.
- Akimbo 처치 후 추가 행동, Shotgun 위치별 DamageProfile, Buff/Debuff 시스템, Timeline을 직접 바꾸는 Trait는 후속 작업이다.

이 제한을 View 임시 규칙으로 보충하지 않는다. 필요한 기능은 Core 공개 계약을 확장하는 별도 작업으로 다룬다.

## 3. Query, Preview, Command 구분

| 종류 | 의미 | 대표 API | 상태 변경 |
|---|---|---|---|
| Query | 현재 전체 후보나 상태를 조회 | `GetSnapshot`, `GetTimeline`, `GetMovementRange`, `GetAttackRange` | 없음 |
| Preview | 특정 입력 하나의 예상 결과를 조회 | `PreviewCombatPlacement`, `PreviewMove`, `PreviewAttack` | 없음 |
| Command | 현재 상태에서 행동을 다시 검증하고 확정 | `TryPlaceOnCombat`, `AdvanceTimeline`, `TryMove`, `TryAttack`, `EndUnitTurn` | 성공 시 있음 |

Preview는 다음 Command의 허가증이 아니다. Command는 실행 시점의 Phase, 활성 Unit, 자원, 위치, Map 점유, 대상 상태와 Trait를 다시 계산한다.

### 3-1. 전체 공개 호출 지도

| View 행동 | 공개 API | 반환 타입 | View 처리 |
|---|---|---|---|
| Stage 생성 | `StageSessionFactory.Create(config, unitCatalog, structureCatalog)` | `StageSession` | Stage 동안 같은 Session 보관 |
| Player Reserve Unit 생성 | `session.CreatePlayerReserveUnit` | `EntitySnapshot` | 반환 `Id`와 GameObject 연결 |
| 전체 상태 조회 | `session.GetSnapshot` | `StageSnapshot` | 초기 구성·명시적 재동기화 |
| Entity 하나 조회 | `session.TryGetEntitySnapshot` | `bool`, `EntitySnapshot` | 상세 UI 갱신 |
| 배치 후보 | `session.PreviewCombatPlacement` | `CombatPlacementPreview` | 점유 셀 Highlight |
| 배치·재배치 | `session.TryPlaceOnCombat` | `DeploymentResult` | `CurrentStage` 적용 |
| Reserve 복귀 | `session.TryReturnToReserve` | `DeploymentResult` | `CurrentStage` 적용 |
| Player HQ 후보 | `session.PreviewInitialPlayerHeadquartersPlacement` | `CombatPlacementPreview` | 점유 셀 Highlight |
| Player HQ 확정 | `session.TryPlaceInitialPlayerHeadquarters` | `HeadquartersPlacementResult` | `CurrentStage` 적용 |
| Combat 진입 | `session.TryEnterCombat` | `CombatEntryResult` | 성공 여부와 `CurrentStage` 적용 |
| Timeline 조회 | `session.Combat.GetTimeline` | `CombatTimelineResult` | 반환 순서대로 아이콘 표시 |
| Timeline 진행 | `session.Combat.AdvanceTimeline` | `CombatTimelineAdvanceResult` | Unit 시작 또는 Stage Gimmick 발동으로 분기 |
| 턴 종료 | `session.Combat.EndUnitTurn` | `CombatTurnResult` | Snapshot 적용 후 Trait 발동 연출 |
| 이동 범위 | `session.Combat.GetMovementRange` | `MovementRangeResult` | 가능한 목적지 표시 |
| Route preview | `CombatViewPreviewData.Move(actor, path)` via Buffer/Job | `CombatPreviewResult.Move` and `MovementContext` | Display the exact candidate path and Core quote; see Section 8 |
| Route execution | `CombatViewCommandData.Move(actor, path, context)` via Buffer/Job | `CombatResult.Events` and `PathMovement` | Ordered full-result presentation and final Buffer reconciliation; see Section 8 |
| 공격 범위 | `session.Combat.GetAttackRange` | `AttackRangeResult` | 범위·대상·비용 지불 가능 여부 표시 |
| 공격 후보 | `session.Combat.PreviewAttack` | `AttackPreview` | 유효 비용·예상 피해·예상 Trait 표시 |
| 공격 확정 | `session.Combat.TryAttack` | `AttackResult` | 확정 피해·Trait·사망 연출, Snapshot 적용 |

### 3-2. 상태 변경 Result 처리 원칙

```text
1. Succeeded 확인
2. 실패면 FailureReason에 맞는 안내
3. 성공이면 Result의 확정 사실과 CurrentStage를 함께 보관
4. View 연출 정책에 맞춰 확정 사실 연출과 CurrentStage 적용
5. 성공과 실패 모두 CurrentStage가 있으면 영구 UI 상태를 최종 동기화
6. Range/Preview 캐시 무효화
```

Trait 연출도 성공한 Result의 `TraitActivations`만 사용한다. Preview의 예상 상태를 성공 연출로 확정하면 안 된다.

`TraitActivations`는 이번 동작에서 실제 발생한 **일회성 연출 사실**이고, `CurrentStage`는 동작 완료 후 **영구 UI 상태의 최종 권위**다. 성공 Command의 Snapshot을 연출 전·중·후 어느 시점에 적용할지는 View의 애니메이션 구조에 따라 정할 수 있다. 따라서 아래 예제들의 호출 순서가 다를 수 있지만, 연출이 끝난 화면은 반드시 `CurrentStage`와 일치해야 한다. 실패 Result도 `CurrentStage`가 있다면 이를 적용해 Core의 실제 상태와 다시 맞춘다.

## 4. StageConfig, UnitSpec, TraitSpec 구성

Unity Data를 Contracts 타입으로 바꾸는 작업은 Stage 시작 시 한 번 수행한다.

### 4-1. StageConfig

```csharp
InitialEnemyPlacementSpec[] initialEnemies =
{
    new InitialEnemyPlacementSpec(
        EntityKey.Unit_ShotGun,
        new CellCoord(4, 7),
        Rotation4.R180),
};

CellCoord[] railCells =
{
    new CellCoord(0, 4),
    new CellCoord(1, 4),
    new CellCoord(2, 4),
};

StageGimmickSpec[] stageGimmicks =
{
    new RailwayGimmickSpec(
        firstActivationAfterUnitTurns: 3,
        repeatIntervalUnitTurns: 4),
};

StageConfig config = new StageConfig(
    mapWidth: 9,
    mapHeight: 9,
    reserveCapacity: 6,
    playerDeploymentDepth: 3,
    enemyDeploymentDepth: 3,
    timelineSeed: 7301,
    initialEnemyPlacements: initialEnemies,
    railCells: railCells,
    stageGimmicks: stageGimmicks,
    nightLength: 120L,
    initialEnemyStructurePlacements: enemyStructures);
```

| 속성 | 의미 |
|---|---|
| `MapWidth`, `MapHeight` | 논리 Combat Grid 크기 |
| `ReserveCapacity` | Reserve 슬롯 수 |
| `PlayerDeploymentDepth` | `Y = 0`부터 Player 배치 행 수 |
| `EnemyDeploymentDepth` | 위쪽 끝에서 Enemy 배치 행 수 |
| `TimelineSeed` | Timeline 동률 순서를 결정적으로 재현하는 값 |
| `InitialEnemyPlacements` | 생성 시 배치할 Enemy의 Unit Key, anchor, rotation |
| `RailCells` | Railway가 영향을 주는 논리 Grid 좌표 |
| `StageGimmicks` | Timeline에 등록할 기믹 설정. 현재는 `RailwayGimmickSpec` 지원 |
| `NightLength` | 한 Night의 종료 기준이 되는 양수 시간 |
| `InitialEnemyStructurePlacements` | 생성 시 배치할 Enemy Structure Key, anchor, rotation |

두 배치 깊이의 합은 `MapHeight`보다 작아야 한다. 초기 적 anchor와 Rail Cell은 맵 안이어야 하고 Rail Cell은 중복될 수 없다. Railway가 하나라도 있으면 Rail Cell이 최소 하나 필요하다. `FirstActivationAfterUnitTurns`와 `RepeatIntervalUnitTurns`는 모두 양수다.

`InitialEnemyPlacements`, `RailCells`, `StageGimmicks`는 Stage Map Core가 직접 만드는 값이 아니다. `SelectedStageKey`에 대응하는 Unity Stage Data를 읽은 Bootstrap이 세 배열을 구성한다.

### 4-2. Trait가 있는 Radial UnitSpec

```csharp
TraitSpec[] akimboTraits =
{
    new FocusTraitSpec(
        damagePerStack: 2,
        maxStacks: 3),

    new MoveAttackDiscountTraitSpec(
        discountAmount: 2,
        minimumAttackCost: 1),
};

UnitSpec akimbo = new UnitSpec(
    EntityKey.Unit_Akimbo,
    footprintWidth: 1,
    footprintHeight: 1,
    footprintLocalCells: new[] { CellCoord.Zero },
    maxHealth: 10,
    moveMethod: MoveMethod.LiftAndPlace,
    moveRange: 5,
    moveCost: 1,
    attackPattern: AttackPatternSpec.CreateRadial(4),
    attackTargetMode: AttackTargetMode.SingleTarget,
    attackPower: 5,
    maxActionCost: 12,
    attackCost: 3,
    priority: 30,
    delay: 30,
    traits: akimboTraits);
```

Trait 목록은 생략할 수 있으며, 그러면 `TraitStatuses`, Preview projection, Result activation 배열이 비어 있는 Trait-free Unit이 된다. 같은 `TraitKey`를 한 UnitSpec에 두 번 넣거나 `null` Trait을 넣을 수 없다.

Trait 수치의 의미는 다음과 같다.

| Spec | 값 | 의미 |
|---|---|---|
| `FocusTraitSpec` | `DamagePerStack` | 활성 Focus 스택 하나당 추가 피해 |
|  | `MaxStacks` | 활성 스택 상한 |
| `MoveAttackDiscountTraitSpec` | `DiscountAmount` | 준비됐을 때 제안하는 기본 비용 감소량 |
|  | `MinimumAttackCost` | 할인 후 공격 비용 하한 |

### 4-3. DirectionalPattern UnitSpec

```csharp
AttackPatternSpec shotgunPattern = AttackPatternSpec.CreateDirectionalPattern(
    new[]
    {
        new CellCoord(-2, 2),
        new CellCoord(-1, 2),
        new CellCoord(0, 2),
        new CellCoord(1, 2),
        new CellCoord(2, 2),
        new CellCoord(0, 1),
    });

UnitSpec shotgun = new UnitSpec(
    EntityKey.Unit_ShotGun,
    footprintWidth: 1,
    footprintHeight: 1,
    footprintLocalCells: new[] { CellCoord.Zero },
    maxHealth: 10,
    moveMethod: MoveMethod.LiftAndPlace,
    moveRange: 3,
    moveCost: 1,
    attackPattern: shotgunPattern,
    attackTargetMode: AttackTargetMode.AllHostilesInArea,
    attackPower: 5,
    maxActionCost: 6,
    attackCost: 3,
    priority: 20,
    delay: 35,
    traits: Array.Empty<TraitSpec>());
```

DirectionalPattern의 local cell에는 `(0,0)`, 중복 셀, 빈 배열을 넣을 수 없다. 셀은 R0 기준 상대 좌표이며 footprint 셀과 다른 데이터다.

### 4-4. 기본값과 유효값을 구분한다

| 필드 | 의미 |
|---|---|
| `UnitStats.AttackPower` | Trait 적용 전 기본 공격력 |
| `UnitStats.AttackCost` | Trait 적용 전 기본 공격 비용 |
| `AttackPreview.Targets[i].ExpectedDamage` | 현재 Trait까지 적용한 해당 공격의 예상 피해 |
| `AttackPreview.AttackCost` | 현재 Trait까지 적용한 해당 공격의 유효 비용 |
| `AttackResult.Hits[i].DamageDealt` | 대상 HP에서 실제 감소한 양 |
| `AttackResult.ActionCostSpent` | 실제 지불한 유효 비용 |

View가 `AttackPower`와 `AttackCost`만으로 공격 Preview를 만들면 Focus와 할인을 즉시 잘못 표시하게 된다.

`AttackRange`는 `AppointRange`의, `ActionPoint`는 `MaxActionCost`의 호환 별칭이다. 신규 Unity Data 변환은 `AppointRange`, `MaxActionCost`를 기준 이름으로 사용한다.

Timeline 값이 없는 이전 호환 생성자는 컴파일 호환용이다. 그런 UnitSpec이 카탈로그에 있으면 `TryEnterCombat`의 Combat-ready 검증에서 거부된다. 신규 데이터는 양수 `Priority`, `Delay`를 사용한다.

### 4-5. StageSession 생성

```csharp
UnitSpec[] unitCatalog = { akimbo, shotgun };
StageSession session = StageSessionFactory.Create(
    config,
    unitCatalog,
    structureCatalog);
```

카탈로그는 비어 있을 수 없고 `EntityKey`가 중복될 수 없다. 초기 적의 `UnitKey`도 이 카탈로그에 있어야 한다. `StageSessionFactory.Create`는 초기 적 생성·배치까지 끝낸 뒤 Session을 반환하므로 View가 처음 받은 Snapshot에는 적이 이미 Combat Grid에 있다. 초기 적끼리 겹치거나, footprint가 맵 밖이거나, Enemy 배치 구역을 벗어나면 부분 성공 Session 대신 Bootstrap/Data 오류 예외가 난다.

`StageSession`은 선택한 실제 Stage 하나의 전투 수명 동안 Stage Controller/Presenter가 보관한다. Stage Map 한 판의 진행 상태를 보관하는 `StageMapSession`과 수명이 다르다.

## 5. Reserve 생성과 Deployment

### 5-1. Player Unit 생성과 초기 Enemy

```csharp
EntitySnapshot player = session.CreatePlayerReserveUnit(
    EntityKey.Unit_Akimbo,
    reserveSlotIndex: 0);

StageSnapshot initialStage = session.GetSnapshot();
ApplySnapshot(initialStage); // View helper 예시
```

Player 출전 Unit만 `CreatePlayerReserveUnit`으로 Reserve에 만든다. Enemy는 View에서 생성하거나 배치 Command를 호출하지 않는다. `StageConfig.InitialEnemyPlacements`에 들어 있던 Enemy는 Session 생성 시 이미 배치되어 있으므로 `initialStage.Entities`를 순회해 함께 View를 만든다.

`EntityKey`는 Unit 종류, `EntityId`는 이 Stage의 개별 인스턴스 ID다. View는 Snapshot의 ID와 GameObject/Presenter 연결을 저장한다. 각 Unit은 같은 Trait 구성을 사용해도 서로 독립적인 Trait 런타임 상태를 가진다.

없는 Key, 잘못된 슬롯, 점유된 슬롯은 일반 플레이 실패 Result가 아니라 Bootstrap/Data 오류 예외가 될 수 있다.

### 5-2. 배치 구역

9×9 맵, 양쪽 깊이 3일 때 전체 footprint가 다음 범위 안에 있어야 한다.

```text
Player: Y 0..2
중앙:   Y 3..5 (배치 불가)
Enemy:  Y 6..8
Neutral: 모든 행에서 배치 불가
```

anchor만 보지 말고 Preview가 반환한 회전 후 `OccupiedCells`와 `CanPlace`를 사용한다.

### 5-3. Drag Preview와 Drop

```csharp
CombatPlacementPreview preview = session.PreviewCombatPlacement(
    player.Id,
    new CellCoord(4, 2),
    Rotation4.R270);

for (int i = 0; i < preview.OccupiedCells.Length; i++)
{
    CellCoord cell = preview.OccupiedCells.Span[i];
    SetPlacementHighlight(cell, preview.CanPlace); // View helper 예시
}

DeploymentResult placement = session.TryPlaceOnCombat(
    player.Id,
    new CellCoord(4, 2),
    Rotation4.R270);

ClearPlacementGhost();                 // View helper 예시
ApplySnapshot(placement.CurrentStage); // View helper 예시
```

`OccupiedCells`는 배치 불가능한 후보에도 표시할 계산 셀을 제공한다. 존재하지 않는 Entity라면 빈 배열과 `CanPlace == false`다.

Deployment 중 이미 Combat에 있는 Entity를 다시 배치하는 것은 무료다. Combat 이동이 아니므로 이동 거리·행동력·Trait를 변경하지 않는다.

### 5-4. Reserve 복귀

```csharp
DeploymentResult returned = session.TryReturnToReserve(
    player.Id,
    targetReserveSlotIndex: 0);

ApplySnapshot(returned.CurrentStage); // View helper 예시
```

배치 Result에는 별도 실패 enum이 없다. 실패해도 `CurrentStage`를 적용한다. `TryEnterCombat` 성공 뒤 Combat 중에는 배치와 Reserve 복귀가 성공하지 않는다.

## 6. Combat 진입, Unit/Gimmick Timeline과 Trait 수명주기

### 6-1. Combat 진입

```csharp
CombatEntryResult entry = session.TryEnterCombat();
ApplySnapshot(entry.CurrentStage); // View helper 예시

if (entry.Succeeded == false)
{
    ShowCombatEntryFailure(entry.FailureReason); // View helper 예시
    return;
}
```

`TryEnterCombat`은 양 진영의 살아 있는 배치 HQ가 정확히 하나인지와 Unit 정의의 Timeline·이동·공격 설정을 먼저 검사한다. 그 뒤 현재 살아 있고 Combat에 배치된 Unit과 다음 Night에 재배치할 Enemy Unit으로 Timeline을 초기화하고 Phase를 Combat으로 바꾼다. Stage Gimmick 일정은 Session 생성 시 등록되어 있으며 `GetTimeline` Forecast에 Unit과 함께 합쳐진다.

다음은 플레이어 행동 실패가 아니라 Bootstrap 설정 오류이므로 `StageSessionFactory.Create`, Contracts 생성자 또는 `TryEnterCombat`의 실패 결과로 처리한다.

- 누락되거나 0 이하인 `Priority`, `Delay`
- 지원하지 않는 이동 또는 공격 조합
- DirectionalPattern의 잘못된 footprint/local cell
- 등록되지 않았거나 Spec 타입이 맞지 않는 Trait
- Focus 최대 보너스 피해의 정수 범위 초과
- 초기 Enemy의 누락된 Unit Key, 겹침, 범위·배치 구역 위반
- Railway가 있는데 Rail Cell이 없거나, Rail Cell이 범위 밖·중복인 구성

### 6-2. Timeline 조회

```csharp
CombatTimelineResult timeline = session.Combat.GetTimeline(maxEntries: 8);

if (timeline.Succeeded)
{
    for (int i = 0; i < timeline.Entries.Length; i++)
    {
        CombatTimelineEntrySnapshot entry = timeline.Entries.Span[i];
        RenderTimelineIcon(i, entry); // View helper 예시
    }
}
```

`maxEntries`는 1..64다. Entry는 반환 순서 그대로 표시한다. 같은 Unit이나 반복 기믹이 미래 순서에 여러 번 나타나는 것은 정상이다. View는 `ScheduledTime`, `CurrentPriority`, `RemainingUnitTurns`, `IsActive`를 다시 계산하거나 정렬하지 않는다.

`CombatTimelineEntrySnapshot.Kind`를 먼저 분기한다.

| `Kind` | 식별/표시 필드 | 사용하지 않는 필드 |
|---|---|---|
| `UnitTurn` | `UnitId`, `CurrentPriority`, `ScheduledTime`, `IsActive` | `GimmickId`, `GimmickKey`, `RemainingUnitTurns` |
| `StageGimmick` | `GimmickId`, `GimmickKey`, `ScheduledTime`, `RemainingUnitTurns` | `UnitId`, `CurrentPriority`; `IsActive`는 항상 false |

기믹 이름·아이콘·경고 색은 `StageGimmickKey`를 Unity Asset/Localization에 매핑한다. `RemainingUnitTurns`는 지금부터 완료되어야 할 Unit 턴 수이며, 0이면 다음 `AdvanceTimeline()`에서 기믹이 먼저 처리될 수 있다.

`StageSnapshot.CombatTurn.TimelineRevision`은 Timeline 캐시 무효화 키다.

- `EnterCombat`: 초기화로 Revision 변경
- 성공한 Unit 턴 시작: Revision 1 증가
- 성공한 턴 종료: Revision 1 증가
- Stage Gimmick 발동: Revision 1 증가, 발동으로 죽은 Timeline Unit마다 추가 증가
- 일반 이동·비치명 공격: Revision 유지
- 공격으로 죽은 Timeline Unit: 제거된 참가자 하나당 Revision 1 증가

`StageSnapshot.CombatTurn.CompletedUnitTurnCount`는 성공한 `EndUnitTurn()`마다 1 증가한다. Railway 발동 주기는 이 값을 기준으로 하며 View가 별도 카운터를 운영하지 않는다.

### 6-3. Timeline 진행: Unit 시작 또는 기믹 발동

```csharp
CombatTimelineAdvanceResult advance = session.Combat.AdvanceTimeline();

if (advance.Succeeded == false)
{
    ShowTimelineAdvanceFailure(advance.FailureReason); // View helper 예시
    ApplySnapshot(advance.CurrentStage);                // View helper 예시
    return;
}

switch (advance.Kind)
{
    case CombatTimelineAdvanceKind.UnitTurnStarted:
        ApplySnapshot(advance.CurrentStage);                  // View helper 예시
        FocusActiveUnit(advance.ActiveUnitId.Value);       // View helper 예시
        RenderTraitActivations(advance.TraitActivations);  // View helper 예시
        break;

    case CombatTimelineAdvanceKind.StageGimmickActivated:
        RenderStageGimmick(advance.GimmickActivation);    // View helper 예시
        ApplySnapshot(advance.CurrentStage);              // View helper 예시
        // 기믹 연출과 Snapshot 반영을 마친 뒤 AdvanceTimeline을 다시 호출한다.
        break;
}
```

`AdvanceTimeline()`은 현재 완료된 Unit 턴 수를 기준으로 due 기믹을 먼저 확인한다. due 기믹이 없을 때만 다음 Unit을 선택한다. stale Forecast나 View 타이머는 실행 권한이 아니며, 호출 순간의 Core 상태가 무엇을 처리할지 결정한다.

`UnitTurnStarted`는 하나의 Core 작업으로 다음을 확정한다.

```text
Timeline에서 다음 Unit 선택
→ Timeline Begin 반영
→ CombatTurn.ActiveUnitId 설정
→ RemainingMoveRange = MoveRange
→ CurrentActionCost = MaxActionCost
→ MovementCheckpointAnchor = 현재 Anchor
→ CommittedMoveDistance = 0
→ PendingMoveActionCost = 0
→ 턴 시작 Trait 전이 적용
→ 최종 StageSnapshot과 TraitActivations 반환
```

이 분기에서는 `ActiveUnitId`가 있고 `GimmickActivation`은 없다. 다음 행동자가 Enemy라면 기존 Enemy AI 연결은 `CurrentStage.CombatTurn.ActiveUnitId`와 Entity `Owner`를 확인한 뒤 `RunActiveEnemyTurn()`을 호출한다.

현재 Trait의 턴 시작 동작은 다음과 같다.

- Focus: `GainScheduled`가 있으면 활성 스택을 1 올리고 `FocusStackApplied`를 반환한다. 그 뒤 `GainScheduled`와 `MovedThisTurn`을 해제한다.
- MoveAttackDiscount: 이전 턴의 `FirstMoveConsumed`, `DiscountArmed` 플래그를 새 턴 상태로 초기화한다. 이 초기화 자체는 activation을 만들지 않는다.

### 6-4. Railway 발동 처리

`StageGimmickActivated`에서는 `ActiveUnitId`가 없고 `GimmickActivation`이 있다. 현재 지원 타입은 `RailwayGimmickActivationSnapshot`이다.

```csharp
void RenderStageGimmick(StageGimmickActivationSnapshot activation) // View helper 예시
{
    if (activation is RailwayGimmickActivationSnapshot railway)
    {
        PlayRailwayWarningAndImpact(railway.AffectedRailCells); // View helper 예시
        PlayKilledUnitEffects(railway.KilledUnitIds);            // View helper 예시
    }
}
```

- `AffectedRailCells`는 이번 Railway가 친 모든 Rail Cell이다.
- `KilledUnitIds`는 그 셀에 걸쳐 있던 살아 있는 배치 Unit 중 실제로 제거된 ID다. Player/Enemy를 구분하지 않는다.
- 최종 HP, 사망 여부, 빈 점유 셀과 Timeline 제거 상태는 `CurrentStage`가 권위다.
- 발동은 Unit 턴을 시작하지 않는다. 연출과 화면 동기화 후 `AdvanceTimeline()`을 다시 호출해야 다음 due 기믹 또는 Unit 턴으로 진행한다.
- 반복 일정은 Core가 `RepeatIntervalUnitTurns`만큼 갱신한다. View에서 재예약하지 않는다.

### 6-5. 턴 종료

```csharp
CombatTurnResult end = session.Combat.EndUnitTurn();

if (end.Succeeded == false)
{
    ShowTurnFailure(end.FailureReason); // View helper 예시
}

ApplySnapshot(end.CurrentStage);              // View helper 예시
RenderTraitActivations(end.TraitActivations); // View helper 예시
```

성공한 턴 종료는 현재 위치까지의 Pending 이동을 Commit하고 Timeline의 다음 행동 시간을 확정한 뒤, Trait 종료 전이를 적용하고 Active Unit을 해제한다. Commit은 행동력이나 이동 범위를 다시 차감하지 않는다. 다음 자기 턴은 마지막 위치를 새 이동 체크포인트로 사용한다.

현재는 사용하지 않은 이동 후 공격 할인이 준비돼 있으면 `AttackDiscountExpired`로 만료한다. 이미 공격에서 소비된 할인은 종료 시 중복 activation을 만들지 않는다.

다음 Unit이나 due 기믹은 자동 처리되지 않는다. View 연출을 마친 뒤 별도로 `AdvanceTimeline()`을 호출한다.

### 6-6. Timeline, 기믹과 Trait의 관계

현재 Focus와 MoveAttackDiscount는 Priority, Delay, CurrentTime, 다음 행동 순서를 직접 바꾸지 않는다. Unit Timeline과 Stage Gimmick Timeline의 결합 순서는 Core가 단일 권위자다. Trait 때문에 Timeline Revision이 바뀌는 경우는 현재 없으며, 공격이나 Railway로 Unit이 죽어 참가자 제거가 발생하면 해당 Command의 최종 Snapshot 전에 Timeline에서도 제거된다.

## 7. Trait 공개 데이터 계약

Trait UI를 만들 때 가장 중요한 구분은 다음 세 가지다.

| 데이터 | 위치 | 시점 | 뜻 | 사용처 |
|---|---|---|---|---|
| 현재 상태 | `UnitStatsSnapshot.TraitStatuses` | Snapshot 생성 시 | 이미 Core에 반영된 실제 상태 | 상태 아이콘, 상세 패널, 저장된 View 모델 |
| 예상 상태 | `MovePreview.ProjectedTraitStatuses`, `AttackPreview.ProjectedTraitStatuses` | 성공 가능한 Preview | 그 행동이 지금 성공하면 공격자/이동자의 Trait가 될 상태 | Hover 비교, 예상 아이콘 |
| 확정 발동 사실 | `MovementResult`, `AttackResult`, `CombatTurnResult`의 `TraitActivations` | 성공한 Command/턴 처리 후 | 이번 성공 동작에서 실제 발생한 알림 | VFX, SFX, 전투 로그, 토스트 |

흐름은 다음과 같다.

```text
현재 Snapshot.TraitStatuses
          │
          ├─ Preview 실패 → ProjectedTraitStatuses 비어 있음, 실제 상태 그대로
          │
          └─ Preview 성공 → ProjectedTraitStatuses로 예상 표시
                         │
                         ├─ Command 실패 → activation 없음, CurrentStage도 이전 상태
                         │
                         └─ Command 성공 → TraitActivations로 연출
                                          + CurrentStage.Entities의 해당 Unit
                                            → UnitStats.Value.TraitStatuses가 새 실제 상태
```

### 7-1. 현재 상태 읽기

배열 위치를 가정하지 말고 타입 또는 `Key`로 찾는다. 현재 출력은 `TraitKey` 순으로 결정적이지만, UI가 index 0을 Focus로 고정할 이유는 없다.

```csharp
UnitStatsSnapshot stats = entity.UnitStats.Value;

for (int i = 0; i < stats.TraitStatuses.Length; i++)
{
    TraitStatusSnapshot status = stats.TraitStatuses.Span[i];

    switch (status)
    {
        case FocusTraitStatusSnapshot focus:
            RenderFocusStatus( // View helper 예시
                focus.ActiveStacks,
                focus.GainScheduled,
                focus.MovedThisTurn);
            break;

        case MoveAttackDiscountTraitStatusSnapshot discount:
            RenderDiscountStatus( // View helper 예시
                discount.FirstMoveConsumed,
                discount.DiscountArmed);
            break;
    }
}
```

### 7-2. 발동 사실 읽기

아래는 공격 Result를 예로 든 View 처리 코드다. 이동·턴 Result도 같은 필드 형태로 처리한다.

```csharp
AttackResult attackResult = session.Combat.TryAttack(attackerId, primaryTargetId);

for (int i = 0; i < attackResult.TraitActivations.Length; i++)
{
    TraitActivationSnapshot activation = attackResult.TraitActivations.Span[i];

    EntitySnapshot source = FindCachedEntity(activation.SourceUnitId); // View helper 예시
    EntitySnapshot target = activation.TargetId.HasValue
        ? FindCachedEntity(activation.TargetId.Value)                  // View helper 예시
        : null;

    RenderTraitActivation( // View helper 예시
        source,
        target,
        activation.TraitKey,
        activation.TraitActivationKind,
        activation.Magnitude);
}
```

현재 두 Trait의 `TargetId`는 `null`이지만 optional 계약을 유지한다. `Magnitude`는 activation 종류에 따른 이벤트 값이지 HP나 행동력의 권위 있는 최종값이 아니다. 실제 피해·비용은 `Hits`, `ActionCostSpent`, `CurrentStage`를 사용한다.

### 7-3. enum 기반 로컬라이징

Core는 표시 문자열, Sprite, VFX prefab을 반환하지 않는다. View의 로컬 Asset/Localization Table에서 다음 키를 매핑한다.

- `TraitKey.Focus`, `TraitKey.MoveAttackDiscount`
- `TraitActivationKind.FocusGainScheduled`
- `TraitActivationKind.FocusStackApplied`
- `TraitActivationKind.FocusReset`
- `TraitActivationKind.AttackDiscountArmed`
- `TraitActivationKind.AttackCostReduced`
- `TraitActivationKind.AttackDiscountExpired`

표시 문구를 만들기 위해 Core 내부 Rule/RuntimeState 타입을 참조하지 않는다.

## 8. Current drag-path movement integration

Use the exported InGame View group's `Combat` service, the existing CombatView input Buffer, and the View-owned Flow/Plan/Job path. Core computes immutable transitions; Character/Combat Buffers own authoritative gameplay state. View never calls calculators to mutate a character and never reconstructs a route from its endpoint.

### Scene attachment and input

The merged production `CombatViewSystem` is registered by `InGameViewSystemFactory`. Its PostBind phase takes the existing Buffer, Global input/loading/Render, stage input slot and Combat entry services. Initialize registers the Combat scene hooks. After the existing entry owner activates Combat, the scene hook finds the single authored `CombatSceneRoot` and calls `viewGroup.Combat.Attach(sceneRoot)`. The root retains the incoming scene GUID and Field/Units/Hand/HUD/Ghost/camera references. It also forwards focus loss and disable. Scene-free Core developer/test compositions explicitly omit the live View module; they do not initialize a scene View with missing services.

`CombatSceneDisplay` supplies continuous screen-to-grid conversion, UI/hand cancel-area detection, path highlights, the Core quote and checkpoint Return UI through the existing presenters. Grid cells use floor semantics; centers are `(x + 0.5, y + 0.5)`. The Combat scene authors `InnerCellInset = 0.15` and `MovementStepSeconds = 0.12` as presentation/input tuning, not gameplay rules. Existing StageInput controllers forward press, ordered pointer samples and final release. `CombatPathInput` coordinates movement/Return input lifetime; the mutable draft remains private to `CombatMovementInteraction`. `CombatInteraction` retains placement and attack. No endpoint Move branch remains. RecordInputOnly does not synthesize a movement validation context or submit an unvalidated route.

### Preview and execution contracts

The input participant submits `CombatViewPreviewData.Move(actorId, path)` through `SetPreview`; `path` is a `MovementPath` beginning at the actual origin and containing each orthogonally adjacent entered cell. The existing Preview Job calls `ProcessLatestPreview`. Read `CombatPreviewResult.Move` and `MovementContext` from the Combat state Buffer. Draft revision/request/path/lifetime correlation is distinct from relevant public gameplay freshness; real publication validates its real read dependencies. Hidden hazards do not appear in public context or hidden-only stale rejection.

On release, freeze the exact path and wait for its matching valid quote. Submit `CombatViewCommandData.Move(actorId, path, context)` once. The Command Job calls `ProcessNextCommand`; Core revalidates before calculating and preparing one publication. Invalid release cancels the whole draft; no valid-prefix fallback or automatic rerouting exists. Range queries are hints and do not select the player's route.

Ordinary movement charges every actually entered voluntary step, including detours, reversal and loops. `PendingMoveDistance` and `PendingMoveActionCost` hold recorded refundable movement. `CommittedMoveDistance` holds sealed distance. `AttackRefundEligibleMoveCost` is a separate reviewed attack-category basis. Successful attack and turn end seal pending movement without a checkpoint refund; unsuccessful attack/mode cancellation do not. Core resolves entry hazards/death/recovery immediately, acquires souls only at final living arrival, and applies destination modifiers only there. Maximum-HP normalization caps current HP without a damage event. No attack/turn settlement replays those voluntary effects.

### Return and ordered presentation

The Return UI calls `viewGroup.Combat.RequestReturn()`. It obtains a fresh Core `ReturnToCheckpoint` preview and submits the existing Return intent only when allowed. Core refunds the pending ledger and recomputes position modifiers, without entry hazards, Object consumption, soul acquisition or reverse traversal. Sealed distance, HP and traits are not rewound.

`CombatViewSystem` remains the sole whole-result owner. `CombatResultPlayback` guards the existing result lifetime/token; `OrderedCombatPresentation` dispatches events in order inside the owner's Tick. `CombatMovementPlayback` handles one Moved step or immediate field-to-field Returned snap. `ICombatResultPresenter` handles non-movement events with `CanPresent` and `BeginEvent`; it no longer reads Buffers or reconciles the whole result. The production display uses event facts for character/structure health, death and soul character changes; non-movement VFX remain immediate at the incoming View's current presentation level. Unsupported events block completion. After all events complete, the owner refreshes from Buffers, removes handled commands and calls `AcknowledgePresented` once. No presentation participant applies gameplay or starts processing Jobs.

### Exit and remaining acceptance

The existing transition owner must stop new Flow registration, finish registered Jobs and settle the entire pending result before normal detach/input clearing. Detach invalidates old callbacks, cancels draft/animation and unbinds input; it does not acknowledge unfinished results or drain/clear the shared Runner. The incoming F2 normal-exit transition remains a separate dependency.

The merged Combat scene, actual database, Render color scopes and pooled unit presenters have an automated drag -> step playback -> Return-button integration check. The final merged EditMode suite passed 2,255/2,255 after review fixes. See the [merge verification record](../handoffs/2026-10-04-stage-merge-verification.md) for final evidence. Authored mine/Object/destination-modifier facts, live perception, polished non-movement effects and manual zoom/FPS/UI-overlap acceptance remain separate work; no values or new gameplay rules were invented for them.

## 9. 공격: Range → Preview → Command

### 9-1. 공격 범위 Query

```csharp
AttackRangeResult range = session.Combat.GetAttackRange(activeUnitId);

if (range.Succeeded == false)
{
    ShowAttackFailure(range.FailureReason); // View helper 예시
    return;
}

ShowAttackRange(range.RangeCells);             // View helper 예시
ShowSelectableTargets(range.InRangeTargetIds); // View helper 예시
SetAttackButtonEnabled(range.CanAffordAttack); // View helper 예시
```

`CanAffordAttack`은 기본 `UnitStats.AttackCost`가 아니라 현재 Trait를 적용한 유효 비용으로 계산한다. Range Result는 비용 숫자를 직접 주지 않으므로 선택 대상의 실제 예상 비용 표시는 `PreviewAttack.AttackCost`를 사용한다.

Radial의 `RangeCells`는 거리 범위, DirectionalPattern의 `RangeCells`는 네 방향 셀의 합집합이다. 실제 한 번의 공격 방향은 PrimaryTarget을 선택한 Preview/Command에서 정한다.

### 9-2. 공격 Preview

```csharp
AttackPreview preview = session.Combat.PreviewAttack(
    activeUnitId,
    primaryTargetId);

if (preview.CanAttack == false)
{
    ShowAttackFailure(preview.FailureReason); // View helper 예시
    return;
}

ShowAttackDirection(preview.AttackDirection);       // View helper 예시
ShowAffectedCells(preview.AffectedCells);            // View helper 예시
ShowEffectiveAttackCost(preview.AttackCost);         // View helper 예시
ShowProjectedTraits(preview.ProjectedTraitStatuses); // View helper 예시

for (int i = 0; i < preview.Targets.Length; i++)
{
    AttackTargetPreview target = preview.Targets.Span[i];
    ShowPredictedHealth( // View helper 예시
        target.TargetId,
        target.ExpectedDamage,
        target.TargetHealthBefore,
        target.TargetHealthAfter,
        target.TargetKilled);
}
```

Preview는 현재 상태에서 하나의 공격 계획을 계산한다.

```text
공격 문맥·Map 점유·PrimaryTarget 검증
→ 공격 방향과 영향 셀 결정
→ 현재 Trait로 유효 공격 비용 계산
→ 현재 Trait로 대상별 유효 피해 계산
→ 공격 성공 후 예상 Trait 상태 계산
→ AttackPreview 반환, 실제 상태는 그대로
```

Radial은 PrimaryTarget 한 명만 `Targets`에 넣는다. DirectionalPattern은 PrimaryTarget이 포함되는 결정적 한 방향을 고르고 그 방향과 겹치는 모든 적대 Entity를 `EntityId` 오름차순으로 넣는다.

`AttackDirection`은 공격 연출 방향이지 Entity의 배치 회전 변경 명령이 아니다.

### 9-3. 실제 공격

```csharp
AttackResult result = session.Combat.TryAttack(
    activeUnitId,
    primaryTargetId);

if (result.Succeeded == false)
{
    ShowAttackFailure(result.FailureReason); // View helper 예시
    ApplySnapshot(result.CurrentStage);      // View helper 예시
    return;
}

PlayAttack( // View helper 예시
    result.AttackerId,
    result.AttackDirection,
    result.AffectedCells,
    result.Hits,
    result.ActionCostSpent);

RenderTraitActivations(result.TraitActivations); // View helper 예시
ApplySnapshot(result.CurrentStage);              // View helper 예시
```

`AttackResult`의 확정값은 다음과 같다.

| 필드 | 의미 |
|---|---|
| `ActionCostSpent` | 실제 지불한 Trait 적용 후 비용. 대상 수와 관계없이 한 번만 소비 |
| `AffectedCells` | 실제 선택된 한 방향의 영향 셀 |
| `Hits` | 피해를 받은 대상별 실제 결과 |
| `TraitActivations` | 성공한 공격에서 실제 확정된 Trait 발동 |
| `CurrentStage` | 피해·자원·Trait·사망 정리까지 완료된 Stage |

`TryAttack` 성공은 공격 직전 위치까지의 Pending 이동도 함께 Commit한다. 그 위치가 다음 이동 구간의 체크포인트가 되며, 이미 사용한 턴 전체 `MoveRange`는 충전되지 않는다. `GetAttackRange`, `PreviewAttack`, 실패한 `TryAttack`은 이동을 Commit하지 않는다.

`AttackHitResult.DamageDealt`는 공격 수치가 아니라 실제 HP 감소량이다. 예를 들어 예상 피해가 10이어도 대상 HP가 3이면 `DamageDealt == 3`이다.

### 9-4. 공격의 내부 원자성 흐름

다음 타입과 메서드는 `internal`이며 View API가 아니다. 다만 실패 시 왜 일부 상태만 바뀌는 일이 없어야 하는지 이해하기 위한 구조다.

```text
AttackResolutionPlan 구성
  - 유효 비용
  - 대상별 유효 피해와 공격 전후 HP
  - 사망 시 해제할 Map 셀
  - Trait 최종 전이·예상 상태·발동 사실

TimelineRemovalPlan 구성
  - 죽을 Timeline Unit ID 집합
  - 현재 Revision
  - 제거 수를 반영한 최종 Revision

모든 preflight 성공
→ HP·Map·공격 자원·Trait 일괄 Commit
→ 공격자의 Pending 이동 Commit, 현재 위치로 체크포인트 전진
→ 죽은 Timeline 참가자 일괄 Commit
→ 최종 StageSnapshot 생성
```

다중 대상 공격으로 Unit 둘이 죽으면 둘을 한 번의 사전 검증된 batch로 제거하고 Revision은 참가자 수만큼 증가한다. Structure나 이미 Timeline에 없는 대상은 Timeline 제거 대상이 아니다. 최종 Snapshot을 만들 때는 죽은 Unit이 Timeline에서 이미 빠져 있다.

이 구조 때문에 View는 `Hits`를 순회하면서 Timeline 제거 API를 따로 호출하지 않는다. 그런 공개 API도 없다.

실패 시 HP, Map, 행동력, 이동 체크포인트·Pending 비용, Trait, 활성 턴, Timeline 참가자·시간·Revision이 모두 보존되고 `TraitActivations`는 비어 있다.

## 10. Focus 정확한 동작

Focus 상태는 다음 세 값으로 보인다.

| 상태 | 의미 |
|---|---|
| `ActiveStacks` | 지금 공격의 추가 피해에 반영되는 스택 |
| `GainScheduled` | 다음 자기 턴 시작에 스택 1 증가가 예약됨 |
| `MovedThisTurn` | 이번 자기 턴에 성공 이동했음 |

### 10-1. 공격과 예약

성공 공격 시 다음 조건을 모두 만족하면 한 번 예약한다.

```text
MovedThisTurn == false
GainScheduled == false
ActiveStacks < MaxStacks
```

이때 `FocusGainScheduled`, `Magnitude = 1`이 반환된다. 같은 턴의 두 번째 성공 공격은 이미 `GainScheduled == true`이므로 중복 예약·activation이 없다.

예약된 스택은 방금 성공한 공격의 피해에 들어가지 않는다. 피해 계산은 공격 전에 이미 활성화된 `ActiveStacks`만 사용한다.

```text
유효 피해 = 기본 AttackPower + DamagePerStack × ActiveStacks
```

### 10-2. 다음 자기 턴 시작

다음 자기 턴에 `AdvanceTimeline()`이 그 Unit의 `UnitTurnStarted`를 반환할 때 예약 스택을 실제 활성 스택으로 옮긴다.

```text
GainScheduled == true
→ ActiveStacks + 1, 단 MaxStacks 이하
→ GainScheduled = false
→ MovedThisTurn = false
→ FocusStackApplied
```

다른 Unit의 턴 시작이 아니라 **해당 Unit 자신의 다음 턴 시작**이다.

### 10-3. 성공 이동과 실패 이동

성공 이동은 활성 스택과 예약을 모두 없애고 `MovedThisTurn = true`로 만든다. 활성 스택이나 예약이 실제로 제거되면 `FocusReset`을 반환한다.

`FocusReset.Magnitude`는 제거 전 활성 스택 수다. 예약만 취소된 상황이면 0일 수 있으므로, View는 `Magnitude > 0`을 activation 존재 여부로 사용하면 안 된다.

실패 이동은 Focus를 변경하지 않는다. 성공 이동 뒤 같은 턴에 공격해도 `MovedThisTurn == true`이므로 새 Focus 스택을 예약하지 않는다. 체크포인트로 돌아와 이동 행동력을 모두 환급해도 `MovedThisTurn`, 제거된 Focus 스택과 예약은 복구되지 않는다.

## 11. MoveAttackDiscount 정확한 동작

할인 상태는 다음 두 값으로 보인다.

| 상태 | 의미 |
|---|---|
| `FirstMoveConsumed` | 이번 자기 턴에 할인을 준비할 첫 성공 이동 기회를 이미 사용함 |
| `DiscountArmed` | 다음 유효 공격에 적용할 할인이 준비됨 |

### 11-1. 첫 성공 이동

자기 턴의 첫 성공한 위치 변경이며 이번 Command에서 새로 차감한 이동 비용이 양수이면 다음 상태가 된다.

```text
FirstMoveConsumed = true
DiscountArmed = true
AttackDiscountArmed 발생
```

Current route behavior: every entered voluntary step is paid, including reverse movement and loops. Movement-success traits run once per executed drag, using actual paid movement. Explicit Return refunds its recorded pending resources without rewinding trait state or triggering another movement-success event. Failed drafts do not consume the first-move opportunity.

### 11-2. Range와 Preview

할인이 준비되면 `GetAttackRange.CanAffordAttack`, `PreviewAttack.AttackCost`, `TryAttack.ActionCostSpent`가 같은 유효 비용 계산을 사용한다.

```text
유효 비용 = max(적용 중인 비용 하한, 기본 AttackCost - 적용 감소량 합)
```

여기서 적용 중인 비용 하한은 **Core 기본 하한 1**과 **양수 감소량을 기여한 Trait들의 `MinimumCost` 중 최댓값**이다. 이는 계약을 이해하기 위한 설명이며, View는 이 공식을 다시 계산하지 않고 Query·Preview·Command가 반환한 비용을 사용한다.

Preview는 할인을 소비하지 않는다. 다만 공격이 성공하면 소비될 상황이라면 `ProjectedTraitStatuses`에서 `DiscountArmed == false`를 보여 준다. 실제 Snapshot은 Preview 전 상태를 유지한다.

### 11-3. 성공 공격의 소비와 0 감소 예외

할인이 실제 양수 비용 감소에 기여한 성공 공격만 할인을 소비하고 `AttackCostReduced`를 반환한다. 공격이 실패하면 행동력과 할인 모두 그대로다.

기본 `AttackCost`가 이미 `MinimumAttackCost` 이하라면 실제 감소 기여는 0이다.

```text
DiscountArmed == true
기본 AttackCost <= MinimumAttackCost
→ 유효 비용은 기본 비용
→ 할인 소비 안 함
→ AttackCostReduced 없음
→ DiscountArmed 유지
```

기본 비용이 하한보다 크지만 `DiscountAmount` 전체를 빼면 하한 아래가 되는 경우에는 하한까지만 실제 비용이 줄어도 Trait는 양수 기여자로 취급되어 성공 시 소비된다.

`AttackCostReduced.Magnitude`는 현재 구성된 `DiscountAmount`를 담는다. 하한 때문에 실제 감소량과 다를 수 있으므로 실제 지불 비용은 `AttackResult.ActionCostSpent`, 예상 비용은 `AttackPreview.AttackCost`를 사용한다.

### 11-4. 턴 종료와 다음 턴

준비된 할인을 사용하지 않고 턴을 끝내면 다음이 발생한다.

```text
DiscountArmed = false
AttackDiscountExpired 발생
```

이미 공격으로 소비한 경우에는 종료 시 만료 activation이 없다. 다음 자기 턴 시작에 두 플래그가 새 턴 상태로 초기화된다.

## 12. Snapshot과 Result를 화면에 적용하는 기준

### 12-1. StageSnapshot

| 필드 | View 적용 대상 |
|---|---|
| `MapWidth`, `MapHeight` | 논리 Grid |
| `CurrentPhase` | Deployment/Combat 입력 모드 |
| `Entities` | Entity View 생성·갱신 |
| `ReserveSlots` | Reserve UI |
| `CombatTurn` | 활성 Unit과 Timeline Revision 캐시 |

`Entities`는 `EntityId` 오름차순으로 만들어진다. 그래도 View 데이터 연결은 index가 아니라 `EntityId`를 기준으로 한다.

### 12-2. EntitySnapshot과 UnitStatsSnapshot

`EntityArea.Reserve`이면 `ReserveSlotIndex`, `EntityArea.Combat`이면 `CombatAnchor`, `CombatRotation`, `OccupiedCells`를 사용한다.

공통 필드는 `Id`, `Key`, `Kind`, `Owner`, `MaxHealth`, `CurrentHealth`, `IsDead`다. Unit은 `UnitStats.HasValue == true`이며 다음을 제공한다.

| 정적/기본값 | 현재값 |
|---|---|
| `MoveMethod`, `MoveRange`, `MoveCost` | `RemainingMoveRange` |
| `AppointMethod`, `AppointRange` | `CurrentActionCost` |
| `AttackPower`, `MaxActionCost`, `AttackCost` | `TraitStatuses` |
| `Priority`, `Delay` |  |

`MoveRange`와 `MaxActionCost`를 현재 남은 자원처럼 표시하지 않는다. `AttackPower`, `AttackCost`도 이번 공격의 Trait 적용 최종값으로 사용하지 않는다.

### 12-3. 사망 Entity

죽은 Entity는 Snapshot에서 즉시 삭제되지 않는다.

```text
IsDead = true
CurrentHealth = 0
CombatAnchor/CombatRotation = 마지막 배치값
OccupiedCells = 빈 배열
```

공격 Result의 `TargetKilled`와 Snapshot의 마지막 위치로 사망 연출을 한다. 빈 `OccupiedCells`는 게임 규칙상 Map 점유가 이미 해제됐다는 뜻이다. 죽은 Unit은 같은 공격의 최종 Snapshot 전에 Timeline에서도 제거된다.

### 12-4. ReadOnlyMemory 순회

```csharp
for (int i = 0; i < stage.Entities.Length; i++)
{
    EntitySnapshot entity = stage.Entities.Span[i];
    ReconcileEntityView(entity); // View helper 예시
}
```

Contracts 배열은 과거 Result/Snapshot이 이후 Command로 바뀌지 않도록 복사된다. View도 이를 수정 가능한 Domain 상태처럼 취급하지 않는다.

## 13. 실패와 예외

### 13-1. 성공과 실패 필드

| 반환 타입 | 성공 확인 | 실패 정보 | Snapshot | 실패 시 Trait 배열 |
|---|---|---|---|---|
| `CombatPlacementPreview` | `CanPlace` | 없음 | 없음 | 해당 없음 |
| `DeploymentResult` | `Succeeded` | 없음 | `CurrentStage` | 해당 없음 |
| `CombatTimelineAdvanceResult` | `Succeeded` | `CombatTimelineAdvanceFailureReason` | `CurrentStage` | 실패 시 비어 있음 |
| `CombatTurnResult` | `Succeeded` | `CombatTurnFailureReason` | `CurrentStage` | 비어 있음 |
| `CombatTimelineResult` | `Succeeded` | `CombatTimelineFailureReason` | 없음 | 해당 없음 |
| `MovementRangeResult` | `Succeeded` | `MovementFailureReason` | 없음 | 해당 없음 |
| `MovePreview` | `CanMove` | `MovementFailureReason` | 없음 | projection 비어 있음 |
| `MovementResult` | `Succeeded` | `MovementFailureReason` | `CurrentStage` | activation 비어 있음 |
| `AttackRangeResult` | `Succeeded` | `AttackFailureReason` | 없음 | 해당 없음 |
| `AttackPreview` | `CanAttack` | `AttackFailureReason` | 없음 | projection 비어 있음 |
| `AttackResult` | `Succeeded` | `AttackFailureReason` | `CurrentStage` | activation 비어 있음 |

### 13-2. Timeline 진행과 턴 종료 실패

`AdvanceTimeline()`의 현재 실패 사유는 다음과 같다.

| `CombatTimelineAdvanceFailureReason` | 의미 |
|---|---|
| `WrongPhase` | Combat이 아님 |
| `ActiveUnitAlreadyExists` | 끝나지 않은 활성 Unit 턴이 있음 |
| `NoTimelineEntry` | 처리할 due 기믹도, 시작할 Unit도 없음 |

`EndUnitTurn()`에서 View가 정상적으로 마주칠 실패는 `WrongPhase`, `NoActiveUnit`이다. 아래 `CombatTurnFailureReason`의 다른 값은 기존 계약 호환이나 내부 흐름에서 남아 있을 수 있으므로 enum exhaustiveness는 유지하되, 새 턴 시작에는 사용하지 않는다.

| 값 | 의미 |
|---|---|
| `WrongPhase` | Combat이 아님 |
| `ActiveUnitAlreadyExists` | 이미 활성 Unit이 있음 |
| `NoActiveUnit` | 종료할 Unit이 없음 |
| `NoEligibleUnit` | Timeline에 다음 참가자가 없음 |

Timeline과 Stage Entity가 서로 모순된 상태는 플레이 입력 실패가 아니라 내부 동기화 오류이므로 예외가 될 수 있다.

### 13-3. MovementFailureReason

| 값 | 의미 |
|---|---|
| `EntityNotFound`, `NotUnit` | ID 해석 실패 |
| `WrongPhase`, `NotInCombat`, `Dead`, `NotActiveUnit` | 행동 문맥 실패 |
| `UnsupportedMoveMethod` | 지원하지 않는 이동 방식 |
| `SameAnchor` | 같은 anchor |
| `OutOfBounds` | footprint가 Map 밖 |
| `DestinationOccupied` | 다른 Entity가 점유 |
| `MoveRangeExceeded` | 남은 거리 초과 |
| `ActionCostInsufficient` | 이동 행동력 부족 |
| `StateInconsistent` | Entity 위치와 Map 점유 불일치 |

### 13-4. AttackFailureReason

| 값 | 의미 |
|---|---|
| `WrongPhase`, `NotInCombat`, `Dead`, `NotActiveUnit` | 공격 문맥 실패 |
| `EntityNotFound`, `NotUnit` | 공격자/대상 해석 실패 |
| `NotSupportAppointMethod` | 지원하지 않는 공격 구성 |
| `InvalidPrimaryTarget` | 자신 등 PrimaryTarget으로 부적절 |
| `NonHostile` | 같은 Owner 또는 Neutral 관계 |
| `OutOfBounds` | 선택 대상이 공격 범위 밖 |
| `ActionCostInsufficient` | Trait 적용 유효 비용 지불 불가 |
| `MapOccupancyMismatch` | Entity footprint와 Map 점유 불일치 |

`StateInconsistent`, `MapOccupancyMismatch`는 일반 입력 오류보다 Core/View 동기화 문제로 로깅하고 반환 Snapshot으로 재동기화한다.

### 13-5. Result가 아닌 예외

잘못된 맵 크기·배치 깊이, 잘못된 UnitSpec/TraitSpec/GimmickSpec, 중복 Key/Trait/Rail Cell, 범위 밖 초기 적·Rail Cell·Reserve 슬롯, 초기 적 배치 충돌, Railway의 빈 Rail Cell, 정의되지 않은 Rotation, 빈 필수 ID, `GetTimeline`의 잘못된 Entry 수는 개발 설정 오류로 예외가 날 수 있다. Unity Data를 Contracts로 바꾸는 Bootstrap 경계에서 검증한다.

## 14. 성능, 호출 빈도와 캐시

Core에는 View의 `Update`처럼 매 프레임 자동 실행되는 루프가 없다. 다만 Range, Preview, Timeline Forecast, Snapshot은 계산과 방어적 배열 복사를 수행하므로 동일한 질문을 매 프레임 반복하지 않는다.

| API | 권장 호출 시점 | 캐시 무효화 |
|---|---|---|
| `PreviewCombatPlacement` | Entity/anchor/rotation 중 하나가 바뀔 때 | 배치 Command 뒤 |
| `GetMovementRange` | 활성 Unit 변경 또는 상태 변경 뒤 | 이동·공격·턴 Command 뒤 |
| Buffer route preview | actor/path/draft revision changes | Relevant public context changes; exact frozen request correlation |
| `GetAttackRange` | 활성 Unit 변경 또는 상태 변경 뒤 | 이동·공격·턴 Command 뒤 |
| `PreviewAttack` | attackerId/primaryTargetId가 바뀔 때 | 상태 변경 뒤 |
| `GetTimeline` | `TimelineRevision`이 바뀔 때 | Revision 변경 시 |
| `GetSnapshot` | 초기 화면 또는 명시적 복구 | Command 직후 중복 호출하지 않음 |

Command가 반환한 `CurrentStage`가 있으면 직후 `GetSnapshot()`을 다시 호출할 필요가 없다. Trait projection도 같은 입력과 같은 Core 상태 동안만 캐시한다.

고정된 GC 바이트 추정치는 문서 계약이 아니다. 실제 최적화 판단은 목표 Scene, Mono/IL2CPP 환경에서 Unity Profiler로 호출 횟수, CPU 시간, GC Alloc을 함께 측정한다.

## 15. 현재 전체 View 호출 흐름

```text
Bootstrap
1. 선택된 StageKey로 Unity Stage Data 조회
2. InitialEnemyPlacements, RailCells, StageGimmicks를 포함한 StageConfig와 UnitSpec, TraitSpec 변환
3. StageSessionFactory.Create(config, unitCatalog, structureCatalog): 초기 Enemy Unit/Structure 배치와 기믹 일정 구성
4. CreatePlayerReserveUnit 반복
5. GetSnapshot 적용: Player Reserve와 이미 배치된 Enemy를 함께 생성

Deployment
6. 입력이 바뀔 때 PreviewCombatPlacement
7. Drop 시 TryPlaceOnCombat 또는 TryReturnToReserve
8. DeploymentResult.CurrentStage 적용

Combat 진입
9. TryPlaceInitialPlayerHeadquarters로 Player HQ 확정
10. TryEnterCombat 결과의 CurrentStage 적용
11. TimelineRevision 기준으로 GetTimeline 호출

Timeline 진행
12. AdvanceTimeline
13-a. UnitTurnStarted: CurrentStage 적용, TraitActivations 연출, Unit 입력/AI 진행
13-b. StageGimmickActivated: 기믹 결과 연출, CurrentStage 적용, 다시 AdvanceTimeline

각 Unit 턴
14. GetMovementRange/GetAttackRange

이동 입력
15. Request Buffer route Preview when the immutable candidate path changes
16. Display exact path, nullable Core quote and independent pointer validity
17. Release freezes the exact path; matching validation submits one Move intent
18. Present all result events in order, reconcile Buffers and acknowledge once
19. Safe drags accumulate pending movement; Return uses a separate fresh Core preview/intent

공격 입력
20. PrimaryTarget 변경 시 PreviewAttack
21. AttackCost + Targets + ProjectedTraitStatuses 임시 표시
22. 공격 상태 취소는 View 입력 모드만 이동 상태로 복귀
23. 확정 시 TryAttack
24. 성공 공격이면 현재 위치로 이동 체크포인트 전진
25. Hits + TraitActivations 연출, CurrentStage 적용
26. 사망 Entity View 처리, TimelineRevision 변경 시 GetTimeline 갱신

턴 종료
27. EndUnitTurn
28. Pending 이동 Commit이 반영된 CurrentStage 적용, 만료 TraitActivations 연출
29. 종료 연출 뒤 다시 AdvanceTimeline
```

View는 위 흐름에서 ID와 좌표를 입력으로 전달하고, Core가 반환한 예상값·확정값을 표시한다. View가 계산한 예상 비용이나 피해를 Command에 다시 전달하는 단계는 없다.

## 16. 향후 Trait 추가 시 경계

이 절은 Core 확장 작업자를 위한 구조 설명이다. View가 아래 내부 타입을 직접 사용하는 방법이 아니다.

새 Trait는 다음 구성을 명시적으로 추가한다.

```text
Contracts TraitSpec
→ 불변 Domain TraitDefinition
→ Unit별 TraitRuntimeState
→ 필요한 capability Rule만 구현
→ Definition/RuntimeState factory
→ TraitRegistry에 한 번 명시적으로 등록
→ 현재/예상 상태용 typed TraitStatusSnapshot
→ 필요한 TraitActivationKind
→ Rule, 통합, View 경계 테스트
```

Rule은 자신의 Definition/RuntimeState와 필요한 scalar context만 읽는다. Entity 사전, Map, Timeline, 이동·공격 Service를 받지 않는다. Rule이 Map/HP/자원/Timeline을 직접 변경하지 않고 최종값 계획을 반환하며 Facade/Service가 전체 작업을 조정한다.

View 쪽 추가 작업은 다음뿐이다.

- 새 `TraitKey`/`TraitActivationKind` 로컬라이징과 Asset 매핑
- 새 typed status의 아이콘·상세 UI
- 성공 activation의 VFX/SFX/로그

캐릭터 `EntityKey` 분기, Reflection 기반 자동 실행, View 이벤트로 Core Rule을 우회하는 방식은 현재 구조가 아니다.

## 17. View 구현 완료 체크리스트

### Assembly와 Bootstrap

- [ ] View asmdef는 `ProjectL.Core.Application`, `ProjectL.Core.Contracts`만 참조한다.
- [ ] View 코드에 `ProjectL.Core.Domain` 참조가 없다.
- [ ] 선택한 `StageKey`를 Unity Stage Data에서 해석해 `StageConfig`를 만든다.
- [ ] `StageConfig`에 배치 깊이, `TimelineSeed`, 초기 Enemy Unit/Structure, Rail Cell, Stage Gimmick, `NightLength`를 설정한다.
- [ ] 초기 Enemy와 Player가 사용할 모든 `EntityKey`를 Unit 카탈로그에 넣는다.
- [ ] 초기 Enemy를 View에서 생성·배치하지 않고 첫 `StageSnapshot.Entities`로 생성한다.
- [ ] 모든 Combat용 UnitSpec에 양수 `Priority`, `Delay`를 설정한다.
- [ ] Trait는 공개 `TraitSpec`으로 구성하고 같은 Key를 중복하지 않는다.
- [ ] Enemy Structure는 Structure Catalog와 초기 배치 입력으로 만들고, Player HQ는 공개 Preview/Commit API로 배치한다.

### Deployment

- [ ] `EntityArea`로 Reserve와 Combat을 구분한다.
- [ ] 배치 Zone과 회전 footprint를 View에서 다시 계산하지 않는다.
- [ ] `PreviewCombatPlacement.OccupiedCells`로 Ghost를 그린다.
- [ ] Drop 뒤 성공·실패 모두 `DeploymentResult.CurrentStage`를 적용한다.
- [ ] Neutral Combat 배치가 현재 지원되지 않음을 반영한다.

### Timeline과 턴

- [ ] 다음 Unit/기믹을 View가 고르지 않고 `AdvanceTimeline()`에 맡긴다.
- [ ] `CombatTimelineEntrySnapshot.Kind`로 Unit과 Stage Gimmick 표시를 분기한다.
- [ ] `CombatTimelineAdvanceResult.Kind`로 Unit 시작과 기믹 발동 처리를 분기한다.
- [ ] Railway의 `AffectedRailCells`, `KilledUnitIds`로 연출하고 최종 상태는 `CurrentStage`로 동기화한다.
- [ ] 기믹 발동 연출 뒤 `AdvanceTimeline()`을 다시 호출한다.
- [ ] `CompletedUnitTurnCount`나 기믹 반복 일정을 View에서 별도 계산하지 않는다.
- [ ] `CombatTurn.ActiveUnitId`로 활성 표시를 갱신한다.
- [ ] Timeline Entry를 재정렬하거나 시간을 재계산하지 않는다.
- [ ] `TimelineRevision`이 바뀔 때만 Timeline 캐시를 갱신한다.
- [ ] `CombatTimelineAdvanceResult`의 Unit 턴 시작 activation과 `CombatTurnResult`의 턴 종료 activation을 처리한다.

### 이동과 공격

- [ ] Range, Preview, Command를 구분한다.
- [ ] Move/Return carry their matching MovementPreviewContext; no View-calculated billing/effects or stale draft response authorizes execution.
- [ ] Use PathMovement/MovePreview for actual route detail and quoted resources; never infer billing from checkpoint displacement. Only explicit Return refunds pending movement.
- [ ] 공격 상태 진입·취소만으로 이동이 Commit되지 않음을 반영한다.
- [ ] Successful attack, turn end and resolved barriers seal pending movement; checkpoint Return remains separate from the attack-category refund basis.
- [ ] 공격 유효 비용은 `AttackPreview.AttackCost`/`AttackResult.ActionCostSpent`를 사용한다.
- [ ] 예상 피해는 `AttackTargetPreview`, 실제 피해는 `AttackHitResult`를 사용한다.
- [ ] Directional Range 합집합과 실제 한 방향 `AffectedCells`를 구분한다.
- [ ] 공격 방향으로 `CombatRotation`을 임의 변경하지 않는다.
- [ ] 사망 Entity의 빈 `OccupiedCells`를 Map 점유 해제로 해석한다.

### Trait UI와 상태 동기화

- [ ] `TraitStatuses`, `ProjectedTraitStatuses`, `TraitActivations`의 역할을 섞지 않는다.
- [ ] Trait 배열 index가 아니라 typed status/enum Key로 매핑한다.
- [ ] Preview로 실제 상태나 확정 연출을 변경하지 않는다.
- [ ] 실패 Result에서 activation이 없음을 전제로 실제 Snapshot을 유지한다.
- [ ] `Magnitude` 대신 Result의 피해·비용 필드를 실제 수치의 권위자로 사용한다.
- [ ] Core enum을 Unity 로컬라이징, Sprite, VFX Asset에 매핑한다.

### 호출 빈도와 검증

- [ ] 같은 Range/Preview를 매 프레임 반복 호출하지 않는다.
- [ ] Command의 `CurrentStage`를 적용하고 중복 `GetSnapshot()`을 피한다.
- [ ] 상태 변경 뒤 관련 Range/Preview 캐시를 버린다.
- [ ] 성능 판단은 Unity Profiler의 호출 빈도, CPU, GC Alloc 실측으로 한다.
- [ ] Line of Sight, 방어력, Buff/Debuff, Timeline 변경 Trait를 View 규칙으로 임시 구현하지 않는다.

이 체크리스트를 만족하면 현재 StageSession의 초기 적·Railway Timeline·배치·이동·공격·Combat Trait를 View가 Core 내부 규칙 없이 연결할 수 있다. Stage Map에서 이 Session으로 들어오는 선택 흐름은 [Stage Map View Handoff](stage-map-view-handoff.md)의 Bootstrap 연결 절을 함께 확인한다.
