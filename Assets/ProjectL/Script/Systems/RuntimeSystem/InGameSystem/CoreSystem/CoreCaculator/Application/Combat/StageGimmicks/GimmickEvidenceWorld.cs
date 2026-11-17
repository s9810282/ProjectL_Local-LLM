using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    /// <summary>사건을 관찰한 시점의 공간 사실만 보관하는 검증용 투영이다. 규칙 실행이나 게시 권한은 없다.</summary>
    internal sealed class GimmickEvidenceWorld
    {
        private readonly CombatCommandInput _source;
        private readonly Dictionary<EntityId, CharacterCombatFacts> _characters;
        private Dictionary<EntityId, StructureCombatFacts> _structures;
        private Dictionary<long, MovementTileFact> _tiles;
        private HashSet<long> _terrain;
        private MovementEnvironmentFacts _environment;
        private EnvironmentSemanticBindings _bindings;
        private StageGimmickStateSet _gimmicks;

        internal IReadOnlyDictionary<EntityId, CharacterCombatFacts> Characters => _characters;
        internal IReadOnlyDictionary<EntityId, StructureCombatFacts> Structures => StructureFacts;
        /// <summary>사건 직전의 기믹 상태다. 점유(<see cref="StageGimmickState.BlockingCells"/>)를 읽는 검증이 쓴다.</summary>
        internal StageGimmickStateSet Gimmicks => _gimmicks;
        internal MovementEnvironmentFacts Environment => _environment ??= new MovementEnvironmentFacts(_tiles.Values.ToArray());
        internal EnvironmentSemanticBindings Bindings => _bindings ??= new EnvironmentSemanticBindings(
            _terrain.ToArray(), _source.EnvironmentBindings.ObjectSourceIds, _source.EnvironmentBindings.ObstacleEntityIds);

        // 캐릭터만 바뀌는 일반 명령에는 보드 전체를 복사하지 않는다.
        private Dictionary<EntityId, StructureCombatFacts> StructureFacts =>
            _structures ??= _source.State.Structures.ToDictionary(s => s.EntityId);
        private Dictionary<long, MovementTileFact> TileFacts =>
            _tiles ??= _source.State.Environment.Tiles.ToDictionary(t => t.Id);

        internal GimmickEvidenceWorld(CombatCommandInput source)
        {
            _source = source;
            _characters = source.Characters.ToDictionary(c => c.EntityId);
            _environment = source.State.Environment;
            _bindings = source.EnvironmentBindings;
            _gimmicks = source.State.Gimmicks;
        }

        internal CombatCommandInput CreateInput(CombatCommandState state) => _source.WithWorld(
            state.With(environment: Environment, structures: StructureFacts.Values.ToArray(), gimmicks: _gimmicks),
            _characters.Values.ToArray(), _source.Main, _source.SoulCounts, _source.Souls, Bindings);

        /// <summary>소비자는 이 사건의 직전 사실을 검사한 다음 호출한다. 불변 스냅샷은 해당 원본 변경 때만 다시 만든다.</summary>
        internal void Apply(CombatEvent occurrence)
        {
            switch (occurrence)
            {
                case CombatCharacterEvent change:
                    _characters[change.Final.EntityId] = change.Final;
                    break;
                case CombatTraitStateEvent change:
                    _characters[change.Final.EntityId] = change.Final;
                    break;
                case CombatSummonEvent change:
                    _characters[change.Created.EntityId] = change.Created;
                    break;
                case CombatReinforcementEvent change:
                    _characters[change.Created.EntityId] = change.Created;
                    break;
                case CombatStructureEvent change:
                    StructureFacts[change.Final.EntityId] = change.Final;
                    break;
                case CombatObstacleDurabilityEvent change:
                    StructureFacts[change.Final.EntityId] = change.Final;
                    break;
                case CombatMovementSourceEvent change:
                    TileFacts[change.Final.Id] = change.Final;
                    _environment = null;
                    break;
                case CombatPillarTerrainEvent change:
                    TileFacts[change.Final.Id] = change.Final;
                    _terrain ??= _source.EnvironmentBindings.TerrainSourceIds.ToHashSet();
                    _terrain.Add(change.Final.Id);
                    _environment = null;
                    _bindings = null;
                    break;
                case IStageGimmickStateChange change:
                    // 기믹 상태를 선언하는 사건만 반영한다. 점유가 있는 새 기믹은 사건에 이 인터페이스만 붙인다.
                    _gimmicks = _gimmicks.Replace(change.FinalGimmick);
                    break;
            }
        }
    }
}
