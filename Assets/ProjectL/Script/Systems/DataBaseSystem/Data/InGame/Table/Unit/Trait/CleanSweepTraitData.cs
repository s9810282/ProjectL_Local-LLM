using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class CleanSweepTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.CleanSweep;
        public int AttackBonusPerAllyAttack { get; }
        public int DurationControllerTurnEnds { get; }

        public CleanSweepTraitData(int attackBonusPerAllyAttack, int durationControllerTurnEnds)
        {
            AttackBonusPerAllyAttack = attackBonusPerAllyAttack;
            DurationControllerTurnEnds = durationControllerTurnEnds;
        }

        public override void Validate()
        {
            try
            {
                _ = new CleanSweepTraitSpec(AttackBonusPerAllyAttack, DurationControllerTurnEnds);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"CleanSweep.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
