using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class ImpactBoltTests
    {
        [Test]
        public void ImpactBolt_IsInstalledThroughDefaultRegistry()
        {
            Assert.DoesNotThrow(() => TraitRegistry.CreateDefault().GetRequiredRegistration((TraitKey)210));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ConfiguredDistance_SurvivesRegistryCodecStatusAndCommandCapture(int distance)
        {
            var registry = TraitRegistry.CreateDefault();
            var catalog = new UnitCatalog(new[] { Spec(EntityKey.Unit_Ballista, new ImpactBoltTraitSpec(distance)) }, registry);
            var definition = catalog.GetRequired(EntityKey.Unit_Ballista);
            var entry = registry.GetRequiredRegistration(TraitKey.ImpactBolt);
            var trait = (ImpactBoltTraitDefinition)definition.TraitDefinitions.Span[0];
            var state = entry.TraitRuntimeStateFactory.BuildRuntimeState(trait);
            var captured = TraitEffectStateCodec.Capture(state);
            Assert.That(TraitEffectStateCodec.Restore(captured), Is.TypeOf<ImpactBoltRuntimeState>());
            Assert.That(TraitEffectStateCodec.Capture(state.Clone()).ValueEquals(captured), Is.True);
            var status = (ImpactBoltTraitStatusSnapshot)new TraitEffectProvider(registry)
                .CreateStatuses(definition, new UnitTraitStateSet(new[] { state })).Single();
            Assert.That(status.DistanceCells, Is.EqualTo(distance));
            Assert.That(status.SameValues(new ImpactBoltTraitStatusSnapshot(distance)), Is.True);
            Assert.That(status.SameValues(new ImpactBoltTraitStatusSnapshot(distance + 1)), Is.False);
            var input = Input(distance);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var bindings = CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor));
            Assert.That(new CombatEffectEngine().EvaluateKnockback(bindings).Contributions.Single().DistanceCells,
                Is.EqualTo(distance));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Distance_MustBePositive(int distance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactBoltTraitSpec(distance));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactBoltTraitDefinition(distance));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactBoltTraitStatusSnapshot(distance));
        }

        [TestCase(-1, -1)]
        [TestCase(0, -1)]
        [TestCase(1, -1)]
        [TestCase(-1, 0)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        public void EightDirections_UseImpactCenterAndConfiguredDistance(int dx, int dy)
        {
            foreach (int distance in new[] { 1, 2 })
            foreach (var shooter in new[] { new CellCoord(0, 4), new CellCoord(4, 0) })
            {
                var input = Input(distance, dx, dy, shooter: shooter);
                var result = Run(input);
                Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4 + (distance + 1) * dx, 4 + (distance + 1) * dy)));
                Assert.That(Final(result, 2).Anchor, Is.EqualTo(new CellCoord(4, 4)));
                Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(900));
                Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(930));
                Assert.That(result.Attack.Value.PiercingShot, Is.Null);
                Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Moved && e.Final.EntityId.Value == 3),
                    Is.EqualTo(distance));
                // 중심 대상은 기록이 없고, 이웃은 착탄 칸 바깥 방향으로 요청 거리만큼 기록된다.
                var record = result.Attack.Value.Knockbacks.Single();
                Assert.That(record.TargetId.Value, Is.EqualTo(3));
                Assert.That(record.Step, Is.EqualTo(new CellCoord(dx, dy)));
                Assert.That(record.Stop, Is.EqualTo(AttackKnockbackStop.Completed));
                Assert.That(record.Path.Count, Is.EqualTo(distance));
                Assert.That(record.FinalAnchor, Is.EqualTo(Final(result, 3).Anchor.Value));
            }
        }

        [Test]
        public void EmptyCenter_StillDisplacesDeliveredNeighbor()
        {
            var result = Run(Input(centerTarget: false));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(6, 4)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(930));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ZeroFinalDamage_DoesNotSuppressDisplacement(bool defended)
        {
            var input = Input(method: Method(defended ? 70 : 0));
            if (defended) input = Replace(input, 3, c => HeldEnvironmentEffects.AcquireBadge(c, 41));
            var result = Run(input);
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(1000));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(6, 4)));
        }

        [Test]
        public void PhysicalDefense_RetainsExistingDamagePipeline()
        {
            var input = Replace(Input(), 3, c => CombatTraitTestFixtures.Stats(c, defense: 50));
            var result = Run(input);
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(965));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(6, 4)));
        }

        [TestCase(70, 965)]
        [TestCase(40, 980)]
        [TestCase(0, 1000)]
        public void AuthoredSplash_PreservesDefenseAndPreviewAfterLegacyRetirement(int percent, int neighborHealth)
        {
            var input = Input(method: Method(percent));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.Owner == Owner.Enemy
                ? CombatTraitTestFixtures.Stats(c, defense: 50) : c).ToArray());
            var result = Run(input);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projection);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(950));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(neighborHealth));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(6, 4)));
            Assert.That(Final(projection, 3).Health.CurrentHealth, Is.EqualTo(neighborHealth));
            Assert.That(Final(projection, 3).Anchor, Is.EqualTo(new CellCoord(6, 4)));
            Assert.That(result.Attack.Value.PiercingShot, Is.Null);
        }

        [Test]
        public void LethalTarget_DoesNotMove()
        {
            var result = Run(Replace(Input(), 3, c => CombatCommandTestData.With(c, hp: 50)));
            Assert.That(Final(result, 3).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved && e.Final.EntityId.Value == 3), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Heavy_MovesUnlessExplicitlyImmune(bool immune)
        {
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.CharacterKey == EntityKey.Unit_Demon
                ? CombatCommandTestData.With(c, category: CategorySnapshot.CreateStateless(CategoryKey.Heavy)) : c).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon
                ? new CombatAttackProfile(CopySpec(p.Definition, category: CategoryKey.Heavy,
                    behavior: new UnitBehaviorSpec(true, true, immune)), p.Method) : p).ToArray());
            var result = Run(input);
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(immune ? 5 : 6, 4)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.LessThan(1000));
        }

        [TestCase(1, 0, 6, 4)]
        [TestCase(1, 1, 6, 6)]
        public void OccupiedDestination_StopsWithoutCollisionDamage(int dx, int dy, int x, int y)
        {
            var input = Input(2, dx, dy);
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(Target(50, new CellCoord(x, y))).ToArray());
            var result = Run(input);
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4 + dx, 4 + dy)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(930));
            Assert.That(Final(result, 50).Health.CurrentHealth, Is.EqualTo(1000));
        }

        [Test]
        public void BoardEdge_StopsAtLastCell()
        {
            var result = Run(Input(4));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(7, 4)));
        }

        [Test]
        public void WallBlockedDelivery_HasNoHitsOrMovement()
        {
            var input = Input(occlusion: AttackOcclusionSpec.Default);
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71, new CellCoord(2, 4)));
            CombatCommandTransition result = null;
            Assert.DoesNotThrow(() => result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Events.Where(e => e.Kind == CombatEventKind.Moved), Is.Empty);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 3).Anchor, Is.EqualTo(new CellCoord(5, 4)));
        }

        [Test]
        public void LobbedDelivery_IgnoresInterveningWall()
        {
            var input = WallMovementTests.WithWalls(Input(), WallStateLifecycleTests.Wall(71, new CellCoord(2, 4)));
            Assert.That(Final(Run(input), 3).Anchor, Is.EqualTo(new CellCoord(6, 4)));
        }

        [Test]
        public void Allies_AreKnockedOutward_StructuresKeepPosition_DamageEligibilityUnchanged()
        {
            var input = Input();
            input = Replace(input, 10, c => CombatCommandTestData.With(c, position: new CellCoord(4, 5)));
            var ally = input.Characters.Single(c => c.EntityId.Value == 10);
            var barrel = ObstacleReactionTests.Barrel(40, new CellCoord(3, 4), 1000, 1);
            input = ObstacleReactionTests.Copy(input, structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var control = WithoutTrait(input);
            var result = Run(input);
            var ordinary = Run(control);
            // 충격 볼트는 진영과 무관하게 맞은 유닛을 민다. 중심 (4,4)에서 아래쪽 아군은 한 칸 아래로 간다.
            Assert.That(Final(result, 10).Anchor, Is.EqualTo(ally.Anchor.Value + new CellCoord(0, 1)));
            Assert.That(Final(result, 10).Health.CurrentHealth, Is.EqualTo(Final(ordinary, 10).Health.CurrentHealth));
            Assert.That(result.FinalState.Structures.Single().Anchor, Is.EqualTo(barrel.Structure.Anchor));
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(ordinary.FinalState.Structures.Single().Health.CurrentHealth));
            // 중심 유닛과 구조물은 넉백 기록이 없다.
            Assert.That(result.Attack.Value.Knockbacks.Select(k => k.TargetId.Value), Is.EqualTo(new[] { 3, 10 }));
        }

        [TestCase(EntityKey.Unit_Akimbo, true)]
        [TestCase(EntityKey.Unit_Ballista, true)]
        [TestCase(EntityKey.Unit_Ballista, false)]
        public void InstalledTrait_DeterminesBehaviorRegardlessOfUnitIdentity(EntityKey key, bool installed)
        {
            var result = Run(Input(actorKey: key, installed: installed));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(installed ? 6 : 5, 4)));
        }

        [TestCase(TargetingSplash.Cross, 40)]
        [TestCase(TargetingSplash.Diagonal, 70)]
        [TestCase(TargetingSplash.Radial, 70)]
        [TestCase(TargetingSplash.None, 0)]
        public void SplashConfiguration_ControlsDeliveredMembership(TargetingSplash splash, int percent)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, splash, percent);
            foreach (var offset in new[] { new CellCoord(1, 0), new CellCoord(1, 1) })
            {
                var result = Run(Input(2, offset.X, offset.Y, method: method));
                bool delivered = splash == TargetingSplash.Radial ||
                    (splash == TargetingSplash.Cross && offset.Y == 0) || (splash == TargetingSplash.Diagonal && offset.Y == 1);
                Assert.That(Final(result, 3).Anchor, Is.EqualTo(delivered
                    ? new CellCoord(7, 4 + offset.Y * 3) : new CellCoord(5, 4 + offset.Y)));
                Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(delivered ? 1000 - percent : 1000));
            }
        }

        [TestCase(TargetingShape.CardinalLine, TargetingChoiceAngle.Front, 6)]
        [TestCase(TargetingShape.RadialSquare, TargetingChoiceAngle.Unrestricted, 4)]
        [TestCase(TargetingShape.DiagonalLine, TargetingChoiceAngle.Unrestricted, 2)]
        public void Profile_AllowsAlternateShapeAngleAndRange(TargetingShape shape, TargetingChoiceAngle angle, int range)
        {
            var method = new TargetingMethodSpec(shape, TargetingChoiceMode.Single, TargetingSplash.Cross, 40, angle);
            Assert.DoesNotThrow(() => new CombatAttackProfile(CopySpec(Spec(EntityKey.Unit_Ballista, new ImpactBoltTraitSpec(2)), range: range), method));
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void Profile_RejectsCenterlessModes(TargetingChoiceMode choice)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(Spec(EntityKey.Unit_Ballista, new ImpactBoltTraitSpec(1)), method));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Profile_RejectsConflictingDisplacementOrigins(bool piercing)
        {
            TraitSpec other = piercing ? new PiercingFalloffTraitSpec(30, int.MaxValue) : new KnockbackTraitSpec(1);
            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(Spec(EntityKey.Unit_Ballista,
                new ImpactBoltTraitSpec(1), other), new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)));
        }

        [Test]
        public void ReflectionDeath_SuppressesDisplacementThroughSharedExecutor()
        {
            var input = ThornHeartTestData.Input(hp: 2, splash: true,
                traits: new TraitSpec[] { new ImpactBoltTraitSpec(1) }, statuses: new TraitStatusSnapshot[] { new ImpactBoltTraitStatusSnapshot(1) });
            var result = Run(input);
            Assert.That(Final(result, 1).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Count(e => e.Reflection != null), Is.EqualTo(1));
            Assert.That(result.Events.Where(e => e.Kind == CombatEventKind.Moved), Is.Empty);
            Assert.That(result.Attack.Value.Knockbacks, Is.Empty);
        }

        [Test]
        public void ThornAndMineTraversal_PreservesDeferredFinalArrival()
        {
            var input = Input(2, 0, 1);
            input = CombatCommandTestData.With(input, state: CombatCommandEffectTests.WithFields(input,
                new ThornFieldCellSnapshot(new CellCoord(4, 6), 3, 6)));
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(4, 7)));
            var result = Run(input, MineEnvironmentTests.Rules);
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4, 7)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(917));
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId.Value == 3).TargetHealthAfter, Is.EqualTo(924));
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            Assert.That(result.FinalState.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.False);
        }

        [Test]
        public void RepeatedPreview_MatchesCommitAndLeavesInputUnchanged()
        {
            var input = Input(2, 0, -1);
            for (int i = 0; i < 3; i++)
            {
                var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projection);
                Assert.That(preview.CanAttack, Is.True);
                Assert.That(Final(projection, 3).Anchor, Is.EqualTo(new CellCoord(4, 1)));
                Assert.That(Final(projection, 3).Health.CurrentHealth, Is.EqualTo(930));
            }
            var result = Run(input);
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4, 1)));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 3).Anchor, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 3).Health.CurrentHealth, Is.EqualTo(1000));
        }

        [Test]
        public void Contribution_RejectsUnknownModeAndConflictingOrigins()
        {
            var bolt = TraitEffectOrigin.Create(new EntityId(1), TraitKey.ImpactBolt);
            var knockback = TraitEffectOrigin.Create(new EntityId(1), TraitKey.Knockback);
            Assert.Throws<ArgumentOutOfRangeException>(() => new EffectKnockbackContribution(bolt, 1, (EffectKnockbackMode)99));
            var bindings = new[] {
                new EffectRuleBinding(bolt, new ImpactBoltEffectRule(new ImpactBoltTraitDefinition(1),
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules.ImpactBoltTraitRule()), new ImpactBoltEffectStateSnapshot()),
                new EffectRuleBinding(knockback, new KnockbackEffectRule(new KnockbackTraitDefinition(2),
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules.KnockbackTraitRule()), new KnockbackEffectStateSnapshot()) };
            Assert.Throws<ArgumentException>(() => new CombatEffectEngine().EvaluateKnockback(bindings));
        }

        [Test]
        public void EffectBinding_RejectsMismatchedOriginAndState()
        {
            var rule = (IEffectKnockbackRule)TraitRegistry.CreateDefault().GetRequiredRegistration(TraitKey.ImpactBolt)
                .EffectRuleFactory(new ImpactBoltTraitDefinition(2));
            var bolt = TraitEffectOrigin.Create(new EntityId(1), TraitKey.ImpactBolt);
            var other = TraitEffectOrigin.Create(new EntityId(1), TraitKey.Knockback);
            Assert.Throws<ArgumentException>(() => rule.EvaluateKnockback(new EffectRuleInput(other, new ImpactBoltEffectStateSnapshot())));
            Assert.Throws<ArgumentException>(() => rule.EvaluateKnockback(new EffectRuleInput(bolt, new KnockbackEffectStateSnapshot())));
            var input = Input();
            input = Replace(input, 1, c => CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) }));
            Assert.Throws<InvalidOperationException>(() => CombatCommandEffects.Capture(input.Characters.Single(c => c.EntityId == input.ActorId),
                input.Profiles.Single(p => p.Definition.Key == EntityKey.Unit_Ballista)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingCenter_IsErrorOnlyWhenHitsWereDelivered(bool delivered)
        {
            var (original, world, ledger) = CombatItemDirectDamageTests.Prepare(Input());
            var s = original.Selection;
            var reachable = new AttackCellSelection(null, Array.Empty<CellCoord>(), Array.Empty<CellCoord>());
            var selection = new ResolvedAttackSelection(s.Cells, reachable, s.Targets, s.EligibleCharacters, s.EligibleStructures, s.Policy);
            var delivery = delivered ? original.Delivery : new ResolvedAttackDelivery(Array.Empty<DeliveredAttackHit>(), Array.Empty<CellCoord>());
            var plan = new CommandAttackPlan(original.Input,original.Rules,original.Profile,original.OriginalActor,original.Actor,                original.Environment,original.Walls,selection,delivery,original.Engine,original.Bindings,original.Pipeline,                original.CommonDamage,original.Direction,original.PublicPreview);
            var arrival = world.CreateArrivalProjection(plan);
            if (delivered)
                Assert.Throws<InvalidOperationException>(() => AttackKnockbackCalculator.Resolve(plan, world, ledger, arrival));
            else
                Assert.That(AttackKnockbackCalculator.Resolve(plan, world, ledger, arrival), Is.Empty);
        }

        [Test]
        public void LedgerOnlyVictim_IsNotDisplaced_AndDuplicateDeliveryMovesOnce()
        {
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(Target(50, new CellCoord(2, 2))).ToArray());
            var (original, world, ledger) = CombatItemDirectDamageTests.Prepare(input);
            var hit = original.Delivery.Hits.Single(h => h.TargetId.Value == 3);
            var delivery = new ResolvedAttackDelivery(new[] { hit, hit }, original.Delivery.AffectedCells);
            var plan = new CommandAttackPlan(original.Input,original.Rules,original.Profile,original.OriginalActor,original.Actor,                original.Environment,original.Walls,original.Selection,delivery,original.Engine,original.Bindings,original.Pipeline,                original.CommonDamage,original.Direction,original.PublicPreview);
            ledger.AddThornVictim(new EntityId(50));
            var moved = AttackKnockbackCalculator.Resolve(plan, world, ledger, world.CreateArrivalProjection(plan));
            Assert.That(world.Characters[new EntityId(50)].Anchor, Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(world.Characters[new EntityId(3)].Anchor, Is.EqualTo(new CellCoord(6, 4)));
            Assert.That(moved.Count, Is.EqualTo(1));
            Assert.That(moved[0].CompletedSteps, Is.EqualTo(1));
        }

        [Test]
        public void OrdinaryKnockback_RetainsObstacleMovement()
        {
            var input = Input(centerTarget: false);
            input = Replace(input, 1, c => CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) }));
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId.Value != 3).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Ballista
                    ? new CombatAttackProfile(CopySpec(p.Definition, traits: new TraitSpec[] { new KnockbackTraitSpec(1) }), p.Method) : p).ToArray());
            var barrel = ObstacleReactionTests.Barrel(40, new CellCoord(4, 4), 1000, 1);
            input = ObstacleReactionTests.Copy(input, structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var result = Run(input);
            Assert.That(result.FinalState.Structures.Single().Anchor, Is.EqualTo(new CellCoord(5, 4)));
            var record = result.Attack.Value.Knockbacks.Single(k => k.TargetId.Value == 40);
            Assert.That(record.TargetKind, Is.EqualTo(AttackKnockbackTargetKind.Structure));
            Assert.That(record.Stop, Is.EqualTo(AttackKnockbackStop.Completed));
            Assert.That(record.Step, Is.EqualTo(CellCoord.Right));
            Assert.That(record.Path, Is.EqualTo(new[] { new CellCoord(5, 4) }));
        }

        [Test]
        public void MineKillsObjective_StopsLaterTargetsBeforeTheirThorns()
        {
            var input = Input(2, 0, 1);
            var bossId = new EntityId(8);
            var boss = CombatCommandTestData.With(CombatCommandTestData.Character(8, EntityKey.Unit_Gravekeeper, new CellCoord(5, 6), Owner.Enemy), hp: 7);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Concat(new[] { boss, Target(9, new CellCoord(3, 4)) }).ToArray(),
                profiles: input.Profiles.Append(new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 1), Method())).ToArray());
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(4, 6)));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.IsNight,s.Structures,new[] { new ThornFieldCellSnapshot(new CellCoord(2, 4), 3, 100) },                s.Turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,BattleObjective.DesignatedBoss(bossId),s.Allocation));
            var result = Run(input, MineEnvironmentTests.Rules);
            Assert.That(Final(result, 8).Health.IsDead, Is.True);
            Assert.That(result.Outcome.Reason, Is.EqualTo(StageOutcomeReason.DesignatedBossDefeated));
            Assert.That(Final(result, 9).Anchor, Is.EqualTo(new CellCoord(3, 4)));
            Assert.That(Final(result, 9).Health.CurrentHealth, Is.EqualTo(930));
            Assert.That(result.FinalState.ThornFields.Single().Cell, Is.EqualTo(new CellCoord(2, 4)));
            Assert.That(result.Events.OfType<CombatOutcomeEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void FullRing_MovesEachEnemyOutwardInEntityIdOrder()
        {
            var input = Input(2);
            var ring = new[] { Target(11, new CellCoord(3, 3)), Target(12, new CellCoord(4, 3)), Target(13, new CellCoord(5, 3)),
                Target(14, new CellCoord(3, 4)), Target(15, new CellCoord(3, 5)), Target(16, new CellCoord(4, 5)), Target(17, new CellCoord(5, 5)) };
            input = CombatCommandTestData.With(input, characters: input.Characters.Concat(ring).Reverse().ToArray());
            var result = Run(input);
            Assert.That(new[] { 3, 11, 12, 13, 14, 15, 16, 17 }.Select(id => Final(result, id).Anchor), Is.EqualTo(new CellCoord?[] {
                new CellCoord(7, 4), new CellCoord(1, 1), new CellCoord(4, 1), new CellCoord(7, 1), new CellCoord(1, 4),
                new CellCoord(1, 7), new CellCoord(4, 7), new CellCoord(7, 7) }));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved)
                .Select(e => e.Final.EntityId.Value).Distinct(), Is.EqualTo(new[] { 3, 11, 12, 13, 14, 15, 16, 17 }));
        }

        [Test]
        public void EligibilityRule_ExcludesCenterFootprintDeadAndOffFieldUnits()
        {
            var target = Target(3, new CellCoord(3, 4));
            var footprint = target.WithPlacement(CharacterRuntimeArea.Field, null, target.Anchor, target.Rotation,
                new[] { new CellCoord(3, 4), new CellCoord(4, 4) });
            var waiting = target.WithPlacement(CharacterRuntimeArea.EnemyWaiting, null, null, null, Array.Empty<CellCoord>());
            var dead = target.With(health: new HealthStateSnapshot(0, 1000));
            var center = new CellCoord(4, 4);
            foreach (var excluded in new[] { footprint, waiting, dead })
            {
                Assert.That(ImpactBoltTraitRule.TryResolveStep(excluded, center, out var step), Is.False);
                Assert.That(step, Is.EqualTo(CellCoord.Zero));
            }
            Assert.Throws<ArgumentNullException>(() => ImpactBoltTraitRule.TryResolveStep(null, center, out _));
            Assert.That(ImpactBoltTraitRule.TryResolveStep(target, center, out var outward), Is.True);
            Assert.That(outward, Is.EqualTo(new CellCoord(-1, 0)));
            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(new UnitSpec(EntityKey.Unit_Ballista, UnitRole.Regular,
                2, 1, new[] { CellCoord.Zero, CellCoord.Right }, 100, MoveMethod.LiftAndPlace, 4, CategoryKey.Cannon, 100, 0, 1,
                new TraitSpec[] { new ImpactBoltTraitSpec(1) }), Method()));
        }

        [TestCase(Owner.Enemy)]
        [TestCase(Owner.Player)]
        public void EligibilityRule_PushesEveryOwner(Owner targetOwner)
        {
            Assert.That(ImpactBoltTraitRule.TryResolveStep(Target(3, new CellCoord(5, 4), targetOwner),
                new CellCoord(4, 4), out var step), Is.True);
            Assert.That(step, Is.EqualTo(new CellCoord(1, 0)));
        }

        internal static TargetingMethodSpec Method(int percent = 70) =>
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, percent);

        internal static UnitSpec Spec(EntityKey key, params TraitSpec[] traits) =>
            CopySpec(CombatCommandTestData.Spec(key, 100, CategoryKey.Cannon, traits), occlusion: AttackOcclusionSpec.Lobbed);

        internal static UnitSpec CopySpec(UnitSpec s, CategoryKey? category = null, UnitBehaviorSpec behavior = null,
            AttackOcclusionSpec occlusion = null, int? range = null, TraitSpec[] traits = null) =>
            new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth,
                s.MoveMethod, range ?? s.AttackRange, category ?? s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                traits ?? s.Traits.ToArray(), s.OwnerEligibility, occlusion ?? s.Occlusion, behavior ?? s.Behavior, s.KillReward);

        internal static CharacterCombatFacts Target(int id, CellCoord cell, Owner owner = Owner.Enemy)
        {
            var c = CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, owner);
            return CharacterSettlementCalculator.Copy(c, new HealthStateSnapshot(1000, 1000), c.Stats);
        }

        internal static CombatCommandInput Input(int distance = 1, int dx = 1, int dy = 0, bool centerTarget = true,
            CellCoord? shooter = null, TargetingMethodSpec method = null, EntityKey actorKey = EntityKey.Unit_Ballista,
            bool installed = true, AttackOcclusionSpec occlusion = null)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, actorKey, shooter ?? new CellCoord(0, 4), Owner.Player, 100),
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon), traits: installed
                    ? new TraitStatusSnapshot[] { new ImpactBoltTraitStatusSnapshot(distance) } : Array.Empty<TraitStatusSnapshot>());
            var characters = new List<CharacterCombatFacts> { actor,
                CombatCommandTestData.With(seed.Characters[1], position: new CellCoord(0, 0)), Target(99, new CellCoord(7, 0)),
                Target(3, new CellCoord(4 + dx, 4 + dy)) };
            if (centerTarget) characters.Add(Target(2, new CellCoord(4, 4)));
            var profiles = seed.Profiles.Where(p => p.Definition.Key != EntityKey.Unit_Akimbo).Append(
                new CombatAttackProfile(CopySpec(Spec(actorKey, installed ? new TraitSpec[] { new ImpactBoltTraitSpec(distance) }
                    : Array.Empty<TraitSpec>()), occlusion: occlusion), method ?? Method())).ToArray();
            return new CombatCommandInput(1, CombatCommandKind.Attack, actor.EntityId, AttackAim.ForCell(new CellCoord(4, 4)),
                seed.State, characters, profiles, seed.Main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0), seed.Souls, seed.Artifacts,
                battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
        }

        internal static CombatCommandInput WithoutTrait(CombatCommandInput input) => CombatCommandTestData.With(
            Replace(input, 1, c => CombatCommandTestData.With(c, traits: Array.Empty<TraitStatusSnapshot>())),
            profiles: input.Profiles.Select(p => p.Definition.Key == input.Characters.Single(c => c.EntityId == input.ActorId).CharacterKey
                ? new CombatAttackProfile(CopySpec(p.Definition, traits: Array.Empty<TraitSpec>()), p.Method) : p).ToArray());

        internal static CombatCommandInput Replace(CombatCommandInput input, int id, Func<CharacterCombatFacts, CharacterCombatFacts> change) =>
            CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == id ? change(c) : c).ToArray());

        internal static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == id);

        internal static CombatCommandTransition Run(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules = null)
        {
            CombatCommandTransition result = null;
            Assert.DoesNotThrow(() => result = CombatCommandCalculator.Calculate(input, rules ?? Array.Empty<IMovementTileRule>()));
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }
    }
}
