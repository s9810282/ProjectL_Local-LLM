# 전투 클립 이펙트 조각 설계

작성일: 2026-10-19. 상태: **결정 완료(2026-10-19, 사용자 "Q-E1~Q-E4 모두 A로 해. Task 계획 쓰고 EF-1부터 진행해.")**, Task 계획 [clip-effect-plan](../plans/2026-10-19-combat-clip-effect-plan.md). **EF-1~EF-3 완료(EF-1·EF-2 Unity 확인 문제 없음, 2026-10-19).** 구현 기록은 계획의 실행 기록이 원본이다.

> 2026-10-31: 몸 자리(ActorBody·SubjectBody) 이펙트는 큐의 남기기 칸이 꺼져 있으면 유닛 위치를 따라간다(Q-E1 A를 몸 자리에 한해 바꿈). 정책 원본은 [이펙트 따라가기·회수 설계](2026-10-31-combat-effect-follow-recall-design.md).

사용자 요청: "이펙트에 관한 것도 지금 미리 작업해두자" — 공격 클립의 이벤트 타이밍(`ClipEvent`)에 이펙트를 낼 수 있는 길을 미리 만든다. 지금 등록된 실제 이펙트는 없으므로 모든 칸의 기본값은 "이펙트 없음"이고 화면은 바뀌지 않는다.

관련: [클립 설계](2026-10-19-combat-unit-animation-clip-design.md) AC7 표의 "4 이펙트 조각(나중) — Render 창구가 생기면 한 줄"을 채우는 설계다. 조각·조립 규칙은 [조각 설계](2026-10-18-combat-presentation-pieces-design.md) BP1~BP4를 따른다.

## 1. 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| E-F1 | Framework 이펙트는 `IEffectSystemServices.GetEffectHandle()`로 받은 `EffectScope` 하나로 낸다. `Play(EffectScopeRequest(종류, 부모, 지역 위치, 지역 회전))`가 풀의 파티클을 그 부모 아래로 옮겨 재생하고, 파티클이 자연히 멈추면(`ParticleStopNotifier`) 풀로 돌아간다. 요청 하나만 멈추는 API는 없고 `StopAll()`뿐이다 | `RenderSystem/Core/EffectSystem/EffectScope.cs`, `ParticleStopNotifier.cs` |
| E-F2 | `GetEffectHandle()`은 Scope를 시스템 목록에 넣고 Shutdown 때만 정리한다. 부를 때마다 쌓인다(View·Framework 사용 평가 G2, Framework 쪽 해제 API 없음) | `EffectSystem.cs:99-106`, [사용 평가](2026-10-18-view-framework-usage-review.md) G2 |
| E-F3 | 이펙트 종류는 Render DB의 `EffectType`(Empty·TestFire·Boom)이다. 새 이펙트 = enum 값 + `EffectDataSO` 항목(프리팹 루트에 ParticleSystem·`ParticleStopNotifier`). 등록 안 된 종류는 **첫 재생 때** 예외다 | `EffectType.cs`, `EffectDataSO.cs`, `EffectSystem.cs:133-148` |
| E-F4 | View DB가 `EffectType`을 칸으로 갖는 선례가 있다: 기차 `RailwayVisualDataSO.effect`, 스테이지 맵 `hoverEffect`·`selectEffect`. 기본 Empty = 없음, 검사는 `System.Enum.IsDefined` | `RailwayVisualDataSO.cs:31,69`, `StageMapViewDataSO.cs:50-52,103` |
| E-F5 | 전투 표시는 이미 `IEffectSystemServices`를 받는다(`CombatDisplayServices.Effects`). 폭발 이펙트 `CombatEnvironmentEffectPresenter`가 부모 `field.CellsRoot`·Scope 하나(처음 쓸 때)·재생 취소와 해제 때 `StopAll`로 쓴다 | `CombatSceneDisplayFactory.cs:82-84`, `CombatEnvironmentEffectPresenter.cs` |
| E-F6 | 시간 없는 조각 선례: 소리 `CombatEventSoundPiece`·흔들림 `CombatCameraShakePiece`(시작에서 내고 바로 끝, 취소는 아무것도 안 함). 흔들림은 창구(`CombatCameraWindow`)를 Factory가 만들어 Presenter를 거쳐 처리기에 넘긴다 | 각 파일, `CombatSceneEventPresenter.cs:90-106` |
| E-F7 | 클립 대여는 이벤트 컴포넌트의 자리 표시를 읽는 입구를 이미 갖는다: `ICombatUnitClip.TryGetMuzzle`(월드 위치, Unity 평가 전이라 앞 프레임 자세) | `ICombatUnitClip.cs:31-35`, `UnitClipChannel.cs:197` |
| E-F8 | 클립 작업(AC-1~AC-6)은 아직 커밋 전이고 바꿀 파일이 겹친다(`CombatClipAttack`·`CombatUnitAnimationSO`·`CombatClipAttackData`·`CombatAnimationEvents`는 새 파일, 처리기·Presenter는 수정 중). 지금 같은 파일을 고치는 다른 세션은 없다 | `git status` |

**추론한 위험(E-R1):** 이펙트를 유닛 모델 아래에 붙이면(부모 = 유닛) 유닛 표시가 꺼질 때 파티클이 멈춰 콜백이 오지 않아 풀로 돌아가지 않고, 유닛 GameObject가 파괴되면 풀의 이펙트도 함께 파괴된다(풀이 파괴된 객체를 다시 내줄 수 있음). Framework 계약이 다루지 않는 경우라 피한다.

## 2. 설계

### EF1. 이펙트 창구 `CombatEffectWindow` (새, `ViewSystem/Combat/Display/`)

- 카메라 창구와 같은 자리의 표시 부품이다. 가진 것: `IEffectSystemServices`, 부모 Transform(필드 `CellsRoot`), Scope **하나**(처음 낼 때 받음 — E-F2 누적을 표시 하나당 하나로 묶음).
- `Play(EffectType type, Vector3 worldPosition, Quaternion worldRotation)`: 월드 값을 부모 지역 값으로 바꿔 `EffectScopeRequest`로 낸다. Empty·정의 안 된 값은 예외.
- `StopAll()`: 남은 이펙트를 모두 풀로 돌린다(Q-E4). `Release()`: `StopAll` 뒤 다시 쓰면 예외.
- 재진입 가드는 폭발 Presenter 규칙을 따른다(재생 중 정리가 다시 들어오면 재생 뒤 정리).

### EF2. 이펙트 조각 `CombatEffectPiece` (새, `ViewSystem/Combat/`)

- 시간 없는 조각(소리·흔들림과 같은 모양). 시작에서 공격자 클립 대여의 이펙트 지점을 읽어 창구로 한 번 내고 바로 끝난다. Tick·Cancel은 아무것도 하지 않는다(개별 회수 API 없음, E-F1).
- 이펙트 지점은 클립 대여에서 읽으므로 조립이 그 유닛의 클립 채널을 빌려 둬야 한다(클립 공격 조립은 이미 빌림).

### EF3. 값 — 클립 묶음 SO 공격 칸

- `CombatUnitAnimationSO` 공격 칸에 `attackEffect`(`EffectType`, 기본 Empty = 이펙트 없음)·`effectDelay`(이벤트 타이밍 뒤 초, 0 이상)를 더한다. 피격·소리 선딜레이와 같은 칸이다(클립 설계 AC8 "선딜레이는 공격 칸").
- `CombatClipAttackData`에 `Effect`·`EffectDelay`·`HasEffect`. 선딜레이 규칙은 기존 `IsValidDelay` 하나를 같이 쓴다.
- 이펙트 종류의 원본은 Render DB `EffectType` 그대로다(E-F4 선례, Framework 형식 재사용). 새 이펙트 등록은 Render 작업자(민형님) 몫이다.

### EF4. 이펙트 지점 — 이벤트 컴포넌트의 자리 표시

- `CombatAnimationEvents`에 선택 칸 `effectPoint`(Transform)를 더한다. 원거리 `muzzle`과 같은 종류의 작업 입력이다(이름표·자리 표시만, 런타임 상태 없음).
- 대여 입구 `ICombatUnitClip.TryGetEffectPoint(out Vector3 position, out Quaternion rotation)`(앞 프레임 자세, E-F7과 같은 주의). 구현은 `UnitClipChannel`·`FieldUnitView` 대여.
- 검사(Q-E2 A면): 공격 칸에 이펙트가 있는데 프리팹에 `effectPoint`가 없으면 부트에서 실패. 규칙 원본은 `CombatUnitAnimationRules`(포구 필수 규칙 옆).

### EF5. 조립 — 클립 공격 공통 앞부분에 한 줄

```text
4 이펙트 — After(1 클립, ClipEvent, 이펙트 선딜레이) — CombatEffectPiece   (공격 칸에 이펙트가 있을 때만)
```

- `CombatClipAttack.BeginClip`(근접·원거리 공통)에 넣는다. 창구는 입구 두 개의 끝 선택 인자로 받고, 없으면(시험 조립) 항목을 넣지 않는다(피격 조립의 카메라 인자 선례).
- 완료·Gate는 바뀌지 않는다(기다리지 않는 시간 없는 조각).

### EF6. 연결과 수명

- Factory: `new CombatEffectWindow(services.Effects, field.CellsRoot)` 한 줄, Presenter에 이름 있는 인자 `effects:`로 넘김(카메라 창구와 같은 길). 민형님 Factory 공유 문서에 절 추가.
- Presenter: 처리기 `CombatAttackEventHandler`에 넘기고, 재생 취소(`CancelMovement`)에서 `StopAll`, `Release`에서 `Release`(Q-E4 A). 창구가 없으면(시험) 빈 창구를 쓰지 않고 null을 넘겨 조립이 항목을 빼게 한다.

## 3. 정할 것

**결정 완료(2026-10-19): Q-E1~Q-E4 모두 A.** 아래는 결정 때의 선택지 기록이다.

- **Q-E1. 이펙트 위치 — 붙일까 놓을까**
  - **A(권장):** 이벤트 타이밍의 그 순간 이펙트 지점의 **월드 위치·회전에 놓고 따라가지 않는다**(부모 = 필드 루트). 짧은 터짐·베기 이펙트에 맞고, E-R1 위험이 없다.
  - B: 이펙트 지점 아래에 붙여 유닛을 따라간다. 칼 궤적처럼 붙어 있어야 하는 이펙트에 자연스럽지만 E-R1(표시 꺼짐·파괴 때 풀 오염)이 Framework 계약 밖이다.
- **Q-E2. 이펙트 지점**
  - **A(권장):** `CombatAnimationEvents`에 새 선택 칸 `effectPoint`. **이펙트가 있는 공격 칸이면 필수**(부트 검사). 잘못된 자리에 조용히 나오는 것을 막는다(포구 규칙과 같은 방식). 원거리는 포구와 같은 Transform을 넣어도 된다.
  - B: 기존 `muzzle`을 같이 쓴다. 칸이 늘지 않지만 근접 유닛도 "포구"를 채워야 해 이름 뜻이 어긋난다.
  - C: `effectPoint`가 없으면 모델(Animator GameObject) 위치로 대신한다. 검사가 없어 편하지만 발밑에 잘못 나와도 알 수 없다.
- **Q-E3. 이번 범위**
  - **A(권장):** 공격 클립의 이벤트 타이밍 하나(근접·원거리 공통). 요청 그대로이고, 다른 자리는 같은 조각·창구를 다시 쓰는 한 줄이 된다.
  - B: A + 피격 쪽 이펙트(맞은 유닛 위치, 피격 조립 시작 = 맞는 순간). 값이 "공격자 무기별"인지 "맞은 유닛별"인지 정해야 하고 맞은 유닛 위치 읽기가 더 필요하다.
  - C: B + 코드 모션 공격 길(클립 없는 유닛). 코드 모션 재생(`CombatAttackPlayback`)의 타격 순간에 연결해야 해 범위가 커진다.
- **Q-E4. 결과 재생이 취소될 때 남은 이펙트**
  - **A(권장):** 재생 취소·해제 때 창구가 모두 회수한다(`StopAll`). 폭발 이펙트와 같은 규칙이다. 조각 하나가 취소될 때는 회수하지 않는다(개별 회수 API 없음).
  - B: 자연 종료만 둔다(소리·흔들림처럼). 해제 때만 회수한다.

## 4. 원칙 점검 (7기준, 권장안 기준 예상)

| 기준 | 예상 | 근거 |
|---|---|---|
| SSOT | 9 | 이펙트 종류 = Render DB 하나, 시점 = 클립 `ClipEvent` + 공격 칸 선딜레이, 지점 필수 규칙 = `CombatUnitAnimationRules` 하나, 선딜레이 규칙 = `IsValidDelay` 하나 |
| SOLID | 9 | 조각은 내기만, 창구는 Scope 수명만, 조립은 시점만. 새 자리는 조립 한 줄로 확장. `CombatClipAttack` 입구에 선택 인자 하나가 는다 |
| FW 계약 | 8 | `EffectScope`·`EffectScopeRequest`·`EffectType`을 그대로 씀, Framework 수정 없음. 단 전투 입장마다 Scope가 하나 더 쌓인다(G2와 같은 종류, Framework 해제 API 대기) |
| 독립성 | 8 | 기본값이 모두 Empty라 화면 변화 없음. 다만 커밋 전인 클립 파일과 같은 파일을 고쳐 커밋을 따로 나누기 어렵다(E-F8 — 클립 묶음을 먼저 커밋하거나 함께 커밋) |
| 캡슐화 | 9 | Scope는 창구 안, 조각은 대여에서 지점만 읽음 |
| C#/Unity 권장 | 9 | 풀 재사용(Framework), 이펙트를 낼 때 요청 struct 하나 외 할당 없음 |
| 무의미한 If/Tick | 9 | 조각 Tick 없음, 매 프레임 비용 0, 이펙트 없는 칸은 항목 자체를 넣지 않음 |

예상 약 **8.7/10**.

## 5. Phase 개요 (결정 뒤 Task 계획으로)

- **EF-1 값·지점·검사(화면 변화 없음):** `CombatUnitAnimationSO` 두 칸, `CombatClipAttackData` 값, `CombatAnimationEvents.effectPoint`, `CombatUnitAnimationRules` 지점 필수 규칙, 시험(Unity 전용 SO·규칙 시험).
- **EF-2 창구·조각·조립·연결:** `CombatEffectWindow`, 대여 `TryGetEffectPoint`(계약·`UnitClipChannel`·`FieldUnitView`·시험 가짜 대여 셋), `CombatEffectPiece`, `CombatClipAttack` 한 줄, 처리기·Presenter·Factory, 시험(조립 항목·창구 지역 변환·회수).
- **EF-3 공유·기록:** 호진님(공격 칸 새 칸·이펙트 지점), 민형님(Factory 한 줄 + 새 `EffectType` 등록 요청), 클립 설계 AC7 표 4번 줄을 이 문서로 연결, CLAUDE.md.
- 검증은 지금까지처럼 사용자가 Unity에서 컴파일·시험. 실제 화면 확인은 Render DB에 전투용 이펙트가 등록된 뒤(지금은 `TestFire`·`Boom`으로 임시 확인 가능).
