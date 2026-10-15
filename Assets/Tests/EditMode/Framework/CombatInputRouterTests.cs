using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 구조 개선 VR2: 입력 의도별 처리 순서와 포인터 대상 판정(CombatInputRouter).
    /// 시작·턴 종료·드로우 사례는 CombatInteractionTests에서 기대값 그대로 옮겼다.
    /// </summary>
    public sealed class CombatInputRouterTests
    {
        private static readonly EntityId Main = new(1);

        private ICombatViewBufferReadSink _read;
        private CombatCommandGate _gate;
        private CombatInputRouter _router;
        private CombatInputFacts _facts;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            IBufferQuery buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _read = buffers.CombatViewReadSink;
            _gate = new CombatCommandGate(buffers.CombatViewWriteSink);
            var interaction = new CombatInteraction(buffers.CombatViewWriteSink, _read,
                new CombatInteractionTests.FakePicker(), new CombatInteractionTests.FakeFeedback(), _gate);
            _router = new CombatInputRouter(interaction, _gate, includeUnhandledCommand => _facts);
        }

        #region 포인터 대상 (단일 판정식)

        [TestCase("deployment", false, "Placement")]
        [TestCase("deployment", true, "Placement")]
        [TestCase("combat", false, "Path")]
        [TestCase("combat", true, "Attack")]
        [TestCase("none", false, "None")]
        [TestCase("none", true, "None")]
        public void ResolvePointerTarget_UsesActualLayerThenAttackMode(string layer, bool attack, string expected)
        {
            Assert.That(CombatInputRouter.ResolvePointerTarget(LayerOf(layer), attack, CombatExecutionMode.Live).ToString(),
                Is.EqualTo(expected));
        }

        // 기록 모드는 경로 지정이 시작되지 않으므로 전투 Layer 포인터가 모드와 관계없이 공격 소유자로 간다(계획 §7.48).
        [TestCase("deployment", false, "Placement")]
        [TestCase("combat", false, "Attack")]
        [TestCase("combat", true, "Attack")]
        [TestCase("none", false, "None")]
        public void ResolvePointerTarget_RecordInputOnly_SendsCombatPointerToAttack(string layer, bool attack, string expected)
        {
            Assert.That(CombatInputRouter.ResolvePointerTarget(LayerOf(layer), attack, CombatExecutionMode.RecordInputOnly).ToString(),
                Is.EqualTo(expected));
        }

        private static InputLayer LayerOf(string layer)
        {
            return layer == "deployment" ? StageInputLayers.Deployment
                : layer == "combat" ? StageInputLayers.Combat
                : default;
        }

        #endregion

        #region 시작 · 턴 종료 · 드로우 (§7.2)

        [Test]
        public void EnterCombat_Twice_RecordsOneCommand_AndKeepsPending()
        {
            Input(CombatInputRouter.Request.EnterCombat, Deployment());
            Input(CombatInputRouter.Request.EnterCombat, Deployment());

            Assert.That(Kinds(), Is.EqualTo(new[] { CombatViewCommandKind.EnterCombat }));
            Assert.That(_gate.Pending.Value.Kind, Is.EqualTo(CombatViewCommandKind.EnterCombat));
            Assert.That(_gate.Pending.Value.Number, Is.EqualTo(_read.Commands[0].Number));
        }

        [Test]
        public void EndTurn_TwiceInOneFrame_RecordsOne()
        {
            Input(CombatInputRouter.Request.EndTurn, Combat(Main));
            Input(CombatInputRouter.Request.EndTurn, Combat(Main));

            Assert.That(Kinds(), Is.EqualTo(new[] { CombatViewCommandKind.EndUnitTurn }));
            Assert.That(_gate.HasPending, Is.True);
        }

        [Test]
        public void EndTurn_WithUnhandledEarlierCommand_RecordsNothing()
        {
            Input(CombatInputRouter.Request.EndTurn, Combat(Main, hasUnhandledCommand: true));

            Assert.That(_read.Commands, Is.Empty, "선행 Move/Attack이 남아 있으면 다음 행위자 턴이 끝날 수 있다(§7.1).");
            Assert.That(_gate.HasPending, Is.False);
        }

        [Test]
        public void EndTurn_WithoutActiveActor_RecordsNothing()
        {
            Input(CombatInputRouter.Request.EndTurn, Combat(EntityId.Empty));

            Assert.That(_read.Commands, Is.Empty);
        }

        [Test]
        public void Automatic_UnpresentedResult_OrWrongLayer_RecordNothing()
        {
            Input(CombatInputRouter.Request.EndTurn, Combat(Main, phase: CombatFlowPhase.Automatic));
            Input(CombatInputRouter.Request.EndTurn, Combat(Main, latest: 1, presented: 0));
            Input(CombatInputRouter.Request.Draw, Deployment(layer: StageInputLayers.Combat));
            Input(CombatInputRouter.Request.EnterCombat, Deployment(stagePhase: StagePhase.Combat));

            Assert.That(_read.Commands, Is.Empty);
        }

        [Test]
        public void RecordInputOnly_UsesActualLayer_ForEndTurnWithoutActor_ThenWaits()
        {
            // Core는 배치 단계(행위자 없음)지만 개발자가 고른 Combat Layer로 기록한다.
            CombatInputFacts record = Record(StageInputLayers.Combat);

            Input(CombatInputRouter.Request.EndTurn, record);
            Input(CombatInputRouter.Request.EndTurn, record);

            Assert.That(Kinds(), Is.EqualTo(new[] { CombatViewCommandKind.EndUnitTurn }));
            Assert.That(_gate.HasPending, Is.True);
        }
        [Test]
        public void Draw_SubmitsWithoutPending()
        {
            Input(CombatInputRouter.Request.Draw, CombatInteractionTests.Deployment());

            Assert.That(Kinds(), Is.EqualTo(new[] { CombatViewCommandKind.DrawCharacter }));
            Assert.That(_gate.HasPending, Is.False, "드로우는 대기를 만들지 않는다.");
        }

        #endregion

        #region 경로 입력과의 순서

        [Test]
        public void EndTurn_Rejected_StillCancelsPathFirst()
        {
            var rig = new CombatMovementFeedbackTests.Rig();
            var router = RouterFor(rig, CombatInteractionTests.Deployment());
            rig.Start(); rig.Move(rig.Up);
            Assert.That(rig.Display.Path, Is.Not.Null);

            router.Route(CombatInputRouter.Request.EndTurn, 0f, 0f, Array.Empty<CharacterData>());

            Assert.That(rig.Display.Path, Is.Null, "제출이 거절돼도 경로 지정은 먼저 취소된다.");
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void EndTurn_Accepted_CancelsPathAndRecordsPending()
        {
            var rig = new CombatMovementFeedbackTests.Rig();
            var gate = new CombatCommandGate(rig.B.CombatViewWriteSink);
            var router = RouterFor(rig, PathInputTestFacts.Read(rig.B, true), gate);
            rig.Start(); rig.Move(rig.Up);

            router.Route(CombatInputRouter.Request.EndTurn, 0f, 0f, Array.Empty<CharacterData>());

            Assert.That(rig.Display.Path, Is.Null);
            Assert.That(rig.B.CombatViewReadSink.Commands.Single().Data.Kind, Is.EqualTo(CombatViewCommandKind.EndUnitTurn));
            Assert.That(gate.HasPending, Is.True);
        }

        [Test]
        public void EnterCombat_DoesNotCancelPath()
        {
            var rig = new CombatMovementFeedbackTests.Rig();
            var router = RouterFor(rig, CombatInteractionTests.Deployment(phase: CombatFlowPhase.Automatic));
            rig.Start(); rig.Move(rig.Up);

            router.Route(CombatInputRouter.Request.EnterCombat, 0f, 0f, Array.Empty<CharacterData>());

            Assert.That(rig.Display.Path, Is.Not.Null, "시작 입력은 경로 취소를 추가하지 않는다.");
        }

        #endregion

        #region 도우미

        private CombatViewCommandKind[] Kinds() => _read.Commands.Select(c => c.Data.Kind).ToArray();

        private void Input(CombatInputRouter.Request request, CombatInputFacts facts)
        {
            _facts = facts;
            _router.Route(request, 0f, 0f, Array.Empty<CharacterData>());
        }

        private static CombatInputFacts Deployment(
            CombatFlowPhase phase = CombatFlowPhase.PlayerInput,
            StagePhase stagePhase = StagePhase.Deployment,
            InputLayer? layer = null) => CombatInteractionTests.Deployment(phase, stagePhase, layer);

        private static CombatInputFacts Combat(
            EntityId actor,
            CombatFlowPhase phase = CombatFlowPhase.PlayerInput,
            int latest = 0,
            int presented = 0,
            bool finalizing = false,
            bool hasUnhandledCommand = false) =>
            CombatInteractionTests.Combat(actor, phase, latest, presented, finalizing, hasUnhandledCommand);

        private static CombatInputFacts Record(InputLayer layer) => CombatInteractionTests.Record(layer);

        /// <summary>실제 경로 입력(이동 피드백 테스트의 Rig)을 연결한 Router입니다. Router가 읽는 사실은 고정값입니다.</summary>
        private static CombatInputRouter RouterFor(CombatMovementFeedbackTests.Rig rig, CombatInputFacts facts, CombatCommandGate gate = null)
        {
            gate ??= new CombatCommandGate(rig.B.CombatViewWriteSink);
            var interaction = new CombatInteraction(rig.B.CombatViewWriteSink, rig.B.CombatViewReadSink,
                new CombatInteractionTests.FakePicker(), new CombatInteractionTests.FakeFeedback(), gate);
            var router = new CombatInputRouter(interaction, gate, includeUnhandledCommand => facts);
            router.SetPathInput(rig.Input);
            return router;
        }

        #endregion
    }
}
