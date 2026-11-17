using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class GimmickOccupancyPublicationTests
    {
        [Test]
        public void HeadRejectsDeploymentWithoutChangingCharacterPosition()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new HaboobPublicationTests.Source(new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.LavaChainGimmickSpec(
                    new CellCoord(3, 1), 3, 1, 1) }), enterCombat: false);
            var before = battle.Actor.Anchor;
            battle.Buffers.CombatViewWriteSink.Submit(CombatViewCommandData.PlaceUnit(battle.Actor.EntityId, new CellCoord(3, 1), Rotation4.R0));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Actor.Anchor, Is.EqualTo(before));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
        }

        [Test]
        public void InitialCharacterOverlapIsRejectedByPreparation()
        {
            Assert.Throws<InvalidOperationException>(() => new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new LavaChainPublicationTests.FixedSource(overlap: true)));
        }

        [Test]
        public void RetractionReleasesOldHeadAndOnlyFinalHeadBlocks()
        {
            using var battle = LavaChainPublicationTests.Open();
            var oldHead = LavaChainPublicationTests.Read(battle).HeadCell;
            LavaChainPublicationTests.Reach(battle, 3);
            var state = LavaChainPublicationTests.Current(battle);
            var final = LavaChainPublicationTests.Read(battle).HeadCell;
            Assert.That(final, Is.Not.EqualTo(oldHead));
            var input = HaboobPublicationTests.EndInput(battle);
            Assert.That(CellEnvironmentQuery.Read(state, input.Characters, input.EnvironmentBindings, oldHead).IsGimmickBlocked, Is.False);
            Assert.That(CellEnvironmentQuery.Read(state, input.Characters, input.EnvironmentBindings, final).IsGimmickBlocked, Is.True);
        }
    }
}
