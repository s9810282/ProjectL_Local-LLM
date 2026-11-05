# 전투 이동 Drag 입력 설계 (2026-10-13)

- **목표:** 전투 이동 입력을 "Q로 진입 + 마우스 호버로 경로 + 클릭으로 확정"에서 **유닛을 Drag해서 이동**하는 방식으로 바꾼다. 경로 그리기·Core 견적·승인 대기·제출 로직은 그대로 쓴다.
- **상태:** 설계 확정(2026-10-13, M1~M3 추천안). 진행·검증 기록은 View 계획 §7.75.
- **2026-10-19 바뀜:** §1 요구 1("턴 시작 시 아무 모드도 아니다")·3("Drag 시작 시 범위 표시")의 범위 부분은 [이동 모드 범위 설계](2026-10-19-combat-move-mode-range-design.md)로 바뀌었다. 턴 시작부터 이동 모드로 범위가 보이고, Drag는 그 범위를 그대로 쓴다. 입력(Drag 시작·확정·취소) 규칙은 이 문서 그대로다.

## 1. 사용자 요구(2026-10-13)

1. 유닛 턴 시작 시 아무 모드도 아니다.
2. 유닛은 Drag로 이동한다.
3. Drag 시작 시 기존처럼 이동 가능 범위를 표시한다.
4. Drag 중 기존처럼 지나간 경로를 표시한다(로직 변경 없음).
5. Drag를 놓으면(DragEnd) 캐릭터를 최종 이동한다.

## 2. 지금 흐름과 바뀌는 점

| 단계 | 지금 | 바뀐 뒤 |
|---|---|---|
| 진입 | Q(`MoveMode`) → `CombatPathInput.BeginPlanning`(범위 요청, 시작 칸 대기) | 행위자 칸에서 **누름**(`SelectPressed`) → 같은 `BeginPlanning` → 그 자리에서 바로 시작 |
| 시작 칸 | 행위자 앵커 또는 상하좌우 붙은 칸에 포인터가 들어오면 시작 | 누른 칸 = 행위자 앵커(아군 유닛은 모두 1×1). 붙은 칸 시작은 쓰지 않음 |
| 범위 표시 | `MoveRange` 미리보기 응답으로 표시 | 같음 |
| 경로 그리기 | `CombatMovementInteraction.PointerMoved`(지나간 칸·히스테리시스·견적) | 같음 |
| 확정 | 끝점 클릭(`SelectPressed`) → `Confirm` → 승인 대기 → 제출 | **놓기**(`SelectReleased`) → 같은 `Confirm` → 승인 대기 → 제출 |
| 취소 | 우클릭·포커스 잃음·E·턴 종료 | 같음 + 확정할 수 없는 곳에서 놓기(M3) |
| 공격(E)·복귀(R)·배치 Drag | — | 바뀌지 않음 |

`CombatMovementInteraction`(경로 초안·견적·승인·제출)과 `MovementDraft`·`MovementPointerSampler`는 바꾸지 않는다. 바뀌는 곳은 진입과 확정을 정하는 `CombatPathInput`, 그리고 Q 처리(M1)뿐이다.

## 3. 구조

```text
SelectPressed (전투 Layer, 공격 모드 아님 → Path 대상, 기존 Router 판정 그대로)
  CombatPathInput.SelectPressed
    지정 중 아님 + 누른 칸 == 활성 아군 행위자 앵커 → BeginPlanning() → 같은 좌표로 시작 처리(_waitingForOrigin 해소)
    그 밖 → 아무것도 안 함
PointerMoved  → 지금과 같음(경로·견적·고스트·커서 스냅)
SelectReleased
  CombatPathInput.SelectReleased
    지정 중 + 시작됨 → Confirm(놓은 칸)
       확정 성립(경로 있음·끝점 = 놓은 칸·미정 칸 없음·필드 안) → 동결 → 승인 오면 제출(기존)
       확정 불성립 → 지정 취소(M3)
    놓은 뒤 승인 응답이 "이동 불가"면 → 지정 취소(M3)
```

- **C1 진입 조건 한 곳:** "누를 수 있는가"는 기존 입구 검사(`AllowsInput`: 수명·전투·입력 가용·Path 대상·`CanEditPath`)와 기존 `BeginPlanning`의 행위자 조건(Player·Field·생존·`MoveMethod != None`·`PlayerInput` 국면)을 그대로 쓴다. 새 조건은 "누른 칸이 그 행위자의 앵커"뿐이다.
- **C2 놓기 상태:** `CombatPathInput`이 "버튼을 놓았는가"(`_released`)를 갖는다. 놓은 뒤에는 포인터 이동을 무시하고(동결과 같음) 승인 결과만 기다린다.
- **C3 경로 로직 무변경:** 지나간 칸 기록·경계 히스테리시스·불가 응답의 앞부분 유지·`IsInside` 확정 규칙은 그대로다. Drag 중 불가 칸에 닿으면 지금처럼 그 앞까지 유지하고 다른 후보로 이어 그릴 수 있다.
- **C4 커서 스냅:** Drag 중은 `IsPlanning`이므로 기존 커서 그리드 스냅이 그대로 적용된다.

## 4. 결정 필요

| ID | 질문 | 추천 |
|---|---|---|
| M1 | Q(`MoveMode`) 입력을 어떻게 할까 | **완전히 제거.** 입력 액션(`PlayerInputActions.inputactions`의 Combat `MoveMode`), `StageInputNames.Combat.MoveMode`, `CombatInputController` 바인딩, `IStageInputReceiver.MoveModeRequested`·수신 슬롯·`CombatInputReceiver`, Router `Request.MoveMode`, 관련 테스트(약 20곳). 남겨 두면 아무 일도 안 하는 키가 생긴다. 대안: Router에서만 무시하고 나머지는 둠 |
| M2 | Drag를 시작할 수 있는 칸 | **활성 아군 행위자의 앵커 칸만.** 아군 유닛은 모두 1×1이라 앵커 = 유닛 칸이다. 큰 발판 유닛이 생기면 그때 오프셋을 설계한다 |
| M3 | 확정할 수 없는 곳에서 놓거나, 놓은 뒤 승인이 "이동 불가"로 오면 | **지정을 취소하고 이동하지 않는다**(범위·경로·고스트 정리, 유닛 원위치). 다시 Drag하면 된다. 대안: 확정 가능한 앞부분까지 이동 |

정해진 기본 동작(바꾸지 않음): 우클릭·포커스 잃음·E·턴 종료로 Drag 취소, 놓은 뒤 승인 대기 중 새 누름 무시, 놓은 칸이 시작 칸(경로 0칸)이면 취소.

## 5. 바뀌는 파일(추천안 기준)

| 파일 | 변경 |
|---|---|
| `ViewSystem/Combat/CombatPathInput.cs` | `SelectPressed`: 진입(C1). `SelectReleased`: 확정·불성립 취소(M3). `OnPreview` 뒤 놓은 상태에서 불가·확정 해제면 취소. `_released` 상태, `BeginPlanning`은 내부 진입으로만 사용 |
| `ViewSystem/Combat/CombatInputRouter.cs` | (M1) `Request.MoveMode` 제거 |
| `ViewSystem/Combat/CombatInputReceiver.cs`, `StageInput/IStageInputReceiver.cs`·`StageInputReceiverSlot.cs`·`CombatInputController.cs`·`StageInputNames.cs` | (M1) `MoveModeRequested`·`MoveMode` 제거 |
| `Assets/ProjectL/Input/PlayerInputActions.inputactions` | (M1) Combat 맵의 `MoveMode` 액션·Q 바인딩 제거 |
| 테스트 | 진입(앵커 누름만·다른 칸·공격 모드·허용 안 됨), 놓기 확정(승인 뒤 제출 1회), 놓기 취소(시작 칸·필드 밖·UI·미정 칸), 놓은 뒤 불가 응답 취소, 우클릭 취소, 기존 경로 그리기 테스트의 Q 진입을 누름 진입으로 교체 |
| `viewDocs/stage-input-guide.md` §2.2 | 조작 설명을 Drag 방식으로 교체 |

## 6. SOLID·SSOT·FW 점검

- **S:** 진입·확정 시점은 `CombatPathInput`, 경로·견적·제출은 `CombatMovementInteraction`, 대상 판정은 Router(변경 없음).
- **SSOT:** 허용 조건은 기존 `AllowsInput`·`CanEditPath` 하나, 행위자 조건은 기존 `ActiveActor` 하나. Q 제거로 같은 진입이 두 경로로 생기지 않는다.
- **FW:** 입력 계약(Framework InputSystem)은 그대로이고, Stage 입력 맵에서 액션 하나만 줄어든다.

## 7. 확인 방법

- EditMode: 위 테스트, 기존 `CombatMovementFeedbackTests`·`MovementDraftTests`·`CombatPathInputCleanupTests`·`CombatInputRouterTests` 유지.
- 재생(Live): 턴 시작 때 아무 표시 없음 → 활성 유닛을 누르면 범위 표시 → 끌면 경로·고스트·커서 스냅 → 놓으면 이동. 시작 칸·필드 밖·UI에서 놓기, 불가 칸으로 놓기, 우클릭, E는 모두 이동 없이 취소. Q는 아무 일도 하지 않음(M1 완전 제거면 입력 자체 없음). R 복귀·공격·배치 Drag는 전과 같음.
