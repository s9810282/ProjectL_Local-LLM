using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 재구성 Lv3 부활 넉백(Phase E)입니다. 재구성 적 2가 (4,4)에서 남은 1로 적 진영 턴 시작을 맞아 부활하고,
    /// 주변 8칸의 한 칸 캐릭터를 바깥으로 한 칸 밉니다. 판은 8×8, 플레이어 1·메인 10은 기본 고정물에서 옵니다.
    /// </summary>
    public sealed class ReconstructionReviveKnockbackTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly IMovementTileRule[] DamageRules = { new EnvironmentDamageTests.DamageRule() };
        private static readonly TargetingMethodSpec SingleNoSplash =
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        private static readonly CellCoord Center = new CellCoord(4, 4);
        private static readonly EntityId Holder = new EntityId(2);

        [Test]
        public void Lv3Revive_PushesEveryNeighborOneCellOutward()
        {
            var expected = new Dictionary<int, (CellCoord From, CellCoord To)>
            {
                [1] = (new CellCoord(3, 4), new CellCoord(2, 4)),
                [3] = (new CellCoord(3, 3), new CellCoord(2, 2)),
                [4] = (new CellCoord(4, 3), new CellCoord(4, 2)),
                [5] = (new CellCoord(5, 3), new CellCoord(6, 2)),
                [6] = (new CellCoord(5, 4), new CellCoord(6, 4)),
                [7] = (new CellCoord(3, 5), new CellCoord(2, 6)),
                [8] = (new CellCoord(4, 5), new CellCoord(4, 6)),
                [9] = (new CellCoord(5, 5), new CellCoord(6, 6)),
            };
            var neighbors = expected.Select(pair => pair.Key == 1 ? Player(pair.Value.From) : Demon(pair.Key, pair.Value.From)).ToArray();
            var input = TurnInput(3, neighbors);
            var result = CombatTurnCalculator.BeginControllerTurn(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ReconstructionTraitTests.Reconstruction(ReconstructionTraitTests.Final(result, input, 2)), Is.Null);
            foreach (var pair in expected)
                Assert.That(ReconstructionTraitTests.Final(result, input, pair.Key).Anchor, Is.EqualTo(pair.Value.To), pair.Key.ToString());

            var applied = ReconstructionTraitTests.Activations(result, TraitActivationKind.KnockbackApplied).Single();
            Assert.That(applied.SourceUnitId, Is.EqualTo(Holder));
            Assert.That(applied.TraitKey, Is.EqualTo(TraitKey.Reconstruction));
            Assert.That(applied.Magnitude, Is.EqualTo(1));
            Assert.That(applied.TargetIds, Is.EqualTo(expected.Keys.OrderBy(k => k).Select(k => new EntityId(k)).ToArray()));
            var moves = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).ToArray();
            Assert.That(moves.Select(e => e.Final.EntityId.Value), Is.EquivalentTo(expected.Keys));
            Assert.That(moves.All(e => e.MovementCause.Agency == CombatMovementAgency.Forced &&
                e.MovementCause.TraitKey == TraitKey.Reconstruction), Is.True);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [Test]
        public void Lv2Revive_DoesNotPush()
        {
            var input = TurnInput(2, new[] { Demon(3, new CellCoord(5, 4)) });
            var result = CombatTurnCalculator.BeginControllerTurn(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ReconstructionTraitTests.Activations(result, TraitActivationKind.ReconstructionEnded), Has.Length.EqualTo(1));
            Assert.That(ReconstructionTraitTests.Final(result, input, 3).Anchor, Is.EqualTo(new CellCoord(5, 4)));
            Assert.That(ReconstructionTraitTests.Activations(result, TraitActivationKind.KnockbackApplied), Is.Empty);
        }

        [Test]
        public void BlockedNeighbor_StaysAndIsNotListed()
        {
            var input = TurnInput(3, new[]
            {
                Demon(3, new CellCoord(5, 4)), Demon(4, new CellCoord(6, 4)), Demon(5, new CellCoord(3, 4)),
            });
            var result = CombatTurnCalculator.BeginControllerTurn(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ReconstructionTraitTests.Final(result, input, 3).Anchor, Is.EqualTo(new CellCoord(5, 4)));
            Assert.That(ReconstructionTraitTests.Final(result, input, 4).Anchor, Is.EqualTo(new CellCoord(6, 4)));
            Assert.That(ReconstructionTraitTests.Final(result, input, 5).Anchor, Is.EqualTo(new CellCoord(2, 4)));
            Assert.That(ReconstructionTraitTests.Activations(result, TraitActivationKind.KnockbackApplied).Single().TargetIds,
                Is.EqualTo(new[] { new EntityId(5) }));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [Test]
        public void SteadfastWillNeighbor_IsNotPushed_AndRecordsThePrevention()
        {
            var steadfast = CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Sniper, new CellCoord(5, 4), Owner.Enemy),
                traits: new TraitStatusSnapshot[] { new SteadfastWillTraitStatusSnapshot() });
            var input = TurnInput(3, new[] { steadfast },
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Sniper, 1, CategoryKey.Main, new SteadfastWillTraitSpec()), SingleNoSplash));
            var result = CombatTurnCalculator.BeginControllerTurn(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ReconstructionTraitTests.Final(result, input, 3).Anchor, Is.EqualTo(new CellCoord(5, 4)));
            var prevented = ReconstructionTraitTests.Activations(result, TraitActivationKind.KnockbackPrevented).Single();
            Assert.That(prevented.SourceUnitId, Is.EqualTo(new EntityId(3)));
            Assert.That(prevented.TraitKey, Is.EqualTo(TraitKey.SteadfastWill));
            Assert.That(ReconstructionTraitTests.Activations(result, TraitActivationKind.KnockbackApplied), Is.Empty);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [Test]
        public void NeighborPushedOntoHazardAndKilled_SelfDestructsAtTheTurnStartEnd()
        {
            var bomber = CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Sniper, new CellCoord(5, 4), Owner.Enemy, 20),
                hp: 5, traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(1) });
            var input = TurnInput(3, new[] { bomber, Demon(4, new CellCoord(7, 4)) },
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Sniper, 20, CategoryKey.Main, new SelfDestructTraitSpec(1)), SingleNoSplash),
                new[] { (new CellCoord(6, 4), 10) });
            var result = CombatTurnCalculator.BeginControllerTurn(input, DamageRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ReconstructionTraitTests.Final(result, input, 3).Health.IsDead, Is.True);
            var blast = ReconstructionTraitTests.Activations(result, TraitActivationKind.SelfDestructExploded).Single();
            Assert.That(blast.SourceUnitId, Is.EqualTo(new EntityId(3)));
            Assert.That(blast.TargetCell, Is.EqualTo(new CellCoord(6, 4)));
            Assert.That(ReconstructionTraitTests.Final(result, input, 4).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(ReconstructionTraitTests.Final(result, input, 2).Health.CurrentHealth, Is.EqualTo(100));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [Test]
        public void StageEnd_StopsTheRemainingPushes()
        {
            // 메인 10이 먼저 밀려 위험 오브젝트로 죽으면 패배가 정해지고, 뒤의 적 11은 밀지 않는다(설계 P13·P6).
            var input = TurnInput(3, new[] { Main(new CellCoord(3, 4)), Demon(11, new CellCoord(5, 4)) },
                hazards: new[] { (new CellCoord(2, 4), 150) });
            var result = CombatTurnCalculator.BeginControllerTurn(input, DamageRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(ReconstructionTraitTests.Final(result, input, 11).Anchor, Is.EqualTo(new CellCoord(5, 4)));
            Assert.That(ReconstructionTraitTests.Activations(result, TraitActivationKind.KnockbackApplied).Single().TargetIds,
                Is.EqualTo(new[] { new EntityId(10) }));
        }

        private static CombatCommandInput TurnInput(int level, IEnumerable<CharacterCombatFacts> neighbors,
            CombatAttackProfile extraProfile = null, (CellCoord Cell, int Damage)[] hazards = null)
        {
            var input = CombatCommandTestData.Input(SingleNoSplash, empty: true);
            var holder = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, Center, Owner.Enemy, 20),
                    traits: new TraitStatusSnapshot[] { new ReconstructionTraitStatusSnapshot(level, true) })
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[]
                    { new ReconstructionStatus(TraitEffectOrigin.Create(Holder, TraitKey.Reconstruction), 1) }));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[Holder] = holder;
            foreach (var neighbor in neighbors) characters[neighbor.EntityId] = neighbor;
            var profiles = input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, new ReconstructionTraitSpec(level)), SingleNoSplash));
            if (extraProfile != null) profiles = profiles.Append(extraProfile);
            var state = hazards == null ? input.State : EnvironmentDamageTests.WithDamageObjects(input.State, hazards);
            var source = CombatCommandTestData.With(input, characters.Values.OrderBy(c => c.EntityId).ToArray(), state, profiles.ToArray());
            return EnemyControllerPhaseTests.Input(source, CombatControllerPhase.StartingEnemy, completed: 1);
        }

        private static CharacterCombatFacts Demon(int id, CellCoord cell) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy);

        private static CharacterCombatFacts Player(CellCoord cell) =>
            CombatCommandTestData.With(CombatCommandTestData.Input(empty: true).Characters.Single(c => c.EntityId.Value == 1), position: cell);

        private static CharacterCombatFacts Main(CellCoord cell) =>
            CombatCommandTestData.With(CombatCommandTestData.Input(empty: true).Characters.Single(c => c.EntityId.Value == 10), position: cell);
    }
}
