using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class FocusTraitSpec : TraitSpec
    {
        public int DamagePerStack { get; } // 조준 효과 중첩 당 추가되는 피해
        public int MaxStacks { get; } // 최대 스택
        public FocusTraitSpec(int damagePerStack, int maxStacks)
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