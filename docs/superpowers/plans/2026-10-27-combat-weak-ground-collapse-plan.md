# 전투 약한 지반 붕괴 계획 (2026-10-27)

- 작성일: 2026-10-27. 코드 변경 없음(계획만).
- 설계: [weak-ground-collapse-after-move-design](../specs/2026-10-27-combat-weak-ground-collapse-after-move-design.md)(WG1~WG7, P1~P4, 2판).
- 사용자 요청 흐름: "약한 지반을 켜고 붕괴 시점을 이동 후로" → 시점 A(이동 명령 끝만) → 설계 초안 → "모두 권장안대로 하되 Q-WG2는 넉백으로 밟은 것도 포함" → "Q-WG8은 A(그 공격 명령 끝)로 하고 Task 계획 써줘".
- 경로 약칭: `Core/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/`, `C/` = `…/ViewSystem/Combat/`, `M/` = `C/Display/Map/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`, `T/` = `Assets/Tests/EditMode/`, `E/` = `Assets/ProjectL/Prefabs/Default/Environment/`.
- 성격: WG-a는 **Core 규칙을 켬**(그리지 않아도 칸이 세어지고 무너짐, 무너진 칸은 결과 끝에 검게 바뀜). WG-b에서 **약한 지반이 보이고, 무너지는 순간 검게 바뀐다**. WG-c는 문서·검토다.
- 상시 지시: MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다. 보고에는 "컴파일 미확인"을 적는다. 천명님 시험은 Q-WG7 허락 범위(§4)만 바꾼다.
- Core 변경은 Q-WG1 A(사용자 지시로 이 세션이 직접)에 따른다. 바꾼 것은 Core 전달 문서 §5.55에 남긴다.

## 0. 결정과 착수 조건

### 0.1 설계 결정 (2026-10-27 사용자)

| # | 결정 |
|---|---|
| Q-WG1 | A — Core는 이 세션이 직접 고치고 Core 전달 문서 §5.55에 남긴다 |
| Q-WG2 | **B — 넉백·끌어오기로 밟은 것도 센다** |
| Q-WG3 | A — 지나가기만 해도 센다 |
| Q-WG4 | A — 적 AI는 이번에 그대로(적이 스스로 떨어질 수 있음) |
| Q-WG5 | A — 임시 반투명 갈색 사각형을 이 작업에서 만든다 |
| Q-WG6 | A — 무너지는 사건 순간에 칸을 바꾼다 |
| Q-WG7 | A — 천명님 시험 여덟 사례를 새 규칙으로 바꾸는 것을 허락, 우리 `TerrainVisualDatabaseTests`도 맞춘다 |
| Q-WG8 | A — 넉백으로 찬 칸은 그 공격 명령 끝에 무너진다. 붕괴 호출은 모든 명령의 공통 끝 한 곳 |

### 0.2 계획에서 정한 것 (TP-1~TP-10, 사용자 확인 대상)

설계와 다르게 읽은 것은 TP-3 하나다(설계 WG6의 "거절"을 "사건 시점에는 하지 않고 결과 끝에 맡김"으로 읽음). 나머지는 설계가 비워 둔 세부다.

| # | 정한 것 | 이유(원칙) |
|---|---|---|
| TP-1 | 붕괴 호출은 `MovementProjection.Complete` 맨 앞의 `ResolveInterruptedActionEnd(this)` 자리를 `if (!Terminal) WeakGroundCalculator.CollapsePending(this);`로 바꾼다 | 붕괴 시점 원본이 한 줄(SSOT). 턴 끝·적 국면 끝은 `Complete`를 지나지 않으므로(§1 F4) 턴 경계에 붕괴가 생기지 않는다 |
| TP-2 | `ResolveInterruptedActionEnd`는 지운다. `StaticTerrainFact.RegisterEntry`의 주석 "붕괴 처리는 행동 종료 경계에 맡긴다"는 지금도 맞아 그대로 둔다 | 빈 함수 호출 제거(무의미한 If/Tick). Contracts 파일은 건드리지 않는다 |
| TP-3 | View 사건 시점 갱신은 **지금 보이는 원본만** 바꾼다. 원본이 보이지 않거나(프리팹 없음·숨음), 옛·새 종류 중 하나가 이웃 모양을 쓰거나, 표면 높이가 다르면 사건 시점에는 아무것도 하지 않고 결과 끝 `Render`에 맡긴다(예외 아님) | 토네이도 `ShowAt` 규칙("표시되지 않은 원본은 아무것도 하지 않음")과 같다. 이웃 표·높이 표를 원본 하나로 고치면 표 규칙이 두 벌이 된다(캡슐화) |
| TP-4 | `TerrainViewSet`의 원본 하나 입구는 **이미 항목이 있는 원본**만 받는다. 그래서 분류를 다시 보지 않고, 새 사실의 켜짐·공개·그릴 수 있음만 본다. `Render`의 원본 하나 처리부(외형 선택 → 열쇠가 바뀌면 반환 후 대여 → 적용)는 private 함수 하나로 빼서 둘이 함께 쓴다 | 항목이 있음 = 지난 `Render`가 분류·공개를 확인함. 처리부 공유로 판단이 두 벌이 되지 않는다(SSOT) |
| TP-5 | `FieldTerrainPresenter`는 마지막 `Render`의 스테이지 키를 필드 하나(`_stageKey`)로 남기고, 사건 시점 갱신 때 회전 표를 다시 찾는다. `_mapper`·`_rotations`는 지금처럼 호출 동안만 둔다 | 전투 중 스테이지는 바뀌지 않는다. 회전 표 사본을 들고 있지 않는다 |
| TP-6 | 사실 처리기의 새 인자는 토네이도처럼 끝 선택 인자(`showTerrainChange = null`)다. 필드 창구의 새 칸은 필수다 | `T/Framework/CombatFactEventHandlerTests.cs:28`(우리)이 인자 셋으로 만든다. 창구는 칸이 모두 필수인 규칙(View 3라운드 W9-3) |
| TP-7 | 새 프리팹·재질은 낭떠러지처럼 YAML로 쓴다(GUID 새로). 재질은 URP Unlit Transparent(`_Surface` 1, `_Blend` 0, `_SrcBlend` 5, `_DstBlend` 10, `_ZWrite` 0, 큐 3000, `_SURFACE_TYPE_TRANSPARENT`), 색 `#6B4A2B` 알파 0.45, 그림자 끔 | 설계 WG4. 낭떠러지와 같은 꼴이라 손으로 만들 일이 없다 |
| TP-8 | 천명님 시험은 §4 표대로만 바꾼다. 이름이 뜻과 반대가 되는 셋은 이름도 바꾼다(그중 하나는 보류 전 옛 이름으로 되돌림) | 확인 내용이 바뀌는 범위를 Q-WG7 허락 안으로 한정 |
| TP-9 | 검토 에이전트는 묶음 끝(WG-c)에 하나만 돌린다 | 사용자 지시(검토는 마지막에 한 번) |
| TP-10 | 확인용 약한 지반 배치는 사용자가 에디터에서 한다(`Combat_Normal_A` 등, 확인 뒤 되돌림). 빨리 보려면 정의 한도 3을 임시로 1로 | 설계 WG7. 스테이지 데이터는 사용자 영역 |

### 0.3 착수 조건

- 작업 트리에 이번 작업이 아닌 변경이 있다: FMOD 캐시·글꼴 에셋, 그리고 다른 세션의 Core 전달 문서 `## 42`(배치 구역 칸 요청)와 새 요청 문서. §5.55는 다른 자리에 쓰지만 같은 파일이라 커밋 때 hunk를 나눠야 한다.
- 다른 조건은 없다. WG-a부터 바로 시작할 수 있다.

## 1. 지금 코드 (HEAD `864a9d89` + 작업 트리, 2026-10-27 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | 약한 지반 계산 셋은 본문이 비어 있다. 옛 본문은 `2d0509df`에 있고, 쓰는 멤버가 지금 `MovementProjection`에 모두 있다 | `Core/WeakGroundCalculator.cs`, 설계 F3 |
| F2 | 진입 세기를 부르는 곳: 이동 경로(`Core/CombatMovementCalculator.cs:105`), 강제 진입 `ResolveForcedTerrainEntry`(`Core/MovementProjection.cs:385`, 캐릭터만). 강제 진입을 부르는 곳은 넉백(`Core/AttackKnockbackCalculator.cs:169`)·끌어오기(`Core/AttackForcedMovement.cs:57`)·이동 중 환경 밀기(`Core/MovementEnvironmentCalculator.cs:191`) | grep |
| F3 | `MovementProjection.Complete`(`:424`)를 지나는 명령: 이동(`Transition`, 미리보기 포함), 공격·반응 공격(`Core/CombatAttackCalculator.cs:156·159`), 배치(`Core/CombatPlacementCalculator.cs:58`), 전투 시작(`Core/CombatTurnCalculator.cs:43·54`) | grep |
| F4 | **플레이어 턴 끝·적 국면 끝은 `Complete`를 지나지 않는다**(`CombatTurnCalculator.Changed`로 바로 만듦). 그래서 TP-1 자리에서도 턴 경계에는 붕괴가 없다 | `Core/CombatTurnCalculator.cs:58·95·142` |
| F5 | 공격 미리보기 값은 `AttackResultFactory.Create`(`:150`)에서 `Complete`(`:159`)보다 먼저 만들어진다(설계 P2) | `Core/CombatAttackCalculator.cs` |
| F6 | 붕괴 안의 사망 경계는 공격이 미룬 사망(`DeferDeathsUntilCompletion`)을 앞당기지 않는다(`_preexisting`에 든 것은 정산에서 빠짐) | `Core/CommandDeathSettlement.cs:25·39` |
| F7 | 사건 형식 `CombatMovementSourceEvent(Expected, Final)`. 붕괴 사건은 `Expected` = 예정 약한 지반, `Final` = 같은 ID 낭떠러지 | `Contracts/Combat/CombatEvent.cs:198` |
| F8 | 지형 표시: `TerrainViewSet.Render`가 원본마다 보일지(`TryShow`: 분류·켜짐·공개) → 그릴 수 있음(`CanDisplay`: 프리팹 유무) → 외형 선택 → 열쇠가 바뀌면 반환 후 대여 → 적용을 한다. `_classified`는 호출 동안만 있다. 사건 시점 선례는 `FieldTerrainPresenter.Hide`·`ShowAt`(`:245·256`) | `M/TerrainViewSet.cs:46-104`, `M/FieldTerrainPresenter.cs` |
| F9 | 필드 창구 생성: 운영 `F/CombatSceneDisplayFactory.cs:139`(이름 인자), 우리 도우미 `T/Framework/CombatViewSystemTests.cs:1951` `FieldPorts`, 천명님 `T/CoreIntegration/PullAndMarkPresentationTests.cs:422`(위치 인자) | grep |
| F10 | 사실 처리기 생성: Presenter `C/Playback/CombatSceneEventPresenter.cs:127`, 우리 시험 `T/Framework/CombatFactEventHandlerTests.cs:28`(인자 셋) | grep |
| F11 | 약한 지반 외형 에셋 `TerrainVisual_WeakGround.asset`: 바닥 형상 모드, 프리팹 비어 있음, 설명 "해당 턴 종료 시 …". 낭떠러지 외형은 오늘 프리팹을 연결했다 | 에셋 |
| F12 | 약한 지반을 배치한 스테이지는 없다. 낭떠러지는 `Combat_Normal_A`(사용자 커밋 `864a9d89`)·`Test`에 있다 | GUID 검색 |

## 2. 파일 단위 설계

### 2.1 Core 계산 — `Core/WeakGroundCalculator.cs` (WG-a)

- `RegisterEntry`·`CollapsePending` 본문을 `git show 2d0509df:<경로>` 그대로 되살린다(설계 F3).
- `ResolveInterruptedActionEnd`를 지운다(TP-2).
- 클래스 주석: "실제로 진입한 캐릭터의 약한 지반 누적을 명령 내부 투영에만 반영한다." 뒤에 "이동 경로·넉백·끌어오기 진입을 모두 세고, 한도에 닿은 칸은 그 명령 끝(`MovementProjection.Complete`)에 무너진다(약한 지반 설계 WG1)."를 더한다.
- `CollapsePending` 주석은 그대로 두고 "부르는 곳은 명령 끝 한 곳이다"를 한 줄 더한다.

### 2.2 Core 명령 끝 — `Core/MovementProjection.cs:426` (WG-a)

```csharp
internal CombatCommandTransition Complete(AttackResult? attack = null, CombatCommandState state = null)
{
    // 진입이 일어난 명령 끝에 한도에 닿은 약한 지반을 무너뜨립니다(약한 지반 설계 WG1·Q-WG8).
    // 승패가 이미 났으면 무너뜨리지 않습니다. 턴 끝·적 국면 끝은 이 함수를 지나지 않습니다.
    if (!Terminal) WeakGroundCalculator.CollapsePending(this);
    return CombatCommandCalculator.Finish(…그대로…);
}
```

`ResolveForcedTerrainEntry`(`:382-387`)는 그대로다.

### 2.3 지형 원본 하나 입구 — `M/TerrainViewSet.cs` (WG-b)

- `Render`의 반복 안(원본 하나 처리)을 private `ShowOne(MovementTileFact source, TerrainContentKind kind, Action<TView, MovementTileFact, TerrainLook> apply)`로 뺀다. 내용은 지금 줄 그대로(외형 선택 → 종류 검사 → 열쇠가 바뀌면 `Remove` → 없으면 대여 → `Pass` → `apply`). `Render`는 `TryShow`·`_canDisplay` 뒤 `ShowOne`을 부른다.
- 새 입구:

```csharp
/// <summary>
/// 이미 보이고 있는 원본 하나를 새 사실로 다시 보이게 합니다(약한 지반 설계 WG6, 붕괴 순간). 항목이 없으면 아무것도 하지 않고
/// false를 돌려줍니다(결과 끝 Render가 맞춤). 항목이 있다는 것은 지난 Render가 분류·공개를 확인했다는 뜻이라 분류는 다시 보지 않습니다.
/// 새 사실이 꺼졌거나 공개되지 않았거나 그릴 수 없으면 View를 반환합니다.
/// </summary>
internal bool TryReplace(MovementTileFact source, Action<TView, MovementTileFact, TerrainLook> apply)
```

- 실패는 `Render`와 달리 전체 `Clear`를 하지 않고 예외를 그대로 올린다(오류 경계 F1). 옛 View를 먼저 반환했다면 그 원본은 빠진 채이고 다음 `Render`가 다시 빌린다.

### 2.4 지형 표시 — `M/FieldTerrainPresenter.cs` (WG-b)

- 필드 `private int _stageKey;`. `Render` 맨 앞(검사 뒤)에서 `_stageKey = stageKey;`(TP-5). `Clear`·`Release`는 건드리지 않는다(다음 `Render`가 덮음).
- 새 입구:

```csharp
/// <summary>
/// 약한 지반 붕괴처럼 같은 원본의 종류가 사건 시점에 바뀔 때 그 칸 외형을 새 종류로 바꿉니다(약한 지반 설계 WG6).
/// 지금 보이는 원본만 바꿉니다. 옛·새 종류 중 하나가 이웃 모양을 쓰거나 표면 높이가 다르면 결과 끝 Render에 맡깁니다(TP-3).
/// 결과 끝 Render가 최종 사실로 다시 맞춥니다.
/// </summary>
internal void ShowChanged(MovementTileFact final, FieldGridMapper mapper)
{
    if (final == null) throw new ArgumentNullException(nameof(final));
    if (mapper == null) throw new ArgumentNullException(nameof(mapper));
    if (_isReleased || !_views.TryGet(final.Id, out Instance shown)) return;
    TerrainVisualData before = _visuals.GetTerrainVisual(shown.Kind);
    TerrainVisualData after = _visuals.GetTerrainVisual(EnvironmentVisualKinds.TerrainKind(final));
    if (before.UsesEdgeShapes || after.UsesEdgeShapes || before.SurfaceHeight != after.SurfaceHeight) return;
    _mapper = mapper;
    try
    {
        _rotations = _stageKey > 0 && _stages.TryGetStageEnvironmentVisual(_stageKey, out var data) ? data : null;
        _views.TryReplace(final, _apply);
    }
    finally
    {
        _mapper = null;
        _rotations = null;
    }
}
```

- `_bodyShapes`(걸음 접촉 몸 캐시)는 원본 ID로 남는다. 약한 지반·낭떠러지는 접촉 원본이 아니라(설계 F13) 쓰지 않는다. 결과 끝 갱신에서도 바뀌지 않는 지금 성질 그대로다.

### 2.5 필드 창구 — `C/Playback/CombatFieldEventPorts.cs` (WG-b)

- 생성자 끝 인자 `Action<MovementTileFact, FieldGridMapper> showTerrainChange`(필수, null이면 `ArgumentNullException`), 속성 `ShowTerrainChange`.
- `<param>`: "종류가 바뀐 지형 원본(약한 지반 붕괴)의 칸 외형을 사건 시점에 바꿉니다(약한 지반 설계 WG6)."
- 머리 주석의 "창구 여덟"이 있으면 "아홉"으로.

### 2.6 사실 처리기 — `C/Playback/Handlers/CombatFactEventHandler.cs` (WG-b)

- 필드 `private readonly Action<MovementTileFact> _showTerrainChange;`, 생성자 끝 선택 인자 `Action<MovementTileFact> showTerrainChange = null`(TP-6), `<param>` 한 줄.
- 갈래(토네이도 갈래 뒤):

```csharp
// 약한 지반이 무너지면 사건 시점에 칸을 낭떠러지로 바꿉니다(약한 지반 설계 WG6). 종류가 같은 사건(진입 수만 바뀜)은 외형이 같아 건너뜁니다.
// 결과 끝 갱신이 최종 사실로 다시 맞춥니다. 뒤따르는 추락 사건·사망은 이미 있는 추락 연출이 받습니다.
case CombatMovementSourceEvent change when change.Expected is StaticTerrainFact before &&
    change.Final is StaticTerrainFact after && before.Kind != after.Kind:
    _showTerrainChange?.Invoke(after);
    break;
```

### 2.7 사건 Presenter — `C/Playback/CombatSceneEventPresenter.cs:118-127` (WG-b)

- `Action<MovementTileFact> showTerrainChange = final => ports.ShowTerrainChange(final, _mapper);`(토네이도 줄 옆, 주석 한 줄)
- `facts: new CombatFactEventHandler(_facts, ports.ConsumeThorn, ports.HideObject, ports.HideTornado, showTornadoAt, showTerrainChange)`

### 2.8 운영 조립 — `F/CombatSceneDisplayFactory.cs:149` (WG-b)

```csharp
showTornado: terrain.ShowAt,
// 무너진 약한 지반은 사건 시점에 칸을 낭떠러지로 바꿉니다(약한 지반 설계 WG6). 결과 끝 Refresh가 최종 사실로 다시 맞춥니다.
showTerrainChange: terrain.ShowChanged);
```

머리 주석 "필드 표시 창구 여덟"을 "아홉"으로.

### 2.9 에셋 (WG-b)

| 파일 | 내용 |
|---|---|
| `E/Terrain_WeakGround.prefab`(+ `.meta`) | `Terrain_Cliff.prefab`과 같은 꼴: 루트 `Terrain_WeakGround`(레이어 0, 회전 없음) 아래 `Surface` = Quad(`fileID 10210`), 회전 X 90, 위치 y 0.01, 배율 (1.05, 1.05, 1), 그림자 끔, Rendering Layer 1, 재질 아래 것 |
| `E/WeakGround/WeakGroundSurface.mat`(+ `.meta`, 폴더 `.meta`) | TP-7 값 |
| `Assets/ProjectL/DataBaseSO/ViewGroup/TerrainVisual/Item/TerrainVisual_WeakGround.asset` | `prefab` = 새 프리팹 루트, `description` = 설계 WG5 2판 문구(유니코드 이스케이프로 씀, 낭떠러지 에셋 꼴) |

## 3. Phase·Task

### WG-a Core 규칙 켜기

| T | 할 일 | 파일 |
|---|---|---|
| T1 | 계산 본문 복원·빈 함수 삭제·주석(§2.1) | `Core/WeakGroundCalculator.cs` |
| T2 | 명령 끝 한 줄(§2.2) | `Core/MovementProjection.cs` |
| T3 | 천명님 시험 Core 쪽 여섯 사례(§4 ①~⑥) | `T/WeakGroundMovementTests.cs`, `T/WeakGroundTurnTests.cs`, `T/CliffFallTests.cs` |
| T4 | 천명님 게시 시험 두 사례(§4 ⑦·⑧) | `T/Framework/WeakGroundPublicationTests.cs` |
| T5 | Core 전달 문서 §5.55(규칙·바꾼 줄·고친 시험·기획 질문 답·공격 미리보기 차이·적 AI 미반영) | `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md` |

WG-a 확인(사용자 Unity): 컴파일, §4 표의 Core 시험 전부. 재생은 약한 지반 배치가 있어야 보인다(TP-10). 이 단계에서는 칸이 그려지지 않고, 무너진 칸은 결과가 끝난 뒤 검게 바뀐다.

### WG-b View

| T | 할 일 | 파일 |
|---|---|---|
| T6 | 원본 하나 입구·처리부 공유(§2.3) | `M/TerrainViewSet.cs` |
| T7 | 스테이지 키·`ShowChanged`(§2.4) | `M/FieldTerrainPresenter.cs` |
| T8 | 창구 칸(§2.5) | `C/Playback/CombatFieldEventPorts.cs` |
| T9 | 사실 처리기 갈래(§2.6) | `C/Playback/Handlers/CombatFactEventHandler.cs` |
| T10 | Presenter 넘기기(§2.7) | `C/Playback/CombatSceneEventPresenter.cs` |
| T11 | 운영 조립 한 줄(§2.8) | `F/CombatSceneDisplayFactory.cs` |
| T12 | 프리팹·재질·외형 에셋(§2.9) | `E/…`, `TerrainVisual_WeakGround.asset` |
| T13 | 시험 맞춤(§4 ⑨~⑪): 우리 `FieldPorts` 도우미·`TerrainVisualDatabaseTests`, 천명님 `PullAndMarkPresentationTests:422` 컴파일 한 인자 | `T/Framework/CombatViewSystemTests.cs`, `T/Framework/TerrainVisualDatabaseTests.cs`, `T/CoreIntegration/PullAndMarkPresentationTests.cs` |

WG-b 확인(사용자 Unity): 컴파일, `TerrainVisualDatabaseTests`·`TerrainViewSetTests`·`FieldTerrainPresenterTests`·`CombatFactEventHandlerTests`·`PullAndMarkPresentationTests`·`CombatSceneEventPresenterTests`, 재생 §5.

### WG-c 문서·검토

| T | 할 일 |
|---|---|
| T14 | 민형님 Factory 공유 §35(창구 칸 한 줄), 설계 §11 구현 메모(TP-3 등 계획에서 정한 것), 이 계획 §7 실행 기록 |
| T15 | 검토 에이전트 하나(TP-9): 설계·계획과 바뀐 파일만 주고 7기준·붕괴 시점이 한 곳인지·사건 시점 갱신 조건·시험 변경이 허락 범위인지 본다. 반영은 보고 뒤 |
| T16 | CLAUDE.md 현재 상태·메모리 |

## 4. 시험 영향 (새 `[Test]` 없음)

①~⑧은 천명님 시험(Q-WG7 허락), ⑨·⑩은 우리 시험, ⑪은 천명님 시험 컴파일 맞춤이다. "이미 실패로 보임"은 코드 대조 판단이다.

| # | 시험 | 바꿈 |
|---|---|---|
| ① | `WeakGroundMovementTests.MovementDoesNotCountOrCollapseWeakGround(long)` → 이름 `Movement_CountsEveryEntry_AndCollapsesAtCommandEnd` | `TestCase(2L)` → `TestCase(1L)`(시작부터 예정 상태인 입력은 이제 생길 수 없음). 기대: 미리보기 공개 효과·결과 사건에 원본 사건이 **있음**. count 0이면 71·72 진입 수 1, 낭떠러지 없음. count 1이면 71(지나간 B)이 낭떠러지, 이동자는 D에서 살아 있음, 72 진입 수 1. 환경은 입력과 **다름**. 행동 사용·예산 단언은 그대로 |
| ② | `WeakGroundMovementTests.HiddenGround_RemainsHiddenAndUnchangedDuringMovement` → 이름 `HiddenGround_PreviewDoesNotRevealOrCountItButExecutionDoes`(보류 전 옛 이름) | 미리보기·맥락 단언 그대로. 실행 단언 `SameValues(ground)` → `SameValues(ground.RegisterEntry())` |
| ③ | `WeakGroundMovementTests.ForcedAttackRelocations_DoNotChangeWeakGroundOrAttackerSettlement` → 이름 `ForcedAttackRelocations_CountEveryEntryWithoutChangingAttackerSettlement` | 71·72 진입 수 0 → 1(보류 전 기대값과 같음), 원본 사건 `Is.Empty` → 둘. 공격자 정산·예산 단언 그대로 |
| ④ | `WeakGroundTurnTests.EndBoundaries_DoNotCollapsePendingGround(bool)` | 삭제. 지운 함수를 부르고, "턴 끝에는 무너지지 않음"은 같은 파일의 `ControllerCompletion_…`이 이미 확인한다 |
| ⑤ | `WeakGroundTurnTests.EntryBoundary_DoesNotArmACollapse` → 이름 `EntryThenCollapse_TurnsGroundIntoCliffAndDropsOccupant` | 직접 부른 진입·붕괴의 기대: 그 칸이 낭떠러지, 원본 사건 둘(진입·붕괴), 추락 사건 하나(행위자, 그 칸), 행위자 사망 |
| ⑥ | `CliffFallTests.AttackKnockback_ResolvesTerrainBeforeThornsAndObjects_AndPreviewAgrees(WeakGround)` | 약한 지반 갈래만: `:179` 진입 수 0 → 1, `:182` 원본 사건 71 `Is.False` → `Is.True`(한도 2 미만이라 붕괴 없음). 낭떠러지 갈래·공통 단언 그대로 |
| ⑦ | `WeakGroundPublicationTests.PreviewAndEntry_PublishExactCountPendingAndOneResult_WithoutChangingOriginal`(이미 실패로 보임) → 이름 `PreviewAndEntry_PublishExactCountAndOneResult_WithoutChangingOriginal` | 지반 한도 2 → 3(이동자가 그 칸에 멈추므로 한도에 닿으면 떨어짐). 기대 `RemainingEntries` 0 → 1, `PendingCollapse` true → false. 나머지 그대로 |
| ⑧ | `WeakGroundPublicationTests.Collapse_PublishesEverySourceAndFallWithFinalOccupantsInOneAcceptedResult`(이미 실패로 보임) | 명령을 턴 끝에서 **이동**으로 바꾼다: 지반 72 (1,2)·71 (1,3) 모두 한도 2·진입 1, 메인이 (1,1)→(1,2)→(1,3) 이동(같은 생성 꼴에 종류 Move·경로). 원본 사건 단언은 붕괴 사건(`Final`이 낭떠러지)만 골라 {71, 72}, 추락 칸 (1,3). 완료를 붙여 게시하는 나머지 그대로. 메인 이동 범위가 2보다 작으면 한 칸(지반 하나)으로 줄이고 그 사실을 실행 기록에 남긴다 |
| ⑨ | 우리 `CombatViewSystemTests.FieldPorts` 도우미 | 생성 인자 끝에 `(_, _) => { }` |
| ⑩ | 우리 `TerrainVisualDatabaseTests.ActualCatalog_ProvidesPlaceholderCubesTornadoParticlesAndGroundShapeVisuals` `:228-234` | 주석 "약한 지반·낭떠러지는 아직 필드에 그리지 않습니다(Q-HG5 A)" → "약한 지반·낭떠러지는 임시 사각형 프리팹을 가지며 표면 높이는 바닥입니다(낭떠러지 임시 연출, 약한 지반 설계 WG4)", `Prefab Is.Null` → `Is.Not.Null`. 높이 0 단언 그대로 |
| ⑪ | 천명님 `PullAndMarkPresentationTests.cs:422` | 컴파일 맞춤: 창구 생성 끝에 `(_, _) => { }` 한 인자 |

그대로 통과로 보는 것: `WeakGroundTurnTests.ControllerCompletion_…`(턴 끝은 `Complete`를 안 지남), `ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_…`(같은 이유), `RejectedRoute_…`, `PlayerPlacement_…`, `ObstacleReactionTests.Knockback_SurvivingObstacleMovesWithoutGroundCount`(장애물은 안 셈), `CliffFallTests.EnvironmentForcedEntry_…`(낭떠러지 사례만), `DeferredCombatFeatureTests.EnvironmentClocksDoNotActivateOrArmCounters`(되살린 `RegisterEntry`가 필드에 있는 캐릭터를 요구 — 시험 행위자가 필드에 있는지 Unity 결과로 확인), `WeakGroundPublicationTests`의 나머지(**검토 정정: `KnockedObstacleThenCollapse_…`·`FreshBattle_…` 둘은 실패한다, §7.5**), `WeakGroundStateTests`·`TileSuccessorTests`(상태 규칙), `CombatEnvironmentHoverTests.WeakGroundDescription_…`(문구를 시험이 직접 넘김), `CombatFactEventHandlerTests`(새 인자는 선택).

## 5. Unity 확인 목록 (WG-b 뒤, 사용자)

사용자가 한 스테이지에 약한 지반 몇 칸을 임시로 둔다(TP-10).

1. 컴파일, §4 시험.
2. 약한 지반 칸이 갈색 반투명으로 보이고 격자가 비친다(낮·밤). 낭떠러지는 검게 격자를 덮는다.
3. 호버 설명: 총 횟수와 "N회 더 밟으면 무너집니다"가 맞고, 밟을 때마다 준다(지나가기 포함).
4. 마지막 한 번을 밟고 멈추면: 걸음이 끝날 때 칸이 검게 바뀌고 → 이동자가 가라앉으며 작아지고 → 사망 처리.
5. 마지막 한 번을 지나가기만 하면: 이동이 끝날 때 그 칸만 검게 바뀌고 이동자는 무사하다.
6. 넉백·끌어오기로 밀어 넣으면 남은 횟수가 준다(여러 칸 밀리면 지나간 칸마다). 마지막 한 번으로 밀려 멈춘 대상은 넉백이 끝난 뒤 칸이 검게 바뀌고 떨어진다. 공격 호버 체력 예상에는 이 추락이 나오지 않는다(설계 P2).
7. 적 이동·적 넉백도 센다. 적이 마지막 칸에서 멈추면 떨어진다(Q-WG4 A).
8. 무너진 칸에는 다시 들어갈 수 없다(낭떠러지 규칙).
9. 결과 끝 갱신 뒤에도 같은 모습(튀지 않음). 턴 끝·밤 끝에 아무 일도 없다.

## 6. 커밋 묶음

- WG-a: `WeakGroundCalculator.cs`, `MovementProjection.cs`, 천명님 시험 넷 파일, Core 전달 문서 §5.55 hunk(다른 세션의 `## 42` hunk는 빼고).
- WG-b: View 코드 여섯, 새 프리팹·재질(+ `.meta`, 폴더 `.meta`), `TerrainVisual_WeakGround.asset`, 시험 셋. 프리팹·재질과 외형 에셋은 한 커밋(외형이 프리팹 GUID를 참조).
- WG-a와 WG-b는 따로 커밋해도 된다(WG-a만으로 컴파일·동작이 맞음).
- FMOD 캐시·글꼴 에셋은 이번 것이 아니다.

## 7. 실행 기록

사용자 지시(2026-10-27): "WG-c까지 쭉 진행해, 완료하고 나면 진행한 계획 및 구현 사항 검토 후 Core 측 전달 문서 작성하자". TP-1~TP-10은 그대로 진행했다. 그래서 **T5(Core 전달 문서 §5.55)는 WG-a에서 빼서 검토 뒤로 옮겼다.** 모든 단계 컴파일 미확인, 새 `[Test]` 없음.

### 7.1 WG-a Core 완료

- `Core/WeakGroundCalculator.cs`: `RegisterEntry`·`CollapsePending` 본문을 `2d0509df`대로 되살렸다(줄 단위 같음). `ResolveInterruptedActionEnd`는 지웠다. 클래스·`CollapsePending` 주석에 규칙과 부르는 곳을 적었다.
- `Core/MovementProjection.cs` `Complete`: 빈 함수 호출을 `if (!Terminal) WeakGroundCalculator.CollapsePending(this);`로 바꿨다(주석 두 줄).
  - 구현 중 확인한 제약: `Complete(state: …)`로 상태를 넘기는 호출(전투 시작 `CombatTurnCalculator.cs:54`)은 그 상태를 `Complete` 전에 만든다. 그 안에서 붕괴하면 넘긴 상태의 환경이 낡는다. 전투 시작에는 진입이 없어(재배치는 세지 않음) 무너질 칸이 없으므로 지금은 문제가 없다. 주석에 적었다.
- 천명님 시험 여덟(§4 ①~⑧): 계획대로 바꿨다. 계획보다 더한 단언은 셋이다. ①에 "추락 없음·이동자 생존", ⑤에 "같은 칸", ⑧에 "계산 결과 수락"(이동 명령이 거절되면 원인을 바로 보이게).
  - ⑧은 메인 이동 범위가 2 이상이라고 보고 두 칸 경로로 썼다(아킴보 기본값, Unity 결과로 확인).
  - 각 사례 위에 "2026-10-27 View 작업에서 사용자 허락으로 기대값 변경" 주석을 달았다.

### 7.2 WG-b View 완료

- `M/TerrainViewSet.cs`: `Render`의 원본 하나 처리를 private `ShowOne`으로 뺐다. 새 입구 `TryReplace`(항목이 있는 원본만, TP-4)를 더했다.
  - 계획에 없던 것: 보일지 조건 중 분류 밖의 것(켜짐·Player 공개)을 private `IsShownFact`로 빼서 `TryShow`와 `TryReplace`가 함께 쓴다. 두 벌이 되지 않게 하려는 것이다.
- `M/FieldTerrainPresenter.cs`: `_stageKey`(TP-5), 회전 표 찾기 `RotationsOf`(Render와 `ShowChanged` 공유), `ShowChanged`(TP-3).
- `C/Playback/CombatFieldEventPorts.cs`: 필수 칸 `ShowTerrainChange`(창구 아홉).
- `C/Playback/Handlers/CombatFactEventHandler.cs`: 끝 선택 인자 `showTerrainChange`와 갈래 하나(종류가 바뀐 `CombatMovementSourceEvent`만).
- `C/Playback/CombatSceneEventPresenter.cs`: 넘기기 두 줄. `F/CombatSceneDisplayFactory.cs`: 이름 인자 한 줄.
- 에셋: 새 `E/Terrain_WeakGround.prefab`(GUID `63c7f0275b414b0e9a9c992954226c7d`, 루트 fileID `4729318560127384193`), `E/WeakGround/WeakGroundSurface.mat`(GUID `083a75a7b9894316905133e5b26be22a`, 템플릿 = `Resources/View/MovementGhost.mat`의 반투명 Unlit 값), 폴더 `.meta`(GUID `288262e769634139a5852392a599bffb`). `TerrainVisual_WeakGround.asset`에 프리팹과 설계 WG5 문구를 넣었다.
- 시험 맞춤(§4 ⑨~⑪): 우리 `FieldPorts` 도우미 한 인자, 우리 `TerrainVisualDatabaseTests` 단언·주석, 천명님 `PullAndMarkPresentationTests:422` 한 인자(컴파일 맞춤).

### 7.3 WG-c 문서

- 민형님 Factory 공유 §35(창구 칸 한 줄, 미전달).
- 설계 §11 구현 메모.
- 검토(T15)·Core 전달 문서(T5)·현재 상태(T16)는 아래에 이어 적는다.

### 7.4 추가 요청 — 금 간 무늬 (2026-10-27 사용자 "약한 지반에 금간 듯한 효과" → 세 안 중 "일단 A로", 코드 변경 없음)

- 새 `E/WeakGround/WeakGroundCrack.png`(256×256, GUID `fb2f14544aa149569549a845a1d9353b`): 바탕은 옛 재질 색(`#6B4A2B`, 알파 0.45)이고, 그 위에 어두운 갈색 금 선(알파 약 0.92)을 그렸다. 가운데에서 갈라지는 큰 금 넷과 잔금 셋이다. 가장자리 14px 안쪽에서 멈춰 이웃 칸과 이어 보이지 않게 했다.
  - 스크립트로 만든 임시 그림이다(System.Drawing, 고정 난수 27, 스크립트는 저장소 밖). 아트가 나오면 재질의 텍스처 칸만 바꾼다.
  - 가져오기 설정은 `Prefabs/Combat/Models/cactusT.png.meta` 꼴에서 감싸기 Clamp, 알파는 투명으로만 바꿨다.
- `WeakGroundSurface.mat`: `_BaseMap`·`_MainTex` = 새 텍스처, `_BaseColor`·`_Color` = 흰색 알파 1(색과 투명도는 텍스처가 정함). 반투명 설정은 그대로라 격자는 지금처럼 비친다.
- 남은 것: Unity에서 보이는지와 밤 화면 확인. 단계 표시(남은 횟수에 따라 금이 늘어남, 안 B)는 필요하면 설계부터.

### 7.5 묶음 끝 검토 (2026-10-27, 새 에이전트 하나, 정적, WG-a~WG-c, 금 간 텍스처 전)

7기준 평균 **약 8.6**(SSOT 9 · SOLID 8.5 · FW 9 · 독립성 8 · 캡슐화 8.5 · C#/Unity 8 · If/Tick 9). 높음 0, 중간 1, 낮음 3. 맞다고 확인한 것: Core 본문이 `2d0509df`과 줄 단위로 같음, 예정 붕괴가 새는 경로 없음(진입을 세고 `Complete`를 안 지나는 것은 버려지는 이동 맥락 투영뿐), 미룬 사망·승패 건너뜀, `Render` 동작 같음, 사건 순서(붕괴 사건이 장벽이라 걸음이 끝난 뒤 칸이 바뀌고 그 다음 가라앉음), 시험 ①~⑪, 에셋.

| # | 심각도 | 지적 | 처리 |
|---|---|---|---|
| R1 | 중간 | 천명님 `WeakGroundPublicationTests.KnockedObstacleThenCollapse_…`(`:195`)·`FreshBattle_…`(`:334`)이 "작성 때부터 예정인 지반이 턴 끝에 무너진다"에 기대 실패한다. §4 목록 밖이다(HEAD에서도 이미 실패로 보임) | **반영(사용자 허락)**: §7.6 |
| R2 | 낮음 | 진입 사건(종류 그대로인 `CombatMovementSourceEvent`)은 주체가 없어 동시 묶음 밖 장벽이 된다. 여러 대상 동시 넉백에서 한 대상이 약한 지반에 들어가면 그 뒤 대상의 이동이 차례 재생으로 바뀐다(`Playback/Ordering/CombatConcurrentGroups.cs:58`) | **반영**: §7.6 |
| R3 | 낮음 | 반투명 사각형의 정렬 순서가 0이라 칸 채우기(−1)보다 나중에 그려져 약한 지반 칸의 범위 채우기가 갈색에 덮인다 | **반영**: §7.6 |
| R4 | 낮음 | `Complete(state:)` 호출에서 붕괴하면 넘긴 상태가 낡는다(지금 경로 없음, 주석만) | **그대로**(사용자 결정, 주석만) |

- 검토 뒤 확인: 사용자가 에디터에서 `Terrain_WeakGround.prefab`의 레이어를 20(BothRendering)으로 바꿨다(고지대 `HeightGround_Center`와 같음). 낭떠러지 데칼 때 밤 카메라 컬링 마스크(0·22·30·31)에 20이 없어 밤에 안 보였던 일이 있다. 그대로 두고 밤 확인 항목에 넣는다.
- Core 전달 문서 §5.55(T5)는 검토 뒤에 썼다(사용자 지시).

### 7.6 검토 반영 (2026-10-27 사용자 "현재 관련 작업물 스테이지에 올려주고 권장안대로 처리해", 컴파일 미확인, 새 시험 없음)

먼저 그때까지의 작업물을 스테이지했다(우리 파일 28개). CLAUDE.md·Core 전달 문서는 다른 세션 hunk(배치 구역 항목, `## 42`·`## 43`)를 빼고 우리 부분만 올렸다. 사용자가 확인용으로 넣은 `Test.asset`의 약한 지반 배치(칸 (6,3))는 임시라 올리지 않았다. 아래 반영분은 스테이지하지 않았다.

- **R1(천명님 시험 둘, 사용자 허락):**
  - `KnockedObstacleThenCollapse_…`: 턴 끝 단계를 걷고, 밀기 공격의 결과 안에서 낭떠러지·화약통 51 추락·`Destroyed` 반응 없음을 본다. "장애물 진입은 세지 않는다"는 원본 사건이 붕괴 하나뿐인지로 본다. 공격 결과 `Hits`의 맞은 뒤 체력은 붕괴 전 값이라(설계 P2) 단언하지 않는다.
  - `FreshBattle_…`: 앞 단계를 이동 명령으로 바꿨다. 지반을 (1,2)·진입 1로 두고 메인이 (1,1)→(1,2)→(1,3)으로 지나가면 무너진다. 새 전투 설치 단언은 그대로다.
- **R2(동시 넉백):** `CombatConcurrentGroups`에 `AttachEntryRecords`를 더했다. 종류가 그대로인 진입 기록을 바로 앞 이동 사건(같은 행동 구간, 묶음 안)과 같은 묶음·주체에 둔다(아이템 사건 `AttachItemEvents`와 같은 꼴). `SplitInterruptedPaths` 앞에 불러, 여러 칸 넉백 경로 중간의 진입 기록도 경로를 끊지 않는다. 클래스 설명의 "지형 등록이 끼면 끊김" 예시를 고쳤다. 종류가 바뀐 붕괴 기록은 그대로 장벽이다.
- **R3(그리기 순서):** `Terrain_WeakGround.prefab`의 `m_SortingOrder` 0 → −2. `FieldSortingOrders` 설명에 "반투명 바닥 표면 프리팹은 에셋 값 −2"를 적었다.
- **R4:** 그대로(주석만).
- 이어서 사용자가 R1~R3 반영분과 기록까지 모두 `2805717b`로 커밋했다. 같은 커밋에 사용자가 에디터에서 고친 약한 지반 설명 문구가 들어갔다. 설계 WG5 문구와 다르며, 문구의 원본은 에셋 `TerrainVisual_WeakGround.asset`이다.
