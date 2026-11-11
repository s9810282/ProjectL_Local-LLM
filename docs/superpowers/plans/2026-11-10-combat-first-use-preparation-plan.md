# 전투 첫 동작 Core 준비 — Task 계획 (2026-11-10)

> **상태:** **계획 확정(2026-11-10 사용자 "문제 없는 지 체크하고 확정")**, 코드 착수 전. 7기준 점검으로 보완한 내용은 §7이고, FU-P5·FU-P7을 고치고 FU-P12~FU-P16을 더했다. 사용자 결정(2026-11-10): "우리측에서 작업하는 걸로 하고 Job에 거는 시점까지 계획으로 잡아". 작업 문서 D7 = 이 작업에서 Core까지 고친다(Core 직접 수정은 이 지시로만 허용, Core 전달 문서 §5에 기록).
> 원본: [작업 문서](../handoffs/2026-11-10-combat-first-use-preparation-core-work.md)(측정·약속·D1~D7), [성능 계획](2026-11-10-combat-first-use-performance-plan.md) Q-FP1·P5.
> 규칙: 테스트 코드 없음(Assets/Tests는 사용자 의도로 지워짐), 전투 View 범위라 MSBuild 없음, **컴파일·재생은 사용자 확인**. Phase마다 멈추고 보고한다. 커밋·push는 사용자.

## 1. 목표와 범위

- 목표: 첫 배치·공격·이동·적 턴에서 나는 Core JIT(측정 합계 약 436ms)를, Combat 씬 로딩 화면이 가린 동안 미리 끝낸다.
- 범위: Core 준비 서비스(Core) + 전투 골격의 로딩 준비·Job(우리) + 연결 줄(Factory·ServiceKeys·JobType·Core 그룹 멤버).
- 범위 밖: View 쪽 첫 실행(약 50ms, 성능 계획 P3 View), 게시 경로 JIT(첫 배치 `Core.Combat.Publish` 38ms, D3), 에디터 전용 재질 로딩, IL2CPP 전환.

## 2. Job을 거는 시점 (흐름)

```text
노드 선택 / 개발 직행
 └ JobType.Entry → [CombatPrepareJob: Core 전투 준비 → Buffer에 전투가 생김] → [CombatEntryTransitionJob: Change(Combat)]
LodingSystem.Load(Combat)
 ├ BeforeLoad → 앞 씬 SceneExitLoaded → 비동기 씬 로드
 ├ Combat SceneEnterLoaded (순서 원본 SceneNodeOrder)
 │    −195 배경 · −190 카메라 · 0 CombatLifeCycle 활성화 · 100 View 연결 …
 ├ AfterLoad
 ├ 씬 준비 (로딩 화면이 가린 채, 등록 순서대로 하나씩)     ← 여기
 │    ① 선택 유닛 준비(기존 View, CombatUnitPreparation)  — InGame 그룹이 먼저 초기화되어 먼저 등록됨(FU-0)
 │    ② 첫 사용 준비(새, 우리)  Begin → Register(Combat, JobType.FirstUsePrepare)
 │         Runner: 매 Tick CombatFirstUsePrepareJob.Execute → core.CombatFirstUse.PrepareNext() 한 번
 │         false가 오면 Job 완료 → 준비 Complete
 └ Reveal → 전투 화면
```

- 이 기록에서 씬 로드는 140·143(504ms·892ms), 선택 유닛 준비는 144~148이었다. 첫 사용 준비는 그 근처에 붙는다.
- 준비 Job이 Runner의 현재 Job인 동안 Runner는 비지 않는다. 그래서 `CombatLifeCycle.OnTick`이 `Next`를 걸지 않고, 그동안 Plan 판단은 멈춘다. 튜토리얼도 Runner가 빌 때만 `Next`를 걸므로(`TutorialViewSystem.cs:216`) 그동안 미뤄진다. 로딩 중이라 둘 다 할 일이 없고, 준비가 끝나면 다음 Tick부터 다시 건다.
- Runner는 끝나지 않은 Job을 다음 Tick에도 현재 Job으로 둔다(`JobRunnerSystem.Tick`). 준비 Job 하나가 Tick마다 단계 하나씩 진행한다.
- 두 번째 전투부터는 `PrepareNext()`가 바로 false라, 준비는 한 Tick 안에 끝난다.

## 3. 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 근거·대안 |
|---|---|---|
| FU-P1 | 입구는 `bool PrepareNext()` 하나(D1) | 단계 목록과 순서는 Core 안에 둔다. 대안: 단계 열거형을 받는 `Prepare(stage)` |
| FU-P2 | 가상 상태는 Core에 이미 있는 `CombatCommandComposition.Project(transition)`·`Intent(...)`로 잇는다(D2) | 자동 명령이 중간 결과를 Buffer에 게시하지 않고 다음 입력을 만들 때 쓰는 입구다. 새 내부 입구가 필요 없다. |
| FU-P3 | 게시 경로(38ms)는 그대로 둔다(D3) | 첫 배치에서만 남는다. 버리는 Buffer에 적용하는 안은 범위가 커진다. |
| FU-P4 | 준비 중 예외는 오류 경계(F1)로 보낸다(D4) | 프로젝트 규칙(숨기지 않음)이다. 대안: 경고 뒤 건너뜀(최적화라 게임은 계속). **사용자 결정 권장 항목.** |
| FU-P5 | **런타임(서비스 인스턴스) 수명당 한 번**, 첫 전투의 유닛·특성만 덮는다(D5). 끝났다는 표시는 서비스의 **인스턴스 필드**에 둔다(정적 필드 아님) | 작업 문서 권장(새 유닛 종류는 전투마다 추가)보다 단순하다. 다음 전투에서 처음 나온 유닛은 약 25ms가 다시 난다. 측정 뒤 늘릴지 정한다. **작업 문서와 다른 점.** 정적 표시는 Core에 첫 정적 가변 상태를 만들어서 피한다(§7 L1). Play마다 런타임을 새로 만들면 다시 돌지만, JIT가 이미 끝났으면 계산만 하므로 짧다. |
| FU-P6 | 노출은 `IInGameCoreSystemGroupService.CombatFirstUse` 새 멤버(D6) | `CombatCommands`·`CombatPreview`와 같은 꼴(서비스 키 + 그룹 해석 줄) |
| FU-P7 | 준비는 진입 서비스(`CombatEntryService`)가 만들고, **현재 진입 요청(`CombatEntryRequest`)의 internal 멤버**로 붙인다. Plan은 `RequireEntry(lifeCycles).Current`에서 읽는다 | 진입 수명(수락 → 활성화 → 로딩 중 준비)의 일부다. 공개 계약 `ICombatEntryService`는 Stage(`StageLifeCycle`·`StageProgress`)·Bootstrap도 쓰므로 넓히지 않는다(§7 M1). 그룹·서비스 키도 바꾸지 않는다. `CombatLifeCycle`은 트리거 역할 그대로 둔다. |
| FU-P8 | Job은 Tick마다 한 단계씩 도는 **여러 Tick Job** 하나 | Runner 계약이 허용한다. 진입·전환 Job은 한 번에 끝나지만, `CombatNextJob` 안 단계 Job의 "한 번에 끝남" 규칙은 이 별도 등록 Job에 해당하지 않는다. 대안: 준비가 Tick마다 Job을 새로 등록. 형식은 기존 Job들처럼 `internal struct`로 두고, 진행 상태는 준비 객체와 Core 서비스(클래스)에 둔다(FU-P15). |
| FU-P9 | 가림이 없는 진입(로딩 화면 없음, `Begin(isCovered: false)`)에서도 준비를 돈다 | 개발 직행에서 끊김이 보일 수 있다. 대안: 가림이 없으면 건너뜀 |
| FU-P10 | 두 준비의 순서는 등록 순서를 따른다. 실제로는 **선택 유닛 준비 → 첫 사용 준비**다(FU-0 확인) | 어느 쪽이 먼저여도 결과가 같다. 그룹 등록·초기화 순서는 Buffer → Global → InGame → LifeCycle이다(`RunTimeSystemFactory.cs:41-44`). |
| FU-P11 | 비교용 끄기 스위치는 만들지 않는다 | 기준 기록(`20261110-212514-765-4380a2dc`)과 P1 뒤 새 기록을 기준선으로 삼는다. |
| FU-P12 | 단계 계산은 Core Application 어셈블리의 `public sealed class CombatFirstUseRehearsal` 하나로 낸다. public 표면은 생성자(시작 입력·이동 규칙)와 `bool RunNext()`뿐이다. 연쇄 상태는 private이고 `CombatCommandComposition`은 internal 그대로 쓴다 | Application 어셈블리는 Systems 어셈블리에 internal을 열지 않는다(`InternalsVisibleTo`는 시험 어셈블리만). 그래서 서비스가 부를 입구는 public이어야 한다. 표면은 최소로 둔다(§7 M2). |
| FU-P13 | 계측은 층마다 기존 방식을 쓴다. Application(리허설)은 `CoreProfiling.Sample(CoreProfileSection.FirstUseRehearsal)`(열거 값 하나), Systems(서비스)는 `CoreProfilerMarkers.FirstUse` 하나 | 새 계측 방식을 만들지 않는다(§7 L2). 측정기(`CoreProfilerMarkers.UnityCoreProfiler`)가 열거 값 이름으로 `Core.Calc.<값>` 마커를 자동으로 만들므로, 이름을 적는 줄은 필요 없다(FU-0 확인). |
| FU-P14 | 서비스는 리허설을 시작한 전투 번호(`BattleInstanceId`)를 기억한다. 다른 전투에서 부르면 이전 리허설을 버리고 처음부터 한다. 끝까지 마친 뒤에만 "끝남"을 세운다 | 로딩 중 취소 뒤 다음 전투가 옛 전투 입력으로 이어 계산하지 않게 한다(§7 L3). |
| FU-P15 | Job struct의 가변 값은 `IsCompleted` 하나다(기존 Job과 같음). 단계 진행은 Core 서비스, 취소·완료는 준비 객체(클래스)가 가진다 | 여러 Tick 동안의 상태를 struct 박싱에 기대지 않는다(§7 L4). |
| FU-P16 | 현재 진입 요청이 없거나 활성이 아닌데 Combat 씬 준비가 만들어지면 예외를 낸다 | View의 `GetScenePreparation`과 같은 꼴이다. Combat 씬은 진입 전환으로만 열린다(§7 L7). |

## 4. Core 준비가 도는 단계 (Core 쪽 상세)

각 단계는 `PrepareNext()` 한 번에 하나씩 돈다. 모두 ① `CombatInputAssembler.Assemble`(Buffer 읽기)이나 앞 단계 결과의 `Project`로 입력을 얻고, ② 정적 계산기를 부르고, ③ 결과를 버린다. 게시(`CombatTransitionPublisher`·`PublishPreview`)는 하지 않는다.

| 단계 | 하는 일 | 덮는 측정 경로 |
|---:|---|---|
| S1 | 메인 배치: 배치 미리보기(`CombatPlacementCalculator.Preview`) → 배치 명령 `Calculate(PlaceMainCharacter)` → `Project` | 1404(Calculate·CommandFinalization·MovementUndoReconcile·CommandCompletion·TraitPresenceReconcile·ProjectionCurrentInput) |
| S2 | 서브 배치: 손패 첫 유닛 `Calculate(PlaceUnit)` → `Project` | 1778(Core 쪽 작음) |
| S3 | 전투 시작: `Calculate(EnterCombat)` → `Project`, 국면이 자동이면 `CombatCommandCalculator.Advance`를 플레이어 입력까지 → `Project` | 4223(적 턴 Calculate 26ms) |
| S4 | 아군 하나로 행동 가능 여부·이동 범위·유닛 공격 범위·공격 범위 조회 | 1780·2512 |
| S5 | 같은 아군으로 이동 미리보기(`CombatMovementCalculator.Preview`·`MovementContextCalculator.Capture`) → 이동 명령 `Calculate(Move)` → `Project` | 2816·2832 |
| S6 | 공격 범위의 첫 조준으로 공격 미리보기(`CombatCommandCalculator.Preview`) → 공격 명령 `Calculate(Attack)` | 2552·2630 |
| S7 | 아군 공격 종류마다(이번 전투 로드아웃) 공격 범위 → 공격 미리보기 | 3985 |
| S8 | 턴 종료 `Calculate(EndPlayerTurn)` → `Advance` 한 번 이상 | 4223·5376(넉백 경로는 적·아군 행동에서 나면 덮임) |

- **칸·대상 고르기:** 배치 칸은 배치 구역, 이동 칸은 `Range`, 조준은 `GetAttackRange`의 결과에서 첫 값을 쓴다. 모두 Core 자신의 조회라 규칙을 베끼지 않는다.
- **거절:** 계산이 거절(`Accepted == false`)이면 그 경로의 코드는 이미 지났다. 이어 갈 상태가 없으면 남은 의존 단계를 건너뛴다. 거절은 예외가 아니다.
- **단계당 길이:** 첫 공격 미리보기처럼 한 단계가 100ms를 넘을 수 있다. 가린 화면 뒤라 받아들이고, 로딩 애니메이션이 눈에 띄게 멈추면 단계를 더 쪼갠다.
- **계측:** FU-P13대로 서비스는 `CoreProfilerMarkers.FirstUse`, 리허설은 `CoreProfiling.Sample(CoreProfileSection.FirstUseRehearsal)` 하나를 쓴다. 단계별 길이는 Profiler에서 이 구간을 Tick별로 보면 된다(단계 하나 = `PrepareNext` 한 번 = 한 Tick).
- **기대 식별자:** 연쇄 입력의 기대 값(전투 번호, 컨트롤러 턴 순번, 이동 명령 번호)은 앞 단계 결과 상태에 맞춘다. 실제 Buffer 번호와 비교하는 검사는 서비스 쪽(`TryGetNextCommand` 등)이라 리허설은 지나지 않는다.
- **확인한 전제:** Core 계산기는 입력만으로 계산한다. 난수는 입력에 실린 상태나 시드로 만든 지역 `Random`이다(`EnemySpawnCalculator`·`InitialDrawCalculator`). `CoreCaculator/Application`·`Domain`·`CombatSystem`에 readonly가 아닌 정적 필드나 정적 컬렉션 캐시가 없다(2026-11-10 grep). 그래서 결과를 버리면 남는 상태가 없다고 본다. 착수 때 단계마다 다시 확인한다.

## 5. Phase와 Task

### FU-0 착수 전 확인 (코드 변경 없음)

1. 성능 계획 P1(마커 지연 생성)의 Unity 컴파일과, 같은 조건의 새 기록을 사용자에게 받는다. 이것이 이 작업의 기준선이다(P1 효과와 섞이지 않게).
2. 고칠 파일의 최근 변경을 확인한다. `CombatSystemFactory.cs`(천명님 11-08), `InGameCoreSystemGroup.cs`, `InGameCoreSystemServiceKey.cs`, `JobRootType.cs`, `CombatEntryService.cs`·`CombatEntryRequest.cs`, `CombatPlan.cs`에 다른 작업이 섞여 있는지 본다(git status·blame).
3. 초기화 순서를 확인한다(FU-P10: LifeCycle 그룹과 InGame 그룹 중 누가 먼저 `RegisterScenePreparation`을 부르는지).

### FU-a Core 준비 서비스 (Core, 화면 변화 없음)

| Task | 파일 | 내용 |
|---|---|---|
| a1 | 새 `RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/ICombatFirstUsePreparation.cs` | 계약 `bool PrepareNext()`(§2.2 작업 문서) |
| a2 | 새 `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatFirstUseRehearsal.cs` | 순수 단계 계산 S1~S8. Buffer 없음, 결과는 버림. public 표면은 생성자와 `RunNext()`뿐(FU-P12) |
| a3 | 새 `RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatFirstUsePreparationService.cs` | Buffer 바인딩·조립기·이동 규칙을 받는다. 첫 호출에서 시작 입력을 조립해 리허설을 만들고, 단계를 진행하며, 전투 번호(FU-P14)와 끝남 인스턴스 필드(FU-P5)를 관리한다. 게시 협력자는 받지 않는다(구조상 게시 불가). |
| a4 | `CombatSystem/CoreProfilerMarkers.cs`(`FirstUse` 마커), `CoreCaculator/Application/Diagnostics/CoreProfiling.cs`(`CoreProfileSection.FirstUseRehearsal` 열거 값) | 계측(FU-P13) |
| a5 | `Factory/Runtime/InGame/Core/CombatSystemFactory.cs` | `new CombatFirstUsePreparationService(assembler, movementRules).ExportServices(sink);` 한 줄 |
| a6 | `ServiceKeys/Runtime/InGame/Core/InGameCoreSystemServiceKey.cs` | `CombatFirstUse` 키 |
| a7 | `RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs` | 그룹 계약 멤버와 해석 줄 하나씩 |

- 새 `.cs` 셋의 `.meta`는 Unity가 만든다. 커밋 때 함께 넣는다.
- 확인(사용자): Unity 컴파일, 아무 전투 재생에서 전과 같은지(아직 아무도 부르지 않음). 끝나면 멈추고 보고한다.

### FU-b 골격 연결 — Job을 거는 시점 (우리)

| Task | 파일 | 내용 |
|---|---|---|
| b1 | `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `FirstUsePrepare = 10`(전투 전용 값, 주석 한 줄) |
| b2 | 새 `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatFirstUseLoadingPreparation.cs` | `ILodingPreparation`. Begin → 진입 서비스의 등록기로 `Register(Combat, FirstUsePrepare)`. `Tick`은 계약상 비어 있다(완료는 Job이 알림). Job이 완료를 알리면 `Complete`, 실패면 `Failed`, `Cancel`이면 취소 표시(internal 메서드). 씬 진입마다 새 인스턴스 |
| b3 | 새 `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatFirstUsePrepareJob.cs` | `internal struct`(FU-P15). Execute마다 준비가 취소됐으면 끝낸다. 아니면 `PrepareNext()`를 한 번 부르고, false면 준비에 완료를 알리고 `IsCompleted = true`. 예외는 준비를 실패로 바꾼 뒤 다시 던진다(F1, FU-P4. `CombatPrepareJob`의 `Fail(); throw;`와 같은 꼴) |
| b4 | `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs` | `Register`의 `case JobType.FirstUsePrepare` → `RequireEntry(lifeCycles).Current`의 준비와 `service.CoreGroup.CombatFirstUse`로 Job 하나. 준비가 없거나 시작 전이면 예외(진입·착지 전환과 같은 꼴) |
| b5 | `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatEntryService.cs`, `CombatEntryRequest.cs`(`CombatEntryRequest` 클래스만) | `PostBindService`에서 `LodingRegisterService`를 받고, `Initialize`에서 `RegisterScenePreparation(SceneIndices.Combat, new LodingPreparationNode(CreateFirstUsePreparation))`. 만든 준비는 현재 활성 요청의 internal 멤버로 붙인다(FU-P7). 요청이 없으면 예외(FU-P16). **`ICombatEntryService` 공개 계약은 바꾸지 않는다.** `Shutdown`에서 정리 |

- `JobRegisterSystem.cs:66`(전투 Plan 등록 줄)은 바꾸지 않는다. 새 값은 같은 `CombatPlan.Register`로 간다.
- 확인(사용자):
  - Unity 컴파일
  - 직행·맵 진입 둘 다 로딩 뒤 전투가 정상으로 열리는지
  - 두 번째 전투 진입이 바로 열리는지
  - 로딩 중 Play 종료나 씬 바뀜에서 예외가 없는지
- 끝나면 멈추고 보고한다.

### FU-c 측정·문서 (사용자 측정 후)

1. 같은 시작 조건으로 기록한다. 비교할 것: 첫 배치·첫 공격 미리보기·첫 공격·첫 이동·첫 적 턴의 Core 안 JIT, 로딩 준비 프레임 길이, 두 번째 전투 로딩.
2. 같은 행동의 결과(배치 가능 칸, 피해, 적 행동)가 준비가 없을 때와 같은지 눈으로 확인한다.
3. 기록:
   - Core 전달 문서 §5 새 항목(Core 변경, 제목 갱신)
   - 천명님 공유(Core 파일 여섯: 새 셋·기존 셋, 계산기 입구를 바꾸면 리허설도 맞춘다는 안내 포함)
   - 민형님 Factory 공유(Factory 한 줄·서비스 키·`JobType` 값·그룹 멤버)
   - 성능 계획 P5 결과
   - 평가 아티팩트 '적용 기록' 차수
4. 묶음 끝 검토를 한 번 한다(7기준, 검토 에이전트 하나).

## 6. 위험

| 위험 | 대응 |
|---|---|
| 가상 연쇄가 중간에 거절돼 뒤 단계를 못 덮음 | 거절은 예외가 아니므로 남은 의존 단계를 건너뛴다. 측정에서 덮은 양으로 본다. |
| 준비 코드의 예외가 게임을 멈춤(FU-P4) | 예외 위치를 F1 보고로 바로 알 수 있다. 사용자가 "경고 뒤 건너뜀"을 고르면 바꾼다. |
| 로딩 애니메이션이 단계마다 멈춰 보임 | 단계를 더 쪼갠다(Core 안에서만 바뀜). |
| 실제 결과가 달라짐(정적 상태) | §4 전제(정적 가변 상태 없음)를 착수 때 다시 확인하고, FU-c 2에서 눈으로 대조한다. |
| 다른 작업자가 같은 Core 파일을 고치는 중 | FU-0 2에서 확인한다. 겹치면 멈추고 묻는다. |
| 전투 Plan이 준비 동안 멈춤 | 로딩 중에는 할 일이 없다. Reveal 뒤 첫 Tick부터 다시 돈다(Runner가 비면 `Next`). |
| Core 계산기 입구가 바뀌면 리허설도 고쳐야 함 | 리허설은 서비스가 이미 쓰는 공개 계산기와 `Project`·`Intent`만 부른다. 천명님 공유에 "계산기 입구를 바꾸면 리허설도 맞춘다"를 적는다(§7 L5). |

## 7. 7기준 점검 (2026-11-10, 확정 전)

사용자 요청: SSOT · SOLID · JMH Framework 지침·FW 계약 · 기능 변경의 독립성 · 캡슐화 · C#·Unity 권장 구조 · 무의미한 If/Tick. 코드로 직접 확인했다(진입 계약 사용처, 어셈블리 internal 공개, Job 형식, 튜토리얼 Runner 사용, Core 정적 상태, Core 계측 방식). 높음은 없다.

| 번호 | 기준 | 찾은 것 | 반영 |
|---|---|---|---|
| M1 | 캡슐화·SOLID(ISP) | 계획 b5가 공개 계약 `ICombatEntryService`에 "현재 준비" 멤버를 더함. 이 계약은 Stage(`StageLifeCycle`·`StageProgress`)·`GameBootstrap`·Factory도 쓴다 | FU-P7·b5: 준비를 현재 진입 요청(`CombatEntryRequest`, public 클래스)의 internal 멤버로 붙임. 공개 계약 무변경 |
| M2 | C#·FW(어셈블리 경계)·캡슐화 | 리허설이 있는 Core Application 어셈블리는 Systems에 internal을 열지 않는다. 계획이 리허설의 공개 범위를 정하지 않음 | FU-P12: public 클래스 하나, 표면은 생성자와 `RunNext()`뿐 |
| L1 | C#·SSOT | "프로세스당 한 번"을 정적 표시로 두면 Core에 첫 정적 가변 상태가 생긴다(지금 Core 계산·서비스에 없음) | FU-P5: 서비스 인스턴스 필드 |
| L2 | FW(있는 기능 사용) | 계획이 리허설 단계 마커를 `CoreProfilerMarkers`에 더하려 함. Application 층은 `CoreProfiling.Sample(CoreProfileSection)` 방식이다 | FU-P13: 층마다 기존 방식 |
| L3 | 기능 독립성·정확성 | 로딩 중 취소 뒤 다음 전투에서 옛 전투의 연쇄 입력으로 이어 계산할 수 있음 | FU-P14: 전투 번호가 바뀌면 처음부터, 다 마친 뒤에만 끝남 |
| L4 | C#·Unity | 여러 Tick Job을 struct로 두면 진행 상태가 박싱된 사본에 기댄다 | FU-P15: Job의 가변 값은 `IsCompleted`뿐, 진행은 클래스 쪽 |
| L5 | 기능 독립성 | 리허설이 Core 계산기 공개 입구에 묶여, 입구가 바뀌면 함께 고쳐야 함 | 받아들임. 쓰는 입구는 서비스와 같은 공개 계산기와 `Project`·`Intent`뿐이다. 천명님 공유에 적는다 |
| L6 | 무의미한 Tick·독립성 | 준비 동안 튜토리얼 `Next`도 미뤄지는지 | 확인함. `TutorialViewSystem.cs:216`이 Runner가 빌 때만 건다. 로딩 중이라 영향 없음(§2에 적음) |
| L7 | FW | 진입 요청 없이 준비가 만들어질 때의 처리가 비어 있음 | FU-P16: 예외(View 준비와 같은 꼴) |

나머지는 문제 없다.

- **SSOT:** 단계 목록·끝남 표시는 Core 서비스·리허설 한 곳이다. 칸·조준은 Core 자신의 조회로 고른다. JobType 값은 하나다.
- **SOLID:** 역할이 나뉘어 있다. 서비스는 바인딩·진행, 리허설은 순수 계산, 준비는 로딩 계약, Job은 Core 호출 하나, Plan은 조립이다. Plan 확장은 기존 `switch` 갈래 하나다.
- **FW:** Core 호출은 Job만 한다. 서비스는 `ExportServices`·`PostBindService` 꼴이다. 로딩 준비는 기존 `RegisterScenePreparation` 계약이다. 연결 줄은 영역 규칙 범위 안이다.
- **If/Tick:** 준비의 `Tick`은 계약상 빈 메서드다. Job은 단계마다 한 번 부르고, 두 번째 전투에서는 첫 Execute에서 끝난다. 매 Tick 반복 검사는 취소 확인 하나다.

| 기준 | 계획 그대로 | 반영 뒤(예상) |
|---|---:|---:|
| SSOT | 9.0 | 9.5 |
| SOLID | 8.0 | 9.5 |
| FW 계약 | 8.5 | 9.5 |
| 기능 독립성 | 8.5 | 9.0 |
| 캡슐화 | 7.5 | 9.5 |
| C#·Unity 권장 | 8.0 | 9.5 |
| 무의미한 If/Tick | 9.0 | 9.5 |
| **평균** | **8.4** | **9.4** |

예상은 알려진 문제 기준이다. 구현 뒤 묶음 끝 검토(FU-c 4)에서 다시 잰다. 기능 독립성은 L5를 받아들였으므로 9.0에 머문다.

## 8. 실행 기록

### FU-0 착수 전 확인 (2026-11-10, 코드 변경 없음)

1. **P1 컴파일·기준 기록**
   - **컴파일:** 성공. 이전 에디터 세션 로그(`Editor-prev.log`)에 `error CS`가 없고, P1 변경(21:34) 뒤 빌드가 `Tundra build success`였다. 23:06에 새로 연 세션도 성공했다. Editor.log로 확인했으며, Unity를 직접 조작하지 않았다.
   - **새 기록:** `Logs/ProfilerCapture/20261110-224015-c32cd37340e143299720bda1aa4f67ea`(22:40~22:48, 47,466프레임, 원본 7.4GB, Play 정상 종료). P1이 들어간 상태에서 8분간 문제없이 재생됐다.
   - **변환 실패:** 이 기록을 CSV로 바꾸던 중 에디터가 크래시했다(`ProfilerContinuousCapture.ExportPending` → `ProfilerCaptureImport.Export` → `ProfilerCaptureHistory.LoadFile` → Unity 내부 `ProfilerDriver.LoadProfile`, "Got a UNKNOWN while executing native code"). 그 직전 에디터 메모리는 약 6GB였다(물리 메모리 16GB). 그래서 익스포트가 없고, **새 기준 기록은 아직 없다.**
   - 원본은 지우지 않았다. 기준 기록은 짧게(약 1~2분, 첫 배치·공격 미리보기·공격·이동·턴 종료 포함) 다시 받아야 한다. 이전 기준은 70초·원본 1.1GB였고 변환에 성공했다.
   - 큰 원본을 변환할 때의 크래시는 기록 도구(연속 기록 계획 C2) 쪽 문제이며 이 계획의 범위 밖이다.
2. **고칠 파일의 변경**
   - 대상 9개(`CoreProfilerMarkers`·`CoreProfiling`·`InGameCoreSystemGroup`·`CombatSystemFactory`·`InGameCoreSystemServiceKey`·`JobRootType`·`CombatPlan`·`CombatEntryService`·`CombatEntryRequest`)에는 로컬 커밋 전 변경이 없다.
   - `origin/develop`은 로컬보다 25커밋 앞서지만 대상 파일을 건드리지 않는다.
   - **같은 브랜치 원격(`origin/feature/Yonggeun/Refector-Stage`)이 로컬보다 8커밋 앞선다.**
     - 들어 있는 것: 사용자 커밋 `dbca14910`·`189248bfb` "[용근] 전투 View, 신규 특성 및 상태 SO 추가", 천명님 `0e2a5ee06` 적 전용 특성 재구성·자폭·공포 Core 구현, 병합 셋, 파일 101개.
     - 대상 9개와 Core 계산기·조립기 입구(`CombatCommandCalculator`·`CombatCommandComposition`·`CombatInputAssembler`·`CombatCommandInput`)는 바뀌지 않았다.
   - 받을 때 겹치는 곳 둘:
     - Core 전달 문서: 원격이 제목을 바꾸고 **§67을 다른 주제(적 전용 특성 View 인계)로 썼다.** 로컬 §67(첫 동작 조사 요청 + 이번 작업 문서·계획 줄)과 번호가 겹친다. 받은 뒤 로컬 절을 새 번호로 옮긴다.
     - `CombatActionPanel.cs`: 로컬 커밋 전 변경과 원격 변경이 겹친다(이 작업과 무관).
   - 원격의 새 Core 특성(재구성·자폭·공포)도 리허설이 지나갈 경로다. 그래서 **FU-a는 원격을 받은 뒤 그 위에서 한다.**
3. **등록 순서·계측**
   - 그룹 등록·초기화 순서는 Buffer → Global → InGame → LifeCycle이다(`RunTimeSystemFactory.cs:41-44`). 그래서 씬 준비는 View 선택 유닛 준비 → 첫 사용 준비 순서다. FU-P10과 §2를 고쳤다.
   - Core 측정기가 열거 이름으로 마커를 자동으로 만든다(`CoreProfilerMarkers.cs:58-60`). FU-P13과 a4에서 "이름 한 줄"을 뺐다.

**FU-a 착수 조건:**

1. ~~사용자가 원격 8커밋을 받는다.~~ → 받음(아래 "원격 8커밋 받기").
2. 짧은 새 기준 기록. FU-a 코드 작성과는 병행할 수 있지만, FU-c 비교 전에는 있어야 한다.

### 원격 8커밋 받기 (2026-11-10 사용자 "받아서 정리해 줘")

1. **백업:** 받기 전에 스크래치패드 `pull-backup/`에 저장했다.
   - 스테이지 패치·스테이지 안 된 패치·상태 목록(113줄)·HEAD(`90fad7eb7`)
   - 겹치는 두 파일의 로컬 사본
2. **겹치는 두 파일만 빼 두기:** `git stash push -- <두 파일>`로 두 파일만 넣었다. 나머지 로컬 변경 111줄은 그대로였다. stash 이름은 "pre-ff 189248bfb: overlap files"이고, 백업으로 남겨 두었다.
3. **빨리 감기:** `git merge --ff-only origin/feature/Yonggeun/Refector-Stage`. HEAD가 `189248bfb`가 되어 원격과 같아졌다. 커밋은 만들지 않았다.
4. **되돌리기:**
   - `CombatActionPanel.cs`: 스테이지 패치를 `git apply --index`로 다시 넣었다. 원래처럼 스테이지된 상태다. 로컬 계측 마커와 원격 호진님 `EndTurnButtonRect`는 다른 곳이라 둘 다 들어 있다.
   - Core 전달 문서: 원격판을 기준으로 했다.
     - 제목은 최신 주제(첫 동작 Core 준비 계획 확정)로 바꾸고, 이전 주제에 원격 주제를 적었다.
     - 로컬 §67(첫 동작 조사·작업 문서·계획 줄)은 원격 §67(천명님 적 전용 특성) 뒤에 **§68**로 붙였다. 옮긴 이유 한 줄을 더했다.
     - "측정기 설치 자리 이름 한 줄"은 FU-0에서 확인한 대로 고쳤다.
     - 원래처럼 스테이지 안 된 상태다.
5. **참조 고침:** 첫 동작 절을 "§67"로 가리키던 곳을 §68로 바꿨다(작업 문서 2곳, 성능 계획 3곳, 프로파일러 세션 인계 1곳). 위 FU-0 2의 §67 서술은 받기 전의 기록이라 그대로 두었다.
6. **받은 것 중 이 계획에 영향:**
   - 천명님 적 전용 특성(재구성·자폭·공포, Core 전달 문서 §67)이 들어왔다. 리허설이 지나갈 Core 경로가 늘었다.
   - 원격 §67에 따르면, View가 상태 `Reconstructing`(19) 표시 변환을 등록하기 전에 그 특성이 붙은 적이 HUD에 나오면 예외가 난다(`CombatEffectFormatters`). 운영 적 유닛에는 아직 붙지 않았다.
   - 리허설은 이번 전투의 실제 유닛으로 경로를 고르므로, 그 특성이 붙은 적이 없으면 이 경로를 지나지 않는다.
   - 원격 커밋으로 `Assets/Tests/EditMode/`에 시험 파일이 다시 생겼다(천명님 Core 시험). 이 작업은 시험을 쓰지 않는다는 규칙 그대로다.
   - (같은 날 확인) 위 `Reconstructing` HUD 표시 변환은 받은 사용자 커밋이 이미 넣었다(민형님 Factory 공유 §53).

### FU-a·FU-b 코드 (2026-11-10 사용자 "쭉 진행해")

**컴파일:** Unity 컴파일 성공. 마지막 소스 수정(23:54:17) 뒤 Unity가 스스로 다시 가져와 `ProjectL.Core.Application.dll`(23:54:25)·`ProjectL.Systems.dll`(23:54:31)을 빌드했고(`Tundra build success`), Editor.log에 `error CS`가 없다. Unity를 직접 조작하지 않았다. 재생과 측정은 하지 않았다. 새 시험 없음, 시험 변경 없음, MSBuild 없음.

**FU-a(Core):**

- 새 `CombatSystem/ICombatFirstUsePreparation.cs`·`CombatSystem/CombatFirstUsePreparationService.cs`·`CoreCaculator/Application/Combat/CombatFirstUseRehearsal.cs`(+ Unity가 만든 `.meta`)
- `CoreProfilerMarkers`(`FirstUse`), `CoreProfiling`(`FirstUseRehearsal`), `InGameCoreSystemGroup`(멤버·해석 줄)
- 연결: `CombatSystemFactory` 한 줄, `InGameCoreSystemServiceKey.CombatFirstUse`

**FU-b(골격):**

- `JobRootType.cs`(`FirstUsePrepare = 10`)
- 새 `LifeCycles/Combat/CombatFirstUseLoadingPreparation.cs`·`Jobs/CombatFirstUsePrepareJob.cs`(+ `.meta`)
- `CombatPlan`(갈래 `FirstUsePrepare`·설명 한 줄)
- `CombatEntryService`(로딩 등록기 받기, `Initialize`에서 씬 준비 등록, 준비 만들기·Job 요청)
- `CombatEntryRequest`(internal `FirstUsePreparation`·`AttachFirstUsePreparation`). `ICombatEntryService`는 그대로다.

**계획과 다른 점:**

1. **리허설 생성자에 시작 입력 함수(seed)를 더했다**(FU-P12의 "생성자 = 시작 입력·이동 규칙"과 다름).
   - 이유: 가상으로 턴이 넘어가면 다음 행동 입력의 기대 턴 순번·처리 종류가 맞아야 한다. Core가 `CombatActionAdmission.cs:33`·`CombatTurnCalculator.cs:83`에서 검사하고, `CombatAdvance`는 `ProcessingKind.Advance`만 받는다.
   - 그런데 `CombatCommandInput`에는 기대 값을 바꾸는 입구가 없다. 또 Application 어셈블리는 Systems의 `CombatActionRequest`를 모른다.
   - 그래서 서비스가 "처리 종류·기대 턴 순번 → 새로 조립한 입력" 함수를 넘긴다. 리허설은 가상 세계를 공개 입구 `WithWorld`·`WithPersistentFacts`로 그 위에 옮긴다.
   - Core 계약 파일(`CombatCommandInput`)은 고치지 않았다. FU-P2의 "새 내부 입구 없음"은 지켰다.
2. **천명님 공유는 Core 전달 문서 §5.84 하나로 했다.** 별도 공유 파일을 만들면 같은 내용이 두 곳에 생겨서(SSOT)다. §5.84에 "계산기 입구를 바꾸면 리허설도 맞춘다" 안내가 있다.
3. **단계 세부:**
   - S2는 손패 서브를 최대 6까지 놓는다.
   - S7 공격 종류는 필드 아군의 유닛 정의 키별로 고른다.
   - S5 이동은 한 걸음 거리 목적지로 두 칸 경로를 만든다(이동 범위 결과에 경로가 없어서).
   - 무한 반복을 막는 상한: 배치 후보 48, 자동 진행 24, 전체 96단계.

**공유 기록(모두 미전달):**

- Core 전달 문서 §5.84(제목 갱신)와 §68 "구현" 줄
- 민형님 Factory 공유 §54

### 묶음 끝 검토와 반영 (2026-11-11)

검토 에이전트 하나가 코드만 읽었다(7기준·컴파일·예외·상태 안전).

- **결과:** 컴파일 오류, 정상 경로 예외, 상태 오염은 없었다. 중간 1·낮음 8. 7기준 평균 약 8.1(SSOT 8, SOLID 8, FW 8.5, 독립성 8, 캡슐화 7.5, C#/Unity 8.5, If/Tick 8.5).
- 확인된 것:
  - 준비를 만들 때 요청이 활성이다.
  - Job은 한 번 박싱되어 같은 객체가 다시 실행된다.
  - 기대 식별자가 Admission·턴 검사를 통과한다.
  - 운영 적 턴 경로(`usePreparedEnemyAdvance = false` → `CombatCommandCalculator.Advance`)와 같다.
  - 정적 가변 상태는 측정기 하나뿐이다.

**반영(사용자 "쭉 진행해"의 범위 안, 계획 결정은 바꾸지 않음):**

- **M1(중간) 공격이 피해 경로를 거의 데우지 않음:** 칸 조준이 사거리 안 첫 칸이라, 배치 직후에는 빈 칸인 경우가 많았다.
  - 이제 `InRangeTargetIds`가 있으면 대상이 선 칸을 조준한다. 전체 조준은 그대로 맞힌다.
  - 공격 명령은 대상이 있는 첫 아군으로 한다.
  - 첫 턴에 대상을 맞히지 못했으면, 적 턴 뒤(적이 다가온 상태) 둘째 플레이어 턴에 한 번 더 공격한다.
  - 방향 조준은 어느 방향이 맞히는지 판단하지 않고 대상 없음으로 본다(한계).
- **L5 이동 검사 경로 건너뜀:** 명령 처리와 같이 `MovementContextCalculator.Matches`가 참일 때만 이동 명령을 계산한다.
- **L6 자동 진행 상한 공유:** 턴 종료 때 `_advanceSteps`를 0으로 돌려, 자동 진행 구간마다 24를 새로 센다.
- **L7 오류 상태 무시:** `CreateFirstUsePreparation`이 서비스의 `IsActive`(준비·오류 없음·활성 요청)를 본다.
- **캡슐화 7.5 이름 혼동:** LifeCycle 준비 `CombatFirstUsePreparation` → `CombatFirstUseLoadingPreparation`(파일·`.meta` 함께 이름 바꿈, 커밋 전 새 파일). Core 계약 `ICombatFirstUsePreparation`과 헷갈리지 않게 했다.

**그대로 둔 것(이유):**

- **L2 로딩 완료가 Runner 순서에 묶임:** 지금 Reveal을 기다리는 Job이 없다. 시간 상한을 두면 "덮인 채 남음"이 "준비 건너뜀"으로 바뀔 뿐이라, 생기면 그때 본다.
- **L3 일찍 끝나도 다시 하지 않음:** 메인이 손패에 없는 진입은 정상 흐름에 없다. 다시 시도하려면 리허설 공개 표면이 늘어서 그대로 둔다.
- **L4 자동 국면 조건 사본:** `RequireAdvance`의 진영 목록과 같게 두어야, Core에 새 단계가 생겨도 예외 대신 리허설이 끝나는 쪽(안전)으로 빠진다. `CombatAdvanceService.CreateRequest`와 같은 "적이면 적 턴 완료, 아니면 진영 시작" 고르기도 같은 이유다.
- **L8 단계마다 전체 재조립·취소 뒤 리허설 보관:** 로딩 중 비용이고, 다음 전투에서 버린다.
- **L9 IL2CPP:** JIT가 없어 효과가 거의 없다. 지금 PC 빌드는 Mono다.

**반영 뒤 예상(알려진 문제 기준):** SSOT 8, SOLID 8.5, FW 8.5, 독립성 8, 캡슐화 8.5, C#/Unity 8.5, If/Tick 8.5, 평균 약 8.4다. 계획 §7의 예상 9.4보다 낮다. 검토가 계획 때 보지 못한 실제 동작(조준·시점·사본)을 짚었기 때문이다.

**남은 것:**

- ~~반영분의 Unity 컴파일 확인~~ — 2026-11-11 00:20 Unity가 다시 가져와 `ProjectL.Core.Application.dll`(00:20:09)·`ProjectL.Systems.dll`(00:20:13) 빌드 성공, `error CS` 없음(Editor.log)
- Unity 재생 확인: 직행·맵 진입 로딩, 두 번째 전투, 로딩 중 종료
- ~~짧은(약 1분) 새 기준 기록과 FU-c 비교~~ — 아래 "FU-c 비교"
- ~~평가 아티팩트 '적용 기록' 11차~~ — 2026-11-11 올림(판 11, 점수 변화 없음, 상태 `outside` = 평가 문제 목록 밖)

### FU-c 비교 (2026-11-11, 사용자 기록)

- **기록:** `Logs/ProfilerExport/20261111-002203-127-35b7ea71`(4,185프레임, 약 44초, Editor·Main Thread). 개발 진입(Combat 직행·보스 스테이지 2135·로드아웃 1), 사용자가 직접 플레이했다. 반영분 컴파일(00:20) 뒤 기록이다. 비교 기준은 `20261110-212514-765-4380a2dc`(성능 계획 "기준 기록 2차 분석")다. 두 기록은 플레이 순서가 달라 프레임 번호는 맞지 않는다. 각 동작의 첫 번째끼리 비교했다.
- **준비 비용(가림 중):** `Core.Combat.FirstUse`가 로딩 준비 단계의 20프레임(187~206)에 걸쳐 모두 671ms, 그중 JIT 510ms다. 가장 큰 프레임은 197(CPU 215.7ms)이다. 로딩 화면이 덮은 동안이라 사용자에게 끊김으로 보이지 않는다. 로딩이 그만큼 길어진다.

| 첫 동작 | 기준 CPU ms (Core JIT) | 이번 CPU ms (Core JIT) |
|---|---:|---:|
| 배치 | 182.0 (116) | 1753: 77.3 (30.5) |
| 공격 미리보기 | 160.7 (120) | 3801: 20.6 (2.1) |
| 공격 실행 | 108.3 (16.7) | 3881: 40.8 (4.2) |
| 이동 미리보기 | 75.0 (45) | 2637: 22.1 (0.2) |
| 이동 실행 | 91.1 (32) | 2738: 27.8 (1.5) |
| 적 턴 시작 | 48.5 (27) | 기록에 없음(턴을 끝내지 않음) |

- **Core 안 JIT 합계:** 준비를 뺀 Core 마커 안 JIT는 55.6ms(25프레임)다. 기준은 약 436ms(9프레임)였다.
- **남은 것:** 첫 배치의 Core JIT 30.5ms는 거의 모두 `Core.Combat.Publish` 안(29.7ms)이다. 리허설은 결과를 게시하지 않으므로(FU-P3·작업 문서 D3, 기준 38ms) 정한 대로 남긴 몫이다. 기준 38ms에서 29.7ms로 준 것은 게시 전 단계의 공용 코드가 이미 데워져서로 본다(추정). 마커 밖 JIT(첫 배치 16.8ms, `GameBootstrap.Update` 바로 아래)와 View 마커 안 JIT(합계 112.6ms, 프레임마다 6~12ms)는 View·결과 소비의 첫 실행이다. 첫 공격 실행 40.8ms의 나머지는 View JIT 9.9ms·마커 밖 JIT 7.4ms 등이다.
- **P1:** `CombatUnitMotionPlayback..cctor`·`CombatUnitReactionPresenter..cctor`가 1ms 넘는 정적 초기화 목록에 더는 없다(기준 21.85ms·12.49ms). 남은 Combat 정적 초기화는 진입 때 `CombatEventTypeTable..cctor` 2.39ms다.
- **판정:** 첫 동작의 Core JIT 끊김은 목표대로 로딩 가림 뒤로 옮겨졌다. 적 턴 시작은 다음 기록에서 확인한다.
