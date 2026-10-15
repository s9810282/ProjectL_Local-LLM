using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>View 계획 C.1a: 종료 판정과 목표 Layer(설계 §5.5·§5.6).</summary>
    public sealed class CombatInputPolicyTests
    {
        [TestCase(CombatFlowPhase.Finished, false, true)]
        [TestCase(CombatFlowPhase.PlayerInput, true, true)]
        [TestCase(CombatFlowPhase.Automatic, true, true)]
        [TestCase(CombatFlowPhase.PlayerInput, false, false)]
        [TestCase(CombatFlowPhase.Automatic, false, false)]
        [TestCase(CombatFlowPhase.None, false, false)]
        public void IsTerminal_UsesFinishedOrCompletionOnly(CombatFlowPhase phase, bool hasCompletion, bool expected)
        {
            Assert.That(CombatInputPolicy.IsTerminal(Facts(phase: phase, hasCompletion: hasCompletion)), Is.EqualTo(expected));
        }

        [Test]
        public void IsTerminal_IgnoresHasBattle_BecauseCoreKeepsBattleAfterEnd()
        {
            Assert.That(CombatInputPolicy.IsTerminal(Facts(hasBattle: true, phase: CombatFlowPhase.Finished)), Is.True);
            Assert.That(CombatInputPolicy.IsTerminal(Facts(hasBattle: false, phase: CombatFlowPhase.PlayerInput)), Is.False);
        }

        [Test]
        public void GetDesiredLayer_FollowsCoreStagePhase()
        {
            Assert.That(CombatInputPolicy.GetDesiredLayer(Facts(stagePhase: StagePhase.Deployment)),
                Is.EqualTo(StageInputLayers.Deployment));
            Assert.That(CombatInputPolicy.GetDesiredLayer(Facts(stagePhase: StagePhase.Combat)),
                Is.EqualTo(StageInputLayers.Combat));
        }

        [Test]
        public void GetDesiredLayer_KeepsCombatLayerDuringAutomatic()
        {
            // 자동 진행이라는 이유만으로 Layer를 바꾸지 않는다. 제출 차단은 매 입력의 정책이 한다(§5.5).
            Assert.That(CombatInputPolicy.GetDesiredLayer(
                    Facts(phase: CombatFlowPhase.Automatic, stagePhase: StagePhase.Combat)),
                Is.EqualTo(StageInputLayers.Combat));
        }

        [Test]
        public void GetDesiredLayer_IsNone_BeforeBattleOrFirstDisplay()
        {
            Assert.That(CombatInputPolicy.GetDesiredLayer(Facts(hasBattle: false)), Is.EqualTo(default(InputLayer)));
            Assert.That(CombatInputPolicy.GetDesiredLayer(Facts(isBattleDisplayed: false)), Is.EqualTo(default(InputLayer)));
        }

        [TestCase(CombatFlowPhase.Finished, false)]
        [TestCase(CombatFlowPhase.PlayerInput, true)]
        public void GetDesiredLayer_TerminalWinsOverStagePhase(CombatFlowPhase phase, bool hasCompletion)
        {
            Assert.That(CombatInputPolicy.GetDesiredLayer(
                    Facts(phase: phase, hasCompletion: hasCompletion, stagePhase: StagePhase.Deployment)),
                Is.EqualTo(default(InputLayer)));
        }

        #region 명령·미리보기 허용 (E.1a, 설계 §7.1)

        [Test]
        public void CanSubmit_Live_RequiresPlayerInputMatchingStageAndLayerAndPresented()
        {
            Assert.That(CombatInputPolicy.CanSubmit(Live(), false, CombatViewCommandKind.EnterCombat), Is.True);

            Assert.That(CombatInputPolicy.CanSubmit(Live(phase: CombatFlowPhase.Automatic), false, CombatViewCommandKind.EnterCombat), Is.False);
            Assert.That(CombatInputPolicy.CanSubmit(Live(latest: 1, presented: 0), false, CombatViewCommandKind.EnterCombat), Is.False, "S > P");
            Assert.That(CombatInputPolicy.CanSubmit(Live(layer: StageInputLayers.Combat), false, CombatViewCommandKind.EnterCombat), Is.False, "실제 Layer 불일치");
            Assert.That(CombatInputPolicy.CanSubmit(Live(stagePhase: StagePhase.Combat), false, CombatViewCommandKind.EnterCombat), Is.False, "Core 단계 불일치");
            Assert.That(CombatInputPolicy.CanSubmit(Live(), true, CombatViewCommandKind.EnterCombat), Is.False, "대기 중");
            Assert.That(CombatInputPolicy.CanSubmit(Live(finalizing: true), false, CombatViewCommandKind.EnterCombat), Is.False, "후처리 중");
            Assert.That(CombatInputPolicy.CanSubmit(Live(displayed: false), false, CombatViewCommandKind.EnterCombat), Is.False, "최초 표시 전");
            Assert.That(CombatInputPolicy.CanSubmit(Live(phase: CombatFlowPhase.Finished), false, CombatViewCommandKind.EnterCombat), Is.False, "종료");
        }

        [Test]
        public void CanSubmit_EndTurn_RequiresActorAndNoUnhandledCommand()
        {
            var actor = new EntityId(1);

            Assert.That(CombatInputPolicy.CanSubmit(LiveCombat(actor), false, CombatViewCommandKind.EndUnitTurn), Is.True);
            Assert.That(CombatInputPolicy.CanSubmit(LiveCombat(EntityId.Empty), false, CombatViewCommandKind.EndUnitTurn), Is.False);
            Assert.That(CombatInputPolicy.CanSubmit(LiveCombat(actor, unhandled: true), false, CombatViewCommandKind.EndUnitTurn), Is.False);
            Assert.That(CombatInputPolicy.CanSubmit(LiveCombat(actor, unhandled: true), false, CombatViewCommandKind.Move), Is.True,
                "선행 명령 조건은 턴 종료에만 적용한다.");
        }

        [Test]
        public void CanPreview_AllowsUnpresentedResult_ButNotFinalizingOrAutomatic()
        {
            var actor = new EntityId(1);

            Assert.That(CombatInputPolicy.CanPreview(LiveCombat(actor, latest: 2, presented: 1), false, CombatViewPreviewKind.MoveRange), Is.True);
            Assert.That(CombatInputPolicy.CanPreview(LiveCombat(actor, finalizing: true), false, CombatViewPreviewKind.MoveRange), Is.False);
            Assert.That(CombatInputPolicy.CanPreview(LiveCombat(actor, phase: CombatFlowPhase.Automatic), false, CombatViewPreviewKind.MoveRange), Is.False);
            Assert.That(CombatInputPolicy.CanPreview(LiveCombat(actor), true, CombatViewPreviewKind.MoveRange), Is.False, "대기 중");
            Assert.That(CombatInputPolicy.CanPreview(LiveCombat(actor), false, CombatViewPreviewKind.UnitPlacement), Is.False, "단계 불일치");
        }

        [Test]
        public void RecordInputOnly_UsesActualLayer_InsteadOfLiveConditions()
        {
            // Core는 배치 단계·행위자 없음이지만 개발자가 고른 Combat Layer로 기록한다(설계 §8.1).
            CombatInputFacts combatLayer = Record(StageInputLayers.Combat);

            Assert.That(CombatInputPolicy.CanSubmit(combatLayer, false, CombatViewCommandKind.EndUnitTurn), Is.True);
            Assert.That(CombatInputPolicy.CanSubmit(combatLayer, false, CombatViewCommandKind.EnterCombat), Is.False);
            Assert.That(CombatInputPolicy.CanSubmit(combatLayer, true, CombatViewCommandKind.EndUnitTurn), Is.False, "대기 검사는 유지한다.");
            Assert.That(CombatInputPolicy.CanPreview(combatLayer, false, CombatViewPreviewKind.MoveRange), Is.True);
        }

        [Test]
        public void CanChangeDevLayer_OnlyRecordInputOnly_WithoutPendingOrTerminal()
        {
            Assert.That(CombatInputPolicy.CanChangeDevLayer(Record(StageInputLayers.Deployment), false), Is.True);
            Assert.That(CombatInputPolicy.CanChangeDevLayer(Record(StageInputLayers.Deployment), true), Is.False);
            Assert.That(CombatInputPolicy.CanChangeDevLayer(Live(), false), Is.False);
            Assert.That(CombatInputPolicy.CanChangeDevLayer(
                new CombatInputFacts(true, true, CombatFlowPhase.Finished, StagePhase.Deployment, false,
                    CombatExecutionMode.RecordInputOnly, StageInputLayers.Deployment), false), Is.False);
        }

        // 구조 개선 VR1: 옛 AllowsPathInput·PresentationAllowsInput·PathInput.AllowsInput에 흩어져 있던 공통 조건을 한 곳에서 고정한다.
        [TestCase("ok", true)]
        [TestCase("pending", false)]
        [TestCase("presenting", false)]
        [TestCase("unhandled", false)]
        [TestCase("unpresentedResult", false)]
        [TestCase("finalizing", false)]
        [TestCase("automatic", false)]
        [TestCase("deploymentLayer", false)]
        [TestCase("deploymentStage", false)]
        [TestCase("finished", false)]
        [TestCase("recordCombatLayer", true)]
        [TestCase("recordPresenting", false)]
        public void CanEditPath_CombinesMoveSubmitWithPresentationAndQueue(string scenario, bool expected)
        {
            Assert.That(CombatInputPolicy.CanEditPath(PathFacts(scenario)), Is.EqualTo(expected), scenario);
        }

        [Test]
        public void CanEditPath_DoesNotUseInputAvailabilityOrTarget()
        {
            // 가용성·대상은 경로 입력 입구가 따로 확인한다. 정책은 이 값만으로 허용하지도 막지도 않는다.
            Assert.That(CombatInputPolicy.CanEditPath(PathFacts("ok", available: false, target: false)), Is.True);
        }

        [TestCase(CombatViewCommandKind.PlaceMainCharacter, StagePhase.Deployment)]
        [TestCase(CombatViewCommandKind.PlaceUnit, StagePhase.Deployment)]
        [TestCase(CombatViewCommandKind.ReturnUnit, StagePhase.Deployment)]
        [TestCase(CombatViewCommandKind.EnterCombat, StagePhase.Deployment)]
        [TestCase(CombatViewCommandKind.DrawCharacter, StagePhase.Deployment)]
        [TestCase(CombatViewCommandKind.Move, StagePhase.Combat)]
        [TestCase(CombatViewCommandKind.Attack, StagePhase.Combat)]
        [TestCase(CombatViewCommandKind.EndUnitTurn, StagePhase.Combat)]
        public void StageOf_MapsEveryCommand(CombatViewCommandKind kind, StagePhase expected)
        {
            Assert.That(CombatInputPolicy.StageOf(kind), Is.EqualTo(expected));
        }

        private static CombatInputFacts Live(
            CombatFlowPhase phase = CombatFlowPhase.PlayerInput,
            StagePhase stagePhase = StagePhase.Deployment,
            InputLayer? layer = null,
            int latest = 0,
            int presented = 0,
            bool finalizing = false,
            bool displayed = true)
        {
            return new CombatInputFacts(displayed, true, phase, stagePhase, false, CombatExecutionMode.Live,
                layer ?? StageInputLayers.Deployment, latest, presented, finalizing);
        }

        private static CombatInputFacts LiveCombat(
            EntityId actor,
            CombatFlowPhase phase = CombatFlowPhase.PlayerInput,
            int latest = 0,
            int presented = 0,
            bool finalizing = false,
            bool unhandled = false)
        {
            return new CombatInputFacts(true, true, phase, StagePhase.Combat, false, CombatExecutionMode.Live,
                StageInputLayers.Combat, latest, presented, finalizing, actor, unhandled);
        }

        private static CombatInputFacts Record(InputLayer layer)
        {
            return new CombatInputFacts(true, true, CombatFlowPhase.PlayerInput, StagePhase.Deployment, false,
                CombatExecutionMode.RecordInputOnly, layer);
        }

        /// <summary>경로 편집 판단용 사실입니다. "ok"는 Live 전투 단계에서 모든 조건을 통과하는 기준값입니다.</summary>
        private static CombatInputFacts PathFacts(string scenario, bool available = true, bool target = true)
        {
            bool record = scenario.StartsWith("record");
            return new CombatInputFacts(
                isBattleDisplayed: true,
                hasBattle: true,
                phase: scenario == "automatic" ? CombatFlowPhase.Automatic
                    : scenario == "finished" ? CombatFlowPhase.Finished
                    : CombatFlowPhase.PlayerInput,
                stagePhase: scenario == "deploymentStage" || record ? StagePhase.Deployment : StagePhase.Combat,
                hasCompletion: false,
                executionMode: record ? CombatExecutionMode.RecordInputOnly : CombatExecutionMode.Live,
                currentLayer: scenario == "deploymentLayer" ? StageInputLayers.Deployment : StageInputLayers.Combat,
                latestResultSequence: scenario == "unpresentedResult" ? 2 : 1,
                presentedResultSequence: 1,
                isFinalizing: scenario == "finalizing",
                activeActorId: new EntityId(1),
                hasUnhandledCommand: scenario == "unhandled",
                hasPendingCommand: scenario == "pending",
                isPresenting: scenario == "presenting" || scenario == "recordPresenting" || scenario == "unpresentedResult",
                isInputAvailable: available,
                isPathTarget: target);
        }

        #endregion

        #region 포기 버튼 (포기 UI 설계 C2)

        [TestCase("ok", true, true)]
        [TestCase("deploymentStage", true, true)]
        [TestCase("pending", true, true)]
        [TestCase("presenting", true, true)]
        [TestCase("ok", false, false)]
        [TestCase("record", true, false)]
        [TestCase("notDisplayed", true, false)]
        [TestCase("noBattle", true, false)]
        [TestCase("finished", true, false)]
        [TestCase("completion", true, false)]
        public void CanRequestAbandon_LiveDisplayedNotTerminalAndExitIdle(string scenario, bool exitIdle, bool expected)
        {
            // 대기 중 명령·연출 중·배치 단계는 막지 않는다. 남은 처리·표시는 골격이 기다린다(골격 설계 §6.5).
            var facts = new CombatInputFacts(
                isBattleDisplayed: scenario != "notDisplayed",
                hasBattle: scenario != "noBattle",
                phase: scenario == "finished" ? CombatFlowPhase.Finished : CombatFlowPhase.PlayerInput,
                stagePhase: scenario == "deploymentStage" ? StagePhase.Deployment : StagePhase.Combat,
                hasCompletion: scenario == "completion",
                executionMode: scenario == "record" ? CombatExecutionMode.RecordInputOnly : CombatExecutionMode.Live,
                currentLayer: StageInputLayers.Combat,
                hasPendingCommand: scenario == "pending",
                isPresenting: scenario == "presenting");

            Assert.That(CombatInputPolicy.CanRequestAbandon(facts, exitIdle), Is.EqualTo(expected));
        }

        #endregion

        private static CombatInputFacts Facts(
            bool isBattleDisplayed = true,
            bool hasBattle = true,
            CombatFlowPhase phase = CombatFlowPhase.PlayerInput,
            StagePhase stagePhase = StagePhase.Deployment,
            bool hasCompletion = false)
        {
            return new CombatInputFacts(isBattleDisplayed, hasBattle, phase, stagePhase, hasCompletion);
        }
    }
}
