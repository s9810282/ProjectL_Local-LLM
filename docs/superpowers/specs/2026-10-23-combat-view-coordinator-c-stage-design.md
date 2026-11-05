# 전투 View 조정자 C 단계 설계 (2026-10-23, 코드 변경 없음)

- **상태:** 2판. 1판을 하위 에이전트 2개(사실 대조·설계 타당성)로 검증한 뒤, 정정과 권장안 변경을 반영했다(사용자 지시, 반영 기록 §9). **결정 완료(2026-10-23 사용자): Q-C1~Q-C8·Q-T1~Q-T2 모두 A, P1~P8 그대로.** Task 계획: [c-stage-plan](../plans/2026-10-23-combat-view-coordinator-c-stage-plan.md)(Phase마다 멈춘다). **구현 완료(2026-10-23): Phase C-a~C-d 코드(각 Phase 새 에이전트 검토·사용자 Unity 확인 문제 없음), C-e 공유 메모·안내(미전달).** 실행 중 바뀐 것: `CanEnterFromDeployment`에는 모달 판정을 넣지 않음, 옛 984 시험을 호출만 맞춰 되살림, `Detach`의 되돌리기 순서(§2 C-6) — 모두 계획 실행 기록.
- **기준:** HEAD `4c328d07`, 작업 트리 코드 변경 없음. 줄 번호는 이 시점 기준이다.
- **입력:** [조정자 현재 상태 정리](2026-10-23-combat-view-coordinator-inventory.md)(이하 "정리"), 10-23 독립 재평가 7.5(M·H 번호는 그 평가의 감점 번호, 보고서는 저장소 밖), [배경 설계](2026-10-23-combat-backdrop-before-camera-bind-design.md) Q-B5.
- **약어:** `V/` = `…/ViewSystem/Combat/`, `C/` = `…/LifeCycleSystem/LifeCycles/Combat/`.
- **지킬 것:**
  - MSBuild를 돌리지 않고 `[Test]`를 새로 만들지 않는다(보고에 "컴파일 미확인").
  - 커밋하지 않는다.
  - 바꾸는 코드는 모두 전투 View 영역(`V/`·`C/`)이다.
  - Framework·GlobalSystem·Factory·ServiceKeys는 바꾸지 않는다(연결 줄 변경 없음).
  - 다른 사람 줄은 §7에 모은다(`git blame`으로 확인).

## 0. 목표와 범위

1. **골격이 기대는 View 상태를 한 클래스로 뗀다.** 지금 골격이 읽는 진행 사실 다섯 중 셋(제출 정지·완료 표시·포기 요청)이 조정자 상태다(정리 §2). 이것을 `ICombatProgressView`를 구현하는 작은 클래스로 옮긴다.
   - 그러면 다음 단계(골격 점검)에서 읽을 View 쪽 코드는 그 클래스와 좁은 호스트 계약 넷(조정자의 구현 네 곳)으로 줄어든다.
   - 조정자 전체를 읽지 않아도 되지만, 호스트 계약의 구현(`CanShowCompletion`·`IsAttached`)은 조정자 상태를 읽으므로 0은 아니다.
2. **두 벌인 판정을 하나로 한다.** "재생 중"(M1)과 "전투 끝"·제출 정지 주인(M2).
3. **죽은 경로·조용한 실패·넓은 계약을 걷는다.** 포기 확인창(M11), 조용한 분리(M9), `ICombatViewService` 멤버(M6).

**원칙은 동작 동일이다.** 바뀌는 동작은 항목마다 "동작" 줄에 적었다. 모두 정상 흐름에서 닿지 않는 상태(계약 위반)이거나, 화면·입력에 드러나지 않는 깃발 시점이다.

**범위 밖:**
- 조정자의 남은 책임 분리(입력 조립·결과 소비·표시 값·화면 파생, M4). C 단계 뒤 크기를 다시 보고 정한다.
- H1 최종 승리 화면(사용자 결정으로 미룸).
- 골격 쪽 개선(K1~K8, 다음 단계).

## 1. 한눈에

| # | 문제 | 감점 | 바꾸는 것 | 동작 | 시험 |
|---|---|---|---|---|---|
| **C-1** | 포기 확인창 표시 계약이 빈 구현이라 `IsConfirmOpen`은 늘 거짓인데 조정자가 세 곳에서 읽는다 | M11 | 계약·빈 메서드·컨트롤러의 버튼/확인창 길을 걷고, 모달 판정을 하나로 | 없음 | 우리 시험 지움·맞춤 |
| **C-2** | 진행 사실 셋이 조정자 상태이고, 결과 유지 판정·확인 콜백·배경음이 섞여 있다 | (M4 일부) | 새 클래스 `CombatViewProgress`가 진행 계약을 구현, 조정자는 좁은 호스트 계약으로 정리만 | 없음 | 호출 맞춤 28줄 |
| **C-3** | "재생 중"이 조정자 식(`재생 중 ∨ (전투 있음 ∧ S > P)`)과 골격 식(`P < S`) 둘 | M1 | 규칙 함수 하나, View도 골격과 같은 식 | 계약 위반 때만 | 없음 |
| **C-4** | 제출 정지 깃발을 세우는 곳이 둘(View 스스로, Plan 1행), "끝" 식도 둘(`Finished ∨ 완료`, `완료`) | M2 | 깃발은 골격만, View는 종료 결과를 보면 입력만 바로 놓음, 끝 = 완료 하나 | 없음(깃발 시점만) | 우리 시험 맞춤·사례 지움 |
| **C-5** | 결과 유지가 아닌데 전투가 사라지면 오류 없이 분리 → 골격이 오류 없이 멈춤 | M9 | F1 보고 뒤 분리 | 계약 위반 때만 | 없음(통과) |
| **C-6** | 씬·연결 수명의 상태 되돌리기가 흩어지고 비대칭, 씬 루트 참조 둘 | 낮음 | 이름 있는 묶음·되돌리기 함수, 필드 이름 | 없음 | 없음 |
| **C-7** | `ICombatViewService` 멤버 다섯의 바깥 운영 호출이 0이고, `CaptureLost` = `FocusLost` | M6 | 계약에서 다섯을 빼고 `CaptureLost` 삭제 | 없음 | 우리 1·천명님 3 |
| **C-8** | 환경 호버 허용 식을 조정자가 조립 | 낮음 | 사실 조건만 정책 함수로(대상 판정은 호출 자리) | 없음 | 없음 |

## 2. 항목별 설계

### C-1 죽은 포기 확인창 경로 걷기 (M11)

**지금.**
- 표시 계약 `ICombatAbandonDisplay`(`V/Input/ActionButtons/ICombatAbandonDisplay.cs`)를 `ICombatBattleDisplay`가 상속한다(`V/Display/ICombatBattleDisplay.cs:24`).
- 유일한 운영 구현 `CombatSceneDisplay`의 네 메서드는 빈 구현이다(`#region 포기 표시 계약 호환`, `V/Display/CombatSceneDisplay.cs:291-300`). 현강님이 포기 UI를 Option으로 옮기면서(`9354c522`) "기존 컨트롤러 계약은 유지"하고 비웠다.
  - 그래서 `CombatAbandonController`의 `Open`·`Confirm`·`Cancel`은 운영에서 불리지 않는다.
  - `IsConfirmOpen`은 늘 거짓이다.
- 그런데 조정자가 그 값을 세 곳에서 읽는다: 수신 창구 모달(`CombatViewSystem.cs:259`), 손패 입력(`:1054`), 배치 진입(`:1196`).
- 이 계약을 구현하는 것은 운영 `CombatSceneDisplay`, 시험 가짜 `FakeDisplay`(`CombatViewSystemTests`), `AbandonDisplay`(`CombatPanelControllerTests`) 셋뿐이다.
- 살아 있는 입구는 OptionUI → `ICombatAbandonRequestService.TryRequestAbandon` 하나다. OptionUI는 가능 여부 표시를 쓰지 않는다.

**바꾼 뒤.**
- **삭제:**
  - `ICombatAbandonDisplay.cs`(+ `.meta`)
  - `ICombatBattleDisplay`의 상속 목록 항목과 머리 주석 중 포기 문장(`:17-18`, 행동 버튼 문장은 남김)
  - `CombatSceneDisplay`의 region 통째(`:291-300`)
  - 행동 버튼 주석의 "(포기 버튼과 같은 규칙)" 참조(`:306`)
- **컨트롤러:** 버튼·확인창 길(`Attach`·`Sync`·`Close`·`Stop`·`Detach`·`Open`·`Confirm`·`Cancel`·`IsConfirmOpen`·표시 참조·대리자 셋)을 지운다. 요청 상태는 C-2의 진행 클래스로 옮긴다. C-1과 C-2는 같은 Phase에서 한다(§5). 그래서 중간 형태(`Reset()`)는 만들지 않고, 컨트롤러는 바로 C-2의 상태 없는 입구가 된다.
- **조정자:** 모달 판정을 이름 하나로 모은다.

```diff
- _receiver = new CombatInputReceiver(slot, _faults, () => IsSceneActive,
-     () => _abandon.IsConfirmOpen || (_deploymentConfirmation?.IsOpen ?? false), RouteInput);
+ _receiver = new CombatInputReceiver(slot, _faults, () => IsSceneActive, () => IsModalOpen, RouteInput);
  …
- _abandon.Attach(display);                        // ContinueAttach: 연결할 표시가 없다
- _abandon.Detach();                               // Detach: 요청 지우기는 진행 클래스(C-2)
- CleanupErrors.Try(_abandon.Stop, ref errors);    // StopInput: 끌 버튼이 없다
- _abandon.Sync();                                 // SuspendSubmissions·SyncSubmissionState: 맞출 버튼이 없다
  …
+ // 전투 입력을 막는 확인창은 배치 확인창 하나다.
+ private bool IsModalOpen => _deploymentConfirmation?.IsOpen ?? false;
- private bool CanReceiveDeckInput() => IsInputReady && !_abandon.IsConfirmOpen &&
-     !(_deploymentConfirmation?.IsOpen ?? false) && !_submissionsSuspended;
+ private bool CanReceiveDeckInput() => IsInputReady && !IsModalOpen && !_progress.IsSubmissionSuspended;
```

- `CanEnterFromDeployment`(`:1196`)에서 `|| _abandon.IsConfirmOpen`을 빼고, `SyncActionButtons`(`:1188`)는 `IsModalOpen`을 쓴다.
- 클래스 머리 주석(`:53` "포기 확인창")과 컨트롤러 머리 주석을 고친다.

**동작:** 없음.
- 표시 메서드가 빈 구현이었으므로 화면·입력은 같다.
- OptionUI 경로(`TryRequestAbandon`)도 순서가 같다. 빠진 `Close`·`Sync`는 빈 구현만 불렀다.

**시험(Q-T1):** 모두 우리 줄이다. 현강님 `Abandon_ExternalConfirmedRequest_…`(`CombatViewSystemTests.cs:956`)는 본문을 그대로 두고, C-2에서 한 줄(`:963`)만 호출을 맞춘다.

| 시험 | 처리 | 이유 |
|---|---|---|
| `CombatPanelControllerTests.cs`(+ `.meta`) 6개 | 파일 삭제 | 넷은 걷은 기능(연결·버튼·확인창·표시 실패)을 본다. 남는 둘(한 번만 요청, 연결 때 지움)은 `CombatViewSystemTests`의 956·969가 같은 사실을 본다 |
| `CombatViewSystemTests` 920·942 | 삭제 | 버튼 보임, 확인창 열기·취소 |
| 〃 984 `Abandon_ExitStarted_…` | 처음엔 삭제했다가, 사용자 결정(10-23)으로 호출만 맞춰 되살림(Phase C-b, `Abandon_ExitStarted_RejectsRequest`) | 규칙("이탈 중에는 포기를 받지 않음")이 남는다. 버튼·확인창 대신 `TryRequestAbandon()`이 false인지 본다 |
| 〃 969 `Abandon_RequestIsClearedByDetachAndNextAttach` | 호출 맞춤 | `AbandonOpen()`·`AbandonConfirm()` → `TryRequestAbandon()` |
| 〃 1056 `ActionButtons_…ClearedAtTickAfterAbandonOpen` | 호출 맞춤 | 입력 밖 정리 경로가 `TryRequestAbandon`으로 남는다. `AbandonConfirmShown` 단언(`:1067`)을 뺀다 |
| 〃 1073 `ActionButtons_HiddenBySuspendAndFault_AndClickIgnoredWhileAbandonConfirmOpen` | `:1098-1101`(확인창 열기·버튼 누름·단언)만 삭제, 이름에서 `_AndClickIgnored…` 뺌 | 확인창이 없다. 그 위의 연결·Tick(`:1096-1097`)은 남겨 뒤의 오류 정지 단언(`:1103-1104`)이 연결된 rig에서 돌게 한다 |
| 〃 가짜 표시의 포기 멤버(`:1679-1702`) | 삭제 | 걷은 기능의 가짜. `FakeDisplay`를 상속한 가짜 11개는 이 멤버를 쓰지 않는다 |

`CombatAbandonPanel.cs`(`V/Display/Hud/`, 우리 파일)는 씬·프리팹 참조가 0이지만 이 계약과 무관하고, 삭제 금지 규칙이라 그대로 둔다(§8).

### C-2 진행 사실 클래스 분리

**지금(정리 §2·§9-1~4).**
- 제출 정지 `_submissionsSuspended`(`:91`), 완료 표시 `CompletionDisplay`(`:1100`), 보인 완료 `_shownCompletion`(`:95`)은 조정자 필드다. 포기 요청은 컨트롤러의 `IsRequested`다.
- 조정자가 `ICombatProgressView`를 직접 구현하고 `Progress => this`를 낸다(`:1098`).
- 제출 정지 함수(`:893-907`)가 값 하나를 바꾸고 입력·확인창·버튼까지 정리한다.
- 완료 확인 콜백(`:922-930`)이 한 곳에서 지난 연결 확인 + 상태 바꾸기 + 배경음 전환을 한다.
- 결과 유지 판정(`:1013-1018`)이 진행 사실 셋을 직접 섞는다.

**바꾼 뒤.** 새 파일 하나(위치 Q-C2): 진행 사실의 원본과 전이 규칙은 새 클래스에, 화면·입력 정리는 조정자에 둔다. 둘 사이는 조정자가 명시적으로 구현하는 좁은 계약 하나다.

```csharp
/// <summary>진행 사실 클래스가 조정자에게 묻고 알리는 것입니다. 조정자가 명시적으로 구현합니다.</summary>
internal interface ICombatProgressHost
{
    /// <summary>표시·재생이 연결돼 있는지입니다(첫 그리기 전에도 참, P1).</summary>
    bool IsAttached { get; }

    /// <summary>지금 이 완료를 보여도 되는지: 씬 활성·재생 연결, 확인하지 않은 결과 없음, Core가 기록한 완료와 같음.</summary>
    bool CanShowCompletion(BattleCompletionChanges completion);

    /// <summary>새 제출이 막 닫혔습니다. 배치 확인창 닫기·입력 표시 취소·입력 해제·행동 버튼 숨김을 합니다.</summary>
    void OnSubmissionsSuspended();

    /// <summary>
    /// 결과 화면을 엽니다. 사용자가 확인하면 연결이 그대로일 때만 <paramref name="confirm"/>을 부르고,
    /// 참이면 확인 뒤 처리(배경음)를 합니다.
    /// </summary>
    void ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm);
}

/// <summary>
/// 골격이 읽는 View 쪽 진행 사실의 원본입니다: 새 제출 정지, 완료 표시, 포기 요청.
/// 전이 규칙(한 번만 닫기, 같은 완료를 한 번만 표시·확인, 이탈 전에 한 번만 포기 요청)은 여기 한 곳이고, 화면·입력 정리는 호스트가 합니다.
/// 연결 사이(해제 뒤 다음 연결 전)에는 제출 정지의 마지막 값을 유지합니다(P2).
/// </summary>
internal sealed class CombatViewProgress : ICombatProgressView
{
    private readonly ICombatProgressHost _host;
    private BattleCompletionChanges _shownCompletion;

    internal CombatViewProgress(ICombatProgressHost host) => _host = host ?? throw new ArgumentNullException(nameof(host));

    public bool IsAttached => _host.IsAttached;
    public bool IsSubmissionSuspended { get; private set; }
    public CombatCompletionDisplay CompletionDisplay { get; private set; }
    public bool IsAbandonRequested { get; private set; }

    /// <summary>아직 포기를 요청하지 않았고 이탈(새 제출 정지)도 시작되지 않았는지입니다. 포기 가능 판단(정책)의 "이탈 전" 재료입니다.</summary>
    internal bool CanMarkAbandon => !IsAbandonRequested && !IsSubmissionSuspended;

    public void SuspendSubmissions()
    {
        if (IsSubmissionSuspended)
            return;
        IsSubmissionSuspended = true;
        _host.OnSubmissionsSuspended();
    }

    public void PresentCompletion(BattleCompletionChanges completion)
    {
        if (_shownCompletion != null || !_host.CanShowCompletion(completion))
            throw new InvalidOperationException("Completion 표시는 현재 전투의 일반 결과 확인 뒤 한 번만 가능합니다.");
        _shownCompletion = completion;
        CompletionDisplay = CombatCompletionDisplay.Showing;
        _host.ShowCompletion(completion, () => TryConfirm(completion));
    }

    /// <summary>포기 요청을 세웁니다. 부르는 쪽(포기 입구)이 <see cref="CanMarkAbandon"/>과 정책을 먼저 확인합니다.</summary>
    internal void MarkAbandonRequested()
    {
        if (!CanMarkAbandon)
            throw new InvalidOperationException("이미 포기를 요청했거나 이탈이 시작된 뒤에는 포기를 요청할 수 없습니다.");
        IsAbandonRequested = true;
    }

    /// <summary>이 완료를 보이는 중인지(제출 닫힘 ∧ 표시 시작 ∧ 같은 완료)입니다. 조정자의 결과 유지 판정이 씁니다.</summary>
    internal bool IsShowingCompletionOf(BattleCompletionChanges recorded) =>
        IsSubmissionSuspended && CompletionDisplay != CombatCompletionDisplay.None &&
        CombatCompletionIdentity.IsSame(_shownCompletion, recorded);

    /// <summary>표시 연결 시작: 지난 연결의 사실을 모두 지웁니다.</summary>
    internal void BeginConnection()
    {
        IsSubmissionSuspended = false;
        ClearShownFacts();
    }

    /// <summary>표시 연결 끝: 완료 표시·포기 요청을 지웁니다. 제출 정지는 다음 연결 시작까지 남깁니다(P2).</summary>
    internal void EndConnection() => ClearShownFacts();

    private void ClearShownFacts()
    {
        _shownCompletion = null;
        CompletionDisplay = CombatCompletionDisplay.None;
        IsAbandonRequested = false;
    }

    private bool TryConfirm(BattleCompletionChanges completion)
    {
        if (CompletionDisplay != CombatCompletionDisplay.Showing ||
            !CombatCompletionIdentity.IsSame(_shownCompletion, completion))
            return false;
        CompletionDisplay = CombatCompletionDisplay.Confirmed;
        return true;
    }
}
```

조정자 쪽:

```diff
 internal sealed class CombatViewSystem :
     ICombatViewService,
-    ICombatProgressView,
+    ICombatProgressHost,
     …
-private bool _submissionsSuspended;
-private BattleCompletionChanges _shownCompletion;
+// 골격이 읽는 진행 사실의 원본(C 단계 C-2). 조정자는 묻고 시키기만 합니다.
+private readonly CombatViewProgress _progress;

 // 생성자: 진행 객체를 포기 입구보다 먼저 만든다(골격 사실 읽기가 PostBind 때 Progress를 잡아 둔다, C/CombatProgressReader.cs:49)
+_progress = new CombatViewProgress(this);
-_abandon = new CombatAbandonController(_faults, () => ReadFacts(), () => IsSceneActive,
-    CancelInputPresentation, () => _submissionsSuspended);
+_abandon = new CombatAbandonController(_faults, () => ReadFacts(), () => IsSceneActive,
+    CancelInputPresentation, _progress);

 // TakeDisplay (줄 순서는 C-6 BeginConnectionState를 따른다)
-_submissionsSuspended = false;
-_shownCompletion = null;
-CompletionDisplay = CombatCompletionDisplay.None;
+_progress.BeginConnection();
 // Detach
-_shownCompletion = null;
-CompletionDisplay = CombatCompletionDisplay.None;
-_abandon.Detach();
+_progress.EndConnection();

-public void SuspendSubmissions() { … }
+void ICombatProgressHost.OnSubmissionsSuspended()
+{
+    _deploymentConfirmation?.Close();
+    CancelInputPresentation();
+    ReleaseInputOrReport();
+    // 제출 정지는 진행 계약의 공개 입구라 호출 시점과 관계없이 행동 버튼을 바로 숨긴다(SV6).
+    if (_display != null)
+        HideActions();
+}

-public void PresentCompletion(BattleCompletionChanges completion) { … }
+bool ICombatProgressHost.CanShowCompletion(BattleCompletionChanges completion) =>
+    CanContinue() && !HasPendingPresentation && CombatCompletionIdentity.IsSame(completion, _state.Completion);
+
+void ICombatProgressHost.ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm)
+{
+    ICombatBattleDisplay display = _display;
+    display.ShowCompletion(completion, () =>
+    {
+        // 오류 정지·지난 연결의 늦은 확인은 무시한다. 같은 완료·표시 중인지는 진행 쪽(confirm)이 본다.
+        if (!CanContinue() || !ReferenceEquals(_display, display) || !confirm())
+            return;
+        _audio?.SetPreset(AmbgmEvent.BGM, CombatEventSounds.BgmPreset(CombatBgmPhase.AfterCompletion));
+        if (_audio != null && !_audio.IsPlaying(AmbgmEvent.BGM)) _audio.Play(AmbgmEvent.BGM);
+    });
+}

-public ICombatProgressView Progress => this;
-public bool IsSubmissionSuspended => _submissionsSuspended;
-public CombatCompletionDisplay CompletionDisplay { get; private set; }
-public bool IsAbandonRequested => _abandon.IsRequested;
+public ICombatProgressView Progress => _progress;

 private bool IsHoldingResult()
 {
-    return !_state.HasBattle && _battleInstance == _state.BattleInstanceId && _submissionsSuspended &&
-        CombatCompletionIdentity.IsSame(_shownCompletion, _state.Completion) &&
-        CompletionDisplay != CombatCompletionDisplay.None;
+    return !_state.HasBattle && _battleInstance == _state.BattleInstanceId &&
+        _progress.IsShowingCompletionOf(_state.Completion);
 }
```

- `_submissionsSuspended`를 읽던 곳(`SyncInputLayer :866`, `CanReceiveDeckInput`, `CanEnterFromDeployment :1196`, `SyncActionButtons :1188-1191`)은 `_progress.IsSubmissionSuspended`를 읽는다.
- **포기 컨트롤러:** 상태 없는 입력 입구가 된다(가능 판단·입력 표시 취소·예외 보고, P4, 위치 Q-C2). "이탈 전" 규칙(`CanMarkAbandon`)과 깃발은 진행 클래스 한 곳에, 사실 조건은 정책 한 곳에 있다.

```diff
 internal bool TryRequestAbandon()
 {
-    if (!_canAct() || !CombatInputPolicy.CanRequestAbandon(_readFacts(), !IsRequested && !_isSubmissionSuspended()))
+    if (!_canAct() || !CombatInputPolicy.CanRequestAbandon(_readFacts(), _progress.CanMarkAbandon))
         return false;
     try
     {
         _cancelInputPresentation();
-        IsRequested = true;
+        _progress.MarkAbandonRequested();
         return true;
     }
     …
```

- 확인 콜백의 검사는 둘로 나뉜다. 조정자는 자기 사실(오류 아님·진입 활성·같은 표시)을, 진행 클래스는 자기 사실(표시 중·같은 완료)을 본다. 지금 한 줄의 네 조건(`:924-925`)과 같다.
  - 콜백이 동기로 바로 불려도, `Showing`을 먼저 세우므로 결과가 같다.
  - 배경음은 조정자에 남는다(P3).
- 포기 요청을 지우는 시점이 `ContinueAttach`에서 `TakeDisplay`(`BeginConnection`)로 옮겨진다. 둘은 같은 동기 연결 순서 안에 있고, 그 사이에 이 값을 읽는 곳이 없다(실패 경로도 같음).
- `IsAttached`는 조정자 속성으로 남는다. 호스트 멤버이고, 시험 23곳(`CombatViewSystemTests` 16·`CombatDisplayReuseTests` 7)이 그대로 읽는다.
- 진행 계약 `ICombatProgressView` remarks에 "연결 사이에는 마지막 값 유지"를 적는다(P2).

**동작:** 없음. 상태 이동과 검사 위치만 바뀐다. 다시 연결할 때 되돌리는 값과 시점도 같다.

**시험(Q-T1·Q-T2):** 조정자가 더는 `ICombatProgressView`가 아니어서 컴파일이 깨지는 곳만 맞춘다.
- **`CombatViewSystemTests`:** `_rig.View.SuspendSubmissions()`·`PresentCompletion`·`CompletionDisplay`·`IsSubmissionSuspended`·`IsAbandonRequested`를 쓰는 줄은 HEAD에 28줄이다. 이 중 3줄(`:934·950·991`)은 C-1에서 지우는 시험 안에 있어, 남는 **25줄**을 `_rig.View.Progress.…`로 맞춘다(우리 24, 현강님 `:963` 1).
- **`CombatMovementViewTests.cs:46·186·248`**(천명님 파일 안의 우리 줄, R3): `CombatProgressTestDriver(…, r.View)` → `r.View.Progress`.
- **그대로 둠:**
  - 가짜 진행 계약(`TestProgressView`·`FakeView`·`PresentationConsumer`·호진님 `CombatViewStub`)은 계약 모양이 같아 그대로다.
  - 운영 사용처는 이미 `.Progress`만 쓴다(`C/CombatProgressReader.cs:49`, `C/Plans/CombatStepContext.cs:75`). `CoreStageCombatCycleTests:246`도 이미 `.Progress`다.

### C-3 "재생 중" 판정 하나로 (M1)

**지금.**
- 골격: `CombatProgressFacts.HasUnpresentedResult => P < S`(`C/CombatProgressFacts.cs:99`), `IsPresenting => HasUnpresentedResult`(`:104`).
- 튜토리얼 판정: `CombatPlan.IsAwaitingPlayerCommand`가 `presented >= latest`를 따로 쓴다(`C/Plans/CombatPlan.cs:154`).
- 조정자: `IsPresenting(hasBattle) => _playback.IsBusy || (hasBattle && S > P)`(`:1021-1024`).
- 두 식이 같다는 근거는 진행 계약 주석의 불변식이다(`C/ICombatProgressView.cs` remarks): "재생은 P < S일 때만 시작하고, P를 올리는 것과 재생을 끝내는 것이 같은 호출 안"(`FinalizeResult :831-832`, `CombatResultPlayback.cs:53-118`).
- 조정자 식에는 문제가 하나 더 있다. 완료 표시(`PresentCompletion`)는 늘 정리(`EndBattle`) 바로 뒤라 전투가 없다(`C/Jobs/CombatExitJobs.cs:69·130`). 그래서 `hasBattle` 조건 때문에 완료 표시 앞의 "남은 재생" 검사(`HasPendingPresentation`, `:1095·916`)는 사실상 `_playback.IsBusy`만 본다.

**바꾼 뒤(Q-C3 A).** 뜻("확인하지 않은 결과가 있다")을 규칙 함수 하나로 두고, 세 곳이 그것을 부른다. View도 골격과 **같은 식**을 쓴다(`hasBattle`·`_playback.IsBusy`를 뺌).

```diff
 // CombatProgressFacts
+/// <summary>확인하지 않은 결과가 있는지(P &lt; S)의 규칙 원본입니다. 골격·튜토리얼·View가 이것을 부릅니다.</summary>
+internal static bool IsUnpresented(int presentedResultSequence, int latestResultSequence) =>
+    presentedResultSequence < latestResultSequence;
-public bool HasUnpresentedResult => PresentedResultSequence < LatestResultSequence;
+public bool HasUnpresentedResult => IsUnpresented(PresentedResultSequence, LatestResultSequence);

 // CombatPlan.IsAwaitingPlayerCommand
-phase == CombatFlowPhase.PlayerInput && presentedResultSequence >= latestResultSequence && !hasUnhandledCommand;
+phase == CombatFlowPhase.PlayerInput
+    && !CombatProgressFacts.IsUnpresented(presentedResultSequence, latestResultSequence) && !hasUnhandledCommand;

 // CombatViewSystem
-/// <summary>결과를 재생·후처리 중이거나 확인하지 않은 결과가 남았는지입니다(Playback busy 또는 S &gt; P).</summary>
-private bool IsPresenting(bool hasBattle)
-{
-    return _playback.IsBusy || (hasBattle && _state.LatestResultSequence > _inputRead.PresentedResultSequence);
-}
+/// <summary>
+/// 확인하지 않은 결과가 남았는지입니다. 재생·후처리는 이때만 하므로 "재생 중"도 이것과 같고(진행 계약 remarks), 골격과 같은 식입니다.
+/// 정리 뒤에도 Core가 결과 순번을 남기므로 완료 표시 앞의 "남은 재생" 검사에도 그대로 씁니다.
+/// </summary>
+private bool IsPresenting =>
+    CombatProgressFacts.IsUnpresented(_inputRead.PresentedResultSequence, _state.LatestResultSequence);
```

- 부르는 곳 셋을 맞춘다: `ReadFacts`(`isPresenting:`), `RouteInput`(`:1151`), `HasPendingPresentation`(`:1095`).
- 규칙 함수를 사실 묶음 형식에 두는 까닭: P < S의 뜻은 사실 묶음이 정의하고, Plan·튜토리얼·View는 그 뜻을 쓰는 쪽이다. View는 이미 같은 폴더의 `CombatCompletionIdentity`를 부른다.
- `_playback.IsBusy`는 재생기 자기 수명에만 남는다(`ConsumeResult`, `TickCore :336`). "후처리 중"(`IsFinalizing`)은 다른 사실이라 그대로다.

**동작:** 정상 흐름에서는 없다.
- 전투가 있을 때: 불변식대로 두 식이 같다.
- 전투가 없을 때: 지금은 "재생 중" = `_playback.IsBusy`, 바뀐 뒤는 P < S다.
  - 정리는 `IsPresenting`이 거짓일 때만 하고(Decide), 정리 뒤 S는 그대로라 P = S다. 그래서 정상 흐름에서는 둘 다 거짓이다.
  - 바뀌는 것은 계약 위반 때뿐이다(전투 없음 ∧ P < S). 이때 입력은 정책이 `HasBattle`로 이미 막고, 완료 표시는 이제 예외(F1)로 막힌다. 지금은 그냥 통과한다.

**시험:** 없음. `HasPendingPresentation`을 읽는 우리 줄(`CoreStageCombatCycleTests.cs:344-345`, 천명님 파일 안)도 결과 제시 뒤 P = S라 같다.

### C-4 제출 정지 주인·"끝" 판정 하나로 (M2)

**지금.**
- 제출 정지 깃발을 세우는 곳이 둘이다.
  - View: 새 결과를 처음 볼 때 `SyncSubmissionState`가 `CombatInputPolicy.IsTerminal`(`Finished ∨ 완료`, `V/Input/CombatInputPolicy.cs:27-30`)이면 깃발을 세운다(`:1070-1082`, 첫 그리기 `:531`에서도).
  - 골격: `CombatPlan.Decide` 1행이 `완료`이고 아직 열려 있으면 `CombatSuspendJob`을 낸다(`C/Plans/CombatPlan.cs:96-97`). 포기 블록(`:120`)도 같은 Job이다.
- 10-22 D3에서는 "같은 사실"이라는 주석만 달았다(`CombatPlan.cs:94-95`, `CombatViewSystem.cs:1073-1074`).
- 입력 정책은 이미 모든 명령·미리보기 입구에서 `IsTerminal`로 막는다.
  - 제출·미리보기 공통 문(`PassesCommon`), 목표 Layer(`GetDesiredLayer`), 경로 편집, 포기 가능, 배치 Drag 시작(`CanPreview`)
  - 정책 밖 입구는 표시만 하는 손패 호버·유닛 정보 둘이다(P5).
- 그래서 View 자체 닫기가 실제로 하는 일은 막기가 아니라 **"종료를 본 그 Tick에 입력 연결을 놓고 버튼을 숨기는 것"**이다.

**바꾼 뒤(Q-C4 A·Q-C5 A).**
- **깃발의 주인은 골격 하나.** View는 깃발을 세우지 않는다. 깃발과 그 정리(`OnSubmissionsSuspended`)는 골격 Job이 부를 때만 일어난다. "판단은 Plan 한 곳"(재구성 설계 J2)과 같은 방향이다.
- **View는 종료 결과를 처음 보면 입력만 바로 놓는다.** Layer 판단의 원본 `SyncInputLayer`가 "목표 Layer 없음"으로 표시 취소 + 해제를 한다. 종료가 아닌 결과에서 부르면 재생 전에 Layer가 바뀌므로, 종료일 때만 부른다.

```diff
 // TickCore
-// 새 결과를 처음 관측했다. 종료(Completion·Finished)와 포기 가능 여부는 결과 게시 때만 바뀌므로 여기서 한 번 맞춘다(D12).
-if (signal.UnpresentedResult != null && !_playback.IsBusy)
-{
-    SyncSubmissionState();
-    if (!CanContinue())
-        return;
-}
+// 새 결과가 승패를 기록했으면 입력 연결만 바로 놓는다(목표 Layer 없음). 새 제출 정지는 전투 Plan 1행이 정한다(C 단계 C-4).
+// 종료가 아닌 결과에서 SyncInputLayer를 부르면 재생 전에 Layer가 바뀌므로 종료일 때만 부른다.
+if (signal.UnpresentedResult != null && !_playback.IsBusy && CombatInputPolicy.IsTerminal(ReadFacts()))
+{
+    SyncInputLayer();
+    if (!CanContinue())
+        return;
+}

 // ShowBattle: 끝난 전투면 바로 아래 SyncInputLayer가 목표 Layer 없음으로 입력을 잇지 않는다
 _battleShown = true;
-SyncSubmissionState();
-if (!CanContinue())
-    return;
 SyncInputLayer();

-private void SyncSubmissionState() { … }
```

- **끝 = "승패 기록됨"(완료) 하나.**

```diff
 internal static bool IsTerminal(in CombatInputFacts facts)
 {
-    return facts.Phase == CombatFlowPhase.Finished || facts.HasCompletion;
+    return facts.HasCompletion;
 }
```

근거: Core가 Finished와 완료를 한 거래로만 게시한다는 것이고, 코드로 강제돼 있다.
- 받아들인 명령은 "끝났음"과 Finished가 같아야 한다(`InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatCommandTransition.cs:88`).
- 끝난 명령은 완료 제안 없이 게시할 수 없다(`BufferSystems/Core/BattleTransactions.cs:219`). 완료(`:228`)와 명령(`:230`)은 같은 `Apply` 거래 안에서 설치된다. 다른 경로에서 Finished가 생기지 않는다.
- 포기는 `CompleteBattle`(`:150-172`)로 완료만 남기고 국면은 그대로이며, Plan이 먼저 제출을 닫는다(`CombatPlan.cs:118-121`).
- 그래서 View가 기댈 사실은 완료 하나다.

주석도 이 근거를 가리키게 고친다: Plan 1행(`CombatPlan.cs:94-95`), `IsTerminal`(`:21-26`), 조정자 `Tick` 요약(`:274-279`), 클래스 remarks(`:46-48`).

**동작:** 화면·입력은 없다. 깃발이 서는 시점만 바뀐다.
- **같은 View Tick에 지금과 같게 일어나는 것:** 입력 해제와 입력 표시 취소. 그 Tick 끝에서는 행동 버튼이 숨고(입력 없음 → `CombatActionButtons.Read` 기본값) 배치 확인창이 닫힌다(진입 불가 → `CombatDeploymentConfirmation.Sync`가 닫음). 복귀·공격 버튼도 숨으므로, 1판 대안의 "보이되 눌리는 1프레임"은 없다.
- **깃발 시점:** 다음 Runner Tick의 골격 Job으로 늦어진다. 튜토리얼이 그 프레임에 Runner를 먼저 잡으면 더 늦다(`TutorialViewSystem.cs:227-230`). 그 사이 깃발을 읽는 곳은 모두 같은 결과를 낸다.
  - `SyncInputLayer`: 깃발이 있으면 바로 해제, 없으면 목표 Layer 없음으로 해제. 이미 풀려 있다.
  - 손패·가방 `CanReceiveDeckInput`, 배치 진입 `CanEnterFromDeployment`: 입력 해제로 `IsInputReady`가 거짓이다.
  - 행동 버튼: 입력이 없어 기본값이다.
  - `BattleChangerUI`(`:53`)·`PackUI`(`:91`): 완료로 숨긴다.
  - 포기 가능: 정책이 `!IsTerminal`로 막는다.
- **Finished인데 완료가 없는 상태**는 Core가 만들 수 없어 차이가 없다.

**시험(Q-T1):** 모두 우리 줄이다.

| 시험 | 처리 | 이유 |
|---|---|---|
| `CombatInputPolicyTests` `[TestCase(Finished, false, true)]`(`:16`) | 사례 삭제, 이름 `IsTerminal_UsesFinishedOrCompletionOnly` → `IsTerminal_UsesCompletionOnly` | 바뀐 동작("Finished만으로 끝") |
| 〃 `IsTerminal_IgnoresHasBattle_…`(`:30`) | 입력 맞춤: `phase: Finished` → `hasCompletion: true` | 보는 내용(HasBattle 무시)은 그대로 |
| 〃 `GetDesiredLayer_TerminalWinsOverStagePhase` `[TestCase(Finished, false)]`(`:59`) | 사례 삭제 | 완료 사례가 남는다 |
| 〃 `CanRequestAbandon_…` `[TestCase("finished", true, false)]`(`:233`) | 사례 삭제 | `"completion"` 사례가 남는다 |
| `CombatViewSystemTests` `FirstDisplay_OfFinishedBattle_DoesNotBindInput`(`:278`) | 입력 맞춤: 완료를 넣음 | 보는 내용(끝난 전투의 첫 그리기는 입력을 잇지 않음)은 그대로 |
| 〃 `Finished_ReleasesInputOnly_…`(`:615`) | 입력 맞춤: 완료를 넣음 | 보는 내용(입력만 놓고 재생·확인은 계속)은 그대로. View가 그 Tick에 입력을 놓으므로 골격 역할 호출은 넣지 않는다 |
| 〃 641·659·902, Policy 82·145, `CombatInteractionTests.Terminal_BlocksEverything_…`(`:289`) | 변경 없음 | `SuspendSubmissions`를 직접 부르거나 국면 검사(PlayerInput만, `PlacementDragInteraction.cs:263`·`CombatInputPolicy.cs:57·78`)로 막힌다 |

실제 Core를 쓰는 View 시험은 승패까지 가지 않는다.

### C-5 조용한 분리를 F1로 (M9)

**지금(`:303-310`).**
- 결과 유지가 아닌데 전투가 사라지거나 다른 전투로 바뀌면 표시를 비우고 `Detach`한다. 오류는 남기지 않는다.
- 그 뒤 골격은 `IsViewAttached`가 거짓이라 할 일이 없어(`Decide :98`) 오류 없이 멈춘다.

**정상 흐름에서 닿지 않는 근거(검증됨).**
- 운영에서 `EndBattle`을 부르는 곳은 `CombatEndBattleJob` 하나다(`C/Jobs/CombatExitJobs.cs:130`).
  - 그 Job은 `CombatPlan.Settle`에서 챕터 완료·완료 표시와 기다림 없이 이어진다(`C/Plans/CombatPlan.cs:211-221`).
  - 같은 우선순위 Job은 들어온 순서대로 한 Runner Tick에 이어 돈다(JobQueue, Tick당 최대 32개).
  - 그래서 다음 View Tick은 결과 유지(`IsHoldingResult`)로 위에서 돌아간다.
  - "같은 Tick" 주장은 Runner 예산에 기댄다. 다른 Root Job이 몰려 예산이 찰 수 있는지는 확인하지 못했다(추측).
- `EndBattle` 뒤에도 Core는 전투 번호와 완료를 남긴다(`BufferSystems/Combat/CombatStateBuffer.cs:167-172`). 완료를 지우는 곳은 새 Run 시작뿐이다(`ProgressTransactions.cs:189`).
- 같은 씬에 다음 전투를 붙이지 않는다(D9). 다른 전투가 되는 것은 씬 이탈(`ReleaseScene` → `Detach`) 뒤다.
- 정리 중 실패는 Job 예외라 이미 F1이고, 오류 상태면 `CanContinue`가 맨 위에서 거른다. 앱 종료 때는 정지 뒤 Tick이 없다.

**바꾼 뒤.** 이 주석 블록(`:299-303`)은 호진님 줄이다(`cb76bd2c`, §7).

```diff
-// 그 밖에 전투가 사라지거나 다른 전투로 바뀌면 표시를 비우고 씬 연결을 끊는다. 같은 씬에 다음 전투를 다시 붙이지 않는다(D9).
+// 그 밖에 전투가 사라지거나 다른 전투로 바뀌는 것은 정상 흐름에 없다. 정리와 완료 표시는 같은 Runner Tick에 이어지고(CombatPlan.Settle)
+// 같은 씬에 다음 전투를 다시 붙이지 않는다(D9). 조용히 끊으면 골격이 "연결 안 됨"으로 오류 없이 멈추므로, 원인을 먼저 F1로 보고한 뒤 정리한다.
 if (!_state.HasBattle || _battleInstance != _state.BattleInstanceId)
 {
+    _faults.Report(new InvalidOperationException(_state.HasBattle
+        ? "[CombatViewSystem] 연결한 전투가 아닌 다른 전투가 설치됐습니다."
+        : "[CombatViewSystem] 완료를 보이기 전에 전투가 정리됐습니다."));
     if (!_state.HasBattle)
         _display.Clear();
     Detach();
     return;
 }
```

보고한 뒤의 순서(비우기 → 분리)는 지금과 같다. Phase 6의 씬 루트 F1(`OnCombatSceneDisabled`)과 같은 꼴이다.

**동작:** 계약 위반 때만 바뀐다(조용한 분리 → F1 + 분리).

**시험:** 깨지는 곳 없음.
- 우리 `BattleRemoved_DetachesAndReleasesInput`(`CombatViewSystemTests.cs:291`)·`BattleRemoved_ClearsDisplay_AndDetachReleasesItOnce`(`:1308`)는 입력 해제·비우기 1회·반환 1회를 본다. F1 뒤에도 같다. 시험 rig는 로그를 목록에 모으므로 Unity 로그 실패도 없다.
- 천명님 `CombatMovementViewTests.BattleEndBeforePointerCallback_CancelsWithoutMappingMissingState`(`:364`)는 오류 경계가 아무것도 하지 않는 rig(`_ => {}`)이고, 명령 없음·Layer 해제만 보아 통과한다. 이 시험이 그리던 장면(전투 중 정리)은 이제 F1 경로다. 보고만 한다(§7).

### C-6 씬·연결 수명 정리 (Q-B5, 낮음)

**지금(정리 §4·§5·§9-6·10·11).**
- 수명이 셋(씬·연결·서비스)이고 해제 입구가 둘(`Detach`·`ReleaseScene`)이다.
- 씬 수명 메서드(`OnCombatSceneLoaded :706`, `OnCombatSceneEntered :726`)와 해제(`ReleaseScene :634`, `ReleaseBackdrop :642`)가 연결 코드 사이에 흩어져 있다.
- 연결 상태 되돌리기가 `TakeDisplay`(`:491-499`)와 `Detach`(`:614-623`)에 줄 단위로 나뉘어 있다. 시작은 8개, 해제는 6개를 되돌린다.
- 씬 루트 참조가 둘이다: `_loadedRoot`(씬 수명)와 `_sceneRoot`(이벤트 구독).

**바꾼 뒤(Q-C7 A: 새 클래스 없이).**
- **씬 수명:** `#region 씬 수명(−195 ~ ReleaseScene)`에 −195 Node·Enter 100·`ReleaseScene`·`ReleaseBackdrop`을 모은다(Q-B5대로 −195를 "씬 연결" 묶음에 넣음).
- **연결 수명:** `#region 연결 수명(TakeDisplay ~ Detach)` 안의 상태 되돌리기를 함수 둘로 둔다. **실패 경로 순서를 지키려고 `TakeDisplay`의 입력 Buffer 비우기는 함수에 넣지 않는다.** 지금처럼 연출·재생 연결보다 먼저 비워, 연결이 실패해도 지난 전투의 명령은 이미 버려진 상태로 남는다.

```csharp
// TakeDisplay: 사전 검사 → _inputWrite.Clear() → _presentation.Attach(…) → _playback.Attach() → BeginConnectionState() → _display = display

/// <summary>연결 시작의 상태입니다. 관측·첫 그리기·진행 사실을 새로 시작하고 이 전투 번호를 기억합니다. 입력 Buffer는 연출 연결 전에 이미 비웠습니다.</summary>
private void BeginConnectionState()
{
    _refresh.Reset();
    _progress.BeginConnection();
    _battleShown = false;
    _battleInstance = _state.BattleInstanceId;
}

// Detach: … _display = null → _interaction·_gate·_router = null → EndConnectionState() → 연출 연결 끊기 → 표시 반환
//   (입력 Buffer 비우기가 HEAD처럼 대입 뒤 마지막에 오게. 1판·계획 첫 글은 EndConnectionState를 null 대입 앞에 뒀다 — Phase C-c에서 바꾸고 사용자가 유지 결정, 10-23)

/// <summary>
/// 연결 끝의 상태입니다. 시작과 같은 것을 되돌리되 둘은 남깁니다:
/// 전투 번호(다음 시작이 덮어씀)와 제출 정지(연결 사이에는 마지막 값 유지, P2).
/// </summary>
private void EndConnectionState()
{
    _battleShown = false;
    _progress.EndConnection();
    _refresh.Reset();
    _inputWrite.Clear();
}
```

`Detach` 쪽은 단순 대입 사이에서 줄 순서만 바뀌므로 결과가 같다(실패할 수 있는 호출은 앞뒤로 옮기지 않음).
- `_sceneRoot` → `_subscribedRoot`(구독 수명이라는 뜻). 닿는 줄은 `:440·593-600·674-675·1188-1189`이고, 대부분 현강님 줄이다(§7). `_loadedRoot`는 그대로다.

**동작:** 없음. 메서드 위치·이름과 되돌리기 묶음만 바뀐다.

**시험:** 없음. C 단계가 바꾸는 private 필드(`_sceneRoot`·`_submissionsSuspended`·`_shownCompletion`·`_abandon` 등)를 반사로 읽는 시험은 없다. 호진님 `HojinCombatAttackDragIntegrationTests.cs:128-129`이 조정자의 `_interaction`·`_display`를 반사로 읽지만, C는 이 둘을 바꾸지 않는다.

### C-7 `ICombatViewService` 좁히기 (M6)

**지금.**
- 계약 멤버 10개 중 `Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn`(`ICombatViewService.cs:23-27`, 천명님 `e0b02cc7`)은 바깥 운영 호출이 0이다. 안에서(로딩 노드·씬 이벤트·Tick) 부르거나 시험이 부른다.
- `CaptureLost`는 `FocusLost`를 부를 뿐이다(`CombatViewSystem.cs:1120`, 천명님 줄).
- UI는 `DeckHover`·`DeckPlacement`·`ItemInput`·`TryRequestAbandon`만 쓰고, 골격은 `Progress`만 쓴다.

**바꾼 뒤(Q-C8 A).**

```diff
 public interface ICombatViewService : IServiceModule, ICombatAbandonRequestService
 {
     ICombatProgressView Progress { get; }
     ICombatDeckPlacementInput DeckPlacement { get; }
     ICombatItemInput ItemInput { get; }
     ICombatDeckHoverInput DeckHover { get; }
-    void Attach(CombatSceneRoot scene);
-    void Detach();
-    void FocusLost();
-    void CaptureLost();
-    void RequestReturn();
 }
```

- **조정자:**
  - `Attach`·`Detach`·`FocusLost`·`RequestReturn`은 `internal`로 둔다. `FocusLost`는 씬 루트 이벤트가, 나머지는 로딩 노드·시험이 부른다.
  - `RequestReturn`은 시험 입구로만 남는다(운영 복귀는 복귀 키(입력 슬롯)·복귀 버튼이 수신 창구로 바로 들어감). 같은 일을 하는 시험 입구 `Input.RequestReturn()`(`:1112`)이 이미 있지만, 남기면 천명님 줄 셋(`CombatMovementViewTests` 152·227·484)을 고치지 않아도 된다. 그래서 남기고, "시험 입구"라고 주석을 단다(점수에서는 완전 해소로 세지 않음, §6).
  - `CaptureLost`는 지운다.
- 진행 계약은 지금처럼 `Progress` 속성으로 낸다(P7).
- 시험 가짜(`TestProgressView`·`FakeView`·`PresentationConsumer`·호진님 `CombatViewStub`)는 그대로 둔다. 빠진 멤버가 공개 메서드로 남아도 컴파일된다(명시적 구현 없음, 검증됨).

**동작:** 없음.

**시험(Q-T1·Q-T2):**

| 시험 | 줄 주인 | 처리 |
|---|---|---|
| `CombatViewSystemTests.FocusOrCaptureCleanupException_…`(`:718-735`) | 우리 | `capture` 사례 삭제(포커스 사례는 그대로) |
| `CombatMovementViewTests.cs:394` `group.Combat.Attach(null)` | 천명님 | → `owner.Attach(null)`(같은 시험 위 줄의 캐스트 변수) |
| `CombatMovementViewTests.FocusOrCaptureLoss_CancelsFrozenDraft`(`:347-356`) | 천명님 | capture 사례 삭제 |
| `CoreStageCombatCycleTests.cs:355` `view.Combat.RequestReturn()` | 천명님 | → `((CombatViewSystem)view.Combat).RequestReturn()`(같은 파일에 같은 캐스트가 이미 있음) |

`CombatMovementRangeFeedbackTests:183`·`MovementDraftTests:314`의 `CaptureLost`는 `CombatPathInput` 것이라 무관하다. 천명님 줄을 덜 고치려면 Q-C8 B(`CaptureLost`를 내부로 남김)를 고른다. 그러면 394·355 두 줄만 바뀐다.

### C-8 환경 호버 허용 식을 정책으로 (낮음)

**지금(`:1169-1171`, 현강님 줄).** 조정자가 사실 다섯과 Router 대상 판정을 직접 조립한다.

**바꾼 뒤.** 사실 조건만 정책 함수로 옮긴다. Router 대상 판정은 호출 자리에 남긴다. 정책의 기존 관례(`CombatInputPolicy.cs:114-117`: "대상 판정은 입구가 따로 확인")를 따르고, 정책 → Router 의존을 만들지 않으려는 것이다.

```diff
 // CombatInputPolicy
+/// <summary>환경 설명 호버를 보일 수 있는 사실인지입니다. 표시가 끝난 결과만 설명합니다.</summary>
+/// <remarks>이 식만으로 보이지 않습니다. 수신 가능·포인터 대상(<see cref="CombatInputRouter.ResolvePointerTarget"/>)은 호출자가 따로 확인합니다.</remarks>
+internal static bool CanShowEnvironmentHover(in CombatInputFacts facts) =>
+    facts.IsBattleDisplayed && facts.HasBattle && !facts.IsPresenting && !facts.IsFinalizing && !IsTerminal(facts);

 // CombatViewSystem.SyncEnvironmentHover
-bool allowed = CanReceiveDeckInput() && facts.IsBattleDisplayed && facts.HasBattle
-    && !facts.IsPresenting && !facts.IsFinalizing && !facts.HasCompletion
-    && CombatInputRouter.ResolvePointerTarget(facts.CurrentLayer, facts.IsAttackActive) != CombatInputRouter.PointerTarget.None;
+bool allowed = CanReceiveDeckInput() && CombatInputPolicy.CanShowEnvironmentHover(facts)
+    && CombatInputRouter.ResolvePointerTarget(facts.CurrentLayer, facts.IsAttackActive) != CombatInputRouter.PointerTarget.None;
```

`!facts.HasCompletion`은 C-4 뒤 `!IsTerminal(facts)`와 같다.

**그대로 두는 것.** 행동 버튼·호버를 Tick 끝과 입력 직후 두 번 맞추는 것은 SV6 결정(입력 즉시 반영)이라 바꾸지 않는다(P6).

**동작·시험:** 없음.

## 3. 정할 것

권장은 모두 A다.

| # | 묻는 것 | A (권장) | B | C |
|---|---|---|---|---|
| **Q-C1** | C-2 구조 | 진행 사실 클래스 + 좁은 호스트 계약 넷. 포기 요청 깃발과 "이탈 전" 규칙(`CanMarkAbandon`)도 이 클래스에 두고, 포기 입구는 정책 확인·표시 취소·예외 보고만 함 | 포기 요청은 컨트롤러에 두고 진행 클래스가 읽음(진행 사실 원본이 두 클래스) | 새 클래스 없이 조정자 안에서 이름 있는 묶음만 |
| **Q-C2** | 새 파일 `CombatViewProgress.cs`(호스트 계약 같은 파일)와 상태가 없어진 포기 컨트롤러의 위치 | 진행 클래스는 `Input/` 바로 아래(`NS.Input`, 입력 Gate·판단용 사실과 같은 자리, internal이라 루트 자리가 아님). 포기 컨트롤러는 `Input/ActionButtons/` → `Input/UiEntry/`(다른 작업자 UI가 부르는 입구의 구현, `.meta` GUID 유지, MonoBehaviour 아님) | 진행 클래스는 루트(12 → 13개, 배치 안내 5번 규칙을 고쳐 루트 예외를 적음), 컨트롤러는 그대로 | — |
| **Q-C3** | C-3 View 쪽 식 | 골격과 같은 식 하나(`IsUnpresented(P, S)`만, `hasBattle`·`_playback.IsBusy` 뺌) | 규칙 함수를 쓰되 `_playback.IsBusy ∨ (전투 있음 ∧ …)`은 남김 | 주석만 |
| **Q-C4** | C-4 제출 정지 깃발 | 깃발은 골격 하나. View는 종료 결과를 처음 보면 입력만 바로 놓음(`SyncInputLayer`, 지금과 같은 프레임) | 깃발은 골격 하나, View는 아무것도 안 함(입력이 1 Runner Tick 이상 연결된 채 남고, 그동안 복귀·공격 버튼이 눌림. 정책은 무시함) | 지금처럼 둘 다 깃발 |
| **Q-C5** | C-4 "끝" 식 | 완료 하나(`IsTerminal = HasCompletion`) | 지금(`Finished ∨ 완료`) | — |
| **Q-C6** | C-5 | 계약 위반을 F1로 보고한 뒤 분리 | 그대로(조용한 분리) | — |
| **Q-C7** | C-6 | 새 클래스 없이 묶음·되돌리기 함수·필드 이름 | 씬 수명 클래스(새 파일 하나 더) | — |
| **Q-C8** | C-7 | 다섯을 빼고 `CaptureLost` 삭제(천명님 3줄) | 다섯을 빼고 `CaptureLost`는 내부로 남김(천명님 2줄) | 그대로 |
| **Q-T1** | 우리 시험 처리 | 10-22 Q1과 같게: 걷은 기능·바뀐 동작을 보던 우리 시험(사례)은 지우고, 남는 내용은 호출·입력만 맞춤 | 지우지 않고 모두 고쳐 남김 | — |
| **Q-T2** | 다른 사람 시험 줄 | 고침 허락: 현강님 1줄(C-2 `:963`), 천명님 3줄(C-7, Q-C8 B면 2줄) | 고치지 않음 — 그 항목을 그 사람 시험이 깨지지 않는 선택지로 바꿈 | — |

## 4. 설계에서 정한 것 (확인 대상)

- **P1** 골격이 보는 "연결됨"은 지금처럼 첫 그리기 **전**에 참이다(`TakeDisplay`). 첫 그리기 끝(`_battleShown`)은 조정자 내부 사실로 남는다.
- **P2** 제출 정지는 연결 해제 뒤에도 다음 연결 시작까지 남긴다. 진행 계약에 "연결 사이에는 마지막 값 유지"로 적는다.
  - 근거: 지금 동작이 그렇고(동작 동일), 지금 운영 흐름에서는 해제 뒤 이 값을 다시 쓰는 경로가 없다.
    - Combat Exit −100(View 해제)과 0(LifeCycle 이탈)은 한 번의 노드 실행 안에서 동기로 이어 돈다(`LodingSystem.cs:149·167-171`).
    - `LifeCycleManager.Change`는 다음 모듈을 먼저 활성으로 바꾸고(`:68`), Tick은 활성 모듈만 돈다(`:104`). 그래서 그 사이에 Combat Plan이 등록·실행되지 않는다.
    - 전투 중간의 해제(전투 사라짐·씬 루트 꺼짐)는 C-5·Phase 6으로 모두 F1이 먼저 나서 Plan이 더 등록하지 않는다.
  - 1판의 "지우면 다음 씬 Layer를 지울 수 있다"는 근거는 운영에서 닿지 않아 뺐다. 그 뿌리(`ClearLayerIfSet`이 남의 Layer도 지울 수 있음)는 §8에 보고만 한다.
- **P3** 결과 화면 확인 뒤 배경음 전환은 조정자(호스트)에 둔다. 소리는 View 쪽 일이고 진행 사실이 아니다.
- **P4** 포기 컨트롤러는 상태 없는 입력 입구다. 하는 일은 정책 확인·`CanMarkAbandon` 확인·입력 표시 취소·예외 보고다. 위치는 Q-C2.
- **P5** C-4 뒤 끝을 보는 입력 입구 목록을 Phase C-b 기록에 남긴다.
  - 정책으로 막는 곳: `PassesCommon`·`GetDesiredLayer`·`CanEditPath`·`CanRequestAbandon`·`CanInspectUnit`, 배치 Drag 시작(`CanPreview`), 환경 호버(C-8 정책 함수)
  - 정책 밖에서 표시만 하는 곳: 손패 호버(`CombatDeckHoverInput.Hover`), 유닛 정보(`InspectUnit`, `RouteInput :1151`, `!IsPresenting`만 봄). 둘 다 지금과 같다.
- **P6** 행동 버튼·호버의 두 번 맞춤(SV6)은 그대로다.
- **P7** 진행 계약은 `Progress` 속성으로 낸다. 서비스 키를 더하지 않으므로 민형님 공유가 없다.
- **P8** 시험 가짜는 컴파일이 깨지지 않으면 손대지 않는다(빠진 계약 멤버가 공개 메서드로 남는 것 포함). C-1의 가짜 포기 멤버만 걷은 시험과 함께 지운다(Q-T1).

## 5. Phase 초안

Phase마다 멈추고 보고한다. 각 Phase는 컴파일 미확인으로 보고하고, 사용자가 Unity에서 컴파일·EditMode·재생을 확인한다.

| Phase | 항목 | 바뀌는 파일 | 시험 | 누적 예상 |
|---|---|---|---|---|
| **C-a** | C-1·C-2 | `ICombatAbandonDisplay.cs`(+ `.meta`) 삭제, `ICombatBattleDisplay.cs`, `CombatSceneDisplay.cs`(현강님 줄), 새 `CombatViewProgress.cs`(+ `.meta`), `CombatAbandonController.cs`(Q-C2 A면 이동, `.meta` 함께), `CombatViewSystem.cs`, `ICombatProgressView.cs`(주석) | `CombatPanelControllerTests.cs`(+ `.meta`) 삭제, `CombatViewSystemTests.cs`(지움·맞춤 + `.Progress` 25줄, 현강님 1), `CombatMovementViewTests.cs` 우리 3줄 | 7.6 |
| **C-b** | C-3·C-4 | `CombatProgressFacts.cs`, `CombatPlan.cs`(규칙 호출·1행 주석), `CombatInputPolicy.cs`, `CombatViewSystem.cs`(`Tick` 요약·클래스 remarks 포함) | `CombatInputPolicyTests.cs`, `CombatViewSystemTests.cs` | 7.8 |
| **C-c** | C-5·C-6 | `CombatViewSystem.cs`(호진님 주석 줄·현강님 `_sceneRoot` 줄 포함) | 없음 | 8.0 |
| **C-d** | C-7·C-8 | `ICombatViewService.cs`(천명님 멤버 줄), `CombatViewSystem.cs`, `CombatInputPolicy.cs` | `CombatViewSystemTests.cs`, 천명님 `CombatMovementViewTests.cs`·`CoreStageCombatCycleTests.cs` | 8.1 |
| **C-e** | 문서 | 공유 메모(§7), 배치 안내(`viewDocs/combat-view-folder-guide.md`, 새 위치·Q-C2 결과), CLAUDE.md, 정리 문서 머리말 "C 단계 뒤 바뀜" | — | — |

순서 이유:
- C-a에서 C-1과 C-2를 합친다. 포기 컨트롤러가 중간 형태(`Reset()`)를 거치지 않고 바로 상태 없는 입구가 된다. 포기·모달 판정이 줄어든 자리에 진행 클래스가 들어간다.
- C-b는 그 클래스 위에서 판정을 하나로 합친다(규칙·정책 파일이 따로라 나눔).
- C-c는 결과 유지 판정이 옮겨진 뒤라, 분리 경로·수명 묶음을 같이 본다.
- C-d는 계약에서 뺄 것이 앞에서 정해진다.

## 6. 원칙 점검과 점수

**SSOT.**
- "확인하지 않은 결과" 규칙이 함수 하나가 되고, View와 골격이 같은 식을 쓴다(C-3).
- 제출 정지는 원본 하나(진행 클래스), 깃발 주인 하나(골격), 사실 하나(완료)가 된다(C-4).
- 진행 사실 셋과 "이탈 전" 규칙이 한 클래스에 있다(C-2).
- 남는 것: 진행 사실을 읽는 길이 셋(골격 사실 읽기·튜토리얼·조정자, M3). 이것은 골격 단계 K1이다.

**SOLID.**
- 조정자에서 진행 계약 구현이 빠지고(바뀌는 이유 하나 줄어듦), 계약 멤버 다섯이 빠진다(ISP, M6).
- 호스트 계약은 진행 클래스가 아는 조정자 기능을 넷으로 제한한다.
- 남는 것: 조정자 크기(M4), Bootstrap(M5).

**FW 계약.**
- 서비스·모듈 등록, 서비스 키, Factory는 그대로다. 진행 클래스는 모듈이 아니라 조정자가 소유한다.
- 진행 객체는 조정자 생성자에서 만든다. 골격 사실 읽기가 PostBind 때 `Progress`를 잡아 두기 때문이다(`CombatProgressReader.cs:49`).
- Plan이 판단하고 View가 따르는 방향이 C-4로 한 갈래가 된다. 계약 위반은 F1로 드러난다(C-3 완료 표시 앞, C-5).

**있는 기능 사용.** 새 장치가 없다. `CombatCompletionIdentity`·정책·`SyncInputLayer`·오류 경계를 그대로 쓴다.

**기능 변경의 독립성 — 대표 변경 시나리오 전후 비교(인계 §4.6).**

| 시나리오 | 지금: 고칠 곳 / 읽을 곳 | C 뒤: 고칠 곳 / 읽을 곳 | 남는 공통 경계 |
|---|---|---|---|
| 결과 화면 확인 규칙 변경(예: 확인 두 단계) | 조정자 `PresentCompletion`(상태·표시·소리 한 함수) / 조정자 연결·재생 수명 | 진행 클래스 `PresentCompletion`·`TryConfirm` / 호스트 계약 넷 | `ICombatProgressView`, 호스트 `ShowCompletion` |
| 완료 뒤 배경음·연출 변경 | 조정자 확인 콜백(진행 상태 검사와 섞임) | 조정자 `ShowCompletion` 구현만 / 진행 규칙은 읽지 않아도 됨 | 같음 |
| 포기 조건 중 사실 조건 변경(예: 배치 단계 금지) | 정책 `CanRequestAbandon` | 같음 | 정책 |
| 포기 조건 중 이탈 조건 변경 | 컨트롤러 `CanRequestNow`(조정자 제출 정지 대리자) | 진행 클래스 `CanMarkAbandon` | — |
| 골격이 읽는 진행 사실 추가 | 진행 계약 + 조정자(1,217줄) + 가짜 | 진행 계약 + 진행 클래스(필요하면 호스트 계약 한 멤버) + 가짜 | 호스트 계약 |
| 종료 처리 순서 변경 | Plan 1행 + View `SyncSubmissionState` | Plan 1행(깃발) / View는 Layer 판단 `SyncInputLayer` 한 곳 | 정책 `IsTerminal` |
| "재생 중" 의미 변경 | 골격 사실 묶음 + Plan 튜토리얼 판정 + 조정자 식 | 규칙 함수 하나 | 진행 계약 remarks의 불변식 |

포기 조건 변경은 고칠 파일 수가 줄지 않는다(사실 조건은 정책, 이탈 조건은 진행 클래스). 다만 한 변경이 한 파일에 머문다.

**불필요·우회.**
- 닿지 않는 포기 경로(M11)와 중복 입구 `CaptureLost`가 빠진다.
- 시험 입구로만 남는 `RequestReturn`은 그대로다(같은 일을 하는 `Input.RequestReturn()`이 있음).

**새로 생기는 문제(낮음).**
- 진행 클래스 ↔ 조정자가 서로를 안다(진행 → 호스트 계약, 조정자 → 진행 클래스). 호스트 계약을 넷으로 좁혀 줄였다.
- 1판 검증에서 나온 새 낮음 넷은 2판에서 풀었다(C-3 무의미한 검사, C-4 틈, 포기 규칙 두 클래스, 정책 → Router 의존, §9).

**점수(재평가 감점 방식, 높음 −2·중간 −0.75·낮음 −0.25):**

| 기준 | 지금 | C 뒤 | 근거 |
|---|---|---|---|
| SSOT | 6.75 | 8.25 | M1·M2 해소 |
| SOLID | 6.75 | 7.5 | M6 해소, 진행 계약 분리(+0.25), 새 양방향(−0.25), M4·M5 남음 |
| FW 계약 | 6.75 | 7.5 | M9 해소(이 기준으로 가정), H1 미룸 |
| 있는 기능 사용 | 9.25 | 9.25 | — |
| 독립성 | 7.75 | 7.75 | 위 시나리오는 나아지지만, 감점(M8·M10)은 골격 단계 몫 |
| 불필요·우회 | 7.5 | 8.25 | M11, `CaptureLost`(+0.25), 시험 입구 `RequestReturn` 남음(−0.25) |
| **합** | **7.5** | **약 8.0(7.9~8.1)** | H1까지 고치면 약 8.4 |

M9가 어느 기준의 감점이었는지는 평가 보고서(저장소 밖)를 다시 확인하지 못했다. 대화 기록의 요약으로 추정했다. 다른 기준이어도 합은 같다.

## 7. 다른 사람 줄과 공유 (Phase C-e, 모두 미전달로 남김)

줄 주인은 HEAD `git blame`으로 확인했다(중괄호뿐인 줄은 뺐다).

**현강님** — 10-22 메모 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md`에 절을 더한다.
- `CombatSceneDisplay.cs:291-300`(현강님 줄 291·293-298, `9354c522`): 빈 포기 메서드 region을 지운다. Option 이관 뒤 남은 `ICombatAbandonDisplay`도 지운다. (`:306` 주석의 포기 참조도 지우지만 그 줄은 우리 줄이다.)
- `CombatAbandonController.cs:152-179` 중 현강님 줄(`9354c522`, 중괄호 빼고 13줄: 지운 `Confirm()` 몸통과 `TryRequestAbandon` 본문): 확인창 닫기·버튼 맞춤 호출을 빼고, 요청 깃발을 진행 클래스로 옮긴다(Q-C2 A면 파일이 `Input/UiEntry/`로 감).
- `CombatViewSystem.cs`의 현강님 줄:
  - 259(모달, C-1)
  - 898(제출 정지 때 배치 확인창 닫기 → 호스트 `OnSubmissionsSuspended`, C-2)
  - 929(완료 확인 배경음 → 호스트 `ShowCompletion`, C-2)
  - 1054-1055(손패 수신, C-1)
  - 1105-1106(`TryRequestAbandon`, 그대로 위임)
  - 1151(`RouteInput`의 유닛 정보 열기 조건 `!IsPresenting(_state.HasBattle)` → `!IsPresenting`, `80eb7481`, C-3, Phase C-b 검토에서 찾음)
  - 1169-1170(환경 호버, C-8)
  - 1188-1189(행동 버튼 맞춤, C-1·C-2·C-6 이름)
  - 1196(배치 진입, C-1·C-2)
  - 595-596·674-675(`_sceneRoot` 이름, C-6)
- `ICombatAbandonRequestService`(`ICombatViewService.cs:6-10`)는 그대로다.
- `PackUI.cs:91`의 `!facts.HasCompletion && !CombatInputPolicy.IsTerminal(facts)`는 C-4 뒤 같은 조건 둘이다. 고칠 필요 없이 알림만 한다.
- 시험 956의 한 줄(`:963`, `.Progress.IsAbandonRequested`, Q-T2).

**천명님** — Core 전달 문서 `handoffs/2026-09-24-view-core-data-boundary-handoff.md`에 새 절을 단다. 요청은 없다.
- `ICombatViewService.cs:23-27`의 멤버 다섯(`e0b02cc7`)을 계약에서 빼고, `CombatViewSystem.cs:1120` `CaptureLost`(`e0b02cc7`)를 지운다(C-7). 나머지 넷은 조정자 내부 입구로 남는다.
- `CombatViewSystem.cs:86` 필드 선언 `_sceneRoot`(`e0b02cc7`): C-6에서 이름을 `_subscribedRoot`로 바꿨다(Phase C-c 검토에서 찾음).
- `CombatViewSystem.cs:379` `Attach`·`:587` `Detach`(`e0b02cc7`): 계약에서 빠져 `public` → `internal`이 됐다(Phase C-d 검토에서 찾음).
- 시험 줄: `CombatMovementViewTests.cs:347-349`(`[TestCase]` 둘·서명 → `[Test]`, 이름 그대로)·`:356`(capture 갈래 삭제)·`:394`(`owner.Attach(null)`), `CoreStageCombatCycleTests.cs:355`(캐스트)(Q-T2·Q-C8).
- 기대는 Core 동작: "Finished와 완료는 한 `Apply` 거래"(`CombatCommandTransition.cs:88`, `BattleTransactions.cs:219·228·230`). View가 끝을 완료 하나로 본다.
- `BattleEndBeforePointerCallback_…`(`:364`)이 그리던 장면은 이제 F1 경로다(고치지 않음, 통과함).

**호진님** — 10-22 메모 `handoffs/2026-10-22-combat-view-structure-hojin-share.md`에 절을 더한다.
- `CombatViewSystem.cs:299-303` 주석(`cb76bd2c`, 보스 경로 때 쓴 결과 유지 설명): C-5가 `:303` 줄을 "정상 흐름에 없음 → F1" 설명으로 다시 쓴다. 결과 유지 블록(`:299-301`)의 동작은 그대로다.
- `CombatViewSystem.cs:912` 문서 주석("지난 수명의 확인은 무시합니다", `PresentCompletion` 요약): C-2로 진행 클래스 `PresentCompletion`과 호스트 `ShowCompletion` 요약으로 옮겼다(Phase C-a 검토에서 찾음).
- 시험 `CombatViewStub`·`HojinCombatAttackDragIntegrationTests`는 영향이 없다.

**민형님:** 없음. 연결 줄 변경이 없다(P7).

## 8. 보고만 (삭제·수정하지 않음)

- `V/Display/Hud/CombatAbandonPanel.cs`(우리 파일, 10-07·10-10): 씬·프리팹 참조가 0이다. 이 계약을 구현하지 않고 자기 `Bind`를 가진다. 쓰는 곳은 우리 시험 `CombatDisplayReuseTests.AbandonPanel_ButtonsReachEntriesThroughEventSystem_NotOnClick` 하나다. 삭제 금지 규칙이라 그대로 둔다(10-18 R9와 같은 대상).
- `CombatViewSystem.ClearLayerIfSet`(`:954`): 현재 Layer가 무엇이든 비운다. 그래서 연결이 없는 View에 해제 호출이 오면 다른 씬이 세운 Layer를 지울 수 있다. 지금 운영 흐름에서는 닿지 않는다(P2 근거). 고친다면 "내가 세운 Layer일 때만 비움"이 맞는데, C 단계 범위 밖이라 후보로만 둔다.
- C 단계 밖: 조정자 남은 책임(M4), H1, 골격 K1~K8.

## 9. 검증 반영 기록 (1판 → 2판, 2026-10-23)

하위 에이전트 2개(사실 대조·설계 타당성, 읽기 전용)가 1판을 검증했다. 둘이 갈린 줄 주인은 직접 `git blame`으로 확인했다. 높음은 없었다.

**정정(사실).**
1. **줄 주인·공유 대상(중간):** `ICombatViewService`의 멤버 다섯과 `CaptureLost`는 천명님 줄이다(1판은 현강님). 선언 줄은 우리 줄이다. 빠졌던 현강님 줄(`CombatViewSystem.cs` 여러 곳, `TryRequestAbandon` 본문)과 호진님 줄(`:299-303`)을 §7에 더했다.
2. **P2 근거(낮음):** "다음 씬 Layer를 지울 수 있다"는 운영에서 닿지 않는다(노드 동기 실행·활성 모듈 전환·중간 분리 F1). 결정은 유지하고 근거를 고쳤으며, 뿌리는 §8에 보고만 한다.
3. **C-6 순서(낮음):** `TakeDisplay`의 입력 Buffer 비우기는 연출·재생 연결보다 먼저다. 함수에 넣으면 실패 경로가 달라지므로 밖에 남겼다.
4. **숫자·인용(낮음):**
   - C-2 줄 수: HEAD 28 → C-1 뒤 25. 1판 §1의 "32줄"은 오기였다.
   - `BattleTransactions` 인용: 228·230
   - `IsAttached`를 읽는 시험: 23곳
   - 시험 1073에서 지울 범위: `:1098-1101`만
   - `CombatSceneDisplay` 범위: region 291-300 + `:306`
   - `ICombatBattleDisplay` 주석: 17-18
   - P5 목록에 손패 호버·유닛 정보 추가
   - 반사 문장을 "바꾸는 필드를 읽는 시험 없음"으로 좁힘
   - 1판의 "앞 목록의 C-8" 참조는 정리 문서에 없어 지움
5. **과장(중간):** "골격 점검 범위가 그 클래스까지"를 "읽을 View 쪽 코드가 진행 클래스 + 호스트 구현 넷으로 줄어듦"으로 고쳤다. 인계 §4.6의 대표 변경 시나리오 비교를 더했다. 점수를 약 8.1에서 약 8.0으로 낮췄다.

**권장안 변경(사용자 지시로 반영).**
1. **Q-C3:** 1판 A(`hasBattle ∧ 규칙`)는 완료 표시 앞의 "남은 재생" 검사가 늘 통과하는 결함이 있었다. 1판의 "C-5가 F1로 잡는다"는 설명도 틀렸다(결과를 보이는 동안 결과 유지 판정이 먼저 돌아감). 그래서 A를 골격과 같은 식(`IsUnpresented(P, S)`만)으로 바꿨다.
2. **Q-C4:** 1판 A(View는 아무것도 안 함)는 입력이 1 Runner Tick 이상 연결된 채 남고(튜토리얼이 Runner를 잡으면 더), 그동안 복귀·공격 버튼이 눌렸다. 1판의 "누를 수 없음"은 틀렸다. A를 "깃발은 골격, View는 종료 결과에 입력만 바로 놓음"으로 바꿨다. 1판 A는 B로 남겼다.
3. **Q-C1:** 포기 "이탈 전" 규칙이 컨트롤러와 진행 클래스로 나뉘던 것을 `CanMarkAbandon`으로 진행 클래스 한 곳에 모았다. `MarkAbandonRequested`는 위반 시 예외를 던진다.
4. **C-8:** 정책 → Router 의존을 만들지 않게, 대상 판정은 호출 자리에 남겼다.
5. **Q-C2:** 진행 클래스를 루트 대신 `Input/` 바로 아래로, 상태 없는 포기 컨트롤러를 `Input/UiEntry/`로 권장한다. 루트는 B로 남겼다(배치 안내 갱신 필요). C-e 문서 목록에 배치 안내를 더했다.
6. **Phase:** C-1과 C-2를 합쳐 C-a로 했다(중간 형태 `Reset()` 없음). 주석 갱신 목록(`Tick` 요약·클래스 remarks·Plan 1행)을 C-b에 넣었다. Phase는 다섯이 됐다.
7. **C-7:** `RequestReturn`은 같은 일을 하는 시험 입구가 이미 있다는 점을 적고, 점수에서 완전 해소로 세지 않았다.
