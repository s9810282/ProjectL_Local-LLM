using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    internal static class PullTestData
    {
        internal static readonly EntityId Target = new(2);
        internal static readonly CellCoord Origin = new(7, 7);
        internal static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        internal static CombatCommandInput Input(int range = 1, int extra = 1, int distance = 2,
            CellCoord? ray = null, Owner owner = Owner.Enemy, bool trait = true,
            EntityKey key = EntityKey.Unit_Akimbo, TargetingMethodSpec method = null)
        {
            method ??= new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var direction = ray ?? CellCoord.Right;
            var targetCell = new CellCoord(Origin.X + direction.X * distance, Origin.Y + direction.Y * distance);
            var source = CombatCommandTestData.Input(method, AttackAim.ForCell(targetCell), empty: true);
            var statuses = trait ? new[] { Status(extra) } : Array.Empty<TraitStatusSnapshot>();
            var original = CombatCommandTestData.With(source.Characters[0], position: Origin, appointRange: range, traits: statuses);
            var actor = new CharacterCombatFacts(original.EntityId, original.OwnedId, key, original.Owner,
                original.OwnershipRole, original.Area, original.Health, original.Stats, original.ReserveSlotIndex,
                original.Anchor, original.Rotation, original.OccupiedCells, original.ActionUse);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, targetCell, owner);
            var main = CombatCommandTestData.With(source.Characters[1], position: CellCoord.Zero);
            var characters = new[] { actor, main, target };
            var profiles = source.Profiles.Where(p => p.Definition.Key != EntityKey.Unit_Akimbo).Append(
                new CombatAttackProfile(UnitSpecTestFactory.Create(key, 1, 1, new[] { CellCoord.Zero }, 100,
                    MoveMethod.LiftAndPlace, range, 20, 100, trait ? new[] { Spec(extra) } : Array.Empty<TraitSpec>(),
                    categoryKey: CategoryKey.Main), method)).ToArray();
            var state = new CombatCommandState(16,16,source.State.Phase,                source.State.Structures,source.State.ThornFields,source.State.Turn);
            var input = CombatCommandTestData.With(source, characters, state, profiles);
            input = input.WithPersistentFacts(characters.Where(c => c.OwnedId.HasValue)
                .Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                input.ItemInventory, input.GoldBalance, input.FameBalance);
            return input.WithWorld(input.State, input.Characters, input.Main,
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0),
                input.Souls, input.EnvironmentBindings);
        }

        internal static TraitSpec Spec(int extra)
            => new PullTraitSpec(extra);

        private static TraitStatusSnapshot Status(int extra)
            => new PullTraitStatusSnapshot(extra);

        internal static CombatCommandTransition Run(CombatCommandInput input, IMovementTileRule[] rules = null)
        {
            var result = CombatCommandCalculator.Calculate(input, rules ?? NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        internal static CharacterCombatFacts Final(CombatCommandTransition result) =>
            (result.Accepted ? CombatCommandComposition.Project(result) : result.Input).Characters.Single(c => c.EntityId == Target);

        internal static CombatCommandInput TargetCategory(CombatCommandInput input, CategoryKey category, bool immune = false)
        {
            return CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == Target ? CombatCommandTestData.With(c,
                    category: CategorySnapshot.CreateStateless(category)) : c).ToArray(),
                profiles: input.Profiles.Select(p =>
                {
                    if (p.Definition.Key != EntityKey.Unit_Demon) return p;
                    var s = p.Definition;
                    return new CombatAttackProfile(new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight,
                        s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.AttackRange, category,
                        s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(), s.OwnerEligibility,
                        s.Occlusion, new UnitBehaviorSpec(true, true, immune), killReward: s.KillReward), p.Method);
                }).ToArray());
        }
    }
}
