using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
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
    internal static class CombatCommandTestData
    {
        internal static CombatCommandInput Input(TargetingMethodSpec method = null, AttackAim? aim = null,
            bool empty = false, bool pending = false, bool friendly = false, CategoryKey category = CategoryKey.Base)
        {
            method ??= new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);
            var main = new MainCharacterPersistentState(100, 100, 1, 5, 4, 1);
            var characters = new List<CharacterCombatFacts>
            {
                With(Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 20, pending: pending),
                    category: category == CategoryKey.Rider ? CategorySnapshot.CreateRider(false)
                        : category == CategoryKey.Heavy ? CategorySnapshot.CreateHeavy(Rotation4.R0, false, 50)
                        : CategorySnapshot.CreateStateless(category)),
                Character(10, EntityKey.Unit_MainCharacter, new CellCoord(7, 7), Owner.Player, 1, main: true),
            };
            if (!empty)
            {
                characters.Add(Character(2, EntityKey.Unit_Demon, friendly ? new CellCoord(3, 3) : new CellCoord(4, 3), friendly ? Owner.Player : Owner.Enemy));
                characters.Add(Character(3, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy));
                characters.Add(Character(4, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy));
            }
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, new EntityId(1), false,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                new[] { CombatTimelineEntrySnapshot.ForUnit(new EntityId(1), 100, 0, true) });
            var profiles = new[]
            {
                new CombatAttackProfile(Spec(EntityKey.Unit_Akimbo, 20, category), method),
                new CombatAttackProfile(Spec(EntityKey.Unit_Demon, 1), new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)),
                new CombatAttackProfile(Spec(EntityKey.Unit_MainCharacter, 1), new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)),
            };
            return new CombatCommandInput(1, CombatCommandKind.Attack, new EntityId(1),
                aim ?? AttackAim.ForCell(new CellCoord(4, 3)), state, characters, profiles, main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value,
                    c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0),
                SoulFieldSnapshot.Empty, new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()));
        }

        internal static CharacterCombatFacts Character(int id, EntityKey key, CellCoord cell,
            Owner owner, int attack = 1, bool main = false, bool pending = false)
        {
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 4, 1, 4,
                attack, 0, 5, 2, 4, 5, 100, 10, Array.Empty<TraitStatusSnapshot>(), CategorySnapshot.CreateStateless(CategoryKey.Base));
            CharacterInstanceId? owned = owner == Owner.Player ? new CharacterInstanceId(main ? 1 : id + 1) : null;
            return new CharacterCombatFacts(new EntityId(id), owned,
                key, owner, owned.HasValue ? (main ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub) : null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(100, 100), stats, null, cell, Rotation4.R0, new[] { cell },
                pending ? new CellCoord(cell.X, cell.Y - 1) : cell, 0, pending ? 1 : 0);
        }

        internal static UnitSpec Spec(EntityKey key, int attack, CategoryKey category = CategoryKey.Base,
            params TraitSpec[] traits)
        {
            return UnitSpecTestFactory.Create(key, 1, 1, new[] { CellCoord.Zero }, 100, MoveMethod.LiftAndPlace, 4, 1,
                4,
                attack, 5, 2, 100, 10, traits, categoryKey: category);
        }

        internal static CombatCommandInput With(CombatCommandInput input,
            CharacterCombatFacts[] characters = null, CombatCommandState state = null,
            CombatAttackProfile[] profiles = null, SoulFieldSnapshot souls = null,
            ArtifactLoadoutSnapshot? artifacts = null, MainCharacterPersistentState main = null) => new CombatCommandInput(input.Number, input.Kind,
                input.ActorId, input.Aim, state ?? input.State, characters ?? input.Characters.ToArray(),
                profiles ?? input.Profiles.ToArray(), main ?? input.Main, input.Owned, input.SoulCounts,
                souls ?? input.Souls, artifacts ?? input.Artifacts, input.TargetCell, input.Rotation,
                input.Battle, input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems,
                input.EnvironmentBindings, input.ObstacleContents);

        internal static CharacterCombatFacts With(CharacterCombatFacts c, int? hp = null,
            TraitStatusSnapshot[] traits = null, CategorySnapshot? category = null, CellCoord? position = null,
            int? ap = null, int? appointRange = null)
        {
            var s = c.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost, appointRange ?? s.AppointRange,
                s.AttackPower, s.PhysicalDefense, s.MaxActionCost, s.AttackCost, s.RemainingMoveRange,
                ap ?? s.CurrentActionCost, s.Priority, s.Delay, traits ?? s.TraitStatuses.ToArray(), category ?? s.Category,
                s.ArtifactAttackBonus, s.EffectiveAttackPower, s.SoulAttackBonus, s.SoulMaxHealthBonus, s.PositionAttackBonus);
            var cell = position ?? c.Anchor.Value;
            return new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                c.Area, new HealthStateSnapshot(hp ?? c.Health.CurrentHealth, c.Health.MaxHealth), stats,
                c.ReserveSlotIndex, cell, c.Rotation, new[] { cell }, c.MovementCheckpointAnchor,
                c.CommittedMoveDistance, c.PendingMoveActionCost, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                c.PendingMoveDistance, c.AttackRefundEligibleMoveCost, c.PositionModifier);
        }
    }
}
