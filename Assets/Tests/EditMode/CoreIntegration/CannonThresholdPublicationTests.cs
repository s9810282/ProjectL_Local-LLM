using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CannonThresholdPublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ThreeFactors_PublishExactDamageAtomically(bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ThresholdSource(true));
            var victim = battle.Character(EntityKey.Unit_Demon);
            var publication = battle.Publication();
            var input = publication.Attack(battle.Actor, victim.Anchor.Value);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var owned = battle.Buffers.CharacterReadSink.Owned.ToArray();
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Single().TargetHealthAfter, Is.EqualTo(940));
            before.AssertUnchanged(battle.Buffers);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(60));
            if (stale)
            {
                battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(victim, null, 999));
                before = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                before.AssertUnchanged(battle.Buffers);
                return;
            }
            publication.Publisher.Publish(transition);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(940));
            Assert.That(battle.Buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(60));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            publication.Publisher.Publish(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefeatPreviewAndPublication_AreAtomic(bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ThresholdSource());
            var victim = battle.Character(EntityKey.Unit_Demon);
            var input = battle.Publication().Attack(battle.Actor, victim.Anchor.Value);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Single().TargetHealthAfter, Is.Zero);
            before.AssertUnchanged(battle.Buffers);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            if (stale)
            {
                battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(victim, null, 14));
                before = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(result));
                before.AssertUnchanged(battle.Buffers);
                return;
            }
            battle.Buffers.BattleWriteSink.Apply(result);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.IsDead, Is.True);
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(result);
            once.AssertUnchanged(battle.Buffers);
        }
        private sealed class ThresholdSource : IBattleInitializationSource
        {
            private readonly bool _fourFactors;
            internal ThresholdSource(bool fourFactors = false) { _fourFactors = fourFactors; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? (_fourFactors ? 1000 : 15) : s.MaxHealth, s.MoveMethod, s.AttackRange,
                    _fourFactors && s.Key == EntityKey.Unit_Demon ? basis.Units.Single(u => u.Key == EntityKey.Unit_Cowboy).CategoryKey : s.CategoryKey,
                    s.Key == EntityKey.Unit_Cowboy ? (_fourFactors ? 10 : 100) : _fourFactors && s.Key == EntityKey.Unit_Demon ? 5 : s.AttackPower,
                    _fourFactors && s.Key == EntityKey.Unit_Demon ? 0 : s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? (_fourFactors ? new TraitSpec[] { new StationaryFireTraitSpec(100),
                        new TrackingShotTraitSpec(50, 100), new FocusedBombardmentTraitSpec(1, 50) }
                        : new TraitSpec[] { new FinishingShotTraitSpec(20) })
                        : s.Key == EntityKey.Unit_Demon ? (_fourFactors ? Array.Empty<TraitSpec>() : new TraitSpec[] { new SoloActionTraitSpec(100, 1, 0, 1) }) : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
