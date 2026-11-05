using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>접촉 후 생존 대상에게 적용할 디버프의 기대값과 최종값입니다.</summary>
    public sealed class MovementDebuffChange
    {
        public EntityId TargetId
        {
            get;
        }
        public CharacterDebuffState Expected
        {
            get;
        }
        public CharacterDebuffState Final
        {
            get;
        }
        public MovementDebuffChange(EntityId targetId, CharacterDebuffState expected, CharacterDebuffState final)
        {
            if (targetId.IsEmpty)
                throw new ArgumentException("대상 ID가 필요합니다.", nameof(targetId));
            TargetId = targetId;
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
        }
    }
}
