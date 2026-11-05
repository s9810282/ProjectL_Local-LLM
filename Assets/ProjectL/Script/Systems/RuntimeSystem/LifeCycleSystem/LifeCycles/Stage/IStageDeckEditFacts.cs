namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 덱 편집 창이 읽는 Stage 흐름의 사실입니다. 값은 매 호출마다 원본(수명·Flow·Buffer)에서 읽고 따로 저장하지 않습니다.
    /// 어떤 사실이 어떤 불가 이유가 되는지는 <see cref="StageDeckEditWindow"/> 한 곳이 정합니다.
    /// </summary>
    internal interface IStageDeckEditFacts
    {
        bool IsFaulted { get; }

        /// <summary>활성 LifeCycle이 Stage인지입니다.</summary>
        bool IsStageActive { get; }

        bool IsLoading { get; }

        bool IsRunnerIdle { get; }

        /// <summary>Flow 전체나 맵·상점·회복 도메인 하나가 정지했는지입니다.</summary>
        bool IsFlowStopped { get; }

        /// <summary>맵 그래프가 준비됐는지입니다.</summary>
        bool IsMapPrepared { get; }

        /// <summary>Run 로스터가 설치됐는지입니다.</summary>
        bool HasRoster { get; }

        /// <summary>등록한 맵·상점·회복 요청의 처리 확인을 기다리는지입니다.</summary>
        bool HasPendingFlowCheck { get; }

        /// <summary>제출됐지만 아직 등록하지 않은 맵·상점·회복 요청이 있는지입니다.</summary>
        bool HasUnregisteredRequest { get; }

        /// <summary>들어간 노드가 아직 클리어되지 않았는지입니다(상점·회복·전투 등 노드 상호작용 진행 중).</summary>
        bool HasOpenInteraction { get; }
    }
}
