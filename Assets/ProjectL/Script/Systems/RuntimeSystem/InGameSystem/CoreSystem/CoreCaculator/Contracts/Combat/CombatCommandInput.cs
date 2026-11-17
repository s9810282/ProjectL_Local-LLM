using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatCommandKind
    {
        None = 0, PlaceMainCharacter = 1, PlaceUnit = 2, ReturnUnit = 3,
        EnterCombat = 4, Move = 5, Attack = 6, EndPlayerTurn = 7, StartControllerTurn = 13, CompleteEnemyTurn = 14, EquipItem = 10, UnequipItem = 11, ReplaceItem = 12, UndoMove = 15,
        UsePlayerSkill = 16,
        /// <summary>장애물 계산 API가 만드는 기폭 전이의 내부 처리 종류다.</summary>
        DetonatePowderBarrel = 17
    }

    /// <summary>Systems가 Buffer 원본을 읽어 한 명령 동안만 제공하는 불변 입력이다.</summary>
    public sealed class CombatCommandInput
    {
        public int? GoldBalance { get; }
        public int? FameBalance { get; }
        public int? ItemKey { get; }
        public ItemInventorySnapshot ItemInventory { get; }
        public IReadOnlyList<ItemDefinition> ItemDefinitions { get; }
        public int Number { get; }
        public CombatCommandKind Kind { get; }
        /// <summary>공개 사실로만 계산한 폐기용 입력이다. 저장소에는 게시할 수 없다.</summary>
        internal Owner? CalculationObserver { get; }
        internal CombatResolutionWindow ResolutionWindow { get; }
        public EntityId ActorId { get; }
        public AttackAim Aim { get; }
        public AttackSweepSide? SweepSide { get; }
        public CellCoord TargetCell { get; }
        public Rotation4 Rotation { get; }
        public CombatBattleFacts Battle { get; }
        public CombatProcessingKind ProcessingKind { get; }
        public CombatCommandState State { get; }
        public IReadOnlyList<CharacterCombatFacts> Characters { get; }
        public IReadOnlyList<CombatAttackProfile> Profiles { get; }
        public CharacterInstanceId MainId { get; }
        public MainCharacterPersistentState Main { get; }
        public IReadOnlyList<OwnedCharacterRecord> Owned { get; }
        public IReadOnlyDictionary<CharacterInstanceId, int> SoulCounts { get; }
        public SoulFieldSnapshot Souls { get; }
        public ArtifactLoadoutSnapshot Artifacts { get; }
        public MovementPath Path { get; }
        public MovementPreviewContext MovementContext { get; }
        public RunDeckSnapshot Membership { get; }
        public long BattleInstanceId { get; }
        public long ExpectedBattleInstanceId { get; }
        public long ExpectedControllerTurnSequence { get; }
        public int ExpectedMoveCommandNumber { get; }
        public IReadOnlyList<ArtifactItemState> ArtifactItems { get; }
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }
        public PlayerSkillRequest PlayerSkillRequest { get; }
        public PlayerSkillSourceStamp? PlayerSkillSource { get; }

        public CombatCommandInput(int number, CombatCommandKind kind, EntityId actorId, AttackAim aim,
            CombatCommandState state, IReadOnlyList<CharacterCombatFacts> characters,
            IReadOnlyList<CombatAttackProfile> profiles, MainCharacterPersistentState main,
            IReadOnlyList<OwnedCharacterRecord> owned, IReadOnlyDictionary<CharacterInstanceId, int> soulCounts,
            SoulFieldSnapshot souls, ArtifactLoadoutSnapshot artifacts,
            CellCoord targetCell = default, Rotation4 rotation = Rotation4.R0, CombatBattleFacts battle = null,
            CombatProcessingKind processingKind = CombatProcessingKind.Command,
            MovementPath path = null, MovementPreviewContext movementContext = null,
            long battleInstanceId = 0, IReadOnlyList<ArtifactItemState> artifactItems = null,
            EnvironmentSemanticBindings environmentBindings = null, IReadOnlyList<ObstacleContentProfile> obstacleContents = null,
            int? itemKey = null, ItemInventorySnapshot itemInventory = null, IReadOnlyList<ItemDefinition> itemDefinitions = null,
            int? goldBalance = null, int? fameBalance = null, RunDeckSnapshot membership = null,
            long expectedBattleInstanceId = 0, long expectedControllerTurnSequence = 0, int expectedMoveCommandNumber = 0,
            AttackSweepSide? sweepSide = null, PlayerSkillRequest playerSkillRequest = null,
            PlayerSkillSourceStamp? playerSkillSource = null)
        {
            if (processingKind == CombatProcessingKind.Command ? number < 1 : number != 0)
                throw new ArgumentOutOfRangeException(nameof(number));
            ValidateKind(kind);
            PlayerSkillRequest = playerSkillRequest;
            PlayerSkillSource = playerSkillSource;
            GoldBalance = ValidateBalance(goldBalance, nameof(goldBalance));
            FameBalance = ValidateBalance(fameBalance, nameof(fameBalance));
            ItemKey = itemKey;
            ItemInventory = itemInventory ?? ItemInventorySnapshot.Empty;
            ItemDefinitions = Array.AsReadOnly((itemDefinitions ?? Array.Empty<ItemDefinition>()).ToArray());
            if (ItemDefinitions.Any(d => d == null) || ItemDefinitions.Select(d => d.Key).Distinct().Count() != ItemDefinitions.Count)
                throw new ArgumentException("아이템 정의는 null이나 중복 키를 포함할 수 없습니다.", nameof(itemDefinitions));
            Number = number; Kind = kind; ActorId = actorId; Aim = aim;
            AttackSweepSideValidation.Validate(sweepSide);
            SweepSide = sweepSide;
            TargetCell = targetCell; Rotation = rotation;
            Battle = battle; ProcessingKind = processingKind;
            Path = path; MovementContext = movementContext;
            if (battleInstanceId < 0) throw new ArgumentOutOfRangeException(nameof(battleInstanceId));
            Membership = membership;
            BattleInstanceId = battleInstanceId;
            if (expectedBattleInstanceId < 0 || expectedControllerTurnSequence < 0) throw new ArgumentOutOfRangeException(nameof(expectedBattleInstanceId));
            ExpectedBattleInstanceId = expectedBattleInstanceId;
            ExpectedControllerTurnSequence = expectedControllerTurnSequence;
            if (expectedMoveCommandNumber < 0) throw new ArgumentOutOfRangeException(nameof(expectedMoveCommandNumber));
            ExpectedMoveCommandNumber = expectedMoveCommandNumber;
            ArtifactItems = artifactItems == null ? null : Array.AsReadOnly(artifactItems.ToArray());
            EnvironmentBindings = environmentBindings ?? EnvironmentSemanticBindings.Empty;
            ObstacleContents = Array.AsReadOnly((obstacleContents ?? Array.Empty<ObstacleContentProfile>()).ToArray());
            if (ObstacleContents.Any(p => p == null) || ObstacleContents.Select(p => p.EntityId).Distinct().Count() != ObstacleContents.Count)
                throw new ArgumentException("장애물 프로필은 null이나 중복 ID를 포함할 수 없습니다.");
            State = state ?? throw new ArgumentNullException(nameof(state));
            Characters = CopyFacts(characters, nameof(characters));
            Owned = CopyFacts(owned, nameof(owned));
            // 전투 메인은 진입 때 포착한 참가 목록이 정한다. 런타임 조립과 Buffer 게시는 항상 목록을 준다.
            // 목록이 없는 입력은 게시 없이 계산만 확인하는 순수 계산 입력이며, 그때만 전투 캐릭터의 유일한 메인을 쓴다.
            var fieldMainId = Characters.SingleOrDefault(c => c.OwnershipRole == CharacterOwnershipRole.Main)?.OwnedId;
            MainId = membership?.MainId ?? fieldMainId ?? CharacterInstanceId.Empty;
            if (membership != null && (fieldMainId != membership.MainId ||
                !Owned.Select(o => o.Id).OrderBy(id => id.Value).SequenceEqual(membership.OrderedIds.OrderBy(id => id.Value)) ||
                Characters.Any(c => c.Owner == Owner.Player && (!c.OwnedId.HasValue || !membership.OrderedIds.Contains(c.OwnedId.Value)))))
                throw new ArgumentException("전투 입력은 포착한 참가 목록과 일치해야 합니다.");
            Profiles = CopyFacts(profiles, nameof(profiles));
            SoulCounts = CopySoulCounts(soulCounts);
            Main = main ?? throw new ArgumentNullException(nameof(main));
            Souls = souls ?? throw new ArgumentNullException(nameof(souls));
            Artifacts = artifacts;
            if (Profiles.Select(p => (p.Definition.Key, p.OwnedId)).Distinct().Count() != Profiles.Count)
                throw new ArgumentException("같은 유닛 정의의 공격 방식을 중복 연결할 수 없습니다.");
        }

        /// <summary>자동 판단이 고른 행위자만 교체하며 요청의 기대 식별자는 보존한다.</summary>
        public CombatCommandInput WithActor(EntityId actorId) => new CombatCommandInput(this, actorId);

        internal CombatCommandInput ForSkillForecast(PlayerSkillRequest request) => new(this, request);

        public void RequireAuthoritativeCalculation()
        {
            if (CalculationObserver.HasValue)
                throw new InvalidOperationException("공개 정보 예측은 실제 전투에 설치할 수 없습니다.");
        }

        private CombatCommandInput(CombatCommandInput source, PlayerSkillRequest request) : this(source)
        {
            PlayerSkillRequest = request ?? throw new ArgumentNullException(nameof(request));
            CalculationObserver = Owner.Player;
        }

        private CombatCommandInput(CombatCommandInput source, EntityId actorId) : this(source)
        {
            ActorId = actorId;
        }

        /// <summary>같은 의도의 조준 등을 교체하면서 쓸기 방향을 보존합니다. null 경로와 문맥은 기존 값을 지웁니다.</summary>
        public CombatCommandInput WithIntent(CombatCommandKind kind, AttackAim aim, CellCoord targetCell,
            MovementPath path, MovementPreviewContext movementContext) =>
            WithIntent(kind, aim, targetCell, path, movementContext, SweepSide);

        /// <summary>새 의도의 쓸기 방향을 명시합니다. null을 넘기면 이전 선택을 지웁니다.</summary>
        public CombatCommandInput WithIntent(CombatCommandKind kind, AttackAim aim, CellCoord targetCell,
            MovementPath path, MovementPreviewContext movementContext, AttackSweepSide? sweepSide) =>
            new(this, kind, aim, targetCell, path, movementContext, sweepSide);

        /// <summary>투영한 전투 사실만 교체하고 보유·재고·잔액 문맥은 유지한다.</summary>
        public CombatCommandInput WithWorld(CombatCommandState state, IReadOnlyList<CharacterCombatFacts> characters,
            MainCharacterPersistentState main, IReadOnlyDictionary<CharacterInstanceId, int> soulCounts,
            SoulFieldSnapshot souls, EnvironmentSemanticBindings environmentBindings) =>
            new(this, state, characters, main, soulCounts, souls, environmentBindings);

        /// <summary>보유·재고·잔액 사실만 교체한다. null 잔액은 선택적 잔액 문맥을 지운다.</summary>
        public CombatCommandInput WithPersistentFacts(IReadOnlyList<OwnedCharacterRecord> owned,
            ItemInventorySnapshot itemInventory, int? goldBalance, int? fameBalance) =>
            new(this, owned, itemInventory, goldBalance, fameBalance);

        // 새 문맥 필드를 추가하면 이 생성자 한 곳에서 모든 파생 입력의 보존을 연결한다.
        private CombatCommandInput(CombatCommandInput source)
        {
            CalculationObserver = source.CalculationObserver;
            ResolutionWindow = source.ResolutionWindow;
            Number = source.Number;
            Kind = source.Kind;
            ActorId = source.ActorId;
            Aim = source.Aim;
            SweepSide = source.SweepSide;
            TargetCell = source.TargetCell;
            Rotation = source.Rotation;
            Battle = source.Battle;
            ProcessingKind = source.ProcessingKind;
            State = source.State;
            Characters = source.Characters;
            Profiles = source.Profiles;
            MainId = source.MainId;
            Main = source.Main;
            Owned = source.Owned;
            SoulCounts = source.SoulCounts;
            Souls = source.Souls;
            Artifacts = source.Artifacts;
            Path = source.Path;
            MovementContext = source.MovementContext;
            Membership = source.Membership;
            BattleInstanceId = source.BattleInstanceId;
            ExpectedBattleInstanceId = source.ExpectedBattleInstanceId;
            ExpectedControllerTurnSequence = source.ExpectedControllerTurnSequence;
            ExpectedMoveCommandNumber = source.ExpectedMoveCommandNumber;
            PlayerSkillRequest = source.PlayerSkillRequest;
            PlayerSkillSource = source.PlayerSkillSource;
            ArtifactItems = source.ArtifactItems;
            EnvironmentBindings = source.EnvironmentBindings;
            ObstacleContents = source.ObstacleContents;
            ItemKey = source.ItemKey;
            ItemInventory = source.ItemInventory;
            ItemDefinitions = source.ItemDefinitions;
            GoldBalance = source.GoldBalance;
            FameBalance = source.FameBalance;
        }

        private CombatCommandInput(CombatCommandInput source, CombatCommandKind kind, AttackAim aim,
            CellCoord targetCell, MovementPath path, MovementPreviewContext movementContext, AttackSweepSide? sweepSide)
            : this(source)
        {
            ValidateKind(kind);
            Kind = kind;
            Aim = aim;
            AttackSweepSideValidation.Validate(sweepSide);
            SweepSide = sweepSide;
            TargetCell = targetCell;
            Path = path;
            MovementContext = movementContext;
        }

        private CombatCommandInput(CombatCommandInput source, CombatCommandState state,
            IReadOnlyList<CharacterCombatFacts> characters, MainCharacterPersistentState main,
            IReadOnlyDictionary<CharacterInstanceId, int> soulCounts, SoulFieldSnapshot souls,
            EnvironmentSemanticBindings environmentBindings) : this(source)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Characters = CopyFacts(characters, nameof(characters));
            Main = main ?? throw new ArgumentNullException(nameof(main));
            SoulCounts = CopySoulCounts(soulCounts);
            Souls = souls ?? throw new ArgumentNullException(nameof(souls));
            EnvironmentBindings = environmentBindings ?? EnvironmentSemanticBindings.Empty;
        }

        private CombatCommandInput(CombatCommandInput source, IReadOnlyList<OwnedCharacterRecord> owned,
            ItemInventorySnapshot itemInventory, int? goldBalance, int? fameBalance) : this(source)
        {
            Owned = CopyFacts(owned, nameof(owned));
            ItemInventory = itemInventory ?? ItemInventorySnapshot.Empty;
            GoldBalance = ValidateBalance(goldBalance, nameof(goldBalance));
            FameBalance = ValidateBalance(fameBalance, nameof(fameBalance));
        }

        internal CombatCommandInput WithResolutionWindow(CombatResolutionWindow window) => new(this, window);

        private CombatCommandInput(CombatCommandInput source, CombatResolutionWindow window) : this(source)
        {
            if (!Enum.IsDefined(typeof(CombatResolutionWindow), window)) throw new ArgumentOutOfRangeException(nameof(window));
            ResolutionWindow = window;
        }

        private static void ValidateKind(CombatCommandKind kind)
        {
            if (kind == CombatCommandKind.None || !Enum.IsDefined(typeof(CombatCommandKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        private static int? ValidateBalance(int? balance, string name)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(name);
            return balance;
        }

        private static IReadOnlyList<T> CopyFacts<T>(IReadOnlyList<T> facts, string name) where T : class
        {
            var copy = (facts ?? throw new ArgumentNullException(name)).ToArray();
            if (copy.Any(f => f == null)) throw new ArgumentException("명령 사실 목록은 null을 포함할 수 없습니다.", name);
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyDictionary<CharacterInstanceId, int> CopySoulCounts(
            IReadOnlyDictionary<CharacterInstanceId, int> soulCounts) =>
            new ReadOnlyDictionary<CharacterInstanceId, int>(
                (soulCounts ?? throw new ArgumentNullException(nameof(soulCounts))).ToDictionary(p => p.Key, p => p.Value));
    }
}
