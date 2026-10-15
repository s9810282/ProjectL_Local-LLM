# CombatViewBuffer 구현 계획

- 작성일: 2026-09-29 (3차: Buffer 전용으로 정리)
- 설계: [CombatViewBuffer 설계](../specs/2026-09-29-combat-view-buffer-design.md)
- 상태: **Phase 0~3 완료. Phase 4(마무리) 남음.**
- 범위 변경: 2차 계획의 Phase 4(전투 Job/Plan 기반)와 Phase 5(Flow·연출 대기)는 이 계획에서 뺐다. 전투 Flow는 별도 설계·계획으로 다시 쓴다(설계 §12).

## 전역 제약

- 새 파일은 `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/`에 두고, 네임스페이스는 `ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView`다.
- 순수 C#으로 쓴다. `using UnityEngine`을 쓰지 않는다. Core는 Contracts 타입만 참조한다.
- 코드 형식은 기존 Buffer를 따른다: `#region 서비스` / `#region 시스템 모듈`, `sealed`, Allman 중괄호, 한국어 XML doc.
- Framework 공용 파일은 필요한 줄만 추가하고, 변경 목록을 민형님께 공유한다.
- Core 파일은 건드리지 않는다.

## Phase 0 — 요청 전달 ✅

| Task | 결과 |
|---|---|
| 0.1 | [Core 측 요청](../handoffs/2026-09-29-combat-view-buffer-core-requests.md)을 새 파일로 작성(R1~R6). 원래 계획은 기존 전달 문서에 덧붙이는 것이었으나 사용자 지시로 새 파일로 바꿨다 |
| 0.2 | [Framework 공유](../handoffs/2026-09-29-combat-view-buffer-framework-share.md)를 새 파일로 작성(공용 파일 5곳, D1·D7 확인 요청) |

커밋: `27f8c8f4`

## Phase 1 — 값 타입 ✅

`CombatViewCommandKind.cs`, `CombatViewCommand.cs`(형식 검사 도우미 포함), `CombatViewPreviewKind.cs`, `CombatViewPreview.cs`

커밋 `8d0c9947`, 에디터 컴파일 에러 0

## Phase 2 — Buffer 본체 ✅

`CombatViewBuffer.cs`: `ICombatViewBufferReadSink`, `ICombatViewBufferWriteSink`, `CombatViewBuffer`

커밋 `ec7152fb`, 에디터 컴파일 에러 0

## Phase 3 — 등록 ✅

`DataBaseGroupServiceKey.cs`(`CombatViewBufferKey`), `BufferSystemFactory.cs`(생성 한 줄), `BufferSystemGroup.cs`(`IBufferQuery` 두 속성). `ResetRunProgress`는 변경하지 않았다.

커밋 `22ba69fc`, 에디터 컴파일 에러 0

## Phase 4 — 마무리

| Task | 내용 | 완료 조건 |
|---|---|---|
| 4.1 | ✅ **D5 결정: 테스트를 추가한다**(사용자) | — |
| 4.2 | ✅ 작성: `Assets/Tests/EditMode/Framework/CombatViewBufferTests.cs`(12개). **EditMode 실행 결과 대기** | EditMode 테스트 통과 |
| 4.3 | ✅ 후속 공유 문서를 새로 작성: [Framework 반영 결과](../handoffs/2026-09-29-combat-view-buffer-framework-applied.md). 먼저 보낸 공유 문서는 그대로 둔다 | — |
| 4.4 | 설계 §13 구현 결과를 최종 반영한다. 테스트 결과와 4.3 결과를 추가한다 | 문서 반영 |

4.2에서 확인할 항목

- 등록: `CombatViewReadSink`와 `CombatViewWriteSink`가 null이 아니고 같은 인스턴스다
- 번호
  - `Submit`과 `SetPreview`가 같은 카운터로 1, 2, 3을 발급한다
  - `Clear`한 뒤에도 번호가 이어진다
  - `default` 값을 넣어 예외가 나도 번호를 쓰지 않는다
- `TryGetNextCommand`
  - 처리 번호보다 큰 첫 명령을 준다
  - 새 명령이 없으면 false다
- `RemoveCommandsThrough`
  - 해당 번호 이하만 지운다
  - 음수나 발급한 적 없는 번호에는 예외를 던진다
- 미리보기
  - `SetPreview`가 덮어쓰고 번호가 바뀐다
  - `ClearPreview`를 하면 `HasPreview`가 false다
- `AcknowledgePresented`
  - 같은 값은 허용한다
  - 줄어드는 값에는 예외를 던진다
  - `Clear`한 뒤에도 값이 유지된다
- 값 타입
  - 비어 있는 `EntityId`, 정의되지 않은 `Rotation4`, `default(AttackAim)`을 거절한다

## 이 계획에 없는 것

- 전투 Flow: `CombatLifeCycle`, `CombatPlan`, 전투 `JobType`·Job, 연출 대기, `Clear()` 호출 위치. 별도 설계로 다룬다
- View 입력 핸들러와 연출 코드
- Core 처리 서비스, 결과·전투 상태 Buffer
