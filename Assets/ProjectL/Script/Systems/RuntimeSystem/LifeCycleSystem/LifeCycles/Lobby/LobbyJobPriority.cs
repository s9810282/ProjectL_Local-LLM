namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby
{
    /// <summary>
    /// 로비 Job 묶음의 Runner 우선순위입니다. Runner는 작은 값부터 돌고, 같은 값은 등록 순서를 지킵니다.
    /// 묶음 안의 Job은 이 값 하나를 함께 읽어 "같은 우선순위라 등록 바로 뒤에 돈다"는 순서 계약을 지킵니다
    /// (로비 리팩토링 2차 설계 M-07).
    /// </summary>
    internal static class LobbyJobPriority
    {
        /// <summary>아티팩트 편집·프리뷰 호출 Job과 그 처리 기록 확인 Job입니다. 확인은 호출 바로 뒤에 돕니다.</summary>
        internal const int Artifact = 3;

        /// <summary>출발의 새 Run 준비 Job과 Stage 전환 Job입니다. 전환은 새 Run 바로 뒤에 돕니다.</summary>
        internal const int Departure = 0;
    }
}
