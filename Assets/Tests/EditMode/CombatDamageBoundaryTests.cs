using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class CombatDamageBoundaryTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void HeldDefense_IsConsumedAfterIncomingCalculation()
        {
            var input = Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == new EntityId(3)
                ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray());
            var preview = CombatCommandCalculator.Preview(input, NoRules);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(3)).IncomingReduction, Is.Zero);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(3));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(final.HeldEnvironment.BadgeAvailable, Is.False);
        }


        private static CharacterCombatFacts UnownedHeavy(int id, Owner owner, CellCoord cell)
        {
            var source = CombatCommandTestData.With(CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, owner),
                category: CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            return new CharacterCombatFacts(source.EntityId, null, source.CharacterKey, owner, null,
                source.Area, source.Health, source.Stats, null, cell, source.Rotation,
                source.OccupiedCells, UnitActionUse.Available);
        }

        [Test]
        public void EnvironmentalDamage_IsUnchangedBesideHeavy()
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right);
            var provider = UnownedHeavy(88, Owner.Player, new CellCoord(1, 1));
            var state = input.State;
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] { provider }).ToArray(),
                state: EnvironmentDamageTests.WithDamageTiles(new CombatCommandState(state.BoardWidth,state.BoardHeight,state.Phase,                    state.Structures,turn: state.Turn), (CellCoord.Right, 6)));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new EnvironmentDamageTests.DamageRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(94));
        }

        [Test]
        public void Reflection_IsUnchangedBesideHeavy()
        {
            var input = ThornHeartTestData.Input();
            var provider = UnownedHeavy(88, Owner.Enemy, new CellCoord(1, 2));
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] { provider }).ToArray());
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.CurrentHealth, Is.EqualTo(7));
        }

        [Test]
        public void AttackTriggeredExplosion_IsUnchangedBesideHeavy()
        {
            var input = ObstacleReactionTests.Barrels(ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 1, 6));
            var provider = UnownedHeavy(88, Owner.Enemy, new CellCoord(4, 6));
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] { provider }).ToArray());
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(3)).Health.CurrentHealth, Is.EqualTo(94));
        }


        [Test]
        public void AttackAndEnvironment_PreserveCategorylessStructureDamage()
        {
            var input = Input();
            var actor = input.Characters[0];
            var victim = input.Characters.Single(c => c.EntityId == new EntityId(3));
            var engine = new CombatEffectEngine();
            var pipeline = new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()));
            Assert.That(CombatDamageCalculator.Attack(input, engine, pipeline, actor, victim, 6,
                AttackTargetRole.Designated, 100, actor.Anchor, null).FinalDamage, Is.EqualTo(6));
            Assert.That(CombatDamageCalculator.Attack(input, engine, pipeline, actor, null, 6,
                AttackTargetRole.Designated, 100, actor.Anchor, null).FinalDamage, Is.EqualTo(6));
            Assert.That(CombatDamageCalculator.Environment(input, 6, new CombatDamageOrigin(actor.Anchor, actor.EntityId),
                victim.Anchor).FinalDamage, Is.EqualTo(6));
        }

        private static CombatCommandInput Input(bool reverse = false, bool mutual = false, bool splash = false)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare,
                splash ? TargetingChoiceMode.Single : TargetingChoiceMode.All,
                splash ? TargetingSplash.Radial : TargetingSplash.None, splash ? 100 : 0);
            var input = CombatCommandTestData.Input(method, splash ? AttackAim.ForCell(new CellCoord(4, 3)) : AttackAim.ForAll(), empty: true);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 6);
            var provider = CombatCommandTestData.With(CombatCommandTestData.Character(reverse ? 3 : 2,
                EntityKey.Unit_ShotGun, new CellCoord(4, 3), Owner.Enemy), hp: 1,
                category: CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            var ally = CombatCommandTestData.Character(reverse ? 2 : 3, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy);
            if (mutual) ally = CombatCommandTestData.With(ally, category: CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            return CombatCommandTestData.With(input, new[] { actor, input.Characters[1], provider, ally },
                profiles: new[] {
                    new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 6), method),
                    new CombatAttackProfile(CombatCommandTestData.Spec(provider.CharacterKey, 1, CategoryKey.Heavy), method),
                    new CombatAttackProfile(CombatCommandTestData.Spec(ally.CharacterKey, 1, mutual ? CategoryKey.Heavy : CategoryKey.Main), method),
                    input.Profiles[2] });
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void OtherHeavyDeath_SameAttack_OrderInvariant(bool reverse, bool mutual)
        {
            var input = Input(reverse, mutual);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(reverse ? 2 : 3)).DamageDealt, Is.EqualTo(6));
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.CharacterKey == EntityKey.Unit_ShotGun).Health.IsDead, Is.True);
        }

        [Test]
        public void NextTurnAttack_UsesCurrentProjection()
        {
            var input = Input();
            var first = CombatCommandCalculator.Calculate(input, NoRules);
            var next = CombatCommandTestData.NextPlayerTurn(CombatCommandComposition.Project(first));
            var result = CombatCommandCalculator.Calculate(next, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatAttackEvent>().Single().Attack.Hits.ToArray().Single(h => h.TargetId == new EntityId(3)).DamageDealt, Is.EqualTo(6));
            Assert.That(input.Characters.Single(c => c.EntityId == new EntityId(2)).Health.IsDead, Is.False);
        }

        [Test]
        public void Preview_MatchesExecutionWithoutChangingInput()
        {
            var input = Input(splash: true);
            var before = input.Characters.Select(c => c.Health.CurrentHealth).ToArray();
            var preview = CombatCommandCalculator.Preview(input, NoRules);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetHealthAfter), Is.EqualTo(result.Attack.Value.Hits.ToArray().Select(h => h.TargetHealthAfter)));
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(3)).ExpectedDamage, Is.EqualTo(6));
            Assert.That(input.Characters.Select(c => c.Health.CurrentHealth), Is.EqualTo(before));
        }

        [Test]
        public void UnprotectedHeavy_HasNoSelfArmor()
        {
            var input = Input();
            var characters = input.Characters.Select(c => c.EntityId == new EntityId(2) ? CombatCommandTestData.With(c, hp: 100) : c).ToArray();
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, characters), NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(2)).DamageDealt, Is.EqualTo(6));
        }

        [Test]
        public void KillChain_UsesCurrentProjectionAfterOtherVictimDeath()
        {
            var input = Input(splash: true);
            var actor = CombatCommandTestData.With(input.Characters[0], traits: new TraitStatusSnapshot[] { new KillChainAttackTraitStatusSnapshot() });
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            input = CombatCommandTestData.With(input,
                input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey ?
                    new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 6, CategoryKey.Main, new KillChainAttackTraitSpec()), method) : p).ToArray());
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(3)).Health.CurrentHealth, Is.EqualTo(94));
        }

        [Test]
        public void Reaction_UsesCurrentProjectionAfterOtherVictimDeath()
        {
            var input = Input();
            var first = CombatCommandCalculator.Calculate(input, NoRules);
            var current = CombatCommandComposition.Project(first);
            var result = CombatAttackCalculator.Reaction(current, NoRules, current.Characters.Single(c => c.EntityId == current.ActorId), input.Aim);
            Assert.That(result, Is.Not.Null);
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(3)).Health.CurrentHealth, Is.EqualTo(88));
        }
    }
}
