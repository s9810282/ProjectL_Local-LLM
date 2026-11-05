using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class MoraleBoostTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.MoraleBoost;
        public int DefeatedEnemyBaseAttackPercent { get; }
        public int DurationControllerTurnEnds { get; }

        public MoraleBoostTraitData(int defeatedEnemyBaseAttackPercent, int durationControllerTurnEnds)
        {
            DefeatedEnemyBaseAttackPercent = defeatedEnemyBaseAttackPercent;
            DurationControllerTurnEnds = durationControllerTurnEnds;
        }

        public override void Validate()
        {
            try
            {
                _ = new MoraleBoostTraitSpec(DefeatedEnemyBaseAttackPercent, DurationControllerTurnEnds);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"MoraleBoost.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
