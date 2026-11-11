using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class RampageTraitDefinition : TraitDefinition
    {
        public int BaseAttackBonus { get; }
        public int RankAttackBonus { get; }
        public int MovementBonus { get; }

        public RampageTraitDefinition(int baseAttackBonus, int rankAttackBonus, int movementBonus) : base(TraitKey.Rampage)
        {
            var spec = new RampageTraitSpec(baseAttackBonus, rankAttackBonus, movementBonus);
            BaseAttackBonus = spec.BaseAttackBonus;
            RankAttackBonus = spec.RankAttackBonus;
            MovementBonus = spec.MovementBonus;
        }
    }
}
