# 전투 클릭 최단 경로 이동 — Core Phase별 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans`, or `superpowers:subagent-driven-development` when that execution method is explicitly selected, to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax. 이 문서는 구현 요청이 아니다. 구현 요청을 받은 뒤에도 **Phase가 끝나면 멈추고 보고한다.**

**Goal:** 기존 드래그 Move를 유지하면서, View가 두 번 클릭 이동에 사용할 최단 경로와 그 경로를 조회한 시점을 Core 이동 범위 응답으로 제공한다.

**Architecture:** 현재 `Reach` BFS를 작은 내부 계산기로 분리해 이동 범위와 유닛 호버가 함께 사용한다. `MovementRangeResult`는 경로에서 기존 목적지 정보를 파생하고, Systems의 `CombatPreviewService`가 `SourceRevision`을 붙여 게시한다. 클릭 횟수·후보·확정은 View의 후속 작업이며, 실제 실행은 기존 Move 견적·문맥·명령을 사용한다.

**Tech Stack:** Unity `6000.0.73f1`, C#, `ProjectL.Core.Contracts` / `ProjectL.Core.Domain` / `ProjectL.Core.Application` / `ProjectL.Systems`, 기존 Buffer·Job·JMH Framework.

**Spec:** [승인된 Core 설계](../specs/2026-11-17-combat-click-shortest-move-core-design.md). 설계 §4의 C1~C5와 §5의 인수 조건을 함께 읽는다.

**Status / 기준:** 2026-11-17 사용자 설계 승인 후 Core 계획을 작성했다. 계획 작성과 Phase 1 시작 시 확인한 브랜치는 `feature/Cheon/Core/NewCombatSytem`, HEAD는 `dfe3a7e88a95ded64e22c81eb9512dacc5823a28`이다. 설계 작성 당시 HEAD와 달라져 현재 작업 트리의 관련 소스를 다시 대조했다. 최신 진행 상태는 §9를 따른다.

## Global Constraints

- **커밋과 push는 사용자가 직접 한다.** Codex의 커밋·push 단계는 없다. 기존 사용자 변경을 정리하거나 되돌리지 않는다.
- 큰 작업은 **설계 → Task 계획 → Phase 단위 구현** 순서다. 설계와 다른 결정이 필요하면 구현하지 않고 먼저 확인한다.
- **드래그와 클릭을 별도 설정 없이 동시에 사용한다.** 클릭의 첫 입력은 미리보기, 같은 목적지 재클릭은 이동이며, 이 입력 구현은 이 Core 계획에 포함하지 않는다.
- 최단 거리는 **상하좌우 한 칸 진입 횟수**, 동률은 **`Right → Up → Left → Down`** 최초 발견이다. 목적지 출력 순서는 기존 **`Y → X`**다.
- 공개 환경을 사용한다. 위험 회피·추가 비용 점수·숨은 효과를 이용한 경로 선택·적 AI 정책 변경은 없다.
- **Core.Application은 Buffer를 참조하지 않는다.** 경로 판정은 Core, 게시 revision은 Systems 어댑터, 입력 후보와 취소는 View가 소유한다.
- **새 `ClickMove` 명령이나 실행 서비스를 만들지 않는다.** 기존 `Move`·`MoveRange`·Move 견적·`MovementPreviewContext`를 사용한다.
- `MovementDestination`의 기존 API, 이동 실행·효과·Undo, FieldCamera, Framework, DB/SO, 씬은 수정 대상이 아니다. 새 서비스·Factory·전역 캐시도 추가하지 않는다.
- 네임스페이스는 기존 폴더 경로를 따르고 Allman·공백 4칸·LF·UTF-8·명시적 접근자·기본 `sealed`·한국어 주석을 적용한다. 새 `.cs`에는 `.meta`가 필요하다.
- 저장소의 View 작업 검증 제한을 우선한다. **새 Test 코드 작성과 MSBuild/Unity 자동 실행은 이 계획의 Codex 단계에 넣지 않는다.** Core 계획이라는 이유로 제한의 예외를 만들지 않는다. 기존 시험은 API 변경으로 컴파일이 깨지는 호출만 최소로 맞추며, 타인 시험의 실행 실패 수정은 먼저 확인한다.
- 컴파일·재생·기존 EditMode 회귀는 사용자가 Unity에서 확인한다. 확인 전에는 **컴파일 미확인**, 신규 계약 확인 전에는 **신규 계약 검증 미완료**로 기록한다. 정적 검토를 실행 통과로 보고하지 않는다.
- Core 인계 문서를 갱신하면 제목에도 최신 주제·날짜를 반영한다. 이 기능은 전용 View 인계 문서에서 관리한다. Phase 이력은 §4에, 제공 상태·API·View 인수 조건은 §1~§3에 기록한다. 9월 24일 통합 인계에는 이전 링크만 유지한다.

## Review Focus

일반적인 빈 격자 성공 사례만으로는 놓치는 다섯 항목이다. 담당 Task와 §7의 확인 항목을 함께 적용한다.

1. **통과 전용 칸과 종점 전용 칸:** 도약은 중간 통과를 허용하고 자리 교환 종점 너머는 탐색하지 않는다. Task 1·2, K2.
2. **공개 정보와 동률:** 숨은 지뢰만 바뀌어도 같은 경로이며, 위험 칸 점수 때문에 최단성이 바뀌지 않는다. Task 1·2, K1·K3.
3. **범위 공유 소비자:** `UnitMoveCells`는 행동 가능 범위와 입구 조건·관찰 시점이 다르다. 호버 조회에 플레이어 차례 제한을 새로 넣지 않는다. Task 1, K4.
4. **응답 출처와 Job 완료:** 계산 후 revision으로 다시 찍거나 낡은 답을 게시하지 않는다. 응답을 만들지 않았는데 `AnsweredPreviewNumber`만 올려 Job을 완료시키지 않는다. Task 3, K6.
5. **불변 응답과 실행 분리:** 경로 없는 성공 목적지·default 응답·변경 가능한 원본 컬렉션·견적만으로 행동 사용을 소모하는 경우를 막는다. Task 2·4, K5·K7.

---

## 1. 파일과 책임 지도

아래 별칭은 정확한 저장소 상대 경로다. 작업 시작 시 파일의 현재 상태를 다시 확인한다.

| 별칭 | 정확한 경로 | 처리와 책임 |
|---|---|---|
| `SEARCH` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementRangeSearch.cs` | 신규. BFS의 거리·부모와 경로 복원. 같은 경로의 `.meta`도 신규. |
| `CALC` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatMovementCalculator.cs` | 수정. `Reach` 추출, `Range`/`UnitMoveCells` 연결. 실행 계산은 유지. |
| `RESULT` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Results/MovementRangeResult.cs` | 수정. 경로 원본의 응답과 출처 메타데이터. |
| `PREVIEW` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatPreviewService.cs` | 수정. MoveRange에 출처 부착·게시 검사. |
| `VALIDATOR` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Movement/MovementRouteValidator.cs` | 읽기. `CanTraverse`의 기존 판정 계약을 재사용. |
| `JOB` | `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatPreviewJob.cs` | 읽기. 같은 실행 안에서 응답 번호를 요구하는 기존 완료 계약 확인. |
| `HANDOFF` | `docs/superpowers/handoffs/2026-11-17-combat-click-shortest-move-view-handoff.md` | 각 Phase의 Core 변경·제공 상태·검증 수준 기록. |

조회 소비자는 `CombatAttackQueries.UnitRange` → `CALC.UnitMoveCells`다. 적 행동 통합 확인의 `EnemyStageBehaviorCorpus`도 같은 함수를 사용하지만, 적 AI의 길 선택기는 별도 `EnemyMovementPathfinder`다. 이들은 호출 계약을 확인할 대상이며 변경 대상으로 추가하지 않는다.

현재 `MovementRangeResult.Success`의 운영 생산 호출은 `CALC.Range` 한 곳이다. `MovementDestination`은 별도 계약 시험에서도 쓰이므로 그 생성자를 변경하지 않는다. 구현 시 호출처가 늘었으면 같은 명령으로 다시 확인한다.

## 2. Phase 구성과 리뷰 지점

| Phase | 산출물 | 다음 Phase에 주는 것 | 멈출 때 확인할 상태 |
|---|---|---|---|
| **1. 공통 탐색 분리** | 거리·부모를 가진 내부 BFS. 기존 응답 형태 유지. | `MovementRangeSearch.Calculate/Distances/BuildRoute` | 기존 범위·호버 결과 보존, 공통 규칙 중복 없음. |
| **2. 경로 응답 연결** | 경로를 조회할 수 있는 `MovementRangeResult`. DTO와 생산자 함께 이전. | `Success(routes)`·`TryGetRoute`·`SourceRevision`·`WithSourceRevision` | 순수 Core 경로 제공 완료. 이 단계의 응답 출처는 아직 0. |
| **3. 출처 검증과 게시** | 실제 MoveRange 응답에 양수 revision, 게시 직전 검증. | View가 수락 여부를 판단할 완성된 범위 계약 | 기존 서비스·Job 경로 유지, Core 제공 코드 완료 여부 보고. |
| **4. Core 검증·View 인계** | 검증 증거와 제공 계약 인계 | View 후속 계획의 시작 조건 | Core 제공과 클릭 UI 완성을 구분. 미확인 항목 명시. |

Phase 1 → 2 → 3 → 4 순서다. **Phase 2의 DTO와 유일한 생산자 수정은 같은 리뷰 단위**로 묶어 API만 바꾸고 호출처가 깨진 상태에서 끝내지 않는다. Phase 1~2는 View의 클릭 입력 활성화 시점이 아니다.

### 2.1 Phase별 모델 추천

2026-11-17 로컬 Codex 모델 목록에서 확인한 모델명·지원 추론 수준을 기준으로 한 작업 배정 제안이다. 이번 프로젝트에서 모델별 성능을 측정한 결과는 아니다. 모델 선택은 구현 권한이나 검증 제한을 바꾸지 않는다.

| Phase | 추천 모델 | 추론 수준 | 작업 기준의 이유 |
|---|---|---|---|
| 1 — 공통 탐색 분리 | **GPT-6.1-Sol** | **High** | BFS 자체보다 도약/자리 교환의 통과·착지 차이와 두 소비자의 기존 동작 보존을 함께 추적해야 한다. |
| 2 — 경로 응답 연결 | **GPT-6.1-Sol** | **Medium** | 공개 서명·불변식·단일 생산자 이전이 계획에 구체적으로 정해져 있다. 예상 밖 소비자나 계약 충돌이 나오면 High로 올린다. |
| 3 — 출처 검증·게시 | **GPT-6.1-Sol** | **High** | 변경 줄 수는 적어도 revision·요청 번호·진영 턴·Job 완료 조건을 함께 대조해야 한다. |
| 4 — Core 검증·View 인계 | **GPT-6-Astra** | **High** | 최종 diff를 설계·기능 독립성·검증 증거와 교차 검토하는 역할에 배정한다. 실제 Unity 확인은 여전히 사용자가 한다. |

설정을 단순하게 유지하려면 Phase 1~3은 **GPT-6.1-Sol / High**로 통일하고, Phase 4의 최종 검토에서 **GPT-6-Astra / High**를 사용한다. 검토가 끝난 뒤 확인 결과를 인계 문서에 옮기는 작업만 남았다면 Sol / Medium으로 충분하다고 제안한다. 모든 Phase에서 Max/Ultra를 기본값으로 둘 필요는 없다.

## 3. Phase 1 — 공통 탐색 분리

### Task 1: 기존 `Reach`를 거리·부모 검색으로 추출

**Files:** Create `SEARCH`, `SEARCH.meta`; Modify `CALC`의 `Range`, `UnitMoveCells`, `Reach` 및 관련 XML 설명. `VALIDATOR`, `CALC.CanLand`는 판정 근거로만 읽는다.

**Interfaces:**

- Consumes: `MovementRouteValidator.CanTraverse(CombatCommandInput, CellCoord, bool, MovementEnvironmentFacts)`와 `CombatMovementCalculator.CanLand(CombatCommandInput, CellCoord, MovementEnvironmentFacts, CombatProfileReadSet = null)`의 기존 의미.
- Produces: 같은 `Application.Combat` 네임스페이스의 `internal sealed class MovementRangeSearch`와 다음 내부 API.

```csharp
internal static MovementRangeSearch Calculate(
    CombatCommandInput input, CharacterCombatFacts actor,
    MovementEnvironmentFacts environment, int allowance);
internal IReadOnlyDictionary<CellCoord, int> Distances { get; }
internal MovementPath BuildRoute(CellCoord target);
```

`Calculate`는 검증된 필드 행위자·공개 환경·0 이상 한도를 받는다. 거리 사전에는 원점 0과 통과 전용/종점 전용 정점이 함께 있다. 내부 부모 사전은 공개하지 않는다. `BuildRoute`는 발견한 정점까지 원점 포함 경로를 복원하고, 없는 정점이면 `ArgumentException(nameof(target))`이다. 원점을 지정하면 원점 하나짜리 경로이며, **실제 목적지에서 원점을 제외하고 착지 가능 칸만 고르는 책임은 Range에 남는다.**

- [x] **1.1 기준 확인:** `git status --short`, 현재 branch/HEAD, `CALC`와 `RESULT` 생산·소비 호출을 확인한다. 사용자 변경과 겹치는 부분을 보존하고 계획의 기준과 다른 계약이 있으면 기록한다.
- [x] **1.2 탐색 추출:** `SEARCH`에 기존 `Reach`의 FIFO BFS를 옮긴다. 방향 순서는 한 곳에 `Right, Up, Left, Down`으로 둔다. 처음 발견한 칸의 거리와 부모만 기록한다. 같은 거리로 다시 발견해도 부모를 교체하지 않는다.
- [x] **1.3 통과/종점 보존:** 통과 가능 칸은 큐에 넣고, 통과 불가이나 `CALC.CanLand`가 허용한 칸은 거리·부모만 기록한다. 기존 `CanTraverse`와 강화된 `CanLand`를 호출하며 특성 이름·벽/지뢰 판정을 새로 쓰지 않는다.
- [x] **1.4 복원과 수명:** 부모를 따라 역추적 후 뒤집어 `MovementPath`를 만든다. 거리·부모는 이번 계산 객체 안에서만 소유하고 반환 후 바꾸지 않는다. 원본 사전의 쓰기 참조를 노출하지 않는다. 부모 연결 불일치를 정상적인 짧은 경로로 숨기지 않는다.
- [x] **1.5 소비자 이전:** `CALC.Range`와 `UnitMoveCells`가 `SEARCH.Calculate(...).Distances`를 읽게 바꾸고 기존 `Reach`를 제거한다. 이 Phase에서는 `Range`의 기존 `MovementDestination` 생산 API와 Y/X 정렬을 유지한다. `UnitMoveCells`의 다른 입구 조건·호출자 제공 환경·출력 정렬을 유지하며 경로를 복원하지 않는다.
- [x] **1.6 정적 검토:** 새 파일 `.meta`의 GUID 고유성, 네임스페이스·순수 C# 경계, `git diff --check`를 확인한다. `rg -n 'Reach\(|MovementRangeSearch|BuildRoute' Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`로 기존 BFS가 남지 않았고 두 소비자가 같은 탐색을 사용하는지 대조한다.
- [x] **1.7 사용자 확인·보고:** §7 K1~K4의 Phase 1 범위 및 기존 `MovementRouteTests`·`LeapTraitTests`·`PositionSwapMovementTests`·`UnitAttackRangeQueryTests` 확인 항목을 인계한다. `HANDOFF` §1~§4와 실행 기록에 구현·정적 검토·컴파일/실행 상태를 나누어 적고 **멈춘다.**

**완료 조건:** 내부 검색이 실제 두 소비자에서 사용되고, 기존 목적지/거리/호버 칸의 의미가 유지된다. 클릭 입력·효과 실행 코드를 건드리지 않는다. 이 Phase의 검토에 필요한 실행기 내부는 `CanLand` 계약까지이며 `ProjectRoute`의 효과 연쇄 전체를 재구성하지 않는다.

## 4. Phase 2 — 경로 응답 연결

### Task 2: 경로를 원본으로 하는 성공 응답과 생산자를 함께 이전

**Files:** Modify `RESULT`, `CALC.Range`. `MovementPath`·`MovementDestination`·기존 View 소비자 파일은 읽기 확인 대상이다. 현재 확인된 호출처 기준으로 시험 파일 변경은 예상하지 않는다.

**Interfaces:**

- Consumes: Task 1의 `Calculate/Distances/BuildRoute`, 기존 불변 `MovementPath`.
- Produces: `MovementRangeResult`의 기존 결과 프로퍼티·Failure API를 유지하면서 다음 공개 API를 제공한다.

```csharp
public static MovementRangeResult Success(
    EntityId unitId, IReadOnlyList<MovementPath> routes);
public bool TryGetRoute(CellCoord target, out MovementPath route);
public long SourceRevision { get; }
public MovementRangeResult WithSourceRevision(long revision);
```

- [x] **2.1 생산 호출 전수 확인:** `rg -n 'MovementRangeResult\.Success\(|new MovementDestination' Assets/ProjectL/Script Assets/Tests`로 이전 대상을 다시 확인한다. `new MovementDestination` 전체를 일괄 수정하지 않는다.
- [x] **2.2 성공 생성 계약:** 유효한 `unitId`, null이 아닌 경로 목록, null이 아닌 1걸음 이상 경로, 공통 원점, 고유 종점을 검사한다. null 인자는 `ArgumentNullException`, 잘못된 경로 목록/식별자는 `ArgumentException`이다. 정상적인 빈 목록은 성공한 빈 범위로 허용한다. 맵·행동 규칙 검증은 DTO로 옮기지 않는다.
- [x] **2.3 원본과 파생값:** 경로 목록을 방어적으로 복사해 private 저장소로 소유한다. 각 경로의 `Endpoint`, `StepCount`, `[Endpoint]`에서 기존 `Destinations`를 생성한다. 서로 다른 거리·경로를 입력받는 API를 만들지 않는다. `MovementDestination` 생성자는 그대로 둔다.
- [x] **2.4 조회·기본값:** `TryGetRoute`는 저장된 목적지 경로를 반환한다. 실패·빈 범위·default·없는 목적지는 false/out null이다. 이번 규모에서는 내부 배열 순회로 충분하며 조회마다 경로/컬렉션을 새로 만들지 않는다. `Success`에는 경로 없는 목적지를 넣는 호환 overload를 남기지 않는다.
- [x] **2.5 출처 API 준비:** 순수 계산 성공/실패의 `SourceRevision`은 0이다. `WithSourceRevision`은 양수만 허용하고(0 이하 `ArgumentOutOfRangeException`), default 응답에는 `InvalidOperationException`을 낸다. 정상 성공·빈 범위·실패 결과의 나머지 사실은 그대로 유지한다. 원본 응답을 바꾸지 않고 불변 저장소를 공유하는 새 값으로 반환해도 된다.
- [x] **2.6 Range 연결:** `CALC.Range`에서 기존 행위자·배치 검사를 유지하고, 검색 결과 중 거리 > 0 및 `CALC.CanLand`를 통과한 칸을 Y/X 정렬한다. Task 1의 `BuildRoute`로 각 목적지 경로를 한 번만 복원하고 새 `Success(actorId, routes)`를 호출한다. `UnitMoveCells`에는 이 복원 단계를 넣지 않는다.
- [x] **2.7 원자적 API 이전 확인:** DTO와 `Range`를 함께 검토한다. 위 2.1 명령을 다시 실행해 제거한 `Success(EntityId, MovementDestination[])` 호출이 없고 공통 `MovementDestination` 소비가 그대로인지 확인한다. `git diff --check`, 메모리 소유·default/빈 범위 처리를 정적 확인한다.
- [x] **2.8 사용자 확인·보고:** §7 K1~K5를 확인 항목으로 넘긴다. 특히 모든 목적지에 경로가 존재하는지와 출처 0이 미게시 뜻임을 기록한다. `HANDOFF` §1~§4와 실행 기록을 갱신하고 **멈춘다.**

**완료 조건:** 반환된 목적지마다 정확히 하나의 최단 경로가 있으며 `StepDistance == route.StepCount`다. DTO만 이전한 컴파일 불일치 상태를 Phase 종료 결과로 삼지 않는다. 경로 계약은 준비됐지만 실제 게시 revision 연결은 Phase 3에 남아 있음을 보고한다.

## 5. Phase 3 — 출처 검증과 게시

### Task 3: 기존 MoveRange 응답에 계산 출처를 붙인다

**Files:** Modify `PREVIEW`의 MoveRange 분기와 게시 전 조건. `JOB`, Buffer의 `QueryRevision`·응답 번호, `CombatPreviewResult.ForMoveRange`는 읽기 확인 대상이다.

**Interfaces:**

- Consumes: Task 2의 `WithSourceRevision(long)`과 `SourceRevision`.
- Produces: `CombatPreviewResult.ForMoveRange(request.Number, stampedRange)`를 기존 `_battle.PublishPreview(result)`로 게시한다. `ICombatPreviewService.ProcessLatestPreview(): bool`의 공개 서명과 호출 수명은 그대로다.

- [x] **3.1 출처 연결:** `ProcessLatestPreview`가 입력 조립 전에 읽은 `long revision = _combat.QueryRevision`을 사용한다. MoveRange 분기의 `CombatMovementCalculator.Range(input)` 결과에 `.WithSourceRevision(revision)`을 적용한다. 실패/빈 범위도 같은 출처를 가진 정식 응답이다.
- [x] **3.2 게시 전 검사:** 기존 요청 번호·전투 검사와 revision/진영 턴 검사에 MoveRange를 포함한다. 계산 뒤 최신 값을 stamp로 다시 읽지 않는다. 다른 Preview 종류의 정책을 일괄 교체하지 않는다.
- [x] **3.3 오래된 답 처리:** 기존 방어 분기처럼 오래된 결과를 게시하지 않는다. 응답 번호만 올리기, 빈 성공 답으로 바꾸기, 자동 재요청/Move 실행을 넣지 않는다. 정상적인 현재 요청은 응답을 한 번 게시해 기존 응답 번호 계약을 만족한다.
- [x] **3.4 Job 경계 검토:** `JOB.Execute`는 같은 실행 안에서 `AnsweredPreviewNumber >= requestNumber`를 요구한다. 현재 Range는 동기·불변 입력 계산이며 여기서 비동기 대기나 yield를 추가하지 않는다. 방어 검사에서 버린 응답을 Job 성공으로 가장하지 않는다. 실제 운영 순서에서 계산 도중 판 교체가 가능하다는 증거가 나오면 기존 완료 계약과의 충돌을 먼저 보고하고, Job/Plan을 임의 수정하지 않는다.
- [x] **3.5 조회의 비변경성 확인:** Range/경로 조회와 stamp 부착이 유닛 위치·행동 사용·난수·명령 큐·결과 순번을 바꾸지 않는지 확인한다. Preview 응답 번호의 정상적인 갱신과 게임 실행 결과의 변화를 구분한다.
- [x] **3.6 정적 검토:** `git diff --check`와 `rg -n 'MoveRange|WithSourceRevision|QueryRevision|PublishPreview' Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatPreviewService.cs`를 확인한다. 예상 차이는 MoveRange 결과 stamp와 해당 종류의 게시 검사다. 새 서비스/등록/enum/Buffer 쓰기 경로가 없어야 한다.
- [x] **3.7 사용자 확인·보고:** §7 K6~K7, 기존 `MovementPublicationTests`와 `MovementUndoPublicationTests` 확인 항목을 넘긴다. `HANDOFF`와 실행 기록에 Core 제공 코드 상태·미실행 항목을 적고 **멈춘다.**

**완료 조건:** 게시된 MoveRange의 출처가 계산 당시 양수 revision이며, 오래된 답을 최신 답으로 위장하지 않는다. 새 클릭 실행 기능이 Core에 들어가지 않는다. View의 최신 출처 비교는 여전히 후속 View 작업이다.

## 6. Phase 4 — Core 검증과 View 인계

### Task 4: 승인 경로와 기존 Move 실행의 결합을 확인하고 제공 상태를 남긴다

**Files:** Modify 실행 기록(이 계획), `HANDOFF` §1~§4 및 제목. 코드 수정이 필요하면 해당 원인의 Phase로 돌아가 범위를 명시하며, 여기서 임의의 이동 규칙 개선을 묶지 않는다.

**Interfaces:**

- Consumes: Phase 3의 MoveRange → `TryGetRoute` → 기존 `CombatViewPreviewData.Move(actor, path)` → `MovePreview`/`MovementPreviewContext` → 기존 `CombatViewCommandData.Move(actor, path, context)`.
- Produces: 제공 API·실제 확인 범위·남은 View 조건을 담은 인계 기록. 별도 실행 API는 없다.

- [ ] **4.1 Core 경계 확인:** §7의 전체 K1~K7을 확인하고, 이미 확인한 동일 변경을 이유 없이 반복하지 않는다. 누락된 신규 계약 검증은 기존 회귀 통과와 별도 목록으로 유지한다.
- [ ] **4.2 기존 실행 재사용 확인:** 경로 견적만 요청했을 때 실행 변화가 없고, 유효 문맥으로 기존 Move 하나를 제출했을 때 해당 경로의 결과·사건·행동 사용·Undo가 기존 규칙과 일치하는지 확인한다. 기존 `MovementContextCalculator`·실행기·Undo에 클릭 전용 분기가 없어야 한다.
- [x] **4.3 View 소비 계약 전달:** `SourceRevision > 0`과 현재 revision 비교, 원점·행위자·전투·요청 번호·세대 대조, 첫 클릭은 견적만/두 번째 클릭만 제출, 다른 목적지는 미리보기 교체, 대기 클릭 예약 금지, revision 변경 시 후보 전체 취소를 설계 §5 링크로 인계한다. Core 경로가 있다는 이유로 첫 클릭에서 `Confirm`을 호출하지 않도록 기존 자동 제출 흐름을 지적한다.
- [x] **4.4 구조 검토:** §8의 수정·이해·검증 범위 비교와 SSOT·SOLID·JMH/FW 표를 실제 diff에 대조한다. 예상했던 경계가 유지됐는지 기록하고, 범위 밖 발견은 후속 제안으로만 남긴다.
- [x] **4.5 최종 기록:** Core 변경 파일, 공개 서명, 사용자 Unity 확인 결과/미확인, 관련 기존 시험 결과, 신규 계약 확인 여부, View 미구현 범위를 적는다. `HANDOFF` 제목과 §1~§4를 갱신하고 **멈춘다.** 외부 발송·커밋·push는 하지 않는다.

**2026-11-17 확인 상태:** 4.1·4.2의 정적 대조는 완료했다. 실제 실행으로 확인할 부분은 증거가 없어 체크를 남겼다. 상세 근거와 미확인 항목은 §9.4를 따른다.

**완료 구분:** 정적 검토와 코드 제공이 끝나면 **Core 코드 인계 가능**으로 보고할 수 있다. §7의 필요한 실행 증거가 없으면 **Core 검증 완료**라고 쓰지 않는다. 두 번 클릭 UI의 완성은 이 계획의 완료 기준이 아니며 View 별도 계획·구현·Unity 인수를 거친다.

## 7. 검증 계획과 증거

이 절의 실행 주체는 사용자 Unity 확인이다. Codex는 코드/문서 대조와 허용된 정적 명령만 수행한다. 신규 자동 시험 작성이나 임시 실행 하네스 추가로 제한을 우회하지 않는다. 필요한 신규 계약 증거를 얻지 못하면 미확인으로 남긴다.

### 7.1 신규 계약 확인

Core만 제공된 동안은 새 클릭 입력이 없으므로 **유닛 선택으로 발생하는 기존 MoveRange 응답을 디버거에서 관찰**해 확인한다. DTO·내부 경로의 생성/조회 예외 등 화면에서 드러나지 않는 것은 사용자 디버거/Immediate 확인 대상으로 분리한다. 표의 상태를 만들 수 없거나 관찰하지 않았으면 그 항목은 미실행이다.

| ID / 담당 | 입력·조건 | 확인할 값과 합격 기준 |
|---|---|---|
| **K1 / P1·P2** | 입구 자격을 만족한 단일 칸 유닛, 5×5 격자, 원점 `(1,1)`, 목표 `(3,2)`, 한도 3. 이후 `(2,1)`을 막고 목표 `(3,1)`, 한도 4/3 비교. | 빈 격자는 `(1,1),(2,1),(3,1),(3,2)`의 3걸음. 장애물 사례는 위쪽 우회 4걸음, 한도 3이면 목표 없음. 원점·맵 밖·고립 목표 없음. 반복 조회 동률 동일. |
| **K2 / P1·P2** | 도약의 벽/절벽/유닛 통과와 착지, 자리 교환 가능 종점 및 역자리 막힘. | 중간 전용 칸은 경로에 있어도 목적지에 없음. 종점 전용 칸 너머로 탐색하지 않음. 특성 없는 유닛과 기존 규칙 차이 유지. |
| **K3 / P1·P2** | 더 짧은 위험 경로와 긴 안전 경로, 공개 상태 동일한 두 입력에서 숨은 지뢰만 변경. | 최단 걸음 우선, 숨은 정보로 경로가 달라지지 않음. 실제 효과 중단을 경로 탐색 실패로 혼동하지 않음. |
| **K4 / P1** | 같은 보드의 행동 범위와 유닛 호버 조회, 적 유닛/이미 행동한 유닛 조회. | 각 입구의 기존 자격 차이 유지. `UnitMoveCells`의 칸과 정렬 보존, 경로 전체를 만들지 않음. 공격 계산 내부 변경 없음. |
| **K5 / P2** | 각 목적지, 빈 성공·실패·default·범위 밖 조회. 입력 목록 변경, 혼합 원점/중복 종점/null/0걸음 경로, revision 0/음수. | 모든 목적지의 경로·거리·종점 일치. 공개 응답 불변. 정상 빈 범위와 실패 구분. 부적합 성공 입력/출처는 정한 예외, false 조회는 out null. |
| **K6 / P3** | 현재 MoveRange 게시, 게시 뒤 게임 상태 변경과 재조회. 계산 중 요청/판/턴 불일치의 방어 분기 코드 대조. | stamp는 최초 캡처 값, 새 조회는 새 값. 불일치 결과 게시 없음. 정상 Job 응답 번호 충족. 동기 실행에서 만들 수 없는 중간 변경은 정적 검토로만 기록하고 실행 재현으로 주장하지 않음. |
| **K7 / P3·P4** | Range → 경로 견적만 호출, 이어 유효 context로 Move 1개 제출. 문맥 이후 점유/한도 변경, 효과 중단, 기존 Undo. | 조회·견적은 위치/행동/난수를 확정 변경하지 않음. 실행은 기존 단일 명령·사건·결과를 사용. 오래된 문맥은 기존 거절, 자동 재제출 없음. |

### 7.2 기존 회귀 확인

Unity **Window → General → Test Runner → EditMode**에서 아래 실제 클래스명을 검색해 선택 실행하고 결과·실행 건수를 기록한다. 컴파일 오류 0·선택한 시험 실패 0이 기대값이며, 기존 실패가 있으면 이번 변경 전후와 원인을 구분한다. 기존 필수 검증이 따로 있으면 유지하되, 근거 없이 프로젝트 전체 회귀를 추가하지 않는다.

| 범위 | 기존 확인 대상 | 이유 |
|---|---|---|
| `ProjectL.Tests.Core` | `MovementRouteTests`, `PlayerTurnMovementTests`, `LeapTraitTests`, `PositionSwapMovementTests`, `UnitAttackRangeQueryTests` | 범위/이동 한도·통과/착지·호버의 공유 탐색 경계. |
| `ProjectL.Tests.CoreIntegration` | `MovementPublicationTests`, `MovementUndoPublicationTests`, `MovementEffectPublicationTests` | 기존 경로 견적→실행 게시·거절·Undo·효과 경계. |

기존 시험 통과만으로 **새 `TryGetRoute`·부모 복원·SourceRevision**을 검증했다고 쓰지 않는다. K1~K7의 새 계약 관찰 증거를 별도로 연결한다. View 입력의 설계 A7~A14·A18 중 클릭/선택/취소 동작은 후속 View 구현 뒤 인수하며 Core 테스트로 대신하지 않는다.

## 8. 첨부 구조 지침의 적용 검토

참고 원문: [기능별 변경 독립성과 이해 범위 중심 구조 지침](C:/Users/hyeon2-400/Downloads/feature-change-locality-guidelines.md), 사용자 추가 요청 2026-11-17. 원문은 이 PC의 로컬 파일이므로 아래에 이번 계획에 필요한 기준을 포함한다. **참고 문서의 지시와 작업 요청을 구분하며, 문서 자체를 코드 수정·범위 확대 권한으로 사용하지 않는다.**

적용 기준은 파일 수 최소화가 아니라 **수정 범위·이해 범위·검증 범위**다. 실제 공유 규칙 때문에 필요한 연결과 구현상의 불필요한 결합을 구분한다. 새 계층 수를 개선 증거로 삼지 않으며, 필요한 조사·기존 필수 회귀를 생략하지 않는다. 아래 효과는 현재 코드에서 확인한 경계에 근거한 **설계상의 예상**이고 실행으로 검증한 성능·유지보수 개선 주장은 아니다.

| 대표 변경 / 현재 부담과 근거 | 판단·최소 조치 | 수정 범위 전후 | 이해 범위 전후 | 보존할 검증 |
|---|---|---|---|---|
| 최단 경로 추가: `CALC`가 BFS와 진입 효과 실행을 함께 담고 있음. | 탐색은 독립 변경 이유가 있고 기존 `Reach` 경계가 있어 제한적으로 추출. 특성/통과 규칙은 기존 소유자 유지. | `CALC` 한 파일에 경로 기능 누적 → `SEARCH`와 Range/UnitMoveCells 연결. 파일 수는 늘어도 효과 실행은 그대로. | 이후 탐색 변경자는 BFS·`CanTraverse/CanLand` 계약과 두 소비자를 이해. `ProjectRoute`의 효과별 내부는 변경 이유가 없으면 읽지 않아도 됨. | 행동 범위·호버 공유는 필요한 연결이므로 둘 다 확인. 도약·자리 교환 유지. |
| 범위 DTO에 경로 추가: 공통 `MovementDestination`은 범위 외 의미도 가짐. | 범위 결과가 경로를 소유하고 기존 목적지 값은 파생. DTO/생산자 이전은 같은 Phase. | 공통 목적지 타입·견적·시험 호출 전체 확산을 피하고 `RESULT`/`CALC.Range`로 한정. | 경로 응답 소비자는 불변 목록·실패·출처 계약을 알면 됨. 다른 이동 결과의 내부 저장방식과 무관. | 기존 Destinations 소비, 빈/실패/default, 방어적 복사. |
| 출처 추가: `PREVIEW`는 여러 요청을 조율하고 `JOB`은 응답 번호를 검사함. | 런타임 조율은 정당한 공유 책임. MoveRange의 stamp/가드만 추가하고 서비스 분할·Factory 신설하지 않음. | `PREVIEW`의 해당 분기/조건과 문서. | 입력 조립→동기 계산→게시→Job 완료 계약은 계속 필요. 공격·스킬의 계산 내부까지 이해할 필요는 없음. | 응답 번호·revision·전투/턴·조회 비변경성. 방어 가드가 완료 계약을 우회하지 않음. |

SSOT는 기존 통과·도착 규칙, 한 BFS, 경로 원본의 응답, 기존 실행 context로 유지한다. **S**는 검색/응답/게시/실행 분리, **O**는 기존 Move에 새 입력 근거를 제공하는 확장, **L**은 드래그의 승인·거절·중단 보존, **I**는 거리·경로·출처에 한정한 계약, **D**는 Core.Contracts와 기존 Buffer/서비스 경계를 통해 점검한다.

JMH/FW 조립 경로는 `CombatSystemFactory` 생성 → Core Builder 내부 등록 → `InGameCoreSystemGroup`/`InGameSystemGroup.CoreGroup` 공개 → `CombatStepContext`/`CombatPreviewJob` 소비 그대로다. 새 등록/Builder/루트 Export가 없어 FW-001~003의 구조를 유지하고, 게시와 입력의 기존 경계(FW-004~007)를 우회하지 않는다. UI/렌더/이벤트/Pool/Queue/수명 계약(FW-008~016)은 새 동작을 넣지 않고 기존 소유자가 유지한다.

**남는 의존 비용:** `SEARCH`는 `CALC.CanLand`를 호출하고 `CALC.Range/UnitMoveCells`는 `SEARCH`를 사용한다. 이는 전체 이동 실행기의 동작을 호출하는 순환이 아니라 착지 판정이라는 좁은 기존 함수 의존이다. 이를 피하려고 판정을 복제하거나 범용 콜백/전략 계층을 만들지 않는다. 이 함수의 책임까지 바꿔야 한다면 이번 추출과 별도 설계 변경으로 다룬다.

## 9. 설계 대응과 실행 기록

| 설계 항목 | 계획에서 맡는 곳 |
|---|---|
| C1~C2 최단성·동률·통과/착지·공개 환경 | Phase 1·2, K1~K4 |
| C3 DTO·경로 조회·출처 API | Phase 2, K5 |
| C4 Core stamp·게시 검사 | Phase 3, K6. View의 현재 출처 비교·취소는 인계 대상. |
| C5 기존 Move 합류·비변경 조회·실행/Undo 보존 | Phase 4, K7 |
| V1~V2 두 번 클릭·선택·슬롯·취소 | Phase 4에서 제공 조건 인계. View 구현은 이 계획에 없음. |
| §7 SSOT/SOLID/JMH/FW·기능 독립성 및 첨부 지침 | 모든 Phase의 경계와 본 계획 §8 |

| Phase | 코드 상태 | 정적 검토 | 사용자 컴파일/실행 | 보고·다음 Phase |
|---|---|---|---|---|
| 1 | 공통 탐색 분리 및 두 호출처 연결 완료 | 기존 분기·정렬·소비 계약 대조, 호출 검색, GUID와 공백 검사 완료 | 컴파일 미확인, K1~K4·기존 EditMode 미실행 | 2026-11-17 첫 Phase 구현. Phase 1 리뷰 지점 |
| 2 | 경로 조회 계약과 `Range` 생산자 이전 완료 | 단일 성공 생산자, 목적지 파생·메모리 소유·실패/빈/default 처리와 공백 검사 확인 | 컴파일 미확인, K1~K5·기존 EditMode 미실행 | 2026-11-17 Phase 2 리뷰 지점 |
| 3 | MoveRange 출처 부착과 게시 직전 검증 연결 완료 | 리비전 캡처·게시·Job 완료 경로, 공백 검사 확인 | 컴파일 미확인, K6~K7·기존 EditMode 미실행 | 2026-11-17 Phase 3 리뷰 지점 |
| 4 | 추가 코드 수정 없음, Core 코드 인계 가능 | 전체 변경 독립 리뷰·기존 Move 경계·구조 대조 완료, 구현 결함 미발견 | 컴파일 미확인, K1~K7 실행 관찰·기존 EditMode 미실행 | 2026-11-17 정적 검토·View 인계 기록 완료. 실행 인수 대기 |

계획 작성 자체는 현재 소스·공개 서명·호출처·첨부 지침을 대조하고 설계 대응, Phase 경계, 타입/메서드명, 검증 제한을 자체 검토했다. **코드·에셋·시험 변경 없음. 컴파일 미확인.** 계획 작성은 구현 승인이나 실행 완료가 아니다.

### 9.1 Phase 1 실행 기록 — 2026-11-17

- `MovementRangeSearch`와 Unity 메타를 추가하고 `Range` 및 `UnitMoveCells`를 이전했다. `BuildRoute`는 탐색에서 기록한 거리로 원점 포함 배열 길이를 정한 뒤 부모를 따라 뒤에서부터 채운다. 거리 사전은 읽기 전용으로 제공한다.
- 기존 BFS와 대조해 통과 가능 칸의 큐 등록, 착지만 가능한 칸의 종점 처리, 양쪽 조회의 환경·착지 필터·Y/X 정렬을 확인했다. SSOT는 기존 `CanTraverse`/`CanLand`와 단일 BFS에 있다. SOLID 검토에서 탐색·응답·실행의 책임 분리(S), 내부 경로 기능 확장(O), 기존 호출 계약 유지(L), 좁은 내부 조회 API(I), 순수 Core 타입 의존(D)을 확인했다. 지원 범위는 두 조회 경로이며 JMH/FW 서비스 조립과 입력·게시 경계 변경은 없다.
- **사용자 Unity 확인 항목.** §7 K1~K3의 경로는 `MovementRangeSearch.Calculate` 반환값과 `BuildRoute`를 디버거에서 확인할 수 있다. K4는 기존 호버 결과를 사용한다. 기존 회귀는 `MovementRouteTests`, `LeapTraitTests`, `PositionSwapMovementTests`, `UnitAttackRangeQueryTests`다. **컴파일 미확인. 신규 계약 검증 미완료.**
- [전용 View 인계 §4.1](../handoffs/2026-11-17-combat-click-shortest-move-view-handoff.md)와 같은 문서 §1~§3에 기록했다. 다음 리뷰 단위는 Phase 2의 경로 응답 계약과 `Range` 생산자 이전이다.

### 9.2 Phase 2 실행 기록 — 2026-11-17

- `MovementRangeResult.Success(unitId, routes)`는 경로 목록을 복사해 보관하고 `Endpoint`, `StepCount`, `[Endpoint]`에서 `Destinations`를 만든다. 기존 성공 서명의 유일한 생산 호출인 `Range`를 함께 이전했다. `Range`는 기존 자격 및 착지 검사를 거쳐 찾은 목적지를 Y/X로 정렬한 뒤 각 경로를 한 번씩 복원한다.
- `TryGetRoute`는 보관된 불변 경로를 조회한다. 실패·빈 성공·default·범위 밖은 false와 null이다. `WithSourceRevision`은 양수 출처를 붙인 새 값을 만들며 정상 실패와 빈 성공도 보존한다. 현 단계의 범위 응답은 출처 0이며 게시 어댑터 연결은 Phase 3이다.
- SSOT는 보관된 경로다. 경로 탐색·응답 파생·게시의 책임을 분리하고(S), 기존 목적지 소비 계약 위에 경로 조회를 추가했다(O·L). 조회에는 목적지 좌표만 필요하고(I) Core.Contracts는 순수 Core 타입만 사용한다(D). 정적 검토는 DTO, `Range`, 기존 `Destinations` 소비 경계에 집중했다. 호버는 Phase 1의 칸 조회를 유지하고 JMH/FW 조립과 게시 경로도 기존 연결을 사용한다.
- **사용자 Unity 확인 항목.** §7 K1~K5에서 `MoveRange`의 모든 목적지에 대해 `TryGetRoute` 성공과 `TargetAnchor == route.Endpoint`, `StepDistance == route.StepCount`를 확인한다. K5는 입력 목록 변경, 빈 성공과 실패, 잘못된 경로·출처도 포함한다. **컴파일 미확인. 신규 계약 검증 미완료.**
- [전용 View 인계 §4.2](../handoffs/2026-11-17-combat-click-shortest-move-view-handoff.md)와 같은 문서 §1~§3에 기록했다.

### 9.3 Phase 3 실행 기록 — 2026-11-17

- `CombatPreviewService`는 입력 조립 전에 읽은 `QueryRevision`을 `MoveRange` 응답에 부착한다. 실패와 빈 성공에도 같은 값을 붙인다. 활성 전투의 리비전은 1부터 시작한다. 게시 전 요청 번호·전투 인스턴스·리비전·진영 턴을 대조하고 불일치 응답은 게시 전에 반환한다.
- 기존 `CombatNextJob`이 동기적으로 `CombatPreviewJob`을 실행하며, 완료는 `AnsweredPreviewNumber`로 판단한다. 정상 게시가 갱신하는 값은 최신 미리보기와 응답 번호다. 방어 분기에서 반환하면 두 값은 갱신되지 않으며, 요청 번호에 해당하는 응답이 여전히 없으면 기존 Job 예외 계약이 적용된다. 계산 중 판 교체에 대한 방어 분기는 정적으로 검토했다. 실행 재현은 미확인이다.
- SSOT는 `CombatStateBuffer.QueryRevision`이다. 계산과 게시 책임은 각각 Core 계산기와 기존 서비스가 맡고(S), 기존 게시 검증의 적용 대상에 `MoveRange`를 포함했다(O). 서비스와 Job의 계약을 유지하며(L), 기존 응답·읽기 및 쓰기 Sink로 필요한 값만 전달한다(I·D). JMH/FW 조립과 서비스 조회 경로는 기존 Factory → Core Group → Job 연결을 사용한다. 정적 검토 범위는 `MoveRange` 계산·게시·Job 완료 경계였으며, Range와 출처 부착은 이동 실행 상태를 쓰지 않는다.
- **사용자 Unity 확인 항목.** §7 K6~K7에서 최초 캡처 리비전과 게시 응답을 대조하고, 상태 변경 뒤 새 응답의 리비전과 정상 응답 번호를 확인한다. 기존 `MovementPublicationTests`·`MovementUndoPublicationTests`도 확인한다. **컴파일 미확인. 신규 계약 검증 미완료.**
- [전용 View 인계 §4.3](../handoffs/2026-11-17-combat-click-shortest-move-view-handoff.md)과 같은 문서 §1~§3에 기록했다. 다음 리뷰 단위는 Phase 4의 Core 검증과 View 인계다.

### 9.4 Phase 4 정적 검토와 View 인계 — 2026-11-17

**판정: Core 코드 인계 가능. 컴파일 미확인. 신규 계약 실행 검증 미완료.** 사용자 요청에 따라 Phase 1~3의 작업 트리를 검토했다. 기준 브랜치와 HEAD는 문서 상단과 같으며, 구현은 아직 커밋되지 않은 변경이다. 이 Phase에서는 계획과 `HANDOFF`만 갱신했다. Core/View 코드·시험·씬·에셋은 추가로 수정하지 않았다.

**검토 범위와 결과.** §1의 `CALC`·`RESULT`·`PREVIEW` 수정 diff와 신규 `SEARCH`/`.meta`를 대상으로 직접 검토와 별도 GPT-6-Astra / High 읽기 전용 리뷰를 수행했다. 승인 설계에서 벗어난 구현이나 수정을 요구할 결함은 발견하지 못했다. 아래는 코드 대조 증거이며 실행 통과 기록이 아니다.

| 항목 | 정적으로 확인한 근거 | 남은 사용자 확인 |
|---|---|---|
| K1 | `SEARCH.Calculate`의 FIFO, Right/Up/Left/Down 순서, 최초 발견 거리·부모와 `BuildRoute` 역복원. `CALC.Range`의 양수 거리·착지 필터 및 Y/X 정렬. | §7.1의 실제 경로·장애물 우회·한도·반복 동률 관찰. |
| K2 | 통과 가능 칸만 큐에 넣고 착지만 가능한 칸은 종점으로 기록한다. 기존 `CanTraverse`와 강화된 `CanLand`의 자리 교환·면역·프로필 판정을 재사용한다. | 도약 중간 통과/착지 차이, 자리 교환 종점과 역자리 불가 사례. |
| K3 | 행동 범위는 `VisibleTo(actor.Owner)` 환경을 사용한다. 새 탐색에 위험 점수·숨은 지뢰 조회·적 AI 경로 호출이 없다. | 공개 상태가 같은 두 입력의 경로 동일성 및 위험 경로 최단성. |
| K4 | `UnitMoveCells`의 기존 별도 자격 검사, 호출자 환경, Y/X 정렬을 유지한다. 거리만 소비하고 `BuildRoute`를 호출하지 않는다. | 적/행동 완료 유닛의 호버 범위와 기존 회귀. |
| K5 | 입력 경로 목록을 복사하고 불변 `MovementPath`를 보관한다. 목적지와 거리는 경로에서 파생한다. null/0걸음/혼합 원점/중복 종점 및 출처 유효성 검사, 실패·빈 성공·default 조회를 대조했다. | 목록 변경 후 불변성, 모든 경로·거리 일치, 예외와 false/out null 관찰. |
| K6 | 입력 조립 전 revision을 부착하고 게시 전 요청·전투·revision·진영 턴을 검사한다. 활성 전투 revision은 1부터다. `JOB`은 실제 응답 번호를 요구하며, 거절 분기는 번호를 올리지 않는다. | 정상 게시/상태 변경 뒤 새 stamp·응답 번호 관찰. 동기 계산 중 변경 분기는 정적 확인만 했으며 재현을 주장하지 않는다. |
| K7 | Range/경로 조회는 값만 생성한다. 기존 Move 견적·문맥 포착, 명령 서비스의 문맥 대조/낡은 문맥 거절, Move 계산·게시·Undo 경로에 클릭 전용 분기나 자동 재제출을 추가하지 않았다. | 견적만 요청했을 때 실제 상태 불변, Move 1개 실행·효과 중단·행동 사용·사건·Undo, 낡은 문맥 거절 관찰. |

**기존 실행 합류.** `PREVIEW`의 Move 분기는 기존 `CombatMovementCalculator.Preview`와 `MovementContextCalculator.Capture`를 사용한다. `CombatCommandService.ProcessNextCommand`는 명령의 경로·문맥을 조립하고 `Matches` 실패 시 `RejectStale`로 보낸다. 성공 시 `CombatCommandCalculator`의 기존 Move/Undo 분기와 게시 경로를 따른다. 이 호출 경계는 그대로이며, 실제 결과·난수·효과·Undo 일치 여부는 K7 실행 증거로 확인해야 한다.

**구조 판정.** SSOT는 한 BFS, 기존 통과/착지 규칙, 경로 원본에서 파생한 목적지, 기존 `QueryRevision`과 실행 문맥이다. S(검색·응답·게시 책임), O(기존 Move에 경로 제공), L(기존 드래그 승인·거절 계약), I(필요한 조회 API), D(순수 Core 타입과 기존 Sink 경계)를 diff와 대조했고 구체적 위반을 찾지 못했다. JMH/FW의 Factory → Core Builder 등록 → Core Group 공개 → StepContext/Job 소비와 수명·게시 경계는 유지된다. 새 서비스/enum/Factory/전역 캐시는 없다.

첨부 지침의 수정·이해·검증 범위도 §8과 일치한다. 변경은 검색·범위 DTO·게시 분기에 국한됐다. 공유 탐색의 호버, 통과/착지 규칙, 응답 번호 계약은 필요한 검토 범위이며 공격·스킬 계산 내부까지 변경하지 않았다. `SEARCH → CALC.CanLand`는 계획에 명시된 좁은 판정 의존이다. 할당 성능과 유지보수 개선 효과는 측정하지 않았다.

**View 제공 API와 인계.** `MovementRangeResult.Success(EntityId, IReadOnlyList<MovementPath>)`, `TryGetRoute(CellCoord, out MovementPath)`, `SourceRevision`, `WithSourceRevision(long)`을 제공한다. 실패·빈 성공도 게시되면 양수 출처가 있으며, 조회 성공만으로 실행 승인이 되지는 않는다. `MoveRange → TryGetRoute → CombatViewPreviewData.Move(actor, path) → MovePreview/MovementPreviewContext → CombatViewCommandData.Move(actor, path, context)`를 사용한다. 설계 §5의 클릭·선택·슬롯·취소 계약과 현재 `CombatMovementInteraction.Confirm`의 `_frozen` 자동 제출 주의점을 `HANDOFF` §1~§3에 정리했다. 이 View 동작은 아직 구현하지 않았다.

**실행 증거와 종료 지점.** 이번 Phase의 Unity 컴파일/재생/EditMode 실행은 0회이며, 사용자 실행 결과도 전달받지 않았다. §7.2의 기존 8개 시험 클래스 결과와 §7.1 K1~K7 신규 계약 관찰은 모두 대기다. 이전의 다른 기능 시험 결과를 이번 변경 증거로 사용하지 않았다. `git diff --check`로 공백 오류가 없음을 확인했다. 4.3~4.5는 문서로 완료하고, 4.1~4.2의 실행 확인은 미완료로 유지한다. Core 검증 완료나 클릭 UI 완성을 선언하지 않고 이 Phase의 리뷰 지점에서 멈춘다. 커밋·push·외부 전달 없음.

### 9.5 플랜 전용 View 인계 분리 — 2026-11-17

사용자 요청으로 9월 24일 통합 인계의 이번 기능 상세 내용을 전용 `HANDOFF`로 이전했다. 통합 문서는 기존 Phase 절과 §74에 이전 링크만 남겼으며, 이 계획과 설계의 인계 참조를 전용 문서로 갱신했다. 이후 이 기능의 인계 갱신은 전용 문서에서 진행한다. 코드·설계 정책·검증 상태 변경 없음.

### 9.6 코드 리뷰 후속 — 2026-11-17

Claude 코드 리뷰([리뷰 문서](../specs/2026-11-17-shortest-move-meteor-chain-code-review.md))에서 결함은 없었다. 중복 규칙 하나를 고쳤다(R6): "목적지 = 원점이 아닌 착지 가능 칸, Y→X 순서"를 `MovementRangeSearch.Destinations` 한 곳에서 만들고 `Range`·`UnitMoveCells`가 그것을 읽는다. 스왑 종점의 `CanLand` 이중 호출이 없어졌고 `Distances`는 private이 됐다. 출력 순서·내용과 경로 동률 규칙은 같다. 신규 계약 K1~K7의 Core 시험이 없다는 점은 리뷰 F7·Q3으로 남겼다. **컴파일·시험 미실행.**

사용자 결정("둘다 권장대로", 2026-11-17)으로 리뷰 Q3를 반영해 신규 계약 시험을 추가했다. `MovementRangeSearchTests`(Core)는 동률 순서, 대각·우회 걸음 수, 한도 경계, 경로 불변식, `UnitMoveCells`와 행동 범위의 목적지 일치, `Success`·`SourceRevision`·`default`를 확인한다. `CoreIntegration.MoveRangePublicationTests`는 계산 전 리비전 부착, 조회가 판을 바꾸지 않음, 경로 하나가 기존 Move 한 번으로 실행됨을 확인한다. 이것으로 §7의 K1~K7 대부분을 시험이 맡게 된다. 다만 도약·자리 교환 종점(K2)과 숨은 지뢰(K3)는 이번 시험에 넣지 않았다. **컴파일·시험 미실행.** 기록은 [리뷰 §7.4](../specs/2026-11-17-shortest-move-meteor-chain-code-review.md).
