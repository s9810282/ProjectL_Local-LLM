using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Run
{
    public interface IRunProgressBufferReadSink : IServiceModule
    {
        RunOutcome Outcome { get; }
        RunCompletionReceipt Completion { get; }
        /// <summary>
        /// 이번 Run의 스테이지 첫 손패 수(메인 포함). 새 Run 시작 때 시작 로드아웃에서 정해진다.
        /// 새 Run 시작을 거치지 않은 로스터(개발·테스트)면 null이며, 이때 Core는 기본 규칙을 쓴다.
        /// </summary>
        int? StartingHandSize { get; }
    }

    internal interface IRunProgressBufferStore : IServiceModule
    {
        RunCompletionReceipt PrepareCompletion(RunCompletionReceipt receipt);
        void InstallCompletion(RunCompletionReceipt prepared);
        void InstallStartingHandSize(int startingHandSize);
        void Reset();
    }

    /// <summary>종료 기록이 없으면 진행 중이며 명시적 초기화 전에는 다시 열지 않는다. Run 단위 설정(첫 손패 수)도 함께 보관한다.</summary>
    internal sealed class RunProgressBuffer : IRunProgressBufferReadSink, IRunProgressBufferStore,
        IExportSystemServices
    {
        public RunOutcome Outcome => Completion?.Outcome ?? RunOutcome.InProgress;
        public RunCompletionReceipt Completion { get; private set; }
        public int? StartingHandSize { get; private set; }

        RunCompletionReceipt IRunProgressBufferStore.PrepareCompletion(RunCompletionReceipt receipt)
        {
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));
            if (Completion != null) throw new InvalidOperationException("RunEnded: 이미 종료된 Run입니다.");
            return receipt;
        }

        void IRunProgressBufferStore.InstallCompletion(RunCompletionReceipt prepared) => Completion = prepared;

        void IRunProgressBufferStore.InstallStartingHandSize(int startingHandSize)
        {
            if (startingHandSize < 1) throw new ArgumentOutOfRangeException(nameof(startingHandSize));
            StartingHandSize = startingHandSize;
        }

        void IRunProgressBufferStore.Reset()
        {
            Completion = null;
            StartingHandSize = null;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRunProgressBufferReadSink>(this);
            sink.AddService<IRunProgressBufferStore>(this);
        }
    }
}
