using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitMovementAttackPublicationTests
    {
        [Test]
        public void MovePreviewAndNativeAttack_PublishHeldBonusThenConsumeOnce()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new MovementAttackSource());
            var before = battle.CaptureGameplay();
            var command = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(2, 2));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            battle.Submit(command);
            Assert.That(Held(battle.Actor), Is.EqualTo(6));
            Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.EqualTo(6));
            Assert.That(Moved(battle.Actor), Is.True);
            var target = battle.Character(EntityKey.Unit_Demon).Anchor.Value;
            before = battle.CaptureGameplay();
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            var transition = CombatCommandCalculator.Calculate(battle.Publication().Attack(battle.Actor, target), Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(Held(battle.Actor), Is.Zero);
            Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.Zero);
            Assert.That(Moved(battle.Actor), Is.True);
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void StaleAttack_RejectsBonusConsumptionAtomically()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new MovementAttackSource());
            battle.Submit(battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1)));
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var actor = battle.Actor;
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(actor, null, actor.Health.CurrentHealth - 1));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(Held(battle.Actor), Is.EqualTo(2));
        }

        [Test]
        public void Acceleration_SurvivesOrdinaryTurns_ButActualNightRecallClearsIt()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new MovementAttackSource(enemy: false));
            battle.Submit(battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1)));
            for (int turn = 0; turn < 8; turn++)
            {
                battle.Submit(battle.EndPlayerTurn());
                battle.AdvanceToPlayerOrDay();
                int expectedHeld = battle.Actor.Area == CharacterRuntimeArea.Field ? 2 : 0;
                Assert.That(Held(battle.Actor), Is.EqualTo(expectedHeld));
                Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.EqualTo(expectedHeld));
                if (turn < 7) Assert.That(Moved(battle.Actor), Is.False);
            }
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
            Assert.That(battle.Actor.Area, Is.Not.EqualTo(CharacterRuntimeArea.Field));
        }

        [Test]
        public void HeldProjection_CannotBePublishedWithoutItsStatusContribution()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new MovementAttackSource());
            battle.Submit(battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1)));
            var actor = battle.Actor;
            var malformed = actor.With(stats: actor.Stats.With(traitAttackBonus: 0));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(
                new[] { new CharacterStateChange(actor, malformed) }, Array.Empty<SoulCountChange>())));
            before.AssertUnchanged(battle.Buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FreshBattle_RejectsHeldBonusOrPriorMovement(bool held)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new MovementAttackSource());
            var fresh = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var membership = battle.Buffers.CombatStateReadSink.Current.Membership;
            battle.Buffers.BattleWriteSink.EndBattle();
            var store = (ICharacterBufferStore)battle.Buffers.CharacterReadSink;
            Assert.DoesNotThrow(() => store.PrepareBattle(fresh, membership));
            var stale = fresh.Select(c => c.CharacterKey != EntityKey.Unit_Cowboy ? c : c.With(stats: c.Stats.With(
                traitStatuses: c.Stats.TraitStatuses.ToArray().Select(t => held && t.Key == TraitKey.Acceleration
                    ? new AccelerationTraitStatusSnapshot(6) : !held && t.Key == TraitKey.StationaryFire
                    ? (TraitStatusSnapshot)new StationaryFireTraitStatusSnapshot(true) : t).ToArray(), traitAttackBonus: held ? 6 : 0))).ToArray();
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => store.PrepareBattle(stale, membership));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void MovementAndAttack_PreserveOwnedHunterContribution()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new MovementAttackSource());
            var actor = battle.Actor;
            var owned = battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == actor.OwnedId);
            var awarded = new OwnedCharacterRecord(owned.Id, owned.CharacterKey, owned.Role, owned.Level, owned.Experience, owned.Growth, 7);
            var cells = new[] { new CellCoord(0, 1), new CellCoord(1, 1) };
            var command = battle.PreviewMove(actor, cells);
            int number = battle.Buffers.CombatViewWriteSink.Submit(command);
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move, actor.EntityId, default,
                cell: cells.Last(), path: new MovementPath(cells), movementContext: command.MovementContext,
                expectedBattleInstanceId: battle.Buffers.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            // Hunter 실행을 켜지 않고 기존 소유·전투 원자 게시 계약에 공존할 기여분만 제안합니다.
            var characters = new CharacterChanges(move.Characters.Characters.Select(c => c.Final.EntityId == actor.EntityId
                ? new CharacterStateChange(c.Expected, CombatEffectiveStatsCalculator.ProjectTraitAttack(c.Final, 7)) : c).ToArray(),
                move.Characters.Souls, move.Characters.Main, move.Characters.Created, new[] { new OwnedCharacterChange(owned, awarded) });
            battle.Buffers.BattleWriteSink.Apply(MovementUndoPolicy.Reconcile(new CombatCommandTransition(input, true, "", characters, move.Field, move.FinalState, move.Outcome,
                consumedSoulIds: move.ConsumedSoulIds, events: move.Events, pathMovement: move.PathMovement, actionUseSettlement: move.ActionUseSettlement)));
            Assert.That(Held(battle.Actor), Is.EqualTo(2));
            Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.EqualTo(9));
            battle.Submit(battle.Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value));
            Assert.That(Held(battle.Actor), Is.Zero);
            Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.EqualTo(7));
            Assert.That(battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == owned.Id).HunterAttackBonus, Is.EqualTo(7));
        }

        private static int Held(CharacterCombatFacts actor) => (int)Value(actor, TraitKey.Acceleration, "HeldAttackBonus");
        private static bool Moved(CharacterCombatFacts actor) => (bool)Value(actor, TraitKey.StationaryFire, "MovedThisOwnTurn");
        private static object Value(CharacterCombatFacts actor, TraitKey key, string property) =>
            actor.Stats.TraitStatuses.ToArray().Single(t => t.Key == key).GetType().GetProperty(property)
                .GetValue(actor.Stats.TraitStatuses.ToArray().Single(t => t.Key == key));

        internal sealed class MovementAttackSource : IBattleInitializationSource
        {
            private readonly bool _enemy;
            internal MovementAttackSource(bool enemy = true) => _enemy = enemy;
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(_enemy ? new CellCoord(2, 3) : (CellCoord?)null).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? 1000 : s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower,
                    s.PhysicalDefense, s.Priority, s.Key == EntityKey.Unit_Cowboy
                        ? new TraitSpec[] { new AccelerationTraitSpec(2), new StationaryFireTraitSpec(100) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
