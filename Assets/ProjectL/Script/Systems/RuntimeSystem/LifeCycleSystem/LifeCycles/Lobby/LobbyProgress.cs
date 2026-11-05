using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby
{
    /// <summary>
    /// 로비 판단에 쓰는 사실을 읽는 유일한 자리입니다(로비 재구성 설계 §4.6·P1). 읽기만 합니다.
    /// LobbyPlan·로비 Job·로비 이탈 검사가 Buffer를 직접 받지 않고 이것으로 읽습니다.
    /// </summary>
    public interface ILobbyProgressReader : IServiceModule
    {
        /// <summary>지금 사실을 한 번 읽습니다. 부를 때마다 새로 읽습니다.</summary>
        LobbyProgressFacts Read();
    }

    /// <summary>
    /// 로비 사실(로딩·출발 요청·전투)과 아티팩트 도메인의 사실을 합칩니다(계획 LP1). 쓰거나 저장하지 않습니다.
    /// </summary>
    internal sealed class LobbyProgress : ILobbyProgressReader, ISystemPostBindService
    {
        private readonly LobbyArtifactDomain _artifacts;
        private ILodingSystemService _loading;
        private ILobbyViewBufferReadSink _departures;

        internal LobbyProgress(LobbyArtifactDomain artifacts)
        {
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            // IBufferQuery는 모든 싱크를 노출하므로 여기서 필요한 것만 꺼내 보관한다.
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            Bind(resolver.GetService(SystemGroupsService.Global).LodingServiceCommand.LodingService,
                buffers.LobbyViewReadSink);
        }

        /// <summary>필요한 서비스와 읽기 싱크만 보관합니다.</summary>
        internal void Bind(ILodingSystemService loading, ILobbyViewBufferReadSink departures)
        {
            _loading = loading ?? throw new ArgumentNullException(nameof(loading));
            _departures = departures ?? throw new ArgumentNullException(nameof(departures));
        }

        public LobbyProgressFacts Read()
        {
            // 서비스 연결 보장은 여기 한 곳에서 봅니다. 아래 읽기는 다시 검사하지 않습니다(로비 리팩토링 2차 설계 N-21).
            if (_loading == null || !_artifacts.IsBound)
                throw new InvalidOperationException("[LobbyProgress] 서비스를 받기 전에 읽을 수 없습니다.");

            return new LobbyProgressFacts(
                _loading.IsLoading,
                _artifacts.IsBoardInitialized,
                _artifacts.IsStopped,
                _artifacts.NextCommandNumber,
                _artifacts.PendingPreviewNumber,
                _departures.Current);
        }
    }
}
