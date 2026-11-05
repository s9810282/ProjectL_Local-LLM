using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 자동 국면에서 진영 시작 또는 적 진행 요청을 만들어 계산하고 게시한다. View 명령을 읽거나 소비하지 않는다.
    /// </summary>
    internal sealed class CombatAdvanceService : ICombatAdvanceService, ISystemPostBindService, IExportSystemServices
    {
        private readonly CombatInputAssembler _assembler;
        private readonly CombatTransitionPublisher _publisher;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;
        private ICombatStateBufferReadSink _combat;
        private IRunProgressBufferReadSink _run;

        internal CombatAdvanceService(CombatInputAssembler assembler, CombatTransitionPublisher publisher,
            IReadOnlyList<IMovementTileRule> movementRules)
        {
            _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _movementRules = movementRules ?? throw new ArgumentNullException(nameof(movementRules));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _combat = buffers.CombatStateReadSink;
            _run = buffers.RunProgressReadSink;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatAdvanceService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public bool AdvanceCombat()
        {
            if (_combat == null)
                throw new InvalidOperationException("전투 서비스는 Buffer 바인딩이 필요합니다.");
            if (RunEnded) return false;
            if (!_combat.HasBattle || _combat.Phase != CombatFlowPhase.Automatic)
                return false;

            using var sample = CoreProfilerMarkers.Advance.Auto();
            var turn = _combat.Current.CommandState.Turn;
            var kind = turn.ControllerPhase == CombatControllerPhase.Enemy
                ? CombatCommandKind.CompleteEnemyTurn : CombatCommandKind.StartControllerTurn;
            var input = _assembler.Assemble(new CombatActionRequest(0, kind,
                default, default, processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: _combat.BattleInstanceId,
                expectedControllerTurnSequence: turn.ControllerTurnSequence));
            if (RunEnded) return false;
            CombatCommandTransition transition;
            using (CoreProfilerMarkers.Calculate.Auto())
                transition = CombatCommandCalculator.Advance(input, _movementRules);
            return _publisher.Publish(transition);
        }

        private bool RunEnded => _run.Outcome != RunOutcome.InProgress;
    }
}
