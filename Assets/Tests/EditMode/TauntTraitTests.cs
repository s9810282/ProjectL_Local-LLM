using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using static ProjectL.Tests.Core.WallStateLifecycleTests;

namespace ProjectL.Tests.Core
{
    public sealed class TauntTraitTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();
        private static EnemyAiPolicy Policy => new(new HostilityPolicy(), Rules);

        [TestCase(false, 0, 90)]
        [TestCase(true, 0, 100)]
        [TestCase(false, 95, 0)]
        public void RankedAreaHit_ReducesOnlyDamageToEachCurrentTaunter(bool firstLeftArea, int shield, int firstDamage)
        {
            var input = RankedAreaAttack();
            if (firstLeftArea) input = Move(input, 5, new CellCoord(3, 1));

            if (shield > 0)
            {
                var origin = new EffectOriginKey(EffectOwnerKind.Trait, 5, (int)TraitKey.Vanguard, "shield");
                var status = new StatusEffectSet(new AppliedStatus[] { new ShieldStatus(origin, shield, true, 0) });
                input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 5
                    ? c.WithStatusEffects(status) : c).ToArray());
            }
            var preview = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(preview.CanAttack, Is.True, preview.FailureReason.ToString());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var expected = new[] { 100, firstDamage, 85 };
            Assert.That(preview.Targets.ToArray().OrderBy(t => t.TargetId).Select(t => t.TargetHealthBefore - t.TargetHealthAfter), Is.EqualTo(expected));
            Assert.That(result.Attack.Value.Hits.ToArray().OrderBy(h => h.TargetId).Select(h => h.DamageDealt), Is.EqualTo(expected));
            if (shield > 0) Assert.That(TraitCombatFixtures.Final(result, 5).TraitEffects.ActiveShieldTotal, Is.EqualTo(5));
        }

        [Test]
        public void RankedAreaHit_ReportsFirstTauntersDeathAndReleaseBeforeSecondReduction()
        {
            var input = RankedAreaAttack();
            var first = new EntityId(5);
            var second = new EntityId(6);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == first
                ? c.WithHealth(new HealthStateSnapshot(50, 100)) : c).ToArray());

            var attack = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            var events = attack.Events.ToList();
            int death = events.FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Died && c.Final.EntityId == first);
            int release = events.FindIndex(e => e is CombatTraitStateEvent t && t.Final.EntityId == input.ActorId &&
                t.Expected.StatusEffects.Entries.OfType<TauntedStatus>().Any(s => s.Origin.Value.OwnerInstanceId == first.Value) &&
                !t.Final.StatusEffects.Entries.OfType<TauntedStatus>().Any(s => s.Origin.Value.OwnerInstanceId == first.Value));
            int reduction = events.FindIndex(e => e is CombatEffectsEvent fx && fx.Traits.Any(t => t.TraitKey == TraitKey.Taunt &&
                t.TraitActivationKind == TraitActivationKind.DamageModified && t.SourceUnitId == second));
            Assert.That(death, Is.GreaterThanOrEqualTo(0));
            Assert.That(release, Is.GreaterThan(death));
            Assert.That(reduction, Is.GreaterThan(release));
        }

        private static CombatCommandInput RankedAreaAttack()
        {
            var all = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0);
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(1, 0));
            input = Source(input, 5, EntityKey.Unit_Cowboy, new CellCoord(2, 1), new TauntTraitSpec(1, 10));
            input = Source(input, 6, EntityKey.Unit_ShotGun, new CellCoord(1, 2), new TauntTraitSpec(1, 15));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                    ? c.With(stats: c.Stats.With(attackPower: 100, appointRange: 2)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Demon, 100), all) : p).ToArray());
            return CombatAttackQueries.WithAim(input, AttackAim.ForAll());
        }

        [TestCase(true, 85)]
        [TestCase(false, 100)]
        public void SecondaryDamage_ReducesOnlyACreditedTauntedSource(bool credited, int expectedDamage)
        {
            var input = TraitCombatFixtures.Equip(Input(), EntityKey.Unit_Cowboy, new TauntTraitSpec(2, 15));
            var current = input.Characters.ToDictionary(c => c.EntityId);
            var damage = CombatDamageApplication.For(input, current, new List<CombatEvent>());
            var origin = new CombatDamageOrigin(current[input.ActorId].Anchor, credited ? input.ActorId : (EntityId?)null);
            damage.Apply(new EntityId(5), CombatDamageKind.EnvironmentDamage, origin, 100);
            Assert.That(current[new EntityId(5)].Health.CurrentHealth, Is.EqualTo(100 - expectedDamage));
        }

        [Test]
        public void AttackableTaunter_IsRequiredTarget()
        {
            var input = Input(range: 2);
            Assert.That(Policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(4, 2)));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(5) }));
            Assert.That(result.PathMovement, Is.Null);
        }

        [Test]
        public void UnreachableTaunter_PrioritizesNextTurnAttackPosition()
        {
            var input = Input();
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(TraitCombatFixtures.Final(result, 1).Anchor, Is.EqualTo(new CellCoord(3, 2)));
            var next = CombatCommandCalculator.Advance(CombatCommandComposition.Project(result), Rules);
            Assert.That(next.PathMovement, Is.Null);
            Assert.That(next.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(5) }));
        }

        [Test]
        public void MultipleTaunters_UseStableNearestChoice()
        {
            var input = Source(Input(range: 2), 6, EntityKey.Unit_ShotGun, new CellCoord(3, 2));
            Assert.That(Policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var nearest), Is.True);
            Assert.That(nearest.Cell, Is.EqualTo(new CellCoord(3, 2)));
            input = Move(input, 5, new CellCoord(2, 3));
            Assert.That(Policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var tie), Is.True);
            Assert.That(tie.Cell, Is.EqualTo(new CellCoord(2, 3)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeadOrOutOfAreaSource_ReleasesConstraint(bool dead)
        {
            var input = Input(range: 2);
            input = dead ? CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 5
                ? c.WithHealth(new HealthStateSnapshot(0, 100)) : c).ToArray()) : Move(input, 5, new CellCoord(0, 0));
            if (!dead) input = Move(input, 1, new CellCoord(3, 2));
            Assert.That(Policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(2, 1)));
        }

        [Test]
        public void WallAndOccupiedDestination_UseOnlyLegalApproach()
        {
            var input = WallMovementTests.WithWalls(Input(), Wall(71, new CellCoord(3, 2)));
            var blocker = CombatCommandTestData.Character(7, EntityKey.Unit_Akimbo, new CellCoord(3, 1), Owner.Player);
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { blocker }).ToArray());
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.PathMovement.EnteredCells, Has.No.Member(new CellCoord(3, 2)));
            Assert.That(result.PathMovement.EnteredCells, Has.No.Member(new CellCoord(3, 1)));
            Assert.That(TraitCombatFixtures.Final(result, 1).Anchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(TraitCombatFixtures.Final(result, 1).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(result.Attack.HasValue, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TauntedEnemy_MovesTowardTaunter_OrAttacksOthersIfAlreadyMoved(bool moved)
        {
            // 이미 이동해 도발자를 칠 수도 갈 수도 없으면 도발을 지킬 수 없으므로 기존 AI대로 옆의 (2,1)을 친다(2026-11-02 결정).
            var input = Input();
            input = moved ? CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(actionUse: UnitActionUse.Moved) : c).ToArray()) : CombatCommandTestData.With(input,
                    state: input.State.With());
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.PathMovement == null, Is.EqualTo(moved));
            Assert.That(result.Attack.HasValue, Is.EqualTo(moved));
            if (moved) Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.Zero);
            Assert.That(TauntSelections(result), Is.EqualTo(moved ? 0 : 1));
            Assert.That(TraitCombatFixtures.Final(result, 1).Anchor, Is.EqualTo(moved ? new CellCoord(2, 2) : new CellCoord(3, 2)));
        }

        [Test]
        public void TauntWithoutPathOrAttack_FallsBackToOrdinaryAttack()
        {
            // 이동 거리 0이라 도발자에게 갈 길이 없고 칠 수도 없다 → 도발 없이 가장 이득인 대상을 친다.
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(moveRange: 0)) : c).ToArray());
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(TauntSelections(result), Is.Zero);
        }

        private static int TauntSelections(CombatCommandTransition result) => TraitActivationOutcomeTests.Activations(result.Events, TraitKey.Taunt)
            .Count(a => a.TraitActivationKind == TraitActivationKind.TauntSelected);

        private static CombatCommandInput Input(int range = 1)
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(2, 2), targetCell: new CellCoord(2, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(appointRange: range)) : c).ToArray());
            return Source(input, 5, EntityKey.Unit_Cowboy, new CellCoord(4, 2));
        }

        private static CombatCommandInput Source(CombatCommandInput input, int id, EntityKey key, CellCoord cell, TauntTraitSpec spec = null)
        {
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { CombatCommandTestData.Character(id, key, cell, Owner.Player) }).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Concat(new[] { new CombatAttackProfile(
                CombatCommandTestData.Spec(key, 1), new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)) }).ToArray());
            return TraitCombatFixtures.Equip(input, key, spec ?? new TauntTraitSpec(2));
        }

        private static CombatCommandInput Move(CombatCommandInput input, int id, CellCoord cell) => CombatCommandTestData.With(input,
            characters: input.Characters.Select(c => c.EntityId.Value == id ? c.WithPlacement(CharacterRuntimeArea.Field, null, cell, Rotation4.R0, new[] { cell }) : c).ToArray());
    }
}
