using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{
    internal readonly struct ArtifactCombatUnitFact
    {
        internal EntityId Id { get; }
        internal Owner Owner { get; }

        internal ArtifactCombatUnitFact(EntityId id, Owner owner)
        {
            if (id.IsEmpty) throw new ArgumentException(nameof(id));
            if (owner != Owner.Player && owner != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(owner));
            Id = id;
            Owner = owner;
        }
    }

    internal sealed class ArtifactCombatFacts
    {
        internal ReadOnlyMemory<ArtifactCombatUnitFact> Units { get; }
        internal int PlayerUnitCount { get; }
        internal int EnemyUnitCount { get; }

        private ArtifactCombatFacts(ArtifactCombatUnitFact[] units)
        {
            Units = units;
            foreach (ArtifactCombatUnitFact unit in units)
            {
                if (unit.Owner == Owner.Player) PlayerUnitCount++;
                else EnemyUnitCount++;
            }
        }

        internal static ArtifactCombatFacts Capture(IEnumerable<CharacterCombatFacts> characters)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            var units = new List<ArtifactCombatUnitFact>();
            var seen = new HashSet<EntityId>();
            foreach (CharacterCombatFacts character in characters)
            {
                if (character == null || !seen.Add(character.EntityId))
                    throw new ArgumentException("Null or duplicate character facts.", nameof(characters));
                if (character.Health.IsDead || character.Area != CharacterRuntimeArea.Field) continue;
                units.Add(new ArtifactCombatUnitFact(character.EntityId, character.Owner));
            }
            units.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));
            return new ArtifactCombatFacts(units.ToArray());
        }

        internal ArtifactCombatFacts Excluding(IEnumerable<EntityId> excludedIds)
        {
            if (excludedIds == null) throw new ArgumentNullException(nameof(excludedIds));
            var excluded = new HashSet<EntityId>(excludedIds);
            var remaining = new List<ArtifactCombatUnitFact>(Units.Length);
            foreach (ArtifactCombatUnitFact unit in Units.Span)
            {
                if (!excluded.Contains(unit.Id)) remaining.Add(unit);
            }
            return new ArtifactCombatFacts(remaining.ToArray());
        }
    }
}
