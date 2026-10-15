using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>Buffer 사실로 계산하며 지속 상태와 전투 투영을 한 변경 제안으로 묶는다.</summary>
    public static class CharacterSettlementCalculator
    {
        public static CharacterChanges SetHealth(CharacterCombatFacts character,
            MainCharacterPersistentState main, int currentHealth)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var health = new HealthStateSnapshot(currentHealth, character.Health.MaxHealth);
            MainCharacterPersistentChange persistent = null;
            if (character.OwnershipRole == CharacterOwnershipRole.Main)
            {
                ValidateMain(character, main);
                persistent = new MainCharacterPersistentChange(main, WithHealth(main, currentHealth));
            }
            return new CharacterChanges(new[] { new CharacterStateChange(character,
                Copy(character, health, character.Stats)) }, Array.Empty<SoulCountChange>(), persistent);
        }

        public static CharacterChanges SetPersistentMain(CharacterCombatFacts character,
            MainCharacterPersistentState expected, MainCharacterPersistentState final)
        {
            ValidateMain(character, expected);
            if (final == null) throw new ArgumentNullException(nameof(final));
            int maxHealth = checked(final.PersistentMaxHealth + character.Stats.SoulMaxHealthBonus +
                character.TemporaryMaxHealthBonus + character.PositionModifier.MaxHealthBonus);
            UnitStatsSnapshot s = character.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod, final.MoveRange, final.MoveCost,
                s.AppointRange, final.PersistentAttack, s.PhysicalDefense,
                final.MaxActionCost, s.AttackCost, Math.Min(s.RemainingMoveRange, final.MoveRange),
                Math.Min(s.CurrentActionCost, final.MaxActionCost), s.Priority, s.Delay,
                s.TraitStatuses.ToArray(), s.Category, s.ArtifactAttackBonus,
                checked(final.PersistentAttack + s.ArtifactAttackBonus + s.PositionAttackBonus),
                s.SoulAttackBonus, s.SoulMaxHealthBonus, s.PositionAttackBonus);
            return new CharacterChanges(new[] { new CharacterStateChange(character,
                Copy(character, new HealthStateSnapshot(final.CurrentHealth, maxHealth), stats)) },
                Array.Empty<SoulCountChange>(), new MainCharacterPersistentChange(expected, final));
        }

        /// <summary>이동·효과·사망 정산이 끝난 제안에 종료 주체의 행동 자원 복원을 합친다.</summary>
        public static SoulActionChanges EndTurn(CharacterCombatFacts actor, SoulActionChanges settled)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (settled == null) throw new ArgumentNullException(nameof(settled));
            var changes = new List<CharacterStateChange>(settled.Characters.Characters);
            int index = changes.FindIndex(c => c.Expected.EntityId == actor.EntityId);
            CharacterCombatFacts final = index >= 0 ? changes[index].Final : actor;
            if (!final.Health.IsDead && final.Area == CharacterRuntimeArea.Field)
            {
                if (final.PendingMoveActionCost != 0 || final.MovementCheckpointAnchor != final.Anchor.Value)
                    throw new InvalidOperationException("턴 종료 전에 이동 비용을 정산해야 합니다.");
                UnitStatsSnapshot s = final.Stats;
                var stats = new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost,
                    s.AppointRange, s.AttackPower, s.PhysicalDefense,
                    s.MaxActionCost, s.AttackCost, s.RemainingMoveRange, s.MaxActionCost,
                    s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category, s.ArtifactAttackBonus,
                    s.EffectiveAttackPower, s.SoulAttackBonus, s.SoulMaxHealthBonus, s.PositionAttackBonus);
                var change = new CharacterStateChange(index >= 0 ? changes[index].Expected : actor,
                    Copy(final, final.Health, stats));
                if (index >= 0) changes[index] = change;
                else changes.Add(change);
            }
            return new SoulActionChanges(new CharacterChanges(changes, settled.Characters.Souls,
                settled.Characters.Main), settled.Field, settled.ConsumedSoulIds);
        }

        internal static MainCharacterPersistentState WithHealth(MainCharacterPersistentState main, int hp) =>
            new(hp, main.PersistentMaxHealth, main.PersistentAttack, main.MaxActionCost, main.MoveRange, main.MoveCost);

        internal static void ValidateMain(CharacterCombatFacts character, MainCharacterPersistentState main)
        {
            if (character == null || main == null || character.OwnershipRole != CharacterOwnershipRole.Main ||
                character.Health.CurrentHealth != main.CurrentHealth)
                throw new ArgumentException("메인 사실과 지속 상태가 일치해야 합니다.");
        }

        internal static CharacterCombatFacts Copy(CharacterCombatFacts c, HealthStateSnapshot health,
            UnitStatsSnapshot stats) => new(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner,
                c.OwnershipRole, c.Area, health, stats, c.ReserveSlotIndex, c.Anchor, c.Rotation,
                c.OccupiedCells, c.MovementCheckpointAnchor, c.CommittedMoveDistance,
                c.PendingMoveActionCost, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                c.PendingMoveDistance, c.AttackRefundEligibleMoveCost, c.PositionModifier, c.HeldEnvironment);
    }
}
