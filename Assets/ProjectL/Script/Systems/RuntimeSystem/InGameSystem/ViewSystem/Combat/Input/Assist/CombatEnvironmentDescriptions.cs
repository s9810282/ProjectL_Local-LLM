using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.RuntimeSystem.Common.Descriptions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>준비 시 검사한 환경 설명 틀에 현재 호버 사실의 값을 넣습니다. DB나 런타임 상태 사본은 갖지 않습니다.</summary>
    internal sealed class CombatEnvironmentDescriptions
    {
        private readonly Dictionary<ObjectContentKind, DescriptionTemplate<ObjectSourceFact>> _objects;
        private readonly Dictionary<TerrainContentKind, DescriptionTemplate<MovementTileFact>> _terrains;
        private readonly Dictionary<ObstacleContentKind, DescriptionTemplate<ObstacleContentProfile>> _obstacles;
        private readonly DescriptionTemplate<TraitTrailFact> _dustScreen;
        private readonly DescriptionTemplate<TornadoMemberSpec> _tornado;
        private readonly DescriptionTemplate<RockfallGimmickSpec> _rockfall;
        private readonly DescriptionTemplate<HaboobGimmickState> _haboob;

        internal CombatEnvironmentDescriptions(
            Dictionary<ObjectContentKind, DescriptionTemplate<ObjectSourceFact>> objects,
            Dictionary<TerrainContentKind, DescriptionTemplate<MovementTileFact>> terrains,
            Dictionary<ObstacleContentKind, DescriptionTemplate<ObstacleContentProfile>> obstacles,
            DescriptionTemplate<TraitTrailFact> dustScreen, DescriptionTemplate<TornadoMemberSpec> tornado,
            DescriptionTemplate<RockfallGimmickSpec> rockfall, DescriptionTemplate<HaboobGimmickState> haboob)
        {
            _objects = new Dictionary<ObjectContentKind, DescriptionTemplate<ObjectSourceFact>>(objects ?? throw new ArgumentNullException(nameof(objects)));
            _terrains = new Dictionary<TerrainContentKind, DescriptionTemplate<MovementTileFact>>(terrains ?? throw new ArgumentNullException(nameof(terrains)));
            _obstacles = new Dictionary<ObstacleContentKind, DescriptionTemplate<ObstacleContentProfile>>(obstacles ?? throw new ArgumentNullException(nameof(obstacles)));
            // 선택적 외형은 실제 해당 환경을 표시할 때 필요합니다. 등록된 외형의 틀은 Factory에서 모두 검사합니다.
            _dustScreen = dustScreen;
            _tornado = tornado;
            _rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
            _haboob = haboob ?? throw new ArgumentNullException(nameof(haboob));
        }

        internal string Object(ObjectContentKind kind, ObjectSourceFact source) =>
            Required(_objects, kind, "ObjectVisual.Description").Format(source);

        internal string Terrain(TerrainContentKind kind, MovementTileFact source) =>
            Required(_terrains, kind, "TerrainVisual.Description").Format(source);

        internal string Obstacle(ObstacleContentProfile source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Required(_obstacles, source.Kind, "UnitVisual.Description").Format(source);
        }

        internal string DustScreen(TraitTrailFact source) =>
            (_dustScreen ?? throw Missing("TraitVisual.DustScreen.TrailDescription")).Format(source);

        internal string Haboob(HaboobGimmickState source) => _haboob.Format(source);

        internal string Tornado(TornadoAreaFact area, IReadOnlyList<StageGimmickSpec> gimmicks)
        {
            var template = _tornado ?? throw Missing("TerrainVisual.Tornado.Description");
            if (!template.RequiresValues) return template.Format(null);
            var spec = Definition<TornadoGimmickSpec>(area.GimmickId.Value, gimmicks, "TerrainVisual.Tornado.Description");
            foreach (TornadoMemberSpec member in spec.Members)
                if (member.SourceId == area.SourceId) return template.Format(member);
            throw new InvalidOperationException($"[CombatEnvironmentDescriptions] TerrainVisual.Tornado.Description: 기믹 {area.GimmickId.Value}에서 원본 {area.SourceId}의 이동주기를 찾을 수 없습니다.");
        }

        internal string Rockfall(RockfallGimmickState state, IReadOnlyList<StageGimmickSpec> gimmicks) =>
            _rockfall.Format(_rockfall.RequiresValues
                ? Definition<RockfallGimmickSpec>(state.Id.Value, gimmicks, "RockfallVisual.Description") : null);

        /// <summary>상태의 기믹 번호는 1부터이며, 정의는 같은 전투 읽기에서 가져옵니다.</summary>
        private static T Definition<T>(int id, IReadOnlyList<StageGimmickSpec> gimmicks, string context) where T : StageGimmickSpec
        {
            if (gimmicks == null || id < 1 || id > gimmicks.Count || gimmicks[id - 1] is not T spec)
                throw new InvalidOperationException($"[CombatEnvironmentDescriptions] {context}: 기믹 {id}의 공개 정의가 없거나 {typeof(T).Name} 형식과 다릅니다.");
            return spec;
        }

        private static DescriptionTemplate<TSource> Required<TKey, TSource>(
            Dictionary<TKey, DescriptionTemplate<TSource>> templates, TKey key, string field) where TSource : class =>
            templates.TryGetValue(key, out var template) && template != null
                ? template : throw Missing($"{field}, Key: {key}");

        private static InvalidOperationException Missing(string context) =>
            new InvalidOperationException($"[CombatEnvironmentDescriptions] {context}: 설명 틀이 등록되지 않았습니다.");
    }
}
