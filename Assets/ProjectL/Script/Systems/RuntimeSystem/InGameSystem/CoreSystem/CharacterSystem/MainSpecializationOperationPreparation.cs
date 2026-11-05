using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal sealed class MainSpecializationOperationPreparation
    {
        internal CharacterRosterService Owner { get; }
        internal MainSpecializationDecision Decision { get; }
        internal MainSpecializationSource Source { get; }
        internal MainSpecializationDefinitionSnapshot Definitions { get; }
        internal TraitGrowthProgressSnapshot Growth { get; }
        internal bool Installed { get; private set; }

        internal MainSpecializationOperationPreparation(CharacterRosterService owner, MainSpecializationDecision decision,
            MainSpecializationSource source = null, MainSpecializationDefinitionSnapshot definitions = null, TraitGrowthProgressSnapshot growth = null)
        {
            Owner = owner;
            Decision = decision;
            Source = source;
            Definitions = definitions;
            Growth = growth;
        }
        internal void MarkInstalled() => Installed = true;
    }
}
