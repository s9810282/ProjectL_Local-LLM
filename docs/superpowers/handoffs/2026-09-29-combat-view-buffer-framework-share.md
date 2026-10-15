# CombatViewBuffer 작업의 Framework 공용 파일 변경 공유

- 작성일: 2026-09-29
- 작성: View 작업(스테이지 내부 담당)
- 전달 대상: Framework 작업자(민형님)
- 목적: 전투 입력 Buffer와 전투 Flow를 만들면서 Framework 공용 파일 **5곳**에 줄을 추가할 예정이다. 변경 전에 공유하고, 확인이 필요한 두 가지(D1, D7)를 여쭙는다.
- 상태: 변경 예정. 아직 코드는 바뀌지 않았다.

관련 문서

- [CombatViewBuffer 설계](../specs/2026-09-29-combat-view-buffer-design.md)
- [구현 계획](../plans/2026-09-29-combat-view-buffer-plan.md)
- [Core 측 요청](2026-09-29-combat-view-buffer-core-requests.md)

## 1. 무엇을 만드나

- **`CombatViewBuffer`**: View가 쓰고 Core 측(Flow·Job·처리 서비스)은 읽기만 하는 전투 입력 Buffer다. 실행 명령, 미리보기 요청, 연출 완료 확인을 담는다. 기존 Buffer와 같은 방식이다. ReadSink/WriteSink 쌍을 만들고 `BufferSystemFactory`에 등록하며 `IBufferQuery`로 노출한다.
- **전투 Flow**: `CombatLifeCycle.OnTick`이 Runner가 비어 있을 때 `Register(Combat, …)`를 호출한다. `CombatPlan`이 명령 처리·진행·미리보기 Job과 연출 대기 Job을 만든다.
  - 명령 내용은 `Register`로 넘길 수 없다(enum 두 개만 받는다). 그래서 Job이 Execute할 때 `CombatViewBuffer`에서 읽는다. 기존 `StageJob`이 Current Buffer를 읽는 방식과 같다.

새 파일은 `BufferSystems/CombatView/`와 `LifeCycles/Combat/` 아래에만 둔다.

## 2. 공용 파일 변경 예정

| 파일 | 변경 | 계획 Phase |
|---|---|---|
| `Systems/ServiceKeys/DataBaseGroupServiceKey.cs` | `internal static class CombatViewBufferKey { Read; Write; }` 추가. 다른 Buffer 키와 같은 모양 | 3 |
| `Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs` | `new CombatViewBuffer().ExportServices(builder);` 한 줄 | 3 |
| `Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs` | `IBufferQuery`와 구현에 `CombatViewReadSink`·`CombatViewWriteSink` 속성을 추가하고 생성자에서 조회. **`ResetRunProgress`는 변경하지 않는다.** 전투 수명 데이터라 Run 초기화 범위가 아니다 | 3 |
| `Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `JobType`에 `CombatCommand = 3`, `CombatAdvance = 4`, `CombatPreview = 5` 추가. 기존 값은 그대로 | 4 |
| `Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | `_plans[JobRootType.Combat]`을 `StagePlan`과 같은 람다로 바꿔 `_buffers`를 넘긴다. `Register`의 공개 계약은 그대로 | 4 |

```csharp
// JobRegisterSystem.Initialize — 변경 예정인 한 줄
_plans[JobRootType.Combat] = (service, jobType) => CombatPlan.Register(service, jobType, _buffers);
```

## 3. 확인 부탁드리는 것

### D1. Buffer 쓰기 권한을 코드로 막을지

`IBufferQuery`가 모든 Buffer의 ReadSink와 WriteSink를 함께 노출한다. 그래서 "View만 쓴다 / Core만 쓴다"는 규칙이 지금은 관례로만 지켜진다.

- 이번 작업은 **기존 Buffer와 똑같이** `IBufferQuery`에 둘 다 올릴 예정이다.
- 코드로 막으려면 View용과 Core용 조회 인터페이스를 나눠야 한다. 이건 모든 Buffer에 걸리는 일이라 Framework 쪽 판단이 필요하다. 나눌 생각이 있으신지 여쭙는다.

### D7. 전투를 나갈 때 Runner를 비워도 되는지

연출 대기 Job은 View가 연출 완료를 기록할 때까지 끝나지 않는다. 전투를 나갈 때 이 Job이 Runner에 남아 있으면 다음 LifeCycle의 Job이 영원히 시작되지 않는다.

- 제안: `CombatLifeCycle.OnExit`에서 `IJobRunner.Clear()`를 호출한다.
- 걱정: Runner가 전역 하나라서, 전환 중에 다른 LifeCycle이 이미 등록한 Job까지 지울 수 있다. 로딩 순서상 안전한지, 또는 LifeCycle 전환 때 Runner를 비우는 공통 규칙을 Framework 쪽에 둘지 의견을 부탁드린다.
- 답을 받기 전까지 이 한 줄은 넣지 않는다.
