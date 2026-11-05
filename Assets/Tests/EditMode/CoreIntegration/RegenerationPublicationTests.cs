using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RegenerationPublicationTests
    {
        [Test]
        public void EndTurn_PublishesCappedRecoveryAndMainHealthOnce_WithoutChangingOldSnapshots()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RegenerationSource(1));
            Injure(battle, battle.Actor);
            Injure(battle, battle.Main);
            var old = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == battle.Main.EntityId);
            var transition = CombatCommandCalculator.Calculate(EndPlayerInput(battle), Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var recoveries = transition.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.Regeneration && t.TraitActivationKind == TraitActivationKind.HealthRecovered).ToArray();
            Assert.That(recoveries.Select(t => t.Magnitude), Is.EqualTo(new[] { 5, 5 }));
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(battle.Actor.Health.MaxHealth));
            Assert.That(battle.Main.Health.CurrentHealth, Is.EqualTo(battle.Main.Health.MaxHealth));
            Assert.That(battle.Buffers.CharacterReadSink.TryGetMainState(battle.Main.OwnedId.Value, out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(battle.Main.Health.CurrentHealth));
            Assert.That(old.Health.CurrentHealth, Is.EqualTo(old.Health.MaxHealth - 5));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void FreshBattle_RejectsCarriedCadenceBeforeAnyStoreInstalls()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RegenerationSource(2));
            var fresh = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var membership = battle.Buffers.CombatStateReadSink.Current.Membership;
            battle.Buffers.BattleWriteSink.EndBattle();
            var store = (ICharacterBufferStore)battle.Buffers.CharacterReadSink;
            var carried = fresh.Select(c => c.CharacterKey == EntityKey.Unit_Cowboy ? WithProgress(c, 1) : c).ToArray();
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => store.PrepareBattle(carried, membership));
            before.AssertUnchanged(battle.Buffers);
            Assert.DoesNotThrow(() => store.PrepareBattle(fresh, membership));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void StaleCadence_RejectsEveryStoreAndResult()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RegenerationSource(2));
            var actor = battle.Actor;
            var transition = CombatCommandCalculator.Calculate(EndPlayerInput(battle), Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(Status(transition.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final).CompletedEligibleTurnEnds, Is.EqualTo(1));
            var changed = WithProgress(actor, 1);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor, changed) }, Array.Empty<SoulCountChange>()));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(Status(actor).CompletedEligibleTurnEnds, Is.Zero);
        }

        [Test]
        public void LatePreparationFailure_PublishesNeitherCadenceNorHealthNorResult()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RegenerationSource(2));
            var actor = battle.Actor;
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor, WithProgress(actor, 1)) }, Array.Empty<SoulCountChange>()));
            Injure(battle, battle.Actor);
            var input = EndPlayerInput(battle);
            input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(999, 0)), input.Characters, input.Main,
                input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var change = transition.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId);
            Assert.That(change.Final.Health.CurrentHealth, Is.GreaterThan(change.Expected.Health.CurrentHealth));
            Assert.That(Status(change.Final).CompletedEligibleTurnEnds, Is.Zero);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void Preview_DoesNotAdvanceOrPublishCadence()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RegenerationSource(2));
            var before = battle.CaptureGameplay();
            var snapshot = battle.Actor;
            for (int count = 0; count < 2; count++)
            {
                battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId,
                    AttackAim.ForCell(battle.Character(EntityKey.Unit_Demon).Anchor.Value)));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
                Assert.That(Status(battle.Actor).CompletedEligibleTurnEnds, Is.Zero);
            }
            Assert.That(Status(snapshot).CompletedEligibleTurnEnds, Is.Zero);
        }

        [Test]
        public void NightRecall_PublishesZeroCadenceForReturnedSub_AndRetainsFieldMainProgress()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RegenerationSource(3, passive: true));
            CharacterCombatFacts captured = null;
            for (int ending = 1; ending <= 8; ending++)
            {
                Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput),
                    $"ending={ending}, MainHP={battle.Main.Health.CurrentHealth}, ActorHP={battle.Actor.Health.CurrentHealth}");
                battle.Submit(battle.EndPlayerTurn());
                if (ending == 8) captured = battle.Actor;
                battle.AdvanceToPlayerOrDay();
            }
            Assert.That(battle.Actor.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(Status(battle.Actor).CompletedEligibleTurnEnds, Is.Zero);
            Assert.That(battle.Main.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(Status(battle.Main).CompletedEligibleTurnEnds, Is.EqualTo(2));
            Assert.That(Status(captured).CompletedEligibleTurnEnds, Is.EqualTo(2));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var returned = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Returned && e.Final.EntityId == battle.Actor.EntityId);
            Assert.That(Status(returned.Expected).CompletedEligibleTurnEnds, Is.EqualTo(2));
            Assert.That(Status(returned.Final).CompletedEligibleTurnEnds, Is.EqualTo(2));
            var reset = result.Events.OfType<CombatTraitStateEvent>().Last(e => e.Final.EntityId == battle.Actor.EntityId);
            Assert.That(Status(reset.Expected).CompletedEligibleTurnEnds, Is.EqualTo(2));
            Assert.That(Status(reset.Final).CompletedEligibleTurnEnds, Is.Zero);
            var ordered = result.Events.ToArray();
            Assert.That(Array.IndexOf(ordered, reset), Is.GreaterThan(Array.IndexOf(ordered, returned)));
        }

        [Test]
        public void PostBattleEnhancement_ConstructsFreshZeroCadence_WithoutLiveReplacement()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1);
            var tables = new RegenerationGrowthTables();
            game.Database.TraitGrowthDefinitions = tables;
            game.Database.CategoryTraitProgressions = tables;
            game.Database.SetUnit(new UnitData(EntityKey.Unit_ShotGun, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, CategoryKey.Cannon, 5, 1, new UnitTraitData[] { new RegenerationTraitData(10, 2) },
                targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            game.EnterBattle();
            void Submit(CombatViewCommandData command)
            {
                game.Buffers.CombatViewWriteSink.Submit(command);
                Assert.That(game.Core.CombatCommands.ProcessNextCommand(), Is.True);
                Assert.That(game.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                Assert.That(result.Accepted, Is.True, result.Reason);
            }
            var holder = game.Buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == game.TargetId);
            Submit(CombatViewCommandData.PlaceMainCharacter(CellCoord.Zero, Rotation4.R0));
            Submit(CombatViewCommandData.PlaceUnit(holder.EntityId, new CellCoord(1, 0), Rotation4.R0));
            Submit(CombatViewCommandData.EnterCombat());
            Assert.That(game.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var state = game.Buffers.CombatStateReadSink.Current.CommandState;
            Submit(CombatViewCommandData.EndPlayerTurn(game.Buffers.CombatStateReadSink.BattleInstanceId, state.Turn.ControllerTurnSequence));
            var prior = game.Buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == game.TargetId).ToFacts();
            Assert.That(Status(prior).CompletedEligibleTurnEnds, Is.EqualTo(1));
            game.SetTargetLevel(2);
            Assert.That(game.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer, Is.Null);
            Assert.That(game.Current.Growth.Selections, Is.Empty);
            game.Buffers.BattleWriteSink.EndBattle();
            var offer = game.Open();
            var receipt = game.Publish(game.Request(offer, TraitKey.Regeneration));
            Assert.That(receipt.Result.Succeeded, Is.True);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number), Is.True);
            game.InjectBattleDuringDefinitionRead();
            var fresh = game.Buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == game.TargetId).ToFacts();
            Assert.That((Status(fresh).HealAmount, Status(fresh).HealEveryOwnSideTurnEnds, Status(fresh).CompletedEligibleTurnEnds), Is.EqualTo((20, 3, 0)));
            Assert.That(Status(prior).CompletedEligibleTurnEnds, Is.EqualTo(1));
        }

        private static RegenerationTraitStatusSnapshot Status(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<RegenerationTraitStatusSnapshot>().Single();

        private static CharacterCombatFacts WithProgress(CharacterCombatFacts character, int progress)
        {
            var source = Status(character);
            return character.With(stats: character.Stats.With(traitStatuses: new TraitStatusSnapshot[]
                { new RegenerationTraitStatusSnapshot(source.HealAmount, source.HealEveryOwnSideTurnEnds, progress) }));
        }

        private static void Injure(ConfiguredMovementBattle battle, CharacterCombatFacts character)
        {
            MainCharacterPersistentState main = null;
            if (character.OwnershipRole == CharacterOwnershipRole.Main)
                Assert.That(battle.Buffers.CharacterReadSink.TryGetMainState(character.OwnedId.Value, out main), Is.True);
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(character, main, character.Health.MaxHealth - 5));
        }

        private static CombatCommandInput EndPlayerInput(ConfiguredMovementBattle battle)
        {
            var buffers = battle.Buffers;
            int number = buffers.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            return battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.EndPlayerTurn, EntityId.Empty, default,
                expectedBattleInstanceId: buffers.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
        }

        private sealed class RegenerationSource : IBattleInitializationSource
        {
            private readonly int _interval;
            private readonly bool _passive;
            internal RegenerationSource(int interval, bool passive = false)
            {
                _interval = interval;
                _passive = passive;
            }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(_passive ? (CellCoord?)null : new CellCoord(7, 5)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy || s.Key == EntityKey.Unit_Akimbo ? new TraitSpec[] { new RegenerationTraitSpec(10, _interval) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }

        private sealed class RegenerationGrowthTables : ITraitGrowthDefinitionTableQuery, ICategoryTraitProgressionTableQuery
        {
            private readonly TraitGrowthDefinitionData _definition = new(new RegenerationTraitData(10, 2), new RegenerationTraitData(20, 3), "regen", "normal", "regen+", "enhanced");
            private readonly CategoryTraitProgressionData[] _categories = new[] { CategoryKey.Main, CategoryKey.Heavy, CategoryKey.Rider, CategoryKey.Cannon }
                .Select(key => new CategoryTraitProgressionData(key, key == CategoryKey.Cannon ? new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Enhance) } : Array.Empty<TraitGrowthStepSpec>(),
                    key == CategoryKey.Cannon ? new[] { TraitKey.Regeneration } : Array.Empty<TraitKey>())).ToArray();
            TraitGrowthDefinitionData ITraitGrowthDefinitionTableQuery.Get(TraitKey key) => key == _definition.Key ? _definition : throw new KeyNotFoundException();
            bool ITraitGrowthDefinitionTableQuery.TryGet(TraitKey key, out TraitGrowthDefinitionData data) { data = key == _definition.Key ? _definition : null; return data != null; }
            IReadOnlyList<TraitGrowthDefinitionData> ITraitGrowthDefinitionTableQuery.GetAll() => new[] { _definition };
            CategoryTraitProgressionData ICategoryTraitProgressionTableQuery.Get(CategoryKey key) => _categories.Single(c => c.Category == key);
            bool ICategoryTraitProgressionTableQuery.TryGet(CategoryKey key, out CategoryTraitProgressionData data) { data = _categories.SingleOrDefault(c => c.Category == key); return data != null; }
            IReadOnlyList<CategoryTraitProgressionData> ICategoryTraitProgressionTableQuery.GetAll() => _categories;
        }
    }
}
