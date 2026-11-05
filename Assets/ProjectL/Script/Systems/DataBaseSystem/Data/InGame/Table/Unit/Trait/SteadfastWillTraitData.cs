using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class SteadfastWillTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.SteadfastWill;

        public SteadfastWillTraitData()
        {
        }

        public override void Validate()
        {
            try
            {
                _ = new SteadfastWillTraitSpec();
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"SteadfastWill.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
