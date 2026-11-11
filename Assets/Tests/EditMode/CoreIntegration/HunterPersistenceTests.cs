using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class HunterPersistenceTests
    {
        internal static ConfiguredMovementBattle LateHunterBattle()
        {
            var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new HunterSource(twoEnemies: true, lateHunter: true), deploySniper: false);
            try
            {
                for (int turn = 0; turn < 8; turn++)
                {
                    battle.Submit(battle.EndPlayerTurn());
                    battle.AdvanceToPlayerOrDay();
                }
                Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
                Assert.That(battle.Buffers.CharacterReadSink.Characters.Where(c => c.Owner == Owner.Enemy)
                    .All(c => c.Area == CharacterRuntimeArea.EnemyWaiting), Is.True);
                battle.Submit(CombatViewCommandData.PlaceUnit(battle.Character(EntityKey.Unit_Sniper).EntityId, new CellCoord(4, 0), Rotation4.R0));
                return battle;
            }
            catch { battle.Dispose(); throw; }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 0)]
        public void FailedOrDuplicatePublication_DoesNotAwardTwice(int failure, int award = 2)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HunterSource(twoEnemies: true, award: award));
            var target = Marked(battle);
            var input = battle.Publication().Attack(battle.Actor, target.Anchor.Value);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            before.AssertUnchanged(battle.Buffers);
            if (failure == 1)
                input = input.WithPersistentFacts(input.Owned.Select(o => o.Id == battle.Actor.OwnedId
                    ? new OwnedCharacterRecord(o.Id, o.CharacterKey, o.Role, o.Level, o.Experience, o.Growth, 1) : o).ToArray(),
                    input.ItemInventory, input.GoldBalance, input.FameBalance);
            if (failure == 3)
            {
                var progress = input.State.EnvironmentProgress;
                input = input.WithWorld(input.State.With(environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal,
                    progress.NextSourceId, progress.RandomState + 1)), input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            }
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            if (failure == 2)
            {
                var actor = battle.Actor;
                var status = actor.Stats.TraitStatuses.ToArray().Single(t => t.Key == TraitKey.Hunter);
                var changed = (TraitStatusSnapshot)Activator.CreateInstance(status.GetType(), new object[] { Target(actor), 1 });
                var updated = actor.With(stats: actor.Stats.With(traitStatuses: actor.Stats.TraitStatuses.ToArray()
                    .Select(t => t.Key == TraitKey.Hunter ? changed : t).ToArray()));
                battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor, updated) }, Array.Empty<SoulCountChange>()));
                before = new CoreIntegrationSnapshot(battle.Buffers);
            }
            if (failure != 0)
            {
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                before.AssertUnchanged(battle.Buffers);
                Assert.That(Bonus(battle), Is.Zero);
                return;
            }
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(Bonus(battle), Is.EqualTo(award));
            Assert.That(Rewards(battle.Actor), Is.EqualTo(1));
            Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.EqualTo(award));
            Assert.That(Target(battle.Actor), Is.Not.EqualTo(target.EntityId));
            Assert.That(transition.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Any(a => a.TraitKey == TraitKey.Hunter && a.TraitActivationKind == TraitActivationKind.AttackGranted), Is.EqualTo(award > 0));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void SameVictory_ComposesHunterAndExperienceGrowth()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HunterSource(victory: true, experience: true));
            battle.Submit(battle.Attack(battle.Actor, Marked(battle).Anchor.Value));
            Assert.That(battle.Buffers.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(Bonus(battle), Is.EqualTo(2));
            var owned = battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == battle.Actor.OwnedId);
            Assert.That(owned.Level, Is.GreaterThan(1));
            Assert.That(owned.Growth.IsEmpty, Is.True);
            Assert.That(Rewards(battle.Actor), Is.Zero);
            Assert.That(Target(battle.Actor), Is.Null);
            battle.Core.BattleCompletion.EndBattle();
            Assert.That(battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == owned.Id).HunterAttackBonus, Is.EqualTo(2));
        }

        [Test]
        public void SecondStage_ResetsTargetAndCapWhileUsingRunBonus()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HunterSource(victory: true));
            // 두 전투 노드를 가진 fixture 그래프를 구성하고, 클리어와 다음 진입은 실제 Core 게시를 거칩니다.
            battle.Buffers.StageProgressWriteSink.Clear();
            battle.Buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new ProjectL.Systems.RuntimeSystem.BufferSystems.Stage.StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new ProjectL.Systems.RuntimeSystem.BufferSystems.Stage.StageProgressData(2, 1101, 1, 0, new[] { 3 }),
                new ProjectL.Systems.RuntimeSystem.BufferSystems.Stage.StageProgressData(3, 1101, 2, 0, Array.Empty<int>()) });
            var actorId = battle.Actor.OwnedId;
            battle.Submit(battle.Attack(battle.Actor, Marked(battle).Anchor.Value));
            Assert.That(Bonus(battle), Is.EqualTo(2));
            battle.Core.BattleCompletion.EndBattle();
            Assert.That(battle.Core.BattleEntry.EnterBattle(1, new StageNodeId(3)).Accepted, Is.True);
            var fresh = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).Single(c => c.OwnedId == actorId);
            Assert.That(fresh.Stats.TraitAttackBonus, Is.EqualTo(2));
            Assert.That(Rewards(fresh), Is.Zero);
            Assert.That(Target(fresh), Is.Null);
        }

        [Test]
        public void AbandonThenNewRun_ReplacesEveryHunterContribution()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HunterSource(twoEnemies: true));
            battle.Submit(battle.Attack(battle.Actor, Marked(battle).Anchor.Value));
            Assert.That(Bonus(battle), Is.EqualTo(2));
            battle.Core.BattleCompletion.AbandonBattle();
            Assert.That(battle.Buffers.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(Rewards(battle.Actor), Is.Zero);
            Assert.That(Target(battle.Actor), Is.Null);
            battle.Core.BattleCompletion.EndBattle();
            Assert.That(battle.Core.RunStart.StartNewRun(1).Succeeded, Is.True);
            Assert.That(battle.Buffers.CharacterReadSink.Owned.All(o => o.HunterAttackBonus == 0), Is.True);
            Assert.That(battle.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(battle.Buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        private static CharacterCombatFacts Marked(ConfiguredMovementBattle battle) => battle.Buffers.CharacterReadSink.Characters
            .Select(c => c.ToFacts()).Single(c => c.EntityId == Target(battle.Actor));
        private static EntityId? Target(CharacterCombatFacts actor) => (EntityId?)Value(actor, "TargetId");
        private static int Rewards(CharacterCombatFacts actor) => (int)Value(actor, "SuccessfulRewards");
        private static object Value(CharacterCombatFacts actor, string property)
        {
            var state = actor.Stats.TraitStatuses.ToArray().Single(t => t.Key == TraitKey.Hunter);
            return state.GetType().GetProperty(property).GetValue(state);
        }
        private static int Bonus(ConfiguredMovementBattle battle) => battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == battle.Actor.OwnedId).HunterAttackBonus;

        internal sealed class HunterSource : IBattleInitializationSource
        {
            private readonly bool _victory;
            private readonly bool _experience;
            private readonly bool _twoEnemies;
            private readonly bool _lateHunter;
            private readonly int _award;
            internal HunterSource(bool victory = false, bool experience = false, bool twoEnemies = false, bool lateHunter = false, int award = 2)
            { _victory = victory; _experience = experience; _twoEnemies = twoEnemies; _lateHunter = lateHunter; _award = award; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3), _victory).Read(database, chapterKey, stageKey);
                var placements = new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(0, 3), Rotation4.R180) }
                    .Concat(_twoEnemies ? new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(6, 3), Rotation4.R180) }
                        : Array.Empty<InitialEnemyPlacementSpec>()).ToArray();
                var stage = new StageConfig(8, 6, 8, 2, 3, 42, placements, Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(),
                    new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, _victory ? new CellCoord(1, 3) : new CellCoord(3, 5), Rotation4.R180) });
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? (_lateHunter ? 100 : 1) : s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Demon ? 0 : s.Key == EntityKey.Unit_Cowboy ? 20 : s.AttackPower,
                    s.PhysicalDefense, s.Priority, s.Key == (_lateHunter ? EntityKey.Unit_Sniper : EntityKey.Unit_Cowboy)
                        ? new TraitSpec[] { new HunterTraitSpec(_award, 3) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key),
                        p.Definition.Key == EntityKey.Unit_Cowboy && _victory
                            ? new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70) : p.Method, p.OwnedId)).ToArray(),
                    progression: _experience ? new LevelProgressionSpec(150, 100, 3, 1, 1) : default,
                    clearRewards: _experience ? new StageClearRewardSpec(0, 0, 950) : null);
            }
        }
    }
}
