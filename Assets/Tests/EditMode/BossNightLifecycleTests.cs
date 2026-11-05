using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Summoning;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>낮을 지나도 보스·소환수가 같은 개체로 보존되고 다음 밤에 정책대로 복귀하는지 확인한다(계획 T3.2, G1·G4·G5).</summary>
    public sealed class BossNightLifecycleTests
    {
        private static readonly EntityId Boss = CombatSummonTests.Boss;
        private static readonly EntityId Summoned = new EntityId(11);

        [Test]
        public void SummonThenNightEnds_RecallsSameEntityWithRecord()
        {
            var night = EndNight(new CellCoord(3, 3));

            var summoned = CombatCommandComposition.Project(night).Characters.Single(c => c.EntityId == Summoned);
            Assert.That(summoned.EntityId, Is.EqualTo(Summoned));
            Assert.That(summoned.Area, Is.EqualTo(CharacterRuntimeArea.EnemyWaiting));
            Assert.That(summoned.Health.IsDead, Is.False);
            Assert.That(night.FinalState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
            Assert.That(night.FinalState.IsNight, Is.False);
            Assert.That(night.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.None));
            Assert.That(night.FinalState.Turn.Summons.Single().SummonedId, Is.EqualTo(Summoned));
            Assert.That(night.Characters.Characters.Single(c => c.Final.EntityId == Boss).Final.Area, Is.EqualTo(CharacterRuntimeArea.EnemyWaiting));
        }

        [Test]
        public void NightDayNight_PreservesIdentityAndStats_ReturnsAroundBossWithOpening()
        {
            var day = Damaged(CombatCommandComposition.Project(EndNight(new CellCoord(3, 3))), summonHp: 4, bossHp: 50);

            var entered = CombatCommandCalculator.Calculate(EnterCombat(day), Array.Empty<IMovementTileRule>());

            Assert.That(entered.Accepted, Is.True, entered.Reason);
            var final = CombatCommandComposition.Project(entered);
            var boss = final.Characters.Single(c => c.EntityId == Boss);
            var summoned = final.Characters.Single(c => c.EntityId == Summoned);
            Assert.That(boss.Anchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(boss.Health.CurrentHealth, Is.EqualTo(50));
            Assert.That(summoned.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(summoned.Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(Math.Max(Math.Abs(summoned.Anchor.Value.X - 3), Math.Abs(summoned.Anchor.Value.Y - 3)), Is.EqualTo(1));
            Assert.That(summoned.Rotation, Is.EqualTo(boss.Rotation));
            // G1: 복귀한 소환수는 다른 유닛과 같은 밤 시작 순서로 참가한다.
            Assert.That(summoned.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That(entered.Characters.Created, Is.Empty);
            Assert.That(entered.FinalState.Allocation.NextEntityId, Is.EqualTo(day.State.Allocation.NextEntityId));
            Assert.That(entered.FinalState.Turn.Summons.SequenceEqual(day.State.Turn.Summons), Is.True);
        }

        [Test]
        public void NoRoomOnReturn_StaysWaiting_AndCombatEntryContinues()
        {
            var day = CombatCommandComposition.Project(EndNight(new CellCoord(0, 0)));
            var blocked = WithStructures(day, new CellCoord(1, 0), new CellCoord(0, 1), new CellCoord(1, 1));

            var entered = CombatCommandCalculator.Calculate(EnterCombat(blocked), Array.Empty<IMovementTileRule>());

            Assert.That(entered.Accepted, Is.True, entered.Reason);
            var final = CombatCommandComposition.Project(entered);
            Assert.That(final.Characters.Single(c => c.EntityId == Summoned).Area, Is.EqualTo(CharacterRuntimeArea.EnemyWaiting));
            Assert.That(final.Characters.Single(c => c.EntityId == Boss).Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(final.Characters.Single(c => c.EntityId == Summoned).ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            // 대기 중인 소환수도 생존 상한에 포함된다.
            Assert.That(CombatSummonCalculator.LivingSummons(final, Boss, TraitKey.Necromancy), Is.EqualTo(1));
        }

        [Test]
        public void DeadSummon_NeverReturns_AndFreesItsSlot()
        {
            var day = CombatCommandComposition.Project(EndNight(new CellCoord(3, 3)));
            var dead = CombatCommandTestData.With(day, day.Characters.Select(c => c.EntityId == Summoned
                ? CombatCommandCalculator.Copy(c, new HealthStateSnapshot(0, c.Health.MaxHealth), c.Stats) : c).ToArray());

            var entered = CombatCommandCalculator.Calculate(EnterCombat(dead), Array.Empty<IMovementTileRule>());

            Assert.That(entered.Accepted, Is.True, entered.Reason);
            var final = CombatCommandComposition.Project(entered);
            Assert.That(final.Characters.Single(c => c.EntityId == Summoned).Area, Is.EqualTo(CharacterRuntimeArea.Excluded));
            Assert.That(final.Characters.Single(c => c.EntityId == Summoned).Health.IsDead, Is.True);
            Assert.That(CombatSummonCalculator.LivingSummons(final, Boss, TraitKey.Necromancy), Is.Zero);
        }

        [Test]
        public void NightDayNight_ReserveReturnResetsRoarCounter()
        {
            // 실제 예비 구역 진입에서 임시 포효 카운터를 비우고 다음 밤에도 초기값을 유지한다.
            var day = CombatCommandComposition.Project(EndNight(new CellCoord(3, 3), roarHits: 4));
            Assert.That(RoarHitCounter.HitCount(day.Characters.Single(c => c.EntityId == Boss)), Is.Zero);

            var entered = CombatCommandCalculator.Calculate(EnterCombat(day), Array.Empty<IMovementTileRule>());

            Assert.That(entered.Accepted, Is.True, entered.Reason);
            Assert.That(entered.Events.OfType<CombatRoarEvent>(), Is.Empty);
            Assert.That(RoarHitCounter.HitCount(CombatCommandComposition.Project(entered).Characters.Single(c => c.EntityId == Boss)),
                Is.Zero);
        }

        [Test]
        public void PendingRoarAcrossDay_RoarsAfterReturnAtNextNight()
        {
            // 밤이 끝날 때 대기 포효가 남았으면 카운터를 보존하고, 다음 밤 보스가 필드에 복귀한 뒤 첫 반응 경계에서 포효한다(G6-6).
            var day = CombatCommandComposition.Project(EndNight(new CellCoord(3, 3), roarHits: 4));
            day = CombatCommandTestData.With(day, day.Characters.Select(c => c.EntityId != Boss ? c : CombatCommandCalculator.Copy(c, c.Health,
                CombatCommandEffects.With(c.Stats,
                    traits: c.Stats.TraitStatuses.ToArray().Select(t => t is RoarTraitStatusSnapshot roar ? new RoarTraitStatusSnapshot(5, roar.HitThreshold) : t).ToArray())))
                .ToArray());
            Assert.That(BossRoarCalculator.PendingBosses(day), Is.Empty);

            var entered = CombatCommandCalculator.Calculate(EnterCombat(day), Array.Empty<IMovementTileRule>());

            Assert.That(entered.Accepted, Is.True, entered.Reason);
            Assert.That(entered.Events.OfType<CombatRoarEvent>().Single().BossId, Is.EqualTo(Boss));
            Assert.That(RoarHitCounter.HitCount(CombatCommandComposition.Project(entered).Characters.Single(c => c.EntityId == Boss)),
                Is.Zero);
        }

        /// <summary>이미 존재하는 소환 기록을 가진 여덟 번째 적 응답의 종료를 만든다. 소환 발동은 하지 않는다.</summary>
        private static CombatCommandTransition EndNight(CellCoord bossCell, int? roarHits = null)
        {
            var summoned = CombatSummonTests.Enemy(Summoned.Value, new CellCoord(bossCell.X + 1, bossCell.Y), EntityKey.Unit_Gravekeeper)
                .With(actionUse: UnitActionUse.Moved);
            var input = BossTurnTests.BossInput(bossCell, completed: 8,
                records: new[] { new CombatSummonRecord(Summoned, Boss, TraitKey.Necromancy) }, extra: new[] { summoned });
            if (roarHits.HasValue) input = BossTurnTests.WithRoar(input, roarHits.Value);
            var result = CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        private static CombatCommandInput Damaged(CombatCommandInput day, int summonHp, int bossHp) =>
            CombatCommandTestData.With(day, day.Characters.Select(c => c.EntityId == Summoned || c.EntityId == Boss
                ? CombatCommandCalculator.Copy(c, new HealthStateSnapshot(c.EntityId == Boss ? bossHp : summonHp, c.Health.MaxHealth), c.Stats)
                : c).ToArray());

        private static CombatCommandInput WithStructures(CombatCommandInput i, params CellCoord[] cells)
        {
            var s = i.State;
            var structures = cells.Select((cell, index) => new StructureCombatFacts(new EntityId(40 + index),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 10, false, 0),
                new HealthStateSnapshot(10, 10), cell, Rotation4.R0, new[] { cell }, Owner.Enemy)).ToArray();
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.IsNight,structures,                s.ThornFields,s.Turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,s.Objective,s.Allocation);
            return CombatCommandTestData.With(i, state: state);
        }

        private static CombatCommandInput EnterCombat(CombatCommandInput day) =>
            new CombatCommandInput(1, CombatCommandKind.EnterCombat, EntityId.Empty, default, day.State, day.Characters,
                day.Profiles, day.Main, day.Owned, day.SoulCounts, day.Souls, day.Artifacts, battle: day.Battle,
                environmentBindings: day.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (day.State).Turn.ControllerTurnSequence);
    }
}
