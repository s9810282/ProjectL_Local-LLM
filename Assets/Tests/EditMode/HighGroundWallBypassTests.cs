using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 고지대 → 저지대 직격의 Wall 무시를 명령·미리보기·범위·반응·적 AI 경로로 확인한다(고지대 Wall 무시 설계 §5, 계획 H2).
    /// 판정 공식 자체의 단위 시험은 <see cref="AttackElevationTests"/>가 맡는다.
    /// </summary>
    public sealed class HighGroundWallBypassTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord Actor = new CellCoord(1, 3);
        private static readonly CellCoord Wall = new CellCoord(3, 3);
        private static readonly CellCoord Low = new CellCoord(5, 3);
        private static readonly Owner[] Both = { Owner.Player, Owner.Enemy };

        [TestCase(true, false, AttackFailureReason.None)]
        [TestCase(true, true, AttackFailureReason.BlockedByWall)]
        [TestCase(false, true, AttackFailureReason.BlockedByWall)]
        [TestCase(false, false, AttackFailureReason.BlockedByWall)]
        public void Single_WallIsIgnoredOnlyFromHighToLow(bool actorHigh, bool targetHigh, AttackFailureReason reason)
        {
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low, walls: new[] { Wall }),
                Highs(actorHigh ? Actor : (CellCoord?)null, targetHigh ? Low : (CellCoord?)null));
            WallAttackAdmissionTests.AssertAgreement(input, reason);
            var evaluation = WallAttackAdmissionTests.Evaluate(input);
            if (reason == AttackFailureReason.None)
            {
                Assert.That(evaluation.BlockedContacts, Is.Empty);
                var result = CombatCommandCalculator.Calculate(input, Rules);
                Assert.That(result.BlockedContacts, Is.Empty);
                Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(2)).DamageDealt, Is.GreaterThan(0));
            }
            else
            {
                Assert.That(evaluation.BlockedContacts.Single().WallSourceIds, Is.EqualTo(new long[] { 71 }));
            }
        }

        [Test]
        public void WallCellAim_IsRejectedEvenFromHighGround()
        {
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Wall, walls: new[] { Wall },
                targets: new[] { WallAttackAdmissionTests.Target(2, Low) }), Highs(Actor));
            WallAttackAdmissionTests.AssertAgreement(input, AttackFailureReason.AppointCellRejected);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AllShape_DecidesEachCell(bool includeLowTarget)
        {
            var highTarget = new CellCoord(5, 4);
            var targets = includeLowTarget
                ? new[] { WallAttackAdmissionTests.Target(2, Low), WallAttackAdmissionTests.Target(3, highTarget) }
                : new[] { WallAttackAdmissionTests.Target(3, highTarget) };
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low, targets: targets,
                walls: new[] { new CellCoord(3, 2), Wall, new CellCoord(3, 4) },
                method: new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0)),
                Highs(Actor, highTarget));
            WallAttackAdmissionTests.AssertAgreement(input,
                includeLowTarget ? AttackFailureReason.None : AttackFailureReason.BlockedByWall);
            var contacts = WallAttackAdmissionTests.Evaluate(input).BlockedContacts.Select(c => c.Contact).ToArray();
            Assert.That(contacts, Has.Member(highTarget), "Wall 뒤 고지대 칸은 막힌다");
            Assert.That(contacts, Has.No.Member(Low), "Wall 뒤 저지대 칸은 무시한다");
            if (!includeLowTarget) return;
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
        }

        [Test]
        public void Splash_DirectBypassesButSpreadIsCheckedFromImpact()
        {
            var splashWall = new CellCoord(6, 3);
            var splashTarget = new CellCoord(5, 4);
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low,
                targets: new[] { WallAttackAdmissionTests.Target(2, Low), WallAttackAdmissionTests.Target(3, splashTarget) },
                walls: new[] { Wall, splashWall }, method: WallAttackAdmissionTests.Single(TargetingSplash.Radial)), Highs(Actor));
            WallAttackAdmissionTests.AssertAgreement(input, AttackFailureReason.None);
            var spread = WallAttackAdmissionTests.Evaluate(input).BlockedContacts
                .Single(c => c.Phase == AttackOcclusionPhase.Propagation);
            Assert.That(spread.Origin, Is.EqualTo(Low));
            Assert.That(spread.Contact, Is.EqualTo(splashWall));
            var hits = CombatCommandCalculator.Calculate(input, Rules).Attack.Value.Hits.ToArray().Select(h => h.TargetId).ToArray();
            Assert.That(hits, Is.EquivalentTo(new[] { new EntityId(2), new EntityId(3) }));
        }

        [Test]
        public void Pierce_PassesWallToLowCellsAndStopsAtHighCell()
        {
            var lowTarget = new CellCoord(4, 3);
            var highTarget = new CellCoord(6, 3);
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, lowTarget, pierce: true, walls: new[] { Wall },
                targets: new[] { WallAttackAdmissionTests.Target(2, lowTarget), WallAttackAdmissionTests.Target(3, highTarget) }),
                Highs(Actor, highTarget));
            WallAttackAdmissionTests.AssertAgreement(input, AttackFailureReason.None);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            var shot = result.Attack.Value.PiercingShot;
            Assert.That(shot.TraveledCells.ToArray(), Is.EqualTo(new[] { new CellCoord(2, 3), Wall, lowTarget, Low }),
                "Wall 칸은 경로에만 남는다");
            Assert.That(shot.Encounters.ToArray().Select(e => e.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(shot.Endpoint, Is.EqualTo(highTarget));
            Assert.That(shot.StopReason, Is.EqualTo(PiercingShotStopReason.Wall));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Range_OffersBypassedAimAndDropsItsEvidence(bool actorHigh)
        {
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low, walls: new[] { Wall }),
                Highs(actorHigh ? Actor : (CellCoord?)null));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray(), actorHigh ? Has.Member(AttackAim.ForCell(Low)) : Has.No.Member(AttackAim.ForCell(Low)));
            var contacts = range.BlockedContacts.Select(c => c.Contact).ToArray();
            Assert.That(contacts, actorHigh ? Has.No.Member(Low) : Has.Member(Low));
            Assert.That(contacts, Has.Member(Wall), "Wall 칸 자체는 고지대에서도 증거로 남는다");
        }

        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void Lobbed_IsUnchangedByElevation(bool actorHigh, bool targetHigh)
        {
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low, walls: new[] { Wall }, policy: AttackOcclusionSpec.Lobbed),
                Highs(actorHigh ? Actor : (CellCoord?)null, targetHigh ? Low : (CellCoord?)null));
            WallAttackAdmissionTests.AssertAgreement(input, AttackFailureReason.None);
            Assert.That(WallAttackAdmissionTests.Evaluate(input).BlockedContacts, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Reaction_UsesSameRule(bool actorHigh)
        {
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low, walls: new[] { Wall }),
                Highs(actorHigh ? Actor : (CellCoord?)null));
            var result = CombatAttackCalculator.Reaction(input, Rules, input.Characters[0], AttackAim.ForCell(Low));
            Assert.That(result != null, Is.EqualTo(actorHigh));
        }

        /// <summary>D8: 맞는 칸이 플레이어에게 숨은 고지대면 미리보기는 저지대로 보고 허용, 실행은 실제 고지대로 보고 거절한다.</summary>
        [Test]
        public void HiddenHighTarget_PreviewAllowsButExecutionRejectsWithoutChange()
        {
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low, walls: new[] { Wall }),
                High(91, Actor, Both), High(92, Low, new[] { Owner.Enemy }));
            Assert.That(CombatCommandCalculator.Preview(input, Rules).FailureReason, Is.EqualTo(AttackFailureReason.None));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray(), Has.Member(input.Aim));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(AttackFailureReason.BlockedByWall.ToString()));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        /// <summary>D8: 공격자 칸이 플레이어에게 숨은 고지대면 미리보기·범위는 Wall 너머 조준을 주지 않고, 실행은 실제 값으로 받아들인다.</summary>
        [Test]
        public void HiddenHighAttacker_PreviewDeniesButExecutionAccepts()
        {
            var input = WithHighGround(WallAttackAdmissionTests.Input(Actor, Low, walls: new[] { Wall }),
                High(91, Actor, new[] { Owner.Enemy }));
            Assert.That(CombatCommandCalculator.Preview(input, Rules).FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray(), Has.No.Member(input.Aim));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        [Test]
        public void EnemyOnHighGround_AttacksAcrossWallWithoutMoving()
        {
            var input = WithHighGround(EnemyInput(new CellCoord(1, 1)), High(91, new CellCoord(1, 1), Both));
            var aim = AttackAim.ForCell(new CellCoord(3, 1));
            Assert.That(CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Rules).CanAttack, Is.True);
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out _), Is.False, "이미 공격할 수 있는 자리");
        }

        /// <summary>
        /// x = 2 열 전체가 Wall이라 저지대 어디서도 대상 (3, 1)을 쏠 수 없고 건너갈 수도 없다.
        /// 고지대 (1, 1)이 있을 때만 공격 자리가 생기고 추격이 그 칸을 고른다.
        /// (Wall 하나만 두면 Wall 모서리를 스치는 저지대 칸이 고지대 진입 비용 +2보다 싸서 그쪽을 고른다.)
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void EnemyPursuit_TreatsHighGroundCellAsAttackPosition(bool highGround)
        {
            var input = EnemyInput(new CellCoord(0, 1), Enumerable.Range(0, 5).Select(y => new CellCoord(2, y)).ToArray());
            if (highGround) input = WithHighGround(input, High(91, new CellCoord(1, 1), Both));
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.EqualTo(highGround));
            if (highGround) Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(1, 1)));
        }

        /// <summary>
        /// 적 (actorCell)·플레이어 대상 (3, 1)인 5×5 적 차례 입력이다(<see cref="WallAttackAiTests"/>와 같은 배치).
        /// Wall을 주지 않으면 (2, 1) 하나다.
        /// </summary>
        private static CombatCommandInput EnemyInput(CellCoord actorCell, CellCoord[] walls = null)
        {
            var input = EnemyMovementPathTests.Input(actorCell: actorCell, targetCell: new CellCoord(3, 1));
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, appointRange: 4) : c.OwnershipRole == CharacterOwnershipRole.Main
                    ? CombatCommandTestData.With(c, position: new CellCoord(4, 1)) : c).ToArray());
            return WallMovementTests.WithWalls(input, (walls ?? new[] { new CellCoord(2, 1) })
                .Select((cell, i) => WallStateLifecycleTests.Wall(71 + i, cell)).ToArray());
        }

        /// <summary>
        /// 기존 환경(Wall 등)에 고지대 원본을 더하고 지형 의미 분류에도 넣는다. Wall 차단 설정은 그대로 둔다.
        /// 환경 진행 사실은 새 원본 ID를 포함하도록 다시 만든다(옛 값을 넘기면 "다음 환경 식별자" 검사에 걸린다).
        /// </summary>
        private static CombatCommandInput WithHighGround(CombatCommandInput input, params StaticTerrainFact[] highGround)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                s.Structures,s.ThornFields,s.Turn,                new MovementEnvironmentFacts(s.Environment.Tiles.Concat(highGround).ToArray()),environmentProgress: null,                wallBlocking: s.WallBlocking,objective: s.Objective,allocation: s.Allocation,                itemRandomState: s.ItemRandomState,rewards: s.Rewards);
            var b = input.EnvironmentBindings;
            var bindings = new EnvironmentSemanticBindings(b.TerrainSourceIds.Concat(highGround.Select(h => h.Id)).ToArray(),
                b.ObjectSourceIds, b.ObstacleEntityIds);
            return input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls, bindings);
        }

        private static StaticTerrainFact[] Highs(params CellCoord?[] cells) =>
            cells.Where(c => c.HasValue).Select((c, i) => High(91 + i, c.Value, Both)).ToArray();

        private static StaticTerrainFact High(long id, CellCoord cell, Owner[] knownTo) =>
            new StaticTerrainFact(id, cell, StaticTerrainKind.HighGround, true, knownTo);
    }
}
