using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class FocusedBombardmentTraitDefinition : TraitDefinition
    {
        public int RequiredEnemyCount { get; }
        public int ExtraDamagePercent { get; }

        public FocusedBombardmentTraitDefinition(int requiredEnemyCount, int extraDamagePercent) : base(TraitKey.FocusedBombardment)
        {
            var spec = new FocusedBombardmentTraitSpec(requiredEnemyCount, extraDamagePercent);
            RequiredEnemyCount = spec.RequiredEnemyCount;
            ExtraDamagePercent = spec.ExtraDamagePercent;
        }
    }
}
