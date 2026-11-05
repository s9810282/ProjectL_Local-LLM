# 전투 넉백 미리보기 View 계획 (2026-10-27)

- 작성일: 2026-10-27. 코드 변경 없음(계획만).
- 설계: [knockback-preview-view-design](../specs/2026-10-27-combat-knockback-preview-view-design.md)(KB1~KB7, P1~P8, 결정 §6 모두 A).
- Core 원본: [attack-knockback-preview-design](../specs/2026-10-27-attack-knockback-preview-design.md)(천명님, `AttackPreview.Knockbacks`, 커밋 `35872a45` → 병합 `a4b2603b`).
- 사용자 요청 흐름: Core 메모와 레퍼런스(Into the Breach) → "이해되니" → "7기준 염두하고 설계 초안" → "Q-KB1~Q-KB6·Q-KB-T 모두 권장안(A)으로 하고 Task 계획 써줘".
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `D/` = `C/Display/`, `T/` = `Assets/Tests/EditMode/Framework/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`, `DB/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageField/Item/`.
- 성격: KB-a는 **새 부품과 값(화면 변화 없음, 아직 아무도 부르지 않음)**, KB-b에서 **화면에 넉백 미리보기가 나온다**, KB-c는 문서다.
- 상시 지시: MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다. 보고에는 "컴파일 미확인"을 적는다. 기존 시험은 컴파일이 깨지는 곳만 최소로 맞추고, 실행에서 깨지는 우리 시험은 결정(Q-KB-T)대로 맞춘다.

## 0. 결정과 착수 조건

### 0.1 설계 결정 (2026-10-27, 사용자 "모두 권장안(A)")

| # | 결정 |
|---|---|
| Q-KB1 | 화살표(대상 앞 가장자리, 밀리는 방향) + 도착 칸 테두리 |
| Q-KB2 | 화살표 색 셋(밀림·막힘·면역), 프리팹 하나. 막힘·면역은 경로가 없으면 도착 칸 없음. `Interrupted`는 밀림과 같음 |
| Q-KB3 | 결과 재생 직전(`BeforeResult`)에 지움(궤적과 같은 자리) |
| Q-KB4 | 입력 쪽은 필수 인자(`?.` 없음). 천명님 `WallAttackInteractionTests:242`는 컴파일 한 줄 |
| Q-KB5 | View DB 필수 프리팹 + 임시 프리팹(사용자가 에디터에서 만듦). 코드와 에셋은 한 커밋 |
| Q-KB6 | 끌어오기 도착 칸과는 이번에 합치지 않음(층만 나눔) |
| Q-KB-T | 우리 시험 실행 깨짐 맞춤: a `FieldOutlineRendererTests` 손잡이 수, b `ViewDataTableTests` 설정 도우미 둘 |

### 0.2 계획에서 정한 것 (TP-1~TP-12, 사용자 확인 대상)

설계와 다르게 정한 것은 TP-1·TP-2 둘이다. 나머지는 설계가 비워 둔 세부다.

| # | 정한 것 | 이유(원칙) |
|---|---|---|
| **TP-1** | **설계와 다름.** 캔버스 계약이 도착 칸과 표지를 함께 받는다: `ShowKnockbacks(destinations, marks)`·`ClearKnockbacks()`. `FieldView`가 자기 층을 칠하고 표지 부품을 부른다. 구성 `CombatKnockbackPreview`는 캔버스만 안다. 설계 KB3은 구성이 `FieldView.SetLayerCells`를 직접 부르는 꼴이었다 | 구성의 의존이 계약 하나가 된다(DIP). "도착 칸은 어느 층인가"를 필드 쪽 한 곳이 안다(캡슐화). 층과 표지가 한 호출로 바뀌어 둘이 어긋난 순간이 없다 |
| **TP-2** | **설계와 다름.** 화살표 색은 프리팹의 **모든** Renderer에 덮어쓴다(인스턴스마다 색 핸들 여럿). DB 검사는 "Renderer 하나 이상"이다. 설계 KB5는 "Renderer 하나"였다 | 임시 프리팹(큐브 둘)과 나중 아트(메시 여럿일 수 있음)가 모두 된다. 첫 Renderer만 칠하면 조용히 반만 칠해진다 |
| TP-3 | 재질이 색 덮어쓰기 속성(`MaterialBaseTypeKey.MainColor` = `_BaseColor`)을 갖는지는 표지 부품이 필드 구성 때 한 번 검사하고, 없으면 예외다. DB 검사에는 넣지 않는다 | 렌더 키는 Render 쪽 사실이라 DB 데이터 층이 Render 키를 참조하지 않게 한다. 구성 때 바로 멈추므로 조용한 실패가 없다. 선례 `DrawPointLineSystem.cs:62` |
| TP-4 | 프리팹 약속: 화살표는 로컬 +Z를 가리키고, 바닥 평면(XZ)에 눕혀 있으며, 피벗은 화살표 가운데다. 충돌체는 없다 | 회전을 `LookRotation(방향, 위)` 하나로 정한다. 피벗이 칸 가장자리에 놓여 화살표가 두 칸에 걸친다(레퍼런스와 같음). 충돌체가 있으면 다른 물리 Raycast에 걸릴 수 있다 |
| TP-5 | 띄움 높이(`knockbackArrowLift`)는 표지 부품이 더한다. 구성이 넘기는 표지 위치는 "점유 칸 표면 높이" 점이다(KB-b 검토 뒤 결정 Q-KBb3 A로 "점유 칸과 그 앞 칸 표면 중 가장 높은 값"이 됨, §7.2) | 모양 값(DB)은 그리기 쪽에서만 읽는다. 구성은 판 위 사실만 다룬다 |
| TP-6 | 입력 쪽 인자 자리는 `gate` 바로 뒤의 필수 인자 `knockback`이다 | 선택 인자(`sounds` 이하) 앞이라 이름 인자 없이 넘길 수 있다 |
| TP-7 | 시험용 아무것도 안 하는 가짜는 우리 `T/CombatInputTestFactory.cs`의 정적 값 `NoKnockbackPreview` 하나다. 시험 조립(`CreateInteraction`)은 끝 선택 인자 `knockback = null`을 받아 null이면 이 값을 쓴다. 천명님 Wall 시험과 우리 궤적 입력 시험은 이 값을 넘긴다 | 가짜를 한 곳에만 둔다. `CreateInteraction`을 부르는 많은 시험은 그대로다 |
| TP-8 | 층 표에서 새 줄의 자리는 `PullDestination` 바로 뒤, `BlockedWall` 앞이다. 색은 이동 초안 색, 채움 없음, 평소에는 안 그림 | "공격이 옮길 칸" 둘이 이웃한다. 막힌 벽은 지금처럼 맨 위다 |
| TP-9 | 기본값: 띄움 높이 0.05. 씬 색 기본값은 밀림 `(1, 0.85, 0.2)`, 막힘 `(1, 0.3, 0.3)`, 면역 `(0.65, 0.65, 0.65)` | 막힘 색은 궤적 막힘 색과 같다. 씬에는 다음 저장 때 칸이 생긴다 |
| TP-10 | 표지 값·종류 열거·캔버스 계약은 파일 하나(`D/Field/ICombatKnockbackCanvas.cs`)에 둔다 | 셋은 함께만 바뀐다. 선례 `FieldHighlightLayers.cs`(열거 + 표) |
| TP-11 | 필드 해제 순서는 궤적 → 넉백 표지 → 표시기다. 앞 것이 실패해도 뒤 것을 한다(`try/finally` 사슬) | 지금 `Release`·`OnDestroy`의 꼴(F6)을 그대로 넓힌다 |
| TP-12 | KB-b 끝에 검토 에이전트 하나를 돌린다(정적 검토, 코드만) | 컴파일·실행 확인을 못 하므로 Unity 확인 전에 한 번 더 본다. 사용자가 원하지 않으면 건너뛴다 |

### 0.3 착수 조건

1. 작업 트리에 이번 작업이 아닌 변경이 있다(세션 시작 때 `git status`: 낭떠러지 연출 `CombatSceneEventPresenter`·처리기 둘·`CombatShownFacts`·`CombatPresentationSettings*`·`CombatSinkPiece`·`CombatFallMotionSettings`, 그리고 `GlobalUICanvas.prefab`·`Test.asset`·`Unit_ShotGun.asset`). **이 계획이 고치는 파일과 겹치지 않는다.** 커밋 때 묶음만 나누면 된다(§6).
2. KB-a 앞에 사용자가 에디터에서 임시 화살표 프리팹을 만든다(T2). 프리팹이 없으면 KB-a 코드가 들어간 뒤 DB 생성이 실패한다.
3. KB-b는 KB-a의 Unity 확인(부트·기존 시험) 뒤에 시작한다.

## 1. 지금 코드 (HEAD `a4b2603b` + 작업 트리, 2026-10-27 대조)

| 무엇 | 어디 | 사실 |
|---|---|---|
| Core 값 | `…/CoreCaculator/Contracts/Results/AttackKnockbackSnapshot.cs` | `TargetId`·`TargetKind`·`StartAnchor`·`Step`·`RequestedDistance`·`Path`·`FinalAnchor`·`Stop`. `AttackPreview.Knockbacks`(없으면 빈 목록). 기록 대상은 `Targets` 안(`AttackPreview.cs:107-109`) |
| 점유 칸 | `…/Contracts/Results/AttackTargetPreview.cs:24` | `TargetOccupiedCells`(`ReadOnlyMemory<CellCoord>`), 공격 전 자리 |
| 공격 호버 | `C/Input/Interaction/AttackTargetingInteraction.cs` | 생성자 `:61-81`, `BeforeResult` `:206`, 응답 적용 `:299-330`(가능 분기 `:301-308`), 모드 정리 `ResetRangeAnswer(int)` `:477-491`, 호버 표시 지우기 `ClearHoverDisplay` `:505-511` |
| 입력 조립 | `F/CombatSceneInputFactory.cs:150-151` | `new AttackTargetingInteraction(inputWrite, inputRead, display, display, gate, sounds, rejections: notices, trajectory: display, actionAvailability: …)` |
| 표시 계약 묶음 | `D/ICombatBattleDisplay.cs:22-24` | `…, ICombatUnitRangeFeedback, ICombatAttackTrajectoryFeedback` |
| 씬 표시 | `D/CombatSceneDisplay.cs:574-594` | 궤적 region: `if (_isReleased) return; _attack.…` |
| 공격 표시 | `D/Feedback/CombatAttackFeedback.cs:17-42` | 필드 `_field`·`_hud`·`_trajectory`, 생성자 `(FieldView, CombatUnitHudFeedback, CombatAttackTrajectory)` |
| 표시 조립 | `F/CombatSceneDisplayFactory.cs:99` | `new CombatAttackFeedback(field, units.HudFeedback, new CombatAttackTrajectory(field, …))` — **이 계획은 고치지 않는다**(설계 P4) |
| 필드 | `D/Field/FieldView.cs` | 색 칸 `:33-86`(궤적 색 `:82-86`), `Build` `:106-144`(`BuildTrajectory` `:136`), `SetLayerCells`·`ClearLayer` `:180-197`(구성 전이면 무시), `Release` `:294-308`, `OnDestroy` `:310-323`, 궤적 캔버스 명시 구현 `:325-336`, `BuildTrajectory` `:342-350` |
| 좌표 | `D/Field/FieldGridMapper.cs` | `CellCenterToLocal` `:66`(칸 밖 예외), `SurfaceHeightOf` `:98`, 칸 폭·길이는 private `_cellWidth`·`_cellLength` |
| 층 표 | `D/Field/FieldHighlightLayers.cs` | 열거 `:13-55`(마지막 `PullDestination = 1 << 12`), 표 `:89-111` |
| 그리기 선례 | `D/Field/FieldTrajectoryLine.cs` | 생성자 검사 → `Build`(GameObject·핸들, 실패 시 거둠) → `Show`·`Clear` → `Release`(실패 모아 `AggregateException`) |
| 색 덮어쓰기 | `GlobalSystem/RenderSystem/Core/ColorSystem` | `IColorSystemService.CreateColorHandle(Renderer)`, `IColorHandle.SetOverrideData(0, MaterialBaseTypeKey.MainColor, color)`, `MainColor` = `_BaseColor` |
| DB 값 | `DB/StageFieldViewDataSO.cs` | 궤적 칸 `:53-65`, 접근자 `:116-122`, `Validate` `:133-156`, `ValidateTrajectory` `:159-173`. 에셋 `DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset` |

## 2. 파일 단위 설계

### 2.1 DB 값 — `DB/StageFieldViewDataSO.cs` (KB-a)

```csharp
[Header("Knockback Preview (넉백 미리보기 설계 KB6)")]
[Tooltip("공격 호버 때 밀릴 대상 앞에 놓는 화살표 프리팹입니다. 필수입니다. 로컬 +Z를 가리키고 바닥 평면에 눕혀 두며, 피벗은 가운데, 충돌체는 없습니다. 모든 Renderer의 _BaseColor를 밀림·막힘·면역 색으로 덮어씁니다.")]
[SerializeField] private GameObject knockbackArrowPrefab;
[Tooltip("화살표를 대상 칸 표면 위로 띄우는 높이입니다.")]
[SerializeField, Min(0f)] private float knockbackArrowLift = 0.05f;

public GameObject KnockbackArrowPrefab => knockbackArrowPrefab;   // 맨 설정(시험)에서는 null일 수 있음(설계 P7)
public float KnockbackArrowLift => knockbackArrowLift;

private void ValidateKnockbackPreview()   // Validate 끝에서 부름, 표시 모드 무관
{
    if (knockbackArrowPrefab == null) throw new InvalidOperationException("[StageFieldViewData] Knockback arrow prefab is required.");
    if (knockbackArrowPrefab.GetComponentInChildren<Renderer>(true) == null) throw … "requires a Renderer."
    if (!float.IsFinite(knockbackArrowLift) || knockbackArrowLift < 0f) throw … "lift must be a finite value of 0 or more."
}
```

예외 문구는 이웃 검사처럼 영어다.

### 2.2 좌표 — `D/Field/FieldGridMapper.cs` (KB-a)

```csharp
/// <summary>한 칸 방향(각 성분 -1·0·1)을 필드 로컬 변위로 바꿉니다. 이웃 칸 중심의 차이와 같습니다. 칸 밖도 됩니다.</summary>
internal Vector3 StepToLocal(CellCoord step) => new(step.X * _cellWidth, 0f, step.Y * _cellLength);
```

`CellCenterToLocal`과 같은 축이다(X = 열, Z = 행). 판 끝에서 밀리는 대상도 다음 칸이 판 밖일 수 있어, 칸 중심 차이 대신 이 함수를 쓴다.

### 2.3 층 — `D/Field/FieldHighlightLayers.cs` (KB-a)

- 열거: `KnockbackDestination = 1 << 13`. 주석은 "공격 미리보기의 넉백 도착 칸입니다(넉백 미리보기 설계 KB1). 끌어오기 도착 칸과 소유자·지우는 시점이 달라 층을 나눕니다."
- 표: `PullDestination` 줄 뒤에 `new LayerRow(FieldHighlightLayer.KnockbackDestination, FieldHighlightColor.MovementDraft, false, false)`. 주석 한 줄 "넉백 도착 칸도 이동 초안 색입니다(끌어오기 도착 칸과 같은 모양, 설계 P5)."
- `FieldView.cs:33-34`의 우선순위 설명 주석에 "넉백 도착"을 끼운다.
- 팔레트(`FieldHighlightPalette`)는 바뀌지 않는다(있는 색을 씀).

### 2.4 캔버스 계약·표지 값 — `D/Field/ICombatKnockbackCanvas.cs` (새, KB-a)

```csharp
/// <summary>넉백 표지 종류입니다. 색만 가릅니다. Core 멈춤 이유를 모르는 필드 쪽 형식입니다(설계 KB1).</summary>
internal enum FieldKnockbackMarkKind { Push = 1, Blocked = 2, Immune = 3 }

/// <summary>필드 셀 루트 로컬의 표지 하나입니다. 위치는 대상 점유 칸 표면 높이의 점이고, 띄움 높이는 그리는 쪽이 더합니다(TP-5).</summary>
internal readonly struct FieldKnockbackMark
{
    internal FieldKnockbackMark(Vector3 localPosition, Quaternion localRotation, FieldKnockbackMarkKind kind);
    internal Vector3 LocalPosition { get; }
    internal Quaternion LocalRotation { get; }
    internal FieldKnockbackMarkKind Kind { get; }
}

/// <summary>넉백 미리보기를 필드에 그리는 좁은 계약입니다(TP-1). 구현: FieldView(명시 구현).</summary>
internal interface ICombatKnockbackCanvas
{
    /// <summary>현재 필드 좌표 변환입니다. 구성 전에는 null입니다.</summary>
    FieldGridMapper Mapper { get; }
    /// <summary>도착 칸 층과 표지를 함께 바꿉니다. 빈 목록이면 지운 것과 같습니다. 구성 전이면 아무것도 하지 않습니다.</summary>
    void ShowKnockbacks(IReadOnlyList<CellCoord> destinations, IReadOnlyList<FieldKnockbackMark> marks);
    /// <summary>도착 칸 층과 표지를 지웁니다. 여러 번 불려도 안전합니다.</summary>
    void ClearKnockbacks();
}
```

`ICombatTrajectoryCanvas`에도 `Mapper`가 있다. 둘은 다른 계약이라 각각 둔다(ISP). `FieldView`의 두 명시 구현은 같은 `_mapper`를 돌려준다.

### 2.5 그리기 부품 — `D/Field/FieldKnockbackMarkers.cs` (새, KB-a)

꼴은 `FieldTrajectoryLine`과 같다.

```csharp
internal sealed class FieldKnockbackMarkers
{
    private const string Name = "FieldKnockback";

    internal FieldKnockbackMarkers(Transform parent, StageFieldViewData config,
        Color push, Color blocked, Color immune, IColorSystemService colors);
    internal void Build();                                          // 빈 부모 하나만 만든다
    internal void Show(IReadOnlyList<FieldKnockbackMark> marks);
    internal void Clear();                                          // 모두 끈다. 구성 전이어도 안전
    internal void Release();                                        // 핸들 → 부모 파괴, 실패 모아 한 번 던짐

    private sealed class Marker                                     // 인스턴스 하나
    {
        internal GameObject GameObject;
        internal IColorHandle[] Colors;                             // Renderer마다 하나(TP-2)
        internal FieldKnockbackMarkKind? ShownKind;                 // 색을 바꿀 때만 덮어쓰기
    }
}
```

- 생성자: 프리팹이 없으면 `ArgumentException`(필드가 프리팹 있을 때만 만듦). 프리팹 Renderer 전부의 `sharedMaterials`가 `_BaseColor`를 가지는지 한 번 본다(TP-3). 없으면 `InvalidOperationException("[FieldKnockbackMarkers] 화살표 재질에 _BaseColor가 없습니다: …")`.
- `Show`: 표지 수만큼 인스턴스를 갖춘다(모자라면 `Instantiate(prefab, root, false)` → Renderer마다 `CreateColorHandle`, null이면 예외). 앞쪽 n개는 `SetLocalPositionAndRotation(위치 + Vector3.up × lift, 회전)`, 종류가 바뀐 것만 색 덮어쓰기, 켬. 나머지는 끈다. `marks`가 null이면 `ArgumentNullException`.
- 종류 → 색은 `switch` 하나(모르는 값은 예외).
- 인스턴스는 가장 많이 쓴 수만큼 남겨 다시 쓴다. 필드를 해제할 때만 파괴한다.
- 매 프레임 하는 일이 없다.

### 2.6 필드 — `D/Field/FieldView.cs` (KB-a)

- 씬 색 셋(TP-9), 궤적 색 다음에 새 머리말:

```csharp
[Header("넉백 미리보기 색 (넉백 미리보기 설계 KB6)")]
[Tooltip("밀리는 대상의 화살표(다 가거나 중간에 멈춤).")]  [SerializeField] private Color knockbackColor = new(1f, 0.85f, 0.2f, 1f);
[Tooltip("막혀서 덜 가거나 못 가는 대상의 화살표.")]        [SerializeField] private Color knockbackBlockedColor = new(1f, 0.3f, 0.3f, 1f);
[Tooltip("넉백 면역이라 안 움직이는 대상의 화살표.")]       [SerializeField] private Color knockbackImmuneColor = new(0.65f, 0.65f, 0.65f, 1f);
```

- 클래스 선언: `MonoBehaviour, ICombatTrajectoryCanvas, ICombatKnockbackCanvas`. remarks에 넉백 표지 한 줄.
- 필드 `private FieldKnockbackMarkers _knockbackMarkers;`
- `Build`: `BuildTrajectory(...)` 다음에 `BuildKnockbackMarkers(config, colors)`. 실패하면 지금처럼 `Release()` 후 다시 던진다.
- `BuildKnockbackMarkers`: 프리팹이 없는 맨 설정이면 만들지 않는다(설계 P7, 궤적 `:344`와 같은 꼴, 주석에 "운영 설정은 DB 검사가 강제").
- 명시 구현:

```csharp
FieldGridMapper ICombatKnockbackCanvas.Mapper => _mapper;

void ICombatKnockbackCanvas.ShowKnockbacks(IReadOnlyList<CellCoord> destinations, IReadOnlyList<FieldKnockbackMark> marks)
{
    if (marks == null) throw new ArgumentNullException(nameof(marks));
    SetLayerCells(FieldHighlightLayer.KnockbackDestination, destinations);   // 구성 전이면 SetLayerCells가 무시
    _knockbackMarkers?.Show(marks);                                         // 맨 설정(시험)은 부품 없음(P7)
}

void ICombatKnockbackCanvas.ClearKnockbacks()
{
    ClearLayer(FieldHighlightLayer.KnockbackDestination);
    _knockbackMarkers?.Clear();
}
```

- `Release`·`OnDestroy`: 궤적 → 넉백 표지 → 표시기 순서로 각각 거둔다(TP-11). 참조를 먼저 비운다.

### 2.7 입력 쪽 계약 — `C/Input/Contracts/ICombatKnockbackPreviewFeedback.cs` (새, KB-b)

```csharp
/// <summary>
/// 공격 조준이 호버 응답으로 넉백 미리보기 표시 의도만 전하는 좁은 계약입니다(넉백 미리보기 설계 KB1, 결정 Q-KB4 A).
/// 공격 조준만 받고, 배치·이동·아이템 입력은 이 계약을 모릅니다. 구현: 씬 표시(CombatSceneDisplay), 시험 가짜.
/// </summary>
/// <remarks>부르는 쪽은 Core 응답을 그대로 넘기고 방향·칸·모양을 계산하지 않습니다. 구현은 멱등이고, 해제 뒤 늦게 온 호출은 무시합니다.</remarks>
internal interface ICombatKnockbackPreviewFeedback
{
    /// <summary>공격 가능 응답의 넉백 기록을 그립니다. 기록이 없으면 지운 것과 같습니다.</summary>
    void ShowKnockbackPreview(in AttackPreview preview);
    void ClearKnockbackPreview();
}
```

`D/ICombatBattleDisplay.cs` 상속 목록 끝에 `ICombatKnockbackPreviewFeedback`을 더한다.

### 2.8 구성 — `D/Feedback/CombatKnockbackPreview.cs` (새, KB-b)

```csharp
/// <summary>
/// 공격 미리보기의 넉백 기록을 도착 칸·표지로 바꿔 필드에 넘깁니다(넉백 미리보기 설계 KB1·KB4, 계획 TP-1).
/// "멈춘 이유 → 표지 종류"와 "점유 칸 → 도착 칸·화살표 자리"의 원본입니다. 방향·거리·막힘·면역은 계산하지 않습니다(원본 Core).
/// </summary>
internal sealed class CombatKnockbackPreview
{
    private readonly ICombatKnockbackCanvas _canvas;
    private readonly List<CellCoord> _destinations = new();
    private readonly List<FieldKnockbackMark> _marks = new();
    private readonly List<CellCoord> _occupied = new();       // SurfaceHeightOf에 넘길 작업 목록

    internal CombatKnockbackPreview(ICombatKnockbackCanvas canvas);
    internal void Show(in AttackPreview preview);
    internal void Clear() => _canvas.ClearKnockbacks();
}
```

`Show`:

1. `mapper = _canvas.Mapper`가 null이면 `Clear()` 후 끝(필드 구성 전).
2. 두 목록을 비운다. 기록마다:
   - `_occupied` ← `preview.Targets`에서 `TargetId`가 같은 대상의 `TargetOccupiedCells`. 없으면 `InvalidOperationException`(Core 계약 위반, 설계 P2). 비어 있으면 `StartAnchor` 하나.
   - `Path.Count > 0`이면 점유 칸마다 `+ (FinalAnchor − StartAnchor)`를 `_destinations`에 넣는다.
   - 화살표 자리: 점유 칸 중 `X·Step.X + Y·Step.Y`가 가장 큰 칸들의 `CellCenterToLocal` 평균 + `0.5 × mapper.StepToLocal(Step)`, 높이 = `mapper.SurfaceHeightOf(_occupied, StartAnchor)`.
   - 회전: `Quaternion.LookRotation(mapper.StepToLocal(Step), Vector3.up)`.
   - 종류: `KindOf(Stop)`.
3. `_canvas.ShowKnockbacks(_destinations, _marks)`. 기록이 없으면 빈 목록이라 지운 것과 같다(따로 갈래를 두지 않음).

```csharp
/// <summary>Core 멈춤 이유 → 표지 종류입니다. 이 바꾸기의 원본은 여기 한 곳입니다(Q-KB2 A).</summary>
internal static FieldKnockbackMarkKind KindOf(AttackKnockbackStop stop) => stop switch
{
    AttackKnockbackStop.Completed or AttackKnockbackStop.Interrupted => FieldKnockbackMarkKind.Push,
    AttackKnockbackStop.Blocked => FieldKnockbackMarkKind.Blocked,
    AttackKnockbackStop.Immune => FieldKnockbackMarkKind.Immune,
    _ => throw new ArgumentOutOfRangeException(nameof(stop), stop, null),
};
```

- 대상 찾기는 `preview.Targets.Span` 인덱스 반복이다(포인터마다가 아니라 응답마다라 작음, 할당 없음).
- 같은 입력 건너뛰기는 두지 않는다(설계 P8).

### 2.9 공격 표시·씬 표시 (KB-b)

`D/Feedback/CombatAttackFeedback.cs`:

```csharp
private readonly CombatKnockbackPreview _knockback;

// 생성자 끝에: 넉백 미리보기는 필드만 필요한 공격 표시의 한 부분이라 여기서 만듭니다(설계 P4, Factory 무변경).
_knockback = new CombatKnockbackPreview(field);

internal void ShowKnockbackPreview(in AttackPreview preview) => _knockback.Show(preview);
internal void ClearKnockbackPreview() => _knockback.Clear();
```

클래스 summary를 "공격의 범위·영향·차단 벽·궤적·넉백과 체력 미리보기 표시 의도를 적용합니다"로, remarks에 "넉백은 구성(`CombatKnockbackPreview`)에 넘기기만 합니다" 한 줄.

`D/CombatSceneDisplay.cs`: 궤적 region 다음에 `#region 넉백 미리보기 (ICombatKnockbackPreviewFeedback, 넉백 미리보기 설계 KB3)`. 두 메서드 모두 `if (_isReleased) return;` 뒤 `_attack.…`(궤적과 같은 꼴).

### 2.10 입력 — `C/Input/Interaction/AttackTargetingInteraction.cs` (KB-b)

- 필드 `private readonly ICombatKnockbackPreviewFeedback _knockback;`(궤적 필드 옆, 주석 "넉백 미리보기 표시 의도입니다. 운영·시험 모두 필수입니다(결정 Q-KB4 A).").
- 생성자: `CombatCommandGate gate, ICombatKnockbackPreviewFeedback knockback, CombatInputSounds sounds = null, …`(TP-6), `_knockback = knockback ?? throw new ArgumentNullException(nameof(knockback));`.
- 네 곳:

| 자리 | 바꿈 |
|---|---|
| `ApplyAttackAnswer` 가능 분기(`:301-304`) | `_feedback.ShowAffectedCells(…)` 다음에 `_knockback.ShowKnockbackPreview(attack);` |
| 같은 곳 불가 분기(`:305-308`) | `_feedback.ClearAffectedCells();` 다음에 `_knockback.ClearKnockbackPreview();` |
| `ClearHoverDisplay`(`:505-511`) | `ClearTrajectory();` 앞에 `_knockback.ClearKnockbackPreview();`. summary의 목록에 "넉백" |
| `ResetRangeAnswer(int)`(`:484-490`) | 궤적 람다 다음에 `() => { if (generation == _modeGeneration) _knockback.ClearKnockbackPreview(); }` |
| `BeforeResult`(`:206-212`) | `ClearTrajectory();` 다음에 `_knockback.ClearKnockbackPreview();`. summary에 "넉백 미리보기도 지웁니다 — 실제로 밀리는 재생 위에 옛 화살표가 남지 않게(결정 Q-KB3 A)" |

- 클래스 summary의 "소유" 목록은 그대로 둔다(넉백은 상태를 갖지 않음).

### 2.11 입력 조립 — `F/CombatSceneInputFactory.cs:150-151` (KB-b)

```csharp
// 넉백 미리보기 표시 의도도 표시가 함께 제공한다(넉백 미리보기 설계 KB1, 결정 Q-KB4 A).
var attack = new AttackTargetingInteraction(inputWrite, inputRead, display, display, gate, display, sounds,
    rejections: notices, trajectory: display, actionAvailability: actionAvailability);
```

Factory 연결 줄이라 민형님 공유 §33에 적는다(KB-c).

## 3. Phase·Task

### KB-a 값·그리기 부품 (화면 변화 없음)

| T | 할 일 | 파일 |
|---|---|---|
| T1 | DB 칸 둘·접근자·`ValidateKnockbackPreview`(§2.1) | `DB/StageFieldViewDataSO.cs` |
| T2 | **사용자 에디터:** 임시 화살표 프리팹 `Assets/ProjectL/Prefabs/Combat/KnockbackArrow.prefab`을 만든다(TP-4: 루트 빈 오브젝트 + 납작한 큐브 둘(자루·머리, 머리는 Y축 45°), 충돌체 삭제, 재질은 `_BaseColor`가 있는 URP Unlit). `StageFieldView_Default.asset`의 `Knockback Arrow Prefab`에 연결 | 에셋(사용자) |
| T3 | `FieldGridMapper.StepToLocal`(§2.2) | `D/Field/FieldGridMapper.cs` |
| T4 | 층 값·표 한 줄·`FieldView` 우선순위 주석(§2.3) | `D/Field/FieldHighlightLayers.cs`, `D/Field/FieldView.cs` |
| T5 | 캔버스 계약·표지 값·종류(§2.4) | `D/Field/ICombatKnockbackCanvas.cs`(+ `.meta`) |
| T6 | 그리기 부품(§2.5) | `D/Field/FieldKnockbackMarkers.cs`(+ `.meta`) |
| T7 | 필드: 색 셋·부품 구성·명시 구현·해제 순서(§2.6) | `D/Field/FieldView.cs` |
| T8 | 시험 맞춤(Q-KB-T): `FieldOutlineRendererTests` `:84` 15→16, `:312` 30→32, `:575` 16→17, `:593` 15→16(층 하나 = 아웃라인 손잡이 하나, 확인 내용 그대로). `ViewDataTableTests.FieldView()`(`:473`)·`OutlineFieldView()`(`:485`)에 Renderer 있는 화살표 프리팹 한 줄씩(`cellPrefab`을 만드는 줄과 같은 꼴) | `T/FieldOutlineRendererTests.cs`, `T/ViewDataTableTests.cs` |

`.meta`는 새 `.cs` 둘에 Unity 규칙대로 만든다(GUID 새로).

KB-a 확인(사용자 Unity): 컴파일, Boot부터 Combat 진입(DB 생성·필드 구성 성공), `FieldOutlineRendererTests`·`ViewDataTableTests`·`DataBaseAssetTests`·`FieldTrajectoryLineTests`. 화면은 그대로여야 한다.

### KB-b 구성·입력 연결 (화면에 나옴)

| T | 할 일 | 파일 |
|---|---|---|
| T9 | 입력 쪽 계약(§2.7), 표시 계약 묶음 상속 | `C/Input/Contracts/ICombatKnockbackPreviewFeedback.cs`(+ `.meta`), `D/ICombatBattleDisplay.cs` |
| T10 | 구성(§2.8) | `D/Feedback/CombatKnockbackPreview.cs`(+ `.meta`) |
| T11 | 공격 표시·씬 표시(§2.9) | `D/Feedback/CombatAttackFeedback.cs`, `D/CombatSceneDisplay.cs` |
| T12 | 입력 네 곳·생성자(§2.10) | `C/Input/Interaction/AttackTargetingInteraction.cs` |
| T13 | 입력 조립 한 줄(§2.11) | `F/CombatSceneInputFactory.cs` |
| T14 | 시험 컴파일 맞춤: `NoKnockbackPreview`·`CreateInteraction` 끝 선택 인자(TP-7), `AttackTrajectoryInteractionTests:130`·`WallAttackInteractionTests:242`(천명님)에 `CombatInputTestFactory.NoKnockbackPreview` 한 줄, `CombatViewSystemTests.FakeDisplay`(`:1800` 근처)에 빈 멤버 둘 | `T/CombatInputTestFactory.cs`, `T/AttackTrajectoryInteractionTests.cs`, `T/WallAttackInteractionTests.cs`, `T/CombatViewSystemTests.cs` |
| T15 | 검토 에이전트 하나(TP-12): 이 계획·설계와 바뀐 파일만 주고 7기준·누락 호출 자리·해제 순서를 본다. 반영은 보고 뒤 | — |

KB-b 확인(사용자 Unity): 컴파일, 기존 공격 조준 시험(`AttackTrajectoryInteractionTests`·`WallAttackInteractionTests`·`CombatInteractionTests`·`CombatAttackFeedbackTests`·`PullAndMarkPresentationTests`), 재생 확인 §5.

### KB-c 문서·공유

| T | 할 일 |
|---|---|
| T16 | 공유(모두 미전달로 작성): 민형님 [Factory 공유](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) §33(입력 Factory 연결 줄, Framework·서비스 무변경). 호진님 [메모](../handoffs/2026-10-22-combat-view-structure-hojin-share.md) §15(`StageFieldViewData` 칸 둘·필수 검사, `ViewDataTableTests` 도우미). 천명님 [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.54(`WallAttackInteractionTests:242` 인자 한 줄, View 요청 없음, Core 설계 §7 인계를 받아 구현함) |
| T17 | [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md)에 새 파일 넷(`Display/Field/` 둘, `Display/Feedback/` 하나, `Input/Contracts/` 하나) |
| T18 | 설계 §구현 메모(TP-1·TP-2 반영, 설계와 다른 점), 이 계획 §7 실행 기록, CLAUDE.md 현재 상태 |

## 4. 시험 영향 (새 `[Test]` 없음)

| 시험 | 작성 | 깨짐 | Phase | 처리 |
|---|---|---|---|---|
| `FieldOutlineRendererTests:84·312·575·593` | 우리 | 실행(손잡이 수) | KB-a | 기대값 +1·+2(Q-KB-T a) |
| `ViewDataTableTests` 도우미 `:473·485` | 우리 | 실행(필수 칸) | KB-a | 화살표 프리팹 한 줄씩(Q-KB-T b) |
| `CombatViewSystemTests.FakeDisplay` | 우리 | 컴파일 | KB-b | 빈 멤버 둘 |
| `CombatInputTestFactory` | 우리 | 컴파일 | KB-b | TP-7 |
| `AttackTrajectoryInteractionTests:130` | 우리 | 컴파일 | KB-b | 인자 한 줄 |
| `WallAttackInteractionTests:242` | 천명님 | 컴파일 | KB-b | 인자 한 줄(Q-KB4 A로 허락됨), Core 전달 문서 §5.54 |

깨지지 않는 것(확인함): `PullAndMarkPresentationTests`(`CombatAttackFeedback` 생성자·`ShowAffectedCells` 그대로), `HojinCombatAttackDragTests.Probe`(`ICombatInteractionFeedback`만 구현), `CombatUnitRangeHoverTests`·`CombatInteractionTests` 가짜(같은 이유), 층 조합을 쓰는 `CombatDisplayReuseTests`·`WallAttackInteractionTests:160`(새 층을 쓰지 않음).

## 5. Unity 확인 목록 (KB-b 뒤, 사용자)

1. 충격 볼트(Ballista): 적 둘레 3×3에 호버 → 상하좌우·대각선 대상마다 바깥쪽 화살표(칸 가장자리·꼭짓점)와 도착 칸 테두리.
2. 착탄 중심에 있는 대상·아군에는 화살표가 없다.
3. 벽·유닛 바로 앞 대상: 막힘 색 화살표, 도착 칸 없음. 몇 칸 가다 막히면 막힘 색 + 멈춘 칸.
4. 면역 대상: 면역 색 화살표, 도착 칸 없음.
5. 가시·낙사로 경로 중 사망: 밀림 색 + 멈춘 칸, HUD 체력 0.
6. 밀 수 있는 장애물(다칸이면 점유 칸 전체가 옮겨진 자리).
7. 고지대 위 대상: 화살표가 블록 윗면 위에 뜬다.
8. 호버 칸을 옮기면 바로 바뀐다. 칸 밖·불가 칸·공격 모드 종료(E·우클릭)·턴 종료·결과 재생 시작 때 사라진다.
9. 공격 뒤 결과 재생 동안 화살표가 없고, 재생 뒤 같은 칸 호버면 새 자리 기준으로 다시 나온다.
10. 끌어오기 공격: 지금처럼 도착 칸이 보이고(이번 변경 없음), 넉백 표시와 섞이지 않는다.
11. 필드 표시 모드 CellPrefab·GridOutline 둘 다(도착 칸은 칸 색·테두리).

## 6. 커밋 묶음

- **KB-a:** `DB/StageFieldViewDataSO.cs` + `StageFieldView_Default.asset` + `Prefabs/Combat/KnockbackArrow.prefab`(+ `.meta`, 사용자가 만든 것)은 **한 커밋**(없으면 DB 생성 실패). 같은 묶음에 `FieldGridMapper`·`FieldHighlightLayers`·`FieldView`·`ICombatKnockbackCanvas`(+ `.meta`)·`FieldKnockbackMarkers`(+ `.meta`)·시험 둘. `Combat.unity`는 `FieldView` 색 칸이 생겨 저장되면 함께(값만 바뀜).
- **KB-b:** 계약(+ `.meta`)·구성(+ `.meta`)·표시 둘·입력·입력 Factory·시험 넷. KB-a 위에서만 컴파일된다.
- **KB-c:** 문서.
- 작업 트리의 다른 변경(§0.3 1)은 넣지 않는다. 커밋·push는 사용자가 한다.

## 7. 실행 기록

### 7.1 KB-a 코드 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음)

사용자 지시: "TP-1~TP-12 그대로 하고 KB-a 진행해. 임시 화살표 프리팹은 내가 에디터에서 만들어 연결할게." MSBuild는 돌리지 않았다(상시 지시). 시작 때 HEAD는 `ae055d3e`이고, 작업 트리의 다른 변경(지형 이웃 모양 에셋, `Combat.unity`, FMOD 캐시 등)은 이 Phase 파일과 겹치지 않는다.

| T | 한 것 |
|---|---|
| T1 | `DB/StageFieldViewDataSO.cs`: 머리말 "Knockback Preview", 칸 `knockbackArrowPrefab`·`knockbackArrowLift`(기본 0.05), 접근자 둘, `Validate` 끝에서 `ValidateKnockbackPreview()`(프리팹 필수·Renderer 하나 이상·띄움 높이 유한 0 이상) |
| T2 | **사용자 몫, 대기.** 프리팹 `Prefabs/Combat/KnockbackArrow.prefab`을 만들어 `StageFieldView_Default.asset`의 `Knockback Arrow Prefab`에 연결 |
| T3 | `FieldGridMapper.StepToLocal(CellCoord)` |
| T4 | 열거 `KnockbackDestination = 1 << 13`, 층 표 한 줄(`PullDestination` 뒤, 이동 초안 색, 채움 없음, 평소에는 안 그림), `FieldView` 우선순위 주석 |
| T5 | 새 `D/Field/ICombatKnockbackCanvas.cs`(+ `.meta`, GUID `76c548bd…`): 종류 열거 `FieldKnockbackMarkKind`, 값 `FieldKnockbackMark`, 계약 `ICombatKnockbackCanvas` |
| T6 | 새 `D/Field/FieldKnockbackMarkers.cs`(+ `.meta`, GUID `8f7e6a2f…`): 생성 때 재질 `_BaseColor` 검사(TP-3), `Build`는 빈 부모만, `Show`는 인스턴스를 늘려 다시 쓰고 종류가 바뀐 것만 Renderer마다·재질 칸마다 색을 덮어씀(TP-2), `Clear`, `Release`(실패 모아 한 번) |
| T7 | `FieldView`: 색 셋(머리말 "넉백 미리보기 색", TP-9 기본값), 부품 필드, `Build`에서 `BuildKnockbackMarkers`(프리팹 없는 맨 설정이면 만들지 않음), `Release`·`OnDestroy`는 궤적 → 넉백 표지 → 표시기(TP-11), 캔버스 명시 구현(층 칠하기 + 표지) |
| T8 | 우리 `FieldOutlineRendererTests` `:84` 15→16·`:312` 30→32·`:575` 16→17·`:593` 15→16. 우리 `ViewDataTableTests`의 `FieldView()`·`OutlineFieldView()`에 `knockbackArrowPrefab` 한 줄씩과 도우미 `KnockbackArrow()`(Renderer 있는 오브젝트, `[Test]` 아님) |

계획과 다른 점:

- 색 핸들은 Renderer마다 하나이고, 재질 칸(`sharedMaterials`)마다 색을 넣는다. ColorSystem `SetOverrideData`의 첫 인자가 재질 칸 번호라서다(`ColorHandle.cs:63`). 계획 §2.5는 "Renderer마다 색 핸들"까지만 적었다. TP-2의 뜻("일부만 칠해지지 않게")과 같다.
- 캔버스 명시 구현의 `ShowKnockbacks`에는 "구성됐나" 검사를 따로 두지 않았다. 표지 부품은 구성이 성공할 때만 생기고 해제 때 함께 비워지므로, 부품이 있으면 늘 구성된 상태다(무의미한 If를 두지 않음).

7기준 자체 점검(KB-a 범위):

- SSOT: 화살표 필수·Renderer 조건은 DB 검사 한 곳, 색 덮어쓰기 속성은 표지 부품 한 곳, 층 색은 층 표 한 줄, 방향 → 로컬 변위는 mapper 한 곳이다.
- SOLID·캡슐화: 표지 부품은 Core 형식을 모르고(자체 종류 열거), 캔버스 계약은 두 동작과 좌표 변환만 낸다.
- FW: 서비스·모듈·Factory 무변경. ColorSystem 핸들은 기존 방식(등록·덮어쓰기·Dispose)으로 쓴다.
- If/Tick: 새 Tick 없음. 새 조건은 프리팹 없는 맨 설정 갈래(궤적과 같은 기존 성질, 설계 P7) 하나다.
- 남는 것(낮음): `ICombatTrajectoryCanvas`와 `ICombatKnockbackCanvas`가 같은 `Mapper`를 각각 낸다(ISP로 계약을 나눈 대가).

**KB-a 확인(사용자 Unity, T2 뒤):**

1. 컴파일.
2. Boot부터 Combat 진입 — DB 생성(프리팹 필수 검사)과 필드 구성(재질 `_BaseColor` 검사)이 통과해야 한다. 프리팹을 연결하기 전에는 "Knockback arrow prefab is required."로 멈추는 것이 맞다.
3. 시험: `FieldOutlineRendererTests`·`ViewDataTableTests`·`DataBaseAssetTests`·`FieldTrajectoryLineTests`, 그리고 실제 `StageFieldView_Default`를 읽어 필드를 만드는 `CombatMovementPlanningDisplayTests`·`FieldRailwayPresenterTests`.
4. 화면은 그대로여야 한다(아직 아무도 부르지 않음). 하이어라키의 셀 루트 아래에 빈 `FieldKnockback`이 생긴다.

커밋 묶음(§6): 이 Phase의 코드·시험과 사용자가 만든 프리팹(+ `.meta`), `StageFieldView_Default.asset`을 한 커밋에 넣는다. `Combat.unity`는 `FieldView` 색 칸이 저장되면 함께 넣는다(이번 작업 전부터 다른 변경이 있으니 hunk를 확인).

KB-a Unity 확인(2026-10-27, 사용자): "프리팹 연결하고 Unity 확인했어. 문제 없으니 KB-b 진행해." 사용자가 만든 프리팹 이름은 계획의 `KnockbackArrow.prefab`이 아니라 `Prefabs/Combat/Arrow.prefab`이다(위치는 같음, 커밋 때 이 이름으로 묶는다).

### 7.2 KB-b 코드 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음)

| T | 한 것 |
|---|---|
| T9 | 새 `C/Input/Contracts/ICombatKnockbackPreviewFeedback.cs`(+ `.meta`, GUID `eae49536…`), `ICombatBattleDisplay` 상속 목록 끝에 더함 |
| T10 | 새 `D/Feedback/CombatKnockbackPreview.cs`(+ `.meta`, GUID `8e1a5744…`): 점유 칸은 같은 응답의 `TargetOccupiedCells`(없으면 예외, 비면 시작 칸), 도착 칸은 경로가 있을 때만, 화살표 자리 = 앞선 칸들 중심 + 반 칸·높이 = 점유 칸 표면 최고, 회전 = `StepToLocal` 방향, 종류 바꾸기 `KindOf` 한 곳 |
| T11 | `CombatAttackFeedback`: 생성자 안에서 구성 부품을 만들고(설계 P4) 두 메서드로 넘김. `CombatSceneDisplay`: 넉백 region(해제 뒤 무시) |
| T12 | `AttackTargetingInteraction`: 필수 인자 `knockback`(`gate` 바로 뒤, TP-6), 가능 응답에서 보임·불가 응답·`ClearHoverDisplay`·`ResetRangeAnswer` 람다·`BeforeResult`에서 지움 |
| T13 | `F/CombatSceneInputFactory.cs:151`: `gate, display, sounds` |
| T14 | 시험 컴파일 맞춤: 우리 `CombatInputTestFactory`(정적 `NoKnockbackPreview`, `CreateInteraction` 끝 선택 인자 `knockback`), 우리 `AttackTrajectoryInteractionTests:130`·천명님 `WallAttackInteractionTests:242`에 `CombatInputTestFactory.NoKnockbackPreview` 한 줄, 우리 `CombatViewSystemTests.FakeDisplay`에 빈 멤버 둘 |
| T15 | 검토 에이전트 하나(아래) |

**검토(새 에이전트 하나, 정적, 2026-10-27):** 높음 0, 중간 2, 낮음 6. 컴파일 오류는 찾지 못했고(생성 자리 넷의 인자 순서, `ICombatBattleDisplay` 구현 둘), 실행에서 깨질 시험도 찾지 못했다. 지워야 할 자리(격자 밖·새 칸·공격 아닌 응답·불가 응답·모드 종료·결과 재생 직전·결과 뒤처리·표시 해제·필드 재구성)도 모두 지운다. 중간 둘은 직접 코드와 에셋으로 확인했다.

| # | 지적 | 확인 | 처리 |
|---|---|---|---|
| 중간 1 | 사용자 프리팹 `Arrow.prefab`의 화살표가 로컬 +X를 가리킨다(자식 둘 Y ±30°, z ±1 → 만나는 끝이 +X). BoxCollider 둘이 남아 있다(TP-4와 다름) | 맞음(YAML) | **사용자 결정 대기**(Q-KBb2) |
| 중간 2 | 결과 재생 중에 화살표가 다시 나올 수 있다. Plan이 재생 중에도 미리보기에 답하고(`CombatPlan.cs:142-148`), 공격 호버 요청 조건 `CanPreview`는 재생 중인지를 보지 않는다. 재생 중 포인터가 다른 칸으로 가면 새 응답의 화살표가 움직이는 유닛 위에 그려진다. 궤적도 같은 기존 성질 | 맞음 | **사용자 결정 대기**(Q-KBb1, 계획 P3을 바꾸는 일) |
| 낮음 3 | `CombatKnockbackPreview.Show`의 필드 구성 전 `Clear()`는 아무 일도 하지 않는다 | 맞음 | 반영: `return`만 |
| 낮음 4 | 넉백 지우기가 네 곳(SSOT), 불가 응답 분기의 지우기는 중복(요청 때 이미 지움) | 맞음 | 반영: 도우미 `ClearShotPreview()`(넉백 + 궤적)를 `ClearHoverDisplay`·`BeforeResult`가 같이 부름, 불가 분기의 넉백 지우기는 걷고 주석. 남은 자리는 도우미·`ResetRangeAnswer` 람다(세대 검사) 둘 |
| 낮음 5 | 화살표 앞 절반은 다음 칸 위인데 높이는 점유 칸만 봄 → 고지대 쪽으로 막히면 묻힘 | 맞음 | **사용자 결정 대기**(Q-KBb3, 설계 KB2 높이 규칙을 바꾸는 일) |
| 낮음 6 | `TargetOccupiedCells`는 공격 전 자리라, 같은 공격에서 넉백 전에 대상이 옮겨지면(끌어오기 + 넉백 등) 어긋난다. 2연발 반복 넉백은 기록에 없다 | 맞음(Core 설계 §6과 같은 범위) | 기록, KB-c에서 Core 전달 문서 §5.54에 질문으로 |
| 낮음 7 | 입력 Factory의 새 주석이 `placement` 줄 위 | 맞음 | 반영: `attack` 줄 위로 |
| 낮음 8 | 캔버스 `Mapper`가 `FieldGridMapper` 전체를 냄 | 궤적 캔버스와 같은 기존 대가 | 기록만 |

**계획과 다른 점:** 지우는 자리를 도우미로 묶고 불가 분기의 넉백 지우기를 걷었다(§2.10 표의 "같은 곳 불가 분기" 줄). 동작은 같다(그 요청을 보낼 때 `RequestHoverForCell` → `ClearHoverDisplay`가 이미 지움, 응답 단계는 새 응답일 때만 돎).

**검토 결정 반영 (2026-10-27, 사용자 "Q-KBb1~Q-KBb3 모두 권장안으로 반영해", 컴파일 미확인).** 사용자 지시로 이 반영 뒤에는 검토를 돌리지 않고, KB-c까지 끝낸 뒤 마지막에 한 번 일괄로 검토한다.

| 결정 | 반영 |
|---|---|
| Q-KBb1 A(계획 P3·설계 P3 바뀜) | 넉백은 체력·비용 예상과 같은 조건(`attack.CanAttack && CanShowHealthPreview(facts)`, 결과 재생 중이 아님)에서만 그린다. `ApplyAttackAnswer`의 영향 칸 분기에서 결과 예상 분기로 옮겼고, `CanShowHealthPreview`에 "결과 예상(체력·비용·넉백) 조건의 원본" 요약을 달았다. 재생 뒤에는 결과 후처리가 범위 → 호버를 다시 요청해 다시 나온다. 궤적은 그대로다(기존 성질, B안을 고르지 않음) |
| Q-KBb2 A | 사용자 프리팹 `Prefabs/Combat/Arrow.prefab`을 YAML로 고쳤다. 자식 `a`는 위치 (−1, 0, 0)·Y −60°, `a (1)`은 (1, 0, 0)·Y −120°로 두어(둘 다 루트 기준 Y −90°만큼 돌린 자리) 두 막대가 로컬 +Z에서 만난다. BoxCollider 둘(`5400447481232622894`·`8065419159645289572`)과 그 컴포넌트 목록 줄을 지웠다. 루트 위치·배율, 레이어 20, 재질은 그대로다 |
| Q-KBb3 A(설계 KB2 높이 규칙 바뀜) | `CombatKnockbackPreview.MarkOf`: 높이 = 점유 칸 표면 최고와, 앞선 칸마다 그 앞 칸(판 안일 때만, `mapper.IsInside`) 표면 높이(`mapper.Surface.HeightAt`) 중 가장 높은 값 |

KB-b(결정 반영 포함) Unity 확인(2026-10-27, 사용자): "Unity 확인했어. 문제 없으니 KB-c 진행하고 마지막에 일괄 검토해."

### 7.3 KB-c 문서·공유 완료 (2026-10-27, 코드 변경 없음, 모두 미전달)

| T | 한 것 |
|---|---|
| T16 | 민형님 [Factory 공유](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) §33(입력 Factory 연결 줄, ColorSystem 핸들 사용, 강조 층·손잡이 수). 호진님 [메모](../handoffs/2026-10-22-combat-view-structure-hojin-share.md) §15(`StageFieldViewData` 칸 둘·필수 검사·임시 화살표 약속). 천명님 [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.54(`WallAttackInteractionTests:242` 한 줄, View가 기대는 Core 사실, 질문 둘: 넉백 전 이동으로 점유 칸 어긋남·2연발 넉백) |
| T17 | [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md) §1.1 표의 `Input/Contracts/`·`Display/Feedback/`·`Display/Field/` 예에 새 파일 넷 |
| T18 | 설계 §10 구현 메모, 이 기록, CLAUDE.md 현재 상태 |

일괄 검토는 아래 §7.4(사용자 지시: 모든 작업이 끝난 뒤 한 번).

### 7.4 일괄 검토 (2026-10-27, 새 에이전트 하나, 정적, KB-a~KB-c 전체)

결론은 높음·중간 0, 낮음 10이다. KB-b 검토 뒤 바꾼 다섯(그리는 조건, 지우는 도우미, 높이, 프리팹, 구성 전 `return`)은 모두 맞게 들어갔다고 확인받았다. 프리팹은 YAML로 계산해 +Z 꺾쇠·피벗 가운데·지운 콜라이더 참조 없음을 확인받았다. 수명(호버 변경·격자 밖·공격 아닌 응답·불가 응답·재생 직전·결과 뒤처리·모드 종료·턴 종료·표시 해제·필드 재구성)에서 남거나 빠지는 표시는 없다. 실행에서 깨질 시험은 찾지 못했다(실제 `StageFieldView_Default`를 쓰는 시험은 `Bullet.mat`에 `_BaseColor`가 있어 통과, 색 핸들은 처음 그릴 때만 만듦). 예상 점수는 SSOT 8.75·SOLID 8.5·FW 9.5·독립성 9.0·캡슐화 9.0·C#/Unity 8.75·If/Tick 9.0, 합계 약 8.9다.

| # | 지적 | 처리 |
|---|---|---|
| 1 | CLAUDE.md 넉백 항목이 낡음(착수 전 문구·T2 대기 등) | 반영: 완료 상태로 다시 씀 |
| 2 | Factory 공유 §33의 메서드 이름이 `CreateSceneInput`인데 실제는 `CreateSceneInteraction`(`CombatSceneInputFactory.cs:104`) | 반영(확인함) |
| 3 | `ICombatKnockbackCanvas`의 표지 위치 주석과 TP-5가 Q-KBb3 앞 규칙 | 반영: 주석·TP-5에 바뀐 규칙 |
| 4 | 설계 §10 "넷만 다르다"에 KB4(구성 전 지움 → 끝냄)가 빠짐 | 반영: 다섯째 항목 |
| 5 | 대각선 화살표는 꼭짓점에서 네 칸에 걸치는데 높이는 앞 칸과 대각선 앞 칸만 봄 → 옆 두 칸이 고지대면 묻힘 | 반영(2026-10-27 사용자 "대각선 옆 두 칸 높이도 넣고"): 대각선이면 앞선 칸마다 `(X+sx, Y)`·`(X, Y+sy)`도 판 안일 때 높이 최대값에 넣음, 도우미 `HigherSurface` |
| 6 | `CanShowHealthPreview` 요약이 "원본"이라 지우는 때까지 정하는 것처럼 읽힘 | 반영: "그리는 조건만, 지우는 때는 따로" |
| 7 | `ResetRangeAnswer` 람다가 `ClearShotPreview` 내용을 다시 적음(SSOT) | 반영: 도우미 요약에 "모드 정리에도 같이 더함". 구조는 그대로(정리 실패를 서로 떼어 둠) |
| 8 | 임시 화살표가 그림자를 드리우고(`m_CastShadows: 1`), 재질을 `Bullet.mat`과 같이 씀 | 그림자는 반영(2026-10-27 사용자 "화살표 그림자도 꺼줘"): 프리팹 값이 아니라 표지 부품이 인스턴스를 만들 때 Renderer마다 `shadowCastingMode = Off`·`receiveShadows = false`(궤적 점선과 같은 자리·같은 값, 나중 아트도 그대로 따름). 재질 공유는 기록(아트가 나오면 전용 재질) |

### 7.5 도착 칸 전용 색·채우기, 테두리 가림 (2026-10-27, 사용자 요청, 컴파일 미확인, 새 시험 없음)

사용자 요청 셋: ① 넉백 도착 칸 테두리가 공격 범위 테두리와 같은 계층이라 가려짐 → 위로, ② 도착 칸 테두리 색을 설정 가능하게, ③ 색 채우기.

| # | 처리 |
|---|---|
| ② | 색 종류 `FieldHighlightColor.KnockbackDestination = 16`, 팔레트 칸·`TryGet` 한 줄(생성자 인자 `knockbackDestination`, `fillAlpha` 앞), `FieldView` 씬 색 `knockbackDestinationColor`(머리말 "넉백 미리보기 색", 기본값 = 옛 이동 초안 색이라 화면 색은 그대로), 층 표 줄의 색을 이 종류로. 이제 이동 경로선·끌어오기 도착 칸과 색이 따로다 |
| ③ | 층 표 줄의 채움을 켬. GridOutline 채우기는 칸마다 우선순위가 가장 높은 층 하나만 칠하므로(DrawOrder), 공격 범위·영향 칸과 겹친 칸도 넉백 도착 색으로 채워진다. 불투명도는 공용 `fillAlpha` |
| ① | 원인: 모든 칸 테두리는 GlobalSystem `AreaOutline`(민형님)이 같은 정렬 순서 0, 투명·`ZWrite Off` 셰이더로 그린다. 그래서 `FieldOutlineRenderer`가 층마다 0.002씩 올리는 높이로는 그리는 순서가 정해지지 않고, Unity가 카메라 거리로 정한다. 정렬 순서를 정할 입구가 `AreaOutlineData`·핸들 계약에 없었다. **반영(사용자 "이 범위로 A 진행해. AreaOutline 두 파일 수정 허락할게"):** `AreaOutlineData`에 `SortingOrder`(생성자 끝 선택 인자, 기본 0), `AreaOutlineHandle.Update`가 렌더러에 넣고 `Reset`이 0으로 되돌림. 새 정렬 순서 표 `F/FieldSortingOrders.cs`(+ `.meta`, GUID `53014467…`: 채우기 −1, 테두리 0부터 테두리 칸 순, 궤적 100). `FieldOutlineRenderer.Apply`가 테두리 칸 번호로 정렬 순서를 넘김(옛 매개변수 이름 `heightIndex` → `slotIndex`, 생성자에서 테두리 최대 순서가 궤적에 닿으면 예외), `FieldCellFill`·`FieldTrajectoryLine`은 상수 대신 표를 읽음(궤적 1 → 100). 이제 모든 칸 테두리가 층 표 우선순위대로 겹친다. 이동 경로선(`DrawPointLine`)은 입구가 없어 0으로 남음(민형님 공유 §34에 제안) |

Unity 확인 문제 없음(2026-10-27, 사용자 "Unity 확인했어. 문제 없으니 이번 변경까지 스테이지에 다시 올려줘.", ①~③ 모두).

시험(우리 것만, 컴파일·실행 맞춤): 팔레트 생성자 셋(`CombatDisplayReuseTests:245`·`CombatObjectiveDisplayTests:69`·`FieldOutlineRendererTests:64`)에 색 하나, `FieldOutlineRendererTests.FillNames`에 `FieldFill_KnockbackDestination`(채우기 칸이 하나 늘어남, 아웃라인 손잡이 수는 그대로).

결정 둘 반영(위 §7.4의 5·8)은 코드 두 파일(`CombatKnockbackPreview`·`FieldKnockbackMarkers`)만 바꿨다. 사용자 지시대로 이 반영 뒤에는 검토를 다시 돌리지 않았다. Unity 확인 문제 없음(2026-10-27, 사용자 "Unity에서 확인했어. 문제 없어."). **넉백 미리보기 작업(KB-a~KB-c) 끝.** 남은 것은 사용자 커밋(§6 묶음)과 공유 메모 셋 전달이다.
| 9 | 폴더 배치 안내 diff에 다른 작업(Ballista 전략 걷기)의 hunk가 섞임 | 기록: 커밋 때 hunk로 나눔 |
| 10 | `ColorHandle.Dispose`는 Renderer가 먼저 파괴됐으면 바로 돌아가 핸들이 ColorSystem에 반환되지 않을 수 있음(씬을 내릴 때 자식이 먼저 파괴되는 경우) | 기록만: 궤적 부품과 같은 기존 성질, ColorSystem은 민형님 영역 |
