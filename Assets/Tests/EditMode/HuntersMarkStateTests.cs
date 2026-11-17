using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class HuntersMarkStateTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static HuntersMarkState Mark => new(new EntityId(90), Owner.Player, 25);

        [Test]
        public void IndependentStatusChanges_PreserveMark()
        {
            var state = new CharacterDebuffState(huntersMark: Mark);
            var intimidated = CombatDebuffCalculator.ApplyIntimidation(state, new IntimidationTraitSpec(20));
            var reduced = CombatDebuffCalculator.ApplyDefenseReduction(intimidated, 30, false);
            var cleared = CombatDebuffCalculator.ClearIntimidation(reduced);
            var ticked = new DefenseReductionStatusRule().OnTurnEnded(cleared);
            foreach (var result in new[] { intimidated, reduced, cleared, ticked })
                Assert.That(result.HuntersMark?.SameValues(Mark), Is.True);
            Assert.That(ticked.Intimidation, Is.Null);
            Assert.That(ticked.DefenseReduction, Is.Null);
            Assert.That(state.Intimidation, Is.Null);
        }

        [Test]
        public void Equality_IncludesSourceOwnerPercentageAndAbsence()
        {
            var state = new CharacterDebuffState(huntersMark: Mark);
            Assert.That(state.SameValues(new CharacterDebuffState(huntersMark: Mark)), Is.True);
            Assert.That(state.SameValues(CharacterDebuffState.Empty), Is.False);
            Assert.That(state.SameValues(new CharacterDebuffState(huntersMark:
                new HuntersMarkState(new EntityId(91), Owner.Player, 25))), Is.False);
            Assert.That(state.SameValues(new CharacterDebuffState(huntersMark:
                new HuntersMarkState(new EntityId(90), Owner.Enemy, 25))), Is.False);
            Assert.That(state.SameValues(new CharacterDebuffState(huntersMark:
                new HuntersMarkState(new EntityId(90), Owner.Player, 26))), Is.False);
        }

        [TestCase("damage")]
        [TestCase("movement")]
        [TestCase("turn-reset")]
        [TestCase("hand-return")]
        public void CopiesPreserveMark_ReserveEntryClearsItWithoutChangingSource(string operation)
        {
            var source = CombatCommandTestData.Input().Characters[0].With(debuffs: new CharacterDebuffState(huntersMark: Mark));
            var final = operation switch
            {
                "damage" => CombatCommandCalculator.Hurt(source, 5),
                "movement" => source.WithPlacement(source.Area, source.ReserveSlotIndex, CellCoord.Right,
                    source.Rotation, new[] { CellCoord.Right }, actionUse: UnitActionUse.Moved),
                "turn-reset" => source.With(actionUse: UnitActionUse.Available),
                "hand-return" => CombatPlacementCalculator.Location(source, CharacterRuntimeArea.Hand, 0, null, null),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
            if (operation == "hand-return")
                Assert.That(final.Debuffs.HuntersMark, Is.Null);
            else
                Assert.That(final.Debuffs.HuntersMark.SameValues(Mark), Is.True);
            Assert.That(source.Debuffs.HuntersMark.SameValues(Mark), Is.True);
        }

        [Test]
        public void SourceDeath_DoesNotClearVictimsMark()
        {
            var input = CombatCommandTestData.Input();
            var mark = new HuntersMarkState(new EntityId(2), Owner.Enemy, 10);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.With(debuffs: new CharacterDebuffState(huntersMark: mark))
                : c.EntityId.Value == 2 ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Single(c => c.EntityId.Value == 2).Health.IsDead, Is.True);
            Assert.That(final.Characters.Single(c => c.EntityId == input.ActorId).Debuffs.HuntersMark.SameValues(mark), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TargetDeath_ClearsMarkThroughActualCommand(bool fall)
        {
            var input = fall ? CliffFallTests.Knockback() : CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? CombatCommandTestData.With(c, hp: fall ? 100 : 1).With(debuffs: new CharacterDebuffState(
                    huntersMark: new HuntersMarkState(input.ActorId, Owner.Player, 25))) : c).ToArray());
            if (fall)
                input = CliffFallTests.WithEnvironment(input, CliffFallTests.Ground(StaticTerrainKind.Cliff, cell: new CellCoord(5, 3)));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var target = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == 2);
            Assert.That(target.Health.IsDead, Is.True);
            Assert.That(target.Debuffs.HuntersMark, Is.Null);
            Assert.That(result.Events.OfType<CombatFallEvent>().Any(), Is.EqualTo(fall));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Debuffs.HuntersMark, Is.Not.Null);
        }

        [Test]
        public void Constructor_RejectsInvalidSourceOwnerAndPercent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkState(default, Owner.Player, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkState(new EntityId(1), Owner.Neutral, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkState(new EntityId(1), Owner.Player, -1));
            Assert.That(new HuntersMarkState(new EntityId(1), Owner.Player, int.MaxValue).BonusDamagePercent, Is.EqualTo(int.MaxValue));
        }

        [TestCase(1, false)]
        public void TurnPreservesMark_NightRecallClearsOnlyReturnedRecipients(long playerTurns, bool nightEnds)
        {
            var seed = CombatCommandTestData.Input();
            seed = CombatCommandTestData.With(seed, seed.Characters.Select(c =>
                c.With(debuffs: new CharacterDebuffState(huntersMark: Mark))).ToArray());
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                controllerTurnSequence: 1, completedPlayerTurnCount: playerTurns);
            var state = new CombatCommandState(8,8,CombatFlowPhase.Automatic,                seed.State.Structures,turn);
            var input = new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, seed.ActorId, seed.Aim,
                state, seed.Characters, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(4, 1, 8, 8, Array.Empty<CharacterInstanceId>(), 0),
                processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: seed.ExpectedBattleInstanceId,
                expectedControllerTurnSequence: seed.ExpectedControllerTurnSequence, battleInstanceId: seed.BattleInstanceId);
            var result = CombatTurnCalculator.EndEnemyPhase(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result);
            foreach (var character in final.Characters)
            {
                if (character.Area == CharacterRuntimeArea.Field)
                    Assert.That(character.Debuffs.HuntersMark?.SameValues(Mark), Is.True);
                else
                    Assert.That(character.Debuffs.HuntersMark, Is.Null);
            }
            Assert.That(final.Characters.Single(c => c.EntityId == input.ActorId).Area,
                Is.EqualTo(nightEnds ? CharacterRuntimeArea.Hand : CharacterRuntimeArea.Field));
        }
    }
}
