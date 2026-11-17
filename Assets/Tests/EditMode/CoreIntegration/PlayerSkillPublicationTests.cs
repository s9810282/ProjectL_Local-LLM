using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PlayerSkillPublicationTests
    {
        [Test]
        public void RealPowderBlossomPublishesOccurrenceHealthAndOneUseThroughTheExistingPublisher()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            fixture.Move(fixture.Battle.Main.EntityId, new[] { CellCoord.Zero, new CellCoord(1, 0), new CellCoord(1, 1), new CellCoord(1, 2) });
            var input = fixture.Input();
            var before = fixture.Battle.Buffers.CombatStateReadSink.LatestResultSequence;
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.HealthClaims, Is.Not.Empty);
            Assert.That(fixture.Publisher.Publish(transition), Is.True);
            var buffers = fixture.Battle.Buffers;
            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Sequence, Is.EqualTo(before + 1));
            Assert.That(result.PlayerSkill, Is.SameAs(transition.PlayerSkill));
            Assert.That(result.Events.OfType<CombatPlayerSkillAttackEvent>().First(),
                Is.SameAs(transition.Events.OfType<CombatPlayerSkillAttackEvent>().First()));
            Assert.That(buffers.CombatStateReadSink.Current.CommandState.PlayerSkill.RemainingUses, Is.EqualTo(1));
            Assert.That(fixture.Battle.Main.ActionUse, Is.EqualTo(input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SyntheticCast_PublishesOneSequenceAndPreservesOrdinaryAction(bool ordinaryAlreadyUsed)
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var battle = fixture.Battle;
            if (ordinaryAlreadyUsed) fixture.AttackWithMain();
            var beforeAction = battle.Main.ActionUse;
            var input = fixture.Input();
            var transition = fixture.Cast(input);
            int sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            Assert.That(fixture.Publisher.Publish(transition), Is.True);
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(state.PlayerSkill.RemainingUses, Is.EqualTo(1));
            Assert.That(state.PlayerSkill.LastUsedControllerTurnSequence, Is.EqualTo(input.State.Turn.ControllerTurnSequence));
            Assert.That(battle.Main.ActionUse, Is.EqualTo(beforeAction));
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.PlayerSkill, Is.SameAs(transition.PlayerSkill));
            Assert.That(result.Events.Single(), Is.SameAs(transition.Events.Single()));
            Assert.That(result.Attack, Is.Null);
            Assert.That(result.ActionUseSettlement, Is.Null);

            var once = new CoreIntegrationSnapshot(battle.Buffers);
            fixture.Publisher.Publish(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [TestCase("missing")]
        [TestCase("noCharge")]
        [TestCase("doubleCharge")]
        [TestCase("turn")]
        [TestCase("actor")]
        [TestCase("binding")]
        [TestCase("ordinaryAction")]
        [TestCase("borrowedEvent")]
        public void MalformedSkillSettlement_RejectsBeforeAnyStoreChanges(string mutation)
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var input = fixture.Input();
            var original = new CoreIntegrationSnapshot(fixture.Battle.Buffers);
            Assert.Catch<Exception>(() => fixture.Publisher.Publish(fixture.Cast(input, mutation)));
            original.AssertUnchanged(fixture.Battle.Buffers);
        }

        [TestCase("expectedRevision")]
        [TestCase("capturedRevision")]
        [TestCase("staleRevision")]
        [TestCase("battle")]
        [TestCase("turn")]
        [TestCase("actor")]
        [TestCase("missingExpected")]
        [TestCase("missingCaptured")]
        public void StaleOrMismatchedSkillSource_DoesNotConsumeUsesOrRandom(string mutation)
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var input = fixture.Input(mutation);
            var before = new CoreIntegrationSnapshot(fixture.Battle.Buffers);
            Assert.Catch<Exception>(() => fixture.Publisher.Publish(fixture.Cast(input)));
            before.AssertUnchanged(fixture.Battle.Buffers);
        }

        [Test]
        public void RejectedSkill_OnlyAdvancesHandledAndResultBookkeeping()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            var input = fixture.Input("expectedRevision");
            var gameplay = fixture.Battle.CaptureGameplay();
            long revision = fixture.Battle.Buffers.CombatStateReadSink.QueryRevision;
            int sequence = fixture.Battle.Buffers.CombatStateReadSink.LatestResultSequence;
            var rejected = CombatCommandCalculator.Reject(input, PlayerSkillFailureReason.SourceChanged.ToString());
            fixture.Publisher.Publish(rejected);
            var after = fixture.Battle.CaptureGameplay();
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.Current.CommandState.SameValues(input.State), Is.True);
            // 거절 결과 게시도 조회 래퍼는 새로 만듭니다. 게임 사실은 값과 나머지 저장소 원본으로 확인합니다.
            Assert.That(fixture.Battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts())
                .Zip(input.Characters, (actual, expected) => actual.SameValues(expected)).All(same => same), Is.True);
            Assert.That(after.Where((_, index) => index != 3 && index != 6),
                Is.EqualTo(gameplay.Where((_, index) => index != 3 && index != 6)));
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision));
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(input.Number));
            var once = new CoreIntegrationSnapshot(fixture.Battle.Buffers);
            fixture.Publisher.Publish(rejected);
            once.AssertUnchanged(fixture.Battle.Buffers);
        }

        [Test]
        public void OrdinaryCommand_CannotResetSkillUsage()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            fixture.Publisher.Publish(fixture.Cast(fixture.Input()));
            var input = fixture.Assemble(CombatCommandKind.EquipItem);
            var reset = input.State.PlayerSkill.WithUsage(2, 0);
            var forged = new CombatCommandTransition(input, true, "",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State.With(playerSkill: reset), default);
            var before = new CoreIntegrationSnapshot(fixture.Battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => fixture.Publisher.Publish(forged));
            before.AssertUnchanged(fixture.Battle.Buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedDamageAndForcedMovement_PublishTheirExactOccurrences(bool knockback)
        {
            using var fixture = new PlayerSkillPublicationFixture(knockback);
            var input = fixture.Input();
            var target = fixture.Battle.Character(EntityKey.Unit_Demon);
            var transition = fixture.DamagingCast(input, target.EntityId);
            Assert.DoesNotThrow(() => CombatHealthEvidenceValidator.Validate(transition));
            Assert.That(transition.Events.OfType<CombatPlayerSkillAttackEvent>().Single().Attack.Hits.Single().TargetId,
                Is.EqualTo(target.EntityId));
            int sequence = fixture.Battle.Buffers.CombatStateReadSink.LatestResultSequence;
            fixture.Publisher.Publish(transition);
            Assert.That(fixture.Battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(fixture.Battle.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(knockback ? new CellCoord(0, 4) : new CellCoord(0, 3)));
            Assert.That(fixture.Battle.Main.ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.Current.CommandState.PlayerSkill.RemainingUses, Is.EqualTo(1));
            Assert.That(fixture.Battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events, Is.EqualTo(transition.Events));
            Assert.That(result.PlayerSkill, Is.SameAs(transition.PlayerSkill));
        }

        [Test]
        public void SkillCompletion_PreservesPayloadAndSettlesOneUseThroughPublisher()
        {
            using var fixture = new PlayerSkillPublicationFixture(terminalHeadquarters: true);
            var input = fixture.Input();
            var headquarters = input.State.Structures.Single(s => s.Definition.Key == EntityKey.Structure_Headquarters);
            var transition = fixture.DamagingCast(input, headquarters.EntityId);
            Assert.That(transition.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            fixture.Publisher.Publish(transition);
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(fixture.Battle.Buffers.CombatStateReadSink.Current.CommandState.PlayerSkill.RemainingUses, Is.EqualTo(1));
            Assert.That(fixture.Battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.PlayerSkill, Is.SameAs(transition.PlayerSkill));
            Assert.That(result.Events, Is.EqualTo(transition.Events));
        }

        [Test]
        public void SkillInvalidatesItsActorsMoveButPreservesUnrelatedCheckpoint()
        {
            using var fixture = new PlayerSkillPublicationFixture();
            fixture.Move(fixture.Battle.Actor.EntityId, new[] { new CellCoord(0, 1), new CellCoord(1, 1) });
            fixture.Move(fixture.Battle.Main.EntityId, new[] { CellCoord.Zero, new CellCoord(1, 0) });
            var before = fixture.Battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo;
            Assert.That(before.Checkpoints.Count, Is.EqualTo(2));
            var transition = MovementUndoPolicy.Reconcile(fixture.Cast(fixture.Input()));
            fixture.Publisher.Publish(transition);
            var after = fixture.Battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo;
            Assert.That(after.Checkpoints.Single().ActorId, Is.EqualTo(fixture.Battle.Actor.EntityId));
            Assert.That(fixture.Battle.Main.ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }
    }

    internal sealed class PlayerSkillPublicationFixture : IDisposable
    {
        private readonly bool _knockback;
        internal ConfiguredMovementBattle Battle { get; }
        internal CombatTransitionPublisher Publisher => Battle.Publication().Publisher;

        internal PlayerSkillPublicationFixture(bool knockback = false, bool terminalHeadquarters = false,
            PlayerSkillKey skillKey = PlayerSkillKey.PowderBlossom, PlayerSkillUpgradeKey upgrade = PlayerSkillUpgradeKey.None)
        {
            _knockback = knockback;
            Battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3), terminalHeadquarters), enterCombat: false);
            var buffers = Battle.Buffers;
            var current = buffers.CombatStateReadSink.Current;
            var skill = new PlayerSkillBattleState(Battle.Main.OwnedId.Value, Battle.Main.EntityId,
                new PlayerSkillDefinition(skillKey, EntityKey.Unit_Akimbo, 2), upgrade);
            var combat = new CombatPreparation(current.BoardWidth, current.BoardHeight, current.ReserveCapacity,
                current.TimelineSeed, CombatFlowPhase.PlayerInput, current.DrawOrder, current.DrawCursor, current.PlayerZone,
                current.TraitSettings, current.RailCells.ToArray(), current.StageGimmicks.ToArray(), current.Environment,
                current.EnvironmentBindings, current.ObstacleContents, current.CommandState.WallBlocking);
            var preparation = new BattlePreparation(buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray(),
                combat, current.Structures, commandState: current.CommandState.With(playerSkill: skill),
                objective: current.CommandState.Objective, membership: current.Membership, rosterRevision: buffers.CharacterReadSink.RosterRevision);
            buffers.BattleWriteSink.EndBattle();
            // Phase B는 스킬 조립 전의 저장 계약만 검사합니다. 시험 소유 초기 상태도 실제 준비·설치 경로를 거칩니다.
            buffers.BattleWriteSink.PrepareBattle(preparation);
            Publisher.Publish(CombatCommandCalculator.Calculate(Assemble(CombatCommandKind.EnterCombat), Array.Empty<IMovementTileRule>()));
            Publisher.Publish(CombatCommandCalculator.Advance(Assemble(CombatCommandKind.StartControllerTurn, advance: true), Array.Empty<IMovementTileRule>()));
        }

        internal CombatCommandInput Assemble(CombatCommandKind kind, bool advance = false)
        {
            var b = Battle.Buffers;
            int number = advance ? 0 : b.CombatViewWriteSink.Submit(CombatViewCommandData.ReturnUnit(Battle.Main.EntityId));
            return Battle.Publication().Assembler.Assemble(new CombatActionRequest(number, kind, Battle.Main.EntityId,
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.AttackAim.ForCell(
                    Battle.Character(EntityKey.Unit_Demon).Anchor.Value),
                processingKind: advance ? CombatProcessingKind.Advance : CombatProcessingKind.Command,
                expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
        }

        internal void AttackWithMain()
        {
            Move(Battle.Main.EntityId, new[] { CellCoord.Zero, new CellCoord(1, 0), new CellCoord(1, 1), new CellCoord(1, 2) });
            var result = CombatCommandCalculator.Calculate(Assemble(CombatCommandKind.Attack), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Publisher.Publish(result);
            Assert.That(Battle.Main.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        internal void Move(EntityId actor, CellCoord[] path)
        {
            var input = Assemble(CombatCommandKind.Move).WithActor(actor)
                .WithIntent(CombatCommandKind.Move, default, path.Last(), new MovementPath(path), null);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Publisher.Publish(result);
        }

        internal CombatCommandInput Input(string mutation = null)
        {
            var i = Assemble(CombatCommandKind.UsePlayerSkill);
            var b = Battle.Buffers.CombatStateReadSink;
            var source = new PlayerSkillSourceStamp(b.BattleInstanceId, i.State.Turn.ControllerTurnSequence, b.QueryRevision, i.ActorId);
            PlayerSkillSourceStamp? expected = mutation switch
            {
                "expectedRevision" or "staleRevision" => new PlayerSkillSourceStamp(source.BattleInstanceId, source.ControllerTurnSequence, source.QueryRevision + 1, source.ActorId),
                "battle" => new PlayerSkillSourceStamp(source.BattleInstanceId + 1, source.ControllerTurnSequence, source.QueryRevision, source.ActorId),
                "turn" => new PlayerSkillSourceStamp(source.BattleInstanceId, source.ControllerTurnSequence + 1, source.QueryRevision, source.ActorId),
                "actor" => new PlayerSkillSourceStamp(source.BattleInstanceId, source.ControllerTurnSequence, source.QueryRevision, Battle.Actor.EntityId),
                "missingExpected" => null,
                _ => source
            };
            PlayerSkillSourceStamp? captured = mutation == "missingCaptured" ? null : mutation is "capturedRevision" or "staleRevision"
                ? new PlayerSkillSourceStamp(source.BattleInstanceId, source.ControllerTurnSequence, source.QueryRevision + 1, source.ActorId) : source;
            return new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, i.State, i.Characters, i.Profiles, i.Main,
                i.Owned, i.SoulCounts, i.Souls, i.Artifacts, i.TargetCell, i.Rotation, i.Battle, i.ProcessingKind,
                i.Path, i.MovementContext, i.BattleInstanceId, i.ArtifactItems, i.EnvironmentBindings, i.ObstacleContents,
                i.ItemKey, i.ItemInventory, i.ItemDefinitions, i.GoldBalance, i.FameBalance, i.Membership,
                i.ExpectedBattleInstanceId, i.ExpectedControllerTurnSequence, i.ExpectedMoveCommandNumber, i.SweepSide,
                new PlayerSkillRequest(Array.Empty<CellCoord>(), expected), captured);
        }

        internal CombatCommandTransition Cast(CombatCommandInput input, string mutation = null)
        {
            var before = input.State.PlayerSkill;
            var use = new PlayerSkillUseSettlement(mutation == "actor" ? Battle.Actor.EntityId : input.ActorId,
                before.RemainingUses, before.LastUsedControllerTurnSequence,
                input.State.Turn.ControllerTurnSequence + (mutation == "turn" ? 1 : 0));
            var finalSkill = before.WithUsage(use.FinalRemainingUses, use.FinalLastUsedControllerTurnSequence);
            if (mutation == "noCharge") finalSkill = before;
            if (mutation == "doubleCharge") finalSkill = before.WithUsage(0, use.FinalLastUsedControllerTurnSequence);
            if (mutation == "binding") finalSkill = new PlayerSkillBattleState(before.MainInstanceId, before.ActorId,
                new PlayerSkillDefinition(before.Definition.Key, before.Definition.MainCharacterKey, 3), before.UpgradeKey)
                .WithUsage(1, use.FinalLastUsedControllerTurnSequence);
            var attack = new PlayerSkillAttackResult(input.ActorId, 0, 1, Array.Empty<CellCoord>(), Array.Empty<AttackHitResult>());
            var occurrence = new CombatPlayerSkillAttackEvent(attack);
            var events = new CombatEvent[] { occurrence };
            var skill = mutation == "missing" ? null : new PlayerSkillResult(before.Definition, before.UpgradeKey,
                mutation == "borrowedEvent" ? new CombatEvent[] { new CombatPlayerSkillAttackEvent(attack) } : events, use);
            var changes = mutation == "ordinaryAction" ? new[] { new CharacterStateChange(Battle.Main,
                Battle.Main.With(actionUse: UnitActionUse.Attacked)) } : Array.Empty<CharacterStateChange>();
            return new CombatCommandTransition(input, true, "", new CharacterChanges(changes, Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State.With(playerSkill: finalSkill), default,
                events: events, playerSkill: skill);
        }

        internal CombatCommandTransition DamagingCast(CombatCommandInput input, EntityId targetId)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var target = input.Characters.FirstOrDefault(c => c.EntityId == targetId);
            var structure = target == null ? input.State.Structures.Single(s => s.EntityId == targetId) : null;
            var targetCells = target?.OccupiedCells ?? structure.OccupiedCells;
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, characters, events);
            var origin = new CombatDamageOrigin(actor.Anchor, actor.EntityId, 1);
            int beforeHealth = target?.Health.CurrentHealth ?? structure.Health.CurrentHealth;
            if (target != null) projection.Damage.Apply(targetId, CombatDamageKind.PlayerSkillAttack, origin, 5);
            else projection.ChangeHealth(new MovementHealthRequest(targetId, 5), false, origin);
            int afterHealth = target == null ? projection.Structure(targetId).Health.CurrentHealth : characters[targetId].Health.CurrentHealth;
            var knockbacks = Array.Empty<AttackKnockbackSnapshot>();
            if (_knockback && target != null)
            {
                // 기하 규칙을 활성화하지 않는 B2 게시 표본입니다. 실제 강제 이동 계산은 Core 시험에서 별도로 검증합니다.
                var beforeMove = characters[targetId];
                var destination = beforeMove.Anchor.Value + CellCoord.Up;
                var afterMove = beforeMove.WithPlacement(beforeMove.Area, beforeMove.ReserveSlotIndex, destination, beforeMove.Rotation,
                    beforeMove.OccupiedCells.Select(cell => cell + CellCoord.Up).ToArray());
                projection.MergeCharacter(new CharacterStateChange(beforeMove, afterMove));
                events.Add(new CombatCharacterEvent(CombatEventKind.Moved, beforeMove, afterMove, null, null,
                    CombatDamageCause.NumericDamage, new CombatMovementCause(CombatMovementAgency.Forced)));
                knockbacks = new[] { new AttackKnockbackSnapshot(targetId, AttackKnockbackTargetKind.Character,
                    beforeMove.Anchor.Value, CellCoord.Up, 1, new[] { destination }, AttackKnockbackStop.Completed) };
            }
            var hit = new AttackHitResult(targetId, beforeHealth, afterHealth, AttackTargetRole.Designated, 100, 0, 0,
                new[] { new AttackHitContactSnapshot(0, targetId, AttackTargetRole.Designated, 100, origin) });
            var occurrence = new CombatPlayerSkillAttackEvent(new PlayerSkillAttackResult(input.ActorId, 0, 1, targetCells,
                new[] { hit }, knockbacks));
            events.Insert(0, occurrence);
            var steps = projection.Damage.HealthExecutions.SelectMany(e => e.Steps).ToArray();
            // Application 전용 생성자를 게시 표본에서만 호출합니다. 제품 어셈블리의 접근 범위는 바꾸지 않습니다.
            var healthScope = (CombatHealthScope)Activator.CreateInstance(typeof(CombatHealthScope),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { occurrence, -1, steps }, null);
            var claim = (CombatHealthClaim)Activator.CreateInstance(typeof(CombatHealthClaim),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { CombatHealthClaimKind.AttackHit, occurrence, -1, 0, targetId, steps, healthScope, null }, null);
            projection.Damage.HealthClaims.Add(claim);
            var resolved = projection.Complete();
            var before = input.State.PlayerSkill;
            var use = new PlayerSkillUseSettlement(input.ActorId, before.RemainingUses, before.LastUsedControllerTurnSequence,
                input.State.Turn.ControllerTurnSequence);
            var result = new PlayerSkillResult(before.Definition, before.UpgradeKey, resolved.Events, use);
            return MovementUndoPolicy.Reconcile(new CombatCommandTransition(input, true, "", resolved.Characters, resolved.Field,
                resolved.FinalState.With(playerSkill: before.WithUsage(use.FinalRemainingUses, use.FinalLastUsedControllerTurnSequence)),
                resolved.Outcome, consumedSoulIds: resolved.ConsumedSoulIds, events: resolved.Events,
                environmentBindings: resolved.EnvironmentBindings, rewardChange: resolved.RewardChange,
                rewardSettlement: resolved.RewardSettlement, movementEffectUses: resolved.MovementEffectUses,
                healthExecutions: resolved.HealthExecutions, healthClaims: resolved.HealthClaims, playerSkill: result));
        }

        public void Dispose() => Battle.Dispose();
    }
}
