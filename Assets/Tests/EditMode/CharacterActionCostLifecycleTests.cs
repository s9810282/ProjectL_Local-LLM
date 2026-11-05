using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    /// <summary>생성·입장·사본·행동이 공용 자금과 개별 사용 상태를 섞지 않는지 확인한다.</summary>
    public sealed class CharacterActionCostLifecycleTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [TestCase(EntityKey.Unit_Gravekeeper, Owner.Player)]
        [TestCase(EntityKey.Unit_Akimbo, Owner.Player)]
        [TestCase(EntityKey.Unit_Demon, Owner.Enemy)]
        public void Creation_DefersActionUntilOwnControllerStart(EntityKey key, Owner owner)
        {
            bool main = key == EntityKey.Unit_Gravekeeper;
            var spec = new UnitSpec(key, main ? UnitRole.MainCharacter : UnitRole.Regular, 1, 1,
                new[] { CellCoord.Zero }, 100, MoveMethod.LiftAndPlace, 4, CategoryKey.Main,
                1, 0, 100, Array.Empty<TraitSpec>(),
                ownerEligibility: main ? UnitOwnerEligibility.PlayerOnly : UnitOwnerEligibility.Both);
            var traits = TraitRegistry.CreateDefault();
            var definition = new UnitCatalog(new[] { spec }, traits).GetRequired(key);
            var owned = owner == Owner.Player ? new OwnedCharacterRecord(new CharacterInstanceId(1), key,
                main ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub) : null;
            var persistent = main ? new MainCharacterPersistentState(100, 100, 1) : null;
            var created = owner == Owner.Player
                ? BattleCharacterCalculator.CreateOwned(new EntityId(1), definition, owned, persistent, 0, null,
                    CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, traits)
                : BattleCharacterCalculator.CreateEnemy(new EntityId(1), definition, 1,
                    CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, traits);
            Assert.That(created.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That(created.Stats.MoveRange, Is.EqualTo(3));
        }

        [Test]
        public void BasicAttack_ConsumesActionAndPreservesInput()
        {
            var input = CombatCommandTestData.Input();
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [TestCase(CombatCommandKind.Attack)]
        [TestCase(CombatCommandKind.Move)]
        public void DisabledBehavior_RejectsWithoutCharacterChanges(CombatCommandKind kind)
        {
            var input = CombatCommandTestData.Input();
            var profile = input.Profiles.Single(p => p.Definition.Key == input.Characters[0].CharacterKey);
            var d = profile.Definition;
            var definition = new UnitSpec(d.Key, d.Role, d.FootprintWidth, d.FootprintHeight,
                d.FootprintLocalCells.ToArray(), d.MaxHealth, d.MoveMethod, d.AttackRange, d.CategoryKey,
                d.AttackPower, d.PhysicalDefense, d.Priority, d.Traits.ToArray(), d.OwnerEligibility,
                d.Occlusion, new UnitBehaviorSpec(false, false, true), killReward: d.KillReward);
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => ReferenceEquals(p, profile)
                ? new CombatAttackProfile(definition, p.Method) : p).ToArray());
            if (kind == CombatCommandKind.Move) input = MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(kind == CombatCommandKind.Move
                ? nameof(MovementFailureReason.VoluntaryMoveDisabled) : nameof(AttackFailureReason.BasicAttackDisabled)));
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [TestCase(CombatCommandKind.Attack)]
        [TestCase(CombatCommandKind.Move)]
        public void HandCharacterCannotAct(CombatCommandKind kind)
        {
            var input = CombatCommandTestData.Input();
            var source = input.Characters[0];
            var hand = source.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == hand.EntityId ? hand : c).ToArray());
            if (kind == CombatCommandKind.Move) input = MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(nameof(AttackFailureReason.NotInCombat)));
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [TestCase(UnitActionUse.Moved)]
        [TestCase(UnitActionUse.Attacked)]
        [TestCase(UnitActionUse.Deferred)]
        public void StatAndPlacementCopiesPreserveActionUse(UnitActionUse actionUse)
        {
            var actor = CombatCommandTestData.Input().Characters[0].With(actionUse: actionUse);
            var stats = actor.Stats.With(attackPower: actor.Stats.AttackPower + 1);
            var copied = actor.With(stats: stats).WithPlacement(actor.Area, actor.ReserveSlotIndex,
                new CellCoord(2, 3), actor.Rotation, new[] { new CellCoord(2, 3) });
            Assert.That(copied.ActionUse, Is.EqualTo(actionUse));
            Assert.That(actor.ActionUse, Is.EqualTo(actionUse));
        }
    }
}
