namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>
    /// 적 AI 탐색·점수의 코드 상수다(적 AI 근간 개편 설계 6.5.1). 값을 바꾸면 결정 단위 시험과 운영 스테이지 행동 모음으로 확인한다.
    /// 기획자가 고치는 용감함·접근 값은 조정 에셋이 맡는다(계획 R-3).
    /// </summary>
    internal static class EnemyAiTuning
    {
        /// <summary>
        /// 교전 필드에서 아군이 선 칸에 들어가는 추가 비용이다(설계 6.3). 이번 턴 이동은 여전히 막히지만,
        /// 여러 턴 접근 추정에서는 아군이 비켜 줄 수 있다고 보고 영구 벽으로 세지 않는다.
        /// </summary>
        internal const int AllySoftCost = 2;

        /// <summary>같은 적 진영 단계의 후속 공격(이동 뒤 공격) 이득 가중치다(설계 6.5). 지금 공격과 같은 무게다.</summary>
        internal const long FollowupWeight = 1;
    }
}
