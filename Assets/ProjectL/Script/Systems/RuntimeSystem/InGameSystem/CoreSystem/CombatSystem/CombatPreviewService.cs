using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
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
            bool platformRequest = request.Data.Kind == CombatViewPreviewKind.PlatformForecast;
            var kind = request.Data.Kind switch
            {
                CombatViewPreviewKind.MainCharacterPlacement => CombatCommandKind.PlaceMainCharacter,
                CombatViewPreviewKind.UnitPlacement => CombatCommandKind.PlaceUnit,
                CombatViewPreviewKind.Move or CombatViewPreviewKind.MoveRange => CombatCommandKind.Move,
                CombatViewPreviewKind.Attack or CombatViewPreviewKind.AttackRange or CombatViewPreviewKind.UnitAttackRange
                    or CombatViewPreviewKind.RoarForecast => CombatCommandKind.Attack,
                CombatViewPreviewKind.ActionAvailability or CombatViewPreviewKind.ActionAvailabilities => CombatCommandKind.Attack,
                CombatViewPreviewKind.PlatformForecast => CombatCommandKind.StartControllerTurn,
                CombatViewPreviewKind.SkillAvailability or CombatViewPreviewKind.SkillSelection or CombatViewPreviewKind.SkillForecast
                    => CombatCommandKind.UsePlayerSkill,
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            };
            var input = _assembler.Assemble(new CombatActionRequest(
                platformRequest ? 0 : request.Number, kind, request.Data.EntityId, request.Data.Aim,
                request.Data.Cell, request.Data.Rotation, path: request.Data.Path,
                processingKind: platformRequest ? CombatProcessingKind.Advance : CombatProcessingKind.Command,
                expectedBattleInstanceId: _combat.BattleInstanceId,
                expectedControllerTurnSequence: _combat.Current.CommandState.Turn.ControllerTurnSequence,
                sweepSide: request.Data.SweepSide, playerSkill: request.Data.PlayerSkill));
            using var kindSample = CoreProfilerMarkers.PreviewOf(request.Data.Kind).Auto();
            var result = request.Data.Kind switch
            {
                CombatViewPreviewKind.AttackRange => CombatPreviewResult.ForAttackRange(request.Number, CombatCommandCalculator.GetAttackRange(input)),
                CombatViewPreviewKind.Attack => CombatPreviewResult.ForAttack(request.Number, CombatCommandCalculator.Preview(input, _movementRules)),
                CombatViewPreviewKind.MoveRange => CombatPreviewResult.ForMoveRange(request.Number,
                    CombatMovementCalculator.Range(input).WithSourceRevision(revision)),
                CombatViewPreviewKind.Move => CombatPreviewResult.ForMove(request.Number, CombatMovementCalculator.Preview(input, _movementRules),
                    MovementContextCalculator.Capture(input, Owner.Player, _movementRules)),
                CombatViewPreviewKind.ActionAvailability => CombatPreviewResult.ForActionAvailability(request.Number,
                    CombatCommandCalculator.GetActionAvailability(input).WithSourceRevision(revision)),
                CombatViewPreviewKind.ActionAvailabilities => CombatPreviewResult.ForActionAvailabilities(request.Number,
                    WithRevision(CombatCommandCalculator.GetActionAvailabilities(input), revision)),
                CombatViewPreviewKind.UnitAttackRange => CombatPreviewResult.ForUnitAttackRange(request.Number,
                    CombatCommandCalculator.GetUnitAttackRange(input)),
                CombatViewPreviewKind.RoarForecast => CombatPreviewResult.ForRoarForecast(request.Number,
                    CombatCommandCalculator.GetRoarForecast(input, _movementRules)),
                CombatViewPreviewKind.PlatformForecast => CombatPreviewResult.ForPlatformForecast(request.Number,
                    PlatformForecastCalculator.Forecast(input, revision, _movementRules)),
                CombatViewPreviewKind.SkillAvailability => CombatPreviewResult.ForSkillAvailability(request.Number,
                    PlayerSkillQueries.Availability(input, input.PlayerSkillSource.Value)),
                CombatViewPreviewKind.SkillSelection => CombatPreviewResult.ForSkillSelection(request.Number,
                    PlayerSkillQueries.Selection(input, input.PlayerSkillSource.Value)),
                CombatViewPreviewKind.SkillForecast => CombatPreviewResult.ForSkillForecast(request.Number,
                    PlayerSkillQueries.Forecast(input, _movementRules, input.PlayerSkillSource.Value)),
                CombatViewPreviewKind.MainCharacterPlacement or CombatViewPreviewKind.UnitPlacement =>
                    CombatPreviewResult.ForPlacement(request.Number, CombatPlacementCalculator.Preview(input)),
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            };
            // 게시 직전에 요청과 전투를 대조하고, 출처 검증 대상은 리비전과 진영 턴도 확인한다.
            if (!_combat.HasBattle || _combat.BattleInstanceId != input.BattleInstanceId ||
                !_view.HasPreview || _view.Preview.Number != request.Number ||
                (request.Data.Kind is CombatViewPreviewKind.MoveRange or CombatViewPreviewKind.ActionAvailability
                    or CombatViewPreviewKind.ActionAvailabilities ||
                    kind == CombatCommandKind.UsePlayerSkill || platformRequest) &&
                (revision != _combat.QueryRevision || input.State.Turn.ControllerTurnSequence !=
                    _combat.Current.CommandState.Turn.ControllerTurnSequence)) return false;
            using (CoreProfilerMarkers.PublishPreview.Auto())
                _battle.PublishPreview(result);
            return true;
        }

        // 묶음 답의 항목마다 계산에 쓴 원본 판을 붙인다(단일 답의 WithSourceRevision과 같은 규칙).
        private static CombatActionAvailability[] WithRevision(IReadOnlyList<CombatActionAvailability> availabilities, long revision)
        {
            var stamped = new CombatActionAvailability[availabilities.Count];
            for (int i = 0; i < stamped.Length; i++)
                stamped[i] = availabilities[i].WithSourceRevision(revision);
            return stamped;
        }
    }
}
