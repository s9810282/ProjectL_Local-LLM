using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ReconSquadAuthoringTests
    {
        [TestCase(10, 1)]
        [TestCase(25, 2)]
        [TestCase(0, 3)]
        public void DefaultAndConfiguredValues_RoundTripIndependently(int percent, int radius)
        {
            var type = TypeCache.GetTypesDerivedFrom<UnitTraitAuthoring>().Single(t => t.Name == "ReconSquadTraitAuthoring");
            var baseline = (UnitTraitAuthoring)Activator.CreateInstance(type);
            var configured = (UnitTraitAuthoring)Activator.CreateInstance(type);
            type.GetField("increasePercent", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(configured, percent);
            type.GetField("radius", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(configured, radius);
            var spec = (ReconSquadTraitSpec)TraitSpecAdapter.Read(configured.ToData());
            var normal = (ReconSquadTraitSpec)TraitSpecAdapter.Read(baseline.ToData());
            Assert.That((normal.IncreasePercent, normal.Radius), Is.EqualTo((10, 1)));
            Assert.That((spec.IncreasePercent, spec.Radius), Is.EqualTo((percent, radius)));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HuntersMarkBattleSource(spec));
            var status = battle.Actor.Stats.TraitStatuses.ToArray().OfType<ReconSquadTraitStatusSnapshot>().Single();
            Assert.That((status.IncreasePercent, status.Radius), Is.EqualTo((percent, radius)));
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var profile = input.Profiles.Single(p => p.OwnedId == battle.Actor.OwnedId);
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(91), profile.Definition.Key, CharacterOwnershipRole.Sub);
            var read = OwnedCharacterStatsCalculator.Calculate(new[] { profile },
                UnitSpecAdapter.ReadCategories(ConfiguredMovementBattle.Config), new[] { owned }, null,
                new Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 }).Single();
            Assert.That((read.ReconSquadDescription.IncreasePercent, read.ReconSquadDescription.Radius), Is.EqualTo((percent, radius)));
            Assert.That(read.TraitAvailability.Single().UnavailableReason, Is.Empty);
        }

        [TestCase(-1, 1)]
        [TestCase(10, 0)]
        public void InvalidConfiguration_RejectsAtAuthoringBoundary(int percent, int radius)
        {
            var data = new ReconSquadTraitData(percent, radius);
            Assert.Throws<InvalidOperationException>(() => data.Validate());
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data));
        }
    }
}
