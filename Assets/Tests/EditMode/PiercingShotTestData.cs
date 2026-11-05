using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    internal static class PiercingShotTestData
    {
        internal static CombatCommandInput Input(int cap = int.MaxValue, int damage = 10, bool piercing = true, bool emptyAim = false,
            int appointRange = 6)
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var category = emptyAim ? CategoryKey.Cannon : CategoryKey.Main;
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(5, 3)), empty: true, category: category);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo,
                new CellCoord(1, 3), Owner.Player, damage), appointRange: appointRange, category: CategorySnapshot.CreateStateless(category),
                traits: piercing ? new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) } : Array.Empty<TraitStatusSnapshot>());
            var characters = new[] { actor, input.Characters[1], Character(9, 3), Character(7, 6) };
            if (!emptyAim) characters = characters.Concat(new[] { Character(2, 5) }).ToArray();
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, damage, category,
                piercing ? new TraitSpec[] { new PiercingFalloffTraitSpec(30, cap) } : Array.Empty<TraitSpec>()), method);
            return CombatCommandTestData.With(input, characters: characters, profiles: profiles);
        }

        internal static CharacterCombatFacts Character(int id, int x, int y = 3) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, new CellCoord(x, y), Owner.Enemy);
    }
}
