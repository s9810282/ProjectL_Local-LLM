using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct ThornFieldApplicationEffectContext
    {
        public CellCoord SelectedCenter { get; }

        public ThornFieldApplicationEffectContext(CellCoord selectedCenter)
        {
            SelectedCenter = selectedCenter;
        }
    }
}
