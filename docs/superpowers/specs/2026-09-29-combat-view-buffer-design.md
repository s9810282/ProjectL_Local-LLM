# CombatViewBuffer 설계 — View가 쓰고 Core 측이 읽는 전투 입력 Buffer

- 작성일: 2026-09-29
- 작성: View 작업(스테이지 내부 담당)
- 상태: **Buffer 구현 완료(계획 Phase 1~3).** 마무리 항목은 계획 Phase 4에 있다.
- 이 문서가 답하는 것: 천명님 캐릭터 Buffer 이전 계획의 **G5 "View 입력/결과 계약"** 중 입력 쪽
- 범위 변경(3차): 전투 Flow(`CombatLifeCycle`·`CombatPlan`·전투 Job)는 이 문서에서 떼어 **별도 설계**로 옮겼다. 옮긴 내용은 §12에 출발점으로만 남긴다.

## 0. 요약

- 스테이지 내부(배치·전투)의 사용자 선택을 담는 Buffer를 **하나** 만들었다. 이름은 **`CombatViewBuffer`**다.
- View만 쓰고, Core 측(Systems의 Flow·Job·처리 서비스)은 **읽기만** 한다.
- 담는 것은 세 가지다.
  1. **실행 명령**: 쌓아 두고 번호 순서대로 처리한다.
  2. **미리보기 요청**: 한 칸만 두고 최신 것으로 덮어쓴다.
  3. **연출 완료 확인**: "Core 결과 N번까지 연출을 끝냈다"는 번호 하나다.
- 번호는 Buffer가 발급한다. 계속 커지기만 하고 다시 쓰지 않는다. Core 측은 "어디까지 처리했나"를 **자기 쪽에** 기록한다. 그래서 Core가 입력을 지우거나 표시하지 않아도 같은 입력을 두 번 처리하지 않는다.
- 이 Buffer가 꼭 필요한 이유가 있다. `JobRegisterSystem.Register`는 enum 두 개(`JobRootType`, `JobType`)만 받는다. 명령 내용을 Job에 넘길 수 있는 통로는 Buffer뿐이다. 기존 `StageJob`도 Execute할 때 Current Buffer를 읽는다.

## 1. 범위

| 포함 | 제외 |
|---|---|
| `CombatViewBuffer`: 명령 8종, 미리보기 6종, 연출 완료 확인 | **전투 Flow**(`CombatLifeCycle`, `CombatPlan`, 전투 `JobType`·Job, 연출 대기): 별도 설계 |
| 값 타입과 형식 검사 | 스테이지 **맵**·상점·회복 입력(천명님 제안의 StageInputBuffer) |
| Buffer 등록(Factory, 서비스 키, `IBufferQuery`) | Core 측 결과·전투 상태 Buffer와 처리 서비스(천명님 Task 7, G1) |
| Core 측에 요청할 결과 번호 계약(§9) | View 입력 핸들러(클릭 → 명령)와 연출 코드 |

## 2. 전제

| 출처 | 내용 |
|---|---|
| 천명님 [View 입력 Buffer 제안](../handoffs/2026-09-29-view-input-buffer-core-contract-ko.md) | Core는 입력을 읽기만 한다. 입력 제거는 View/Framework가 한다. 입력 번호로 결과를 대응시킨다(제안). 기존 ReadSink/WriteSink·PostBindService·Plan/Job을 재사용한다 |
| 천명님 [캐릭터 Buffer 이전 계획](../plans/2026-09-29-character-buffer-effect-migration-plan.md) Task 7·8 | Core는 명령마다 Buffer에서 상태를 복원해 처리하고, "다음 기록 + 결과"를 한 번에 쓴다. 손패·필드 명령은 `EntityId`를 쓴다 |
| 기존 Buffer 코드 | `XxxBuffer : IXxxReadSink, IXxxWriteSink, IExportSystemServices`로 만들고 `BufferSystemFactory`에서 생성한다. 외부에는 `IBufferQuery`로 노출한다. 서비스 저장소는 **인터페이스 타입**으로 등록하므로 키 이름이 `Read`/`Write`로 같아도 충돌하지 않는다 |
| CLAUDE.md P4 | 공용 타입은 Core.Contracts 것을 재사용한다(`EntityId`, `CellCoord`, `Rotation4`, `AttackAim`) |

## 3. 규칙

| 번호 | 규칙 |
|---|---|
| **V1** | **View만 쓴다.** Core 측에 주는 `ICombatViewBufferReadSink`에는 값을 바꾸는 메서드가 없다(`Dequeue`, `MarkProcessed`, `Clear` 없음). `Commands`는 읽기 전용 래퍼다 |
| **V2** | **번호는 Buffer가 발급한다.** 명령과 미리보기가 같은 카운터를 쓴다. 1부터 계속 커지며 `Clear`해도 되돌리지 않는다 |
| **V3** | **명령에는 사용자의 선택만 담는다.** 개체 ID, 논리 칸, 방향, 조준만 넣는다. 체력·비용·가능 여부·피해량, 화면 좌표, GameObject는 넣지 않는다. 형식만 검사하고 규칙상 가능한지는 Core가 판단한다 |
| **V4** | **실행 명령은 처리되기 전에 사라지지 않는다.** 번호 순서대로 쌓는다. View는 Core가 "N번까지 처리했다"고 알린 뒤에만 N번 이하를 지운다 |
| **V5** | **미리보기 요청은 한 칸이다.** 덮어쓸 때마다 새 번호를 받는다. 결과 번호가 현재 요청 번호와 다르면 View가 버린다 |
| **V6** | **연출 완료 확인은 줄어들지 않는다.** 이전보다 작은 값은 거절한다. 같은 값은 다시 넣어도 된다 |
| **V7** | **처리 여부는 Core 측이 기록한다.** Buffer는 "처리 중"이나 "처리됨" 상태를 갖지 않는다. 읽는 쪽은 `TryGetNextCommand(마지막 처리 번호)`로 다음 명령을 찾는다. 같은 처리가 두 번 요청돼도 두 번째는 새 명령을 찾지 못한다 |
| **V8** | **View는 모든 결과를 확인한다.** 연출할 것이 없는 결과나 거절된 결과도 확인 번호를 올린다. 연출 완료를 기다리는 쪽이 멈추지 않게 하기 위해서다 |
| **V9** | **번호 타입의 생성자는 `internal`이다.** 다른 어셈블리(에디터 도구, 테스트)는 번호를 지어낼 수 없다. 다만 같은 `ProjectL.Systems` 어셈블리의 View 코드는 막지 못하므로, 거기서는 **WriteSink로만 기록한다**는 규칙으로 지킨다 |

## 4. 데이터

타입은 모두 `ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView`에 있다. 순수 C#이며 `using UnityEngine`을 쓰지 않는다.

### 4.1 실행 명령 — `CombatViewCommandKind`

| Kind | 값 | 현재 Core 진입점 |
|---|---|---|
| `PlaceMainCharacter` | 칸, 방향 | `TryPlaceInitialMainCharacter` |
| `PlaceUnit` | 개체 ID, 칸, 방향 | `TryPlaceOnCombat` |
| `ReturnUnit` | 개체 ID | `TryReturnToAvailableReserve`(빈 슬롯 자동 선택) |
| `EnterCombat` | 없음 | `TryEnterCombat` |
| `Move` | 개체 ID, 칸 | `TryMove` |
| `Attack` | 개체 ID, 조준(`AttackAim`) | `TryAttack(attackerId, aim)` |
| `EndUnitTurn` | 없음 | `EndUnitTurn` |
| `DrawCharacter` | 없음 | `TryDraw` |

`None = 0`은 기본값이다. 보관되는 명령에는 쓰이지 않는다. 확장 자리(D3): 본부 수동 배치, 지정 슬롯으로 회수, 아티팩트 조작.

### 4.2 미리보기 요청 — `CombatViewPreviewKind`

| Kind | 값 | 현재 Core 진입점 |
|---|---|---|
| `MainCharacterPlacement` | 칸, 방향 | `PreviewInitialMainCharacterPlacement` |
| `UnitPlacement` | 개체 ID, 칸, 방향 | `PreviewCombatPlacement` |
| `MoveRange` | 개체 ID | `GetMovementRange` |
| `Move` | 개체 ID, 칸 | `PreviewMove` |
| `AttackRange` | 개체 ID | `GetAttackRange` |
| `Attack` | 개체 ID, 조준 | `PreviewAttack(attackerId, aim)` |

미리보기 해제는 요청을 비우는 것으로 표현한다(`ClearPreview`).

### 4.3 값 타입

| 타입 | 모양 |
|---|---|
| `CombatViewCommandData` | `readonly struct`. 속성 `Kind`, `EntityId`, `Cell`, `Rotation`, `Aim`. 생성자는 private이고 명령마다 정적 생성 메서드가 있다. Kind에 쓰이지 않는 값은 기본값이며 의미가 없다 |
| `CombatViewCommand` | `readonly struct`. `Number` + `Data`. `internal` 생성자에서 번호가 1 이상인지, Kind가 `None`이 아닌지 검사한다 |
| `CombatViewPreviewData` / `CombatViewPreviewRequest` | 명령과 같은 모양 |
| `CombatViewInputGuard` (internal) | 형식 검사만 한다: 개체 ID가 비었는지, `Rotation4`가 정의된 값인지, 조준의 `Kind`가 `None`인지. `AttackAimKind`의 0이 `None`이라 `default(AttackAim)`은 거절된다 |

## 5. Buffer 인터페이스

```csharp
public interface ICombatViewBufferReadSink : IServiceModule
{
    IReadOnlyList<CombatViewCommand> Commands { get; }                        // 번호 오름차순
    bool TryGetNextCommand(int handledThrough, out CombatViewCommand command);
    bool HasPreview { get; }
    CombatViewPreviewRequest Preview { get; }     // HasPreview가 false이면 기본값
    int PresentedResultSequence { get; }          // 0은 아직 없음
}

public interface ICombatViewBufferWriteSink : IServiceModule
{
    int Submit(CombatViewCommandData command);            // 발급 번호 반환
    int SetPreview(CombatViewPreviewData preview);        // 교체, 발급 번호 반환
    void ClearPreview();
    void RemoveCommandsThrough(int handledNumber);        // 음수·미발급 번호는 ArgumentOutOfRangeException
    void AcknowledgePresented(int resultSequence);        // 줄어드는 값은 ArgumentOutOfRangeException
    void Clear();                                         // 명령·미리보기만 비움. 카운터·확인 값은 유지
}
```

외부 접근은 `IBufferQuery.CombatViewReadSink` / `CombatViewWriteSink`로 한다.

## 6. 사용 계약

```text
1. View      : Submit(Move(unit 7, (3,4)))                         → 명령 42
2. Core 측   : TryGetNextCommand(마지막 처리 41) → 42
               → 처리 → 결과 118 기록, 마지막 처리 번호 = 42        (R1·R2)
3. View      : 결과 118 읽음 → RemoveCommandsThrough(42)
               → 연출 → AcknowledgePresented(118)
4. Core 측   : 연출 완료가 필요한 다음 진행은 PresentedResultSequence ≥ 118을 보고 시작
```

- 거절도 결과다. 거절된 명령도 처리 번호가 올라가고 View가 지운다. 자동으로 다시 실행하지 않는다.
- 미리보기: `SetPreview`로 번호 43을 받는다. Core 측 결과에 "요청 43"이 적힌다(R3). View는 현재 요청 번호가 43일 때만 표시한다.
- **누가 언제 읽고 등록하는지**(Flow, Job 구성, 연출 대기)는 전투 Flow 설계가 정한다.

## 7. 수명과 초기화

| 시점 | 할 일 | 비고 |
|---|---|---|
| 전투 진입·종료 | `Clear()` | 호출 위치는 전투 Flow 설계에서 정한다(후보: `CombatLifeCycle.OnEnter`/`OnExit`) |
| 새 Run(`ResetRunProgress`) | **호출하지 않는다** | 전투 수명 데이터라 Run 초기화 범위가 아니다 |

번호 카운터와 `PresentedResultSequence`는 `Clear`해도 유지한다. 전투가 바뀌어도 번호가 섞이지 않게 하기 위해서다. 그래서 Core 측 결과 순번도 되돌리지 않아야 한다(R1).

## 8. 파일

| 파일 | 내용 | 영역 |
|---|---|---|
| `BufferSystems/CombatView/CombatViewCommandKind.cs` | 명령 종류 | 신규 |
| `BufferSystems/CombatView/CombatViewCommand.cs` | `CombatViewCommandData`, `CombatViewCommand`, `CombatViewInputGuard` | 신규 |
| `BufferSystems/CombatView/CombatViewPreviewKind.cs` | 미리보기 종류 | 신규 |
| `BufferSystems/CombatView/CombatViewPreview.cs` | `CombatViewPreviewData`, `CombatViewPreviewRequest` | 신규 |
| `BufferSystems/CombatView/CombatViewBuffer.cs` | 인터페이스 두 개와 Buffer | 신규 |
| `ServiceKeys/DataBaseGroupServiceKey.cs` | `CombatViewBufferKey.Read/Write` | Framework 공용 |
| `Factory/Runtime/Buffer/BufferSystemFactory.cs` | `new CombatViewBuffer().ExportServices(builder)` | Framework 공용 |
| `BufferSystems/Core/BufferSystemGroup.cs` | `IBufferQuery`에 두 속성 추가. `ResetRunProgress`는 그대로 | Framework 공용 |

폴더 이름은 Core 전투 상태 Buffer(G1)와 겹치지 않게 `CombatView`로 했다.

## 9. Core 측에 요청한 것 (Buffer 관련)

[Core 측 요청 문서](../handoffs/2026-09-29-combat-view-buffer-core-requests.md)에 R1~R6을 적어 보냈다. 이 Buffer와 직접 맞물리는 것은 아래 넷이다. R4(현재 국면)와 R6(처리 서비스)은 전투 Flow 설계에서 다룬다.

| 번호 | 요청 | 이 Buffer와의 관계 |
|---|---|---|
| **R1** | 결과마다 계속 커지는 **결과 순번**을 붙이고, 명령에서 나온 결과면 **명령 번호**도 적는다. 최신 결과 순번을 읽을 수 있게 한다 | `AcknowledgePresented`에 넣을 값이다 |
| **R2** | **마지막으로 처리한 명령 번호**를 읽을 수 있게 한다 | `TryGetNextCommand`와 `RemoveCommandsThrough`에 넣을 값이다 |
| **R3** | 미리보기 결과에 **요청 번호**를 적는다 | `Preview.Number`와 비교해 오래된 결과를 버린다 |
| **R5** | 결과에 한 명령 안에서 **일어난 일을 순서대로 담은 목록**을 넣는다 | 연출의 재료. 연출이 끝나야 확인 번호를 올린다 |

## 10. 결정

| 번호 | 질문 | 결정 / 권장 |
|---|---|---|
| **D1** | Core 측이 WriteSink에 손대지 못하게 코드로 막을지 | 적용: 기존 Buffer처럼 `IBufferQuery`에 둘 다 올렸다. 강제 분리는 모든 Buffer에 걸리는 일이라 민형님께 여쭈었다 |
| **D2** | 전투 결과 화면 명령(계속, 재도전, 새 Run) | 넣지 않는다. Run 진행 명령이고 StageInputBuffer 쪽이다 |
| **D3** | 본부 수동 배치, 지정 슬롯 회수, 아티팩트 조작 | 이번에는 제외한다. 화면에 생길 때 Kind를 추가한다 |
| **D4** | 실행 명령을 여러 개 쌓을지 | 쌓는다. 입력을 막는 건 View 입력 레이어가 한다 |
| **D5** | 검증 방법 | **결정: 에디터 컴파일과 EditMode 테스트.** 기존 `ProjectL.Tests.Framework`에 `CombatViewBufferTests`를 추가했다. 새 asmdef는 만들지 않았다. 이 어셈블리는 커밋 `62f7fda5`에서 옛 Artifact API가 정리되어 컴파일된다 |

## 11. 범위 밖

- 전투 Flow 전체: 별도 설계(§12)
- View 입력 핸들러와 연출 코드
- Core 처리 서비스, 결과·전투 상태 Buffer(천명님 Task 7, G1)
- 스테이지 맵·상점·회복 입력

## 12. 전투 Flow 설계로 넘긴 내용 (출발점)

2차 설계에 있던 내용이다. 확정이 아니고, 새 설계에서 다시 검토한다.

- Flow 위치 후보는 `CombatLifeCycle.OnTick`이다. Runner가 비어 있을 때(`IJobRunner.IsIdle`)만 `Register(Combat, …)`를 호출한다.
- `JobType` 후보: `CombatCommand`, `CombatAdvance`, `CombatPreview`. `CombatPlan`은 `StagePlan`처럼 람다로 `IBufferQuery`를 받는다.
- Job 구성 후보: 명령 처리 → 연출 대기, 진행 → 연출 대기, 미리보기 단독.
- **연출 대기 Job**은 `PresentedResultSequence ≥ 최신 결과 순번`일 때 완료된다. 옛 연출 대기 게이트를 대신한다.
- Core 측 요청 R4(현재 국면)와 R6(처리 서비스)가 필요하다.
- 열린 질문
  - D6: 담당. 사용자가 맡기로 했다.
  - D7: 전투를 나갈 때 `IJobRunner.Clear()`를 호출할지. 민형님께 여쭈었다.
  - D8: Core 서비스가 없는 동안 Job 몸통을 어떻게 둘지

## 13. 구현 결과 (2026-09-29)

| Phase | 결과 | 커밋 |
|---|---|---|
| 0 | 전달 문서 두 개를 **새 파일로** 작성: [Core 측 요청](../handoffs/2026-09-29-combat-view-buffer-core-requests.md), [Framework 공유](../handoffs/2026-09-29-combat-view-buffer-framework-share.md). 계획은 기존 전달 문서에 덧붙이는 것이었으나 사용자 지시로 바꿨다 | `27f8c8f4` |
| 1 | 값 타입 4개 | `8d0c9947` |
| 2 | `CombatViewBuffer` | `ec7152fb` |
| 3 | 공용 파일 3곳 등록 | `22ba69fc` |

설계와 달라진 점

- **`CombatViewInputGuard`(internal)를 `CombatViewCommand.cs`에 두었다.** 명령과 미리보기가 같은 형식 검사를 쓰기 때문이다. 파일 수는 계획대로다.
- **값 검사에 실패하면 번호를 쓰지 않는다.** 번호를 먼저 정해 두고, 번호 타입 생성자의 검사를 통과한 뒤에 확정한다.
- **미리보기가 없을 때 `Preview`는 예외 대신 기본값을 돌려준다.** `CurrentProgressBuffer.CurrentStage`와 같은 방식이다.
- **`internal` 생성자에 대한 설명을 바로잡았다(V9).** 1차 설계에는 "View가 번호를 지어낼 수 없다"고 적었다. 하지만 같은 어셈블리 안에서는 `internal`로 막을 수 없어서 규칙으로 지킨다.

검증

- Phase 1~3마다 에디터 컴파일 에러가 0개였다(사용자 확인).
- Buffer 테스트 `Assets/Tests/EditMode/Framework/CombatViewBufferTests.cs`(12개)를 작성했다. **EditMode 실행 결과는 아직 받지 않았다.**

전달

- 먼저 보낸 [Framework 공유 문서](../handoffs/2026-09-29-combat-view-buffer-framework-share.md)는 "변경 예정"으로 쓰였고, 전투 Flow 쪽 변경도 담고 있다.
- 후속으로 [Framework 반영 결과](../handoffs/2026-09-29-combat-view-buffer-framework-applied.md)를 새로 썼다.
  - Buffer 쪽 세 파일은 반영했다.
  - Flow 쪽 두 파일(`JobType`, `JobRegisterSystem`)은 변경하지 않았고 새 Flow 설계에서 다시 정한다.
