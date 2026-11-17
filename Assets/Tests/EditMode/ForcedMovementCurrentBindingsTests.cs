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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 같은 공격 안에서 새로 생긴 기둥 잔해 벽이 유닛 강제 이동을 막는지 확인한다(검토 OB-1, 설계 2026-11-17 C-1a).
    /// 잔해 벽은 새 원본 ID라 공격 시작 바인딩에는 없고 현재 바인딩에만 있다.
    /// 기본 배치: 공격자 1 (1,3), 메인 10 (7,7), 대상 2 (4,3), 기둥 51 (6,3). 대상은 +X 방향으로 밀리고 잔해 벽은 (7,3)에 생긴다.
    /// </summary>
    public sealed class ForcedMovementCurrentBindingsTests
    {
        private static readonly CellCoord Target = new(4, 3);
        private static readonly CellCoord PillarCell = new(6, 3);
        private static readonly CellCoord DebrisCell = new(7, 3);

        [Test]
        public void MineMidPathFellsPillar_UnitStopsBeforeTheNewDebris()
        {
            var input = WithPillar(Knockback(3));
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 3)));
            var result = Run(input, MineEnvironmentTests.Rules);

            Assert.That(result.Events.OfType<CombatPillarFallEvent>().Single().Origin.PhysicalCell, Is.EqualTo(new CellCoord(5, 3)));
            AssertStoppedBeforeDebris(result);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BarrelPillarChainInDirectStage_KnockbackStopsBeforeTheNewDebris(bool crashImpact)
        {
            var input = crashImpact
                ? CombatCommandEffectTests.ActorEffect(new CrashImpactTraitSpec(3, 10, 10), new CrashImpactTraitStatusSnapshot(3, 10, 10))
                : Knockback(3);
            input = WithBarrelAndPillar(Plain(input));
            var result = Run(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Events.OfType<CombatPillarFallEvent>().Single().Origin.PhysicalCell, Is.EqualTo(new CellCoord(5, 3)));
            AssertStoppedBeforeDebris(result);
            var collision = result.Attack.Value.Knockbacks.Single(k => k.TargetId == new EntityId(2)).Collision;
            if (crashImpact)
                Assert.That((collision.Kind, collision.Cell), Is.EqualTo((AttackKnockbackCollisionKind.Wall, DebrisCell)));
            else
                Assert.That(collision, Is.Null);
        }

        [Test]
        public void CorpsePush_StopsBeforeDebrisCreatedEarlierInTheSameAttack()
        {
            var input = Knockback(3);
            var bomber = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, Target, Owner.Enemy, 20),
                hp: 5, traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(1) });
            var profiles = input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, new SelfDestructTraitSpec(1)),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 2 ? bomber : c).ToArray(),
                profiles: profiles);
            input = WithBarrelAndPillar(input);
            var result = Run(input, Array.Empty<IMovementTileRule>());

            var corpse = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitActivationKind == TraitActivationKind.CorpseDisplaced);
            Assert.That(corpse.TargetCells, Is.EqualTo(new[] { Target, new CellCoord(5, 3), PillarCell }));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitActivationKind == TraitActivationKind.SelfDestructExploded).TargetCell, Is.EqualTo(PillarCell));
        }

        private static void AssertStoppedBeforeDebris(CombatCommandTransition result)
        {
            var debris = result.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Single(t => t.Cell == DebrisCell);
            Assert.That(debris.Kind, Is.EqualTo(StaticTerrainKind.Wall));
            Assert.That(result.EnvironmentBindings.Final.TerrainSourceIds, Does.Contain(debris.Id));
            var victim = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(2));
            Assert.That(victim.Health.IsDead, Is.False);
            Assert.That(victim.Anchor, Is.EqualTo(PillarCell));
            var knockback = result.Attack.Value.Knockbacks.Single(k => k.TargetId == new EntityId(2));
            Assert.That(knockback.Stop, Is.EqualTo(AttackKnockbackStop.Blocked));
            Assert.That(knockback.Path, Is.EqualTo(new[] { new CellCoord(5, 3), PillarCell }));
        }

        /// <summary>대상 2만 남긴 넉백 공격이다. 기본 방사 스플래시(70%)는 (5,3)을 덮지만 (6,3)은 덮지 않는다.</summary>
        private static CombatCommandInput Knockback(int distance) =>
            Plain(CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(distance), new KnockbackTraitStatusSnapshot(distance)));

        private static CombatCommandInput Plain(CombatCommandInput input) =>
            CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 3 && c.EntityId.Value != 4).ToArray());

        private static CombatCommandInput WithPillar(CombatCommandInput input)
        {
            var pillar = PillarTestData.Pillar(51, PillarCell);
            return ObstacleReactionTests.Copy(input, structures: new[] { pillar.Structure }, profiles: new[] { pillar.Profile });
        }

        /// <summary>스플래시가 (5,3) 화약통을 터뜨리고 폭발이 (6,3) 기둥을 +X로 넘어뜨린다.</summary>
        private static CombatCommandInput WithBarrelAndPillar(CombatCommandInput input)
        {
            var barrel = ObstacleReactionTests.Barrel(52, new CellCoord(5, 3), 1, 5);
            var pillar = PillarTestData.Pillar(51, PillarCell);
            return ObstacleReactionTests.Copy(input, structures: new[] { pillar.Structure, barrel.Structure },
                profiles: new[] { pillar.Profile, barrel.Profile });
        }

        private static CombatCommandTransition Run(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result));
            return result;
        }
    }
}
