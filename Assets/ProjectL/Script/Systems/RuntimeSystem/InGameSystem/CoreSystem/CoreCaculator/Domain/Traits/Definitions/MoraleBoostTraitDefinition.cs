using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class MoraleBoostTraitDefinition : TraitDefinition
    {
        public int DefeatedEnemyBaseAttackPercent { get; }
        public int DurationControllerTurnEnds { get; }

        public MoraleBoostTraitDefinition(int defeatedEnemyBaseAttackPercent, int durationControllerTurnEnds) : base(TraitKey.MoraleBoost)
        {
            var spec = new MoraleBoostTraitSpec(defeatedEnemyBaseAttackPercent, durationControllerTurnEnds);
            DefeatedEnemyBaseAttackPercent = spec.DefeatedEnemyBaseAttackPercent;
            DurationControllerTurnEnds = spec.DurationControllerTurnEnds;
        }
    }
}
