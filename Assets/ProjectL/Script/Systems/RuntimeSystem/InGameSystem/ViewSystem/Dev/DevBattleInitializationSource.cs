#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>
    /// 개발 전용 전투 준비 입력(fixture)입니다(설계 W7·§8, 계획 B2.2).
    /// Core가 이 정의로 전투 준비를 계산·기록하며, View는 Buffer에 쓰지 않습니다.
    /// </summary>
    /// <remarks>
    /// DB를 읽어 운영 전투를 조립하지 않습니다(운영 준비는 Core의 T1/G7). 유닛 수치는 UnitDataSO 에셋 값을 손으로 옮긴 사본입니다.
    /// 보유 로스터(PlayerLoadout_1: 메인 캐릭터 + Akimbo·ShotGun·Sniper)의 키를 모두 정의해야 준비가 성공합니다.
    /// 유닛은 1칸·Base로 단순화했고 특성은 에셋과 같게 Akimbo(이동 후 공격 할인)·Sniper(집중)만 있습니다. ShotGun은 Fan/Direction, 나머지는 RadialSquare/Single입니다.
    /// 스테이지 골드(시작 골드·수동 드로우 비용)는 요청 스테이지의 DB 경제 값(StageEconomy)을 그대로 씁니다. 골드는 Core가 전투 상태에 둡니다(2026-10-06 Core 재화 분리).
    /// 두 모드 모두 진입 판정은 DB의 요청 노드를, 경제를 뺀 전투 내용은 이 개발 정의를 따릅니다. 필드 설정은 생성 시 주입합니다.
    /// 대상별 정의 일치나 운영 DB 준비 성공을 검증하는 source가 아닙니다(설계 §8.2·§22).
    /// </remarks>
    internal sealed class DevBattleInitializationSource : IBattleInitializationSource
    {
        internal const int DefaultMapWidth = 8;
        internal const int DefaultMapHeight = 7;
        internal const int DefaultPlayerDeploymentDepth = 2;
        internal const int DefaultEnemyDeploymentDepth = 2;
        private const int ReserveCapacity = 5;
        private const int TimelineSeed = 42;
        private const long NightLength = 500;

        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly int _playerDeploymentDepth;
        private readonly int _enemyDeploymentDepth;

        /// <summary>재생 시작 때 필드 설정을 고정하고 초기 배치에 필요한 공간을 검사합니다.</summary>
        internal DevBattleInitializationSource(int mapWidth = DefaultMapWidth, int mapHeight = DefaultMapHeight,
            int playerDeploymentDepth = DefaultPlayerDeploymentDepth, int enemyDeploymentDepth = DefaultEnemyDeploymentDepth)
        {
            if (mapWidth <= 0 || mapHeight <= 0 || playerDeploymentDepth <= 0 || enemyDeploymentDepth <= 0)
                throw new ArgumentException("[DevBattle] 필드 가로·세로·배치 깊이는 모두 1 이상이어야 합니다.");
            if ((long)playerDeploymentDepth + enemyDeploymentDepth >= mapHeight)
                throw new ArgumentException("[DevBattle] 아군·적군 배치 깊이의 합은 필드 세로보다 작아야 합니다.");
            if ((long)mapWidth * enemyDeploymentDepth < 2)
                throw new ArgumentException("[DevBattle] 적군 배치 구역에는 적 유닛과 본부를 놓을 최소 2칸이 필요합니다.");

            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _playerDeploymentDepth = playerDeploymentDepth;
            _enemyDeploymentDepth = enemyDeploymentDepth;
        }

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            // 전투마다 한 번 불린다. 경제만 요청 스테이지의 DB 값이고 나머지는 이 개발 정의다.
            return CreateDefinition(ReadStageEconomy(database, stageKey));
        }

        /// <summary>
        /// 요청 스테이지의 경제(시작 골드·수동 드로우 비용)를 DB에서 읽습니다. 조회 경로는 DB 전투 소스(BattleDefinitionAdapter)와 같습니다.
        /// 전투 스테이지가 아니거나 경제가 없으면 준비를 멈춥니다(조용히 0으로 바꾸지 않음).
        /// </summary>
        private static StageEconomySpec ReadStageEconomy(IDataBaseQueryContext database, StageKey stageKey)
        {
            var query = database?.InGameQuery ?? throw new ArgumentNullException(nameof(database));
            if (stageKey.IsEmpty)
                throw new ArgumentException("[DevBattle] 스테이지 키가 필요합니다.", nameof(stageKey));
            if (query.StageTableQuery.GetStage(stageKey.Value).StageData is not CombatStageDetail combat)
                throw new InvalidOperationException($"[DevBattle] 전투 스테이지가 아닙니다. StageKey: {stageKey.Value}");

            var economy = query.StageEconomyTableQuery.GetStageEconomy(combat.EconomyKey);
            economy.Validate();
            return new StageEconomySpec(economy.InitialGold, economy.ManualDrawGoldCost);
        }

        private BattleInitialization CreateDefinition(StageEconomySpec economy)
        {
            // 수치는 DataBaseSO/InGameGroup/Unit/Item/Unit_*.asset(UnitDataSO)의 사본이다(2026-10-06 기준).
            // DB 준비 소스(T1/G7)가 연결되면 이 fixture와 함께 사라지며, 그 전에 에셋 값을 바꾸면 여기도 맞춘다.
            UnitSpec[] units =
            {
                Unit(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 50, moveRange: 3, moveCost: 1, attackRange: 1, attackPower: 1,
                    maxActionCost: 5, attackCost: 1, priority: 30, delay: 20),
                Unit(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 10, moveRange: 3, moveCost: 1, attackRange: 1, attackPower: 1,
                    maxActionCost: 5, attackCost: 2, priority: 10, delay: 10,
                    new MoveAttackDiscountTraitSpec(discountAmount: 1, minimumAttackCost: 1)),
                Unit(EntityKey.Unit_ShotGun, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 10, moveRange: 3, moveCost: 1, attackRange: 2, attackPower: 1,
                    maxActionCost: 3, attackCost: 1, priority: 10, delay: 10),
                Unit(EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 10, moveRange: 3, moveCost: 1, attackRange: 4, attackPower: 1,
                    maxActionCost: 3, attackCost: 1, priority: 10, delay: 10,
                    new FocusTraitSpec(damagePerStack: 1, maxStacks: 3)),
                Unit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                    maxHealth: 5, moveRange: 3, moveCost: 1, attackRange: 1, attackPower: 1,
                    maxActionCost: 3, attackCost: 1, priority: 10, delay: 10)
            };

            // 기본 8×7에서는 기존 위치를 유지하며, 작은 필드에서도 각 배치 구역 안에 겹치지 않게 둔다.
            var mainAnchor = new CellCoord((_mapWidth - 1) / 2, 0);
            var demonAnchor = new CellCoord((_mapWidth - 1) / 2, _mapHeight - _enemyDeploymentDepth);
            var headquartersAnchor = _enemyDeploymentDepth > 1
                ? new CellCoord(_mapWidth / 2, _mapHeight - 1)
                : new CellCoord((demonAnchor.X + 1) % _mapWidth, _mapHeight - 1);

            var stage = new StageConfig(_mapWidth, _mapHeight, ReserveCapacity, _playerDeploymentDepth, _enemyDeploymentDepth,
                TimelineSeed,
                new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, demonAnchor, Rotation4.R0) },
                Array.Empty<CellCoord>(),
                Array.Empty<StageGimmickSpec>(),
                NightLength,
                new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, headquartersAnchor, Rotation4.R0) });

            StructureSpec[] structures =
            {
                new(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, maxHealth: 3, isAttackable: false, attackPower: 0)
            };

            CategorySpec[] categories =
            {
                new(CategoryKey.Base, Array.Empty<CategoryRuleSpec>()),
                new(CategoryKey.Rider, new CategoryRuleSpec[] { new ManeuverRefundCategoryRuleSpec() }),
                new(CategoryKey.Heavy, new CategoryRuleSpec[] { new DirectionalArmorCategoryRuleSpec(50, Rotation4.R0) }),
                new(CategoryKey.Cannon, new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() })
            };

            // 지정 방식은 Core 인벤토리 §1.1의 사용자 결정과 같다(설계 §21.2): ShotGun은 Fan/Direction, 나머지는 RadialSquare/Single.
            // 방향 공격을 개발 Live에서 검증하려고 ShotGun만 다르게 둔다. 옛 공격 패턴(UnitSpec)은 Core 호환 검사용으로 그대로 둔다.
            var profiles = new CombatAttackProfile[units.Length];
            for (int i = 0; i < units.Length; i++)
            {
                TargetingMethodSpec method = units[i].Key == EntityKey.Unit_ShotGun
                    ? new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0)
                    : new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
                profiles[i] = new CombatAttackProfile(units[i], method);
            }

            return new BattleInitialization(stage, mainAnchor, Rotation4.R0, units, structures, categories, profiles,
                economy);
        }

        private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility,
            int maxHealth, int moveRange, int moveCost, int attackRange, int attackPower,
            int maxActionCost, int attackCost, int priority, int delay, params TraitSpec[] traits)
        {
            return new UnitSpec(key, role, 1, 1, new[] { CellCoord.Zero }, maxHealth, MoveMethod.LiftAndPlace,
                moveRange, moveCost, attackRange,
                CategoryKey.Base, attackPower, maxActionCost, attackCost,
                physicalDefense: 0, priority, delay, hasTimelineConfiguration: true, traits, eligibility);
        }
    }
}
#endif
