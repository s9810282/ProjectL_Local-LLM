# 배치 Drag 상호작용 가이드

이 문서는 Deployment 단계의 Drag 상호작용에 2026-08-22 ~ 08-23에 추가된 기능과 그 경계를 정리한다. `feature/Yonggeun/Combat` 브랜치 작업분이다.

작업 전에는 Drag 중 시각 피드백이 "커서가 닿은 Cell의 Footprint만 색이 바뀌는 것"뿐이었고, 한번 배치한 Unit은 되돌릴 수 없었다. 이번 작업으로 네 가지가 추가됐다.

1. Drag 중 **맵 전체**가 배치 가능/불가로 구분되어 표시된다.
2. Drag 중 Unit 모형(Ghost)이 커서를 따라다니고, 그동안 원본 View는 숨는다.
3. Field에 배치한 Unit을 다시 집어 **재배치**하거나 **Hand로 되돌릴** 수 있다.
4. Stage 시작 시 적을 Field에 랜덤 스폰한다. 기획 미확정이라 임시 구현이며 10절에서 따로 다룬다.

Core 계약은 바꾸지 않았다. 추가된 것은 전부 `ProjectL.Unity` 어셈블리 안이며, Core는 기존 `PreviewCombatPlacement` / `TryPlaceOnCombat` / `TryReturnToReserve`를 그대로 사용한다.

## 1. Drag 처리 흐름

Drag 상태를 소유하는 곳은 `PlacementInteractionPresenter` 하나다. 입력 계층은 상태를 갖지 않고 의도만 전달한다.

```text
PlayerInputActions.inputactions  (Deployment 액션맵)
  Press: <Pointer>/press,  interactions = Press(behavior=Release Only)
  PointerPosition: <Pointer>/position
  Cancel: <Mouse>/rightButton
        ↓
PlacementInput.PlacementController          입력 phase → 의도 번역
  Started   → TryBeginPlacementAtPointer    (누름 = 집기)
  Vector2   → UpdatePointerPosition         (이동)
  Performed → ConfirmPlacement              (뗌 = 놓기)
  Canceled  → CancelPlacement
        ↓  IPlacementInputTarget
InputInstaller                              null-safe 포워딩
        ↓
PlacementInteractionPresenter               ★ Drag 상태 머신
        ├→ HandUnitPicker      스크린 → EntityId (손패)
        ├→ FieldCellPicker     스크린 → CellCoord / Cell 중심 / 평면 좌표
        ├→ HandDropZone        스크린 → 손패 영역 안인지
        ├→ StageSessionHost    Core 질의 및 명령
        ├→ FieldPlacementHighlight   Board / Hover 표시
        ├→ PlacementGhostPresenter   Ghost 표시
        ├→ FieldUnitPresenter        Field 원본 숨김
        └→ HandPresenter             Hand 원본 숨김
```

`Press`에 `behavior=Release Only`가 걸려 있어 누를 때 `Started`, 뗄 때 `Performed`가 온다. 즉 누른 채 끌다가 떼는 진짜 Drag다.

## 2. 배치 가능 영역 전체 표시

### 표시 구조

Cell 색은 두 층으로 나뉜다.

| 층 | 갱신 시점 | 색 |
| --- | --- | --- |
| Board | Drag 시작 시 1회 | 배치 가능 / 불가 |
| Hover | 커서가 Cell을 넘을 때 | 커서가 가리키는 Footprint |

Hover 층은 Board 층 위에 덧칠된다. 커서가 Cell을 벗어나면 그 Cell은 Board 색으로 **복원**된다.

### FieldPlacementMap이 필요한 이유

복원할 "밑바닥 색"을 알아야 하므로, Drag 시작 시 계산한 Cell별 배치 가능 여부를 들고 있어야 한다. 그 조회표가 `FieldPlacementMap`이다.

```csharp
public readonly struct FieldPlacementMap
{
    public int Width { get; }
    public int Height { get; }
    public bool Contains(CellCoord cell);
    public bool IsPlaceable(CellCoord cell);
}
```

이 조회표가 없으면 선택지는 둘뿐이다. 커서가 Cell 경계를 넘을 때마다 보드 전체를 다시 칠하거나, Board 표시를 포기하거나.

### 비용

`PreviewCombatPlacement`는 상태를 바꾸지 않는 순수 조회이므로 전체 Cell 스캔이 안전하다. Footprint와 Map 점유 상태는 Drag 도중 바뀌지 않으므로 **Drag 시작 시 한 번만** 계산한다.

기존 구현은 `Show()` 첫 줄에서 `ClearCellHighlights()`로 보드 전체를 밀고 Footprint만 다시 칠했다. 즉 이미 "Cell을 넘을 때마다 W×H 순회"였다.

| Hover Cell이 바뀔 때 | 기존 | 현재 |
| --- | --- | --- |
| 셀 경계를 넘을 때마다 | W×H 초기화 + N 칠하기 | N 복원 + N 칠하기 |
| Drag 시작 시 | 없음 | W×H 칠하기 (1회) |

N은 Footprint Cell 수다. 전체 순회가 매 Hover에서 Drag당 1회로 옮겨갔으므로 오히려 가벼워졌다.

### 배치 불가 판정에 포함되는 것

`EntityDeploymentService.PreviewCombatPlacement`가 판단하며 `DeploymentZonePolicy`도 포함된다. 따라서 아군 배치 구역 밖은 전부 불가로 표시된다.

## 3. Drag Ghost

`PlacementGhostPresenter`가 Drag 중인 Unit의 모형을 커서 위치에 띄운다.

| 커서 위치 | 동작 |
| --- | --- |
| Cell 위 | Cell 중심으로 스냅. 하이라이트 위치와 일치 |
| Grid 밖 (Field 평면 위) | 평면을 따라 자유 이동, 불가 색 |
| 평면과 만나지 않음 | 위치 유지 |

색은 배치 가능/불가에 따라 하이라이트와 동기화된다.

### Drag 중 원본 View 숨김

Ghost가 커서를 따라다니는 동안 원본 View가 그대로 남아 있으면 같은 Unit이 두 곳에 보인다. Drag 출처와 무관하게 원본을 숨긴다.

| Drag 출처 | 숨기는 대상 |
| --- | --- |
| Hand | `HandPresenter` — 손패 View |
| Field | `FieldUnitPresenter` — Field View |

두 Presenter에 같은 API를 두었다.

```csharp
public void SetHiddenEntity(EntityId entityId);
public void ClearHiddenEntity();
```

이는 Snapshot 이력을 기록해 되돌리는 문제가 아니다. Drag 중에는 Core 상태가 아예 바뀌지 않으므로(확정 시점에만 명령을 보낸다) 되돌릴 것이 없다. Presenter가 Snapshot만 보고 그리기 때문에 "이 Entity는 지금 다른 곳에서 표현 중"이라는 사실을 모를 뿐이며, 그것만 알려주면 된다.

두 Presenter는 Drag를 알지 못한다. "이 Entity는 지금 숨김"이라는 표시 상태만 안다. 사망 연출이나 컷신에도 그대로 쓸 수 있는 일반적인 View 기능이며 단방향 View→View 의존이다.

`ConfirmPlacement`는 `CancelPlacement()`를 먼저 호출한 뒤 Core 명령을 보내므로, 숨김 해제가 `SnapshotChanged` → `Render`보다 앞선다. 각 경로는 다음과 같이 정리된다.

| 경로 | 결과 |
| --- | --- |
| 재배치 성공 | 숨김 해제 → Render → 새 위치에 표시 |
| Hand로 되돌리기 | 숨김 해제 → Render → Field에서 제거, Hand에 등장 |
| 빈 Slot이 없어 실패 | 숨김 해제 → Render 없음 → 원위치 즉시 복귀 |
| 우클릭 취소 | 숨김 해제 → Render 없음 → 원위치 즉시 복귀 |

`ApplyHiddenState()`가 `SetActive`를 직접 호출하므로 Render를 기다리지 않는다. 마지막 두 경로가 이 덕에 성립한다.

Hand에서 집은 경우 숨긴 자리는 **비워 둔다.** `LayoutViews()`는 `Render` 때만 돌고 Drag 중에는 Snapshot이 바뀌지 않으므로 자연히 그렇게 된다. 자리를 메우면 Drag를 시작하는 순간 손패가 한 칸 밀렸다가 취소 시 다시 벌어져 흔들린다.

부수 효과로 `SetActive(false)`는 `HandUnitSelectable`의 Collider도 함께 끈다. Drag 중인 Unit을 다시 집는 것이 물리적으로도 불가능해진다.

### 구현 방식과 그 이유

현재는 **Prefab을 Instantiate해서 Prefab별로 캐시**한다. `SkinnedMeshRenderer`와 `Animator`가 살아 있으므로 리깅 모델이 들어오면 Ghost도 애니메이션이 재생된다.

캐시 키는 `EntityKey`가 아니라 **Prefab 참조**다. 현재 `UnitVisualCatalog`의 4개 엔트리가 전부 같은 Prefab을 가리키므로 인스턴스는 1개만 생긴다. 나중에 Prefab이 갈라지면 자동으로 종류별로 나뉜다.

Ghost는 표시 전용이므로 생성 시 Collider를 모두 끄고 Layer 0으로 내린다. `HandUnitPicker`가 Layer 8을 물리 Raycast하므로, 이를 하지 않으면 Ghost가 자기 자신을 다시 집는다.

### 검토했다가 보류한 방식

Prefab을 Instantiate하지 않고 Mesh와 Material 참조만 갈아끼우는 방식을 구현해 봤다. Unit 종류가 늘어도 Ghost 오브젝트가 1개로 유지되는 장점이 있다.

하지만 `SkinnedMeshRenderer`는 **Mesh만으로 그릴 수 없다.** 렌더링에 `bones`(Transform 배열)와 `rootBone`이 필요하고, Mesh의 bindpose가 그 스켈레톤과 맞아야 한다. 모든 캐릭터에 애니메이션이 들어갈 예정이므로 이 방식은 그대로는 성립하지 않는다.

현재 `BaseModel.fbx`는 임시 에셋이며 스킨 데이터가 없다(`Deformer` / `Cluster` / `LimbNode` / `BindPose` 모두 없음). 리깅된 실제 모델이 없어 검증이 불가능하므로 보류했다.

리깅 모델이 확정되면 선택지는 다음과 같다.

| 방식 | 전제 | 결과 |
| --- | --- | --- |
| 공용 리그 + Mesh 스왑 | 모든 Unit이 한 스켈레톤 공유 | 가장 가볍고 애니메이션도 됨 |
| `BakeMesh` | 없음 | 리그가 제각각이어도 동작, 정지 포즈. Prefab별 구운 Mesh 1개 캐시 |
| 현재 방식 유지 | 없음 | 애니메이션 그대로, Unit 종류만큼 인스턴스 증가 |

공용 리그는 Ghost 때문이 아니라 그 자체로 이득이 크다(애니메이션 클립 재사용, 리타게팅 불필요). 아트 작업 시작 전에 정하는 편이 좋다.

공개 API(`Show` / `MoveTo` / `SetPlaceable` / `Hide` / `Release`)는 어느 방식에서도 동일하므로 내부만 교체하면 호출부는 손대지 않아도 된다.

## 4. Field Unit 되돌리기와 재배치

### 동작

| Drag 출처 | Drop 위치 | 결과 |
| --- | --- | --- |
| Hand | Cell | 배치 (`TryPlaceOnCombat`) |
| Field | Cell | 재배치 (`TryPlaceOnCombat`, Core가 Combat→Combat 이동 지원) |
| Field | Hand 영역 | 되돌리기 (`TryReturnToHand`) |
| 그 외 | 어디든 | 취소 |

`PlacementInteractionPresenter`가 `PlacementOrigin`(Hand / Field)을 추적한다. Field에서 집은 Unit만 Hand로 되돌릴 수 있다.

Field Unit 집기는 Cell 점유 기준이다. `StageSessionHost.TryGetPlayerCombatUnitAt`이 Snapshot의 Entity를 훑어 해당 Cell을 점유한 살아있는 Player Unit을 찾는다. Collider에 의존하지 않는다.

### 재화 환급이 붙을 자리

되돌릴 때 배치 비용을 환급하는 규칙이 추후 필요하다. 이를 위해 **View에는 비용/환급 지식을 넣지 않았다.**

```text
PlacementInteractionPresenter        "이 Unit을 Hand로" — 의도만 표현
        ↓
StageSessionHost.TryReturnToHand()   Slot 선택 등 어댑터 기본값
        ↓
StageSession.TryReturnToReserve()    ★ 환급 규칙이 들어갈 자리
        ↓
DeploymentResult(Succeeded, CurrentStage)
```

환급이 생기면 Core 안쪽에서 처리하고 Snapshot에 실어 보내면 된다. 재화 UI는 다른 Presenter처럼 `SnapshotChanged`를 구독하면 되고, `PlacementInteractionPresenter`는 바뀌지 않는다.

이를 위해 지킨 규칙 두 가지다.

- **Slot 선택을 View가 하지 않는다.** `TryReturnToReserve`는 Slot 인덱스를 요구하지만, `PlacementInteractionPresenter`는 그것을 모른다. `StageSessionHost`가 Snapshot에서 빈 Slot을 찾아 채운다.
- **View가 성공 여부를 미리 판단하지 않는다.** "환급 가능한가", "Slot이 있나" 같은 판단을 View에서 하면 규칙이 두 곳으로 갈라진다. 전부 Core가 결정하고 `DeploymentResult`로 답한다.

다만 현재 Slot 선택은 `StageSessionHost`, 즉 **Unity 어댑터 계층**에 있다. 되돌리기 트랜잭션이 두 계층에 걸쳐 있는 상태이므로 8절의 후속 과제를 함께 볼 것.

### Hand Drop 영역 판정

Collider를 쓰지 않는다. Field가 이미 쓰는 방식과 같이 평면 교차 + 로컬 좌표 범위 검사로 처리한다.

| | 기하 소유 | 카메라 어댑터 |
| --- | --- | --- |
| Field | `FieldRoot.TryRayToFieldPlane` | `FieldCellPicker` |
| Hand | `HandRoot.TryRayToHandPlane` | `HandDropZone` |

`HandFieldAnchor`가 Hand를 Field 평면 위에 올리고 Field를 향해 회전시키므로, Hand는 평면 위의 회전된 사각형이다. `InverseTransformPoint`로 로컬 좌표를 얻으면 회전이 상쇄되어 범위 검사 한 줄로 끝난다.

영역 크기는 `HandRoot._dropZoneSize`로 **고정**한다. 슬롯 수에서 유도하지 않는 이유는 두 가지다.

- Hand가 비면 영역이 0이 되어 마지막 Unit을 되돌릴 수 없다.
- Unit을 하나씩 되돌릴 때마다 목표가 줄어들어 조작감이 나빠진다.

`HandRoot`를 선택하면 씬 뷰에 영역이 기즈모로 표시된다.

## 5. 추가·변경된 스크립트

### 신규

| 스크립트 | 하는 일 |
| --- | --- |
| `View/Field/FieldPlacementMap.cs` | Cell별 배치 가능 여부 조회표 |
| `View/PlacementPreview/PlacementGhostPresenter.cs` | Drag Ghost 생성·이동·틴트·정리 |
| `View/HandQueue/HandDropZone.cs` | 화면 좌표가 Hand 영역 안인지 검사 |
| `Data/ScriptableObjects/Unit/EnemyLoadoutSO.cs` | 적 편성 (임시, 10절) |
| `Data/Runtime/EnemySpawnPlan.cs` | 스폰할 적 목록과 Cell 후보 (임시, 10절) |
| `Runtime/Stage/EnemySpawnPlanner.cs` | EnemyLoadoutSO → EnemySpawnPlan (임시, 10절) |

### 변경

| 스크립트 | 변경 내용 |
| --- | --- |
| `View/Field/FieldPlacementHighlight.cs` | `Show` → `ShowBoard` / `ShowHover` / `ClearHover` / `Clear`로 분리 |
| `View/Field/FieldRoot.cs` | `TryRayToFieldPlane` 추출, `ClearCellHighlight(CellCoord)` 추가 |
| `View/Field/FieldRuntime.cs` | `ClearCellHighlight(CellCoord)` 추가 |
| `View/Field/FieldCellHandle.cs` | Renderer마다 `IColorHandle` 소유 (11절) |
| `View/Field/FieldCellPicker.cs` | Cell 중심 좌표 오버로드, `TryPickFieldPoint` 추가 |
| `View/Field/FieldUnitPresenter.cs` | `SetHiddenEntity` / `ClearHiddenEntity` |
| `View/HandQueue/HandRoot.cs` | `TryRayToHandPlane`, `ContainsWorldPoint`, 영역 기즈모 |
| `View/HandQueue/HandPresenter.cs` | `SetHiddenEntity` / `ClearHiddenEntity` |
| `View/PlacementPreview/PlacementInteractionPresenter.cs` | Board 스캔, Ghost 연동, Drag 출처 추적, Drop 분기, 원본 숨김 |
| `Runtime/Stage/StageSessionHost.cs` | `TryGetEntitySnapshot` / `TryGetPlayerCombatUnitAt` / `TryReturnToHand` / `SpawnInitialEnemies` |
| `Data/ScriptableObjects/Stage/StageSetupSO.cs` | `EnemyLoadout` 필드 추가 |
| `Data/Runtime/StageBootstrapData.cs` | `EnemySpawn` 추가 (기존 생성자 유지) |
| `Runtime/Stage/StageBootstrapBuilder.cs` | `EnemyLoadoutSO`를 받는 오버로드 추가 |
| `Runtime/Stage/StageBootstrapper.cs` | `StageSetup.EnemyLoadout` 전달 |
| `View/Field/FieldRoot.cs` | `RenderSystemTestCode` 참조, ColorSystem 서비스 조회 (11절) |
| `View/Field/FieldRuntime.cs` | `IColorSystemService` 전달 (11절) |
| `View/Field/FieldCellPool.cs` | `IColorSystemService` 전달 (11절) |

Framework 어셈블리에도 파일이 추가·변경됐다. 11절 참고.

| 스크립트 | 변경 내용 |
| --- | --- |
| `Framework/RunTimeSystems/RenderSystem/RenderSystemTestCode.cs` | 신규. Render 전용 Runtime 부트스트랩 |
| `Framework/LodingSystems/DataBaseSystem/Core/DatabaseCatalogSO.cs` | `DatabaseDataSO.cs`에서 이름 변경 |

## 6. 함께 고친 것

### Drag가 걸린 채 남던 문제

`ConfirmPlacement`가 Grid 밖에서 놓았을 때 `false`만 반환하고 Drag 상태를 풀지 않았다. `Press(behavior=Release Only)`라 Release 후 `Canceled`가 오지 않으므로 Drag가 그대로 남았다. Board 하이라이트와 Ghost가 화면에 남는 증상의 원인이기도 하다.

지금은 Release가 곧 Drag의 끝이므로 어디에 놓든 상태를 정리한다.

### MaterialPropertyBlock 생성 위치

`PlacementGhostPresenter`에서 `static readonly MaterialPropertyBlock`을 필드 초기화로 만들었다가 다음 예외로 죽었다.

```text
UnityException: CreateImpl is not allowed to be called from a MonoBehaviour
constructor (or instance field initializer), call it in Awake or Start instead.
```

MonoBehaviour 타입의 필드 초기화 시점에는 네이티브 객체를 만들 수 없다. 지연 생성으로 해결했다.

```csharp
private static MaterialPropertyBlock _sharedPropertyBlock;

private static MaterialPropertyBlock SharedPropertyBlock =>
    _sharedPropertyBlock ??= new MaterialPropertyBlock();
```

`FieldCellHandle`에도 한동안 같은 패턴이 있었다. MonoBehaviour가 아닌 순수 클래스라 문제되지 않았고, 지금은 11절의 전환으로 `MaterialPropertyBlock`을 직접 다루지 않는다.

### Ghost 정리 경로

`PlacementGhostPresenter`에 `Release()`와 `OnDestroy` 연결을 추가했다. 처음에는 파괴 경로가 없어 Ghost 인스턴스가 GameObject가 죽을 때까지 남았다.

## 7. Unity 씬 연결

다음 네 가지가 필요하다.

**1. PlacementGhostPresenter**

컴포넌트를 추가하고 `Unit Visual Catalog`에 `FieldUnitPresenter`가 쓰는 것과 같은 SO를 연결한다. `Ghost Material`은 선택 사항이다.

`MaterialPropertyBlock`으로 `_BaseColor` / `_Color`를 덮어쓰므로, URP Lit의 Opaque Material은 알파를 무시해 색만 바뀌고 반투명하게 보이지 않는다. 반투명 Ghost를 원하면 Surface Type = Transparent인 Material을 만들어 `Ghost Material`에 넣는다.

**2. HandDropZone**

컴포넌트를 추가하고 `Hand Root`와 `World Camera`를 연결한다. `HandRoot`의 `Drop Zone Size`로 크기를 맞추되, **Field Grid와 겹치지 않게 한다.** `ConfirmPlacement`는 Hand 판정을 먼저 보므로 겹친 구역에서는 배치가 되지 않는다.

**3. PlacementInteractionPresenter**

새로 생긴 참조 네 개를 연결한다.

| 필드 | 연결 대상 |
| --- | --- |
| `Placement Ghost Presenter` | 1번에서 추가한 컴포넌트 |
| `Hand Drop Zone` | 2번에서 추가한 컴포넌트 |
| `Field Unit Presenter` | `FieldRoot`에 붙어 있는 것 |
| `Hand Presenter` | 씬의 `HandPresenter` |

넷 다 **필수**다. 프로젝트의 다른 Presenter와 동일하게 `ValidateReferences()`가 검사하며, 빠뜨리면 Drag 시작 시 어느 참조가 없는지 명시하며 예외를 던진다.

**4. RenderSystemTestCode와 FieldRoot**

GameObject를 만들어 `RenderSystemTestCode`를 붙이고 `Database`에 `Assets/DataBase/SO/DataBaseSO.asset`을 연결한다. 그 다음 `FieldRoot`의 `Render Bootstrap`에 그 컴포넌트를 연결한다. Cell 하이라이트가 여기에 의존하므로 둘 다 필수다. 자세한 내용은 11절.

참조 사용 정책은 경로에 따라 나뉜다.

| 경로 | 방식 |
| --- | --- |
| Drag 시작 | `ValidateReferences()` — 없으면 throw |
| Drag 진행 | 직접 호출 |
| 정리 / 종료 | `?.` — 생명주기 방어 |

정리 경로의 `?.`는 필수/선택 구분이 아니다. `CancelPlacement`는 `OnDisable`과 `StageBootstrapper.ApplyStagePhase(Combat)`에서도 불리는데, 이는 `ValidateReferences`를 거치지 않고 씬 파괴 중일 수도 있는 경로다. 기존 `_fieldPlacementHighlight`도 필수 참조이면서 이 경로에서는 `?.`를 쓴다.

## 8. 남은 이슈

### View 쪽

**Ghost의 스킨드 메시 대응.** 3절 참조. 리깅 모델 도입 시 결정한다. 현재 방식으로도 애니메이션은 정상 재생되므로 급한 사안은 아니며, 공용 리그 여부를 아트 작업 시작 전에 정하는 것이 핵심이다.

### Core 쪽 후속 과제

아래 두 가지는 Core 담당자의 판단이 필요해 이번 작업에서 손대지 않았다. View는 현재 구조로도 정상 동작하지만, 계층 경계가 흐려진 지점이므로 기록해 둔다.

**1. Reserve Slot 선택 정책이 Unity 어댑터에 있다.**

`StageSessionHost.TryReturnToHand`가 Snapshot에서 빈 Slot을 찾아 `StageSession.TryReturnToReserve(entityId, slotIndex)`에 넘긴다. 그런데 "어느 Slot으로 되돌릴 것인가"는 리터럴 기본값이 아니라 **정책**이다. 원래 있던 Slot으로 복귀할지, 정렬을 유지할지에 따라 바뀔 수 있다.

4절에 적은 환급 확장 지점과도 어긋난다. 환급 규칙이 `StageSession.TryReturnToReserve` 안에 들어가면 되돌리기 트랜잭션이 Core와 어댑터 두 곳에 걸치게 된다. Core가 나중에 "원래 Slot으로 복귀"를 원하면 어댑터가 방해한다.

제안: `StageSession.TryReturnToReserve(EntityId)` 오버로드를 Core에 추가해 Slot 선택까지 Core가 맡는다. 기존 시그니처를 남기는 가산적 변경이라 충돌 위험이 낮다.

**2. Cell 점유 조회를 View가 Snapshot에서 재유도한다.**

`StageSessionHost.TryGetPlayerCombatUnitAt`은 전체 Snapshot을 빌드한 뒤 Entity를 훑으며 `OccupiedCells`를 비교한다. 그러나 `StageMapState.GetOccupantId(cell)`이 이미 존재하며 맵이 점유를 직접 소유한다.

Drag 시작 한 번에 `GetSnapshot()`이 최대 3회 호출된다(`BuildPlacementMap`, `TryGetPlayerCombatUnitAt`, `TryReturnToHand`). `StageSnapshotBuilder.Build()`는 매번 전체 Entity를 정렬·복사하고 각 `EntitySnapshot`이 `OccupiedCells` 배열을 다시 복사한다.

프레임 문제는 아니다. Drag 시작은 드물다. 다만 **View가 전체 Snapshot을 범용 질의 수단으로 쓰기 시작했다는 신호**이며, Core에는 `TryGetEntitySnapshot` 같은 좁은 API가 이미 있다.

제안: Cell 점유 조회를 Core에 노출한다. 현재 `StageSession`은 맵을 직접 들고 있지 않고 `EntityDeploymentService`가 소유하므로 경유 방법은 Core 담당자가 정하는 편이 좋다.

## 9. 검증 상태

`ProjectL.Unity`와 `ProjectL.Tests.CombatViewBoundary` 모두 경고 0, 오류 0으로 빌드된다.

플레이 검증은 씬 연결이 끝난 뒤에 수행한다. 7절의 연결이 완료되기 전까지 Ghost, 되돌리기, 원본 숨김은 동작하지 않는다.

## 10. 적 스폰 (임시)

Stage 시작 시 적을 Field에 랜덤 스폰한다. **적 편성 기획이 확정되기 전의 임시 구현**이며, 확정 후에는 배치 패턴·웨이브·난이도 스케일링 등이 이 절의 구성 요소를 대체할 가능성이 높다.

### 구조

```text
StageSetupSO
  └ Enemy Loadout   (비워두면 스폰하지 않는다)
        ↓
StageBootstrapBuilder.Build(field, roster, playerLoadout, enemyLoadout)
        ↓
EnemySpawnPlanner   가중치 추첨 + 적 구역 Cell 섞기 (Seed 고정)
        ↓
EnemySpawnPlan { Units[], CandidateCells[] }
        ↓
StageSessionHost.Initialize
   1. SpawnInitialEnemies    ← 반드시 먼저
   2. Player 손패 생성
```

기존 `StageBootstrapBuilder.Build(field, roster, playerLoadout)`와 `StageBootstrapData`의 3인자 생성자는 그대로 남겼다. 적 편성을 넘기지 않으면 `EnemySpawnPlan.Empty`가 쓰인다.

### Player 편성과 다른 점

| | Player | Enemy |
| --- | --- | --- |
| 뽑는 방식 | Deck(`Count`만큼 복사) 셔플 후 앞에서 N장 | 가중치 추첨, 매번 다시 뽑음 (중복 허용) |
| 도착지 | Reserve 슬롯(손패) | Field 직접 |
| 개수 | `StartingHandSize` | `SpawnCount` |

Deck이 아니라 추첨인 이유는 적이 "손에 든 카드"라는 개념을 갖지 않기 때문이다. 같은 종류가 여러 기 나올 수 있다.

### Cell을 미리 확정하지 않는다

`EnemySpawnPlanner`는 적 배치 구역의 Cell을 전부 모아 섞은 **후보 목록**만 넘긴다. 실제 배치는 `TryPlaceOnCombat`을 후보 순서대로 시도해 처음 성공한 곳에 놓는다.

Footprint 크기, 겹침, 구역 판정은 모두 Core의 배치 규칙이 소유한다. View가 이를 다시 계산하면 규칙이 두 곳으로 갈라지므로, 시도만 하고 판단은 Core에 맡긴다.

### 우회 — Reserve Slot 경유

`StageSessionHost.SpawnInitialEnemies`는 우회다. Core의 Unit 생성 입구가 `StageSession.CreateReserveUnit(key, owner, slotIndex)` 하나뿐이라 **Field에 바로 스폰할 수 없다.** 그래서 Reserve Slot 하나를 경유지로 돌려쓴다.

```text
CreateReserveUnit(key, Owner.Enemy, slot 0)   Slot 0 점유
        ↓
TryPlaceOnCombat(id, cell, R0)                Field 배치 + Slot 0 반납
        ↓
다음 적도 Slot 0을 재사용
```

대가로 **순서 의존성**이 생겼다. 적 스폰이 Player 손패 생성보다 반드시 먼저여야 한다. 현재 데이터가 `_reserveCapacity: 4`, `_startingHandSize: 4`라 손패 생성 이후에는 빈 Slot이 하나도 없기 때문이다.

Core에 Field 직접 스폰 API가 생기면 이 우회는 통째로 사라진다. 8절의 Core 후속 과제와 함께 볼 것.

### 실패 처리

후보 Cell을 전부 시도해도 배치하지 못하면 조정 방법을 안내하며 예외를 던진다.

현재 데이터 기준으로 맵은 9 × 9, `EnemyDeploymentDepth`는 2이므로 적 구역은 **7 ~ 8행의 18칸**이다. `Spawn Count`가 이를 넘거나 Footprint가 커서 들어가지 않으면 실패한다.

### Unity 작업

1. `Create > ProjectL > Unit > Enemy Loadout`으로 SO를 만든다.
2. `Entries`에 Unit Definition과 가중치를 넣는다. **`UnitRoster`에 있는 Unit이어야 한다.** 없으면 어느 항목이 문제인지 명시하며 예외를 던진다.
3. `Spawn Count`와 `Random Seed`를 설정한다.
4. `StageSetup` SO의 `Enemy Loadout` 필드에 연결한다. 비워두면 적을 스폰하지 않는다.

## 11. Cell 하이라이트의 ColorSystem 전환

Cell 색 변경을 `MaterialPropertyBlock` 직접 조작에서 Framework의 `ColorSystem`으로 옮겼다.

### 왜 옮겼나

View가 팀의 렌더 경로를 두고 따로 돌고 있었다. `_BaseColor` 문자열이 `FieldCellHandle`과 `PlacementGhostPresenter` 두 곳에 중복돼 있었고, 프로퍼티 키 어휘를 공유하지 않으면 갈라질 위험이 있었다.

### 구조

```text
RenderSystemTestCode           Assets/Scripts/Framework/RunTimeSystems/RenderSystem/
  RenderSystemFactory.Create()
    → BindScene → InitData → PostBindService → Initialize
  ServiceResolver 노출, FixedUpdate에서 Tick
        ↓
FieldRoot   [SerializeField] RenderSystemTestCode
        ↓   GetService(Render).ColorSystemServices
FieldRuntime → FieldCellPool → FieldCellHandle
        ↓
IColorHandle (Renderer마다 1개, Cell 생성 시 만들어 계속 보유)
```

`IColorSystemService`는 `FieldRuntime` / `FieldCellPool` / `FieldCellHandle` 모두에서 필수 인자다. 빠지면 생성 시점에 `ArgumentNullException`이 난다.

Cell은 파괴되지 않고 Pool에 계속 보관되므로 Scope를 해제하는 경로는 두지 않았다. 수명은 `ColorSystem.Shutdown`이 정리한다.

### RenderSystemTestCode가 InputSystemTestCode와 다른 점

`InputSystemTestCode`를 그대로 본뜬 과도기 부트스트랩이지만 한 가지가 다르다.

`RenderSystemGroup`은 `ISystemInitData`를 구현하므로 `RunTimeSystem.Initialize()`가 다음 검사에 걸린다.

```csharp
if (InitDataModules.Count > 0 && !_isDataInitialized)
    throw new InvalidOperationException(
        "[SystemManager] InitData must be completed before Initialize.");
```

그래서 `DatabaseCatalogSO`를 받아 `InitData(DataBaseFactory.Create(database))`를 호출해야 한다. Input 모듈은 `ISystemInitData`를 구현하지 않아 DB 없이 동작한다.

`RunTimeSystem`의 생성자가 `internal`이라 이 부트스트랩은 **Framework 어셈블리 안에** 있어야 한다. `RenderSystemFactory`는 `public`이지만 그 결과를 담을 `RunTimeSystem`을 만들 수 없기 때문이다.

### 되돌리기 처리

`IColorHandle`에는 "오버라이드 해제"에 해당하는 수단이 없다. `SetPropertyBlock(null)`처럼 오버라이드를 걷어낼 수 없으므로, Cell 생성 시 `sharedMaterial`에서 `_BaseColor` 기본값을 읽어 두고 `ClearHighlight`에서 그 값을 다시 넣는다. 머티리얼에 해당 프로퍼티가 없으면 `Color.white`로 둔다.

Hover 복원(2절)은 보드 색으로 덮어쓰는 동작이라 이 문제와 무관하다.

### 검토했다가 버린 접근

처음에는 `FrameworkColorServiceSource`라는 어댑터를 만들어, Render 서비스를 얻지 못하면 기존 `MaterialPropertyBlock` 경로로 폴백하도록 했다. 다음 비용이 따라왔다.

- 부트스트랩 두 종류를 타입 매칭하는 `switch (_bootstrap)`
- `KeyNotFoundException`을 흐름 제어로 catch — `SystemServiceStore`에 `TryGetService`가 있지만 `ISystemServiceStoreReader`와 `ISystemServiceResolver` 어디에도 노출되어 있지 않다
- `FieldCellHandle`의 이중 경로

전부 "Render Runtime이 없다"는 상황을 감내하려는 비용이었다. `InputSystemTestCode`라는 선례가 이미 있었으므로 Render Runtime을 만드는 쪽이 옳았고, 어댑터와 폴백은 모두 제거했다.

### 함께 고친 것 — DatabaseCatalogSO 파일명

이 작업 중 `DataBaseSO.asset`이 Script is missing으로 뜨고, 새 에셋을 인스펙터에 넣으면 다음 오류가 났다.

```text
ProjectL.Framework.DataBase.DatabaseSO.DatabaseCatalogSO.
Containing file and class name must match.
```

`DatabaseCatalogSO` 클래스가 `DatabaseDataSO.cs`에 들어 있었다. Unity는 `ScriptableObject`의 클래스명과 파일명이 일치해야 스크립트를 바인딩한다. 같은 파일의 나머지 클래스는 `abstract`라 규칙에 걸리지 않았고, `RenderGroup`은 `RenderGroup.cs`에 있어 정상이었다.

이 불일치는 계속 있었지만 `DatabaseCatalogSO`를 인스펙터에 실제로 할당하는 코드가 없어 드러나지 않았다. `RenderSystemTestCode`가 처음으로 요구하면서 나타났다.

수정은 파일 이름 변경뿐이다. 코드는 건드리지 않았다.

```text
DatabaseDataSO.cs → DatabaseCatalogSO.cs
```

`.meta`를 함께 옮겨 guid(`1f010b74a81448f4192a360c7186191f`)를 유지했다. 그리고 `DataBaseSO.asset`의 끊어진 참조를 복구해 기존 데이터를 살렸다.

```diff
- m_Script: {fileID: 0}
+ m_Script: {fileID: 11500000, guid: 1f010b74a81448f4192a360c7186191f, type: 3}
```

`databaseGroups`의 `RenderGroup` 참조가 남아 있어 에셋을 새로 만들 필요는 없었다.

`DatabaseCatalogSO.cs`에는 여전히 인터페이스와 추상 클래스가 함께 들어 있어 파일명이 그중 한 타입만 가리킨다. 변경 범위를 최소로 두려고 이름만 바꿨으며, 정리한다면 추상 타입을 별도 파일로 분리하는 것이 맞다.

### 남은 것

**Runtime이 두 개 공존한다.** `InputSystemTestCode`와 `RenderSystemTestCode`가 각각 `RunTimeSystem`을 만든다. 서비스 스토어가 독립이라 충돌하지 않지만 의도된 과도기 상태다. 구성이 확정되면 `BaseSystemTestCode`처럼 `RunTimeSystemFactory.Create()` 하나로 통합하고, `FieldRoot`와 `InputInstaller`가 그 부트스트랩을 바라보게 하면 된다.

**`_Color`를 더 이상 쓰지 않는다.** `MaterialBaseTypeKey.MainColor`(`_BaseColor`)만 설정한다. Cell 프리팹 머티리얼이 Built-in RP의 `_Color`에 의존하면 색이 적용되지 않는다. 현재 구성에서는 정상 동작을 확인했다.

`IColorHandle.Dispose()`는 `ColorSystem`의 Renderer 등록과 활성 Handle 목록에서 해당 Handle을 제거한 뒤 풀에 반환한다. 따라서 반복 생성·폐기가 가능한 수명 계약이다.
