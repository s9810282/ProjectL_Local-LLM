using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class CharacterRosterRules
    {
        public const int StartingSubCount = 3;

        public static CharacterAcquisitionDecision PlanInitialRoster(CharacterDefinitionFacts main,
            IReadOnlyList<CharacterDefinitionFacts> startingSubs, bool rosterIsEmpty)
        {
            if (startingSubs == null)
                throw new ArgumentNullException(nameof(startingSubs));
            if (rosterIsEmpty == false || startingSubs.Count != StartingSubCount)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidRequest);
            if (IsEligible(main, CharacterOwnershipRole.Main) == false)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
            if (main.InitialMain == null ||
                main.InitialMain.CurrentHealth != main.InitialMain.PersistentMaxHealth)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidMainState);

            var requests = new RosterAcquireRequest[startingSubs.Count + 1];
            requests[0] = new RosterAcquireRequest(main.Key, CharacterOwnershipRole.Main);
            for (int i = 0; i < startingSubs.Count; i++)
            {
                if (IsEligible(startingSubs[i], CharacterOwnershipRole.Sub) == false)
                    return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
                requests[i + 1] = new RosterAcquireRequest(startingSubs[i].Key, CharacterOwnershipRole.Sub);
            }

            return CharacterAcquisitionDecision.Success(requests, main.InitialMain);
        }

        public static CharacterAcquisitionDecision PlanAcquireSub(CharacterDefinitionFacts definition)
        {
            if (IsEligible(definition, CharacterOwnershipRole.Sub) == false)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);

            return CharacterAcquisitionDecision.Success(new[]
            {
                new RosterAcquireRequest(definition.Key, CharacterOwnershipRole.Sub)
            });
        }

        private static bool IsEligible(CharacterDefinitionFacts definition, CharacterOwnershipRole role)
        {
            return definition != null &&
                   (definition.OwnerEligibility == UnitOwnerEligibility.PlayerOnly ||
                    definition.OwnerEligibility == UnitOwnerEligibility.Both) &&
                   definition.Role == (role == CharacterOwnershipRole.Main
                       ? UnitRole.MainCharacter : UnitRole.Regular);
        }
    }
}
