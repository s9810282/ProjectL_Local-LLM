using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class DustScreenTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.DustScreen;
        public int AttackRangeReduction { get; }
        public int AttackDamageReductionPercent { get; }
        public int OpposingTurnEndsToExpire { get; }

        public DustScreenTraitData(int attackRangeReduction, int attackDamageReductionPercent, int opposingTurnEndsToExpire)
        {
            AttackRangeReduction = attackRangeReduction;
            AttackDamageReductionPercent = attackDamageReductionPercent;
            OpposingTurnEndsToExpire = opposingTurnEndsToExpire;
        }

        public override void Validate()
        {
            try
            {
                _ = new DustScreenTraitSpec(AttackRangeReduction, AttackDamageReductionPercent, OpposingTurnEndsToExpire);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"DustScreen.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
