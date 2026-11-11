using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>
    /// 로딩 대기 → 보드 → 선로 → 구조물 → Object → Nexus → 적 → 완료 순서와 진입 대기 사실의 유일한 소유자입니다.
    /// 보드 위 물체는 타일이 다 올라온 뒤 구조물부터 차례대로 보입니다(평가 C10-06, 2026-11-05 사용자 결정 Q-N7).
    /// 구조물은 적과 같은 소환 상승으로 올라오고, Object는 구조물이 다 선 뒤 한 번에 보입니다. Nexus 낙하가 꺼졌으면 Nexus는 구조물과 함께 올라옵니다.
    /// </summary>
    internal sealed class CombatBoardEntryPresentation : ICombatEntryPresentation
    {
        private enum Phase
        {
            Complete,
            WaitingForLoading,
            Board,
            Railway,
            Structures,
            Nexus,
            Enemies
        }

        private readonly IReadOnlyList<Transform> _tiles;
        private readonly IReadOnlyList<CombatBoardRevealTarget> _backdropTargets;
        private readonly FieldView _field;
        private readonly FieldTerrainPresenter _terrain;
        private readonly FieldUnitPresenter _units;
        private readonly FieldObjectPresenter _objects;
        private readonly CombatBoardRevealSettings _settings;
        private readonly CombatBoardReveal _board;
        private readonly CombatEntryRailwayAppearance _railway;
        private readonly CombatEntryRiseAppearance _structures;
        private readonly CombatEntryNexusAppearance _nexus;
        private readonly CombatEntryRiseAppearance _enemies;
        private readonly CombatEntryEnemyRows _enemyRows;
        private readonly List<CombatEntryVisibility> _objectVisibility = new();
        private readonly CombatEntryDeferredDisplay _deferredDisplay;
        private readonly IAudioSystemService _audio;
        private Phase _phase;
        private bool _prepared;
        private bool _fieldHidden;

        internal CombatBoardEntryPresentation(IReadOnlyList<Transform> tiles,
            IReadOnlyList<CombatBoardRevealTarget> backdropTargets, FieldView field,
            FieldTerrainPresenter terrain, FieldUnitPresenter units, FieldObjectPresenter objects, CombatBoardRevealSettings settings,
            CombatBoardReveal board, CombatEntryRailwayAppearance railway, CombatEntryRiseAppearance structures,
            CombatEntryNexusAppearance nexus, CombatEntryRiseAppearance enemies,
            CombatEntryEnemyRows enemyRows, CombatEntryDeferredDisplay deferredDisplay, IAudioSystemService audio)
        {
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _backdropTargets = backdropTargets ?? throw new ArgumentNullException(nameof(backdropTargets));
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _railway = railway ?? throw new ArgumentNullException(nameof(railway));
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));
            _nexus = nexus ?? throw new ArgumentNullException(nameof(nexus));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _enemyRows = enemyRows ?? throw new ArgumentNullException(nameof(enemyRows));
            _deferredDisplay = deferredDisplay ?? throw new ArgumentNullException(nameof(deferredDisplay));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
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
            var enemyGridRows = new List<int>();
            _units.CollectEntryEnemies(characters, enemies, enemyGridRows);
            float[] enemyStartDelays = _enemyRows.CreateStartDelays(enemyGridRows);
            var nexuses = new List<FieldUnitView>();
            var nexusCells = new List<CellCoord>();
            _units.CollectEntryNexuses(structures, nexuses, nexusCells);
            var nexusGround = new List<Transform>();
            if (_nexus.Drops && nexusCells.Count > 0) _terrain.CollectEntryGroundTargets(nexusCells, nexusGround);
            var boardStructures = new List<FieldUnitView>();
            _units.CollectEntryStructures(structures, !_nexus.Drops, boardStructures);
            var objects = new List<Transform>();
            _objects.CollectEntryObjects(objects);
            _phase = Phase.WaitingForLoading;
            try
            {
                _board.Prepare(targets);
                _railway.Prepare();
                _structures.Prepare(boardStructures, _field.Mapper);
                foreach (Transform obj in objects)
                {
                    var visibility = new CombatEntryVisibility(obj);
                    _objectVisibility.Add(visibility);
                    visibility.Hide();
                }
                _nexus.Prepare(nexuses, _field.Mapper, nexusGround);
                _enemies.Prepare(enemies, _field.Mapper, enemyStartDelays);
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
            if (_phase == Phase.WaitingForLoading)
            {
                _phase = Phase.Board;
                _audio.Play(SfxEvent.MapDeploy);
            }
            if (_phase == Phase.Board)
            {
                _board.Tick(deltaTime);
                if (!_board.IsComplete) return;
                ShowField();
                _deferredDisplay.RevealBoard();
                _phase = Phase.Railway;
                _railway.Begin();
                if (_railway.IsComplete) BeginStructures();
                // 다음 단계는 Begin 자세만 적용합니다. 같은 deltaTime을 다시 소비하지 않습니다.
                return;
            }
            // 보드가 공개된 뒤 남은 진입 단계 동안 토네이도 생성 전환을 진행합니다(평가 C10-18).
            _deferredDisplay.TickRevealed(deltaTime);
            if (_phase == Phase.Railway)
            {
                _railway.Tick(deltaTime);
                if (_railway.IsComplete) BeginStructures();
                return;
            }
            if (_phase == Phase.Structures)
            {
                _structures.Tick(deltaTime);
                if (_structures.IsComplete) AfterStructures();
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
            CleanupErrors.Try(RevealObjects, ref errors);
            CleanupErrors.Try(_structures.Cancel, ref errors);
            CleanupErrors.Try(_railway.Cancel, ref errors);
            CleanupErrors.Try(_board.Cancel, ref errors);
            CleanupErrors.Try(_deferredDisplay.Cancel, ref errors);
            CleanupErrors.Try(ShowField, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatBoardEntryPresentation] 진입 연출 정리에 실패했습니다.");
        }

        private void BeginStructures()
        {
            _phase = Phase.Structures;
            _structures.Begin();
            if (_structures.IsComplete) AfterStructures();
        }

        // 구조물이 다 선 뒤 Object를 보이고 Nexus 낙하로 넘어갑니다. 같은 프레임에 이어지므로 따로 단계를 두지 않습니다.
        private void AfterStructures()
        {
            RevealObjects();
            BeginNexus();
        }

        // 한 번만 보입니다. 보인 뒤 목록을 비우므로 정리(Cancel)에서 다시 불러도 아무것도 하지 않습니다.
        private void RevealObjects()
        {
            List<Exception> errors = null;
            foreach (CombatEntryVisibility visibility in _objectVisibility) CleanupErrors.Try(visibility.Restore, ref errors);
            _objectVisibility.Clear();
            CleanupErrors.ThrowIfAny(errors, "[CombatBoardEntryPresentation] 진입 Object 표시 복구에 실패했습니다.");
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
