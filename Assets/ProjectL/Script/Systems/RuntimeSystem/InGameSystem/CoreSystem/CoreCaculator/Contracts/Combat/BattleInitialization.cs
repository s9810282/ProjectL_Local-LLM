using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>전투 진입에 필요한 정의 값이며 보유 상태나 전투 런타임을 포함하지 않는다.</summary>
    public sealed class BattleInitialization
    {
        public StageConfig Stage { get; }
        public CellCoord MainAnchor { get; }
        public Rotation4 MainRotation { get; }
        public IReadOnlyList<UnitSpec> Units { get; }
        public IReadOnlyList<StructureSpec> Structures { get; }
        public IReadOnlyList<CategorySpec> Categories { get; }
        public IReadOnlyList<CombatAttackProfile> AttackProfiles { get; }
        public StageEconomySpec Economy { get; }
        public MovementEnvironmentFacts Environment { get; }
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public WallBlockingSpec WallBlocking { get; }
        public IReadOnlyList<InitialObstaclePlacementSpec> ObstaclePlacements { get; }

        public BattleInitialization(StageConfig stage, CellCoord mainAnchor, Rotation4 mainRotation,
            IReadOnlyList<UnitSpec> units, IReadOnlyList<StructureSpec> structures,
            IReadOnlyList<CategorySpec> categories, IReadOnlyList<CombatAttackProfile> attackProfiles = null,
            StageEconomySpec economy = null, MovementEnvironmentFacts environment = null,
            EnvironmentSemanticBindings environmentBindings = null, IReadOnlyList<InitialObstaclePlacementSpec> obstaclePlacements = null,
            WallBlockingSpec wallBlocking = null)
        {
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            Economy = economy;
            Environment = environment ?? MovementEnvironmentFacts.Empty;
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

        /// <summary>공격 프로필만 바꾼 사본이다. 나머지 정의(환경·장애물·경제 포함)는 그대로 유지한다.</summary>
        public BattleInitialization WithAttackProfiles(IReadOnlyList<CombatAttackProfile> attackProfiles)
        {
            if (attackProfiles == null) throw new ArgumentNullException(nameof(attackProfiles));
            return new BattleInitialization(Stage, MainAnchor, MainRotation, Units, Structures, Categories, attackProfiles,
                Economy, Environment, EnvironmentBindings, ObstaclePlacements, WallBlocking);
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
