using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class BattleOutcomeCalculator
    {
        public static StageOutcomeSnapshot Resolve(IReadOnlyList<CharacterCombatFacts> characters,
            IReadOnlyList<StructureCombatFacts> structures)
        {
            if (characters == null || structures == null) throw new ArgumentNullException();
            CharacterCombatFacts main = null;
            foreach (var character in characters)
                if (character.OwnershipRole == CharacterOwnershipRole.Main)
                {
                    if (main != null) throw new InvalidOperationException("메인은 유일해야 합니다.");
                    main = character;
                }
            if (main == null) throw new InvalidOperationException("메인 사실이 필요합니다.");
            bool headquartersDead = false;
            foreach (var structure in structures)
                if (structure.Owner == Owner.Enemy && structure.Definition.Key == EntityKey.Structure_Headquarters && structure.Health.IsDead)
                    headquartersDead = true;
            return Resolve(main.Health.IsDead, headquartersDead);
        }

        internal static StageOutcomeSnapshot Resolve(bool mainDead, bool enemyHeadquartersDead) => mainDead
            ? new(StageOutcome.Defeat, StageOutcomeReason.PlayerMainCharacterDied)
            : enemyHeadquartersDead
                ? new(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed)
                : new(StageOutcome.InProgress, StageOutcomeReason.None);
    }
}
