# CombatViewBuffer — 로컬 세션 인계 문서

> 2026-09-29 로컬 세션(PC: Hyeon2-400)에서 진행한 CombatViewBuffer 작업을 다른 PC나 새 세션이 이어받기 위한 문서다.
> 새 세션은 **이 문서 → 저장소 루트 `CLAUDE.md` → [설계](../specs/2026-09-29-combat-view-buffer-design.md) → [계획](../plans/2026-09-29-combat-view-buffer-plan.md)** 순서로 읽고 시작한다.
> 대화 기록과 Claude 메모리는 원래 PC에만 있다. 새 세션에 필요한 내용은 모두 이 문서에 옮겼다.

- 작성일: 2026-09-29
- 작성: View 작업(스테이지 내부 담당, 로컬 세션)
- 대상 브랜치: `feature/Yonggeun/Stage` (최신 `0fef7a76` "[용근] 버퍼 설계 및 문서 마무리")
- 상태: **Buffer 구현 완료. EditMode 테스트 결과 확인 대기.** 다음 작업은 전투 Flow 새 설계다.

## 1. 한눈에 보기

| 항목 | 내용 |
|---|---|
| 목표 | View가 쓰고 Core 측(Systems의 Flow·Job·처리 서비스)은 읽기만 하는 전투 입력 Buffer. 천명님 계획의 **G5 "View 입력/결과 계약"** 중 입력 쪽 답 |
| 결과 | `CombatViewBuffer`: 명령 8종, 미리보기 6종, 연출 완료 확인. 공용 파일 3곳에 등록. 테스트 12개 |
| 남은 일 | §6. 테스트 결과 확인 → 전투 Flow 새 설계 |
| 기준 문서 | [설계](../specs/2026-09-29-combat-view-buffer-design.md) · [계획](../plans/2026-09-29-combat-view-buffer-plan.md) · [Core 측 요청](2026-09-29-combat-view-buffer-core-requests.md) · [Framework 공유(예정)](2026-09-29-combat-view-buffer-framework-share.md) · [Framework 반영 결과](2026-09-29-combat-view-buffer-framework-applied.md) |

## 2. 작업 방식

규칙 전체는 저장소 루트 `CLAUDE.md`에 있다. 아래는 **CLAUDE.md에 없고 원래 PC의 메모리에만 있던 것**이다.

**사용자와 담당 범위**

- 사용자(용근, View 작업자)는 **스테이지 내부** 담당이다. 한 스테이지 안의 배치·전투·연출·전투 UI가 범위다.
- **이름이 헷갈린다.**
  - 새 구조에서 스테이지 내부는 **`CombatLifeCycle`**이다.
  - **`StageLifeCycle`은 스테이지 맵**(챕터 노드 선택)이고 사용자 범위로 단정하지 않는다.
  - 옛 코드의 "Stage 씬"은 전투 씬이다.
- 사용자는 전투 Flow(`CombatLifeCycle`·`CombatPlan`·전투 `JobType`·전투 Job)도 맡기로 했다(설계 D6).

**구현 방식**

- 새 기능은 기본적으로 **실습 워크시트 artifact**로 준다. 파일을 직접 고치지 않고, 사용자가 보고 친다.
  - 형식: 단계별 접히는 카드, 파일 경로와 실제 줄 번호, "참고할 기존 코드 / 칠 코드", 단계마다 "왜"와 "함정", 끝에 흔한 에러 표
- 사용자가 **"너가 진행해줘" / "너가 해라"**라고 하면 Claude가 직접 구현한다. 이번 작업의 Phase 1~4는 모두 이렇게 했다.
- 커밋·push는 사용자가 한다. Phase가 끝나면 멈추고 보고한다.
- 설계와 다른 결정이 필요하면 구현하지 말고 먼저 묻는다.
- 사용자가 "파일은 수정이 아니라 새로 작성"이라고 하면 기존 문서를 고치지 않고 새 파일을 만든다(Phase 0, 공유 문서 후속이 그 예).

**검증**

- 이 PC에는 dotnet이 없다. 컴파일은 Unity 에디터로만 확인한다.
- View(Unity 쪽) 코드를 위해 **새 테스트 asmdef를 제안하지 않는다.** 이번 Buffer 테스트는 순수 C#이라 기존 `ProjectL.Tests.Framework`에 넣었다.

## 3. 커밋 이력 (`feature/Yonggeun/Stage`)

| 커밋 | 내용 |
|---|---|
| `62f7fda5` | 설계·계획 수립. 테스트의 옛 Artifact API 정리도 포함(사용자) |
| `27f8c8f4` | Phase 0: Core 측 요청, Framework 공유(예정) 문서 |
| `8d0c9947` | Phase 1: 값 타입 4개 |
| `ec7152fb` | Phase 2: `CombatViewBuffer` |
| `22ba69fc` | Phase 3: 공용 파일 3곳 등록 |
| `0fef7a76` | Phase 4: 테스트, Framework 반영 결과 문서, 설계·계획 정리 |

## 4. 완료한 작업

| Phase | 결과 |
|---|---|
| 0 | [Core 측 요청](2026-09-29-combat-view-buffer-core-requests.md)(R1~R6), [Framework 공유(예정)](2026-09-29-combat-view-buffer-framework-share.md). 둘 다 새 파일로 작성 |
| 1 | `BufferSystems/CombatView/`: `CombatViewCommandKind.cs`, `CombatViewCommand.cs`(형식 검사 도우미 `CombatViewInputGuard` 포함), `CombatViewPreviewKind.cs`, `CombatViewPreview.cs` |
| 2 | `CombatViewBuffer.cs`: `ICombatViewBufferReadSink`(조회만), `ICombatViewBufferWriteSink`, `CombatViewBuffer` |
| 3 | `DataBaseGroupServiceKey.cs`(`CombatViewBufferKey`), `BufferSystemFactory.cs`(생성 한 줄), `BufferSystemGroup.cs`(`IBufferQuery.CombatViewReadSink/WriteSink`). `ResetRunProgress`는 변경하지 않음 |
| 4 | `Assets/Tests/EditMode/Framework/CombatViewBufferTests.cs`(12개), [Framework 반영 결과](2026-09-29-combat-view-buffer-framework-applied.md), 설계 §12·§13 |

Phase 1~3은 매번 에디터 컴파일 에러 0을 사용자가 확인했다. Phase 1 워크시트 artifact(https://claude.ai/artifact/FupR1YSAJyHibCPHvWojiq)도 있다. 사용자 계정에서만 열린다.

## 5. 알아 둘 것

**Buffer 설계의 핵심** (설계 §3)

- 번호는 Buffer가 발급한다. 명령과 미리보기가 한 카운터를 쓰고, 번호는 계속 커지며 `Clear`해도 되돌리지 않는다.
- Core 측은 **마지막 처리 번호 하나만 자기 쪽에** 기록한다. 그리고 `TryGetNextCommand(그 번호)`로 다음 명령을 찾는다. Buffer에는 "처리됨" 상태가 없다.
- View는 Core가 처리했다고 알린 번호 이하만 `RemoveCommandsThrough`로 지운다.
- View는 모든 결과에 대해 `AcknowledgePresented`를 올린다. 연출할 게 없는 결과나 거절된 결과도 마찬가지다.
- `internal` 생성자는 다른 어셈블리만 막는다. 같은 `ProjectL.Systems`의 View 코드는 **WriteSink로만 기록한다**는 규칙으로 지킨다(V9).

**Framework 구조에서 확인한 사실**

- 모든 Buffer는 `IBufferQuery`(`BufferSystemKey.Buffer`) 하나로 노출된다. ReadSink와 WriteSink를 함께 노출하므로, 쓰기 권한은 관례로만 지켜진다(D1, 민형님께 질문).
- 서비스 저장소는 인터페이스 **타입**으로 등록한다. 키 이름 `Read`/`Write`가 겹쳐도 괜찮다.
- `JobRegisterSystem.Register(JobRootType, JobType)`은 enum 두 개만 받는다. 그래서 명령 내용을 Job에 넘기는 통로는 Buffer뿐이다. `StageJob`도 Execute할 때 Buffer를 읽는다.
- `JobRunnerSystem`에 대해
  - 전역에 하나뿐이다.
  - `IsCompleted`가 false인 Job을 다음 Tick에도 붙잡는다. 이 동작을 이용하면 연출 대기 Job을 만들 수 있다.
  - `IsIdle`로 비었는지 알 수 있다.
- `CombatLifeCycle`, `CombatPlan`, `CombatJob`은 골격과 TODO뿐이다. Flow 클래스는 아직 없다.
- `SystemBuilderHelper`의 서비스 등록, `LifeCycleBase`의 `OnEnter`/`OnTick`/`OnExit` 흐름은 `Systems/Documentation/framework-architecture.md` §4·§5·§7에 있다.

**Core 쪽 방향** (천명님 09-29 문서)

- [캐릭터 Buffer 이전 설계](../specs/2026-09-29-character-buffer-effect-migration-design.md)와 [계획](../plans/2026-09-29-character-buffer-effect-migration-plan.md)에 따르면, Core는 상태를 들고 있지 않게 된다. 명령마다 Buffer에서 상태를 복원해 처리하고 "다음 기록 + 결과"를 한 번에 쓴다(Task 7).
- 이 계획에는 G1(전투 상태 Buffer 위치)과 G5(View 입력/결과 계약)가 열려 있다. 우리 작업이 G5의 입력 쪽 답이다.
- CLAUDE.md는 "GwonCheonmyeong 작성 문서를 기준으로 삼지 않는다"고 한다. 다만 이 두 문서는 **현재 Core 방향을 보여 주는 최신 계획**이라 참고했다. 규칙의 근거로는 쓰지 않는다.

**기타**

- `docs/superpowers/viewDocs/`는 이 브랜치에 **없다.** 옛 전투 연출 재설계(Projector·Director, 연출 대기 게이트)는 다른 브랜치의 `viewDocs/stage-redesign/`에 있었다.
- 에디터가 `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset`(타임스탬프)와 `Assets/Scenes/Stage.unity`(줄바꿈)를 바꿔 놓는 경우가 있다. 이번 작업과 무관하다.
- Core 요청 문서는 CLAUDE.md가 지정한 `2026-09-24-view-core-data-boundary-handoff.md`가 아니라 **새 파일**에 있다. 천명님께는 새 문서 링크로 전달해야 한다.

## 6. 남은 일 (우선순위 순)

### 6.1 Buffer 테스트 결과 확인

- 에디터 Test Runner → EditMode → `ProjectL.Tests.Framework` → `CombatViewBufferTests` 12개를 실행한다.
- 결과는 아직 받지 않았다.
- 전부 통과하면 설계 §13과 계획 Phase 4.2를 "통과"로 고치고 Buffer 작업을 닫는다.

### 6.2 전달 (사용자가 직접)

| 대상 | 문서 | 덧붙일 말 |
|---|---|---|
| 천명님(Core) | [Core 측 요청](2026-09-29-combat-view-buffer-core-requests.md) | §4 "전투 Flow가 입력을 처리로 넘기는 방식"은 이후 Flow를 따로 설계하기로 해서 **예정** 단계다. R1~R6 요청은 그대로 유효하다 |
| 민형님(Framework) | [공유(예정)](2026-09-29-combat-view-buffer-framework-share.md) + [반영 결과](2026-09-29-combat-view-buffer-framework-applied.md) | 3곳은 반영했다. `JobType`·`JobRegisterSystem`은 Flow 설계에서 다시 정한다. D1은 질문으로 남아 있다 |

전달했는지는 확인하지 않았다. 새 세션에서 사용자에게 묻는다.

### 6.3 전투 Flow — 새 설계·계획 (다음 작업)

사용자 결정: 기존 Buffer 문서와 나눠 **새 설계·계획 문서**로 처음부터 쓴다. 설계 §12의 내용은 출발점일 뿐이며 확정이 아니다.

설계 전에 읽을 것

- [Buffer 설계 §12](../specs/2026-09-29-combat-view-buffer-design.md): 넘겨받은 후보들
  - Flow 위치 `CombatLifeCycle.OnTick`
  - `JobType` 후보 `CombatCommand` / `CombatAdvance` / `CombatPreview`
  - 연출 대기 Job
  - 열린 질문 D7(나갈 때 Runner 비우기), D8(Core 서비스가 없는 동안 Job 몸통)
- [Core 측 요청](2026-09-29-combat-view-buffer-core-requests.md): R4(현재 국면), R6(처리 서비스). Flow가 여기에 기댄다
- 코드
  - `LifeCycleSystem/Base/LifeCycleBase.cs`, `LifeCycleManager.cs`
  - `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs`, `JobRunner/JobRunnerSystem.cs`
  - `LifeCycles/Stage/Plans/StagePlan.cs`, `Jobs/StageJob.cs`(Buffer를 넘겨받는 선례)
  - `LifeCycles/Combat/*`(골격)
- 참고: [옛 Stage View → Buffer 대응표](../specs/2026-09-28-stage-view-buffer-mapping.md) §3.4 F(진행 주도)·§5(결정할 것)

알려진 선행 조건

- Core 측 R1~R4가 없으면 Flow 본체(`OnTick` 분기)와 연출 대기 Job을 끝까지 만들 수 없다.
- Core가 늦을 때 "읽기용 좁은 인터페이스를 View 쪽에 먼저 정의"하는 방안이 있다. 설계에 없던 결정이라 **사용자에게 먼저 묻는다.** 천명님께도 의견을 여쭈었다.

### 6.4 그 뒤 후보 (사용자가 정하면 설계부터)

- View 입력 핸들러: 클릭·드래그를 `CombatViewCommandData`로 바꿔 `Submit`
- 연출: Core 결과(R5의 사건 목록)를 읽어 연출하고 `AcknowledgePresented`

## 7. 검증 방법

- 컴파일: Unity 에디터 Console 에러 0
- 테스트: Test Runner → EditMode → `ProjectL.Tests.Framework`

## 8. 새 세션 시작 예시

```text
docs/superpowers/handoffs/2026-09-29-combat-view-buffer-local-session-handoff.md 부터 읽어.
CombatViewBufferTests 결과는 ○○였어. 이어서 전투 Flow 새 설계 시작하자.
```
