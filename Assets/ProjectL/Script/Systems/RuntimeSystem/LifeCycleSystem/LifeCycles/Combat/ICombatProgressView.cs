using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>승패 완료 표시가 어디까지 왔는지입니다. 새 씬 연결(Attach)에서 None으로 돌아갑니다.</summary>
    public enum CombatCompletionDisplay
    {
        None = 0,
        Showing = 1,
        Confirmed = 2
    }

    /// <summary>
    /// 전투 진행 판단이 View에게 묻는 상태와 시키는 동작입니다(재구성 설계 J12).
    /// Buffer에 없는 View 상태만 둡니다. 명령·미리보기 번호와 연출 확인 번호(P)는 CombatViewBuffer에서 읽습니다.
    /// </summary>
    public interface ICombatProgressView
    {
        /// <summary>Combat 씬에 연결돼 있는지입니다.</summary>
        bool IsAttached { get; }

        /// <summary>결과를 재생하거나 후처리하는 중인지입니다. 확인하지 않은 결과(P &lt; S)는 Buffer로 따로 봅니다.</summary>
        bool IsPlaybackBusy { get; }

        /// <summary>새 입력 제출을 닫았는지입니다.</summary>
        bool IsSubmissionSuspended { get; }

        /// <summary>새 입력 제출만 닫습니다. 진행 중인 재생과 제출된 명령은 그대로 둡니다. 여러 번 불러도 됩니다.</summary>
        void SuspendSubmissions();

        CombatCompletionDisplay CompletionDisplay { get; }

        /// <summary>현재 전투의 승패를 한 번 표시합니다. 사용자가 확인하면 <see cref="CompletionDisplay"/>가 Confirmed가 됩니다.</summary>
        void PresentCompletion(BattleCompletionChanges completion);

        /// <summary>사용자가 포기를 확인했는지입니다.</summary>
        bool IsAbandonRequested { get; }
    }
}
