using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item
{
    /// <summary>
    /// 유닛 카테고리 규칙의 설정값입니다. Core가 받는 순수 C# 데이터이며 CategoryConfigDataSO가 생성합니다.
    /// </summary>
    /// <remarks>
    /// Core는 이 값으로 CategorySpec 4개를 만듭니다: Base(규칙 없음), Rider(ManeuverRefund),
    /// Heavy(DirectionalArmor: 이 설정의 피해 감소율과 초기 방향), Cannon(SafeBombardment).
    /// 카테고리 상성은 CategoryMatchupSpec.CreateDefault()를 쓰므로 DB 값이 없습니다.
    /// </remarks>
    public sealed class CategoryConfigData : IData
    {
        public int HeavyDamageReductionPercent { get; }
        public Rotation4 HeavyInitialFacing { get; }

        public CategoryConfigData(int heavyDamageReductionPercent, Rotation4 heavyInitialFacing)
        {
            HeavyDamageReductionPercent = heavyDamageReductionPercent;
            HeavyInitialFacing = heavyInitialFacing;
        }

        public void Validate()
        {
            if (HeavyDamageReductionPercent < 1 || HeavyDamageReductionPercent > 100)
                throw new InvalidOperationException("[CategoryConfigData] HeavyDamageReductionPercent must be between 1 and 100.");
            if (!System.Enum.IsDefined(typeof(Rotation4), HeavyInitialFacing))
                throw new InvalidOperationException("[CategoryConfigData] HeavyInitialFacing is invalid.");
        }
    }
}
