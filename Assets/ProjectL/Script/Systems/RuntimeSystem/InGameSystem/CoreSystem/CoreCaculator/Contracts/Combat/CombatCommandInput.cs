using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatCommandKind
    {
        None = 0, PlaceMainCharacter = 1, PlaceUnit = 2, ReturnUnit = 3,
        EnterCombat = 4, Move = 5, Attack = 6, EndUnitTurn = 7, DrawCharacter = 8, ReturnToCheckpoint = 9
    }

    /// <summary>Systems가 Buffer 원본을 읽어 한 명령 동안만 제공하는 불변 입력이다.</summary>
    public sealed class CombatCommandInput
    {
        public int Number { get; }
        public CombatCommandKind Kind { get; }
        public EntityId ActorId { get; }
        public AttackAim Aim { get; }
        public CellCoord TargetCell { get; }
        public Rotation4 Rotation { get; }
        public CombatBattleFacts Battle { get; }
        public CombatProcessingKind ProcessingKind { get; }
        public CombatCommandState State { get; }
        public IReadOnlyList<CharacterCombatFacts> Characters { get; }
        public IReadOnlyList<CombatAttackProfile> Profiles { get; }
        public MainCharacterPersistentState Main { get; }
        public IReadOnlyList<OwnedCharacterRecord> Owned { get; }
        public IReadOnlyDictionary<CharacterInstanceId, int> SoulCounts { get; }
        public SoulFieldSnapshot Souls { get; }
        public ArtifactLoadoutSnapshot Artifacts { get; }
        public MovementPath Path { get; }
        public MovementPreviewContext MovementContext { get; }
        public long BattleInstanceId { get; }
        public IReadOnlyList<ArtifactItemState> ArtifactItems { get; }
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }

        public CombatCommandInput(int number, CombatCommandKind kind, EntityId actorId, AttackAim aim,
            CombatCommandState state, IReadOnlyList<CharacterCombatFacts> characters,
            IReadOnlyList<CombatAttackProfile> profiles, MainCharacterPersistentState main,
            IReadOnlyList<OwnedCharacterRecord> owned, IReadOnlyDictionary<CharacterInstanceId, int> soulCounts,
            SoulFieldSnapshot souls, ArtifactLoadoutSnapshot artifacts,
            CellCoord targetCell = default, Rotation4 rotation = Rotation4.R0, CombatBattleFacts battle = null,
            CombatProcessingKind processingKind = CombatProcessingKind.Command,
            MovementPath path = null, MovementPreviewContext movementContext = null,
            long battleInstanceId = 0, IReadOnlyList<ArtifactItemState> artifactItems = null,
            EnvironmentSemanticBindings environmentBindings = null, IReadOnlyList<ObstacleContentProfile> obstacleContents = null)
        {
            if (processingKind == CombatProcessingKind.Command ? number < 1 : number != 0)
                throw new ArgumentOutOfRangeException(nameof(number));
            if (kind == CombatCommandKind.None || !Enum.IsDefined(typeof(CombatCommandKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Number = number; Kind = kind; ActorId = actorId; Aim = aim;
            TargetCell = targetCell; Rotation = rotation;
            Battle = battle; ProcessingKind = processingKind;
            Path = path; MovementContext = movementContext;
            if (battleInstanceId < 0) throw new ArgumentOutOfRangeException(nameof(battleInstanceId));
            BattleInstanceId = battleInstanceId;
            ArtifactItems = artifactItems == null ? null : Array.AsReadOnly(artifactItems.ToArray());
            EnvironmentBindings = environmentBindings ?? EnvironmentSemanticBindings.Empty;
            ObstacleContents = Array.AsReadOnly((obstacleContents ?? Array.Empty<ObstacleContentProfile>()).ToArray());
            if (ObstacleContents.Any(p => p == null) || ObstacleContents.Select(p => p.EntityId).Distinct().Count() != ObstacleContents.Count)
                throw new ArgumentException("장애물 프로필은 null이나 중복 ID를 포함할 수 없습니다.");
            State = state ?? throw new ArgumentNullException(nameof(state));
            Characters = Array.AsReadOnly((characters ?? throw new ArgumentNullException(nameof(characters))).ToArray());
            Profiles = Array.AsReadOnly((profiles ?? throw new ArgumentNullException(nameof(profiles))).ToArray());
            Owned = Array.AsReadOnly((owned ?? throw new ArgumentNullException(nameof(owned))).ToArray());
            SoulCounts = new ReadOnlyDictionary<CharacterInstanceId, int>(
                (soulCounts ?? throw new ArgumentNullException(nameof(soulCounts))).ToDictionary(p => p.Key, p => p.Value));
            Main = main ?? throw new ArgumentNullException(nameof(main));
            Souls = souls ?? throw new ArgumentNullException(nameof(souls));
            Artifacts = artifacts;
            if (Characters.Any(c => c == null) || Profiles.Any(p => p == null) || Owned.Any(o => o == null))
                throw new ArgumentException("명령 사실 목록은 null을 포함할 수 없습니다.");
            if (Profiles.Select(p => p.Definition.Key).Distinct().Count() != Profiles.Count)
                throw new ArgumentException("같은 유닛 정의의 공격 방식을 중복 연결할 수 없습니다.");
        }
    }
}
