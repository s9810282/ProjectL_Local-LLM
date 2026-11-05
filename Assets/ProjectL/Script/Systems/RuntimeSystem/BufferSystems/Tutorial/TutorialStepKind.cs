namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>정의가 단계마다 정하는 종류입니다(튜토리얼 설계 §8).</summary>
    public enum TutorialStepKind
    {
        /// <summary>첫 단계 이전이거나 단계를 떠난 종료 정리 중입니다.</summary>
        None = 0,
        /// <summary>설명 단계입니다. 사용자 확인이나 5초 만료로 넘어갑니다.</summary>
        Explanation = 1,
        /// <summary>실습 단계입니다. 게임 결과로만 통과합니다.</summary>
        Practice = 2
    }
}
