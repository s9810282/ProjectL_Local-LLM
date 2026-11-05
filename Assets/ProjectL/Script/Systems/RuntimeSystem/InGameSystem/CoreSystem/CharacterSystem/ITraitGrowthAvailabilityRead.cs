using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    public interface ITraitGrowthAvailabilityRead
    {
        TraitGrowthProgressSnapshot ReadTraitGrowthProgress();
    }
}
