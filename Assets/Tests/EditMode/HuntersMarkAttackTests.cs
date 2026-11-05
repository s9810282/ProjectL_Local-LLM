using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;

namespace ProjectL.Tests.Core
{
    public sealed class HuntersMarkAttackTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly EntityId Target = new(2);

        [Test]
        public void OrderedHits_FirstGrantAffectsOnlyLaterHits()
        {
            var input = Input(markPercent: 25);
            var (world, ledger) = RepeatedDirectStage(input);
            Assert.That(world.Characters[Target].Health.CurrentHealth, Is.EqualTo(55));
            Assert.That(ledger.Direct(Target), Is.EqualTo(45));
            Assert.That(ledger.Breakdown(Target).FinalDamage, Is.EqualTo(25));
            Assert.That(Exposures(world.Characters[Target]).Single().IncreasePercent, Is.EqualTo(25));
            Assert.That(world.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
            Assert.That(input.Characters.Single(c => c.EntityId == Target).StatusEffects.Entries, Is.Empty);
        }

        [Test]
        public void ExistingSourceAndNewSource_UseMaxWithoutConsumption()
        {
            var input = Input(markPercent: 25, existingPercent: 10);
            var (world, ledger) = RepeatedDirectStage(input);
            Assert.That(world.Characters[Target].Health.CurrentHealth, Is.EqualTo(53));
            Assert.That(ledger.Direct(Target), Is.EqualTo(47));
            Assert.That(Exposures(world.Characters[Target]).Select(s => s.IncreasePercent), Is.EqualTo(new[] { 10, 25 }));
            Assert.That(world.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
        }

        [Test]
        public void GrantPublishesFullFactsAfterHealthAndPreservesOtherSource()
        {
            var result = Run(Input(markPercent: 25, existingPercent: 10));
            Assert.That(Hit(result).DamageDealt, Is.EqualTo(22));
            Assert.That(Exposures(Final(result)).Select(s => s.IncreasePercent), Is.EqualTo(new[] { 10, 25 }));
            Assert.That(Final(result).Debuffs.HuntersMark, Is.Null);
            var events = result.Events.ToArray();
            int health = Array.FindIndex(events, e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.HealthChanged && c.Final.EntityId == Target);
            int status = Array.FindIndex(events, e => e is CombatTraitStateEvent t && t.Final.EntityId == Target &&
                Exposures(t.Final).Length > Exposures(t.Expected).Length);
            Assert.That(health, Is.GreaterThanOrEqualTo(0));
            Assert.That(status, Is.GreaterThan(health));
        }

        [Test]
        public void HeldDefenseAndZeroDamage_DoNotConsumeExposure()
        {
            foreach (bool badge in new[] { false, true })
            {
                var input = Input(trait: false, existingPercent: 25);
                input = CombatCommandTestData.With(input, input.Characters.Select(c => badge && c.EntityId == Target
                    ? HeldEnvironmentEffects.AcquireBadge(c, 41) : !badge && c.EntityId == input.ActorId
                    ? c.With(debuffs: new CharacterDebuffState(new IntimidationDebuffState(100))) : c).ToArray());
                var result = Run(input);
                Assert.That(Hit(result).DamageDealt, Is.Zero);
                Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(100));
                Assert.That(Exposures(Final(result)).Single().IncreasePercent, Is.EqualTo(25));
                Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
                if (badge) Assert.That(Final(result).HeldEnvironment.BadgeAvailable, Is.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ZeroSplash_DoesNotGrantWhileShieldedContactDoes(bool shield)
        {
            var input = Input(markPercent: 25, method: new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.Radial, 0));
            if (shield) input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == Target ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray());
            var result = Run(input);
            Assert.That(Exposures(Final(result)).Single().IncreasePercent, Is.EqualTo(25));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(shield ? 100 : 80));
            Assert.That(CombatCommandComposition.Project(result).Characters.Where(c => c.Owner == Owner.Enemy && c.EntityId != Target)
                .All(c => Exposures(c).Length == 0), Is.True);
        }

        [Test]
        public void LethalAndStructureHits_DoNotGrant()
        {
            var input = Input(markPercent: 25);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == Target ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            var result = Run(input);
            Assert.That(Final(result).Health.IsDead, Is.True);
            Assert.That(Exposures(Final(result)), Is.Empty);
            input = Input(markPercent: 25);
            var structure = new StructureCombatFacts(new EntityId(51), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100), new CellCoord(4, 3), Rotation4.R0, new[] { new CellCoord(4, 3) }, Owner.Enemy);
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId != Target).ToArray(), state: input.State.With(structures: new[] { structure }));
            result = Run(input);
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
        }

        [Test]
        public void Reaction_ReceivesAmplificationWithoutGranting()
        {
            var input = Input(markPercent: 25, existingPercent: 10);
            var result = CombatAttackCalculator.Reaction(input, NoRules, input.Characters[0], input.Aim);
            Assert.That(result, Is.Not.Null);
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(78));
            Assert.That(Exposures(Final(result)).Single().Origin.Value.OwnerInstanceId, Is.EqualTo(90));
        }

        [Test]
        public void PreviewAiAndRejectedAttack_DoNotMutateSource()
        {
            var input = Input(trait: false, existingPercent: 25);
            var original = input.Characters.Single(c => c.EntityId == Target);
            for (int i = 0; i < 3; i++)
            {
                var preview = CombatCommandCalculator.Preview(input, NoRules, out var projection);
                Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == Target).ExpectedDamage, Is.EqualTo(25));
                Assert.That(Final(projection).StatusEffects.SameValues(original.StatusEffects), Is.True);
                Assert.That(new EnemyAiPolicy(new HostilityPolicy(), NoRules).TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out _), Is.True);
                Assert.That(original.Health.CurrentHealth, Is.EqualTo(100));
            }
            var rejected = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))), NoRules);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(original.StatusEffects.Entries.Count, Is.EqualTo(1));
            Assert.That(Hit(Run(input)).DamageDealt, Is.EqualTo(25));
        }

        [TestCase(EntityKey.Unit_Akimbo)]
        [TestCase(EntityKey.Unit_ShotGun)]
        public void GrantDependsOnTraitAndConfiguredDuration(EntityKey key)
        {
            var result = Run(Input(key: key, markPercent: 25, duration: 3));
            var status = Exposures(Final(result)).Single();
            Assert.That((status.IncreasePercent, status.RemainingAffectedSideTurnEnds), Is.EqualTo((25, (int?)3)));
            Assert.That(status.Origin.Value.OwnerInstanceId, Is.EqualTo(result.Input.ActorId.Value));
            Assert.That(Exposures(Final(Run(Input(trait: false, key: key)))), Is.Empty);
        }

        [Test]
        public void OrdinaryKillChain_UsesPersistentExposureWithoutOwningTrait()
        {
            var input = CombatTraitTestFixtures.KillChainInput(building: false);
            var id = new EntityId(3);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == id ? Expose(c, 25) : c).ToArray());
            var result = Run(input);
            var final = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == id);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(75));
            Assert.That(Exposures(final).Single().IncreasePercent, Is.EqualTo(25));
        }

        [Test]
        public void SourceAttackerBenefitsAndRefreshesWeakerShorterValues()
        {
            var input = Input(markPercent: 10, duration: 1);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == Target ? Expose(c, 30, 3, input.ActorId.Value) : c).ToArray());
            var result = Run(input);
            Assert.That(Hit(result).DamageDealt, Is.EqualTo(26));
            var status = Exposures(Final(result)).Single();
            Assert.That((status.IncreasePercent, status.RemainingAffectedSideTurnEnds), Is.EqualTo((10, (int?)1)));
        }

        internal static CharacterCombatFacts Expose(CharacterCombatFacts target, int percent = 30, int duration = 2, int source = 90) =>
            target.WithStatusEffects(WeaknessExposureRules.Apply(target.StatusEffects,
                new WeaknessExposureStatus(new EffectOriginKey(EffectOwnerKind.Trait, source, (int)TraitKey.HuntersMark, "main"), percent, duration)));
        internal static WeaknessExposureStatus[] Exposures(CharacterCombatFacts target) => target.StatusEffects.Entries.OfType<WeaknessExposureStatus>().ToArray();

        internal static CombatCommandInput Input(bool trait = true, int markPercent = 10, int? existingPercent = null,
            EntityKey key = EntityKey.Unit_Akimbo, TargetingMethodSpec method = null, int duration = 1)
        {
            method ??= new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method);
            var source = input.Characters[0];
            var statuses = trait ? new TraitStatusSnapshot[] { new HuntersMarkTraitStatusSnapshot(markPercent, duration) } : Array.Empty<TraitStatusSnapshot>();
            var actor = new CharacterCombatFacts(source.EntityId, source.OwnedId, key, source.Owner, source.OwnershipRole,
                source.Area, source.Health, source.Stats.With(traitStatuses: statuses), source.ReserveSlotIndex, source.Anchor,
                source.Rotation, source.OccupiedCells, source.ActionUse);
            var profiles = input.Profiles.Where(p => p.Definition.Key != source.CharacterKey).Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(key, 20, CategoryKey.Main,
                    trait ? new TraitSpec[] { new HuntersMarkTraitSpec(markPercent, duration) } : Array.Empty<TraitSpec>()), method)).ToArray();
            var changed = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == actor.EntityId ? actor :
                c.EntityId == Target && existingPercent.HasValue ? Expose(c, existingPercent.Value) : c).ToArray(), profiles: profiles);
            return changed.WithPersistentFacts(changed.Owned.Select(o => o.Id == actor.OwnedId
                ? new OwnedCharacterRecord(o.Id, key, o.Role, o.Level, o.Experience) : o).ToArray(),
                changed.ItemInventory, changed.GoldBalance, changed.FameBalance);
        }

        internal static (CommandAttackProjection World, CommandAttackLedger Ledger) RepeatedDirectStage(CombatCommandInput input)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var environment = input.State.Environment;
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var selection = CombatAttackQueries.Selection(input, actor, profile, environment: environment, walls: walls);
            Assert.That(selection.FailureReason, Is.EqualTo(AttackFailureReason.None));
            var elevation = AttackElevation.From(environment, input.EnvironmentBindings);
            var delivery = CombatAttackDelivery.Resolve(input, actor, profile, selection.Selection, 20, walls, elevation);
            var repeated = new ResolvedAttackDelivery(new[] { delivery.Hits.Single(), delivery.Hits.Single() }, delivery.AffectedCells);
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            var plan = new CommandAttackPlan(input,NoRules,profile,actor,actor,environment,walls,selection.Selection,                repeated,engine,bindings,new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),                20,Rotation4.R0,false);
            var world = new CommandAttackProjection(input, environment, selection.BlockedContacts);
            var ledger = new CommandAttackLedger();
            AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>());
            return (world, ledger);
        }

        internal static CombatCommandTransition Run(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        internal static CharacterCombatFacts Final(CombatCommandTransition result) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == Target);

        internal static AttackHitResult Hit(CombatCommandTransition result) =>
            result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == Target);
    }
}
