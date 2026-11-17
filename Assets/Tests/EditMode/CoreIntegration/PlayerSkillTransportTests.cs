using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PlayerSkillTransportTests
    {
        [Test]
        public void MagicBulletPointsRoundTripAndExecutionUsesTheSameServicePath()
        {
            using var fixture = new PlayerSkillPublicationFixture(skillKey: PlayerSkillKey.MagicBullet, upgrade: PlayerSkillUpgradeKey.Judgment);
            var b = fixture.Battle;
            var a = new CellCoord(1, 0);
            var points = new[] { a, new CellCoord(1, 1) };
            var query = CombatViewPreviewData.ForPlayerSkillForecast(b.Main.EntityId, points);
            points[0] = new CellCoord(99, 99);
            Assert.That(query.PlayerSkill.Points[0], Is.EqualTo(a));
            b.Buffers.CombatViewWriteSink.SetPreview(query);
            Assert.That(SkillTransport.Preview(b), Is.True);
            Assert.That(b.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.SkillForecast.CanUse, Is.True);
            var command = CombatViewCommandData.ForPlayerSkill(b.Main.EntityId, query.PlayerSkill.Points, preview.SkillForecast.Source);
            b.Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(SkillTransport.Execute(b), Is.True);
            Assert.That(b.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PlayerSkill.SkillKey, Is.EqualTo(PlayerSkillKey.MagicBullet));
            Assert.That(result.PlayerSkill.UpgradeKey, Is.EqualTo(PlayerSkillUpgradeKey.Judgment));
            Assert.That(result.PlayerSkill.UseSettlement.FinalRemainingUses, Is.EqualTo(1));
        }

        [Test]
        public void RealServiceQueriesForecastsSubmitsAndAcknowledgesOneResult()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var b = fixture.Battle;
            fixture.Move(b.Main.EntityId, new[] { CellCoord.Zero, new CellCoord(1, 0), new CellCoord(1, 1), new CellCoord(1, 2) });
            var actionUse = b.Main.ActionUse;
            var source = SkillTransport.Query<PlayerSkillAvailability>(b, "ForPlayerSkillAvailability", "SkillAvailability", b.Main.EntityId).Source;
            var selection = SkillTransport.Query<PlayerSkillSelectionResult>(b, "ForPlayerSkillSelection", "SkillSelection", b.Main.EntityId, Array.Empty<CellCoord>());
            Assert.That(selection.IsComplete, Is.True);
            var forecast = SkillTransport.Query<PlayerSkillForecast>(b, "ForPlayerSkillForecast", "SkillForecast", b.Main.EntityId, Array.Empty<CellCoord>());
            Assert.That(forecast.CanUse, Is.True, forecast.FailureReason.ToString());
            var before = b.Buffers.CombatStateReadSink.LatestResultSequence;
            b.Buffers.CombatViewWriteSink.Submit(SkillTransport.Command(b, source));
            Assert.That(SkillTransport.Execute(b), Is.True);
            Assert.That(b.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(before + 1));
            Assert.That(b.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.PlayerSkill.UseSettlement.FinalRemainingUses, Is.EqualTo(forecast.Prediction.UseSettlement.FinalRemainingUses));
            Assert.That(b.Main.ActionUse, Is.EqualTo(actionUse));
            var expectedHits = forecast.Prediction.Events.OfType<CombatPlayerSkillAttackEvent>().SelectMany(e => e.Attack.Hits).ToArray();
            var actualHits = result.PlayerSkill.Events.OfType<CombatPlayerSkillAttackEvent>().SelectMany(e => e.Attack.Hits).ToArray();
            Assert.That(actualHits, Is.Not.Empty);
            Assert.That(actualHits.Select(h => (h.TargetId, h.TargetHealthBefore, h.TargetHealthAfter, h.DamageDealt)),
                Is.EqualTo(expectedHits.Select(h => (h.TargetId, h.TargetHealthBefore, h.TargetHealthAfter, h.DamageDealt))));
            Assert.That(result.Events.Select(e => e.Kind), Is.EqualTo(forecast.Prediction.Events.Select(e => e.Kind)));
            Assert.That(SkillTransport.Execute(b), Is.False);
            b.Buffers.CombatViewWriteSink.AcknowledgePresented(before + 1);
        }

        [Test]
        public void StaleSubmissionRejectsWithoutUsageOrRngChanges()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var b = fixture.Battle;
            var source = SkillTransport.Query<PlayerSkillAvailability>(b, "ForPlayerSkillAvailability", "SkillAvailability", b.Main.EntityId).Source;
            var stale = new PlayerSkillSourceStamp(source.BattleInstanceId, source.ControllerTurnSequence, source.QueryRevision + 1, source.ActorId);
            var before = b.Buffers.CombatStateReadSink.Current.CommandState;
            b.Buffers.CombatViewWriteSink.Submit(SkillTransport.Command(b, stale));
            Assert.That(SkillTransport.Execute(b), Is.True);
            Assert.That(b.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(nameof(PlayerSkillFailureReason.SourceChanged)));
            Assert.That(b.Buffers.CombatStateReadSink.Current.CommandState.SameValues(before), Is.True);
        }
    }

    internal static class SkillTransport
    {
        internal static bool Execute(ConfiguredMovementBattle battle)
        {
            var pipeline = battle.Publication();
            var service = new CombatCommandService(pipeline.Assembler, pipeline.Publisher, Array.Empty<IMovementTileRule>());
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(battle.Buffers);
            service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            return service.ProcessNextCommand();
        }

        internal static bool Preview(ConfiguredMovementBattle battle)
        {
            var service = new CombatPreviewService(battle.Publication().Assembler, Array.Empty<IMovementTileRule>());
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(battle.Buffers);
            service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            return service.ProcessLatestPreview();
        }
        internal static T Invoke<T>(Type type, string method, params object[] args)
        {
            var factory = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.That(factory, Is.Not.Null, method);
            return (T)factory.Invoke(null, args);
        }
        internal static CombatViewCommandData Command(ConfiguredMovementBattle b, PlayerSkillSourceStamp source) =>
            Invoke<CombatViewCommandData>(typeof(CombatViewCommandData), "ForPlayerSkill", b.Main.EntityId, Array.Empty<CellCoord>(), source);
        internal static T Query<T>(ConfiguredMovementBattle b, string factory, string arm, params object[] args)
        {
            b.Buffers.CombatViewWriteSink.SetPreview(Invoke<CombatViewPreviewData>(typeof(CombatViewPreviewData), factory, args));
            Assert.That(Preview(b), Is.True);
            Assert.That(b.Buffers.CombatStateReadSink.TryGetLatestPreview(out var result), Is.True);
            var property = typeof(CombatPreviewResult).GetProperty(arm);
            Assert.That(property, Is.Not.Null, arm);
            return (T)property.GetValue(result);
        }
    }
}
