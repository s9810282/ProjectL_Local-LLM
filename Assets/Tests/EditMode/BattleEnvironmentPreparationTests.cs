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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class BattleEnvironmentPreparationTests
    {
        [Test]
        public void Preparation_WeakGroundInitialOccupantDoesNotCountAndFreshBattlesStayIndependent()
        {
            var ground = new StaticTerrainFact(71, CellCoord.Zero, StaticTerrainKind.WeakGround,
                true, new[] { Owner.Player }, 2);
            var initialization = Initialization(Environment(ground), Bindings(true, 71));
            var first = Prepare(initialization);
            var firstGround = (StaticTerrainFact)first.CommandState.Environment.Tiles.Single();
            var collapsed = firstGround.RegisterEntry().RegisterEntry().Collapse();
            var second = Prepare(initialization);
            var secondGround = (StaticTerrainFact)second.CommandState.Environment.Tiles.Single();

            Assert.That(first.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main).Anchor,
                Is.Null);
            Assert.That(first.Characters[0].Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(firstGround.EntryCount, Is.Zero);
            Assert.That(secondGround.EntryCount, Is.Zero);
            Assert.That(secondGround.EntryThreshold, Is.EqualTo(2));
            Assert.That(secondGround.PendingCollapse, Is.False);
            Assert.That(secondGround.Kind, Is.EqualTo(StaticTerrainKind.WeakGround));
            Assert.That(collapsed.Kind, Is.EqualTo(StaticTerrainKind.Cliff));
            Assert.That(first.Characters[0], Is.Not.SameAs(second.Characters[0]));
        }

        [Test]
        public void FieldPreparation_RejectsMainCharacterOnCliff()
        {
            var cliff = new StaticTerrainFact(71, CellCoord.Zero, StaticTerrainKind.Cliff, true, new[] { Owner.Player });
            Assert.Throws<InvalidOperationException>(() => PrepareFieldFacts(Initialization(Environment(cliff), Bindings(true, 71))));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Preparation_RejectsInitialStructureFootprintOnCliff(int offset)
        {
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 2, 1,
                new[] { CellCoord.Zero, new CellCoord(1, 0) }, 20, false, 0);
            var anchor = new CellCoord(4, 4);
            var cliff = new StaticTerrainFact(71, new CellCoord(anchor.X + offset, anchor.Y),
                StaticTerrainKind.Cliff, true, new[] { Owner.Player });
            var stage = Stage(new[] { new InitialEnemyStructurePlacementSpec(definition.Key, anchor, Rotation4.R0) });
            Assert.Throws<InvalidOperationException>(() => Prepare(Initialization(Environment(cliff),
                Bindings(true, 71), stage, new[] { definition })));
        }

        [Test]
        public void Preparation_RejectsAuthoredObstacleOnWeakGround()
        {
            var cell = new CellCoord(4, 3);
            var ground = new StaticTerrainFact(71, cell, StaticTerrainKind.WeakGround, true, new[] { Owner.Player }, 2);
            var original = Initialization(Environment(ground), Bindings(true, 71));
            var definition = new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1, new[] { CellCoord.Zero }, 10, false, 0);
            var initialized = new BattleInitialization(original.Stage, original.MainAnchor, original.MainRotation,
                original.Units, original.Structures, original.Categories, original.AttackProfiles,
                original.Environment, original.EnvironmentBindings,
                new[] { new InitialObstaclePlacementSpec(definition, ObstacleContentKind.PowderBarrel, cell, Rotation4.R0, 7) });
            Assert.Throws<InvalidOperationException>(() => Prepare(initialized));
        }

        [TestCase(StaticTerrainKind.WeakGround)]
        [TestCase(StaticTerrainKind.Cliff)]
        public void InitialEnvironment_RejectsDuplicateGroundCells(StaticTerrainKind kind)
        {
            var ground = new StaticTerrainFact(71, CellCoord.Zero, StaticTerrainKind.WeakGround, true, new[] { Owner.Player }, 2);
            var other = new StaticTerrainFact(72, CellCoord.Zero, kind, true, new[] { Owner.Player },
                kind == StaticTerrainKind.WeakGround ? 5 : 0);
            Assert.Throws<InvalidOperationException>(() =>
                CellEnvironmentQuery.ValidateInitialEnvironment(8, 6, Environment(ground, other)));
        }

        [Test]
        public void Preparation_MineSupportsTerrainCoexistenceWithoutSynthesizingArrival()
        {
            var environment = Environment(new StaticTerrainFact(71, CellCoord.Zero, StaticTerrainKind.HighGround, true, new[] { Owner.Player }),
                new MineSourceFact(11, CellCoord.Zero, 1, 7, true, new[] { Owner.Player }));
            var initialized = Initialization(environment, new EnvironmentSemanticBindings(new[] { 71L }, new[] { 11L }, Array.Empty<EntityId>()));
            var prepared = Prepare(initialized);
            Assert.That(prepared.CommandState.Environment.Tiles.All(t => t.Active), Is.True);
            Assert.That(prepared.CommandState.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(2));
        }

        [Test]
        public void Preparation_DeferredFlagIsPlacedInactiveWithoutShiftingOtherSources()
        {
            var terrain = new StaticTerrainFact(71, CellCoord.Zero, StaticTerrainKind.HighGround, true, new[] { Owner.Player });
            var flag = new FlagSourceFact(12, new CellCoord(2, 2), 2, 50, true, new[] { Owner.Player, Owner.Enemy });
            var mine = new MineSourceFact(11, new CellCoord(3, 2), 1, 7, true, new[] { Owner.Player });
            var environment = Environment(terrain, flag, mine);
            var initialized = Initialization(environment, new EnvironmentSemanticBindings(new[] { 71L }, new[] { 12L, 11L }, Array.Empty<EntityId>()));
            var prepared = Prepare(initialized);
            var tiles = prepared.CommandState.Environment.Tiles;
            Assert.That(tiles.Select(t => t.Id), Is.EqualTo(new[] { 71L, 12L, 11L }));
            var placed = (FlagSourceFact)tiles[1];
            Assert.That(placed.Active, Is.False);
            Assert.That(placed.SameValues(flag.WithActive(false)), Is.True);
            Assert.That(tiles[0].SameValues(terrain), Is.True);
            Assert.That(tiles[2].SameValues(mine), Is.True);
            Assert.That(prepared.CommandState.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(3));
            Assert.That(prepared.CommandState.EnvironmentProgress.NextSourceId, Is.EqualTo(72));
            Assert.That(prepared.Combat.EnvironmentBindings.ObjectSourceIds, Is.EqualTo(new[] { 12L, 11L }));
        }

        [Test]
        public void Preparation_TypedBadgesAllowTerrainCoexistenceWithoutSynthesizingArrival()
        {
            var environment = Environment(new StaticTerrainFact(71, CellCoord.Zero, StaticTerrainKind.HighGround, true, new[] { Owner.Player }),
                new SheriffBadgeSourceFact(99, CellCoord.Zero, 1, true, new[] { Owner.Player }),
                new SheriffBadgeSourceFact(1, CellCoord.Zero, 2, true, new[] { Owner.Player }));
            var initialized = Initialization(environment, new EnvironmentSemanticBindings(new[] { 71L }, new[] { 99L, 1L }, Array.Empty<EntityId>()));
            var prepared = Prepare(initialized);
            Assert.That(prepared.CommandState.Environment.Tiles.All(t => t.Active), Is.True);
            Assert.That(prepared.Characters.All(c => !c.HeldEnvironment.BadgeAvailable), Is.True);
            Assert.That(prepared.CommandState.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(3));
        }
        [TestCase(true)]
        [TestCase(false)]
        public void Preparation_SuppliesPreservesExplicitRewards_RejectsTerrainObstacleCoexistence(bool supplies)
        {
            var kind = supplies ? ObstacleContentKind.Supplies : ObstacleContentKind.PowderBarrel;
            var definition = new StructureSpec(ObstacleContentProfile.KeyFor(kind), 1, 1, new[] { CellCoord.Zero }, 10, false, 0);
            var original = Initialization(supplies ? null : Environment(Tile(11, new CellCoord(4, 3))),
                supplies ? null : Bindings(true, 11));
            var initialized = new BattleInitialization(original.Stage, original.MainAnchor, original.MainRotation,
                original.Units, original.Structures, original.Categories, original.AttackProfiles,
                original.Environment, original.EnvironmentBindings,
                new[] { new InitialObstaclePlacementSpec(definition, kind, new CellCoord(4, 3), Rotation4.R0, 0,
                    supplies ? 2 : 0, supplies ? 5 : 0) });
            if (supplies)
            {
                var profile = Prepare(initialized).Combat.ObstacleContents.Single();
                Assert.That(profile.HitAttackBonus, Is.EqualTo(2));
                Assert.That(profile.DestructionAttackBonus, Is.EqualTo(5));
            }
            else Assert.Throws<InvalidOperationException>(() => Prepare(initialized));
        }

        [Test]
        public void SuppliesWithoutExplicitRewards_RejectsRatherThanInventingBalance()
        {
            Assert.Throws<ArgumentException>(() => new ObstacleContentProfile(new EntityId(51), ObstacleContentKind.Supplies, 0));
            var definition = new StructureSpec(EntityKey.Structure_Supplies, 1, 1, new[] { CellCoord.Zero }, 10, false, 0);
            Assert.Throws<ArgumentException>(() => new InitialObstaclePlacementSpec(definition, ObstacleContentKind.Supplies,
                CellCoord.Zero, Rotation4.R0, 0));
        }

        [Test]
        public void Preparation_BarrelUsesAllocatedStructureIdentityWithoutArrival()
        {
            var definition = new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1, new[] { CellCoord.Zero }, 10, false, 0);
            var original = Initialization();
            var initialized = new BattleInitialization(original.Stage, original.MainAnchor, original.MainRotation,
                original.Units, original.Structures, original.Categories, original.AttackProfiles,
                original.Environment, original.EnvironmentBindings,
                new[] { new InitialObstaclePlacementSpec(definition, ObstacleContentKind.PowderBarrel, new CellCoord(4, 3), Rotation4.R0, 7) });
            var result = Prepare(initialized);
            Assert.That(result.Structures.Single().Definition.Key, Is.EqualTo(EntityKey.Structure_PowderBarrel));
            Assert.That(result.Structures.Single().Owner, Is.EqualTo(Owner.Neutral));
            Assert.That(result.Combat.ObstacleContents.Single().EntityId, Is.EqualTo(result.Structures.Single().EntityId));
            Assert.That(result.Combat.EnvironmentBindings.ObstacleEntityIds, Is.EqualTo(new[] { result.Structures.Single().EntityId }));
            Assert.That(result.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main).Health.CurrentHealth, Is.EqualTo(4));
        }

        [TestCase(StaticTerrainKind.HighGround)]
        [TestCase(StaticTerrainKind.Sandstorm)]
        public void Preparation_StaticTerrainPreservesBaseStatsWithoutArrival(StaticTerrainKind kind)
        {
            var source = new StaticTerrainFact(71, CellCoord.Zero, kind, true, new[] { Owner.Player });
            var legacy = Prepare(Initialization());
            var prepared = Prepare(Initialization(Environment(source), Bindings(true, 71)));
            AssertUnchanged(legacy.Characters[0], prepared.Characters[0]);
            Assert.That(prepared.Combat.Environment.Tiles.Single().Active, Is.True);
            Assert.That(prepared.Combat.EnvironmentBindings.TerrainSourceIds, Is.EqualTo(new long[] { 71 }));
        }

        [Test]
        public void Preparation_RejectsUnclassifiedStaticTerrain()
        {
            var source = new StaticTerrainFact(71, CellCoord.Zero, StaticTerrainKind.Sandstorm, true, new[] { Owner.Player });
            Assert.Throws<NotSupportedException>(() => Prepare(Initialization(Environment(source))));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FieldPreparation_AppliesPresenceWithoutHealingOrChangingPersistentFacts(bool classified)
        {
            var main = Main();
            var roster = Roster();
            var souls = roster.ToDictionary(r => r.Id, r => r.Id.Value == 1 ? 3 : 2);
            var tile = Tile();
            var environment = Environment(tile);
            var bindings = Bindings(classified, tile.Id);
            var prepared = PrepareFieldFacts(Initialization(environment, bindings), main, souls);
            var actor = prepared.Characters[0];

            Assert.That(actor.Stats.PositionAttackBonus, Is.EqualTo(2));
            Assert.That(actor.Stats.EffectiveAttackPower, Is.EqualTo(12));
            Assert.That(actor.Health.MaxHealth, Is.EqualTo(18));
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(actor.PositionModifier.SourceIds, Is.EqualTo(new[] { 11L }));
            Assert.That(actor.Stats.SoulAttackBonus, Is.EqualTo(3));
            Assert.That(actor.Stats.SoulMaxHealthBonus, Is.EqualTo(3));
            Assert.That(main.PersistentMaxHealth, Is.EqualTo(12));
            Assert.That(main.PersistentAttack, Is.EqualTo(7));
            Assert.That(main.CurrentHealth, Is.EqualTo(4));
            Assert.That(souls.Values, Is.EqualTo(new[] { 3, 2 }));
            Assert.That(prepared.Combat.EnvironmentBindings, Is.SameAs(bindings));
            Assert.That(prepared.Combat.Environment, Is.SameAs(environment));
            Assert.That(prepared.CommandState.Environment.SameValues(environment), Is.True);
            Assert.That(prepared.CommandState.Environment.Tiles[0], Is.SameAs(tile));
            Assert.That(prepared.CommandState.Environment.Tiles[0].SameValues(tile), Is.True);
            var cell = CellEnvironmentQuery.Read(prepared.CommandState, prepared.Characters,
                prepared.Combat.EnvironmentBindings, CellCoord.Zero);
            Assert.That(cell.TerrainSourceIds, Is.EqualTo(classified ? new[] { 11L } : Array.Empty<long>()));
            Assert.That(cell.ObjectSourceIds, Is.Empty);
        }

        [Test]
        public void Preparation_PreservesEmptyLegacyAndHandStates()
        {
            var legacy = Prepare(Initialization());
            var generic = CellEnvironmentTestData.Generic(12, CellCoord.Zero, isMine: true);
            var inactive = Tile(active: false);
            var environment = Environment(inactive, generic);
            var prepared = Prepare(Initialization(environment, Bindings(true, inactive.Id)));
            Assert.That(prepared.Characters.Count, Is.EqualTo(2));
            for (int i = 0; i < prepared.Characters.Count; i++)
                AssertUnchanged(legacy.Characters[i], prepared.Characters[i]);
            Assert.That(prepared.Characters[1].Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(prepared.Combat.DrawCursor, Is.EqualTo(1));
            Assert.That(prepared.CommandState.Allocation.NextEntityId, Is.EqualTo(3));
            Assert.That(legacy.Combat.EnvironmentBindings, Is.SameAs(EnvironmentSemanticBindings.Empty));
            Assert.That(prepared.CommandState.Environment.Tiles, Is.EqualTo(new[] { inactive, generic }));
            Assert.That(prepared.CommandState.Environment.SameValues(environment), Is.True);
        }

        [TestCase(false, 0, 0)]
        [TestCase(true, 0, 0)]
        [TestCase(false, 5, 3)]
        [TestCase(true, 5, 3)]
        public void Preparation_RejectsActiveCompositionOnOccupiedAndUnoccupiedCells(bool classified, int x, int y)
        {
            var cell = new CellCoord(x, y);
            var environment = Environment(Tile(11, cell), Tile(12, cell));
            var error = Assert.Throws<NotSupportedException>(() => Prepare(
                Initialization(environment, Bindings(classified, 11, 12))));
            Assert.That(error.Message, Does.Contain("중첩 목적지 보정"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Preparation_AllowsOneActiveAndOneInactiveSourceOnUnoccupiedCell(bool classified)
        {
            var environment = Environment(Tile(11, new CellCoord(5, 3)), Tile(12, new CellCoord(5, 3), false));
            var prepared = Prepare(Initialization(environment, Bindings(classified, 11, 12)));
            Assert.That(prepared.Characters[0].Health.MaxHealth, Is.EqualTo(15));
            Assert.That(prepared.CommandState.Environment.SameValues(environment), Is.True);
        }

        [TestCase(-1, 0, true)]
        [TestCase(8, 0, false)]
        [TestCase(0, 6, true)]
        public void Preparation_RejectsSourceOutsideBoardIncludingInactive(int x, int y, bool active)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Prepare(
                Initialization(Environment(Tile(cell: new CellCoord(x, y), active: active)))));
        }

        [Test]
        public void Preparation_RejectsDanglingBinding()
        {
            Assert.Throws<ArgumentException>(() => Prepare(Initialization(
                MovementEnvironmentFacts.Empty, Bindings(true, 999))));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Preparation_RejectsUnsupportedBoundTerrainKindIncludingInactive(bool active)
        {
            Assert.Throws<NotSupportedException>(() => Prepare(Initialization(
                Environment(CellEnvironmentTestData.Generic(11, CellCoord.Zero, active)), Bindings(true, 11))));
        }

        [Test]
        public void Preparation_RejectsObjectExecutableContentWithoutChoosingMultiplicityPolicy()
        {
            var bindings = new EnvironmentSemanticBindings(Array.Empty<long>(), new[] { 11L }, Array.Empty<EntityId>());
            Assert.Throws<NotSupportedException>(() => Prepare(Initialization(Environment(Tile()), bindings)));
        }

        [Test]
        public void Preparation_RejectsObstacleExecutableContentAfterReferenceValidation()
        {
            var bindings = new EnvironmentSemanticBindings(Array.Empty<long>(), Array.Empty<long>(), new[] { new EntityId(2) });
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var stage = Stage(new[] { new InitialEnemyStructurePlacementSpec(definition.Key, new CellCoord(4, 4), Rotation4.R0) });
            var initialization = Initialization(bindings: bindings, stage: stage, structures: new[] { definition });
            Assert.Throws<NotSupportedException>(() => Prepare(initialization));
        }

        [Test]
        public void Preparation_AllowsTerrainWithUnclassifiedHeadquarters()
        {
            var cell = new CellCoord(4, 4);
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var stage = Stage(new[] { new InitialEnemyStructurePlacementSpec(definition.Key, cell, Rotation4.R0) });
            var prepared = Prepare(Initialization(Environment(Tile(cell: cell)), Bindings(true, 11), stage, new[] { definition }));
            var snapshot = CellEnvironmentQuery.Read(prepared.CommandState, prepared.Characters, prepared.Combat.EnvironmentBindings, cell);
            Assert.That(snapshot.BlockingStructureIds, Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(snapshot.ObstacleIds, Is.Empty);
            Assert.That(CellEnvironmentQuery.CheckPlacement(snapshot), Is.EqualTo(EnvironmentPlacementViolation.None));
            Assert.That(prepared.Structures[0].Health.MaxHealth, Is.EqualTo(20));
            // 구조물 시작 체력이 정의 최대 체력이라는 규칙은 Core 전투 준비가 소유한다(Buffer는 범위만 확인).
            Assert.That(prepared.Structures[0].Health.CurrentHealth, Is.EqualTo(20));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FieldPreparation_RejectsMainHealthChangingClamp(bool classified)
        {
            var main = Main(12);
            Assert.Throws<NotSupportedException>(() => PrepareFieldFacts(Initialization(
                Environment(Tile(attack: 0, maxHealth: -3)), Bindings(classified, 11)), main, soulCount: 0));
            Assert.That(main.CurrentHealth, Is.EqualTo(12));
            Assert.That(main.PersistentMaxHealth, Is.EqualTo(12));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FieldPreparation_AllowsRepresentableMaxReductionWithoutClamp(bool classified)
        {
            var actor = PrepareFieldFacts(Initialization(Environment(Tile(attack: 0, maxHealth: -3)),
                Bindings(classified, 11)), soulCount: 0).Characters[0];
            Assert.That(actor.Health.MaxHealth, Is.EqualTo(9));
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(4));
        }

        [TestCase(int.MaxValue, 0)]
        [TestCase(0, int.MaxValue)]
        public void FieldPreparation_PropagatesCheckedContributionOverflow(int attack, int maxHealth)
        {
            Assert.Throws<OverflowException>(() => PrepareFieldFacts(Initialization(Environment(Tile(attack: attack, maxHealth: maxHealth)))));
        }

        [Test]
        public void FieldPreparation_RejectsResultingMaxHealthBelowOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PrepareFieldFacts(
                Initialization(Environment(Tile(attack: 0, maxHealth: -12))), soulCount: 0));
        }

        [Test]
        public void StationaryCharacter_RemovesOnlyPositionContribution()
        {
            var prepared = PrepareFieldFacts(Initialization(Environment(Tile()), Bindings(true, 11)));
            var before = CharacterSettlementCalculator.Copy(prepared.Characters[0], new HealthStateSnapshot(17, 18), prepared.Characters[0].Stats);
            var main = Main(17);
            var changes = EnvironmentPresenceReconciler.Calculate(new[] { before }, MovementEnvironmentFacts.Empty, new[] { CellCoord.Zero }, main);
            var after = changes.Characters.Single().Final;
            Assert.That(changes.Characters[0].Expected, Is.SameAs(before));
            Assert.That(after.Health.MaxHealth, Is.EqualTo(15));
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(15));
            Assert.That(after.Stats.PositionAttackBonus, Is.Zero);
            Assert.That(after.Stats.EffectiveAttackPower, Is.EqualTo(10));
            Assert.That(after.PositionModifier.SourceIds, Is.Empty);
            Assert.That(after.Stats.SoulAttackBonus, Is.EqualTo(3));
            Assert.That(after.Stats.SoulMaxHealthBonus, Is.EqualTo(3));
            Assert.That(after.TemporaryAttackBonus, Is.EqualTo(before.TemporaryAttackBonus));
            Assert.That(after.TemporaryMaxHealthBonus, Is.EqualTo(before.TemporaryMaxHealthBonus));
            Assert.That(changes.Souls, Is.Empty);
            Assert.That(changes.Created, Is.Empty);
            Assert.That(changes.Main.Expected, Is.SameAs(main));
            Assert.That(changes.Main.Final.CurrentHealth, Is.EqualTo(15));
            Assert.That(changes.Main.Final.PersistentMaxHealth, Is.EqualTo(12));
            Assert.That(changes.Main.Final.PersistentAttack, Is.EqualTo(7));
            Assert.That(before.Health.CurrentHealth, Is.EqualTo(17));
            Assert.That(before.Health.MaxHealth, Is.EqualTo(18));
            Assert.That(before.PositionModifier.AttackBonus, Is.EqualTo(2));
            Assert.That(main.CurrentHealth, Is.EqualTo(17));
        }

        [Test]
        public void Reconcile_IsIdempotentAndDeduplicatesAffectedCells()
        {
            var before = PrepareFieldFacts(Initialization()).Characters[0];
            var environment = Environment(Tile());
            var first = EnvironmentPresenceReconciler.Calculate(new[] { before }, environment,
                new[] { CellCoord.Zero, CellCoord.Zero }, Main());
            Assert.That(first.Characters.Count, Is.EqualTo(1));
            Assert.That(first.Main, Is.Null);
            var second = EnvironmentPresenceReconciler.Calculate(new[] { first.Characters[0].Final }, environment,
                new[] { CellCoord.Zero, CellCoord.Zero }, Main());
            AssertEmpty(second);
        }

        [Test]
        public void Reconcile_PreservesTemporaryArtifactIdentityAndMovementLedger()
        {
            var basic = CellEnvironmentTestData.Character();
            var s = new UnitStatsSnapshot(MoveMethod.LiftAndPlace,4,1,                6,2,7,Array.Empty<TraitStatusSnapshot>(),basic.Stats.Category,                artifactAttackBonus: 4,soulAttackBonus: 3,soulMaxHealthBonus: 3);
            var before = new CharacterCombatFacts(basic.EntityId, null, basic.CharacterKey, basic.Owner, null,
                basic.Area, new HealthStateSnapshot(7, 18), s, null, basic.Anchor, basic.Rotation,
                basic.OccupiedCells, UnitActionUse.Available, temporaryAttackBonus: 4, temporaryMaxHealthBonus: 5);
            var changes = EnvironmentPresenceReconciler.Calculate(new[] { before }, Environment(Tile(cell: basic.Anchor)),
                basic.OccupiedCells, Main());
            var after = changes.Characters.Single().Final;
            AssertIdentityAndLedger(before, after);
            Assert.That(after.TemporaryAttackBonus, Is.EqualTo(4));
            Assert.That(after.TemporaryMaxHealthBonus, Is.EqualTo(5));
            Assert.That(after.Stats.ArtifactAttackBonus, Is.EqualTo(4));
            Assert.That(after.Stats.SoulAttackBonus, Is.EqualTo(3));
            Assert.That(after.Stats.SoulMaxHealthBonus, Is.EqualTo(3));
            Assert.That(after.Stats.AttackPower, Is.EqualTo(6));
            Assert.That(after.Stats.EffectiveAttackPower, Is.EqualTo(12));
            Assert.That(after.Health.MaxHealth, Is.EqualTo(21));
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(after.Stats.PhysicalDefense, Is.EqualTo(2));
            Assert.That(after.Stats.Priority, Is.EqualTo(7));
            Assert.That(changes.Main, Is.Null);
            Assert.That(before.PositionModifier.SourceIds, Is.Empty);
        }

        [Test]
        public void Reconcile_EmptyUnaffectedDeadAndHandInputsReturnNoChanges()
        {
            var field = CellEnvironmentTestData.Character();
            var hand = CellEnvironmentTestData.Character(12, area: CharacterRuntimeArea.Hand);
            var dead = CellEnvironmentTestData.Character(13, hp: 0);
            var environment = Environment(Tile(cell: field.Anchor));
            AssertEmpty(EnvironmentPresenceReconciler.Calculate(Array.Empty<CharacterCombatFacts>(), environment, field.OccupiedCells, Main()));
            AssertEmpty(EnvironmentPresenceReconciler.Calculate(new[] { field }, environment, Array.Empty<CellCoord>(), Main()));
            AssertEmpty(EnvironmentPresenceReconciler.Calculate(new[] { field }, environment, new[] { CellCoord.Zero }, Main()));
            AssertEmpty(EnvironmentPresenceReconciler.Calculate(new[] { dead, hand }, environment, field.OccupiedCells, Main()));
        }

        [Test]
        public void Reconcile_RejectsNullOrDuplicateInputsEvenWithoutAffectedCells()
        {
            var empty = Array.Empty<CellCoord>();
            var actor = CellEnvironmentTestData.Character();
            Assert.Throws<ArgumentNullException>(() => EnvironmentPresenceReconciler.Calculate(null, MovementEnvironmentFacts.Empty, empty, Main()));
            Assert.Throws<ArgumentNullException>(() => EnvironmentPresenceReconciler.Calculate(new[] { actor }, null, empty, Main()));
            Assert.Throws<ArgumentNullException>(() => EnvironmentPresenceReconciler.Calculate(new[] { actor }, MovementEnvironmentFacts.Empty, null, Main()));
            Assert.Throws<ArgumentNullException>(() => EnvironmentPresenceReconciler.Calculate(new[] { actor }, MovementEnvironmentFacts.Empty, empty, null));
            Assert.Throws<ArgumentException>(() => EnvironmentPresenceReconciler.Calculate(new CharacterCombatFacts[] { null }, MovementEnvironmentFacts.Empty, empty, Main()));
            Assert.Throws<ArgumentException>(() => EnvironmentPresenceReconciler.Calculate(new[] { actor, actor }, MovementEnvironmentFacts.Empty, empty, Main()));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Reconcile_MultiCellContactWithPresenceIsUnsupportedOnAnyOccupiedCell(int index)
        {
            var cells = new[] { new CellCoord(1, 1), new CellCoord(2, 1) };
            var actor = CellEnvironmentTestData.Character(cells: cells);
            Assert.Throws<NotSupportedException>(() => EnvironmentPresenceReconciler.Calculate(new[] { actor },
                Environment(Tile(cell: cells[index])), new[] { cells[1] }, Main()));
        }

        [Test]
        public void Reconcile_LegacyMultiCellWithoutPresenceIsUnchanged()
        {
            var cells = new[] { new CellCoord(1, 1), new CellCoord(2, 1) };
            var actor = CellEnvironmentTestData.Character(cells: cells);
            AssertEmpty(EnvironmentPresenceReconciler.Calculate(new[] { actor }, MovementEnvironmentFacts.Empty, cells, Main()));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void Preparation_MultiCellEnemyPreservesLegacyButRejectsPresence(int sourceX)
        {
            var original = Initialization();
            var wide = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 2, 1,
                new[] { CellCoord.Zero, new CellCoord(1, 0) }, 10, MoveMethod.LiftAndPlace,
                1, CategoryKey.Main,
                5, 0, 1, Array.Empty<TraitSpec>(), UnitOwnerEligibility.EnemyOnly);
            var stage = new StageConfig(8, 6, 8, 2, 2, 42,
                new[] { new InitialEnemyPlacementSpec(wide.Key, new CellCoord(1, 4), Rotation4.R0) }, Array.Empty<CellCoord>(),
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(), Array.Empty<InitialEnemyStructurePlacementSpec>());
            var initialization = new BattleInitialization(stage, original.MainAnchor, original.MainRotation,
                original.Units.Concat(new[] { wide }).ToArray(), original.Structures,
                original.Categories, environment: sourceX < 0 ? MovementEnvironmentFacts.Empty : Environment(Tile(cell: new CellCoord(sourceX + 1, 4))));
            if (sourceX >= 0)
                Assert.Throws<NotSupportedException>(() => Prepare(initialization));
            else
            {
                var actor = Prepare(initialization).Characters.Single(c => c.Owner == Owner.Enemy);
                Assert.That(actor.OccupiedCells.Count, Is.EqualTo(2));
                Assert.That(actor.Health.MaxHealth, Is.EqualTo(10));
                Assert.That(actor.Stats.EffectiveAttackPower, Is.EqualTo(5));
                Assert.That(actor.PositionModifier.SourceIds, Is.Empty);
            }
        }

        private static void AssertEmpty(CharacterChanges changes)
        {
            Assert.That(changes.Characters, Is.Empty);
            Assert.That(changes.Souls, Is.Empty);
            Assert.That(changes.Created, Is.Empty);
            Assert.That(changes.Main, Is.Null);
        }

        private static void AssertUnchanged(CharacterCombatFacts expected, CharacterCombatFacts actual)
        {
            AssertIdentityAndLedger(expected, actual);
            Assert.That(actual.Health, Is.EqualTo(expected.Health));
            Assert.That(actual.Stats.MoveMethod, Is.EqualTo(expected.Stats.MoveMethod));
            Assert.That(actual.Stats.MoveRange, Is.EqualTo(expected.Stats.MoveRange));
            Assert.That(actual.Stats.AppointRange, Is.EqualTo(expected.Stats.AppointRange));
            Assert.That(actual.Stats.AttackPower, Is.EqualTo(expected.Stats.AttackPower));
            Assert.That(actual.Stats.EffectiveAttackPower, Is.EqualTo(expected.Stats.EffectiveAttackPower));
            Assert.That(actual.Stats.PhysicalDefense, Is.EqualTo(expected.Stats.PhysicalDefense));
            Assert.That(actual.Stats.Priority, Is.EqualTo(expected.Stats.Priority));
            Assert.That(actual.Stats.ArtifactAttackBonus, Is.EqualTo(expected.Stats.ArtifactAttackBonus));
            Assert.That(actual.Stats.SoulAttackBonus, Is.EqualTo(expected.Stats.SoulAttackBonus));
            Assert.That(actual.Stats.SoulMaxHealthBonus, Is.EqualTo(expected.Stats.SoulMaxHealthBonus));
            Assert.That(actual.Stats.PositionAttackBonus, Is.EqualTo(expected.Stats.PositionAttackBonus));
            Assert.That(actual.Stats.TraitStatuses.ToArray(), Is.EqualTo(expected.Stats.TraitStatuses.ToArray()));
            Assert.That(actual.Stats.Category.Key, Is.EqualTo(expected.Stats.Category.Key));
            Assert.That(actual.PositionModifier.SameValues(expected.PositionModifier), Is.True);
            Assert.That(actual.TemporaryAttackBonus, Is.EqualTo(expected.TemporaryAttackBonus));
            Assert.That(actual.TemporaryMaxHealthBonus, Is.EqualTo(expected.TemporaryMaxHealthBonus));
        }

        private static void AssertIdentityAndLedger(CharacterCombatFacts expected, CharacterCombatFacts actual)
        {
            Assert.That(actual.EntityId, Is.EqualTo(expected.EntityId));
            Assert.That(actual.OwnedId, Is.EqualTo(expected.OwnedId));
            Assert.That(actual.CharacterKey, Is.EqualTo(expected.CharacterKey));
            Assert.That(actual.Owner, Is.EqualTo(expected.Owner));
            Assert.That(actual.OwnershipRole, Is.EqualTo(expected.OwnershipRole));
            Assert.That(actual.Area, Is.EqualTo(expected.Area));
            Assert.That(actual.ReserveSlotIndex, Is.EqualTo(expected.ReserveSlotIndex));
            Assert.That(actual.Anchor, Is.EqualTo(expected.Anchor));
            Assert.That(actual.Rotation, Is.EqualTo(expected.Rotation));
            Assert.That(actual.OccupiedCells, Is.EqualTo(expected.OccupiedCells));
            Assert.That(actual.ActionUse, Is.EqualTo(expected.ActionUse));
        }

        private static DestinationTileModifierFact Tile(long id = 11, CellCoord? cell = null,
            bool active = true, int attack = 2, int maxHealth = 3) =>
            new(id, cell ?? CellCoord.Zero, active, Array.Empty<Owner>(), attack, maxHealth, 9);

        private static MovementEnvironmentFacts Environment(params MovementTileFact[] tiles) => new(tiles);
        private static EnvironmentSemanticBindings Bindings(bool classified, params long[] ids) =>
            classified ? new EnvironmentSemanticBindings(ids, Array.Empty<long>(), Array.Empty<EntityId>()) : EnvironmentSemanticBindings.Empty;
        private static MainCharacterPersistentState Main(int hp = 4) => new(hp, 12, 7);
        private static OwnedCharacterRecord[] Roster() => new[]
        {
            new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Gravekeeper, CharacterOwnershipRole.Main),
            new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
        };

        private static BattlePreparation Prepare(BattleInitialization initialization,
            MainCharacterPersistentState main = null, IReadOnlyDictionary<CharacterInstanceId, int> souls = null, int soulCount = 3) =>
            BattlePreparationCalculator.Prepare(initialization, Roster(), main ?? Main(),
                souls ?? Roster().ToDictionary(r => r.Id, r => r.Id.Value == 1 ? soulCount : 2),
                new InitialDrawPlan(new[] { new CharacterInstanceId(2) }), Array.Empty<CharacterCombatFacts>());

        // 위치 기여분 계산기의 필드 입력을 명시적으로 만든다. 운영 초기 손패를 자동 배치하는 경로가 아니다.
        private static BattlePreparation PrepareFieldFacts(BattleInitialization initialization,
            MainCharacterPersistentState main = null, IReadOnlyDictionary<CharacterInstanceId, int> souls = null, int soulCount = 3)
        {
            main ??= Main();
            var prepared = Prepare(initialization, main, souls, soulCount);
            var field = prepared.Characters.Select(character => character.OwnershipRole == CharacterOwnershipRole.Main
                ? CombatPlacementCalculator.Location(character, CharacterRuntimeArea.Field, null,
                    initialization.MainAnchor, initialization.MainRotation) : character).ToArray();
            var reconciled = BattleEnvironmentPreparation.Prepare(initialization, field, prepared.Structures, main);
            return new BattlePreparation(reconciled, prepared.Combat, prepared.Structures, commandState: prepared.CommandState);
        }

        private static StageConfig Stage(InitialEnemyStructurePlacementSpec[] placements = null) =>
            new(8, 6, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(), Array.Empty<CellCoord>(),
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(), placements ?? Array.Empty<InitialEnemyStructurePlacementSpec>());

        private static BattleInitialization Initialization(MovementEnvironmentFacts environment = null,
            EnvironmentSemanticBindings bindings = null, StageConfig stage = null, StructureSpec[] structures = null) =>
            new(stage ?? Stage(), CellCoord.Zero, Rotation4.R0,
                new[] { Unit(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter), Unit(EntityKey.Unit_Akimbo, UnitRole.Regular) },
                structures ?? Array.Empty<StructureSpec>(), UnitSpecTestFactory.CreateDefaultCategoryCatalog(),
                environment: environment, environmentBindings: bindings);

        private static UnitSpec Unit(EntityKey key, UnitRole role) =>
            new(key, role, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace,
                1, CategoryKey.Main,
                5, 0, 1, Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly);
    }
}
