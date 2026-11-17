using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>관찰 가능한 이동 계산 기대값이다. 요청 번호·연출 상태·숨겨진 환경 원본은 담지 않는다.</summary>
    public sealed class MovementPreviewContext
    {
        public long BattleInstanceId { get; }
        public UnitActionUse ActionUse { get; }
        public CombatControllerPhase ControllerPhase { get; }
        public long ControllerTurnSequence { get; }
        public long CompletedPlayerTurnCount { get; }
        public Owner Observer { get; }
        public EntityId ActorId { get; }
        public CombatCommandKind IntentKind { get; }
        public MovementPath Path { get; }
        public CharacterRuntimeArea Area { get; }
        public CellCoord? Anchor { get; }
        public Rotation4? Rotation { get; }
        public IReadOnlyList<CellCoord> OccupiedCells { get; }
        public HealthStateSnapshot Health { get; }
        public MoveMethod MoveMethod { get; }
        public int MoveRange { get; }
        public PositionTileModifier PositionModifier { get; }
        public MovementEnvironmentFacts VisibleEnvironment { get; }
        public EnvironmentSemanticBindings VisibleTerrainBindings { get; }
        /// <summary>공개 이동에서 토네이도를 발동한 경우에만 비교하는 환경 난수 기대값이다.</summary>
        public uint? EnvironmentRandomState { get; }
        public IReadOnlyList<TornadoAreaFact> VisibleTornadoAreas { get; }
        internal IReadOnlyList<CellCoord> GimmickBlockingCells { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }
        public CharacterCombatFacts Mover { get; }
        public CharacterCombatFacts PositionSwapPassenger { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public CombatFlowPhase Phase { get; }
        public StagePhase StagePhase { get; }
        public bool HasExpectedFacts { get; }
        public IReadOnlyList<(EntityId Id, Owner Owner, IReadOnlyList<CellCoord> Cells)> Occupancy { get; }
        public IReadOnlyList<CharacterCombatFacts> EffectTargets { get; }
        public IReadOnlyList<StructureCombatFacts> EffectStructures { get; }
        public IReadOnlyList<SoulFieldFacts> Souls { get; }
        public IReadOnlyList<OwnedCharacterRecord> Collectors { get; }
        public IReadOnlyDictionary<CharacterInstanceId, int> SoulCounts { get; }
        public MainCharacterPersistentState Main { get; }
        public IReadOnlyList<UnitSpec> Definitions { get; }
        public IReadOnlyList<CombatAttackProfile> OwnedProfiles { get; }
        public IReadOnlyList<(EffectOriginKey Origin, int Attack, int MaxHealth, int Heal, int Duration)> ArtifactInputs { get; }

        public MovementPreviewContext(long battleInstanceId, Owner observer, CharacterCombatFacts actor,
            CombatCommandKind intentKind, MovementPath path = null,
            MovementEnvironmentFacts visibleEnvironment = null, CombatCommandState admission = null,
            IReadOnlyList<(EntityId Id, Owner Owner, IReadOnlyList<CellCoord> Cells)> occupancy = null,
            IReadOnlyList<CharacterCombatFacts> effectTargets = null, IReadOnlyList<StructureCombatFacts> effectStructures = null,
            IReadOnlyList<SoulFieldFacts> souls = null,
            IReadOnlyList<OwnedCharacterRecord> collectors = null, IReadOnlyDictionary<CharacterInstanceId, int> soulCounts = null,
            MainCharacterPersistentState main = null, IReadOnlyList<UnitSpec> definitions = null,
            IReadOnlyList<(EffectOriginKey Origin, int Attack, int MaxHealth, int Heal, int Duration)> artifactInputs = null,
            EnvironmentSemanticBindings environmentBindings = null, IReadOnlyList<ObstacleContentProfile> obstacleContents = null, IReadOnlyList<CombatAttackProfile> ownedProfiles = null,
            uint? environmentRandomState = null, CharacterCombatFacts positionSwapPassenger = null, IReadOnlyList<Commands.StageGimmicks.StageGimmickSpec> gimmickDefinitions = null,
            IReadOnlyList<CellCoord> gimmickBlockingCells = null)
        {
            if (battleInstanceId < 1) throw new ArgumentOutOfRangeException(nameof(battleInstanceId));
            if (observer != Owner.Player && observer != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(observer));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (intentKind != CombatCommandKind.Move || path == null)
                throw new ArgumentException("이동 의도와 정규화 경로가 필요합니다.");
            BattleInstanceId = battleInstanceId;

            ActionUse = actor.ActionUse;
            ControllerPhase = admission?.Turn.ControllerPhase ?? CombatControllerPhase.None;
            ControllerTurnSequence = admission?.Turn.ControllerTurnSequence ?? 0;
            CompletedPlayerTurnCount = admission?.Turn.CompletedPlayerTurnCount ?? 0;
            Observer = observer;
            ActorId = actor.EntityId;
            IntentKind = intentKind;
            Path = path;

            Area = actor.Area;
            Anchor = actor.Anchor;
            Rotation = actor.Rotation;
            OccupiedCells = Array.AsReadOnly(actor.OccupiedCells.ToArray());
            Health = actor.Health;
            MoveMethod = actor.Stats.MoveMethod;
            MoveRange = actor.Stats.MoveRange;


            PositionModifier = actor.PositionModifier;
            // 생성 경계에서도 필터링하여 호출자가 숨겨진 원본을 넘겨도 공개 문맥에 남기지 않는다.
            VisibleEnvironment = (visibleEnvironment ?? MovementEnvironmentFacts.Empty).VisibleTo(observer);
            EnvironmentRandomState = environmentRandomState;
            VisibleTornadoAreas = admission == null || gimmickDefinitions == null ? Array.Empty<TornadoAreaFact>() : TornadoAreaFact.Read(admission, gimmickDefinitions, observer);
            GimmickBlockingCells = gimmickBlockingCells == null || gimmickBlockingCells.Count == 0
                ? Array.Empty<CellCoord>() : Array.AsReadOnly(gimmickBlockingCells.ToArray());
            ObstacleContents = Array.AsReadOnly((obstacleContents ?? Array.Empty<ObstacleContentProfile>()).ToArray());
            // 비용·방어·밀치기 계산이 읽은 공개 지형만 분류 정보로 남긴다.
            var terrainIds = VisibleEnvironment.Tiles.Where(t => environmentRandomState.HasValue || t is StaticTerrainFact)
                .Select(t => t.Id).ToHashSet();
            VisibleTerrainBindings = new EnvironmentSemanticBindings(
                (environmentBindings ?? EnvironmentSemanticBindings.Empty).TerrainSourceIds.Where(terrainIds.Contains).ToArray(),
                Array.Empty<long>(), Array.Empty<EntityId>());
            Mover = actor;
            PositionSwapPassenger = positionSwapPassenger;
            HasExpectedFacts = admission != null;
            BoardWidth = admission?.BoardWidth ?? 0; BoardHeight = admission?.BoardHeight ?? 0;
            Phase = admission?.Phase ?? CombatFlowPhase.None;
            StagePhase = admission?.Turn.StagePhase ?? default;

            Occupancy = Array.AsReadOnly((occupancy ?? Array.Empty<(EntityId, Owner, IReadOnlyList<CellCoord>)>())
                .Select(o => (o.Id, o.Owner, (IReadOnlyList<CellCoord>)Array.AsReadOnly(o.Cells.ToArray()))).ToArray());
            EffectTargets = Array.AsReadOnly((effectTargets ?? Array.Empty<CharacterCombatFacts>()).ToArray());
            EffectStructures = Array.AsReadOnly((effectStructures ?? Array.Empty<StructureCombatFacts>()).ToArray());
            Souls = Array.AsReadOnly((souls ?? Array.Empty<SoulFieldFacts>()).ToArray());
            Collectors = Array.AsReadOnly((collectors ?? Array.Empty<OwnedCharacterRecord>()).ToArray());
            SoulCounts = new System.Collections.ObjectModel.ReadOnlyDictionary<CharacterInstanceId, int>(
                (soulCounts ?? new Dictionary<CharacterInstanceId, int>()).ToDictionary(p => p.Key, p => p.Value));
            OwnedProfiles = Array.AsReadOnly((ownedProfiles ?? Array.Empty<CombatAttackProfile>()).ToArray());
            Main = main; Definitions = Array.AsReadOnly((definitions ?? Array.Empty<UnitSpec>()).ToArray());
            ArtifactInputs = Array.AsReadOnly((artifactInputs ?? Array.Empty<(EffectOriginKey, int, int, int, int)>()).ToArray());
        }
    }
}
