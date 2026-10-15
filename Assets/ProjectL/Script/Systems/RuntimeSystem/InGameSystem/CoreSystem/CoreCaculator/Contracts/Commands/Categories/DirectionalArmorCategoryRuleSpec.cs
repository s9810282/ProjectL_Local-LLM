using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories
{
    /// <summary>
    /// Heavy의 전방 피해 경감 규칙입니다.
    /// 경감률 50은 사용자 확정값이며, 초기 Facing은 기획 확인이 남아 있어 명시 입력으로 받습니다.
    /// </summary>
    public sealed class DirectionalArmorCategoryRuleSpec : CategoryRuleSpec
    {
        public int DamageReductionPercent { get; }
        public Rotation4 InitialFacing { get; }

        public DirectionalArmorCategoryRuleSpec(int damageReductionPercent, Rotation4 initialFacing)
            : base(CategoryRuleKey.DirectionalArmor)
        {
            if (damageReductionPercent < 1 || damageReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(damageReductionPercent));
            if (Enum.IsDefined(typeof(Rotation4), initialFacing) == false)
                throw new ArgumentOutOfRangeException(nameof(initialFacing));

            DamageReductionPercent = damageReductionPercent;
            InitialFacing = initialFacing;
        }
    }
}
