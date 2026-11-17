using System;
using System.Reflection;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PlayerSkillPreviewPublicationTests
    {
        [Test]
        public void UnknownPreviewKindDoesNotFallThroughToPlacement()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var b = fixture.Battle;
            var unknown = (CombatViewPreviewData)Activator.CreateInstance(typeof(CombatViewPreviewData), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { (CombatViewPreviewKind)999, b.Main.EntityId, default(CellCoord), default(Rotation4), default(AttackAim), null, null, null }, null);
            b.Buffers.CombatViewWriteSink.SetPreview(unknown);
            Assert.Throws<ArgumentOutOfRangeException>(() => SkillTransport.Preview(b));
        }

        [Test]
        public void KnownInformationCalculationCannotBePublishedAsAnExecution()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var input = fixture.Input();
            var projected = (CombatCommandInput)typeof(CombatCommandInput).GetMethod("ForSkillForecast", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(input, new object[] { input.PlayerSkillRequest });
            var before = new CoreIntegrationSnapshot(fixture.Battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => fixture.Publisher.Publish(fixture.Cast(projected)));
            before.AssertUnchanged(fixture.Battle.Buffers);
        }

        [Test]
        public void AResponseCalculatedBeforeARealMoveCannotBeInstalledAfterIt()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var b = fixture.Battle;
            var payload = SkillTransport.Query<PlayerSkillAvailability>(b, "ForPlayerSkillAvailability", "SkillAvailability", b.Main.EntityId);
            int number = b.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ForPlayerSkillAvailability(b.Main.EntityId));
            var response = CombatPreviewResult.ForSkillAvailability(number, payload);
            fixture.Move(b.Main.EntityId, new[] { CellCoord.Zero, new CellCoord(1, 0) });
            Assert.Throws<InvalidOperationException>(() => b.Buffers.BattleWriteSink.PublishPreview(response));
        }

        [Test]
        public void WrongResponseArmAndSelectedPointsAreRejected()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var b = fixture.Battle;
            var payload = SkillTransport.Query<PlayerSkillAvailability>(b, "ForPlayerSkillAvailability", "SkillAvailability", b.Main.EntityId);
            int number = b.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ForPlayerSkillSelection(b.Main.EntityId, Array.Empty<CellCoord>()));
            Assert.Throws<InvalidOperationException>(() => b.Buffers.BattleWriteSink.PublishPreview(CombatPreviewResult.ForSkillAvailability(number, payload)));
            var wrong = new PlayerSkillSelectionResult(payload.Source, new[] { CellCoord.Zero }, 0, Array.Empty<CellCoord>(), PlayerSkillFailureReason.WrongPointCount);
            Assert.Throws<InvalidOperationException>(() => b.Buffers.BattleWriteSink.PublishPreview(CombatPreviewResult.ForSkillSelection(number, wrong)));
        }

        [TestCase("revision")]
        [TestCase("turn")]
        [TestCase("battle")]
        [TestCase("actor")]
        public void BufferRejectsWrongSourceBeforeInstallingResponse(string mutation)
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var b = fixture.Battle;
            var source = SkillTransport.Query<PlayerSkillAvailability>(b, "ForPlayerSkillAvailability", "SkillAvailability", b.Main.EntityId).Source;
            int number = b.Buffers.CombatViewWriteSink.SetPreview(SkillTransport.Invoke<CombatViewPreviewData>(typeof(CombatViewPreviewData),
                "ForPlayerSkillAvailability", b.Main.EntityId));
            var changed = new PlayerSkillSourceStamp(source.BattleInstanceId + (mutation == "battle" ? 1 : 0),
                source.ControllerTurnSequence + (mutation == "turn" ? 1 : 0), source.QueryRevision + (mutation == "revision" ? 1 : 0),
                mutation == "actor" ? b.Actor.EntityId : source.ActorId);
            var payload = new PlayerSkillAvailability(changed, b.Buffers.CombatStateReadSink.Current.CommandState.PlayerSkill, false, PlayerSkillFailureReason.None);
            var response = SkillTransport.Invoke<CombatPreviewResult>(typeof(CombatPreviewResult), "ForSkillAvailability", number, payload);
            Assert.Throws<InvalidOperationException>(() => b.Buffers.BattleWriteSink.PublishPreview(response));
        }

        [Test]
        public void ReplacementAndClearPreviewRespectRequestOwnership()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var b = fixture.Battle;
            SkillTransport.Query<PlayerSkillAvailability>(b, "ForPlayerSkillAvailability", "SkillAvailability", b.Main.EntityId);
            Assert.That(b.Buffers.CombatStateReadSink.TryGetLatestPreview(out var old), Is.True);
            int next = b.Buffers.CombatViewWriteSink.SetPreview(SkillTransport.Invoke<CombatViewPreviewData>(typeof(CombatViewPreviewData),
                "ForPlayerSkillForecast", b.Main.EntityId, Array.Empty<CellCoord>()));
            b.Buffers.CombatViewWriteSink.ClearPreview(old.RequestNumber);
            Assert.That(b.Buffers.CombatViewReadSink.Preview.Number, Is.EqualTo(next));
            Assert.Throws<InvalidOperationException>(() => b.Buffers.BattleWriteSink.PublishPreview(old));
            b.Buffers.CombatViewWriteSink.ClearPreview(next);
            Assert.That(b.Core.CombatPreview.ProcessLatestPreview(), Is.False);
        }
    }
}
