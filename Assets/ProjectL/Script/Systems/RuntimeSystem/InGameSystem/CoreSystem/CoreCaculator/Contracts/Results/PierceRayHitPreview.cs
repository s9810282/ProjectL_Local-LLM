using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct PierceRayHitPreview
    {
        public int RayIndex { get; }
        public int EncounterIndex { get; }
        public int DamageBeforeMatchup { get; }
        public int MatchupNumerator { get; }
        public int MatchupDenominator { get; }
        public int DamageAfterMatchup { get; }
        public int DamageAfterPhysicalDefense { get; }
        public int IncomingReduction { get; }
        public int FinalDamage { get; }

        public PierceRayHitPreview(int rayIndex, int encounterIndex, int damageBeforeMatchup,
            int matchupNumerator, int matchupDenominator, int damageAfterMatchup,
            int damageAfterPhysicalDefense, int incomingReduction, int finalDamage)
        {
            if (rayIndex < 0 || rayIndex > 2) throw new ArgumentOutOfRangeException(nameof(rayIndex));
            if (encounterIndex < 0) throw new ArgumentOutOfRangeException(nameof(encounterIndex));
            if (damageBeforeMatchup < 0) throw new ArgumentOutOfRangeException(nameof(damageBeforeMatchup));
            if (matchupNumerator < 0) throw new ArgumentOutOfRangeException(nameof(matchupNumerator));
            if (matchupDenominator <= 0) throw new ArgumentOutOfRangeException(nameof(matchupDenominator));
            if (damageAfterMatchup < 0) throw new ArgumentOutOfRangeException(nameof(damageAfterMatchup));
            if (damageAfterPhysicalDefense < 0) throw new ArgumentOutOfRangeException(nameof(damageAfterPhysicalDefense));
            if (incomingReduction < 0) throw new ArgumentOutOfRangeException(nameof(incomingReduction));
            if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));
            RayIndex = rayIndex;
            EncounterIndex = encounterIndex;
            DamageBeforeMatchup = damageBeforeMatchup;
            MatchupNumerator = matchupNumerator;
            MatchupDenominator = matchupDenominator;
            DamageAfterMatchup = damageAfterMatchup;
            DamageAfterPhysicalDefense = damageAfterPhysicalDefense;
            IncomingReduction = incomingReduction;
            FinalDamage = finalDamage;
        }
    }
}
