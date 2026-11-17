using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PowderBlossomSkillTests
    {
        [Test]
        public void BloomUsesExactMasksAndStartsAgainAtEightCellsNextCast()
        {
            var input = PowderBlossomFixture.Input(PlayerSkillUpgradeKey.Bloom, enemies: new[]
            {
                PowderBlossomFixture.Enemy(2, 4, 3, 10),
                PowderBlossomFixture.Enemy(3, 5, 3, 10),
                PowderBlossomFixture.Enemy(4, 6, 3, 10)
            });
            var output = PowderBlossomFixture.Calculate(input);
            var attacks = PowderBlossomFixture.Attacks(output);
            Assert.That(attacks.Select(a => a.AffectedCells.Count), Is.EqualTo(new[] { 8, 20, 36, 36 }));
            AssertMask(attacks[0], 3, 3, 3);
            AssertMask(attacks[1], 3, 5, 5, 5, 3);
            AssertMask(attacks[2], 3, 5, 7, 7, 7, 5, 3);
            Assert.That(attacks.Select(a => a.AttackGroup).Distinct().Count(), Is.EqualTo(4));
            Assert.That(output.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(4));
            Assert.That(output.FinalState.PlayerSkill.RemainingUses, Is.EqualTo(1));
            var next = PowderBlossomFixture.Input(PlayerSkillUpgradeKey.Bloom, sequence: 3,
                uses: output.FinalState.PlayerSkill);
            Assert.That(PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(next)).Single().AffectedCells.Count, Is.EqualTo(8));
        }

        [Test]
        public void ClippedEmptyCastConsumesOnlySkillUse()
        {
            var input = PowderBlossomFixture.Input(center: CellCoord.Zero);
            var output = PowderBlossomFixture.Calculate(input);
            var attack = PowderBlossomFixture.Attacks(output).Single();
            Assert.That(attack.AffectedCells, Is.EquivalentTo(new[] { new CellCoord(0, 1), new CellCoord(1, 0), new CellCoord(1, 1) }));
            Assert.That(attack.Hits, Is.Empty);
            Assert.That(output.FinalState.PlayerSkill.RemainingUses, Is.EqualTo(1));
            Assert.That(output.ActionUseSettlement, Is.Null);
            Assert.That(output.Characters.Characters.All(c => c.Expected.ActionUse == c.Final.ActionUse), Is.True);
        }

        [Test]
        public void WallsDoNotOccludeAndMultiCellStructuresAreHitOnce()
        {
            var wall = new StaticTerrainFact(71, new CellCoord(4, 3), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });
            var structure = PowderBlossomFixture.Structure(51, EntityKey.Structure_Headquarters, 2, 2, 100, Owner.Enemy, width: 2);
            var input = PowderBlossomFixture.Input(PlayerSkillUpgradeKey.Condemnation,
                enemies: new[] { PowderBlossomFixture.Enemy(2, 5, 3, 100) },
                obstacle: structure, wall: wall, ally: new CellCoord(4, 4));
            var output = PowderBlossomFixture.Calculate(input);
            var attack = PowderBlossomFixture.Attacks(output).Single();
            Assert.That(attack.Hits.Select(h => h.TargetId.Value), Is.EquivalentTo(new[] { 2, 51 }));
            Assert.That(attack.Hits.Single(h => h.TargetId.Value == 51).Contacts.Count, Is.EqualTo(1));
            Assert.That(attack.DamageByRecipient[new EntityId(2)].DamageBeforeMatchup, Is.EqualTo(10));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OneBurstSchedulesAtMostOneExtraBurstAndKillChainRunsFirst(bool condemnation)
        {
            var input = PowderBlossomFixture.Input(condemnation ? PlayerSkillUpgradeKey.Condemnation : PlayerSkillUpgradeKey.None,
                enemies: new[] { PowderBlossomFixture.Enemy(2, 4, 3, 10), PowderBlossomFixture.Enemy(3, 2, 3, 10),
                    PowderBlossomFixture.Enemy(4, 6, 6, 20) }, trait: new KillChainAttackTraitSpec());
            var output = PowderBlossomFixture.Calculate(input);
            var attacks = output.Events.OfType<CombatPlayerSkillAttackEvent>().ToArray();
            var child = output.Events.OfType<CombatEffectsEvent>().Single(e => e.Traits.Any(t =>
                t.TraitKey == TraitKey.KillChainAttack && t.ChildAttack != null));
            Assert.That(PowderBlossomFixture.Attacks(output)[0].Hits.Count(h => h.TargetKilled), Is.EqualTo(2));
            Assert.That(attacks.Length, Is.EqualTo(condemnation ? 1 : 2));
            Assert.That(Array.IndexOf(output.Events.ToArray(), attacks[0]), Is.LessThan(Array.IndexOf(output.Events.ToArray(), child)));
            if (!condemnation)
                Assert.That(Array.IndexOf(output.Events.ToArray(), child), Is.LessThan(Array.IndexOf(output.Events.ToArray(), attacks[1])));
            Assert.That(output.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(attacks.Length + 1));
        }

        [Test]
        public void CondemnationStunsActualSurvivingContactInItsTwentyCellMask()
        {
            var target = PowderBlossomFixture.Enemy(2, 5, 3, 100);
            var input = PowderBlossomFixture.Input(PlayerSkillUpgradeKey.Condemnation, enemies: new[] { target });
            var output = PowderBlossomFixture.Calculate(input);
            var attack = PowderBlossomFixture.Attacks(output).Single();
            AssertMask(attack, 3, 5, 5, 5, 3);
            var recipient = output.Characters.Characters.Single(c => c.Final.EntityId == target.EntityId).Final;
            var stun = (ActionRestrictionStatus)recipient.StatusEffects.Entries.Single(s => s.Key == StatusEffectKey.Stun);
            Assert.That(stun.RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            Assert.That(stun.Origin.Value.Kind, Is.EqualTo(EffectOwnerKind.PlayerSkill));
            Assert.That(stun.Origin.Value.OwnerInstanceId, Is.EqualTo(input.ActorId.Value));
        }

        [Test]
        public void ObstacleExplosionDeathDoesNotSeedAnotherBurstOrKillChain()
        {
            var barrel = PowderBlossomFixture.Structure(51, EntityKey.Structure_PowderBarrel, 4, 3, 1, Owner.Neutral);
            var input = PowderBlossomFixture.Input(enemies: new[] { PowderBlossomFixture.Enemy(2, 5, 3, 5) },
                obstacle: barrel, profile: new ObstacleContentProfile(barrel.EntityId, ObstacleContentKind.PowderBarrel, 10),
                trait: new KillChainAttackTraitSpec());
            var output = PowderBlossomFixture.Calculate(input);
            Assert.That(PowderBlossomFixture.Attacks(output).Length, Is.EqualTo(1));
            Assert.That(output.Events.OfType<CombatObstacleReactionEvent>().Any(), Is.True);
            Assert.That(output.Characters.Characters.Single(c => c.Final.EntityId.Value == 2).Final.Health.IsDead, Is.True);
            Assert.That(output.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.ChildAttack != null), Is.False);
        }

        [Test]
        public void SuppliesChangeNextBurstCurrentAttackButEachBurstStillUsesHalf()
        {
            var supplies = PowderBlossomFixture.Structure(51, EntityKey.Structure_Supplies, 3, 4, 1, Owner.Neutral);
            var input = PowderBlossomFixture.Input(PlayerSkillUpgradeKey.Bloom,
                enemies: new[] { PowderBlossomFixture.Enemy(2, 4, 3, 10), PowderBlossomFixture.Enemy(3, 5, 3, 100) },
                obstacle: supplies, profile: new ObstacleContentProfile(supplies.EntityId, ObstacleContentKind.Supplies, 0, 0, 12));
            var attacks = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input));
            Assert.That(attacks.Length, Is.EqualTo(2));
            Assert.That(attacks[0].DamageByRecipient[new EntityId(2)].DamageBeforeMatchup, Is.EqualTo(10));
            Assert.That(attacks[1].DamageByRecipient[new EntityId(3)].DamageBeforeMatchup, Is.EqualTo(16));
        }

        [Test]
        public void CurrentDamageRebasePreservesThePrimarySkillFactor()
        {
            var input = PowderBlossomFixture.Input(enemies: new[] { PowderBlossomFixture.Enemy(2, 4, 3, 100) });
            var (raw, _, _) = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack);
            var scaled = new CommandAttackPlan(raw.Input, raw.Rules, raw.Profile, raw.OriginalActor, raw.Actor,
                raw.Environment, raw.Walls, raw.Selection, raw.Delivery, raw.Engine, raw.Bindings, raw.Pipeline,
                raw.CommonDamage, raw.Direction, raw.PublicPreview, raw.DamageKind, damagePercent: 50);
            var supplied = HeldEnvironmentEffects.GrantSupplies(raw.Actor, 11);

            Assert.That(CombatAttackDelivery.CurrentBaseDamage(scaled, raw.Actor, 0), Is.EqualTo(10));
            Assert.That(CombatAttackDelivery.CurrentBaseDamage(raw, supplied, 0), Is.EqualTo(31));
            Assert.That(CombatAttackDelivery.CurrentBaseDamage(scaled, supplied, 0), Is.EqualTo(16));
        }

        [Test]
        public void KillChainReadsSuppliesBeforeTheNextBurstAndKeepsItsOwnDamageRule()
        {
            var supplies = PowderBlossomFixture.Structure(51, EntityKey.Structure_Supplies, 3, 4, 1, Owner.Neutral);
            var input = PowderBlossomFixture.Input(PlayerSkillUpgradeKey.Bloom,
                enemies: new[] { PowderBlossomFixture.Enemy(2, 4, 3, 10), PowderBlossomFixture.Enemy(3, 5, 3, 100) },
                obstacle: supplies, profile: new ObstacleContentProfile(supplies.EntityId, ObstacleContentKind.Supplies, 0, 0, 12),
                trait: new KillChainAttackTraitSpec());
            var output = PowderBlossomFixture.Calculate(input);
            var child = output.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitKey == TraitKey.KillChainAttack && t.ChildAttack != null).ChildAttack;

            Assert.That(child.Hits.Single().TargetHealthAfter, Is.EqualTo(68));
            Assert.That(PowderBlossomFixture.Attacks(output)[1].DamageByRecipient[new EntityId(3)].DamageBeforeMatchup, Is.EqualTo(16));
            Assert.That(output.Characters.Characters.Single(c => c.Final.EntityId.Value == 3).Final.Health.CurrentHealth, Is.EqualTo(52));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalOutcomeInterruptsPendingBurstButKeepsTheSingleCharge(bool defeat)
        {
            var structure = defeat
                ? PowderBlossomFixture.Structure(51, EntityKey.Structure_PowderBarrel, 4, 4, 1, Owner.Neutral)
                : PowderBlossomFixture.Structure(51, EntityKey.Structure_Headquarters, 4, 4, 5, Owner.Enemy);
            var profile = defeat ? new ObstacleContentProfile(structure.EntityId, ObstacleContentKind.PowderBarrel, 150) : null;
            var input = PowderBlossomFixture.Input(enemies: new[] { PowderBlossomFixture.Enemy(2, 4, 3, 5) },
                obstacle: structure, profile: profile, replaceHeadquarters: !defeat);
            var output = PowderBlossomFixture.Calculate(input);
            Assert.That(output.Outcome.Outcome, Is.EqualTo(defeat ? StageOutcome.Defeat : StageOutcome.Victory));
            Assert.That(PowderBlossomFixture.Attacks(output).Length, Is.EqualTo(1));
            Assert.That(output.FinalState.PlayerSkill.RemainingUses, Is.EqualTo(1));
        }

        private static void AssertMask(PlayerSkillAttackResult attack, params int[] widths)
        {
            var expected = widths.SelectMany((width, row) => Enumerable.Range(3 - width / 2, width)
                .Select(x => new CellCoord(x, 3 + row - widths.Length / 2))).Where(c => c != new CellCoord(3, 3));
            Assert.That(attack.AffectedCells, Is.EquivalentTo(expected));
        }
    }

    internal static class PowderBlossomFixture
    {
        internal static CombatCommandInput Input(PlayerSkillUpgradeKey upgrade = PlayerSkillUpgradeKey.None,
            CharacterCombatFacts[] enemies = null, CellCoord? center = null, StructureCombatFacts obstacle = null,
            ObstacleContentProfile profile = null, StaticTerrainFact wall = null, CellCoord? ally = null,
            TraitSpec trait = null, bool replaceHeadquarters = false, long sequence = 1, PlayerSkillBattleState uses = null,
            long revisionDelta = 0, CellCoord[] points = null)
        {
            var source = PlayerSkillTestData.Input();
            var main = CombatCommandTestData.With(source.Characters.Single(c => c.EntityId == source.ActorId),
                position: center ?? new CellCoord(3, 3), actionUse: UnitActionUse.Attacked);
            var sub = CombatCommandTestData.With(source.Characters.Single(c => c.EntityId.Value == 1),
                position: ally ?? new CellCoord(0, 7));
            var characters = new[] { main, sub }.Concat(enemies ?? Array.Empty<CharacterCombatFacts>()).ToArray();
            var structures = replaceHeadquarters ? new[] { obstacle } :
                new[] { Structure(50, EntityKey.Structure_Headquarters, 7, 7, 100, Owner.Enemy), obstacle }
                    .Where(s => s != null).ToArray();
            var environment = wall == null ? MovementEnvironmentFacts.Empty : new MovementEnvironmentFacts(new[] { wall });
            var state = source.State.With(turn: new CombatTurnFacts(StagePhase.Combat, CombatControllerPhase.Player, sequence),
                structures: structures, environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment),
                itemRandomState: 41, playerSkill: uses ?? new PlayerSkillBattleState(source.MainId, main.EntityId, PlayerSkillTestData.Definition, upgrade));
            var stamp = new PlayerSkillSourceStamp(source.BattleInstanceId, sequence, 7, main.EntityId);
            var expected = new PlayerSkillSourceStamp(source.BattleInstanceId, sequence, 7 + revisionDelta, main.EntityId);
            var input = new CombatCommandInput(1, CombatCommandKind.UsePlayerSkill, main.EntityId, default, state,
                characters, source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battleInstanceId: source.BattleInstanceId, membership: source.Membership,
                environmentBindings: new EnvironmentSemanticBindings(wall == null ? Array.Empty<long>() : new[] { wall.Id },
                    Array.Empty<long>(), profile == null ? Array.Empty<EntityId>() : new[] { profile.EntityId }),
                obstacleContents: profile == null ? Array.Empty<ObstacleContentProfile>() : new[] { profile },
                playerSkillRequest: new PlayerSkillRequest(points ?? Array.Empty<CellCoord>(), expected), playerSkillSource: stamp);
            if (trait == null) return input;
            var equipped = TraitCombatFixtures.Equip(input, main.CharacterKey, trait);
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State,
                equipped.Characters, equipped.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                battleInstanceId: input.BattleInstanceId, environmentBindings: input.EnvironmentBindings,
                obstacleContents: input.ObstacleContents, membership: input.Membership,
                playerSkillRequest: input.PlayerSkillRequest, playerSkillSource: input.PlayerSkillSource);
        }

        internal static CharacterCombatFacts Enemy(int id, int x, int y, int hp) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, new CellCoord(x, y), Owner.Enemy)
                .WithHealth(new HealthStateSnapshot(hp, 100));

        internal static StructureCombatFacts Structure(int id, EntityKey key, int x, int y, int hp, Owner owner, int width = 1)
        {
            var offsets = Enumerable.Range(0, width).Select(i => new CellCoord(i, 0)).ToArray();
            var anchor = new CellCoord(x, y);
            var cells = offsets.Select(c => anchor + c).ToArray();
            if (ObstacleContentProfile.IsNativeObstacleKey(key))
                return new StructureCombatFacts(new EntityId(id), new StructureSpec(key, width, 1, offsets, 1, false, 0),
                    new ObstacleDurabilitySnapshot(hp, hp), anchor, Rotation4.R0, cells, owner);
            return new StructureCombatFacts(new EntityId(id), new StructureSpec(key, width, 1, offsets, hp, false, 0),
                new HealthStateSnapshot(hp, hp), anchor, Rotation4.R0, cells, owner);
        }

        internal static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var output = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(output.Accepted, Is.True, output.Reason);
            CombatHealthEvidenceValidator.Validate(output);
            return output;
        }

        internal static PlayerSkillAttackResult[] Attacks(CombatCommandTransition transition) =>
            transition.Events.OfType<CombatPlayerSkillAttackEvent>().Select(e => e.Attack).ToArray();
    }
}
