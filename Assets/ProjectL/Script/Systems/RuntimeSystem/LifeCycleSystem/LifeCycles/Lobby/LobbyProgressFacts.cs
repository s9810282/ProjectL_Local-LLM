using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby
{
    /// <summary>
    /// 로비의 다음 할 일과 이탈 가능 여부를 정할 때 쓰는 사실 묶음입니다(로비 재구성 설계 §4.6).
    /// <see cref="LobbyProgress"/>가 부를 때마다 새로 읽어 만들며, 저장하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 판단 시점의 일시적 사본입니다. 원본은 로딩 서비스, 아티팩트 Buffer·도메인 정지 칸, 로비 출발 요청입니다.
    /// 값 형식이라 매 Tick 읽어도 할당이 없습니다.
    /// </remarks>
    public readonly struct LobbyProgressFacts
    {
        internal LobbyProgressFacts(
            bool isLoading,
            bool isBoardInitialized,
            bool isArtifactStopped,
            int artifactCommandNumber,
            int artifactPreviewNumber,
            LobbyDepartureRequest departure)
        {
            IsLoading = isLoading;
            IsBoardInitialized = isBoardInitialized;
            IsArtifactStopped = isArtifactStopped;
            ArtifactCommandNumber = artifactCommandNumber;
            ArtifactPreviewNumber = artifactPreviewNumber;
            Departure = departure;
        }

        /// <summary>씬 로딩 중입니다.</summary>
        public bool IsLoading { get; }

        /// <summary>아티팩트 보드가 처음 크기로 준비됐습니다.</summary>
        public bool IsBoardInitialized { get; }

        /// <summary>이번 로비 진입에서 기록 누락으로 아티팩트 도메인이 멈췄습니다(D11).</summary>
        public bool IsArtifactStopped { get; }

        /// <summary>Core 처리 번호 다음 첫 편집 입력의 번호입니다. 없으면 0입니다.</summary>
        public int ArtifactCommandNumber { get; }

        /// <summary>아직 답하지 않은 최신 프리뷰의 번호입니다. 없으면 0입니다.</summary>
        public int ArtifactPreviewNumber { get; }

        /// <summary>지금 출발 요청입니다. 없으면 null입니다.</summary>
        public LobbyDepartureRequest Departure { get; }
    }
}
