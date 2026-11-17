using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PlayerSkillEndToEndTests
    {
        [TestCase(PlayerSkillKey.PowderBlossom, PlayerSkillUpgradeKey.None)]
        [TestCase(PlayerSkillKey.PowderBlossom, PlayerSkillUpgradeKey.Bloom)]
        [TestCase(PlayerSkillKey.PowderBlossom, PlayerSkillUpgradeKey.Condemnation)]
        [TestCase(PlayerSkillKey.MagicBullet, PlayerSkillUpgradeKey.None)]
        [TestCase(PlayerSkillKey.MagicBullet, PlayerSkillUpgradeKey.Distortion)]
        [TestCase(PlayerSkillKey.MagicBullet, PlayerSkillUpgradeKey.Judgment)]
        public void SixFormsTraverseRealServicesAndMatchFullInformationCalculation(PlayerSkillKey key, PlayerSkillUpgradeKey upgrade)
        {
            using var game = Prepare(key, upgrade);
            var available = Availability(game);
            Assert.That(available.CanUse, Is.True);
            var points = Points(key, upgrade);
            for (int i = 0; i <= points.Length; i++)
            {
                var prefix = game.Selection(points.Take(i).ToArray());
                Assert.That(prefix.PrefixIsValid, Is.True, prefix.FailureReason.ToString());
                Assert.That(prefix.IsComplete, Is.EqualTo(i == points.Length));
                if (i < points.Length) Assert.That(prefix.NextCandidateCells, Has.Member(points[i]));
            }
            var state = game.Buffers.CombatStateReadSink.Current.CommandState;
            var forecast = game.Forecast(points);
            Assert.That(forecast.CanUse, Is.True, forecast.FailureReason.ToString());
            Assert.That(forecast.Coverage, Is.EqualTo(PlayerSkillForecastCoverage.KnownInformationConditional));
            EqualFacts(game.Forecast(points), forecast);
            Assert.That(game.Buffers.CombatStateReadSink.Current.CommandState, Is.SameAs(state));
            int sequence = game.Buffers.CombatStateReadSink.LatestResultSequence;
            var command = CombatViewCommandData.ForPlayerSkill(game.Skill.ActorId, points, forecast.Source);
            int number = game.Buffers.CombatViewWriteSink.Submit(command);
            // 운영 서비스가 실제로 쓰는 조립기를 관찰하여 같은 번호·원본·난수의 내부 계산을 비교한다.
            var assembler = (CombatInputAssembler)typeof(CombatCommandService).GetField("_assembler", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game.Core.CombatCommands);
            var input = assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.UsePlayerSkill, game.Skill.ActorId, default,
                playerSkill: command.PlayerSkill, expectedBattleInstanceId: forecast.Source.BattleInstanceId,
                expectedControllerTurnSequence: forecast.Source.ControllerTurnSequence));
            var local = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(local.Accepted, Is.True, local.Reason);
            CombatHealthEvidenceValidator.Validate(local);
            Assert.That(local.HealthClaims.All(c => local.Events.Any(e => ReferenceEquals(e, c.Event))), Is.True);
            Assert.That(game.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(game.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
            EqualFacts(result.Events, local.Events);
            EqualFacts(result.PlayerSkill, local.PlayerSkill);
            EqualFacts(result.Outcome, local.Outcome);
            Assert.That(game.Buffers.CombatStateReadSink.Current.CommandState.SameGameplayValues(local.FinalState), Is.True);
            foreach (var change in local.Characters.Characters)
                Assert.That(game.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == change.Final.EntityId).ToFacts().SameValues(change.Final), Is.True);
            Assert.That(result.ActionUseSettlement, Is.Null);
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatPlayerSkillAttackEvent>().SelectMany(e => e.Attack.Hits), Is.Not.Empty);
            if (upgrade == PlayerSkillUpgradeKey.Condemnation)
                Assert.That(local.Characters.Characters.Any(c => c.Final.StatusEffects.Entries.Any(s => s.Key == StatusEffectKey.Stun)), Is.True);
            if (upgrade == PlayerSkillUpgradeKey.Judgment)
                Assert.That(result.Events.OfType<CombatPlayerSkillAttackEvent>().Single().Attack.Hits.Count, Is.EqualTo(1));
            game.Buffers.CombatViewWriteSink.AcknowledgePresented(result.Sequence);
            Assert.That(game.Buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
            Assert.That(game.Core.CombatCommands.ProcessNextCommand(), Is.False);
        }

        [TestCase(PlayerSkillKey.PowderBlossom)]
        [TestCase(PlayerSkillKey.MagicBullet)]
        public void EmptyCastStillPublishesAndConsumesExactlyOnce(PlayerSkillKey key)
        {
            using var game = Prepare(key, PlayerSkillUpgradeKey.None, empty: true);
            var points = Points(key, PlayerSkillUpgradeKey.None);
            var preview = game.Forecast(points);
            var result = game.Submit(CombatViewCommandData.ForPlayerSkill(game.Skill.ActorId, points, preview.Source));
            Assert.That(result.PlayerSkill.Events.OfType<CombatPlayerSkillAttackEvent>().SelectMany(e => e.Attack.Hits), Is.Empty);
            Assert.That(result.PlayerSkill.UseSettlement.FinalRemainingUses, Is.EqualTo(1));
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(1));
        }

        [Test]
        public void BloomChainStopsWhenItsExpandedBurstEndsTheBattle()
        {
            using var game = Prepare(PlayerSkillKey.PowderBlossom, PlayerSkillUpgradeKey.Bloom, terminal: true);
            var preview = game.Forecast();
            Assert.That(preview.CanUse, Is.True);
            var result = game.Submit(CombatViewCommandData.ForPlayerSkill(game.Skill.ActorId, Array.Empty<CellCoord>(), preview.Source));
            Assert.That(result.PlayerSkill.Events.OfType<CombatPlayerSkillAttackEvent>().Count(), Is.EqualTo(2));
            Assert.That(result.Outcome.HasValue, Is.True);
            Assert.That(game.Buffers.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(result.PlayerSkill.UseSettlement.FinalRemainingUses, Is.EqualTo(1));
            game.Buffers.CombatViewWriteSink.AcknowledgePresented(result.Sequence);
        }

        [Test]
        public void StaleCastCompletesBookkeepingWithoutChangingGameplayAndCannotCancelAnotherPreview()
        {
            using var game = Prepare(PlayerSkillKey.PowderBlossom, PlayerSkillUpgradeKey.None);
            var source = game.Forecast().Source;
            int oldNumber = game.Buffers.CombatViewReadSink.Preview.Number;
            int currentNumber = game.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ForPlayerSkillAvailability(game.Skill.ActorId));
            Assert.That(game.Buffers.CombatViewWriteSink.ClearPreview(oldNumber), Is.False);
            Assert.That(game.Buffers.CombatViewReadSink.Preview.Number, Is.EqualTo(currentNumber));
            var stale = new PlayerSkillSourceStamp(source.BattleInstanceId, source.ControllerTurnSequence, source.QueryRevision + 1, source.ActorId);
            var before = CoreIntegrationSnapshot.CaptureGameplay(game.Buffers);
            int number = game.Buffers.CombatViewWriteSink.Submit(CombatViewCommandData.ForPlayerSkill(game.Skill.ActorId, Array.Empty<CellCoord>(), stale));
            int sequence = game.Buffers.CombatStateReadSink.LatestResultSequence;
            Assert.That(game.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(game.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(nameof(PlayerSkillFailureReason.SourceChanged)));
            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
            Assert.That(game.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(number));
            EqualFacts(CoreIntegrationSnapshot.CaptureGameplay(game.Buffers), before);
            Assert.That(game.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(game.Buffers.CombatViewReadSink.Preview.Number, Is.EqualTo(currentNumber));
        }

        private static PlayerSkillAvailability Availability(PlayerSkillRuntimeFixture game)
        {
            game.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ForPlayerSkillAvailability(game.Skill.ActorId));
            Assert.That(game.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(game.Buffers.CombatStateReadSink.TryGetLatestPreview(out var result), Is.True);
            return result.SkillAvailability;
        }

        private static CellCoord[] Points(PlayerSkillKey key, PlayerSkillUpgradeKey upgrade) => key == PlayerSkillKey.PowderBlossom
            ? Array.Empty<CellCoord>() : upgrade == PlayerSkillUpgradeKey.Distortion
                ? new[] { new CellCoord(3, 1), new CellCoord(3, 2), new CellCoord(4, 2) }
                : new[] { new CellCoord(3, 1), new CellCoord(3, 2) };

        private static PlayerSkillRuntimeFixture Prepare(PlayerSkillKey key, PlayerSkillUpgradeKey upgrade, bool empty = false, bool terminal = false)
        {
            // 행동 검증용 결합이다. 운영 Akimbo/Sniper 자격은 별도의 실제 카탈로그 시험이 검증한다.
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config, headquartersHealth: terminal ? 5 : 100)
                { PlayerActiveSkills = new SkillTable(key) };
            database.SetTargetingMethod(new TargetingMethodData(1, TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var traits = key == PlayerSkillKey.MagicBullet ? new UnitTraitData[] { new PiercingFalloffTraitData(25, 4) }
                : terminal ? Array.Empty<UnitTraitData>() : new UnitTraitData[] { new KillChainAttackTraitData() };
            database.SetUnit(new UnitData(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                1, 1, 100, 0, 4, CategoryKey.Main, 20, 1, traits, 1, killReward: UnitKillRewardSpec.Excluded));
            database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 6, 0, 4, CategoryKey.Main, 1, 1, Array.Empty<UnitTraitData>(), 1, killReward: UnitKillRewardSpec.Excluded));
            database.SetUnit(new UnitData(EntityKey.Unit_Cowboy, UnitRole.Regular, UnitOwnerEligibility.Both,
                1, 1, 100, 0, 4, CategoryKey.Rider, 1, 1, Array.Empty<UnitTraitData>(), 1, killReward: UnitKillRewardSpec.Excluded));
            var enemies = empty ? new[] { new EntityPlacement(EntityKey.Unit_Cowboy, new CellCoord(6, 4), Rotation4.R180) }
                : terminal ? new[] { new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(2, 1), Rotation4.R180) }
                : new[] { new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(2, 1), Rotation4.R180),
                    new EntityPlacement(EntityKey.Unit_Cowboy, new CellCoord(3, 1), Rotation4.R180) };
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1, enemies, Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(),
                    EntityKey.Structure_Headquarters, new CellCoord(1, 1), Rotation4.R0,
                    terminal ? new CellCoord(3, 1) : new CellCoord(7, 5), Rotation4.R180, null,
                    playerDeploymentCells: new[] { new CellCoord(1, 1) })));
            var game = new PlayerSkillRuntimeFixture(database);
            try
            {
                if (upgrade != PlayerSkillUpgradeKey.None)
                {
                    var available = game.Core.Characters.ReadPlayerSkillUpgradeAvailability();
                    Assert.That(game.Core.Characters.TryApplyPlayerSkillUpgrade(available.RunId, available.RosterRevision,
                        available.MainId, upgrade).Succeeded, Is.True);
                }
                game.Enter(2);
                game.DeployMain(new CellCoord(1, 1));
                return game;
            }
            catch { game.Dispose(); throw; }
        }

        private sealed class SkillTable : IPlayerActiveSkillTableQuery
        {
            private readonly PlayerActiveSkillData _data;
            internal SkillTable(PlayerSkillKey key) => _data = new PlayerActiveSkillData(new PlayerSkillDefinition(key, EntityKey.Unit_Akimbo, 2));
            public PlayerActiveSkillData GetPlayerSkill(PlayerSkillKey key) => TryGetPlayerSkill(key, out var data) ? data : throw new KeyNotFoundException();
            public bool TryGetPlayerSkill(PlayerSkillKey key, out PlayerActiveSkillData data) { data = key == _data.Definition.Key ? _data : null; return data != null; }
            public bool TryGetPlayerSkillForMain(EntityKey key, out PlayerActiveSkillData data) { data = key == EntityKey.Unit_Akimbo ? _data : null; return data != null; }
        }

        private static void EqualFacts(object actual, object expected, string path = "facts", int depth = 0)
        {
            Assert.That(depth, Is.LessThan(100), path);
            if (actual == null || expected == null) { Assert.That(actual, Is.EqualTo(expected), path); return; }
            var type = actual.GetType();
            Assert.That(type, Is.EqualTo(expected.GetType()), path);
            if (type.IsPrimitive || type.IsEnum || actual is string || actual is Guid || actual is decimal)
            { Assert.That(actual, Is.EqualTo(expected), path); return; }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>))
            { EqualFacts(type.GetMethod("ToArray").Invoke(actual, null), type.GetMethod("ToArray").Invoke(expected, null), path, depth + 1); return; }
            if (actual is IEnumerable sequence)
            {
                var a = sequence.Cast<object>().ToArray(); var b = ((IEnumerable)expected).Cast<object>().ToArray();
                Assert.That(a.Length, Is.EqualTo(b.Length), path);
                for (int i = 0; i < a.Length; i++) EqualFacts(a[i], b[i], path + "[" + i + "]", depth + 1);
                return;
            }
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
                EqualFacts(property.GetValue(actual), property.GetValue(expected), path + "." + property.Name, depth + 1);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                EqualFacts(field.GetValue(actual), field.GetValue(expected), path + "." + field.Name, depth + 1);
        }
    }
}
