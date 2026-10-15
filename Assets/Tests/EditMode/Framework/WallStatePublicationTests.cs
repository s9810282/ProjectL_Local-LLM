using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed class WallStatePublicationTests
    {
        [Test]
        public void EndAndFreshEntry_ClearPreviousWallsAndSettings()
        {
            var store = new CombatStateBuffer();
            var write = (ICombatStateBufferStore)store;
            var preparation = Preparation();
            write.InstallBattle(write.PrepareBattle(preparation));
            var original = store.Current;
            Assert.That(original.CommandState.WallBlocking.CornerCutTicks, Is.EqualTo(257));
            write.EndBattle();
            Assert.That(store.HasBattle, Is.False);
            Assert.That(store.Current, Is.Null);
            write.InstallBattle(write.PrepareBattle(Preparation(empty: true)));
            Assert.That(store.Current.CommandState.WallBlocking.CornerCutTicks, Is.EqualTo(128));
            Assert.That(store.Current.Environment.Tiles, Is.Empty);
            Assert.That(original.Environment.Tiles.Single().Id, Is.EqualTo(71));
        }

        [TestCase("settings")]
        [TestCase("move")]
        [TestCase("remove")]
        [TestCase("deactivate")]
        [TestCase("hide")]
        [TestCase("kind")]
        public void CommandPublication_RejectsStaticWallOrSettingMutation(string mutation)
        {
            var store = new CombatStateBuffer();
            var write = (ICombatStateBufferStore)store;
            var preparation = Preparation();
            write.InstallBattle(write.PrepareBattle(preparation));
            var s = store.Current.CommandState;
            var wall = (StaticTerrainFact)s.Environment.Tiles.Single();
            var changed = new StaticTerrainFact(wall.Id,
                mutation == "move" ? new CellCoord(5, 3) : wall.Cell,
                mutation == "kind" ? StaticTerrainKind.HighGround : wall.Kind, mutation != "deactivate",
                mutation == "hide" ? new[] { Owner.Player } : wall.KnownTo);
            var environment = mutation == "remove" ? MovementEnvironmentFacts.Empty :
                new MovementEnvironmentFacts(new[] { changed });
            var final = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, environment, s.EnvironmentProgress,
                mutation == "settings" ? new WallBlockingSpec(128) : s.WallBlocking);
            var input = Input(preparation, s);
            var transition = new CombatCommandTransition(input, true, "",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), final, default);
            Assert.Throws<InvalidOperationException>(() => write.PrepareCommand(transition, preparation.Characters));
            Assert.That(store.Current.CommandState, Is.SameAs(s));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreparedPublication_UsesCoreWallValidation(bool overlap)
        {
            var preparation = Preparation(invalid: true, overlap: overlap);
            Assert.That(() => BattlePreparationValidator.Validate(preparation), Throws.Exception);
        }

        private static BattlePreparation Preparation(bool empty = false, bool invalid = false, bool overlap = false)
        {
            var main = CharacterBufferTestData.MainFacts(new CharacterInstanceId(1), CharacterBufferTestData.MainState());
            var environment = empty ? MovementEnvironmentFacts.Empty : new MovementEnvironmentFacts(new[] {
                new StaticTerrainFact(71, overlap ? main.Anchor.Value : new CellCoord(4, 3), StaticTerrainKind.Wall,
                    true, invalid && !overlap ? new[] { Owner.Player } : new[] { Owner.Player, Owner.Enemy }) });
            var bindings = new EnvironmentSemanticBindings(environment.Tiles.Select(t => t.Id).ToArray(),
                Array.Empty<long>(), Array.Empty<EntityId>());
            return new BattlePreparation(new[] { main }, new CombatPreparation(8, 6, 4, 100, 73,
                CombatFlowPhase.PlayerInput, Array.Empty<CharacterInstanceId>(), environment: environment,
                environmentBindings: bindings, wallBlocking: empty ? WallBlockingSpec.Default : new WallBlockingSpec(257)));
        }

        private static CombatCommandInput Input(BattlePreparation p, CombatCommandState state) =>
            new(1, CombatCommandKind.EndUnitTurn, p.Characters[0].EntityId, default, state, p.Characters,
                Array.Empty<CombatAttackProfile>(), CharacterBufferTestData.MainState(),
                new[] { new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) },
                new Dictionary<CharacterInstanceId, int> { [new CharacterInstanceId(1)] = 0 }, SoulFieldSnapshot.Empty,
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()),
                environmentBindings: p.Combat.EnvironmentBindings);
    }
}
