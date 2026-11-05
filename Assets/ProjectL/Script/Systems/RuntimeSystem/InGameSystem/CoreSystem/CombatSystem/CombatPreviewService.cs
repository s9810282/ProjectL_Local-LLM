using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 최신 미리보기 요청을 해석·계산하고, 계산 뒤 요청 번호와 전투 인스턴스가 그대로일 때만 응답을 게시한다.
    /// 실제 명령 번호·게임 상태·전투 결과는 쓰지 않는다.
    /// </summary>
    internal sealed class CombatPreviewService : ICombatPreviewService, ISystemPostBindService, IExportSystemServices
    {
        private readonly CombatInputAssembler _assembler;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;
        private ICombatStateBufferReadSink _combat;
        private ICombatViewBufferReadSink _view;
        private IBattleBufferWriteSink _battle;

        internal CombatPreviewService(CombatInputAssembler assembler, IReadOnlyList<IMovementTileRule> movementRules)
        {
            _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
            _movementRules = movementRules ?? throw new ArgumentNullException(nameof(movementRules));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _combat = buffers.CombatStateReadSink;
            _view = buffers.CombatViewReadSink;
            _battle = buffers.BattleWriteSink;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatPreviewService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public bool ProcessLatestPreview()
        {
            if (_combat == null)
                throw new InvalidOperationException("전투 서비스는 Buffer 바인딩이 필요합니다.");
            if (!_combat.HasBattle || !_view.HasPreview)
                return false;

            var request = _view.Preview;
            long revision = _combat.QueryRevision;
            if (request.Number <= _combat.AnsweredPreviewNumber)
                return false;

            using var sample = CoreProfilerMarkers.Preview.Auto();
            var kind = request.Data.Kind switch
            {
                CombatViewPreviewKind.MainCharacterPlacement => CombatCommandKind.PlaceMainCharacter,
                CombatViewPreviewKind.UnitPlacement => CombatCommandKind.PlaceUnit,
                CombatViewPreviewKind.Move or CombatViewPreviewKind.MoveRange => CombatCommandKind.Move,
                CombatViewPreviewKind.Attack or CombatViewPreviewKind.AttackRange or CombatViewPreviewKind.UnitAttackRange
                    => CombatCommandKind.Attack,
                CombatViewPreviewKind.ActionAvailability => CombatCommandKind.Attack,
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            };
            var input = _assembler.Assemble(new CombatActionRequest(
                request.Number, kind, request.Data.EntityId, request.Data.Aim,
                request.Data.Cell, request.Data.Rotation, path: request.Data.Path,
                expectedBattleInstanceId: _combat.BattleInstanceId,
                expectedControllerTurnSequence: _combat.Current.CommandState.Turn.ControllerTurnSequence));
            using var kindSample = CoreProfilerMarkers.PreviewOf(request.Data.Kind).Auto();
            var result = request.Data.Kind switch
            {
                CombatViewPreviewKind.AttackRange => CombatPreviewResult.ForAttackRange(request.Number, CombatCommandCalculator.GetAttackRange(input)),
                CombatViewPreviewKind.Attack => CombatPreviewResult.ForAttack(request.Number, CombatCommandCalculator.Preview(input, _movementRules)),
                CombatViewPreviewKind.MoveRange => CombatPreviewResult.ForMoveRange(request.Number, CombatMovementCalculator.Range(input)),
                CombatViewPreviewKind.Move => CombatPreviewResult.ForMove(request.Number, CombatMovementCalculator.Preview(input, _movementRules),
                    MovementContextCalculator.Capture(input, Owner.Player, _movementRules)),
                CombatViewPreviewKind.ActionAvailability => CombatPreviewResult.ForActionAvailability(request.Number,
                    CombatCommandCalculator.GetActionAvailability(input).WithSourceRevision(revision)),
                // 기본 갈래가 배치 계산이므로 새 종류는 여기에 명시한다(빠뜨리면 예외 없이 배치 미리보기가 돈다).
                CombatViewPreviewKind.UnitAttackRange => CombatPreviewResult.ForUnitAttackRange(request.Number,
                    CombatCommandCalculator.GetUnitAttackRange(input)),
                _ => CombatPreviewResult.ForPlacement(request.Number, CombatPlacementCalculator.Preview(input))
            };
            // 계산 중 요청이 바뀌었거나 전투가 정리·교체됐으면 오래된 응답을 게시하지 않는다.
            if (!_combat.HasBattle || _combat.BattleInstanceId != input.BattleInstanceId ||
                !_view.HasPreview || _view.Preview.Number != request.Number ||
                request.Data.Kind == CombatViewPreviewKind.ActionAvailability && revision != _combat.QueryRevision) return false;
            using (CoreProfilerMarkers.PublishPreview.Auto())
                _battle.PublishPreview(result);
            return true;
        }
    }
}
