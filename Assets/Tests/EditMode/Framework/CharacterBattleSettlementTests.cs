using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class CharacterBattleSettlementTests
    {
        [Test]
        public void Abandonment_ClearsHeldEffectsWithoutChangingPersistentAttack()
        {
            var b = Create();
            var main = Main(b);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(main,
                HeldEnvironmentEffects.WithHeldState(main, new CharacterHeldEnvironmentState(41, true, 7))) }, Array.Empty<SoulCountChange>()));
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var result = new BattleCompletionCalculator(default).Abandon(Main(b), persistent,
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(10)));
            Assert.That(result.Characters.Characters.Single().Final.HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty), Is.True);
            Assert.That(result.Characters.Main.Final.PersistentAttack, Is.EqualTo(persistent.PersistentAttack));
        }

        [Test]
        public void BattleEnd_ClampsHealthAboveRemainingMax_AndCleanupSucceeds()
        {
            // 임시 최대 체력 +5로 13/15가 된 메인은 전투 종료 시 남는 최대치(지속 10 + 영혼 0)로 정리된다(D2).
            var b = Create();
            var before = Main(b);
            b.CharacterReadSink.TryGetMainState(out var initial);
            var raised = CharacterBufferTestData.MainState(13);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before,
                CharacterBufferTestData.MainFacts(before.OwnedId.Value, raised, temporaryMaxHealth: 5)) },
                Array.Empty<SoulCountChange>(), new MainCharacterPersistentChange(initial, raised)));
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(13, 15)));

            b.CharacterReadSink.TryGetMainState(out var persistent);
            var result = new BattleCompletionCalculator(default).Abandon(Main(b), persistent,
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(10)));
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.EqualTo(10));
            b.BattleWriteSink.CompleteBattle(result);
            Assert.DoesNotThrow(() => b.BattleWriteSink.EndBattle());
            b.CharacterReadSink.TryGetMainState(out var stored);
            Assert.That(stored.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void MainHealth_IsVisibleAfterEachCommand()
        {
            var b = Create();
            foreach (int hp in new[] { 4, 7, 9 })
            {
                var main = Main(b);
                b.CharacterReadSink.TryGetMainState(out var persistent);
                b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(main, persistent, hp));
                Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(hp));
                b.CharacterReadSink.TryGetMainState(out var stored);
                Assert.That(stored.CurrentHealth, Is.EqualTo(hp));
            }
        }

        [Test]
        public void MainPersistentStatsSurvive_TemporaryBuffExpires()
        {
            var b = Create();
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var final = CharacterBufferTestData.MainState(7, 15, 8, 6, 5, 2);
            var before = Main(b);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before,
                CharacterBufferTestData.MainFacts(before.OwnedId.Value, persistent, temporaryAttack: 3)) },
                Array.Empty<SoulCountChange>()));
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetPersistentMain(Main(b), persistent, final));
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(7, 15)));
            Assert.That(Main(b).Stats.EffectiveAttackPower, Is.EqualTo(11));
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(BattlePreparationCalculator.Prepare(
                CharacterBattlePreparationTests.Initialization(), b.CharacterReadSink.Owned, final,
                new System.Collections.Generic.Dictionary<CharacterInstanceId, int> { [new CharacterInstanceId(1)] = 0 },
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>()));
            Assert.That(Main(b).Stats.AttackPower, Is.EqualTo(8));
            Assert.That(Main(b).Stats.MaxActionCost, Is.EqualTo(6));
            Assert.That(Main(b).Stats.MoveRange, Is.EqualTo(5));
            Assert.That(Main(b).Stats.MoveCost, Is.EqualTo(2));
            Assert.That(Main(b).TemporaryAttackBonus, Is.Zero);
            Assert.That(Main(b).Stats.EffectiveAttackPower, Is.EqualTo(8));
        }

        [Test]
        public void TurnEnd_RestoresEndingUnitBeforeItsNextTurn()
        {
            var b = Create();
            var main = Main(b);
            var field = b.CombatStateReadSink.SoulField;
            var input = new SoulActionChanges(Empty(), new SoulFieldChange(field, field), Array.Empty<long>());
            var changes = CharacterSettlementCalculator.EndTurn(main, input);
            Assert.That(Main(b).Stats.CurrentActionCost, Is.Zero, "Calculation is also the preview path.");
            b.BattleWriteSink.Apply(changes);
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(main.Stats.MaxActionCost));
            Assert.That(Main(b).Stats.RemainingMoveRange, Is.EqualTo(main.Stats.RemainingMoveRange));
        }

        [Test]
        public void RejectedTurnEnd_DoesNotRestoreActionOrChangeEffects()
        {
            var b = Create();
            var main = Main(b);
            var field = b.CombatStateReadSink.SoulField;
            var stale = CharacterBufferTestData.WithSnapshot(main,
                new HealthStateSnapshot(6, main.Health.MaxHealth));
            var bad = new CharacterChanges(new[] { new CharacterStateChange(stale, main) },
                Array.Empty<SoulCountChange>());
            var changes = CharacterSettlementCalculator.EndTurn(main,
                new SoulActionChanges(bad, new SoulFieldChange(field, field), Array.Empty<long>()));
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(changes));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.Zero);
            Assert.That(b.CombatStateReadSink.SoulField, Is.SameAs(field));
        }

        [Test]
        public void TurnSettlementDeath_DoesNotRestoreOrRecreateActor()
        {
            var b = Create();
            var main = Main(b);
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var death = CharacterSettlementCalculator.SetHealth(main, persistent, 0);
            var field = b.CombatStateReadSink.SoulField;
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.EndTurn(main,
                new SoulActionChanges(death, new SoulFieldChange(field, field), Array.Empty<long>())));
            Assert.That(Main(b).Health.CurrentHealth, Is.Zero);
            Assert.That(Main(b).Stats.CurrentActionCost, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TurnCompletion_PublishesDeathAndOutcomeTogether_OrRejectsEverything(bool reject)
        {
            var b = Create();
            var main = Main(b);
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var field = b.CombatStateReadSink.SoulField;
            var turn = CharacterSettlementCalculator.EndTurn(main, new SoulActionChanges(
                CharacterSettlementCalculator.SetHealth(main, persistent, 0),
                new SoulFieldChange(field, field), Array.Empty<long>()));
            var completion = new BattleCompletionCalculator(Chalice()).Complete(main, persistent,
                new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.PlayerMainCharacterDied),
                ChapterProgressionResult.Defeat(1, new StageNodeId(reject ? 3 : 2), new StageKey(10)), turn);
            if (reject)
            {
                Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.CompleteBattle(completion));
                Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
                Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            }
            else
            {
                b.BattleWriteSink.CompleteBattle(completion);
                Assert.That(Main(b).Health.CurrentHealth, Is.Zero);
                Assert.That(b.CombatStateReadSink.Completion.Outcome.Reason, Is.EqualTo(StageOutcomeReason.PlayerMainCharacterDied));
                Assert.That(b.BattleWriteSink.CompleteBattle(completion), Is.False);
            }
            Assert.That(Main(b).Stats.CurrentActionCost, Is.Zero);
            Assert.That(b.CombatStateReadSink.SoulField, Is.SameAs(field));
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.False);
        }

        [Test]
        public void DuplicateCompletion_HealsOnceAndLeavesRosterUntouched()
        {
            var b = Create();
            var calculator = new BattleCompletionCalculator(Chalice());
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var decision = calculator.Complete(Main(b), persistent,
                Victory(), ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true));
            Assert.That(b.BattleWriteSink.CompleteBattle(decision), Is.True);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(b.BattleWriteSink.CompleteBattle(decision), Is.False);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(b.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            b.BattleWriteSink.EndBattle();
            Assert.That(b.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(b.CharacterReadSink.Owned.Count, Is.EqualTo(1));
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.True);
        }

        [Test]
        public void VoluntaryExit_RecordsDefeatOnce()
        {
            var b = Create();
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var decision = new BattleCompletionCalculator(Chalice()).Abandon(Main(b), persistent,
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(10)));
            Assert.That(b.BattleWriteSink.CompleteBattle(decision), Is.True);
            Assert.That(b.BattleWriteSink.CompleteBattle(decision), Is.False);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            Assert.That(b.CombatStateReadSink.Completion.Outcome.Reason, Is.EqualTo(StageOutcomeReason.VoluntaryAbandonment));
        }

        [Test]
        public void InvalidCompletion_ChangesNeitherHealthNorProgress()
        {
            var b = Create();
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var decision = new BattleCompletionCalculator(Chalice()).Complete(Main(b), persistent,
                Victory(), ChapterProgressionResult.FinishStage(1, new StageNodeId(3), new StageKey(20), false));
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.CompleteBattle(decision));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.False);
        }

        [Test]
        public void Recovery_IsOneUseAndPaysAndHealsTogether()
        {
            var b = Create();
            b.BattleWriteSink.EndBattle();
            b.CurrencyWriteSink.SetAmount(8);
            b.CurrentProgressWriteSink.SetRecoveryOptions(new[]
            {
                new RecoveryOptionSpec(new RecoveryOptionId(1), 3, new RecoveryHealSpec(RecoveryHealMode.Full, 0)),
                new RecoveryOptionSpec(new RecoveryOptionId(2), 1, new RecoveryHealSpec(RecoveryHealMode.Flat, 1))
            });
            b.CharacterReadSink.TryGetMainState(out var before);
            var option = b.CurrentProgressReadSink.Recovery.Options[0];
            var decision = CharacterRecoveryCalculator.Plan(before, MainOwnedId(b), 0, option, 8);
            Assert.That(b.RecoveryStageWriteSink.PublishRecovery(new StageNodeId(2), new StageKey(10), decision), Is.True);
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(5));
            b.CharacterReadSink.TryGetMainState(out var persistent);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(10));
            Assert.That(b.RecoveryStageWriteSink.PublishRecovery(new StageNodeId(2), new StageKey(10), decision), Is.False);
            var other = CharacterRecoveryCalculator.Plan(before, MainOwnedId(b), 0, b.CurrentProgressReadSink.Recovery.Options[1], 8);
            Assert.Throws<InvalidOperationException>(() => b.RecoveryStageWriteSink.PublishRecovery(
                new StageNodeId(2), new StageKey(10), other));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(5));
        }

        private static CharacterCombatFacts Main(IBufferQuery b) =>
            b.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Main).ToFacts();

        [Test]
        public void StaleCompletion_ChangesNeitherHealthNorProgressNorReceipt()
        {
            var b = Create();
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var completion = new BattleCompletionCalculator(Chalice()).Complete(Main(b), persistent,
                Victory(), ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true));
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(Main(b), persistent, 4));
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.CompleteBattle(completion));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.False);
        }

        [Test]
        public void Recovery_StaleCurrencyChangesNeitherHealthNorReceipt()
        {
            var b = Create();
            b.BattleWriteSink.EndBattle();
            b.CurrencyWriteSink.SetAmount(8);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), 3, new RecoveryHealSpec(RecoveryHealMode.Full, 0));
            b.CurrentProgressWriteSink.SetRecoveryOptions(new[] { option });
            b.CharacterReadSink.TryGetMainState(out var main);
            var recovery = CharacterRecoveryCalculator.Plan(main, MainOwnedId(b), 0, option, 8);
            b.CurrencyWriteSink.SetAmount(2);
            Assert.Throws<InvalidOperationException>(() => b.RecoveryStageWriteSink.PublishRecovery(new StageNodeId(2), new StageKey(10), recovery));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(2));
            Assert.That(b.CurrentProgressReadSink.Recovery.AppliedOptionId, Is.Null);
            b.CharacterReadSink.TryGetMainState(out var final);
            Assert.That(final.CurrentHealth, Is.EqualTo(7));
        }

        [TestCase(RecoveryHealMode.Full, 0, 10)]
        [TestCase(RecoveryHealMode.Flat, 2, 2)]
        [TestCase(RecoveryHealMode.PercentOfMax, 25, 4)]
        public void Recovery_UsesSoulAdjustedMaxAndExistingRounding(RecoveryHealMode mode, int value, int healed)
        {
            var result = CharacterRecoveryCalculator.Plan(CharacterBufferTestData.MainState(7, 15), new CharacterInstanceId(1), 2,
                new RecoveryOptionSpec(new RecoveryOptionId(1), 3, new RecoveryHealSpec(mode, value)), 8);
            Assert.That(result.HealedAmount, Is.EqualTo(healed));
            Assert.That(result.Characters.Main.Final.PersistentMaxHealth, Is.EqualTo(15));
        }

        [Test]
        public void Outcome_MainDeathTakesPriorityOverDestroyedEnemyHeadquarters()
        {
            var b = Create();
            var deadMain = CharacterBufferTestData.WithHealth(Main(b), 0);
            var headquarters = new StructureCombatFacts(new EntityId(2),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0),
                new HealthStateSnapshot(0, 20), new CellCoord(2, 4), Rotation4.R0, new[] { new CellCoord(2, 4) });
            var outcome = BattleOutcomeCalculator.Resolve(new[] { deadMain }, new[] { headquarters });
            Assert.That(outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(outcome.Reason, Is.EqualTo(StageOutcomeReason.PlayerMainCharacterDied));
        }
        private static CharacterChanges Empty() => new(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>());
        private static StageOutcomeSnapshot Victory() => new(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed);
        private static CharacterInstanceId MainOwnedId(IBufferQuery b) =>
            b.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id;

        internal static IBufferQuery Create()
        {
            var root = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(root);
            var b = root.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            b.MapProgressWriteSink.RegisterMap(1);
            b.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(2, 10, 1, 0, Array.Empty<int>(), false)
            });
            b.CurrentProgressWriteSink.SelectMap(1);
            b.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(10), "Battle");
            var state = CharacterBufferTestData.MainState(7);
            var main = b.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, state).Acquired.Single();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(main.Id, state)
            }, CharacterBufferTestData.Combat()));
            return b;
        }

        [Test]
        public void MultipleChalices_SettleOnceAndPersistHealedHealthAcrossCleanupAndNextPreparation()
        {
            var b = Create();
            b.CharacterReadSink.TryGetMainState(out var before);
            var persistent = CharacterBufferTestData.MainState(7, 15);
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetPersistentMain(Main(b), before, persistent));
            var completion = new BattleCompletionCalculator(Chalice(2)).Complete(Main(b), persistent,
                Victory(), ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true));
            Assert.That(b.BattleWriteSink.CompleteBattle(completion), Is.True);
            Assert.That(b.BattleWriteSink.CompleteBattle(completion), Is.False);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(13));
            b.CombatViewWriteSink.AcknowledgePresented(Math.Max(1, b.CombatStateReadSink.LatestResultSequence));
            b.BattleWriteSink.EndBattle();
            b.CharacterReadSink.TryGetMainState(out var final);
            Assert.That(final.CurrentHealth, Is.EqualTo(13));
            Assert.That(b.CombatStateReadSink.Completion, Is.SameAs(completion));
            b.BattleWriteSink.PrepareBattle(BattlePreparationCalculator.Prepare(
                CharacterBattlePreparationTests.Initialization(), b.CharacterReadSink.Owned, final,
                new System.Collections.Generic.Dictionary<CharacterInstanceId, int> { [new CharacterInstanceId(1)] = 0 },
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>()));
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(13, 15)));
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.True);
        }

        private static ArtifactLoadoutSnapshot Chalice(int count = 1)
        {
            var data = new ArtifactEffectData
            {
                entryId = "chalice", trigger = ArtifactTriggerType.StageCleared,
                source = new ArtifactSourceData { faction = ArtifactFactionFilter.Any },
                target = new ArtifactTargetData { type = ArtifactTargetType.MainCharacter, faction = ArtifactFactionFilter.Ally },
                effect = new ArtifactEffectPayloadData { type = ArtifactEffectType.Heal },
                value = new ArtifactValueData { amount = 3 }
            };
            return new ArtifactLoadoutSnapshot(0, Enumerable.Range(1, count)
                .Select(id => new ArtifactPlacedEffectSnapshot(id, 1, data)).ToArray());
        }
    }
}
