using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// View 계획 E.1·E.2·E.3·E.6: 입력 해석, 시작/턴 종료 단일 대기, Drag·조준, 미리보기 요청 규칙.
    /// 실제 CombatViewBuffer의 번호·목록으로 검증하고, 집기·표시는 가짜로 대신한다.
    /// </summary>
    public sealed class CombatInteractionTests
    {
        private static readonly EntityId Main = new(1);
        private static readonly EntityId FieldSub = new(2);
        private static readonly EntityId HandSub = new(3);
        private static readonly EntityId Enemy = new(9);

        private ICombatViewBufferReadSink _read;
        private ICombatViewBufferWriteSink _write;
        private FakePicker _picker;
        private FakeFeedback _feedback;
        private CombatInteraction _interaction;
        private CombatCommandGate _gate;
        private CombatInputRouter _router;
        private CombatInputFacts _facts;
        private List<CharacterData> _characters;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            IBufferQuery buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _read = buffers.CombatViewReadSink;
            _write = buffers.CombatViewWriteSink;
            _picker = new FakePicker();
            _feedback = new FakeFeedback();
            _gate = new CombatCommandGate(_write);
            _interaction = new CombatInteraction(_write, _read, _picker, _feedback, _gate);
            _router = new CombatInputRouter(_interaction, _gate, includeUnhandledCommand => _facts);
            _characters = new List<CharacterData>
            {
                FieldUnit(Main, EntityKey.Unit_MainCharacter, Owner.Player, new CellCoord(3, 0), main: true),
                FieldUnit(FieldSub, EntityKey.Unit_Akimbo, Owner.Player, new CellCoord(1, 1)),
                HandUnit(HandSub, slot: 0),
                FieldUnit(Enemy, EntityKey.Unit_Demon, Owner.Enemy, new CellCoord(3, 5))
            };
            _picker.HandSlots[0] = HandSub;
        }

        #region 단일 대기 (§7.2)

        [Test]
        public void WhilePending_DrawPreviewAndCancel_DoNotRecord_AndPendingStays()
        {
            Input(CombatInputRouter.Request.EnterCombat, Deployment());
            int pending = _gate.Pending.Value.Number;

            Input(CombatInputRouter.Request.Draw, Deployment());
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), HandX(0), HandY);
            Input(CombatInputRouter.Request.PointerMoved, Deployment(), CellX(2), CellY(1));
            Input(CombatInputRouter.Request.Cancel, _facts);
            Input(CombatInputRouter.Request.SelectCanceled, _facts);

            Assert.That(_read.Commands.Count, Is.EqualTo(1));
            Assert.That(_read.HasPreview, Is.False);
            Assert.That(_interaction.Drag, Is.EqualTo(CombatInteraction.DragKind.None));
            Assert.That(_gate.Pending.Value.Number, Is.EqualTo(pending), "취소로 대기를 지우지 않는다.");
        }

        // 대기 해제(Resolve) 사례는 원본 소유자 CombatCommandGateTests로, Stop 시 대기 해제는
        // 소유자 CombatViewSystem의 StopInput 사례(CombatViewSystemTests)로 옮겼다(구조 개선 VR1).

        [Test]
        public void CancelPresentationAndStop_DoNotClearGatePending()
        {
            Input(CombatInputRouter.Request.EnterCombat, Deployment());

            _interaction.CancelPresentation();
            Assert.That(_gate.HasPending, Is.True);

            _interaction.Stop();
            Assert.That(_gate.HasPending, Is.True, "대기의 정리는 Gate 소유자(CombatViewSystem)가 한다.");
            Assert.That(_read.Commands.Count, Is.EqualTo(1));
        }

        #endregion

        #region 입력 시점 차단 (§7.1)

        [Test]
        public void DuringNormalPresentation_PreviewIsAllowed_ButSubmitIsNot()
        {
            CombatInputFacts presenting = Combat(Main, latest: 1, presented: 0);

            Input(CombatInputRouter.Request.AttackMode, presenting);
            AnswerAttackRange(presenting, AttackAim.ForCell(new CellCoord(3, 1)));
            Input(CombatInputRouter.Request.PointerMoved, presenting, CellX(3), CellY(1));
            Input(CombatInputRouter.Request.SelectReleased, presenting, CellX(3), CellY(1));

            Assert.That(_read.HasPreview, Is.True);
            Assert.That(_read.Preview.Data.Kind, Is.EqualTo(CombatViewPreviewKind.Attack));
            Assert.That(_read.Commands, Is.Empty);
        }

        [Test]
        public void DuringFinalization_PreviewAndSubmit_AreBlocked()
        {
            CombatInputFacts finalizing = Combat(Main, finalizing: true);

            Input(CombatInputRouter.Request.MoveMode, finalizing);
            Input(CombatInputRouter.Request.EndTurn, finalizing);

            Assert.That(_read.HasPreview, Is.False);
            Assert.That(_read.Commands, Is.Empty);
            Assert.That(_interaction.Mode, Is.EqualTo(CombatInteraction.TargetMode.None));
        }

        [Test]
        public void Terminal_BlocksEverything_ButCancelStillWorks()
        {
            CombatInputFacts finished = Deployment(phase: CombatFlowPhase.Finished);

            Input(CombatInputRouter.Request.EnterCombat, finished);
            Input(CombatInputRouter.Request.SelectPressed, finished, HandX(0), HandY);
            Input(CombatInputRouter.Request.Cancel, _facts);

            Assert.That(_read.Commands, Is.Empty);
            Assert.That(_feedback.GhostShownCount, Is.Zero);
        }

        #endregion

        #region 배치 Drag (E.2·E.3)

        [Test]
        public void HandDrag_ToCell_RecordsPlaceUnit_AndRestoresDisplay()
        {
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), HandX(0), HandY);
            Assert.That(_feedback.GhostKey, Is.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(_feedback.HiddenId, Is.EqualTo(HandSub));

            Input(CombatInputRouter.Request.PointerMoved, Deployment(), CellX(2), CellY(1));
            Input(CombatInputRouter.Request.SelectReleased, Deployment(), CellX(2), CellY(1));

            CombatViewCommandData placed = _read.Commands.Single().Data;
            Assert.That(placed.Kind, Is.EqualTo(CombatViewCommandKind.PlaceUnit));
            Assert.That(placed.EntityId, Is.EqualTo(HandSub));
            Assert.That(placed.Cell, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(_feedback.GhostKey, Is.Null, "놓은 뒤 고스트를 걷는다.");
            Assert.That(_feedback.HiddenId, Is.EqualTo(EntityId.Empty), "원본 숨김을 푼다.");
            Assert.That(_read.HasPreview, Is.False);
        }

        [Test]
        public void Drag_RequestsPlacementPreview_OnlyWhenCellChanges()
        {
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), HandX(0), HandY);
            Input(CombatInputRouter.Request.PointerMoved, Deployment(), CellX(2), CellY(1));
            int first = _read.Preview.Number;

            Input(CombatInputRouter.Request.PointerMoved, Deployment(), CellX(2) + 1f, CellY(1) + 1f);
            Assert.That(_read.Preview.Number, Is.EqualTo(first), "같은 칸이면 다시 요청하지 않는다.");

            Input(CombatInputRouter.Request.PointerMoved, Deployment(), CellX(4), CellY(1));
            Assert.That(_read.Preview.Number, Is.GreaterThan(first));
            Assert.That(_read.Preview.Data.Kind, Is.EqualTo(CombatViewPreviewKind.UnitPlacement));
            Assert.That(_read.Preview.Data.Cell, Is.EqualTo(new CellCoord(4, 1)));
        }

        [Test]
        public void SelectCanceled_DuringDrag_RecordsNothing_AndRestoresDisplay()
        {
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), HandX(0), HandY);
            Input(CombatInputRouter.Request.PointerMoved, Deployment(), CellX(2), CellY(1));

            Input(CombatInputRouter.Request.SelectCanceled, _facts);

            Assert.That(_read.Commands, Is.Empty);
            Assert.That(_feedback.GhostKey, Is.Null);
            Assert.That(_feedback.HiddenId, Is.EqualTo(EntityId.Empty));
            Assert.That(_read.HasPreview, Is.False);
        }

        [Test]
        public void FieldUnit_DroppedOnHand_RecordsReturnUnit_ButMainCannotReturn()
        {
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), CellX(1), CellY(1));
            Input(CombatInputRouter.Request.SelectReleased, Deployment(), 30f, DropY);

            Input(CombatInputRouter.Request.SelectPressed, Deployment(), CellX(3), CellY(0));
            Input(CombatInputRouter.Request.SelectReleased, Deployment(), 30f, DropY);

            CombatViewCommandData returned = _read.Commands.Single().Data;
            Assert.That(returned.Kind, Is.EqualTo(CombatViewCommandKind.ReturnUnit));
            Assert.That(returned.EntityId, Is.EqualTo(FieldSub));
        }

        [Test]
        public void MainCharacter_DraggedOnField_RecordsPlaceMainCharacter()
        {
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), CellX(3), CellY(0));
            Input(CombatInputRouter.Request.SelectReleased, Deployment(), CellX(4), CellY(0));

            CombatViewCommandData placed = _read.Commands.Single().Data;
            Assert.That(placed.Kind, Is.EqualTo(CombatViewCommandKind.PlaceMainCharacter));
            Assert.That(placed.Cell, Is.EqualTo(new CellCoord(4, 0)));
        }

        [Test]
        public void ClickToPick_ThenClickCell_Places()
        {
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), HandX(0), HandY);
            Input(CombatInputRouter.Request.SelectReleased, Deployment(), HandX(0) + 1f, HandY);
            Assert.That(_read.Commands, Is.Empty, "제자리 클릭은 집기만 한다.");
            Assert.That(_interaction.Drag, Is.EqualTo(CombatInteraction.DragKind.Placement));

            Input(CombatInputRouter.Request.SelectPressed, Deployment(), CellX(5), CellY(1));
            Input(CombatInputRouter.Request.SelectReleased, Deployment(), CellX(5), CellY(1));

            Assert.That(_read.Commands.Single().Data.Cell, Is.EqualTo(new CellCoord(5, 1)));
        }

        #endregion

        #region 조준 모드 (E.3)

        [Test]
        public void MoveMode_DoesNotRecordEndpointCommands()
        {
            Input(CombatInputRouter.Request.MoveMode, Combat(Main));
            Input(CombatInputRouter.Request.PointerMoved, Combat(Main), CellX(3), CellY(2));
            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(3), CellY(2));
            Assert.That(_read.Commands, Is.Empty);
            Assert.That(_read.HasPreview, Is.False, "경로 입력은 CombatPathInput이 실제 Core 프리뷰와 함께 처리한다.");
        }

        [Test]
        public void AttackMode_Single_UsesCellAimFromRangeAnswer_EvenOnOccupiedCell()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            AnswerAttackRange(Combat(Main), AttackAim.ForCell(new CellCoord(3, 5)), AttackAim.ForCell(new CellCoord(3, 4)));

            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(6), CellY(4));
            Assert.That(_read.Commands, Is.Empty, "목록에 없는 칸은 기록하지 않는다.");

            // 적이 있는 칸이어도 Single은 칸 조준이다(개체 조준은 새 Core가 받지 않음, §21).
            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(3), CellY(5));
            AttackAim aim = _read.Commands.Single().Data.Aim;
            Assert.That(aim.Kind, Is.EqualTo(AttackAimKind.Cell));
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(3, 5)));
        }

        [TestCase(3, 4)]
        [TestCase(1, 1)]
        public void AttackMode_NonCannonRangeAnswer_ExcludesInvalidCellAim_AndSubmitsNothing(int x, int y)
        {
            CellCoord invalidCell = new(x, y);
            CellCoord hostileCell = new(3, 5);
            CombatInputFacts facts = Combat(Main);
            Input(CombatInputRouter.Request.AttackMode, facts);
            int rangeRequest = _read.Preview.Number;

            // 빈 칸과 아군 칸은 기하학적 범위에 남지만, 비포격 Core 응답의 조준 목록에서는 제외된다.
            _interaction.PreviewAnswered(WithAnswer(facts, CombatPreviewResult.ForAttackRange(rangeRequest,
                AttackRangeResult.Success(Main, new[] { invalidCell, hostileCell }, new[] { Enemy }, true,
                    new[] { AttackAim.ForCell(hostileCell) }))), _characters);

            Assert.That(_interaction.HasRangeAnswer, Is.True);
            Assert.That(_feedback.RangeCells, Does.Contain(invalidCell));
            Input(CombatInputRouter.Request.PointerMoved, facts, CellX(x), CellY(y));
            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Invalid));
            Assert.That(_read.Preview.Number, Is.EqualTo(rangeRequest), "조준 목록에 없는 칸은 공격 미리보기를 요청하지 않는다.");

            Input(CombatInputRouter.Request.SelectPressed, facts, CellX(x), CellY(y));
            Input(CombatInputRouter.Request.SelectReleased, facts, CellX(x), CellY(y));

            Assert.That(_read.Commands, Is.Empty);
        }

        [Test]
        public void AttackMode_CannonRangeAnswer_IncludesLegalEmptyCellAim_AndSubmitsAttack()
        {
            CellCoord emptyCell = new(3, 4);
            CombatInputFacts facts = Combat(Main);
            Input(CombatInputRouter.Request.AttackMode, facts);

            // 포격의 빈 칸 허용은 Core가 조준 목록으로 전달하며 View는 범주나 점유를 판정하지 않는다.
            _interaction.PreviewAnswered(WithAnswer(facts, CombatPreviewResult.ForAttackRange(_read.Preview.Number,
                AttackRangeResult.Success(Main, new[] { emptyCell }, Array.Empty<EntityId>(), true,
                    new[] { AttackAim.ForCell(emptyCell) }))), _characters);

            Input(CombatInputRouter.Request.PointerMoved, facts, CellX(3), CellY(4));
            Assert.That(_read.Preview.Data.Kind, Is.EqualTo(CombatViewPreviewKind.Attack));
            Input(CombatInputRouter.Request.SelectPressed, facts, CellX(3), CellY(4));
            Input(CombatInputRouter.Request.SelectReleased, facts, CellX(3), CellY(4));

            CombatViewCommandData attack = _read.Commands.Single().Data;
            Assert.That(attack.Kind, Is.EqualTo(CombatViewCommandKind.Attack));
            Assert.That(attack.EntityId, Is.EqualTo(Main));
            Assert.That(attack.Aim.Kind, Is.EqualTo(AttackAimKind.Cell));
            Assert.That(attack.Aim.Cell, Is.EqualTo(emptyCell));
        }

        [Test]
        public void AttackMode_Direction_PicksNearestListedDirectionFromActor()
        {
            // Main은 (3,0). Up = +Y(Core 정의).
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            AnswerAttackRange(Combat(Main),
                AttackAim.ForDirection(AttackDirection.Up), AttackAim.ForDirection(AttackDirection.Right));

            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(3), CellY(4));
            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(7), CellY(1));

            CombatViewCommand[] attacks = _read.Commands.ToArray();
            Assert.That(attacks[0].Data.Aim.Kind, Is.EqualTo(AttackAimKind.Direction));
            Assert.That(attacks[0].Data.Aim.Direction, Is.EqualTo(AttackDirection.Up));
            Assert.That(attacks[1].Data.Aim.Direction, Is.EqualTo(AttackDirection.Right));
        }

        [TestCase("base-empty", false)]
        [TestCase("base-ally", false)]
        [TestCase("base-enemy", true)]
        [TestCase("base-obstacle", true)]
        [TestCase("cannon-empty", true)]
        [TestCase("cannon-ally", true)]
        [TestCase("cannon-near", false)]
        [TestCase("shotgun-mixed", true)]
        public void ActualCorePolicy_RangeHoverPreviewAndSubmissionAgree(string scenario, bool accepted)
        {
            var input = PolicySmokeInput(scenario);
            _characters = input.Characters.Select(c => new CharacterData(c)).ToList();
            var facts = Combat(input.ActorId);
            var range = CombatCommandCalculator.GetAttackRange(input);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(range.Succeeded, Is.True);
            Assert.That(range.AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(accepted));
            Assert.That(preview.CanAttack, Is.EqualTo(accepted));

            Input(CombatInputRouter.Request.AttackMode, facts);
            int rangeRequest = _read.Preview.Number;
            _interaction.PreviewAnswered(WithAnswer(facts, CombatPreviewResult.ForAttackRange(rangeRequest, range)), _characters);
            var hovered = input.Aim.Cell ?? new CellCoord(4, 1);
            Input(CombatInputRouter.Request.PointerMoved, facts, CellX(hovered.X), CellY(hovered.Y));
            if (accepted)
            {
                Assert.That(_read.Preview.Data.Kind, Is.EqualTo(CombatViewPreviewKind.Attack));
                Assert.That(_read.Preview.Data.Aim, Is.EqualTo(input.Aim));
                _interaction.PreviewAnswered(WithAnswer(facts, CombatPreviewResult.ForAttack(_read.Preview.Number, preview)), _characters);
                Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Valid));
                Assert.That(_feedback.AffectedCells, Is.EqualTo(preview.AffectedCells.ToArray()));
            }
            else
            {
                Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Invalid));
                Assert.That(_read.Preview.Number, Is.EqualTo(rangeRequest));
            }
            Input(CombatInputRouter.Request.SelectPressed, facts, CellX(hovered.X), CellY(hovered.Y));
            Input(CombatInputRouter.Request.SelectReleased, facts, CellX(hovered.X), CellY(hovered.Y));

            // View가 실제 Core 조준 목록으로 제출한 명령을 다시 같은 계산 경로에 넣는다.
            var commandInput = input;
            if (accepted)
            {
                var submitted = _read.Commands.Single().Data;
                commandInput = new CombatCommandInput(input.Number, input.Kind, submitted.EntityId, submitted.Aim,
                    input.State, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls,
                    input.Artifacts, battleInstanceId: input.BattleInstanceId, environmentBindings: input.EnvironmentBindings,
                    obstacleContents: input.ObstacleContents);
            }
            var result = CombatCommandCalculator.Calculate(commandInput, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.EqualTo(accepted), result.Reason);
            if (!accepted)
            {
                Assert.That(_read.Commands, Is.Empty);
                Assert.That(result.Characters.Characters, Is.Empty);
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.FinalState, Is.SameAs(input.State));
                Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).Stats.CurrentActionCost, Is.EqualTo(5));
                return;
            }
            Assert.That(_read.Commands.Single().Data.Kind, Is.EqualTo(CombatViewCommandKind.Attack));
            Assert.That(_read.Commands.Single().Data.EntityId, Is.EqualTo(input.ActorId));
            long[] targets = scenario == "cannon-empty" ? Array.Empty<long>()
                : scenario == "base-obstacle" ? new[] { 20L }
                : scenario == "cannon-ally" ? new[] { (long)HandSub.Value } : new[] { (long)Enemy.Value };
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => (long)h.TargetId.Value), Is.EqualTo(targets));
            Assert.That(preview.Targets.ToArray().Select(t => (t.TargetId, t.TargetHealthAfter)),
                Is.EqualTo(result.Attack.Value.Hits.ToArray().Select(h => (h.TargetId, h.TargetHealthAfter))));
            Assert.That(result.Attack.Value.Hits.ToArray().All(h => h.DamageDealt == 20), Is.True);
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Stats.CurrentActionCost, Is.EqualTo(3));
            foreach (var ally in input.Characters.Where(c => c.Owner == Owner.Player && c.EntityId != input.ActorId))
            {
                var final = result.Characters.Characters.FirstOrDefault(c => c.Final.EntityId == ally.EntityId)?.Final ?? ally;
                Assert.That(final.Health.CurrentHealth, Is.EqualTo(scenario == "cannon-ally" && ally.EntityId == HandSub ? 80 : 100));
            }
        }

        [Test]
        public void AttackMode_All_RecordsAllAimOnAnyCell()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            AnswerAttackRange(Combat(Main), AttackAim.ForAll());

            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(0), CellY(6));

            Assert.That(_read.Commands.Single().Data.Aim.Kind, Is.EqualTo(AttackAimKind.All));
        }

        [Test]
        public void AttackMode_BeforeRangeAnswer_OrRejectedRange_RecordsNothing()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(3), CellY(5));
            Assert.That(_read.Commands, Is.Empty);

            CombatPreviewResult rejected = CombatPreviewResult.ForAttackRange(_read.Preview.Number,
                AttackRangeResult.Failure(AttackFailureReason.ActionCostInsufficient, Main));
            _interaction.PreviewAnswered(WithAnswer(Combat(Main), rejected), _characters);
            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(3), CellY(5));

            Assert.That(_read.Commands, Is.Empty);
            Assert.That(_feedback.RangeCells, Is.Empty);
        }

        [Test]
        public void AttackMode_CannotAfford_ShowsRangeAsInvalid_AndSubmitsNothing()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            AnswerAttackRange(Combat(Main), canAfford: false, AttackAim.ForCell(new CellCoord(3, 5)));
            int rangeRequest = _read.Preview.Number;

            // 범위는 보여주되, 조준 가능한 칸도 미리보기 요청 없이 바로 불가로 표시한다.
            Input(CombatInputRouter.Request.PointerMoved, Combat(Main), CellX(3), CellY(5));
            Assert.That(_feedback.RangeCells, Is.EquivalentTo(new[] { new CellCoord(3, 5) }));
            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Invalid));
            Assert.That(_read.Preview.Number, Is.EqualTo(rangeRequest), "AP 부족이면 호버 미리보기를 요청하지 않는다.");

            // Core가 반드시 거절할 명령은 제출하지 않는다.
            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(3), CellY(5));
            Assert.That(_read.Commands, Is.Empty);
        }

        [Test]
        public void AttackMode_CanAffordAgainAfterResult_SubmitsWithNewRangeAnswer()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            AnswerAttackRange(Combat(Main), canAfford: false, AttackAim.ForCell(new CellCoord(3, 5)));

            // 결과 뒤 범위를 다시 요청하고, 새 응답의 판정을 따른다.
            _interaction.AfterResult(Combat(Main), _characters);
            AnswerAttackRange(Combat(Main), canAfford: true, AttackAim.ForCell(new CellCoord(3, 5)));
            Input(CombatInputRouter.Request.SelectReleased, Combat(Main), CellX(3), CellY(5));

            Assert.That(_read.Commands.Single().Data.Aim.Cell, Is.EqualTo(new CellCoord(3, 5)));
        }

        [Test]
        public void StaleAnswer_ForEarlierRequest_IsIgnored()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            int stale = _read.Preview.Number;
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));

            _interaction.PreviewAnswered(WithAnswer(Combat(Main), CombatPreviewResult.ForAttackRange(stale,
                AttackRangeResult.Success(Main, new[] { new CellCoord(3, 5) }, Array.Empty<EntityId>(), true,
                    new[] { AttackAim.ForCell(new CellCoord(3, 5)) }))), _characters);

            Assert.That(_interaction.HasRangeAnswer, Is.False);
        }

        [Test]
        public void HoverAnswer_ShowsValidity_AndPlacementTintsGhost()
        {
            Input(CombatInputRouter.Request.SelectPressed, Deployment(), HandX(0), HandY);
            Input(CombatInputRouter.Request.PointerMoved, Deployment(), CellX(2), CellY(1));
            int request = _read.Preview.Number;

            _interaction.PreviewAnswered(WithAnswer(Deployment(),
                CombatPreviewResult.ForPlacement(request, new CombatPlacementPreview(false, new[] { new CellCoord(2, 1) }))), _characters);

            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Invalid));
            Assert.That(_feedback.GhostPlaceable, Is.False);
        }

        [Test]
        public void AttackHoverAnswer_ShowsAffectedCells()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            AnswerAttackRange(Combat(Main), AttackAim.ForCell(new CellCoord(3, 5)));
            Input(CombatInputRouter.Request.PointerMoved, Combat(Main), CellX(3), CellY(5));
            Assert.That(_read.Preview.Data.Kind, Is.EqualTo(CombatViewPreviewKind.Attack));

            _interaction.PreviewAnswered(WithAnswer(Combat(Main), CombatPreviewResult.ForAttack(_read.Preview.Number,
                AttackPreview.Failure(AttackFailureReason.ActionCostInsufficient, Main, EntityId.Empty))), _characters);

            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Invalid));
            Assert.That(_feedback.AffectedCells, Is.Empty);
        }

        [Test]
        public void AfterResult_ActorChanged_ExitsMode_WithoutRequestingForOldActor()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            int before = _read.Preview.Number;

            _interaction.AfterResult(Combat(FieldSub), _characters);

            Assert.That(_interaction.Mode, Is.EqualTo(CombatInteraction.TargetMode.None));
            Assert.That(_read.HasPreview, Is.False, "이전 행위자로 다음 턴의 미리보기를 재요청하지 않는다.");
            Assert.That(before, Is.GreaterThan(0));
        }

        [Test]
        public void AfterResult_SameActor_InvalidatesAndReRequestsRange()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));
            int before = _read.Preview.Number;

            _interaction.AfterResult(Combat(Main), _characters);

            Assert.That(_read.HasPreview, Is.True);
            Assert.That(_read.Preview.Number, Is.GreaterThan(before), "새 번호로 다시 요청한다.");
            Assert.That(_read.Preview.Data.Kind, Is.EqualTo(CombatViewPreviewKind.AttackRange));
        }

        [Test]
        public void AfterResult_ToDeployment_DropsCombatMode()
        {
            Input(CombatInputRouter.Request.AttackMode, Combat(Main));

            _interaction.AfterResult(Deployment(), _characters);

            Assert.That(_interaction.Mode, Is.EqualTo(CombatInteraction.TargetMode.None));
            Assert.That(_read.HasPreview, Is.False);
        }

        #endregion

        #region RecordInputOnly (§8.1)

        [Test]
        public void RecordInputOnly_PressedPlayerUnit_BecomesAttackActor()
        {
            CombatInputFacts record = Record(StageInputLayers.Combat);

            // 실제 입력 경로: 기록 모드의 전투 Layer 포인터는 공격 모드 전에도 공격 소유자로 간다(View 계획 §7.48).
            Input(CombatInputRouter.Request.SelectPressed, record, CellX(1), CellY(1));
            Input(CombatInputRouter.Request.AttackMode, record);
            Assert.That(_interaction.HasRangeAnswer, Is.False, "기록 모드는 Core 응답 없이 빈 칸 조준을 기록한다.");
            Input(CombatInputRouter.Request.SelectReleased, record, CellX(1), CellY(3));

            CombatViewCommandData attack = _read.Commands.Single().Data;
            Assert.That(attack.Kind, Is.EqualTo(CombatViewCommandKind.Attack));
            Assert.That(attack.EntityId, Is.EqualTo(FieldSub));
            Assert.That(attack.Aim.Kind, Is.EqualTo(AttackAimKind.Cell));
            Assert.That(attack.Aim.Cell, Is.EqualTo(new CellCoord(1, 3)));
        }

        #endregion

        #region 도우미

        private static CombatCommandInput PolicySmokeInput(string scenario)
        {
            var category = scenario.StartsWith("cannon", StringComparison.Ordinal) ? CategoryKey.Cannon : CategoryKey.Base;
            bool direction = scenario == "shotgun-mixed";
            var cell = scenario == "cannon-near" ? new CellCoord(2, 2) : new CellCoord(4, 1);
            var method = new TargetingMethodSpec(direction ? TargetingShape.EightDirectionLine : TargetingShape.RadialSquare,
                direction ? TargetingChoiceMode.Direction : TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var characters = new List<CharacterCombatFacts> {
                PolicySmokeCharacter(FieldSub, direction ? EntityKey.Unit_ShotGun : EntityKey.Unit_Akimbo,
                    Owner.Player, new CellCoord(1, 1), category),
                PolicySmokeCharacter(Main, EntityKey.Unit_MainCharacter, Owner.Player, new CellCoord(7, 6), CategoryKey.Base) };
            if (scenario.EndsWith("ally", StringComparison.Ordinal) || direction)
                characters.Add(PolicySmokeCharacter(HandSub, EntityKey.Unit_Demon, Owner.Player,
                    direction ? new CellCoord(3, 1) : cell, CategoryKey.Base));
            if (scenario == "base-enemy" || direction)
                characters.Add(PolicySmokeCharacter(Enemy, EntityKey.Unit_Demon, Owner.Enemy, cell, CategoryKey.Base));
            var structures = scenario == "base-obstacle" ? new[] { new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), cell, Rotation4.R0, new[] { cell }, Owner.Neutral) } : Array.Empty<StructureCombatFacts>();
            var profiles = characters.GroupBy(c => c.CharacterKey).Select(g => {
                var c = g.First();
                var definition = new UnitSpec(c.CharacterKey, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100,
                    MoveMethod.LiftAndPlace, 4, 1, 4, c.Stats.Category.Key,
                    20, 5, 2, 0, 100, 10, true, Array.Empty<TraitSpec>());
                return new CombatAttackProfile(definition, c.EntityId == FieldSub ? method
                    : new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            }).ToArray();
            var state = new CombatCommandState(8, 7, CombatFlowPhase.PlayerInput, FieldSub, false, structures,
                Array.Empty<ThornFieldCellSnapshot>(), new[] { CombatTimelineEntrySnapshot.ForUnit(FieldSub, 100, 0, true) });
            return new CombatCommandInput(1, CombatCommandKind.Attack, FieldSub,
                direction ? AttackAim.ForDirection(AttackDirection.Right) : AttackAim.ForCell(cell), state, characters, profiles,
                new MainCharacterPersistentState(100, 100, 20, 5, 4, 1),
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0), SoulFieldSnapshot.Empty,
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()), battleInstanceId: 1,
                environmentBindings: new EnvironmentSemanticBindings(Array.Empty<long>(), Array.Empty<long>(), structures.Select(s => s.EntityId).ToArray()),
                obstacleContents: structures.Select(s => new ObstacleContentProfile(s.EntityId, ObstacleContentKind.PowderBarrel, 0)).ToArray());
        }

        private static CharacterCombatFacts PolicySmokeCharacter(EntityId id, EntityKey key, Owner owner, CellCoord cell, CategoryKey category)
        {
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 4, 1, 4, 20, 0, 5, 2, 4, 5, 100, 10,
                Array.Empty<TraitStatusSnapshot>(), CategorySnapshot.CreateStateless(category));
            return new CharacterCombatFacts(id, owner == Owner.Player ? new CharacterInstanceId(id.Value) : null, key, owner,
                owner == Owner.Player ? (id == Main ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub) : null,
                CharacterRuntimeArea.Field, new HealthStateSnapshot(100, 100), stats, null, cell, Rotation4.R0, new[] { cell }, cell, 0, 0);
        }

        private const float HandY = -50f;
        private const float DropY = -45f;

        private static float CellX(int x) => (x * 10f) + 5f;

        private static float CellY(int y) => (y * 10f) + 5f;

        private static float HandX(int slot) => (slot * 10f) + 5f;

        private CombatViewCommandKind[] Kinds() => _read.Commands.Select(c => c.Data.Kind).ToArray();

        /// <summary>
        /// 실제 입력 경로와 같이 Router를 거쳐 보낸다(구조 개선 VR2·VR3). Router가 읽는 사실은 이 호출의 facts이며,
        /// 경로 입력은 연결하지 않으므로 전투 Layer의 비공격 포인터는 아무 소유자에게도 가지 않는다.
        /// </summary>
        private void Input(CombatInputRouter.Request request, CombatInputFacts facts, float screenX = 0f, float screenY = 0f)
        {
            _facts = facts;
            _router.Route(request, screenX, screenY, _characters);
        }

        internal static CombatInputFacts Deployment(
            CombatFlowPhase phase = CombatFlowPhase.PlayerInput,
            StagePhase stagePhase = StagePhase.Deployment,
            InputLayer? layer = null)
        {
            return new CombatInputFacts(true, true, phase, stagePhase, false,
                CombatExecutionMode.Live, layer ?? StageInputLayers.Deployment);
        }

        internal static CombatInputFacts Combat(
            EntityId actor,
            CombatFlowPhase phase = CombatFlowPhase.PlayerInput,
            int latest = 0,
            int presented = 0,
            bool finalizing = false,
            bool hasUnhandledCommand = false)
        {
            return new CombatInputFacts(true, true, phase, StagePhase.Combat, false,
                CombatExecutionMode.Live, StageInputLayers.Combat, latest, presented, finalizing, actor, hasUnhandledCommand);
        }

        internal static CombatInputFacts Record(InputLayer layer)
        {
            return new CombatInputFacts(true, true, CombatFlowPhase.PlayerInput, StagePhase.Deployment, false,
                CombatExecutionMode.RecordInputOnly, layer);
        }

        private static CharacterData FieldUnit(EntityId id, EntityKey key, Owner owner, CellCoord cell, bool main = false)
        {
            CharacterCombatFacts source = owner == Owner.Enemy
                ? CharacterBufferTestData.EnemyFacts(id)
                : main
                    ? CharacterBufferTestData.MainFacts(new CharacterInstanceId(id.Value), CharacterBufferTestData.MainState(), entityId: id)
                    : CharacterBufferTestData.SubFacts(new CharacterInstanceId(id.Value), id);
            return new CharacterData(new CharacterCombatFacts(
                id, source.OwnedId, key, source.Owner, source.OwnershipRole, CharacterRuntimeArea.Field,
                source.Health, source.Stats, null, cell, Rotation4.R0, new[] { cell }, cell, 0, 0));
        }

        private static CharacterData HandUnit(EntityId id, int slot)
        {
            CharacterCombatFacts source = CharacterBufferTestData.SubFacts(new CharacterInstanceId(id.Value), id);
            return new CharacterData(new CharacterCombatFacts(
                id, source.OwnedId, EntityKey.Unit_Akimbo, source.Owner, source.OwnershipRole, CharacterRuntimeArea.Hand,
                source.Health, source.Stats, slot, null, null, Array.Empty<CellCoord>(), default, 0, 0));
        }

        /// <summary>
        /// 화면 좌표 규칙: y ≥ 0은 칸(10px 단위, 8×7), y = -50은 손패 슬롯(10px 단위), y ≤ -40은 손패 Drop 영역이다.
        /// </summary>
        internal sealed class FakePicker : ICombatPointerPicker
        {
            internal readonly Dictionary<int, EntityId> HandSlots = new();

            public bool TryPickCell(float screenX, float screenY, out CellCoord cell)
            {
                cell = default;
                if (screenX < 0f || screenY < 0f || screenX >= 80f || screenY >= 70f)
                    return false;

                cell = new CellCoord((int)(screenX / 10f), (int)(screenY / 10f));
                return true;
            }

            public bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId)
            {
                entityId = EntityId.Empty;
                return Math.Abs(screenY - HandY) < 0.01f && HandSlots.TryGetValue((int)(screenX / 10f), out entityId);
            }

            public bool IsOverHandDropZone(float screenX, float screenY) => screenY <= -40f;
        }

        internal sealed class FakeFeedback : ICombatInteractionFeedback
        {
            internal EntityKey? GhostKey;
            internal int GhostShownCount;
            internal EntityId HiddenId = EntityId.Empty;
            internal CellCoord? HoverCell;

            public void ShowGhost(EntityKey entityKey)
            {
                GhostKey = entityKey;
                GhostShownCount++;
            }

            public void MoveGhostToCell(CellCoord cell)
            {
            }

            public void MoveGhostToPointer(float screenX, float screenY)
            {
            }

            public void HideGhost() => GhostKey = null;

            public void HideOriginal(EntityId entityId) => HiddenId = entityId;

            public void ShowOriginal() => HiddenId = EntityId.Empty;

            internal CombatHoverState HoverState;
            internal bool GhostPlaceable = true;
            internal CellCoord[] RangeCells = Array.Empty<CellCoord>();
            internal CellCoord[] AffectedCells = Array.Empty<CellCoord>();
            internal AttackBlockedContact[] Blockers = Array.Empty<AttackBlockedContact>();
            internal EntityId CostActor;
            internal int? AttackCost;

            public void SetGhostPlaceable(bool placeable) => GhostPlaceable = placeable;

            public void SetHoverCell(CellCoord cell, CombatHoverState state)
            {
                HoverCell = cell;
                HoverState = state;
            }

            public void ClearHoverCell()
            {
                HoverCell = null;
                HoverState = CombatHoverState.Neutral;
            }

            public void ShowRangeCells(IReadOnlyList<CellCoord> cells) => RangeCells = cells.ToArray();

            public void ClearRangeCells() => RangeCells = Array.Empty<CellCoord>();

            public void ShowAffectedCells(IReadOnlyList<CellCoord> cells) => AffectedCells = cells.ToArray();

            public void ClearAffectedCells() { AffectedCells = Array.Empty<CellCoord>(); CostActor = EntityId.Empty; AttackCost = null; }
            public void ShowAttackCostPreview(EntityId actorId, int cost) { CostActor = actorId; AttackCost = cost; }
            public void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts) => Blockers = contacts.ToArray();
            public void ClearAttackBlockers() => Blockers = Array.Empty<AttackBlockedContact>();
        }

        /// <summary>Core 역할로 현재 요청(이동 범위)에 답한다.</summary>
        private void AnswerMoveRange(CombatInputFacts facts, params CellCoord[] anchors)
        {
            MovementDestination[] destinations = anchors.Select(a => new MovementDestination(a, new[] { a }, 1, 1)).ToArray();
            _interaction.PreviewAnswered(WithAnswer(facts, CombatPreviewResult.ForMoveRange(_read.Preview.Number,
                MovementRangeResult.Success(facts.ActiveActorId, destinations))), _characters);
        }

        /// <summary>Core 역할로 현재 요청(공격 범위)에 답한다. 조준 목록이 곧 공격 방식이다.</summary>
        private void AnswerAttackRange(CombatInputFacts facts, params AttackAim[] aims)
        {
            AnswerAttackRange(facts, canAfford: true, aims);
        }

        /// <summary>Core 역할로 공격 범위에 답하되 AP 판정(CanAffordAttack)을 지정한다.</summary>
        private void AnswerAttackRange(CombatInputFacts facts, bool canAfford, params AttackAim[] aims)
        {
            CellCoord[] cells = aims.Where(a => a.Cell.HasValue).Select(a => a.Cell.Value).ToArray();
            _interaction.PreviewAnswered(WithAnswer(facts, CombatPreviewResult.ForAttackRange(_read.Preview.Number,
                AttackRangeResult.Success(facts.ActiveActorId, cells, Array.Empty<EntityId>(), canAfford, aims))), _characters);
        }

        private static CombatInputFacts WithAnswer(CombatInputFacts f, CombatPreviewResult answer)
        {
            return new CombatInputFacts(f.IsBattleDisplayed, f.HasBattle, f.Phase, f.StagePhase, f.HasCompletion,
                f.ExecutionMode, f.CurrentLayer, f.LatestResultSequence, f.PresentedResultSequence, f.IsFinalizing,
                f.ActiveActorId, f.HasUnhandledCommand, answer);
        }

        #endregion
    }
}
