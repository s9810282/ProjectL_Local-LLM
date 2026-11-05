using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item
{
    /// <summary>
    /// 유닛 카테고리 규칙의 설정값입니다. Core가 받는 순수 C# 데이터이며 CategoryConfigDataSO가 생성합니다.
    /// </summary>
    /// <remarks>
    /// Core는 이 값으로 CategorySpec 5개를 만듭니다. Cannon만 SafeBombardment 규칙을 가집니다.
    /// 각 카테고리의 레벨당 능력치 상승치도 여기서 정합니다. 레벨 적용 공식은 Core가 가집니다.
    /// 경험치·레벨업 수치(요구량 단계, 처치 경험치, 메인 성장량)도 같은 설정에 별도 묶음으로 둡니다. 공식은 Core가 가집니다.
    /// 카테고리 상성은 CategoryMatchupSpec.CreateDefault()를 쓰므로 DB 값이 없습니다.
    /// </remarks>
    public sealed class CategoryConfigData : IData
    {
        public IReadOnlyDictionary<CategoryKey, TraitKey> DefaultTraits { get; }
        public int MainMovementAllowance { get; }
        public int RiderMovementAllowance { get; }
        public int HeavyMovementAllowance { get; }
        public int CannonMovementAllowance { get; }
        public int BossMovementAllowance { get; }
        public CategoryLevelGrowth MainLevelGrowth { get; }
        public CategoryLevelGrowth RiderLevelGrowth { get; }
        public CategoryLevelGrowth HeavyLevelGrowth { get; }
        public CategoryLevelGrowth CannonLevelGrowth { get; }
        public CategoryLevelGrowth BossLevelGrowth { get; }
        /// <summary>경험치·레벨업 수치입니다. 기본값은 꺼짐(처치 경험치가 생기지 않음)입니다.</summary>
        public LevelProgressionSpec LevelProgression { get; }

        /// <summary>상승치를 생략한 카테고리는 레벨이 올라도 성장하지 않고, 레벨 진행을 생략하면 경험치가 꺼집니다.</summary>
        public CategoryConfigData(
            int mainMovementAllowance,
            int riderMovementAllowance,
            int heavyMovementAllowance,
            int cannonMovementAllowance,
            int bossMovementAllowance,
            CategoryLevelGrowth mainLevelGrowth = default,
            CategoryLevelGrowth riderLevelGrowth = default,
            CategoryLevelGrowth heavyLevelGrowth = default,
            CategoryLevelGrowth cannonLevelGrowth = default,
            CategoryLevelGrowth bossLevelGrowth = default,
            LevelProgressionSpec levelProgression = default,
            IReadOnlyDictionary<CategoryKey, TraitKey> defaultTraits = null)
        {
            DefaultTraits = new ReadOnlyDictionary<CategoryKey, TraitKey>(defaultTraits == null
                ? new Dictionary<CategoryKey, TraitKey>()
                : defaultTraits.ToDictionary(pair => pair.Key, pair => pair.Value));
            MainMovementAllowance = mainMovementAllowance;
            RiderMovementAllowance = riderMovementAllowance;
            HeavyMovementAllowance = heavyMovementAllowance;
            CannonMovementAllowance = cannonMovementAllowance;
            BossMovementAllowance = bossMovementAllowance;
            MainLevelGrowth = mainLevelGrowth;
            RiderLevelGrowth = riderLevelGrowth;
            HeavyLevelGrowth = heavyLevelGrowth;
            CannonLevelGrowth = cannonLevelGrowth;
            BossLevelGrowth = bossLevelGrowth;
            LevelProgression = levelProgression;
        }

        public void Validate()
        {
            ValidateMovement(MainMovementAllowance, "Main");
            ValidateMovement(RiderMovementAllowance, "Rider");
            ValidateMovement(HeavyMovementAllowance, "Heavy");
            ValidateMovement(CannonMovementAllowance, "Cannon");
            ValidateMovement(BossMovementAllowance, "Boss");
        }

        private static void ValidateMovement(int movementAllowance, string category)
        {
            if (movementAllowance < 0)
                throw new InvalidOperationException(
                    $"[CategoryConfigData] {category} movement allowance must be nonnegative.");
        }
    }
}
