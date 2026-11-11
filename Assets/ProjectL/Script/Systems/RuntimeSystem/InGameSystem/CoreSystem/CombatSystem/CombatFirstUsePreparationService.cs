using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 전투 첫 동작 Core 준비입니다(첫 동작 Core 준비 계획 FU-a). 지금 Buffer의 전투로 입력을 조립해 리허설(<see cref="CombatFirstUseRehearsal"/>)을
    /// 단계마다 진행합니다. 결과 게시 협력자를 받지 않으므로 구조상 Buffer에 쓰지 않습니다.
    /// </summary>
    /// <remarks>
    /// 끝까지 마치면 이 서비스 인스턴스 수명 동안 다시 하지 않습니다(FU-P5, 정적 상태 없음). JIT는 프로세스 단위라 두 번째 전투부터는 할 일이 없습니다.
    /// 다른 전투에서 부르면 이전 리허설을 버리고 처음부터 합니다(FU-P14).
    /// </remarks>
    internal sealed class CombatFirstUsePreparationService : ICombatFirstUsePreparation, ISystemPostBindService, IExportSystemServices
    {
        private readonly CombatInputAssembler _assembler;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;
        private readonly Func<CombatProcessingKind, long, CombatCommandInput> _seed;
        private ICombatStateBufferReadSink _combat;
        private CombatFirstUseRehearsal _rehearsal;
        private long _rehearsalBattle;
        private bool _prepared;

        internal CombatFirstUsePreparationService(CombatInputAssembler assembler, IReadOnlyList<IMovementTileRule> movementRules)
        {
            _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
            _movementRules = movementRules ?? throw new ArgumentNullException(nameof(movementRules));
            _seed = Seed;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _combat = resolver.GetService(BufferSystemKey.Buffer).CombatStateReadSink;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatFirstUsePreparation>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public bool PrepareNext()
        {
            if (_combat == null)
                throw new InvalidOperationException("첫 동작 준비는 Buffer 바인딩이 필요합니다.");
            if (_prepared)
                return false;

            using var sample = CoreProfilerMarkers.FirstUse.Auto();
            if (!_combat.HasBattle)
                throw new InvalidOperationException("첫 동작 준비에는 준비된 전투가 필요합니다.");

            long battle = _combat.BattleInstanceId;
            if (_rehearsal == null || _rehearsalBattle != battle)
            {
                long turnSequence = _combat.Current.CommandState.Turn.ControllerTurnSequence;
                _rehearsal = new CombatFirstUseRehearsal(Seed(CombatProcessingKind.Command, turnSequence), _seed, _movementRules);
                _rehearsalBattle = battle;
            }
            if (_rehearsal.RunNext())
                return true;

            _rehearsal = null;
            _prepared = true;
            return false;
        }

        // 지금 전투로 새 입력을 조립합니다. 행동 종류·행위자는 리허설이 바꾸며, 기대 식별자와 처리 종류만 여기서 정합니다.
        private CombatCommandInput Seed(CombatProcessingKind processing, long expectedControllerTurnSequence)
        {
            bool advance = processing == CombatProcessingKind.Advance;
            return _assembler.Assemble(new CombatActionRequest(
                advance ? 0 : _combat.HandledCommandNumber + 1,
                advance ? CombatCommandKind.StartControllerTurn : CombatCommandKind.EnterCombat,
                EntityId.Empty, default, processingKind: processing,
                expectedBattleInstanceId: _combat.BattleInstanceId,
                expectedControllerTurnSequence: expectedControllerTurnSequence));
        }
    }
}
