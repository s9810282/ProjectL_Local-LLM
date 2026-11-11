using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class IntimidationAttackTests
    {
        internal static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void ThreeAttacks_FromDifferentCasters_RetainFirstAttackReduction()
        {
            var input = Caster();
            var first = Run(input);
            AssertDebuff(Final(first, 2), 20);
            var secondInput = NextCaster(CombatCommandComposition.Project(first), 5, 50);
            var second = Run(secondInput);
            AssertDebuff(Final(second, 2), 20);
            var third = Run(NextCaster(CombatCommandComposition.Project(second), 6, 80));
            AssertDebuff(Final(third, 2), 20);
            Assert.That(Final(third, 2).Stats.TraitStatuses.ToArray(), Is.Empty);
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Debuffs.Intimidation, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeliveredHit_AppliesAtZeroDamage_ButNotAfterLethalDamage(bool lethal)
        {
            var input = Caster();
            input = Replace(input, c => c.EntityId.Value != 2 ? c : lethal
                ? CharacterSettlementCalculator.SetHealth(c, input.Main, 1).Characters.Single().Final
                : HeldEnvironmentEffects.AcquireBadge(c, 41));
            var result = Run(input);
            if (lethal)
                Assert.That(Final(result, 2).Debuffs.Intimidation, Is.Null);
            else
            {
                Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.Zero);
                AssertDebuff(Final(result, 2), 20);
            }
        }

        [Test]
        public void WholeAttack_ReducesSplashAndExpiresBeforeNextAttack()
        {
            var input = Caster(splash: true);
            input = Replace(input, c => c.EntityId == input.ActorId ? Mark(c) : c);
            var first = Run(input);
            Assert.That(first.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 12, 8, 8 }));
            Assert.That(Final(first, 1).Debuffs.Intimidation, Is.Null);
            foreach (var id in new[] { 2, 3, 4 }) AssertDebuff(Final(first, id), 20);
            var afterFirst = CombatCommandComposition.Project(first);
            var restarted = CombatTurnCalculator.BeginControllerTurn(EnemyControllerPhaseTests.Input(afterFirst, CombatControllerPhase.StartingPlayer), Array.Empty<IMovementTileRule>());
            var second = Run(CombatCommandTestData.FreshCommand(CombatCommandComposition.Project(restarted),
                CombatCommandKind.Attack, input.ActorId, input.Aim));
            Assert.That(second.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(15));
            AssertDebuff(input.Characters[0], 20);
        }

        [TestCase(5, 16)]
        [TestCase(10, 20)]
        public void CurrentPositionContribution_IsReducedTogetherWithBase_AndSurvivesExpiry(int bonus, int expected)
        {
            var input = Caster();
            input = Replace(input, c => c.EntityId == input.ActorId
                ? PositionTileModifierCalculator.Replace(Mark(c), new PositionTileModifier(c.Anchor.Value, new[] { 51L }, bonus, 0)) : c);
            var first = Run(input);
            Assert.That(first.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(expected));
            Assert.That(Final(first, 1).PositionModifier.AttackBonus, Is.EqualTo(bonus));
            var afterFirst = CombatCommandComposition.Project(first);
            var restarted = CombatTurnCalculator.BeginControllerTurn(EnemyControllerPhaseTests.Input(afterFirst, CombatControllerPhase.StartingPlayer), Array.Empty<IMovementTileRule>());
            var second = Run(CombatCommandTestData.FreshCommand(CombatCommandComposition.Project(restarted).WithActor(input.ActorId),
                CombatCommandKind.Attack, input.ActorId, input.Aim));
            Assert.That(second.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(15 + bonus));
        }

        [Test]
        public void ContributionChangedWhileDebuffed_UsesNewValueWithoutRestoringCachedAttack()
        {
            var input = Caster();
            input = Replace(input, c => c.EntityId == input.ActorId
                ? PositionTileModifierCalculator.Replace(Mark(c), new PositionTileModifier(c.Anchor.Value, new[] { 51L }, 5, 0)) : c);
            Assert.That(CombatCommandCalculator.Preview(input, NoRules).Targets.Span[0].ExpectedDamage, Is.EqualTo(16));
            input = Replace(input, c => c.EntityId == input.ActorId
                ? PositionTileModifierCalculator.Replace(c, new PositionTileModifier(c.Anchor.Value, new[] { 51L }, 10, 0)) : c);
            var first = Run(input);
            Assert.That(first.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            Assert.That(Run(CombatCommandTestData.NextPlayerTurn(CombatCommandComposition.Project(first))).Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(25));
        }

        [Test]
        public void ChainRepeatedVictim_UsesCapturedDamageAndAppliesOnlyFirstStage()
        {
            var input = AddTrait(Caster(splash: true), new KillChainAttackTraitSpec(), new KillChainAttackTraitStatusSnapshot());
            input = Replace(input, c => c.EntityId == input.ActorId ? Mark(c)
                : c.EntityId.Value == 2 || c.EntityId.Value == 3
                    ? CharacterSettlementCalculator.SetHealth(c, input.Main, 1).Characters.Single().Final : c);
            var result = Run(input);
            // 직접 타격 8 + 처치 두 건의 연쇄 타격 12씩. 같은 생존 대상의 위압은 한 번만 적용한다.
            Assert.That(Final(result, 4).Health.CurrentHealth, Is.EqualTo(68));
            AssertDebuff(Final(result, 4), 20);
            Assert.That(Final(result, 1).Debuffs.Intimidation, Is.Null);
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.KillChainAttack).Select(t => t.Magnitude), Is.EqualTo(new[] { 12, 12 }));
        }
        [Test]
        public void Reaction_ConsumesActualAttackerOnly_AndNoTargetPreservesIt()
        {
            var seed = BossRoarTests.Input(0);
            seed = Replace(seed, c => c.EntityId == BossRoarTests.Boss || c.EntityId == seed.ActorId ? Mark(c) : c);
            var boss = seed.Characters.Single(c => c.EntityId == BossRoarTests.Boss);
            var result = CombatAttackCalculator.Reaction(seed, NoRules, boss, AttackAim.ForAll());
            Assert.That(result, Is.Not.Null);
            Assert.That(Final(result, 2).Debuffs.Intimidation, Is.Null);
            AssertDebuff(Final(result, 1), 20);
            Assert.That(result.Events.OfType<CombatRoarEvent>().Single().Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(4));
            var empty = BossRoarTests.Input(0, withTargets: false);
            empty = Replace(empty, c => c.EntityId == BossRoarTests.Boss ? Mark(c) : c);
            Assert.That(CombatAttackCalculator.Reaction(empty, NoRules,
                empty.Characters.Single(c => c.EntityId == BossRoarTests.Boss), AttackAim.ForAll()), Is.Null);
            AssertDebuff(empty.Characters.Single(c => c.EntityId == BossRoarTests.Boss), 20);
        }

        [Test]
        public void RejectedAttackAndPreview_DoNotConsumeLiveState()
        {
            var input = Replace(Caster(), c => c.EntityId.Value == 1 ? Mark(c) : c);
            var preview = CombatCommandCalculator.Preview(input, NoRules);
            Assert.That(preview.Targets.Span[0].ExpectedDamage, Is.EqualTo(12));
            AssertDebuff(input.Characters[0], 20);
            var rejected = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))), NoRules);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
        }

        [Test]
        public void CompleteAttack_ChangesOnlyDebuffs_RejectsFriendlyAndNonCharacterIds()
        {
            var input = Caster();
            var actor = Mark(input.Characters[0]);
            var before = input.Characters.ToDictionary(c => c.EntityId);
            before[actor.EntityId] = actor;
            var projected = before.ToDictionary(p => p.Key, p => p.Value);
            CombatDebuffApplication.CompleteAttack(input, actor,
                new[] { new EntityId(2), new EntityId(2), new EntityId(10), new EntityId(999) }, projected);
            AssertDebuff(projected[new EntityId(2)], 20);
            Assert.That(projected[actor.EntityId].Debuffs.Intimidation, Is.Null);
            Assert.That(projected[new EntityId(10)].Debuffs.Intimidation, Is.Null);
            foreach (var pair in before)
            {
                Assert.That(projected[pair.Key].StatusEffects.Entries.Where(s => s.Key != StatusEffectKey.AttackDecrease),
                    Is.EqualTo(pair.Value.StatusEffects.Entries.Where(s => s.Key != StatusEffectKey.AttackDecrease)));
                foreach (var property in typeof(CharacterCombatFacts).GetProperties().Where(p => p.Name != nameof(CharacterCombatFacts.Debuffs) && p.Name != nameof(CharacterCombatFacts.StatusEffects)))
                    Assert.That(property.GetValue(projected[pair.Key]), Is.EqualTo(property.GetValue(pair.Value)), property.Name);
            }
        }

        [Test]
        public void Piercing_AttenuatesReducedCommonDamageAndAppliesEachVictimOnce()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(3, 3)), empty: true, category: CategoryKey.Cannon);
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[]
            {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy)
            }).ToArray());
            input = AddTrait(AddTrait(input, new PiercingFalloffTraitSpec(30, 99), new PiercingFalloffTraitStatusSnapshot(30)),
                new IntimidationTraitSpec(20), new IntimidationTraitStatusSnapshot(20));
            input = Replace(input, c => c.EntityId == input.ActorId ? Mark(c) : c);
            var result = Run(input);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 16, 11, 6 }));
            foreach (var id in new[] { 2, 3, 4 }) AssertDebuff(Final(result, id), 20);
        }

        [Test]
        public void PiercingDeliveredAllyAndStructure_DoNotGainVictimDebuffs()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(3, 3)), empty: true, category: CategoryKey.Cannon);
            var structure = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 40, false, 0), new HealthStateSnapshot(40, 40), new CellCoord(5, 3),
                Rotation4.R0, new[] { new CellCoord(5, 3) }, Owner.Player);
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[]
            {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Player)
            }).ToArray(), new CombatCommandState(8,8,input.State.Phase,                new[] { structure },input.State.ThornFields,turn: input.State.Turn));
            input = AddTrait(AddTrait(input, new PiercingFalloffTraitSpec(30, 99), new PiercingFalloffTraitStatusSnapshot(30)),
                new IntimidationTraitSpec(20), new IntimidationTraitStatusSnapshot(20));
            var result = Run(input);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId),
                Is.EqualTo(new[] { new EntityId(2), new EntityId(3), new EntityId(20) }));
            AssertDebuff(Final(result, 2), 20);
            Assert.That(Final(result, 3).Debuffs.Intimidation, Is.Null);
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.LessThan(40));
            Assert.That(result.Characters.Characters.Any(c => c.Final.EntityId.Value == 20), Is.False);
        }

        [Test]
        public void RoarKillsOriginalAttacker_BothCompletedAttacksConsumeTheirState()
        {
            var input = BossRoarTests.Input(4, playerHp: 3);
            input = Replace(input, c => c.EntityId == input.ActorId || c.EntityId == BossRoarTests.Boss ? Mark(c) : c);
            var result = Run(input);
            Assert.That(Final(result, 1).Health.IsDead, Is.True);
            Assert.That(Final(result, 1).Debuffs.Intimidation, Is.Null);
            Assert.That(Final(result, 2).Debuffs.Intimidation, Is.Null);
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(16));
        }

        internal static CombatCommandInput Caster(bool splash = false)
        {
            var input = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, splash ? TargetingSplash.Radial : TargetingSplash.None, splash ? 70 : 0));
            input = Replace(input, c => c.EntityId.Value == 1
                ? CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, c.Anchor.Value, Owner.Player, 15) : c);
            return AddTrait(input, new IntimidationTraitSpec(20), new IntimidationTraitStatusSnapshot(20));
        }

        internal static CombatCommandInput AddTrait(CombatCommandInput input, TraitSpec spec, TraitStatusSnapshot status)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = input.Profiles.Single(p => p.Definition.Key == actor.CharacterKey);
            var next = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, actor.Stats.AttackPower,
                actor.Stats.Category.Key, profile.Definition.Traits.ToArray().Append(spec).ToArray()), profile.Method);
            return CombatCommandTestData.With(Replace(input, c => c.EntityId == actor.EntityId
                ? CombatCommandCalculator.Copy(c, c.Health, CombatCommandEffects.With(c.Stats, traits: c.Stats.TraitStatuses.ToArray().Append(status).ToArray())) : c),
                profiles: input.Profiles.Select(p => p == profile ? next : p).ToArray());
        }

        internal static CharacterCombatFacts Mark(CharacterCombatFacts c, int percent = 20) =>
            CombatDebuffApplication.WithDebuffs(c, new CharacterDebuffState(new IntimidationDebuffState(percent)));
        internal static CombatCommandInput Replace(CombatCommandInput input, Func<CharacterCombatFacts, CharacterCombatFacts> map) =>
            CombatCommandTestData.With(input, characters: input.Characters.Select(map).ToArray());
        internal static CombatCommandTransition Run(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }
        internal static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == id);
        internal static void AssertDebuff(CharacterCombatFacts c, int percent)
        {
            Assert.That(c.Debuffs.Intimidation, Is.Not.Null, c.EntityId.ToString());
            Assert.That(c.Debuffs.SameValues(new CharacterDebuffState(new IntimidationDebuffState(percent, c.Debuffs.Intimidation.Origin))), Is.True);
        }

        private static CombatCommandInput NextCaster(CombatCommandInput input, int id, int percent)
        {
            var actor = CombatCommandTestData.Character(id, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 15);
            actor = CombatCommandCalculator.Copy(actor, actor.Health, CombatCommandEffects.With(actor.Stats,
                traits: new TraitStatusSnapshot[] { new IntimidationTraitStatusSnapshot(percent) }));
            var characters = input.Characters.Where(c => c.EntityId != input.ActorId).Prepend(actor).ToArray();
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,                input.State.Structures,input.State.ThornFields,turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: (CombatFlowPhase.PlayerInput) == CombatFlowPhase.Automatic ? CombatControllerPhase.Enemy : CombatControllerPhase.Player, controllerTurnSequence: 1));
            return new CombatCommandInput(input.Number + 1, CombatCommandKind.Attack, actor.EntityId, input.Aim, state, characters,
                input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey ? new CombatAttackProfile(
                    CombatCommandTestData.Spec(actor.CharacterKey, 15, CategoryKey.Main, new IntimidationTraitSpec(percent)), p.Method) : p).ToArray(),
                input.Main, characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0), input.Souls, input.Artifacts,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence,
                battleInstanceId: input.BattleInstanceId);
        }
    }
}
