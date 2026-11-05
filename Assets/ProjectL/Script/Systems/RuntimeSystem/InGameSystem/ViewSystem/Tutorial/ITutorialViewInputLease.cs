namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 설명 구간 동안 튜토리얼 입력을 빌리고 돌려주는 좁은 계약입니다(튜토리얼 설계 §15.4.3). 실행 소유자만 부릅니다.
    /// 구현은 <see cref="TutorialViewInputLease"/>(튜토리얼 Layer 빌리기와 UI 선택 비우기)입니다.
    /// </summary>
    internal interface ITutorialViewInputLease
    {
        /// <summary>다른 기능이 입력을 독점하고 있는지입니다. 지금은 현재 Layer가 설정 Layer인지로 판정합니다(설계 §10 한계).</summary>
        bool IsMonopolized { get; }

        /// <summary>빌린 상태인지입니다.</summary>
        bool IsBorrowed { get; }

        /// <summary>
        /// 빌립니다. 빌리기 직전에 다른 기능이 입력을 잡고 있으면(같은 프레임 경합) false입니다. 설명 시작 Job이 바로 앞에서
        /// 시작 조건(입력 독점 포함)을 다시 확인하므로 정상 흐름에서는 오지 않으며, 오면 세션을 Faulted(ExecutionFault)로 끝냅니다(설계 §8.3).
        /// 독점 여부를 알 수 없는 다른 실패는 예외입니다(모든 예외를 경합으로 보지 않음). 이미 빌린 상태면 예외입니다.
        /// </summary>
        bool TryBorrow();

        /// <summary>돌려줍니다. 빌리지 않았으면 아무것도 하지 않습니다. 실패는 예외이며, 실패해도 빌린 상태는 놓아 다시 시도하지 않습니다.</summary>
        void Return();
    }
}
