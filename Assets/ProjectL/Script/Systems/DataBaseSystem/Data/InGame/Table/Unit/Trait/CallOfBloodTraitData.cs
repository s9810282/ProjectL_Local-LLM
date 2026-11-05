using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class CallOfBloodTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.CallOfBlood;
        public int EnemyMaxHealthHealPercent { get; }

        public CallOfBloodTraitData(int enemyMaxHealthHealPercent)
        {
            EnemyMaxHealthHealPercent = enemyMaxHealthHealPercent;
        }

        public override void Validate()
        {
            try
            {
                _ = new CallOfBloodTraitSpec(EnemyMaxHealthHealPercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"CallOfBlood.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
