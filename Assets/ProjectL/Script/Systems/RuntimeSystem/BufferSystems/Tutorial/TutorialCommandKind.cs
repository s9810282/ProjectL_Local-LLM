namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>튜토리얼 요청 Buffer에 제출하는 명령의 종류입니다(튜토리얼 설계 §6).</summary>
    public enum TutorialCommandKind
    {
        /// <summary>기본값입니다. 보관되는 명령에는 쓰이지 않습니다.</summary>
        None = 0,
        /// <summary>튜토리얼 세션을 시작합니다.</summary>
        Start = 1,
        /// <summary>표시된 설명 하나를 사용자가 확인했습니다.</summary>
        Continue = 2,
        /// <summary>외부 사유로 세션을 중단합니다.</summary>
        Cancel = 3,
        /// <summary>입력 제한 때문에 막힌 조작이 있었음을 알립니다.</summary>
        ReportBlockedInput = 4
    }
}
