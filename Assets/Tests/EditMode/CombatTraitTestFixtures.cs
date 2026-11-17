using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    internal static class CombatTraitTestFixtures
    {
        internal static CombatCommandInput FanAttackInput(int nearHealth = 1, int farHealth = 100)
        {
            var method = new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], appointRange: 2,
                traits: new TraitStatusSnapshot[] { new DistanceDamageScaleTraitStatusSnapshot(new[] { 150, 50 }) });
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main,
                new TraitSpec[] { new DistanceDamageScaleTraitSpec(new[] { 150, 50 }) }), method);
            var characters = new[] { actor, input.Characters[1],
                CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2,3), Owner.Enemy), hp: nearHealth),
                CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3,3), Owner.Enemy), hp: farHealth),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(6,6), Owner.Enemy) };
            return CombatCommandTestData.With(input, characters, profiles: profiles);
        }

        internal static CombatCommandTransition Run(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        internal static int Health(CombatCommandTransition result, int id) => result.Characters.Characters
            .Single(c => c.Final.EntityId.Value == id).Final.Health.CurrentHealth;

        internal static CombatCommandInput WithRewards(CombatCommandInput input)
        {
            var enemyKeys = input.Characters.Where(c => c.Owner == Owner.Enemy).Select(c => c.CharacterKey).ToArray();
            var profiles = input.Profiles.Select(p =>
            {
                if (!enemyKeys.Contains(p.Definition.Key))
                    return p;
                var s = p.Definition;
                return new CombatAttackProfile(new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth,
                    s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense,
                    s.Priority, s.Traits.ToArray(), s.OwnerEligibility, s.Occlusion, s.Behavior, killReward:
                    new UnitKillRewardSpec(7, 10)), p.Method, p.OwnedId);
            }).ToArray();
            var characters = input.Characters.Select(c => c.Owner != Owner.Enemy ? c : new CharacterCombatFacts(c.EntityId, null, c.CharacterKey, c.Owner, null,
                c.Area, c.Health, c.Stats, c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.ActionUse,
                experience: CharacterExperienceFacts.ForEnemy(1))).ToArray();
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State, characters, profiles, input.Main, input.Owned,
                input.SoulCounts, input.Souls, input.Artifacts, battle: new CombatBattleFacts(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0,
                    progression: new LevelProgressionSpec(150, 100, 3, 1, 1)), goldBalance: 0, fameBalance: 0,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence,
                battleInstanceId: input.BattleInstanceId);
        }

        internal static CombatCommandInput KillChainInput(bool building = true, bool boss = false, bool kill = true, bool baseTrait = true)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method);
            var specs = new System.Collections.Generic.List<TraitSpec>();
            var statuses = new System.Collections.Generic.List<TraitStatusSnapshot>();
            if (baseTrait)
            {
                specs.Add(new KillChainAttackTraitSpec());
                statuses.Add(new KillChainAttackTraitStatusSnapshot());
            }
            var actor = CombatCommandTestData.With(input.Characters[0], traits: statuses.ToArray());
            var characters = input.Characters.Select(c => c.EntityId.Value == 1 ? actor : c.EntityId.Value == 2
                ? CombatCommandTestData.With(c, hp: kill ? 1 : 100) : c.EntityId.Value == 3 && boss
                    ? CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Stage1Boss, c.Anchor.Value, Owner.Enemy), category: CategorySnapshot.CreateStateless(CategoryKey.Boss)) : c).ToArray();
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, specs.ToArray()), method);
            if (boss)
                profiles = profiles.Append(new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Stage1Boss, 1, CategoryKey.Boss), method)).ToArray();
            var s = input.State;
            var cell = new CellCoord(0, 7);
            var structures = building ? new[] { new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] {CellCoord.Zero}, 100, false, 0),
                new HealthStateSnapshot(100,100), cell, Rotation4.R0, new[] {cell}, Owner.Enemy) } : Array.Empty<StructureCombatFacts>();
            return CombatCommandTestData.With(input, characters, new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,structures,turn: s.Turn), profiles);
        }

        internal static TraitActivationSnapshot[] Activations(CombatCommandTransition result, TraitKey key) =>
            result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitKey == key).ToArray();

        internal static CharacterCombatFacts Stats(CharacterCombatFacts c, int? defense = null, int? attack = null)
        {
            var s = c.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod,s.MoveRange,s.AppointRange,                attack ?? s.AttackPower,defense ?? s.PhysicalDefense,s.Priority,s.TraitStatuses.ToArray(),s.Category);
            return new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                c.Area, c.Health, stats, c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.ActionUse);
        }

        internal static readonly IMovementTileRule[] DamageRules = EnvironmentDamageTests.DamageRules;

        internal static CombatCommandInput HazardMovement(int hp = 100, int defense = 11)
        {
            var input = CombatCommandTestData.Input(empty: true);
            var actor = CombatTraitTestFixtures.Stats(CombatCommandTestData.With(input.Characters[0], hp: hp), defense);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1] }, state:
                EnvironmentDamageTests.WithDamageTiles(input.State, (new CellCoord(2, 3), 7)));
            return CombatCommandComposition.Intent(input, CombatCommandKind.Move,
                path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) }));
        }

        internal static CombatCommandInput KnockbackInput(int distance = 1, int defense = 0, int health = 100)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var seed = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(2, 3)), empty: true);
            var actor = CombatCommandTestData.With(seed.Characters[0], traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(distance) });
            var victim = CombatTraitTestFixtures.Stats(CombatCommandTestData.With(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy), hp: health), defense);
            var profiles = seed.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main,
                new TraitSpec[] { new KnockbackTraitSpec(distance) }), method);
            return CombatCommandTestData.With(seed, new[] { actor, seed.Characters[1], victim }, profiles: profiles);
        }
    }
}
