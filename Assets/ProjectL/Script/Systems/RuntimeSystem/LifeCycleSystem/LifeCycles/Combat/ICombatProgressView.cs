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
    /// <remarks>
    /// 결과 재생 중인지는 묻지 않습니다. View는 확인하지 않은 결과(P &lt; S)가 있을 때만 결과 재생을 시작하고, P를 올리는 것과 재생을 끝내는 것이
    /// 같은 호출 안이므로 골격이 읽는 어느 시점에서도 "재생 중이면 P &lt; S"입니다(개선 설계 §7.1 D4).
    /// 결과를 만들지 않는 진입 연출은 별도 대기 프로퍼티로 읽습니다.
    /// 연결 사이(해제 뒤 다음 연결 전)에는 제출 정지의 마지막 값을 유지하고, 다음 씬 연결 시작이 지웁니다(C 단계 설계 P2).
    /// </remarks>
    public interface ICombatProgressView
    {
        /// <summary>Combat 씬에 연결돼 있는지입니다.</summary>
        bool IsAttached { get; }

        /// <summary>로딩 종료를 기다리거나 보드·초기 적 등장 연출이 남아 있는지입니다. 결과 확인 번호(P)와 별개입니다.</summary>
        bool IsEntryPresentationPending { get; }

        /// <summary>새 입력 제출을 닫았는지입니다.</summary>
        bool IsSubmissionSuspended { get; }

        /// <summary>새 입력 제출만 닫습니다. 진행 중인 재생과 제출된 명령은 그대로 둡니다. 여러 번 불러도 됩니다.</summary>
        void SuspendSubmissions();

        CombatCompletionDisplay CompletionDisplay { get; }

        /// <summary>
        /// 현재 전투의 승패를 한 번 표시합니다. 모든 결과는 정리(보스 승리면 챕터 완료까지) 뒤 보존된 완료를 표시합니다.
        /// 사용자가 확인하면 <see cref="CompletionDisplay"/>가 Confirmed가 됩니다.
        /// </summary>
        void PresentCompletion(BattleCompletionChanges completion);

        /// <summary>사용자가 포기를 확인했는지입니다.</summary>
        bool IsAbandonRequested { get; }
    }
}
