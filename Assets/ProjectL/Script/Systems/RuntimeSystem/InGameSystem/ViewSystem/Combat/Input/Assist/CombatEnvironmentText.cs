using System;
using System.Collections.Generic;
using System.Text;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Haboob.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>공개된 환경 분류에 대응하는 Visual SO의 이름과 Description을 분리해 전달합니다. 일반 유닛은 설명 대상이 아닙니다.</summary>
    internal sealed class CombatEnvironmentText
    {
        private readonly IObjectVisualTableQuery _objects;
        private readonly ITerrainVisualTableQuery _terrains;
        private readonly IUnitVisualTableQuery _units;
        private readonly ITraitVisualTableQuery _traits;
        private readonly RockfallVisualData _rockfall;
        private readonly HaboobVisualData _haboob;
        private readonly CombatEnvironmentDescriptions _descriptions;
        private readonly StringBuilder _text = new StringBuilder(512);
        private readonly StringBuilder _names = new StringBuilder(128);
        private readonly HashSet<string> _trailDescriptions = new(StringComparer.Ordinal);

        internal CombatEnvironmentText(IObjectVisualTableQuery objects, ITerrainVisualTableQuery terrains, IUnitVisualTableQuery units,
            RockfallVisualData rockfall, HaboobVisualData haboob, ITraitVisualTableQuery traits,
            CombatEnvironmentDescriptions descriptions)
        {
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
            _terrains = terrains ?? throw new ArgumentNullException(nameof(terrains));
            _units = units ?? throw new ArgumentNullException(nameof(units));
            _rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
            _haboob = haboob ?? throw new ArgumentNullException(nameof(haboob));
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
            _descriptions = descriptions ?? throw new ArgumentNullException(nameof(descriptions));
        }

        internal (string Name, string Description) Build(CombatStateData state, CellCoord cell)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _text.Clear();
            _names.Clear();
            _trailDescriptions.Clear();
            EnvironmentSemanticBindings bindings = state.EnvironmentBindings;
            IReadOnlyList<ObstacleContentProfile> obstacles = state.ObstacleContents;
            foreach (MovementTileFact source in state.Environment.Tiles)
            {
                if (source.Cell != cell || !CombatEnvironmentFacts.IsShown(source)) continue;
                if (CombatEnvironmentFacts.IsDustScreen(source, out TraitTrailFact trail))
                {
                    var visual = _traits.GetTraitVisual(trail.Key);
                    string description = _descriptions.DustScreen(trail);
                    // 같은 완성 문구만 묶습니다. 수치가 다른 흔적은 유효 효과를 추측하지 않고 각각 표시합니다.
                    if (_trailDescriptions.Add(description))
                        AppendDescription(visual.DisplayName, description);
                }
                else if (CombatEnvironmentFacts.IsTerrain(bindings, source.Id))
                {
                    var kind = EnvironmentVisualKinds.TerrainKind(source);
                    var visual = _terrains.GetTerrainVisual(kind);
                    AppendDescription(visual.DisplayName, _descriptions.Terrain(kind, source));
                }
                else if (CombatEnvironmentFacts.TryGetObject(bindings, source, out ObjectSourceFact item))
                {
                    var kind = EnvironmentVisualKinds.ObjectKind(item);
                    var visual = _objects.GetObjectVisual(kind);
                    AppendDescription(visual.DisplayName, _descriptions.Object(kind, item));
                }
            }

            foreach (StructureCombatFacts structure in state.Structures)
            {
                if (!CombatEnvironmentFacts.TryGetObstacleAt(bindings, obstacles, structure, cell, out ObstacleContentProfile obstacle)) continue;
                var visual = _units.GetUnitVisual(structure.Definition.Key);
                AppendDescription(visual.DisplayName, _descriptions.Obstacle(obstacle));
            }

            // 움직이는 토네이도는 환경 칸 목록에 없고, 덮는 칸마다 같은 설명을 보입니다(토네이도 View 설계 TN7).
            foreach (TornadoAreaFact area in state.ReadTornadoAreas(Owner.Player))
            {
                if (!ContainsCell(area.Cells, cell)) continue;
                var visual = _terrains.GetTerrainVisual(TerrainContentKind.Tornado);
                AppendDescription(visual.DisplayName, _descriptions.Tornado(area, state.StageGimmicks));
            }

            foreach (StageGimmickState gimmick in state.Gimmicks.Entries)
            {
                // 먼지구름 칸입니다. 정적 모래폭풍(사거리 감소)과 다른 기믹이라 칸마다 자기 설명을 보입니다(하붑 View 계획 HP-11).
                if (gimmick is HaboobGimmickState haboob)
                {
                    if (ContainsCell(haboob.Cells, cell))
                        AppendDescription(_haboob.DisplayName, _descriptions.Haboob(haboob));
                    continue;
                }
                // 낙석 경고 칸입니다. 칸은 대기 단계에 비어 있어 단계를 따로 보지 않습니다(낙석 계획 RP-10).
                if (gimmick is not RockfallGimmickState rockfall || !ContainsCell(rockfall.WarnedCells, cell)) continue;
                AppendDescription(_rockfall.DisplayName, _descriptions.Rockfall(rockfall, state.StageGimmicks));
            }
            return _text.Length == 0 ? (string.Empty, string.Empty) : (_names.ToString(), _text.ToString());
        }

        private void AppendDescription(string name, string description)
        {
            // 빈 설명은 정상 입력이며, 다른 문구로 대체하거나 패널을 열지 않습니다.
            if (string.IsNullOrWhiteSpace(description)) return;
            if (_text.Length > 0)
            {
                _names.Append("\n\n");
                _text.Append("\n\n");
            }
            _names.Append(name);
            _text.Append(description);
        }

        private static bool ContainsCell(IReadOnlyList<CellCoord> values, CellCoord cell)
        {
            for (int i = 0; i < values.Count; i++) if (values[i] == cell) return true;
            return false;
        }
    }
}
