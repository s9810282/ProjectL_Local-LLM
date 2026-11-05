# 전투 경로 입력 분리 Task 계획 (2026-10-19)

- 설계: [경로 입력 분리 설계](../specs/2026-10-19-combat-movement-range-tracker-design.md)(RT1~RT8). 규칙·결정의 원본은 설계 문서다. 이 문서는 Task·순서·검증·실행 기록만 둔다.
- 사용자 요청(2026-10-19): "저 3가지 넣어서 계획 다시 짜." 세 가지는 다음과 같다.
  - 슬롯 규칙 함수(M2): 호버 U-2가 이미 작업 트리에 `CombatPreviewSlotRule`로 넣었다. 이 계획은 착수 조건으로만 다룬다.
  - 복귀 견적 소유자 분리(RT7)
  - 초안 표시 거울 정리(RT8)
  - 여기에 처음 설계한 범위 소유자 분리(RT1~RT6)를 더해 Phase 넷으로 묶는다.
- 목표 점수(설계 §4): SSOT 10·SOLID 8·FW 9·독립성 9(종합 9.0). 지금은 7.3이다.
- 상태: **R-1~R-4 완료(2026-10-19), Unity 컴파일 문제 없음(사용자 확인), Unity Test Runner·재생 확인 대기(§8).** 결정 Q-R1~Q-R5 모두 A, R-0 건너뜀(사용자). 원칙 재평가 9.0/10(설계 §8). 실행 기록 §9.

## 0. 착수 조건·기준

### 0.1 착수 조건

- **호버 U-1·U-2 커밋이 먼저다.** 지금 작업 트리에 커밋 전으로 있고, 같은 파일을 고쳤다(`CombatPathInput.cs`·`CombatReturnRequest.cs`·`CombatInteraction.cs`·`CombatViewSystem.cs`·`CombatSceneInputFactory.cs`). 새 파일은 `CombatPreviewSlotRule.cs`·`CombatUnitRangeHover.cs`·`ICombatUnitRangeFeedback.cs`·`UnitAttackRangeResult.cs`와 시험 넷이다. 호버 계획 §6의 Unity 확인도 받은 뒤가 좋다(같은 경로를 이 작업이 다시 바꿈).
- 순서(설계 Q-R3): 권장은 이 분리 → 폴더 정리(M)다. M이 먼저 들어가면 새 파일은 M 대응표 위치(`Input/MovePath/`·`Input/Contracts/`)에 둔다.
- 다른 세션의 진행 중 작업: 조각 실행 대여 분리(`CombatPieceRun`·`CombatPieceContext`·`CombatComposition`·`CombatPieceRunLeases`)는 이 작업과 파일이 겹치지 않는다. [공격 궤적 계획](2026-10-19-combat-attack-trajectory-plan.md) TJ-3(`CombatAttackFeedback`·`ICombatBattleDisplay`·`CombatSceneDisplay`·`AttackTargetingInteraction`·두 Factory)도 파일이 겹치지 않는다. 다만 R-3의 비용 표시 멱등화는 공격 조준의 비용 표시에도 적용된다(§5 T14). 착수 때 `git status`로 다시 확인한다. 겹치지 않는 미커밋 파일은 검증 빌드에서 HEAD 판으로 고정한다(이동 모드 범위 설계 §7.1의 방법).
- 착수 전 확인할 파일(미커밋 변경이 없어야 함): `CombatPathInput.cs`·`CombatReturnRequest.cs`·`CombatMovementRangeRequest.cs`·`CombatInteraction.cs`·`CombatInputRouter.cs`·`ICombatMovementFeedback.cs`·`Display/CombatUnitHudFeedback.cs`, 시험 `CombatMovementFeedbackTests.cs`·`CombatMovementPlanningInputTests.cs`·`CombatUnitHudFeedbackTests.cs`·`FieldHighlightStateTests.cs`.

### 0.2 검증 방법

- 컴파일: VS 2022 MSBuild + extra.targets(새 파일만 Compile에 더함, 산출물은 스크래치패드). 시험·에디터 정의.
- 회귀: Unity 내장 Mono 관리 실행. 시험 하나당 프로세스 하나, 순번(`@n`)으로 실행. 비교는 R-0 기준선 대비 **시험 이름 단위 상태 변화**로 한다.
  - `CombatPathInput`은 입력 경로 공통 파일이라 Phase마다 Core·Framework **전체**를 돌린다. 호버 기록 기준 약 9,001개, 백그라운드로 약 20분.
- R-1·R-2는 동작 동일이다. **기존 시험을 고치지 않는다.** 하나라도 상태가 바뀌면 고치지 않고 멈춰 보고한다.
- R-3만 기존 시험(설계 F14의 10개)을 바꾼다. 바꾸는 이유와 전후 단언을 실행 기록에 남긴다.
- Unity: 관리 실행에서 NATIVE·Unity 전용인 시험과 화면은 Phase 끝 사용자 확인 목록으로 넘긴다.

## 1. 바꾸는 파일

| 파일 | Phase | 변경 |
|---|---|---|
| `C/ICombatMovementRangeFeedback.cs`(새) | R-1 | 범위 표시 두 멤버(설계 RT4) |
| `C/CombatMovementRangeTracker.cs`(새) | R-1 | 범위 소유자(RT2) |
| `C/ICombatMovementFeedback.cs` | R-1·R-2 | 두 좁은 계약을 상속하고 해당 멤버를 뺀다 |
| `C/CombatPathInput.cs` | R-1·R-2·R-3 | 범위 → 소유자 호출(RT3), 복귀 → 소유자 호출(RT7), 거울 삭제(RT8) |
| `C/CombatMovementRangeRequest.cs` | R-1 | 머리 주석의 소유자 이름만 |
| `C/CombatInteraction.cs`·`C/CombatInputRouter.cs` | R-1 | (Q-R4 A) `CancelPath` → `LeaveMoveMode`, 호출 두 곳, XML doc |
| `C/ICombatReturnFeedback.cs`(새) | R-2 | `SetReturnState` 하나 |
| `C/CombatReturnEstimateTracker.cs`(새) | R-2 | 복귀 견적 소유자(RT7) |
| `C/CombatReturnRequest.cs` | R-2 | 머리 주석의 소유자 이름만 |
| `C/Display/CombatUnitHudFeedback.cs` | R-3 | 비용 표시 멱등(RT8 1) |
| 시험 | R-1~R-3 | §7 |
| 문서 | R-4 | §6 |

`C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`. 새 파일 넷은 `.meta`(GUID 32자리)와 함께 만든다. 모두 순수 C#이라 `MovedFrom`은 필요 없다.

바꾸지 않는 것: Factory(`CombatSceneInputFactory` — 경로 입력 생성자 서명 그대로), 서비스·키, Buffer, Core, `CombatSceneDisplay`·Presenter·FieldView(계약 상속만 바뀜), 다른 작업자 파일·시험 가짜(명시 구현이 없어 그대로 컴파일, 설계 F4).

## 2. Phase R-0 기준선

- **T1** 착수 조건(§0.1)을 확인한다: `git status`·HEAD를 적고, 대상 파일의 미커밋 변경 여부를 본다. csproj Compile 목록과 디스크를 대조한다.
- **T2** 관리 실행 도구를 스크래치에 다시 만들고 Core·Framework 전체 기준선을 잡는다. 수·기존 FAIL 목록은 §9.1에 적는다. 호버 계획 §7.3의 9,001개(기존 FAIL 94)와 대조한다.

## 3. Phase R-1 범위 소유자 (동작 동일)

- **T3 계약** `ICombatMovementRangeFeedback`: `ShowMovementRange`·`ClearMovementRange`. 두 멤버의 XML doc(범위만 표시, 이동 승인은 별도 견적)을 `ICombatMovementFeedback`에서 옮긴다. `ICombatMovementFeedback : ICombatMovementRangeFeedback`.
- **T4 소유자** `CombatMovementRangeTracker`(설계 RT2):
  - 상태: `CombatMovementRangeRequest`(값 객체 재사용)·응답 세대·보임·보이는 사실.
  - 본문은 경로 입력에서 그대로 옮긴다.
    - `Matches` = 지금 `RangeMatches`(슬롯 규칙 `IsSettled` 호출 포함, 호버 U-2 판)
    - `Request` = `RequestMovementRange`
    - `Drop` = `DropRangeRequest`
    - `DropIfSlotLost` = `OnPreview` 첫 줄
    - `Clear` = `ClearMovementRange`(조기 반환·두 정리 모두 시도 포함)
    - `TryShowAnswer` = `ConsumeMovementRange`의 소비 → 슬롯 비움 → 보임 켜기 → 표시. 보임을 표시보다 먼저 켜는 순서를 지킨다(설계 F6).
  - 정리 실패 문구: `[CombatMovementRangeTracker] 이동 범위 정리 중 실패했습니다.`
- **T5 경로 입력** `CombatPathInput`(설계 RT3 표):
  - 생성자에서 `_range = new CombatMovementRangeTracker(input, write, presenter)`.
  - 범위 필드 넷과 옮긴 메서드를 지운다. 호출 지점 14곳을 표대로 바꾼다.
  - "지우고 → 지정 세대 검사 → 요청" 두 곳(`TryBeginPlanning`·`RefreshMoveModeRange`)의 검사 위치는 그대로 둔다.
  - `ConsumeMovementRange`의 지정 세대는 `TryShowAnswer` 호출 **앞에서** 읽는다. 지금도 슬롯 비움 뒤·표시 앞에서 읽고, 슬롯 비움은 콜백이 없으므로 같다.
  - 머리 주석: "범위 기계는 `CombatMovementRangeTracker`, 언제·무엇을은 여기."
- **T6 이름**(Q-R4 A): `CombatInteraction.CancelPath` → `LeaveMoveMode`, `CombatInputRouter`의 호출 두 곳. 두 메서드 XML doc에 범위를 남기는지 지우는지 적는다(`CancelInputs` = 이동 모드면 남김, `LeaveMoveMode` = 지움).
- **T7** `CombatMovementRangeRequest` 머리 주석: "CombatPathInput이 한 개를 소유" → "CombatMovementRangeTracker가 한 개를 소유".
- **T8 시험**(§7.1).
- **검증·보고:** MSBuild 오류 0·새 경고 0, 관리 실행 전체가 R-0 대비 기존 시험 상태 변화 0, 새 시험 PASS. §9.2에 적고 멈춘다.

## 4. Phase R-2 복귀 견적 소유자 (동작 동일)

- **T9 계약** `ICombatReturnFeedback`: `SetReturnState(CheckpointReturnPreview)`. `ICombatMovementFeedback`이 상속한다.
- **T10 소유자** `CombatReturnEstimateTracker`(설계 RT7):
  - 상태: `CombatReturnRequest`(값 객체 재사용)·전투 ID.
  - `IsSettled` = 지금 `RefreshReturnEstimate`의 R-b 판단(`OwnsPreview && !IsAnswered`, `IsAnswered`, 슬롯 규칙 `IsSettled`)
  - `Request` = 끝 줄의 `Begin(SetPreview(ReturnToCheckpoint))`
  - `RequestWanted` = `RequestReturn` 끝 두 줄
  - `ShowUnavailable` = `SetReturnState(null)`
  - `HandleAnswer` = `HandleReturnPreview`에서 허용 판단을 뺀 나머지(자기 응답 확인 → 답 받음 → 표시 → 원함이면 승인·정체성 확인 뒤 제출 → 정리)
  - `Clear` = `ClearReturnRequest`
- **T11 경로 입력**:
  - `_return` → `_returnEstimate`(생성자에서 `(input, write, presenter, _battle)`).
  - `RefreshReturnEstimate`에는 "언제" 조건만 남긴다(지정 중 아님·허용·이동 상호작용 비활성·PlayerInput·행위자 이동 여부·슬롯 한가 R-a).
  - `RequestReturn`: 입구 검사 → `CancelInputs` → `_range.Drop()` → `_returnEstimate.RequestWanted`.
  - `OnPreview`: `HandleReturnPreview(preview)` → `if (AllowsInput()) _returnEstimate.HandleAnswer(preview)`. 그 자리에서 사실을 다시 읽는 지금 규칙을 지킨다(설계 RT7).
  - 정리 목록(`Detach`·`CancelInputs`)의 `ClearReturnRequest` → `_returnEstimate.Clear`.
- **T12** `CombatReturnRequest` 머리 주석: 소유자 이름.
- **T13 시험**(§7.2).
- **검증·보고:** R-1과 같다. §9.3에 적고 멈춘다.

## 5. Phase R-3 초안 표시 멱등화·거울 삭제

- **T14 HUD 비용 표시 멱등**(설계 RT8 1) `CombatUnitHudFeedback`:
  - 보인 비용과 보인 HUD 대상(`ICombatUnitHudTarget` 참조)을 기억한다.
  - `ShowCostPreview(actor, cost)`는 같은 행위자·같은 비용·지금 찾은 HUD 대상이 기억한 대상과 같으면 아무것도 하지 않는다.
  - `ClearCostPreview`가 기억을 지운다.
  - HUD 비용에 쓰는 곳은 이 클래스뿐이다(`ICombatUnitHudTarget.SetCostPreview` 호출 두 곳, 계획 작성 때 grep).
  - **공격 모드 비용 표시도 같은 함수를 쓴다**(`AttackTargetingInteraction` → `CombatAttackFeedback.ShowAttackCostPreview`). 그래서 공격 쪽도 같은 값 재호출 때 지웠다 다시 쓰지 않게 된다. 화면 결과는 같다. 공격 시험(`CombatAttackFeedbackTests`·`WallAttackInteractionTests`)도 회귀 대상이다.
- **T15 경로 입력 거울 삭제**(RT8 2):
  - 필드 다섯(`_shownPath`·`_shownPreview`·`_shownPointerValid`·`_shownCandidate`·`_draftDisplayVersion`), `InvalidateDraftDisplay`, 그리고 그 호출 다섯 곳(`Detach`·시작 칸 처리·`CancelInputs`·`ClearDraft`·`ShowDraft`)을 지운다.
  - `ShowDraft()`는 지정 중이면 포인터 갱신 → 지정 세대 검사 → `_draftShown = true` → `_presenter.ShowDraft(...)` 순서로 매번 부른다.
  - `_draftShown`과 "그린 게 있을 때만 지움"(`ClearDraft`)은 남긴다.
- **T16 시험**(§7.3): 설계 F14의 10개를 "최종 표시 값" 단언으로 바꾸고, 멱등 시험을 더한다.
- **T17 할당 확인:** 포인터 이벤트 한 번에 표시 경로(`CombatSceneDisplay.ShowDraft` → Presenter → FieldView·HUD 피드백)에서 할당이 없는지 코드로 확인해 §9.4에 적는다. 측정 기준이 없어 Profiler 측정은 하지 않는다.
- **검증·보고:**
  - MSBuild 오류 0·새 경고 0.
  - 관리 실행 전체: 상태가 바뀐 기존 시험은 T16에서 고친 것뿐이어야 한다. 고친 시험은 PASS, 새 시험 PASS.
  - Unity 확인 목록(§8)을 넘기고 멈춘다.

## 6. Phase R-4 문서·재평가

- **T18 문서:**
  - 이동 모드 범위 설계 MR1의 소유자 줄(범위 기계 = `CombatMovementRangeTracker`)
  - [구조 재평가](../specs/2026-10-15-combat-view-structure-reevaluation.md) N3에 (a)·② 해소·④ 해소 표시(날짜·이 계획 링크)
  - 폴더 정리 계획 §3: `Input/MovePath/` 줄에 소유자 둘, `Input/Contracts/` 줄에 계약 둘
  - CLAUDE.md 현재 상태
- **T19 원칙 재평가:** 설계 문서에 새 절(§8)을 둔다. 기준은 이동 모드 범위 설계 §8과 같다. 점수와 예상(설계 §4)의 차이, 남는 것(입력 게이트·슬롯 나누기)을 적는다.
- 공유: Factory·서비스·다른 작업자 파일 변경이 없어 공유 문서는 쓰지 않는다. 호진님 시험(`HojinCombatAttackDragTests`)은 가짜가 계약 상속만으로 그대로 컴파일된다(고치지 않음).

## 7. 시험

### 7.1 R-1

| 파일 | 사례 |
|---|---|
| 새 `CombatMovementRangeTrackerTests.cs`(순수, 실제 `CombatViewBuffer` + 기록용 표시 가짜) | `Matches`: 보임은 사실로, 기다림은 사실 + 슬롯 소유로 / 다른 사실·슬롯을 잃은 기다림은 불일치 · `Request`: MoveRange 게시, `IsPending` · `TryShowAnswer`: 자기 응답 한 번만 소비·슬롯 비움·표시, 남의 번호·다른 사실·늦은 응답은 무시 · 표시 예외: 보임이 켜진 채 남아 뒤의 `Clear`가 표시를 지움 · `Clear`: 자기 슬롯만 비움(남의 요청 보존), 보였을 때만 표시 지움, 둘 다 시도하고 실패를 모음, 할 일 없으면 표시를 부르지 않음 · `Drop`·`DropIfSlotLost`: 슬롯·표시 무변화, 슬롯을 가진 요청은 `DropIfSlotLost`가 남김 |
| 기존 범위 시험(`CombatMovementRangeRequestTests`·`CombatMovementRangeFeedbackTests`·`CombatMoveModeRangeTests`·`CombatMovementFeedbackTests` 등) | 고치지 않고 통과 |

### 7.2 R-2

| 파일 | 사례 |
|---|---|
| 새 `CombatReturnEstimateTrackerTests.cs`(순수) | `IsSettled`: 자기 요청이 기다리는 중·답 받음(슬롯을 잃어도) 참, `Clear` 뒤 거짓 · `Request`: ReturnToCheckpoint 게시 · `RequestWanted`: 원함 표시 · `HandleAnswer`: 남의 번호 무시, 자기 응답은 답 받음 + 표시, 원함 + 승인 + 맞는 정체성이면 제출 한 번 + 정리, 원함 + 거절이면 제출 없음·답 받음 유지, 정체성 불일치면 제출 없음 · `Clear`: 자기 슬롯만 비우고 표시 null · `ShowUnavailable`: 표시 null |
| 기존 복귀 시험(`CombatReturnRequestTests`·`CombatMoveModeRangeTests`의 R-a/R-b·`CombatMovementViewTests`의 복귀 사례) | 고치지 않고 통과(Unity 전용은 Unity 결과로) |

### 7.3 R-3

| 파일 | 사례 |
|---|---|
| `CombatMovementFeedbackTests`(9개)·`CombatMovementPlanningInputTests`(1개) — 설계 F14 | 표시 호출 수 단언을 Probe의 최종 값(Path·Candidate·PointerValid·Preview·Range) 단언으로 바꾼다. 리플렉션 `_shownPath` 단언은 지운다. "늦은/옛 응답이 표시를 바꾸지 않음"처럼 화면에 보이는 뜻은 최종 값으로 그대로 확인한다. |
| `CombatUnitHudFeedbackTests` | 같은 대상·같은 비용 재호출은 HUD 대상 호출 없음, 비용이 바뀌면 다시 씀, 대상(HUD 객체)이 바뀌면 다시 씀, `Clear` 뒤 같은 값은 다시 씀 |
| `FieldHighlightStateTests` | 같은 칸·같은 상태의 `SetHover` 재호출은 바뀐 칸 0(`SetLayerCells`의 같은 입력은 기존 시험이 이미 확인) |

## 8. Unity 확인 목록 (R-3 끝, 사용자)

1. Unity Test Runner: 관리 실행에서 NATIVE·Unity 전용인 시험. 특히 `CombatMovementViewTests`·`CombatDisplayReuseTests`(이동·복귀 사례), `CombatMovementPlanningDisplayTests`, `UnitHUDTests`(비용).
2. 이동 모드: 턴 시작 때 범위, 공격 모드 출입, 우클릭·놓기 실패 뒤 유지, 한 번 이동한 뒤 새 범위.
3. Drag 중: 경로·끝점 색·고스트가 지금과 같고, **HUD 비용 숫자가 포인터를 움직일 때 깜빡이지 않음**(RT8의 핵심). 공격 모드에서 대상을 옮길 때 비용 표시도 지금과 같음.
4. 복귀: 이동 뒤 복귀 버튼 상태, 복귀 버튼으로 복귀, 거절될 때 버튼 상태.
5. 호버 공격 범위와 함께: 이동 모드 범위 위 호버 범위, 이동 뒤 호버.
6. 턴 종료·적 차례·결과 재생 중 범위·초안이 남지 않음.

## 9. 실행 기록

### 9.1 R-0 (건너뜀)

- 사용자 결정(2026-10-19): "R-0 스킵하고, 나머지는 추천안대로 진행해." Q-R1~Q-R5 모두 A다.
- 착수 조건만 확인했다. 호버 U-1·U-2는 사용자 커밋 `950a4b34`로 들어갔고, 대상 파일에 미커밋 변경이 없었다.
- 기준선은 새로 돌리지 않았다. 호버 세션의 마지막 전체 실행(U-2 판, Framework 5,752·Core 3,249) 결과를 참조로 썼다. 그 실행은 카메라 조각 대여 분리(`3c50a379`)가 들어가기 전 판이다.

### 9.2 R-1 (2026-10-19, 코드 완료)

- 코드(T3~T7):
  - 새 `ICombatMovementRangeFeedback`·`CombatMovementRangeTracker`(+ `.meta`)
  - `ICombatMovementFeedback`이 좁은 계약을 상속
  - `CombatPathInput`의 범위 필드 넷·메서드 넷(`RangeMatches`·`RequestMovementRange`·`DropRangeRequest`·`ClearMovementRange`)을 소유자 호출로 바꿈
  - `CancelPath` → `LeaveMoveMode`(Router 두 곳)
  - `CombatMovementRangeRequest` 주석
- 시험(T8): 새 `CombatMovementRangeTrackerTests` 15개.
- 검증:
  - MSBuild 오류 0·새 경고 0.
  - 관리 실행 전체(5,790개)는 사용자 지시("전체 시험 돌리지마")로 멈췄다. 멈추기 전 끝난 4,790개는 참조 대비 상태 변화 0이고, FAIL 78은 참조와 같은 기존 실패다.
  - `ProjectL.Tests.Core`는 Core 어셈블리만 참조해 이 작업의 영향을 받지 않으므로 돌리지 않았다.

### 9.3 R-2 (2026-10-19, 코드 완료)

- 코드(T9~T12):
  - 새 `ICombatReturnFeedback`·`CombatReturnEstimateTracker`(+ `.meta`)
  - `ICombatMovementFeedback`에서 `SetReturnState`를 상속으로 옮김
  - `CombatPathInput`의 `_return`·`HandleReturnPreview`·`ClearReturnRequest`를 소유자 호출로 바꿈. 응답 처리 앞의 평가 순서를 지키려고 `IsAnswer` → `AllowsInput` → `HandleAnswer` 순으로 둠
  - `CombatReturnRequest` 주석
- 시험(T13): 새 `CombatReturnEstimateTrackerTests` 12개. 승인·제출은 실제 Core 미리보기 context로 확인한다.
- 검증:
  - MSBuild 오류 0·새 경고 0.
  - 새 27개 PASS(R-1분 포함).
  - 계획의 "Phase마다 전체 실행" 대신, 바뀐 클래스 묶음(R-3 뒤 315개 PASS)으로 확인했다.

### 9.4 R-3 (2026-10-19, 코드 완료)

- 코드(T14·T15):
  - `CombatUnitHudFeedback.ShowCostPreview` 멱등(같은 행위자·같은 비용·같은 HUD 객체면 그대로)
  - `CombatPathInput`의 거울 필드 다섯·`InvalidateDraftDisplay`(호출 다섯 곳) 삭제. `ShowDraft`는 지정 중이면 매번 표시를 부른다. `_draftShown`(소유 표지)은 남김.
- 시험(T16):
  - 계획은 10개를 고친다고 했으나, 따라가 보니 **실제로 깨지는 것은 5개**였다. `SamePathNewQuote…`·`FailedDisplay…`·`BlockedCandidate…`는 바뀐 값마다 한 번씩 그리는 것을 보므로 그대로 통과한다.
  - 고친 5개: `SameAcceptedPreview_KeepsDisplayedValues_AndValidityChangesRender`·`UnacceptedOldResponse_DoesNotReplaceDisplayedDraft`·`CancelOrDetachDuringDisplay_LeavesNoDraft`(리플렉션 `_shownPath` 삭제)·`NewGestureDuringDisplay_KeepsInnerDisplayAsLastApplied`(호출 수 → 최종 값)·`QWaitingDoesNotStartAppearance_SameCellPointerMovesWithoutQuoteOrDraftChange`.
  - 이름만 고친 1개: `InputDisabledWithCachedPreview_ClearsDraftAndDoesNotRedraw`.
  - `CombatUnitHudFeedbackTests`에 멱등 사례 3개를 더했다. `FieldHighlightStateTests`는 같은 입력 무변화를 이미 확인하고 있어(`SetHover` 88행·`SetLayerCells` 62행) 더하지 않았다.
- T17 할당: 표시 경로(`CombatSceneDisplay.ShowDraft` → Presenter → `FieldView.SetLayerCells`/`SetHoverCell` → `FieldHighlightState` 내부 버퍼, HUD 비교)에 할당이 없음을 코드로 확인했다.
- **원칙 평가 중 발견·수정(E0, 설계 §8.6):** HUD 비용 기억이 표시 반환을 몰랐다. `OnReturned`가 그 유닛의 비용 대상 기억을 버리게 고치고 시험 하나를 더했다(합계 HUD 새 시험 4개).
- 검증:
  - MSBuild 오류 0·새 경고 0. 같은 시각 다른 세션(공격 궤적 TJ-3·클립 AC-1)의 미커밋 파일 18개는 HEAD 판으로 고정하고 새 파일은 뺐다(`tool2/pin.sh`).
  - 관리 실행: 바뀐 클래스 묶음 15개 클래스 315개 PASS(수정 전), 수정 뒤 4개 클래스 57개(PASS 51·NATIVE 6 — `CombatAttackFeedbackTests`, 참조에서도 NATIVE).
  - Unity 컴파일 문제 없음(2026-10-19, 사용자 확인).
  - Unity Test Runner·재생은 §8 목록대로 남았다.

### 9.5 R-4 (2026-10-19)

- 문서(T18):
  - 이동 모드 범위 설계 MR1 소유자 줄
  - 구조 재평가 N3 실행 표시
  - 폴더 정리 계획 §3 두 줄
  - CLAUDE.md
- 원칙 재평가(T19): 설계 §8. **SSOT 10·SOLID 8·FW 9·독립성 9 = 9.0/10**(예상과 같음). 중간 0(평가 중 찾은 E0은 고침), 낮음 4.
- 커밋 때 파일(커밋은 사용자):
  - 새 파일 여섯 + `.meta`: `CombatMovementRangeTracker.cs`·`ICombatMovementRangeFeedback.cs`·`CombatReturnEstimateTracker.cs`·`ICombatReturnFeedback.cs`·`CombatMovementRangeTrackerTests.cs`·`CombatReturnEstimateTrackerTests.cs`
  - 고친 파일: `CombatPathInput.cs`·`CombatInteraction.cs`·`CombatInputRouter.cs`·`ICombatMovementFeedback.cs`·`CombatMovementRangeRequest.cs`·`CombatReturnRequest.cs`·`Display/CombatUnitHudFeedback.cs`·시험 `CombatMovementFeedbackTests.cs`·`CombatMovementPlanningInputTests.cs`·`CombatUnitHudFeedbackTests.cs`
  - 문서: 설계·계획·이동 모드 범위 설계·구조 재평가·폴더 정리 계획·CLAUDE.md
  - 같은 작업 트리의 다른 세션 파일(공격 궤적·클립 등)은 이 작업이 아니다.
