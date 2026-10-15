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
        public Owner Observer { get; }
        public EntityId ActorId { get; }
        public CombatCommandKind IntentKind { get; }
        public MovementPath Path { get; }
        public CellCoord? ReturnCheckpoint { get; }
        public CharacterRuntimeArea Area { get; }
        public CellCoord? Anchor { get; }
        public Rotation4? Rotation { get; }
        public IReadOnlyList<CellCoord> OccupiedCells { get; }
        public HealthStateSnapshot Health { get; }
        public MoveMethod MoveMethod { get; }
        public int MoveRange { get; }
        public int MoveCost { get; }
        public int RemainingMoveRange { get; }
        public int CurrentActionCost { get; }
        public int MaxActionCost { get; }
        public CellCoord MovementCheckpointAnchor { get; }
        public int CommittedMoveDistance { get; }
        public int PendingMoveActionCost { get; }
        public int PendingMoveDistance { get; }
        public int AttackRefundEligibleMoveCost { get; }
        public PositionTileModifier PositionModifier { get; }
        public MovementEnvironmentFacts VisibleEnvironment { get; }
        public EnvironmentSemanticBindings VisibleTerrainBindings { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }
        public CharacterCombatFacts Mover { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public CombatFlowPhase Phase { get; }
        public EntityId ActiveActorId { get; }
        public bool IsNight { get; }
        public StagePhase StagePhase { get; }
        public long CurrentTime { get; }
        public long CompletedUnitTurnCount { get; }
        public bool HasExpectedFacts { get; }
        public IReadOnlyList<(EntityId Id, Owner Owner, IReadOnlyList<CellCoord> Cells)> Occupancy { get; }
        public IReadOnlyList<CharacterCombatFacts> EffectTargets { get; }
        public IReadOnlyList<StructureCombatFacts> EffectStructures { get; }
        public IReadOnlyList<ThornFieldCellSnapshot> Thorns { get; }
        public IReadOnlyList<SoulFieldFacts> Souls { get; }
        public IReadOnlyList<OwnedCharacterRecord> Collectors { get; }
        public IReadOnlyDictionary<CharacterInstanceId, int> SoulCounts { get; }
        public MainCharacterPersistentState Main { get; }
        public IReadOnlyList<UnitSpec> Definitions { get; }
        public IReadOnlyList<(EffectOriginKey Origin, int Attack, int MaxHealth, int Heal, int Duration)> ArtifactInputs { get; }

        public MovementPreviewContext(long battleInstanceId, Owner observer, CharacterCombatFacts actor,
            CombatCommandKind intentKind, MovementPath path = null, CellCoord? returnCheckpoint = null,
            MovementEnvironmentFacts visibleEnvironment = null, CombatCommandState admission = null,
            IReadOnlyList<(EntityId Id, Owner Owner, IReadOnlyList<CellCoord> Cells)> occupancy = null,
            IReadOnlyList<CharacterCombatFacts> effectTargets = null, IReadOnlyList<StructureCombatFacts> effectStructures = null,
            IReadOnlyList<ThornFieldCellSnapshot> thorns = null, IReadOnlyList<SoulFieldFacts> souls = null,
            IReadOnlyList<OwnedCharacterRecord> collectors = null, IReadOnlyDictionary<CharacterInstanceId, int> soulCounts = null,
            MainCharacterPersistentState main = null, IReadOnlyList<UnitSpec> definitions = null,
            IReadOnlyList<(EffectOriginKey Origin, int Attack, int MaxHealth, int Heal, int Duration)> artifactInputs = null,
            EnvironmentSemanticBindings environmentBindings = null, IReadOnlyList<ObstacleContentProfile> obstacleContents = null)
        {
            if (battleInstanceId < 1) throw new ArgumentOutOfRangeException(nameof(battleInstanceId));
            if (observer != Owner.Player && observer != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(observer));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (intentKind != CombatCommandKind.Move && intentKind != CombatCommandKind.ReturnToCheckpoint)
                throw new ArgumentOutOfRangeException(nameof(intentKind));
            if (intentKind == CombatCommandKind.Move ? path == null || returnCheckpoint.HasValue :
                path != null || !returnCheckpoint.HasValue)
                throw new ArgumentException("이동은 경로, 복귀는 체크포인트 기대값이 필요합니다.");
            BattleInstanceId = battleInstanceId;
            Observer = observer;
            ActorId = actor.EntityId;
            IntentKind = intentKind;
            Path = path;
            ReturnCheckpoint = returnCheckpoint;
            Area = actor.Area;
            Anchor = actor.Anchor;
            Rotation = actor.Rotation;
            OccupiedCells = Array.AsReadOnly(actor.OccupiedCells.ToArray());
            Health = actor.Health;
            MoveMethod = actor.Stats.MoveMethod;
            MoveRange = actor.Stats.MoveRange;
            MoveCost = actor.Stats.MoveCost;
            RemainingMoveRange = actor.Stats.RemainingMoveRange;
            CurrentActionCost = actor.Stats.CurrentActionCost;
            MaxActionCost = actor.Stats.MaxActionCost;
            MovementCheckpointAnchor = actor.MovementCheckpointAnchor;
            CommittedMoveDistance = actor.CommittedMoveDistance;
            PendingMoveActionCost = actor.PendingMoveActionCost;
            PendingMoveDistance = actor.PendingMoveDistance;
            AttackRefundEligibleMoveCost = actor.AttackRefundEligibleMoveCost;
            PositionModifier = actor.PositionModifier;
            // 생성 경계에서도 필터링하여 호출자가 숨겨진 원본을 넘겨도 공개 문맥에 남기지 않는다.
            VisibleEnvironment = (visibleEnvironment ?? MovementEnvironmentFacts.Empty).VisibleTo(observer);
            ObstacleContents = Array.AsReadOnly((obstacleContents ?? Array.Empty<ObstacleContentProfile>()).ToArray());
            // 비용·방어·밀치기 계산이 읽은 공개 지형만 분류 정보로 남긴다.
            var terrainIds = VisibleEnvironment.Tiles.OfType<StaticTerrainFact>()
                .Select(t => t.Id).ToHashSet();
            VisibleTerrainBindings = new EnvironmentSemanticBindings(
                (environmentBindings ?? EnvironmentSemanticBindings.Empty).TerrainSourceIds.Where(terrainIds.Contains).ToArray(),
                Array.Empty<long>(), Array.Empty<EntityId>());
            Mover = actor;
            HasExpectedFacts = admission != null;
            BoardWidth = admission?.BoardWidth ?? 0; BoardHeight = admission?.BoardHeight ?? 0;
            Phase = admission?.Phase ?? CombatFlowPhase.None; ActiveActorId = admission?.ActiveActorId ?? EntityId.Empty;
            IsNight = admission?.IsNight ?? false; StagePhase = admission?.Turn.StagePhase ?? default;
            CurrentTime = admission?.Turn.CurrentTime ?? 0; CompletedUnitTurnCount = admission?.Turn.CompletedUnitTurnCount ?? 0;
            Occupancy = Array.AsReadOnly((occupancy ?? Array.Empty<(EntityId, Owner, IReadOnlyList<CellCoord>)>())
                .Select(o => (o.Id, o.Owner, (IReadOnlyList<CellCoord>)Array.AsReadOnly(o.Cells.ToArray()))).ToArray());
            EffectTargets = Array.AsReadOnly((effectTargets ?? Array.Empty<CharacterCombatFacts>()).ToArray());
            EffectStructures = Array.AsReadOnly((effectStructures ?? Array.Empty<StructureCombatFacts>()).ToArray());
            Thorns = Array.AsReadOnly((thorns ?? Array.Empty<ThornFieldCellSnapshot>()).ToArray());
            Souls = Array.AsReadOnly((souls ?? Array.Empty<SoulFieldFacts>()).ToArray());
            Collectors = Array.AsReadOnly((collectors ?? Array.Empty<OwnedCharacterRecord>()).ToArray());
            SoulCounts = new System.Collections.ObjectModel.ReadOnlyDictionary<CharacterInstanceId, int>(
                (soulCounts ?? new Dictionary<CharacterInstanceId, int>()).ToDictionary(p => p.Key, p => p.Value));
            Main = main; Definitions = Array.AsReadOnly((definitions ?? Array.Empty<UnitSpec>()).ToArray());
            ArtifactInputs = Array.AsReadOnly((artifactInputs ?? Array.Empty<(EffectOriginKey, int, int, int, int)>()).ToArray());
        }
    }
}
