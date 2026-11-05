#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>
    /// 개발 전용 전투 준비 입력(fixture)입니다(설계 W7·§8, 계획 B2.2).
    /// Core가 이 정의로 전투 준비를 계산·기록하며, View는 Buffer에 쓰지 않습니다.
    /// </summary>
    /// <remarks>
    /// Play From Boot의 Fixture 모드는 2026-10-28 사용자 지시로 지웠습니다. 지금 이 클래스를 쓰는 곳은 천명님 시험
    /// <c>CategoryDefinitionMigrationTests</c>뿐이고, 그 시험을 위해 남겨 둡니다(사용자 결정). 게임 실행 경로에서는 만들지 않습니다.
    /// DB를 읽어 운영 전투를 조립하지 않습니다(운영 준비는 Core의 T1/G7). 유닛 수치는 UnitDataSO 에셋 값을 손으로 옮긴 사본입니다.
    /// 보유 로스터(PlayerLoadout_1: 메인 캐릭터 + Akimbo·ShotGun·Sniper)의 키를 모두 정의해야 준비가 성공합니다.
    /// 유닛은 1칸·Base로 단순화했고 특성은 에셋과 같게 Akimbo(이동 후 공격 할인)·Sniper(집중)만 있습니다. ShotGun은 Fan/Direction, 나머지는 RadialSquare/Single입니다.
    /// 스테이지 골드(시작 골드·수동 드로우 비용)는 요청 스테이지의 DB 경제 값(StageEconomy)을 그대로 씁니다. 골드는 Core가 전투 상태에 둡니다(2026-10-06 Core 재화 분리).
    /// 아이템 정의는 운영 DB 소스와 같은 선택(DB 소모 아이템 카탈로그 전체)을 같은 Core 변환으로 넣습니다(시작 아이템 설계 DI9).
    /// 아이템 값은 여기에 베끼지 않습니다. Core가 전투별 아이템 선택 규칙을 바꾸면 여기도 따라야 합니다. 아이템 표가 없는 조회(시험용 임시 DB)는 아이템 없이 만듭니다.
    /// 진입 판정은 DB의 요청 노드를, 경제를 뺀 전투 내용은 이 개발 정의를 따릅니다. 필드 설정은 생성 시 주입합니다.
    /// 대상별 정의 일치나 운영 DB 준비 성공을 검증하는 source가 아닙니다(설계 §8.2·§22).
    /// </remarks>
    internal sealed class DevBattleInitializationSource : IBattleInitializationSource
    {
        private const int DefaultMapWidth = 8;
        private const int DefaultMapHeight = 7;
        private const int DefaultPlayerDeploymentDepth = 2;
        private const int ReserveCapacity = 5;
        private const int TimelineSeed = 42;

        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly int _playerDeploymentDepth;

        /// <summary>재생 시작 때 필드 설정을 고정하고 초기 배치에 필요한 공간을 검사합니다.</summary>
        /// <remarks>아군 배치 구역은 아래부터 줄 수입니다. 적 배치 구역은 없고, 적은 아군 구역 밖에 놓입니다(Core 규칙).</remarks>
        internal DevBattleInitializationSource(int mapWidth = DefaultMapWidth, int mapHeight = DefaultMapHeight,
            int playerDeploymentDepth = DefaultPlayerDeploymentDepth)
        {
            string error = FieldError(mapWidth, mapHeight, playerDeploymentDepth);
            if (error != null)
                throw new ArgumentException($"[DevBattle] {error}");

            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _playerDeploymentDepth = playerDeploymentDepth;
        }

        /// <summary>개발 필드 값이 이 Fixture를 만들 수 있는지 봅니다. 맞으면 null, 아니면 이유 문구입니다.</summary>
        private static string FieldError(int mapWidth, int mapHeight, int playerDeploymentDepth)
        {
            if (mapWidth <= 0 || mapHeight <= 0 || playerDeploymentDepth <= 0)
                return "필드 가로·세로·아군 배치 줄 수는 모두 1 이상이어야 합니다.";
            if (playerDeploymentDepth >= mapHeight)
                return "아군 배치 줄 수는 필드 세로보다 작아야 합니다.";
            if ((long)mapWidth * (mapHeight - playerDeploymentDepth) < 2)
                return "아군 배치 구역 밖에 적 유닛과 본부를 놓을 칸이 2칸 이상 필요합니다.";
            return null;
        }

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            // 요청한 전투 스테이지를 검증한 뒤 개발 정의를 만든다. 골드는 Run Buffer가 소유한다.
            ValidateCombatStage(database, stageKey);
            return CreateDefinition(ReadItemDefinitions(database));
        }

        /// <summary>운영 DB 소스와 같이 소모 아이템 카탈로그 전체를 Core 정의로 바꿉니다. 표가 없으면 null(아이템 없음)입니다.</summary>
        private static IReadOnlyList<ItemDefinition> ReadItemDefinitions(IDataBaseQueryContext database)
        {
            var items = (database.InGameQuery as IHasConsumableItemTable)?.ConsumableItems;
            return items == null ? null : ItemDefinitionAdapter.Build(items.GetAll());
        }

        /// <summary>개발 정의에서도 요청한 스테이지가 전투인지 확인한다.</summary>
        private static void ValidateCombatStage(IDataBaseQueryContext database, StageKey stageKey)
        {
            var query = database?.InGameQuery ?? throw new ArgumentNullException(nameof(database));
            if (stageKey.IsEmpty)
                throw new ArgumentException("[DevBattle] 스테이지 키가 필요합니다.", nameof(stageKey));
            if (query.StageTableQuery.GetStage(stageKey.Value).StageData is not CombatStageDetail)
                throw new InvalidOperationException($"[DevBattle] 전투 스테이지가 아닙니다. StageKey: {stageKey.Value}");

        }

        private BattleInitialization CreateDefinition(IReadOnlyList<ItemDefinition> items)
        {
            // 수치는 DataBaseSO/InGameGroup/Unit/Item/Unit_*.asset(UnitDataSO)의 사본이다(2026-10-06 기준).
            // DB 준비 소스(T1/G7)가 연결되면 이 fixture와 함께 사라지며, 그 전에 에셋 값을 바꾸면 여기도 맞춘다.
            UnitSpec[] units =
            {
                Unit(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 50, attackRange: 1, attackPower: 1,
                    priority: 30),
                Unit(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 10, attackRange: 1, attackPower: 1,
                    priority: 10,
                    new KillChainAttackTraitSpec()),
                Unit(EntityKey.Unit_ShotGun, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 10, attackRange: 2, attackPower: 1,
                    priority: 10),
                Unit(EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                    maxHealth: 10, attackRange: 4, attackPower: 1,
                    priority: 10,
                    new FocusTraitSpec(damagePerStack: 1, maxStacks: 3)),
                Unit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                    maxHealth: 5, attackRange: 1, attackPower: 1,
                    priority: 10)
            };

            // 적·본부는 아군 구역 위쪽 남은 줄(최대 2줄)에 겹치지 않게 둔다. 기본 8×7·아군 2줄에서는 옛 위치와 같다.
            int top = Math.Min(2, _mapHeight - _playerDeploymentDepth);
            var mainAnchor = new CellCoord((_mapWidth - 1) / 2, 0);
            var demonAnchor = new CellCoord((_mapWidth - 1) / 2, _mapHeight - top);
            var headquartersAnchor = top > 1
                ? new CellCoord(_mapWidth / 2, _mapHeight - 1)
                : new CellCoord((demonAnchor.X + 1) % _mapWidth, _mapHeight - 1);

            var stage = new StageConfig(_mapWidth, _mapHeight, ReserveCapacity,
                PlayerDeploymentZone.Rows(_playerDeploymentDepth),
                TimelineSeed,
                new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, demonAnchor, Rotation4.R0) },
                Array.Empty<CellCoord>(),
                Array.Empty<StageGimmickSpec>(),
                new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, headquartersAnchor, Rotation4.R0) });

            StructureSpec[] structures =
            {
                new(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, maxHealth: 3, isAttackable: false, attackPower: 0)
            };

            CategorySpec[] categories =
            {
                new(CategoryKey.Main, Array.Empty<CategoryRuleSpec>(), 3),
                new(CategoryKey.Rider, Array.Empty<CategoryRuleSpec>(), 4),
                new(CategoryKey.Heavy, Array.Empty<CategoryRuleSpec>(), 3),
                new(CategoryKey.Cannon, new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() }, 2)
            };

            // 지정 방식은 Core 인벤토리 §1.1의 사용자 결정과 같다(설계 §21.2): ShotGun은 Fan/Direction, 나머지는 RadialSquare/Single.
            // 방향 공격을 개발 전투에서 검증하려고 ShotGun만 다르게 둔다. 옛 공격 패턴(UnitSpec)은 Core 호환 검사용으로 그대로 둔다.
            var profiles = new CombatAttackProfile[units.Length];
            for (int i = 0; i < units.Length; i++)
            {
                TargetingMethodSpec method = units[i].Key == EntityKey.Unit_ShotGun
                    ? new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0)
                    : new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
                profiles[i] = new CombatAttackProfile(units[i], method);
            }

            return new BattleInitialization(stage, mainAnchor, Rotation4.R0, units, structures, categories, profiles,
                itemDefinitions: items);
        }

        private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility,
            int maxHealth, int attackRange, int attackPower,
            int priority, params TraitSpec[] traits)
        {
            return new UnitSpec(key, role, 1, 1, new[] { CellCoord.Zero }, maxHealth, MoveMethod.LiftAndPlace,
                attackRange, CategoryKey.Main, attackPower, 0, priority, traits, eligibility);
        }
    }
}
#endif
