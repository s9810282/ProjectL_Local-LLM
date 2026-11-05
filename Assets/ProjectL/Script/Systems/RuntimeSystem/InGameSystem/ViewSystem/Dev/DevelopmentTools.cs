#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.Development;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>화면 수명과 표시 상태를 확인한 뒤 Core에 개발 명령을 위임한다.</summary>
    internal sealed class DevelopmentTools :
        IDevelopmentTools,
        ISystemPostBindService,
        ISystemShutdown,
        IExportSystemServices
    {
        private Func<bool> _canUse;
        private Action _requestMapRedraw;
        private readonly RuntimeFaultBoundary _faults;
        private IDevelopmentStateCommands _commands;
        private bool _stopped;

        internal DevelopmentTools(Func<bool> canUse, Action requestMapRedraw, RuntimeFaultBoundary faults)
        {
            _canUse = canUse ?? throw new ArgumentNullException(nameof(canUse));
            _requestMapRedraw = requestMapRedraw ?? throw new ArgumentNullException(nameof(requestMapRedraw));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (_stopped || _commands != null)
                throw new InvalidOperationException("개발 도구는 실행 수명에 한 번만 연결할 수 있습니다.");
            var inGame = resolver.GetService(SystemGroupsService.InGame);
            if (!(inGame.CoreGroup is IDevelopmentStateProvider provider) || provider.DevelopmentState == null)
                throw new InvalidOperationException("개발 도구에는 같은 인게임 그룹의 Core 개발 명령이 필요합니다.");
            _commands = provider.DevelopmentState;
        }

        public DevelopmentContext ReadContext() =>
            Invoke(() => _commands.ReadContext(), UnavailableContext);

        public DevelopmentGoldResult GrantGold(DevelopmentContext expected, int amount) =>
            Invoke(() => _commands.GrantGold(expected, amount),
                () => DevelopmentGoldResult.Refused(DevelopmentFailureReason.Unavailable));

        public DevelopmentUnitResult GrantUnit(DevelopmentContext expected, EntityKey key) =>
            Invoke(() => _commands.GrantUnit(expected, key),
                () => DevelopmentUnitResult.Refused(DevelopmentFailureReason.Unavailable));

        public DevelopmentNavigationResult SkipNextNode(DevelopmentContext expected, StageNodeId nodeId) =>
            Invoke(() => RedrawChanged(_commands.SkipNextNode(expected, nodeId)),
                () => UnavailableNavigation(nodeId));

        public DevelopmentNavigationResult PrepareJumpToNode(DevelopmentContext expected, StageNodeId targetNodeId) =>
            Invoke(() => RedrawChanged(_commands.PrepareJumpToNode(expected, targetNodeId)),
                () => UnavailableNavigation(targetNodeId));

        private T Invoke<T>(Func<T> command, Func<T> unavailable) where T : class
        {
            try
            {
                if (_stopped || _commands == null || _faults.IsFaulted || !_canUse())
                    return unavailable();
                // 준비 확인 도중 종료나 오류가 발생했다면 보관한 호출자도 더 진행하지 않는다.
                if (_stopped || _commands == null || _faults.IsFaulted)
                    return unavailable();
                return command() ?? throw new InvalidOperationException("개발 명령 결과가 없습니다.");
            }
            catch (Exception error)
            {
                _faults.Report(error);
                throw;
            }
        }

        private DevelopmentNavigationResult RedrawChanged(DevelopmentNavigationResult result)
        {
            if (result == null) throw new InvalidOperationException("개발 이동 결과가 없습니다.");
            // 게시가 끝난 뒤 표시를 갱신한다. 표시 실패는 이미 게시한 진행을 되돌리지 않는다.
            if (result.Changed) _requestMapRedraw();
            return result;
        }

        private static DevelopmentContext UnavailableContext() =>
            new DevelopmentContext(default, 0, false, default, default, DevelopmentFailureReason.Unavailable);

        private static DevelopmentNavigationResult UnavailableNavigation(StageNodeId nodeId) =>
            new DevelopmentNavigationResult(DevelopmentFailureReason.Unavailable, nodeId, default,
                Array.Empty<StageNodeId>(), UnavailableContext());

        public void Shutdown()
        {
            _stopped = true;
            _commands = null;
            _canUse = null;
            _requestMapRedraw = null;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<IDevelopmentTools>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
#endif
