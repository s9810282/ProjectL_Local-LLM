using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    internal static class NexusReinforcementTestData
    {
        internal static NexusReinforcementSpec Pool(int a = 3, int b = 2, int batch = 2, int padding = 2,
            int limit = NexusReinforcementSpec.DefaultFieldEnemyLimit) =>
            new(new[] { (EntityKey.Unit_Demon, a), (EntityKey.Unit_Cowboy, b) }.Where(row => row.Item2 > 0)
                .Select(row => new NexusReinforcementEntry(row.Item1, row.Item2)).ToArray(), 3, batch, padding, limit);

        internal static CombatCommandInput Input(NexusReinforcementSpec spec, int living = 0,
            CombatControllerPhase phase = CombatControllerPhase.Enemy, uint random = 17,
            CellCoord[] blocked = null, bool presence = false)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var nexus = CellEnvironmentTestData.Structure(30, cells: new[] { new CellCoord(4, 5) });
            var enemies = Enumerable.Range(0, living).Select(x => CombatCommandTestData.Character(x + 40,
                EntityKey.Unit_Demon, new CellCoord(x, 0), Owner.Enemy)).ToArray();
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: phase,
                controllerTurnSequence: 2, enemyOrigins: enemies.Select(c =>
                    new CombatEnemyOrigin(c.EntityId, c.Anchor.Value, c.Rotation.Value)).ToArray());
            var state = seed.State.With(phase: phase == CombatControllerPhase.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,
                structures: new[] { nexus }, turn: turn, allocation: new CombatAllocationFacts(100, 23), itemRandomState: 31,
                reinforcements: spec == null ? NexusReinforcementState.Disabled :
                    new NexusReinforcementState(nexus.EntityId, random, Array.Empty<NexusReinforcementRecord>()));
            var profiles = seed.Profiles.Concat(new[] { new CombatAttackProfile(UnitSpecTestFactory.Create(
                EntityKey.Unit_Cowboy, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 2, 3, 1,
                new TraitSpec[] { new FocusTraitSpec(1, 3) }, categoryKey: CategoryKey.Heavy), UnitSpecTestFactory.DefaultMethod()) }).ToArray();
            var characters = seed.Characters.Concat(enemies).ToArray();
            if (presence)
            {
                var ally = characters[0];
                characters[0] = CombatCommandTestData.With(ally, traits: new TraitStatusSnapshot[] { new TauntTraitStatusSnapshot() });
                profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(ally.CharacterKey, 20, CategoryKey.Main,
                    new TauntTraitSpec(7)), profiles[0].Method);
                int enemy = Array.FindIndex(characters, c => c.EntityId == new EntityId(40));
                if (enemy >= 0)
                {
                    var old = characters[enemy];
                    characters[enemy] = CombatCommandTestData.With(old, traits: new TraitStatusSnapshot[] { new SoloActionTraitStatusSnapshot() });
                    int profile = Array.FindIndex(profiles, p => p.Definition.Key == old.CharacterKey);
                    profiles[profile] = new CombatAttackProfile(CombatCommandTestData.Spec(old.CharacterKey, 1, CategoryKey.Main,
                        new SoloActionTraitSpec(3, 7, 4, 1)), profiles[profile].Method);
                }
            }
            var battle = new CombatBattleFacts(8, 42, PlayerDeploymentZone.Rows(2), Array.Empty<CharacterInstanceId>(), 0,
                railCells: blocked, categories: UnitSpecTestFactory.CreateGrowthCategoryCatalog(), nexusReinforcements: spec);
            return new CombatCommandInput(0, CombatCommandKind.StartControllerTurn, EntityId.Empty, default,
                state, characters, profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts, battle: battle,
                processingKind: CombatProcessingKind.Advance);
        }
    }
}
