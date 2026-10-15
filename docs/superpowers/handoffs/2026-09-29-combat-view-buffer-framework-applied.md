# CombatViewBuffer — Framework 공용 파일 반영 결과

- 작성일: 2026-09-29
- 작성: View 작업(스테이지 내부 담당)
- 전달 대상: Framework 작업자(민형님)
- 이 문서는 [먼저 보낸 변경 예정 공유](2026-09-29-combat-view-buffer-framework-share.md)의 **후속**이다. 예정했던 5곳 중 3곳은 반영했고, 2곳은 이번 작업에서 뺐다.

관련 문서

- [CombatViewBuffer 설계](../specs/2026-09-29-combat-view-buffer-design.md)
- [구현 계획](../plans/2026-09-29-combat-view-buffer-plan.md)
- [Core 측 요청](2026-09-29-combat-view-buffer-core-requests.md)

## 1. 바뀐 범위

처음에는 전투 입력 Buffer와 전투 Flow(`CombatLifeCycle`·`CombatPlan`·전투 Job)를 한 작업으로 계획했다. 이후 둘을 나눴다.

- **이번 작업:** `CombatViewBuffer`만. 완료했다.
- **전투 Flow:** 별도 설계로 처음부터 다시 쓴다. Flow에 필요한 Framework 변경은 그 설계에서 다시 공유한다.

## 2. 반영한 공용 파일 (3곳)

| 파일 | 변경 | 커밋 |
|---|---|---|
| `Systems/ServiceKeys/DataBaseGroupServiceKey.cs` | `using …BufferSystems.CombatView;` 한 줄, `internal static class CombatViewBufferKey { Read; Write; }`. `CurrentProgressBufferKey` 바로 아래 | `22ba69fc` |
| `Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs` | `using` 한 줄, `new CombatViewBuffer().ExportServices(builder);` 한 줄. 기존 Buffer들 다음 | `22ba69fc` |
| `Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs` | `using` 한 줄. `IBufferQuery`와 구현에 `CombatViewReadSink`·`CombatViewWriteSink` 속성, 생성자에서 조회하는 두 줄. **`ResetRunProgress`는 변경하지 않았다**(전투 수명 데이터라 Run 초기화 범위가 아니다) | `22ba69fc` |

- 서비스 저장소가 인터페이스 타입으로 등록하므로, 키 이름 `Read`/`Write`가 다른 Buffer와 같아도 충돌하지 않는다.
- `IBufferQuery`를 직접 구현하는 곳은 `BufferSystemGroup` 하나뿐이다(테스트 대역 없음). 그래서 멤버를 추가해도 깨지는 코드가 없다.
- 에디터 컴파일 에러는 0개였다.

새 파일은 모두 `Systems/RuntimeSystem/BufferSystems/CombatView/` 아래에 있다(값 타입 4개, Buffer 1개).

## 3. 이번 작업에서 뺀 공용 파일 (2곳)

| 파일 | 처음 예정했던 변경 | 지금 |
|---|---|---|
| `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `JobType`에 전투용 값 3개 추가 | **변경하지 않았다.** 전투 Flow 설계에서 다시 정한다 |
| `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | `_plans[JobRootType.Combat]`을 람다로 바꿔 Buffer 전달 | **변경하지 않았다.** 위와 같다 |

## 4. 테스트

`Assets/Tests/EditMode/Framework/CombatViewBufferTests.cs`를 기존 `ProjectL.Tests.Framework` 어셈블리에 추가했다. 새 asmdef는 없다. `MapProgressBufferTests`처럼 `BufferSystemFactory.Create()`로 만들어 `IBufferQuery`에서 꺼낸다.

확인하는 항목은 다음과 같다.
- 등록
- 번호 발급과 `Clear` 뒤에도 번호가 이어지는지
- 다음 명령 찾기와 지우기 범위
- 미리보기 교체
- 연출 확인 값이 줄어들 때 거절하는지
- 읽기 전용 목록
- 값 형식 검사

## 5. 아직 여쭙고 싶은 것

- **D1 (그대로 유효):** `IBufferQuery`가 모든 Buffer의 ReadSink와 WriteSink를 함께 노출한다. 이번에도 기존 방식을 따랐다. View용과 Core용 조회 인터페이스를 나눠 쓰기 권한을 코드로 막을 생각이 있으신지 여쭙는다.
- **D7 (전투 Flow로 넘어감):** 전투를 나갈 때 `IJobRunner.Clear()`를 호출해도 되는지는 전투 Flow 설계에서 다시 여쭙겠다. 먼저 의견을 주셔도 좋다.
