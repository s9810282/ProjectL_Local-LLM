using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class CallOfBloodTraitDefinition : TraitDefinition
    {
        public int EnemyMaxHealthHealPercent { get; }

        public CallOfBloodTraitDefinition(int enemyMaxHealthHealPercent) : base(TraitKey.CallOfBlood)
        {
            var spec = new CallOfBloodTraitSpec(enemyMaxHealthHealPercent);
            EnemyMaxHealthHealPercent = spec.EnemyMaxHealthHealPercent;
        }
    }
}
