using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public abstract class TraitSpec
    {
        public TraitKey TraitKey { get; }

        protected TraitSpec(TraitKey traitKey)
        {
            if (traitKey == TraitKey.None || Enum.IsDefined(typeof(TraitKey), traitKey) == false)
                throw new ArgumentOutOfRangeException(nameof(traitKey));

            this.TraitKey = traitKey;
        }

        /// <summary>형식·키·모든 설정 값이 같은지 비교한다. 객체의 전역 동등성은 변경하지 않는다.</summary>
        public bool SameValues(TraitSpec other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null || GetType() != other.GetType() || TraitKey != other.TraitKey)
            {
                return false;
            }

            return SameValuesCore(other);
        }

        /// <summary>같은 형식·키인 특성의 설정 값을 비교한다. 각 특성은 자기 설정만 비교한다.</summary>
        /// <param name="other">null이 아니며 현재 객체와 정확한 형식·키가 같은 특성이다.</param>
        /// <remarks>파생 구현은 모든 설정 값을 비교하며 반사성·대칭성·추이성을 지킨다.</remarks>
        protected abstract bool SameValuesCore(TraitSpec other);
    }
}
