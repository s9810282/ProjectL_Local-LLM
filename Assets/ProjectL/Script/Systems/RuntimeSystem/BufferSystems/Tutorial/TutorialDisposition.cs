namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>요청 하나의 처리 결과 구분입니다(튜토리얼 설계 §7).</summary>
    public enum TutorialDisposition
    {
        /// <summary>기본값입니다. 게시되는 결과에는 쓰이지 않습니다.</summary>
        None = 0,
        /// <summary>명령을 받아들였습니다. 기능의 완료를 뜻하지는 않습니다.</summary>
        Accepted = 1,
        /// <summary>형식은 맞지만 지금 상태에서 받을 수 없는 정상 거절입니다.</summary>
        Rejected = 2,
        /// <summary>실행 중 오류나 런타임 정지로 처리하지 못했습니다.</summary>
        Failed = 3
    }
}
