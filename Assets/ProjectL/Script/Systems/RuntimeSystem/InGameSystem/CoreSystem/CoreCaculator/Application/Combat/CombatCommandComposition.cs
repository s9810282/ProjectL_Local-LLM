using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 자동 명령 내부의 계산 결과를 합친다. 중간 결과를 Buffer에 게시하지 않는다.</summary>
    internal static class CombatCommandComposition
    {
        internal static CombatCommandInput Intent(CombatCommandInput i, CombatCommandKind kind, AttackAim aim = default,
            CellCoord cell = default, MovementPath path = null, MovementPreviewContext movementContext = null) =>
            new(i.Number, kind, i.ActorId, aim, i.State, i.Characters, i.Profiles, i.Main, i.Owned,
                i.SoulCounts, i.Souls, i.Artifacts, cell, i.Rotation, i.Battle, i.ProcessingKind,
                path ?? i.Path, movementContext ?? i.MovementContext, i.BattleInstanceId, i.ArtifactItems, i.EnvironmentBindings, i.ObstacleContents);
        internal static CombatCommandInput Project(CombatCommandTransition t)
        {
            var i = t.Input;
            if (!t.Accepted) throw new InvalidOperationException("거절된 계산을 자동 명령의 중간 변경으로 사용할 수 없습니다.");
            var characters = i.Characters.ToDictionary(c => c.EntityId);
            foreach (var c in t.Characters.Characters) characters[c.Final.EntityId] = c.Final;
            var counts = i.SoulCounts.ToDictionary(p => p.Key, p => p.Value);
            foreach (var c in t.Characters.Souls) counts[c.Id] = c.FinalCount;
            return new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, t.FinalState, characters.Values.ToArray(), i.Profiles,
                t.Characters.Main?.Final ?? i.Main, i.Owned, counts, t.Field.Final, i.Artifacts, i.TargetCell, i.Rotation, i.Battle, i.ProcessingKind,
                i.Path, i.MovementContext, i.BattleInstanceId, i.ArtifactItems, t.EnvironmentBindings?.Final ?? i.EnvironmentBindings, i.ObstacleContents);
        }
        internal static CombatCommandTransition Merge(CombatCommandInput original, IReadOnlyList<CombatCommandTransition> steps)
        {
            var final = Project(steps.Last());
            var characters = final.Characters.ToDictionary(c => c.EntityId);
            var changes = new CharacterChanges(original.Characters.Where(c => !ReferenceEquals(c, characters[c.EntityId]))
                .Select(c => new CharacterStateChange(c, characters[c.EntityId])).ToArray(),
                final.SoulCounts.Where(p => p.Value != original.SoulCounts[p.Key]).Select(p => new SoulCountChange(p.Key, original.SoulCounts[p.Key], p.Value)).ToArray(),
                steps.Any(s => s.Characters.Main != null) ? new MainCharacterPersistentChange(original.Main, final.Main) : null);
            return new CombatCommandTransition(original, true, "", changes, new SoulFieldChange(original.Souls, final.Souls), final.State,
                steps.Last().Outcome, steps.Select(s => s.Attack).LastOrDefault(a => a.HasValue),
                steps.SelectMany(s => s.ConsumedSoulIds).Distinct().ToArray(), movement: steps.Select(s => s.Movement).LastOrDefault(m => m.HasValue),
                events: steps.SelectMany(s => s.Events).ToArray(), pathMovement: steps.Select(s => s.PathMovement).LastOrDefault(p => p != null),
                returnMovement: steps.Select(s => s.ReturnMovement).LastOrDefault(r => r != null),
                environmentBindings: original.EnvironmentBindings.SameValues(final.EnvironmentBindings) ? null :
                    new EnvironmentBindingsChange(original.EnvironmentBindings, final.EnvironmentBindings),
                blockedContacts: steps.SelectMany(s => s.BlockedContacts).ToArray());
        }
    }
}
