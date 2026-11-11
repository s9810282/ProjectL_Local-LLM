using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 낙석 충돌과 적 전용 사망 특성(Phase F.1)입니다. 기본 배치는 <see cref="RockfallGimmickTests.Input"/>
    /// (플레이어 1 (1,3), 메인 10 (7,7), 적 2 (4,3)·3 (4,4)·4 (5,3))이고, 경고 칸을 정한 충돌을 적 진영 종료 경계 4에서 엽니다.
    /// 특성화 사례는 정산 경로를 이동 투영 완료로 바꾸기 전의 결과를 고정합니다(계획 TP-2).
    /// </summary>
    public sealed class RockfallDeathTraitTests
    {
        private static readonly TargetingMethodSpec SingleNoSplash =
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        private static readonly CellCoord[] ThreeCells = { new CellCoord(4, 3), new CellCoord(4, 4), new CellCoord(5, 3) };

        // 2026-11-10 정산 경로를 바꾸기 전(낙석이 Finish를 직접 부르던 판)의 출력이다.
        private const string MixedExpected =
            "events: RockfallImpact:[2/3/4], Died:2:10>0, HealthChanged:3:100>80, TraitState:3, HealthChanged:4:100>80, TraitState:4\n" +
            "final: C1:100:Field:, C2:0:Excluded:, C3:80:Field:Bind, C4:80:Field:Bind, C10:100:Field:\n" +
            "souls:0, consumed:0, outcome:InProgress, phase:PlayerInput, gimmicks:RockfallGimmickState@6, reward:False, rewardSettlement:True";
        private const string TerminalExpected =
            "events: RockfallImpact:[4/10], Died:4:100>0, Died:10:100>0, CombatOutcomeEvent/StageEnded\n" +
            "final: C1:100:Field:, C2:100:Field:, C3:100:Field:, C4:0:Excluded:, C10:0:Excluded:\n" +
            "souls:0, consumed:0, outcome:Defeat, phase:Finished, gimmicks:, reward:False, rewardSettlement:True";

        [Test]
        public void Characterization_MixedDeathAndSurvival()
        {
            var input = Impact(ThreeCells, edit: c => c.EntityId.Value == 2 ? CombatCommandTestData.With(c, hp: 10) : c);
            Pin(Advance(input), MixedExpected);
        }

        [Test]
        public void Characterization_MainDeathEndsTheStage()
        {
            var input = Impact(new[] { new CellCoord(7, 7), new CellCoord(5, 3) }, new RockfallGimmickSpec(maximumHealthLossPercent: 100));
            Pin(Advance(input), TerminalExpected);
        }

        [Test]
        public void RockfallDeath_SelfDestructsAfterEveryRecipientEvent()
        {
            var bomber = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, new CellCoord(4, 3), Owner.Enemy, 20),
                hp: 10, traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(1) });
            var input = Impact(ThreeCells, edit: c => c.EntityId.Value == 2 ? bomber : c,
                extra: Profile(new SelfDestructTraitSpec(1)));
            var result = Advance(input);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 2).Health.IsDead, Is.True);
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(70));
            Assert.That(Final(result, 4).Health.CurrentHealth, Is.EqualTo(70));
            Assert.That(Final(result, 1).Health.CurrentHealth, Is.EqualTo(100));
            var events = result.Events.ToList();
            int lastRecipient = events.FindLastIndex(e => e is CombatTraitStateEvent s && s.Final.EntityId.Value == 4 &&
                s.Final.StatusEffects.Entries.Any(x => x.Key == StatusEffectKey.Bind));
            int blast = events.FindIndex(e => e is CombatEffectsEvent f && f.Traits.Any(t => t.TraitActivationKind == TraitActivationKind.SelfDestructExploded));
            Assert.That(lastRecipient, Is.GreaterThanOrEqualTo(0));
            Assert.That(blast, Is.GreaterThan(lastRecipient));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        [Test]
        public void RockfallDeath_TriggersFear_AndKeepsTheLongerRockfallBind()
        {
            var fear = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, new CellCoord(4, 3), Owner.Enemy),
                hp: 10, traits: new TraitStatusSnapshot[] { new FearTraitStatusSnapshot() });
            var input = Impact(ThreeCells, edit: c => c.EntityId.Value == 2 ? fear : c, extra: Profile(new FearTraitSpec()));
            var result = Advance(input);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var origin = TraitEffectOriginOf(2, TraitKey.Fear);
            foreach (int id in new[] { 3, 4 })
            {
                var statuses = Final(result, id).StatusEffects.Entries.OfType<ActionRestrictionStatus>().ToArray();
                Assert.That(statuses.Any(s => s.Key == StatusEffectKey.Disarm && s.Origin == origin), Is.True, id.ToString());
                // 낙석 속박(남은 2)이 공포 속박(남은 1)보다 길어 그대로 남는다.
                Assert.That(statuses.Single(s => s.Key == StatusEffectKey.Bind).Origin, Is.Not.EqualTo(origin), id.ToString());
            }
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        [Test]
        public void ReconstructionHolder_SurvivesRockfall_AndIsBound()
        {
            var holder = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, new CellCoord(4, 3), Owner.Enemy),
                hp: 10, traits: new TraitStatusSnapshot[] { new ReconstructionTraitStatusSnapshot(1, false) });
            var input = Impact(new[] { new CellCoord(4, 3) }, edit: c => c.EntityId.Value == 2 ? holder : c,
                extra: Profile(new ReconstructionTraitSpec(1)));
            var result = Advance(input);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = Final(result, 2);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(ReconstructionTraitTests.Reconstruction(final).RemainingOwnTurnStarts, Is.EqualTo(2));
            Assert.That(final.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Any(s => s.Key == StatusEffectKey.Bind), Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Died), Is.False);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Final.EntityId.Value == 2).Kind,
                Is.EqualTo(CombatEventKind.Reconstructed));
            var started = ReconstructionTraitTests.Activations(result, TraitActivationKind.ReconstructionStarted).Single();
            Assert.That(started.Magnitude, Is.EqualTo(10));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        private static CombatCommandInput Impact(CellCoord[] cells, RockfallGimmickSpec spec = null,
            Func<CharacterCombatFacts, CharacterCombatFacts> edit = null, CombatAttackProfile extra = null)
        {
            var input = RockfallGimmickTests.At(RockfallGimmickTests.Input(spec), 4);
            var state = input.State.With(gimmicks: input.State.Gimmicks.Replace(
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 4, cells)));
            var characters = edit == null ? input.Characters.ToArray() : input.Characters.Select(edit).ToArray();
            var profiles = extra == null ? input.Profiles.ToArray() : input.Profiles.Append(extra).ToArray();
            return CombatCommandTestData.With(input, characters, state, profiles);
        }

        private static CombatAttackProfile Profile(TraitSpec trait) =>
            new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, trait), SingleNoSplash);

        private static CombatCommandTransition Advance(CombatCommandInput input) =>
            CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Enemy, 4));

        private static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == id);

        private static EffectOriginKey TraitEffectOriginOf(int owner, TraitKey key) =>
            TraitEffectOrigin.Create(new EntityId(owner), key);

        private static void Pin(CombatCommandTransition result, string expected)
        {
            Assert.That(result.Accepted, Is.True, result.Reason);
            string actual = Describe(result);
            // 기대값이 없으면 현재 동작을 출력한다. 검토를 거친 뒤에만 기대값으로 고정한다.
            if (expected == null) Assert.Fail("ACTUAL<<\n" + actual + "\n>>ACTUAL");
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static string Describe(CombatCommandTransition result)
        {
            var characters = CombatCommandComposition.Project(result).Characters;
            return string.Join("\n",
                "events: " + string.Join(", ", result.Events.Select(Signature)),
                "final: " + string.Join(", ", characters.OrderBy(c => c.EntityId).Select(c =>
                    $"C{c.EntityId.Value}:{c.Health.CurrentHealth}:{c.Area}:" +
                    string.Join("+", c.StatusEffects.Entries.Select(s => s.Key.ToString())))),
                $"souls:{result.Field.Final.Entries.Count}, consumed:{result.ConsumedSoulIds.Count}, outcome:{result.Outcome.Outcome}, " +
                $"phase:{result.FinalState.Phase}, gimmicks:" +
                string.Join("/", result.FinalState.Gimmicks.Entries.Select(g => g.GetType().Name + "@" + g.NextBoundaryCount)) +
                $", reward:{result.RewardChange != null}, rewardSettlement:{result.RewardSettlement != null}");
        }

        private static string Signature(CombatEvent e) => e switch
        {
            CombatCharacterEvent c => $"{c.Kind}:{c.Final.EntityId.Value}:{c.Expected.Health.CurrentHealth}>{c.Final.Health.CurrentHealth}",
            CombatTraitStateEvent s => $"TraitState:{s.Final.EntityId.Value}",
            CombatRockfallEvent r => $"Rockfall{r.Step}:[{string.Join("/", r.RecipientIds.Select(id => id.Value))}]",
            CombatEffectsEvent f => "Effects:" + string.Join("+", f.Traits.Select(t => t.TraitKey + "." + t.TraitActivationKind)),
            _ => e.GetType().Name + "/" + e.Kind,
        };
    }
}
