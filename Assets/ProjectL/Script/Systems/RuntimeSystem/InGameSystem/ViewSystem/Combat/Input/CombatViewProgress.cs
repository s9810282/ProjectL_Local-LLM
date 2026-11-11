using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 진행 사실(<see cref="CombatViewProgress"/>)이 조정자에게 묻고 알리는 것입니다(C 단계 설계 C-2). 조정자가 명시적으로 구현합니다.
    /// </summary>
    internal interface ICombatProgressHost
    {
        /// <summary>표시·재생이 연결돼 있는지입니다. 첫 그리기 전에도 참입니다(C 단계 설계 P1).</summary>
        bool IsAttached { get; }

        bool IsEntryPresentationPending { get; }

        /// <summary>지금 이 완료를 보여도 되는지입니다: 씬 활성·재생 연결, 확인하지 않은 결과 없음, Core가 기록한 완료와 같음.</summary>
        bool CanShowCompletion(BattleCompletionChanges completion);

        /// <summary>
        /// 새 입력이 막 닫혔습니다(제출 정지 또는 포기 요청, <see cref="CombatInputPolicy.IsInputClosed"/>).
        /// 확인창(배치·턴 종료) 닫기·입력 표시 취소·입력 해제·행동 버튼 숨김을 합니다. 두 사실이 차례로 서면 두 번 불리므로 멱등적이어야 합니다.
        /// </summary>
        void OnInputClosed();

        /// <summary>
        /// 결과 화면을 엽니다. 사용자가 확인하면 연결이 그대로일 때만 <paramref name="confirm"/>을 부르고,
        /// 그것이 참이면 표시가 확인 뒤 처리(완료 뒤 배경음)를 합니다(View 2라운드 W6-5).
        /// </summary>
        void ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm);
    }

    /// <summary>
    /// 전투 진행 골격이 읽는 View 쪽 진행 사실의 원본입니다: 새 제출 정지, 완료 표시, 포기 요청(C 단계 설계 C-2, 재구성 설계 J12).
    /// 전이 규칙(한 번만 닫기, 같은 완료를 한 번만 표시·확인, 이탈 전에 한 번만 포기 요청)은 여기 한 곳이고,
    /// 화면·입력 정리는 호스트(<see cref="ICombatProgressHost"/>)가 합니다.
    /// </summary>
    /// <remarks>
    /// 조정자가 생성자에서 만들어 <c>ICombatViewService.Progress</c>로 냅니다. 골격 사실 읽기가 서비스 연결 때 이 참조를 잡아 둡니다.
    /// 연결 사이(해제 뒤 다음 연결 전)에는 제출 정지의 마지막 값을 유지하고, 다음 연결 시작이 지웁니다(C 단계 설계 P2).
    /// </remarks>
    internal sealed class CombatViewProgress : ICombatProgressView
    {
        private readonly ICombatProgressHost _host;
        private BattleCompletionChanges _shownCompletion;

        internal CombatViewProgress(ICombatProgressHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public bool IsAttached => _host.IsAttached;

        public bool IsEntryPresentationPending => _host.IsEntryPresentationPending;

        public bool IsSubmissionSuspended { get; private set; }

        public CombatCompletionDisplay CompletionDisplay { get; private set; }

        /// <summary>사용자가 포기를 확인했는지입니다. 씬 연결 시작·끝에 false로 돌아갑니다.</summary>
        public bool IsAbandonRequested { get; private set; }

        /// <summary>아직 포기를 요청하지 않았고 이탈(새 제출 정지)도 시작되지 않았는지입니다. 포기 가능 판단(정책)의 "이탈 전" 재료입니다.</summary>
        internal bool CanMarkAbandon => !IsAbandonRequested && !IsSubmissionSuspended;

        /// <summary>새 입력 제출만 닫습니다. 진행 중인 재생과 제출된 명령은 그대로 둡니다. 멱등적입니다.</summary>
        public void SuspendSubmissions()
        {
            if (IsSubmissionSuspended)
                return;
            IsSubmissionSuspended = true;
            _host.OnInputClosed();
        }

        /// <summary>
        /// 현재 전투의 승패를 한 번 표시합니다. 모든 결과는 Core의 정리(보스 승리면 챕터 완료까지)를 마친 뒤 같은 완료를 표시합니다.
        /// 사용자가 확인하면 <see cref="CompletionDisplay"/>가 Confirmed가 되고, 다음 판단에서 전투 Plan이 전환을 합니다.
        /// 지난 수명의 확인은 무시합니다(재구성 설계 J7·J12).
        /// </summary>
        public void PresentCompletion(BattleCompletionChanges completion)
        {
            if (_shownCompletion != null || !_host.CanShowCompletion(completion))
                throw new InvalidOperationException("Completion 표시는 현재 전투의 일반 결과 확인 뒤 한 번만 가능합니다.");
            _shownCompletion = completion;
            CompletionDisplay = CombatCompletionDisplay.Showing;
            _host.ShowCompletion(completion, () => TryConfirm(completion));
        }

        /// <summary>
        /// 포기 요청을 세우고 새 입력을 그 자리에서 닫습니다(View 단계 설계 W2, 결정 Q-V3 A). 부르는 쪽(포기 입구)이 <see cref="CanMarkAbandon"/>과 정책을 먼저 확인합니다.
        /// 실제 포기는 전투 Plan이 남은 처리·표시를 끝낸 뒤 합니다(재구성 설계 J8).
        /// </summary>
        internal void MarkAbandonRequested()
        {
            if (!CanMarkAbandon)
                throw new InvalidOperationException("이미 포기를 요청했거나 이탈이 시작된 뒤에는 포기를 요청할 수 없습니다.");
            IsAbandonRequested = true;
            _host.OnInputClosed();
        }

        /// <summary>이 완료를 보이는 중인지(제출 닫힘 ∧ 표시 시작 ∧ 같은 완료)입니다. 조정자의 결과 유지 판정이 씁니다.</summary>
        internal bool IsShowingCompletionOf(BattleCompletionChanges recorded) =>
            IsSubmissionSuspended && CompletionDisplay != CombatCompletionDisplay.None &&
            CombatCompletionIdentity.IsSame(_shownCompletion, recorded);

        /// <summary>표시 연결 시작입니다. 지난 연결의 사실을 모두 지웁니다.</summary>
        internal void BeginConnection()
        {
            IsSubmissionSuspended = false;
            ClearShownFacts();
        }

        /// <summary>표시 연결 끝입니다. 완료 표시·포기 요청을 지웁니다. 제출 정지는 다음 연결 시작까지 남깁니다(P2).</summary>
        internal void EndConnection() => ClearShownFacts();

        private void ClearShownFacts()
        {
            _shownCompletion = null;
            CompletionDisplay = CombatCompletionDisplay.None;
            IsAbandonRequested = false;
        }

        /// <summary>표시 중인 같은 완료일 때만 Confirmed로 바꾸고 참을 돌려줍니다.</summary>
        private bool TryConfirm(BattleCompletionChanges completion)
        {
            if (CompletionDisplay != CombatCompletionDisplay.Showing ||
                !CombatCompletionIdentity.IsSame(_shownCompletion, completion))
                return false;
            CompletionDisplay = CombatCompletionDisplay.Confirmed;
            return true;
        }
    }
}
