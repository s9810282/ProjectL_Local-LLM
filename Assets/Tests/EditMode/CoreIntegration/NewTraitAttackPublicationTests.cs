using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class NewTraitAttackPublicationTests
    {
        [Test]
        public void PreviewIsolated_CommitMatchesEvents()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new NewTraitSource());
            var target = battle.Character(EntityKey.Unit_Demon);
            var before = battle.CaptureGameplay();
            for (int i = 0; i < 2; i++)
            {
                battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target.Anchor.Value)));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            }
            var parts = battle.Publication();
            var input = parts.Attack(battle.Actor, target.Anchor.Value);
            var proposal = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(proposal.Accepted, Is.True, proposal.Reason);
            Assert.That(battle.Character(EntityKey.Unit_Demon).StatusEffects.Entries, Is.Empty);
            Assert.That(parts.Publisher.Publish(proposal), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events, Is.EqualTo(proposal.Events));
            var final = battle.Character(EntityKey.Unit_Demon);
            Assert.That(final.StatusEffects.Entries.OfType<FireStatus>().Count(), Is.EqualTo(1));
            Assert.That(final.StatusEffects.Entries.OfType<BleedingStatus>().Single().Stacks.Single().Damage, Is.EqualTo(2));
            var eventFinal = proposal.Events.OfType<CombatTraitStateEvent>().Last(e => e.Final.EntityId == target.EntityId).Final;
            Assert.That(eventFinal.StatusEffects.SameValues(final.StatusEffects), Is.True);
            Assert.That(proposal.FinalState.ItemRandomState, Is.EqualTo(input.State.ItemRandomState));
        }

        [Test]
        public void CanonicalStatusesSurviveCommit()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new NewTraitSource());
            var target = battle.Character(EntityKey.Unit_Demon);
            var initial = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == target.EntityId);
            battle.Submit(battle.Attack(battle.Actor, target.Anchor.Value));
            var applied = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == target.EntityId);
            Assert.That(applied.StatusEffects, Is.SameAs(applied.ToFacts().StatusEffects));
            Assert.That(applied.StatusEffectSnapshots.OfType<FireEffectSnapshot>().Single().RemainingAffectedSideStarts, Is.EqualTo(2));
            Assert.That(applied.StatusEffectSnapshots.OfType<BleedingEffectSnapshot>().Single().Stacks.Single().Origin.OwnerInstanceId,
                Is.EqualTo(battle.Actor.EntityId.Value));
            Assert.That(applied.StatusEffectSnapshots.OfType<MovementRangeDecreaseEffectSnapshot>().Single().ExpiresAtCompletedControllerTurnCount,
                Is.EqualTo(2));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(battle.Character(EntityKey.Unit_Demon).StatusEffects.Entries.OfType<FireStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(1));
            Assert.That(applied.StatusEffectSnapshots.OfType<FireEffectSnapshot>().Single().RemainingAffectedSideStarts, Is.EqualTo(2));
            Assert.That(initial.StatusEffects.Entries, Is.Empty);
        }

        internal sealed class NewTraitSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new TraitKillRewardPublicationTests.KillSource().Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => s.Key != EntityKey.Unit_Cowboy ? s : new UnitSpec(s.Key, s.Role,
                    s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod,
                    s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                    new TraitSpec[] { new LavaCannonTraitSpec(), new BleedingTraitSpec(2) }, s.OwnerEligibility, s.Occlusion,
                    s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures,
                    basis.Categories, basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key),
                        p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
