# Task 4A — 전투 결과 최소 계약과 실제 조회 등록

이 문서는 [사용자 승인 답변](2026-09-30-combat-core-response-to-view.md)과 [이전 계획 Task 4A](../plans/2026-09-29-character-buffer-effect-migration-plan.md)의 Core 제공 범위를 정리한다. View가 Flow·Plan·Job과 호출 연결을 담당하고, Core가 규칙·처리 서비스·결과를 담당한다는 합의는 유지한다. 사용자가 View에 전달할 문서이며, View의 수신·동의나 후속 구현 완료를 주장하지 않는다.

## 1. 이번에 구현한 API

값 타입의 위치와 네임스페이스:

- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatResult.cs`
- `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat`
- 어셈블리: `ProjectL.Core.Contracts` — Framework/Unity 참조 없음.

```csharp
public enum CombatProcessingKind { Command = 1, Advance = 2 }

public sealed class CombatResult
{
    public int Sequence { get; }
    public CombatProcessingKind Kind { get; }
    public int CommandNumber { get; }
    public bool Accepted { get; }
    public string Reason { get; }

    public CombatResult(int sequence, CombatProcessingKind kind, int commandNumber,
        bool accepted, string reason);
}
```

`Sequence`는 양수다. `Command`는 양수 입력 번호를 갖고, `Advance`는 `CommandNumber == 0`이다. 두 번호가 같거나 연속일 필요는 없다. 수락은 `Accepted == true`, `Reason == ""`; 거절은 `Accepted == false`, 비어 있지 않은 사유 코드다. null 사유는 허용하지 않는다. 사유는 번역·연출용 문구가 아닌 Core 식별 코드다. 예를 들어 테스트의 `Attack.InsufficientActionCost`는 형태를 설명하는 예시이며, 전체 사유 코드 목록과 기존 실패 enum의 매핑은 실제 처리 구현 때 제공한다. 현재 세부 규칙을 대신할 포괄적인 실패 enum은 추가하지 않았다.

읽기 계약은 Systems에 있다:

- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs`
- `ProjectL.Systems.RuntimeSystem.BufferSystems.Combat`
- 어셈블리: `ProjectL.Systems`

```csharp
public interface ICombatResultReadSink : IServiceModule
{
    bool TryGetLatest(out CombatResult result);
}
```

조회 반환값 `false`는 **결과 없음**이며 out은 null이다. `true`는 **결과 있음**이다. 게임 행동의 수락 여부는 `result.Accepted`로 별도 확인한다. 거절도 처리 결과이므로, 조회 반환값과 수락 여부를 혼동하지 않는다. View의 Flow 테스트는 이 인터페이스의 테스트용 구현에 수락/거절 값을 제공할 수 있다.

## 2. 실제 등록 경로와 현재 한계

기존 `CombatStateBuffer` 인스턴스가 상태 조회, 내부 저장 계약, 새 결과 조회를 함께 구현·Export한다. 별도 결과 버퍼나 더미 조회 서비스를 만들지 않았다.

```text
BufferSystemFactory.Create()
  → CombatStateBuffer.ExportServices(builder)
  → CombatStateBufferKey.Results : SystemGroupKey<ICombatResultReadSink>
  → BufferSystemGroup / IBufferQuery.CombatResultReadSink
```

Systems의 기존 바인딩 경로에서는 다음처럼 얻는다:

```csharp
var buffers = resolver.GetService(BufferSystemKey.Buffer);
ICombatResultReadSink results = buffers.CombatResultReadSink;
```

`IBufferQuery`와 키는 기존처럼 Systems 내부 계약이다. 외부 어셈블리에서 내부 키를 직접 조회하도록 공개 범위를 넓히지 않았다. 새 서비스가 독립 루트에 등록됐다고 가정하지 않는다. Factory/Framework 공용 구현을 바꾸지 않고 기존 leaf Export와 그룹 노출에 연결했다.

전투 진입 전에도 씬이나 처리기를 필요로 하지 않고 조회 가능하다. 현재는 저장소의 최신 결과 슬롯이 비어 있다. `PrepareBattle`, View 확인, 입력 Clear, `EndBattle`이 성공/실패 결과를 임의로 생성하지 않는다. **명령·자동 진행의 실제 게시 경로는 아직 없다.** 따라서 지금 생산 조회가 항상 결과 없음인 것은 현재 처리 기능의 제공 범위이며, 실제 전투 실행 완료를 뜻하지 않는다.

## 3. 후속 처리 API 제안 — 아직 구현/View 합의 전

기존 `CoreGroup.Combat`의 `ICombatProcessingService`를 확장하는 다음 형태를 제안한다. 현재 인터페이스에 아래 메서드는 없으며, View가 이 이름으로 생산 Job을 먼저 연결해서는 안 된다.

| 제안 멤버 | 정상 true | 정상 false |
|---|---|---|
| `bool TryProcessCommand()` | 미처리 명령 하나의 수락 또는 규칙상 거절을 최종 기록 | 읽을 수 있는 미처리 명령 없음 |
| `bool TryProcessPreview()` | 현재 요청 하나에 대응한 미리보기 응답 기록 | 요청 없음, 이미 응답했거나 처리 대상에서 사라짐 |
| `bool TryAdvance()` | Automatic 국면의 규칙 단위 한 번을 처리하고 최종 결과 기록 | 진행할 수 있는 Automatic 작업 없음 |

모두 동기 호출 제안이다. bool은 작업 처리 여부이며 행동 수락 여부가 아니다. false에서는 처리 번호·상태 변경 결과 순번·게임 상태를 변경하지 않고, 가짜 거절/성공 결과를 발행하지 않는다. 예상 밖 예외는 전파하며 정상 거절로 바꾸지 않는다. 세부 입력 유효성·국면별 거절 사유와 자동 한 단위의 범위는 실제 계산 구현에 맞춰 확정한다.

명령은 PlayerInput에서 요청하고, 실제 규칙의 최종 상태에 따라 PlayerInput 유지, Automatic 또는 Finished 전환을 게시한다. 자동 처리는 Automatic에서 한 단위 처리 후 다음 규칙이 요구하는 국면으로 전환한다. 미리보기는 게임 상태·국면·상태 변경 결과 순번을 바꾸지 않는다. 준비는 현재 구현처럼 초기 PlayerInput 상태를 설치한다. 여기서 새 게임 규칙을 정하지 않는다.

**현재 Flow와의 조정 지점:** 현재 `CombatFlow.TryConfirmPending`은 명령 번호가 기대값에 못 미치면 정지한다. 정상 false를 허용하려면 View가 Job의 처리 여부를 Flow에 전달하거나, 입력/국면이 실행까지 유지되는 불변 조건을 보장해 정상 no-input이 발생하지 않도록 해야 한다. 단순히 번호 정지 검사를 없애면 잘못된 미게시도 감지하지 못한다. View가 원하는 방식을 확인한 후 호출 계약과 완료 확인을 함께 연결한다. 이번 작업은 Flow·Job에 이 조정을 구현하지 않았다.

## 4. 번호·결과 수명 기준

- 입력 번호는 CombatViewBuffer에서 명령/미리보기가 함께 발급한다. 번호의 간격은 정상이다. Core는 미처리 입력을 하나 선택하고, View는 Core가 처리한 번호를 보고 입력을 제거한다.
- 상태 변경 결과 순번은 최종 명령/자동 진행마다 1 증가한다. 규칙상 거절도 포함한다. 자동 진행의 원본 명령 번호는 0이다. 초기 0은 아직 처리/게시한 항목 없음이다.
- 미리보기 응답 번호는 별도다. 교체된 미리보기의 옛 응답을 현재 요청의 응답으로 쓰지 않는다. 교체·제거된 요청을 기다리지 않는 현재 Flow 동작을 보존한다.
- 입력 발급 번호와 기존 처리/응답/결과 카운터는 ordinary EndBattle로 되감지 않는다. 후속 전투는 이전 번호를 기준으로 새 입력을 구분한다.
- Runner → View → Flow 순서를 고려해, View의 `AcknowledgePresented`는 Core 최신 결과를 삭제하지 않는다. 최신 결과는 Flow도 읽을 수 있어야 한다. 한 개의 최신 슬롯을 사용하고 무제한 결과 이력은 두지 않는다.
- 후속 게시에서 현재 결과의 필요한 소비가 끝나기 전에 새 결과로 덮어쓰지 않도록 View의 outstanding/presentation 확인과 Core 게시를 연결한다. 결과 없음과 거절 모두 별도로 확인한다.
- 마지막 결과 정리, 새 전투에서 옛 결과 재생 방지와 EndBattle/전환 호출자는 F2/F3 수명 합의 및 Tasks 7–8에서 검증한다. 이번 빈 저장소 테스트로 실제 결과의 보존·교체를 검증했다고 주장하지 않는다.

## 5. View에 남은 확인 사항

1. 위 처리 멤버/반환 제안과 no-input 처리 방식: Job 처리 여부를 Flow에 전달할지, 실행 시점까지 입력 유지로 보장할지.
2. R7: 준비 인덱스/리비전으로 해결하려는 실제 사용 사례가 무엇인지. 같은 준비 반복 감지인지, 다음 전투 초기화 감지인지에 따라 번호 범위를 정해야 한다.
3. R8: 표시용 blocked-cell 원본의 실제 입력 경로가 무엇인지. Core는 기존 지형/설치 금지 규칙에서 읽을 값을 제공하고, 임의의 지형이나 새로운 금지 규칙을 만들지 않는다.
4. F2: 마지막 결과 소비 후 정리/전환 순서와 호출자. 자발적 포기·로비 복귀는 패배이며, 실제 최종 결과를 처리·소비한 뒤 정리한다.

R7/R8과 위 처리 멤버 협의는 제공된 최소 F3 타입·조회 등록을 사용하는 테스트를 막지 않는다. 실제 자동 진행/연출/전환의 생산 연결은 실제 게시 경로와 필요한 F1/F2/G7/G9가 준비된 후 검증한다.

## 6. 검증 및 제공 단계

Unity 6000.0.73f1 batch EditMode에서 다음 세 fixture를 함께 실행해 **21개 통과, 실패/건너뜀 0개**를 확인했다:

- `ProjectL.Tests.Core.CombatResultContractTests`: 15개 — 수락/거절, 원본 번호 간격, 자동 번호 0, 잘못된 메타데이터 거절, 불변성.
- `ProjectL.Tests.Framework.CombatResultReadBindingTests`: 2개 — 실제 leaf export/key와 Factory/group의 같은 저장소 조회, 전투 전/준비/종료의 결과 없음.
- `ProjectL.Tests.Framework.CombatStateBufferTests`: 4개 — 기존 준비/원자성 회귀.

테스트를 먼저 추가한 RED 실행은 예상한 미구현 타입·키·그룹 속성의 컴파일 오류를 확인했다. 구현 후 위 필터로 GREEN을 확인했다. Unity가 새 C# 파일의 `.meta`를 생성했다. 최초 샌드박스 실행은 Unity 사용자 캐시 DB 접근에서 종료돼 RED 근거에서 제외했으며, 필요한 권한으로 실행한 뒤 검증했다.

검증 원본은 Git에 넣지 않는 `.superpowers/sdd/2026-09-29-character-buffer-effect-migration-plan/task4a/`의 `red-unity.log`, `green-unity.log`, `green-results.xml`에 있다. 전체 테스트/PlayMode·실제 전투 구동은 이번 검증에 포함하지 않는다.

독립 리뷰는 변경 코드·테스트·인계의 범위와 실제 저장소 연결을 확인했으며 조치가 필요한 문제를 발견하지 않았다. 이는 정적 리뷰이고, 위 Unity 실행 증거와 구분한다.

최소 타입과 실제 빈 저장소 조회 등록은 제공 완료다. 실제 게임 처리 게시, 발행된 결과의 확인 이후 보존/교체, View 생산 소비는 후속 작업이다. Flow/Plan/Job 구현 소유권은 View에 유지한다.
