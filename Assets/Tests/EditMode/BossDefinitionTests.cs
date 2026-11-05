using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    /// <summary>보스 행동 제한과 Boss 카테고리 계약을 확인한다(계획 T1.2).</summary>
    public sealed class BossDefinitionTests
    {
        private static readonly UnitBehaviorSpec BossBehavior = new UnitBehaviorSpec(false, false, true);

        [TestCase(3)]
        [TestCase(5)]
        public void BossBehavior_RejectsMoveAndBasicAttackDespiteFullAp(int allowance)
        {
            var source = MovementTestData.WithMovement(CombatCommandTestData.Input(),allowance);
            var actor = source.Characters[0];
            var input = WithBehavior(CombatCommandTestData.With(source,
                source.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray()), actor.CharacterKey, BossBehavior);
            var rules = Array.Empty<IMovementTileRule>();

            var attack = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(attack.Accepted, Is.False);
            Assert.That(attack.Reason, Is.EqualTo(nameof(AttackFailureReason.BasicAttackDisabled)));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).FailureReason, Is.EqualTo(AttackFailureReason.BasicAttackDisabled));
            var preview = CombatCommandCalculator.Preview(input, rules);
            Assert.That(preview.CanAttack, Is.False);
            Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.BasicAttackDisabled));

            var move = Move(input, new CellCoord(1, 3), new CellCoord(2, 3));
            var moved = CombatCommandCalculator.Calculate(move, rules);
            Assert.That(moved.Accepted, Is.False);
            Assert.That(moved.Reason, Is.EqualTo(nameof(MovementFailureReason.VoluntaryMoveDisabled)));
            Assert.That(CombatMovementCalculator.Range(move).FailureReason, Is.EqualTo(MovementFailureReason.VoluntaryMoveDisabled));
            Assert.That(CombatMovementCalculator.Preview(move, rules).CanMove, Is.False);
            Assert.That(CombatMovementCalculator.Preview(move, rules).FailureReason, Is.EqualTo(MovementFailureReason.VoluntaryMoveDisabled));
        }

        [Test]
        public void DefaultBehavior_StillMovesAndAttacks()
        {
            var input = CombatCommandTestData.Input();
            var rules = Array.Empty<IMovementTileRule>();
            Assert.That(input.Profiles.All(p => ReferenceEquals(p.Definition.Behavior, UnitBehaviorSpec.Default)), Is.True);
            Assert.That(CombatCommandCalculator.Calculate(input, rules).Accepted, Is.True);
            Assert.That(CombatCommandCalculator.Calculate(Move(CombatCommandTestData.Input(empty: true),
                new CellCoord(1, 3), new CellCoord(2, 3)), rules).Accepted, Is.True);
        }

        [Test]
        public void BossBehavior_EndTurnStillAccepted()
        {
            var source = CombatCommandTestData.Input(empty: true);
            var input = CombatCommonCommandTests.Command(CombatCommandKind.EndPlayerTurn,
                WithBehavior(source, source.Characters[0].CharacterKey, BossBehavior));

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnemyAi_WithBossBehavior_OnlyEndsTurn(bool boss)
        {
            var source = EnemyMovementPathTests.Input();
            var enemy = source.Characters[0];
            var input = boss ? WithBehavior(source, enemy.CharacterKey, BossBehavior) : source;

            var result = EnemyAiTurnService.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            bool acted = result.Attack.HasValue || result.PathMovement != null;
            Assert.That(acted, Is.EqualTo(!boss));
            if (!boss) return;
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Attacked || e.Kind == CombatEventKind.Moved), Is.False);
            var final = result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == enemy.EntityId)?.Final ?? enemy;
            Assert.That(final.Anchor, Is.EqualTo(enemy.Anchor));
            Assert.That(result.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(input.State.Turn.CompletedPlayerTurnCount));
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingPlayer));
        }

        [Test]
        public void BossCategory_DisablesUnspecifiedMovement_AndAllowsConfiguredAttack()
        {
            var input = CombatCommandTestData.Input(category: CategoryKey.Boss);
            Assert.That(input.Profiles.Single(p => p.Definition.Key == input.Characters[0].CharacterKey).Definition.Behavior.CanMoveVoluntarily,
                Is.False);

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        [TestCase(CategoryKey.Boss, CategoryKey.Main)]
        [TestCase(CategoryKey.Boss, CategoryKey.Rider)]
        [TestCase(CategoryKey.Boss, CategoryKey.Heavy)]
        [TestCase(CategoryKey.Boss, CategoryKey.Cannon)]
        [TestCase(CategoryKey.Boss, CategoryKey.Boss)]
        [TestCase(CategoryKey.Main, CategoryKey.Boss)]
        [TestCase(CategoryKey.Rider, CategoryKey.Boss)]
        [TestCase(CategoryKey.Heavy, CategoryKey.Boss)]
        [TestCase(CategoryKey.Cannon, CategoryKey.Boss)]
        public void BossMatchups_AreNeutral(CategoryKey attacker, CategoryKey defender)
        {
            var multiplier = new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()).GetMultiplier(attacker, defender);

            Assert.That(multiplier.Numerator, Is.EqualTo(multiplier.Denominator));
        }

        [Test]
        public void MatchupTable_RequiresAllTwentyFiveEntries()
        {
            var keys = new[] { CategoryKey.Main, CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon };
            var withoutBoss = keys.SelectMany(a => keys.Select(d => new CategoryMatchupEntry(a, d, 10, 10))).ToArray();

            Assert.Throws<ArgumentException>(() => new CategoryMatchupSpec(withoutBoss));
            Assert.DoesNotThrow(() => CategoryMatchupSpec.CreateDefault());
        }

        [Test]
        public void Registry_BossIsOptionalRulelessCategory()
        {
            var core = UnitSpecTestFactory.CreateDefaultCategoryCatalog().ToArray();
            var withoutBoss = new CategoryRegistry(core);
            var withBoss = new CategoryRegistry(core.Concat(new[] { new CategorySpec(CategoryKey.Boss,Array.Empty<CategoryRuleSpec>(),0) }).ToArray());

            Assert.Throws<InvalidOperationException>(() => withoutBoss.GetRequired(CategoryKey.Boss));
            Assert.That(withBoss.GetRequired(CategoryKey.Boss).RuleDefinitions.Length, Is.Zero);
            Assert.That(CategoryRegistry.CreateDefault().GetRequired(CategoryKey.Boss).Key, Is.EqualTo(CategoryKey.Boss));
            Assert.Throws<ArgumentException>(() => new CategoryRegistry(core.Concat(new[]
            {
                new CategorySpec(CategoryKey.Boss,new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() },0)
            }).ToArray()));
        }

        private static CombatCommandInput WithBehavior(CombatCommandInput input, EntityKey key, UnitBehaviorSpec behavior) =>
            CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key != key ? p :
                new CombatAttackProfile(With(p.Definition, behavior), p.Method)).ToArray());

        private static UnitSpec With(UnitSpec s, UnitBehaviorSpec behavior) => new UnitSpec(s.Key, s.Role, s.FootprintWidth,
            s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.AttackRange,
            s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(), s.OwnerEligibility, s.Occlusion, behavior);

        private static CombatCommandInput Move(CombatCommandInput i, params CellCoord[] cells) =>
            new CombatCommandInput(1, CombatCommandKind.Move, i.ActorId, default, i.State, i.Characters, i.Profiles, i.Main,
                i.Owned, i.SoulCounts, i.Souls, i.Artifacts, path: new MovementPath(cells),
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (i.State).Turn.ControllerTurnSequence);
    }
}
