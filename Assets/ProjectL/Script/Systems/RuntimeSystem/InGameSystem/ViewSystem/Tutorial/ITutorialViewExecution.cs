namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// TutorialPlan·Job이 튜토리얼 실행 소유자에게 요구하는 좁은 창구입니다(튜토리얼 설계 §9.1·§10, 구현 계획 T-3).
    /// 사실은 실행 소유자가 기록한 것을 읽기만 하고, 실행은 한 걸음만 요청합니다. 단계·종료 결정은 TutorialPlan이 합니다.
    /// 실행 소유자 구현은 T-4입니다.
    /// </summary>
    /// <remarks>
    /// 표시·정리 완료는 비동기일 수 있습니다. 완료 사실은 다음 실행 기회의 TutorialPlan이 읽습니다.
    /// 세션·revision이 지금과 다른 완료 통지는 실행 소유자가 버립니다(설계 §8.3).
    /// </remarks>
    public interface ITutorialViewExecution
    {
        /// <summary>다른 기능이 입력을 독점하고 있는지입니다. 지금은 현재 Layer가 설정 Layer인지로 판정합니다(설계 §8.1 조건 5·§10).</summary>
        bool IsInputMonopolized { get; }

        /// <summary>그 단계에 마지막으로 요청한 표시가 끝났는지와 그 종류입니다.</summary>
        bool TryGetCompletedPresentation(int sessionId, int stepRevision, out TutorialViewPresentationKind kind);

        /// <summary>그 세션의 정리가 끝났는지와 성공 여부입니다. 실패는 입력·화면 막·UI 선택을 돌려주지 못한 경우입니다(설계 §8.3).</summary>
        bool TryGetCleanupResult(int sessionId, out bool succeeded);

        /// <summary>
        /// 튜토리얼 입력 Layer를 빌리고 UI 선택을 비웁니다. 같은 프레임 경합으로 빌리지 못하면 false입니다(설계 §8.3).
        /// 그 밖의 오류는 예외입니다.
        /// </summary>
        bool TryBorrowInput();

        /// <summary>빌린 입력 Layer를 돌려주고 UI 선택을 되돌립니다. 빌리지 않았으면 아무것도 하지 않습니다.</summary>
        void ReturnInput();

        /// <summary>표시를 시작합니다. 같은 단계에 새 표시를 요청하면 앞의 완료 사실은 버립니다.</summary>
        void BeginPresentation(int sessionId, int stepRevision, int tutorialKey, int stepKey,
            TutorialViewPresentationKind kind);

        /// <summary>입력 제한 때문에 막힌 조작이 있었다는 안내를 잠시 띄웁니다. 단계와 진행은 바꾸지 않습니다.</summary>
        void ShowBlockedInputNotice(int sessionId, int stepRevision, int tutorialKey, int stepKey);

        /// <summary>세션이 가진 입력·화면 막·UI 선택·표시를 정리하기 시작합니다. 끝나면 <see cref="TryGetCleanupResult"/>가 답합니다.</summary>
        void BeginCleanup(int sessionId);
    }
}
