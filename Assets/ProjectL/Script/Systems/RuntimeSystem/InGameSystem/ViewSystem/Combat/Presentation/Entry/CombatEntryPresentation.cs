using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>맵 진입 뒤 배치 안내·카드를 잇습니다. 전체 입력 대기는 두 표시의 실제 진행 상태에서 나옵니다.</summary>
    internal sealed class CombatEntryPresentation : ICombatEntryPresentation
    {
        private readonly CombatBoardEntryPresentation _board;
        private readonly CombatDeploymentPresentation _deployment;
        private readonly CombatEntryDeferredDisplay _deferredDisplay;

        internal CombatEntryPresentation(CombatBoardEntryPresentation board, CombatDeploymentPresentation deployment,
            CombatEntryDeferredDisplay deferredDisplay)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _deployment = deployment ?? throw new ArgumentNullException(nameof(deployment));
            _deferredDisplay = deferredDisplay ?? throw new ArgumentNullException(nameof(deferredDisplay));
        }

        public bool IsPending => _board.IsPending || _deployment.IsPending;

        internal void PrepareDisplay()
        {
            _deployment.Prepare();
            _board.PrepareDisplay();
        }

        internal void Prepare(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures)
            => _board.Prepare(characters, structures);

        internal void RenderDeployment(StagePhase stage, CombatFlowPhase flow) => _deployment.Render(stage, flow);

        public void Tick(float deltaTime, bool isLoading)
        {
            if (!IsPending || isLoading) return;
            if (_board.IsPending)
            {
                _board.Tick(deltaTime, false);
                // 맵이 끝난 프레임의 시간을 안내·카드에 다시 적용하지 않습니다.
                return;
            }
            _deferredDisplay.TickRevealed(deltaTime);
            _deployment.Tick(deltaTime);
        }

        public void Cancel()
        {
            List<Exception> errors = null;
            CleanupErrors.Try(_deployment.Cancel, ref errors);
            CleanupErrors.Try(_board.Cancel, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatEntryPresentation] 진입 연출 정리에 실패했습니다.");
        }
    }
}
