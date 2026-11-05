using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>로딩 대기 → 보드 → 선로 → Nexus → 적 → 완료 순서와 진입 대기 사실의 유일한 소유자입니다.</summary>
    internal sealed class CombatBoardEntryPresentation : ICombatEntryPresentation
    {
        private enum Phase
        {
            Complete,
            WaitingForLoading,
            Board,
            Railway,
            Nexus,
            Enemies
        }

        private readonly IReadOnlyList<Transform> _tiles;
        private readonly IReadOnlyList<CombatBoardRevealTarget> _backdropTargets;
        private readonly FieldView _field;
        private readonly FieldTerrainPresenter _terrain;
        private readonly FieldUnitPresenter _units;
        private readonly CombatBoardRevealSettings _settings;
        private readonly CombatBoardReveal _board;
        private readonly CombatEntryRailwayAppearance _railway;
        private readonly CombatEntryNexusAppearance _nexus;
        private readonly CombatEntryEnemyAppearance _enemies;
        private readonly CombatEntryDeferredDisplay _deferredDisplay;
        private Phase _phase;
        private bool _prepared;
        private bool _fieldHidden;

        internal CombatBoardEntryPresentation(IReadOnlyList<Transform> tiles,
            IReadOnlyList<CombatBoardRevealTarget> backdropTargets, FieldView field,
            FieldTerrainPresenter terrain, FieldUnitPresenter units, CombatBoardRevealSettings settings,
            CombatBoardReveal board, CombatEntryRailwayAppearance railway,
            CombatEntryNexusAppearance nexus, CombatEntryEnemyAppearance enemies,
            CombatEntryDeferredDisplay deferredDisplay)
        {
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _backdropTargets = backdropTargets ?? throw new ArgumentNullException(nameof(backdropTargets));
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _railway = railway ?? throw new ArgumentNullException(nameof(railway));
            _nexus = nexus ?? throw new ArgumentNullException(nameof(nexus));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _deferredDisplay = deferredDisplay ?? throw new ArgumentNullException(nameof(deferredDisplay));
        }

        public bool IsPending => _phase != Phase.Complete;

        /// <summary>최초 사실 그리기 전에 기믹과 상시 효과를 보류합니다. 진입을 건너뛰는 경로는 즉시 표시합니다.</summary>
        internal void PrepareDisplay()
        {
            if (!_prepared && _settings.Enabled && _tiles.Count > 0) _deferredDisplay.Begin();
        }

        /// <summary>첫 화면을 완성 위치에서 구성한 뒤 로딩이 걷히기 전에 한 번 준비합니다.</summary>
        internal void Prepare(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures)
        {
            if (_prepared) return;
            _prepared = true;
            if (!_settings.Enabled || _tiles.Count == 0) return;
            var targets = new List<CombatBoardRevealTarget>(_tiles.Count + _backdropTargets.Count);
            foreach (Transform tile in _tiles) targets.Add(new CombatBoardRevealTarget(tile, tile));
            foreach (CombatBoardRevealTarget target in _backdropTargets) targets.Add(target);
            _terrain.CollectBoardRevealTargets(targets);
            var enemies = new List<FieldUnitView>();
            _units.CollectEntryEnemies(characters, enemies);
            var nexuses = new List<FieldUnitView>();
            _units.CollectEntryNexuses(structures, nexuses);
            _phase = Phase.WaitingForLoading;
            try
            {
                _board.Prepare(targets);
                _railway.Prepare();
                _nexus.Prepare(nexuses, _field.Mapper);
                _enemies.Prepare(enemies, _field.Mapper);
                _fieldHidden = true;
                _field.SetEntryVisible(false);
            }
            catch (Exception error)
            {
                List<Exception> errors = new() { error };
                CleanupErrors.Try(Cancel, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[CombatBoardEntryPresentation] 진입 연출 준비에 실패했습니다.");
            }
        }

        public void Tick(float deltaTime, bool isLoading)
        {
            if (!IsPending || isLoading) return;
            if (_phase == Phase.WaitingForLoading) _phase = Phase.Board;
            if (_phase == Phase.Board)
            {
                _board.Tick(deltaTime);
                if (!_board.IsComplete) return;
                ShowField();
                _deferredDisplay.RevealBoard();
                _phase = Phase.Railway;
                _railway.Begin();
                if (_railway.IsComplete) BeginNexus();
                // 다음 단계는 Begin 자세만 적용합니다. 같은 deltaTime을 다시 소비하지 않습니다.
                return;
            }
            if (_phase == Phase.Railway)
            {
                _railway.Tick(deltaTime);
                if (_railway.IsComplete) BeginNexus();
                return;
            }
            if (_phase == Phase.Nexus)
            {
                _nexus.Tick(deltaTime);
                if (_nexus.IsComplete) BeginEnemies();
                return;
            }
            _enemies.Tick(deltaTime);
            if (_enemies.IsComplete) Complete();
        }

        public void Cancel()
        {
            _phase = Phase.Complete;
            List<Exception> errors = null;
            CleanupErrors.Try(_enemies.Cancel, ref errors);
            CleanupErrors.Try(_nexus.Cancel, ref errors);
            CleanupErrors.Try(_railway.Cancel, ref errors);
            CleanupErrors.Try(_board.Cancel, ref errors);
            CleanupErrors.Try(_deferredDisplay.Cancel, ref errors);
            CleanupErrors.Try(ShowField, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatBoardEntryPresentation] 진입 연출 정리에 실패했습니다.");
        }

        private void BeginNexus()
        {
            _phase = Phase.Nexus;
            _nexus.Begin();
            if (_nexus.IsComplete) BeginEnemies();
        }

        private void BeginEnemies()
        {
            _phase = Phase.Enemies;
            _enemies.Begin();
            if (_enemies.IsComplete) Complete();
        }

        private void Complete()
        {
            _deferredDisplay.RevealUnits();
            Cancel();
        }

        private void ShowField()
        {
            if (!_fieldHidden) return;
            _fieldHidden = false;
            if (_field != null) _field.SetEntryVisible(true);
        }
    }
}
