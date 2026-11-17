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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 작렬(설계 2026-11-10). 부딪힘 피해는 같은 공격에서 작렬만 뺀 대조(넉백 없음)와의 체력 차이로 본다.
    /// 해머(1)는 (2,4)에서 오른쪽 (3,4)의 적(3)을 친다. 넉백 방향은 (1,0), 기본 거리 3, 판은 8×8이다.
    /// </summary>
    public sealed class CrashImpactTests
    {
        private static readonly CellCoord TargetCell = new CellCoord(3, 4);

        [Test]
        public void CrashImpact_IsInstalledThroughDefaultRegistry_AndSurvivesCodecAndStatus()
        {
            var registry = TraitRegistry.CreateDefault();
            var catalog = new UnitCatalog(new[] { HammerSpec(new CrashImpactTraitSpec(3, 10, 20)) }, registry);
            var definition = catalog.GetRequired(EntityKey.Unit_DynamiteHammer);
            var trait = (CrashImpactTraitDefinition)definition.TraitDefinitions.Span[0];
            var state = registry.GetRequiredRegistration(TraitKey.CrashImpact).TraitRuntimeStateFactory.BuildRuntimeState(trait);
            var captured = TraitEffectStateCodec.Capture(state);
            Assert.That(TraitEffectStateCodec.Restore(captured), Is.TypeOf<CrashImpactRuntimeState>());
            var status = (CrashImpactTraitStatusSnapshot)new TraitEffectProvider(registry)
                .CreateStatuses(definition, new UnitTraitStateSet(new[] { state })).Single();
            Assert.That((status.DistanceCells, status.WallDamagePercent, status.UnitDamagePercent), Is.EqualTo((3, 10, 20)));
            Assert.That(status.SameValues(new CrashImpactTraitStatusSnapshot(3, 10, 20)), Is.True);
            Assert.That(status.SameValues(new CrashImpactTraitStatusSnapshot(3, 10, 21)), Is.False);
            var input = Input(wall: 10, unit: 20);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var knockback = new CombatEffectEngine().EvaluateKnockback(CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor)));
            Assert.That(knockback.Contributions.Single().DistanceCells, Is.EqualTo(3));
            Assert.That((knockback.Collision.Value.WallDamagePercent, knockback.Collision.Value.UnitDamagePercent), Is.EqualTo((10, 20)));
        }

        [TestCase(0, 10, 10)]
        [TestCase(3, -1, 10)]
        [TestCase(3, 10, 101)]
        public void InvalidValues_AreRejectedAtEveryLayer(int distance, int wall, int unit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrashImpactTraitSpec(distance, wall, unit));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrashImpactTraitDefinition(distance, wall, unit));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrashImpactTraitStatusSnapshot(distance, wall, unit));
        }

        [TestCase(1000, 10, 0, 100)]
        [TestCase(20, 10, 5, 1)]     // 기준 2, 방어 뒤 1.9 → 1
        [TestCase(20, 10, 99, 1)]    // 기준이 1 이상이면 방어 뒤 최소 1
        [TestCase(20, 10, 100, 1)]
        [TestCase(1000, 0, 0, 0)]    // 비율 0이면 피해 없음
        [TestCase(9, 10, 0, 0)]      // 기준 0(내림)이면 피해 없음
        public void CollisionRules_FloorBaseThenApplyDefenseWithMinimumOne(int maxHealth, int percent, int defense, int expected)
        {
            int baseDamage = CrashImpactCollisionRules.BaseDamage(maxHealth, percent);
            Assert.That(CrashImpactCollisionRules.AfterDefense(baseDamage, defense), Is.EqualTo(expected));
        }

        [Test]
        public void CollisionRules_PickUnitPercentOnlyForCharacters()
        {
            var collision = new EffectKnockbackCollision(10, 30);
            Assert.That(CrashImpactCollisionRules.PercentFor(collision, AttackKnockbackCollisionKind.Wall), Is.EqualTo(10));
            Assert.That(CrashImpactCollisionRules.PercentFor(collision, AttackKnockbackCollisionKind.Structure), Is.EqualTo(10));
            Assert.That(CrashImpactCollisionRules.PercentFor(collision, AttackKnockbackCollisionKind.Character), Is.EqualTo(30));
        }

        [Test]
        public void Wall_DamagesOnlyTheFlyer_AndPublishesSignalBeforeDamage()
        {
            var input = WallMovementTests.WithWalls(Input(), WallStateLifecycleTests.Wall(71, new CellCoord(5, 4)));
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4, 4)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control - 100));
            var record = result.Attack.Value.Knockbacks.Single();
            Assert.That(record.Stop, Is.EqualTo(AttackKnockbackStop.Blocked));
            Assert.That((record.Collision.Kind, record.Collision.Cell, record.Collision.EntityId),
                Is.EqualTo((AttackKnockbackCollisionKind.Wall, new CellCoord(5, 4), (EntityId?)null)));
            var signal = Signal(result);
            Assert.That(signal.SourceUnitId, Is.EqualTo(new EntityId(1)));
            Assert.That(signal.TraitKey, Is.EqualTo(TraitKey.CrashImpact));
            Assert.That(signal.TargetIds, Is.EqualTo(new[] { new EntityId(3) }));
            Assert.That(signal.TargetCells, Is.EqualTo(new[] { new CellCoord(5, 4) }));
            Assert.That(signal.TargetCell, Is.EqualTo(new CellCoord(4, 4)));
            Assert.That(signal.Magnitude, Is.EqualTo(100));
            // 신호는 마지막 걸음 뒤, 부딪힘 체력 사건 앞에 온다.
            var events = result.Events.ToList();
            int signalIndex = events.FindIndex(e => e is CombatEffectsEvent fx && fx.Traits.Any(t => t.TraitActivationKind == TraitActivationKind.CrashImpactCollided));
            int lastMove = events.FindLastIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Moved && c.Final.EntityId.Value == 3);
            int lastHealth = events.FindLastIndex(e => e is CombatCharacterEvent c && c.Kind != CombatEventKind.Moved && c.Final.EntityId.Value == 3);
            Assert.That(lastMove, Is.LessThan(signalIndex));
            Assert.That(signalIndex, Is.LessThan(lastHealth));
            // 날기 시작 신호는 기존 넉백 발동 사건에 작렬 키로 실린다.
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t =>
                t.TraitKey == TraitKey.CrashImpact && t.TraitActivationKind == TraitActivationKind.KnockbackApplied), Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved)
                .All(e => e.MovementCause.TraitKey == TraitKey.CrashImpact), Is.True);
        }

        [Test]
        public void AdjacentWall_IsStillACollision()
        {
            var input = WallMovementTests.WithWalls(Input(), WallStateLifecycleTests.Wall(71, new CellCoord(4, 4)));
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(TargetCell));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control - 100));
            Assert.That(result.Attack.Value.Knockbacks.Single().Collision.Cell, Is.EqualTo(new CellCoord(4, 4)));
        }

        [Test]
        public void FullDistance_IsNotACollision()
        {
            var input = Input();
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(6, 4)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control));
            Assert.That(result.Attack.Value.Knockbacks.Single().Collision, Is.Null);
            Assert.That(SignalOrNull(result), Is.Null);
        }

        [Test]
        public void BoardEdge_IsAWall()
        {
            var input = Input(actorCell: new CellCoord(4, 4), targetCell: new CellCoord(5, 4));
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(7, 4)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control - 100));
            Assert.That(result.Attack.Value.Knockbacks.Single().Collision.Kind, Is.EqualTo(AttackKnockbackCollisionKind.Wall));
            Assert.That(result.Attack.Value.Knockbacks.Single().Collision.Cell, Is.EqualTo(new CellCoord(8, 4)));
        }

        [Test]
        public void HighGroundStep_IsAWall()
        {
            var input = HighGroundMovementTests.TerrainInput(Input(), HighGroundMovementTests.High(81, new CellCoord(5, 4)));
            var result = Run(input);
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4, 4)));
            var collision = result.Attack.Value.Knockbacks.Single().Collision;
            Assert.That((collision.Kind, collision.Cell), Is.EqualTo((AttackKnockbackCollisionKind.Wall, new CellCoord(5, 4))));
        }

        [TestCase(Owner.Enemy)]
        [TestCase(Owner.Player)]
        public void Unit_DamagesBothWithFlyerMaxHealthAndOwnDefense(Owner blockerOwner)
        {
            var blocker = CombatTraitTestFixtures.Stats(Unit(50, new CellCoord(5, 4), 500, blockerOwner), defense: 50);
            var input = WithCharacter(Input(unit: 10), blocker);
            var result = Run(input);
            var control = Run(WithoutTrait(input));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4, 4)));
            // 날아간 유닛 최대 체력 1000의 10% = 100. 부딪힌 유닛은 자기 방어 50으로 50만 받는다.
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(Final(control, 3).Health.CurrentHealth - 100));
            Assert.That(Final(result, 50).Health.CurrentHealth, Is.EqualTo(450));
            Assert.That(Final(result, 50).Anchor, Is.EqualTo(new CellCoord(5, 4)));
            var collision = result.Attack.Value.Knockbacks.Single().Collision;
            Assert.That((collision.Kind, collision.EntityId), Is.EqualTo((AttackKnockbackCollisionKind.Character, (EntityId?)new EntityId(50))));
            Assert.That(Signal(result).TargetIds, Is.EqualTo(new[] { new EntityId(3), new EntityId(50) }));
        }

        [Test]
        public void UnitPercent_IsSeparateFromWallPercent()
        {
            var input = WithCharacter(Input(wall: 10, unit: 30), Unit(50, new CellCoord(5, 4), 1000));
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control - 300));
            Assert.That(Final(result, 50).Health.CurrentHealth, Is.EqualTo(700));
        }

        [Test]
        public void Structure_IsAWallAndTakesNoDamage()
        {
            var barrel = ObstacleReactionTests.Barrel(40, new CellCoord(5, 4), 1000, 1);
            var input = ObstacleReactionTests.Copy(Input(), structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control - 100));
            Assert.That(result.FinalState.Structures.Single().ObstacleDurability.RemainingHits, Is.EqualTo(1000));
            var collision = result.Attack.Value.Knockbacks.Single().Collision;
            Assert.That((collision.Kind, collision.EntityId), Is.EqualTo((AttackKnockbackCollisionKind.Structure, (EntityId?)new EntityId(40))));
            Assert.That(Signal(result).TargetIds, Is.EqualTo(new[] { new EntityId(3) }));
        }

        [Test]
        public void ZeroPercent_SignalsWithoutDamage()
        {
            var input = WallMovementTests.WithWalls(Input(wall: 0), WallStateLifecycleTests.Wall(71, new CellCoord(5, 4)));
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control));
            Assert.That(Signal(result).Magnitude, Is.EqualTo(0));
        }

        [Test]
        public void ImmuneTarget_NeitherMovesNorCollides()
        {
            var input = WallMovementTests.WithWalls(Input(), WallStateLifecycleTests.Wall(71, new CellCoord(4, 4)));
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.CharacterKey == EntityKey.Unit_Demon
                    ? CombatCommandTestData.With(c, category: CategorySnapshot.CreateStateless(CategoryKey.Heavy)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon
                    ? new CombatAttackProfile(ImpactBoltTests.CopySpec(p.Definition, category: CategoryKey.Heavy,
                        behavior: new UnitBehaviorSpec(true, true, true)), p.Method) : p).ToArray());
            var result = Run(input);
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(TargetCell));
            Assert.That(result.Attack.Value.Knockbacks.Single().Stop, Is.EqualTo(AttackKnockbackStop.Immune));
            Assert.That(result.Attack.Value.Knockbacks.Single().Collision, Is.Null);
            Assert.That(SignalOrNull(result), Is.Null);
        }

        [Test]
        public void LethalCollision_KillsAtTheStoppedCell()
        {
            var input = WallMovementTests.WithWalls(Input(), WallStateLifecycleTests.Wall(71, new CellCoord(5, 4)));
            input = ImpactBoltTests.Replace(input, 3, c => CombatCommandTestData.With(c, hp: 110));
            var result = Run(input);
            Assert.That(Final(result, 3).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Died && e.Final.EntityId.Value == 3), Is.True);
        }

        [Test]
        public void TargetsFlyInIdOrder_EarlierFlyerCrashesIntoLaterTarget()
        {
            // 방사 스플래시로 (3,4)의 3과 (4,4)의 4를 함께 친다. 3이 먼저 날아 바로 4에 부딪히고, 4는 그 뒤 3칸을 다 날아간다.
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);
            var input = WithCharacter(Input(method: method), Unit(4, new CellCoord(4, 4), 1000));
            var result = Run(input);
            var control = Run(WithoutTrait(input));
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(TargetCell));
            Assert.That(Final(result, 4).Anchor, Is.EqualTo(new CellCoord(7, 4)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(Final(control, 3).Health.CurrentHealth - 100));
            Assert.That(Final(result, 4).Health.CurrentHealth, Is.EqualTo(Final(control, 4).Health.CurrentHealth - 100));
            var records = result.Attack.Value.Knockbacks;
            Assert.That(records.Select(r => r.TargetId.Value), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(records[0].Collision.EntityId, Is.EqualTo((EntityId?)new EntityId(4)));
            Assert.That(records[1].Collision, Is.Null);
        }

        [Test]
        public void Preview_MatchesCommit_AndListsTheStruckUnitAsAuxiliaryRow()
        {
            var input = WithCharacter(Input(), Unit(50, new CellCoord(5, 4), 1000));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projection);
            var result = Run(input);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Knockbacks.Single().Collision.EntityId, Is.EqualTo((EntityId?)new EntityId(50)));
            Assert.That(Final(projection, 3).Health.CurrentHealth, Is.EqualTo(Final(result, 3).Health.CurrentHealth));
            Assert.That(Final(projection, 50).Health.CurrentHealth, Is.EqualTo(900));
            var struck = preview.Targets.ToArray().Single(t => t.TargetId.Value == 50);
            Assert.That((struck.IsDirectHit, struck.ExpectedDamage, struck.TargetHealthAfter), Is.EqualTo((false, 100, 900)));
            var flyer = preview.Targets.ToArray().Single(t => t.TargetId.Value == 3);
            Assert.That(flyer.TargetHealthAfter, Is.EqualTo(Final(result, 3).Health.CurrentHealth));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 50).Health.CurrentHealth, Is.EqualTo(1000));
        }

        [Test]
        public void TraitlessKnockback_StillStopsWithoutCollision()
        {
            var input = WallMovementTests.WithWalls(Input(), WallStateLifecycleTests.Wall(71, new CellCoord(5, 4)));
            input = WithTraits(input, new KnockbackTraitSpec(3), new KnockbackTraitStatusSnapshot(3));
            var result = Run(input);
            int control = Final(Run(WithoutTrait(input)), 3).Health.CurrentHealth;
            Assert.That(Final(result, 3).Anchor, Is.EqualTo(new CellCoord(4, 4)));
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(control));
            Assert.That(result.Attack.Value.Knockbacks.Single().Collision, Is.Null);
        }

        [Test]
        public void ProfileRejectsKnockbackAndCrashImpactTogether()
        {
            var input = Input(actorCell: new CellCoord(0, 4), targetCell: new CellCoord(1, 4));
            Assert.Throws<ArgumentException>(() => WithTraits(input,
                new TraitSpec[] { new KnockbackTraitSpec(5), new CrashImpactTraitSpec(3, 10, 10) },
                new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(5), new CrashImpactTraitStatusSnapshot(3, 10, 10) }));
        }

        [Test]
        public void Profile_RejectsImpactBoltCombination()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(HammerSpec(new ImpactBoltTraitSpec(1), new CrashImpactTraitSpec(3, 10, 10)), method));
            Assert.DoesNotThrow(() => new CombatAttackProfile(HammerSpec(new CrashImpactTraitSpec(3, 10, 10)), method));
        }

        [Test]
        public void Snapshot_AllowsCollisionOnlyOnTheBlockedNextCellOfACharacter()
        {
            var collision = new AttackKnockbackCollision(AttackKnockbackCollisionKind.Wall, new CellCoord(5, 4), null);
            Assert.DoesNotThrow(() => new AttackKnockbackSnapshot(new EntityId(3), AttackKnockbackTargetKind.Character, TargetCell,
                CellCoord.Right, 3, new[] { new CellCoord(4, 4) }, AttackKnockbackStop.Blocked, collision));
            Assert.Throws<ArgumentException>(() => new AttackKnockbackSnapshot(new EntityId(3), AttackKnockbackTargetKind.Character, TargetCell,
                CellCoord.Right, 3, new[] { new CellCoord(4, 4) }, AttackKnockbackStop.Blocked,
                new AttackKnockbackCollision(AttackKnockbackCollisionKind.Wall, new CellCoord(6, 4), null)));
            Assert.Throws<ArgumentException>(() => new AttackKnockbackSnapshot(new EntityId(3), AttackKnockbackTargetKind.Character, TargetCell,
                CellCoord.Right, 1, new[] { new CellCoord(4, 4) }, AttackKnockbackStop.Completed,
                new AttackKnockbackCollision(AttackKnockbackCollisionKind.Wall, new CellCoord(5, 4), null)));
            Assert.Throws<ArgumentException>(() => new AttackKnockbackCollision(AttackKnockbackCollisionKind.Wall, CellCoord.Zero, new EntityId(1)));
            Assert.Throws<ArgumentException>(() => new AttackKnockbackCollision(AttackKnockbackCollisionKind.Character, CellCoord.Zero, null));
        }

        private static UnitSpec HammerSpec(params TraitSpec[] traits) =>
            CombatCommandTestData.Spec(EntityKey.Unit_DynamiteHammer, 20, CategoryKey.Main, traits);

        private static CharacterCombatFacts Unit(int id, CellCoord cell, int maxHealth, Owner owner = Owner.Enemy)
        {
            var c = CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, owner);
            return CharacterSettlementCalculator.Copy(c, new HealthStateSnapshot(maxHealth, maxHealth), c.Stats);
        }

        private static CombatCommandInput Input(int distance = 3, int wall = 10, int unit = 10, CellCoord? actorCell = null,
            CellCoord? targetCell = null, TargetingMethodSpec method = null)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var spec = new CrashImpactTraitSpec(distance, wall, unit);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_DynamiteHammer,
                    actorCell ?? new CellCoord(2, 4), Owner.Player, 20),
                traits: new TraitStatusSnapshot[] { new CrashImpactTraitStatusSnapshot(distance, wall, unit) });
            var target = targetCell ?? TargetCell;
            var characters = new List<CharacterCombatFacts> { actor,
                CombatCommandTestData.With(seed.Characters[1], position: new CellCoord(0, 0)), Unit(3, target, 1000) };
            var profiles = seed.Profiles.Where(p => p.Definition.Key != EntityKey.Unit_Akimbo).Append(new CombatAttackProfile(HammerSpec(spec),
                method ?? new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
            return new CombatCommandInput(1, CombatCommandKind.Attack, actor.EntityId, AttackAim.ForCell(target),
                seed.State, characters, profiles, seed.Main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0), seed.Souls, seed.Artifacts,
                battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
        }

        private static CombatCommandInput WithCharacter(CombatCommandInput input, CharacterCombatFacts character)
        {
            var characters = input.Characters.Append(character).ToArray();
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State, characters, input.Profiles,
                input.Main, characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey,
                    c.OwnershipRole.Value)).ToArray(), characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0),
                input.Souls, input.Artifacts, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
        }

        private static CombatCommandInput WithTraits(CombatCommandInput input, TraitSpec spec, TraitStatusSnapshot status) =>
            WithTraits(input, new[] { spec }, new[] { status });

        private static CombatCommandInput WithTraits(CombatCommandInput input, TraitSpec[] specs, TraitStatusSnapshot[] statuses) =>
            CombatCommandTestData.With(
                ImpactBoltTests.Replace(input, 1, c => CombatCommandTestData.With(c, traits: statuses)),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_DynamiteHammer
                    ? new CombatAttackProfile(ImpactBoltTests.CopySpec(p.Definition, traits: specs), p.Method) : p).ToArray());

        private static CombatCommandInput WithoutTrait(CombatCommandInput input) =>
            WithTraits(input, Array.Empty<TraitSpec>(), Array.Empty<TraitStatusSnapshot>());

        private static TraitActivationSnapshot SignalOrNull(CombatCommandTransition result) =>
            result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .SingleOrDefault(t => t.TraitActivationKind == TraitActivationKind.CrashImpactCollided);

        private static TraitActivationSnapshot Signal(CombatCommandTransition result) =>
            SignalOrNull(result) ?? throw new AssertionException("작렬 부딪힘 신호가 없습니다.");

        private static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == id);

        private static CombatCommandTransition Run(CombatCommandInput input) => ImpactBoltTests.Run(input);
    }
}
