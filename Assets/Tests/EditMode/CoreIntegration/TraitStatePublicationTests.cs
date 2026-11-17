using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitStatePublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void StalePublication_ChangesNothing(bool staleOwned)
        {
            using var battle = Battle();
            var transition = Award(battle, out var finalOwned);
            var input = transition.Input;
            var staleState = staleOwned ? input.State : WithTurns(input.State, 9);
            var staleInput = input.WithWorld(staleState, input.Characters, input.Main, input.SoulCounts,
                input.Souls, input.EnvironmentBindings);
            if (staleOwned)
                staleInput = staleInput.WithPersistentFacts(input.Owned.Select(o => o.Id == finalOwned.Id ? Owned(o, 3) : o).ToArray(),
                    input.ItemInventory, input.GoldBalance, input.FameBalance);
            var staleChanges = transition.Characters;
            if (staleOwned)
            {
                var expected = staleInput.Owned.Single(o => o.Id == finalOwned.Id);
                var projected = staleChanges.Characters.Select(c => c.Final.OwnedId == expected.Id
                    ? new CharacterStateChange(c.Expected, Project(c.Final, 4)) : c).ToArray();
                staleChanges = new CharacterChanges(projected, staleChanges.Souls, staleChanges.Main, staleChanges.Created,
                    new[] { new OwnedCharacterChange(expected, Owned(expected, 4)) });
            }
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var candidate = Replace(transition, input: staleInput, characters: staleChanges);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(candidate));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == finalOwned.Id).SameValues(finalOwned), Is.False);
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == finalOwned.Id).SameValues(finalOwned), Is.True);
            var beforeActor = transition.Input.Characters.Single(c => c.EntityId == transition.Input.ActorId);
            Assert.That(battle.Actor.Stats.AttackPower, Is.EqualTo(beforeActor.Stats.AttackPower));
            Assert.That(battle.Actor.Stats.EffectiveAttackPower, Is.EqualTo(beforeActor.Stats.EffectiveAttackPower + 5));
            Assert.That(Get(Get(battle.Buffers.CombatStateReadSink.Current.CommandState, "TraitTurn"), "PlayerAttackActionCount"), Is.EqualTo(1));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void MismatchedOwnedProjection_RejectsBeforeAnyStore()
        {
            using var battle = Battle();
            var transition = Award(battle, out _);
            // 특성 투영만 원본으로 되돌려 보유·수신 기여분과의 불일치를 만든다.
            var altered = transition.Characters.Characters.Select(c => c.Final.EntityId == battle.Actor.EntityId
                ? new CharacterStateChange(c.Expected, ResetTraitProjection(c.Final, c.Expected.Stats.AttackPower)) : c).ToArray();
            var changes = new CharacterChanges(altered, transition.Characters.Souls, transition.Characters.Main,
                transition.Characters.Created, transition.Characters.Owned);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(Replace(transition, characters: changes)));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void ChangedReceivedGrant_RejectsOldExpectedFacts()
        {
            using var battle = Battle();
            var transition = Attack(battle);
            var actor = battle.Actor;
            var changed = Project(WithEffects(actor, Effects(3)), 0);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor, changed) },
                Array.Empty<SoulCountChange>()));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Actor.Stats.EffectiveAttackPower, Is.EqualTo(actor.Stats.EffectiveAttackPower + 3));
        }

        [Test]
        public void StageEndPreservesOwnedBonus_NewRunReplacesAllTraitContributions()
        {
            using var battle = Battle();
            var transition = Award(battle, out var awarded);
            battle.Buffers.BattleWriteSink.Apply(transition);
            battle.Buffers.BattleWriteSink.EndBattle();
            Assert.That(battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == awarded.Id).HunterAttackBonus, Is.EqualTo(2));
            Assert.That(battle.Buffers.CharacterReadSink.Characters, Is.Empty);
            var projected = battle.Core.CharacterStats.ReadOwned().Items.Single(o => o.OwnedId == awarded.Id);
            Assert.That(projected.Stats.AttackPower, Is.EqualTo(battle.OwnedStats.Stats.AttackPower + 2));
            Assert.That(battle.Core.RunStart.StartNewRun(1).Succeeded, Is.True);
            Assert.That(battle.Buffers.CharacterReadSink.Owned.All(o => o.HunterAttackBonus == 0 && o.Growth.IsEmpty), Is.True);
            Assert.That(battle.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(battle.Buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [Test]
        public void FreshBattle_RejectsReceivedGrantsAndKeepsHunterProjection()
        {
            using var battle = Battle();
            var buffers = battle.Buffers;
            var original = buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var membership = buffers.CombatStateReadSink.Current.Membership;
            var transition = Award(battle, out var awarded);
            buffers.BattleWriteSink.Apply(transition);
            buffers.BattleWriteSink.EndBattle();
            var store = (ICharacterBufferStore)buffers.CharacterReadSink;
            var fresh = original.Select(c => Project(c, c.OwnedId == awarded.Id ? 2 : 0)).ToArray();
            var before = new CoreIntegrationSnapshot(buffers);
            Assert.DoesNotThrow(() => store.PrepareBattle(fresh, membership));
            before.AssertUnchanged(buffers);
            var stale = fresh.Select(c => c.OwnedId == awarded.Id ? Project(WithEffects(c, Effects(3)), 2) : c).ToArray();
            Assert.Throws<InvalidOperationException>(() => store.PrepareBattle(stale, membership));
            before.AssertUnchanged(buffers);
        }

        private static ConfiguredMovementBattle Battle() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));

        private static CombatCommandTransition Attack(ConfiguredMovementBattle battle)
        {
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            return transition;
        }

        private static CombatCommandTransition Award(ConfiguredMovementBattle battle, out OwnedCharacterRecord finalOwned)
        {
            var transition = Attack(battle);
            var original = transition.Input.Owned.Single(o => o.Id == battle.Actor.OwnedId);
            finalOwned = Owned(original, 2);
            var c = transition.Characters;
            var facts = c.Characters.Select(change => change.Final.EntityId == battle.Actor.EntityId
                ? new CharacterStateChange(change.Expected, Project(WithEffects(change.Final, Effects(3)), 2)) : change).ToArray();
            var changes = new CharacterChanges(facts, c.Souls, c.Main, c.Created, new[] { new OwnedCharacterChange(original, finalOwned) });
            return Replace(transition, characters: changes, state: WithTurns(transition.FinalState, 1));
        }

        private static object Effects(int amount)
        {
            Type grant = Contract("TraitAttackGrant");
            var list = Array.CreateInstance(grant, 1);
            list.SetValue(Activator.CreateInstance(grant, new EffectOriginKey(EffectOwnerKind.Trait, 1, 220, "fixture"), amount, 1), 0);
            return Activator.CreateInstance(Contract("CharacterTraitEffects"), new object[] { list });
        }

        private static Type Contract(string name)
        {
            Type result = typeof(CharacterCombatFacts).Assembly.GetType(typeof(CharacterCombatFacts).Namespace + "." + name);
            Assert.That(result, Is.Not.Null, name);
            return result;
        }

        private static object Get(object value, string property) => value.GetType().GetProperty(property).GetValue(value);

        private static object With(object value, string parameter, object argument)
        {
            var method = value.GetType().GetMethod("With");
            Assert.That(method.GetParameters().Any(p => p.Name == parameter), Is.True, parameter);
            return method.Invoke(value, method.GetParameters().Select(p => p.Name == parameter ? argument : p.DefaultValue).ToArray());
        }

        private static CharacterCombatFacts WithEffects(CharacterCombatFacts facts, object effects) => (CharacterCombatFacts)With(facts, "traitEffects", effects);
        private static CombatCommandState WithTurns(CombatCommandState state, int count) =>
            (CombatCommandState)With(state, "traitTurn", Activator.CreateInstance(Contract("TraitTurnFacts"), count, 0));

        private static CharacterCombatFacts Project(CharacterCombatFacts character, int hunter)
        {
            var method = typeof(CombatEffectiveStatsCalculator).GetMethod("ProjectTraitAttack");
            Assert.That(method, Is.Not.Null);
            return (CharacterCombatFacts)method.Invoke(null, new object[] { character, hunter });
        }

        private static CharacterCombatFacts ResetTraitProjection(CharacterCombatFacts character, int attack)
        {
            var stats = character.Stats.With(attackPower: attack);
            stats = (ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.UnitStatsSnapshot)With(stats, "traitAttackBonus", 0);
            return character.With(stats: stats);
        }

        private static OwnedCharacterRecord Owned(OwnedCharacterRecord original, int hunter)
        {
            var constructor = typeof(OwnedCharacterRecord).GetConstructors().Single();
            Assert.That(constructor.GetParameters().Any(p => p.Name == "hunterAttackBonus"), Is.True);
            return (OwnedCharacterRecord)constructor.Invoke(new object[] { original.Id, original.CharacterKey, original.Role,
                original.Level, original.Experience, original.Growth, hunter, original.SpecializationCategory, original.PlayerSkillUpgrade });
        }

        private static CombatCommandTransition Replace(CombatCommandTransition t, CombatCommandInput input = null,
            CharacterChanges characters = null, CombatCommandState state = null) =>
            new(input ?? t.Input, t.Accepted, t.Reason, characters ?? t.Characters, t.Field, state ?? t.FinalState,
                t.Outcome, t.Attack, t.ConsumedSoulIds, t.Completion, t.Movement, t.Events, t.PathMovement,
                t.EnvironmentBindings, t.BlockedContacts, t.InventoryChange, t.RewardChange, t.RewardSettlement, t.ActionUseSettlement,
                t.UndoMovement, t.MovementEffectUses, t.HealthExecutions, t.HealthClaims);
    }
}
