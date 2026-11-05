using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>선택 검증과 정보 갱신 순서만 조정합니다. 표시 사실과 선택 ID를 복제하지 않습니다.</summary>
    internal sealed class CombatInspectionPresenter
    {
        private readonly CombatSelectionState _selection;
        private readonly ICombatInspectionFactsReader _facts;
        private readonly CombatInspectionInfoMapper _mapper;
        private readonly CombatUnitInfo _panel;
        private Action _selectionChanged;
        internal void BindSelectionChanged(Action changed) => _selectionChanged = changed;
        private IReadOnlyList<OwnedCharacterRecord> _owned = Array.Empty<OwnedCharacterRecord>();
        // 같은 표시에서 받은 전투 중 불변 설정을 보관하고, 연출 중에는 최신 Buffer를 다시 읽지 않습니다.
        private BattleTraitSettings _traitSettings;
        private int _generation;
        private bool _released;
        internal CombatInspectionPresenter(CombatSelectionState selection, ICombatInspectionFactsReader facts,
            CombatInspectionInfoMapper mapper, CombatUnitInfo panel, Action selectionChanged)
        {
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _panel = panel != null ? panel : throw new ArgumentNullException(nameof(panel));
            _selectionChanged = selectionChanged;
            _panel.BindInspection(ReadSelection);
        }
        internal void Synchronize(StagePhase stage, IReadOnlyList<OwnedCharacterRecord> owned, BattleTraitSettings traitSettings)
        {
            if (_released) return;
            _traitSettings = traitSettings ?? throw new ArgumentNullException(nameof(traitSettings));
            _owned = owned ?? Array.Empty<OwnedCharacterRecord>();
            _selection.Synchronize(stage);
        }
        internal void SelectCell(CellCoord? cell)
        {
            if (_released) return;
            _selection.SelectCell(cell);
            Refresh();
        }
        internal CombatInspectionInfo ReadSelection()
        {
            if (_released || !_facts.TryGet(_selection.SelectedEntityId, out var facts)) return null;
            if (_traitSettings == null)
                throw new InvalidOperationException("[CombatInspectionPresenter] 선택 정보를 표시하기 전에 전투 특성 설정을 연결해야 합니다.");
            return _mapper.Map(facts, _owned, facts.Character != null ? _traitSettings.For(facts.Character) : null);
        }
        internal void Refresh()
        {
            if (_released) return;
            int generation = _generation;
            _selection.ValidateSelection();
            var selected = _selection.SelectedEntityId;
            _selectionChanged?.Invoke();
            if (_released || generation != _generation || selected != _selection.SelectedEntityId) return;
            _panel.ShowSelection();
        }
        internal void Clear()
        {
            _generation++;
            _selection.Clear();
            _owned = Array.Empty<OwnedCharacterRecord>();
            _traitSettings = null;
        }
        internal void Release()
        {
            if (_released) return;
            _released = true;
            Clear();
            _panel.BindInspection(null);
        }
    }
}
