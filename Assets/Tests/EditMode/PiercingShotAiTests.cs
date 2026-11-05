using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class PiercingShotAiTests
    {
        [Test]
        public void Ai_UsesDeliveredDamageAndStableCellAimTieBreak()
        {
            var input = PiercingShotTestData.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Concat(new[] { PiercingShotTestData.Character(8, 1, 5) }).Reverse().ToArray());
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim, Is.EqualTo(AttackAim.ForCell(new CellCoord(3,3))));
            var preview = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Sum(t => t.ExpectedDamage), Is.EqualTo(21));
        }

        [Test]
        public void ExhaustedShot_DoesNotMakeFarTargetReachableForPursuit()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(0,2), targetCell: new CellCoord(4,2));
            var data = PiercingShotTestData.Input(cap: 1);
            var actor = CombatCommandTestData.With(input.Characters[0], appointRange: 4,
                traits: data.Characters[0].Stats.TraitStatuses.ToArray());
            var profiles = input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 10, CategoryKey.Main,
                    data.Profiles[0].Definition.Traits.ToArray()), data.Profiles[0].Method) : p).ToArray();
            var blocker = CombatCommandTestData.Character(9, EntityKey.Unit_Akimbo, new CellCoord(2,2), Owner.Player);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], input.Characters[2], blocker }, profiles: profiles);
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.True);
            Assert.That(path.StepCount, Is.GreaterThan(0));
        }

        [Test]
        public void CannonShot_PrefersLowerDamageWithoutFriendlyFire()
        {
            var input = PiercingShotTestData.Input(emptyAim: true);
            input = CombatCommandTestData.With(input, characters: input.Characters.Concat(new[] {
                PiercingShotTestData.Character(8, 1, 5),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Player) }).ToArray());
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            var preview = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId.Value), Is.EqualTo(new[] { 8 }));
        }
    }
}
