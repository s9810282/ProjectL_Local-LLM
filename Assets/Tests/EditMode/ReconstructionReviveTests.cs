using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 재구성 중 행동 막기와 자기 진영 턴 시작 부활(Phase D)입니다.
    /// 배치는 <see cref="ReconstructionTraitTests"/>와 같고, 플레이어 공격으로 적 2가 재구성한 뒤 진영 턴 시작을 차례로 엽니다.
    /// </summary>
    public sealed class ReconstructionReviveTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly EntityId Holder = new EntityId(2);

        [TestCase(1, 0)]
        [TestCase(2, 50)]
        public void PlayerTurnTrigger_BlocksTheNextEnemyTurn_AndRevivesOnTheOneAfter(int level, int shield)
        {
            var attackInput = ReconstructionTraitTests.Input(level);
            var attack = CombatCommandCalculator.Calculate(attackInput, NoRules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);

            var firstInput = TurnStart(attackInput, All(attack, attackInput), CombatControllerPhase.StartingEnemy, 1);
            var first = CombatTurnCalculator.BeginControllerTurn(firstInput, Array.Empty<IMovementTileRule>());
            Assert.That(first.Accepted, Is.True, first.Reason);
            var waiting = ReconstructionTraitTests.Final(first, firstInput, 2);
            Assert.That(ReconstructionTraitTests.Reconstruction(waiting).RemainingOwnTurnStarts, Is.EqualTo(1));
            Assert.That(ReconstructionTraitTests.Activations(first, TraitActivationKind.ReconstructionEnded), Is.Empty);
            Assert.That(ReconstructionTraitTests.Shield(waiting), Is.EqualTo(shield));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(first.Events));

            // 재구성 중에는 이동·공격 모두 막고 이유를 재구성으로 낸다.
            var blocked = All(first, firstInput);
            Assert.That(CombatActionAdmission.Actor(EnemyAction(firstInput, blocked, CombatCommandKind.Move), CombatCommandKind.Move, out _),
                Is.EqualTo(MovementFailureReason.Reconstructing));
            Assert.That(CombatActionAdmission.Actor(EnemyAction(firstInput, blocked, CombatCommandKind.Attack), CombatCommandKind.Attack, out _),
                Is.EqualTo(MovementFailureReason.Reconstructing));
            Assert.That(CombatAttackQueries.IntrinsicUnitReadiness(firstInput, waiting), Is.EqualTo(AttackFailureReason.Reconstructing));
            Assert.That(CombatActionAdmission.IsAttackRestricted(waiting), Is.True);
            Assert.That(CombatActionAdmission.IsMovementRestricted(waiting), Is.True);

            var secondInput = TurnStart(firstInput, blocked, CombatControllerPhase.StartingEnemy, 2);
            var second = CombatTurnCalculator.BeginControllerTurn(secondInput, Array.Empty<IMovementTileRule>());
            Assert.That(second.Accepted, Is.True, second.Reason);
            var revived = ReconstructionTraitTests.Final(second, secondInput, 2);
            Assert.That(ReconstructionTraitTests.Reconstruction(revived), Is.Null);
            Assert.That(revived.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(revived.ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(ReconstructionTraitTests.Shield(revived), Is.EqualTo(shield));

            var ended = ReconstructionTraitTests.Activations(second, TraitActivationKind.ReconstructionEnded).Single();
            Assert.That(ended.SourceUnitId, Is.EqualTo(Holder));
            Assert.That(ended.TargetIds, Is.EqualTo(new[] { Holder }));
            var events = second.Events.ToList();
            Assert.That(events.FindIndex(e => e is CombatEffectsEvent effects &&
                    effects.Traits.Any(t => t.TraitActivationKind == TraitActivationKind.ReconstructionEnded)),
                Is.LessThan(events.FindIndex(e => e is CombatTraitStateEvent state && state.Final.EntityId == Holder &&
                    ReconstructionTraitTests.Reconstruction(state.Expected) != null && ReconstructionTraitTests.Reconstruction(state.Final) == null)));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(second.Events));

            var acting = All(second, secondInput);
            Assert.That(CombatActionAdmission.Actor(EnemyAction(secondInput, acting, CombatCommandKind.Move), CombatCommandKind.Move, out _),
                Is.EqualTo(MovementFailureReason.None));
        }

        [Test]
        public void PlayerTurnStart_DoesNotCountForAnEnemyReconstruction()
        {
            var attackInput = ReconstructionTraitTests.Input(1);
            var attack = CombatCommandCalculator.Calculate(attackInput, NoRules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);

            var playerInput = TurnStart(attackInput, All(attack, attackInput), CombatControllerPhase.StartingPlayer, 1);
            var player = CombatTurnCalculator.BeginControllerTurn(playerInput, Array.Empty<IMovementTileRule>());
            Assert.That(player.Accepted, Is.True, player.Reason);
            Assert.That(ReconstructionTraitTests.Reconstruction(ReconstructionTraitTests.Final(player, playerInput, 2)).RemainingOwnTurnStarts,
                Is.EqualTo(2));
            Assert.That(ReconstructionTraitTests.Activations(player, TraitActivationKind.ReconstructionEnded), Is.Empty);
        }

        private static CharacterCombatFacts[] All(CombatCommandTransition result, CombatCommandInput input) =>
            input.Characters.Select(c => ReconstructionTraitTests.Final(result, input, c.EntityId.Value)).ToArray();

        private static CombatCommandInput TurnStart(CombatCommandInput source, CharacterCombatFacts[] characters,
            CombatControllerPhase phase, long completed) =>
            EnemyControllerPhaseTests.Input(source, phase, completed: completed, characters: characters);

        private static CombatCommandInput EnemyAction(CombatCommandInput source, CharacterCombatFacts[] characters, CombatCommandKind kind)
        {
            var phased = EnemyControllerPhaseTests.Input(source, CombatControllerPhase.Enemy, characters: characters);
            return new CombatCommandInput(0, kind, Holder, default, phased.State, phased.Characters.ToArray(), phased.Profiles,
                phased.Main, phased.Owned, phased.SoulCounts, phased.Souls, phased.Artifacts, battle: phased.Battle,
                processingKind: CombatProcessingKind.Advance, battleInstanceId: 17, environmentBindings: phased.EnvironmentBindings,
                obstacleContents: phased.ObstacleContents, expectedBattleInstanceId: 17,
                expectedControllerTurnSequence: phased.State.Turn.ControllerTurnSequence);
        }
    }
}
