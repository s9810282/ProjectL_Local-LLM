# 전투 공격 궤적 미리보기 계획 (2026-10-19)

- 작성일: 2026-10-19. 코드 변경 없음(계획만).
- 설계: [attack-trajectory-design](../specs/2026-10-19-combat-attack-trajectory-design.md)(TJ1~TJ9, 결정 §7 모두 A, MultiLine UV 고침 보류).
- 사용자 요청 흐름: 레퍼런스 2장과 함께 궤적 점선 요청 → Framework 작업자 의견 "상위 렌더 말고 하위 렌더를 고치거나 더해서 쓰기" → 성능 걱정과 네 기준으로 설계 → "MultiLine UV는 바꾸지 말고 문제 생기면 그때" → "모두 A, Task 계획은 SSOT·SOLID·FW·독립성 염두".
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `D/` = `C/Display/`, `T/` = `Assets/Tests/EditMode/Framework/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`, `DB/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageField/Item/`.
- 성격: TJ-1은 **동작 동일 정리**, TJ-2는 **새 부품(화면 변화 없음, 아직 연결 안 함)**, TJ-3에서 **화면에 궤적이 나온다**.

## 0. 결정과 착수 조건

### 0.1 설계 결정(2026-10-19, 사용자)

| # | 결정 |
|---|---|
| UV | MultiLine UV는 고치지 않는다. 점선은 같은 길이 구간 하나 = 점 하나, `uvScale = 1 / 구간 길이`(설계 TJ7) |
| Q-T1 | 방향 조준도 궤적을 보인다(끝점 = 실제 발사와 같은 `Resolve`) |
| Q-T2 | 막힘 X는 View 프리팹(`StageFieldViewData` 칸) |
| Q-T3 | 지금은 네모 점, 둥근 점은 UV 고침과 함께 보류 |
| Q-T4 | 궤적 전용 재질 칸 + 새 재질 에셋(AreaOutline 셰이더) |
| Q-T5 | 색은 `FieldView` 씬 값 |
| Q-T6 | 좁은 새 계약 `ICombatAttackTrajectoryFeedback` |
| Q-T7 | 막힌 궤적의 X 뒤는 그리지 않는다 |

### 0.2 계획에서 정한 것(P1~P9, 사용자 확인 대상)

| # | 정한 것 | 이유(원칙) |
|---|---|---|
| P1 | 재질은 DB에서 필수(`Validate`). 다만 재질이 없는 설정(시험이 `new StageFieldViewData()`로 만드는 맨 설정)이면 `FieldView`가 궤적 부품을 만들지 않고, 궤적 의도는 아무것도 하지 않는다. | 맨 설정으로 `FieldView`를 구성하는 기존 시험이 10곳 남짓이다. 운영 경로는 DB 검사가 재질을 강제하므로 빠질 수 없다(독립성: 관계없는 시험을 고치지 않음). "필수 여부"의 원본은 `Validate` 한 곳이다(SSOT). |
| P2 | X 표지 프리팹은 **선택 값**(없으면 표지 없음, 궤적은 그대로 끊김). 있으면 Renderer가 있어야 한다. | 아트가 생기기 전에도 DB 생성이 막히지 않는다. 넣으면 바로 보인다. |
| P3 | 궤적 색 둘(가능·막힘)은 `FieldView` 씬 값으로 두되, 칸 강조 팔레트(`FieldHighlightPalette`)에는 넣지 않고 궤적 부품에 바로 넘긴다. | 팔레트 생성자를 쓰는 시험 둘(`CombatObjectiveDisplayTests`·`FieldOutlineRendererTests`)을 건드리지 않는다. 팔레트는 칸 그리기용이고 궤적은 칸이 아니다(S). |
| P4 | 공격자의 지금 위치는 새 좁은 읽기 계약 `ICombatUnitPlacementReader`(`FieldUnitPresenter`가 명시 구현)로 읽는다. | 표시 계약 `ICombatUnitPresentationTarget`이나 대상 묶음 `ICombatUnitTargets`에 멤버를 더하면 시험 가짜 넷을 고쳐야 하고, 그중 `T/CombatPieceRunTests.cs`는 [조각 실행 대여 분리 계획](2026-10-19-combat-piece-run-leases-plan.md)·클립 AC-2가 손대지 않기로 한 파일이다(I·독립성). 읽는 값은 실제 직선 공격의 출발 위치(`Unit.Position`)와 같은 Transform 위치다(SSOT). |
| P5 | 궤적 의도 계약은 `ICombatBattleDisplay`가 상속하고 씬 표시가 구현한다. `AttackTargetingInteraction` 생성자 끝에 **선택 인자**로 받는다(null이면 궤적 없음, `CombatInteraction`의 `path`·`rangeHover` 선례). | 기존 생성 두 곳(`F/CombatSceneInputFactory.cs`, `T/WallAttackInteractionTests.cs`)이 그대로 돈다. 계약 가짜는 `CombatViewSystemTests.FakeDisplay` 하나만 멤버가 는다. |
| P6 | 점선 규칙(같은 길이 구간으로 다시 나누기)은 순수 정적 `D/FieldTrajectorySegments.cs`에 두고 그리기 부품만 부른다. | 관리 실행으로 시험할 수 있다. 규칙 원본이 한 곳이다(SSOT). |
| P7 | 궤적 경로를 만드는 계산(시작·끝·끊는 진행률·표본)은 `CombatAttackTrajectory`의 순수 정적 함수, 창구·유닛 읽기는 얇은 인스턴스 부분으로 나눈다. 그리기 쪽은 좁은 계약 `ICombatTrajectoryCanvas`(`FieldView` 구현)로 받는다. | 경로 계산을 그리기 가짜로 관리 실행에서 시험한다. 무기(MonoBehaviour)가 필요한 곡사·Ballista 시작점만 Unity 시험이다. |
| P8 | 궤적이 지워지는 때에 **결과 재생 직전(`BeforeResult`)**을 넣는다. 영향 칸은 지금처럼 결과 뒤까지 남는다. | 실제 발사체가 날아가는 동안 점선이 겹쳐 보이지 않게 한다(설계 TJ5 "결과 재생 전 지움"). |
| P9 | 새 재질은 `Assets/ProjectL/Prefabs/Combat/Materials/AttackTrajectory.mat`(민형님 `AreaOutline.mat`을 복사해 새 GUID). | 전투 프리팹 재질 폴더에 둔다. 민형님 셰이더 폴더의 원본 재질은 그대로 둔다. |

### 0.3 착수 조건

1. **TJ-1·TJ-2는 바로 가능.** 고치는 파일이 지금 작업 트리의 진행 중 변경(유닛 호버 공격 범위 U-2: `C/CombatInteraction.cs`·`C/CombatPathInput.cs`·`F/CombatSceneInputFactory.cs` 등, 조각 실행 대여 L: `C/CombatPieceRun.cs` 등)과 겹치지 않는다(2026-10-19 `git status` 대조).
2. **TJ-3은 호버 U-2 커밋 뒤.** `F/CombatSceneInputFactory.cs`를 U-2가 지금 고치고 있다. 그 밖의 TJ-3 파일은 겹치지 않는다.
3. **폴더 정리(M)와의 순서는 사용자 결정.** M이 먼저 끝나면 이 계획의 경로를 그 대응표로 고친다(M-4 문서 정리).
4. 기준선(TJ-0)은 착수하는 시점의 HEAD로 잡는다.

## 1. 지금 코드 (HEAD `927962fb` + 작업 트리, 2026-10-19 대조)

| 무엇 | 어디 | 사실 |
|---|---|---|
| 곡선 식 | `D/CombatProjectilePool.cs:282-289`(`Advance`) | `position = Lerp(From, To, t); position += Up * (Height * (4f * t * (1f - t)))`. 직선은 `Height = 0` |
| 곡사 높이 | `D/CombatMortarAnimation.cs:158-159` | 시작 = `_weapon.MuzzlePosition`, 높이 = `_mapper.WorldCellWidth * _settings.ArcHeightInCells`, 위 = `_mapper.WorldUp`(:73) |
| 직선 시작·끝 | `D/CombatLinearAttackAnimation.cs:59-63` | `from = Origin + Up * ProjectileHeight`, `to = Target + Up * ProjectileHeight`, `from += 방향 * Min(.2, 거리)`. `Origin = Unit.Position`(`D/CombatAttackMotion.cs:52`) |
| Ballista 끝 | `D/CombatBallistaAnimation.cs:88-91` | 시작 = 무기 총구, 끝 = `CellCenterToWorld(Endpoint) + Up * ProjectileHeight`, 지나간 칸이 없으면 끝 = 시작 |
| 끝점 해석 | `D/CombatAttackTarget.cs:9` | `Resolve(in AttackResult, mapper, origin)`: 주 칸 중심, 없으면 방향 앞 가장 먼 영향 칸 |
| 접촉 기하 | `D/CombatContactGeometry.cs:24` | `TrySegmentContact(from, to, movingHalf, body, out t)`, 칸 몸 `FieldGridMapper.CellBody` |
| 공격 연출 값·무기 | `D/CombatUnitPresentationWindow.cs:186-230` | `TryGetAttackPresentation`·`TryGetMortarPresentation`(무기 없으면 예외)·`GetBallistaWeapon` |
| 채우기 선례 | `D/FieldCellFill.cs` | GameObject·MeshFilter·MeshRenderer 만들고 Mesh·Color 핸들 등록, 해제는 Color → Mesh → GameObject |
| 필드 수명 | `D/FieldView.cs:72`(`Build`)·`:217`(`Release`) | 같은 크기면 다시 구성 안 함. 색 값은 씬 직렬화(:31-53) |
| 필드 설정 | `DB/StageFieldViewDataSO.cs` | `StageFieldViewData.Validate()`(:108)를 `StageFieldViewTable`이 부른다. 에셋 `DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset` |
| 공격 조준 | `C/AttackTargetingInteraction.cs` | 범위 응답 `:234`, 호버 응답 `:260`, 호버 칸 요청 `:282`, 결과 전 `:170`, 정리 `:412` |
| 입력 조립 | `F/CombatSceneInputFactory.cs:82` | `new AttackTargetingInteraction(input, requests, picker, feedback, gate, audio)` |
| 표시 조립 | `F/CombatSceneDisplayFactory.cs:60` | `new CombatAttackFeedback(field, units.HudFeedback)` |
| 표시 계약 | `C/ICombatBattleDisplay.cs:20` | 피드백 계약들을 상속. 시험 가짜 `T/CombatViewSystemTests.cs:1592`(`FakeDisplay`) |
| 셰이더 | `Script/Render/Shader/View/CustomShader/Outline/AreaOutline.shader`(GUID `512d850e…`) | uv.x만 읽음, `_DashEnabled`·`_DashCount`·`_DashFill`·`_FlowDirection`·`_FlowSpeed`, Transparent·Cull Off |

## 2. 파일 단위 설계

### 2.1 발사 기하 `D/CombatShotPath.cs` (새, TJ-1)

```csharp
/// <summary>실제 발사와 궤적 미리보기가 함께 쓰는 발사 기하입니다(궤적 설계 TJ3). 판정은 하지 않습니다.</summary>
internal static class CombatShotPath
{
    // 원본 0.2 출발 오프셋입니다. 가까운 목표를 넘어서지 않습니다.
    private const float LinearStartOffset = .2f;

    /// <summary>곡선 위 점입니다. 높이가 0이면 직선입니다. 연산 순서는 옛 발사체 풀과 같습니다.</summary>
    internal static Vector3 ArcPoint(Vector3 from, Vector3 to, Vector3 up, float height, float t);

    /// <summary>곡사 높이(월드 길이) = 셀 폭 × 외형 곡사 높이(셀).</summary>
    internal static float ArcHeight(FieldGridMapper mapper, CombatAttackPresentationData settings);

    /// <summary>발사 높이만큼 위로 올린 점입니다.</summary>
    internal static Vector3 Raise(Vector3 point, Vector3 up, float height);

    /// <summary>직선 발사의 시작점입니다. 시작에서 끝 쪽으로 최대 0.2만큼 옮깁니다.</summary>
    internal static Vector3 LinearStart(Vector3 raisedFrom, Vector3 raisedTo);
}
```

- `D/CombatAttackTarget.cs`에 형태 하나 추가: `Resolve(CellCoord? primaryCell, Rotation4 direction, ReadOnlySpan<CellCoord> affected, FieldGridMapper mapper, Vector3 origin)`. 기존 `Resolve(in AttackResult …)`는 이것을 부른다. 미리보기(`AttackPreview`)도 같은 세 값을 가진다.
- 부르는 곳: `CombatProjectilePool.Advance`(`ArcPoint`), `CombatMortarAnimation.Fire`(`ArcHeight`), `CombatLinearAttackAnimation.Advance`(`Raise`·`LinearStart`), `CombatBallistaAnimation`(`Raise`).
- **부동소수 같은 값:** 식을 옮길 때 연산 순서를 그대로 둔다(`Lerp` 뒤 `+= up * (height * (4f * t * (1f - t)))`). 기존 시험이 같은 값을 기대한다.

### 2.2 점선 규칙 `D/FieldTrajectorySegments.cs` (새, TJ-2, 순수 정적)

```csharp
/// <summary>
/// 경로를 같은 길이 구간으로 다시 나눕니다. 지금 MultiLine UV에서 구간 하나에 점 하나가 찍히는 규칙의 원본입니다(궤적 설계 TJ7).
/// </summary>
internal static class FieldTrajectorySegments
{
    /// <summary>구간 수 상한입니다. 필드 대각선을 가장 짧은 간격으로 나눠도 넘지 않는 값입니다.</summary>
    internal const int MaxSegments = 256;

    internal static float Length(Vector3[] path, int count);
    /// <summary>max(1, round(길이 / 간격))이며 상한을 넘지 않습니다.</summary>
    internal static int SegmentCount(float length, float spacing);
    /// <summary>호 길이 기준으로 같은 간격의 점 segments + 1개를 output에 채웁니다. output 길이는 segments + 1이어야 합니다.</summary>
    internal static void Resample(Vector3[] path, int count, float length, int segments, Vector3[] output);
}
```

### 2.3 그리기 부품 `D/FieldTrajectoryLine.cs` (새, TJ-2)

- 꼴은 `FieldCellFill`과 같다: `Build` / `Show` / `Clear` / `Release`, 생성자로 부모·재질·`IMeshSystemService`·`IColorSystemService`·값 묶음(두께·간격·채움·흐름 속도·가능 색·막힘 색)·X 표지 프리팹(선택)을 받는다.
- `Build`: GameObject 하나(그림자 끔, 렌더러 꺼 둠), Mesh·Color 핸들 등록, 셰이더 고정 값 한 번 설정(`_DashEnabled = 1`·`_DashCount = 1`·`_FlowDirection = 1`·`_DashFill`·`_FlowSpeed`), 표지가 있으면 하나 만들어 꺼 둠. 실패하면 만든 것을 거두고 원인 전파(`FieldCellFill.Build`와 같음).
- `Show(Vector3[] localPath, int count, bool blocked)`:
  1. 길이·구간 수 → 구간 수별 캐시 배열(`Vector3[N + 1]`, 처음 쓸 때 한 번 만듦)에 다시 나누기.
  2. `IMeshRenderHandle.Update(new MultiLineCreateData(이름, 배열, 두께, 1 / 구간 길이, Color.white))`(구체 DTO, FW-010).
  3. 색은 상태(가능·막힘)가 바뀔 때만 `MainColor` 덮어쓰기.
  4. 렌더러 켬. 막힘이고 표지가 있으면 마지막 점에 표지를 두고 켬, 아니면 끔.
- `Clear`: 렌더러·표지 끔(멱등).
- `Release`: Color → Mesh `Dispose`, 표지·GameObject 파괴(FW-011). 여러 번 불려도 안전.
- 좌표는 부모(`FieldView.cellsRoot`) 로컬이다.

### 2.4 필드 설정 값 `DB/StageFieldViewDataSO.cs` (TJ-2)

새 묶음 `[Header("Attack Trajectory")]`:

| 칸 | 형식 | 검사(`Validate`, 표시 모드와 무관) | 기본 |
|---|---|---|---|
| `trajectoryMaterial` | Material | 필수(P1) | 새 `AttackTrajectory.mat` |
| `trajectoryThickness` | float | 유한, > 0 | 0.08 |
| `trajectoryDotSpacing` | float | 유한, > 0 (월드 길이) | 0.3 |
| `trajectoryDotFill` | float | (0, 1] | 0.45 |
| `trajectoryFlowSpeed` | float | 유한, ≥ 0 | 0.6 |
| `trajectoryBlockedMarker` | GameObject | 선택(P2), 있으면 Renderer 필요 | 없음 |

- 에셋 `StageFieldView_Default.asset`에 값과 재질 참조를 넣는다. **코드와 에셋은 한 커밋**(없으면 DB 생성 실패).

### 2.5 `D/FieldView.cs` (TJ-2)

- 색 두 칸: `trajectoryColor`(따뜻한 흰색), `trajectoryBlockedColor`(빨강). 기존 씬 인스턴스는 필드 초기값을 받는다.
- `Build` 끝: 설정에 재질이 있으면 `FieldTrajectoryLine`을 만들고 구성(P1). `Release`에서 해제.
- `ICombatTrajectoryCanvas`(새 좁은 계약, TJ-3에서 쓰지만 TJ-2에서 같이 둠) 구현: `Mapper`, `ShowAttackTrajectory(Vector3[] localPath, int count, bool blocked)`, `ClearAttackTrajectory()`. 부품이 없으면 둘 다 아무것도 하지 않는다(P1).

### 2.6 궤적 구성 `D/CombatAttackTrajectory.cs` (새, TJ-3)

- 순수 정적 부분(P7): `BuildPath(kind, worldFrom, worldTo, up, arcHeight, mapper, directWallCells, pathBuffer) → (count, blocked)`.
  - 직선: 2점 `[from, Lerp(from, to, t끊음)]`. 곡사: 33점 `ArcPoint(from, to, up, h, t끊음 × i / 32)`.
  - 끊는 진행률(Direct 벽만, 설계 TJ6): 바닥 투영 `TrySegmentContact(Flatten(from), Flatten(to), 0, CellBody(벽))`의 최솟값, 못 찾으면 첫 벽 칸 중심을 `ProjectOnSegment`.
  - 결과는 `mapper.WorldToLocal`로 바꿔 고정 버퍼(직선 2·곡사 33)에 채운다.
- 인스턴스 부분: 생성자로 `ICombatTrajectoryCanvas`·`CombatUnitPresentationWindow`·`ICombatUnitPlacementReader`.
  - `Show(EntityId attacker, in AttackPreview preview)`: 외형 종류를 창구에서 읽음(근접·없음 → 지움), 시작(곡사·Ballista = 무기 총구, 직선 = `LinearStart(Raise(위치), Raise(끝))`), 끝(곡사 = 주 칸 중심, 직선 = `Resolve` + 높이, Ballista = `PiercingShot.Endpoint` + 높이), Direct 벽 칸을 응답의 `BlockedContacts`에서 거른다.
  - `ShowBlocked(EntityId attacker, CellCoord target, IReadOnlyList<AttackBlockedContact> contacts)`: 끝 = 대상 칸(직선은 높이), Direct가 하나도 없으면 지움.
  - `Clear()`.
  - **같은 입력 건너뜀:** 마지막 (공격자, 종류, 시작, 끝, 끊는 진행률, 막힘)과 같으면 캔버스를 부르지 않는다.
- "어느 증거가 궤적을 끊나(Direct만)"와 "어느 외형이 궤적이 없나(근접)"의 원본은 이 클래스 하나다.

### 2.7 읽기 계약 `D/ICombatUnitPlacementReader.cs` (새, TJ-3)

```csharp
/// <summary>표시 중인 유닛의 지금 위치를 읽기만 합니다(궤적 미리보기의 직선 시작점). 대여·자세를 바꾸지 않습니다.</summary>
internal interface ICombatUnitPlacementReader
{
    bool TryGetPlacedPosition(EntityId id, out Vector3 position);
}
```

- `D/FieldUnitPresenter.cs`가 명시 구현: 대여 중이고 켜진 표시면 그 Transform 위치.

### 2.8 궤적 의도 계약 `C/ICombatAttackTrajectoryFeedback.cs` (새, TJ-3)

```csharp
/// <summary>공격 조준이 호버 응답·범위 증거로 궤적 표시 의도만 전합니다(궤적 설계 TJ2·TJ5). 구현은 멱등이어야 합니다.</summary>
internal interface ICombatAttackTrajectoryFeedback
{
    void ShowAttackTrajectory(EntityId attackerId, in AttackPreview preview);
    void ShowBlockedAttackTrajectory(EntityId attackerId, CellCoord target, IReadOnlyList<AttackBlockedContact> contacts);
    void ClearAttackTrajectory();
}
```

- `C/ICombatBattleDisplay.cs`가 상속, `C/CombatSceneDisplay.cs`가 구현해 `CombatAttackFeedback`에 넘김, `C/CombatAttackFeedback.cs`가 `CombatAttackTrajectory`를 가진다(생성자 인자).
- `F/CombatSceneDisplayFactory.cs:60`: `new CombatAttackFeedback(field, units.HudFeedback, new CombatAttackTrajectory(field, units.Presentation, units))`.

### 2.9 공격 조준 `C/AttackTargetingInteraction.cs` (TJ-3)

- 생성자 끝 선택 인자 `ICombatAttackTrajectoryFeedback trajectory = null`(P5).
- "어느 조준 종류가 궤적을 보이나"(칸·방향 = 보임, 전체 = 안 보임, Q-T1)는 이 클래스의 함수 하나.
- 부르는 자리:

| 자리 | 동작 |
|---|---|
| `ApplyAttackAnswer` 가능 응답 | 조준 종류가 보이는 종류면 `ShowAttackTrajectory(_actor, attack)`, 아니면 지움 |
| `ApplyAttackAnswer` 불가 응답 | 응답 증거가 있으면 `ShowBlockedAttackTrajectory(_actor, 호버 칸, attack.BlockedContacts)`, 없으면 지움 |
| `RequestHoverForCell` | 먼저 지움, 범위 증거가 있는 칸이면 `ShowBlockedAttackTrajectory(_actor, cell, contacts)` |
| `PointerMoved` 칸 없음, `PreviewAnswered` 거절, `BeforeResult`(P8), `ResetRangeAnswer`(모드 종료·결과 뒤) | 지움 |

- Direct만 거르는 일은 하지 않는다(그 원본은 §2.6).
- `F/CombatSceneInputFactory.cs`: `CreateInteraction`에 같은 선택 인자, `CreateSceneInteraction`이 `display`를 넘긴다.

## 3. Phase

Phase마다 멈추고 보고한다. 보고 끝에 다음 Phase의 목표·파일·주의점·확인 방법을 적는다.

### TJ-0 기준선

- **T0.1** 착수 시점 HEAD에서 MSBuild(시험·에디터·비개발 정의) 오류·경고 수, 관리 실행 전체 결과(PASS·NATIVE·FAIL·UnityTest)를 기록한다. 관리 실행 도구는 세션 스크래치에 다시 만든다(메모 [[projectl-build-check]]).
- **T0.2** 진행 중인 다른 작업(U-2·L)이 그사이 커밋됐는지 보고 §0.3을 다시 대조한다.

### TJ-1 발사 기하 공유 (동작 동일)

- **T1.1** `D/CombatShotPath.cs`(+ `.meta`) 작성(§2.1).
- **T1.2** `CombatProjectilePool.Advance`가 `ArcPoint`를 부른다(연산 순서 그대로).
- **T1.3** `CombatMortarAnimation`이 `ArcHeight`, `CombatLinearAttackAnimation`이 `Raise`·`LinearStart`, `CombatBallistaAnimation`이 `Raise`를 부른다. 0.2 상수는 `CombatShotPath`로 옮기고 옛 자리 주석도 옮긴다.
- **T1.4** `CombatAttackTarget.Resolve` 새 형태, 옛 형태는 위임.
- **T1.5** 새 시험 `T/CombatShotPathTests.cs`(순수): 곡선 양 끝 = from·to, 꼭대기(t = 0.5) = 높이, 높이 0이면 `Lerp`와 같음, 옛 식과 비트 단위로 같음(여러 t), 0.2 오프셋은 거리보다 길지 않음, `Resolve` 두 형태가 같은 값.
- **확인:** MSBuild 오류·새 경고 0, 관리 실행 기존 시험 상태 변화 0, Unity 묶음 `CombatProjectileTests`·`CombatMortarAnimationTests`·`CombatMortarPresentationTests`·`CombatBallistaAttackTests`·`CombatBallistaLifecycleTests`·`CombatStandardAttackTests`(직선, 사용자 확인).
- **원칙:** 발사 기하의 원본이 네 곳에서 한 곳으로(SSOT). 화면·완료 시점·예외 문구가 바뀌지 않는다.

### TJ-2 그리기 부품·값 (화면 변화 없음)

- **T2.1** `D/FieldTrajectorySegments.cs`(+ `.meta`) 작성(§2.2), 시험 `T/FieldTrajectorySegmentsTests.cs`(순수): 구간 수 공식·상한, 직선은 구간 길이가 모두 같음, 곡선 경로(촘촘한 33점)도 구간 길이 차이 1% 안, 출력 양 끝 = 경로 양 끝, 짧은 경로(간격보다 짧음)는 구간 1.
- **T2.2** `StageFieldViewData` 값 묶음·검사(§2.4). 새 재질 에셋 `AttackTrajectory.mat`(+ `.meta`, P9). `StageFieldView_Default.asset`에 값·재질 참조.
- **T2.3** `D/FieldTrajectoryLine.cs`(+ `.meta`) 작성(§2.3).
- **T2.4** `D/FieldView.cs` 색 두 칸·구성·해제·`ICombatTrajectoryCanvas` 구현(§2.5), `D/ICombatTrajectoryCanvas.cs`(+ `.meta`).
- **T2.5** 시험: `T/FieldTrajectoryLineTests.cs`(Unity — 구성·표시·지움·해제, 셰이더 고정 값, 같은 구간 수 배열 재사용, 색은 상태가 바뀔 때만, 표지 위치·켜짐), 설정 검사 사례(재질 없음·간격 0·채움 범위 밖·표지 Renderer 없음 → 예외), `DataBaseAssetTests`(실제 에셋 통과).
- **T2.6** 호진님 공유 메모 새로 작성(`StageFieldViewData` 새 묶음·재질 에셋, 미전달).
- **확인:** MSBuild 오류·새 경고 0, 관리 실행 기존 시험 상태 변화 0, Unity 묶음 새 시험 + `DataBaseAssetTests` + 필드 시험(`FieldOutlineRendererTests` 등, 맨 설정 호환 확인)(사용자 확인). Combat 진입이 그대로 되는지(사용자 확인).
- **원칙:** 점선 규칙은 이 Phase의 두 파일에만(SSOT·S). 아직 아무도 부르지 않는다(독립성: 화면 변화 없음).

### TJ-3 구성·입력 연결 (화면에 궤적이 나옴)

- **T3.1** `D/ICombatUnitPlacementReader.cs`(+ `.meta`), `FieldUnitPresenter` 명시 구현(§2.7).
- **T3.2** `D/CombatAttackTrajectory.cs`(+ `.meta`) 작성(§2.6).
- **T3.3** `C/ICombatAttackTrajectoryFeedback.cs`(+ `.meta`), `ICombatBattleDisplay` 상속, `CombatSceneDisplay` 구현, `CombatAttackFeedback` 생성자 인자(§2.8). `T/CombatViewSystemTests.cs` `FakeDisplay`에 멤버 셋, `T/CombatAttackFeedbackTests.cs` 생성 한 줄.
- **T3.4** `F/CombatSceneDisplayFactory.cs:60` 연결, `AttackTargetingInteraction`·`F/CombatSceneInputFactory.cs` 연결(§2.9).
- **T3.5** 시험:
  - `T/CombatAttackTrajectoryTests.cs`(순수 `BuildPath` — 직선 2점·곡사 33점·꼭대기 높이, Direct만 끊음·Propagation 무시, 대체 투영, 로컬 좌표 변환; 인스턴스 — 근접 외형은 지움, 같은 입력이면 캔버스 호출 0회. 곡사·Ballista 총구는 Unity 사례).
  - `T/AttackTrajectoryInteractionTests.cs`(순수, `WallAttackInteractionTests`의 조립 방식): 가능 응답 → 보임, 전체 조준 → 안 보임, 방향 조준 → 보임, 막힌 칸 → 막힌 궤적, 칸 바뀜·거절·결과 전·모드 종료 → 지움, 선택 인자 null이면 아무 일 없음.
- **T3.6** 민형님 Factory 공유 문서 다음 절(지금 마지막 §16, 호버 U-2가 §17 예정 → §18): Factory 한 줄·입력 Factory 선택 인자. 하위 MultiLine UV에 기대는 점과 "고쳐도 궤적은 같다"(설계 TJ7)도 함께 적는다.
- **T3.7** 재생 확인 목록(사용자): 직선 유효 / 직선 막힘(X·끊김) / 곡사가 벽을 넘음 / Ballista 끝점 / 방향 조준(샷건) / 근접은 없음 / 칸을 빠르게 옮겨도 끊김 없음 / 공격 실행하면 궤적이 사라짐 / 모드 끄면 사라짐. 선택: Profiler에서 포인터를 멈춘 프레임 GC Alloc 0.
- **확인:** MSBuild 오류·새 경고 0, 관리 실행 기존 시험 상태 변화 0, Unity 묶음(새 시험 + `CombatInteractionTests`·`HojinCombatAttackDragTests`·`HojinCombatAttackDragIntegrationTests`·`WallAttackInteractionTests`·`CombatAttackFeedbackTests`·`CombatViewSystemTests`), 재생 확인(사용자).
- **원칙:** 언제(입력)·무엇(구성)·어떻게(그리기)가 각각 한 곳(S). 새 계약 둘은 좁다(I). 기존 가짜는 `FakeDisplay` 하나만 늘어난다(독립성).

## 4. 원칙 점검표 (Phase마다 보고에 적는다)

| 기준 | 지킬 것 | 확인 방법 |
|---|---|---|
| SSOT | 발사 기하는 `CombatShotPath`, 점선 규칙은 `FieldTrajectorySegments`, Direct만 끊는 규칙·근접 없음은 `CombatAttackTrajectory`, 보이는 조준 종류는 `AttackTargetingInteraction`, 값은 `StageFieldViewData`·`FieldView` 한 곳씩. 막힘·끝 칸은 Core 응답만 쓴다 | 같은 식·상수(0.2, `4t(1−t)`, 구간 공식)를 grep해 한 곳인지 본다 |
| SOLID | 그리기 부품은 공격을 모른다. 구성 부품은 핸들·점선 규칙을 모른다. 새 계약 셋(`ICombatAttackTrajectoryFeedback`·`ICombatTrajectoryCanvas`·`ICombatUnitPlacementReader`)은 각자 쓰는 쪽에 필요한 만큼만 | 각 파일의 using·생성자 인자로 확인 |
| FW 지침·계약 | 새 서비스 등록 없음(FW-001·002, 내부 협력 객체는 등록하지 않음). Mesh는 구체 DTO(FW-010), 반환은 핸들 `Dispose`(FW-011), Build 뒤 구조 불변(§11), Tick·Find 없음(§15·25), 반복 할당 없음(§19) | 코드 검토 + `FieldTrajectoryLineTests` |
| 독립성 | Phase별 고치는 파일이 §3 목록 밖으로 나가지 않는다. 다른 진행 중 작업의 파일을 건드리지 않는다(§0.3). 맨 설정 시험·팔레트 시험을 고치지 않는다(P1·P3) | `git diff --stat`을 Phase 목록과 대조 |

## 5. 위험과 대응

| 위험 | 대응 |
|---|---|
| TJ-1에서 식을 옮기며 부동소수 값이 달라짐 | 연산 순서를 그대로 두고, 옛 식과 비트 단위 비교 시험(T1.5) |
| 맨 설정 시험이 재질 검사에 걸림 | 검사는 DB 표(`StageFieldViewTable`)에서만 돈다. `FieldView`는 재질이 없으면 부품을 만들지 않는다(P1) |
| 호버 U-2·조각 실행 대여 L과 같은 파일 | TJ-3은 U-2 커밋 뒤(§0.3). L의 파일은 이 계획에 없다 |
| 누군가 MultiLine UV를 먼저 고침 | `uvScale = 1 / 구간 길이`라 화면이 같다(설계 TJ7). 재생 확인만 다시 한다 |
| 경로 양 끝 반쪽 점·네모 점이 거슬림 | 보류 항목(UV 고침 + 셰이더 위상·둥근 점)으로 넘긴다(설계 O-6·Q-T3) |
| 무기 없는 곡사 외형에서 창구가 예외 | 지금 실제 공격과 같은 규칙(무기 연결 누락은 데이터 오류). 궤적이 먼저 드러낼 뿐이다 |
| 하위 MultiLine 빌더의 갱신당 List 할당 | Hot Path가 아니다. 관찰로만 공유 문서에 적는다(설계 O-1) |

## 6. 커밋 묶음 안내 (사용자가 커밋)

- TJ-1: 새 `D/CombatShotPath.cs`·`T/CombatShotPathTests.cs`(+ `.meta`), 고친 연출 파일 다섯.
- TJ-2: 새 `D/FieldTrajectorySegments.cs`·`D/FieldTrajectoryLine.cs`·`D/ICombatTrajectoryCanvas.cs`·시험 둘(+ `.meta`), **`StageFieldViewDataSO.cs`와 `StageFieldView_Default.asset`·`AttackTrajectory.mat`(+ `.meta`)은 한 커밋**, `FieldView.cs`, 호진님 메모.
- TJ-3: 새 `D/CombatAttackTrajectory.cs`·`D/ICombatUnitPlacementReader.cs`·`C/ICombatAttackTrajectoryFeedback.cs`·시험 둘(+ `.meta`), 고친 파일(§3 TJ-3 목록), 민형님 공유 절.
- `FMODStudioCache.asset` 등 에디터가 바꾼 파일은 이 작업이 아니다.

## 7. 실행 기록

### 7.1 TJ-1 발사 기하 공유 (2026-10-19, 코드 완료, Unity 컴파일·시험 문제 없음 — 사용자 확인)

- **사용자 지시:** "TJ-0 스킵, TJ-1부터, P1~P9 그대로". 착수 전 겹침 확인: 호버 U-2(`950a4b34`)·조각 실행 대여 L(`3c50a379`)은 커밋됨, TJ-1 파일은 작업 트리 변경 없음. 클립 계획은 이 변경을 이미 반영(§0.7 CC5 — T20은 옛 `Resolve` 형태를 부름). 폴더 정리(M)는 착수 전이라, 착수하면 대응표에 `CombatShotPath.cs`·`CombatShotPathTests.cs` 두 줄이 필요하다.
- **고친 것:**
  - 새 `D/CombatShotPath.cs`(+ `.meta`): `ArcPoint`·`ArcHeight`·`Raise`·`LinearStart`, 0.2 상수 원본.
  - `D/CombatProjectilePool.cs`(`Advance` → `ArcPoint`), `D/CombatMortarAnimation.cs`(`ArcHeight`), `D/CombatLinearAttackAnimation.cs`(`Raise`·`LinearStart`), `D/CombatBallistaAnimation.cs`(`Raise`).
  - `D/CombatAttackTarget.cs`: 새 형태 `Resolve(CellCoord?, Rotation4, ReadOnlySpan<CellCoord>, mapper, origin)`, 옛 `Resolve(in AttackResult …)`는 한 줄 위임.
  - 새 시험 `T/CombatShotPathTests.cs`(+ `.meta`) 9개.
- **검증:**
  - MSBuild(`ProjectL.Tests.Framework`, Unity가 이미 새 파일을 csproj에 넣어 추가 targets 없음) 오류 0, 바뀐 파일의 경고 0. 이후 사용자 지시 "MS 빌드 건너뛰어"로 추가 빌드는 하지 않는다.
  - 관리 실행: 새 시험 PASS 8(옛 풀·직선 식과 비트 단위로 같음 포함), Unity 전용 1(`Resolve_…`, GameObject 필요). 기존 `CombatProjectileTests` 18개는 전부 Unity 전용(관리 실행 판정 불가), 나머지 기존 연출 시험은 사용자 지시로 돌리다 멈춤.
  - TJ-0 기준선이 없어 "기존 시험 상태 변화 0"은 관리 실행으로 확인하지 않았다 → Unity 묶음이 증거.
- **Unity 확인 목록(사용자):** `CombatShotPathTests`(9개), `CombatProjectileTests`, `CombatMortarAnimationTests`, `CombatMortarPresentationTests`, `CombatStandardAttackTests`, `CombatBallistaAttackTests`, `CombatBallistaLifecycleTests`, `CombatAttackPresenterTests`. 재생으로 직선·곡사·Ballista 공격이 전과 같은지.
- **원칙:** 발사 기하가 네 파일 → `CombatShotPath` 한 곳(SSOT). 식은 연산 순서를 그대로 옮겨 값이 같다. 다른 진행 중 작업 파일을 건드리지 않았다(독립성).
- **커밋 묶음:** 새 파일 둘(+ `.meta`)과 고친 파일 다섯.

### 7.2 TJ-2 그리기 부품·값 (2026-10-19, 코드 완료, Unity 컴파일·시험 문제 없음 — 사용자 확인)

- **사용자 지시:** "TJ-2 진행, MSBuild는 건너뛰고". 그래서 MSBuild도 관리 실행(빌드 산출물 필요)도 하지 않았다 — **컴파일 미확인**. 겹침: 다른 세션의 경로 입력 분리(R-1·R-2, `CombatMovementRangeTracker` 등)는 TJ-2 파일을 고치지 않는다.
- **새 파일(+ `.meta`):**
  - `D/FieldTrajectorySegments.cs` — 점선 규칙 원본(구간 수 = max(1, round(길이 / 간격)), 상한 256, 호 길이 기준 다시 나누기, 할당 없음).
  - `D/ICombatTrajectoryCanvas.cs` — `Mapper`·`ShowAttackTrajectory`·`ClearAttackTrajectory`.
  - `D/FieldTrajectoryLine.cs` — `FieldCellFill` 꼴. 구성 때 GameObject(꺼 둠)·표지·Mesh·Color 핸들·셰이더 고정 값(`_DashEnabled 1`·`_DashCount 1`·`_DashFill`·`_FlowDirection 1`·`_FlowSpeed`) 한 번, `Show`는 다시 나눈 점으로 `MultiLineCreateData`(`uvScale = 구간 수 / 길이`), 색은 상태가 바뀔 때만, 구간 수별 배열 재사용, 막힘이면 끝점에 표지, 정렬 순서 1(칸 테두리 위).
  - `Prefabs/Combat/Materials/AttackTrajectory.mat` — `AreaOutline.mat` 복사(이름·`_DashCount 1`·`_DashFill .45`·`_FlowSpeed .6`·흰색, 발광 끔).
  - 시험 `T/FieldTrajectorySegmentsTests.cs`(순수 7개), `T/FieldTrajectoryLineTests.cs`(Unity 10개).
- **고친 파일:**
  - `DB/StageFieldViewDataSO.cs` — "Attack Trajectory" 묶음 여섯 칸·속성·`ValidateTrajectory`(재질 필수 P1, 표지 선택 P2·있으면 Renderer).
  - `StageFieldView_Default.asset` — 값과 재질 참조, 표지는 비움.
  - `D/FieldView.cs` — 색 두 칸(`trajectoryColor`·`trajectoryBlockedColor`, 칸 팔레트 밖 P3), `ICombatTrajectoryCanvas` 명시 구현, `Build` 끝에 재질이 있을 때만 궤적 부품(P1), `Release`·`OnDestroy`에서 해제(궤적 해제가 실패해도 표시기 해제는 함).
  - `T/ViewDataTableTests.cs` — 설정 도우미 둘(`FieldView()`·`OutlineFieldView()`)에 궤적 재질(필수가 되어서), 새 시험 `StageFieldView_AttackTrajectoryValuesAreChecked`. **기존 시험을 고친 곳은 이 도우미 둘뿐**이다(계획에 없던 수정 — P1의 직접 결과).
- **공유:** 호진님 메모 `handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md`(미전달).
- **Unity 확인 목록(사용자):** 컴파일, `FieldTrajectorySegmentsTests`·`FieldTrajectoryLineTests`·`ViewDataTableTests`·`DataBaseAssetTests`(실제 에셋), 필드 시험(`FieldOutlineRendererTests`·`CombatAttackFeedbackTests`·`CombatPlacementFeedbackTests`, 맨 설정 호환), TJ-1 목록(§7.1). Combat 진입이 그대로 되는지. 화면 변화는 없다(아직 아무도 궤적을 부르지 않음).
- **원칙:** 점선 규칙은 `FieldTrajectorySegments`·`FieldTrajectoryLine` 두 파일에만(SSOT·S). 값은 DB 한 곳·색은 씬 한 곳. 새 서비스 없음, 구체 DTO(FW-010), 핸들 `Dispose`(FW-011), Tick·Find 없음. 칸 팔레트·맨 설정 시험은 건드리지 않음(독립성).
- **커밋 묶음:** 위 새 파일(+ `.meta`), **`StageFieldViewDataSO.cs`·`StageFieldView_Default.asset`·`AttackTrajectory.mat`(+ `.meta`)은 한 커밋**, `FieldView.cs`, `ViewDataTableTests.cs`, 호진님 메모.

### 7.3 TJ-3 구성·입력 연결 (2026-10-19, 코드 완료, Unity 컴파일·시험·재생 문제 없음 — 사용자 확인)

- **사용자 지시:** TJ-1·TJ-2 Unity "문제 없음" 뒤 "TJ-3 진행해". MSBuild는 앞 지시대로 건너뛰어 **컴파일 미확인**. 착수 조건: 호버 U-2는 `950a4b34`로 커밋됨. 작업 트리에 다른 세션의 경로 입력 분리(R) 변경이 있으나 TJ-3 파일과 겹치지 않는다(`CombatInteraction`·`CombatPathInput`·`CombatUnitHudFeedback` 등은 손대지 않음). 클립 T12(`FieldUnitPresenter`)는 착수 전.
- **새 파일(+ `.meta`):**
  - `D/ICombatUnitPlacementReader.cs` — 위치 읽기(P4). `FieldUnitPresenter` 명시 구현: 대여 중·켜진 표시의 Transform 위치(실제 공격 자세 위치 `FieldUnitView.cs:402`와 같은 값).
  - `D/CombatAttackTrajectory.cs` — 순수 `CutProgress`(Direct 벽 칸 몸에 처음 닿는 진행률, 못 닿으면 첫 벽 칸 중심 투영)·`FillPath`(직선 2점, 곡사 33점 `ArcPoint`), 인스턴스 `Show`(가능)·`ShowBlocked`(막힘)·`Clear`, 외형별 시작·끝(곡사 = 총구 → 주 칸, Ballista = 총구 → 관통 종점 + 높이, 직선 = `LinearStart` → `Resolve` + 높이, 근접·표시 없음 = 지움), 로컬 좌표 고정 버퍼, 같은 입력 건너뜀.
  - `C/ICombatAttackTrajectoryFeedback.cs` — 표시 의도 셋(Q-T6 A).
  - 시험 `T/CombatAttackTrajectoryTests.cs`(순수 5·Unity 4), `T/AttackTrajectoryInteractionTests.cs`(6, 실제 Core 범위·미리보기 계산).
- **고친 파일:** `C/ICombatBattleDisplay.cs`(상속), `C/CombatSceneDisplay.cs`(위임 셋, 해제 뒤 무시), `C/CombatAttackFeedback.cs`(생성자 인자·위임 셋), `C/AttackTargetingInteraction.cs`(끝 선택 인자, 보이는 조준 종류 `ShowsTrajectory` 원본, 부르는 자리 — 가능 응답·불가 응답 증거·범위 증거·칸 밖·거절·`BeforeResult`(P8)·`ResetRangeAnswer`), `D/FieldUnitPresenter.cs`(위치 계약), `F/CombatSceneDisplayFactory.cs:60`, `F/CombatSceneInputFactory.cs`(끝 선택 인자 `trajectory`, 씬 조립만 `display`), 시험 `T/CombatViewSystemTests.cs`(`FakeDisplay` 멤버 셋), `T/CombatAttackFeedbackTests.cs`(생성 한 줄).
- **계획과 다른 점(보정):**
  1. §2.6은 가능 응답도 응답의 Direct 증거로 거른다고 적었지만, 가능 응답은 **끊지 않는다**(공격이 대상에 닿으므로). 끊는 것은 막힌 궤적뿐이다 — 설계 TJ5 표와 같다.
  2. 입력 시험은 `WallAttackInteractionTests`에 더하지 않고 새 파일로 썼다(그 파일은 천명님이 처음 만든 파일). 조립 도우미 약 20줄이 겹친다.
- **공유:** 민형님 Factory 공유 문서 §18(하위 Render 사용·MultiLine UV 상태 포함, 미전달).
- **Unity 확인 목록(사용자):** 컴파일, 새 시험 둘, `CombatAttackFeedbackTests`·`CombatViewSystemTests`·`CombatInteractionTests`·`WallAttackInteractionTests`·`HojinCombatAttackDragTests`·`HojinCombatAttackDragIntegrationTests`.
- **재생 확인(사용자, 계획 T3.7):** 직선 유효 / 직선 막힘(끊김·막힘 색, X 표지는 프리팹을 넣었을 때만) / 곡사가 벽을 넘음 / Ballista 끝점 / 방향 조준(샷건) / 근접은 없음 / 전체 조준은 없음 / 칸을 빠르게 옮겨도 끊김 없음 / 공격 실행하면 궤적이 사라지고 결과 뒤 같은 칸이면 다시 보임 / 모드를 끄면 사라짐. 선택: Profiler에서 포인터를 멈춘 프레임 GC Alloc 0.
- **원칙:** 언제(공격 조준 `ShowsTrajectory`)·무엇(구성 — Direct만 끊음·근접 없음)·어떻게(그리기 — 점선 규칙)가 각각 한 곳(SSOT·S). 새 계약 셋은 좁다(I) — 기존 가짜는 `FakeDisplay` 하나만 늘었다(독립성). 새 서비스 등록 없음(FW-001·002), Tick·Find 없음.
- **커밋 묶음:** 위 새 파일(+ `.meta`)과 고친 파일, 민형님 공유 §18. 작업 트리의 경로 입력 분리(R) 변경과는 다른 묶음이다.

### 7.4 구현 평가 개선 적용 (2026-10-19, 코드 완료, Unity 컴파일·시험·재생 문제 없음 — 사용자 확인)

- **근거:** 설계 §10(구현 뒤 원칙 평가 8.0) → 사용자 결정 "P-V1·P-V2·P-V5·P-V6 적용, MSBuild 건너뛰고" → **컴파일 미확인**.
- **고친 파일:** `D/CombatShotPath.cs`(`TryTarget`), `C/CombatAttackPlayback.cs`(직접 공격·곡사 목표점, 동작 동일), `D/CombatBallistaAnimation.cs`(관통 종점, 동작 동일), `D/CombatAttackTrajectory.cs`(두 갈래 → `Draw`·`Segment` 하나, P-V5·P-V6), `C/AttackTargetingInteraction.cs`(`ClearHoverDisplay`), 시험 `T/CombatShotPathTests.cs`·`T/CombatAttackTrajectoryTests.cs`.
- **겹침:** 다른 세션의 경로 입력 분리(R) 변경 파일과 겹치지 않는다. `CombatAttackPlayback.cs`는 이번에 처음 고쳤다(작업 트리 변경 없었음).
- **Unity 확인 목록(사용자):** 컴파일, `CombatShotPathTests`·`CombatAttackTrajectoryTests`·`AttackTrajectoryInteractionTests`, **실제 공격 연출(동작 동일 확인)** `CombatStandardAttackTests`·`CombatMortarAnimationTests`·`CombatMortarPresentationTests`·`CombatBallistaAttackTests`·`CombatBallistaLifecycleTests`·`CombatAttackPresenterTests`, 재생(직선·곡사·Ballista 공격이 전과 같은지, §7.3 궤적 재생 목록).
- **점수:** 8.0 → 8.5(설계 §10.8, 남는 낮음 V8 새로 적음).
