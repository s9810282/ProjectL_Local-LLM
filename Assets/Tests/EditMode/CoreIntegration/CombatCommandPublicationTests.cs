using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatCommandPublicationTests
    {
        [Test]
        public void MoveThenAttack_PassesPublicationAndConsumesEachActionOnce()
        {
            // 이동한 유닛의 공격 정산(미사용 아님 → 공격함)도 Buffer 게시 검사를 통과한다(설계 2026-10-27 이동 후 공격).
            // 대상 칸은 출발 칸 (0,1)에서 공격이 확인된 거리 (1,2)를 이동 뒤 (1,1)에서도 유지한다.
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(2, 3)));
            battle.Move(battle.Actor, new CellCoord(1, 1));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            battle.Submit(battle.Attack(battle.Actor, new CellCoord(2, 3)));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.ActionUseSettlement.ActorId, Is.EqualTo(battle.Actor.EntityId));
            Assert.That(result.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [Test]
        public void CalculationOverflow_LeavesEveryStoreAndCommandMarkersUnpublished()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new OverflowAttackSource());
            var b = battle.Buffers;
            int number = b.CombatViewWriteSink.Submit(battle.Attack(battle.Actor,
                battle.Character(EntityKey.Unit_Demon).Anchor.Value));
            var before = new CoreIntegrationSnapshot(b);
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int handled = b.CombatStateReadSink.HandledCommandNumber;
            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<OverflowException>(() => battle.Core.CombatCommands.ProcessNextCommand());
                before.AssertUnchanged(b);
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(b.CombatViewReadSink.TryGetNextCommand(handled, out var pending), Is.True);
                Assert.That(pending.Number, Is.EqualTo(number));
            }
        }

        private sealed class OverflowAttackSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3))
                    .Read(database, chapterKey, stageKey);
                var units = source.Units.Select(s => s.Key != EntityKey.Unit_Cowboy ? s :
                    new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                        s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, int.MaxValue, s.PhysicalDefense, s.Priority,
                        new TraitSpec[] { new DistanceDamageScaleTraitSpec(new[] { 200, 200 }) },
                        s.OwnerEligibility, s.Occlusion, s.Behavior, killReward: s.KillReward)).ToArray();
                var profiles = source.AttackProfiles.Select(p =>
                    new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray();
                return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation,
                    units, source.Structures, source.Categories, profiles);
            }
        }

        [Test]
        public void RepeatedAttackPreviewThenCommand_PreservesGameplayAndPublishesOnce()
        {
            using var battle = Create();
            var b = battle.Buffers;
            var actor = battle.Actor;
            var victim = battle.Character(EntityKey.Unit_Demon);
            var before = battle.CaptureGameplay();
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int handled = b.CombatStateReadSink.HandledCommandNumber;
            int expectedHealth = 0;
            for (int i = 0; i < 3; i++)
            {
                b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(actor.EntityId,
                    AttackAim.ForCell(victim.Anchor.Value)));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                Assert.That(preview.Attack.Value.CanAttack, Is.True);
                expectedHealth = preview.Attack.Value.Targets.ToArray().Single(t => t.TargetId == victim.EntityId).TargetHealthAfter;
                Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            }
            int number = b.CombatViewWriteSink.Submit(battle.Attack(actor, victim.Anchor.Value));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(expectedHealth));
            var once = new CoreIntegrationSnapshot(b);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(b);
        }

        [Test]
        public void Publication_StaleDebuffRejectsEveryStore_AndBattleEndClearsCharacters()
        {
            using var battle = Create();
            var b = battle.Buffers;
            var valid = Attack(battle);
            var victim = valid.Input.Characters.First(c => c.Owner == Owner.Enemy);
            var changed = CombatDebuffApplication.WithDebuffs(victim,
                new CharacterDebuffState(new IntimidationDebuffState(20),
                    defenseReduction: new DefenseReductionDebuffState(30, 1)));
            b.BattleWriteSink.Apply(new CharacterChanges(
                new[] { new CharacterStateChange(victim, changed) }, Array.Empty<SoulCountChange>()));
            var before = new CoreIntegrationSnapshot(b);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(valid));
            before.AssertUnchanged(b);
            b.BattleWriteSink.EndBattle();
            Assert.That(b.CharacterReadSink.Characters, Is.Empty);
            Assert.That(victim.Debuffs.SameValues(CharacterDebuffState.Empty), Is.True);
            Assert.That(changed.Debuffs.DefenseReduction.ReductionPercent, Is.EqualTo(30));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Publication_DifferentOwnedLevelRejectsWithoutWrites(bool differentLevel)
        {
            using var battle = Create();
            var b = battle.Buffers;
            var valid = Attack(battle);
            var ownedBefore = b.CharacterReadSink.Owned.ToArray();
            var target = valid.Input.Owned.Single(o => o.CharacterKey == EntityKey.Unit_Sniper);
            var expected = new OwnedCharacterRecord(target.Id, target.CharacterKey, target.Role,
                differentLevel ? target.Level + 2 : target.Level, target.Experience);
            var input = OwnedInput(valid.Input, valid.Input.Owned.Select(o => o.Id == target.Id ? expected : o).ToArray());
            var candidate = Replace(valid, input);
            if (differentLevel)
            {
                var before = new CoreIntegrationSnapshot(b);
                Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(candidate)).Message,
                    Does.Contain("보유·영혼 원본"));
                before.AssertUnchanged(b);
            }
            // 같은 행동의 원본을 바꾸지 않은 대조군이 실제로 게시되어야 한다.
            AssertAccepted(battle, differentLevel ? valid : candidate);
            Assert.That(b.CharacterReadSink.Owned, Is.EqualTo(ownedBefore));
        }

        [Test]
        public void Publication_DifferentOwnedExperienceRejectsWithoutWrites()
        {
            using var battle = Create();
            var b = battle.Buffers;
            var valid = Attack(battle);
            var owned = valid.Input.Owned.Select(o => o.CharacterKey == EntityKey.Unit_Sniper
                ? new OwnedCharacterRecord(o.Id, o.CharacterKey, o.Role, o.Level, o.Experience + 5) : o).ToArray();
            var stale = Replace(valid, OwnedInput(valid.Input, owned));
            var before = new CoreIntegrationSnapshot(b);
            Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(stale)).Message,
                Does.Contain("보유·영혼 원본"));
            before.AssertUnchanged(b);
            AssertAccepted(battle, valid);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Publication_OwnedChangeInstallsWithCommandAtomically(bool staleSoulField)
        {
            using var battle = Create();
            var b = battle.Buffers;
            var valid = WithOwnedChange(Attack(battle), out var leveled);
            var candidate = valid;
            if (staleSoulField)
            {
                var input = valid.Input;
                var stale = new SoulFieldSnapshot(input.Souls.NextSoulId + 1, input.Souls.Entries);
                var source = input.WithWorld(input.State, input.Characters, input.Main, input.SoulCounts, stale, input.EnvironmentBindings);
                candidate = Replace(valid, source, field: new SoulFieldChange(stale, stale));
                var before = new CoreIntegrationSnapshot(b);
                Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(candidate)).Message,
                    Does.Contain("영혼 필드 원본"));
                before.AssertUnchanged(b);
            }
            AssertAccepted(battle, staleSoulField ? valid : candidate);
            Assert.That(b.CharacterReadSink.TryGetOwned(leveled.Id, out var stored), Is.True);
            Assert.That(stored.SameValues(leveled), Is.True);
        }

        [Test]
        public void Publication_AfterOwnedChange_NextInputMustReadInstalledRecord()
        {
            using var battle = Create();
            var b = battle.Buffers;
            var first = WithOwnedChange(Attack(battle), out var leveled);
            AssertAccepted(battle, first);
            battle.Submit(battle.EndPlayerTurn());
            battle.AdvanceToPlayerOrDay();
            var current = Attack(battle);
            Assert.That(current.Input.Owned.Single(o => o.Id == leveled.Id).SameValues(leveled), Is.True);
            var stale = Replace(current, OwnedInput(current.Input, first.Input.Owned));
            var before = new CoreIntegrationSnapshot(b);
            Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(stale)).Message,
                Does.Contain("보유·영혼 원본"));
            before.AssertUnchanged(b);
            AssertAccepted(battle, current);
        }

        [Test]
        public void Rejection_PublishesOnceWithoutGameplayChanges()
        {
            using var battle = Create();
            var b = battle.Buffers;
            var input = battle.Publication().Attack(battle.Actor, new CellCoord(99, 99));
            var rejected = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
            var state = b.CombatStateReadSink.Current.CommandState;
            var characters = b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var souls = b.CombatStateReadSink.SoulField;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            b.BattleWriteSink.Apply(rejected);
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(souls), Is.True);
            foreach (var c in characters)
                Assert.That(b.CharacterReadSink.Characters.Single(stored => stored.EntityId == c.EntityId).ToFacts().SameValues(c), Is.True);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(input.Number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(rejected.Reason));
            var once = new CoreIntegrationSnapshot(b);
            // 서비스의 handled 검사보다 낮은 실제 거래 경계에서 같은 제안을 재적용한다.
            b.BattleWriteSink.Apply(rejected);
            once.AssertUnchanged(b);
        }

        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));

        private static CombatCommandTransition Attack(ConfiguredMovementBattle battle)
        {
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.ActionUseSettlement, Is.Not.Null);
            return result;
        }

        private static void AssertAccepted(ConfiguredMovementBattle battle, CombatCommandTransition result)
        {
            var b = battle.Buffers;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            b.BattleWriteSink.Apply(result);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(result.Input.Number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(result.ActionUseSettlement.ActorId, Is.EqualTo(battle.Actor.EntityId));
            Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            foreach (var change in result.Characters.Characters)
                Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == change.Final.EntityId)
                    .ToFacts().SameValues(change.Final), Is.True);
        }

        private static CombatCommandInput OwnedInput(CombatCommandInput input,
            System.Collections.Generic.IReadOnlyList<OwnedCharacterRecord> owned) =>
            input.WithPersistentFacts(owned, input.ItemInventory, input.GoldBalance, input.FameBalance);

        private static CombatCommandTransition WithOwnedChange(CombatCommandTransition result, out OwnedCharacterRecord leveled)
        {
            var sub = result.Input.Owned.Single(o => o.CharacterKey == EntityKey.Unit_Sniper);
            leveled = new OwnedCharacterRecord(sub.Id, sub.CharacterKey, sub.Role, 2, 20);
            var c = result.Characters;
            var changes = new CharacterChanges(c.Characters, c.Souls, c.Main, c.Created, new[] { new OwnedCharacterChange(sub, leveled) });
            return Replace(result, characters: changes);
        }

        private static CombatCommandTransition Replace(CombatCommandTransition t, CombatCommandInput input = null,
            CharacterChanges characters = null, SoulFieldChange field = null) =>
            new(input ?? t.Input, t.Accepted, t.Reason, characters ?? t.Characters, field ?? t.Field, t.FinalState,
                t.Outcome, t.Attack, t.ConsumedSoulIds, t.Completion, t.Movement, t.Events, t.PathMovement,
                t.EnvironmentBindings, t.BlockedContacts, t.InventoryChange, t.RewardChange, t.RewardSettlement, t.ActionUseSettlement);
    }
}
