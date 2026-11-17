using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 전용 공포 특성과 사망 특성 명령 끝 확인(Phase B)입니다.
    /// 기본 배치: 플레이어 공격자 1 (3,3) 공격력 20, 공포 적 2 (4,3) 체력 5, 이웃 적 3 (5,3), 먼 적 4 (6,6). 플레이어 턴입니다.
    /// </summary>
    public sealed class FearTraitTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly TargetingMethodSpec SingleNoSplash =
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        private static readonly CellCoord FearCell = new CellCoord(4, 3);

        [Test]
        public void AttackKill_RestrictsSurroundingCharactersUntilTheirNextOwnTurnEnds()
        {
            var input = FearInput();
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            // 플레이어 턴에 걸렸으므로 플레이어 쪽은 이번 턴 종료 감소를 한 번 더 버틴다(2), 적 쪽은 다음 적 턴 한 번(1).
            AssertFear(Final(result, input, 1), 2);
            AssertFear(Final(result, input, 3), 1);
            Assert.That(FearStatuses(Final(result, input, 4)), Is.Empty);

            var activation = FearActivations(result).Single();
            Assert.That(activation.SourceUnitId, Is.EqualTo(new EntityId(2)));
            Assert.That(activation.TraitActivationKind, Is.EqualTo(TraitActivationKind.StatusApplied));
            Assert.That(activation.TargetIds, Is.EqualTo(new[] { new EntityId(1), new EntityId(3) }));
            Assert.That(activation.TargetCells, Is.EqualTo(new[] { FearCell }));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [Test]
        public void Activation_ComesAfterTheAttackAndBeforeTheFearStateEvents()
        {
            var result = CombatCommandCalculator.Calculate(FearInput(), NoRules);
            var events = result.Events.ToList();
            int attack = events.FindIndex(e => e is CombatAttackEvent);
            int activation = events.FindIndex(e => e is CombatEffectsEvent fx && fx.Traits.Any(t => t.TraitKey == TraitKey.Fear));
            int firstFearState = events.FindIndex(e => e is CombatTraitStateEvent s && FearStatuses(s.Final).Any());
            Assert.That(attack, Is.GreaterThanOrEqualTo(0));
            Assert.That(activation, Is.GreaterThan(attack));
            Assert.That(firstFearState, Is.GreaterThan(activation));
        }

        [Test]
        public void PublicPreview_DoesNotRunFear()
        {
            CombatCommandCalculator.Preview(FearInput(), NoRules, out var projection);
            Assert.That(projection.Accepted, Is.True, projection.Reason);
            Assert.That(FearActivations(projection), Is.Empty);
            Assert.That(FearStatuses(Final(projection, FearInput(), 1)), Is.Empty);
        }

        [Test]
        public void LongerExistingRestriction_IsKept_AndOnlyChangedTargetsAreListed()
        {
            var other = TraitEffectOrigin.Create(new EntityId(9), TraitKey.Disarm);
            var neighbor = Enemy(3, new CellCoord(5, 3), 100).WithStatusEffects(new StatusEffectSet(new AppliedStatus[]
            {
                new ActionRestrictionStatus(StatusEffectKey.Disarm, other, 3),
                new ActionRestrictionStatus(StatusEffectKey.Bind, other, 3),
            }));
            var input = FearInput(neighbor);
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var kept = Final(result, input, 3).StatusEffects.Entries.OfType<ActionRestrictionStatus>().ToArray();
            Assert.That(kept.Select(s => (s.Key, s.Origin, s.RemainingAffectedSideTurnEnds)), Is.EquivalentTo(new[]
            {
                (StatusEffectKey.Disarm, (EffectOriginKey?)other, 3), (StatusEffectKey.Bind, (EffectOriginKey?)other, 3),
            }));
            Assert.That(FearActivations(result).Single().TargetIds, Is.EqualTo(new[] { new EntityId(1) }));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [Test]
        public void DeathWithoutAFearTrait_RunsNothing()
        {
            var input = FearInput(fearHolder: false);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(FearActivations(result), Is.Empty);
            Assert.That(FearStatuses(Final(result, input, 1)), Is.Empty);
        }

        [Test]
        public void Engine_ReportsFearAndRejectsAMismatchedSnapshot()
        {
            var engine = new CombatEffectEngine();
            Assert.That(engine.EvaluateDeathTraits(Array.Empty<EffectRuleBinding>()).HasContribution, Is.False);
            var evaluation = engine.EvaluateDeathTraits(new[] { FearBinding(StatelessEffectStateSnapshot.Instance) });
            Assert.That(evaluation.Fear, Is.True);
            Assert.That(evaluation.SelfDestructPercent, Is.Zero);
            Assert.That(evaluation.FearOrigin, Is.EqualTo(TraitEffectOrigin.Create(new EntityId(7), TraitKey.Fear)));
            Assert.Throws<ArgumentException>(() =>
                engine.EvaluateDeathTraits(new[] { FearBinding(new KillChainAttackEffectStateSnapshot()) }));
        }

        [Test]
        public void Eligibility_RejectsAFearHolderOnThePlayerSide()
        {
            var spec = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 2, CategoryKey.Heavy, 5, 0, 1, new TraitSpec[] { new FearTraitSpec() },
                UnitOwnerEligibility.Both);
            var definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault()).GetRequired(spec.Key);
            var error = Assert.Throws<InvalidOperationException>(() => UnitEligibilityRules.Require(definition, Owner.Player, false));
            Assert.That(error.Message, Does.Contain("적 전용"));
            Assert.DoesNotThrow(() => UnitEligibilityRules.Require(definition, Owner.Enemy, false));
        }

        [TestCase(StagePhase.Combat, CombatControllerPhase.Player, 3L, Owner.Player)]
        [TestCase(StagePhase.Combat, CombatControllerPhase.Enemy, 3L, Owner.Enemy)]
        [TestCase(StagePhase.Combat, CombatControllerPhase.StartingEnemy, 3L, Owner.Player)]
        [TestCase(StagePhase.Combat, CombatControllerPhase.StartingPlayer, 4L, Owner.Enemy)]
        public void PendingEndSide_IsTheSideWhoseEndDecrementComesNext(StagePhase stage, CombatControllerPhase phase,
            long completed, Owner expected)
        {
            var turn = new CombatTurnFacts(stage, controllerPhase: phase, controllerTurnSequence: 1, completedControllerTurnCount: completed);
            Assert.That(ActionRestrictionTiming.PendingEndSide(turn), Is.EqualTo(expected));
            Assert.That(ActionRestrictionTiming.Duration(1, expected, turn), Is.EqualTo(2));
            Assert.That(ActionRestrictionTiming.Duration(1, expected == Owner.Player ? Owner.Enemy : Owner.Player, turn), Is.EqualTo(1));
        }

        [Test]
        public void PendingEndSide_IsNoneAtCombatEntry()
        {
            var entry = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.StartingPlayer,
                controllerTurnSequence: 0, completedControllerTurnCount: 0);
            Assert.That(ActionRestrictionTiming.PendingEndSide(entry), Is.Null);
        }

        [TestCase(2, 1, 2)]
        [TestCase(1, 2, 2)]
        [TestCase(2, 2, 2)]
        public void Restrictions_KeepTheLongerRemainingCount(int existing, int incoming, int expected)
        {
            var first = TraitEffectOrigin.Create(new EntityId(8), TraitKey.Disarm);
            var second = TraitEffectOrigin.Create(new EntityId(9), TraitKey.Fear);
            var current = new StatusEffectSet(new AppliedStatus[] { new ActionRestrictionStatus(StatusEffectKey.Bind, first, existing) });
            var applied = AppliedStatusRules.Apply(current, new ActionRestrictionStatus(StatusEffectKey.Bind, second, incoming));
            var bind = applied.Entries.OfType<ActionRestrictionStatus>().Single();
            Assert.That(bind.RemainingAffectedSideTurnEnds, Is.EqualTo(expected));
            // 같으면 새 것으로 바꾸고, 기존이 더 길면 기존 출처가 남는다.
            Assert.That(bind.Origin, Is.EqualTo(existing > incoming ? first : second));
        }

        private static CombatCommandInput FearInput(CharacterCombatFacts neighbor = null, bool fearHolder = true)
        {
            var input = CombatCommandTestData.Input(SingleNoSplash, AttackAim.ForCell(FearCell), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], position: new CellCoord(3, 3));
            var fear = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, FearCell, Owner.Enemy),
                hp: 5, traits: fearHolder ? new TraitStatusSnapshot[] { new FearTraitStatusSnapshot() } : Array.Empty<TraitStatusSnapshot>());
            var characters = new[]
            {
                actor, input.Characters[1], fear,
                neighbor ?? Enemy(3, new CellCoord(5, 3), 100),
                Enemy(4, new CellCoord(6, 6), 100),
            };
            var profiles = input.Profiles.Append(new CombatAttackProfile(fearHolder
                ? CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 1, CategoryKey.Main, new FearTraitSpec())
                : CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 1), SingleNoSplash)).ToArray();
            return CombatCommandTestData.With(input, characters, profiles: profiles);
        }

        private static CharacterCombatFacts Enemy(int id, CellCoord cell, int hp) =>
            CombatCommandTestData.With(CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy), hp: hp);

        private static CharacterCombatFacts Final(CombatCommandTransition result, CombatCommandInput input, int id)
        {
            var entityId = new EntityId(id);
            return result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == entityId)?.Final ??
                input.Characters.Single(c => c.EntityId == entityId);
        }

        private static ActionRestrictionStatus[] FearStatuses(CharacterCombatFacts character) =>
            character.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Where(s => s.Origin.HasValue &&
                s.Origin.Value.Kind == EffectOwnerKind.Trait && s.Origin.Value.DefinitionKey == (int)TraitKey.Fear).ToArray();

        private static void AssertFear(CharacterCombatFacts character, int remaining)
        {
            var fear = FearStatuses(character);
            Assert.That(fear.Select(s => s.Key), Is.EquivalentTo(new[] { StatusEffectKey.Disarm, StatusEffectKey.Bind }), character.EntityId.ToString());
            Assert.That(fear.All(s => s.RemainingAffectedSideTurnEnds == remaining), Is.True, character.EntityId.ToString());
            Assert.That(fear.All(s => s.Origin == TraitEffectOrigin.Create(new EntityId(2), TraitKey.Fear)), Is.True);
        }

        private static TraitActivationSnapshot[] FearActivations(CombatCommandTransition result) =>
            result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitKey == TraitKey.Fear).ToArray();

        private static EffectRuleBinding FearBinding(IEffectStateSnapshot state) => new EffectRuleBinding(
            TraitEffectOrigin.Create(new EntityId(7), TraitKey.Fear), new FearEffectRule(new FearTraitDefinition(), new FearTraitRule()), state);
    }
}
