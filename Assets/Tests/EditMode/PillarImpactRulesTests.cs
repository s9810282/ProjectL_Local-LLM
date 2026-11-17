using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 기둥 낙하 규칙의 단일 원본(설계 2026-11-17 OB-6), 압사 실제 감소량(OB-9), 자폭의 장애물 타격 기록(OB-10)을 확인한다.
    /// </summary>
    public sealed class PillarImpactRulesTests
    {
        private static readonly CellCoord[] Line = { new(5, 3), new(6, 3), new(7, 3) };

        [Test]
        public void CrushedUnitsAreOrderedByNearestImpactCellThenId()
        {
            var far = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(7, 3), Owner.Enemy);
            var nearHigh = CombatCommandTestData.Character(9, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Player);
            var outside = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(5, 4), Owner.Enemy);
            var dead = CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(6, 3), Owner.Enemy)
                .WithHealth(new HealthStateSnapshot(0, 100));
            var crushed = PillarImpactRules.CrushedUnits(new[] { far, nearHigh, outside, dead }, Line);
            Assert.That(crushed.Select(c => c.EntityId.Value), Is.EqualTo(new[] { 9, 2 }));
        }

        [Test]
        public void DestroyedObstaclesSkipOrdinaryAndAlreadyDestroyedStructures()
        {
            var barrel = ObstacleReactionTests.Barrel(52, new CellCoord(6, 3), 3, 0).Structure;
            var broken = ObstacleReactionTests.Barrel(53, new CellCoord(5, 3), 3, 0).Structure
                .WithObstacleDurability(new ObstacleDurabilitySnapshot(0, 3));
            var spec = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var ordinary = new StructureCombatFacts(new EntityId(55), spec, new HealthStateSnapshot(20, 20), new CellCoord(5, 3),
                Rotation4.R0, new[] { new CellCoord(5, 3) });
            Assert.That(PillarImpactRules.DestroyedObstacles(new[] { barrel, broken, ordinary }, Line).Select(s => s.EntityId.Value),
                Is.EqualTo(new[] { 52 }));
        }

        [Test]
        public void DebrisEligibilityReadsOnlyBoundTerrainAndLiveOrdinaryStructures()
        {
            var cell = new CellCoord(5, 3);
            var wall = PlatformTestData.Terrain(71, cell, StaticTerrainKind.Wall);
            var filled = PlatformTestData.Terrain(72, cell, StaticTerrainKind.FilledCliff);
            var cliff = PlatformTestData.Terrain(73, cell);
            var platform = PlatformTestData.Platform(74, cell, new CellCoord(5, 6));
            var none = Array.Empty<StructureCombatFacts>();
            EnvironmentSemanticBindings Bind(params long[] ids) => new(ids, Array.Empty<long>(), Array.Empty<EntityId>());

            Assert.That(PillarImpactRules.CanReceiveDebris(new MovementTileFact[] { wall }, Bind(71), none, cell), Is.False);
            Assert.That(PillarImpactRules.CanReceiveDebris(new MovementTileFact[] { filled }, Bind(72), none, cell), Is.False);
            Assert.That(PillarImpactRules.CanReceiveDebris(new MovementTileFact[] { cliff }, Bind(73), none, cell), Is.True);
            Assert.That(PillarImpactRules.CanReceiveDebris(new MovementTileFact[] { cliff, platform }, Bind(73, 74), none, cell), Is.False);
            // 계산기와 같이 바인딩 밖 원본은 지형으로 보지 않는다(검사 쪽 사본과 달랐던 부분).
            Assert.That(PillarImpactRules.CanReceiveDebris(new MovementTileFact[] { wall }, Bind(), none, cell), Is.True);
            Assert.That(PillarImpactRules.CanReceiveDebris(new MovementTileFact[] { cliff, platform }, Bind(73), none, cell), Is.True);

            var spec = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var ordinary = new StructureCombatFacts(new EntityId(55), spec, new HealthStateSnapshot(20, 20), cell, Rotation4.R0, new[] { cell });
            var destroyed = new StructureCombatFacts(new EntityId(55), spec, new HealthStateSnapshot(0, 20), cell, Rotation4.R0, new[] { cell });
            Assert.That(PillarImpactRules.CanReceiveDebris(Array.Empty<MovementTileFact>(), Bind(), new[] { ordinary }, cell), Is.False);
            Assert.That(PillarImpactRules.CanReceiveDebris(Array.Empty<MovementTileFact>(), Bind(), new[] { destroyed }, cell), Is.True);
            var barrel = ObstacleReactionTests.Barrel(52, cell, 3, 0).Structure;
            Assert.That(PillarImpactRules.CanReceiveDebris(Array.Empty<MovementTileFact>(), Bind(), new[] { barrel }, cell), Is.True);
        }

        [Test]
        public void CrushTotalUsesTheActualLossWhenAnEarlierCrushHealsALaterVictim()
        {
            // 기둥 (4,3)이 (3,3) 출처로 +X로 넘어진다. 적 4 (5,3)이 먼저 압사되고 피의 부름이 공격자를 회복시킨 뒤 공격자 (6,3)이 압사된다.
            var input = TraitCombatFixtures.Equip(PillarTestData.Input(), EntityKey.Unit_Akimbo, new CallOfBloodTraitSpec(10));
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 3)
                .Select(c => c.EntityId == input.ActorId
                    ? CombatCommandTestData.With(c, position: new CellCoord(6, 3)).WithHealth(new HealthStateSnapshot(50, 100)) : c)
                .ToArray());
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var damage = new CombatDamageApplication(Array.Empty<ItemDefinition>(), characters, events, input);
            var environment = new CommandEnvironmentState(input.State.Environment, input.EnvironmentBindings, input.State.EnvironmentProgress);
            var obstacles = new ObstacleReactionCalculator(input, environment, damage,
                input.State.Structures.ToDictionary(s => s.EntityId), new Dictionary<EntityId, CellCoord>());

            obstacles.ApplyHit(new EntityId(51), new CombatDamageOrigin(new CellCoord(3, 3), input.ActorId));
            obstacles.ResolveExplosions();

            Assert.That(events.OfType<CombatPillarFallEvent>().Single().CrushedUnitIds,
                Is.EqualTo(new[] { new EntityId(4), input.ActorId }));
            var recovered = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitKey == TraitKey.CallOfBlood && t.TraitActivationKind == TraitActivationKind.HealthRecovered);
            Assert.That(recovered.Magnitude, Is.EqualTo(10));
            Assert.That(characters[input.ActorId].Health.IsDead, Is.True);
            Assert.That(obstacles.BlastDamage[new EntityId(4)], Is.EqualTo(100));
            Assert.That(obstacles.BlastDamage[input.ActorId], Is.EqualTo(60));
        }

        [Test]
        public void SelfDestructRecordsANonDestroyingObstacleHitAsAffected()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var blast = new CellCoord(4, 3);
            var seed = CombatCommandTestData.Input(method, AttackAim.ForCell(blast), empty: true);
            var actor = CombatCommandTestData.With(seed.Characters[0], position: new CellCoord(3, 3));
            var bomber = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, blast, Owner.Enemy, 20),
                hp: 5, traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(1) });
            var profiles = seed.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, new SelfDestructTraitSpec(1)), method)).ToArray();
            var input = CombatCommandTestData.With(seed, new[] { actor, seed.Characters[1], bomber }, profiles: profiles);
            var barrel = ObstacleReactionTests.Barrel(52, new CellCoord(5, 3), 3, 0);
            input = ObstacleReactionTests.Copy(input, structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Structures.Single().ObstacleDurability.RemainingHits, Is.EqualTo(2));
            var exploded = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitActivationKind == TraitActivationKind.SelfDestructExploded);
            Assert.That(exploded.TargetIds, Does.Contain(barrel.Structure.EntityId));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result));
        }
    }
}
