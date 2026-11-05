using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using NUnit.Framework;
using UnityEditor;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PositionSwapAuthoringTests
    {
        [Test]
        public void AuthoringAdapterAndRuntime_RoundTripIntoExchangeWithoutExtraMovementSettings()
        {
            var type = TypeCache.GetTypesDerivedFrom<UnitTraitAuthoring>().Single(t => t.Name == "PositionSwapTraitAuthoring");
            var authoring = (UnitTraitAuthoring)Activator.CreateInstance(type);
            var spec = (PositionSwapTraitSpec)TraitSpecAdapter.Read(authoring.ToData());
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HuntersMarkBattleSource(spec));
            Assert.That(battle.Actor.Stats.TraitStatuses.ToArray().Single(), Is.TypeOf<PositionSwapTraitStatusSnapshot>());
            var request = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 0));
            Assert.That(request.MovementContext.PositionSwapPassenger.EntityId, Is.EqualTo(battle.Character(EntityKey.Unit_DynamiteHammer).EntityId));
            battle.Submit(request);
            Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(2, 0)));
        }
    }
}
