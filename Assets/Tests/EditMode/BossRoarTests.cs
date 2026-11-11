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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 원래 행동 정산 뒤 포효 반응을 확인한다(계획 T5.2, D4·D8·D11, G6·G7·G13).
    /// fixture: 8x8, 보스(2) (3,3) 공격력 5·지정 사거리 2·Fan/All·Knockback 3·Roar(임계값), 플레이어(1) (4,3)이 보스를 단일 공격(20).
    /// G7: 포효는 보스의 현재 공격력과 일반 피해 계산을 쓰고 비용이 없다. 방어 0·상성 1.0이므로 피해는 5다.
    /// </summary>
    public sealed class BossRoarTests
    {
        internal static readonly EntityId Boss = new EntityId(2);
        internal static readonly EntityId Player = new EntityId(1);
        internal static readonly EntityId Sub = new EntityId(5);
        internal static readonly EntityId Far = new EntityId(6);
        internal static readonly EntityId Ally = new EntityId(7);
        internal static readonly EntityId MainId = new EntityId(10);

        [TestCase(false)]
        [TestCase(true)]
        public void Roar_HeavyMovesUnlessExplicitlyImmune_OtherTargetStillMoves(bool immune)
        {
            var input = WithHeavyTargets(Input(4), sharedDefinition: false, immune: immune);

            var result = Calculate(input);

            Assert.That(Final(result, Sub).Anchor, Is.EqualTo(new CellCoord(immune ? 1 : 0, 3)));
            Assert.That(Final(result, Sub).Health.CurrentHealth, Is.EqualTo(95));
            Assert.That(Final(result, Player).Anchor, Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(Final(result, Player).Health.CurrentHealth, Is.EqualTo(95));
            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(Final(result, Boss).ActionUse, Is.EqualTo(Original(input, Boss).ActionUse));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(
                e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == Sub), Is.EqualTo(!immune));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Roar_TwoHeavyInstancesFollowExplicitImmunity(bool immune)
        {
            var input = WithHeavyTargets(Input(4), sharedDefinition: true, immune: immune);

            var result = Calculate(input);

            Assert.That(Final(result, Player).Anchor, Is.EqualTo(new CellCoord(immune ? 4 : 7, 3)));
            Assert.That(Final(result, Sub).Anchor, Is.EqualTo(new CellCoord(immune ? 1 : 0, 3)));
            Assert.That(Final(result, Player).Health.CurrentHealth, Is.EqualTo(95));
            Assert.That(Final(result, Sub).Health.CurrentHealth, Is.EqualTo(95));
            Assert.That(Changed(result, Far), Is.False);
            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved &&
                (e.Final.EntityId == Player || e.Final.EntityId == Sub)), Is.EqualTo(!immune));
        }

        [Test]
        public void FifthNightHit_RoarsAcrossEntireFan()
        {
            var input = Input(4);
            var baseline = Calculate(Input(0));

            var result = Calculate(input);

            Assert.That(result.Events.OfType<CombatRoarEvent>().Single().BossId, Is.EqualTo(Boss));
            // 범위 안 적대 유닛만 피해 5와 넉백 3(보드 끝에서 정지)을 받는다.
            Assert.That(Final(result, Player).Anchor, Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(Final(result, Player).Health.CurrentHealth, Is.EqualTo(95));
            Assert.That(Final(result, Sub).Anchor, Is.EqualTo(new CellCoord(0, 3)));
            Assert.That(Final(result, Sub).Health.CurrentHealth, Is.EqualTo(95));
            // 범위 밖 적대 유닛·아군(적 진영) 유닛은 바뀌지 않는다.
            Assert.That(Changed(result, Far), Is.False);
            Assert.That(Changed(result, Ally), Is.False);
            Assert.That(Changed(result, MainId), Is.False);
            // 카운터는 0으로, 보스 행동 여부·공유 예산·진영 턴 진행는 포효 없는 기준과 같다.
            var boss = Final(result, Boss);
            Assert.That(RoarHitCounter.HitCount(boss), Is.EqualTo(0));
            Assert.That(boss.ActionUse, Is.EqualTo(Original(input, Boss).ActionUse));
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Player));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
            Assert.That(result.FinalState.Turn.SameValues(baseline.FinalState.Turn), Is.True);
        }

        [Test]
        public void PlayerAttackResult_PreservedAfterRoar()
        {
            var result = Calculate(Input(4));

            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Attack.Value.AttackerId, Is.EqualTo(Player));
            var roar = result.Events.OfType<CombatRoarEvent>().Single();
            Assert.That(roar.Attack.HasValue, Is.True);
            Assert.That(roar.Attack.Value.AttackerId, Is.EqualTo(Boss));
            // 플레이어 공격 사건 하나 뒤에 포효 사건이 온다. 포효는 공격 사건(Attacked)을 만들지 않는다.
            var kinds = result.Events.Select(e => e.Kind).ToList();
            Assert.That(kinds.Count(k => k == CombatEventKind.Attacked), Is.EqualTo(1));
            Assert.That(kinds.IndexOf(CombatEventKind.Attacked), Is.LessThan(kinds.IndexOf(CombatEventKind.Roared)));
        }

        [Test]
        public void RoarKillsActingUnit_KeepsPlayerControllerUntilManualEnd()
        {
            var result = Calculate(Input(4, playerHp: 3));

            Assert.That(Final(result, Player).Health.IsDead, Is.True);
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Player));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
            Assert.That(result.Attack.Value.AttackerId, Is.EqualTo(Player));
        }

        [Test]
        public void TenIndependentHits_RoarOnceAndResetToZero()
        {
            // 독립 피격이 쌓여 임계값의 두 배를 넘어도 한 번만 포효하고 초과분은 버린다(G6-2·3).
            var result = Calculate(Input(9));

            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(RoarHitCounter.HitCount(Final(result, Boss)), Is.EqualTo(0));
        }

        [Test]
        public void RoarSelfDerivedHit_CountsOnce()
        {
            // 포효가 보스 옆 화약통을 터뜨려 보스가 폭발에 맞으면 일반 규칙대로 1회 센다(G6-5). 초기화가 피해 전이므로 남는다.
            var result = Calculate(Input(4, barrel: true));

            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(RoarHitCounter.HitCount(Final(result, Boss)), Is.EqualTo(1));
            Assert.That(Final(result, Boss).Health.CurrentHealth, Is.EqualTo(100 - 20 - 7));
        }

        [Test]
        public void ThresholdOne_SelfHit_DoesNotLoopWithinBoundary()
        {
            // 임계값 1에서 자기 폭발 피격으로 다시 대기가 되어도 한 반응 경계에서는 한 번만 포효한다(G6-3 안전 규칙).
            var result = Calculate(Input(0, threshold: 1, barrel: true));

            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(RoarHitCounter.HitCount(Final(result, Boss)), Is.EqualTo(1));
            Assert.That(BossRoarCalculator.PendingBosses(CombatCommandComposition.Project(result)), Is.EqualTo(new[] { Boss }));
        }

        [Test]
        public void FifthHitKillsBoss_CancelsRoar()
        {
            var result = Calculate(Input(4, bossHp: 10));

            Assert.That(Final(result, Boss).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatRoarEvent>(), Is.Empty);
            Assert.That(Changed(result, Sub), Is.False);
        }

        [Test]
        public void Preview_DoesNotIncludeRoarOrPublish()
        {
            var input = Input(4);

            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projection);

            Assert.That(preview.CanAttack, Is.True);
            Assert.That(projection.Events.OfType<CombatRoarEvent>(), Is.Empty);
            Assert.That(projection.Characters.Characters.Any(c => c.Final.EntityId == Sub), Is.False);
            // 미리보기 투영에서는 카운터가 5가 되지만 반응 경계를 건너뛰므로 포효하지 않는다(G13).
            Assert.That(RoarHitCounter.HitCount(Final(projection, Boss)), Is.EqualTo(5));
        }

        [Test]
        public void NoTargetInRange_StillRoarsAndResets()
        {
            var input = Input(5, withTargets: false);
            var advance = WithKind(input, CombatCommandKind.CompleteEnemyTurn);

            var result = BossRoarCalculator.TryRoar(advance, Boss, Array.Empty<IMovementTileRule>());

            var roar = result.Events.OfType<CombatRoarEvent>().Single();
            Assert.That(roar.Attack.HasValue, Is.False);
            Assert.That(roar.AreaCells, Is.Not.Empty, "대상이 없어도 포효 범위 칸은 표시용으로 게시한다.");
            Assert.That(RoarHitCounter.HitCount(Final(result, Boss)), Is.EqualTo(0));
        }

        [Test]
        public void RoarAreaCells_MatchAttackAffectedCells_AndDoNotDependOnTargets()
        {
            // 포효 범위 칸은 직접 공격의 영향 칸과 같은 식 하나로 만든다(View 요청, 2026-10-15). 대상 유무는 칸을 바꾸지 않는다.
            var hit = BossRoarCalculator.TryRoar(WithKind(Input(5), CombatCommandKind.CompleteEnemyTurn), Boss,
                Array.Empty<IMovementTileRule>()).Events.OfType<CombatRoarEvent>().Single();
            var miss = BossRoarCalculator.TryRoar(WithKind(Input(5, withTargets: false), CombatCommandKind.CompleteEnemyTurn), Boss,
                Array.Empty<IMovementTileRule>()).Events.OfType<CombatRoarEvent>().Single();

            Assert.That(hit.Attack.HasValue, Is.True);
            Assert.That(hit.AreaCells, Is.EqualTo(hit.Attack.Value.AffectedCells.ToArray()));
            Assert.That(miss.Attack.HasValue, Is.False);
            Assert.That(miss.AreaCells, Is.EqualTo(hit.AreaCells));
        }

        [Test]
        public void EmptyAllSelection_KeepsFailureReason_AndOnlyAddsDisplayCells()
        {
            // 가림 계산을 빈 대상 검사 앞으로 옮겨도 실패 사유·차단 증거는 그대로다.
            var input = WithKind(Input(5, withTargets: false), CombatCommandKind.CompleteEnemyTurn);
            var boss = input.Characters.Single(c => c.EntityId == Boss);
            var profile = input.Profiles.Single(p => p.Definition.Key == boss.CharacterKey);
            var evaluation = CombatAttackQueries.Selection(input, boss, profile, AttackAim.ForAll());

            Assert.That(evaluation.FailureReason, Is.EqualTo(AttackFailureReason.UnresolvedEmptyAll));
            Assert.That(evaluation.BlockedContacts, Is.Empty);
            Assert.That(evaluation.EmptyAreaCells, Is.Not.Empty);
            Assert.That(evaluation.Selection, Is.Null);
        }

        /// <summary>혼합 대상은 서브만 다른 키로, 동일 정의 검증은 아킴보 전체를 Heavy로 조립한다.</summary>
        private static CombatCommandInput WithHeavyTargets(CombatCommandInput input, bool sharedDefinition, bool immune = false)
        {
            var key = sharedDefinition ? EntityKey.Unit_Akimbo : EntityKey.Unit_ShotGun;
            var characters = input.Characters.Select(c =>
            {
                if (!sharedDefinition && c.EntityId == Sub)
                    c = CombatCommandTestData.Character(Sub.Value, key, c.Anchor.Value, c.Owner);
                return c.CharacterKey == key
                    ? CombatCommandTestData.With(c, category: CategorySnapshot.CreateStateless(CategoryKey.Heavy)) : c;
            }).ToArray();
            var method = input.Profiles.Single(p => p.Definition.Key == EntityKey.Unit_Akimbo).Method;
            var s = CombatCommandTestData.Spec(key, sharedDefinition ? 20 : 1, CategoryKey.Heavy);
            var heavyProfile = new CombatAttackProfile(new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight,
                s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower,
                s.PhysicalDefense, s.Priority, s.Traits.ToArray(), s.OwnerEligibility, s.Occlusion,
                new UnitBehaviorSpec(true, true, immune), s.KillReward), method);
            var profiles = input.Profiles.Where(p => p.Definition.Key != key).Append(heavyProfile).ToArray();
            var owned = characters.Where(c => c.OwnedId.HasValue).Select(c =>
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.OwnedCharacterRecord(
                    c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State, characters,
                profiles, input.Main, owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell, input.Rotation,
                input.Battle, input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems,
                input.EnvironmentBindings, input.ObstacleContents,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence);
        }

        private static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        internal static CharacterCombatFacts Final(CombatCommandTransition result, EntityId id) =>
            result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == id)?.Final ?? Original(result.Input, id);

        private static CharacterCombatFacts Original(CombatCommandInput input, EntityId id) => input.Characters.Single(c => c.EntityId == id);

        private static bool Changed(CombatCommandTransition result, EntityId id) =>
            result.Characters.Characters.Any(c => c.Final.EntityId == id);

        [TestCase(3, 1, 2, false)]
        [TestCase(3, 2, 0, true)]
        [TestCase(7, 5, 6, false)]
        [TestCase(7, 6, 0, true)]
        public void DamageAndRoar_PreserveAuthoredThreshold(int threshold, int before, int after, bool roared)
        {
            var result = Calculate(Input(before, threshold: threshold));
            var status = Final(result, Boss).Stats.TraitStatuses.ToArray().OfType<RoarTraitStatusSnapshot>().Single();

            Assert.That(status.HitCount, Is.EqualTo(after));
            Assert.That(status.HitThreshold, Is.EqualTo(threshold));
            Assert.That(result.Events.OfType<CombatRoarEvent>().Any(), Is.EqualTo(roared));
        }

        internal static RoarTraitStatusSnapshot Status(int hitCount, int threshold = 5) => new RoarTraitStatusSnapshot(hitCount, threshold);

        /// <summary>보스(Roar·Knockback 3, Fan/All)를 플레이어가 공격하는 밤 입력이다.</summary>
        internal static CombatCommandInput Input(int hitCount, int threshold = 5, int playerHp = 100,
            int bossHp = 100, bool barrel = false, bool withTargets = true)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var player = CombatCommandTestData.With(seed.Characters[0], position: new CellCoord(4, 3), hp: playerHp);
            var main = seed.Characters[1];
            var boss = CombatCommandTestData.With(CombatCommandTestData.Character(Boss.Value, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy, 5),
                hp: bossHp, appointRange: 2, traits: new TraitStatusSnapshot[] { Status(hitCount, threshold), new KnockbackTraitStatusSnapshot(3) });
            var characters = new List<CharacterCombatFacts> { boss, main };
            if (withTargets)
            {
                characters.Add(player);
                characters.Add(CombatCommandTestData.Character(Sub.Value, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player));
                characters.Add(CombatCommandTestData.Character(Far.Value, EntityKey.Unit_Akimbo, new CellCoord(3, 7), Owner.Player));
                characters.Add(CombatCommandTestData.Character(Ally.Value, EntityKey.Unit_Gravekeeper, new CellCoord(3, 4), Owner.Enemy));
            }
            var single = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var profiles = new[]
            {
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20), single),
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Demon, 5, CategoryKey.Main,
                    new RoarTraitSpec(threshold), new KnockbackTraitSpec(3)),
                    new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.All, TargetingSplash.None, 0)),
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_MainCharacter, 1), single),
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 1), single),
            };
            var structures = new List<StructureCombatFacts>();
            var obstacles = new List<ObstacleContentProfile>();
            if (barrel)
            {
                var b = ObstacleReactionTests.Barrel(40, new CellCoord(3, 2), 1, 7);
                structures.Add(b.Structure);
                obstacles.Add(b.Profile);
            }
            var turn = new CombatTurnFacts(StagePhase.Combat,
                controllerPhase: withTargets ? CombatControllerPhase.Player : CombatControllerPhase.Enemy,
                controllerTurnSequence: 1, completedPlayerTurnCount: 3);
            var state = new CombatCommandState(8,8,withTargets ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,structures,Array.Empty<ThornFieldCellSnapshot>(),                turn);
            var owned = characters.Where(c => c.OwnedId.HasValue).Select(c => new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator
                .Contracts.Roster.OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            return new CombatCommandInput(1, CombatCommandKind.Attack, withTargets ? Player : Boss, AttackAim.ForCell(boss.Anchor.Value), state,
                characters.ToArray(), profiles, seed.Main, owned, owned.ToDictionary(o => o.Id, _ => 0), seed.Souls, seed.Artifacts,
                battleInstanceId: 1,
                environmentBindings: new EnvironmentSemanticBindings(Array.Empty<long>(), Array.Empty<long>(),
                    obstacles.Select(o => o.EntityId).ToArray()),
                obstacleContents: obstacles.ToArray(),
                expectedBattleInstanceId: 1, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }

        private static CombatCommandInput WithKind(CombatCommandInput i, CombatCommandKind kind) =>
            new CombatCommandInput(0, kind, i.ActorId, i.Aim, i.State, i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts,
                i.Souls, i.Artifacts, i.TargetCell, i.Rotation, i.Battle, CombatProcessingKind.Advance, i.Path, i.MovementContext,
                i.BattleInstanceId, i.ArtifactItems, i.EnvironmentBindings, i.ObstacleContents,
                expectedBattleInstanceId: i.BattleInstanceId, expectedControllerTurnSequence: (i.State).Turn.ControllerTurnSequence);
    }
}
