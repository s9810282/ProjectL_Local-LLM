using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem
{
    public interface IResultQueryService : IServiceModule
    {
        /// <summary>
        /// 마지막으로 게시된 전투 결과를 조회한다. 완료 전·다음 전투 준비 후·새 회차 시작 후에는 false와 null이다.
        /// 전투 정리 후에도 조회할 수 있다. 회차 최종 승리가 확정되면 다시 조회해 회차 결과를 갱신한다.
        /// </summary>
        bool TryGetLatest(out BattleResultSnapshot result);
    }

    /// <summary>결과 조회에 필요한 읽기 저장소만 연결한다. 정산 규칙은 Core 계산기, 표시는 View가 소유한다.</summary>
    internal sealed class ResultQueryService : IResultQueryService, ISystemPostBindService, IExportSystemServices
    {
        private ICombatStateBufferReadSink _combat;
        private IStageProgressBufferReadSink _stages;
        private IRunProgressBufferReadSink _run;

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _combat = buffers.CombatStateReadSink;
            _stages = buffers.StageProgressReadSink;
            _run = buffers.RunProgressReadSink;
        }

        public bool TryGetLatest(out BattleResultSnapshot result)
        {
            if (_combat == null) throw new InvalidOperationException("결과 조회 서비스의 바인딩이 필요합니다.");
            result = null;
            var completion = _combat.Completion;
            if (completion == null) return false;
            var progress = completion.Progression;
            var node = _stages.GetStages(progress.ChapterKey).Single(n =>
                n.NodeId == progress.NodeId.Value && n.StageKey == progress.StageKey.Value);
            result = BattleResultCalculator.Create(completion, node.Depth, _run.Completion);
            return true;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IResultQueryService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
