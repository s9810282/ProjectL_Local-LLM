using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public class CombatCommonPublicationTests
    {
        [Test]
        public void QueriesAfterEndBattle_ReportMissingBattle()
        {
            var b = Create(out var service);
            b.BattleWriteSink.EndBattle();
            service.EndBattle();
            Assert.Throws<InvalidOperationException>(() => service.GetTimeline());
            Assert.Throws<InvalidOperationException>(() => service.GetDrawAvailability());
        }
        [Test]
        public void DrawPublication_InstallsCharacterCurrencyCursorAndResultOnce()
        {
            var b = SecondPreparation(out var service);
            int beforeCount = b.CharacterReadSink.Characters.Count;
            var before = b.CombatStateReadSink.Current;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            var healthAndActions = b.CharacterReadSink.Characters
                .Select(c => (c.EntityId, c.Health, c.Stats.CurrentActionCost)).ToArray();
            var souls = b.CharacterReadSink.Owned.Select(o => b.CharacterReadSink.GetSoulCount(o.Id)).ToArray();
            var query = service.GetDrawAvailability();
            Assert.That(query.CanDraw, Is.True, query.FailureReason.ToString());
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            int number = b.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
            Assert.That(service.ProcessNextCommand(), Is.True);
            var installed = b.CombatStateReadSink.Current;
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var published), Is.True);
            Assert.That(published.Accepted, Is.True, published.Reason);
            Assert.That(service.ProcessNextCommand(), Is.False);
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(installed));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(number));
            Assert.That(b.CombatViewReadSink.Commands.Any(c => c.Number == number), Is.True);
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
            Assert.That(b.CharacterReadSink.Characters.Take(beforeCount)
                .Select(c => (c.EntityId, c.Health, c.Stats.CurrentActionCost)), Is.EqualTo(healthAndActions));
            Assert.That(b.CharacterReadSink.Owned.Select(o => b.CharacterReadSink.GetSoulCount(o.Id)), Is.EqualTo(souls));
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            // 드로우는 스테이지 골드만 줄이고 Run 재화는 그대로 둔다.
            Assert.That(b.CombatStateReadSink.Current.StageGold, Is.EqualTo(8));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            Assert.That(b.CharacterReadSink.Characters.Count, Is.EqualTo(beforeCount + 1));
            Assert.That(b.CombatStateReadSink.Current.DrawCursor, Is.EqualTo(before.DrawCursor + 1));
            Assert.That(b.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(before.NextEntityId + 1));
            b.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result, Is.SameAs(published));
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Draw.FinalGold, Is.EqualTo(8));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Drawn).Final.ReserveSlotIndex, Is.EqualTo(query.Slot));
        }

        [Test]
        public void InvalidDrawReserveSlot_InstallsNeitherCurrencyCharacterNorResult()
        {
            var b = SecondPreparation(out _);
            var before = b.CombatStateReadSink.Current;
            var characters = b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            b.CharacterReadSink.TryGetMainState(out var main);
            var definitions = CharacterBattlePreparationTests.Initialization(withEnemies: true);
            var input = new CombatCommandInput(20, CombatCommandKind.DrawCharacter, EntityId.Empty, default,
                before.CommandState, characters, definitions.Units.Select(u => new CombatAttackProfile(u,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray(),
                main, b.CharacterReadSink.Owned, b.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => b.CharacterReadSink.GetSoulCount(o.Id)),
                b.CombatStateReadSink.SoulField, new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot(0,
                    Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactPlacedEffectSnapshot>()),
                battle: new CombatBattleFacts(before.ReserveCapacity, before.NightLength, before.TimelineSeed,
                    before.PlayerDeploymentDepth, before.EnemyDeploymentDepth, before.DrawOrder, before.DrawCursor, before.NextEntityId, 10, 2,
                    categories: definitions.Categories));
            var valid = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var c = valid.Characters.Created.Single();
            int occupiedSlot = characters.First(x => x.Area == CharacterRuntimeArea.Hand).ReserveSlotIndex.Value;
            var bad = new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole, c.Area,
                c.Health, c.Stats, occupiedSlot, c.Anchor, c.Rotation, c.OccupiedCells, c.MovementCheckpointAnchor, 0, 0);
            var d = valid.Draw;
            var invalid = new CombatCommandTransition(input, true, "", new CharacterChanges(Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(), created: new[] { bad }), valid.Field, valid.FinalState, valid.Outcome,
                draw: new CombatDrawSettlement(d.EntityId, d.CharacterKey, occupiedSlot, d.ExpectedCursor, d.ExpectedNextEntityId, d.ExpectedGold, 2));
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(invalid));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CharacterReadSink.Characters.Count, Is.EqualTo(characters.Length));
            Assert.That(b.CombatStateReadSink.Current.StageGold, Is.EqualTo(10));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
        }

        [Test]
        public void StaleStageGold_DrawIsRejected_AndNothingInstalls()
        {
            var b = SecondPreparation(out _);
            var before = b.CombatStateReadSink.Current;
            var characters = b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            b.CharacterReadSink.TryGetMainState(out var main);
            var definitions = CharacterBattlePreparationTests.Initialization(withEnemies: true);
            // 계산 시점의 골드(9)가 설치된 스테이지 골드(10)와 다르면 Buffer는 드로우 전체를 거부한다.
            var input = new CombatCommandInput(20, CombatCommandKind.DrawCharacter, EntityId.Empty, default,
                before.CommandState, characters, definitions.Units.Select(u => new CombatAttackProfile(u,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray(),
                main, b.CharacterReadSink.Owned, b.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => b.CharacterReadSink.GetSoulCount(o.Id)),
                b.CombatStateReadSink.SoulField, new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot(0,
                    Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactPlacedEffectSnapshot>()),
                battle: new CombatBattleFacts(before.ReserveCapacity, before.NightLength, before.TimelineSeed,
                    before.PlayerDeploymentDepth, before.EnemyDeploymentDepth, before.DrawOrder, before.DrawCursor, before.NextEntityId, 9, 2,
                    categories: definitions.Categories));
            var stale = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(stale.Draw.ExpectedGold, Is.EqualTo(9));
            int sequence = b.CombatStateReadSink.LatestResultSequence;

            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(stale));

            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CharacterReadSink.Characters.Count, Is.EqualTo(characters.Length));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
        }

        [Test]
        public void StageGold_EndsWithBattle_AndNextBattleStartsFromItsInitialGold()
        {
            var b = SecondPreparation(out var service);
            b.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
            Assert.That(service.ProcessNextCommand(), Is.True);
            Assert.That(b.CombatStateReadSink.Current.StageGold, Is.EqualTo(8));

            b.BattleWriteSink.EndBattle();
            service.EndBattle();
            Assert.That(b.CombatStateReadSink.HasBattle, Is.False);
            service.PrepareBattle();

            // 다음 전투는 남은 골드를 이어받지 않고 자기 스테이지 설정값으로 시작한다. Run 재화는 유지된다.
            Assert.That(b.CombatStateReadSink.Current.StageGold, Is.EqualTo(10));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
        }

        // 포기 완료(Abandoned·AlreadyCompleted·RunEnded) 경로는 스테이지 정의가 있는 CoreStageCombatCycleTests에서 검증한다.
        [Test]
        public void AbandonBattle_WithoutActiveBattle_ReportsNoActiveBattle()
        {
            var b = Create(out var service);
            b.BattleWriteSink.EndBattle();
            service.EndBattle();

            Assert.That(service.AbandonBattle(), Is.EqualTo(BattleAbandonResult.NoActiveBattle));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
        }

        [Test]
        public void AbandonBattle_UnknownPlacedArtifactDefinition_ThrowsAndChangesNothing()
        {
            var b = Create(out var service);
            // 픽스처 DB에는 Artifact 정의가 없다. 배치된 Artifact의 정의 조회 실패는 결과가 아니라 예외다.
            Assert.That(b.ArtifactInitializationWriteSink.TryInitialize(
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactState(4, 4,
                    Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactItemState>(),
                    new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactItemState(
                        777, 1, true, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) })), Is.True);

            Assert.Catch(() => service.AbandonBattle());

            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
        }

        private static IBufferQuery SecondPreparation(out ICombatProcessingService service, int stageGold = 10)
        {
            var b = Create(out service, stageGold);
            b.CombatViewWriteSink.Submit(CombatViewCommandData.EnterCombat());
            Assert.That(service.ProcessNextCommand(), Is.True);
            for (int i = 0; i < 20 && b.CombatStateReadSink.Current.CommandState.Turn.StagePhase == StagePhase.Combat; i++)
            {
                if (b.CombatStateReadSink.Phase == CombatFlowPhase.Automatic) Assert.That(service.AdvanceCombat(), Is.True);
                else { b.CombatViewWriteSink.Submit(CombatViewCommandData.EndUnitTurn()); Assert.That(service.ProcessNextCommand(), Is.True); }
            }
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.PreparationIndex, Is.EqualTo(2));
            return b;
        }

        [Test]
        public void CommandSequence_EntersCombat_AdvancesTurns_AndReturnsAtNightEnd()
        {
            var b = Create(out var service);
            var sub = b.CharacterReadSink.Characters.First(c => c.Area == CharacterRuntimeArea.Hand);
            Queue(CombatViewCommandData.PlaceUnit(sub.EntityId, new CellCoord(1, 1), Rotation4.R0));
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == sub.EntityId).Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Queue(CombatViewCommandData.EnterCombat());
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.MainPlacementLocked, Is.True);
            int command = b.CombatStateReadSink.HandledCommandNumber;
            for (int i = 0; i < 5; i++)
            {
                if (b.CombatStateReadSink.Phase == CombatFlowPhase.Automatic)
                {
                    int previousSequence = b.CombatStateReadSink.LatestResultSequence;
                    Assert.That(service.AdvanceCombat(), Is.True);
                    b.CombatResultReadSink.TryGetLatest(out var automatic);
                    Assert.That(automatic.Kind, Is.EqualTo(CombatProcessingKind.Advance));
                    Assert.That(automatic.CommandNumber, Is.Zero);
                    Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(command));
                    Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(previousSequence + 1));
                    Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
                }
                else if (b.CombatStateReadSink.Current.CommandState.Turn.StagePhase == StagePhase.Combat)
                {
                    Queue(CombatViewCommandData.EndUnitTurn());
                    command = b.CombatStateReadSink.HandledCommandNumber;
                }
                if (b.CombatStateReadSink.Current.CommandState.Turn.StagePhase == StagePhase.Deployment) break;
            }
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.PreparationIndex, Is.EqualTo(2));
            Assert.That(b.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == sub.EntityId).Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.Owner == Owner.Enemy).Area, Is.EqualTo(CharacterRuntimeArea.EnemyWaiting));
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main).Area, Is.EqualTo(CharacterRuntimeArea.Field));
            var idleState = b.CombatStateReadSink.Current;
            int idleSequence = b.CombatStateReadSink.LatestResultSequence;
            Assert.That(service.AdvanceCombat(), Is.False);
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(idleState));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(idleSequence));
            void Queue(CombatViewCommandData data)
            {
                int number = b.CombatViewWriteSink.Submit(data);
                Assert.That(service.ProcessNextCommand(), Is.True);
                b.CombatResultReadSink.TryGetLatest(out var result);
                Assert.That(result.Accepted, Is.True, result.Reason);
                b.CombatViewWriteSink.RemoveCommandsThrough(number);
            }
        }

        [Test]
        public void PlacementAndMovePreview_PublishResponseOnly()
        {
            var b = Create(out var service);
            var before = b.CombatStateReadSink.Current;
            var characters = b.CharacterReadSink.Characters.ToArray();
            var souls = b.CombatStateReadSink.SoulField;
            Assert.That(service.ProcessNextCommand(), Is.False);
            Assert.That(service.ProcessLatestPreview(), Is.False);
            Assert.That(service.AdvanceCombat(), Is.False);
            var sub = b.CharacterReadSink.Characters.First(c => c.Area == CharacterRuntimeArea.Hand);
            int number = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.UnitPlacement(sub.EntityId, new CellCoord(1, 1), Rotation4.R0));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            Assert.That(service.ProcessLatestPreview(), Is.False);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.RequestNumber, Is.EqualTo(number));
            Assert.That(preview.Placement.Value.CanPlace, Is.True);
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == sub.EntityId).Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CharacterReadSink.Characters, Is.EqualTo(characters));
            Assert.That(b.CombatStateReadSink.SoulField, Is.SameAs(souls));
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(b.CombatViewReadSink.HasPreview, Is.True);
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
        }

        [Test]
        public void InsufficientStageGold_RejectionKeepsAllGameplay_AndRunCurrencyDoesNotFundDraws()
        {
            var b = SecondPreparation(out var service, stageGold: 1);
            var before = b.CombatStateReadSink.Current;
            var ids = b.CharacterReadSink.Characters.Select(c => c.EntityId).ToArray();
            var characters = b.CharacterReadSink.Characters.ToArray();
            var soulField = b.CombatStateReadSink.SoulField;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            Assert.That(service.GetDrawAvailability().FailureReason.ToString(), Is.EqualTo("InsufficientGold"));
            int number = b.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
            Assert.That(service.ProcessNextCommand(), Is.True);
            var handled = b.CombatStateReadSink.Current;
            Assert.That(service.ProcessNextCommand(), Is.False);
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(handled));
            // 거부 결과 게시도 조회 객체를 재생성할 수 있으므로 참조 대신 게임 상태를 비교한다.
            foreach (var expected in characters)
            {
                var actual = b.CharacterReadSink.Characters.Single(c => c.EntityId == expected.EntityId);
                Assert.That(actual.Health, Is.EqualTo(expected.Health));
                Assert.That(actual.Stats.CurrentActionCost, Is.EqualTo(expected.Stats.CurrentActionCost));
                Assert.That(actual.OwnedId, Is.EqualTo(expected.OwnedId));
                Assert.That(actual.Area, Is.EqualTo(expected.Area));
                Assert.That(actual.ReserveSlotIndex, Is.EqualTo(expected.ReserveSlotIndex));
                Assert.That(actual.Anchor, Is.EqualTo(expected.Anchor));
                Assert.That(actual.Rotation, Is.EqualTo(expected.Rotation));
                Assert.That(actual.OccupiedCells, Is.EqualTo(expected.OccupiedCells));
            }
            Assert.That(b.CombatStateReadSink.SoulField, Is.SameAs(soulField));
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(number));
            Assert.That(b.CombatViewReadSink.Commands.Any(c => c.Number == number), Is.True);
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
            Assert.That(b.CharacterReadSink.Characters.Select(c => c.EntityId), Is.EqualTo(ids));
            Assert.That(b.CombatStateReadSink.Current.StageGold, Is.EqualTo(1));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            Assert.That(b.CombatStateReadSink.Current.DrawCursor, Is.EqualTo(before.DrawCursor));
            Assert.That(b.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(before.NextEntityId));
            b.CombatResultReadSink.TryGetLatest(out var rejected);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.CommandNumber, Is.EqualTo(number));
            // 두 재화는 서로 교환되지 않는다. Run 재화가 많아도 드로우는 여전히 골드 부족이다.
            b.CurrencyWriteSink.SetAmount(1000);
            Assert.That(service.GetDrawAvailability().FailureReason.ToString(), Is.EqualTo("InsufficientGold"));
            Assert.That(before.DrawCursor, Is.EqualTo(3));
        }

        [Test]
        public void RepeatedNights_PreserveMainHealthPositionAndLock_AndEnemyDeploymentOrigins()
        {
            var b = Create(out var service);
            var main = b.CharacterReadSink.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var enemy = b.CharacterReadSink.Characters.Single(c => c.Owner == Owner.Enemy);
            var hq = b.CombatStateReadSink.Current.Structures.Single();
            for (int night = 0; night < 2; night++)
            {
                b.CombatViewWriteSink.Submit(CombatViewCommandData.EnterCombat());
                Assert.That(service.ProcessNextCommand(), Is.True);
                for (int step = 0; step < 20 && b.CombatStateReadSink.Current.CommandState.Turn.StagePhase == StagePhase.Combat; step++)
                {
                    if (b.CombatStateReadSink.Phase == CombatFlowPhase.Automatic)
                        Assert.That(service.AdvanceCombat(), Is.True);
                    else
                    {
                        b.CombatViewWriteSink.Submit(CombatViewCommandData.EndUnitTurn());
                        Assert.That(service.ProcessNextCommand(), Is.True);
                    }
                }
                var state = b.CombatStateReadSink.Current;
                Assert.That(state.CommandState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
                Assert.That(state.CommandState.Turn.PreparationIndex, Is.EqualTo(night + 2));
                Assert.That(state.CommandState.Turn.MainPlacementLocked, Is.True);
                var retained = b.CharacterReadSink.Characters.Single(c => c.EntityId == main.EntityId);
                Assert.That(retained.Health, Is.EqualTo(main.Health));
                Assert.That(retained.Anchor, Is.EqualTo(main.Anchor));
                Assert.That(retained.Area, Is.EqualTo(CharacterRuntimeArea.Field));
                Assert.That(state.Structures.Single().Health, Is.EqualTo(hq.Health));
                var origin = state.CommandState.Turn.EnemyOrigins.Single();
                Assert.That(origin.EntityId, Is.EqualTo(enemy.EntityId));
                Assert.That(origin.Anchor, Is.EqualTo(enemy.Anchor.Value));
                // 같은 스테이지의 국면 변경(밤 반복)에서 스테이지 골드는 다시 지급되거나 초기화되지 않는다.
                Assert.That(state.StageGold, Is.EqualTo(10));
                Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            }
        }

        [Test]
        public void InvalidDrawTimeline_InstallsNeitherCharacterCurrencyCursorNorResult()
        {
            var b = SecondPreparation(out _);
            var before = b.CombatStateReadSink.Current;
            var characters = b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            b.CharacterReadSink.TryGetMainState(out var main);
            var initialization = CharacterBattlePreparationTests.Initialization(withEnemies: true);
            var input = new CombatCommandInput(20, CombatCommandKind.DrawCharacter, EntityId.Empty, default,
                before.CommandState, characters, initialization.Units.Select(u => new CombatAttackProfile(u,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray(),
                main, b.CharacterReadSink.Owned, b.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => b.CharacterReadSink.GetSoulCount(o.Id)),
                b.CombatStateReadSink.SoulField, new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot(0,
                    Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactPlacedEffectSnapshot>()),
                battle: new CombatBattleFacts(before.ReserveCapacity, before.NightLength, before.TimelineSeed,
                    before.PlayerDeploymentDepth, before.EnemyDeploymentDepth, before.DrawOrder, before.DrawCursor, before.NextEntityId, 10, 2,
                    categories: initialization.Categories));
            var transition = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var sequence = b.CombatStateReadSink.LatestResultSequence;
            // 없는 참가자를 가리키는 타임라인은 드로우·금액·캐릭터 반영 전에 전체를 거절해야 한다.
            var invalidState = new CombatCommandState(before.BoardWidth, before.BoardHeight, before.Phase,
                EntityId.Empty, false, before.Structures, before.CommandState.ThornFields,
                new[] { ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot.ForUnit(new EntityId(999), 10, 0, false) },
                before.CommandState.Turn);
            var invalid = new CombatCommandTransition(input, true, "", transition.Characters, transition.Field,
                invalidState, transition.Outcome, draw: transition.Draw, events: transition.Events);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(invalid));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CombatStateReadSink.Current.StageGold, Is.EqualTo(10));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            Assert.That(b.CharacterReadSink.Characters.Select(c => c.EntityId), Is.EqualTo(characters.Select(c => c.EntityId)));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
        }

        [Test]
        public void CurrencyValidationAndIndependentBuffers_PreserveRejectedAndUnrelatedBalances()
        {
            var first = Create(out _);
            var second = Create(out _);
            first.CurrencyWriteSink.SetAmount(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => first.CurrencyWriteSink.SetAmount(-1));
            Assert.That(first.CurrencyReadSink.Amount, Is.Zero);
            first.CurrencyWriteSink.SetAmount(int.MaxValue);
            Assert.That(first.CurrencyReadSink.Amount, Is.EqualTo(int.MaxValue));
            Assert.That(second.CurrencyReadSink.Amount, Is.EqualTo(RunCurrency));
            first.BattleWriteSink.EndBattle();
            Assert.That(first.CurrencyReadSink.Amount, Is.EqualTo(int.MaxValue));
            Assert.That(second.CombatStateReadSink.HasBattle, Is.True);
        }

        /// <summary>전투 중 바뀌지 않아야 하는 Run 재화 값. 스테이지 골드(기본 10)와 구분하려고 다른 값을 쓴다.</summary>
        private const int RunCurrency = 50;

        internal static IBufferQuery Create(out ICombatProcessingService service, int stageGold = 10)
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.EndBattle();
            var source = CharacterBattlePreparationTests.Initialization(withEnemies: true);
            var stage = new StageConfig(8, 6, 8, 2, 2, 42, source.Stage.InitialEnemyPlacements.ToArray(),
                Array.Empty<CellCoord>(), Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(),
                1, source.Stage.InitialEnemyStructurePlacements.ToArray());
            var definition = new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, source.Units, source.Structures,
                source.Categories, source.Units.Select(u => new CombatAttackProfile(u, new TargetingMethodSpec(TargetingShape.RadialSquare,
                    TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray(), new StageEconomySpec(stageGold, 2));
            var root = new SystemBuilderHelper();
            root.AddService<IBufferQuery>(b);
            var group = InGameCoreSystemFactory.Create(new Source(definition));
            group.ExportServices(root);
            var resolver = root.BuildSystemReader().ServiceStoreReader;
            ((ISystemInitData)group).InitData(new CharacterRosterDatabase());
            ((ISystemPostBindService)group).PostBindService(resolver);
            var core = resolver.GetService(InGameSystemGroupServiceKey.Core);
            // 초기 메인은 기존 Buffer 픽스처를 사용하고, 추가 보유·드로우는 등록된 로스터 서비스로 검증한다.
            for (int i = 0; i < 4; i++)
                Assert.That(core.Characters.AcquireSub(EntityKey.Unit_Akimbo).Succeeded, Is.True);
            service = core.Combat;
            service.PrepareBattle();
            b.CurrencyWriteSink.SetAmount(RunCurrency);
            return b;
        }
        private sealed class Source : IBattleInitializationSource
        {
            private readonly BattleInitialization _definition;
            internal Source(BattleInitialization definition) { _definition = definition; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey) => _definition;
        }
    }
}
