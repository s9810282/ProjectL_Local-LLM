using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class HunterTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Hunter;
        public int AttackPerTargetKill { get; }
        public int MaxRewardsPerStage { get; }

        public HunterTraitData(int attackPerTargetKill, int maxRewardsPerStage)
        {
            AttackPerTargetKill = attackPerTargetKill;
            MaxRewardsPerStage = maxRewardsPerStage;
        }

        public override void Validate()
        {
            try
            {
                _ = new HunterTraitSpec(AttackPerTargetKill, MaxRewardsPerStage);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Hunter.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
