using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>정보 선택 ID와 국면만 소유합니다. 선택 가능한 사실은 같은 reader에서 읽습니다.</summary>
    internal sealed class CombatSelectionState : ICombatSelectionReader
    {
        private ICombatInspectionFactsReader _facts;
        private StagePhase? _stage;
        private EntityId _selected;
        internal void Bind(ICombatInspectionFactsReader facts) => _facts = facts ?? throw new ArgumentNullException(nameof(facts));
        public EntityId SelectedEntityId => _selected;
        public EntityId SelectedActorId => IsPlayerActor(_selected) ? _selected : EntityId.Empty;
        public IReadOnlyList<CellCoord> SelectedActorCells => _facts != null && _facts.TryGet(SelectedActorId, out var facts)
            ? facts.Cells : Array.Empty<CellCoord>();
        internal void Synchronize(StagePhase stage)
        {
            if (_stage != stage) _selected = EntityId.Empty;
            _stage = stage;
            ValidateSelection();
        }
        internal void ValidateSelection()
        {
            if (_facts == null || !_facts.TryGet(_selected, out _)) _selected = EntityId.Empty;
        }
        internal void SelectCell(CellCoord? cell)
        {
            if (_stage != StagePhase.Combat && _stage != StagePhase.Deployment) return;
            if (_facts == null) throw new InvalidOperationException("선택 사실 reader가 연결되지 않았습니다.");
            _selected = cell.HasValue && _facts.TryPick(cell.Value, out EntityId id) ? id : EntityId.Empty;
        }
        internal void SelectPicked(EntityId entity)
        {
            if (_stage == StagePhase.Combat || _stage == StagePhase.Deployment)
                _selected = entity;
        }

        internal bool TrySelectActor(EntityId actor)
        {
            if (_stage != StagePhase.Combat) return false;
            if (_facts == null) throw new InvalidOperationException("선택 사실 reader가 연결되지 않았습니다.");
            if (!IsPlayerActor(actor)) return false;
            _selected = actor;
            return true;
        }
        private bool IsPlayerActor(EntityId id) => _facts != null && _facts.TryGet(id, out var facts) &&
            facts.Character != null && facts.Owner == Owner.Player;

        internal void Clear()
        {
            _selected = EntityId.Empty;
            _stage = null;
        }
    }
}
