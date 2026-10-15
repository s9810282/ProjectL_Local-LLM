using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 구조 개선 VR1: 명령 제출의 마지막 관문과 시작/턴 종료 대기의 유일한 원본(CombatCommandGate).
    /// 실제 CombatViewBuffer의 번호·목록으로 검증한다.
    /// </summary>
    public sealed class CombatCommandGateTests
    {
        private ICombatViewBufferReadSink _read;
        private CombatCommandGate _gate;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            IBufferQuery buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _read = buffers.CombatViewReadSink;
            _gate = new CombatCommandGate(buffers.CombatViewWriteSink);
        }

        [Test]
        public void SubmitPending_RecordsNumber_AndBlocksSecondSubmit()
        {
            Assert.That(_gate.SubmitPending(Deployment(), CombatViewCommandData.EnterCombat()), Is.True);
            Assert.That(_gate.Pending.Value.Kind, Is.EqualTo(CombatViewCommandKind.EnterCombat));
            Assert.That(_gate.Pending.Value.Number, Is.EqualTo(_read.Commands[0].Number));

            // 시점 규칙: 대기를 기록하기 전에 읽은 facts를 다시 써도 Gate의 현재 값으로 막는다.
            CombatInputFacts stale = Deployment();
            Assert.That(_gate.SubmitPending(stale, CombatViewCommandData.EnterCombat()), Is.False);
            Assert.That(_gate.TrySubmit(stale, CombatViewCommandData.DrawCharacter()), Is.Zero);
            Assert.That(_read.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void TrySubmit_DoesNotCreatePending()
        {
            Assert.That(_gate.TrySubmit(Deployment(), CombatViewCommandData.DrawCharacter()), Is.GreaterThan(0));

            Assert.That(_gate.HasPending, Is.False, "일반 명령은 대기를 만들지 않는다.");
        }

        [Test]
        public void RejectedByPolicy_SubmitsNothing_AndKeepsNoPending()
        {
            CombatInputFacts automatic = Deployment(CombatFlowPhase.Automatic);

            Assert.That(_gate.SubmitPending(automatic, CombatViewCommandData.EnterCombat()), Is.False);
            Assert.That(_gate.TrySubmit(automatic, CombatViewCommandData.DrawCharacter()), Is.Zero);
            Assert.That(_read.Commands, Is.Empty);
            Assert.That(_gate.HasPending, Is.False);
        }

        // CombatInteractionTests의 ResolvePending 사례를 원본 소유자로 옮겼다. 기대값은 같다.
        [Test]
        public void Resolve_OnlyMatchingCommandResult_ClearsIt_AcceptedOrRejected()
        {
            _gate.SubmitPending(Deployment(), CombatViewCommandData.EnterCombat());
            int number = _gate.Pending.Value.Number;

            _gate.Resolve(new CombatResult(1, CombatProcessingKind.Advance, 0, true, string.Empty));
            _gate.Resolve(new CombatResult(2, CombatProcessingKind.Command, number + 5, true, string.Empty));
            Assert.That(_gate.HasPending, Is.True, "다른 명령·자동 진행 결과는 대기를 풀지 않는다.");

            _gate.Resolve(new CombatResult(3, CombatProcessingKind.Command, number, false, "Rejected"));
            Assert.That(_gate.HasPending, Is.False);
        }

        [Test]
        public void Clear_EndsPending_AndKeepsCommandBuffer()
        {
            _gate.SubmitPending(Deployment(), CombatViewCommandData.EnterCombat());

            _gate.Clear();

            Assert.That(_gate.HasPending, Is.False);
            Assert.That(_read.Commands.Count, Is.EqualTo(1), "로컬 대기만 끝내고 명령 Buffer는 보존한다.");
        }

        private static CombatInputFacts Deployment(CombatFlowPhase phase = CombatFlowPhase.PlayerInput)
        {
            return new CombatInputFacts(true, true, phase, StagePhase.Deployment, false,
                CombatExecutionMode.Live, StageInputLayers.Deployment);
        }
    }
}
