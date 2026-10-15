using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States
{
    /// <summary>
    /// Stage 안에서 개별 Unit마다 따로 소유하는 Category 규칙의 현재 상태입니다.
    /// UnitTraitStateSet과 물리적으로 분리된 저장소에 보관합니다.
    /// </summary>
    public abstract class CategoryRuntimeState
    {
        public CategoryRuleKey Key { get; }

        protected CategoryRuntimeState(CategoryRuleKey key)
        {
            if (key == CategoryRuleKey.None ||
                Enum.IsDefined(typeof(CategoryRuleKey), key) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }

            Key = key;
        }

        /// <summary>
        /// Preview와 사전 검증에 사용할 독립적인 깊은 복사본을 만듭니다.
        /// </summary>
        public abstract CategoryRuntimeState Clone();

        /// <summary>
        /// 공통 CombatEffects에 전달할 불변 Snapshot을 만듭니다.
        /// </summary>
        public abstract IEffectStateSnapshot Capture();

        internal abstract void CopyFinalValuesFrom(CategoryRuntimeState source);
    }
}
