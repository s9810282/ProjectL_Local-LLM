using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>전투 진입에 필요한 정의 값이며 보유 상태나 전투 런타임을 포함하지 않는다.</summary>
    public sealed class BattleInitialization
    {
        public TraitGrowthCatalog TraitGrowth { get; }
        public IReadOnlyList<ItemDefinition> ItemDefinitions { get; }
        public StageConfig Stage { get; }
        public CellCoord MainAnchor { get; }
        public Rotation4 MainRotation { get; }
        public IReadOnlyList<UnitSpec> Units { get; }
        public IReadOnlyList<StructureSpec> Structures { get; }
        public IReadOnlyList<CategorySpec> Categories { get; }
        public IReadOnlyList<CombatAttackProfile> AttackProfiles { get; }
        public MovementEnvironmentFacts Environment { get; }
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public WallBlockingSpec WallBlocking { get; }
        public IReadOnlyList<InitialObstaclePlacementSpec> ObstaclePlacements { get; }
        /// <summary>승리 목표 종류다. 지정 보스 목표의 대상은 초기 적 배치의 표식으로 준비 때 결합한다.</summary>
        public BattleObjectiveKind ObjectiveKind { get; }
        /// <summary>경험치·레벨업 수치다. 기본값은 꺼짐이다(설정이 없는 개발 정의·시험).</summary>
        public LevelProgressionSpec Progression { get; }
        public StageClearRewardSpec ClearRewards { get; }
        public NexusReinforcementSpec NexusReinforcements { get; }
        /// <summary>
        /// 전투 준비가 칸을 정할 최초 무작위 토네이도다. 정의 입력이며 전투 중 환경은 설치된 명령 상태에서 읽는다.
        /// </summary>
        public IReadOnlyList<InitialRandomTornadoPlacementSpec> InitialRandomTornadoPlacements { get; }

        public BattleInitialization(StageConfig stage, CellCoord mainAnchor, Rotation4 mainRotation,
            IReadOnlyList<UnitSpec> units, IReadOnlyList<StructureSpec> structures,
            IReadOnlyList<CategorySpec> categories, IReadOnlyList<CombatAttackProfile> attackProfiles = null,
            MovementEnvironmentFacts environment = null,
            EnvironmentSemanticBindings environmentBindings = null, IReadOnlyList<InitialObstaclePlacementSpec> obstaclePlacements = null,
            WallBlockingSpec wallBlocking = null,
            BattleObjectiveKind objectiveKind = BattleObjectiveKind.EnemyHeadquartersDestroyed,
            IReadOnlyList<ItemDefinition> itemDefinitions = null, LevelProgressionSpec progression = default,
            IReadOnlyList<InitialRandomTornadoPlacementSpec> initialRandomTornadoPlacements = null, StageClearRewardSpec clearRewards = null, TraitGrowthCatalog traitGrowth = null,
            NexusReinforcementSpec nexusReinforcements = null)
        {
            TraitGrowth = traitGrowth ?? TraitGrowthCatalog.Disabled;
            ItemDefinitions = Copy(itemDefinitions ?? Array.Empty<ItemDefinition>());
            InitialRandomTornadoPlacements = Copy(initialRandomTornadoPlacements ?? Array.Empty<InitialRandomTornadoPlacementSpec>());
            Progression = progression;
            ClearRewards = clearRewards;
            NexusReinforcements = nexusReinforcements;
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            if (objectiveKind == BattleObjectiveKind.None || Enum.IsDefined(typeof(BattleObjectiveKind), objectiveKind) == false)
                throw new ArgumentOutOfRangeException(nameof(objectiveKind));
            ObjectiveKind = objectiveKind;
            Environment = environment ?? MovementEnvironmentFacts.Empty;
            foreach (var tile in Environment.Tiles)
                if (tile is TraitTrailFact)
                    throw new ArgumentException("전투 정의에는 이전 전투의 특성 흔적을 넣을 수 없습니다.", nameof(environment));
            EnvironmentBindings = environmentBindings ?? EnvironmentSemanticBindings.Empty;
            WallBlocking = wallBlocking ?? WallBlockingSpec.Default;
            ObstaclePlacements = Copy(obstaclePlacements ?? Array.Empty<InitialObstaclePlacementSpec>());
            if (Enum.IsDefined(typeof(Rotation4), mainRotation) == false)
                throw new ArgumentOutOfRangeException(nameof(mainRotation));

            MainAnchor = mainAnchor;
            MainRotation = mainRotation;
            Units = Copy(units);
            Structures = Copy(structures);
            Categories = Copy(categories);
            AttackProfiles = Copy(attackProfiles ?? Array.Empty<CombatAttackProfile>());
        }

        /// <summary>공격 프로필만 바꾼 사본이다. 나머지 정의(환경·장애물·최초 무작위 토네이도 포함)는 그대로 유지한다.</summary>
        public BattleInitialization WithAttackProfiles(IReadOnlyList<CombatAttackProfile> attackProfiles)
        {
            if (attackProfiles == null)
                throw new ArgumentNullException(nameof(attackProfiles));
            return new BattleInitialization(Stage, MainAnchor, MainRotation, Units, Structures, Categories, attackProfiles, Environment, EnvironmentBindings, ObstaclePlacements, WallBlocking, ObjectiveKind, ItemDefinitions, Progression, InitialRandomTornadoPlacements, ClearRewards, TraitGrowth, NexusReinforcements);
        }

        public BattleInitialization WithTraitGrowth(TraitGrowthCatalog traitGrowth)
        {
            if (traitGrowth == null)
                throw new ArgumentNullException(nameof(traitGrowth));
            return new BattleInitialization(Stage, MainAnchor, MainRotation, Units, Structures, Categories, AttackProfiles,
                Environment, EnvironmentBindings, ObstaclePlacements, WallBlocking, ObjectiveKind, ItemDefinitions,
                Progression, InitialRandomTornadoPlacements, ClearRewards, traitGrowth, NexusReinforcements);
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source) where T : class
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = source[i] ?? throw new ArgumentException("정의 목록에는 null을 넣을 수 없습니다.");

            return Array.AsReadOnly(copy);
        }
    }
}
