using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class NexusReinforcementLifecycleTests
    {
        [Test]
        public void EnemyStart_SpawnsOnceBeforeAi()
        {
            var basis = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(), living: 4,
                phase: CombatControllerPhase.StartingEnemy);
            var started = CombatTurnCalculator.BeginControllerTurn(basis, Array.Empty<IMovementTileRule>());
            Assert.That(started.Events[0].Kind, Is.EqualTo(CombatEventKind.ControllerTurnStarted));
            Assert.That(started.Events.OfType<CombatReinforcementEvent>().Count(), Is.EqualTo(1));
            Assert.That(started.Characters.Created.Single().ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            var next = CombatCommandComposition.Project(started);
            Assert.That(next.Characters.Where(c => c.Owner == Owner.Enemy && c.EntityId.Value < 100)
                .Select(c => c.ActionUse), Is.All.EqualTo(UnitActionUse.Available));
            Assert.That(CombatTurnCalculator.BeginControllerTurn(next, Array.Empty<IMovementTileRule>()).Accepted, Is.False);
            var player = EnemyControllerPhaseTests.Input(next, CombatControllerPhase.StartingPlayer);
            Assert.That(CombatTurnCalculator.BeginControllerTurn(player, Array.Empty<IMovementTileRule>()).Characters.Created, Is.Empty);
        }

        [Test]
        public void NewActor_CannotMoveOrAttackUntilNextEnemyStart()
        {
            var basis = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(1, 0),
                phase: CombatControllerPhase.StartingEnemy);
            var started = CombatTurnCalculator.BeginControllerTurn(basis, Array.Empty<IMovementTileRule>());
            var created = started.Characters.Created.Single();
            Assert.That(UnitActionUseRules.CanStartMove(created.ActionUse), Is.False);
            Assert.That(UnitActionUseRules.CanStartAttack(created.ActionUse), Is.False);
            var next = CombatCommandComposition.Project(started);
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.AnkleBreaker, "main");
            var statuses = new StatusEffectSet(new[] { new ActionRestrictionStatus(StatusEffectKey.Bind, origin, 2) });
            var restricted = created.WithStatusEffects(statuses);
            var updated = next.Characters.Select(c => c.EntityId == created.EntityId ? restricted : c).ToArray();
            var retry = EnemyControllerPhaseTests.Input(next, CombatControllerPhase.StartingEnemy, characters: updated);
            var restarted = CombatCommandComposition.Project(CombatTurnCalculator.BeginControllerTurn(retry, Array.Empty<IMovementTileRule>())).Characters
                .Single(c => c.EntityId == created.EntityId);
            Assert.That(restarted.ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(restarted.StatusEffects.Entries.Single().Key, Is.EqualTo(StatusEffectKey.Bind));
        }

    }
}
