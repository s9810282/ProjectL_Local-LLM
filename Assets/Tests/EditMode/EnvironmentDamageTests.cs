using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class EnvironmentDamageTests
    {
        [TestCase(AttackTargetRole.Primary, false, false, 11, 10, 11, 11)]
        [TestCase(AttackTargetRole.Additional, false, false, 7, 10, 7, 7)]
        [TestCase(AttackTargetRole.Primary, true, false, 11, 7, 8, 8)]
        [TestCase(AttackTargetRole.Additional, true, false, 7, 7, 5, 5)]
        [TestCase(AttackTargetRole.Primary, true, true, 11, 1, 11, 11)]
        [TestCase(AttackTargetRole.Additional, true, true, 7, 1, 7, 7)]
        [TestCase(AttackTargetRole.Primary, true, false, 11, 7, 8, 7, 5, 7)]
        [TestCase(AttackTargetRole.Additional, true, false, 7, 7, 5, 4, 5, 4)]
        [TestCase(AttackTargetRole.Additional, false, false, 0, 10, 0, 0, 0, 0, 1)]
        public void Attack_PreservesMatchupAndPhysicalDefense_BeforeFinalMinimum(AttackTargetRole role, bool heavy,
            bool structure, int before, int numerator, int after, int final,
            int defense = 0, int? afterDefense = null, int splashPercent = 70)
        {
            var input = CombatCommandTestData.Input();
            var actor = CombatCommandTestData.With(input.Characters[0], category:
                heavy ? CategorySnapshot.CreateStateless(CategoryKey.Rider) : CategorySnapshot.CreateStateless(CategoryKey.Main));
            var victim = CombatCommandTestData.With(input.Characters[2], category:
                heavy ? CategorySnapshot.CreateStateless(CategoryKey.Heavy) : CategorySnapshot.CreateStateless(CategoryKey.Main));
            var stats = victim.Stats;
            victim = CombatCommandCalculator.Copy(victim, victim.Health, new UnitStatsSnapshot(
                stats.MoveMethod,stats.MoveRange,stats.AppointRange,stats.AttackPower,defense,stats.Priority,stats.TraitStatuses.ToArray(),stats.Category));
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20,
                actor.Stats.Category.Key), profiles[0].Method);
            profiles[1] = new CombatAttackProfile(CombatCommandTestData.Spec(victim.CharacterKey, 1,
                victim.Stats.Category.Key), profiles[1].Method);
            input = CombatCommandTestData.With(input, profiles: profiles);
            var result = CombatDamageCalculator.Attack(input, new CombatEffectEngine(), new TargetDamagePipeline(
                new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())), actor, structure ? null : victim,
                11, role, splashPercent, actor.Anchor, null);
            Assert.That(result.DamageBeforeMatchup, Is.EqualTo(before));
            Assert.That(result.MatchupNumerator, Is.EqualTo(numerator));
            Assert.That(result.MatchupDenominator, Is.EqualTo(structure ? 1 : 10));
            Assert.That(result.DamageAfterMatchup, Is.EqualTo(after));
            Assert.That(result.DamageAfterPhysicalDefense, Is.EqualTo(afterDefense ?? after));
            Assert.That(result.IncomingReduction, Is.Zero);
            Assert.That(result.FinalDamage, Is.EqualTo(final));
            Assert.That(result.DamageAfterReceivedPercentages, Is.EqualTo((decimal)(afterDefense ?? after)));
        }

        [Test]
        public void EnvironmentalDamage_DoesNotGainCategoryOrHeavyRules()
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right);
            var actor = CombatCommandTestData.With(input.Characters[0],
                category: CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            var s = input.State;
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20,
                CategoryKey.Heavy), profiles[0].Method);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                profiles: profiles,
                state: EnvironmentDamageTests.WithDamageTiles(new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                    s.Structures,s.Turn,s.Environment), (CellCoord.Right, 7)));
            var rules = DamageRules;
            var preview = CombatMovementCalculator.Preview(input, rules);
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(preview.ProjectedActor.Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(EnvironmentDamageTests.ActiveDamageTiles(result.FinalState), Is.Empty);
            var damage = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.HealthChanged);
            Assert.That(damage.DamageOrigins.Count, Is.EqualTo(1));
            Assert.That(damage.DamageOrigins.Single().PhysicalCell, Is.EqualTo(CellCoord.Right));
            Assert.That(damage.DamageOrigins.Single().CreditedCharacterId, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PhysicalOrigin_IsIndependentOfCredit_AfterSourceConsumption(bool lethal)
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right);
            var s = input.State;
            var credit = input.Characters.Single(c => c.EntityId != input.ActorId).EntityId;
            var source = new DamageTile(71, CellCoord.Right, true, lethal ? 100 : 7, credit);
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth,s.BoardHeight,                s.Phase,s.Structures,s.Turn,                new MovementEnvironmentFacts(new MovementTileFact[] { source })));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new DamageRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(input.Characters.Single(c => c.EntityId == credit).Anchor, Is.EqualTo(new CellCoord(7, 7)));
            var damage = result.Events.OfType<CombatCharacterEvent>().Single(e =>
                e.Kind == (lethal ? CombatEventKind.Died : CombatEventKind.HealthChanged));
            Assert.That(damage.DamageOrigins.Count, Is.EqualTo(1));
            Assert.That(damage.DamageOrigins.Single().PhysicalCell, Is.EqualTo(CellCoord.Right));
            Assert.That(damage.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(credit));
            Assert.That(damage.Final.Health.CurrentHealth, Is.EqualTo(lethal ? 0 : 93));
        }

        [Test]
        public void AttackEvents_PreservePrimaryAndSplashPhysicalOrigins()
        {
            var input = CombatCommandTestData.Input();
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var changes = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.HealthChanged).ToArray();
            var primary = changes.Single(e => e.Final.EntityId == new EntityId(2));
            var splash = changes.Single(e => e.Final.EntityId == new EntityId(3));
            Assert.That(primary.DamageOrigins.Count, Is.EqualTo(1));
            Assert.That(splash.DamageOrigins.Count, Is.EqualTo(1));
            Assert.That(primary.DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(splash.DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(primary.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(input.ActorId));
            Assert.That(splash.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(input.ActorId));
        }

        [Test]
        public void LegacyEnvironment_AllowsMissingPhysicalOriginWithoutInventingCreditOrCategory()
        {
            var result = CombatDamageCalculator.Environment(7, new CombatDamageOrigin(null, null));
            Assert.That(result.FinalDamage, Is.EqualTo(7));
            Assert.That(result.MatchupNumerator, Is.EqualTo(1));
            Assert.That(result.MatchupDenominator, Is.EqualTo(1));
            Assert.That(result.IncomingReduction, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatDamageCalculator.Environment(-1,
                new CombatDamageOrigin(null, null)));
            Assert.Throws<ArgumentNullException>(() => CombatDamageCalculator.Environment(1, null));
            Assert.Throws<ArgumentException>(() => new CombatDamageOrigin(null, EntityId.Empty));
        }

        internal static readonly IMovementTileRule[] DamageRules = { new DamageRule() };

        internal static CombatCommandState WithDamageTiles(CombatCommandState state, params (CellCoord Cell, int Damage)[] hazards)
        {
            var progress = state.EnvironmentProgress;
            var tiles = state.Environment.Tiles.Concat(hazards.Select((hazard, index) =>
                (MovementTileFact)new DamageTile(checked(progress.NextSourceId + index), hazard.Cell, true, hazard.Damage, null))).ToArray();
            return state.With(environment: new MovementEnvironmentFacts(tiles), environmentProgress: new EnvironmentProgressFacts(
                progress.NextObjectEntryOrdinal, checked(progress.NextSourceId + hazards.Length), progress.RandomState));
        }

        internal static IReadOnlyList<MovementTileFact> ActiveDamageTiles(CombatCommandState state) =>
            state.Environment.Tiles.Where(t => t is DamageTile or DamageObject && t.Active).ToArray();

        internal static CombatCommandState WithDamageObjects(CombatCommandState state, params (CellCoord Cell, int Damage)[] hazards)
        {
            var progress = state.EnvironmentProgress;
            var tiles = state.Environment.Tiles.Concat(hazards.Select((hazard, index) =>
                (MovementTileFact)new DamageObject(checked(progress.NextSourceId + index), hazard.Cell,
                    checked(progress.NextObjectEntryOrdinal + index), true, hazard.Damage))).ToArray();
            return state.With(environment: new MovementEnvironmentFacts(tiles), environmentProgress: new EnvironmentProgressFacts(
                checked(progress.NextObjectEntryOrdinal + hazards.Length), checked(progress.NextSourceId + hazards.Length), progress.RandomState));
        }

        internal static EnvironmentSemanticBindings WithDamageObjectBindings(EnvironmentSemanticBindings bindings, CombatCommandState state)
        {
            var ids = state.Environment.Tiles.OfType<DamageObject>().Select(t => t.Id).Except(bindings.ObjectSourceIds).ToArray();
            return ids.Length == 0 ? bindings : new EnvironmentSemanticBindings(bindings.TerrainSourceIds,
                bindings.ObjectSourceIds.Concat(ids).ToArray(), bindings.ObstacleEntityIds);
        }

        private sealed class DamageObject : ObjectSourceFact
        {
            internal int Damage { get; }

            internal DamageObject(long id, CellCoord cell, long ordinal, bool active, int damage, IReadOnlyList<Owner> knownTo = null)
                : base(id, cell, "origin-fixture", ordinal, active, knownTo ?? new[] { Owner.Player, Owner.Enemy })
            {
                Damage = damage;
            }

            public override bool SameValues(MovementTileFact other) =>
                other is DamageObject obj && SameObjectValues(obj) && Damage == obj.Damage;
            public override ObjectSourceFact WithActive(bool active) =>
                new DamageObject(Id, Cell, EntryOrdinal, active, Damage, KnownTo);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
                new DamageObject(Id, Cell, EntryOrdinal, Active, Damage, knownTo);
        }

        internal sealed class DamageTile : MovementTileFact
        {
            internal int Damage { get; }
            internal EntityId? Credit { get; }
            internal DamageTile(long id, CellCoord cell, bool active, int damage, EntityId? credit, IReadOnlyList<Owner> knownTo = null)
                : base(id, cell, "origin-fixture", 0, active, knownTo ?? new[] { Owner.Player, Owner.Enemy })
            { Damage = damage; Credit = credit; }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
                other is DamageTile tile && Damage == tile.Damage && Credit == tile.Credit;
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
                new DamageTile(Id, Cell, Active, Damage, Credit, knownTo);
        }

        internal sealed class DamageRule : IMovementTileRule
        {
            public string Key => "origin-fixture";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact source)
            {
                if (input.Phase != MovementEffectPhase.Hazard) return new MovementStepEffects();
                if (source is DamageObject obj)
                    return new MovementStepEffects(damage: new[] { new MovementHealthRequest(input.MoverId, obj.Damage) },
                        expectedSource: obj, finalSource: obj.WithActive(false));

                var tile = (DamageTile)source;
                return new MovementStepEffects(damage: new[] { new MovementHealthRequest(input.MoverId,
                    tile.Damage, new CombatDamageOrigin(tile.Cell, tile.Credit)) }, expectedSource: tile,
                    finalSource: new DamageTile(tile.Id, tile.Cell, false, tile.Damage, tile.Credit, tile.KnownTo));
            }
        }
    }
}
