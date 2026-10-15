using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{

    /// <summary>
    /// One action's immutable, action-start-based health/growth/death projection.
    /// Health transitions for the same Entity are composed sequentially before commit.
    /// </summary>
    internal sealed class ArtifactActionProjection
    {
        // The migrated route returns values only; legacy instance methods below remain until Task 7.
        internal static SoulActionChanges ProjectCollection(SoulCollectorInput input,
            CharacterCombatFacts final, int count, SoulFieldChange field, IReadOnlyList<long> consumed)
        {
            if (consumed.Count == 0)
                return new SoulActionChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                    Array.Empty<SoulCountChange>()), field, consumed);
            MainCharacterPersistentChange main = null;
            if (input.Main != null)
            {
                MainCharacterPersistentState source = input.Main;
                main = new MainCharacterPersistentChange(source, new MainCharacterPersistentState(
                    final.Health.CurrentHealth, source.PersistentMaxHealth, source.PersistentAttack,
                    source.MaxActionCost, source.MoveRange, source.MoveCost));
            }
            return new SoulActionChanges(new CharacterChanges(
                new[] { new CharacterStateChange(input.Character, final) },
                new[] { new SoulCountChange(input.Owned.Id, input.SoulCount, count) }, main), field, consumed);
        }
    }
}
