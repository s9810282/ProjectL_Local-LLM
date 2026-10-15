using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>
    /// Focus Trait가 사용하는 변경되지 않는 피해 및 스택 설정입니다.
    /// </summary>
    public sealed class FocusTraitDefinition : TraitDefinition
    {
        /// <summary>
        /// 활성 Focus 스택 하나가 각 공격 대상의 피해에 더하는 고정 피해량입니다.
        /// </summary>
        public int DamagePerStack { get; }

        /// <summary>
        /// 한 Unit이 동시에 보유할 수 있는 활성 Focus 스택의 최대 수입니다.
        /// </summary>
        public int MaxStacks { get; }

        public FocusTraitDefinition(int damagePerStack, int maxStacks)
            : base(TraitKey.Focus)
        {
            if (damagePerStack <= 0)
                throw new ArgumentOutOfRangeException(nameof(damagePerStack));
            if (maxStacks <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStacks));

            DamagePerStack = damagePerStack;
            MaxStacks = maxStacks;
        }
    }
}
