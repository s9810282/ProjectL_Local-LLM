# 전투 포기 버튼·확인창 설계 (4-V, 2026-10-07)

- **목표:** 전투 화면에서 플레이어가 자발적으로 포기할 수 있게 한다. 포기 순서(남은 처리·표시 → Core `AbandonBattle()` → 완료 표시 → `EndBattle` → 새 Run → StageSelect)는 골격이 이미 갖고 있으므로, View는 **버튼·확인창·완료 문구**와 **요청 한 번**만 맡는다.
- **상태:** 설계 확정(2026-10-07, P1~P5 추천안). 진행·검증 기록은 View 계획 §7.58.
- **골격 계약 원본:** [골격 설계](2026-09-30-combat-flow-skeleton-design.md) §6.5 K22, [골격 최종 인계](../handoffs/2026-10-07-combat-skeleton-final-handoff.md) §2.

## 1. 이미 있는 것

| 대상 | 현재 |
|---|---|
| `ICombatExitService.RequestAbandon()` | 조건(활성 진입·전투 있음·Completion 없음·View 부착·이탈 Idle·Live·오류 없음)이 맞으면 true, 아니면 false. 같은 요청이 진행 중이면 true |
| `ICombatExitService.ExitStatus` | `Idle`, `Draining`, `Abandoning`, `Presenting`, `Ending`, `PreparingNewRun`, `Transitioning`, `Failed` |
| 이탈 중 View 호출 | 골격이 `SuspendSubmissions()` → `PresentCompletion(completion, completed)`를 부른다. 포기도 같은 경로다 |
| `CombatHud.ShowCompletion` | 문구가 "승리 · 결과 확인" / "패배 · 결과 확인" 두 가지뿐이다 |
| View의 LifeCycle 참조 선례 | `CombatViewSystem.PostBindService`가 LifeCycle 그룹의 `CombatEntry`를 받아 좁은 읽기 계약 `ICombatEntryRead`로만 보관한다 |

## 2. 구조

```text
CombatAbandonPanel (Combat 씬 Canvas, MonoBehaviour)   포기 버튼 + 확인창(문구·확인·취소). 받은 값만 그림
   ▲ 표시 의도: SetAbandonAvailable(bool) · ShowAbandonConfirm() · HideAbandonConfirm()
   │           BindAbandon(onOpen, onConfirm, onCancel)
CombatSceneDisplay (ICombatBattleDisplay)               위 의도를 패널에 전달, 해제 뒤 무시
   ▲
CombatViewSystem                                         가능 여부 판단(Policy) → 패널 갱신, 확인 시 RequestAbandon 1회
   │  CombatInputPolicy.CanRequestAbandon(facts, exitIdle)   ← 버튼 가능 조건의 원본(SSOT)
   └─ ICombatAbandonRequest (P2)                         ExitStatus 읽기 + RequestAbandon()
```

- **C1 표시와 판단 분리:** 패널은 버튼·확인창만 그린다. 누를 수 있는지는 `CombatInputPolicy`가, 요청은 `CombatViewSystem`이 한다. 패널은 LifeCycle·Buffer를 모른다.
- **C2 가능 조건(Policy 한 곳):** `ExecutionMode == Live` · 전투 있음·표시됨 · 종료 아님(Completion 없음) · 오류 없음 · 이탈 `Idle`. 대기 중 명령·연출 중이어도 가능하다(골격이 처리·표시가 끝날 때까지 기다린 뒤 포기). 배치 단계에서도 가능하다(골격 검증 1번).
- **C3 갱신 시점:** 이탈 상태는 View 밖(LifeCycle)에서 바뀌므로 `Tick`마다 가능 여부를 계산하고 **값이 바뀔 때만** 패널에 쓴다. 할당 없음.
- **C4 최종 판단은 골격:** View가 가능하다고 봐도 `RequestAbandon()`이 false면 아무것도 하지 않는다(확인창만 닫음). View가 골격 조건을 복제해 우회하지 않는다.
- **C5 완료 문구:** `ShowCompletion`이 `Completion.Outcome.Reason == VoluntaryAbandonment`이면 "포기 · 결과 확인"을 보여 준다. 대기 중 승패가 먼저 확정된 경우(골격 A4 `AlreadyCompleted`)는 실제 승패 문구가 그대로 나온다.
- **C6 확인창 수명:** 이탈이 `Idle`이 아니게 되거나, 가능 조건이 깨지거나, 전투 정리·StopInput·Detach·오류가 나면 확인창을 닫는다. 확인창은 View 지역 상태이며 게임 상태가 아니다.
- **C7 RecordInputOnly:** 버튼을 숨긴다(골격 A3: 포기 불가).

## 3. 결정 필요

| ID | 질문 | 추천 |
|---|---|---|
| P1 | 버튼·확인창을 어디에 둘까 | **별도 `CombatAbandonPanel`**(Combat 씬 Canvas, `CombatSceneRoot`가 참조). `CombatHud`(재화·견적·완료)에 넣으면 책임이 커진다. 대안: `CombatHud`에 필드 추가 |
| P2 | View가 골격 서비스를 어떻게 받을까 | **골격 파일에 좁은 계약 `ICombatAbandonRequest { CombatExitStatus ExitStatus; bool RequestAbandon(); }`를 추가하고 `ICombatExitService`가 상속.** View는 `CombatEntry`(→ `ICombatEntryRead`)처럼 이 좁은 계약으로만 보관한다(ISP, `RequestNormalExit` 노출 안 함). 골격 파일 1개에 인터페이스만 더한다. 대안: 골격을 건드리지 않고 `ICombatExitService`를 그대로 보관 |
| P3 | 확인창이 열린 동안 전투 입력 | **막는다.** 확인창을 열 때 진행 중 Drag·조준·경로 표시를 정리하고, 열린 동안 `Receive`가 포인터·키 입력을 무시한다. 취소하면 다시 받는다. 대안: 막지 않음(UI 위 포인터는 이미 취소 영역이지만 Q·E·턴 종료 키는 들어감) |
| P4 | 포기 완료 문구 | **"포기 · 결과 확인"**. 승리/패배와 같은 형식 |
| P5 | 단축키(Esc 등)로 확인창 열기 | **이번 범위 밖.** 입력 액션맵 변경이 필요해 별도 작업 |

## 4. 바뀌는 파일(추천안 기준)

| 파일 | 변경 |
|---|---|
| `ViewSystem/Combat/Display/CombatAbandonPanel.cs`(신규) | 버튼·확인창·확인/취소 버튼. 같은 값이면 다시 쓰지 않음. `FindMissingReference` |
| `CombatSceneRoot.cs` | `abandonPanel` 참조·검증 |
| `ICombatBattleDisplay.cs`·`CombatSceneDisplay.cs` | `BindAbandon`·`SetAbandonAvailable`·`ShowAbandonConfirm`·`HideAbandonConfirm` 전달(해제 뒤 무시, 해제(null)는 항상 전달 — `BindReturn`과 같은 규칙) |
| `CombatInputPolicy.cs` | `CanRequestAbandon(in facts, bool exitIdle)` |
| `CombatViewSystem.cs` | 서비스 보관, Attach 때 Bind·StopInput/Detach 때 해제, Tick에서 가능 여부 갱신, 열기·확인·취소 처리, 확인창 열린 동안 입력 무시(P3) |
| `Display/CombatHud.cs` | 포기 완료 문구(C5) |
| `LifeCycles/Combat/CombatExitSequence.cs`(골격) | P2 추천 시 `ICombatAbandonRequest` 추가·`ICombatExitService` 상속만 |
| 테스트 | Policy 가능 조건 표, ViewSystem(가짜 요청 서비스: 확인 1회 호출·false 무시·상태 변화로 확인창 닫힘·RecordInputOnly 숨김·입력 무시), HUD 포기 문구 |
| 씬(사용자 에디터) | Combat.unity Canvas에 포기 버튼·확인창, `CombatSceneRoot`에 연결 |

## 5. SOLID·SSOT·FW 점검

- **S:** 패널은 그리기, Policy는 가능 판단, ViewSystem은 연결·요청, 골격은 포기 순서.
- **I/D:** View는 골격의 넓은 서비스 대신 좁은 계약만 본다(P2). 패널은 표시 의도 인터페이스로만 호출된다.
- **SSOT:** 버튼 가능 조건은 `CombatInputPolicy` 한 곳, 포기 순서·최종 허용은 골격 `CombatExitSequence` 한 곳, 완료 문구는 `CombatHud` 한 곳.
- **FW:** 새 서비스·모듈 없음. 씬 오브젝트는 에디터에서 미리 둔다.

## 6. 확인 방법

- EditMode: 위 테스트, 기존 `CombatViewSystemTests`·`CombatEntryIntegrationTests`(골격 포기 4건) 유지.
- Live 재생: 배치 단계·전투 단계·자동 진행 중에 포기 → 확인창 → 확인 → (남은 처리 표시) → "포기 · 결과 확인" → StageSelect·새 로스터. 취소하면 그대로 계속. RecordInputOnly에서는 버튼 없음.
