using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class CleanSweepTraitDefinition : TraitDefinition
    {
        public int AttackBonusPerAllyAttack { get; }
        public int DurationControllerTurnEnds { get; }

        public CleanSweepTraitDefinition(int attackBonusPerAllyAttack, int durationControllerTurnEnds) : base(TraitKey.CleanSweep)
        {
            var spec = new CleanSweepTraitSpec(attackBonusPerAllyAttack, durationControllerTurnEnds);
            AttackBonusPerAllyAttack = spec.AttackBonusPerAllyAttack;
            DurationControllerTurnEnds = spec.DurationControllerTurnEnds;
        }
    }
}
