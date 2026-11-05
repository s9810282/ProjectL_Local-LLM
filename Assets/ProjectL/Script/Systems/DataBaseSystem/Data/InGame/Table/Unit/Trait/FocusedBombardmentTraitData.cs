using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class FocusedBombardmentTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.FocusedBombardment;
        public int RequiredEnemyCount { get; }
        public int ExtraDamagePercent { get; }

        public FocusedBombardmentTraitData(int requiredEnemyCount, int extraDamagePercent)
        {
            RequiredEnemyCount = requiredEnemyCount;
            ExtraDamagePercent = extraDamagePercent;
        }

        public override void Validate()
        {
            try
            {
                _ = new FocusedBombardmentTraitSpec(RequiredEnemyCount, ExtraDamagePercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"FocusedBombardment.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
