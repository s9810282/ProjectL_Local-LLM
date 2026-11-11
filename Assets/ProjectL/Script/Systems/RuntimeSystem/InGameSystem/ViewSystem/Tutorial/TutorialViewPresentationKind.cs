namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>튜토리얼 실행 소유자에게 요청하는 표시의 종류입니다(튜토리얼 설계 §8 `Presentation`).</summary>
    public enum TutorialViewPresentationKind
    {
        /// <summary>기본값입니다. 요청에는 쓰지 않습니다.</summary>
        None = 0,
        /// <summary>설명 상자와 덮개를 띄웁니다. 끝나면 설명이 사용자 확인을 기다립니다.</summary>
        Explanation = 1,
        /// <summary>덮개를 닫고 상단 실습 안내를 띄웁니다. 끝나면 실습이 통과 조건을 기다립니다.</summary>
        Practice = 2,
        /// <summary>사용자 확인을 받은 설명을 닫습니다. 끝나면 다음 단계로 넘어갑니다.</summary>
        Transition = 3
    }
}
