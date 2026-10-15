using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class MineEnvironmentTests
    {
        internal static readonly CellCoord Center = new(2, 2);
        internal static readonly IMovementTileRule[] Rules = { new SheriffBadgeMovementTileRule(), new MineMovementTileRule() };
        internal static MineSourceFact Mine(long id = 11, long ordinal = 1, int damage = 10, bool hidden = false,
            CellCoord? cell = null) => new(id, cell ?? Center, ordinal, damage, true, new[] { hidden ? Owner.Enemy : Owner.Player });

        [Test]
        public void Mask_IncludesCenterAndEightNeighbors_ClipsBoardEdges()
        {
            var cells = MineMovementTileRule.Cells(Center, 5, 5);
            Assert.That(cells.Count, Is.EqualTo(9));
            Assert.That(cells.Distinct().Count(), Is.EqualTo(9));
            Assert.That(cells, Has.Member(Center));
            Assert.That(MineMovementTileRule.Cells(CellCoord.Zero, 5, 5), Is.EquivalentTo(new[] {
                CellCoord.Zero, CellCoord.Right, CellCoord.Up, new CellCoord(1, 1) }));
            Assert.That(MineMovementTileRule.Cells(CellCoord.Zero, 1, 1), Is.EqualTo(new[] { CellCoord.Zero }));
            Assert.Throws<ArgumentOutOfRangeException>(() => MineMovementTileRule.Cells(Center, 0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => MineMovementTileRule.Cells(new CellCoord(5, 0), 5, 5));
        }

        [Test]
        public void ImmutableSource_PreservesDamageMineIdentityOrderAndVisibility()
        {
            var mine = Mine(damage: 7);
            Assert.That(mine.IsMine, Is.True);
            Assert.That(((MineSourceFact)mine.WithActive(false)).Damage, Is.EqualTo(7));
            Assert.That(((MineSourceFact)mine.WithKnownTo(new[] { Owner.Enemy })).EntryOrdinal, Is.EqualTo(1));
            Assert.That(mine.SameValues(Mine(damage: 8)), Is.False);
            var placement = new MinePlacementSpec(99, Center, 7, new[] { Owner.Player });
            Assert.That(((MineSourceFact)placement.CreateSource(42)).Damage, Is.EqualTo(7));
            Assert.That(placement.CreateSource(42).EntryOrdinal, Is.EqualTo(42));
            Assert.Throws<ArgumentOutOfRangeException>(() => Mine(damage: -1));
        }

        [Test]
        public void Entry_HitsBothFactionsCenterDiagonalsAndStructuresExactlyOnce()
        {
            var i = Input(Mine());
            var ally = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Player);
            var enemy = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy);
            var outside = CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(4, 2), Owner.Enemy);
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(1, 3), 100, 5,
                new[] { CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0) });
            i = Copy(i, characters: i.Characters.Concat(new[] { ally, enemy, outside }).ToArray(),
                structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            var final = CombatCommandComposition.Project(r);
            foreach (var id in new[] { 1, 2, 3 })
                Assert.That(final.Characters.Single(c => c.EntityId == new EntityId(id)).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(final.Characters.Single(c => c.EntityId == new EntityId(4)).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(r.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(r.Events.OfType<CombatStructureEvent>().Count(), Is.EqualTo(1));
            Assert.That(r.FinalState.Environment.Tiles.Single().Active, Is.False);
            var origins = r.Events.OfType<CombatCharacterEvent>().SelectMany(e => e.DamageOrigins)
                .Concat(r.Events.OfType<CombatStructureEvent>().SelectMany(e => e.DamageOrigins)).ToArray();
            Assert.That(origins.Select(o => o.AttackGroup).Distinct().Count(), Is.EqualTo(1));
            Assert.That(origins.All(o => o.PhysicalCell == Center && o.CreditedCharacterId == null), Is.True);
        }

        [Test]
        public void Entry_UsesCurrentProjectionAndIgnoresDeadOrOffFieldVictims()
        {
            var i = Input(Mine());
            var enemy = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy);
            var dead = CombatCommandTestData.With(CombatCommandTestData.Character(4, EntityKey.Unit_Demon,
                new CellCoord(2, 3), Owner.Enemy), hp: 0);
            var waiting = CombatPlacementCalculator.Location(CombatCommandTestData.Character(5, EntityKey.Unit_Demon,
                new CellCoord(2, 3), Owner.Enemy), CharacterRuntimeArea.EnemyWaiting, null, null, null);
            i = Copy(i, characters: i.Characters.Concat(new[] { enemy, dead, waiting }).ToArray());
            var p = new MovementProjection(i);
            var moved = CombatMovementCalculator.MoveTo(enemy, new CellCoord(4, 2));
            p.MergeCharacter(new CharacterStateChange(enemy, moved));
            p.Enter(Center, 1);
            MovementEnvironmentCalculator.ResolveObjectsAt(p, i.ActorId, Center, Rules);
            Assert.That(p.Actor.Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(p.Character(enemy.EntityId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(p.EffectTargetIds, Has.No.Member(enemy.EntityId));
            Assert.That(p.EffectTargetIds, Has.No.Member(dead.EntityId));
            Assert.That(p.EffectTargetIds, Has.No.Member(waiting.EntityId));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BadgeMineOrder_SettlesDamageBeforeNextObjectAndPreservesLaterPickupOnDeath(bool mineFirst)
        {
            var badge = new SheriffBadgeSourceFact(99, Center, mineFirst ? 2 : 1, true, new[] { Owner.Player }, priority: 100);
            var i = Input(Mine(ordinal: mineFirst ? 1 : 2, damage: 100), badge);
            var enemy = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy);
            i = Copy(i, characters: i.Characters.Append(enemy).ToArray());
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(MovementTestData.Actor(r).Health.IsDead, Is.EqualTo(mineFirst));
            Assert.That(CombatCommandComposition.Project(r).Characters.Single(c => c.EntityId == enemy.EntityId).Health.IsDead, Is.True);
            Assert.That(r.FinalState.Environment.Tiles.Single(t => t.Id == badge.Id).Active, Is.EqualTo(mineFirst));
            Assert.That(r.Events.OfType<CombatObjectActivationEvent>().Select(e => e.Source.Id),
                Is.EqualTo(mineFirst ? new[] { 11L } : new[] { 99L, 11L }));
        }

        [Test]
        public void SeparateMines_HaveSeparateProtectionGroups()
        {
            var i = Input(new SheriffBadgeSourceFact(99, Center, 1, true, new[] { Owner.Player }),
                Mine(11, 2, 7), Mine(12, 3, 9));
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(MovementTestData.Actor(r).Health.CurrentHealth, Is.EqualTo(91));
            Assert.That(MovementTestData.Actor(r).HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(r.FinalState.Environment.Tiles.All(t => !t.Active), Is.True);
        }

        [TestCase(0)]
        [TestCase(10)]
        public void ConsumedMine_BlocksReturnEvenWhenBadgePreventsAllDamage(int damage)
        {
            var i = Input(new SheriffBadgeSourceFact(99, Center, 1, true, new[] { Owner.Player }), Mine(11, 2, damage));
            var preview = CombatMovementCalculator.Preview(i, Rules);
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(MovementTestData.Actor(r).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(MovementTestData.Actor(r).HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(preview.CanReturn, Is.False);
            Assert.That(r.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(CombatCommandCalculator.Calculate(MovementTestData.Return(CombatCommandComposition.Project(r)), Rules).Accepted, Is.False);
        }

        [Test]
        public void Mine_UsesTerrainDefenseAtEachVictimWithItsPhysicalOrigin()
        {
            var i = Input(Mine());
            var high = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy);
            var sand = CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy);
            i = Copy(i, characters: i.Characters.Concat(new[] { high, sand }).ToArray(), environment: new MovementEnvironmentFacts(i.State.Environment.Tiles.Concat(new MovementTileFact[] {
                new StaticTerrainFact(71, high.Anchor.Value, StaticTerrainKind.HighGround, true, new[] { Owner.Player }),
                new StaticTerrainFact(72, sand.Anchor.Value, StaticTerrainKind.Sandstorm, true, new[] { Owner.Player }) }).ToArray()), terrainIds: new[] { 71L, 72L });
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            var final = CombatCommandComposition.Project(r);
            Assert.That(final.Characters.Single(c => c.EntityId == high.EntityId).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(final.Characters.Single(c => c.EntityId == sand.EntityId).Health.CurrentHealth, Is.EqualTo(92));
            Assert.That(MovementTestData.Actor(r).Health.CurrentHealth, Is.EqualTo(90));
        }

        [Test]
        public void MineDestroysBarrel_ChainHasIndependentGroupAndDoesNotDetonateNeighborMine()
        {
            var i = Input(new SheriffBadgeSourceFact(99, Center, 1, true, new[] { Owner.Player }),
                Mine(11, 2, 5), Mine(12, 3, 99, cell: new CellCoord(2, 3)));
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(3, 2), 1, 3);
            var second = ObstacleReactionTests.Barrel(52, new CellCoord(4, 2), 1, 4);
            i = Copy(i, structures: new[] { barrel.Structure, second.Structure }, profiles: new[] { barrel.Profile, second.Profile });
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(MovementTestData.Actor(r).Health.CurrentHealth, Is.EqualTo(97));
            Assert.That(r.FinalState.Structures.All(s => s.Health.IsDead), Is.True);
            Assert.That(r.FinalState.Environment.Tiles.Single(t => t.Id == 12).Active, Is.True);
            var reactions = r.Events.OfType<CombatObstacleReactionEvent>().ToArray();
            Assert.That(reactions.Select(e => e.ObstacleId), Is.EquivalentTo(new[] { new EntityId(51), new EntityId(52) }));
            Assert.That(reactions.Select(e => e.Origin.AttackGroup).Distinct().Count(), Is.EqualTo(2));
            Assert.That(reactions.All(e => e.Origin.CreditedCharacterId == null), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreviewAndContext_ExposeVictimHealthOnlyForKnownMine(bool hidden)
        {
            var i = Input(Mine(hidden: hidden));
            var enemy = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy);
            i = Copy(i, characters: i.Characters.Append(enemy).ToArray());
            var preview = CombatMovementCalculator.Preview(i, Rules);
            Assert.That(preview.CanMove, Is.True);
            Assert.That(preview.ProjectedActor.Health.CurrentHealth, Is.EqualTo(hidden ? 100 : 90));
            Assert.That(preview.PublicEffects.OfType<CombatObjectActivationEvent>().Any(), Is.EqualTo(!hidden));
            Assert.That(preview.PublicEffects.OfType<CombatCharacterEvent>().Any(e => e.Final.EntityId == enemy.EntityId), Is.EqualTo(!hidden));
            var context = MovementContextCalculator.Capture(i, Owner.Player, Rules);
            var changed = Copy(i, characters: i.Characters.Select(c => c.EntityId == enemy.EntityId ? CombatCommandTestData.With(c, hp: 80) : c).ToArray());
            Assert.That(MovementContextCalculator.Matches(context, changed, Owner.Player, Rules), Is.EqualTo(hidden));
            var changedDamage = Copy(i, environment: new MovementEnvironmentFacts(new[] { Mine(damage: 99, hidden: hidden) }));
            Assert.That(MovementContextCalculator.Matches(context, changedDamage, Owner.Player, Rules), Is.EqualTo(hidden));
            Assert.That(CombatCommandCalculator.Calculate(i, Rules).Accepted, Is.True);
            Assert.That(MovementTestData.Actor(CombatCommandCalculator.Calculate(i, Rules)).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(i.State.Environment.Tiles.Single().Active, Is.True);
        }

        [Test]
        public void DirectAttackAndBarrelBlast_CannotDamageOrTriggerMine()
        {
            var seed = ObstacleReactionTests.Barrels(ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 1, 7));
            var mine = Mine(cell: new CellCoord(4, 4));
            var i = Copy(seed, environment: new MovementEnvironmentFacts(new[] { mine }));
            var blast = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(blast.Accepted, Is.True, blast.Reason);
            Assert.That(blast.FinalState.Environment.Tiles.Single().SameValues(mine), Is.True);
            Assert.That(blast.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            var aim = CombatAttackQueries.WithAim(i, AttackAim.ForCell(Center));
            i = Copy(aim, environment: new MovementEnvironmentFacts(new[] { Mine() }));
            var emptyAttack = CombatCommandCalculator.Calculate(i, Rules);
            // 지뢰만 있는 칸은 일반 공격의 유효 주 대상이 아니므로 정산 전에 거절한다.
            Assert.That(emptyAttack.Accepted, Is.False);
            Assert.That(emptyAttack.Reason, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget.ToString()));
            Assert.That(emptyAttack.Attack, Is.Null);
            Assert.That(emptyAttack.Characters.Characters, Is.Empty);
            Assert.That(emptyAttack.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            Assert.That(emptyAttack.FinalState.Environment.Tiles.Single().Active, Is.True);
            Assert.That(emptyAttack.FinalState.Environment.Tiles.Single().SameValues(i.State.Environment.Tiles.Single()), Is.True);

            // 같은 칸의 적대 유닛을 합법적으로 공격해도 지뢰는 피해나 활성화의 대상이 되지 않는다.
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, Center, Owner.Enemy);
            var direct = CombatCommandCalculator.Calculate(Copy(i, characters: i.Characters.Append(target).ToArray()), Rules);
            Assert.That(direct.Accepted, Is.True, direct.Reason);
            Assert.That(direct.Attack.Value.Hits.ToArray().Single().TargetId, Is.EqualTo(target.EntityId));
            Assert.That(direct.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(20));
            Assert.That(direct.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            Assert.That(direct.FinalState.Environment.Tiles.Single().SameValues(i.State.Environment.Tiles.Single()), Is.True);
            Assert.That(i.State.Environment.Tiles.Single().Active, Is.True);
        }

        [TestCase(0)]
        [TestCase(5)]
        public void MineBarrelChain_BadgeBlockedVictimStillInvalidatesStaleHeldPrediction(int explosionDamage)
        {
            var i = Input(Mine(damage: 1));
            var enemy = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 2), Owner.Enemy);
            var protectedEnemy = HeldEnvironmentEffects.AcquireBadge(enemy, 100);
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(3, 2), 1, explosionDamage);
            i = Copy(i, characters: i.Characters.Append(protectedEnemy).ToArray(),
                structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var preview = CombatMovementCalculator.Preview(i, Rules);
            Assert.That(preview.CanMove, Is.True);
            Assert.That(preview.PublicEffects.OfType<CombatHeldEnvironmentEvent>().Any(e =>
                e.CharacterId == enemy.EntityId && !e.Final.BadgeAvailable), Is.True);
            var expected = MovementContextCalculator.Capture(i, Owner.Player, Rules);
            var changed = Copy(i, characters: i.Characters.Select(c => c.EntityId == enemy.EntityId
                ? HeldEnvironmentEffects.AcquireBadge(enemy, 200) : c).ToArray());
            Assert.That(MovementContextCalculator.Matches(expected, changed, Owner.Player, Rules), Is.False,
                "폭발 피해가 0이어도 뱃지 소비를 예측한 수신자의 보유 기대값을 검증한다.");
            Assert.That(expected.EffectTargets.Select(c => c.EntityId), Has.Member(enemy.EntityId));
            Assert.That(protectedEnemy.HeldEnvironment.BadgeAvailable, Is.True);
        }

        [Test]
        public void OriginAndCheckpointReturn_DoNotActivateMine()
        {
            var i = Input(Mine(cell: new CellCoord(1, 2)));
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(MovementTestData.Actor(r).Health.CurrentHealth, Is.EqualTo(100));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(CombatCommandComposition.Project(r)), Rules);
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(returned.FinalState.Environment.Tiles.Single().Active, Is.True);
            Assert.That(returned.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
        }

        [Test]
        public void PlayerPlacement_ActivatesMineWithoutMovementCharge()
        {
            var i = CombatCommonCommandTests.Command(CombatCommandKind.PlaceUnit);
            var hand = CombatPlacementCalculator.Location(i.Characters[0], CharacterRuntimeArea.Hand, 0, null, null);
            var cell = new CellCoord(2, 0);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                i.State.Structures, i.State.ThornFields, i.State.Timeline, new CombatTurnFacts(StagePhase.Deployment),
                new MovementEnvironmentFacts(new[] { Mine(cell: cell) }));
            var input = new CombatCommandInput(1, i.Kind, i.ActorId, default, state, new[] { hand, i.Characters[1] },
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, targetCell: cell,
                battle: new CombatBattleFacts(10, 20, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2),
                environmentBindings: ObjectArrivalOrderTests.Bindings(11));
            var r = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            var final = r.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(hand.Health.CurrentHealth - 10));
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(hand.Stats.CurrentActionCost));
            Assert.That(r.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(r.PathMovement, Is.Null);
        }

        [Test]
        public void EnemyNightRedeployment_ActivatesMineBeforeParticipantsAreBuilt()
        {
            var i = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat);
            var cell = new CellCoord(2, 7);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, cell, Owner.Enemy);
            var waiting = CombatPlacementCalculator.Location(enemy, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var headquarters = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), new CellCoord(0, 7), Rotation4.R0, new[] { new CellCoord(0, 7) }, Owner.Enemy);
            var turn = new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, cell, Rotation4.R0) });
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                new[] { headquarters }, Array.Empty<ThornFieldCellSnapshot>(), Array.Empty<CombatTimelineEntrySnapshot>(), turn,
                new MovementEnvironmentFacts(new[] { Mine(cell: cell) }));
            var input = new CombatCommandInput(1, i.Kind, EntityId.Empty, default, state,
                new[] { i.Characters.Single(c => c.CharacterKey == EntityKey.Unit_MainCharacter), waiting },
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                battle: new CombatBattleFacts(10, 20, 37, 1, 1, Array.Empty<CharacterInstanceId>(), 0, 21, 10, 2),
                environmentBindings: ObjectArrivalOrderTests.Bindings(11));
            var r = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.Characters.Characters.Single(c => c.Final.EntityId == enemy.EntityId).Final.Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(r.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(r.FinalState.Turn.Participants.Select(p => p.EntityId), Has.Member(enemy.EntityId));
        }

        [Test]
        public void AttackKnockback_MineDamagesCurrentDestinationOccupantsAfterDirectHit()
        {
            var i = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            i = CombatCommandTestData.With(i, characters: i.Characters.Where(c =>
                c.EntityId != new EntityId(3) && c.EntityId != new EntityId(4)).ToArray());
            var cell = new CellCoord(5, 3);
            i = Copy(i, environment: new MovementEnvironmentFacts(new[] { Mine(cell: cell) }));
            var r = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(r.Accepted, Is.True, r.Reason);
            var victim = r.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final;
            Assert.That(victim.Anchor, Is.EqualTo(cell));
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(70));
            Assert.That(r.Attack.Value.Hits.ToArray().Single(h => h.TargetId == victim.EntityId).DamageDealt, Is.EqualTo(20));
            Assert.That(r.FinalState.Environment.Tiles.Single().Active, Is.False);
        }

        internal static CombatCommandInput Input(params ObjectSourceFact[] sources)
        {
            var seed = MovementTestData.SafeRoute(5, 2, 1, new CellCoord(1, 2), Center);
            return Copy(seed, environment: new MovementEnvironmentFacts(sources));
        }

        internal static CombatCommandInput Copy(CombatCommandInput i, CharacterCombatFacts[] characters = null,
            StructureCombatFacts[] structures = null, ObstacleContentProfile[] profiles = null,
            MovementEnvironmentFacts environment = null, long[] terrainIds = null)
        {
            characters ??= i.Characters.ToArray();
            structures ??= i.State.Structures.ToArray();
            profiles ??= i.ObstacleContents.ToArray();
            environment ??= i.State.Environment;
            var s = i.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                structures, s.ThornFields, s.Timeline, s.Turn, environment);
            var owned = characters.Where(c => c.OwnedId.HasValue).Select(c =>
                new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            return new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, state, characters,
                i.Profiles, i.Main, owned, owned.ToDictionary(o => o.Id, o => i.SoulCounts.GetValueOrDefault(o.Id)),
                i.Souls, i.Artifacts, i.TargetCell, i.Rotation, i.Battle, i.ProcessingKind, i.Path,
                i.MovementContext, 1, i.ArtifactItems,
                new EnvironmentSemanticBindings(terrainIds ?? i.EnvironmentBindings.TerrainSourceIds,
                    environment.Tiles.OfType<ObjectSourceFact>().Select(o => o.Id).ToArray(), profiles.Select(p => p.EntityId).ToArray()), profiles);
        }
    }
}
