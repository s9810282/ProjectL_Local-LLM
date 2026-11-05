using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class CliffFallTests
    {
        private static readonly CellCoord Cell = new CellCoord(4, 3);

        [TestCase(0, 20, 0)]
        [TestCase(75, 20, 5)]
        [TestCase(0, 150, 0)]
        public void PreviewFromHit_UsesSettledHealthAndPreservesDamageBreakdown(int finalHealth, int damage, int normalization)
        {
            var hit = new AttackHitResult(new EntityId(2), 100, finalHealth, AttackTargetRole.Additional, 70, normalization);
            var preview = new AttackTargetPreview(hit, damage, new[] { Cell }, damage + 2, 1, 1, 2,
                secondaryDamage: 3);
            Assert.That(preview.TargetId, Is.EqualTo(hit.TargetId));
            Assert.That(preview.TargetHealthBefore, Is.EqualTo(hit.TargetHealthBefore));
            Assert.That(preview.TargetHealthAfter, Is.EqualTo(finalHealth));
            Assert.That(preview.TargetKilled, Is.EqualTo(hit.TargetKilled));
            Assert.That(preview.ExpectedDamage, Is.EqualTo(damage));
            Assert.That(preview.SecondaryDamage, Is.EqualTo(3));
            Assert.That(preview.DamageBeforeMatchup, Is.EqualTo(damage + 2));
            Assert.That(preview.IncomingReduction, Is.EqualTo(2));
            Assert.That(preview.Role, Is.EqualTo(hit.Role));
            Assert.That(preview.AppliedSplashPercent, Is.EqualTo(70));
            Assert.That(preview.HealthNormalizationLoss, Is.EqualTo(normalization));
        }

        [Test]
        public void LegacyPreviewConstructor_StillDerivesHealthFromDamageAndNormalization()
        {
            var preview = new AttackTargetPreview(new EntityId(2), 20, 100, new[] { Cell }, 20, 1, 1, 0,
                healthNormalizationLoss: 5);
            Assert.That(preview.TargetHealthAfter, Is.EqualTo(75));
            Assert.That(preview.TargetKilled, Is.False);
        }

        [Test]
        public void PreviewFromHit_RejectsDefaultUninitializedResult()
        {
            Assert.Throws<ArgumentException>(() => new AttackTargetPreview(default(AttackHitResult), 20, new[] { Cell },
                20, 1, 1, 0));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Fall_BypassesHealthAndHeldProtection_ClearsHeldWithoutDrops(bool settleImmediately)
        {
            var seed = SoulReady(CombatCommandTestData.Input());
            var victim = seed.Characters.Single(c => c.EntityId.Value == 2);
            victim = CombatCommandCalculator.Copy(victim, new HealthStateSnapshot(int.MaxValue, int.MaxValue), victim.Stats);
            victim = HeldEnvironmentEffects.GrantSupplies(HeldEnvironmentEffects.AcquireBadge(
                HeldEnvironmentEffects.AcquireFlag(victim, new FlagSourceFact(500, Cell, 3, 7, true, new[] { Owner.Player })), 501), 20);
            var loot = new FlagSourceFact(72, Cell, 1, 1, true, new[] { Owner.Player });
            var input = WithEnvironment(CombatCommandTestData.With(seed,
                seed.Characters.Select(c => c.EntityId == victim.EntityId ? victim : c).ToArray()), Ground(), loot);
            var projection = Projection(input);
            Assert.That(CliffFallCalculator.ResolveAt(projection, victim.EntityId, Cell, settleImmediately), Is.True);
            Assert.That(CliffFallCalculator.ResolveAt(projection, victim.EntityId, Cell, settleImmediately), Is.False);
            projection.ResolveDeathBoundary();
            var result = projection.Complete();
            var final = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == victim.EntityId);
            Assert.That(final.Health.IsDead, Is.True);
            Assert.That(final.Area, Is.EqualTo(CharacterRuntimeArea.Excluded));
            Assert.That(final.OccupiedCells, Is.Empty);
            Assert.That(final.HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty), Is.True);
            Assert.That(result.Events.OfType<CombatFallEvent>().Single().TerrainSourceId, Is.EqualTo(71));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => !e.Expected.Health.IsDead && e.Final.Health.IsDead), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatObjectCreatedEvent>(), Is.Empty);
            Assert.That(result.Field.Final.SameValues(input.Souls), Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 72).SameValues(loot), Is.True);
            Assert.That(input.Characters.Single(c => c.EntityId == victim.EntityId).Health.IsDead, Is.False);
        }

        [Test]
        public void FallWithoutLocalSettlement_FinishStillSuppressesSoul()
        {
            var input = WithEnvironment(SoulReady(CombatCommandTestData.Input()), Ground());
            var projection = Projection(input);
            Assert.That(CliffFallCalculator.ResolveAt(projection, new EntityId(2), Cell, false), Is.True);
            Assert.That(projection.Complete().Field.Final.Entries, Is.Empty);
        }

        [TestCase(StaticTerrainKind.WeakGround, true, true)]
        [TestCase(StaticTerrainKind.Cliff, false, true)]
        [TestCase(StaticTerrainKind.Cliff, true, false)]
        public void IneligibleTerrain_DoesNotFall(StaticTerrainKind kind, bool active, bool classified)
        {
            var input = WithEnvironment(CombatCommandTestData.Input(), Ground(kind, active));
            if (!classified) input = Copy(input, EnvironmentSemanticBindings.Empty);
            var projection = Projection(input);
            Assert.That(CliffFallCalculator.ResolveAt(projection, new EntityId(2), Cell), Is.False);
            Assert.That(projection.Events, Is.Empty);
        }

        [Test]
        public void WrongCell_DoesNotFall()
        {
            var projection = Projection(WithEnvironment(CombatCommandTestData.Input(), Ground()));
            Assert.That(CliffFallCalculator.ResolveAt(projection, new EntityId(3), Cell), Is.False);
        }
        [Test]
        public void BarrelFall_KeepsDeadFactAndBinding_WithoutReactionOrBlast()
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var structure = new StructureCombatFacts(new EntityId(51), new StructureSpec(EntityKey.Structure_PowderBarrel,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100), Cell, Rotation4.R0, new[] { Cell }, Owner.Neutral);
            var s = seed.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.IsNight,                new[] { structure },s.ThornFields,s.Turn,new MovementEnvironmentFacts(new[] { Ground() }));
            var input = new CombatCommandInput(seed.Number, seed.Kind, seed.ActorId, seed.Aim, state, seed.Characters,
                seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                environmentBindings: new EnvironmentSemanticBindings(new[] { 71L }, Array.Empty<long>(), new[] { structure.EntityId }),
                obstacleContents: new[] { new ObstacleContentProfile(structure.EntityId, ObstacleContentKind.PowderBarrel, 1000) },
                expectedBattleInstanceId: seed.ExpectedBattleInstanceId, expectedControllerTurnSequence: seed.ExpectedControllerTurnSequence,
                battleInstanceId: seed.BattleInstanceId);
            var projection = Projection(input);
            Assert.That(CliffFallCalculator.ResolveAt(projection, structure.EntityId, Cell), Is.True);
            var result = projection.Complete();
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(projection.Bindings.ObstacleEntityIds, Has.Member(structure.EntityId));
            Assert.That(result.Events.OfType<CombatStructureEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>(), Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Field.Final.Entries, Is.Empty);
        }

        [TestCase(StaticTerrainKind.Cliff)]
        [TestCase(StaticTerrainKind.WeakGround)]
        public void AttackKnockback_ResolvesTerrainBeforeThornsAndObjects_AndPreviewAgrees(StaticTerrainKind kind)
        {
            var seed = Knockback();
            var destination = new CellCoord(5, 3);
            var ground = Ground(kind, cell: destination);
            var badge = new SheriffBadgeSourceFact(72, destination, 1, true, new[] { Owner.Player });
            var input = WithEnvironment(seed, ground, badge);
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth,s.BoardHeight,                s.Phase,s.IsNight,s.Structures,new[] { new ThornFieldCellSnapshot(destination, 5, 7) },s.Turn,s.Environment));
            var attackPreview = CombatCommandCalculator.Preview(input, TornadoEnvironmentTests.Rules, out var preview);
            var result = CombatCommandCalculator.Calculate(input, TornadoEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var victim = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == 2);
            Assert.That(victim.Health.IsDead, Is.EqualTo(kind == StaticTerrainKind.Cliff));
            Assert.That(preview.FinalState.Environment.SameValues(result.FinalState.Environment.VisibleTo(Owner.Player)), Is.True);
            Assert.That(CombatCommandComposition.Project(preview).Characters.Single(c => c.EntityId.Value == 2).Health.CurrentHealth,
                Is.EqualTo(victim.Health.CurrentHealth));
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId.Value == 2).TargetHealthAfter, Is.EqualTo(victim.Health.CurrentHealth));
            Assert.That(attackPreview.Targets.ToArray().Single(h => h.TargetId.Value == 2).TargetHealthAfter, Is.EqualTo(victim.Health.CurrentHealth));
            Assert.That(input.State.Environment.Tiles.OfType<StaticTerrainFact>().Single().EntryCount, Is.Zero);
            if (kind == StaticTerrainKind.Cliff)
            {
                var targetPreview = attackPreview.Targets.ToArray().Single(t => t.TargetId.Value == 2);
                Assert.That(targetPreview.TargetKilled, Is.True);
                Assert.That(targetPreview.ExpectedDamage, Is.EqualTo(20));
                Assert.That(targetPreview.SecondaryDamage, Is.Zero);
                Assert.That(result.Events.OfType<CombatFallEvent>().Count(), Is.EqualTo(1));
                Assert.That(result.FinalState.ThornFields.Count, Is.EqualTo(1));
                Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == badge.Id).Active, Is.True);
                Assert.That(result.Field.Final.Entries, Is.Empty);
            }
            else
            {
                // 넉백 진입도 센다(약한 지반 설계 WG1, Q-WG2 B, 2026-10-27 사용자 허락으로 기대값 변경). 한도 2 미만이라 무너지지 않는다.
                Assert.That(result.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Single().EntryCount, Is.EqualTo(1));
                Assert.That(victim.Anchor, Is.EqualTo(destination));
                Assert.That(victim.ActionUse, Is.EqualTo(input.Characters.Single(c => c.EntityId == victim.EntityId).ActionUse));
                Assert.That(result.Events.OfType<CombatMovementSourceEvent>().Any(e => e.Final.Id == 71), Is.True);
            }
        }

        [TestCase(StaticTerrainKind.Cliff)]
        public void EnvironmentForcedEntry_UsesTerrainOnce_AndStopsBeforeLaterEffects(StaticTerrainKind kind)
        {
            var destination = new CellCoord(1, 1);
            var input = WithEnvironment(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)),
                new PushSource(), Ground(kind, cell: destination),
                new SheriffBadgeSourceFact(72, destination, 1, true, new[] { Owner.Player }));
            input = CombatCommandTestData.With(input, souls: new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul"), destination, 5, 2) }));
            var rules = new IMovementTileRule[] { new PushRule(), new SheriffBadgeMovementTileRule() };
            var preview = CombatMovementCalculator.Preview(input, rules);
            var result = CombatCommandCalculator.Calculate(input, rules);
            var actor = MovementTestData.Actor(result);
            Assert.That(actor.Health.IsDead, Is.EqualTo(kind == StaticTerrainKind.Cliff));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(preview.ProjectedActor.Health.CurrentHealth, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(result.Events.OfType<CombatFallEvent>().Count(), Is.EqualTo(kind == StaticTerrainKind.Cliff ? 1 : 0));
            Assert.That(result.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Single().EntryCount,
                Is.EqualTo(kind == StaticTerrainKind.WeakGround ? 1 : 0));
            Assert.That(actor.HeldEnvironment.BadgeAvailable, Is.EqualTo(kind == StaticTerrainKind.WeakGround));
            Assert.That(result.ConsumedSoulIds.Count, Is.EqualTo(kind == StaticTerrainKind.Cliff ? 0 : 1));
            if (kind == StaticTerrainKind.Cliff) Assert.That(result.Field.Final.SameValues(input.Souls), Is.True);
        }

        [Test]
        public void AttackDistanceAboveOne_IsSupported()
        {
            // 거리 N 넉백 도입(계획 T4.1)으로 거리 1 초과 거절을 제거했다. 세부 경로 규칙은 KnockbackPathTests가 검증한다.
            var input = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(2), new KnockbackTraitStatusSnapshot(2));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        internal static CombatCommandInput Knockback()
        {
            var input = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            return CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 4).ToArray());
        }

        [Test]
        public void FallThenKillChain_PreservesCauseWithoutDeferredSoulDrops()
        {
            var input = SoulReady(Knockback());
            var actor = CombatCommandTestData.With(input.Characters[0], traits: new TraitStatusSnapshot[] {
                new KnockbackTraitStatusSnapshot(1), new KillChainAttackTraitStatusSnapshot() });
            var chainVictim = CombatCommandTestData.With(input.Characters.Single(c => c.EntityId.Value == 3),
                hp: 10, position: new CellCoord(6, 6));
            var profiles = input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey ? new CombatAttackProfile(
                CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, new KnockbackTraitSpec(1), new KillChainAttackTraitSpec()),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)) : p).ToArray();
            input = WithEnvironment(CombatCommandTestData.With(input,
                input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c.EntityId == chainVictim.EntityId ? chainVictim : c).ToArray(), profiles: profiles),
                Ground(cell: new CellCoord(5, 3)));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var characters = CombatCommandComposition.Project(result).Characters;
            Assert.That(characters.Single(c => c.EntityId.Value == 2).Health.IsDead, Is.True);
            Assert.That(characters.Single(c => c.EntityId.Value == 3).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatFallEvent>().Select(e => e.EntityId.Value), Is.EqualTo(new[] { 2 }));
            Assert.That(result.Field.Final.Entries, Is.Empty);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(2));
        }

        private sealed class PushSource : MovementTileFact
        {
            internal PushSource() : base(70, CellCoord.Right, "cliff-push-fixture", 0, true, new[] { Owner.Player }) { }
            public override bool SameValues(MovementTileFact other) => other is PushSource && SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class PushRule : IMovementTileRule
        {
            public string Key => "cliff-push-fixture";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) =>
                input.Phase == MovementEffectPhase.Hazard ? new MovementStepEffects(forcedMovement: new[] {
                    new MovementForcedMoveRequest(input.MoverId, Rotation4.R0, 1) }) : new MovementStepEffects();
        }

        private static CombatCommandInput SoulReady(CombatCommandInput input)
        {
            var s = input.State;
            return CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth,s.BoardHeight,                s.Phase,true,s.Structures,s.ThornFields,s.Turn,s.Environment),
                artifacts: new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) }));
        }

        internal static StaticTerrainFact Ground(StaticTerrainKind kind = StaticTerrainKind.Cliff, bool active = true,
            CellCoord? cell = null) => new(71, cell ?? Cell, kind, active, new[] { Owner.Player, Owner.Enemy },
                entryThreshold: kind == StaticTerrainKind.WeakGround ? 2 : 0);

        internal static MovementProjection Projection(CombatCommandInput input) =>
            MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());

        internal static CombatCommandInput WithEnvironment(CombatCommandInput input, params MovementTileFact[] sources)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.IsNight,                s.Structures,s.ThornFields,s.Turn,new MovementEnvironmentFacts(sources));
            return Copy(CombatCommandTestData.With(input, state: state), new EnvironmentSemanticBindings(
                sources.OfType<StaticTerrainFact>().Select(t => t.Id).ToArray(), sources.OfType<ObjectSourceFact>().Select(t => t.Id).ToArray(), input.EnvironmentBindings.ObstacleEntityIds));
        }

        private static CombatCommandInput Copy(CombatCommandInput i, EnvironmentSemanticBindings bindings) =>
            new(i.Number, i.Kind, i.ActorId, i.Aim, i.State, i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts,
                i.Souls, i.Artifacts, i.TargetCell, i.Rotation, i.Battle, i.ProcessingKind, i.Path,
                environmentBindings: bindings, obstacleContents: i.ObstacleContents,
                expectedBattleInstanceId: i.ExpectedBattleInstanceId, expectedControllerTurnSequence: i.ExpectedControllerTurnSequence,
                battleInstanceId: i.BattleInstanceId);
    }
}
