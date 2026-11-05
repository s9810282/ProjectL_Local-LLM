namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Plans
{
    /// <summary>로비 판단이 고른 다음 단계의 종류입니다(로비 재구성 설계 §4.3).</summary>
    internal enum LobbyStepKind
    {
        /// <summary>지금은 할 일이 없습니다. 다음 Tick에 다시 판단합니다.</summary>
        None = 0,
        /// <summary>처리 안 된 편집 N을 Core에 넘기고 바로 기록을 확인합니다.</summary>
        ArtifactEdit = 1,
        /// <summary>답 안 된 프리뷰 N을 Core에 넘기고 바로 기록을 확인합니다.</summary>
        ArtifactPreview = 2,
        /// <summary>제출된 출발로 새 Run을 시작하고 Stage로 전환합니다.</summary>
        Depart = 3,
        /// <summary>Runner가 빈 뒤에도 출발 요청이 진행 단계에 남았습니다. Job이 요청을 마감하지 못한 계약 위반입니다.</summary>
        Broken = 4
    }

    /// <summary>다음 단계와 그 단계의 번호입니다. 아티팩트는 요청 번호, 출발은 출발 요청 번호입니다.</summary>
    internal readonly struct LobbyStep
    {
        internal static readonly LobbyStep Nothing = new(LobbyStepKind.None, 0);

        internal LobbyStep(LobbyStepKind kind, int number)
        {
            Kind = kind;
            Number = number;
        }

        internal LobbyStepKind Kind { get; }

        internal int Number { get; }

        public override string ToString() => Number == 0 ? Kind.ToString() : $"{Kind}({Number})";
    }
}
