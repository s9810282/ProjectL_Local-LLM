using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    internal static class MovementTestData
    {
        internal static CombatCommandInput SafeRoute(int ap, int range, int moveCost, params CellCoord[] cells)
        {
            var path = new MovementPath(cells);
            var input = CombatCommandTestData.Input(empty: true);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var s = actor.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod, range, moveCost, s.AppointRange,
                s.AttackPower, s.PhysicalDefense, ap, s.AttackCost, range, ap, s.Priority, s.Delay,
                s.TraitStatuses.ToArray(), s.Category, s.ArtifactAttackBonus, s.EffectiveAttackPower,
                s.SoulAttackBonus, s.SoulMaxHealthBonus, s.PositionAttackBonus);
            var replacement = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey,
                actor.Owner, actor.OwnershipRole, actor.Area, actor.Health, stats, actor.ReserveSlotIndex,
                path.Origin, actor.Rotation, new[] { path.Origin }, path.Origin, 0, 0,
                actor.TemporaryAttackBonus, actor.TemporaryMaxHealthBonus,
                actor.PendingMoveDistance, actor.AttackRefundEligibleMoveCost, actor.PositionModifier);
            // 작성 정의의 양수 비용 계약을 유지하고, 0 비용은 런타임 Stats에만 둔다.
            var definition = UnitSpecTestFactory.Create(actor.CharacterKey, 1, 1, new[] { CellCoord.Zero },
                actor.Health.MaxHealth, s.MoveMethod, range, Math.Max(1, moveCost), s.AppointRange, s.AttackPower, ap, s.AttackCost, s.Priority, s.Delay,
                Array.Empty<TraitSpec>(), categoryKey: s.Category.Key);
            var profiles = input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                ? new CombatAttackProfile(definition, p.Method) : p).ToArray();
            return new CombatCommandInput(input.Number, CombatCommandKind.Move, input.ActorId, default,
                input.State, input.Characters.Select(c => c.EntityId == actor.EntityId ? replacement : c).ToArray(),
                profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                path.Endpoint, input.Rotation, input.Battle, input.ProcessingKind, path,
                environmentBindings: input.EnvironmentBindings);
        }

        internal static CharacterCombatFacts Actor(CombatCommandTransition transition) =>
            transition.Characters.Characters.FirstOrDefault(c => c.Final.EntityId == transition.Input.ActorId)?.Final
            ?? transition.Input.Characters.Single(c => c.EntityId == transition.Input.ActorId);

        internal static CombatCommandInput After(CombatCommandTransition transition) =>
            CombatCommandComposition.Project(transition);

        internal static CombatCommandInput Route(CombatCommandInput source, params CellCoord[] cells)
        {
            var path = new MovementPath(cells);
            return new CombatCommandInput(source.ProcessingKind == CombatProcessingKind.Advance ? 0 : checked(source.Number + 1),
                CombatCommandKind.Move, source.ActorId, default,
                source.State, source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts,
                source.Souls, source.Artifacts, path.Endpoint, source.Rotation, source.Battle, source.ProcessingKind, path,
                battleInstanceId: source.BattleInstanceId, artifactItems: source.ArtifactItems,
                environmentBindings: source.EnvironmentBindings);
        }

        internal static CombatCommandInput Return(CombatCommandInput source) =>
            new(source.ProcessingKind == CombatProcessingKind.Advance ? 0 : checked(source.Number + 1),
                CombatCommandKind.ReturnToCheckpoint, source.ActorId, default,
                source.State, source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts,
                source.Souls, source.Artifacts, rotation: source.Rotation, battle: source.Battle,
                processingKind: source.ProcessingKind, battleInstanceId: source.BattleInstanceId, artifactItems: source.ArtifactItems,
                environmentBindings: source.EnvironmentBindings);
    }
}
