using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>
    /// Unit 종류에 설정되며 전투 중 변하지 않는 Trait 규칙 값을 나타냅니다.
    /// </summary>
    public abstract class TraitDefinition
    {
        /// <summary>
        /// 이 Definition이 어느 Trait의 설정인지 식별하는 안정적인 Key입니다.
        /// </summary>
        public TraitKey TraitKey { get; }

        protected TraitDefinition(TraitKey traitKey)
        {
            if (traitKey == TraitKey.None || Enum.IsDefined(typeof(TraitKey), traitKey) == false)
                throw new ArgumentOutOfRangeException(nameof(traitKey));

            TraitKey = traitKey;
        }
    }
}
