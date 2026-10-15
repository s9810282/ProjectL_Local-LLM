using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>
    /// Stage 안에서 개별 Unit마다 따로 소유하는 Trait의 현재 상태를 나타냅니다.
    /// </summary>
    public abstract class TraitRuntimeState
    {
        /// <summary>
        /// 이 Runtime State가 어느 Trait의 상태인지 식별하는 안정적인 Key입니다.
        /// </summary>
        public TraitKey TraitKey { get; }

        protected TraitRuntimeState(TraitKey traitKey)
        {
            if (traitKey == TraitKey.None ||
                Enum.IsDefined(typeof(TraitKey), traitKey) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(traitKey));
            }

            TraitKey = traitKey;
        }

        /// <summary>
        /// Preview와 상태 변경 사전 검증에 사용할 독립적인 깊은 복사본을 만듭니다.
        /// </summary>
        internal abstract TraitRuntimeState Clone();

        /// <summary>
        /// 규칙 계산이 끝난 같은 Trait 타입의 최종 상태값을 현재 객체에 복사합니다.
        /// </summary>
        internal abstract void CopyFinalValuesFrom(TraitRuntimeState source);
    }
}
