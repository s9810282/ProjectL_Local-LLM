# Shared Knockback and Gravekeeper — Unity/View Handoff

> 과거 기록 (2026-10-06 정리): 아래 내용은 Framework/Buffer 전환 전 구현이며 현재 구현 지침이 아닙니다. 삭제된 계획·설계 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있습니다. 현재 구조와 API는 [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)와 실제 코드를 기준으로 확인합니다.

> 이 문서는 구현 계획이 아니라 현재 Core 완료 상태와 Unity/View 후속 작업의 경계를 전달하는 인계 문서다. 여기서 View는 화면 표현만이 아니라 `ProjectL/Unity/**`의 입력, Adapter, Stage 조립, 콘텐츠 SO, 프리팹과 연출을 포함한다.

- 작성일: 2026-09-21
- Core 구현 완료 HEAD: `de9d8edd35eacc31fbaa3da424cfb2c8d9031f24`
- 최종 검증 문서 커밋: `b7d13548922f250ee5f7a3aebe9fd487bdbf9971`
- 구현 계획: Shared Knockback and Gravekeeper Implementation Plan (Git 이력: `2026-09-21-shared-knockback-gravekeeper-implementation-plan.md`)
- 검증 결과: Shared Knockback and Gravekeeper Verification (Git 이력: `2026-09-21-shared-knockback-gravekeeper-verification.md`)
- 상태: **Core 전투 기능과 묘지기 Unit 저작은 완료됐다. 실제 게임 출현, View 입력과 전용 연출은 아직 연결되지 않았다.**

## 1. 인계 결론

Core에는 묘지기를 전투에 넣었을 때 필요한 게임 규칙이 모두 있다.

- `EntityKey.Unit_Gravekeeper = 17`
- 재사용 가능한 `Knockback` Trait와 거리 1 저작
- Method-2 전방 3방향 fan 공격과 명시적 방향 조준
- 살아남은 피격 Unit의 한 칸 강제 이동
- 경계·Unit·Structure·HQ 충돌 시 이동 차단
- 일반 공격 사망, 점유 해제, 강제 이동, ThornField 피해, Timeline 제거와 Outcome의 원자적 정산
- Player/Enemy 공통 규칙과 Enemy AI 실행
- `UnitRoster.asset` 등록

따라서 View 담당은 Knockback 규칙이나 피해 계산을 다시 만들지 않는다. 남은 일은 다음 네 가지다.

1. 묘지기 프리팹과 `UnitVisualCatalog` 등록
2. 실제 Stage의 적 배치 또는 Enemy Loadout에 묘지기 연결
3. `AttackRangeResult.AttackAims`의 방향을 보존하는 Unity 공격 입력 경계
4. 필요 시 강제 이동 전용 표현 이벤트와 넉백 애니메이션 연결

## 2. 이미 저작된 묘지기 데이터

원본 자산은 [Gravekeeper.asset](../../../Assets/DataBase/SO/Stage/UnitTable/Enemy/Unit/Gravekeeper.asset)이다.

| 항목 | 현재 값 |
|---|---:|
| Key | `Unit_Gravekeeper (17)` |
| 표시 이름 | `Gravekeeper` |
| Owner eligibility | `EnemyOnly` |
| Footprint | `1 x 1` |
| Max Health | `10` |
| Physical Defense | `0` |
| Move Range / Cost | `3 / 1` |
| Category | `Rider` |
| Appoint Method | `ForwardLineWithDiagonal (Method 2)` |
| Attack Range | `1` |
| Target Mode | `AllHostilesInArea` |
| Attack Power | `10` |
| Max AP / Attack Cost | `5 / 2` |
| Priority / Delay | `30 / 35` |
| Trait | `Knockback(distanceCells: 1)` |

[UnitRoster.asset](../../../Assets/DataBase/SO/Stage/UnitTable/UnitRoster.asset)에는 묘지기 정의가 한 번 등록돼 있다. 이 등록은 Catalog 변환을 가능하게 할 뿐, 특정 Stage에 묘지기를 생성하지는 않는다.

### Method-2 공격 모양

방향이 `Up`이면 묘지기 앞의 왼쪽 대각선, 정면, 오른쪽 대각선 세 칸이 한 공격 범위다. 각 살아남은 대상은 맞은 가지 방향을 따라 한 칸 더 밀린다.

```text
밀릴 목적지        □  □  □
피격 fan           ■  ■  ■
                   ↖  ↑  ↗
                     G
```

- 공격 범위와 대상 선정은 Core의 `AttackPreview.AffectedCells`와 `Targets`를 사용한다.
- 같은 대각선 대상이 둘 이상의 cardinal fan에 포함될 수 있으므로 방향 없는 조준은 `AmbiguousAttackDirection`으로 거절될 수 있다.
- View가 좌표로 방향이나 Knockback 목적지를 재계산하면 안 된다.

## 3. 현재 Unity 경계에서 바로 막히는 지점

### 3.1 묘지기 View가 없다

[UnitVisualCatalog.asset](../../../Assets/DataBase/SO/Stage/UnitTable/Visual/UnitVisualCatalog.asset)에는 현재 `Unit_Gravekeeper` 항목이 없다. `FieldUnitPresenter`는 `UnitVisualCatalogSO.GetPrefabOrThrow(EntityKey)`를 사용하므로 묘지기가 실제 Snapshot에 나타나면 Visual entry 누락 예외가 발생한다.

### 3.2 실제 출현 경로가 없다

현재 Stage 자산의 `_initialEnemyPlacements`에는 묘지기가 없고, Chapter Enemy Loadout에도 묘지기 참조가 없다. 다음 중 콘텐츠 의도에 맞는 한 경로를 선택해야 한다.

- 고정 배치: `StageDefinitionSO.InitialEnemyPlacements`에 Gravekeeper 정의와 Anchor/Rotation을 저작한다.
- 가중치 출현: 해당 Chapter의 `EnemyLoadoutSO`에 Gravekeeper 정의와 양의 Weight를 추가하고 `SpawnCount` 정책을 유지한다.

둘을 동시에 넣으면 한 Stage에 고정 묘지기와 추첨 묘지기가 함께 나올 수 있다. 의도하지 않았다면 한 경로만 사용한다. Boss 소환, 별도 묘지기 생성 규칙, 플레이어 덱 등록은 이번 인계 범위가 아니다.

### 3.3 현재 Player 공격 입력은 방향 정보를 버린다

Core의 [AttackRangeResult.cs](../../../Assets/Scripts/ProjectL/Core/Contracts/Results/AttackRangeResult.cs)는 `AttackAims`를 공개하고, 각 `AttackAim`에는 대상과 `Direction`이 들어 있다. 그러나 현재 Unity의 다음 경계는 대상 ID만 받는다.

- `IStageCombatSession.PreviewAttack(EntityId, EntityId)`
- `IStageCombatSession.TryAttack(EntityId, EntityId)`
- `StageSessionHost`의 동일한 두 메서드
- `CombatAttackTargeting`의 hover/commit

이 상태에서 대각선 교차 대상은 방향 없는 조준으로 들어가 `AmbiguousAttackDirection`이 될 수 있다. Enemy AI는 이미 `AttackAims`를 사용하므로 영향을 받지 않는다.

### 3.4 전용 Knockback 표현 계약이 없다

공격 성공 후 `AttackResult.CurrentStage`에는 이동이 끝난 최종 Anchor가 들어 있어 `SnapshotChanged` 동기화 자체는 가능하다. 하지만 현재 공개 결과와 Unity 사건에는 다음 정보가 없다.

- 어떤 대상이 강제 이동했는지
- 강제 이동의 `From`과 `To`
- 차단된 대상과 성공한 대상의 구분

`UnitMovedEvent`는 자발 이동과 AP 정산을 표현하는 사건이므로 Knockback에 그대로 재사용하면 안 된다. 현재 상태에서는 Unit이 최종 칸으로 갱신될 수는 있지만, 별도 넉백 이동 연출은 정확히 만들 수 없다.

## 4. Unity/View 후속 구현 범위

### V1. 프리팹과 Visual Catalog

- [ ] 기존 `FieldUnitView` 계약을 따르는 묘지기 프리팹을 만든다.
- [ ] 프리팹에 필요한 Collider, Animator 또는 현재 임시 `EnemyUnitMotion` 구성을 연결한다.
- [ ] `UnitVisualCatalog.asset`에 `Unit_Gravekeeper -> prefab` 항목을 정확히 한 번 추가한다.
- [ ] Key 누락, null prefab, 중복 Key를 EditMode 테스트로 막는다.
- [ ] 외형 선택에 이름 문자열이나 `Rider` Category를 사용하지 않는다. `EntityKey.Unit_Gravekeeper`만 Visual lookup key로 사용한다.

권장 자산 위치는 기존 Stage Unit prefab 관례를 따른다. 새 경로를 만들기 전에 현재 Unit prefab들이 실제로 놓인 폴더와 prefab root 구성을 확인한다.

### V2. Stage 출현 연결

- [ ] 묘지기를 처음 노출할 Stage와 출현 방식을 콘텐츠 담당자와 확정한다.
- [ ] 고정 배치라면 유효한 enemy deployment cell과 cardinal Rotation을 저작한다.
- [ ] 가중치 출현이라면 해당 `EnemyLoadoutSO`에 정의와 Weight를 추가한다.
- [ ] `EnemySpawnPlanner`와 `StageEntryDataBuilder`를 우회해 런타임에서 직접 Entity를 만들지 않는다.
- [ ] `UnitRoster` 등록을 스폰 등록으로 오해하지 않는다.

묘지기는 `EnemyOnly`다. 플레이어 덱/로드아웃에 넣으려면 게임 규칙 변경과 별도 승인이 필요하며, View 편의 때문에 `Both`로 바꾸지 않는다.

### V3. 명시적 AttackAim 입력 연결

Unity 경계가 Core의 이미 존재하는 `AttackAim`을 끝까지 보존하도록 확장한다.

```csharp
AttackPreview PreviewAttack(EntityId attackerId, AttackAim aim);
AttackResult TryAttack(EntityId attackerId, AttackAim aim);
```

권장 연결 순서:

1. `IStageCombatSession`과 `StageSessionHost`에 위 overload를 추가하고 기존 EntityId overload는 기존 호출자용으로 유지한다.
2. `CombatAttackTargeting.Refresh`에서 `AttackRangeResult.AttackAims`를 보관한다.
3. hover한 Entity에 대응하는 aim이 하나면 그대로 Preview한다.
4. 둘 이상이면 방향 선택 UI 또는 현재 입력 방향과 일치하는 aim을 명시적으로 고른다. 임의의 첫 항목을 고르지 않는다.
5. Preview에 사용한 동일한 `AttackAim`을 Commit까지 보존한다.
6. 실패 시 Core의 `AmbiguousAttackDirection`을 그대로 표시한다.

`AttackAim.Direction`은 cardinal `Up/Right/Down/Left`가 정상 입력이다. 대각선 대상이라고 `UpRight` 같은 대각 방향을 새로 넣지 않는다. 대각선 밀림 방향은 선택한 cardinal fan 안에서 Core가 가지별로 해결한다.

### V4. Knockback 표현

기능 연결의 최소 완료 기준은 authoritative Snapshot의 최종 Anchor를 그대로 그리는 것이다. 이 단계에서는 순간 이동처럼 보일 수 있지만 게임 상태는 정확하다.

전용 넉백 모션이 필요하면 Unity 경계에서 별도의 presentation-only 사건을 추가한다.

```csharp
UnitForcedMovedEvent
  EntityId UnitId
  EntityId AttackerId
  CellCoord From
  CellCoord To
```

권장 원칙:

- Core의 Knockback 공식, 충돌 판정 또는 목적지 계산을 Unity에 복사하지 않는다.
- 공격 직전/직후 authoritative Snapshot의 동일 Entity Anchor 차이를 Unity Adapter가 포착해 `From/To`를 만든다.
- 자발 이동 의미와 AP 필드를 가진 `UnitMovedEvent`에는 합치지 않는다.
- `StageSessionHost`의 기존 순서인 Core 명령 → `SnapshotChanged` → 결과 사건 발행을 보존한다.
- 사건 수신 후 `FieldUnitPresenter`가 EntityId로 View를 찾아 `From -> To`를 재생한다. 바깥 Presenter가 View/Animator를 직접 검색하지 않는다.
- 다음 턴까지 기다려야 하는 모션이면 `CombatPresentationGate`를 잡고, 모션 종료 또는 시작 실패 시 반드시 release한다.
- 차단된 Knockback에는 이동 사건을 발행하지 않는다.
- 공격으로 죽은 Unit은 기존 `UnitDiedEvent`와 death hold 경로를 유지한다.

공격 전후 Snapshot 비교로 충분하지 않은 연출 정보가 실제로 필요해질 때만 Core 공개 DTO 변경을 별도 협의한다. 이번 Core 완료 범위에는 public displacement DTO가 없으며, View 작업 중 조용히 새 전투 규칙을 추가하지 않는다.

## 5. View가 재구현하면 안 되는 규칙

- Knockback은 묘지기 전용 분기가 아니라 재사용 Trait다.
- 현재 runtime 지원 거리는 1이다. 2 이상은 쓰기 전에 거절된다.
- 최종 피해가 0이어도 살아 있으면 밀릴 수 있다.
- 일반 공격 피해로 먼저 죽은 대상은 밀리지 않는다.
- 목적지가 맵 밖이거나 Unit, Structure, HQ가 점유하면 피해만 받고 이동은 차단된다.
- chain push, collision damage, partial push는 없다.
- 강제 이동은 이동 AP, 자발 이동 checkpoint, Rider 환급, Heavy 규칙을 발동하지 않는다.
- 강제 이동 도중 밟은 ThornField는 저장된 피해를 주고 소비될 수 있으며, 그 결과 죽을 수 있다.
- 사망, 점유, Timeline, Outcome은 공격 전체와 함께 Core가 확정한다.
- Preview와 Command의 범위·피해·최종 HP는 Core 결과를 표시한다. View에서 예상 피해를 다시 계산하지 않는다.
- Cowboy, Boss necromancy, Boss distance-3 Knockback은 구현되지 않았다.

## 6. 권장 파일 범위

실제 코드 구조를 다시 확인한 뒤 최소 범위로 작업한다.

| 목적 | 우선 확인할 파일 |
|---|---|
| 프리팹 lookup | `Assets/DataBase/SO/Stage/UnitTable/Visual/UnitVisualCatalog.asset`, `Unity/Data/ScriptableObjects/Unit/UnitVisualCatalogSO.cs` |
| Stage 출현 | `StageDefinitionSO.cs`, `EnemyLoadoutSO.cs`, `EnemySpawnPlanner.cs`, `StageEntryDataBuilder.cs`, Chapter Enemy Loadout assets |
| 방향 입력 | `Runtime/Stage/IStageCombatSession.cs`, `StageSessionHost.cs`, `View/Combat/CombatAttackTargeting.cs` |
| 결과 사건 | `Adapter/Stage/StageResultEventPublisher.cs`, `Runtime/Stage/Events/CombatDomainEvents.cs`, `CombatEventRelay.cs` |
| Unit 표현 | `View/Field/FieldUnitPresenter.cs`, `FieldUnitView.cs`, `View/Animation/EnemyUnitMotion.cs` |
| 연출 대기 | `CombatPresentationGate`와 [combat-presentation-hooks.md](../viewDocs/combat-presentation-hooks.md) |

Core의 `UnitAttackService`, `ForcedDisplacementPlan`, map/timeline/outcome 서비스는 View 연결을 위해 수정하지 않는다. 공개 경계에서 실제 결함이 확인될 때만 재현 테스트와 함께 Core 담당에게 반환한다.

## 7. 최소 검증 기준

중복 테스트를 늘리지 말고 아래 위험 경계를 대표 사례로 검증한다.

### 자동 검증

- [ ] Visual Catalog가 `Unit_Gravekeeper` prefab을 반환하고 기존 Unit lookup을 보존한다.
- [ ] 선택한 Stage 준비 결과에 묘지기가 실제 enemy combat entity로 존재한다.
- [ ] Method-2 대각선 교차 대상에서 여러 `AttackAims`를 보존하며, 선택한 방향의 Preview와 Command가 일치한다.
- [ ] 방향을 생략한 모호한 입력은 성공으로 위장하지 않고 `AmbiguousAttackDirection`을 노출한다.
- [ ] 성공한 Knockback은 최종 Snapshot의 Anchor에 반영되고, 차단/일반 사망 대상은 이동 연출을 만들지 않는다.
- [ ] Enemy AI 묘지기가 실제 Method-2 공격을 수행하고 View 사건 변환이 예외 없이 끝난다.
- [ ] 기존 단일 방향 공격 Unit의 hover/commit 흐름이 그대로 동작한다.

### 수동 Play 검증

- [ ] 묘지기 프리팹이 올바른 위치·회전·크기로 생성된다.
- [ ] 전방 세 칸이 같은 공격으로 표시되고, 선택 방향과 실제 공격 범위가 일치한다.
- [ ] 왼쪽 대각선/정면/오른쪽 대각선 대상이 각 가지 방향으로 밀린다.
- [ ] 벽, Unit, Structure/HQ에 막히면 대상이 제자리에 남는다.
- [ ] Knockback/피격/사망 연출 동안 다음 입력·AI 턴이 조기에 진행되지 않는다.
- [ ] 가시밭 위로 밀려 죽는 경우 최종 위치, HP, 사망 연출과 승패 UI가 Core Snapshot과 일치한다.

## 8. Core 검증 증거

Unity 연결 전에 완료된 Core 검증은 다음과 같다.

- 집중 fixture 6개: `42/42` 통과
- 기존 회귀 fixture 36개: `452/452` 통과
- 두 실행 합계: `494` 실행, 실패·skip·inconclusive·컴파일 오류 0
- 최종 셀프 리뷰: 새 Critical/Important/Minor finding 없음

이는 Core 규칙과 SO round-trip 검증이다. 묘지기 prefab, Visual Catalog, Stage 출현, Player 방향 선택 UI와 실제 Play 연출이 완료됐다는 뜻은 아니다.

## 9. 완료 보고 형식

View 담당은 완료 시 다음을 구분해 보고한다.

1. 연결한 prefab과 `UnitVisualCatalog` 항목
2. 묘지기를 출현시킨 Stage/Enemy Loadout과 선택한 출현 방식
3. `AttackAim` 방향을 보존한 Unity API 및 UI 흐름
4. Knockback 표현 사건을 추가했는지, 아니면 최종 Snapshot 동기화만 사용했는지
5. 자동 테스트 결과와 실제 Unity Play 확인 결과
6. 수정한 Unity/asset/prefab/scene 파일 목록
7. 남은 제한 또는 Core에 반환할 실제 재현 결함

이 체크리스트가 끝나기 전에는 “묘지기를 실제 게임에서 사용할 수 있다”고 완료 판정하지 않는다. 현재 판정은 **Core-ready, View/content integration pending**이다.
