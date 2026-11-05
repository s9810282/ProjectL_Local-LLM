using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class VanguardTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Vanguard;
        public int AttackBonus { get; }
        public int DurationControllerTurnEnds { get; }

        public VanguardTraitData(int attackBonus, int durationControllerTurnEnds)
        {
            AttackBonus = attackBonus;
            DurationControllerTurnEnds = durationControllerTurnEnds;
        }

        public override void Validate()
        {
            try
            {
                _ = new VanguardTraitSpec(AttackBonus, DurationControllerTurnEnds);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Vanguard.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
