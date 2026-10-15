using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Framework
{
    internal static class CharacterBufferTestData
    {
        internal static MainCharacterPersistentState MainState(int health = 10, int maxHealth = 10,
            int attack = 5, int maxAction = 4, int moveRange = 3, int moveCost = 1)
        {
            return new MainCharacterPersistentState(
                health, maxHealth, attack, maxAction, moveRange, moveCost);
        }

        internal static CharacterCombatFacts MainFacts(CharacterInstanceId ownedId,
            MainCharacterPersistentState state, int soulCount = 0, int temporaryAttack = 0,
            int temporaryMaxHealth = 0, EntityId? entityId = null)
        {
            int attackBonus = soulCount + temporaryAttack;
            return Facts(
                entityId ?? new EntityId(1),
                ownedId,
                EntityKey.Unit_MainCharacter,
                Owner.Player,
                CharacterOwnershipRole.Main,
                state.CurrentHealth,
                state.PersistentMaxHealth + soulCount + temporaryMaxHealth,
                state.PersistentAttack,
                state.MaxActionCost,
                state.MoveRange,
                state.MoveCost,
                attackBonus,
                soulCount,
                temporaryAttack,
                temporaryMaxHealth);
        }

        internal static CharacterCombatFacts SubFacts(CharacterInstanceId ownedId,
            EntityId entityId, int soulCount = 0)
        {
            return Facts(entityId, ownedId, EntityKey.Unit_Akimbo, Owner.Player,
                CharacterOwnershipRole.Sub, 6 + soulCount, 6 + soulCount,
                3, 3, 3, 1, soulCount, soulCount, 0, 0);
        }

        internal static CharacterCombatFacts EnemyFacts(EntityId entityId, int currentHealth = 5)
        {
            return Facts(entityId, null, EntityKey.Unit_Demon, Owner.Enemy, null,
                currentHealth, 5, 2, 3, 2, 1, 0, 0, 0, 0);
        }

        internal static CharacterCombatFacts WithHealth(CharacterCombatFacts source,
            int currentHealth)
        {
            return WithSnapshot(source, new HealthStateSnapshot(currentHealth, source.Health.MaxHealth));
        }

        internal static CharacterCombatFacts WithSnapshot(CharacterCombatFacts source,
            HealthStateSnapshot? health = null, UnitStatsSnapshot? stats = null)
        {
            return new CharacterCombatFacts(
                source.EntityId,
                source.OwnedId,
                source.CharacterKey,
                source.Owner,
                source.OwnershipRole,
                source.Area,
                health ?? source.Health,
                stats ?? source.Stats,
                source.ReserveSlotIndex,
                source.Anchor,
                source.Rotation,
                source.OccupiedCells,
                source.MovementCheckpointAnchor,
                source.CommittedMoveDistance,
                source.PendingMoveActionCost,
                source.TemporaryAttackBonus,
                source.TemporaryMaxHealthBonus,
                source.PendingMoveDistance, source.AttackRefundEligibleMoveCost, source.PositionModifier);
        }

        internal static CharacterCombatFacts WithStatuses(CharacterCombatFacts source,
            CategorySnapshot category, params TraitStatusSnapshot[] traits)
        {
            UnitStatsSnapshot stats = source.Stats;
            return WithSnapshot(source, stats: new UnitStatsSnapshot(
                stats.MoveMethod, stats.MoveRange, stats.MoveCost,
                stats.AppointRange, stats.AttackPower,
                stats.PhysicalDefense, stats.MaxActionCost, stats.AttackCost,
                stats.RemainingMoveRange, stats.CurrentActionCost, stats.Priority, stats.Delay,
                traits, category, stats.ArtifactAttackBonus, stats.EffectiveAttackPower,
                stats.SoulAttackBonus, stats.SoulMaxHealthBonus, stats.PositionAttackBonus));
        }

        internal static CombatPreparation Combat(params CharacterInstanceId[] drawOrder)
        {
            return new CombatPreparation(8, 6, 4, 100, 73,
                CombatFlowPhase.PlayerInput, drawOrder, drawOrder.Length);
        }

        private static CharacterCombatFacts Facts(EntityId entityId,
            CharacterInstanceId? ownedId, EntityKey key, Owner owner,
            CharacterOwnershipRole? role, int currentHealth, int maxHealth,
            int attack, int maxAction, int moveRange, int moveCost,
            int appliedAttackBonus, int soulCount, int temporaryAttack,
            int temporaryMaxHealth)
        {
            var stats = new UnitStatsSnapshot(
                MoveMethod.LiftAndPlace,
                moveRange,
                moveCost,
                2,
                attack,
                0,
                maxAction,
                1,
                moveRange,
                0,
                1,
                1,
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base),
                appliedAttackBonus,
                attack + appliedAttackBonus,
                soulCount,
                soulCount);

            return new CharacterCombatFacts(
                entityId,
                ownedId,
                key,
                owner,
                role,
                CharacterRuntimeArea.Field,
                new HealthStateSnapshot(currentHealth, maxHealth),
                stats,
                null,
                new CellCoord(entityId.Value, 1),
                Rotation4.R0,
                new[] { new CellCoord(entityId.Value, 1) },
                new CellCoord(entityId.Value, 1),
                0,
                0,
                temporaryAttack,
                temporaryMaxHealth);
        }
    }
}
