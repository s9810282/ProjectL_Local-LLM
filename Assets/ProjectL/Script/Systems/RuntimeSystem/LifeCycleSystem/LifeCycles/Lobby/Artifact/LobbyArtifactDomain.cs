using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact
{
    /// <summary>처리 기록을 확인할 아티팩트 요청의 종류입니다.</summary>
    public enum LobbyArtifactRequestKind
    {
        Edit = 0,
        Preview = 1
    }

    /// <summary>
    /// 아티팩트 처리 기록을 확인하는 Job을 만드는 입구입니다(로비 재구성 설계 P1).
    /// 아티팩트 도메인 정지는 이 입구가 만든 확인 Job만 할 수 있습니다. 정지 자체는 인터페이스에 내지 않습니다.
    /// </summary>
    public interface ILobbyArtifactChecks : IServiceModule
    {
        /// <summary>호출 Job 바로 뒤에 등록할, 같은 번호의 처리 기록 확인 Job을 만듭니다.</summary>
        IJobService CreateCheck(LobbyArtifactRequestKind kind, int number);
    }

    /// <summary>
    /// 로비 아티팩트 도메인의 사실을 읽고 도메인 정지 한 칸을 소유합니다(설계 P1·계획 LP1, 정책 D11).
    /// </summary>
    /// <remarks>
    /// 정지·해제·정지 여부는 어떤 인터페이스에도 없습니다. 정지는 이 객체가 만든 확인 Job, 해제는 구체 객체를 생성자로 받은
    /// LobbyLifeCycle, 정지 여부 읽기는 LobbyProgress만 합니다. 쓰지 않고 Buffer 읽기 싱크만 보관합니다.
    /// </remarks>
    internal sealed class LobbyArtifactDomain : ILobbyArtifactChecks, ISystemPostBindService
    {
        private readonly Action<string> _report;
        private IArtifactViewBufferReadSink _input;
        private IArtifactCommandResultReadSink _results;
        private IArtifactBufferReadSink _board;

        /// <param name="report">기록 누락을 알리는 오류 로그입니다. 공용 오류 경계가 아닙니다(D11).</param>
        internal LobbyArtifactDomain(Action<string> report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>이번 로비 진입에서 기록 누락으로 아티팩트 도메인이 멈췄는지입니다.</summary>
        internal bool IsStopped { get; private set; }

        internal bool IsBoardInitialized
        {
            get
            {
                return _board.Current.IsInitialized;
            }
        }

        /// <summary>Core 처리 번호 다음 첫 편집 입력의 번호입니다. 없으면 0입니다.</summary>
        internal int NextCommandNumber
        {
            get
            {
                return ArtifactEditFacts.NextCommandNumber(_input, _results);
            }
        }

        /// <summary>아직 답하지 않은 최신 프리뷰의 번호입니다. 없으면 0입니다.</summary>
        internal int PendingPreviewNumber
        {
            get
            {
                return ArtifactEditFacts.PendingPreviewNumber(_input, _results);
            }
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            // IBufferQuery는 모든 싱크를 노출하므로 여기서 필요한 것만 꺼내 보관한다.
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            Bind(buffers.ArtifactViewReadSink, buffers.ArtifactCommandResults, buffers.ArtifactReadSink);
        }

        /// <summary>필요한 읽기 싱크만 보관합니다.</summary>
        internal void Bind(IArtifactViewBufferReadSink input, IArtifactCommandResultReadSink results,
            IArtifactBufferReadSink board)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public IJobService CreateCheck(LobbyArtifactRequestKind kind, int number)
        {
            if (kind != LobbyArtifactRequestKind.Edit && kind != LobbyArtifactRequestKind.Preview)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "알 수 없는 아티팩트 요청 종류입니다.");
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number));

            return new LobbyArtifactCheckJob(this, kind, number);
        }

        /// <summary>같은 번호가 아직 처리 안 된 다음 요청으로 남아 있는지입니다. 확인 Job만 부릅니다.</summary>
        internal bool IsStillPending(LobbyArtifactRequestKind kind, int number)
        {
            return (kind == LobbyArtifactRequestKind.Edit ? NextCommandNumber : PendingPreviewNumber) == number;
        }

        /// <summary>
        /// 기록 누락을 오류 로그로 남기고 아티팩트 도메인만 멈춥니다(D11). 이미 멈췄으면 처음 진단만 남깁니다. 확인 Job만 부릅니다.
        /// </summary>
        internal void Stop(string diagnosis)
        {
            if (IsStopped)
                return;

            IsStopped = true;
            _report(diagnosis);
        }

        /// <summary>로비에 다시 들어올 때 정지를 풉니다(D11 "재진입하면 다시 받음"). LobbyLifeCycle만 부릅니다.</summary>
        internal void Reset()
        {
            IsStopped = false;
        }

        /// <summary>읽기 싱크를 받았는지입니다. 사실 읽기(LobbyProgress.Read)가 한 번 확인합니다(로비 리팩토링 2차 설계 N-21).</summary>
        internal bool IsBound => _input != null;
    }
}
