using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    internal interface ICharacterStatusLifecycleRule
    {
        CharacterDebuffState OnTurnEnded(CharacterDebuffState state);
    }
}
