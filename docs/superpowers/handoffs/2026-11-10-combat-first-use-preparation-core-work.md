# 전투 첫 동작 Core 준비 — Core 작업 문서 (2026-11-10)

> **상태:** 작성만 했다. 할지 말지는 사용자가 판단한다(2026-11-10 사용자 "Core 측 작업문서 만들고 내가 할지말지 판단할게"). 코드 변경 없음, 천명님께 미전달.
> **결정(2026-11-10 사용자 "우리측에서 작업하는 걸로 하고 Job에 거는 시점까지 계획으로 잡아"):** D7 = 이 작업에서 Core까지 고친다. Task 계획은 [첫 동작 Core 준비 계획](../plans/2026-11-10-combat-first-use-preparation-plan.md)(FU-0~FU-c, 계획에서 정한 것 FU-P1~FU-P11)이다. D1~D6은 그 계획의 FU-P1~FU-P6으로 정했다. 그중 D5는 이 문서의 권장과 다르게 "프로세스당 한 번"으로 단순하게 잡았다.
> 근거: `Logs/ProfilerExport/20261110-212514-765-4380a2dc`(Editor·Main Thread 기록), [성능 계획](../plans/2026-11-10-combat-first-use-performance-plan.md)의 "기준 기록 2차 분석"·Q-FP1 절, [Core 전달 문서 §68](2026-09-24-view-core-data-boundary-handoff.md).

## 1. 왜 필요한가

첫 배치·공격·이동이 느린 가장 큰 원인은 Core 코드가 처음 실행될 때의 JIT(Mono가 메서드를 처음 부를 때 기계어로 바꾸는 비용)다. 두 번째부터는 대부분 20ms 아래다.

| 프레임 | 동작 | Core 안 JIT(ms) | JIT가 많이 난 Core 단계(ms) |
|---:|---|---:|---|
| 1404 | 첫 배치 명령 | 116.1 | Publish 38.0 · Calculate 22.0 · CommandFinalization 20.8 · MovementUndoReconcile 18.9 · CommandCompletion 7.4 · TraitPresenceReconcile 5.1 · ProjectionCurrentInput 3.4 |
| 1780 | 첫 유닛 공격 범위 미리보기 | 26.7 | Preview.UnitAttackRange 23.8 · AttackEffectBinding 2.9 |
| 2552 | 첫 공격 미리보기 | 119.6 | Preview.Attack 85.1 · AttackResultConstruction 23.4 · AttackCommonDamage 4.1 · CommandFinalization 2.7 · TraitPresenceReconcile 2.2 |
| 2630 | 첫 공격 명령 | 16.7 | MovementUndoReconcile 11.2 · Calculate 4.5 |
| 2816 | 첫 이동 미리보기 | 45.3 | Preview.Move 45.3 |
| 2832 | 첫 이동 명령 | 32.1 | MovementUndoReconcile 15.8 · Calculate 12.9 · Publish 3.4 |
| 3985 | 다른 유닛의 첫 공격 미리보기 | 25.0 | Preview.Attack 17.1 · AttackForcedPathStep 6.8 |
| 4223 | 첫 적 턴(턴 종료 명령) | 27.2 | Calculate 25.9 |
| 5376 | 넉백이 처음 나는 공격 명령 | 27.9 | CommandFinalization 12.3 · AttackForcedPathStep 4.7 · Calculate 4.5 · MovementUndoReconcile 4.3 |
| | **합계** | **약 436** | |

- 단계 이름은 Core 마커(`CoreProfilerMarkers`·`Core.Calc.*`) 그대로다. JIT 시간은 그 단계 아래에서 난 `Mono.JIT`의 합이다.
- PC(Standalone) 스크립팅 백엔드가 설정돼 있지 않아 기본값인 Mono다(`ProjectSettings.asset`의 `scriptingBackend`에 Android만 있음). 그래서 이 비용은 빌드에서도 남는다고 본다. 크기는 에디터와 다를 수 있다(미측정).
- View 쪽 첫 실행(약 50ms)과 에디터 전용 재질 로딩(`ShaderGUIUtility.ValidateMaterial` → `File.Open`)은 이 문서의 범위가 아니다.

## 2. 무엇을 만드나

**Core 서비스 하나**(가칭 `ICombatFirstUsePreparation`)를 만든다. 전투 골격(LifeCycle Job)이 Combat 씬 로딩 중에 이 서비스를 부르면, Core가 위 경로들을 한 번씩 미리 실행해 JIT를 끝내 둔다.

### 2.1 지켜야 할 약속

1. **상태를 바꾸지 않는다.**
   - Buffer에 쓰지 않는다. `CombatTransitionPublisher.Publish`(→ `_battle.Apply`)와 `_battle.PublishPreview`를 부르지 않는다.
   - `HandledCommandNumber`·`AnsweredPreviewNumber`·`QueryRevision`·결과 순번이 그대로여야 한다. 전투 사건도 만들지 않는다.
2. **실제 전투의 난수를 소비하지 않는다.** 계산기는 입력(`CombatCommandInput`)에 실린 난수 상태로 계산한다(예: `CombatAttackCalculator`의 `arrival.ItemRandomState`). 그래서 결과를 버리면 실제 상태는 그대로라고 보지만, Core 담당이 확인해야 한다.
3. **실제 게임 결과가 같아야 한다.** 준비가 정적 캐시(특성 등록 표 등)를 먼저 채우더라도, 그 뒤의 실제 명령·미리보기 결과는 준비가 없을 때와 같아야 한다.
4. **프로세스에서 한 번만 한다.** JIT는 프로세스 단위다. 두 번째 전투부터는 바로 끝나야 한다(정적 표시 하나). 다만 이번 전투에만 있는 유닛·특성 경로(§2.3)를 따로 데울지는 D5에서 정한다.
5. **한 번 부를 때 길지 않게 한다.** 로딩 화면이 가린 동안 돌지만, 한 프레임이 수백 ms면 로딩 애니메이션이 멈춰 보인다. 단계(배치·공격·이동·적 턴 등)로 나눠, 한 번 부를 때 단계 하나를 돌게 한다. 목표는 단계당 약 100ms 이하다.

### 2.2 입구 모양(안)

```csharp
/// <summary>전투 첫 동작에 쓰는 Core 경로를 상태를 바꾸지 않고 한 번씩 미리 실행합니다.</summary>
public interface ICombatFirstUsePreparation
{
    /// <summary>남은 준비 단계 하나를 실행합니다. 더 할 단계가 남았으면 true입니다. 이미 끝났으면 바로 false입니다.</summary>
    bool PrepareNext();
}
```

- 노출 자리는 `IInGameCoreSystemGroupService`의 새 멤버 하나다(지금 `CombatCommands`·`CombatPreview`·`CombatAdvance`가 있는 곳).
- 이 그룹 파일은 Core 영역이고, 서비스 키를 쓰면 `ServiceKeys`(민형님)에 연결 줄이 생긴다.

### 2.3 미리 실행할 경로(측정 우선순위)

지금 명령·미리보기는 모두 같은 순서다. ① `CombatInputAssembler.Assemble(request)`로 Buffer의 현재 전투를 읽어 입력을 만들고, ② 정적 계산기를 부르고, ③ 결과를 게시한다. 준비는 ①과 ②만 하고 ③은 하지 않는다.

| 순서 | 경로 | 계산기(지금 코드) |
|---:|---|---|
| 1 | 배치 명령 | `CombatCommandCalculator.Calculate`(`PlaceMainCharacter`·`PlaceUnit`) |
| 2 | 공격 미리보기·공격 범위·유닛 공격 범위·행동 가능 여부 | `CombatCommandCalculator.Preview`·`GetAttackRange`·`GetUnitAttackRange`·`GetActionAvailability` |
| 3 | 이동 미리보기·이동 범위 | `CombatMovementCalculator.Preview`·`Range`, `MovementContextCalculator.Capture` |
| 4 | 공격·이동 명령 | `CombatCommandCalculator.Calculate`(`Attack`·`Move`) |
| 5 | 적 턴(자동 진행) | `CombatCommandCalculator.Advance` |
| 6 | 넉백·끌어오기 등 강제 이동이 있는 공격 | 같은 계산기 안 `AttackForcedPathStep` 경로 |

- 공격·특성 경로는 유닛마다 다르다(3985·5376). 이번 전투의 실제 아군 로드아웃·적 배치에서 종류마다 한 번씩 고르면 처음 지나는 새 경로까지 덮을 수 있다. 무엇을 고를지는 Core가 정한다.

### 2.4 가장 큰 설계 문제: 준비 시점의 상태

- 준비는 Combat 씬 로딩의 "씬 준비" 단계에서 돈다. 이때는 전투 준비(`BattleEntry.EnterBattle`)가 이미 끝나 Buffer에 전투가 있다. 국면은 배치이고, **필드에 아군이 없다**(손패만 있음).
- 그래서 지금 상태로는 공격·이동 입력을 만들 수 없다. 공격자·이동자가 필드에 없기 때문이다.
- 한 가지 방법은 **배치 계산 결과(`CombatCommandTransition`)의 상태에서 이어 계산**하는 것이다. 이를 위해 Buffer가 아니라 계산 결과 상태로 입력을 만드는 내부 입구가 필요하다.
- 적 턴도 비슷하다. 아군이 놓이고 전투 국면이 된 가상 상태가 있어야 의미 있는 경로를 지난다.
- 이 부분은 Core 내부 구조를 아는 사람이 정해야 한다(D2).

## 3. 못 덮는 것·주의

- **게시 경로:** 첫 배치의 `Core.Combat.Publish` 아래 JIT 38ms는 Buffer에 쓰는 코드(`CombatTransitionPublisher.Publish` → `_battle.Apply`)다. 상태를 바꾸지 않는 준비로는 실제 Buffer에서 이 코드를 돌릴 수 없다(D3).
- **새 경로:** 이번 전투에 없는 유닛·특성 경로는 못 덮는다. 다음 전투에서 처음 지날 때 다시 난다.
- **로딩 길이:** 덮는 만큼(최대 약 400ms) 첫 전투 로딩이 길어진다. 비용이 가린 화면 뒤로 옮겨 가는 것이지 없어지는 것은 아니다. 이 기록의 Combat 로딩은 이미 504ms·892ms 프레임이 있었다(140·143). 선택 유닛 준비는 144~148에 돌았다.
- **빌드 설정 대안:** PC 빌드를 IL2CPP로 바꾸면 JIT 자체가 없어진다. 이 작업과 별개로 검토할 수 있다(빌드 설정 담당 결정).
- **하지 않는 것(§68):** View가 가짜 배치·공격·이동 명령을 내지 않는다. View가 reflection으로 Core를 강제 JIT하지 않는다. 게임 결과·난수·Buffer를 오염시키지 않는다.

## 4. 전투 골격(우리 몫) — 참고

Core 입구가 생기면 우리 쪽은 이렇게 붙인다(상세는 결정 뒤 설계).

1. 전투 골격(`LifeCycles/Combat/`)에 로딩 준비 하나(가칭 `CombatFirstUseLoadingPreparation : ILodingPreparation`)를 만들어 Combat 씬 준비로 등록한다. View의 선택 유닛 준비(`CombatViewSystem.cs:247` → `CombatUnitPreparation`)와 같은 단계이며, 두 준비의 순서는 설계 때 정한다.
2. 준비가 Tick마다 `Register(Combat, JobType.FirstUsePrepare)`(새 전투 값)를 하나씩 건다. Job은 `PrepareNext()`를 한 번 부른다. false가 오면 준비를 완료로 바꾸고 화면이 드러난다.
3. 연결 줄(`JobType` 전투 값, `JobRegisterSystem`의 Combat 줄)은 영역 규칙상 연결 작업이다. 바꾸면 민형님 공유 문서에 적는다.
4. Core 입구가 없으면 이 준비를 등록하지 않는다.

## 5. 정할 것

| 번호 | 정할 것 | 안 |
|---|---|---|
| D1 | 단계 나눔 | `PrepareNext()` 하나(권장, 단계 목록은 Core 안) / 단계 열거형을 받는 `Prepare(stage)` |
| D2 | 공격·이동·적 턴을 데울 가상 상태 | 배치 계산 결과 상태에서 이어 계산(권장) / 별도 시험용 전투 정의 / 배치 경로만 데움 |
| D3 | 게시 경로(38ms) | 그대로 둠(권장, 첫 배치에서만 남음) / 버리는 Buffer 인스턴스에 적용 / 게시 코드 자체 정리 |
| D4 | 준비 중 예외 | 숨기지 않고 오류 경계(F1)로 보냄(권장, 프로젝트 규칙) / 경고 뒤 건너뜀 |
| D5 | 반복 범위 | 공통 경로는 프로세스당 한 번 + 이번 전투의 새 유닛·특성 경로만 추가(권장) / 매 전투 전부 / 처음 한 번만 |
| D6 | 노출 자리 | `IInGameCoreSystemGroupService` 새 멤버(권장) / 기존 서비스에 메서드 추가 |
| D7 | 누가 만드나 | 사용자 판단: 천명님께 요청 / 사용자 지시로 이번 작업에서 Core 수정(그러면 Core 전달 문서 §5에 기록) |

## 6. 확인 방법

- 같은 시작 조건으로 기록해, 첫 배치(1404 상당)·첫 공격 미리보기(2552)·첫 이동(2816·2832)의 Core 안 JIT가 줄었는지 본다. 로딩 준비 프레임이 얼마나 늘었는지도 함께 적는다.
- 준비가 있을 때와 없을 때, 같은 입력에 대한 명령·미리보기 결과(사건 목록·피해·난수 결과)가 같은지 확인한다. Core 쪽 시험은 Core 담당이 정한다. View 작업은 시험 코드를 쓰지 않는다.
- 두 번째 전투 진입에서 준비가 바로 끝나는지 확인한다(D5).
