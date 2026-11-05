using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class SacrificePublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RedirectHpAndOwnerCap_PublishTogetherAndPreviewIsPure(bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new SacrificeSource());
            var target = battle.Character(EntityKey.Unit_DynamiteHammer);
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(target, null, 1));
            target = battle.Character(target.CharacterKey);
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep().WithActor(battle.Character(EntityKey.Unit_Demon).EntityId)
                .WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(target.Anchor.Value), default, null, null);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            before.AssertUnchanged(battle.Buffers);
            if (stale) input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(9, 0)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            if (stale)
            {
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                before.AssertUnchanged(battle.Buffers);
                Assert.That(Used(battle.Actor), Is.Zero);
            }
            else
            {
                battle.Buffers.BattleWriteSink.Apply(transition);
                Assert.That(battle.Character(target.CharacterKey).Health.CurrentHealth, Is.EqualTo(1));
                Assert.That(battle.Actor.Health.CurrentHealth, Is.InRange(1, battle.Actor.Health.MaxHealth - 1));
                Assert.That(Used(battle.Actor), Is.EqualTo(1));
                var once = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                once.AssertUnchanged(battle.Buffers);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        public void PublicAttack_ReactionsAndRecipientFactsAreAtomicAndReadOnly(bool child, bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new SacrificeSource(child));
            var target = battle.Character(EntityKey.Unit_DynamiteHammer);
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(target, null, 1));
            target = battle.Character(target.CharacterKey);
            if (child) battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(battle.Character(EntityKey.Unit_Sniper), null, 1));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var enemy = battle.Character(EntityKey.Unit_Demon);
            var input = battle.Publication().EnemyStep().WithActor(enemy.EntityId).WithIntent(CombatCommandKind.Attack,
                child ? AttackAim.ForDirection(AttackDirection.Down) : AttackAim.ForCell(target.Anchor.Value), default, null, null);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            var projected = preview.Targets.ToArray().Single(t => t.TargetId == battle.Actor.EntityId);
            Assert.That(projected.Contacts.Single().OriginalTargetId, Is.EqualTo(target.EntityId));
            Assert.That(projected.TargetOccupiedCells.ToArray(), Is.EqualTo(battle.Actor.OccupiedCells));
            before.AssertUnchanged(battle.Buffers);
            if (stale) input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(9, 0)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var proposed = transition.Attack.Value;
            Assert.That(proposed.TraitActivations.ToArray().Count(t => t.Interception != null), Is.EqualTo(1));
            Assert.That(proposed.Hits.ToArray().Single(h => h.TargetId == battle.Actor.EntityId).TargetId, Is.EqualTo(battle.Actor.EntityId));
            if (stale)
            {
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                before.AssertUnchanged(battle.Buffers);
                return;
            }
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var published), Is.True);
            Assert.That(published.Accepted, Is.True, published.Reason);
            Assert.That(published.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved || e.Kind == CombatEventKind.Relocated), Is.False);
            Assert.That(battle.Actor.Anchor, Is.EqualTo(input.Characters.Single(c => c.EntityId == battle.Actor.EntityId).Anchor));
            Assert.That(battle.Character(target.CharacterKey).Anchor, Is.EqualTo(target.Anchor));
            Assert.That(battle.Character(target.CharacterKey).Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(published.ActionUseSettlement.ActorId, Is.EqualTo(transition.ActionUseSettlement.ActorId));
            Assert.That(published.ActionUseSettlement.BeforeActionUse, Is.EqualTo(transition.ActionUseSettlement.BeforeActionUse));
            Assert.That(published.ActionUseSettlement.FinalActionUse, Is.EqualTo(transition.ActionUseSettlement.FinalActionUse));

            var attack = published.Events.OfType<CombatAttackEvent>().Single();
            var reaction = attack.Attack.TraitActivations.ToArray().Single(t => t.Interception != null);
            Assert.That(reaction, Is.SameAs(published.Attack.Value.TraitActivations.ToArray().Single(t => t.Interception != null)));
            Assert.That(reaction.Interception.ProtectedId, Is.EqualTo(target.EntityId));
            Assert.That(reaction.Interception.ActualRecipientId, Is.EqualTo(battle.Actor.EntityId));
            Assert.That(reaction.ResultOrigin.CreditedCharacterId, Is.EqualTo(enemy.EntityId));
            Assert.That(reaction.ResultOrigin.PhysicalCell, Is.EqualTo(enemy.Anchor));
            int start = published.Events.ToList().IndexOf(attack);
            int consumed = published.Events.ToList().FindIndex(e => e is CombatTraitStateEvent t && t.Final.EntityId == battle.Actor.EntityId);
            Assert.That(start, Is.LessThan(consumed));
            Assert.That(published.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.Interception != null), Is.False);
            var children = published.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.ChildAttack != null).ToArray();
            Assert.That(children.Length, Is.EqualTo(child ? 1 : 0));
            if (child)
            {
                Assert.That(children[0].ChildAttack.Reactions.Count, Is.EqualTo(1));
                Assert.That(children[0].ChildAttack.Hits.Single().TargetHealthBefore, Is.EqualTo(proposed.Hits.ToArray().Single(h => h.TargetId == battle.Actor.EntityId).TargetHealthAfter));
                Assert.That(children[0].ChildAttack.Hits.Single().TargetHealthAfter, Is.EqualTo(battle.Actor.Health.CurrentHealth));
            }
            else Assert.That(proposed.Hits.ToArray().Single(h => h.TargetId == battle.Actor.EntityId).TargetHealthAfter, Is.EqualTo(battle.Actor.Health.CurrentHealth));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var reread), Is.True);
                Assert.That(reread, Is.SameAs(published));
                Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            }
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            once.AssertUnchanged(battle.Buffers);
        }

        private static int Used(CharacterCombatFacts source)
        {
            var status = source.Stats.TraitStatuses.ToArray().Single(t => t.Key == TraitKey.Sacrifice);
            return (int)status.GetType().GetProperty("ActivationsThisEnemyTurn").GetValue(status);
        }

        private sealed class SacrificeSource : IBattleInitializationSource
        {
            private readonly bool _child;
            internal SacrificeSource(bool child = false) { _child = child; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(_child ? 4 : 0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.Key == EntityKey.Unit_Demon ? 8 : s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Demon ? 4 : s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new SacrificeTraitSpec(2, _child ? 2 : 1) }
                        : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), _child && p.Definition.Key == EntityKey.Unit_Demon
                        ? new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0) : p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
