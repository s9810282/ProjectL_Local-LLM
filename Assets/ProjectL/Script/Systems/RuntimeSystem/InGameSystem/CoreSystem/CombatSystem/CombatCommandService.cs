using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 다음 번호의 View 명령만 수락하고, 명령 종류 변환·행위자 해석·이동 context 확인 뒤 계산 결과를 게시한다.
    /// 자동 진행이나 미리보기 분기를 갖지 않는다.
    /// </summary>
    internal sealed class CombatCommandService : ICombatCommandService, ISystemPostBindService, IExportSystemServices
    {
        private readonly CombatInputAssembler _assembler;
        private readonly CombatTransitionPublisher _publisher;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;
        private ICombatStateBufferReadSink _combat;
        private ICombatViewBufferReadSink _view;
        private IRunProgressBufferReadSink _run;

        internal CombatCommandService(CombatInputAssembler assembler, CombatTransitionPublisher publisher,
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
            _view = buffers.CombatViewReadSink;
            _run = buffers.RunProgressReadSink;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatCommandService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public bool ProcessNextCommand()
        {
            if (_combat == null)
                throw new InvalidOperationException("전투 서비스는 Buffer 바인딩이 필요합니다.");
            if (RunEnded) return false;
            if (!_combat.HasBattle)
                return false;
            if (!_view.TryGetNextCommand(_combat.HandledCommandNumber, out var command))
                return false;

            using var sample = CoreProfilerMarkers.Command.Auto();
            var kind = ToCommandKind(command.Data.Kind);
            var actor = command.Data.EntityId;
            var input = _assembler.Assemble(new CombatActionRequest(
                command.Number, kind, actor, command.Data.Aim, command.Data.Cell, command.Data.Rotation,
                path: command.Data.Path, movementContext: command.Data.MovementContext, itemKey: command.Data.ItemKey,
                expectedBattleInstanceId: command.Data.ExpectedBattleInstanceId,
                expectedControllerTurnSequence: command.Data.ExpectedControllerTurnSequence,
                expectedMoveCommandNumber: command.Data.ExpectedMoveCommandNumber));
            if (RunEnded) return false;
            CombatCommandTransition transition;
            using (CoreProfilerMarkers.Calculate.Auto())
                transition = input.Kind == CombatCommandKind.Move &&
                    !MovementContextCalculator.Matches(input.MovementContext, input, Owner.Player, _movementRules)
                    ? MovementContextCalculator.RejectStale(input)
                    : CombatCommandCalculator.Calculate(input, _movementRules);
            return _publisher.Publish(transition);
        }

        private static CombatCommandKind ToCommandKind(CombatViewCommandKind kind) => kind switch
        {
            CombatViewCommandKind.EquipItem => CombatCommandKind.EquipItem,
            CombatViewCommandKind.UnequipItem => CombatCommandKind.UnequipItem,
            CombatViewCommandKind.ReplaceItem => CombatCommandKind.ReplaceItem,
            CombatViewCommandKind.PlaceMainCharacter => CombatCommandKind.PlaceMainCharacter,
            CombatViewCommandKind.PlaceUnit => CombatCommandKind.PlaceUnit,
            CombatViewCommandKind.ReturnUnit => CombatCommandKind.ReturnUnit,
            CombatViewCommandKind.EnterCombat => CombatCommandKind.EnterCombat,
            CombatViewCommandKind.Move => CombatCommandKind.Move,
            CombatViewCommandKind.UndoMove => CombatCommandKind.UndoMove,
            CombatViewCommandKind.Attack => CombatCommandKind.Attack,
            CombatViewCommandKind.EndPlayerTurn => CombatCommandKind.EndPlayerTurn,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        private bool RunEnded => _run.Outcome != RunOutcome.InProgress;
    }
}
