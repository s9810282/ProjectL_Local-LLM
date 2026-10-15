# Stage·Run 이전에 따른 Buffer·Plan·Job 변경 검토 요청

- 작성일: 2026-09-29
- 대상: 프레임워크 개발자
- 목적: Core 진행 기능을 기존 프레임워크에 연결한 방식이 책임 분리와 서비스 연결 규칙에 맞는지 확인받기 위한 문서입니다.
- 이 문서는 코드 비교와 작업 중 합의를 정리한 것이며, 프레임워크 개발자의 검토가 끝났다는 의미는 아닙니다.

## 1. 비교 기준과 먼저 확인받고 싶은 내용

| 구분 | 커밋 |
|---|---|
| 이전 기준 | `ac3e3e35621c0257ba5ca46bea4bb9372ef16605` |
| Stage·Run 연결 및 초기화 추가 기준 | `cfda31733597cc9ea2106f4e092154284233be21` |
| 문서 작성 시 현재 코드 | `019a9c2df8f81b577f0ed05bbd41ebfd69f3e2bb` |

주요 설명은 첫 번째와 두 번째 커밋의 차이를 기준으로 합니다. 현재 코드도 함께 확인했습니다. 두 번째 커밋 이후 런타임 스크립트 차이는 `MapProgressBuffer`, `StageProgressBuffer`의 줄 간격 정리이며, 아래 설명하는 기능은 동일합니다. 이후 추가된 테스트 결과는 별도로 구분합니다.

**핵심 검토 요청은 다음 네 가지입니다.**

1. 여러 진행 Buffer의 초기화 범위를 `BufferSystemGroup.ResetRunProgress()`에 모으는 것이 맞는지 확인 부탁드립니다.
2. `JobRegisterSystem`이 기존 `PostBindService`에서 Buffer 서비스를 받아 Stage Plan에 전달하는 것이 맞는지 확인 부탁드립니다.
3. Stage Plan이 초기화 Job과 맵 준비 Job을 구성하고, Job이 Buffer 공개 API와 Core 진행 함수를 호출하는 방식이 맞는지 확인 부탁드립니다.
4. 일반 맵 준비와 새 Run 시작을 `Start` / `NewRun`으로 구분한 것이 기존 실행 흐름에 맞는지 확인 부탁드립니다.

## 2. 참고한 계약과 이후 추가된 요구사항

### 2.1 프레임워크 문서와 전달받은 계약

다음 자료를 기준으로 코드와 책임을 확인했습니다.

- [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md): 서비스 등록, Factory 조립, `InitData`·`PostBindService`, 서비스 조회 경로
- [Framework 구조 설명](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md): Plan·Job·Runner의 관계와 실행 방식
- 전달받은 `계약 확인 요약.txt`와 채팅에 붙여주신 확정 계약
- `2026-09-26-db-so-develop-merge-core-notice.md`: DB-SO 쪽 Stage 데이터 변경과 전투 View용 Buffer 후속 논의

전달받은 핵심 원칙은 다음과 같습니다.

- Buffer가 런타임 진행 데이터의 원본입니다.
- Core는 Buffer 상태를 바탕으로 규칙을 판단하며, 별도 맵 진행 상태를 중복 보관하지 않습니다.
- View는 명령을 보내고 Buffer를 읽습니다. DB 조회나 Core Snapshot 조립을 View에 맡기지 않습니다.
- `Core.Application` / `Core.Domain`은 Systems의 Buffer를 직접 참조하지 않습니다.
- Buffer 연결은 Systems의 Core 처리 계층에서 `PostBindService`를 통해 수행합니다.
- Buffer가 초기화 대상과 기록 보존 정책을 책임집니다.
- Plan·Job은 게임 작업의 실행 순서를 담당합니다.
- 요구되지 않은 Run ID·버전·락·트랜잭션·과거 요청 차단 체계를 먼저 추가하지 않습니다.

### 2.2 초기 계약 이후 사용자가 확정한 재시작 요구

최초 전달 문서에는 Run 재시작이 범위 밖으로 적혀 있었습니다. 이후 대화에서 다음 요구가 추가됐습니다. 이번 초기화 API는 이 후속 요구를 반영한 것입니다.

- 패배하면 챕터 1부터 새 Run을 시작합니다.
- 스테이지 맵을 다시 생성합니다.
- 첫 스테이지에 자동 진입하지 않고, 플레이어가 첫 노드를 선택합니다.
- 영구 Artifact 보유 기록은 유지합니다.
- 일반적인 맵 복귀나 챕터 진입에서는 진행 기록을 지우지 않습니다.

따라서 **기존 계약의 상태 소유 원칙을 유지하면서, 명시적인 새 Run 초기화 기능을 추가**했습니다. 사용자에게 Buffer API 추가와 좁은 Plan·Job 연결 변경을 승인받았지만, 이것을 프레임워크 개발자의 구조 검토 승인으로 간주하지는 않습니다.

## 3. 실제 변경 파일

아래 경로는 `Assets/ProjectL/Script/Systems/`를 기준으로 합니다.

| 파일 | 이전 상태 | 반영한 변경 |
|---|---|---|
| `RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs` | 개별 Buffer의 Read/WriteSink 제공 | `IRunProgressBufferWriteSink`와 `ResetRunProgress()` 추가, 기존 `IBufferQuery`에 접근자 추가 |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | InGame 서비스를 바인딩하고 Plan에 전달 | Buffer도 바인딩해 Stage Plan에 전달, 종료 시 참조 해제 |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `JobType.None = 0`, `Start = 1` | `NewRun = 2` 추가. 기존 값과 `JobRootType`은 유지 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs` | `Start`에서 빈 StageJob 하나 등록 | `Start`에서 맵 준비, `NewRun`에서 초기화 후 맵 준비 Job 구성 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageJob.cs` | 서비스와 우선순위를 받고, 실제 작업 없이 완료 표시 | 현재 챕터를 읽어 Core 맵 준비 호출. 같은 파일에 `ResetRunProgressJob` 추가 |

**Buffer 영역에서 위 비교 구간의 기능 변경은 `BufferSystemGroup.cs`에만 있습니다.** `CurrentProgressBuffer`, `StageProgressBuffer`, `MapProgressBuffer`의 기존 데이터 필드나 개별 공개 API를 새로 설계하지 않았습니다.

Core 연결을 위해 추가·수정한 관련 파일도 있습니다.

- `ChapterProgressionCoordinator.cs`: Buffer 조회 → Core 계산 → Buffer 기록을 연결하는 Systems 서비스
- `InGameCoreSystemFactory.cs`, `InGameCoreSystemGroup.cs`, `SystemGroupsService.cs`: 위 서비스의 생성·등록·조회 연결
- `ProjectL.Systems.asmdef`: Core.Application 호출을 위한 단방향 참조 추가
- `StageServiceManager.cs`: 기존 `Enter(stageKey)` 유지, 내부 DB 조회를 재사용하는 `IsBossStage()`와 실행 없는 `ValidateEntry()` 추가

`StagePlan`, `StageJob`, `StageServiceManager`는 원래 프레임워크에 있던 골격을 활용했습니다. 새로 만든 별도 실행 프레임워크가 아닙니다. 다만 StageJob 생성자와 StagePlan 내부 함수에 서비스 인자가 추가됐으므로, 소스 변경 전체가 단순한 코드 추가만인 것은 아닙니다.

## 4. Buffer 초기화 API와 판단 근거

### 4.1 추가한 공개 기능

```csharp
public interface IRunProgressBufferWriteSink : IServiceModule
{
    void ResetRunProgress();
}
```

기존 내부 조회 계약인 `IBufferQuery`에 다음 접근자를 추가했습니다.

```csharp
IRunProgressBufferWriteSink RunProgressWriteSink { get; }
```

`BufferSystemGroup`이 이 인터페이스를 직접 구현하고, 접근자는 `this`를 반환합니다. 새로운 상태 Buffer, 별도 Factory, 별도 루트 서비스 키는 만들지 않았습니다. 기존 `IBufferQuery` 등록을 통해 접근합니다. `IRunProgressBufferWriteSink`를 루트에 별도 등록한 것은 아닙니다.

### 4.2 무엇을 지우고 무엇을 남기는가

| 대상 | 새 Run 처리 | 이유 |
|---|---|---|
| Current | `Clear()` | 현재 챕터와 선택/진입 스테이지 제거 |
| Stage | `Clear()` | 모든 챕터의 생성된 맵과 클리어 기록 제거, 이후 새로 생성 |
| Map | 등록된 각 맵에 `SetClear(key, false)` | 챕터 완료 기록 초기화, DB에서 준비한 맵 등록은 유지 |
| Character | `Clear()` | 현재 Buffer에 있는 임시 캐릭터 기록 초기화 |
| Currency | `Clear()` | 현재 Buffer에 있는 임시 재화 초기화 |
| Artifact | 변경 없음 | 영구 보유 기록 유지 |

`MapProgressBuffer.Clear()`는 맵 등록 자체를 지우므로 사용하지 않았습니다. 맵 등록은 DB 초기화 단계에서 준비되며, 새 Run마다 Framework의 `InitData()`를 다시 호출하는 구조를 만들지 않기 위해 등록을 유지했습니다.

현재 구현은 맵 키를 복사한 뒤 Current → Stage → Map 완료 값 → Character → Currency 순서로 처리합니다. 키 복사는 완료 값을 변경하는 동안 같은 맵 컬렉션을 열거하지 않기 위한 것입니다.

이 내부 호출 순서는 초기화 API의 구현 상세입니다. **전투 정리 → 초기화 → 맵 생성 → 화면 전환 같은 게임 작업 순서는 Buffer가 결정하지 않습니다.**

### 4.3 이 위치를 선택한 이유와 한계

Core나 Job에서 개별 Buffer의 `Clear()`를 직접 나열하면, 외부 호출자가 초기화 정책을 소유하게 됩니다. 그래서 Job은 `ResetRunProgress()` 한 번만 호출하고, 초기화 범위는 Buffer 쪽에 모았습니다.

다만 여러 Buffer를 묶는 이 정책을 `BufferSystemGroup`이 직접 구현하는 것이 기존 그룹의 역할에 맞는지는 이번 검토의 핵심 질문입니다. 별도 관리 계층이 필요하다고 미리 가정하지 않았습니다.

- 승패 필드, 패배 이력, Run ID·버전, 영구 종료 상태는 추가하지 않았습니다.
- 초기화 함수는 맵 생성·전투 정리·씬 전환을 하지 않습니다.
- Character/Artifact 데이터는 현재 단순한 기록 형태입니다. 기존 전투 객체·덱·체력·Artifact 효과 자원의 정리까지 구현했다고 볼 수 없습니다.
- 새 Run 요청을 다시 등록하면 다시 초기화됩니다. 여러 요청을 하나로 묶거나 무시하는 기능은 없습니다.

## 5. Register·Plan·Job을 연결한 방법

### 5.1 Register의 공개 계약은 유지했습니다

```csharp
void Register(JobRootType rootType, JobType jobType);
```

기존처럼 루트별 Plan을 찾고, 반환받은 Job 목록을 Runner에 등록합니다. 챕터 번호, 맵 규칙, 승패 판단은 Register에 넣지 않았습니다.

추가된 부분은 서비스 전달입니다.

```csharp
// PostBindService에서 실행한다. 여기서 초기화 작업을 실행하지는 않는다.
_inGameService = resolver.GetService(SystemGroupsService.InGame);
_buffers = resolver.GetService(BufferSystemKey.Buffer);

// Initialize에서 Stage Plan의 필요한 서비스를 연결한다.
_plans[JobRootType.Stage] =
    (service, jobType) => StagePlan.Register(service, jobType, _buffers);
```

나머지 Lobby·Combat·Artifact Plan 등록 방식은 유지했습니다. `Shutdown()`에서는 `_buffers` 참조도 해제합니다.

**구성 의존성은 바뀌었습니다.** 이제 JobRegister의 `PostBindService()`는 Buffer 서비스도 요구합니다. 기존 기본 루트에는 Buffer가 등록돼 있지만, Job과 InGame만 따로 조립하던 테스트나 외부 구성에는 Buffer가 추가로 필요합니다. 이 영향을 숨기지 않고 검토받으려 합니다.

### 5.2 StagePlan의 기존 switch를 유지했습니다

```csharp
switch (jobType)
{
    case JobType.None:
        break;
    case JobType.NewRun:
        jobs.Add(new ResetRunProgressJob(buffers.RunProgressWriteSink, 0));
        jobs.Add(new StageJob(service, 1, buffers.CurrentProgressReadSink));
        break;
    case JobType.Start:
        jobs.Add(new StageJob(service, 0, buffers.CurrentProgressReadSink));
        break;
    default:
        throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown Stage job type.");
}
```

기존 Runner에서 작은 `Priority`가 먼저 실행되므로 새 Run에서는 0번 초기화 후 1번 맵 준비를 수행하도록 했습니다. `JobType` enum의 숫자는 작업 종류이며, 실행 우선순위가 아닙니다.

Runner·Queue의 코드는 변경하지 않았습니다. 전체 공용 큐에서 다른 Job도 실행될 수 있으므로, 두 Job이 다른 작업과 절대 섞이지 않는 원자적 실행 묶음이라고 보장하지는 않습니다. 실제 전투 정리와 재시작 요청 시점은 전투 이전 후 연결해야 합니다.

### 5.3 두 Job의 역할

`ResetRunProgressJob.Execute()`는 주입받은 `IRunProgressBufferWriteSink.ResetRunProgress()`만 호출하고 완료합니다. 개별 Buffer의 초기화 범위를 알지 않습니다.

`StageJob.Execute()`의 주요 코드는 다음과 같습니다.

```csharp
if (IsCompleted) return;

int chapterKey = _current.HasMap ? _current.CurrentMapKey : 1;
var result = _service.CoreGroup.ChapterProgression.EnterChapter(chapterKey);
if (!result.Accepted)
    throw new InvalidOperationException($"Chapter entry rejected: {result.FailureReason}.");

IsCompleted = true;
```

현재 챕터를 Job 등록 시 복사하지 않고 **실행 시점에 Buffer에서 읽습니다.** 그래야 앞선 초기화 Job이 Current를 비운 뒤 챕터 1을 준비할 수 있습니다. 현재 챕터가 있으면 그 챕터를 대상으로 준비합니다.

`1`은 사용자와 합의한 시작 챕터를 코드에서 키 1로 표현한 것입니다. 실제 DB 키가 다른 규칙이라면 이 부분은 조정해야 합니다. 다음 챕터 해금·선택 정책은 추가하지 않았습니다.

두 Job의 `IsCompleted` 검사는 동일 Job 인스턴스의 재실행을 막습니다. 매번 새로 등록되는 Job까지 중복 요청으로 판정하는 장치는 아닙니다.

## 6. 실제 호출 흐름과 상태 기록

### 새 Run 준비

```text
호출자: Register(Stage, NewRun)
 → Register가 StagePlan의 Job 목록을 Runner에 등록
 → Tick에서 ResetRunProgressJob 실행
 → BufferSystemGroup.ResetRunProgress()
 → StageJob 실행: Current가 비어 있으므로 챕터 1 선택
 → ChapterProgressionCoordinator.EnterChapter(1)
 → Core 규칙 확인, 그래프가 없으면 기존 Generator로 생성
 → StageProgressBuffer에 생성 결과 기록
 → CurrentProgressBuffer.SelectMap(1)
 → 스테이지 미선택 상태로 첫 노드 선택 대기
```

`InitData()`는 DB Query를 받는 준비 단계이고, 실제 생성은 이후 `Generate(mapKey)` 호출에서 수행합니다. 일반 `Start`는 초기화 Job을 넣지 않습니다. 맵 생성 알고리즘과 난수 설정은 기존 Generator/DB 설정을 재사용하며, 재생성이 항상 다른 모양을 보장하는 것은 아닙니다. 무작위 재생성에는 기존 `RandomizeSeedEachRun` 설정이 필요합니다.

### 노드 선택과 결과 기록

```text
Systems의 ChapterProgressionCoordinator
 → 기존 PostBindService로 받은 Buffer 읽기
 → Core.Contracts 값으로 변환
 → Core.Application → Core.Domain에서 규칙 판단
 → 승인된 결과만 기존 Buffer 공개 API에 반영
```

- `PrepareStageEntry()`는 진행 규칙과 DB 데이터를 확인하지만 Current를 기록하지 않습니다.
- `ConfirmStageEntry()`는 실제 준비가 성공한 호출자가 사용하도록 분리했습니다. 다시 검사한 뒤 기존 `SelectStage()`를 호출합니다. 현재 임시 표시명은 `Stage {key}`입니다.
- `ResolveStage()`는 전달받은 결과를 규칙에 적용합니다. 일반 승리는 Stage 클리어, 보스 승리는 Stage와 Map 클리어를 기록합니다.
- 패배는 반환 결과의 `EndAttempt`로 전달합니다. Buffer에 패배를 저장하거나, 이 함수가 자동으로 초기화하지는 않습니다.

`ConfirmStageEntry()`는 실제 준비 완료를 스스로 감지하지 않습니다. `ResolveStage()`도 실제 전투의 승패를 계산하지 않습니다. 두 함수의 실제 전투 호출자는 아직 연결하지 않았습니다.

일반 진행 상태는 Buffer가 원본이며, Core 계산에 전달하는 배열은 한 명령의 임시 입력입니다. View에 별도 Core 진행 Snapshot을 공급하지 않습니다. 기존 독립 Core의 `StageMapSession` 등은 기존 Run/전투 이전과 함께 정리할 대상이며, 이번 Buffer 경로에는 연결하지 않았습니다.

## 7. 진행 중 시도했지만 최종 코드에서 제외한 내용

| 중간 시도 | 최종 정리와 이유 |
|---|---|
| Core에서 Current·Stage·Map을 직접 초기화하는 함수 | 제거했습니다. 초기화 범위는 Buffer가 결정해야 하기 때문입니다. |
| Register에 챕터 번호를 받는 overload 추가 | 제거했습니다. 기존 두 인자 Register를 유지하고 현재 챕터는 Buffer에서 읽습니다. |
| Register에서 맵 존재 여부 조회·Runner 상태에 따른 재시작 허용 검사 | 제거했습니다. 공용 등록기가 진행 정책을 판단하지 않도록 했습니다. |
| Stage의 아무 작업도 하지 않는 호환용 경로 유지 | 별도 경로는 제거하고 기존 Stage Plan·Job을 실제 맵 준비 기능으로 채웠습니다. |
| `None`·`default` 처리를 switch 밖으로 이동 | 기존 코드 스타일에 맞춰 switch 안으로 되돌렸습니다. |
| StageJob이 `ChapterCompleted` 거절을 정상 종료로 취급 | 완료 맵 복귀가 확정된 요구가 아니므로 제거했습니다. 현재는 거절 결과에 예외를 발생시킵니다. |

위 중간 시도 중 일부는 미커밋 상태에서 수정·철회됐으므로, 지정한 두 커밋의 diff에 모두 나타나지는 않습니다. 대화상 판단 이력을 설명하기 위한 표입니다.

## 8. 이번 변경으로 완료했다고 보지 않는 부분

- 전투 Buffer와 전투 Core 상태 이전
- 실제 게임 시작·노드 입력·승패·전투 정리에서 위 API를 호출하는 연결
- View 화면 구현과 씬 전환, 로딩 완료 처리
- 보스 클리어 후 다음 챕터를 선택·해금·전환하는 실행 경로
- 이전 Core의 전투 객체·덱·체력·Artifact 효과 자원 정리
- 예외 발생 후 Runner 복구, 전투 비동기 완료와 재시작의 순서 조율

`JobRunner`, `JobQueue`, `LifeCycleBase`/Manager, `StageLifeCycle`, Combat Plan·Job, View, 공용 Framework 어셈블리의 실행 구조는 이 비교 구간에서 변경하지 않았습니다.

DB-SO 별도 작업에서 안내한 `StageProgressData.StageType/MapRole` 추가는 이번 초기화 변경과 별개입니다. 비교 대상 코드에는 해당 필드가 없으며, 보스 여부는 Systems의 StageServiceManager가 DB에서 조회합니다. 후속 DB-SO 통합 때 같은 필드·검증을 중복 구현하지 않도록 확인해야 합니다.

## 9. 검증 결과와 한계

아래는 기존 작업에서 얻은 결과이며, 이 문서를 작성하면서 새로 실행한 테스트 결과는 아닙니다.

| 시점/범위 | 결과 | 해석 |
|---|---|---|
| 초기화·Stage Job 연결 검증 | 39/39 통과 | 개별 Job, Buffer, Core 조립 검증 |
| 독립 진행 보강 및 완료 챕터 예외 조건 철회 후 관련 재검증 | 4/4 통과 | 세 알고리즘의 챕터 진행과 명시적 재시작 검증 |
| 병합 전 `7fac54b8` 기준, Stage·Run 43건 + 기존 Run/Artifact 39건 | 총 82건 중 81 통과, 1 실패 | Stage·Run 묶음은 통과. 아래 기존 Run 테스트 실패는 미해결 |

남은 실패는 `InvalidActivation_LeavesRunAndMapUnchanged_AndValidPreparationCanRetry("disposed", InvalidStage)`에서 기대값 `InvalidStage` 대신 `ArtifactLoadoutUnavailable`이 반환되는 건입니다. 프레임워크 변경으로 새로 생긴 문제인지에 대한 기준 브랜치 비교 판정은 하지 않았습니다.

독립 진행 테스트는 실제 Buffer·InGame·Job Factory를 테스트용 Runtime에 조립하고 Tick으로 실행했습니다. 전투 준비 성공과 승패는 테스트에서 입력했습니다. 실제 전투를 실행한 검증은 아닙니다.

전체 루트 Factory 초기화 시도는 `OutlineSystem.Initialize()`의 Material Query 연결에서 실패했습니다. 렌더링 코드를 임의로 수정하지 않고 독립 진행 검증으로 범위를 한정했습니다. 따라서 전체 루트·씬·실제 플레이가 정상이라는 결론은 아닙니다.

## 10. 프레임워크 개발자 확인 요청

| 번호 | 확인할 사항 | 현재 구현/판단 | 검토 의견 |
|---|---|---|---|
| 1 | 초기화 정책의 소유 위치 | BufferSystemGroup이 기존 Buffer를 묶어 초기화하고 Artifact는 보존합니다. | |
| 2 | 초기화 범위 | Current·Stage·Map 완료·Character·Currency를 초기화하며 맵 등록은 보존합니다. | |
| 3 | 초기화 API 노출 | 기존 IBufferQuery에 RunProgressWriteSink 접근자를 추가했습니다. 별도 루트 키는 없습니다. | |
| 4 | JobRegister의 서비스 의존성 | PostBindService에서 Buffer도 필수로 조회하고 Stage Plan에 전달합니다. 진행 판단은 하지 않습니다. | |
| 5 | Plan과 Job의 역할 | Plan이 순서를 정하고 Job이 Buffer API/Core 함수를 호출합니다. 필요한 읽기/쓰기 인터페이스만 Job에 주입합니다. | |
| 6 | Start / NewRun 구분 | Start는 기존 진행을 유지하며 준비하고, NewRun만 초기화 후 챕터 1을 준비합니다. 다른 루트의 NewRun 처리는 추가하지 않았습니다. | |
| 7 | 첫 챕터 결정 위치 | StageJob이 현재 챕터가 없으면 키 1을 사용합니다. 실제 DB 키와 기존 시작 흐름에 적합한지 확인이 필요합니다. | |
| 8 | Core 결과 처리 | Job이 공개 Contracts 결과를 확인하며, 거절을 정상 완료로 바꾸지 않습니다. 실패 후 흐름은 추후 연결 대상입니다. | |
| 9 | 후속 전투 연결 시점 | 전투 Buffer/상태 이전을 먼저 진행하고, 실제 진입 성공·결과·정리 완료 신호를 이후 연결할 계획입니다. | |

이 구조를 승인받기 전부터 추가 Buffer, 새 전투 관리 계층, 공용 실행기 변경을 전제하지 않겠습니다. 검토 결과에 따라 필요한 부분만 기존 코드 스타일에 맞춰 조정하려고 합니다.
