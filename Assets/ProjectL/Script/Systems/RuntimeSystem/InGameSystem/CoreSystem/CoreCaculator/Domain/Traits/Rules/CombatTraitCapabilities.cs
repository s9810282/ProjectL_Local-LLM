using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal interface IAttackCostModifier
    {
        AttackCostContribution EvaluateAttackCost(in AttackCostTraitContext context);
    }

    internal interface IAttackDamageModifier
    {
        AttackDamageContribution EvaluateAttackDamage(in AttackDamageTraitContext context);
    }

    internal interface IMoveSucceededTrait
    {
        TraitStateChange PlanMoveSucceeded(in MoveSucceededTraitContext context);
    }

    internal interface IAttackSucceededTrait
    {
        TraitStateChange PlanAttackSucceeded(in AttackSucceededTraitContext context);
    }

    internal interface ITurnStartedTrait
    {
        TraitStateChange PlanTurnStarted(in TurnStartedTraitContext context);
    }

    internal interface ITurnEndedTrait
    {
        TraitStateChange PlanTurnEnded(in TurnEndedTraitContext context);
    }
}