using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class VanguardTraitDefinition : TraitDefinition
    {
        public int AttackBonus { get; }
        public int DurationControllerTurnEnds { get; }

        public VanguardTraitDefinition(int attackBonus, int durationControllerTurnEnds) : base(TraitKey.Vanguard)
        {
            var spec = new VanguardTraitSpec(attackBonus, durationControllerTurnEnds);
            AttackBonus = spec.AttackBonus;
            DurationControllerTurnEnds = spec.DurationControllerTurnEnds;
        }
    }
}
