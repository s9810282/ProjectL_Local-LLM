using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class DustScreenTraitDefinition : TraitDefinition
    {
        public int AttackRangeReduction { get; }
        public int AttackDamageReductionPercent { get; }
        public int OpposingTurnEndsToExpire { get; }

        public DustScreenTraitDefinition(int attackRangeReduction, int attackDamageReductionPercent, int opposingTurnEndsToExpire) : base(TraitKey.DustScreen)
        {
            var spec = new DustScreenTraitSpec(attackRangeReduction, attackDamageReductionPercent, opposingTurnEndsToExpire);
            AttackRangeReduction = spec.AttackRangeReduction;
            AttackDamageReductionPercent = spec.AttackDamageReductionPercent;
            OpposingTurnEndsToExpire = spec.OpposingTurnEndsToExpire;
        }
    }
}
